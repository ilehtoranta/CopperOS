/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaHandledEventsStateStructCodecTests
{
	[Fact]
	public void HandledEventsStateRoundTripsThroughSequentialRecord()
	{
		Assert.Equal(24, Unsafe.SizeOf<MuiAreaHandledEventsStateRecord>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2400);
		var expected = default(MuiAreaHandledEventsStateRecord);
		expected.Signature = MuiAreaHandledEventsStateRecord.Magic;
		expected.Events = 0xA5A5u;
		expected.Window = APTR.FromPointer(0x3000);
		expected.Handler = APTR.FromPointer(0x3100);
		expected.Generation = 19;
		expected.HandlerFlags = 0x1234;
		expected.Priority = -7;
		expected.Reserved = 0x5A;

		Assert.True(MuiAreaHandledEventsStateStructCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiAreaHandledEventsStateStructCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Signature, actual.Signature);
		Assert.Equal(expected.Events, actual.Events);
		Assert.Equal(expected.Window, actual.Window);
		Assert.Equal(expected.Handler, actual.Handler);
		Assert.Equal(expected.Generation, actual.Generation);
		Assert.Equal(expected.HandlerFlags, actual.HandlerFlags);
		Assert.Equal(expected.Priority, actual.Priority);
		Assert.Equal(expected.Reserved, actual.Reserved);
	}

	[Fact]
	public void HandledEventsStateCodecRejectsIncompleteGuestRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var crossingEnd = APTR.FromPointer(0x20FE9);
		var value = default(MuiAreaHandledEventsStateRecord);
		Assert.False(MuiAreaHandledEventsStateStructCodec.Write(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaHandledEventsStateStructCodec.TryRead(ref platform,
			crossingEnd, out _));
		Assert.False(MuiAreaHandledEventsStateStructCodec.TryRead(ref platform,
			APTR.Null, out _));
	}
}
