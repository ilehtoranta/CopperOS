/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

internal static class MuiHeadlessLayout
{
	public const uint Magic = 0x4D554934;
	public const uint Version = 1;
	public const uint StateSize = 32;
	public const uint ClassSize = 28;
	public const uint ObjectSize = 64;
	public const uint AttributeSize = 16;
	public const uint NotificationSize = 32;
	public const uint ChildSize = 16;
	public const uint StoreSize = 24;
	public const uint AllocationFlags = 0x00010001;
	public const uint MaximumTraversal = 65535;
	public const uint MaximumNotificationDepth = 32;

	public const int StateClasses = 8;
	public const int StateObjects = 12;
	public const int StateNextSequence = 16;
	public const int StateNotifyDepth = 20;
	public const int StateMutation = 24;

	public const int ClassNext = 0;
	public const int ClassName = 4;
	public const int ClassBoopsi = 8;
	public const int ClassSuper = 12;
	public const int ClassInstanceSize = 16;
	public const int ClassFlags = 20;
	public const int ClassObjectCount = 24;

	public const int ObjectNext = 0;
	public const int ObjectBoopsi = 4;
	public const int ObjectClass = 8;
	public const int ObjectAttributes = 12;
	public const int ObjectNotifications = 16;
	public const int ObjectChildrenHead = 20;
	public const int ObjectChildrenTail = 24;
	public const int ObjectParent = 28;
	public const int ObjectStores = 32;
	public const int ObjectSemaphoreOwner = 36;
	public const int ObjectSemaphoreDepth = 40;
	public const int ObjectSemaphoreShared = 44;
	public const int ObjectFlags = 48;
	public const int ObjectGeneration = 52;
	public const int ObjectId = 56;
	public const int ObjectUserData = 60;

	public const int AttributeNext = 0;
	public const int AttributeId = 4;
	public const int AttributeValue = 8;
	public const int AttributeGeneration = 12;

	public const int NotificationNext = 0;
	public const int NotificationSequence = 4;
	public const int NotificationTriggerAttribute = 8;
	public const int NotificationTriggerValue = 12;
	public const int NotificationDestination = 16;
	public const int NotificationFollowCount = 20;
	public const int NotificationFlags = 24;
	public const int NotificationPayload = 32;

	public const int ChildNext = 0;
	public const int ChildPrevious = 4;
	public const int ChildObject = 8;
	public const int ChildOwner = 12;

	public const int StoreNext = 0;
	public const int StoreKey = 4;
	public const int StoreData = 8;
	public const int StoreLength = 12;
	public const int StoreFlags = 16;
	public const int StoreGeneration = 20;
}

// A large part of the MorphOS MUI opGet surface returns one caller-owned
// ULONG. Keep that four-byte guest slot named even when the surrounding
// method packet is decoded by a specialist-specific codec.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGuestUlongStorage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal const uint ValueOffset = 0;
	internal uint Value;
}

internal enum MuiGuestUlongStorageField : byte
{
	Value,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGuestUlongStorageFieldCursor
{
	internal APTR Storage;
	internal MuiGuestUlongStorageField Field;
}

// Struct-first guest-memory adapter for a caller-owned ULONG result slot.
internal static class MuiGuestUlongStorageMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR storage, MuiGuestUlongStorageField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (field != MuiGuestUlongStorageField.Value || storage.IsNull ||
			storage.Raw > uint.MaxValue - MuiGuestUlongStorage.ValueOffset ||
			!platform.IsMapped(storage, MuiGuestUlongStorage.Size)) return false;
		address = APTR.FromPointer(storage.Raw + MuiGuestUlongStorage.ValueOffset);
		return platform.IsMapped(address, MuiGuestUlongStorage.FieldSize);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR storage, MuiGuestUlongStorageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, storage, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR storage, MuiGuestUlongStorageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, storage, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for typed cursor callers; production access
// routes through MuiGuestUlongStorageMemoryCodec.
internal static class MuiGuestUlongStorageFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGuestUlongStorageFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestUlongStorageMemoryCodec.TryGetAddress(ref platform,
			cursor.Storage, cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR storage, MuiGuestUlongStorageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestUlongStorageMemoryCodec.TryRead(ref platform, storage, field,
			out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR storage, MuiGuestUlongStorageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestUlongStorageMemoryCodec.TryWrite(ref platform, storage, field,
			value);
}

internal static class MuiGuestUlongStorageCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteValue<TPlatform>(ref TPlatform platform,
		APTR address, uint value) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGuestUlongStorage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value))
			return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGuestUlongStorage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value))
			return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGuestUlongStorage record) where TPlatform : struct, IMuiGuestMemory
		=> WriteValue(ref platform, address, record.Value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGuestUlongStorage record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!TryReadValue(ref platform, address, out var value)) return false;
		record.Value = value;
		return true;
	}
}

// Fixed 32-byte header for the guest-resident headless state. The state is
// intentionally a value record: linked class/object heads are typed APTR
// fields, while counters retain their fixed-width ABI representation. The
// named Reserved field is also the transient operation-local
// MUIA_NoNotifyMethod selector while an OM_SET tag list is executing; it is
// restored before the operation returns and is never exposed as object data.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessStateRecord
{
	internal const uint Size = 32;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint VersionOffset = 4;
	internal const uint ClassesOffset = 8;
	internal const uint ObjectsOffset = 12;
	internal const uint NextSequenceOffset = 16;
	internal const uint NotifyDepthOffset = 20;
	internal const uint MutationOffset = 24;
	internal const uint ReservedOffset = 28;
	internal uint Magic;
	internal uint Version;
	internal APTR Classes;
	internal APTR Objects;
	internal uint NextSequence;
	internal uint NotifyDepth;
	internal uint Mutation;
	internal uint Reserved;

	// Semantic alias for the fixed reserved word while an OM_SET operation is
	// active. The wire layout remains unchanged; callers use this named field
	// rather than depending on a byte offset or an anonymous scratch slot.
	internal uint NotifySuppressionMethod
	{
		get { return Reserved; }
		set { Reserved = value; }
	}
}

