/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;

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
	internal const uint Cookie = 0x41544D52u; // 'ATMR'

	internal uint Magic;
	internal int Value;
	internal uint Generation;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaTimerEventStateRecord
{
	internal const uint Size = 24;
	internal const uint Cookie = 0x41544556u; // 'ATEV'

	internal uint Magic;
	internal uint Armed;
	internal uint MouseOver;
	internal uint DelayElapsed;
	internal uint LastTick;
	internal uint Generation;
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
	private static bool TryResolve(MuiAreaTimerStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaTimerStateField.Magic:
				offset = 0;
				return true;
			case MuiAreaTimerStateField.Value:
				offset = 4;
				return true;
			case MuiAreaTimerStateField.Generation:
				offset = 8;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaTimerStateFieldCursor cursor, out Amiga.APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = Amiga.APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiAreaTimerStateRecord.Size))
			return false;
		address = Amiga.APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		Amiga.APTR record, MuiAreaTimerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaTimerStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		Amiga.APTR record, MuiAreaTimerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaTimerStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaTimerStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		Amiga.APTR address, out MuiAreaTimerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaTimerStateRecord.Size) ||
			!MuiAreaTimerStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaTimerStateField.Magic, out var magic) ||
			magic != MuiAreaTimerStateRecord.Cookie ||
			!MuiAreaTimerStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaTimerStateField.Value, out var rawValue) ||
			!MuiAreaTimerStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaTimerStateField.Generation, out var generation))
			return false;
		value.Magic = magic;
		value.Value = unchecked((int)rawValue);
		value.Generation = generation;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		Amiga.APTR address, MuiAreaTimerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaTimerStateRecord.Size) || value.Magic !=
			MuiAreaTimerStateRecord.Cookie) return false;
		return MuiAreaTimerStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaTimerStateField.Magic, value.Magic) &&
			MuiAreaTimerStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaTimerStateField.Value,
			unchecked((uint)value.Value)) &&
			MuiAreaTimerStateFieldCursorCodec.TryWriteUInt32(ref platform,
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

internal static class MuiAreaTimerEventStateCodec
{
	private static bool TryResolve(MuiAreaTimerEventStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaTimerEventStateField.Magic:
				offset = 0;
				return true;
			case MuiAreaTimerEventStateField.Armed:
				offset = 4;
				return true;
			case MuiAreaTimerEventStateField.MouseOver:
				offset = 8;
				return true;
			case MuiAreaTimerEventStateField.DelayElapsed:
				offset = 12;
				return true;
			case MuiAreaTimerEventStateField.LastTick:
				offset = 16;
				return true;
			case MuiAreaTimerEventStateField.Generation:
				offset = 20;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	private static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaTimerEventStateFieldCursor cursor, out Amiga.APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = Amiga.APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiAreaTimerEventStateRecord.Size))
			return false;
		address = Amiga.APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	private static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		Amiga.APTR record, MuiAreaTimerEventStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaTimerEventStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	private static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		Amiga.APTR record, MuiAreaTimerEventStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaTimerEventStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		Amiga.APTR address, out MuiAreaTimerEventStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaTimerEventStateRecord.Size) ||
			!TryReadUInt32(ref platform, address,
				MuiAreaTimerEventStateField.Magic, out var magic) ||
			magic != MuiAreaTimerEventStateRecord.Cookie ||
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
		value.Armed = armed == 0 ? 0u : 1u;
		value.MouseOver = mouseOver == 0 ? 0u : 1u;
		value.DelayElapsed = delayElapsed == 0 ? 0u : 1u;
		value.LastTick = lastTick;
		value.Generation = generation;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		Amiga.APTR address, MuiAreaTimerEventStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaTimerEventStateRecord.Size) || value.Magic !=
			MuiAreaTimerEventStateRecord.Cookie) return false;
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
