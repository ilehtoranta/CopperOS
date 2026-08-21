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
internal struct MuiApplicationPersistenceFrameState
{
	internal const uint Size = 12;

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

internal static class MuiApplicationPersistenceFrameFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationPersistenceFrameFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiApplicationPersistenceFrameField.Object:
				offset = 0;
				break;
			case MuiApplicationPersistenceFrameField.NextChild:
				offset = 4;
				break;
			case MuiApplicationPersistenceFrameField.VisitMarker:
				offset = 8;
				break;
			default:
				return false;
		}
		if (cursor.Frame.IsNull || cursor.Frame.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Frame.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR frame, MuiApplicationPersistenceFrameField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationPersistenceFrameFieldCursor);
		cursor.Frame = frame;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR frame, MuiApplicationPersistenceFrameField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationPersistenceFrameFieldCursor);
		cursor.Frame = frame;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
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
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >=
			MuiApplicationPersistenceFrameCursor.MaximumEntries || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiApplicationPersistenceFrameCursor.EntrySize) return false;
		var offset = cursor.Index *
			MuiApplicationPersistenceFrameCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address,
			MuiApplicationPersistenceFrameCursor.EntrySize);
	}
}

internal static class MuiApplicationPersistenceFrameCodec
{
	internal static bool TryGetFrame<TPlatform>(ref TPlatform platform,
		APTR stack, uint depth, out APTR frame)
		where TPlatform : struct, IMuiGuestMemory
	{
		frame = APTR.Null;
		if (stack.IsNull || depth == 0) return false;
		var cursor = default(MuiApplicationPersistenceFrameCursor);
		cursor.Base = stack;
		cursor.Index = depth - 1;
		return MuiApplicationPersistenceFrameCursorCodec.TryGetEntry(
			ref platform, cursor, out frame);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationPersistenceFrameState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationPersistenceFrameState.Size)) return false;
		if (!MuiApplicationPersistenceFrameFieldCursorCodec.TryRead(ref platform,
			address, MuiApplicationPersistenceFrameField.Object, out var raw))
			return false;
		value.Object = APTR.FromPointer(raw);
		return MuiApplicationPersistenceFrameFieldCursorCodec.TryRead(ref platform,
			address, MuiApplicationPersistenceFrameField.NextChild,
			out value.NextChild) &&
			MuiApplicationPersistenceFrameFieldCursorCodec.TryRead(ref platform,
				address, MuiApplicationPersistenceFrameField.VisitMarker,
				out value.VisitMarker);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationPersistenceFrameState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationPersistenceFrameState.Size)) return false;
		return MuiApplicationPersistenceFrameFieldCursorCodec.TryWrite(ref platform,
			address, MuiApplicationPersistenceFrameField.Object, value.Object.Raw) &&
			MuiApplicationPersistenceFrameFieldCursorCodec.TryWrite(ref platform,
				address, MuiApplicationPersistenceFrameField.NextChild,
				value.NextChild) &&
			MuiApplicationPersistenceFrameFieldCursorCodec.TryWrite(ref platform,
				address, MuiApplicationPersistenceFrameField.VisitMarker,
				value.VisitMarker);
	}
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
		if (!MuiApplicationPersistenceFrameCodec.Write(ref platform, stack,
			rootFrame)) return Finish(ref platform, stack, stackBytes, false);

		while (depth != 0)
		{
			if (!MuiApplicationPersistenceFrameCodec.TryGetFrame(ref platform,
				stack, depth, out var frame))
				return Finish(ref platform, stack, stackBytes, false);
			if (!MuiApplicationPersistenceFrameCodec.TryRead(ref platform, frame,
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
				if (!MuiApplicationPersistenceFrameCodec.Write(ref platform, frame,
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
			if (!MuiApplicationPersistenceFrameCodec.Write(ref platform, frame,
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
		if (!MuiApplicationPersistenceFrameCodec.TryGetFrame(ref platform, stack,
			depth + 1, out var frame)) return false;
		var value = default(MuiApplicationPersistenceFrameState);
		value.Object = obj;
		value.NextChild = nextChild;
		return MuiApplicationPersistenceFrameCodec.Write(ref platform, frame,
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
