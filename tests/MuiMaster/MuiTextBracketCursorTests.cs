/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiTextBracketCursorTests
{
	[Fact]
	public void TextBracketPayloadUsesBoundedNamedReads()
	{
		Assert.Equal(8u, (uint)Unsafe.SizeOf<MuiTextBracketByteCursor>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1300);
		platform.WriteCString(text, "I[abc]");
		var cursor = default(MuiTextBracketByteCursor);
		cursor.Text = text;
		cursor.Index = 0;
		Assert.True(MuiTextBracketByteCursorCodec.TryReadByte(ref platform,
			cursor, out var value));
		Assert.Equal((byte)'I', value);
		cursor.Index = 1;
		Assert.True(MuiTextBracketByteCursorCodec.TryReadByte(ref platform,
			cursor, out value));
		Assert.Equal((byte)'[', value);
		Assert.True(MuiCommonControlCore.TryReadBracketedPayload(ref platform,
			text, 0, out var start, out var length));
		Assert.Equal(2, start);
		Assert.Equal(3, length);
		cursor.Index = MuiTextBracketByteCursor.MaximumLength;
		Assert.False(MuiTextBracketByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Text = APTR.Null;
		cursor.Index = 0;
		Assert.False(MuiTextBracketByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Text = APTR.FromPointer(uint.MaxValue);
		cursor.Index = 1;
		Assert.False(MuiTextBracketByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Text = APTR.FromPointer(0x30000);
		cursor.Index = 0;
		Assert.False(MuiTextBracketByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		Assert.False(MuiCommonControlCore.TryReadBracketedPayload(ref platform,
			text, -2, out _, out _));
	}

	[Fact]
	public void TextBracketSpecConsumptionUsesBoundedCursor()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1380);
		platform.WriteCString(text, "I[abc]");
		Assert.Equal(6, MuiCommonControlCore.ConsumeBracketedSpec(ref platform,
			text, 0));
		Assert.Equal(0, MuiCommonControlCore.ConsumeBracketedSpec(ref platform,
			text, -1));
		platform.WriteCString(text, "I[abc");
		Assert.Equal(6, MuiCommonControlCore.ConsumeBracketedSpec(ref platform,
			text, 0));
	}
}
