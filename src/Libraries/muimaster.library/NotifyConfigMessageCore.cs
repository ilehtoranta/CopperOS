/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGetConfigItemMessage
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal uint MethodId;
	internal uint ConfigId;
	internal APTR Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGetConfigItemMethodMessage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal uint MethodId;
}

internal enum MuiGetConfigItemPacketField : byte
{
	MethodId,
	ConfigId,
	Storage,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGetConfigItemPacketFieldCursor
{
	internal APTR Message;
	internal MuiGetConfigItemPacketField Field;
}

// Struct-first guest-memory adapter for the fixed MUIM_GetConfigItem records.
// The packet kind is inferred from the field, and the complete record span is
// validated before a member address is returned.
internal static class MuiGetConfigItemMessageMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiGetConfigItemPacketField field,
		out uint index, out uint recordSize)
	{
		switch (field)
		{
			case MuiGetConfigItemPacketField.MethodId:
				index = 0;
				recordSize = MuiGetConfigItemMethodMessage.Size;
				return true;
			case MuiGetConfigItemPacketField.ConfigId:
				index = 1;
				recordSize = MuiGetConfigItemMessage.Size;
				return true;
			case MuiGetConfigItemPacketField.Storage:
				index = 2;
				recordSize = MuiGetConfigItemMessage.Size;
				return true;
		}
		index = uint.MaxValue;
		recordSize = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiGetConfigItemPacketField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGetConfigItemPacketFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGetConfigItemPacketFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index,
			out var recordSize) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Message,
				recordSize, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiGetConfigItemMessage.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiGetConfigItemMessage.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGetConfigItemPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiGetConfigItemPacketField field, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGetConfigItemPacketFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiGetConfigItemPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (field == MuiGetConfigItemPacketField.MethodId)
			return MuiGetConfigItemMessageCodec.TryReadMethodIdValue(ref platform,
				message, out value);
		if (!MuiGetConfigItemMessageCodec.TryReadStructural(ref platform, message,
			out var packet)) return false;
		if (field == MuiGetConfigItemPacketField.ConfigId)
			value = packet.ConfigId;
		else if (field == MuiGetConfigItemPacketField.Storage)
			value = packet.Storage.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiGetConfigItemPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (field == MuiGetConfigItemPacketField.MethodId)
			return MuiGetConfigItemMessageCodec.WriteMethodHeaderValue(ref platform,
				message, value);
		if (!MuiGetConfigItemMessageCodec.TryReadStructural(ref platform, message,
			out var packet)) return false;
		if (field == MuiGetConfigItemPacketField.ConfigId)
			packet.ConfigId = value;
		else if (field == MuiGetConfigItemPacketField.Storage)
			packet.Storage = APTR.FromPointer(value);
		else return false;
		return MuiGetConfigItemMessageCodec.WriteStructural(ref platform, message,
			packet);
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// field cursor. The live message codec routes to the struct adapter above.
internal static class MuiGetConfigItemPacketFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGetConfigItemPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGetConfigItemMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Field, out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGetConfigItemPacketFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGetConfigItemMessageMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiGetConfigItemPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGetConfigItemMessageMemoryCodec.TryReadUInt32(ref platform,
			message, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiGetConfigItemPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGetConfigItemMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, field, value);
}

// Central codec for the fixed MorphOS MUIM_GetConfigItem envelope. The public
// core below exposes the named record while this adapter owns guest-boundary
// traversal and the distinct method-only header shape.
internal static class MuiGetConfigItemMessageCodec
{
	internal const uint Method = 0x80423EDB;

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodHeaderValue<TPlatform>(
		ref TPlatform platform, APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGetConfigItemMethodMessage.Size, out var cursor) ||
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
			MuiGetConfigItemMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiGetConfigItemMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	// Selector admission remains scalar for callers that only need MethodID, but
	// it is read from the named one-ULONG method record in declaration order.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadMethodHeaderValue(ref platform, message, out methodId);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiGetConfigItemMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadStructural(ref platform, message, out packet) ||
			packet.MethodId != Method) return false;
		return true;
	}

	// Complete named-record exchange used by typed field adapters. It does not
	// validate the selector, so a caller-owned packet can be modified before
	// semantic MUIM_GetConfigItem admission is applied.
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR message, out MuiGetConfigItemMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiGetConfigItemMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var configId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawStorage) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		packet.MethodId = rawMethodId;
		packet.ConfigId = configId;
		packet.Storage = APTR.FromPointer(rawStorage);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiGetConfigItemMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet.MethodId = Method;
		return WriteStructural(ref platform, message, packet);
	}

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR message, MuiGetConfigItemMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiGetConfigItemMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.ConfigId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Storage.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Struct-first codec for the MorphOS MUIM_GetConfigItem packet. This small
// boundary stays separate from the broad Notify implementation so packet-only
// freestanding qualification does not import unrelated Notify methods.
public static class MuiNotifyConfigMessageCore
{
	public const uint GetConfigItemMethod = MuiGetConfigItemMessageCodec.Method;

	public static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR storage, uint configId, APTR resultStorage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiGetConfigItemMessage);
		packet.MethodId = GetConfigItemMethod;
		packet.ConfigId = configId;
		packet.Storage = resultStorage;
		return MuiGetConfigItemMessageCodec.TryWrite(ref platform, storage,
			packet);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR message,
		out MuiGetConfigItemMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGetConfigItemMessageCodec.TryRead(ref platform, message,
			out packet);

	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory =>
		TryRead(ref platform, message, out var packet) ? packet.Storage.Raw : 0;
}
