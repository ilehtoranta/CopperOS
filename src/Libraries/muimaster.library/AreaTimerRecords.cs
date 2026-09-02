/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_Timer is a getter-only signed LONG event counter. The input/provider
// layer owns the repeat timing policy and publishes the current value through
// this named record.
public struct MuiAreaTimerStateInput
{
	public int Value;
	public uint Armed;
	public uint MouseOver;
	public uint DelayElapsed;
	public uint LastTick;
}

// The provider supplies these transitions from its native input/event loop.
// The core never creates a timer task or reads a managed clock. IntuiTick is a
// discrete event; Tick is only an event identity used to reject duplicate
// delivery of the same tick.
public enum MuiAreaTimerEventKind : byte
{
	RelVerifyPress,
	IntuiTick,
	PointerEnter,
	PointerLeave,
	RelVerifyRelease,
}

public struct MuiAreaTimerEventInput
{
	public MuiAreaTimerEventKind Kind;
	public uint Tick;
	public uint PointerOver;
	public uint DelayElapsed;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaTimerStateRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ValueOffset = 4;
	internal const uint GenerationOffset = 8;
	internal const uint Cookie = 0x41544D52u; // 'ATMR'

	internal uint Magic;
	internal int Value;
	internal uint Generation;
}

// MUIA_Timer is a signed LONG event counter.  The value is lossless; only the
// record identity and publication lifetime are admitted here.  Live consumers
// additionally verify that the Dataspace record belongs to the current object.
internal static class MuiAreaTimerStateAdmission
{
	internal static bool Validate(MuiAreaTimerStateRecord value) =>
		value.Magic == MuiAreaTimerStateRecord.Cookie && value.Generation != 0;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaTimerStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaTimerEventStateRecord
{
	internal const uint Size = 24;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ArmedOffset = 4;
	internal const uint MouseOverOffset = 8;
	internal const uint DelayElapsedOffset = 12;
	internal const uint LastTickOffset = 16;
	internal const uint GenerationOffset = 20;
	internal const uint Cookie = 0x41544556u; // 'ATEV'

	internal uint Magic;
	internal uint Armed;
	internal uint MouseOver;
	internal uint DelayElapsed;
	internal uint LastTick;
	internal uint Generation;
}

// Timer input flags are canonical BOOL values.  LastTick remains an opaque
// IntuiTick identity and is therefore retained losslessly as a ULONG.
internal static class MuiAreaTimerEventStateAdmission
{
	internal static bool Validate(MuiAreaTimerEventStateRecord value) =>
		value.Magic == MuiAreaTimerEventStateRecord.Cookie &&
		value.Armed <= 1 && value.MouseOver <= 1 &&
		value.DelayElapsed <= 1 && value.Generation != 0;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaTimerEventStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaTimerStateField : byte
{
	Magic,
	Value,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaTimerStateFieldCursor
{
	internal Amiga.APTR Record;
	internal MuiAreaTimerStateField Field;
}

internal static class MuiAreaTimerStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaTimerStateFieldCursor cursor, out Amiga.APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaTimerStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		Amiga.APTR record, MuiAreaTimerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaTimerStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		Amiga.APTR record, MuiAreaTimerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaTimerStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area timer state is transferred as a named record. Numeric guest
// positions are confined to this ABI adapter; the compatibility cursor above
// remains available only to legacy callers and malformed-state diagnostics.
internal static class MuiAreaTimerStateRecordMemoryCodec
{
	private static bool TryResolve(MuiAreaTimerStateField field,
		out uint offset)
	{
		if (field == MuiAreaTimerStateField.Magic)
			offset = MuiAreaTimerStateRecord.MagicOffset;
		else if (field == MuiAreaTimerStateField.Value)
			offset = MuiAreaTimerStateRecord.ValueOffset;
		else if (field == MuiAreaTimerStateField.Generation)
			offset = MuiAreaTimerStateRecord.GenerationOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		Amiga.APTR record, MuiAreaTimerStateField field, out Amiga.APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = Amiga.APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = Amiga.APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiAreaTimerStateRecord.Size) &&
			platform.IsMapped(address, MuiAreaTimerStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		Amiga.APTR record, MuiAreaTimerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaTimerStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaTimerStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaTimerStateField.Value)
			value = unchecked((uint)state.Value);
		else if (field == MuiAreaTimerStateField.Generation)
			value = state.Generation;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		Amiga.APTR record, MuiAreaTimerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaTimerStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaTimerStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaTimerStateField.Value)
			state.Value = unchecked((int)value);
		else if (field == MuiAreaTimerStateField.Generation)
			state.Generation = value;
		else return false;
		return MuiAreaTimerStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiAreaTimerStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		Amiga.APTR address, out MuiAreaTimerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaTimerStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawValue) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Generation)) return false;
		value.Value = unchecked((int)rawValue);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		Amiga.APTR address, MuiAreaTimerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaTimerStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Value)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Generation) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		Amiga.APTR address, out MuiAreaTimerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		Amiga.APTR address, out MuiAreaTimerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaTimerStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform,
		Amiga.APTR address, MuiAreaTimerStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiAreaTimerStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}

