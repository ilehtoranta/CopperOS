/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

internal enum MuiMiscSpecialistPacketKind : byte
{
	Method,
	Lifecycle,
	Get,
	Set,
	Pointer,
	Pair,
	RegisterGadget,
}

internal enum MuiMiscSpecialistField : byte
{
	MethodId,
	Attribute,
	Storage,
	Value,
	Pointer,
	First,
	Second,
	Gadget,
	Id,
	Parameters,
	Title,
	Label,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscSpecialistFieldCursor
{
	internal APTR Message;
	internal MuiMiscSpecialistPacketKind Packet;
	internal MuiMiscSpecialistField Field;
}

internal static class MuiMiscSpecialistFieldCursorCodec
{
	private static bool TryResolve(MuiMiscSpecialistPacketKind packet,
		MuiMiscSpecialistField field, out uint offset)
	{
		switch (packet)
		{
			case MuiMiscSpecialistPacketKind.Method:
			case MuiMiscSpecialistPacketKind.Lifecycle:
				if (field == MuiMiscSpecialistField.MethodId) { offset = 0; return true; }
				break;
			case MuiMiscSpecialistPacketKind.Get:
				if (field == MuiMiscSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiMiscSpecialistField.Attribute) { offset = 4; return true; }
				if (field == MuiMiscSpecialistField.Storage) { offset = 8; return true; }
				break;
			case MuiMiscSpecialistPacketKind.Set:
				if (field == MuiMiscSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiMiscSpecialistField.Attribute) { offset = 4; return true; }
				if (field == MuiMiscSpecialistField.Value) { offset = 8; return true; }
				break;
			case MuiMiscSpecialistPacketKind.Pointer:
				if (field == MuiMiscSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiMiscSpecialistField.Pointer) { offset = 4; return true; }
				break;
			case MuiMiscSpecialistPacketKind.Pair:
				if (field == MuiMiscSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiMiscSpecialistField.First) { offset = 4; return true; }
				if (field == MuiMiscSpecialistField.Second) { offset = 8; return true; }
				break;
			case MuiMiscSpecialistPacketKind.RegisterGadget:
				if (field == MuiMiscSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiMiscSpecialistField.Gadget) { offset = 4; return true; }
				if (field == MuiMiscSpecialistField.Id) { offset = 8; return true; }
				if (field == MuiMiscSpecialistField.Parameters) { offset = 12; return true; }
				if (field == MuiMiscSpecialistField.Title) { offset = 16; return true; }
				if (field == MuiMiscSpecialistField.Attribute) { offset = 20; return true; }
				if (field == MuiMiscSpecialistField.Label) { offset = 24; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMiscSpecialistFieldCursor cursor, out APTR address)
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
		APTR message, MuiMiscSpecialistPacketKind packet,
		MuiMiscSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiMiscSpecialistFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiMiscSpecialistPacketKind packet,
		MuiMiscSpecialistField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiMiscSpecialistFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Shared fixed-packet boundary for the Misc specialist family. The public
// records remain the consumer-facing shape; only this adapter knows the
// packed guest offsets and mapping checks. Both standalone and object-aware
// dispatchers use this codec so their ABI validation cannot drift apart.
internal static class MuiMiscSpecialistMessageCodec
{
	internal const uint OmDispose = 0x00000102u;
	internal const uint OmGet = 0x00000104u;
	internal const uint MethodSet = 0x8042549Au;
	internal const uint MethodNoNotifySet = 0x8042216Fu;

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMiscSpecialistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiMiscSpecialistMethodMessage.Size)) return false;
		return MuiMiscSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Method,
			MuiMiscSpecialistField.MethodId, out packet.MethodId);
	}

