/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared Area drawing policy.  Generic Area drawing and text helpers use one
// named guest-resident record instead of independently projecting the same
// public attributes at each call site.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaRenderPolicyStateRecord
{
	internal const uint Size = 36;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint FillAreaOffset = 4;
	internal const uint BackgroundOffset = 8;
	internal const uint FrameOffset = 12;
	internal const uint FontOffset = 16;
	internal const uint FrameVisibleOffset = 20;
	internal const uint FramePhantomHorizOffset = 24;
	internal const uint FrameTitleOffset = 28;
	internal const uint FrameDynamicOffset = 32;
	internal const uint Cookie = 0x41525052u; // 'ARPR'

	internal uint Magic;
	internal uint FillArea;
	internal uint Background;
	internal uint Frame;
	internal uint Font;
	internal uint FrameVisible;
	internal uint FramePhantomHoriz;
	internal APTR FrameTitle;
	internal uint FrameDynamic;
}

internal static class MuiAreaRenderPolicyStateAdmission
{
	internal static bool Validate(MuiAreaRenderPolicyStateRecord value) =>
		MuiAreaRenderPolicyStateValidation.IsValidState(value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaRenderPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(value) || obj.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		return value.FrameTitle.IsNull || CStringCodec.TryReadLength(ref platform,
			value.FrameTitle, 4096, out _);
	}
}

internal enum MuiAreaRenderPolicyStateField : byte
{
	Magic,
	FillArea,
	Background,
	Frame,
	Font,
	FrameVisible,
	FramePhantomHoriz,
	FrameTitle,
	FrameDynamic,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaRenderPolicyStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaRenderPolicyStateField Field;
}

internal static class MuiAreaRenderPolicyStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaRenderPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaRenderPolicyStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaRenderPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaRenderPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaRenderPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaRenderPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaRenderPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. The named render-policy record is the
// semantic boundary; this bounded adapter is the only layer that translates
// the fixed guest layout into addresses. The cursor codec remains available
// for compatibility and malformed-state diagnostics.
internal static class MuiAreaRenderPolicyStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiAreaRenderPolicyStateField field,
		out uint index)
	{
		if (field == MuiAreaRenderPolicyStateField.Magic) index = 0;
		else if (field == MuiAreaRenderPolicyStateField.FillArea) index = 1;
		else if (field == MuiAreaRenderPolicyStateField.Background) index = 2;
		else if (field == MuiAreaRenderPolicyStateField.Frame) index = 3;
		else if (field == MuiAreaRenderPolicyStateField.Font) index = 4;
		else if (field == MuiAreaRenderPolicyStateField.FrameVisible) index = 5;
		else if (field == MuiAreaRenderPolicyStateField.FramePhantomHoriz) index = 6;
		else if (field == MuiAreaRenderPolicyStateField.FrameTitle) index = 7;
		else if (field == MuiAreaRenderPolicyStateField.FrameDynamic) index = 8;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaRenderPolicyStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaRenderPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaRenderPolicyStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiAreaRenderPolicyStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiAreaRenderPolicyStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiAreaRenderPolicyStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaRenderPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaRenderPolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaRenderPolicyStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaRenderPolicyStateField.FillArea)
			value = state.FillArea;
		else if (field == MuiAreaRenderPolicyStateField.Background)
			value = state.Background;
		else if (field == MuiAreaRenderPolicyStateField.Frame)
			value = state.Frame;
		else if (field == MuiAreaRenderPolicyStateField.Font)
			value = state.Font;
		else if (field == MuiAreaRenderPolicyStateField.FrameVisible)
			value = state.FrameVisible;
		else if (field == MuiAreaRenderPolicyStateField.FramePhantomHoriz)
			value = state.FramePhantomHoriz;
		else if (field == MuiAreaRenderPolicyStateField.FrameTitle)
			value = state.FrameTitle.Raw;
		else if (field == MuiAreaRenderPolicyStateField.FrameDynamic)
			value = state.FrameDynamic;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaRenderPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaRenderPolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaRenderPolicyStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaRenderPolicyStateField.FillArea)
			state.FillArea = value;
		else if (field == MuiAreaRenderPolicyStateField.Background)
			state.Background = value;
		else if (field == MuiAreaRenderPolicyStateField.Frame)
			state.Frame = value;
		else if (field == MuiAreaRenderPolicyStateField.Font)
			state.Font = value;
		else if (field == MuiAreaRenderPolicyStateField.FrameVisible)
			state.FrameVisible = value;
		else if (field == MuiAreaRenderPolicyStateField.FramePhantomHoriz)
			state.FramePhantomHoriz = value;
		else if (field == MuiAreaRenderPolicyStateField.FrameTitle)
			state.FrameTitle = APTR.FromPointer(value);
		else if (field == MuiAreaRenderPolicyStateField.FrameDynamic)
			state.FrameDynamic = value;
		else return false;
		return MuiAreaRenderPolicyStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiAreaRenderPolicyStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaRenderPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaRenderPolicyStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.FillArea) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Background) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Frame) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Font) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.FrameVisible) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.FramePhantomHoriz) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var frameTitle) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.FrameDynamic)) return false;
		value.FrameTitle = APTR.FromPointer(frameTitle);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaRenderPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaRenderPolicyStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.FillArea) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Background) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Frame) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Font) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.FrameVisible) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.FramePhantomHoriz) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.FrameTitle.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.FrameDynamic) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaRenderPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaRenderPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiAreaRenderPolicyStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaRenderPolicyStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaRenderPolicyStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}

// FillArea, FrameVisible, FramePhantomHoriz, and FrameDynamic are BOOL-like
// MorphOS ULONGs. The remaining fields are selectors or caller-owned pointers
// and are intentionally preserved without narrowing their bit patterns.
internal static class MuiAreaRenderPolicyStateValidation
{
	internal static bool IsValidState(MuiAreaRenderPolicyStateRecord value) =>
		value.Magic == MuiAreaRenderPolicyStateRecord.Cookie &&
		value.FillArea <= 1 && value.FrameVisible <= 1 &&
		value.FramePhantomHoriz <= 1 && value.FrameDynamic <= 1;

	internal static bool IsValidRecord(MuiAreaRenderPolicyStateRecord value) =>
		IsValidState(value);
}
