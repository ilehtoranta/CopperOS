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

// Struct-first guest-memory adapter for the fixed Popstring/Popobject/Popasl
// packets. Packet kinds own complete MorphOS record spans; field names select
// members without exposing numeric positions to dispatch code.
internal static class MuiPopSpecialistMessageMemoryCodec
{
	private static bool TryResolve(MuiPopSpecialistPacketKind packet,
		MuiPopSpecialistField field, out uint offset, out uint size)
	{
		switch (packet)
		{
			case MuiPopSpecialistPacketKind.Method:
				size = MuiPopSpecialistMethodMessage.Size;
				if (field == MuiPopSpecialistField.MethodId)
					offset = MuiPopSpecialistMethodMessage.MethodIdOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiPopSpecialistPacketKind.Get:
				size = MuiPopSpecialistGetMessage.Size;
				if (field == MuiPopSpecialistField.MethodId)
					offset = MuiPopSpecialistGetMessage.MethodIdOffset;
				else if (field == MuiPopSpecialistField.Attribute)
					offset = MuiPopSpecialistGetMessage.AttributeOffset;
				else if (field == MuiPopSpecialistField.Storage)
					offset = MuiPopSpecialistGetMessage.StorageOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiPopSpecialistPacketKind.Set:
				size = MuiPopSpecialistSetMessage.Size;
				if (field == MuiPopSpecialistField.MethodId)
					offset = MuiPopSpecialistSetMessage.MethodIdOffset;
				else if (field == MuiPopSpecialistField.Attribute)
					offset = MuiPopSpecialistSetMessage.AttributeOffset;
				else if (field == MuiPopSpecialistField.Value)
					offset = MuiPopSpecialistSetMessage.ValueOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiPopSpecialistPacketKind.Close:
				size = MuiPopSpecialistCloseMessage.Size;
				if (field == MuiPopSpecialistField.MethodId)
					offset = MuiPopSpecialistCloseMessage.MethodIdOffset;
				else if (field == MuiPopSpecialistField.Result)
					offset = MuiPopSpecialistCloseMessage.ResultOffset;
				else { offset = 0; size = 0; return false; }
				return true;
		}
		offset = 0;
		size = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiPopSpecialistPacketKind packet,
		MuiPopSpecialistField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset, out var size) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, size)) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiPopSpecialistMethodMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiPopSpecialistPacketKind packet,
		MuiPopSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiPopSpecialistPacketKind packet,
		MuiPopSpecialistField field, uint value)
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
internal static class MuiPopSpecialistFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiPopSpecialistFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiPopSpecialistMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Packet, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiPopSpecialistPacketKind packet,
		MuiPopSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiPopSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiPopSpecialistPacketKind packet,
		MuiPopSpecialistField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiPopSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, packet, field, value);
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
		if (!TryReadMethodIdValue(ref platform, message, out var methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	// Native selector admission stays scalar so compiler paths do not need to
	// materialize a temporary one-field record. Public packet consumers still
	// receive the named struct above.
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (message.IsNull || !platform.IsMapped(message,
			MuiPopSpecialistMethodMessage.Size)) return false;
		return MuiPopSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiPopSpecialistPacketKind.Method,
			MuiPopSpecialistField.MethodId, out methodId);
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
			TryReadMethodIdValue(ref platform, message, out var methodId) &&
			methodId == method;
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
		if (!TryReadMethodIdValue(ref platform, message, out var methodId)) return false;
		packet.MethodId = methodId;
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
		if (!TryReadMethodIdValue(ref platform, message, out var methodId)) return false;
		packet.MethodId = methodId;
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
		if (!TryReadMethodIdValue(ref platform, message, out var methodId)) return false;
		packet.MethodId = methodId;
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
			!TryReadMethodIdValue(ref platform, message, out var methodId)) return false;
		return methodId == method;
	}
}
