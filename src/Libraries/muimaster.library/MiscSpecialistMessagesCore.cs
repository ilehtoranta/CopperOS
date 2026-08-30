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
	HandleInput,
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
	IntuiMessage,
	MuiKey,
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

// Struct-first guest-memory adapter for the fixed Misc specialist packets.
// Packet kinds own complete MorphOS record spans; field names select members
// without exposing numeric positions to dispatch code.
internal static class MuiMiscSpecialistMessageMemoryCodec
{
	private static bool TryResolve(MuiMiscSpecialistPacketKind packet,
		MuiMiscSpecialistField field, out uint offset, out uint size)
	{
		switch (packet)
		{
			case MuiMiscSpecialistPacketKind.Method:
				size = MuiMiscSpecialistMethodMessage.Size;
				if (field == MuiMiscSpecialistField.MethodId)
					offset = MuiMiscSpecialistMethodMessage.MethodIdOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiMiscSpecialistPacketKind.Lifecycle:
				size = MuiMiscLifecycleMessage.Size;
				if (field == MuiMiscSpecialistField.MethodId)
					offset = MuiMiscLifecycleMessage.MethodIdOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiMiscSpecialistPacketKind.Get:
				size = MuiMiscSpecialistGetMessage.Size;
				if (field == MuiMiscSpecialistField.MethodId)
					offset = MuiMiscSpecialistGetMessage.MethodIdOffset;
				else if (field == MuiMiscSpecialistField.Attribute)
					offset = MuiMiscSpecialistGetMessage.AttributeOffset;
				else if (field == MuiMiscSpecialistField.Storage)
					offset = MuiMiscSpecialistGetMessage.StorageOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiMiscSpecialistPacketKind.Set:
				size = MuiMiscSpecialistSetMessage.Size;
				if (field == MuiMiscSpecialistField.MethodId)
					offset = MuiMiscSpecialistSetMessage.MethodIdOffset;
				else if (field == MuiMiscSpecialistField.Attribute)
					offset = MuiMiscSpecialistSetMessage.AttributeOffset;
				else if (field == MuiMiscSpecialistField.Value)
					offset = MuiMiscSpecialistSetMessage.ValueOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiMiscSpecialistPacketKind.Pointer:
				size = MuiMiscSpecialistPointerMessage.Size;
				if (field == MuiMiscSpecialistField.MethodId)
					offset = MuiMiscSpecialistPointerMessage.MethodIdOffset;
				else if (field == MuiMiscSpecialistField.Pointer)
					offset = MuiMiscSpecialistPointerMessage.PointerOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiMiscSpecialistPacketKind.Pair:
				size = MuiMiscSpecialistPairMessage.Size;
				if (field == MuiMiscSpecialistField.MethodId)
					offset = MuiMiscSpecialistPairMessage.MethodIdOffset;
				else if (field == MuiMiscSpecialistField.First)
					offset = MuiMiscSpecialistPairMessage.FirstOffset;
				else if (field == MuiMiscSpecialistField.Second)
					offset = MuiMiscSpecialistPairMessage.SecondOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiMiscSpecialistPacketKind.HandleInput:
				size = MuiMiscHandleInputMessage.Size;
				if (field == MuiMiscSpecialistField.MethodId)
					offset = MuiMiscHandleInputMessage.MethodIdOffset;
				else if (field == MuiMiscSpecialistField.IntuiMessage)
					offset = MuiMiscHandleInputMessage.IntuiMessageOffset;
				else if (field == MuiMiscSpecialistField.MuiKey)
					offset = MuiMiscHandleInputMessage.MuiKeyOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiMiscSpecialistPacketKind.RegisterGadget:
				size = MuiMiscSpecialistRegisterGadgetMessage.Size;
				if (field == MuiMiscSpecialistField.MethodId)
					offset = MuiMiscSpecialistRegisterGadgetMessage.MethodIdOffset;
				else if (field == MuiMiscSpecialistField.Gadget)
					offset = MuiMiscSpecialistRegisterGadgetMessage.GadgetOffset;
				else if (field == MuiMiscSpecialistField.Id)
					offset = MuiMiscSpecialistRegisterGadgetMessage.IdOffset;
				else if (field == MuiMiscSpecialistField.Parameters)
					offset = MuiMiscSpecialistRegisterGadgetMessage.ParametersOffset;
				else if (field == MuiMiscSpecialistField.Title)
					offset = MuiMiscSpecialistRegisterGadgetMessage.TitleOffset;
				else if (field == MuiMiscSpecialistField.Attribute)
					offset = MuiMiscSpecialistRegisterGadgetMessage.AttributeOffset;
				else if (field == MuiMiscSpecialistField.Label)
					offset = MuiMiscSpecialistRegisterGadgetMessage.LabelOffset;
				else { offset = 0; size = 0; return false; }
				return true;
		}
		offset = 0;
		size = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiMiscSpecialistPacketKind packet,
		MuiMiscSpecialistField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset, out var size) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, size)) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiMiscSpecialistMethodMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiMiscSpecialistPacketKind packet,
		MuiMiscSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiMiscSpecialistPacketKind packet,
		MuiMiscSpecialistField field, uint value)
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
internal static class MuiMiscSpecialistFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMiscSpecialistFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscSpecialistMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Packet, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiMiscSpecialistPacketKind packet,
		MuiMiscSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiMiscSpecialistPacketKind packet,
		MuiMiscSpecialistField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, packet, field, value);
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
	internal const uint HandleInput = 0x80422A1Au;

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMiscSpecialistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadMethodIdValue(ref platform, message, out var methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	// Native qualification keeps selector admission scalar while the
	// dispatcher-facing overload above retains the named value-type record.
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (message.IsNull || !platform.IsMapped(message,
			MuiMiscSpecialistMethodMessage.Size)) return false;
		return MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Method,
			MuiMiscSpecialistField.MethodId, out methodId);
	}

	internal static bool TryReadLifecycle<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMiscLifecycleMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (!IsLifecycleMethod(method) || message.IsNull ||
			!platform.IsMapped(message, MuiMiscLifecycleMessage.Size) ||
			!TryReadMethodIdValue(ref platform, message, out methodId) ||
			methodId != method) return false;
		return MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Lifecycle,
			MuiMiscSpecialistField.MethodId, out packet.MethodId);
	}

	internal static bool WriteLifecycle<TPlatform>(ref TPlatform platform,
		APTR message, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsLifecycleMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiMiscLifecycleMessage.Size)) return false;
		return MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
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
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId)) return false;
		packet.MethodId = methodId;
		return MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Get,
			MuiMiscSpecialistField.Attribute, out packet.Attribute) &&
			MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiMiscSpecialistPacketKind.Get, MuiMiscSpecialistField.Storage,
				out packet.Storage);
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform,
		APTR message, uint attribute, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiMiscSpecialistGetMessage.Size)) return false;
		return MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Get,
			MuiMiscSpecialistField.MethodId, OmGet) &&
			MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
				message, MuiMiscSpecialistPacketKind.Get,
				MuiMiscSpecialistField.Attribute, attribute) &&
				MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
					message, MuiMiscSpecialistPacketKind.Get,
					MuiMiscSpecialistField.Storage, storage);
	}

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMiscSpecialistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (message.IsNull || !platform.IsMapped(message,
			MuiMiscSpecialistMethodMessage.Size) ||
			!TryReadMethodIdValue(ref platform, message, out methodId) ||
			methodId != method) return false;
		packet.MethodId = methodId;
		return true;
	}

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiMiscSpecialistMethodMessage.Size)) return false;
		return MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
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
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId)) return false;
		packet.MethodId = methodId;
		return MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Set,
			MuiMiscSpecialistField.Attribute, out packet.Attribute) &&
			MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiMiscSpecialistPacketKind.Set, MuiMiscSpecialistField.Value,
				out packet.Value);
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform, APTR message,
		uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSetMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiMiscSpecialistSetMessage.Size)) return false;
		return MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Set,
			MuiMiscSpecialistField.MethodId, method) &&
			MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
				message, MuiMiscSpecialistPacketKind.Set,
				MuiMiscSpecialistField.Attribute, attribute) &&
				MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform, message,
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
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId)) return false;
		packet.MethodId = methodId;
		return MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Pointer,
			MuiMiscSpecialistField.Pointer, out packet.Pointer);
	}

	internal static bool WritePointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint pointer)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiMiscSpecialistPointerMessage.Size)) return false;
		return MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Pointer,
			MuiMiscSpecialistField.MethodId, method) &&
			MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform, message,
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
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId)) return false;
		packet.MethodId = methodId;
		return MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Pair,
			MuiMiscSpecialistField.First, out packet.First) &&
			MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiMiscSpecialistPacketKind.Pair, MuiMiscSpecialistField.Second,
				out packet.Second);
	}

	internal static bool WritePair<TPlatform>(ref TPlatform platform, APTR message,
		uint method, uint first, uint second)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiMiscSpecialistPairMessage.Size)) return false;
		return MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.Pair,
			MuiMiscSpecialistField.MethodId, method) &&
			MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiMiscSpecialistPacketKind.Pair, MuiMiscSpecialistField.First, first) &&
			MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiMiscSpecialistPacketKind.Pair, MuiMiscSpecialistField.Second, second);
	}

	internal static bool TryReadHandleInput<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMiscHandleInputMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiMiscHandleInputMessage.Size,
			HandleInput)) return false;
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId)) return false;
		packet.MethodId = methodId;
		if (!MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.HandleInput,
			MuiMiscSpecialistField.IntuiMessage, out packet.IntuiMessage) ||
			!MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiMiscSpecialistPacketKind.HandleInput,
				MuiMiscSpecialistField.MuiKey, out var rawMuiKey)) return false;
		packet.MuiKey = unchecked((int)rawMuiKey);
		return true;
	}

	internal static bool WriteHandleInput<TPlatform>(ref TPlatform platform,
		APTR message, uint intuiMessage, int muiKey)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiMiscHandleInputMessage.Size)) return false;
		return MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiMiscSpecialistPacketKind.HandleInput,
			MuiMiscSpecialistField.MethodId, HandleInput) &&
			MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiMiscSpecialistPacketKind.HandleInput,
				MuiMiscSpecialistField.IntuiMessage, intuiMessage) &&
			MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiMiscSpecialistPacketKind.HandleInput,
				MuiMiscSpecialistField.MuiKey, unchecked((uint)muiKey));
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
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId)) return false;
		packet.MethodId = methodId;
		return MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiMiscSpecialistPacketKind.RegisterGadget,
			MuiMiscSpecialistField.Gadget, out packet.Gadget) &&
			MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiMiscSpecialistPacketKind.RegisterGadget,
				MuiMiscSpecialistField.Id, out packet.Id) &&
				MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform, message,
					MuiMiscSpecialistPacketKind.RegisterGadget,
					MuiMiscSpecialistField.Parameters, out packet.Parameters) &&
					MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform, message,
						MuiMiscSpecialistPacketKind.RegisterGadget,
						MuiMiscSpecialistField.Title, out packet.Title) &&
						MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform, message,
							MuiMiscSpecialistPacketKind.RegisterGadget,
							MuiMiscSpecialistField.Attribute, out packet.Attribute) &&
							MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform, message,
								MuiMiscSpecialistPacketKind.RegisterGadget,
								MuiMiscSpecialistField.Label, out packet.Label);
	}

	internal static bool WriteRegisterGadget<TPlatform>(ref TPlatform platform,
		APTR message, uint gadget, uint id, uint parameters, uint title,
		uint attribute, uint label) where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiMiscSpecialistRegisterGadgetMessage.Size)) return false;
		return MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiMiscSpecialistPacketKind.RegisterGadget,
			MuiMiscSpecialistField.MethodId,
			MuiMiscAttributes.Mccprefs_RegisterGadget) &&
			MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiMiscSpecialistPacketKind.RegisterGadget,
				MuiMiscSpecialistField.Gadget, gadget) &&
				MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform, message,
					MuiMiscSpecialistPacketKind.RegisterGadget,
					MuiMiscSpecialistField.Id, id) &&
					MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform, message,
						MuiMiscSpecialistPacketKind.RegisterGadget,
						MuiMiscSpecialistField.Parameters, parameters) &&
						MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform, message,
							MuiMiscSpecialistPacketKind.RegisterGadget,
							MuiMiscSpecialistField.Title, title) &&
							MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform, message,
								MuiMiscSpecialistPacketKind.RegisterGadget,
								MuiMiscSpecialistField.Attribute, attribute) &&
								MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform, message,
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
		uint methodId;
		if (message.IsNull || !platform.IsMapped(message, size) ||
			!TryReadMethodIdValue(ref platform, message, out methodId)) return false;
		return methodId == method;
	}
}
