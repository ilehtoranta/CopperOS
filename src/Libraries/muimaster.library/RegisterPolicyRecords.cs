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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint FrameOffset = 4;
	internal const uint TitlesOffset = 8;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiRegisterPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiRegisterPolicyStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiRegisterPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

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
	private static bool TryResolveFieldIndex(MuiRegisterPolicyStateField field,
		out uint index)
	{
		if (field == MuiRegisterPolicyStateField.Magic)
			index = 0;
		else if (field == MuiRegisterPolicyStateField.Frame)
			index = 1;
		else if (field == MuiRegisterPolicyStateField.Titles)
			index = 2;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiRegisterPolicyStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiRegisterPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiRegisterPolicyStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiRegisterPolicyStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiRegisterPolicyStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiRegisterPolicyStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRegisterPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiRegisterPolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiRegisterPolicyStateField.Magic)
			value = state.Magic;
		else if (field == MuiRegisterPolicyStateField.Frame)
			value = state.Frame;
		else if (field == MuiRegisterPolicyStateField.Titles)
			value = state.Titles.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRegisterPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiRegisterPolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiRegisterPolicyStateField.Magic)
			state.Magic = value;
		else if (field == MuiRegisterPolicyStateField.Frame)
			state.Frame = value;
		else if (field == MuiRegisterPolicyStateField.Titles)
			state.Titles = APTR.FromPointer(value);
		else return false;
		return MuiRegisterPolicyStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiRegisterPolicyStateRecord.Size -
			MuiRegisterPolicyStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiRegisterPolicyStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiRegisterPolicyStateRecord.FieldSize);
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
	// The guest record is the declaration-order tuple
	// { Magic, Frame, Titles }.  Keep production exchange cursor-based so the
	// semantic struct, rather than byte offsets, defines the ABI boundary.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiRegisterPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiRegisterPolicyStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Frame) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var titles)) return false;
		value.Titles = APTR.FromPointer(titles);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiRegisterPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiRegisterPolicyStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Frame) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Titles.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiRegisterPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

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
		return WriteRecord(ref platform, address, value);
	}
}
