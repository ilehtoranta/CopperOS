/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS MUIA_Floating is documented as an Area [ISG] BOOL.  MorphOS's
// public autodoc intentionally does not define the placement effect, so this
// record owns the normalized public value without inventing layout semantics.
public struct MuiAreaFloatingStateInput
{
	public uint Enabled;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaFloatingStateRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint Cookie = 0x41464C54u; // 'AFLT'

	internal uint Magic;
	internal uint Enabled;
	internal uint Generation;
}

// MUIA_Floating is a canonical BOOL policy.  Its placement effect remains a
// MorphOS provider concern; this record admits only identity, BOOL shape, and
// publication lifetime, with live ownership checked by consumers.
internal static class MuiAreaFloatingStateAdmission
{
	internal static bool Validate(MuiAreaFloatingStateRecord value) =>
		value.Magic == MuiAreaFloatingStateRecord.Cookie &&
		value.Enabled <= 1 && value.Generation != 0;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaFloatingStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaFloatingStateField : byte
{
	Magic,
	Enabled,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaFloatingStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaFloatingStateField Field;
}

internal static class MuiAreaFloatingStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaFloatingStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaFloatingStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaFloatingStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFloatingStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaFloatingStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFloatingStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaFloatingStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area floating state is transferred as a named record. The bounded
// cursor walks the complete packed struct before selecting a field; the
// compatibility cursor above remains available only to legacy callers and
// malformed-state diagnostics.
internal static class MuiAreaFloatingStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiAreaFloatingStateField field,
		out uint index)
	{
		if (field == MuiAreaFloatingStateField.Magic) index = 0;
		else if (field == MuiAreaFloatingStateField.Enabled) index = 1;
		else if (field == MuiAreaFloatingStateField.Generation) index = 2;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFloatingStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaFloatingStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaFloatingStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiAreaFloatingStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiAreaFloatingStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiAreaFloatingStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFloatingStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaFloatingStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaFloatingStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaFloatingStateField.Enabled)
			value = state.Enabled;
		else if (field == MuiAreaFloatingStateField.Generation)
			value = state.Generation;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFloatingStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaFloatingStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaFloatingStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaFloatingStateField.Enabled)
			state.Enabled = value;
		else if (field == MuiAreaFloatingStateField.Generation)
			state.Generation = value;
		else return false;
		return MuiAreaFloatingStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiAreaFloatingStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaFloatingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaFloatingStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Enabled) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Generation)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaFloatingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaFloatingStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Enabled) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Generation) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAreaFloatingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaFloatingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaFloatingStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaFloatingStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiAreaFloatingStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}

internal static class MuiAreaFloatingCore
{
	internal const uint StateKey = 0x7F070047u;

	internal static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaFloatingStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var enabled = 0u;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			MuiCommonControlCore.Floating, out var raw))
			enabled = raw == 0 ? 0u : 1u;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj, StateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			StateKey);
		MuiAreaFloatingStateRecord record;
		if (block.IsNotNull || length != 0)
		{
			// A present block is authoritative typed state.  Do not repair a
			// malformed BOOL or generation from the legacy raw attribute.
			if (length != unchecked((int)MuiAreaFloatingStateRecord.Size) ||
				!MuiAreaFloatingStateRecordCodec.TryReadStructural(ref platform, block,
					out record) || !MuiAreaFloatingStateAdmission.ValidateLive(ref platform,
					state, obj, record)) return false;
			if (record.Enabled != enabled)
			{
				record.Enabled = enabled;
				record.Generation = record.Generation == uint.MaxValue ? 1u :
					record.Generation + 1u;
				if (!MuiAreaFloatingStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			value.Enabled = record.Enabled;
			return true;
		}
		if (!WriteState(ref platform, state, obj, enabled, 1)) return false;
		value.Enabled = enabled;
		return true;
	}

	internal static bool WriteState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint enabled, uint generation)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaFloatingStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaFloatingStateRecord.Size);
		var record = default(MuiAreaFloatingStateRecord);
		record.Magic = MuiAreaFloatingStateRecord.Cookie;
		record.Enabled = enabled == 0 ? 0u : 1u;
		record.Generation = generation == 0 ? 1u : generation;
		var written = MuiAreaFloatingStateAdmission.ValidateLive(ref platform, state,
			obj, record) && MuiAreaFloatingStateRecordCodec.Write(ref platform,
			scratch, record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, unchecked((int)MuiAreaFloatingStateRecord.Size));
		platform.Clear(scratch, MuiAreaFloatingStateRecord.Size);
		platform.Free(scratch, MuiAreaFloatingStateRecord.Size);
		return stored;
	}
}

public static class MuiAreaFloatingPacketCore
{
	public static bool Set<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint enabled)
		where TPlatform : struct, IMuiLayoutPlatform =>
		MuiCommonControlCore.SetControlAttribute(ref platform, state, obj,
			MuiCommonControlCore.Floating, enabled, true);

	public static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaFloatingStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaFloatingCore.TryReadState(ref platform, state, obj, out value);
}
