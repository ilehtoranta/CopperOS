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
	private static bool TryResolve(MuiListtreePacketKind packet,
		MuiListtreeField field, out uint offset)
	{
		switch (packet)
		{
			case MuiListtreePacketKind.Method:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				break;
			case MuiListtreePacketKind.Set:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.Attribute) { offset = 4; return true; }
				if (field == MuiListtreeField.Value) { offset = 8; return true; }
				break;
			case MuiListtreePacketKind.Get:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.Attribute) { offset = 4; return true; }
				if (field == MuiListtreeField.Storage) { offset = 8; return true; }
				break;
			case MuiListtreePacketKind.Insert:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.Name) { offset = 4; return true; }
				if (field == MuiListtreeField.User) { offset = 8; return true; }
				if (field == MuiListtreeField.ListNode) { offset = 12; return true; }
				if (field == MuiListtreeField.PrevNode) { offset = 16; return true; }
				if (field == MuiListtreeField.Flags) { offset = 20; return true; }
				break;
			case MuiListtreePacketKind.Remove:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.ListNode) { offset = 4; return true; }
				if (field == MuiListtreeField.TreeNode) { offset = 8; return true; }
				if (field == MuiListtreeField.Flags) { offset = 12; return true; }
				break;
			case MuiListtreePacketKind.GetEntry:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.Node) { offset = 4; return true; }
				if (field == MuiListtreeField.Position) { offset = 8; return true; }
				if (field == MuiListtreeField.Flags) { offset = 12; return true; }
				break;
			case MuiListtreePacketKind.OpenClose:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.ListNode) { offset = 4; return true; }
				if (field == MuiListtreeField.TreeNode) { offset = 8; return true; }
				if (field == MuiListtreeField.Flags) { offset = 12; return true; }
				break;
			case MuiListtreePacketKind.Sort:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.ListNode) { offset = 4; return true; }
				if (field == MuiListtreeField.Flags) { offset = 8; return true; }
				break;
			case MuiListtreePacketKind.GetNr:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.TreeNode) { offset = 4; return true; }
				if (field == MuiListtreeField.Flags) { offset = 8; return true; }
				break;
			case MuiListtreePacketKind.MoveExchange:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.OldListNode) { offset = 4; return true; }
				if (field == MuiListtreeField.OldTreeNode) { offset = 8; return true; }
				if (field == MuiListtreeField.NewListNode) { offset = 12; return true; }
				if (field == MuiListtreeField.NewTreeNode) { offset = 16; return true; }
				if (field == MuiListtreeField.Flags) { offset = 20; return true; }
				break;
			case MuiListtreePacketKind.Rename:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.TreeNode) { offset = 4; return true; }
				if (field == MuiListtreeField.NewName) { offset = 8; return true; }
				if (field == MuiListtreeField.Flags) { offset = 12; return true; }
				break;
			case MuiListtreePacketKind.FindName:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.ListNode) { offset = 4; return true; }
				if (field == MuiListtreeField.Name) { offset = 8; return true; }
				if (field == MuiListtreeField.Flags) { offset = 12; return true; }
				break;
			case MuiListtreePacketKind.DropMark:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.Entry) { offset = 4; return true; }
				if (field == MuiListtreeField.Values) { offset = 8; return true; }
				break;
			case MuiListtreePacketKind.TestPos:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.X) { offset = 4; return true; }
				if (field == MuiListtreeField.Y) { offset = 8; return true; }
				if (field == MuiListtreeField.Result) { offset = 12; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListtreeFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Packet, cursor.Field, out var offset) ||
			cursor.Message.IsNull || cursor.Message.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

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

