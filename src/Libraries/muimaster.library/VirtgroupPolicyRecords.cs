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
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiVirtgroupPolicyStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiVirtgroupPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);
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
	private static bool TryResolveFieldIndex(MuiVirtgroupPolicyStateField field,
		out uint index)
	{
		if (field == MuiVirtgroupPolicyStateField.Magic)
			index = 0;
		else if (field == MuiVirtgroupPolicyStateField.Input)
			index = 1;
		else if (field == MuiVirtgroupPolicyStateField.Width)
			index = 2;
		else if (field == MuiVirtgroupPolicyStateField.Height)
			index = 3;
		else if (field == MuiVirtgroupPolicyStateField.Left)
			index = 4;
		else if (field == MuiVirtgroupPolicyStateField.Top)
			index = 5;
		else if (field == MuiVirtgroupPolicyStateField.TryFit)
			index = 6;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiVirtgroupPolicyStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiVirtgroupPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiVirtgroupPolicyStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiVirtgroupPolicyStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiVirtgroupPolicyStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiVirtgroupPolicyStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiVirtgroupPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiVirtgroupPolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiVirtgroupPolicyStateField.Magic)
			value = state.Magic;
		else if (field == MuiVirtgroupPolicyStateField.Input)
			value = state.Input;
		else if (field == MuiVirtgroupPolicyStateField.Width)
			value = unchecked((uint)state.Width);
		else if (field == MuiVirtgroupPolicyStateField.Height)
			value = unchecked((uint)state.Height);
		else if (field == MuiVirtgroupPolicyStateField.Left)
			value = unchecked((uint)state.Left);
		else if (field == MuiVirtgroupPolicyStateField.Top)
			value = unchecked((uint)state.Top);
		else if (field == MuiVirtgroupPolicyStateField.TryFit)
			value = state.TryFit;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiVirtgroupPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiVirtgroupPolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiVirtgroupPolicyStateField.Magic)
			state.Magic = value;
		else if (field == MuiVirtgroupPolicyStateField.Input)
			state.Input = value;
		else if (field == MuiVirtgroupPolicyStateField.Width)
			state.Width = unchecked((int)value);
		else if (field == MuiVirtgroupPolicyStateField.Height)
			state.Height = unchecked((int)value);
		else if (field == MuiVirtgroupPolicyStateField.Left)
			state.Left = unchecked((int)value);
		else if (field == MuiVirtgroupPolicyStateField.Top)
			state.Top = unchecked((int)value);
		else if (field == MuiVirtgroupPolicyStateField.TryFit)
			state.TryFit = value;
		else return false;
		return MuiVirtgroupPolicyStateRecordCodec.WriteStructural(ref platform,
			record, state);
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

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiVirtgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);

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
