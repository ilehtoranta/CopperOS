/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiDirlistExampleVolumeNameStructAdapterTests
{
	[Fact]
	public void ExampleVolumeNameUsesNamedRecord()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var value = MuiDirlistExampleVolumeNameRecord.Create((byte)'7');

		Assert.Equal(10, Unsafe.SizeOf<MuiDirlistExampleVolumeNameRecord>());
		Assert.True(MuiDirlistExampleVolumeNameRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiDirlistExampleVolumeNameRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Separator, decoded.Separator);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.False(MuiDirlistExampleVolumeNameRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x20FF7), out _));
		Assert.False(MuiDirlistExampleVolumeNameRecordCodec.WriteRecord(ref platform,
			APTR.FromPointer(0x20FF7), value));
	}

	[Fact]
	public void ExampleVolumeWriterPublishesTheCompleteRecord()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var scratch = APTR.FromPointer(0x2400);

		Assert.True(MuiDirlistCore.WriteExampleVolumeEntry(ref platform, scratch,
			(byte)'3'));
		var name = APTR.FromPointer(scratch.Raw +
			MuiDirlistScanEntryWireState.NameOffset);
		Assert.True(MuiDirlistExampleVolumeNameRecordCodec.TryReadRecord(ref platform,
			name, out var value));
		Assert.Equal(0x4578616Du, value.Word0);
		Assert.Equal(0x706C6533u, value.Word1);
		Assert.Equal((byte)':', value.Separator);
		Assert.Equal((byte)0, value.Terminator);
	}

	[Fact]
	public void ExampleVolumeNameSequentialRecordPreservesMixedWidthsAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiDirlistExampleVolumeNameRecord
		{
			Word0 = 0xFFFFFFFFu,
			Word1 = 0xCAFEBABEu,
			Separator = 0xA5,
			Terminator = 0x5A,
		};

		Assert.True(MuiDirlistExampleVolumeNameRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiDirlistExampleVolumeNameRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Separator, decoded.Separator);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.False(MuiDirlistExampleVolumeNameRecordCodec.WriteRecord(ref platform,
			APTR.FromPointer(0x20FF7), value));
		Assert.False(MuiDirlistExampleVolumeNameRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x20FF7), out _));
	}
}
