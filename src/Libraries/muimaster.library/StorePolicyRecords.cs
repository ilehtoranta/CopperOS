/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS keeps the Datamap/Objectmap policy knobs as ordinary ULONG-backed
// attributes. Keep the pair together as a value record at the MUI boundary so
// dispatch code does not grow independent scalar lookups or anonymous state.
// The record is host-side semantic state; the actual values remain in the
// existing guest-resident named attribute nodes.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStorePolicyRecord
{
	internal uint AutoLock;
	internal uint CopyKeys;
}

// MorphOS store classes accept one optional external Exec memory pool during
// construction. Keep the pool handle and its ownership mode together as a
// named value record; the handle remains opaque to MUI and is never treated as
// a host object or managed allocation.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStorePoolPolicyRecord
{
	internal APTR Pool;
	internal uint UsesExternalPool;
}

// Class-owned pool state. The pointer is kept in one private
// guest attribute and the pointed-to value is a named record so object
// disposal can recover the pool without a managed side table.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStorePoolStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint PoolOffset = 0;
	internal const uint PolicyOffset = 4;
	internal const uint OwnsPoolOffset = 8;
	internal const uint MagicOffset = 12;
	internal const uint MagicValue = 0x504F4F4Cu;
	internal APTR Pool;
	internal uint Policy;
	internal uint OwnsPool;
	internal uint Magic;
}

internal enum MuiStorePoolStateField : byte
{
	Pool,
	Policy,
	OwnsPool,
	Magic,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStorePoolStateFieldCursor
{
	internal APTR Record;
	internal MuiStorePoolStateField Field;
}

internal static class MuiStorePoolStateCodec
{
	private static bool TryResolveFieldIndex(MuiStorePoolStateField field,
		out uint index)
	{
		index = field switch
		{
			MuiStorePoolStateField.Pool => 0,
			MuiStorePoolStateField.Policy => 1,
			MuiStorePoolStateField.OwnsPool => 2,
			MuiStorePoolStateField.Magic => 3,
			_ => uint.MaxValue,
		};
		return index != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR address, MuiStorePoolStateField field, out APTR fieldAddress)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStorePoolStateFieldCursor);
		cursor.Record = address;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out fieldAddress, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStorePoolStateFieldCursor cursor, out APTR fieldAddress,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		fieldAddress = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiStorePoolStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiStorePoolStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				fieldAddress = candidate;
				fieldSize = MuiStorePoolStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiStorePoolStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryReadStructural(ref platform, address, out var state)) return false;
		if (field == MuiStorePoolStateField.Pool) value = state.Pool.Raw;
		else if (field == MuiStorePoolStateField.Policy) value = state.Policy;
		else if (field == MuiStorePoolStateField.OwnsPool) value = state.OwnsPool;
		else if (field == MuiStorePoolStateField.Magic) value = state.Magic;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiStorePoolStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadStructural(ref platform, address, out var state)) return false;
		if (field == MuiStorePoolStateField.Pool) state.Pool = APTR.FromPointer(value);
		else if (field == MuiStorePoolStateField.Policy) state.Policy = value;
		else if (field == MuiStorePoolStateField.OwnsPool) state.OwnsPool = value;
		else if (field == MuiStorePoolStateField.Magic) state.Magic = value;
		else return false;
		return WriteStructural(ref platform, address, state);
	}

	// Declaration-order pool state: Pool APTR, policy/ownership flags, magic.
	// The semantic record is exchanged through the bounded cursor; field
	// addressing above remains solely for diagnostics and compatibility.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStorePoolStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStorePoolStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawPool) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Policy) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.OwnsPool) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic)) return false;
		value.Pool = APTR.FromPointer(rawPool);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStorePoolStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiStorePoolStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiStorePoolStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStorePoolStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Pool.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Policy) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.OwnsPool) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStorePoolStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStorePoolStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);
}

