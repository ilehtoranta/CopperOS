/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS MUIM_UpdateConfig packet. The SDK declares the two redraw tables as
// inline arrays (64 BOOPSI object pointers followed by 64 UBYTE flags). Keep
// those tables as explicit value-type records so the public ABI is visible in
// the type system; only this codec performs the unavoidable guest-byte mapping.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiUpdateConfigObjectTable
{
	internal APTR Object00;
	internal APTR Object01;
	internal APTR Object02;
	internal APTR Object03;
	internal APTR Object04;
	internal APTR Object05;
	internal APTR Object06;
	internal APTR Object07;
	internal APTR Object08;
	internal APTR Object09;
	internal APTR Object10;
	internal APTR Object11;
	internal APTR Object12;
	internal APTR Object13;
	internal APTR Object14;
	internal APTR Object15;
	internal APTR Object16;
	internal APTR Object17;
	internal APTR Object18;
	internal APTR Object19;
	internal APTR Object20;
	internal APTR Object21;
	internal APTR Object22;
	internal APTR Object23;
	internal APTR Object24;
	internal APTR Object25;
	internal APTR Object26;
	internal APTR Object27;
	internal APTR Object28;
	internal APTR Object29;
	internal APTR Object30;
	internal APTR Object31;
	internal APTR Object32;
	internal APTR Object33;
	internal APTR Object34;
	internal APTR Object35;
	internal APTR Object36;
	internal APTR Object37;
	internal APTR Object38;
	internal APTR Object39;
	internal APTR Object40;
	internal APTR Object41;
	internal APTR Object42;
	internal APTR Object43;
	internal APTR Object44;
	internal APTR Object45;
	internal APTR Object46;
	internal APTR Object47;
	internal APTR Object48;
	internal APTR Object49;
	internal APTR Object50;
	internal APTR Object51;
	internal APTR Object52;
	internal APTR Object53;
	internal APTR Object54;
	internal APTR Object55;
	internal APTR Object56;
	internal APTR Object57;
	internal APTR Object58;
	internal APTR Object59;
	internal APTR Object60;
	internal APTR Object61;
	internal APTR Object62;
	internal APTR Object63;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiUpdateConfigFlagTable
{
	internal byte Flag00;
	internal byte Flag01;
	internal byte Flag02;
	internal byte Flag03;
	internal byte Flag04;
	internal byte Flag05;
	internal byte Flag06;
	internal byte Flag07;
	internal byte Flag08;
	internal byte Flag09;
	internal byte Flag10;
	internal byte Flag11;
	internal byte Flag12;
	internal byte Flag13;
	internal byte Flag14;
	internal byte Flag15;
	internal byte Flag16;
	internal byte Flag17;
	internal byte Flag18;
	internal byte Flag19;
	internal byte Flag20;
	internal byte Flag21;
	internal byte Flag22;
	internal byte Flag23;
	internal byte Flag24;
	internal byte Flag25;
	internal byte Flag26;
	internal byte Flag27;
	internal byte Flag28;
	internal byte Flag29;
	internal byte Flag30;
	internal byte Flag31;
	internal byte Flag32;
	internal byte Flag33;
	internal byte Flag34;
	internal byte Flag35;
	internal byte Flag36;
	internal byte Flag37;
	internal byte Flag38;
	internal byte Flag39;
	internal byte Flag40;
	internal byte Flag41;
	internal byte Flag42;
	internal byte Flag43;
	internal byte Flag44;
	internal byte Flag45;
	internal byte Flag46;
	internal byte Flag47;
	internal byte Flag48;
	internal byte Flag49;
	internal byte Flag50;
	internal byte Flag51;
	internal byte Flag52;
	internal byte Flag53;
	internal byte Flag54;
	internal byte Flag55;
	internal byte Flag56;
	internal byte Flag57;
	internal byte Flag58;
	internal byte Flag59;
	internal byte Flag60;
	internal byte Flag61;
	internal byte Flag62;
	internal byte Flag63;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiUpdateConfigMessage
{
	internal const uint Size = 332;
	internal const uint MethodIdOffset = 0;
	internal const uint CfgIdOffset = 4;
	internal const uint RedrawCountOffset = 8;
	internal uint MethodId;
	internal uint CfgId;
	internal int RedrawCount;
	internal MuiUpdateConfigObjectTable RedrawObjects;
	internal MuiUpdateConfigFlagTable RedrawFlags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiUpdateConfigMethodMessage
{
	internal const uint Size = 4;
	internal const uint MethodIdOffset = 0;
	internal uint MethodId;
}

internal enum MuiUpdateConfigPacketField : byte
{
	MethodId,
	CfgId,
	RedrawCount,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiUpdateConfigPacketFieldCursor
{
	internal APTR Message;
	internal MuiUpdateConfigPacketField Field;
}

internal static class MuiUpdateConfigPacketFieldMemoryCodec
{
	private static bool TryResolve(MuiUpdateConfigPacketField field,
		out uint offset)
	{
		if (field == MuiUpdateConfigPacketField.MethodId)
		{
			offset = MuiUpdateConfigMessage.MethodIdOffset;
			return true;
		}
		if (field == MuiUpdateConfigPacketField.CfgId)
		{
			offset = MuiUpdateConfigMessage.CfgIdOffset;
			return true;
		}
		if (field == MuiUpdateConfigPacketField.RedrawCount)
		{
			offset = MuiUpdateConfigMessage.RedrawCountOffset;
			return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiUpdateConfigPacketField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || message.IsNull ||
			message.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiUpdateConfigPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiUpdateConfigPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// UpdateConfig packet-field cursor. New code passes the message address and
// named field directly to the struct-backed memory adapter above.
internal static class MuiUpdateConfigPacketFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiUpdateConfigPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiUpdateConfigPacketFieldMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiUpdateConfigPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiUpdateConfigPacketFieldMemoryCodec.TryReadUInt32(ref platform,
			message, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiUpdateConfigPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiUpdateConfigPacketFieldMemoryCodec.TryWriteUInt32(ref platform,
			message, field, value);
}

// The redraw object table is a contiguous array of APTR slots in the public
// packet. Keep the dynamic table writer on this named slot boundary rather than
// duplicating the ULONG pointer field offset in the mutation path.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiUpdateConfigObjectSlot
{
	internal const uint Size = 4;
	internal const uint ObjectOffset = 0;
	internal APTR Object;
}

internal enum MuiUpdateConfigObjectSlotField : byte
{
	Object,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiUpdateConfigObjectSlotFieldCursor
{
	internal APTR Record;
	internal MuiUpdateConfigObjectSlotField Field;
}

internal static class MuiUpdateConfigObjectSlotFieldMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiUpdateConfigObjectSlotField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (field != MuiUpdateConfigObjectSlotField.Object ||
			record.IsNull || !platform.IsMapped(record,
				MuiUpdateConfigObjectSlot.Size)) return false;
		address = APTR.FromPointer(record.Raw +
			MuiUpdateConfigObjectSlot.ObjectOffset);
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiUpdateConfigObjectSlotField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiUpdateConfigObjectSlotField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// UpdateConfig object-slot cursor. New code uses the direct named-record
// adapter above.
internal static class MuiUpdateConfigObjectSlotFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiUpdateConfigObjectSlotFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiUpdateConfigObjectSlotFieldMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiUpdateConfigObjectSlotField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiUpdateConfigObjectSlotFieldMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiUpdateConfigObjectSlotField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiUpdateConfigObjectSlotFieldMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
}

internal static class MuiUpdateConfigObjectSlotCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiUpdateConfigObjectSlot slot)
		where TPlatform : struct, IMuiGuestMemory
	{
		slot = default;
		if (!MuiUpdateConfigObjectSlotFieldMemoryCodec.TryReadUInt32(ref platform,
			address, MuiUpdateConfigObjectSlotField.Object, out var value)) return false;
		slot.Object = APTR.FromPointer(value);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiUpdateConfigObjectSlot slot)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiUpdateConfigObjectSlotFieldMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiUpdateConfigObjectSlotField.Object, slot.Object.Raw);
	}
}

// The redraw flag table is a contiguous UBYTE slot array. Keep the dynamic
// writer on a named one-byte record so the value's wire width is explicit.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiUpdateConfigFlagSlot
{
	internal const uint Size = 1;
	internal const uint ValueOffset = 0;
	internal byte Value;
}

internal enum MuiUpdateConfigFlagSlotField : byte
{
	Value,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiUpdateConfigFlagSlotFieldCursor
{
	internal APTR Record;
	internal MuiUpdateConfigFlagSlotField Field;
}

internal static class MuiUpdateConfigFlagSlotFieldMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiUpdateConfigFlagSlotField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (field != MuiUpdateConfigFlagSlotField.Value ||
			record.IsNull || !platform.IsMapped(record,
				MuiUpdateConfigFlagSlot.Size)) return false;
		address = APTR.FromPointer(record.Raw + MuiUpdateConfigFlagSlot.ValueOffset);
		return true;
	}

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiUpdateConfigFlagSlotField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt8(address, 0);
		return true;
	}

	internal static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiUpdateConfigFlagSlotField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt8(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// UpdateConfig flag-slot cursor. New code uses the direct named-record adapter.
internal static class MuiUpdateConfigFlagSlotFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiUpdateConfigFlagSlotFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiUpdateConfigFlagSlotFieldMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiUpdateConfigFlagSlotField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiUpdateConfigFlagSlotFieldMemoryCodec.TryReadUInt8(ref platform,
			record, field, out value);

	internal static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiUpdateConfigFlagSlotField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiUpdateConfigFlagSlotFieldMemoryCodec.TryWriteUInt8(ref platform,
			record, field, value);
}

internal static class MuiUpdateConfigFlagSlotCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiUpdateConfigFlagSlot slot)
		where TPlatform : struct, IMuiGuestMemory
	{
		slot = default;
		if (!MuiUpdateConfigFlagSlotFieldMemoryCodec.TryReadUInt8(ref platform,
			address, MuiUpdateConfigFlagSlotField.Value, out slot.Value)) return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiUpdateConfigFlagSlot slot)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiUpdateConfigFlagSlotFieldMemoryCodec.TryWriteUInt8(ref platform,
			address, MuiUpdateConfigFlagSlotField.Value, slot.Value);
	}
}

