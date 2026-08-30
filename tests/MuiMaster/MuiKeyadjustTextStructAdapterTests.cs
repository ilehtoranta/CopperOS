/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiKeyadjustTextStructAdapterTests
{
	[Fact]
	public void KeyadjustTextUsesNamedBytesAndCompleteBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var value = new MuiKeyadjustTextRecord
		{
			Character = (byte)'A',
			Terminator = 0,
		};

		Assert.Equal(2, Unsafe.SizeOf<MuiKeyadjustTextRecord>());
		Assert.True(MuiKeyadjustTextRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiKeyadjustTextRecordCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.Character, decoded.Character);
		Assert.Equal(value.Terminator, decoded.Terminator);

		var cursor = default(MuiKeyadjustTextFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiKeyadjustTextField.Terminator;
		Assert.True(MuiKeyadjustTextRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out var terminatorAddress));
		Assert.Equal(address.Raw + 1, terminatorAddress.Raw);
		cursor.Field = (MuiKeyadjustTextField)255;
		Assert.False(MuiKeyadjustTextRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out _));
		Assert.False(MuiKeyadjustTextRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFF), out _));
	}

	[Fact]
	public void KeyadjustTextSequentialRecordPreservesBytesAndRejectsTruncation()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			state);
		var address = APTR.FromPointer(0x3600);
		var expected = new MuiKeyadjustTextRecord
		{
			Character = 0xFF,
			Terminator = 0,
		};
		Assert.True(MuiKeyadjustTextRecordCodec.WriteRecord(ref platform,
			address, expected));
		Assert.True(MuiKeyadjustTextRecordCodec.TryReadRecord(ref platform,
			address, out var actual));
		Assert.Equal(expected.Character, actual.Character);
		Assert.Equal(expected.Terminator, actual.Terminator);
		Assert.False(MuiKeyadjustTextRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FFF), out _));
	}
}
