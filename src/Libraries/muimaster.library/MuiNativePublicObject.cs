/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// The native public-object list is separate from the portable headless
// object's list. The owner embeds this small named state record so a native
// binding can never be mistaken for a MuiHeadlessObjectRecord by sidecar code.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativePublicObjectRegistryRecord
{
	internal const uint Magic = 0x4D554F52; // "MUOR"
	internal const uint Version = 1;
	internal const uint Size = 16;
	internal uint Signature;
	internal uint Revision;
	internal APTR Head;
	internal uint Mutation;
}

internal static class MuiNativePublicObjectRegistryCodec
{
	internal static bool TryRead<T>(ref T memory, APTR address,
		out MuiNativePublicObjectRegistryRecord value)
		where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativePublicObjectRegistryRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Signature) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Revision) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var head) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Mutation) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Head = APTR.FromPointer(head);
		return value.Signature == MuiNativePublicObjectRegistryRecord.Magic &&
			value.Revision == MuiNativePublicObjectRegistryRecord.Version;
	}

	internal static bool Write<T>(ref T memory, APTR address,
		MuiNativePublicObjectRegistryRecord value)
		where T : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativePublicObjectRegistryRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Signature) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Revision) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Head.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Mutation) &&
		MuiGuestStructCursor.IsComplete(cursor);
}

// Native public-object ownership is kept in the library-owned companion
// registry. This is a separate named record shape from the portable
// headless object record: a native object is owned by Intuition, while this
// record only retains the class-service lease needed by MUI. No object layout
// is inferred from caller memory.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativePublicObjectBinding
{
	internal const uint Magic = 0x4D554F42; // "MUOB"
	internal const uint Size = 44;
	internal const uint StateLive = 0;
	internal const uint StateNativeDisposed = 1;
	internal const uint StateLeaseReleased = 2;
	internal uint Signature;
	internal APTR Next;
	internal APTR Object;
	internal APTR Class;
	internal APTR Lease;
	internal APTR CustomClass;
	internal APTR OwnerRoot;
	internal APTR Parent;
	internal APTR Sidecar;
	internal uint ActiveCalls;
	internal uint DisposeState;
}

internal static class MuiNativePublicObjectBindingCodec
{
	internal static bool TryRead<T>(ref T memory, APTR address,
		out MuiNativePublicObjectBinding value) where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativePublicObjectBinding.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Signature) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var next) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var obj) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var cls) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var lease) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var customClass) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var root) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var parent) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var sidecar) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.ActiveCalls) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.DisposeState) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Next = APTR.FromPointer(next);
		value.Object = APTR.FromPointer(obj);
		value.Class = APTR.FromPointer(cls);
		value.Lease = APTR.FromPointer(lease);
		value.CustomClass = APTR.FromPointer(customClass);
		value.OwnerRoot = APTR.FromPointer(root);
		value.Parent = APTR.FromPointer(parent);
		value.Sidecar = APTR.FromPointer(sidecar);
		return true;
	}

	internal static bool Write<T>(ref T memory, APTR address,
		MuiNativePublicObjectBinding value) where T : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativePublicObjectBinding.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Signature) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Next.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Object.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Class.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Lease.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.CustomClass.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.OwnerRoot.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Parent.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Sidecar.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.ActiveCalls) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.DisposeState) &&
		MuiGuestStructCursor.IsComplete(cursor);
}