// Guest-resident iterator state for the string-map methods. The caller owns
// only the four-byte counter; this record keeps the current and next entries
// in guest memory so removal of the current entry cannot skip its successor.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStoreIterationStateRecord
{
	internal const uint Size = 24;
	internal const uint FieldSize = 4;
	internal const uint NextOffset = 0;
	internal const uint CounterOffset = 4;
	internal const uint CurrentOffset = 8;
	internal const uint NextRecordOffset = 12;
	internal const uint KindOffset = 16;
	internal const uint MagicOffset = 20;
	internal const uint MagicValue = 0x49544552u;
	internal APTR Next;
	internal APTR Counter;
	internal APTR Current;
	internal APTR NextRecord;
	internal uint Kind;
	internal uint Magic;
}

internal enum MuiStoreIterationStateField : byte
{
	Next,
	Counter,
	Current,
	NextRecord,
	Kind,
	Magic,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStoreIterationStateFieldCursor
{
	internal APTR Record;
	internal MuiStoreIterationStateField Field;
}

internal static class MuiStoreIterationStateCodec
{
	private static bool TryResolveFieldIndex(MuiStoreIterationStateField field,
		out uint index)
	{
		index = field switch
		{
			MuiStoreIterationStateField.Next => 0,
			MuiStoreIterationStateField.Counter => 1,
			MuiStoreIterationStateField.Current => 2,
			MuiStoreIterationStateField.NextRecord => 3,
			MuiStoreIterationStateField.Kind => 4,
			MuiStoreIterationStateField.Magic => 5,
			_ => uint.MaxValue,
		};
		return index != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR address, MuiStoreIterationStateField field, out APTR fieldAddress)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStoreIterationStateFieldCursor);
		cursor.Record = address;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out fieldAddress, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStoreIterationStateFieldCursor cursor, out APTR fieldAddress,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		fieldAddress = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiStoreIterationStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiStoreIterationStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				fieldAddress = candidate;
				fieldSize = MuiStoreIterationStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiStoreIterationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryReadStructural(ref platform, address, out var state)) return false;
		if (field == MuiStoreIterationStateField.Next) value = state.Next.Raw;
		else if (field == MuiStoreIterationStateField.Counter) value = state.Counter.Raw;
		else if (field == MuiStoreIterationStateField.Current) value = state.Current.Raw;
		else if (field == MuiStoreIterationStateField.NextRecord) value = state.NextRecord.Raw;
		else if (field == MuiStoreIterationStateField.Kind) value = state.Kind;
		else if (field == MuiStoreIterationStateField.Magic) value = state.Magic;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiStoreIterationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadStructural(ref platform, address, out var state)) return false;
		if (field == MuiStoreIterationStateField.Next) state.Next = APTR.FromPointer(value);
		else if (field == MuiStoreIterationStateField.Counter) state.Counter = APTR.FromPointer(value);
		else if (field == MuiStoreIterationStateField.Current) state.Current = APTR.FromPointer(value);
		else if (field == MuiStoreIterationStateField.NextRecord) state.NextRecord = APTR.FromPointer(value);
		else if (field == MuiStoreIterationStateField.Kind) state.Kind = value;
		else if (field == MuiStoreIterationStateField.Magic) state.Magic = value;
		else return false;
		return WriteStructural(ref platform, address, state);
	}

	// Declaration-order iterator record: four APTR links followed by Kind and
	// magic. Keep cursor traversal as the sole production serialization path.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStoreIterationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStoreIterationStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawNext) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawCounter) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawCurrent) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawNextRecord) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Kind) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic)) return false;
		value.Next = APTR.FromPointer(rawNext);
		value.Counter = APTR.FromPointer(rawCounter);
		value.Current = APTR.FromPointer(rawCurrent);
		value.NextRecord = APTR.FromPointer(rawNextRecord);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStoreIterationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiStoreIterationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiStoreIterationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStoreIterationStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Next.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Counter.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Current.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.NextRecord.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Kind) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStoreIterationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStoreIterationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);
}

internal enum MuiStorePolicyKind : byte
{
	None,
	Dataspace,
	Datamap,
	Objectmap,
}

