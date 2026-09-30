/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_Menuitem_Trigger is documented as a pointer to Intuition's packed
// struct MenuItem. The public SDK already owns the semantic MenuItem value;
// this adapter is the only place that knows how that value crosses the
// big-endian guest-memory boundary. Keep the wire shape as one named record so
// menu behavior does not depend on consumer-side offsets.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiMenuItemRecord
{
	internal const uint Size = 34;
	internal const uint NextItemOffset = 0;
	internal const uint LeftEdgeOffset = 4;
	internal const uint TopEdgeOffset = 6;
	internal const uint WidthOffset = 8;
	internal const uint HeightOffset = 10;
	internal const uint FlagsOffset = 12;
	internal const uint MutualExcludeOffset = 14;
	internal const uint ItemFillOffset = 18;
	internal const uint SelectFillOffset = 22;
	internal const uint CommandOffset = 26;
	internal const uint PaddingOffset = 27;
	internal const uint SubItemOffset = 28;
	internal const uint NextSelectOffset = 32;

	internal APTR NextItem;
	internal short LeftEdge;
	internal short TopEdge;
	internal short Width;
	internal short Height;
	internal ushort Flags;
	internal int MutualExclude;
	internal APTR ItemFill;
	internal APTR SelectFill;
	internal sbyte Command;
	internal byte Padding;
	internal APTR SubItem;
	internal ushort NextSelect;
}

internal enum MuiMenuItemField : byte
{
	NextItem,
	LeftEdge,
	TopEdge,
	Width,
	Height,
	Flags,
	MutualExclude,
	ItemFill,
	SelectFill,
	Command,
	Padding,
	SubItem,
	NextSelect,
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiMenuItemFieldCursor
{
	internal APTR Record;
	internal MuiMenuItemField Field;
}

internal static class MuiMenuItemRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiMenuItemRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiMenuItemRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var nextItem) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var leftEdge) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var topEdge) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var width) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var height) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var flags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var mutualExclude) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var itemFill) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var selectFill) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var command) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var padding) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var subItem) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var nextSelect)) return false;
		value.NextItem = APTR.FromPointer(nextItem);
		value.LeftEdge = unchecked((short)leftEdge);
		value.TopEdge = unchecked((short)topEdge);
		value.Width = unchecked((short)width);
		value.Height = unchecked((short)height);
		value.Flags = flags;
		value.MutualExclude = unchecked((int)mutualExclude);
		value.ItemFill = APTR.FromPointer(itemFill);
		value.SelectFill = APTR.FromPointer(selectFill);
		value.Command = unchecked((sbyte)command);
		value.Padding = padding;
		value.SubItem = APTR.FromPointer(subItem);
		value.NextSelect = nextSelect;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiMenuItemRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiMenuItemRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.NextItem.Raw) &&
		MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
			unchecked((ushort)value.LeftEdge)) &&
		MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
			unchecked((ushort)value.TopEdge)) &&
		MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
			unchecked((ushort)value.Width)) &&
		MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
			unchecked((ushort)value.Height)) &&
		MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
			value.Flags) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.MutualExclude)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ItemFill.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.SelectFill.Raw) &&
		MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
			unchecked((byte)value.Command)) &&
		MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
			value.Padding) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.SubItem.Raw) &&
		MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
			value.NextSelect) && MuiGuestStructCursor.IsComplete(cursor);
}

