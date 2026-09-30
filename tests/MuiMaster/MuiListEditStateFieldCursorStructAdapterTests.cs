using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListEditStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedEditStateRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListEditState
		{
			Magic = MuiListCore.MuiListEditState.Cookie,
			Row = -2,
			Column = 3,
			Entry = APTR.FromPointer(0x4567),
			EditObject = APTR.FromPointer(0x5678),
			Flags = 0x80000001u,
		};
		Assert.True(MuiListCore.MuiListEditStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListEditFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListEditField.Column,
		};
		Assert.True(MuiListCore.MuiListEditFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3508u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListEditFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiListCore.MuiListEditField.Column, 9));
		Assert.True(MuiListCore.MuiListEditFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiListCore.MuiListEditField.Row, out var row));
		Assert.Equal(unchecked((uint)-2), row);
		Assert.True(MuiListCore.MuiListEditStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(-2, decoded.Row);
		Assert.Equal(9, decoded.Column);
		Assert.Equal(value.Entry, decoded.Entry);
		Assert.Equal(value.EditObject, decoded.EditObject);
		Assert.Equal(value.Flags, decoded.Flags);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListEditFieldCursorCodec.TryGetAddress(
			ref platform,
			new MuiListCore.MuiListEditFieldCursor
			{
				Address = address,
				Field = (MuiListCore.MuiListEditField)255,
			}, out _, out _));
		Assert.False(MuiListCore.MuiListEditFieldCursorCodec.TryGetAddress(
			ref platform,
			new MuiListCore.MuiListEditFieldCursor
			{
				Address = APTR.FromPointer(0x30FF9),
				Field = MuiListCore.MuiListEditField.Magic,
			}, out _, out _));
		Assert.False(MuiListCore.MuiListEditMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListCore.MuiListEditField.Magic,
			out _, out _));
	}
}