internal enum MuiHeadlessStateField : byte
{
	Magic,
	Version,
	Classes,
	Objects,
	NextSequence,
	NotifyDepth,
	Mutation,
	Reserved,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessStateFieldCursor
{
	internal APTR State;
	internal MuiHeadlessStateField Field;
}

// Struct-first guest-memory adapter for the canonical headless state header.
internal static class MuiHeadlessStateMemoryCodec
{
	private static bool TryResolve(MuiHeadlessStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiHeadlessStateField.Magic => MuiHeadlessStateRecord.MagicOffset,
			MuiHeadlessStateField.Version => MuiHeadlessStateRecord.VersionOffset,
			MuiHeadlessStateField.Classes => MuiHeadlessStateRecord.ClassesOffset,
			MuiHeadlessStateField.Objects => MuiHeadlessStateRecord.ObjectsOffset,
			MuiHeadlessStateField.NextSequence =>
				MuiHeadlessStateRecord.NextSequenceOffset,
			MuiHeadlessStateField.NotifyDepth =>
				MuiHeadlessStateRecord.NotifyDepthOffset,
			MuiHeadlessStateField.Mutation => MuiHeadlessStateRecord.MutationOffset,
			MuiHeadlessStateField.Reserved => MuiHeadlessStateRecord.ReservedOffset,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR state, MuiHeadlessStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || state.IsNull ||
			state.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(state, MuiHeadlessStateRecord.Size)) return false;
		address = APTR.FromPointer(state.Raw + offset);
		return platform.IsMapped(address, MuiHeadlessStateRecord.FieldSize);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR state, MuiHeadlessStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, state, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR state, MuiHeadlessStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, state, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for typed cursor callers; production access
// routes through MuiHeadlessStateMemoryCodec.
internal static class MuiHeadlessStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiHeadlessStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessStateMemoryCodec.TryGetAddress(ref platform, cursor.State,
			cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR state,
		MuiHeadlessStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessStateMemoryCodec.TryRead(ref platform, state, field, out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform, APTR state,
		MuiHeadlessStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessStateMemoryCodec.TryWrite(ref platform, state, field, value);
}

internal static class MuiHeadlessStateAdmission
{
	internal static bool Validate(MuiHeadlessStateRecord record) =>
		record.Magic == MuiHeadlessLayout.Magic &&
		record.Version == MuiHeadlessLayout.Version &&
		record.NotifyDepth <= MuiHeadlessLayout.MaximumNotificationDepth;
}

internal static class MuiHeadlessStateCodec
{
	// Sequential named-struct path used by all state-header consumers. The
	// complete fixed record is admitted before any field is exposed; the
	// field adapter above remains only for compatibility diagnostics.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiHeadlessStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHeadlessStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Version) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawClasses) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawObjects) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.NextSequence) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.NotifyDepth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Mutation) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Reserved) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		record.Classes = APTR.FromPointer(rawClasses);
		record.Objects = APTR.FromPointer(rawObjects);
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiHeadlessStateRecord record)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out record);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiHeadlessStateRecord record) where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out record) &&
		MuiHeadlessStateAdmission.Validate(record);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform, APTR address,
		MuiHeadlessStateRecord record) where TPlatform : struct, IMuiGuestMemory
		=> MuiHeadlessStateAdmission.Validate(record) &&
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHeadlessStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Version) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Classes.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Objects.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.NextSequence) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.NotifyDepth) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Mutation) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Reserved) &&
		MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiHeadlessStateRecord record) where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, record);
}

// Fixed 28-byte class registry entry. The explicit reserved UWORD preserves
// the MorphOS-compatible gap between the instance-size word and the ULONG
// flags field.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessClassRecord
{
	internal const uint Size = 28;
	internal const uint PointerFieldSize = 4;
	internal const uint WordFieldSize = 2;
	internal const uint NextOffset = 0;
	internal const uint NameOffset = 4;
	internal const uint BoopsiOffset = 8;
	internal const uint SuperOffset = 12;
	internal const uint InstanceSizeOffset = 16;
	internal const uint ReservedOffset = 18;
	internal const uint FlagsOffset = 20;
	internal const uint ObjectCountOffset = 24;
	internal APTR Next;
	internal APTR Name;
	internal APTR Boopsi;
	internal APTR Super;
	internal ushort InstanceSize;
	internal ushort Reserved;
	internal uint Flags;
	internal uint ObjectCount;
}

