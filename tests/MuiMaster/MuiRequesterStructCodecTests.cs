/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiRequesterStructCodecTests
{
	[Fact]
	public void RequesterStateAndParameterSlotRoundTripSequentially()
	{
		Assert.Equal(8, Unsafe.SizeOf<MuiRequesterServiceStateRecord>());
		Assert.Equal(4, Unsafe.SizeOf<MuiRequesterParameterSlot>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var stateAddress = APTR.FromPointer(0x2400);
		var slotAddress = APTR.FromPointer(0x2410);
		var state = default(MuiRequesterServiceStateRecord);
		state.Magic = MuiRequesterServiceLayout.Magic;
		state.Generation = 31;
		var slot = default(MuiRequesterParameterSlot);
		slot.Value = 0xFEDCBA98u;

		Assert.True(MuiRequesterServiceStateStructCodec.Write(ref platform,
			stateAddress, state));
		Assert.True(MuiRequesterServiceStateStructCodec.TryRead(ref platform,
			stateAddress, out var actualState));
		Assert.Equal(state.Magic, actualState.Magic);
		Assert.Equal(state.Generation, actualState.Generation);

		Assert.True(MuiRequesterParameterSlotStructCodec.Write(ref platform,
			slotAddress, slot));
		Assert.True(MuiRequesterParameterSlotStructCodec.TryRead(ref platform,
			slotAddress, out var actualSlot));
		Assert.Equal(slot.Value, actualSlot.Value);
	}

	[Fact]
	public void RequesterStructCodecsRejectNullAndIncompleteGuestRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var state = default(MuiRequesterServiceStateRecord);
		var slot = default(MuiRequesterParameterSlot);
		var stateCrossing = APTR.FromPointer(0x20FFC);
		var slotCrossing = APTR.FromPointer(0x20FFE);

		Assert.False(MuiRequesterServiceStateStructCodec.Write(ref platform,
			APTR.Null, state));
		Assert.False(MuiRequesterServiceStateStructCodec.TryRead(ref platform,
			APTR.Null, out _));
		Assert.False(MuiRequesterServiceStateStructCodec.Write(ref platform,
			stateCrossing, state));
		Assert.False(MuiRequesterServiceStateStructCodec.TryRead(ref platform,
			stateCrossing, out _));
		Assert.False(MuiRequesterParameterSlotStructCodec.Write(ref platform,
			APTR.Null, slot));
		Assert.False(MuiRequesterParameterSlotStructCodec.TryRead(ref platform,
			APTR.Null, out _));
		Assert.False(MuiRequesterParameterSlotStructCodec.Write(ref platform,
			slotCrossing, slot));
		Assert.False(MuiRequesterParameterSlotStructCodec.TryRead(ref platform,
			slotCrossing, out _));
	}
}
