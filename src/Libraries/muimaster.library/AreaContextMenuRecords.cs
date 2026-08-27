/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_ContextMenu and MUIA_ContextMenuTrigger are opaque MUI object
// relationships.  The pointers remain caller-owned; this record only keeps
// the public Area projection and the last trigger published by the default
// ContextMenuChoice path.
public struct MuiAreaContextMenuStateInput
{
	public APTR MenuStrip;
	public APTR Trigger;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaContextMenuStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint MenuStripOffset = 4;
	internal const uint TriggerOffset = 8;
	internal const uint GenerationOffset = 12;
	internal const uint Cookie = 0x41434D50u; // 'ACMP'

	internal uint Magic;
	internal APTR MenuStrip;
	internal APTR Trigger;
	internal uint Generation;
}

// Menu-strip and trigger pointers are opaque caller-owned MUI objects.  The
// record's integrity boundary therefore consists of its cookie and non-zero
// generation, plus live-owner validation at the consumer seam; pointer bits
// are retained losslessly rather than guessed or dereferenced here.
internal static class MuiAreaContextMenuStateAdmission
{
	internal static bool Validate(MuiAreaContextMenuStateRecord value) =>
		value.Magic == MuiAreaContextMenuStateRecord.Cookie &&
		value.Generation != 0;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaContextMenuStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaContextMenuStateField : byte
{
	Magic,
	MenuStrip,
	Trigger,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaContextMenuStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaContextMenuStateField Field;
}

internal static class MuiAreaContextMenuStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaContextMenuStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaContextMenuStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaContextMenuStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaContextMenuStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaContextMenuStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaContextMenuStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area ContextMenu state is transferred as a named record. Numeric
// guest positions are confined to this ABI adapter; the compatibility cursor
// above remains available only to legacy callers and malformed-state
// diagnostics.
internal static class MuiAreaContextMenuStateRecordMemoryCodec
{
	private static bool TryResolve(MuiAreaContextMenuStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaContextMenuStateField.Magic:
				offset = MuiAreaContextMenuStateRecord.MagicOffset;
				return true;
			case MuiAreaContextMenuStateField.MenuStrip:
				offset = MuiAreaContextMenuStateRecord.MenuStripOffset;
				return true;
			case MuiAreaContextMenuStateField.Trigger:
				offset = MuiAreaContextMenuStateRecord.TriggerOffset;
				return true;
			case MuiAreaContextMenuStateField.Generation:
				offset = MuiAreaContextMenuStateRecord.GenerationOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaContextMenuStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiAreaContextMenuStateRecord.Size) &&
			platform.IsMapped(address, MuiAreaContextMenuStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaContextMenuStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaContextMenuStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaContextMenuStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAreaContextMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiAreaContextMenuStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaContextMenuStateField.Magic, out value.Magic) ||
			!MuiAreaContextMenuStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiAreaContextMenuStateField.MenuStrip, out var menuStrip) ||
			!MuiAreaContextMenuStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiAreaContextMenuStateField.Trigger, out var trigger) ||
			!MuiAreaContextMenuStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiAreaContextMenuStateField.Generation, out value.Generation))
			return false;
		value.MenuStrip = APTR.FromPointer(menuStrip);
		value.Trigger = APTR.FromPointer(trigger);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaContextMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaContextMenuStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaContextMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaContextMenuStateRecord.Size) || value.Magic !=
			MuiAreaContextMenuStateRecord.Cookie ||
			!MuiAreaContextMenuStateAdmission.Validate(value)) return false;
		return MuiAreaContextMenuStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaContextMenuStateField.Magic, value.Magic) &&
			MuiAreaContextMenuStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaContextMenuStateField.MenuStrip, value.MenuStrip.Raw) &&
			MuiAreaContextMenuStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaContextMenuStateField.Trigger, value.Trigger.Raw) &&
			MuiAreaContextMenuStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaContextMenuStateField.Generation, value.Generation);
	}
}
