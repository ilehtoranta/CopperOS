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

// Struct-first guest-memory adapter for the fixed Menustrip/Menu/Menuitem
// packets. Packet kinds own complete MorphOS record spans; field names select
// members without exposing numeric positions to dispatch code.
internal static class MuiMenuSpecialistMessageMemoryCodec
{
	private static bool TryResolve(MuiMenuSpecialistPacketKind packet,
		MuiMenuSpecialistField field, out uint offset, out uint size)
	{
		switch (packet)
		{
			case MuiMenuSpecialistPacketKind.Method:
				size = MuiMenuSpecialistMethodMessage.Size;
				if (field == MuiMenuSpecialistField.MethodId)
					offset = MuiMenuSpecialistMethodMessage.MethodIdOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiMenuSpecialistPacketKind.Get:
				size = MuiMenuSpecialistGetMessage.Size;
				if (field == MuiMenuSpecialistField.MethodId)
					offset = MuiMenuSpecialistGetMessage.MethodIdOffset;
				else if (field == MuiMenuSpecialistField.Attribute)
					offset = MuiMenuSpecialistGetMessage.AttributeOffset;
				else if (field == MuiMenuSpecialistField.Storage)
					offset = MuiMenuSpecialistGetMessage.StorageOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiMenuSpecialistPacketKind.Set:
				size = MuiMenuSpecialistSetMessage.Size;
				if (field == MuiMenuSpecialistField.MethodId)
					offset = MuiMenuSpecialistSetMessage.MethodIdOffset;
				else if (field == MuiMenuSpecialistField.Attribute)
					offset = MuiMenuSpecialistSetMessage.AttributeOffset;
				else if (field == MuiMenuSpecialistField.Value)
					offset = MuiMenuSpecialistSetMessage.ValueOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiMenuSpecialistPacketKind.Pointer:
				size = MuiMenuSpecialistPointerMessage.Size;
				if (field == MuiMenuSpecialistField.MethodId)
					offset = MuiMenuSpecialistPointerMessage.MethodIdOffset;
				else if (field == MuiMenuSpecialistField.ObjectPointer)
					offset = MuiMenuSpecialistPointerMessage.ObjectPointerOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiMenuSpecialistPacketKind.Pair:
				size = MuiMenuSpecialistPairMessage.Size;
				if (field == MuiMenuSpecialistField.MethodId)
					offset = MuiMenuSpecialistPairMessage.MethodIdOffset;
				else if (field == MuiMenuSpecialistField.First)
					offset = MuiMenuSpecialistPairMessage.FirstOffset;
				else if (field == MuiMenuSpecialistField.Second)
					offset = MuiMenuSpecialistPairMessage.SecondOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiMenuSpecialistPacketKind.Popup:
				size = MuiMenuSpecialistPopupMessage.Size;
				if (field == MuiMenuSpecialistField.MethodId)
					offset = MuiMenuSpecialistPopupMessage.MethodIdOffset;
				else if (field == MuiMenuSpecialistField.Window)
					offset = MuiMenuSpecialistPopupMessage.WindowOffset;
				else if (field == MuiMenuSpecialistField.X)
					offset = MuiMenuSpecialistPopupMessage.XOffset;
				else if (field == MuiMenuSpecialistField.Y)
					offset = MuiMenuSpecialistPopupMessage.YOffset;
				else { offset = 0; size = 0; return false; }
				return true;
		}
		offset = 0;
		size = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiMenuSpecialistPacketKind packet,
		MuiMenuSpecialistField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset, out var size) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, size)) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiMenuSpecialistMethodMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiMenuSpecialistPacketKind packet,
		MuiMenuSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiMenuSpecialistPacketKind packet,
		MuiMenuSpecialistField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// field cursor. The live message codecs route to the struct adapter above.
