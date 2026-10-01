/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Borrowed native pointers follow the same valid-address contract as the OS C
// APIs. IsMapped checks null/alignment-independent range arithmetic, not an MMU
// probe. Ownership is represented separately by the root/binding records.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeClassMemory : IMuiGuestMemory
{
	// The freestanding generic ABI requires a scalar carrier even for a
	// stateless memory capability. This reserved value is ABI padding, not a
	// cached OS base or a guest-memory claim.
	private uint _abiSlot;
	public bool IsMapped(APTR address, uint size)
	{
		_ = _abiSlot;
		return address.IsNotNull && size != 0 && address.Raw <= uint.MaxValue - size;
	}
	public byte ReadUInt8(APTR address, int offset = 0) => APTR.ReadUInt8(address, offset);
	public ushort ReadUInt16(APTR address, int offset = 0) => APTR.ReadUInt16(address, offset);
	public uint ReadUInt32(APTR address, int offset = 0) => APTR.ReadUInt32(address, offset);
	public void WriteUInt8(APTR address, int offset, byte value) => APTR.WriteUInt8(address, offset, value);
	public void WriteUInt16(APTR address, int offset, ushort value) => APTR.WriteUInt16(address, offset, value);
	public void WriteUInt32(APTR address, int offset, uint value) => APTR.WriteUInt32(address, offset, value);
	public void Clear(APTR address, uint count)
	{
		if (!IsMapped(address, count)) return;
		for (uint index = 0; index < count; index++) APTR.WriteUInt8(APTR.FromPointer(address.Raw + index), 0, 0);
	}
	public void Copy(APTR source, APTR destination, uint count)
	{
		if (!IsMapped(source, count) || !IsMapped(destination, count)) return;
		if (destination.Raw > source.Raw)
		{
			for (var index = count; index != 0;)
			{
				index--;
				APTR.WriteUInt8(APTR.FromPointer(destination.Raw + index), 0,
					APTR.ReadUInt8(APTR.FromPointer(source.Raw + index), 0));
			}
		}
		else
		{
			for (uint index = 0; index < count; index++)
				APTR.WriteUInt8(APTR.FromPointer(destination.Raw + index), 0,
					APTR.ReadUInt8(APTR.FromPointer(source.Raw + index), 0));
		}
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeClassBinding
{
	// The binding is the native-class sidecar.  Keep the live-call and live-
	// object pins in the same declaration-ordered record so class retirement
	// never has to infer lifecycle state from a guest offset or an IClass field.
	internal const uint Size = 40;
	internal const uint NativeClassFreed = 1;
	internal const uint NativeObjectLinkPoisoned = 2;
	internal APTR Next;
	internal APTR Class;
	internal APTR Dispatcher;
	internal APTR LibraryBase;
	internal APTR OwnerRoot;
	internal APTR SidecarClass;
	internal APTR ObjectHead;
	internal uint ActiveCalls;
	internal uint ActiveObjects;
	internal uint LifecycleFlags;
}

internal static class MuiNativeClassBindingCodec
{
	internal static bool TryRead<T>(ref T memory, APTR address, out MuiNativeClassBinding value)
		where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address, MuiNativeClassBinding.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var next) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var cls) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var dispatcher) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var library) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var root) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var sidecar) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var objectHead) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.ActiveCalls) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.ActiveObjects) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.LifecycleFlags)) return false;
		value.Next = APTR.FromPointer(next);
		value.Class = APTR.FromPointer(cls);
		value.Dispatcher = APTR.FromPointer(dispatcher);
		value.LibraryBase = APTR.FromPointer(library);
		value.OwnerRoot = APTR.FromPointer(root);
		value.SidecarClass = APTR.FromPointer(sidecar);
		value.ObjectHead = APTR.FromPointer(objectHead);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<T>(ref T memory, APTR address, MuiNativeClassBinding value)
		where T : struct, IMuiGuestMemory
	{
		return MuiGuestStructCursor.TryCreate(ref memory, address, MuiNativeClassBinding.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Next.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Class.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Dispatcher.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.LibraryBase.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.OwnerRoot.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.SidecarClass.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.ObjectHead.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.ActiveCalls) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.ActiveObjects) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.LifecycleFlags) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}
}

// One native custom-class object pin.  This is deliberately separate from
// MuiNativePublicObjectBinding: direct Intuition objects have no class-service
// lease to release, but still need a guest-resident identity for exactly-once
// OM_NEW/OM_DISPOSE retirement.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeClassObjectBinding
{
	internal const uint Size = 32;
	internal const uint Magic = 0x4D434F42; // "MCOB"
	internal const uint NativeDisposed = 1;
	internal const uint PublicSidecarBorrowed = 2;
	internal uint Signature;
	internal APTR Next;
	internal APTR Object;
	internal APTR Class;
	internal APTR OwnerRoot;
	internal APTR Sidecar;
	internal uint ActiveCalls;
	internal uint LifecycleFlags;
}

internal static class MuiNativeClassObjectBindingCodec
{
	internal static bool TryRead<T>(ref T memory, APTR address,
		out MuiNativeClassObjectBinding value)
		where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeClassObjectBinding.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Signature) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var next) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var obj) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var cls) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var root) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var sidecar) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.ActiveCalls) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.LifecycleFlags) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Next = APTR.FromPointer(next);
		value.Object = APTR.FromPointer(obj);
		value.Class = APTR.FromPointer(cls);
		value.OwnerRoot = APTR.FromPointer(root);
		value.Sidecar = APTR.FromPointer(sidecar);
		return true;
	}

	internal static bool Write<T>(ref T memory, APTR address,
		MuiNativeClassObjectBinding value)
		where T : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeClassObjectBinding.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Signature) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Next.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Object.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Class.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.OwnerRoot.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Sidecar.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.ActiveCalls) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.LifecycleFlags) &&
		MuiGuestStructCursor.IsComplete(cursor);
}

// Caller-owned native context. Its address is the scalar capability handle;
// both named pointers remain in guest storage for the complete operation.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeClassContext
{
	internal const uint FieldSize = 4;
	internal const uint Size = 8;
	internal APTR IntuitionBase;
	internal APTR OwnerRoot;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeClassContextLocations
{
	internal APTR IntuitionBase;
	internal APTR OwnerRoot;
}

internal static class MuiNativeClassContextCodec
{
	internal static bool TryRead<T>(ref T memory, APTR address, out MuiNativeClassContext value)
		where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address, MuiNativeClassContext.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var intuition) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var root)) return false;
		value.IntuitionBase = APTR.FromPointer(intuition);
		value.OwnerRoot = APTR.FromPointer(root);
		return MuiGuestStructCursor.IsComplete(cursor);
	}
	internal static bool Write<T>(ref T memory, APTR address, MuiNativeClassContext value)
		where T : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address, MuiNativeClassContext.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.IntuitionBase.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.OwnerRoot.Raw) &&
		MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryGetLocations<T>(ref T memory, APTR address,
		out MuiNativeClassContextLocations value) where T : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref memory, address, MuiNativeClassContext.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeClassContext.FieldSize, out value.IntuitionBase) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeClassContext.FieldSize, out value.OwnerRoot) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Native class services borrow a live context and pinned Intuition base. The
