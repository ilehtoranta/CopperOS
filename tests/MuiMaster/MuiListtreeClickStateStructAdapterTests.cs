using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeClickStateStructAdapterTests
{
	[Fact]
	public void ClickStateFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListtreeCore.MuiListtreeClickState
		{
			Magic = MuiListtreeCore.MuiListtreeClickState.Cookie,
			LastNode = APTR.FromPointer(0x36200),
			LastSeconds = 10,
			LastMicros = 20,
			Clicks = 2,
			TimestampValid = 7,
			DoubleClick = 3,
		};

		Assert.True(MuiListtreeCore.MuiListtreeClickStateCodec.WriteRecord(
			ref platform, address, value));
		var fieldCursor = new MuiListtreeCore.MuiListtreeClickStateFieldCursor
		{
			Address = address,
			Field = MuiListtreeCore.MuiListtreeClickStateField.LastNode,
		};
		Assert.True(MuiListtreeCore.MuiListtreeClickStateFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var lastNodeAddress));
		Assert.Equal(0x3504u, lastNodeAddress.Raw);
		Assert.True(MuiListtreeCore.MuiListtreeClickStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeClickStateField.LastMicros,
			30));
		Assert.True(MuiListtreeCore.MuiListtreeClickStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeClickStateField.LastNode,
			0x36400));
		Assert.True(MuiListtreeCore.MuiListtreeClickStateMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeClickStateField.LastNode,
			out var lastNode));
		Assert.Equal(0x36400u, lastNode);
		Assert.True(MuiListtreeCore.MuiListtreeClickStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(0x36400u, decoded.LastNode.Raw);
		Assert.Equal(value.LastSeconds, decoded.LastSeconds);
		Assert.Equal(30u, decoded.LastMicros);
		Assert.Equal(value.Clicks, decoded.Clicks);
		Assert.Equal(value.TimestampValid, decoded.TimestampValid);
		Assert.Equal(value.DoubleClick, decoded.DoubleClick);
		Assert.True(MuiListtreeCore.MuiListtreeClickStateMemoryCodec.TryGetAddress(
			ref platform, address, MuiListtreeCore.MuiListtreeClickStateField.DoubleClick,
			out var doubleClickAddress));
		Assert.Equal(0x3518u, doubleClickAddress.Raw);
	}

	[Fact]
	public void ClickStateAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListtreeCore.MuiListtreeClickStateMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeClickStateField)0xFF,
			out _));
		Assert.False(MuiListtreeCore.MuiListtreeClickStateMemoryCodec.TryWriteUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeClickStateField)0xFF,
			1));
		Assert.False(MuiListtreeCore.MuiListtreeClickStateMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FF0),
			MuiListtreeCore.MuiListtreeClickStateField.Clicks, out _));
		Assert.False(MuiListtreeCore.MuiListtreeClickStateMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null, MuiListtreeCore.MuiListtreeClickStateField.Magic,
			1));
	}
}
