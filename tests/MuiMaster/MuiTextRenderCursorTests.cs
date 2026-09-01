/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiTextRenderCursorTests
{
	[Fact]
	public void RenderCursorUsesBoundedNamedReadWrite()
	{
		Assert.Equal(12u, (uint)Unsafe.SizeOf<MuiTextRenderByteCursor>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1300);
		var buffer = APTR.FromPointer(0x1400);
		platform.WriteCString(text, "abc");
		var cursor = default(MuiTextRenderByteCursor);
		cursor.Base = text;
		cursor.Index = 0;
		cursor.Length = 4;
		Assert.True(MuiTextRenderByteCursorCodec.TryReadByte(ref platform,
			cursor, out var value));
		Assert.Equal((byte)'a', value);
		cursor.Base = buffer;
		cursor.Index = 0;
		Assert.True(MuiTextRenderByteCursorCodec.TryWriteByte(ref platform,
			cursor, (byte)'Z'));
		Assert.Equal((byte)'Z', platform.ReadUInt8(buffer, 0));
		cursor.Index = 4;
		Assert.False(MuiTextRenderByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Length = MuiTextRenderByteCursor.MaximumLength + 1;
		Assert.False(MuiTextRenderByteCursorCodec.TryWriteByte(ref platform,
			cursor, 0));
		cursor.Length = 4;
		cursor.Base = APTR.Null;
		Assert.False(MuiTextRenderByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Base = APTR.FromPointer(uint.MaxValue);
		cursor.Index = 1;
		Assert.False(MuiTextRenderByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Base = APTR.FromPointer(0x30000);
		cursor.Index = 0;
		Assert.False(MuiTextRenderByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
	}

	[Fact]
	public void RenderCursorUsesBoundedNamedSubspans()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1700);
		platform.WriteCString(text, "abcdefgh");
		var cursor = default(MuiTextRenderByteCursor);
		cursor.Base = text;
		cursor.Index = 2;
		cursor.Length = 8;
		Assert.True(MuiTextRenderByteCursorCodec.TryGetRange(ref platform,
			cursor, 3, out var range));
		Assert.Equal(text.Raw + 2, range.Raw);
		Assert.Equal((byte)'c', platform.ReadUInt8(range, 0));
		Assert.Equal((byte)'e', platform.ReadUInt8(range, 2));
		Assert.False(MuiTextRenderByteCursorCodec.TryGetRange(ref platform,
			cursor, 7, out _));
		cursor.Index = cursor.Length;
		Assert.False(MuiTextRenderByteCursorCodec.TryGetRange(ref platform,
			cursor, 1, out _));
		cursor.Index = 0;
		cursor.Length = MuiTextRenderByteCursor.MaximumLength + 1;
		Assert.False(MuiTextRenderByteCursorCodec.TryGetRange(ref platform,
			cursor, 0, out _));
		cursor.Length = 8;
		cursor.Base = APTR.FromPointer(uint.MaxValue);
		cursor.Index = 1;
		Assert.False(MuiTextRenderByteCursorCodec.TryGetRange(ref platform,
			cursor, 0, out _));
		cursor.Base = APTR.FromPointer(0x30000);
		cursor.Index = 0;
		Assert.False(MuiTextRenderByteCursorCodec.TryGetRange(ref platform,
			cursor, 0, out _));
	}

	[Fact]
	public void AppendRenderUsesCursorForSourceAndDestination()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1480);
		var buffer = APTR.FromPointer(0x1500);
		platform.WriteCString(text, "\u001brHi\nX");
		var outIndex = 0;
		var engineOn = true;
		var atLineStart = true;
		var align = 0;
		var frontPen = uint.MaxValue;
		MuiCommonControlCore.AppendRender(ref platform, text, buffer, 32,
			ref outIndex, ref engineOn, ref atLineStart, ref align,
			ref frontPen);
		Assert.Equal(4, outIndex);
		Assert.Equal(2, align);
		Assert.Equal((byte)'H', platform.ReadUInt8(buffer, 0));
		Assert.Equal((byte)'i', platform.ReadUInt8(buffer, 1));
		Assert.Equal((byte)'\n', platform.ReadUInt8(buffer, 2));
		Assert.Equal((byte)'X', platform.ReadUInt8(buffer, 3));

		platform.WriteCString(text, "\u001bI[abc]Z");
		outIndex = 0;
		engineOn = true;
		atLineStart = true;
		align = 0;
		frontPen = uint.MaxValue;
		MuiCommonControlCore.AppendRender(ref platform, text, buffer, 32,
			ref outIndex, ref engineOn, ref atLineStart, ref align,
			ref frontPen);
		Assert.Equal(1, outIndex);
		Assert.Equal((byte)'Z', platform.ReadUInt8(buffer, 0));
	}

	[Fact]
	public void MultilineStringDrawUsesBoundedRenderCursor()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1580);
		platform.WriteCString(text, "abc\ndef");
		MuiCommonControlCore.DrawStringMultilineContent(ref platform,
			APTR.Null, APTR.Null, APTR.FromPointer(0x1600),
			APTR.FromPointer(0x1620), text, 0, 0, 80, 32, 0, false, false);
		Assert.Equal(2u, platform.TextCount);
		Assert.Equal(3, platform.LastTextLength);
		Assert.NotEqual(APTR.Null, platform.FirstText);
		Assert.NotEqual(APTR.Null, platform.SecondText);
	}
}
