/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS Dataspace packets. APTR fields remain 32-bit on the 68k ABI and are
// exposed as named fields; the codec below is the only guest-wire boundary.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceMethodMessage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceAddMessage
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal uint MethodId;
	internal APTR Data;
	internal int Length;
	internal uint Id;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceFindMessage
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal uint MethodId;
	internal uint Id;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceGetMessage
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal uint MethodId;
	internal uint Id;
	internal APTR SizeStorage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceMergeMessage
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal uint MethodId;
	internal APTR Dataspace;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceRemoveMessage
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal uint MethodId;
	internal uint Id;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceClearMessage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal uint MethodId;
}

internal enum MuiDataspacePacketKind : byte
{
	Method,
	Add,
	Find,
	Get,
	Merge,
	Remove,
	Clear,
}

internal enum MuiDataspaceField : byte
{
	MethodId,
	Data,
	Length,
	Id,
	SizeStorage,
	Dataspace,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceFieldCursor
{
	internal APTR Message;
	internal MuiDataspacePacketKind Packet;
	internal MuiDataspaceField Field;
}

// Struct-first guest-memory adapter for the fixed Dataspace packet family.
// Header reads admit the 4-byte method record; payload fields require their
// packet's complete named record before being accessed.
internal static class MuiDataspaceMessageMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiDataspacePacketKind packet,
		MuiDataspaceField field, out uint index, out uint recordSize)
	{
		index = 0;
		recordSize = 0;
		switch (packet)
		{
			case MuiDataspacePacketKind.Method:
			case MuiDataspacePacketKind.Clear:
				if (field == MuiDataspaceField.MethodId)
				{
					index = 0;
					recordSize = MuiDataspaceMethodMessage.Size;
					return true;
				}
				break;
			case MuiDataspacePacketKind.Add:
				recordSize = MuiDataspaceAddMessage.Size;
				if (field == MuiDataspaceField.MethodId) { index = 0; return true; }
				if (field == MuiDataspaceField.Data) { index = 1; return true; }
				if (field == MuiDataspaceField.Length) { index = 2; return true; }
				if (field == MuiDataspaceField.Id) { index = 3; return true; }
				break;
			case MuiDataspacePacketKind.Find:
			case MuiDataspacePacketKind.Remove:
				recordSize = packet == MuiDataspacePacketKind.Find ?
					MuiDataspaceFindMessage.Size : MuiDataspaceRemoveMessage.Size;
				if (field == MuiDataspaceField.MethodId)
				{
					index = 0;
					return true;
				}
				if (field == MuiDataspaceField.Id)
				{
					index = 1;
					return true;
				}
				break;
			case MuiDataspacePacketKind.Get:
				recordSize = MuiDataspaceGetMessage.Size;
				if (field == MuiDataspaceField.MethodId) { index = 0; return true; }
				if (field == MuiDataspaceField.Id) { index = 1; return true; }
				if (field == MuiDataspaceField.SizeStorage) { index = 2; return true; }
				break;
			case MuiDataspacePacketKind.Merge:
				recordSize = MuiDataspaceMergeMessage.Size;
				if (field == MuiDataspaceField.MethodId) { index = 0; return true; }
				if (field == MuiDataspaceField.Dataspace) { index = 1; return true; }
				break;
		}
		index = uint.MaxValue;
		recordSize = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspacePacketKind packet, MuiDataspaceField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiDataspaceFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiDataspaceFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Packet, cursor.Field, out var index,
			out var recordSize) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Message,
				recordSize, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiDataspaceAddMessage.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiDataspaceAddMessage.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiDataspaceFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspacePacketKind packet, MuiDataspaceField field,
		out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiDataspaceFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspacePacketKind packet, MuiDataspaceField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (packet == MuiDataspacePacketKind.Method ||
			packet == MuiDataspacePacketKind.Clear)
			return field == MuiDataspaceField.MethodId &&
				MuiDataspaceMethodHeaderCodec.TryReadValue(ref platform, message,
					out value);
		if (packet == MuiDataspacePacketKind.Add)
		{
			if (!MuiDataspaceMessageStructCodec.TryReadAddRecord(ref platform, message,
				out var add)) return false;
			if (field == MuiDataspaceField.MethodId) value = add.MethodId;
			else if (field == MuiDataspaceField.Data) value = add.Data.Raw;
			else if (field == MuiDataspaceField.Length)
				value = unchecked((uint)add.Length);
			else if (field == MuiDataspaceField.Id) value = add.Id;
			else return false;
			return true;
		}
		if (packet == MuiDataspacePacketKind.Find)
		{
			if (!MuiDataspaceMessageStructCodec.TryReadFindRecord(ref platform, message,
				out var find)) return false;
			if (field == MuiDataspaceField.MethodId) value = find.MethodId;
			else if (field == MuiDataspaceField.Id) value = find.Id;
			else return false;
			return true;
		}
		if (packet == MuiDataspacePacketKind.Get)
		{
			if (!MuiDataspaceMessageStructCodec.TryReadGetRecord(ref platform, message,
				out var get)) return false;
			if (field == MuiDataspaceField.MethodId) value = get.MethodId;
			else if (field == MuiDataspaceField.Id) value = get.Id;
			else if (field == MuiDataspaceField.SizeStorage)
				value = get.SizeStorage.Raw;
			else return false;
			return true;
		}
		if (packet == MuiDataspacePacketKind.Merge)
		{
			if (!MuiDataspaceMessageStructCodec.TryReadMergeRecord(ref platform, message,
				out var merge)) return false;
			if (field == MuiDataspaceField.MethodId) value = merge.MethodId;
			else if (field == MuiDataspaceField.Dataspace)
				value = merge.Dataspace.Raw;
			else return false;
			return true;
		}
		if (packet == MuiDataspacePacketKind.Remove)
		{
			if (!MuiDataspaceMessageStructCodec.TryReadRemoveRecord(ref platform, message,
				out var remove)) return false;
			if (field == MuiDataspaceField.MethodId) value = remove.MethodId;
			else if (field == MuiDataspaceField.Id) value = remove.Id;
			else return false;
			return true;
		}
		return false;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspacePacketKind packet, MuiDataspaceField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (packet == MuiDataspacePacketKind.Method ||
			packet == MuiDataspacePacketKind.Clear)
			return field == MuiDataspaceField.MethodId &&
				MuiDataspaceMethodHeaderCodec.WriteValue(ref platform, message, value);
		if (packet == MuiDataspacePacketKind.Add)
		{
			if (!MuiDataspaceMessageStructCodec.TryReadAddRecord(ref platform, message,
				out var add)) return false;
			if (field == MuiDataspaceField.MethodId) add.MethodId = value;
			else if (field == MuiDataspaceField.Data) add.Data = APTR.FromPointer(value);
			else if (field == MuiDataspaceField.Length)
				add.Length = unchecked((int)value);
			else if (field == MuiDataspaceField.Id) add.Id = value;
			else return false;
			return MuiDataspaceMessageStructCodec.WriteAddRecord(ref platform,
				message, add);
		}
		if (packet == MuiDataspacePacketKind.Find)
		{
			if (!MuiDataspaceMessageStructCodec.TryReadFindRecord(ref platform, message,
				out var find)) return false;
			if (field == MuiDataspaceField.MethodId) find.MethodId = value;
			else if (field == MuiDataspaceField.Id) find.Id = value;
			else return false;
			return MuiDataspaceMessageStructCodec.WriteFindRecord(ref platform,
				message, find);
		}
		if (packet == MuiDataspacePacketKind.Get)
		{
			if (!MuiDataspaceMessageStructCodec.TryReadGetRecord(ref platform, message,
				out var get)) return false;
			if (field == MuiDataspaceField.MethodId) get.MethodId = value;
			else if (field == MuiDataspaceField.Id) get.Id = value;
			else if (field == MuiDataspaceField.SizeStorage)
				get.SizeStorage = APTR.FromPointer(value);
			else return false;
			return MuiDataspaceMessageStructCodec.WriteGetRecord(ref platform,
				message, get);
		}
		if (packet == MuiDataspacePacketKind.Merge)
		{
			if (!MuiDataspaceMessageStructCodec.TryReadMergeRecord(ref platform, message,
				out var merge)) return false;
			if (field == MuiDataspaceField.MethodId) merge.MethodId = value;
			else if (field == MuiDataspaceField.Dataspace)
				merge.Dataspace = APTR.FromPointer(value);
			else return false;
			return MuiDataspaceMessageStructCodec.WriteMergeRecord(ref platform,
				message, merge);
		}
		if (packet == MuiDataspacePacketKind.Remove)
		{
			if (!MuiDataspaceMessageStructCodec.TryReadRemoveRecord(ref platform, message,
				out var remove)) return false;
			if (field == MuiDataspaceField.MethodId) remove.MethodId = value;
			else if (field == MuiDataspaceField.Id) remove.Id = value;
			else return false;
			return MuiDataspaceMessageStructCodec.WriteRemoveRecord(ref platform,
				message, remove);
		}
		return false;
	}
}

