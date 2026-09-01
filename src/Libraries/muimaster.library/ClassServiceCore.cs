/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Fixed guest-memory layout for the MG09 class-service gateway. The service
// owns its own guest-resident state block and a singly linked list of lease
// records; it never allocates on the managed heap and holds no managed data.
internal static class MuiClassServiceLayout
{
	public const uint Magic = 0x4D554339;   // "MUI9"
	public const uint Version = 1;

	// Service state block.
	public const uint StateSize = 16;
	public const int StateHead = 4;           // head of the lease record list
	public const int StateHeadless = 8;       // frozen headless registry state
	public const int StateGeneration = 12;

	// Lease / custom-class record.
	public const uint RecordSize = 44;
	public const int RecordNext = 0;
	public const int RecordFlags = 4;
	public const int RecordClassId = 8;       // classid C-string (0 for private)
	public const int RecordBoopsi = 12;       // struct IClass*
	public const int RecordLibraryBase = 16;  // loader lease (0 when none)
	public const int RecordRefCount = 20;     // GetClass/FreeClass reference count
	public const int RecordHeadlessClass = 24;// frozen registry record (external)
	public const int RecordCustomClass = 28;  // MUI_CustomClass block (custom)
	public const int RecordSuperService = 32; // super lease record (child link)
	public const int RecordObjectCount = 36;  // outstanding objects (custom)
	public const int RecordChildCount = 40;   // outstanding sub classes (custom)

	// Record flag bits.
	public const uint FlagExternal = 1;       // opened through the mui/<id> loader
	public const uint FlagBuiltin = 2;         // resolved from the headless registry
	public const uint FlagCustom = 4;          // created by CreateCustomClass
	public const uint FlagPublic = 8;          // public custom class (A6 base bound)
	public const uint FlagOwnsNamedSuper = 16; // holds a GetClass lease on its super
	public const uint FlagOwnsClassId = 32;    // owns the copied class-id string

	// struct MUI_CustomClass (libraries/mui.h) — exactly seven APTR fields.
	public const uint CustomClassSize = 28;
	public const int MccUserData = 0;
	public const int MccUtilityBase = 4;
	public const int MccDosBase = 8;
	public const int MccGfxBase = 12;
	public const int MccIntuitionBase = 16;
	public const int MccSuper = 20;
	public const int MccClass = 24;

	// Bounded "mui/<classid>" construction.
	public const uint ClassIdMaximum = 255;
	public const uint MaxInstanceSize = 65535;
	public const uint MaximumTraversal = 65535;
}

// Named cursor for bounded class-id C strings used by the class-service
// loader. Consumers carry only a guest STRPTR and logical byte index; this
// adapter owns the MorphOS class-id bound, overflow guard, and mapped-byte
// admission instead of forming raw STRPTR + index expressions at call sites.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiClassServiceStringByteCursor
{
	internal const uint MaximumLength = MuiClassServiceLayout.ClassIdMaximum + 1;
	internal APTR Text;
	internal uint Index;
}

internal static class MuiClassServiceStringByteCursorCodec
{
	internal static bool TryReadAt<TPlatform>(ref TPlatform platform,
		APTR text, int index, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (index < 0) return false;
		var cursor = default(MuiClassServiceStringByteCursor);
		cursor.Text = text;
		cursor.Index = (uint)index;
		return TryReadByte(ref platform, cursor, out value);
	}

	internal static bool TryGetByte<TPlatform>(ref TPlatform platform,
		MuiClassServiceStringByteCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Text;
		shared.Index = cursor.Index;
		shared.Limit = MuiClassServiceStringByteCursor.MaximumLength;
		return MuiCStringByteCursorCodec.TryGetAddress(ref platform, shared,
			out address);
	}

