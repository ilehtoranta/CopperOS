/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiMenuItemTriggerStructAdapterTests
{
	[Fact]
	public void MenuItemUsesThePackedMorphosWireSize()
	{
		Assert.Equal(MenuItem.Size, (uint)Unsafe.SizeOf<MenuItem>());
		Assert.Equal(MenuItem.Size, (uint)Unsafe.SizeOf<MuiMenuItemRecord>());
		Assert.Equal(IntuiText.Size,
			(uint)Unsafe.SizeOf<IntuiText>());
		Assert.Equal(MuiMenuItemTriggerStoragePrefix.Size,
			(uint)Unsafe.SizeOf<MuiMenuItemTriggerStoragePrefix>());
	}

	[Fact]
	public void MenuItemCodecRoundTripsNamedFields()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var expected = default(MenuItem);
		expected.NextItem = APTR.FromPointer(0x3456);
		expected.LeftEdge = -3;
		expected.TopEdge = 4;
		expected.Width = 120;
		expected.Height = 17;
		expected.Flags = MenuItemFlags.CheckIt | MenuItemFlags.ItemText |
			MenuItemFlags.CommandSequence | MenuItemFlags.MenuToggle |
			MenuItemFlags.Enabled | MenuItemFlags.Checked;
		expected.MutualExclude = unchecked((int)0x80000003u);
		expected.ItemFill = APTR.FromPointer(0x4567);
		expected.SelectFill = APTR.FromPointer(0x5678);
		expected.Command = (sbyte)'Q';
		expected.SubItem = APTR.FromPointer(0x6789);
		expected.NextSelect = 0x1234;

		Assert.True(MuiMenuItemMemoryCodec.WriteRecord(ref platform, address, expected));
		Assert.True(MuiMenuItemMemoryCodec.TryReadRecord(ref platform, address,
			out var actual));
		Assert.Equal(expected.NextItem, actual.NextItem);
		Assert.Equal(expected.LeftEdge, actual.LeftEdge);
		Assert.Equal(expected.TopEdge, actual.TopEdge);
		Assert.Equal(expected.Width, actual.Width);
		Assert.Equal(expected.Height, actual.Height);
		Assert.Equal(expected.Flags, actual.Flags);
		Assert.Equal(expected.MutualExclude, actual.MutualExclude);
		Assert.Equal(expected.ItemFill, actual.ItemFill);
		Assert.Equal(expected.SelectFill, actual.SelectFill);
		Assert.Equal(expected.Command, actual.Command);
		Assert.Equal(expected.SubItem, actual.SubItem);
		Assert.Equal(expected.NextSelect, actual.NextSelect);

		Assert.False(MuiMenuItemMemoryCodec.TryReadRecord(ref platform,
			APTR.FromPointer(0x20FF0), out _));
	}

	[Fact]
	public void MenuItemFieldAccessUsesTheCompleteNamedRecord()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2800);
		var record = default(MuiMenuItemRecord);
		record.NextItem = APTR.FromPointer(0x3456);
		record.LeftEdge = -3;
		record.TopEdge = 4;
		record.Width = 120;
		record.Height = 17;
		record.Flags = (ushort)MenuItemFlags.ItemText;
		record.MutualExclude = -7;
		record.ItemFill = APTR.FromPointer(0x4567);
		record.SelectFill = APTR.FromPointer(0x5678);
		record.Command = (sbyte)'Q';
		record.SubItem = APTR.FromPointer(0x6789);
		record.NextSelect = 0x1234;
		Assert.True(MuiMenuItemRecordCodec.WriteStructural(ref platform, address,
			record));
		Assert.True(MuiMenuItemRecordMemoryCodec.TryWriteUInt16(ref platform,
			address, MuiMenuItemField.LeftEdge, unchecked((ushort)-22)));
		Assert.True(MuiMenuItemRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiMenuItemField.MutualExclude, out var exclude) &&
			exclude == unchecked((uint)-7));
		Assert.True(MuiMenuItemRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiMenuItemField.SubItem, out var subItem) && subItem.Raw ==
			0x281Cu);
		var cursor = new MuiMenuItemFieldCursor
		{
			Record = address,
			Field = MuiMenuItemField.SubItem,
		};
		Assert.True(MuiMenuItemFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var typedSubItem, out var typedSubItemSize));
		Assert.Equal(subItem, typedSubItem);
		Assert.Equal(4u, typedSubItemSize);
		cursor.Field = MuiMenuItemField.Command;
		Assert.True(MuiMenuItemFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var commandAddress, out var commandSize));
		Assert.Equal(address.Raw + MuiMenuItemRecord.CommandOffset,
			commandAddress.Raw);
		Assert.Equal(1u, commandSize);
		Assert.True(MuiMenuItemRecordCodec.TryReadStructural(ref platform, address,
			out var decoded) && decoded.LeftEdge == -22 && decoded.TopEdge ==
			record.TopEdge && decoded.ItemFill == record.ItemFill &&
			decoded.SubItem == record.SubItem);
		Assert.False(MuiMenuItemRecordMemoryCodec.TryReadUInt8(ref platform,
			address, MuiMenuItemField.Width, out _));
		Assert.False(MuiMenuItemRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiMenuItemField.NextItem, out _));
		cursor.Field = (MuiMenuItemField)255;
		Assert.False(MuiMenuItemFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		cursor.Record = APTR.Null;
		cursor.Field = MuiMenuItemField.NextItem;
		Assert.False(MuiMenuItemFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
	}

	[Fact]
	public void TriggerStorageComposesTheTwoNamedRecords()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			state);
		var address = MuiHeadlessMemory.Allocate(ref platform,
			MuiMenuItemTriggerStorageCodec.Size);
		Assert.True(MuiMenuItemTriggerStorageCodec.IsMapped(ref platform, address));
		Assert.True(MuiMenuItemTriggerStorageCodec.TryGetTextAddress(ref platform,
			address, out var textAddress));
		Assert.Equal(address.Raw + MuiMenuItemRecord.Size, textAddress.Raw);
		Assert.True(MuiMenuItemTriggerStorageCodec.TryGetStringAddress(ref platform,
			address, out var stringAddress));
		Assert.Equal(address.Raw + MuiMenuItemTriggerStoragePrefix.Size,
			stringAddress.Raw);
		var menu = default(MenuItem);
		menu.ItemFill = textAddress;
		menu.SelectFill = textAddress;
		Assert.True(MuiMenuItemMemoryCodec.WriteRecord(ref platform, address, menu));
		var text = default(IntuiText);
		text.DrawMode = DrawMode.Jam1;
		text.Text = STRPTR.FromPointer(stringAddress.Raw);
		Assert.True(MuiIntuiTextMemoryCodec.WriteRecord(ref platform, textAddress,
			text));
	}

	[Fact]
	public void IntuiTextFieldAccessUsesTheCompleteNamedRecord()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2A00);
		var record = default(MuiIntuiTextRecord);
		record.FrontPen = 2;
		record.BackPen = 3;
		record.DrawMode = (byte)DrawMode.Jam1;
		record.LeftEdge = -4;
		record.TopEdge = 6;
		record.Font = APTR.FromPointer(0x4567);
		record.Text = APTR.FromPointer(0x5678);
		record.NextText = APTR.FromPointer(0x6789);
		Assert.True(MuiIntuiTextRecordCodec.WriteStructural(ref platform, address,
			record));
		Assert.True(MuiIntuiTextRecordMemoryCodec.TryWriteUInt16(ref platform,
			address, MuiIntuiTextField.LeftEdge, unchecked((ushort)-12)));
		Assert.True(MuiIntuiTextRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiIntuiTextField.Text, out var text) && text == 0x5678u);
		Assert.True(MuiIntuiTextRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiIntuiTextField.NextText, out var nextText) && nextText.Raw ==
			0x2A10u);
		var cursor = new MuiIntuiTextFieldCursor
		{
			Record = address,
			Field = MuiIntuiTextField.NextText,
		};
		Assert.True(MuiIntuiTextFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var typedNextText, out var typedNextTextSize));
		Assert.Equal(nextText, typedNextText);
		Assert.Equal(4u, typedNextTextSize);
		cursor.Field = MuiIntuiTextField.LeftEdge;
		Assert.True(MuiIntuiTextFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var leftEdgeAddress, out var leftEdgeSize));
		Assert.Equal(address.Raw + MuiIntuiTextRecord.LeftEdgeOffset,
			leftEdgeAddress.Raw);
		Assert.Equal(2u, leftEdgeSize);
		Assert.True(MuiIntuiTextRecordCodec.TryReadStructural(ref platform, address,
			out var decoded) && decoded.LeftEdge == -12 && decoded.TopEdge ==
			record.TopEdge && decoded.Font == record.Font && decoded.NextText ==
			record.NextText);
		Assert.False(MuiIntuiTextRecordMemoryCodec.TryReadUInt8(ref platform,
			address, MuiIntuiTextField.LeftEdge, out _));
		Assert.False(MuiIntuiTextRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
		cursor.Field = (MuiIntuiTextField)255;
		Assert.False(MuiIntuiTextFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		cursor.Record = APTR.Null;
		cursor.Field = MuiIntuiTextField.FrontPen;
		Assert.False(MuiIntuiTextFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
	}
}
