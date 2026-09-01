using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiRequesterFormatCursorTests
{
	[Fact]
	public void RequesterFormatByteCursorUsesBoundedNamedReads()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var text = APTR.FromPointer(0x1300);
		platform.WriteUInt8(text, 0, (byte)'%');
		Assert.True(MuiRequesterFormatByteCursorCodec.TryCreate(ref platform,
			text, 1, out var cursor));
		Assert.True(MuiRequesterFormatByteCursorCodec.TryReadByte(ref platform,
			cursor, out var value));
		Assert.Equal((byte)'%', value);
		cursor.Index = cursor.Length;
		Assert.False(MuiRequesterFormatByteCursorCodec.TryReadByte(ref platform,
			cursor, out _));
		Assert.False(MuiRequesterFormatByteCursorCodec.TryCreate(ref platform,
			text, 0, out _));
		Assert.False(MuiRequesterFormatByteCursorCodec.TryCreate(ref platform,
			text, MuiRequesterFormatByteCursor.MaximumLength + 1, out _));
		Assert.True(MuiRequesterFormatByteCursorCodec.TryCreate(ref platform,
			APTR.FromPointer(uint.MaxValue), 1, out var overflowCursor));
		Assert.False(MuiRequesterFormatByteCursorCodec.TryReadByte(ref platform,
			overflowCursor, out _));
		Assert.False(MuiRequesterFormatByteCursorCodec.TryCreate(ref platform,
			APTR.Null, 1, out _));
	}

	[Fact]
	public void RequesterOutputByteCursorUsesBoundedNamedWrites()
	{
		Assert.Equal(12, Unsafe.SizeOf<MuiRequesterOutputByteCursor>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var output = APTR.FromPointer(0x1300);
		var cursor = default(MuiRequesterOutputByteCursor);
		cursor.Base = output;
		cursor.Capacity = 4;
		Assert.True(MuiRequesterOutputByteCursorCodec.TryWriteByte(ref platform,
			cursor, (byte)'R'));
		Assert.Equal((byte)'R', platform.ReadUInt8(output, 0));
		cursor.Index = 4;
		Assert.False(MuiRequesterOutputByteCursorCodec.TryWriteByte(ref platform,
			cursor, 0));
		cursor.Index = 0;
		cursor.Capacity = MuiRequesterOutputByteCursor.MaximumLength + 1;
		Assert.False(MuiRequesterOutputByteCursorCodec.TryWriteByte(ref platform,
			cursor, 0));
		cursor.Capacity = 4;
		cursor.Base = APTR.Null;
		Assert.False(MuiRequesterOutputByteCursorCodec.TryWriteByte(ref platform,
			cursor, 0));
		cursor.Base = APTR.FromPointer(uint.MaxValue);
		cursor.Index = 1;
		Assert.False(MuiRequesterOutputByteCursorCodec.TryWriteByte(ref platform,
			cursor, 0));
	}
}
