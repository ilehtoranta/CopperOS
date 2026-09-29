/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident String.mui presentation policy.  These initializer-oriented
// values are kept together so rendering, cursor metrics, and input encoding do
// not reconstruct policy from an undocumented private object layout.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringPresentationStateRecord
{
	internal const uint Size = 20;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint MaxLenOffset = 4;
	internal const uint SecretOffset = 8;
	internal const uint FormatOffset = 12;
	internal const uint UnicodeOffset = 16;
	internal const uint Cookie = 0x4D535052u; // 'MSPR'

	internal uint Magic;
	internal uint MaxLen;
	internal uint Secret;
	internal uint Format;
	internal uint Unicode;
}

internal static class MuiStringPresentationStateAdmission
{
	internal static bool Validate(MuiStringPresentationStateRecord value) =>
		value.Magic == MuiStringPresentationStateRecord.Cookie &&
		value.MaxLen <= 0x7FFFFFFFu && value.Secret <= 1 &&
		value.Format <= 2 && value.Unicode <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiStringPresentationStateField : byte
{
	Magic,
	MaxLen,
	Secret,
	Format,
	Unicode,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringPresentationStateFieldCursor
{
	internal APTR Record;
	internal MuiStringPresentationStateField Field;
}

internal static class MuiStringPresentationStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringPresentationStateFieldCursor cursor, out APTR address)
	where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringPresentationStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
	where TPlatform : struct, IMuiGuestMemory
		=> MuiStringPresentationStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Named presentation fields remain the
// semantic record; this bounded adapter owns fixed guest-layout translation.
internal static class MuiStringPresentationStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiStringPresentationStateField field,
		out uint index)
	{
		if (field == MuiStringPresentationStateField.Magic)
			index = 0;
		else if (field == MuiStringPresentationStateField.MaxLen)
			index = 1;
		else if (field == MuiStringPresentationStateField.Secret)
			index = 2;
		else if (field == MuiStringPresentationStateField.Format)
			index = 3;
		else if (field == MuiStringPresentationStateField.Unicode)
			index = 4;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringPresentationStateField field, out APTR address)
	where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringPresentationStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiStringPresentationStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiStringPresentationStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiStringPresentationStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiStringPresentationStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiStringPresentationStateField.Magic)
			value = state.Magic;
		else if (field == MuiStringPresentationStateField.MaxLen)
			value = state.MaxLen;
		else if (field == MuiStringPresentationStateField.Secret)
			value = state.Secret;
		else if (field == MuiStringPresentationStateField.Format)
			value = state.Format;
		else if (field == MuiStringPresentationStateField.Unicode)
			value = state.Unicode;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiStringPresentationStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiStringPresentationStateField.Magic)
			state.Magic = value;
		else if (field == MuiStringPresentationStateField.MaxLen)
			state.MaxLen = value;
		else if (field == MuiStringPresentationStateField.Secret)
			state.Secret = value;
		else if (field == MuiStringPresentationStateField.Format)
			state.Format = value;
		else if (field == MuiStringPresentationStateField.Unicode)
			state.Unicode = value;
		else return false;
		return MuiStringPresentationStateRecordCodec.WriteStructural(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiStringPresentationStateRecord.Size -
			MuiStringPresentationStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringPresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiStringPresentationStateRecord.FieldSize);
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

internal static class MuiStringPresentationStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringPresentationStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MaxLen) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Secret) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Format) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Unicode) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiStringPresentationStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiStringPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStringPresentationStateAdmission.Validate(value) &&
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiStringPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringPresentationStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.MaxLen) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Secret) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Format) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Unicode) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);
}
