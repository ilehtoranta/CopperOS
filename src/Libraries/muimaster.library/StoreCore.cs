/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiStoreIterationCounter
{
	internal const uint Size = 4;
	internal uint Ordinal;
}

internal enum MuiStoreIterationCounterField : byte
{
	Ordinal,
}

[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiStoreIterationCounterFieldCursor
{
	internal APTR Record;
	internal MuiStoreIterationCounterField Field;
}

internal static class MuiStoreIterationCounterFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStoreIterationCounterFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Field != MuiStoreIterationCounterField.Ordinal ||
			cursor.Record.IsNull || !platform.IsMapped(cursor.Record,
				MuiStoreIterationCounter.Size)) return false;
		address = cursor.Record;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStoreIterationCounterField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStoreIterationCounterFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStoreIterationCounterField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStoreIterationCounterFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStoreIterationCounterCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStoreIterationCounter value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStoreIterationCounter.Size)) return false;
		if (!MuiStoreIterationCounterFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiStoreIterationCounterField.Ordinal,
			out value.Ordinal)) return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiStoreIterationCounter value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStoreIterationCounter.Size)) return false;
		return MuiStoreIterationCounterFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiStoreIterationCounterField.Ordinal,
			value.Ordinal);
	}
}

public static class MuiStoreCore
{
	private const uint OwnsData = 1;
	private const uint OwnsKey = 2;
	private const uint RetainsKey = 4;
	private const uint DataspaceKind = 0x100;
	private const uint DatamapKind = 0x200;
	private const uint ObjectmapKind = 0x300;
	private const uint KindMask = 0xF00;

	private enum StoreIterationField
	{
		Key,
		Data
	}

