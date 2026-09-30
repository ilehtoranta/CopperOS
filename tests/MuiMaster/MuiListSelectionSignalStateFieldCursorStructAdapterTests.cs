using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListSelectionSignalStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedSelectionSignalRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListSelectionSignalState
		{
			Magic = MuiListCore.MuiListSelectionSignalState.Cookie,
			Value = 1,
		};
		Assert.True(MuiListCore.MuiListSelectionSignalStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListSelectionSignalStateFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListSelectionSignalStateField.Value,
		};
		Assert.True(MuiListCore.MuiListSelectionSignalStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3504u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListSelectionSignalStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListSelectionSignalStateField.Value, 0));
		Assert.True(MuiListCore.MuiListSelectionSignalStateFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListSelectionSignalStateField.Value, out var signal));
		Assert.Equal(0u, signal);
		Assert.True(MuiListCore.MuiListSelectionSignalStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(0u, decoded.Value);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListSelectionSignalStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListSelectionSignalStateFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListSelectionSignalStateField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListSelectionSignalStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListSelectionSignalStateFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListSelectionSignalStateField.Magic,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListSelectionSignalStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null,
			MuiListCore.MuiListSelectionSignalStateField.Magic, out _, out _));
	}
}