internal static class MuiNativePublicObjectCore
{
	// Admission for native public helpers is separate from disposal. A binding
	// is usable only while its named ownership record is live; callers never
	// infer validity from the object pointer or inspect a native object offset.
	internal static bool TryFindLive<TPlatform>(ref TPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj,
		out MuiNativePublicObjectBinding binding)
		where TPlatform : struct, IMuiGuestMemory
	{
		binding = default;
		if (obj.IsNull || ownerRoot.IsNull || publicObjects.IsNull ||
			!Find(ref platform, publicObjects, ownerRoot, obj, out _, out _,
				out binding)) return false;
		if (binding.Signature != MuiNativePublicObjectBinding.Magic ||
			binding.Object != obj ||
			binding.DisposeState != MuiNativePublicObjectBinding.StateLive ||
			binding.Sidecar.IsNull) return false;
		return MuiNativeMuiObjectCodec.TryRead(ref platform, binding.Sidecar,
			out var sidecar) && sidecar.Object == obj &&
			sidecar.Class == binding.Class && sidecar.OwnerRoot == ownerRoot &&
			sidecar.LifecycleState == MuiNativeMuiObjectRecord.StateLive &&
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) != 0 &&
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectDisposing) == 0;
	}

	// Dispatcher admission needs to distinguish a direct BOOPSI callback object
	// from a public MUI object whose sidecar is malformed or retiring. Both use
	// named records; only the latter should consume MUI notification methods.
	internal static bool TryFindBinding<TPlatform>(ref TPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj,
		out MuiNativePublicObjectBinding binding)
		where TPlatform : struct, IMuiGuestMemory =>
		Find(ref platform, publicObjects, ownerRoot, obj, out _, out _, out binding);

	internal static bool GetAttribute(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj, uint attribute,
		out uint value)
	{
		value = 0;
		if (!TryFindLive(ref platform, publicObjects, ownerRoot, obj,
			out var binding)) return false;
		if (MuiNativeAreaDoubleBufferCore.TryGetTemporaryGeometry(ref platform,
			binding.Sidecar, ownerRoot, obj, attribute, out value)) return true;
		if (TryGetLiveWindowGeometry(ref platform, binding.Sidecar,
			attribute, out value)) return true;
		return MuiNativeObjectStateCore.TryGetAttribute(ref platform,
			binding.Sidecar, attribute, out value);
	}

	// Window geometry is mutable in Intuition after IDCMP_CHANGEWINDOW. Project
	// the live named Window record for [I.G] attributes rather than returning
	// only the initializer values retained in the MUI sidecar.
	private static bool TryGetLiveWindowGeometry(
		ref MuiNativeClassPlatform platform, APTR sidecarAddress,
		uint attribute, out uint value)
	{
		value = 0;
		if (attribute != MuiWindowPublicCore.LeftEdge &&
			attribute != MuiWindowPublicCore.TopEdge &&
			attribute != MuiWindowPublicCore.Width &&
			attribute != MuiWindowPublicCore.Height) return false;
		if (!MuiNativeMuiObjectCodec.TryRead(ref platform, sidecarAddress,
			out var sidecar) || sidecar.NativeWindow.IsNull ||
			!platform.IsMapped(sidecar.NativeWindow, Window.Size)) return false;
		var window = IntuitionScreenWindowGuestCodec.ReadWindow(ref platform,
			sidecar.NativeWindow);
		if (attribute == MuiWindowPublicCore.LeftEdge)
			value = unchecked((uint)(int)window.LeftEdge);
		else if (attribute == MuiWindowPublicCore.TopEdge)
			value = unchecked((uint)(int)window.TopEdge);
		else if (attribute == MuiWindowPublicCore.Width)
			value = unchecked((uint)(int)window.Width);
		else value = unchecked((uint)(int)window.Height);
		return true;
	}

	// Cache the native Intuition Window returned by MUIA_Window_Window in the
	// object's named sidecar. Application NewInput uses only these observed live
	// window handles when collecting the application's wait-signal mask.
	internal static bool SetNativeWindow(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj, APTR nativeWindow)
	{
		if (!TryFindLive(ref platform, publicObjects, ownerRoot, obj,
			out var binding)) return false;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, binding.Sidecar,
			out var sidecar)) return false;
		sidecar.NativeWindow = nativeWindow;
		return MuiNativeMuiObjectCodec.Write(ref memory, binding.Sidecar,
			sidecar) && MuiNativeApplicationSleep.TryApplyOpenWindowSleep(
			ref platform, binding.Sidecar, nativeWindow);
	}

	// MUI_RequestIDCMP/MUI_RejectIDCMP operate on the Intuition Window exposed
	// by MUIA_Window_Window. Read the SDK-owned named Window record and modify
	// its IDCMP mask through Intuition; the only guest scratch storage is the
	// existing named ULONG result record used by BOOPSI GetAttr.
	internal static void ChangeIDCMP(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj, uint flags, bool request)
	{
		if (flags == 0 || !TryFindLive(ref platform, publicObjects, ownerRoot,
			obj, out var binding)) return;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeObjectStateCore.TryUpdateIDCMPRequest(ref memory,
			binding.Sidecar, flags, request, out var requested, out var rejected))
			return;
		if (platform.IntuitionBase.IsNull) return;
		var window = ReadNativeWindow(ref platform, publicObjects, ownerRoot,
			obj);
		ApplyIDCMPMask(ref platform, window, requested, rejected);
	}

	internal static void ApplyRequestedIDCMP(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj, APTR window)
	{
		if (window.IsNull || platform.IntuitionBase.IsNull ||
			!TryFindLive(ref platform, publicObjects, ownerRoot, obj,
				out var binding)) return;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, binding.Sidecar,
			out var sidecar)) return;
		ApplyIDCMPMask(ref platform, window, sidecar.RequestedIDCMP,
			sidecar.RejectedIDCMP);
	}

	internal static void ApplyRequestedIDCMPToCurrentWindow(
		ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR obj)
	{
		if (platform.IntuitionBase.IsNull ||
			!TryFindLive(ref platform, publicObjects, ownerRoot, obj, out _))
			return;
		var window = ReadNativeWindow(ref platform, publicObjects, ownerRoot,
			obj);
		ApplyRequestedIDCMP(ref platform, publicObjects, ownerRoot, obj, window);
	}

	internal static APTR ReadNativeWindow(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj)
	{
		var storage = platform.Allocate(MuiGuestUlongStorage.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (storage.IsNull) return APTR.Null;
		var window = APTR.Null;
		if (MuiGuestUlongStorageCodec.WriteValue(ref platform, storage, 0) &&
			MuiNativeIntuitionCalls.GetAttr(platform.IntuitionBase,
				MuiWindowPublicCore.Window, obj, storage) != 0 &&
			MuiGuestUlongStorageCodec.TryReadValue(ref platform, storage,
				out var windowRaw))
		{
			window = APTR.FromPointer(windowRaw);
			SetNativeWindow(ref platform, publicObjects, ownerRoot, obj, window);
		}
		platform.Free(storage, MuiGuestUlongStorage.Size);
		return window;
	}

	private static void ApplyIDCMPMask(ref MuiNativeClassPlatform platform,
		APTR windowAddress, uint requested, uint rejected)
	{
		if (windowAddress.IsNull || platform.IntuitionBase.IsNull ||
			!platform.IsMapped(windowAddress, Amiga.Window.Size)) return;
		var window = IntuitionScreenWindowGuestCodec.ReadWindow(ref platform,
			windowAddress);
		if (ProjectIDCMPMask(ref window, requested, rejected))
			MuiNativeIntuitionCalls.ModifyIDCMP(platform.IntuitionBase,
				windowAddress, (uint)window.IDCMPFlags);
	}

	// Project the caller's last-operation-wins request/rejection state onto the
	// typed Intuition Window record. The MUI event pump also requires geometry,
	// pointer-motion, and menu-selection/help messages for its own event and
	// application attributes, so those internal classes remain enabled even if a
	// caller rejects them. Keeping the transformation on the named Window struct
	// makes the native boundary independent of private object offsets.
	internal static bool ProjectIDCMPMask(ref Window window, uint requested,
		uint rejected)
	{
		var currentMask = (uint)window.IDCMPFlags;
		var required = (uint)IDCMPFlags.ChangeWindow |
			(uint)IDCMPFlags.MouseMove | (uint)IDCMPFlags.MenuPick |
			(uint)IDCMPFlags.MenuHelp;
		var updatedMask = ((currentMask | requested) & ~rejected) | required;
		if (updatedMask == currentMask) return false;
		window.IDCMPFlags = (IDCMPFlags)updatedMask;
		return true;
	}

	internal static bool SetAttribute(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj, uint attribute,
		uint value)
		=> SetAttribute(ref platform, publicObjects, ownerRoot, obj, attribute,
			value, true);

	internal static bool SetAttribute(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj, uint attribute,
		uint value, bool notify)
	{
		if (attribute == MuiApplicationWindowCore.ApplicationSleep)
			return MuiNativeApplicationSleep.TrySetApplicationSleepValue(
				ref platform, publicObjects, ownerRoot, obj, value, notify);
		if (attribute == MuiWindowPublicCore.Sleep)
			return MuiNativeApplicationSleep.TrySetWindowSleepValue(ref platform,
				publicObjects, ownerRoot, obj, value, notify);
		if (!TryFindLive(ref platform, publicObjects, ownerRoot, obj,
			out var binding)) return false;
		return MuiNativeObjectStateCore.SetAttribute(ref platform,
			binding.Sidecar, attribute, value, notify);
	}

	internal static bool ApplyTags(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj, APTR tags, bool notify)
	{
		if (!TryFindLive(ref platform, publicObjects, ownerRoot, obj,
			out var binding)) return false;
		return MuiNativeObjectStateCore.ApplyTags(ref platform, binding.Sidecar,
			tags, notify, publicObjects, ownerRoot, obj);
	}

	// MUIM_MultiSet keeps its fixed packet and target vector in guest memory.
	// Walk the vector as named entries and reuse the same live-binding setter as
	// MUIM_Set; no host collection or caller-object offset is retained.
	internal static bool ApplyMultiSet(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR executor,
		MuiMultiSetMessage packet, APTR vector)
	{
		if (executor.IsNull || vector.IsNull ||
			!TryFindLive(ref platform, publicObjects, ownerRoot, executor,
				out _) || packet.FirstObject == 0) return false;
		var firstObject = APTR.FromPointer(packet.FirstObject);
		if (!TryFindLive(ref platform, publicObjects, ownerRoot, firstObject,
			out _)) return false;
		var targetCursor = default(MuiMultiSetTargetVectorCursor);
		targetCursor.Base = vector;
		if (!MuiMultiSetTargetVectorCodec.TryReadValue(ref platform,
			targetCursor, out var targetRaw)) return false;
		var count = 1u;
		if (targetRaw != 0)
		{
			var target = APTR.FromPointer(targetRaw);
			if (!TryFindLive(ref platform, publicObjects, ownerRoot, target,
				out _)) return false;
			count = 2;
			while (count <= MuiNotifyCore.MaximumMultiSetTargets)
			{
				if (!MuiMultiSetTargetVectorCodec.TryAdvance(ref targetCursor, 1) ||
					!MuiMultiSetTargetVectorCodec.TryReadValue(ref platform,
						targetCursor, out targetRaw)) return false;
				target = APTR.FromPointer(targetRaw);
				if (target.IsNull) break;
				if (count == MuiNotifyCore.MaximumMultiSetTargets ||
					!TryFindLive(ref platform, publicObjects, ownerRoot, target,
						out _)) return false;
				count++;
			}
		}
		if (count == 0 || count > MuiNotifyCore.MaximumMultiSetTargets)
			return false;
		if (firstObject != executor &&
			!SetAttribute(ref platform, publicObjects, ownerRoot, firstObject,
				packet.Attribute, packet.Value, true))
			return false;
		targetCursor.Index = 0;
		for (var index = 1u; index < count; index++)
		{
			if (!MuiMultiSetTargetVectorCodec.TryReadValue(ref platform,
				targetCursor, out targetRaw) || targetRaw == 0) return false;
			var target = APTR.FromPointer(targetRaw);
			if (target != executor)
			{
				if (!TryFindLive(ref platform, publicObjects, ownerRoot, target,
					out _) || !SetAttribute(ref platform, publicObjects, ownerRoot,
					target, packet.Attribute, packet.Value, true))
					return false;
			}
			if (index + 1 < count &&
				!MuiMultiSetTargetVectorCodec.TryAdvance(ref targetCursor, 1))
				return false;
		}
		return true;
	}

	internal static bool AddNotification(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj, uint triggerAttribute,
		uint triggerValue, APTR destination, uint followCount, uint flags,
		out APTR notification)
	{
		notification = APTR.Null;
		if (!TryFindLive(ref platform, publicObjects, ownerRoot, obj,
			out var binding)) return false;
		return MuiNativeObjectStateCore.AddNotification(ref platform,
			binding.Sidecar, triggerAttribute, triggerValue, destination,
			followCount, flags, out notification);
	}

	internal static bool AddNotification(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj, uint triggerAttribute,
		uint triggerValue, APTR destination, uint followCount,
		APTR followParameters, uint flags, out APTR notification)
	{
		notification = APTR.Null;
		if (!TryFindLive(ref platform, publicObjects, ownerRoot, obj,
			out var binding)) return false;
		return MuiNativeObjectStateCore.AddNotification(ref platform,
			binding.Sidecar, triggerAttribute, triggerValue, destination,
			followCount, followParameters, flags, out notification);
	}

	internal static APTR NewObject(ref MuiNativeClassPlatform platform,
		APTR serviceState, APTR ownerRoot, APTR publicObjects,
		APTR className, APTR tags)
		=> NewObject(ref platform, serviceState, ownerRoot, publicObjects,
			className, tags, className);

	// Some generated constructors materialize a temporary class-name record for
	// the synchronous Intuition call. Keep the class-service lease's class-ID
	// pointer separately named and stable so freeing that scratch record cannot
	// leave a borrowed string in the guest-resident lease.
	internal static APTR NewObject(ref MuiNativeClassPlatform platform,
		APTR serviceState, APTR ownerRoot, APTR publicObjects,
		APTR className, APTR tags, APTR serviceClassId)
	{
		if (className.IsNull || serviceClassId.IsNull ||
			!MuiAslTagListCore.Validate(ref platform, tags))
			return APTR.Null;
		var classPointer = MuiClassServiceCore.GetClassAndLease(ref platform,
			serviceState, serviceClassId, out var classLease);
		if (classPointer.IsNull || classLease.IsNull) return APTR.Null;
		var hasCustomClass = MuiClassServiceCore.TryGetCustomClassByLease(
			ref platform, serviceState, classLease, out var customClass);
		if (hasCustomClass && !MuiClassServiceCore.FreeClassLease(ref platform,
			serviceState, classLease)) return APTR.Null;
		var obj = hasCustomClass
			? MuiClassServiceCore.CreateCustomObject(ref platform, serviceState,
				customClass, tags)
			: platform.NewObject(classPointer, tags);
		if (obj.IsNull)
		{
			if (!hasCustomClass)
				MuiClassServiceCore.FreeClassLease(ref platform, serviceState,
					classLease);
			return APTR.Null;
		}
		var tracked = hasCustomClass || MuiClassServiceCore.TrackObjectLeaseByLease(
			ref platform, serviceState, classLease);
		if (!tracked)
		{
			platform.DisposeObject(obj);
			MuiClassServiceCore.FreeClassLease(ref platform, serviceState,
				classLease);
			return APTR.Null;
		}
		if (!Link(ref platform, publicObjects, ownerRoot, obj, classPointer,
			hasCustomClass ? customClass : APTR.Null,
			hasCustomClass ? APTR.Null : classLease, tags))
		{
			platform.DisposeObject(obj);
			if (hasCustomClass)
				MuiClassServiceCore.ReleaseCustomObjectLease(ref platform,
					serviceState, customClass);
			else
				MuiClassServiceCore.ReleaseObjectLeaseByLease(ref platform,
					serviceState, classLease);
			return APTR.Null;
		}
		return obj;
	}

	// Custom classes are addressed by their MUI_CustomClass record rather than
	// by a builtin class-id string. The caller owns the class reference; this
	// path reserves only the custom-class object count and records that identity
	// in the same named public-object binding used by NewObjectA.
	internal static APTR NewCustomObject(ref MuiNativeClassPlatform platform,
		APTR serviceState, APTR ownerRoot, APTR publicObjects, APTR customClass,
		APTR tags)
	{
		if (customClass.IsNull || !MuiAslTagListCore.Validate(ref platform, tags) ||
			!MuiCustomClassCodec.TryRead(ref platform, customClass,
				out var customValue) || customValue.Class.IsNull)
			return APTR.Null;
		var obj = MuiClassServiceCore.CreateCustomObject(ref platform,
			serviceState, customClass, tags);
		if (obj.IsNull) return APTR.Null;
		if (!Link(ref platform, publicObjects, ownerRoot, obj, customValue.Class,
			customClass, APTR.Null, tags))
		{
			platform.DisposeObject(obj);
			MuiClassServiceCore.ReleaseCustomObjectLease(ref platform,
				serviceState, customClass);
			return APTR.Null;
		}
		return obj;
	}

	internal static bool DisposeObject(ref MuiNativeClassPlatform platform,
		APTR serviceState, APTR ownerRoot, APTR publicObjects, APTR obj)
	{
		if (obj.IsNull || ownerRoot.IsNull) return false;
		var memory = default(MuiNativeClassMemory);
		if (!Find(ref memory, publicObjects, ownerRoot, obj, out var previous, out var bindingAddress,
			out var binding) || binding.ActiveCalls != 0 ||
			(binding.DisposeState != MuiNativePublicObjectBinding.StateLive &&
			 binding.DisposeState != MuiNativePublicObjectBinding.StateNativeDisposed &&
			 binding.DisposeState != MuiNativePublicObjectBinding.StateLeaseReleased)) return false;
		var hasSidecar = binding.Sidecar.IsNotNull;
		var sidecar = default(MuiNativeMuiObjectRecord);
		if (hasSidecar && (!MuiNativeMuiObjectCodec.TryRead(ref memory,
			binding.Sidecar, out sidecar) || sidecar.Object != obj ||
			sidecar.Class != binding.Class || sidecar.OwnerRoot != ownerRoot ||
			sidecar.ActiveCalls != 0)) return false;
		binding.ActiveCalls = 1;
		if (!MuiNativePublicObjectBindingCodec.Write(ref memory, bindingAddress, binding))
			return false;

		// A Family/Group child must be removed through its documented public
		// mutation method before it can be disposed independently. Other parent
		// relationships (notably Application_Window and the generated menu tree)
		// remain owner-only and are deliberately not detached through this seam.
		if (binding.DisposeState == MuiNativePublicObjectBinding.StateLive &&
			binding.Parent.IsNotNull)
		{
			var parentKind = sidecar.Flags &
				MuiNativeMuiObjectRecord.ObjectParentKindMask;
			var listAttribute = parentKind ==
				MuiNativeMuiObjectRecord.ObjectParentFamilyChild
				? MuiNativeGuiMode.FamilyList
				: parentKind == MuiNativeMuiObjectRecord.ObjectParentGroupChild
					? MuiNativeGuiMode.GroupChildList : 0;
			if (!hasSidecar || sidecar.Parent != binding.Parent ||
				listAttribute == 0)
			{
				RestoreDisposalAdmission(ref memory, bindingAddress,
					binding.Sidecar, obj);
				return false;
			}
			sidecar.Flags |= MuiNativeMuiObjectRecord.ObjectDisposing;
			if (!MuiNativeMuiObjectCodec.Write(ref memory, binding.Sidecar,
				sidecar) || !MuiNativeFamilyRemovalCore.TryRemove(ref platform,
				binding.Parent, obj, listAttribute) ||
				!ClearParent(ref platform, ownerRoot, publicObjects,
					binding.Parent, obj) ||
				!MuiNativePublicObjectBindingCodec.TryRead(ref memory,
					bindingAddress, out var detachedBinding) ||
				detachedBinding.Signature != MuiNativePublicObjectBinding.Magic ||
				detachedBinding.Object != obj || detachedBinding.OwnerRoot != ownerRoot ||
				detachedBinding.ActiveCalls != 1 || detachedBinding.Parent.IsNotNull ||
				!MuiNativeMuiObjectCodec.TryRead(ref memory, binding.Sidecar,
					out var detachedSidecar) || detachedSidecar.Object != obj ||
				detachedSidecar.Parent.IsNotNull ||
				(detachedSidecar.Flags &
				 MuiNativeMuiObjectRecord.ObjectParentKindMask) != 0 ||
				(detachedSidecar.Flags &
				 MuiNativeMuiObjectRecord.ObjectDisposing) == 0)
			{
				RestoreDisposalAdmission(ref memory, bindingAddress,
					binding.Sidecar, obj);
				return false;
			}
			binding = detachedBinding;
			sidecar = detachedSidecar;
		}

		if (binding.DisposeState == MuiNativePublicObjectBinding.StateLive)
		{
			if (hasSidecar)
			{
				sidecar.Flags |= MuiNativeMuiObjectRecord.ObjectDisposing;
				if (!MuiNativeMuiObjectCodec.Write(ref memory, binding.Sidecar,
					sidecar))
				{
					binding.ActiveCalls = 0;
					MuiNativePublicObjectBindingCodec.Write(ref memory,
						bindingAddress, binding);
					return false;
				}
			}
			if (!TryReadRetainCount(ref platform, obj, out var retainCount) ||
				retainCount == 0)
			{
				RestoreDisposalAdmission(ref memory, bindingAddress,
					binding.Sidecar, obj);
				return false;
			}
			if (retainCount > 1)
			{
				// MorphOS DisposeObject consumes one reference when an object is
				// retained. The native object and its descendants remain alive; keep
				// their public bindings and class leases registered.
				platform.DisposeObject(obj);
				RestoreDisposalAdmission(ref memory, bindingAddress,
					binding.Sidecar, obj);
				return true;
			}
			if (!CapturePendingReleaseSnapshots(ref memory, ref platform,
				ownerRoot, publicObjects, obj))
			{
				RestoreDisposalAdmission(ref memory, bindingAddress,
					binding.Sidecar, obj);
				return false;
			}
			platform.DisposeObject(obj);
			binding.DisposeState = MuiNativePublicObjectBinding.StateNativeDisposed;
			if (!MuiNativePublicObjectBindingCodec.Write(ref memory, bindingAddress, binding))
				return false;
			if (hasSidecar)
			{
				sidecar.Flags &= ~MuiNativeMuiObjectRecord.ObjectDisposing;
				sidecar.Flags |= MuiNativeMuiObjectRecord.ObjectNativeDisposed;
				sidecar.LifecycleState = MuiNativeMuiObjectRecord.StateNativeDisposed;
				if (!MuiNativeMuiObjectCodec.Write(ref memory, binding.Sidecar,
					sidecar)) return false;
			}
		}
		if (!ReconcilePendingReleaseSnapshots(ref memory, ownerRoot,
			publicObjects, obj))
		{
			binding.ActiveCalls = 0;
			MuiNativePublicObjectBindingCodec.Write(ref memory, bindingAddress,
				binding);
			return false;
		}

		// A MUI Family object disposes unretained children as part of OM_DISPOSE.
		// Reconciliation above marked only those children actually destroyed;
		// retained children remain registered and have been detached from the
		// destroyed parent. Process dead children deepest-first while the named
		// Parent links are still available.
		if (!DisposeDescendantBindings(ref platform, serviceState, ownerRoot,
			publicObjects, obj))
		{
			binding.ActiveCalls = 0;
			MuiNativePublicObjectBindingCodec.Write(ref memory, bindingAddress, binding);
			return false;
		}
		// Descendant removal may have rewritten this binding's Next link when
		// the root was the list predecessor. Refresh the complete named record
		// before releasing its lease or unlinking it; the pre-teardown snapshot
		// can otherwise restore a pointer to a freed child binding.
		if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, bindingAddress,
			out binding) || binding.Signature != MuiNativePublicObjectBinding.Magic ||
			binding.Object != obj || binding.OwnerRoot != ownerRoot)
			return false;
		hasSidecar = binding.Sidecar.IsNotNull;
		if (hasSidecar && (!MuiNativeMuiObjectCodec.TryRead(ref memory,
			binding.Sidecar, out sidecar) || sidecar.Object != obj ||
			sidecar.Class != binding.Class || sidecar.OwnerRoot != ownerRoot))
			return false;

		if (binding.DisposeState == MuiNativePublicObjectBinding.StateNativeDisposed)
		{
			var released = binding.CustomClass.IsNotNull
				? MuiClassServiceCore.ReleaseCustomObjectLease(ref platform,
					serviceState, binding.CustomClass)
				: binding.Lease.IsNotNull
					? MuiClassServiceCore.ReleaseObjectLeaseByLease(ref platform,
						serviceState, binding.Lease)
					: MuiClassServiceCore.ReleaseObjectLease(ref platform, serviceState,
						binding.Class);
			if (!released)
			{
				// Keep the named binding available for diagnostics/recovery. The native
				// object has already received OM_DISPOSE, so a retry only releases the
				// class lease and never calls the native dispose vector twice.
				binding.ActiveCalls = 0;
				MuiNativePublicObjectBindingCodec.Write(ref memory, bindingAddress, binding);
				return false;
			}
			binding.DisposeState = MuiNativePublicObjectBinding.StateLeaseReleased;
			if (!MuiNativePublicObjectBindingCodec.Write(ref memory, bindingAddress, binding))
				return false;
			if (hasSidecar)
			{
				sidecar.Flags |= MuiNativeMuiObjectRecord.ObjectLeaseReleased;
				sidecar.LifecycleState = MuiNativeMuiObjectRecord.StateLeaseReleased;
				if (!MuiNativeMuiObjectCodec.Write(ref memory, binding.Sidecar,
					sidecar)) return false;
			}
		}
		if (hasSidecar && !MuiNativeObjectStateCore.ReleaseLists(ref platform,
			binding.Sidecar))
		{
			binding.ActiveCalls = 0;
			MuiNativePublicObjectBindingCodec.Write(ref memory, bindingAddress,
				binding);
			return false;
		}

		if (!Remove(ref memory, publicObjects, ownerRoot, previous, bindingAddress, binding))
		{
			binding.ActiveCalls = 0;
			MuiNativePublicObjectBindingCodec.Write(ref memory, bindingAddress, binding);
			return false;
		}
		if (hasSidecar)
			platform.Free(binding.Sidecar, MuiNativeMuiObjectRecord.Size);
		platform.Free(bindingAddress, MuiNativePublicObjectBinding.Size);
		return true;
	}

	// Record named native ownership relationships for teardown. No object layout
	// is inferred here; this is only the library-owned parent graph.
	internal static bool SetParent<TMemory>(ref TMemory platform,
		APTR ownerRoot, APTR publicObjects, APTR parent, APTR child,
		uint parentKind)
		where TMemory : struct, IMuiGuestMemory
	{
		if (ownerRoot.IsNull || parent.IsNull || child.IsNull || parent == child ||
			(parentKind != 0 && parentKind !=
				MuiNativeMuiObjectRecord.ObjectParentFamilyChild && parentKind !=
				MuiNativeMuiObjectRecord.ObjectParentGroupChild && parentKind !=
				MuiNativeMuiObjectRecord.ObjectParentWindowRootObject))
			return false;
		if (!Find(ref platform, publicObjects, ownerRoot, parent, out _,
			out _, out var parentBinding) ||
			!Find(ref platform, publicObjects, ownerRoot, child, out _,
				out var childAddress, out var childBinding) ||
			parentBinding.DisposeState != MuiNativePublicObjectBinding.StateLive ||
			childBinding.DisposeState != MuiNativePublicObjectBinding.StateLive ||
			childBinding.Parent.IsNotNull) return false;
		var childSidecar = default(MuiNativeMuiObjectRecord);
		if (childBinding.Sidecar.IsNull ||
			!MuiNativeMuiObjectCodec.TryRead(ref platform, childBinding.Sidecar,
				out childSidecar) || childSidecar.Object != child ||
			childSidecar.Class != childBinding.Class ||
			childSidecar.OwnerRoot != ownerRoot ||
			childSidecar.Parent.IsNotNull ||
			(childSidecar.Flags &
			 MuiNativeMuiObjectRecord.ObjectParentKindMask) != 0) return false;
		childBinding.Parent = parent;
		if (!MuiNativePublicObjectBindingCodec.Write(ref platform, childAddress,
			childBinding)) return false;
		if (childBinding.Sidecar.IsNotNull)
		{
			childSidecar.Parent = parent;
			childSidecar.Flags |= parentKind;
			if (!MuiNativeMuiObjectCodec.Write(ref platform, childBinding.Sidecar,
				childSidecar))
			{
				childSidecar.Parent = APTR.Null;
				childSidecar.Flags &=
					~MuiNativeMuiObjectRecord.ObjectParentKindMask;
				MuiNativeMuiObjectCodec.Write(ref platform, childBinding.Sidecar,
					childSidecar);
				childBinding.Parent = APTR.Null;
				MuiNativePublicObjectBindingCodec.Write(ref platform, childAddress,
					childBinding);
				return false;
			}
		}
		return true;
	}

	internal static bool ClearParent<TMemory>(ref TMemory platform,
		APTR ownerRoot, APTR publicObjects, APTR parent, APTR child)
		where TMemory : struct, IMuiGuestMemory
	{
		if (!Find(ref platform, publicObjects, ownerRoot, child, out _,
			out var childAddress, out var childBinding) ||
			childBinding.Parent != parent) return false;
		var childSidecar = default(MuiNativeMuiObjectRecord);
		if (childBinding.Sidecar.IsNotNull &&
			(!MuiNativeMuiObjectCodec.TryRead(ref platform, childBinding.Sidecar,
				out childSidecar) || childSidecar.Parent != parent)) return false;
		childBinding.Parent = APTR.Null;
		if (!MuiNativePublicObjectBindingCodec.Write(ref platform, childAddress,
			childBinding)) return false;
		if (childBinding.Sidecar.IsNull) return true;
		var parentKind = childSidecar.Flags &
			MuiNativeMuiObjectRecord.ObjectParentKindMask;
		childSidecar.Parent = APTR.Null;
		childSidecar.Flags &= ~MuiNativeMuiObjectRecord.ObjectParentKindMask;
		if (MuiNativeMuiObjectCodec.Write(ref platform, childBinding.Sidecar,
			childSidecar)) return true;
		childSidecar.Parent = parent;
		childSidecar.Flags |= parentKind;
	MuiNativeMuiObjectCodec.Write(ref platform, childBinding.Sidecar, childSidecar);
		childBinding.Parent = parent;
		MuiNativePublicObjectBindingCodec.Write(ref platform, childAddress,
			childBinding);
	return false;
}

	private static void RestoreDisposalAdmission<TMemory>(ref TMemory memory,
		APTR bindingAddress, APTR sidecarAddress, APTR obj)
		where TMemory : struct, IMuiGuestMemory
	{
		if (MuiNativePublicObjectBindingCodec.TryRead(ref memory, bindingAddress,
			out var binding) && binding.Object == obj && binding.ActiveCalls == 1)
		{
			binding.ActiveCalls = 0;
			MuiNativePublicObjectBindingCodec.Write(ref memory, bindingAddress,
				binding);
		}
		if (!sidecarAddress.IsNull && MuiNativeMuiObjectCodec.TryRead(ref memory,
			sidecarAddress, out var sidecar) && sidecar.Object == obj)
		{
			sidecar.Flags &= ~MuiNativeMuiObjectRecord.ObjectDisposing;
			MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress, sidecar);
		}
	}

	private static bool TryReadRetainCount(
		ref MuiNativeClassPlatform platform, APTR obj, out uint retainCount)
	{
		retainCount = 0;
		var message = platform.Allocate(MuiNativeObjectMethodMessage.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (message.IsNull || !platform.IsMapped(message,
			MuiNativeObjectMethodMessage.Size))
		{
			if (message.IsNotNull)
				platform.Free(message, MuiNativeObjectMethodMessage.Size);
			return false;
		}
		var memory = default(MuiNativeClassMemory);
		var request = new MuiNativeObjectMethodMessage
		{
			MethodId = MuiNativeBoopsiMethodId.RetainCount,
		};
		var written = MuiNativeObjectMethodMessageCodec.Write(ref memory,
			message, request);
		if (written) retainCount = platform.DoMethod(obj, message);
		platform.Free(message, MuiNativeObjectMethodMessage.Size);
		return written && retainCount != 0;
	}

	// Snapshot each registered native descendant before its owning Group or
	// Family is released. OM_RETAINCNT lets teardown distinguish descendants
	// destroyed by that release from independently retained descendants that
	// survive with their own subtrees intact.
	private static bool CapturePendingReleaseSnapshots<TMemory>(
		ref TMemory memory, ref MuiNativeClassPlatform platform, APTR ownerRoot,
		APTR publicObjects, APTR objectRoot)
		where TMemory : struct, IMuiGuestMemory
	{
		if (!MuiNativePublicObjectRegistryCodec.TryRead(ref memory, publicObjects,
			out var state)) return false;
		var current = state.Head;
		for (uint visited = 0; current.IsNotNull &&
			visited < MuiHeadlessLayout.MaximumTraversal; visited++)
		{
			if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, current,
				out var binding) || binding.Signature !=
				MuiNativePublicObjectBinding.Magic || binding.OwnerRoot != ownerRoot)
			{
				ClearPendingReleaseSnapshots(ref memory, ownerRoot, publicObjects,
					objectRoot);
				return false;
			}
			if (binding.Object != objectRoot &&
				TryGetAncestorDepth(ref memory, publicObjects, ownerRoot,
					binding.Parent, objectRoot, out var depth))
			{
				if (binding.DisposeState != MuiNativePublicObjectBinding.StateLive ||
					binding.ActiveCalls != 0 || binding.Sidecar.IsNull ||
					!MuiNativeMuiObjectCodec.TryRead(ref memory, binding.Sidecar,
						out var sidecar) || sidecar.Object != binding.Object ||
					!TryReadRetainCount(ref platform, binding.Object,
						out var retainCount))
				{
					ClearPendingReleaseSnapshots(ref memory, ownerRoot,
						publicObjects, objectRoot);
					return false;
				}
				sidecar.PendingReleaseCount = retainCount;
				sidecar.PendingReleaseDepth = depth;
				sidecar.PendingReleaseParent = binding.Parent;
				if (!MuiNativeMuiObjectCodec.Write(ref memory, binding.Sidecar,
					sidecar))
				{
					ClearPendingReleaseSnapshots(ref memory, ownerRoot,
						publicObjects, objectRoot);
					return false;
				}
			}
			current = binding.Next;
		}
		if (current.IsNotNull)
		{
			ClearPendingReleaseSnapshots(ref memory, ownerRoot, publicObjects,
				objectRoot);
			return false;
		}
		return true;
	}

	private static bool ClearPendingReleaseSnapshots<TMemory>(ref TMemory memory,
		APTR ownerRoot, APTR publicObjects, APTR objectRoot)
		where TMemory : struct, IMuiGuestMemory
	{
		if (!MuiNativePublicObjectRegistryCodec.TryRead(ref memory, publicObjects,
			out var state)) return false;
		var current = state.Head;
		for (uint visited = 0; current.IsNotNull &&
			visited < MuiHeadlessLayout.MaximumTraversal; visited++)
		{
			if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, current,
				out var binding) || binding.Signature !=
				MuiNativePublicObjectBinding.Magic || binding.OwnerRoot != ownerRoot)
				return false;
			if (binding.Object != objectRoot && binding.Sidecar.IsNotNull &&
				TryGetAncestorDepth(ref memory, publicObjects, ownerRoot,
					binding.Parent, objectRoot, out _) &&
				MuiNativeMuiObjectCodec.TryRead(ref memory, binding.Sidecar,
					out var sidecar) && (sidecar.PendingReleaseCount != 0 ||
					sidecar.PendingReleaseDepth != 0 ||
					sidecar.PendingReleaseParent.IsNotNull))
			{
				sidecar.PendingReleaseCount = 0;
				sidecar.PendingReleaseDepth = 0;
				sidecar.PendingReleaseParent = APTR.Null;
				if (!MuiNativeMuiObjectCodec.Write(ref memory, binding.Sidecar,
					sidecar)) return false;
			}
			current = binding.Next;
		}
		return current.IsNull;
	}

	internal static bool ReconcilePendingReleaseSnapshots<TMemory>(
		ref TMemory memory, APTR ownerRoot, APTR publicObjects, APTR objectRoot)
		where TMemory : struct, IMuiGuestMemory
	{
		// Snapshot depth is recorded before OM_DISPOSE. Find the deepest actual
		// pending record first so a shallow object tree does not pay for all
		// 65,535 representable traversal levels. All persistent state remains in
		// the named registry, binding, and sidecar records.
		if (!MuiNativePublicObjectRegistryCodec.TryRead(ref memory,
			publicObjects, out var registry)) return false;
		var scan = registry.Head;
		uint maximumPendingDepth = 0;
		for (uint visited = 0; scan.IsNotNull &&
			visited < MuiHeadlessLayout.MaximumTraversal; visited++)
		{
			if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, scan,
				out var binding) || binding.Signature !=
				MuiNativePublicObjectBinding.Magic || binding.OwnerRoot != ownerRoot)
				return false;
			if (binding.Sidecar.IsNotNull)
			{
				if (!MuiNativeMuiObjectCodec.TryRead(ref memory, binding.Sidecar,
					out var sidecar) || sidecar.Object != binding.Object ||
					sidecar.OwnerRoot != ownerRoot) return false;
				var hasSnapshot = sidecar.PendingReleaseCount != 0 ||
					sidecar.PendingReleaseDepth != 0 ||
					sidecar.PendingReleaseParent.IsNotNull;
				if (hasSnapshot)
				{
					if (binding.Object == objectRoot ||
						sidecar.PendingReleaseCount == 0 ||
						sidecar.PendingReleaseDepth == 0 ||
						sidecar.PendingReleaseDepth >
							MuiHeadlessLayout.MaximumTraversal ||
						sidecar.PendingReleaseParent.IsNull ||
						binding.ActiveCalls != 0) return false;
					if (sidecar.PendingReleaseDepth > maximumPendingDepth)
						maximumPendingDepth = sidecar.PendingReleaseDepth;
				}
			}
			scan = binding.Next;
		}
		if (scan.IsNotNull) return false;
		if (maximumPendingDepth == 0) return true;

		// Visit shallowest descendants first: whether a child was destroyed
		// determines whether its children were visited by native Group teardown.
		for (uint depth = 1; depth <= maximumPendingDepth; depth++)
		{
			if (!MuiNativePublicObjectRegistryCodec.TryRead(ref memory,
				publicObjects, out var state)) return false;
			var current = state.Head;
			for (uint visited = 0; current.IsNotNull &&
				visited < MuiHeadlessLayout.MaximumTraversal; visited++)
			{
				if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, current,
					out var binding) || binding.Signature !=
					MuiNativePublicObjectBinding.Magic || binding.OwnerRoot != ownerRoot)
					return false;
				if (binding.Object != objectRoot && binding.Sidecar.IsNotNull &&
					MuiNativeMuiObjectCodec.TryRead(ref memory, binding.Sidecar,
						out var sidecar) && sidecar.PendingReleaseDepth == depth)
				{
					if (sidecar.PendingReleaseCount == 0 ||
						sidecar.PendingReleaseParent.IsNull ||
						binding.ActiveCalls != 0 ||
						!TryFindByObject(ref memory, publicObjects, ownerRoot,
							sidecar.PendingReleaseParent, out var parent)) return false;
					var parentDisposed = parent.DisposeState ==
						MuiNativePublicObjectBinding.StateNativeDisposed ||
						parent.DisposeState ==
						MuiNativePublicObjectBinding.StateLeaseReleased;
					if (parentDisposed && sidecar.PendingReleaseCount == 1)
					{
						if (binding.DisposeState ==
							MuiNativePublicObjectBinding.StateLive)
						{
							binding.DisposeState =
								MuiNativePublicObjectBinding.StateNativeDisposed;
							if (!MuiNativePublicObjectBindingCodec.Write(ref memory,
								current, binding)) return false;
							sidecar.Flags &=
								~MuiNativeMuiObjectRecord.ObjectDisposing;
							sidecar.Flags |=
								MuiNativeMuiObjectRecord.ObjectNativeDisposed;
							sidecar.LifecycleState =
								MuiNativeMuiObjectRecord.StateNativeDisposed;
						}
						else if (binding.DisposeState !=
							MuiNativePublicObjectBinding.StateNativeDisposed)
							return false;
					}
					else if (parentDisposed)
					{
						if (binding.DisposeState !=
							MuiNativePublicObjectBinding.StateLive ||
							(binding.Parent.IsNotNull &&
								(binding.Parent != sidecar.PendingReleaseParent ||
								!ClearParent(ref memory, ownerRoot, publicObjects,
									sidecar.PendingReleaseParent, binding.Object))))
							return false;
						if (!MuiNativeMuiObjectCodec.TryRead(ref memory,
							binding.Sidecar, out sidecar)) return false;
					}
					sidecar.PendingReleaseCount = 0;
					sidecar.PendingReleaseDepth = 0;
					sidecar.PendingReleaseParent = APTR.Null;
					if (!MuiNativeMuiObjectCodec.Write(ref memory, binding.Sidecar,
						sidecar)) return false;
				}
				current = binding.Next;
			}
			if (current.IsNotNull) return false;
		}
		return true;
	}

	private static bool DisposeDescendantBindings(
		ref MuiNativeClassPlatform platform, APTR serviceState, APTR ownerRoot,
		APTR publicObjects, APTR objectRoot)
	{
		var memory = default(MuiNativeClassMemory);
		for (uint count = 0; count < MuiHeadlessLayout.MaximumTraversal; count++)
		{
			if (!FindDeepestDisposedDescendant(ref memory, publicObjects, ownerRoot,
				objectRoot, out var previous, out var address, out var binding,
				out var found)) return false;
			if (!found) return true;
			if (binding.DisposeState == MuiNativePublicObjectBinding.StateNativeDisposed)
			{
				if (!ReleaseLease(ref platform, serviceState, binding)) return false;
				binding.DisposeState = MuiNativePublicObjectBinding.StateLeaseReleased;
				if (!MuiNativePublicObjectBindingCodec.Write(ref memory, address,
					binding)) return false;
			}
			if (binding.Sidecar.IsNotNull &&
				!MuiNativeObjectStateCore.ReleaseLists(ref platform,
					binding.Sidecar)) return false;
			if (!Remove(ref memory, publicObjects, ownerRoot, previous, address,
				binding)) return false;
			if (binding.Sidecar.IsNotNull)
				platform.Free(binding.Sidecar, MuiNativeMuiObjectRecord.Size);
			platform.Free(address, MuiNativePublicObjectBinding.Size);
		}
		return false;
	}

	private static bool FindDeepestDisposedDescendant<T>(ref T memory,
		APTR publicObjects, APTR ownerRoot, APTR objectRoot, out APTR previous,
		out APTR address, out MuiNativePublicObjectBinding value, out bool found)
		where T : struct, IMuiGuestMemory
	{
		previous = APTR.Null;
		address = APTR.Null;
		value = default;
		found = false;
		if (!MuiNativePublicObjectRegistryCodec.TryRead(ref memory, publicObjects,
			out var state)) return false;
		var current = state.Head;
		var bestDepth = 0u;
		var bestPrevious = APTR.Null;
		var bestAddress = APTR.Null;
		var bestValue = default(MuiNativePublicObjectBinding);
		for (uint visited = 0; current.IsNotNull &&
			visited < MuiHeadlessLayout.MaximumTraversal; visited++)
		{
			if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, current,
				out var candidate) || candidate.Signature != MuiNativePublicObjectBinding.Magic ||
				candidate.OwnerRoot != ownerRoot) return false;
			if (candidate.Object != objectRoot &&
				(candidate.DisposeState == MuiNativePublicObjectBinding.StateNativeDisposed ||
					candidate.DisposeState == MuiNativePublicObjectBinding.StateLeaseReleased) &&
				TryGetAncestorDepth(ref memory, publicObjects, ownerRoot,
					candidate.Parent, objectRoot, out var depth) &&
				(!found || depth > bestDepth))
			{
				found = true;
				bestDepth = depth;
				bestPrevious = previous;
				bestAddress = current;
				bestValue = candidate;
			}
			previous = current;
			current = candidate.Next;
		}
		if (current.IsNotNull) return false;
		previous = bestPrevious;
		address = bestAddress;
		value = bestValue;
		return true;
	}

	private static bool TryGetAncestorDepth<T>(ref T memory, APTR publicObjects,
		APTR ownerRoot, APTR parent, APTR objectRoot, out uint depth)
		where T : struct, IMuiGuestMemory
	{
		depth = 0;
		var current = parent;
		for (var index = 1u; current.IsNotNull &&
			index <= MuiHeadlessLayout.MaximumTraversal; index++)
		{
			if (current == objectRoot) { depth = index; return true; }
			if (!TryFindByObject(ref memory, publicObjects, ownerRoot, current,
				out var value)) return false;
			current = value.Parent;
		}
		return false;
	}

	private static bool TryFindByObject<T>(ref T memory, APTR publicObjects,
		APTR ownerRoot, APTR obj, out MuiNativePublicObjectBinding value)
		where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiNativePublicObjectRegistryCodec.TryRead(ref memory, publicObjects,
			out var state)) return false;
		var current = state.Head;
		for (uint visited = 0; current.IsNotNull &&
			visited < MuiHeadlessLayout.MaximumTraversal; visited++)
		{
			if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, current,
				out var candidate) || candidate.Signature != MuiNativePublicObjectBinding.Magic ||
				candidate.OwnerRoot != ownerRoot) return false;
			if (candidate.Object == obj) { value = candidate; return true; }
			current = candidate.Next;
		}
		return false;
	}

	private static bool ReleaseLease(ref MuiNativeClassPlatform platform,
		APTR serviceState, MuiNativePublicObjectBinding binding) =>
		binding.CustomClass.IsNotNull
			? MuiClassServiceCore.ReleaseCustomObjectLease(ref platform,
				serviceState, binding.CustomClass)
			: binding.Lease.IsNotNull
				? MuiClassServiceCore.ReleaseObjectLeaseByLease(ref platform,
					serviceState, binding.Lease)
				: MuiClassServiceCore.ReleaseObjectLease(ref platform, serviceState,
					binding.Class);

	private static bool Link(ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR obj, APTR classPointer, APTR customClass,
		APTR lease, APTR tags)
	{
		var memory = default(MuiNativeClassMemory);
		var sidecarResolution = MuiNativeClassDispatcher.ResolveNativeObjectSidecar(
			ref memory, classPointer, ownerRoot, obj, out var nativeSidecarAddress,
			out var nativeObjectBindingAddress);
		if (sidecarResolution ==
			MuiNativeClassDispatcher.NativeObjectSidecarResolution.Invalid)
			return false;
		var classOwnsSidecar = sidecarResolution ==
			MuiNativeClassDispatcher.NativeObjectSidecarResolution.Found;
		if (!MuiNativePublicObjectRegistryCodec.TryRead(ref memory, publicObjects,
			out var state))
			return false;
		var bindingAddress = platform.Allocate(MuiNativePublicObjectBinding.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (bindingAddress.IsNull) return false;
		var sidecarAddress = nativeSidecarAddress;
		if (classOwnsSidecar &&
			(MuiNativeClassDispatcher.ResolveNativeObjectSidecar(ref memory,
				classPointer, ownerRoot, obj, out var confirmedSidecar,
				out var confirmedObjectBinding) !=
				MuiNativeClassDispatcher.NativeObjectSidecarResolution.Found ||
			confirmedSidecar != nativeSidecarAddress ||
			confirmedObjectBinding != nativeObjectBindingAddress))
		{
			platform.Free(bindingAddress, MuiNativePublicObjectBinding.Size);
			return false;
		}
		if (!classOwnsSidecar)
			sidecarAddress = platform.Allocate(MuiNativeMuiObjectRecord.Size,
				MuiHeadlessLayout.AllocationFlags);
		if (sidecarAddress.IsNull)
		{
			platform.Free(bindingAddress, MuiNativePublicObjectBinding.Size);
			return false;
		}
		var sidecar = default(MuiNativeMuiObjectRecord);
		if (classOwnsSidecar)
		{
			if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
				out sidecar) || sidecar.Object != obj ||
				sidecar.Class != classPointer || sidecar.OwnerRoot != ownerRoot)
			{
				platform.Free(bindingAddress, MuiNativePublicObjectBinding.Size);
				return false;
			}
			sidecar.Generation = state.Mutation == uint.MaxValue
				? uint.MaxValue : state.Mutation + 1;
		}
		else
		{
			sidecar.Signature = MuiNativeMuiObjectRecord.Magic;
			sidecar.Revision = MuiNativeMuiObjectRecord.Version;
			sidecar.Object = obj;
			sidecar.Class = classPointer;
			sidecar.OwnerRoot = ownerRoot;
			sidecar.Flags = MuiNativeMuiObjectRecord.ObjectInitialized;
			sidecar.Generation = state.Mutation == uint.MaxValue
				? uint.MaxValue : state.Mutation + 1;
		}
		if (!MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress, sidecar))
		{
			if (!classOwnsSidecar)
				platform.Free(sidecarAddress, MuiNativeMuiObjectRecord.Size);
			platform.Free(bindingAddress, MuiNativePublicObjectBinding.Size);
			return false;
		}
		if (!MuiNativeObjectStateCore.ApplyTags(ref platform, sidecarAddress,
			tags))
		{
			ReleaseUnpublishedSidecar(ref platform, sidecarAddress,
				classOwnsSidecar);
			platform.Free(bindingAddress, MuiNativePublicObjectBinding.Size);
			return false;
		}
		var binding = default(MuiNativePublicObjectBinding);
		binding.Signature = MuiNativePublicObjectBinding.Magic;
		binding.Next = state.Head;
		binding.Object = obj;
		binding.Class = classPointer;
		binding.Lease = lease;
		binding.CustomClass = customClass;
		binding.OwnerRoot = ownerRoot;
		binding.Sidecar = sidecarAddress;
		if (!MuiNativePublicObjectBindingCodec.Write(ref memory, bindingAddress, binding))
		{
			ReleaseUnpublishedSidecar(ref platform, sidecarAddress,
				classOwnsSidecar);
			platform.Free(bindingAddress, MuiNativePublicObjectBinding.Size);
			return false;
		}
		state.Head = bindingAddress;
		state.Mutation++;
		if (!MuiNativePublicObjectRegistryCodec.Write(ref memory, publicObjects,
			state))
		{
			MuiNativeObjectStateCore.ReleaseLists(ref platform, sidecarAddress);
			platform.Free(sidecarAddress, MuiNativeMuiObjectRecord.Size);
			platform.Free(bindingAddress, MuiNativePublicObjectBinding.Size);
			return false;
		}
		if (!TryLinkFamilyChildren(ref platform, publicObjects, ownerRoot,
			obj, tags))
		{
			state.Head = binding.Next;
			state.Mutation = state.Mutation == uint.MaxValue ? uint.MaxValue :
				state.Mutation + 1;
			MuiNativePublicObjectRegistryCodec.Write(ref memory, publicObjects,
				state);
			ReleaseUnpublishedSidecar(ref platform, sidecarAddress,
				classOwnsSidecar);
			platform.Free(bindingAddress, MuiNativePublicObjectBinding.Size);
			return false;
		}
		if (!TryLinkWindowRootObject(ref platform, publicObjects, ownerRoot,
			obj, tags))
		{
			RollbackFamilyChildren(ref platform, publicObjects, ownerRoot, obj,
				tags);
			state.Head = binding.Next;
			state.Mutation = state.Mutation == uint.MaxValue ? uint.MaxValue :
				state.Mutation + 1;
			MuiNativePublicObjectRegistryCodec.Write(ref memory, publicObjects,
				state);
			ReleaseUnpublishedSidecar(ref platform, sidecarAddress,
				classOwnsSidecar);
			platform.Free(bindingAddress, MuiNativePublicObjectBinding.Size);
			return false;
		}
		if (!TryLinkApplicationWindows(ref platform, publicObjects, ownerRoot,
			obj, tags, out var linkedApplicationWindows))
		{
			RollbackFamilyChildren(ref platform, publicObjects, ownerRoot, obj,
				tags);
			RollbackWindowRootObject(ref platform, publicObjects, ownerRoot, obj,
				tags);
			state.Head = binding.Next;
			state.Mutation = state.Mutation == uint.MaxValue ? uint.MaxValue :
				state.Mutation + 1;
			MuiNativePublicObjectRegistryCodec.Write(ref memory, publicObjects,
				state);
			ReleaseUnpublishedSidecar(ref platform, sidecarAddress,
				classOwnsSidecar);
			platform.Free(bindingAddress, MuiNativePublicObjectBinding.Size);
			return false;
		}
		if (!MuiNativeApplicationSleep.TryApplyInitialSleepAttributes(
			ref platform, publicObjects, ownerRoot, obj, tags))
		{
			RollbackFamilyChildren(ref platform, publicObjects, ownerRoot, obj,
				tags);
			RollbackWindowRootObject(ref platform, publicObjects, ownerRoot, obj,
				tags);
			RollbackApplicationWindows(ref platform, publicObjects, ownerRoot,
				obj, tags, linkedApplicationWindows);
			state.Head = binding.Next;
			state.Mutation = state.Mutation == uint.MaxValue ? uint.MaxValue :
				state.Mutation + 1;
			MuiNativePublicObjectRegistryCodec.Write(ref memory, publicObjects,
				state);
			ReleaseUnpublishedSidecar(ref platform, sidecarAddress,
				classOwnsSidecar);
			platform.Free(bindingAddress, MuiNativePublicObjectBinding.Size);
			return false;
		}
		if (classOwnsSidecar &&
				!MuiNativeClassDispatcher.TrySetPublicSidecarBorrowed(ref memory,
					nativeObjectBindingAddress, classPointer, ownerRoot, obj,
					sidecarAddress))
		{
			RollbackFamilyChildren(ref platform, publicObjects, ownerRoot, obj,
				tags);
			RollbackWindowRootObject(ref platform, publicObjects, ownerRoot, obj,
				tags);
			RollbackApplicationWindows(ref platform, publicObjects, ownerRoot,
				obj, tags, linkedApplicationWindows);
			state.Head = binding.Next;
			state.Mutation = state.Mutation == uint.MaxValue ? uint.MaxValue :
				state.Mutation + 1;
			MuiNativePublicObjectRegistryCodec.Write(ref memory, publicObjects,
				state);
			ReleaseUnpublishedSidecar(ref platform, sidecarAddress,
				classOwnsSidecar);
			platform.Free(bindingAddress, MuiNativePublicObjectBinding.Size);
			return false;
		}
		return true;
	}

	private static void ReleaseUnpublishedSidecar(
		ref MuiNativeClassPlatform platform, APTR sidecarAddress,
		bool classOwnsSidecar)
	{
		if (classOwnsSidecar || sidecarAddress.IsNull) return;
		MuiNativeObjectStateCore.ReleaseLists(ref platform, sidecarAddress);
		platform.Free(sidecarAddress, MuiNativeMuiObjectRecord.Size);
	}

	private const uint ApplicationWindowTag = 0x8042BFE0;
	private const uint FamilyChildTag = MuiGroupChildrenCore.FamilyChild;
	private const uint GroupChildTag = MuiGroupChildrenCore.Child;
	private const uint WindowRootObjectTag = MuiWindowPublicCore.RootObject;

	internal struct MuiNativeWindowRootObjectTagRecord
	{
		internal APTR Object;
		internal uint HasTag;
	}

	// Window.mui owns exactly the object named by its final RootObject tag.
	// Preserve that ownership in the named sidecar graph so disposing a Window
	// can retire its descendants after native OM_DISPOSE has handled the tree.
	internal static bool TryLinkWindowRootObject<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR window, APTR tags)
		where TMemory : struct, IMuiGuestMemory
	{
		if (!Find(ref memory, publicObjects, ownerRoot, window, out _, out _,
			out var windowBinding) || windowBinding.DisposeState !=
			MuiNativePublicObjectBinding.StateLive ||
			!MuiNativeApplicationSleep.TryIsWindowClass(ref memory,
				windowBinding.Class, out var isWindow)) return false;
		if (!isWindow) return true;
		if (!TryReadWindowRootObjectTag(ref memory, tags, out var rootTag))
			return false;
		if (rootTag.HasTag == 0 || rootTag.Object.IsNull) return true;
		if (rootTag.Object == window) return false;
		if (!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
			ownerRoot, rootTag.Object, out var rootBinding))
		{
			// Foreign BOOPSI objects remain legal. A stale or malformed owned
			// binding, however, cannot participate in CopperOS disposal tracking.
			return !MuiNativePublicObjectCore.TryFindBinding(ref memory,
				publicObjects, ownerRoot, rootTag.Object, out _);
		}
		if (rootBinding.Parent.IsNotNull) return false;
		return SetParent(ref memory, ownerRoot, publicObjects, window,
			rootTag.Object,
			MuiNativeMuiObjectRecord.ObjectParentWindowRootObject);
	}

	private static bool TryReadWindowRootObjectTag<TMemory>(ref TMemory memory,
		APTR tags, out MuiNativeWindowRootObjectTagRecord rootTag)
		where TMemory : struct, IMuiGuestMemory
	{
		rootTag = default;
		if (tags.IsNull) return true;
		var cursor = default(MuiAslTagItemCursor);
		cursor.Base = tags;
		uint visited = 0;
		var terminated = false;
		while (TryReadApplicationTag(ref memory, ref cursor, ref visited,
			out var item, out var done))
		{
			if (done) { terminated = true; break; }
			if (item.Tag != WindowRootObjectTag) continue;
			rootTag.Object = APTR.FromPointer(item.Data);
			rootTag.HasTag = 1;
		}
		return terminated;
	}

	private static void RollbackWindowRootObject<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR window, APTR tags)
		where TMemory : struct, IMuiGuestMemory
	{
		if (!TryReadWindowRootObjectTag(ref memory, tags, out var rootTag) ||
			rootTag.HasTag == 0 || rootTag.Object.IsNull) return;
		ClearParent(ref memory, ownerRoot, publicObjects, window,
			rootTag.Object);
	}

	// Retain the same named native parent graph used by recursive object
	// disposal and active-group event routing for initializer Family children.
	// Foreign BOOPSI children remain legal; only objects already represented in
	// this library's live public registry receive a sidecar parent link.
	private static bool TryLinkFamilyChildren(
		ref MuiNativeClassPlatform platform, APTR publicObjects, APTR ownerRoot,
		APTR parent, APTR tags)
	{
		if (tags.IsNull) return true;
		var memory = default(MuiNativeClassMemory);
		if (!Find(ref memory, publicObjects, ownerRoot, parent, out _,
			out _, out var parentBinding) || parentBinding.DisposeState !=
			MuiNativePublicObjectBinding.StateLive) return false;
		var cursor = default(MuiAslTagItemCursor);
		cursor.Base = tags;
		uint visited = 0;
		var terminated = false;
		while (TryReadApplicationTag(ref platform, ref cursor, ref visited,
			out var item, out var done))
		{
			if (done) { terminated = true; break; }
			if (item.Tag != FamilyChildTag && item.Tag != GroupChildTag) continue;
			var child = APTR.FromPointer(item.Data);
			if (child.IsNull || child == parent) return false;
			if (!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, child, out var childBinding))
			{
				if (MuiNativePublicObjectCore.TryFindBinding(ref platform,
					publicObjects, ownerRoot, child, out _)) return false;
				continue;
			}
			if (childBinding.Parent.IsNotNull) return false;
		}
		if (!terminated) return false;

		cursor = default;
		cursor.Base = tags;
		visited = 0;
		while (TryReadApplicationTag(ref platform, ref cursor, ref visited,
			out var item, out var done))
		{
			if (done) return true;
			if (item.Tag != FamilyChildTag && item.Tag != GroupChildTag) continue;
			var child = APTR.FromPointer(item.Data);
			if (!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, child, out _)) continue;
			var parentKind = item.Tag == FamilyChildTag
				? MuiNativeMuiObjectRecord.ObjectParentFamilyChild
				: MuiNativeMuiObjectRecord.ObjectParentGroupChild;
			if (SetParent(ref platform, ownerRoot, publicObjects, parent, child,
				parentKind))
				continue;
			RollbackFamilyChildren(ref platform, publicObjects, ownerRoot, parent,
				tags);
			return false;
		}
		RollbackFamilyChildren(ref platform, publicObjects, ownerRoot, parent,
			tags);
		return false;
	}

	private static void RollbackFamilyChildren(
		ref MuiNativeClassPlatform platform, APTR publicObjects, APTR ownerRoot,
		APTR parent, APTR tags)
	{
		if (tags.IsNull) return;
		var cursor = default(MuiAslTagItemCursor);
		cursor.Base = tags;
		uint visited = 0;
		while (TryReadApplicationTag(ref platform, ref cursor, ref visited,
			out var item, out var done))
		{
			if (done) return;
			if (item.Tag != FamilyChildTag && item.Tag != GroupChildTag) continue;
			ClearParent(ref platform, ownerRoot, publicObjects, parent,
				APTR.FromPointer(item.Data));
		}
	}

	// Link the initializer-only MUIA_Application_Window relations after the
	// application itself is visible in the native registry. This gives
	// NewInput a precise set of application-owned windows instead of collecting
	// unrelated message-port bits from every MUI object in the library.
	private static bool TryLinkApplicationWindows(
		ref MuiNativeClassPlatform platform, APTR publicObjects, APTR ownerRoot,
		APTR application, APTR tags, out uint linkedCount)
	{
		linkedCount = 0;
		if (tags.IsNull) return true;
		var memory = default(MuiNativeClassMemory);
		if (!Find(ref memory, publicObjects, ownerRoot, application, out _,
			out _, out var applicationBinding) || applicationBinding.DisposeState !=
			MuiNativePublicObjectBinding.StateLive) return false;
		var cursor = default(MuiAslTagItemCursor);
		cursor.Base = tags;
		uint visited = 0;
		var terminated = false;
		while (TryReadApplicationTag(ref platform, ref cursor, ref visited,
			out var item, out var done))
		{
			if (done) { terminated = true; break; }
			if (item.Tag != ApplicationWindowTag) continue;
			var window = APTR.FromPointer(item.Data);
			if (window.IsNull || window == application ||
				!Find(ref memory, publicObjects, ownerRoot, window, out _,
					out _, out var windowBinding) || windowBinding.DisposeState !=
					MuiNativePublicObjectBinding.StateLive ||
				windowBinding.Parent.IsNotNull) return false;
		}
		if (!terminated) return false;

		cursor = default;
		cursor.Base = tags;
		visited = 0;
		uint linked = 0;
		while (TryReadApplicationTag(ref platform, ref cursor, ref visited,
			out var item, out var done))
		{
			if (done)
			{
				linkedCount = linked;
				return true;
			}
			if (item.Tag != ApplicationWindowTag) continue;
			var window = APTR.FromPointer(item.Data);
			if (SetParent(ref platform, ownerRoot, publicObjects, application,
				window, 0))
			{
				linked++;
				continue;
			}
			RollbackApplicationWindows(ref platform, publicObjects, ownerRoot,
				application, tags, linked);
			return false;
		}
		RollbackApplicationWindows(ref platform, publicObjects, ownerRoot,
			application, tags, linked);
		return false;
	}

	private static void RollbackApplicationWindows(
		ref MuiNativeClassPlatform platform, APTR publicObjects, APTR ownerRoot,
		APTR application, APTR tags, uint linked)
	{
		var cursor = default(MuiAslTagItemCursor);
		cursor.Base = tags;
		uint visited = 0;
		var remaining = linked;
		while (remaining != 0 && TryReadApplicationTag(ref platform, ref cursor,
			ref visited, out var item, out var done))
		{
			if (done) return;
			if (item.Tag != ApplicationWindowTag) continue;
			ClearParent(ref platform, ownerRoot, publicObjects, application,
				APTR.FromPointer(item.Data));
			remaining--;
		}
	}

	private static bool TryReadApplicationTag<TMemory>(
		ref TMemory platform, ref MuiAslTagItemCursor cursor,
		ref uint visited, out MuiAslTagItemRecord item, out bool done)
		where TMemory : struct, IMuiGuestMemory
	{
		item = default;
		done = false;
		while (cursor.Base.IsNotNull && visited++ < MuiAslTagListCore.MaximumSteps)
		{
			if (!MuiAslTagItemVectorCodec.TryRead(ref platform, cursor,
				out item)) return false;
			if (item.Tag == MuiAslTagListCore.TagDone)
			{
				done = true;
				return true;
			}
			if (item.Tag == MuiAslTagListCore.TagIgnore)
			{
				if (!MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1))
					return false;
				continue;
			}
			if (item.Tag == MuiAslTagListCore.TagMore)
			{
				if (item.Data == 0)
				{
					done = true;
					return true;
				}
				cursor.Base = APTR.FromPointer(item.Data);
				cursor.Index = 0;
				continue;
			}
			if (item.Tag == MuiAslTagListCore.TagSkip)
			{
				if (item.Data == uint.MaxValue ||
					!MuiAslTagItemVectorCodec.TryAdvance(ref cursor,
						item.Data + 1)) return false;
				continue;
			}
			return MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1);
		}
		return false;
	}

	private static bool Find<T>(ref T memory, APTR publicObjects, APTR ownerRoot, APTR obj,
		out APTR previous, out APTR address,
		out MuiNativePublicObjectBinding value) where T : struct, IMuiGuestMemory
	{
		previous = APTR.Null;
		address = APTR.Null;
		value = default;
		if (!MuiNativePublicObjectRegistryCodec.TryRead(ref memory, publicObjects,
			out var state))
			return false;
		var current = state.Head;
		for (uint visited = 0; current.IsNotNull &&
			visited < MuiHeadlessLayout.MaximumTraversal; visited++)
		{
			if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, current,
				out var candidate) || candidate.Signature != MuiNativePublicObjectBinding.Magic ||
				candidate.OwnerRoot != ownerRoot) return false;
			if (candidate.Object == obj)
			{
				address = current;
				value = candidate;
				return true;
			}
			previous = current;
			current = candidate.Next;
		}
		return false;
	}

	private static bool Remove<T>(ref T memory, APTR publicObjects, APTR ownerRoot, APTR previous,
		APTR address, MuiNativePublicObjectBinding value)
		where T : struct, IMuiGuestMemory
	{
		if (!MuiNativePublicObjectRegistryCodec.TryRead(ref memory, publicObjects,
			out var state))
			return false;
		if (previous.IsNull)
		{
			if (state.Head != address) return false;
			state.Head = value.Next;
		}
		else
		{
			if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, previous,
				out var previousValue) || previousValue.Next != address) return false;
			previousValue.Next = value.Next;
			if (!MuiNativePublicObjectBindingCodec.Write(ref memory, previous,
				previousValue)) return false;
		}
		state.Mutation++;
		return MuiNativePublicObjectRegistryCodec.Write(ref memory, publicObjects,
			state);
	}

}
