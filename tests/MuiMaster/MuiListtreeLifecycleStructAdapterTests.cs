using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeLifecycleStructAdapterTests
{
	[Fact]
	public void LifecycleFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListtreeCore.MuiListtreeLifecycleStateRecord
		{
			Magic = MuiListtreeCore.MuiListtreeLifecycleStateRecord.Cookie,
			RenderInfo = APTR.FromPointer(0x36200),
			Setup = 1,
			Shown = 0,
		};

		Assert.True(MuiListtreeCore.MuiListtreeLifecycleStateRecordCodec.WriteRecord(
			ref platform, address, value));
		var fieldCursor = new MuiListtreeCore.MuiListtreeLifecycleFieldCursor
		{
			Address = address,
			Field = MuiListtreeCore.MuiListtreeLifecycleField.Setup,
		};
		Assert.True(MuiListtreeCore.MuiListtreeLifecycleFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var setupAddress));
		Assert.Equal(0x3508u, setupAddress.Raw);
		Assert.True(MuiListtreeCore.MuiListtreeLifecycleMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeLifecycleField.RenderInfo,
			0x36400));
		Assert.True(MuiListtreeCore.MuiListtreeLifecycleMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeLifecycleField.Shown,
			1));
		Assert.True(MuiListtreeCore.MuiListtreeLifecycleMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeLifecycleField.RenderInfo,
			out var renderInfo));
		Assert.Equal(0x36400u, renderInfo);
		Assert.True(MuiListtreeCore.MuiListtreeLifecycleStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(0x36400u, decoded.RenderInfo.Raw);
		Assert.Equal(value.Setup, decoded.Setup);
		Assert.Equal(1u, decoded.Shown);
		Assert.True(MuiListtreeCore.MuiListtreeLifecycleMemoryCodec.TryGetAddress(
			ref platform, address, MuiListtreeCore.MuiListtreeLifecycleField.Shown,
			out var shownAddress));
		Assert.Equal(0x350Cu, shownAddress.Raw);
	}

	[Fact]
	public void LifecycleAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListtreeCore.MuiListtreeLifecycleMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeLifecycleField)0xFF,
			out _));
		Assert.False(MuiListtreeCore.MuiListtreeLifecycleMemoryCodec.TryWriteUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeLifecycleField)0xFF,
			1));
		Assert.False(MuiListtreeCore.MuiListtreeLifecycleMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FF8),
			MuiListtreeCore.MuiListtreeLifecycleField.Shown, out _));
		Assert.False(MuiListtreeCore.MuiListtreeLifecycleMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null, MuiListtreeCore.MuiListtreeLifecycleField.Magic,
			1));
	}
}
