/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiMakeObjectMenuBarLabelStructAdapterTests
{
	[Fact]
	public void MenuBarCommandKeyLookaheadUsesNamedRecord()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		platform.WriteUInt8(address, 0, (byte)'X');
		platform.WriteUInt8(address, 1, 0);

		Assert.Equal(2, Unsafe.SizeOf<MuiMakeObjectMenuBarLabelRecord>());
		Assert.True(MuiMakeObjectMenuBarLabelRecordCodec.TryReadRecord(ref platform,
			address, out var value));
		Assert.Equal((byte)'X', value.Character);
		Assert.Equal(0, value.Terminator);
		Assert.False(MuiMakeObjectMenuBarLabelRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x20FFF), out _));
	}

	[Fact]
	public void MenuBarCommandKeyLookaheadSequentialRecordPreservesBytes()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		platform.WriteUInt8(address, 0, 0xA5);
		platform.WriteUInt8(address, 1, 0x5A);
		Assert.True(MuiMakeObjectMenuBarLabelRecordCodec.TryReadRecord(ref platform,
			address, out var value));
		Assert.Equal(0xA5, value.Character);
		Assert.Equal(0x5A, value.Terminator);
		Assert.False(MuiMakeObjectMenuBarLabelRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x2FFFF), out _));
	}
}
