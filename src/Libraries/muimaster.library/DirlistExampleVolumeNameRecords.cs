/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The deterministic volume-list fixture uses a fixed ten-byte guest string:
// "Exam" + "pleN" + ':' + NUL. Keep the complete wire value named so the
// producer does not own a sequence of anonymous byte positions.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDirlistExampleVolumeNameRecord
{
	internal const uint Size = 10;

	internal uint Word0;
	internal uint Word1;
	internal byte Separator;
	internal byte Terminator;

	internal static MuiDirlistExampleVolumeNameRecord Create(byte digit) =>
		new()
		{
			Word0 = 0x4578616Du,
			Word1 = 0x706C6500u | digit,
			Separator = (byte)':',
			Terminator = 0,
		};
}

internal static class MuiDirlistExampleVolumeNameRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiDirlistExampleVolumeNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiDirlistExampleVolumeNameRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var word0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var word1) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var separator) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var terminator)) return false;
		value.Word0 = word0;
		value.Word1 = word1;
		value.Separator = separator;
		value.Terminator = terminator;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiDirlistExampleVolumeNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiDirlistExampleVolumeNameRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word0) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word1) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Separator) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Terminator) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiDirlistExampleVolumeNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiDirlistExampleVolumeNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}
