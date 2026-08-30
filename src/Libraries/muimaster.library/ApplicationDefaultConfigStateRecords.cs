/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// DefaultConfigItem result state. The platform supplies the value; the guest
// record retains the requested ID, accepted value, and saturating request
// counter without introducing a managed configuration store.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationDefaultConfigStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ConfigIdOffset = 4;
	internal const uint ValueOffset = 8;
	internal const uint RequestsOffset = 12;
	internal const uint Cookie = 0x41444354u; // 'ADCT'

	internal uint Magic;
	internal uint ConfigId;
	internal uint Value;
	internal uint Requests;
}

// DefaultConfigItem returns an opaque ULONG value for an opaque config ID.
// Admission therefore owns the record cookie and live Application capability;
// the ID/value/counter retain their complete MorphOS ULONG representation.
internal static class MuiApplicationDefaultConfigStateAdmission
{
	internal static bool Validate(MuiApplicationDefaultConfigStateRecord value) =>
		value.Magic == MuiApplicationDefaultConfigStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, MuiApplicationDefaultConfigStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull;
}

internal enum MuiApplicationDefaultConfigStateField : byte
{
	Magic,
	ConfigId,
	Value,
	Requests,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationDefaultConfigStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationDefaultConfigStateField Field;
}

internal static class MuiApplicationDefaultConfigStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationDefaultConfigStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationDefaultConfigStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationDefaultConfigStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationDefaultConfigStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationDefaultConfigStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationDefaultConfigStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Fixed DefaultConfigItem result state is read and written as a named value.
// Keep packed guest positions in this small ABI adapter; production consumers
// do not select fields through the compatibility cursor.
internal static class MuiApplicationDefaultConfigStateRecordMemoryCodec
{
	private static bool TryResolve(MuiApplicationDefaultConfigStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationDefaultConfigStateField.Magic:
				offset = MuiApplicationDefaultConfigStateRecord.MagicOffset;
				return true;
			case MuiApplicationDefaultConfigStateField.ConfigId:
				offset = MuiApplicationDefaultConfigStateRecord.ConfigIdOffset;
				return true;
			case MuiApplicationDefaultConfigStateField.Value:
				offset = MuiApplicationDefaultConfigStateRecord.ValueOffset;
				return true;
			case MuiApplicationDefaultConfigStateField.Requests:
				offset = MuiApplicationDefaultConfigStateRecord.RequestsOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationDefaultConfigStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record,
			MuiApplicationDefaultConfigStateRecord.Size) &&
			platform.IsMapped(address,
				MuiApplicationDefaultConfigStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationDefaultConfigStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationDefaultConfigStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationDefaultConfigStateRecordCodec
{
	// DefaultConfigItem state is a fixed four-ULONG record. Exchange the
	// declaration-ordered fields as one named struct so callers do not rebuild
	// private offsets; the field adapter remains available for corruption tests.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationDefaultConfigStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationDefaultConfigStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.ConfigId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Value) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Requests) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationDefaultConfigStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationDefaultConfigStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ConfigId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Value) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Requests) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationDefaultConfigStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationDefaultConfigStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationDefaultConfigStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationDefaultConfigStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationDefaultConfigStateRecord.Size) ||
			!MuiApplicationDefaultConfigStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}
