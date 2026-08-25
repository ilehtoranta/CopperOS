using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaHandledEventsTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void RegistrationFollowsWindowParentAndOwnsNamedHandler()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			State);
		var windowName = APTR.FromPointer(0x1100);
		var groupName = APTR.FromPointer(0x1140);
		platform.WriteCString(windowName, "Window.mui");
		platform.WriteCString(groupName, "Group.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var windowClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, windowName, APTR.Null, 0, APTR.FromPointer(1));
		var groupClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, groupName, APTR.Null, 0, APTR.FromPointer(1));
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			windowClass, APTR.Null);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, group));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, child));

		const uint events = 0x00000200;
		Assert.True(MuiAreaEventHandlerPacketCore.SetHandledEvents(ref platform, State,
			child, events));
		Assert.True(MuiAreaEventHandlerPacketCore.TryGet(ref platform, State,
			child, out var publicState));
		Assert.Equal(events, publicState.Events);
		Assert.Equal(window, publicState.Window);
		Assert.True(publicState.Handler.IsNotNull);
		Assert.True(MuiAreaEventHandlerCore.TryGetHandledEventsState(
			ref platform, State, child, out var registration));
		Assert.Equal(events, registration.Events);
		Assert.Equal(window, registration.Window);
		Assert.True(registration.Handler.IsNotNull);
		Assert.True(MuiEventHandlerNodeCodec.TryRead(ref platform,
			registration.Handler, out var handler));
		Assert.Equal(child, handler.Object);
		Assert.Equal(events, handler.Events);
		Assert.NotEqual(0, handler.Flags & MuiEventHandlerNodeInput.MUI_EHF_GUIMODE);
		Assert.NotEqual(0, handler.Flags & MuiEventHandlerNodeInput.MUI_EHF_ISENABLED);
		Assert.True(MuiAreaEventHandlerCore.TryGetEventHandlerPolicy(
			ref platform, State, child, out var policyFlags, out var priority));
		Assert.NotEqual(0, policyFlags & MuiEventHandlerNodeInput.MUI_EHF_GUIMODE);
		Assert.Equal((sbyte)0, priority);

		Assert.True(MuiAreaEventHandlerPacketCore.SetEventHandlerAlwaysKeys(
			ref platform, State, child, true));
		Assert.True(MuiAreaEventHandlerPacketCore.SetEventHandlerGuiMode(
			ref platform, State, child, false));
		Assert.True(MuiAreaEventHandlerPacketCore.SetEventHandlerPriority(
			ref platform, State, child, -7));
		Assert.True(MuiAreaEventHandlerCore.TryGetHandledEventsState(
			ref platform, State, child, out registration));
		Assert.True(MuiEventHandlerNodeCodec.TryRead(ref platform,
			registration.Handler, out handler));
		Assert.NotEqual(0, handler.Flags & MuiEventHandlerNodeInput.MUI_EHF_ALWAYSKEYS);
		Assert.Equal(0, handler.Flags & MuiEventHandlerNodeInput.MUI_EHF_GUIMODE);
		Assert.Equal((sbyte)-7, handler.Priority);

		Assert.True(MuiAreaEventHandlerPacketCore.SetHandledEvents(ref platform, State,
			child, events | 0x00000400));
		Assert.True(MuiAreaEventHandlerCore.TryGetHandledEventsState(
			ref platform, State, child, out registration));
		Assert.Equal(events | 0x00000400, registration.Events);
		Assert.True(MuiEventHandlerNodeCodec.TryRead(ref platform,
			registration.Handler, out handler));
		Assert.NotEqual(0, handler.Flags & MuiEventHandlerNodeInput.MUI_EHF_ALWAYSKEYS);
		Assert.Equal(0, handler.Flags & MuiEventHandlerNodeInput.MUI_EHF_GUIMODE);
		Assert.Equal((sbyte)-7, handler.Priority);

		Assert.True(MuiFamilyCore.Remove(ref platform, State, group, child));
		Assert.True(MuiAreaEventHandlerCore.TryGetHandledEventsState(
			ref platform, State, child, out registration));
		Assert.Equal(APTR.Null, registration.Window);
		Assert.Equal(APTR.Null, registration.Handler);

		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, child));
		Assert.True(MuiAreaEventHandlerCore.TryGetHandledEventsState(
			ref platform, State, child, out registration));
		Assert.Equal(window, registration.Window);
		Assert.True(registration.Handler.IsNotNull);

		Assert.True(MuiAreaEventHandlerPacketCore.SetHandledEvents(ref platform, State,
			child, 0));
		Assert.False(MuiAreaEventHandlerCore.TryGetHandledEventsState(
			ref platform, State, child, out _));
	}

	[Fact]
	public void GeneratedHandledEventsNodeReceivesWindowEventAndDetaches()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			State);
		var windowName = APTR.FromPointer(0x1100);
		var groupName = APTR.FromPointer(0x1140);
		var packet = APTR.FromPointer(0x1800);
		platform.WriteCString(windowName, "Window.mui");
		platform.WriteCString(groupName, "Group.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var windowClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, windowName, APTR.Null, 0, APTR.FromPointer(1));
		var groupClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, groupName, APTR.Null, 0, APTR.FromPointer(1));
		var application = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			windowClass, APTR.Null);
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			windowClass, APTR.Null);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0x20));
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, window));
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State,
			window, 4));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, group));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, child));
		Assert.True(MuiAreaEventHandlerPacketCore.SetHandledEvents(ref platform,
			State, child, 4));
		Assert.True(MuiCommonControlPacketCore.WriteHandleEvent(ref platform,
			packet, APTR.FromPointer(0x90000077), -1, APTR.Null));
		platform.DispatchResult = 1;

		Assert.Equal(1u, MuiApplicationWindowCore.DispatchWindowEvent(ref platform,
			State, window, packet, 4));
		Assert.Equal(child, platform.LastDispatchObject);
		Assert.Equal(MuiCommonControlPacketCore.HandleEvent,
			platform.LastDispatchMethod);

		Assert.True(MuiFamilyCore.Remove(ref platform, State, group, child));
		Assert.True(MuiAreaEventHandlerPacketCore.TryGet(ref platform, State, child,
			out var detached));
		Assert.Equal(APTR.Null, detached.Window);
		Assert.Equal(APTR.Null, detached.Handler);
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(ref platform,
			State, window, packet, 4));
	}

	[Fact]
	public void PolicyCanBeSetBeforeHandledEventsAndWaitsForMask()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			State);
		var windowName = APTR.FromPointer(0x1100);
		var groupName = APTR.FromPointer(0x1140);
		platform.WriteCString(windowName, "Window.mui");
		platform.WriteCString(groupName, "Group.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var windowClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, windowName, APTR.Null, 0, APTR.FromPointer(1));
		var groupClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, groupName, APTR.Null, 0, APTR.FromPointer(1));
		var window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			windowClass, APTR.Null);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, group));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, child));

		Assert.True(MuiAreaEventHandlerPacketCore.SetEventHandlerAlwaysKeys(
			ref platform, State, child, true));
		Assert.True(MuiAreaEventHandlerPacketCore.SetEventHandlerGuiMode(
			ref platform, State, child, false));
		Assert.True(MuiAreaEventHandlerPacketCore.SetEventHandlerPriority(
			ref platform, State, child, -7));
		Assert.True(MuiAreaEventHandlerCore.TryGetHandledEventsState(
			ref platform, State, child, out var pending));
		Assert.Equal(0u, pending.Events);
		Assert.Equal(APTR.Null, pending.Window);
		Assert.Equal(APTR.Null, pending.Handler);
		Assert.NotEqual(0, pending.HandlerFlags &
			MuiEventHandlerNodeInput.MUI_EHF_ALWAYSKEYS);
		Assert.Equal(0, pending.HandlerFlags & MuiEventHandlerNodeInput.MUI_EHF_GUIMODE);
		Assert.Equal((sbyte)-7, pending.Priority);

		Assert.True(MuiAreaEventHandlerPacketCore.SetHandledEvents(ref platform, State,
			child, 0x00000200));
		Assert.True(MuiAreaEventHandlerCore.TryGetHandledEventsState(
			ref platform, State, child, out var active));
		Assert.Equal(0x00000200u, active.Events);
		Assert.Equal(window, active.Window);
		Assert.True(active.Handler.IsNotNull);
		Assert.True(MuiEventHandlerNodeCodec.TryRead(ref platform,
			active.Handler, out var handler));
		Assert.NotEqual(0, handler.Flags & MuiEventHandlerNodeInput.MUI_EHF_ALWAYSKEYS);
		Assert.Equal(0, handler.Flags & MuiEventHandlerNodeInput.MUI_EHF_GUIMODE);
		Assert.Equal((sbyte)-7, handler.Priority);
	}
}