internal static class MuiMenuSpecialistFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMenuSpecialistFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMenuSpecialistMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Packet, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiMenuSpecialistPacketKind packet,
		MuiMenuSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMenuSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiMenuSpecialistPacketKind packet,
		MuiMenuSpecialistField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMenuSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, packet, field, value);
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
		uint methodId;
		if (!IsMethod(method) || !TryReadMethodIdValue(ref platform, message,
			out methodId) || methodId != method) return false;
		packet.MethodId = methodId;
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
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	// Native qualification keeps method-header admission scalar while the
	// dispatcher-facing overload above retains the named value-type record.
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (message.IsNull || !platform.IsMapped(message,
			MuiMenuSpecialistMethodMessage.Size)) return false;
		return MuiMenuSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Method,
			MuiMenuSpecialistField.MethodId, out methodId);
	}

	internal static bool IsValidMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMethod(method)) return false;
		uint methodId;
		return TryReadMethodIdValue(ref platform, message, out methodId) &&
			methodId == method;
	}

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMethod(method) || message.IsNull || !platform.IsMapped(message,
			MuiMenuSpecialistMethodMessage.Size)) return false;
		return MuiMenuSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
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
		return MuiMenuSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Get,
			MuiMenuSpecialistField.MethodId, out packet.MethodId) &&
			MuiMenuSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Get,
				MuiMenuSpecialistField.Attribute, out packet.Attribute) &&
				MuiMenuSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
					message, MuiMenuSpecialistPacketKind.Get,
					MuiMenuSpecialistField.Storage, out packet.Storage);
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform,
		APTR message, uint attribute, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiMenuSpecialistGetMessage.Size)) return false;
		return MuiMenuSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Get,
			MuiMenuSpecialistField.MethodId, OmGet) &&
			MuiMenuSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Get,
				MuiMenuSpecialistField.Attribute, attribute) &&
				MuiMenuSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
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
		return MuiMenuSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Set,
			MuiMenuSpecialistField.MethodId, out packet.MethodId) &&
			MuiMenuSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Set,
				MuiMenuSpecialistField.Attribute, out packet.Attribute) &&
				MuiMenuSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
					message, MuiMenuSpecialistPacketKind.Set,
					MuiMenuSpecialistField.Value, out packet.Value);
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSetMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiMenuSpecialistSetMessage.Size)) return false;
		return MuiMenuSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Set,
			MuiMenuSpecialistField.MethodId, method) &&
			MuiMenuSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Set,
				MuiMenuSpecialistField.Attribute, attribute) &&
				MuiMenuSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
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
		return MuiMenuSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Pointer,
			MuiMenuSpecialistField.MethodId, out packet.MethodId) &&
			MuiMenuSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Pointer,
				MuiMenuSpecialistField.ObjectPointer, out packet.ObjectPointer);
	}

	internal static bool WritePointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint objectPointer)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsPointerMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiMenuSpecialistPointerMessage.Size)) return false;
		return MuiMenuSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Pointer,
			MuiMenuSpecialistField.MethodId, method) &&
			MuiMenuSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
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
		return MuiMenuSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Pair,
			MuiMenuSpecialistField.MethodId, out packet.MethodId) &&
			MuiMenuSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Pair,
				MuiMenuSpecialistField.First, out packet.First) &&
				MuiMenuSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
					message, MuiMenuSpecialistPacketKind.Pair,
					MuiMenuSpecialistField.Second, out packet.Second);
	}

	internal static bool WritePair<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint first, uint second)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsPairMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiMenuSpecialistPairMessage.Size)) return false;
		return MuiMenuSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Pair,
			MuiMenuSpecialistField.MethodId, method) &&
			MuiMenuSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Pair,
				MuiMenuSpecialistField.First, first) &&
				MuiMenuSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
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
		return MuiMenuSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Popup,
			MuiMenuSpecialistField.MethodId, out packet.MethodId) &&
			MuiMenuSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Popup,
				MuiMenuSpecialistField.Window, out packet.Window) &&
				MuiMenuSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
					message, MuiMenuSpecialistPacketKind.Popup,
					MuiMenuSpecialistField.X, out packet.X) &&
					MuiMenuSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
						message, MuiMenuSpecialistPacketKind.Popup,
						MuiMenuSpecialistField.Y, out packet.Y);
	}

	internal static bool WritePopup<TPlatform>(ref TPlatform platform,
		APTR message, uint window, uint x, uint y)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiMenuSpecialistPopupMessage.Size)) return false;
		return MuiMenuSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiMenuSpecialistPacketKind.Popup,
			MuiMenuSpecialistField.MethodId, MuiMenuAttributes.Menustrip_Popup) &&
			MuiMenuSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
				message, MuiMenuSpecialistPacketKind.Popup,
				MuiMenuSpecialistField.Window, window) &&
				MuiMenuSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
					message, MuiMenuSpecialistPacketKind.Popup,
					MuiMenuSpecialistField.X, x) &&
					MuiMenuSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
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
		TryReadMethodIdValue(ref platform, message, out uint methodId) &&
		methodId == method && platform.IsMapped(message, size);
}
