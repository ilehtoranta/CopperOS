using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringEditHookTests
{
	[Fact]
	public void StringEditRecordFieldCursorUsesNamedMixedWidths()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var cursor = default(MuiStringEditRecordFieldCursor);
		cursor.Address = APTR.FromPointer(0x1800);
		cursor.Field = MuiStringEditRecordField.Actions;
		Assert.True(MuiStringEditRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x181Eu, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		cursor.Field = MuiStringEditRecordField.BufferPos;
		Assert.True(MuiStringEditRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out fieldAddress, out fieldSize));
		Assert.Equal(0x181Au, fieldAddress.Raw);
		Assert.Equal(2u, fieldSize);
		Assert.True(MuiStringEditRecordFieldCursorCodec.TryWriteUInt16(ref platform,
			cursor.Address, MuiStringEditRecordField.BufferPos,
			unchecked((ushort)-7)));
		Assert.True(MuiStringEditRecordFieldCursorCodec.TryReadUInt16(ref platform,
			cursor.Address, MuiStringEditRecordField.BufferPos, out var position));
		Assert.Equal(unchecked((ushort)-7), position);
		Assert.False(MuiStringEditRecordFieldCursorCodec.TryReadUInt32(ref platform,
			cursor.Address, MuiStringEditRecordField.BufferPos, out _));
		cursor.Address = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Field = MuiStringEditRecordField.EditOp;
		Assert.False(MuiStringEditRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
	}
}
