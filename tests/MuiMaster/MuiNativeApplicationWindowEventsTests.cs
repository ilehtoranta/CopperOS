using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeApplicationWindowEventsTests
{
	[Fact]
	public void WindowContentRectangleUsesNamedClientAreaGeometry()
	{
		var window = new Window
		{
			LeftEdge = 17,
			TopEdge = 23,
			Width = 640,
			Height = 240,
			BorderLeft = 4,
			BorderTop = 12,
			BorderRight = 6,
			BorderBottom = 5,
		};

		Assert.True(MuiNativeWindowContentLayoutCore.TryCreate(window,
			out var rectangle));
		Assert.Equal(4, rectangle.Left);
		Assert.Equal(12, rectangle.Top);
		Assert.Equal(630, rectangle.Width);
		Assert.Equal(223, rectangle.Height);
	}

	[Fact]
	public void WindowContentRectangleRejectsInvalidBordersAndEmptyClientArea()
	{
		var negativeBorder = new Window
		{
			Width = 100,
			Height = 80,
			BorderLeft = -1,
		};
		var emptyArea = new Window
		{
			Width = 10,
			Height = 8,
			BorderLeft = 5,
			BorderRight = 5,
		};

		Assert.False(MuiNativeWindowContentLayoutCore.TryCreate(negativeBorder,
			out _));
		Assert.False(MuiNativeWindowContentLayoutCore.TryCreate(emptyArea,
			out _));
	}

	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void NativeWindowEventUsesNamedHandleEventPacket()
	{
		var platform = CreatePlatform();
		var intuiMessage = APTR.FromPointer(0x1200);
		var packetAddress = APTR.FromPointer(0x1300);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform,
			intuiMessage, MuiIntuiMessageCodec.RawKeyClass, 0x0041, 0x0008,
			0x11223344, -12, 23));

		Assert.True(MuiNativeApplicationWindowEvents.TryPrepareHandleEvent(
			ref platform, intuiMessage, packetAddress, out var eventClass));
		Assert.Equal(MuiIntuiMessageCodec.RawKeyClass, eventClass);
		Assert.True(MuiCommonHandleEventMessageCodec.TryRead(ref platform,
			packetAddress, out var packet));
		Assert.Equal(MuiCommonControlPacketCore.HandleEvent, packet.MethodId);
		Assert.Equal(intuiMessage.Raw, packet.InputMessage);
		Assert.Equal(-1, packet.MuiKey);
		Assert.Equal(0u, packet.EventHandlerNode);
	}

	[Fact]
	public void NativeKnownRawKeysMapToMuiNavigationAndHelpKeys()
	{
		var helpMessage = default(MuiIntuiPointerMessage);
		helpMessage.Class = MuiIntuiMessageCodec.RawKeyClass;
		helpMessage.Code = 0x005F;
		Assert.Equal(MuiHelpTriggerInput.HelpKey,
			MuiNativeApplicationWindowEvents.KnownMuiKeyForRawKey(
				helpMessage));
		var unknownMessage = default(MuiIntuiPointerMessage);
		unknownMessage.Class = MuiIntuiMessageCodec.RawKeyClass;
		unknownMessage.Code = 0x00DF;
		Assert.Equal(-1,
			MuiNativeApplicationWindowEvents.KnownMuiKeyForRawKey(
				unknownMessage));
		var nonRawKeyMessage = default(MuiIntuiPointerMessage);
		nonRawKeyMessage.Class = (uint)IDCMPFlags.MouseMove;
		nonRawKeyMessage.Code = 0x005F;
		Assert.Equal(-1,
			MuiNativeApplicationWindowEvents.KnownMuiKeyForRawKey(
				nonRawKeyMessage));

		var platform = CreatePlatform();
		var intuiMessage = APTR.FromPointer(0x1200);
		var packetAddress = APTR.FromPointer(0x1300);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform,
			intuiMessage, MuiIntuiMessageCodec.RawKeyClass, 0x005F, 0,
			0, 0, 0));
		Assert.True(MuiNativeApplicationWindowEvents.TryPrepareHandleEvent(
			ref platform, intuiMessage, packetAddress, out _));
		Assert.True(MuiCommonHandleEventMessageCodec.TryRead(ref platform,
			packetAddress, out var packet));
		Assert.Equal(MuiHelpTriggerInput.HelpKey, packet.MuiKey);
	}

	[Theory]
	[InlineData((ushort)MuiNativeRawKeyCode.Up, (ushort)0,
		(int)MuiNativeHandleEventKey.Up)]
	[InlineData((ushort)MuiNativeRawKeyCode.Down, (ushort)0,
		(int)MuiNativeHandleEventKey.Down)]
	[InlineData((ushort)MuiNativeRawKeyCode.PageUp, (ushort)0,
		(int)MuiNativeHandleEventKey.PageUp)]
	[InlineData((ushort)MuiNativeRawKeyCode.PageDown, (ushort)0,
		(int)MuiNativeHandleEventKey.PageDown)]
	[InlineData((ushort)MuiNativeRawKeyCode.Left, (ushort)0,
		(int)MuiNativeHandleEventKey.Left)]
	[InlineData((ushort)MuiNativeRawKeyCode.Right, (ushort)0,
		(int)MuiNativeHandleEventKey.Right)]
	[InlineData((ushort)MuiNativeRawKeyCode.Left,
		(ushort)InputEventQualifier.Control,
		(int)MuiNativeHandleEventKey.WordLeft)]
	[InlineData((ushort)MuiNativeRawKeyCode.Right,
		(ushort)InputEventQualifier.Control,
		(int)MuiNativeHandleEventKey.WordRight)]
	[InlineData((ushort)MuiNativeRawKeyCode.Home, (ushort)0,
		(int)MuiNativeHandleEventKey.LineStart)]
	[InlineData((ushort)MuiNativeRawKeyCode.End, (ushort)0,
		(int)MuiNativeHandleEventKey.LineEnd)]
	[InlineData((ushort)MuiNativeRawKeyCode.Home,
		(ushort)InputEventQualifier.Control,
		(int)MuiNativeHandleEventKey.Top)]
	[InlineData((ushort)MuiNativeRawKeyCode.End,
		(ushort)InputEventQualifier.Control,
		(int)MuiNativeHandleEventKey.Bottom)]
	public void NativeRawNavigationKeysBecomeHandleEventKeys(ushort code,
		ushort qualifier, int expectedMuiKey)
	{
		var platform = CreatePlatform();
		var intuiMessage = APTR.FromPointer(0x1200);
		var packetAddress = APTR.FromPointer(0x1300);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform,
			intuiMessage, MuiIntuiMessageCodec.RawKeyClass, code, qualifier,
			0, 0, 0));

		Assert.True(MuiNativeApplicationWindowEvents.TryPrepareHandleEvent(
			ref platform, intuiMessage, packetAddress, out _));
		Assert.True(MuiCommonHandleEventMessageCodec.TryRead(ref platform,
			packetAddress, out var packet));
		Assert.Equal(expectedMuiKey, packet.MuiKey);
	}

	[Theory]
	[InlineData((uint)'s', (byte)'S', (ushort)0, true)]
	[InlineData((uint)'S', (byte)'s', (ushort)0, false)]
	[InlineData((uint)'S', (byte)'S',
		(ushort)InputEventQualifier.LeftShift, true)]
	[InlineData((uint)'S', (byte)'S',
		(ushort)InputEventQualifier.RightShift, true)]
	[InlineData((uint)'S', (byte)'S',
		(ushort)InputEventQualifier.Control, false)]
	[InlineData((uint)'x', (byte)'y', (ushort)0, false)]
	[InlineData(0u, (byte)'x', (ushort)0, false)]
	[InlineData(256u, (byte)'x', (ushort)0, false)]
	[InlineData((uint)'x', (byte)0, (ushort)0, false)]
	public void NativeControlCharPolicyMatchesTranslatedCharacters(
		uint controlChar, byte translated, ushort qualifier, bool expected)
	{
		Assert.Equal(expected, MuiNativeControlCharKeyPolicy.Matches(
			controlChar, translated, qualifier));
	}

	[Fact]
	public void NativeControlCharPromotesMappedKeyForTheActiveObject()
	{
		var platform = CreateNativeControlCharPlatform((uint)'S',
			(ushort)InputEventQualifier.LeftShift, out var publicObjects,
			out var ownerRoot, out var windowSidecar, out var intuiMessage,
			out var inputEvent, out var packet, out _);
		platform.RawKeyMapCharacter = (byte)'S';

		Assert.True(MuiNativeAreaControlCharInput.TryPromote(ref platform,
			publicObjects, ownerRoot, windowSidecar, intuiMessage, inputEvent,
			packet));
		Assert.Equal(1u, platform.RawKeyMapCalls);
		Assert.Equal(inputEvent, platform.LastRawKeyMapInputEvent);
		Assert.True(MuiCommonHandleEventMessageCodec.TryRead(ref platform,
			packet, out var handleEvent));
		Assert.Equal((int)MuiNativeHandleEventKey.Press, handleEvent.MuiKey);
	}

	[Fact]
	public void NativeControlCharDoesNotPromoteRawKeyRelease()
	{
		var platform = CreateNativeControlCharPlatform((uint)'S',
			(ushort)InputEventQualifier.LeftShift, out var publicObjects,
			out var ownerRoot, out var windowSidecar, out var intuiMessage,
			out var inputEvent, out var packet, out _);
		var releaseCode = (ushort)((ushort)MuiNativeRawKeyCode.S |
			MuiIntuiMessageCodec.RawKeyUpPrefix);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform,
			intuiMessage, MuiIntuiMessageCodec.RawKeyClass, releaseCode,
			(ushort)InputEventQualifier.LeftShift, 0, 0, 0));
		Assert.True(MuiNativeApplicationWindowEvents.TryPrepareHandleEvent(
			ref platform, intuiMessage, packet, out _));
		Assert.True(MuiWindowInputEventRecordCodec.WriteStructural(ref platform,
			inputEvent, new MuiWindowInputEventRecord
			{
				Class = (byte)InputEventClass.RawKey,
				SubClass = (byte)InputEventSubClass.Compatible,
				Code = releaseCode,
				Qualifier = (ushort)InputEventQualifier.LeftShift,
			}));
		platform.RawKeyMapCharacter = (byte)'S';

		Assert.False(MuiNativeAreaControlCharInput.TryPromote(ref platform,
			publicObjects, ownerRoot, windowSidecar, intuiMessage, inputEvent,
			packet));
		Assert.Equal(0u, platform.RawKeyMapCalls);
		Assert.True(MuiCommonHandleEventMessageCodec.TryRead(ref platform,
			packet, out var handleEvent));
		Assert.Equal((int)MuiNativeHandleEventKey.None, handleEvent.MuiKey);
	}

	[Fact]
	public void NativeUppercaseControlCharRequiresPhysicalShift()
	{
		var platform = CreateNativeControlCharPlatform((uint)'S',
			(ushort)InputEventQualifier.Control, out var publicObjects,
			out var ownerRoot, out var windowSidecar, out var intuiMessage,
			out var inputEvent, out var packet, out _);
		platform.RawKeyMapCharacter = (byte)'S';

		Assert.False(MuiNativeAreaControlCharInput.TryPromote(ref platform,
			publicObjects, ownerRoot, windowSidecar, intuiMessage, inputEvent,
			packet));
		Assert.Equal(1u, platform.RawKeyMapCalls);
		Assert.True(MuiCommonHandleEventMessageCodec.TryRead(ref platform,
			packet, out var handleEvent));
		Assert.Equal((int)MuiNativeHandleEventKey.None, handleEvent.MuiKey);
	}

	[Fact]
	public void NativeControlCharIgnoresAnUnregisteredActiveObject()
	{
		var platform = CreateNativeControlCharPlatform((uint)'s', 0,
			out var publicObjects, out var ownerRoot, out var windowSidecar,
			out var intuiMessage, out var inputEvent, out var packet,
			out var activeObjectAttribute);
		platform.RawKeyMapCharacter = (byte)'s';
		Assert.True(MuiNativeObjectAttributeCodec.TryRead(ref platform,
			activeObjectAttribute, out var activeObject));
		activeObject.Value = 0x1FF0;
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref platform,
			activeObjectAttribute, activeObject));

		Assert.False(MuiNativeAreaControlCharInput.TryPromote(ref platform,
			publicObjects, ownerRoot, windowSidecar, intuiMessage, inputEvent,
			packet));
		Assert.Equal(0u, platform.RawKeyMapCalls);
	}

	[Fact]
	public void NativeCloseWindowEventClassComesFromNamedIntuiMessage()
	{
		var platform = CreatePlatform();
		var intuiMessage = APTR.FromPointer(0x1200);
		var packetAddress = APTR.FromPointer(0x1300);
		var closeWindowClass = (uint)IDCMPFlags.CloseWindow;
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform,
			intuiMessage, closeWindowClass, 0, 0, 0, 0, 0));

		Assert.True(MuiNativeApplicationWindowEvents.TryPrepareHandleEvent(
			ref platform, intuiMessage, packetAddress, out var eventClass));
		Assert.Equal(closeWindowClass, eventClass);
	}

	[Fact]
	public void RefreshWindowDispatchesNamedApplicationCheckRefreshMethod()
	{
		var platform = CreatePlatform();
		var application = APTR.FromPointer(0x1800);
		var packet = APTR.FromPointer(0x1300);

		Assert.True(MuiNativeApplicationWindowEvents.
			TryDispatchApplicationCheckRefresh(ref platform, application,
				packet));

		Assert.Equal(1u, platform.DispatchCount);
		Assert.Equal(application, platform.LastDispatchObject);
		Assert.Equal(MuiApplicationDispatcher.ApplicationCheckRefreshMethod,
			platform.LastDispatchMethod);
		Assert.Equal(0u, platform.AllocationCount);
		Assert.Equal(0u, platform.FreeCount);
	}

	[Fact]
	public void RefreshWindowRejectsUnmappedPacketWithoutDispatch()
	{
		var platform = CreatePlatform();

		Assert.False(MuiNativeApplicationWindowEvents.
			TryDispatchApplicationCheckRefresh(ref platform,
				APTR.FromPointer(0x1800), APTR.FromPointer(0x22000)));

		Assert.Equal(0u, platform.DispatchCount);
		Assert.Equal(0u, platform.AllocationCount);
		Assert.Equal(0u, platform.FreeCount);
	}

	[Theory]
	[InlineData((uint)IDCMPFlags.MenuPick, (ushort)0x1234)]
	[InlineData((uint)IDCMPFlags.MenuHelp, (ushort)0xFFFF)]
	public void NativeMenuEventsUseTheCommonHandleEventPath(uint messageClass,
		ushort menuCode)
	{
		var platform = CreatePlatform();
		var intuiMessage = APTR.FromPointer(0x1200);
		var packetAddress = APTR.FromPointer(0x1300);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform,
			intuiMessage, messageClass, menuCode, 0, 0, 0, 0));

		Assert.True(MuiNativeApplicationWindowEvents.TryPrepareHandleEvent(
			ref platform, intuiMessage, packetAddress, out var eventClass));
		Assert.Equal(messageClass, eventClass);
		Assert.True(MuiCommonHandleEventMessageCodec.TryRead(ref platform,
			packetAddress, out var packet));
		Assert.Equal(MuiCommonControlPacketCore.HandleEvent, packet.MethodId);
		Assert.Equal(intuiMessage.Raw, packet.InputMessage);
		Assert.Equal(-1, packet.MuiKey);
		Assert.True(MuiIntuiMessageCodec.TryReadPointerRecord(ref platform,
			APTR.FromPointer(packet.InputMessage), out var forwardedMessage));
		Assert.Equal(messageClass, forwardedMessage.Class);
		Assert.Equal(menuCode, forwardedMessage.Code);
	}

	[Theory]
	[InlineData((uint)IDCMPFlags.ActiveWindow, true, 1u)]
	[InlineData((uint)IDCMPFlags.InactiveWindow, true, 0u)]
	[InlineData((uint)IDCMPFlags.RawKey, false, 0u)]
	public void WindowActivationStateUsesNamedIntuitionClasses(uint eventClass,
		bool expectedResult, uint expectedValue)
	{
		var result = MuiWindowPublicCore.TryGetActivationValue(eventClass,
			out var activationValue);

		Assert.Equal(expectedResult, result);
		Assert.Equal(expectedValue, activationValue);
	}

	[Theory]
	[InlineData((uint)IDCMPFlags.MouseMove, true)]
	[InlineData((uint)IDCMPFlags.MouseButtons, true)]
	[InlineData((uint)IDCMPFlags.GadgetDown, true)]
	[InlineData((uint)IDCMPFlags.GadgetUp, true)]
	[InlineData((uint)IDCMPFlags.ChangeWindow, true)]
	[InlineData((uint)IDCMPFlags.RawKey, false)]
	public void MouseObjectRefreshIncludesWindowGeometryEvents(
		uint eventClass, bool expected)
	{
		Assert.Equal(expected,
			MuiNativeApplicationWindowEvents.ShouldRefreshMouseObject(eventClass));
	}

	[Fact]
	public void NativeRawKeyTranslationBuildsNamedInputEventWithDeadKeyAddress()
	{
		var platform = CreatePlatform();
		var intuiMessage = APTR.FromPointer(0x1200);
		var addressRecord = APTR.FromPointer(0x1400);
		var deadKeyAddress = APTR.FromPointer(0x1500);
		Assert.True(MuiWindowInputEventAddressCodec.Write(ref platform,
			addressRecord, new MuiWindowInputEventAddressRecord
			{
				Address = deadKeyAddress,
			}));
		Assert.True(MuiIntuiMessageCodec.WritePointerWithTime(ref platform,
			intuiMessage, MuiIntuiMessageCodec.RawKeyClass, 0x00C1, 0x0088,
			addressRecord.Raw, -2, 7, 1234, 567890));

		Assert.True(MuiNativeApplicationWindowEvents.TryTranslateInputEvent(
			ref platform, intuiMessage, out var inputEvent));
		Assert.Equal(InputEventClass.RawKey, inputEvent.Class);
		Assert.Equal(InputEventSubClass.Compatible, inputEvent.SubClass);
		Assert.Equal((ushort)0x00C1, inputEvent.Code);
		Assert.Equal((InputEventQualifier)0x0088, inputEvent.Qualifier);
		Assert.Equal(unchecked((int)deadKeyAddress.Raw), inputEvent.Position);
		Assert.Equal(1234u, inputEvent.TimeStamp.Seconds);
		Assert.Equal(567890u, inputEvent.TimeStamp.Microseconds);
	}

	[Fact]
	public void NativeRawKeyTranslationRejectsInvalidDeadKeyPointerAndOtherClasses()
	{
		var platform = CreatePlatform();
		var intuiMessage = APTR.FromPointer(0x1200);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform,
			intuiMessage, MuiIntuiMessageCodec.RawKeyClass, 0x0041, 0x0008,
			0x22000, 0, 0));
		Assert.False(MuiNativeApplicationWindowEvents.TryTranslateInputEvent(
			ref platform, intuiMessage, out _));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform,
			intuiMessage, 0x00000008, 0x0041, 0x0008, 0, 0, 0));
		Assert.False(MuiNativeApplicationWindowEvents.TryTranslateInputEvent(
			ref platform, intuiMessage, out _));
	}

	[Theory]
	[InlineData((uint)IDCMPFlags.DiskInserted,
		(byte)InputEventClass.DiskInserted)]
	[InlineData((uint)IDCMPFlags.DiskRemoved,
		(byte)InputEventClass.DiskRemoved)]
	public void NativeDiskEventsTranslateWithoutReadingUnusedAddress(
		uint messageClass, byte expectedEventClass)
	{
		var platform = CreatePlatform();
		var intuiMessage = APTR.FromPointer(0x1200);
		Assert.True(MuiIntuiMessageCodec.WritePointerWithTime(ref platform,
			intuiMessage, messageClass, 0x0023, 0x0048, 0x22000, -2, 7,
			1234, 567890));

		Assert.True(MuiNativeApplicationWindowEvents.TryTranslateInputEvent(
			ref platform, intuiMessage, out var inputEvent));
		Assert.Equal((InputEventClass)expectedEventClass, inputEvent.Class);
		Assert.Equal(InputEventSubClass.Compatible, inputEvent.SubClass);
		Assert.Equal((ushort)0x0023, inputEvent.Code);
		Assert.Equal((InputEventQualifier)0x0048, inputEvent.Qualifier);
		Assert.Equal(0, inputEvent.Position);
		Assert.Equal(1234u, inputEvent.TimeStamp.Seconds);
		Assert.Equal(567890u, inputEvent.TimeStamp.Microseconds);
	}

	[Fact]
	public void NativeWindowEventRejectsEmptyClassMalformedAndTruncatedMessages()
	{
		var platform = CreatePlatform();
		var message = APTR.FromPointer(0x1200);
		var packet = APTR.FromPointer(0x1300);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, message,
			0, 0, 0, 0, 0, 0));
		Assert.False(MuiNativeApplicationWindowEvents.TryPrepareHandleEvent(
			ref platform, message, packet, out _));
		Assert.False(MuiNativeApplicationWindowEvents.TryPrepareHandleEvent(
			ref platform, APTR.FromPointer(0x20FFC), packet, out _));
		Assert.False(MuiNativeApplicationWindowEvents.TryPrepareHandleEvent(
			ref platform, APTR.FromPointer(0x22000), packet, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x20000, 0x4000, State);

	private static MuiHeadlessTestPlatform CreateNativeControlCharPlatform(
		uint controlChar, ushort qualifier, out APTR publicObjects,
		out APTR ownerRoot, out APTR windowSidecar, out APTR intuiMessage,
		out APTR inputEvent, out APTR packet, out APTR activeObjectAttribute)
	{
		var platform = CreatePlatform();
		publicObjects = APTR.FromPointer(0x1400);
		ownerRoot = APTR.FromPointer(0x1800);
		var windowObject = APTR.FromPointer(0x1900);
		var activeObject = APTR.FromPointer(0x1910);
		var objectClass = APTR.FromPointer(0x1A00);
		windowSidecar = APTR.FromPointer(0x2000);
		var activeSidecar = APTR.FromPointer(0x2100);
		var windowBindingAddress = APTR.FromPointer(0x3000);
		var activeBindingAddress = APTR.FromPointer(0x3040);
		activeObjectAttribute = APTR.FromPointer(0x3200);
		var controlCharAttribute = APTR.FromPointer(0x3240);
		intuiMessage = APTR.FromPointer(0x4000);
		inputEvent = APTR.FromPointer(0x4100);
		packet = APTR.FromPointer(0x4200);

		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref platform,
			publicObjects, new MuiNativePublicObjectRegistryRecord
			{
				Signature = MuiNativePublicObjectRegistryRecord.Magic,
				Revision = MuiNativePublicObjectRegistryRecord.Version,
				Head = windowBindingAddress,
			}));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref platform,
			windowBindingAddress, new MuiNativePublicObjectBinding
			{
				Signature = MuiNativePublicObjectBinding.Magic,
				Next = activeBindingAddress,
				Object = windowObject,
				Class = objectClass,
				OwnerRoot = ownerRoot,
				Sidecar = windowSidecar,
			}));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref platform,
			activeBindingAddress, new MuiNativePublicObjectBinding
			{
				Signature = MuiNativePublicObjectBinding.Magic,
				Object = activeObject,
				Class = objectClass,
				OwnerRoot = ownerRoot,
				Sidecar = activeSidecar,
			}));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref platform, windowSidecar,
			new MuiNativeMuiObjectRecord
			{
				Signature = MuiNativeMuiObjectRecord.Magic,
				Revision = MuiNativeMuiObjectRecord.Version,
				Object = windowObject,
				Class = objectClass,
				OwnerRoot = ownerRoot,
				Attributes = activeObjectAttribute,
				Flags = MuiNativeMuiObjectRecord.ObjectInitialized,
				LifecycleState = MuiNativeMuiObjectRecord.StateLive,
			}));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref platform, activeSidecar,
			new MuiNativeMuiObjectRecord
			{
				Signature = MuiNativeMuiObjectRecord.Magic,
				Revision = MuiNativeMuiObjectRecord.Version,
				Object = activeObject,
				Class = objectClass,
				OwnerRoot = ownerRoot,
				Attributes = controlCharAttribute,
				Flags = MuiNativeMuiObjectRecord.ObjectInitialized,
				LifecycleState = MuiNativeMuiObjectRecord.StateLive,
			}));
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref platform,
			activeObjectAttribute, new MuiNativeObjectAttributeRecord
			{
				Signature = MuiNativeObjectAttributeRecord.Magic,
				Revision = MuiNativeObjectAttributeRecord.Version,
				Attribute = MuiWindowPublicCore.ActiveObject,
				Value = activeObject.Raw,
				Generation = 1,
			}));
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref platform,
			controlCharAttribute, new MuiNativeObjectAttributeRecord
			{
				Signature = MuiNativeObjectAttributeRecord.Magic,
				Revision = MuiNativeObjectAttributeRecord.Version,
				Attribute = MuiCommonControlCore.ControlChar,
				Value = controlChar,
				Generation = 1,
			}));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform,
			intuiMessage, MuiIntuiMessageCodec.RawKeyClass,
			(ushort)MuiNativeRawKeyCode.S, qualifier, 0, 0, 0));
		Assert.True(MuiNativeApplicationWindowEvents.TryPrepareHandleEvent(
			ref platform, intuiMessage, packet, out _));
		Assert.True(MuiWindowInputEventRecordCodec.WriteStructural(ref platform,
			inputEvent, new MuiWindowInputEventRecord
			{
				Class = (byte)InputEventClass.RawKey,
				SubClass = (byte)InputEventSubClass.Compatible,
				Code = (ushort)MuiNativeRawKeyCode.S,
				Qualifier = qualifier,
			}));
		return platform;
	}
}
