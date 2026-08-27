/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Image selection, selected-visual, and free-axis policy shared by layout,
// drawing, and input.
// These values remain ULONG-compatible with MorphOS while callers consume one
// named semantic state instead of separate private widget slots.
public struct MuiImageRenderState
{
	public uint ImageState;
	public uint Selected;
	public uint FreeHoriz;
	public uint FreeVert;
	public uint ShowSelState;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageRenderStateRecord
{
	internal const uint Size = 24;
	internal const uint Cookie = 0x4D495253u; // 'MIRS'

	internal uint Magic;
	internal uint ImageState;
	internal uint Selected;
	internal uint FreeHoriz;
	internal uint FreeVert;
	internal uint ShowSelState;
}

internal enum MuiImageRenderStateField : byte
{
	Magic,
	ImageState,
	Selected,
	FreeHoriz,
	FreeVert,
	ShowSelState,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageRenderStateFieldCursor
{
	internal APTR Record;
	internal MuiImageRenderStateField Field;
}

internal static class MuiImageRenderStateFieldCursorCodec
{
	private static bool TryResolve(MuiImageRenderStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiImageRenderStateField.Magic => 0,
			MuiImageRenderStateField.ImageState => 4,
			MuiImageRenderStateField.Selected => 8,
			MuiImageRenderStateField.FreeHoriz => 12,
			MuiImageRenderStateField.FreeVert => 16,
			MuiImageRenderStateField.ShowSelState => 20,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiImageRenderStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiImageRenderStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageRenderStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiImageRenderStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageRenderStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiImageRenderStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Image consumers use the named render
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiImageRenderStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiImageRenderStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiImageRenderStateRecord.Size)) return false;
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

internal static class MuiImageRenderStateAdmission
{
	internal static bool Validate(MuiImageRenderStateRecord value) =>
		value.Magic == MuiImageRenderStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiImageRenderStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal static class MuiImageRenderStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiImageRenderStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiImageRenderStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic) &&
			MuiImageRenderStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out value.ImageState) &&
			MuiImageRenderStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 8, out value.Selected) &&
			MuiImageRenderStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 12, out value.FreeHoriz) &&
			MuiImageRenderStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 16, out value.FreeVert) &&
			MuiImageRenderStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 20, out value.ShowSelState);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiImageRenderStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiImageRenderStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiImageRenderStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiImageRenderStateAdmission.Validate(value)) return false;
		return MuiImageRenderStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiImageRenderStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			4, value.ImageState) &&
			MuiImageRenderStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			8, value.Selected) &&
			MuiImageRenderStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			12, value.FreeHoriz) &&
			MuiImageRenderStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			16, value.FreeVert) &&
			MuiImageRenderStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			20, value.ShowSelState);
	}
}
