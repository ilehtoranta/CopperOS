using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaContextMenuTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ContextMenuPacketsUseNamedMorphosFields()
	{
		Assert.Equal(28, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiContextMenuAddSample>());
		Assert.Equal(0, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiContextMenuAddSample>(nameof(MuiContextMenuAddSample.Object)).ToInt32());
		Assert.Equal(16, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiContextMenuAddSample>(nameof(MuiContextMenuAddSample.MouseXPointer)).ToInt32());
		Assert.Equal(24, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiContextMenuAddSample>(nameof(MuiContextMenuAddSample.Result)).ToInt32());
		Assert.Equal(8, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiContextMenuChoiceSample>());
		Assert.Equal(4, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiContextMenuChoiceSample>(nameof(MuiContextMenuChoiceSample.Item)).ToInt32());

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
	public void ContextMenuMessageAdapterOwnsStructBounds()
	{
		var platform = CreatePlatform(out _);
		var add = APTR.FromPointer(0x2200);
		Assert.True(MuiAreaContextMenuMessageMemoryCodec.TryWriteUInt32(
			ref platform, add, MuiAreaContextMenuPacketKind.Add,
			MuiAreaContextMenuMessageField.MethodId,
			MuiAreaContextMenuMessageCodec.Add));
		Assert.True(MuiAreaContextMenuMessageMemoryCodec.TryWriteUInt32(
			ref platform, add, MuiAreaContextMenuPacketKind.Add,
			MuiAreaContextMenuMessageField.MouseX, unchecked((uint)-7)));
		Assert.True(MuiAreaContextMenuMessageMemoryCodec.TryReadUInt32(
			ref platform, add, MuiAreaContextMenuPacketKind.Add,
			MuiAreaContextMenuMessageField.MouseX, out var mouseX));
		Assert.Equal(unchecked((uint)-7), mouseX);
		Assert.True(MuiAreaContextMenuMessageMemoryCodec.TryGetAddress(
			ref platform, add, MuiAreaContextMenuPacketKind.Add,
			MuiAreaContextMenuMessageField.MouseYPointer, out var pointer));
		Assert.Equal(add.Raw + MuiAreaContextMenuAddMessage.MouseYPointerOffset,
			pointer.Raw);

		var choice = APTR.FromPointer(0x2240);
		Assert.True(MuiAreaContextMenuMessageMemoryCodec.TryWriteUInt32(
			ref platform, choice, MuiAreaContextMenuPacketKind.Choice,
			MuiAreaContextMenuMessageField.Item, 0x3300));
		Assert.True(MuiAreaContextMenuMessageMemoryCodec.TryReadUInt32(
			ref platform, choice, MuiAreaContextMenuPacketKind.Choice,
			MuiAreaContextMenuMessageField.Item, out var item));
		Assert.Equal(0x3300u, item);

		Assert.False(MuiAreaContextMenuMessageMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x20FF0),
			MuiAreaContextMenuPacketKind.Add,
			MuiAreaContextMenuMessageField.MouseYPointer, out _));
		Assert.False(MuiAreaContextMenuMessageMemoryCodec.TryGetAddress(
			ref platform, add, MuiAreaContextMenuPacketKind.Choice,
			MuiAreaContextMenuMessageField.MouseX, out _));
		Assert.False(MuiAreaContextMenuMessageMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaContextMenuPacketKind.Build,
			MuiAreaContextMenuMessageField.MouseX, out _));
	}

	[Fact]
	public void ContextMenuRecordUsesDedicatedStructCodec()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1E00);
		var value = default(MuiAreaContextMenuStateRecord);
		value.Magic = MuiAreaContextMenuStateRecord.Cookie;
		value.MenuStrip = APTR.FromPointer(0x1F00);
		value.Trigger = APTR.FromPointer(0x2000);
		value.Generation = 7;

		Assert.True(MuiAreaContextMenuStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiAreaContextMenuStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.MenuStrip, decoded.MenuStrip);
		Assert.Equal(value.Trigger, decoded.Trigger);
		Assert.Equal(value.Generation, decoded.Generation);
		Assert.True(MuiAreaContextMenuStateRecordCodec.TryRead(ref platform,
			address, out decoded));
		Assert.False(MuiAreaContextMenuStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
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
	public void ContextMenuBuildReturnsStaticMenuAndNullWhenUnset()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var packet = APTR.FromPointer(0x2100);
		Assert.True(MuiAreaContextMenuMessageCodec.WriteBuild(ref platform,
			packet, 12, 18));
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));

		var menu = APTR.FromPointer(0x2200);
		Assert.True(MuiAreaContextMenuPacketCore.Set(ref platform, State, obj,
			menu));
		Assert.Equal(menu.Raw, MuiCommonControlDispatcher.Dispatch(ref platform,
			State, obj, packet));
		Assert.Equal(menu.Raw, MuiLayoutDispatcher.Dispatch(ref platform, State,
			obj, packet));
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

	[Fact]
	public void ContextMenuAdmissionRequiresGenerationAndLiveOwner()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var valid = new MuiAreaContextMenuStateRecord
		{
			Magic = MuiAreaContextMenuStateRecord.Cookie,
			MenuStrip = APTR.FromPointer(0x1E00),
			Trigger = APTR.FromPointer(0x1F00),
			Generation = 1,
		};
		Assert.True(MuiAreaContextMenuStateAdmission.Validate(valid));
		Assert.True(MuiAreaContextMenuStateAdmission.ValidateLive(ref platform,
			State, obj, valid));
		var malformed = valid;
		malformed.Generation = 0;
		Assert.False(MuiAreaContextMenuStateAdmission.Validate(malformed));
		Assert.False(MuiAreaContextMenuStateAdmission.ValidateLive(ref platform,
			State, obj, malformed));
		Assert.False(MuiAreaContextMenuStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedContextMenuFailsClosedBeforeRawRepair()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var menu = APTR.FromPointer(0x1E00);
		Assert.True(MuiAreaContextMenuPacketCore.Set(ref platform, State, obj,
			menu));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, obj,
			MuiAreaContextMenuCore.StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiAreaContextMenuStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiAreaContextMenuStateField.Generation, 0));
		Assert.True(MuiAreaContextMenuStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Generation);
		Assert.False(MuiAreaContextMenuStateAdmission.Validate(structural));
		Assert.False(MuiAreaContextMenuStateRecordCodec.TryRead(ref platform,
			block, out _));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiAreaContextMenuPacketCore.TryGet(ref platform, State, obj,
			out _));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, obj,
			MuiAreaContextMenuCore.StateKey));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, obj,
			MuiCommonControlCore.ContextMenu, out var raw));
		Assert.Equal(menu.Raw, raw);
	}

	[Fact]
	public void ContextMenuBuildFailsClosedForMalformedTypedState()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var menu = APTR.FromPointer(0x2300);
		Assert.True(MuiAreaContextMenuPacketCore.Set(ref platform, State, obj,
			menu));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, obj,
			MuiAreaContextMenuCore.StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiAreaContextMenuStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiAreaContextMenuStateField.Generation, 0));
		var packet = APTR.FromPointer(0x2400);
		Assert.True(MuiAreaContextMenuMessageCodec.WriteBuild(ref platform,
			packet, 1, 2));
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));
		Assert.Equal(0u, MuiLayoutDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, obj,
			MuiCommonControlCore.ContextMenu, out var raw));
		Assert.Equal(menu.Raw, raw);
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
