/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS MUI Dataspace IFF packets.  These are the guest ABI records for
// MUIM_Dataspace_ReadIFF and MUIM_Dataspace_WriteIFF; callers do not need to
// remember byte offsets when constructing or validating a packet.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceReadIffMessage
{
	internal const uint Size = 8;
	internal uint MethodId;
	internal APTR Handle;
}

internal enum MuiDataspaceReadIffField : byte
{
	MethodId,
	Handle,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceReadIffFieldCursor
{
	internal APTR Message;
	internal MuiDataspaceReadIffField Field;
}

internal static class MuiDataspaceReadIffFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiDataspaceReadIffFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiDataspaceReadIffField.MethodId:
				offset = 0;
				break;
			case MuiDataspaceReadIffField.Handle:
				offset = 4;
				break;
			default:
				return false;
		}
		if (cursor.Message.IsNull || cursor.Message.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceReadIffField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiDataspaceReadIffFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceReadIffField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiDataspaceReadIffFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceWriteIffMessage
{
	internal const uint Size = 16;
	internal uint MethodId;
	internal APTR Handle;
	internal uint Type;
	internal uint Id;
}

internal enum MuiDataspaceWriteIffField : byte
{
	MethodId,
	Handle,
	Type,
	Id,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceWriteIffFieldCursor
{
	internal APTR Message;
	internal MuiDataspaceWriteIffField Field;
}

internal static class MuiDataspaceWriteIffFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiDataspaceWriteIffFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiDataspaceWriteIffField.MethodId:
				offset = 0;
				break;
			case MuiDataspaceWriteIffField.Handle:
				offset = 4;
				break;
			case MuiDataspaceWriteIffField.Type:
				offset = 8;
				break;
			case MuiDataspaceWriteIffField.Id:
				offset = 12;
				break;
			default:
				return false;
		}
		if (cursor.Message.IsNull || cursor.Message.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceWriteIffField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiDataspaceWriteIffFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceWriteIffField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiDataspaceWriteIffFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Named view of one guest-resident Dataspace store record.  StoreCore remains
// the owner of allocation and linkage; this value record is the only place
// where the persistence bridge interprets that fixed internal layout.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceEntryRecord
{
	internal APTR Next;
	internal uint Id;
	internal APTR Data;
	internal uint Length;
	internal uint Flags;
	internal uint Generation;
}

// One entry in the private Dataspace IFF chunk stream. The wire header is
// exactly two big-endian ULONGs; keep its meaning named so the streaming
// implementation never reads an anonymous id/length offset.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceIffEntryHeader
{
	internal const uint Size = 8;
	internal uint Id;
	internal uint Length;
}

internal enum MuiDataspaceIffEntryHeaderField : byte
{
	Id,
	Length,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceIffEntryHeaderFieldCursor
{
	internal APTR Header;
	internal MuiDataspaceIffEntryHeaderField Field;
}

internal static class MuiDataspaceIffEntryHeaderFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiDataspaceIffEntryHeaderFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiDataspaceIffEntryHeaderField.Id:
				offset = 0;
				break;
			case MuiDataspaceIffEntryHeaderField.Length:
				offset = 4;
				break;
			default:
				return false;
		}
		if (cursor.Header.IsNull || cursor.Header.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Header.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR header, MuiDataspaceIffEntryHeaderField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiDataspaceIffEntryHeaderFieldCursor);
		cursor.Header = header;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR header, MuiDataspaceIffEntryHeaderField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiDataspaceIffEntryHeaderFieldCursor);
		cursor.Header = header;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiDataspaceIffEntryHeaderCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiDataspaceIffEntryHeader value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiDataspaceIffEntryHeader.Size)) return false;
		return MuiDataspaceIffEntryHeaderFieldCursorCodec.TryRead(ref platform,
			address, MuiDataspaceIffEntryHeaderField.Id, out value.Id) &&
			MuiDataspaceIffEntryHeaderFieldCursorCodec.TryRead(ref platform,
				address, MuiDataspaceIffEntryHeaderField.Length, out value.Length);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiDataspaceIffEntryHeader value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiDataspaceIffEntryHeader.Size)) return false;
		return MuiDataspaceIffEntryHeaderFieldCursorCodec.TryWrite(ref platform,
			address, MuiDataspaceIffEntryHeaderField.Id, value.Id) &&
			MuiDataspaceIffEntryHeaderFieldCursorCodec.TryWrite(ref platform,
				address, MuiDataspaceIffEntryHeaderField.Length, value.Length);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceIffTransferCursor
{
	internal APTR Base;
	internal uint Offset;
}

internal static class MuiDataspaceIffTransferCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiDataspaceIffTransferCursor cursor, uint byteCount, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Base.Raw >
			uint.MaxValue - cursor.Offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + cursor.Offset);
		return byteCount == 0 || platform.IsMapped(address, byteCount);
	}
}

// Central codec for the two fixed Dataspace IFF packets. Consumers receive
// named records; only this adapter carries the packed guest offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceIffMethodMessage
{
	internal const uint Size = 4;
	internal uint MethodId;
}

internal enum MuiDataspaceIffMethodField : byte
{
	MethodId,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceIffMethodFieldCursor
{
	internal APTR Message;
	internal MuiDataspaceIffMethodField Field;
}

internal static class MuiDataspaceIffMethodFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiDataspaceIffMethodFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Field != MuiDataspaceIffMethodField.MethodId ||
			cursor.Message.IsNull) return false;
		address = cursor.Message;
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceIffMethodField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiDataspaceIffMethodFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceIffMethodField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiDataspaceIffMethodFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiDataspaceIffMessageCodec
{
	internal const uint ReadIffMethod = 0x80420DFB;
	internal const uint WriteIffMethod = 0x80425E8E;

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceIffMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiDataspaceIffMethodMessage.Size)) return false;
		return MuiDataspaceIffMethodFieldCursorCodec.TryRead(ref platform,
			message, MuiDataspaceIffMethodField.MethodId, out packet.MethodId);
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

	internal static bool TryReadReadIff<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceReadIffMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiDataspaceReadIffMessage.Size,
			ReadIffMethod)) return false;
		packet.MethodId = ReadIffMethod;
		if (!MuiDataspaceReadIffFieldCursorCodec.TryRead(ref platform, message,
			MuiDataspaceReadIffField.Handle, out var rawHandle)) return false;
		packet.Handle = APTR.FromPointer(rawHandle);
		return true;
	}

	internal static bool TryReadWriteIff<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceWriteIffMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiDataspaceWriteIffMessage.Size,
			WriteIffMethod)) return false;
		packet.MethodId = WriteIffMethod;
		if (!MuiDataspaceWriteIffFieldCursorCodec.TryRead(ref platform, message,
			MuiDataspaceWriteIffField.Handle, out var rawHandle)) return false;
		packet.Handle = APTR.FromPointer(rawHandle);
		return MuiDataspaceWriteIffFieldCursorCodec.TryRead(ref platform, message,
			MuiDataspaceWriteIffField.Type, out packet.Type) &&
			MuiDataspaceWriteIffFieldCursorCodec.TryRead(ref platform, message,
				MuiDataspaceWriteIffField.Id, out packet.Id);
	}

	internal static bool TryWriteReadIff<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceReadIffMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMapped(ref platform, message, MuiDataspaceReadIffMessage.Size))
			return false;
		return MuiDataspaceReadIffFieldCursorCodec.TryWrite(ref platform,
			message, MuiDataspaceReadIffField.MethodId, ReadIffMethod) &&
			MuiDataspaceReadIffFieldCursorCodec.TryWrite(ref platform, message,
				MuiDataspaceReadIffField.Handle, packet.Handle.Raw);
	}

	internal static bool TryWriteWriteIff<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceWriteIffMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMapped(ref platform, message, MuiDataspaceWriteIffMessage.Size))
			return false;
		return MuiDataspaceWriteIffFieldCursorCodec.TryWrite(ref platform,
			message, MuiDataspaceWriteIffField.MethodId, WriteIffMethod) &&
			MuiDataspaceWriteIffFieldCursorCodec.TryWrite(ref platform, message,
				MuiDataspaceWriteIffField.Handle, packet.Handle.Raw) &&
			MuiDataspaceWriteIffFieldCursorCodec.TryWrite(ref platform, message,
				MuiDataspaceWriteIffField.Type, packet.Type) &&
			MuiDataspaceWriteIffFieldCursorCodec.TryWrite(ref platform, message,
				MuiDataspaceWriteIffField.Id, packet.Id);
	}

	private static bool IsPacket<TPlatform>(ref TPlatform platform,
		APTR message, uint size, uint method)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadMethodId(ref platform, message, out var header) &&
		header.MethodId == method && IsMapped(ref platform, message, size);

	private static bool IsMapped<TPlatform>(ref TPlatform platform,
		APTR message, uint size) where TPlatform : struct, IMuiGuestMemory =>
		message.IsNotNull && platform.IsMapped(message, size);
}

