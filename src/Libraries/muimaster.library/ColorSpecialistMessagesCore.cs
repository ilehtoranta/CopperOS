/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

internal enum MuiColorSpecialistPacketKind : byte
{
	Method,
	Get,
	Set,
	Pointer,
	Rgb,
}

internal enum MuiColorSpecialistField : byte
{
	MethodId,
	Attribute,
	Storage,
	Value,
	Pointer,
	Red,
	Green,
	Blue,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiColorSpecialistFieldCursor
{
	internal APTR Message;
	internal MuiColorSpecialistPacketKind Packet;
	internal MuiColorSpecialistField Field;
}

internal static class MuiColorSpecialistFieldCursorCodec
{
	private static bool TryResolve(MuiColorSpecialistPacketKind packet,
		MuiColorSpecialistField field, out uint offset)
	{
		switch (packet)
		{
			case MuiColorSpecialistPacketKind.Method:
				if (field == MuiColorSpecialistField.MethodId) { offset = 0; return true; }
				break;
			case MuiColorSpecialistPacketKind.Get:
				if (field == MuiColorSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiColorSpecialistField.Attribute) { offset = 4; return true; }
				if (field == MuiColorSpecialistField.Storage) { offset = 8; return true; }
				break;
			case MuiColorSpecialistPacketKind.Set:
				if (field == MuiColorSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiColorSpecialistField.Attribute) { offset = 4; return true; }
				if (field == MuiColorSpecialistField.Value) { offset = 8; return true; }
				break;
			case MuiColorSpecialistPacketKind.Pointer:
				if (field == MuiColorSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiColorSpecialistField.Pointer) { offset = 4; return true; }
				break;
			case MuiColorSpecialistPacketKind.Rgb:
				if (field == MuiColorSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiColorSpecialistField.Red) { offset = 4; return true; }
				if (field == MuiColorSpecialistField.Green) { offset = 8; return true; }
				if (field == MuiColorSpecialistField.Blue) { offset = 12; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiColorSpecialistFieldCursor cursor, out APTR address)
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
		APTR message, MuiColorSpecialistPacketKind packet,
		MuiColorSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiColorSpecialistFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiColorSpecialistPacketKind packet,
		MuiColorSpecialistField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiColorSpecialistFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Central codec for the fixed MorphOS pen/color specialist packet family.
// Dispatch consumers use the named records declared at the public boundary;
// only this adapter owns their packed guest-memory layout.
internal static class MuiColorSpecialistMessageCodec
{
	internal const uint OmDispose = 0x00000102u;
	internal const uint OmGet = 0x00000104u;
	internal const uint MethodSet = 0x8042549Au;
	internal const uint MethodNoNotifySet = 0x8042216Fu;
	internal const uint SetColormap = 0x80426C80u;
	internal const uint SetMUIPen = 0x8042039Du;
	internal const uint SetRGB = 0x8042C131u;

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiColorSpecialistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiColorSpecialistMethodMessage.Size)) return false;
		return MuiColorSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiColorSpecialistPacketKind.Method,
			MuiColorSpecialistField.MethodId, out packet.MethodId);
	}

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiColorSpecialistMethodMessage packet)
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
			MuiColorSpecialistMethodMessage.Size)) return false;
		return MuiColorSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiColorSpecialistPacketKind.Method,
			MuiColorSpecialistField.MethodId, method);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiColorSpecialistGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiColorSpecialistGetMessage.Size,
			OmGet)) return false;
		if (!TryReadMethodId(ref platform, message, out var header)) return false;
		packet.MethodId = header.MethodId;
		return MuiColorSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiColorSpecialistPacketKind.Get,
			MuiColorSpecialistField.Attribute, out packet.Attribute) &&
			MuiColorSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiColorSpecialistPacketKind.Get,
				MuiColorSpecialistField.Storage, out packet.Storage);
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform,
		APTR message, uint attribute, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiColorSpecialistGetMessage.Size)) return false;
		return MuiColorSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiColorSpecialistPacketKind.Get,
			MuiColorSpecialistField.MethodId, OmGet) &&
			MuiColorSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiColorSpecialistPacketKind.Get,
				MuiColorSpecialistField.Attribute, attribute) &&
			MuiColorSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiColorSpecialistPacketKind.Get,
				MuiColorSpecialistField.Storage, storage);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiColorSpecialistSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsSetMethod(method) || !IsPacket(ref platform, message,
			MuiColorSpecialistSetMessage.Size, method)) return false;
		if (!TryReadMethodId(ref platform, message, out var header)) return false;
		packet.MethodId = header.MethodId;
		return MuiColorSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiColorSpecialistPacketKind.Set,
			MuiColorSpecialistField.Attribute, out packet.Attribute) &&
			MuiColorSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiColorSpecialistPacketKind.Set,
				MuiColorSpecialistField.Value, out packet.Value);
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSetMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiColorSpecialistSetMessage.Size)) return false;
		return MuiColorSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiColorSpecialistPacketKind.Set,
			MuiColorSpecialistField.MethodId, method) &&
			MuiColorSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiColorSpecialistPacketKind.Set,
				MuiColorSpecialistField.Attribute, attribute) &&
			MuiColorSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiColorSpecialistPacketKind.Set,
				MuiColorSpecialistField.Value, value);
	}

	internal static bool TryReadPointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiColorSpecialistPointerMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPointerMethod(method) || !IsPacket(ref platform, message,
			MuiColorSpecialistPointerMessage.Size, method)) return false;
		if (!TryReadMethodId(ref platform, message, out var header)) return false;
		packet.MethodId = header.MethodId;
		return MuiColorSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiColorSpecialistPacketKind.Pointer,
			MuiColorSpecialistField.Pointer, out packet.Pointer);
	}

	internal static bool WritePointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint pointer)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsPointerMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiColorSpecialistPointerMessage.Size)) return false;
		return MuiColorSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiColorSpecialistPacketKind.Pointer,
			MuiColorSpecialistField.MethodId, method) &&
			MuiColorSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiColorSpecialistPacketKind.Pointer,
				MuiColorSpecialistField.Pointer, pointer);
	}

	internal static bool TryReadRgb<TPlatform>(ref TPlatform platform,
		APTR message, out MuiColorSpecialistRgbMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiColorSpecialistRgbMessage.Size,
			SetRGB)) return false;
		if (!TryReadMethodId(ref platform, message, out var header)) return false;
		packet.MethodId = header.MethodId;
		return MuiColorSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiColorSpecialistPacketKind.Rgb,
			MuiColorSpecialistField.Red, out packet.Red) &&
			MuiColorSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiColorSpecialistPacketKind.Rgb,
				MuiColorSpecialistField.Green, out packet.Green) &&
				MuiColorSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
					message, MuiColorSpecialistPacketKind.Rgb,
					MuiColorSpecialistField.Blue, out packet.Blue);
	}

	internal static bool WriteRgb<TPlatform>(ref TPlatform platform,
		APTR message, uint red, uint green, uint blue)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiColorSpecialistRgbMessage.Size)) return false;
		return MuiColorSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiColorSpecialistPacketKind.Rgb,
			MuiColorSpecialistField.MethodId, SetRGB) &&
			MuiColorSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiColorSpecialistPacketKind.Rgb,
				MuiColorSpecialistField.Red, red) &&
				MuiColorSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
					message, MuiColorSpecialistPacketKind.Rgb,
					MuiColorSpecialistField.Green, green) &&
					MuiColorSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
						message, MuiColorSpecialistPacketKind.Rgb,
						MuiColorSpecialistField.Blue, blue);
	}

	private static bool IsMethod(uint method) => method == OmDispose;

	private static bool IsSetMethod(uint method) => method == MethodSet ||
		method == MethodNoNotifySet;

	private static bool IsPointerMethod(uint method) => method == SetColormap ||
		method == SetMUIPen;

	private static bool IsPacket<TPlatform>(ref TPlatform platform, APTR message,
		uint size, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message, size) ||
			!TryReadMethodId(ref platform, message, out var header)) return false;
		return header.MethodId == method;
	}
}
