/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Native application-sleep projection used by synchronous online help. The
// counters stay in the existing named native attribute records, while each
// transition uses the shared typed sleep record for admission and arithmetic.
internal struct MuiNativeApplicationSleepScope
{
	internal APTR BusyPointerTags;
}

internal static class MuiNativeApplicationSleep
{
	private const uint WindowSleepDepthAttribute = 0x7FFE003D;
	private const uint WindowSleepSavedDisabledAttribute = 0x7FFE003E;
	private const uint ApplicationSleepDepthAttribute = 0x7FFE003F;
	private const uint WindowSleepWidthAttribute = 0x7FFE0040;
	private const uint WindowSleepHeightAttribute = 0x7FFE0041;
	private const uint DisabledAttribute = MuiCommonControlCore.Disabled;
	private const uint ApplicationSleepAttribute =
		MuiApplicationWindowCore.ApplicationSleep;
	private const uint ApplicationClassNameHash = 0xC243A52E;
	private const uint WindowClassNameHash = 0x61DACF36;
	private const uint AreaClassNameHash = 0x0D917027;
	private const uint MaximumClassDepth = 64;
	private const uint MaximumTagBytes = 2 * MuiAslTagItemRecord.Size;
	private const uint MaximumMessagesPerWindow =
		MuiHeadlessLayout.MaximumTraversal;

	internal static bool TryChangeApplicationSleep(
		ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR application, bool sleep,
		ref MuiNativeApplicationSleepScope scope, bool notifyRequest = false)
	{
		var memory = default(MuiNativeClassMemory);
		if (publicObjects.IsNull || ownerRoot.IsNull || application.IsNull ||
			!MuiNativePublicObjectRegistryCodec.TryRead(ref memory,
				publicObjects, out var registry) ||
			!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, application, out var appBinding) ||
			!TryClassIsOrDerives(ref memory, appBinding.Class,
				ApplicationClassNameHash, out var isApplication) ||
			!isApplication ||
			!TryReadSleepState(ref memory, appBinding.Sidecar,
				ApplicationSleepDepthAttribute, 0, ApplicationSleepAttribute,
				out var applicationSleep)) return false;

		if (!sleep && applicationSleep.Depth == 0) return true;
		if (sleep && applicationSleep.Depth == uint.MaxValue) return false;
		var previousApplicationSleep = applicationSleep;
		var nextApplicationDepth = sleep ? applicationSleep.Depth + 1 :
			applicationSleep.Depth - 1;

		if (!TryEnsureBusyPointerTags(ref platform, ref scope, sleep))
		{
			if (sleep) Release(ref platform, ref scope);
			return false;
		}

		if (!TryEnsureSleepAttributes(ref platform, appBinding.Sidecar,
			ApplicationSleepDepthAttribute, 0, ApplicationSleepAttribute,
			applicationSleep) || !TryPrepareChildWindows(ref platform,
				publicObjects, ownerRoot, application, registry.Head))
		{
			if (sleep) Release(ref platform, ref scope);
			return false;
		}

