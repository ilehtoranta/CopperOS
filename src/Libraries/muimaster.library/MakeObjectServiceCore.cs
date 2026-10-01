/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Named bounded cursor for MUI_MakeObject menu/control-label scanning.  The
// guest string remains caller-owned; this adapter centralizes the 4 KiB bound,
// address overflow, and mapped-byte admission used by underscore-key lookup.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMakeObjectControlCharByteCursor
{
	internal const uint MaximumLength = 4096;
	internal APTR Text;
	internal uint Index;
}

internal static class MuiMakeObjectControlCharByteCursorCodec
{
	internal static bool TryReadByte<TPlatform>(ref TPlatform platform,
		MuiMakeObjectControlCharByteCursor cursor, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Text;
		shared.Index = cursor.Index;
		shared.Limit = MuiMakeObjectControlCharByteCursor.MaximumLength;
		return MuiCStringByteCursorCodec.TryReadByte(ref platform, shared,
			out value);
	}
}

// Host-side view of the variable MUI_MakeObjectA parameter prefix. The guest
// vector is decoded once at this boundary; construction code consumes named
// fields rather than repeating byte offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMakeObjectParameterRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	// Compatibility aliases retained for callers that still inspect the wire
	// layout. The live cursor adapter walks this record declaration instead.
	internal const uint FirstOffset = 0;
	internal const uint SecondOffset = 4;
	internal const uint ThirdOffset = 8;
	internal const uint FourthOffset = 12;

	internal uint First;
	internal uint Second;
	internal uint Third;
	internal uint Fourth;
}

// Internal construction plan for one MUIO_* form. Keeping the type-specific
// parameters together prevents callers from splitting the decoded varargs
// record into positional scalar arguments between validation and TagItem
// generation.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMakeObjectBuildShapeRecord
{
	internal uint Type;
	internal MuiMakeObjectParameterRecord Parameters;
	internal uint ClassKind;
	internal uint TagCount; // Maximum emitted count, including optional tags.
	internal uint PreParseKind;
}

internal enum MuiMakeObjectParameterField : byte
{
	First,
	Second,
	Third,
	Fourth,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMakeObjectParameterFieldCursor
{
	internal APTR Base;
	internal MuiMakeObjectParameterField Field;
}

internal static class MuiMakeObjectParameterFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMakeObjectParameterFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiMakeObjectParameterMemoryCodec.TryGetAddress(ref platform,
			cursor.Base, cursor.Field, MuiMakeObjectParameterRecord.Size,
			out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMakeObjectParameterFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiMakeObjectParameterMemoryCodec.TryGetAddress(ref platform,
			cursor.Base, cursor.Field, MuiMakeObjectParameterRecord.Size,
			out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR parameters, MuiMakeObjectParameterField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiMakeObjectParameterMemoryCodec.TryReadUInt32(ref platform,
			parameters, field, MuiMakeObjectParameterRecord.Size, out value);
}

// Struct-first guest-memory adapter for the variable MUI_MakeObjectA
// parameter prefix. The caller supplies the admitted prefix length so a
// short object form does not require bytes beyond its actual vector.
internal static class MuiMakeObjectParameterMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiMakeObjectParameterField field,
		out uint index)
	{
		if (field == MuiMakeObjectParameterField.First) index = 0;
		else if (field == MuiMakeObjectParameterField.Second) index = 1;
		else if (field == MuiMakeObjectParameterField.Third) index = 2;
		else if (field == MuiMakeObjectParameterField.Fourth) index = 3;
		else { index = uint.MaxValue; return false; }
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR parameters, MuiMakeObjectParameterField field, uint availableBytes,
		out APTR address) where TPlatform : struct, IMuiGuestMemory =>
		TryGetAddress(ref platform, parameters, field, availableBytes,
			out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR parameters, MuiMakeObjectParameterField field, uint availableBytes,
		out APTR address, out uint fieldSize) where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(field, out var index) || parameters.IsNull)
			return false;
		var requiredBytes = (index + 1) * MuiMakeObjectParameterRecord.FieldSize;
		if (availableBytes < requiredBytes ||
			!MuiGuestStructCursor.TryCreate(ref platform, parameters, requiredBytes,
				out var cursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiMakeObjectParameterRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiMakeObjectParameterRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR parameters, MuiMakeObjectParameterField field, uint availableBytes,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, parameters, field, availableBytes,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}
}

internal static class MuiMakeObjectParameterCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR parameters, uint count, out MuiMakeObjectParameterRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (count == 0) return true;
		if (count > 4 || !MuiGuestStructCursor.TryCreate(ref platform,
			parameters, count * MuiMakeObjectParameterRecord.FieldSize,
			out var cursor)) return false;
		if (!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
			out var first)) return false;
		record.First = first;
		if (count > 1)
		{
			if (!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var second)) return false;
			record.Second = second;
		}
		if (count > 2)
		{
			if (!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var third)) return false;
			record.Third = third;
		}
		if (count > 3)
		{
			if (!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var fourth)) return false;
			record.Fourth = fourth;
		}
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	// Native qualification fixtures also need to build a caller-owned prefix.
	// Keep that exchange on the same complete named record as the reader so a
	// test or ABI adapter never has to reproduce parameter offsets.
	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR parameters, uint count, MuiMakeObjectParameterRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (count == 0) return true;
		if (count > 4 || !MuiGuestStructCursor.TryCreate(ref platform,
			parameters, count * MuiMakeObjectParameterRecord.FieldSize,
			out var cursor)) return false;
		if (!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			record.First)) return false;
		if (count > 1 && !MuiGuestStructCursor.TryWriteUInt32(ref platform,
			ref cursor, record.Second)) return false;
		if (count > 2 && !MuiGuestStructCursor.TryWriteUInt32(ref platform,
			ref cursor, record.Third)) return false;
		if (count > 3 && !MuiGuestStructCursor.TryWriteUInt32(ref platform,
			ref cursor, record.Fourth)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Fixed GadTools NewMenu entry as it crosses the guest-memory boundary. The
// parser consumes this named record; field access follows its declaration
// order and preserves the packed byte/word/long widths.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNewMenuRecord
{
	internal const uint Size = 20;
	internal const uint ByteFieldSize = 1;
	internal const uint WordFieldSize = 2;
	internal const uint LongFieldSize = 4;
	// Compatibility aliases retained for callers that still inspect the wire
	// layout. The live cursor adapter walks this record declaration instead.
	internal const uint TypeOffset = 0;
	internal const uint PaddingOffset = 1;
	internal const uint LabelOffset = 2;
	internal const uint CommandKeyOffset = 6;
	internal const uint FlagsOffset = 10;
	internal const uint MutualExcludeOffset = 12;
	internal const uint UserDataOffset = 16;
	internal byte Type;
	internal byte Padding;
	internal uint Label;
	internal uint CommandKey;
	internal ushort Flags;
	internal uint MutualExclude;
	internal uint UserData;
}

internal enum MuiNewMenuField : byte
{
	Type,
	Padding,
	Label,
	CommandKey,
	Flags,
	MutualExclude,
	UserData,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNewMenuFieldCursor
{
	internal APTR Record;
	internal MuiNewMenuField Field;
}

internal static class MuiNewMenuFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiNewMenuFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNewMenuRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNewMenuField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNewMenuRecordMemoryCodec.TryReadUInt32(ref platform, record, field,
			out value);

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiNewMenuField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNewMenuRecordMemoryCodec.TryReadUInt16(ref platform, record, field,
			out value);

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiNewMenuField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNewMenuRecordMemoryCodec.TryReadUInt8(ref platform, record, field,
			out value);
}

// Struct-first guest-memory adapter for one packed GadTools NewMenu record.
// The named record owns the byte/word/long positions; this bounded adapter is
// the only place that projects those positions into guest memory.
internal static class MuiNewMenuRecordMemoryCodec
{
	private static bool TryResolveField(MuiNewMenuField field, out uint index,
		out uint fieldSize)
	{
		fieldSize = MuiNewMenuRecord.LongFieldSize;
		if (field == MuiNewMenuField.Type) { index = 0; fieldSize = MuiNewMenuRecord.ByteFieldSize; }
		else if (field == MuiNewMenuField.Padding) { index = 1; fieldSize = MuiNewMenuRecord.ByteFieldSize; }
		else if (field == MuiNewMenuField.Label) index = 2;
		else if (field == MuiNewMenuField.CommandKey) index = 3;
		else if (field == MuiNewMenuField.Flags) { index = 4; fieldSize = MuiNewMenuRecord.WordFieldSize; }
		else if (field == MuiNewMenuField.MutualExclude) index = 5;
		else if (field == MuiNewMenuField.UserData) index = 6;
		else { index = uint.MaxValue; fieldSize = 0; return false; }
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiNewMenuField field, out APTR address,
		out uint fieldSize) where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveField(field, out var index, out fieldSize) ||
			!MuiGuestStructCursor.TryCreate(ref platform, record,
				MuiNewMenuRecord.Size, out var cursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			uint width;
			if (current < 2) width = MuiNewMenuRecord.ByteFieldSize;
			else if (current == 4) width = MuiNewMenuRecord.WordFieldSize;
			else width = MuiNewMenuRecord.LongFieldSize;
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor, width,
				out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNewMenuField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address,
			out var fieldSize) || fieldSize != MuiNewMenuRecord.LongFieldSize)
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiNewMenuField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address,
			out var fieldSize) || fieldSize != MuiNewMenuRecord.WordFieldSize)
			return false;
		value = platform.ReadUInt16(address, 0);
		return true;
	}

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiNewMenuField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address,
			out var fieldSize) || fieldSize != MuiNewMenuRecord.ByteFieldSize)
			return false;
		value = platform.ReadUInt8(address, 0);
		return true;
	}
}

internal static class MuiNewMenuRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiNewMenuRecord record) where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNewMenuRecord.Size, out var cursor)) return false;
		if (!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
			out var type) || !MuiGuestStructCursor.TryReadUInt8(ref platform,
			ref cursor, out var padding) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var label) || !MuiGuestStructCursor.TryReadUInt32(ref platform,
				ref cursor, out var commandKey) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var flags) || !MuiGuestStructCursor.TryReadUInt32(ref platform,
				ref cursor, out var mutualExclude) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var userData) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		record.Type = type;
		record.Padding = padding;
		record.Label = label;
		record.CommandKey = commandKey;
		record.Flags = flags;
		record.MutualExclude = mutualExclude;
		record.UserData = userData;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiNewMenuRecord record) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNewMenuRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				record.Type) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				record.Padding) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Label) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.CommandKey) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				record.Flags) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.MutualExclude) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.UserData)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNewMenuCursor
{
	internal const uint EntrySize = MuiNewMenuRecord.Size;
	internal const uint MaximumEntries = 256;
	internal APTR Base;
	internal uint Index;
}

// Temporary native menu-construction vectors contain only object pointers.
// Keep their four-byte wire element named and bounded so menu planning never
// exposes an ad-hoc pointer offset to the construction code.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeMenuObjectPointerRecord
{
	internal const uint Size = 4;
	internal APTR Object;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeMenuObjectVectorCursor
{
	internal const uint MaximumEntries = MuiNewMenuCursor.MaximumEntries;
	internal APTR Base;
	internal uint Index;
}

internal static class MuiNativeMenuObjectVectorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiNativeMenuObjectVectorCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryGetRecordAddress(ref platform, cursor.Base, cursor.Index,
			MuiNativeMenuObjectVectorCursor.MaximumEntries,
			MuiNativeMenuObjectPointerRecord.Size, out address);
	}

	internal static bool TryGetRecordAddress<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, uint maximumEntries, uint recordSize,
		out APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (vector.IsNull || index >= maximumEntries || recordSize == 0 ||
			index == uint.MaxValue || index + 1 > uint.MaxValue / recordSize)
			return false;
		var byteSize = (index + 1) * recordSize;
		if (!MuiGuestStructCursor.TryCreate(ref platform, vector, byteSize,
			out var cursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				recordSize, out var candidate)) return false;
			if (current == index) address = candidate;
		}
		return address.IsNotNull && cursor.Remaining == 0;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		MuiNativeMenuObjectVectorCursor cursor, out APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = APTR.Null;
		if (!TryGetEntry(ref platform, cursor, out var address) ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiNativeMenuObjectPointerRecord.Size, out var recordCursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref recordCursor,
				out var raw) || !MuiGuestStructCursor.IsComplete(recordCursor))
			return false;
		value = APTR.FromPointer(raw);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		MuiNativeMenuObjectVectorCursor cursor, APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetEntry(ref platform, cursor, out var address) ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiNativeMenuObjectPointerRecord.Size, out var recordCursor))
			return false;
		return MuiGuestStructCursor.TryWriteUInt32(ref platform, ref recordCursor,
			value.Raw) && MuiGuestStructCursor.IsComplete(recordCursor);
	}
}

