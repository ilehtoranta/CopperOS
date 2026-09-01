/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiMenuItemTriggerStringCursorTests
{
	[Fact]
	public void TriggerStringCursorUsesBoundedNamedReadWrite()
	{
		Assert.Equal(12u,
			(uint)Unsafe.SizeOf<MuiMenuItemTriggerStringByteCursor>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var source = APTR.FromPointer(0x1300);
		var destination = APTR.FromPointer(0x1340);
		platform.WriteUInt8(source, 0, (byte)'M');
		var sourceCursor = default(MuiMenuItemTriggerStringByteCursor);
		sourceCursor.Base = source;
		sourceCursor.Index = 0;
		sourceCursor.Length = 1;
		var destinationCursor = default(MuiMenuItemTriggerStringByteCursor);
		destinationCursor.Base = destination;
		destinationCursor.Index = 0;
		destinationCursor.Length = 1;

		Assert.True(MuiMenuItemTriggerStringByteCursorCodec.TryReadByte(
			ref platform, sourceCursor, out var value));
		Assert.Equal((byte)'M', value);
		Assert.True(MuiMenuItemTriggerStringByteCursorCodec.TryWriteByte(
			ref platform, destinationCursor, value));
		Assert.Equal((byte)'M', platform.ReadUInt8(destination, 0));

		sourceCursor.Index = sourceCursor.Length;
		Assert.False(MuiMenuItemTriggerStringByteCursorCodec.TryReadByte(
			ref platform, sourceCursor, out _));
		sourceCursor.Index = 0;
		sourceCursor.Length = MuiMenuItemTriggerStringByteCursor.MaximumLength + 1;
		Assert.False(MuiMenuItemTriggerStringByteCursorCodec.TryReadByte(
			ref platform, sourceCursor, out _));
		sourceCursor.Length = 1;
		sourceCursor.Base = APTR.FromPointer(uint.MaxValue);
		sourceCursor.Index = 1;
		Assert.False(MuiMenuItemTriggerStringByteCursorCodec.TryReadByte(
			ref platform, sourceCursor, out _));
		sourceCursor.Base = APTR.Null;
		sourceCursor.Index = 0;
		Assert.False(MuiMenuItemTriggerStringByteCursorCodec.TryReadByte(
			ref platform, sourceCursor, out _));
	}
}
