/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using Amiga.MUI;

namespace CopperOS.MuiMaster;

// Native Window event-handler registration stores the caller-owned MorphOS
// MUI_EventHandlerNode records themselves in a sidecar-owned, priority-ordered
// doubly linked list. The public guest records remain the source of truth.
internal static class MuiNativeWindowEventHandlerQueue
{
	private const ushort GuiMode = MuiEventHandlerNodeInput.MUI_EHF_GUIMODE;
	private const ushort Priority = MuiEventHandlerNodeInput.MUI_EHF_PRIORITY;
	private const ushort IsCalling = MuiEventHandlerNodeInput.MUI_EHF_ISCALLING;
	private const ushort IsEnabled = MuiEventHandlerNodeInput.MUI_EHF_ISENABLED;
	private const ushort IsActive = MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE;
	private const ushort IsActiveGroup = MuiEventHandlerNodeInput.MUI_EHF_ISACTIVEGRP;
	private const ushort AlwaysKeys = MuiEventHandlerNodeInput.MUI_EHF_ALWAYSKEYS;
	private const uint EventHandlerEat = 1;
	internal const uint EventClassActiveWindow = 0x00040000;
	internal const uint EventClassInactiveWindow = 0x00080000;
	internal const uint EventClassChangeWindow = 0x02000000;
	internal const uint EventClassMouseObject = MUIConstants.IDCMP_MOUSEOBJECT;
	private const uint IsShownAttribute = 0x7FFF0003;
	private const int MuiKeyNone = -1;

	internal static bool WriteMouseObjectMessage<TMemory>(ref TMemory memory,
		APTR messageAddress, MuiIntuiPointerMessage source)
		where TMemory : struct, IMuiGuestMemory
	{
		var message = default(MuiIntuiPointerMessage);
		message.Class = EventClassMouseObject;
		message.Qualifier = source.Qualifier;
		message.MouseX = source.MouseX;
		message.MouseY = source.MouseY;
		message.Seconds = source.Seconds;
		message.Micros = source.Micros;
		message.TimestampValid = source.TimestampValid;
		return MuiIntuiMessageCodec.WritePointerRecord(ref memory,
			messageAddress, message);
	}