// Struct-first guest-memory adapter for caller-owned NewMenu vectors.
// Complete 20-byte records and the MorphOS 256-entry bound are admitted here;
// the typed cursor remains a compatibility wrapper for existing callers.
internal static class MuiNewMenuVectorMemoryCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNativeMenuObjectVectorCodec.TryGetRecordAddress(ref platform,
			vector, index, MuiNewMenuCursor.MaximumEntries,
			MuiNewMenuRecord.Size, out address);
}

// Production vector bridge. Each caller-owned NewMenu slot is admitted by
// the bounded vector adapter and exchanged as the complete named record;
// callers do not reproduce slot arithmetic or packed field positions.
internal static class MuiNewMenuVectorCodec
{
	internal static bool TryAdvance(ref MuiNewMenuCursor cursor, uint items)
	{
		if (items == 0 || cursor.Index > uint.MaxValue - items)
			return false;
		var next = cursor.Index + items;
		if (next > MuiNewMenuCursor.MaximumEntries) return false;
		cursor.Index = next;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		MuiNewMenuCursor cursor, out MuiNewMenuRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiNewMenuCursorCodec.TryGetEntry(ref platform, cursor,
			out var address) || !MuiNewMenuRecordCodec.TryRead(ref platform, address,
			out value))
		{
			value = default;
			return false;
		}
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		MuiNewMenuCursor cursor, MuiNewMenuRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiNewMenuCursorCodec.TryGetEntry(ref platform, cursor,
			out var address)) return false;
		return MuiNewMenuRecordCodec.Write(ref platform, address, value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR vector,
		uint index, out MuiNewMenuRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiNewMenuVectorMemoryCodec.TryGetEntry(ref platform, vector, index,
			out var address) || !MuiNewMenuRecordCodec.TryRead(ref platform, address,
			out value))
		{
			value = default;
			return false;
		}
		return true;
	}
}

internal static class MuiNewMenuCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiNewMenuCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNewMenuVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);
	}

// Native-safe bounded implementation of the MorphOS MUI_MakeObjectA helper.
// The parameter vector is not a TagItem list: each object type has its own
// fixed prefix. Unsupported types are rejected until their MorphOS contracts
// have a dedicated implementation and qualification evidence.
public static class MuiMakeObjectServiceCore
{
	public const uint MUIO_Label = 1;
	public const uint MUIO_Button = 2;
	public const uint MUIO_Checkmark = 3;
	public const uint MUIO_Cycle = 4;
	public const uint MUIO_Radio = 5;
	public const uint MUIO_Slider = 6;
	public const uint MUIO_String = 7;
	public const uint MUIO_PopButton = 8;
	public const uint MUIO_HSpace = 9;
	public const uint MUIO_VSpace = 10;
	public const uint MUIO_HBar = 11;
	public const uint MUIO_VBar = 12;
	public const uint MUIO_MenustripNM = 13;
	public const uint MUIO_Menuitem = 14;
	public const uint MUIO_BarTitle = 15;
	public const uint MUIO_NumericButton = 16;

	private const uint LabelSingleFrame = 0x00000100;
	private const uint LabelDoubleFrame = 0x00000200;
	private const uint LabelLeftAligned = 0x00000400;
	private const uint LabelCentered = 0x00000800;
	private const uint LabelFreeVert = 0x00001000;
	private const uint LabelTiny = 0x00002000;
	private const uint LabelDontCopy = 0x00004000;
	private const uint LabelKnownFlags = 0x00007FFF;

	private const uint Frame = 0x8042AC64;
	private const uint InputMode = 0x8042FB04;
	private const uint Background = 0x8042545B;
	private const uint Font = 0x8042BE50;
	private const uint Selected = 0x8042654B;
	private const uint ShowSelState = 0x8042CAAC;
	private const uint ImageSpec = 0x804233D5;
	private const uint ImageFreeHoriz = 0x8042DA84;
	private const uint ImageFreeVert = 0x8042EA28;
	private const uint TextContents = 0x8042F8DC;
	private const uint TextPreParse = 0x8042566D;
	private const uint TextHiChar = 0x804218FF;
	private const uint TextCopy = 0x80427727;
	private const uint TextSetVMax = 0x80420D8B;
	private const uint ControlChar = 0x8042120B;
	private const uint CycleChain = 0x80421CE7;
	private const uint CycleEntries = 0x80420629;
	private const uint RadioEntries = 0x8042B6A1;
	private const uint NumericMin = 0x8042E404;
	private const uint NumericMax = 0x8042D78A;
	private const uint NumericFormat = 0x804263E9;
	private const uint StringMaxLen = 0x80424984;
	private const uint MenuTitle = 0x8042A0E3;
	private const uint FamilyChild = 0x8042C696;
	private const uint MenuEnabled = 0x8042ED48;
	private const uint MenuitemTitle = 0x804218BE;
	private const uint MenuitemShortcut = 0x80422030;
	private const uint MenuitemCheckit = 0x80425ACE;
	private const uint MenuitemChecked = 0x8042562A;
	private const uint MenuitemToggle = 0x80424D5C;
	private const uint MenuitemEnabled = 0x8042AE0F;
	private const uint MenuitemExclude = 0x80420BC6;
	private const uint MenuitemCommandString = 0x8042B9CC;
	private const uint MenuitemCopyStrings = 0x8042DC1B;
	private const uint UserData = 0x80420313;
	private const uint FixWidth = 0x8042A3F1;
	private const uint FixHeight = 0x8042A92B;
	private const uint RectangleBarTitle = 0x80426689;
	private const uint RectangleHBar = 0x8042C943;
	private const uint RectangleVBar = 0x80422204;

	private const uint TextFrame = 3;
	private const uint ButtonFrame = 1;
	private const uint ImageButtonFrame = 2;
	private const uint InputModeRelVerify = 1;
	private const uint InputModeToggle = 3;
	private const uint ButtonBackground = 2;
	private const uint CheckmarkImage = 15;
	private const uint ButtonFont = unchecked((uint)-7);
	private const uint TinyFont = unchecked((uint)-3);
	private const uint MUIImageBuiltinMax = 0x00000093;
	private const uint MUIO_MenustripNMCommandKeyCheck = 1;
	private const uint MUIO_MenuitemCopyStrings = 0x40000000;
	private const uint NewMenuBarLabel = 0xFFFFFFFF;
	private const uint NewMenuMenuDisabled = 0x0001;
	private const uint NewMenuItemDisabled = 0x0010;
	private const uint NewMenuCheckit = 0x0001;
	private const uint NewMenuChecked = 0x0100;
	private const uint NewMenuToggle = 0x0008;
	private const uint NewMenuCommandString = 0x0020;
	private const uint MaximumMenuEntries = 256;

	private const uint ClassText = 1;
	private const uint ClassRectangle = 2;
	private const uint ClassImage = 3;
	private const uint ClassCycle = 4;
	private const uint ClassRadio = 5;
	private const uint ClassSlider = 6;
	private const uint ClassString = 7;
	private const uint ClassNumericbutton = 8;
	private const uint ClassMenustrip = 9;
	private const uint ClassMenu = 10;
	private const uint ClassMenuitem = 11;
	private const uint ClassNameStorage = MuiMakeObjectClassNameRecord.Size;
	private const uint TagStorage = 88; // ten TagItems plus TAG_DONE
	private const uint NativeRectangleTagCapacity = 3; // two tags plus TAG_DONE
	private const uint NativeRectangleTagStorage =
		NativeRectangleTagCapacity * MuiAslTagItemRecord.Size;
	private const uint MaximumCString = 4096;

