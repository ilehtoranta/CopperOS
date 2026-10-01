/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Stable MorphOS Intuition raw-key codes used by native Window.HandleEvent.
// These are physical-key identifiers, not translated character values.
internal enum MuiNativeRawKeyCode : ushort
{
	S = 0x21,
	PageUp = 0x48,
	PageDown = 0x49,
	Up = 0x4C,
	Down = 0x4D,
	Right = 0x4E,
	Left = 0x4F,
	Help = 0x5F,
	Home = 0x70,
	End = 0x71,
}

// MUI's preprocessed HandleEvent keys. Keep values aligned with the shared
// common-control and collection dispatchers that consume these key numbers.
internal enum MuiNativeHandleEventKey : int
{
	Press = 0,
	None = -1,
	Up = 2,
	Down = 3,
	PageUp = 4,
	PageDown = 5,
	Top = 6,
	Bottom = 7,
	Left = 8,
	Right = 9,
	WordLeft = 10,
	WordRight = 11,
	LineStart = 12,
	LineEnd = 13,
	Help = 20,
}

// Native Application Input/NewInput drains only this application's open
// window ports. IntuiMessages remain owned by Intuition until ReplyMsg, so the
// complete HandleEvent call is synchronous and uses the original message.
internal static class MuiNativeApplicationWindowEvents
{
	private const uint MaximumMessagesPerWindow =
		MuiHeadlessLayout.MaximumTraversal;
	private const uint InputEventAttribute = MuiWindowPublicCore.InputEvent;
	private const uint RawKeyClass = MuiIntuiMessageCodec.RawKeyClass;
	private const uint RefreshWindowClass = (uint)IDCMPFlags.RefreshWindow;
	private const uint CloseWindowClass = (uint)IDCMPFlags.CloseWindow;
	private const uint GeometryChangeClass = (uint)IDCMPFlags.ChangeWindow;
	private const uint MouseMoveClass = (uint)IDCMPFlags.MouseMove;
	private const uint MouseButtonsClass = (uint)IDCMPFlags.MouseButtons;
	private const uint GadgetDownClass = (uint)IDCMPFlags.GadgetDown;
	private const uint GadgetUpClass = (uint)IDCMPFlags.GadgetUp;
	private const uint DiskInsertedClass = (uint)IDCMPFlags.DiskInserted;
	private const uint DiskRemovedClass = (uint)IDCMPFlags.DiskRemoved;
	private const uint CloseRequestAttribute = MuiWindowPublicCore.CloseRequest;
	private const uint ActivateAttribute = MuiWindowPublicCore.Activate;
	private const int MuiKeyNone = (int)MuiNativeHandleEventKey.None;
	internal const ushort RawKeyHelp = (ushort)MuiNativeRawKeyCode.Help;
	private const ushort ControlQualifier =
		(ushort)InputEventQualifier.Control;

	internal static int KnownMuiKeyForRawKey(
		MuiIntuiPointerMessage message)
	{
		if (message.Class != RawKeyClass) return MuiKeyNone;
		var rawKey = (MuiNativeRawKeyCode)message.Code;
		if ((message.Qualifier & ControlQualifier) != 0)
		{
			if (rawKey == MuiNativeRawKeyCode.Left)
				return (int)MuiNativeHandleEventKey.WordLeft;
			if (rawKey == MuiNativeRawKeyCode.Right)
				return (int)MuiNativeHandleEventKey.WordRight;
			if (rawKey == MuiNativeRawKeyCode.Home)
				return (int)MuiNativeHandleEventKey.Top;
			if (rawKey == MuiNativeRawKeyCode.End)
				return (int)MuiNativeHandleEventKey.Bottom;
		}
		return rawKey switch
		{
			MuiNativeRawKeyCode.Up => (int)MuiNativeHandleEventKey.Up,
			MuiNativeRawKeyCode.Down => (int)MuiNativeHandleEventKey.Down,
			MuiNativeRawKeyCode.PageUp =>
				(int)MuiNativeHandleEventKey.PageUp,
			MuiNativeRawKeyCode.PageDown =>
				(int)MuiNativeHandleEventKey.PageDown,
			MuiNativeRawKeyCode.Left => (int)MuiNativeHandleEventKey.Left,
			MuiNativeRawKeyCode.Right => (int)MuiNativeHandleEventKey.Right,
			MuiNativeRawKeyCode.Home =>
				(int)MuiNativeHandleEventKey.LineStart,
			MuiNativeRawKeyCode.End =>
				(int)MuiNativeHandleEventKey.LineEnd,
			MuiNativeRawKeyCode.Help => MuiHelpTriggerInput.HelpKey,
			_ => MuiKeyNone,
		};
	}

