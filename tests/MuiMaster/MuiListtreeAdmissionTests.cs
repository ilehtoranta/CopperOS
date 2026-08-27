using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeAdmissionTests
{
	[Fact]
	public void ListtreePolicyHookPoolAndClickRecordsRoundTripThroughStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var policyAddress = APTR.FromPointer(0x1500);
		var poolAddress = APTR.FromPointer(0x1540);
		var clickAddress = APTR.FromPointer(0x1580);
		var policy = new MuiListtreeCore.MuiListtreePolicyStateRecord
		{
			Magic = MuiListtreeCore.MuiListtreePolicyStateRecord.Cookie,
			Active = APTR.FromPointer(0x1800),
			DuplicateNodeName = 1,
			Quiet = 0,
			DragDropSort = 1,
			DoubleClick = 0xFFFFFFFF,
			CloseHook = APTR.FromPointer(0x1840),
			ConstructHook = APTR.FromPointer(0x1880),
			DestructHook = APTR.FromPointer(0x18C0),
			DisplayHook = APTR.FromPointer(0x1900),
			OpenHook = APTR.FromPointer(0x1940),
			SortHook = APTR.FromPointer(0x1980),
		};
		var pool = new MuiListtreeCore.MuiListtreeHookPoolStateRecord
		{
			Magic = MuiListtreeCore.MuiListtreeHookPoolStateRecord.Cookie,
			Pool = APTR.FromPointer(0x1A00),
			Requirements = 8,
			PuddleSize = 1024,
			ThresholdSize = 4096,
			Owned = 1,
		};
		var click = new MuiListtreeCore.MuiListtreeClickState
		{
			Magic = MuiListtreeCore.MuiListtreeClickState.Cookie,
			LastNode = APTR.FromPointer(0x1B00),
			LastSeconds = 10,
			LastMicros = 20,
			Clicks = 2,
			TimestampValid = 2,
			DoubleClick = 7,
		};

		Assert.True(MuiListtreeCore.MuiListtreePolicyStateRecordCodec.Write(ref platform,
			policyAddress, policy));
		Assert.True(MuiListtreeCore.MuiListtreeHookPoolStateRecordCodec.Write(ref platform,
			poolAddress, pool));
		Assert.True(MuiListtreeCore.MuiListtreeClickStateCodec.Write(ref platform, clickAddress,
			click));
		Assert.True(MuiListtreeCore.MuiListtreePolicyStateRecordCodec.TryRead(ref platform,
			policyAddress, out var readPolicy));
		Assert.Equal(policy.Active, readPolicy.Active);
		Assert.Equal(policy.SortHook, readPolicy.SortHook);
		Assert.True(MuiListtreeCore.MuiListtreeHookPoolStateRecordCodec.TryRead(ref platform,
			poolAddress, out var readPool));
		Assert.Equal(pool.Pool, readPool.Pool);
		Assert.Equal(pool.Owned, readPool.Owned);
		Assert.True(MuiListtreeCore.MuiListtreeClickStateCodec.TryRead(ref platform, clickAddress,
			out var readClick));
		Assert.Equal(click.LastNode, readClick.LastNode);
		Assert.Equal(1u, readClick.TimestampValid);
		Assert.Equal(1u, readClick.DoubleClick);
	}

	[Fact]
	public void MalformedListtreeMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var policyAddress = APTR.FromPointer(0x1600);
		var poolAddress = APTR.FromPointer(0x1640);
		var clickAddress = APTR.FromPointer(0x1680);
		Assert.True(MuiListtreeCore.MuiListtreePolicyStateRecordCodec.Write(ref platform,
			policyAddress, new MuiListtreeCore.MuiListtreePolicyStateRecord
			{
				Magic = MuiListtreeCore.MuiListtreePolicyStateRecord.Cookie,
				DuplicateNodeName = 1,
			}));
		Assert.True(MuiListtreeCore.MuiListtreeHookPoolStateRecordCodec.Write(ref platform,
			poolAddress, new MuiListtreeCore.MuiListtreeHookPoolStateRecord
			{
				Magic = MuiListtreeCore.MuiListtreeHookPoolStateRecord.Cookie,
				Pool = APTR.FromPointer(0x1A00),
				Owned = 1,
			}));
		Assert.True(MuiListtreeCore.MuiListtreeClickStateCodec.Write(ref platform, clickAddress,
			new MuiListtreeCore.MuiListtreeClickState
			{
				Magic = MuiListtreeCore.MuiListtreeClickState.Cookie,
				Clicks = 1,
			}));
		Assert.True(MuiListtreeCore.MuiListtreePolicyFieldCursorCodec.TryWriteUInt32(ref platform,
			policyAddress, MuiListtreeCore.MuiListtreePolicyField.Magic, 0));
		Assert.True(MuiListtreeCore.MuiListtreeHookPoolFieldCursorCodec.TryWriteUInt32(ref platform,
			poolAddress, MuiListtreeCore.MuiListtreeHookPoolField.Magic, 0));
		Assert.True(MuiListtreeCore.MuiListtreeClickStateFieldCursorCodec.TryWriteUInt32(ref platform,
			clickAddress, MuiListtreeCore.MuiListtreeClickStateField.Magic, 0));

		Assert.True(MuiListtreeCore.MuiListtreePolicyStateRecordCodec.TryReadStructural(ref platform,
			policyAddress, out var policy));
		Assert.Equal(0u, policy.Magic);
		Assert.False(MuiListtreeCore.MuiListtreePolicyStateRecordCodec.TryRead(ref platform,
			policyAddress, out _));
		Assert.True(MuiListtreeCore.MuiListtreeHookPoolStateRecordCodec.TryReadStructural(ref platform,
			poolAddress, out var pool));
		Assert.Equal(0u, pool.Magic);
		Assert.False(MuiListtreeCore.MuiListtreeHookPoolStateRecordCodec.TryRead(ref platform,
			poolAddress, out _));
		Assert.True(MuiListtreeCore.MuiListtreeClickStateCodec.TryReadStructural(ref platform,
			clickAddress, out var click));
		Assert.Equal(0u, click.Magic);
		Assert.False(MuiListtreeCore.MuiListtreeClickStateCodec.TryRead(ref platform,
			clickAddress, out _));
	}

	[Fact]
	public void ListtreeClickColumnSurfaceAndLifecycleRoundTripThroughStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var columnAddress = APTR.FromPointer(0x1700);
		var surfaceAddress = APTR.FromPointer(0x1740);
		var lifecycleAddress = APTR.FromPointer(0x1780);
		Assert.True(MuiListtreeCore.MuiListtreeClickColumnStateCodec.Write(ref platform,
			columnAddress, new MuiListtreeCore.MuiListtreeClickColumnState
			{
				Magic = MuiListtreeCore.MuiListtreeClickColumnState.Cookie,
				LastColumn = 3,
				Valid = 2,
			}));
		Assert.True(MuiListtreeCore.MuiListtreeSurfaceStateRecordCodec.Write(ref platform,
			surfaceAddress, new MuiListtreeCore.MuiListtreeSurfaceStateRecord
			{
				Magic = MuiListtreeCore.MuiListtreeSurfaceStateRecord.Cookie,
				Left = -4,
				Top = 8,
				Width = 320,
				Height = 180,
				RowHeight = 12,
				FirstVisible = 5,
			}));
		Assert.True(MuiListtreeCore.MuiListtreeLifecycleStateRecordCodec.Write(ref platform,
			lifecycleAddress, new MuiListtreeCore.MuiListtreeLifecycleStateRecord
			{
				Magic = MuiListtreeCore.MuiListtreeLifecycleStateRecord.Cookie,
				RenderInfo = APTR.FromPointer(0x1800),
				Setup = 1,
				Shown = 1,
			}));
		Assert.True(MuiListtreeCore.MuiListtreeClickColumnStateCodec.TryRead(ref platform,
			columnAddress, out var column));
		Assert.Equal(3u, column.LastColumn);
		Assert.Equal(1u, column.Valid);
		Assert.True(MuiListtreeCore.MuiListtreeSurfaceStateRecordCodec.TryRead(ref platform,
			surfaceAddress, out var surface));
		Assert.Equal(-4, surface.Left);
		Assert.Equal(180, surface.Height);
		Assert.True(MuiListtreeCore.MuiListtreeLifecycleStateRecordCodec.TryRead(ref platform,
			lifecycleAddress, out var lifecycle));
		Assert.Equal(APTR.FromPointer(0x1800), lifecycle.RenderInfo);
		Assert.Equal(1u, lifecycle.Shown);
	}

	[Fact]
	public void MalformedListtreeClickColumnSurfaceAndLifecycleMagicRemainStructural()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var columnAddress = APTR.FromPointer(0x1800);
		var surfaceAddress = APTR.FromPointer(0x1840);
		var lifecycleAddress = APTR.FromPointer(0x1880);
		Assert.True(MuiListtreeCore.MuiListtreeClickColumnStateCodec.Write(ref platform,
			columnAddress, new MuiListtreeCore.MuiListtreeClickColumnState
			{
				Magic = MuiListtreeCore.MuiListtreeClickColumnState.Cookie,
			}));
		Assert.True(MuiListtreeCore.MuiListtreeSurfaceStateRecordCodec.Write(ref platform,
			surfaceAddress, new MuiListtreeCore.MuiListtreeSurfaceStateRecord
			{
				Magic = MuiListtreeCore.MuiListtreeSurfaceStateRecord.Cookie,
			}));
		Assert.True(MuiListtreeCore.MuiListtreeLifecycleStateRecordCodec.Write(ref platform,
			lifecycleAddress, new MuiListtreeCore.MuiListtreeLifecycleStateRecord
			{
				Magic = MuiListtreeCore.MuiListtreeLifecycleStateRecord.Cookie,
			}));
		Assert.True(MuiListtreeCore.MuiListtreeClickColumnFieldCursorCodec.TryWriteUInt32(
			ref platform, columnAddress,
			MuiListtreeCore.MuiListtreeClickColumnField.Magic, 0));
		Assert.True(MuiListtreeCore.MuiListtreeSurfaceFieldCursorCodec.TryWriteUInt32(
			ref platform, surfaceAddress, MuiListtreeCore.MuiListtreeSurfaceField.Magic,
			0));
		Assert.True(MuiListtreeCore.MuiListtreeLifecycleFieldCursorCodec.TryWriteUInt32(
			ref platform, lifecycleAddress,
			MuiListtreeCore.MuiListtreeLifecycleField.Magic, 0));
		Assert.True(MuiListtreeCore.MuiListtreeClickColumnStateCodec.TryReadStructural(
			ref platform, columnAddress, out var column));
		Assert.Equal(0u, column.Magic);
		Assert.False(MuiListtreeCore.MuiListtreeClickColumnStateCodec.TryRead(ref platform,
			columnAddress, out _));
		Assert.True(MuiListtreeCore.MuiListtreeSurfaceStateRecordCodec.TryReadStructural(
			ref platform, surfaceAddress, out var surface));
		Assert.Equal(0u, surface.Magic);
		Assert.False(MuiListtreeCore.MuiListtreeSurfaceStateRecordCodec.TryRead(ref platform,
			surfaceAddress, out _));
		Assert.True(MuiListtreeCore.MuiListtreeLifecycleStateRecordCodec.TryReadStructural(
			ref platform, lifecycleAddress, out var lifecycle));
		Assert.Equal(0u, lifecycle.Magic);
		Assert.False(MuiListtreeCore.MuiListtreeLifecycleStateRecordCodec.TryRead(ref platform,
			lifecycleAddress, out _));
	}
}
