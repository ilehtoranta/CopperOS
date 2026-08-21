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
}