	internal static bool TryReadLifecycle<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMiscLifecycleMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsLifecycleMethod(method) || message.IsNull ||
			!platform.IsMapped(message, MuiMiscLifecycleMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != method) return false;
		return MuiMiscSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Lifecycle,
			MuiMiscSpecialistField.MethodId, out packet.MethodId);
	}

	internal static bool WriteLifecycle<TPlatform>(ref TPlatform platform,
		APTR message, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsLifecycleMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiMiscLifecycleMessage.Size)) return false;
		return MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Lifecycle,
			MuiMiscSpecialistField.MethodId, method);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMiscSpecialistGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiMiscSpecialistGetMessage.Size,
			OmGet)) return false;
		if (!TryReadMethodId(ref platform, message, out var header)) return false;
		packet.MethodId = header.MethodId;
		return MuiMiscSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Get,
			MuiMiscSpecialistField.Attribute, out packet.Attribute) &&
			MuiMiscSpecialistFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiMiscSpecialistPacketKind.Get, MuiMiscSpecialistField.Storage,
				out packet.Storage);
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform,
		APTR message, uint attribute, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiMiscSpecialistGetMessage.Size)) return false;
		return MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Get,
			MuiMiscSpecialistField.MethodId, OmGet) &&
			MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiMiscSpecialistPacketKind.Get,
				MuiMiscSpecialistField.Attribute, attribute) &&
				MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
					message, MuiMiscSpecialistPacketKind.Get,
					MuiMiscSpecialistField.Storage, storage);
	}

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMiscSpecialistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiMiscSpecialistMethodMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != method) return false;
		packet.MethodId = header.MethodId;
		return true;
	}

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiMiscSpecialistMethodMessage.Size)) return false;
		return MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Method,
			MuiMiscSpecialistField.MethodId, method);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMiscSpecialistSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsSetMethod(method) || !IsPacket(ref platform, message,
			MuiMiscSpecialistSetMessage.Size, method)) return false;
		if (!TryReadMethodId(ref platform, message, out var header)) return false;
		packet.MethodId = header.MethodId;
		return MuiMiscSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Set,
			MuiMiscSpecialistField.Attribute, out packet.Attribute) &&
			MuiMiscSpecialistFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiMiscSpecialistPacketKind.Set, MuiMiscSpecialistField.Value,
				out packet.Value);
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform, APTR message,
		uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSetMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiMiscSpecialistSetMessage.Size)) return false;
		return MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Set,
			MuiMiscSpecialistField.MethodId, method) &&
			MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiMiscSpecialistPacketKind.Set,
				MuiMiscSpecialistField.Attribute, attribute) &&
				MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform, message,
					MuiMiscSpecialistPacketKind.Set, MuiMiscSpecialistField.Value,
					value);
	}

	internal static bool TryReadPointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMiscSpecialistPointerMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message,
			MuiMiscSpecialistPointerMessage.Size, method)) return false;
		if (!TryReadMethodId(ref platform, message, out var header)) return false;
		packet.MethodId = header.MethodId;
		return MuiMiscSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Pointer,
			MuiMiscSpecialistField.Pointer, out packet.Pointer);
	}

	internal static bool WritePointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint pointer)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiMiscSpecialistPointerMessage.Size)) return false;
		return MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Pointer,
			MuiMiscSpecialistField.MethodId, method) &&
			MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiMiscSpecialistPacketKind.Pointer, MuiMiscSpecialistField.Pointer,
				pointer);
	}

	internal static bool TryReadPair<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMiscSpecialistPairMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message,
			MuiMiscSpecialistPairMessage.Size, method)) return false;
		if (!TryReadMethodId(ref platform, message, out var header)) return false;
		packet.MethodId = header.MethodId;
		return MuiMiscSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Pair,
			MuiMiscSpecialistField.First, out packet.First) &&
			MuiMiscSpecialistFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiMiscSpecialistPacketKind.Pair, MuiMiscSpecialistField.Second,
				out packet.Second);
	}

	internal static bool WritePair<TPlatform>(ref TPlatform platform, APTR message,
		uint method, uint first, uint second)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiMiscSpecialistPairMessage.Size)) return false;
		return MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Pair,
			MuiMiscSpecialistField.MethodId, method) &&
			MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiMiscSpecialistPacketKind.Pair, MuiMiscSpecialistField.First, first) &&
			MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiMiscSpecialistPacketKind.Pair, MuiMiscSpecialistField.Second, second);
	}

	internal static bool TryReadRegisterGadget<TPlatform>(
		ref TPlatform platform, APTR message,
		out MuiMiscSpecialistRegisterGadgetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message,
			MuiMiscSpecialistRegisterGadgetMessage.Size,
			MuiMiscAttributes.Mccprefs_RegisterGadget)) return false;
		if (!TryReadMethodId(ref platform, message, out var header)) return false;
		packet.MethodId = header.MethodId;
		return MuiMiscSpecialistFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiMiscSpecialistPacketKind.RegisterGadget,
			MuiMiscSpecialistField.Gadget, out packet.Gadget) &&
			MuiMiscSpecialistFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiMiscSpecialistPacketKind.RegisterGadget,
				MuiMiscSpecialistField.Id, out packet.Id) &&
				MuiMiscSpecialistFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiMiscSpecialistPacketKind.RegisterGadget,
					MuiMiscSpecialistField.Parameters, out packet.Parameters) &&
					MuiMiscSpecialistFieldCursorCodec.TryReadUInt32(ref platform, message,
						MuiMiscSpecialistPacketKind.RegisterGadget,
						MuiMiscSpecialistField.Title, out packet.Title) &&
						MuiMiscSpecialistFieldCursorCodec.TryReadUInt32(ref platform, message,
							MuiMiscSpecialistPacketKind.RegisterGadget,
							MuiMiscSpecialistField.Attribute, out packet.Attribute) &&
							MuiMiscSpecialistFieldCursorCodec.TryReadUInt32(ref platform, message,
								MuiMiscSpecialistPacketKind.RegisterGadget,
								MuiMiscSpecialistField.Label, out packet.Label);
	}

	internal static bool WriteRegisterGadget<TPlatform>(ref TPlatform platform,
		APTR message, uint gadget, uint id, uint parameters, uint title,
		uint attribute, uint label) where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiMiscSpecialistRegisterGadgetMessage.Size)) return false;
		return MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiMiscSpecialistPacketKind.RegisterGadget,
			MuiMiscSpecialistField.MethodId,
			MuiMiscAttributes.Mccprefs_RegisterGadget) &&
			MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiMiscSpecialistPacketKind.RegisterGadget,
				MuiMiscSpecialistField.Gadget, gadget) &&
				MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform, message,
					MuiMiscSpecialistPacketKind.RegisterGadget,
					MuiMiscSpecialistField.Id, id) &&
					MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform, message,
						MuiMiscSpecialistPacketKind.RegisterGadget,
						MuiMiscSpecialistField.Parameters, parameters) &&
						MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform, message,
							MuiMiscSpecialistPacketKind.RegisterGadget,
							MuiMiscSpecialistField.Title, title) &&
							MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform, message,
								MuiMiscSpecialistPacketKind.RegisterGadget,
								MuiMiscSpecialistField.Attribute, attribute) &&
								MuiMiscSpecialistFieldCursorCodec.TryWriteUInt32(ref platform, message,
									MuiMiscSpecialistPacketKind.RegisterGadget,
									MuiMiscSpecialistField.Label, label);
	}

	private static bool IsLifecycleMethod(uint method) =>
		method == MuiMiscAttributes.Setup || method == MuiMiscAttributes.Cleanup;

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
