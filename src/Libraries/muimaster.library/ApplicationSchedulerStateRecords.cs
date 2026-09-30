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
					node.Value, out var handler) || node.Packet != handler.Method)
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
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiApplicationSchedulerStateField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiApplicationSchedulerStateField.Magic:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationSchedulerStateRecord.FieldSize, out address);
			case MuiApplicationSchedulerStateField.ReturnHead:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationSchedulerStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationSchedulerStateRecord.FieldSize, out address);
			case MuiApplicationSchedulerStateField.ReturnTail:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationSchedulerStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationSchedulerStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationSchedulerStateRecord.FieldSize, out address);
			case MuiApplicationSchedulerStateField.InputHandlers:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationSchedulerStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationSchedulerStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationSchedulerStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationSchedulerStateRecord.FieldSize, out address);
			case MuiApplicationSchedulerStateField.SignalMask:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationSchedulerStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationSchedulerStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationSchedulerStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationSchedulerStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationSchedulerStateRecord.FieldSize, out address);
			case MuiApplicationSchedulerStateField.PushHead:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationSchedulerStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationSchedulerStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationSchedulerStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationSchedulerStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationSchedulerStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationSchedulerStateRecord.FieldSize, out address);
			case MuiApplicationSchedulerStateField.PushTail:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationSchedulerStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationSchedulerStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationSchedulerStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationSchedulerStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationSchedulerStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationSchedulerStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationSchedulerStateRecord.FieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSchedulerStateField field, out APTR address)
	where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, record,
			MuiApplicationSchedulerStateRecord.Size, out var cursor) ||
			!TryTakeField(ref platform, ref cursor, field, out address)) return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSchedulerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiApplicationSchedulerStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiApplicationSchedulerStateField.Magic)
			value = state.Magic;
		else if (field == MuiApplicationSchedulerStateField.ReturnHead)
			value = state.ReturnHead.Raw;
		else if (field == MuiApplicationSchedulerStateField.ReturnTail)
			value = state.ReturnTail.Raw;
		else if (field == MuiApplicationSchedulerStateField.InputHandlers)
			value = state.InputHandlers.Raw;
		else if (field == MuiApplicationSchedulerStateField.SignalMask)
			value = state.SignalMask;
		else if (field == MuiApplicationSchedulerStateField.PushHead)
			value = state.PushHead.Raw;
		else if (field == MuiApplicationSchedulerStateField.PushTail)
			value = state.PushTail.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSchedulerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiApplicationSchedulerStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiApplicationSchedulerStateField.Magic)
			state.Magic = value;
		else if (field == MuiApplicationSchedulerStateField.ReturnHead)
			state.ReturnHead = APTR.FromPointer(value);
		else if (field == MuiApplicationSchedulerStateField.ReturnTail)
			state.ReturnTail = APTR.FromPointer(value);
		else if (field == MuiApplicationSchedulerStateField.InputHandlers)
			state.InputHandlers = APTR.FromPointer(value);
		else if (field == MuiApplicationSchedulerStateField.SignalMask)
			state.SignalMask = value;
		else if (field == MuiApplicationSchedulerStateField.PushHead)
			state.PushHead = APTR.FromPointer(value);
		else if (field == MuiApplicationSchedulerStateField.PushTail)
			state.PushTail = APTR.FromPointer(value);
		else return false;
		return MuiApplicationSchedulerStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}
}

internal static class MuiApplicationSchedulerStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationSchedulerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationSchedulerStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var returnHead) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var returnTail) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var inputHandlers) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.SignalMask) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var pushHead) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var pushTail)) return false;
		value.ReturnHead = APTR.FromPointer(returnHead);
		value.ReturnTail = APTR.FromPointer(returnTail);
		value.InputHandlers = APTR.FromPointer(inputHandlers);
		value.PushHead = APTR.FromPointer(pushHead);
		value.PushTail = APTR.FromPointer(pushTail);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationSchedulerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationSchedulerStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ReturnHead.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ReturnTail.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.InputHandlers.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.SignalMask) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.PushHead.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.PushTail.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationSchedulerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationSchedulerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationSchedulerStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationSchedulerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiApplicationSchedulerStateAdmission.Validate(
			ref platform, value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
