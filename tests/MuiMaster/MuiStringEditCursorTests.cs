/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringEditCursorTests
{
	[Fact]
	public void StringEditCursorUsesBoundedNamedReadWrite()
	{
		Assert.Equal(12u, (uint)Unsafe.SizeOf<MuiStringEditByteCursor>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1300);
		platform.WriteCString(text, "abc");
		var cursor = default(MuiStringEditByteCursor);
		cursor.Base = text;
		cursor.Index = 1;
		cursor.Length = 4;
		Assert.True(MuiStringEditByteCursorCodec.TryReadByte(ref platform,
			cursor, out var value));
		Assert.Equal((byte)'b', value);
		cursor.Index = 3;
		Assert.True(MuiStringEditByteCursorCodec.TryWriteByte(ref platform,
			cursor, (byte)'X'));
		Assert.Equal((byte)'X', platform.ReadUInt8(text, 3));
		cursor.Index = 4;
		Assert.False(MuiStringEditByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Length = MuiStringEditByteCursor.MaximumLength + 1;
		Assert.False(MuiStringEditByteCursorCodec.TryWriteByte(ref platform,
			cursor, 0));
		cursor.Length = 4;
		cursor.Base = APTR.Null;
		Assert.False(MuiStringEditByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Base = APTR.FromPointer(uint.MaxValue);
		cursor.Index = 1;
		Assert.False(MuiStringEditByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Base = APTR.FromPointer(0x30000);
		cursor.Index = 0;
		Assert.False(MuiStringEditByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
	}

	[Fact]
	public void StringCodePointReadUsesEditCursor()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1380);
		platform.WriteCString(text, "abc");
		Assert.True(MuiCommonControlCore.TryReadStringCodePoint(ref platform,
			text, 1, 3, false, out var codePoint, out var bytes));
		Assert.Equal((uint)'b', codePoint);
		Assert.Equal(1u, bytes);
		Assert.False(MuiCommonControlCore.TryReadStringCodePoint(ref platform,
			text, 3, 3, false, out _, out _));
		Assert.False(MuiCommonControlCore.TryReadStringCodePoint(ref platform,
			APTR.FromPointer(0x30000), 0, 1, false, out _, out _));
	}

	[Fact]
	public void Utf8CharacterWritesUseEditCursor()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var destination = APTR.FromPointer(0x1400);
		var encoded = default(MuiCommonControlCore.MuiUtf8Character);
		encoded.Length = 4;
		encoded.First = 0xF0;
		encoded.Second = 0x9F;
		encoded.Third = 0x98;
		encoded.Fourth = 0x80;
		MuiCommonControlCore.WriteUtf8Character(ref platform, destination, 2,
			encoded);
		Assert.Equal((byte)0xF0, platform.ReadUInt8(destination, 2));
		Assert.Equal((byte)0x9F, platform.ReadUInt8(destination, 3));
		Assert.Equal((byte)0x98, platform.ReadUInt8(destination, 4));
		Assert.Equal((byte)0x80, platform.ReadUInt8(destination, 5));
		MuiCommonControlCore.WriteUtf8Character(ref platform, destination,
			-1, encoded);
		Assert.False(MuiStringEditByteCursorCodec.TryReadByte(ref platform,
			new MuiStringEditByteCursor
			{
				Base = APTR.FromPointer(0x30000), Index = 0, Length = 1,
			}, out _));
	}
}
