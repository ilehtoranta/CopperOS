/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
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

internal static class MuiGuestUlongStorageFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGuestUlongStorageFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Field != MuiGuestUlongStorageField.Value ||
			cursor.Storage.IsNull) return false;
		address = cursor.Storage;
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR storage, MuiGuestUlongStorageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiGuestUlongStorageFieldCursor);
		cursor.Storage = storage;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR storage, MuiGuestUlongStorageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGuestUlongStorageFieldCursor);
		cursor.Storage = storage;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiGuestUlongStorageCodec
{
	internal static bool WriteValue<TPlatform>(ref TPlatform platform,
		APTR address, uint value) where TPlatform : struct, IMuiGuestMemory
	{
		var record = default(MuiGuestUlongStorage);
		record.Value = value;
		return Write(ref platform, address, record);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGuestUlongStorage record) where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiGuestUlongStorage.Size)) return false;
		return MuiGuestUlongStorageFieldCursorCodec.TryWrite(ref platform,
			address, MuiGuestUlongStorageField.Value, record.Value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGuestUlongStorage record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiGuestUlongStorage.Size)) return false;
		return MuiGuestUlongStorageFieldCursorCodec.TryRead(ref platform, address,
			MuiGuestUlongStorageField.Value, out record.Value);
	}
}

// Fixed 32-byte header for the guest-resident headless state. The state is
// intentionally a value record: linked class/object heads are typed APTR
// fields, while counters retain their fixed-width ABI representation.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessStateRecord
{
	internal const uint Size = 32;
	internal uint Magic;
	internal uint Version;
	internal APTR Classes;
	internal APTR Objects;
	internal uint NextSequence;
	internal uint NotifyDepth;
	internal uint Mutation;
	internal uint Reserved;
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

internal static class MuiHeadlessStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiHeadlessStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiHeadlessStateField.Magic:
				offset = 0;
				break;
			case MuiHeadlessStateField.Version:
				offset = 4;
				break;
			case MuiHeadlessStateField.Classes:
				offset = 8;
				break;
			case MuiHeadlessStateField.Objects:
				offset = 12;
				break;
			case MuiHeadlessStateField.NextSequence:
				offset = 16;
				break;
			case MuiHeadlessStateField.NotifyDepth:
				offset = 20;
				break;
			case MuiHeadlessStateField.Mutation:
				offset = 24;
				break;
			case MuiHeadlessStateField.Reserved:
				offset = 28;
				break;
			default:
				return false;
		}
		if (cursor.State.IsNull || cursor.State.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.State.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR state, MuiHeadlessStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiHeadlessStateFieldCursor);
		cursor.State = state;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR state, MuiHeadlessStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiHeadlessStateFieldCursor);
		cursor.State = state;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiHeadlessStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiHeadlessStateRecord record) where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiHeadlessStateRecord.Size)) return false;
		if (!MuiHeadlessStateFieldCursorCodec.TryRead(ref platform, address,
			MuiHeadlessStateField.Magic, out record.Magic) ||
			!MuiHeadlessStateFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessStateField.Version, out record.Version)) return false;
		if (!MuiHeadlessStateFieldCursorCodec.TryRead(ref platform, address,
			MuiHeadlessStateField.Classes, out var rawClasses) ||
			!MuiHeadlessStateFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessStateField.Objects, out var rawObjects)) return false;
		record.Classes = APTR.FromPointer(rawClasses);
		record.Objects = APTR.FromPointer(rawObjects);
		return MuiHeadlessStateFieldCursorCodec.TryRead(ref platform, address,
			MuiHeadlessStateField.NextSequence, out record.NextSequence) &&
			MuiHeadlessStateFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessStateField.NotifyDepth, out record.NotifyDepth) &&
			MuiHeadlessStateFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessStateField.Mutation, out record.Mutation) &&
			MuiHeadlessStateFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessStateField.Reserved, out record.Reserved);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiHeadlessStateRecord record) where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiHeadlessStateRecord.Size)) return false;
		return MuiHeadlessStateFieldCursorCodec.TryWrite(ref platform, address,
			MuiHeadlessStateField.Magic, record.Magic) &&
			MuiHeadlessStateFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessStateField.Version, record.Version) &&
			MuiHeadlessStateFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessStateField.Classes, record.Classes.Raw) &&
			MuiHeadlessStateFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessStateField.Objects, record.Objects.Raw) &&
			MuiHeadlessStateFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessStateField.NextSequence, record.NextSequence) &&
			MuiHeadlessStateFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessStateField.NotifyDepth, record.NotifyDepth) &&
			MuiHeadlessStateFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessStateField.Mutation, record.Mutation) &&
			MuiHeadlessStateFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessStateField.Reserved, record.Reserved);
	}
}

