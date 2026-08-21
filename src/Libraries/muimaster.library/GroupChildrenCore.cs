/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupForwardState
{
	public const uint Magic = 0x47465744; // "GFWD"
	public const uint Size = 16;
	public uint Cookie;
	public uint Forward;
	public uint ForwardDepth;
	public uint ForwardCount;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupChildListState
{
	public const uint Magic = 0x47434C53; // "GCLS"
	public const uint Size = 32;
	public uint Cookie;
	public APTR Group;
	public APTR List;
	public APTR Entries;
	public uint Count;
	public uint Capacity;
	public uint Mutation;
	public uint Generation;
}

internal static class MuiGroupForwardStateCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupForwardState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupForwardState.Size)) return false;
		return MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGroupRecordKind.Forward, MuiGroupRecordField.Cookie,
			MuiGroupForwardState.Magic) &&
			MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupRecordKind.Forward, MuiGroupRecordField.Forward,
				value.Forward) &&
			MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupRecordKind.Forward, MuiGroupRecordField.ForwardDepth,
				value.ForwardDepth) &&
			MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupRecordKind.Forward, MuiGroupRecordField.ForwardCount,
				value.ForwardCount);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupForwardState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupForwardState.Size) ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupRecordKind.Forward, MuiGroupRecordField.Cookie,
				out var cookie) || cookie != MuiGroupForwardState.Magic ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupRecordKind.Forward, MuiGroupRecordField.Forward,
				out var forward) ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupRecordKind.Forward, MuiGroupRecordField.ForwardDepth,
				out var forwardDepth) ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupRecordKind.Forward, MuiGroupRecordField.ForwardCount,
				out var forwardCount)) return false;
		value.Cookie = MuiGroupForwardState.Magic;
		value.Forward = forward == 0 ? 0u : 1u;
		value.ForwardDepth = forwardDepth == 0 ? 0u : 1u;
		value.ForwardCount = forwardCount;
		return true;
	}
}

internal static class MuiGroupChildListStateCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupChildListState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupChildListState.Size) || value.Capacity < value.Count)
			return false;
		return MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGroupRecordKind.ChildList, MuiGroupRecordField.Cookie,
			MuiGroupChildListState.Magic) &&
			MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupRecordKind.ChildList, MuiGroupRecordField.Group,
				value.Group.Raw) &&
			MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupRecordKind.ChildList, MuiGroupRecordField.List,
				value.List.Raw) &&
			MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupRecordKind.ChildList, MuiGroupRecordField.Entries,
				value.Entries.Raw) &&
			MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupRecordKind.ChildList, MuiGroupRecordField.Count,
				value.Count) &&
			MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupRecordKind.ChildList, MuiGroupRecordField.Capacity,
				value.Capacity) &&
			MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupRecordKind.ChildList, MuiGroupRecordField.Mutation,
				value.Mutation) &&
			MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupRecordKind.ChildList, MuiGroupRecordField.Generation,
				value.Generation);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupChildListState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupChildListState.Size) ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupRecordKind.ChildList, MuiGroupRecordField.Cookie,
				out var cookie) || cookie != MuiGroupChildListState.Magic ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupRecordKind.ChildList, MuiGroupRecordField.Group,
				out var group) ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupRecordKind.ChildList, MuiGroupRecordField.List,
				out var list) ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupRecordKind.ChildList, MuiGroupRecordField.Entries,
				out var entries) ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupRecordKind.ChildList, MuiGroupRecordField.Count,
				out value.Count) ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupRecordKind.ChildList, MuiGroupRecordField.Capacity,
				out value.Capacity) ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupRecordKind.ChildList, MuiGroupRecordField.Mutation,
				out value.Mutation) ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupRecordKind.ChildList, MuiGroupRecordField.Generation,
				out value.Generation)) return false;
		value.Cookie = MuiGroupChildListState.Magic;
		value.Group = APTR.FromPointer(group);
		value.List = APTR.FromPointer(list);
		value.Entries = APTR.FromPointer(entries);
		return value.Capacity >= value.Count;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiGroupForwardRecordInput
{
	public uint Forward;
	public uint ForwardDepth;
	public uint ForwardCount;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiGroupChildListStateInput
{
	public APTR Group;
	public APTR List;
	public APTR Entries;
	public uint Count;
	public uint Capacity;
	public uint Mutation;
	public uint Generation;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupChildListEntry
{
	public const uint Size = 16;
	public const uint ProjectionMagic = 0x47454E54; // "GENT"
	public APTR Next;
	public APTR Previous;
	public APTR Object;
	public APTR Reserved;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupChildListEntryCursor
{
	internal const uint EntrySize = MuiGroupChildListEntry.Size;
	internal APTR Base;
	internal uint Index;
}

internal static class MuiGroupChildListEntryVectorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiGroupChildListEntryCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiGroupChildListEntryCursor.EntrySize) return false;
		var offset = cursor.Index *
			MuiGroupChildListEntryCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address,
			MuiGroupChildListEntryCursor.EntrySize);
	}
}

internal static class MuiGroupChildListEntryCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupChildListEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupChildListEntry.Size)) return false;
		if (!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiGroupRecordKind.ChildEntry, MuiGroupRecordField.Next,
			out var next) ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupRecordKind.ChildEntry, MuiGroupRecordField.Previous,
				out var previous) ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupRecordKind.ChildEntry, MuiGroupRecordField.Object,
				out var @object) ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupRecordKind.ChildEntry, MuiGroupRecordField.Reserved,
				out var reserved)) return false;
		value.Next = APTR.FromPointer(next);
		value.Previous = APTR.FromPointer(previous);
		value.Object = APTR.FromPointer(@object);
		value.Reserved = APTR.FromPointer(reserved);
		return value.Reserved.Raw == MuiGroupChildListEntry.ProjectionMagic;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupChildListEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupChildListEntry.Size)) return false;
		return MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiGroupRecordKind.ChildEntry, MuiGroupRecordField.Next,
			value.Next.Raw) &&
			MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupRecordKind.ChildEntry, MuiGroupRecordField.Previous,
				value.Previous.Raw) &&
			MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupRecordKind.ChildEntry, MuiGroupRecordField.Object,
				value.Object.Raw) &&
			MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupRecordKind.ChildEntry, MuiGroupRecordField.Reserved,
				value.Reserved.Raw);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupExecListRecord
{
	internal const uint Size = Amiga.List.Size;
	internal APTR Head;
	internal APTR Tail;
	internal APTR TailPred;
	internal NodeType Type;
	internal byte Padding;
}

internal enum MuiGroupRecordKind : byte
{
	Forward,
	ChildList,
	ChildEntry,
	ExecList,
}

