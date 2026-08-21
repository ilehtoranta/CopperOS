/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Application scheduler state shared by ReturnID/Input, input-handler
// registration, pushed methods, and the signal wait loop. The public MUI
// attributes remain projections; queue ownership and signal selection use
// this one named guest-resident record.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationSchedulerStateRecord
{
	internal const uint Size = 28;
	internal const uint Cookie = 0x41535354u; // 'ASST'

	internal uint Magic;
	internal APTR ReturnHead;
	internal APTR ReturnTail;
	internal APTR InputHandlers;
	internal uint SignalMask;
	internal APTR PushHead;
	internal APTR PushTail;
}

internal enum MuiApplicationSchedulerStateField : byte
{
	Magic,
	ReturnHead,
	ReturnTail,
	InputHandlers,
	SignalMask,
	PushHead,
	PushTail,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationSchedulerStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationSchedulerStateField Field;
}

internal static class MuiApplicationSchedulerStateFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationSchedulerStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationSchedulerStateField.Magic:
			case MuiApplicationSchedulerStateField.ReturnHead:
			case MuiApplicationSchedulerStateField.ReturnTail:
			case MuiApplicationSchedulerStateField.InputHandlers:
			case MuiApplicationSchedulerStateField.SignalMask:
			case MuiApplicationSchedulerStateField.PushHead:
			case MuiApplicationSchedulerStateField.PushTail:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationSchedulerStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiApplicationSchedulerStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSchedulerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationSchedulerStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSchedulerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationSchedulerStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationSchedulerStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationSchedulerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationSchedulerStateRecord.Size) ||
			!MuiApplicationSchedulerStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSchedulerStateField.Magic, out var magic) ||
			magic != MuiApplicationSchedulerStateRecord.Cookie ||
			!MuiApplicationSchedulerStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSchedulerStateField.ReturnHead,
				out var returnHead) ||
			!MuiApplicationSchedulerStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSchedulerStateField.ReturnTail,
				out var returnTail) ||
			!MuiApplicationSchedulerStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSchedulerStateField.InputHandlers,
				out var inputHandlers) ||
			!MuiApplicationSchedulerStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSchedulerStateField.SignalMask,
				out value.SignalMask) ||
			!MuiApplicationSchedulerStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSchedulerStateField.PushHead,
				out var pushHead) ||
			!MuiApplicationSchedulerStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSchedulerStateField.PushTail,
				out var pushTail)) return false;
		value.Magic = magic;
		value.ReturnHead = APTR.FromPointer(returnHead);
		value.ReturnTail = APTR.FromPointer(returnTail);
		value.InputHandlers = APTR.FromPointer(inputHandlers);
		value.PushHead = APTR.FromPointer(pushHead);
		value.PushTail = APTR.FromPointer(pushTail);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationSchedulerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationSchedulerStateRecord.Size) || value.Magic !=
			MuiApplicationSchedulerStateRecord.Cookie) return false;
		return MuiApplicationSchedulerStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationSchedulerStateField.Magic, value.Magic) &&
			MuiApplicationSchedulerStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationSchedulerStateField.ReturnHead,
				value.ReturnHead.Raw) &&
			MuiApplicationSchedulerStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationSchedulerStateField.ReturnTail,
				value.ReturnTail.Raw) &&
			MuiApplicationSchedulerStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationSchedulerStateField.InputHandlers,
				value.InputHandlers.Raw) &&
			MuiApplicationSchedulerStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationSchedulerStateField.SignalMask,
				value.SignalMask) &&
			MuiApplicationSchedulerStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationSchedulerStateField.PushHead,
				value.PushHead.Raw) &&
			MuiApplicationSchedulerStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationSchedulerStateField.PushTail,
				value.PushTail.Raw);
	}
}
