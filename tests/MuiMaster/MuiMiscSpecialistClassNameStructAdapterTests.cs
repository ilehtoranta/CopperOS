/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiMiscSpecialistClassNameStructAdapterTests
{
	[Fact]
	public void MiscClassNamesUseExactNamedRecordLengths()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			state);
		var addresses = new[]
		{
			APTR.FromPointer(0x2400), APTR.FromPointer(0x2440),
			APTR.FromPointer(0x2480), APTR.FromPointer(0x24C0),
			APTR.FromPointer(0x2500), APTR.FromPointer(0x2540),
			APTR.FromPointer(0x2580), APTR.FromPointer(0x25C0),
			APTR.FromPointer(0x2600), APTR.FromPointer(0x2640),
		};
		var values = new[]
		{
			new MuiMiscSpecialistClassNameRecord { Word0 = 0x4B657961, Word1 = 0x646A7573, Word2 = 0x742E6D75, Word3 = 0x69000000 },
			new MuiMiscSpecialistClassNameRecord { Word0 = 0x50616E65, Word1 = 0x6C2E6D75, Word2 = 0x69000000 },
			new MuiMiscSpecialistClassNameRecord { Word0 = 0x46696C65, Word1 = 0x70616E65, Word2 = 0x6C2E6D75, Word3 = 0x69000000 },
			new MuiMiscSpecialistClassNameRecord { Word0 = 0x466F6E74, Word1 = 0x64697370, Word2 = 0x6C61792E, Word3 = 0x6D756900 },
			new MuiMiscSpecialistClassNameRecord { Word0 = 0x46535072, Word1 = 0x6F746563, Word2 = 0x74696F6E, Word3 = 0x42697473, Word4 = 0x2E6D7569 },
			new MuiMiscSpecialistClassNameRecord { Word0 = 0x5363726D, Word1 = 0x6F64656C, Word2 = 0x6973742E, Word3 = 0x6D756900 },
			new MuiMiscSpecialistClassNameRecord { Word0 = 0x41726773, Word1 = 0x7472696E, Word2 = 0x672E6D75, Word3 = 0x69000000 },
			new MuiMiscSpecialistClassNameRecord { Word0 = 0x41626F75, Word1 = 0x746D7569, Word2 = 0x2E6D7569 },
			new MuiMiscSpecialistClassNameRecord { Word0 = 0x4D636370, Word1 = 0x72656673, Word2 = 0x2E6D7569 },
			new MuiMiscSpecialistClassNameRecord { Word0 = 0x5469746C, Word1 = 0x652E6D75, Word2 = 0x69000000 },
		};
		var sizes = new uint[] { 14, 10, 14, 16, 21, 16, 14, 13, 13, 10 };
		var classes = new[]
		{
			MuiMiscSpecialistClass.Keyadjust, MuiMiscSpecialistClass.Panel,
			MuiMiscSpecialistClass.Filepanel, MuiMiscSpecialistClass.Fontdisplay,
			MuiMiscSpecialistClass.FSProtectionBits, MuiMiscSpecialistClass.Scrmodelist,
			MuiMiscSpecialistClass.Argstring, MuiMiscSpecialistClass.Aboutmui,
			MuiMiscSpecialistClass.Mccprefs, MuiMiscSpecialistClass.Title,
		};

		Assert.Equal(21, Unsafe.SizeOf<MuiMiscSpecialistClassNameRecord>());
		for (var i = 0; i < values.Length; i++)
		{
			Assert.True(MuiMiscSpecialistClassNameRecordCodec.WriteRecord(ref platform,
				addresses[i], sizes[i], values[i]));
			Assert.Equal(classes[i], MuiMiscSpecialistCore.ClassifyName(ref platform,
				addresses[i]));
			Assert.True(MuiMiscSpecialistClassNameRecordCodec.TryReadRecord(ref platform,
				addresses[i], sizes[i], out var roundTrip));
			Assert.Equal(values[i].Word0, roundTrip.Word0);
			Assert.Equal(values[i].Word1, roundTrip.Word1);
			Assert.Equal(values[i].Word2, roundTrip.Word2);
			Assert.Equal(values[i].Word3, roundTrip.Word3);
			Assert.Equal(values[i].Word4, roundTrip.Word4);
		}
		Assert.False(MuiMiscSpecialistClassNameRecordCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x30FF0), 21, out _));
	}

	[Fact]
	public void MiscClassNameSequentialCodecPreservesPartialWordBytes()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			state);
		var value = new MuiMiscSpecialistClassNameRecord
		{
			Word0 = 0xFFFFFFFFu, Word1 = 0x01020304u, Word2 = 0xA5B6C7D8u,
			Word3 = 0x11223344u, Word4 = 0x55667788u, Terminator = 0x5A,
		};
		var cases = new[]
		{
			(10u, 0x2400u), (13u, 0x2440u), (14u, 0x2480u),
			(16u, 0x24C0u), (21u, 0x2500u),
		};
		foreach (var item in cases)
		{
			var address = APTR.FromPointer(item.Item2);
			Assert.True(MuiMiscSpecialistClassNameRecordCodec.WriteRecord(
				ref platform, address, item.Item1, value));
			Assert.True(MuiMiscSpecialistClassNameRecordCodec.TryReadRecord(
				ref platform, address, item.Item1, out var decoded));
			Assert.Equal(value.Word0, decoded.Word0);
			Assert.Equal(value.Word1, decoded.Word1);
			Assert.Equal(value.Word2 & (item.Item1 == 10 ? 0xFFFF0000u :
				0xFFFFFFFFu), decoded.Word2);
			if (item.Item1 >= 13) Assert.Equal(value.Word3 &
				(item.Item1 == 13 ? 0xFF000000u : item.Item1 == 14 ?
				0xFFFF0000u : 0xFFFFFFFFu), decoded.Word3);
			if (item.Item1 == 21)
			{
				Assert.Equal(value.Word4, decoded.Word4);
				Assert.Equal(value.Terminator, decoded.Terminator);
			}
		}
		Assert.False(MuiMiscSpecialistClassNameRecordCodec.WriteRecord(ref platform,
			APTR.FromPointer(0x30FF0), 21, value));
		Assert.False(MuiMiscSpecialistClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x30FF0), 21, out _));
	}
}
