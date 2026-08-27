/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Rectangle decorative-bar state. MorphOS documents HBar and VBar as BOOL;
// retain their guest-width representation while admitting only canonical
// values at the named state boundary.
public struct MuiRectanglePresentationState
{
	public uint HorizontalBar;
	public uint VerticalBar;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRectanglePresentationStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x4D525443u; // 'MRTC'

	internal uint Magic;
	internal uint HorizontalBar;
	internal uint VerticalBar;
}

internal enum MuiRectanglePresentationStateField : byte
{
	Magic,
	HorizontalBar,
	VerticalBar,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRectanglePresentationStateFieldCursor
{
	internal APTR Record;
	internal MuiRectanglePresentationStateField Field;
}

internal static class MuiRectanglePresentationStateFieldCursorCodec
{
	private static bool TryResolve(MuiRectanglePresentationStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiRectanglePresentationStateField.Magic => 0,
			MuiRectanglePresentationStateField.HorizontalBar => 4,
			MuiRectanglePresentationStateField.VerticalBar => 8,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiRectanglePresentationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiRectanglePresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRectanglePresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiRectanglePresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRectanglePresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiRectanglePresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Bar flags remain named semantic fields;
// fixed guest-layout translation is bounded to this adapter.
internal static class MuiRectanglePresentationStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiRectanglePresentationStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiRectanglePresentationStateRecord.Size)) return false;
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

internal static class MuiRectanglePresentationStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiRectanglePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiRectanglePresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic)) return false;
		return MuiRectanglePresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out value.HorizontalBar) &&
			MuiRectanglePresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 8, out value.VerticalBar);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiRectanglePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiRectanglePresentationStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiRectanglePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiRectanglePresentationStateAdmission.Validate(value)) return false;
		return MuiRectanglePresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiRectanglePresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 4, value.HorizontalBar) &&
			MuiRectanglePresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 8, value.VerticalBar);
	}
}

internal static class MuiRectanglePresentationStateAdmission
{
	internal static bool Validate(MuiRectanglePresentationState value) =>
		value.HorizontalBar <= 1 && value.VerticalBar <= 1;

	internal static bool Validate(MuiRectanglePresentationStateRecord value)
	{
		if (value.Magic != MuiRectanglePresentationStateRecord.Cookie) return false;
		var state = default(MuiRectanglePresentationState);
		state.HorizontalBar = value.HorizontalBar;
		state.VerticalBar = value.VerticalBar;
		return Validate(state);
	}

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiRectanglePresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}
