/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringInteger64StructCodecTests
{
	[Fact]
	public void QuadRoundTripsThroughSequentialHighLowStruct()
	{
		Assert.Equal(8, Unsafe.SizeOf<MuiStringInteger64Value>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2400);
		var expected = default(MuiStringInteger64Value);
		expected.High = 0x80000000u;
		expected.Low = 0x00000001u;

		Assert.True(MuiStringInteger64ValueStructCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiStringInteger64ValueStructCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.High, actual.High);
		Assert.Equal(expected.Low, actual.Low);
	}

	[Fact]
	public void QuadStructCodecRejectsNullAndIncompleteGuestRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var value = default(MuiStringInteger64Value);
		value.High = 0xFFFFFFFFu;
		value.Low = 0xFFFFFFFFu;
		var crossing = APTR.FromPointer(0x20FFC);

		Assert.False(MuiStringInteger64ValueStructCodec.Write(ref platform,
			APTR.Null, value));
		Assert.False(MuiStringInteger64ValueStructCodec.TryRead(ref platform,
			APTR.Null, out _));
		Assert.False(MuiStringInteger64ValueStructCodec.Write(ref platform,
			crossing, value));
		Assert.False(MuiStringInteger64ValueStructCodec.TryRead(ref platform,
			crossing, out _));
	}
}
