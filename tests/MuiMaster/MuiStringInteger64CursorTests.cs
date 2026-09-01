using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringInteger64CursorTests
{
	[Fact]
	public void StringInteger64TextCursorUsesBoundedNamedReadWrite()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var source = APTR.FromPointer(0x1300);
		platform.WriteCString(source, "-123");
		var cursor = default(MuiStringInteger64TextByteCursor);
		cursor.Base = source;
		cursor.Length = 5;
		cursor.Index = 1;
		Assert.True(MuiStringInteger64TextByteCursorCodec.TryReadByte(ref platform,
			cursor, out var value));
		Assert.Equal((byte)'1', value);
		Assert.True(MuiStringInteger64TextByteCursorCodec.TryReadAt(ref platform,
			source, 5, 4, out value));
		Assert.Equal(0, value);
		var destination = APTR.FromPointer(0x1340);
		cursor.Base = destination;
		cursor.Length = 4;
		cursor.Index = 0;
		Assert.True(MuiStringInteger64TextByteCursorCodec.TryWriteByte(ref platform,
			cursor, (byte)'x'));
		Assert.Equal((byte)'x', platform.ReadUInt8(destination, 0));
		Assert.False(MuiStringInteger64TextByteCursorCodec.TryReadAt(ref platform,
			source, 5, -1, out _));
		Assert.False(MuiStringInteger64TextByteCursorCodec.TryReadAt(ref platform,
			source, 5, 5, out _));
		Assert.False(MuiStringInteger64TextByteCursorCodec.TryReadAt(ref platform,
			source, MuiStringInteger64TextByteCursor.MaximumLength + 1, 0,
			out _));
		Assert.False(MuiStringInteger64TextByteCursorCodec.TryReadAt(ref platform,
			APTR.FromPointer(uint.MaxValue - 1u), 4, 2, out _));
		Assert.False(MuiStringInteger64TextByteCursorCodec.TryReadAt(ref platform,
			APTR.FromPointer(0x30000), 4, 0, out _));
		Assert.False(MuiStringInteger64TextByteCursorCodec.TryReadAt(ref platform,
			APTR.Null, 4, 0, out _));
	}
}
