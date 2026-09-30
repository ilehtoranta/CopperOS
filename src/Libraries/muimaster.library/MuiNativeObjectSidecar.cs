/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Native MUI object state is kept in a complete, guest-resident record.  The
// native object itself remains owned by Intuition; this record is the MUI
// lifetime and future attribute/notification state attached to that object.
// No native object layout is inferred from an address or an offset. The
// notification-depth/suppression fields, requested/rejected IDCMP masks, the
// FIFO ReturnID head, application wait state, cached native Window, and
// input-handler queue/generation plus its shared timer port are guest-resident
// alongside each Window's caller-owned MUI_EventHandlerNode queue/generation.
// Application PushMethod entries are retained through their own head and
// monotonic queue identifier rather than hidden state.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeMuiObjectRecord
{
	internal const uint Magic = 0x4D554F49; // "MUOI"
	internal const uint Version = 12;
	internal const uint Size = 128;

	internal const uint ObjectInitialized = 1;
	internal const uint ObjectDisposing = 2;
	internal const uint ObjectNativeDisposed = 4;
	internal const uint ObjectLeaseReleased = 8;
	internal const uint ObjectParentFamilyChild = 0x10;
	internal const uint ObjectParentGroupChild = 0x20;
	internal const uint ObjectParentWindowRootObject = 0x40;
	internal const uint ObjectParentKindMask = ObjectParentFamilyChild |
		ObjectParentGroupChild | ObjectParentWindowRootObject;

	internal const uint StateLive = 0;
	internal const uint StateNativeDisposed = 1;
	internal const uint StateLeaseReleased = 2;

	internal uint Signature;
	internal uint Revision;
	internal APTR Object;
	internal APTR Class;
	internal APTR OwnerRoot;
	internal APTR Attributes;
	internal APTR Notifications;
	internal APTR Parent;
	internal uint Flags;
	internal uint Generation;
	internal uint ObjectId;
	internal uint UserData;
	internal uint ActiveCalls;
	internal uint LifecycleState;
	internal uint NotifyDepth;
	internal uint NotifySuppressionMethod;
	internal uint RequestedIDCMP;
	internal uint RejectedIDCMP;
	internal APTR ReturnIdQueue;
	internal APTR InputSignalTask;
	internal uint InputSignalMask;
	internal APTR NativeWindow;
	internal APTR InputHandlers;
	internal uint InputHandlerGeneration;
	internal APTR InputTimerPort;
	internal APTR WindowEventHandlers;
	internal uint WindowEventHandlerGeneration;
	internal uint PendingReleaseCount;
	internal uint PendingReleaseDepth;
	internal APTR PendingReleaseParent;
	internal APTR ApplicationPushQueue;
	internal uint ApplicationPushGeneration;
}

internal static class MuiNativeMuiObjectCodec
{
	internal static bool TryRead<T>(ref T memory, APTR address,
		out MuiNativeMuiObjectRecord value) where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeMuiObjectRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Signature) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Revision) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var obj) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var cls) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var root) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var attributes) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var notifications) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var parent) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Flags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Generation) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.ObjectId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.UserData) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.ActiveCalls) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.LifecycleState) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.NotifyDepth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.NotifySuppressionMethod) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.RequestedIDCMP) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.RejectedIDCMP) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var returnIdQueue) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var inputSignalTask) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.InputSignalMask) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var nativeWindow) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var inputHandlers) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.InputHandlerGeneration) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var inputTimerPort) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var windowEventHandlers) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.WindowEventHandlerGeneration) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.PendingReleaseCount) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.PendingReleaseDepth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var pendingReleaseParent) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var applicationPushQueue) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.ApplicationPushGeneration) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Object = APTR.FromPointer(obj);
		value.Class = APTR.FromPointer(cls);
		value.OwnerRoot = APTR.FromPointer(root);
		value.Attributes = APTR.FromPointer(attributes);
		value.Notifications = APTR.FromPointer(notifications);
		value.Parent = APTR.FromPointer(parent);
		value.ReturnIdQueue = APTR.FromPointer(returnIdQueue);
		value.InputSignalTask = APTR.FromPointer(inputSignalTask);
		value.NativeWindow = APTR.FromPointer(nativeWindow);
		value.InputHandlers = APTR.FromPointer(inputHandlers);
		value.InputTimerPort = APTR.FromPointer(inputTimerPort);
		value.WindowEventHandlers = APTR.FromPointer(windowEventHandlers);
		value.PendingReleaseParent = APTR.FromPointer(pendingReleaseParent);
		value.ApplicationPushQueue = APTR.FromPointer(applicationPushQueue);
		return value.Signature == MuiNativeMuiObjectRecord.Magic &&
			value.Revision == MuiNativeMuiObjectRecord.Version;
	}

	internal static bool Write<T>(ref T memory, APTR address,
		MuiNativeMuiObjectRecord value) where T : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeMuiObjectRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Signature) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Revision) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Object.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Class.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.OwnerRoot.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Attributes.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Notifications.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Parent.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Flags) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Generation) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.ObjectId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.UserData) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.ActiveCalls) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.LifecycleState) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.NotifyDepth) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.NotifySuppressionMethod) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.RequestedIDCMP) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.RejectedIDCMP) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.ReturnIdQueue.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.InputSignalTask.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.InputSignalMask) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.NativeWindow.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.InputHandlers.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.InputHandlerGeneration) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.InputTimerPort.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.WindowEventHandlers.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.WindowEventHandlerGeneration) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.PendingReleaseCount) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.PendingReleaseDepth) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.PendingReleaseParent.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.ApplicationPushQueue.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.ApplicationPushGeneration) &&
		MuiGuestStructCursor.IsComplete(cursor);
}