	internal static bool TryPrepareHandleEvent<TMemory>(ref TMemory memory,
		APTR intuiMessage, APTR handleEventPacket, out uint eventClass)
		where TMemory : struct, IMuiGuestMemory
	{
		eventClass = 0;
		if (!MuiIntuiMessageCodec.TryReadPointerRecord(ref memory, intuiMessage,
			out var eventMessage) || eventMessage.Class == 0) return false;
		eventClass = eventMessage.Class;
		var packet = default(MuiCommonHandleEventMessage);
		packet.MethodId = MuiCommonControlPacketCore.HandleEvent;
		packet.InputMessage = intuiMessage.Raw;
		packet.MuiKey = KnownMuiKeyForRawKey(eventMessage);
		packet.EventHandlerNode = 0;
		return MuiCommonHandleEventMessageCodec.TryWrite(ref memory,
			handleEventPacket, packet);
	}

	// MUI's normal application loop services IDCMP_REFRESHWINDOW itself. Route
	// that notification through the named Application method instead of exposing
	// an Intuition damage message as ordinary HandleEvent input.
	internal static bool TryDispatchApplicationCheckRefresh<TPlatform>(
		ref TPlatform platform, APTR application, APTR message)
		where TPlatform : struct, IMuiGuestMemory, IMuiBoopsiCapability
	{
		if (application.IsNull || message.IsNull || !platform.IsMapped(message,
			MuiApplicationCheckRefreshMessage.Size)) return false;
		var packet = default(MuiApplicationCheckRefreshMessage);
		packet.MethodId = MuiApplicationDispatcher.ApplicationCheckRefreshMethod;
		var admitted = MuiApplicationCheckRefreshMessageCodec.Write(ref platform,
			message, ref packet);
		if (admitted) platform.DoMethod(application, message);
		return admitted;
	}

	internal static bool TryTranslateInputEvent<TMemory>(
		ref TMemory memory, APTR intuiMessage, out InputEvent inputEvent)
		where TMemory : struct, IMuiGuestMemory
	{
		inputEvent = default;
		if (!MuiIntuiMessageCodec.TryReadPointerRecord(ref memory, intuiMessage,
			out var source)) return false;

		InputEventClass eventClass;
		switch (source.Class)
		{
			case RawKeyClass:
				eventClass = InputEventClass.RawKey;
				break;
			case DiskInsertedClass:
				eventClass = InputEventClass.DiskInserted;
				break;
			case DiskRemovedClass:
				eventClass = InputEventClass.DiskRemoved;
				break;
			default:
				return false;
		}

		var eventAddress = APTR.Null;
		// The IntuiMessage IAddress indirection carries the dead-key address
		// only for RAWKEY. Disk events leave InputEvent's position union zeroed.
		if (source.Class == RawKeyClass && source.IAddress != 0)
		{
			var addressRecord = APTR.FromPointer(source.IAddress);
			if (!MuiWindowInputEventAddressCodec.TryRead(ref memory,
				addressRecord, out var address)) return false;
			eventAddress = address.Address;
		}

		inputEvent.NextEvent = APTR.Null;
		inputEvent.Class = eventClass;
		inputEvent.SubClass = InputEventSubClass.Compatible;
		inputEvent.Code = source.Code;
		inputEvent.Qualifier = (InputEventQualifier)source.Qualifier;
		inputEvent.Position = unchecked((int)eventAddress.Raw);
		if (source.TimestampValid != 0)
		{
			inputEvent.TimeStamp.Seconds = source.Seconds;
			inputEvent.TimeStamp.Microseconds = source.Micros;
		}
		return true;
	}

