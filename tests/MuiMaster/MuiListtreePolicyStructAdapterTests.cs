using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreePolicyStructAdapterTests
{
	[Fact]
	public void PolicyFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListtreeCore.MuiListtreePolicyStateRecord
		{
			Magic = MuiListtreeCore.MuiListtreePolicyStateRecord.Cookie,
			Active = APTR.FromPointer(0x36200),
			DuplicateNodeName = 1,
			Quiet = 0,
			DragDropSort = 2,
			DoubleClick = 1,
			CloseHook = APTR.FromPointer(0x36400),
			ConstructHook = APTR.FromPointer(0x36440),
			DestructHook = APTR.FromPointer(0x36480),
			DisplayHook = APTR.FromPointer(0x364C0),
			OpenHook = APTR.FromPointer(0x36500),
			SortHook = APTR.FromPointer(0x36540),
		};

		Assert.True(MuiListtreeCore.MuiListtreePolicyStateRecordCodec.WriteRecord(
			ref platform, address, value));
		var fieldCursor = new MuiListtreeCore.MuiListtreePolicyFieldCursor
		{
			Address = address,
			Field = MuiListtreeCore.MuiListtreePolicyField.DisplayHook,
		};
		Assert.True(MuiListtreeCore.MuiListtreePolicyFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var displayHookAddress));
		Assert.Equal(0x3524u, displayHookAddress.Raw);
		Assert.True(MuiListtreeCore.MuiListtreePolicyMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreePolicyField.Quiet, 1));
		Assert.True(MuiListtreeCore.MuiListtreePolicyMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreePolicyField.DisplayHook,
			0x36600));
		Assert.True(MuiListtreeCore.MuiListtreePolicyMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreePolicyField.DisplayHook,
			out var displayHook));
		Assert.Equal(0x36600u, displayHook);
		Assert.True(MuiListtreeCore.MuiListtreePolicyStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Active.Raw, decoded.Active.Raw);
		Assert.Equal(value.DuplicateNodeName, decoded.DuplicateNodeName);
		Assert.Equal(1u, decoded.Quiet);
		Assert.Equal(value.DragDropSort, decoded.DragDropSort);
		Assert.Equal(value.DoubleClick, decoded.DoubleClick);
		Assert.Equal(value.CloseHook.Raw, decoded.CloseHook.Raw);
		Assert.Equal(value.ConstructHook.Raw, decoded.ConstructHook.Raw);
		Assert.Equal(value.DestructHook.Raw, decoded.DestructHook.Raw);
		Assert.Equal(0x36600u, decoded.DisplayHook.Raw);
		Assert.Equal(value.OpenHook.Raw, decoded.OpenHook.Raw);
		Assert.Equal(value.SortHook.Raw, decoded.SortHook.Raw);
		Assert.True(MuiListtreeCore.MuiListtreePolicyMemoryCodec.TryGetAddress(
			ref platform, address, MuiListtreeCore.MuiListtreePolicyField.SortHook,
			out var sortHookAddress));
		Assert.Equal(0x352Cu, sortHookAddress.Raw);
	}

	[Fact]
	public void PolicyAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListtreeCore.MuiListtreePolicyMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreePolicyField)0xFF,
			out _));
		Assert.False(MuiListtreeCore.MuiListtreePolicyMemoryCodec.TryWriteUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreePolicyField)0xFF, 1));
		Assert.False(MuiListtreeCore.MuiListtreePolicyMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FF0),
			MuiListtreeCore.MuiListtreePolicyField.Quiet, out _));
		Assert.False(MuiListtreeCore.MuiListtreePolicyMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null, MuiListtreeCore.MuiListtreePolicyField.Magic, 1));
	}
}
