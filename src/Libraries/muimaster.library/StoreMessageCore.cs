/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStoreMethodMessage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStoreClearMessage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStoreKeyMessage
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint KeyOffset = 4;
	internal uint MethodId;
	internal APTR Key;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStoreCounterMessage
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint CounterOffset = 4;
	internal uint MethodId;
	internal APTR Counter;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDatamapSetMessage
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint DataOffset = 4;
	internal const uint LengthOffset = 8;
	internal const uint KeyOffset = 12;
	internal uint MethodId;
	internal APTR Data;
	internal int Length;
	internal APTR Key;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDatamapGetMessage
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint KeyOffset = 4;
	internal const uint SizeStorageOffset = 8;
	internal uint MethodId;
	internal APTR Key;
	internal APTR SizeStorage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiObjectmapSetMessage
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint ObjectOffset = 4;
	internal const uint KeyOffset = 8;
	internal uint MethodId;
	internal APTR Object;
	internal APTR Key;
}

internal enum MuiStorePacketKind : byte
{
	Method,
	Clear,
	Key,
	Counter,
	DatamapSet,
	DatamapGet,
	ObjectmapSet,
}

internal enum MuiStoreField : byte
{
	MethodId,
	Data,
	Length,
	Key,
	SizeStorage,
	Object,
	Counter,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStoreFieldCursor
{
	internal APTR Message;
	internal MuiStorePacketKind Packet;
	internal MuiStoreField Field;
}

// Struct-first guest-memory adapter for the store method packet family.
internal static class MuiStoreMessageMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiStorePacketKind packet,
		MuiStoreField field, out uint index, out uint recordSize)
	{
		index = uint.MaxValue;
		recordSize = 0;
		switch (packet)
		{
			case MuiStorePacketKind.Method:
				if (field == MuiStoreField.MethodId)
				{
					index = 0; recordSize = MuiStoreMethodMessage.Size;
					return true;
				}
				return false;
			case MuiStorePacketKind.Clear:
				if (field == MuiStoreField.MethodId)
				{
					index = 0; recordSize = MuiStoreClearMessage.Size;
					return true;
				}
				return false;
			case MuiStorePacketKind.Key:
				if (field == MuiStoreField.MethodId)
				{
					index = 0; recordSize = MuiStoreKeyMessage.Size;
					return true;
				}
				if (field == MuiStoreField.Key)
				{
					index = 1; recordSize = MuiStoreKeyMessage.Size;
					return true;
				}
				return false;
			case MuiStorePacketKind.Counter:
				if (field == MuiStoreField.MethodId)
				{
					index = 0; recordSize = MuiStoreCounterMessage.Size;
					return true;
				}
				if (field == MuiStoreField.Counter)
				{
					index = 1; recordSize = MuiStoreCounterMessage.Size;
					return true;
				}
				return false;
			case MuiStorePacketKind.DatamapSet:
				if (field == MuiStoreField.MethodId)
				{
					index = 0; recordSize = MuiDatamapSetMessage.Size;
					return true;
				}
				if (field == MuiStoreField.Data)
				{
					index = 1; recordSize = MuiDatamapSetMessage.Size;
					return true;
				}
				if (field == MuiStoreField.Length)
				{
					index = 2; recordSize = MuiDatamapSetMessage.Size;
					return true;
				}
				if (field == MuiStoreField.Key)
				{
					index = 3; recordSize = MuiDatamapSetMessage.Size;
					return true;
				}
				return false;
			case MuiStorePacketKind.DatamapGet:
				if (field == MuiStoreField.MethodId)
				{
					index = 0; recordSize = MuiDatamapGetMessage.Size;
					return true;
				}
				if (field == MuiStoreField.Key)
				{
					index = 1; recordSize = MuiDatamapGetMessage.Size;
					return true;
				}
				if (field == MuiStoreField.SizeStorage)
				{
					index = 2; recordSize = MuiDatamapGetMessage.Size;
					return true;
				}
				return false;
			case MuiStorePacketKind.ObjectmapSet:
				if (field == MuiStoreField.MethodId)
				{
					index = 0; recordSize = MuiObjectmapSetMessage.Size;
					return true;
				}
				if (field == MuiStoreField.Object)
				{
					index = 1; recordSize = MuiObjectmapSetMessage.Size;
					return true;
				}
				if (field == MuiStoreField.Key)
				{
					index = 2; recordSize = MuiObjectmapSetMessage.Size;
					return true;
				}
				return false;
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiStorePacketKind packet, MuiStoreField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryGetAddress(ref platform, message, packet, field,
			out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiStorePacketKind packet, MuiStoreField field,
		out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(packet, field, out var index,
			out var recordSize) ||
			!MuiGuestStructCursor.TryCreate(ref platform, message, recordSize,
				out var cursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
				out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = 4;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiStorePacketKind packet, MuiStoreField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryResolveFieldIndex(packet, field, out _, out var size) ||
			message.IsNull || !platform.IsMapped(message, size)) return false;
		switch (packet)
		{
			case MuiStorePacketKind.Method:
				if (field != MuiStoreField.MethodId ||
					!MuiStorePacketSequentialStructCodec.TryReadMethodIdValue(
						ref platform, message, out value)) return false;
				return true;
			case MuiStorePacketKind.Clear:
				if (field != MuiStoreField.MethodId ||
					!MuiStorePacketSequentialStructCodec.TryReadClearMethodIdValue(
						ref platform, message, out value)) return false;
				return true;
			case MuiStorePacketKind.Key:
				if (!MuiStorePacketSequentialStructCodec.TryReadKey(ref platform,
					message, out var key)) return false;
				if (field == MuiStoreField.MethodId) value = key.MethodId;
				else if (field == MuiStoreField.Key) value = key.Key.Raw;
				else return false;
				return true;
			case MuiStorePacketKind.Counter:
				if (!MuiStorePacketSequentialStructCodec.TryReadCounter(ref platform,
					message, out var counter)) return false;
				if (field == MuiStoreField.MethodId) value = counter.MethodId;
				else if (field == MuiStoreField.Counter) value = counter.Counter.Raw;
				else return false;
				return true;
			case MuiStorePacketKind.DatamapSet:
				if (!MuiStorePacketSequentialStructCodec.TryReadDatamapSet(
					ref platform, message, out var datamapSet)) return false;
				if (field == MuiStoreField.MethodId) value = datamapSet.MethodId;
				else if (field == MuiStoreField.Data) value = datamapSet.Data.Raw;
				else if (field == MuiStoreField.Length)
					value = unchecked((uint)datamapSet.Length);
				else if (field == MuiStoreField.Key) value = datamapSet.Key.Raw;
				else return false;
				return true;
			case MuiStorePacketKind.DatamapGet:
				if (!MuiStorePacketSequentialStructCodec.TryReadDatamapGet(
					ref platform, message, out var datamapGet)) return false;
				if (field == MuiStoreField.MethodId) value = datamapGet.MethodId;
				else if (field == MuiStoreField.Key) value = datamapGet.Key.Raw;
				else if (field == MuiStoreField.SizeStorage)
					value = datamapGet.SizeStorage.Raw;
				else return false;
				return true;
			case MuiStorePacketKind.ObjectmapSet:
				if (!MuiStorePacketSequentialStructCodec.TryReadObjectmapSet(
					ref platform, message, out var objectmapSet)) return false;
				if (field == MuiStoreField.MethodId) value = objectmapSet.MethodId;
				else if (field == MuiStoreField.Object) value = objectmapSet.Object.Raw;
				else if (field == MuiStoreField.Key) value = objectmapSet.Key.Raw;
				else return false;
				return true;
		}
		return false;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiStorePacketKind packet, MuiStoreField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryResolveFieldIndex(packet, field, out _, out var size) ||
			message.IsNull || !platform.IsMapped(message, size)) return false;
		switch (packet)
		{
			case MuiStorePacketKind.Method:
				if (field != MuiStoreField.MethodId ||
					!MuiStorePacketSequentialStructCodec.TryWriteMethodIdValue(
						ref platform, message, value)) return false;
				return true;
			case MuiStorePacketKind.Clear:
				if (field != MuiStoreField.MethodId ||
					!MuiStorePacketSequentialStructCodec.TryWriteClearMethodIdValue(
						ref platform, message, value)) return false;
				return true;
			case MuiStorePacketKind.Key:
				if (!MuiStorePacketSequentialStructCodec.TryReadKey(ref platform,
					message, out var key)) return false;
				if (field == MuiStoreField.MethodId) key.MethodId = value;
				else if (field == MuiStoreField.Key) key.Key = APTR.FromPointer(value);
				else return false;
				return MuiStorePacketSequentialStructCodec.WriteKey(ref platform,
					message, key);
			case MuiStorePacketKind.Counter:
				if (!MuiStorePacketSequentialStructCodec.TryReadCounter(ref platform,
					message, out var counter)) return false;
				if (field == MuiStoreField.MethodId) counter.MethodId = value;
				else if (field == MuiStoreField.Counter)
					counter.Counter = APTR.FromPointer(value);
				else return false;
				return MuiStorePacketSequentialStructCodec.WriteCounter(ref platform,
					message, counter);
			case MuiStorePacketKind.DatamapSet:
				if (!MuiStorePacketSequentialStructCodec.TryReadDatamapSet(
					ref platform, message, out var datamapSet)) return false;
				if (field == MuiStoreField.MethodId) datamapSet.MethodId = value;
				else if (field == MuiStoreField.Data)
					datamapSet.Data = APTR.FromPointer(value);
				else if (field == MuiStoreField.Length)
					datamapSet.Length = unchecked((int)value);
				else if (field == MuiStoreField.Key)
					datamapSet.Key = APTR.FromPointer(value);
				else return false;
				return MuiStorePacketSequentialStructCodec.WriteDatamapSet(
					ref platform, message, datamapSet);
			case MuiStorePacketKind.DatamapGet:
				if (!MuiStorePacketSequentialStructCodec.TryReadDatamapGet(
					ref platform, message, out var datamapGet)) return false;
				if (field == MuiStoreField.MethodId) datamapGet.MethodId = value;
				else if (field == MuiStoreField.Key)
					datamapGet.Key = APTR.FromPointer(value);
				else if (field == MuiStoreField.SizeStorage)
					datamapGet.SizeStorage = APTR.FromPointer(value);
				else return false;
				return MuiStorePacketSequentialStructCodec.WriteDatamapGet(
					ref platform, message, datamapGet);
			case MuiStorePacketKind.ObjectmapSet:
				if (!MuiStorePacketSequentialStructCodec.TryReadObjectmapSet(
					ref platform, message, out var objectmapSet)) return false;
				if (field == MuiStoreField.MethodId) objectmapSet.MethodId = value;
				else if (field == MuiStoreField.Object)
					objectmapSet.Object = APTR.FromPointer(value);
				else if (field == MuiStoreField.Key)
					objectmapSet.Key = APTR.FromPointer(value);
				else return false;
				return MuiStorePacketSequentialStructCodec.WriteObjectmapSet(
					ref platform, message, objectmapSet);
		}
		return false;
	}
}

