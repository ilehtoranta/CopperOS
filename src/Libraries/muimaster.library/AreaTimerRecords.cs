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
		switch (field)
		{
			case MuiAreaTimerStateField.Magic:
				offset = MuiAreaTimerStateRecord.MagicOffset;
				return true;
			case MuiAreaTimerStateField.Value:
				offset = MuiAreaTimerStateRecord.ValueOffset;
				return true;
			case MuiAreaTimerStateField.Generation:
				offset = MuiAreaTimerStateRecord.GenerationOffset;
				return true;
		}
		offset = 0;
		return false;
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
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		Amiga.APTR record, MuiAreaTimerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaTimerStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		Amiga.APTR address, out MuiAreaTimerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiAreaTimerStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaTimerStateField.Magic, out value.Magic) ||
			!MuiAreaTimerStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiAreaTimerStateField.Value, out var rawValue) ||
			!MuiAreaTimerStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiAreaTimerStateField.Generation, out value.Generation))
			return false;
		value.Value = unchecked((int)rawValue);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		Amiga.APTR address, out MuiAreaTimerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaTimerStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform,
		Amiga.APTR address, MuiAreaTimerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaTimerStateRecord.Size) || value.Magic !=
			MuiAreaTimerStateRecord.Cookie ||
			!MuiAreaTimerStateAdmission.Validate(value)) return false;
		return MuiAreaTimerStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiAreaTimerStateField.Magic, value.Magic) &&
			MuiAreaTimerStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaTimerStateField.Value,
				unchecked((uint)value.Value)) &&
			MuiAreaTimerStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaTimerStateField.Generation, value.Generation);
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
		switch (field)
		{
			case MuiAreaTimerEventStateField.Magic:
				offset = MuiAreaTimerEventStateRecord.MagicOffset;
				return true;
			case MuiAreaTimerEventStateField.Armed:
				offset = MuiAreaTimerEventStateRecord.ArmedOffset;
				return true;
			case MuiAreaTimerEventStateField.MouseOver:
				offset = MuiAreaTimerEventStateRecord.MouseOverOffset;
				return true;
			case MuiAreaTimerEventStateField.DelayElapsed:
				offset = MuiAreaTimerEventStateRecord.DelayElapsedOffset;
				return true;
			case MuiAreaTimerEventStateField.LastTick:
				offset = MuiAreaTimerEventStateRecord.LastTickOffset;
				return true;
			case MuiAreaTimerEventStateField.Generation:
				offset = MuiAreaTimerEventStateRecord.GenerationOffset;
				return true;
		}
		offset = 0;
		return false;
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
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		Amiga.APTR record, MuiAreaTimerEventStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaTimerEventStateCodec
{
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
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaTimerEventStateRecord.Size) ||
			!TryReadUInt32(ref platform, address,
				MuiAreaTimerEventStateField.Magic, out var magic) ||
			!TryReadUInt32(ref platform, address,
				MuiAreaTimerEventStateField.Armed, out var armed) ||
			!TryReadUInt32(ref platform, address,
				MuiAreaTimerEventStateField.MouseOver, out var mouseOver) ||
			!TryReadUInt32(ref platform, address,
				MuiAreaTimerEventStateField.DelayElapsed, out var delayElapsed) ||
			!TryReadUInt32(ref platform, address,
				MuiAreaTimerEventStateField.LastTick, out var lastTick) ||
			!TryReadUInt32(ref platform, address,
				MuiAreaTimerEventStateField.Generation, out var generation))
			return false;
		value.Magic = magic;
		value.Armed = armed;
		value.MouseOver = mouseOver;
		value.DelayElapsed = delayElapsed;
		value.LastTick = lastTick;
		value.Generation = generation;
		return true;
	}

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
		return TryWriteUInt32(ref platform, address,
			MuiAreaTimerEventStateField.Magic, value.Magic) &&
			TryWriteUInt32(ref platform, address,
				MuiAreaTimerEventStateField.Armed, value.Armed == 0 ? 0u : 1u) &&
			TryWriteUInt32(ref platform, address,
				MuiAreaTimerEventStateField.MouseOver,
				value.MouseOver == 0 ? 0u : 1u) &&
			TryWriteUInt32(ref platform, address,
				MuiAreaTimerEventStateField.DelayElapsed,
				value.DelayElapsed == 0 ? 0u : 1u) &&
			TryWriteUInt32(ref platform, address,
				MuiAreaTimerEventStateField.LastTick, value.LastTick) &&
			TryWriteUInt32(ref platform, address,
				MuiAreaTimerEventStateField.Generation, value.Generation);
	}
}
