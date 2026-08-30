/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeClassNameStructAdapterTests
{
	[Fact]
	public void ListtreeClassNameUsesNamedRecord()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var value = new MuiListtreeClassNameRecord
		{
			Word0 = 0x4C697374,
			Word1 = 0x74726565,
			Word2 = 0x2E6D6363,
			Terminator = 0,
		};

		Assert.Equal(13, Unsafe.SizeOf<MuiListtreeClassNameRecord>());
		Assert.True(MuiListtreeClassNameRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiListtreeClassNameRecordCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.False(MuiListtreeClassNameRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FF4), out _));
	}

	[Fact]
	public void ListtreeClassNameSequentialRecordPreservesMixedWidthBytesAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiListtreeClassNameRecord
		{
			Word0 = 0xFFFFFFFFu,
			Word1 = 0xCAFEBABEu,
			Word2 = 0x01020304u,
			Terminator = 0xA5,
		};

		Assert.True(MuiListtreeClassNameRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiListtreeClassNameRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Terminator, decoded.Terminator);

		var crossingEnd = APTR.FromPointer(0x20FFF);
		Assert.False(MuiListtreeClassNameRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiListtreeClassNameRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