// The two inline redraw tables have different wire widths. Keep each cursor
// typed and bounded so table consumers do not duplicate the packet offsets or
// accidentally read beyond the 64-entry public ABI.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiUpdateConfigObjectCursor
{
	internal const uint EntrySize = MuiUpdateConfigObjectSlot.Size;
	internal const uint MaximumEntries = 64;
	internal APTR Base;
	internal uint Index;
}

// Struct-first adapter for the inline redraw-object table. A complete named
// pointer slot and the 64-entry MorphOS bound are checked before exposure;
// the typed cursor remains a compatibility wrapper for callers that carry it.
internal static class MuiUpdateConfigObjectVectorMemoryCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (vector.IsNull || index >= MuiUpdateConfigObjectCursor.MaximumEntries ||
			index > (uint.MaxValue - vector.Raw) /
			MuiUpdateConfigObjectSlot.Size) return false;
		var offset = index * MuiUpdateConfigObjectSlot.Size;
		if (vector.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector.Raw + offset);
		return platform.IsMapped(address, MuiUpdateConfigObjectSlot.Size);
	}
}

internal static class MuiUpdateConfigObjectCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiUpdateConfigObjectCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiUpdateConfigObjectVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiUpdateConfigFlagCursor
{
	internal const uint EntrySize = MuiUpdateConfigFlagSlot.Size;
	internal const uint MaximumEntries = 64;
	internal APTR Base;
	internal uint Index;
}

