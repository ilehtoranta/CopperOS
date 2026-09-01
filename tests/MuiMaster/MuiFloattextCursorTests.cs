using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiFloattextCursorTests
{
	[Fact]
	public void FloattextByteCursorUsesBoundedNamedReadWrite()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1300);
		platform.WriteCString(text, "Float");
		var cursor = default(MuiFloattextByteCursor);
		cursor.Base = text;
		cursor.Length = 6;
		cursor.Index = 2;
		Assert.True(MuiFloattextByteCursorCodec.TryReadByte(ref platform,
			cursor, out var value));
		Assert.Equal((byte)'o', value);
		Assert.True(MuiFloattextByteCursorCodec.TryReadAt(ref platform, text,
			6, 5, out value));
		Assert.Equal(0, value);
		var destination = APTR.FromPointer(0x1340);
		cursor.Base = destination;
		cursor.Length = 4;
		cursor.Index = 1;
		Assert.True(MuiFloattextByteCursorCodec.TryWriteByte(ref platform,
			cursor, (byte)'x'));
		Assert.Equal((byte)'x', platform.ReadUInt8(destination, 1));
		Assert.False(MuiFloattextByteCursorCodec.TryReadAt(ref platform, text,
			6, -1, out _));
		Assert.False(MuiFloattextByteCursorCodec.TryReadAt(ref platform, text,
			6, 6, out _));
		Assert.False(MuiFloattextByteCursorCodec.TryReadAt(ref platform, text,
			6, unchecked((int)MuiFloattextByteCursor.MaximumLength + 1), out _));
		Assert.False(MuiFloattextByteCursorCodec.TryReadAt(ref platform,
			APTR.FromPointer(uint.MaxValue - 1u), 4, 2, out _));
		Assert.False(MuiFloattextByteCursorCodec.TryReadAt(ref platform,
			APTR.FromPointer(0x30000), 4, 0, out _));
	}
}
