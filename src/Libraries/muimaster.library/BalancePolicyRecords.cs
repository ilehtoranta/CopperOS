/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Balance.mui exposes one initializer/getter policy flag.  Keep the value in
// a fixed-width guest record so construction, generic Get, and persistence
// synchronization share one typed representation.
public struct MuiBalancePolicyState
{
	public uint Quiet;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBalancePolicyStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x42414C4Eu; // 'BALN'

	internal uint Magic;
	internal uint Quiet;
}

internal enum MuiBalancePolicyStateField : byte
{
	Magic,
	Quiet,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBalancePolicyStateFieldCursor
{
	internal APTR Record;
	internal MuiBalancePolicyStateField Field;
}

internal static class MuiBalancePolicyStateFieldCursorCodec
{
	private static bool TryResolve(MuiBalancePolicyStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiBalancePolicyStateField.Magic:
				offset = 0;
				return true;
			case MuiBalancePolicyStateField.Quiet:
				offset = 4;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBalancePolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiBalancePolicyStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBalancePolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiBalancePolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBalancePolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiBalancePolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiBalancePolicyStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiBalancePolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiBalancePolicyStateRecord.Size) ||
			!MuiBalancePolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiBalancePolicyStateField.Magic, out var magic) ||
			magic != MuiBalancePolicyStateRecord.Cookie ||
			!MuiBalancePolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiBalancePolicyStateField.Quiet, out var quiet))
			return false;
		value.Magic = magic;
		value.Quiet = quiet;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiBalancePolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiBalancePolicyStateRecord.Size) || value.Magic !=
			MuiBalancePolicyStateRecord.Cookie) return false;
		return MuiBalancePolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBalancePolicyStateField.Magic, value.Magic) &&
			MuiBalancePolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBalancePolicyStateField.Quiet, value.Quiet);
	}
}

// Keep the Balance policy wire field lossless for malformed-state diagnostics,
// but admit only the canonical MorphOS BOOL representation.
internal static class MuiBalancePolicyStateValidation
{
	internal static bool IsValidRecord(MuiBalancePolicyStateRecord value) =>
		value.Quiet <= 1;

	internal static bool IsValidState(MuiBalancePolicyState value) =>
		value.Quiet <= 1;
}