internal enum MuiAreaTimerEventStateField : byte
{
	Magic,
	Armed,
	MouseOver,
	DelayElapsed,
	LastTick,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaTimerEventStateFieldCursor
{
	internal Amiga.APTR Record;
	internal MuiAreaTimerEventStateField Field;
}

// Fixed Area timer event state is transferred as a named record. Numeric
// guest positions are confined to this ABI adapter; callers operate on the
// semantic field enum instead of carrying private record offsets.
internal static class MuiAreaTimerEventStateRecordMemoryCodec
{
	private static bool TryResolve(MuiAreaTimerEventStateField field,
		out uint offset)
	{
		if (field == MuiAreaTimerEventStateField.Magic)
			offset = MuiAreaTimerEventStateRecord.MagicOffset;
		else if (field == MuiAreaTimerEventStateField.Armed)
			offset = MuiAreaTimerEventStateRecord.ArmedOffset;
		else if (field == MuiAreaTimerEventStateField.MouseOver)
			offset = MuiAreaTimerEventStateRecord.MouseOverOffset;
		else if (field == MuiAreaTimerEventStateField.DelayElapsed)
			offset = MuiAreaTimerEventStateRecord.DelayElapsedOffset;
		else if (field == MuiAreaTimerEventStateField.LastTick)
			offset = MuiAreaTimerEventStateRecord.LastTickOffset;
		else if (field == MuiAreaTimerEventStateField.Generation)
			offset = MuiAreaTimerEventStateRecord.GenerationOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		Amiga.APTR record, MuiAreaTimerEventStateField field,
		out Amiga.APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = Amiga.APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = Amiga.APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiAreaTimerEventStateRecord.Size) &&
			platform.IsMapped(address, MuiAreaTimerEventStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		Amiga.APTR record, MuiAreaTimerEventStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaTimerEventStateCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiAreaTimerEventStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaTimerEventStateField.Armed)
			value = state.Armed;
		else if (field == MuiAreaTimerEventStateField.MouseOver)
			value = state.MouseOver;
		else if (field == MuiAreaTimerEventStateField.DelayElapsed)
			value = state.DelayElapsed;
		else if (field == MuiAreaTimerEventStateField.LastTick)
			value = state.LastTick;
		else if (field == MuiAreaTimerEventStateField.Generation)
			value = state.Generation;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		Amiga.APTR record, MuiAreaTimerEventStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaTimerEventStateCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiAreaTimerEventStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaTimerEventStateField.Armed)
			state.Armed = value;
		else if (field == MuiAreaTimerEventStateField.MouseOver)
			state.MouseOver = value;
		else if (field == MuiAreaTimerEventStateField.DelayElapsed)
			state.DelayElapsed = value;
		else if (field == MuiAreaTimerEventStateField.LastTick)
			state.LastTick = value;
		else if (field == MuiAreaTimerEventStateField.Generation)
			state.Generation = value;
		else return false;
		return MuiAreaTimerEventStateCodec.WriteRecord(ref platform, record, state);
	}
}

internal static class MuiAreaTimerEventStateCodec
{
	// Declaration-order timer event record: magic, canonical BOOL flags,
	// opaque tick identity, and generation. All fields are exchanged through
	// the bounded named cursor; the enum adapter remains diagnostic only.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		Amiga.APTR address, out MuiAreaTimerEventStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaTimerEventStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Armed) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MouseOver) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.DelayElapsed) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.LastTick) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Generation) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		Amiga.APTR address, MuiAreaTimerEventStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaTimerEventStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Armed) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.MouseOver) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.DelayElapsed) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.LastTick) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Generation) && MuiGuestStructCursor.IsComplete(cursor);

	private static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		Amiga.APTR record, MuiAreaTimerEventStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaTimerEventStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		Amiga.APTR record, MuiAreaTimerEventStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaTimerEventStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		Amiga.APTR address, out MuiAreaTimerEventStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		Amiga.APTR address, out MuiAreaTimerEventStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaTimerEventStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform,
		Amiga.APTR address, MuiAreaTimerEventStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaTimerEventStateRecord.Size) || value.Magic !=
			MuiAreaTimerEventStateRecord.Cookie ||
			!MuiAreaTimerEventStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}
