using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringscrollAdmissionTests
{
	[Fact]
	public void StringscrollContentPolicyScrollbarAndCompositionRoundTrip()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var stateAddress = APTR.FromPointer(0x1500);
		var policyAddress = APTR.FromPointer(0x1540);
		var scrollbarAddress = APTR.FromPointer(0x1580);
		var compositionAddress = APTR.FromPointer(0x15A0);
		var state = new MuiStringscrollStateRecord
		{
			Magic = MuiStringscrollStateRecord.Cookie,
			String = APTR.FromPointer(0x1800),
			ContentWidth = 640,
			ContentHeight = 24,
			ScrollX = 17,
			ScrollY = 3,
		};
		var policy = new MuiStringscrollPolicyRecord
		{
			Magic = MuiStringscrollPolicyRecord.Cookie,
			HorizBar = 1,
			NoInput = 0,
			SetMin = 1,
			SetVMin = 0,
			UseWinBorder = 1,
			VertBar = 1,
			VertScrollerOnly = 0,
		};
		var scrollbar = new MuiStringscrollScrollbarRecord
		{
			Magic = MuiStringscrollScrollbarRecord.Cookie,
			HorizBar = APTR.FromPointer(0x1840),
			VertBar = APTR.FromPointer(0x1880),
		};
		var composition = new MuiStringscrollCompositionRecord
		{
			Magic = MuiStringscrollCompositionRecord.Cookie,
			Horizontal = scrollbar.HorizBar,
			Vertical = scrollbar.VertBar,
			OwnedMask = MuiStringscrollCompositionState.HorizontalOwned |
				MuiStringscrollCompositionState.VerticalOwned,
			LastHorizontalFirst = 5,
			LastVerticalFirst = 2,
		};

		Assert.True(MuiStringscrollStateRecordCodec.Write(ref platform,
			stateAddress, state));
		Assert.True(MuiStringscrollPolicyRecordCodec.Write(ref platform,
			policyAddress, policy));
		Assert.True(MuiStringscrollScrollbarRecordCodec.Write(ref platform,
			scrollbarAddress, scrollbar));
		Assert.True(MuiStringscrollCompositionRecordCodec.Write(ref platform,
			compositionAddress, composition));
		Assert.True(MuiStringscrollStateRecordCodec.TryRead(ref platform,
			stateAddress, out var readState));
		Assert.Equal(state.String, readState.String);
		Assert.Equal(state.ScrollX, readState.ScrollX);
		Assert.True(MuiStringscrollPolicyRecordCodec.TryRead(ref platform,
			policyAddress, out var readPolicy));
		Assert.Equal(policy.SetMin, readPolicy.SetMin);
		Assert.Equal(policy.VertBar, readPolicy.VertBar);
		Assert.True(MuiStringscrollScrollbarRecordCodec.TryRead(ref platform,
			scrollbarAddress, out var readScrollbar));
		Assert.Equal(scrollbar.VertBar, readScrollbar.VertBar);
		Assert.True(MuiStringscrollCompositionRecordCodec.TryRead(ref platform,
			compositionAddress, out var readComposition));
		Assert.Equal(composition.OwnedMask, readComposition.OwnedMask);
		Assert.Equal(composition.LastVerticalFirst, readComposition.LastVerticalFirst);
	}

	[Fact]
	public void MalformedStringscrollCookiesRemainStructuralButFailClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var stateAddress = APTR.FromPointer(0x1700);
		var policyAddress = APTR.FromPointer(0x1740);
		var scrollbarAddress = APTR.FromPointer(0x1780);
		var compositionAddress = APTR.FromPointer(0x17A0);
		Assert.True(MuiStringscrollStateRecordCodec.Write(ref platform,
			stateAddress, new MuiStringscrollStateRecord
			{
				Magic = MuiStringscrollStateRecord.Cookie,
				ContentWidth = 1,
				ContentHeight = 2,
			}));
		Assert.True(MuiStringscrollPolicyRecordCodec.Write(ref platform,
			policyAddress, new MuiStringscrollPolicyRecord
			{
				Magic = MuiStringscrollPolicyRecord.Cookie,
			}));
		Assert.True(MuiStringscrollScrollbarRecordCodec.Write(ref platform,
			scrollbarAddress, new MuiStringscrollScrollbarRecord
			{
				Magic = MuiStringscrollScrollbarRecord.Cookie,
			}));
		Assert.True(MuiStringscrollCompositionRecordCodec.Write(ref platform,
			compositionAddress, new MuiStringscrollCompositionRecord
			{
				Magic = MuiStringscrollCompositionRecord.Cookie,
			}));
		Assert.True(MuiStringscrollStateFieldCursorCodec.TryWriteUInt32(ref platform,
			stateAddress, MuiStringscrollStateField.Magic, 0));
		Assert.True(MuiStringscrollPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
			policyAddress, MuiStringscrollPolicyField.Magic, 0));
		Assert.True(MuiStringscrollScrollbarFieldCursorCodec.TryWriteUInt32(ref platform,
			scrollbarAddress, MuiStringscrollScrollbarField.Magic, 0));
		Assert.True(MuiStringscrollCompositionFieldCursorCodec.TryWriteUInt32(
			ref platform, compositionAddress, MuiStringscrollCompositionField.Magic, 0));

		Assert.True(MuiStringscrollStateRecordCodec.TryReadStructural(ref platform,
			stateAddress, out var state));
		Assert.Equal(0u, state.Magic);
		Assert.False(MuiStringscrollStateRecordCodec.TryRead(ref platform,
			stateAddress, out _));
		Assert.True(MuiStringscrollPolicyRecordCodec.TryReadStructural(ref platform,
			policyAddress, out var policy));
		Assert.Equal(0u, policy.Magic);
		Assert.False(MuiStringscrollPolicyRecordCodec.TryRead(ref platform,
			policyAddress, out _));
		Assert.True(MuiStringscrollScrollbarRecordCodec.TryReadStructural(ref platform,
			scrollbarAddress, out var scrollbar));
		Assert.Equal(0u, scrollbar.Magic);
		Assert.False(MuiStringscrollScrollbarRecordCodec.TryRead(ref platform,
			scrollbarAddress, out _));
		Assert.True(MuiStringscrollCompositionRecordCodec.TryReadStructural(ref platform,
			compositionAddress, out var composition));
		Assert.Equal(0u, composition.Magic);
		Assert.False(MuiStringscrollCompositionRecordCodec.TryRead(ref platform,
			compositionAddress, out _));
	}

	[Fact]
	public void StringscrollPointerRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var recordAddress = APTR.FromPointer(0x1D20);
		var value = new MuiStringscrollPointerState
		{
			Magic = MuiStringscrollPointerState.Cookie,
			Axis = MuiStringscrollPointerState.HorizontalAxis,
			GrabOffset = -3,
			StartScroll = 17,
			StartX = 10,
			StartY = 20,
			LastPointer = 30,
			Flags = MuiStringscrollPointerState.ActiveFlag |
				MuiStringscrollPointerState.CapturedFlag,
		};
		Assert.True(MuiStringscrollPointerStateCodec.Write(ref platform,
			recordAddress, value));
		Assert.True(MuiStringscrollPointerStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, 24, out var pointerAddress));
		Assert.Equal(0x1D38u, pointerAddress.Raw);
		Assert.True(MuiStringscrollPointerStateRecordMemoryCodec.TryReadInt32(
			ref platform, recordAddress, 8, out var grabOffset));
		Assert.Equal(-3, grabOffset);
		Assert.True(MuiStringscrollPointerStateRecordMemoryCodec.TryWriteInt32(
			ref platform, recordAddress, 24, 0));
		Assert.True(MuiStringscrollPointerStateCodec.TryReadStructural(ref platform,
			recordAddress, out var decoded));
		Assert.Equal(0, decoded.LastPointer);
		Assert.False(MuiStringscrollPointerStateRecordMemoryCodec.TryGetAddress(
			ref platform, recordAddress, MuiStringscrollPointerState.Size, out _));
		Assert.False(MuiStringscrollPointerStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, 0, out _));
		Assert.False(MuiStringscrollPointerStateCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}
}
