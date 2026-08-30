/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiMenuSpecialistClassNameStructAdapterTests
{
	[Fact]
	public void MenuFamilyClassNamesUseExactNamedRecords()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var menuAddress = APTR.FromPointer(0x2400);
		var menustripAddress = APTR.FromPointer(0x2420);
		var menuitemAddress = APTR.FromPointer(0x2440);
		var menu = new MuiMenuSpecialistMenuClassNameRecord
		{
			Word0 = 0x4D656E75, Word1 = 0x2E6D7569, Terminator = 0,
		};
		var menustrip = new MuiMenuSpecialistMenustripClassNameRecord
		{
			Word0 = 0x4D656E75, Word1 = 0x73747269,
			Word2 = 0x702E6D75, Character = (byte)'i', Terminator = 0,
		};
		var menuitem = new MuiMenuSpecialistMenuitemClassNameRecord
		{
			Word0 = 0x4D656E75, Word1 = 0x6974656D,
			Word2 = 0x2E6D7569, Terminator = 0,
		};

		Assert.Equal(9, Unsafe.SizeOf<MuiMenuSpecialistMenuClassNameRecord>());
		Assert.Equal(14,
			Unsafe.SizeOf<MuiMenuSpecialistMenustripClassNameRecord>());
		Assert.Equal(13,
			Unsafe.SizeOf<MuiMenuSpecialistMenuitemClassNameRecord>());
		Assert.True(MuiMenuSpecialistMenuClassNameRecordCodec.Write(ref platform,
			menuAddress, menu));
		Assert.True(MuiMenuSpecialistMenustripClassNameRecordCodec.Write(ref platform,
			menustripAddress, menustrip));
		Assert.True(MuiMenuSpecialistMenuitemClassNameRecordCodec.Write(ref platform,
			menuitemAddress, menuitem));
		Assert.True(MuiMenuSpecialistMenuClassNameRecordCodec.TryMatch(ref platform,
			menuAddress));
		Assert.True(MuiMenuSpecialistMenustripClassNameRecordCodec.TryMatch(ref platform,
			menustripAddress));
		Assert.True(MuiMenuSpecialistMenuitemClassNameRecordCodec.TryMatch(ref platform,
			menuitemAddress));
		Assert.Equal(MuiMenuSpecialistClass.Menu,
			MuiMenuSpecialistCore.ClassifyName(ref platform, menuAddress));
		Assert.Equal(MuiMenuSpecialistClass.Menustrip,
			MuiMenuSpecialistCore.ClassifyName(ref platform, menustripAddress));
		Assert.Equal(MuiMenuSpecialistClass.Menuitem,
			MuiMenuSpecialistCore.ClassifyName(ref platform, menuitemAddress));
		Assert.False(MuiMenuSpecialistMenuClassNameRecordCodec.TryMatch(ref platform,
			APTR.FromPointer(0x20FF8)));
		Assert.False(MuiMenuSpecialistMenustripClassNameRecordCodec.TryMatch(ref platform,
			APTR.FromPointer(0x20FF3)));
		Assert.False(MuiMenuSpecialistMenuitemClassNameRecordCodec.TryMatch(ref platform,
			APTR.FromPointer(0x20FF4)));
	}

	[Fact]
	public void MenuFamilySequentialRecordsPreserveMixedWidthBytesAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var menuAddress = APTR.FromPointer(0x2480);
		var menustripAddress = APTR.FromPointer(0x24A0);
		var menuitemAddress = APTR.FromPointer(0x24C0);
		var menu = new MuiMenuSpecialistMenuClassNameRecord
		{
			Word0 = 0xFFFFFFFFu, Word1 = 0xCAFEBABEu, Terminator = 0xA5,
		};
		var menustrip = new MuiMenuSpecialistMenustripClassNameRecord
		{
			Word0 = 0x01020304u, Word1 = 0xA5A5A5A5u,
			Word2 = 0x5A5A5A5Au, Character = 0xCC, Terminator = 0,
		};
		var menuitem = new MuiMenuSpecialistMenuitemClassNameRecord
		{
			Word0 = 0x11223344u, Word1 = 0x55667788u,
			Word2 = 0x99AABBCCu, Terminator = 0x7F,
		};

		Assert.True(MuiMenuSpecialistMenuClassNameRecordCodec.WriteRecord(
			ref platform, menuAddress, menu));
		Assert.True(MuiMenuSpecialistMenustripClassNameRecordCodec.WriteRecord(
			ref platform, menustripAddress, menustrip));
		Assert.True(MuiMenuSpecialistMenuitemClassNameRecordCodec.WriteRecord(
			ref platform, menuitemAddress, menuitem));
		Assert.True(MuiMenuSpecialistMenuClassNameRecordCodec.TryReadRecord(
			ref platform, menuAddress, out var menuRoundTrip));
		Assert.True(MuiMenuSpecialistMenustripClassNameRecordCodec.TryReadRecord(
			ref platform, menustripAddress, out var menustripRoundTrip));
		Assert.True(MuiMenuSpecialistMenuitemClassNameRecordCodec.TryReadRecord(
			ref platform, menuitemAddress, out var menuitemRoundTrip));
		Assert.Equal(menu.Word0, menuRoundTrip.Word0);
		Assert.Equal(menu.Word1, menuRoundTrip.Word1);
		Assert.Equal(menu.Terminator, menuRoundTrip.Terminator);
		Assert.Equal(menustrip.Word0, menustripRoundTrip.Word0);
		Assert.Equal(menustrip.Word1, menustripRoundTrip.Word1);
		Assert.Equal(menustrip.Word2, menustripRoundTrip.Word2);
		Assert.Equal(menustrip.Character, menustripRoundTrip.Character);
		Assert.Equal(menuitem.Word0, menuitemRoundTrip.Word0);
		Assert.Equal(menuitem.Word1, menuitemRoundTrip.Word1);
		Assert.Equal(menuitem.Word2, menuitemRoundTrip.Word2);
		Assert.Equal(menuitem.Terminator, menuitemRoundTrip.Terminator);
		Assert.False(MuiMenuSpecialistMenuClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x20FFF), out _));
		Assert.False(MuiMenuSpecialistMenustripClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x20FFF), out _));
		Assert.False(MuiMenuSpecialistMenuitemClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x20FFF), out _));
	}
}