// Fixed MorphOS packet records are consumed as named structs. Keep the guest
// positions in this small ABI adapter instead of making production dispatch
// choose a field through the shared packet/field cursor above. The legacy
// cursor remains available to compatibility tests while these codecs own the
// live method, Set, Get, GetEntry, Insert, Remove, OpenClose, Sort, GetNr,
// DropMark, and TestPos paths.
internal static class MuiListtreePacketMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, uint packetSize, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (message.IsNull || offset > packetSize ||
			packetSize - offset < 4 || message.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(message, packetSize) &&
			platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, uint packetSize, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packetSize, offset,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, uint packetSize, uint offset, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packetSize, offset,
			out var address)) return false;
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

	private static bool TryResolve(MuiListtreePacketKind packet,
		MuiListtreeField field, out uint offset)
	{
		switch (packet)
		{
			case MuiListtreePacketKind.Method:
				if (field == MuiListtreeField.MethodId)
				{
					offset = MuiListtreeMethodMessage.MethodIdOffset;
					return true;
				}
				break;
			case MuiListtreePacketKind.Set:
				if (field == MuiListtreeField.MethodId)
				{
					offset = MuiListtreeSetMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiListtreeField.Attribute)
				{
					offset = MuiListtreeSetMessage.AttributeOffset;
					return true;
				}
				if (field == MuiListtreeField.Value)
				{
					offset = MuiListtreeSetMessage.ValueOffset;
					return true;
				}
				break;
			case MuiListtreePacketKind.Get:
				if (field == MuiListtreeField.MethodId)
				{
					offset = MuiListtreeGetMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiListtreeField.Attribute)
				{
					offset = MuiListtreeGetMessage.AttributeOffset;
					return true;
				}
				if (field == MuiListtreeField.Storage)
				{
					offset = MuiListtreeGetMessage.StorageOffset;
					return true;
				}
				break;
			case MuiListtreePacketKind.GetEntry:
				if (field == MuiListtreeField.MethodId)
				{
					offset = MuiListtreeGetEntryMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiListtreeField.Node)
				{
					offset = MuiListtreeGetEntryMessage.NodeOffset;
					return true;
				}
				if (field == MuiListtreeField.Position)
				{
					offset = MuiListtreeGetEntryMessage.PositionOffset;
					return true;
				}
				if (field == MuiListtreeField.Flags)
				{
					offset = MuiListtreeGetEntryMessage.FlagsOffset;
					return true;
				}
				break;
			case MuiListtreePacketKind.Insert:
				if (field == MuiListtreeField.MethodId)
				{
					offset = MuiListtreeInsertMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiListtreeField.Name)
				{
					offset = MuiListtreeInsertMessage.NameOffset;
					return true;
				}
				if (field == MuiListtreeField.User)
				{
					offset = MuiListtreeInsertMessage.UserOffset;
					return true;
				}
				if (field == MuiListtreeField.ListNode)
				{
					offset = MuiListtreeInsertMessage.ListNodeOffset;
					return true;
				}
				if (field == MuiListtreeField.PrevNode)
				{
					offset = MuiListtreeInsertMessage.PrevNodeOffset;
					return true;
				}
				if (field == MuiListtreeField.Flags)
				{
					offset = MuiListtreeInsertMessage.FlagsOffset;
					return true;
				}
				break;
			case MuiListtreePacketKind.Remove:
				if (field == MuiListtreeField.MethodId)
				{
					offset = MuiListtreeRemoveMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiListtreeField.ListNode)
				{
					offset = MuiListtreeRemoveMessage.ListNodeOffset;
					return true;
				}
				if (field == MuiListtreeField.TreeNode)
				{
					offset = MuiListtreeRemoveMessage.TreeNodeOffset;
					return true;
				}
				if (field == MuiListtreeField.Flags)
				{
					offset = MuiListtreeRemoveMessage.FlagsOffset;
					return true;
				}
				break;
			case MuiListtreePacketKind.OpenClose:
				if (field == MuiListtreeField.MethodId)
				{
					offset = MuiListtreeOpenCloseMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiListtreeField.ListNode)
				{
					offset = MuiListtreeOpenCloseMessage.ListNodeOffset;
					return true;
				}
				if (field == MuiListtreeField.TreeNode)
				{
					offset = MuiListtreeOpenCloseMessage.TreeNodeOffset;
					return true;
				}
				if (field == MuiListtreeField.Flags)
				{
					offset = MuiListtreeOpenCloseMessage.FlagsOffset;
					return true;
				}
				break;
			case MuiListtreePacketKind.Sort:
				if (field == MuiListtreeField.MethodId)
				{
					offset = MuiListtreeSortMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiListtreeField.ListNode)
				{
					offset = MuiListtreeSortMessage.ListNodeOffset;
					return true;
				}
				if (field == MuiListtreeField.Flags)
				{
					offset = MuiListtreeSortMessage.FlagsOffset;
					return true;
				}
				break;
			case MuiListtreePacketKind.GetNr:
				if (field == MuiListtreeField.MethodId)
				{
					offset = MuiListtreeGetNrMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiListtreeField.TreeNode)
				{
					offset = MuiListtreeGetNrMessage.TreeNodeOffset;
					return true;
				}
				if (field == MuiListtreeField.Flags)
				{
					offset = MuiListtreeGetNrMessage.FlagsOffset;
					return true;
				}
				break;
			case MuiListtreePacketKind.MoveExchange:
				if (field == MuiListtreeField.MethodId)
				{
					offset = MuiListtreeMoveExchangeMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiListtreeField.OldListNode)
				{
					offset = MuiListtreeMoveExchangeMessage.OldListNodeOffset;
					return true;
				}
				if (field == MuiListtreeField.OldTreeNode)
				{
					offset = MuiListtreeMoveExchangeMessage.OldTreeNodeOffset;
					return true;
				}
				if (field == MuiListtreeField.NewListNode)
				{
					offset = MuiListtreeMoveExchangeMessage.NewListNodeOffset;
					return true;
				}
				if (field == MuiListtreeField.NewTreeNode)
				{
					offset = MuiListtreeMoveExchangeMessage.NewTreeNodeOffset;
					return true;
				}
				if (field == MuiListtreeField.Flags)
				{
					offset = MuiListtreeMoveExchangeMessage.FlagsOffset;
					return true;
				}
				break;
			case MuiListtreePacketKind.Rename:
				if (field == MuiListtreeField.MethodId)
				{
					offset = MuiListtreeRenameMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiListtreeField.TreeNode)
				{
					offset = MuiListtreeRenameMessage.TreeNodeOffset;
					return true;
				}
				if (field == MuiListtreeField.NewName)
				{
					offset = MuiListtreeRenameMessage.NewNameOffset;
					return true;
				}
				if (field == MuiListtreeField.Flags)
				{
					offset = MuiListtreeRenameMessage.FlagsOffset;
					return true;
				}
				break;
			case MuiListtreePacketKind.FindName:
				if (field == MuiListtreeField.MethodId)
				{
					offset = MuiListtreeFindNameMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiListtreeField.ListNode)
				{
					offset = MuiListtreeFindNameMessage.ListNodeOffset;
					return true;
				}
				if (field == MuiListtreeField.Name)
				{
					offset = MuiListtreeFindNameMessage.NameOffset;
					return true;
				}
				if (field == MuiListtreeField.Flags)
				{
					offset = MuiListtreeFindNameMessage.FlagsOffset;
					return true;
				}
				break;
			case MuiListtreePacketKind.DropMark:
				if (field == MuiListtreeField.MethodId)
				{
					offset = MuiListtreeDropMarkMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiListtreeField.Entry)
				{
					offset = MuiListtreeDropMarkMessage.EntryOffset;
					return true;
				}
				if (field == MuiListtreeField.Values)
				{
					offset = MuiListtreeDropMarkMessage.ValuesOffset;
					return true;
				}
				break;
			case MuiListtreePacketKind.TestPos:
				if (field == MuiListtreeField.MethodId)
				{
					offset = MuiListtreeTestPosMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiListtreeField.X)
				{
					offset = MuiListtreeTestPosMessage.XOffset;
					return true;
				}
				if (field == MuiListtreeField.Y)
				{
					offset = MuiListtreeTestPosMessage.YOffset;
					return true;
				}
				if (field == MuiListtreeField.Result)
				{
					offset = MuiListtreeTestPosMessage.ResultOffset;
					return true;
				}
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreePacketKind packet, MuiListtreeField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset) ||
			!TryGetPacketSize(packet, out var packetSize) || message.IsNull ||
			message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, packetSize)) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiListtreeMethodMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreePacketKind packet, MuiListtreeField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreePacketKind packet, MuiListtreeField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiListtreeMethodMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.Method, MuiListtreeField.MethodId,
			out packet.MethodId);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.Method, MuiListtreeField.MethodId, method);
}

