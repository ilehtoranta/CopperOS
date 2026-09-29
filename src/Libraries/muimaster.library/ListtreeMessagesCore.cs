/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Central codec for the fixed MorphOS 3.20 Listtree.mcc packet family. The
// standalone external dispatcher consumes the named records declared next to
// its public surface; only this adapter owns their packed guest boundaries.
internal enum MuiListtreePacketKind : byte
{
	Method,
	Set,
	Get,
	Insert,
	Remove,
	GetEntry,
	OpenClose,
	Sort,
	GetNr,
	MoveExchange,
	Rename,
	FindName,
	DropMark,
	TestPos,
}

internal enum MuiListtreeField : byte
{
	MethodId,
	Attribute,
	Value,
	Storage,
	Name,
	User,
	ListNode,
	PrevNode,
	Flags,
	Node,
	TreeNode,
	Position,
	OldListNode,
	OldTreeNode,
	NewListNode,
	NewTreeNode,
	NewName,
	X,
	Y,
	Entry,
	Values,
	Result,
}

[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeFieldCursor
{
	internal APTR Message;
	internal MuiListtreePacketKind Packet;
	internal MuiListtreeField Field;
}

internal static class MuiListtreeFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListtreeFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return MuiListtreeMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Packet, cursor.Field, out address);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListtreeFieldCursor cursor, out APTR address, out uint size)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Packet, cursor.Field, out address, out size);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreePacketKind packet, MuiListtreeField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiListtreeFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreePacketKind packet, MuiListtreeField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiListtreeFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Named packet adapters keep every fixed Listtree record as a semantic
// struct. Only this bounded layer translates MorphOS guest boundaries; the
// legacy cursor remains available for compatibility callers but is not needed
// by the live packet codecs.
internal static class MuiListtreeMessageMemoryCodec
{
	private static bool TryGetPacketSize(MuiListtreePacketKind packet,
		out uint size)
	{
		switch (packet)
		{
			case MuiListtreePacketKind.Method:
				size = MuiListtreeMethodMessage.Size;
				return true;
			case MuiListtreePacketKind.Set:
				size = MuiListtreeSetMessage.Size;
				return true;
			case MuiListtreePacketKind.Get:
				size = MuiListtreeGetMessage.Size;
				return true;
			case MuiListtreePacketKind.GetEntry:
				size = MuiListtreeGetEntryMessage.Size;
				return true;
			case MuiListtreePacketKind.Insert:
				size = MuiListtreeInsertMessage.Size;
				return true;
			case MuiListtreePacketKind.Remove:
				size = MuiListtreeRemoveMessage.Size;
				return true;
			case MuiListtreePacketKind.OpenClose:
				size = MuiListtreeOpenCloseMessage.Size;
				return true;
			case MuiListtreePacketKind.Sort:
				size = MuiListtreeSortMessage.Size;
				return true;
			case MuiListtreePacketKind.GetNr:
				size = MuiListtreeGetNrMessage.Size;
				return true;
			case MuiListtreePacketKind.MoveExchange:
				size = MuiListtreeMoveExchangeMessage.Size;
				return true;
			case MuiListtreePacketKind.Rename:
				size = MuiListtreeRenameMessage.Size;
				return true;
			case MuiListtreePacketKind.FindName:
				size = MuiListtreeFindNameMessage.Size;
				return true;
			case MuiListtreePacketKind.DropMark:
				size = MuiListtreeDropMarkMessage.Size;
				return true;
			case MuiListtreePacketKind.TestPos:
				size = MuiListtreeTestPosMessage.Size;
				return true;
		}
		size = 0;
		return false;
	}

