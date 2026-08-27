/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_ShortHelp is an opaque OBString pointer. Keep the public seam as a
// value type so a future bubble service can consume the guest object without
// exposing a managed string or a private Area offset.
public struct MuiAreaShortHelpStateInput
{
	public APTR Text;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaShortHelpStateRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint TextOffset = 4;
	internal const uint GenerationOffset = 8;
	internal const uint Cookie = 0x41534850u; // 'ASHP'

	internal uint Magic;
	internal APTR Text;
	internal uint Generation;
}

// ShortHelp is an opaque caller-owned OBString pointer. Preserve it
// losslessly, while admitting record identity and publication lifetime; live
// consumers additionally verify that the record belongs to the current object.
internal static class MuiAreaShortHelpStateAdmission
{
	internal static bool Validate(MuiAreaShortHelpStateRecord value) =>
		value.Magic == MuiAreaShortHelpStateRecord.Cookie &&
		value.Generation != 0;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaShortHelpStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaShortHelpStateField : byte
{
	Magic,
	Text,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaShortHelpStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaShortHelpStateField Field;
}

internal static class MuiAreaShortHelpStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaShortHelpStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaShortHelpStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaShortHelpStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaShortHelpStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaShortHelpStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaShortHelpStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area ShortHelp state is transferred as a named record. Numeric guest
// positions are confined to this ABI adapter; the compatibility cursor above
// remains available only to legacy callers and malformed-state diagnostics.
internal static class MuiAreaShortHelpStateRecordMemoryCodec
{
	private static bool TryResolve(MuiAreaShortHelpStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaShortHelpStateField.Magic:
				offset = MuiAreaShortHelpStateRecord.MagicOffset;
				return true;
			case MuiAreaShortHelpStateField.Text:
				offset = MuiAreaShortHelpStateRecord.TextOffset;
				return true;
			case MuiAreaShortHelpStateField.Generation:
				offset = MuiAreaShortHelpStateRecord.GenerationOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaShortHelpStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiAreaShortHelpStateRecord.Size) &&
			platform.IsMapped(address, MuiAreaShortHelpStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaShortHelpStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaShortHelpStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaShortHelpStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAreaShortHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiAreaShortHelpStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaShortHelpStateField.Magic, out value.Magic) ||
			!MuiAreaShortHelpStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiAreaShortHelpStateField.Text, out var text) ||
			!MuiAreaShortHelpStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiAreaShortHelpStateField.Generation, out value.Generation)) return false;
		value.Text = APTR.FromPointer(text);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaShortHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaShortHelpStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaShortHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaShortHelpStateRecord.Size) || value.Magic !=
			MuiAreaShortHelpStateRecord.Cookie ||
			!MuiAreaShortHelpStateAdmission.Validate(value)) return false;
		return MuiAreaShortHelpStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaShortHelpStateField.Magic, value.Magic) &&
			MuiAreaShortHelpStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaShortHelpStateField.Text, value.Text.Raw) &&
			MuiAreaShortHelpStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaShortHelpStateField.Generation, value.Generation);
	}
}
