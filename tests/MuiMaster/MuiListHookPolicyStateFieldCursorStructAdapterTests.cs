using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListHookPolicyStateFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedHookPolicyRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListHookPolicyState
		{
			Magic = MuiListCore.MuiListHookPolicyState.Cookie,
			ConstructHook = 0x100,
			DestructHook = 0x200,
			DisplayHook = 0x300,
			CompareHook = 0x400,
			MultiTestHook = 0x500,
		};
		Assert.True(MuiListCore.MuiListHookPolicyStateCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListHookPolicyStateFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListHookPolicyStateField.MultiTestHook,
		};
		Assert.True(MuiListCore.MuiListHookPolicyStateFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3514u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListHookPolicyStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListHookPolicyStateField.CompareHook, 0x444));
		Assert.True(MuiListCore.MuiListHookPolicyStateFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListHookPolicyStateField.DisplayHook, out var display));
		Assert.Equal(0x300u, display);
		Assert.True(MuiListCore.MuiListHookPolicyStateCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.ConstructHook, decoded.ConstructHook);
		Assert.Equal(value.DestructHook, decoded.DestructHook);
		Assert.Equal(value.DisplayHook, decoded.DisplayHook);
		Assert.Equal(0x444u, decoded.CompareHook);
		Assert.Equal(value.MultiTestHook, decoded.MultiTestHook);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListHookPolicyStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListHookPolicyStateFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListHookPolicyStateField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListHookPolicyStateFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListHookPolicyStateFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListHookPolicyStateField.Magic,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListHookPolicyStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListCore.MuiListHookPolicyStateField.Magic,
			out _, out _));
	}
}