// Compatibility wrapper retained for existing typed cursor callers. The live
// packet codecs route through the named-record adapter above.
internal static class MuiDataspaceFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiDataspaceFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDataspaceMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Packet, cursor.Field, out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiDataspaceFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDataspaceMessageMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspacePacketKind packet, MuiDataspaceField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform, message, packet,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspacePacketKind packet, MuiDataspaceField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform, message, packet,
			field, value);
}

// Sequential codecs for the fixed Dataspace records.  The legacy field
// adapter above remains available for compatibility diagnostics, but live
// packet reads and writes go through these declaration-order cursors so the
// ABI is described by named structs rather than repeated offsets.
internal static class MuiDataspaceMessageStructCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDataspaceMethodHeaderCodec.TryReadValue(ref platform, message,
			out methodId);

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryWriteMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDataspaceMethodHeaderCodec.WriteValue(ref platform, message,
			methodId);

	internal static bool TryReadAddRecord<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceAddMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDataspaceAddMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawData) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawLength) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var id) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		packet.MethodId = methodId;
		packet.Data = APTR.FromPointer(rawData);
		packet.Length = unchecked((int)rawLength);
		packet.Id = id;
		return true;
	}

	internal static bool TryReadAdd<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceAddMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadAddRecord(ref platform, message, out packet) &&
		packet.MethodId == MuiDataspaceMessageCodec.AddMethod;

	internal static bool TryWriteAdd<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceAddMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet.MethodId = MuiDataspaceMessageCodec.AddMethod;
		return WriteAddRecord(ref platform, message, packet);
	}

	internal static bool WriteAddRecord<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceAddMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDataspaceAddMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Data.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)packet.Length)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Id)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadFindRecord<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceFindMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDataspaceFindMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var id) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		packet.MethodId = methodId;
		packet.Id = id;
		return true;
	}

	internal static bool TryReadFind<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceFindMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadFindRecord(ref platform, message, out packet) &&
		packet.MethodId == MuiDataspaceMessageCodec.FindMethod;

	internal static bool TryWriteFind<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceFindMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet.MethodId = MuiDataspaceMessageCodec.FindMethod;
		return WriteFindRecord(ref platform, message, packet);
	}

	internal static bool WriteFindRecord<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceFindMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDataspaceFindMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Id)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadGetRecord<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDataspaceGetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var id) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawStorage) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		packet.MethodId = methodId;
		packet.Id = id;
		packet.SizeStorage = APTR.FromPointer(rawStorage);
		return true;
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadGetRecord(ref platform, message, out packet) &&
		packet.MethodId == MuiDataspaceMessageCodec.GetMethod;

	internal static bool TryWriteGet<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet.MethodId = MuiDataspaceMessageCodec.GetMethod;
		return WriteGetRecord(ref platform, message, packet);
	}

	internal static bool WriteGetRecord<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDataspaceGetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Id) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.SizeStorage.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadMergeRecord<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceMergeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDataspaceMergeMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawDataspace) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		packet.MethodId = methodId;
		packet.Dataspace = APTR.FromPointer(rawDataspace);
		return true;
	}

	internal static bool TryReadMerge<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceMergeMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadMergeRecord(ref platform, message, out packet) &&
		packet.MethodId == MuiDataspaceMessageCodec.MergeMethod;

	internal static bool TryWriteMerge<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceMergeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet.MethodId = MuiDataspaceMessageCodec.MergeMethod;
		return WriteMergeRecord(ref platform, message, packet);
	}

	internal static bool WriteMergeRecord<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceMergeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDataspaceMergeMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Dataspace.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadRemoveRecord<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceRemoveMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDataspaceRemoveMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var id) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		packet.MethodId = methodId;
		packet.Id = id;
		return true;
	}

	internal static bool TryReadRemove<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceRemoveMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRemoveRecord(ref platform, message, out packet) &&
		packet.MethodId == MuiDataspaceMessageCodec.RemoveMethod;

	internal static bool TryWriteRemove<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceRemoveMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet.MethodId = MuiDataspaceMessageCodec.RemoveMethod;
		return WriteRemoveRecord(ref platform, message, packet);
	}

	internal static bool WriteRemoveRecord<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceRemoveMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDataspaceRemoveMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Id)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadClearRecord<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceClearMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDataspaceClearMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		packet.MethodId = methodId;
		return true;
	}

	internal static bool TryReadClear<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceClearMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadClearRecord(ref platform, message, out packet) &&
		packet.MethodId == MuiDataspaceMessageCodec.ClearMethod;

	internal static bool TryWriteClear<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceClearMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet.MethodId = MuiDataspaceMessageCodec.ClearMethod;
		return WriteClearRecord(ref platform, message, packet);
	}

	internal static bool WriteClearRecord<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceClearMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDataspaceClearMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.MethodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Struct-first codec for the method-only Dataspace header. The named