internal static class MuiMenuItemRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiMenuItemField field,
		out uint index, out uint fieldSize)
	{
		switch (field)
		{
			case MuiMenuItemField.NextItem: index = 0; fieldSize = 4; return true;
			case MuiMenuItemField.LeftEdge: index = 1; fieldSize = 2; return true;
			case MuiMenuItemField.TopEdge: index = 2; fieldSize = 2; return true;
			case MuiMenuItemField.Width: index = 3; fieldSize = 2; return true;
			case MuiMenuItemField.Height: index = 4; fieldSize = 2; return true;
			case MuiMenuItemField.Flags: index = 5; fieldSize = 2; return true;
			case MuiMenuItemField.MutualExclude: index = 6; fieldSize = 4; return true;
			case MuiMenuItemField.ItemFill: index = 7; fieldSize = 4; return true;
			case MuiMenuItemField.SelectFill: index = 8; fieldSize = 4; return true;
			case MuiMenuItemField.Command: index = 9; fieldSize = 1; return true;
			case MuiMenuItemField.Padding: index = 10; fieldSize = 1; return true;
			case MuiMenuItemField.SubItem: index = 11; fieldSize = 4; return true;
			case MuiMenuItemField.NextSelect: index = 12; fieldSize = 2; return true;
		}
		index = uint.MaxValue;
		fieldSize = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiMenuItemFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMenuItemFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index,
			out var selectedSize) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiMenuItemRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			var currentSize = current == 0 || current >= 6 && current <= 8 ||
				current == 11 ? 4u : current <= 5 || current == 12 ? 2u : 1u;
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				currentSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = selectedSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiMenuItemRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		switch (field)
		{
			case MuiMenuItemField.NextItem: value = state.NextItem.Raw; return true;
			case MuiMenuItemField.MutualExclude: value = unchecked((uint)state.MutualExclude); return true;
			case MuiMenuItemField.ItemFill: value = state.ItemFill.Raw; return true;
			case MuiMenuItemField.SelectFill: value = state.SelectFill.Raw; return true;
			case MuiMenuItemField.SubItem: value = state.SubItem.Raw; return true;
		}
		return false;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiMenuItemRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiMenuItemField.LeftEdge) value = unchecked((ushort)state.LeftEdge);
		else if (field == MuiMenuItemField.TopEdge) value = unchecked((ushort)state.TopEdge);
		else if (field == MuiMenuItemField.Width) value = unchecked((ushort)state.Width);
		else if (field == MuiMenuItemField.Height) value = unchecked((ushort)state.Height);
		else if (field == MuiMenuItemField.Flags) value = state.Flags;
		else if (field == MuiMenuItemField.NextSelect) value = state.NextSelect;
		else return false;
		return true;
	}

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiMenuItemRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiMenuItemField.Command) value = unchecked((byte)state.Command);
		else if (field == MuiMenuItemField.Padding) value = state.Padding;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiMenuItemRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiMenuItemField.NextItem) state.NextItem = APTR.FromPointer(value);
		else if (field == MuiMenuItemField.MutualExclude) state.MutualExclude = unchecked((int)value);
		else if (field == MuiMenuItemField.ItemFill) state.ItemFill = APTR.FromPointer(value);
		else if (field == MuiMenuItemField.SelectFill) state.SelectFill = APTR.FromPointer(value);
		else if (field == MuiMenuItemField.SubItem) state.SubItem = APTR.FromPointer(value);
		else return false;
		return MuiMenuItemRecordCodec.WriteStructural(ref platform, record, state);
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiMenuItemRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiMenuItemField.LeftEdge) state.LeftEdge = unchecked((short)value);
		else if (field == MuiMenuItemField.TopEdge) state.TopEdge = unchecked((short)value);
		else if (field == MuiMenuItemField.Width) state.Width = unchecked((short)value);
		else if (field == MuiMenuItemField.Height) state.Height = unchecked((short)value);
		else if (field == MuiMenuItemField.Flags) state.Flags = value;
		else if (field == MuiMenuItemField.NextSelect) state.NextSelect = value;
		else return false;
		return MuiMenuItemRecordCodec.WriteStructural(ref platform, record, state);
	}

	internal static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiMenuItemRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiMenuItemField.Command) state.Command = unchecked((sbyte)value);
		else if (field == MuiMenuItemField.Padding) state.Padding = value;
		else return false;
		return MuiMenuItemRecordCodec.WriteStructural(ref platform, record, state);
	}
}