// embedding library owns provider acquisition and context storage; this handle
// fits the generic native ABI without cached OS globals or lost context fields.
// Every custom dispatcher binding is linked to a live owned root, preventing
// root destruction until its native class can actually be freed.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeClassPlatform : IMuiClassServicePlatform,
	IMuiBoopsiCapability, IMuiDrawingServicePlatform,
	IMuiDoubleBufferCapability, IMuiCallbackCapability,
	IMuiNativeApplicationLoopPlatform, IMuiNativeRawKeyMapCapability
{
	internal APTR Context;
	internal APTR IntuitionBase
	{
		get
		{
			var memory = default(MuiNativeClassMemory);
			if (MuiNativeClassContextCodec.TryRead(ref memory, Context, out var value)) return value.IntuitionBase;
			return APTR.Null;
		}
	}
	internal APTR OwnerRoot
	{
		get
		{
			var memory = default(MuiNativeClassMemory);
			if (MuiNativeClassContextCodec.TryRead(ref memory, Context, out var value)) return value.OwnerRoot;
			return APTR.Null;
		}
	}
	internal APTR GraphicsBase
	{
		get
		{
			var memory = default(MuiNativeClassMemory);
			if (!MuiMasterPrivateRootCodec.TryRead(ref memory, OwnerRoot,
				out var root)) return APTR.Null;
			var owner = APTR.FromPointer(root.LoaderState);
			if (!MuiNativeClassOwnerCodec.TryRead(ref memory, owner,
				out var value)) return APTR.Null;
			return value.GraphicsBase;
		}
	}
	internal APTR KeymapBase
	{
		get
		{
			var memory = default(MuiNativeClassMemory);
			if (!MuiMasterPrivateRootCodec.TryRead(ref memory, OwnerRoot,
				out var root)) return APTR.Null;
			var owner = APTR.FromPointer(root.LoaderState);
			if (!MuiNativeClassOwnerCodec.TryRead(ref memory, owner,
				out var value)) return APTR.Null;
			return value.KeymapBase;
		}
	}
	public bool IsMapped(APTR address, uint size) => default(MuiNativeClassMemory).IsMapped(address, size);
	public byte ReadUInt8(APTR address, int offset = 0) => APTR.ReadUInt8(address, offset);
	public ushort ReadUInt16(APTR address, int offset = 0) => APTR.ReadUInt16(address, offset);
	public uint ReadUInt32(APTR address, int offset = 0) => APTR.ReadUInt32(address, offset);
	public void WriteUInt8(APTR address, int offset, byte value) => APTR.WriteUInt8(address, offset, value);
	public void WriteUInt16(APTR address, int offset, ushort value) => APTR.WriteUInt16(address, offset, value);
	public void WriteUInt32(APTR address, int offset, uint value) => APTR.WriteUInt32(address, offset, value);
	public void Clear(APTR address, uint size) => default(MuiNativeClassMemory).Clear(address, size);
	public void Copy(APTR source, APTR destination, uint size) => default(MuiNativeClassMemory).Copy(source, destination, size);
	public APTR Allocate(uint size, uint flags) => Exec.AllocMem(size, (Exec.MemoryFlags)flags);
	public void Free(APTR address, uint size) => Exec.FreeMem(address, size);
	public APTR OpenLibrary(APTR name, ushort version) => Exec.OpenLibraryRaw(CString.FromPointer(name.Raw), version);
	public void CloseLibrary(APTR library) => Exec.CloseLibrary(library);
	public bool TryMapRawKey(APTR inputEvent, out byte character) =>
		MuiNativeKeymapCalls.TryMapRawKey(KeymapBase, inputEvent, out character);
	public APTR NewObject(APTR cls, APTR tags) => MuiNativeIntuitionCalls.NewObject(IntuitionBase, cls, tags);
	public void DisposeObject(APTR obj) => MuiNativeIntuitionCalls.DisposeObject(IntuitionBase, obj);
	public uint DoMethod(APTR obj, APTR message) =>
		MuiNativeBoopsiDispatch.DoMethod(ref this, obj, message);
	public uint WaitMuiSignals(uint signalMask) => Exec.Wait(signalMask);
	public uint CoerceMethod(APTR classPointer, APTR obj, APTR message) =>
		MuiNativeBoopsiDispatch.CoerceMethod(ref this, classPointer, obj,
			message);
	public uint InvokeHook(APTR hook, APTR objectAddress, APTR messageAddress)
	{
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeClassOwnerCodec.TryRead(ref memory, OwnerRoot,
			out var owner) || owner.UtilityBase.IsNull) return 0;
		return MuiNativeUtilityCalls.CallHookPkt(owner.UtilityBase, hook,
			objectAddress, messageAddress);
	}
	public bool LockLayer(APTR layer) =>
		MuiNativeDrawingPlatformCore.LockLayer(ref this, layer);
	public void UnlockLayer(APTR layer) =>
		MuiNativeDrawingPlatformCore.UnlockLayer(ref this, layer);
	public bool BeginUpdate(APTR layer) =>
		MuiNativeDrawingPlatformCore.BeginUpdate(ref this, layer);
	public void EndUpdate(APTR layer, bool completed) =>
		MuiNativeDrawingPlatformCore.EndUpdate(ref this, layer, completed);
	public APTR PushClip(APTR layer, int left, int top, int width, int height) =>
		MuiNativeDrawingPlatformCore.PushClip(ref this, layer, left, top,
			width, height);
	public void PopClip(APTR layer, APTR previous) =>
		MuiNativeDrawingPlatformCore.PopClip(ref this, layer, previous);
	public APTR InstallClipRegion(APTR layer, APTR region) =>
		MuiNativeDrawingPlatformCore.InstallClipRegion(ref this, layer, region);
	public void RestoreClipRegion(APTR layer, APTR previousRegion) =>
		MuiNativeDrawingPlatformCore.RestoreClipRegion(ref this, layer,
			previousRegion);
	public int ObtainPen(APTR renderInfo, APTR penSpec, uint flags) =>
		MuiNativeDrawingPlatformCore.ObtainPen(ref this, renderInfo, penSpec,
			flags);
	public void ReleasePen(APTR renderInfo, int pen) =>
		MuiNativeDrawingPlatformCore.ReleasePen(ref this, renderInfo, pen);
	public bool GetRGBColor(APTR renderInfo, APTR penSpec, APTR rgbColor) =>
		MuiNativeDrawingPlatformCore.GetRGBColor(ref this, renderInfo, penSpec,
			rgbColor);
	public bool BeginMuiDoubleBuffer(ref MuiDoubleBufferRenderRequest request) =>
		MuiNativeAreaDoubleBufferGraphicsCore.Begin(ref this, ref request);
	public bool EndMuiDoubleBuffer(ref MuiDoubleBufferRenderRequest request,
		bool completed) => MuiNativeAreaDoubleBufferGraphicsCore.End(ref this,
			ref request, completed);

	public APTR MakeClass(APTR classId, APTR superClass, ushort size, APTR dispatcher)
	{
		APTR cls;
		if (superClass.IsNull)
			cls = MuiNativeIntuitionCalls.MakeClass(IntuitionBase, classId,
				APTR.FromPointer(CString.ToUInt32(CString.FromLiteral("rootclass"))), superClass, size);
		else cls = MuiNativeIntuitionCalls.MakeClass(IntuitionBase, classId, APTR.Null, superClass, size);
		if (cls.IsNull) return APTR.Null;
		var value = ReadNativeClass(cls);
		value.cl_Dispatcher.Entry = dispatcher;
		WriteNativeClass(cls, value);
		return cls;
	}
	public bool AddClass(APTR cls)
	{
		if (cls.IsNull) return false;
		MuiNativeIntuitionCalls.AddClass(IntuitionBase, cls);
		return (ReadNativeClass(cls).cl_Flags & BOOPSI.CLF_INLIST) != 0;
	}
	public bool RemoveClass(APTR cls)
	{
		if (cls.IsNull) return false;
		MuiNativeIntuitionCalls.RemoveClass(IntuitionBase, cls);
		return (ReadNativeClass(cls).cl_Flags & BOOPSI.CLF_INLIST) == 0;
	}
	public bool FreeClass(APTR cls) => MuiNativeIntuitionCalls.FreeClass(IntuitionBase, cls);
	public APTR ResolveExternalClass(APTR library, APTR classId)
	{
		if (library.IsNull || classId.IsNull) return APTR.Null;
		var mcc = MuiNativeIntuitionCalls.QueryCustomClass(library);
		return MuiCustomClassCodec.TryRead(ref this, mcc, out var value) ? value.Class : APTR.Null;
	}

	public APTR MakeCustomClass(APTR superClass, ushort size, APTR dispatcher, APTR library)
	{
		if (IntuitionBase.IsNull || superClass.IsNull || dispatcher.IsNull || OwnerRoot.IsNull) return APTR.Null;
		Exec.Forbid();
		var result = CreateCustomLocked(superClass, size, dispatcher, library);
		Exec.Permit();
		return result;
	}
	private APTR CreateCustomLocked(APTR superClass, ushort size, APTR dispatcher, APTR library)
	{
		if (!MuiMasterPrivateRootCodec.TryRead(ref this, OwnerRoot, out var root) || root.RegistryGeneration == 0)
			return APTR.Null;
		var binding = Allocate(MuiNativeClassBinding.Size, (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear));
		if (binding.IsNull) return APTR.Null;
		var cls = MuiNativeIntuitionCalls.MakeClass(IntuitionBase, APTR.Null, APTR.Null, superClass, size);
		if (cls.IsNull) { Free(binding, MuiNativeClassBinding.Size); return APTR.Null; }
		var sidecar = APTR.Null;
		if (root.LoaderState != 0 || root.ClassRegistry != 0)
		{
			if (!TryGetOwnerRegistry(ref root, out var registry))
			{
				MuiNativeIntuitionCalls.FreeClass(IntuitionBase, cls);
				Free(binding, MuiNativeClassBinding.Size);
				return APTR.Null;
			}
			sidecar = MuiHeadlessObjectCore.RegisterNativeClass(ref this,
				registry, cls, superClass, size);
			if (sidecar.IsNull)
			{
				MuiNativeIntuitionCalls.FreeClass(IntuitionBase, cls);
				Free(binding, MuiNativeClassBinding.Size);
				return APTR.Null;
			}
		}
		var value = default(MuiNativeClassBinding);
		value.Next = APTR.FromPointer(root.ExternalClassHead);
		value.Class = cls;
		value.Dispatcher = dispatcher;
		value.LibraryBase = library;
		value.OwnerRoot = OwnerRoot;
		value.SidecarClass = sidecar;
		if (!MuiNativeClassBindingCodec.Write(ref this, binding, value))
		{
			if (sidecar.IsNotNull && TryGetOwnerRegistry(ref root, out var registry))
				MuiHeadlessObjectCore.DeleteClass(ref this, registry, sidecar);
			MuiNativeIntuitionCalls.FreeClass(IntuitionBase, cls);
			Free(binding, MuiNativeClassBinding.Size);
			return APTR.Null;
		}
		var classValue = ReadNativeClass(cls);
		classValue.cl_Dispatcher.Entry = APTR.ExportAddress(MuiNativeClassDispatcher.ExportName);
		classValue.cl_Dispatcher.SubEntry = APTR.Null;
		classValue.cl_Dispatcher.Data = binding;
		WriteNativeClass(cls, classValue);
		root.ExternalClassHead = binding.Raw;
		if (!MuiMasterPrivateRootCodec.Write(ref this, OwnerRoot, root))
		{
			if (sidecar.IsNotNull && TryGetOwnerRegistry(ref root, out var registry))
				MuiHeadlessObjectCore.DeleteClass(ref this, registry, sidecar);
			MuiNativeIntuitionCalls.FreeClass(IntuitionBase, cls);
			Free(binding, MuiNativeClassBinding.Size);
			return APTR.Null;
		}
		return cls;
	}

	public bool FreeCustomClass(APTR cls)
	{
		if (cls.IsNull || IntuitionBase.IsNull || OwnerRoot.IsNull) return false;
		Exec.Forbid();
		var result = FreeCustomLocked(cls);
		Exec.Permit();
		return result;
	}
	private bool FreeCustomLocked(APTR cls)
	{
		if (!MuiMasterPrivateRootCodec.TryRead(ref this, OwnerRoot, out var root)) return false;
		var current = APTR.FromPointer(root.ExternalClassHead);
		var previous = APTR.Null;
		var previousValue = default(MuiNativeClassBinding);
		for (uint visited = 0; current.IsNotNull && visited < 65536; visited++)
		{
			if (!MuiNativeClassBindingCodec.TryRead(ref this, current, out var value) || value.OwnerRoot != OwnerRoot)
				return false;
			if (value.Class == cls)
			{
				if (value.ActiveCalls != 0 || value.ActiveObjects != 0 ||
					value.ObjectHead.IsNotNull) return false;
				if ((value.LifecycleFlags & MuiNativeClassBinding.NativeClassFreed) == 0)
				{
					if (!MuiNativeIntuitionCalls.FreeClass(IntuitionBase, cls)) return false;
					value.LifecycleFlags |= MuiNativeClassBinding.NativeClassFreed;
					if (!MuiNativeClassBindingCodec.Write(ref this, current, value)) return false;
				}
				if (value.SidecarClass.IsNotNull)
				{
					if (!TryGetOwnerRegistry(ref root, out var registry) ||
						!MuiHeadlessObjectCore.DeleteClass(ref this, registry,
							value.SidecarClass)) return false;
					value.SidecarClass = APTR.Null;
					if (!MuiNativeClassBindingCodec.Write(ref this, current, value)) return false;
				}
				if (previous.IsNull)
				{
					root.ExternalClassHead = value.Next.Raw;
					if (!MuiMasterPrivateRootCodec.Write(ref this, OwnerRoot, root)) return false;
				}
				else
				{
					previousValue.Next = value.Next;
					if (!MuiNativeClassBindingCodec.Write(ref this, previous, previousValue)) return false;
				}
				Free(current, MuiNativeClassBinding.Size);
				return true;
			}
			previous = current;
			previousValue = value;
			current = value.Next;
		}
		return false;
	}

	private bool TryGetOwnerRegistry(ref MuiMasterPrivateRoot root,
		out APTR registry)
	{
		registry = APTR.Null;
		if (root.LoaderState == 0 || root.ClassRegistry == 0) return false;
		var owner = APTR.FromPointer(root.LoaderState);
		if (!MuiNativeClassOwnerCodec.TryRead(ref this, owner, out var value) ||
			value.Context.OwnerRoot != OwnerRoot ||
			!MuiNativeClassOwnerCodec.TryGetRegistryAddress(ref this, owner,
				out registry) || registry.Raw != root.ClassRegistry) return false;
		return MuiHeadlessStateCodec.TryRead(ref this, registry, out var state) &&
			state.Magic == MuiHeadlessLayout.Magic &&
			state.Version == MuiHeadlessLayout.Version;
	}

	// SDK structure access uses the stateless memory capability. Class-service
	// generic calls use the scalar handle to the complete named provider context.
	private static IClass ReadNativeClass(APTR cls)
	{
		var memory = default(MuiNativeClassMemory);
		return BOOPSIGuestCodec.ReadClass(ref memory, cls);
	}
	private static void WriteNativeClass(APTR cls, IClass value)
	{
		var memory = default(MuiNativeClassMemory);
		BOOPSIGuestCodec.WriteClass(ref memory, cls, value);
	}
}

