using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaContextMenuTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ContextMenuPacketsUseNamedMorphosFields()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1800);
		var menu = APTR.FromPointer(0x1900);
		var mxp = APTR.FromPointer(0x1A00);
		var myp = APTR.FromPointer(0x1A04);
		Assert.True(MuiAreaContextMenuMessageCodec.WriteAdd(ref platform, packet,
			menu, -8, 21, mxp, myp));
		Assert.True(MuiAreaContextMenuMessageCodec.TryReadAdd(ref platform, packet,
			out var add));
		Assert.Equal(menu, add.MenuStrip);
		Assert.Equal(-8, add.MouseX);
		Assert.Equal(21, add.MouseY);
		Assert.Equal(mxp, add.MouseXPointer);
		Assert.Equal(myp, add.MouseYPointer);

		var build = APTR.FromPointer(0x1B00);
		Assert.True(MuiAreaContextMenuMessageCodec.WriteBuild(ref platform, build,
			3, -4));
		Assert.True(MuiAreaContextMenuMessageCodec.TryReadBuild(ref platform, build,
			out var buildPacket));
		Assert.Equal(3, buildPacket.MouseX);
		Assert.Equal(-4, buildPacket.MouseY);

		var choice = APTR.FromPointer(0x1C00);
		var item = APTR.FromPointer(0x1D00);
		Assert.True(MuiAreaContextMenuMessageCodec.WriteChoice(ref platform, choice,
			item));
		Assert.True(MuiAreaContextMenuMessageCodec.TryReadChoice(ref platform,
			choice, out var choicePacket));
		Assert.Equal(item, choicePacket.Item);
		Assert.False(MuiAreaContextMenuMessageCodec.TryReadChoice(ref platform,
			APTR.FromPointer(0x21000u), out _));
	}

	[Fact]
	public void ContextMenuAttributeAndDefaultChoicePublishNamedTrigger()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var menu = APTR.FromPointer(0x1E00);
		Assert.True(MuiAreaContextMenuPacketCore.Set(ref platform, State, obj,
			menu));
		Assert.True(MuiAreaContextMenuPacketCore.TryGet(ref platform, State, obj,
			out var state));
		Assert.Equal(menu, state.MenuStrip);
		Assert.Equal(APTR.Null, state.Trigger);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj,
			MuiCommonControlCore.ContextMenu, out var menuValue, out var menuHandled));
		Assert.True(menuHandled);
		Assert.Equal(menu.Raw, menuValue);

		var item = APTR.FromPointer(0x1F00);
		var packet = APTR.FromPointer(0x2000);
		Assert.True(MuiAreaContextMenuMessageCodec.WriteChoice(ref platform, packet,
			item));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));
		Assert.True(MuiAreaContextMenuPacketCore.TryGet(ref platform, State, obj,
			out state));
		Assert.Equal(item, state.Trigger);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj,
			MuiCommonControlCore.ContextMenuTrigger, out var triggerValue,
			out var triggerHandled));
		Assert.True(triggerHandled);
		Assert.Equal(item.Raw, triggerValue);
	}

	[Fact]
	public void ContextMenuAddUsesProviderAndChoiceCanBeConsumed()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		platform.ContextMenuAddSampleAvailable = true;
		platform.ContextMenuAddResult = 0x55u;
		var packet = APTR.FromPointer(0x1800);
		var menu = APTR.FromPointer(0x1900);
		var mxp = APTR.FromPointer(0x1A00);
		var myp = APTR.FromPointer(0x1A04);
		Assert.True(MuiAreaContextMenuMessageCodec.WriteAdd(ref platform, packet,
			menu, -2, 7, mxp, myp));
		Assert.Equal(0x55u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));
		Assert.Equal(obj, platform.LastContextMenuAddObject);
		Assert.Equal(menu, platform.LastContextMenuAddMenuStrip);
		Assert.Equal(-2, platform.LastContextMenuAddMouseX);
		Assert.Equal(7, platform.LastContextMenuAddMouseY);
		Assert.Equal(mxp, platform.LastContextMenuAddMouseXPointer);
		Assert.Equal(myp, platform.LastContextMenuAddMouseYPointer);

		platform.ContextMenuChoiceSampleAvailable = true;
		var item = APTR.FromPointer(0x1B00);
		Assert.True(MuiAreaContextMenuMessageCodec.WriteChoice(ref platform, packet,
			item));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));
		Assert.Equal(obj, platform.LastContextMenuChoiceObject);
		Assert.Equal(item, platform.LastContextMenuChoiceItem);
		Assert.True(MuiAreaContextMenuPacketCore.TryGet(ref platform, State, obj,
			out var state));
		Assert.Equal(APTR.Null, state.Trigger);
	}

	[Fact]
	public void ContextMenuAddRejectsUnmappedCoordinateStorage()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		platform.ContextMenuAddSampleAvailable = true;
		var packet = APTR.FromPointer(0x1800);
		Assert.True(MuiAreaContextMenuMessageCodec.WriteAdd(ref platform, packet,
			APTR.FromPointer(0x1900), 1, 2, APTR.FromPointer(0x21000u), APTR.Null));
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));
		Assert.Equal(APTR.Null, platform.LastContextMenuAddObject);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR areaClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Text.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		areaClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