// one-ULONG record remains the ABI contract; shared guest storage keeps the
// selector boundary free of direct scalar lowering in freestanding 68k code.
internal static class MuiDataspaceMethodHeaderCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiDataspaceMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.TryReadValue(ref platform, valueAddress,
				out methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteValue<TPlatform>(ref TPlatform platform,
		APTR address, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiDataspaceMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Central codec for the fixed Dataspace packet family. All consumers receive
// named records; the explicit offsets below are confined to this packed ABI
// adapter and are never repeated by dispatch or store code.
internal static class MuiDataspaceMessageCodec
{
	internal const uint AddMethod = 0x80423366;
	internal const uint ClearMethod = 0x8042B6C9;
	internal const uint FindMethod = 0x8042832C;
	internal const uint GetMethod = 0x8042483F;
	internal const uint MergeMethod = 0x80423E2B;
	internal const uint RemoveMethod = 0x8042DCE1;

	// Keep scalar method admission at the guest ABI boundary; Dataspace
	// consumers continue to use the named fixed-width method record.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiDataspaceMessageStructCodec.TryReadMethodIdValue(ref platform,
			message, out methodId);
	}

	internal static bool TryWriteMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDataspaceMessageStructCodec.TryWriteMethodIdValue(ref platform,
			message, methodId);

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiDataspaceMethodMessage.Size)) return false;
		if (!TryReadMethodIdValue(ref platform, message, out packet.MethodId))
			return false;
		return true;
	}

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, out uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		method = 0;
		if (!TryReadMethodId(ref platform, message, out var packet)) return false;
		method = packet.MethodId;
		return true;
	}

	internal static bool TryReadAdd<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceAddMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiDataspaceMessageStructCodec.TryReadAdd(ref platform, message,
			out packet) && packet.MethodId == AddMethod;
	}

	internal static bool TryReadFind<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceFindMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiDataspaceMessageStructCodec.TryReadFind(ref platform, message,
			out packet) && packet.MethodId == FindMethod;
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiDataspaceMessageStructCodec.TryReadGet(ref platform, message,
			out packet) && packet.MethodId == GetMethod;
	}

	internal static bool TryReadMerge<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceMergeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiDataspaceMessageStructCodec.TryReadMerge(ref platform, message,
			out packet) && packet.MethodId == MergeMethod;
	}

	internal static bool TryReadRemove<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceRemoveMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiDataspaceMessageStructCodec.TryReadRemove(ref platform, message,
			out packet) && packet.MethodId == RemoveMethod;
	}

	internal static bool TryReadClear<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceClearMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiDataspaceMessageStructCodec.TryReadClear(ref platform, message,
			out packet) && packet.MethodId == ClearMethod;
	}

	internal static bool TryWriteAdd<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceAddMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDataspaceMessageStructCodec.TryWriteAdd(ref platform, message,
			packet);

	internal static bool TryWriteFind<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceFindMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDataspaceMessageStructCodec.TryWriteFind(ref platform, message,
			packet);

	internal static bool TryWriteGet<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDataspaceMessageStructCodec.TryWriteGet(ref platform, message,
			packet);

	internal static bool TryWriteMerge<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceMergeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDataspaceMessageStructCodec.TryWriteMerge(ref platform, message,
			packet);

	internal static bool TryWriteRemove<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceRemoveMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDataspaceMessageStructCodec.TryWriteRemove(ref platform, message,
			packet);

	internal static bool TryWriteClear<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceClearMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDataspaceMessageStructCodec.TryWriteClear(ref platform, message,
			packet);

}