internal static class MuiListtreeSetMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.Set, MuiListtreeField.MethodId,
			out packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Set, MuiListtreeField.Attribute,
				out packet.Attribute) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Set, MuiListtreeField.Value,
				out packet.Value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.Set, MuiListtreeField.MethodId,
			packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Set, MuiListtreeField.Attribute,
				packet.Attribute) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Set, MuiListtreeField.Value,
				packet.Value);
	}
}

internal static class MuiListtreeGetMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.Get, MuiListtreeField.MethodId,
			out packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Get, MuiListtreeField.Attribute,
				out packet.Attribute) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Get, MuiListtreeField.Storage,
				out packet.Storage);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.Get, MuiListtreeField.MethodId,
			packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Get, MuiListtreeField.Attribute,
				packet.Attribute) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Get, MuiListtreeField.Storage,
				packet.Storage);
	}
}

internal static class MuiListtreeGetEntryMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.GetEntry, MuiListtreeField.MethodId,
			out packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.GetEntry, MuiListtreeField.Node,
				out packet.Node) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.GetEntry, MuiListtreeField.Position,
				out packet.Position) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.GetEntry, MuiListtreeField.Flags,
				out packet.Flags);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.GetEntry, MuiListtreeField.MethodId,
			packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.GetEntry, MuiListtreeField.Node,
				packet.Node) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.GetEntry, MuiListtreeField.Position,
				packet.Position) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.GetEntry, MuiListtreeField.Flags,
				packet.Flags);
	}
}

