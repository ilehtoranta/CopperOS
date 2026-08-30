/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiDirlistMessageStructCodecTests
{
	[Fact]
	public void OperationPacketsRoundTripThroughSequentialRecords()
	{
		Assert.Equal(4, Unsafe.SizeOf<MuiDirlistMethodMessage>());
		Assert.Equal(12, Unsafe.SizeOf<MuiDirlistSetMessage>());
		Assert.Equal(12, Unsafe.SizeOf<MuiDirlistRenameMessage>());
		Assert.Equal(12, Unsafe.SizeOf<MuiDirlistProtectionMessage>());
		Assert.Equal(12, Unsafe.SizeOf<MuiDirlistGetEntryMessage>());

		var platform = new MuiHeadlessTestPlatform(0x1000, 0x80000, 0x8000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2C00);
		Assert.True(MuiDirlistMessageStructCodec.TryWriteMethod(ref platform,
			address, MuiDirlistMessageCodec.ReRead));
		Assert.True(MuiDirlistMessageStructCodec.TryReadMethod(ref platform,
			address, out var method));
		Assert.Equal(MuiDirlistMessageCodec.ReRead, method.MethodId);

		var set = default(MuiDirlistSetMessage);
		set.MethodId = MuiDirlistMessageCodec.Set;
		set.Attribute = 7;
		set.Value = 9;
		Assert.True(MuiDirlistMessageStructCodec.TryWriteSet(ref platform,
			address, set));
		Assert.True(MuiDirlistMessageStructCodec.TryReadSet(ref platform,
			address, out var setRead));
		Assert.Equal(set.Attribute, setRead.Attribute);
		Assert.Equal(set.Value, setRead.Value);

		var rename = default(MuiDirlistRenameMessage);
		rename.MethodId = MuiDirlistMessageCodec.Rename;
		rename.Entry = 3;
		rename.Name = 0x2800;
		Assert.True(MuiDirlistMessageStructCodec.TryWriteRename(ref platform,
			address, rename));
		Assert.True(MuiDirlistMessageStructCodec.TryReadRename(ref platform,
			address, out var renameRead));
		Assert.Equal(rename.Entry, renameRead.Entry);
		Assert.Equal(rename.Name, renameRead.Name);

		var protection = default(MuiDirlistProtectionMessage);
		protection.MethodId = MuiDirlistMessageCodec.SetProtection;
		protection.Entry = 4;
		protection.Protection = 0x12345678;
		Assert.True(MuiDirlistMessageStructCodec.TryWriteProtection(
			ref platform, address, protection));
		Assert.True(MuiDirlistMessageStructCodec.TryReadProtection(
			ref platform, address, out var protectionRead));
		Assert.Equal(protection.Protection, protectionRead.Protection);

		var getEntry = default(MuiDirlistGetEntryMessage);
		getEntry.MethodId = MuiDirlistMessageCodec.ListGetEntry;
		getEntry.Position = unchecked((uint)-2);
		getEntry.Storage = 0x2900;
		Assert.True(MuiDirlistMessageStructCodec.TryWriteGetEntry(
			ref platform, address, getEntry));
		Assert.True(MuiDirlistMessageStructCodec.TryReadGetEntry(
			ref platform, address, out var getEntryRead));
		Assert.Equal(getEntry.Position, getEntryRead.Position);
		Assert.Equal(getEntry.Storage, getEntryRead.Storage);
	}

	[Fact]
	public void OperationPacketsRejectIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x80000, 0x8000,
			APTR.FromPointer(0x1000));
		var crossingEnd = APTR.FromPointer(0x80FF5);
		var packet = default(MuiDirlistSetMessage);
		Assert.False(MuiDirlistMessageStructCodec.TryWriteSet(ref platform,
			crossingEnd, packet));
		Assert.False(MuiDirlistMessageStructCodec.TryReadRename(ref platform,
			crossingEnd, out _));
		Assert.False(MuiDirlistMessageStructCodec.TryReadMethodIdValue(
			ref platform, APTR.Null, out _));
	}
}
