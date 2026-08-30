/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiPopSpecialistClassNameStructAdapterTests
{
	[Fact]
	public void PopFamilyClassNamesUseExactNamedRecords()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			state);
		var addresses = new[]
		{
			APTR.FromPointer(0x2400), APTR.FromPointer(0x2420),
			APTR.FromPointer(0x2440), APTR.FromPointer(0x2460),
			APTR.FromPointer(0x2480), APTR.FromPointer(0x24A0),
			APTR.FromPointer(0x24C0),
		};
		var popstring = new MuiPopSpecialistPopstringClassNameRecord
		{
			Word0 = 0x506F7073, Word1 = 0x7472696E, Word2 = 0x672E6D75,
			Character = (byte)'i', Terminator = 0,
		};
		var popobject = new MuiPopSpecialistPopobjectClassNameRecord
		{
			Word0 = 0x506F706F, Word1 = 0x626A6563, Word2 = 0x742E6D75,
			Character = (byte)'i', Terminator = 0,
		};
		var poplist = new MuiPopSpecialistPoplistClassNameRecord
		{
			Word0 = 0x506F706C, Word1 = 0x6973742E, Word2 = 0x6D756900,
		};
		var popasl = new MuiPopSpecialistPopaslClassNameRecord
		{
			Word0 = 0x506F7061, Word1 = 0x736C2E6D,
			Character0 = (byte)'u', Character1 = (byte)'i', Terminator = 0,
		};
		var popscreen = new MuiPopSpecialistPopscreenClassNameRecord
		{
			Word0 = 0x506F7073, Word1 = 0x63726565, Word2 = 0x6E2E6D75,
			Character = (byte)'i', Terminator = 0,
		};
		var popcolor = new MuiPopSpecialistPopcolorClassNameRecord
		{
			Word0 = 0x506F7063, Word1 = 0x6F6C6F72, Word2 = 0x2E6D7569,
			Terminator = 0,
		};
		var poppen = new MuiPopSpecialistPoppenClassNameRecord
		{
			Word0 = 0x506F7070, Word1 = 0x656E2E6D,
			Character0 = (byte)'u', Character1 = (byte)'i', Terminator = 0,
		};

		Assert.Equal(14, Unsafe.SizeOf<MuiPopSpecialistPopstringClassNameRecord>());
		Assert.Equal(14, Unsafe.SizeOf<MuiPopSpecialistPopobjectClassNameRecord>());
		Assert.Equal(12, Unsafe.SizeOf<MuiPopSpecialistPoplistClassNameRecord>());
		Assert.Equal(11, Unsafe.SizeOf<MuiPopSpecialistPopaslClassNameRecord>());
		Assert.Equal(14, Unsafe.SizeOf<MuiPopSpecialistPopscreenClassNameRecord>());
		Assert.Equal(13, Unsafe.SizeOf<MuiPopSpecialistPopcolorClassNameRecord>());
		Assert.Equal(11, Unsafe.SizeOf<MuiPopSpecialistPoppenClassNameRecord>());
		Assert.True(MuiPopSpecialistPopstringClassNameRecordCodec.Write(ref platform, addresses[0], popstring));
		Assert.True(MuiPopSpecialistPopobjectClassNameRecordCodec.Write(ref platform, addresses[1], popobject));
		Assert.True(MuiPopSpecialistPoplistClassNameRecordCodec.Write(ref platform, addresses[2], poplist));
		Assert.True(MuiPopSpecialistPopaslClassNameRecordCodec.Write(ref platform, addresses[3], popasl));
		Assert.True(MuiPopSpecialistPopscreenClassNameRecordCodec.Write(ref platform, addresses[4], popscreen));
		Assert.True(MuiPopSpecialistPopcolorClassNameRecordCodec.Write(ref platform, addresses[5], popcolor));
		Assert.True(MuiPopSpecialistPoppenClassNameRecordCodec.Write(ref platform, addresses[6], poppen));
		Assert.Equal(MuiPopSpecialistClass.Popstring, MuiPopSpecialistCore.ClassifyName(ref platform, addresses[0]));
		Assert.Equal(MuiPopSpecialistClass.Popobject, MuiPopSpecialistCore.ClassifyName(ref platform, addresses[1]));
		Assert.Equal(MuiPopSpecialistClass.Poplist, MuiPopSpecialistCore.ClassifyName(ref platform, addresses[2]));
		Assert.Equal(MuiPopSpecialistClass.Popasl, MuiPopSpecialistCore.ClassifyName(ref platform, addresses[3]));
		Assert.Equal(MuiPopSpecialistClass.Popscreen, MuiPopSpecialistCore.ClassifyName(ref platform, addresses[4]));
		Assert.Equal(MuiPopSpecialistClass.Popcolor, MuiPopSpecialistCore.ClassifyName(ref platform, addresses[5]));
		Assert.Equal(MuiPopSpecialistClass.Poppen, MuiPopSpecialistCore.ClassifyName(ref platform, addresses[6]));
		Assert.False(MuiPopSpecialistPopstringClassNameRecordCodec.TryMatch(ref platform, APTR.FromPointer(0x2FFF3)));
		Assert.False(MuiPopSpecialistPopobjectClassNameRecordCodec.TryMatch(ref platform, APTR.FromPointer(0x2FFF3)));
		Assert.False(MuiPopSpecialistPoplistClassNameRecordCodec.TryMatch(ref platform, APTR.FromPointer(0x2FFF5)));
		Assert.False(MuiPopSpecialistPopaslClassNameRecordCodec.TryMatch(ref platform, APTR.FromPointer(0x2FFF6)));
		Assert.False(MuiPopSpecialistPopscreenClassNameRecordCodec.TryMatch(ref platform, APTR.FromPointer(0x2FFF3)));
		Assert.False(MuiPopSpecialistPopcolorClassNameRecordCodec.TryMatch(ref platform, APTR.FromPointer(0x2FFF4)));
		Assert.False(MuiPopSpecialistPoppenClassNameRecordCodec.TryMatch(ref platform, APTR.FromPointer(0x2FFF6)));
	}

	[Fact]
	public void PopstringSequentialRecordPreservesMixedWidthBytesAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiPopSpecialistPopstringClassNameRecord
		{
			Word0 = 0xFFFFFFFFu, Word1 = 0xCAFEBABEu,
			Word2 = 0x01020304u, Character = 0xA5, Terminator = 0x5A,
		};

		Assert.True(MuiPopSpecialistPopstringClassNameRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiPopSpecialistPopstringClassNameRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Character, decoded.Character);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.False(MuiPopSpecialistPopstringClassNameRecordCodec.WriteRecord(
			ref platform, APTR.FromPointer(0x2FFFF), value));
		Assert.False(MuiPopSpecialistPopstringClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x2FFFF), out _));
	}

	[Fact]
	public void PopobjectSequentialRecordPreservesMixedWidthBytesAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiPopSpecialistPopobjectClassNameRecord
		{
			Word0 = 0xFFFFFFFFu, Word1 = 0xCAFEBABEu,
			Word2 = 0x01020304u, Character = 0xA5, Terminator = 0x5A,
		};

		Assert.True(MuiPopSpecialistPopobjectClassNameRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiPopSpecialistPopobjectClassNameRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Character, decoded.Character);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.False(MuiPopSpecialistPopobjectClassNameRecordCodec.WriteRecord(
			ref platform, APTR.FromPointer(0x2FFFF), value));
		Assert.False(MuiPopSpecialistPopobjectClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x2FFFF), out _));
	}

	[Fact]
	public void PoplistSequentialRecordPreservesWordsAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiPopSpecialistPoplistClassNameRecord
		{
			Word0 = 0xFFFFFFFFu, Word1 = 0xCAFEBABEu, Word2 = 0x01020304u,
		};

		Assert.True(MuiPopSpecialistPoplistClassNameRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiPopSpecialistPoplistClassNameRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.False(MuiPopSpecialistPoplistClassNameRecordCodec.WriteRecord(
			ref platform, APTR.FromPointer(0x2FFFF), value));
		Assert.False(MuiPopSpecialistPoplistClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x2FFFF), out _));
	}

	[Fact]
	public void PopaslSequentialRecordPreservesMixedWidthBytesAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiPopSpecialistPopaslClassNameRecord
		{
			Word0 = 0xFFFFFFFFu, Word1 = 0xCAFEBABEu,
			Character0 = 0xA5, Character1 = 0x5A, Terminator = 0xCC,
		};

		Assert.True(MuiPopSpecialistPopaslClassNameRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiPopSpecialistPopaslClassNameRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Character0, decoded.Character0);
		Assert.Equal(value.Character1, decoded.Character1);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.False(MuiPopSpecialistPopaslClassNameRecordCodec.WriteRecord(
			ref platform, APTR.FromPointer(0x2FFFF), value));
		Assert.False(MuiPopSpecialistPopaslClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x2FFFF), out _));
	}

	[Fact]
	public void PopscreenSequentialRecordPreservesMixedWidthBytesAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiPopSpecialistPopscreenClassNameRecord
		{
			Word0 = 0xFFFFFFFFu, Word1 = 0xCAFEBABEu,
			Word2 = 0x01020304u, Character = 0xA5, Terminator = 0x5A,
		};

		Assert.True(MuiPopSpecialistPopscreenClassNameRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiPopSpecialistPopscreenClassNameRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Character, decoded.Character);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.False(MuiPopSpecialistPopscreenClassNameRecordCodec.WriteRecord(
			ref platform, APTR.FromPointer(0x2FFFF), value));
		Assert.False(MuiPopSpecialistPopscreenClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x2FFFF), out _));
	}

	[Fact]
	public void PopcolorSequentialRecordPreservesWordsAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiPopSpecialistPopcolorClassNameRecord
		{
			Word0 = 0xFFFFFFFFu, Word1 = 0xCAFEBABEu,
			Word2 = 0x01020304u, Terminator = 0xA5,
		};

		Assert.True(MuiPopSpecialistPopcolorClassNameRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiPopSpecialistPopcolorClassNameRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.False(MuiPopSpecialistPopcolorClassNameRecordCodec.WriteRecord(
			ref platform, APTR.FromPointer(0x2FFFF), value));
		Assert.False(MuiPopSpecialistPopcolorClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x2FFFF), out _));
	}

	[Fact]
	public void PoppenSequentialRecordPreservesMixedWidthBytesAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiPopSpecialistPoppenClassNameRecord
		{
			Word0 = 0xFFFFFFFFu, Word1 = 0xCAFEBABEu,
			Character0 = 0xA5, Character1 = 0x5A, Terminator = 0xCC,
		};

		Assert.True(MuiPopSpecialistPoppenClassNameRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiPopSpecialistPoppenClassNameRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Character0, decoded.Character0);
		Assert.Equal(value.Character1, decoded.Character1);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.False(MuiPopSpecialistPoppenClassNameRecordCodec.WriteRecord(
			ref platform, APTR.FromPointer(0x2FFFF), value));
		Assert.False(MuiPopSpecialistPoppenClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x2FFFF), out _));
	}
}
