using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewHorizontalScrollerStructAdapterTests
{
	[Fact]
	public void HorizontalScrollerFieldsRoundTripThroughNamedRecord()
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
		Assert.True(MuiListviewHorizontalScrollerMemoryCodec.TryWriteInt32(
			ref platform, address, MuiListviewHorizontalScrollerField.TrackLeft, -20));
		Assert.True(MuiListviewHorizontalScrollerMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListviewHorizontalScrollerField.ScrollX, 9));
		Assert.True(MuiListviewHorizontalScrollerMemoryCodec.TryReadInt32(
			ref platform, address, MuiListviewHorizontalScrollerField.TrackLeft,
			out var trackLeft));
		Assert.Equal(-20, trackLeft);

		Assert.True(MuiListviewHorizontalScrollerStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(-20, decoded.TrackLeft);
		Assert.Equal(value.TrackTop, decoded.TrackTop);
		Assert.Equal(value.TrackRight, decoded.TrackRight);
		Assert.Equal(value.TrackBottom, decoded.TrackBottom);
		Assert.Equal(value.ThumbLeft, decoded.ThumbLeft);
		Assert.Equal(value.ThumbTop, decoded.ThumbTop);
		Assert.Equal(value.ThumbRight, decoded.ThumbRight);
		Assert.Equal(value.ThumbBottom, decoded.ThumbBottom);
		Assert.Equal(value.ContentWidth, decoded.ContentWidth);
		Assert.Equal(value.ViewWidth, decoded.ViewWidth);
		Assert.Equal(9u, decoded.ScrollX);
		Assert.Equal(value.MaxScrollX, decoded.MaxScrollX);

		Assert.True(MuiListviewHorizontalScrollerMemoryCodec.TryGetAddress(
			ref platform, address, MuiListviewHorizontalScrollerField.ScrollX,
			out var scrollAddress));
		Assert.Equal(0x352Cu, scrollAddress.Raw);
	}

	[Fact]
	public void HorizontalScrollerAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewHorizontalScrollerMemoryCodec.TryReadUInt32(ref platform,
			address, (MuiListviewHorizontalScrollerField)0xFF, out _));
		Assert.False(MuiListviewHorizontalScrollerMemoryCodec.TryWriteUInt32(ref platform,
			address, (MuiListviewHorizontalScrollerField)0xFF, 1));
		Assert.False(MuiListviewHorizontalScrollerMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x30FF0),
			MuiListviewHorizontalScrollerField.MaxScrollX, out _));
		Assert.False(MuiListviewHorizontalScrollerMemoryCodec.TryWriteUInt32(ref platform,
			APTR.Null, MuiListviewHorizontalScrollerField.Magic, 1));
	}
}
