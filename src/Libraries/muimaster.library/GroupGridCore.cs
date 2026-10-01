/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiGroupGridSpec
{
	public const uint Size = 32;
	public const uint FieldSize = 4;
	public const uint ColumnsOffset = 0;
	public const uint RowsOffset = 4;
	public const uint HorizontalSpacingOffset = 8;
	public const uint VerticalSpacingOffset = 12;
	public const uint SameWidthOffset = 16;
	public const uint SameHeightOffset = 20;
	public const uint HorizontalCenterOffset = 24;
	public const uint VerticalCenterOffset = 28;
	public uint Columns;
	public uint Rows;
	public uint HorizontalSpacing;
	public uint VerticalSpacing;
	public uint SameWidth;
	public uint SameHeight;
	public uint HorizontalCenter;
	public uint VerticalCenter;
}

internal enum MuiGroupGridSpecField : byte
{
	Columns,
	Rows,
	HorizontalSpacing,
	VerticalSpacing,
	SameWidth,
	SameHeight,
	HorizontalCenter,
	VerticalCenter,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupGridSpecFieldCursor
{
	internal APTR Record;
	internal MuiGroupGridSpecField Field;
}

internal static class MuiGroupGridSpecFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupGridSpecFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupGridSpecMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupGridSpecField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupGridSpecMemoryCodec.TryReadUInt32(ref platform, record,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupGridSpecField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupGridSpecMemoryCodec.TryWriteUInt32(ref platform, record,
			field, value);
}

// Struct-first guest-memory adapter for the fixed eight-ULONG Group grid
// specification. The named record owns the wire positions; this bounded
// adapter is the only place that turns those positions into guest addresses.
internal static class MuiGroupGridSpecMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiGroupGridSpecField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		var selected = (uint)field;
		if (selected > (uint)MuiGroupGridSpecField.VerticalCenter) return false;
		for (var index = 0u; index <= selected; index++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGroupGridSpec.FieldSize, out var candidate)) return false;
			if (index == selected)
			{
				address = candidate;
				return true;
			}
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupGridSpecField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, record,
			MuiGroupGridSpec.Size, out var cursor)) return false;
		return TryTakeField(ref platform, ref cursor, field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupGridSpecField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiGroupGridSpecCodec.TryRead(ref platform, record, out var spec))
			return false;
		if (field == MuiGroupGridSpecField.Columns)
			value = spec.Columns;
		else if (field == MuiGroupGridSpecField.Rows)
			value = spec.Rows;
		else if (field == MuiGroupGridSpecField.HorizontalSpacing)
			value = spec.HorizontalSpacing;
		else if (field == MuiGroupGridSpecField.VerticalSpacing)
			value = spec.VerticalSpacing;
		else if (field == MuiGroupGridSpecField.SameWidth)
			value = spec.SameWidth;
		else if (field == MuiGroupGridSpecField.SameHeight)
			value = spec.SameHeight;
		else if (field == MuiGroupGridSpecField.HorizontalCenter)
			value = spec.HorizontalCenter;
		else if (field == MuiGroupGridSpecField.VerticalCenter)
			value = spec.VerticalCenter;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupGridSpecField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGroupGridSpecCodec.TryRead(ref platform, record, out var spec))
			return false;
		if (field == MuiGroupGridSpecField.Columns)
			spec.Columns = value;
		else if (field == MuiGroupGridSpecField.Rows)
			spec.Rows = value;
		else if (field == MuiGroupGridSpecField.HorizontalSpacing)
			spec.HorizontalSpacing = value;
		else if (field == MuiGroupGridSpecField.VerticalSpacing)
			spec.VerticalSpacing = value;
		else if (field == MuiGroupGridSpecField.SameWidth)
			spec.SameWidth = value;
		else if (field == MuiGroupGridSpecField.SameHeight)
			spec.SameHeight = value;
		else if (field == MuiGroupGridSpecField.HorizontalCenter)
			spec.HorizontalCenter = value;
		else if (field == MuiGroupGridSpecField.VerticalCenter)
			spec.VerticalCenter = value;
		else return false;
		return MuiGroupGridSpecCodec.Write(ref platform, record, spec);
	}
}

internal static class MuiGroupGridSpecCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupGridSpec spec)
		where TPlatform : struct, IMuiGuestMemory
	{
		spec = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupGridSpec.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out spec.Columns) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out spec.Rows) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out spec.HorizontalSpacing) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out spec.VerticalSpacing) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out spec.SameWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out spec.SameHeight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out spec.HorizontalCenter) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out spec.VerticalCenter) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupGridSpec spec)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupGridSpec.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				spec.Columns) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				spec.Rows) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				spec.HorizontalSpacing) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				spec.VerticalSpacing) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				spec.SameWidth) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				spec.SameHeight) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				spec.HorizontalCenter) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				spec.VerticalCenter)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupGridSpec spec)
		where TPlatform : struct, IMuiGuestMemory
	{
		return WriteRecord(ref platform, address, spec);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupGridSpec spec)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadRecord(ref platform, address, out spec);
	}
}