internal enum MuiStoreMethodKind : byte
{
	None,
	Dataspace,
	Datamap,
	Objectmap,
}

internal static class MuiStorePolicyCore
{
	internal const uint PoolStateAttribute = 0x7FFE0047u;
	internal const uint IterationStateAttribute = 0x7FFE0048u;
	internal const uint DefaultPoolPuddleSize = 2008;
	internal const uint DefaultPoolThreshold = 1024;
	internal const uint DatamapAutoLockAttribute = 0x8042FBE4;
	internal const uint DatamapCopyKeysAttribute = 0x8042A179;
	internal const uint DatamapPoolAttribute = 0x80424724;
	internal const uint DataspacePoolAttribute = 0x80424CF9;
	internal const uint ObjectmapAutoLockAttribute = 0x8042E65F;
	internal const uint ObjectmapCopyKeysAttribute = 0x8042B964;
	internal const uint ObjectmapPoolAttribute = 0x80422ED3;

	internal static MuiStoreMethodKind ClassifyMethod(uint method)
	{
		if (method == MuiDataspaceMessageCore.AddMethod ||
			method == MuiDataspaceMessageCore.ClearMethod ||
			method == MuiDataspaceMessageCore.FindMethod ||
			method == MuiDataspaceMessageCore.GetMethod ||
			method == MuiDataspaceMessageCore.MergeMethod ||
			method == MuiDataspaceMessageCore.RemoveMethod)
			return MuiStoreMethodKind.Dataspace;
		if (method == MuiStoreMessageCore.DatamapSetMethod ||
			method == MuiStoreMessageCore.DatamapFindMethod ||
			method == MuiStoreMessageCore.DatamapGetMethod ||
			method == MuiStoreMessageCore.DatamapIterateMethod ||
			method == MuiStoreMessageCore.DatamapIterationKeyMethod ||
			method == MuiStoreMessageCore.DatamapRemoveMethod ||
			method == MuiStoreMessageCore.DatamapClearMethod)
			return MuiStoreMethodKind.Datamap;
		if (method == MuiStoreMessageCore.ObjectmapSetMethod ||
			method == MuiStoreMessageCore.ObjectmapFindMethod ||
			method == MuiStoreMessageCore.ObjectmapIterateMethod ||
			method == MuiStoreMessageCore.ObjectmapIterationKeyMethod ||
			method == MuiStoreMessageCore.ObjectmapRemoveMethod ||
			method == MuiStoreMessageCore.ObjectmapClearMethod)
			return MuiStoreMethodKind.Objectmap;
		return MuiStoreMethodKind.None;
	}

