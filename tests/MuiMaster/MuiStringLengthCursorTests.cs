/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringLengthCursorTests
{
	[Fact]
	public void StringLengthCursorUsesBoundedNamedReads()
	{
		Assert.Equal(8u, (uint)Unsafe.SizeOf<MuiStringLengthByteCursor>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1300);
		platform.WriteCString(text, "abc");
		var cursor = default(MuiStringLengthByteCursor);
		cursor.Text = text;
		cursor.Index = 0;
		Assert.True(MuiStringLengthByteCursorCodec.TryReadByte(ref platform,
			cursor, out var value));
		Assert.Equal((byte)'a', value);
		cursor.Index = 3;
		Assert.True(MuiStringLengthByteCursorCodec.TryReadByte(ref platform,
			cursor, out value));
		Assert.Equal((byte)0, value);
		cursor.Index = MuiStringLengthByteCursor.MaximumLength;
		Assert.False(MuiStringLengthByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Text = APTR.Null;
		cursor.Index = 0;
		Assert.False(MuiStringLengthByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Text = APTR.FromPointer(uint.MaxValue);
		cursor.Index = 1;
		Assert.False(MuiStringLengthByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Text = APTR.FromPointer(0x30000);
		cursor.Index = 0;
		Assert.False(MuiStringLengthByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
	}
}
