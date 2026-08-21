/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS MUIP_GoActive and MUIP_GoInactive share the fixed
// { MethodID, flags } packet.  Keep the guest representation in this codec so
// Area activation logic consumes a named value record rather than offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaActivationMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaActivationMethodMessage
{
	internal const uint Size = 4;
	internal uint MethodId;
}

internal enum MuiAreaActivationPacketKind : byte
{
	Method,
	Activation,
}

internal enum MuiAreaActivationField : byte
{
	MethodId,
	Flags,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaActivationFieldCursor
{
	internal APTR Message;
	internal MuiAreaActivationPacketKind Packet;
	internal MuiAreaActivationField Field;
}

internal static class MuiAreaActivationFieldCursorCodec
{
	private static bool TryResolve(MuiAreaActivationPacketKind packet,
		MuiAreaActivationField field, out uint offset)
	{
		switch (packet)
		{
			case MuiAreaActivationPacketKind.Method:
				if (field == MuiAreaActivationField.MethodId) { offset = 0; return true; }
				break;
			case MuiAreaActivationPacketKind.Activation:
				if (field == MuiAreaActivationField.MethodId) { offset = 0; return true; }
				if (field == MuiAreaActivationField.Flags) { offset = 4; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaActivationFieldCursor cursor, out APTR address)
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
		APTR message, MuiAreaActivationPacketKind packet,
		MuiAreaActivationField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaActivationFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaActivationPacketKind packet,
		MuiAreaActivationField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaActivationFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaActivationMessageCodec
{
	internal const uint GoActive = 0x8042491Au;
	internal const uint GoInactive = 0x80422C0Cu;

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaActivationMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiAreaActivationMethodMessage.Size)) return false;
		return MuiAreaActivationFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiAreaActivationPacketKind.Method,
			MuiAreaActivationField.MethodId, out packet.MethodId);
	}

	internal static bool IsMethod(uint method) => method == GoActive ||
		method == GoInactive;

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR message,
		out MuiAreaActivationMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiAreaActivationMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			!IsMethod(header.MethodId)) return false;
		return MuiAreaActivationFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiAreaActivationPacketKind.Activation,
			MuiAreaActivationField.MethodId, out packet.MethodId) &&
			MuiAreaActivationFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiAreaActivationPacketKind.Activation,
				MuiAreaActivationField.Flags, out packet.Flags);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR message,
		uint method, uint flags) where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMethod(method) || message.IsNull ||
			!platform.IsMapped(message, MuiAreaActivationMessage.Size)) return false;
		return MuiAreaActivationFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiAreaActivationPacketKind.Activation,
			MuiAreaActivationField.MethodId, method) &&
			MuiAreaActivationFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaActivationPacketKind.Activation,
				MuiAreaActivationField.Flags, flags);
	}
}
