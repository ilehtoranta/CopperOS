using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewSelectionSignalFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedSelectionSignalRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListviewCore.MuiListviewSelectionSignalState
		{
			Magic = MuiListviewCore.MuiListviewSelectionSignalState.Cookie,
			Value = 7,
		};
		Assert.True(MuiListviewCore.MuiListviewSelectionSignalStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListviewCore.MuiListviewSelectionSignalFieldCursor
		{
			Record = address,
			Field = MuiListviewCore.MuiListviewSelectionSignalField.Value,
		};
		Assert.True(MuiListviewCore.MuiListviewSelectionSignalFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress,
				out var fieldSize));
		Assert.Equal(0x3504u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListviewCore.MuiListviewSelectionSignalFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListviewCore.MuiListviewSelectionSignalField.Value, 9));
		Assert.True(MuiListviewCore.MuiListviewSelectionSignalFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListviewCore.MuiListviewSelectionSignalField.Value, out var signal));
		Assert.Equal(9u, signal);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListviewCore.MuiListviewSelectionSignalFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListviewCore.MuiListviewSelectionSignalFieldCursor
				{
					Record = address,
					Field = (MuiListviewCore.MuiListviewSelectionSignalField)255,
				}, out _, out _));
		Assert.False(MuiListviewCore.MuiListviewSelectionSignalFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListviewCore.MuiListviewSelectionSignalFieldCursor
				{
					Record = APTR.FromPointer(0x30FFC),
					Field = MuiListviewCore.MuiListviewSelectionSignalField.Magic,
				}, out _, out _));
		Assert.False(MuiListviewCore.MuiListviewSelectionSignalMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
			MuiListviewCore.MuiListviewSelectionSignalField.Magic, out _, out _));
	}
}
