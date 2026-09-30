using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewHorizontalScrollerFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedHorizontalScrollerRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewHorizontalScrollerState
		{
			Magic = MuiListviewHorizontalScrollerState.Cookie,
			TrackLeft = -10,
			TrackTop = 2,
			TrackRight = 90,
			TrackBottom = 20,
			ThumbLeft = 5,
			ThumbTop = 6,
			ThumbRight = 30,
			ThumbBottom = 12,
			ContentWidth = 200,
			ViewWidth = 80,
			ScrollX = 7,
			MaxScrollX = 120,
		};
		Assert.True(MuiListviewHorizontalScrollerStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListviewHorizontalScrollerFieldCursor
		{
			Record = address,
			Field = MuiListviewHorizontalScrollerField.MaxScrollX,
		};
		Assert.True(MuiListviewHorizontalScrollerFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3530u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListviewHorizontalScrollerFieldCursorCodec.TryWriteInt32(
			ref platform, address, MuiListviewHorizontalScrollerField.TrackLeft,
			-20));
		Assert.True(MuiListviewHorizontalScrollerFieldCursorCodec.TryReadInt32(
			ref platform, address, MuiListviewHorizontalScrollerField.TrackLeft,
			out var trackLeft));
		Assert.Equal(-20, trackLeft);
		Assert.True(MuiListviewHorizontalScrollerStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(-20, decoded.TrackLeft);
		Assert.Equal(value.ScrollX, decoded.ScrollX);
		Assert.Equal(value.MaxScrollX, decoded.MaxScrollX);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewHorizontalScrollerFieldCursorCodec.TryGetAddress(
			ref platform,
			new MuiListviewHorizontalScrollerFieldCursor
			{
				Record = address,
				Field = (MuiListviewHorizontalScrollerField)255,
			}, out _, out _));
		Assert.False(MuiListviewHorizontalScrollerFieldCursorCodec.TryGetAddress(
			ref platform,
			new MuiListviewHorizontalScrollerFieldCursor
			{
				Record = APTR.FromPointer(0x30FD0),
				Field = MuiListviewHorizontalScrollerField.Magic,
			}, out _, out _));
		Assert.False(MuiListviewHorizontalScrollerMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListviewHorizontalScrollerField.Magic,
			out _, out _));
	}
}
