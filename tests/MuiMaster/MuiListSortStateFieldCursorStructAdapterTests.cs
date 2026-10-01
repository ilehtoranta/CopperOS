using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListSortStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedSortStateRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListSortState
		{
			Magic = MuiListCore.MuiListSortState.Cookie,
			SortColumn = 2,
			TitleClick = 3,
		};
		Assert.True(MuiListCore.MuiListSortStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListSortStateFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListSortStateField.TitleClick,
		};
		Assert.True(MuiListCore.MuiListSortStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3508u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListSortStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListSortStateField.SortColumn, 8));
		Assert.True(MuiListCore.MuiListSortStateFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListSortStateField.TitleClick, out var titleClick));
		Assert.Equal(value.TitleClick, titleClick);
		Assert.True(MuiListCore.MuiListSortStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(8u, decoded.SortColumn);
		Assert.Equal(value.TitleClick, decoded.TitleClick);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListSortStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListSortStateFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListSortStateField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListSortStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListSortStateFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListSortStateField.Magic,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListSortStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListCore.MuiListSortStateField.Magic,
			out _, out _));
	}
}
