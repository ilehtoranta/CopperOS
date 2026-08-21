/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Window event-facing state shared by native polling and getter-only pointer
// publication. InputEvent and MouseObject remain caller/object pointers; the
// record stores only validated guest addresses and the canonical close BOOL.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowEventStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x57455654u; // 'WEVT'

	internal uint Magic;
	internal uint CloseRequest;
	internal APTR InputEvent;
	internal APTR MouseObject;
}

internal enum MuiWindowEventStateField : byte
{
	Magic,
	CloseRequest,
	InputEvent,
	MouseObject,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowEventStateFieldCursor
{
	internal APTR Record;
	internal MuiWindowEventStateField Field;
}

internal static class MuiWindowEventStateFieldCursorCodec
{
	private static bool TryResolve(MuiWindowEventStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiWindowEventStateField.Magic:
			case MuiWindowEventStateField.CloseRequest:
			case MuiWindowEventStateField.InputEvent:
			case MuiWindowEventStateField.MouseObject:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowEventStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiWindowEventStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowEventStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiWindowEventStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowEventStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiWindowEventStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiWindowEventStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowEventStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiWindowEventStateRecord.Size) ||
			!MuiWindowEventStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowEventStateField.Magic, out var magic) ||
			magic != MuiWindowEventStateRecord.Cookie ||
			!MuiWindowEventStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowEventStateField.CloseRequest,
				out value.CloseRequest) ||
			!MuiWindowEventStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowEventStateField.InputEvent, out var inputEvent) ||
			!MuiWindowEventStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowEventStateField.MouseObject,
				out var mouseObject)) return false;
		value.Magic = magic;
		value.InputEvent = APTR.FromPointer(inputEvent);
		value.MouseObject = APTR.FromPointer(mouseObject);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowEventStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiWindowEventStateRecord.Size) || value.Magic !=
			MuiWindowEventStateRecord.Cookie) return false;
		return MuiWindowEventStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiWindowEventStateField.Magic, value.Magic) &&
			MuiWindowEventStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiWindowEventStateField.CloseRequest,
			value.CloseRequest) &&
			MuiWindowEventStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiWindowEventStateField.InputEvent,
			value.InputEvent.Raw) &&
			MuiWindowEventStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiWindowEventStateField.MouseObject,
			value.MouseObject.Raw);
	}
}
