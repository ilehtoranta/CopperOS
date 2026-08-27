/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_DoubleBuffer is a BOOL Area policy. Keep the public value as a named
// struct so callers do not need to know anything about the guest Dataspace
// record used by the headless implementation.
public struct MuiAreaDoubleBufferStateInput
{
	public uint Enabled;
}

// The private state record is deliberately separate from the render-policy
// record: enabling double buffering is an Area capability, while FillArea,
// Background, Frame, and Font describe drawing policy. The generation field
// lets diagnostics distinguish a next setting from a stale raw compatibility
// slot without exposing a positional widget layout.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDoubleBufferStateRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint EnabledOffset = 4;
	internal const uint GenerationOffset = 8;
	internal const uint Cookie = 0x41444252u; // 'ADBR'

	internal uint Magic;
	internal uint Enabled;
	internal uint Generation;
}

// MUIA_DoubleBuffer is a canonical BOOL policy.  The record is admitted only
// when its identity, normalized BOOL, and publication generation are valid;
// live consumers additionally require ownership by the current object.
internal static class MuiAreaDoubleBufferStateAdmission
{
	internal static bool Validate(MuiAreaDoubleBufferStateRecord value) =>
		value.Magic == MuiAreaDoubleBufferStateRecord.Cookie &&
		value.Enabled <= 1 && value.Generation != 0;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaDoubleBufferStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaDoubleBufferStateField : byte
{
	Magic,
	Enabled,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDoubleBufferStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaDoubleBufferStateField Field;
}

internal static class MuiAreaDoubleBufferStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaDoubleBufferStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDoubleBufferStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleBufferStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDoubleBufferStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleBufferStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDoubleBufferStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area double-buffer state is transferred as a named record. Numeric
// guest positions are confined to this ABI adapter; the compatibility cursor
// above remains available only to legacy callers and malformed-state
// diagnostics.
internal static class MuiAreaDoubleBufferStateRecordMemoryCodec
{
	private static bool TryResolve(MuiAreaDoubleBufferStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaDoubleBufferStateField.Magic:
				offset = MuiAreaDoubleBufferStateRecord.MagicOffset;
				return true;
			case MuiAreaDoubleBufferStateField.Enabled:
				offset = MuiAreaDoubleBufferStateRecord.EnabledOffset;
				return true;
			case MuiAreaDoubleBufferStateField.Generation:
				offset = MuiAreaDoubleBufferStateRecord.GenerationOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleBufferStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiAreaDoubleBufferStateRecord.Size) &&
			platform.IsMapped(address, MuiAreaDoubleBufferStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleBufferStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleBufferStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaDoubleBufferStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAreaDoubleBufferStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiAreaDoubleBufferStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaDoubleBufferStateField.Magic,
			out value.Magic) ||
			!MuiAreaDoubleBufferStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiAreaDoubleBufferStateField.Enabled,
				out value.Enabled) ||
			!MuiAreaDoubleBufferStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiAreaDoubleBufferStateField.Generation,
				out value.Generation)) return false;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaDoubleBufferStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaDoubleBufferStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaDoubleBufferStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaDoubleBufferStateRecord.Size) || value.Magic !=
			MuiAreaDoubleBufferStateRecord.Cookie ||
			!MuiAreaDoubleBufferStateAdmission.Validate(value)) return false;
		return MuiAreaDoubleBufferStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaDoubleBufferStateField.Magic,
			value.Magic) &&
			MuiAreaDoubleBufferStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaDoubleBufferStateField.Enabled, value.Enabled) &&
			MuiAreaDoubleBufferStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaDoubleBufferStateField.Generation,
				value.Generation);
	}
}