	internal static bool TryReadByte<TPlatform>(ref TPlatform platform,
		MuiClassServiceStringByteCursor cursor, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetByte(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt8(address, 0);
		return true;
	}

	internal static bool TryWriteByte<TPlatform>(ref TPlatform platform,
		MuiClassServiceStringByteCursor cursor, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetByte(ref platform, cursor, out var address)) return false;
		platform.WriteUInt8(address, 0, value);
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiClassServiceStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint HeadOffset = 4;
	internal const uint HeadlessOffset = 8;
	internal const uint GenerationOffset = 12;
	internal uint Magic;
	internal APTR Head;
	internal APTR Headless;
	internal uint Generation;
}

internal enum MuiClassRecordKind : byte
{
	State,
	Lease,
	CustomClass,
}

internal enum MuiClassRecordField : byte
{
	Magic,
	Head,
	Headless,
	Generation,
	Next,
	Flags,
	ClassId,
	Boopsi,
	LibraryBase,
	RefCount,
	HeadlessClass,
	CustomClass,
	SuperService,
	ObjectCount,
	ChildCount,
	UserData,
	UtilityBase,
	DosBase,
	GfxBase,
	IntuitionBase,
	Super,
	Class,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiClassRecordFieldCursor
{
	internal APTR Address;
	internal MuiClassRecordKind Record;
	internal MuiClassRecordField Field;
}

// Struct-first guest-memory adapter for class-service state, lease, and
// MUI_CustomClass records. Named record layouts own the complete bounds and
// packed field translation used by the production codecs.
internal static class MuiClassRecordMemoryCodec
{
	private static bool TryResolve(MuiClassRecordKind record,
		MuiClassRecordField field, out uint offset, out uint size)
	{
		offset = 0;
		size = 0;
		switch (record)
		{
			case MuiClassRecordKind.State:
				size = MuiClassServiceStateRecord.Size;
				offset = field switch
				{
					MuiClassRecordField.Magic => MuiClassServiceStateRecord.MagicOffset,
					MuiClassRecordField.Head => MuiClassServiceStateRecord.HeadOffset,
					MuiClassRecordField.Headless => MuiClassServiceStateRecord.HeadlessOffset,
					MuiClassRecordField.Generation => MuiClassServiceStateRecord.GenerationOffset,
					_ => uint.MaxValue,
				};
				break;
			case MuiClassRecordKind.Lease:
				size = MuiClassServiceLeaseRecord.Size;
				offset = field switch
				{
					MuiClassRecordField.Next => MuiClassServiceLeaseRecord.NextOffset,
					MuiClassRecordField.Flags => MuiClassServiceLeaseRecord.FlagsOffset,
					MuiClassRecordField.ClassId => MuiClassServiceLeaseRecord.ClassIdOffset,
					MuiClassRecordField.Boopsi => MuiClassServiceLeaseRecord.BoopsiOffset,
					MuiClassRecordField.LibraryBase => MuiClassServiceLeaseRecord.LibraryBaseOffset,
					MuiClassRecordField.RefCount => MuiClassServiceLeaseRecord.RefCountOffset,
					MuiClassRecordField.HeadlessClass => MuiClassServiceLeaseRecord.HeadlessClassOffset,
					MuiClassRecordField.CustomClass => MuiClassServiceLeaseRecord.CustomClassOffset,
					MuiClassRecordField.SuperService => MuiClassServiceLeaseRecord.SuperServiceOffset,
					MuiClassRecordField.ObjectCount => MuiClassServiceLeaseRecord.ObjectCountOffset,
					MuiClassRecordField.ChildCount => MuiClassServiceLeaseRecord.ChildCountOffset,
					_ => uint.MaxValue,
				};
				break;
			case MuiClassRecordKind.CustomClass:
				size = MuiCustomClassRecord.Size;
				offset = field switch
				{
					MuiClassRecordField.UserData => MuiCustomClassRecord.UserDataOffset,
					MuiClassRecordField.UtilityBase => MuiCustomClassRecord.UtilityBaseOffset,
					MuiClassRecordField.DosBase => MuiCustomClassRecord.DosBaseOffset,
					MuiClassRecordField.GfxBase => MuiCustomClassRecord.GfxBaseOffset,
					MuiClassRecordField.IntuitionBase => MuiCustomClassRecord.IntuitionBaseOffset,
					MuiClassRecordField.Super => MuiCustomClassRecord.SuperOffset,
					MuiClassRecordField.Class => MuiCustomClassRecord.ClassOffset,
					_ => uint.MaxValue,
				};
				break;
			default:
				offset = uint.MaxValue;
				break;
		}
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR recordAddress, MuiClassRecordKind record, MuiClassRecordField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(record, field, out var offset, out var size) ||
			recordAddress.IsNull || recordAddress.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(recordAddress, size)) return false;
		address = APTR.FromPointer(recordAddress.Raw + offset);
		return platform.IsMapped(address, MuiClassServiceStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiClassRecordKind record, MuiClassRecordField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, address, record, field,
			out var fieldAddress))
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiClassRecordKind record, MuiClassRecordField field,
		uint value) where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, address, record, field,
			out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for typed cursor callers; production access
// routes through MuiClassRecordMemoryCodec with named record layouts.
internal static class MuiClassRecordFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiClassRecordFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiClassRecordMemoryCodec.TryGetAddress(ref platform, cursor.Address,
			cursor.Record, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiClassRecordKind record, MuiClassRecordField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory =>
		MuiClassRecordMemoryCodec.TryReadUInt32(ref platform, address, record,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiClassRecordKind record, MuiClassRecordField field,
		uint value) where TPlatform : struct, IMuiGuestMemory =>
		MuiClassRecordMemoryCodec.TryWriteUInt32(ref platform, address, record,
			field, value);
}

// Canonical sequential codec for the complete class-service state record.
// The field resolver above remains a compatibility seam for diagnostics;
// class registration and lease management exchange this named struct.
internal static class MuiClassServiceStateStructCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiClassServiceStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiClassServiceStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var head) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var headless) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Generation) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		record.Head = APTR.FromPointer(head);
		record.Headless = APTR.FromPointer(headless);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiClassServiceStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiClassServiceStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Magic) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Head.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Headless.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Generation)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiClassServiceStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiClassServiceStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiClassServiceStateStructCodec.TryRead(ref platform, address,
			out record);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiClassServiceStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiClassServiceStateStructCodec.Write(ref platform, address, record);
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiClassServiceLeaseRecord
{
	internal const uint Size = 44;
	internal const uint FieldSize = 4;
	internal const uint NextOffset = 0;
	internal const uint FlagsOffset = 4;
	internal const uint ClassIdOffset = 8;
	internal const uint BoopsiOffset = 12;
	internal const uint LibraryBaseOffset = 16;
	internal const uint RefCountOffset = 20;
	internal const uint HeadlessClassOffset = 24;
	internal const uint CustomClassOffset = 28;
	internal const uint SuperServiceOffset = 32;
	internal const uint ObjectCountOffset = 36;
	internal const uint ChildCountOffset = 40;
	internal APTR Next;
	internal uint Flags;
	internal APTR ClassId;
	internal APTR Boopsi;
	internal APTR LibraryBase;
	internal uint RefCount;
	internal APTR HeadlessClass;
	internal APTR CustomClass;
	internal APTR SuperService;
	internal uint ObjectCount;
	internal uint ChildCount;
}

internal static class MuiClassServiceLeaseStructCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiClassServiceLeaseRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiClassServiceLeaseRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var next) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Flags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var classId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var boopsi) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var libraryBase) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.RefCount) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var headlessClass) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var customClass) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var superService) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.ObjectCount) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.ChildCount) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		record.Next = APTR.FromPointer(next);
		record.ClassId = APTR.FromPointer(classId);
		record.Boopsi = APTR.FromPointer(boopsi);
		record.LibraryBase = APTR.FromPointer(libraryBase);
		record.HeadlessClass = APTR.FromPointer(headlessClass);
		record.CustomClass = APTR.FromPointer(customClass);
		record.SuperService = APTR.FromPointer(superService);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiClassServiceLeaseRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiClassServiceLeaseRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Next.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Flags) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.ClassId.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Boopsi.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.LibraryBase.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.RefCount) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.HeadlessClass.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.CustomClass.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.SuperService.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.ObjectCount) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.ChildCount)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiClassServiceLeaseCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiClassServiceLeaseRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiClassServiceLeaseStructCodec.TryRead(ref platform, address,
			out record);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiClassServiceLeaseRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiClassServiceLeaseStructCodec.Write(ref platform, address, record);
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCustomClassRecord
{
	internal const uint Size = 28;
	internal const uint FieldSize = 4;
	internal const uint UserDataOffset = 0;
	internal const uint UtilityBaseOffset = 4;
	internal const uint DosBaseOffset = 8;
	internal const uint GfxBaseOffset = 12;
	internal const uint IntuitionBaseOffset = 16;
	internal const uint SuperOffset = 20;
	internal const uint ClassOffset = 24;
	internal APTR UserData;
	internal APTR UtilityBase;
	internal APTR DosBase;
	internal APTR GfxBase;
	internal APTR IntuitionBase;
	internal APTR Super;
	internal APTR Class;
}

internal static class MuiCustomClassStructCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiCustomClassRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiCustomClassRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var userData) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var utilityBase) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var dosBase) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var gfxBase) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var intuitionBase) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var super) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var @class) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		record.UserData = APTR.FromPointer(userData);
		record.UtilityBase = APTR.FromPointer(utilityBase);
		record.DosBase = APTR.FromPointer(dosBase);
		record.GfxBase = APTR.FromPointer(gfxBase);
		record.IntuitionBase = APTR.FromPointer(intuitionBase);
		record.Super = APTR.FromPointer(super);
		record.Class = APTR.FromPointer(@class);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiCustomClassRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiCustomClassRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.UserData.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.UtilityBase.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.DosBase.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.GfxBase.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.IntuitionBase.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Super.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Class.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiCustomClassCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiCustomClassRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCustomClassStructCodec.TryRead(ref platform, address, out record);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiCustomClassRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCustomClassStructCodec.Write(ref platform, address, record);
}

// Scalar qualification surface for the class-service state, lease, and
// MUI_CustomClass records. Live class-service lifecycle remains in
// MuiClassServiceCore; this seam proves the fixed named layouts independently.
public static class MuiClassServiceRecordPacketCore
{
	public static bool WriteState<TPlatform>(ref TPlatform platform, APTR address,
		uint magic, APTR head, APTR headless, uint generation)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiClassServiceStateRecord record = default;
		record.Magic = magic;
		record.Head = head;
		record.Headless = headless;
		record.Generation = generation;
		return MuiClassServiceStateCodec.Write(ref platform, address, record);
	}

	public static uint DispatchState<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiClassServiceStateCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Magic ^ record.Head.Raw ^ record.Headless.Raw ^
			record.Generation;
	}

	public static bool WriteLease<TPlatform>(ref TPlatform platform, APTR address,
		APTR next, uint flags, APTR classId, APTR boopsi, APTR libraryBase,
		uint refCount, APTR headlessClass, APTR customClass, APTR superService,
		uint objectCount, uint childCount)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiClassServiceLeaseRecord record = default;
		record.Next = next;
		record.Flags = flags;
		record.ClassId = classId;
		record.Boopsi = boopsi;
		record.LibraryBase = libraryBase;
		record.RefCount = refCount;
		record.HeadlessClass = headlessClass;
		record.CustomClass = customClass;
		record.SuperService = superService;
		record.ObjectCount = objectCount;
		record.ChildCount = childCount;
		return MuiClassServiceLeaseCodec.Write(ref platform, address, record);
	}

	public static uint DispatchLease<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiClassServiceLeaseCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Next.Raw ^ record.Flags ^ record.ClassId.Raw ^
			record.Boopsi.Raw ^ record.LibraryBase.Raw ^ record.RefCount ^
			record.HeadlessClass.Raw ^ record.CustomClass.Raw ^
			record.SuperService.Raw ^ record.ObjectCount ^ record.ChildCount;
	}

	public static bool WriteCustomClass<TPlatform>(ref TPlatform platform,
		APTR address, APTR super, APTR boopsi)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiCustomClassRecord record = default;
		record.Super = super;
		record.Class = boopsi;
		return MuiCustomClassCodec.Write(ref platform, address, record);
	}

	public static uint DispatchCustomClass<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiCustomClassCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Super.Raw ^ record.Class.Raw;
	}
}