		var changedWindows = 0u;
		var current = registry.Head;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, current,
				out var binding) || binding.Signature !=
				MuiNativePublicObjectBinding.Magic || binding.OwnerRoot != ownerRoot)
			{
				RollbackChildWindows(ref platform, publicObjects, ownerRoot,
					application, registry.Head, changedWindows, !sleep,
					ref scope, false);
				if (sleep) Release(ref platform, ref scope);
				return false;
			}
			var next = binding.Next;
			if (binding.DisposeState == MuiNativePublicObjectBinding.StateLive &&
				binding.Parent == application)
			{
				if (!TryClassIsOrDerives(ref memory, binding.Class,
					WindowClassNameHash, out var isWindow))
				{
					RollbackChildWindows(ref platform, publicObjects, ownerRoot,
						application, registry.Head, changedWindows, !sleep,
						ref scope, false);
					if (sleep) Release(ref platform, ref scope);
					return false;
				}
				if (isWindow)
				{
					if (!MuiNativePublicObjectCore.TryFindLive(ref platform,
						publicObjects, ownerRoot, binding.Object, out var liveWindow) ||
						!TryChangeWindowSleep(ref platform, liveWindow.Object,
							liveWindow.Sidecar, sleep, ref scope,
							out var changed))
					{
						RollbackChildWindows(ref platform, publicObjects, ownerRoot,
							application, registry.Head, changedWindows, !sleep,
							ref scope, false);
						if (sleep) Release(ref platform, ref scope);
						return false;
					}
					if (changed) changedWindows++;
				}
			}
			else if (binding.DisposeState !=
				MuiNativePublicObjectBinding.StateNativeDisposed &&
				binding.DisposeState !=
				MuiNativePublicObjectBinding.StateLeaseReleased)
			{
				RollbackChildWindows(ref platform, publicObjects, ownerRoot,
					application, registry.Head, changedWindows, !sleep,
					ref scope, false);
				if (sleep) Release(ref platform, ref scope);
				return false;
			}
			current = next;
		}
		if (current.IsNotNull)
		{
			RollbackChildWindows(ref platform, publicObjects, ownerRoot,
				application, registry.Head, changedWindows, !sleep,
					ref scope, false);
			if (sleep) Release(ref platform, ref scope);
			return false;
		}

		applicationSleep.Depth = nextApplicationDepth;
		applicationSleep.Request = nextApplicationDepth;
		if (!WriteSleepState(ref platform, appBinding.Sidecar,
			ApplicationSleepDepthAttribute, 0, ApplicationSleepAttribute,
			applicationSleep, notifyRequest))
		{
			WriteSleepState(ref platform, appBinding.Sidecar,
				ApplicationSleepDepthAttribute, 0, ApplicationSleepAttribute,
				previousApplicationSleep);
			RollbackChildWindows(ref platform, publicObjects, ownerRoot,
				application, registry.Head, changedWindows, !sleep,
				ref scope, false);
			if (sleep) Release(ref platform, ref scope);
			return false;
		}
		return true;
	}

	internal static bool TryReadWindowSleepDepth<TMemory>(ref TMemory memory,
		APTR sidecar, out uint depth)
		where TMemory : struct, IMuiGuestMemory
	{
		depth = 0;
		if (!TryReadSleepState(ref memory, sidecar, WindowSleepDepthAttribute,
			WindowSleepSavedDisabledAttribute, MuiWindowPublicCore.Sleep,
			out var sleep)) return false;
		depth = sleep.Depth;
		return true;
	}

	internal static bool TrySetApplicationSleepValue(
		ref MuiNativeClassPlatform platform, APTR publicObjects, APTR ownerRoot,
		APTR application, uint value, bool notify)
	{
		var scope = default(MuiNativeApplicationSleepScope);
		var changed = TryChangeApplicationSleep(ref platform, publicObjects,
			ownerRoot, application, value != 0, ref scope, notify);
		Release(ref platform, ref scope);
		return changed;
	}

	internal static bool TrySetWindowSleepValue(
		ref MuiNativeClassPlatform platform, APTR publicObjects, APTR ownerRoot,
		APTR windowObject, uint value, bool notify)
	{
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
			ownerRoot, windowObject, out var binding) ||
			!TryClassIsOrDerives(ref memory, binding.Class, WindowClassNameHash,
				out var isWindow) || !isWindow) return false;

		var scope = default(MuiNativeApplicationSleepScope);
		if (!TryEnsureBusyPointerTags(ref platform, ref scope, value != 0))
		{
			Release(ref platform, ref scope);
			return false;
		}
		var changed = TryChangeWindowSleep(ref platform, windowObject,
			binding.Sidecar, value != 0, ref scope, out _, true, notify);
		Release(ref platform, ref scope);
		return changed;
	}

	internal static bool TryApplyInitialSleepAttributes(
		ref MuiNativeClassPlatform platform, APTR publicObjects, APTR ownerRoot,
		APTR obj, APTR tags)
	{
		if (tags.IsNull) return true;
		if (!TryGetLastTagValue(ref platform, tags, ApplicationSleepAttribute,
			out var hasApplicationSleep, out var applicationSleep) ||
			!TryGetLastTagValue(ref platform, tags, MuiWindowPublicCore.Sleep,
				out var hasWindowSleep, out var windowSleep)) return false;
		if (!hasApplicationSleep && !hasWindowSleep) return true;

		var memory = default(MuiNativeClassMemory);
		if (!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
			ownerRoot, obj, out var binding) ||
			!TryClassIsOrDerives(ref memory, binding.Class,
				ApplicationClassNameHash, out var isApplication) ||
			!TryClassIsOrDerives(ref memory, binding.Class, WindowClassNameHash,
				out var isWindow)) return false;
		if (isApplication && isWindow) return false;

		if (isApplication && hasApplicationSleep && applicationSleep != 0)
		{
			if (!MuiNativeObjectStateCore.SetAttribute(ref platform,
				binding.Sidecar, ApplicationSleepAttribute, 0, false)) return false;
			return TrySetApplicationSleepValue(ref platform, publicObjects,
				ownerRoot, obj, applicationSleep, false);
		}
		if (isWindow && hasWindowSleep && windowSleep != 0)
		{
			if (!MuiNativeObjectStateCore.SetAttribute(ref platform,
				binding.Sidecar, MuiWindowPublicCore.Sleep, 0, false)) return false;
			return TrySetWindowSleepValue(ref platform, publicObjects, ownerRoot,
				obj, windowSleep, false);
		}
		return true;
	}

	internal static bool TryGetLastTagValue<TMemory>(ref TMemory memory,
		APTR tags, uint attribute, out bool found, out uint value)
		where TMemory : struct, IMuiGuestMemory
	{
		found = false;
		value = 0;
		if (tags.IsNull) return true;
		var cursor = default(MuiAslTagItemCursor);
		cursor.Base = tags;
		uint visited = 0;
		while (cursor.Base.IsNotNull && visited++ <
			MuiAslTagListCore.MaximumSteps)
		{
			if (!MuiAslTagItemVectorCodec.TryRead(ref memory, cursor,
				out var item)) return false;
			if (item.Tag == MuiAslTagListCore.TagDone) return true;
			if (item.Tag == MuiAslTagListCore.TagIgnore)
			{
				if (!MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1))
					return false;
				continue;
			}
			if (item.Tag == MuiAslTagListCore.TagMore)
			{
				if (item.Data == 0) return true;
				cursor.Base = APTR.FromPointer(item.Data);
				cursor.Index = 0;
				continue;
			}
			if (item.Tag == MuiAslTagListCore.TagSkip)
			{
				if (item.Data == uint.MaxValue ||
					!MuiAslTagItemVectorCodec.TryAdvance(ref cursor,
						item.Data + 1u)) return false;
				continue;
			}
			if (item.Tag == attribute)
			{
				found = true;
				value = item.Data;
			}
			if (!MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1))
				return false;
		}
		return false;
	}

	internal static bool TryApplyOpenWindowSleep(
		ref MuiNativeClassPlatform platform, APTR sidecar, APTR nativeWindow)
	{
		if (nativeWindow.IsNull) return true;
		var memory = default(MuiNativeClassMemory);
		if (!memory.IsMapped(nativeWindow, Window.Size) ||
			!TryReadWindowSleepDepth(ref memory, sidecar, out var depth))
			return false;
		if (depth == 0) return true;
		if (!TryCaptureWindowSleepGeometry(ref platform, sidecar, nativeWindow,
			true))
			return false;
		if (!SetAttribute(ref platform, sidecar, DisabledAttribute, 1))
			return false;
		var scope = default(MuiNativeApplicationSleepScope);
		if (!TryEnsureBusyPointerTags(ref platform, ref scope, true))
		{
			Release(ref platform, ref scope);
			return false;
		}
		var applied = SetBusyPointer(ref platform, nativeWindow, true,
			scope.BusyPointerTags);
		Release(ref platform, ref scope);
		return applied;
	}

	internal static bool TryHandleSleepingWindowMessage(
		ref MuiNativeClassPlatform platform, APTR sidecar, APTR nativeWindow,
		APTR intuiMessage) => TryRestoreWindowSleepGeometry(ref platform,
			sidecar, nativeWindow, intuiMessage);

	private static bool TryCaptureWindowSleepGeometry(
		ref MuiNativeClassPlatform platform, APTR sidecar, APTR nativeWindow,
		bool onlyIfMissing = false)
	{
		if (nativeWindow.IsNull) return true;
		if (onlyIfMissing &&
			TryReadAttribute(ref platform, sidecar, WindowSleepWidthAttribute,
				out _, out var widthFound) && widthFound &&
			TryReadAttribute(ref platform, sidecar, WindowSleepHeightAttribute,
				out _, out var heightFound) && heightFound) return true;
		if (!platform.IsMapped(nativeWindow, Window.Size)) return false;
		var window = IntuitionScreenWindowGuestCodec.ReadWindow(ref platform,
			nativeWindow);
		return SetAttribute(ref platform, sidecar, WindowSleepWidthAttribute,
			unchecked((uint)(int)window.Width)) && SetAttribute(ref platform,
			sidecar, WindowSleepHeightAttribute,
			unchecked((uint)(int)window.Height));
	}

	private static bool TryRestoreWindowSleepGeometry(
		ref MuiNativeClassPlatform platform, APTR sidecar, APTR nativeWindow,
		APTR intuiMessage)
	{
		if (nativeWindow.IsNull) return true;
		if (!platform.IsMapped(nativeWindow, Window.Size)) return false;
		if (intuiMessage.IsNotNull)
		{
			var memory = default(MuiNativeClassMemory);
			if (!MuiIntuiMessageCodec.TryReadPointerRecord(ref memory,
				intuiMessage, out var message)) return false;
			if (message.Class != (uint)IDCMPFlags.ChangeWindow ||
				message.Code != (ushort)IDCMPCode.WindowMoveSize) return true;
		}
		if (!TryReadAttribute(ref platform, sidecar, WindowSleepWidthAttribute,
			out var width, out var widthFound) || !widthFound || width == 0 ||
			width > ushort.MaxValue ||
			!TryReadAttribute(ref platform, sidecar,
				WindowSleepHeightAttribute, out var height, out var heightFound) ||
			!heightFound || height == 0 || height > ushort.MaxValue) return false;
		var widthValue = (int)width;
		var heightValue = (int)height;
		var window = IntuitionScreenWindowGuestCodec.ReadWindow(ref platform,
			nativeWindow);
		if ((int)window.Width == widthValue &&
			(int)window.Height == heightValue) return true;
		if (platform.IntuitionBase.IsNull) return false;
		MuiNativeIntuitionCalls.ChangeWindowBox(platform.IntuitionBase,
			nativeWindow, window.LeftEdge, window.TopEdge, widthValue,
			heightValue);
		return true;
	}

	internal static void Release(ref MuiNativeClassPlatform platform,
		ref MuiNativeApplicationSleepScope scope)
	{
		if (scope.BusyPointerTags.IsNotNull)
		{
			platform.Clear(scope.BusyPointerTags, MaximumTagBytes);
			platform.Free(scope.BusyPointerTags, MaximumTagBytes);
		}
		scope = default;
	}

	private static bool TryEnsureBusyPointerTags(
		ref MuiNativeClassPlatform platform,
		ref MuiNativeApplicationSleepScope scope, bool busy)
	{
		if (scope.BusyPointerTags.IsNotNull) return true;
		scope.BusyPointerTags = platform.Allocate(MaximumTagBytes,
			MuiHeadlessLayout.AllocationFlags);
		if (scope.BusyPointerTags.IsNotNull &&
			WriteBusyPointerTag(ref platform, scope.BusyPointerTags, busy))
			return true;
		Release(ref platform, ref scope);
		return false;
	}

	internal static bool TryClassIsOrDerives<TMemory>(ref TMemory memory,
		APTR classPointer, uint classNameHash, out bool matches)
		where TMemory : struct, IMuiGuestMemory
	{
		matches = false;
		if (classPointer.IsNull) return false;
		var current = classPointer;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MaximumClassDepth)
		{
			if (!memory.IsMapped(current, IClass.Size)) return false;
			var classRecord = BOOPSIGuestCodec.ReadClass(ref memory, current);
			if (!classRecord.cl_ID.IsNull)
			{
				if (!TryGetClassNameHash(ref memory, classRecord.cl_ID,
					out var currentHash)) return false;
				if (currentHash == classNameHash)
				{
					matches = true;
					return true;
				}
			}
			current = classRecord.cl_Super;
		}
		return current.IsNull;
	}

	internal static bool TryIsApplicationClass<TMemory>(ref TMemory memory,
		APTR classPointer, out bool matches)
		where TMemory : struct, IMuiGuestMemory =>
		TryClassIsOrDerives(ref memory, classPointer,
			ApplicationClassNameHash, out matches);

	internal static bool TryIsWindowClass<TMemory>(ref TMemory memory,
		APTR classPointer, out bool matches)
		where TMemory : struct, IMuiGuestMemory =>
		TryClassIsOrDerives(ref memory, classPointer, WindowClassNameHash,
			out matches);

	internal static bool TryIsAreaClass<TMemory>(ref TMemory memory,
		APTR classPointer, out bool matches)
		where TMemory : struct, IMuiGuestMemory =>
		TryClassIsOrDerives(ref memory, classPointer, AreaClassNameHash,
			out matches);

	private static bool TryGetClassNameHash<TMemory>(ref TMemory memory,
		APTR className, out uint hash)
		where TMemory : struct, IMuiGuestMemory
	{
		hash = 2166136261u;
		if (className.IsNull) return false;
		var cursor = default(MuiClassNameByteCursor);
		cursor.Name = className;
		for (uint index = 0; index < MuiClassNameByteCursor.MaximumLength;
			index++)
		{
			cursor.Index = index;
			if (!MuiClassNameByteCursorCodec.TryReadByte(ref memory, cursor,
				out var value)) return false;
			if (value == 0) return true;
			if (value >= (byte)'A' && value <= (byte)'Z')
				value = unchecked((byte)(value + 32));
			hash = unchecked((hash ^ value) * 16777619u);
		}
		return false;
	}

	private static bool TryPrepareChildWindows(
		ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR application, APTR firstBinding)
	{
		var memory = default(MuiNativeClassMemory);
		var current = firstBinding;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, current,
				out var binding) || binding.Signature !=
				MuiNativePublicObjectBinding.Magic || binding.OwnerRoot != ownerRoot)
				return false;
			if (binding.DisposeState == MuiNativePublicObjectBinding.StateLive &&
				binding.Parent == application)
			{
				if (!TryClassIsOrDerives(ref memory, binding.Class,
					WindowClassNameHash, out var isWindow)) return false;
				if (isWindow &&
					(!MuiNativePublicObjectCore.TryFindLive(ref platform,
						publicObjects, ownerRoot, binding.Object, out var liveWindow) ||
					 !TryReadSleepState(ref memory, liveWindow.Sidecar,
						WindowSleepDepthAttribute,
						WindowSleepSavedDisabledAttribute,
						MuiWindowPublicCore.Sleep, out var state) ||
					 !TryEnsureSleepAttributes(ref platform, liveWindow.Sidecar,
						WindowSleepDepthAttribute,
						WindowSleepSavedDisabledAttribute,
						MuiWindowPublicCore.Sleep, state) ||
					 !EnsureAttribute(ref platform, liveWindow.Sidecar,
						DisabledAttribute, state.SavedDisabled))) return false;
			}
			else if (binding.DisposeState !=
				MuiNativePublicObjectBinding.StateNativeDisposed &&
				binding.DisposeState !=
				MuiNativePublicObjectBinding.StateLeaseReleased) return false;
			current = binding.Next;
		}
		return current.IsNull;
	}

	private static void RollbackChildWindows(
		ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR application, APTR firstBinding,
		uint changedWindowCount, bool sleep,
		ref MuiNativeApplicationSleepScope scope, bool drainOnWake)
	{
		var memory = default(MuiNativeClassMemory);
		var current = firstBinding;
		var changed = 0u;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal && changed < changedWindowCount)
		{
			if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, current,
				out var binding) || binding.Signature !=
				MuiNativePublicObjectBinding.Magic || binding.OwnerRoot != ownerRoot)
				return;
			if (binding.DisposeState == MuiNativePublicObjectBinding.StateLive &&
				binding.Parent == application)
			{
				if (!TryClassIsOrDerives(ref memory, binding.Class,
					WindowClassNameHash, out var isWindow)) return;
				if (isWindow &&
					MuiNativePublicObjectCore.TryFindLive(ref platform,
						publicObjects, ownerRoot, binding.Object,
						out var liveWindow) &&
					TryChangeWindowSleep(ref platform, liveWindow.Object,
						liveWindow.Sidecar, sleep, ref scope, out var didChange,
						drainOnWake) && didChange) changed++;
			}
			current = binding.Next;
		}
	}

	private static bool TryChangeWindowSleep(
		ref MuiNativeClassPlatform platform, APTR windowObject,
		APTR sidecar, bool sleep,
		ref MuiNativeApplicationSleepScope scope, out bool changed,
		bool drainOnWake = true, bool notifyRequest = false)
	{
		changed = false;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecar,
			out var sidecarRecord) || sidecarRecord.Object != windowObject ||
			!TryClassIsOrDerives(ref memory, sidecarRecord.Class,
				WindowClassNameHash, out var isWindow) || !isWindow ||
			!TryReadSleepState(ref memory, sidecar, WindowSleepDepthAttribute,
			WindowSleepSavedDisabledAttribute, MuiWindowPublicCore.Sleep,
			out var state)) return false;
		if (!sleep && state.Depth == 0) return true;
		if (sleep && state.Depth == uint.MaxValue) return false;
		var previousState = state;
		var previous = state.Depth;
		var next = sleep ? previous + 1 : previous - 1;
		if (next == previous) return true;

		if (sleep && previous == 0)
		{
			if (!TryReadNativeWindow(ref memory, sidecar, out var nativeWindow) ||
				!TryCaptureWindowSleepGeometry(ref platform, sidecar, nativeWindow) ||
				!SetBusyPointer(ref platform, nativeWindow, true,
					scope.BusyPointerTags)) return false;
			state.SavedDisabled = ReadCurrentDisabled(ref memory, sidecar);
			if (!SetAttribute(ref platform, sidecar,
				WindowSleepSavedDisabledAttribute, state.SavedDisabled) ||
				!SetAttribute(ref platform, sidecar, DisabledAttribute, 1))
			{
				SetBusyPointer(ref platform, nativeWindow, false,
					scope.BusyPointerTags);
				return false;
			}
		}
		else if (!sleep && next == 0)
		{
			if (!TryReadNativeWindow(ref memory, sidecar,
				out var nativeWindow)) return false;
			if (drainOnWake && !DrainWindowMessages(ref platform, sidecar,
				nativeWindow)) return false;
			if (!TryRestoreWindowSleepGeometry(ref platform, sidecar,
				nativeWindow, APTR.Null)) return false;
			if (!SetBusyPointer(ref platform, nativeWindow, false,
				scope.BusyPointerTags)) return false;
			if (!SetAttribute(ref platform, sidecar, DisabledAttribute,
				state.SavedDisabled))
			{
				SetBusyPointer(ref platform, nativeWindow, true,
					scope.BusyPointerTags);
				return false;
			}
			if (!SetAttribute(ref platform, sidecar,
				WindowSleepSavedDisabledAttribute, 0))
			{
				SetAttribute(ref platform, sidecar, DisabledAttribute, 1);
				SetBusyPointer(ref platform, nativeWindow, true,
					scope.BusyPointerTags);
				return false;
			}
			state.SavedDisabled = 0;
		}

		state.Depth = next;
		state.Request = next;
		if (!WriteSleepState(ref platform, sidecar, WindowSleepDepthAttribute,
			WindowSleepSavedDisabledAttribute, MuiWindowPublicCore.Sleep, state,
			notifyRequest))
		{
			WriteSleepState(ref platform, sidecar, WindowSleepDepthAttribute,
				WindowSleepSavedDisabledAttribute, MuiWindowPublicCore.Sleep,
				previousState);
			if (sleep && previous == 0)
			{
				SetAttribute(ref platform, sidecar, DisabledAttribute,
					state.SavedDisabled);
				SetAttribute(ref platform, sidecar,
					WindowSleepSavedDisabledAttribute, 0);
				TryReadNativeWindow(ref memory, sidecar, out var nativeWindow);
				SetBusyPointer(ref platform, nativeWindow, false,
					scope.BusyPointerTags);
			}
			else if (!sleep && next == 0)
			{
				SetAttribute(ref platform, sidecar, DisabledAttribute, 1);
				SetAttribute(ref platform, sidecar,
					WindowSleepSavedDisabledAttribute, state.SavedDisabled);
				TryReadNativeWindow(ref memory, sidecar, out var nativeWindow);
				SetBusyPointer(ref platform, nativeWindow, true,
					scope.BusyPointerTags);
			}
			return false;
		}
		changed = true;
		return true;
	}

	private static bool TryReadSleepState<TMemory>(ref TMemory memory,
		APTR sidecar, uint depthAttribute, uint savedDisabledAttribute,
		uint requestAttribute, out MuiSleepStateRecord state)
		where TMemory : struct, IMuiGuestMemory
	{
		state = default;
		var savedDisabled = 0u;
		var savedDisabledFound = false;
		var currentDisabled = 0u;
		if (!TryReadAttribute(ref memory, sidecar, requestAttribute,
			out var request, out var requestFound) ||
			!TryReadAttribute(ref memory, sidecar, depthAttribute,
				out var depth, out var depthFound) ||
			(requestFound && depthFound && request != depth))
			return false;
		if (savedDisabledAttribute != 0 &&
			(!TryReadAttribute(ref memory, sidecar, savedDisabledAttribute,
				out savedDisabled, out savedDisabledFound) ||
			!TryReadAttribute(ref memory, sidecar, DisabledAttribute,
				out currentDisabled, out _) || currentDisabled > 1 ||
			(savedDisabledFound && savedDisabled > 1))) return false;
		state.Magic = MuiSleepStateRecord.Cookie;
		state.Depth = depthFound ? depth : request;
		state.SavedDisabled = savedDisabledAttribute == 0 ? 0 :
			savedDisabledFound ? savedDisabled : currentDisabled;
		state.Request = requestFound ? request : state.Depth;
		return MuiSleepStateAdmission.Validate(state);
	}

	private static bool TryEnsureSleepAttributes(
		ref MuiNativeClassPlatform platform, APTR sidecar,
		uint depthAttribute, uint savedDisabledAttribute,
		uint requestAttribute,
		MuiSleepStateRecord state)
	{
		return EnsureAttribute(ref platform, sidecar, depthAttribute, state.Depth) &&
			(savedDisabledAttribute == 0 || EnsureAttribute(ref platform, sidecar,
				savedDisabledAttribute, state.SavedDisabled)) &&
			EnsureAttribute(ref platform, sidecar, requestAttribute,
				state.Request);
	}

	private static bool EnsureAttribute(ref MuiNativeClassPlatform platform,
		APTR sidecar, uint attribute, uint initialValue)
	{
		var memory = default(MuiNativeClassMemory);
		if (!TryReadAttribute(ref memory, sidecar, attribute, out _,
			out var found)) return false;
		return found || SetAttribute(ref platform, sidecar, attribute,
			initialValue);
	}

	internal static bool TryReadAttribute<TMemory>(ref TMemory memory,
		APTR sidecarAddress, uint attribute, out uint value, out bool found)
		where TMemory : struct, IMuiGuestMemory
	{
		value = 0;
		found = false;
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
			out var sidecar) || sidecar.LifecycleState !=
			MuiNativeMuiObjectRecord.StateLive ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) == 0 ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectDisposing) != 0)
			return false;
		var current = sidecar.Attributes;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativeObjectAttributeCodec.TryRead(ref memory, current,
				out var record)) return false;
			if (record.Attribute == attribute)
			{
				value = record.Value;
				found = true;
				return true;
			}
			current = record.Next;
		}
		return current.IsNull;
	}

	private static bool WriteSleepState(ref MuiNativeClassPlatform platform,
		APTR sidecar, uint depthAttribute, uint savedDisabledAttribute,
		uint requestAttribute, MuiSleepStateRecord state,
		bool notifyRequest = false)
	{
		if (!MuiSleepStateAdmission.Validate(state) ||
			!SetAttribute(ref platform, sidecar, depthAttribute, state.Depth) ||
			(savedDisabledAttribute != 0 &&
			 !SetAttribute(ref platform, sidecar, savedDisabledAttribute,
				state.SavedDisabled))) return false;
		return MuiNativeObjectStateCore.SetAttribute(ref platform, sidecar,
			requestAttribute, state.Request, notifyRequest);
	}

	private static uint ReadCurrentDisabled<TMemory>(ref TMemory memory,
		APTR sidecar)
		where TMemory : struct, IMuiGuestMemory =>
		TryReadAttribute(ref memory, sidecar, DisabledAttribute, out var value,
			out _) ? value : 0;

	private static bool SetAttribute(ref MuiNativeClassPlatform platform,
		APTR sidecar, uint attribute, uint value) =>
		MuiNativeObjectStateCore.SetAttribute(ref platform, sidecar, attribute,
			value, false);

	private static bool TryReadNativeWindow<TMemory>(ref TMemory memory,
		APTR sidecarAddress, out APTR nativeWindow)
		where TMemory : struct, IMuiGuestMemory
	{
		nativeWindow = APTR.Null;
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
			out var sidecar) || sidecar.LifecycleState !=
			MuiNativeMuiObjectRecord.StateLive) return false;
		nativeWindow = sidecar.NativeWindow;
		return nativeWindow.IsNull || memory.IsMapped(nativeWindow, Window.Size);
	}

	private static bool SetBusyPointer(ref MuiNativeClassPlatform platform,
		APTR nativeWindow, bool busy, APTR tags)
	{
		if (nativeWindow.IsNull) return true;
		if (platform.IntuitionBase.IsNull || tags.IsNull ||
			!platform.IsMapped(nativeWindow, Window.Size) ||
			!WriteBusyPointerTag(ref platform, tags, busy)) return false;
		MuiNativeIntuitionCalls.SetWindowPointerA(platform.IntuitionBase,
			nativeWindow, tags);
		return true;
	}

	private static bool WriteBusyPointerTag<TMemory>(ref TMemory memory,
		APTR tags, bool busy)
		where TMemory : struct, IMuiGuestMemory
	{
		var busyPointerTag = default(MuiAslTagItemRecord);
		busyPointerTag.Tag = IntuitionWindowTags.WA_BusyPointer;
		busyPointerTag.Data = busy ? 1u : 0u;
		var terminator = default(MuiAslTagItemRecord);
		terminator.Tag = MuiAslTagListCore.TagDone;
		return MuiAslTagItemVectorCodec.TryWrite(ref memory, tags, 0,
			busyPointerTag) && MuiAslTagItemVectorCodec.TryWrite(ref memory,
				tags, 1, terminator);
	}

	private static bool DrainWindowMessages(
		ref MuiNativeClassPlatform platform, APTR sidecar, APTR nativeWindow)
	{
		if (nativeWindow.IsNull) return true;
		if (!platform.IsMapped(nativeWindow, Window.Size)) return false;
		var window = IntuitionScreenWindowGuestCodec.ReadWindow(ref platform,
			nativeWindow);
		if (window.UserPort.IsNull) return true;
		for (var index = 0u; index < MaximumMessagesPerWindow; index++)
		{
			var message = Exec.GetMsg(window.UserPort);
			if (message.IsNull) return true;
			var restored = TryRestoreWindowSleepGeometry(ref platform, sidecar,
				nativeWindow, message);
			Exec.ReplyMsg(message);
			if (!restored) return false;
		}
		// Probe once to distinguish a drained queue from the bounded-work case.
		// GetMsg transfers message ownership even when this limit was reached,
		// so the probe must be returned to Intuition before reporting that more
		// queued input remains for a later wake attempt.
		var remaining = Exec.GetMsg(window.UserPort);
		if (remaining.IsNull) return true;
		var remainingRestored = TryRestoreWindowSleepGeometry(ref platform,
			sidecar, nativeWindow, remaining);
		Exec.ReplyMsg(remaining);
		if (!remainingRestored) return false;
		return false;
	}
}