internal enum MuiHeadlessClassField : byte
{
	Next,
	Name,
	Boopsi,
	Super,
	InstanceSize,
	Reserved,
	Flags,
	ObjectCount,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessClassFieldCursor
{
	internal APTR Record;
	internal MuiHeadlessClassField Field;
}

// Struct-first guest-memory adapter for a headless class registry entry.
internal static class MuiHeadlessClassMemoryCodec
{
	private static bool TryResolve(MuiHeadlessClassField field,
		out uint offset, out uint size)
	{
		offset = 0;
		size = 0;
		switch (field)
		{
			case MuiHeadlessClassField.Next:
				offset = MuiHeadlessClassRecord.NextOffset;
				size = MuiHeadlessClassRecord.PointerFieldSize;
				break;
			case MuiHeadlessClassField.Name:
				offset = MuiHeadlessClassRecord.NameOffset;
				size = MuiHeadlessClassRecord.PointerFieldSize;
				break;
			case MuiHeadlessClassField.Boopsi:
				offset = MuiHeadlessClassRecord.BoopsiOffset;
				size = MuiHeadlessClassRecord.PointerFieldSize;
				break;
			case MuiHeadlessClassField.Super:
				offset = MuiHeadlessClassRecord.SuperOffset;
				size = MuiHeadlessClassRecord.PointerFieldSize;
				break;
			case MuiHeadlessClassField.InstanceSize:
				offset = MuiHeadlessClassRecord.InstanceSizeOffset;
				size = MuiHeadlessClassRecord.WordFieldSize;
				break;
			case MuiHeadlessClassField.Reserved:
				offset = MuiHeadlessClassRecord.ReservedOffset;
				size = MuiHeadlessClassRecord.WordFieldSize;
				break;
			case MuiHeadlessClassField.Flags:
				offset = MuiHeadlessClassRecord.FlagsOffset;
				size = MuiHeadlessClassRecord.PointerFieldSize;
				break;
			case MuiHeadlessClassField.ObjectCount:
				offset = MuiHeadlessClassRecord.ObjectCountOffset;
				size = MuiHeadlessClassRecord.PointerFieldSize;
				break;
			default:
				return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessClassField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset, out var size) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, MuiHeadlessClassRecord.Size))
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, size);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessClassField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address) ||
			!TryResolve(field, out _, out var size) || size != 4) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessClassField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address) ||
			!TryResolve(field, out _, out var size) || size != 4) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessClassField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address) ||
			!TryResolve(field, out _, out var size) || size != 2) return false;
		value = platform.ReadUInt16(address, 0);
		return true;
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessClassField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address) ||
			!TryResolve(field, out _, out var size) || size != 2) return false;
		platform.WriteUInt16(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for typed cursor callers; production access
// routes through MuiHeadlessClassMemoryCodec.
internal static class MuiHeadlessClassFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiHeadlessClassFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessClassMemoryCodec.TryGetAddress(ref platform, cursor.Record,
			cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessClassField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessClassMemoryCodec.TryReadUInt32(ref platform, record, field,
			out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessClassField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessClassMemoryCodec.TryWriteUInt32(ref platform, record, field,
			value);

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessClassField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessClassMemoryCodec.TryReadUInt16(ref platform, record, field,
			out value);

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessClassField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessClassMemoryCodec.TryWriteUInt16(ref platform, record, field,
			value);
}

internal static class MuiHeadlessClassCodec
{
	// Sequential named-struct path used by class-registry consumers. The
	// explicit UWORD pair is consumed in declaration order between the APTR
	// links and the ULONG counters; the field adapter remains diagnostic only.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiHeadlessClassRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHeadlessClassRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawNext) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawName) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawBoopsi) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawSuper) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out record.InstanceSize) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out record.Reserved) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Flags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.ObjectCount) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		record.Next = APTR.FromPointer(rawNext);
		record.Name = APTR.FromPointer(rawName);
		record.Boopsi = APTR.FromPointer(rawBoopsi);
		record.Super = APTR.FromPointer(rawSuper);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiHeadlessClassRecord record)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out record);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiHeadlessClassRecord record) where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHeadlessClassRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Next.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Name.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Boopsi.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Super.Raw) &&
		MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
			record.InstanceSize) &&
		MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
			record.Reserved) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Flags) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.ObjectCount) &&
		MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiHeadlessClassRecord record) where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, record);
}

// Fixed 64-byte headless object record. Pointer-bearing links are represented
// as APTR fields; only counters, flags, generations, and public scalar values
// remain ULONGs.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessObjectRecord
{
	internal const uint Size = 64;
	internal const uint FieldSize = 4;
	internal const uint NextOffset = 0;
	internal const uint BoopsiOffset = 4;
	internal const uint ClassOffset = 8;
	internal const uint AttributesOffset = 12;
	internal const uint NotificationsOffset = 16;
	internal const uint ChildrenHeadOffset = 20;
	internal const uint ChildrenTailOffset = 24;
	internal const uint ParentOffset = 28;
	internal const uint StoresOffset = 32;
	internal const uint SemaphoreOwnerOffset = 36;
	internal const uint SemaphoreDepthOffset = 40;
	internal const uint SemaphoreSharedOffset = 44;
	internal const uint FlagsOffset = 48;
	internal const uint GenerationOffset = 52;
	internal const uint ObjectIdOffset = 56;
	internal const uint UserDataOffset = 60;
	internal APTR Next;
	internal APTR Boopsi;
	internal APTR Class;
	internal APTR Attributes;
	internal APTR Notifications;
	internal APTR ChildrenHead;
	internal APTR ChildrenTail;
	internal APTR Parent;
	internal APTR Stores;
	internal APTR SemaphoreOwner;
	internal uint SemaphoreDepth;
	internal uint SemaphoreShared;
	internal uint Flags;
	internal uint Generation;
	internal uint ObjectId;
	internal uint UserData;
}

