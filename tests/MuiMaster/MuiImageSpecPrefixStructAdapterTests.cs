/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiImageSpecPrefixStructAdapterTests
{
	[Fact]
	public void ImageSpecPrefixUsesNamedRecord()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		platform.WriteUInt8(address, 0, (byte)'2');
		platform.WriteUInt8(address, 1, (byte)':');

		Assert.Equal(2, Unsafe.SizeOf<MuiImageSpecPrefixRecord>());
		Assert.True(MuiImageSpecPrefixRecordCodec.TryRead(ref platform, address,
			out var value));
		Assert.Equal((byte)'2', value.Kind);
		Assert.Equal((byte)':', value.Separator);
		Assert.False(MuiImageSpecPrefixRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFF), out _));
	}

	[Fact]
	public void ImageSpecPrefixSequentialRecordRoundTripsAndRejectsTruncation()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var expected = new MuiImageSpecPrefixRecord
		{
			Kind = (byte)'s',
			Separator = (byte)':',
		};
		Assert.True(MuiImageSpecPrefixRecordCodec.WriteRecord(ref platform,
			address, expected));
		Assert.True(MuiImageSpecPrefixRecordCodec.TryReadRecord(ref platform,
			address, out var actual));
		Assert.Equal(expected.Kind, actual.Kind);
		Assert.Equal(expected.Separator, actual.Separator);
		Assert.False(MuiImageSpecPrefixRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x20FFF), out _));
	}
}
