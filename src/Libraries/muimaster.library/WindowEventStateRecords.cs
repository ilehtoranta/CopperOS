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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint CloseRequestOffset = 4;
	internal const uint InputEventOffset = 8;
	internal const uint MouseObjectOffset = 12;
	internal const uint Cookie = 0x57455654u; // 'WEVT'

	internal uint Magic;
	internal uint CloseRequest;
	internal APTR InputEvent;
	internal APTR MouseObject;
}

// Event state carries one canonical BOOL and two caller/object capabilities.
// The structural codec admits only a mapped guest representation; consumers
// add the object-registry and owner checks through ValidateLive.
internal static class MuiWindowEventStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiWindowEventStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiWindowEventStateRecord.Cookie &&
		value.CloseRequest <= 1 &&
		IsMapped(ref platform, value.InputEvent,
			global::Amiga.InputEvent.Size) &&
		IsMapped(ref platform, value.MouseObject, 1);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, MuiWindowEventStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) && !owner.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, owner).IsNull &&
		(value.MouseObject.IsNull || (value.MouseObject != owner &&
			!MuiHeadlessObjectCore.FindObject(ref platform, state,
				value.MouseObject).IsNull));

	private static bool IsMapped<TPlatform>(ref TPlatform platform, APTR value,
		uint size) where TPlatform : struct, IMuiGuestMemory =>
		value.IsNull || platform.IsMapped(value, size);
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
	private static bool TryResolveFieldIndex(MuiWindowEventStateField field,
		out uint index)
	{
		index = field switch
		{
			MuiWindowEventStateField.Magic => 0,
			MuiWindowEventStateField.CloseRequest => 1,
			MuiWindowEventStateField.InputEvent => 2,
			MuiWindowEventStateField.MouseObject => 3,
			_ => uint.MaxValue,
		};
		return index != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowEventStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowEventStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiWindowEventStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiWindowEventStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiWindowEventStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowEventStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiWindowEventStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowEventStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiWindowEventStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter for event state. The named record keeps
// the close BOOL and opaque event/object capabilities semantic; this bounded
// adapter owns only their fixed four-byte guest representation.
internal static class MuiWindowEventStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiWindowEventStateField field,
		out uint index)
	{
		index = field switch
		{
			MuiWindowEventStateField.Magic => 0,
			MuiWindowEventStateField.CloseRequest => 1,
			MuiWindowEventStateField.InputEvent => 2,
			MuiWindowEventStateField.MouseObject => 3,
			_ => uint.MaxValue,
		};
		return index != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowEventStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiWindowEventStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowEventStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiWindowEventStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiWindowEventStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiWindowEventStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowEventStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiWindowEventStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowEventStateField.Magic)
			value = state.Magic;
		else if (field == MuiWindowEventStateField.CloseRequest)
			value = state.CloseRequest;
		else if (field == MuiWindowEventStateField.InputEvent)
			value = state.InputEvent.Raw;
		else if (field == MuiWindowEventStateField.MouseObject)
			value = state.MouseObject.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowEventStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowEventStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowEventStateField.Magic)
			state.Magic = value;
		else if (field == MuiWindowEventStateField.CloseRequest)
			state.CloseRequest = value;
		else if (field == MuiWindowEventStateField.InputEvent)
			state.InputEvent = APTR.FromPointer(value);
		else if (field == MuiWindowEventStateField.MouseObject)
			state.MouseObject = APTR.FromPointer(value);
		else return false;
		return MuiWindowEventStateRecordCodec.WriteStructural(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiWindowEventStateRecord.Size -
			MuiWindowEventStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiWindowEventStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiWindowEventStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiWindowEventStateRecordCodec
{
	// Sequential named-struct path used by Window event dispatch. Cookie, the
	// close BOOL, and two opaque capabilities are exchanged in declaration
	// order; numeric positions remain confined to the compatibility adapter.
	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowEventStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowEventStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWindowEventStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.CloseRequest) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.InputEvent.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.MouseObject.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiWindowEventStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWindowEventStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var closeRequest) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var inputEvent) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var mouseObject) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Magic = magic;
		value.CloseRequest = closeRequest;
		value.InputEvent = APTR.FromPointer(inputEvent);
		value.MouseObject = APTR.FromPointer(mouseObject);
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiWindowEventStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowEventStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiWindowEventStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowEventStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowEventStateAdmission.Validate(ref platform, value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}