internal enum MuiHeadlessObjectField : byte
{
	Next,
	Boopsi,
	Class,
	Attributes,
	Notifications,
	ChildrenHead,
	ChildrenTail,
	Parent,
	Stores,
	SemaphoreOwner,
	SemaphoreDepth,
	SemaphoreShared,
	Flags,
	Generation,
	ObjectId,
	UserData,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessObjectFieldCursor
{
	internal APTR Record;
	internal MuiHeadlessObjectField Field;
}

// Struct-first guest-memory adapter for a headless object record.
internal static class MuiHeadlessObjectMemoryCodec
{
	private static bool TryResolve(MuiHeadlessObjectField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiHeadlessObjectField.Next => MuiHeadlessObjectRecord.NextOffset,
			MuiHeadlessObjectField.Boopsi => MuiHeadlessObjectRecord.BoopsiOffset,
			MuiHeadlessObjectField.Class => MuiHeadlessObjectRecord.ClassOffset,
			MuiHeadlessObjectField.Attributes =>
				MuiHeadlessObjectRecord.AttributesOffset,
			MuiHeadlessObjectField.Notifications =>
				MuiHeadlessObjectRecord.NotificationsOffset,
			MuiHeadlessObjectField.ChildrenHead =>
				MuiHeadlessObjectRecord.ChildrenHeadOffset,
			MuiHeadlessObjectField.ChildrenTail =>
				MuiHeadlessObjectRecord.ChildrenTailOffset,
			MuiHeadlessObjectField.Parent => MuiHeadlessObjectRecord.ParentOffset,
			MuiHeadlessObjectField.Stores => MuiHeadlessObjectRecord.StoresOffset,
			MuiHeadlessObjectField.SemaphoreOwner =>
				MuiHeadlessObjectRecord.SemaphoreOwnerOffset,
			MuiHeadlessObjectField.SemaphoreDepth =>
				MuiHeadlessObjectRecord.SemaphoreDepthOffset,
			MuiHeadlessObjectField.SemaphoreShared =>
				MuiHeadlessObjectRecord.SemaphoreSharedOffset,
			MuiHeadlessObjectField.Flags => MuiHeadlessObjectRecord.FlagsOffset,
			MuiHeadlessObjectField.Generation =>
				MuiHeadlessObjectRecord.GenerationOffset,
			MuiHeadlessObjectField.ObjectId => MuiHeadlessObjectRecord.ObjectIdOffset,
			MuiHeadlessObjectField.UserData => MuiHeadlessObjectRecord.UserDataOffset,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessObjectField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, MuiHeadlessObjectRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiHeadlessObjectRecord.FieldSize);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessObjectField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessObjectField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for typed cursor callers; production access
// routes through MuiHeadlessObjectMemoryCodec.
internal static class MuiHeadlessObjectFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiHeadlessObjectFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessObjectMemoryCodec.TryGetAddress(ref platform, cursor.Record,
			cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR record,
		MuiHeadlessObjectField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessObjectMemoryCodec.TryRead(ref platform, record, field,
			out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform, APTR record,
		MuiHeadlessObjectField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessObjectMemoryCodec.TryWrite(ref platform, record, field, value);
}

internal static class MuiHeadlessObjectCodec
{
	// Sequential named-struct path used by object-graph consumers. All ten
	// APTR links precede the six ULONG scalar fields exactly as declared; the
	// field adapter remains only for compatibility diagnostics.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiHeadlessObjectRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHeadlessObjectRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawNext) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawBoopsi) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawClass) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawAttributes) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawNotifications) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawChildrenHead) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawChildrenTail) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawParent) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawStores) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawSemaphoreOwner) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.SemaphoreDepth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.SemaphoreShared) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Flags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Generation) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.ObjectId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.UserData) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		record.Next = APTR.FromPointer(rawNext);
		record.Boopsi = APTR.FromPointer(rawBoopsi);
		record.Class = APTR.FromPointer(rawClass);
		record.Attributes = APTR.FromPointer(rawAttributes);
		record.Notifications = APTR.FromPointer(rawNotifications);
		record.ChildrenHead = APTR.FromPointer(rawChildrenHead);
		record.ChildrenTail = APTR.FromPointer(rawChildrenTail);
		record.Parent = APTR.FromPointer(rawParent);
		record.Stores = APTR.FromPointer(rawStores);
		record.SemaphoreOwner = APTR.FromPointer(rawSemaphoreOwner);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiHeadlessObjectRecord record)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out record);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiHeadlessObjectRecord record) where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHeadlessObjectRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Next.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Boopsi.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Class.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Attributes.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Notifications.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.ChildrenHead.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.ChildrenTail.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Parent.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Stores.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.SemaphoreOwner.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.SemaphoreDepth) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.SemaphoreShared) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Flags) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Generation) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.ObjectId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.UserData) &&
		MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiHeadlessObjectRecord record) where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, record);
}

// Scalar qualification surface for the fixed headless object record. The
// production object remains private to the headless implementation; this
// helper proves that all named pointer and scalar fields round-trip through
// the guest memory codec without exposing managed state.
public static class MuiHeadlessObjectPacketCore
{
	public static bool WriteLinkFieldsA<TPlatform>(ref TPlatform platform,
		APTR address, APTR next, APTR boopsi, APTR classRecord,
		APTR attributes, APTR notifications) where TPlatform : struct, IMuiGuestMemory
	{
		MuiHeadlessObjectRecord record = default;
		record.Next = next;
		record.Boopsi = boopsi;
		record.Class = classRecord;
		record.Attributes = attributes;
		record.Notifications = notifications;
		return MuiHeadlessObjectCodec.Write(ref platform, address, record);
	}

