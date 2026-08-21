/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The public MUIA_Application_WindowList value is an Exec List pointer. The
// list is a read-only guest projection over the application's owned Window
// children; application and child links remain in the existing typed Family
// records. The projection is rebuilt after a guest topology mutation. ABI
// field offsets are confined to the small codecs below; list logic uses the
// named state and entry structs.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowListState
{
	internal const uint Magic = 0x41574C53; // "AWLS"
	internal const uint Size = 32;
	internal uint Cookie;
	internal APTR Application;
	internal APTR List;
	internal APTR Entries;
	internal uint Count;
	internal uint Capacity;
	internal uint Mutation;
	internal uint Generation;
}

internal enum MuiApplicationWindowListStateField : byte
{
	Cookie,
	Application,
	List,
	Entries,
	Count,
	Capacity,
	Mutation,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowListStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationWindowListStateField Field;
}

internal static class MuiApplicationWindowListStateFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationWindowListStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiApplicationWindowListStateField.Cookie => 0,
			MuiApplicationWindowListStateField.Application => 4,
			MuiApplicationWindowListStateField.List => 8,
			MuiApplicationWindowListStateField.Entries => 12,
			MuiApplicationWindowListStateField.Count => 16,
			MuiApplicationWindowListStateField.Capacity => 20,
			MuiApplicationWindowListStateField.Mutation => 24,
			MuiApplicationWindowListStateField.Generation => 28,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationWindowListStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowListStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationWindowListStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowListStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationWindowListStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationWindowListStateCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationWindowListState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationWindowListState.Size) || value.Capacity < value.Count)
			return false;
		return MuiApplicationWindowListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationWindowListStateField.Cookie,
			MuiApplicationWindowListState.Magic) &&
			MuiApplicationWindowListStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationWindowListStateField.Application,
				value.Application.Raw) &&
			MuiApplicationWindowListStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationWindowListStateField.List, value.List.Raw) &&
			MuiApplicationWindowListStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationWindowListStateField.Entries,
				value.Entries.Raw) &&
			MuiApplicationWindowListStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationWindowListStateField.Count, value.Count) &&
			MuiApplicationWindowListStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationWindowListStateField.Capacity, value.Capacity) &&
			MuiApplicationWindowListStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationWindowListStateField.Mutation, value.Mutation) &&
			MuiApplicationWindowListStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationWindowListStateField.Generation,
				value.Generation);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationWindowListState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationWindowListState.Size) ||
			!MuiApplicationWindowListStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationWindowListStateField.Cookie, out var cookie) ||
			cookie != MuiApplicationWindowListState.Magic ||
			!MuiApplicationWindowListStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationWindowListStateField.Application,
				out var application) ||
			!MuiApplicationWindowListStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationWindowListStateField.List, out var list) ||
			!MuiApplicationWindowListStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationWindowListStateField.Entries,
				out var entries) ||
			!MuiApplicationWindowListStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationWindowListStateField.Count, out var count) ||
			!MuiApplicationWindowListStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationWindowListStateField.Capacity,
				out var capacity) ||
			!MuiApplicationWindowListStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationWindowListStateField.Mutation,
				out var mutation) ||
			!MuiApplicationWindowListStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationWindowListStateField.Generation,
				out var generation)) return false;
		value.Cookie = MuiApplicationWindowListState.Magic;
		value.Application = APTR.FromPointer(application);
		value.List = APTR.FromPointer(list);
		value.Entries = APTR.FromPointer(entries);
		value.Count = count;
		value.Capacity = capacity;
		value.Mutation = mutation;
		value.Generation = generation;
		return value.Capacity >= value.Count;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowListEntry
{
	internal const uint Size = 16;
	internal const uint ProjectionMagic = 0x4157454E; // "AWEN"
	internal APTR Next;
	internal APTR Previous;
	internal APTR Object;
	internal APTR Reserved;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowListEntryCursor
{
	internal const uint EntrySize = MuiApplicationWindowListEntry.Size;
	internal APTR Base;
	internal uint Index;
}

internal enum MuiApplicationWindowListEntryField : byte
{
	Next,
	Previous,
	Object,
	Reserved,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowListEntryFieldCursor
{
	internal APTR Record;
	internal MuiApplicationWindowListEntryField Field;
}

internal static class MuiApplicationWindowListEntryFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationWindowListEntryField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiApplicationWindowListEntryField.Next => 0,
			MuiApplicationWindowListEntryField.Previous => 4,
			MuiApplicationWindowListEntryField.Object => 8,
			MuiApplicationWindowListEntryField.Reserved => 12,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationWindowListEntryFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowListEntryField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationWindowListEntryFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowListEntryField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationWindowListEntryFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationWindowListEntryVectorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiApplicationWindowListEntryCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiApplicationWindowListEntryCursor.EntrySize) return false;
		var offset = cursor.Index *
			MuiApplicationWindowListEntryCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address,
			MuiApplicationWindowListEntryCursor.EntrySize);
	}
}

internal static class MuiApplicationWindowListEntryCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationWindowListEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationWindowListEntry.Size)) return false;
		return MuiApplicationWindowListEntryFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationWindowListEntryField.Next,
			value.Next.Raw) &&
			MuiApplicationWindowListEntryFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationWindowListEntryField.Previous,
				value.Previous.Raw) &&
			MuiApplicationWindowListEntryFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationWindowListEntryField.Object,
				value.Object.Raw) &&
			MuiApplicationWindowListEntryFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationWindowListEntryField.Reserved,
				value.Reserved.Raw);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationWindowListEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationWindowListEntry.Size) ||
			!MuiApplicationWindowListEntryFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationWindowListEntryField.Next, out var next) ||
			!MuiApplicationWindowListEntryFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationWindowListEntryField.Previous,
				out var previous) ||
			!MuiApplicationWindowListEntryFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationWindowListEntryField.Object, out var obj) ||
			!MuiApplicationWindowListEntryFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationWindowListEntryField.Reserved,
				out var reserved)) return false;
		value.Next = APTR.FromPointer(next);
		value.Previous = APTR.FromPointer(previous);
		value.Object = APTR.FromPointer(obj);
		value.Reserved = APTR.FromPointer(reserved);
		return value.Reserved.Raw == MuiApplicationWindowListEntry.ProjectionMagic;
	}
}

public static class MuiApplicationWindowListCore
{
	public const uint WindowList = 0x80429ABE;
	private const uint WindowOwner = 0x7FFE0010;
	private const uint StateAttribute = 0x7FFE0045;

	// The returned Exec List is a named, read-only projection. Generic OM_GET
	// uses this predicate to admit the getter even though Application.mui is not
	// a common-control class.
	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == WindowList;