// Fixed 28-byte class registry entry. The explicit reserved UWORD preserves
// the MorphOS-compatible gap between the instance-size word and the ULONG
// flags field.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessClassRecord
{
	internal const uint Size = 28;
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

internal static class MuiHeadlessClassFieldCursorCodec
{
	private static bool TryResolve(MuiHeadlessClassField field,
		out uint offset, out uint size)
	{
		offset = 0;
		size = 0;
		switch (field)
		{
			case MuiHeadlessClassField.Next:
				offset = unchecked((uint)MuiHeadlessLayout.ClassNext);
				size = 4;
				break;
			case MuiHeadlessClassField.Name:
				offset = unchecked((uint)MuiHeadlessLayout.ClassName);
				size = 4;
				break;
			case MuiHeadlessClassField.Boopsi:
				offset = unchecked((uint)MuiHeadlessLayout.ClassBoopsi);
				size = 4;
				break;
			case MuiHeadlessClassField.Super:
				offset = unchecked((uint)MuiHeadlessLayout.ClassSuper);
				size = 4;
				break;
			case MuiHeadlessClassField.InstanceSize:
				offset = unchecked((uint)MuiHeadlessLayout.ClassInstanceSize);
				size = 2;
				break;
			case MuiHeadlessClassField.Reserved:
				offset = unchecked((uint)(MuiHeadlessLayout.ClassInstanceSize + 2));
				size = 2;
				break;
			case MuiHeadlessClassField.Flags:
				offset = unchecked((uint)MuiHeadlessLayout.ClassFlags);
				size = 4;
				break;
			case MuiHeadlessClassField.ObjectCount:
				offset = unchecked((uint)MuiHeadlessLayout.ClassObjectCount);
				size = 4;
				break;
			default:
				return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiHeadlessClassFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset, out var size) ||
			cursor.Record.IsNull || cursor.Record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, size);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessClassField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiHeadlessClassFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address) ||
			!TryResolve(field, out _, out var size) || size != 4) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessClassField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiHeadlessClassFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address) ||
			!TryResolve(field, out _, out var size) || size != 4) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessClassField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiHeadlessClassFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address) ||
			!TryResolve(field, out _, out var size) || size != 2) return false;
		value = platform.ReadUInt16(address, 0);
		return true;
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessClassField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiHeadlessClassFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address) ||
			!TryResolve(field, out _, out var size) || size != 2) return false;
		platform.WriteUInt16(address, 0, value);
		return true;
	}
}

