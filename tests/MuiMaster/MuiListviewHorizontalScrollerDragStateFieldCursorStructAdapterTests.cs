using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewHorizontalScrollerDragStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedHorizontalDragRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewHorizontalScrollerDragState
		{
			Magic = MuiListviewHorizontalScrollerDragState.Cookie,
			GrabOffset = -12,
			StartScroll = 80,
			LastPointer = 144,
			Flags = MuiListviewHorizontalScrollerDragState.ActiveFlag,
		};
		Assert.True(MuiListviewHorizontalScrollerDragStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListviewHorizontalScrollerDragStateFieldCursor
		{
			Address = address,
			Field = MuiListviewHorizontalScrollerDragStateField.Flags,
		};
		Assert.True(MuiListviewHorizontalScrollerDragStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress,
				out var fieldSize));
		Assert.Equal(0x3510u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListviewHorizontalScrollerDragStateFieldCursorCodec.TryWrite(
			ref platform, address,
			MuiListviewHorizontalScrollerDragStateField.GrabOffset,
			unchecked((uint)-20)));
		Assert.True(MuiListviewHorizontalScrollerDragStateFieldCursorCodec.TryRead(
			ref platform, address,
			MuiListviewHorizontalScrollerDragStateField.GrabOffset,
			out var grabOffset));
		Assert.Equal(unchecked((uint)-20), grabOffset);
		Assert.True(MuiListviewHorizontalScrollerDragStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(-20, decoded.GrabOffset);
		Assert.Equal(value.StartScroll, decoded.StartScroll);
		Assert.Equal(value.Flags, decoded.Flags);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewHorizontalScrollerDragStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListviewHorizontalScrollerDragStateFieldCursor
				{
					Address = address,
					Field = (MuiListviewHorizontalScrollerDragStateField)255,
				}, out _, out _));
		Assert.False(MuiListviewHorizontalScrollerDragStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListviewHorizontalScrollerDragStateFieldCursor
				{
					Address = APTR.FromPointer(0x30FF0),
					Field = MuiListviewHorizontalScrollerDragStateField.Magic,
				}, out _, out _));
		Assert.False(MuiListviewHorizontalScrollerDragStateMemoryCodec
			.TryGetAddress(ref platform, APTR.Null,
				MuiListviewHorizontalScrollerDragStateField.Magic, out _, out _));
	}
}