// Struct-first adapter for the inline redraw-flag table. Its one-byte named
// record and 64-entry bound stay separate from the four-byte object vector.
internal static class MuiUpdateConfigFlagVectorMemoryCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (vector.IsNull || index >= MuiUpdateConfigFlagCursor.MaximumEntries ||
			index > (uint.MaxValue - vector.Raw) /
			MuiUpdateConfigFlagSlot.Size) return false;
		var offset = index * MuiUpdateConfigFlagSlot.Size;
		if (vector.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector.Raw + offset);
		return platform.IsMapped(address, MuiUpdateConfigFlagSlot.Size);
	}
}

internal static class MuiUpdateConfigFlagCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiUpdateConfigFlagCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiUpdateConfigFlagVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);
}

// Focused, struct-first bridge for the public MorphOS UpdateConfig packet.
// Preferences propagation and live BOOPSI redraw scheduling remain separate
// capabilities; this slice proves that the complete packet can cross the
// freestanding ABI without managed arrays, exceptions, or runtime allocation.
public static class MuiUpdateConfigCore
{
	public const uint Method = 0x8042B0A9u;
	public const uint PacketSize = MuiUpdateConfigMessage.Size;
	public const int MaximumRedrawObjects = 64;

	private const int ObjectTableOffset = 12;
	private const int FlagTableOffset = 268;