public static class MuiNativeClassDispatcher
{
	public const string ExportName = "copperos.mui.custom.dispatch";
	internal enum NativeObjectSidecarResolution : uint
	{
		NotOwned = 0,
		Found = 1,
		Invalid = 2,
	}

	// A public MUI constructor may run only after the native OM_NEW chain has
	// returned. Reuse the top native custom class's named sidecar instead of
	// allocating a second record for the same object. External Intuition classes
	// retain the ordinary public-object allocation path.
	internal static NativeObjectSidecarResolution ResolveNativeObjectSidecar(
		ref MuiNativeClassMemory memory, APTR cls, APTR ownerRoot, APTR obj,
		out APTR sidecar, out APTR objectBindingAddress)
	{
		sidecar = APTR.Null;
		objectBindingAddress = APTR.Null;
		if (cls.IsNull || ownerRoot.IsNull || obj.IsNull)
			return NativeObjectSidecarResolution.Invalid;
		var classValue = BOOPSIGuestCodec.ReadClass(ref memory, cls);
		if (classValue.cl_Dispatcher.Entry !=
			APTR.ExportAddress(ExportName))
			return NativeObjectSidecarResolution.NotOwned;
		var bindingAddress = classValue.cl_Dispatcher.Data;
		if (!TryFindNativeObjectSidecar(ref memory, bindingAddress, cls,
			ownerRoot, obj, out sidecar, out objectBindingAddress))
			return NativeObjectSidecarResolution.Invalid;
		return NativeObjectSidecarResolution.Found;
	}

