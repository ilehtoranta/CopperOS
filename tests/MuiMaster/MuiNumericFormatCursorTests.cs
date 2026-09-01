using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNumericFormatCursorTests
{
	[Fact]
	public void NumericFormatCursorUsesBoundedNamedReads()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var format = APTR.FromPointer(0x1300);
		platform.WriteUInt8(format, 0, (byte)'%');
		Assert.True(MuiNumericFormatByteCursorCodec.TryReadByte(ref platform,
			new MuiNumericFormatByteCursor { Text = format, Index = 0 },
			out var value));
		Assert.Equal((byte)'%', value);
		Assert.False(MuiNumericFormatByteCursorCodec.TryReadByte(ref platform,
			new MuiNumericFormatByteCursor { Text = format,
				Index = MuiNumericFormatByteCursor.MaximumLength }, out _));
		Assert.False(MuiNumericFormatByteCursorCodec.TryReadByte(ref platform,
			new MuiNumericFormatByteCursor { Text = APTR.Null, Index = 0 }, out _));
		Assert.False(MuiNumericFormatByteCursorCodec.TryReadByte(ref platform,
			new MuiNumericFormatByteCursor { Text = APTR.FromPointer(uint.MaxValue),
				Index = 1 }, out _));
		Assert.False(MuiNumericFormatByteCursorCodec.TryReadByte(ref platform,
			new MuiNumericFormatByteCursor { Text = APTR.FromPointer(0x30000),
				Index = 0 }, out _));
	}
}