	private static void SetObjectField(ref MuiUpdateConfigObjectTable table,
		uint index, APTR value)
	{
		switch (index)
		{
		case 0: table.Object00 = value; return;
		case 1: table.Object01 = value; return;
		case 2: table.Object02 = value; return;
		case 3: table.Object03 = value; return;
		case 4: table.Object04 = value; return;
		case 5: table.Object05 = value; return;
		case 6: table.Object06 = value; return;
		case 7: table.Object07 = value; return;
		case 8: table.Object08 = value; return;
		case 9: table.Object09 = value; return;
		case 10: table.Object10 = value; return;
		case 11: table.Object11 = value; return;
		case 12: table.Object12 = value; return;
		case 13: table.Object13 = value; return;
		case 14: table.Object14 = value; return;
		case 15: table.Object15 = value; return;
		case 16: table.Object16 = value; return;
		case 17: table.Object17 = value; return;
		case 18: table.Object18 = value; return;
		case 19: table.Object19 = value; return;
		case 20: table.Object20 = value; return;
		case 21: table.Object21 = value; return;
		case 22: table.Object22 = value; return;
		case 23: table.Object23 = value; return;
		case 24: table.Object24 = value; return;
		case 25: table.Object25 = value; return;
		case 26: table.Object26 = value; return;
		case 27: table.Object27 = value; return;
		case 28: table.Object28 = value; return;
		case 29: table.Object29 = value; return;
		case 30: table.Object30 = value; return;
		case 31: table.Object31 = value; return;
		case 32: table.Object32 = value; return;
		case 33: table.Object33 = value; return;
		case 34: table.Object34 = value; return;
		case 35: table.Object35 = value; return;
		case 36: table.Object36 = value; return;
		case 37: table.Object37 = value; return;
		case 38: table.Object38 = value; return;
		case 39: table.Object39 = value; return;
		case 40: table.Object40 = value; return;
		case 41: table.Object41 = value; return;
		case 42: table.Object42 = value; return;
		case 43: table.Object43 = value; return;
		case 44: table.Object44 = value; return;
		case 45: table.Object45 = value; return;
		case 46: table.Object46 = value; return;
		case 47: table.Object47 = value; return;
		case 48: table.Object48 = value; return;
		case 49: table.Object49 = value; return;
		case 50: table.Object50 = value; return;
		case 51: table.Object51 = value; return;
		case 52: table.Object52 = value; return;
		case 53: table.Object53 = value; return;
		case 54: table.Object54 = value; return;
		case 55: table.Object55 = value; return;
		case 56: table.Object56 = value; return;
		case 57: table.Object57 = value; return;
		case 58: table.Object58 = value; return;
		case 59: table.Object59 = value; return;
		case 60: table.Object60 = value; return;
		case 61: table.Object61 = value; return;
		case 62: table.Object62 = value; return;
		case 63: table.Object63 = value; return;
		}
	}