internal static class MuiListtreeInsertMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeInsertMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.Insert, MuiListtreeField.MethodId,
			out packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.Name,
				out packet.Name) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.User,
				out packet.User) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.ListNode,
				out packet.ListNode) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.PrevNode,
				out packet.PrevNode) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.Flags,
				out packet.Flags);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeInsertMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.Insert, MuiListtreeField.MethodId,
			packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.Name, packet.Name) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.User, packet.User) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.ListNode,
				packet.ListNode) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.PrevNode,
				packet.PrevNode) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.Flags, packet.Flags);
	}
}

internal static class MuiListtreeRemoveMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeRemoveMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.Remove, MuiListtreeField.MethodId,
			out packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Remove, MuiListtreeField.ListNode,
				out packet.ListNode) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Remove, MuiListtreeField.TreeNode,
				out packet.TreeNode) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Remove, MuiListtreeField.Flags,
				out packet.Flags);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeRemoveMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.Remove, MuiListtreeField.MethodId,
			packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Remove, MuiListtreeField.ListNode,
				packet.ListNode) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Remove, MuiListtreeField.TreeNode,
				packet.TreeNode) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Remove, MuiListtreeField.Flags, packet.Flags);
	}
}

internal static class MuiListtreeOpenCloseMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeOpenCloseMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.OpenClose, MuiListtreeField.MethodId,
			out packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.OpenClose, MuiListtreeField.ListNode,
				out packet.ListNode) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.OpenClose, MuiListtreeField.TreeNode,
				out packet.TreeNode) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.OpenClose, MuiListtreeField.Flags,
				out packet.Flags);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeOpenCloseMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.OpenClose, MuiListtreeField.MethodId,
			packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.OpenClose, MuiListtreeField.ListNode,
				packet.ListNode) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.OpenClose, MuiListtreeField.TreeNode,
				packet.TreeNode) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.OpenClose, MuiListtreeField.Flags,
				packet.Flags);
	}
}

internal static class MuiListtreeSortMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeSortMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.Sort, MuiListtreeField.MethodId,
			out packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Sort, MuiListtreeField.ListNode,
				out packet.ListNode) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Sort, MuiListtreeField.Flags,
				out packet.Flags);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeSortMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.Sort, MuiListtreeField.MethodId,
			packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Sort, MuiListtreeField.ListNode,
				packet.ListNode) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Sort, MuiListtreeField.Flags, packet.Flags);
	}
}

