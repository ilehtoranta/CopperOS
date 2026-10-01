using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeHookPoolStructAdapterTests
{
	[Fact]
	public void HookPoolFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListtreeCore.MuiListtreeHookPoolStateRecord
		{
			Magic = MuiListtreeCore.MuiListtreeHookPoolStateRecord.Cookie,
			Pool = APTR.FromPointer(0x36200),
			Requirements = 1,
			PuddleSize = 256,
			ThresholdSize = 128,
			Owned = 1,
		};

		Assert.True(MuiListtreeCore.MuiListtreeHookPoolStateRecordCodec.WriteRecord(
			ref platform, address, value));
		var fieldCursor = new MuiListtreeCore.MuiListtreeHookPoolFieldCursor
		{
			Address = address,
			Field = MuiListtreeCore.MuiListtreeHookPoolField.Pool,
		};
		Assert.True(MuiListtreeCore.MuiListtreeHookPoolFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var poolAddress));
		Assert.Equal(0x3504u, poolAddress.Raw);
		Assert.True(MuiListtreeCore.MuiListtreeHookPoolMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeHookPoolField.ThresholdSize,
			192));
		Assert.True(MuiListtreeCore.MuiListtreeHookPoolMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeHookPoolField.Pool,
			0x36400));
		Assert.True(MuiListtreeCore.MuiListtreeHookPoolMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeHookPoolField.Pool,
			out var pool));
		Assert.Equal(0x36400u, pool);
		Assert.True(MuiListtreeCore.MuiListtreeHookPoolStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(0x36400u, decoded.Pool.Raw);
		Assert.Equal(value.Requirements, decoded.Requirements);
		Assert.Equal(value.PuddleSize, decoded.PuddleSize);
		Assert.Equal(192u, decoded.ThresholdSize);
		Assert.Equal(value.Owned, decoded.Owned);
		Assert.True(MuiListtreeCore.MuiListtreeHookPoolMemoryCodec.TryGetAddress(
			ref platform, address, MuiListtreeCore.MuiListtreeHookPoolField.Owned,
			out var ownedAddress));
		Assert.Equal(0x3514u, ownedAddress.Raw);
	}

	[Fact]
	public void HookPoolAdapterRejectsInvalidOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListtreeCore.MuiListtreeHookPoolMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeHookPoolField)0xFF,
			out _));
		Assert.False(MuiListtreeCore.MuiListtreeHookPoolMemoryCodec.TryWriteUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeHookPoolField)0xFF,
			1));
		Assert.False(MuiListtreeCore.MuiListtreeHookPoolMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FF0),
			MuiListtreeCore.MuiListtreeHookPoolField.ThresholdSize, out _));
		Assert.False(MuiListtreeCore.MuiListtreeHookPoolMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null, MuiListtreeCore.MuiListtreeHookPoolField.Magic,
			1));
	}
}
