/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using Amiga.MUI;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeWindowEventHandlerQueueTests
{
	private static readonly APTR Sidecar = APTR.FromPointer(0x1800);
	private static readonly APTR Normal = APTR.FromPointer(0x1900);
	private static readonly APTR HighPriority = APTR.FromPointer(0x1920);
	private static readonly APTR LowPriority = APTR.FromPointer(0x1940);
	private static readonly APTR ActiveAttribute = APTR.FromPointer(0x1880);
	private static readonly APTR DefaultAttribute = APTR.FromPointer(0x18A0);

	[Fact]
	public void MouseObjectEventUsesMorphOsClassAndCopiesNamedPointerFields()
	{
		var memory = new MuiHeadlessTestPlatform(0x1000, 0x1000, 0,
			APTR.FromPointer(0x1000));
		var syntheticAddress = APTR.FromPointer(0x1800);
		var source = new MuiIntuiPointerMessage
		{
			Class = (uint)IDCMPFlags.MouseMove,
			Code = 3,
			Qualifier = 0x0040,
			MouseX = -17,
			MouseY = 29,
		};

		Assert.True(MuiNativeWindowEventHandlerQueue.WriteMouseObjectMessage(
			ref memory, syntheticAddress, source));
		Assert.True(MuiIntuiMessageCodec.TryReadPointerRecord(ref memory,
			syntheticAddress, out var synthetic));
		Assert.Equal(MUIConstants.IDCMP_MOUSEOBJECT, synthetic.Class);
		Assert.Equal(source.Qualifier, synthetic.Qualifier);
		Assert.Equal(source.MouseX, synthetic.MouseX);
		Assert.Equal(source.MouseY, synthetic.MouseY);
		Assert.Equal((ushort)0, synthetic.Code);
		Assert.Equal(0u, synthetic.IAddress);
	}

	[Theory]
	[InlineData(0, 0, 20, 10, 10, 5, 10, 10, true)]
	[InlineData(0, 0, 10, 10, 10, 0, 4, 10, false)]
	[InlineData(-20, -10, 12, 8, -15, -5, 20, 10, true)]
	[InlineData(int.MaxValue - 1, 0, 10, 2, int.MaxValue, 0, 1, 2, true)]
	[InlineData(int.MinValue, 0, int.MaxValue, 2, 0, 0, 1, 2, false)]
	public void NativeGuiModeRectanglesUseStructValuesWithoutEdgeOverflow(
		int firstLeft, int firstTop, int firstWidth, int firstHeight,
		int secondLeft, int secondTop, int secondWidth, int secondHeight,
		bool expected)
	{
		var first = new MuiNativeAreaRectangle
		{
			Left = firstLeft,
			Top = firstTop,
			Width = firstWidth,
			Height = firstHeight,
		};
		var second = new MuiNativeAreaRectangle
		{
			Left = secondLeft,
			Top = secondTop,
			Width = secondWidth,
			Height = secondHeight,
		};

		Assert.Equal(expected, MuiNativeGuiMode.RectanglesIntersect(first,
			second));
	}

	[Theory]
	[InlineData(3, 0x00000008u, true)]
	[InlineData(2, 0x00000008u, false)]
	[InlineData(-1, 0xFFFFFFFFu, false)]
	[InlineData(32, 0xFFFFFFFFu, false)]
	public void NativeWindowDisableKeysFiltersOnlyValidSelectedMuiKeys(
		int muiKey, uint disabledKeys, bool expected)
	{
		var memory = new MuiHeadlessTestPlatform(Sidecar.Raw, 0x400, 0,
			Sidecar);
		var attributeAddress = APTR.FromPointer(0x18C0);
		var packetAddress = APTR.FromPointer(0x1900);
		var sidecar = new MuiNativeMuiObjectRecord
		{
			Signature = MuiNativeMuiObjectRecord.Magic,
			Revision = MuiNativeMuiObjectRecord.Version,
			Flags = MuiNativeMuiObjectRecord.ObjectInitialized,
			LifecycleState = MuiNativeMuiObjectRecord.StateLive,
			Attributes = attributeAddress,
		};
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, Sidecar, sidecar));
		var attribute = new MuiNativeObjectAttributeRecord
		{
			Signature = MuiNativeObjectAttributeRecord.Magic,
			Revision = MuiNativeObjectAttributeRecord.Version,
			Attribute = MuiWindowPublicCore.DisableKeys,
			Value = disabledKeys,
			Generation = 1,
		};
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref memory,
			attributeAddress, attribute));
		var packet = default(MuiCommonHandleEventMessage);
		packet.MethodId = MuiCommonControlPacketCore.HandleEvent;
		packet.InputMessage = 0x1A00;
		packet.MuiKey = muiKey;
		Assert.True(MuiCommonHandleEventMessageCodec.TryWrite(ref memory,
			packetAddress, packet));

		Assert.Equal(expected, MuiNativeWindowKeyPolicy.IsDisabled(ref memory,
			Sidecar, packetAddress));
	}

	[Fact]
	public void NativeWindowHandlersUseCallerOwnedNamedRecordsAndPriorityOrder()
	{
		var memory = new MuiHeadlessTestPlatform(Sidecar.Raw, 0x400, 0,
			Sidecar);
		var sidecar = new MuiNativeMuiObjectRecord
		{
			Signature = MuiNativeMuiObjectRecord.Magic,
			Revision = MuiNativeMuiObjectRecord.Version,
			Flags = MuiNativeMuiObjectRecord.ObjectInitialized,
			LifecycleState = MuiNativeMuiObjectRecord.StateLive,
		};
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, Sidecar, sidecar));
		WriteHandler(ref memory, Normal, 100, 0);
		WriteHandler(ref memory, HighPriority, 5,
			MuiEventHandlerNodeInput.MUI_EHF_PRIORITY);
		WriteHandler(ref memory, LowPriority, -1,
			MuiEventHandlerNodeInput.MUI_EHF_PRIORITY);

		Assert.True(MuiNativeWindowEventHandlerQueue.TryAdd(ref memory, Sidecar,
			Normal));
		Assert.True(MuiNativeWindowEventHandlerQueue.TryAdd(ref memory, Sidecar,
			LowPriority));
		Assert.True(MuiNativeWindowEventHandlerQueue.TryAdd(ref memory, Sidecar,
			HighPriority));
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref memory, Sidecar,
			out var registered));
		Assert.Equal(HighPriority, registered.WindowEventHandlers);
		Assert.Equal(3u, registered.WindowEventHandlerGeneration);
		Assert.True(MuiNativeWindowEventHandlerQueue.Validate(ref memory,
			registered));
		Assert.True(MuiEventHandlerNodeCodec.TryRead(ref memory, HighPriority,
			out var high));
		Assert.Equal(LowPriority, high.NodeSuccessor);
		Assert.True(MuiEventHandlerNodeCodec.TryRead(ref memory, LowPriority,
			out var low));
		Assert.Equal(HighPriority, low.NodePredecessor);
		Assert.Equal(Normal, low.NodeSuccessor);
		Assert.True(MuiEventHandlerNodeCodec.TryRead(ref memory, Normal,
			out var normal));
		Assert.Equal(LowPriority, normal.NodePredecessor);
		Assert.NotEqual((ushort)0, high.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISENABLED);

		Assert.True(MuiNativeWindowEventHandlerQueue.TryRemove(ref memory,
			Sidecar, LowPriority));
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref memory, Sidecar,
			out registered));
		Assert.Equal(4u, registered.WindowEventHandlerGeneration);
		Assert.True(MuiEventHandlerNodeCodec.TryRead(ref memory, HighPriority,
			out high));
		Assert.Equal(Normal, high.NodeSuccessor);
		Assert.True(MuiEventHandlerNodeCodec.TryRead(ref memory, LowPriority,
			out low));
		Assert.True(low.NodeSuccessor.IsNull);
		Assert.True(low.NodePredecessor.IsNull);
		Assert.Equal((ushort)0, (ushort)(low.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISENABLED));
		Assert.True(MuiNativeWindowEventHandlerQueue.Validate(ref memory,
			registered));
	}

	[Fact]
	public void ObjectDisposalDetachesButDoesNotFreeCallerOwnedHandlers()
	{
		var memory = new MuiHeadlessTestPlatform(Sidecar.Raw, 0x400, 0,
			Sidecar);
		var sidecar = new MuiNativeMuiObjectRecord
		{
			Signature = MuiNativeMuiObjectRecord.Magic,
			Revision = MuiNativeMuiObjectRecord.Version,
			WindowEventHandlers = Normal,
		};
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, Sidecar, sidecar));
		WriteHandler(ref memory, Normal, 2,
			MuiEventHandlerNodeInput.MUI_EHF_GUIMODE |
			MuiEventHandlerNodeInput.MUI_EHF_ISENABLED |
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE |
			MuiEventHandlerNodeInput.MUI_EHF_ISCALLING);

		Assert.True(MuiNativeWindowEventHandlerQueue.Validate(ref memory,
			sidecar));
		Assert.True(MuiNativeWindowEventHandlerQueue.DetachValidated(ref memory,
			sidecar.WindowEventHandlers));
		Assert.True(MuiEventHandlerNodeCodec.TryRead(ref memory, Normal,
			out var detached));
		Assert.True(detached.NodeSuccessor.IsNull);
		Assert.True(detached.NodePredecessor.IsNull);
		Assert.Equal(MuiEventHandlerNodeInput.MUI_EHF_GUIMODE,
			(ushort)(detached.Flags & (MuiEventHandlerNodeInput.MUI_EHF_ISENABLED |
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE |
			MuiEventHandlerNodeInput.MUI_EHF_ISCALLING |
			MuiEventHandlerNodeInput.MUI_EHF_GUIMODE)));
	}

	[Fact]
	public void NativeActiveAndDefaultObjectsRefreshReadOnlyHandlerFlags()
	{
		var memory = new MuiHeadlessTestPlatform(Sidecar.Raw, 0x400, 0,
			Sidecar);
		var activeObject = APTR.FromPointer(0x2200);
		var defaultObject = APTR.FromPointer(0x2240);
		var handlerObject = APTR.FromPointer(0x2280);
		var sidecar = new MuiNativeMuiObjectRecord
		{
			Signature = MuiNativeMuiObjectRecord.Magic,
			Revision = MuiNativeMuiObjectRecord.Version,
			Flags = MuiNativeMuiObjectRecord.ObjectInitialized,
			LifecycleState = MuiNativeMuiObjectRecord.StateLive,
			Attributes = ActiveAttribute,
			WindowEventHandlers = Normal,
			WindowEventHandlerGeneration = 1,
		};
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, Sidecar, sidecar));
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref memory,
			ActiveAttribute, new MuiNativeObjectAttributeRecord
			{
				Signature = MuiNativeObjectAttributeRecord.Magic,
				Revision = MuiNativeObjectAttributeRecord.Version,
				Next = DefaultAttribute,
				Attribute = MuiWindowPublicCore.ActiveObject,
				Value = activeObject.Raw,
				Generation = 1,
			}));
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref memory,
			DefaultAttribute, new MuiNativeObjectAttributeRecord
			{
				Signature = MuiNativeObjectAttributeRecord.Magic,
				Revision = MuiNativeObjectAttributeRecord.Version,
				Attribute = MuiWindowPublicCore.DefaultObject,
				Value = defaultObject.Raw,
				Generation = 1,
			}));
		WriteHandler(ref memory, Normal, 0,
			MuiEventHandlerNodeInput.MUI_EHF_GUIMODE |
			MuiEventHandlerNodeInput.MUI_EHF_ISENABLED);
		Assert.True(MuiEventHandlerNodeCodec.TryRead(ref memory, Normal,
			out var handler));
		handler.Object = handlerObject;
		Assert.True(MuiEventHandlerNodeCodec.Write(ref memory, Normal, handler));

		Assert.True(MuiNativeWindowEventHandlerQueue.RefreshActiveFlags(ref memory,
			APTR.Null, APTR.Null, Sidecar));
		Assert.True(MuiEventHandlerNodeCodec.TryRead(ref memory, Normal,
			out handler));
		Assert.Equal((ushort)0, (ushort)(handler.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE));

		handler.Object = activeObject;
		Assert.True(MuiEventHandlerNodeCodec.Write(ref memory, Normal, handler));
		Assert.True(MuiNativeWindowEventHandlerQueue.RefreshActiveFlags(ref memory,
			APTR.Null, APTR.Null, Sidecar));
		Assert.True(MuiEventHandlerNodeCodec.TryRead(ref memory, Normal,
			out handler));
		Assert.NotEqual((ushort)0, (ushort)(handler.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE));

		handler.Object = defaultObject;
		Assert.True(MuiEventHandlerNodeCodec.Write(ref memory, Normal, handler));
		Assert.True(MuiNativeWindowEventHandlerQueue.RefreshActiveFlags(ref memory,
			APTR.Null, APTR.Null, Sidecar));
		Assert.True(MuiEventHandlerNodeCodec.TryRead(ref memory, Normal,
			out handler));
		Assert.NotEqual((ushort)0, (ushort)(handler.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE));
	}

	[Fact]
	public void NativeActiveGroupFlagFollowsNamedPublicObjectParentLinks()
	{
		var publicObjects = APTR.FromPointer(0x1100);
		var ownerRoot = APTR.FromPointer(0x1300);
		var activeBindingAddress = APTR.FromPointer(0x1200);
		var groupBindingAddress = APTR.FromPointer(0x1240);
		var windowSidecar = Sidecar;
		var activeSidecar = APTR.FromPointer(0x1880);
		var groupSidecar = APTR.FromPointer(0x1900);
		var activeObject = APTR.FromPointer(0x2200);
		var groupObject = APTR.FromPointer(0x2240);
		var classPointer = APTR.FromPointer(0x2280);
		var activeAttribute = APTR.FromPointer(0x1A00);
		var handlerAddress = APTR.FromPointer(0x1B00);
		var memory = new MuiHeadlessTestPlatform(0x1000, 0x2000, 0,
			APTR.FromPointer(0x1000));

		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref memory,
			publicObjects, new MuiNativePublicObjectRegistryRecord
			{
				Signature = MuiNativePublicObjectRegistryRecord.Magic,
				Revision = MuiNativePublicObjectRegistryRecord.Version,
				Head = activeBindingAddress,
			}));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			activeBindingAddress, NewBinding(activeObject, classPointer,
				activeSidecar, ownerRoot, groupObject, groupBindingAddress)));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			groupBindingAddress, NewBinding(groupObject, classPointer,
				groupSidecar, ownerRoot, APTR.Null, APTR.Null)));
		var activeSidecarRecord = NewSidecar(activeObject, classPointer,
			ownerRoot);
		activeSidecarRecord.Parent = groupObject;
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, activeSidecar,
			activeSidecarRecord));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, groupSidecar,
			NewSidecar(groupObject, classPointer, ownerRoot)));
		var window = NewSidecar(APTR.FromPointer(0x2300), classPointer,
			ownerRoot);
		window.Attributes = activeAttribute;
		window.WindowEventHandlers = handlerAddress;
		window.WindowEventHandlerGeneration = 1;
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, windowSidecar,
			window));
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref memory,
			activeAttribute, new MuiNativeObjectAttributeRecord
			{
				Signature = MuiNativeObjectAttributeRecord.Magic,
				Revision = MuiNativeObjectAttributeRecord.Version,
				Attribute = MuiWindowPublicCore.ActiveObject,
				Value = activeObject.Raw,
				Generation = 1,
			}));
		WriteHandler(ref memory, handlerAddress, 0,
			MuiEventHandlerNodeInput.MUI_EHF_GUIMODE |
			MuiEventHandlerNodeInput.MUI_EHF_ISENABLED |
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVEGRP);
		Assert.True(MuiEventHandlerNodeCodec.TryRead(ref memory, handlerAddress,
			out var handler));
		handler.Object = groupObject;
		Assert.True(MuiEventHandlerNodeCodec.Write(ref memory, handlerAddress,
			handler));

		Assert.True(MuiNativeWindowEventHandlerQueue.RefreshActiveFlags(ref memory,
			publicObjects, ownerRoot, windowSidecar));
		Assert.True(MuiEventHandlerNodeCodec.TryRead(ref memory, handlerAddress,
			out handler));
		Assert.NotEqual((ushort)0, (ushort)(handler.Flags &
			MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE));
	}

	[Fact]
	public void NativeGuiModeUsesInheritedNamedAttributesAndAllowsWindowTransitions()
	{
		var publicObjects = APTR.FromPointer(0x1100);
		var ownerRoot = APTR.FromPointer(0x1300);
		var childBindingAddress = APTR.FromPointer(0x1200);
		var parentBindingAddress = APTR.FromPointer(0x1240);
		var childSidecar = APTR.FromPointer(0x1800);
		var parentSidecar = APTR.FromPointer(0x1840);
		var disabledAttribute = APTR.FromPointer(0x1A00);
		var showMeAttribute = APTR.FromPointer(0x1A20);
		var child = APTR.FromPointer(0x2200);
		var parent = APTR.FromPointer(0x2240);
		var classPointer = APTR.FromPointer(0x2280);
		var memory = new MuiHeadlessTestPlatform(0x1000, 0x2000, 0,
			APTR.FromPointer(0x1000));

		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref memory,
			publicObjects, new MuiNativePublicObjectRegistryRecord
			{
				Signature = MuiNativePublicObjectRegistryRecord.Magic,
				Revision = MuiNativePublicObjectRegistryRecord.Version,
				Head = childBindingAddress,
			}));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			childBindingAddress, NewBinding(child, classPointer, childSidecar,
				ownerRoot, parent, parentBindingAddress)));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			parentBindingAddress, NewBinding(parent, classPointer, parentSidecar,
				ownerRoot, APTR.Null, APTR.Null)));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, childSidecar,
			NewSidecar(child, classPointer, ownerRoot)));
		var parentState = NewSidecar(parent, classPointer, ownerRoot);
		parentState.Attributes = disabledAttribute;
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, parentSidecar,
			parentState));
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref memory,
			disabledAttribute, new MuiNativeObjectAttributeRecord
			{
				Signature = MuiNativeObjectAttributeRecord.Magic,
				Revision = MuiNativeObjectAttributeRecord.Version,
				Next = showMeAttribute,
				Attribute = MuiCommonControlCore.Disabled,
				Value = 1,
				Generation = 1,
			}));
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref memory,
			showMeAttribute, new MuiNativeObjectAttributeRecord
			{
				Signature = MuiNativeObjectAttributeRecord.Magic,
				Revision = MuiNativeObjectAttributeRecord.Version,
				Attribute = MuiCommonControlCore.ShowMe,
				Value = 1,
				Generation = 1,
			}));

		Assert.False(MuiNativeWindowEventHandlerQueue.GuiModeAllows(ref memory,
			publicObjects, ownerRoot, child, 0x00000008));
		Assert.True(MuiNativeObjectAttributeCodec.TryRead(ref memory,
			disabledAttribute, out var disabled));
		disabled.Value = 0;
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref memory,
			disabledAttribute, disabled));
		Assert.True(MuiNativeWindowEventHandlerQueue.GuiModeAllows(ref memory,
			publicObjects, ownerRoot, child, 0x00000008));
		Assert.True(MuiNativeObjectAttributeCodec.TryRead(ref memory,
			showMeAttribute, out var visibility));
		visibility.Value = 0;
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref memory,
			showMeAttribute, visibility));
		Assert.False(MuiNativeWindowEventHandlerQueue.GuiModeAllows(ref memory,
			publicObjects, ownerRoot, child, 0x00000008));
		Assert.True(MuiNativeWindowEventHandlerQueue.GuiModeAllows(ref memory,
			publicObjects, ownerRoot, child, 0x00040000));
	}

	private static void WriteHandler(ref MuiHeadlessTestPlatform memory,
		APTR address,
		sbyte priority, ushort flags)
	{
		Assert.True(MuiEventHandlerNodeCodec.Write(ref memory, address,
			new MuiEventHandlerNodeRecord
			{
				Priority = priority,
				Flags = flags,
				Object = APTR.FromPointer(0x2200),
				Events = 0x00000008,
		}));
	}

	private static MuiNativePublicObjectBinding NewBinding(APTR obj,
		APTR classPointer, APTR sidecar, APTR ownerRoot, APTR parent,
		APTR next) => new()
	{
		Signature = MuiNativePublicObjectBinding.Magic,
		Next = next,
		Object = obj,
		Class = classPointer,
		OwnerRoot = ownerRoot,
		Parent = parent,
		Sidecar = sidecar,
		DisposeState = MuiNativePublicObjectBinding.StateLive,
	};

	private static MuiNativeMuiObjectRecord NewSidecar(APTR obj,
		APTR classPointer, APTR ownerRoot) => new()
	{
		Signature = MuiNativeMuiObjectRecord.Magic,
		Revision = MuiNativeMuiObjectRecord.Version,
		Object = obj,
		Class = classPointer,
		OwnerRoot = ownerRoot,
		Flags = MuiNativeMuiObjectRecord.ObjectInitialized,
		LifecycleState = MuiNativeMuiObjectRecord.StateLive,
	};
}
