using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiByteRunCursorTests
{
	[Fact]
	public void ByteRunCursorUsesBoundedNamedReadWriteAndRanges()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var source = APTR.FromPointer(0x1300);
		platform.WriteUInt8(source, 0, 0x02);
		platform.WriteUInt8(source, 1, 0x11);
		platform.WriteUInt8(source, 2, 0x22);
		platform.WriteUInt8(source, 3, 0x33);
		var cursor = default(MuiByteRunByteCursor);
		cursor.Base = source;
		cursor.Length = 4;
		cursor.Index = 1;
		Assert.True(MuiByteRunByteCursorCodec.TryReadByte(ref platform, cursor,
			out var value));
		Assert.Equal(0x11, value);
		cursor.Index = 1;
		Assert.True(MuiByteRunByteCursorCodec.TryGetRange(ref platform, cursor,
			3, out var range));
		Assert.Equal(source.Raw + 1, range.Raw);
		var destination = APTR.FromPointer(0x1340);
		cursor.Base = destination;
		cursor.Length = 4;
		cursor.Index = 2;
		Assert.True(MuiByteRunByteCursorCodec.TryWriteByte(ref platform, cursor,
			0x7F));
		Assert.Equal(0x7F, platform.ReadUInt8(destination, 2));
		Assert.False(MuiByteRunByteCursorCodec.TryReadAt(ref platform, source, 4,
			-1, out _));
		Assert.False(MuiByteRunByteCursorCodec.TryReadAt(ref platform, source, 4,
			4, out _));
		Assert.False(MuiByteRunByteCursorCodec.TryReadAt(ref platform, source,
			MuiByteRunByteCursor.MaximumLength + 1, 0, out _));
		Assert.False(MuiByteRunByteCursorCodec.TryReadAt(ref platform,
			APTR.FromPointer(uint.MaxValue - 1u), 4, 2, out _));
		Assert.False(MuiByteRunByteCursorCodec.TryReadAt(ref platform,
			APTR.FromPointer(0x30000), 4, 0, out _));
		Assert.False(MuiByteRunByteCursorCodec.TryReadAt(ref platform,
			APTR.Null, 4, 0, out _));
	}
}
