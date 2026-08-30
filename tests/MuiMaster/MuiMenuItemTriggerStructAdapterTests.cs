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
}