internal static class MuiHeadlessClassCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiHeadlessClassRecord record) where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiHeadlessClassRecord.Size)) return false;
		if (!MuiHeadlessClassFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiHeadlessClassField.Next, out var rawNext) ||
			!MuiHeadlessClassFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiHeadlessClassField.Name, out var rawName) ||
			!MuiHeadlessClassFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiHeadlessClassField.Boopsi, out var rawBoopsi) ||
			!MuiHeadlessClassFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiHeadlessClassField.Super, out var rawSuper)) return false;
		record.Next = APTR.FromPointer(rawNext);
		record.Name = APTR.FromPointer(rawName);
		record.Boopsi = APTR.FromPointer(rawBoopsi);
		record.Super = APTR.FromPointer(rawSuper);
		return MuiHeadlessClassFieldCursorCodec.TryReadUInt16(ref platform,
			address, MuiHeadlessClassField.InstanceSize, out record.InstanceSize) &&
			MuiHeadlessClassFieldCursorCodec.TryReadUInt16(ref platform, address,
				MuiHeadlessClassField.Reserved, out record.Reserved) &&
			MuiHeadlessClassFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiHeadlessClassField.Flags, out record.Flags) &&
			MuiHeadlessClassFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiHeadlessClassField.ObjectCount, out record.ObjectCount);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiHeadlessClassRecord record) where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiHeadlessClassRecord.Size)) return false;
		return MuiHeadlessClassFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiHeadlessClassField.Next, record.Next.Raw) &&
			MuiHeadlessClassFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiHeadlessClassField.Name, record.Name.Raw) &&
			MuiHeadlessClassFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiHeadlessClassField.Boopsi, record.Boopsi.Raw) &&
			MuiHeadlessClassFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiHeadlessClassField.Super, record.Super.Raw) &&
			MuiHeadlessClassFieldCursorCodec.TryWriteUInt16(ref platform, address,
				MuiHeadlessClassField.InstanceSize, record.InstanceSize) &&
			MuiHeadlessClassFieldCursorCodec.TryWriteUInt16(ref platform, address,
				MuiHeadlessClassField.Reserved, record.Reserved) &&
			MuiHeadlessClassFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiHeadlessClassField.Flags, record.Flags) &&
			MuiHeadlessClassFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiHeadlessClassField.ObjectCount, record.ObjectCount);
	}
}

