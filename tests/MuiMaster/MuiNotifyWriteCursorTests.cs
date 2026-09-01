using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNotifyWriteCursorTests
{
	[Fact]
	public void NotifyWriteByteCursorUsesBoundedNamedReadWrite()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var source = APTR.FromPointer(0x1300);
		var destination = APTR.FromPointer(0x1340);
		platform.WriteUInt8(source, 0, (byte)'X');
		Assert.True(MuiNotifyWriteByteCursorCodec.TryCreate(ref platform,
			source, 1, out var sourceCursor));
		Assert.True(MuiNotifyWriteByteCursorCodec.TryCreate(ref platform,
			destination, 1, out var destinationCursor));
		Assert.True(MuiNotifyWriteByteCursorCodec.TryReadByte(ref platform,
			sourceCursor, out var value));
		Assert.Equal((byte)'X', value);
		Assert.True(MuiNotifyWriteByteCursorCodec.TryWriteByte(ref platform,
			destinationCursor, value));
		Assert.Equal((byte)'X', platform.ReadUInt8(destination, 0));
		Assert.False(MuiNotifyWriteByteCursorCodec.TryCreate(ref platform,
			source, 0, out _));
		Assert.False(MuiNotifyWriteByteCursorCodec.TryCreate(ref platform,
			source, MuiNotifyWriteByteCursor.MaximumLength + 1, out _));
		sourceCursor.Index = sourceCursor.Length;
		Assert.False(MuiNotifyWriteByteCursorCodec.TryReadByte(ref platform,
			sourceCursor, out _));
		Assert.True(MuiNotifyWriteByteCursorCodec.TryCreate(ref platform,
			APTR.FromPointer(uint.MaxValue), 1, out var overflowCursor));
		Assert.False(MuiNotifyWriteByteCursorCodec.TryReadByte(ref platform,
			overflowCursor, out _));
		Assert.False(MuiNotifyWriteByteCursorCodec.TryCreate(ref platform,
			APTR.Null, 1, out _));
	}
}
