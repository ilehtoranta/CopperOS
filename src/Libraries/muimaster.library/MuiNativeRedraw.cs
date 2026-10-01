/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using System.Runtime.InteropServices;
using MuiConstants = Amiga.MUI.MUIConstants;

namespace CopperOS.MuiMaster;

// MorphOS MUIM_Draw is a declaration-ordered {MethodID, flags} message. Keep
// this native helper independent from collection-specific message records: a
// redraw request is a public muimaster operation, not a List method packet.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeRedrawMessage
{
	internal const uint Size = 8;
	internal const uint DrawMethodId = MuiConstants.MUIM_Draw;
	internal const uint DrawObjectFlag = MuiConstants.MADF_DRAWOBJECT;
	internal const uint DrawUpdateFlag = MuiConstants.MADF_DRAWUPDATE;
	internal const uint AllowedFlags = DrawObjectFlag | DrawUpdateFlag;
	internal uint MethodId;
	internal uint Flags;
}

internal static class MuiNativeRedrawMessageCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiNativeRedrawMessage value) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNativeRedrawMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiNativeRedrawMessage value) where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNativeRedrawMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		return value.MethodId == MuiNativeRedrawMessage.DrawMethodId;
	}
}

// The only raw call in the native redraw path is the ABI dispatcher entry.
// Message layout and ownership stay in the named record above; this bridge
// merely supplies the BOOPSI A0/A1/A2/A6 register contract.
internal static class MuiNativeRedrawCalls
{
	internal static uint Dispatch(APTR entry, APTR cls, APTR obj, APTR message,
		APTR libraryBase) => DispatchCall(entry, cls, obj, message, libraryBase);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint DispatchCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A0)] APTR cls,
		[M68kRegister(M68kRegister.A2)] APTR obj,
		[M68kRegister(M68kRegister.A1)] APTR message,
		[M68kRegister(M68kRegister.A6)] APTR libraryBase);
}

// Resolved class dispatch identity for one live MUI object. This is transient
// call state, not a guest ABI record; persistent pointers remain in the named
// public-object and class-lease records.
internal struct MuiNativeRedrawTarget
{
	internal APTR Object;
	internal APTR Class;
	internal APTR Entry;
	internal APTR LibraryBase;
}

// One item in the target-to-root path used to validate redraw clipping across
// GetAttr callbacks. Store the public object and its library-owned sidecar as
// named pointer fields; never infer either value from a native object offset.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeRedrawAncestorRecord
{
	internal const uint Size = 8;
	internal APTR Object;
	internal APTR Sidecar;
}

internal static class MuiNativeRedrawAncestorCodec
{
	internal static bool Write<TMemory>(ref TMemory memory, APTR address,
		MuiNativeRedrawAncestorRecord value)
		where TMemory : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeRedrawAncestorRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Object.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Sidecar.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TMemory>(ref TMemory memory, APTR address,
		out MuiNativeRedrawAncestorRecord value)
		where TMemory : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeRedrawAncestorRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var obj) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var sidecar) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.Object = APTR.FromPointer(obj);
		value.Sidecar = APTR.FromPointer(sidecar);
		return true;
	}
}

// Transient rendering identity for one MUI_Redraw call. Retaining each named
// link lets the caller detect a changed public render target after re-entrant
// attribute queries without inferring identity from a native object offset.
internal struct MuiNativeRedrawLayerBinding
{
	internal APTR Object;
	internal APTR Sidecar;
	internal APTR RenderInfo;
	internal APTR RastPort;
	internal APTR Layer;
}

// MorphOS MUI_Redraw must not enter MUIM_Draw without both the live
// MUI_RenderInfo and its RastPort. Keep this prerequisite separate from the
// optional Layer needed only when a virtual-group clip is pushed.
internal struct MuiNativeRedrawRenderBinding
{
	internal APTR Object;
	internal APTR Sidecar;
	internal APTR RenderInfo;
	internal APTR RastPort;
}

