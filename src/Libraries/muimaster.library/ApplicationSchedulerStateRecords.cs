/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Application scheduler state shared by ReturnID/Input, input-handler
// registration, pushed methods, and the signal wait loop. The public MUI
// attributes remain projections; queue ownership and signal selection use
// this one named guest-resident record.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationSchedulerStateRecord
{
	internal const uint Size = 28;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ReturnHeadOffset = 4;
	internal const uint ReturnTailOffset = 8;
	internal const uint InputHandlersOffset = 12;
	internal const uint SignalMaskOffset = 16;
	internal const uint PushHeadOffset = 20;
	internal const uint PushTailOffset = 24;
	internal const uint Cookie = 0x41535354u; // 'ASST'

	internal uint Magic;
	internal APTR ReturnHead;
	internal APTR ReturnTail;
	internal APTR InputHandlers;
	internal uint SignalMask;
	internal APTR PushHead;
	internal APTR PushTail;
}

// The three scheduler queues are guest-resident singly linked lists of named
// ApplicationWindow nodes. Admission keeps head/tail pairing, bounded
// topology, input-handler payloads, and pushed-method payload sizes coherent;
// the semantic codec remains separate so malformed records can be inspected
// without becoming consumable by the scheduler.
internal static class MuiApplicationSchedulerStateAdmission
{
	private const uint ReturnQueue = 0;
	private const uint InputHandlerQueue = 1;
	private const uint PushQueue = 2;

	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiApplicationSchedulerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiApplicationSchedulerStateRecord.Cookie &&
		ValidateQueue(ref platform, value.ReturnHead, value.ReturnTail,
			ReturnQueue, true) &&
		ValidateQueue(ref platform, value.InputHandlers, APTR.Null,
			InputHandlerQueue, false) &&
		ValidateQueue(ref platform, value.PushHead, value.PushTail, PushQueue,
			true);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, MuiApplicationSchedulerStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(ref platform, value) || owner.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, owner).IsNull)
			return false;
		return ValidateInputHandlerTargets(ref platform, state,
			value.InputHandlers);
	}

	private static bool ValidateQueue<TPlatform>(ref TPlatform platform,
		APTR head, APTR tail, uint kind, bool hasTail)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (hasTail && head.IsNull != tail.IsNull) return false;
		if (head.IsNull) return true;
		var current = head;
		var last = APTR.Null;
		uint visited = 0;
		while (current.IsNotNull)
		{
			if (visited++ >= MuiHeadlessLayout.MaximumTraversal ||
				!MuiApplicationWindowNodeCodec.TryRead(ref platform, current,
					out var node)) return false;
			if (kind == InputHandlerQueue)
			{
				if (node.Value.IsNull || !MuiInputHandlerCodec.TryRead(ref platform,
					node.Value, out var handler) || node.Packet != handler.Packet)
					return false;
			}
			else if (kind == PushQueue)
			{
				if (node.Value.IsNull || node.Auxiliary == 0 ||
					node.Auxiliary > 7 || node.Auxiliary >
					(uint.MaxValue / 4u) ||
					!MuiApplicationWindowNodeCodec.TryGetPayload(ref platform, current,
						node.Auxiliary * 4u, out _)) return false;
			}
			last = current;
			current = node.Next;
		}
		return !hasTail || last == tail;
	}

	private static bool ValidateInputHandlerTargets<TPlatform>(
		ref TPlatform platform, APTR state, APTR head)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var current = head;
		uint visited = 0;
		while (current.IsNotNull)
		{
			if (visited++ >= MuiHeadlessLayout.MaximumTraversal ||
				!MuiApplicationWindowNodeCodec.TryRead(ref platform, current,
					out var node) || !MuiInputHandlerCodec.TryRead(ref platform,
					node.Value, out var handler)) return false;
			if (handler.Object.IsNotNull &&
				MuiHeadlessObjectCore.FindObject(ref platform, state,
					handler.Object).IsNull) return false;
			current = node.Next;
		}
		return true;
	}
}

