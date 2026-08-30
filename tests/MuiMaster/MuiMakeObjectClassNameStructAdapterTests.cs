/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiMakeObjectClassNameStructAdapterTests
{
	[Fact]
	public void MakeObjectClassNameUsesNamedLongwordsAndCompleteBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var value = new MuiMakeObjectClassNameRecord
		{
			Word0 = 0x52656374,
			Word1 = 0x616E676C,
			Word2 = 0x652E6D75,
			Word3 = 0x69000000,
			Word4 = 0xA5A5A5A5,
			Word5 = 0x5A5A5A5A,
		};

		Assert.Equal(24, Unsafe.SizeOf<MuiMakeObjectClassNameRecord>());
		Assert.True(MuiMakeObjectClassNameRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiMakeObjectClassNameRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Word3, decoded.Word3);
		Assert.Equal(value.Word4, decoded.Word4);
		Assert.Equal(value.Word5, decoded.Word5);

		Assert.False(MuiMakeObjectClassNameRecordCodec.WriteRecord(ref platform,
			APTR.FromPointer(0x20FE9), value));
		Assert.False(MuiMakeObjectClassNameRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x20FE9), out _));
	}

	[Fact]
	public void MakeObjectClassNameSequentialRecordPreservesAllLongwords()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiMakeObjectClassNameRecord
		{
			Word0 = 0xFFFFFFFFu, Word1 = 0xCAFEBABEu, Word2 = 0x01020304u,
			Word3 = 0x11223344u, Word4 = 0xA5A5A5A5u, Word5 = 0x5A5A5A5Au,
		};

		Assert.True(MuiMakeObjectClassNameRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiMakeObjectClassNameRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Word3, decoded.Word3);
		Assert.Equal(value.Word4, decoded.Word4);
		Assert.Equal(value.Word5, decoded.Word5);
		Assert.False(MuiMakeObjectClassNameRecordCodec.WriteRecord(ref platform,
			APTR.FromPointer(0x2FFFF), value));
		Assert.False(MuiMakeObjectClassNameRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x2FFFF), out _));
	}
}
