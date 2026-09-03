/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS MUIP_GoActive and MUIP_GoInactive share the fixed
// { MethodID, flags } packet.  Keep the guest representation in this codec so
// Area activation logic consumes a named value record rather than offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaActivationMessage
{
	public const uint Size = 8;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint FlagsOffset = 4;
	public uint MethodId;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaActivationMethodMessage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal uint MethodId;
}

internal static class MuiAreaActivationMethodMessageCodec
{
	// The method-only packet is a complete one-ULONG named record. Scalar-safe
	// entry points keep the freestanding call boundary independent of the
	// current one-field struct ABI lowering while retaining the typed record.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out methodId);

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteValue<TPlatform>(ref TPlatform platform,
		APTR address, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestUlongStorageCodec.WriteValue(ref platform, address, methodId);

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadValue(ref platform, address, out methodId);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaActivationMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return TryReadStructural(ref platform, address, out value);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaActivationMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteStructural(ref platform, address, value);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaActivationMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out var methodId)) return false;
		value.MethodId = methodId;
		return true;
	}

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaActivationMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGuestUlongStorageCodec.WriteValue(ref platform, address,
			value.MethodId);
	}
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

// Compatibility adapter retained for callers that still construct the typed
// field cursor. Live activation packet consumers use the named record adapter.
internal static class MuiAreaActivationFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaActivationFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaActivationRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Packet, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaActivationPacketKind packet,
		MuiAreaActivationField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaActivationRecordMemoryCodec.TryReadUInt32(ref platform,
			message, packet, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaActivationPacketKind packet,
		MuiAreaActivationField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaActivationRecordMemoryCodec.TryWriteUInt32(ref platform,
			message, packet, field, value);
	}
}

// Struct-first guest-memory adapter for the fixed MUIP_GoActive/
// MUIP_GoInactive records and their method-only header. The packet kind owns
// the complete record boundary; callers never need to supply a numeric size.
internal static class MuiAreaActivationRecordMemoryCodec
{
	private static bool TryResolve(MuiAreaActivationPacketKind packet,
		MuiAreaActivationField field, out uint offset, out uint size,
		out uint fieldSize)
	{
		fieldSize = MuiAreaActivationMessage.FieldSize;
		if (packet == MuiAreaActivationPacketKind.Method)
		{
			if (field == MuiAreaActivationField.MethodId)
			{
				offset = MuiAreaActivationMethodMessage.MethodIdOffset;
				size = MuiAreaActivationMethodMessage.Size;
				return true;
			}
		}
		else if (packet == MuiAreaActivationPacketKind.Activation)
		{
			if (field == MuiAreaActivationField.MethodId)
				offset = MuiAreaActivationMessage.MethodIdOffset;
			else if (field == MuiAreaActivationField.Flags)
				offset = MuiAreaActivationMessage.FlagsOffset;
			else
			{
				offset = 0;
				size = 0;
				return false;
			}
			size = MuiAreaActivationMessage.Size;
			return true;
		}
		offset = 0;
		size = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaActivationPacketKind packet,
		MuiAreaActivationField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset, out var size,
			out var fieldSize) || message.IsNull ||
			message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, size)) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaActivationPacketKind packet,
		MuiAreaActivationField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (packet == MuiAreaActivationPacketKind.Method)
		{
			// Keep the one-ULONG method header on its scalar-safe seam. The
			// public shape is still the named method record, but current
			// freestanding lowering cannot reliably pass a one-field struct.
			return field == MuiAreaActivationField.MethodId &&
				MuiAreaActivationMethodMessageCodec.TryReadValue(ref platform,
					message, out value);
		}
		if (packet == MuiAreaActivationPacketKind.Activation)
		{
			if (!MuiAreaActivationMessageCodec.TryReadStructural(ref platform,
				message, out var activation)) return false;
			if (field == MuiAreaActivationField.MethodId)
				value = activation.MethodId;
			else if (field == MuiAreaActivationField.Flags)
				value = activation.Flags;
			else
				return false;
			return true;
		}
		return false;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaActivationPacketKind packet,
		MuiAreaActivationField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (packet == MuiAreaActivationPacketKind.Method)
		{
			return field == MuiAreaActivationField.MethodId &&
				MuiAreaActivationMethodMessageCodec.WriteValue(ref platform,
					message, value);
		}
		if (packet == MuiAreaActivationPacketKind.Activation)
		{
			if (!MuiAreaActivationMessageCodec.TryReadStructural(ref platform,
				message, out var activation)) return false;
			if (field == MuiAreaActivationField.MethodId)
				activation.MethodId = value;
			else if (field == MuiAreaActivationField.Flags)
				activation.Flags = value;
			else
				return false;
			return MuiAreaActivationMessageCodec.WriteStructural(ref platform,
				message, activation);
		}
		return false;
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
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	// Selector admission is decoded through the complete named method-header
	// record; the field adapter remains available for explicit compatibility
	// callers only.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		return MuiAreaActivationMethodMessageCodec.TryReadMethodId(ref platform,
			message, out methodId);
	}

	internal static bool IsMethod(uint method) => method == GoActive ||
		method == GoInactive;

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR message,
		out MuiAreaActivationMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadStructural(ref platform, message, out packet) ||
			!IsMethod(packet.MethodId))
		{
			packet = default;
			return false;
		}
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR message,
		MuiAreaActivationMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return IsMethod(packet.MethodId) &&
			WriteStructural(ref platform, message, packet);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaActivationMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiAreaActivationMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Flags)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaActivationMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiAreaActivationMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Flags)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR message,
		uint method, uint flags) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaActivationMessage);
		packet.MethodId = method;
		packet.Flags = flags;
		return Write(ref platform, message, packet);
	}
}
