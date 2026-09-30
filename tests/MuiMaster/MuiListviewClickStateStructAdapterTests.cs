using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewClickStateStructAdapterTests
{
	[Fact]
	public void ClickFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewCore.MuiListviewClickState
		{
			Magic = MuiListviewCore.MuiListviewClickState.Cookie,
			ClickColumn = 2,
			DoubleClick = 0xA5A5A5A5u,
			AgainClick = 0x5A5A5A5Au,
			Clicks = 1,
			DefClickColumn = 4,
		};

		Assert.True(MuiListviewCore.MuiListviewClickStateCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiListviewCore.MuiListviewClickStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListviewCore.MuiListviewClickStateField.Clicks,
			3));
		Assert.True(MuiListviewCore.MuiListviewClickStateMemoryCodec.TryReadUInt32(
			ref platform, address,
			MuiListviewCore.MuiListviewClickStateField.Clicks, out var clicks));
		Assert.Equal(3u, clicks);

		Assert.True(MuiListviewCore.MuiListviewClickStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.ClickColumn, decoded.ClickColumn);
		Assert.Equal(value.DoubleClick, decoded.DoubleClick);
		Assert.Equal(value.AgainClick, decoded.AgainClick);
		Assert.Equal(3u, decoded.Clicks);
		Assert.Equal(value.DefClickColumn, decoded.DefClickColumn);

		Assert.True(MuiListviewCore.MuiListviewClickStateMemoryCodec.TryGetAddress(
			ref platform, address,
			MuiListviewCore.MuiListviewClickStateField.DefClickColumn,
			out var defColumnAddress));
		Assert.Equal(0x3514u, defColumnAddress.Raw);
	}

	[Fact]
	public void ClickAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewCore.MuiListviewClickStateMemoryCodec.TryReadUInt32(
			ref platform, address,
			(MuiListviewCore.MuiListviewClickStateField)0xFF, out _));
		Assert.False(MuiListviewCore.MuiListviewClickStateMemoryCodec.TryWriteUInt32(
			ref platform, address,
			(MuiListviewCore.MuiListviewClickStateField)0xFF, 1));
		Assert.False(MuiListviewCore.MuiListviewClickStateMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FF0),
			MuiListviewCore.MuiListviewClickStateField.Clicks, out _));
		Assert.False(MuiListviewCore.MuiListviewClickStateMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null,
			MuiListviewCore.MuiListviewClickStateField.Magic, 1));
	}
}