internal static class MuiMenuItemFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMenuItemFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMenuItemFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMenuItemRecordMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);
}

internal static class MuiMenuItemMemoryCodec
{
	internal static bool IsMapped<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory =>
		!address.IsNull && platform.IsMapped(address, MuiMenuItemRecord.Size);

	private static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiMenuItemRecordMemoryCodec.TryGetAddress(ref platform, record, field,
			out address);

	private static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiMenuItemRecordMemoryCodec.TryReadUInt32(ref platform, record, field,
			out value);

	private static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiMenuItemRecordMemoryCodec.TryReadUInt16(ref platform, record, field,
			out value);

	private static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiMenuItemRecordMemoryCodec.TryReadUInt8(ref platform, record, field,
			out value);

	private static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiMenuItemRecordMemoryCodec.TryWriteUInt32(ref platform, record, field,
			value);

	private static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiMenuItemRecordMemoryCodec.TryWriteUInt16(ref platform, record, field,
			value);

	private static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiMenuItemRecordMemoryCodec.TryWriteUInt8(ref platform, record, field,
			value);

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MenuItem value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiMenuItemRecordCodec.TryReadStructural(ref platform, address,
			out var record)) return false;
		value.NextItem = record.NextItem;
		value.LeftEdge = record.LeftEdge;
		value.TopEdge = record.TopEdge;
		value.Width = record.Width;
		value.Height = record.Height;
		value.Flags = (MenuItemFlags)record.Flags;
		value.MutualExclude = record.MutualExclude;
		value.ItemFill = record.ItemFill;
		value.SelectFill = record.SelectFill;
		value.Command = record.Command;
		value.SubItem = record.SubItem;
		value.NextSelect = record.NextSelect;
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MenuItem value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var record = default(MuiMenuItemRecord);
		record.NextItem = value.NextItem;
		record.LeftEdge = value.LeftEdge;
		record.TopEdge = value.TopEdge;
		record.Width = value.Width;
		record.Height = value.Height;
		record.Flags = (ushort)value.Flags;
		record.MutualExclude = value.MutualExclude;
		record.ItemFill = value.ItemFill;
		record.SelectFill = value.SelectFill;
		record.Command = value.Command;
		record.Padding = 0;
		record.SubItem = value.SubItem;
		record.NextSelect = value.NextSelect;
		return MuiMenuItemRecordCodec.WriteStructural(ref platform, address, record);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MenuItem value) where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MenuItem value) where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}

// IntuiText is the composed text node stored immediately after the MenuItem
// in trigger storage. Keep its packed mixed-size wire shape named as well;
// callers still receive the SDK IntuiText semantic value.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiIntuiTextRecord
{
	internal const uint Size = 20;
	internal const uint FrontPenOffset = 0;
	internal const uint BackPenOffset = 1;
	internal const uint DrawModeOffset = 2;
	internal const uint PaddingOffset = 3;
	internal const uint LeftEdgeOffset = 4;
	internal const uint TopEdgeOffset = 6;
	internal const uint FontOffset = 8;
	internal const uint TextOffset = 12;
	internal const uint NextTextOffset = 16;

	internal byte FrontPen;
	internal byte BackPen;
	internal byte DrawMode;
	internal byte Padding;
	internal short LeftEdge;
	internal short TopEdge;
	internal APTR Font;
	internal APTR Text;
	internal APTR NextText;
}