	// This helper deliberately accepts an explicit typed binding address so the
	// complete object-list and sidecar contracts can be qualified with bounded
	// host memory, independently of the compiler's native export relocation.
	internal static bool TryFindNativeObjectSidecar<TMemory>(ref TMemory memory,
		APTR bindingAddress, APTR cls, APTR ownerRoot, APTR obj,
		out APTR sidecarAddress, out APTR objectBindingAddress)
		where TMemory : struct, IMuiGuestMemory
	{
		sidecarAddress = APTR.Null;
		objectBindingAddress = APTR.Null;
		if (bindingAddress.IsNull || cls.IsNull || ownerRoot.IsNull || obj.IsNull ||
			!MuiNativeClassBindingCodec.TryRead(ref memory, bindingAddress,
				out var binding) || binding.Class != cls ||
			binding.OwnerRoot != ownerRoot || binding.Dispatcher.IsNull ||
			(binding.LifecycleFlags & MuiNativeClassBinding.NativeClassFreed) != 0 ||
			!IsNativeObjectListValid(ref memory, binding.ObjectHead, ownerRoot,
				cls, obj, out var found) || !found)
			return false;

		var current = binding.ObjectHead;
		for (uint visited = 0; current.IsNotNull &&
			visited < MuiHeadlessLayout.MaximumTraversal; visited++)
		{
			if (!MuiNativeClassObjectBindingCodec.TryRead(ref memory, current,
				out var objectBinding)) return false;
			if (objectBinding.Object == obj)
			{
				if (objectBinding.Sidecar.IsNull ||
					!MuiNativeMuiObjectCodec.TryRead(ref memory,
						objectBinding.Sidecar, out var sidecar) ||
					sidecar.Object != obj || sidecar.Class != cls ||
					sidecar.OwnerRoot != ownerRoot ||
					sidecar.LifecycleState != MuiNativeMuiObjectRecord.StateLive ||
					(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) == 0 ||
					(sidecar.Flags & (MuiNativeMuiObjectRecord.ObjectDisposing |
						MuiNativeMuiObjectRecord.ObjectNativeDisposed)) != 0)
					return false;
					sidecarAddress = objectBinding.Sidecar;
				objectBindingAddress = current;
				return true;
			}
			current = objectBinding.Next;
		}
		return false;
	}

	internal static bool TrySetPublicSidecarBorrowed<TMemory>(ref TMemory memory,
		APTR objectBindingAddress, APTR cls, APTR ownerRoot, APTR obj,
		APTR sidecarAddress)
		where TMemory : struct, IMuiGuestMemory
	{
		if (objectBindingAddress.IsNull || cls.IsNull || ownerRoot.IsNull ||
			obj.IsNull || sidecarAddress.IsNull ||
			!MuiNativeClassObjectBindingCodec.TryRead(ref memory,
				objectBindingAddress, out var objectBinding) ||
			objectBinding.Signature != MuiNativeClassObjectBinding.Magic ||
			objectBinding.Object != obj || objectBinding.Class != cls ||
			objectBinding.OwnerRoot != ownerRoot ||
			objectBinding.Sidecar != sidecarAddress ||
			(objectBinding.LifecycleFlags &
				MuiNativeClassObjectBinding.PublicSidecarBorrowed) != 0)
			return false;
		objectBinding.LifecycleFlags |=
			MuiNativeClassObjectBinding.PublicSidecarBorrowed;
		return MuiNativeClassObjectBindingCodec.Write(ref memory,
			objectBindingAddress, objectBinding);
	}

