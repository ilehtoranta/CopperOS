/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared Text presentation and input state. Text contents and preparse remain
// in their dedicated pointer records; this record carries the scalar policy
// consumed by sizing, keyboard activation, shortening, and drawing.
public struct MuiTextPresentationState
{
	public uint SetMin;
	public uint SetMax;
	public uint SetVMax;
	public uint ControlChar;
	public uint Marking;
	public uint Shorten;
	public uint HiChar;
	public uint HiCharPresent;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextPresentationStateRecord
{
	internal const uint Size = 36;
	internal const uint Cookie = 0x4D545850u; // 'MTXP'

	internal uint Magic;
	internal uint SetMin;
	internal uint SetMax;
	internal uint SetVMax;
	internal uint ControlChar;
	internal uint Marking;
	internal uint Shorten;
	internal uint HiChar;
	internal uint HiCharPresent;
}

internal enum MuiTextPresentationStateField : byte
{
	Magic,
	SetMin,
	SetMax,
	SetVMax,
	ControlChar,
	Marking,
	Shorten,
	HiChar,
	HiCharPresent,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextPresentationStateFieldCursor
{
	internal APTR Record;
	internal MuiTextPresentationStateField Field;
}

internal static class MuiTextPresentationStateFieldCursorCodec
{
	private static bool TryResolve(MuiTextPresentationStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiTextPresentationStateField.Magic => 0,
			MuiTextPresentationStateField.SetMin => 4,
			MuiTextPresentationStateField.SetMax => 8,
			MuiTextPresentationStateField.SetVMax => 12,
			MuiTextPresentationStateField.ControlChar => 16,
			MuiTextPresentationStateField.Marking => 20,
			MuiTextPresentationStateField.Shorten => 24,
			MuiTextPresentationStateField.HiChar => 28,
			MuiTextPresentationStateField.HiCharPresent => 32,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextPresentationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiTextPresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiTextPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiTextPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiTextPresentationStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiTextPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiTextPresentationStateRecord.Size) ||
			!MuiTextPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiTextPresentationStateField.Magic, out var magic) ||
			magic != MuiTextPresentationStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiTextPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiTextPresentationStateField.SetMin, out value.SetMin) &&
			MuiTextPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiTextPresentationStateField.SetMax, out value.SetMax) &&
			MuiTextPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiTextPresentationStateField.SetVMax, out value.SetVMax) &&
			MuiTextPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiTextPresentationStateField.ControlChar,
			out value.ControlChar) &&
			MuiTextPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiTextPresentationStateField.Marking, out value.Marking) &&
			MuiTextPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiTextPresentationStateField.Shorten, out value.Shorten) &&
			MuiTextPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiTextPresentationStateField.HiChar, out value.HiChar) &&
			MuiTextPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiTextPresentationStateField.HiCharPresent,
			out value.HiCharPresent);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiTextPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiTextPresentationStateRecord.Size) || value.Magic !=
			MuiTextPresentationStateRecord.Cookie) return false;
		return MuiTextPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiTextPresentationStateField.Magic, value.Magic) &&
			MuiTextPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiTextPresentationStateField.SetMin, value.SetMin) &&
			MuiTextPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiTextPresentationStateField.SetMax, value.SetMax) &&
			MuiTextPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiTextPresentationStateField.SetVMax, value.SetVMax) &&
			MuiTextPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiTextPresentationStateField.ControlChar,
			value.ControlChar) &&
			MuiTextPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiTextPresentationStateField.Marking, value.Marking) &&
			MuiTextPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiTextPresentationStateField.Shorten, value.Shorten) &&
			MuiTextPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiTextPresentationStateField.HiChar, value.HiChar) &&
			MuiTextPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiTextPresentationStateField.HiCharPresent,
			value.HiCharPresent);
	}
}