	public static bool WriteLinkFieldsB<TPlatform>(ref TPlatform platform,
		APTR address, APTR childrenHead, APTR childrenTail, APTR parent,
		APTR stores, APTR semaphoreOwner) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, address,
			out var record)) return false;
		record.ChildrenHead = childrenHead;
		record.ChildrenTail = childrenTail;
		record.Parent = parent;
		record.Stores = stores;
		record.SemaphoreOwner = semaphoreOwner;
		return MuiHeadlessObjectCodec.Write(ref platform, address, record);
	}

	public static bool WriteScalarFields<TPlatform>(ref TPlatform platform,
		APTR address, uint semaphoreDepth, uint semaphoreShared, uint flags,
		uint generation, uint objectId, uint userData)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, address,
			out var record)) return false;
		record.SemaphoreDepth = semaphoreDepth;
		record.SemaphoreShared = semaphoreShared;
		record.Flags = flags;
		record.Generation = generation;
		record.ObjectId = objectId;
		record.UserData = userData;
		return MuiHeadlessObjectCodec.Write(ref platform, address, record);
	}

	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Next.Raw ^ record.Boopsi.Raw ^ record.Class.Raw ^
			record.Attributes.Raw ^ record.Notifications.Raw ^
			record.ChildrenHead.Raw ^ record.ChildrenTail.Raw ^
			record.Parent.Raw ^ record.Stores.Raw ^ record.SemaphoreOwner.Raw ^
			record.SemaphoreDepth ^ record.SemaphoreShared ^ record.Flags ^
			record.Generation ^ record.ObjectId ^ record.UserData;
	}
}

// Scalar qualification surface for the fixed class-registry record. The
// production class type stays internal to the headless object implementation.
public static class MuiHeadlessClassPacketCore
{
	public static bool WriteRecord<TPlatform>(ref TPlatform platform, APTR address,
		APTR next, APTR name, APTR boopsi, APTR super, ushort instanceSize,
		uint flags, uint objectCount) where TPlatform : struct, IMuiGuestMemory
	{
		MuiHeadlessClassRecord record = default;
		record.Next = next;
		record.Name = name;
		record.Boopsi = boopsi;
		record.Super = super;
		record.InstanceSize = instanceSize;
		record.Flags = flags;
		record.ObjectCount = objectCount;
		return MuiHeadlessClassCodec.Write(ref platform, address, record);
	}

	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiHeadlessClassCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Boopsi.Raw ^ record.Super.Raw ^
			record.InstanceSize ^ record.Flags ^ record.ObjectCount;
	}
}

// Small scalar surface for native qualification of the state record. The
// production state remains private; this helper proves its fixed layout
// without exposing a managed object or collection.
public static class MuiHeadlessStatePacketCore
{
	public static bool WriteRecord<TPlatform>(ref TPlatform platform, APTR address,
		uint magic, uint version, APTR classes, APTR objects, uint nextSequence,
		uint notifyDepth, uint mutation, uint reserved)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiHeadlessStateRecord record = default;
		record.Magic = magic;
		record.Version = version;
		record.Classes = classes;
		record.Objects = objects;
		record.NextSequence = nextSequence;
		record.NotifyDepth = notifyDepth;
		record.Mutation = mutation;
		record.Reserved = reserved;
		return MuiHeadlessStateCodec.Write(ref platform, address, record);
	}

	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiHeadlessStateCodec.TryRead(ref platform, address,
			out var record) || record.Magic != MuiHeadlessLayout.Magic ||
			record.Version != MuiHeadlessLayout.Version) return 0;
		return record.Classes.Raw ^ record.Objects.Raw ^ record.NextSequence ^
			record.NotifyDepth ^ record.Mutation ^ record.Reserved;
	}
}

// Fixed 16-byte guest attribute node. Attribute links are APTRs; the
// identifier, value, and generation remain fixed-width ULONG fields.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessAttributeRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint NextOffset = 0;
	internal const uint IdOffset = 4;
	internal const uint ValueOffset = 8;
	internal const uint GenerationOffset = 12;
	internal APTR Next;
	internal uint Id;
	internal uint Value;
	internal uint Generation;
}

internal enum MuiHeadlessAttributeField : byte
{
	Next,
	Id,
	Value,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessAttributeFieldCursor
{
	internal APTR Record;
	internal MuiHeadlessAttributeField Field;
}

// Struct-first guest-memory adapter for a headless attribute node.
internal static class MuiHeadlessAttributeMemoryCodec
{
	private static bool TryResolve(MuiHeadlessAttributeField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiHeadlessAttributeField.Next => MuiHeadlessAttributeRecord.NextOffset,
			MuiHeadlessAttributeField.Id => MuiHeadlessAttributeRecord.IdOffset,
			MuiHeadlessAttributeField.Value => MuiHeadlessAttributeRecord.ValueOffset,
			MuiHeadlessAttributeField.Generation =>
				MuiHeadlessAttributeRecord.GenerationOffset,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessAttributeField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, MuiHeadlessAttributeRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiHeadlessAttributeRecord.FieldSize);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessAttributeField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessAttributeField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for typed cursor callers; production access
// routes through MuiHeadlessAttributeMemoryCodec.
internal static class MuiHeadlessAttributeFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiHeadlessAttributeFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessAttributeMemoryCodec.TryGetAddress(ref platform, cursor.Record,
			cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR record,
		MuiHeadlessAttributeField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessAttributeMemoryCodec.TryRead(ref platform, record, field,
			out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform, APTR record,
		MuiHeadlessAttributeField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessAttributeMemoryCodec.TryWrite(ref platform, record, field,
			value);
}

internal static class MuiHeadlessAttributeCodec
{
	// Sequential named-struct path used by attribute-list consumers. The APTR
	// link and three ULONG fields are exchanged in declaration order; the field
	// adapter remains only for compatibility diagnostics.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiHeadlessAttributeRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHeadlessAttributeRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawNext) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Id) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Value) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Generation) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		record.Next = APTR.FromPointer(rawNext);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiHeadlessAttributeRecord record)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out record);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiHeadlessAttributeRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHeadlessAttributeRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Next.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Id) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Value) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Generation) &&
		MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiHeadlessAttributeRecord record)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, record);
}

// Scalar qualification surface for the fixed attribute node. Production
// attribute mutation remains inside HeadlessObjectCore; this seam proves the
// four named fields without introducing managed state.
public static class MuiHeadlessAttributePacketCore
{
	public static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, APTR next, uint id, uint value, uint generation)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiHeadlessAttributeRecord record = default;
		record.Next = next;
		record.Id = id;
		record.Value = value;
		record.Generation = generation;
		return MuiHeadlessAttributeCodec.Write(ref platform, address, record);
	}

	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiHeadlessAttributeCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Next.Raw ^ record.Id ^ record.Value ^ record.Generation;
	}
}

