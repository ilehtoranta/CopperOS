using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStringscrollLayoutAdmissionTests
{
	[Fact]
	public void StringscrollProjectionsUseNamedPackedLayouts()
	{
		Assert.Equal(20, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiStringscrollState>());
		Assert.Equal(0, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiStringscrollState>(nameof(MuiStringscrollState.String)).ToInt32());
		Assert.Equal(16, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiStringscrollState>(nameof(MuiStringscrollState.ScrollY)).ToInt32());

		Assert.Equal(28, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiStringscrollPolicyState>());
		Assert.Equal(24, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiStringscrollPolicyState>(nameof(MuiStringscrollPolicyState.VertScrollerOnly)).ToInt32());
		Assert.Equal(8, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiStringscrollScrollbarState>());
		Assert.Equal(4, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiStringscrollScrollbarState>(nameof(MuiStringscrollScrollbarState.VertBar)).ToInt32());

		Assert.Equal(12, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiStringscrollCompositionState>());
		Assert.Equal(8, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiStringscrollCompositionState>(nameof(MuiStringscrollCompositionState.OwnedMask)).ToInt32());
		Assert.Equal(16, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiStringscrollLayoutState>());
		Assert.Equal(12, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiStringscrollLayoutState>(nameof(MuiStringscrollLayoutState.Height)).ToInt32());
		Assert.Equal(12, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiStringscrollRenderState>());
		Assert.Equal(8, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiStringscrollRenderState>(nameof(MuiStringscrollRenderState.Font)).ToInt32());
		Assert.Equal(24, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiStringscrollViewportState>());
		Assert.Equal(20, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiStringscrollViewportState>(nameof(MuiStringscrollViewportState.MaxScrollY)).ToInt32());
		Assert.Equal(24, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiStringScrollMetricsState>());
		Assert.Equal(20, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiStringScrollMetricsState>(nameof(MuiStringScrollMetricsState.Top)).ToInt32());
	}

	[Fact]
	public void StringscrollLayoutRenderAndViewportRoundTrip()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var layoutAddress = APTR.FromPointer(0x1500);
		var renderAddress = APTR.FromPointer(0x1540);
		var viewportAddress = APTR.FromPointer(0x1560);
		var layout = new MuiStringscrollLayoutStateRecord
		{
			Magic = MuiStringscrollLayoutStateRecord.Cookie,
			Left = 12,
			Top = 7,
			Width = 320,
			Height = 180,
		};
		var render = new MuiStringscrollRenderStateRecord
		{
			Magic = MuiStringscrollRenderStateRecord.Cookie,
			RenderInfo = APTR.FromPointer(0x1800),
			RastPort = APTR.FromPointer(0x1840),
			Font = APTR.FromPointer(0x1880),
		};
		var viewport = new MuiStringscrollViewportStateRecord
		{
			Magic = MuiStringscrollViewportStateRecord.Cookie,
			ViewportWidth = 100,
			ViewportHeight = 80,
			HorizontalVisible = 1,
			VerticalVisible = 0,
			MaxScrollX = 220,
			MaxScrollY = 100,
		};
		Assert.True(MuiStringscrollLayoutStateRecordCodec.Write(ref platform,
			layoutAddress, layout));
		Assert.True(MuiStringscrollRenderStateRecordCodec.Write(ref platform,
			renderAddress, render));
		Assert.True(MuiStringscrollViewportStateRecordCodec.Write(ref platform,
			viewportAddress, viewport));
		Assert.True(MuiStringscrollLayoutStateRecordCodec.TryRead(ref platform,
			layoutAddress, out var readLayout));
		Assert.Equal(layout.Left, readLayout.Left);
		Assert.Equal(layout.Height, readLayout.Height);
		Assert.True(MuiStringscrollRenderStateRecordCodec.TryRead(ref platform,
			renderAddress, out var readRender));
		Assert.Equal(render.RenderInfo, readRender.RenderInfo);
		Assert.Equal(render.Font, readRender.Font);
		Assert.True(MuiStringscrollViewportStateRecordCodec.TryRead(ref platform,
			viewportAddress, out var readViewport));
		Assert.Equal(viewport.ViewportWidth, readViewport.ViewportWidth);
		Assert.Equal(viewport.MaxScrollY, readViewport.MaxScrollY);
	}

	[Fact]
	public void MalformedStringscrollLayoutCookiesRemainStructuralButFailClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var layoutAddress = APTR.FromPointer(0x1700);
		var renderAddress = APTR.FromPointer(0x1740);
		var viewportAddress = APTR.FromPointer(0x1760);
		Assert.True(MuiStringscrollLayoutStateRecordCodec.Write(ref platform,
			layoutAddress, new MuiStringscrollLayoutStateRecord
			{
				Magic = MuiStringscrollLayoutStateRecord.Cookie,
				Width = 1,
				Height = 2,
			}));
		Assert.True(MuiStringscrollRenderStateRecordCodec.Write(ref platform,
			renderAddress, new MuiStringscrollRenderStateRecord
			{
				Magic = MuiStringscrollRenderStateRecord.Cookie,
			}));
		Assert.True(MuiStringscrollViewportStateRecordCodec.Write(ref platform,
			viewportAddress, new MuiStringscrollViewportStateRecord
			{
				Magic = MuiStringscrollViewportStateRecord.Cookie,
			}));
		Assert.True(MuiStringscrollLayoutStateFieldCursorCodec.TryWriteUInt32(
			ref platform, layoutAddress, MuiStringscrollLayoutStateField.Magic, 0));
		Assert.True(MuiStringscrollRenderStateFieldCursorCodec.TryWriteUInt32(
			ref platform, renderAddress, MuiStringscrollRenderStateField.Magic, 0));
		Assert.True(MuiStringscrollViewportStateFieldCursorCodec.TryWriteUInt32(
			ref platform, viewportAddress, MuiStringscrollViewportStateField.Magic, 0));

		Assert.True(MuiStringscrollLayoutStateRecordCodec.TryReadStructural(ref platform,
			layoutAddress, out var layout));
		Assert.Equal(0u, layout.Magic);
		Assert.False(MuiStringscrollLayoutStateRecordCodec.TryRead(ref platform,
			layoutAddress, out _));
		Assert.True(MuiStringscrollRenderStateRecordCodec.TryReadStructural(ref platform,
			renderAddress, out var render));
		Assert.Equal(0u, render.Magic);
		Assert.False(MuiStringscrollRenderStateRecordCodec.TryRead(ref platform,
			renderAddress, out _));
		Assert.True(MuiStringscrollViewportStateRecordCodec.TryReadStructural(ref platform,
			viewportAddress, out var viewport));
		Assert.Equal(0u, viewport.Magic);
		Assert.False(MuiStringscrollViewportStateRecordCodec.TryRead(ref platform,
			viewportAddress, out _));
	}
}