	private static void SetFlagField(ref MuiUpdateConfigFlagTable table,
		uint index, byte value)
	{
		switch (index)
		{
		case 0: table.Flag00 = value; return;
		case 1: table.Flag01 = value; return;
		case 2: table.Flag02 = value; return;
		case 3: table.Flag03 = value; return;
		case 4: table.Flag04 = value; return;
		case 5: table.Flag05 = value; return;
		case 6: table.Flag06 = value; return;
		case 7: table.Flag07 = value; return;
		case 8: table.Flag08 = value; return;
		case 9: table.Flag09 = value; return;
		case 10: table.Flag10 = value; return;
		case 11: table.Flag11 = value; return;
		case 12: table.Flag12 = value; return;
		case 13: table.Flag13 = value; return;
		case 14: table.Flag14 = value; return;
		case 15: table.Flag15 = value; return;
		case 16: table.Flag16 = value; return;
		case 17: table.Flag17 = value; return;
		case 18: table.Flag18 = value; return;
		case 19: table.Flag19 = value; return;
		case 20: table.Flag20 = value; return;
		case 21: table.Flag21 = value; return;
		case 22: table.Flag22 = value; return;
		case 23: table.Flag23 = value; return;
		case 24: table.Flag24 = value; return;
		case 25: table.Flag25 = value; return;
		case 26: table.Flag26 = value; return;
		case 27: table.Flag27 = value; return;
		case 28: table.Flag28 = value; return;
		case 29: table.Flag29 = value; return;
		case 30: table.Flag30 = value; return;
		case 31: table.Flag31 = value; return;
		case 32: table.Flag32 = value; return;
		case 33: table.Flag33 = value; return;
		case 34: table.Flag34 = value; return;
		case 35: table.Flag35 = value; return;
		case 36: table.Flag36 = value; return;
		case 37: table.Flag37 = value; return;
		case 38: table.Flag38 = value; return;
		case 39: table.Flag39 = value; return;
		case 40: table.Flag40 = value; return;
		case 41: table.Flag41 = value; return;
		case 42: table.Flag42 = value; return;
		case 43: table.Flag43 = value; return;
		case 44: table.Flag44 = value; return;
		case 45: table.Flag45 = value; return;
		case 46: table.Flag46 = value; return;
		case 47: table.Flag47 = value; return;
		case 48: table.Flag48 = value; return;
		case 49: table.Flag49 = value; return;
		case 50: table.Flag50 = value; return;
		case 51: table.Flag51 = value; return;
		case 52: table.Flag52 = value; return;
		case 53: table.Flag53 = value; return;
		case 54: table.Flag54 = value; return;
		case 55: table.Flag55 = value; return;
		case 56: table.Flag56 = value; return;
		case 57: table.Flag57 = value; return;
		case 58: table.Flag58 = value; return;
		case 59: table.Flag59 = value; return;
		case 60: table.Flag60 = value; return;
		case 61: table.Flag61 = value; return;
		case 62: table.Flag62 = value; return;
		case 63: table.Flag63 = value; return;
		}
	}

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiUpdateConfigMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadMethodIdValue(ref platform, message, out var methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	// Native selector admission stays scalar so the compiler does not need to
	// materialize a temporary one-field record. The public method header remains
	// a named struct for host and dispatcher consumers.
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (message.IsNull || !platform.IsMapped(message,
			MuiUpdateConfigMethodMessage.Size)) return false;
		return MuiUpdateConfigPacketFieldMemoryCodec.TryReadUInt32(ref platform,
			message, MuiUpdateConfigPacketField.MethodId, out methodId);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR message,
		out MuiUpdateConfigMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadMethodIdValue(ref platform, message, out var methodId) ||
			methodId != Method || !platform.IsMapped(message,
			MuiUpdateConfigMessage.Size)) return false;
		if (!MuiUpdateConfigPacketFieldMemoryCodec.TryReadUInt32(ref platform,
			message, MuiUpdateConfigPacketField.CfgId, out packet.CfgId) ||
			!MuiUpdateConfigPacketFieldMemoryCodec.TryReadUInt32(ref platform,
				message, MuiUpdateConfigPacketField.RedrawCount,
				out var rawRedrawCount)) return false;
		packet.MethodId = methodId;
		packet.RedrawCount = unchecked((int)rawRedrawCount);
		if (packet.RedrawCount < 0 || packet.RedrawCount > MaximumRedrawObjects)
			return false;
		var objectVector = APTR.FromPointer(message.Raw +
			unchecked((uint)ObjectTableOffset));
		var flagVector = APTR.FromPointer(message.Raw +
			unchecked((uint)FlagTableOffset));
		for (var index = 0u; index < MuiUpdateConfigObjectCursor.MaximumEntries;
			index++)
		{
			if (!MuiUpdateConfigObjectVectorMemoryCodec.TryGetEntry(ref platform,
				objectVector, index, out var objectAddress) ||
				!MuiUpdateConfigObjectSlotCodec.TryRead(ref platform, objectAddress,
				out var objectSlot)) return false;
			SetObjectField(ref packet.RedrawObjects, index, objectSlot.Object);
			if (!MuiUpdateConfigFlagVectorMemoryCodec.TryGetEntry(ref platform,
				flagVector, index, out var flagAddress) ||
				!MuiUpdateConfigFlagSlotCodec.TryRead(ref platform, flagAddress,
				out var flagSlot)) return false;
			SetFlagField(ref packet.RedrawFlags, index, flagSlot.Value);
		}
		return true;
	}

	// Initialize a complete packet record. Redraw entries are written with the
	// bounded WriteEntry helper so callers never need to calculate wire offsets.
	public static bool WriteRecord<TPlatform>(ref TPlatform platform, APTR message,
		uint cfgId, int redrawCount)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || redrawCount < 0 ||
			redrawCount > MaximumRedrawObjects ||
			!platform.IsMapped(message, MuiUpdateConfigMessage.Size)) return false;
		platform.Clear(message, MuiUpdateConfigMessage.Size);
		return MuiUpdateConfigPacketFieldMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiUpdateConfigPacketField.MethodId, Method) &&
			MuiUpdateConfigPacketFieldMemoryCodec.TryWriteUInt32(ref platform,
				message, MuiUpdateConfigPacketField.CfgId, cfgId) &&
			MuiUpdateConfigPacketFieldMemoryCodec.TryWriteUInt32(ref platform,
				message, MuiUpdateConfigPacketField.RedrawCount,
				unchecked((uint)redrawCount));
	}

	// Set one named redraw-table entry. The packet remains valid when entries are
	// sparse; redrawCount is the caller-declared prefix length, matching the SDK
	// contract. This helper is the only dynamic table-index operation.
	public static bool WriteEntry<TPlatform>(ref TPlatform platform, APTR message,
		int index, APTR redrawObject, byte redrawFlags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || index < 0 || index >= MaximumRedrawObjects ||
			!TryReadMethodIdValue(ref platform, message, out var methodId) ||
			methodId != Method ||
			!platform.IsMapped(message, MuiUpdateConfigMessage.Size)) return false;
		var objectVector = APTR.FromPointer(message.Raw +
			unchecked((uint)ObjectTableOffset));
		if (!MuiUpdateConfigObjectVectorMemoryCodec.TryGetEntry(ref platform,
			objectVector, unchecked((uint)index), out var objectSlot)) return false;
		var record = default(MuiUpdateConfigObjectSlot);
		record.Object = redrawObject;
		if (!MuiUpdateConfigObjectSlotCodec.Write(ref platform, objectSlot,
			record)) return false;
		var flagVector = APTR.FromPointer(message.Raw +
			unchecked((uint)FlagTableOffset));
		if (!MuiUpdateConfigFlagVectorMemoryCodec.TryGetEntry(ref platform,
			flagVector, unchecked((uint)index), out var flagSlot)) return false;
		var flagRecord = default(MuiUpdateConfigFlagSlot);
		flagRecord.Value = redrawFlags;
		return MuiUpdateConfigFlagSlotCodec.Write(ref platform, flagSlot,
			flagRecord);
	}

	// A packet-only qualification seam. Returning cfgid makes the decoded
	// configuration identity observable without pretending to implement the
	// preference service or an external BOOPSI callback.
	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory =>
		TryRead(ref platform, message, out var packet) ? packet.CfgId : 0u;
}
