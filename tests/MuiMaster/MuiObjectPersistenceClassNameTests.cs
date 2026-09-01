/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiObjectPersistenceClassNameTests
{
	[Fact]
	public void PersistenceClassProbeUsesNamedCursor()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var name = APTR.FromPointer(0x1300);
		Write(platform, name, "AREA.MUI");
		Assert.True(MuiObjectPersistenceCore.MatchesNamedClass(ref platform, name,
			false));
		Write(platform, name, "GROUP.MUI");
		Assert.True(MuiObjectPersistenceCore.MatchesNamedClass(ref platform, name,
			true));
		Write(platform, name, "other.mui");
		Assert.False(MuiObjectPersistenceCore.MatchesNamedClass(ref platform, name,
			false));
	}

	[Fact]
	public void PersistenceClassProbeRejectsMalformedGuestNames()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		Assert.False(MuiObjectPersistenceCore.MatchesNamedClass(ref platform,
			APTR.Null, false));
		Assert.False(MuiObjectPersistenceCore.MatchesNamedClass(ref platform,
			APTR.FromPointer(0x30000), false));
		var name = APTR.FromPointer(0x1300);
		Write(platform, name, "area.mui");
		Assert.False(MuiObjectPersistenceCore.MatchesNamedClass(ref platform, name,
			true));
	}

	[Fact]
	public void PersistenceStringTerminatorsUseNamedCursors()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var empty = APTR.FromPointer(0x1300);
		Assert.True(MuiObjectPersistenceCore.WriteEmptyPersistenceString(
			ref platform, empty));
		Assert.Equal((byte)0, platform.ReadUInt8(empty, 0));
		Assert.True(MuiObjectPersistenceCore.TryReadPersistenceTerminator(
			ref platform, empty, 1));
		platform.WriteUInt8(empty, 0, (byte)'x');
		Assert.False(MuiObjectPersistenceCore.TryReadPersistenceTerminator(
			ref platform, empty, 1));
		Assert.False(MuiObjectPersistenceCore.TryReadPersistenceTerminator(
			ref platform, APTR.Null, 1));
	}

	private static void Write(MuiHeadlessTestPlatform platform, APTR address,
		string text)
	{
		for (var index = 0; index < text.Length; index++)
			platform.WriteUInt8(address, index, (byte)text[index]);
		platform.WriteUInt8(address, text.Length, 0);
	}
}