// HasClip and DrawEligible are canonical ULONGs so this transient plan crosses
// the freestanding test seam without relying on managed boolean layout.
internal struct MuiNativeRedrawClipPlan
{
	internal MuiNativeAreaRectangle Bounds;
	internal uint HasClip;
	internal uint DrawEligible;
	internal MuiNativeRedrawRenderBinding RenderBinding;
	internal MuiNativeRedrawLayerBinding LayerBinding;
}

// Accumulate one virtual-group content rectangle into the named redraw plan.
// Coordinates stay signed pixel rectangles; zero-area or disjoint viewports
// suppress drawing, while malformed negative extents fail closed.
internal static class MuiNativeRedrawClipPlanCore
{
	internal static bool IncludeViewport(ref MuiNativeRedrawClipPlan plan,
		MuiNativeAreaRectangle viewport)
	{
		if (viewport.Width < 0 || viewport.Height < 0) return false;
		var hadClip = plan.HasClip != 0;
		plan.HasClip = 1;
		if (plan.DrawEligible == 0) return true;
		if (viewport.Width == 0 || viewport.Height == 0)
		{
			plan.DrawEligible = 0;
			return true;
		}
		if (!hadClip)
		{
			plan.Bounds = viewport;
			return true;
		}
		if (MuiNativeGuiMode.TryIntersectRectangles(plan.Bounds, viewport,
			out var intersection))
		{
			plan.Bounds = intersection;
			return true;
		}
		plan.DrawEligible = 0;
		return true;
	}
}

