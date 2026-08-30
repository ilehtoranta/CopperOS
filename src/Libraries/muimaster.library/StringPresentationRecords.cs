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
	private static bool TryResolve(MuiStringPresentationStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiStringPresentationStateField.Magic => 0,
			MuiStringPresentationStateField.MaxLen => 4,
			MuiStringPresentationStateField.Secret => 8,
			MuiStringPresentationStateField.Format => 12,
			MuiStringPresentationStateField.Unicode => 16,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringPresentationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiStringPresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiStringPresentationStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringPresentationStateRecord.Size)) return false;
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
		MuiGuestStructCursor.TryCreate(ref platform, address,
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
