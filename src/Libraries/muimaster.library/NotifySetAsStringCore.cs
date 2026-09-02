/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS Notify MUIM_SetAsString packet.  The fixed header is a named guest
// record; the documented variadic ULONG arguments follow it in guest memory
// and are consumed by the shared bounded formatter through one codec seam.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSetAsStringMessage
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint AttributeOffset = 4;
	internal const uint FormatOffset = 8;
	internal const uint ValueOffset = 12;
	internal uint MethodId;
	internal uint Attribute;
	internal APTR Format;
	internal uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSetAsStringMethodMessage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSetAsStringPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiSetAsStringMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Field, MuiSetAsStringMessage.Size, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiSetAsStringPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiSetAsStringMessageMemoryCodec.TryReadUInt32(ref platform,
			message, field, MuiSetAsStringMessage.Size, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiSetAsStringPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiSetAsStringMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, field, MuiSetAsStringMessage.Size, value);
}

// Struct-first guest-memory adapter for the fixed MUIM_SetAsString packet.
// The named packet owns the wire positions; the available-size argument keeps
// the method-only header admission distinct from the complete packet.
internal static class MuiSetAsStringMessageMemoryCodec
{
	private static bool TryResolve(MuiSetAsStringPacketField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiSetAsStringPacketField.MethodId => MuiSetAsStringMessage.MethodIdOffset,
			MuiSetAsStringPacketField.Attribute => MuiSetAsStringMessage.AttributeOffset,
			MuiSetAsStringPacketField.Format => MuiSetAsStringMessage.FormatOffset,
			MuiSetAsStringPacketField.Value => MuiSetAsStringMessage.ValueOffset,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiSetAsStringPacketField field, uint availableSize,
		out APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || message.IsNull ||
			availableSize < offset || availableSize - offset <
			MuiSetAsStringMessage.FieldSize ||
			message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, availableSize)) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiSetAsStringMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiSetAsStringPacketField field, uint availableSize,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, field, availableSize,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiSetAsStringPacketField field, uint availableSize,
		uint value) where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, field, availableSize,
			out var address)) return false;
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
		return MuiSetAsStringMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, MuiSetAsStringPacketField.Value,
			MuiSetAsStringMessage.Size, out value);
	}
}

internal static class MuiSetAsStringMessageCodec
{
	internal const uint Method = 0x80422590;
	internal const uint ParameterSize = 4;

	// Scalar-safe codec for the method-only SetAsString header. The complete
	// packet continues to use MuiSetAsStringMessage; this helper owns only the
	// bounded four-byte named header representation.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodHeaderValue<TPlatform>(
		ref TPlatform platform, APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiSetAsStringMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.TryReadValue(ref platform, valueAddress,
				out methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteMethodHeaderValue<TPlatform>(
		ref TPlatform platform, APTR address, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiSetAsStringMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	// Return the address of the trailing Value ULONG by walking the complete
	// named record.  This is the live payload boundary used by MUIM_SetAsString;
	// the compatibility field adapter below remains available for legacy callers.
	internal static bool TryGetValueAddress<TPlatform>(ref TPlatform platform,
		APTR message, out APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiSetAsStringMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiSetAsStringMessage.FieldSize, out _) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiSetAsStringMessage.FieldSize, out _) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiSetAsStringMessage.FieldSize, out _) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiSetAsStringMessage.FieldSize, out value))
		{
			value = APTR.Null;
			return false;
		}
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryGetParameters<TPlatform>(ref TPlatform platform,
		APTR message, out APTR parameters)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryGetValueAddress(ref platform, message, out parameters);
	}

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiSetAsStringMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	// Complete packets use the named record for selector admission. A method-only
	// header remains supported through the bounded scalar compatibility seam.
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (message.IsNotNull && platform.IsMapped(message,
			MuiSetAsStringMessage.Size) &&
			TryReadFixed(ref platform, message, out var complete))
		{
			methodId = complete.MethodId;
			return true;
		}
		if (message.IsNull || !platform.IsMapped(message,
			MuiSetAsStringMethodMessage.Size)) return false;
		return TryReadMethodHeaderValue(ref platform, message, out methodId);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR message,
		out MuiSetAsStringMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadFixed(ref platform, message, out packet) ||
			packet.MethodId != Method)
		{
			packet = default;
			return false;
		}
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform, APTR message,
		MuiSetAsStringMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (packet.MethodId != Method) return false;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiSetAsStringMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Format.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	private static bool TryReadFixed<TPlatform>(ref TPlatform platform,
		APTR message, out MuiSetAsStringMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiSetAsStringMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawFormat) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Value)) return false;
		packet.Format = APTR.FromPointer(rawFormat);
		return MuiGuestStructCursor.IsComplete(cursor);
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
