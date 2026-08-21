/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

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
	UserData,
	Parent,
	Previous,
	Flags,
	Node,
	Position,
	NewParent,
	X,
	Y,
	Entry,
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
				if (field == MuiListtreeField.UserData) { offset = 8; return true; }
				if (field == MuiListtreeField.Parent) { offset = 12; return true; }
				if (field == MuiListtreeField.Previous) { offset = 16; return true; }
				if (field == MuiListtreeField.Flags) { offset = 20; return true; }
				break;
			case MuiListtreePacketKind.Remove:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.Parent) { offset = 4; return true; }
				if (field == MuiListtreeField.Node) { offset = 8; return true; }
				if (field == MuiListtreeField.Flags) { offset = 12; return true; }
				break;
			case MuiListtreePacketKind.GetEntry:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.Parent) { offset = 4; return true; }
				if (field == MuiListtreeField.Position) { offset = 8; return true; }
				if (field == MuiListtreeField.Flags) { offset = 12; return true; }
				break;
			case MuiListtreePacketKind.OpenClose:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.Parent) { offset = 4; return true; }
				if (field == MuiListtreeField.Node) { offset = 8; return true; }
				if (field == MuiListtreeField.Flags) { offset = 12; return true; }
				break;
			case MuiListtreePacketKind.Sort:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.Parent) { offset = 4; return true; }
				if (field == MuiListtreeField.Flags) { offset = 8; return true; }
				break;
			case MuiListtreePacketKind.MoveExchange:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.Parent) { offset = 4; return true; }
				if (field == MuiListtreeField.Node) { offset = 8; return true; }
				if (field == MuiListtreeField.NewParent) { offset = 12; return true; }
				if (field == MuiListtreeField.Previous) { offset = 16; return true; }
				if (field == MuiListtreeField.Flags) { offset = 20; return true; }
				break;
			case MuiListtreePacketKind.Rename:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.Node) { offset = 4; return true; }
				if (field == MuiListtreeField.Name) { offset = 8; return true; }
				if (field == MuiListtreeField.Flags) { offset = 12; return true; }
				break;
			case MuiListtreePacketKind.FindName:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.Parent) { offset = 4; return true; }
				if (field == MuiListtreeField.Name) { offset = 8; return true; }
				if (field == MuiListtreeField.Flags) { offset = 12; return true; }
				break;
			case MuiListtreePacketKind.DropMark:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.Position) { offset = 4; return true; }
				if (field == MuiListtreeField.Flags) { offset = 8; return true; }
				break;
			case MuiListtreePacketKind.TestPos:
				if (field == MuiListtreeField.MethodId) { offset = 0; return true; }
				if (field == MuiListtreeField.X) { offset = 4; return true; }
				if (field == MuiListtreeField.Y) { offset = 8; return true; }
				if (field == MuiListtreeField.Entry) { offset = 12; return true; }
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

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiListtreeMethodMessage.Size)) return false;
		return MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.Method, MuiListtreeField.MethodId,
			out packet.MethodId);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiListtreeSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsSetMethod(method) || !IsPacket(ref platform, message,
			MuiListtreeSetMessage.Size, method)) return false;
		packet.MethodId = method;
		return MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.Set, MuiListtreeField.Attribute,
			out packet.Attribute) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Set, MuiListtreeField.Value, out packet.Value);
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSetMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiListtreeSetMessage.Size)) return false;
		return MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.Set, MuiListtreeField.MethodId, method) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Set, MuiListtreeField.Attribute, attribute) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Set, MuiListtreeField.Value, value);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiListtreeGetMessage.Size, Get))
			return false;
		packet.MethodId = Get;
		return MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.Get, MuiListtreeField.Attribute,
			out packet.Attribute) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Get, MuiListtreeField.Storage,
				out packet.Storage);
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform,
		APTR message, uint attribute, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiListtreeGetMessage.Size)) return false;
		return MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.Get, MuiListtreeField.MethodId, Get) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Get, MuiListtreeField.Attribute, attribute) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Get, MuiListtreeField.Storage, storage);
	}

	internal static bool TryReadInsert<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeInsertMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiListtreeInsertMessage.Size,
			Insert)) return false;
		packet.MethodId = Insert;
		return MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.Insert, MuiListtreeField.Name, out packet.Name) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.UserData,
				out packet.UserData) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.Parent,
				out packet.Parent) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.Previous,
				out packet.Previous) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.Flags,
				out packet.Flags);
	}

	internal static bool WriteInsert<TPlatform>(ref TPlatform platform,
		APTR message, uint name, uint userData, uint parent, uint previous,
		uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiListtreeInsertMessage.Size)) return false;
		return MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.Insert, MuiListtreeField.MethodId, Insert) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.Name, name) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.UserData,
				userData) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.Parent, parent) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.Previous,
				previous) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.Flags, flags);
	}

	internal static bool TryReadRemove<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeRemoveMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiListtreeRemoveMessage.Size,
			Remove)) return false;
		packet.MethodId = Remove;
		return MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.Remove, MuiListtreeField.Parent,
			out packet.Parent) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Remove, MuiListtreeField.Node,
				out packet.Node) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Remove, MuiListtreeField.Flags,
				out packet.Flags);
	}

	internal static bool WriteRemove<TPlatform>(ref TPlatform platform,
		APTR message, uint parent, uint node, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiListtreeRemoveMessage.Size)) return false;
		return MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.Remove, MuiListtreeField.MethodId, Remove) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Remove, MuiListtreeField.Parent, parent) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Remove, MuiListtreeField.Node, node) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Remove, MuiListtreeField.Flags, flags);
	}

	internal static bool TryReadGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiListtreeGetEntryMessage.Size,
			GetEntry)) return false;
		packet.MethodId = GetEntry;
		return MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.GetEntry, MuiListtreeField.Parent,
			out packet.Parent) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.GetEntry, MuiListtreeField.Position,
				out packet.Position) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.GetEntry, MuiListtreeField.Flags,
				out packet.Flags);
	}

	internal static bool WriteGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, uint parent, uint position, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiListtreeGetEntryMessage.Size)) return false;
		return MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.GetEntry, MuiListtreeField.MethodId, GetEntry) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.GetEntry, MuiListtreeField.Parent, parent) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.GetEntry, MuiListtreeField.Position,
				position) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.GetEntry, MuiListtreeField.Flags, flags);
	}

	internal static bool TryReadOpenClose<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiListtreeOpenCloseMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsOpenCloseMethod(method) || !IsPacket(ref platform, message,
			MuiListtreeOpenCloseMessage.Size, method)) return false;
		packet.MethodId = method;
		return MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.OpenClose, MuiListtreeField.Parent,
			out packet.Parent) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.OpenClose, MuiListtreeField.Node,
				out packet.Node) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.OpenClose, MuiListtreeField.Flags,
				out packet.Flags);
	}

	internal static bool WriteOpenClose<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint parent, uint node, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsOpenCloseMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiListtreeOpenCloseMessage.Size)) return false;
		return MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.OpenClose, MuiListtreeField.MethodId, method) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.OpenClose, MuiListtreeField.Parent, parent) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.OpenClose, MuiListtreeField.Node, node) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.OpenClose, MuiListtreeField.Flags, flags);
	}

	internal static bool TryReadSort<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiListtreeSortMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsSortMethod(method) || !IsPacket(ref platform, message,
			MuiListtreeSortMessage.Size, method)) return false;
		packet.MethodId = method;
		return MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.Sort, MuiListtreeField.Parent,
			out packet.Parent) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Sort, MuiListtreeField.Flags,
				out packet.Flags);
	}

	internal static bool WriteSort<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint parent, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSortMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiListtreeSortMessage.Size)) return false;
		return MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.Sort, MuiListtreeField.MethodId, method) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Sort, MuiListtreeField.Parent, parent) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Sort, MuiListtreeField.Flags, flags);
	}

	internal static bool TryReadMoveExchange<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiListtreeMoveExchangeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsMoveExchangeMethod(method) || !IsPacket(ref platform, message,
			MuiListtreeMoveExchangeMessage.Size, method)) return false;
		packet.MethodId = method;
		return MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.MoveExchange, MuiListtreeField.Parent,
			out packet.Parent) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.Node,
				out packet.Node) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.NewParent,
				out packet.NewParent) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.Previous,
				out packet.Previous) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.Flags,
				out packet.Flags);
	}

	internal static bool WriteMoveExchange<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint parent, uint node, uint newParent,
		uint previous, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMoveExchangeMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiListtreeMoveExchangeMessage.Size)) return false;
		return MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.MoveExchange, MuiListtreeField.MethodId, method) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.Parent, parent) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.Node, node) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.NewParent,
				newParent) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.Previous,
				previous) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.Flags, flags);
	}

	internal static bool TryReadRename<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeRenameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiListtreeRenameMessage.Size,
			Rename)) return false;
		packet.MethodId = Rename;
		return MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.Rename, MuiListtreeField.Node,
			out packet.Node) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Rename, MuiListtreeField.Name,
				out packet.Name) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Rename, MuiListtreeField.Flags,
				out packet.Flags);
	}

	internal static bool WriteRename<TPlatform>(ref TPlatform platform,
		APTR message, uint node, uint name, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiListtreeRenameMessage.Size)) return false;
		return MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.Rename, MuiListtreeField.MethodId, Rename) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Rename, MuiListtreeField.Node, node) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Rename, MuiListtreeField.Name, name) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.Rename, MuiListtreeField.Flags, flags);
	}

	internal static bool TryReadFindName<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeFindNameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiListtreeFindNameMessage.Size,
			FindName)) return false;
		packet.MethodId = FindName;
		return MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.FindName, MuiListtreeField.Parent,
			out packet.Parent) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.FindName, MuiListtreeField.Name,
				out packet.Name) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.FindName, MuiListtreeField.Flags,
				out packet.Flags);
	}

	internal static bool WriteFindName<TPlatform>(ref TPlatform platform,
		APTR message, uint parent, uint name, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiListtreeFindNameMessage.Size)) return false;
		return MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.FindName, MuiListtreeField.MethodId, FindName) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.FindName, MuiListtreeField.Parent, parent) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.FindName, MuiListtreeField.Name, name) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.FindName, MuiListtreeField.Flags, flags);
	}

	internal static bool TryReadDropMark<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeDropMarkMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiListtreeDropMarkMessage.Size,
			SetDropMark)) return false;
		packet.MethodId = SetDropMark;
		return MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.DropMark, MuiListtreeField.Position,
			out packet.Position) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.DropMark, MuiListtreeField.Flags,
				out packet.Flags);
	}

	internal static bool WriteDropMark<TPlatform>(ref TPlatform platform,
		APTR message, uint position, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiListtreeDropMarkMessage.Size)) return false;
		return MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.DropMark, MuiListtreeField.MethodId,
			SetDropMark) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.DropMark, MuiListtreeField.Position,
				position) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.DropMark, MuiListtreeField.Flags, flags);
	}

	internal static bool TryReadTestPos<TPlatform>(ref TPlatform platform,
		APTR message, out MuiListtreeTestPosMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiListtreeTestPosMessage.Size,
			TestPos)) return false;
		packet.MethodId = TestPos;
		return MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiListtreePacketKind.TestPos, MuiListtreeField.X, out packet.X) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.TestPos, MuiListtreeField.Y,
				out packet.Y) &&
			MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.TestPos, MuiListtreeField.Entry,
				out packet.Entry);
	}

	internal static bool WriteTestPos<TPlatform>(ref TPlatform platform,
		APTR message, uint x, uint y, uint entry)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiListtreeTestPosMessage.Size)) return false;
		return MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiListtreePacketKind.TestPos, MuiListtreeField.MethodId, TestPos) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.TestPos, MuiListtreeField.X, x) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.TestPos, MuiListtreeField.Y, y) &&
			MuiListtreeFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiListtreePacketKind.TestPos, MuiListtreeField.Entry, entry);
	}

	private static bool IsSetMethod(uint method) => method == Set ||
		method == NoNotifySet;

	private static bool IsOpenCloseMethod(uint method) => method == Open ||
		method == Close;

	private static bool IsSortMethod(uint method) => method == Sort ||
		method == GetNr;

	private static bool IsMoveExchangeMethod(uint method) => method == Move ||
		method == Exchange;

	private static bool IsPacket<TPlatform>(ref TPlatform platform, APTR message,
		uint size, uint method) where TPlatform : struct, IMuiGuestMemory =>
		TryReadMethodId(ref platform, message, out var header) &&
		header.MethodId == method && platform.IsMapped(message, size);
}
