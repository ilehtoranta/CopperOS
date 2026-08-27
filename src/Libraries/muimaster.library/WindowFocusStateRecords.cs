/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Window keyboard-focus projections.  ActiveObject and DefaultObject are
// caller-owned MUI object capabilities; the record keeps the validated guest
// pointers together without a managed object mirror.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowFocusStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x57464F53u; // 'WFOS'

	internal uint Magic;
	internal APTR ActiveObject;
	internal APTR DefaultObject;
}

// Focus pointers are opaque guest object capabilities in the ABI record. The
// structural codec checks only their mapped representation; the live predicate
// below adds the object-registry check needed by Window consumers.
internal static class MuiWindowFocusStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiWindowFocusStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiWindowFocusStateRecord.Cookie &&
		IsMapped(ref platform, value.ActiveObject) &&
		IsMapped(ref platform, value.DefaultObject);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, MuiWindowFocusStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) && !owner.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, owner).IsNull &&
		IsLive(ref platform, state, value.ActiveObject) &&
		IsLive(ref platform, state, value.DefaultObject);

	private static bool IsMapped<TPlatform>(ref TPlatform platform, APTR value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.IsNull || platform.IsMapped(value, 1);

	private static bool IsLive<TPlatform>(ref TPlatform platform, APTR state,
		APTR value) where TPlatform : struct, IMuiHeadlessPlatform =>
		value.IsNull || !MuiHeadlessObjectCore.FindObject(ref platform, state,
			value).IsNull;
}

internal enum MuiWindowFocusStateField : byte
{
	Magic,
	ActiveObject,
	DefaultObject,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowFocusStateFieldCursor
{
	internal APTR Record;
	internal MuiWindowFocusStateField Field;
}

internal static class MuiWindowFocusStateFieldCursorCodec
{
	private static bool TryResolve(MuiWindowFocusStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiWindowFocusStateField.Magic:
			case MuiWindowFocusStateField.ActiveObject:
			case MuiWindowFocusStateField.DefaultObject:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowFocusStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiWindowFocusStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowFocusStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiWindowFocusStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowFocusStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiWindowFocusStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter for the pointer-bearing focus record.
// Object capabilities stay APTR fields in the semantic struct; only this
// bounded adapter handles their four-byte guest representation.
internal static class MuiWindowFocusStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiWindowFocusStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiWindowFocusStateRecord.Size)) return false;
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

internal static class MuiWindowFocusStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiWindowFocusStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiWindowFocusStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out var magic) ||
			!MuiWindowFocusStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 4, out var active) ||
			!MuiWindowFocusStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 8, out var @default))
			return false;
		value.Magic = magic;
		value.ActiveObject = APTR.FromPointer(active);
		value.DefaultObject = APTR.FromPointer(@default);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowFocusStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiWindowFocusStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowFocusStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowFocusStateAdmission.Validate(ref platform, value)) return false;
		return MuiWindowFocusStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiWindowFocusStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 4, value.ActiveObject.Raw) &&
			MuiWindowFocusStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 8, value.DefaultObject.Raw);
	}
}
