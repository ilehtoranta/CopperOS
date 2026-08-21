/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Application lifecycle policy shared by initialization, iconification, and
// active-state dispatch.  The public MUI attributes remain the projection;
// application behavior consumes this one named guest-resident record.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationLifecycleStateRecord
{
	internal const uint Size = 28;
	internal const uint Cookie = 0x41505354u; // 'APST'

	internal uint Magic;
	internal uint Initialized;
	internal uint Iconified;
	internal uint Active;
	internal uint SingleTask;
	internal uint DoubleStart;
	internal uint ForceQuit;
}

internal enum MuiApplicationLifecycleStateField : byte
{
	Magic,
	Initialized,
	Iconified,
	Active,
	SingleTask,
	DoubleStart,
	ForceQuit,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationLifecycleStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationLifecycleStateField Field;
}

internal static class MuiApplicationLifecycleStateFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationLifecycleStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationLifecycleStateField.Magic:
			case MuiApplicationLifecycleStateField.Initialized:
			case MuiApplicationLifecycleStateField.Iconified:
			case MuiApplicationLifecycleStateField.Active:
			case MuiApplicationLifecycleStateField.SingleTask:
			case MuiApplicationLifecycleStateField.DoubleStart:
			case MuiApplicationLifecycleStateField.ForceQuit:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationLifecycleStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiApplicationLifecycleStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationLifecycleStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationLifecycleStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationLifecycleStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationLifecycleStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationLifecycleStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationLifecycleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationLifecycleStateRecord.Size) ||
			!MuiApplicationLifecycleStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationLifecycleStateField.Magic, out var magic) ||
			magic != MuiApplicationLifecycleStateRecord.Cookie ||
			!MuiApplicationLifecycleStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationLifecycleStateField.Initialized,
				out value.Initialized) ||
			!MuiApplicationLifecycleStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationLifecycleStateField.Iconified,
				out value.Iconified) ||
			!MuiApplicationLifecycleStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationLifecycleStateField.Active, out value.Active) ||
			!MuiApplicationLifecycleStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationLifecycleStateField.SingleTask,
				out value.SingleTask) ||
			!MuiApplicationLifecycleStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationLifecycleStateField.DoubleStart,
				out value.DoubleStart) ||
			!MuiApplicationLifecycleStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationLifecycleStateField.ForceQuit,
				out value.ForceQuit)) return false;
		value.Magic = magic;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationLifecycleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationLifecycleStateRecord.Size) || value.Magic !=
			MuiApplicationLifecycleStateRecord.Cookie) return false;
		return MuiApplicationLifecycleStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationLifecycleStateField.Magic,
			value.Magic) &&
			MuiApplicationLifecycleStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationLifecycleStateField.Initialized,
				value.Initialized) &&
			MuiApplicationLifecycleStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationLifecycleStateField.Iconified,
				value.Iconified) &&
			MuiApplicationLifecycleStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationLifecycleStateField.Active, value.Active) &&
			MuiApplicationLifecycleStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationLifecycleStateField.SingleTask,
				value.SingleTask) &&
			MuiApplicationLifecycleStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationLifecycleStateField.DoubleStart,
				value.DoubleStart) &&
			MuiApplicationLifecycleStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationLifecycleStateField.ForceQuit,
				value.ForceQuit);
	}
}