	[M68kExport(ExportName)]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint Dispatch([M68kRegister(M68kRegister.A0)] APTR cls,
		[M68kRegister(M68kRegister.A2)] APTR obj, [M68kRegister(M68kRegister.A1)] APTR message,
		[M68kRegister(M68kRegister.A6)] APTR callerBase)
	{
		var memory = default(MuiNativeClassMemory);
		if (cls.IsNull || message.IsNull) return 0;
		var hasMethod = MuiBoopsiMethodMessageCodec.TryReadMethodId(
			ref memory, message, out var method);
		var receivedSignals = 0u;
		if (hasMethod && (method == MuiApplicationDispatcher.ApplicationInputMethod ||
			method == MuiApplicationDispatcher.ApplicationNewInputMethod) &&
			MuiApplicationInputMessageCodec.TryRead(ref memory, message,
				out var inputMessage) && inputMessage.MethodId == method &&
			MuiApplicationWindowSignalStorageCodec.TryRead(ref memory,
				inputMessage.SignalStorage, out var inputSignalStorage))
			receivedSignals = inputSignalStorage.Signals;
		var canProjectNativeMethod = true;
		var nestedSuperDispatch = false;
		Exec.Forbid();
		var binding = BOOPSIGuestCodec.ReadClass(ref memory, cls).cl_Dispatcher.Data;
		if (!MuiNativeClassBindingCodec.TryRead(ref memory, binding, out var value) ||
			value.Class != cls || value.Dispatcher.IsNull || value.OwnerRoot.IsNull ||
			(value.LifecycleFlags & MuiNativeClassBinding.NativeClassFreed) != 0 ||
			value.ActiveCalls == uint.MaxValue ||
			!MuiMasterPrivateRootCodec.TryRead(ref memory, value.OwnerRoot, out var root) ||
			root.RegistryGeneration == 0 || root.ActiveCallbackDepth == uint.MaxValue)
		{
			Exec.Permit();
			return 0;
		}
		if (hasMethod && root.LoaderState != 0)
		{
			var owner = APTR.FromPointer(root.LoaderState);
			var task = Exec.FindTask(CString.FromPointer(0));
			canProjectNativeMethod =
				MuiNativeClassDispatchContextCore.TryHasSuperDispatchFrame(
					ref memory, owner, task, cls, obj, message, method,
					out nestedSuperDispatch);
		}
		value.ActiveCalls++;
		root.ActiveCallbackDepth++;
		MuiNativeClassBindingCodec.Write(ref memory, binding, value);
		MuiMasterPrivateRootCodec.Write(ref memory, value.OwnerRoot, root);
		Exec.Permit();
		var callbackBase = callerBase;
		if (value.LibraryBase.IsNotNull) callbackBase = value.LibraryBase;
		var result = 0u;
		var stateHandled = false;
		if (hasMethod)
		{
			stateHandled = TryDispatchNativeState(ref memory, value.OwnerRoot,
				obj, message, method, out result);
		}
		var areaDoubleBufferLease = default(MuiNativeAreaDoubleBufferLease);
		var areaDoubleBufferPlatform = default(MuiNativeClassPlatform);
		areaDoubleBufferPlatform.Context = APTR.FromPointer(root.LoaderState);
		if (hasMethod && method == MuiNativeRedrawMessage.DrawMethodId &&
			!stateHandled && canProjectNativeMethod && !nestedSuperDispatch &&
			root.LoaderState != 0 && MuiNativeClassOwnerCodec
				.TryGetPublicObjectStateAddress(ref memory,
					APTR.FromPointer(root.LoaderState), out var publicObjects))
			MuiNativeAreaDoubleBufferCore.TryBeginForObject(
				ref areaDoubleBufferPlatform, publicObjects, value.OwnerRoot,
				obj, message, out areaDoubleBufferLease);
		if (!stateHandled)
			result = InvokeWithDispatchContext(ref memory, value.OwnerRoot,
				value.Dispatcher, cls, obj, message, callbackBase, method);
		if (areaDoubleBufferLease.Active != 0)
			MuiNativeAreaDoubleBufferCore.End(ref areaDoubleBufferPlatform,
				ref areaDoubleBufferLease, true);
		if (hasMethod && !stateHandled && canProjectNativeMethod &&
			!nestedSuperDispatch)
			TryProjectNativeMethod(ref memory, value.OwnerRoot, obj, message,
				method, receivedSignals, ref result);
		// The callback may have returned a native object or completed its
		// destructor before the aggregate counters are balanced.  Keep a
		// declaration-ordered per-object identity in guest memory; failure poisons
		// retirement rather than guessing whether an object record exists.
		var objectBindingHealthy = true;
		if (hasMethod && method == BOOPSI.OM_NEW && result != 0)
			objectBindingHealthy = TryLinkNativeObject(ref memory, binding, cls,
				APTR.FromPointer(result));
		else if (hasMethod && method == BOOPSI.OM_DISPOSE)
			objectBindingHealthy = TryUnlinkNativeObject(ref memory, binding, cls,
				obj);
		Exec.Forbid();
		// Nested callbacks may have changed the counters and linked other classes.
		// Read fresh records before removing this invocation's ownership.
		if (MuiNativeClassBindingCodec.TryRead(ref memory, binding, out var after) &&
			MuiMasterPrivateRootCodec.TryRead(ref memory, after.OwnerRoot, out var afterRoot))
		{
			after.ActiveCalls--;
			// Every custom-class level sees the same successful native object as
			// OM_NEW travels down the superclass chain.  Balance that pin only
			// after the callback returns from OM_DISPOSE.  Saturation is a safe
			// retirement poison: it prevents a wraparound from making a live
			// native object appear disposable.
			if (!objectBindingHealthy)
			{
				after.ActiveObjects = uint.MaxValue;
				after.LifecycleFlags |= MuiNativeClassBinding.NativeObjectLinkPoisoned;
			}
			else if (hasMethod && method == BOOPSI.OM_NEW && result != 0)
			{
				if (after.ActiveObjects != uint.MaxValue) after.ActiveObjects++;
			}
			else if (hasMethod && method == BOOPSI.OM_DISPOSE &&
				after.ActiveObjects != 0 && after.ActiveObjects != uint.MaxValue)
				after.ActiveObjects--;
			if (objectBindingHealthy && hasMethod && after.SidecarClass.IsNotNull &&
				(hasMethod && (method == BOOPSI.OM_NEW && result != 0 ||
					 method == BOOPSI.OM_DISPOSE)))
			{
				if (!MuiNativeClassDispatcher.TryAdjustSidecarObjectCount(ref memory,
					after, method, result))
				{
					after.ActiveObjects = uint.MaxValue;
					after.LifecycleFlags |= MuiNativeClassBinding.NativeObjectLinkPoisoned;
				}
			}
			afterRoot.ActiveCallbackDepth--;
			MuiNativeClassBindingCodec.Write(ref memory, binding, after);
			MuiMasterPrivateRootCodec.Write(ref memory, after.OwnerRoot, afterRoot);
		}
		Exec.Permit();
		return result;
	}

	private static uint InvokeWithDispatchContext(ref MuiNativeClassMemory memory,
		APTR ownerRoot, APTR entry, APTR cls, APTR obj, APTR message,
		APTR callbackBase, uint method)
	{
		if (!MuiMasterPrivateRootCodec.TryRead(ref memory, ownerRoot,
			out var root) || root.LoaderState == 0)
			return Invoke(entry, cls, obj, message, callbackBase);
		var frameAddress = Exec.AllocMem(MuiNativeClassDispatchFrameRecord.Size,
			(Exec.MemoryFlags)MuiHeadlessLayout.AllocationFlags);
		if (frameAddress.IsNull)
			return Invoke(entry, cls, obj, message, callbackBase);

		Exec.Forbid();
		var task = Exec.FindTask(CString.FromPointer(0));
		var entered = MuiNativeClassDispatchContextCore.TryPush(ref memory,
			APTR.FromPointer(root.LoaderState), frameAddress, task, cls, obj,
			message, method);
		Exec.Permit();
		if (!entered)
		{
			Exec.FreeMem(frameAddress, MuiNativeClassDispatchFrameRecord.Size);
			return Invoke(entry, cls, obj, message, callbackBase);
		}

		var result = Invoke(entry, cls, obj, message, callbackBase);
		Exec.Forbid();
		var left = MuiNativeClassDispatchContextCore.TryPop(ref memory,
			APTR.FromPointer(root.LoaderState), frameAddress);
		Exec.Permit();
		// Never free a frame whose removal could not be proven. The owner is
		// poisoned by TryPop and remains non-quiescent instead of authorizing a
		// stale callback or freeing storage still reachable from the guest list.
		if (left) Exec.FreeMem(frameAddress,
			MuiNativeClassDispatchFrameRecord.Size);
		return result;
	}

