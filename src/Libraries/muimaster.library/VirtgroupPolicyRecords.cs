/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Virtgroup policy keeps signed geometry values and BOOL policies in one
// guest-resident struct.  The existing layout record remains the layout
// scratch seam; this policy record is the public Get/Set projection.
public struct MuiVirtgroupPolicyState
{
	public uint Input;
	public int Width;
	public int Height;
	public int Left;
	public int Top;
	public uint TryFit;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiVirtgroupPolicyStateRecord
{
	internal const uint Size = 28;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint InputOffset = 4;
	internal const uint WidthOffset = 8;
	internal const uint HeightOffset = 12;
	internal const uint LeftOffset = 16;
	internal const uint TopOffset = 20;
	internal const uint TryFitOffset = 24;
	internal const uint Cookie = 0x56504750u; // 'VPGP'

	internal uint Magic;
	internal uint Input;
	internal int Width;
	internal int Height;
	internal int Left;
	internal int Top;
	internal uint TryFit;
}

internal enum MuiVirtgroupPolicyStateField : byte
{
	Magic,
	Input,
	Width,
	Height,
	Left,
	Top,
	TryFit,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiVirtgroupPolicyStateFieldCursor
{
	internal APTR Record;
	internal MuiVirtgroupPolicyStateField Field;
}

internal static class MuiVirtgroupPolicyStateValidation
{
	// MorphOS BOOL attributes are canonical guest values.  Signed geometry is
	// intentionally left as int32: VirtualLeft/Top may be negative at the
	// public boundary while LayoutCore applies the live viewport constraints.
	internal static bool IsValid(MuiVirtgroupPolicyStateRecord value) =>
		value.Magic == MuiVirtgroupPolicyStateRecord.Cookie &&
		value.Input <= 1 && value.TryFit <= 1;
}

internal static class MuiVirtgroupPolicyStateAdmission
{
	internal static bool Validate(MuiVirtgroupPolicyStateRecord value) =>
		MuiVirtgroupPolicyStateValidation.IsValid(value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiVirtgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal static class MuiVirtgroupPolicyStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiVirtgroupPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiVirtgroupPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiVirtgroupPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiVirtgroupPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiVirtgroupPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiVirtgroupPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. Signed virtual geometry and BOOL policy
// values remain named semantic fields; bounded fixed-layout translation lives
// here.
internal static class MuiVirtgroupPolicyStateRecordMemoryCodec
{
	private static bool TryResolve(MuiVirtgroupPolicyStateField field,
		out uint offset)
	{
		if (field == MuiVirtgroupPolicyStateField.Magic)
			offset = MuiVirtgroupPolicyStateRecord.MagicOffset;
		else if (field == MuiVirtgroupPolicyStateField.Input)
			offset = MuiVirtgroupPolicyStateRecord.InputOffset;
		else if (field == MuiVirtgroupPolicyStateField.Width)
			offset = MuiVirtgroupPolicyStateRecord.WidthOffset;
		else if (field == MuiVirtgroupPolicyStateField.Height)
			offset = MuiVirtgroupPolicyStateRecord.HeightOffset;
		else if (field == MuiVirtgroupPolicyStateField.Left)
			offset = MuiVirtgroupPolicyStateRecord.LeftOffset;
		else if (field == MuiVirtgroupPolicyStateField.Top)
			offset = MuiVirtgroupPolicyStateRecord.TopOffset;
		else if (field == MuiVirtgroupPolicyStateField.TryFit)
			offset = MuiVirtgroupPolicyStateRecord.TryFitOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiVirtgroupPolicyStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiVirtgroupPolicyStateRecord.Size) &&
			platform.IsMapped(address, MuiVirtgroupPolicyStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiVirtgroupPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiVirtgroupPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiVirtgroupPolicyStateRecordCodec
{
	// Declaration-order guest record: Magic, Input BOOL, signed geometry, and
	// TryFit BOOL. Signed LONGs are carried losslessly as ULONG bit patterns.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiVirtgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiVirtgroupPolicyStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Input) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var width) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var height) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var left) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var top) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.TryFit)) return false;
		value.Width = unchecked((int)width);
		value.Height = unchecked((int)height);
		value.Left = unchecked((int)left);
		value.Top = unchecked((int)top);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiVirtgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiVirtgroupPolicyStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Input) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Width)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Height)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Left)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Top)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.TryFit) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiVirtgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiVirtgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiVirtgroupPolicyStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiVirtgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiVirtgroupPolicyStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}
