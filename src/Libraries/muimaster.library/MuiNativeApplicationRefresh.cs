/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Native projection for MUIM_Application_CheckRefresh. Intuition owns the
// damage region; BeginRefresh/EndRefresh restrict the normal MUI Draw method
// to that region, so windows without pending damage do not paint their full
// contents. The application child list is copied to a bounded guest-resident
// snapshot before any class Draw callback can mutate the public-object registry.
// The only persistent state is the existing named object sidecar.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeApplicationChildSnapshotRecord
{
	internal const uint Size = 16;
	internal APTR Object;
	internal APTR Sidecar;
	internal APTR Class;
	internal APTR Parent;
}

internal static class MuiNativeApplicationChildSnapshotCodec
{
	internal static bool Write<TMemory>(ref TMemory memory, APTR address,
		MuiNativeApplicationChildSnapshotRecord value)
		where TMemory : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeApplicationChildSnapshotRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Object.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Sidecar.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Class.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Parent.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TMemory>(ref TMemory memory, APTR address,
		out MuiNativeApplicationChildSnapshotRecord value)
		where TMemory : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeApplicationChildSnapshotRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var obj) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var sidecar) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var classPointer) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var parent) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.Object = APTR.FromPointer(obj);
		value.Sidecar = APTR.FromPointer(sidecar);
		value.Class = APTR.FromPointer(classPointer);
		value.Parent = APTR.FromPointer(parent);
		return !value.Object.IsNull && !value.Sidecar.IsNull &&
			!value.Class.IsNull && !value.Parent.IsNull;
	}
}

internal struct MuiNativeApplicationChildSnapshotPlan
{
	internal APTR Records;
	internal APTR RegistryHead;
	internal uint RegistryMutation;
	internal uint Count;
	internal uint ByteSize;
}

internal static class MuiNativeApplicationRefreshCore
{
	private const uint DrawObject = MuiNativeRedrawMessage.DrawObjectFlag;
	private const uint WindowOpenAttribute = MuiWindowPublicCore.Open;
	private const uint WindowRootObjectAttribute = MuiWindowPublicCore.RootObject;

	internal static bool TryBuildChildSnapshot<TPlatform>(ref TPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR application,
		out MuiNativeApplicationChildSnapshotPlan plan)
		where TPlatform : struct, IMuiGuestMemory, IMuiAllocationCapability
	{
		plan = default;
		if (!TryCountApplicationChildren(ref platform, publicObjects, ownerRoot,
			application, out var count, out var registry)) return false;
		plan.RegistryHead = registry.Head;
		plan.RegistryMutation = registry.Mutation;
		plan.Count = count;
		if (count == 0) return true;
		if (count > uint.MaxValue / MuiNativeApplicationChildSnapshotRecord.Size)
		{
			plan = default;
			return false;
		}
		plan.ByteSize = count * MuiNativeApplicationChildSnapshotRecord.Size;
		plan.Records = platform.Allocate(plan.ByteSize,
			MuiHeadlessLayout.AllocationFlags);
		if (plan.Records.IsNull || !platform.IsMapped(plan.Records,
			plan.ByteSize))
		{
			if (plan.Records.IsNotNull) platform.Free(plan.Records,
				plan.ByteSize);
			plan = default;
			return false;
		}
		if (!TryWriteChildSnapshot(ref platform, publicObjects, ownerRoot,
			application, registry, plan.Records, count))
		{
			platform.Free(plan.Records, plan.ByteSize);
			plan = default;
			return false;
		}
		return true;
	}

	internal static void ReleaseChildSnapshot<TPlatform>(ref TPlatform platform,
		ref MuiNativeApplicationChildSnapshotPlan plan)
		where TPlatform : struct, IMuiAllocationCapability
	{
		if (plan.Records.IsNotNull) platform.Free(plan.Records, plan.ByteSize);
		plan = default;
	}