internal enum MuiApplicationSchedulerStateField : byte
{
	Magic,
	ReturnHead,
	ReturnTail,
	InputHandlers,
	SignalMask,
	PushHead,
	PushTail,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationSchedulerStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationSchedulerStateField Field;
}

internal static class MuiApplicationSchedulerStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationSchedulerStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationSchedulerStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSchedulerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationSchedulerStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSchedulerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationSchedulerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Struct-first guest-memory adapter. Queue heads/tails, input-handler head,
// and signal mask remain named semantic fields; bounded fixed-layout
// translation is isolated here while queue validation stays in admission.
internal static class MuiApplicationSchedulerStateRecordMemoryCodec
{
	private static bool TryResolve(MuiApplicationSchedulerStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationSchedulerStateField.Magic:
				offset = MuiApplicationSchedulerStateRecord.MagicOffset;
				return true;
			case MuiApplicationSchedulerStateField.ReturnHead:
				offset = MuiApplicationSchedulerStateRecord.ReturnHeadOffset;
				return true;
			case MuiApplicationSchedulerStateField.ReturnTail:
				offset = MuiApplicationSchedulerStateRecord.ReturnTailOffset;
				return true;
			case MuiApplicationSchedulerStateField.InputHandlers:
				offset = MuiApplicationSchedulerStateRecord.InputHandlersOffset;
				return true;
			case MuiApplicationSchedulerStateField.SignalMask:
				offset = MuiApplicationSchedulerStateRecord.SignalMaskOffset;
				return true;
			case MuiApplicationSchedulerStateField.PushHead:
				offset = MuiApplicationSchedulerStateRecord.PushHeadOffset;
				return true;
			case MuiApplicationSchedulerStateField.PushTail:
				offset = MuiApplicationSchedulerStateRecord.PushTailOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSchedulerStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiApplicationSchedulerStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address,
			MuiApplicationSchedulerStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSchedulerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSchedulerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationSchedulerStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationSchedulerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationSchedulerStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationSchedulerStateField.Magic,
			out var magic) ||
			!MuiApplicationSchedulerStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationSchedulerStateField.ReturnHead,
				out var returnHead) ||
			!MuiApplicationSchedulerStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationSchedulerStateField.ReturnTail,
				out var returnTail) ||
			!MuiApplicationSchedulerStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationSchedulerStateField.InputHandlers,
				out var inputHandlers) ||
			!MuiApplicationSchedulerStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationSchedulerStateField.SignalMask,
				out value.SignalMask) ||
			!MuiApplicationSchedulerStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationSchedulerStateField.PushHead,
				out var pushHead) ||
			!MuiApplicationSchedulerStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationSchedulerStateField.PushTail,
				out var pushTail)) return false;
		value.Magic = magic;
		value.ReturnHead = APTR.FromPointer(returnHead);
		value.ReturnTail = APTR.FromPointer(returnTail);
		value.InputHandlers = APTR.FromPointer(inputHandlers);
		value.PushHead = APTR.FromPointer(pushHead);
		value.PushTail = APTR.FromPointer(pushTail);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationSchedulerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationSchedulerStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationSchedulerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiApplicationSchedulerStateAdmission.Validate(ref platform, value))
			return false;
		return MuiApplicationSchedulerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationSchedulerStateField.Magic,
			value.Magic) &&
			MuiApplicationSchedulerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationSchedulerStateField.ReturnHead,
			value.ReturnHead.Raw) &&
			MuiApplicationSchedulerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationSchedulerStateField.ReturnTail,
			value.ReturnTail.Raw) &&
			MuiApplicationSchedulerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationSchedulerStateField.InputHandlers,
			value.InputHandlers.Raw) &&
			MuiApplicationSchedulerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationSchedulerStateField.SignalMask,
			value.SignalMask) &&
			MuiApplicationSchedulerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationSchedulerStateField.PushHead,
			value.PushHead.Raw) &&
			MuiApplicationSchedulerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationSchedulerStateField.PushTail,
			value.PushTail.Raw);
	}
}