// Compatibility wrapper retained for typed cursor callers; production access
// uses MuiStoreMessageMemoryCodec with named packet records.
internal static class MuiStoreFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStoreFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiStoreMessageMemoryCodec.TryGetAddress(ref platform, cursor.Message,
			cursor.Packet, cursor.Field, out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStoreFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiStoreMessageMemoryCodec.TryGetAddress(ref platform, cursor.Message,
			cursor.Packet, cursor.Field, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiStorePacketKind packet, MuiStoreField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory =>
		MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, message, packet,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiStorePacketKind packet, MuiStoreField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, message, packet,
			field, value);
}

internal static class MuiStoreMessageCodec
{
	// Scalar selector admission remains at the guest ABI boundary; callers
	// continue to receive the named store method-header struct.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
	where TPlatform : struct, IMuiGuestMemory
		=> MuiStorePacketSequentialStructCodec.TryReadMethodIdValue(ref platform,
			message, out methodId);

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiStoreMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiStorePacketSequentialStructCodec.TryReadMethodIdValue(
			ref platform, message, out var methodId)) return false;
		packet.MethodId = methodId;
		return true;
	}
}

// Sequential codecs for the complete MorphOS Datamap/Objectmap packet family.
// Every packet is consumed as one declaration-ordered named struct; the
// shared ULONG record codec retains method IDs and pointers with bit 31 set.
internal static class MuiStorePacketSequentialStructCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool TryReadUlong<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiGuestUlongStorage.Size, out var address)) return false;
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool TryWriteUlong<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiGuestUlongStorage.Size, out var address)) return false;
		return MuiGuestUlongStorageCodec.WriteValue(ref platform, address, value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiStoreMethodMessage.Size, out var cursor) ||
			!TryReadUlong(ref platform, ref cursor, out methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryWriteMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiStoreMethodMessage.Size, out var cursor) ||
			!TryWriteUlong(ref platform, ref cursor, methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, out MuiStoreMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadMethodIdValue(ref platform, message, out var methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, MuiStoreMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiStoreMethodMessage.Size, out var cursor) ||
			!TryWriteUlong(ref platform, ref cursor, packet.MethodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadClear<TPlatform>(ref TPlatform platform,
		APTR message, out MuiStoreClearMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiStoreClearMessage.Size, out var cursor) ||
			!TryReadUlong(ref platform, ref cursor, out var methodId) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		packet.MethodId = methodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadClearMethodIdValue<TPlatform>(
		ref TPlatform platform, APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiStoreClearMessage.Size, out var cursor) ||
			!TryReadUlong(ref platform, ref cursor, out methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteClear<TPlatform>(ref TPlatform platform,
		APTR message, MuiStoreClearMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> TryWriteClearMethodIdValue(ref platform, message, packet.MethodId);

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryWriteClearMethodIdValue<TPlatform>(
		ref TPlatform platform, APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiStoreClearMessage.Size, out var cursor) ||
			!TryWriteUlong(ref platform, ref cursor, methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadKey<TPlatform>(ref TPlatform platform,
		APTR message, out MuiStoreKeyMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiStoreKeyMessage.Size, out var cursor) ||
			!TryReadUlong(ref platform, ref cursor, out var methodId) ||
			!TryReadUlong(ref platform, ref cursor, out var key) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		packet.MethodId = methodId;
		packet.Key = APTR.FromPointer(key);
		return true;
	}

	internal static bool WriteKey<TPlatform>(ref TPlatform platform,
		APTR message, MuiStoreKeyMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiStoreKeyMessage.Size, out var cursor) ||
			!TryWriteUlong(ref platform, ref cursor, packet.MethodId) ||
			!TryWriteUlong(ref platform, ref cursor, packet.Key.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadCounter<TPlatform>(ref TPlatform platform,
		APTR message, out MuiStoreCounterMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiStoreCounterMessage.Size, out var cursor) ||
			!TryReadUlong(ref platform, ref cursor, out var methodId) ||
			!TryReadUlong(ref platform, ref cursor, out var counter) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		packet.MethodId = methodId;
		packet.Counter = APTR.FromPointer(counter);
		return true;
	}

	internal static bool WriteCounter<TPlatform>(ref TPlatform platform,
		APTR message, MuiStoreCounterMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiStoreCounterMessage.Size, out var cursor) ||
			!TryWriteUlong(ref platform, ref cursor, packet.MethodId) ||
			!TryWriteUlong(ref platform, ref cursor, packet.Counter.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadDatamapSet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDatamapSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDatamapSetMessage.Size, out var cursor) ||
			!TryReadUlong(ref platform, ref cursor, out var methodId) ||
			!TryReadUlong(ref platform, ref cursor, out var data) ||
			!TryReadUlong(ref platform, ref cursor, out var length) ||
			!TryReadUlong(ref platform, ref cursor, out var key) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		packet.MethodId = methodId;
		packet.Data = APTR.FromPointer(data);
		packet.Length = unchecked((int)length);
		packet.Key = APTR.FromPointer(key);
		return true;
	}

	internal static bool WriteDatamapSet<TPlatform>(ref TPlatform platform,
		APTR message, MuiDatamapSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDatamapSetMessage.Size, out var cursor) ||
			!TryWriteUlong(ref platform, ref cursor, packet.MethodId) ||
			!TryWriteUlong(ref platform, ref cursor, packet.Data.Raw) ||
			!TryWriteUlong(ref platform, ref cursor,
				unchecked((uint)packet.Length)) ||
			!TryWriteUlong(ref platform, ref cursor, packet.Key.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadDatamapGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDatamapGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDatamapGetMessage.Size, out var cursor) ||
			!TryReadUlong(ref platform, ref cursor, out var methodId) ||
			!TryReadUlong(ref platform, ref cursor, out var key) ||
			!TryReadUlong(ref platform, ref cursor, out var storage) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		packet.MethodId = methodId;
		packet.Key = APTR.FromPointer(key);
		packet.SizeStorage = APTR.FromPointer(storage);
		return true;
	}

	internal static bool WriteDatamapGet<TPlatform>(ref TPlatform platform,
		APTR message, MuiDatamapGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDatamapGetMessage.Size, out var cursor) ||
			!TryWriteUlong(ref platform, ref cursor, packet.MethodId) ||
			!TryWriteUlong(ref platform, ref cursor, packet.Key.Raw) ||
			!TryWriteUlong(ref platform, ref cursor, packet.SizeStorage.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadObjectmapSet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiObjectmapSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiObjectmapSetMessage.Size, out var cursor) ||
			!TryReadUlong(ref platform, ref cursor, out var methodId) ||
			!TryReadUlong(ref platform, ref cursor, out var obj) ||
			!TryReadUlong(ref platform, ref cursor, out var key) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		packet.MethodId = methodId;
		packet.Object = APTR.FromPointer(obj);
		packet.Key = APTR.FromPointer(key);
		return true;
	}

	internal static bool WriteObjectmapSet<TPlatform>(ref TPlatform platform,
		APTR message, MuiObjectmapSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiObjectmapSetMessage.Size, out var cursor) ||
			!TryWriteUlong(ref platform, ref cursor, packet.MethodId) ||
			!TryWriteUlong(ref platform, ref cursor, packet.Object.Raw) ||
			!TryWriteUlong(ref platform, ref cursor, packet.Key.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Compatibility packet adapter retained for legacy offset-focused diagnostics.
// Production callers use MuiStorePacketCodec below, which delegates to the
// sequential named-struct implementation above.
internal static class MuiStorePacketOffsetCodec
{
	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, out MuiStoreMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiStoreMethodMessage.Size) ||
			!MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.Method, MuiStoreField.MethodId,
				out packet.MethodId)) return false;
		return true;
	}

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, MuiStoreMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiStoreMethodMessage.Size)) return false;
		return MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiStorePacketKind.Method, MuiStoreField.MethodId, packet.MethodId);
	}

	internal static bool TryReadClear<TPlatform>(ref TPlatform platform,
		APTR message, out MuiStoreClearMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiStoreClearMessage.Size) ||
			!MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.Clear, MuiStoreField.MethodId,
				out packet.MethodId)) return false;
		return true;
	}

	internal static bool WriteClear<TPlatform>(ref TPlatform platform,
		APTR message, MuiStoreClearMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiStoreClearMessage.Size)) return false;
		return MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiStorePacketKind.Clear, MuiStoreField.MethodId, packet.MethodId);
	}

	internal static bool TryReadKey<TPlatform>(ref TPlatform platform,
		APTR message, out MuiStoreKeyMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiStoreKeyMessage.Size) ||
			!MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.Key, MuiStoreField.MethodId,
				out packet.MethodId) ||
			!MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.Key, MuiStoreField.Key, out var rawKey))
			return false;
		packet.Key = APTR.FromPointer(rawKey);
		return true;
	}

	internal static bool WriteKey<TPlatform>(ref TPlatform platform,
		APTR message, MuiStoreKeyMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiStoreKeyMessage.Size)) return false;
		return MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiStorePacketKind.Key, MuiStoreField.MethodId, packet.MethodId) &&
			MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.Key, MuiStoreField.Key, packet.Key.Raw);
	}

	internal static bool TryReadCounter<TPlatform>(ref TPlatform platform,
		APTR message, out MuiStoreCounterMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiStoreCounterMessage.Size) ||
			!MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.Counter, MuiStoreField.MethodId,
				out packet.MethodId) ||
			!MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.Counter, MuiStoreField.Counter,
				out var rawCounter)) return false;
		packet.Counter = APTR.FromPointer(rawCounter);
		return true;
	}

	internal static bool WriteCounter<TPlatform>(ref TPlatform platform,
		APTR message, MuiStoreCounterMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiStoreCounterMessage.Size)) return false;
		return MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiStorePacketKind.Counter, MuiStoreField.MethodId, packet.MethodId) &&
			MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.Counter, MuiStoreField.Counter,
				packet.Counter.Raw);
	}

	internal static bool TryReadDatamapSet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDatamapSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiDatamapSetMessage.Size) ||
			!MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.DatamapSet, MuiStoreField.MethodId,
				out packet.MethodId) ||
			!MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.DatamapSet, MuiStoreField.Data,
				out var rawData) ||
			!MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.DatamapSet, MuiStoreField.Length,
				out var rawLength) ||
			!MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.DatamapSet, MuiStoreField.Key,
				out var rawKey)) return false;
		packet.Data = APTR.FromPointer(rawData);
		packet.Length = unchecked((int)rawLength);
		packet.Key = APTR.FromPointer(rawKey);
		return true;
	}

	internal static bool WriteDatamapSet<TPlatform>(ref TPlatform platform,
		APTR message, MuiDatamapSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiDatamapSetMessage.Size)) return false;
		return MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiStorePacketKind.DatamapSet, MuiStoreField.MethodId,
			packet.MethodId) &&
			MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.DatamapSet, MuiStoreField.Data, packet.Data.Raw) &&
			MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.DatamapSet, MuiStoreField.Length,
				unchecked((uint)packet.Length)) &&
			MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.DatamapSet, MuiStoreField.Key, packet.Key.Raw);
	}

	internal static bool TryReadDatamapGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDatamapGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiDatamapGetMessage.Size) ||
			!MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.DatamapGet, MuiStoreField.MethodId,
				out packet.MethodId) ||
			!MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.DatamapGet, MuiStoreField.Key,
				out var rawKey) ||
			!MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.DatamapGet, MuiStoreField.SizeStorage,
				out var rawStorage)) return false;
		packet.Key = APTR.FromPointer(rawKey);
		packet.SizeStorage = APTR.FromPointer(rawStorage);
		return true;
	}

	internal static bool WriteDatamapGet<TPlatform>(ref TPlatform platform,
		APTR message, MuiDatamapGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiDatamapGetMessage.Size)) return false;
		return MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiStorePacketKind.DatamapGet, MuiStoreField.MethodId,
			packet.MethodId) &&
			MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.DatamapGet, MuiStoreField.Key, packet.Key.Raw) &&
			MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.DatamapGet, MuiStoreField.SizeStorage,
				packet.SizeStorage.Raw);
	}

	internal static bool TryReadObjectmapSet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiObjectmapSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiObjectmapSetMessage.Size) ||
			!MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.ObjectmapSet, MuiStoreField.MethodId,
				out packet.MethodId) ||
			!MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.ObjectmapSet, MuiStoreField.Object,
				out var rawObject) ||
			!MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.ObjectmapSet, MuiStoreField.Key,
				out var rawKey)) return false;
		packet.Object = APTR.FromPointer(rawObject);
		packet.Key = APTR.FromPointer(rawKey);
		return true;
	}

	internal static bool WriteObjectmapSet<TPlatform>(ref TPlatform platform,
		APTR message, MuiObjectmapSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiObjectmapSetMessage.Size)) return false;
		return MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiStorePacketKind.ObjectmapSet, MuiStoreField.MethodId,
			packet.MethodId) &&
			MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.ObjectmapSet, MuiStoreField.Object,
				packet.Object.Raw) &&
			MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.ObjectmapSet, MuiStoreField.Key,
				packet.Key.Raw);
	}
}

