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
	public void StringscrollStateUsesNamedStructMemoryCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1A00);
		var state = new MuiStringscrollStateRecord
		{
			Magic = MuiStringscrollStateRecord.Cookie,
			String = APTR.FromPointer(0x1C00),
			ContentWidth = 800,
			ContentHeight = 32,
			ScrollX = 11,
			ScrollY = 7,
		};

		Assert.True(MuiStringscrollStateRecordCodec.Write(ref platform, address,
			state));
		Assert.True(MuiStringscrollStateMemoryCodec.TryGetAddress(ref platform,
			address, MuiStringscrollStateField.ScrollY, out var scrollYAddress));
		Assert.Equal(address.Raw + MuiStringscrollStateRecord.ScrollYOffset,
			scrollYAddress.Raw);
		Assert.True(MuiStringscrollStateMemoryCodec.TryReadUInt32(ref platform,
			address, MuiStringscrollStateField.ContentWidth, out var width));
		Assert.Equal(state.ContentWidth, width);
		Assert.True(MuiStringscrollStateMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiStringscrollStateField.ScrollX, 19));
		Assert.True(MuiStringscrollStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(19u, decoded.ScrollX);
		Assert.False(MuiStringscrollStateMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiStringscrollStateField.Magic, out _));
		Assert.False(MuiStringscrollStateMemoryCodec.TryGetAddress(ref platform,
			address, (MuiStringscrollStateField)0xFF, out _));
	}

	[Fact]
	public void StringscrollPolicyUsesNamedStructMemoryCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1B00);
		var policy = new MuiStringscrollPolicyRecord
		{
			Magic = MuiStringscrollPolicyRecord.Cookie,
			HorizBar = 1,
			NoInput = 0,
			SetMin = 1,
			SetVMin = 0,
			UseWinBorder = 1,
			VertBar = 1,
			VertScrollerOnly = 1,
		};

		Assert.True(MuiStringscrollPolicyRecordCodec.Write(ref platform, address,
			policy));
		Assert.True(MuiStringscrollPolicyMemoryCodec.TryGetAddress(ref platform,
			address, MuiStringscrollPolicyField.VertScrollerOnly,
			out var scrollerOnlyAddress));
		Assert.Equal(address.Raw + MuiStringscrollPolicyRecord.VertScrollerOnlyOffset,
			scrollerOnlyAddress.Raw);
		Assert.True(MuiStringscrollPolicyMemoryCodec.TryReadUInt32(ref platform,
			address, MuiStringscrollPolicyField.SetMin, out var setMin));
		Assert.Equal(policy.SetMin, setMin);
		Assert.True(MuiStringscrollPolicyMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiStringscrollPolicyField.NoInput, 1));
		Assert.True(MuiStringscrollPolicyRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(1u, decoded.NoInput);
		Assert.False(MuiStringscrollPolicyMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiStringscrollPolicyField.Magic, out _));
		Assert.False(MuiStringscrollPolicyMemoryCodec.TryGetAddress(ref platform,
			address, (MuiStringscrollPolicyField)0xFF, out _));
	}

	[Fact]
	public void StringscrollScrollbarUsesNamedStructMemoryCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1C40);
		var scrollbar = new MuiStringscrollScrollbarRecord
		{
			Magic = MuiStringscrollScrollbarRecord.Cookie,
			HorizBar = APTR.FromPointer(0x1D00),
			VertBar = APTR.FromPointer(0x1D40),
		};

		Assert.True(MuiStringscrollScrollbarRecordCodec.Write(ref platform,
			address, scrollbar));
		Assert.True(MuiStringscrollScrollbarMemoryCodec.TryGetAddress(
			ref platform, address, MuiStringscrollScrollbarField.VertBar,
			out var vertBarAddress));
		Assert.Equal(address.Raw + MuiStringscrollScrollbarRecord.VertBarOffset,
			vertBarAddress.Raw);
		Assert.True(MuiStringscrollScrollbarMemoryCodec.TryReadUInt32(
			ref platform, address, MuiStringscrollScrollbarField.HorizBar,
			out var horizBar));
		Assert.Equal(scrollbar.HorizBar.Raw, horizBar);
		Assert.True(MuiStringscrollScrollbarMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiStringscrollScrollbarField.VertBar,
			0x1D80));
		Assert.True(MuiStringscrollScrollbarRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(0x1D80u, decoded.VertBar.Raw);
		Assert.False(MuiStringscrollScrollbarMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiStringscrollScrollbarField.Magic, out _));
		Assert.False(MuiStringscrollScrollbarMemoryCodec.TryGetAddress(
			ref platform, address, (MuiStringscrollScrollbarField)0xFF, out _));
	}

	[Fact]
	public void StringscrollCompositionUsesNamedStructMemoryCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1D80);
		var composition = new MuiStringscrollCompositionRecord
		{
			Magic = MuiStringscrollCompositionRecord.Cookie,
			Horizontal = APTR.FromPointer(0x1E00),
			Vertical = APTR.FromPointer(0x1E40),
			OwnedMask = MuiStringscrollCompositionState.HorizontalOwned,
			LastHorizontalFirst = 4,
			LastVerticalFirst = 9,
		};

		Assert.True(MuiStringscrollCompositionRecordCodec.Write(ref platform,
			address, composition));
		Assert.True(MuiStringscrollCompositionMemoryCodec.TryGetAddress(
			ref platform, address, MuiStringscrollCompositionField.OwnedMask,
			out var ownedMaskAddress));
		Assert.Equal(address.Raw + MuiStringscrollCompositionRecord.OwnedMaskOffset,
			ownedMaskAddress.Raw);
		Assert.True(MuiStringscrollCompositionMemoryCodec.TryReadUInt32(
			ref platform, address, MuiStringscrollCompositionField.Vertical,
			out var vertical));
		Assert.Equal(composition.Vertical.Raw, vertical);
		Assert.True(MuiStringscrollCompositionMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiStringscrollCompositionField.LastVerticalFirst, 12));
		Assert.True(MuiStringscrollCompositionRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(12u, decoded.LastVerticalFirst);
		Assert.False(MuiStringscrollCompositionMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiStringscrollCompositionField.Magic, out _));
		Assert.False(MuiStringscrollCompositionMemoryCodec.TryGetAddress(
			ref platform, address, (MuiStringscrollCompositionField)0xFF, out _));
	}

	[Fact]
	public void StringscrollLayoutUsesNamedStructMemoryCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1E80);
		var layout = new MuiStringscrollLayoutStateRecord
		{
			Magic = MuiStringscrollLayoutStateRecord.Cookie,
			Left = -7,
			Top = 12,
			Width = 640,
			Height = 240,
		};

		Assert.True(MuiStringscrollLayoutStateRecordCodec.Write(ref platform,
			address, layout));
		Assert.True(MuiStringscrollLayoutStateMemoryCodec.TryGetAddress(
			ref platform, address, MuiStringscrollLayoutStateField.Left,
			out var leftAddress));
		Assert.Equal(address.Raw + MuiStringscrollLayoutStateRecord.LeftOffset,
			leftAddress.Raw);
		Assert.True(MuiStringscrollLayoutStateMemoryCodec.TryReadInt32(
			ref platform, address, MuiStringscrollLayoutStateField.Left,
			out var left));
		Assert.Equal(layout.Left, left);
		Assert.True(MuiStringscrollLayoutStateMemoryCodec.TryWriteInt32(
			ref platform, address, MuiStringscrollLayoutStateField.Top, -19));
		Assert.True(MuiStringscrollLayoutStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(-19, decoded.Top);
		Assert.False(MuiStringscrollLayoutStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiStringscrollLayoutStateField.Magic, out _));
		Assert.False(MuiStringscrollLayoutStateMemoryCodec.TryGetAddress(
			ref platform, address, (MuiStringscrollLayoutStateField)0xFF, out _));
	}

	[Fact]
	public void StringscrollRenderUsesNamedStructMemoryCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1F40);
		var render = new MuiStringscrollRenderStateRecord
		{
			Magic = MuiStringscrollRenderStateRecord.Cookie,
			RenderInfo = APTR.FromPointer(0x2000),
			RastPort = APTR.FromPointer(0x2040),
			Font = APTR.FromPointer(0x2080),
		};

		Assert.True(MuiStringscrollRenderStateRecordCodec.Write(ref platform,
			address, render));
		Assert.True(MuiStringscrollRenderStateMemoryCodec.TryGetAddress(
			ref platform, address, MuiStringscrollRenderStateField.Font,
			out var fontAddress));
		Assert.Equal(address.Raw + MuiStringscrollRenderStateRecord.FontOffset,
			fontAddress.Raw);
		Assert.True(MuiStringscrollRenderStateMemoryCodec.TryReadUInt32(
			ref platform, address, MuiStringscrollRenderStateField.RenderInfo,
			out var renderInfo));
		Assert.Equal(render.RenderInfo.Raw, renderInfo);
		Assert.True(MuiStringscrollRenderStateMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiStringscrollRenderStateField.RastPort,
			0x20C0));
		Assert.True(MuiStringscrollRenderStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(0x20C0u, decoded.RastPort.Raw);
		Assert.False(MuiStringscrollRenderStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiStringscrollRenderStateField.Magic, out _));
		Assert.False(MuiStringscrollRenderStateMemoryCodec.TryGetAddress(
			ref platform, address, (MuiStringscrollRenderStateField)0xFF, out _));
	}

	[Fact]
	public void StringscrollViewportUsesNamedStructMemoryCodec()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x20C0);
		var viewport = new MuiStringscrollViewportStateRecord
		{
			Magic = MuiStringscrollViewportStateRecord.Cookie,
			ViewportWidth = -3,
			ViewportHeight = 80,
			HorizontalVisible = 1,
			VerticalVisible = 0,
			MaxScrollX = 220,
			MaxScrollY = 100,
		};

		Assert.True(MuiStringscrollViewportStateRecordCodec.Write(ref platform,
			address, viewport));
		Assert.True(MuiStringscrollViewportStateMemoryCodec.TryGetAddress(
			ref platform, address, MuiStringscrollViewportStateField.MaxScrollY,
			out var maxScrollYAddress));
		Assert.Equal(address.Raw + MuiStringscrollViewportStateRecord.MaxScrollYOffset,
			maxScrollYAddress.Raw);
		Assert.True(MuiStringscrollViewportStateMemoryCodec.TryReadInt32(
			ref platform, address, MuiStringscrollViewportStateField.ViewportWidth,
			out var width));
		Assert.Equal(viewport.ViewportWidth, width);
		Assert.True(MuiStringscrollViewportStateMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiStringscrollViewportStateField.MaxScrollX, 300));
		Assert.True(MuiStringscrollViewportStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(300u, decoded.MaxScrollX);
		Assert.False(MuiStringscrollViewportStateMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiStringscrollViewportStateField.Magic,
			out _));
		Assert.False(MuiStringscrollViewportStateMemoryCodec.TryGetAddress(
			ref platform, address, (MuiStringscrollViewportStateField)0xFF,
			out _));
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
			ref platform, recordAddress, MuiStringscrollPointerStateField.LastPointer,
			out var typedPointerAddress));
		Assert.Equal(0x1D38u, typedPointerAddress.Raw);
		Assert.True(MuiStringscrollPointerStateRecordMemoryCodec.TryReadInt32(
			ref platform, recordAddress, MuiStringscrollPointerStateField.GrabOffset,
			out var typedGrabOffset));
		Assert.Equal(-3, typedGrabOffset);
		Assert.True(MuiStringscrollPointerStateRecordMemoryCodec.TryWriteInt32(
			ref platform, recordAddress, MuiStringscrollPointerStateField.LastPointer, 0));
		Assert.True(MuiStringscrollPointerStateCodec.TryReadStructural(ref platform,
			recordAddress, out var typedUpdated));
		Assert.Equal(0, typedUpdated.LastPointer);
		Assert.Equal(value.StartX, typedUpdated.StartX);
		Assert.Equal(value.Flags, typedUpdated.Flags);
		Assert.True(MuiStringscrollPointerStateRecordMemoryCodec.TryWriteInt32(
			ref platform, recordAddress, MuiStringscrollPointerStateField.LastPointer,
			value.LastPointer));
		Assert.False(MuiStringscrollPointerStateRecordMemoryCodec.TryReadUInt32(
			ref platform, recordAddress, (MuiStringscrollPointerStateField)255,
			out _));
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
			ref platform, APTR.Null, (uint)0, out _));
		Assert.False(MuiStringscrollPointerStateCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}
}
