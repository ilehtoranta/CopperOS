using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewScrollerDragStateStructAdapterTests
{
	[Fact]
	public void VerticalDragFieldsRoundTripThroughNamedRecord()
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

		Assert.True(MuiListviewScrollerDragStateCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiListviewScrollerDragStateMemoryCodec.TryWrite(
			ref platform, address,
			MuiListviewScrollerDragStateField.GrabOffset, unchecked((uint)-16)));
		Assert.True(MuiListviewScrollerDragStateMemoryCodec.TryWrite(
			ref platform, address,
			MuiListviewScrollerDragStateField.StartFirst, unchecked((uint)-3)));
		Assert.True(MuiListviewScrollerDragStateMemoryCodec.TryRead(ref platform,
			address, MuiListviewScrollerDragStateField.StartFirst,
			out var startFirst));
		Assert.Equal(unchecked((uint)-3), startFirst);

		Assert.True(MuiListviewScrollerDragStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(-16, decoded.GrabOffset);
		Assert.Equal(-3, decoded.StartFirst);
		Assert.Equal(value.LastPointer, decoded.LastPointer);
		Assert.Equal(value.Flags, decoded.Flags);

		Assert.True(MuiListviewScrollerDragStateMemoryCodec.TryGetAddress(
			ref platform, address, MuiListviewScrollerDragStateField.Flags,
			out var flagsAddress));
		Assert.Equal(0x3510u, flagsAddress.Raw);
	}

	[Fact]
	public void VerticalDragAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewScrollerDragStateMemoryCodec.TryRead(ref platform,
			address, (MuiListviewScrollerDragStateField)0xFF, out _));
		Assert.False(MuiListviewScrollerDragStateMemoryCodec.TryWrite(ref platform,
			address, (MuiListviewScrollerDragStateField)0xFF, 1));
		Assert.False(MuiListviewScrollerDragStateMemoryCodec.TryRead(ref platform,
			APTR.FromPointer(0x30FF0), MuiListviewScrollerDragStateField.Flags,
			out _));
		Assert.False(MuiListviewScrollerDragStateMemoryCodec.TryWrite(ref platform,
			APTR.Null, MuiListviewScrollerDragStateField.Magic, 1));
	}
}
