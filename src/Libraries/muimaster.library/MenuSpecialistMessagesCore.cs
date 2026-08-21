/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

internal enum MuiMenuSpecialistPacketKind : byte
{
	Method,
	Get,
	Set,
	Pointer,
	Pair,
	Popup,
}

internal enum MuiMenuSpecialistField : byte
{
	MethodId,
	Attribute,
	Storage,
	Value,
	ObjectPointer,
	First,
	Second,
	Window,
	X,
	Y,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMenuSpecialistFieldCursor
{
	internal APTR Message;
	internal MuiMenuSpecialistPacketKind Packet;
	internal MuiMenuSpecialistField Field;
}

internal static class MuiMenuSpecialistFieldCursorCodec
{
	private static bool TryResolve(MuiMenuSpecialistPacketKind packet,
		MuiMenuSpecialistField field, out uint offset)
	{
		switch (packet)
		{
			case MuiMenuSpecialistPacketKind.Method:
				if (field == MuiMenuSpecialistField.MethodId) { offset = 0; return true; }
				break;
			case MuiMenuSpecialistPacketKind.Get:
				if (field == MuiMenuSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiMenuSpecialistField.Attribute) { offset = 4; return true; }
				if (field == MuiMenuSpecialistField.Storage) { offset = 8; return true; }
				break;
			case MuiMenuSpecialistPacketKind.Set:
				if (field == MuiMenuSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiMenuSpecialistField.Attribute) { offset = 4; return true; }
				if (field == MuiMenuSpecialistField.Value) { offset = 8; return true; }
				break;
			case MuiMenuSpecialistPacketKind.Pointer:
				if (field == MuiMenuSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiMenuSpecialistField.ObjectPointer) { offset = 4; return true; }
				break;
			case MuiMenuSpecialistPacketKind.Pair:
				if (field == MuiMenuSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiMenuSpecialistField.First) { offset = 4; return true; }
				if (field == MuiMenuSpecialistField.Second) { offset = 8; return true; }
				break;
			case MuiMenuSpecialistPacketKind.Popup:
				if (field == MuiMenuSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiMenuSpecialistField.Window) { offset = 4; return true; }
				if (field == MuiMenuSpecialistField.X) { offset = 8; return true; }
				if (field == MuiMenuSpecialistField.Y) { offset = 12; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMenuSpecialistFieldCursor cursor, out APTR address)
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
		APTR message, MuiMenuSpecialistPacketKind packet,
		MuiMenuSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiMenuSpecialistFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiMenuSpecialistPacketKind packet,
		MuiMenuSpecialistField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiMenuSpecialistFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Central codec for the fixed MorphOS Menustrip/Menu/Menuitem packet family.
// Dispatch consumers use the named records declared at the public boundary;
// only this adapter owns their packed guest-memory layout.
internal static class MuiMenuSpecialistMessageCodec
{
	internal const uint OmDispose = 0x00000102u;
	internal const uint OmGet = 0x00000104u;
	internal const uint MethodSet = 0x8042549Au;
	internal const uint MethodNoNotifySet = 0x8042216Fu;

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMenuSpecialistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsMethod(method) || !TryReadMethodId(ref platform, message,
			out var header) || header.MethodId != method) return false;
		packet.MethodId = header.MethodId;
		return true;
	}

	// Read the fixed menu-specialist method header without constraining the
	// selector. Dispatch selection uses this named record; specialized codecs
	// retain validation of complete packet shapes.
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMenuSpecialistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiMenuSpecialistMethodMessage.Size)) return false;
		return MuiMenuSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Method,
			MuiMenuSpecialistField.MethodId, out packet.MethodId);
	}

	internal static bool IsValidMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory =>
		IsMethod(method) && TryReadMethodId(ref platform, message,
			out var header) && header.MethodId == method;

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMethod(method) || message.IsNull || !platform.IsMapped(message,
			MuiMenuSpecialistMethodMessage.Size)) return false;
		return MuiMenuSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Method,
			MuiMenuSpecialistField.MethodId, method);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMenuSpecialistGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiMenuSpecialistGetMessage.Size,
			OmGet)) return false;
		return MuiMenuSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Get,
			MuiMenuSpecialistField.MethodId, out packet.MethodId) &&
			MuiMenuSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Get,
				MuiMenuSpecialistField.Attribute, out packet.Attribute) &&
				MuiMenuSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
					message, MuiMenuSpecialistPacketKind.Get,
					MuiMenuSpecialistField.Storage, out packet.Storage);
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform,
		APTR message, uint attribute, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiMenuSpecialistGetMessage.Size)) return false;
		return MuiMenuSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Get,
			MuiMenuSpecialistField.MethodId, OmGet) &&
			MuiMenuSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Get,
				MuiMenuSpecialistField.Attribute, attribute) &&
				MuiMenuSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
					message, MuiMenuSpecialistPacketKind.Get,
					MuiMenuSpecialistField.Storage, storage);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMenuSpecialistSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsSetMethod(method) || !IsPacket(ref platform, message,
			MuiMenuSpecialistSetMessage.Size, method)) return false;
		return MuiMenuSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Set,
			MuiMenuSpecialistField.MethodId, out packet.MethodId) &&
			MuiMenuSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Set,
				MuiMenuSpecialistField.Attribute, out packet.Attribute) &&
				MuiMenuSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
					message, MuiMenuSpecialistPacketKind.Set,
					MuiMenuSpecialistField.Value, out packet.Value);
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSetMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiMenuSpecialistSetMessage.Size)) return false;
		return MuiMenuSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Set,
			MuiMenuSpecialistField.MethodId, method) &&
			MuiMenuSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Set,
				MuiMenuSpecialistField.Attribute, attribute) &&
				MuiMenuSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
					message, MuiMenuSpecialistPacketKind.Set,
					MuiMenuSpecialistField.Value, value);
	}

	internal static bool TryReadPointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMenuSpecialistPointerMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPointerMethod(method) || !IsPacket(ref platform, message,
			MuiMenuSpecialistPointerMessage.Size, method)) return false;
		return MuiMenuSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Pointer,
			MuiMenuSpecialistField.MethodId, out packet.MethodId) &&
			MuiMenuSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Pointer,
				MuiMenuSpecialistField.ObjectPointer, out packet.ObjectPointer);
	}

	internal static bool WritePointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint objectPointer)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsPointerMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiMenuSpecialistPointerMessage.Size)) return false;
		return MuiMenuSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Pointer,
			MuiMenuSpecialistField.MethodId, method) &&
			MuiMenuSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Pointer,
				MuiMenuSpecialistField.ObjectPointer, objectPointer);
	}

	internal static bool TryReadPair<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMenuSpecialistPairMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPairMethod(method) || !IsPacket(ref platform, message,
			MuiMenuSpecialistPairMessage.Size, method)) return false;
		return MuiMenuSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Pair,
			MuiMenuSpecialistField.MethodId, out packet.MethodId) &&
			MuiMenuSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Pair,
				MuiMenuSpecialistField.First, out packet.First) &&
				MuiMenuSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
					message, MuiMenuSpecialistPacketKind.Pair,
					MuiMenuSpecialistField.Second, out packet.Second);
	}

	internal static bool WritePair<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint first, uint second)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsPairMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiMenuSpecialistPairMessage.Size)) return false;
		return MuiMenuSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Pair,
			MuiMenuSpecialistField.MethodId, method) &&
			MuiMenuSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Pair,
				MuiMenuSpecialistField.First, first) &&
				MuiMenuSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
					message, MuiMenuSpecialistPacketKind.Pair,
					MuiMenuSpecialistField.Second, second);
	}

	internal static bool TryReadPopup<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMenuSpecialistPopupMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiMenuSpecialistPopupMessage.Size,
			MuiMenuAttributes.Menustrip_Popup)) return false;
		return MuiMenuSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Popup,
			MuiMenuSpecialistField.MethodId, out packet.MethodId) &&
			MuiMenuSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Popup,
				MuiMenuSpecialistField.Window, out packet.Window) &&
				MuiMenuSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
					message, MuiMenuSpecialistPacketKind.Popup,
					MuiMenuSpecialistField.X, out packet.X) &&
					MuiMenuSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
						message, MuiMenuSpecialistPacketKind.Popup,
						MuiMenuSpecialistField.Y, out packet.Y);
	}

	internal static bool WritePopup<TPlatform>(ref TPlatform platform,
		APTR message, uint window, uint x, uint y)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiMenuSpecialistPopupMessage.Size)) return false;
		return MuiMenuSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Popup,
			MuiMenuSpecialistField.MethodId, MuiMenuAttributes.Menustrip_Popup) &&
			MuiMenuSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Popup,
				MuiMenuSpecialistField.Window, window) &&
				MuiMenuSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
					message, MuiMenuSpecialistPacketKind.Popup,
					MuiMenuSpecialistField.X, x) &&
					MuiMenuSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
						message, MuiMenuSpecialistPacketKind.Popup,
						MuiMenuSpecialistField.Y, y);
	}

	private static bool IsMethod(uint method) => method == OmDispose ||
		method == MuiMenuAttributes.Menustrip_InitChange ||
		method == MuiMenuAttributes.Menustrip_ExitChange ||
		method == MuiMenuAttributes.Menustrip_WillOpen;

	private static bool IsSetMethod(uint method) => method == MethodSet ||
		method == MethodNoNotifySet;

	private static bool IsPointerMethod(uint method) =>
		method == MuiMenuAttributes.Family_AddTail ||
		method == MuiMenuAttributes.Family_AddHead ||
		method == MuiMenuAttributes.Family_Remove ||
		method == MuiMenuAttributes.Family_Sort ||
		method == MuiMenuAttributes.Family_Transfer;

	private static bool IsPairMethod(uint method) =>
		method == MuiMenuAttributes.Family_Insert ||
		method == MuiMenuAttributes.Family_Reorder;

	private static bool IsPacket<TPlatform>(ref TPlatform platform, APTR message,
		uint size, uint method) where TPlatform : struct, IMuiGuestMemory =>
		TryReadMethodId(ref platform, message, out var header) &&
		header.MethodId == method && platform.IsMapped(message, size);
}
