using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListViewportStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedViewportStateRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListViewportState
		{
			Magic = MuiListCore.MuiListViewportState.Cookie,
			TopPixel = 10,
			VisiblePixel = 120,
			TotalPixel = 960,
			First = 2,
			LineHeight = 16,
			Visible = 8,
			DropMark = 3,
		};
		Assert.True(MuiListCore.MuiListViewportStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListViewportStateFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListViewportStateField.DropMark,
		};
		Assert.True(MuiListCore.MuiListViewportStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x351Cu, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListViewportStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListViewportStateField.Visible, 9));
		Assert.True(MuiListCore.MuiListViewportStateFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListViewportStateField.LineHeight, out var height));
		Assert.Equal(16u, height);
		Assert.True(MuiListCore.MuiListViewportStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.TotalPixel, decoded.TotalPixel);
		Assert.Equal(value.DropMark, decoded.DropMark);
		Assert.Equal(9u, decoded.Visible);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListViewportStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListViewportStateFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListViewportStateField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListViewportStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListViewportStateFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListViewportStateField.Magic,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListViewportStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListCore.MuiListViewportStateField.Magic,
			out _, out _));
	}
}
