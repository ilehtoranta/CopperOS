using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiCStringCursorTests
{
	[Fact]
	public void CStringOperationsUseBoundedNamedCursor()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var left = APTR.FromPointer(0x1300);
		var right = APTR.FromPointer(0x1340);
		var later = APTR.FromPointer(0x1380);
		platform.WriteCString(left, "Copper");
		platform.WriteCString(right, "Copper");
		platform.WriteCString(later, "CopperOS");

		Assert.True(CStringCodec.TryReadLength(ref platform, left, 64,
			out var length));
		Assert.Equal(6u, length);
		Assert.True(CStringCodec.TryEquals(ref platform, left, right, 64,
			out var equal));
		Assert.True(equal);
		Assert.True(CStringCodec.TryEquals(ref platform, left, later, 64,
			out equal));
		Assert.False(equal);
		Assert.True(CStringCodec.TryCompare(ref platform, left, later, 64,
			out var comparison));
		Assert.Equal(-1, comparison);
		Assert.True(CStringCodec.TryCompare(ref platform, later, left, 64,
			out comparison));
		Assert.Equal(1, comparison);

		var cursor = default(MuiCStringByteCursor);
		cursor.Base = left;
		cursor.Limit = 7;
		cursor.Index = 6;
		Assert.True(MuiCStringByteCursorCodec.TryReadByte(ref platform, cursor,
			out var terminator));
		Assert.Equal(0, terminator);
		Assert.Equal(12, Unsafe.SizeOf<MuiCStringByteCursor>());
	}

	[Fact]
	public void CStringCursorRejectsMalformedBoundsAndPointers()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var source = APTR.FromPointer(0x1400);
		platform.WriteCString(source, "x");
		var cursor = default(MuiCStringByteCursor);
		cursor.Base = source;
		cursor.Limit = 2;
		cursor.Index = 2;
		Assert.False(MuiCStringByteCursorCodec.TryReadByte(ref platform, cursor,
			out _));
		cursor.Index = 0;
		cursor.Limit = 0;
		Assert.False(MuiCStringByteCursorCodec.TryReadByte(ref platform, cursor,
			out _));
		cursor.Limit = MuiCStringByteCursor.MaximumLength + 1;
		Assert.False(MuiCStringByteCursorCodec.TryReadByte(ref platform, cursor,
			out _));
		cursor.Limit = 2;
		cursor.Base = APTR.FromPointer(uint.MaxValue);
		Assert.False(MuiCStringByteCursorCodec.TryReadByte(ref platform, cursor,
			out _));
		cursor.Base = APTR.FromPointer(0x30000);
		Assert.False(MuiCStringByteCursorCodec.TryReadByte(ref platform, cursor,
			out _));
		Assert.False(CStringCodec.TryReadLength(ref platform, source, 0,
			out _));
		Assert.False(CStringCodec.TryReadLength(ref platform, source,
			MuiCStringByteCursor.MaximumLength + 1, out _));
		Assert.False(CStringCodec.TryEquals(ref platform, APTR.Null, source, 64,
			out _));
		Assert.False(CStringCodec.TryCompare(ref platform, source,
			APTR.FromPointer(0x30000), 64, out _));
	}
}
