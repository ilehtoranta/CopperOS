/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGuestByteCopyCursorTests
{
	[Fact]
	public void GuestByteCopyUsesBoundedNamedSourceAndDestination()
	{
		Assert.Equal(16u, (uint)Unsafe.SizeOf<MuiGuestByteCopyCursor>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var source = APTR.FromPointer(0x1300);
		var destination = APTR.FromPointer(0x1400);
		platform.WriteUInt8(source, 0, (byte)'C');
		platform.WriteUInt8(source, 1, (byte)'o');
		platform.WriteUInt8(source, 2, (byte)'p');
		platform.WriteUInt8(source, 3, 0);
		var cursor = new MuiGuestByteCopyCursor
		{
			Source = source,
			Destination = destination,
			Length = 4
		};
		for (cursor.Index = 0; cursor.Index < cursor.Length; cursor.Index++)
			Assert.True(MuiGuestByteCopyCursorCodec.TryCopyByte(ref platform, cursor));
		Assert.Equal((byte)'C', platform.ReadUInt8(destination, 0));
		Assert.Equal((byte)'o', platform.ReadUInt8(destination, 1));
		Assert.Equal((byte)'p', platform.ReadUInt8(destination, 2));
		Assert.Equal((byte)0, platform.ReadUInt8(destination, 3));
	}

	[Fact]
	public void GuestByteCopyCursorRejectsMalformedRanges()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var cursor = new MuiGuestByteCopyCursor
		{
			Source = APTR.FromPointer(0x1300),
			Destination = APTR.FromPointer(0x1400),
			Length = 4
		};
		cursor.Index = 4;
		Assert.False(MuiGuestByteCopyCursorCodec.TryCopyByte(ref platform, cursor));
		cursor.Index = 0;
		cursor.Length = MuiGuestByteCopyCursor.MaximumLength + 1;
		Assert.False(MuiGuestByteCopyCursorCodec.TryCopyByte(ref platform, cursor));
		cursor.Length = 4;
		cursor.Source = APTR.Null;
		Assert.False(MuiGuestByteCopyCursorCodec.TryCopyByte(ref platform, cursor));
		cursor.Source = APTR.FromPointer(0x1300);
		cursor.Destination = APTR.Null;
		Assert.False(MuiGuestByteCopyCursorCodec.TryCopyByte(ref platform, cursor));
		cursor.Destination = APTR.FromPointer(0x1400);
		cursor.Source = APTR.FromPointer(uint.MaxValue);
		cursor.Index = 1;
		Assert.False(MuiGuestByteCopyCursorCodec.TryCopyByte(ref platform, cursor));
		cursor.Source = APTR.FromPointer(0x30000);
		cursor.Index = 0;
		Assert.False(MuiGuestByteCopyCursorCodec.TryCopyByte(ref platform, cursor));
		cursor.Source = APTR.FromPointer(0x1300);
		cursor.Destination = APTR.FromPointer(0x30000);
		Assert.False(MuiGuestByteCopyCursorCodec.TryCopyByte(ref platform, cursor));
	}
}
