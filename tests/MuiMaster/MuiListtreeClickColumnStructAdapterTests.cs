using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeClickColumnStructAdapterTests
{
	[Fact]
	public void ClickColumnFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListtreeCore.MuiListtreeClickColumnState
		{
			Magic = MuiListtreeCore.MuiListtreeClickColumnState.Cookie,
			LastColumn = 4,
			Valid = 3,
		};

		Assert.True(MuiListtreeCore.MuiListtreeClickColumnStateCodec.WriteRecord(
			ref platform, address, value));
		var fieldCursor = new MuiListtreeCore.MuiListtreeClickColumnFieldCursor
		{
			Address = address,
			Field = MuiListtreeCore.MuiListtreeClickColumnField.LastColumn,
		};
		Assert.True(MuiListtreeCore.MuiListtreeClickColumnFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var lastColumnAddress));
		Assert.Equal(0x3504u, lastColumnAddress.Raw);
		Assert.True(MuiListtreeCore.MuiListtreeClickColumnMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeClickColumnField.LastColumn,
			7));
		Assert.True(MuiListtreeCore.MuiListtreeClickColumnMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeClickColumnField.Valid,
			5));
		Assert.True(MuiListtreeCore.MuiListtreeClickColumnMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeClickColumnField.LastColumn,
			out var lastColumn));
		Assert.Equal(7u, lastColumn);
		Assert.True(MuiListtreeCore.MuiListtreeClickColumnStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(7u, decoded.LastColumn);
		Assert.Equal(5u, decoded.Valid);
		Assert.True(MuiListtreeCore.MuiListtreeClickColumnMemoryCodec.TryGetAddress(
			ref platform, address, MuiListtreeCore.MuiListtreeClickColumnField.Valid,
			out var validAddress));
		Assert.Equal(0x3508u, validAddress.Raw);
	}

	[Fact]
	public void ClickColumnAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListtreeCore.MuiListtreeClickColumnMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeClickColumnField)0xFF,
			out _));
		Assert.False(MuiListtreeCore.MuiListtreeClickColumnMemoryCodec.TryWriteUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeClickColumnField)0xFF,
			1));
		Assert.False(MuiListtreeCore.MuiListtreeClickColumnMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FFC),
			MuiListtreeCore.MuiListtreeClickColumnField.LastColumn, out _));
		Assert.False(MuiListtreeCore.MuiListtreeClickColumnMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null, MuiListtreeCore.MuiListtreeClickColumnField.Magic,
			1));
	}
}
