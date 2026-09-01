/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiTextScanCursorTests
{
	[Fact]
	public void TextScanUsesBoundedNamedReads()
	{
		Assert.Equal(8u, (uint)Unsafe.SizeOf<MuiTextScanByteCursor>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1300);
		platform.WriteCString(text, "abc");
		var cursor = default(MuiTextScanByteCursor);
		cursor.Text = text;
		cursor.Index = 0;
		Assert.True(MuiTextScanByteCursorCodec.TryReadByte(ref platform,
			cursor, out var value));
		Assert.Equal((byte)'a', value);
		cursor.Index = 3;
		Assert.True(MuiTextScanByteCursorCodec.TryReadByte(ref platform,
			cursor, out value));
		Assert.Equal((byte)0, value);
		cursor.Index = MuiTextScanByteCursor.MaximumLength;
		Assert.False(MuiTextScanByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Text = APTR.Null;
		cursor.Index = 0;
		Assert.False(MuiTextScanByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Text = APTR.FromPointer(uint.MaxValue);
		cursor.Index = 1;
		Assert.False(MuiTextScanByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Text = APTR.FromPointer(0x30000);
		cursor.Index = 0;
		Assert.False(MuiTextScanByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
	}

	[Fact]
	public void VisibleScanUsesCursorForEscapesAndLines()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1380);
		platform.WriteCString(text, "abc\nxy");
		Assert.True(MuiCommonControlCore.TryMeasureTextSpan(ref platform,
			APTR.Null, text, 6, false, out var maxColumns, out var lineCount));
		Assert.Equal(3, maxColumns);
		Assert.Equal(2, lineCount);

		platform.WriteCString(text, "\x1bI[abc]xy");
		Assert.True(MuiCommonControlCore.TryMeasureTextSpan(ref platform,
			APTR.Null, text, 9, false, out maxColumns, out lineCount));
		Assert.Equal(2, maxColumns);
		Assert.Equal(1, lineCount);
		Assert.False(MuiCommonControlCore.TryMeasureTextSpan(ref platform,
			APTR.Null, text, -1, false, out _, out _));
	}
}