// Public packet-facing name retained for the store dispatcher and existing
// callers. All production packet reads and writes are routed through the
// declaration-ordered sequential struct codec.
internal static class MuiStorePacketCodec
{
	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, out MuiStoreMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStorePacketSequentialStructCodec.TryReadMethod(ref platform,
			message, out packet);

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, MuiStoreMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStorePacketSequentialStructCodec.WriteMethod(ref platform,
			message, packet);

	internal static bool TryReadClear<TPlatform>(ref TPlatform platform,
		APTR message, out MuiStoreClearMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStorePacketSequentialStructCodec.TryReadClear(ref platform,
			message, out packet);

	internal static bool WriteClear<TPlatform>(ref TPlatform platform,
		APTR message, MuiStoreClearMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStorePacketSequentialStructCodec.WriteClear(ref platform,
			message, packet);

	internal static bool TryReadKey<TPlatform>(ref TPlatform platform,
		APTR message, out MuiStoreKeyMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStorePacketSequentialStructCodec.TryReadKey(ref platform,
			message, out packet);

	internal static bool WriteKey<TPlatform>(ref TPlatform platform,
		APTR message, MuiStoreKeyMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStorePacketSequentialStructCodec.WriteKey(ref platform,
			message, packet);

	internal static bool TryReadCounter<TPlatform>(ref TPlatform platform,
		APTR message, out MuiStoreCounterMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStorePacketSequentialStructCodec.TryReadCounter(ref platform,
			message, out packet);

	internal static bool WriteCounter<TPlatform>(ref TPlatform platform,
		APTR message, MuiStoreCounterMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStorePacketSequentialStructCodec.WriteCounter(ref platform,
			message, packet);

	internal static bool TryReadDatamapSet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDatamapSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStorePacketSequentialStructCodec.TryReadDatamapSet(ref platform,
			message, out packet);

	internal static bool WriteDatamapSet<TPlatform>(ref TPlatform platform,
		APTR message, MuiDatamapSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStorePacketSequentialStructCodec.WriteDatamapSet(ref platform,
			message, packet);

	internal static bool TryReadDatamapGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDatamapGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStorePacketSequentialStructCodec.TryReadDatamapGet(ref platform,
			message, out packet);

	internal static bool WriteDatamapGet<TPlatform>(ref TPlatform platform,
		APTR message, MuiDatamapGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStorePacketSequentialStructCodec.WriteDatamapGet(ref platform,
			message, packet);

	internal static bool TryReadObjectmapSet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiObjectmapSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStorePacketSequentialStructCodec.TryReadObjectmapSet(ref platform,
			message, out packet);

	internal static bool WriteObjectmapSet<TPlatform>(ref TPlatform platform,
		APTR message, MuiObjectmapSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStorePacketSequentialStructCodec.WriteObjectmapSet(ref platform,
			message, packet);
}

// Struct-first codecs for the MorphOS Datamap/Objectmap method families.
// These records contain only guest pointers and fixed-width scalars; no
// managed key/value representation crosses the ABI boundary.
public static class MuiStoreMessageCore
{
	public const uint DatamapSetMethod = 0x8042B84F;
	public const uint DatamapFindMethod = 0x8042D650;
	public const uint DatamapGetMethod = 0x8042C2BA;
	public const uint DatamapIterateMethod = 0x8042FDA1;
	public const uint DatamapIterationKeyMethod = 0x8042BC15;
	public const uint DatamapRemoveMethod = 0x804203D8;
	public const uint DatamapClearMethod = 0x8042EEBC;
	public const uint ObjectmapSetMethod = 0x80421EC5;
	public const uint ObjectmapFindMethod = 0x80426506;
	public const uint ObjectmapIterateMethod = 0x804262BC;
	public const uint ObjectmapIterationKeyMethod = 0x8042D7FF;
	public const uint ObjectmapRemoveMethod = 0x8042F649;
	public const uint ObjectmapClearMethod = 0x80422EE5;
	public const uint DatamapCopyKeysAttribute =
		MuiStorePolicyCore.DatamapCopyKeysAttribute;
	public const uint ObjectmapCopyKeysAttribute =
		MuiStorePolicyCore.ObjectmapCopyKeysAttribute;

	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiStoreMessageCodec.TryReadMethodIdValue(ref platform, message,
			out var method)) return 0;
		if (!MuiStorePolicyCore.IsMethodClassCompatible(ref platform, state, obj,
			MuiStorePolicyCore.ClassifyMethod(method))) return 0;
		switch (method)
		{
			case DatamapSetMethod:
				if (!TryReadDatamapSet(ref platform, message, out var datamapSet))
					return 0;
				return MuiStoreCore.DatamapSet(ref platform, state, obj,
					datamapSet.Key, datamapSet.Data, datamapSet.Length,
					AttributeEnabled(ref platform, state, obj,
						DatamapCopyKeysAttribute)) ? 1u : 0u;
			case DatamapFindMethod:
				if (!TryReadKey(ref platform, message, DatamapFindMethod,
					out var datamapFind)) return 0;
				return MuiStoreCore.DatamapFind(ref platform, state, obj,
					datamapFind.Key).Raw;
			case DatamapGetMethod:
				if (!TryReadDatamapGet(ref platform, message, out var datamapGet))
					return 0;
				return MuiStoreCore.DatamapGet(ref platform, state, obj,
					datamapGet.Key, datamapGet.SizeStorage).Raw;
			case DatamapIterateMethod:
				if (!TryReadCounter(ref platform, message, DatamapIterateMethod,
					out var datamapIterate)) return 0;
				return MuiStoreCore.DatamapIterate(ref platform, state, obj,
					datamapIterate.Counter).Raw;
			case DatamapIterationKeyMethod:
				if (!TryReadCounter(ref platform, message,
					DatamapIterationKeyMethod, out var datamapKey)) return 0;
				return MuiStoreCore.DatamapIterationKey(ref platform, state, obj,
					datamapKey.Counter).Raw;
			case DatamapRemoveMethod:
				if (!TryReadKey(ref platform, message, DatamapRemoveMethod,
					out var datamapRemove)) return 0;
				return MuiStoreCore.DatamapRemove(ref platform, state, obj,
					datamapRemove.Key) ? 1u : 0u;
			case DatamapClearMethod:
				if (!TryReadClear(ref platform, message, DatamapClearMethod)) return 0;
				return MuiStoreCore.DatamapClear(ref platform, state, obj);
			case ObjectmapSetMethod:
				if (!TryReadObjectmapSet(ref platform, message,
					out var objectmapSet)) return 0;
				return MuiStoreCore.ObjectmapSet(ref platform, state, obj,
					objectmapSet.Key, objectmapSet.Object,
					AttributeEnabled(ref platform, state, obj,
						ObjectmapCopyKeysAttribute)) ? 1u : 0u;
			case ObjectmapFindMethod:
				if (!TryReadKey(ref platform, message, ObjectmapFindMethod,
					out var objectmapFind)) return 0;
				return MuiStoreCore.ObjectmapFind(ref platform, state, obj,
					objectmapFind.Key).Raw;
			case ObjectmapIterateMethod:
				if (!TryReadCounter(ref platform, message, ObjectmapIterateMethod,
					out var objectmapIterate)) return 0;
				return MuiStoreCore.ObjectmapIterate(ref platform, state, obj,
					objectmapIterate.Counter).Raw;
			case ObjectmapIterationKeyMethod:
				if (!TryReadCounter(ref platform, message,
					ObjectmapIterationKeyMethod, out var objectmapKey)) return 0;
				return MuiStoreCore.ObjectmapIterationKey(ref platform, state, obj,
					objectmapKey.Counter).Raw;
			case ObjectmapRemoveMethod:
				if (!TryReadKey(ref platform, message, ObjectmapRemoveMethod,
					out var objectmapRemove)) return 0;
				return MuiStoreCore.ObjectmapRemoveObject(ref platform, state, obj,
					objectmapRemove.Key).Raw;
			case ObjectmapClearMethod:
				if (!TryReadClear(ref platform, message, ObjectmapClearMethod)) return 0;
				return MuiStoreCore.ObjectmapClear(ref platform, state, obj);
		}
		return 0;
	}

	// Focused packet-only seam used by native qualification. It proves every
	// fixed header and returns a decoded guest token without pulling the live
	// store allocator into the small freestanding closure.
	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR message)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiStoreMessageCodec.TryReadMethodIdValue(ref platform, message,
			out var method)) return 0;
		if (method == DatamapSetMethod)
		{
			if (!TryReadDatamapSet(ref platform, message, out var set)) return 0;
			return unchecked((uint)set.Length);
		}
		if (method == DatamapGetMethod)
		{
			if (!TryReadDatamapGet(ref platform, message, out var get)) return 0;
			return get.SizeStorage.Raw;
		}
		if (method == ObjectmapSetMethod)
		{
			if (!TryReadObjectmapSet(ref platform, message, out var set)) return 0;
			return set.Object.Raw;
		}
		if (method == DatamapClearMethod || method == ObjectmapClearMethod)
			return TryReadClear(ref platform, message, method) ? 1u : 0u;
		if (method == DatamapFindMethod || method == DatamapRemoveMethod ||
			method == ObjectmapFindMethod || method == ObjectmapRemoveMethod)
		{
			return TryReadKey(ref platform, message, method, out var key) ?
				key.Key.Raw : 0u;
		}
		if (method == DatamapIterateMethod ||
			method == DatamapIterationKeyMethod ||
			method == ObjectmapIterateMethod ||
			method == ObjectmapIterationKeyMethod)
		{
			return TryReadCounter(ref platform, message, method,
				out var counter) ? counter.Counter.Raw : 0u;
		}
		return 0;
	}

	public static bool WriteDatamapSetRecord<TPlatform>(ref TPlatform platform,
		APTR message, APTR data, int length, APTR key)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiDatamapSetMessage);
		packet.MethodId = DatamapSetMethod;
		packet.Data = data;
		packet.Length = length;
		packet.Key = key;
		return MuiStorePacketCodec.WriteDatamapSet(ref platform, message, packet);
	}

	public static bool WriteDatamapGetRecord<TPlatform>(ref TPlatform platform,
		APTR message, APTR key, APTR sizeStorage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiDatamapGetMessage);
		packet.MethodId = DatamapGetMethod;
		packet.Key = key;
		packet.SizeStorage = sizeStorage;
		return MuiStorePacketCodec.WriteDatamapGet(ref platform, message, packet);
	}

	public static bool WriteDatamapKeyRecord<TPlatform>(ref TPlatform platform,
		APTR message, uint method, APTR key)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (method != DatamapFindMethod && method != DatamapRemoveMethod)
			return false;
		var packet = default(MuiStoreKeyMessage);
		packet.MethodId = method;
		packet.Key = key;
		return MuiStorePacketCodec.WriteKey(ref platform, message, packet);
	}

