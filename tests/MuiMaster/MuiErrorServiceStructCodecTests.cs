/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiErrorServiceStructCodecTests
{
	[Fact]
	public void ErrorStateRoundTripsThroughSequentialNamedRecord()
	{
		Assert.Equal(16, Unsafe.SizeOf<MuiErrorServiceStateRecord>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2400);
		var expected = default(MuiErrorServiceStateRecord);
		expected.Magic = MuiErrorServiceLayout.Magic;
		expected.Version = MuiErrorServiceLayout.Version;
		expected.Error = 0xFFFFFFFEu;
		expected.Sequence = 17;

		Assert.True(MuiErrorServiceStateStructCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiErrorServiceStateStructCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Version, actual.Version);
		Assert.Equal(expected.Error, actual.Error);
		Assert.Equal(expected.Sequence, actual.Sequence);
	}

	[Fact]
	public void ErrorStateStructCodecRejectsNullAndIncompleteGuestRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var value = default(MuiErrorServiceStateRecord);
		var crossing = APTR.FromPointer(0x20FF8);

		Assert.False(MuiErrorServiceStateStructCodec.Write(ref platform,
			APTR.Null, value));
		Assert.False(MuiErrorServiceStateStructCodec.TryRead(ref platform,
			APTR.Null, out _));
		Assert.False(MuiErrorServiceStateStructCodec.Write(ref platform,
			crossing, value));
		Assert.False(MuiErrorServiceStateStructCodec.TryRead(ref platform,
			crossing, out _));
	}
}