internal static class MuiListtreeGetNrMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeGetNrMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.GetNr, MuiListtreeField.MethodId,
			out packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.GetNr, MuiListtreeField.TreeNode,
				out packet.TreeNode) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.GetNr, MuiListtreeField.Flags,
				out packet.Flags);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeGetNrMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.GetNr, MuiListtreeField.MethodId,
			packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.GetNr, MuiListtreeField.TreeNode,
				packet.TreeNode) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.GetNr, MuiListtreeField.Flags, packet.Flags);
	}
}

internal static class MuiListtreeMoveExchangeMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeMoveExchangeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.MoveExchange, MuiListtreeField.MethodId,
			out packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.OldListNode,
				out packet.OldListNode) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.OldTreeNode,
				out packet.OldTreeNode) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.NewListNode,
				out packet.NewListNode) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.NewTreeNode,
				out packet.NewTreeNode) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.Flags,
				out packet.Flags);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeMoveExchangeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.MoveExchange, MuiListtreeField.MethodId,
			packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.OldListNode,
				packet.OldListNode) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.OldTreeNode,
				packet.OldTreeNode) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.NewListNode,
				packet.NewListNode) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.NewTreeNode,
				packet.NewTreeNode) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.Flags,
				packet.Flags);
	}
}

internal static class MuiListtreeRenameMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeRenameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.Rename, MuiListtreeField.MethodId,
			out packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Rename, MuiListtreeField.TreeNode,
				out packet.TreeNode) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Rename, MuiListtreeField.NewName,
				out packet.NewName) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Rename, MuiListtreeField.Flags,
				out packet.Flags);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeRenameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.Rename, MuiListtreeField.MethodId,
			packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Rename, MuiListtreeField.TreeNode,
				packet.TreeNode) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Rename, MuiListtreeField.NewName,
				packet.NewName) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Rename, MuiListtreeField.Flags, packet.Flags);
	}
}

internal static class MuiListtreeFindNameMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeFindNameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.FindName, MuiListtreeField.MethodId,
			out packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.FindName, MuiListtreeField.ListNode,
				out packet.ListNode) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.FindName, MuiListtreeField.Name,
				out packet.Name) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.FindName, MuiListtreeField.Flags,
				out packet.Flags);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeFindNameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.FindName, MuiListtreeField.MethodId,
			packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.FindName, MuiListtreeField.ListNode,
				packet.ListNode) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.FindName, MuiListtreeField.Name, packet.Name) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.FindName, MuiListtreeField.Flags, packet.Flags);
	}
}

internal static class MuiListtreeDropMarkMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeDropMarkMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.DropMark, MuiListtreeField.MethodId,
			out packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.DropMark, MuiListtreeField.Entry,
				out packet.Entry) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.DropMark, MuiListtreeField.Values,
				out packet.Values);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeDropMarkMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.DropMark, MuiListtreeField.MethodId,
			packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.DropMark, MuiListtreeField.Entry,
				packet.Entry) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.DropMark, MuiListtreeField.Values,
				packet.Values);
	}
}

internal static class MuiListtreeTestPosMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeTestPosMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.TestPos, MuiListtreeField.MethodId,
			out packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.TestPos, MuiListtreeField.X,
				out packet.X) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.TestPos, MuiListtreeField.Y,
				out packet.Y) &&
			MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.TestPos, MuiListtreeField.Result,
				out packet.Result);
}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiListtreeTestPosMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.TestPos, MuiListtreeField.MethodId,
			packet.MethodId) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.TestPos, MuiListtreeField.X, packet.X) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.TestPos, MuiListtreeField.Y, packet.Y) &&
			MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.TestPos, MuiListtreeField.Result,
				packet.Result);
	}
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
	{
		if (!MuiListtreeMethodMessageCodec.TryRead(ref platform, message,
			out var packet))
		{
			methodId = 0;
			return false;
		}
		methodId = packet.MethodId;
		return true;
	}

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