// Native MUI_Redraw is an immediate dispatch slice. The object must be live in
// the library-owned registry; a named ancestor snapshot stabilizes virtual-
// group clipping across GetAttr callbacks, and the draw message is dispatched
// through the object's named IClass record. Deferred window scheduling remains
// later presenter work.
internal static class MuiNativeRedrawServiceCore
{
	internal static bool Redraw(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR muiLibrary, APTR obj, uint flags)
	{
		var memory = default(MuiNativeClassMemory);
		if (!MuiMasterPrivateRootCodec.TryRead(ref memory, ownerRoot,
			out var root) || root.LoaderState == 0 ||
			!MuiNativeClassDispatchContextCore.HasActiveFrameForTask(ref memory,
				APTR.FromPointer(root.LoaderState),
				Exec.FindTask(CString.FromPointer(0)))) return false;
		if (!TryResolveTarget(ref platform, publicObjects, ownerRoot,
			muiLibrary, obj, flags, out var target)) return false;
		var storage = platform.Allocate(MuiGuestUlongStorage.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (storage.IsNull || !platform.IsMapped(storage,
			MuiGuestUlongStorage.Size))
		{
			if (storage.IsNotNull) platform.Free(storage,
				MuiGuestUlongStorage.Size);
			return false;
		}
		if (!TryCountAncestorPath(ref platform, publicObjects, ownerRoot,
			obj, out var ancestorCount) || ancestorCount == 0 ||
			ancestorCount > uint.MaxValue / MuiNativeRedrawAncestorRecord.Size)
		{
			platform.Free(storage, MuiGuestUlongStorage.Size);
			return false;
		}
		var ancestorBytes = ancestorCount * MuiNativeRedrawAncestorRecord.Size;
		var ancestors = platform.Allocate(ancestorBytes,
			MuiHeadlessLayout.AllocationFlags);
		if (ancestors.IsNull || !platform.IsMapped(ancestors, ancestorBytes))
		{
			if (ancestors.IsNotNull) platform.Free(ancestors, ancestorBytes);
			platform.Free(storage, MuiGuestUlongStorage.Size);
			return false;
		}
		var planned = TryBuildVirtualClipPlan(ref platform, publicObjects,
			ownerRoot, obj, storage, ancestors, ancestorCount, out var clipPlan);
		platform.Free(storage, MuiGuestUlongStorage.Size);
		if (!planned)
		{
			platform.Free(ancestors, ancestorBytes);
			return false;
		}
		if (clipPlan.DrawEligible == 0)
		{
			platform.Free(ancestors, ancestorBytes);
			return true;
		}

		// Public GetAttr calls used while building the plan may cross a custom
		// dispatcher. Revalidate the chain before using the planned Layer pointer,
		// then re-admit the target before retaining its method entry.
		if (!TryValidateAncestorPath(ref platform, publicObjects, ownerRoot,
			ancestors, ancestorCount) ||
			!TryResolveTarget(ref platform, publicObjects, ownerRoot,
			muiLibrary, obj, flags, out var currentTarget) ||
			!SameTarget(target, currentTarget) ||
			!TryValidateRenderBinding(ref memory, publicObjects, ownerRoot, obj,
				clipPlan.RenderBinding) ||
			(clipPlan.HasClip != 0 && !TryValidateLayerBinding(ref memory,
				publicObjects, ownerRoot, obj, clipPlan.LayerBinding)))
		{
			platform.Free(ancestors, ancestorBytes);
			return false;
		}

		var message = platform.Allocate(MuiNativeRedrawMessage.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (message.IsNull || !memory.IsMapped(message,
			MuiNativeRedrawMessage.Size))
		{
			if (message.IsNotNull) platform.Free(message,
				MuiNativeRedrawMessage.Size);
			platform.Free(ancestors, ancestorBytes);
			return false;
		}

		var clipToken = APTR.Null;
		if (clipPlan.HasClip != 0)
		{
			clipToken = MuiNativeDrawingPlatformCore.PushClipIntersection(
				ref platform, clipPlan.LayerBinding.Layer, clipPlan.Bounds.Left,
				clipPlan.Bounds.Top, clipPlan.Bounds.Width,
				clipPlan.Bounds.Height);
			if (clipToken.IsNull)
			{
				platform.Free(message, MuiNativeRedrawMessage.Size);
				platform.Free(ancestors, ancestorBytes);
				return false;
			}
		}
		// Viewport queries may re-enter custom dispatch. Recheck the complete
		// named parent path and target dispatch identity after clip setup,
		// immediately before entering user code.
		if (!TryValidateAncestorPath(ref platform, publicObjects, ownerRoot,
			ancestors, ancestorCount) ||
			!TryResolveTarget(ref platform, publicObjects, ownerRoot,
				muiLibrary, obj, flags, out currentTarget) ||
			!SameTarget(target, currentTarget) ||
			!TryValidateRenderBinding(ref memory, publicObjects, ownerRoot, obj,
				clipPlan.RenderBinding) ||
			(clipPlan.HasClip != 0 && !TryValidateLayerBinding(ref memory,
				publicObjects, ownerRoot, obj, clipPlan.LayerBinding)))
		{
			if (clipToken.IsNotNull)
				platform.PopClip(clipPlan.LayerBinding.Layer, clipToken);
			platform.Free(message, MuiNativeRedrawMessage.Size);
			platform.Free(ancestors, ancestorBytes);
			return false;
		}
		var redrawn = Invoke(target, message, flags);
		if (clipToken.IsNotNull)
			platform.PopClip(clipPlan.LayerBinding.Layer, clipToken);
		platform.Free(message, MuiNativeRedrawMessage.Size);
		platform.Free(ancestors, ancestorBytes);
		return redrawn;
	}

	internal static bool TryCountAncestorPath<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR obj, out uint count)
		where TMemory : struct, IMuiGuestMemory
	{
		count = 0;
		if (obj.IsNull) return false;
		var current = obj;
		while (current.IsNotNull && count < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativePublicObjectCore.TryFindLive(ref memory,
				publicObjects, ownerRoot, current, out var binding)) return false;
			count++;
			current = binding.Parent;
		}
		return current.IsNull && count != 0;
	}