	public static bool DataspaceAdd<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint id, APTR data, int length)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		SetBlob(ref platform, state, obj, id, APTR.Null, data, length,
			DataspaceKind, false);

	// Resize an owned numeric-key dataspace without reading beyond the existing
	// guest allocation. This is used by editable controls whose contents grow
	// one byte at a time; allocation and copying stay in the guest-memory layer.
	public static bool DataspaceResize<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint id, int length)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (length < 0 || length > 65536) return false;
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (owner.IsNull) return false;
		var item = Find(ref platform, owner, id, APTR.Null, DataspaceKind);
		var created = item.IsNull;
		if (created)
		{
			item = AllocateRecord(ref platform, state, owner, id, DataspaceKind);
			if (item.IsNull) return false;
		}

		var data = APTR.Null;
		if (length != 0)
		{
			data = MuiHeadlessMemory.Allocate(ref platform, (uint)length);
			if (data.IsNull)
			{
				if (created)
				{
					UnlinkStore(ref platform, owner, item);
					FreeRecord(ref platform, item);
				}
				return false;
			}
			platform.Clear(data, (uint)length);
			var old = APTR.Null;
			var oldLength = 0u;
			if (!created)
			{
				if (!MuiStoreRecordCodec.TryRead(ref platform, item,
					out var oldRecord)) return false;
				old = oldRecord.Data;
				oldLength = oldRecord.Length;
			}
			var copyLength = oldLength < (uint)length ? oldLength : (uint)length;
			if (old.IsNotNull && copyLength != 0)
				platform.Copy(old, data, copyLength);
		}
		FreeData(ref platform, item);
		if (!MuiStoreRecordCodec.TryRead(ref platform, item,
			out var itemRecord)) return false;
		itemRecord.Data = data;
		itemRecord.Length = unchecked((uint)length);
		itemRecord.Flags |= length == 0 ? 0u : OwnsData;
		itemRecord.Generation = MuiHeadlessMemory.NextSequence(ref platform,
			state);
		if (!MuiStoreRecordCodec.Write(ref platform, item, itemRecord)) return false;
		MuiHeadlessMemory.Mutated(ref platform, state);
		return true;
	}

	public static int DataspaceLength<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint id)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		var item = owner.IsNull ? APTR.Null : Find(ref platform, owner, id,
			APTR.Null, DataspaceKind);
		if (item.IsNull) return 0;
		if (!MuiStoreRecordCodec.TryRead(ref platform, item,
			out var itemRecord)) return 0;
		var length = itemRecord.Length;
		return length > 65536 ? 65536 : unchecked((int)length);
	}

	public static bool DataspaceMerge<TPlatform>(ref TPlatform platform,
		APTR state, APTR destination, APTR source)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (destination.Raw == source.Raw) return true;
		var sourceRecord = MuiHeadlessObjectCore.FindObject(ref platform, state,
			source);
		if (sourceRecord.IsNull || MuiHeadlessObjectCore.FindObject(ref platform,
			state, destination).IsNull) return false;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, sourceRecord,
			out var sourceValue)) return false;
		var current = sourceValue.Stores;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiStoreRecordCodec.TryRead(ref platform, current,
				out var currentRecord)) return false;
			var next = currentRecord.Next;
			if ((currentRecord.Flags & KindMask) == DataspaceKind &&
				!DataspaceAdd(ref platform, state, destination, currentRecord.Key,
					currentRecord.Data, (int)currentRecord.Length)) return false;
			current = next;
		}
		return current.IsNull;
	}

	public static APTR DataspaceFind<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint id)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		var item = owner.IsNull ? APTR.Null : Find(ref platform, owner, id,
			APTR.Null, DataspaceKind);
		if (item.IsNull || !MuiStoreRecordCodec.TryRead(ref platform, item,
			out var itemRecord)) return APTR.Null;
		return itemRecord.Data;
	}

	public static APTR DataspaceGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint id, APTR sizeStorage)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		var item = owner.IsNull ? APTR.Null : Find(ref platform, owner, id,
			APTR.Null, DataspaceKind);
		if (item.IsNull) return APTR.Null;
		if (!MuiStoreRecordCodec.TryRead(ref platform, item,
			out var itemRecord)) return APTR.Null;
		if (sizeStorage.IsNotNull)
			MuiGuestUlongStorageCodec.WriteValue(ref platform, sizeStorage,
				itemRecord.Length);
		return itemRecord.Data;
	}

	// Returns one numeric-key Dataspace record in ordinal order and advances
	// the caller-owned counter. This is an internal transport primitive for
	// persistence; unlike the public MUI Datamap/Objectmap iteration methods it
	// returns the record so the codec can read key, data, and length atomically.
	public static APTR DataspaceIterationRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR counter)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Iterate(ref platform, state, obj, DataspaceKind, counter,
			StoreIterationField.Data, true);

	public static bool DatamapSet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR key, APTR data, int length, bool copyKey)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		SetBlob(ref platform, state, obj, 0, key, data, length, DatamapKind,
			copyKey);

	public static APTR DatamapFind<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR key) where TPlatform : struct, IMuiHeadlessPlatform
	{
		return DatamapGet(ref platform, state, obj, key, APTR.Null);
	}

	public static APTR DatamapGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR key, APTR sizeStorage)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		var item = owner.IsNull ? APTR.Null : Find(ref platform, owner, 0, key,
			DatamapKind);
		if (item.IsNull) return APTR.Null;
		if (!MuiStoreRecordCodec.TryRead(ref platform, item,
			out var itemRecord)) return APTR.Null;
		if (sizeStorage.IsNotNull)
			MuiGuestUlongStorageCodec.WriteValue(ref platform, sizeStorage,
				itemRecord.Length);
		return itemRecord.Data;
	}

	public static bool ObjectmapSet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR key, APTR value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		ObjectmapSet(ref platform, state, obj, key, value, false);

	public static bool ObjectmapSet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR key, APTR value, bool retainKey)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (owner.IsNull || key.IsNull) return false;
		var item = Find(ref platform, owner, 0, key, ObjectmapKind);
		if (item.IsNull)
		{
			item = AllocateRecord(ref platform, state, owner, key.Raw,
				ObjectmapKind);
			if (item.IsNull) return false;
			if (retainKey)
			{
				if (!platform.RetainObject(key))
				{
					UnlinkStore(ref platform, owner, item);
					FreeRecord(ref platform, item);
					return false;
				}
				if (!MuiStoreRecordCodec.TryRead(ref platform, item,
					out var retainedRecord)) return false;
				retainedRecord.Flags = ObjectmapKind | RetainsKey;
				if (!MuiStoreRecordCodec.Write(ref platform, item,
					retainedRecord)) return false;
			}
		}
		if (!MuiStoreRecordCodec.TryRead(ref platform, item,
			out var itemRecord)) return false;
		itemRecord.Data = value;
		itemRecord.Length = 0;
		if (!MuiStoreRecordCodec.Write(ref platform, item, itemRecord)) return false;
		MuiHeadlessMemory.Mutated(ref platform, state);
		return true;
	}

	public static APTR ObjectmapFind<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR key) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		var item = owner.IsNull ? APTR.Null : Find(ref platform, owner, 0, key,
			ObjectmapKind);
		if (item.IsNull || !MuiStoreRecordCodec.TryRead(ref platform, item,
			out var itemRecord)) return APTR.Null;
		return itemRecord.Data;
	}

	public static bool DataspaceRemove<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint id)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Remove(ref platform, state, obj, id, APTR.Null, DataspaceKind);

	public static uint DataspaceClear<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		Clear(ref platform, state, obj, DataspaceKind);

	public static bool DatamapRemove<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR key)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Remove(ref platform, state, obj, 0, key, DatamapKind);

	public static uint DatamapClear<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		Clear(ref platform, state, obj, DatamapKind);

	public static APTR DatamapIterationKey<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR counter)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Iterate(ref platform, state, obj, DatamapKind, counter,
			StoreIterationField.Key);

	public static APTR DatamapIterate<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR counter)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Iterate(ref platform, state, obj, DatamapKind, counter,
			StoreIterationField.Data);

	public static bool ObjectmapRemove<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR key)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Remove(ref platform, state, obj, 0, key, ObjectmapKind);

	public static uint ObjectmapClear<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		Clear(ref platform, state, obj, ObjectmapKind);

	public static APTR ObjectmapIterationKey<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR counter)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Iterate(ref platform, state, obj, ObjectmapKind, counter,
			StoreIterationField.Key);

	public static APTR ObjectmapIterate<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR counter)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Iterate(ref platform, state, obj, ObjectmapKind, counter,
			StoreIterationField.Data);

	public static bool Remove<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint numericKey, APTR pointerKey, uint kind)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (owner.IsNull) return false;
		var item = Find(ref platform, owner, numericKey, pointerKey, kind);
		if (item.IsNull || !UnlinkStore(ref platform, owner, item))
			return false;
		FreeRecord(ref platform, item);
		MuiHeadlessMemory.Mutated(ref platform, state);
		return true;
	}

	private static APTR Iterate<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint kind, APTR counter, StoreIterationField resultField,
		bool returnRecord = false)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (owner.IsNull || !MuiStoreIterationCounterCodec.TryRead(ref platform,
			counter, out var counterValue))
			return APTR.Null;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var ownerValue)) return APTR.Null;
		var ordinal = counterValue.Ordinal;
		var current = ownerValue.Stores;
		uint matched = 0;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiStoreRecordCodec.TryRead(ref platform, current,
				out var currentRecord)) return APTR.Null;
			if ((currentRecord.Flags & KindMask) == kind)
			{
				if (matched == ordinal)
				{
					counterValue.Ordinal = ordinal + 1;
					if (!MuiStoreIterationCounterCodec.Write(ref platform, counter,
						counterValue)) return APTR.Null;
					if (returnRecord) return current;
					return resultField == StoreIterationField.Key ?
						APTR.FromPointer(currentRecord.Key) : currentRecord.Data;
				}
				matched++;
			}
			current = currentRecord.Next;
		}
		return APTR.Null;
	}

	public static uint Clear<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint kind) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (owner.IsNull) return 0;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var ownerValue)) return 0;
		uint removed = 0;
		var current = ownerValue.Stores;
		var previous = APTR.Null;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiStoreRecordCodec.TryRead(ref platform, current,
				out var currentRecord)) return 0;
			var next = currentRecord.Next;
			if ((currentRecord.Flags & KindMask) == kind)
			{
				if (previous.IsNull) ownerValue.Stores = next;
				else
				{
					if (!MuiStoreRecordCodec.TryRead(ref platform, previous,
						out var previousRecord)) return 0;
					previousRecord.Next = next;
					if (!MuiStoreRecordCodec.Write(ref platform, previous,
						previousRecord)) return 0;
				}
				FreeRecord(ref platform, current);
				removed++;
			}
			else previous = current;
			current = next;
		}
		if (removed != 0)
		{
			if (!MuiHeadlessObjectCodec.Write(ref platform, owner,
				ownerValue)) return 0;
			MuiHeadlessMemory.Mutated(ref platform, state);
		}
		return removed;
	}

	internal static void ClearAll<TPlatform>(ref TPlatform platform, APTR owner)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var ownerValue)) return;
		var current = ownerValue.Stores;
		ownerValue.Stores = APTR.Null;
		if (!MuiHeadlessObjectCodec.Write(ref platform, owner,
			ownerValue)) return;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiStoreRecordCodec.TryRead(ref platform, current,
				out var currentRecord)) return;
			var next = currentRecord.Next;
			FreeRecord(ref platform, current);
			current = next;
		}
	}

	private static bool SetBlob<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint numericKey, APTR pointerKey, APTR data, int length,
		uint kind, bool copyKey) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (length < 0 || (length != 0 && data.IsNull)) return false;
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (owner.IsNull || (kind == DatamapKind && pointerKey.IsNull)) return false;
		var item = Find(ref platform, owner, numericKey, pointerKey, kind);
		var created = item.IsNull;
		if (created)
		{
			item = AllocateRecord(ref platform, state, owner,
				kind == DataspaceKind ? numericKey : pointerKey.Raw, kind);
			if (item.IsNull) return false;
			if (kind == DatamapKind && copyKey)
			{
				var keyCopy = CopyString(ref platform, pointerKey);
				if (keyCopy.IsNull)
				{
					UnlinkStore(ref platform, owner, item);
					FreeRecord(ref platform, item);
					return false;
				}
				if (!MuiStoreRecordCodec.TryRead(ref platform, item,
					out var copiedKeyRecord)) return false;
				copiedKeyRecord.Key = keyCopy.Raw;
				copiedKeyRecord.Flags = kind | OwnsKey;
				if (!MuiStoreRecordCodec.Write(ref platform, item,
					copiedKeyRecord)) return false;
			}
		}
		var dataCopy = APTR.Null;
		if (length != 0)
		{
			dataCopy = MuiHeadlessMemory.Allocate(ref platform, (uint)length);
			if (dataCopy.IsNull)
			{
				if (created)
				{
					UnlinkStore(ref platform, owner, item);
					FreeRecord(ref platform, item);
				}
				return false;
			}
			platform.Copy(data, dataCopy, (uint)length);
		}
		FreeData(ref platform, item);
		if (!MuiStoreRecordCodec.TryRead(ref platform, item,
			out var itemRecord)) return false;
		itemRecord.Data = dataCopy;
		itemRecord.Length = unchecked((uint)length);
		itemRecord.Flags |= length == 0 ? 0u : OwnsData;
		itemRecord.Generation = MuiHeadlessMemory.NextSequence(ref platform,
			state);
		if (!MuiStoreRecordCodec.Write(ref platform, item, itemRecord)) return false;
		MuiHeadlessMemory.Mutated(ref platform, state);
		return true;
	}

	private static APTR AllocateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, uint key, uint kind)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var item = MuiHeadlessMemory.Allocate(ref platform,
			MuiStoreRecord.Size);
		if (item.IsNull) return APTR.Null;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var ownerValue))
		{
			FreeRecord(ref platform, item);
			return APTR.Null;
		}
		MuiStoreRecord itemRecord = default;
		itemRecord.Next = ownerValue.Stores;
		itemRecord.Key = key;
		itemRecord.Flags = kind;
		itemRecord.Generation = MuiHeadlessMemory.NextSequence(ref platform,
			state);
		if (!MuiStoreRecordCodec.Write(ref platform, item, itemRecord))
		{
			FreeRecord(ref platform, item);
			return APTR.Null;
		}
		ownerValue.Stores = item;
		if (!MuiHeadlessObjectCodec.Write(ref platform, owner, ownerValue))
		{
			FreeRecord(ref platform, item);
			return APTR.Null;
		}
		return item;
	}

	private static bool UnlinkStore<TPlatform>(ref TPlatform platform,
		APTR owner, APTR target) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var ownerValue)) return false;
		var current = ownerValue.Stores;
		var previous = APTR.Null;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiStoreRecordCodec.TryRead(ref platform, current,
				out var currentRecord)) return false;
			var next = currentRecord.Next;
			if (current.Raw == target.Raw)
			{
				if (previous.IsNull)
				{
					ownerValue.Stores = next;
					return MuiHeadlessObjectCodec.Write(ref platform, owner,
						ownerValue);
				}
				if (!MuiStoreRecordCodec.TryRead(ref platform, previous,
					out var previousRecord)) return false;
				previousRecord.Next = next;
				return MuiStoreRecordCodec.Write(ref platform, previous,
					previousRecord);
			}
			previous = current;
			current = next;
		}
		return false;
	}

	private static APTR Find<TPlatform>(ref TPlatform platform, APTR owner,
		uint numericKey, APTR pointerKey, uint kind)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var ownerValue)) return APTR.Null;
		var current = ownerValue.Stores;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiStoreRecordCodec.TryRead(ref platform, current,
				out var currentRecord)) return APTR.Null;
			if ((currentRecord.Flags & KindMask) == kind)
			{
				var key = currentRecord.Key;
				if (kind == DataspaceKind && key == numericKey) return current;
				if (kind == ObjectmapKind && key == pointerKey.Raw) return current;
				if (kind == DatamapKind && CStringCodec.TryEquals(ref platform,
					APTR.FromPointer(key), pointerKey, 4096, out var equal) && equal)
					return current;
			}
			current = currentRecord.Next;
		}
		return APTR.Null;
	}

	private static APTR CopyString<TPlatform>(ref TPlatform platform, APTR source)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!CStringCodec.TryReadLength(ref platform, source, 4096,
				out var length))
			return APTR.Null;
		var byteSize = length + 1;
		var copy = MuiHeadlessMemory.Allocate(ref platform, byteSize);
		if (copy.IsNotNull) platform.Copy(source, copy, byteSize);
		return copy;
	}

	private static void FreeData<TPlatform>(ref TPlatform platform, APTR item)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiStoreRecordCodec.TryRead(ref platform, item,
			out var itemRecord)) return;
		var flags = itemRecord.Flags;
		if ((flags & OwnsData) == 0) return;
		var data = itemRecord.Data;
		var length = itemRecord.Length;
		if (data.IsNotNull)
		{
			platform.Clear(data, length);
			platform.Free(data, length);
		}
		itemRecord.Data = APTR.Null;
		itemRecord.Length = 0;
		itemRecord.Flags = flags & ~OwnsData;
		MuiStoreRecordCodec.Write(ref platform, item, itemRecord);
	}

	private static void FreeRecord<TPlatform>(ref TPlatform platform, APTR item)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiStoreRecordCodec.TryRead(ref platform, item,
			out var itemRecord)) return;
		var flags = itemRecord.Flags;
		FreeData(ref platform, item);
		if ((flags & OwnsKey) != 0)
		{
			var key = APTR.FromPointer(itemRecord.Key);
			uint length = 1;
			while (length < 4096 && platform.ReadUInt8(key, (int)(length - 1)) != 0)
				length++;
			platform.Clear(key, length);
			platform.Free(key, length);
		}
		if ((flags & RetainsKey) != 0)
		{
			var key = APTR.FromPointer(itemRecord.Key);
			platform.ReleaseObject(key);
		}
		platform.Clear(item, MuiStoreRecord.Size);
		platform.Free(item, MuiStoreRecord.Size);
	}
}

// Scalar qualification surface for the object-owned Store/Dataspace link.
// The live StoreCore path uses the same named object codec; this seam proves
// the Stores head without exposing a managed map or iteration object.
public static class MuiStoreObjectRecordPacketCore
{
	public static bool WriteStores<TPlatform>(ref TPlatform platform,
		APTR address, APTR stores) where TPlatform : struct, IMuiGuestMemory
	{
		MuiHeadlessObjectRecord record = default;
		record.Stores = stores;
		return MuiHeadlessObjectCodec.Write(ref platform, address, record);
	}

	public static APTR DispatchStores<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		return MuiHeadlessObjectCodec.TryRead(ref platform, address,
			out var record) ? record.Stores : APTR.Null;
	}
}
