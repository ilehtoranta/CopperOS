using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaLayoutCursorTests
{
	[Fact]
	public void AreaLayoutCStringCursorUsesBoundedNamedReads()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1300);
		platform.WriteUInt8(text, 0, (byte)'T');
		Assert.True(MuiAreaLayoutCStringByteCursorCodec.TryReadByte(ref platform,
			new MuiAreaLayoutCStringByteCursor { Text = text, Index = 0 },
			out var value));
		Assert.Equal((byte)'T', value);
		Assert.False(MuiAreaLayoutCStringByteCursorCodec.TryReadByte(ref platform,
			new MuiAreaLayoutCStringByteCursor { Text = text,
				Index = MuiAreaLayoutCStringByteCursor.MaximumLength }, out _));
		Assert.False(MuiAreaLayoutCStringByteCursorCodec.TryReadByte(ref platform,
			new MuiAreaLayoutCStringByteCursor { Text = APTR.Null, Index = 0 },
			out _));
		Assert.False(MuiAreaLayoutCStringByteCursorCodec.TryReadByte(ref platform,
			new MuiAreaLayoutCStringByteCursor { Text = APTR.FromPointer(uint.MaxValue),
				Index = 1 }, out _));
		Assert.False(MuiAreaLayoutCStringByteCursorCodec.TryReadByte(ref platform,
			new MuiAreaLayoutCStringByteCursor { Text = APTR.FromPointer(0x30000),
				Index = 0 }, out _));
	}
}