	internal static bool IsMethodClassCompatible<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, MuiStoreMethodKind method)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (method == MuiStoreMethodKind.None) return true;
		var cls = MuiCommonControlCore.Classify(ref platform, state, obj);
		if (cls == MuiControlClass.Unknown) return true;
		return method == MuiStoreMethodKind.Dataspace
			? cls == MuiControlClass.Dataspace
			: method == MuiStoreMethodKind.Datamap
				? cls == MuiControlClass.Datamap
				: cls == MuiControlClass.Objectmap;
	}

	internal static bool IsPolicyAttribute(uint attribute) =>
		attribute == DatamapAutoLockAttribute ||
		attribute == DatamapCopyKeysAttribute ||
		attribute == DatamapPoolAttribute ||
		attribute == DataspacePoolAttribute ||
		attribute == ObjectmapAutoLockAttribute ||
		attribute == ObjectmapCopyKeysAttribute ||
		attribute == ObjectmapPoolAttribute;

	internal static bool TryGetPolicyKind(uint attribute,
		out MuiStorePolicyKind policy)
	{
		policy = attribute == DatamapAutoLockAttribute ||
			attribute == DatamapCopyKeysAttribute ||
			attribute == DatamapPoolAttribute
			? MuiStorePolicyKind.Datamap
			: attribute == DataspacePoolAttribute
				? MuiStorePolicyKind.Dataspace
				: attribute == ObjectmapAutoLockAttribute ||
					attribute == ObjectmapCopyKeysAttribute ||
					attribute == ObjectmapPoolAttribute
					? MuiStorePolicyKind.Objectmap
					: MuiStorePolicyKind.None;
		return policy != MuiStorePolicyKind.None;
	}

	internal static bool IsInitOnlyAttribute(uint attribute) =>
		IsPolicyAttribute(attribute);

	internal static bool IsGetterlessForKnownStore(uint attribute,
		MuiControlClass cls) => IsPolicyAttribute(attribute) &&
		(cls == MuiControlClass.Datamap || cls == MuiControlClass.Dataspace ||
			cls == MuiControlClass.Objectmap);

	internal static bool IsClassCompatible<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStorePolicyKind policy)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var cls = MuiCommonControlCore.Classify(ref platform, state, obj);
		if (cls == MuiControlClass.Unknown) return true;
		return policy == MuiStorePolicyKind.Dataspace
			? cls == MuiControlClass.Dataspace
			: policy == MuiStorePolicyKind.Datamap
				? cls == MuiControlClass.Datamap
				: policy == MuiStorePolicyKind.Objectmap &&
					cls == MuiControlClass.Objectmap;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, MuiStorePolicyKind policy, out MuiStorePolicyRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (policy != MuiStorePolicyKind.Datamap &&
			policy != MuiStorePolicyKind.Objectmap) return false;
		if (!IsClassCompatible(ref platform, state, obj, policy)) return false;
		var autoLock = policy == MuiStorePolicyKind.Datamap
			? DatamapAutoLockAttribute : ObjectmapAutoLockAttribute;
		var copyKeys = policy == MuiStorePolicyKind.Datamap
			? DatamapCopyKeysAttribute : ObjectmapCopyKeysAttribute;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			autoLock, out var rawAutoLock)) value.AutoLock = rawAutoLock;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			copyKeys, out var rawCopyKeys)) value.CopyKeys = rawCopyKeys;
		return true;
	}

	internal static bool TryReadPool<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStorePolicyKind policy,
		out MuiStorePoolPolicyRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (policy == MuiStorePolicyKind.None ||
			!IsClassCompatible(ref platform, state, obj, policy)) return false;
		var attribute = policy == MuiStorePolicyKind.Dataspace
			? DataspacePoolAttribute
			: policy == MuiStorePolicyKind.Datamap
				? DatamapPoolAttribute
				: ObjectmapPoolAttribute;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			attribute, out var rawPool))
		{
			value.Pool = APTR.FromPointer(rawPool);
			value.UsesExternalPool = value.Pool.IsNotNull ? 1u : 0u;
		}
		return true;
	}

	internal static bool TryReadOwnedPoolState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiStorePolicyKind policy, out APTR stateAddress,
		out MuiStorePoolStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		stateAddress = APTR.Null;
		value = default;
		if (policy == MuiStorePolicyKind.None ||
			!IsClassCompatible(ref platform, state, obj, policy) ||
			!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				PoolStateAttribute, out var rawState)) return false;
		stateAddress = APTR.FromPointer(rawState);
		return MuiStorePoolStateCodec.TryRead(ref platform, stateAddress,
			out value) && value.Pool.IsNotNull && value.OwnsPool != 0 &&
			value.Magic == MuiStorePoolStateRecord.MagicValue &&
			value.Policy == (uint)policy;
	}

	internal static MuiStorePolicyKind PolicyForStoreKind(uint kind) =>
		kind == 0x100u ? MuiStorePolicyKind.Dataspace :
		kind == 0x200u ? MuiStorePolicyKind.Datamap :
		kind == 0x300u ? MuiStorePolicyKind.Objectmap :
		MuiStorePolicyKind.None;

	internal static bool AutoLockEnabled<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStorePolicyKind policy)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryRead(ref platform, state, obj, policy, out var value) &&
		value.AutoLock != 0;

	internal static bool CopyKeysEnabled<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStorePolicyKind policy)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryRead(ref platform, state, obj, policy, out var value) &&
		value.CopyKeys != 0;
}
