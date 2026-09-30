using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewScrollerDragStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedVerticalDragRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewScrollerDragState
		{
			Magic = MuiListviewScrollerDragState.Cookie,
			GrabOffset = -8,
			StartFirst = 12,
			LastPointer = 144,
			Flags = MuiListviewScrollerDragState.CapturedFlag,
		};
		Assert.True(MuiListviewScrollerDragStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListviewScrollerDragStateFieldCursor
		{
			Address = address,
			Field = MuiListviewScrollerDragStateField.Flags,
		};
		Assert.True(MuiListviewScrollerDragStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3510u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListviewScrollerDragStateFieldCursorCodec.TryWrite(
			ref platform, address, MuiListviewScrollerDragStateField.StartFirst,
			unchecked((uint)-4)));
		Assert.True(MuiListviewScrollerDragStateFieldCursorCodec.TryRead(
			ref platform, address, MuiListviewScrollerDragStateField.StartFirst,
			out var startFirst));
		Assert.Equal(unchecked((uint)-4), startFirst);
		Assert.True(MuiListviewScrollerDragStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.GrabOffset, decoded.GrabOffset);
		Assert.Equal(-4, decoded.StartFirst);
		Assert.Equal(value.LastPointer, decoded.LastPointer);
		Assert.Equal(value.Flags, decoded.Flags);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewScrollerDragStateFieldCursorCodec.TryGetAddress(
			ref platform,
			new MuiListviewScrollerDragStateFieldCursor
			{
				Address = address,
				Field = (MuiListviewScrollerDragStateField)255,
			}, out _, out _));
		Assert.False(MuiListviewScrollerDragStateFieldCursorCodec.TryGetAddress(
			ref platform,
			new MuiListviewScrollerDragStateFieldCursor
			{
				Address = APTR.FromPointer(0x30FF0),
				Field = MuiListviewScrollerDragStateField.Magic,
			}, out _, out _));
		Assert.False(MuiListviewScrollerDragStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListviewScrollerDragStateField.Magic,
			out _, out _));
	}
}
