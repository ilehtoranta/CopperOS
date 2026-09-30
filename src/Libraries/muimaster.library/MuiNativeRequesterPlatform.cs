/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using Amiga.MUI;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

internal enum MuiNativeRequesterRoute : byte
{
	Unsupported,
	IntuitionSystemFallback,
	ApplicationMui,
}

internal static class MuiNativeRequesterRouteCore
{
	internal static bool TrySelect(
		MuiRequesterCallRecord request,
		out MuiNativeRequesterRoute route)
	{
		route = MuiNativeRequesterRoute.Unsupported;
		// The MorphOS 3.20 MUImaster autodoc requires flags to be zero.
		// Do not adopt image/position extensions from other MUI targets.
		if (request.Flags != 0) return false;
		route = request.Application.IsNull
			? MuiNativeRequesterRoute.IntuitionSystemFallback
			: MuiNativeRequesterRoute.ApplicationMui;
		return true;
	}
}

// Native requester calls keep MorphOS ExtEasyStruct/TagItem records in guest
// storage for the duration of a synchronous call. MUI_RequestA uses its
// application-backed object tree and input pump; the null-application route
// uses Intuition's EasyRequestArgs. Object-reference consumption is reported
// only after a requester has actually completed, so admission/setup failures
// do not release caller-owned objects.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeRequesterPlatform : IMuiRequesterCapability,
	IMuiRequesterWindowCapability, IMuiRequesterApplicationTitleCapability
{
	// Keep the generic requester capability within the native compiler's
	// single-slot shared ABI. The existing class-platform handle is one named
	// context pointer; owner, requester state, and object-registry addresses are
	// read from that guest-resident owner record when needed.
	internal MuiNativeClassPlatform Classes;

	internal APTR IntuitionBase => Classes.IntuitionBase;
	internal APTR OwnerRoot => Classes.OwnerRoot;
	internal APTR PublicObjects
	{
		get
		{
			var memory = default(MuiNativeClassMemory);
			return MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(
				ref memory, Classes.Context, out var address)
				? address : APTR.Null;
		}
	}
	internal APTR ServiceState
	{
		get
		{
			var memory = default(MuiNativeClassMemory);
			return MuiNativeClassOwnerCodec.TryGetRequesterStateAddress(
				ref memory, Classes.Context, out var address)
				? address : APTR.Null;
		}
	}

	public bool IsMapped(APTR address, uint size) =>
		default(MuiNativeClassMemory).IsMapped(address, size);
	public byte ReadUInt8(APTR address, int offset = 0) =>
		APTR.ReadUInt8(address, offset);
	public ushort ReadUInt16(APTR address, int offset = 0) =>
		APTR.ReadUInt16(address, offset);
	public uint ReadUInt32(APTR address, int offset = 0) =>
		APTR.ReadUInt32(address, offset);
	public void WriteUInt8(APTR address, int offset, byte value) =>
		APTR.WriteUInt8(address, offset, value);
	public void WriteUInt16(APTR address, int offset, ushort value) =>
		APTR.WriteUInt16(address, offset, value);
	public void WriteUInt32(APTR address, int offset, uint value) =>
		APTR.WriteUInt32(address, offset, value);
	public void Clear(APTR address, uint size) =>
		default(MuiNativeClassMemory).Clear(address, size);
	public void Copy(APTR source, APTR destination, uint size) =>
		default(MuiNativeClassMemory).Copy(source, destination, size);
	public APTR Allocate(uint size, uint flags) => Exec.AllocMem(size,
		(Exec.MemoryFlags)flags);
	public void Free(APTR address, uint size) => Exec.FreeMem(address, size);

	public int Request(MuiRequesterCallRecord request) =>
		RequestCore(request, out _);

	private int RequestCore(MuiRequesterCallRecord request,
		out bool requesterCompleted)
	{
		requesterCompleted = false;
		if (!MuiNativeRequesterRouteCore.TrySelect(request, out var route))
			return 0;

		var platform = this;
		if (route == MuiNativeRequesterRoute.ApplicationMui)
		{
			if (IntuitionBase.IsNull ||
				!MuiNativeRequesterApplicationAdmissionCore.TryValidate(
					ref platform, request, PublicObjects, OwnerRoot,
					out var admission)) return 0;
			return MuiNativeRequesterApplicationPresenter.Request(ref platform,
				ServiceState, request, admission);
		}
		if (route != MuiNativeRequesterRoute.IntuitionSystemFallback ||
			IntuitionBase.IsNull) return 0;
		if (!MuiNativeRequesterWindowCore.TryResolve(ref platform,
			request.Window, out var windowResolution)) return 0;
		if (!MuiNativeRequesterPreparationCore.TryPrepare(ref platform,
			request.Title, request.Gadgets, request.Format, out var preparation))
			return 0;
		var call = MuiNativeRequesterPreparationCore.CreateCall(preparation,
			windowResolution.IntuitionWindow);
		var previousIntuitionBase = Intuition.IntuitionLibraryBase;
		Intuition.IntuitionLibraryBase = IntuitionBase;
		var result = Intuition.EasyRequestArgs(call.Window.Raw,
			call.EasyStruct.Raw, call.Idcmp.Raw, call.Arguments.Raw);
		requesterCompleted = true;
		Intuition.IntuitionLibraryBase = previousIntuitionBase;
		MuiNativeRequesterPreparationCore.Release(ref platform, ref preparation);
		return result;
	}

	public bool TryGetNativeWindow(APTR muiWindow, out APTR intuitionWindow)
	{
		intuitionWindow = APTR.Null;
		if (muiWindow.IsNull || IntuitionBase.IsNull) return false;
		var storage = Allocate(MuiGuestUlongStorage.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (storage.IsNull || !IsMapped(storage,
			MuiGuestUlongStorage.Size))
		{
			if (storage.IsNotNull)
				Free(storage, MuiGuestUlongStorage.Size);
			return false;
		}

		var memory = default(MuiNativeClassMemory);
		var nativeWindow = 0u;
		var resolved = MuiGuestUlongStorageCodec.WriteValue(ref memory,
			storage, 0) && MuiNativeIntuitionCalls.GetAttr(IntuitionBase,
				MuiWindowPublicCore.Window, muiWindow, storage) != 0 &&
			MuiGuestUlongStorageCodec.TryReadValue(ref memory, storage,
				out nativeWindow);
		if (resolved)
			intuitionWindow = APTR.FromPointer(nativeWindow);
		Free(storage, MuiGuestUlongStorage.Size);
		return resolved;
	}

	public bool TryGetApplicationTitle(APTR application, out APTR title)
	{
		title = APTR.Null;
		if (application.IsNull || IntuitionBase.IsNull) return false;
		var storage = Allocate(MuiGuestUlongStorage.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (storage.IsNull || !IsMapped(storage, MuiGuestUlongStorage.Size))
		{
			if (storage.IsNotNull)
				Free(storage, MuiGuestUlongStorage.Size);
			return false;
		}

		var memory = default(MuiNativeClassMemory);
		var titleRaw = 0u;
		var resolved = MuiGuestUlongStorageCodec.WriteValue(ref memory,
			storage, 0) && MuiNativeIntuitionCalls.GetAttr(IntuitionBase,
			MUIConstants.MUIA_Application_Title, application, storage) != 0 &&
			MuiGuestUlongStorageCodec.TryReadValue(ref memory, storage,
			out titleRaw);
		if (resolved) title = APTR.FromPointer(titleRaw);
		Free(storage, MuiGuestUlongStorage.Size);
		return resolved;
	}

	public int RequestObject(MuiRequesterCallRecord request,
		out bool consumeReference)
	{
		consumeReference = false;
		if (!MuiNativeRequesterRouteCore.TrySelect(request, out var route))
			return 0;
		var platform = this;
		if (route == MuiNativeRequesterRoute.IntuitionSystemFallback)
		{
			var result = RequestCore(request, out var requesterCompleted);
			consumeReference = requesterCompleted && request.Object.IsNotNull;
			return result;
		}
		if (route != MuiNativeRequesterRoute.ApplicationMui ||
			IntuitionBase.IsNull ||
			!MuiNativeRequesterApplicationAdmissionCore.TryValidate(ref platform,
				request, PublicObjects, OwnerRoot, out var admission)) return 0;
		return MuiNativeRequesterApplicationPresenter.RequestObject(ref platform,
			ServiceState, request, admission, out consumeReference);
	}

	public bool ReleaseObject(APTR obj) =>
		MuiNativePublicObjectCore.DisposeObject(ref Classes, ServiceState,
			OwnerRoot, PublicObjects, obj);
}
