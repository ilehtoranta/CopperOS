using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeDisplaySnapshotStructAdapterTests
{
	[Fact]
	public void DisplaySnapshotFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListtreeCore.MuiListtreeDisplaySnapshotState
		{
			Magic = MuiListtreeCore.MuiListtreeDisplaySnapshotState.Cookie,
			Node = APTR.FromPointer(0x36200),
			Columns = 3,
			Values = APTR.FromPointer(0x36400),
			DisplayFlags = MuiListtreeCore.MuiListtreeDisplaySnapshotState.DisplayListNode |
				MuiListtreeCore.MuiListtreeDisplaySnapshotState.DisplayOpen,
			Reserved = 0x55,
		};

		Assert.True(MuiListtreeCore.MuiListtreeDisplaySnapshotStateCodec.WriteRecord(
			ref platform, address, value));
		var fieldCursor = new MuiListtreeCore.MuiListtreeDisplaySnapshotFieldCursor
		{
			Address = address,
			Field = MuiListtreeCore.MuiListtreeDisplaySnapshotField.Values,
		};
		Assert.True(MuiListtreeCore.MuiListtreeDisplaySnapshotFieldCursorCodec
			.TryGetAddress(ref platform, fieldCursor, out var valuesAddress));
		Assert.Equal(0x350Cu, valuesAddress.Raw);
		Assert.True(MuiListtreeCore.MuiListtreeDisplaySnapshotMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeDisplaySnapshotField.Values,
			0x36600));
		Assert.True(MuiListtreeCore.MuiListtreeDisplaySnapshotMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiListtreeCore.MuiListtreeDisplaySnapshotField.DisplayFlags, 0x12));
		Assert.True(MuiListtreeCore.MuiListtreeDisplaySnapshotMemoryCodec.TryReadUInt32(
			ref platform, address,
			MuiListtreeCore.MuiListtreeDisplaySnapshotField.Values, out var values));
		Assert.Equal(0x36600u, values);
		Assert.True(MuiListtreeCore.MuiListtreeDisplaySnapshotStateCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Node.Raw, decoded.Node.Raw);
		Assert.Equal(value.Columns, decoded.Columns);
		Assert.Equal(0x36600u, decoded.Values.Raw);
		Assert.Equal(0x12u, decoded.DisplayFlags);
		Assert.Equal(value.Reserved, decoded.Reserved);
		Assert.True(MuiListtreeCore.MuiListtreeDisplaySnapshotMemoryCodec.TryGetAddress(
			ref platform, address,
			MuiListtreeCore.MuiListtreeDisplaySnapshotField.DisplayFlags,
			out var flagsAddress));
		Assert.Equal(0x3510u, flagsAddress.Raw);
	}

	[Fact]
	public void DisplaySnapshotAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListtreeCore.MuiListtreeDisplaySnapshotMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeDisplaySnapshotField)0xFF,
			out _));
		Assert.False(MuiListtreeCore.MuiListtreeDisplaySnapshotMemoryCodec.TryWriteUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeDisplaySnapshotField)0xFF,
			1));
		Assert.False(MuiListtreeCore.MuiListtreeDisplaySnapshotMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FF0),
			MuiListtreeCore.MuiListtreeDisplaySnapshotField.DisplayFlags, out _));
		Assert.False(MuiListtreeCore.MuiListtreeDisplaySnapshotMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null,
			MuiListtreeCore.MuiListtreeDisplaySnapshotField.Magic, 1));
	}
}