internal enum MuiGroupRecordField : byte
{
	Cookie,
	Forward,
	ForwardDepth,
	ForwardCount,
	Group,
	List,
	Entries,
	Count,
	Capacity,
	Mutation,
	Generation,
	Next,
	Previous,
	Object,
	Reserved,
	Head,
	Tail,
	TailPred,
	Type,
	Padding,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupRecordFieldCursor
{
	internal APTR Address;
	internal MuiGroupRecordKind Record;
	internal MuiGroupRecordField Field;
}

internal static class MuiGroupRecordFieldCursorCodec
{
	private static bool TryResolve(MuiGroupRecordKind record,
		MuiGroupRecordField field, out uint offset, out uint size,
		out uint fieldSize)
	{
		offset = 0;
		size = 0;
		fieldSize = 0;
		switch (record)
		{
			case MuiGroupRecordKind.Forward:
				size = MuiGroupForwardState.Size;
				offset = field switch
				{
					MuiGroupRecordField.Cookie => 0,
					MuiGroupRecordField.Forward => 4,
					MuiGroupRecordField.ForwardDepth => 8,
					MuiGroupRecordField.ForwardCount => 12,
					_ => uint.MaxValue,
				};
				fieldSize = 4;
				break;
			case MuiGroupRecordKind.ChildList:
				size = MuiGroupChildListState.Size;
				offset = field switch
				{
					MuiGroupRecordField.Cookie => 0,
					MuiGroupRecordField.Group => 4,
					MuiGroupRecordField.List => 8,
					MuiGroupRecordField.Entries => 12,
					MuiGroupRecordField.Count => 16,
					MuiGroupRecordField.Capacity => 20,
					MuiGroupRecordField.Mutation => 24,
					MuiGroupRecordField.Generation => 28,
					_ => uint.MaxValue,
				};
				fieldSize = 4;
				break;
			case MuiGroupRecordKind.ChildEntry:
				size = MuiGroupChildListEntry.Size;
				offset = field switch
				{
					MuiGroupRecordField.Next => 0,
					MuiGroupRecordField.Previous => 4,
					MuiGroupRecordField.Object => 8,
					MuiGroupRecordField.Reserved => 12,
					_ => uint.MaxValue,
				};
				fieldSize = 4;
				break;
			case MuiGroupRecordKind.ExecList:
				size = MuiGroupExecListRecord.Size;
				offset = field switch
				{
					MuiGroupRecordField.Head => 0,
					MuiGroupRecordField.Tail => 4,
					MuiGroupRecordField.TailPred => 8,
					MuiGroupRecordField.Type => 12,
					MuiGroupRecordField.Padding => 13,
					_ => uint.MaxValue,
				};
				fieldSize = field == MuiGroupRecordField.Type ||
					field == MuiGroupRecordField.Padding ? 1u : 4u;
				break;
		}
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupRecordFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolve(cursor.Record, cursor.Field, out var offset,
			out var size, out fieldSize) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, size)) return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupRecordKind record, MuiGroupRecordField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiGroupRecordFieldCursor);
		cursor.Address = address;
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
			out var fieldSize) || fieldSize != 4) return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupRecordKind record, MuiGroupRecordField field,
		uint value) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGroupRecordFieldCursor);
		cursor.Address = address;
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
			out var fieldSize) || fieldSize != 4) return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupRecordKind record, MuiGroupRecordField field,
		out byte value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiGroupRecordFieldCursor);
		cursor.Address = address;
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
			out var fieldSize) || fieldSize != 1) return false;
		value = platform.ReadUInt8(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupRecordKind record, MuiGroupRecordField field,
		byte value) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGroupRecordFieldCursor);
		cursor.Address = address;
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
			out var fieldSize) || fieldSize != 1) return false;
		platform.WriteUInt8(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiGroupExecListCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupExecListRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupExecListRecord.Size)) return false;
		if (!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiGroupRecordKind.ExecList, MuiGroupRecordField.Head,
			out var head) ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupRecordKind.ExecList, MuiGroupRecordField.Tail,
				out var tail) ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupRecordKind.ExecList, MuiGroupRecordField.TailPred,
				out var tailPred) ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt8(ref platform, address,
				MuiGroupRecordKind.ExecList, MuiGroupRecordField.Type,
				out var type) ||
			!MuiGroupRecordFieldCursorCodec.TryReadUInt8(ref platform, address,
				MuiGroupRecordKind.ExecList, MuiGroupRecordField.Padding,
				out value.Padding)) return false;
		value.Head = APTR.FromPointer(head);
		value.Tail = APTR.FromPointer(tail);
		value.TailPred = APTR.FromPointer(tailPred);
		value.Type = (NodeType)type;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupExecListRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupExecListRecord.Size)) return false;
		return MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiGroupRecordKind.ExecList, MuiGroupRecordField.Head,
			value.Head.Raw) &&
			MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupRecordKind.ExecList, MuiGroupRecordField.Tail,
				value.Tail.Raw) &&
			MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupRecordKind.ExecList, MuiGroupRecordField.TailPred,
				value.TailPred.Raw) &&
			MuiGroupRecordFieldCursorCodec.TryWriteUInt8(ref platform, address,
				MuiGroupRecordKind.ExecList, MuiGroupRecordField.Type,
				(byte)value.Type) &&
			MuiGroupRecordFieldCursorCodec.TryWriteUInt8(ref platform, address,
				MuiGroupRecordKind.ExecList, MuiGroupRecordField.Padding,
				value.Padding);
	}
}

