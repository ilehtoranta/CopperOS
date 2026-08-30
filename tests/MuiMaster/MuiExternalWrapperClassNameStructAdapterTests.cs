/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiExternalWrapperClassNameStructAdapterTests
{
	[Fact]
	public void BoopsiAndDtpicClassNamesUseExactNamedRecords()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var boopsiAddress = APTR.FromPointer(0x2400);
		var dtpicAddress = APTR.FromPointer(0x2420);
		var boopsi = new MuiExternalWrapperBoopsiClassNameRecord
		{
			Word0 = 0x426F6F70,
			Word1 = 0x73692E6D,
			Character0 = (byte)'u',
			Character1 = (byte)'i',
			Terminator = 0,
		};
		var dtpic = new MuiExternalWrapperDtpicClassNameRecord
		{
			Word0 = 0x44747069,
			Word1 = 0x632E6D75,
			Character = (byte)'i',
			Terminator = 0,
		};

		Assert.Equal(11,
			Unsafe.SizeOf<MuiExternalWrapperBoopsiClassNameRecord>());
		Assert.Equal(10,
			Unsafe.SizeOf<MuiExternalWrapperDtpicClassNameRecord>());
		Assert.True(MuiExternalWrapperBoopsiClassNameRecordCodec.Write(ref platform,
			boopsiAddress, boopsi));
		Assert.True(MuiExternalWrapperDtpicClassNameRecordCodec.Write(ref platform,
			dtpicAddress, dtpic));
		Assert.True(MuiExternalWrapperBoopsiClassNameRecordCodec.TryMatch(ref platform,
			boopsiAddress));
		Assert.True(MuiExternalWrapperDtpicClassNameRecordCodec.TryMatch(ref platform,
			dtpicAddress));
		Assert.Equal(MuiExternalWrapperClass.Boopsi,
			MuiExternalWrapperCore.ClassifyName(ref platform, boopsiAddress));
		Assert.Equal(MuiExternalWrapperClass.Dtpic,
			MuiExternalWrapperCore.ClassifyName(ref platform, dtpicAddress));
		Assert.False(MuiExternalWrapperBoopsiClassNameRecordCodec.TryMatch(ref platform,
			APTR.FromPointer(0x20FF6)));
		Assert.False(MuiExternalWrapperDtpicClassNameRecordCodec.TryMatch(ref platform,
			APTR.FromPointer(0x20FF7)));
	}

	[Fact]
	public void ExternalWrapperSequentialRecordsPreserveMixedWidthBytesAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var boopsiAddress = APTR.FromPointer(0x2480);
		var dtpicAddress = APTR.FromPointer(0x24A0);
		var boopsi = new MuiExternalWrapperBoopsiClassNameRecord
		{
			Word0 = 0xFFFFFFFFu, Word1 = 0xCAFEBABEu,
			Character0 = 0xA5, Character1 = 0x5A, Terminator = 0,
		};
		var dtpic = new MuiExternalWrapperDtpicClassNameRecord
		{
			Word0 = 0x01020304u, Word1 = 0xA5A5A5A5u,
			Character = 0xCC, Terminator = 0,
		};

		Assert.True(MuiExternalWrapperBoopsiClassNameRecordCodec.WriteRecord(
			ref platform, boopsiAddress, boopsi));
		Assert.True(MuiExternalWrapperDtpicClassNameRecordCodec.WriteRecord(
			ref platform, dtpicAddress, dtpic));
		Assert.True(MuiExternalWrapperBoopsiClassNameRecordCodec.TryReadRecord(
			ref platform, boopsiAddress, out var boopsiRoundTrip));
		Assert.True(MuiExternalWrapperDtpicClassNameRecordCodec.TryReadRecord(
			ref platform, dtpicAddress, out var dtpicRoundTrip));
		Assert.Equal(boopsi.Word0, boopsiRoundTrip.Word0);
		Assert.Equal(boopsi.Word1, boopsiRoundTrip.Word1);
		Assert.Equal(boopsi.Character0, boopsiRoundTrip.Character0);
		Assert.Equal(boopsi.Character1, boopsiRoundTrip.Character1);
		Assert.Equal(dtpic.Word0, dtpicRoundTrip.Word0);
		Assert.Equal(dtpic.Word1, dtpicRoundTrip.Word1);
		Assert.Equal(dtpic.Character, dtpicRoundTrip.Character);
		Assert.False(MuiExternalWrapperBoopsiClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x20FFF), out _));
		Assert.False(MuiExternalWrapperDtpicClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x20FFF), out _));
	}
}
