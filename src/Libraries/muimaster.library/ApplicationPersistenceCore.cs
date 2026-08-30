/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident depth-first traversal state for Application Save/Load. The
// walker owns a bounded stack of these records; callers use the named fields
// and never depend on the guest word offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationPersistenceFrameState
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint ObjectOffset = 0;
	internal const uint NextChildOffset = 4;
	internal const uint VisitMarkerOffset = 8;

	internal APTR Object;
	internal uint NextChild;
	internal uint VisitMarker;
}

internal enum MuiApplicationPersistenceFrameField : byte
{
	Object,
	NextChild,
	VisitMarker,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationPersistenceFrameFieldCursor
{
	internal APTR Frame;
	internal MuiApplicationPersistenceFrameField Field;
}

// Struct-first guest-memory adapter for one persistence traversal frame. The
// stack cursor selects records; this codec owns complete-frame admission and
// named Object/NextChild/VisitMarker field translation.
internal static class MuiApplicationPersistenceFrameMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR frame, MuiApplicationPersistenceFrameField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (field)
		{
			case MuiApplicationPersistenceFrameField.Object:
				offset = MuiApplicationPersistenceFrameState.ObjectOffset;
				break;
			case MuiApplicationPersistenceFrameField.NextChild:
				offset = MuiApplicationPersistenceFrameState.NextChildOffset;
				break;
			case MuiApplicationPersistenceFrameField.VisitMarker:
				offset = MuiApplicationPersistenceFrameState.VisitMarkerOffset;
				break;
			default:
				return false;
		}
		if (frame.IsNull || frame.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(frame, MuiApplicationPersistenceFrameState.Size))
			return false;
		address = APTR.FromPointer(frame.Raw + offset);
		return platform.IsMapped(address,
			MuiApplicationPersistenceFrameState.FieldSize);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR frame, MuiApplicationPersistenceFrameField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, frame, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR frame, MuiApplicationPersistenceFrameField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, frame, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for existing typed cursor diagnostics.
internal static class MuiApplicationPersistenceFrameFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationPersistenceFrameFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiApplicationPersistenceFrameMemoryCodec.TryGetAddress(ref platform,
			cursor.Frame, cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR frame, MuiApplicationPersistenceFrameField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiApplicationPersistenceFrameMemoryCodec.TryRead(ref platform, frame, field,
			out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR frame, MuiApplicationPersistenceFrameField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiApplicationPersistenceFrameMemoryCodec.TryWrite(ref platform, frame, field,
			value);
}

// Save/Load keeps its depth-first stack in guest memory. Represent the frame
// index as a named cursor so stack access has one bounded, overflow-checked
// entry boundary instead of repeating `stack + depth * 12` arithmetic.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationPersistenceFrameCursor
{
	internal const uint EntrySize = MuiApplicationPersistenceFrameState.Size;
	internal const uint MaximumEntries = 256;
	internal APTR Base;
	internal uint Index;
}

internal static class MuiApplicationPersistenceFrameCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiApplicationPersistenceFrameCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiApplicationPersistenceFrameVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);
}

// Struct-first guest-memory adapter for the bounded persistence frame stack.
// The complete named frame record is admitted before traversal code reads it;
// the typed cursor above remains a compatibility wrapper.
internal static class MuiApplicationPersistenceFrameVectorMemoryCodec
{
	internal static bool TryGetFrame<TPlatform>(ref TPlatform platform,
		APTR stack, uint depth, out APTR frame)
		where TPlatform : struct, IMuiGuestMemory
	{
		frame = APTR.Null;
		return depth != 0 && TryGetEntry(ref platform, stack, depth - 1,
			out frame);
	}

	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR stack, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (stack.IsNull || index >=
			MuiApplicationPersistenceFrameCursor.MaximumEntries || index >
			(uint.MaxValue - stack.Raw) /
			MuiApplicationPersistenceFrameState.Size) return false;
		var offset = index * MuiApplicationPersistenceFrameState.Size;
		if (stack.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(stack.Raw + offset);
		return platform.IsMapped(address, MuiApplicationPersistenceFrameState.Size);
	}
}

