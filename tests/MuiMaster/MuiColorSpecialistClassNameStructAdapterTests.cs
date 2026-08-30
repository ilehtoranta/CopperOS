/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiColorSpecialistClassNameStructAdapterTests
{
	[Fact]
	public void ColorSpecialistClassNamesUseExactNamedRecords()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var pendisplayAddress = APTR.FromPointer(0x2400);
		var colorfieldAddress = APTR.FromPointer(0x2420);
		var coloradjustAddress = APTR.FromPointer(0x2440);
		var paletteAddress = APTR.FromPointer(0x2460);
		var penadjustAddress = APTR.FromPointer(0x2480);

		var pendisplay = new MuiColorSpecialistPendisplayClassNameRecord
		{
			Word0 = 0x50656E64,
			Word1 = 0x6973706C,
			Word2 = 0x61792E6D,
			Character0 = (byte)'u',
			Character1 = (byte)'i',
			Terminator = 0,
		};
		var colorfield = new MuiColorSpecialistColorfieldClassNameRecord
		{
			Word0 = 0x436F6C6F,
			Word1 = 0x72666965,
			Word2 = 0x6C642E6D,
			Character0 = (byte)'u',
			Character1 = (byte)'i',
			Terminator = 0,
		};
		var coloradjust = new MuiColorSpecialistColoradjustClassNameRecord
		{
			Word0 = 0x436F6C6F,
			Word1 = 0x7261646A,
			Word2 = 0x7573742E,
			Word3 = 0x6D756900,
		};
		var palette = new MuiColorSpecialistPaletteClassNameRecord
		{
			Word0 = 0x50616C65,
			Word1 = 0x7474652E,
			Word2 = 0x6D756900,
		};
		var penadjust = new MuiColorSpecialistPenadjustClassNameRecord
		{
			Word0 = 0x50656E61,
			Word1 = 0x646A7573,
			Word2 = 0x742E6D75,
			Character = (byte)'i',
			Terminator = 0,
		};

		Assert.Equal(15, Unsafe.SizeOf<MuiColorSpecialistPendisplayClassNameRecord>());
		Assert.Equal(15, Unsafe.SizeOf<MuiColorSpecialistColorfieldClassNameRecord>());
		Assert.Equal(16, Unsafe.SizeOf<MuiColorSpecialistColoradjustClassNameRecord>());
		Assert.Equal(12, Unsafe.SizeOf<MuiColorSpecialistPaletteClassNameRecord>());
		Assert.Equal(14, Unsafe.SizeOf<MuiColorSpecialistPenadjustClassNameRecord>());
		Assert.True(MuiColorSpecialistPendisplayClassNameRecordCodec.Write(
			ref platform, pendisplayAddress, pendisplay));
		Assert.True(MuiColorSpecialistColorfieldClassNameRecordCodec.Write(
			ref platform, colorfieldAddress, colorfield));
		Assert.True(MuiColorSpecialistColoradjustClassNameRecordCodec.Write(
			ref platform, coloradjustAddress, coloradjust));
		Assert.True(MuiColorSpecialistPaletteClassNameRecordCodec.Write(ref platform,
			paletteAddress, palette));
		Assert.True(MuiColorSpecialistPenadjustClassNameRecordCodec.Write(ref platform,
			penadjustAddress, penadjust));

		Assert.True(MuiColorSpecialistPendisplayClassNameRecordCodec.TryMatch(
			ref platform, pendisplayAddress));
		Assert.True(MuiColorSpecialistColorfieldClassNameRecordCodec.TryMatch(
			ref platform, colorfieldAddress));
		Assert.True(MuiColorSpecialistColoradjustClassNameRecordCodec.TryMatch(
			ref platform, coloradjustAddress));
		Assert.True(MuiColorSpecialistPaletteClassNameRecordCodec.TryMatch(ref platform,
			paletteAddress));
		Assert.True(MuiColorSpecialistPenadjustClassNameRecordCodec.TryMatch(ref platform,
			penadjustAddress));

		Assert.Equal(MuiColorSpecialistClass.Pendisplay,
			MuiColorSpecialistCore.ClassifyName(ref platform, pendisplayAddress));
		Assert.Equal(MuiColorSpecialistClass.Colorfield,
			MuiColorSpecialistCore.ClassifyName(ref platform, colorfieldAddress));
		Assert.Equal(MuiColorSpecialistClass.Coloradjust,
			MuiColorSpecialistCore.ClassifyName(ref platform, coloradjustAddress));
		Assert.Equal(MuiColorSpecialistClass.Palette,
			MuiColorSpecialistCore.ClassifyName(ref platform, paletteAddress));
		Assert.Equal(MuiColorSpecialistClass.Penadjust,
			MuiColorSpecialistCore.ClassifyName(ref platform, penadjustAddress));
		Assert.False(MuiColorSpecialistPendisplayClassNameRecordCodec.TryMatch(
			ref platform, APTR.FromPointer(0x20FF2)));
	}

	[Fact]
	public void PendisplaySequentialRecordPreservesMixedWidthBytesAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiColorSpecialistPendisplayClassNameRecord
		{
			Word0 = 0xFFFFFFFFu, Word1 = 0xCAFEBABEu, Word2 = 0x01020304u,
			Character0 = 0xA5, Character1 = 0x5A, Terminator = 0xCC,
		};

		Assert.True(MuiColorSpecialistPendisplayClassNameRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiColorSpecialistPendisplayClassNameRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Character0, decoded.Character0);
		Assert.Equal(value.Character1, decoded.Character1);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.False(MuiColorSpecialistPendisplayClassNameRecordCodec.WriteRecord(
			ref platform, APTR.FromPointer(0x2FFFF), value));
		Assert.False(MuiColorSpecialistPendisplayClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x2FFFF), out _));
	}

	[Fact]
	public void ColorfieldSequentialRecordPreservesMixedWidthBytesAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiColorSpecialistColorfieldClassNameRecord
		{
			Word0 = 0xFFFFFFFFu, Word1 = 0xCAFEBABEu, Word2 = 0x01020304u,
			Character0 = 0xA5, Character1 = 0x5A, Terminator = 0xCC,
		};

		Assert.True(MuiColorSpecialistColorfieldClassNameRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiColorSpecialistColorfieldClassNameRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Character0, decoded.Character0);
		Assert.Equal(value.Character1, decoded.Character1);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.False(MuiColorSpecialistColorfieldClassNameRecordCodec.WriteRecord(
			ref platform, APTR.FromPointer(0x2FFFF), value));
		Assert.False(MuiColorSpecialistColorfieldClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x2FFFF), out _));
	}

	[Fact]
	public void ColoradjustSequentialRecordPreservesWordsAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiColorSpecialistColoradjustClassNameRecord
		{
			Word0 = 0xFFFFFFFFu, Word1 = 0xCAFEBABEu,
			Word2 = 0x01020304u, Word3 = 0xA5A5A5A5u,
		};

		Assert.True(MuiColorSpecialistColoradjustClassNameRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiColorSpecialistColoradjustClassNameRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Word3, decoded.Word3);
		Assert.False(MuiColorSpecialistColoradjustClassNameRecordCodec.WriteRecord(
			ref platform, APTR.FromPointer(0x2FFF0), value));
		Assert.False(MuiColorSpecialistColoradjustClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x2FFF0), out _));
	}

	[Fact]
	public void PaletteSequentialRecordPreservesWordsAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiColorSpecialistPaletteClassNameRecord
		{
			Word0 = 0xFFFFFFFFu, Word1 = 0xCAFEBABEu, Word2 = 0x01020304u,
		};

		Assert.True(MuiColorSpecialistPaletteClassNameRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiColorSpecialistPaletteClassNameRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.False(MuiColorSpecialistPaletteClassNameRecordCodec.WriteRecord(
			ref platform, APTR.FromPointer(0x2FFFF), value));
		Assert.False(MuiColorSpecialistPaletteClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x2FFFF), out _));
	}

	[Fact]
	public void PenadjustSequentialRecordPreservesMixedWidthBytesAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2480);
		var value = new MuiColorSpecialistPenadjustClassNameRecord
		{
			Word0 = 0xFFFFFFFFu, Word1 = 0xCAFEBABEu, Word2 = 0x01020304u,
			Character = 0xA5, Terminator = 0x5A,
		};

		Assert.True(MuiColorSpecialistPenadjustClassNameRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiColorSpecialistPenadjustClassNameRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Word0, decoded.Word0);
		Assert.Equal(value.Word1, decoded.Word1);
		Assert.Equal(value.Word2, decoded.Word2);
		Assert.Equal(value.Character, decoded.Character);
		Assert.Equal(value.Terminator, decoded.Terminator);
		Assert.False(MuiColorSpecialistPenadjustClassNameRecordCodec.WriteRecord(
			ref platform, APTR.FromPointer(0x2FFFF), value));
		Assert.False(MuiColorSpecialistPenadjustClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x2FFFF), out _));
	}
}