// Fixed 64-byte headless object record. Pointer-bearing links are represented
// as APTR fields; only counters, flags, generations, and public scalar values
// remain ULONGs.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessObjectRecord
{
	internal const uint Size = 64;
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

internal static class MuiHeadlessObjectFieldCursorCodec
{
	private static bool TryResolve(MuiHeadlessObjectField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiHeadlessObjectField.Next:
				offset = unchecked((uint)MuiHeadlessLayout.ObjectNext);
				break;
			case MuiHeadlessObjectField.Boopsi:
				offset = unchecked((uint)MuiHeadlessLayout.ObjectBoopsi);
				break;
			case MuiHeadlessObjectField.Class:
				offset = unchecked((uint)MuiHeadlessLayout.ObjectClass);
				break;
			case MuiHeadlessObjectField.Attributes:
				offset = unchecked((uint)MuiHeadlessLayout.ObjectAttributes);
				break;
			case MuiHeadlessObjectField.Notifications:
				offset = unchecked((uint)MuiHeadlessLayout.ObjectNotifications);
				break;
			case MuiHeadlessObjectField.ChildrenHead:
				offset = unchecked((uint)MuiHeadlessLayout.ObjectChildrenHead);
				break;
			case MuiHeadlessObjectField.ChildrenTail:
				offset = unchecked((uint)MuiHeadlessLayout.ObjectChildrenTail);
				break;
			case MuiHeadlessObjectField.Parent:
				offset = unchecked((uint)MuiHeadlessLayout.ObjectParent);
				break;
			case MuiHeadlessObjectField.Stores:
				offset = unchecked((uint)MuiHeadlessLayout.ObjectStores);
				break;
			case MuiHeadlessObjectField.SemaphoreOwner:
				offset = unchecked((uint)MuiHeadlessLayout.ObjectSemaphoreOwner);
				break;
			case MuiHeadlessObjectField.SemaphoreDepth:
				offset = unchecked((uint)(MuiHeadlessLayout.ObjectSemaphoreOwner + 4));
				break;
			case MuiHeadlessObjectField.SemaphoreShared:
				offset = unchecked((uint)(MuiHeadlessLayout.ObjectSemaphoreOwner + 8));
				break;
			case MuiHeadlessObjectField.Flags:
				offset = unchecked((uint)MuiHeadlessLayout.ObjectFlags);
				break;
			case MuiHeadlessObjectField.Generation:
				offset = unchecked((uint)MuiHeadlessLayout.ObjectGeneration);
				break;
			case MuiHeadlessObjectField.ObjectId:
				offset = unchecked((uint)MuiHeadlessLayout.ObjectId);
				break;
			case MuiHeadlessObjectField.UserData:
				offset = unchecked((uint)MuiHeadlessLayout.ObjectUserData);
				break;
			default:
				offset = 0;
				return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiHeadlessObjectFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessObjectField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiHeadlessObjectFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessObjectField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiHeadlessObjectFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiHeadlessObjectCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiHeadlessObjectRecord record) where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiHeadlessObjectRecord.Size)) return false;
		if (!MuiHeadlessObjectFieldCursorCodec.TryRead(ref platform, address,
			MuiHeadlessObjectField.Next, out var rawNext) ||
			!MuiHeadlessObjectFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessObjectField.Boopsi, out var rawBoopsi) ||
			!MuiHeadlessObjectFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessObjectField.Class, out var rawClass) ||
			!MuiHeadlessObjectFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessObjectField.Attributes, out var rawAttributes) ||
			!MuiHeadlessObjectFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessObjectField.Notifications, out var rawNotifications) ||
			!MuiHeadlessObjectFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessObjectField.ChildrenHead, out var rawChildrenHead) ||
			!MuiHeadlessObjectFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessObjectField.ChildrenTail, out var rawChildrenTail) ||
			!MuiHeadlessObjectFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessObjectField.Parent, out var rawParent) ||
			!MuiHeadlessObjectFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessObjectField.Stores, out var rawStores) ||
			!MuiHeadlessObjectFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessObjectField.SemaphoreOwner, out var rawSemaphoreOwner))
			return false;
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
		return MuiHeadlessObjectFieldCursorCodec.TryRead(ref platform, address,
			MuiHeadlessObjectField.SemaphoreDepth, out record.SemaphoreDepth) &&
			MuiHeadlessObjectFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessObjectField.SemaphoreShared, out record.SemaphoreShared) &&
			MuiHeadlessObjectFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessObjectField.Flags, out record.Flags) &&
			MuiHeadlessObjectFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessObjectField.Generation, out record.Generation) &&
			MuiHeadlessObjectFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessObjectField.ObjectId, out record.ObjectId) &&
			MuiHeadlessObjectFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessObjectField.UserData, out record.UserData);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiHeadlessObjectRecord record) where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiHeadlessObjectRecord.Size)) return false;
		return MuiHeadlessObjectFieldCursorCodec.TryWrite(ref platform, address,
			MuiHeadlessObjectField.Next, record.Next.Raw) &&
			MuiHeadlessObjectFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessObjectField.Boopsi, record.Boopsi.Raw) &&
			MuiHeadlessObjectFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessObjectField.Class, record.Class.Raw) &&
			MuiHeadlessObjectFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessObjectField.Attributes, record.Attributes.Raw) &&
			MuiHeadlessObjectFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessObjectField.Notifications, record.Notifications.Raw) &&
			MuiHeadlessObjectFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessObjectField.ChildrenHead, record.ChildrenHead.Raw) &&
			MuiHeadlessObjectFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessObjectField.ChildrenTail, record.ChildrenTail.Raw) &&
			MuiHeadlessObjectFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessObjectField.Parent, record.Parent.Raw) &&
			MuiHeadlessObjectFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessObjectField.Stores, record.Stores.Raw) &&
			MuiHeadlessObjectFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessObjectField.SemaphoreOwner, record.SemaphoreOwner.Raw) &&
			MuiHeadlessObjectFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessObjectField.SemaphoreDepth, record.SemaphoreDepth) &&
			MuiHeadlessObjectFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessObjectField.SemaphoreShared, record.SemaphoreShared) &&
			MuiHeadlessObjectFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessObjectField.Flags, record.Flags) &&
			MuiHeadlessObjectFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessObjectField.Generation, record.Generation) &&
			MuiHeadlessObjectFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessObjectField.ObjectId, record.ObjectId) &&
			MuiHeadlessObjectFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessObjectField.UserData, record.UserData);
	}
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

