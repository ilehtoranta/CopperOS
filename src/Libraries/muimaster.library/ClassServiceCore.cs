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
	// Numeric aliases document the guest ABI for legacy diagnostics; typed
	// field access is declaration-ordered through MuiGuestStructCursor.
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
	private static bool TryResolveFieldIndex(MuiClassRecordKind record,
		MuiClassRecordField field, out uint index, out uint size)
	{
		index = 0;
		size = 0;
		switch (record)
		{
			case MuiClassRecordKind.State:
				size = MuiClassServiceStateRecord.Size;
			if (field == MuiClassRecordField.Magic)
				{
					index = 0;
					return true;
				}
				if (field == MuiClassRecordField.Head)
				{
					index = 1;
					return true;
				}
				if (field == MuiClassRecordField.Headless)
				{
					index = 2;
					return true;
				}
				if (field == MuiClassRecordField.Generation)
				{
					index = 3;
					return true;
				}
				return false;
			case MuiClassRecordKind.Lease:
				size = MuiClassServiceLeaseRecord.Size;
			if (field == MuiClassRecordField.Next)
				{
					index = 0;
					return true;
				}
				if (field == MuiClassRecordField.Flags)
				{
					index = 1;
					return true;
				}
				if (field == MuiClassRecordField.ClassId)
				{
					index = 2;
					return true;
				}
				if (field == MuiClassRecordField.Boopsi)
				{
					index = 3;
					return true;
				}
				if (field == MuiClassRecordField.LibraryBase)
				{
					index = 4;
					return true;
				}
				if (field == MuiClassRecordField.RefCount)
				{
					index = 5;
					return true;
				}
				if (field == MuiClassRecordField.HeadlessClass)
				{
					index = 6;
					return true;
				}
				if (field == MuiClassRecordField.CustomClass)
				{
					index = 7;
					return true;
				}
				if (field == MuiClassRecordField.SuperService)
				{
					index = 8;
					return true;
				}
				if (field == MuiClassRecordField.ObjectCount)
				{
					index = 9;
					return true;
				}
				if (field == MuiClassRecordField.ChildCount)
				{
					index = 10;
					return true;
				}
				return false;
			case MuiClassRecordKind.CustomClass:
				size = MuiCustomClassRecord.Size;
			if (field == MuiClassRecordField.UserData)
				{
					index = 0;
					return true;
				}
				if (field == MuiClassRecordField.UtilityBase)
				{
					index = 1;
					return true;
				}
				if (field == MuiClassRecordField.DosBase)
				{
					index = 2;
					return true;
				}
				if (field == MuiClassRecordField.GfxBase)
				{
					index = 3;
					return true;
				}
				if (field == MuiClassRecordField.IntuitionBase)
				{
					index = 4;
					return true;
				}
				if (field == MuiClassRecordField.Super)
				{
					index = 5;
					return true;
				}
				if (field == MuiClassRecordField.Class)
				{
					index = 6;
					return true;
				}
				return false;
			default:
				return false;
		}
	}

	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiClassRecordKind record,
		MuiClassRecordField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolveFieldIndex(record, field, out var index, out _))
			return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiClassServiceStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				return true;
			}
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR recordAddress, MuiClassRecordKind record, MuiClassRecordField field,
		out APTR address)
	where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolveFieldIndex(record, field, out _, out var size) ||
			!MuiGuestStructCursor.TryCreate(ref platform, recordAddress, size,
				out var cursor)) return false;
		return TryTakeField(ref platform, ref cursor, record, field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiClassRecordKind record, MuiClassRecordField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (record == MuiClassRecordKind.State)
		{
			if (!MuiClassServiceStateStructCodec.TryRead(ref platform, address,
				out var state)) return false;
			if (field == MuiClassRecordField.Magic)
				value = state.Magic;
			else if (field == MuiClassRecordField.Head)
				value = state.Head.Raw;
			else if (field == MuiClassRecordField.Headless)
				value = state.Headless.Raw;
			else if (field == MuiClassRecordField.Generation)
				value = state.Generation;
			else return false;
			return true;
		}
		if (record == MuiClassRecordKind.Lease)
		{
			if (!MuiClassServiceLeaseStructCodec.TryRead(ref platform, address,
				out var lease)) return false;
			if (field == MuiClassRecordField.Next)
				value = lease.Next.Raw;
			else if (field == MuiClassRecordField.Flags)
				value = lease.Flags;
			else if (field == MuiClassRecordField.ClassId)
				value = lease.ClassId.Raw;
			else if (field == MuiClassRecordField.Boopsi)
				value = lease.Boopsi.Raw;
			else if (field == MuiClassRecordField.LibraryBase)
				value = lease.LibraryBase.Raw;
			else if (field == MuiClassRecordField.RefCount)
				value = lease.RefCount;
			else if (field == MuiClassRecordField.HeadlessClass)
				value = lease.HeadlessClass.Raw;
			else if (field == MuiClassRecordField.CustomClass)
				value = lease.CustomClass.Raw;
			else if (field == MuiClassRecordField.SuperService)
				value = lease.SuperService.Raw;
			else if (field == MuiClassRecordField.ObjectCount)
				value = lease.ObjectCount;
			else if (field == MuiClassRecordField.ChildCount)
				value = lease.ChildCount;
			else return false;
			return true;
		}
		if (record == MuiClassRecordKind.CustomClass)
		{
			if (!MuiCustomClassStructCodec.TryRead(ref platform, address,
				out var custom)) return false;
			if (field == MuiClassRecordField.UserData)
				value = custom.UserData.Raw;
			else if (field == MuiClassRecordField.UtilityBase)
				value = custom.UtilityBase.Raw;
			else if (field == MuiClassRecordField.DosBase)
				value = custom.DosBase.Raw;
			else if (field == MuiClassRecordField.GfxBase)
				value = custom.GfxBase.Raw;
			else if (field == MuiClassRecordField.IntuitionBase)
				value = custom.IntuitionBase.Raw;
			else if (field == MuiClassRecordField.Super)
				value = custom.Super.Raw;
			else if (field == MuiClassRecordField.Class)
				value = custom.Class.Raw;
			else return false;
			return true;
		}
		return false;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiClassRecordKind record, MuiClassRecordField field,
		uint value) where TPlatform : struct, IMuiGuestMemory
	{
		if (record == MuiClassRecordKind.State)
		{
			if (!MuiClassServiceStateStructCodec.TryRead(ref platform, address,
				out var state)) return false;
			if (field == MuiClassRecordField.Magic)
				state.Magic = value;
			else if (field == MuiClassRecordField.Head)
				state.Head = APTR.FromPointer(value);
			else if (field == MuiClassRecordField.Headless)
				state.Headless = APTR.FromPointer(value);
			else if (field == MuiClassRecordField.Generation)
				state.Generation = value;
			else return false;
			return MuiClassServiceStateStructCodec.Write(ref platform, address,
				state);
		}
		if (record == MuiClassRecordKind.Lease)
		{
			if (!MuiClassServiceLeaseStructCodec.TryRead(ref platform, address,
				out var lease)) return false;
			if (field == MuiClassRecordField.Next)
				lease.Next = APTR.FromPointer(value);
			else if (field == MuiClassRecordField.Flags)
				lease.Flags = value;
			else if (field == MuiClassRecordField.ClassId)
				lease.ClassId = APTR.FromPointer(value);
			else if (field == MuiClassRecordField.Boopsi)
				lease.Boopsi = APTR.FromPointer(value);
			else if (field == MuiClassRecordField.LibraryBase)
				lease.LibraryBase = APTR.FromPointer(value);
			else if (field == MuiClassRecordField.RefCount)
				lease.RefCount = value;
			else if (field == MuiClassRecordField.HeadlessClass)
				lease.HeadlessClass = APTR.FromPointer(value);
			else if (field == MuiClassRecordField.CustomClass)
				lease.CustomClass = APTR.FromPointer(value);
			else if (field == MuiClassRecordField.SuperService)
				lease.SuperService = APTR.FromPointer(value);
			else if (field == MuiClassRecordField.ObjectCount)
				lease.ObjectCount = value;
			else if (field == MuiClassRecordField.ChildCount)
				lease.ChildCount = value;
			else return false;
			return MuiClassServiceLeaseStructCodec.Write(ref platform, address,
				lease);
		}
		if (record == MuiClassRecordKind.CustomClass)
		{
			if (!MuiCustomClassStructCodec.TryRead(ref platform, address,
				out var custom)) return false;
			if (field == MuiClassRecordField.UserData)
				custom.UserData = APTR.FromPointer(value);
			else if (field == MuiClassRecordField.UtilityBase)
				custom.UtilityBase = APTR.FromPointer(value);
			else if (field == MuiClassRecordField.DosBase)
				custom.DosBase = APTR.FromPointer(value);
			else if (field == MuiClassRecordField.GfxBase)
				custom.GfxBase = APTR.FromPointer(value);
			else if (field == MuiClassRecordField.IntuitionBase)
				custom.IntuitionBase = APTR.FromPointer(value);
			else if (field == MuiClassRecordField.Super)
				custom.Super = APTR.FromPointer(value);
			else if (field == MuiClassRecordField.Class)
				custom.Class = APTR.FromPointer(value);
			else return false;
			return MuiCustomClassStructCodec.Write(ref platform, address, custom);
		}
		return false;
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
	// Numeric aliases document the guest ABI for legacy diagnostics; typed
	// field access is declaration-ordered through MuiGuestStructCursor.
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

// Provider handles supplied by an owning caller. Carrying these values does not
// acquire or release their libraries; the caller retains them for class lifetime.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiClassProviderBases
{
	public const uint Size = 16;
	public APTR UtilityBase;
	public APTR DosBase;
	public APTR GraphicsBase;
	public APTR IntuitionBase;

	public bool IsComplete => UtilityBase.IsNotNull && DosBase.IsNotNull &&
		GraphicsBase.IsNotNull && IntuitionBase.IsNotNull;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCustomClassRecord
{
	// Numeric aliases document the guest ABI for legacy diagnostics; typed
	// field access is declaration-ordered through MuiGuestStructCursor.
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

// The MG09 custom-class and external-class service backend keeps class leases
// in guest-resident state without managed allocations or runtime dependencies.
// External classes enter the existing headless registry through its public
// RegisterExternalClass/DeleteClass entry points. This is not yet the complete
// native public MUI_CreateCustomClass contract: the explicit provider-base path
// still requires owned library lifetimes before exposing that wrapper.
public static class MuiClassServiceCore
{
	private enum LeaseField
	{
		Boopsi,
		CustomClass
	}

	private struct CustomClassRemoval
	{
		internal MuiClassServiceStateRecord Service;
		internal APTR Previous;
		internal MuiClassServiceLeaseRecord PreviousLease;
	}

	public static bool Initialize<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR headlessState)
		where TPlatform : struct, IMuiClassServicePlatform
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
		where TPlatform : struct, IMuiClassServicePlatform
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

	// Return both the class pointer and the exact lease acquired for this class
	// ID. The caller keeps the lease address in its own named binding record.
	internal static APTR GetClassAndLease<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR classId, out APTR lease)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		lease = APTR.Null;
		var classPointer = GetClass(ref platform, serviceState, classId);
		if (classPointer.IsNull) return APTR.Null;
		lease = FindLeaseByClassId(ref platform, serviceState, classId);
		if (lease.IsNull)
		{
			FreeClass(ref platform, serviceState, classPointer);
			return APTR.Null;
		}
		return classPointer;
	}

	// MUI_FreeClass(classptr). Releases one reference. When the final reference
	// is dropped the loader lease is closed and the external registry record is
	// removed; if outstanding objects prevent removal the reference is restored
	// and the call fails without freeing anything.
	public static bool FreeClass<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR classPointer)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		if (!Ready(ref platform, serviceState) || classPointer.IsNull) return false;
		var lease = FindLeaseByBoopsi(ref platform, serviceState, classPointer);
		if (lease.IsNull) return false;
		return FreeClassLease(ref platform, serviceState, lease);
	}

	// The native public-object bridge retains the exact service lease record,
	// rather than resolving it again by BOOPSI class pointer. MorphOS may expose
	// multiple class IDs backed by one class pointer, so pointer-only lookup is
	// not sufficient for object/reference accounting.
	internal static bool FreeClassLease<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR lease)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		if (!Ready(ref platform, serviceState) || lease.IsNull) return false;
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
			// Stop exposing this reference before the irreversible loader close.
			// Its callback may acquire the same class again; that fresh lease must
			// not be overwritten or unlinked by this retiring frame.
			if (!PrepareCustomClassRemoval(ref platform, serviceState, lease,
				out var removal)) return false;
			if (!DetachCustomClassLease(ref platform, serviceState, removal, leaseValue.Next))
			{
				RestoreCustomClassLease(ref platform, serviceState, removal);
				return false;
			}
			var headless = Headless(ref platform, serviceState);
			var headlessRecord = leaseValue.HeadlessClass;
			if (!MuiHeadlessObjectCore.DeleteClass(ref platform, headless,
				headlessRecord))
			{
				RestoreCustomClassLease(ref platform, serviceState, removal);
				return false;   // outstanding objects: leave the reference in place
			}
			var library = leaseValue.LibraryBase;
			FreeDetachedLease(ref platform, lease, leaseValue);
			if (library.IsNotNull) platform.CloseLibrary(library);
			return true; // Never touch saved service/lease state after the callback.
		}

		leaseValue.RefCount = 0;
		if (!MuiClassServiceLeaseCodec.Write(ref platform, lease, leaseValue))
			return false;
		return UnlinkAndFreeLease(ref platform, serviceState, lease);
	}

	// MUI_CreateCustomClass(base, supername, supermcc, datasize, dispfunc).
	// Enforces exactly one super-class source, a non-null dispatcher and a
	// bounded data size, resolves the super class, binds the A6 library base for
	// public classes, and publishes the 28-byte MUI_CustomClass wire shape.
	// This legacy portable overload leaves the four provider bases empty. The
	// explicit overload populates them from a separately owned provider context.
	// Returns the MUI_CustomClass pointer or Null; allocation/publication failures
	// roll back while callers serialize lifecycle and retain owned mappings.
	public static APTR CreateCustomClass<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR libraryBase, APTR superClassId, APTR superMcc,
		int dataSize, APTR dispatcher)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		MuiClassProviderBases providerBases = default;
		return CreateCustomClassCore(ref platform, serviceState, libraryBase,
			superClassId, superMcc, dataSize, dispatcher, providerBases);
	}

	// All four handles are required and copied before the class becomes visible.
	// They are independent of libraryBase, which supplies the dispatcher A6 value.
	// No library ownership is transferred by this value record.
	public static APTR CreateCustomClass<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR libraryBase, APTR superClassId, APTR superMcc,
		int dataSize, APTR dispatcher, MuiClassProviderBases providerBases)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		if (!providerBases.IsComplete) return APTR.Null;
		return CreateCustomClassCore(ref platform, serviceState, libraryBase,
			superClassId, superMcc, dataSize, dispatcher, providerBases);
	}

	private static APTR CreateCustomClassCore<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR libraryBase, APTR superClassId, APTR superMcc,
		int dataSize, APTR dispatcher, MuiClassProviderBases providerBases)
		where TPlatform : struct, IMuiClassServicePlatform
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

		// Reject a saturated or unreadable parent before creating a native class.
		// The caller keeps service-owned mappings stable through this operation.
		if (superService.IsNotNull &&
			(!MuiClassServiceLeaseCodec.TryRead(ref platform, superService,
				out var checkedSuper) || checkedSuper.ChildCount == uint.MaxValue))
		{
			if (ownsNamedSuper) FreeClass(ref platform, serviceState, superClass);
			return APTR.Null;
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
		// Keep both records detached until their complete contents and the parent
		// bookkeeping are ready. A failed initialization must never leave a lease
		// pointing to a class that the rollback has already freed.
		var record = MuiHeadlessMemory.Allocate(ref platform,
			MuiClassServiceLeaseRecord.Size);
		if (record.IsNull)
		{
			platform.Clear(mcc, MuiCustomClassRecord.Size);
			platform.Free(mcc, MuiCustomClassRecord.Size);
			platform.FreeCustomClass(boopsi);
			if (ownsNamedSuper) FreeClass(ref platform, serviceState, superClass);
			return APTR.Null;
		}

		if (PublishCustomClass(ref platform, serviceState, record, mcc, boopsi,
			superClass, superService, flags, providerBases)) return mcc;

		platform.Clear(record, MuiClassServiceLeaseRecord.Size);
		platform.Free(record, MuiClassServiceLeaseRecord.Size);
		platform.Clear(mcc, MuiCustomClassRecord.Size);
		platform.Free(mcc, MuiCustomClassRecord.Size);
		platform.FreeCustomClass(boopsi);
		if (ownsNamedSuper) FreeClass(ref platform, serviceState, superClass);
		return APTR.Null;
	}

	// MUI_DeleteCustomClass(mcc). Fails atomically (freeing nothing) when the
	// class still has outstanding objects or sub classes. On success it frees
	// the boopsi class, releases any named super-class lease, decrements the
	// super's child count and frees the MUI_CustomClass structure.
	public static bool DeleteCustomClass<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR mcc)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		if (!Ready(ref platform, serviceState) || mcc.IsNull) return false;
		var record = FindLeaseByCustomClass(ref platform, serviceState, mcc);
		if (record.IsNull) return false;
		if (!MuiClassServiceLeaseCodec.TryRead(ref platform, record,
			out var recordValue)) return false;
		if (recordValue.ObjectCount != 0 || recordValue.ChildCount != 0)
			return false;

		var flags = recordValue.Flags;
		var superService = recordValue.SuperService;
		if (!MuiCustomClassCodec.TryRead(ref platform, mcc,
			out var customValue)) return false;
		var super = customValue.Super;
		var boopsi = recordValue.Boopsi;
		if (boopsi.IsNull || customValue.Class != boopsi) return false;
		MuiClassServiceLeaseRecord superValue = default;
		if (superService.IsNotNull &&
			(!MuiClassServiceLeaseCodec.TryRead(ref platform, superService,
				out superValue) || superValue.ChildCount == 0 ||
				superValue.Boopsi != super)) return false;
		if (!PrepareCustomClassRemoval(ref platform, serviceState, record,
			out var removal)) return false;

		// Complete fallible guest-record writes before the irreversible native
		// free. Keep snapshots so native refusal leaves the service unchanged.
		// Callers serialize class lifecycle and retain these owned mappings.
		if (superService.IsNotNull)
		{
			var updatedSuper = superValue;
			updatedSuper.ChildCount--;
			if (!MuiClassServiceLeaseCodec.Write(ref platform, superService,
				updatedSuper))
			{
				MuiClassServiceLeaseCodec.Write(ref platform, superService, superValue);
				return false;
			}
		}
		if (!DetachCustomClassLease(ref platform, serviceState, removal,
			recordValue.Next))
		{
			RestoreCustomClassLease(ref platform, serviceState, removal);
			if (superService.IsNotNull)
				MuiClassServiceLeaseCodec.Write(ref platform, superService, superValue);
			return false;
		}
		// Native Intuition also sees objects/subclasses created directly by
		// clients, which our optional object helpers never counted.
		if (!platform.FreeCustomClass(boopsi))
		{
			RestoreCustomClassLease(ref platform, serviceState, removal);
			if (superService.IsNotNull)
				MuiClassServiceLeaseCodec.Write(ref platform, superService, superValue);
			return false;
		}
		// The named-super release may unlink and free superService; never touch
		// its saved address after dropping that final loader lease.
		if ((flags & MuiClassServiceLayout.FlagOwnsNamedSuper) != 0 &&
			super.IsNotNull)
			FreeClass(ref platform, serviceState, super);

		platform.Clear(mcc, MuiCustomClassRecord.Size);
		platform.Free(mcc, MuiCustomClassRecord.Size);
		platform.Clear(record, MuiClassServiceLeaseRecord.Size);
		platform.Free(record, MuiClassServiceLeaseRecord.Size);
		return true;
	}

	// Create an object from a custom class and record it against the class so
	// that DeleteCustomClass can detect outstanding objects. Returns the object.
	public static APTR CreateCustomObject<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR mcc, APTR tags)
		where TPlatform : struct, IMuiClassServicePlatform, IMuiBoopsiObjectLifetimeCapability
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
		if (boopsi.IsNull || boopsi != recordValue.Boopsi ||
			recordValue.ObjectCount == uint.MaxValue) return APTR.Null;
		// Reserve before dispatch: construction may reenter this service, create
		// other objects/classes, or attempt class deletion. Do not overwrite a
		// callback's changes with the record captured before NewObject.
		var originalRecord = recordValue;
		recordValue.ObjectCount++;
		if (!MuiClassServiceLeaseCodec.Write(ref platform, record, recordValue))
		{
			MuiClassServiceLeaseCodec.Write(ref platform, record, originalRecord);
			return APTR.Null;
		}
		var obj = platform.NewObject(boopsi, tags);
		if (obj.IsNull &&
			MuiClassServiceLeaseCodec.TryRead(ref platform, record, out var afterCall) &&
			afterCall.CustomClass == mcc && afterCall.Boopsi == boopsi &&
			afterCall.ObjectCount != 0)
		{
			afterCall.ObjectCount--;
			MuiClassServiceLeaseCodec.Write(ref platform, record, afterCall);
		}
		return obj;
	}

	// Dispose an object created by CreateCustomObject and release its count.
	public static bool DisposeCustomObject<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR mcc, APTR obj)
		where TPlatform : struct, IMuiClassServicePlatform, IMuiBoopsiObjectLifetimeCapability
	{
		if (!Ready(ref platform, serviceState) || mcc.IsNull || obj.IsNull)
			return false;
		var record = FindLeaseByCustomClass(ref platform, serviceState, mcc);
		if (record.IsNull) return false;
		if (!MuiClassServiceLeaseCodec.TryRead(ref platform, record,
			out var recordValue)) return false;
		var count = recordValue.ObjectCount;
		if (count == 0) return false;
		// Keep this object's reservation while its dispatcher runs. A nested
		// create/dispose or subclass operation may change any lease field.
		platform.DisposeObject(obj);
		if (!MuiClassServiceLeaseCodec.TryRead(ref platform, record, out var afterCall) ||
			afterCall.CustomClass != mcc || afterCall.Boopsi != recordValue.Boopsi ||
			afterCall.ObjectCount == 0) return false;
		afterCall.ObjectCount--;
		return MuiClassServiceLeaseCodec.Write(ref platform, record, afterCall);
	}

	// The native public-object binding has already sent OM_DISPOSE. Release only
	// the custom-class object reservation here; the caller's MUI_CustomClass
	// reference remains owned by the caller and must not be consumed implicitly.
	internal static bool ReleaseCustomObjectLease<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR mcc)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		if (!Ready(ref platform, serviceState) || mcc.IsNull) return false;
		var record = FindLeaseByCustomClass(ref platform, serviceState, mcc);
		if (record.IsNull || !MuiClassServiceLeaseCodec.TryRead(ref platform,
			record, out var recordValue) || recordValue.ObjectCount == 0)
			return false;
		recordValue.ObjectCount--;
		return MuiClassServiceLeaseCodec.Write(ref platform, record, recordValue);
	}

	// Hold one class-service lease for an object created through the public
	// object factory. The count is guest-resident so a final FreeClass cannot
	// detach a class while one of these objects still exists.
	public static bool TrackObjectLease<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR classPointer)
		where TPlatform : struct, IMuiClassServicePlatform
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

	internal static bool TrackObjectLeaseByLease<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR lease)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		if (!Ready(ref platform, serviceState) || lease.IsNull ||
			!MuiClassServiceLeaseCodec.TryRead(ref platform, lease,
				out var leaseValue)) return false;
		var flags = leaseValue.Flags;
		if ((flags & (MuiClassServiceLayout.FlagBuiltin |
			MuiClassServiceLayout.FlagExternal)) == 0 ||
			leaseValue.ObjectCount == uint.MaxValue) return false;
		leaseValue.ObjectCount++;
		return MuiClassServiceLeaseCodec.Write(ref platform, lease, leaseValue);
	}

	// Public native object construction needs to distinguish a custom-class
	// lease from a builtin/external class lease before dispatch. Keep that
	// classification in the declaration-ordered service record instead of
	// inferring it from the BOOPSI class layout or a side table.
	internal static bool TryGetCustomClass<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR classPointer, out APTR customClass)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		customClass = APTR.Null;
		if (!Ready(ref platform, serviceState) || classPointer.IsNull) return false;
		var lease = FindLeaseByBoopsi(ref platform, serviceState, classPointer);
		if (lease.IsNull || !MuiClassServiceLeaseCodec.TryRead(ref platform,
			lease, out var leaseValue) ||
			(leaseValue.Flags & MuiClassServiceLayout.FlagCustom) == 0 ||
			leaseValue.CustomClass.IsNull)
			return false;
		customClass = leaseValue.CustomClass;
		return true;
	}

	internal static bool TryGetCustomClassByLease<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR lease, out APTR customClass)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		customClass = APTR.Null;
		if (!Ready(ref platform, serviceState) || lease.IsNull ||
			!MuiClassServiceLeaseCodec.TryRead(ref platform, lease,
				out var leaseValue) ||
			(leaseValue.Flags & MuiClassServiceLayout.FlagCustom) == 0 ||
			leaseValue.CustomClass.IsNull) return false;
		customClass = leaseValue.CustomClass;
		return true;
	}

	// Release the class-service lease held by one object. The object must have
	// already been removed from the headless registry so an external final
	// release can close its loader and unregister its class.
	public static bool ReleaseObjectLease<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR classPointer)
		where TPlatform : struct, IMuiClassServicePlatform
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

	internal static bool ReleaseObjectLeaseByLease<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR lease)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		if (!Ready(ref platform, serviceState) || lease.IsNull ||
			!MuiClassServiceLeaseCodec.TryRead(ref platform, lease,
				out var leaseValue)) return false;
		var count = leaseValue.ObjectCount;
		if (count == 0 || leaseValue.RefCount == 0) return false;
		leaseValue.ObjectCount = count - 1;
		if (!MuiClassServiceLeaseCodec.Write(ref platform, lease, leaseValue))
			return false;
		if (FreeClassLease(ref platform, serviceState, lease)) return true;
		if (!MuiClassServiceLeaseCodec.TryRead(ref platform, lease,
			out leaseValue)) return false;
		leaseValue.ObjectCount = count;
		MuiClassServiceLeaseCodec.Write(ref platform, lease, leaseValue);
		return false;
	}

	// Read back the reference count of a class lease (test/telemetry helper).
	public static uint ReferenceCount<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR classPointer)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		if (!Ready(ref platform, serviceState) || classPointer.IsNull) return 0;
		var lease = FindLeaseByBoopsi(ref platform, serviceState, classPointer);
		return lease.IsNull || !MuiClassServiceLeaseCodec.TryRead(ref platform,
			lease, out var leaseValue) ? 0 : leaseValue.RefCount;
	}

	public static uint ObjectLeaseCount<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR classPointer)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		if (!Ready(ref platform, serviceState) || classPointer.IsNull) return 0;
		var lease = FindLeaseByBoopsi(ref platform, serviceState, classPointer);
		return lease.IsNull || !MuiClassServiceLeaseCodec.TryRead(ref platform,
			lease, out var leaseValue) ? 0 : leaseValue.ObjectCount;
	}

	// ---- Internals -----------------------------------------------------------

	private static APTR LoadExternal<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR headless, APTR classId)
		where TPlatform : struct, IMuiClassServicePlatform
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
		// Open can reenter MUI and publish this class. Retain that published
		// lease before dropping the redundant loader reference.
		if (TryReferencePublishedClass(ref platform, serviceState, classId, out var published))
		{
			platform.CloseLibrary(library);
			return published;
		}

		var boopsi = platform.ResolveExternalClass(library, classId);
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

		// Resolve/name allocation may also invoke guest code.
		if (TryReferencePublishedClass(ref platform, serviceState, classId, out published))
		{
			platform.Clear(ownedId, length + 1u);
			platform.Free(ownedId, length + 1u);
			platform.CloseLibrary(library);
			return published;
		}
		// Reserve both records while neither is visible. Allocator recovery can
		// reenter MUI, so identity is checked again after the final allocation.
		var headlessRecord = MuiHeadlessMemory.Allocate(ref platform, MuiHeadlessClassRecord.Size);
		if (headlessRecord.IsNull)
		{
			platform.Clear(ownedId, length + 1u);
			platform.Free(ownedId, length + 1u);
			platform.CloseLibrary(library);
			return APTR.Null;
		}

		var lease = MuiHeadlessMemory.Allocate(ref platform, MuiClassServiceLeaseRecord.Size);
		var result = APTR.Null;
		var publishedElsewhere = TryReferencePublishedClass(ref platform, serviceState, classId, out result);
		if (!publishedElsewhere && lease.IsNotNull &&
			MuiHeadlessObjectCore.FindClassByName(ref platform, headless, classId).IsNull &&
			MuiExternalClassPublication.TryPublish(ref platform, serviceState, headless,
				headlessRecord, lease, ownedId, boopsi, library)) return boopsi;
		// Only our detached allocations are released. Never remove a registry
		// record belonging to a nested publication, even if its name is equal.
		if (lease.IsNotNull)
		{
			platform.Clear(lease, MuiClassServiceLeaseRecord.Size);
			platform.Free(lease, MuiClassServiceLeaseRecord.Size);
		}
		platform.Clear(headlessRecord, MuiHeadlessClassRecord.Size);
		platform.Free(headlessRecord, MuiHeadlessClassRecord.Size);
		platform.Clear(ownedId, length + 1u);
		platform.Free(ownedId, length + 1u);
		platform.CloseLibrary(library);
		return result;
	}

	// True means an existing publication owns the identity, even when its
	// reference cannot be increased. Never publish a duplicate on overflow.
	private static bool TryReferencePublishedClass<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR classId, out APTR boopsi)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		boopsi = APTR.Null;
		var lease = FindLeaseByClassId(ref platform, serviceState, classId);
		if (lease.IsNull) return false;
		if (MuiClassServiceLeaseCodec.TryRead(ref platform, lease, out var value) &&
			value.Boopsi.IsNotNull && Reference(ref platform, lease)) boopsi = value.Boopsi;
		return true;
	}

	private static bool PrepareCustomClassRemoval<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR record, out CustomClassRemoval removal)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		removal = default;
		if (!MuiClassServiceStateCodec.TryRead(ref platform, serviceState,
			out removal.Service)) return false;
		var current = removal.Service.Head;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiClassServiceLayout.MaximumTraversal)
		{
			if (current == record) return true;
			if (!MuiClassServiceLeaseCodec.TryRead(ref platform, current,
				out removal.PreviousLease)) return false;
			removal.Previous = current;
			current = removal.PreviousLease.Next;
		}
		return false;
	}

	private static bool DetachCustomClassLease<TPlatform>(ref TPlatform platform,
		APTR serviceState, CustomClassRemoval removal, APTR next)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		if (removal.Previous.IsNull)
		{
			var service = removal.Service;
			service.Head = next;
			return MuiClassServiceStateCodec.Write(ref platform, serviceState, service);
		}
		var previous = removal.PreviousLease;
		previous.Next = next;
		return MuiClassServiceLeaseCodec.Write(ref platform, removal.Previous, previous);
	}

	private static void RestoreCustomClassLease<TPlatform>(ref TPlatform platform,
		APTR serviceState, CustomClassRemoval removal)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		if (removal.Previous.IsNull)
			MuiClassServiceStateCodec.Write(ref platform, serviceState, removal.Service);
		else
			MuiClassServiceLeaseCodec.Write(ref platform, removal.Previous,
				removal.PreviousLease);
	}

	private static bool PublishCustomClass<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR record, APTR mcc, APTR boopsi, APTR superClass,
		APTR superService, uint flags, MuiClassProviderBases providerBases)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		if (!MuiClassServiceStateCodec.TryRead(ref platform, serviceState,
			out var originalService)) return false;
		MuiClassServiceLeaseRecord originalSuper = default;
		if (superService.IsNotNull &&
			(!MuiClassServiceLeaseCodec.TryRead(ref platform, superService,
				out originalSuper) || originalSuper.ChildCount == uint.MaxValue))
			return false;

		MuiCustomClassRecord customValue = default;
		customValue.UtilityBase = providerBases.UtilityBase;
		customValue.DosBase = providerBases.DosBase;
		customValue.GfxBase = providerBases.GraphicsBase;
		customValue.IntuitionBase = providerBases.IntuitionBase;
		customValue.Super = superClass;
		customValue.Class = boopsi;
		MuiClassServiceLeaseRecord recordValue = default;
		recordValue.Next = originalService.Head;
		recordValue.Flags = flags;
		recordValue.Boopsi = boopsi;
		recordValue.RefCount = 1;
		recordValue.CustomClass = mcc;
		recordValue.SuperService = superService;
		if (!MuiCustomClassCodec.Write(ref platform, mcc, customValue) ||
			!MuiClassServiceLeaseCodec.Write(ref platform, record, recordValue))
			return false;

		if (superService.IsNotNull)
		{
			var updatedSuper = originalSuper;
			updatedSuper.ChildCount++;
			if (!MuiClassServiceLeaseCodec.Write(ref platform, superService,
				updatedSuper))
			{
				MuiClassServiceLeaseCodec.Write(ref platform, superService,
					originalSuper);
				return false;
			}
		}
		var updatedService = originalService;
		updatedService.Head = record;
		if (MuiClassServiceStateCodec.Write(ref platform, serviceState,
			updatedService)) return true;

		// Restore snapshots before the caller releases the still-detached
		// resources. Service-owned mappings must remain valid until completion.
		MuiClassServiceStateCodec.Write(ref platform, serviceState, originalService);
		if (superService.IsNotNull)
			MuiClassServiceLeaseCodec.Write(ref platform, superService, originalSuper);
		return false;
	}

	private static APTR NewLease<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR classId, APTR boopsi, APTR library,
		APTR headlessRecord, uint flags)
		where TPlatform : struct, IMuiClassServicePlatform
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
		where TPlatform : struct, IMuiClassServicePlatform
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
		where TPlatform : struct, IMuiClassServicePlatform
	{
		if (!MuiClassServiceLeaseCodec.TryRead(ref platform, lease,
			out var leaseValue)) return false;
		if (!UnlinkLease(ref platform, serviceState, lease)) return false;
		FreeDetachedLease(ref platform, lease, leaseValue);
		return true;
	}

	private static void FreeDetachedLease<TPlatform>(ref TPlatform platform,
		APTR lease, MuiClassServiceLeaseRecord leaseValue)
		where TPlatform : struct, IMuiClassServicePlatform
	{
		var flags = leaseValue.Flags;
		var classId = leaseValue.ClassId;
		var classIdLength = 0u;
		if ((flags & MuiClassServiceLayout.FlagOwnsClassId) != 0 &&
			classId.IsNotNull)
			classIdLength = Measure(ref platform, classId,
				MuiClassServiceLayout.ClassIdMaximum);
		if (classIdLength != 0)
		{
			platform.Clear(classId, classIdLength + 1u);
			platform.Free(classId, classIdLength + 1u);
		}
		platform.Clear(lease, MuiClassServiceLeaseRecord.Size);
		platform.Free(lease, MuiClassServiceLeaseRecord.Size);
	}

	private static APTR FindLeaseByClassId<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR classId)
		where TPlatform : struct, IMuiClassServicePlatform
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
		where TPlatform : struct, IMuiClassServicePlatform =>
		FindLeaseByField(ref platform, serviceState,
			LeaseField.Boopsi, boopsi);

	private static APTR FindLeaseByCustomClass<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR mcc)
		where TPlatform : struct, IMuiClassServicePlatform =>
		FindLeaseByField(ref platform, serviceState,
			LeaseField.CustomClass, mcc);

	private static APTR FindLeaseByField<TPlatform>(ref TPlatform platform,
		APTR serviceState, LeaseField field, APTR value)
		where TPlatform : struct, IMuiClassServicePlatform
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
		where TPlatform : struct, IMuiClassServicePlatform
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
		uint maximum) where TPlatform : struct, IMuiClassServicePlatform
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
		where TPlatform : struct, IMuiClassServicePlatform =>
		serviceState.IsNotNull &&
		MuiClassServiceStateCodec.TryRead(ref platform, serviceState,
			out var serviceValue) && serviceValue.Magic == MuiClassServiceLayout.Magic;

	private static APTR Headless<TPlatform>(ref TPlatform platform,
		APTR serviceState) where TPlatform : struct, IMuiClassServicePlatform =>
		MuiClassServiceStateCodec.TryRead(ref platform, serviceState,
			out var serviceValue) ? serviceValue.Headless : APTR.Null;
}
