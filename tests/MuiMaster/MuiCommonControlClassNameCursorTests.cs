using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiCommonControlClassNameCursorTests
{
	[Fact]
	public void CommonControlClassNameCursorUsesBoundedNamedReads()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1300);
		platform.WriteUInt8(text, 0, (byte)'T');
		var cursor = new MuiCommonControlClassNameByteCursor
		{
			Text = text,
			Index = 0
		};
		Assert.True(MuiCommonControlClassNameByteCursorCodec.TryReadByte(ref platform,
			cursor, out var value));
		Assert.Equal((byte)'T', value);
		cursor.Index = MuiCommonControlClassNameByteCursor.MaximumLength;
		Assert.False(MuiCommonControlClassNameByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Text = APTR.Null;
		cursor.Index = 0;
		Assert.False(MuiCommonControlClassNameByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Text = APTR.FromPointer(uint.MaxValue);
		cursor.Index = 1;
		Assert.False(MuiCommonControlClassNameByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Text = APTR.FromPointer(0x30000);
		cursor.Index = 0;
		Assert.False(MuiCommonControlClassNameByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
	}
}
