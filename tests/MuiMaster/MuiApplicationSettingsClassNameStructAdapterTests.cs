/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationSettingsClassNameStructAdapterTests
{
	[Fact]
	public void ApplicationSettingsDataspaceClassNameUsesNamedRecord()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var value = new MuiApplicationSettingsDataspaceClassNameRecord
		{
			Word0 = 0x44617461,
			Word1 = 0x73706163,
			Word2 = 0x652E6D75,
			Character = (byte)'i',
			Terminator = 0,
		};

		Assert.Equal(14,
			Unsafe.SizeOf<MuiApplicationSettingsDataspaceClassNameRecord>());
		Assert.True(MuiApplicationSettingsDataspaceClassNameRecordCodec.Write(
			ref platform, address, value));
		Assert.True(MuiApplicationSettingsDataspaceClassNameRecordCodec.TryRead(
			ref platform, address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Character, decoded.Character);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.False(MuiApplicationSettingsDataspaceClassNameRecordCodec.TryRead(
			ref platform, APTR.FromPointer(0x20FF3), out _));
	}

	[Fact]
	public void ApplicationSettingsDataspaceClassNameSequentialRecordPreservesBytesAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2500);
		var value = new MuiApplicationSettingsDataspaceClassNameRecord
		{
			Word0 = uint.MaxValue,
			Word1 = 0x01020304u,
			Word2 = 0xAABBCCDDu,
			Character = (byte)0x7F,
			Terminator = 0,
		};

		Assert.True(MuiApplicationSettingsDataspaceClassNameRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiApplicationSettingsDataspaceClassNameRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Character, decoded.Character);
		Assert.Equal(value.Terminator, decoded.Terminator);

		var crossingEnd = APTR.FromPointer(0x20FF3);
		Assert.False(MuiApplicationSettingsDataspaceClassNameRecordCodec.WriteRecord(
			ref platform, crossingEnd, value));
		Assert.False(MuiApplicationSettingsDataspaceClassNameRecordCodec.TryReadRecord(
			ref platform, crossingEnd, out _));
	}
}
