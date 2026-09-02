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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint HorizontalBarOffset = 4;
	internal const uint VerticalBarOffset = 8;
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
		if (field == MuiRectanglePresentationStateField.Magic)
			offset = MuiRectanglePresentationStateRecord.MagicOffset;
		else if (field == MuiRectanglePresentationStateField.HorizontalBar)
			offset = MuiRectanglePresentationStateRecord.HorizontalBarOffset;
		else if (field == MuiRectanglePresentationStateField.VerticalBar)
			offset = MuiRectanglePresentationStateRecord.VerticalBarOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
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
		return platform.IsMapped(address, MuiRectanglePresentationStateRecord.FieldSize);
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
	private static bool TryResolve(MuiRectanglePresentationStateField field,
		out uint offset)
	{
		if (field == MuiRectanglePresentationStateField.Magic)
			offset = MuiRectanglePresentationStateRecord.MagicOffset;
		else if (field == MuiRectanglePresentationStateField.HorizontalBar)
			offset = MuiRectanglePresentationStateRecord.HorizontalBarOffset;
		else if (field == MuiRectanglePresentationStateField.VerticalBar)
			offset = MuiRectanglePresentationStateRecord.VerticalBarOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiRectanglePresentationStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryResolve(field, out var offset) &&
			TryGetAddress(ref platform, record, offset, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRectanglePresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiRectanglePresentationStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiRectanglePresentationStateField.Magic)
			value = state.Magic;
		else if (field == MuiRectanglePresentationStateField.HorizontalBar)
			value = state.HorizontalBar;
		else if (field == MuiRectanglePresentationStateField.VerticalBar)
			value = state.VerticalBar;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRectanglePresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiRectanglePresentationStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiRectanglePresentationStateField.Magic)
			state.Magic = value;
		else if (field == MuiRectanglePresentationStateField.HorizontalBar)
			state.HorizontalBar = value;
		else if (field == MuiRectanglePresentationStateField.VerticalBar)
			state.VerticalBar = value;
		else return false;
		return MuiRectanglePresentationStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiRectanglePresentationStateRecord.Size -
			MuiRectanglePresentationStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiRectanglePresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiRectanglePresentationStateRecord.FieldSize);
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
	// Production access is sequential and struct-shaped. The field-address
	// adapters above remain available for compatibility diagnostics only.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiRectanglePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiRectanglePresentationStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var horizontal) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var vertical) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.Magic = magic;
		value.HorizontalBar = horizontal;
		value.VerticalBar = vertical;
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiRectanglePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiRectanglePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiRectanglePresentationStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiRectanglePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiRectanglePresentationStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.HorizontalBar) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.VerticalBar)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiRectanglePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiRectanglePresentationStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
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
