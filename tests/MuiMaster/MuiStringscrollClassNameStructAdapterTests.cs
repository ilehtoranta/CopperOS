/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringscrollClassNameStructAdapterTests
{
	[Fact]
	public void StringscrollClassNameUsesNamedWordsAndExactBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var value = new MuiStringscrollScrollbarClassNameRecord
		{
			Word0 = 0x7363726F,
			Word1 = 0x6C6C6261,
			Word2 = 0x722E6D75,
			TailCharacter = (byte)'i',
			Terminator = 0,
		};

		Assert.Equal(14, Unsafe.SizeOf<MuiStringscrollScrollbarClassNameRecord>());
		Assert.True(MuiStringscrollScrollbarClassNameRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiStringscrollScrollbarClassNameRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.TailCharacter, decoded.TailCharacter);
		Assert.Equal(value.Terminator, decoded.Terminator);

		Assert.False(MuiStringscrollScrollbarClassNameRecordCodec.WriteRecord(
			ref platform, APTR.FromPointer(0x20FF3), value));
		Assert.False(MuiStringscrollScrollbarClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x20FF3), out _));
	}

	[Fact]
	public void StringscrollClassNameSequentialRecordPreservesMixedWidths()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiStringscrollScrollbarClassNameRecord
		{
			Word0 = 0xFFFFFFFFu, Word1 = 0xCAFEBABEu, Word2 = 0x01020304u,
			TailCharacter = 0xA5, Terminator = 0x5A,
		};

		Assert.True(MuiStringscrollScrollbarClassNameRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiStringscrollScrollbarClassNameRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.TailCharacter, decoded.TailCharacter);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.False(MuiStringscrollScrollbarClassNameRecordCodec.WriteRecord(
			ref platform, APTR.FromPointer(0x2FFFF), value));
		Assert.False(MuiStringscrollScrollbarClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x2FFFF), out _));
	}
}
