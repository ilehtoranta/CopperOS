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

// Struct-first guest-memory adapter for event state. The named record keeps
// the close BOOL and opaque event/object capabilities semantic; this bounded
// adapter owns only their fixed four-byte guest representation.
internal static class MuiWindowEventStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiWindowEventStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiWindowEventStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, 4);
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
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiWindowEventStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiWindowEventStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out var magic) ||
			!MuiWindowEventStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 4, out value.CloseRequest) ||
			!MuiWindowEventStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 8, out var inputEvent) ||
			!MuiWindowEventStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 12, out var mouseObject)) return false;
		value.Magic = magic;
		value.InputEvent = APTR.FromPointer(inputEvent);
		value.MouseObject = APTR.FromPointer(mouseObject);
		return true;
	}

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
		return MuiWindowEventStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiWindowEventStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 4, value.CloseRequest) &&
			MuiWindowEventStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 8, value.InputEvent.Raw) &&
			MuiWindowEventStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 12, value.MouseObject.Raw);
	}
}
