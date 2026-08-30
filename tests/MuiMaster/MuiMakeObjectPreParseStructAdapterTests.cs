/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiMakeObjectPreParseStructAdapterTests
{
	[Fact]
	public void MakeObjectPreParseUsesNamedBytesAndCompleteBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var value = new MuiMakeObjectPreParseRecord
		{
			Escape = 0x1B,
			Command = (byte)'c',
			Terminator = 0,
			Reserved = 0,
		};

		Assert.Equal(4, Unsafe.SizeOf<MuiMakeObjectPreParseRecord>());
		Assert.True(MuiMakeObjectPreParseRecordCodec.WriteRecord(ref platform, address,
			value));
		Assert.True(MuiMakeObjectPreParseRecordCodec.TryReadRecord(ref platform, address,
			out var decoded));
		Assert.Equal(value.Escape, decoded.Escape);
		Assert.Equal(value.Command, decoded.Command);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.Equal(value.Reserved, decoded.Reserved);

		var cursor = default(MuiMakeObjectPreParseFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiMakeObjectPreParseField.Terminator;
		Assert.True(MuiMakeObjectPreParseRecordMemoryCodec.TryGetAddress(
			ref platform, cursor, out var terminatorAddress));
		Assert.Equal(address.Raw + 2, terminatorAddress.Raw);
		cursor.Field = (MuiMakeObjectPreParseField)255;
		Assert.False(MuiMakeObjectPreParseRecordMemoryCodec.TryGetAddress(
			ref platform, cursor, out _));
		Assert.False(MuiMakeObjectPreParseRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x20FFD), out _));
	}
}
