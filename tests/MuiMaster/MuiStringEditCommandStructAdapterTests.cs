/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringEditCommandStructAdapterTests
{
	[Fact]
	public void StringEditCommandUsesNamedRecordAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var value = new MuiStringEditCommandRecord
		{
			Command = MuiStringEditWorkCodec.CommandKey,
		};

		Assert.Equal(4, Unsafe.SizeOf<MuiStringEditCommandRecord>());
		Assert.True(MuiStringEditCommandCodec.TryGetAddress(ref platform,
			address, out var fieldAddress));
		Assert.Equal(address.Raw + MuiStringEditCommandRecord.CommandOffset,
			fieldAddress.Raw);
		Assert.True(MuiStringEditCommandCodec.Write(ref platform, address,
			value));
		Assert.True(MuiStringEditCommandCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.Command, decoded.Command);

		Assert.False(MuiStringEditCommandCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x20FFE), out _));
		Assert.False(MuiStringEditCommandCodec.TryRead(ref platform, APTR.Null,
			out _));
	}
}