	public static bool WriteDatamapCounterRecord<TPlatform>(ref TPlatform platform,
		APTR message, uint method, APTR counter)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (method != DatamapIterateMethod &&
			method != DatamapIterationKeyMethod) return false;
		var packet = default(MuiStoreCounterMessage);
		packet.MethodId = method;
		packet.Counter = counter;
		return MuiStorePacketCodec.WriteCounter(ref platform, message, packet);
	}

	public static bool WriteDatamapClearRecord<TPlatform>(ref TPlatform platform,
		APTR message)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteClearRecord(ref platform, message, DatamapClearMethod);

	public static bool WriteObjectmapSetRecord<TPlatform>(ref TPlatform platform,
		APTR message, APTR obj, APTR key)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiObjectmapSetMessage);
		packet.MethodId = ObjectmapSetMethod;
		packet.Object = obj;
		packet.Key = key;
		return MuiStorePacketCodec.WriteObjectmapSet(ref platform, message, packet);
	}

	public static bool WriteObjectmapKeyRecord<TPlatform>(ref TPlatform platform,
		APTR message, uint method, APTR key)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (method != ObjectmapFindMethod && method != ObjectmapRemoveMethod)
			return false;
		var packet = default(MuiStoreKeyMessage);
		packet.MethodId = method;
		packet.Key = key;
		return MuiStorePacketCodec.WriteKey(ref platform, message, packet);
	}

	public static bool WriteObjectmapCounterRecord<TPlatform>(
		ref TPlatform platform, APTR message, uint method, APTR counter)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (method != ObjectmapIterateMethod &&
			method != ObjectmapIterationKeyMethod) return false;
		var packet = default(MuiStoreCounterMessage);
		packet.MethodId = method;
		packet.Counter = counter;
		return MuiStorePacketCodec.WriteCounter(ref platform, message, packet);
	}

	public static bool WriteObjectmapClearRecord<TPlatform>(ref TPlatform platform,
		APTR message)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteClearRecord(ref platform, message, ObjectmapClearMethod);

	private static bool WriteClearRecord<TPlatform>(ref TPlatform platform,
		APTR message, uint method) where TPlatform : struct, IMuiGuestMemory
		=> MuiStorePacketSequentialStructCodec.TryWriteClearMethodIdValue(
			ref platform, message, method);

	private static bool TryReadDatamapSet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDatamapSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiStorePacketCodec.TryReadDatamapSet(ref platform, message,
			out packet) && packet.MethodId == DatamapSetMethod;
	}

	private static bool TryReadDatamapGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDatamapGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiStorePacketCodec.TryReadDatamapGet(ref platform, message,
			out packet) && packet.MethodId == DatamapGetMethod;
	}

	private static bool TryReadObjectmapSet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiObjectmapSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiStorePacketCodec.TryReadObjectmapSet(ref platform, message,
			out packet) && packet.MethodId == ObjectmapSetMethod;
	}

	private static bool TryReadKey<TPlatform>(ref TPlatform platform, APTR message,
		uint method, out MuiStoreKeyMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (method != DatamapFindMethod && method != DatamapRemoveMethod &&
			method != ObjectmapFindMethod && method != ObjectmapRemoveMethod)
			return false;
		return MuiStorePacketCodec.TryReadKey(ref platform, message, out packet) &&
			packet.MethodId == method;
	}

	private static bool TryReadCounter<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiStoreCounterMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (method != DatamapIterateMethod &&
			method != DatamapIterationKeyMethod &&
			method != ObjectmapIterateMethod &&
			method != ObjectmapIterationKeyMethod) return false;
		return MuiStorePacketCodec.TryReadCounter(ref platform, message,
			out packet) && packet.MethodId == method;
	}

	private static bool TryReadClear<TPlatform>(ref TPlatform platform,
		APTR message, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		if (method != DatamapClearMethod && method != ObjectmapClearMethod)
			return false;
		return MuiStorePacketSequentialStructCodec.TryReadClearMethodIdValue(
			ref platform, message, out var methodId) && methodId == method;
	}

	private static bool AttributeEnabled<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		attribute == DatamapCopyKeysAttribute
			? MuiStorePolicyCore.CopyKeysEnabled(ref platform, state, obj,
				MuiStorePolicyKind.Datamap)
			: attribute == ObjectmapCopyKeysAttribute
				? MuiStorePolicyCore.CopyKeysEnabled(ref platform, state, obj,
					MuiStorePolicyKind.Objectmap)
				: false;
}
