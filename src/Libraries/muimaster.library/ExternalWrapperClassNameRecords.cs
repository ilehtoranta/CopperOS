/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Exact fixed class-id records used by the external-wrapper family. These are
// guest C strings with a fixed wire length; named packed values keep the ABI
// boundary explicit and avoid consumer-side byte walks.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiExternalWrapperBoopsiClassNameRecord
{
	internal const uint Size = 11;

	internal uint Word0;
	internal uint Word1;
	internal byte Character0;
	internal byte Character1;
	internal byte Terminator;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalWrapperDtpicClassNameRecord
{
	internal const uint Size = 10;

	internal uint Word0;
	internal uint Word1;
	internal byte Character;
	internal byte Terminator;
}

internal static class MuiExternalWrapperBoopsiClassNameRecordCodec
{
	internal static bool TryMatch<TPlatform>(ref TPlatform platform, APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadRecord(ref platform, address, out var value)) return false;
		return value.Word0 == 0x426F6F70u && value.Word1 == 0x73692E6Du &&
			value.Character0 == (byte)'u' && value.Character1 == (byte)'i' &&
			value.Terminator == 0;
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiExternalWrapperBoopsiClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalWrapperBoopsiClassNameRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word1) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var character0) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var character1) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var terminator)) return false;
		value.Character0 = character0;
		value.Character1 = character1;
		value.Terminator = terminator;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiExternalWrapperBoopsiClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalWrapperBoopsiClassNameRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word0) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word1) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Character0) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Character1) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Terminator) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiExternalWrapperBoopsiClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiExternalWrapperBoopsiClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}

internal static class MuiExternalWrapperDtpicClassNameRecordCodec
{
	internal static bool TryMatch<TPlatform>(ref TPlatform platform, APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadRecord(ref platform, address, out var value)) return false;
		return value.Word0 == 0x44747069u && value.Word1 == 0x632E6D75u &&
			value.Character == (byte)'i' && value.Terminator == 0;
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiExternalWrapperDtpicClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalWrapperDtpicClassNameRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word1) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var character) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var terminator)) return false;
		value.Character = character;
		value.Terminator = terminator;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiExternalWrapperDtpicClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalWrapperDtpicClassNameRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word0) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word1) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Character) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Terminator) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiExternalWrapperDtpicClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiExternalWrapperDtpicClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}
