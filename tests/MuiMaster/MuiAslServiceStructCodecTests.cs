/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAslServiceStructCodecTests
{
	[Fact]
	public void StateAndLeaseRoundTripThroughSequentialNamedRecords()
	{
		Assert.Equal(12, Unsafe.SizeOf<MuiAslServiceStateRecord>());
		Assert.Equal(16, Unsafe.SizeOf<MuiAslRequestLeaseRecord>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var stateAddress = APTR.FromPointer(0x2400);
		var leaseAddress = APTR.FromPointer(0x2420);
		var state = default(MuiAslServiceStateRecord);
		state.Magic = MuiAslServiceLayout.Magic;
		state.Head = APTR.FromPointer(0x3500);
		state.Generation = 23;
		var lease = default(MuiAslRequestLeaseRecord);
		lease.Next = APTR.FromPointer(0x2420);
		lease.Requester = APTR.FromPointer(0x3600);
		lease.Type = 7;
		lease.Tags = APTR.FromPointer(0x3700);

		Assert.True(MuiAslServiceStateStructCodec.Write(ref platform,
			stateAddress, state));
		Assert.True(MuiAslServiceStateStructCodec.TryRead(ref platform,
			stateAddress, out var actualState));
		Assert.Equal(state.Magic, actualState.Magic);
		Assert.Equal(state.Head, actualState.Head);
		Assert.Equal(state.Generation, actualState.Generation);

		Assert.True(MuiAslRequestLeaseStructCodec.Write(ref platform,
			leaseAddress, lease));
		Assert.True(MuiAslRequestLeaseStructCodec.TryRead(ref platform,
			leaseAddress, out var actualLease));
		Assert.Equal(lease.Next, actualLease.Next);
		Assert.Equal(lease.Requester, actualLease.Requester);
		Assert.Equal(lease.Type, actualLease.Type);
		Assert.Equal(lease.Tags, actualLease.Tags);
	}

	[Fact]
	public void SequentialAslCodecsRejectNullAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var state = default(MuiAslServiceStateRecord);
		var lease = default(MuiAslRequestLeaseRecord);
		var crossing = APTR.FromPointer(0x20FF8);

		Assert.False(MuiAslServiceStateStructCodec.Write(ref platform,
			APTR.Null, state));
		Assert.False(MuiAslServiceStateStructCodec.TryRead(ref platform,
			APTR.Null, out _));
		Assert.False(MuiAslServiceStateStructCodec.Write(ref platform,
			crossing, state));
		Assert.False(MuiAslServiceStateStructCodec.TryRead(ref platform,
			crossing, out _));
		Assert.False(MuiAslRequestLeaseStructCodec.Write(ref platform,
			crossing, lease));
		Assert.False(MuiAslRequestLeaseStructCodec.TryRead(ref platform,
			crossing, out _));
	}
}
