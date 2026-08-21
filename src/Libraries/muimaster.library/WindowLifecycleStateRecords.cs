/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Native-window lifecycle state shared by open/close, IDCMP, and application
// iconification paths.  NativeWindow is an opaque capability; the guest
// record keeps it together with the public lifecycle projections.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowLifecycleStateRecord
{
	internal const uint Size = 20;
	internal const uint Cookie = 0x574C5354u; // 'WLST'

	internal uint Magic;
	internal APTR NativeWindow;
	internal uint Open;
	internal uint EventMask;
	internal uint IconifiedOpen;
}

internal enum MuiWindowLifecycleStateField : byte
{
	Magic,
	NativeWindow,
	Open,
	EventMask,
	IconifiedOpen,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowLifecycleStateFieldCursor
{
	internal APTR Record;
	internal MuiWindowLifecycleStateField Field;
}

internal static class MuiWindowLifecycleStateFieldCursorCodec
{
	private static bool TryResolve(MuiWindowLifecycleStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiWindowLifecycleStateField.Magic:
			case MuiWindowLifecycleStateField.NativeWindow:
			case MuiWindowLifecycleStateField.Open:
			case MuiWindowLifecycleStateField.EventMask:
			case MuiWindowLifecycleStateField.IconifiedOpen:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowLifecycleStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiWindowLifecycleStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowLifecycleStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiWindowLifecycleStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowLifecycleStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiWindowLifecycleStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiWindowLifecycleStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowLifecycleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiWindowLifecycleStateRecord.Size) ||
			!MuiWindowLifecycleStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowLifecycleStateField.Magic, out var magic) ||
			magic != MuiWindowLifecycleStateRecord.Cookie ||
			!MuiWindowLifecycleStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowLifecycleStateField.NativeWindow,
				out var nativeWindow) ||
			!MuiWindowLifecycleStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowLifecycleStateField.Open, out value.Open) ||
			!MuiWindowLifecycleStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowLifecycleStateField.EventMask,
				out value.EventMask) ||
			!MuiWindowLifecycleStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowLifecycleStateField.IconifiedOpen,
				out value.IconifiedOpen)) return false;
		value.Magic = magic;
		value.NativeWindow = APTR.FromPointer(nativeWindow);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowLifecycleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiWindowLifecycleStateRecord.Size) || value.Magic !=
			MuiWindowLifecycleStateRecord.Cookie) return false;
		return MuiWindowLifecycleStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiWindowLifecycleStateField.Magic, value.Magic) &&
			MuiWindowLifecycleStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowLifecycleStateField.NativeWindow,
				value.NativeWindow.Raw) &&
			MuiWindowLifecycleStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowLifecycleStateField.Open, value.Open) &&
			MuiWindowLifecycleStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowLifecycleStateField.EventMask,
				value.EventMask) &&
			MuiWindowLifecycleStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowLifecycleStateField.IconifiedOpen,
				value.IconifiedOpen);
	}
}