	internal static bool TryValidateAncestorPath<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR ancestors, uint count)
		where TMemory : struct, IMuiGuestMemory
	{
		if (ancestors.IsNull || count == 0 ||
			count > MuiHeadlessLayout.MaximumTraversal ||
			count > uint.MaxValue / MuiNativeRedrawAncestorRecord.Size)
			return false;
		var byteSize = count * MuiNativeRedrawAncestorRecord.Size;
		if (!MuiGuestStructCursor.TryCreate(ref memory, ancestors, byteSize,
			out var cursor)) return false;
		for (var index = 0u; index < count; index++)
		{
			if (!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeRedrawAncestorRecord.Size, out var recordAddress) ||
			!MuiNativeRedrawAncestorCodec.TryRead(ref memory, recordAddress,
				out var record) || record.Object.IsNull || record.Sidecar.IsNull ||
			!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
				ownerRoot, record.Object, out var binding) ||
			binding.Sidecar != record.Sidecar) return false;

			var expectedParent = APTR.Null;
			if (index + 1 < count)
			{
				var lookahead = cursor;
				if (!MuiGuestStructCursor.TryTake(ref memory, ref lookahead,
					MuiNativeRedrawAncestorRecord.Size, out var nextAddress) ||
				!MuiNativeRedrawAncestorCodec.TryRead(ref memory, nextAddress,
					out var next) || next.Object.IsNull) return false;
				expectedParent = next.Object;
			}
			if (binding.Parent != expectedParent) return false;
		}
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryBuildVirtualClipPlan(
		ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR obj, APTR storage, APTR ancestors,
		uint ancestorCount,
		out MuiNativeRedrawClipPlan plan)
	{
		plan = default;
		plan.DrawEligible = 1;
		if (obj.IsNull || storage.IsNull || platform.IntuitionBase.IsNull ||
			ancestors.IsNull || ancestorCount == 0 ||
			ancestorCount > uint.MaxValue / MuiNativeRedrawAncestorRecord.Size ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, storage, 0))
			return false;
		var ancestorBytes = ancestorCount * MuiNativeRedrawAncestorRecord.Size;
		if (!MuiGuestStructCursor.TryCreate(ref platform, ancestors,
			ancestorBytes, out var ancestorCursor)) return false;

		var current = obj;
		var visited = 0u;
		var captured = 0u;
		var emptyIntersection = false;
		while (current.IsNotNull &&
			visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativePublicObjectCore.TryFindLive(ref platform,
				publicObjects, ownerRoot, current, out var childBinding))
				return false;
			if (captured >= ancestorCount ||
				!MuiGuestStructCursor.TryTake(ref platform,
					ref ancestorCursor, MuiNativeRedrawAncestorRecord.Size,
					out var recordAddress)) return false;
			var ancestorRecord = default(MuiNativeRedrawAncestorRecord);
			ancestorRecord.Object = current;
			ancestorRecord.Sidecar = childBinding.Sidecar;
			if (!MuiNativeRedrawAncestorCodec.Write(ref platform,
				recordAddress, ancestorRecord)) return false;
			captured++;
			var parent = childBinding.Parent;
			if (parent.IsNull)
			{
				current = APTR.Null;
				break;
			}
			if (!MuiNativePublicObjectCore.TryFindLive(ref platform,
				publicObjects, ownerRoot, parent, out var parentBinding))
				return false;

			if (!emptyIntersection)
			{
				var hasWidth = MuiNativeGuiMode.TryGetAttribute(ref platform,
					parent, MuiNativeGuiMode.VirtgroupWidth, storage,
					out _);
				var hasHeight = MuiNativeGuiMode.TryGetAttribute(ref platform,
					parent, MuiNativeGuiMode.VirtgroupHeight, storage,
					out _);
				if (hasWidth != hasHeight) return false;
				if (hasWidth)
				{
					if (!MuiNativeGuiMode.TryReadInnerAreaRectangle(ref platform,
						parent, storage, out var viewport) ||
						!MuiNativeRedrawClipPlanCore.IncludeViewport(ref plan,
							viewport)) return false;
					emptyIntersection = plan.DrawEligible == 0;
				}
			}

			// GetAttr is a callback boundary. Do not keep a parent pointer whose
			// binding was disposed or reparented while the viewport was queried.
			if (!MuiNativePublicObjectCore.TryFindLive(ref platform,
				publicObjects, ownerRoot, parent, out var currentParent) ||
				currentParent.Sidecar != parentBinding.Sidecar ||
				currentParent.Parent != parentBinding.Parent)
				return false;
			current = parent;
		}
		if (current.IsNotNull || captured != ancestorCount ||
			!MuiGuestStructCursor.IsComplete(ancestorCursor)) return false;
		if (plan.DrawEligible == 0) return true;

		// MorphOS requires render info and RastPort for every MUI_Redraw, not
		// merely when clipping is needed. Missing setup state is a successful
		// no-draw result; it must never reach the object's MUIM_Draw dispatcher.
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
			ownerRoot, obj, out var targetBinding) ||
			!TryReadRenderBinding(ref memory, obj, targetBinding.Sidecar,
				out var renderBinding))
		{
			plan.DrawEligible = 0;
			return true;
		}
		plan.RenderBinding = renderBinding;
		if (plan.HasClip == 0) return true;

		// A clipped virtual-group child also needs a live Layers Layer for the
		// temporary clip region; this is a stricter requirement than drawing.
		if (!TryReadLayerBinding(ref memory, obj, targetBinding.Sidecar,
			out var layerBinding))
		{
			plan.DrawEligible = 0;
			return true;
		}
		plan.LayerBinding = layerBinding;
		return true;
	}

	internal static bool TryReadRenderBinding<TMemory>(ref TMemory memory,
		APTR obj, APTR sidecar, out MuiNativeRedrawRenderBinding binding)
		where TMemory : struct, IMuiGuestMemory
	{
		binding = default;
		if (obj.IsNull || sidecar.IsNull ||
			!MuiNativeObjectStateCore.TryGetAttribute(ref memory, sidecar,
				MuiAreaWindowRelationshipCore.RenderInfoAttribute,
				out var renderInfoRaw) || renderInfoRaw == 0)
			return false;

		var renderInfoAddress = APTR.FromPointer(renderInfoRaw);
		if (!MuiDrawingRenderInfoCodec.TryRead(ref memory, renderInfoAddress,
			out var renderInfo) || renderInfo.RastPort.IsNull ||
			!MuiDrawingRasterPortCodec.TryRead(ref memory, renderInfo.RastPort,
				out _)) return false;

		binding.Object = obj;
		binding.Sidecar = sidecar;
		binding.RenderInfo = renderInfoAddress;
		binding.RastPort = renderInfo.RastPort;
		return true;
	}

	internal static bool TryReadLayerBinding<TMemory>(ref TMemory memory,
		APTR obj, APTR sidecar, out MuiNativeRedrawLayerBinding binding)
		where TMemory : struct, IMuiGuestMemory
	{
		binding = default;
		if (!TryReadRenderBinding(ref memory, obj, sidecar,
			out var renderBinding))
			return false;

		if (!MuiDrawingRasterPortCodec.TryRead(ref memory,
			renderBinding.RastPort, out var rasterPort) || rasterPort.Layer.IsNull ||
			!memory.IsMapped(rasterPort.Layer, Layer.Size)) return false;

		binding.Object = renderBinding.Object;
		binding.Sidecar = renderBinding.Sidecar;
		binding.RenderInfo = renderBinding.RenderInfo;
		binding.RastPort = renderBinding.RastPort;
		binding.Layer = rasterPort.Layer;
		return true;
	}

	internal static bool TryValidateRenderBinding<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR obj,
		MuiNativeRedrawRenderBinding expected)
		where TMemory : struct, IMuiGuestMemory =>
		obj == expected.Object &&
		MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
			ownerRoot, obj, out var liveBinding) &&
		liveBinding.Sidecar == expected.Sidecar &&
		TryReadRenderBinding(ref memory, obj, liveBinding.Sidecar,
			out var current) && SameRenderBinding(expected, current);

	internal static bool TryValidateLayerBinding<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR obj,
		MuiNativeRedrawLayerBinding expected)
		where TMemory : struct, IMuiGuestMemory =>
		obj == expected.Object &&
		MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
			ownerRoot, obj, out var liveBinding) &&
		liveBinding.Sidecar == expected.Sidecar &&
		TryReadLayerBinding(ref memory, obj, liveBinding.Sidecar,
			out var current) && SameLayerBinding(expected, current);

	internal static bool TryResolveTarget(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR muiLibrary, APTR obj,
		uint flags, out MuiNativeRedrawTarget target)
	{
		target = default;
		if (obj.IsNull || ownerRoot.IsNull || publicObjects.IsNull ||
			muiLibrary.IsNull || flags == 0 ||
			(flags & ~MuiNativeRedrawMessage.AllowedFlags) != 0 ||
			!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, obj, out var binding)) return false;

		var memory = default(MuiNativeClassMemory);
		if (!memory.IsMapped(obj, _Object.Size)) return false;
		var objectValue = BOOPSIGuestCodec.ReadObjectHeader(ref memory, obj);
		var classPointer = objectValue.o_Class;
		if (classPointer.IsNull || !memory.IsMapped(classPointer, IClass.Size))
			return false;
		var classValue = BOOPSIGuestCodec.ReadClass(ref memory, classPointer);
		var entry = classValue.cl_Dispatcher.Entry;
		if (entry.IsNull) return false;
		if (!MuiNativeLayoutServiceCore.TryResolveCallbackBase(ref memory,
			binding.Lease, classPointer, muiLibrary, out var callbackBase))
			return false;
		target.Object = obj;
		target.Class = classPointer;
		target.Entry = entry;
		target.LibraryBase = callbackBase;
		return true;
	}

	internal static bool Invoke(MuiNativeRedrawTarget target, APTR message,
		uint flags)
	{
		if (target.Object.IsNull || target.Class.IsNull || target.Entry.IsNull ||
			target.LibraryBase.IsNull || flags == 0 ||
			(flags & ~MuiNativeRedrawMessage.AllowedFlags) != 0) return false;
		var memory = default(MuiNativeClassMemory);
		if (!memory.IsMapped(message, MuiNativeRedrawMessage.Size)) return false;
		var packet = default(MuiNativeRedrawMessage);
		packet.MethodId = MuiNativeRedrawMessage.DrawMethodId;
		packet.Flags = flags;
		if (!MuiNativeRedrawMessageCodec.Write(ref memory, message, packet))
			return false;
		_ = MuiNativeRedrawCalls.Dispatch(target.Entry, target.Class,
			target.Object, message, target.LibraryBase);
		return true;
	}

	private static bool SameTarget(MuiNativeRedrawTarget first,
		MuiNativeRedrawTarget second) => first.Object == second.Object &&
		first.Class == second.Class && first.Entry == second.Entry &&
		first.LibraryBase == second.LibraryBase;

	private static bool SameRenderBinding(MuiNativeRedrawRenderBinding first,
		MuiNativeRedrawRenderBinding second) => first.Object == second.Object &&
		first.Sidecar == second.Sidecar && first.RenderInfo == second.RenderInfo &&
		first.RastPort == second.RastPort;

	private static bool SameLayerBinding(MuiNativeRedrawLayerBinding first,
		MuiNativeRedrawLayerBinding second) => first.Object == second.Object &&
		first.Sidecar == second.Sidecar && first.RenderInfo == second.RenderInfo &&
		first.RastPort == second.RastPort && first.Layer == second.Layer;
}
