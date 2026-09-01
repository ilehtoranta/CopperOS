using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiDirlistMatchCursorTests
{
	[Fact]
	public void DirlistMatchCursorUsesBoundedNamedReads()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1300);
		platform.WriteUInt8(text, 0, (byte)'A');
		var cursor = new MuiDirlistMatchByteCursor
		{
			Base = text,
			Index = 0,
			Length = 1
		};
		Assert.True(MuiDirlistMatchByteCursorCodec.TryReadByte(ref platform,
			cursor, out var value));
		Assert.Equal((byte)'A', value);
		cursor.Index = cursor.Length;
		Assert.False(MuiDirlistMatchByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Length = MuiDirlistMatchByteCursor.MaximumLength + 1;
		cursor.Index = 0;
		Assert.False(MuiDirlistMatchByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Length = 1;
		cursor.Base = APTR.Null;
		Assert.False(MuiDirlistMatchByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Base = APTR.FromPointer(uint.MaxValue);
		cursor.Index = 1;
		Assert.False(MuiDirlistMatchByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		cursor.Base = APTR.FromPointer(0x30000);
		cursor.Index = 0;
		Assert.False(MuiDirlistMatchByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
	}
}
