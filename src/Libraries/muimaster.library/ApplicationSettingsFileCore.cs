/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Bounded, guest-resident transport for the Dataspace produced by the
// application MUIM_Export walk. This is CopperOS' internal settings format;
// it is deliberately not presented as MorphOS' opaque on-disk format. The
// DOS capability owns ENV/ENVARC path resolution and the file handle, while
// this core owns validation, short-transfer handling, and cleanup.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationSettingsHeader
{
	internal const uint Size = 16;
	internal uint MagicValue;
	internal uint VersionValue;
	internal uint RecordCount;
	internal uint PayloadBytes;
}

internal enum MuiApplicationSettingsHeaderField : byte
{
	MagicValue,
	VersionValue,
	RecordCount,
	PayloadBytes,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationSettingsHeaderFieldCursor
{
	internal APTR Header;
	internal MuiApplicationSettingsHeaderField Field;
}

internal static class MuiApplicationSettingsHeaderFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationSettingsHeaderFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiApplicationSettingsHeaderField.MagicValue:
				offset = 0;
				break;
			case MuiApplicationSettingsHeaderField.VersionValue:
				offset = 4;
				break;
			case MuiApplicationSettingsHeaderField.RecordCount:
				offset = 8;
				break;
			case MuiApplicationSettingsHeaderField.PayloadBytes:
				offset = 12;
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
		APTR header, MuiApplicationSettingsHeaderField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationSettingsHeaderFieldCursor);
		cursor.Header = header;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR header, MuiApplicationSettingsHeaderField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationSettingsHeaderFieldCursor);
		cursor.Header = header;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationSettingsRecord
{
	internal const uint Size = 8;
	internal uint Key;
	internal uint Length;
}

internal enum MuiApplicationSettingsRecordField : byte
{
	Key,
	Length,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationSettingsRecordFieldCursor
{
	internal APTR Record;
	internal MuiApplicationSettingsRecordField Field;
}

internal static class MuiApplicationSettingsRecordFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationSettingsRecordFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiApplicationSettingsRecordField.Key:
				offset = 0;
				break;
			case MuiApplicationSettingsRecordField.Length:
				offset = 4;
				break;
			default:
				return false;
		}
		if (cursor.Record.IsNull || cursor.Record.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSettingsRecordField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationSettingsRecordFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSettingsRecordField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationSettingsRecordFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationSettingsHeaderCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiApplicationSettingsHeader value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationSettingsHeader.Size)) return false;
		return MuiApplicationSettingsHeaderFieldCursorCodec.TryRead(ref platform,
			address, MuiApplicationSettingsHeaderField.MagicValue,
			out value.MagicValue) &&
			MuiApplicationSettingsHeaderFieldCursorCodec.TryRead(ref platform,
				address, MuiApplicationSettingsHeaderField.VersionValue,
				out value.VersionValue) &&
			MuiApplicationSettingsHeaderFieldCursorCodec.TryRead(ref platform,
				address, MuiApplicationSettingsHeaderField.RecordCount,
				out value.RecordCount) &&
			MuiApplicationSettingsHeaderFieldCursorCodec.TryRead(ref platform,
				address, MuiApplicationSettingsHeaderField.PayloadBytes,
				out value.PayloadBytes);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationSettingsHeader value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationSettingsHeader.Size)) return false;
		return MuiApplicationSettingsHeaderFieldCursorCodec.TryWrite(ref platform,
			address, MuiApplicationSettingsHeaderField.MagicValue,
			value.MagicValue) &&
			MuiApplicationSettingsHeaderFieldCursorCodec.TryWrite(ref platform,
				address, MuiApplicationSettingsHeaderField.VersionValue,
				value.VersionValue) &&
			MuiApplicationSettingsHeaderFieldCursorCodec.TryWrite(ref platform,
				address, MuiApplicationSettingsHeaderField.RecordCount,
				value.RecordCount) &&
			MuiApplicationSettingsHeaderFieldCursorCodec.TryWrite(ref platform,
				address, MuiApplicationSettingsHeaderField.PayloadBytes,
				value.PayloadBytes);
	}
}