// The MG09 custom-class and external-class service gateway. Implements the
// documented behaviour of MUI_GetClass, MUI_FreeClass, MUI_CreateCustomClass
// and MUI_DeleteCustomClass over guest-resident state, with no managed
// allocations or runtime dependencies. The frozen generic dispatchers and
// cores are not modified; external classes are published into the existing
// headless registry through its public RegisterExternalClass/DeleteClass entry
// points so object counting and disposal stay consistent.
public static class MuiClassServiceCore
{
	private enum LeaseField
	{
		Boopsi,
		CustomClass
	}

	public static bool Initialize<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR headlessState)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (serviceState.IsNull ||
			!platform.IsMapped(serviceState, MuiClassServiceStateRecord.Size) ||
			headlessState.IsNull ||
			!MuiHeadlessStateCodec.TryReadStructural(ref platform, headlessState,
				out _) ||
			!MuiHeadlessMemory.Ensure(ref platform, headlessState))
			return false;
		platform.Clear(serviceState, MuiClassServiceStateRecord.Size);
		MuiClassServiceStateRecord serviceValue = default;
		serviceValue.Magic = MuiClassServiceLayout.Magic;
		serviceValue.Headless = headlessState;
		serviceValue.Generation = MuiClassServiceLayout.Version;
		return MuiClassServiceStateCodec.Write(ref platform, serviceState,
			serviceValue);
	}

	// MUI_GetClass(classid). Case-sensitive. Returns a struct IClass* on
	// success. An already-leased class bumps its reference count; a class found
	// in the headless registry is adopted as a builtin lease; otherwise the
	// class is loaded through the mui/<classid> library, resolved, published and
	// leased. Every failure path is atomic and leaks nothing.
	public static APTR GetClass<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR classId)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || classId.IsNull)
			return APTR.Null;

		var lease = FindLeaseByClassId(ref platform, serviceState, classId);
		if (lease.IsNotNull)
		{
			if (!Reference(ref platform, lease)) return APTR.Null;
			return MuiClassServiceLeaseCodec.TryRead(ref platform, lease,
				out var leaseValue) ? leaseValue.Boopsi : APTR.Null;
		}

		var headless = Headless(ref platform, serviceState);
		var existing = MuiHeadlessObjectCore.FindClassByName(ref platform, headless,
			classId);
		if (existing.IsNotNull)
		{
			var boopsi = MuiHeadlessObjectCore.ClassPointer(ref platform, existing);
			if (boopsi.IsNull) return APTR.Null;
			var record = NewLease(ref platform, serviceState, classId, boopsi,
				APTR.Null, existing, MuiClassServiceLayout.FlagBuiltin);
			if (record.IsNull) return APTR.Null;
			return boopsi;
		}

		return LoadExternal(ref platform, serviceState, headless, classId);
	}

	// MUI_FreeClass(classptr). Releases one reference. When the final reference
	// is dropped the loader lease is closed and the external registry record is
	// removed; if outstanding objects prevent removal the reference is restored
	// and the call fails without freeing anything.
	public static bool FreeClass<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR classPointer)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || classPointer.IsNull) return false;
		var lease = FindLeaseByBoopsi(ref platform, serviceState, classPointer);
		if (lease.IsNull) return false;
		if (!MuiClassServiceLeaseCodec.TryRead(ref platform, lease,
			out var leaseValue)) return false;
		var count = leaseValue.RefCount;
		if (count == 0) return false;
		var flags = leaseValue.Flags;
		if (count == 1 && leaseValue.ObjectCount != 0 &&
			(flags & (MuiClassServiceLayout.FlagBuiltin |
				MuiClassServiceLayout.FlagExternal)) != 0)
			return false;
		if (count > 1)
		{
			leaseValue.RefCount = count - 1;
			return MuiClassServiceLeaseCodec.Write(ref platform, lease,
				leaseValue);
		}

		if ((flags & MuiClassServiceLayout.FlagExternal) != 0)
		{
			var headless = Headless(ref platform, serviceState);
			var headlessRecord = leaseValue.HeadlessClass;
			if (!MuiHeadlessObjectCore.DeleteClass(ref platform, headless,
				headlessRecord))
				return false;   // outstanding objects: leave the reference in place
			var library = leaseValue.LibraryBase;
			if (library.IsNotNull) platform.CloseLibrary(library);
		}

		leaseValue.RefCount = 0;
		if (!MuiClassServiceLeaseCodec.Write(ref platform, lease, leaseValue))
			return false;
		return UnlinkAndFreeLease(ref platform, serviceState, lease);
	}

	// MUI_CreateCustomClass(base, supername, supermcc, datasize, dispfunc).
	// Enforces exactly one super-class source, a non-null dispatcher and a
	// bounded data size, resolves the super class, binds the A6 library base for
	// public classes, and publishes an exact 28-byte MUI_CustomClass structure.
	// Returns the MUI_CustomClass pointer or Null; failures are atomic.
	public static APTR CreateCustomClass<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR libraryBase, APTR superClassId, APTR superMcc,
		int dataSize, APTR dispatcher)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || dispatcher.IsNull) return APTR.Null;

		// Exactly one super-class source: a name or a private mcc, never both,
		// never neither.
		var hasName = superClassId.IsNotNull;
		var hasMcc = superMcc.IsNotNull;
		if (hasName == hasMcc) return APTR.Null;

		// Bounded data size (the boopsi instance size is a UWORD).
		if (dataSize < 0 || (uint)dataSize > MuiClassServiceLayout.MaxInstanceSize)
			return APTR.Null;

		APTR superClass;
		var superService = APTR.Null;
		var ownsNamedSuper = false;
		if (hasName)
		{
			superClass = GetClass(ref platform, serviceState, superClassId);
			if (superClass.IsNull) return APTR.Null;
			ownsNamedSuper = true;
			superService = FindLeaseByBoopsi(ref platform, serviceState, superClass);
		}
		else
		{
			if (!MuiCustomClassCodec.TryRead(ref platform, superMcc,
				out var superMccValue))
				return APTR.Null;
			superClass = superMccValue.Class;
			if (superClass.IsNull) return APTR.Null;
			superService = FindLeaseByCustomClass(ref platform, serviceState,
				superMcc);
		}

		var isPublic = libraryBase.IsNotNull;
		// Keep the two capability calls in separate basic blocks. CopperSharp's
		// freestanding lowering represents APTR.Null as a scalar zero at some
		// call sites; merging that value with an APTR argument creates an
		// incompatible evaluation-stack type. The split is semantically identical
		// and leaves both branches entirely guest/native.
		APTR boopsi;
		if (isPublic)
			boopsi = platform.MakeCustomClass(superClass, (ushort)dataSize,
				dispatcher, libraryBase);
		else
			boopsi = platform.MakeCustomClass(superClass, (ushort)dataSize,
				dispatcher, APTR.Null);
		if (boopsi.IsNull)
		{
			if (ownsNamedSuper) FreeClass(ref platform, serviceState, superClass);
			return APTR.Null;
		}

		var mcc = MuiHeadlessMemory.Allocate(ref platform,
			MuiCustomClassRecord.Size);
		if (mcc.IsNull)
		{
			platform.FreeCustomClass(boopsi);
			if (ownsNamedSuper) FreeClass(ref platform, serviceState, superClass);
			return APTR.Null;
		}

		var flags = MuiClassServiceLayout.FlagCustom |
			(isPublic ? MuiClassServiceLayout.FlagPublic : 0u) |
			(ownsNamedSuper ? MuiClassServiceLayout.FlagOwnsNamedSuper : 0u);
		var record = NewLease(ref platform, serviceState, APTR.Null, boopsi,
			APTR.Null, APTR.Null, flags);
		if (record.IsNull)
		{
			platform.Clear(mcc, MuiCustomClassRecord.Size);
			platform.Free(mcc, MuiCustomClassRecord.Size);
			platform.FreeCustomClass(boopsi);
			if (ownsNamedSuper) FreeClass(ref platform, serviceState, superClass);
			return APTR.Null;
		}

		MuiCustomClassRecord customValue = default;
		customValue.Super = superClass;
		customValue.Class = boopsi;
		if (!MuiCustomClassCodec.Write(ref platform, mcc, customValue))
		{
			platform.Clear(mcc, MuiCustomClassRecord.Size);
			platform.Free(mcc, MuiCustomClassRecord.Size);
			platform.FreeCustomClass(boopsi);
			if (ownsNamedSuper) FreeClass(ref platform, serviceState, superClass);
			return APTR.Null;
		}
		if (!MuiClassServiceLeaseCodec.TryRead(ref platform, record,
			out var recordValue)) return APTR.Null;
		recordValue.CustomClass = mcc;
		recordValue.SuperService = superService;
		if (!MuiClassServiceLeaseCodec.Write(ref platform, record, recordValue))
			return APTR.Null;
		if (superService.IsNotNull)
		{
			if (!MuiClassServiceLeaseCodec.TryRead(ref platform, superService,
				out var superValue)) return APTR.Null;
			if (superValue.ChildCount == uint.MaxValue) return APTR.Null;
			superValue.ChildCount++;
			if (!MuiClassServiceLeaseCodec.Write(ref platform, superService,
				superValue)) return APTR.Null;
		}
		return mcc;
	}

	// MUI_DeleteCustomClass(mcc). Fails atomically (freeing nothing) when the
	// class still has outstanding objects or sub classes. On success it frees
	// the boopsi class, releases any named super-class lease, decrements the
	// super's child count and frees the MUI_CustomClass structure.
	public static bool DeleteCustomClass<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR mcc)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || mcc.IsNull) return false;
		var record = FindLeaseByCustomClass(ref platform, serviceState, mcc);
		if (record.IsNull) return false;
		if (!MuiClassServiceLeaseCodec.TryRead(ref platform, record,
			out var recordValue)) return false;
		if (recordValue.ObjectCount != 0 || recordValue.ChildCount != 0)
			return false;

		var boopsi = recordValue.Boopsi;
		if (boopsi.IsNull || !platform.FreeCustomClass(boopsi)) return false;

		var flags = recordValue.Flags;
		var superService = recordValue.SuperService;
		if (!MuiCustomClassCodec.TryRead(ref platform, mcc,
			out var customValue)) return false;
		var super = customValue.Super;

		// The named-super release may unlink and free superService. Decrement the
		// child count first so no guest record is accessed after its last lease is
		// released.
		if (superService.IsNotNull)
		{
			if (!MuiClassServiceLeaseCodec.TryRead(ref platform, superService,
				out var superValue)) return false;
			if (superValue.ChildCount != 0)
			{
				superValue.ChildCount--;
				if (!MuiClassServiceLeaseCodec.Write(ref platform, superService,
					superValue)) return false;
			}
		}
		if ((flags & MuiClassServiceLayout.FlagOwnsNamedSuper) != 0 &&
			super.IsNotNull)
			FreeClass(ref platform, serviceState, super);

		platform.Clear(mcc, MuiCustomClassRecord.Size);
		platform.Free(mcc, MuiCustomClassRecord.Size);
		return UnlinkAndFreeLease(ref platform, serviceState, record);
	}

	// Create an object from a custom class and record it against the class so
	// that DeleteCustomClass can detect outstanding objects. Returns the object.
	public static APTR CreateCustomObject<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR mcc, APTR tags)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || mcc.IsNull) return APTR.Null;
		var record = FindLeaseByCustomClass(ref platform, serviceState, mcc);
		if (record.IsNull) return APTR.Null;
		if (!MuiClassServiceLeaseCodec.TryRead(ref platform, record,
			out var recordValue) || recordValue.CustomClass.Raw != mcc.Raw ||
			!MuiCustomClassCodec.TryRead(ref platform, mcc,
				out var customValue))
			return APTR.Null;
		var boopsi = customValue.Class;
		if (boopsi.IsNull) return APTR.Null;
		var obj = platform.NewObject(boopsi, tags);
		if (obj.IsNull) return APTR.Null;
		if (recordValue.ObjectCount == uint.MaxValue) return APTR.Null;
		recordValue.ObjectCount++;
		if (!MuiClassServiceLeaseCodec.Write(ref platform, record, recordValue))
			return APTR.Null;
		return obj;
	}

	// Dispose an object created by CreateCustomObject and release its count.
	public static bool DisposeCustomObject<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR mcc, APTR obj)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || mcc.IsNull || obj.IsNull)
			return false;
		var record = FindLeaseByCustomClass(ref platform, serviceState, mcc);
		if (record.IsNull) return false;
		if (!MuiClassServiceLeaseCodec.TryRead(ref platform, record,
			out var recordValue)) return false;
		var count = recordValue.ObjectCount;
		if (count == 0) return false;
		platform.DisposeObject(obj);
		recordValue.ObjectCount = count - 1;
		if (!MuiClassServiceLeaseCodec.Write(ref platform, record, recordValue))
			return false;
		return true;
	}

	// Hold one class-service lease for an object created through the public
	// object factory. The count is guest-resident so a final FreeClass cannot
	// detach a class while one of these objects still exists.
	public static bool TrackObjectLease<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR classPointer)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || classPointer.IsNull) return false;
		var lease = FindLeaseByBoopsi(ref platform, serviceState, classPointer);
		if (lease.IsNull) return false;
		if (!MuiClassServiceLeaseCodec.TryRead(ref platform, lease,
			out var leaseValue)) return false;
		var flags = leaseValue.Flags;
		if ((flags & (MuiClassServiceLayout.FlagBuiltin |
			MuiClassServiceLayout.FlagExternal)) == 0) return false;
		if (leaseValue.ObjectCount == uint.MaxValue) return false;
		leaseValue.ObjectCount++;
		return MuiClassServiceLeaseCodec.Write(ref platform, lease, leaseValue);
	}

	// Release the class-service lease held by one object. The object must have
	// already been removed from the headless registry so an external final
	// release can close its loader and unregister its class.
	public static bool ReleaseObjectLease<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR classPointer)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || classPointer.IsNull) return false;
		var lease = FindLeaseByBoopsi(ref platform, serviceState, classPointer);
		if (lease.IsNull) return false;
		if (!MuiClassServiceLeaseCodec.TryRead(ref platform, lease,
			out var leaseValue)) return false;
		var count = leaseValue.ObjectCount;
		if (count == 0 || leaseValue.RefCount == 0) return false;
		leaseValue.ObjectCount = count - 1;
		if (!MuiClassServiceLeaseCodec.Write(ref platform, lease, leaseValue))
			return false;
		if (FreeClass(ref platform, serviceState, classPointer)) return true;
		if (!MuiClassServiceLeaseCodec.TryRead(ref platform, lease,
			out leaseValue)) return false;
		leaseValue.ObjectCount = count;
		MuiClassServiceLeaseCodec.Write(ref platform, lease, leaseValue);
		return false;
	}

	// Read back the reference count of a class lease (test/telemetry helper).
	public static uint ReferenceCount<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR classPointer)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || classPointer.IsNull) return 0;
		var lease = FindLeaseByBoopsi(ref platform, serviceState, classPointer);
		return lease.IsNull || !MuiClassServiceLeaseCodec.TryRead(ref platform,
			lease, out var leaseValue) ? 0 : leaseValue.RefCount;
	}

	public static uint ObjectLeaseCount<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR classPointer)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || classPointer.IsNull) return 0;
		var lease = FindLeaseByBoopsi(ref platform, serviceState, classPointer);
		return lease.IsNull || !MuiClassServiceLeaseCodec.TryRead(ref platform,
			lease, out var leaseValue) ? 0 : leaseValue.ObjectCount;
	}

	// ---- Internals -----------------------------------------------------------

	private static APTR LoadExternal<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR headless, APTR classId)
		where TPlatform : struct, IMuiServicePlatform
	{
		var length = Measure(ref platform, classId,
			MuiClassServiceLayout.ClassIdMaximum);
		if (length == 0) return APTR.Null;
		var prefixSize = MuiClassServiceLibraryPrefixRecord.Size;
		var total = prefixSize + length + 1u;   // "mui/" + classid + NUL
		var name = MuiHeadlessMemory.Allocate(ref platform, total);
		if (name.IsNull) return APTR.Null;
		if (!MuiClassServiceLibraryPrefixRecordCodec.WritePrefix(ref platform, name,
			MuiClassServiceLibraryPrefixRecordCodec.MuiSlash))
		{
			platform.Clear(name, total);
			platform.Free(name, total);
			return APTR.Null;
		}
		if (!MuiClassServiceLibraryPrefixRecordCodec.TryGetPayloadAddress(
			ref platform, name, out var libraryId))
		{
			platform.Clear(name, total);
			platform.Free(name, total);
			return APTR.Null;
		}
		var sourceCursor = default(MuiClassServiceStringByteCursor);
		sourceCursor.Text = classId;
		var destinationCursor = default(MuiClassServiceStringByteCursor);
		destinationCursor.Text = libraryId;
		for (uint index = 0; index < length; index++)
		{
			sourceCursor.Index = index;
			destinationCursor.Index = index;
			if (!MuiClassServiceStringByteCursorCodec.TryReadByte(ref platform,
				sourceCursor, out var value) ||
				!MuiClassServiceStringByteCursorCodec.TryWriteByte(ref platform,
					destinationCursor, value))
			{
				platform.Clear(name, total);
				platform.Free(name, total);
				return APTR.Null;
			}
		}
		destinationCursor.Index = length;
		if (!MuiClassServiceStringByteCursorCodec.TryWriteByte(ref platform,
			destinationCursor, 0))
		{
			platform.Clear(name, total);
			platform.Free(name, total);
			return APTR.Null;
		}

		var library = platform.OpenLibrary(name, 0);
		platform.Clear(name, total);
		platform.Free(name, total);
		if (library.IsNull) return APTR.Null;

		var boopsi = platform.ResolvePublicClass(classId);
		if (boopsi.IsNull)
		{
			platform.CloseLibrary(library);   // rollback the loader lease
			return APTR.Null;
		}

		var ownedId = MuiHeadlessMemory.Allocate(ref platform, length + 1u);
		if (ownedId.IsNull)
		{
			platform.CloseLibrary(library);
			return APTR.Null;
		}
		destinationCursor = default(MuiClassServiceStringByteCursor);
		destinationCursor.Text = ownedId;
		for (uint index = 0; index < length; index++)
		{
			sourceCursor.Index = index;
			destinationCursor.Index = index;
			if (!MuiClassServiceStringByteCursorCodec.TryReadByte(ref platform,
				sourceCursor, out var value) ||
				!MuiClassServiceStringByteCursorCodec.TryWriteByte(ref platform,
					destinationCursor, value))
			{
				platform.Clear(ownedId, length + 1u);
				platform.Free(ownedId, length + 1u);
				platform.CloseLibrary(library);
				return APTR.Null;
			}
		}
		destinationCursor.Index = length;
		if (!MuiClassServiceStringByteCursorCodec.TryWriteByte(ref platform,
			destinationCursor, 0))
		{
			platform.Clear(ownedId, length + 1u);
			platform.Free(ownedId, length + 1u);
			platform.CloseLibrary(library);
			return APTR.Null;
		}

		var headlessRecord = MuiHeadlessObjectCore.RegisterExternalClass(
			ref platform, headless, ownedId, boopsi, APTR.Null);
		if (headlessRecord.IsNull)
		{
			platform.Clear(ownedId, length + 1u);
			platform.Free(ownedId, length + 1u);
			platform.CloseLibrary(library);
			return APTR.Null;
		}

		var lease = NewLease(ref platform, serviceState, ownedId, boopsi, library,
			headlessRecord, MuiClassServiceLayout.FlagExternal |
				MuiClassServiceLayout.FlagOwnsClassId);
		if (lease.IsNull)
		{
			MuiHeadlessObjectCore.DeleteClass(ref platform, headless, headlessRecord);
			platform.Clear(ownedId, length + 1u);
			platform.Free(ownedId, length + 1u);
			platform.CloseLibrary(library);
			return APTR.Null;
		}
		return boopsi;
	}

	private static APTR NewLease<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR classId, APTR boopsi, APTR library,
		APTR headlessRecord, uint flags)
		where TPlatform : struct, IMuiServicePlatform
	{
		var record = MuiHeadlessMemory.Allocate(ref platform,
			MuiClassServiceLeaseRecord.Size);
		if (record.IsNull) return APTR.Null;
		if (!MuiClassServiceStateCodec.TryRead(ref platform, serviceState,
			out var serviceValue))
		{
			platform.Free(record, MuiClassServiceLeaseRecord.Size);
			return APTR.Null;
		}
		MuiClassServiceLeaseRecord leaseValue = default;
		leaseValue.Next = serviceValue.Head;
		leaseValue.Flags = flags;
		leaseValue.ClassId = classId;
		leaseValue.Boopsi = boopsi;
		leaseValue.LibraryBase = library;
		leaseValue.RefCount = 1;
		leaseValue.HeadlessClass = headlessRecord;
		if (!MuiClassServiceLeaseCodec.Write(ref platform, record, leaseValue))
		{
			platform.Free(record, MuiClassServiceLeaseRecord.Size);
			return APTR.Null;
		}
		serviceValue.Head = record;
		if (!MuiClassServiceStateCodec.Write(ref platform, serviceState,
			serviceValue))
		{
			platform.Clear(record, MuiClassServiceLeaseRecord.Size);
			platform.Free(record, MuiClassServiceLeaseRecord.Size);
			return APTR.Null;
		}
		return record;
	}

	private static bool Reference<TPlatform>(ref TPlatform platform, APTR lease)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiClassServiceLeaseCodec.TryRead(ref platform, lease,
			out var leaseValue) || leaseValue.RefCount == uint.MaxValue)
			return false;
		leaseValue.RefCount++;
		return MuiClassServiceLeaseCodec.Write(ref platform, lease,
			leaseValue);
	}

	private static bool UnlinkAndFreeLease<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR lease)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiClassServiceLeaseCodec.TryRead(ref platform, lease,
			out var leaseValue)) return false;
		var flags = leaseValue.Flags;
		var classId = leaseValue.ClassId;
		var classIdLength = 0u;
		if ((flags & MuiClassServiceLayout.FlagOwnsClassId) != 0 &&
			classId.IsNotNull)
			classIdLength = Measure(ref platform, classId,
				MuiClassServiceLayout.ClassIdMaximum);
		if (!UnlinkLease(ref platform, serviceState, lease)) return false;
		if (classIdLength != 0)
		{
			platform.Clear(classId, classIdLength + 1u);
			platform.Free(classId, classIdLength + 1u);
		}
		platform.Clear(lease, MuiClassServiceLeaseRecord.Size);
		platform.Free(lease, MuiClassServiceLeaseRecord.Size);
		return true;
	}

	private static APTR FindLeaseByClassId<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR classId)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiClassServiceStateCodec.TryRead(ref platform, serviceState,
			out var serviceValue)) return APTR.Null;
		var current = serviceValue.Head;
		uint visited = 0;
		while (current.IsNotNull &&
			visited++ < MuiClassServiceLayout.MaximumTraversal)
		{
			if (!MuiClassServiceLeaseCodec.TryRead(ref platform, current,
				out var leaseValue)) return APTR.Null;
			var candidate = leaseValue.ClassId;
			if (candidate.IsNotNull && CStringCodec.TryEquals(ref platform,
				candidate, classId, MuiClassServiceLayout.ClassIdMaximum + 1,
				out var equal) && equal) return current;
			current = leaseValue.Next;
		}
		return APTR.Null;
	}

	private static APTR FindLeaseByBoopsi<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR boopsi)
		where TPlatform : struct, IMuiServicePlatform =>
		FindLeaseByField(ref platform, serviceState,
			LeaseField.Boopsi, boopsi);

	private static APTR FindLeaseByCustomClass<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR mcc)
		where TPlatform : struct, IMuiServicePlatform =>
		FindLeaseByField(ref platform, serviceState,
			LeaseField.CustomClass, mcc);

	private static APTR FindLeaseByField<TPlatform>(ref TPlatform platform,
		APTR serviceState, LeaseField field, APTR value)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (value.IsNull) return APTR.Null;
		if (!MuiClassServiceStateCodec.TryRead(ref platform, serviceState,
			out var serviceValue)) return APTR.Null;
		var current = serviceValue.Head;
		uint visited = 0;
		while (current.IsNotNull &&
			visited++ < MuiClassServiceLayout.MaximumTraversal)
		{
			if (!MuiClassServiceLeaseCodec.TryRead(ref platform, current,
				out var leaseValue)) return APTR.Null;
			var candidate = field == LeaseField.Boopsi ?
				leaseValue.Boopsi : leaseValue.CustomClass;
			if (candidate.Raw == value.Raw) return current;
			current = leaseValue.Next;
		}
		return APTR.Null;
	}

	private static bool UnlinkLease<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR target)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiClassServiceStateCodec.TryRead(ref platform, serviceState,
			out var serviceValue)) return false;
		var current = serviceValue.Head;
		var previous = APTR.Null;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiClassServiceLayout.MaximumTraversal)
		{
			if (!MuiClassServiceLeaseCodec.TryRead(ref platform, current,
				out var currentValue)) return false;
			if (current.Raw == target.Raw)
			{
				if (previous.IsNull)
				{
					serviceValue.Head = currentValue.Next;
					return MuiClassServiceStateCodec.Write(ref platform,
						serviceState, serviceValue);
				}
				if (!MuiClassServiceLeaseCodec.TryRead(ref platform, previous,
					out var previousValue)) return false;
				previousValue.Next = currentValue.Next;
				return MuiClassServiceLeaseCodec.Write(ref platform, previous,
					previousValue);
			}
			previous = current;
			current = currentValue.Next;
		}
		return false;
	}

	private static uint Measure<TPlatform>(ref TPlatform platform, APTR text,
		uint maximum) where TPlatform : struct, IMuiServicePlatform
	{
		if (text.IsNull) return 0;
		uint index = 0;
		while (index < maximum)
		{
			if (!MuiClassServiceStringByteCursorCodec.TryReadAt(ref platform,
				text, (int)index, out var value)) return 0;
			if (value == 0) return index;
			index++;
		}
		return 0;   // unterminated within the bound
	}

	private static bool Ready<TPlatform>(ref TPlatform platform, APTR serviceState)
		where TPlatform : struct, IMuiServicePlatform =>
		serviceState.IsNotNull &&
		MuiClassServiceStateCodec.TryRead(ref platform, serviceState,
			out var serviceValue) && serviceValue.Magic == MuiClassServiceLayout.Magic;

	private static APTR Headless<TPlatform>(ref TPlatform platform,
		APTR serviceState) where TPlatform : struct, IMuiServicePlatform =>
		MuiClassServiceStateCodec.TryRead(ref platform, serviceState,
			out var serviceValue) ? serviceValue.Headless : APTR.Null;
}
