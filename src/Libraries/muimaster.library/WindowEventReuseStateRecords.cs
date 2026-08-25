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
	internal const uint Cookie = 0x57525355u; // 'WRSU'

	internal uint Magic;
	internal uint ContextActive;
	internal uint Pending;
	internal APTR EventMessage;
	internal APTR InputEvent;
	internal uint EventClass;
	internal int MuiKey;
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
	private static bool TryResolve(MuiWindowEventReuseStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiWindowEventReuseStateField.Magic:
			case MuiWindowEventReuseStateField.ContextActive:
			case MuiWindowEventReuseStateField.Pending:
			case MuiWindowEventReuseStateField.EventMessage:
			case MuiWindowEventReuseStateField.InputEvent:
			case MuiWindowEventReuseStateField.EventClass:
			case MuiWindowEventReuseStateField.MuiKey:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowEventReuseStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiWindowEventReuseStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowEventReuseStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiWindowEventReuseStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowEventReuseStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiWindowEventReuseStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiWindowEventReuseStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowEventReuseStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiWindowEventReuseStateRecord.Size) ||
			!MuiWindowEventReuseStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowEventReuseStateField.Magic, out var magic) ||
			magic != MuiWindowEventReuseStateRecord.Cookie ||
			!MuiWindowEventReuseStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowEventReuseStateField.ContextActive,
				out value.ContextActive) ||
			!MuiWindowEventReuseStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowEventReuseStateField.Pending,
				out value.Pending) ||
			!MuiWindowEventReuseStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowEventReuseStateField.EventMessage,
				out var eventMessage) ||
			!MuiWindowEventReuseStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowEventReuseStateField.InputEvent,
				out var inputEvent) ||
			!MuiWindowEventReuseStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowEventReuseStateField.EventClass,
				out value.EventClass) ||
			!MuiWindowEventReuseStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowEventReuseStateField.MuiKey,
				out var muiKey)) return false;
		value.Magic = magic;
		value.EventMessage = APTR.FromPointer(eventMessage);
		value.InputEvent = APTR.FromPointer(inputEvent);
		value.MuiKey = unchecked((int)muiKey);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowEventReuseStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiWindowEventReuseStateRecord.Size) || value.Magic !=
			MuiWindowEventReuseStateRecord.Cookie) return false;
		return MuiWindowEventReuseStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiWindowEventReuseStateField.Magic,
			value.Magic) &&
			MuiWindowEventReuseStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiWindowEventReuseStateField.ContextActive,
				value.ContextActive) &&
			MuiWindowEventReuseStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiWindowEventReuseStateField.Pending, value.Pending) &&
			MuiWindowEventReuseStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiWindowEventReuseStateField.EventMessage,
				value.EventMessage.Raw) &&
			MuiWindowEventReuseStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiWindowEventReuseStateField.InputEvent,
				value.InputEvent.Raw) &&
			MuiWindowEventReuseStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiWindowEventReuseStateField.EventClass,
				value.EventClass) &&
			MuiWindowEventReuseStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiWindowEventReuseStateField.MuiKey,
				unchecked((uint)value.MuiKey));
	}
}
