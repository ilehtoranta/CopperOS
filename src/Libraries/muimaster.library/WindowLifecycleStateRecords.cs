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

// The packed codec checks the guest representation; this admission boundary
// checks the lifecycle contract consumed by native-window operations.  A
// native capability exists exactly while the public Open projection is true.
// IconifiedOpen is intentionally independent: MorphOS retains an open request
// while an application is iconified, both before and after the native window
// is temporarily closed.
internal static class MuiWindowLifecycleStateAdmission
{
	internal static bool Validate(MuiWindowLifecycleStateRecord value) =>
		value.Magic == MuiWindowLifecycleStateRecord.Cookie &&
		value.Open <= 1 && value.IconifiedOpen <= 1 &&
		(value.Open == 0 ? value.NativeWindow.IsNull :
			value.NativeWindow.IsNotNull);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, MuiWindowLifecycleStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !window.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull;
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

// Struct-first guest-memory adapter. Lifecycle topology remains named state;
// this bounded boundary owns only the fixed four-byte guest fields.
internal static class MuiWindowLifecycleStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiWindowLifecycleStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiWindowLifecycleStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiWindowLifecycleStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiWindowLifecycleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiWindowLifecycleStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out var magic) ||
			!MuiWindowLifecycleStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 4, out var nativeWindow) ||
			!MuiWindowLifecycleStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 8, out value.Open) ||
			!MuiWindowLifecycleStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 12, out value.EventMask) ||
			!MuiWindowLifecycleStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 16, out value.IconifiedOpen)) return false;
		value.Magic = magic;
		value.NativeWindow = APTR.FromPointer(nativeWindow);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowLifecycleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiWindowLifecycleStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowLifecycleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowLifecycleStateAdmission.Validate(value)) return false;
		return MuiWindowLifecycleStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiWindowLifecycleStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 4, value.NativeWindow.Raw) &&
			MuiWindowLifecycleStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 8, value.Open) &&
			MuiWindowLifecycleStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 12, value.EventMask) &&
			MuiWindowLifecycleStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 16, value.IconifiedOpen);
	}
}