	private static bool TryDispatchNativeState(ref MuiNativeClassMemory memory,
		APTR ownerRoot, APTR obj, APTR message, uint method, out uint result)
	{
		result = 0;
		var stateMethod = method ==
			MuiApplicationDispatcher.ApplicationReturnIdMethod || method ==
			MuiApplicationPushMethodMessage.Id || method ==
			MuiApplicationUnpushMethodMessage.Id || method ==
			MuiApplicationDispatcher.ApplicationInputMethod || method ==
			MuiApplicationDispatcher.ApplicationNewInputMethod || method ==
			MuiApplicationLoopMessage.ExecuteMethodId || method ==
			MuiApplicationLoopMessage.RunMethodId || method ==
			MuiApplicationDispatcher.WindowAddEventHandlerMethod || method ==
			MuiApplicationDispatcher.WindowRemoveEventHandlerMethod || method ==
			MuiApplicationDispatcher.AddInputHandlerMethod || method ==
			MuiApplicationDispatcher.RemoveInputHandlerMethod;
		if (!stateMethod && method != MuiNotifyCore.NotifyMethod &&
			method != MuiNotifyCore.KillNotifyMethod &&
			method != MuiNotifyCore.KillNotifyObjectMethod) return false;
		if (!MuiMasterPrivateRootCodec.TryRead(ref memory, ownerRoot,
			out var root) || root.LoaderState == 0) return false;
		var owner = APTR.FromPointer(root.LoaderState);
		if (!MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref memory,
			owner, out var publicObjects)) return false;
		var platform = default(MuiNativeClassPlatform);
		platform.Context = owner;
		if (method == MuiApplicationLoopMessage.ExecuteMethodId || method ==
			MuiApplicationLoopMessage.RunMethodId)
			return MuiNativeApplicationLoopCore.TryDispatch(ref platform,
				publicObjects, ownerRoot, obj, message, method, out result);
		if ((method == MuiApplicationDispatcher.ApplicationInputMethod ||
			method == MuiApplicationDispatcher.ApplicationNewInputMethod) &&
			MuiApplicationInputMessageCodec.TryRead(ref memory, message,
				out var inputMessage) && inputMessage.MethodId == method &&
			MuiApplicationWindowSignalStorageCodec.TryRead(ref memory,
				inputMessage.SignalStorage, out _))
			MuiNativeApplicationPushMethodDispatch.DispatchNext(ref platform,
				publicObjects, ownerRoot, obj);
		if (stateMethod)
			return MuiNativeApplicationInputCore.TryDispatch(ref platform,
				publicObjects, ownerRoot, obj, message, method, out result);
		if (!MuiNativePublicObjectCore.TryFindBinding(ref platform,
			publicObjects, ownerRoot, obj, out _)) return false;
		return MuiNativeObjectStateCore.TryHandleNotifyMethod(ref platform,
			publicObjects, ownerRoot, obj, message, method, out result);
	}

	private static bool TryProjectNativeMethod(ref MuiNativeClassMemory memory,
		APTR ownerRoot, APTR obj, APTR message, uint method,
		uint receivedSignals, ref uint result)
	{
		if (method != BOOPSI.OM_GET && method != BOOPSI.OM_SET &&
			method != MuiLayoutPacketCore.Setup &&
			method != MuiLayoutPacketCore.Cleanup &&
			method != MuiApplicationDispatcher.ApplicationInputMethod &&
			method != MuiApplicationDispatcher.ApplicationNewInputMethod &&
			method != MuiApplicationDispatcher.ApplicationInputBufferedMethod &&
			method != MuiApplicationDispatcher.ApplicationCheckRefreshMethod &&
			method != MuiNotifyCore.SetMethod &&
			method != MuiNotifyCore.NoNotifySetMethod &&
			method != MuiNotifyCore.MultiSetMethod &&
			method != MuiNotifySetAsStringCore.Method &&
			method != MuiNotifyWriteCore.WriteLongMethod &&
			method != MuiNotifyWriteCore.WriteStringMethod &&
			method != MuiCallHookCore.Method &&
			method != MuiNotifyCore.FindObjectMethod &&
			method != MuiNotifyUserDataCore.FindUData &&
			method != MuiNotifyUserDataCore.GetUData &&
			method != MuiNotifyUserDataCore.SetUData &&
			method != MuiNotifyUserDataCore.SetUDataOnce) return false;
		if (!MuiMasterPrivateRootCodec.TryRead(ref memory, ownerRoot,
			out var root) || root.LoaderState == 0) return false;
		var owner = APTR.FromPointer(root.LoaderState);
		var platform = default(MuiNativeClassPlatform);
		platform.Context = owner;
		// These Notify helpers are stateless with respect to the MUI public
		// object registry. They still require the owner context (for the named
		// utility base in CallHook), but direct BOOPSI objects may use them even
		// when no MUI sidecar has been adopted.
		if (method == MuiNotifyWriteCore.WriteLongMethod)
		{
			if (!MuiNotifyWriteCore.TryReadWriteLong(ref platform, message,
				out var packet)) return false;
			result = MuiNotifyWriteCore.WriteLong(ref platform, packet.Value,
				packet.Memory) ? 1u : 0u;
			return true;
		}
		if (method == MuiNotifyWriteCore.WriteStringMethod)
		{
			if (!MuiNotifyWriteCore.TryReadWriteString(ref platform, message,
				out var packet)) return false;
			result = MuiNotifyWriteCore.WriteString(ref platform, packet.String,
				packet.Memory) ? 1u : 0u;
			return true;
		}
		if (method == MuiCallHookCore.Method)
		{
			result = MuiCallHookCore.DispatchRecord(ref platform, obj, message);
			return true;
		}
		if (!MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref memory,
			owner, out var publicObjects)) return false;
		if (!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
			ownerRoot, obj, out var liveBinding)) return false;
		if (method == MuiLayoutPacketCore.Setup ||
			method == MuiLayoutPacketCore.Cleanup)
			return MuiNativeAreaLifecycleCore.TryProjectAfterClassCallback(
				ref platform, liveBinding.Sidecar, message, method, ref result);
		if (method == MuiApplicationDispatcher.ApplicationInputMethod ||
			method == MuiApplicationDispatcher.ApplicationNewInputMethod ||
			method == MuiApplicationDispatcher.ApplicationInputBufferedMethod)
			return MuiNativeApplicationInputCore.TryProjectAfterClassCallback(
				ref platform, publicObjects, ownerRoot, obj, liveBinding.Sidecar,
				message, method, receivedSignals, ref result);
		if (method == MuiApplicationDispatcher.ApplicationCheckRefreshMethod)
		{
			if (!MuiApplicationCheckRefreshMessageCodec.TryRead(ref platform,
				message, out var packet) || packet.MethodId != method) return false;
			return MuiNativeApplicationRefreshCore.TryCheck(ref platform,
				publicObjects, ownerRoot, obj, message, ref result);
		}
		if (method == MuiNotifySetAsStringCore.Method)
		{
			if (!MuiNotifySetAsStringCore.TryRead(ref platform, message,
				out var packet) || !MuiSetAsStringMessageCodec.TryGetParameters(
					ref platform, message, out var parameters)) return false;
			result = MuiNativeSetAsStringCore.Apply(ref platform,
				liveBinding.Sidecar, packet, parameters) ? 1u : 0u;
			return true;
		}
		if (method == BOOPSI.OM_GET)
		{
			if (!MuiExternalBoopsiOpGetMessageStructCodec.TryRead(ref platform,
				message, out var packet) || packet.MethodId != BOOPSI.OM_GET)
				return false;
			if (packet.Attribute == MuiWindowPublicCore.Window)
			{
				if (result != 0 && !packet.Storage.IsNull &&
					platform.IsMapped(packet.Storage,
					MuiGuestUlongStorage.Size) &&
					MuiGuestUlongStorageCodec.TryReadValue(ref platform,
						packet.Storage, out var nativeWindow))
				{
					var window = APTR.FromPointer(nativeWindow);
					MuiNativePublicObjectCore.SetNativeWindow(ref platform,
						publicObjects, ownerRoot, obj, window);
					if (window.IsNotNull)
						MuiNativePublicObjectCore.ApplyRequestedIDCMP(ref platform,
							publicObjects, ownerRoot, obj, window);
				}
				// MUIA_Window_Window is produced by the class callback. Preserve
				// that result; this branch only applies any deferred IDCMP mask.
				return true;
			}
			if (!MuiNativePublicObjectCore.GetAttribute(ref platform,
				publicObjects, ownerRoot, obj, packet.Attribute, out var value))
				return false;
			var storage = packet.Storage;
			if (storage.IsNull || !platform.IsMapped(storage,
				MuiGuestUlongStorage.Size))
			{
				result = 0;
				return true;
			}
			result = MuiGuestUlongStorageCodec.WriteValue(ref platform, storage,
				value) ? 1u : 0u;
			return true;
		}
		if (method == BOOPSI.OM_SET)
		{
			if (!MuiHeadlessOmSetMessageCodec.TryRead(ref platform, message,
				out var packet) || packet.MethodId != MuiHeadlessOmSetMessageCodec.Method)
				return false;
			if (!MuiNativePublicObjectCore.ApplyTags(ref platform, publicObjects,
				ownerRoot, obj, packet.Attributes, true)) return false;
			if (MuiNativeObjectStateCore.TryGetAttribute(ref memory,
				liveBinding.Sidecar, MuiWindowPublicCore.Open,
				out var windowOpen))
			{
				if (windowOpen != 0)
					MuiNativePublicObjectCore.ApplyRequestedIDCMPToCurrentWindow(
						ref platform, publicObjects, ownerRoot, obj);
				else
					MuiNativePublicObjectCore.SetNativeWindow(ref platform,
						publicObjects, ownerRoot, obj, APTR.Null);
			}
			if (result == 0) result = 1;
			return true;
		}
		if (method == MuiNotifyCore.MultiSetMethod)
		{
			if (!MuiNotifyCore.TryReadMultiSet(ref platform, message, method,
				out var packet)) return false;
			var vector = MuiNotifyCore.MultiSetVector(ref platform, message);
			if (!MuiNativePublicObjectCore.ApplyMultiSet(ref platform,
				publicObjects, ownerRoot, obj, packet, vector)) return false;
			if (result == 0) result = 1;
			return true;
		}
		if (method == MuiNotifyCore.FindObjectMethod)
		{
			if (!MuiNotifyCore.TryReadFindObject(ref platform, message, method,
				out var packet)) return false;
			result = MuiNativeUserDataCore.FindObject(ref platform,
				publicObjects, ownerRoot, obj,
				APTR.FromPointer(packet.FindObject)) ? 1u : 0u;
			return true;
		}
		if (method == MuiNotifyUserDataCore.FindUData)
		{
			if (!MuiNotifyUserDataCore.TryReadFind(ref platform, message, method,
				out var packet)) return false;
			result = MuiNativeUserDataCore.Find(ref platform, publicObjects,
				ownerRoot, obj, packet.UserData).Raw;
			return true;
		}
		if (method == MuiNotifyUserDataCore.GetUData)
		{
			if (!MuiNotifyUserDataCore.TryReadGet(ref platform, message, method,
				out var packet)) return false;
			result = MuiNativeUserDataCore.Get(ref platform, publicObjects,
				ownerRoot, obj, packet.UserData, packet.Attribute,
				APTR.FromPointer(packet.Storage)) ? 1u : 0u;
			return true;
		}
		if (method == MuiNotifyUserDataCore.SetUData ||
			method == MuiNotifyUserDataCore.SetUDataOnce)
		{
			if (!MuiNotifyUserDataCore.TryReadSet(ref platform, message, method,
				out var packet)) return false;
			result = MuiNativeUserDataCore.Set(ref platform, publicObjects,
				ownerRoot, obj, packet.UserData, packet.Attribute, packet.Value,
				method == MuiNotifyUserDataCore.SetUDataOnce) ? 1u : 0u;
			return true;
		}
		if (!MuiNotifyCore.TryReadSet(ref platform, message, method,
			out var setPacket)) return false;
		var notify = method == MuiNotifyCore.SetMethod;
		if (!MuiNativePublicObjectCore.SetAttribute(ref platform, publicObjects,
			ownerRoot, obj, setPacket.Attribute, setPacket.Value, notify))
			return false;
		if (result == 0) result = 1;
		return true;
	}

	private static bool TryLinkNativeObject(ref MuiNativeClassMemory memory,
		APTR bindingAddress, APTR cls, APTR obj)
	{
		if (bindingAddress.IsNull || cls.IsNull || obj.IsNull ||
			!MuiNativeClassBindingCodec.TryRead(ref memory, bindingAddress,
				out var binding) || binding.Class != cls || binding.OwnerRoot.IsNull)
			return false;
		var allocation = Exec.AllocMem(MuiNativeClassObjectBinding.Size,
			(Exec.MemoryFlags)MuiHeadlessLayout.AllocationFlags);
		if (allocation.IsNull) return false;
		var sidecarAllocation = Exec.AllocMem(MuiNativeMuiObjectRecord.Size,
			(Exec.MemoryFlags)MuiHeadlessLayout.AllocationFlags);
		if (sidecarAllocation.IsNull)
		{
			Exec.FreeMem(allocation, MuiNativeClassObjectBinding.Size);
			return false;
		}
		// The allocation itself is callback-capable in the general Exec contract;
		// re-read the complete binding and list after it before publication.
		if (!MuiNativeClassBindingCodec.TryRead(ref memory, bindingAddress,
			out binding) || binding.Class != cls || binding.OwnerRoot.IsNull ||
			(binding.LifecycleFlags & MuiNativeClassBinding.NativeClassFreed) != 0 ||
			!IsNativeObjectListValid(ref memory, binding.ObjectHead,
				binding.OwnerRoot, cls, obj, out var duplicate))
		{
			Exec.FreeMem(sidecarAllocation, MuiNativeMuiObjectRecord.Size);
			Exec.FreeMem(allocation, MuiNativeClassObjectBinding.Size);
			return false;
		}
		if (duplicate)
		{
			Exec.FreeMem(sidecarAllocation, MuiNativeMuiObjectRecord.Size);
			Exec.FreeMem(allocation, MuiNativeClassObjectBinding.Size);
			return false;
		}
		var value = default(MuiNativeClassObjectBinding);
		value.Signature = MuiNativeClassObjectBinding.Magic;
		value.Next = binding.ObjectHead;
		value.Object = obj;
		value.Class = cls;
		value.OwnerRoot = binding.OwnerRoot;
		value.Sidecar = sidecarAllocation;
		var sidecar = default(MuiNativeMuiObjectRecord);
		sidecar.Signature = MuiNativeMuiObjectRecord.Magic;
		sidecar.Revision = MuiNativeMuiObjectRecord.Version;
		sidecar.Object = obj;
		sidecar.Class = cls;
		sidecar.OwnerRoot = binding.OwnerRoot;
		sidecar.Flags = MuiNativeMuiObjectRecord.ObjectInitialized;
		if (!MuiNativeMuiObjectCodec.Write(ref memory, sidecarAllocation, sidecar))
		{
			Exec.FreeMem(sidecarAllocation, MuiNativeMuiObjectRecord.Size);
			Exec.FreeMem(allocation, MuiNativeClassObjectBinding.Size);
			return false;
		}
		if (!MuiNativeClassObjectBindingCodec.Write(ref memory, allocation, value))
		{
			Exec.FreeMem(sidecarAllocation, MuiNativeMuiObjectRecord.Size);
			Exec.FreeMem(allocation, MuiNativeClassObjectBinding.Size);
			return false;
		}
		binding.ObjectHead = allocation;
		if (!MuiNativeClassBindingCodec.Write(ref memory, bindingAddress, binding))
		{
			Exec.FreeMem(allocation, MuiNativeClassObjectBinding.Size);
			return false;
		}
		return true;
	}

	private static bool TryUnlinkNativeObject(ref MuiNativeClassMemory memory,
		APTR bindingAddress, APTR cls, APTR obj)
	{
		if (bindingAddress.IsNull || cls.IsNull || obj.IsNull ||
			!MuiNativeClassBindingCodec.TryRead(ref memory, bindingAddress,
				out var binding) || binding.Class != cls || binding.OwnerRoot.IsNull)
			return false;
		// Validate the complete list before changing its head or predecessor.
		// This rejects cycles and a missing object deterministically; otherwise a
		// successful scalar decrement could make a corrupt class look disposable.
		if (!IsNativeObjectListValid(ref memory, binding.ObjectHead,
			binding.OwnerRoot, cls, obj, out var found) || !found) return false;
		var current = binding.ObjectHead;
		var previous = APTR.Null;
		for (uint visited = 0; current.IsNotNull &&
			visited < MuiHeadlessLayout.MaximumTraversal; visited++)
		{
			if (!MuiNativeClassObjectBindingCodec.TryRead(ref memory, current,
				out var value) || value.Signature != MuiNativeClassObjectBinding.Magic ||
				value.OwnerRoot != binding.OwnerRoot || value.Class != cls)
				return false;
			if (value.Object == obj)
			{
				if (value.ActiveCalls != 0) return false;
				var sidecar = default(MuiNativeMuiObjectRecord);
				var sidecarIsPubliclyBorrowed =
					(value.LifecycleFlags &
						MuiNativeClassObjectBinding.PublicSidecarBorrowed) != 0;
				if (value.Sidecar.IsNotNull &&
					(!MuiNativeMuiObjectCodec.TryRead(ref memory, value.Sidecar,
						out sidecar) || sidecar.Object != obj ||
						sidecar.Class != cls || sidecar.OwnerRoot != binding.OwnerRoot))
					return false;
				if (value.Sidecar.IsNotNull && !sidecarIsPubliclyBorrowed &&
					!MuiNativeObjectStateCore.ReleaseLists(ref memory,
						value.Sidecar)) return false;
				if (value.Sidecar.IsNotNull && !sidecarIsPubliclyBorrowed)
				{
					sidecar.Flags &= ~MuiNativeMuiObjectRecord.ObjectDisposing;
					sidecar.Flags |= MuiNativeMuiObjectRecord.ObjectNativeDisposed;
					sidecar.LifecycleState = MuiNativeMuiObjectRecord.StateNativeDisposed;
					if (!MuiNativeMuiObjectCodec.Write(ref memory, value.Sidecar,
						sidecar)) return false;
				}
				if (previous.IsNull)
				{
					binding.ObjectHead = value.Next;
					if (!MuiNativeClassBindingCodec.Write(ref memory, bindingAddress,
						binding)) return false;
				}
				else
				{
					if (!MuiNativeClassObjectBindingCodec.TryRead(ref memory,
						previous, out var previousValue) ||
						previousValue.Next != current) return false;
					previousValue.Next = value.Next;
					if (!MuiNativeClassObjectBindingCodec.Write(ref memory, previous,
						previousValue)) return false;
				}
				if (value.Sidecar.IsNotNull && !sidecarIsPubliclyBorrowed)
					Exec.FreeMem(value.Sidecar, MuiNativeMuiObjectRecord.Size);
				Exec.FreeMem(current, MuiNativeClassObjectBinding.Size);
				return true;
			}
			previous = current;
			current = value.Next;
		}
		return false;
	}

	private static bool IsNativeObjectListValid<TMemory>(ref TMemory memory,
		APTR head, APTR ownerRoot, APTR cls, APTR obj, out bool duplicate)
		where TMemory : struct, IMuiGuestMemory
	{
		duplicate = false;
		var current = head;
		for (uint visited = 0; current.IsNotNull &&
			visited < MuiHeadlessLayout.MaximumTraversal; visited++)
		{
			if (!MuiNativeClassObjectBindingCodec.TryRead(ref memory, current,
				out var value) || value.Signature != MuiNativeClassObjectBinding.Magic ||
				value.OwnerRoot != ownerRoot || value.Class != cls)
				return false;
			if (value.Object == obj)
			{
				if (duplicate) return false;
				duplicate = true;
			}
			current = value.Next;
		}
		return current.IsNull;
	}

	internal static bool TryAdjustSidecarObjectCount<TMemory>(ref TMemory memory,
		MuiNativeClassBinding binding, uint method, uint result)
		where TMemory : struct, IMuiGuestMemory
	{
		if (binding.SidecarClass.IsNull) return true;
		if (!MuiMasterPrivateRootCodec.TryRead(ref memory, binding.OwnerRoot,
			out var root) || root.ClassRegistry == 0 ||
			!IsRegisteredClass(ref memory,
				APTR.FromPointer(root.ClassRegistry), binding.SidecarClass) ||
			!MuiHeadlessClassCodec.TryRead(ref memory,
				binding.SidecarClass, out var sidecar) ||
			sidecar.Boopsi != binding.Class) return false;
		if (method == BOOPSI.OM_NEW && result != 0)
		{
			if (sidecar.ObjectCount != uint.MaxValue) sidecar.ObjectCount++;
		}
		else if (method == BOOPSI.OM_DISPOSE &&
			sidecar.ObjectCount != 0 && sidecar.ObjectCount != uint.MaxValue)
			sidecar.ObjectCount--;
		return MuiHeadlessClassCodec.Write(ref memory, binding.SidecarClass,
			sidecar);
	}

	private static bool IsRegisteredClass<TMemory>(ref TMemory memory,
		APTR state, APTR target) where TMemory : struct, IMuiGuestMemory
	{
		if (state.IsNull || target.IsNull ||
			!MuiHeadlessStateCodec.TryRead(ref memory, state,
				out var registry)) return false;
		var current = registry.Classes;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (current == target) return true;
			if (!MuiHeadlessClassCodec.TryRead(ref memory, current,
				out var value)) return false;
			current = value.Next;
		}
		return false;
	}

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint Invoke([M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A0)] APTR cls, [M68kRegister(M68kRegister.A2)] APTR obj,
		[M68kRegister(M68kRegister.A1)] APTR message, [M68kRegister(M68kRegister.A6)] APTR library);
}
