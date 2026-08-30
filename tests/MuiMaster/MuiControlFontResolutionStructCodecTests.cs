/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiControlFontResolutionStructCodecTests
{
	[Fact]
	public void ControlFontResolutionRoundTripsThroughSequentialStruct()
	{
		Assert.Equal(20, Unsafe.SizeOf<MuiControlFontResolutionRecord>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2800);
		var expected = new MuiControlFontResolutionRecord
		{
			Magic = MuiControlFontResolutionRecord.Cookie,
			Present = 1,
			Inherited = 1,
			Depth = 0xFEDCBA98u,
			Font = APTR.FromPointer(0x81234567u),
		};

		Assert.True(MuiControlFontResolutionRecordStructCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiControlFontResolutionRecordStructCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Present, actual.Present);
		Assert.Equal(expected.Inherited, actual.Inherited);
		Assert.Equal(expected.Depth, actual.Depth);
		Assert.Equal(expected.Font, actual.Font);
		Assert.True(MuiControlFontResolutionRecordCodec.TryRead(ref platform,
			address, out var validated));
		Assert.Equal(expected.Font, validated.Font);
	}

	[Fact]
	public void ControlFontResolutionStructCodecRejectsNullAndIncompleteRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var value = new MuiControlFontResolutionRecord
		{
			Magic = MuiControlFontResolutionRecord.Cookie,
			Present = 1,
			Inherited = 0,
			Depth = 0,
			Font = APTR.Null,
		};
		var crossing = APTR.FromPointer(0x30FF1);

		Assert.False(MuiControlFontResolutionRecordStructCodec.Write(ref platform,
			APTR.Null, value));
		Assert.False(MuiControlFontResolutionRecordStructCodec.TryRead(ref platform,
			APTR.Null, out _));
		Assert.False(MuiControlFontResolutionRecordStructCodec.Write(ref platform,
			crossing, value));
		Assert.False(MuiControlFontResolutionRecordStructCodec.TryRead(ref platform,
			crossing, out _));
	}
}