internal static class MuiHeadlessAttributeFieldCursorCodec
{
	private static bool TryResolve(MuiHeadlessAttributeField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiHeadlessAttributeField.Next:
				offset = unchecked((uint)MuiHeadlessLayout.AttributeNext);
				break;
			case MuiHeadlessAttributeField.Id:
				offset = unchecked((uint)MuiHeadlessLayout.AttributeId);
				break;
			case MuiHeadlessAttributeField.Value:
				offset = unchecked((uint)MuiHeadlessLayout.AttributeValue);
				break;
			case MuiHeadlessAttributeField.Generation:
				offset = unchecked((uint)MuiHeadlessLayout.AttributeGeneration);
				break;
			default:
				offset = 0;
				return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiHeadlessAttributeFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessAttributeField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiHeadlessAttributeFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessAttributeField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiHeadlessAttributeFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiHeadlessAttributeCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiHeadlessAttributeRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiHeadlessAttributeRecord.Size)) return false;
		if (!MuiHeadlessAttributeFieldCursorCodec.TryRead(ref platform, address,
			MuiHeadlessAttributeField.Next, out var rawNext)) return false;
		record.Next = APTR.FromPointer(rawNext);
		return MuiHeadlessAttributeFieldCursorCodec.TryRead(ref platform, address,
			MuiHeadlessAttributeField.Id, out record.Id) &&
			MuiHeadlessAttributeFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessAttributeField.Value, out record.Value) &&
			MuiHeadlessAttributeFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessAttributeField.Generation, out record.Generation);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiHeadlessAttributeRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiHeadlessAttributeRecord.Size)) return false;
		return MuiHeadlessAttributeFieldCursorCodec.TryWrite(ref platform, address,
			MuiHeadlessAttributeField.Next, record.Next.Raw) &&
			MuiHeadlessAttributeFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessAttributeField.Id, record.Id) &&
			MuiHeadlessAttributeFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessAttributeField.Value, record.Value) &&
			MuiHeadlessAttributeFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessAttributeField.Generation, record.Generation);
	}
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

internal static class MuiHeadlessChildFieldCursorCodec
{
	private static bool TryResolve(MuiHeadlessChildField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiHeadlessChildField.Next:
				offset = unchecked((uint)MuiHeadlessLayout.ChildNext);
				break;
			case MuiHeadlessChildField.Previous:
				offset = unchecked((uint)MuiHeadlessLayout.ChildPrevious);
				break;
			case MuiHeadlessChildField.Object:
				offset = unchecked((uint)MuiHeadlessLayout.ChildObject);
				break;
			case MuiHeadlessChildField.Owner:
				offset = unchecked((uint)MuiHeadlessLayout.ChildOwner);
				break;
			default:
				offset = 0;
				return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiHeadlessChildFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessChildField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiHeadlessChildFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessChildField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiHeadlessChildFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiHeadlessChildCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiHeadlessChildRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiHeadlessChildRecord.Size)) return false;
		if (!MuiHeadlessChildFieldCursorCodec.TryRead(ref platform, address,
			MuiHeadlessChildField.Next, out var rawNext) ||
			!MuiHeadlessChildFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessChildField.Previous, out var rawPrevious) ||
			!MuiHeadlessChildFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessChildField.Object, out var rawObject) ||
			!MuiHeadlessChildFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessChildField.Owner, out var rawOwner)) return false;
		record.Next = APTR.FromPointer(rawNext);
		record.Previous = APTR.FromPointer(rawPrevious);
		record.Object = APTR.FromPointer(rawObject);
		record.Owner = APTR.FromPointer(rawOwner);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiHeadlessChildRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiHeadlessChildRecord.Size)) return false;
		return MuiHeadlessChildFieldCursorCodec.TryWrite(ref platform, address,
			MuiHeadlessChildField.Next, record.Next.Raw) &&
			MuiHeadlessChildFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessChildField.Previous, record.Previous.Raw) &&
			MuiHeadlessChildFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessChildField.Object, record.Object.Raw) &&
			MuiHeadlessChildFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessChildField.Owner, record.Owner.Raw);
	}
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

