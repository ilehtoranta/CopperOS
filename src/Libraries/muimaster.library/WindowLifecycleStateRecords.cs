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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint NativeWindowOffset = 4;
	internal const uint OpenOffset = 8;
	internal const uint EventMaskOffset = 12;
	internal const uint IconifiedOpenOffset = 16;
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
		if (field == MuiWindowLifecycleStateField.Magic)
			offset = MuiWindowLifecycleStateRecord.MagicOffset;
		else if (field == MuiWindowLifecycleStateField.NativeWindow)
			offset = MuiWindowLifecycleStateRecord.NativeWindowOffset;
		else if (field == MuiWindowLifecycleStateField.Open)
			offset = MuiWindowLifecycleStateRecord.OpenOffset;
		else if (field == MuiWindowLifecycleStateField.EventMask)
			offset = MuiWindowLifecycleStateRecord.EventMaskOffset;
		else if (field == MuiWindowLifecycleStateField.IconifiedOpen)
			offset = MuiWindowLifecycleStateRecord.IconifiedOpenOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
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
		return platform.IsMapped(address, MuiWindowLifecycleStateRecord.FieldSize);
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
	private static bool TryResolve(MuiWindowLifecycleStateField field,
		out uint offset)
	{
		if (field == MuiWindowLifecycleStateField.Magic)
			offset = MuiWindowLifecycleStateRecord.MagicOffset;
		else if (field == MuiWindowLifecycleStateField.NativeWindow)
			offset = MuiWindowLifecycleStateRecord.NativeWindowOffset;
		else if (field == MuiWindowLifecycleStateField.Open)
			offset = MuiWindowLifecycleStateRecord.OpenOffset;
		else if (field == MuiWindowLifecycleStateField.EventMask)
			offset = MuiWindowLifecycleStateRecord.EventMaskOffset;
		else if (field == MuiWindowLifecycleStateField.IconifiedOpen)
			offset = MuiWindowLifecycleStateRecord.IconifiedOpenOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowLifecycleStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryResolve(field, out var offset) &&
			TryGetAddress(ref platform, record, offset, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowLifecycleStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiWindowLifecycleStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowLifecycleStateField.Magic)
			value = state.Magic;
		else if (field == MuiWindowLifecycleStateField.NativeWindow)
			value = state.NativeWindow.Raw;
		else if (field == MuiWindowLifecycleStateField.Open)
			value = state.Open;
		else if (field == MuiWindowLifecycleStateField.EventMask)
			value = state.EventMask;
		else if (field == MuiWindowLifecycleStateField.IconifiedOpen)
			value = state.IconifiedOpen;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowLifecycleStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowLifecycleStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowLifecycleStateField.Magic)
			state.Magic = value;
		else if (field == MuiWindowLifecycleStateField.NativeWindow)
			state.NativeWindow = APTR.FromPointer(value);
		else if (field == MuiWindowLifecycleStateField.Open)
			state.Open = value;
		else if (field == MuiWindowLifecycleStateField.EventMask)
			state.EventMask = value;
		else if (field == MuiWindowLifecycleStateField.IconifiedOpen)
			state.IconifiedOpen = value;
		else return false;
		return MuiWindowLifecycleStateRecordCodec.WriteStructural(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiWindowLifecycleStateRecord.Size -
			MuiWindowLifecycleStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiWindowLifecycleStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiWindowLifecycleStateRecord.FieldSize);
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
	// Sequential named-struct path used by Window lifecycle operations. Cookie,
	// native-window capability, and lifecycle projections are exchanged in
	// declaration order; numeric positions remain confined to the compatibility
	// adapter.
	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowLifecycleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowLifecycleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWindowLifecycleStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.NativeWindow.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Open) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.EventMask) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.IconifiedOpen) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiWindowLifecycleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWindowLifecycleStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var nativeWindow) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var open) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var eventMask) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var iconifiedOpen) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Magic = magic;
		value.NativeWindow = APTR.FromPointer(nativeWindow);
		value.Open = open;
		value.EventMask = eventMask;
		value.IconifiedOpen = iconifiedOpen;
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiWindowLifecycleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

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
		return WriteRecord(ref platform, address, value);
	}
}