public static class MuiDataspaceIffMessageCore
{
	public const uint ReadIffMethod = MuiDataspaceIffMessageCodec.ReadIffMethod;
	public const uint WriteIffMethod = MuiDataspaceIffMessageCodec.WriteIffMethod;

	internal static bool TryReadReadIff<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceReadIffMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDataspaceIffMessageCodec.TryReadReadIff(ref platform, message,
			out packet);

	internal static bool TryReadWriteIff<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceWriteIffMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDataspaceIffMessageCodec.TryReadWriteIff(ref platform, message,
			out packet);

	public static bool WriteReadIffRecord<TPlatform>(ref TPlatform platform,
		APTR message, APTR handle) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiDataspaceReadIffMessage);
		packet.MethodId = ReadIffMethod;
		packet.Handle = handle;
		return MuiDataspaceIffMessageCodec.TryWriteReadIff(ref platform,
			message, packet);
	}

	public static bool WriteWriteIffRecord<TPlatform>(ref TPlatform platform,
		APTR message, APTR handle, uint type, uint id)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiDataspaceWriteIffMessage);
		packet.MethodId = WriteIffMethod;
		packet.Handle = handle;
		packet.Type = type;
		packet.Id = id;
		return MuiDataspaceIffMessageCodec.TryWriteWriteIff(ref platform,
			message, packet);
	}

	// Struct-only native qualification seam. It proves packet construction and
	// decode without pulling IFFParse or the managed test store into a native
	// closure.
	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiDataspaceIffMessageCodec.TryReadMethod(ref platform, message,
			out var method)) return 0;
		switch (method)
		{
			case ReadIffMethod:
				return TryReadReadIff(ref platform, message, out var read) ?
					read.Handle.Raw : 0;
			case WriteIffMethod:
				if (!TryReadWriteIff(ref platform, message, out var write))
					return 0;
				return write.Handle.Raw ^ write.Type ^ write.Id;
		}
		return 0;
	}
}

public static class MuiDataspaceIffCore
{
	// IFFParse error values are negative LONGs on the guest ABI. Keeping the
	// constants here avoids a managed enum conversion in the freestanding path.
	public const int Eof = -1;
	public const int NoMem = -4;
	public const int Read = -5;
	public const int Write = -6;
	public const int Mangled = -8;
	private const uint UnknownChunkSize = 0xFFFFFFFF;
	private const uint MaximumEntryLength = 65536;