// Fixed packet codecs for the Dataspace superclass. The live store remains
// in MuiStoreCore; this type owns only ABI validation and named field decode.
public static class MuiDataspaceMessageCore
{
	public const uint AddMethod = MuiDataspaceMessageCodec.AddMethod;
	public const uint ClearMethod = MuiDataspaceMessageCodec.ClearMethod;
	public const uint FindMethod = MuiDataspaceMessageCodec.FindMethod;
	public const uint GetMethod = MuiDataspaceMessageCodec.GetMethod;
	public const uint MergeMethod = MuiDataspaceMessageCodec.MergeMethod;
	public const uint RemoveMethod = MuiDataspaceMessageCodec.RemoveMethod;

	internal static bool TryReadAdd<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceAddMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDataspaceMessageCodec.TryReadAdd(ref platform, message, out packet);

	internal static bool TryReadFind<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceFindMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDataspaceMessageCodec.TryReadFind(ref platform, message, out packet);

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDataspaceMessageCodec.TryReadGet(ref platform, message, out packet);

	internal static bool TryReadMerge<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceMergeMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDataspaceMessageCodec.TryReadMerge(ref platform, message, out packet);

	internal static bool TryReadRemove<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceRemoveMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDataspaceMessageCodec.TryReadRemove(ref platform, message, out packet);