	internal static uint Dispatch(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR application, APTR task,
		uint receivedSignals, bool newInput)
	{
		if (publicObjects.IsNull || ownerRoot.IsNull || application.IsNull ||
			task.IsNull) return 0;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativePublicObjectRegistryCodec.TryRead(ref memory,
			publicObjects, out var registry)) return 0;
		var current = registry.Head;
		var packet = APTR.Null;
		var dispatched = 0u;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, current,
				out var binding) || binding.Signature !=
				MuiNativePublicObjectBinding.Magic || binding.OwnerRoot != ownerRoot)
				break;
			var next = binding.Next;
			if (binding.DisposeState == MuiNativePublicObjectBinding.StateLive)
			{
				if (!TryReadLiveWindow(ref memory, binding, application, task,
					out var sidecar, out var userPort,
					out var signalBit)) break;
				if (binding.Parent == application &&
					sidecar.NativeWindow.IsNotNull && userPort.IsNotNull &&
					(!newInput ||
					 (receivedSignals & (1u << (int)signalBit)) != 0))
				{
					if (packet.IsNull)
					{
						packet = platform.Allocate(
							MuiCommonHandleEventMessage.Size,
							MuiHeadlessLayout.AllocationFlags);
						if (packet.IsNull) break;
					}
					for (var index = 0u; index < MaximumMessagesPerWindow;
						index++)
					{
						if (!IsStillOpen(ref memory, current, ownerRoot,
							application, task, binding.Object,
							sidecar.NativeWindow, userPort, signalBit)) break;
						var intuiMessage = Exec.GetMsg(userPort);
						if (intuiMessage.IsNull) break;
						var hasHandleEvent = TryPrepareHandleEvent(ref memory,
							intuiMessage, packet, out var eventClass);
						if (hasHandleEvent && eventClass == RefreshWindowClass)
						{
							if (TryDispatchApplicationCheckRefresh(ref platform,
								application, packet)) dispatched++;
							Exec.ReplyMsg(intuiMessage);
							continue;
						}
						var hasSleepState =
							MuiNativeApplicationSleep.TryReadWindowSleepDepth(
								ref memory, binding.Sidecar, out var sleepDepth);
						if (!hasSleepState || sleepDepth != 0)
						{
							// A sleeping Window consumes no MUI input. Restore any
							// Intuition size change before replying, so its native box
							// remains at the geometry captured when sleep began.
							var geometryRestored = hasSleepState &&
								MuiNativeApplicationSleep.
									TryHandleSleepingWindowMessage(ref platform,
										binding.Sidecar, sidecar.NativeWindow,
										intuiMessage);
							Exec.ReplyMsg(intuiMessage);
							if (!hasSleepState || sleepDepth == 0 ||
								!geometryRestored) break;
							continue;
						}
						if (hasHandleEvent)
						{
							var publishedInputEvent = TryPublishInputEvent(
								ref platform, binding.Sidecar, intuiMessage,
								out var inputEventStorage, out var previousInputEvent);
							if (publishedInputEvent)
								MuiNativeAreaControlCharInput.TryPromote(ref platform,
									publicObjects, ownerRoot, binding.Sidecar,
									intuiMessage, inputEventStorage, packet);
							var shouldDispatch = !publishedInputEvent ||
								IsStillOpen(ref memory, current, ownerRoot, application,
									task, binding.Object, sidecar.NativeWindow,
									userPort, signalBit);
							var mouseObjectChanged = false;
							var mouseObject = APTR.Null;
							if (shouldDispatch)
							{
								mouseObjectChanged = PublishWindowEventAttribute(
									ref platform, publicObjects,
									ownerRoot, binding.Object, binding.Sidecar,
									intuiMessage, eventClass, out mouseObject);
								shouldDispatch = IsStillOpen(ref memory, current,
									ownerRoot, application, task, binding.Object,
									sidecar.NativeWindow, userPort, signalBit);
							}
							if (shouldDispatch && mouseObjectChanged)
							{
								MuiNativeWindowEventHandlerQueue.
									DispatchMouseObjectChange(ref platform,
										publicObjects, ownerRoot, binding.Sidecar,
										mouseObject, intuiMessage);
								shouldDispatch = IsStillOpen(ref memory, current,
									ownerRoot, application, task, binding.Object,
									sidecar.NativeWindow, userPort, signalBit);
							}
							if (shouldDispatch)
							{
								var helpDisplayed = MuiNativeApplicationHelp.
									TryHandleHelpKey(ref platform, publicObjects,
										ownerRoot, application, binding.Object,
										binding.Sidecar, intuiMessage);
								if (helpDisplayed)
								{
									if (IsStillOpen(ref memory, current, ownerRoot,
										application, task, binding.Object,
										sidecar.NativeWindow, userPort, signalBit))
										dispatched++;
								}
								else
								{
									var eaten = MuiNativeWindowEventHandlerQueue.
										Dispatch(ref platform, publicObjects,
											ownerRoot, binding.Sidecar, intuiMessage,
											packet);
									if (!eaten)
										MuiNativeBoopsiDispatch.DoMethod(ref platform,
											binding.Object, packet);
									dispatched++;
								}
							}
							if (publishedInputEvent)
								RetireInputEvent(ref platform, binding.Sidecar,
									inputEventStorage, previousInputEvent);
						}
						// Even an unrecognized or malformed event must be returned to
						// Intuition after GetMsg transfers ownership to this task.
						Exec.ReplyMsg(intuiMessage);
					}
				}
			}
			else if (binding.DisposeState !=
				MuiNativePublicObjectBinding.StateNativeDisposed &&
				binding.DisposeState !=
				MuiNativePublicObjectBinding.StateLeaseReleased) break;
			current = next;
		}
		if (current.IsNotNull) dispatched = 0;
		if (packet.IsNotNull)
		{
			platform.Clear(packet, MuiCommonHandleEventMessage.Size);
			platform.Free(packet, MuiCommonHandleEventMessage.Size);
		}
		return dispatched;
	}

	private static bool PublishWindowEventAttribute(
		ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR windowObject, APTR sidecarAddress,
		APTR intuiMessage, uint eventClass, out APTR mouseObject)
	{
		mouseObject = APTR.Null;
		if (eventClass == GeometryChangeClass)
		{
			if (PublishWindowGeometry(ref platform, publicObjects, ownerRoot,
				windowObject, sidecarAddress, intuiMessage))
				MuiNativeWindowContentLayoutCore.TryRelayoutRootAfterResize(
					ref platform, publicObjects, ownerRoot, windowObject,
					sidecarAddress);
		}
		// ChangeWindow can move or resize the content under a stationary
		// pointer, so refresh after publishing the new live geometry. Use the
		// current pointer fields from the typed Window record for this case;
		// queued IntuiMessage coordinates describe an earlier event sample.
		if (ShouldRefreshMouseObject(eventClass))
		{
			if (eventClass == GeometryChangeClass)
				return PublishWindowMouseObjectFromCurrentWindow(ref platform,
					publicObjects, ownerRoot, windowObject, sidecarAddress,
					out mouseObject);
			return PublishWindowMouseObject(ref platform, publicObjects,
				ownerRoot, windowObject, sidecarAddress, intuiMessage,
				out mouseObject);
		}
		if (eventClass == CloseWindowClass)
		{
			// MorphOS reports a close gadget through
			// MUIA_Window_CloseRequest; it does not close the native Window
			// automatically. Use the live object's named attribute path so
			// subscribers receive the normal notification.
			MuiNativePublicObjectCore.SetAttribute(ref platform, publicObjects,
				ownerRoot, windowObject, CloseRequestAttribute, 1);
			return false;
		}
		if (!MuiWindowPublicCore.TryGetActivationValue(eventClass,
			out var activationValue)) return false;
		var hasCurrentValue = MuiNativePublicObjectCore.GetAttribute(
			ref platform, publicObjects, ownerRoot, windowObject,
			ActivateAttribute, out var currentValue);
		if ((hasCurrentValue && currentValue == activationValue) ||
			(!hasCurrentValue && activationValue == 0)) return false;
		MuiNativePublicObjectCore.SetAttribute(ref platform, publicObjects,
			ownerRoot, windowObject, ActivateAttribute, activationValue);
		return false;
	}

	internal static bool ShouldRefreshMouseObject(uint eventClass) =>
		eventClass == MouseMoveClass || eventClass == MouseButtonsClass ||
		eventClass == GadgetDownClass || eventClass == GadgetUpClass ||
		eventClass == GeometryChangeClass;

	// MUIA_Window_MouseObject is the deepest live Area beneath the current
	// pointer. MorphOS MUI 4+ emits notifications whenever this value changes;
	// NeedsMouseObject is retained as an obsolete initializer attribute and is
	// not a gate on current behavior. The hit-test uses public Area attributes.
	private static bool PublishWindowMouseObject(
		ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR windowObject, APTR sidecarAddress,
		APTR intuiMessage, out APTR mouseObject)
	{
		mouseObject = APTR.Null;
		var memory = default(MuiNativeClassMemory);
		if (!MuiIntuiMessageCodec.TryReadPointerRecord(ref memory, intuiMessage,
			out var message) || message.Class == 0) return false;
		return PublishWindowMouseObject(ref platform, publicObjects, ownerRoot,
			windowObject, sidecarAddress,
			MuiNativePointerPosition.FromMessage(message), out mouseObject);
	}

	private static bool PublishWindowMouseObjectFromCurrentWindow(
		ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR windowObject, APTR sidecarAddress,
		out APTR mouseObject)
	{
		mouseObject = APTR.Null;
		var memory = default(MuiNativeClassMemory);
		if (!TryReadWindowGeometry(ref memory, publicObjects, ownerRoot,
			windowObject, sidecarAddress, out var nativeWindow, out _) ||
			!memory.IsMapped(nativeWindow, Window.Size)) return false;
		var window = IntuitionScreenWindowGuestCodec.ReadWindow(ref memory,
			nativeWindow);
		return PublishWindowMouseObject(ref platform, publicObjects, ownerRoot,
			windowObject, sidecarAddress,
			MuiNativePointerPosition.FromWindow(window), out mouseObject);
	}

	private static bool PublishWindowMouseObject(
		ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR windowObject, APTR sidecarAddress,
		MuiNativePointerPosition pointer, out APTR mouseObject)
	{
		mouseObject = APTR.Null;
		if (!MuiNativeWindowPointerHitTest.TryResolve(ref platform,
			publicObjects, ownerRoot, windowObject, sidecarAddress, pointer,
			out var resolvedMouseObject)) return false;
		var memory = default(MuiNativeClassMemory);
		var hasPrevious = MuiNativeObjectStateCore.TryGetAttribute(ref memory,
			sidecarAddress, MuiWindowPublicCore.MouseObject, out var previous);
		if ((hasPrevious && previous == resolvedMouseObject.Raw) ||
			(!hasPrevious && resolvedMouseObject.IsNull)) return false;
		if (!MuiNativePublicObjectCore.SetAttribute(ref platform, publicObjects,
			ownerRoot, windowObject, MuiWindowPublicCore.MouseObject,
			resolvedMouseObject.Raw)) return false;
		mouseObject = resolvedMouseObject;
		return true;
	}

	// IDCMP_CHANGEWINDOW/CWCODE_MOVESIZE reports that Intuition has committed a
	// new geometry. Snapshot the named Window fields, update every changed MUI
	// attribute before invoking subscribers, then publish the changes through
	// the ordinary notification path. All pointer and geometry state stays in
	// named structs; no Window field offsets are used here.
	private static bool PublishWindowGeometry(
		ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR windowObject, APTR sidecarAddress,
		APTR intuiMessage)
	{
		var memory = default(MuiNativeClassMemory);
		if (!MuiIntuiMessageCodec.TryReadPointerRecord(ref memory, intuiMessage,
			out var message) || message.Class != GeometryChangeClass ||
			message.Code != (ushort)IDCMPCode.WindowMoveSize ||
			!TryReadWindowGeometry(ref memory, publicObjects, ownerRoot,
				windowObject, sidecarAddress, out var nativeWindow,
				out var geometry))
			return false;

		var left = unchecked((uint)geometry.LeftEdge);
		var top = unchecked((uint)geometry.TopEdge);
		var width = unchecked((uint)geometry.Width);
		var height = unchecked((uint)geometry.Height);
		var leftChanged = !MuiNativeObjectStateCore.TryGetAttribute(ref memory,
			sidecarAddress, MuiWindowPublicCore.LeftEdge, out var oldLeft) ||
			oldLeft != left;
		var topChanged = !MuiNativeObjectStateCore.TryGetAttribute(ref memory,
			sidecarAddress, MuiWindowPublicCore.TopEdge, out var oldTop) ||
			oldTop != top;
		var widthChanged = !MuiNativeObjectStateCore.TryGetAttribute(ref memory,
			sidecarAddress, MuiWindowPublicCore.Width, out var oldWidth) ||
			oldWidth != width;
		var heightChanged = !MuiNativeObjectStateCore.TryGetAttribute(ref memory,
			sidecarAddress, MuiWindowPublicCore.Height, out var oldHeight) ||
			oldHeight != height;
		if (!leftChanged && !topChanged && !widthChanged && !heightChanged)
			return false;
		var resized = widthChanged || heightChanged;

		// Make the complete new sample visible to notification callbacks before
		// the first callback can query a related geometry attribute.
		if ((leftChanged && !MuiNativePublicObjectCore.SetAttribute(ref platform,
			publicObjects, ownerRoot, windowObject, MuiWindowPublicCore.LeftEdge,
			left, false)) ||
			(topChanged && !MuiNativePublicObjectCore.SetAttribute(ref platform,
				publicObjects, ownerRoot, windowObject, MuiWindowPublicCore.TopEdge,
				top, false)) ||
			(widthChanged && !MuiNativePublicObjectCore.SetAttribute(ref platform,
				publicObjects, ownerRoot, windowObject, MuiWindowPublicCore.Width,
				width, false)) ||
			(heightChanged && !MuiNativePublicObjectCore.SetAttribute(ref platform,
				publicObjects, ownerRoot, windowObject, MuiWindowPublicCore.Height,
			height, false))) return false;

		if (leftChanged && IsWindowGeometryCurrent(ref platform, publicObjects,
			ownerRoot, windowObject, sidecarAddress, nativeWindow, geometry))
			MuiNativePublicObjectCore.SetAttribute(ref platform, publicObjects,
				ownerRoot, windowObject, MuiWindowPublicCore.LeftEdge, left);
		if (topChanged && IsWindowGeometryCurrent(ref platform, publicObjects,
			ownerRoot, windowObject, sidecarAddress, nativeWindow, geometry))
			MuiNativePublicObjectCore.SetAttribute(ref platform, publicObjects,
				ownerRoot, windowObject, MuiWindowPublicCore.TopEdge, top);
		if (widthChanged && IsWindowGeometryCurrent(ref platform, publicObjects,
			ownerRoot, windowObject, sidecarAddress, nativeWindow, geometry))
			MuiNativePublicObjectCore.SetAttribute(ref platform, publicObjects,
				ownerRoot, windowObject, MuiWindowPublicCore.Width, width);
		if (heightChanged && IsWindowGeometryCurrent(ref platform, publicObjects,
			ownerRoot, windowObject, sidecarAddress, nativeWindow, geometry))
			MuiNativePublicObjectCore.SetAttribute(ref platform, publicObjects,
				ownerRoot, windowObject, MuiWindowPublicCore.Height, height);
		return resized;
	}

	private static bool IsWindowGeometryCurrent(
		ref MuiNativeClassPlatform platform, APTR publicObjects, APTR ownerRoot,
		APTR windowObject, APTR sidecarAddress, APTR expectedNativeWindow,
		MuiWindowPublicCore.MuiWindowGeometry expectedGeometry)
	{
		var memory = default(MuiNativeClassMemory);
		return TryReadWindowGeometry(ref memory, publicObjects, ownerRoot,
			windowObject, sidecarAddress, out var nativeWindow,
			out var geometry) && nativeWindow == expectedNativeWindow &&
			geometry.LeftEdge == expectedGeometry.LeftEdge &&
			geometry.TopEdge == expectedGeometry.TopEdge &&
			geometry.Width == expectedGeometry.Width &&
			geometry.Height == expectedGeometry.Height;
	}

	private static bool TryReadWindowGeometry<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR windowObject,
		APTR sidecarAddress, out APTR nativeWindow,
		out MuiWindowPublicCore.MuiWindowGeometry geometry)
		where TMemory : struct, IMuiGuestMemory
	{
		nativeWindow = APTR.Null;
		geometry = default;
		if (!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
			ownerRoot, windowObject, out var binding) ||
			binding.Sidecar != sidecarAddress ||
			!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
				out var sidecar) || sidecar.NativeWindow.IsNull ||
			!memory.IsMapped(sidecar.NativeWindow, Window.Size)) return false;
		nativeWindow = sidecar.NativeWindow;
		var window = IntuitionScreenWindowGuestCodec.ReadWindow(ref memory,
			nativeWindow);
		geometry.Height = window.Height;
		geometry.Width = window.Width;
		geometry.LeftEdge = window.LeftEdge;
		geometry.TopEdge = window.TopEdge;
		return true;
	}

	private static bool TryPublishInputEvent(
		ref MuiNativeClassPlatform platform, APTR sidecarAddress,
		APTR intuiMessage, out APTR inputEventStorage,
		out uint previousInputEvent)
	{
		inputEventStorage = APTR.Null;
		previousInputEvent = 0;
		var memory = default(MuiNativeClassMemory);
		if (!TryTranslateInputEvent(ref memory, intuiMessage,
			out var inputEvent)) return false;
		MuiNativeObjectStateCore.TryGetAttribute(ref memory, sidecarAddress,
			InputEventAttribute, out previousInputEvent);
		inputEventStorage = platform.Allocate(InputEvent.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (inputEventStorage.IsNull) return false;
		if (!MuiWindowInputEventCodec.WriteRecord(ref platform,
			inputEventStorage, inputEvent) ||
			!MuiNativeObjectStateCore.SetAttribute(ref platform, sidecarAddress,
				InputEventAttribute, inputEventStorage.Raw))
		{
			platform.Free(inputEventStorage, InputEvent.Size);
			inputEventStorage = APTR.Null;
			return false;
		}
		return true;
	}

	private static void RetireInputEvent(
		ref MuiNativeClassPlatform platform, APTR sidecarAddress,
		APTR inputEventStorage, uint previousInputEvent)
	{
		if (inputEventStorage.IsNull) return;
		if (MuiNativeObjectStateCore.SetAttribute(ref platform, sidecarAddress,
			InputEventAttribute, previousInputEvent, false))
		{
			platform.Free(inputEventStorage, InputEvent.Size);
			return;
		}

		// If the callback destroyed the Window or changed the getter-only value,
		// no live attribute points to this scratch record. If the live value is
		// still the published address, retain its backing allocation rather than
		// leave a dangling MUIA_Window_InputEvent pointer.
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeObjectStateCore.TryGetAttribute(ref memory, sidecarAddress,
			InputEventAttribute, out var liveValue) ||
			liveValue != inputEventStorage.Raw)
			platform.Free(inputEventStorage, InputEvent.Size);
	}

	private static bool TryReadLiveWindow(ref MuiNativeClassMemory memory,
		MuiNativePublicObjectBinding binding, APTR application, APTR task,
		out MuiNativeMuiObjectRecord sidecar, out APTR userPort,
		out uint signalBit)
	{
		sidecar = default;
		userPort = APTR.Null;
		signalBit = 0;
		if (binding.Object.IsNull || binding.Sidecar.IsNull ||
			!MuiNativeMuiObjectCodec.TryRead(ref memory, binding.Sidecar,
				out sidecar) || sidecar.Object != binding.Object ||
			sidecar.Class != binding.Class || sidecar.OwnerRoot !=
			binding.OwnerRoot || sidecar.Parent != binding.Parent ||
			sidecar.LifecycleState != MuiNativeMuiObjectRecord.StateLive ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) == 0 ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectDisposing) != 0)
			return false;
		if (binding.Parent != application || sidecar.NativeWindow.IsNull)
			return true;
		if (!memory.IsMapped(sidecar.NativeWindow, Window.Size)) return false;
		var window = IntuitionScreenWindowGuestCodec.ReadWindow(ref memory,
			sidecar.NativeWindow);
		userPort = window.UserPort;
		if (userPort.IsNull) return true;
		if (!ExecMsgPortCodec.IsMapped(ref memory, userPort)) return false;
		var port = ExecMsgPortCodec.Read(ref memory, userPort);
		if (port.SignalTask != task || port.SignalBit >= 32)
		{
			userPort = APTR.Null;
			return true;
		}
		signalBit = port.SignalBit;
		return true;
	}

	private static bool IsStillOpen(ref MuiNativeClassMemory memory,
		APTR bindingAddress, APTR ownerRoot, APTR application, APTR task,
		APTR windowObject, APTR nativeWindow, APTR userPort,
		uint signalBit)
	{
		if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory,
			bindingAddress, out var binding) || binding.Signature !=
			MuiNativePublicObjectBinding.Magic || binding.OwnerRoot != ownerRoot ||
			binding.DisposeState != MuiNativePublicObjectBinding.StateLive ||
			binding.Object != windowObject || binding.Parent != application ||
			binding.Sidecar.IsNull ||
			!MuiNativeMuiObjectCodec.TryRead(ref memory, binding.Sidecar,
				out var sidecar) || sidecar.Object != windowObject ||
			sidecar.Class != binding.Class || sidecar.OwnerRoot != ownerRoot ||
			sidecar.Parent != application || sidecar.NativeWindow != nativeWindow ||
			sidecar.LifecycleState != MuiNativeMuiObjectRecord.StateLive ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) == 0 ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectDisposing) != 0 ||
			!memory.IsMapped(nativeWindow, Window.Size)) return false;
		var window = IntuitionScreenWindowGuestCodec.ReadWindow(ref memory,
			nativeWindow);
		if (window.UserPort != userPort ||
			!ExecMsgPortCodec.IsMapped(ref memory, userPort)) return false;
		var port = ExecMsgPortCodec.Read(ref memory, userPort);
		return port.SignalTask == task && port.SignalBit == signalBit;
	}
}
