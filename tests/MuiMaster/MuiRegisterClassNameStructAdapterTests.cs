/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiRegisterClassNameStructAdapterTests
{
	[Fact]
	public void RegisterClassNameUsesNamedRecord()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var value = new MuiRegisterClassNameRecord
		{
			Word0 = 0x52656769,
			Word1 = 0x73746572,
			Word2 = 0x2E6D7569,
			Terminator = 0,
		};

		Assert.Equal(13, Unsafe.SizeOf<MuiRegisterClassNameRecord>());
		Assert.True(MuiRegisterClassNameRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiRegisterClassNameRecordCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.False(MuiRegisterClassNameRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FF4), out _));
	}

	[Fact]
	public void RegisterClassNameSequentialRecordPreservesMixedWidthBytesAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiRegisterClassNameRecord
		{
			Word0 = 0xFFFFFFFFu,
			Word1 = 0xCAFEBABEu,
			Word2 = 0x01020304u,
			Terminator = 0xA5,
		};

		Assert.True(MuiRegisterClassNameRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiRegisterClassNameRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Terminator, decoded.Terminator);

		var crossingEnd = APTR.FromPointer(0x20FFF);
		Assert.False(MuiRegisterClassNameRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiRegisterClassNameRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}
}
