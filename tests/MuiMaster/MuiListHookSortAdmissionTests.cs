using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListHookSortAdmissionTests
{
	[Fact]
	public void ListHookAndSortRecordsRoundTripThroughNamedStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var hookAddress = APTR.FromPointer(0x3400);
		var sortAddress = APTR.FromPointer(0x3440);
		var hooks = new MuiListCore.MuiListHookPolicyState
		{
			Magic = MuiListCore.MuiListHookPolicyState.Cookie,
			ConstructHook = 0x1001,
			DestructHook = 0x1002,
			DisplayHook = 0x1003,
			CompareHook = 0x1004,
			MultiTestHook = 0x1005,
		};
		var sort = new MuiListCore.MuiListSortState
		{
			Magic = MuiListCore.MuiListSortState.Cookie,
			SortColumn = 3,
			TitleClick = 7,
		};
		Assert.True(MuiListCore.MuiListHookPolicyStateCodec.Write(ref platform,
			hookAddress, hooks));
		Assert.True(MuiListCore.MuiListSortStateCodec.Write(ref platform,
			sortAddress, sort));
		Assert.True(MuiListCore.MuiListHookPolicyStateCodec.TryRead(ref platform,
			hookAddress, out var readHooks));
		Assert.Equal(hooks.ConstructHook, readHooks.ConstructHook);
		Assert.Equal(hooks.DisplayHook, readHooks.DisplayHook);
		Assert.Equal(hooks.MultiTestHook, readHooks.MultiTestHook);
		Assert.True(MuiListCore.MuiListSortStateCodec.TryRead(ref platform,
			sortAddress, out var readSort));
		Assert.Equal(sort.SortColumn, readSort.SortColumn);
		Assert.Equal(sort.TitleClick, readSort.TitleClick);
	}

	[Fact]
	public void MalformedListHookAndSortMagicRemainStructuralButFailClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var hookAddress = APTR.FromPointer(0x3480);
		var sortAddress = APTR.FromPointer(0x34C0);
		Assert.True(MuiListCore.MuiListHookPolicyStateCodec.Write(ref platform,
			hookAddress, new MuiListCore.MuiListHookPolicyState
			{
				Magic = MuiListCore.MuiListHookPolicyState.Cookie,
				ConstructHook = 0x2001,
				MultiTestHook = 0x2005,
			}));
		Assert.True(MuiListCore.MuiListSortStateCodec.Write(ref platform,
			sortAddress, new MuiListCore.MuiListSortState
			{
				Magic = MuiListCore.MuiListSortState.Cookie,
				SortColumn = 5,
				TitleClick = 9,
			}));
		Assert.True(MuiListCore.MuiListHookPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, hookAddress,
				MuiListCore.MuiListHookPolicyStateField.Magic, 0));
		Assert.True(MuiListCore.MuiListSortStateFieldCursorCodec.TryWriteUInt32(
			ref platform, sortAddress,
				MuiListCore.MuiListSortStateField.Magic, 0));
		Assert.True(MuiListCore.MuiListHookPolicyStateCodec.TryReadStructural(
			ref platform, hookAddress, out var structuralHooks));
		Assert.Equal(0u, structuralHooks.Magic);
		Assert.Equal(0x2005u, structuralHooks.MultiTestHook);
		Assert.False(MuiListCore.MuiListHookPolicyStateCodec.TryRead(ref platform,
			hookAddress, out _));
		Assert.True(MuiListCore.MuiListHookPolicyStateCodec.TryReadStorage(
			ref platform, hookAddress, out _));
		Assert.True(MuiListCore.MuiListSortStateCodec.TryReadStructural(ref platform,
			sortAddress, out var structuralSort));
		Assert.Equal(0u, structuralSort.Magic);
		Assert.Equal(5u, structuralSort.SortColumn);
		Assert.False(MuiListCore.MuiListSortStateCodec.TryRead(ref platform,
			sortAddress, out _));
		Assert.True(MuiListCore.MuiListSortStateCodec.TryReadStorage(ref platform,
			sortAddress, out _));
	}
}
