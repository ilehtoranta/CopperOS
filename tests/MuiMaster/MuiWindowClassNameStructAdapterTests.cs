/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiWindowClassNameStructAdapterTests
{
	[Fact]
	public void WindowClassNameUsesNamedRecord()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var value = new MuiWindowClassNameRecord
		{
			Word0 = 0x57696E64,
			Word1 = 0x6F772E6D,
			Character0 = (byte)'u',
			Character1 = (byte)'i',
			Terminator = 0,
		};

		Assert.Equal(11, Unsafe.SizeOf<MuiWindowClassNameRecord>());
		Assert.True(MuiWindowClassNameRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiWindowClassNameRecordCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Character0, decoded.Character0);
		Assert.Equal(value.Character1, decoded.Character1);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.False(MuiWindowClassNameRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFB), out _));
	}

	[Fact]
	public void WindowClassNameSequentialRecordPreservesMixedWidthBytesAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiWindowClassNameRecord
		{
			Word0 = 0xFFFFFFFFu,
			Word1 = 0xCAFEBABEu,
			Character0 = 0xA5,
			Character1 = 0x5A,
			Terminator = 0,
		};

		Assert.True(MuiWindowClassNameRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiWindowClassNameRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Character0, decoded.Character0);
		Assert.Equal(value.Character1, decoded.Character1);
		Assert.Equal(value.Terminator, decoded.Terminator);

		var crossingEnd = APTR.FromPointer(0x20FFF);
		Assert.False(MuiWindowClassNameRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiWindowClassNameRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