	public static APTR MakeObjectA<TPlatform>(ref TPlatform platform, APTR state,
		uint type, APTR parameters) where TPlatform : struct, IMuiServicePlatform
	{
		uint parameterCount;
		if (!ParameterCount(type, out parameterCount)) return APTR.Null;
		if (!MuiMakeObjectParameterCodec.TryRead(ref platform, parameters,
			parameterCount, out var parameterRecord)) return APTR.Null;
		if (type == MUIO_MenustripNM)
			return MakeMenustripNM(ref platform, state, parameterRecord.First,
				parameterRecord.Second);

		if (!TryBuildShape(type, parameterRecord, out var shape))
			return APTR.Null;

		if ((type == MUIO_Button || type == MUIO_Label ||
				type == MUIO_BarTitle || type == MUIO_Cycle ||
				type == MUIO_Radio || type == MUIO_Slider ||
				type == MUIO_String || type == MUIO_NumericButton) &&
			!ValidCString(ref platform, parameterRecord.First))
			return APTR.Null;
		if (type == MUIO_Menuitem &&
			!ValidMenuitemLabel(ref platform, parameterRecord.First)) return APTR.Null;
		if (type == MUIO_Menuitem && parameterRecord.Second != 0 &&
			!ValidCString(ref platform, parameterRecord.Second)) return APTR.Null;
		if (type == MUIO_Menuitem &&
			(parameterRecord.Third & ~(NewMenuCheckit | NewMenuChecked | NewMenuToggle |
				NewMenuItemDisabled | NewMenuCommandString |
				MUIO_MenuitemCopyStrings)) != 0)
			return APTR.Null;
		if (type == MUIO_PopButton &&
			!ValidImageSpec(ref platform, parameterRecord.First))
			return APTR.Null;
		if (type == MUIO_NumericButton && parameterRecord.Fourth != 0 &&
			!ValidCString(ref platform, parameterRecord.Fourth)) return APTR.Null;
		if ((type == MUIO_Cycle || type == MUIO_Radio) &&
			!ValidEntryVector(ref platform, parameterRecord.Second,
				type == MUIO_Radio))
			return APTR.Null;

		var className = MuiHeadlessMemory.Allocate(ref platform, ClassNameStorage);
		var tags = MuiHeadlessMemory.Allocate(ref platform, TagStorage);
		var preParse = APTR.Null;
		if (className.IsNull || tags.IsNull)
		{
			ReleaseTemporary(ref platform, className, tags, preParse);
			return APTR.Null;
		}
		if (shape.PreParseKind != 0)
		{
			preParse = MuiHeadlessMemory.Allocate(ref platform,
				MuiMakeObjectPreParseRecord.Size);
			if (preParse.IsNull)
			{
				ReleaseTemporary(ref platform, className, tags, preParse);
				return APTR.Null;
			}
			var preParseRecord = default(MuiMakeObjectPreParseRecord);
			preParseRecord.Escape = 0x1B;
			preParseRecord.Command = shape.PreParseKind == 1 ? (byte)'c' : (byte)'l';
			if (!MuiMakeObjectPreParseRecordCodec.Write(ref platform, preParse,
				preParseRecord))
			{
				ReleaseTemporary(ref platform, className, tags, preParse);
				return APTR.Null;
			}
		}

		if (!WriteClassName(ref platform, className, shape.ClassKind) ||
			!WriteTags(ref platform, tags, shape, preParse))
		{
			ReleaseTemporary(ref platform, className, tags, preParse);
			return APTR.Null;
		}
		var classRecord = MuiHeadlessObjectCore.FindClassByName(ref platform,
			state, className);
		var obj = classRecord.IsNull ? APTR.Null :
			MuiCommonControlCore.CreateControl(ref platform, state, classRecord,
				tags);
		if (obj.IsNotNull && shape.ClassKind == ClassMenuitem &&
			AttachMenuSpecialist(ref platform, state, shape.ClassKind, obj).IsNull)
		{
			MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj);
			obj = APTR.Null;
		}
		ReleaseTemporary(ref platform, className, tags, preParse);
		return obj;
	}

	// First native public-vector MakeObjectA slice. MorphOS's simple rectangle,
	// text and menu forms can be bridged without pulling the full service aggregate
	// into the resident library. The temporary class-name, preparse and TagItem
	// vectors are complete named records; the public-object core owns the
	// resulting Intuition object and class lease after this call returns.
	internal static APTR MakeNativeObjectA(ref MuiNativeClassPlatform platform,
		APTR serviceState, APTR ownerRoot, APTR publicObjects, uint type,
		APTR parameters)
	{
		if (type != MUIO_HSpace && type != MUIO_VSpace &&
			type != MUIO_HBar && type != MUIO_VBar && type != MUIO_BarTitle &&
			type != MUIO_Button && type != MUIO_Label &&
			type != MUIO_Checkmark && type != MUIO_PopButton &&
			type != MUIO_Cycle && type != MUIO_Radio &&
			type != MUIO_Slider && type != MUIO_String &&
			type != MUIO_NumericButton && type != MUIO_Menuitem &&
			type != MUIO_MenustripNM)
			return APTR.Null;
		if (!ParameterCount(type, out var parameterCount) ||
			!MuiMakeObjectParameterCodec.TryRead(ref platform, parameters,
				parameterCount, out var parameterRecord)) return APTR.Null;
		if (type == MUIO_MenustripNM)
			return MakeNativeMenustripNMStruct(ref platform, serviceState, ownerRoot,
				publicObjects, parameterRecord.First, parameterRecord.Second);

		if (!TryBuildShape(type, parameterRecord, out var shape) ||
			(shape.ClassKind != ClassRectangle && shape.ClassKind != ClassText &&
				shape.ClassKind != ClassImage && shape.ClassKind != ClassCycle &&
				shape.ClassKind != ClassRadio && shape.ClassKind != ClassSlider &&
				shape.ClassKind != ClassString &&
				shape.ClassKind != ClassNumericbutton &&
				shape.ClassKind != ClassMenuitem))
			return APTR.Null;
		if ((type == MUIO_BarTitle || type == MUIO_Button || type == MUIO_Label ||
			type == MUIO_Cycle || type == MUIO_Radio || type == MUIO_Slider ||
			type == MUIO_String || type == MUIO_NumericButton) &&
			!ValidCString(ref platform, parameterRecord.First)) return APTR.Null;
		if (type == MUIO_Menuitem &&
			!ValidMenuitemLabel(ref platform, parameterRecord.First)) return APTR.Null;
		if (type == MUIO_Menuitem && parameterRecord.Second != 0 &&
			!ValidCString(ref platform, parameterRecord.Second)) return APTR.Null;
		if (type == MUIO_Menuitem &&
			(parameterRecord.Third & ~(NewMenuCheckit | NewMenuChecked | NewMenuToggle |
				NewMenuItemDisabled | NewMenuCommandString |
				MUIO_MenuitemCopyStrings)) != 0) return APTR.Null;

		var className = platform.Allocate(ClassNameStorage,
			MuiHeadlessLayout.AllocationFlags);
		if (className.IsNull) return APTR.Null;
		var tagStorage = shape.ClassKind == ClassRectangle ? NativeRectangleTagStorage :
			TagStorage;
		var tags = platform.Allocate(tagStorage,
			MuiHeadlessLayout.AllocationFlags);
		if (tags.IsNull)
		{
			platform.Free(className, ClassNameStorage);
			return APTR.Null;
		}
		var preParse = APTR.Null;
		if (shape.PreParseKind != 0)
		{
			preParse = platform.Allocate(MuiMakeObjectPreParseRecord.Size,
				MuiHeadlessLayout.AllocationFlags);
			if (preParse.IsNull)
			{
				platform.Free(tags, tagStorage);
				platform.Free(className, ClassNameStorage);
				return APTR.Null;
			}
			var preParseRecord = default(MuiMakeObjectPreParseRecord);
			preParseRecord.Escape = 0x1B;
			preParseRecord.Command = shape.PreParseKind == 1 ? (byte)'c' : (byte)'l';
			if (!MuiMakeObjectPreParseRecordCodec.Write(ref platform, preParse,
				preParseRecord))
			{
				platform.Free(preParse, MuiMakeObjectPreParseRecord.Size);
				platform.Free(tags, tagStorage);
				platform.Free(className, ClassNameStorage);
				return APTR.Null;
			}
		}

		if (!WriteClassName(ref platform, className, shape.ClassKind) ||
			!WriteNativeObjectTags(ref platform, tags, shape, preParse))
		{
			if (preParse.IsNotNull) platform.Free(preParse,
				MuiMakeObjectPreParseRecord.Size);
			platform.Free(tags, tagStorage);
			platform.Free(className, ClassNameStorage);
			return APTR.Null;
		}

		var result = MuiNativePublicObjectCore.NewObject(ref platform,
			serviceState, ownerRoot, publicObjects, className, tags,
			NativeClassId(shape.ClassKind));
		if (preParse.IsNotNull) platform.Free(preParse,
			MuiMakeObjectPreParseRecord.Size);
		platform.Free(tags, tagStorage);
		platform.Free(className, ClassNameStorage);
		return result;
	}

	private static APTR NativeClassId(uint classKind)
	{
		return classKind switch
		{
			ClassText => APTR.FromPointer(CString.ToUInt32(
				CString.FromLiteral("Text.mui"))),
			ClassRectangle => APTR.FromPointer(CString.ToUInt32(
				CString.FromLiteral("Rectangle.mui"))),
			ClassImage => APTR.FromPointer(CString.ToUInt32(
				CString.FromLiteral("Image.mui"))),
			ClassCycle => APTR.FromPointer(CString.ToUInt32(
				CString.FromLiteral("Cycle.mui"))),
			ClassRadio => APTR.FromPointer(CString.ToUInt32(
				CString.FromLiteral("Radio.mui"))),
			ClassSlider => APTR.FromPointer(CString.ToUInt32(
				CString.FromLiteral("Slider.mui"))),
			ClassString => APTR.FromPointer(CString.ToUInt32(
				CString.FromLiteral("String.mui"))),
			ClassNumericbutton => APTR.FromPointer(CString.ToUInt32(
				CString.FromLiteral("Numericbutton.mui"))),
			ClassMenustrip => APTR.FromPointer(CString.ToUInt32(
				CString.FromLiteral("Menustrip.mui"))),
			ClassMenu => APTR.FromPointer(CString.ToUInt32(
				CString.FromLiteral("Menu.mui"))),
			ClassMenuitem => APTR.FromPointer(CString.ToUInt32(
				CString.FromLiteral("Menuitem.mui"))),
			_ => APTR.Null
		};
	}

	private static bool ParameterCount(uint type, out uint count)
	{
		switch (type)
		{
			case MUIO_Label:
				count = 2; return true;
			case MUIO_Button:
			case MUIO_Checkmark:
			case MUIO_HSpace:
			case MUIO_VSpace:
			case MUIO_BarTitle:
				count = 1; return true;
			case MUIO_Cycle:
			case MUIO_Radio:
			case MUIO_String:
				count = 2; return true;
			case MUIO_PopButton:
				count = 1; return true;
			case MUIO_Slider:
				count = 3; return true;
			case MUIO_MenustripNM:
				count = 2; return true;
			case MUIO_Menuitem:
				count = 4; return true;
			case MUIO_NumericButton:
				count = 4; return true;
			case MUIO_HBar:
			case MUIO_VBar:
				count = 1; return true;
			default:
				count = 0; return false;
		}
	}

	internal static bool TryBuildShape(uint type,
		MuiMakeObjectParameterRecord parameters,
		out MuiMakeObjectBuildShapeRecord shape)
	{
		shape = default;
		shape.Type = type;
		shape.Parameters = parameters;
		switch (type)
		{
			case MUIO_HSpace:
			case MUIO_VSpace:
			case MUIO_HBar:
			case MUIO_VBar:
			case MUIO_BarTitle:
				shape.ClassKind = ClassRectangle;
				shape.TagCount = type == MUIO_HBar || type == MUIO_VBar ? 2u : 1u;
				return true;
			case MUIO_Button:
				shape.ClassKind = ClassText;
				// MUIA_ControlChar is added when the label has an underscore key.
				shape.TagCount = 7;
				shape.PreParseKind = 1;
				return true;
			case MUIO_Checkmark:
				shape.ClassKind = ClassImage;
				shape.TagCount = 8;
				return true;
			case MUIO_PopButton:
				shape.ClassKind = ClassImage;
				shape.TagCount = 6;
				return true;
			case MUIO_Cycle:
				shape.ClassKind = ClassCycle;
				shape.TagCount = 5;
				return true;
			case MUIO_Radio:
				shape.ClassKind = ClassRadio;
				shape.TagCount = 2;
				return true;
			case MUIO_Slider:
				shape.ClassKind = ClassSlider;
				shape.TagCount = 3;
				return true;
			case MUIO_String:
				shape.ClassKind = ClassString;
				shape.TagCount = 3;
				return true;
			case MUIO_Menuitem:
				shape.ClassKind = ClassMenuitem;
				shape.TagCount = 8u + ((parameters.Third &
					NewMenuCommandString) != 0 ? 1u : 0u);
				return true;
			case MUIO_NumericButton:
				shape.ClassKind = ClassNumericbutton;
				shape.TagCount = 4;
				return true;
			case MUIO_Label:
				var flags = parameters.Second;
				if ((flags & ~LabelKnownFlags) != 0 ||
					(flags & LabelSingleFrame) != 0 &&
					(flags & LabelDoubleFrame) != 0 ||
					(flags & LabelLeftAligned) != 0 &&
					(flags & LabelCentered) != 0) return false;
				shape.ClassKind = ClassText;
				shape.TagCount = 2;
				if ((flags & LabelSingleFrame) != 0 ||
					(flags & LabelDoubleFrame) != 0) shape.TagCount++;
				if ((flags & LabelLeftAligned) != 0 ||
					(flags & LabelCentered) != 0) { shape.TagCount++;
					shape.PreParseKind =
					(flags & LabelCentered) != 0 ? 1u : 2u; }
				if ((flags & LabelFreeVert) != 0) shape.TagCount++;
				if ((flags & LabelTiny) != 0) shape.TagCount++;
				if ((flags & 0xFF) != 0) shape.TagCount += 2;
				return shape.TagCount <= 8;
			default:
				return false;
		}
	}

	private static bool WriteClassName<TPlatform>(ref TPlatform platform,
		APTR address, uint classKind) where TPlatform : struct, IMuiGuestMemory
	{
		var value = default(MuiMakeObjectClassNameRecord);
		switch (classKind)
		{
			case ClassText:
				value.Word0 = 0x54657874; // Text
				value.Word1 = 0x2E6D7569; // .mui
				break;
			case ClassRectangle:
				value.Word0 = 0x52656374; // Rect
				value.Word1 = 0x616E676C; // angl
				value.Word2 = 0x652E6D75; // e.mu
				value.Word3 = 0x69000000; // i\0
				break;
			case ClassImage:
				value.Word0 = 0x496D6167; // Imag
				value.Word1 = 0x652E6D75; // e.mu
				value.Word2 = 0x69000000; // i\0
				break;
			case ClassCycle:
				value.Word0 = 0x4379636C; // Cycl
				value.Word1 = 0x652E6D75; // e.mu
				value.Word2 = 0x69000000; // i\0
				break;
			case ClassRadio:
				value.Word0 = 0x52616469; // Radi
				value.Word1 = 0x6F2E6D75; // o.mu
				value.Word2 = 0x69000000; // i\0
				break;
			case ClassSlider:
				value.Word0 = 0x536C6964; // Slid
				value.Word1 = 0x65722E6D; // er.m
				value.Word2 = 0x75690000; // ui\0
				break;
			case ClassString:
				value.Word0 = 0x53747269; // Stri
				value.Word1 = 0x6E672E6D; // ng.m
				value.Word2 = 0x75690000; // ui\0
				break;
			case ClassNumericbutton:
				value.Word0 = 0x4E756D65; // Nume
				value.Word1 = 0x72696362; // ricb
				value.Word2 = 0x7574746F; // utto
				value.Word3 = 0x6E2E6D75; // n.mu
				value.Word4 = 0x69000000; // i\0
				break;
			case ClassMenustrip:
				value.Word0 = 0x4D656E75; // Menu
				value.Word1 = 0x73747269; // stri
				value.Word2 = 0x702E6D75; // p.mu
				value.Word3 = 0x69000000; // i\0
				break;
			case ClassMenu:
				value.Word0 = 0x4D656E75; // Menu
				value.Word1 = 0x2E6D7569; // .mui
				break;
			case ClassMenuitem:
				value.Word0 = 0x4D656E75; // Menu
				value.Word1 = 0x6974656D; // item
				value.Word2 = 0x2E6D7569; // .mui
				break;
			default:
				return false;
		}
		return MuiMakeObjectClassNameRecordCodec.WriteRecord(ref platform, address,
			value);
	}

	private static bool WriteTags<TPlatform>(ref TPlatform platform, APTR tags,
		MuiMakeObjectBuildShapeRecord shape, APTR preParse)
		where TPlatform : struct, IMuiGuestMemory
	{
		uint index = 0;
		if (shape.Type == MUIO_HSpace) AddTag(ref platform, tags, ref index,
			FixWidth, shape.Parameters.First);
		else if (shape.Type == MUIO_VSpace) AddTag(ref platform, tags, ref index,
			FixHeight, shape.Parameters.First);
		else if (shape.Type == MUIO_HBar)
		{
			AddTag(ref platform, tags, ref index, RectangleHBar, 1);
			AddTag(ref platform, tags, ref index, FixHeight,
				shape.Parameters.First);
		}
		else if (shape.Type == MUIO_VBar)
		{
			AddTag(ref platform, tags, ref index, RectangleVBar, 1);
			AddTag(ref platform, tags, ref index, FixWidth,
				shape.Parameters.First);
		}
		else if (shape.Type == MUIO_BarTitle) AddTag(ref platform, tags, ref index,
			RectangleBarTitle, shape.Parameters.First);
		else if (shape.Type == MUIO_Button)
			return WriteButtonTagRecords(ref platform, tags, shape, preParse);
		else if (shape.Type == MUIO_Checkmark)
		{
			AddTag(ref platform, tags, ref index, Frame, ImageButtonFrame);
			AddTag(ref platform, tags, ref index, InputMode, InputModeToggle);
			AddTag(ref platform, tags, ref index, ImageSpec, CheckmarkImage);
			AddTag(ref platform, tags, ref index, ImageFreeVert, 1);
			AddTag(ref platform, tags, ref index, Selected,
				shape.Parameters.First);
			AddTag(ref platform, tags, ref index, Background, ButtonBackground);
			AddTag(ref platform, tags, ref index, ShowSelState, 0);
		}
		else if (shape.Type == MUIO_PopButton)
		{
			AddTag(ref platform, tags, ref index, Frame, ImageButtonFrame);
			AddTag(ref platform, tags, ref index, Background, ButtonBackground);
			AddTag(ref platform, tags, ref index, ImageSpec,
				shape.Parameters.First);
			AddTag(ref platform, tags, ref index, InputMode, InputModeRelVerify);
			AddTag(ref platform, tags, ref index, ImageFreeVert, 1);
			AddTag(ref platform, tags, ref index, ImageFreeHoriz, 0);
		}
		else if (shape.Type == MUIO_Cycle)
		{
			AddTag(ref platform, tags, ref index, Frame, ButtonFrame);
			AddTag(ref platform, tags, ref index, Font, ButtonFont);
			AddTag(ref platform, tags, ref index, CycleEntries,
				shape.Parameters.Second);
			var key = ControlCharFromCString(ref platform,
				shape.Parameters.First);
			if (key != 0) AddTag(ref platform, tags, ref index, ControlChar, key);
			AddTag(ref platform, tags, ref index, CycleChain, 1);
		}
		else if (shape.Type == MUIO_Radio)
		{
			AddTag(ref platform, tags, ref index, RadioEntries,
				shape.Parameters.Second);
			var key = ControlCharFromCString(ref platform,
				shape.Parameters.First);
			if (key != 0) AddTag(ref platform, tags, ref index, ControlChar, key);
		}
		else if (shape.Type == MUIO_Slider)
		{
			AddTag(ref platform, tags, ref index, NumericMin,
				shape.Parameters.Second);
			AddTag(ref platform, tags, ref index, NumericMax,
				shape.Parameters.Third);
			var key = ControlCharFromCString(ref platform,
				shape.Parameters.First);
			if (key != 0) AddTag(ref platform, tags, ref index, ControlChar, key);
		}
		else if (shape.Type == MUIO_String)
		{
			AddTag(ref platform, tags, ref index, Frame, 4);
			AddTag(ref platform, tags, ref index, StringMaxLen,
				shape.Parameters.Second);
			var key = ControlCharFromCString(ref platform,
				shape.Parameters.First);
			if (key != 0) AddTag(ref platform, tags, ref index, ControlChar, key);
		}
		else if (shape.Type == MUIO_NumericButton)
		{
			AddTag(ref platform, tags, ref index, NumericMin,
				shape.Parameters.Second);
			AddTag(ref platform, tags, ref index, NumericMax,
				shape.Parameters.Third);
			if (shape.Parameters.Fourth != 0) AddTag(ref platform, tags,
				ref index, NumericFormat, shape.Parameters.Fourth);
			var key = ControlCharFromCString(ref platform,
				shape.Parameters.First);
			if (key != 0) AddTag(ref platform, tags, ref index, ControlChar, key);
		}
		else if (shape.Type == MUIO_Menuitem)
		{
			// CopyStrings is an init-only latch and must precede the title and
			// shortcut tags so their setters take ownership during OM_NEW.
			AddTag(ref platform, tags, ref index, MenuitemCopyStrings,
				(shape.Parameters.Third & MUIO_MenuitemCopyStrings) != 0 ? 1u : 0u);
			AddTag(ref platform, tags, ref index, MenuitemTitle,
				shape.Parameters.First);
			AddTag(ref platform, tags, ref index, MenuitemShortcut,
				shape.Parameters.Second);
			AddTag(ref platform, tags, ref index, MenuitemCheckit,
				(shape.Parameters.Third & NewMenuCheckit) != 0 ? 1u : 0u);
			AddTag(ref platform, tags, ref index, MenuitemChecked,
				(shape.Parameters.Third & NewMenuChecked) != 0 ? 1u : 0u);
			AddTag(ref platform, tags, ref index, MenuitemToggle,
				(shape.Parameters.Third & NewMenuToggle) != 0 ? 1u : 0u);
			AddTag(ref platform, tags, ref index, MenuitemEnabled,
				(shape.Parameters.Third & NewMenuItemDisabled) == 0 ? 1u : 0u);
			if ((shape.Parameters.Third & NewMenuCommandString) != 0)
				AddTag(ref platform, tags, ref index, MenuitemCommandString, 1);
			AddTag(ref platform, tags, ref index, UserData,
				shape.Parameters.Fourth);
		}
		else if (shape.Type == MUIO_Label)
		{
			var flags = shape.Parameters.Second;
			AddTag(ref platform, tags, ref index, TextContents,
				shape.Parameters.First);
			if ((flags & LabelSingleFrame) != 0)
				AddTag(ref platform, tags, ref index, Frame, TextFrame);
			else if ((flags & LabelDoubleFrame) != 0)
				AddTag(ref platform, tags, ref index, Frame, ButtonFrame);
			if ((flags & (LabelLeftAligned | LabelCentered)) != 0)
				AddTag(ref platform, tags, ref index, TextPreParse, preParse.Raw);
			if ((flags & LabelFreeVert) != 0)
				AddTag(ref platform, tags, ref index, TextSetVMax, 0);
			if ((flags & LabelTiny) != 0)
				AddTag(ref platform, tags, ref index, Font, TinyFont);
			AddTag(ref platform, tags, ref index, TextCopy,
				(flags & LabelDontCopy) != 0 ? 0u : 1u);
			if ((flags & 0xFF) != 0)
			{
				var key = flags & 0xFF;
				AddTag(ref platform, tags, ref index, TextHiChar, key);
				AddTag(ref platform, tags, ref index, 0x8042120B, key);
			}
		}
		WriteTagDone(ref platform, tags, index);
		return true;
	}

	private static bool WriteNativeRectangleTags<TPlatform>(ref TPlatform platform,
		APTR tags, MuiMakeObjectBuildShapeRecord shape)
		where TPlatform : struct, IMuiGuestMemory
	{
		var index = 0u;
		if (shape.Type == MUIO_HSpace)
			AddTag(ref platform, tags, ref index, FixWidth,
				shape.Parameters.First);
		else if (shape.Type == MUIO_VSpace)
			AddTag(ref platform, tags, ref index, FixHeight,
				shape.Parameters.First);
		else if (shape.Type == MUIO_HBar)
		{
			AddTag(ref platform, tags, ref index, RectangleHBar, 1);
			AddTag(ref platform, tags, ref index, FixHeight,
				shape.Parameters.First);
		}
		else if (shape.Type == MUIO_VBar)
		{
			AddTag(ref platform, tags, ref index, RectangleVBar, 1);
			AddTag(ref platform, tags, ref index, FixWidth,
				shape.Parameters.First);
		}
		else if (shape.Type == MUIO_BarTitle)
			AddTag(ref platform, tags, ref index, RectangleBarTitle,
				shape.Parameters.First);
		else return false;
		WriteTagDone(ref platform, tags, index);
		return true;
	}

	private static bool WriteNativeObjectTags<TPlatform>(ref TPlatform platform,
		APTR tags, MuiMakeObjectBuildShapeRecord shape, APTR preParse)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (shape.ClassKind == ClassRectangle)
			return WriteNativeRectangleTags(ref platform, tags, shape);
		return WriteTags(ref platform, tags, shape, preParse);
	}

	// Native menu construction is deliberately tag-first.  MUIA_Family_Child
	// is supplied while each Family object is born, so this route never needs a
	// raw BOOPSI DoMethodA import or an invented method address.  The named
	// pointer vectors below are only temporary planning storage; all durable
	// ownership is represented by MuiNativePublicObjectBinding.Parent.
	private static APTR MakeNativeMenustripNMStruct(
		ref MuiNativeClassPlatform platform, APTR serviceState, APTR ownerRoot,
		APTR publicObjects, uint newMenuRaw, uint flags)
	{
		var newMenu = APTR.FromPointer(newMenuRaw);
		if (ValidateNewMenuCode(ref platform, newMenu, flags) != 0)
			return APTR.Null;

		var vectorBytes = MuiNativeMenuObjectVectorCursor.MaximumEntries *
			MuiNativeMenuObjectPointerRecord.Size;
		var menuObjects = platform.Allocate(vectorBytes,
			MuiHeadlessLayout.AllocationFlags);
		var menuItemObjects = platform.Allocate(vectorBytes,
			MuiHeadlessLayout.AllocationFlags);
		var itemObjects = platform.Allocate(vectorBytes,
			MuiHeadlessLayout.AllocationFlags);
		var pendingObjects = platform.Allocate(vectorBytes,
			MuiHeadlessLayout.AllocationFlags);
		if (menuObjects.IsNull || menuItemObjects.IsNull || itemObjects.IsNull ||
			pendingObjects.IsNull)
		{
			FreeNativeMenuVectors(ref platform, vectorBytes, menuObjects,
				menuItemObjects, itemObjects, pendingObjects);
			return APTR.Null;
		}

		uint menuCount = 0;
		uint menuItemCount = 0;
		uint itemCount = 0;
		uint entryIndex = 0;
		var reachedEnd = false;
		while (entryIndex < MuiNewMenuCursor.MaximumEntries)
		{
			if (!MuiNewMenuVectorCodec.TryRead(ref platform, newMenu, entryIndex,
				out var titleRecord) ||
				!MuiNewMenuTypeRecordCodec.TryClassify(titleRecord.Type,
					out var titleKind))
				return FailNativeMenustripNM(ref platform, serviceState, ownerRoot,
					publicObjects, vectorBytes, menuObjects, menuItemObjects,
					itemObjects, pendingObjects, menuCount, menuItemCount,
					itemCount, 0, APTR.Null);
			if (titleKind == MuiNewMenuEntryKind.End)
			{
				reachedEnd = true;
				break;
			}
			if (titleKind == MuiNewMenuEntryKind.Ignored)
			{
				entryIndex++;
				continue;
			}
			if (titleKind != MuiNewMenuEntryKind.Title)
				return FailNativeMenustripNM(ref platform, serviceState, ownerRoot,
					publicObjects, vectorBytes, menuObjects, menuItemObjects,
					itemObjects, pendingObjects, menuCount, menuItemCount,
					itemCount, 0, APTR.Null);

			// Locate the end of this title's segment before constructing any
			// object.  A segment consists of Item/Sub records and ignored slots;
			// the next title or NM_END begins the next segment.
			var segmentEnd = entryIndex + 1;
			while (segmentEnd < MuiNewMenuCursor.MaximumEntries)
			{
				if (!MuiNewMenuVectorCodec.TryRead(ref platform, newMenu, segmentEnd,
					out var segmentRecord) ||
					!MuiNewMenuTypeRecordCodec.TryClassify(segmentRecord.Type,
						out var segmentKind))
					return FailNativeMenustripNM(ref platform, serviceState,
						ownerRoot, publicObjects, vectorBytes, menuObjects,
						menuItemObjects, itemObjects, pendingObjects, menuCount,
						menuItemCount, itemCount, 0, APTR.Null);
				if (segmentKind == MuiNewMenuEntryKind.End ||
					segmentKind == MuiNewMenuEntryKind.Title) break;
				if (segmentKind != MuiNewMenuEntryKind.Ignored &&
					segmentKind != MuiNewMenuEntryKind.Item &&
					segmentKind != MuiNewMenuEntryKind.Sub)
					return FailNativeMenustripNM(ref platform, serviceState,
						ownerRoot, publicObjects, vectorBytes, menuObjects,
						menuItemObjects, itemObjects, pendingObjects, menuCount,
						menuItemCount, itemCount, 0, APTR.Null);
				segmentEnd++;
			}
			if (segmentEnd >= MuiNewMenuCursor.MaximumEntries)
				return FailNativeMenustripNM(ref platform, serviceState, ownerRoot,
					publicObjects, vectorBytes, menuObjects, menuItemObjects,
					itemObjects, pendingObjects, menuCount, menuItemCount,
					itemCount, 0, APTR.Null);

			var menuItemStart = menuItemCount;
			var blockStart = entryIndex + 1;
			while (blockStart < segmentEnd)
			{
				if (!MuiNewMenuVectorCodec.TryRead(ref platform, newMenu, blockStart,
					out var itemRecord) ||
					!MuiNewMenuTypeRecordCodec.TryClassify(itemRecord.Type,
						out var itemKind))
					return FailNativeMenustripNM(ref platform, serviceState,
						ownerRoot, publicObjects, vectorBytes, menuObjects,
						menuItemObjects, itemObjects, pendingObjects, menuCount,
						menuItemCount, itemCount, 0, APTR.Null);
				if (itemKind == MuiNewMenuEntryKind.Ignored)
				{
					blockStart++;
					continue;
				}
				if (itemKind != MuiNewMenuEntryKind.Item)
					return FailNativeMenustripNM(ref platform, serviceState,
						ownerRoot, publicObjects, vectorBytes, menuObjects,
						menuItemObjects, itemObjects, pendingObjects, menuCount,
						menuItemCount, itemCount, 0, APTR.Null);

				var blockEnd = blockStart + 1;
				while (blockEnd < segmentEnd)
				{
					if (!MuiNewMenuVectorCodec.TryRead(ref platform, newMenu,
						blockEnd, out var blockRecord) ||
						!MuiNewMenuTypeRecordCodec.TryClassify(blockRecord.Type,
							out var blockKind))
						return FailNativeMenustripNM(ref platform, serviceState,
							ownerRoot, publicObjects, vectorBytes, menuObjects,
							menuItemObjects, itemObjects, pendingObjects, menuCount,
							menuItemCount, itemCount, 0, APTR.Null);
					if (blockKind == MuiNewMenuEntryKind.Ignored ||
						blockKind == MuiNewMenuEntryKind.Sub)
					{
						blockEnd++;
						continue;
					}
					if (blockKind == MuiNewMenuEntryKind.Item) break;
					return FailNativeMenustripNM(ref platform, serviceState,
						ownerRoot, publicObjects, vectorBytes, menuObjects,
						menuItemObjects, itemObjects, pendingObjects, menuCount,
						menuItemCount, itemCount, 0, APTR.Null);
				}

				uint pendingCount = 0;
				var subIndex = blockEnd;
				while (subIndex > blockStart + 1)
				{
					subIndex--;
					if (!MuiNewMenuVectorCodec.TryRead(ref platform, newMenu,
						subIndex, out var subRecord) ||
						!MuiNewMenuTypeRecordCodec.TryClassify(subRecord.Type,
							out var subKind))
						return FailNativeMenustripNM(ref platform, serviceState,
							ownerRoot, publicObjects, vectorBytes, menuObjects,
							menuItemObjects, itemObjects, pendingObjects, menuCount,
							menuItemCount, itemCount, pendingCount, APTR.Null);
					if (subKind == MuiNewMenuEntryKind.Ignored) continue;
					if (subKind != MuiNewMenuEntryKind.Sub ||
						pendingCount >= MuiNativeMenuObjectVectorCursor.MaximumEntries ||
						!ResolveMenuItemStrings(ref platform, subRecord.Label,
							subRecord.CommandKey, flags, out var subLabel,
							out var subShortcut))
						return FailNativeMenustripNM(ref platform, serviceState,
							ownerRoot, publicObjects, vectorBytes, menuObjects,
							menuItemObjects, itemObjects, pendingObjects, menuCount,
							menuItemCount, itemCount, pendingCount, APTR.Null);
				var subTags = MakeNativeMenuTags(ref platform, subLabel,
					subShortcut, subRecord.Flags, subRecord.MutualExclude,
					subRecord.UserData, pendingObjects, 0);
				if (subTags.IsNull)
					return FailNativeMenustripNM(ref platform, serviceState,
						ownerRoot, publicObjects, vectorBytes, menuObjects,
						menuItemObjects, itemObjects, pendingObjects, menuCount,
						menuItemCount, itemCount, pendingCount, APTR.Null);
				var subObject = CreateNativeMenuObjectStruct(ref platform,
					serviceState, ownerRoot, publicObjects, ClassMenuitem, subTags);
				var subTagBytes = NativeMenuItemTagBytes(0);
				platform.Free(subTags, subTagBytes);
				var subObjectCursor = NativeMenuObjectCursor(itemObjects, itemCount);
				if (subObject.IsNull || !MuiNativeMenuObjectVectorCodec.TryWrite(
					ref platform, subObjectCursor, subObject))
				{
					if (subObject.IsNotNull) platform.DisposeObject(subObject);
					return FailNativeMenustripNM(ref platform, serviceState,
						ownerRoot, publicObjects, vectorBytes, menuObjects,
						menuItemObjects, itemObjects, pendingObjects, menuCount,
						menuItemCount, itemCount, pendingCount, APTR.Null);
				}
				itemCount++;
				var pendingObjectCursor = NativeMenuObjectCursor(pendingObjects,
					pendingCount);
				if (!MuiNativeMenuObjectVectorCodec.TryWrite(ref platform,
					pendingObjectCursor, subObject))
					return FailNativeMenustripNM(ref platform, serviceState,
						ownerRoot, publicObjects, vectorBytes, menuObjects,
						menuItemObjects, itemObjects, pendingObjects, menuCount,
						menuItemCount, itemCount, pendingCount, APTR.Null);
				pendingCount++;
				}

				if (!ResolveMenuItemStrings(ref platform, itemRecord.Label,
					itemRecord.CommandKey, flags, out var itemLabel,
					out var itemShortcut))
					return FailNativeMenustripNM(ref platform, serviceState,
						ownerRoot, publicObjects, vectorBytes, menuObjects,
						menuItemObjects, itemObjects, pendingObjects, menuCount,
						menuItemCount, itemCount, pendingCount, APTR.Null);
				var itemTags = MakeNativeMenuTags(ref platform, itemLabel,
					itemShortcut, itemRecord.Flags, itemRecord.MutualExclude,
					itemRecord.UserData, pendingObjects, pendingCount);
				if (itemTags.IsNull)
					return FailNativeMenustripNM(ref platform, serviceState,
						ownerRoot, publicObjects, vectorBytes, menuObjects,
						menuItemObjects, itemObjects, pendingObjects, menuCount,
						menuItemCount, itemCount, pendingCount, APTR.Null);
				var itemObject = CreateNativeMenuObjectStruct(ref platform,
					serviceState, ownerRoot, publicObjects, ClassMenuitem, itemTags);
				var itemTagBytes = NativeMenuItemTagBytes(pendingCount);
				platform.Free(itemTags, itemTagBytes);
				if (itemObject.IsNull || itemCount >=
					MuiNativeMenuObjectVectorCursor.MaximumEntries ||
						!MuiNativeMenuObjectVectorCodec.TryWrite(ref platform,
							NativeMenuObjectCursor(itemObjects, itemCount), itemObject))
				{
					if (itemObject.IsNotNull) platform.DisposeObject(itemObject);
					return FailNativeMenustripNM(ref platform, serviceState,
						ownerRoot, publicObjects, vectorBytes, menuObjects,
						menuItemObjects, itemObjects, pendingObjects, menuCount,
						menuItemCount, itemCount, pendingCount, APTR.Null);
				}
				itemCount++;
				if (menuItemCount >= MuiNativeMenuObjectVectorCursor.MaximumEntries ||
					!MuiNativeMenuObjectVectorCodec.TryWrite(ref platform,
						NativeMenuObjectCursor(menuItemObjects, menuItemCount), itemObject))
					return FailNativeMenustripNM(ref platform, serviceState,
						ownerRoot, publicObjects, vectorBytes, menuObjects,
						menuItemObjects, itemObjects, pendingObjects, menuCount,
						menuItemCount, itemCount, pendingCount, APTR.Null);
				menuItemCount++;
				pendingCount = 0;
				blockStart = blockEnd;
			}

			if (menuCount >= MuiNativeMenuObjectVectorCursor.MaximumEntries)
				return FailNativeMenustripNM(ref platform, serviceState, ownerRoot,
					publicObjects, vectorBytes, menuObjects, menuItemObjects,
					itemObjects, pendingObjects, menuCount, menuItemCount,
					itemCount, 0, APTR.Null);
			var menuTags = MakeNativeMenuContainerTags(ref platform,
				titleRecord.Label, titleRecord.UserData, titleRecord.Flags,
				menuItemObjects, menuItemStart, menuItemCount - menuItemStart);
			if (menuTags.IsNull)
				return FailNativeMenustripNM(ref platform, serviceState, ownerRoot,
					publicObjects, vectorBytes, menuObjects, menuItemObjects,
					itemObjects, pendingObjects, menuCount, menuItemCount,
					itemCount, 0, APTR.Null);
			var menuObject = CreateNativeMenuObjectStruct(ref platform,
				serviceState, ownerRoot, publicObjects, ClassMenu, menuTags);
			platform.Free(menuTags, NativeMenuContainerTagBytes(
				menuItemCount - menuItemStart));
				if (menuObject.IsNull || !MuiNativeMenuObjectVectorCodec.TryWrite(
					ref platform, NativeMenuObjectCursor(menuObjects, menuCount),
					menuObject))
			{
				if (menuObject.IsNotNull) platform.DisposeObject(menuObject);
				return FailNativeMenustripNM(ref platform, serviceState, ownerRoot,
					publicObjects, vectorBytes, menuObjects, menuItemObjects,
					itemObjects, pendingObjects, menuCount, menuItemCount,
					itemCount, 0, APTR.Null);
			}
			menuCount++;
			entryIndex = segmentEnd;
		}

		if (!reachedEnd)
			return FailNativeMenustripNM(ref platform, serviceState, ownerRoot,
				publicObjects, vectorBytes, menuObjects, menuItemObjects,
				itemObjects, pendingObjects, menuCount, menuItemCount,
				itemCount, 0, APTR.Null);
		var stripTags = MakeNativeMenustripTags(ref platform, menuObjects,
			menuCount);
		if (stripTags.IsNull)
			return FailNativeMenustripNM(ref platform, serviceState, ownerRoot,
				publicObjects, vectorBytes, menuObjects, menuItemObjects,
				itemObjects, pendingObjects, menuCount, menuItemCount,
				itemCount, 0, APTR.Null);
		var stripObject = CreateNativeMenuObjectStruct(ref platform,
			serviceState, ownerRoot, publicObjects, ClassMenustrip, stripTags);
		platform.Free(stripTags, NativeMenustripTagBytes(menuCount));
		if (stripObject.IsNull)
			return FailNativeMenustripNM(ref platform, serviceState, ownerRoot,
				publicObjects, vectorBytes, menuObjects, menuItemObjects,
				itemObjects, pendingObjects, menuCount, menuItemCount,
				itemCount, 0, APTR.Null);
		FreeNativeMenuVectors(ref platform, vectorBytes, menuObjects,
			menuItemObjects, itemObjects, pendingObjects);
		return stripObject;
	}

	private static APTR MakeNativeMenuTags(ref MuiNativeClassPlatform platform,
		uint label, uint shortcut, ushort flags, uint mutualExclude,
		uint userData, APTR childVector, uint childCount)
	{
		var bytes = NativeMenuItemTagBytes(childCount);
		var tags = bytes == 0 ? APTR.Null : platform.Allocate(bytes,
			MuiHeadlessLayout.AllocationFlags);
		if (tags.IsNull) return APTR.Null;
		var index = 0u;
		if (!TryNativeTag(ref platform, tags, ref index, MenuitemTitle, label) ||
			!TryNativeTag(ref platform, tags, ref index, MenuitemShortcut, shortcut) ||
			!TryNativeTag(ref platform, tags, ref index, UserData, userData) ||
			!TryNativeTag(ref platform, tags, ref index, MenuitemExclude,
				mutualExclude) ||
			!TryNativeTag(ref platform, tags, ref index, MenuitemCheckit,
				(flags & NewMenuCheckit) != 0 ? 1u : 0u) ||
			!TryNativeTag(ref platform, tags, ref index, MenuitemChecked,
				(flags & NewMenuChecked) != 0 ? 1u : 0u) ||
			!TryNativeTag(ref platform, tags, ref index, MenuitemToggle,
				(flags & NewMenuToggle) != 0 ? 1u : 0u) ||
			!TryNativeTag(ref platform, tags, ref index, MenuitemCommandString,
				(flags & NewMenuCommandString) != 0 ? 1u : 0u) ||
			!TryNativeTag(ref platform, tags, ref index, MenuitemEnabled,
				(flags & NewMenuItemDisabled) == 0 ? 1u : 0u))
		{
			platform.Free(tags, bytes);
			return APTR.Null;
		}
		if (!TryNativeTagChildren(ref platform, tags, ref index, childVector,
			childCount) || !TryNativeTagDone(ref platform, tags, ref index))
		{
			platform.Free(tags, bytes);
			return APTR.Null;
		}
		return tags;
	}

	private static APTR MakeNativeMenuContainerTags(
		ref MuiNativeClassPlatform platform, uint label, uint userData,
		ushort flags, APTR childVector, uint childStart, uint childCount)
	{
		var bytes = NativeMenuContainerTagBytes(childCount);
		var tags = bytes == 0 ? APTR.Null : platform.Allocate(bytes,
			MuiHeadlessLayout.AllocationFlags);
		if (tags.IsNull) return APTR.Null;
		var index = 0u;
		if (!TryNativeTag(ref platform, tags, ref index, MenuTitle, label) ||
			!TryNativeTag(ref platform, tags, ref index, UserData, userData) ||
			!TryNativeTag(ref platform, tags, ref index, MenuEnabled,
				(flags & NewMenuMenuDisabled) == 0 ? 1u : 0u) ||
			!TryNativeTagChildren(ref platform, tags, ref index, childVector,
				childCount, childStart) ||
			!TryNativeTagDone(ref platform, tags, ref index))
		{
			platform.Free(tags, bytes);
			return APTR.Null;
		}
		return tags;
	}

	private static APTR MakeNativeMenustripTags(
		ref MuiNativeClassPlatform platform, APTR childVector, uint childCount)
	{
		var bytes = NativeMenustripTagBytes(childCount);
		var tags = bytes == 0 ? APTR.Null : platform.Allocate(bytes,
			MuiHeadlessLayout.AllocationFlags);
		if (tags.IsNull) return APTR.Null;
		var index = 0u;
		if (!TryNativeTagChildren(ref platform, tags, ref index, childVector,
			childCount, 0) || !TryNativeTagDone(ref platform, tags, ref index))
		{
			platform.Free(tags, bytes);
			return APTR.Null;
		}
		return tags;
	}

	private static APTR CreateNativeMenuObjectStruct(
		ref MuiNativeClassPlatform platform, APTR serviceState, APTR ownerRoot,
		APTR publicObjects, uint classKind, APTR tags)
	{
		var classId = NativeClassId(classKind);
		return classId.IsNull ? APTR.Null : MuiNativePublicObjectCore.NewObject(
			ref platform, serviceState, ownerRoot, publicObjects, classId, tags,
			classId);
	}

	private static MuiNativeMenuObjectVectorCursor NativeMenuObjectCursor(
		APTR baseAddress, uint index)
	{
		var cursor = default(MuiNativeMenuObjectVectorCursor);
		cursor.Base = baseAddress;
		cursor.Index = index;
		return cursor;
	}

	private static bool TryNativeTag<TPlatform>(ref TPlatform platform,
		APTR tags, ref uint index, uint tag, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var item = default(MuiAslTagItemRecord);
		item.Tag = tag;
		item.Data = value;
		var cursor = default(MuiAslTagItemCursor);
		cursor.Base = tags;
		cursor.Index = index;
		if (!MuiAslTagItemVectorCodec.TryWrite(ref platform, cursor, item))
			return false;
		index++;
		return true;
	}

	private static bool TryNativeTagDone<TPlatform>(ref TPlatform platform,
		APTR tags, ref uint index) where TPlatform : struct, IMuiGuestMemory =>
		TryNativeTag(ref platform, tags, ref index, MuiAslTagListCore.TagDone, 0);

	private static bool TryNativeTagChildren<TPlatform>(ref TPlatform platform,
		APTR tags, ref uint index, APTR childVector, uint childCount,
		uint childStart = 0) where TPlatform : struct, IMuiGuestMemory
	{
		if (childVector.IsNull || childStart >
			MuiNativeMenuObjectVectorCursor.MaximumEntries ||
			childCount > MuiNativeMenuObjectVectorCursor.MaximumEntries - childStart)
			return childCount == 0;
		for (var childIndex = 0u; childIndex < childCount; childIndex++)
		{
			if (!MuiNativeMenuObjectVectorCodec.TryRead(ref platform,
				NativeMenuObjectCursor(childVector, childStart + childIndex),
				out var child) || child.IsNull ||
				!TryNativeTag(ref platform, tags, ref index, FamilyChild,
					child.Raw)) return false;
		}
		return true;
	}

	private static uint NativeMenuItemTagBytes(uint childCount) =>
		NativeMenuTagBytes(9u, childCount);
	private static uint NativeMenuContainerTagBytes(uint childCount) =>
		NativeMenuTagBytes(3u, childCount);
	private static uint NativeMenustripTagBytes(uint childCount) =>
		NativeMenuTagBytes(0, childCount);

	private static uint NativeMenuTagBytes(uint baseValues, uint childCount)
	{
		if (childCount > MuiNativeMenuObjectVectorCursor.MaximumEntries ||
			baseValues > uint.MaxValue - childCount - 1) return 0;
		var valueCount = baseValues + childCount + 1; // TAG_DONE
		return valueCount > uint.MaxValue / MuiAslTagItemRecord.Size
			? 0 : valueCount * MuiAslTagItemRecord.Size;
	}

	private static void FreeNativeMenuVectors(ref MuiNativeClassPlatform platform,
		uint bytes, APTR menuObjects, APTR menuItemObjects, APTR itemObjects,
		APTR pendingObjects)
	{
		if (menuObjects.IsNotNull) platform.Free(menuObjects, bytes);
		if (menuItemObjects.IsNotNull) platform.Free(menuItemObjects, bytes);
		if (itemObjects.IsNotNull) platform.Free(itemObjects, bytes);
		if (pendingObjects.IsNotNull) platform.Free(pendingObjects, bytes);
	}

	private static APTR FailNativeMenustripNM(
		ref MuiNativeClassPlatform platform, APTR serviceState, APTR ownerRoot,
		APTR publicObjects, uint vectorBytes, APTR menuObjects,
		APTR menuItemObjects, APTR itemObjects, APTR pendingObjects,
		uint menuCount, uint menuItemCount, uint itemCount, uint pendingCount,
		APTR strip)
	{
		if (strip.IsNotNull)
			MuiNativePublicObjectCore.DisposeObject(ref platform, serviceState,
				ownerRoot, publicObjects, strip);
		DisposeNativeMenuVector(ref platform, serviceState, ownerRoot,
			publicObjects, itemObjects, itemCount);
		DisposeNativeMenuVector(ref platform, serviceState, ownerRoot,
			publicObjects, pendingObjects, pendingCount);
		DisposeNativeMenuVector(ref platform, serviceState, ownerRoot,
			publicObjects, menuItemObjects, menuItemCount);
		DisposeNativeMenuVector(ref platform, serviceState, ownerRoot,
			publicObjects, menuObjects, menuCount);
		FreeNativeMenuVectors(ref platform, vectorBytes, menuObjects,
			menuItemObjects, itemObjects, pendingObjects);
		return APTR.Null;
	}

	private static void DisposeNativeMenuVector(ref MuiNativeClassPlatform platform,
		APTR serviceState, APTR ownerRoot, APTR publicObjects, APTR vector,
		uint count)
	{
		for (var index = 0u; index < count; index++)
		{
			if (MuiNativeMenuObjectVectorCodec.TryRead(ref platform,
				NativeMenuObjectCursor(vector, index),
				out var value) && value.IsNotNull)
				MuiNativePublicObjectCore.DisposeObject(ref platform, serviceState,
					ownerRoot, publicObjects, value);
		}
	}

	// Native MUIO_MenustripNM construction mirrors the validated headless
	// NewMenu traversal, but every node is a real Intuition object. Child
	// relationships are sent through MUIM_Family_AddTail and recorded in the
	// public binding registry so the returned strip owns the complete tree.
	private static APTR MakeNativeMenustripNM(
		ref MuiNativeClassPlatform platform, APTR serviceState, APTR ownerRoot,
		APTR publicObjects, uint newMenuRaw, uint flags)
	{
		var newMenu = APTR.FromPointer(newMenuRaw);
		if (ValidateNewMenuCode(ref platform, newMenu, flags) != 0)
			return APTR.Null;

		var strip = CreateNativeMenuObject(ref platform, serviceState, ownerRoot,
			publicObjects, ClassMenustrip, APTR.Null);
		if (strip.IsNull) return APTR.Null;
		var menu = APTR.Null;
		var menuItem = APTR.Null;
		var cursor = default(MuiNewMenuCursor);
		cursor.Base = newMenu;
		cursor.Index = 0;
		for (var index = 0u; index < MuiNewMenuCursor.MaximumEntries; index++)
		{
			if (!MuiNewMenuVectorCodec.TryRead(ref platform, cursor,
				out var record))
			{
				DisposeNativeMenuTree(ref platform, serviceState, ownerRoot,
					publicObjects, strip);
				return APTR.Null;
			}
			if (!MuiNewMenuTypeRecordCodec.TryClassify(record.Type,
				out var entryKind))
			{
				DisposeNativeMenuTree(ref platform, serviceState, ownerRoot,
					publicObjects, strip);
				return APTR.Null;
			}
			if (entryKind == MuiNewMenuEntryKind.End) return strip;
			if (entryKind == MuiNewMenuEntryKind.Ignored)
			{
				if (index + 1 < MuiNewMenuCursor.MaximumEntries &&
					!MuiNewMenuVectorCodec.TryAdvance(ref cursor, 1))
				{
					DisposeNativeMenuTree(ref platform, serviceState, ownerRoot,
						publicObjects, strip);
					return APTR.Null;
				}
				continue;
			}
			if (entryKind == MuiNewMenuEntryKind.ImageItem ||
				entryKind == MuiNewMenuEntryKind.ImageSub ||
				entryKind == MuiNewMenuEntryKind.ImageUnsupported)
			{
				DisposeNativeMenuTree(ref platform, serviceState, ownerRoot,
					publicObjects, strip);
				return APTR.Null;
			}

			if (entryKind == MuiNewMenuEntryKind.Title)
			{
				var tags = platform.Allocate(TagStorage,
					MuiHeadlessLayout.AllocationFlags);
				if (tags.IsNull || !WriteNativeMenuTitleTags(ref platform, tags,
					record.Label, record.UserData, record.Flags))
				{
					if (tags.IsNotNull) platform.Free(tags, TagStorage);
					DisposeNativeMenuTree(ref platform, serviceState, ownerRoot,
						publicObjects, strip);
					return APTR.Null;
				}
				menu = CreateNativeMenuObject(ref platform, serviceState, ownerRoot,
					publicObjects, ClassMenu, tags);
				platform.Free(tags, TagStorage);
				if (menu.IsNull || !MuiNativePublicObjectCore.SetParent(ref platform,
					ownerRoot, publicObjects, strip, menu, 0))
				{
					if (menu.IsNotNull) MuiNativePublicObjectCore.DisposeObject(ref platform,
						serviceState, ownerRoot, publicObjects, menu);
					DisposeNativeMenuTree(ref platform, serviceState, ownerRoot,
						publicObjects, strip);
					return APTR.Null;
				}
				menuItem = APTR.Null;
			}
			else
			{
				if (!ResolveMenuItemStrings(ref platform, record.Label,
					record.CommandKey, flags, out var label, out var shortcut))
				{
					DisposeNativeMenuTree(ref platform, serviceState, ownerRoot,
						publicObjects, strip);
					return APTR.Null;
				}
				var tags = platform.Allocate(TagStorage,
					MuiHeadlessLayout.AllocationFlags);
				if (tags.IsNull || !WriteNativeMenuItemTags(ref platform, tags,
					label, shortcut, record.Flags, record.MutualExclude,
					record.UserData))
				{
					if (tags.IsNotNull) platform.Free(tags, TagStorage);
					DisposeNativeMenuTree(ref platform, serviceState, ownerRoot,
						publicObjects, strip);
					return APTR.Null;
				}
				var item = CreateNativeMenuObject(ref platform, serviceState, ownerRoot,
					publicObjects, ClassMenuitem, tags);
				platform.Free(tags, TagStorage);
				var parent = entryKind == MuiNewMenuEntryKind.Sub ? menuItem : menu;
				if (item.IsNull || parent.IsNull ||
					!MuiNativePublicObjectCore.SetParent(ref platform, ownerRoot,
						publicObjects, parent, item, 0))
				{
					if (item.IsNotNull) MuiNativePublicObjectCore.DisposeObject(ref platform,
						serviceState, ownerRoot, publicObjects, item);
					DisposeNativeMenuTree(ref platform, serviceState, ownerRoot,
						publicObjects, strip);
					return APTR.Null;
				}
				if (entryKind == MuiNewMenuEntryKind.Item) menuItem = item;
			}
			if (index + 1 < MuiNewMenuCursor.MaximumEntries &&
				!MuiNewMenuVectorCodec.TryAdvance(ref cursor, 1))
			{
				DisposeNativeMenuTree(ref platform, serviceState, ownerRoot,
					publicObjects, strip);
				return APTR.Null;
			}
		}
		DisposeNativeMenuTree(ref platform, serviceState, ownerRoot,
			publicObjects, strip);
		return APTR.Null;
	}

	private static APTR CreateNativeMenuObject(
		ref MuiNativeClassPlatform platform, APTR serviceState, APTR ownerRoot,
		APTR publicObjects, uint classKind, APTR tags)
	{
		var classId = NativeClassId(classKind);
		return classId.IsNull ? APTR.Null : MuiNativePublicObjectCore.NewObject(
			ref platform, serviceState, ownerRoot, publicObjects, classId, tags,
			classId);
	}

	private static bool WriteNativeMenuTitleTags<TPlatform>(ref TPlatform platform,
		APTR tags, uint label, uint userData, ushort flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		var index = 0u;
		AddTag(ref platform, tags, ref index, MenuTitle, label);
		AddTag(ref platform, tags, ref index, UserData, userData);
		AddTag(ref platform, tags, ref index, MenuEnabled,
			(flags & NewMenuMenuDisabled) == 0 ? 1u : 0u);
		WriteTagDone(ref platform, tags, index);
		return true;
	}

	private static bool WriteNativeMenuItemTags<TPlatform>(ref TPlatform platform,
		APTR tags, uint label, uint shortcut, ushort flags, uint mutualExclude,
		uint userData) where TPlatform : struct, IMuiGuestMemory
	{
		var index = 0u;
		AddTag(ref platform, tags, ref index, MenuitemTitle, label);
		AddTag(ref platform, tags, ref index, MenuitemShortcut, shortcut);
		AddTag(ref platform, tags, ref index, UserData, userData);
		AddTag(ref platform, tags, ref index, MenuitemExclude, mutualExclude);
		AddTag(ref platform, tags, ref index, MenuitemCheckit,
			(flags & NewMenuCheckit) != 0 ? 1u : 0u);
		AddTag(ref platform, tags, ref index, MenuitemChecked,
			(flags & NewMenuChecked) != 0 ? 1u : 0u);
		AddTag(ref platform, tags, ref index, MenuitemToggle,
			(flags & NewMenuToggle) != 0 ? 1u : 0u);
		AddTag(ref platform, tags, ref index, MenuitemCommandString,
			(flags & NewMenuCommandString) != 0 ? 1u : 0u);
		AddTag(ref platform, tags, ref index, MenuitemEnabled,
			(flags & NewMenuItemDisabled) == 0 ? 1u : 0u);
		WriteTagDone(ref platform, tags, index);
		return true;
	}

	private static void DisposeNativeMenuTree(
		ref MuiNativeClassPlatform platform, APTR serviceState, APTR ownerRoot,
		APTR publicObjects, APTR strip)
	{
		if (strip.IsNotNull) MuiNativePublicObjectCore.DisposeObject(ref platform,
			serviceState, ownerRoot, publicObjects, strip);
	}

	// Shared generated-TagItem seam for the button form of MUI_MakeObjectA.
	// Keeping this small typed route separate lets native qualification prove
	// the generated records without pulling the complete object factory into a
	// freestanding closure.
	internal static bool WriteButtonTagRecords<TPlatform>(ref TPlatform platform,
		APTR tags, MuiMakeObjectBuildShapeRecord shape, APTR preParse)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (shape.Type != MUIO_Button) return false;
		var index = 0u;
		AddTag(ref platform, tags, ref index, Frame, ButtonFrame);
		AddTag(ref platform, tags, ref index, Font, ButtonFont);
		AddTag(ref platform, tags, ref index, TextContents,
			shape.Parameters.First);
		AddTag(ref platform, tags, ref index, TextPreParse, preParse.Raw);
		var controlChar = ControlCharFromCString(ref platform,
			shape.Parameters.First);
		if (controlChar != 0)
			AddTag(ref platform, tags, ref index, ControlChar, controlChar);
		AddTag(ref platform, tags, ref index, InputMode, InputModeRelVerify);
		AddTag(ref platform, tags, ref index, Background, ButtonBackground);
		WriteTagDone(ref platform, tags, index);
		return true;
	}

	private static void AddTag<TPlatform>(ref TPlatform platform, APTR tags,
		ref uint index, uint tag, uint value) where TPlatform : struct, IMuiGuestMemory
	{
		var item = default(MuiAslTagItemRecord);
		item.Tag = tag;
		item.Data = value;
		var cursor = default(MuiAslTagItemCursor);
		cursor.Base = tags;
		cursor.Index = index;
		MuiAslTagItemVectorCodec.TryWrite(ref platform, cursor, item);
		index++;
	}

	private static APTR MakeMenustripNM<TPlatform>(ref TPlatform platform,
		APTR state, uint newMenuRaw, uint flags)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (ValidateNewMenuCode(ref platform,
			APTR.FromPointer(newMenuRaw), flags) != 0)
			return APTR.Null;
		var emptyTags = MuiHeadlessMemory.Allocate(ref platform, TagStorage);
		if (emptyTags.IsNull) return APTR.Null;
		WriteTagDone(ref platform, emptyTags, 0);
		var strip = CreateRegisteredObject(ref platform, state, ClassMenustrip,
			emptyTags);
		platform.Free(emptyTags, TagStorage);
		if (strip.IsNull) return APTR.Null;

		APTR menu = APTR.Null;
		APTR menuItem = APTR.Null;
		var menuCursor = default(MuiNewMenuCursor);
		menuCursor.Base = APTR.FromPointer(newMenuRaw);
		menuCursor.Index = 0;
		for (var index = 0u; index < MuiNewMenuCursor.MaximumEntries; index++)
		{
			if (!MuiNewMenuVectorCodec.TryRead(ref platform, menuCursor,
				out var menuRecord))
			{
				DisposeMenuTree(ref platform, state, strip);
				return APTR.Null;
			}
			if (!MuiNewMenuTypeRecordCodec.TryClassify(menuRecord.Type,
				out var entryKind))
			{
				DisposeMenuTree(ref platform, state, strip);
				return APTR.Null;
			}
			var label = menuRecord.Label;
			var shortcut = menuRecord.CommandKey;
			var menuFlags = menuRecord.Flags;
			var mutualExclude = menuRecord.MutualExclude;
			var userData = menuRecord.UserData;
			if (entryKind == MuiNewMenuEntryKind.End) return strip;
			if (entryKind == MuiNewMenuEntryKind.Ignored)
			{
				if (index + 1 < MuiNewMenuCursor.MaximumEntries &&
					!MuiNewMenuVectorCodec.TryAdvance(ref menuCursor, 1))
				{
					DisposeMenuTree(ref platform, state, strip);
					return APTR.Null;
				}
				continue;
			}
			// MorphOS Menuitem.mui deliberately excludes GadTools image menus.
			// Keep the rejection explicit and before any attempt to interpret the
			// label as a text pointer.
			if (entryKind == MuiNewMenuEntryKind.ImageItem ||
				entryKind == MuiNewMenuEntryKind.ImageSub ||
				entryKind == MuiNewMenuEntryKind.ImageUnsupported)
			{
				DisposeMenuTree(ref platform, state, strip);
				return APTR.Null;
			}
			if (entryKind == MuiNewMenuEntryKind.Title)
			{
				var tags = MuiHeadlessMemory.Allocate(ref platform, TagStorage);
				if (tags.IsNull)
				{
					DisposeMenuTree(ref platform, state, strip);
					return APTR.Null;
				}
				var tagIndex = 0u;
				AddTag(ref platform, tags, ref tagIndex, MenuTitle, label);
				AddTag(ref platform, tags, ref tagIndex, UserData, userData);
				AddTag(ref platform, tags, ref tagIndex, MenuEnabled,
					(menuFlags & NewMenuMenuDisabled) == 0 ? 1u : 0u);
				WriteTagDone(ref platform, tags, tagIndex);
				menu = CreateRegisteredObject(ref platform, state, ClassMenu, tags);
				platform.Free(tags, TagStorage);
				if (menu.IsNull || !MuiFamilyCore.AddTail(ref platform, state,
					strip, menu))
				{
					DisposeMenuTree(ref platform, state, strip);
					return APTR.Null;
				}
				menuItem = APTR.Null;
				if (index + 1 < MuiNewMenuCursor.MaximumEntries &&
					!MuiNewMenuVectorCodec.TryAdvance(ref menuCursor, 1))
				{
					DisposeMenuTree(ref platform, state, strip);
					return APTR.Null;
				}
				continue;
			}

			if (!ResolveMenuItemStrings(ref platform, label, shortcut, flags,
				out var effectiveLabel, out var effectiveShortcut))
			{
				DisposeMenuTree(ref platform, state, strip);
				return APTR.Null;
			}
			var itemTags = MuiHeadlessMemory.Allocate(ref platform, TagStorage);
			if (itemTags.IsNull)
			{
				DisposeMenuTree(ref platform, state, strip);
				return APTR.Null;
			}
			var itemTagIndex = 0u;
			AddTag(ref platform, itemTags, ref itemTagIndex, MenuitemTitle,
				effectiveLabel);
			AddTag(ref platform, itemTags, ref itemTagIndex, MenuitemShortcut,
				effectiveShortcut);
			AddTag(ref platform, itemTags, ref itemTagIndex, UserData, userData);
			AddTag(ref platform, itemTags, ref itemTagIndex, MenuitemExclude,
				mutualExclude);
			AddTag(ref platform, itemTags, ref itemTagIndex, MenuitemCheckit,
				(menuFlags & NewMenuCheckit) != 0 ? 1u : 0u);
			AddTag(ref platform, itemTags, ref itemTagIndex, MenuitemChecked,
				(menuFlags & NewMenuChecked) != 0 ? 1u : 0u);
			AddTag(ref platform, itemTags, ref itemTagIndex, MenuitemToggle,
				(menuFlags & NewMenuToggle) != 0 ? 1u : 0u);
			AddTag(ref platform, itemTags, ref itemTagIndex,
				MenuitemCommandString,
				(menuFlags & NewMenuCommandString) != 0 ? 1u : 0u);
			AddTag(ref platform, itemTags, ref itemTagIndex, MenuitemEnabled,
				(menuFlags & NewMenuItemDisabled) == 0 ? 1u : 0u);
			WriteTagDone(ref platform, itemTags, itemTagIndex);
			var item = CreateRegisteredObject(ref platform, state, ClassMenuitem,
				itemTags);
			platform.Free(itemTags, TagStorage);
			var parent = entryKind == MuiNewMenuEntryKind.Sub ? menuItem : menu;
			if (item.IsNull || parent.IsNull ||
				!MuiFamilyCore.AddTail(ref platform, state, parent, item))
			{
				DisposeMenuTree(ref platform, state, strip);
				return APTR.Null;
			}
			if (entryKind == MuiNewMenuEntryKind.Item) menuItem = item;
			if (index + 1 < MuiNewMenuCursor.MaximumEntries &&
				!MuiNewMenuVectorCodec.TryAdvance(ref menuCursor, 1))
			{
				DisposeMenuTree(ref platform, state, strip);
				return APTR.Null;
			}
		}

		DisposeMenuTree(ref platform, state, strip);
		return APTR.Null;
	}

	// MUI_MakeObjectA creates real menu-family objects, not merely generic
	// records with menu attributes. Attach the additive specialist sidecar at
	// construction time so callers can dispatch Menustrip/Menu/Menuitem methods
	// immediately. The helper also keeps all partial-tree rollback paths
	// ownership-correct when a later NewMenu entry fails.
	private static bool DisposeMenuTree<TPlatform>(ref TPlatform platform,
		APTR state, APTR strip)
		where TPlatform : struct, IMuiServicePlatform
	{
		return MuiMenuSpecialistCore.Valid(ref platform, state, strip)
			? MuiMenuSpecialistLifecycle.Dispose(ref platform, state, strip)
			: MuiHeadlessObjectCore.DisposeObject(ref platform, state, strip);
	}

	private static APTR CreateRegisteredObject<TPlatform>(ref TPlatform platform,
		APTR state, uint classKind, APTR tags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var className = MuiHeadlessMemory.Allocate(ref platform, ClassNameStorage);
		if (className.IsNull) return APTR.Null;
		if (!WriteClassName(ref platform, className, classKind))
		{
			platform.Free(className, ClassNameStorage);
			return APTR.Null;
		}
		var classRecord = MuiHeadlessObjectCore.FindClassByName(ref platform,
			state, className);
		var obj = classRecord.IsNull ? APTR.Null :
			MuiHeadlessObjectCore.CreateObjectA(ref platform, state, classRecord,
				tags);
		if (obj.IsNotNull && classKind >= ClassMenustrip &&
			classKind <= ClassMenuitem &&
			AttachMenuSpecialist(ref platform, state, classKind, obj).IsNull)
		{
			MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj);
			obj = APTR.Null;
		}
		platform.Free(className, ClassNameStorage);
		return obj;
	}

	private static APTR AttachMenuSpecialist<TPlatform>(ref TPlatform platform,
		APTR state, uint classKind, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var specialistClass = classKind == ClassMenustrip
			? MuiMenuSpecialistClass.Menustrip
			: classKind == ClassMenu
				? MuiMenuSpecialistClass.Menu
				: MuiMenuSpecialistClass.Menuitem;
		return MuiMenuSpecialistCore.Attach(ref platform, state, obj,
			specialistClass);
	}

	internal static uint ValidateNewMenuCode<TPlatform>(ref TPlatform platform,
		APTR newMenu, uint flags) where TPlatform : struct, IMuiGuestMemory
	{
		if (newMenu.IsNull ||
			(flags & ~MUIO_MenustripNMCommandKeyCheck) != 0) return 1;
		var haveMenu = false;
		var haveItem = false;
		var menuCursor = default(MuiNewMenuCursor);
		menuCursor.Base = newMenu;
		menuCursor.Index = 0;
		for (var index = 0u; index < MuiNewMenuCursor.MaximumEntries; index++)
		{
			if (!MuiNewMenuVectorCodec.TryRead(ref platform, menuCursor,
				out var menuRecord)) return 2;
			var entryType = menuRecord.Type;
			if (!MuiNewMenuTypeRecordCodec.TryClassify(entryType,
				out var entryKind)) return 5;
			var label = menuRecord.Label;
			var shortcut = menuRecord.CommandKey;
			if (entryKind == MuiNewMenuEntryKind.End) return 0;
			if (entryKind == MuiNewMenuEntryKind.Ignored)
			{
				if (index + 1 < MuiNewMenuCursor.MaximumEntries &&
					!MuiNewMenuVectorCodec.TryAdvance(ref menuCursor, 1)) return 2;
				continue;
			}
			if (entryKind == MuiNewMenuEntryKind.ImageItem ||
				entryKind == MuiNewMenuEntryKind.ImageSub ||
				entryKind == MuiNewMenuEntryKind.ImageUnsupported) return 3;
			if (entryKind == MuiNewMenuEntryKind.Title)
			{
				if (!ValidCString(ref platform, label)) return 4;
				haveMenu = true;
				haveItem = false;
				if (index + 1 < MuiNewMenuCursor.MaximumEntries &&
					!MuiNewMenuVectorCodec.TryAdvance(ref menuCursor, 1)) return 2;
				continue;
			}
			if (entryKind != MuiNewMenuEntryKind.Item &&
				entryKind != MuiNewMenuEntryKind.Sub) return 5;
			if (!haveMenu || entryKind == MuiNewMenuEntryKind.Sub && !haveItem)
				return 6;
			if (!ResolveMenuItemStrings(ref platform, label, shortcut, flags,
				out _, out _)) return 7;
			if (entryKind == MuiNewMenuEntryKind.Item) haveItem = true;
			if (index + 1 < MuiNewMenuCursor.MaximumEntries &&
				!MuiNewMenuVectorCodec.TryAdvance(ref menuCursor, 1)) return 2;
		}
		return 8;
	}

	private static bool ResolveMenuItemStrings<TPlatform>(ref TPlatform platform,
		uint rawLabel, uint rawShortcut, uint flags, out uint label,
		out uint shortcut) where TPlatform : struct, IMuiGuestMemory
	{
		label = rawLabel;
		shortcut = rawShortcut;
		if (rawLabel == NewMenuBarLabel)
			return rawShortcut == 0 || ValidCString(ref platform, rawShortcut);
		if (!ValidCString(ref platform, rawLabel)) return false;
		var labelAddress = APTR.FromPointer(rawLabel);
		if ((flags & MUIO_MenustripNMCommandKeyCheck) != 0 &&
			MuiMakeObjectMenuBarLabelRecordCodec.TryReadRecord(ref platform,
				labelAddress, out var labelPrefix) && labelPrefix.Terminator == 0)
		{
			if (rawLabel > uint.MaxValue - 2) return false;
			label = rawLabel + 2;
			shortcut = label;
			return ValidCString(ref platform, label);
		}
		return rawShortcut == 0 || ValidCString(ref platform, rawShortcut);
	}

	private static bool ValidMenuitemLabel<TPlatform>(ref TPlatform platform,
		uint raw) where TPlatform : struct, IMuiGuestMemory =>
		raw == 0 || raw == NewMenuBarLabel || ValidCString(ref platform, raw);

	private static void WriteTagDone<TPlatform>(ref TPlatform platform, APTR tags,
		uint index) where TPlatform : struct, IMuiGuestMemory
	{
		var item = default(MuiAslTagItemRecord);
		item.Tag = MuiAslTagListCore.TagDone;
		var cursor = default(MuiAslTagItemCursor);
		cursor.Base = tags;
		cursor.Index = index;
		MuiAslTagItemVectorCodec.TryWrite(ref platform, cursor, item);
	}

	private static bool ValidCString<TPlatform>(ref TPlatform platform, uint raw)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (raw == 0) return true;
		uint length;
		return CStringCodec.TryReadLength(ref platform, APTR.FromPointer(raw),
			MaximumCString + 1, out length);
	}

	private static bool ValidImageSpec<TPlatform>(ref TPlatform platform, uint raw)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (raw == 0 || raw <= MUIImageBuiltinMax) return true;
		return ValidCString(ref platform, raw);
	}

	internal static bool ValidEntryVector<TPlatform>(ref TPlatform platform,
		uint raw, bool requireEntry) where TPlatform : struct, IMuiGuestMemory
	{
		if (raw == 0) return !requireEntry;
		var entryCursor = default(MuiChoiceEntryCursor);
		entryCursor.Base = APTR.FromPointer(raw);
		entryCursor.Index = 0;
		for (var index = 0u; index < MuiChoiceEntryCursor.MaximumEntries;
			index++)
		{
			// The cursor bridge owns entry bounds and the named Text field;
			// validation does not expose or rebuild a caller-owned slot address.
			if (!MuiChoiceEntryVectorCodec.TryReadValue(ref platform, entryCursor,
				out var rawText)) return false;
			var text = APTR.FromPointer(rawText);
			if (text.IsNull) return index != 0 || !requireEntry;
			if (!CStringCodec.TryReadLength(ref platform, text,
				MaximumCString + 1, out _)) return false;
			if (index + 1 < MuiChoiceEntryCursor.MaximumEntries &&
				!MuiChoiceEntryVectorCodec.TryAdvance(ref entryCursor, 1))
				return false;
		}
		return false;
	}

	private static uint ControlCharFromCString<TPlatform>(ref TPlatform platform,
		uint raw) where TPlatform : struct, IMuiGuestMemory
	{
		if (raw == 0) return 0;
		var text = APTR.FromPointer(raw);
		var cursor = default(MuiMakeObjectControlCharByteCursor);
		cursor.Text = text;
		for (var index = 0u; index < MaximumCString; index++)
		{
			cursor.Index = index;
			if (!MuiMakeObjectControlCharByteCursorCodec.TryReadByte(ref platform,
				cursor, out var ch)) return 0;
			if (ch == 0) return 0;
			if (ch == (byte)'_')
			{
				cursor.Index = index + 1;
				if (!MuiMakeObjectControlCharByteCursorCodec.TryReadByte(
					ref platform, cursor, out var key) || key == 0) return 0;
				return key;
			}
		}
		return 0;
	}

	private static void ReleaseTemporary<TPlatform>(ref TPlatform platform,
		APTR className, APTR tags, APTR preParse)
		where TPlatform : struct, IMuiExecCapability
	{
		if (preParse.IsNotNull) platform.Free(preParse,
			MuiMakeObjectPreParseRecord.Size);
		if (tags.IsNotNull) platform.Free(tags, TagStorage);
		if (className.IsNotNull) platform.Free(className, ClassNameStorage);
	}
}