internal static class MuiApplicationSettingsRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiApplicationSettingsRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationSettingsRecord.Size)) return false;
		return MuiApplicationSettingsRecordFieldCursorCodec.TryRead(ref platform,
			address, MuiApplicationSettingsRecordField.Key, out value.Key) &&
			MuiApplicationSettingsRecordFieldCursorCodec.TryRead(ref platform,
				address, MuiApplicationSettingsRecordField.Length, out value.Length);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationSettingsRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationSettingsRecord.Size)) return false;
		return MuiApplicationSettingsRecordFieldCursorCodec.TryWrite(ref platform,
			address, MuiApplicationSettingsRecordField.Key, value.Key) &&
			MuiApplicationSettingsRecordFieldCursorCodec.TryWrite(ref platform,
				address, MuiApplicationSettingsRecordField.Length, value.Length);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationSettingsTransferCursor
{
	internal APTR Base;
	internal uint Offset;
}

internal static class MuiApplicationSettingsTransferCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationSettingsTransferCursor cursor, uint byteCount,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Base.Raw >
			uint.MaxValue - cursor.Offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + cursor.Offset);
		return byteCount == 0 || platform.IsMapped(address, byteCount);
	}
}

public static class MuiApplicationSettingsPacketCore
{
	internal static bool TryReadHeader<TPlatform>(ref TPlatform platform,
		APTR address, out MuiApplicationSettingsHeader packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiApplicationSettingsHeaderCodec.TryRead(ref platform, address,
			out packet);

	internal static bool WriteHeader<TPlatform>(ref TPlatform platform,
		APTR address, uint records, uint payloadBytes)
		where TPlatform : struct, IMuiGuestMemory
	{
		var value = default(MuiApplicationSettingsHeader);
		value.MagicValue = MuiApplicationSettingsFileCore.Magic;
		value.VersionValue = MuiApplicationSettingsFileCore.Version;
		value.RecordCount = records;
		value.PayloadBytes = payloadBytes;
		return MuiApplicationSettingsHeaderCodec.Write(ref platform, address,
			value);
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiApplicationSettingsRecord packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiApplicationSettingsRecordCodec.TryRead(ref platform, address,
			out packet);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, uint key, uint length)
		where TPlatform : struct, IMuiGuestMemory
	{
		var value = default(MuiApplicationSettingsRecord);
		value.Key = key;
		value.Length = length;
		return MuiApplicationSettingsRecordCodec.Write(ref platform, address,
			value);
	}

	// Narrow packet-only qualification surface.  It exposes scalar helpers
	// while keeping the guest record types and the file transport private to
	// the library implementation.
	public static bool WriteHeaderRecord<TPlatform>(ref TPlatform platform,
		APTR address, uint records, uint payloadBytes)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteHeader(ref platform, address, records, payloadBytes);

	public static bool WriteDataRecord<TPlatform>(ref TPlatform platform,
		APTR address, uint key, uint length)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, key, length);

	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		return DispatchHeaderRecord(ref platform, address) != 0 ?
			DispatchHeaderRecord(ref platform, address) :
			DispatchDataRecord(ref platform, address);
	}

	public static uint DispatchHeaderRecord<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadHeader(ref platform, address, out var header) &&
			header.MagicValue == MuiApplicationSettingsFileCore.Magic &&
			header.VersionValue == MuiApplicationSettingsFileCore.Version
			? header.RecordCount : 0;
	}

	public static uint DispatchDataRecord<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadRecord(ref platform, address, out var record) ?
			record.Key ^ record.Length : 0;
	}
}

public static class MuiApplicationSettingsFileCore
{
	public const uint Magic = 0x4D554953; // "MUIS"
	public const uint Version = 1;
	public const uint HeaderSize = 16;
	public const uint RecordSize = 8;
	public const uint MaximumRecords = 4096;
	public const uint MaximumPayloadBytes = 1_048_576;
	public const int OldFileMode = 1005;
	public const int NewFileMode = 1006;

