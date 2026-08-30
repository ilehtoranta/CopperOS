/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationUsedClassesVectorEntryStructCodecTests
{
	[Fact]
	public void UsedClassesVectorEntryRoundTripsFullPointerThroughSequentialStruct()
	{
		Assert.Equal(4, Unsafe.SizeOf<MuiApplicationUsedClassesVectorEntry>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2800);
		var expected = new MuiApplicationUsedClassesVectorEntry
		{
			Name = APTR.FromPointer(0xFEDCBA98u),
		};

		Assert.True(MuiApplicationUsedClassesVectorEntryStructCodec.Write(
			ref platform, address, expected));
		Assert.True(MuiApplicationUsedClassesVectorEntryStructCodec.TryRead(
			ref platform, address, out var actual));
		Assert.Equal(expected.Name, actual.Name);
		Assert.True(MuiApplicationUsedClassesVectorEntryCodec.TryRead(ref platform,
			address, out var validated));
		Assert.Equal(expected.Name, validated.Name);
	}

	[Fact]
	public void UsedClassesVectorEntryStructCodecRejectsNullAndIncompleteSlot()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var value = new MuiApplicationUsedClassesVectorEntry
		{
			Name = APTR.Null,
		};
		var crossing = APTR.FromPointer(0x30FFF);

		Assert.False(MuiApplicationUsedClassesVectorEntryStructCodec.Write(
			ref platform, APTR.Null, value));
		Assert.False(MuiApplicationUsedClassesVectorEntryStructCodec.TryRead(
			ref platform, APTR.Null, out _));
		Assert.False(MuiApplicationUsedClassesVectorEntryStructCodec.Write(
			ref platform, crossing, value));
		Assert.False(MuiApplicationUsedClassesVectorEntryStructCodec.TryRead(
			ref platform, crossing, out _));
	}

	[Fact]
	public void UsedClassesVectorBridgeUsesCompleteNamedPointerEntries()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var vector = APTR.FromPointer(0x2900);
		var expected = new MuiApplicationUsedClassesVectorEntry
		{
			Name = APTR.FromPointer(0xFEDCBA98u),
		};

		Assert.True(MuiApplicationUsedClassesVectorCodec.TryWrite(ref platform,
			vector, 2, expected));
		Assert.True(MuiApplicationUsedClassesVectorCodec.TryRead(ref platform,
			vector, 2, out var actual));
		Assert.Equal(expected.Name, actual.Name);

		Assert.False(MuiApplicationUsedClassesVectorCodec.TryRead(ref platform,
			vector, 0x40000000u, out _));
		Assert.False(MuiApplicationUsedClassesVectorCodec.TryRead(ref platform,
			APTR.FromPointer(0x30FFEu), 0, out _));
		Assert.False(MuiApplicationUsedClassesVectorCodec.TryWrite(ref platform,
			APTR.Null, 0, expected));
	}
}
