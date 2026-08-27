/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Register.mui keeps its two class-specific construction policies together:
// Frame is a normalized BOOL and Titles is the caller-owned guest pointer to
// the title vector.  No managed array or host string is retained.
public struct MuiRegisterPolicyState
{
	public uint Frame;
	public APTR Titles;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRegisterPolicyStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x52504752u; // 'RPGR'

	internal uint Magic;
	internal uint Frame;
	internal APTR Titles;
}

internal enum MuiRegisterPolicyStateField : byte
{
	Magic,
	Frame,
	Titles,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRegisterPolicyStateFieldCursor
{
	internal APTR Record;
	internal MuiRegisterPolicyStateField Field;
}

internal static class MuiRegisterPolicyStateValidation
{
	internal static bool IsValid<TPlatform>(ref TPlatform platform,
		MuiRegisterPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiRegisterPolicyStateRecord.Cookie &&
		value.Frame <= 1 && (value.Titles.IsNull ||
			platform.IsMapped(value.Titles, 4));
}

internal static class MuiRegisterPolicyStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiRegisterPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiRegisterPolicyStateValidation.IsValid(ref platform, value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiRegisterPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal static class MuiRegisterPolicyStateFieldCursorCodec
{
	private static bool TryResolve(MuiRegisterPolicyStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiRegisterPolicyStateField.Magic:
			case MuiRegisterPolicyStateField.Frame:
			case MuiRegisterPolicyStateField.Titles:
				offset = (uint)field * 4;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiRegisterPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiRegisterPolicyStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRegisterPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiRegisterPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRegisterPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiRegisterPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Frame and Titles remain semantic named
// fields; bounded translation of their fixed guest layout lives here.
internal static class MuiRegisterPolicyStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiRegisterPolicyStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiRegisterPolicyStateRecord.Size)) return false;
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

internal static class MuiRegisterPolicyStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiRegisterPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiRegisterPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic) ||
			!MuiRegisterPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out value.Frame) ||
			!MuiRegisterPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 8, out var titles))
			return false;
		value.Titles = APTR.FromPointer(titles);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiRegisterPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiRegisterPolicyStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiRegisterPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiRegisterPolicyStateAdmission.Validate(ref platform, value))
			return false;
		return MuiRegisterPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiRegisterPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 4, value.Frame) &&
			MuiRegisterPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 8, value.Titles.Raw);
	}
}