// Fixed 16-byte guest child-list node.  All Family topology code consumes
// this named record; byte offsets remain confined to the ABI codec below.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessChildRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint NextOffset = 0;
	internal const uint PreviousOffset = 4;
	internal const uint ObjectOffset = 8;
	internal const uint OwnerOffset = 12;
	internal APTR Next;
	internal APTR Previous;
	internal APTR Object;
	internal APTR Owner;
}

internal enum MuiHeadlessChildField : byte
{
	Next,
	Previous,
	Object,
	Owner,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessChildFieldCursor
{
	internal APTR Record;
	internal MuiHeadlessChildField Field;
}

// Struct-first guest-memory adapter for a Family child-list node.
internal static class MuiHeadlessChildMemoryCodec
{
	private static bool TryResolve(MuiHeadlessChildField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiHeadlessChildField.Next => MuiHeadlessChildRecord.NextOffset,
			MuiHeadlessChildField.Previous => MuiHeadlessChildRecord.PreviousOffset,
			MuiHeadlessChildField.Object => MuiHeadlessChildRecord.ObjectOffset,
			MuiHeadlessChildField.Owner => MuiHeadlessChildRecord.OwnerOffset,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessChildField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, MuiHeadlessChildRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiHeadlessChildRecord.FieldSize);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessChildField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessChildField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for typed cursor callers; production access
// routes through MuiHeadlessChildMemoryCodec.
internal static class MuiHeadlessChildFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiHeadlessChildFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessChildMemoryCodec.TryGetAddress(ref platform, cursor.Record,
			cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR record,
		MuiHeadlessChildField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessChildMemoryCodec.TryRead(ref platform, record, field, out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform, APTR record,
		MuiHeadlessChildField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessChildMemoryCodec.TryWrite(ref platform, record, field, value);
}

internal static class MuiHeadlessChildCodec
{
	// Sequential named-struct path used by Family topology consumers. The four
	// APTR links are exchanged in declaration order; the field adapter remains
	// only for compatibility diagnostics.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiHeadlessChildRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHeadlessChildRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawNext) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawPrevious) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawObject) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawOwner) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		record.Next = APTR.FromPointer(rawNext);
		record.Previous = APTR.FromPointer(rawPrevious);
		record.Object = APTR.FromPointer(rawObject);
		record.Owner = APTR.FromPointer(rawOwner);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiHeadlessChildRecord record)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out record);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiHeadlessChildRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHeadlessChildRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Next.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Previous.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Object.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Owner.Raw) &&
		MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiHeadlessChildRecord record)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, record);
}

// Scalar qualification surface for the Family child-list node.  The live
// Family implementation owns link mutation; this seam proves the four named
// pointer fields round-trip through the fixed guest ABI record.
public static class MuiHeadlessChildPacketCore
{
	public static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, APTR next, APTR previous, APTR obj, APTR owner)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiHeadlessChildRecord record = default;
		record.Next = next;
		record.Previous = previous;
		record.Object = obj;
		record.Owner = owner;
		return MuiHeadlessChildCodec.Write(ref platform, address, record);
	}

	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiHeadlessChildCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Next.Raw ^ record.Previous.Raw ^ record.Object.Raw ^
			record.Owner.Raw;
	}
}

// Fixed 32-byte notification header.  The payload is trailing guest storage
// owned by the notification node; the fixed header itself uses named fields,
// including the reserved ULONG before that payload.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessNotificationRecord
{
	internal const uint Size = 32;
	internal const uint FieldSize = 4;
	internal const uint NextOffset = 0;
	internal const uint SequenceOffset = 4;
	internal const uint TriggerAttributeOffset = 8;
	internal const uint TriggerValueOffset = 12;
	internal const uint DestinationOffset = 16;
	internal const uint FollowCountOffset = 20;
	internal const uint FlagsOffset = 24;
	internal const uint ReservedOffset = 28;
	internal APTR Next;
	internal uint Sequence;
	internal uint TriggerAttribute;
	internal uint TriggerValue;
	internal APTR Destination;
	internal uint FollowCount;
	internal uint Flags;
	internal uint Reserved;
}

