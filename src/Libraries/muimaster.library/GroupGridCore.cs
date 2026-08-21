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
	private static bool TryResolve(MuiGroupGridSpecField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiGroupGridSpecField.Columns => 0,
			MuiGroupGridSpecField.Rows => 4,
			MuiGroupGridSpecField.HorizontalSpacing => 8,
			MuiGroupGridSpecField.VerticalSpacing => 12,
			MuiGroupGridSpecField.SameWidth => 16,
			MuiGroupGridSpecField.SameHeight => 20,
			MuiGroupGridSpecField.HorizontalCenter => 24,
			MuiGroupGridSpecField.VerticalCenter => 28,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupGridSpecFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupGridSpecField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiGroupGridSpecFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupGridSpecField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGroupGridSpecFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiGroupGridSpecCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupGridSpec spec)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupGridSpec.Size)) return false;
		return MuiGroupGridSpecFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGroupGridSpecField.Columns, spec.Columns) &&
			MuiGroupGridSpecFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupGridSpecField.Rows, spec.Rows) &&
			MuiGroupGridSpecFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupGridSpecField.HorizontalSpacing, spec.HorizontalSpacing) &&
			MuiGroupGridSpecFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupGridSpecField.VerticalSpacing, spec.VerticalSpacing) &&
			MuiGroupGridSpecFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupGridSpecField.SameWidth, spec.SameWidth) &&
			MuiGroupGridSpecFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupGridSpecField.SameHeight, spec.SameHeight) &&
			MuiGroupGridSpecFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupGridSpecField.HorizontalCenter, spec.HorizontalCenter) &&
			MuiGroupGridSpecFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupGridSpecField.VerticalCenter, spec.VerticalCenter);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupGridSpec spec)
		where TPlatform : struct, IMuiGuestMemory
	{
		spec = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupGridSpec.Size)) return false;
		if (!MuiGroupGridSpecFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiGroupGridSpecField.Columns, out spec.Columns) ||
			!MuiGroupGridSpecFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupGridSpecField.Rows, out spec.Rows) ||
			!MuiGroupGridSpecFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupGridSpecField.HorizontalSpacing, out spec.HorizontalSpacing) ||
			!MuiGroupGridSpecFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupGridSpecField.VerticalSpacing, out spec.VerticalSpacing) ||
			!MuiGroupGridSpecFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupGridSpecField.SameWidth, out spec.SameWidth) ||
			!MuiGroupGridSpecFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupGridSpecField.SameHeight, out spec.SameHeight) ||
			!MuiGroupGridSpecFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupGridSpecField.HorizontalCenter, out spec.HorizontalCenter) ||
			!MuiGroupGridSpecFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupGridSpecField.VerticalCenter, out spec.VerticalCenter))
			return false;
		return true;
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
		var source = attribute == Spacing ? Spacing : attribute;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
			source, out _) &&
			(attribute != HorizontalSpacing && attribute != VerticalSpacing ||
				!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
					Spacing, out _))) return false;
		var spec = Read(ref platform, state, group);
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
		if (PublishState(ref platform, state, group, out var record))
			return ToSpec(record);
		return ReadRawSpec(ref platform, state, group);
	}

	internal static bool TryGetStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, out MuiGroupGridStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, group,
			StateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, group, StateKey) !=
			unchecked((int)MuiGroupGridStateRecord.Size)) return false;
		return MuiGroupGridStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool PublishState<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, out MuiGroupGridStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, group,
			StateKey);
		if (TryGetStateRecord(ref platform, state, group, out value))
		{
			FillState(ref platform, state, group, ref value);
			return MuiGroupGridStateRecordCodec.Write(ref platform, block, value);
		}

		value = default;
		value.Magic = MuiGroupGridStateRecord.Cookie;
		FillState(ref platform, state, group, ref value);
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

	private static void FillState<TPlatform>(ref TPlatform platform, APTR state,
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
		value.HorizontalSpacing = ClampSpacing(value.HorizontalSpacing);
		value.VerticalSpacing = ClampSpacing(value.VerticalSpacing);
		value.HorizontalCenter = ClampCenter(value.HorizontalCenter);
		value.VerticalCenter = ClampCenter(value.VerticalCenter);
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
		result.HorizontalSpacing = ClampSpacing(result.HorizontalSpacing);
		result.VerticalSpacing = ClampSpacing(result.VerticalSpacing);
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

	internal static MuiMinMaxValues ComputeMinMax<TPlatform>(
		ref TPlatform platform, APTR state, APTR group, MuiGroupGridSpec spec,
		int count) where TPlatform : struct, IMuiHeadlessPlatform
	{
		ResolveDimensions(spec, count, out var columns, out var rows);
		var result = default(MuiMinMaxValues);
		for (var column = 0; column < columns; column++)
		{
			var values = ColumnValues(ref platform, state, group, count,
				columns, rows, column);
			result.MinWidth = Add(result.MinWidth, values.MinWidth);
			result.MaxWidth = Add(result.MaxWidth, values.MaxWidth);
			result.DefWidth = Add(result.DefWidth, values.DefWidth);
		}
		for (var row = 0; row < rows; row++)
		{
			var values = RowValues(ref platform, state, group, count, columns,
				rows, row);
			result.MinHeight = Add(result.MinHeight, values.MinHeight);
			result.MaxHeight = Add(result.MaxHeight, values.MaxHeight);
			result.DefHeight = Add(result.DefHeight, values.DefHeight);
		}
		if (columns > 1)
		{
			var gaps = (int)spec.HorizontalSpacing * (columns - 1);
			result.MinWidth = Add(result.MinWidth, gaps);
			result.MaxWidth = Add(result.MaxWidth, gaps);
			result.DefWidth = Add(result.DefWidth, gaps);
		}
		if (rows > 1)
		{
			var gaps = (int)spec.VerticalSpacing * (rows - 1);
			result.MinHeight = Add(result.MinHeight, gaps);
			result.MaxHeight = Add(result.MaxHeight, gaps);
			result.DefHeight = Add(result.DefHeight, gaps);
		}
		return result;
	}

	internal static bool Layout<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, int left, int top, int width, int height,
		MuiGroupGridSpec spec, int count)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		ResolveDimensions(spec, count, out var columns, out var rows);
		var horizontalGaps = (int)spec.HorizontalSpacing * (columns - 1);
		var verticalGaps = (int)spec.VerticalSpacing * (rows - 1);
		var availableWidth = width - horizontalGaps;
		var availableHeight = height - verticalGaps;
		if (availableWidth < 0) availableWidth = 0;
		if (availableHeight < 0) availableHeight = 0;
		for (var index = 0; index < count; index++)
		{
			var row = index / columns;
			var column = index - row * columns;
			var child = MuiFamilyCore.GetChild(ref platform, state, group,
				index, APTR.Null);
			if (child.IsNull) return false;
			var childWidth = AxisExtent(ref platform, state, group, count,
				columns, rows, column, availableWidth, spec, true);
			var childHeight = AxisExtent(ref platform, state, group, count,
				columns, rows, row, availableHeight, spec, false);
			var childLeft = left + AxisOffset(ref platform, state, group, count,
				columns, rows, column, availableWidth, spec, true);
			var childTop = top + AxisOffset(ref platform, state, group, count,
				columns, rows, row, availableHeight, spec, false);
			var minMax = MuiAreaLayoutCore.ComputeMinMax(ref platform, state, child);
			var placedWidth = Preferred(minMax.DefWidth, minMax.MinWidth,
				minMax.MaxWidth, childWidth);
			var placedHeight = Preferred(minMax.DefHeight, minMax.MinHeight,
				minMax.MaxHeight, childHeight);
			childLeft += Align(childWidth - placedWidth, spec.HorizontalCenter);
			childTop += Align(childHeight - placedHeight, spec.VerticalCenter);
			if (!MuiAreaLayoutCore.Layout(ref platform, state, child, childLeft,
				childTop, placedWidth, placedHeight)) return false;
		}
		return MuiAreaLayoutCore.Layout(ref platform, state, group, left, top,
			width, height);
	}

	private static MuiMinMaxValues ColumnValues<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, int columns, int rows, int column)
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
		APTR state, APTR group, int count, int columns, int rows, int row)
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
		MuiGroupGridSpec spec, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var axes = horizontal ? columns : rows;
		if (axis == axes - 1) return available - AxisTotalBefore(ref platform,
			state, group, count, columns, rows, axes - 1, available, spec,
			horizontal);
		if ((horizontal && spec.SameWidth != 0) ||
			(!horizontal && spec.SameHeight != 0)) return available / axes;
		var totalWeight = AxisWeightTotal(ref platform, state, group, count,
			columns, rows, axes, horizontal);
		var weight = AxisWeight(ref platform, state, group, count, columns, rows,
			axis, horizontal);
		return (int)((uint)available * weight / totalWeight);
	}

	private static int AxisOffset<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, int count, int columns, int rows, int axis, int available,
		MuiGroupGridSpec spec, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		AxisTotalBefore(ref platform, state, group, count, columns, rows, axis,
			available, spec, horizontal) + (horizontal ? (int)spec.HorizontalSpacing :
			(int)spec.VerticalSpacing) * axis;

	private static int AxisTotalBefore<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, int columns, int rows, int axis,
		int available, MuiGroupGridSpec spec, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var total = 0;
		for (var index = 0; index < axis; index++)
		{
			total += AxisExtentNonLast(ref platform, state, group, count,
				columns, rows, index, available, spec, horizontal);
		}
		return total;
	}

	private static int AxisExtentNonLast<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, int columns, int rows, int axis,
		int available, MuiGroupGridSpec spec, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var axes = horizontal ? columns : rows;
		if ((horizontal && spec.SameWidth != 0) ||
			(!horizontal && spec.SameHeight != 0)) return available / axes;
		var totalWeight = AxisWeightTotal(ref platform, state, group, count,
			columns, rows, axes, horizontal);
		var weight = AxisWeight(ref platform, state, group, count, columns, rows,
			axis, horizontal);
		return (int)((uint)available * weight / totalWeight);
	}

	private static uint AxisWeightTotal<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, int columns, int rows, int axes,
		bool horizontal) where TPlatform : struct, IMuiHeadlessPlatform
	{
		uint total = 0;
		for (var axis = 0; axis < axes; axis++) total += AxisWeight(ref platform,
			state, group, count, columns, rows, axis, horizontal);
		return total == 0 ? (uint)axes : total;
	}

	private static uint AxisWeight<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, int count, int columns, int rows, int axis, bool horizontal)
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
				total += NormalizedWeight(MuiAreaLayoutCore.VerticalWeight(
					ref platform, state, child));
			}
		}
		return total;
	}

	private static uint NormalizedWeight(uint value) => value == 0 ? 1u : value;

	private static int Preferred(short def, short min, short max, int cell)
	{
		if (def == 0 && min == 0 && max == 0) return cell;
		var value = def == 0 ? min : def;
		if (value < min) value = min;
		if (max != 0 && value > max) value = max;
		if (value < 0) value = 0;
		return value > cell ? cell : value;
	}

	private static int Align(int free, uint center) => center == 0 ? 0 :
		center == 2 ? free : free / 2;

	private static void ResolveDimensions(MuiGroupGridSpec spec, int count,
		out int columns, out int rows)
	{
		columns = (int)spec.Columns;
		rows = (int)spec.Rows;
		if (columns == 0 && rows == 0)
		{
			columns = 1;
			rows = count;
		}
		else if (columns == 0)
		{
			rows = rows > count ? count : rows;
			columns = (count + rows - 1) / rows;
		}
		else
		{
			columns = columns > count ? count : columns;
			var requiredRows = (count + columns - 1) / columns;
			rows = rows < requiredRows ? requiredRows : rows;
		}
		if (columns < 1) columns = 1;
		if (rows < 1) rows = 1;
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

	private static uint ClampSpacing(uint value) => value > 10000 ? 10000 : value;

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
