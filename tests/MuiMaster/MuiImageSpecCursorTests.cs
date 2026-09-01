/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiImageSpecCursorTests
{
	[Fact]
	public void ImageSpecPayloadUsesBoundedNamedCursor()
	{
		Assert.Equal(12u, (uint)Unsafe.SizeOf<MuiImageSpecByteCursor>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var spec = APTR.FromPointer(0x1300);
		platform.WriteCString(spec, "2:ff8040");
		var cursor = default(MuiImageSpecByteCursor);
		cursor.Base = spec;
		cursor.Index = 2;
		cursor.Length = MuiImageSpecByteCursor.MaximumLength;
		Assert.True(MuiImageSpecByteCursorCodec.TryReadByte(ref platform,
			cursor, out var value));
		Assert.Equal((byte)'f', value);
		Assert.True(MuiCommonControlCore.TryParseImageSpec(ref platform, spec,
			out var parsed));
		Assert.Equal(MuiImageSpecKind.Color, parsed.Kind);
		Assert.Equal(0xFF8040u, parsed.Value);
		cursor.Index = MuiImageSpecByteCursor.MaximumLength;
		Assert.False(MuiImageSpecByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Base = APTR.Null;
		cursor.Index = 0;
		Assert.False(MuiImageSpecByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Base = APTR.FromPointer(uint.MaxValue);
		cursor.Index = 1;
		Assert.False(MuiImageSpecByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Base = APTR.FromPointer(0x30000);
		cursor.Index = 0;
		Assert.False(MuiImageSpecByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
	}

	[Fact]
	public void ImageSpecPayloadBranchesRemainBounded()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var spec = APTR.FromPointer(0x1380);
		platform.WriteCString(spec, "0:37");
		Assert.True(MuiCommonControlCore.TryParseImageSpec(ref platform, spec,
			out var pattern));
		Assert.Equal(MuiImageSpecKind.BackgroundPattern, pattern.Kind);
		Assert.Equal(37u, pattern.Value);
		platform.WriteCString(spec, "4:brush");
		Assert.True(MuiCommonControlCore.TryParseImageSpec(ref platform, spec,
			out var named));
		Assert.Equal(MuiImageSpecKind.Brush, named.Kind);
		platform.WriteCString(spec, "1:12");
		Assert.True(MuiCommonControlCore.TryParseImageSpec(ref platform, spec,
			out var builtin));
		Assert.Equal(12u, builtin.Value);
		Assert.False(MuiCommonControlCore.TryParseImageSpec(ref platform,
			APTR.FromPointer(0x30000), out _));
	}
}