	// Field addressing follows each packet's named declaration order. The
	// bounded guest struct cursor owns complete packet admission.
	private static bool TryResolveFieldIndex(MuiListtreePacketKind packet,
		MuiListtreeField field, out uint index)
	{
		index = uint.MaxValue;
		switch (packet)
		{
			case MuiListtreePacketKind.Method:
				if (field == MuiListtreeField.MethodId) index = 0;
				break;
			case MuiListtreePacketKind.Set:
				if (field == MuiListtreeField.MethodId) index = 0;
				else if (field == MuiListtreeField.Attribute) index = 1;
				else if (field == MuiListtreeField.Value) index = 2;
				break;
			case MuiListtreePacketKind.Get:
				if (field == MuiListtreeField.MethodId) index = 0;
				else if (field == MuiListtreeField.Attribute) index = 1;
				else if (field == MuiListtreeField.Storage) index = 2;
				break;
			case MuiListtreePacketKind.Insert:
				if (field == MuiListtreeField.MethodId) index = 0;
				else if (field == MuiListtreeField.Name) index = 1;
				else if (field == MuiListtreeField.User) index = 2;
				else if (field == MuiListtreeField.ListNode) index = 3;
				else if (field == MuiListtreeField.PrevNode) index = 4;
				else if (field == MuiListtreeField.Flags) index = 5;
				break;
			case MuiListtreePacketKind.Remove:
				if (field == MuiListtreeField.MethodId) index = 0;
				else if (field == MuiListtreeField.ListNode) index = 1;
				else if (field == MuiListtreeField.TreeNode) index = 2;
				else if (field == MuiListtreeField.Flags) index = 3;
				break;
			case MuiListtreePacketKind.GetEntry:
				if (field == MuiListtreeField.MethodId) index = 0;
				else if (field == MuiListtreeField.Node) index = 1;
				else if (field == MuiListtreeField.Position) index = 2;
				else if (field == MuiListtreeField.Flags) index = 3;
				break;
			case MuiListtreePacketKind.OpenClose:
				if (field == MuiListtreeField.MethodId) index = 0;
				else if (field == MuiListtreeField.ListNode) index = 1;
				else if (field == MuiListtreeField.TreeNode) index = 2;
				else if (field == MuiListtreeField.Flags) index = 3;
				break;
			case MuiListtreePacketKind.Sort:
				if (field == MuiListtreeField.MethodId) index = 0;
				else if (field == MuiListtreeField.ListNode) index = 1;
				else if (field == MuiListtreeField.Flags) index = 2;
				break;
			case MuiListtreePacketKind.GetNr:
				if (field == MuiListtreeField.MethodId) index = 0;
				else if (field == MuiListtreeField.TreeNode) index = 1;
				else if (field == MuiListtreeField.Flags) index = 2;
				break;
			case MuiListtreePacketKind.MoveExchange:
				if (field == MuiListtreeField.MethodId) index = 0;
				else if (field == MuiListtreeField.OldListNode) index = 1;
				else if (field == MuiListtreeField.OldTreeNode) index = 2;
				else if (field == MuiListtreeField.NewListNode) index = 3;
				else if (field == MuiListtreeField.NewTreeNode) index = 4;
				else if (field == MuiListtreeField.Flags) index = 5;
				break;
			case MuiListtreePacketKind.Rename:
				if (field == MuiListtreeField.MethodId) index = 0;
				else if (field == MuiListtreeField.TreeNode) index = 1;
				else if (field == MuiListtreeField.NewName) index = 2;
				else if (field == MuiListtreeField.Flags) index = 3;
				break;
			case MuiListtreePacketKind.FindName:
				if (field == MuiListtreeField.MethodId) index = 0;
				else if (field == MuiListtreeField.ListNode) index = 1;
				else if (field == MuiListtreeField.Name) index = 2;
				else if (field == MuiListtreeField.Flags) index = 3;
				break;
			case MuiListtreePacketKind.DropMark:
				if (field == MuiListtreeField.MethodId) index = 0;
				else if (field == MuiListtreeField.Entry) index = 1;
				else if (field == MuiListtreeField.Values) index = 2;
				break;
			case MuiListtreePacketKind.TestPos:
				if (field == MuiListtreeField.MethodId) index = 0;
				else if (field == MuiListtreeField.X) index = 1;
				else if (field == MuiListtreeField.Y) index = 2;
				else if (field == MuiListtreeField.Result) index = 3;
				break;
		}
		return index != uint.MaxValue;
	}

	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiListtreePacketKind packet,
		MuiListtreeField field, out APTR address, out uint size)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		size = 0;
		if (!TryResolveFieldIndex(packet, field, out var index)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiListtreeMethodMessage.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				size = MuiListtreeMethodMessage.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreePacketKind packet, MuiListtreeField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryGetAddress(ref platform, message, packet, field,
			out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreePacketKind packet, MuiListtreeField field,
		out APTR address, out uint size)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		size = 0;
		if (!TryGetPacketSize(packet, out var packetSize) ||
			!MuiGuestStructCursor.TryCreate(ref platform, message, packetSize,
				out var cursor)) return false;
		return TryTakeField(ref platform, ref cursor, packet, field,
			out address, out size);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreePacketKind packet, MuiListtreeField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (packet == MuiListtreePacketKind.Method)
		{
			if (field != MuiListtreeField.MethodId ||
			!MuiGuestUlongStorageCodec.TryReadValue(ref platform, message,
				out value)) return false;
			return true;
		}
		if (packet == MuiListtreePacketKind.Set)
		{
			if (!MuiListtreeStructPacketCodec.TryReadSet(ref platform, message,
				out var set)) return false;
			if (field == MuiListtreeField.MethodId) value = set.MethodId;
			else if (field == MuiListtreeField.Attribute) value = set.Attribute;
			else if (field == MuiListtreeField.Value) value = set.Value;
			else return false;
			return true;
		}
		if (packet == MuiListtreePacketKind.Get)
		{
			if (!MuiListtreeStructPacketCodec.TryReadGet(ref platform, message,
				out var get)) return false;
			if (field == MuiListtreeField.MethodId) value = get.MethodId;
			else if (field == MuiListtreeField.Attribute) value = get.Attribute;
			else if (field == MuiListtreeField.Storage) value = get.Storage;
			else return false;
			return true;
		}
		if (packet == MuiListtreePacketKind.GetEntry)
		{
			if (!MuiListtreeStructPacketCodec.TryReadGetEntry(ref platform, message,
				out var entry)) return false;
			if (field == MuiListtreeField.MethodId) value = entry.MethodId;
			else if (field == MuiListtreeField.Node) value = entry.Node;
			else if (field == MuiListtreeField.Position) value = entry.Position;
			else if (field == MuiListtreeField.Flags) value = entry.Flags;
			else return false;
			return true;
		}
		if (packet == MuiListtreePacketKind.Insert)
		{
			if (!MuiListtreeStructPacketCodec.TryReadInsert(ref platform, message,
				out var insert)) return false;
			if (field == MuiListtreeField.MethodId) value = insert.MethodId;
			else if (field == MuiListtreeField.Name) value = insert.Name;
			else if (field == MuiListtreeField.User) value = insert.User;
			else if (field == MuiListtreeField.ListNode) value = insert.ListNode;
			else if (field == MuiListtreeField.PrevNode) value = insert.PrevNode;
			else if (field == MuiListtreeField.Flags) value = insert.Flags;
			else return false;
			return true;
		}
		if (packet == MuiListtreePacketKind.Remove)
		{
			if (!MuiListtreeStructPacketCodec.TryReadRemove(ref platform, message,
				out var remove)) return false;
			if (field == MuiListtreeField.MethodId) value = remove.MethodId;
			else if (field == MuiListtreeField.ListNode) value = remove.ListNode;
			else if (field == MuiListtreeField.TreeNode) value = remove.TreeNode;
			else if (field == MuiListtreeField.Flags) value = remove.Flags;
			else return false;
			return true;
		}
		if (packet == MuiListtreePacketKind.OpenClose)
		{
			if (!MuiListtreeStructPacketCodec.TryReadOpenClose(ref platform, message,
				out var openClose)) return false;
			if (field == MuiListtreeField.MethodId) value = openClose.MethodId;
			else if (field == MuiListtreeField.ListNode) value = openClose.ListNode;
			else if (field == MuiListtreeField.TreeNode) value = openClose.TreeNode;
			else if (field == MuiListtreeField.Flags) value = openClose.Flags;
			else return false;
			return true;
		}
		if (packet == MuiListtreePacketKind.Sort)
		{
			if (!MuiListtreeStructPacketCodec.TryReadSort(ref platform, message,
				out var sort)) return false;
			if (field == MuiListtreeField.MethodId) value = sort.MethodId;
			else if (field == MuiListtreeField.ListNode) value = sort.ListNode;
			else if (field == MuiListtreeField.Flags) value = sort.Flags;
			else return false;
			return true;
		}
		if (packet == MuiListtreePacketKind.GetNr)
		{
			if (!MuiListtreeStructPacketCodec.TryReadGetNr(ref platform, message,
				out var getNr)) return false;
			if (field == MuiListtreeField.MethodId) value = getNr.MethodId;
			else if (field == MuiListtreeField.TreeNode) value = getNr.TreeNode;
			else if (field == MuiListtreeField.Flags) value = getNr.Flags;
			else return false;
			return true;
		}
		if (packet == MuiListtreePacketKind.MoveExchange)
		{
			if (!MuiListtreeStructPacketCodec.TryReadMoveExchange(ref platform,
				message, out var move)) return false;
			if (field == MuiListtreeField.MethodId) value = move.MethodId;
			else if (field == MuiListtreeField.OldListNode) value = move.OldListNode;
			else if (field == MuiListtreeField.OldTreeNode) value = move.OldTreeNode;
			else if (field == MuiListtreeField.NewListNode) value = move.NewListNode;
			else if (field == MuiListtreeField.NewTreeNode) value = move.NewTreeNode;
			else if (field == MuiListtreeField.Flags) value = move.Flags;
			else return false;
			return true;
		}
		if (packet == MuiListtreePacketKind.Rename)
		{
			if (!MuiListtreeStructPacketCodec.TryReadRename(ref platform, message,
				out var rename)) return false;
			if (field == MuiListtreeField.MethodId) value = rename.MethodId;
			else if (field == MuiListtreeField.TreeNode) value = rename.TreeNode;
			else if (field == MuiListtreeField.NewName) value = rename.NewName;
			else if (field == MuiListtreeField.Flags) value = rename.Flags;
			else return false;
			return true;
		}
		if (packet == MuiListtreePacketKind.FindName)
		{
			if (!MuiListtreeStructPacketCodec.TryReadFindName(ref platform, message,
				out var find)) return false;
			if (field == MuiListtreeField.MethodId) value = find.MethodId;
			else if (field == MuiListtreeField.ListNode) value = find.ListNode;
			else if (field == MuiListtreeField.Name) value = find.Name;
			else if (field == MuiListtreeField.Flags) value = find.Flags;
			else return false;
			return true;
		}
		if (packet == MuiListtreePacketKind.DropMark)
		{
			if (!MuiListtreeStructPacketCodec.TryReadDropMark(ref platform, message,
				out var dropMark)) return false;
			if (field == MuiListtreeField.MethodId) value = dropMark.MethodId;
			else if (field == MuiListtreeField.Entry) value = dropMark.Entry;
			else if (field == MuiListtreeField.Values) value = dropMark.Values;
			else return false;
			return true;
		}
		if (packet == MuiListtreePacketKind.TestPos)
		{
			if (!MuiListtreeStructPacketCodec.TryReadTestPos(ref platform, message,
				out var testPos)) return false;
			if (field == MuiListtreeField.MethodId) value = testPos.MethodId;
			else if (field == MuiListtreeField.X) value = testPos.X;
			else if (field == MuiListtreeField.Y) value = testPos.Y;
			else if (field == MuiListtreeField.Result) value = testPos.Result;
			else return false;
			return true;
		}
		return false;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreePacketKind packet, MuiListtreeField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (packet == MuiListtreePacketKind.Method)
		{
			return field == MuiListtreeField.MethodId &&
				MuiGuestUlongStorageCodec.WriteValue(ref platform, message, value);
		}
		if (packet == MuiListtreePacketKind.Set)
		{
			if (!MuiListtreeStructPacketCodec.TryReadSet(ref platform, message,
				out var set)) return false;
			if (field == MuiListtreeField.MethodId) set.MethodId = value;
			else if (field == MuiListtreeField.Attribute) set.Attribute = value;
			else if (field == MuiListtreeField.Value) set.Value = value;
			else return false;
			return MuiListtreeStructPacketCodec.TryWriteSet(ref platform, message,
				set);
		}
		if (packet == MuiListtreePacketKind.Get)
		{
			if (!MuiListtreeStructPacketCodec.TryReadGet(ref platform, message,
				out var get)) return false;
			if (field == MuiListtreeField.MethodId) get.MethodId = value;
			else if (field == MuiListtreeField.Attribute) get.Attribute = value;
			else if (field == MuiListtreeField.Storage) get.Storage = value;
			else return false;
			return MuiListtreeStructPacketCodec.TryWriteGet(ref platform, message,
				get);
		}
		if (packet == MuiListtreePacketKind.GetEntry)
		{
			if (!MuiListtreeStructPacketCodec.TryReadGetEntry(ref platform, message,
				out var entry)) return false;
			if (field == MuiListtreeField.MethodId) entry.MethodId = value;
			else if (field == MuiListtreeField.Node) entry.Node = value;
			else if (field == MuiListtreeField.Position) entry.Position = value;
			else if (field == MuiListtreeField.Flags) entry.Flags = value;
			else return false;
			return MuiListtreeStructPacketCodec.TryWriteGetEntry(ref platform,
				message, entry);
		}
		if (packet == MuiListtreePacketKind.Insert)
		{
			if (!MuiListtreeStructPacketCodec.TryReadInsert(ref platform, message,
				out var insert)) return false;
			if (field == MuiListtreeField.MethodId) insert.MethodId = value;
			else if (field == MuiListtreeField.Name) insert.Name = value;
			else if (field == MuiListtreeField.User) insert.User = value;
			else if (field == MuiListtreeField.ListNode) insert.ListNode = value;
			else if (field == MuiListtreeField.PrevNode) insert.PrevNode = value;
			else if (field == MuiListtreeField.Flags) insert.Flags = value;
			else return false;
			return MuiListtreeStructPacketCodec.TryWriteInsert(ref platform, message,
				insert);
		}
		if (packet == MuiListtreePacketKind.Remove)
		{
			if (!MuiListtreeStructPacketCodec.TryReadRemove(ref platform, message,
				out var remove)) return false;
			if (field == MuiListtreeField.MethodId) remove.MethodId = value;
			else if (field == MuiListtreeField.ListNode) remove.ListNode = value;
			else if (field == MuiListtreeField.TreeNode) remove.TreeNode = value;
			else if (field == MuiListtreeField.Flags) remove.Flags = value;
			else return false;
			return MuiListtreeStructPacketCodec.TryWriteRemove(ref platform, message,
				remove);
		}
		if (packet == MuiListtreePacketKind.OpenClose)
		{
			if (!MuiListtreeStructPacketCodec.TryReadOpenClose(ref platform, message,
				out var openClose)) return false;
			if (field == MuiListtreeField.MethodId) openClose.MethodId = value;
			else if (field == MuiListtreeField.ListNode) openClose.ListNode = value;
			else if (field == MuiListtreeField.TreeNode) openClose.TreeNode = value;
			else if (field == MuiListtreeField.Flags) openClose.Flags = value;
			else return false;
			return MuiListtreeStructPacketCodec.TryWriteOpenClose(ref platform,
				message, openClose);
		}
		if (packet == MuiListtreePacketKind.Sort)
		{
			if (!MuiListtreeStructPacketCodec.TryReadSort(ref platform, message,
				out var sort)) return false;
			if (field == MuiListtreeField.MethodId) sort.MethodId = value;
			else if (field == MuiListtreeField.ListNode) sort.ListNode = value;
			else if (field == MuiListtreeField.Flags) sort.Flags = value;
			else return false;
			return MuiListtreeStructPacketCodec.TryWriteSort(ref platform, message,
				sort);
		}
		if (packet == MuiListtreePacketKind.GetNr)
		{
			if (!MuiListtreeStructPacketCodec.TryReadGetNr(ref platform, message,
				out var getNr)) return false;
			if (field == MuiListtreeField.MethodId) getNr.MethodId = value;
			else if (field == MuiListtreeField.TreeNode) getNr.TreeNode = value;
			else if (field == MuiListtreeField.Flags) getNr.Flags = value;
			else return false;
			return MuiListtreeStructPacketCodec.TryWriteGetNr(ref platform, message,
				getNr);
		}
		if (packet == MuiListtreePacketKind.MoveExchange)
		{
			if (!MuiListtreeStructPacketCodec.TryReadMoveExchange(ref platform,
				message, out var move)) return false;
			if (field == MuiListtreeField.MethodId) move.MethodId = value;
			else if (field == MuiListtreeField.OldListNode) move.OldListNode = value;
			else if (field == MuiListtreeField.OldTreeNode) move.OldTreeNode = value;
			else if (field == MuiListtreeField.NewListNode) move.NewListNode = value;
			else if (field == MuiListtreeField.NewTreeNode) move.NewTreeNode = value;
			else if (field == MuiListtreeField.Flags) move.Flags = value;
			else return false;
			return MuiListtreeStructPacketCodec.TryWriteMoveExchange(ref platform,
				message, move);
		}
		if (packet == MuiListtreePacketKind.Rename)
		{
			if (!MuiListtreeStructPacketCodec.TryReadRename(ref platform, message,
				out var rename)) return false;
			if (field == MuiListtreeField.MethodId) rename.MethodId = value;
			else if (field == MuiListtreeField.TreeNode) rename.TreeNode = value;
			else if (field == MuiListtreeField.NewName) rename.NewName = value;
			else if (field == MuiListtreeField.Flags) rename.Flags = value;
			else return false;
			return MuiListtreeStructPacketCodec.TryWriteRename(ref platform, message,
				rename);
		}
		if (packet == MuiListtreePacketKind.FindName)
		{
			if (!MuiListtreeStructPacketCodec.TryReadFindName(ref platform, message,
				out var find)) return false;
			if (field == MuiListtreeField.MethodId) find.MethodId = value;
			else if (field == MuiListtreeField.ListNode) find.ListNode = value;
			else if (field == MuiListtreeField.Name) find.Name = value;
			else if (field == MuiListtreeField.Flags) find.Flags = value;
			else return false;
			return MuiListtreeStructPacketCodec.TryWriteFindName(ref platform,
				message, find);
		}
		if (packet == MuiListtreePacketKind.DropMark)
		{
			if (!MuiListtreeStructPacketCodec.TryReadDropMark(ref platform, message,
				out var dropMark)) return false;
			if (field == MuiListtreeField.MethodId) dropMark.MethodId = value;
			else if (field == MuiListtreeField.Entry) dropMark.Entry = value;
			else if (field == MuiListtreeField.Values) dropMark.Values = value;
			else return false;
			return MuiListtreeStructPacketCodec.TryWriteDropMark(ref platform,
				message, dropMark);
		}
		if (packet == MuiListtreePacketKind.TestPos)
		{
			if (!MuiListtreeStructPacketCodec.TryReadTestPos(ref platform, message,
				out var testPos)) return false;
			if (field == MuiListtreeField.MethodId) testPos.MethodId = value;
			else if (field == MuiListtreeField.X) testPos.X = value;
			else if (field == MuiListtreeField.Y) testPos.Y = value;
			else if (field == MuiListtreeField.Result) testPos.Result = value;
			else return false;
			return MuiListtreeStructPacketCodec.TryWriteTestPos(ref platform, message,
				testPos);
		}
		return false;
	}
}

