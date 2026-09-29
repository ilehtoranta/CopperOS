/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using Amiga.MUI;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// A temporary typed builder for native TagItem vectors. Only the TagItem
// codec knows the packed ABI representation; all presenter state is named.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRequesterApplicationTagPlanRecord
{
	internal APTR Items;
	internal uint AllocationSize;
	internal uint Capacity;
	internal uint Written;
}

internal static class MuiRequesterApplicationTagPlanCore
{
	internal static bool TryCreate<TPlatform>(ref TPlatform platform,
		uint capacity, out MuiRequesterApplicationTagPlanRecord plan)
		where TPlatform : struct, IMuiAllocationPlatform
	{
		plan = default;
		if (capacity == 0 || capacity > uint.MaxValue /
			MuiAslTagItemRecord.Size) return false;
		plan.Capacity = capacity;
		plan.AllocationSize = capacity * MuiAslTagItemRecord.Size;
		plan.Items = platform.Allocate(plan.AllocationSize,
			MuiHeadlessLayout.AllocationFlags);
		if (plan.Items.IsNull || !platform.IsMapped(plan.Items,
			plan.AllocationSize))
		{
			Release(ref platform, ref plan);
			return false;
		}
		return true;
	}

	internal static bool TryAdd<TPlatform>(ref TPlatform platform,
		ref MuiRequesterApplicationTagPlanRecord plan, uint tag, uint data)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (plan.Items.IsNull || plan.Written >= plan.Capacity ||
			!MuiAslTagItemVectorCodec.TryWrite(ref platform, plan.Items,
				plan.Written, new MuiAslTagItemRecord { Tag = tag, Data = data }))
			return false;
		plan.Written++;
		return true;
	}

	internal static bool TryFinish<TPlatform>(ref TPlatform platform,
		ref MuiRequesterApplicationTagPlanRecord plan)
		where TPlatform : struct, IMuiGuestMemory =>
		TryAdd(ref platform, ref plan, MuiAslTagListCore.TagDone, 0) &&
		plan.Written == plan.Capacity;

	internal static void Release<TPlatform>(ref TPlatform platform,
		ref MuiRequesterApplicationTagPlanRecord plan)
		where TPlatform : struct, IMuiAllocationPlatform
	{
		if (plan.Items.IsNotNull && plan.AllocationSize != 0)
			platform.Free(plan.Items, plan.AllocationSize);
		plan = default;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeRequesterApplicationPresenterRecord
{
	internal MuiRequesterApplicationGadgetPlanRecord Gadgets;
	internal APTR TitleCopy;
	internal uint TitleCopySize;
	internal APTR FormatCopy;
	internal uint FormatCopySize;
	internal APTR Text;
	internal APTR ButtonGroup;
	internal APTR RootGroup;
	internal APTR Window;
	internal APTR ActiveButton;
	internal APTR RequestObject;
	internal uint WindowAttached;
	internal uint RequestObjectAttached;
	internal uint PresentationCompleted;
	internal uint Result;
}

internal static class MuiRequesterApplicationCompletionCore
{
	internal static bool ShouldConsumeReference(
		MuiNativeRequesterApplicationPresenterRecord state) =>
		state.RequestObject.IsNotNull && state.PresentationCompleted != 0 &&
		state.RequestObjectAttached == 0;
}

internal static class MuiRequesterApplicationTitleCore
{
	internal static bool TryResolve<TPlatform>(ref TPlatform platform,
		APTR application, APTR requestedTitle, out APTR title)
		where TPlatform : struct, IMuiRequesterApplicationTitleCapability
	{
		title = requestedTitle;
		if (title.IsNotNull) return true;
		if (application.IsNull || !platform.TryGetApplicationTitle(application,
			out title))
		{
			title = APTR.Null;
			return false;
		}
		return true;
	}
}

internal static class MuiNativeRequesterSignalPolicyCore
{
	internal static uint CreateWaitMask(uint applicationSignals) =>
		applicationSignals | MuiNativeApplicationLoopCore.SignalBreakCtrlC;

	internal static bool HasBreakSignal(uint receivedSignals) =>
		(receivedSignals & MuiNativeApplicationLoopCore.SignalBreakCtrlC) != 0;
}

// Synchronous MUI_RequestA and MUI_RequestObjectA presenter. Its transient
// window is linked to the application's private native event graph, not to
// the public initializer-only MUIA_Application_Window relation. Input is
// pumped with typed window and input-handler cores as MUIM_Application_NewInput,
// leaving the application's
// ReturnID FIFO untouched for its caller.
internal static class MuiNativeRequesterApplicationPresenter
{
	private const uint ApplicationReturnUserData =
		MuiNativeObjectStateCore.UserDataAttribute;
	private const uint OpenRequester = 1;
	private const uint ClosedRequester = 0;
	private const uint HorizontalGroup = 1;
	private const uint VerticalGroup = 0;

	internal static int Request(ref MuiNativeRequesterPlatform platform,
		APTR serviceState, MuiRequesterCallRecord request,
		MuiNativeRequesterApplicationAdmissionRecord admission)
		=> Present(ref platform, serviceState, request, admission, APTR.Null,
			out _);

	internal static int RequestObject(ref MuiNativeRequesterPlatform platform,
		APTR serviceState, MuiRequesterCallRecord request,
		MuiNativeRequesterApplicationAdmissionRecord admission,
		out bool consumeReference)
	{
		consumeReference = false;
		if (!MuiNativeRequesterApplicationAdmissionCore.TryValidateAreaObject(
			ref platform, platform.PublicObjects, platform.OwnerRoot,
			request.Object, out var areaAdmission)) return 0;
		return Present(ref platform, serviceState, request, admission,
			areaAdmission.Object, out consumeReference);
	}

	private static int Present(ref MuiNativeRequesterPlatform platform,
		APTR serviceState, MuiRequesterCallRecord request,
		MuiNativeRequesterApplicationAdmissionRecord admission,
		APTR requestObject, out bool consumeReference)
	{
		consumeReference = false;
		var state = default(MuiNativeRequesterApplicationPresenterRecord);
		state.RequestObject = requestObject;
		if (serviceState.IsNull || admission.Application.IsNull ||
			admission.ApplicationSidecar.IsNull ||
			!MuiRequesterApplicationGadgetPlanCore.TryPrepare(ref platform,
				request.Gadgets, out state.Gadgets)) return 0;

		var prepared = false;
		do
		{
			if (!MuiRequesterApplicationTitleCore.TryResolve(ref platform,
				admission.Application, request.Title, out var title) ||
				!TryCopyCString(ref platform, title,
				MuiRequesterPayloadCore.MaximumStringLength,
				out state.TitleCopy, out state.TitleCopySize) ||
				!TryCopyCString(ref platform, request.Format,
				MuiRequesterPayloadCore.MaximumStringLength,
				out state.FormatCopy, out state.FormatCopySize) ||
				!CreateButtons(ref platform, serviceState, ref state,
					out state.ActiveButton) ||
				!CreateButtonGroup(ref platform, serviceState, ref state) ||
				!CreateText(ref platform, serviceState, ref state) ||
				!CreateRootGroup(ref platform, serviceState, ref state) ||
				!CreateWindow(ref platform, serviceState, admission,
					ref state) ||
				!AttachRootObject(ref platform, state.Window, state.RootGroup) ||
				!MuiNativeRequesterApplicationWindowCore.Attach(
					ref platform.Classes, platform.PublicObjects, platform.OwnerRoot,
					admission.Application, state.Window)) break;

			state.WindowAttached = 1;
			if (!AddCloseNotification(ref platform, state.Window) ||
				!AddButtonNotifications(ref platform, state.Gadgets,
					state.Window)) break;
			if (!TrySetObjectAttribute(ref platform, state.Window,
				MuiWindowPublicCore.Open, OpenRequester) ||
				!TryPumpApplication(ref platform, admission.Application,
					admission.ApplicationSidecar, state.Window,
					out state.Result)) break;
			state.PresentationCompleted = 1;
			prepared = true;
		} while (false);

		var releaseScratch = Cleanup(ref platform, serviceState, admission,
			ref state);
		prepared &= releaseScratch;
		consumeReference = MuiRequesterApplicationCompletionCore
			.ShouldConsumeReference(state);
		if (releaseScratch)
		{
			MuiRequesterApplicationGadgetPlanCore.Release(ref platform,
				ref state.Gadgets);
			FreeCopy(ref platform, ref state.TitleCopy,
				ref state.TitleCopySize);
			FreeCopy(ref platform, ref state.FormatCopy,
				ref state.FormatCopySize);
		}
		return prepared ? unchecked((int)state.Result) : 0;
	}

	private static bool CreateButtons(ref MuiNativeRequesterPlatform platform,
		APTR serviceState, ref MuiNativeRequesterApplicationPresenterRecord state,
		out APTR activeButton)
	{
		activeButton = APTR.Null;
		for (var index = 0u; index < state.Gadgets.ButtonCount; index++)
		{
			if (!MuiRequesterApplicationButtonArrayCodec.TryRead(ref platform,
				state.Gadgets.Buttons, state.Gadgets.ButtonCount, index,
				out var button)) return false;
			var parameters = platform.Allocate(
				MuiGuestUlongStorage.Size, MuiHeadlessLayout.AllocationFlags);
			if (parameters.IsNull || !platform.IsMapped(parameters,
				MuiGuestUlongStorage.Size))
			{
				if (parameters.IsNotNull)
					platform.Free(parameters, MuiGuestUlongStorage.Size);
				return false;
			}
			var parameterReady = MuiGuestUlongStorageCodec.WriteValue(ref platform,
				parameters, button.Label.Raw);
			var buttonObject = parameterReady
				? MuiMakeObjectServiceCore.MakeNativeObjectA(ref platform.Classes,
					serviceState, platform.OwnerRoot, platform.PublicObjects,
					MuiMakeObjectServiceCore.MUIO_Button, parameters)
				: APTR.Null;
			platform.Free(parameters, MuiGuestUlongStorage.Size);
			if (buttonObject.IsNull) return false;
			button.Object = buttonObject;
			if (!MuiRequesterApplicationButtonArrayCodec.TryWrite(ref platform,
				state.Gadgets.Buttons, state.Gadgets.ButtonCount, index, button))
				return false;
			if (button.IsActive != 0) activeButton = buttonObject;
		}
		return true;
	}

	private static bool CreateButtonGroup(ref MuiNativeRequesterPlatform platform,
		APTR serviceState, ref MuiNativeRequesterApplicationPresenterRecord state)
	{
		if (state.Gadgets.ButtonCount == 0) return true;
		if (!TryCreateGroup(ref platform, serviceState, HorizontalGroup,
			out state.ButtonGroup)) return false;
		for (var index = 0u; index < state.Gadgets.ButtonCount; index++)
		{
			if (!MuiRequesterApplicationButtonArrayCodec.TryRead(ref platform,
				state.Gadgets.Buttons, state.Gadgets.ButtonCount, index,
				out var button) || button.Object.IsNull ||
				!TryAddGroupChild(ref platform, state.ButtonGroup,
					button.Object)) return false;
		}
		return true;
	}

	private static bool CreateText(ref MuiNativeRequesterPlatform platform,
		APTR serviceState, ref MuiNativeRequesterApplicationPresenterRecord state)
	{
		if (state.FormatCopy.IsNull) return true;
		var tags = default(MuiRequesterApplicationTagPlanRecord);
		if (!MuiRequesterApplicationTagPlanCore.TryCreate(ref platform, 2,
			out tags)) return false;
		var complete = MuiRequesterApplicationTagPlanCore.TryAdd(ref platform,
			ref tags, MuiCommonControlCore.TextContents, state.FormatCopy.Raw) &&
			MuiRequesterApplicationTagPlanCore.TryFinish(ref platform, ref tags);
		if (complete)
			state.Text = NewObject(ref platform, serviceState,
				APTR.FromPointer(CString.ToUInt32(
					CString.FromLiteral("Text.mui"))), tags.Items);
		MuiRequesterApplicationTagPlanCore.Release(ref platform, ref tags);
		return complete && state.Text.IsNotNull;
	}

	private static bool CreateRootGroup(ref MuiNativeRequesterPlatform platform,
		APTR serviceState, ref MuiNativeRequesterApplicationPresenterRecord state)
	{
		if (!TryCreateGroup(ref platform, serviceState, VerticalGroup,
			out state.RootGroup)) return false;
		if (state.Text.IsNotNull && !TryAddGroupChild(ref platform,
			state.RootGroup, state.Text)) return false;
		if (state.RequestObject.IsNotNull)
		{
			if (!TryAddGroupChild(ref platform, state.RootGroup,
				state.RequestObject)) return false;
			state.RequestObjectAttached = 1;
		}
		if (state.ButtonGroup.IsNotNull && !TryAddGroupChild(ref platform,
			state.RootGroup, state.ButtonGroup)) return false;
		return true;
	}

	private static bool TryCreateGroup(ref MuiNativeRequesterPlatform platform,
		APTR serviceState, uint horizontal, out APTR group)
	{
		group = APTR.Null;
		var tags = default(MuiRequesterApplicationTagPlanRecord);
		if (!MuiRequesterApplicationTagPlanCore.TryCreate(ref platform, 2,
			out tags)) return false;
		var complete = MuiRequesterApplicationTagPlanCore.TryAdd(ref platform,
			ref tags, MuiCommonControlCore.GroupHoriz, horizontal) &&
			MuiRequesterApplicationTagPlanCore.TryFinish(ref platform, ref tags);
		if (complete)
			group = NewObject(ref platform, serviceState,
				APTR.FromPointer(CString.ToUInt32(
					CString.FromLiteral("Group.mui"))), tags.Items);
		MuiRequesterApplicationTagPlanCore.Release(ref platform, ref tags);
		return complete && group.IsNotNull;
	}

	private static bool TryAddGroupChild(ref MuiNativeRequesterPlatform platform,
		APTR group, APTR child)
	{
		var message = platform.Classes.Allocate(MuiFamilyChildMessage.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (message.IsNull || !platform.Classes.IsMapped(message,
			MuiFamilyChildMessage.Size))
		{
			if (message.IsNotNull)
				platform.Classes.Free(message, MuiFamilyChildMessage.Size);
			return false;
		}
		var written = MuiFamilyMutationCore.WriteRecord(ref platform.Classes,
			message, MuiFamilyMutationCore.AddTailMethod, child);
		var added = written && MuiNativeBoopsiDispatch.DoMethod(
			ref platform.Classes, group, message) != 0;
		platform.Classes.Free(message, MuiFamilyChildMessage.Size);
		if (added && MuiNativePublicObjectCore.SetParent(ref platform.Classes,
			platform.OwnerRoot, platform.PublicObjects, group, child,
			MuiNativeMuiObjectRecord.ObjectParentGroupChild)) return true;
		if (MuiNativeFamilyRemovalCore.TryRemove(ref platform.Classes, group,
			child, MuiNativeGuiMode.GroupChildList))
			MuiNativePublicObjectCore.ClearParent(ref platform.Classes,
				platform.OwnerRoot, platform.PublicObjects, group, child);
		return false;
	}

	private static bool CreateWindow(ref MuiNativeRequesterPlatform platform,
		APTR serviceState,
		MuiNativeRequesterApplicationAdmissionRecord admission,
		ref MuiNativeRequesterApplicationPresenterRecord state)
	{
		var count = 3u; // Open, Title and TAG_DONE.
		if (admission.HasReferenceWindow != 0) count++;
		if (state.ActiveButton.IsNotNull) count++;
		var tags = default(MuiRequesterApplicationTagPlanRecord);
		if (!MuiRequesterApplicationTagPlanCore.TryCreate(ref platform, count,
			out tags)) return false;
		var complete = MuiRequesterApplicationTagPlanCore.TryAdd(ref platform,
			ref tags, MuiWindowPublicCore.Open, ClosedRequester) &&
			MuiRequesterApplicationTagPlanCore.TryAdd(ref platform, ref tags,
				MuiWindowPublicCore.Title, state.TitleCopy.Raw);
		if (complete && admission.HasReferenceWindow != 0)
			complete = MuiRequesterApplicationTagPlanCore.TryAdd(ref platform,
				ref tags, MuiWindowPublicCore.RefWindow, admission.Window.Raw);
		if (complete && state.ActiveButton.IsNotNull)
			complete = MuiRequesterApplicationTagPlanCore.TryAdd(ref platform,
				ref tags, MuiWindowPublicCore.DefaultObject,
				state.ActiveButton.Raw);
		complete = complete && MuiRequesterApplicationTagPlanCore.TryFinish(
			ref platform, ref tags);
		if (complete)
			state.Window = NewObject(ref platform, serviceState,
				APTR.FromPointer(CString.ToUInt32(
					CString.FromLiteral("Window.mui"))), tags.Items);
		MuiRequesterApplicationTagPlanCore.Release(ref platform, ref tags);
		return complete && state.Window.IsNotNull;
	}

	private static bool AttachRootObject(ref MuiNativeRequesterPlatform platform,
		APTR window, APTR root)
	{
		if (!TrySetObjectAttribute(ref platform, window,
			MuiWindowPublicCore.RootObject, root.Raw) ||
			!MuiNativePublicObjectCore.GetAttribute(ref platform.Classes,
				platform.PublicObjects, platform.OwnerRoot, window,
				MuiWindowPublicCore.RootObject, out var observedRoot) ||
			observedRoot != root.Raw) return false;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativePublicObjectCore.TryFindLive(ref memory,
			platform.PublicObjects, platform.OwnerRoot, root,
			out var rootBinding)) return false;
		if (rootBinding.Parent.IsNull)
			return MuiNativePublicObjectCore.SetParent(ref platform.Classes,
				platform.OwnerRoot, platform.PublicObjects, window, root,
				MuiNativeMuiObjectRecord.ObjectParentWindowRootObject);
		if (rootBinding.Parent == window &&
			MuiNativeMuiObjectCodec.TryRead(ref memory, rootBinding.Sidecar,
				out var rootSidecar) &&
			(rootSidecar.Flags &
				MuiNativeMuiObjectRecord.ObjectParentKindMask) ==
				MuiNativeMuiObjectRecord.ObjectParentWindowRootObject) return true;
		return false;
	}

	private static bool AddCloseNotification(ref MuiNativeRequesterPlatform platform,
		APTR window) => AddSetNotification(ref platform, window,
		MuiWindowPublicCore.CloseRequest, 1, window,
		MuiWindowPublicCore.Open, ClosedRequester);

	private static bool AddButtonNotifications(ref MuiNativeRequesterPlatform platform,
		MuiRequesterApplicationGadgetPlanRecord gadgets, APTR window)
	{
		for (var index = 0u; index < gadgets.ButtonCount; index++)
		{
			if (!MuiRequesterApplicationButtonArrayCodec.TryRead(ref platform,
				gadgets.Buttons, gadgets.ButtonCount, index, out var button) ||
				button.Object.IsNull ||
				!AddSetNotification(ref platform, button.Object,
					MuiCommonControlCore.Pressed, 0, window,
					ApplicationReturnUserData, button.ReturnId) ||
				!AddSetNotification(ref platform, button.Object,
					MuiCommonControlCore.Pressed, 0, window,
					MuiWindowPublicCore.Open, ClosedRequester)) return false;
		}
		return true;
	}

	private static bool AddSetNotification(ref MuiNativeRequesterPlatform platform,
		APTR source, uint triggerAttribute, uint triggerValue, APTR destination,
		uint attribute, uint value)
	{
		var count = 3u;
		var bytes = count * MuiNotifyFollowParameterSlot.Size;
		var follow = platform.Classes.Allocate(bytes,
			MuiHeadlessLayout.AllocationFlags);
		if (follow.IsNull || !platform.Classes.IsMapped(follow, bytes))
		{
			if (follow.IsNotNull) platform.Classes.Free(follow, bytes);
			return false;
		}
		var cursor = default(MuiNotifyFollowParameterVectorCursor);
		cursor.Base = follow;
		var complete = MuiNotifyFollowParameterVectorCodec.TryWrite(ref platform,
			cursor, new MuiNotifyFollowParameterSlot
			{
				Value = MuiNotifyCore.SetMethod,
			}) && MuiNotifyFollowParameterVectorCodec.TryAdvance(ref cursor, 1) &&
			MuiNotifyFollowParameterVectorCodec.TryWrite(ref platform, cursor,
				new MuiNotifyFollowParameterSlot { Value = attribute }) &&
			MuiNotifyFollowParameterVectorCodec.TryAdvance(ref cursor, 1) &&
			MuiNotifyFollowParameterVectorCodec.TryWrite(ref platform, cursor,
				new MuiNotifyFollowParameterSlot { Value = value });
		var added = complete && MuiNativePublicObjectCore.AddNotification(
			ref platform.Classes, platform.PublicObjects, platform.OwnerRoot,
			source, triggerAttribute, triggerValue, destination, count, follow, 0,
			out _);
		platform.Classes.Free(follow, bytes);
		return added;
	}

	private static bool TryPumpApplication(ref MuiNativeRequesterPlatform platform,
		APTR application, APTR applicationSidecar, APTR window,
		out uint returnId)
	{
		returnId = 0;
		var task = Exec.FindTask(CString.FromPointer(0));
		if (task.IsNull) return false;
		while (true)
		{
			if (!MuiNativePublicObjectCore.GetAttribute(ref platform.Classes,
				platform.PublicObjects, platform.OwnerRoot, window,
				MuiWindowPublicCore.Open, out var isOpen)) return false;
			if (isOpen == 0) break;
			var memory = default(MuiNativeClassMemory);
			if (!MuiNativeApplicationWindowSignals.TryBuildMask(ref memory,
				platform.PublicObjects, platform.OwnerRoot, application, task,
				out var signals)) return false;
			var received = Exec.Wait(
				MuiNativeRequesterSignalPolicyCore.CreateWaitMask(signals));
			if (MuiNativeRequesterSignalPolicyCore.HasBreakSignal(received))
			{
				// End the synchronous requester cleanly; the application-owned
				// requester tree is closed and released by Present's cleanup path.
				return true;
			}
			MuiNativeApplicationWindowEvents.Dispatch(ref platform.Classes,
				platform.PublicObjects, platform.OwnerRoot, application, task,
				received, true);
			MuiNativeApplicationInputHandlerQueue.Dispatch(ref platform.Classes,
				platform.PublicObjects, platform.OwnerRoot, application,
				applicationSidecar, received);
		}
		return MuiNativePublicObjectCore.GetAttribute(ref platform.Classes,
			platform.PublicObjects, platform.OwnerRoot, window,
			ApplicationReturnUserData, out returnId);
	}

	private static bool TrySetObjectAttribute(ref MuiNativeRequesterPlatform platform,
		APTR obj, uint attribute, uint value)
	{
		var message = platform.Classes.Allocate(MuiSetAttributeMessage.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (message.IsNull || !platform.Classes.IsMapped(message,
			MuiSetAttributeMessage.Size))
		{
			if (message.IsNotNull)
				platform.Classes.Free(message, MuiSetAttributeMessage.Size);
			return false;
		}
		var packet = new MuiSetAttributeMessage
		{
			MethodId = MuiNotifyCore.SetMethod,
			Attribute = attribute,
			Value = value,
		};
		var wrote = MuiSetAttributeMessageCodec.Write(ref platform.Classes,
			message, packet);
		var result = wrote ? MuiNativeBoopsiDispatch.DoMethod(
			ref platform.Classes, obj, message) : 0u;
		platform.Classes.Free(message, MuiSetAttributeMessage.Size);
		var observed = MuiNativePublicObjectCore.GetAttribute(
			ref platform.Classes, platform.PublicObjects, platform.OwnerRoot, obj,
			attribute, out var current) && current == value;
		return result != 0 || observed;
	}

	private static APTR NewObject(ref MuiNativeRequesterPlatform platform,
		APTR serviceState, APTR classId, APTR tags)
	{
		return MuiNativePublicObjectCore.NewObject(ref platform.Classes,
			serviceState, platform.OwnerRoot, platform.PublicObjects, classId,
			tags);
	}

	private static bool TryCopyCString(ref MuiNativeRequesterPlatform platform,
		APTR source, uint maximumLength, out APTR copy, out uint copySize)
	{
		copy = APTR.Null;
		copySize = 0;
		if (source.IsNull) return true;
		if (!CStringCodec.TryReadLength(ref platform, source, maximumLength,
			out var length) || length == uint.MaxValue) return false;
		copySize = length + 1;
		copy = platform.Allocate(copySize, MuiHeadlessLayout.AllocationFlags);
		if (copy.IsNull || !platform.IsMapped(copy, copySize) ||
			!platform.IsMapped(source, copySize))
		{
			FreeCopy(ref platform, ref copy, ref copySize);
			return false;
		}
		platform.Copy(source, copy, copySize);
		return true;
	}

	private static void FreeCopy(ref MuiNativeRequesterPlatform platform,
		ref APTR copy, ref uint size)
	{
		if (copy.IsNotNull && size != 0) platform.Free(copy, size);
		copy = APTR.Null;
		size = 0;
	}

	private static bool Cleanup(ref MuiNativeRequesterPlatform platform,
		APTR serviceState,
		MuiNativeRequesterApplicationAdmissionRecord admission,
		ref MuiNativeRequesterApplicationPresenterRecord state)
	{
		if (state.Window.IsNotNull &&
			MuiNativePublicObjectCore.GetAttribute(ref platform.Classes,
				platform.PublicObjects, platform.OwnerRoot, state.Window,
				MuiWindowPublicCore.Open, out var open) && open != 0)
			TrySetObjectAttribute(ref platform, state.Window,
				MuiWindowPublicCore.Open, ClosedRequester);

		// MUI_RequestObjectA consumes the caller's reference after the modal call.
		// Remove that object from the temporary Group first so disposing the
		// requester tree cannot consume the same ownership a second time.
		if (state.RequestObjectAttached != 0)
		{
			if (state.RootGroup.IsNull ||
				!MuiNativeFamilyRemovalCore.TryRemove(ref platform.Classes,
					state.RootGroup, state.RequestObject,
					MuiNativeGuiMode.GroupChildList) ||
				!MuiNativePublicObjectCore.ClearParent(ref platform.Classes,
					platform.OwnerRoot, platform.PublicObjects, state.RootGroup,
					state.RequestObject)) return false;
			state.RequestObjectAttached = 0;
		}

		var windowClosed = state.Window.IsNull ||
			MuiNativePublicObjectCore.GetAttribute(ref platform.Classes,
				platform.PublicObjects, platform.OwnerRoot, state.Window,
				MuiWindowPublicCore.Open, out var closed) && closed == 0;
		if (state.WindowAttached != 0 && windowClosed &&
			MuiNativeRequesterApplicationWindowCore.Detach(ref platform.Classes,
				platform.PublicObjects, platform.OwnerRoot,
				admission.Application, state.Window)) state.WindowAttached = 0;
		if (state.Window.IsNotNull && state.WindowAttached == 0 && windowClosed)
			TryDisposeIfUnparented(ref platform, serviceState, state.Window);

		if (state.RootGroup.IsNotNull)
			TryDisposeIfUnparented(ref platform, serviceState, state.RootGroup);
		if (state.ButtonGroup.IsNotNull)
			TryDisposeIfUnparented(ref platform, serviceState, state.ButtonGroup);
		if (state.Text.IsNotNull)
			TryDisposeIfUnparented(ref platform, serviceState, state.Text);
		for (var index = 0u; index < state.Gadgets.ButtonCount; index++)
		{
			if (MuiRequesterApplicationButtonArrayCodec.TryRead(ref platform,
				state.Gadgets.Buttons, state.Gadgets.ButtonCount, index,
				out var button) && button.Object.IsNotNull)
				TryDisposeIfUnparented(ref platform, serviceState, button.Object);
		}

		return NoLiveObject(ref platform, state.Window) &&
			NoLiveObject(ref platform, state.RootGroup) &&
			NoLiveObject(ref platform, state.ButtonGroup) &&
			NoLiveObject(ref platform, state.Text) &&
			NoLiveButtons(ref platform, state.Gadgets);
	}

	private static void TryDisposeIfUnparented(
		ref MuiNativeRequesterPlatform platform, APTR serviceState, APTR obj)
	{
		var memory = default(MuiNativeClassMemory);
		if (MuiNativePublicObjectCore.TryFindBinding(ref memory,
			platform.PublicObjects, platform.OwnerRoot, obj, out var binding) &&
			binding.DisposeState == MuiNativePublicObjectBinding.StateLive &&
			binding.Parent.IsNull)
			MuiNativePublicObjectCore.DisposeObject(ref platform.Classes,
				serviceState, platform.OwnerRoot, platform.PublicObjects, obj);
	}

	private static bool NoLiveObject(ref MuiNativeRequesterPlatform platform,
		APTR obj)
	{
		if (obj.IsNull) return true;
		var memory = default(MuiNativeClassMemory);
		return !MuiNativePublicObjectCore.TryFindBinding(ref memory,
			platform.PublicObjects, platform.OwnerRoot, obj, out var binding) ||
			binding.DisposeState != MuiNativePublicObjectBinding.StateLive;
	}

	private static bool NoLiveButtons(ref MuiNativeRequesterPlatform platform,
		MuiRequesterApplicationGadgetPlanRecord gadgets)
	{
		for (var index = 0u; index < gadgets.ButtonCount; index++)
		{
			if (!MuiRequesterApplicationButtonArrayCodec.TryRead(ref platform,
				gadgets.Buttons, gadgets.ButtonCount, index, out var button))
				return false;
			if (!NoLiveObject(ref platform, button.Object)) return false;
		}
		return true;
	}
}