internal static class MuiIntuiTextRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiIntuiTextRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiIntuiTextRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var frontPen) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var backPen) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var drawMode) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var padding) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var leftEdge) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var topEdge) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var font) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var text) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var nextText)) return false;
		value.FrontPen = frontPen;
		value.BackPen = backPen;
		value.DrawMode = drawMode;
		value.Padding = padding;
		value.LeftEdge = unchecked((short)leftEdge);
		value.TopEdge = unchecked((short)topEdge);
		value.Font = APTR.FromPointer(font);
		value.Text = APTR.FromPointer(text);
		value.NextText = APTR.FromPointer(nextText);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiIntuiTextRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiIntuiTextRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
			value.FrontPen) &&
		MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
			value.BackPen) &&
		MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
			value.DrawMode) &&
		MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
			value.Padding) &&
		MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
			unchecked((ushort)value.LeftEdge)) &&
		MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
			unchecked((ushort)value.TopEdge)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Font.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Text.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.NextText.Raw) && MuiGuestStructCursor.IsComplete(cursor);
}

internal static class MuiIntuiTextRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiIntuiTextField field,
		out uint index, out uint fieldSize)
	{
		switch (field)
		{
			case MuiIntuiTextField.FrontPen: index = 0; fieldSize = 1; return true;
			case MuiIntuiTextField.BackPen: index = 1; fieldSize = 1; return true;
			case MuiIntuiTextField.DrawMode: index = 2; fieldSize = 1; return true;
			case MuiIntuiTextField.Padding: index = 3; fieldSize = 1; return true;
			case MuiIntuiTextField.LeftEdge: index = 4; fieldSize = 2; return true;
			case MuiIntuiTextField.TopEdge: index = 5; fieldSize = 2; return true;
			case MuiIntuiTextField.Font: index = 6; fieldSize = 4; return true;
			case MuiIntuiTextField.Text: index = 7; fieldSize = 4; return true;
			case MuiIntuiTextField.NextText: index = 8; fieldSize = 4; return true;
		}
		index = uint.MaxValue;
		fieldSize = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiIntuiTextFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiIntuiTextFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index,
			out var selectedSize) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiIntuiTextRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			var currentSize = current <= 3 ? 1u : current <= 5 ? 2u : 4u;
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				currentSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = selectedSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiIntuiTextRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiIntuiTextField.Font) value = state.Font.Raw;
		else if (field == MuiIntuiTextField.Text) value = state.Text.Raw;
		else if (field == MuiIntuiTextField.NextText) value = state.NextText.Raw;
		else return false;
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiIntuiTextRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiIntuiTextField.LeftEdge) value = unchecked((ushort)state.LeftEdge);
		else if (field == MuiIntuiTextField.TopEdge) value = unchecked((ushort)state.TopEdge);
		else return false;
		return true;
	}

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiIntuiTextRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiIntuiTextField.FrontPen) value = state.FrontPen;
		else if (field == MuiIntuiTextField.BackPen) value = state.BackPen;
		else if (field == MuiIntuiTextField.DrawMode) value = state.DrawMode;
		else if (field == MuiIntuiTextField.Padding) value = state.Padding;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiIntuiTextRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiIntuiTextField.Font) state.Font = APTR.FromPointer(value);
		else if (field == MuiIntuiTextField.Text) state.Text = APTR.FromPointer(value);
		else if (field == MuiIntuiTextField.NextText) state.NextText = APTR.FromPointer(value);
		else return false;
		return MuiIntuiTextRecordCodec.WriteStructural(ref platform, record, state);
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiIntuiTextRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiIntuiTextField.LeftEdge) state.LeftEdge = unchecked((short)value);
		else if (field == MuiIntuiTextField.TopEdge) state.TopEdge = unchecked((short)value);
		else return false;
		return MuiIntuiTextRecordCodec.WriteStructural(ref platform, record, state);
	}

	internal static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiIntuiTextRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiIntuiTextField.FrontPen) state.FrontPen = value;
		else if (field == MuiIntuiTextField.BackPen) state.BackPen = value;
		else if (field == MuiIntuiTextField.DrawMode) state.DrawMode = value;
		else if (field == MuiIntuiTextField.Padding) state.Padding = value;
		else return false;
		return MuiIntuiTextRecordCodec.WriteStructural(ref platform, record, state);
	}
}