// Fixed guest record seam for native qualification. The runtime layout path
// reads the same named fields from Group attributes; this helper only checks
// that the freestanding compiler preserves the typed record representation.
public static class MuiGroupGridQualification
{
	public static bool WriteSpecRecord<TPlatform>(ref TPlatform platform,
		APTR storage, uint columns, uint rows, uint horizontalSpacing,
		uint verticalSpacing, uint sameWidth, uint sameHeight,
		uint horizontalCenter, uint verticalCenter)
		where TPlatform : struct, IMuiGuestMemory
	{
		var spec = default(MuiGroupGridSpec);
		spec.Columns = columns;
		spec.Rows = rows;
		spec.HorizontalSpacing = horizontalSpacing;
		spec.VerticalSpacing = verticalSpacing;
		spec.SameWidth = sameWidth;
		spec.SameHeight = sameHeight;
		spec.HorizontalCenter = horizontalCenter;
		spec.VerticalCenter = verticalCenter;
		return MuiGroupGridSpecCodec.Write(ref platform, storage, spec);
	}

	public static uint DispatchSpecRecord<TPlatform>(ref TPlatform platform,
		APTR storage) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGroupGridSpecCodec.TryRead(ref platform, storage,
			out var spec)) return 0;
		return spec.Columns ^ spec.Rows ^ spec.HorizontalSpacing ^
			spec.VerticalSpacing ^ spec.SameWidth ^ spec.SameHeight ^
			spec.HorizontalCenter ^ spec.VerticalCenter;
	}
}

// Bounded two-dimensional Group layout. The record is value-typed so the
// layout decision is explicit and no managed collection represents the grid.
internal static class MuiGroupGridCore
{
	private const uint Columns = 0x8042F416;
	private const uint Rows = 0x8042B68F;
	private const uint HorizontalSpacing = 0x8042C651;
	private const uint VerticalSpacing = 0x8042E1BF;
	private const uint Spacing = 0x8042866D;
	private const uint SameWidth = 0x8042B3EC;
	private const uint SameHeight = 0x8042037E;
	private const uint SameSize = 0x80420860;
	private const uint HorizontalCenter = 0x8042CC64;
	private const uint VerticalCenter = 0x8042C008;
	private const uint StateKey = 0x0D100014u;
	private const uint MaximumAxis = 256;
	private const int NoDisappearPriority = 0;

	internal static bool IsGridAttribute(uint attribute) =>
		attribute == Columns || attribute == Rows ||
		attribute == HorizontalSpacing || attribute == VerticalSpacing ||
		attribute == Spacing || attribute == SameWidth ||
		attribute == SameHeight || attribute == SameSize ||
		attribute == HorizontalCenter || attribute == VerticalCenter;