// Live Listtree packet transport is declaration-order based.  The public
// records are intentionally kept as named structs in ListtreeDispatcher.cs;
// this codec only performs the bounded guest-memory exchange for those
// structs.  The older field/offset adapters above remain available for
// malformed-packet diagnostics and compatibility tests.
internal static class MuiListtreeStructPacketCodec
{
	private static bool TryCreate<TPlatform>(ref TPlatform platform,
		APTR message, uint size, out MuiGuestStructCursor cursor)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, message, size,
			out cursor);

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeMethodHeaderCodec.TryReadValue(ref platform, message,
			out packet.MethodId);
	}

	internal static bool TryWriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeMethodHeaderCodec.WriteValue(ref platform, message, method);

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiListtreeSetMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Attribute) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Value) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteSet<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiListtreeSetMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Attribute) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Value) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiListtreeGetMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Attribute) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Storage) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteGet<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiListtreeGetMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Attribute) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Storage) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiListtreeGetEntryMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Node) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Position) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiListtreeGetEntryMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Node) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Position) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadInsert<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeInsertMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiListtreeInsertMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Name) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.User) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.ListNode) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.PrevNode) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteInsert<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeInsertMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiListtreeInsertMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Name) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.User) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.ListNode) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.PrevNode) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadRemove<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeRemoveMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiListtreeRemoveMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.ListNode) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.TreeNode) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteRemove<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeRemoveMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiListtreeRemoveMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.ListNode) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.TreeNode) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadOpenClose<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeOpenCloseMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiListtreeOpenCloseMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.ListNode) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.TreeNode) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteOpenClose<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeOpenCloseMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiListtreeOpenCloseMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.ListNode) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.TreeNode) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadSort<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeSortMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiListtreeSortMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.ListNode) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteSort<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeSortMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiListtreeSortMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.ListNode) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadGetNr<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeGetNrMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiListtreeGetNrMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.TreeNode) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteGetNr<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeGetNrMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiListtreeGetNrMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.TreeNode) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadMoveExchange<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeMoveExchangeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiListtreeMoveExchangeMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.OldListNode) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.OldTreeNode) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.NewListNode) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.NewTreeNode) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteMoveExchange<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeMoveExchangeMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiListtreeMoveExchangeMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.OldListNode) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.OldTreeNode) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.NewListNode) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.NewTreeNode) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadRename<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeRenameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiListtreeRenameMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.TreeNode) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.NewName) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteRename<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeRenameMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiListtreeRenameMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.TreeNode) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.NewName) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadFindName<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeFindNameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiListtreeFindNameMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.ListNode) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Name) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteFindName<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeFindNameMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiListtreeFindNameMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.ListNode) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Name) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadDropMark<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeDropMarkMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiListtreeDropMarkMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Entry) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Values) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteDropMark<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeDropMarkMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiListtreeDropMarkMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Entry) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Values) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadTestPos<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeTestPosMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiListtreeTestPosMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.X) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Y) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Result) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteTestPos<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeTestPosMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiListtreeTestPosMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.X) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Y) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Result) && MuiGuestStructCursor.IsComplete(cursor);
}

