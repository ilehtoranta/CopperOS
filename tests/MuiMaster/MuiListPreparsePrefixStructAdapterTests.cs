/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListPreparsePrefixStructAdapterTests
{
	[Fact]
	public void ListPreparsePrefixUsesNamedVariableLengthRecord()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		platform.WriteUInt8(address, 0, (byte)'\\');
		platform.WriteUInt8(address, 1, (byte)'3');
		platform.WriteUInt8(address, 2, (byte)'3');

		Assert.Equal(3, Unsafe.SizeOf<MuiListPreparsePrefixRecord>());
		Assert.True(MuiListPreparsePrefixRecordCodec.TryRead(ref platform,
			address, 2, out var shortValue));
		Assert.Equal((byte)'\\', shortValue.First);
		Assert.Equal((byte)'3', shortValue.Second);
		Assert.Equal(0, shortValue.Third);
		Assert.True(MuiListPreparsePrefixRecordCodec.TryRead(ref platform,
			address, 3, out var value));
		Assert.Equal((byte)'3', value.Third);
		Assert.False(MuiListPreparsePrefixRecordCodec.TryRead(ref platform,
			address, 1, out _));
		Assert.False(MuiListPreparsePrefixRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFE), 3, out _));
	}
}