// Canonical codec for the named persistence frame record.  Live Save/Load
// traversal uses this value-type boundary; the historical codec below only
// forwards for callers that still use its older name.
internal static class MuiApplicationPersistenceFrameStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationPersistenceFrameState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationPersistenceFrameState.Size)) return false;
		if (!MuiApplicationPersistenceFrameMemoryCodec.TryRead(ref platform,
			address, MuiApplicationPersistenceFrameField.Object, out var raw))
			return false;
		value.Object = APTR.FromPointer(raw);
		return MuiApplicationPersistenceFrameMemoryCodec.TryRead(ref platform,
			address, MuiApplicationPersistenceFrameField.NextChild,
			out value.NextChild) &&
			MuiApplicationPersistenceFrameMemoryCodec.TryRead(ref platform,
				address, MuiApplicationPersistenceFrameField.VisitMarker,
				out value.VisitMarker);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationPersistenceFrameState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationPersistenceFrameState.Size)) return false;
		return MuiApplicationPersistenceFrameMemoryCodec.TryWrite(ref platform,
			address, MuiApplicationPersistenceFrameField.Object, value.Object.Raw) &&
			MuiApplicationPersistenceFrameMemoryCodec.TryWrite(ref platform,
				address, MuiApplicationPersistenceFrameField.NextChild,
				value.NextChild) &&
			MuiApplicationPersistenceFrameMemoryCodec.TryWrite(ref platform,
				address, MuiApplicationPersistenceFrameField.VisitMarker,
					value.VisitMarker);
	}
}

// Compatibility wrapper retained for existing typed cursor diagnostics and
// older persistence callers.
internal static class MuiApplicationPersistenceFrameCodec
{
	internal static bool TryGetFrame<TPlatform>(ref TPlatform platform,
		APTR stack, uint depth, out APTR frame)
		where TPlatform : struct, IMuiGuestMemory
	{
		frame = APTR.Null;
		if (stack.IsNull || depth == 0) return false;
		return MuiApplicationPersistenceFrameVectorMemoryCodec.TryGetFrame(ref platform,
			stack, depth, out frame);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationPersistenceFrameState value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiApplicationPersistenceFrameStateCodec.TryRead(ref platform, address,
			out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationPersistenceFrameState value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiApplicationPersistenceFrameStateCodec.Write(ref platform, address, value);
}

// Native-safe application-tree persistence walker for MorphOS MUI. The real
// MUIM_Application_Save/Load file service owns the ENV/ENVARC selectors and
// file format; this core supplies the object-graph part once that service has
// a live Dataspace transport. Traversal state lives in guest memory so the
// implementation has no managed stack, collections, exceptions, or host
// object graph.
public static class MuiApplicationPersistenceCore
{
	private const uint NotVisited = uint.MaxValue;
	// A 256-frame guest stack bounds malformed/cyclic trees while keeping the
	// complete frame area small enough for the freestanding native arena.
	private const uint MaximumDepth =
		MuiApplicationPersistenceFrameCursor.MaximumEntries;
	private const uint ObjectIdAttribute = 0x8042D76E;

	public static bool Export<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, APTR dataspace)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Walk(ref platform, state, application, dataspace, true);

	public static bool Import<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, APTR dataspace)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Walk(ref platform, state, application, dataspace, false);

