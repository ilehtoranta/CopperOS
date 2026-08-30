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
// big-endian guest-memory boundary. Keeping the wire shape behind a named
// record prevents menu behavior from depending on consumer-side offsets.
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

internal static class MuiMenuItemMemoryCodec
{
	private static bool TryResolve(MuiMenuItemField field,
		out uint offset, out uint size)
	{
		switch (field)
		{
			case MuiMenuItemField.NextItem: offset = 0; size = 4; return true;
			case MuiMenuItemField.LeftEdge: offset = 4; size = 2; return true;
			case MuiMenuItemField.TopEdge: offset = 6; size = 2; return true;
			case MuiMenuItemField.Width: offset = 8; size = 2; return true;
			case MuiMenuItemField.Height: offset = 10; size = 2; return true;
			case MuiMenuItemField.Flags: offset = 12; size = 2; return true;
			case MuiMenuItemField.MutualExclude: offset = 14; size = 4; return true;
			case MuiMenuItemField.ItemFill: offset = 18; size = 4; return true;
			case MuiMenuItemField.SelectFill: offset = 22; size = 4; return true;
			case MuiMenuItemField.Command: offset = 26; size = 1; return true;
			case MuiMenuItemField.Padding: offset = 27; size = 1; return true;
			case MuiMenuItemField.SubItem: offset = 28; size = 4; return true;
			case MuiMenuItemField.NextSelect: offset = 32; size = 2; return true;
		}
		offset = 0;
		size = 0;
		return false;
	}

	internal static bool IsMapped<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory =>
		!address.IsNull && platform.IsMapped(address, MenuItem.Size);

	private static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset, out var size) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, MenuItem.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, size);
	}

	private static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	private static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt16(address, 0);
		return true;
	}

	private static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt8(address, 0);
		return true;
	}

	private static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	private static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt16(address, 0, value);
		return true;
	}

	private static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiMenuItemField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt8(address, 0, value);
		return true;
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MenuItem value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address, MenuItem.Size,
			out var cursor) ||
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
		value.Flags = (MenuItemFlags)flags;
		value.MutualExclude = unchecked((int)mutualExclude);
		value.ItemFill = APTR.FromPointer(itemFill);
		value.SelectFill = APTR.FromPointer(selectFill);
		value.Command = unchecked((sbyte)command);
		value.SubItem = APTR.FromPointer(subItem);
		value.NextSelect = nextSelect;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MenuItem value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address, MenuItem.Size,
			out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.NextItem.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				unchecked((ushort)value.LeftEdge)) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				unchecked((ushort)value.TopEdge)) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				unchecked((ushort)value.Width)) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				unchecked((ushort)value.Height)) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				(ushort)value.Flags) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.MutualExclude)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.ItemFill.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.SelectFill.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				unchecked((byte)value.Command)) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor, 0) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.SubItem.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				value.NextSelect)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MenuItem value) where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MenuItem value) where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}

// The MenuItem projection owns one textual fill record. Keeping the fixed
// prefix as a composed struct documents the exact relationship between the
// published MenuItem and its IntuiText; the trailing C-string capacity is
// handled by the storage codec below.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMenuItemTriggerStoragePrefix
{
	internal const uint Size = MenuItem.Size + IntuiText.Size;
	internal MenuItem Menu;
	internal IntuiText Text;
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
		if (!IsMapped(ref platform, address) ||
			address.Raw > uint.MaxValue - MenuItem.Size) return false;
		textAddress = APTR.FromPointer(address.Raw + MenuItem.Size);
		return platform.IsMapped(textAddress, IntuiText.Size);
	}

	internal static bool TryGetStringAddress<TPlatform>(ref TPlatform platform,
		APTR address, out APTR stringAddress)
		where TPlatform : struct, IMuiGuestMemory
	{
		stringAddress = APTR.Null;
		if (!IsMapped(ref platform, address) || address.Raw >
			uint.MaxValue - MuiMenuItemTriggerStoragePrefix.Size) return false;
		stringAddress = APTR.FromPointer(address.Raw +
			MuiMenuItemTriggerStoragePrefix.Size);
		return platform.IsMapped(stringAddress, StringCapacity);
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
			for (var index = 0u; index <= titleLength; index++)
				platform.WriteUInt8(stringAddress, (int)index,
					platform.ReadUInt8(title, (int)index));
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

internal static class MuiIntuiTextMemoryCodec
{
	private static bool TryResolve(MuiIntuiTextField field,
		out uint offset, out uint size)
	{
		switch (field)
		{
			case MuiIntuiTextField.FrontPen: offset = 0; size = 1; return true;
			case MuiIntuiTextField.BackPen: offset = 1; size = 1; return true;
			case MuiIntuiTextField.DrawMode: offset = 2; size = 1; return true;
			case MuiIntuiTextField.Padding: offset = 3; size = 1; return true;
			case MuiIntuiTextField.LeftEdge: offset = 4; size = 2; return true;
			case MuiIntuiTextField.TopEdge: offset = 6; size = 2; return true;
			case MuiIntuiTextField.Font: offset = 8; size = 4; return true;
			case MuiIntuiTextField.Text: offset = 12; size = 4; return true;
			case MuiIntuiTextField.NextText: offset = 16; size = 4; return true;
		}
		offset = 0;
		size = 0;
		return false;
	}

	private static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset, out var size) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, IntuiText.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, size);
	}

	private static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt8(address, 0, value);
		return true;
	}

	private static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt16(address, 0, value);
		return true;
	}

	private static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	private static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt8(address, 0);
		return true;
	}

	private static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt16(address, 0);
		return true;
	}

	private static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiTextField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out IntuiText value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address, IntuiText.Size,
			out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var frontPen) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var backPen) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var drawMode) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out _) ||
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
		value.DrawMode = (DrawMode)drawMode;
		value.LeftEdge = unchecked((short)leftEdge);
		value.TopEdge = unchecked((short)topEdge);
		value.Font = APTR.FromPointer(font);
		value.Text = STRPTR.FromPointer(text);
		value.NextText = APTR.FromPointer(nextText);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, IntuiText value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address, IntuiText.Size,
			out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.FrontPen) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.BackPen) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				(byte)value.DrawMode) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor, 0) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				unchecked((ushort)value.LeftEdge)) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				unchecked((ushort)value.TopEdge)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Font.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Text.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.NextText.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out IntuiText value) where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		IntuiText value) where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}
