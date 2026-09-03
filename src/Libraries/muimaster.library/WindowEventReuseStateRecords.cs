/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident event context for one open MUI Window.  The context is
// populated only while DispatchWindowEvent is routing a typed HandleEvent
// packet.  A String EditHook can then request reuse without retaining a
// managed callback or guessing the packet layout.  Pending is drained after
// the current dispatch returns, so reuse never recurses through a handler.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowEventReuseStateRecord
{
	internal const uint Size = 28;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ContextActiveOffset = 4;
	internal const uint PendingOffset = 8;
	internal const uint EventMessageOffset = 12;
	internal const uint InputEventOffset = 16;
	internal const uint EventClassOffset = 20;
	internal const uint MuiKeyOffset = 24;
	internal const uint Cookie = 0x57525355u; // 'WRSU'

	internal uint Magic;
	internal uint ContextActive;
	internal uint Pending;
	internal APTR EventMessage;
	internal APTR InputEvent;
	internal uint EventClass;
	internal int MuiKey;
}

// Reuse state is a guest-resident dispatch context, not a managed queue. The
// structural predicate admits canonical context flags and a mapped HandleEvent
// packet. InputEvent is retained as an opaque caller/native message capability
// because MorphOS's HandleEvent packet may carry an address outside the MUI
// guest-memory window; the live predicate additionally ties the state to a
// live Window.
internal static class MuiWindowEventReuseStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiWindowEventReuseStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiWindowEventReuseStateRecord.Cookie &&
		value.ContextActive <= 1 && value.Pending <= 1 &&
		IsMapped(ref platform, value.EventMessage,
			MuiCommonHandleEventMessage.Size) &&
		(value.ContextActive == 0 || value.EventClass != 0) &&
		(value.Pending == 0 ||
			(value.EventMessage.IsNotNull && value.EventClass != 0));

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, MuiWindowEventReuseStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) && !owner.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, owner).IsNull;

	private static bool IsMapped<TPlatform>(ref TPlatform platform, APTR value,
		uint size) where TPlatform : struct, IMuiGuestMemory =>
		value.IsNull || platform.IsMapped(value, size);
}

internal enum MuiWindowEventReuseStateField : byte
{
	Magic,
	ContextActive,
	Pending,
	EventMessage,
	InputEvent,
	EventClass,
	MuiKey,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowEventReuseStateFieldCursor
{
	internal APTR Record;
	internal MuiWindowEventReuseStateField Field;
}

internal static class MuiWindowEventReuseStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowEventReuseStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiWindowEventReuseStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowEventReuseStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiWindowEventReuseStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowEventReuseStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiWindowEventReuseStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. Dispatch context and reuse state stay
// named fields; the bounded adapter is the sole fixed-layout translation for
// event-message/input capabilities and the signed key value.
internal static class MuiWindowEventReuseStateRecordMemoryCodec
{
	private static bool TryResolve(MuiWindowEventReuseStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiWindowEventReuseStateField.Magic:
				offset = MuiWindowEventReuseStateRecord.MagicOffset;
				return true;
			case MuiWindowEventReuseStateField.ContextActive:
				offset = MuiWindowEventReuseStateRecord.ContextActiveOffset;
				return true;
			case MuiWindowEventReuseStateField.Pending:
				offset = MuiWindowEventReuseStateRecord.PendingOffset;
				return true;
			case MuiWindowEventReuseStateField.EventMessage:
				offset = MuiWindowEventReuseStateRecord.EventMessageOffset;
				return true;
			case MuiWindowEventReuseStateField.InputEvent:
				offset = MuiWindowEventReuseStateRecord.InputEventOffset;
				return true;
			case MuiWindowEventReuseStateField.EventClass:
				offset = MuiWindowEventReuseStateRecord.EventClassOffset;
				return true;
			case MuiWindowEventReuseStateField.MuiKey:
				offset = MuiWindowEventReuseStateRecord.MuiKeyOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowEventReuseStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiWindowEventReuseStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiWindowEventReuseStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowEventReuseStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiWindowEventReuseStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowEventReuseStateField.Magic)
			value = state.Magic;
		else if (field == MuiWindowEventReuseStateField.ContextActive)
			value = state.ContextActive;
		else if (field == MuiWindowEventReuseStateField.Pending)
			value = state.Pending;
		else if (field == MuiWindowEventReuseStateField.EventMessage)
			value = state.EventMessage.Raw;
		else if (field == MuiWindowEventReuseStateField.InputEvent)
			value = state.InputEvent.Raw;
		else if (field == MuiWindowEventReuseStateField.EventClass)
			value = state.EventClass;
		else if (field == MuiWindowEventReuseStateField.MuiKey)
			value = unchecked((uint)state.MuiKey);
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowEventReuseStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowEventReuseStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowEventReuseStateField.Magic)
			state.Magic = value;
		else if (field == MuiWindowEventReuseStateField.ContextActive)
			state.ContextActive = value;
		else if (field == MuiWindowEventReuseStateField.Pending)
			state.Pending = value;
		else if (field == MuiWindowEventReuseStateField.EventMessage)
			state.EventMessage = APTR.FromPointer(value);
		else if (field == MuiWindowEventReuseStateField.InputEvent)
			state.InputEvent = APTR.FromPointer(value);
		else if (field == MuiWindowEventReuseStateField.EventClass)
			state.EventClass = value;
		else if (field == MuiWindowEventReuseStateField.MuiKey)
			state.MuiKey = unchecked((int)value);
		else return false;
		return MuiWindowEventReuseStateRecordCodec.WriteStructural(ref platform,
			record, state);
	}
}

internal static class MuiWindowEventReuseStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiWindowEventReuseStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWindowEventReuseStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.ContextActive) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Pending) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var eventMessage) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var inputEvent) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.EventClass) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var muiKey)) return false;
		value.EventMessage = APTR.FromPointer(eventMessage);
		value.InputEvent = APTR.FromPointer(inputEvent);
		value.MuiKey = unchecked((int)muiKey);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowEventReuseStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowEventReuseStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWindowEventReuseStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ContextActive) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Pending) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.EventMessage.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.InputEvent.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.EventClass) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.MuiKey)) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiWindowEventReuseStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowEventReuseStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiWindowEventReuseStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowEventReuseStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiWindowEventReuseStateAdmission.Validate(ref platform,
			value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
