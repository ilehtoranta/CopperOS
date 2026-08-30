/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNewMenuTypeStructAdapterTests
{
	[Fact]
	public void NewMenuTypeUsesNamedPrefixAndPreservesMorphosImageRejection()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);

		Assert.Equal(1, Unsafe.SizeOf<MuiNewMenuTypeRecord>());
		platform.WriteUInt8(address, 0, MuiNewMenuTypeRecord.End);
		Assert.True(MuiNewMenuTypeRecordCodec.TryReadRecord(ref platform, address,
			out var endRecord));
		Assert.Equal(MuiNewMenuTypeRecord.End, endRecord.Type);
		Assert.True(MuiNewMenuTypeRecordCodec.TryClassify(ref platform, address,
			out var kind));
		Assert.Equal(MuiNewMenuEntryKind.End, kind);

		platform.WriteUInt8(address, 0, MuiNewMenuTypeRecord.Title);
		Assert.True(MuiNewMenuTypeRecordCodec.TryClassify(ref platform, address,
			out kind));
		Assert.Equal(MuiNewMenuEntryKind.Title, kind);

		platform.WriteUInt8(address, 0, MuiNewMenuTypeRecord.Item);
		Assert.True(MuiNewMenuTypeRecordCodec.TryClassify(ref platform, address,
			out kind));
		Assert.Equal(MuiNewMenuEntryKind.Item, kind);

		platform.WriteUInt8(address, 0, MuiNewMenuTypeRecord.Sub);
		Assert.True(MuiNewMenuTypeRecordCodec.TryClassify(ref platform, address,
			out kind));
		Assert.Equal(MuiNewMenuEntryKind.Sub, kind);

		platform.WriteUInt8(address, 0, MuiNewMenuTypeRecord.Ignore |
			MuiNewMenuTypeRecord.Item);
		Assert.True(MuiNewMenuTypeRecordCodec.TryClassify(ref platform, address,
			out kind));
		Assert.Equal(MuiNewMenuEntryKind.Ignored, kind);

		platform.WriteUInt8(address, 0, MuiNewMenuTypeRecord.Item |
			MuiNewMenuTypeRecord.Image);
		Assert.True(MuiNewMenuTypeRecordCodec.TryClassify(ref platform, address,
			out kind));
		Assert.Equal(MuiNewMenuEntryKind.ImageItem, kind);

		platform.WriteUInt8(address, 0, MuiNewMenuTypeRecord.Sub |
			MuiNewMenuTypeRecord.Image);
		Assert.True(MuiNewMenuTypeRecordCodec.TryClassify(ref platform, address,
			out kind));
		Assert.Equal(MuiNewMenuEntryKind.ImageSub, kind);

		platform.WriteUInt8(address, 0, MuiNewMenuTypeRecord.Title |
			MuiNewMenuTypeRecord.Image);
		Assert.True(MuiNewMenuTypeRecordCodec.TryClassify(ref platform, address,
			out kind));
		Assert.Equal(MuiNewMenuEntryKind.ImageUnsupported, kind);

		platform.WriteUInt8(address, 0, 4);
		Assert.False(MuiNewMenuTypeRecordCodec.TryClassify(ref platform, address,
			out kind));
		Assert.Equal(MuiNewMenuEntryKind.Invalid, kind);
		Assert.False(MuiNewMenuTypeRecordCodec.TryClassify(ref platform,
			APTR.FromPointer(0x21000), out _));
	}
}
