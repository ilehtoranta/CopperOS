using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewHorizontalScrollerDragStateStructAdapterTests
{
	[Fact]
	public void HorizontalDragFieldsRoundTripThroughNamedRecord()
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
		Assert.True(MuiListviewHorizontalScrollerDragStateMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiListviewHorizontalScrollerDragStateField.GrabOffset,
			unchecked((uint)-20)));
		Assert.True(MuiListviewHorizontalScrollerDragStateMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiListviewHorizontalScrollerDragStateField.StartScroll, 96));
		Assert.True(MuiListviewHorizontalScrollerDragStateMemoryCodec.TryReadUInt32(
			ref platform, address,
			MuiListviewHorizontalScrollerDragStateField.GrabOffset,
			out var grabOffset));
		Assert.Equal(unchecked((uint)-20), grabOffset);

		Assert.True(MuiListviewHorizontalScrollerDragStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(-20, decoded.GrabOffset);
		Assert.Equal(96u, decoded.StartScroll);
		Assert.Equal(value.LastPointer, decoded.LastPointer);
		Assert.Equal(value.Flags, decoded.Flags);

		Assert.True(MuiListviewHorizontalScrollerDragStateMemoryCodec.TryGetAddress(
			ref platform, address,
			MuiListviewHorizontalScrollerDragStateField.Flags,
			out var flagsAddress));
		Assert.Equal(0x3510u, flagsAddress.Raw);
	}

	[Fact]
	public void HorizontalDragAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewHorizontalScrollerDragStateMemoryCodec.TryReadUInt32(
			ref platform, address,
			(MuiListviewHorizontalScrollerDragStateField)0xFF, out _));
		Assert.False(MuiListviewHorizontalScrollerDragStateMemoryCodec.TryWriteUInt32(
			ref platform, address,
			(MuiListviewHorizontalScrollerDragStateField)0xFF, 1));
		Assert.False(MuiListviewHorizontalScrollerDragStateMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FF0),
			MuiListviewHorizontalScrollerDragStateField.Flags, out _));
		Assert.False(MuiListviewHorizontalScrollerDragStateMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null,
			MuiListviewHorizontalScrollerDragStateField.Magic, 1));
	}
}