// The MenuItem projection owns one textual fill record. Keeping the fixed
// prefix as a composed struct documents the exact relationship between the
// published MenuItem and its IntuiText; the trailing C-string capacity is
// handled by the storage codec below.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMenuItemTriggerStoragePrefix
{
	internal const uint Size = MuiMenuItemRecord.Size + IntuiText.Size;
	internal MuiMenuItemRecord Menu;
	internal IntuiText Text;
}

// Bounded byte range used while materializing the caller-visible title.  The
// range is represented as a value type so the copy remains freestanding and
// does not expose guest-address arithmetic to the projection logic.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMenuItemTriggerStringByteCursor
{
	internal const uint MaximumLength = MuiMenuSpecialistLayout.MaximumString + 1;
	internal APTR Base;
	internal uint Index;
	internal uint Length;
}

internal static class MuiMenuItemTriggerStringByteCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMenuItemTriggerStringByteCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Length > MuiMenuItemTriggerStringByteCursor.MaximumLength)
			return false;
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Base;
		shared.Index = cursor.Index;
		shared.Limit = cursor.Length;
		return MuiCStringByteCursorCodec.TryGetAddress(ref platform, shared,
			out address);
	}

	internal static bool TryReadByte<TPlatform>(ref TPlatform platform,
		MuiMenuItemTriggerStringByteCursor cursor, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt8(address, 0);
		return true;
	}

	internal static bool TryWriteByte<TPlatform>(ref TPlatform platform,
		MuiMenuItemTriggerStringByteCursor cursor, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt8(address, 0, value);
		return true;
	}
}

internal static class MuiMenuItemTriggerStorageCodec
{
	internal const uint StringCapacity = MuiMenuSpecialistLayout.MaximumString + 1;
	internal const uint Size = MuiMenuItemTriggerStoragePrefix.Size +
		StringCapacity;

	internal static bool IsMapped<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory =>
		!address.IsNull && (address.Raw & 1u) == 0 && platform.IsMapped(address,
			Size);

	internal static bool TryGetTextAddress<TPlatform>(ref TPlatform platform,
		APTR address, out APTR textAddress)
		where TPlatform : struct, IMuiGuestMemory
	{
		textAddress = APTR.Null;
		if (!TryCreateStorageCursor(ref platform, address,
			MuiMenuItemTriggerStoragePrefix.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiMenuItemRecord.Size, out _) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiIntuiTextRecord.Size, out textAddress) ||
			!MuiGuestStructCursor.IsComplete(cursor))
		{
			textAddress = APTR.Null;
			return false;
		}
		return true;
	}

	internal static bool TryGetStringAddress<TPlatform>(ref TPlatform platform,
		APTR address, out APTR stringAddress)
		where TPlatform : struct, IMuiGuestMemory
	{
		stringAddress = APTR.Null;
		if (!TryCreateStorageCursor(ref platform, address, Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiMenuItemRecord.Size, out _) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiIntuiTextRecord.Size, out _) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				StringCapacity, out stringAddress) ||
			!MuiGuestStructCursor.IsComplete(cursor))
		{
			stringAddress = APTR.Null;
			return false;
		}
		return true;
	}

	private static bool TryCreateStorageCursor<TPlatform>(ref TPlatform platform,
		APTR address, uint byteSize, out MuiGuestStructCursor cursor)
		where TPlatform : struct, IMuiGuestMemory
	{
		cursor = default;
		return IsMapped(ref platform, address) &&
			MuiGuestStructCursor.TryCreate(ref platform, address,
				byteSize, out cursor);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform, APTR address,
		MenuItem menu, APTR title) where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMapped(ref platform, address) ||
			!TryGetTextAddress(ref platform, address, out var textAddress) ||
			!TryGetStringAddress(ref platform, address, out var stringAddress))
			return false;

		var text = default(IntuiText);
		text.DrawMode = DrawMode.Jam1;
		text.Text = STRPTR.FromPointer(stringAddress.Raw);

		var titleLength = 0u;
		var hasTitle = title.IsNotNull && title.Raw != 0xFFFFFFFFu &&
			CStringCodec.TryReadLength(ref platform, title, StringCapacity,
				out titleLength);
		if (hasTitle)
		{
			var sourceCursor = default(MuiMenuItemTriggerStringByteCursor);
			sourceCursor.Base = title;
			sourceCursor.Length = titleLength + 1;
			var destinationCursor = default(MuiMenuItemTriggerStringByteCursor);
			destinationCursor.Base = stringAddress;
			destinationCursor.Length = titleLength + 1;
			for (var index = 0u; index < sourceCursor.Length; index++)
			{
				sourceCursor.Index = index;
				destinationCursor.Index = index;
				if (!MuiMenuItemTriggerStringByteCursorCodec.TryReadByte(
					ref platform, sourceCursor, out var value) ||
					!MuiMenuItemTriggerStringByteCursorCodec.TryWriteByte(
					ref platform, destinationCursor, value)) return false;
			}
		}
		else
		{
			platform.Clear(stringAddress, StringCapacity);
			menu.Flags &= ~MenuItemFlags.ItemText;
		}

		menu.ItemFill = textAddress;
		menu.SelectFill = textAddress;
		return MuiMenuItemMemoryCodec.WriteRecord(ref platform, address, menu) &&
			MuiIntuiTextMemoryCodec.WriteRecord(ref platform, textAddress, text);
	}
}

