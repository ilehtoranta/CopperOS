/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS MUIM_Family_GetChild packet.  Keep the public ABI packet as a named
// record; only this codec touches its packed wire representation.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilyGetChildMessage
{
	internal const uint Size = 12;
	internal uint MethodId;
	internal int Number;
	internal APTR Reference;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilyGetChildMethodMessage
{
	internal const uint Size = 4;
	internal uint MethodId;
}

internal enum MuiFamilyGetChildPacketField : byte
{
	MethodId,
	Number,
	Reference,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilyGetChildPacketFieldCursor
{
	internal APTR Message;
	internal MuiFamilyGetChildPacketField Field;
}

internal static class MuiFamilyGetChildPacketFieldCursorCodec
{
	private static bool TryResolve(MuiFamilyGetChildPacketField field,
		out uint offset)
	{
		if (field == MuiFamilyGetChildPacketField.MethodId) { offset = 0; return true; }
		if (field == MuiFamilyGetChildPacketField.Number) { offset = 4; return true; }
		if (field == MuiFamilyGetChildPacketField.Reference) { offset = 8; return true; }
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiFamilyGetChildPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Message.IsNull ||
			cursor.Message.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyGetChildPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiFamilyGetChildPacketFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyGetChildPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiFamilyGetChildPacketFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Central adapter for the fixed MorphOS MUIM_Family_GetChild packet. The
// public selector record remains the consumer-facing shape; only this codec
// knows the packed guest offsets and mapping boundary.
internal static class MuiFamilyGetChildMessageCodec
{
	internal const uint Method = 0x8042C556;

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyGetChildMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilyGetChildMethodMessage.Size)) return false;
		return MuiFamilyGetChildPacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiFamilyGetChildPacketField.MethodId, out packet.MethodId);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyGetChildMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilyGetChildMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != Method) return false;
		if (!MuiFamilyGetChildPacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiFamilyGetChildPacketField.Number, out var rawNumber) ||
			!MuiFamilyGetChildPacketFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiFamilyGetChildPacketField.Reference,
				out var rawReference)) return false;
		packet.MethodId = header.MethodId;
		packet.Number = unchecked((int)rawNumber);
		packet.Reference = APTR.FromPointer(rawReference);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyGetChildMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilyGetChildMessage.Size)) return false;
		return MuiFamilyGetChildPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, message, MuiFamilyGetChildPacketField.MethodId, Method) &&
			MuiFamilyGetChildPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiFamilyGetChildPacketField.Number,
				unchecked((uint)packet.Number)) &&
			MuiFamilyGetChildPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiFamilyGetChildPacketField.Reference,
				packet.Reference.Raw);
	}
}

// Selector-aware Family_GetChild dispatch.  The actual child topology remains
// in MuiFamilyCore, so no managed collection or iterator crosses the guest ABI.
public static class MuiFamilyGetChildCore
{
	internal const uint Method = MuiFamilyGetChildMessageCodec.Method;
	internal const int First = 0;
	internal const int Last = -1;
	internal const int Next = -2;
	internal const int Previous = -3;
	internal const int Iterate = -4;

	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR family, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryRead(ref platform, message, out var packet)) return 0;
		return MuiFamilyCore.GetChild(ref platform, state, family,
			packet.Number, packet.Reference).Raw;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyGetChildMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyGetChildMessageCodec.TryRead(ref platform, message,
			out packet);

	public static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR message, int number, APTR reference)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiFamilyGetChildMessage);
		packet.MethodId = Method;
		packet.Number = number;
		packet.Reference = reference;
		return MuiFamilyGetChildMessageCodec.Write(ref platform, message,
			packet);
	}

	// Focused native seam for the packet/selector boundary. Production dispatch
	// uses live Family records; this fixed projection keeps the freestanding
	// closure independent of the complete headless lifecycle.
	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR head, APTR tail, APTR message)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryRead(ref platform, message, out var packet)) return 0;
		var node = ResolveRecordNode(ref platform, head, tail, packet.Number,
			packet.Reference);
		if (node.IsNull || !platform.IsMapped(node,
			MuiHeadlessChildRecord.Size) ||
			!MuiHeadlessChildCodec.TryRead(ref platform, node,
			out var childNode)) return 0;
		return childNode.Object.Raw;
	}

	public static bool WriteChildRecord<TPlatform>(ref TPlatform platform,
		APTR storage, APTR next, APTR previous, APTR child)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiHeadlessChildPacketCore.WriteRecord(ref platform, storage,
			next, previous, child, APTR.Null);
	}

	private static APTR ResolveRecordNode<TPlatform>(ref TPlatform platform,
		APTR head, APTR tail, int number, APTR reference)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (number == First) return head;
		if (number == Last) return tail;
		if (number == Next || number == Iterate)
		{
			if (reference.IsNull) return head;
			var node = FindRecordByObject(ref platform, head, reference);
			return node.IsNull ? APTR.Null : ReadLink(ref platform, node, false);
		}
		if (number == Previous)
		{
			if (reference.IsNull) return tail;
			var node = FindRecordByObject(ref platform, head, reference);
			return node.IsNull ? APTR.Null : ReadLink(ref platform, node, true);
		}
		if (number < 0) return APTR.Null;
		var current = head;
		var remaining = number;
		while (current.IsNotNull && remaining-- > 0)
			current = ReadLink(ref platform, current, false);
		return current;
	}

	private static APTR FindRecordByObject<TPlatform>(ref TPlatform platform,
		APTR head, APTR target) where TPlatform : struct, IMuiGuestMemory
	{
		var current = head;
		var visited = 0u;
		while (current.IsNotNull && visited++ < 65535)
		{
			if (!platform.IsMapped(current, MuiHeadlessChildRecord.Size) ||
				!MuiHeadlessChildCodec.TryRead(ref platform, current,
				out var childNode))
				return APTR.Null;
			if (childNode.Object.Raw == target.Raw) return current;
			current = childNode.Next;
		}
		return APTR.Null;
	}

	private static APTR ReadLink<TPlatform>(ref TPlatform platform, APTR node,
		bool previous) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiHeadlessChildCodec.TryRead(ref platform, node,
			out var childNode)) return APTR.Null;
		return previous ? childNode.Previous : childNode.Next;
	}
}