// Struct-first codec for the method-only Listtree header. The named
// one-ULONG record remains the ABI contract; shared guest storage keeps the
// selector boundary free of direct scalar lowering in freestanding 68k code.
internal static class MuiListtreeMethodHeaderCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListtreeMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.TryReadValue(ref platform, valueAddress,
				out methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteValue<TPlatform>(ref TPlatform platform,
		APTR address, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListtreeMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiListtreeMethodMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeMethodHeaderCodec.TryReadValue(ref platform, message,
			out packet.MethodId);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeMethodHeaderCodec.WriteValue(ref platform, message, method);
}

internal static class MuiListtreeSetMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryReadSet(ref platform, message,
			out packet);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryWriteSet(ref platform, message,
			packet);
}

internal static class MuiListtreeGetMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryReadGet(ref platform, message,
			out packet);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryWriteGet(ref platform, message,
			packet);
}

internal static class MuiListtreeGetEntryMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryReadGetEntry(ref platform, message,
			out packet);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryWriteGetEntry(ref platform, message,
			packet);
}

internal static class MuiListtreeInsertMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeInsertMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryReadInsert(ref platform, message,
			out packet);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeInsertMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryWriteInsert(ref platform, message,
			packet);
}

internal static class MuiListtreeRemoveMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeRemoveMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryReadRemove(ref platform, message,
			out packet);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeRemoveMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryWriteRemove(ref platform, message,
			packet);
}

internal static class MuiListtreeOpenCloseMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeOpenCloseMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryReadOpenClose(ref platform, message,
			out packet);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeOpenCloseMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryWriteOpenClose(ref platform, message,
			packet);
}

internal static class MuiListtreeSortMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeSortMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryReadSort(ref platform, message,
			out packet);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeSortMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryWriteSort(ref platform, message,
			packet);
}

internal static class MuiListtreeGetNrMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeGetNrMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryReadGetNr(ref platform, message,
			out packet);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeGetNrMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryWriteGetNr(ref platform, message,
			packet);
}

internal static class MuiListtreeMoveExchangeMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeMoveExchangeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryReadMoveExchange(ref platform,
			message, out packet);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeMoveExchangeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryWriteMoveExchange(ref platform,
			message, packet);
}

internal static class MuiListtreeRenameMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeRenameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryReadRename(ref platform, message,
			out packet);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeRenameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryWriteRename(ref platform, message,
			packet);
}

internal static class MuiListtreeFindNameMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeFindNameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryReadFindName(ref platform, message,
			out packet);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeFindNameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryWriteFindName(ref platform, message,
			packet);
}

internal static class MuiListtreeDropMarkMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeDropMarkMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryReadDropMark(ref platform, message,
			out packet);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeDropMarkMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryWriteDropMark(ref platform, message,
			packet);
}

