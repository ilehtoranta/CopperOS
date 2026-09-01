/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStoreOwnedKeyCursorTests
{
	[Fact]
	public void OwnedKeyLengthUsesBoundedNamedCursor()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var key = APTR.FromPointer(0x1300);
		platform.WriteCString(key, "StoreKey");
		Assert.Equal(9u, MuiStoreCore.OwnedKeyLength(ref platform, key));
		Assert.Equal(0u, MuiStoreCore.OwnedKeyLength(ref platform, APTR.Null));
		Assert.Equal(1u, MuiStoreCore.OwnedKeyLength(ref platform,
			APTR.FromPointer(0x30000)));
	}

	[Fact]
	public void OwnedKeyLengthStopsAtBoundedTerminator()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var key = APTR.FromPointer(0x1380);
		platform.WriteUInt8(key, 0, (byte)'A');
		platform.WriteUInt8(key, 1, (byte)'B');
		platform.WriteUInt8(key, 2, 0);
		Assert.Equal(3u, MuiStoreCore.OwnedKeyLength(ref platform, key));
	}
}
