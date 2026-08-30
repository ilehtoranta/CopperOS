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
	internal const uint FieldSize = 4;
	internal const uint OrdinalOffset = 0;
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

// Struct-first guest-memory adapter for the caller-owned iteration counter.
internal static class MuiStoreIterationCounterMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiStoreIterationCounterField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (field != MuiStoreIterationCounterField.Ordinal || record.IsNull ||
			!platform.IsMapped(record,
				MuiStoreIterationCounter.Size)) return false;
		address = APTR.FromPointer(record.Raw +
			MuiStoreIterationCounter.OrdinalOffset);
		return platform.IsMapped(address, MuiStoreIterationCounter.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStoreIterationCounterField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStoreIterationCounterField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for typed cursor callers; production access
// routes through MuiStoreIterationCounterMemoryCodec.
internal static class MuiStoreIterationCounterFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStoreIterationCounterFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiStoreIterationCounterMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStoreIterationCounterField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiStoreIterationCounterMemoryCodec.TryReadUInt32(ref platform, record,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStoreIterationCounterField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiStoreIterationCounterMemoryCodec.TryWriteUInt32(ref platform, record,
			field, value);
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
		if (!MuiStoreIterationCounterMemoryCodec.TryReadUInt32(
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
		return MuiStoreIterationCounterMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiStoreIterationCounterField.Ordinal,
			value.Ordinal);
	}
}

public static class MuiStoreCore
{
	private const uint OwnsData = 1;
	private const uint OwnsKey = 2;
	private const uint RetainsKey = 4;
	private const uint OwnsObject = 8;
	// High flag bits record allocator provenance without changing the fixed
	// 24-byte MuiStoreRecord layout. They are consumed only by the store
	// lifetime helpers; the low kind/ownership bits remain ABI-compatible.
	private const uint DataUsesPool = 0x80000000u;
	private const uint KeyUsesPool = 0x40000000u;
	private const uint RecordUsesPool = 0x20000000u;
	private const uint DataspaceKind = 0x100;
	private const uint DatamapKind = 0x200;
	private const uint ObjectmapKind = 0x300;
	private const uint KindMask = 0xF00;

	private enum StoreIterationField
	{
		Key,
		Data
	}

	private static APTR AllocateStoreMemory<TPlatform>(ref TPlatform platform,
		APTR pool, uint size) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (size == 0) return APTR.Null;
		var result = pool.IsNotNull
			? platform.AllocPooled(pool, size)
			: MuiHeadlessMemory.Allocate(ref platform, size);
		if (result.IsNull || !platform.IsMapped(result, size))
		{
			if (result.IsNotNull)
			{
				if (pool.IsNotNull) platform.FreePooled(pool, result, size);
				else platform.Free(result, size);
			}
			return APTR.Null;
		}
		platform.Clear(result, size);
		return result;
	}

	private static void FreeStoreMemory<TPlatform>(ref TPlatform platform,
		APTR pool, APTR address, uint size, bool usesPool)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (address.IsNull) return;
		platform.Clear(address, size);
		if (usesPool && pool.IsNotNull) platform.FreePooled(pool, address, size);
		else platform.Free(address, size);
	}

	private static MuiStorePolicyKind PolicyForOwner<TPlatform>(
		ref TPlatform platform, APTR state, APTR owner)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var objectValue)) return MuiStorePolicyKind.None;
		var cls = MuiCommonControlCore.Classify(ref platform, state,
			objectValue.Boopsi);
		return cls == MuiControlClass.Dataspace
			? MuiStorePolicyKind.Dataspace
			: cls == MuiControlClass.Datamap
				? MuiStorePolicyKind.Datamap
				: cls == MuiControlClass.Objectmap
					? MuiStorePolicyKind.Objectmap
					: MuiStorePolicyKind.None;
	}

	private static APTR CreateOwnedStorePool<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, MuiStorePolicyKind policy)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var pool = platform.CreatePool(0, MuiStorePolicyCore.DefaultPoolPuddleSize,
			MuiStorePolicyCore.DefaultPoolThreshold);
		if (pool.IsNull) return APTR.Null;
		var stateAddress = MuiHeadlessMemory.Allocate(ref platform,
			MuiStorePoolStateRecord.Size);
		if (stateAddress.IsNull)
		{
			platform.DeletePool(pool);
			return APTR.Null;
		}
		MuiStorePoolStateRecord stateValue = default;
		stateValue.Pool = pool;
		stateValue.Policy = (uint)policy;
		stateValue.OwnsPool = 1;
		stateValue.Magic = MuiStorePoolStateRecord.MagicValue;
		if (!MuiStorePoolStateCodec.Write(ref platform, stateAddress,
			stateValue) || !MuiHeadlessObjectCore.SetRecordAttributeRaw(
			ref platform, state, owner, MuiStorePolicyCore.PoolStateAttribute,
			stateAddress.Raw, false))
		{
			platform.Clear(stateAddress, MuiStorePoolStateRecord.Size);
			platform.Free(stateAddress, MuiStorePoolStateRecord.Size);
			platform.DeletePool(pool);
			return APTR.Null;
		}
		return pool;
	}

	// MorphOS creates the store's private Exec pool when no external pool was
	// supplied. Keep that lifecycle at construction so an empty store still has
	// the same ownership boundary as a populated one, and make disposal able to
	// recover it from the guest-resident named state record.
	internal static bool InitializeStorePool<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var policy = PolicyForOwner(ref platform, state, owner);
		if (policy == MuiStorePolicyKind.None) return true;
		var kind = policy == MuiStorePolicyKind.Dataspace ? DataspaceKind :
			policy == MuiStorePolicyKind.Datamap ? DatamapKind : ObjectmapKind;
		return ResolveStorePool(ref platform, state, owner, kind).IsNotNull;
	}

	private static APTR ResolveStorePool<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, uint kind, bool createDefault = true)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var objectValue)) return APTR.Null;
		var policy = MuiStorePolicyCore.PolicyForStoreKind(kind);
		if (policy == MuiStorePolicyKind.None || !MuiStorePolicyCore.IsClassCompatible(
			ref platform, state, objectValue.Boopsi, policy)) return APTR.Null;
		if (MuiStorePolicyCore.TryReadPool(ref platform, state, objectValue.Boopsi,
			policy, out var external) && external.Pool.IsNotNull)
			return external.Pool;
		if (MuiStorePolicyCore.TryReadOwnedPoolState(ref platform, state,
			objectValue.Boopsi, policy, out _, out var owned)) return owned.Pool;
		// Unknown compatibility objects may still use an explicitly supplied
		// pool, but only the recognized MorphOS store classes create an owned
		// default pool that can be recovered during disposal.
		if (!createDefault || PolicyForOwner(ref platform, state, owner) != policy)
			return APTR.Null;
		return CreateOwnedStorePool(ref platform, state, owner, policy);
	}

	private static APTR ResolveAnyStorePool<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, bool createDefault = true)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var policy = PolicyForOwner(ref platform, state, owner);
		return policy == MuiStorePolicyKind.None ? APTR.Null :
			ResolveStorePool(ref platform, state, owner,
				policy == MuiStorePolicyKind.Dataspace ? DataspaceKind :
				policy == MuiStorePolicyKind.Datamap ? DatamapKind : ObjectmapKind,
				createDefault);
	}

	private static void ReleaseOwnedStorePool<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, MuiStorePolicyKind policy)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var objectValue)) return;
		if (policy == MuiStorePolicyKind.None ||
			!MuiStorePolicyCore.TryReadOwnedPoolState(ref platform, state,
				objectValue.Boopsi,
				policy, out var stateAddress, out var stateValue)) return;
		platform.DeletePool(stateValue.Pool);
		platform.Clear(stateAddress, MuiStorePoolStateRecord.Size);
		platform.Free(stateAddress, MuiStorePoolStateRecord.Size);
		MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, owner,
			MuiStorePolicyCore.PoolStateAttribute, 0, false);
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
		var pool = ResolveAnyStorePool(ref platform, state, owner);
		var item = Find(ref platform, owner, id, APTR.Null, DataspaceKind);
		var created = item.IsNull;
		if (created)
		{
			item = AllocateRecord(ref platform, state, owner, id, DataspaceKind,
				pool);
			if (item.IsNull) return false;
		}

		var data = APTR.Null;
		if (length != 0)
		{
			data = AllocateStoreMemory(ref platform, pool, (uint)length);
			if (data.IsNull)
			{
				if (created)
				{
					UnlinkStore(ref platform, owner, item);
					FreeRecord(ref platform, item, pool);
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
		FreeData(ref platform, item, pool);
		if (!MuiStoreRecordCodec.TryRead(ref platform, item,
			out var itemRecord)) return false;
		itemRecord.Data = data;
		itemRecord.Length = unchecked((uint)length);
		itemRecord.Flags |= length == 0 ? 0u : OwnsData |
			(pool.IsNotNull ? DataUsesPool : 0u);
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

	// MorphOS MUIA_Dataspace_Count is a getter-only ULONG. Count only the
	// numeric-key Dataspace records owned by this object. The walk consumes the
	// named MuiHeadlessObjectRecord and MuiStoreRecord codecs and fails closed
	// on an invalid link instead of exposing a partial count.
	public static uint DataspaceCount<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryGetCount(ref platform, state, obj, DataspaceKind,
			out var count) ?
			count : 0;
	}

	public static bool TryGetDataspaceCount<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out uint count)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryGetCount(ref platform, state, obj, DataspaceKind, out count);
	}

	public static uint DatamapCount<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryGetDatamapCount(ref platform, state, obj, out var count) ?
			count : 0;
	}

	public static bool TryGetDatamapCount<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out uint count)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryGetCount(ref platform, state, obj, DatamapKind, out count);
	}

	private static bool TryGetCount<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint kind, out uint count)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		count = 0;
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (owner.IsNull || !MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var ownerValue)) return false;
		var current = ownerValue.Stores;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiStoreRecordCodec.TryRead(ref platform, current,
				out var currentRecord)) return false;
			if ((currentRecord.Flags & KindMask) == kind)
			{
				if (count == uint.MaxValue) return false;
				count++;
			}
			current = currentRecord.Next;
		}
		if (current.IsNotNull) { count = 0; return false; }
		return true;
	}

	// MorphOS returns the number of entries added or replaced by Merge, rather
	// than a boolean success flag. Keep the operation's failure status separate
	// from its count so the dispatcher can expose the documented ULONG result
	// without introducing a managed exception or side table.
	public static bool TryDataspaceMerge<TPlatform>(ref TPlatform platform,
		APTR state, APTR destination, APTR source, out uint mergedCount)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		mergedCount = 0;
		if (destination.Raw == source.Raw) return true;
		var sourceRecord = MuiHeadlessObjectCore.FindObject(ref platform, state,
			source);
		var destinationRecord = MuiHeadlessObjectCore.FindObject(ref platform,
			state, destination);
		if (sourceRecord.IsNull || destinationRecord.IsNull) return false;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, sourceRecord,
			out var sourceValue)) return false;
		var current = sourceValue.Stores;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiStoreRecordCodec.TryRead(ref platform, current,
				out var currentRecord)) return false;
			var next = currentRecord.Next;
			if ((currentRecord.Flags & KindMask) == DataspaceKind)
			{
				if (!DataspaceAdd(ref platform, state, destination,
					currentRecord.Key, currentRecord.Data,
					(int)currentRecord.Length)) return false;
				if (mergedCount == uint.MaxValue) return false;
				mergedCount++;
			}
			current = next;
		}
		return current.IsNull;
	}

	public static uint DataspaceMergeCount<TPlatform>(ref TPlatform platform,
		APTR state, APTR destination, APTR source)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryDataspaceMerge(ref platform, state, destination, source,
			out var mergedCount) ? mergedCount : 0;

	// Source-compatibility helper for existing callers that only need the
	// historical boolean result. New ABI dispatch uses DataspaceMergeCount.
	public static bool DataspaceMerge<TPlatform>(ref TPlatform platform,
		APTR state, APTR destination, APTR source)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryDataspaceMerge(ref platform, state, destination, source,
			out _);

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
		APTR obj, APTR key, APTR value, bool copyKey)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (owner.IsNull || key.IsNull) return false;
		var pool = ResolveStorePool(ref platform, state, owner, ObjectmapKind);
		var item = Find(ref platform, owner, 0, key, ObjectmapKind);
		if (item.IsNull)
		{
			item = AllocateRecord(ref platform, state, owner, key.Raw,
				ObjectmapKind, pool);
			if (item.IsNull) return false;
			if (copyKey)
			{
				var keyCopy = CopyString(ref platform, key, pool);
				if (keyCopy.IsNull)
				{
					UnlinkStore(ref platform, owner, item);
					FreeRecord(ref platform, item, pool);
					return false;
				}
				if (!MuiStoreRecordCodec.TryRead(ref platform, item,
					out var copiedKeyRecord)) return false;
				copiedKeyRecord.Key = keyCopy.Raw;
				copiedKeyRecord.Flags = (copiedKeyRecord.Flags & RecordUsesPool) |
					ObjectmapKind | OwnsKey |
					(pool.IsNotNull ? KeyUsesPool : 0u);
				if (!MuiStoreRecordCodec.Write(ref platform, item,
					copiedKeyRecord)) return false;
			}
			if (!OrderStoreRecord(ref platform, owner, item, ObjectmapKind))
			{
				UnlinkStore(ref platform, owner, item);
				FreeRecord(ref platform, item, pool);
				return false;
			}
		}
		if (!MuiStoreRecordCodec.TryRead(ref platform, item,
			out var itemRecord)) return false;
		var previous = itemRecord.Data;
		if (previous.IsNotNull && previous.Raw != value.Raw &&
			!DisposeOwnedObject(ref platform, state, owner, previous))
			return false;
		itemRecord.Data = value;
		itemRecord.Length = 0;
		itemRecord.Flags = (itemRecord.Flags &
			~OwnsObject) | (value.IsNotNull ? OwnsObject : 0u);
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
		CurrentIterationKey(ref platform, state, obj, DatamapKind, counter);

	public static APTR DatamapIterate<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR counter)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		IterateStringMap(ref platform, state, obj, DatamapKind, counter);

	public static bool ObjectmapRemove<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR key)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		ObjectmapRemoveObject(ref platform, state, obj, key).IsNotNull;

	// MorphOS returns the removed object and transfers ownership to the caller.
	// Keep this separate from the historical boolean convenience wrapper above
	// so existing C# callers can continue to test success without hiding the
	// ABI result used by the live dispatcher.
	public static APTR ObjectmapRemoveObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR key)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (owner.IsNull) return APTR.Null;
		var item = Find(ref platform, owner, 0, key, ObjectmapKind);
		if (item.IsNull || !MuiStoreRecordCodec.TryRead(ref platform, item,
			out var itemRecord) ||
			!AdvanceIterationStatesAfterRemoval(ref platform, state, owner, item,
				itemRecord) || !UnlinkStore(ref platform, owner, item))
			return APTR.Null;
		var value = itemRecord.Data;
		FreeRecord(ref platform, item,
			ResolveStorePool(ref platform, state, owner, ObjectmapKind));
		MuiHeadlessMemory.Mutated(ref platform, state);
		return value;
	}

	public static uint ObjectmapClear<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		Clear(ref platform, state, obj, ObjectmapKind);

	public static APTR ObjectmapIterationKey<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR counter)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		CurrentIterationKey(ref platform, state, obj, ObjectmapKind, counter);

	public static APTR ObjectmapIterate<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR counter)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		IterateStringMap(ref platform, state, obj, ObjectmapKind, counter);

	// MorphOS callers obtain the current key after Iterate has returned the
	// current value. Keep that current record in guest-resident iterator state;
	// an ordinal scan would skip entries after the current one is removed.
	private static APTR CurrentIterationKey<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint kind, APTR counter)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiStoreIterationCounterCodec.TryRead(ref platform, counter,
			out var counterValue) || counterValue.Ordinal == 0) return APTR.Null;
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (owner.IsNull || !TryFindIterationState(ref platform, state, owner,
			counter, kind, out var iterationAddress, out var iterationValue) ||
			iterationAddress.IsNull || iterationValue.Current.IsNull ||
			!MuiStoreRecordCodec.TryRead(ref platform, iterationValue.Current,
				out var currentRecord) ||
			(currentRecord.Flags & KindMask) != kind) return APTR.Null;
		return APTR.FromPointer(currentRecord.Key);
	}

	public static bool Remove<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint numericKey, APTR pointerKey, uint kind)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (owner.IsNull) return false;
		var item = Find(ref platform, owner, numericKey, pointerKey, kind);
		if (item.IsNull || !MuiStoreRecordCodec.TryRead(ref platform, item,
			out var itemRecord) ||
			!AdvanceIterationStatesAfterRemoval(ref platform, state, owner, item,
				itemRecord) || !UnlinkStore(ref platform, owner, item))
			return false;
		FreeRecord(ref platform, item,
			ResolveStorePool(ref platform, state, owner, kind));
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

	private static bool FindStringMapRecord<TPlatform>(ref TPlatform platform,
		APTR start, uint kind, out APTR record)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		record = APTR.Null;
		var current = start;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiStoreRecordCodec.TryRead(ref platform, current,
				out var currentRecord)) return false;
			if ((currentRecord.Flags & KindMask) == kind)
			{
				record = current;
				return true;
			}
			current = currentRecord.Next;
		}
		return current.IsNull;
	}

	private static bool TryFindIterationState<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, APTR counter, uint kind, out APTR address,
		out MuiStoreIterationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		address = APTR.Null;
		value = default;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var ownerValue)) return false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			ownerValue.Boopsi, MuiStorePolicyCore.IterationStateAttribute,
			out var rawHead) || rawHead == 0) return true;
		var current = APTR.FromPointer(rawHead);
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiStoreIterationStateCodec.TryRead(ref platform, current,
				out var currentValue)) return false;
			if (currentValue.Magic == MuiStoreIterationStateRecord.MagicValue &&
				currentValue.Counter.Raw == counter.Raw &&
				currentValue.Kind == kind)
			{
				address = current;
				value = currentValue;
				return true;
			}
			current = currentValue.Next;
		}
		return current.IsNull;
	}

	private static bool CreateIterationState<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, APTR counter, uint kind, out APTR address,
		out MuiStoreIterationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		address = APTR.Null;
		value = default;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var ownerValue)) return false;
		uint rawHead = 0;
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			ownerValue.Boopsi, MuiStorePolicyCore.IterationStateAttribute,
			out rawHead);
		var block = MuiHeadlessMemory.Allocate(ref platform,
			MuiStoreIterationStateRecord.Size);
		if (block.IsNull) return false;
		value.Next = APTR.FromPointer(rawHead);
		value.Counter = counter;
		value.Kind = kind;
		value.Magic = MuiStoreIterationStateRecord.MagicValue;
		if (!MuiStoreIterationStateCodec.Write(ref platform, block, value) ||
			!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, owner,
				MuiStorePolicyCore.IterationStateAttribute, block.Raw, false))
		{
			platform.Clear(block, MuiStoreIterationStateRecord.Size);
			platform.Free(block, MuiStoreIterationStateRecord.Size);
			return false;
		}
		address = block;
		return true;
	}

	private static bool FindOrCreateIterationState<TPlatform>(
		ref TPlatform platform, APTR state, APTR owner, APTR counter,
		uint kind, uint ordinal, out APTR address,
		out MuiStoreIterationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryFindIterationState(ref platform, state, owner, counter, kind,
			out address, out value)) return false;
		if (address.IsNotNull)
		{
			if (ordinal == 0)
			{
				value.Current = APTR.Null;
				value.NextRecord = APTR.Null;
				if (!MuiStoreIterationStateCodec.Write(ref platform, address, value))
					return false;
			}
			return true;
		}
		if (ordinal != 0) return false;
		return CreateIterationState(ref platform, state, owner, counter, kind,
			out address, out value);
	}

	private static APTR IterateStringMap<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint kind, APTR counter)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (owner.IsNull || !MuiStoreIterationCounterCodec.TryRead(ref platform,
			counter, out var counterValue) ||
			!MuiHeadlessObjectCodec.TryRead(ref platform, owner,
				out var ownerValue)) return APTR.Null;
		if (!FindOrCreateIterationState(ref platform, state, owner, counter, kind,
			counterValue.Ordinal, out var iterationAddress,
			out var iterationValue) || iterationAddress.IsNull) return APTR.Null;
		var start = counterValue.Ordinal == 0 ? ownerValue.Stores :
			iterationValue.NextRecord;
		if (!FindStringMapRecord(ref platform, start, kind, out var current))
			return APTR.Null;
		if (current.IsNull)
		{
			iterationValue.Current = APTR.Null;
			iterationValue.NextRecord = APTR.Null;
			MuiStoreIterationStateCodec.Write(ref platform, iterationAddress,
				iterationValue);
			return APTR.Null;
		}
		if (!MuiStoreRecordCodec.TryRead(ref platform, current,
			out var currentRecord) ||
			!FindStringMapRecord(ref platform, currentRecord.Next, kind,
				out var nextRecord)) return APTR.Null;
		iterationValue.Current = current;
		iterationValue.NextRecord = nextRecord;
		if (!MuiStoreIterationStateCodec.Write(ref platform, iterationAddress,
			iterationValue)) return APTR.Null;
		counterValue.Ordinal = 1;
		if (!MuiStoreIterationCounterCodec.Write(ref platform, counter,
			counterValue)) return APTR.Null;
		return currentRecord.Data;
	}

	private static bool AdvanceIterationStatesAfterRemoval<TPlatform>(
		ref TPlatform platform, APTR state, APTR owner, APTR item,
		MuiStoreRecord itemRecord)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var ownerValue)) return false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			ownerValue.Boopsi, MuiStorePolicyCore.IterationStateAttribute,
			out var rawHead) || rawHead == 0) return true;
		var current = APTR.FromPointer(rawHead);
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiStoreIterationStateCodec.TryRead(ref platform, current,
				out var value)) return false;
			if ((value.Kind == DatamapKind || value.Kind == ObjectmapKind) &&
				(value.Current.Raw == item.Raw || value.NextRecord.Raw == item.Raw))
			{
				if (value.Current.Raw == item.Raw) value.Current = APTR.Null;
				if (value.NextRecord.Raw == item.Raw &&
					!FindStringMapRecord(ref platform, itemRecord.Next, value.Kind,
						out value.NextRecord)) return false;
				if (!MuiStoreIterationStateCodec.Write(ref platform, current, value))
					return false;
			}
			current = value.Next;
		}
		return current.IsNull;
	}

	private static bool ClearIterationStates<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, uint kind)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var ownerValue)) return false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			ownerValue.Boopsi, MuiStorePolicyCore.IterationStateAttribute,
			out var rawHead) || rawHead == 0) return true;
		var current = APTR.FromPointer(rawHead);
		var previous = APTR.Null;
		var newHead = current;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiStoreIterationStateCodec.TryRead(ref platform, current,
				out var value)) return false;
			var next = value.Next;
			if (kind == 0 || value.Kind == kind)
			{
				if (previous.IsNull) newHead = next;
				else
				{
					if (!MuiStoreIterationStateCodec.TryRead(ref platform, previous,
						out var previousValue)) return false;
					previousValue.Next = next;
					if (!MuiStoreIterationStateCodec.Write(ref platform, previous,
						previousValue)) return false;
				}
				platform.Clear(current, MuiStoreIterationStateRecord.Size);
				platform.Free(current, MuiStoreIterationStateRecord.Size);
			}
			else previous = current;
			current = next;
		}
		if (!current.IsNull) return false;
		return MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			owner, MuiStorePolicyCore.IterationStateAttribute, newHead.Raw, false);
	}

	public static uint Clear<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint kind) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (owner.IsNull) return 0;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var ownerValue)) return 0;
		if (!ClearIterationStates(ref platform, state, owner, kind)) return 0;
		var pool = ResolveStorePool(ref platform, state, owner, kind, false);
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
				if (kind == ObjectmapKind && currentRecord.Data.IsNotNull &&
					!DisposeOwnedObject(ref platform, state, owner,
						currentRecord.Data)) return 0;
				FreeRecord(ref platform, current, pool);
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

	internal static void ClearAll<TPlatform>(ref TPlatform platform, APTR state,
		APTR owner)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var ownerValue)) return;
		if (!ClearIterationStates(ref platform, state, owner, 0)) return;
		var current = ownerValue.Stores;
		ownerValue.Stores = APTR.Null;
		if (!MuiHeadlessObjectCodec.Write(ref platform, owner,
			ownerValue)) return;
		var policy = PolicyForOwner(ref platform, state, owner);
		var pool = ResolveAnyStorePool(ref platform, state, owner, false);
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiStoreRecordCodec.TryRead(ref platform, current,
				out var currentRecord)) return;
			var next = currentRecord.Next;
			if ((currentRecord.Flags & KindMask) == ObjectmapKind &&
				currentRecord.Data.IsNotNull)
				DisposeOwnedObject(ref platform, state, owner,
					currentRecord.Data);
			FreeRecord(ref platform, current, pool);
			current = next;
		}
		ReleaseOwnedStorePool(ref platform, state, owner, policy);
	}

	private static bool SetBlob<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint numericKey, APTR pointerKey, APTR data, int length,
		uint kind, bool copyKey) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (length < 0 || (length != 0 && data.IsNull)) return false;
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (owner.IsNull || (kind == DatamapKind && pointerKey.IsNull)) return false;
		var pool = ResolveStorePool(ref platform, state, owner, kind);
		var item = Find(ref platform, owner, numericKey, pointerKey, kind);
		var created = item.IsNull;
		if (created)
		{
			item = AllocateRecord(ref platform, state, owner,
				kind == DataspaceKind ? numericKey : pointerKey.Raw, kind, pool);
			if (item.IsNull) return false;
			if (kind == DatamapKind && copyKey)
			{
				var keyCopy = CopyString(ref platform, pointerKey, pool);
				if (keyCopy.IsNull)
				{
					UnlinkStore(ref platform, owner, item);
					FreeRecord(ref platform, item, pool);
					return false;
				}
				if (!MuiStoreRecordCodec.TryRead(ref platform, item,
					out var copiedKeyRecord)) return false;
				copiedKeyRecord.Key = keyCopy.Raw;
				copiedKeyRecord.Flags = (copiedKeyRecord.Flags & RecordUsesPool) |
					kind | OwnsKey |
					(pool.IsNotNull ? KeyUsesPool : 0u);
				if (!MuiStoreRecordCodec.Write(ref platform, item,
					copiedKeyRecord)) return false;
			}
			if (!OrderStoreRecord(ref platform, owner, item, kind))
			{
				UnlinkStore(ref platform, owner, item);
				FreeRecord(ref platform, item, pool);
				return false;
			}
		}
		var dataCopy = APTR.Null;
		if (length != 0)
		{
			dataCopy = AllocateStoreMemory(ref platform, pool, (uint)length);
			if (dataCopy.IsNull)
			{
				if (created)
				{
					UnlinkStore(ref platform, owner, item);
					FreeRecord(ref platform, item, pool);
				}
				return false;
			}
			platform.Copy(data, dataCopy, (uint)length);
		}
		FreeData(ref platform, item, pool);
		if (!MuiStoreRecordCodec.TryRead(ref platform, item,
			out var itemRecord)) return false;
		itemRecord.Data = dataCopy;
		itemRecord.Length = unchecked((uint)length);
		itemRecord.Flags |= length == 0 ? 0u : OwnsData |
			(pool.IsNotNull ? DataUsesPool : 0u);
		itemRecord.Generation = MuiHeadlessMemory.NextSequence(ref platform,
			state);
		if (!MuiStoreRecordCodec.Write(ref platform, item, itemRecord)) return false;
		MuiHeadlessMemory.Mutated(ref platform, state);
		return true;
	}

	private static APTR AllocateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, uint key, uint kind, APTR pool)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var item = AllocateStoreMemory(ref platform, pool,
			MuiStoreRecord.Size);
		if (item.IsNull) return APTR.Null;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var ownerValue))
		{
			FreeRecord(ref platform, item, pool);
			return APTR.Null;
		}
		MuiStoreRecord itemRecord = default;
		itemRecord.Next = ownerValue.Stores;
		itemRecord.Key = key;
		itemRecord.Flags = kind |
			(pool.IsNotNull ? RecordUsesPool : 0u);
		itemRecord.Generation = MuiHeadlessMemory.NextSequence(ref platform,
			state);
		if (!MuiStoreRecordCodec.Write(ref platform, item, itemRecord))
		{
			FreeRecord(ref platform, item, pool);
			return APTR.Null;
		}
		ownerValue.Stores = item;
		if (!MuiHeadlessObjectCodec.Write(ref platform, owner, ownerValue))
		{
			FreeRecord(ref platform, item, pool);
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
				if ((kind == DatamapKind || kind == ObjectmapKind) &&
					CStringCodec.TryEquals(ref platform,
					APTR.FromPointer(key), pointerKey, 4096, out var equal) && equal)
					return current;
			}
			current = currentRecord.Next;
		}
		return APTR.Null;
	}

	private static APTR CopyString<TPlatform>(ref TPlatform platform, APTR source,
		APTR pool)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!CStringCodec.TryReadLength(ref platform, source, 4096,
				out var length))
			return APTR.Null;
		var byteSize = length + 1;
		var copy = AllocateStoreMemory(ref platform, pool, byteSize);
		if (copy.IsNotNull) platform.Copy(source, copy, byteSize);
		return copy;
	}

	// Datamap and Objectmap are ordered string maps in MorphOS. Reinsert a fresh
	// named record into the guest-resident chain using bounded strcmp order;
	// Dataspace keeps its numeric-key insertion behavior.
	private static bool OrderStoreRecord<TPlatform>(ref TPlatform platform,
		APTR owner, APTR item, uint kind)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (kind != DatamapKind && kind != ObjectmapKind) return true;
		if (!MuiStoreRecordCodec.TryRead(ref platform, item,
			out var itemValue) || !UnlinkStore(ref platform, owner, item))
			return false;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var ownerValue)) return false;
		var current = ownerValue.Stores;
		var previous = APTR.Null;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiStoreRecordCodec.TryRead(ref platform, current,
				out var currentValue)) return false;
			var next = currentValue.Next;
			if ((currentValue.Flags & KindMask) == kind)
			{
				if (!CStringCodec.TryCompare(ref platform,
					APTR.FromPointer(itemValue.Key),
					APTR.FromPointer(currentValue.Key), 4096,
					out var comparison)) return false;
				if (comparison < 0) break;
			}
			previous = current;
			current = next;
		}
		itemValue.Next = current;
		if (!MuiStoreRecordCodec.Write(ref platform, item, itemValue)) return false;
		if (previous.IsNull) ownerValue.Stores = item;
		else
		{
			if (!MuiStoreRecordCodec.TryRead(ref platform, previous,
				out var previousValue)) return false;
			previousValue.Next = item;
			if (!MuiStoreRecordCodec.Write(ref platform, previous,
				previousValue)) return false;
		}
		return MuiHeadlessObjectCodec.Write(ref platform, owner, ownerValue);
	}

	private static bool DisposeOwnedObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR ownerRecord, APTR value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (value.IsNull) return true;
		if (MuiHeadlessObjectCodec.TryRead(ref platform, ownerRecord,
			out var ownerValue) && value.Raw == ownerValue.Boopsi.Raw)
			return true;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, value).IsNotNull)
			return MuiHeadlessObjectCore.DisposeObject(ref platform, state, value);
		platform.DisposeObject(value);
		return true;
	}

	private static void FreeData<TPlatform>(ref TPlatform platform, APTR item,
		APTR pool)
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
			FreeStoreMemory(ref platform, pool, data, length,
				(flags & DataUsesPool) != 0);
		}
		itemRecord.Data = APTR.Null;
		itemRecord.Length = 0;
		itemRecord.Flags = flags & ~(OwnsData | DataUsesPool);
		MuiStoreRecordCodec.Write(ref platform, item, itemRecord);
	}

	private static void FreeRecord<TPlatform>(ref TPlatform platform, APTR item,
		APTR pool)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiStoreRecordCodec.TryRead(ref platform, item,
			out var itemRecord)) return;
		var flags = itemRecord.Flags;
		FreeData(ref platform, item, pool);
		if ((flags & OwnsKey) != 0)
		{
			var key = APTR.FromPointer(itemRecord.Key);
			uint length = 1;
			while (length < 4096 && platform.ReadUInt8(key, (int)(length - 1)) != 0)
				length++;
			FreeStoreMemory(ref platform, pool, key, length,
				(flags & KeyUsesPool) != 0);
		}
		if ((flags & RetainsKey) != 0)
		{
			var key = APTR.FromPointer(itemRecord.Key);
			platform.ReleaseObject(key);
		}
		FreeStoreMemory(ref platform, pool, item, MuiStoreRecord.Size,
			(flags & RecordUsesPool) != 0);
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