internal enum MuiIntuiTextField : byte
{
	FrontPen,
	BackPen,
	DrawMode,
	Padding,
	LeftEdge,
	TopEdge,
	Font,
	Text,
	NextText,
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiIntuiTextFieldCursor
{
	internal APTR Record;
	internal MuiIntuiTextField Field;
}

internal static class MuiIntuiTextFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiIntuiTextFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiIntuiTextFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiIntuiTextRecordMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);
}

internal static class MuiIntuiTextMemoryCodec
{
	private static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiIntuiTextRecordMemoryCodec.TryGetAddress(ref platform, record, field,
			out address);

	private static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiIntuiTextRecordMemoryCodec.TryWriteUInt8(ref platform, record, field,
			value);

	private static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiIntuiTextRecordMemoryCodec.TryWriteUInt16(ref platform, record, field,
			value);

	private static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiIntuiTextRecordMemoryCodec.TryWriteUInt32(ref platform, record, field,
			value);

	private static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiIntuiTextRecordMemoryCodec.TryReadUInt8(ref platform, record, field,
			out value);

	private static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiIntuiTextRecordMemoryCodec.TryReadUInt16(ref platform, record, field,
			out value);

	private static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiIntuiTextRecordMemoryCodec.TryReadUInt32(ref platform, record, field,
			out value);

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out IntuiText value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiIntuiTextRecordCodec.TryReadStructural(ref platform, address,
			out var record)) return false;
		value.FrontPen = record.FrontPen;
		value.BackPen = record.BackPen;
		value.DrawMode = (DrawMode)record.DrawMode;
		value.LeftEdge = record.LeftEdge;
		value.TopEdge = record.TopEdge;
		value.Font = record.Font;
		value.Text = STRPTR.FromPointer(record.Text.Raw);
		value.NextText = record.NextText;
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, IntuiText value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var record = default(MuiIntuiTextRecord);
		record.FrontPen = value.FrontPen;
		record.BackPen = value.BackPen;
		record.DrawMode = (byte)value.DrawMode;
		record.Padding = 0;
		record.LeftEdge = value.LeftEdge;
		record.TopEdge = value.TopEdge;
		record.Font = value.Font;
		record.Text = APTR.FromPointer(value.Text.Raw);
		record.NextText = value.NextText;
		return MuiIntuiTextRecordCodec.WriteStructural(ref platform, address,
			record);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out IntuiText value) where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		IntuiText value) where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}
