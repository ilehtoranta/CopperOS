/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the getter-only MUIA_Gadget_Gadget relationship.
// The pointer is caller-owned guest state; no managed gadget wrapper is
// created or retained.
public struct MuiGadgetGadgetState
{
	public APTR Gadget;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGadgetGadgetStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint GadgetOffset = 4;
	internal const uint Cookie = 0x4D474744u; // 'MGGD'

	internal uint Magic;
	internal APTR Gadget;
}

// MUIA_Gadget_Gadget is a getter-only Intuition-gadget pointer. The pointer
// remains caller/platform-owned; admission only requires a mapped guest byte
// and a live owning MUI object, never a managed gadget wrapper or object-table
// relationship that the core cannot prove for native Intuition state.
internal static class MuiGadgetGadgetStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiGadgetGadgetStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiGadgetGadgetStateRecord.Cookie &&
		(value.Gadget.IsNull || platform.IsMapped(value.Gadget, 1));

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiGadgetGadgetStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiGadgetGadgetStateField : byte
{
	Magic,
	Gadget,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGadgetGadgetStateFieldCursor
{
	internal APTR Record;
	internal MuiGadgetGadgetStateField Field;
}

internal static class MuiGadgetGadgetStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGadgetGadgetStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGadgetGadgetStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGadgetGadgetStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGadgetGadgetStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGadgetGadgetStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGadgetGadgetStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. Gadget consumers use the named record;
// this bounded adapter is the only layer that translates its fixed guest
// layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiGadgetGadgetStateRecordMemoryCodec
{
	private static bool TryResolve(MuiGadgetGadgetStateField field,
		out uint offset)
	{
		if (field == MuiGadgetGadgetStateField.Magic)
			offset = MuiGadgetGadgetStateRecord.MagicOffset;
		else if (field == MuiGadgetGadgetStateField.Gadget)
			offset = MuiGadgetGadgetStateRecord.GadgetOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiGadgetGadgetStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiGadgetGadgetStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiGadgetGadgetStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGadgetGadgetStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiGadgetGadgetStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiGadgetGadgetStateField.Magic)
			value = state.Magic;
		else if (field == MuiGadgetGadgetStateField.Gadget)
			value = state.Gadget.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGadgetGadgetStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGadgetGadgetStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiGadgetGadgetStateField.Magic)
			state.Magic = value;
		else if (field == MuiGadgetGadgetStateField.Gadget)
			state.Gadget = APTR.FromPointer(value);
		else return false;
		return MuiGadgetGadgetStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiGadgetGadgetStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGadgetGadgetStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGadgetGadgetStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var gadget)) return false;
		value.Gadget = APTR.FromPointer(gadget);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGadgetGadgetStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGadgetGadgetStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Gadget.Raw) &&
			MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGadgetGadgetStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGadgetGadgetStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiGadgetGadgetStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGadgetGadgetStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGadgetGadgetStateAdmission.Validate(ref platform, value) &&
		WriteRecord(ref platform, address, value);
}
