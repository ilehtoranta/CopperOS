using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiMakeObjectControlCharCursorTests
{
	[Fact]
	public void MakeObjectControlCharCursorUsesBoundedNamedReads()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1300);
		platform.WriteUInt8(text, 0, (byte)'_');
		platform.WriteUInt8(text, 1, (byte)'X');
		var cursor = default(MuiMakeObjectControlCharByteCursor);
		cursor.Text = text;
		cursor.Index = 1;
		Assert.True(MuiMakeObjectControlCharByteCursorCodec.TryReadByte(
			ref platform, cursor, out var value));
		Assert.Equal((byte)'X', value);
		cursor.Index = MuiMakeObjectControlCharByteCursor.MaximumLength;
		Assert.False(MuiMakeObjectControlCharByteCursorCodec.TryReadByte(
			ref platform, cursor, out _));
		cursor.Text = APTR.Null;
		cursor.Index = 0;
		Assert.False(MuiMakeObjectControlCharByteCursorCodec.TryReadByte(
			ref platform, cursor, out _));
		cursor.Text = APTR.FromPointer(uint.MaxValue);
		cursor.Index = 1;
		Assert.False(MuiMakeObjectControlCharByteCursorCodec.TryReadByte(
			ref platform, cursor, out _));
		cursor.Text = APTR.FromPointer(0x30000);
		cursor.Index = 0;
		Assert.False(MuiMakeObjectControlCharByteCursorCodec.TryReadByte(
			ref platform, cursor, out _));
	}
}
