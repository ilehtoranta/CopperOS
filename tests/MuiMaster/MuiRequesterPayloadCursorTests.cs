using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiRequesterPayloadCursorTests
{
	[Fact]
	public void RequesterPayloadByteCursorUsesBoundedNamedReads()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var gadgets = APTR.FromPointer(0x1300);
		platform.WriteUInt8(gadgets, 0, (byte)'A');
		Assert.True(MuiRequesterPayloadByteCursorCodec.TryCreate(ref platform,
			gadgets, 1, out var cursor));
		Assert.True(MuiRequesterPayloadByteCursorCodec.TryReadByte(ref platform,
			cursor, out var value));
		Assert.Equal((byte)'A', value);
		cursor.Index = cursor.Length;
		Assert.False(MuiRequesterPayloadByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		Assert.False(MuiRequesterPayloadByteCursorCodec.TryCreate(ref platform,
			gadgets, 0, out _));
		Assert.False(MuiRequesterPayloadByteCursorCodec.TryCreate(ref platform,
			gadgets, MuiRequesterPayloadByteCursor.MaximumLength + 1, out _));
		Assert.True(MuiRequesterPayloadByteCursorCodec.TryCreate(ref platform,
			APTR.FromPointer(uint.MaxValue), 1, out var overflowCursor));
		Assert.False(MuiRequesterPayloadByteCursorCodec.TryReadByte(ref platform,
			overflowCursor, out _));
		Assert.False(MuiRequesterPayloadByteCursorCodec.TryCreate(ref platform,
			APTR.Null, 1, out _));
	}
}