	private static bool TryCountApplicationChildren<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR application, out uint count,
		out MuiNativePublicObjectRegistryRecord registry)
		where TMemory : struct, IMuiGuestMemory
	{
		count = 0;
		registry = default;
		if (publicObjects.IsNull || ownerRoot.IsNull || application.IsNull ||
			!MuiNativePublicObjectRegistryCodec.TryRead(ref memory, publicObjects,
				out registry)) return false;
		var expected = registry;
		var current = registry.Head;
		var visited = 0u;
		while (current.IsNotNull && visited < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!TryReadRegistryBinding(ref memory, current, ownerRoot,
				out var binding)) return false;
			if (binding.DisposeState ==
				MuiNativePublicObjectBinding.StateLive &&
				binding.Parent == application)
			{
				if (count == uint.MaxValue) return false;
				count++;
			}
			current = binding.Next;
			visited++;
		}
		return current.IsNull && TryRegistryUnchanged(ref memory, publicObjects,
			expected);
	}

	private static bool TryWriteChildSnapshot<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR application,
		MuiNativePublicObjectRegistryRecord expectedRegistry, APTR records,
		uint expectedCount)
		where TMemory : struct, IMuiGuestMemory
	{
		if (records.IsNull || expectedCount == 0 || expectedCount >
			uint.MaxValue / MuiNativeApplicationChildSnapshotRecord.Size ||
			!TryRegistryUnchanged(ref memory, publicObjects, expectedRegistry))
			return false;
		var byteSize = expectedCount *
			MuiNativeApplicationChildSnapshotRecord.Size;
		if (!MuiGuestStructCursor.TryCreate(ref memory, records, byteSize,
			out var cursor)) return false;
		var current = expectedRegistry.Head;
		var visited = 0u;
		var captured = 0u;
		while (current.IsNotNull && visited < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!TryReadRegistryBinding(ref memory, current, ownerRoot,
				out var binding)) return false;
			if (binding.DisposeState ==
				MuiNativePublicObjectBinding.StateLive &&
				binding.Parent == application)
			{
				if (captured >= expectedCount ||
					!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
						MuiNativeApplicationChildSnapshotRecord.Size,
						out var recordAddress)) return false;
				var snapshot = new MuiNativeApplicationChildSnapshotRecord
				{
					Object = binding.Object,
					Sidecar = binding.Sidecar,
					Class = binding.Class,
					Parent = binding.Parent,
				};
				if (!MuiNativeApplicationChildSnapshotCodec.Write(ref memory,
					recordAddress, snapshot)) return false;
				captured++;
			}
			current = binding.Next;
			visited++;
		}
		return current.IsNull && captured == expectedCount &&
			MuiGuestStructCursor.IsComplete(cursor) &&
			TryRegistryUnchanged(ref memory, publicObjects, expectedRegistry);
	}

	private static bool TryReadRegistryBinding<TMemory>(ref TMemory memory,
		APTR address, APTR ownerRoot,
		out MuiNativePublicObjectBinding binding)
		where TMemory : struct, IMuiGuestMemory
	{
		binding = default;
		if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, address,
			out binding) || binding.Signature !=
			MuiNativePublicObjectBinding.Magic || binding.OwnerRoot != ownerRoot)
			return false;
		if (binding.DisposeState == MuiNativePublicObjectBinding.StateLive)
			return !binding.Object.IsNull && !binding.Class.IsNull &&
				!binding.Sidecar.IsNull;
		return binding.DisposeState ==
			MuiNativePublicObjectBinding.StateNativeDisposed ||
			binding.DisposeState ==
				MuiNativePublicObjectBinding.StateLeaseReleased;
	}

	private static bool TryRegistryUnchanged<TMemory>(ref TMemory memory,
		APTR publicObjects, MuiNativePublicObjectRegistryRecord expected)
		where TMemory : struct, IMuiGuestMemory =>
		MuiNativePublicObjectRegistryCodec.TryRead(ref memory, publicObjects,
			out var current) && current.Head == expected.Head &&
			current.Mutation == expected.Mutation;

	private static bool TryValidateRegistry<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot,
		MuiNativePublicObjectRegistryRecord expected)
		where TMemory : struct, IMuiGuestMemory
	{
		if (!TryRegistryUnchanged(ref memory, publicObjects, expected))
			return false;
		var current = expected.Head;
		var visited = 0u;
		while (current.IsNotNull && visited < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!TryReadRegistryBinding(ref memory, current, ownerRoot,
				out var binding)) return false;
			current = binding.Next;
			visited++;
		}
		return current.IsNull && TryRegistryUnchanged(ref memory, publicObjects,
			expected);
	}

	// Resolve one immutable snapshot entry against the current registry after a
	// possible class callback. The mutation generation distinguishes a normal
	// sibling removal from registry corruption; the snapshot cursor itself never
	// follows a callback-owned Next link.
	internal static bool TryResolveSnapshotEntry<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR application,
		MuiNativeApplicationChildSnapshotRecord snapshot,
		ref uint observedMutation, out MuiNativePublicObjectBinding binding,
		out bool skip)
		where TMemory : struct, IMuiGuestMemory
	{
		binding = default;
		skip = false;
		if (snapshot.Object.IsNull || snapshot.Sidecar.IsNull ||
			snapshot.Class.IsNull || snapshot.Parent != application ||
			!MuiNativePublicObjectRegistryCodec.TryRead(ref memory, publicObjects,
				out var currentRegistry)) return false;

		var registryChanged = currentRegistry.Mutation != observedMutation;
		if (registryChanged)
		{
			if (!TryValidateRegistry(ref memory, publicObjects, ownerRoot,
				currentRegistry)) return false;
			observedMutation = currentRegistry.Mutation;
		}
		if (!MuiNativePublicObjectCore.TryFindBinding(ref memory, publicObjects,
			ownerRoot, snapshot.Object, out binding))
		{
			if (registryChanged)
			{
				skip = true;
				return true;
			}
			return false;
		}
		if (binding.DisposeState != MuiNativePublicObjectBinding.StateLive ||
			binding.Parent != snapshot.Parent ||
			binding.Sidecar != snapshot.Sidecar ||
			binding.Class != snapshot.Class)
		{
			if (registryChanged)
			{
				skip = true;
				return true;
			}
			return false;
		}
		return MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
			ownerRoot, snapshot.Object, out binding) &&
			binding.Parent == snapshot.Parent &&
			binding.Sidecar == snapshot.Sidecar &&
			binding.Class == snapshot.Class;
	}

	internal static bool TryCheck(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR application, APTR message,
		ref uint result)
	{
		var memory = default(MuiNativeClassMemory);
		if (!MuiApplicationCheckRefreshMessageCodec.TryRead(ref memory, message,
			out var packet) || packet.MethodId !=
				MuiApplicationDispatcher.ApplicationCheckRefreshMethod ||
			!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, application, out var applicationBinding) ||
			!MuiNativeApplicationSleep.TryIsApplicationClass(ref memory,
				applicationBinding.Class, out var isApplication) || !isApplication ||
			!MuiNativeClassOwnerCodec.TryRead(ref memory, platform.Context,
				out var owner) || owner.LibraryBase.IsNull ||
			platform.IntuitionBase.IsNull) return false;

		var refreshedWindows = 0u;
		if (!TryBuildChildSnapshot(ref platform, publicObjects, ownerRoot,
			application, out var snapshotPlan)) return false;
		var snapshotSucceeded = true;
		var observedMutation = snapshotPlan.RegistryMutation;
		if (snapshotPlan.Count != 0)
		{
			if (!MuiGuestStructCursor.TryCreate(ref platform,
				snapshotPlan.Records, snapshotPlan.ByteSize, out var snapshotCursor))
				snapshotSucceeded = false;
			for (var index = 0u; snapshotSucceeded &&
				index < snapshotPlan.Count; index++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform,
					ref snapshotCursor,
					MuiNativeApplicationChildSnapshotRecord.Size,
					out var snapshotAddress) ||
					!MuiNativeApplicationChildSnapshotCodec.TryRead(ref platform,
						snapshotAddress, out var snapshot))
				{
					snapshotSucceeded = false;
					break;
				}
				if (!TryResolveSnapshotEntry(ref memory, publicObjects,
					ownerRoot, application, snapshot, ref observedMutation,
					out var binding, out var skip))
				{
					snapshotSucceeded = false;
					break;
				}
				if (skip) continue;
				if (!MuiNativeApplicationSleep.TryIsWindowClass(ref memory,
					binding.Class, out var isWindow) ||
				(isWindow && !TryRefreshWindow(ref platform, publicObjects,
					ownerRoot, owner.LibraryBase, binding, ref refreshedWindows)))
				{
					snapshotSucceeded = false;
					break;
				}
			}
			if (snapshotSucceeded &&
				!MuiGuestStructCursor.IsComplete(snapshotCursor))
				snapshotSucceeded = false;
		}
		ReleaseChildSnapshot(ref platform, ref snapshotPlan);
		if (!snapshotSucceeded ||
			!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, application, out var liveApplication) ||
			liveApplication.Sidecar != applicationBinding.Sidecar)
			return false;

		var checks = 0u;
		MuiNativeObjectStateCore.TryGetAttribute(ref memory,
			applicationBinding.Sidecar,
			MuiApplicationWindowCore.ApplicationRefreshChecks, out checks);
		if (checks != uint.MaxValue) checks++;
		if (!MuiNativeObjectStateCore.SetAttribute(ref platform,
			applicationBinding.Sidecar,
			MuiApplicationWindowCore.ApplicationRefreshChecks, checks, false) ||
			!MuiNativeObjectStateCore.SetAttribute(ref platform,
				applicationBinding.Sidecar,
				MuiApplicationWindowCore.ApplicationRefreshWindows,
				refreshedWindows, false)) return false;

		// The MorphOS method's result is explicitly undefined. Return an accepted
		// boundary value after processing; callers must not rely on its value.
		result = 1;
		return true;
	}

	private static bool TryRefreshWindow(
		ref MuiNativeClassPlatform platform, APTR publicObjects, APTR ownerRoot,
		APTR muiLibrary, MuiNativePublicObjectBinding binding,
		ref uint refreshedWindows)
	{
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
			ownerRoot, binding.Object, out var liveWindow) ||
			liveWindow.Sidecar != binding.Sidecar ||
			!MuiNativeMuiObjectCodec.TryRead(ref memory, binding.Sidecar,
				out var sidecar) || sidecar.Object != binding.Object ||
			sidecar.Class != binding.Class || sidecar.OwnerRoot != ownerRoot ||
			sidecar.Parent != binding.Parent ||
			sidecar.LifecycleState != MuiNativeMuiObjectRecord.StateLive ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) == 0 ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectDisposing) != 0)
			return false;

		var open = 0u;
		if (!MuiNativeApplicationSleep.TryReadAttribute(ref memory,
			binding.Sidecar, WindowOpenAttribute, out open, out var hasOpen))
			return false;
		if (!hasOpen || open == 0)
			return true;
		var nativeWindow = sidecar.NativeWindow;
		if (nativeWindow.IsNull)
		{
			nativeWindow = MuiNativePublicObjectCore.ReadNativeWindow(ref platform,
				publicObjects, ownerRoot, binding.Object);
			if (nativeWindow.IsNull) return true;
			// GetAttr is a callback boundary. Re-admit the binding, open state, and
			// cached native Window before retaining the returned pointer.
			if (!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, binding.Object, out liveWindow) ||
				liveWindow.Sidecar != binding.Sidecar ||
				!MuiNativeMuiObjectCodec.TryRead(ref memory, binding.Sidecar,
					out sidecar) || sidecar.NativeWindow != nativeWindow ||
				!MuiNativeApplicationSleep.TryReadAttribute(ref memory,
					binding.Sidecar, WindowOpenAttribute, out open, out hasOpen) ||
				!hasOpen || open == 0)
				return true;
		}
		if (!memory.IsMapped(nativeWindow, Window.Size)) return false;
		var nativeWindowRecord = IntuitionScreenWindowGuestCodec.ReadWindow(
			ref memory, nativeWindow);
		var layer = nativeWindowRecord.Layer;
		if (layer.IsNull || !memory.IsMapped(layer, Layer.Size)) return false;
		var layerFlags = LayersLayerCodec.ReadFlags(ref memory, layer);
		if ((layerFlags & LayerFlags.Refresh) == 0) return true;

		var rootObject = APTR.Null;
		if (!MuiNativeApplicationSleep.TryReadAttribute(ref memory,
			binding.Sidecar, WindowRootObjectAttribute, out var rootRaw,
			out var hasRoot)) return false;
		if (hasRoot) rootObject = APTR.FromPointer(rootRaw);

		var target = default(MuiNativeRedrawTarget);
		var renderBinding = default(MuiNativeRedrawRenderBinding);
		var redrawMessage = APTR.Null;
		if (rootObject.IsNotNull)
		{
			if (!MuiNativePublicObjectCore.TryFindLive(ref platform,
				publicObjects, ownerRoot, rootObject, out var rootBinding))
				return false;
			// The CheckRefresh route invokes MUIM_Draw directly inside the
			// window's Intuition refresh bracket. Keep the same render-info/RastPort
			// admission as MUI_Redraw so this alternate route cannot bypass it.
			if (!MuiNativeRedrawServiceCore.TryReadRenderBinding(ref memory,
				rootObject, rootBinding.Sidecar, out renderBinding)) return true;
			if (!MuiNativeRedrawServiceCore.TryResolveTarget(ref platform,
				publicObjects, ownerRoot, muiLibrary, rootObject, DrawObject,
				out target)) return false;
			redrawMessage = platform.Allocate(MuiNativeRedrawMessage.Size,
				MuiHeadlessLayout.AllocationFlags);
			if (redrawMessage.IsNull || !memory.IsMapped(redrawMessage,
				MuiNativeRedrawMessage.Size))
			{
				if (redrawMessage.IsNotNull) platform.Free(redrawMessage,
					MuiNativeRedrawMessage.Size);
				return false;
			}
		}

		// Allocate and resolve the draw call before BeginRefresh so even the
		// failure path cannot strand Intuition's layer-update state.
		MuiNativeIntuitionCalls.BeginRefresh(platform.IntuitionBase,
			nativeWindow);
		var redrawn = rootObject.IsNull ||
			MuiNativeRedrawServiceCore.TryValidateRenderBinding(ref memory,
				publicObjects, ownerRoot, rootObject, renderBinding) &&
			MuiNativeRedrawServiceCore.Invoke(target, redrawMessage, DrawObject);
		MuiNativeIntuitionCalls.EndRefresh(platform.IntuitionBase,
			nativeWindow, redrawn);
		if (redrawMessage.IsNotNull)
			platform.Free(redrawMessage, MuiNativeRedrawMessage.Size);
		if (!redrawn) return false;
		if (refreshedWindows != uint.MaxValue) refreshedWindows++;
		return true;
	}
}
