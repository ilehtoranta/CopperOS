/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiClassServiceLibraryPrefixStructAdapterTests
{
	[Fact]
	public void LoaderPrefixUsesNamedRecord()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var value = new MuiClassServiceLibraryPrefixRecord
		{
			Prefix = MuiClassServiceLibraryPrefixRecordCodec.MuiSlash,
		};
		Assert.Equal(4, Unsafe.SizeOf<MuiClassServiceLibraryPrefixRecord>());
		Assert.True(MuiClassServiceLibraryPrefixRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiClassServiceLibraryPrefixRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Prefix, decoded.Prefix);
		Assert.False(MuiClassServiceLibraryPrefixRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x20FFD), out _));
		Assert.False(MuiClassServiceLibraryPrefixRecordCodec.WriteRecord(ref platform,
			APTR.FromPointer(0x20FFD), value));
	}

	[Fact]
	public void LoaderPrefixSequentialRecordPreservesWordAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiClassServiceLibraryPrefixRecord
		{
			Prefix = 0xFFFFFFFFu,
		};
		Assert.True(MuiClassServiceLibraryPrefixRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiClassServiceLibraryPrefixRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Prefix, decoded.Prefix);
		Assert.False(MuiClassServiceLibraryPrefixRecordCodec.WriteRecord(ref platform,
			APTR.FromPointer(0x20FFD), value));
		Assert.False(MuiClassServiceLibraryPrefixRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x20FFD), out _));
	}
}