internal static class MuiHeadlessNotificationFieldCursorCodec
{
	private static bool TryResolve(MuiHeadlessNotificationField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiHeadlessNotificationField.Next:
				offset = unchecked((uint)MuiHeadlessLayout.NotificationNext);
				break;
			case MuiHeadlessNotificationField.Sequence:
				offset = unchecked((uint)MuiHeadlessLayout.NotificationSequence);
				break;
			case MuiHeadlessNotificationField.TriggerAttribute:
				offset = unchecked((uint)MuiHeadlessLayout.NotificationTriggerAttribute);
				break;
			case MuiHeadlessNotificationField.TriggerValue:
				offset = unchecked((uint)MuiHeadlessLayout.NotificationTriggerValue);
				break;
			case MuiHeadlessNotificationField.Destination:
				offset = unchecked((uint)MuiHeadlessLayout.NotificationDestination);
				break;
			case MuiHeadlessNotificationField.FollowCount:
				offset = unchecked((uint)MuiHeadlessLayout.NotificationFollowCount);
				break;
			case MuiHeadlessNotificationField.Flags:
				offset = unchecked((uint)MuiHeadlessLayout.NotificationFlags);
				break;
			case MuiHeadlessNotificationField.Reserved:
				offset = unchecked((uint)(MuiHeadlessLayout.NotificationPayload - 4));
				break;
			default:
				offset = 0;
				return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiHeadlessNotificationFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessNotificationField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiHeadlessNotificationFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiHeadlessNotificationField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiHeadlessNotificationFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
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

internal static class MuiHeadlessNotificationPayloadCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiHeadlessNotificationPayloadCursor cursor, out APTR payload)
		where TPlatform : struct, IMuiGuestMemory
	{
		payload = APTR.Null;
		if (cursor.Record.IsNull || cursor.Record.Raw >
			uint.MaxValue - MuiHeadlessNotificationRecord.Size ||
			cursor.PayloadBytes > uint.MaxValue -
			MuiHeadlessNotificationRecord.Size) return false;
		var total = MuiHeadlessNotificationRecord.Size + cursor.PayloadBytes;
		if (!platform.IsMapped(cursor.Record, total)) return false;
		payload = APTR.FromPointer(cursor.Record.Raw +
			MuiHeadlessNotificationRecord.Size);
		return payload.Raw <= uint.MaxValue - cursor.PayloadBytes;
	}
}

internal static class MuiHeadlessNotificationCodec
{
	internal static bool TryGetPayload<TPlatform>(ref TPlatform platform,
		APTR address, uint payloadBytes, out APTR payload)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiHeadlessNotificationPayloadCursor);
		cursor.Record = address;
		cursor.PayloadBytes = payloadBytes;
		return MuiHeadlessNotificationPayloadCursorCodec.TryGetAddress(
			ref platform, cursor, out payload);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiHeadlessNotificationRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiHeadlessNotificationRecord.Size)) return false;
		if (!MuiHeadlessNotificationFieldCursorCodec.TryRead(ref platform,
			address, MuiHeadlessNotificationField.Next, out var rawNext) ||
			!MuiHeadlessNotificationFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessNotificationField.Destination, out var rawDestination))
			return false;
		record.Next = APTR.FromPointer(rawNext);
		record.Destination = APTR.FromPointer(rawDestination);
		return MuiHeadlessNotificationFieldCursorCodec.TryRead(ref platform,
			address, MuiHeadlessNotificationField.Sequence, out record.Sequence) &&
			MuiHeadlessNotificationFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessNotificationField.TriggerAttribute,
				out record.TriggerAttribute) &&
			MuiHeadlessNotificationFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessNotificationField.TriggerValue, out record.TriggerValue) &&
			MuiHeadlessNotificationFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessNotificationField.FollowCount, out record.FollowCount) &&
			MuiHeadlessNotificationFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessNotificationField.Flags, out record.Flags) &&
			MuiHeadlessNotificationFieldCursorCodec.TryRead(ref platform, address,
				MuiHeadlessNotificationField.Reserved, out record.Reserved);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiHeadlessNotificationRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiHeadlessNotificationRecord.Size)) return false;
		return MuiHeadlessNotificationFieldCursorCodec.TryWrite(ref platform,
			address, MuiHeadlessNotificationField.Next, record.Next.Raw) &&
			MuiHeadlessNotificationFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessNotificationField.Sequence, record.Sequence) &&
			MuiHeadlessNotificationFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessNotificationField.TriggerAttribute,
				record.TriggerAttribute) &&
			MuiHeadlessNotificationFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessNotificationField.TriggerValue, record.TriggerValue) &&
			MuiHeadlessNotificationFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessNotificationField.Destination, record.Destination.Raw) &&
			MuiHeadlessNotificationFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessNotificationField.FollowCount, record.FollowCount) &&
			MuiHeadlessNotificationFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessNotificationField.Flags, record.Flags) &&
			MuiHeadlessNotificationFieldCursorCodec.TryWrite(ref platform, address,
				MuiHeadlessNotificationField.Reserved, record.Reserved);
	}
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

