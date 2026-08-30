/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiScrollgroupClassNameStructAdapterTests
{
	[Fact]
	public void ScrollgroupClassNameUsesNamedRecord()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var value = new MuiScrollgroupClassNameRecord
		{
			Word0 = 0x5363726F,
			Word1 = 0x6C6C6772,
			Word2 = 0x6F75702E,
			Word3 = 0x6D756900,
		};

		Assert.Equal(16, Unsafe.SizeOf<MuiScrollgroupClassNameRecord>());
		Assert.True(MuiScrollgroupClassNameRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiScrollgroupClassNameRecordCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Word3, decoded.Word3);
		Assert.False(MuiScrollgroupClassNameRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FF1), out _));
	}

	[Fact]
	public void ScrollgroupClassNameSequentialRecordPreservesWordsAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiScrollgroupClassNameRecord
		{
			Word0 = 0xFFFFFFFFu,
			Word1 = 0xCAFEBABEu,
			Word2 = 0x01020304u,
			Word3 = 0xA5A5A5A5u,
		};

		Assert.True(MuiScrollgroupClassNameRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiScrollgroupClassNameRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Word3, decoded.Word3);

		var crossingEnd = APTR.FromPointer(0x20FFF);
		Assert.False(MuiScrollgroupClassNameRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiScrollgroupClassNameRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