	internal static bool TrySet<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint attribute, uint value, bool notify, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		handled = IsPublicGetterAttribute(attribute);
		// The returned Exec List is strictly read-only. Family mutations are the
		// only supported way to change application windows.
		return !handled;
	}

	internal static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, uint attribute, out uint value, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		handled = IsPublicGetterAttribute(attribute);
		if (!handled) return false;
		var list = Ensure(ref platform, state, application);
		value = list.Raw;
		// A live application with no windows still has a valid, empty list
		// projection.  Keep the attribute handled even when its value is Null.
		return true;
	}

	// Read-only NextObject-compatible traversal of the public list projection.
	// The caller supplies the current guest node, normally initialized from
	// Amiga.List.Head; no managed enumerator or object graph is created.
	public static APTR NextObject<TPlatform>(ref TPlatform platform, APTR list,
		ref uint cursorRaw) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = APTR.FromPointer(cursorRaw);
		if (list.IsNull || !platform.IsMapped(list, Amiga.List.Size) ||
			cursor.IsNull || !platform.IsMapped(cursor,
			MuiApplicationWindowListEntry.Size)) return APTR.Null;
		if (!MuiApplicationWindowListEntryCodec.TryRead(ref platform, cursor,
			out var entry)) return APTR.Null;
		cursorRaw = entry.Next.Raw;
		return entry.Object;
	}

	internal static void Cleanup<TPlatform>(ref TPlatform platform, APTR state,
		APTR application) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state,
			application);
		if (record.IsNull || !MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, application, StateAttribute, out var raw)) return;
		FreeStateBlock(ref platform, APTR.FromPointer(raw));
		MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
			StateAttribute, 0, false);
	}

	private static APTR Ensure<TPlatform>(ref TPlatform platform, APTR state,
		APTR application) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state,
			application);
		if (record.IsNull || !MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return APTR.Null;
		var mutation = stateValue.Mutation;
		var oldBlock = APTR.Null;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			application, StateAttribute, out var oldRaw)) oldBlock = APTR.FromPointer(oldRaw);
		if (MuiApplicationWindowListStateCodec.TryRead(ref platform, oldBlock,
			out var current) && current.Application.Raw == application.Raw &&
			current.Mutation == mutation && current.List.IsNotNull &&
			platform.IsMapped(current.List, Amiga.List.Size)) return current.List;

		var count = CountWindows(ref platform, state, application);
		if (count > uint.MaxValue / MuiApplicationWindowListEntry.Size)
			return APTR.Null;
		var list = MuiHeadlessMemory.Allocate(ref platform, Amiga.List.Size);
		if (list.IsNull) return APTR.Null;
		var entriesSize = count * MuiApplicationWindowListEntry.Size;
		var entries = entriesSize == 0 ? APTR.Null :
			MuiHeadlessMemory.Allocate(ref platform, entriesSize);
		if (entriesSize != 0 && entries.IsNull)
		{
			FreeProjection(ref platform, list, APTR.Null, 0);
			return APTR.Null;
		}

		var childIndex = 0u;
		var selected = 0u;
		var previous = APTR.Null;
		while (childIndex < MuiHeadlessLayout.MaximumTraversal &&
			selected < count)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, application,
				unchecked((int)childIndex++), APTR.Null);
			if (child.IsNull) break;
			if (!IsOwnedWindow(ref platform, state, application, child)) continue;
			var cursor = default(MuiApplicationWindowListEntryCursor);
			cursor.Base = entries;
			cursor.Index = selected;
			if (!MuiApplicationWindowListEntryVectorCodec.TryGetEntry(
				ref platform, cursor, out var entry))
			{
				FreeProjection(ref platform, list, entries, entriesSize);
				return APTR.Null;
			}
			var next = APTR.Null;
			if (selected + 1 < count)
			{
				cursor.Index++;
				if (!MuiApplicationWindowListEntryVectorCodec.TryGetEntry(
					ref platform, cursor, out next))
				{
					FreeProjection(ref platform, list, entries, entriesSize);
					return APTR.Null;
				}
			}
			var entryValue = default(MuiApplicationWindowListEntry);
			entryValue.Next = next;
			entryValue.Previous = previous;
			entryValue.Object = child;
			entryValue.Reserved = APTR.FromPointer(
				MuiApplicationWindowListEntry.ProjectionMagic);
			if (!MuiApplicationWindowListEntryCodec.Write(ref platform, entry,
				entryValue))
			{
				FreeProjection(ref platform, list, entries, entriesSize);
				return APTR.Null;
			}
			previous = entry;
			selected++;
		}
		if (selected != count)
		{
			FreeProjection(ref platform, list, entries, entriesSize);
			return APTR.Null;
		}
		var listValue = default(Amiga.List);
		listValue.Head = count == 0 ? APTR.Null : entries;
		listValue.Tail = APTR.Null;
		listValue.TailPred = count == 0 ? APTR.Null : previous;
		listValue.Type = NodeType.Unknown;
		var listRecord = default(MuiGroupExecListRecord);
		listRecord.Head = listValue.Head;
		listRecord.Tail = listValue.Tail;
		listRecord.TailPred = listValue.TailPred;
		listRecord.Type = listValue.Type;
		listRecord.Padding = 0;
		if (!MuiGroupExecListCodec.Write(ref platform, list, listRecord))
		{
			FreeProjection(ref platform, list, entries, entriesSize);
			return APTR.Null;
		}

		var replacement = default(MuiApplicationWindowListState);
		replacement.Cookie = MuiApplicationWindowListState.Magic;
		replacement.Application = application;
		replacement.List = list;
		replacement.Entries = entries;
		replacement.Count = count;
		replacement.Capacity = count;
		replacement.Mutation = mutation;
		replacement.Generation = MuiHeadlessMemory.NextSequence(ref platform,
			state);
		var block = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationWindowListState.Size);
		if (block.IsNull || !MuiHeadlessObjectCore.SetRecordAttributeRaw(
			ref platform, state, record, StateAttribute, block.Raw, false))
		{
			if (block.IsNotNull) platform.Free(block,
				MuiApplicationWindowListState.Size);
			FreeProjection(ref platform, list, entries, entriesSize);
			return APTR.Null;
		}
		// Allocation and the private attribute write do not mutate Family
		// topology, so the captured mutation remains the valid cache key.
		if (!MuiApplicationWindowListStateCodec.Write(ref platform, block,
			replacement))
		{
			MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
				StateAttribute, 0, false);
			platform.Free(block, MuiApplicationWindowListState.Size);
			FreeProjection(ref platform, list, entries, entriesSize);
			return APTR.Null;
		}
		FreeStateBlock(ref platform, oldBlock);
		return list;
	}

	private static uint CountWindows<TPlatform>(ref TPlatform platform,
		APTR state, APTR application) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = 0u;
		for (var index = 0u; index < MuiHeadlessLayout.MaximumTraversal; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, application,
				unchecked((int)index), APTR.Null);
			if (child.IsNull) break;
			if (IsOwnedWindow(ref platform, state, application, child)) count++;
		}
		return count;
	}

	private static bool IsOwnedWindow<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, APTR child)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, child,
			WindowOwner, out var owner) && owner == application.Raw;

	private static void FreeStateBlock<TPlatform>(ref TPlatform platform,
		APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiApplicationWindowListStateCodec.TryRead(ref platform, block,
			out var value)) return;
		var entriesSize = value.Count > uint.MaxValue /
			MuiApplicationWindowListEntry.Size ? 0u : value.Count *
			MuiApplicationWindowListEntry.Size;
		FreeProjection(ref platform, value.List, value.Entries, entriesSize);
		platform.Clear(block, MuiApplicationWindowListState.Size);
		platform.Free(block, MuiApplicationWindowListState.Size);
	}

	private static void FreeProjection<TPlatform>(ref TPlatform platform,
		APTR list, APTR entries, uint entriesSize)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (entries.IsNotNull && entriesSize != 0 && platform.IsMapped(entries,
			entriesSize))
		{
			platform.Clear(entries, entriesSize);
			platform.Free(entries, entriesSize);
		}
		if (list.IsNotNull && platform.IsMapped(list, Amiga.List.Size))
		{
			platform.Clear(list, Amiga.List.Size);
			platform.Free(list, Amiga.List.Size);
		}
	}
}