	internal static bool Validate<TMemory>(ref TMemory memory,
		APTR sidecarAddress)
		where TMemory : struct, IMuiGuestMemory =>
		MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
			out var sidecar) && Validate(ref memory, sidecar);

	internal static bool Validate<TMemory>(ref TMemory memory,
		MuiNativeMuiObjectRecord sidecar)
		where TMemory : struct, IMuiGuestMemory
		=> Validate(ref memory, sidecar, out _);

	private static bool Validate<TMemory>(ref TMemory memory,
		MuiNativeMuiObjectRecord sidecar, out uint count)
		where TMemory : struct, IMuiGuestMemory
	{
		count = 0;
		var current = sidecar.WindowEventHandlers;
		var previous = APTR.Null;
		var hasPrevious = false;
		var previousPriority = false;
		sbyte previousPriorityValue = 0;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiEventHandlerNodeCodec.TryRead(ref memory, current,
				out var handler) || handler.NodeSuccessor == current ||
				handler.NodePredecessor != previous ||
				(handler.Flags & IsEnabled) == 0) return false;
			var isPriority = (handler.Flags & Priority) != 0;
			if (hasPrevious && (isPriority && !previousPriority ||
				isPriority == previousPriority &&
				previousPriorityValue < handler.Priority)) return false;
			previous = current;
			previousPriority = isPriority;
			previousPriorityValue = handler.Priority;
			hasPrevious = true;
			current = handler.NodeSuccessor;
			count++;
		}
		return current.IsNull;
	}

	internal static bool TryAdd<TMemory>(ref TMemory memory,
		APTR sidecarAddress, APTR handlerAddress)
		where TMemory : struct, IMuiGuestMemory
	{
		if (handlerAddress.IsNull ||
			!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
				out var sidecar) ||
			sidecar.WindowEventHandlerGeneration == uint.MaxValue ||
			!MuiEventHandlerNodeCodec.TryRead(ref memory, handlerAddress,
				out var handler) || (handler.Flags & IsEnabled) != 0)
			return false;
		if (!Validate(ref memory, sidecar, out var handlerCount) ||
			handlerCount >= MuiHeadlessLayout.MaximumTraversal) return false;

		var current = sidecar.WindowEventHandlers;
		var previous = APTR.Null;
		var successor = APTR.Null;
		var handlerHasPriority = (handler.Flags & Priority) != 0;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiEventHandlerNodeCodec.TryRead(ref memory, current,
				out var currentHandler)) return false;
			var currentHasPriority = (currentHandler.Flags & Priority) != 0;
			// Priority handlers form the leading partition; within each partition,
			// larger signed BYTE priorities run first and equal priorities are FIFO.
			if (handlerHasPriority != currentHasPriority)
			{
				if (handlerHasPriority)
				{
					successor = current;
					break;
				}
			}
			else if (currentHandler.Priority < handler.Priority)
			{
				successor = current;
				break;
			}
			previous = current;
			current = currentHandler.NodeSuccessor;
		}
		if (current.IsNotNull && successor.IsNull && visited >=
			MuiHeadlessLayout.MaximumTraversal) return false;

		var previousRecord = default(MuiEventHandlerNodeRecord);
		var successorRecord = default(MuiEventHandlerNodeRecord);
		if (previous.IsNotNull && !MuiEventHandlerNodeCodec.TryRead(ref memory,
			previous, out previousRecord) || successor.IsNotNull &&
			!MuiEventHandlerNodeCodec.TryRead(ref memory, successor,
				out successorRecord)) return false;

		handler.Flags = (ushort)((handler.Flags & ~(IsActive | IsCalling)) |
			IsEnabled);
		handler.NodePredecessor = previous;
		handler.NodeSuccessor = successor;
		if (!MuiEventHandlerNodeCodec.Write(ref memory, handlerAddress, handler))
			return false;
		if (previous.IsNotNull)
		{
			previousRecord.NodeSuccessor = handlerAddress;
			if (!MuiEventHandlerNodeCodec.Write(ref memory, previous,
				previousRecord)) return false;
		}
		if (successor.IsNotNull)
		{
			successorRecord.NodePredecessor = handlerAddress;
			if (!MuiEventHandlerNodeCodec.Write(ref memory, successor,
				successorRecord)) return false;
		}
		if (previous.IsNull) sidecar.WindowEventHandlers = handlerAddress;
		sidecar.WindowEventHandlerGeneration++;
		return MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress, sidecar);
	}

	internal static bool TryRemove<TMemory>(ref TMemory memory,
		APTR sidecarAddress, APTR handlerAddress)
		where TMemory : struct, IMuiGuestMemory
	{
		if (handlerAddress.IsNull ||
			!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
				out var sidecar) || !Validate(ref memory, sidecar) ||
			sidecar.WindowEventHandlerGeneration == uint.MaxValue) return false;

		var current = sidecar.WindowEventHandlers;
		var target = default(MuiEventHandlerNodeRecord);
		var found = false;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiEventHandlerNodeCodec.TryRead(ref memory, current,
				out var node)) return false;
			if (current == handlerAddress)
			{
				target = node;
				found = true;
				break;
			}
			current = node.NodeSuccessor;
		}
		if (!found) return false;

		var previousRecord = default(MuiEventHandlerNodeRecord);
		var successorRecord = default(MuiEventHandlerNodeRecord);
		if (target.NodePredecessor.IsNotNull &&
			!MuiEventHandlerNodeCodec.TryRead(ref memory,
				target.NodePredecessor, out previousRecord) ||
			target.NodeSuccessor.IsNotNull &&
			!MuiEventHandlerNodeCodec.TryRead(ref memory,
				target.NodeSuccessor, out successorRecord)) return false;

		if (target.NodePredecessor.IsNotNull)
		{
			previousRecord.NodeSuccessor = target.NodeSuccessor;
			if (!MuiEventHandlerNodeCodec.Write(ref memory,
				target.NodePredecessor, previousRecord)) return false;
		}
		else sidecar.WindowEventHandlers = target.NodeSuccessor;
		if (target.NodeSuccessor.IsNotNull)
		{
			successorRecord.NodePredecessor = target.NodePredecessor;
			if (!MuiEventHandlerNodeCodec.Write(ref memory, target.NodeSuccessor,
				successorRecord)) return false;
		}
		target.NodeSuccessor = APTR.Null;
		target.NodePredecessor = APTR.Null;
		target.Flags = (ushort)(target.Flags &
			~(IsEnabled | IsActive | IsCalling));
		if (!MuiEventHandlerNodeCodec.Write(ref memory, handlerAddress, target))
			return false;
		sidecar.WindowEventHandlerGeneration++;
		return MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress, sidecar);
	}

	// The queue borrows the public handler nodes; object disposal only detaches
	// them and clears MUI-maintained state. It never frees caller-owned memory.
	internal static bool DetachValidated<TMemory>(ref TMemory memory, APTR head)
		where TMemory : struct, IMuiGuestMemory
	{
		var current = head;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiEventHandlerNodeCodec.TryRead(ref memory, current,
				out var handler)) return false;
			var next = handler.NodeSuccessor;
			handler.NodeSuccessor = APTR.Null;
			handler.NodePredecessor = APTR.Null;
			handler.Flags = (ushort)(handler.Flags &
				~(IsEnabled | IsActive | IsCalling));
			if (!MuiEventHandlerNodeCodec.Write(ref memory, current, handler))
				return false;
			current = next;
		}
		return current.IsNull;
	}

	// Returns true when a callback requests MUI_EventHandlerRC_Eat or the
	// preprocessed key is disabled for this Window. Other nonzero callback
	// results do not suppress the Window's remaining HandleEvent processing.
	internal static bool Dispatch(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR sidecarAddress,
		APTR inputMessage, APTR handleEventPacket)
	{
		var memory = default(MuiNativeClassMemory);
		if (!MuiIntuiMessageCodec.TryReadPointerRecord(ref memory, inputMessage,
			out var intuiMessage) || intuiMessage.Class == 0 ||
			!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
				out var sidecar) || !Validate(ref memory, sidecar)) return false;
		if (MuiNativeWindowKeyPolicy.IsDisabled(ref memory, sidecarAddress,
			handleEventPacket)) return true;
		var activeObject = ReadObjectAttribute(ref memory, sidecarAddress,
			MuiWindowPublicCore.ActiveObject);
		var defaultObject = ReadObjectAttribute(ref memory, sidecarAddress,
			MuiWindowPublicCore.DefaultObject);
		var keyEvent = MuiCommonControlPacketCore.TryReadHandleEvent(ref memory,
			handleEventPacket, out var handleEvent) &&
			handleEvent.MuiKey != MuiKeyNone;
		var generation = sidecar.WindowEventHandlerGeneration;
		if (!RefreshActiveFlags(ref memory, publicObjects, ownerRoot,
			sidecarAddress)) return false;

		if (!DispatchPass(ref platform, publicObjects, ownerRoot, sidecarAddress,
			inputMessage, handleEventPacket, intuiMessage.Class, generation,
			true, APTR.Null, activeObject, defaultObject, keyEvent, out var eaten))
			return eaten;
		if (eaten) return true;

		if (activeObject.IsNotNull)
		{
			if (!DispatchPass(ref platform, publicObjects, ownerRoot,
				sidecarAddress, inputMessage, handleEventPacket,
				intuiMessage.Class, generation, false, activeObject, activeObject,
				defaultObject, keyEvent, out eaten)) return eaten;
			if (eaten) return true;

			if (keyEvent && !DispatchActiveParents(ref platform, publicObjects,
				ownerRoot, sidecarAddress, inputMessage, handleEventPacket,
				intuiMessage.Class, generation, activeObject, defaultObject,
				out eaten)) return eaten;
			if (eaten) return true;

			if (defaultObject.IsNotNull && defaultObject != activeObject)
			{
				if (!DispatchPass(ref platform, publicObjects, ownerRoot,
					sidecarAddress, inputMessage, handleEventPacket,
					intuiMessage.Class, generation, false, defaultObject,
					activeObject, defaultObject, keyEvent, out eaten)) return eaten;
				if (eaten) return true;
			}
		}
		else if (defaultObject.IsNotNull)
		{
			if (!DispatchPass(ref platform, publicObjects, ownerRoot,
				sidecarAddress, inputMessage, handleEventPacket,
				intuiMessage.Class, generation, false, defaultObject,
				APTR.Null, defaultObject, keyEvent, out eaten)) return eaten;
			if (eaten) return true;
		}

		if (!DispatchPass(ref platform, publicObjects, ownerRoot, sidecarAddress,
			inputMessage, handleEventPacket, intuiMessage.Class, generation,
			false, APTR.Null, activeObject, defaultObject, keyEvent, out eaten))
			return eaten;
		return eaten;
	}

	// IDCMP_MOUSEOBJECT is a MUI event-handler class, not an Intuition IDCMP
	// subscription. Deliver it synchronously with a short-lived named
	// IntuiMessage record so handlers can match the documented class bit and
	// query the already-published MUIA_Window_MouseObject value.
	internal static void DispatchMouseObjectChange(
		ref MuiNativeClassPlatform platform, APTR publicObjects, APTR ownerRoot,
		APTR sidecarAddress, APTR expectedMouseObject, APTR sourceMessage)
	{
		var memory = default(MuiNativeClassMemory);
		if (!MuiIntuiMessageCodec.TryReadPointerRecord(ref memory, sourceMessage,
			out var source) || source.Class == 0 ||
			!MuiNativeObjectStateCore.TryGetAttribute(ref memory, sidecarAddress,
				MuiWindowPublicCore.MouseObject, out var currentMouseObject) ||
			currentMouseObject != expectedMouseObject.Raw ||
			(expectedMouseObject.IsNotNull &&
			 !MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
				 ownerRoot, expectedMouseObject, out _))) return;

		var syntheticMessage = platform.Allocate(IntuiMessage.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (syntheticMessage.IsNull) return;
		var handleEventPacket = platform.Allocate(
			MuiCommonHandleEventMessage.Size, MuiHeadlessLayout.AllocationFlags);
		if (handleEventPacket.IsNull)
		{
			platform.Free(syntheticMessage, IntuiMessage.Size);
			return;
		}

		platform.Clear(syntheticMessage, IntuiMessage.Size);
		var packet = default(MuiCommonHandleEventMessage);
		packet.MethodId = MuiCommonControlPacketCore.HandleEvent;
		packet.InputMessage = syntheticMessage.Raw;
		packet.MuiKey = MuiKeyNone;
		var written = WriteMouseObjectMessage(ref memory, syntheticMessage,
			source) &&
			MuiCommonHandleEventMessageCodec.TryWrite(ref memory,
				handleEventPacket, packet);
		if (written)
			Dispatch(ref platform, publicObjects, ownerRoot, sidecarAddress,
				syntheticMessage, handleEventPacket);
		platform.Clear(handleEventPacket, MuiCommonHandleEventMessage.Size);
		platform.Free(handleEventPacket, MuiCommonHandleEventMessage.Size);
		platform.Clear(syntheticMessage, IntuiMessage.Size);
		platform.Free(syntheticMessage, IntuiMessage.Size);
	}

	private static bool DispatchActiveParents(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR sidecarAddress,
		APTR inputMessage, APTR handleEventPacket, uint eventClass,
		uint generation, APTR activeObject, APTR defaultObject, out bool eaten)
	{
		eaten = false;
		var memory = default(MuiNativeClassMemory);
		var current = activeObject;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
				ownerRoot, current, out var binding)) return true;
			current = binding.Parent;
			if (current.IsNull) return true;
			if (current != defaultObject)
			{
				if (!DispatchPass(ref platform, publicObjects, ownerRoot,
					sidecarAddress, inputMessage, handleEventPacket, eventClass,
					generation, false, current, activeObject, defaultObject, true,
					out eaten)) return false;
				if (eaten) return true;
				if (ReadObjectAttribute(ref memory, sidecarAddress,
					MuiWindowPublicCore.ActiveObject) != activeObject) return true;
			}
		}
		return current.IsNull;
	}

	// A pass traverses the public linked list directly. Generation changes from
	// callbacks invalidate the saved next pointer and stop this event's walk.
	private static bool DispatchPass(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR sidecarAddress,
		APTR inputMessage, APTR handleEventPacket, uint eventClass,
		uint generation, bool priorityOnly, APTR targetObject,
		APTR activeObject, APTR defaultObject, bool keyEvent, out bool eaten)
	{
		eaten = false;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
			out var sidecar) || !Validate(ref memory, sidecar) ||
			sidecar.WindowEventHandlerGeneration != generation) return false;
		var current = sidecar.WindowEventHandlers;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiEventHandlerNodeCodec.TryRead(ref memory, current,
				out var handler)) return false;
			var next = handler.NodeSuccessor;
			var isPriority = (handler.Flags & Priority) != 0;
			if (isPriority == priorityOnly &&
				(handler.Flags & IsEnabled) != 0 &&
				(handler.Events & eventClass) != 0 &&
				handler.Object.IsNotNull &&
				IsSelected(ref memory, publicObjects, ownerRoot, handler.Object,
					targetObject, activeObject, defaultObject, keyEvent,
					priorityOnly) &&
				((handler.Flags & GuiMode) == 0 || NativeGuiModeAllows(
					ref platform, publicObjects, ownerRoot, handler.Object,
					eventClass)))
			{
				if (!RefreshEventHandlerActiveFlag(ref memory, publicObjects,
					ownerRoot, sidecarAddress, current, ref handler)) return false;
				handler.Flags = (ushort)(handler.Flags | IsCalling);
				if (!MuiEventHandlerNodeCodec.Write(ref memory, current, handler))
					return false;
				var result = handler.Class.IsNotNull ?
					MuiNativeBoopsiDispatch.CoerceMethod(ref platform,
						handler.Class, handler.Object, handleEventPacket) :
					MuiNativeBoopsiDispatch.DoMethod(ref platform,
						handler.Object, handleEventPacket);
				if (MuiEventHandlerNodeCodec.TryRead(ref memory, current,
					out var completed))
				{
					completed.Flags = (ushort)(completed.Flags & ~IsCalling);
					MuiEventHandlerNodeCodec.Write(ref memory, current, completed);
				}
				if (result == EventHandlerEat)
				{
					eaten = true;
					return true;
				}
				if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
					out sidecar) ||
					sidecar.WindowEventHandlerGeneration != generation) return false;
			}
			current = next;
		}
		return current.IsNull;
	}

	// GUI mode is an optional filtering policy on an already registered node.
	// The default path still delivers events to enabled nodes which did not ask
	// for GUI-mode suppression. Named binding/sidecar records provide inherited
	// Disabled and visibility state; no private BOOPSI object layout is read.
	internal static bool GuiModeAllows<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR obj, uint eventClass)
		where TMemory : struct, IMuiGuestMemory
	{
		if ((eventClass & (EventClassActiveWindow | EventClassInactiveWindow |
			EventClassChangeWindow)) != 0) return true;
		var current = obj;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
				ownerRoot, current, out var binding)) return false;
			if (binding.Sidecar.IsNotNull &&
				((MuiNativeObjectStateCore.TryGetAttribute(ref memory,
					binding.Sidecar, MuiCommonControlCore.Disabled, out var disabled) &&
					disabled != 0) ||
				 (MuiNativeObjectStateCore.TryGetAttribute(ref memory,
					binding.Sidecar, MuiCommonControlCore.ShowMe, out var showMe) &&
					showMe == 0) ||
				 (MuiNativeObjectStateCore.TryGetAttribute(ref memory,
					binding.Sidecar, IsShownAttribute, out var isShown) &&
					isShown == 0))) return false;
			current = binding.Parent;
		}
		return current.IsNull;
	}

	private static bool IsSelected(ref MuiNativeClassMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR handlerObject,
		APTR targetObject, APTR activeObject, APTR defaultObject,
		bool keyEvent, bool priorityOnly)
	{
		if (priorityOnly) return true;
		if (targetObject.IsNotNull) return handlerObject == targetObject;
		if (handlerObject == activeObject || handlerObject == defaultObject ||
			keyEvent && IsObjectInParentChain(ref memory, publicObjects, ownerRoot,
				activeObject, handlerObject)) return false;
		return !keyEvent || HasFlag(ref memory, handlerObject, AlwaysKeys);
	}

	private static bool NativeGuiModeAllows(
		ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR obj, uint eventClass)
	{
		var storage = platform.Allocate(MuiGuestUlongStorage.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (storage.IsNull) return false;
		var allowed = MuiNativeGuiMode.Allows(ref platform, publicObjects,
			ownerRoot, obj, eventClass, storage);
		platform.Free(storage, MuiGuestUlongStorage.Size);
		return allowed;
	}

	internal static bool RefreshActiveFlags<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR sidecarAddress)
		where TMemory : struct, IMuiGuestMemory
	{
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
			out var sidecar) || !Validate(ref memory, sidecar)) return false;
		var activeObject = ReadObjectAttribute(ref memory, sidecarAddress,
			MuiWindowPublicCore.ActiveObject);
		var defaultObject = ReadObjectAttribute(ref memory, sidecarAddress,
			MuiWindowPublicCore.DefaultObject);
		var current = sidecar.WindowEventHandlers;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiEventHandlerNodeCodec.TryRead(ref memory, current,
				out var handler) || !SetEventHandlerActiveFlag(ref memory,
				publicObjects, ownerRoot, current, ref handler, activeObject,
				defaultObject)) return false;
			current = handler.NodeSuccessor;
		}
		return current.IsNull;
	}

	private static bool RefreshEventHandlerActiveFlag<TMemory>(
		ref TMemory memory, APTR publicObjects, APTR ownerRoot,
		APTR sidecarAddress, APTR handlerAddress,
		ref MuiEventHandlerNodeRecord handler)
		where TMemory : struct, IMuiGuestMemory
	{
		var currentActive = ReadObjectAttribute(ref memory, sidecarAddress,
			MuiWindowPublicCore.ActiveObject);
		var currentDefault = ReadObjectAttribute(ref memory, sidecarAddress,
			MuiWindowPublicCore.DefaultObject);
		return SetEventHandlerActiveFlag(ref memory, publicObjects, ownerRoot,
			handlerAddress, ref handler, currentActive, currentDefault);
	}

	private static bool SetEventHandlerActiveFlag<TMemory>(
		ref TMemory memory, APTR publicObjects, APTR ownerRoot,
		APTR handlerAddress, ref MuiEventHandlerNodeRecord handler,
		APTR activeObject, APTR defaultObject)
		where TMemory : struct, IMuiGuestMemory
	{
		var isActive = handler.Object.IsNotNull &&
			(handler.Object == activeObject || handler.Object == defaultObject);
		if (!isActive && handler.Object.IsNotNull &&
			(handler.Flags & IsActiveGroup) != 0)
			isActive = IsObjectInParentChain(ref memory, publicObjects, ownerRoot,
				activeObject, handler.Object) || IsObjectInParentChain(ref memory,
					publicObjects, ownerRoot, defaultObject, handler.Object);
		handler.Flags = isActive ? (ushort)(handler.Flags | IsActive) :
			(ushort)(handler.Flags & ~IsActive);
		return MuiEventHandlerNodeCodec.Write(ref memory, handlerAddress, handler);
	}

	private static bool HasFlag(ref MuiNativeClassMemory memory, APTR handler,
		ushort flag) => MuiEventHandlerNodeCodec.TryRead(ref memory, handler,
			out var record) && (record.Flags & flag) != 0;

	private static APTR ReadObjectAttribute<TMemory>(ref TMemory memory,
		APTR sidecarAddress, uint attribute)
		where TMemory : struct, IMuiGuestMemory
	{
		if (!MuiNativeObjectStateCore.TryGetAttribute(ref memory, sidecarAddress,
			attribute, out var value)) return APTR.Null;
		return APTR.FromPointer(value);
	}

	private static bool IsObjectInParentChain<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR obj, APTR ancestor)
		where TMemory : struct, IMuiGuestMemory
	{
		if (obj.IsNull || ancestor.IsNull) return false;
		var current = obj;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (current == ancestor) return true;
			if (!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
				ownerRoot, current, out var binding)) return false;
			current = binding.Parent;
		}
		return false;
	}
}
