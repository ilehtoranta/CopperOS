/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_CycleChain is a signed LONG Area policy. Keep its full bit pattern in
// a named value record; Window_SetCycleChain's object vector is a separate
// window-level method boundary.
public struct MuiAreaCycleChainStateInput
{
	public int Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaCycleChainStateRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ValueOffset = 4;
	internal const uint GenerationOffset = 8;
	internal const uint Cookie = 0x41434359u; // 'ACCY'

	internal uint Magic;
	internal int Value;
	internal uint Generation;
}

// CycleChain is an unrestricted signed LONG policy value.  Its named record
// therefore validates only identity and initialization lifetime; the complete
// signed bit pattern remains lossless through the structural and strict
// codecs.
internal static class MuiAreaCycleChainStateAdmission
{
	internal static bool Validate(MuiAreaCycleChainStateRecord value) =>
		value.Magic == MuiAreaCycleChainStateRecord.Cookie &&
		value.Generation != 0;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaCycleChainStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaCycleChainStateField : byte
{
	Magic,
	Value,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaCycleChainStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaCycleChainStateField Field;
}

internal static class MuiAreaCycleChainStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaCycleChainStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaCycleChainStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCycleChainStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaCycleChainStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCycleChainStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaCycleChainStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area CycleChain state is transferred as a named record. Numeric guest
// positions are confined to this ABI adapter; the compatibility cursor above
// remains available only to legacy callers and malformed-state diagnostics.
internal static class MuiAreaCycleChainStateRecordMemoryCodec
{
	private static bool TryResolve(MuiAreaCycleChainStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaCycleChainStateField.Magic:
				offset = MuiAreaCycleChainStateRecord.MagicOffset;
				return true;
			case MuiAreaCycleChainStateField.Value:
				offset = MuiAreaCycleChainStateRecord.ValueOffset;
				return true;
			case MuiAreaCycleChainStateField.Generation:
				offset = MuiAreaCycleChainStateRecord.GenerationOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCycleChainStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiAreaCycleChainStateRecord.Size) &&
			platform.IsMapped(address, MuiAreaCycleChainStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCycleChainStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCycleChainStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaCycleChainStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAreaCycleChainStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiAreaCycleChainStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaCycleChainStateField.Magic, out value.Magic) ||
			!MuiAreaCycleChainStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiAreaCycleChainStateField.Value, out var rawValue) ||
			!MuiAreaCycleChainStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiAreaCycleChainStateField.Generation, out value.Generation)) return false;
		value.Value = unchecked((int)rawValue);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaCycleChainStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaCycleChainStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaCycleChainStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaCycleChainStateRecord.Size) || value.Magic !=
			MuiAreaCycleChainStateRecord.Cookie ||
			!MuiAreaCycleChainStateAdmission.Validate(value)) return false;
		return MuiAreaCycleChainStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaCycleChainStateField.Magic, value.Magic) &&
			MuiAreaCycleChainStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaCycleChainStateField.Value,
				unchecked((uint)value.Value)) &&
			MuiAreaCycleChainStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaCycleChainStateField.Generation, value.Generation);
	}
}
