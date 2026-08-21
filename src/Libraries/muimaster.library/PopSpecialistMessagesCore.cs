/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

internal enum MuiPopSpecialistPacketKind : byte
{
	Method,
	Get,
	Set,
	Close,
}

internal enum MuiPopSpecialistField : byte
{
	MethodId,
	Attribute,
	Storage,
	Value,
	Result,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPopSpecialistFieldCursor
{
	internal APTR Message;
	internal MuiPopSpecialistPacketKind Packet;
	internal MuiPopSpecialistField Field;
}

internal static class MuiPopSpecialistFieldCursorCodec
{
	private static bool TryResolve(MuiPopSpecialistPacketKind packet,
		MuiPopSpecialistField field, out uint offset)
	{
		switch (packet)
		{
			case MuiPopSpecialistPacketKind.Method:
				if (field == MuiPopSpecialistField.MethodId) { offset = 0; return true; }
				break;
			case MuiPopSpecialistPacketKind.Get:
				if (field == MuiPopSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiPopSpecialistField.Attribute) { offset = 4; return true; }
				if (field == MuiPopSpecialistField.Storage) { offset = 8; return true; }
				break;
			case MuiPopSpecialistPacketKind.Set:
				if (field == MuiPopSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiPopSpecialistField.Attribute) { offset = 4; return true; }
				if (field == MuiPopSpecialistField.Value) { offset = 8; return true; }
				break;
			case MuiPopSpecialistPacketKind.Close:
				if (field == MuiPopSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiPopSpecialistField.Result) { offset = 4; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiPopSpecialistFieldCursor cursor, out APTR address)
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
		APTR message, MuiPopSpecialistPacketKind packet,
		MuiPopSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiPopSpecialistFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiPopSpecialistPacketKind packet,
		MuiPopSpecialistField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiPopSpecialistFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Central codec for the fixed MorphOS Popstring/Popobject/Popasl packet
// family. Dispatch consumers use the named records declared at the public
// boundary; only this adapter owns their packed guest-memory layout.
internal static class MuiPopSpecialistMessageCodec
{
	internal const uint OmDispose = 0x00000102u;
	internal const uint OmGet = 0x00000104u;
	internal const uint MethodSet = 0x8042549Au;
	internal const uint MethodNoNotifySet = 0x8042216Fu;

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiPopSpecialistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiPopSpecialistMethodMessage.Size)) return false;
		return MuiPopSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiPopSpecialistPacketKind.Method,
			MuiPopSpecialistField.MethodId, out packet.MethodId);
	}

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiPopSpecialistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsValidMethod(ref platform, message, method)) return false;
		packet.MethodId = method;
		return true;
	}

	// Native method-only consumers use this scalar form to avoid materializing
	// a one-field out record in compiler paths where it can widen the branch.
	internal static bool IsValidMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		return IsMethod(method) &&
			TryReadMethodId(ref platform, message, out var header) &&
			header.MethodId == method;
	}

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMethod(method) || message.IsNull || !platform.IsMapped(message,
			MuiPopSpecialistMethodMessage.Size)) return false;
		return MuiPopSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiPopSpecialistPacketKind.Method,
			MuiPopSpecialistField.MethodId, method);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiPopSpecialistGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiPopSpecialistGetMessage.Size,
			OmGet)) return false;
		if (!TryReadMethodId(ref platform, message, out var header)) return false;
		packet.MethodId = header.MethodId;
		return MuiPopSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiPopSpecialistPacketKind.Get,
			MuiPopSpecialistField.Attribute, out packet.Attribute) &&
			MuiPopSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiPopSpecialistPacketKind.Get,
				MuiPopSpecialistField.Storage, out packet.Storage);
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform,
		APTR message, uint attribute, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiPopSpecialistGetMessage.Size)) return false;
		return MuiPopSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiPopSpecialistPacketKind.Get,
			MuiPopSpecialistField.MethodId, OmGet) &&
			MuiPopSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiPopSpecialistPacketKind.Get,
				MuiPopSpecialistField.Attribute, attribute) &&
			MuiPopSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiPopSpecialistPacketKind.Get,
				MuiPopSpecialistField.Storage, storage);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiPopSpecialistSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsSetMethod(method) || !IsPacket(ref platform, message,
			MuiPopSpecialistSetMessage.Size, method)) return false;
		if (!TryReadMethodId(ref platform, message, out var header)) return false;
		packet.MethodId = header.MethodId;
		return MuiPopSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiPopSpecialistPacketKind.Set,
			MuiPopSpecialistField.Attribute, out packet.Attribute) &&
			MuiPopSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiPopSpecialistPacketKind.Set,
				MuiPopSpecialistField.Value, out packet.Value);
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSetMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiPopSpecialistSetMessage.Size)) return false;
		return MuiPopSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiPopSpecialistPacketKind.Set,
			MuiPopSpecialistField.MethodId, method) &&
			MuiPopSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiPopSpecialistPacketKind.Set,
				MuiPopSpecialistField.Attribute, attribute) &&
			MuiPopSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiPopSpecialistPacketKind.Set,
				MuiPopSpecialistField.Value, value);
	}

	internal static bool TryReadClose<TPlatform>(ref TPlatform platform,
		APTR message, out MuiPopSpecialistCloseMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiPopSpecialistMethodMessage.Size,
			MuiPopAttributes.Popstring_Close)) return false;
		if (!TryReadMethodId(ref platform, message, out var header)) return false;
		packet.MethodId = header.MethodId;
		// Preserve the MorphOS-compatible tolerant boundary: a method-only close
		// frame means result FALSE, while the documented second word is consumed
		// when present.
		if (platform.IsMapped(message, MuiPopSpecialistCloseMessage.Size))
			MuiPopSpecialistFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiPopSpecialistPacketKind.Close, MuiPopSpecialistField.Result,
				out packet.Result);
		return true;
	}

	internal static bool WriteClose<TPlatform>(ref TPlatform platform,
		APTR message, uint result)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiPopSpecialistCloseMessage.Size)) return false;
		return MuiPopSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiPopSpecialistPacketKind.Close,
			MuiPopSpecialistField.MethodId,
			MuiPopAttributes.Popstring_Close) &&
			MuiPopSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiPopSpecialistPacketKind.Close,
				MuiPopSpecialistField.Result, result);
	}

	private static bool IsMethod(uint method) => method == OmDispose ||
		method == MuiPopAttributes.Popstring_Open ||
		method == MuiPopAttributes.HandleInput ||
		method == MuiPopAttributes.Setup ||
		method == MuiPopAttributes.Cleanup;

	private static bool IsSetMethod(uint method) => method == MethodSet ||
		method == MethodNoNotifySet;

	private static bool IsPacket<TPlatform>(ref TPlatform platform, APTR message,
		uint size, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message, size) ||
			!TryReadMethodId(ref platform, message, out var header)) return false;
		return header.MethodId == method;
	}
}
