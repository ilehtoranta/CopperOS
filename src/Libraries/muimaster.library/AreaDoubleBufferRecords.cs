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
	internal const uint Cookie = 0x41444252u; // 'ADBR'

	internal uint Magic;
	internal uint Enabled;
	internal uint Generation;
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
	private static bool TryResolve(MuiAreaDoubleBufferStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaDoubleBufferStateField.Magic:
				offset = 0;
				return true;
			case MuiAreaDoubleBufferStateField.Enabled:
				offset = 4;
				return true;
			case MuiAreaDoubleBufferStateField.Generation:
				offset = 8;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaDoubleBufferStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiAreaDoubleBufferStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleBufferStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaDoubleBufferStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleBufferStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaDoubleBufferStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaDoubleBufferStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaDoubleBufferStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaDoubleBufferStateRecord.Size) ||
			!MuiAreaDoubleBufferStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaDoubleBufferStateField.Magic, out var magic) ||
			magic != MuiAreaDoubleBufferStateRecord.Cookie ||
			!MuiAreaDoubleBufferStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaDoubleBufferStateField.Enabled, out value.Enabled) ||
			!MuiAreaDoubleBufferStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaDoubleBufferStateField.Generation,
				out value.Generation)) return false;
		value.Magic = magic;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaDoubleBufferStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaDoubleBufferStateRecord.Size) || value.Magic !=
			MuiAreaDoubleBufferStateRecord.Cookie) return false;
		return MuiAreaDoubleBufferStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaDoubleBufferStateField.Magic, value.Magic) &&
			MuiAreaDoubleBufferStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaDoubleBufferStateField.Enabled, value.Enabled) &&
			MuiAreaDoubleBufferStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaDoubleBufferStateField.Generation,
				value.Generation);
	}
}
