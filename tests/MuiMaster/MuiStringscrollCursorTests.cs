using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringscrollCursorTests
{
	[Fact]
	public void StringscrollTextCursorUsesBoundedNamedReads()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1300);
		platform.WriteUInt8(text, 0, (byte)'A');
		platform.WriteUInt8(text, 1, 0xC3);
		platform.WriteUInt8(text, 2, 0xA4);
		platform.WriteUInt8(text, 3, (byte)'Z');
		platform.WriteUInt8(text, 4, 0);
		Assert.True(MuiStringscrollTextByteCursorCodec.TryCreate(ref platform,
			text, 4, out var cursor));
		Assert.True(MuiStringscrollTextByteCursorCodec.TryReadAt(ref platform,
			cursor, 0, out var value));
		Assert.Equal((byte)'A', value);
		Assert.True(MuiStringscrollTextByteCursorCodec.TryReadAt(ref platform,
			cursor, 1, out value));
		Assert.Equal(0xC3, value);
		Assert.False(MuiStringscrollTextByteCursorCodec.TryReadAt(ref platform,
			cursor, 4, out _));
		Assert.False(MuiStringscrollTextByteCursorCodec.TryCreate(ref platform,
			text, MuiStringscrollTextByteCursor.MaximumLength + 1, out _));
		Assert.True(MuiStringscrollTextByteCursorCodec.TryCreate(ref platform,
			APTR.FromPointer(uint.MaxValue), 1, out var overflowCursor));
		Assert.False(MuiStringscrollTextByteCursorCodec.TryReadAt(ref platform,
			overflowCursor, 0, out _));
		Assert.False(MuiStringscrollTextByteCursorCodec.TryCreate(ref platform,
			APTR.Null, 1, out _));
		Assert.True(MuiStringscrollTextByteCursorCodec.TryCreate(ref platform,
			APTR.FromPointer(0x30000), 1, out var unmappedCursor));
		Assert.False(MuiStringscrollTextByteCursorCodec.TryReadAt(ref platform,
			unmappedCursor, 0, out _));
	}
}
