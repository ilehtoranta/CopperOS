/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiHeadlessMessageStructCodecTests
{
	[Fact]
	public void FixedHeadlessPacketsRoundTripThroughSequentialRecords()
	{
		Assert.Equal(4, Unsafe.SizeOf<MuiHeadlessMethodMessage>());
		Assert.Equal(12, Unsafe.SizeOf<MuiHeadlessOmSetMessage>());
		Assert.Equal(16, Unsafe.SizeOf<MuiHeadlessOmUpdateMessage>());

		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2400);
		var method = 0x80420001u;
		Assert.True(MuiHeadlessMessageStructCodec.TryWriteMethod(ref platform,
			address, method));
		Assert.True(MuiHeadlessMessageStructCodec.TryReadMethodIdValue(
			ref platform, address, out var methodRead));
		Assert.Equal(method, methodRead);

		var set = default(MuiHeadlessOmSetMessage);
		set.Attributes = APTR.FromPointer(0x2500);
		set.GadgetInfo = APTR.FromPointer(0x2600);
		Assert.True(MuiHeadlessMessageStructCodec.TryWriteOmSet(ref platform,
			address, set));
		Assert.True(MuiHeadlessMessageStructCodec.TryReadOmSet(ref platform,
			address, out var setRead));
		Assert.Equal(set.Attributes, setRead.Attributes);
		Assert.Equal(set.GadgetInfo, setRead.GadgetInfo);

		var update = default(MuiHeadlessOmUpdateMessage);
		update.Attributes = APTR.FromPointer(0x2700);
		update.GadgetInfo = APTR.FromPointer(0x2800);
		update.Flags = 0xA5A5u;
		Assert.True(MuiHeadlessMessageStructCodec.TryWriteOmUpdate(ref platform,
			address, update));
		Assert.True(MuiHeadlessMessageStructCodec.TryReadOmUpdate(ref platform,
			address, out var updateRead));
		Assert.Equal(update.Attributes, updateRead.Attributes);
		Assert.Equal(update.GadgetInfo, updateRead.GadgetInfo);
		Assert.Equal(update.Flags, updateRead.Flags);
	}

	[Fact]
	public void FixedHeadlessPacketsRejectIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var crossingEnd = APTR.FromPointer(0x20FF5);
		var set = default(MuiHeadlessOmSetMessage);
		Assert.False(MuiHeadlessMessageStructCodec.TryWriteOmSet(ref platform,
			crossingEnd, set));
		Assert.False(MuiHeadlessMessageStructCodec.TryReadOmUpdate(ref platform,
			crossingEnd, out _));
		Assert.False(MuiHeadlessMessageStructCodec.TryReadMethodIdValue(
			ref platform, APTR.Null, out _));
	}
}
