/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGroupClassNameStructAdapterTests
{
	[Fact]
	public void GroupClassNameUsesNamedRecord()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var value = new MuiGroupClassNameRecord
		{
			Word0 = 0x47726F75,
			Word1 = 0x702E6D75,
			Character = (byte)'i',
			Terminator = 0,
		};

		Assert.Equal(10, Unsafe.SizeOf<MuiGroupClassNameRecord>());
		Assert.True(MuiGroupClassNameRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiGroupClassNameRecordCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Character, decoded.Character);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.False(MuiGroupClassNameRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FF7), out _));
	}

	[Fact]
	public void GroupClassNameSequentialRecordPreservesMixedWidthBytesAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiGroupClassNameRecord
		{
			Word0 = 0xFFFFFFFFu,
			Word1 = 0xCAFEBABEu,
			Character = 0xA5,
			Terminator = 0,
		};

		Assert.True(MuiGroupClassNameRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiGroupClassNameRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Character, decoded.Character);
		Assert.Equal(value.Terminator, decoded.Terminator);

		var crossingEnd = APTR.FromPointer(0x20FFF);
		Assert.False(MuiGroupClassNameRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiGroupClassNameRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
