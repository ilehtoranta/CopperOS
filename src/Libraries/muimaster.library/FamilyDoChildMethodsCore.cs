/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS MUIM_Family_DoChildMethods packet. The public SDK declaration is
// intentionally only MethodID; the method forwards that same message to each
// direct child of the receiving Family object.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilyDoChildMethodsMessage
{
	internal const uint Size = 4;
	internal uint MethodId;
}

internal enum MuiFamilyDoChildMethodsPacketField : byte
{
	MethodId,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilyDoChildMethodsPacketFieldCursor
{
	internal APTR Message;
	internal MuiFamilyDoChildMethodsPacketField Field;
}

internal static class MuiFamilyDoChildMethodsPacketFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiFamilyDoChildMethodsPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Field != MuiFamilyDoChildMethodsPacketField.MethodId ||
			cursor.Message.IsNull) return false;
		address = cursor.Message;
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyDoChildMethodsPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiFamilyDoChildMethodsPacketFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyDoChildMethodsPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiFamilyDoChildMethodsPacketFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Central adapter for the fixed MorphOS MUIM_Family_DoChildMethods packet.
// The public message contains only MethodID; this codec owns the packed guest
// boundary so the forwarding walk can stay record-first.
internal static class MuiFamilyDoChildMethodsMessageCodec
{
	internal const uint Method = 0x80429A3Cu;

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyDoChildMethodsMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilyDoChildMethodsMessage.Size)) return false;
		return MuiFamilyDoChildMethodsPacketFieldCursorCodec.TryReadUInt32(
			ref platform, message,
			MuiFamilyDoChildMethodsPacketField.MethodId, out packet.MethodId);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyDoChildMethodsMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsValid(ref platform, message);
	}

	internal static bool IsValid<TPlatform>(ref TPlatform platform, APTR message)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadMethodId(ref platform, message, out var packet) &&
			packet.MethodId == Method;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR message)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilyDoChildMethodsMessage.Size)) return false;
		return MuiFamilyDoChildMethodsPacketFieldCursorCodec.TryWriteUInt32(
			ref platform, message,
			MuiFamilyDoChildMethodsPacketField.MethodId, Method);
	}
}

public static class MuiFamilyDoChildMethodsCore
{
	public const uint Method = 0x80429A3Cu;
	public const uint PacketSize = MuiFamilyDoChildMethodsMessage.Size;
	public const uint MaximumChildren = 65535;

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyDoChildMethodsMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyDoChildMethodsMessageCodec.TryRead(ref platform, message,
			out packet);

	public static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
	{
		return MuiFamilyDoChildMethodsMessageCodec.Write(ref platform, message);
	}

	// Live Family dispatch. Read the next link before invoking a child method so
	// a child-side mutation cannot make the walk revisit the same node. The
	// method continues across all children and returns the last child result.
	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR family, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiFamilyDoChildMethodsMessageCodec.IsValid(ref platform, message))
			return 0;
		var familyRecord = MuiHeadlessObjectCore.FindObject(ref platform, state,
			family);
		if (familyRecord.IsNull) return 0;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, familyRecord,
			out var familyValue)) return 0;
		var current = familyValue.ChildrenHead;
		uint visited = 0;
		uint result = 0;
		while (current.IsNotNull && visited++ < MaximumChildren)
		{
			if (!TryReadChildRecord(ref platform, current, out var childNode))
				return 0;
			var next = childNode.Next;
			var childRecord = childNode.Object;
			if (childRecord.IsNull || !MuiHeadlessObjectCodec.TryRead(
				ref platform, childRecord, out var childValue)) return 0;
			var child = childValue.Boopsi;
			if (child.IsNull) return 0;
			result = platform.DoMethod(child, message);
			current = next;
		}
		return current.IsNull ? result : 0;
	}

	// Focused packet/topology seam for native qualification. It counts the
	// direct projection records that would receive the message; external BOOPSI
	// dispatch remains owned by IMuiBoopsiCapability in the live path.
	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR head, APTR tail, APTR message)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiFamilyDoChildMethodsMessageCodec.IsValid(ref platform, message))
			return 0;
		var current = head;
		uint count = 0;
		while (current.IsNotNull && count < MaximumChildren)
		{
			if (!TryReadChildRecord(ref platform, current, out var childNode))
				return 0;
			count++;
			if (current.Raw == tail.Raw) return count;
			current = childNode.Next;
		}
		return current.IsNull ? count : 0;
	}

	public static bool WriteChildRecord<TPlatform>(ref TPlatform platform,
		APTR storage, APTR next, APTR previous, APTR child)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiHeadlessChildPacketCore.WriteRecord(ref platform, storage,
			next, previous, child, APTR.Null);
	}

	private static bool TryReadChildRecord<TPlatform>(ref TPlatform platform,
		APTR storage, out MuiHeadlessChildRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiHeadlessChildCodec.TryRead(ref platform, storage, out record);
	}
}