	public static bool Save<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, APTR name)
		where TPlatform : struct, IMuiApplicationPlatform, IMuiHeadlessPlatform
	{
		var handle = platform.Open(name, NewFileMode);
		if (handle.IsNull) return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationSettingsHeader.Size);
		if (scratch.IsNull)
			return Finish(ref platform, state, handle, scratch,
				MuiApplicationSettingsHeader.Size,
				APTR.Null, APTR.Null, APTR.Null, false, false);

		var className = MuiHeadlessMemory.Allocate(ref platform, 14);
		if (className.IsNotNull) WriteClassName(ref platform, className);
		var classRecord = className.IsNull ? APTR.Null :
			MuiHeadlessObjectCore.FindClassByName(ref platform, state, className);
		var classCreated = classRecord.IsNull;
		if (classCreated)
			classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
				state, className, APTR.Null, 0, APTR.Null);
		var dataspace = classRecord.IsNull ? APTR.Null :
			MuiHeadlessObjectCore.CreateObjectA(ref platform, state, classRecord,
				APTR.Null);
		if (dataspace.IsNull)
			return Finish(ref platform, state, handle, scratch,
				MuiApplicationSettingsHeader.Size,
				APTR.Null, classRecord, className, classCreated, false);
		if (!MuiApplicationPersistenceCore.Export(ref platform, state,
			application, dataspace))
			return Finish(ref platform, state, handle, scratch,
				MuiApplicationSettingsHeader.Size,
				dataspace, classRecord, className, classCreated, false);

		if (!Measure(ref platform, state, dataspace, out var records,
			out var payloadBytes))
			return Finish(ref platform, state, handle, scratch,
				MuiApplicationSettingsHeader.Size,
				dataspace, classRecord, className, classCreated, false);
		if (!MuiApplicationSettingsPacketCore.WriteHeader(ref platform, scratch,
			records, payloadBytes) || !WriteExact(ref platform, handle, scratch,
			MuiApplicationSettingsHeader.Size))
			return Finish(ref platform, state, handle, scratch,
				MuiApplicationSettingsHeader.Size,
				dataspace, classRecord, className, classCreated, false);

		var counter = MuiHeadlessMemory.Allocate(ref platform, 4);
		if (counter.IsNull)
			return Finish(ref platform, state, handle, scratch,
				MuiApplicationSettingsHeader.Size,
				dataspace, classRecord, className, classCreated, false);
		platform.Clear(counter, 4);
		var writtenRecords = 0u;
		var writtenPayload = 0u;
		while (true)
		{
			var item = MuiStoreCore.DataspaceIterationRecord(ref platform, state,
				dataspace, counter);
			if (item.IsNull) break;
			if (!MuiStoreRecordCodec.TryRead(ref platform, item,
				out var storeRecord))
			{
				platform.Clear(counter, 4);
				platform.Free(counter, 4);
				return Finish(ref platform, state, handle, scratch,
					MuiApplicationSettingsHeader.Size,
					dataspace, classRecord, className, classCreated, false);
			}
			var length = storeRecord.Length;
			var data = storeRecord.Data;
			if (length > 65536 || (length != 0 &&
				(data.IsNull || !platform.IsMapped(data, length))))
			{
				platform.Clear(counter, 4);
				platform.Free(counter, 4);
				return Finish(ref platform, state, handle, scratch,
					MuiApplicationSettingsHeader.Size,
					dataspace, classRecord, className, classCreated, false);
			}
			if (!MuiApplicationSettingsPacketCore.WriteRecord(ref platform,
				scratch, storeRecord.Key, length) ||
				!WriteExact(ref platform, handle, scratch,
					MuiApplicationSettingsRecord.Size) ||
				!WriteExact(ref platform, handle, data, length))
			{
				platform.Clear(counter, 4);
				platform.Free(counter, 4);
				return Finish(ref platform, state, handle, scratch,
					MuiApplicationSettingsHeader.Size,
					dataspace, classRecord, className, classCreated, false);
			}
			writtenRecords++;
			writtenPayload += length;
		}
		platform.Clear(counter, 4);
		platform.Free(counter, 4);
		return Finish(ref platform, state, handle, scratch,
			MuiApplicationSettingsHeader.Size,
			dataspace, classRecord, className, classCreated,
			writtenRecords == records && writtenPayload == payloadBytes);
	}

	public static bool Load<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, APTR name)
		where TPlatform : struct, IMuiApplicationPlatform, IMuiHeadlessPlatform
	{
		var handle = platform.Open(name, OldFileMode);
		if (handle.IsNull) return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationSettingsHeader.Size);
		if (scratch.IsNull)
			return Finish(ref platform, state, handle, scratch,
				MuiApplicationSettingsHeader.Size,
				APTR.Null, APTR.Null, APTR.Null, false, false);
		if (!ReadExact(ref platform, handle, scratch,
			MuiApplicationSettingsHeader.Size) ||
			!MuiApplicationSettingsPacketCore.TryReadHeader(ref platform, scratch,
				out var header) || header.MagicValue != Magic ||
			header.VersionValue != Version)
			return Finish(ref platform, state, handle, scratch,
				MuiApplicationSettingsHeader.Size,
				APTR.Null, APTR.Null, APTR.Null, false, false);
		var records = header.RecordCount;
		var payloadBytes = header.PayloadBytes;
		if (records > MaximumRecords || payloadBytes > MaximumPayloadBytes)
			return Finish(ref platform, state, handle, scratch,
				MuiApplicationSettingsHeader.Size,
				APTR.Null, APTR.Null, APTR.Null, false, false);

		var className = MuiHeadlessMemory.Allocate(ref platform, 14);
		if (className.IsNotNull) WriteClassName(ref platform, className);
		var classRecord = className.IsNull ? APTR.Null :
			MuiHeadlessObjectCore.FindClassByName(ref platform, state, className);
		var classCreated = classRecord.IsNull;
		if (classCreated)
			classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
				state, className, APTR.Null, 0, APTR.Null);
		var dataspace = classRecord.IsNull ? APTR.Null :
			MuiHeadlessObjectCore.CreateObjectA(ref platform, state, classRecord,
				APTR.Null);
		if (dataspace.IsNull)
			return Finish(ref platform, state, handle, scratch,
				MuiApplicationSettingsHeader.Size,
				APTR.Null, classRecord, className, classCreated, false);
		MuiStoreCore.DataspaceClear(ref platform, state, dataspace);
		var loadedPayload = 0u;
		var loadedRecords = 0u;
		while (loadedRecords < records)
		{
			if (!ReadExact(ref platform, handle, scratch, RecordSize))
				return Finish(ref platform, state, handle, scratch,
					MuiApplicationSettingsHeader.Size,
					dataspace, classRecord, className, classCreated, false);
			if (!MuiApplicationSettingsPacketCore.TryReadRecord(ref platform,
				scratch, out var record))
				return Finish(ref platform, state, handle, scratch,
					MuiApplicationSettingsHeader.Size,
					dataspace, classRecord, className, classCreated, false);
			var key = record.Key;
			var length = record.Length;
			if (length > 65536 || length > payloadBytes ||
				loadedPayload > payloadBytes - length)
				return Finish(ref platform, state, handle, scratch,
					MuiApplicationSettingsHeader.Size,
					dataspace, classRecord, className, classCreated, false);
			var data = APTR.Null;
			if (length != 0)
			{
				data = MuiHeadlessMemory.Allocate(ref platform, length);
				if (data.IsNull || !ReadExact(ref platform, handle, data, length))
				{
					if (data.IsNotNull)
					{
						platform.Clear(data, length);
						platform.Free(data, length);
					}
					return Finish(ref platform, state, handle, scratch,
						MuiApplicationSettingsHeader.Size,
						dataspace, classRecord, className, classCreated, false);
				}
			}
			if (!MuiStoreCore.DataspaceAdd(ref platform, state, dataspace, key,
				data, unchecked((int)length)))
			{
				if (data.IsNotNull)
				{
					platform.Clear(data, length);
					platform.Free(data, length);
				}
				return Finish(ref platform, state, handle, scratch,
					MuiApplicationSettingsHeader.Size,
					dataspace, classRecord, className, classCreated, false);
			}
			if (data.IsNotNull)
			{
				platform.Clear(data, length);
				platform.Free(data, length);
			}
			loadedPayload += length;
			loadedRecords++;
		}
		var snapshot = MuiHeadlessObjectCore.CreateObjectA(ref platform, state,
			classRecord, APTR.Null);
		if (snapshot.IsNull)
			return Finish(ref platform, state, handle, scratch,
				MuiApplicationSettingsHeader.Size,
				dataspace, classRecord, className, classCreated, false);
		if (loadedPayload != payloadBytes ||
			!MuiApplicationPersistenceCore.ImportTransactional(ref platform, state,
				application, dataspace, snapshot))
		{
			MuiStoreCore.DataspaceClear(ref platform, state, dataspace);
			return FinishSnapshot(ref platform, state, handle, scratch,
				MuiApplicationSettingsHeader.Size,
				dataspace, snapshot, classRecord, className, classCreated, false);
		}
		return FinishSnapshot(ref platform, state, handle, scratch,
			MuiApplicationSettingsHeader.Size,
			dataspace, snapshot, classRecord, className, classCreated, true);
	}

	private static bool Measure<TPlatform>(ref TPlatform platform, APTR state,
		APTR dataspace, out uint records, out uint payloadBytes)
		where TPlatform : struct, IMuiApplicationPlatform, IMuiHeadlessPlatform
	{
		records = 0;
		payloadBytes = 0;
		var counter = MuiHeadlessMemory.Allocate(ref platform, 4);
		if (counter.IsNull) return false;
		platform.Clear(counter, 4);
		var result = true;
		while (true)
		{
			var item = MuiStoreCore.DataspaceIterationRecord(ref platform, state,
				dataspace, counter);
			if (item.IsNull) break;
			if (!MuiStoreRecordCodec.TryRead(ref platform, item,
				out var storeRecord) ||
				records >= MaximumRecords)
			{
				result = false;
				break;
			}
			var length = storeRecord.Length;
			var data = storeRecord.Data;
			if (length > 65536 || (length != 0 &&
				(data.IsNull || !platform.IsMapped(data, length))) ||
				payloadBytes > MaximumPayloadBytes -
				(length <= MaximumPayloadBytes ? length : MaximumPayloadBytes + 1))
			{
				result = false;
				break;
			}
			records++;
			payloadBytes += length;
		}
		platform.Clear(counter, 4);
		platform.Free(counter, 4);
		return result;
	}

	private static void WriteClassName<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiApplicationPlatform, IMuiHeadlessPlatform
	{
		platform.WriteUInt8(address, 0, (byte)'D');
		platform.WriteUInt8(address, 1, (byte)'a');
		platform.WriteUInt8(address, 2, (byte)'t');
		platform.WriteUInt8(address, 3, (byte)'a');
		platform.WriteUInt8(address, 4, (byte)'s');
		platform.WriteUInt8(address, 5, (byte)'p');
		platform.WriteUInt8(address, 6, (byte)'a');
		platform.WriteUInt8(address, 7, (byte)'c');
		platform.WriteUInt8(address, 8, (byte)'e');
		platform.WriteUInt8(address, 9, (byte)'.');
		platform.WriteUInt8(address, 10, (byte)'m');
		platform.WriteUInt8(address, 11, (byte)'u');
		platform.WriteUInt8(address, 12, (byte)'i');
		platform.WriteUInt8(address, 13, 0);
	}

	private static bool WriteExact<TPlatform>(ref TPlatform platform, APTR handle,
		APTR source, uint length) where TPlatform : struct, IMuiApplicationPlatform, IMuiHeadlessPlatform
	{
		var offset = 0u;
		var cursor = default(MuiApplicationSettingsTransferCursor);
		cursor.Base = source;
		while (offset < length)
		{
			cursor.Offset = offset;
			if (!MuiApplicationSettingsTransferCursorCodec.TryGetAddress(
				ref platform, cursor, length - offset, out var address)) return false;
			var result = platform.Write(handle, address, length - offset);
			if (result <= 0 || (uint)result > length - offset) return false;
			offset += (uint)result;
		}
		return true;
	}

	private static bool ReadExact<TPlatform>(ref TPlatform platform, APTR handle,
		APTR destination, uint length)
		where TPlatform : struct, IMuiApplicationPlatform, IMuiHeadlessPlatform
	{
		var offset = 0u;
		var cursor = default(MuiApplicationSettingsTransferCursor);
		cursor.Base = destination;
		while (offset < length)
		{
			cursor.Offset = offset;
			if (!MuiApplicationSettingsTransferCursorCodec.TryGetAddress(
				ref platform, cursor, length - offset, out var address)) return false;
			var result = platform.Read(handle, address, length - offset);
			if (result <= 0 || (uint)result > length - offset) return false;
			offset += (uint)result;
		}
		return true;
	}

	private static bool Finish<TPlatform>(ref TPlatform platform, APTR state,
		APTR handle, APTR scratch, uint scratchBytes, APTR dataspace,
		APTR classRecord, APTR className, bool classCreated, bool result)
		where TPlatform : struct, IMuiApplicationPlatform, IMuiHeadlessPlatform
	{
		if (scratch.IsNotNull)
		{
			platform.Clear(scratch, scratchBytes);
			platform.Free(scratch, scratchBytes);
		}
		if (dataspace.IsNotNull)
			MuiHeadlessObjectCore.DisposeObject(ref platform, state, dataspace);
		if (classCreated && classRecord.IsNotNull)
			MuiHeadlessObjectCore.DeleteClass(ref platform, state, classRecord);
		if (className.IsNotNull)
		{
			platform.Clear(className, 14);
			platform.Free(className, 14);
		}
		if (handle.IsNotNull) platform.Close(handle);
		return result;
	}

	private static bool FinishSnapshot<TPlatform>(ref TPlatform platform,
		APTR state, APTR handle, APTR scratch, uint scratchBytes, APTR dataspace,
		APTR snapshot, APTR classRecord, APTR className, bool classCreated,
		bool result)
		where TPlatform : struct, IMuiApplicationPlatform, IMuiHeadlessPlatform
	{
		if (snapshot.IsNotNull)
			MuiHeadlessObjectCore.DisposeObject(ref platform, state, snapshot);
		return Finish(ref platform, state, handle, scratch, scratchBytes,
			dataspace, classRecord, className, classCreated, result);
	}
}