internal enum MuiHeadlessNotificationField : byte
{
	Next,
	Sequence,
	TriggerAttribute,
	TriggerValue,
	Destination,
	FollowCount,
	Flags,
	Reserved,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessNotificationFieldCursor
{
	internal APTR Record;
	internal MuiHeadlessNotificationField Field;
}

// Struct-first guest-memory adapter for a headless notification header.
internal static class MuiHeadlessNotificationMemoryCodec
{
	private static bool TryResolve(MuiHeadlessNotificationField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiHeadlessNotificationField.Next =>
				MuiHeadlessNotificationRecord.NextOffset,
			MuiHeadlessNotificationField.Sequence =>
				MuiHeadlessNotificationRecord.SequenceOffset,
			MuiHeadlessNotificationField.TriggerAttribute =>
				MuiHeadlessNotificationRecord.TriggerAttributeOffset,
			MuiHeadlessNotificationField.TriggerValue =>
				MuiHeadlessNotificationRecord.TriggerValueOffset,
			MuiHeadlessNotificationField.Destination =>
				MuiHeadlessNotificationRecord.DestinationOffset,
			MuiHeadlessNotificationField.FollowCount =>
				MuiHeadlessNotificationRecord.FollowCountOffset,
			MuiHeadlessNotificationField.Flags =>
				MuiHeadlessNotificationRecord.FlagsOffset,
			MuiHeadlessNotificationField.Reserved =>
				MuiHeadlessNotificationRecord.ReservedOffset,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessNotificationField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, MuiHeadlessNotificationRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiHeadlessNotificationRecord.FieldSize);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessNotificationField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessNotificationField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for typed cursor callers; production access
// routes through MuiHeadlessNotificationMemoryCodec.
internal static class MuiHeadlessNotificationFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiHeadlessNotificationFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessNotificationMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR record,
		MuiHeadlessNotificationField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessNotificationMemoryCodec.TryRead(ref platform, record, field,
			out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform, APTR record,
		MuiHeadlessNotificationField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessNotificationMemoryCodec.TryWrite(ref platform, record, field,
			value);
}

// Named view of the variable payload trailing a notification header. Keeping
// the record address and requested byte count together centralizes the fixed
// 32-byte boundary and absolute-range validation.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessNotificationPayloadCursor
{
	internal APTR Record;
	internal uint PayloadBytes;
}

// Struct-first guest-memory adapter for the variable payload trailing a
// notification header. The complete header and requested payload range must
// be mapped before the payload address is exposed.
internal static class MuiHeadlessNotificationPayloadMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint payloadBytes, out APTR payload)
		where TPlatform : struct, IMuiGuestMemory
	{
		payload = APTR.Null;
		if (record.IsNull || record.Raw >
			uint.MaxValue - MuiHeadlessNotificationRecord.Size ||
			payloadBytes > uint.MaxValue -
			MuiHeadlessNotificationRecord.Size) return false;
		var total = MuiHeadlessNotificationRecord.Size + payloadBytes;
		if (!platform.IsMapped(record, total)) return false;
		payload = APTR.FromPointer(record.Raw +
			MuiHeadlessNotificationRecord.Size);
		return payload.Raw <= uint.MaxValue - payloadBytes;
	}
}

// Compatibility wrapper retained for the typed payload-range cursor.
internal static class MuiHeadlessNotificationPayloadCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiHeadlessNotificationPayloadCursor cursor, out APTR payload)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiHeadlessNotificationPayloadMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.PayloadBytes, out payload);
}

internal static class MuiHeadlessNotificationCodec
{
	internal static bool TryGetPayload<TPlatform>(ref TPlatform platform,
		APTR address, uint payloadBytes, out APTR payload)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiHeadlessNotificationPayloadMemoryCodec.TryGetAddress(ref platform,
			address, payloadBytes, out payload);

	// Sequential named-struct path used by notification consumers. The APTR
	// links and six ULONG header fields are exchanged in declaration order; the
	// field adapter remains only for compatibility diagnostics.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiHeadlessNotificationRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHeadlessNotificationRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawNext) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Sequence) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.TriggerAttribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.TriggerValue) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawDestination) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.FollowCount) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Flags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Reserved) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		record.Next = APTR.FromPointer(rawNext);
		record.Destination = APTR.FromPointer(rawDestination);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiHeadlessNotificationRecord record)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out record);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiHeadlessNotificationRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiHeadlessNotificationRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Next.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Sequence) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.TriggerAttribute) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.TriggerValue) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Destination.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.FollowCount) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Flags) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Reserved) &&
		MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiHeadlessNotificationRecord record)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, record);
}

// Scalar qualification surface for the fixed notification header.  Payload
// copying and dispatch remain in NotifyCore; this seam proves the named ABI
// fields without introducing managed notification state.
public static class MuiHeadlessNotificationPacketCore
{
	public static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, APTR next, uint sequence, uint triggerAttribute,
		uint triggerValue, APTR destination, uint followCount, uint flags,
		uint reserved) where TPlatform : struct, IMuiGuestMemory
	{
		MuiHeadlessNotificationRecord record = default;
		record.Next = next;
		record.Sequence = sequence;
		record.TriggerAttribute = triggerAttribute;
		record.TriggerValue = triggerValue;
		record.Destination = destination;
		record.FollowCount = followCount;
		record.Flags = flags;
		record.Reserved = reserved;
		return MuiHeadlessNotificationCodec.Write(ref platform, address, record);
	}

	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiHeadlessNotificationCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Next.Raw ^ record.Sequence ^ record.TriggerAttribute ^
			record.TriggerValue ^ record.Destination.Raw ^ record.FollowCount ^
			record.Flags ^ record.Reserved;
	}
}

