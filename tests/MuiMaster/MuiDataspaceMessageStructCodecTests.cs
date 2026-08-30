/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiDataspaceMessageStructCodecTests
{
	[Fact]
	public void FixedPacketsUseSequentialNamedRecords()
	{
		Assert.Equal(16, Unsafe.SizeOf<MuiDataspaceAddMessage>());
		Assert.Equal(8, Unsafe.SizeOf<MuiDataspaceFindMessage>());
		Assert.Equal(12, Unsafe.SizeOf<MuiDataspaceGetMessage>());
		Assert.Equal(8, Unsafe.SizeOf<MuiDataspaceMergeMessage>());
		Assert.Equal(8, Unsafe.SizeOf<MuiDataspaceRemoveMessage>());
		Assert.Equal(4, Unsafe.SizeOf<MuiDataspaceClearMessage>());

		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2400);
		var add = default(MuiDataspaceAddMessage);
		add.Data = APTR.FromPointer(0x3000);
		add.Length = -4;
		add.Id = 19;
		Assert.True(MuiDataspaceMessageStructCodec.TryWriteAdd(ref platform,
			address, add));
		Assert.True(MuiDataspaceMessageStructCodec.TryReadAdd(ref platform,
			address, out var decoded));
		Assert.Equal(add.Data.Raw, decoded.Data.Raw);
		Assert.Equal(add.Length, decoded.Length);
		Assert.Equal(add.Id, decoded.Id);
	}

	[Fact]
	public void FixedPacketsRejectIncompleteGuestRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var crossingEnd = APTR.FromPointer(0x20FF5);
		var add = default(MuiDataspaceAddMessage);
		Assert.False(MuiDataspaceMessageStructCodec.TryWriteAdd(ref platform,
			crossingEnd, add));
		Assert.False(MuiDataspaceMessageStructCodec.TryReadGet(ref platform,
			crossingEnd, out _));
		Assert.False(MuiDataspaceMessageStructCodec.TryReadMethodIdValue(
			ref platform, APTR.Null, out _));
	}
}
