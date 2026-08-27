using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewAdmissionTests
{
	[Fact]
	public void ListviewChildClickPolicySelectionAndLayoutRoundTripThroughStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var childAddress = APTR.FromPointer(0x1E00);
		var clickAddress = APTR.FromPointer(0x1E20);
		var policyAddress = APTR.FromPointer(0x1E40);
		var selectionAddress = APTR.FromPointer(0x1E60);
		var layoutAddress = APTR.FromPointer(0x1E80);
		Assert.True(MuiListviewCore.MuiListviewChildStateCodec.Write(ref platform,
			childAddress, new MuiListviewCore.MuiListviewChildState
			{
				Magic = MuiListviewCore.MuiListviewChildState.Cookie,
				Child = APTR.FromPointer(0x1F00),
			}));
		Assert.True(MuiListviewCore.MuiListviewClickStateCodec.Write(ref platform,
			clickAddress, new MuiListviewCore.MuiListviewClickState
			{
				Magic = MuiListviewCore.MuiListviewClickState.Cookie,
				ClickColumn = 3,
				DoubleClick = 4,
				AgainClick = 2,
				Clicks = 5,
				DefClickColumn = 1,
			}));
		Assert.True(MuiListviewCore.MuiListviewInteractionPolicyStateCodec.Write(
			ref platform, policyAddress,
			new MuiListviewCore.MuiListviewInteractionPolicyState
			{
				Magic = MuiListviewCore.MuiListviewInteractionPolicyState.Cookie,
				Input = 1,
				MultiSelect = 2,
				ScrollerPos = 3,
				DragType = 4,
			}));
		Assert.True(MuiListviewCore.MuiListviewSelectionSignalStateCodec.Write(
			ref platform, selectionAddress,
			new MuiListviewCore.MuiListviewSelectionSignalState
			{
				Magic = MuiListviewCore.MuiListviewSelectionSignalState.Cookie,
				Value = 9,
			}));
		Assert.True(MuiListviewCore.MuiListviewLayoutStateCodec.Write(ref platform,
			layoutAddress, new MuiListviewCore.MuiListviewLayoutState
			{
				Magic = MuiListviewCore.MuiListviewLayoutState.Cookie,
				Left = -4,
				Top = 8,
				Width = 320,
				Height = 180,
				ChildLeft = 2,
				ChildTop = 3,
				ChildWidth = 300,
				ChildHeight = 160,
			}));
		Assert.True(MuiListviewCore.MuiListviewChildStateCodec.TryRead(ref platform,
			childAddress, out var child));
		Assert.Equal(APTR.FromPointer(0x1F00), child.Child);
		Assert.True(MuiListviewCore.MuiListviewClickStateCodec.TryRead(ref platform,
			clickAddress, out var click));
		Assert.Equal(3u, click.ClickColumn);
		Assert.Equal(1u, click.DoubleClick);
		Assert.Equal(1u, click.AgainClick);
		Assert.True(MuiListviewCore.MuiListviewInteractionPolicyStateCodec.TryRead(
			ref platform, policyAddress, out var policy));
		Assert.Equal(4u, policy.DragType);
		Assert.True(MuiListviewCore.MuiListviewSelectionSignalStateCodec.TryRead(
			ref platform, selectionAddress, out var selection));
		Assert.Equal(9u, selection.Value);
		Assert.True(MuiListviewCore.MuiListviewLayoutStateCodec.TryRead(ref platform,
			layoutAddress, out var layout));
		Assert.Equal(-4, layout.Left);
		Assert.Equal(160, layout.ChildHeight);
	}

	[Fact]
	public void MalformedListviewStateCookiesRemainStructuralButFailClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var childAddress = APTR.FromPointer(0x2000);
		var clickAddress = APTR.FromPointer(0x2020);
		var policyAddress = APTR.FromPointer(0x2040);
		var selectionAddress = APTR.FromPointer(0x2060);
		var layoutAddress = APTR.FromPointer(0x2080);
		Assert.True(MuiListviewCore.MuiListviewChildStateCodec.Write(ref platform,
			childAddress, new MuiListviewCore.MuiListviewChildState
			{
				Magic = MuiListviewCore.MuiListviewChildState.Cookie,
			}));
		Assert.True(MuiListviewCore.MuiListviewClickStateCodec.Write(ref platform,
			clickAddress, new MuiListviewCore.MuiListviewClickState
			{
				Magic = MuiListviewCore.MuiListviewClickState.Cookie,
			}));
		Assert.True(MuiListviewCore.MuiListviewInteractionPolicyStateCodec.Write(
			ref platform, policyAddress,
			new MuiListviewCore.MuiListviewInteractionPolicyState
			{
				Magic = MuiListviewCore.MuiListviewInteractionPolicyState.Cookie,
			}));
		Assert.True(MuiListviewCore.MuiListviewSelectionSignalStateCodec.Write(
			ref platform, selectionAddress,
			new MuiListviewCore.MuiListviewSelectionSignalState
			{
				Magic = MuiListviewCore.MuiListviewSelectionSignalState.Cookie,
			}));
		Assert.True(MuiListviewCore.MuiListviewLayoutStateCodec.Write(ref platform,
			layoutAddress, new MuiListviewCore.MuiListviewLayoutState
			{
				Magic = MuiListviewCore.MuiListviewLayoutState.Cookie,
			}));
		Assert.True(MuiListviewCore.MuiListviewChildStateFieldCursorCodec.TryWriteUInt32(
			ref platform, childAddress, MuiListviewCore.MuiListviewChildStateField.Magic,
			0));
		Assert.True(MuiListviewCore.MuiListviewClickStateFieldCursorCodec.TryWriteUInt32(
			ref platform, clickAddress, MuiListviewCore.MuiListviewClickStateField.Magic,
			0));
		Assert.True(MuiListviewCore.MuiListviewInteractionPolicyFieldCursorCodec
			.TryWriteUInt32(ref platform, policyAddress,
				MuiListviewCore.MuiListviewInteractionPolicyField.Magic, 0));
		Assert.True(MuiListviewCore.MuiListviewSelectionSignalFieldCursorCodec
			.TryWriteUInt32(ref platform, selectionAddress,
				MuiListviewCore.MuiListviewSelectionSignalField.Magic, 0));
		Assert.True(MuiListviewCore.MuiListviewLayoutFieldCursorCodec.TryWriteUInt32(
			ref platform, layoutAddress, MuiListviewCore.MuiListviewLayoutField.Magic, 0));
		Assert.True(MuiListviewCore.MuiListviewChildStateCodec.TryReadStructural(ref platform,
			childAddress, out var child));
		Assert.Equal(0u, child.Magic);
		Assert.False(MuiListviewCore.MuiListviewChildStateCodec.TryRead(ref platform,
			childAddress, out _));
		Assert.True(MuiListviewCore.MuiListviewClickStateCodec.TryReadStructural(ref platform,
			clickAddress, out var click));
		Assert.Equal(0u, click.Magic);
		Assert.False(MuiListviewCore.MuiListviewClickStateCodec.TryRead(ref platform,
			clickAddress, out _));
		Assert.True(MuiListviewCore.MuiListviewInteractionPolicyStateCodec
			.TryReadStructural(ref platform, policyAddress, out var policy));
		Assert.Equal(0u, policy.Magic);
		Assert.False(MuiListviewCore.MuiListviewInteractionPolicyStateCodec.TryRead(
			ref platform, policyAddress, out _));
		Assert.True(MuiListviewCore.MuiListviewSelectionSignalStateCodec
			.TryReadStructural(ref platform, selectionAddress, out var selection));
		Assert.Equal(0u, selection.Magic);
		Assert.False(MuiListviewCore.MuiListviewSelectionSignalStateCodec.TryRead(
			ref platform, selectionAddress, out _));
		Assert.True(MuiListviewCore.MuiListviewLayoutStateCodec.TryReadStructural(ref platform,
			layoutAddress, out var layout));
		Assert.Equal(0u, layout.Magic);
		Assert.False(MuiListviewCore.MuiListviewLayoutStateCodec.TryRead(ref platform,
			layoutAddress, out _));
	}

	[Fact]
	public void ListviewRenderConnectionAndScrollerRoundTripThroughStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var renderAddress = APTR.FromPointer(0x2100);
		var connectionAddress = APTR.FromPointer(0x2120);
		var scrollerAddress = APTR.FromPointer(0x2140);
		Assert.True(MuiListviewCore.MuiListviewRenderStateCodec.Write(ref platform,
			renderAddress, new MuiListviewCore.MuiListviewRenderState
			{
				Magic = MuiListviewCore.MuiListviewRenderState.Cookie,
				RenderInfo = APTR.FromPointer(0x2200),
				RastPort = APTR.FromPointer(0x2240),
			}));
		Assert.True(MuiListviewCore.MuiListviewExternalScrollerConnectionStateCodec.Write(
			ref platform, connectionAddress,
			new MuiListviewCore.MuiListviewExternalScrollerConnectionState
			{
				Magic = MuiListviewCore.MuiListviewExternalScrollerConnectionState.Cookie,
				Prop = APTR.FromPointer(0x2280),
			}));
		Assert.True(MuiListviewCore.MuiListviewScrollerStateCodec.Write(ref platform,
			scrollerAddress, new MuiListviewCore.MuiListviewScrollerState
			{
				Magic = MuiListviewCore.MuiListviewScrollerState.Cookie,
				Entries = 100,
				Visible = 10,
				First = 4,
				MaxFirst = 90,
			}));
		Assert.True(MuiListviewCore.MuiListviewRenderStateCodec.TryRead(ref platform,
			renderAddress, out var render));
		Assert.Equal(APTR.FromPointer(0x2240), render.RastPort);
		Assert.True(MuiListviewCore.MuiListviewExternalScrollerConnectionStateCodec
			.TryRead(ref platform, connectionAddress, out var connection));
		Assert.Equal(APTR.FromPointer(0x2280), connection.Prop);
		Assert.True(MuiListviewCore.MuiListviewScrollerStateCodec.TryRead(ref platform,
			scrollerAddress, out var scroller));
		Assert.Equal(90u, scroller.MaxFirst);
	}

	[Fact]
	public void MalformedListviewRenderConnectionAndScrollerCookiesRemainStructural()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var renderAddress = APTR.FromPointer(0x2300);
		var connectionAddress = APTR.FromPointer(0x2320);
		var scrollerAddress = APTR.FromPointer(0x2340);
		Assert.True(MuiListviewCore.MuiListviewRenderStateCodec.Write(ref platform,
			renderAddress, new MuiListviewCore.MuiListviewRenderState
			{
				Magic = MuiListviewCore.MuiListviewRenderState.Cookie,
			}));
		Assert.True(MuiListviewCore.MuiListviewExternalScrollerConnectionStateCodec.Write(
			ref platform, connectionAddress,
			new MuiListviewCore.MuiListviewExternalScrollerConnectionState
			{
				Magic = MuiListviewCore.MuiListviewExternalScrollerConnectionState.Cookie,
			}));
		Assert.True(MuiListviewCore.MuiListviewScrollerStateCodec.Write(ref platform,
			scrollerAddress, new MuiListviewCore.MuiListviewScrollerState
			{
				Magic = MuiListviewCore.MuiListviewScrollerState.Cookie,
			}));
		Assert.True(MuiListviewCore.MuiListviewRenderFieldCursorCodec.TryWriteUInt32(
			ref platform, renderAddress, MuiListviewCore.MuiListviewRenderField.Magic, 0));
		Assert.True(MuiListviewCore.MuiListviewExternalScrollerConnectionFieldCursorCodec
			.TryWriteUInt32(ref platform, connectionAddress,
				MuiListviewCore.MuiListviewExternalScrollerConnectionField.Magic, 0));
		Assert.True(MuiListviewCore.MuiListviewScrollerFieldCursorCodec.TryWriteUInt32(
			ref platform, scrollerAddress, MuiListviewCore.MuiListviewScrollerField.Magic,
			0));
		Assert.True(MuiListviewCore.MuiListviewRenderStateCodec.TryReadStructural(
			ref platform, renderAddress, out var render));
		Assert.Equal(0u, render.Magic);
		Assert.False(MuiListviewCore.MuiListviewRenderStateCodec.TryRead(ref platform,
			renderAddress, out _));
		Assert.True(MuiListviewCore.MuiListviewExternalScrollerConnectionStateCodec
			.TryReadStructural(ref platform, connectionAddress, out var connection));
		Assert.Equal(0u, connection.Magic);
		Assert.False(MuiListviewCore.MuiListviewExternalScrollerConnectionStateCodec
			.TryRead(ref platform, connectionAddress, out _));
		Assert.True(MuiListviewCore.MuiListviewScrollerStateCodec.TryReadStructural(
			ref platform, scrollerAddress, out var scroller));
		Assert.Equal(0u, scroller.Magic);
		Assert.False(MuiListviewCore.MuiListviewScrollerStateCodec.TryRead(ref platform,
			scrollerAddress, out _));
	}
}
