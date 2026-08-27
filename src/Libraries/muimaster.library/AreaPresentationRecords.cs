/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared Area-derived presentation policy. The values retain MorphOS ULONG
// semantics while common-control input, sizing, and drawing consume one named
// guest-resident record.
public struct MuiAreaPresentationState
{
	public uint Disabled;
	public uint ShowMe;
	public uint Background;
	public uint Frame;
	public uint CustomBackfill;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaPresentationStateRecord
{
	internal const uint Size = 24;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint DisabledOffset = 4;
	internal const uint ShowMeOffset = 8;
	internal const uint BackgroundOffset = 12;
	internal const uint FrameOffset = 16;
	internal const uint CustomBackfillOffset = 20;
	internal const uint Cookie = 0x4D415052u; // 'MAPR'

	internal uint Magic;
	internal uint Disabled;
	internal uint ShowMe;
	internal uint Background;
	internal uint Frame;
	internal uint CustomBackfill;
}

internal enum MuiAreaPresentationStateField : byte
{
	Magic,
	Disabled,
	ShowMe,
	Background,
	Frame,
	CustomBackfill,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaPresentationStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaPresentationStateField Field;
}

internal static class MuiAreaPresentationStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaPresentationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaPresentationStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. The cursor codec above remains available
// for compatibility callers, while all named-record reads and writes resolve
// fields through the fixed MorphOS record layout and bounded guest memory.
internal static class MuiAreaPresentationStateRecordMemoryCodec
{
	private static bool TryResolve(MuiAreaPresentationStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaPresentationStateField.Magic:
				offset = MuiAreaPresentationStateRecord.MagicOffset;
				return true;
			case MuiAreaPresentationStateField.Disabled:
				offset = MuiAreaPresentationStateRecord.DisabledOffset;
				return true;
			case MuiAreaPresentationStateField.ShowMe:
				offset = MuiAreaPresentationStateRecord.ShowMeOffset;
				return true;
			case MuiAreaPresentationStateField.Background:
				offset = MuiAreaPresentationStateRecord.BackgroundOffset;
				return true;
			case MuiAreaPresentationStateField.Frame:
				offset = MuiAreaPresentationStateRecord.FrameOffset;
				return true;
			case MuiAreaPresentationStateField.CustomBackfill:
				offset = MuiAreaPresentationStateRecord.CustomBackfillOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaPresentationStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiAreaPresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiAreaPresentationStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaPresentationStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiAreaPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaPresentationStateField.Magic, out value.Magic) &&
			MuiAreaPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaPresentationStateField.Disabled, out value.Disabled) &&
			MuiAreaPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaPresentationStateField.ShowMe, out value.ShowMe) &&
			MuiAreaPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaPresentationStateField.Background, out value.Background) &&
			MuiAreaPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaPresentationStateField.Frame, out value.Frame) &&
			MuiAreaPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaPresentationStateField.CustomBackfill,
			out value.CustomBackfill);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaPresentationStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaPresentationStateAdmission.Validate(value)) return false;
		return MuiAreaPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiAreaPresentationStateField.Magic, value.Magic) &&
			MuiAreaPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiAreaPresentationStateField.Disabled, value.Disabled) &&
			MuiAreaPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiAreaPresentationStateField.ShowMe, value.ShowMe) &&
			MuiAreaPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiAreaPresentationStateField.Background, value.Background) &&
			MuiAreaPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiAreaPresentationStateField.Frame, value.Frame) &&
			MuiAreaPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiAreaPresentationStateField.CustomBackfill,
			value.CustomBackfill);
	}
}

// MorphOS presents Disabled, ShowMe, and CustomBackfill as BOOL-like ULONGs.
// Background and Frame remain unrestricted ULONG selectors/pointers and must
// therefore be preserved losslessly. Keep validation separate from the codec
// so malformed guest state is rejected rather than normalized in place.
internal static class MuiAreaPresentationStateAdmission
{
	internal static bool Validate(MuiAreaPresentationState value) =>
		value.Disabled <= 1 && value.ShowMe <= 1 && value.CustomBackfill <= 1;

	internal static bool Validate(MuiAreaPresentationStateRecord value)
	{
		if (value.Magic != MuiAreaPresentationStateRecord.Cookie) return false;
		var state = default(MuiAreaPresentationState);
		state.Disabled = value.Disabled;
		state.ShowMe = value.ShowMe;
		state.Background = value.Background;
		state.Frame = value.Frame;
		state.CustomBackfill = value.CustomBackfill;
		return Validate(state);
	}

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

// Compatibility alias for existing Area presentation call sites. New state
// boundaries use MuiAreaPresentationStateAdmission directly so live ownership
// is explicit.
internal static class MuiAreaPresentationStateValidation
{
	internal static bool IsValidState(MuiAreaPresentationState value) =>
		MuiAreaPresentationStateAdmission.Validate(value);

	internal static bool IsValidRecord(MuiAreaPresentationStateRecord value) =>
		MuiAreaPresentationStateAdmission.Validate(value);
}
