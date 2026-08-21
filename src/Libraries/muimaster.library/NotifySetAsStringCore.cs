/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS Notify MUIM_SetAsString packet.  The fixed header is a named guest
// record; the documented variadic ULONG arguments follow it in guest memory
// and are consumed by the shared bounded formatter through one codec seam.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSetAsStringMessage
{
	internal const uint Size = 16;
	internal const int ValueOffset = 12;
	internal uint MethodId;
	internal uint Attribute;
	internal APTR Format;
	internal uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSetAsStringMethodMessage
{
	internal const uint Size = 4;
	internal uint MethodId;
}

internal enum MuiSetAsStringPacketField : byte
{
	MethodId,
	Attribute,
	Format,
	Value,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSetAsStringPacketFieldCursor
{
	internal APTR Message;
	internal MuiSetAsStringPacketField Field;
}

internal static class MuiSetAsStringPacketFieldCursorCodec
{
	private static bool TryResolve(MuiSetAsStringPacketField field,
		out uint offset)
	{
		if (field == MuiSetAsStringPacketField.MethodId) { offset = 0; return true; }
		if (field == MuiSetAsStringPacketField.Attribute) { offset = 4; return true; }
		if (field == MuiSetAsStringPacketField.Format) { offset = 8; return true; }
		if (field == MuiSetAsStringPacketField.Value) { offset = 12; return true; }
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSetAsStringPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Message.IsNull ||
			cursor.Message.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiSetAsStringPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiSetAsStringPacketFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiSetAsStringPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiSetAsStringPacketFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Named view of the fixed Value ULONG in a MUIM_SetAsString packet. Keeping
// this address boundary separate from the message codec lets formatter code
// consume caller-owned storage without repeating the packed offset.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSetAsStringValueCursor
{
	internal APTR Message;
}

internal static class MuiSetAsStringValueCursorCodec
{
	internal const uint EntrySize = 4;

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSetAsStringValueCursor cursor, out APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = APTR.Null;
		var packetCursor = default(MuiSetAsStringPacketFieldCursor);
		packetCursor.Message = cursor.Message;
		packetCursor.Field = MuiSetAsStringPacketField.Value;
		return MuiSetAsStringPacketFieldCursorCodec.TryGetAddress(ref platform,
			packetCursor, out value);
	}
}

internal static class MuiSetAsStringMessageCodec
{
	internal const uint Method = 0x80422590;
	internal const uint ParameterSize = 4;

	internal static bool TryGetParameters<TPlatform>(ref TPlatform platform,
		APTR message, out APTR parameters)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiSetAsStringValueCursor);
		cursor.Message = message;
		return MuiSetAsStringValueCursorCodec.TryGetAddress(ref platform,
			cursor, out parameters);
	}

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiSetAsStringMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiSetAsStringMethodMessage.Size)) return false;
		return MuiSetAsStringPacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiSetAsStringPacketField.MethodId, out packet.MethodId);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR message,
		out MuiSetAsStringMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiSetAsStringMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != Method) return false;
		if (!MuiSetAsStringPacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiSetAsStringPacketField.Attribute, out packet.Attribute) ||
			!MuiSetAsStringPacketFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiSetAsStringPacketField.Format, out var rawFormat) ||
			!MuiSetAsStringPacketFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiSetAsStringPacketField.Value, out packet.Value)) return false;
		packet.MethodId = header.MethodId;
		packet.Format = APTR.FromPointer(rawFormat);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform, APTR message,
		MuiSetAsStringMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiSetAsStringMessage.Size)) return false;
		return MuiSetAsStringPacketFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiSetAsStringPacketField.MethodId, Method) &&
			MuiSetAsStringPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiSetAsStringPacketField.Attribute, packet.Attribute) &&
			MuiSetAsStringPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiSetAsStringPacketField.Format, packet.Format.Raw) &&
			MuiSetAsStringPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiSetAsStringPacketField.Value, packet.Value);
	}
}

public static class MuiNotifySetAsStringCore
{
	public const uint Method = MuiSetAsStringMessageCodec.Method;
	public const uint MaximumOutputLength = 1024;
	public const uint MaximumArguments = 8;

	// Private object-store key namespace for the owned text copy.  The key is
	// one-to-one with the target attribute and is never exposed as Dataspace
	// API state; object disposal releases it through the normal store cleanup.
	private const uint StorageKeyBase = 0x7F150000;

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR message,
		out MuiSetAsStringMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiSetAsStringMessageCodec.TryRead(ref platform, message, out packet);

	public static bool WriteRecord<TPlatform>(ref TPlatform platform, APTR message,
		uint attribute, APTR format, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiSetAsStringMessage);
		packet.MethodId = Method;
		packet.Attribute = attribute;
		packet.Format = format;
		packet.Value = value;
		return MuiSetAsStringMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	// Struct-only native qualification seam.  It proves the fixed packet and
	// rejects a truncated header without entering the managed object/store path.
	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
	{
		return TryRead(ref platform, message, out var packet) ? packet.Attribute : 0;
	}

	public static bool Apply<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryRead(ref platform, message, out var packet) ||
			!MuiSetAsStringMessageCodec.TryGetParameters(ref platform, message,
				out var parameters))
			return false;
		return SetAsString(ref platform, state, obj, packet.Attribute,
			packet.Format, parameters);
	}

	public static bool SetAsString<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, APTR format, APTR parameters)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var key = StorageKey(attribute);
		if (format.IsNull)
		{
			MuiStoreCore.DataspaceRemove(ref platform, state, obj, key);
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, 0, true);
		}

		if (!MuiRequesterPayloadCore.TryGetFormatParameterCount(ref platform,
			format, out var argumentCount) || argumentCount > MaximumArguments)
			return false;
		if (!MuiRequesterFormatCore.TryMaterialize(ref platform, format,
			parameters, out var prepared, out var allocationSize)) return false;

		uint length;
		if (!CStringCodec.TryReadLength(ref platform, prepared,
			MaximumOutputLength + 1, out length) || length > MaximumOutputLength)
		{
			if (allocationSize != 0) platform.Free(prepared, allocationSize);
			return false;
		}
		var byteSize = length + 1;
		var stored = MuiStoreCore.DataspaceAdd(ref platform, state, obj, key,
			prepared, unchecked((int)byteSize));
		if (allocationSize != 0) platform.Free(prepared, allocationSize);
		if (!stored) return false;

		var owned = MuiStoreCore.DataspaceFind(ref platform, state, obj, key);
		if (owned.IsNull || !MuiHeadlessObjectCore.SetAttribute(ref platform,
			state, obj, attribute, owned.Raw, true))
		{
			MuiStoreCore.DataspaceRemove(ref platform, state, obj, key);
			return false;
		}
		return true;
	}

	private static uint StorageKey(uint attribute) => StorageKeyBase ^ attribute;
}