internal static class MuiStoreRecordFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStoreRecordFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiStoreRecordField.Next:
				offset = unchecked((uint)MuiHeadlessLayout.StoreNext);
				break;
			case MuiStoreRecordField.Key:
				offset = unchecked((uint)MuiHeadlessLayout.StoreKey);
				break;
			case MuiStoreRecordField.Data:
				offset = unchecked((uint)MuiHeadlessLayout.StoreData);
				break;
			case MuiStoreRecordField.Length:
				offset = unchecked((uint)MuiHeadlessLayout.StoreLength);
				break;
			case MuiStoreRecordField.Flags:
				offset = unchecked((uint)MuiHeadlessLayout.StoreFlags);
				break;
			case MuiStoreRecordField.Generation:
				offset = unchecked((uint)MuiHeadlessLayout.StoreGeneration);
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
		APTR record, MuiStoreRecordField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStoreRecordFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiStoreRecordField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStoreRecordFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStoreRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStoreRecord record) where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address, MuiStoreRecord.Size))
			return false;
		if (!MuiStoreRecordFieldCursorCodec.TryRead(ref platform, address,
			MuiStoreRecordField.Next, out var rawNext)) return false;
		record.Next = APTR.FromPointer(rawNext);
		if (!MuiStoreRecordFieldCursorCodec.TryRead(ref platform, address,
			MuiStoreRecordField.Data, out var rawData)) return false;
		record.Data = APTR.FromPointer(rawData);
		return MuiStoreRecordFieldCursorCodec.TryRead(ref platform, address,
			MuiStoreRecordField.Key, out record.Key) &&
			MuiStoreRecordFieldCursorCodec.TryRead(ref platform, address,
				MuiStoreRecordField.Length, out record.Length) &&
			MuiStoreRecordFieldCursorCodec.TryRead(ref platform, address,
				MuiStoreRecordField.Flags, out record.Flags) &&
			MuiStoreRecordFieldCursorCodec.TryRead(ref platform, address,
				MuiStoreRecordField.Generation, out record.Generation);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStoreRecord record) where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address, MuiStoreRecord.Size))
			return false;
		return MuiStoreRecordFieldCursorCodec.TryWrite(ref platform, address,
			MuiStoreRecordField.Next, record.Next.Raw) &&
			MuiStoreRecordFieldCursorCodec.TryWrite(ref platform, address,
				MuiStoreRecordField.Key, record.Key) &&
			MuiStoreRecordFieldCursorCodec.TryWrite(ref platform, address,
				MuiStoreRecordField.Data, record.Data.Raw) &&
			MuiStoreRecordFieldCursorCodec.TryWrite(ref platform, address,
				MuiStoreRecordField.Length, record.Length) &&
			MuiStoreRecordFieldCursorCodec.TryWrite(ref platform, address,
				MuiStoreRecordField.Flags, record.Flags) &&
			MuiStoreRecordFieldCursorCodec.TryWrite(ref platform, address,
				MuiStoreRecordField.Generation, record.Generation);
	}
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
