using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewSelectionSignalStructAdapterTests
{
	[Fact]
	public void SelectionSignalFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewCore.MuiListviewSelectionSignalState
		{
			Magic = MuiListviewCore.MuiListviewSelectionSignalState.Cookie,
			Value = 0xA5A5A5A5u,
		};

		Assert.True(MuiListviewCore.MuiListviewSelectionSignalStateCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiListviewCore.MuiListviewSelectionSignalMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiListviewCore.MuiListviewSelectionSignalField.Value, 0x5A5A5A5Au));
		Assert.True(MuiListviewCore.MuiListviewSelectionSignalMemoryCodec.TryReadUInt32(
			ref platform, address,
			MuiListviewCore.MuiListviewSelectionSignalField.Value,
			out var signal));
		Assert.Equal(0x5A5A5A5Au, signal);

		Assert.True(MuiListviewCore.MuiListviewSelectionSignalStateCodec
			.TryReadStructural(ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(0x5A5A5A5Au, decoded.Value);

		Assert.True(MuiListviewCore.MuiListviewSelectionSignalMemoryCodec.TryGetAddress(
			ref platform, address, MuiListviewCore.MuiListviewSelectionSignalField.Value,
			out var signalAddress));
		Assert.Equal(0x3504u, signalAddress.Raw);
	}

	[Fact]
	public void SelectionSignalAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewCore.MuiListviewSelectionSignalMemoryCodec.TryReadUInt32(
			ref platform, address,
			(MuiListviewCore.MuiListviewSelectionSignalField)0xFF, out _));
		Assert.False(MuiListviewCore.MuiListviewSelectionSignalMemoryCodec.TryWriteUInt32(
			ref platform, address,
			(MuiListviewCore.MuiListviewSelectionSignalField)0xFF, 1));
		Assert.False(MuiListviewCore.MuiListviewSelectionSignalMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FFC),
			MuiListviewCore.MuiListviewSelectionSignalField.Value, out _));
		Assert.False(MuiListviewCore.MuiListviewSelectionSignalMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null,
			MuiListviewCore.MuiListviewSelectionSignalField.Magic, 1));
	}
}
