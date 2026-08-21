/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStoreMethodMessage
{
	internal const uint Size = 4;
	internal uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStoreClearMessage
{
	internal const uint Size = 4;
	internal uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStoreKeyMessage
{
	internal const uint Size = 8;
	internal uint MethodId;
	internal APTR Key;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStoreCounterMessage
{
	internal const uint Size = 8;
	internal uint MethodId;
	internal APTR Counter;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDatamapSetMessage
{
	internal const uint Size = 16;
	internal uint MethodId;
	internal APTR Data;
	internal int Length;
	internal APTR Key;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDatamapGetMessage
{
	internal const uint Size = 12;
	internal uint MethodId;
	internal APTR Key;
	internal APTR SizeStorage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiObjectmapSetMessage
{
	internal const uint Size = 12;
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

internal static class MuiStoreFieldCursorCodec
{
	private static bool TryResolve(MuiStorePacketKind packet,
		MuiStoreField field, out uint offset)
	{
		switch (packet)
		{
			case MuiStorePacketKind.Method:
			case MuiStorePacketKind.Clear:
				if (field == MuiStoreField.MethodId) { offset = 0; return true; }
				break;
			case MuiStorePacketKind.Key:
				if (field == MuiStoreField.MethodId) { offset = 0; return true; }
				if (field == MuiStoreField.Key) { offset = 4; return true; }
				break;
			case MuiStorePacketKind.Counter:
				if (field == MuiStoreField.MethodId) { offset = 0; return true; }
				if (field == MuiStoreField.Counter) { offset = 4; return true; }
				break;
			case MuiStorePacketKind.DatamapSet:
				if (field == MuiStoreField.MethodId) { offset = 0; return true; }
				if (field == MuiStoreField.Data) { offset = 4; return true; }
				if (field == MuiStoreField.Length) { offset = 8; return true; }
				if (field == MuiStoreField.Key) { offset = 12; return true; }
				break;
			case MuiStorePacketKind.DatamapGet:
				if (field == MuiStoreField.MethodId) { offset = 0; return true; }
				if (field == MuiStoreField.Key) { offset = 4; return true; }
				if (field == MuiStoreField.SizeStorage) { offset = 8; return true; }
				break;
			case MuiStorePacketKind.ObjectmapSet:
				if (field == MuiStoreField.MethodId) { offset = 0; return true; }
				if (field == MuiStoreField.Object) { offset = 4; return true; }
				if (field == MuiStoreField.Key) { offset = 8; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStoreFieldCursor cursor, out APTR address)
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
		APTR message, MuiStorePacketKind packet, MuiStoreField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStoreFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiStorePacketKind packet, MuiStoreField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStoreFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStoreMessageCodec
{
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiStoreMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiStoreMethodMessage.Size)) return false;
		return MuiStoreFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiStorePacketKind.Method, MuiStoreField.MethodId, out packet.MethodId);
	}
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
	public const uint DatamapCopyKeysAttribute = 0x8042A179;
	public const uint ObjectmapCopyKeysAttribute = 0x8042B964;

	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiStoreMessageCodec.TryReadMethodId(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
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
				return MuiStoreCore.ObjectmapRemove(ref platform, state, obj,
					objectmapRemove.Key) ? 1u : 0u;
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
		if (!MuiStoreMessageCodec.TryReadMethodId(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
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
		if (message.IsNull || !platform.IsMapped(message,
			MuiDatamapSetMessage.Size)) return false;
		return MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiStorePacketKind.DatamapSet, MuiStoreField.MethodId, DatamapSetMethod) &&
			MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.DatamapSet, MuiStoreField.Data, data.Raw) &&
			MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.DatamapSet, MuiStoreField.Length,
				unchecked((uint)length)) &&
			MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.DatamapSet, MuiStoreField.Key, key.Raw);
	}

	public static bool WriteDatamapGetRecord<TPlatform>(ref TPlatform platform,
		APTR message, APTR key, APTR sizeStorage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiDatamapGetMessage.Size)) return false;
		return MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiStorePacketKind.DatamapGet, MuiStoreField.MethodId, DatamapGetMethod) &&
			MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.DatamapGet, MuiStoreField.Key, key.Raw) &&
			MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.DatamapGet, MuiStoreField.SizeStorage,
				sizeStorage.Raw);
	}

	public static bool WriteDatamapKeyRecord<TPlatform>(ref TPlatform platform,
		APTR message, uint method, APTR key)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (method != DatamapFindMethod && method != DatamapRemoveMethod ||
			message.IsNull || !platform.IsMapped(message,
			MuiStoreKeyMessage.Size)) return false;
		return MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiStorePacketKind.Key, MuiStoreField.MethodId, method) &&
			MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.Key, MuiStoreField.Key, key.Raw);
	}

	public static bool WriteDatamapCounterRecord<TPlatform>(ref TPlatform platform,
		APTR message, uint method, APTR counter)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (method != DatamapIterateMethod &&
			method != DatamapIterationKeyMethod || message.IsNull ||
			!platform.IsMapped(message, MuiStoreCounterMessage.Size)) return false;
		return MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiStorePacketKind.Counter, MuiStoreField.MethodId, method) &&
			MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.Counter, MuiStoreField.Counter, counter.Raw);
	}

	public static bool WriteDatamapClearRecord<TPlatform>(ref TPlatform platform,
		APTR message)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteClearRecord(ref platform, message, DatamapClearMethod);

	public static bool WriteObjectmapSetRecord<TPlatform>(ref TPlatform platform,
		APTR message, APTR obj, APTR key)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiObjectmapSetMessage.Size)) return false;
		return MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiStorePacketKind.ObjectmapSet, MuiStoreField.MethodId,
			ObjectmapSetMethod) &&
			MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.ObjectmapSet, MuiStoreField.Object, obj.Raw) &&
			MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.ObjectmapSet, MuiStoreField.Key, key.Raw);
	}

	public static bool WriteObjectmapKeyRecord<TPlatform>(ref TPlatform platform,
		APTR message, uint method, APTR key)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (method != ObjectmapFindMethod && method != ObjectmapRemoveMethod ||
			message.IsNull || !platform.IsMapped(message,
			MuiStoreKeyMessage.Size)) return false;
		return MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiStorePacketKind.Key, MuiStoreField.MethodId, method) &&
			MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.Key, MuiStoreField.Key, key.Raw);
	}

	public static bool WriteObjectmapCounterRecord<TPlatform>(
		ref TPlatform platform, APTR message, uint method, APTR counter)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (method != ObjectmapIterateMethod &&
			method != ObjectmapIterationKeyMethod || message.IsNull ||
			!platform.IsMapped(message, MuiStoreCounterMessage.Size)) return false;
		return MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiStorePacketKind.Counter, MuiStoreField.MethodId, method) &&
			MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiStorePacketKind.Counter, MuiStoreField.Counter, counter.Raw);
	}

	public static bool WriteObjectmapClearRecord<TPlatform>(ref TPlatform platform,
		APTR message)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteClearRecord(ref platform, message, ObjectmapClearMethod);

	private static bool WriteClearRecord<TPlatform>(ref TPlatform platform,
		APTR message, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiStoreClearMessage.Size)) return false;
		return MuiStoreFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiStorePacketKind.Clear, MuiStoreField.MethodId, method);
	}

	private static bool TryReadDatamapSet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDatamapSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiStoreMessageCodec.TryReadMethodId(ref platform, message,
			out var header) || header.MethodId != DatamapSetMethod ||
			!platform.IsMapped(message, MuiDatamapSetMessage.Size)) return false;
		if (!MuiStoreFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiStorePacketKind.DatamapSet, MuiStoreField.Data, out var rawData) ||
			!MuiStoreFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.DatamapSet, MuiStoreField.Length, out var rawLength) ||
			!MuiStoreFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.DatamapSet, MuiStoreField.Key, out var rawKey))
			return false;
		packet.MethodId = header.MethodId;
		packet.Data = APTR.FromPointer(rawData);
		packet.Length = unchecked((int)rawLength);
		packet.Key = APTR.FromPointer(rawKey);
		return true;
	}

	private static bool TryReadDatamapGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDatamapGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiStoreMessageCodec.TryReadMethodId(ref platform, message,
			out var header) || header.MethodId != DatamapGetMethod ||
			!platform.IsMapped(message, MuiDatamapGetMessage.Size)) return false;
		if (!MuiStoreFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiStorePacketKind.DatamapGet, MuiStoreField.Key, out var rawKey) ||
			!MuiStoreFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.DatamapGet, MuiStoreField.SizeStorage,
				out var rawStorage)) return false;
		packet.MethodId = header.MethodId;
		packet.Key = APTR.FromPointer(rawKey);
		packet.SizeStorage = APTR.FromPointer(rawStorage);
		return true;
	}

	private static bool TryReadObjectmapSet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiObjectmapSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiStoreMessageCodec.TryReadMethodId(ref platform, message,
			out var header) || header.MethodId != ObjectmapSetMethod ||
			!platform.IsMapped(message, MuiObjectmapSetMessage.Size)) return false;
		if (!MuiStoreFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiStorePacketKind.ObjectmapSet, MuiStoreField.Object,
			out var rawObject) ||
			!MuiStoreFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiStorePacketKind.ObjectmapSet, MuiStoreField.Key,
				out var rawKey)) return false;
		packet.MethodId = header.MethodId;
		packet.Object = APTR.FromPointer(rawObject);
		packet.Key = APTR.FromPointer(rawKey);
		return true;
	}

	private static bool TryReadKey<TPlatform>(ref TPlatform platform, APTR message,
		uint method, out MuiStoreKeyMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (method != DatamapFindMethod && method != DatamapRemoveMethod &&
			method != ObjectmapFindMethod && method != ObjectmapRemoveMethod)
			return false;
		if (!MuiStoreMessageCodec.TryReadMethodId(ref platform, message,
			out var header) || header.MethodId != method ||
			!platform.IsMapped(message, MuiStoreKeyMessage.Size))
			return false;
		packet.MethodId = header.MethodId;
		if (!MuiStoreFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiStorePacketKind.Key, MuiStoreField.Key, out var rawKey))
			return false;
		packet.Key = APTR.FromPointer(rawKey);
		return true;
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
		if (!MuiStoreMessageCodec.TryReadMethodId(ref platform, message,
			out var header) || header.MethodId != method ||
			!platform.IsMapped(message, MuiStoreCounterMessage.Size))
			return false;
		packet.MethodId = header.MethodId;
		if (!MuiStoreFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiStorePacketKind.Counter, MuiStoreField.Counter, out var rawCounter))
			return false;
		packet.Counter = APTR.FromPointer(rawCounter);
		return true;
	}

	private static bool TryReadClear<TPlatform>(ref TPlatform platform,
		APTR message, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		if (method != DatamapClearMethod && method != ObjectmapClearMethod ||
			!MuiStoreMessageCodec.TryReadMethodId(ref platform, message,
				out var header) || header.MethodId != method ||
			!platform.IsMapped(message, MuiStoreClearMessage.Size)) return false;
		return true;
	}

	private static bool AttributeEnabled<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out var value) && value != 0;
}
