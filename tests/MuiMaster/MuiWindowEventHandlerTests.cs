using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiWindowEventHandlerTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void WindowEventHandlerPacketsUseNamedRecordsAndDispatch()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var target = Object(ref platform, cl);
		var handler = APTR.FromPointer(0x1300);
		var eventMessage = APTR.FromPointer(0x1400);
		platform.WriteUInt16(handler, 10, 0x0002);
		platform.WriteUInt32(handler, 12, target.Raw);
		platform.WriteUInt32(handler, 20, 4);
		platform.WriteUInt32(eventMessage, 0, 0x90000001);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.WindowAddEventHandlerMethod);
		platform.WriteUInt32(packet, 4, handler.Raw);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowEventHandler(
			ref platform, State, window, packet));
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(target, platform.LastDispatchObject);

		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.WindowRemoveEventHandlerMethod);
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowEventHandler(
			ref platform, State, window, packet));
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
	}

	[Fact]
	public void WindowEventHandlerPacketsRejectMalformedAndDeadCalls()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowEventHandler(
			ref platform, State, window, packet));
		var truncated = APTR.FromPointer(0x20FFC);
		platform.WriteUInt32(truncated, 0,
			MuiApplicationDispatcher.WindowAddEventHandlerMethod);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowEventHandler(
			ref platform, State, window, truncated));
		var unmapped = APTR.FromPointer(0x21000);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowEventHandler(
			ref platform, State, window, unmapped));

		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.WindowAddEventHandlerMethod);
		platform.WriteUInt32(packet, 4, 0);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowEventHandler(
			ref platform, State, window, packet));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			window));
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.WindowRemoveEventHandlerMethod);
		Assert.Equal(0u, MuiApplicationDispatcher.DispatchWindowEventHandler(
			ref platform, State, window, packet));
	}

	[Fact]
	public void WindowEventHandlerReaderUsesNamedMethodHeader()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.WindowAddEventHandlerMethod);
		platform.WriteUInt32(packet, 4, 0x1300);
		var request = new MuiApplicationMenuPacketCodec.MenuPacketAddress
		{
			Address = packet,
			Method = MuiApplicationDispatcher.WindowAddEventHandlerMethod,
		};
		Assert.True(MuiApplicationMenuPacketCodec.TryReadWindowEventHandler(
			ref platform, ref request, out var value));
		Assert.Equal(request.Method, value.MethodId);
		Assert.Equal(0x1300u, value.Handler);

		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.False(MuiApplicationMenuPacketCodec.TryReadWindowEventHandler(
			ref platform, ref request, out _));
	}

	[Fact]
	public void WindowEventHandlerPacketCoreUsesNamedRecordCodec()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		var handler = APTR.FromPointer(0x1300);

		Assert.True(MuiWindowEventHandlerPacketCore.Write(ref platform, packet,
			new MuiWindowEventHandlerPacketInput
			{
				MethodId = MuiApplicationDispatcher.WindowAddEventHandlerMethod,
				Handler = handler
			}));
		Assert.True(MuiWindowEventHandlerPacketCore.TryRead(ref platform, packet,
			out var value));
		Assert.Equal(MuiApplicationDispatcher.WindowAddEventHandlerMethod,
			value.MethodId);
		Assert.Equal(handler, value.Handler);

		platform.WriteUInt32(packet, 0,
			MuiApplicationDispatcher.WindowRemoveEventHandlerMethod);
		Assert.True(MuiWindowEventHandlerPacketCore.TryRead(ref platform, packet,
			out value));
		Assert.Equal(MuiApplicationDispatcher.WindowRemoveEventHandlerMethod,
			value.MethodId);

		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.False(MuiWindowEventHandlerPacketCore.TryRead(ref platform, packet,
			out _));
		Assert.False(MuiWindowEventHandlerPacketCore.Write(ref platform, packet,
			new MuiWindowEventHandlerPacketInput
			{
				MethodId = 0xDEADBEEFu,
				Handler = handler
			}));
	}

	[Fact]
	public void EventHandlerEnabledFlagTracksRegistrationAndCleanup()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var target = Object(ref platform, cl);
		var handler = APTR.FromPointer(0x1450);
		var packet = APTR.FromPointer(0x1460);
		WriteHandler(ref platform, handler.Raw, target, 0);
		Assert.True(MuiApplicationWindowRecordPacketCore.TryReadEventHandler(
			ref platform, handler, out var initial));
		Assert.Equal((ushort)0, (ushort)(initial.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISENABLED));

		AddHandler(ref platform, window, packet, handler.Raw);
		Assert.True(MuiApplicationWindowRecordPacketCore.TryReadEventHandler(
			ref platform, handler, out var registered));
		Assert.NotEqual((ushort)0, (ushort)(registered.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISENABLED));

		Assert.True(MuiWindowEventHandlerPacketCore.Write(ref platform, packet,
			new MuiWindowEventHandlerPacketInput
			{
				MethodId = MuiApplicationDispatcher.WindowRemoveEventHandlerMethod,
				Handler = handler
			}));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowEventHandler(
			ref platform, State, window, packet));
		Assert.True(MuiApplicationWindowRecordPacketCore.TryReadEventHandler(
			ref platform, handler, out var removed));
		Assert.Equal((ushort)0, (ushort)(removed.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISENABLED));

		AddHandler(ref platform, window, packet, handler.Raw);
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			window));
		Assert.True(MuiApplicationWindowRecordPacketCore.TryReadEventHandler(
			ref platform, handler, out var disposed));
		Assert.Equal((ushort)0, (ushort)(disposed.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISENABLED));
	}

	[Fact]
	public void EventHandlerActiveFlagTracksWindowActiveAndDefaultObjects()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var active = Object(ref platform, cl);
		var defaultObject = Object(ref platform, cl);
		var other = Object(ref platform, cl);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			window, 0x80427925, active.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			window, 0x804294D7, defaultObject.Raw, false));

		var activeHandler = APTR.FromPointer(0x14A0);
		var defaultHandler = APTR.FromPointer(0x14D0);
		var otherHandler = APTR.FromPointer(0x1500);
		var packet = APTR.FromPointer(0x1530);
		var eventMessage = APTR.FromPointer(0x1560);
		WriteHandler(ref platform, activeHandler.Raw, active, 0);
		WriteHandler(ref platform, defaultHandler.Raw, defaultObject, 0);
		WriteHandler(ref platform, otherHandler.Raw, other, 0);
		Assert.True(MuiCommonControlPacketCore.WriteMethod(ref platform,
			eventMessage, 0x90000001));
		AddHandler(ref platform, window, packet, activeHandler.Raw);
		AddHandler(ref platform, window, packet, defaultHandler.Raw);
		AddHandler(ref platform, window, packet, otherHandler.Raw);

		Assert.True(MuiApplicationWindowRecordPacketCore.TryReadEventHandler(
			ref platform, activeHandler, out var activeRecord));
		Assert.True(MuiApplicationWindowRecordPacketCore.TryReadEventHandler(
			ref platform, defaultHandler, out var defaultRecord));
		Assert.True(MuiApplicationWindowRecordPacketCore.TryReadEventHandler(
			ref platform, otherHandler, out var otherRecord));
		Assert.NotEqual((ushort)0, (ushort)(activeRecord.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE));
		Assert.NotEqual((ushort)0, (ushort)(defaultRecord.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE));
		Assert.Equal((ushort)0, (ushort)(otherRecord.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE));

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			window, 0x80427925, other.Raw, false));
		platform.DispatchResult = 0;
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.True(MuiApplicationWindowRecordPacketCore.TryReadEventHandler(
			ref platform, activeHandler, out activeRecord));
		Assert.True(MuiApplicationWindowRecordPacketCore.TryReadEventHandler(
			ref platform, defaultHandler, out defaultRecord));
		Assert.True(MuiApplicationWindowRecordPacketCore.TryReadEventHandler(
			ref platform, otherHandler, out otherRecord));
		Assert.Equal((ushort)0, (ushort)(activeRecord.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE));
		Assert.NotEqual((ushort)0, (ushort)(defaultRecord.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE));
		Assert.NotEqual((ushort)0, (ushort)(otherRecord.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE));

		Assert.True(MuiWindowEventHandlerPacketCore.Write(ref platform, packet,
			new MuiWindowEventHandlerPacketInput
			{
				MethodId = MuiApplicationDispatcher.WindowRemoveEventHandlerMethod,
				Handler = activeHandler
			}));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowEventHandler(
			ref platform, State, window, packet));
		Assert.True(MuiApplicationWindowRecordPacketCore.TryReadEventHandler(
			ref platform, activeHandler, out activeRecord));
		Assert.Equal((ushort)0, (ushort)(activeRecord.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE));
	}

	[Fact]
	public void DefaultObjectSetPacketRefreshesActiveHandlerState()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var first = Object(ref platform, cl);
		var second = Object(ref platform, cl);
		var firstHandler = APTR.FromPointer(0x1600);
		var secondHandler = APTR.FromPointer(0x1630);
		var packet = APTR.FromPointer(0x1660);
		WriteHandler(ref platform, firstHandler.Raw, first, 0);
		WriteHandler(ref platform, secondHandler.Raw, second, 0);
		AddHandler(ref platform, window, packet, firstHandler.Raw);
		AddHandler(ref platform, window, packet, secondHandler.Raw);

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, 0x804294D7, first.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiApplicationWindowRecordPacketCore.TryReadEventHandler(
			ref platform, firstHandler, out var firstRecord));
		Assert.True(MuiApplicationWindowRecordPacketCore.TryReadEventHandler(
			ref platform, secondHandler, out var secondRecord));
		Assert.NotEqual((ushort)0, (ushort)(firstRecord.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE));
		Assert.Equal((ushort)0, (ushort)(secondRecord.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE));

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.NoNotifySet, 0x804294D7, second.Raw));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiApplicationWindowRecordPacketCore.TryReadEventHandler(
			ref platform, firstHandler, out firstRecord));
		Assert.True(MuiApplicationWindowRecordPacketCore.TryReadEventHandler(
			ref platform, secondHandler, out secondRecord));
		Assert.Equal((ushort)0, (ushort)(firstRecord.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE));
		Assert.NotEqual((ushort)0, (ushort)(secondRecord.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE));

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, 0x804294D7, 0));
		Assert.Equal(1u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiApplicationWindowRecordPacketCore.TryReadEventHandler(
			ref platform, secondHandler, out secondRecord));
		Assert.Equal((ushort)0, (ushort)(secondRecord.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE));

		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			packet, MuiCommonControlPacketCore.Set, 0x804294D7, 0x1F00));
		Assert.Equal(0u, MuiApplicationDispatcher.Dispatch(ref platform, State,
			window, packet));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x804294D7, out var storedDefault));
		Assert.Equal(0u, storedDefault);
	}

	[Fact]
	public void EventHandlerNodeCallbackHonorsGuiModeAndEventMask()
	{
		var platform = CreatePlatform(out var cl);
		var target = Object(ref platform, cl);
		var handler = APTR.FromPointer(0x1500);
		var eventMessage = APTR.FromPointer(0x1600);
		platform.WriteUInt16(handler, 10, 0x0002);
		platform.WriteUInt32(handler, 12, target.Raw);
		platform.WriteUInt32(handler, 20, 4);
		platform.WriteUInt32(eventMessage, 0, 0x90000001);
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchEventHandlerNode(
			ref platform, handler, eventMessage, 4));
		Assert.Equal(target, platform.LastDispatchObject);
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchEventHandlerNode(
			ref platform, handler, eventMessage, 8));
		platform.WriteUInt16(handler, 10, 0);
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchEventHandlerNode(
			ref platform, handler, eventMessage, 4));
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchEventHandlerNode(
			ref platform, APTR.FromPointer(0x20FFCu), eventMessage, 4));
	}

	[Fact]
	public void EventHandlerNodeUsesExplicitClassCoercionWhenClassIsSupplied()
	{
		var platform = CreatePlatform(out var cl);
		var target = Object(ref platform, cl);
		var classPointer = MuiHeadlessObjectCore.ClassPointer(ref platform, cl);
		var handler = APTR.FromPointer(0x1700);
		var eventMessage = APTR.FromPointer(0x1800);
		platform.WriteUInt16(handler, 10, 0x0002);
		platform.WriteUInt32(handler, 12, target.Raw);
		platform.WriteUInt32(handler, 16, classPointer.Raw);
		platform.WriteUInt32(handler, 20, 4);
		platform.WriteUInt32(eventMessage, 0, 0x90000001);

		Assert.Equal(1u, MuiApplicationWindowCore.DispatchEventHandlerNode(
			ref platform, handler, eventMessage, 4));
		Assert.Equal(target, platform.LastDispatchObject);
		Assert.Equal(0x90000001u, platform.LastDispatchMethod);
		Assert.Equal(classPointer.Raw, platform.LastDispatchArgument);
		Assert.Equal(0u, platform.DispatchCount);
	}

	[Fact]
	public void EventHandlerCallingFlagIsSetOnlyDuringTypedCallback()
	{
		var platform = CreatePlatform(out var cl);
		var target = Object(ref platform, cl);
		var handler = APTR.FromPointer(0x1780);
		var eventMessage = APTR.FromPointer(0x1790);
		WriteHandler(ref platform, handler.Raw, target, 0);
		Assert.True(MuiCommonControlPacketCore.WriteMethod(ref platform,
			eventMessage, 0x90000001));
		platform.ObservedHandler = handler;

		Assert.Equal(1u, MuiApplicationWindowCore.DispatchEventHandlerNode(
			ref platform, handler, eventMessage, 4));
		Assert.True(platform.CallingFlagObserved);
		Assert.True(MuiApplicationWindowRecordPacketCore.TryReadEventHandler(
			ref platform, handler, out var completed));
		Assert.Equal((ushort)0, (ushort)(completed.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISCALLING));
	}

	[Fact]
	public void EventHandlerReturnCodeIsPreservedAndOnlyEatStopsWindowQueue()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var target = Object(ref platform, cl);
		var secondTarget = Object(ref platform, cl);
		var handler = APTR.FromPointer(0x1850);
		var secondHandler = APTR.FromPointer(0x1880);
		var eventMessage = APTR.FromPointer(0x1900);
		var secondPacket = APTR.FromPointer(0x18C0);
		platform.WriteUInt16(handler, 10, 0x0002);
		platform.WriteUInt32(handler, 12, target.Raw);
		platform.WriteUInt32(handler, 20, 4);
		platform.WriteUInt16(secondHandler, 10, 0x0002);
		platform.WriteUInt32(secondHandler, 12, secondTarget.Raw);
		platform.WriteUInt32(secondHandler, 20, 4);
		platform.WriteUInt32(eventMessage, 0, 0x90000077);

		platform.DispatchResult = 0x77;
		Assert.Equal(0x77u, MuiApplicationWindowCore.DispatchEventHandlerNode(
			ref platform, handler, eventMessage, 4));
		Assert.Equal(0x77u, MuiApplicationWindowCore.DispatchEventHandlerNode(
			ref platform, State, handler, eventMessage, 4));
		AddHandler(ref platform, window, APTR.FromPointer(0x1870), handler.Raw);
		AddHandler(ref platform, window, secondPacket, secondHandler.Raw);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			0x7FFE0012, out var eventList));
		Assert.NotEqual(0u, eventList);
		Assert.Equal(handler.Raw, platform.ReadUInt32(
			APTR.FromPointer(eventList), 4));
		Assert.Equal(4u, platform.ReadUInt32(handler, 20));
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(4u, platform.DispatchCount);

		platform.DispatchResult = 1;
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(5u, platform.DispatchCount);
	}

	[Fact]
	public void WindowEventHandlersUsePriorityAndActiveDefaultPrecedence()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var highObject = Object(ref platform, cl);
		var normalObject = Object(ref platform, cl);
		var negativeObject = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1400);
		var eventMessage = APTR.FromPointer(0x1600);
		WriteHandler(ref platform, 0x1500, highObject, 10);
		WriteHandler(ref platform, 0x1540, normalObject, 0);
		WriteHandler(ref platform, 0x1580, negativeObject, -5);
		platform.WriteUInt32(eventMessage, 0, 0x90000001);

		AddHandler(ref platform, window, packet, 0x1500);
		AddHandler(ref platform, window, packet, 0x1540);
		AddHandler(ref platform, window, packet, 0x1580);
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(highObject, platform.LastDispatchObject);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			0x80427925, negativeObject.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			0x804294D7, normalObject.Raw, false));
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(negativeObject, platform.LastDispatchObject);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			0x80427925, 0, false));
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(normalObject, platform.LastDispatchObject);
	}

	[Fact]
	public void GuiModeSkipsDisabledAndHiddenObjectsExceptWindowStateEvents()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var target = Object(ref platform, cl);
		var packet = APTR.FromPointer(0x1900);
		var handler = APTR.FromPointer(0x1A00);
		var eventMessage = APTR.FromPointer(0x1B00);
		platform.WriteUInt16(handler, 10, 0x0002);
		platform.WriteUInt32(handler, 12, target.Raw);
		platform.WriteUInt32(handler, 20, 4);
		platform.WriteUInt32(eventMessage, 0, 0x90000001);
		AddHandler(ref platform, window, packet, 0x1A00);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, target,
			0x80423661, 1, false));
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, target,
			0x80423661, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, target,
			0x80429BA8, 0, false));
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, target,
			0x80429BA8, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, target,
			0x7FFF0003, 0, false));
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));

		platform.WriteUInt32(handler, 20, 0x00040000);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, target,
			0x80423661, 1, false));
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 0x00040000));
		Assert.Equal(target, platform.LastDispatchObject);
	}

	[Fact]
	public void GuiModeEligibilityInheritsDisabledAndVisibilityFromAncestors()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var group = Object(ref platform, cl);
		var target = Object(ref platform, cl);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, group));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, target));

		var packet = APTR.FromPointer(0x1D00);
		var handler = APTR.FromPointer(0x1D40);
		var eventMessage = APTR.FromPointer(0x1D80);
		platform.WriteUInt16(handler, 10, 0x0002);
		platform.WriteUInt32(handler, 12, target.Raw);
		platform.WriteUInt32(handler, 20, 4);
		platform.WriteUInt32(eventMessage, 0, 0x90000001);
		AddHandler(ref platform, window, packet, handler.Raw);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			0x80423661, 1, false));
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			0x80423661, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			0x80429BA8, 0, false));
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			0x80429BA8, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			0x7FFF0003, 0, false));
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			0x7FFF0003, 1, false));
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(target, platform.LastDispatchObject);
	}

	[Fact]
	public void GuiModeSkipsInactivePageMembers()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var pageGroup = Object(ref platform, cl);
		var firstPage = Object(ref platform, cl);
		var secondPage = Object(ref platform, cl);
		var target = Object(ref platform, cl);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, pageGroup));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, pageGroup, firstPage));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, pageGroup, secondPage));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, secondPage, target));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			pageGroup, 0x80421A5F, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			pageGroup, 0x80424199, 0, false));

		var handler = APTR.FromPointer(0x1F40);
		var packet = APTR.FromPointer(0x1F00);
		var eventMessage = APTR.FromPointer(0x1FC0);
		WriteHandler(ref platform, handler.Raw, target, 0);
		Assert.True(MuiCommonControlPacketCore.WriteMethod(ref platform,
			eventMessage, 0x90000001));
		AddHandler(ref platform, window, packet, handler.Raw);

		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			pageGroup, 0x80424199, 1, false));
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(target, platform.LastDispatchObject);
	}

	[Fact]
	public void GuiModeSkipsObjectsOutsideVirtualGroupViewport()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var virtgroup = Object(ref platform, cl);
		var target = Object(ref platform, cl);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window,
			virtgroup));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, virtgroup,
			target));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			virtgroup, 0x80427C49, 300, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			virtgroup, 0x80423038, 150, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			virtgroup, 0x80429371, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			virtgroup, 0x80425200, 0, false));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, virtgroup,
			10, 20, 100, 60));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, target,
			500, 20, 10, 10));

		var handler = APTR.FromPointer(0x2140);
		var packet = APTR.FromPointer(0x2100);
		var eventMessage = APTR.FromPointer(0x2180);
		WriteHandler(ref platform, handler.Raw, target, 0);
		Assert.True(MuiCommonControlPacketCore.WriteMethod(ref platform,
			eventMessage, 0x90000001));
		AddHandler(ref platform, window, packet, handler.Raw);

		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, target,
			50, 20, 10, 10));
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(target, platform.LastDispatchObject);
	}

	[Fact]
	public void AlwaysKeysAllowsInactiveKeyboardHandlersOnlyForMuiKeys()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var active = Object(ref platform, cl);
		var normal = Object(ref platform, cl);
		var always = Object(ref platform, cl);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, window,
			0x80427925, active.Raw, false));

		var normalHandler = APTR.FromPointer(0x1E40);
		var alwaysHandler = APTR.FromPointer(0x1E80);
		var normalPacket = APTR.FromPointer(0x1E00);
		var alwaysPacket = APTR.FromPointer(0x1E10);
		var eventMessage = APTR.FromPointer(0x1EC0);
		WriteHandler(ref platform, normalHandler.Raw, normal, 0,
			flags: 0x0002);
		WriteHandler(ref platform, alwaysHandler.Raw, always, 0,
			flags: 0x0003);
		Assert.True(MuiCommonControlPacketCore.WriteHandleEvent(ref platform,
			eventMessage, 0, 2, 0));
		AddHandler(ref platform, window, normalPacket, normalHandler.Raw);
		AddHandler(ref platform, window, alwaysPacket, alwaysHandler.Raw);

		Assert.Equal(1u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(always, platform.LastDispatchObject);

		Assert.True(MuiCommonControlPacketCore.WriteHandleEvent(ref platform,
			eventMessage, 0, -1, 0));
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(normal, platform.LastDispatchObject);
	}

	[Fact]
	public void WindowDisableKeysRejectsMaskedMuiKeyBeforeHandlerDispatch()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var target = Object(ref platform, cl);
		var handler = APTR.FromPointer(0x1F00);
		var packet = APTR.FromPointer(0x1F30);
		var eventMessage = APTR.FromPointer(0x1F60);
		WriteHandler(ref platform, handler.Raw, target, 0);
		Assert.True(MuiCommonControlPacketCore.WriteHandleEvent(ref platform,
			eventMessage, 0, 2, 0));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			window, MuiWindowPublicCore.DisableKeys, 1u << 2, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			window, 0x80427925, target.Raw, false));
		AddHandler(ref platform, window, packet, handler.Raw);

		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(0u, platform.DispatchCount);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			window, MuiWindowPublicCore.DisableKeys, 0, false));
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(1u, platform.DispatchCount);
		Assert.Equal(target, platform.LastDispatchObject);
	}

	[Fact]
	public void MuiKeysVisitActiveParentsBeforeDefaultWithoutDuplicateDelivery()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var parent = Object(ref platform, cl);
		var active = Object(ref platform, cl);
		var defaultObject = Object(ref platform, cl);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, parent));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, parent, active));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			window, 0x80427925, active.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			window, 0x804294D7, defaultObject.Raw, false));

		var parentHandler = APTR.FromPointer(0x2300);
		var defaultHandler = APTR.FromPointer(0x2340);
		var parentPacket = APTR.FromPointer(0x2380);
		var defaultPacket = APTR.FromPointer(0x23A0);
		var eventMessage = APTR.FromPointer(0x23C0);
		WriteHandler(ref platform, parentHandler.Raw, parent, 0);
		WriteHandler(ref platform, defaultHandler.Raw, defaultObject, 0);
		Assert.True(MuiCommonControlPacketCore.WriteHandleEvent(ref platform,
			eventMessage, 0x90000001, 2, 0));
		AddHandler(ref platform, window, parentPacket, parentHandler.Raw);
		AddHandler(ref platform, window, defaultPacket, defaultHandler.Raw);

		// An active object's parent gets the MUI key before the default object.
		// With a non-eat result it remains excluded from the final queue pass.
		platform.DispatchResult = 1;
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(parent, platform.LastDispatchObject);

		var baseline = platform.DispatchCount;
		platform.DispatchResult = 0;
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(2u, platform.DispatchCount - baseline);
		Assert.Equal(defaultObject, platform.LastDispatchObject);

		// Non-eat values other than zero (the common 0x77 probe value) also
		// continue through the default and remaining passes.
		baseline = platform.DispatchCount;
		platform.DispatchResult = 0x77;
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(2u, platform.DispatchCount - baseline);
		Assert.Equal(defaultObject, platform.LastDispatchObject);
	}

	[Fact]
	public void PriorityHandlersHaveAbsoluteFocusAndAreNotVisitedTwice()
	{
		var platform = CreatePlatform(out var cl);
		var window = Object(ref platform, cl);
		var active = Object(ref platform, cl);
		var temporary = Object(ref platform, cl);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			window, 0x80427925, active.Raw, false));

		var activeHandler = APTR.FromPointer(0x2200);
		var priorityHandler = APTR.FromPointer(0x2240);
		var activePacket = APTR.FromPointer(0x2280);
		var priorityPacket = APTR.FromPointer(0x22A0);
		var eventMessage = APTR.FromPointer(0x22C0);
		WriteHandler(ref platform, activeHandler.Raw, active, 100);
		WriteHandler(ref platform, priorityHandler.Raw, temporary, -100,
			flags: 0x0802);
		Assert.True(MuiCommonControlPacketCore.WriteMethod(ref platform,
			eventMessage, 0x90000001));
		AddHandler(ref platform, window, activePacket, activeHandler.Raw);
		AddHandler(ref platform, window, priorityPacket, priorityHandler.Raw);

		// A priority handler is checked before the active object even when its
		// signed ehn_Priority is lower. With a non-eat result, it must not be
		// revisited by the ordinary active/default/remaining passes.
		platform.DispatchResult = 0;
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(2u, platform.DispatchCount);
		Assert.Equal(active, platform.LastDispatchObject);

		platform.DispatchResult = 1;
		Assert.Equal(1u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(temporary, platform.LastDispatchObject);
	}

	private static void AddHandler(ref MuiHeadlessTestPlatform platform,
		APTR window, APTR packet, uint handler)
	{
		Assert.True(MuiWindowEventHandlerPacketCore.Write(ref platform, packet,
			new MuiWindowEventHandlerPacketInput
			{
				MethodId = MuiApplicationDispatcher.WindowAddEventHandlerMethod,
				Handler = APTR.FromPointer(handler)
			}));
		Assert.Equal(1u, MuiApplicationDispatcher.DispatchWindowEventHandler(
			ref platform, State, window, packet));
	}

	private static void WriteHandler(ref MuiHeadlessTestPlatform platform,
		uint address, APTR obj, sbyte priority, ushort flags = 0x0002)
	{
		var handler = APTR.FromPointer(address);
		Assert.True(MuiApplicationWindowRecordPacketCore.WriteEventHandler(
			ref platform, handler, new MuiEventHandlerNodeInput
			{
				Priority = priority,
				Flags = flags,
				Object = obj,
				Events = 4
			}));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR cl)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Window.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR Object(ref MuiHeadlessTestPlatform platform, APTR cl) =>
		MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl, APTR.Null);
}
