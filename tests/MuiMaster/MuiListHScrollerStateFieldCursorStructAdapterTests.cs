using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListHScrollerStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedHorizontalScrollerRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListHScrollerState
		{
			Magic = MuiListHScrollerState.Cookie,
			Policy = 2,
			ContentWidth = 1200,
			ViewWidth = 320,
			Visible = 1,
			ScrollX = 40,
			MaxScrollX = 880,
		};
		Assert.True(MuiListHScrollerStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListHScrollerStateFieldCursor
		{
			Address = address,
			Field = MuiListHScrollerStateField.MaxScrollX,
		};
		Assert.True(MuiListHScrollerStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3518u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListHScrollerStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiListHScrollerStateField.ScrollX, 160));
		Assert.True(MuiListHScrollerStateFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiListHScrollerStateField.ScrollX,
			out var scrollX));
		Assert.Equal(160u, scrollX);
		Assert.True(MuiListHScrollerStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Policy, decoded.Policy);
		Assert.Equal(value.ContentWidth, decoded.ContentWidth);
		Assert.Equal(value.ViewWidth, decoded.ViewWidth);
		Assert.Equal(value.Visible, decoded.Visible);
		Assert.Equal(160u, decoded.ScrollX);
		Assert.Equal(value.MaxScrollX, decoded.MaxScrollX);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListHScrollerStateFieldCursorCodec.TryGetAddress(
			ref platform,
			new MuiListHScrollerStateFieldCursor
			{
				Address = address,
				Field = (MuiListHScrollerStateField)255,
			}, out _, out _));
		Assert.False(MuiListHScrollerStateFieldCursorCodec.TryGetAddress(
			ref platform,
			new MuiListHScrollerStateFieldCursor
			{
				Address = APTR.FromPointer(0x30FF0),
				Field = MuiListHScrollerStateField.Magic,
			}, out _, out _));
		Assert.False(MuiListHScrollerStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListHScrollerStateField.Magic,
			out _, out _));
	}
}
