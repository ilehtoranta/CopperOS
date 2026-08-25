/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The MorphOS Area docs define MUIA_Font and MUIA_CustomFont as competing
// choices: whichever setter runs last overrides the other. Keep that choice
// as a named guest record instead of inferring it from a private object layout.
public enum MuiAreaFontSelectionKind : uint
{
	None = 0,
	Font = 1,
	CustomFont = 2,
}

public struct MuiAreaFontSelectionState
{
	public MuiAreaFontSelectionKind Active;
	public APTR Source;
	public uint Generation;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaFontSelectionStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x4146534Cu; // 'AFSL'

	internal uint Magic;
	internal uint Active;
	internal APTR Source;
	internal uint Generation;
}

internal enum MuiAreaFontSelectionStateField : byte
{
	Magic,
	Active,
	Source,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaFontSelectionStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaFontSelectionStateField Field;
}

internal static class MuiAreaFontSelectionStateFieldCursorCodec
{
	private static bool TryResolve(MuiAreaFontSelectionStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiAreaFontSelectionStateField.Magic => 0,
			MuiAreaFontSelectionStateField.Active => 4,
			MuiAreaFontSelectionStateField.Source => 8,
			MuiAreaFontSelectionStateField.Generation => 12,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaFontSelectionStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiAreaFontSelectionStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFontSelectionStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaFontSelectionStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFontSelectionStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaFontSelectionStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaFontSelectionStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaFontSelectionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaFontSelectionStateRecord.Size) ||
			!MuiAreaFontSelectionStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaFontSelectionStateField.Magic, out var magic) ||
			magic != MuiAreaFontSelectionStateRecord.Cookie ||
			!MuiAreaFontSelectionStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaFontSelectionStateField.Active, out value.Active) ||
			value.Active > (uint)MuiAreaFontSelectionKind.CustomFont ||
			!MuiAreaFontSelectionStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaFontSelectionStateField.Source, out var source) ||
			!MuiAreaFontSelectionStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaFontSelectionStateField.Generation,
				out value.Generation)) return false;
		value.Magic = magic;
		value.Source = APTR.FromPointer(source);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaFontSelectionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaFontSelectionStateRecord.Size) || value.Magic !=
			MuiAreaFontSelectionStateRecord.Cookie || value.Active >
			(uint)MuiAreaFontSelectionKind.CustomFont) return false;
		return MuiAreaFontSelectionStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaFontSelectionStateField.Magic, value.Magic) &&
			MuiAreaFontSelectionStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaFontSelectionStateField.Active, value.Active) &&
			MuiAreaFontSelectionStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaFontSelectionStateField.Source, value.Source.Raw) &&
			MuiAreaFontSelectionStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaFontSelectionStateField.Generation,
				value.Generation);
	}
}
