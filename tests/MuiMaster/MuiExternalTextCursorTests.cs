using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiExternalTextCursorTests
{
	[Fact]
	public void ExternalTextCursorUsesBoundedNamedReadWrite()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var source = APTR.FromPointer(0x1300);
		var destination = APTR.FromPointer(0x1340);
		platform.WriteUInt8(source, 0, (byte)'D');
		var sourceCursor = new MuiExternalTextByteCursor
		{
			Base = source,
			Index = 0,
			Length = 1
		};
		var destinationCursor = new MuiExternalTextByteCursor
		{
			Base = destination,
			Index = 0,
			Length = 1
		};
		Assert.True(MuiExternalTextByteCursorCodec.TryReadByte(ref platform,
			sourceCursor, out var value));
		Assert.Equal((byte)'D', value);
		Assert.True(MuiExternalTextByteCursorCodec.TryWriteByte(ref platform,
			destinationCursor, value));
		Assert.Equal((byte)'D', platform.ReadUInt8(destination, 0));
		sourceCursor.Index = sourceCursor.Length;
		Assert.False(MuiExternalTextByteCursorCodec.TryReadByte(ref platform,
			sourceCursor, out _));
		sourceCursor.Length = MuiExternalTextByteCursor.MaximumLength + 1;
		sourceCursor.Index = 0;
		Assert.False(MuiExternalTextByteCursorCodec.TryReadByte(ref platform,
			sourceCursor, out _));
		sourceCursor.Length = 1;
		sourceCursor.Base = APTR.FromPointer(uint.MaxValue);
		sourceCursor.Index = 1;
		Assert.False(MuiExternalTextByteCursorCodec.TryReadByte(ref platform,
			sourceCursor, out _));
		sourceCursor.Base = APTR.Null;
		sourceCursor.Index = 0;
		Assert.False(MuiExternalTextByteCursorCodec.TryReadByte(ref platform,
			sourceCursor, out _));
	}
}