// MorphOS Group child ownership, child-count publication, and SetAttrs
// forwarding. The object topology remains in the existing Family records;
// this boundary only adds the Group-specific ABI semantics around that typed
// topology. Forwarding is deliberately bounded by the same traversal ceiling
// as Family, Notify, and layout walks.
public static class MuiGroupChildrenCore
{
	public const uint Child = 0x804226E6;
	public const uint FamilyChild = 0x8042C696;
	public const uint ChildCount = 0x80420322;
	public const uint ChildList = 0x80424748;
	public const uint FamilyChildCount = 0x8042B25A;
	public const uint FamilyList = 0x80424B9E;
	public const uint Forward = 0x80421422;
	public const uint ForwardDepth = 0x80428488;

	private const uint StateAttribute = 0x7FFE0042;
	private const uint ChildListStateAttribute = 0x7FFE0043;

	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == ChildCount || attribute == ChildList;

	internal static bool IsFamilyPublicGetterAttribute(uint attribute) =>
		attribute == FamilyChildCount || attribute == FamilyList;

	internal static bool TryGetFamily<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, out uint value, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		handled = false;
		if (!IsFamilyPublicGetterAttribute(attribute) ||
			!MuiFamilyCore.IsFamilyObject(ref platform, state, obj)) return false;
		handled = true;
		if (attribute == FamilyChildCount)
		{
			value = CountChildren(ref platform, state, obj);
			return true;
		}
		var list = EnsureChildList(ref platform, state, obj);
		value = list.Raw;
		return list.IsNotNull;
	}

	internal static bool TrySet<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint attribute, uint value, bool notify, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		handled = false;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return false;
		var obj = objectValue.Boopsi;
		if (obj.IsNull) return false;

		if (attribute == FamilyChild)
		{
			handled = true;
			if (MuiHeadlessObjectCore.IsObjectInitialized(ref platform, record) ||
				!MuiFamilyCore.IsFamilyObject(ref platform, state, obj)) return false;
			var child = APTR.FromPointer(value);
			return child.IsNotNull && MuiFamilyCore.AddTail(ref platform, state,
				obj, child);
		}
		if (attribute == Child && MuiFamilyCore.IsFamilyObject(ref platform,
			state, obj) && !MuiGroupChangeCore.IsGroupObject(ref platform, state,
			obj))
		{
			// MorphOS documents MUIA_Group_Child as a Family_Child alias. Keep
			// the alias initialize-only for non-Group Family classes; the existing
			// Group path below retains its established behavior.
			handled = true;
			if (MuiHeadlessObjectCore.IsObjectInitialized(ref platform, record))
				return false;
			var child = APTR.FromPointer(value);
			return child.IsNotNull && MuiFamilyCore.AddTail(ref platform, state,
				obj, child);
		}
		if (!MuiGroupChangeCore.IsGroupObject(ref platform, state, obj)) return false;

		if (attribute == Child)
		{
			handled = true;
			var child = APTR.FromPointer(value);
			if (child.IsNull) return false;
			return MuiFamilyCore.AddTail(ref platform, state, obj, child);
		}

		// MUIA_Group_ChildList is a read-only [..G] projection.  A caller must
		// use OM_ADDMEMBER/OM_REMMEMBER (the Family seam), never replace the
		// returned list pointer through SetAttrs.
		if (attribute == ChildList)
		{
			handled = true;
			return false;
		}

		if (attribute == Forward || attribute == ForwardDepth)
		{
			handled = true;
			return SetForwardState(ref platform, state, record, attribute,
				value, notify);
		}

		if (!TryReadForwardState(ref platform, record, out var forward) ||
			forward.Forward == 0) return false;
		handled = true;
		return ForwardAttribute(ref platform, state, obj, attribute, value,
			notify, forward.ForwardDepth != 0);
	}

	internal static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		handled = false;
		if (!MuiGroupChangeCore.IsGroupObject(ref platform, state, obj))
			return false;
		if (attribute == ChildCount)
		{
			handled = true;
			value = CountChildren(ref platform, state, obj);
			return true;
		}
		if (attribute == ChildList)
		{
			handled = true;
			var list = EnsureChildList(ref platform, state, obj);
			value = list.Raw;
			return list.IsNotNull;
		}
		if (attribute != Forward && attribute != ForwardDepth) return false;
		handled = true;
		if (TryReadForwardState(ref platform,
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj),
			out var forward))
		{
			value = attribute == Forward ? forward.Forward : forward.ForwardDepth;
			return true;
		}
		return MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			attribute, out value);
	}

	// Struct-first native qualification seam for the forward state record.
	public static bool WriteForwardRecord<TPlatform>(ref TPlatform platform,
		APTR storage, uint forward, uint depth, uint requests)
		where TPlatform : struct, IMuiGuestMemory
	{
		var input = default(MuiGroupForwardRecordInput);
		input.Forward = forward;
		input.ForwardDepth = depth;
		input.ForwardCount = requests;
		return WriteForwardRecord(ref platform, storage, input);
	}

	public static bool WriteForwardRecord<TPlatform>(ref TPlatform platform,
		APTR storage, MuiGroupForwardRecordInput input)
		where TPlatform : struct, IMuiGuestMemory
	{
		var value = default(MuiGroupForwardState);
		value.Cookie = MuiGroupForwardState.Magic;
		value.Forward = input.Forward;
		value.ForwardDepth = input.ForwardDepth;
		value.ForwardCount = input.ForwardCount;
		return MuiGroupForwardStateCodec.Write(ref platform, storage, value);
	}

	public static uint DispatchForwardRecord<TPlatform>(ref TPlatform platform,
		APTR storage) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGroupForwardStateCodec.TryRead(ref platform, storage,
			out var value)) return 0;
		return value.Forward ^ value.ForwardDepth ^ value.ForwardCount;
	}

	public static bool WriteChildListStateRecord<TPlatform>(
		ref TPlatform platform, APTR storage, MuiGroupChildListStateInput input)
		where TPlatform : struct, IMuiGuestMemory
	{
		var value = default(MuiGroupChildListState);
		value.Cookie = MuiGroupChildListState.Magic;
		value.Group = input.Group;
		value.List = input.List;
		value.Entries = input.Entries;
		value.Count = input.Count;
		value.Capacity = input.Capacity;
		value.Mutation = input.Mutation;
		value.Generation = input.Generation;
		return MuiGroupChildListStateCodec.Write(ref platform, storage, value);
	}

	public static uint DispatchChildListStateRecord<TPlatform>(
		ref TPlatform platform, APTR storage) where TPlatform : struct,
		IMuiGuestMemory
	{
		if (!MuiGroupChildListStateCodec.TryRead(ref platform, storage,
			out var value)) return 0;
		return value.Group.Raw ^ value.List.Raw ^ value.Entries.Raw ^
			value.Count ^ value.Capacity ^ value.Mutation ^ value.Generation;
	}

	// Struct-first native qualification seam for the read-only List header and
	// its two-entry projection. Production callers obtain this view from
	// MUIA_Group_ChildList; the fixed storage form keeps the freestanding ABI
	// test independent of the headless object's broader dispatcher closure.
	public static bool WriteChildListRecord<TPlatform>(ref TPlatform platform,
		APTR listStorage, APTR entriesStorage, APTR first, APTR second)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (listStorage.IsNull || entriesStorage.IsNull || !platform.IsMapped(
			listStorage, Amiga.List.Size) || !platform.IsMapped(entriesStorage,
			MuiGroupChildListEntry.Size * 2)) return false;
		var cursor = default(MuiGroupChildListEntryCursor);
		cursor.Base = entriesStorage;
		if (!MuiGroupChildListEntryVectorCodec.TryGetEntry(ref platform, cursor,
			out var firstEntry)) return false;
		cursor.Index = 1;
		if (!MuiGroupChildListEntryVectorCodec.TryGetEntry(ref platform, cursor,
			out var secondEntry)) return false;
		WriteEntry(ref platform, firstEntry, secondEntry, APTR.Null, first);
		WriteEntry(ref platform, secondEntry, APTR.Null, firstEntry, second);
		var list = default(Amiga.List);
		list.Head = firstEntry;
		list.Tail = APTR.Null;
		list.TailPred = secondEntry;
		list.Type = NodeType.Unknown;
		return WriteList(ref platform, listStorage, list);
	}

	internal static void Cleanup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		CleanupChildList(ref platform, state, obj);
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull || !TryReadForwardState(ref platform, record,
			out _)) return;
		var block = APTR.FromPointer(ReadPrivateAttribute(ref platform, record));
		if (block.IsNull || !platform.IsMapped(block,
			MuiGroupForwardState.Size)) return;
		platform.Clear(block, MuiGroupForwardState.Size);
		platform.Free(block, MuiGroupForwardState.Size);
		MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
			StateAttribute, 0, false);
	}

	// Read-only NextObject-compatible traversal for the typed projection.  The
	// public MorphOS contract still routes through intuition.library/NextObject;
	// this helper is the local ABI seam used until CopperStart's intuition
	// vector is wired to understand the same guest object representation. The
	// cursor is initialized from Exec List.Head and is advanced to the next
	// projected entry, with no managed enumerator or object allocation.
	public static APTR NextObject<TPlatform>(ref TPlatform platform, APTR list,
		ref uint cursorRaw) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = APTR.FromPointer(cursorRaw);
		if (list.IsNull || !platform.IsMapped(list, Amiga.List.Size) ||
			cursor.IsNull || !platform.IsMapped(cursor,
			MuiGroupChildListEntry.Size)) return APTR.Null;
		if (!MuiGroupChildListEntryCodec.TryRead(ref platform, cursor,
			out var entry)) return APTR.Null;
		var obj = entry.Object;
		cursorRaw = entry.Next.Raw;
		return obj;
	}

	private static bool SetForwardState<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = EnsureForwardState(ref platform, state, record);
		if (block.IsNull || !TryReadState(ref platform, block, out var current))
			return false;
		if (attribute == Forward) current.Forward = value == 0 ? 0u : 1u;
		else current.ForwardDepth = value == 0 ? 0u : 1u;
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, attribute, value == 0 ? 0u : 1u, false)) return false;
		current.ForwardCount = current.ForwardCount == uint.MaxValue
			? uint.MaxValue : current.ForwardCount + 1;
		WriteState(ref platform, block, current);
		MuiHeadlessMemory.Mutated(ref platform, state);
		if (notify) MuiNotifyCore.DispatchAttributeChange(ref platform, state,
			record, attribute, value == 0 ? 0u : 1u);
		return true;
	}

	private static bool ForwardAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, uint attribute, uint value, bool notify,
		bool recursive) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = CountChildren(ref platform, state, group);
		var success = true;
		for (var index = 0u; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group,
				unchecked((int)index), APTR.Null);
			if (child.IsNull || !MuiHeadlessObjectCore.SetAttribute(ref platform,
				state, child, attribute, value, notify)) success = false;
			if (recursive && child.IsNotNull &&
				MuiGroupChangeCore.IsGroupObject(ref platform, state, child) &&
				!ForwardAttribute(ref platform, state, child, attribute, value,
					notify, true)) success = false;
		}
		if (TryReadForwardState(ref platform,
			MuiHeadlessObjectCore.FindObject(ref platform, state, group),
			out var current))
		{
			current.ForwardCount = current.ForwardCount == uint.MaxValue
				? uint.MaxValue : current.ForwardCount + 1;
			var record = MuiHeadlessObjectCore.FindObject(ref platform, state,
				group);
			var block = APTR.FromPointer(ReadPrivateAttribute(ref platform,
				record));
			if (TryReadState(ref platform, block, out _))
				WriteState(ref platform, block, current);
		}
		return success;
	}

	private static uint CountChildren<TPlatform>(ref TPlatform platform,
		APTR state, APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = 0u;
		while (count < MuiHeadlessLayout.MaximumTraversal &&
			MuiFamilyCore.GetChild(ref platform, state, group,
				unchecked((int)count), APTR.Null).IsNotNull) count++;
		return count;
	}

	private static APTR EnsureChildList<TPlatform>(ref TPlatform platform,
		APTR state, APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, group);
		if (record.IsNull) return APTR.Null;
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return APTR.Null;
		var mutation = stateValue.Mutation;
		var block = APTR.FromPointer(ReadPrivateAttribute(ref platform, record,
			ChildListStateAttribute));
		if (TryReadChildListState(ref platform, block, out var current) &&
			current.Group.Raw == group.Raw && current.Mutation == mutation &&
			current.List.IsNotNull && platform.IsMapped(current.List,
				Amiga.List.Size)) return current.List;

		var count = CountChildren(ref platform, state, group);
		if (count > uint.MaxValue / MuiGroupChildListEntry.Size) return APTR.Null;
		var list = MuiHeadlessMemory.Allocate(ref platform, Amiga.List.Size);
		if (list.IsNull) return APTR.Null;
		var entriesSize = count * MuiGroupChildListEntry.Size;
		var entries = entriesSize == 0 ? APTR.Null :
			MuiHeadlessMemory.Allocate(ref platform, entriesSize);
		if (entriesSize != 0 && entries.IsNull)
		{
			platform.Clear(list, Amiga.List.Size);
			platform.Free(list, Amiga.List.Size);
			return APTR.Null;
		}

		var previous = APTR.Null;
		for (var index = 0u; index < count; index++)
		{
			var cursor = default(MuiGroupChildListEntryCursor);
			cursor.Base = entries;
			cursor.Index = index;
			if (!MuiGroupChildListEntryVectorCodec.TryGetEntry(ref platform,
				cursor, out var entry))
			{
				FreeChildListProjection(ref platform, list, entries, entriesSize);
				return APTR.Null;
			}
			var child = MuiFamilyCore.GetChild(ref platform, state, group,
				unchecked((int)index), APTR.Null);
			var next = APTR.Null;
			if (index + 1 < count)
			{
				cursor.Index++;
				if (!MuiGroupChildListEntryVectorCodec.TryGetEntry(ref platform,
					cursor, out next))
				{
					FreeChildListProjection(ref platform, list, entries,
						entriesSize);
					return APTR.Null;
				}
			}
			WriteEntry(ref platform, entry, next, previous, child);
			previous = entry;
		}
		var listValue = default(Amiga.List);
		listValue.Head = count == 0 ? APTR.Null : entries;
		listValue.Tail = APTR.Null;
		listValue.TailPred = count == 0 ? APTR.Null : previous;
		listValue.Type = NodeType.Unknown;
		listValue.Padding = 0;
		if (!WriteList(ref platform, list, listValue))
		{
			FreeChildListProjection(ref platform, list, entries, entriesSize);
			return APTR.Null;
		}

		var replacement = default(MuiGroupChildListState);
		replacement.Cookie = MuiGroupChildListState.Magic;
		replacement.Group = group;
		replacement.List = list;
		replacement.Entries = entries;
		replacement.Count = count;
		replacement.Capacity = count;
		replacement.Mutation = mutation;
		replacement.Generation = MuiHeadlessMemory.NextSequence(ref platform,
			state);
		var oldBlock = block;
		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiGroupChildListState.Size);
		if (block.IsNull)
		{
			if (block.IsNotNull)
			{
				platform.Clear(block, MuiGroupChildListState.Size);
				platform.Free(block, MuiGroupChildListState.Size);
			}
			FreeChildListProjection(ref platform, list, entries, entriesSize);
			return APTR.Null;
		}
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, ChildListStateAttribute, block.Raw, false))
		{
			platform.Clear(block, MuiGroupChildListState.Size);
			platform.Free(block, MuiGroupChildListState.Size);
			FreeChildListProjection(ref platform, list, entries, entriesSize);
			return APTR.Null;
		}
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out stateValue)) return APTR.Null;
		replacement.Mutation = stateValue.Mutation;
		WriteChildListState(ref platform, block, replacement);
		FreeChildListStateBlock(ref platform, oldBlock);
		return list;
	}

	private static void CleanupChildList<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return;
		var block = APTR.FromPointer(ReadPrivateAttribute(ref platform, record,
			ChildListStateAttribute));
		FreeChildListStateBlock(ref platform, block);
		MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
			ChildListStateAttribute, 0, false);
	}

	private static void FreeChildListStateBlock<TPlatform>(ref TPlatform platform,
		APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadChildListState(ref platform, block, out var value)) return;
		var entriesSize = value.Count > uint.MaxValue /
			MuiGroupChildListEntry.Size ? 0u : value.Count *
			MuiGroupChildListEntry.Size;
		FreeChildListProjection(ref platform, value.List, value.Entries,
			entriesSize);
		platform.Clear(block, MuiGroupChildListState.Size);
		platform.Free(block, MuiGroupChildListState.Size);
	}

	private static void FreeChildListProjection<TPlatform>(ref TPlatform platform,
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

	private static void WriteEntry<TPlatform>(ref TPlatform platform, APTR entry,
		APTR next, APTR previous, APTR obj) where TPlatform : struct, IMuiGuestMemory
	{
		var value = default(MuiGroupChildListEntry);
		value.Next = next;
		value.Previous = previous;
		value.Object = obj;
		value.Reserved = APTR.FromPointer(MuiGroupChildListEntry.ProjectionMagic);
		MuiGroupChildListEntryCodec.Write(ref platform, entry, value);
	}

	private static bool WriteList<TPlatform>(ref TPlatform platform, APTR address,
		Amiga.List value) where TPlatform : struct, IMuiGuestMemory
	{
		var record = default(MuiGroupExecListRecord);
		record.Head = value.Head;
		record.Tail = value.Tail;
		record.TailPred = value.TailPred;
		record.Type = value.Type;
		record.Padding = value.Padding;
		return MuiGroupExecListCodec.Write(ref platform, address, record);
	}

	private static bool TryReadChildListState<TPlatform>(ref TPlatform platform,
		APTR block, out MuiGroupChildListState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupChildListStateCodec.TryRead(ref platform, block, out value);

	private static void WriteChildListState<TPlatform>(ref TPlatform platform,
		APTR block, MuiGroupChildListState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupChildListStateCodec.Write(ref platform, block, value);

	private static APTR EnsureForwardState<TPlatform>(ref TPlatform platform,
		APTR state, APTR record) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(ReadPrivateAttribute(ref platform, record));
		if (TryReadState(ref platform, block, out _)) return block;
		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiGroupForwardState.Size);
		if (block.IsNull) return APTR.Null;
		var value = default(MuiGroupForwardState);
		value.Cookie = MuiGroupForwardState.Magic;
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, StateAttribute, block.Raw, false))
		{
			platform.Clear(block, MuiGroupForwardState.Size);
			platform.Free(block, MuiGroupForwardState.Size);
			return APTR.Null;
		}
		WriteState(ref platform, block, value);
		return block;
	}

	private static uint ReadPrivateAttribute<TPlatform>(ref TPlatform platform,
		APTR record, uint attribute = StateAttribute)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return 0;
		var item = objectValue.Attributes;
		var visited = 0u;
		while (item.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessAttributeCodec.TryRead(ref platform, item,
				out var attributeValue)) return 0;
			if (attributeValue.Id == attribute) return attributeValue.Value;
			item = attributeValue.Next;
		}
		return 0;
	}

	private static bool TryReadForwardState<TPlatform>(ref TPlatform platform,
		APTR record, out MuiGroupForwardState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		var block = APTR.FromPointer(ReadPrivateAttribute(ref platform, record));
		return TryReadState(ref platform, block, out value);
	}

	private static void WriteState<TPlatform>(ref TPlatform platform, APTR block,
		MuiGroupForwardState value) where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupForwardStateCodec.Write(ref platform, block, value);

	private static bool TryReadState<TPlatform>(ref TPlatform platform, APTR block,
		out MuiGroupForwardState value) where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupForwardStateCodec.TryRead(ref platform, block, out value);
}
