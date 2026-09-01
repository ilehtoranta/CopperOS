/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNumericOutputCursorTests
{
	[Fact]
	public void NumericOutputUsesBoundedNamedWrites()
	{
		Assert.Equal(12u, (uint)Unsafe.SizeOf<MuiNumericOutputByteCursor>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var buffer = APTR.FromPointer(0x1300);
		Assert.Equal(4, MuiCommonControlCore.StringifyValue(ref platform,
			buffer, 8, -123));
		Assert.Equal((byte)'-', platform.ReadUInt8(buffer, 0));
		Assert.Equal((byte)'1', platform.ReadUInt8(buffer, 1));
		Assert.Equal((byte)'2', platform.ReadUInt8(buffer, 2));
		Assert.Equal((byte)'3', platform.ReadUInt8(buffer, 3));
		Assert.Equal((byte)0, platform.ReadUInt8(buffer, 4));
		Assert.Equal(0, MuiCommonControlCore.StringifyValue(ref platform, buffer,
			1, 99));
		Assert.Equal((byte)0, platform.ReadUInt8(buffer, 0));
		Assert.Equal(-1, MuiCommonControlCore.StringifyValue(ref platform,
			APTR.FromPointer(0x30000), 4, 1));
	}

	[Fact]
	public void NumericOutputCursorRejectsMalformedRanges()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var cursor = default(MuiNumericOutputByteCursor);
		cursor.Base = APTR.FromPointer(0x1300);
		cursor.Index = 0;
		cursor.Capacity = 4;
		Assert.True(MuiNumericOutputByteCursorCodec.TryWriteByte(ref platform,
			cursor, (byte)'A'));
		cursor.Index = 4;
		Assert.False(MuiNumericOutputByteCursorCodec.TryWriteByte(ref platform,
			cursor, 0));
		cursor.Capacity = MuiNumericOutputByteCursor.MaximumLength + 1;
		Assert.False(MuiNumericOutputByteCursorCodec.TryWriteByte(ref platform,
			cursor, 0));
		cursor.Capacity = 4;
		cursor.Base = APTR.Null;
		Assert.False(MuiNumericOutputByteCursorCodec.TryWriteByte(ref platform,
			cursor, 0));
		cursor.Base = APTR.FromPointer(uint.MaxValue);
		cursor.Index = 1;
		Assert.False(MuiNumericOutputByteCursorCodec.TryWriteByte(ref platform,
			cursor, 0));
		cursor.Base = APTR.FromPointer(0x30000);
		cursor.Index = 0;
		Assert.False(MuiNumericOutputByteCursorCodec.TryWriteByte(ref platform,
			cursor, 0));
	}
}