// Named view of the guest-resident dataspace/map record.  The store remains a
// native linked list, but callers should consume its fields through this
// record rather than repeating byte offsets at each persistence boundary.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStoreRecord
{
	internal const uint Size = 24;
	internal const uint FieldSize = 4;
	internal const uint NextOffset = 0;
	internal const uint KeyOffset = 4;
	internal const uint DataOffset = 8;
	internal const uint LengthOffset = 12;
	internal const uint FlagsOffset = 16;
	internal const uint GenerationOffset = 20;
	internal APTR Next;
	internal uint Key;
	internal APTR Data;
	internal uint Length;
	internal uint Flags;
	internal uint Generation;
}

internal enum MuiStoreRecordField : byte
{
	Next,
	Key,
	Data,
	Length,
	Flags,
	Generation,
}

[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiStoreRecordFieldCursor
{
	internal APTR Record;
	internal MuiStoreRecordField Field;
}

// Struct-first guest-memory adapter for the native Store/Dataspace record.
internal static class MuiStoreRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiStoreRecordField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (field)
		{
			case MuiStoreRecordField.Next:
				offset = MuiStoreRecord.NextOffset;
				break;
			case MuiStoreRecordField.Key:
				offset = MuiStoreRecord.KeyOffset;
				break;
			case MuiStoreRecordField.Data:
				offset = MuiStoreRecord.DataOffset;
				break;
			case MuiStoreRecordField.Length:
				offset = MuiStoreRecord.LengthOffset;
				break;
			case MuiStoreRecordField.Flags:
				offset = MuiStoreRecord.FlagsOffset;
				break;
			case MuiStoreRecordField.Generation:
				offset = MuiStoreRecord.GenerationOffset;
				break;
			default:
				return false;
		}
		if (record.IsNull || record.Raw >
			uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStoreRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiStoreRecord.FieldSize);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiStoreRecordField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiStoreRecordField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for typed cursor callers; production access
// routes through MuiStoreRecordMemoryCodec and its named record layout.
internal static class MuiStoreRecordFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStoreRecordFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiStoreRecordMemoryCodec.TryGetAddress(ref platform, cursor.Record,
			cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiStoreRecordField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiStoreRecordMemoryCodec.TryRead(ref platform, record, field, out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiStoreRecordField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiStoreRecordMemoryCodec.TryWrite(ref platform, record, field, value);
}

internal static class MuiStoreRecordCodec
{
	// Sequential named-struct path used by dataspace/store consumers. The two
	// APTR links and four ULONG fields are exchanged in declaration order; the
	// field adapter remains only for compatibility diagnostics.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStoreRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStoreRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawNext) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Key) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawData) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Length) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Flags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Generation) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		record.Next = APTR.FromPointer(rawNext);
		record.Data = APTR.FromPointer(rawData);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStoreRecord record)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out record);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiStoreRecord record) where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStoreRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Next.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Key) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Data.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Length) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Flags) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.Generation) &&
		MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStoreRecord record) where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, record);
}

// Scalar qualification surface for the fixed 24-byte Store/Dataspace record.
// StoreCore owns allocation and lifetime; this seam proves the named fields
// round-trip without exposing a managed map or iterator.
public static class MuiStoreRecordPacketCore
{
	public static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, APTR next, uint key, APTR data, uint length, uint flags,
		uint generation) where TPlatform : struct, IMuiGuestMemory
	{
		MuiStoreRecord record = default;
		record.Next = next;
		record.Key = key;
		record.Data = data;
		record.Length = length;
		record.Flags = flags;
		record.Generation = generation;
		return MuiStoreRecordCodec.Write(ref platform, address, record);
	}

	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiStoreRecordCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Next.Raw ^ record.Key ^ record.Data.Raw ^ record.Length ^
			record.Flags ^ record.Generation;
	}
}

internal static class MuiHeadlessMemory
{
	public static bool Initialize<TPlatform>(ref TPlatform platform, APTR state)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (state.IsNull || !platform.IsMapped(state,
			MuiHeadlessStateRecord.Size))
			return false;
		platform.Clear(state, MuiHeadlessStateRecord.Size);
		MuiHeadlessStateRecord record = default;
		record.Magic = MuiHeadlessLayout.Magic;
		record.Version = MuiHeadlessLayout.Version;
		record.NextSequence = 1;
		return MuiHeadlessStateCodec.Write(ref platform, state, record);
	}

	public static bool Ensure<TPlatform>(ref TPlatform platform, APTR state)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (state.IsNull || !platform.IsMapped(state,
			MuiHeadlessStateRecord.Size))
			return false;
		if (MuiHeadlessStateCodec.TryRead(ref platform, state, out var record) &&
			record.Magic == MuiHeadlessLayout.Magic &&
			record.Version == MuiHeadlessLayout.Version) return true;
		return Initialize(ref platform, state);
	}

	public static APTR Allocate<TPlatform>(ref TPlatform platform, uint size)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var result = platform.Allocate(size, MuiHeadlessLayout.AllocationFlags);
		if (result.IsNull || !platform.IsMapped(result, size))
		{
			if (result.IsNotNull) platform.Free(result, size);
			return APTR.Null;
		}
		platform.Clear(result, size);
		return result;
	}

	public static uint NextSequence<TPlatform>(ref TPlatform platform, APTR state)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state, out var record))
			return 0;
		var value = record.NextSequence;
		if (value == 0) value = 1;
		record.NextSequence = value + 1;
		MuiHeadlessStateCodec.Write(ref platform, state, record);
		return value;
	}

	public static void Mutated<TPlatform>(ref TPlatform platform, APTR state)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state, out var record))
			return;
		record.Mutation++;
		MuiHeadlessStateCodec.Write(ref platform, state, record);
	}
}
