/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiVirtgroupClassNameStructAdapterTests
{
	[Fact]
	public void VirtgroupClassNameUsesNamedRecord()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var value = new MuiVirtgroupClassNameRecord
		{
			Word0 = 0x56697274,
			Word1 = 0x67726F75,
			Word2 = 0x702E6D75,
			Character = (byte)'i',
			Terminator = 0,
		};

		Assert.Equal(14, Unsafe.SizeOf<MuiVirtgroupClassNameRecord>());
		Assert.True(MuiVirtgroupClassNameRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiVirtgroupClassNameRecordCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Character, decoded.Character);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.False(MuiVirtgroupClassNameRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FF3), out _));
	}

	[Fact]
	public void VirtgroupClassNameSequentialRecordPreservesMixedWidthBytesAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiVirtgroupClassNameRecord
		{
			Word0 = 0xFFFFFFFFu,
			Word1 = 0xCAFEBABEu,
			Word2 = 0x01020304u,
			Character = 0xA5,
			Terminator = 0x5A,
		};

		Assert.True(MuiVirtgroupClassNameRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiVirtgroupClassNameRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Character, decoded.Character);
		Assert.Equal(value.Terminator, decoded.Terminator);

		var crossingEnd = APTR.FromPointer(0x20FFF);
		Assert.False(MuiVirtgroupClassNameRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiVirtgroupClassNameRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