	public static bool WriteAddRecord<TPlatform>(ref TPlatform platform,
		APTR message, APTR data, int length, uint id)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiDataspaceAddMessage);
		packet.MethodId = AddMethod;
		packet.Data = data;
		packet.Length = length;
		packet.Id = id;
		return MuiDataspaceMessageCodec.TryWriteAdd(ref platform, message,
			packet);
	}

	public static bool WriteFindRecord<TPlatform>(ref TPlatform platform,
		APTR message, uint id) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiDataspaceFindMessage);
		packet.MethodId = FindMethod;
		packet.Id = id;
		return MuiDataspaceMessageCodec.TryWriteFind(ref platform, message,
			packet);
	}

	public static bool WriteGetRecord<TPlatform>(ref TPlatform platform,
		APTR message, uint id, APTR sizeStorage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiDataspaceGetMessage);
		packet.MethodId = GetMethod;
		packet.Id = id;
		packet.SizeStorage = sizeStorage;
		return MuiDataspaceMessageCodec.TryWriteGet(ref platform, message,
			packet);
	}

	public static bool WriteMergeRecord<TPlatform>(ref TPlatform platform,
		APTR message, APTR dataspace) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiDataspaceMergeMessage);
		packet.MethodId = MergeMethod;
		packet.Dataspace = dataspace;
		return MuiDataspaceMessageCodec.TryWriteMerge(ref platform, message,
			packet);
	}

	public static bool WriteRemoveRecord<TPlatform>(ref TPlatform platform,
		APTR message, uint id) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiDataspaceRemoveMessage);
		packet.MethodId = RemoveMethod;
		packet.Id = id;
		return MuiDataspaceMessageCodec.TryWriteRemove(ref platform, message,
			packet);
	}

	public static bool WriteClearRecord<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiDataspaceClearMessage);
		packet.MethodId = ClearMethod;
		return MuiDataspaceMessageCodec.TryWriteClear(ref platform, message,
			packet);
	}

	// Struct-only native qualification seam. It returns the decoded selector
	// field so a freestanding fixture can verify every packet without pulling
	// the larger object/store lifecycle into its closure.
	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiDataspaceMessageCodec.TryReadMethod(ref platform, message,
			out var method)) return 0;
		switch (method)
		{
			case AddMethod:
				return TryReadAdd(ref platform, message, out var add) ? add.Id : 0;
			case FindMethod:
				return TryReadFind(ref platform, message, out var find) ? find.Id : 0;
			case GetMethod:
				return TryReadGet(ref platform, message, out var get) ?
					get.SizeStorage.Raw : 0;
			case MergeMethod:
				return TryReadMerge(ref platform, message, out var merge) ?
					merge.Dataspace.Raw : 0;
			case RemoveMethod:
				return TryReadRemove(ref platform, message, out var remove) ?
					remove.Id : 0;
			case ClearMethod:
				return MuiDataspaceMessageCodec.TryReadClear(ref platform,
					message, out _) ? 1u : 0u;
		}
		return 0;
	}
}