	public static int ReadIFF<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR handle)
		where TPlatform : struct, IMuiHeadlessPlatform, IMuiIffCapability
	{
		if (handle.IsNull || MuiHeadlessObjectCore.FindObject(ref platform,
			state, obj).IsNull) return Mangled;
		var header = MuiHeadlessMemory.Allocate(ref platform,
			MuiDataspaceIffEntryHeader.Size);
		if (header.IsNull) return NoMem;
		var result = 0;
		var headerCursor = default(MuiDataspaceIffTransferCursor);
		headerCursor.Base = header;
		while (result == 0)
		{
			var received = 0u;
			while (received < MuiDataspaceIffEntryHeader.Size)
			{
				headerCursor.Offset = received;
				if (!MuiDataspaceIffTransferCursorCodec.TryGetAddress(
					ref platform, headerCursor,
					MuiDataspaceIffEntryHeader.Size - received,
					out var headerAddress))
				{
					result = Mangled;
					break;
				}
				var count = platform.ReadChunkBytes(handle,
					headerAddress,
					MuiDataspaceIffEntryHeader.Size - received);
				if (count < 0)
				{
					result = count;
					break;
				}
				if (count == 0)
				{
					result = received == 0 ? 1 : Mangled;
					break;
				}
				if ((uint)count > MuiDataspaceIffEntryHeader.Size - received)
				{
					result = Mangled;
					break;
				}
				received += (uint)count;
			}
			if (result != 0) break;
			if (!MuiDataspaceIffEntryHeaderCodec.TryRead(ref platform, header,
				out var entryHeader))
			{
				result = Mangled;
				break;
			}
			var id = entryHeader.Id;
			var length = entryHeader.Length;
			if (length > MaximumEntryLength)
			{
				result = Mangled;
				break;
			}
			var data = APTR.Null;
			if (length != 0)
			{
				data = MuiHeadlessMemory.Allocate(ref platform, length);
				if (data.IsNull)
				{
					result = NoMem;
					break;
				}
				result = ReadExact(ref platform, handle, data, length);
				if (result != 0)
				{
					platform.Free(data, length);
					break;
				}
			}
			if (!MuiStoreCore.DataspaceAdd(ref platform, state, obj, id, data,
				unchecked((int)length)))
			{
				if (data.IsNotNull) platform.Free(data, length);
				result = NoMem;
				break;
			}
			if (data.IsNotNull) platform.Free(data, length);
		}
		platform.Free(header, MuiDataspaceIffEntryHeader.Size);
		return result == 1 ? 0 : result;
	}

	public static int WriteIFF<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR handle, uint type, uint id)
		where TPlatform : struct, IMuiHeadlessPlatform, IMuiIffCapability
	{
		if (handle.IsNull || MuiHeadlessObjectCore.FindObject(ref platform,
			state, obj).IsNull) return Mangled;
		var push = platform.PushChunk(handle, type, id, UnknownChunkSize);
		if (push != 0) return push;
		var counter = MuiHeadlessMemory.Allocate(ref platform, 4);
		var header = MuiHeadlessMemory.Allocate(ref platform,
			MuiDataspaceIffEntryHeader.Size);
		var result = 0;
		if (counter.IsNull || header.IsNull)
			result = NoMem;
		while (result == 0)
		{
			var record = MuiStoreCore.DataspaceIterationRecord(ref platform,
				state, obj, counter);
			if (record.IsNull) break;
			if (!TryReadEntry(ref platform, record, out var entry))
			{
				result = Mangled;
				break;
			}
			var entryHeader = default(MuiDataspaceIffEntryHeader);
			entryHeader.Id = entry.Id;
			entryHeader.Length = entry.Length;
			if (!MuiDataspaceIffEntryHeaderCodec.Write(ref platform, header,
				entryHeader))
			{
				result = Mangled;
				break;
			}
			result = WriteExact(ref platform, handle, header,
				MuiDataspaceIffEntryHeader.Size);
			if (result == 0 && entry.Length != 0)
				result = WriteExact(ref platform, handle, entry.Data, entry.Length);
		}
		var pop = platform.PopChunk(handle);
		if (result == 0 && pop != 0) result = pop;
		if (header.IsNotNull)
			platform.Free(header, MuiDataspaceIffEntryHeader.Size);
		if (counter.IsNotNull) platform.Free(counter, 4);
		return result;
	}

	private static int ReadExact<TPlatform>(ref TPlatform platform, APTR handle,
		APTR buffer, uint length)
		where TPlatform : struct, IMuiIffCapability, IMuiGuestMemory
	{
		var received = 0u;
		var cursor = default(MuiDataspaceIffTransferCursor);
		cursor.Base = buffer;
		while (received < length)
		{
			cursor.Offset = received;
			if (!MuiDataspaceIffTransferCursorCodec.TryGetAddress(
				ref platform, cursor, length - received, out var address))
				return Mangled;
			var count = platform.ReadChunkBytes(handle,
				address, length - received);
			if (count < 0) return count;
			if (count == 0) return Eof;
			if ((uint)count > length - received) return Mangled;
			received += (uint)count;
		}
		return 0;
	}

	private static int WriteExact<TPlatform>(ref TPlatform platform, APTR handle,
		APTR buffer, uint length)
		where TPlatform : struct, IMuiIffCapability, IMuiGuestMemory
	{
		var written = 0u;
		var cursor = default(MuiDataspaceIffTransferCursor);
		cursor.Base = buffer;
		while (written < length)
		{
			cursor.Offset = written;
			if (!MuiDataspaceIffTransferCursorCodec.TryGetAddress(
				ref platform, cursor, length - written, out var address))
				return Mangled;
			var count = platform.WriteChunkBytes(handle,
				address, length - written);
			if (count < 0) return count;
			if (count == 0) return Write;
			if ((uint)count > length - written) return Mangled;
			written += (uint)count;
		}
		return 0;
	}

	private static bool TryReadEntry<TPlatform>(ref TPlatform platform,
		APTR record, out MuiDataspaceEntryRecord entry)
		where TPlatform : struct, IMuiGuestMemory
	{
		entry = default;
		if (!MuiStoreRecordCodec.TryRead(ref platform, record,
			out var storeRecord)) return false;
		entry.Next = storeRecord.Next;
		entry.Id = storeRecord.Key;
		entry.Data = storeRecord.Data;
		entry.Length = storeRecord.Length;
		entry.Flags = storeRecord.Flags;
		entry.Generation = storeRecord.Generation;
		return entry.Length <= MaximumEntryLength &&
			(entry.Length == 0 || (entry.Data.IsNotNull &&
			platform.IsMapped(entry.Data, entry.Length)));
	}
}
