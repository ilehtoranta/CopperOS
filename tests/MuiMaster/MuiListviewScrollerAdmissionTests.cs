using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewScrollerAdmissionTests
{
	[Fact]
	public void ListviewHorizontalScrollerAndDragRecordsRoundTripThroughStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var horizontalAddress = APTR.FromPointer(0x2400);
		var horizontalDragAddress = APTR.FromPointer(0x2440);
		var verticalDragAddress = APTR.FromPointer(0x2480);
		Assert.True(MuiListviewHorizontalScrollerStateCodec.Write(ref platform,
			horizontalAddress, new MuiListviewHorizontalScrollerState
			{
				Magic = MuiListviewHorizontalScrollerState.Cookie,
				TrackLeft = 0,
				TrackTop = 1,
				TrackRight = 200,
				TrackBottom = 16,
				ThumbLeft = 10,
				ThumbTop = 1,
				ThumbRight = 50,
				ThumbBottom = 16,
				ContentWidth = 1000,
				ViewWidth = 200,
				ScrollX = 40,
				MaxScrollX = 800,
			}));
		Assert.True(MuiListviewHorizontalScrollerDragStateCodec.Write(ref platform,
			horizontalDragAddress, new MuiListviewHorizontalScrollerDragState
			{
				Magic = MuiListviewHorizontalScrollerDragState.Cookie,
				GrabOffset = -3,
				StartScroll = 40,
				LastPointer = 12,
				Flags = MuiListviewHorizontalScrollerDragState.ActiveFlag,
			}));
		Assert.True(MuiListviewScrollerDragStateCodec.Write(ref platform,
			verticalDragAddress, new MuiListviewScrollerDragState
			{
				Magic = MuiListviewScrollerDragState.Cookie,
				GrabOffset = 2,
				StartFirst = 4,
				LastPointer = 9,
				Flags = MuiListviewScrollerDragState.CapturedFlag,
			}));
		Assert.True(MuiListviewHorizontalScrollerStateCodec.TryRead(ref platform,
			horizontalAddress, out var horizontal));
		Assert.Equal(1000u, horizontal.ContentWidth);
		Assert.Equal(800u, horizontal.MaxScrollX);
		Assert.True(MuiListviewHorizontalScrollerDragStateCodec.TryRead(ref platform,
			horizontalDragAddress, out var horizontalDrag));
		Assert.Equal(-3, horizontalDrag.GrabOffset);
		Assert.Equal(1u, horizontalDrag.Flags);
		Assert.True(MuiListviewScrollerDragStateCodec.TryRead(ref platform,
			verticalDragAddress, out var verticalDrag));
		Assert.Equal(4, verticalDrag.StartFirst);
		Assert.Equal(2u, verticalDrag.Flags);
	}

	[Fact]
	public void MalformedListviewScrollerCookiesRemainStructuralButFailClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var horizontalAddress = APTR.FromPointer(0x2500);
		var horizontalDragAddress = APTR.FromPointer(0x2540);
		var verticalDragAddress = APTR.FromPointer(0x2580);
		Assert.True(MuiListviewHorizontalScrollerStateCodec.Write(ref platform,
			horizontalAddress, new MuiListviewHorizontalScrollerState
			{
				Magic = MuiListviewHorizontalScrollerState.Cookie,
			}));
		Assert.True(MuiListviewHorizontalScrollerDragStateCodec.Write(ref platform,
			horizontalDragAddress, new MuiListviewHorizontalScrollerDragState
			{
				Magic = MuiListviewHorizontalScrollerDragState.Cookie,
			}));
		Assert.True(MuiListviewScrollerDragStateCodec.Write(ref platform,
			verticalDragAddress, new MuiListviewScrollerDragState
			{
				Magic = MuiListviewScrollerDragState.Cookie,
			}));
		Assert.True(MuiListviewHorizontalScrollerFieldCursorCodec.TryWriteUInt32(
			ref platform, horizontalAddress,
			MuiListviewHorizontalScrollerField.Magic, 0));
		Assert.True(MuiListviewHorizontalScrollerDragStateCodec.TryWrite(ref platform,
			horizontalDragAddress, MuiListviewHorizontalScrollerDragStateField.Magic, 0));
		Assert.True(MuiListviewScrollerDragStateFieldCursorCodec.TryWrite(ref platform,
			verticalDragAddress, MuiListviewScrollerDragStateField.Magic, 0));
		Assert.True(MuiListviewHorizontalScrollerStateCodec.TryReadStructural(
			ref platform, horizontalAddress, out var horizontal));
		Assert.Equal(0u, horizontal.Magic);
		Assert.False(MuiListviewHorizontalScrollerStateCodec.TryRead(ref platform,
			horizontalAddress, out _));
		Assert.True(MuiListviewHorizontalScrollerDragStateCodec.TryReadStructural(
			ref platform, horizontalDragAddress, out var horizontalDrag));
		Assert.Equal(0u, horizontalDrag.Magic);
		Assert.False(MuiListviewHorizontalScrollerDragStateCodec.TryRead(ref platform,
			horizontalDragAddress, out _));
		Assert.True(MuiListviewScrollerDragStateCodec.TryReadStructural(ref platform,
			verticalDragAddress, out var verticalDrag));
		Assert.Equal(0u, verticalDrag.Magic);
		Assert.False(MuiListviewScrollerDragStateCodec.TryRead(ref platform,
			verticalDragAddress, out _));
	}
}