	// Public Group getter projection. Once the named state record exists it is
	// authoritative; only the first read bootstraps it from raw attributes.
	internal static bool TryGetAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (!IsGridAttribute(attribute)) return false;
		if (TryGetStateRecord(ref platform, state, group, out var record))
		{
			value = attribute == Columns ? record.Columns :
				attribute == Rows ? record.Rows :
				attribute == HorizontalSpacing ? record.HorizontalSpacing :
				attribute == VerticalSpacing ? record.VerticalSpacing :
				attribute == Spacing ? record.HorizontalSpacing :
				attribute == SameWidth ? record.SameWidth :
				attribute == SameHeight ? record.SameHeight :
				attribute == SameSize ? (record.SameWidth != 0 &&
					record.SameHeight != 0 ? 1u : 0u) :
				attribute == HorizontalCenter ? record.HorizontalCenter :
				record.VerticalCenter;
			return true;
		}
		var stateBlock = MuiStoreCore.DataspaceFind(ref platform, state, group,
			StateKey);
		var stateLength = MuiStoreCore.DataspaceLength(ref platform, state, group,
			StateKey);
		// A present named record is authoritative for the public getter seam.
		// Never recover malformed state by projecting raw compatibility slots.
		if (stateBlock.IsNotNull || stateLength != 0) return false;
		var source = attribute == Spacing ? Spacing : attribute;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
			source, out _) &&
			(attribute != HorizontalSpacing && attribute != VerticalSpacing ||
				!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
					Spacing, out _))) return false;
		if (!TryRead(ref platform, state, group, out var spec)) return false;
		value = attribute == Columns ? spec.Columns :
			attribute == Rows ? spec.Rows :
			attribute == HorizontalSpacing ? spec.HorizontalSpacing :
			attribute == VerticalSpacing ? spec.VerticalSpacing :
			attribute == Spacing ? spec.HorizontalSpacing :
			attribute == SameWidth ? spec.SameWidth :
			attribute == SameHeight ? spec.SameHeight :
			attribute == SameSize ? (spec.SameWidth != 0 &&
				spec.SameHeight != 0 ? 1u : 0u) :
			attribute == HorizontalCenter ? spec.HorizontalCenter :
			spec.VerticalCenter;
		return true;
	}

	internal static MuiGroupGridSpec Read<TPlatform>(ref TPlatform platform,
		APTR state, APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryRead(ref platform, state, group, out var spec) ? spec :
			default;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, out MuiGroupGridSpec spec)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		spec = default;
		if (!TryReadState(ref platform, state, group, out var record)) return false;
		spec = ToSpec(record);
		return true;
	}

	internal static bool TryGetStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, out MuiGroupGridStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, group).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, group,
			StateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, group, StateKey) !=
			unchecked((int)MuiGroupGridStateRecord.Size)) return false;
		return MuiGroupGridStateRecordCodec.TryReadStructural(ref platform, block,
			out value) && MuiGroupGridStateAdmission.ValidateLive(ref platform, state,
			group, value);
	}

	internal static bool StateAvailable<TPlatform>(ref TPlatform platform,
		APTR state, APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, group).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, group,
			StateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, group,
			StateKey);
		if (block.IsNull && length == 0) return true;
		return length == unchecked((int)MuiGroupGridStateRecord.Size) &&
			TryGetStateRecord(ref platform, state, group, out _);
	}

	private static bool TryReadState<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, out MuiGroupGridStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, group).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, group,
			StateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, group,
			StateKey);
		var present = block.IsNotNull || length != 0;
		if (present)
		{
			if (length != unchecked((int)MuiGroupGridStateRecord.Size) ||
				!MuiGroupGridStateRecordCodec.TryReadStructural(ref platform, block,
					out var record) ||
				!MuiGroupGridStateAdmission.ValidateLive(ref platform, state, group,
					record)) return false;
			value = record;
			if (!TryFillState(ref platform, state, group, ref value) ||
				!MuiGroupGridStateAdmission.ValidateLive(ref platform, state, group,
					value)) return false;
			if (record.Columns != value.Columns || record.Rows != value.Rows ||
				record.HorizontalSpacing != value.HorizontalSpacing ||
				record.VerticalSpacing != value.VerticalSpacing ||
				record.SameWidth != value.SameWidth ||
				record.SameHeight != value.SameHeight ||
					record.HorizontalCenter != value.HorizontalCenter ||
					record.VerticalCenter != value.VerticalCenter)
				return MuiGroupGridStateAdmission.ValidateLive(ref platform, state,
					group, value) && MuiGroupGridStateRecordCodec.Write(ref platform,
					block, value);
			return true;
		}

		value = default;
		value.Magic = MuiGroupGridStateRecord.Cookie;
		if (!TryFillState(ref platform, state, group, ref value) ||
			!MuiGroupGridStateAdmission.ValidateLive(ref platform, state, group,
				value)) return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiGroupGridStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiGroupGridStateRecord.Size);
		var written = MuiGroupGridStateRecordCodec.Write(ref platform, scratch,
			value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, group,
			StateKey, scratch, unchecked((int)MuiGroupGridStateRecord.Size));
		platform.Clear(scratch, MuiGroupGridStateRecord.Size);
		platform.Free(scratch, MuiGroupGridStateRecord.Size);
		return added;
	}

	private static bool TryFillState<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, ref MuiGroupGridStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		ReadValue(ref platform, state, group, Columns, 0, out value.Columns);
		ReadValue(ref platform, state, group, Rows, 0, out value.Rows);
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
			HorizontalSpacing, out value.HorizontalSpacing))
			ReadValue(ref platform, state, group, Spacing, 0,
				out value.HorizontalSpacing);
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
			VerticalSpacing, out value.VerticalSpacing))
			ReadValue(ref platform, state, group, Spacing, 0,
				out value.VerticalSpacing);
		ReadValue(ref platform, state, group, SameWidth, 0, out value.SameWidth);
		ReadValue(ref platform, state, group, SameHeight, 0,
			out value.SameHeight);
		ReadValue(ref platform, state, group, SameSize, 0, out var sameSize);
		if (sameSize > 1) return false;
		if (sameSize != 0)
		{
			value.SameWidth = 1;
			value.SameHeight = 1;
		}
		ReadValue(ref platform, state, group, HorizontalCenter, 1,
			out value.HorizontalCenter);
		ReadValue(ref platform, state, group, VerticalCenter, 1,
			out value.VerticalCenter);
		value.Columns = ClampAxis(value.Columns);
		value.Rows = ClampAxis(value.Rows);
		value.HorizontalSpacing = NormalizeSpacing(value.HorizontalSpacing);
		value.VerticalSpacing = NormalizeSpacing(value.VerticalSpacing);
		value.HorizontalCenter = ClampCenter(value.HorizontalCenter);
		value.VerticalCenter = ClampCenter(value.VerticalCenter);
		return true;
	}

	private static MuiGroupGridSpec ReadRawSpec<TPlatform>(ref TPlatform platform,
		APTR state, APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var result = default(MuiGroupGridSpec);
		ReadValue(ref platform, state, group, Columns, 0, out result.Columns);
		ReadValue(ref platform, state, group, Rows, 0, out result.Rows);
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
			HorizontalSpacing, out result.HorizontalSpacing))
			ReadValue(ref platform, state, group, Spacing, 0,
				out result.HorizontalSpacing);
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
			VerticalSpacing, out result.VerticalSpacing))
			ReadValue(ref platform, state, group, Spacing, 0,
				out result.VerticalSpacing);
		ReadValue(ref platform, state, group, SameWidth, 0, out result.SameWidth);
		ReadValue(ref platform, state, group, SameHeight, 0,
			out result.SameHeight);
		ReadValue(ref platform, state, group, SameSize, 0, out var sameSize);
		if (sameSize != 0)
		{
			result.SameWidth = 1;
			result.SameHeight = 1;
		}
		ReadValue(ref platform, state, group, HorizontalCenter, 1,
			out result.HorizontalCenter);
		ReadValue(ref platform, state, group, VerticalCenter, 1,
			out result.VerticalCenter);
		result.Columns = ClampAxis(result.Columns);
		result.Rows = ClampAxis(result.Rows);
		result.HorizontalSpacing = NormalizeSpacing(result.HorizontalSpacing);
		result.VerticalSpacing = NormalizeSpacing(result.VerticalSpacing);
		result.HorizontalCenter = ClampCenter(result.HorizontalCenter);
		result.VerticalCenter = ClampCenter(result.VerticalCenter);
		return result;
	}

	private static MuiGroupGridSpec ToSpec(MuiGroupGridStateRecord value)
	{
		var spec = default(MuiGroupGridSpec);
		spec.Columns = value.Columns;
		spec.Rows = value.Rows;
		spec.HorizontalSpacing = value.HorizontalSpacing;
		spec.VerticalSpacing = value.VerticalSpacing;
		spec.SameWidth = value.SameWidth;
		spec.SameHeight = value.SameHeight;
		spec.HorizontalCenter = value.HorizontalCenter;
		spec.VerticalCenter = value.VerticalCenter;
		return spec;
	}

	internal static bool IsEnabled(MuiGroupGridSpec spec, int count) =>
		count > 0 && (spec.Columns != 0 || spec.Rows != 0);

	private static bool IsShown<TPlatform>(ref TPlatform platform, APTR state,
		APTR child) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiAreaLayoutCore.TryReadLayoutPolicyState(ref platform, state,
			child, out var policy)) return true;
		return policy.ShowMe != 0;
	}

	private static int ReadDisappearPriority<TPlatform>(ref TPlatform platform,
		APTR state, APTR child, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiAreaDisappearCore.TryReadState(ref platform, state, child,
			out var disappear)) return NoDisappearPriority;
		return horizontal ? disappear.HorizDisappear : disappear.VertDisappear;
	}

	private static bool IsHidden<TPlatform>(ref TPlatform platform, APTR state,
		APTR child, bool horizontal, int hiddenPriority)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!IsShown(ref platform, state, child)) return true;
		if (hiddenPriority <= NoDisappearPriority) return false;
		var priority = ReadDisappearPriority(ref platform, state, child,
			horizontal);
		return priority > NoDisappearPriority && priority <= hiddenPriority;
	}

	private static int CountCandidates<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var result = 0;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (child.IsNull || !IsShown(ref platform, state, child)) continue;
			if (ReadDisappearPriority(ref platform, state, child, horizontal) >
				NoDisappearPriority) result++;
		}
		return result;
	}

	private static int CountVisible<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, bool horizontal, int hiddenPriority)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var result = 0;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (child.IsNull || IsHidden(ref platform, state, child, horizontal,
				hiddenPriority)) continue;
			result++;
		}
		return result;
	}

	private static int FindNextPriority<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, bool horizontal, int after)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var next = int.MaxValue;
		var found = false;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (child.IsNull || !IsShown(ref platform, state, child)) continue;
			var priority = ReadDisappearPriority(ref platform, state, child,
				horizontal);
			if (priority > after && priority > NoDisappearPriority &&
				(!found || priority < next))
			{
				next = priority;
				found = true;
			}
		}
		return found ? next : NoDisappearPriority;
	}

	private static MuiGroupDisappearSelection ResolveAxisSelection<TPlatform>(
		ref TPlatform platform, APTR state, APTR group, int count, int columns,
		int rows, bool horizontal, int available, int spacing)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var selection = default(MuiGroupDisappearSelection);
		selection.Axis = horizontal ? MuiGroupDisappearAxis.Horizontal :
			MuiGroupDisappearAxis.Vertical;
		selection.Available = available < 0 ? 0 : available;
		selection.Spacing = spacing < 0 ? 0 : spacing;
		selection.HiddenPriority = NoDisappearPriority;
		selection.CandidateCount = CountCandidates(ref platform, state, group,
			count, horizontal);
		selection.VisibleCount = CountVisible(ref platform, state, group, count,
			horizontal, selection.HiddenPriority);
		while (selection.CandidateCount > 0 && selection.VisibleCount > 0 &&
			RequiredExtent(ref platform, state, group, count, columns, rows,
				horizontal, selection.HiddenPriority, selection.Spacing) >
			selection.Available)
		{
			var next = FindNextPriority(ref platform, state, group, count,
				horizontal, selection.HiddenPriority);
			if (next <= NoDisappearPriority) break;
			selection.HiddenPriority = next;
			selection.VisibleCount = CountVisible(ref platform, state, group,
				count, horizontal, selection.HiddenPriority);
		}
		return selection;
	}

	private static MuiGroupGridDisappearSelection ResolveDisappearSelection<TPlatform>(
		ref TPlatform platform, APTR state, APTR group, int count, int columns,
		int rows, int width, int height, int horizontalSpacing,
		int verticalSpacing)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var selection = default(MuiGroupGridDisappearSelection);
		selection.Horizontal = ResolveAxisSelection(ref platform, state, group,
			count, columns, rows, true, width, horizontalSpacing);
		selection.Vertical = ResolveAxisSelection(ref platform, state, group,
			count, columns, rows, false, height, verticalSpacing);
		return selection;
	}

	private static int RequiredExtent<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, int columns, int rows,
		bool horizontal, int hiddenPriority, int spacing)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var extent = 0;
		if (horizontal)
		{
			for (var column = 0; column < columns; column++)
			{
				var values = ColumnValues(ref platform, state, group, count,
					columns, rows, column, true, hiddenPriority);
				extent = AddExtent(extent, values.MinWidth);
			}
			if (columns > 1) extent = AddExtent(extent, spacing * (columns - 1));
		}
		else
		{
			for (var row = 0; row < rows; row++)
			{
				var values = RowValues(ref platform, state, group, count, columns,
					rows, row, true, hiddenPriority);
				extent = AddExtent(extent, values.MinHeight);
			}
			if (rows > 1) extent = AddExtent(extent, spacing * (rows - 1));
		}
		return extent;
	}

	private static int AddExtent(int value, int addition)
	{
		if (addition <= 0) return value;
		return value > int.MaxValue - addition ? int.MaxValue : value + addition;
	}

	private static short ClampMinimum(int value) => unchecked((short)(value >
		10000 ? 10000 : value));

	internal static MuiMinMaxValues ComputeMinMax<TPlatform>(
		ref TPlatform platform, APTR state, APTR group, MuiGroupGridSpec spec,
		int count) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var dimensions = ResolveDimensionPolicy(spec, count);
		var columns = dimensions.Columns;
		var rows = dimensions.Rows;
		var horizontalSpacing = MuiGroupSpacingCore.ResolveForMinMax(
			spec.HorizontalSpacing);
		var verticalSpacing = MuiGroupSpacingCore.ResolveForMinMax(
			spec.VerticalSpacing);
		var result = default(MuiMinMaxValues);
		var horizontalMaximum = default(MuiGroupMaximumSumState);
		var verticalMaximum = default(MuiGroupMaximumSumState);
		for (var column = 0; column < columns; column++)
		{
			var values = ColumnValues(ref platform, state, group, count,
				columns, rows, column);
			result.MinWidth = Add(result.MinWidth, values.MinWidth);
			horizontalMaximum.Include(values.MaxWidth);
			result.DefWidth = Add(result.DefWidth, values.DefWidth);
		}
		for (var row = 0; row < rows; row++)
		{
			var values = RowValues(ref platform, state, group, count, columns,
				rows, row);
			result.MinHeight = Add(result.MinHeight, values.MinHeight);
			verticalMaximum.Include(values.MaxHeight);
			result.DefHeight = Add(result.DefHeight, values.DefHeight);
		}
		if (columns > 1)
		{
			var gaps = horizontalSpacing * (columns - 1);
			result.MinWidth = Add(result.MinWidth, gaps);
			horizontalMaximum.IncludeGap(gaps);
			result.DefWidth = Add(result.DefWidth, gaps);
		}
		if (rows > 1)
		{
			var gaps = verticalSpacing * (rows - 1);
			result.MinHeight = Add(result.MinHeight, gaps);
			verticalMaximum.IncludeGap(gaps);
			result.DefHeight = Add(result.DefHeight, gaps);
		}
		result.MaxWidth = horizontalMaximum.Value;
		result.MaxHeight = verticalMaximum.Value;
		// A positive vertical/horizontal disappearance priority can satisfy the
		// corresponding minimum, while Def/Max retain the full grid aggregate.
		result.MinWidth = ClampMinimum(RequiredExtent(ref platform, state, group,
			count, columns, rows, true, int.MaxValue,
			horizontalSpacing));
		result.MinHeight = ClampMinimum(RequiredExtent(ref platform, state, group,
			count, columns, rows, false, int.MaxValue,
			verticalSpacing));
		return result;
	}

	internal static bool Layout<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, int left, int top, int width, int height,
		MuiGroupGridSpec spec, int count)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var dimensions = ResolveDimensionPolicy(spec, count);
		var columns = dimensions.Columns;
		var rows = dimensions.Rows;
		var spacing = MuiGroupSpacingCore.ResolveSelection(spec.HorizontalSpacing,
			spec.VerticalSpacing, width, height);
		var horizontalSpacing = spacing.Horizontal.Pixels;
		var verticalSpacing = spacing.Vertical.Pixels;
		var selection = ResolveDisappearSelection(ref platform, state, group,
			count, columns, rows, width, height, horizontalSpacing,
			verticalSpacing);
		var horizontalGaps = horizontalSpacing * (columns - 1);
		var verticalGaps = verticalSpacing * (rows - 1);
		var availableWidth = width - horizontalGaps;
		var availableHeight = height - verticalGaps;
		if (availableWidth < 0) availableWidth = 0;
		if (availableHeight < 0) availableHeight = 0;
		var equalWidth = spec.SameWidth == 0 ? default :
			ResolveEqualGridExtent(ref platform, state, group, count,
				columns == 0 ? availableWidth : availableWidth / columns,
				true);
		var equalHeight = spec.SameHeight == 0 ? default :
			ResolveEqualGridExtent(ref platform, state, group, count,
				rows == 0 ? availableHeight : availableHeight / rows,
				false);
		for (var index = 0; index < count; index++)
		{
			var row = index / columns;
			var column = index - row * columns;
			var child = MuiFamilyCore.GetChild(ref platform, state, group,
				index, APTR.Null);
			if (child.IsNull) return false;
			if (IsHidden(ref platform, state, child, true,
				selection.Horizontal.HiddenPriority) ||
				IsHidden(ref platform, state, child, false,
					selection.Vertical.HiddenPriority))
			{
				if (!MuiAreaLayoutCore.Layout(ref platform, state, child, left, top,
					0, 0)) return false;
				continue;
			}
			var childWidth = AxisExtent(ref platform, state, group, count,
				columns, rows, column, availableWidth, spec,
				0, true);
			var childHeight = AxisExtent(ref platform, state, group, count,
				columns, rows, row, availableHeight, spec,
				0, false);
			var childLeft = left + AxisOffset(ref platform, state, group, count,
				columns, rows, column, availableWidth, spec, horizontalSpacing,
				0, true);
			var childTop = top + AxisOffset(ref platform, state, group, count,
				columns, rows, row, availableHeight, spec, verticalSpacing,
				0, false);
			var minMax = MuiAreaLayoutCore.ComputeMinMax(ref platform, state, child);
			var placedWidth = Preferred(minMax.DefWidth, minMax.MinWidth,
				minMax.MaxWidth, childWidth);
			var placedHeight = Preferred(minMax.DefHeight, minMax.MinHeight,
				minMax.MaxHeight, childHeight);
			if (spec.SameWidth != 0 && equalWidth.VisibleCount > 0)
				placedWidth = EqualPreferred(equalWidth.EqualExtent,
					minMax.MinWidth, minMax.MaxWidth, childWidth);
			if (spec.SameHeight != 0 && equalHeight.VisibleCount > 0)
				placedHeight = EqualPreferred(equalHeight.EqualExtent,
					minMax.MinHeight, minMax.MaxHeight, childHeight);
			childLeft += Align(childWidth - placedWidth, spec.HorizontalCenter);
			childTop += Align(childHeight - placedHeight, spec.VerticalCenter);
			if (!MuiAreaLayoutCore.Layout(ref platform, state, child, childLeft,
				childTop, placedWidth, placedHeight)) return false;
		}
		return MuiAreaLayoutCore.Layout(ref platform, state, group, left, top,
			width, height);
	}

	private static MuiMinMaxValues ColumnValues<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, int columns, int rows, int column,
		bool filterDisappear = false, int hiddenPriority = 0)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var result = default(MuiMinMaxValues);
		var any = false;
		for (var row = 0; row < rows; row++)
		{
			var index = row * columns + column;
			if (index >= count) break;
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (filterDisappear && IsHidden(ref platform, state, child, true,
				hiddenPriority)) continue;
			var values = MuiAreaLayoutCore.ComputeMinMax(ref platform, state, child);
			if (!any)
			{
				result = values;
				any = true;
			}
			else
			{
				result.MinWidth = Larger(result.MinWidth, values.MinWidth);
				result.MaxWidth = SmallerMax(result.MaxWidth, values.MaxWidth);
				result.DefWidth = Larger(result.DefWidth, values.DefWidth);
			}
		}
		return result;
	}

	private static MuiMinMaxValues RowValues<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, int columns, int rows, int row,
		bool filterDisappear = false, int hiddenPriority = 0)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var result = default(MuiMinMaxValues);
		var any = false;
		for (var column = 0; column < columns; column++)
		{
			var index = row * columns + column;
			if (index >= count) break;
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (filterDisappear && IsHidden(ref platform, state, child, false,
				hiddenPriority)) continue;
			var values = MuiAreaLayoutCore.ComputeMinMax(ref platform, state, child);
			if (!any)
			{
				result = values;
				any = true;
			}
			else
			{
				result.MinHeight = Larger(result.MinHeight, values.MinHeight);
				result.MaxHeight = SmallerMax(result.MaxHeight, values.MaxHeight);
				result.DefHeight = Larger(result.DefHeight, values.DefHeight);
			}
		}
		return result;
	}

	private static int AxisExtent<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, int count, int columns, int rows, int axis, int available,
		MuiGroupGridSpec spec, int hiddenPriority, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var axes = horizontal ? columns : rows;
		if (axes <= 0 || axis < 0 || axis >= axes) return 0;
		if (axis == axes - 1) return available - AxisTotalBefore(ref platform,
			state, group, count, columns, rows, axes - 1, available, spec,
			hiddenPriority, horizontal);
		if ((horizontal && spec.SameWidth != 0) ||
			(!horizontal && spec.SameHeight != 0)) return available / axes;
		return ResolveAxisExtent(ref platform, state, group, count, columns, rows,
			axis, available, spec, hiddenPriority, horizontal);
	}

	private static int AxisOffset<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, int count, int columns, int rows, int axis, int available,
		MuiGroupGridSpec spec, int spacing, int hiddenPriority, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		AxisTotalBefore(ref platform, state, group, count, columns, rows, axis,
			available, spec, hiddenPriority, horizontal) + spacing * axis;

	private static int AxisTotalBefore<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, int columns, int rows, int axis,
		int available, MuiGroupGridSpec spec, int hiddenPriority, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var total = 0;
		for (var index = 0; index < axis; index++)
		{
			total += AxisExtentNonLast(ref platform, state, group, count,
				columns, rows, index, available, spec, hiddenPriority, horizontal);
		}
		return total;
	}

	private static int AxisExtentNonLast<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, int columns, int rows, int axis,
		int available, MuiGroupGridSpec spec, int hiddenPriority, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var axes = horizontal ? columns : rows;
		if ((horizontal && spec.SameWidth != 0) ||
			(!horizontal && spec.SameHeight != 0)) return available / axes;
		return ResolveAxisExtent(ref platform, state, group, count, columns, rows,
			axis, available, spec, hiddenPriority, horizontal);
	}

	// Resolve one Grid column or row through the same named allocation state as
	// an ordinary Group.  Aggregate min/max values come from the typed column /
	// row records and therefore preserve minimum floors, finite caps, and the
	// MorphOS zero-max unbounded sentinel without a parallel offset table.
	private static int ResolveAxisExtent<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, int columns, int rows, int axis,
		int available, MuiGroupGridSpec spec, int hiddenPriority, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var axes = horizontal ? columns : rows;
		if (axes <= 0 || axis < 0 || axis >= axes) return 0;
		var totalWeight = AxisWeightTotal(ref platform, state, group, count,
			columns, rows, axes, hiddenPriority, horizontal);
		var totalMinimum = AxisMinimumTotal(ref platform, state, group, count,
			columns, rows, axes, hiddenPriority, horizontal);
		// A rectangle smaller than the aggregate minimum cannot satisfy every
		// Grid axis. MorphOS still keeps the original cells in that case, so
		// fall back to weighted sharing instead of letting the first minimum
		// consume the whole axis and collapsing its siblings.
		var enforceMinimum = totalMinimum <= available;
		var allocation = MuiGroupAxisAllocationCore.Begin(available,
			totalWeight, enforceMinimum ? totalMinimum : 0);
		for (var index = 0; index <= axis; index++)
		{
			var values = AxisValues(ref platform, state, group, count, columns,
				rows, index, hiddenPriority, horizontal);
			var weight = AxisWeight(ref platform, state, group, count, columns,
				rows, index, hiddenPriority, horizontal);
			var minimum = horizontal ? values.MinWidth : values.MinHeight;
			var maximum = horizontal ? values.MaxWidth : values.MaxHeight;
			// A fixed-size child still occupies a normal Grid cell; its own
			// placement is centered inside that cell below. Do not collapse the
			// cell to the same fixed extent merely because min == max.
			if (maximum > 0 && maximum <= minimum) maximum = 0;
			if (!enforceMinimum) minimum = 0;
			MuiGroupAxisAllocationCore.Take(ref allocation, weight, minimum,
				maximum, index == axes - 1);
		}
		return allocation.Slot;
	}

	private static int AxisMinimumTotal<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, int columns, int rows, int axes,
		int hiddenPriority, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var total = 0;
		for (var axis = 0; axis < axes; axis++)
		{
			var values = AxisValues(ref platform, state, group, count, columns,
				rows, axis, hiddenPriority, horizontal);
			var minimum = horizontal ? values.MinWidth : values.MinHeight;
			if (minimum > 0)
				total = total > int.MaxValue - minimum ? int.MaxValue :
					total + minimum;
		}
		return total;
	}

	private static MuiMinMaxValues AxisValues<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, int columns, int rows, int axis,
		int hiddenPriority, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform => horizontal ?
			ColumnValues(ref platform, state, group, count, columns, rows, axis,
				true, hiddenPriority) :
			RowValues(ref platform, state, group, count, columns, rows, axis,
				true, hiddenPriority);

	private static uint AxisWeightTotal<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, int columns, int rows, int axes,
		int hiddenPriority, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		uint total = 0;
		for (var axis = 0; axis < axes; axis++) total += AxisWeight(ref platform,
			state, group, count, columns, rows, axis, hiddenPriority, horizontal);
		return total == 0 ? (uint)axes : total;
	}

	private static uint AxisWeight<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, int count, int columns, int rows, int axis, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
		=> AxisWeight(ref platform, state, group, count, columns, rows, axis, 0,
			horizontal);

	private static uint AxisWeight<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, int count, int columns, int rows, int axis,
		int hiddenPriority, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		uint total = 0;
		if (horizontal)
		{
			for (var row = 0; row < rows; row++)
			{
				var index = row * columns + axis;
				if (index >= count) break;
				var child = MuiFamilyCore.GetChild(ref platform, state, group,
					index, APTR.Null);
				if (child.IsNull || IsHidden(ref platform, state, child, true,
					hiddenPriority)) continue;
				total += NormalizedWeight(MuiAreaLayoutCore.HorizontalWeight(
					ref platform, state, child));
			}
		}
		else
		{
			for (var column = 0; column < columns; column++)
			{
				var index = axis * columns + column;
				if (index >= count) break;
				var child = MuiFamilyCore.GetChild(ref platform, state, group,
					index, APTR.Null);
				if (child.IsNull || IsHidden(ref platform, state, child, false,
					hiddenPriority)) continue;
				total += NormalizedWeight(MuiAreaLayoutCore.VerticalWeight(
					ref platform, state, child));
			}
		}
		return total;
	}

	private static uint NormalizedWeight(uint value) => value == 0 ? 1u : value;

	private static MuiGroupEqualExtentSelection ResolveEqualGridExtent<TPlatform>(
		ref TPlatform platform, APTR state, APTR group, int count, int available,
		bool horizontal) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var selection = default(MuiGroupEqualExtentSelection);
		selection.Available = available < 0 ? 0 : available;
		var finiteMaximum = false;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (child.IsNull || !IsShown(ref platform, state, child)) continue;
			var values = MuiAreaLayoutCore.ComputeMinMax(ref platform, state,
				child);
			var minimum = horizontal ? values.MinWidth : values.MinHeight;
			var maximum = horizontal ? values.MaxWidth : values.MaxHeight;
			var preferred = horizontal ? values.DefWidth : values.DefHeight;
			if (minimum > selection.MinimumExtent)
				selection.MinimumExtent = minimum;
			if (preferred > selection.DefaultExtent)
				selection.DefaultExtent = preferred;
			if (maximum > 0 && (!finiteMaximum ||
				maximum < selection.MaximumExtent))
			{
				selection.MaximumExtent = maximum;
				finiteMaximum = true;
			}
			selection.VisibleCount++;
		}
		selection.HasFiniteMaximum = finiteMaximum ? 1u : 0u;
		if (selection.VisibleCount == 0) return selection;
		var equal = selection.Available;
		if (equal < selection.MinimumExtent) equal = selection.MinimumExtent;
		if (finiteMaximum && equal > selection.MaximumExtent)
			equal = selection.MaximumExtent;
		selection.EqualExtent = equal < 0 ? 0 : equal;
		selection.TailExtent = selection.EqualExtent;
		return selection;
	}

	private static int EqualPreferred(int equal, short minimum, short maximum,
		int cell)
	{
		var value = equal;
		if (value < minimum) value = minimum;
		if (maximum > 0 && value > maximum) value = maximum;
		if (value < 0) value = 0;
		return value > cell ? cell : value;
	}

	private static int Preferred(short def, short min, short max, int cell)
	{
		// A child with no preferred or minimum extent is free to occupy its
		// complete Grid cell unless it declares a finite maximum. Area policy
		// publishes an unbounded maximum (the 10000 sentinel) when no explicit
		// maximum was supplied, so only a positive bound narrows this path.
		if (def == 0 && min == 0)
			return max > 0 && max < cell ? max : cell;
		var value = def == 0 ? min : def;
		if (value < min) value = min;
		if (max != 0 && value > max) value = max;
		if (value < 0) value = 0;
		return value > cell ? cell : value;
	}

	private static int Align(int free, uint center) => center == 0 ? 0 :
		center == 2 ? free : free / 2;

	internal static MuiGroupGridDimensionPolicy ResolveDimensionPolicy(
		MuiGroupGridSpec spec, int count)
	{
		var policy = default(MuiGroupGridDimensionPolicy);
		policy.Count = count < 0 ? 0 : count;
		policy.ExplicitColumns = spec.Columns;
		policy.ExplicitRows = spec.Rows;
		var columns = (int)spec.Columns;
		var rows = (int)spec.Rows;
		policy.Divisible = 1;
		if (columns == 0 && rows == 0)
		{
			columns = 1;
			rows = policy.Count;
		}
		else if (columns == 0)
		{
			rows = rows > policy.Count ? policy.Count : rows;
			columns = rows == 0 ? 1 : (policy.Count + rows - 1) / rows;
			if (policy.Count > 0 && policy.Count % rows != 0)
			{
				policy.Divisible = 0;
				policy.Remainder = policy.Count % rows;
			}
		}
		else
		{
			columns = columns > policy.Count ? policy.Count : columns;
			var requiredRows = columns == 0 ? 1 :
				(policy.Count + columns - 1) / columns;
			rows = rows < requiredRows ? requiredRows : rows;
			if (policy.Count > 0 && policy.Count % columns != 0)
			{
				policy.Divisible = 0;
				policy.Remainder = policy.Count % columns;
			}
		}
		if (columns < 1) columns = 1;
		if (rows < 1) rows = 1;
		if (policy.Count > 0 && spec.Columns != 0 &&
			policy.Count % (int)spec.Columns != 0)
		{
			policy.Divisible = 0;
			if (policy.Remainder == 0)
				policy.Remainder = policy.Count % (int)spec.Columns;
		}
		if (policy.Count > 0 && spec.Rows != 0 &&
			policy.Count % (int)spec.Rows != 0)
		{
			policy.Divisible = 0;
			if (policy.Remainder == 0)
				policy.Remainder = policy.Count % (int)spec.Rows;
		}
		policy.Columns = columns;
		policy.Rows = rows;
		return policy;
	}

	private static void ReadValue<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, uint attribute, uint fallback, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
			attribute, out value)) value = fallback;
	}

	private static uint ClampAxis(uint value) => value > MaximumAxis ?
		MaximumAxis : value;

	private static uint NormalizeSpacing(uint value) =>
		MuiGroupSpacingCore.NormalizeRaw(value);

	private static uint ClampCenter(uint value) => value > 2 ? 1 : value;

	private static short Larger(short left, short right) => left > right ? left :
		right;

	private static short SmallerMax(short left, short right)
	{
		if (left == 0) return right;
		if (right == 0) return left;
		return left < right ? left : right;
	}

	private static short Add(short value, int addition)
	{
		var result = (int)value + addition;
		return unchecked((short)(result > 10000 ? 10000 : result));
	}
}