	// Applies one imported Dataspace transactionally. The caller supplies a
	// second live Dataspace owned by the same MUI state. We first snapshot the
	// current object tree, then apply the incoming values; if any object rejects
	// the import, the snapshot is walked back through the same class-specific
	// Import seam. All traversal state remains guest-resident and no managed
	// transaction log is created.
	public static bool ImportTransactional<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, APTR dataspace, APTR snapshot)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (snapshot.IsNull || dataspace.IsNull ||
			snapshot.Raw == dataspace.Raw) return false;
		MuiStoreCore.DataspaceClear(ref platform, state, snapshot);
		if (!Export(ref platform, state, application, snapshot))
			return false;
		if (Import(ref platform, state, application, dataspace)) return true;
	// A failed incoming import remains a failed operation even when the
	// compensating snapshot walk succeeds.
		Import(ref platform, state, application, snapshot);
		return false;
	}

	private static bool Walk<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, APTR dataspace, bool export)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, dataspace).IsNull)
			return false;

		var stackBytes = unchecked(MaximumDepth *
			MuiApplicationPersistenceFrameState.Size);
		var stack = MuiHeadlessMemory.Allocate(ref platform, stackBytes);
		if (stack.IsNull) return false;
		var depth = 1u;
		var visited = 0u;
		var rootFrame = default(MuiApplicationPersistenceFrameState);
		rootFrame.Object = application;
		rootFrame.NextChild = NotVisited;
		if (!MuiApplicationPersistenceFrameStateCodec.Write(ref platform, stack,
			rootFrame)) return Finish(ref platform, stack, stackBytes, false);

		while (depth != 0)
		{
			if (!MuiApplicationPersistenceFrameVectorMemoryCodec.TryGetFrame(ref platform,
				stack, depth, out var frame))
				return Finish(ref platform, stack, stackBytes, false);
			if (!MuiApplicationPersistenceFrameStateCodec.TryRead(ref platform, frame,
				out var frameState))
				return Finish(ref platform, stack, stackBytes, false);
			var current = frameState.Object;
			if (current.IsNull)
				return Finish(ref platform, stack, stackBytes, false);

			var nextChild = frameState.NextChild;
			if (nextChild == NotVisited)
			{
				if (visited++ >= MuiHeadlessLayout.MaximumTraversal)
					return Finish(ref platform, stack, stackBytes, false);
				if (!PersistOne(ref platform, state, current, dataspace, export))
					return Finish(ref platform, stack, stackBytes, false);
				frameState.NextChild = 0;
				frameState.VisitMarker = visited;
				if (!MuiApplicationPersistenceFrameStateCodec.Write(ref platform, frame,
					frameState)) return Finish(ref platform, stack, stackBytes, false);
				continue;
			}

			var child = MuiFamilyCore.GetChild(ref platform, state, current,
				unchecked((int)nextChild), APTR.Null);
			if (child.IsNull)
			{
				depth--;
				continue;
			}
			if (depth >= MaximumDepth)
				return Finish(ref platform, stack, stackBytes, false);
			frameState.NextChild = nextChild + 1;
			if (!MuiApplicationPersistenceFrameStateCodec.Write(ref platform, frame,
				frameState) || !WriteFrame(ref platform, stack, depth, child,
				NotVisited)) return Finish(ref platform, stack, stackBytes, false);
			depth++;
		}

		return Finish(ref platform, stack, stackBytes, true);
	}

	private static bool PersistOne<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR dataspace, bool export)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			ObjectIdAttribute, out var objectId)) return false;
		// MorphOS suppresses MUIM_Export/MUIM_Import for ObjectID == 0.
		if (objectId == 0) return true;
		return export ? MuiObjectPersistenceCore.Export(ref platform, state, obj,
			dataspace) : MuiObjectPersistenceCore.Import(ref platform, state, obj,
			dataspace);
	}

	private static bool WriteFrame<TPlatform>(ref TPlatform platform, APTR stack,
		uint depth, APTR obj, uint nextChild)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiApplicationPersistenceFrameVectorMemoryCodec.TryGetFrame(ref platform, stack,
			depth + 1, out var frame)) return false;
		var value = default(MuiApplicationPersistenceFrameState);
		value.Object = obj;
		value.NextChild = nextChild;
		return MuiApplicationPersistenceFrameStateCodec.Write(ref platform, frame,
			value);
	}

	private static bool Finish<TPlatform>(ref TPlatform platform, APTR stack,
		uint stackBytes, bool result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		platform.Clear(stack, stackBytes);
		platform.Free(stack, stackBytes);
		return result;
	}
}