internal static class MuiListtreeTestPosMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeTestPosMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryReadTestPos(ref platform, message,
			out packet);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeTestPosMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeStructPacketCodec.TryWriteTestPos(ref platform, message,
			packet);
}

internal static class MuiListtreeMessageCodec
{
	internal const uint Set = 0x8042549Au;
	internal const uint NoNotifySet = 0x8042216Fu;
	internal const uint Get = 0x80420371u;
	internal const uint Close = 0x8002001Fu;
	internal const uint Exchange = 0x80020008u;
	internal const uint FindName = 0x8002003Cu;
	internal const uint GetEntry = 0x8002002Bu;
	internal const uint GetNr = 0x8002000Eu;
	internal const uint Insert = 0x80020011u;
	internal const uint Move = 0x80020009u;
	internal const uint Open = 0x8002001Eu;
	internal const uint Remove = 0x80020012u;
	internal const uint Rename = 0x8002000Cu;
	internal const uint SetDropMark = 0x8002004Cu;
	internal const uint Sort = 0x80020029u;
	internal const uint TestPos = 0x8002004Bu;

	// Selector admission remains a scalar ABI seam; Listtree consumers retain
	// the named method-header and payload records.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListtreeMethodHeaderCodec.TryReadValue(ref platform, message,
			out methodId);

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiListtreeMethodMessage.Size)) return false;
		return TryReadMethodIdValue(ref platform, message, out packet.MethodId);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiListtreeSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsSetMethod(method) && MuiListtreeSetMessageCodec.TryRead(
			ref platform, message, out packet) && packet.MethodId == method;
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSetMethod(method)) return false;
		var packet = default(MuiListtreeSetMessage);
		packet.MethodId = method;
		packet.Attribute = attribute;
		packet.Value = value;
		return MuiListtreeSetMessageCodec.TryWrite(ref platform, message, packet);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeGetMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == Get;
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform,
		APTR message, uint attribute, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiListtreeGetMessage);
		packet.MethodId = Get;
		packet.Attribute = attribute;
		packet.Storage = storage;
		return MuiListtreeGetMessageCodec.TryWrite(ref platform, message, packet);
	}

	internal static bool TryReadInsert<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeInsertMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeInsertMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == Insert;
	}

	internal static bool WriteInsert<TPlatform>(ref TPlatform platform,
		APTR message, uint name, uint user, uint listNode, uint prevNode,
		uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiListtreeInsertMessage);
		packet.MethodId = Insert;
		packet.Name = name;
		packet.User = user;
		packet.ListNode = listNode;
		packet.PrevNode = prevNode;
		packet.Flags = flags;
		return MuiListtreeInsertMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	internal static bool TryReadRemove<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeRemoveMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeRemoveMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == Remove;
	}

	internal static bool WriteRemove<TPlatform>(ref TPlatform platform,
		APTR message, uint listNode, uint treeNode, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiListtreeRemoveMessage);
		packet.MethodId = Remove;
		packet.ListNode = listNode;
		packet.TreeNode = treeNode;
		packet.Flags = flags;
		return MuiListtreeRemoveMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	internal static bool TryReadGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeGetEntryMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == GetEntry;
	}

	internal static bool WriteGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, uint node, uint position, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiListtreeGetEntryMessage);
		packet.MethodId = GetEntry;
		packet.Node = node;
		packet.Position = position;
		packet.Flags = flags;
		return MuiListtreeGetEntryMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	internal static bool TryReadOpenClose<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiListtreeOpenCloseMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsOpenCloseMethod(method) &&
			MuiListtreeOpenCloseMessageCodec.TryRead(ref platform, message,
				out packet) && packet.MethodId == method;
	}

	internal static bool WriteOpenClose<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint listNode, uint treeNode, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsOpenCloseMethod(method)) return false;
		var packet = default(MuiListtreeOpenCloseMessage);
		packet.MethodId = method;
		packet.ListNode = listNode;
		packet.TreeNode = treeNode;
		packet.Flags = flags;
		return MuiListtreeOpenCloseMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	internal static bool TryReadSort<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiListtreeSortMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsSortMethod(method) && MuiListtreeSortMessageCodec.TryRead(
			ref platform, message, out packet) && packet.MethodId == method;
	}

	internal static bool WriteSort<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint listNode, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSortMethod(method)) return false;
		var packet = default(MuiListtreeSortMessage);
		packet.MethodId = method;
		packet.ListNode = listNode;
		packet.Flags = flags;
		return MuiListtreeSortMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	internal static bool TryReadGetNr<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiListtreeGetNrMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return method == GetNr && MuiListtreeGetNrMessageCodec.TryRead(
			ref platform, message, out packet) && packet.MethodId == method;
	}

	internal static bool WriteGetNr<TPlatform>(ref TPlatform platform,
		APTR message, uint treeNode, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiListtreeGetNrMessage);
		packet.MethodId = GetNr;
		packet.TreeNode = treeNode;
		packet.Flags = flags;
		return MuiListtreeGetNrMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	internal static bool TryReadMoveExchange<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiListtreeMoveExchangeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsMoveExchangeMethod(method) &&
			MuiListtreeMoveExchangeMessageCodec.TryRead(ref platform, message,
				out packet) && packet.MethodId == method;
	}

	internal static bool WriteMoveExchange<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint oldListNode, uint oldTreeNode,
		uint newListNode, uint newTreeNode, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMoveExchangeMethod(method)) return false;
		var packet = default(MuiListtreeMoveExchangeMessage);
		packet.MethodId = method;
		packet.OldListNode = oldListNode;
		packet.OldTreeNode = oldTreeNode;
		packet.NewListNode = newListNode;
		packet.NewTreeNode = newTreeNode;
		packet.Flags = flags;
		return MuiListtreeMoveExchangeMessageCodec.TryWrite(ref platform,
			message, packet);
	}

	internal static bool TryReadRename<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeRenameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeRenameMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == Rename;
	}

	internal static bool WriteRename<TPlatform>(ref TPlatform platform,
		APTR message, uint treeNode, uint newName, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiListtreeRenameMessage);
		packet.MethodId = Rename;
		packet.TreeNode = treeNode;
		packet.NewName = newName;
		packet.Flags = flags;
		return MuiListtreeRenameMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	internal static bool TryReadFindName<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeFindNameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeFindNameMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == FindName;
	}

	internal static bool WriteFindName<TPlatform>(ref TPlatform platform,
		APTR message, uint listNode, uint name, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiListtreeFindNameMessage);
		packet.MethodId = FindName;
		packet.ListNode = listNode;
		packet.Name = name;
		packet.Flags = flags;
		return MuiListtreeFindNameMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	internal static bool TryReadDropMark<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeDropMarkMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeDropMarkMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == SetDropMark;
	}

	internal static bool WriteDropMark<TPlatform>(ref TPlatform platform,
		APTR message, uint entry, uint values)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiListtreeDropMarkMessage);
		packet.MethodId = SetDropMark;
		packet.Entry = entry;
		packet.Values = values;
		return MuiListtreeDropMarkMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	internal static bool TryReadTestPos<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeTestPosMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeTestPosMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == TestPos;
	}

	internal static bool WriteTestPos<TPlatform>(ref TPlatform platform,
		APTR message, uint x, uint y, uint result)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiListtreeTestPosMessage);
		packet.MethodId = TestPos;
		packet.X = x;
		packet.Y = y;
		packet.Result = result;
		return MuiListtreeTestPosMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	private static bool IsSetMethod(uint method) => method == Set ||
		method == NoNotifySet;

	private static bool IsOpenCloseMethod(uint method) => method == Open ||
		method == Close;

	private static bool IsSortMethod(uint method) => method == Sort;

	private static bool IsMoveExchangeMethod(uint method) => method == Move ||
		method == Exchange;

	private static bool IsPacket<TPlatform>(ref TPlatform platform, APTR message,
		uint size, uint method) where TPlatform : struct, IMuiGuestMemory =>
		TryReadMethodId(ref platform, message, out var header) &&
		header.MethodId == method && platform.IsMapped(message, size);
}
