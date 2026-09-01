using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

// Focused MG08 coverage for the MorphOS Stringscroll baseline. The tests keep
// the public string caller-owned, verify the private guest copy, exercise the
// bounded pixel scroll state and min/max policy, and route layout/draw/set
// packets through the same dispatcher used by the native root.
public sealed class MuiStringscrollTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint Width = 0x8042b59cu;
	private const uint Height = 0x80423237u;
	private const uint RenderInfo = 0x7fff0001u;
	private const uint Font = 0x8042be50u;
	private const uint ContentWidthStateKey = 0x0f110003u;
	private const uint ScrollXStateKey = 0x0f110004u;
	private const uint ScrollYStateKey = 0x0f110005u;
	private const uint PolicyStateKey = 0x0f110007u;
	private const uint CompositionStateKey = 0x0f11000du;
	private const uint LayoutStateKey = 0x0f110009u;
	private const uint RenderStateKey = 0x0f11000au;
	private const uint ViewportStateKey = 0x0f11000bu;
	private const uint Layout = 0x8042845bu;
	private const uint Draw = 0x80426f3fu;
	private const uint AskMinMax = 0x80423874u;
	private const uint Set = 0x8042549au;
	private const int KeyPageDown = 5;
	private const int KeyRight = 9;
	private const int KeyHome = 6;
	private const int KeyEnd = 7;
	private const int KeyRelease = -2;
	private const int KeyNone = -1;
	private const uint IdcmpMouseButtons = 1u << 3;
	private const uint IdcmpMouseMove = 1u << 2;
	private const ushort SelectDown = 0x0068;
	private const ushort SelectUp = 0x0069;

	[Fact]
	public void ClassifierAndConstructionOwnTheString()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5000);
		platform.WriteCString(source, "CopperOS Stringscroll");
		var tags = APTR.FromPointer(0x3000);
		platform.WriteUInt32(tags, 0, MuiStringscrollCore.String);
		platform.WriteUInt32(tags, 4, source.Raw);
		platform.WriteUInt32(tags, 8, 0);
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, tags);
		Assert.NotEqual(APTR.Null, obj);
		Assert.Equal(MuiCollectionClass.Stringscroll, MuiListCore.Classify(ref
			platform, State, obj));
		Assert.True(MuiStringscrollCore.GetAttribute(ref platform, State, obj,
			MuiStringscrollCore.String, out var owned));
		Assert.NotEqual(source.Raw, owned);
		Assert.Equal("CopperOS Stringscroll", ReadCString(ref platform,
			APTR.FromPointer(owned)));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void NamedStateTracksOwnedTextMetricsAndPixelScroll()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5050);
		platform.WriteCString(source, "0123456789\nxy");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			24, 16));
		Assert.True(MuiStringscrollCore.TryReadState(ref platform, State, obj,
			out var scrollState));
		Assert.NotEqual(APTR.Null, scrollState.String);
		Assert.Equal(80u, scrollState.ContentWidth);
		Assert.Equal(16u, scrollState.ContentHeight);
		Assert.Equal(0u, scrollState.ScrollX);
		Assert.Equal(0u, scrollState.ScrollY);

		Assert.True(MuiStringscrollCore.SetScroll(ref platform, State, obj, 8,
			8));
		Assert.True(MuiStringscrollCore.TryReadState(ref platform, State, obj,
			out scrollState));
		Assert.Equal(8u, scrollState.ScrollX);
		Assert.Equal(8u, scrollState.ScrollY);
		Assert.Equal("0123456789\nxy", ReadCString(ref platform,
			scrollState.String));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void StringscrollStateUsesNamedGuestRecord()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x50B0);
		platform.WriteCString(source, "state record text");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			32, 16));
		Assert.True(MuiStringscrollCore.TryGetStateRecord(ref platform, State, obj,
			out var record));
		Assert.Equal(MuiStringscrollStateRecord.Cookie, record.Magic);
		Assert.NotEqual(APTR.Null, record.String);
		Assert.NotEqual(0u, record.ContentWidth);

		// A generic raw write does not replace the canonical typed state. The
		// class-aware setter synchronizes the record before recomputing metrics.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.String, 0, false));
		Assert.True(MuiStringscrollCore.TryReadState(ref platform, State, obj,
			out var state));
		Assert.NotEqual(APTR.Null, state.String);
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.String, 0, false));
		Assert.True(MuiStringscrollCore.TryGetStateRecord(ref platform, State, obj,
			out record));
		Assert.Equal(APTR.Null, record.String);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void NamedLayoutStateTracksSignedAreaGeometry()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5070);
		platform.WriteCString(source, "geometry text");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, -12, -4,
			40, 24));
		Assert.True(MuiStringscrollCore.TryReadLayoutState(ref platform, State,
			obj, out var layout));
		Assert.Equal(-12, layout.Left);
		Assert.Equal(-4, layout.Top);
		Assert.Equal(40, layout.Width);
		Assert.Equal(24, layout.Height);
		Assert.True(MuiStringscrollCore.TryReadState(ref platform, State, obj,
			out var scrollState));
		Assert.Equal(104u, scrollState.ContentWidth);
		Assert.Equal(8u, scrollState.ContentHeight);
		Assert.True(MuiStringscrollCore.SetScroll(ref platform, State, obj, 16,
			0));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var x, out _, out var maxX, out _));
		Assert.Equal(16, x);
		Assert.True(maxX > x);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void StringscrollLayoutUsesNamedGuestRecord()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5080);
		platform.WriteCString(source, "layout record text");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, -8, -6,
			48, 20));
		Assert.True(MuiStringscrollCore.TryGetLayoutRecord(ref platform, State, obj,
			out var record));
		Assert.Equal(MuiStringscrollLayoutStateRecord.Cookie, record.Magic);
		Assert.Equal(-8, record.Left);
		Assert.Equal(-6, record.Top);
		Assert.Equal(48, record.Width);
		Assert.Equal(20, record.Height);

		// A generic raw Area write does not replace the canonical typed geometry;
		// the Stringscroll class-aware setter updates both projections.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			Width, 12, false));
		Assert.True(MuiStringscrollCore.TryReadLayoutState(ref platform, State, obj,
			out var layout));
		Assert.Equal(48, layout.Width);
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			Width, 52, false));
		Assert.True(MuiStringscrollCore.TryGetLayoutRecord(ref platform, State, obj,
			out record));
		Assert.Equal(52, record.Width);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void StringscrollSetterSynchronizesSharedAreaGeometry()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5090);
		platform.WriteCString(source, "shared geometry");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 2, 3,
			40, 20));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			Width, 56));
		Assert.True(MuiAreaLayoutCore.TryGetGeometryStateRecord(ref platform,
			State, obj, out var geometry));
		Assert.Equal(56, geometry.Width);
		Assert.Equal(20, geometry.Height);
		Assert.True(MuiStringscrollCore.TryGetLayoutRecord(ref platform, State, obj,
			out var layout));
		Assert.Equal(56, layout.Width);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void NamedRenderStateDecodesRenderInfoAndFontPointers()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x53A0);
		var renderInfo = APTR.FromPointer(0x53C0);
		var rastPort = APTR.FromPointer(0x53E0);
		platform.WriteCString(source, "render state");
		platform.WriteUInt32(renderInfo, 20, rastPort.Raw);
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			RenderInfo, renderInfo.Raw, false));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 2, 3,
			64, 16));
		Assert.True(MuiStringscrollCore.TryReadRenderState(ref platform, State,
			obj, out var renderState));
		Assert.Equal(renderInfo.Raw, renderState.RenderInfo.Raw);
		Assert.Equal(rastPort.Raw, renderState.RastPort.Raw);
		Assert.Equal(0u, renderState.Font.Raw);
		Assert.True(MuiStringscrollCore.Draw(ref platform, State, obj, 0));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void StringscrollRenderUsesNamedGuestRecord()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x53B0);
		var renderInfo = APTR.FromPointer(0x53D0);
		var rastPort = APTR.FromPointer(0x53F0);
		var font = APTR.FromPointer(0x5410);
		platform.WriteCString(source, "render record");
		platform.WriteUInt32(renderInfo, 20, rastPort.Raw);
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			RenderInfo, renderInfo.Raw, false));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			0x8042be50u, font.Raw, false));
		Assert.True(MuiStringscrollCore.TryGetRenderRecord(ref platform, State, obj,
			out var record));
		Assert.Equal(MuiStringscrollRenderStateRecord.Cookie, record.Magic);
		Assert.Equal(renderInfo.Raw, record.RenderInfo.Raw);
		Assert.Equal(rastPort.Raw, record.RastPort.Raw);
		Assert.Equal(font.Raw, record.Font.Raw);

		// A generic raw RenderInfo write does not replace the canonical typed
		// context; the class-aware setter updates the record and its validation.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			RenderInfo, 0, false));
		Assert.True(MuiStringscrollCore.TryReadRenderState(ref platform, State, obj,
			out var renderState));
		Assert.Equal(renderInfo.Raw, renderState.RenderInfo.Raw);
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			RenderInfo, 0, false));
		Assert.True(MuiStringscrollCore.TryGetRenderRecord(ref platform, State, obj,
			out record));
		Assert.Equal(APTR.Null, record.RenderInfo);
		Assert.Equal(APTR.Null, record.RastPort);
		Assert.False(MuiStringscrollCore.TryReadRenderState(ref platform, State, obj,
			out _));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void NamedViewportStateTracksBarReservationsAndScrollBounds()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5410);
		platform.WriteCString(source,
			"012345678901234567890123456789\nline two");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			40, 24));
		Assert.True(MuiStringscrollCore.TryReadViewportState(ref platform, State,
			obj, out var viewport));
		Assert.Equal(28, viewport.ViewportWidth);
		Assert.Equal(12, viewport.ViewportHeight);
		Assert.Equal(1u, viewport.HorizontalVisible);
		Assert.Equal(1u, viewport.VerticalVisible);
		Assert.Equal(212u, viewport.MaxScrollX);
		Assert.Equal(4u, viewport.MaxScrollY);

		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.UseWinBorder, 1));
		Assert.True(MuiStringscrollCore.TryReadViewportState(ref platform, State,
			obj, out viewport));
		Assert.Equal(40, viewport.ViewportWidth);
		Assert.Equal(24, viewport.ViewportHeight);
		Assert.Equal(0u, viewport.HorizontalVisible);
		Assert.Equal(0u, viewport.VerticalVisible);
		Assert.Equal(200u, viewport.MaxScrollX);
		Assert.Equal(0u, viewport.MaxScrollY);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void StringscrollViewportUsesNamedGuestRecord()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5420);
		platform.WriteCString(source,
			"012345678901234567890123456789\nline two");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			40, 24));
		Assert.True(MuiStringscrollCore.TryGetViewportRecord(ref platform, State,
			obj, out var record));
		Assert.Equal(MuiStringscrollViewportStateRecord.Cookie, record.Magic);
		Assert.Equal(28, record.ViewportWidth);
		Assert.Equal(12, record.ViewportHeight);
		Assert.Equal(1u, record.HorizontalVisible);
		Assert.Equal(1u, record.VerticalVisible);
		Assert.Equal(212u, record.MaxScrollX);
		Assert.Equal(4u, record.MaxScrollY);

		// A generic geometry write does not replace the canonical derived
		// viewport. The class-aware setter recomputes and republishes it.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			Width, 12, false));
		Assert.True(MuiStringscrollCore.TryReadViewportState(ref platform, State,
			obj, out var viewport));
		Assert.Equal(28, viewport.ViewportWidth);
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			Width, 52, false));
		Assert.True(MuiStringscrollCore.TryGetViewportRecord(ref platform, State,
			obj, out record));
		Assert.Equal(40, record.ViewportWidth);
		Assert.Equal(12, record.ViewportHeight);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void NamedPolicyStateCanonicalizesFlagsAndDrivesInputPolicy()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5060);
		platform.WriteCString(source, "policy text");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.HorizBar, 7));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.NoInput, 0));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.SetMin, 9));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.SetVMin, 3));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.UseWinBorder, 5));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.VertBar, 4));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.VertScrollerOnly, 6));
		Assert.True(MuiStringscrollCore.TryReadPolicyState(ref platform, State,
			obj, out var policy));
		Assert.Equal(1u, policy.HorizBar);
		Assert.Equal(0u, policy.NoInput);
		Assert.Equal(1u, policy.SetMin);
		Assert.Equal(1u, policy.SetVMin);
		Assert.Equal(1u, policy.UseWinBorder);
		Assert.Equal(1u, policy.VertBar);
		Assert.Equal(1u, policy.VertScrollerOnly);

		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.NoInput, 2));
		Assert.False(MuiStringscrollCore.SetScroll(ref platform, State, obj, 8,
			0));
		Assert.True(MuiStringscrollCore.TryReadPolicyState(ref platform, State,
			obj, out policy));
		Assert.Equal(1u, policy.NoInput);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void StringscrollPreservesInitializerScrollbarObjectPointers()
	{
		var platform = CreatePlatform(out var stringClass);
		var tags = APTR.FromPointer(0x3000);
		var horizontalBar = APTR.FromPointer(0x5A00);
		var verticalBar = APTR.FromPointer(0x5A40);
		platform.WriteUInt32(tags, 0, MuiStringscrollCore.HorizBar);
		platform.WriteUInt32(tags, 4, horizontalBar.Raw);
		platform.WriteUInt32(tags, 8, MuiStringscrollCore.VertBar);
		platform.WriteUInt32(tags, 12, verticalBar.Raw);
		platform.WriteUInt32(tags, 16, 0);
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, tags);
		Assert.NotEqual(APTR.Null, obj);

		Assert.True(MuiStringscrollCore.TryReadScrollbarState(ref platform, State,
			obj, out var scrollbars));
		Assert.Equal(horizontalBar, scrollbars.HorizBar);
		Assert.Equal(verticalBar, scrollbars.VertBar);
		Assert.True(MuiStringscrollCore.TryReadPolicyState(ref platform, State,
			obj, out var policy));
		Assert.Equal(1u, policy.HorizBar);
		Assert.Equal(1u, policy.VertBar);

		// A later class-aware initializer write updates the typed pointer record,
		// while policy consumers continue to see a canonical BOOL.
		var replacement = APTR.FromPointer(0x5A80);
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.HorizBar, replacement.Raw));
		Assert.True(MuiStringscrollCore.TryReadScrollbarState(ref platform, State,
			obj, out scrollbars));
		Assert.Equal(replacement, scrollbars.HorizBar);
		Assert.True(MuiStringscrollCore.TryReadPolicyState(ref platform, State,
			obj, out policy));
		Assert.Equal(1u, policy.HorizBar);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void StringscrollBuildsTypedAutomaticScrollbarChildren()
	{
		var platform = CreatePlatform(out var stringClass);
		var scrollbarName = APTR.FromPointer(0x1200);
		platform.WriteCString(scrollbarName, "scrollbar.mui");
		var scrollbarClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			scrollbarName, APTR.Null, 0, APTR.FromPointer(1), false);
		Assert.NotEqual(APTR.Null, scrollbarClass);
		var source = APTR.FromPointer(0x5B00);
		platform.WriteCString(source,
			"0123456789012345678901234567890123456789\nsecond line");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.NotEqual(APTR.Null, obj);
		Assert.True(MuiStringscrollCore.TryReadCompositionState(ref platform, State,
			obj, out var composition));
		Assert.NotEqual(APTR.Null, composition.Horizontal);
		Assert.NotEqual(APTR.Null, composition.Vertical);
		Assert.Equal(MuiStringscrollCompositionState.HorizontalOwned |
			MuiStringscrollCompositionState.VerticalOwned, composition.OwnedMask);
		Assert.Equal(MuiControlClass.Scrollbar, MuiCommonControlCore.Classify(
			ref platform, State, composition.Horizontal));
		Assert.Equal(MuiControlClass.Scrollbar, MuiCommonControlCore.Classify(
			ref platform, State, composition.Vertical));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			composition.Horizontal, MuiCommonControlCore.GroupHoriz,
			out var horizontalGroup));
		Assert.Equal(1u, horizontalGroup);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			composition.Vertical, MuiCommonControlCore.GroupHoriz,
			out var verticalGroup));
		Assert.Equal(0u, verticalGroup);

		var renderInfo = APTR.FromPointer(0x5B80);
		platform.WriteUInt32(renderInfo, 20, 0x5BC0);
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			RenderInfo, renderInfo.Raw, false));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			40, 24));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			composition.Horizontal, MuiCommonControlCore.PropEntries,
			out var horizontalEntries));
		Assert.True(horizontalEntries > 0);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			composition.Vertical, MuiCommonControlCore.PropEntries,
			out var verticalEntries));
		Assert.True(verticalEntries > 0);
		Assert.True(MuiStringscrollCore.Draw(ref platform, State, obj, 0));
		var horizontalChild = composition.Horizontal;
		var verticalChild = composition.Vertical;
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State, obj));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			stringClass));
		Assert.Equal(APTR.Null, MuiHeadlessObjectCore.FindObject(ref platform,
			State, horizontalChild));
		Assert.Equal(APTR.Null, MuiHeadlessObjectCore.FindObject(ref platform,
			State, verticalChild));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			scrollbarClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void StringscrollAutomaticScrollbarTagWriterUsesNamedCursor()
	{
		var platform = CreatePlatform(out _);
		var tags = APTR.FromPointer(0x5D00);
		Assert.True(MuiStringscrollCore.WriteAutomaticScrollbarTagRecords(
			ref platform, tags, true));
		var cursor = default(MuiAslTagItemCursor);
		cursor.Base = tags;
		cursor.Index = 0;
		Assert.True(MuiAslTagItemVectorCodec.TryRead(ref platform, cursor,
			out var group));
		Assert.Equal(MuiCommonControlCore.GroupHoriz, group.Tag);
		Assert.Equal(1u, group.Data);
		Assert.True(MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1));
		Assert.True(MuiAslTagItemVectorCodec.TryRead(ref platform, cursor,
			out var done));
		Assert.Equal(MuiAslTagListCore.TagDone, done.Tag);
	}

	[Fact]
	public void ComposedScrollbarChildrenRouteArrowAndThumbInput()
	{
		var platform = CreatePlatform(out var stringClass);
		var scrollbarName = APTR.FromPointer(0x1300);
		platform.WriteCString(scrollbarName, "scrollbar.mui");
		var scrollbarClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			scrollbarName, APTR.Null, 0, APTR.FromPointer(1), false);
		Assert.NotEqual(APTR.Null, scrollbarClass);
		var source = APTR.FromPointer(0x5C00);
		platform.WriteCString(source,
			"01234567890123456789012345678901234567890123456789\nsecond line\nthird line");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.NotEqual(APTR.Null, obj);
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			80, 32));
		Assert.True(MuiStringscrollCore.TryReadCompositionState(ref platform, State,
			obj, out var composition));
		Assert.True(MuiAreaLayoutCore.TryReadGeometryState(ref platform, State,
			composition.Horizontal, out var horizontalGeometry));
		Assert.True(MuiCommonControlCore.TryReadScrollbarLayoutState(ref platform,
			State, composition.Horizontal, out var horizontalLayout));
		Assert.Equal(1u, horizontalLayout.Horizontal);
		Assert.Equal(0u, horizontalLayout.Type);

		var intui = APTR.FromPointer(0x5C80);
		var packet = APTR.FromPointer(0x5D80);
		var arrowX = horizontalGeometry.Left + horizontalGeometry.Width - 8;
		var arrowY = horizontalGeometry.Top + horizontalGeometry.Height / 2;
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, unchecked((short)arrowX),
			unchecked((short)arrowY)));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var afterArrowX, out var afterArrowY, out _, out _));
		Assert.True(afterArrowX > 0);
		Assert.Equal(0, afterArrowY);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			composition.Horizontal, MuiCommonControlCore.PropFirst,
			out var childAfterArrow));
		Assert.Equal((uint)afterArrowX, childAfterArrow);

		var propLength = horizontalGeometry.Width - 32;
		var trackX = horizontalGeometry.Left + 16 + propLength / 2;
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, unchecked((short)trackX),
			unchecked((short)arrowY)));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var afterTrackX, out _, out _, out _));
		Assert.True(afterTrackX > afterArrowX);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			composition.Horizontal, MuiCommonControlCore.PropFirst,
			out var childAfterTrack));
		Assert.Equal((uint)afterTrackX, childAfterTrack);

		// The typed child range is the source of the thumb geometry. A drag is
		// routed through the same child hit region and must update Stringscroll's
		// bounded pixel scroll state, without reading an ABI offset.
		Assert.True(MuiCommonControlCore.TryReadPropRangeState(ref platform, State,
			composition.Horizontal, out var range));
		Assert.True(propLength > 8);
		var knob = (int)(range.Visible * (uint)propLength / range.Entries);
		if (knob <= 0) knob = 1;
		var offset = (int)(range.First * (uint)propLength / range.Entries);
		var thumbX = horizontalGeometry.Left + 16 + offset + knob / 2;
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectDown, 0, 0, unchecked((short)thumbX),
			unchecked((short)arrowY)));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseMove, 0, 0, 0, unchecked((short)(thumbX + 20)),
			unchecked((short)arrowY)));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var afterDragX, out _, out var maxX, out _));
		Assert.True(afterDragX > afterTrackX && afterDragX <= maxX);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			composition.Horizontal, MuiCommonControlCore.PropFirst,
			out var childAfterDrag));
		Assert.Equal((uint)afterDragX, childAfterDrag);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, unchecked((short)(thumbX + 20)),
			unchecked((short)arrowY)));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State, obj));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			stringClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			scrollbarClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void ComposedScrollbarChildFirstChangesAreAdoptedAndResynchronized()
	{
		var platform = CreatePlatform(out var stringClass);
		var scrollbarName = APTR.FromPointer(0x1400);
		platform.WriteCString(scrollbarName, "scrollbar.mui");
		var scrollbarClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			scrollbarName, APTR.Null, 0, APTR.FromPointer(1), false);
		Assert.NotEqual(APTR.Null, scrollbarClass);
		var source = APTR.FromPointer(0x5E00);
		platform.WriteCString(source,
			"01234567890123456789012345678901234567890123456789");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.NotEqual(APTR.Null, obj);
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			80, 32));
		Assert.True(MuiStringscrollCore.TryReadCompositionState(ref platform, State,
			obj, out var composition));

		var handled = false;
		Assert.True(MuiCommonControlCore.TrySetHeadlessPropAttribute(ref platform,
			State, composition.Horizontal, MuiCommonControlCore.PropFirst, 24,
			false, out handled));
		Assert.True(handled);
		Assert.True(MuiStringscrollCore.Recompute(ref platform, State, obj));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var adoptedX, out var adoptedY, out _, out _));
		Assert.Equal(24, adoptedX);
		Assert.Equal(0, adoptedY);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			composition.Horizontal, MuiCommonControlCore.PropFirst,
			out var synchronizedX));
		Assert.Equal(24u, synchronizedX);

		var destination = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			stringClass, APTR.Null);
		var follow = APTR.FromPointer(0x6200);
		platform.WriteUInt32(follow, 0, 0x90000001);
		Assert.True(MuiNotifyCore.Add(ref platform, State, composition.Horizontal,
			MuiCommonControlCore.PropFirst, 8, destination, 1, follow));
		var before = platform.DispatchCount;
		Assert.True(MuiStringscrollCore.SetScroll(ref platform, State, obj, 8, 0));
		Assert.Equal(before + 1, platform.DispatchCount);
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var parentX, out _, out _, out _));
		Assert.Equal(8, parentX);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			composition.Horizontal, MuiCommonControlCore.PropFirst,
			out var parentPublishedX));
		Assert.Equal(8u, parentPublishedX);

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State, obj));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			destination));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			stringClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			scrollbarClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void StringscrollPolicyUsesNamedGuestRecord()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x50A0);
		platform.WriteCString(source, "typed policy");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.TryGetPolicyRecord(ref platform, State, obj,
			out var record));
		Assert.Equal(MuiStringscrollPolicyRecord.Cookie, record.Magic);
		Assert.Equal(1u, record.HorizBar);
		Assert.Equal(1u, record.VertBar);

		// Bypassing the class-aware setter does not replace the canonical guest
		// policy; a normal setter is the synchronization boundary.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.HorizBar, 0, false));
		Assert.True(MuiStringscrollCore.TryReadPolicyState(ref platform, State, obj,
			out var policy));
		Assert.Equal(1u, policy.HorizBar);
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.HorizBar, 0, false));
		Assert.True(MuiStringscrollCore.TryGetPolicyRecord(ref platform, State, obj,
			out record));
		Assert.Equal(0u, record.HorizBar);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void StringscrollPolicyGettersPreferNamedRecord()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5120);
		platform.WriteCString(source, "policy getter text");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.NotEqual(APTR.Null, obj);

		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.HorizBar, 0));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.NoInput, 1));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.SetMin, 1));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.SetVMin, 1));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.UseWinBorder, 1));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.VertBar, 0));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.VertScrollerOnly, 1));

		Assert.True(MuiStringscrollCore.TryGetPolicyRecord(ref platform, State, obj,
			out var record));
		Assert.Equal(0u, record.HorizBar);
		Assert.Equal(1u, record.NoInput);
		Assert.Equal(1u, record.SetMin);
		Assert.Equal(1u, record.SetVMin);
		Assert.Equal(1u, record.UseWinBorder);
		Assert.Equal(0u, record.VertBar);
		Assert.Equal(1u, record.VertScrollerOnly);

		// Deliberately stale scalar nodes must not replace the canonical policy
		// record used by public generic Get and direct Stringscroll reads.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.HorizBar, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.NoInput, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.SetMin, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.SetVMin, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.UseWinBorder, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.VertBar, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.VertScrollerOnly, 0, false));

		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiStringscrollCore.HorizBar, out var horizBar));
		Assert.Equal(0u, horizBar);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiStringscrollCore.NoInput, out var noInput));
		Assert.Equal(1u, noInput);
		Assert.True(MuiStringscrollCore.GetAttribute(ref platform, State, obj,
			MuiStringscrollCore.VertScrollerOnly, out var scrollerOnly));
		Assert.Equal(1u, scrollerOnly);

		var getPacket = APTR.FromPointer(0x5600);
		var getStorage = APTR.FromPointer(0x5640);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getPacket, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getPacket, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			MuiStringscrollCore.NoInput));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			getPacket, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			getStorage.Raw));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			getPacket));
		Assert.Equal(1u, platform.ReadUInt32(getStorage, 0));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void SetStringCopiesAndNullClearsWithoutLeaking()
	{
		var platform = CreatePlatform(out var stringClass);
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, APTR.Null);
		var replacement = APTR.FromPointer(0x5100);
		platform.WriteCString(replacement, "updated");
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.String, replacement.Raw, false));
		Assert.Equal("updated", ReadCString(ref platform, APTR.FromPointer(
			Get(ref platform, obj, MuiStringscrollCore.String))));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.String, 0));
		Assert.Equal(0u, Get(ref platform, obj, MuiStringscrollCore.String));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void SetStringRejectsMalformedNamedStateBeforeReplacingOwnedBuffer()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5120);
		var replacement = APTR.FromPointer(0x5160);
		platform.WriteCString(source, "original");
		platform.WriteCString(replacement, "replacement");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.NotEqual(APTR.Null, obj);
		var original = Get(ref platform, obj, MuiStringscrollCore.String);

		// This is the named CopperOS store identity for the typed Stringscroll
		// state record, not an object-layout offset.  Shrinking it simulates a
		// malformed guest record at the setter boundary.
		const uint stateRecordKey = 0x0f110008u;
		Assert.True(MuiStoreCore.DataspaceResize(ref platform, State, obj,
			stateRecordKey, 4));
		Assert.False(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.String, replacement.Raw, false));
		Assert.Equal(original, Get(ref platform, obj, MuiStringscrollCore.String));
		Assert.Equal("original", ReadCString(ref platform, APTR.FromPointer(original)));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void SetStringRejectsMalformedLayoutBeforeReplacingOwnedBuffer()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5180);
		var replacement = APTR.FromPointer(0x51C0);
		platform.WriteCString(source, "layout-original");
		platform.WriteCString(replacement, "layout-replacement");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.NotEqual(APTR.Null, obj);
		var original = Get(ref platform, obj, MuiStringscrollCore.String);

		// The layout record is a named typed state block.  A malformed dependent
		// record must be rejected before String ownership is replaced because the
		// successful setter path ends by recomputing the scroll viewport.
		Assert.True(MuiStoreCore.DataspaceResize(ref platform, State, obj,
			LayoutStateKey, 4));
		Assert.False(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.String, replacement.Raw, false));
		Assert.Equal(original, Get(ref platform, obj, MuiStringscrollCore.String));
		Assert.Equal("layout-original", ReadCString(ref platform,
			APTR.FromPointer(original)));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void SetGeometryRejectsMalformedNamedStateBeforeRawMutation()
	{
		var platform = CreatePlatform(out var stringClass);
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, APTR.Null);
		Assert.NotEqual(APTR.Null, obj);
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			Width, 80, false));
		var original = Get(ref platform, obj, Width);

		// This is the named CopperOS store identity for the typed layout record,
		// not an object-layout offset.  Shrinking it simulates malformed guest
		// state at the typed setter boundary.
		const uint layoutStateKey = 0x0f110009u;
		Assert.True(MuiStoreCore.DataspaceResize(ref platform, State, obj,
			layoutStateKey, 4));
		Assert.False(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			Width, 120, false));
		Assert.Equal(original, Get(ref platform, obj, Width));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void SetRenderContextRejectsMalformedNamedStateBeforeRawMutation()
	{
		var platform = CreatePlatform(out var stringClass);
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, APTR.Null);
		Assert.NotEqual(APTR.Null, obj);
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			Font, 0x6300, false));
		var original = Get(ref platform, obj, Font);

		// This is the named CopperOS store identity for the typed render record,
		// not an object-layout offset.  Shrinking it simulates malformed guest
		// state at the typed setter boundary.
		const uint renderStateKey = 0x0f11000au;
		Assert.True(MuiStoreCore.DataspaceResize(ref platform, State, obj,
			renderStateKey, 4));
		Assert.False(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			Font, 0x6400, false));
		Assert.Equal(original, Get(ref platform, obj, Font));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void SetPolicyRejectsMalformedNamedStateBeforeRawMutation()
	{
		var platform = CreatePlatform(out var stringClass);
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, APTR.Null);
		Assert.NotEqual(APTR.Null, obj);
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.NoInput, 1, false));
		var original = Get(ref platform, obj, MuiStringscrollCore.NoInput);

		// This is the named CopperOS store identity for the typed policy record,
		// not an object-layout offset.  Shrinking it simulates malformed guest
		// state at the typed setter boundary.
		const uint policyStateKey = 0x0f110007u;
		Assert.True(MuiStoreCore.DataspaceResize(ref platform, State, obj,
			policyStateKey, 4));
		Assert.False(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.NoInput, 0, false));
		Assert.False(MuiStringscrollCore.GetAttribute(ref platform, State, obj,
			MuiStringscrollCore.NoInput, out _));
		Assert.Equal(original, MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			State, obj, MuiStringscrollCore.NoInput, out var raw) ? raw : 0u);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void SetPolicyRejectsMalformedLayoutBeforeRawMutation()
	{
		var platform = CreatePlatform(out var stringClass);
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, APTR.Null);
		Assert.NotEqual(APTR.Null, obj);
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.NoInput, 1, false));
		var original = Get(ref platform, obj, MuiStringscrollCore.NoInput);

		// Policy setters finish by recomputing the viewport.  A malformed named
		// layout record must be admitted before policy raw/named projections are
		// changed, otherwise a failed recompute would expose a partial transition.
		Assert.True(MuiStoreCore.DataspaceResize(ref platform, State, obj,
			LayoutStateKey, 4));
		Assert.False(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.NoInput, 0, false));
		Assert.Equal(original, Get(ref platform, obj, MuiStringscrollCore.NoInput));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void SetScrollbarPolicyRejectsMalformedCompositionBeforeRawMutation()
	{
		var platform = CreatePlatform(out var stringClass);
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, APTR.Null);
		Assert.NotEqual(APTR.Null, obj);
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.HorizBar, 1, false));
		var original = Get(ref platform, obj, MuiStringscrollCore.HorizBar);

		// This is the named CopperOS store identity for the typed scrollbar
		// composition record, not an object-layout offset.  Shrinking it
		// simulates malformed guest state before reconciliation.
		Assert.True(MuiStoreCore.DataspaceResize(ref platform, State, obj,
			CompositionStateKey, 4));
		Assert.False(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.HorizBar, 0, false));
		Assert.Equal(original, Get(ref platform, obj, MuiStringscrollCore.HorizBar));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void MalformedCompositionRecordFailsClosedAcrossConsumers()
	{
		var platform = CreatePlatform(out var stringClass);
		var scrollbarName = APTR.FromPointer(0x5BE0);
		platform.WriteCString(scrollbarName, "scrollbar.mui");
		var scrollbarClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			scrollbarName, APTR.Null, 0, APTR.FromPointer(1), false);
		Assert.NotEqual(APTR.Null, scrollbarClass);
		var source = APTR.FromPointer(0x5C20);
		platform.WriteCString(source,
			"composition admission text that is wider than the viewport");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.NotEqual(APTR.Null, obj);
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			16, 16));
		Assert.True(MuiStringscrollCore.TryReadCompositionState(ref platform, State,
			obj, out var composition));
		Assert.True(MuiStringscrollCore.SetScroll(ref platform, State, obj, 8, 0));
		var originalScrollX = GetRaw(ref platform, obj, ScrollXStateKey);

		// This is the named CopperOS store identity for the typed composition
		// record, not an object-layout offset. Shrinking it simulates malformed
		// guest state at every composition consumer boundary.
		Assert.True(MuiStoreCore.DataspaceResize(ref platform, State, obj,
			CompositionStateKey, 4));
		Assert.False(MuiStringscrollCore.TryReadCompositionState(ref platform, State,
			obj, out _));
		Assert.False(MuiStringscrollCore.SetScroll(ref platform, State, obj, 0, 0));
		Assert.Equal(originalScrollX, GetRaw(ref platform, obj, ScrollXStateKey));
		Assert.False(MuiStringscrollCore.Draw(ref platform, State, obj, 0));

		// The malformed record cannot advertise its owned children, so dispose
		// those captured children explicitly before exercising parent cleanup.
		if (composition.Horizontal.IsNotNull)
			Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
				composition.Horizontal));
		if (composition.Vertical.IsNotNull)
			Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
				composition.Vertical));
		Assert.False(MuiStringscrollCore.Cleanup(ref platform, State, obj));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State, obj));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			stringClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			scrollbarClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void MalformedStateRecordFailsClosedBeforeScrollMutation()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x51c0);
		platform.WriteCString(source, "state admission text with overflow");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.NotEqual(APTR.Null, obj);
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			16, 16));
		Assert.True(MuiStringscrollCore.SetScroll(ref platform, State, obj, 8, 0));
		var contentWidth = GetRaw(ref platform, obj, ContentWidthStateKey);
		var scrollX = GetRaw(ref platform, obj, ScrollXStateKey);
		var scrollY = GetRaw(ref platform, obj, ScrollYStateKey);

		// This is the named CopperOS store identity for the typed Stringscroll
		// content/scroll record, not an object-layout offset.  Shrinking it
		// simulates malformed guest state at a shared consumer boundary.
		const uint stateRecordKey = 0x0f110008u;
		Assert.True(MuiStoreCore.DataspaceResize(ref platform, State, obj,
			stateRecordKey, 4));
		Assert.False(MuiStringscrollCore.TryReadState(ref platform, State, obj,
			out _));
		Assert.False(MuiStringscrollCore.GetAttribute(ref platform, State, obj,
			MuiStringscrollCore.String, out _));
		Assert.False(MuiStringscrollCore.SetScroll(ref platform, State, obj, 0, 0));
		Assert.Equal(contentWidth, GetRaw(ref platform, obj, ContentWidthStateKey));
		Assert.Equal(scrollX, GetRaw(ref platform, obj, ScrollXStateKey));
		Assert.Equal(scrollY, GetRaw(ref platform, obj, ScrollYStateKey));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void StringGetterRejectsDivergentOwnedPointerState()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x51D0);
		platform.WriteCString(source, "typed getter text");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.NotEqual(APTR.Null, obj);
		Assert.True(MuiStringscrollCore.GetAttribute(ref platform, State, obj,
			MuiStringscrollCore.String, out var original));
		Assert.Equal("typed getter text", ReadCString(ref platform,
			APTR.FromPointer(original)));

		// Mutate only the canonical named record.  The getter must reject the
		// pointer divergence instead of returning the private ownership key.
		Assert.True(MuiStringscrollCore.TryGetStateRecord(ref platform, State, obj,
			out var record));
		record.String = APTR.FromPointer(0x7200);
		var stateBlock = MuiStoreCore.DataspaceFind(ref platform, State, obj,
			0x0f110008u);
		Assert.True(MuiStringscrollStateRecordCodec.Write(ref platform, stateBlock,
			record));
		Assert.False(MuiStringscrollCore.GetAttribute(ref platform, State, obj,
			MuiStringscrollCore.String, out _));

		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void MalformedPolicyRecordFailsClosedBeforeScrollMutation()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x51e0);
		platform.WriteCString(source, "policy admission text with overflow");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.NotEqual(APTR.Null, obj);
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			16, 16));
		Assert.True(MuiStringscrollCore.SetScroll(ref platform, State, obj, 8, 0));
		var scrollX = GetRaw(ref platform, obj, ScrollXStateKey);
		var scrollY = GetRaw(ref platform, obj, ScrollYStateKey);
		Assert.True(MuiStringscrollCore.TryGetPolicyRecord(ref platform, State, obj,
			out var policyBefore));

		// This is the named CopperOS store identity for the typed policy record,
		// not an object-layout offset.  Shrinking it simulates malformed guest
		// state at a shared policy-consumer boundary.
		Assert.True(MuiStoreCore.DataspaceResize(ref platform, State, obj,
			PolicyStateKey, 4));
		Assert.False(MuiStringscrollCore.TryReadPolicyState(ref platform, State,
			obj, out _));
		Assert.False(MuiStringscrollCore.SetScroll(ref platform, State, obj, 0, 0));
		Assert.Equal(scrollX, GetRaw(ref platform, obj, ScrollXStateKey));
		Assert.Equal(scrollY, GetRaw(ref platform, obj, ScrollYStateKey));

		// Restore the named record, then corrupt only one canonical BOOL. The
		// record remains structurally readable, but value 2 must fail semantic
		// admission instead of being treated as truthy policy.
		Assert.True(MuiStoreCore.DataspaceResize(ref platform, State, obj,
			PolicyStateKey, unchecked((int)MuiStringscrollPolicyRecord.Size)));
		var policyBlock = MuiStoreCore.DataspaceFind(ref platform, State, obj,
			PolicyStateKey);
		Assert.True(MuiStringscrollPolicyRecordCodec.Write(ref platform,
			policyBlock, policyBefore));
		Assert.True(MuiStringscrollPolicyFieldCursorCodec.TryWriteUInt32(
			ref platform, policyBlock, MuiStringscrollPolicyField.NoInput, 2));
		Assert.False(MuiStringscrollCore.TryReadPolicyState(ref platform, State,
			obj, out _));
		Assert.False(MuiStringscrollCore.GetAttribute(ref platform, State, obj,
			MuiStringscrollCore.NoInput, out _));
		Assert.False(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.NoInput, 0, false));
		Assert.False(MuiStringscrollCore.SetScroll(ref platform, State, obj, 0, 0));
		Assert.Equal(scrollX, GetRaw(ref platform, obj, ScrollXStateKey));
		Assert.Equal(scrollY, GetRaw(ref platform, obj, ScrollYStateKey));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void MalformedLayoutRenderAndViewportRecordsFailClosed()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5200);
		platform.WriteCString(source, "layout render viewport admission");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.NotEqual(APTR.Null, obj);
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			32, 16));
		Assert.True(MuiStringscrollCore.TryReadLayoutState(ref platform, State,
			obj, out _));
		Assert.True(MuiStringscrollCore.TryReadViewportState(ref platform, State,
			obj, out _));

		// These are named CopperOS store identities for typed records, not
		// object-layout offsets.  Shrinking each record simulates malformed guest
		// state at its shared inspection boundary.
		Assert.True(MuiStoreCore.DataspaceResize(ref platform, State, obj,
			LayoutStateKey, 4));
		Assert.False(MuiStringscrollCore.TryReadLayoutState(ref platform, State,
			obj, out _));
		Assert.True(MuiStoreCore.DataspaceResize(ref platform, State, obj,
			RenderStateKey, 4));
		Assert.False(MuiStringscrollCore.TryReadRenderState(ref platform, State,
			obj, out _));
		Assert.True(MuiStoreCore.DataspaceResize(ref platform, State, obj,
			ViewportStateKey, 4));
		Assert.False(MuiStringscrollCore.TryReadViewportState(ref platform, State,
			obj, out _));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void StringscrollRuntimeSetRejectsGetterAndInitializerAttributes()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5150);
		platform.WriteCString(source, "immutable runtime text");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.NotEqual(APTR.Null, obj);

		Assert.False(MuiStringscrollCore.SetRuntimeAttribute(ref platform, State,
			obj, MuiStringscrollCore.String, 0));
		Assert.False(MuiStringscrollCore.SetRuntimeAttribute(ref platform, State,
			obj, MuiStringscrollCore.HorizBar, 0));
		Assert.False(MuiStringscrollCore.SetRuntimeAttribute(ref platform, State,
			obj, MuiStringscrollCore.NoInput, 1));
		Assert.False(MuiStringscrollCore.SetRuntimeAttribute(ref platform, State,
			obj, MuiStringscrollCore.SetMin, 1));
		Assert.False(MuiStringscrollCore.SetRuntimeAttribute(ref platform, State,
			obj, MuiStringscrollCore.SetVMin, 1));
		Assert.False(MuiStringscrollCore.SetRuntimeAttribute(ref platform, State,
			obj, MuiStringscrollCore.UseWinBorder, 1));
		Assert.False(MuiStringscrollCore.SetRuntimeAttribute(ref platform, State,
			obj, MuiStringscrollCore.VertBar, 0));
		Assert.False(MuiStringscrollCore.SetRuntimeAttribute(ref platform, State,
			obj, MuiStringscrollCore.VertScrollerOnly, 1));

		Assert.Equal("immutable runtime text", ReadCString(ref platform,
			APTR.FromPointer(Get(ref platform, obj, MuiStringscrollCore.String))));
		Assert.Equal(1u, Get(ref platform, obj, MuiStringscrollCore.HorizBar));
		Assert.Equal(0u, Get(ref platform, obj, MuiStringscrollCore.NoInput));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void SetMinAndSetVMinExposeContentDimensions()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5200);
		platform.WriteCString(source, "1234567890\nxy");
		var tags = Tags(ref platform, (MuiStringscrollCore.String, source.Raw));
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, tags);
		var storage = APTR.FromPointer(0x5400);
		Assert.True(MuiStringscrollCore.AskMinMax(ref platform, State, obj, storage));
		Assert.Equal(8, platform.ReadUInt16(storage, 0));
		Assert.Equal(8, platform.ReadUInt16(storage, 2));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.SetMin, 1));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.SetVMin, 1));
		Assert.True(MuiStringscrollCore.AskMinMax(ref platform, State, obj, storage));
		Assert.Equal(80, platform.ReadUInt16(storage, 0));
		Assert.Equal(16, platform.ReadUInt16(storage, 2));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void Utf8MetricsCountCodepointsAndPreserveOwnedBytes()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5300);
		var utf8 = new byte[] { 0xC3, 0x85, 0xCE, 0xB2, 0xF0, 0x9F,
			0x99, 0x82, 0x0A, 0 };
		for (var index = 0; index < utf8.Length; index++)
			platform.WriteUInt8(source, index, utf8[index]);
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.NotEqual(APTR.Null, obj);
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.SetMin, 1));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.SetVMin, 1));
		var storage = APTR.FromPointer(0x5400);
		Assert.True(MuiStringscrollCore.AskMinMax(ref platform, State, obj,
			storage));
		Assert.Equal(24, platform.ReadUInt16(storage, 0));
		Assert.Equal(16, platform.ReadUInt16(storage, 2));
		Assert.True(MuiStringscrollCore.GetAttribute(ref platform, State, obj,
			MuiStringscrollCore.String, out var owned));
		for (var index = 0; index < utf8.Length; index++)
			Assert.Equal(utf8[index], platform.ReadUInt8(APTR.FromPointer(owned),
				index));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void DrawStartsAndEndsOnUtf8SequenceBoundaries()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5350);
		var utf8 = new byte[] { 0xC3, 0x85, 0xCE, 0xB2, 0xF0, 0x9F,
			0x99, 0x82, 0 };
		for (var index = 0; index < utf8.Length; index++)
			platform.WriteUInt8(source, index, utf8[index]);
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		var renderInfo = APTR.FromPointer(0x535F);
		platform.WriteUInt32(renderInfo, 20, 0x5360);
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			RenderInfo, renderInfo.Raw, false));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			16, 24));
		Assert.True(MuiStringscrollCore.SetScroll(ref platform, State, obj, 8, 0));
		Assert.True(MuiStringscrollCore.Draw(ref platform, State, obj, 0));
		Assert.Equal(6, platform.LastTextLength);
		Assert.Equal(0xCE, platform.ReadUInt8(platform.LastText, 0));
		Assert.Equal(0xB2, platform.ReadUInt8(platform.LastText, 1));
		Assert.Equal(0xF0, platform.ReadUInt8(platform.LastText, 2));
		Assert.Equal(0x82, platform.ReadUInt8(platform.LastText, 5));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void LayoutComputesAndClampsPixelScroll()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5600);
		platform.WriteCString(source, "01234567890123456789\nsecond line\nthird line");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 10, 20,
			100, 32));
		Assert.True(MuiStringscrollCore.SetScroll(ref platform, State, obj, 999,
			999));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var x, out var y, out var maxX, out var maxY));
		Assert.Equal(maxX, x);
		Assert.Equal(maxY, y);
		Assert.True(maxX > 0);
		Assert.True(maxY > 0);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void NoInputBlocksScrollAndScrollerOnlyRemovesHorizontalReserve()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5800);
		platform.WriteCString(source, "012345678901234567890123456789");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0, 100, 24);
		Assert.True(MuiStringscrollCore.SetScroll(ref platform, State, obj, 16, 0));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.NoInput, 1));
		Assert.False(MuiStringscrollCore.ScrollBy(ref platform, State, obj, 8, 0));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var blockedX, out _, out _, out _));
		Assert.Equal(16, blockedX);
		// Re-enable input and use vertical-only policy; horizontal scrolling is
		// still bounded, but the horizontal bar no longer consumes height.
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.NoInput, 0));
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.VertScrollerOnly, 1));
		Assert.True(MuiStringscrollCore.ScrollBy(ref platform, State, obj, 8, 0));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void BarsFollowOverflowAndWinBorderPolicy()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5900);
		platform.WriteCString(source, "short");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			100, 24));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out _, out _, out var shortMaxX, out var shortMaxY));
		Assert.Equal(0, shortMaxX);
		Assert.Equal(0, shortMaxY);

		platform.WriteCString(source, "012345678901234567890123456789\nline two\nline three\nline four");
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.String, source.Raw));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out _, out _, out var bothMaxX, out var bothMaxY));
		Assert.True(bothMaxX > 0);
		Assert.True(bothMaxY > 0);

		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.UseWinBorder, 1));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out _, out _, out var borderMaxX, out var borderMaxY));
		Assert.True(borderMaxX < bothMaxX);
		Assert.True(borderMaxY < bothMaxY);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void DrawTreatsCrLfAsOneLineWithoutDrawingCarriageReturn()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5B00);
		platform.WriteCString(source, "a\r\nb");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		var renderInfo = APTR.FromPointer(0x5C00);
		platform.WriteUInt32(renderInfo, 20, 0x5D00);
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			RenderInfo, renderInfo.Raw, false));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			64, 32));
		Assert.True(MuiStringscrollCore.Draw(ref platform, State, obj, 0));
		Assert.Equal(2u, platform.TextCount);
		Assert.Equal(1, platform.LastTextLength);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void DrawRendersProportionalThumbsForBothOverflowAxes()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5D20);
		platform.WriteCString(source,
			"01234567890123456789012345678901\nsecond line\nthird line\nfourth line");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		var renderInfo = APTR.FromPointer(0x5E20);
		platform.WriteUInt32(renderInfo, 20, 0x5E40);
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			RenderInfo, renderInfo.Raw, false));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			40, 24));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out _, out _, out var maxX, out var maxY));
		Assert.True(maxX > 0);
		Assert.True(maxY > 0);
		Assert.True(MuiStringscrollCore.SetScroll(ref platform, State, obj,
			maxX / 2, maxY / 2));

		Assert.True(MuiStringscrollCore.Draw(ref platform, State, obj, 0));
		// Both tracks and both proportional thumbs use the graphics seam.
		Assert.Equal(4u, platform.FillCount);
		// The final fill is the vertical thumb: 6px high at the midpoint of
		// the 12px reserved viewport track.
		Assert.Equal(28, platform.LastLeft);
		Assert.Equal(3, platform.LastTop);
		Assert.Equal(39, platform.LastRight);
		Assert.Equal(8, platform.LastBottom);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void DispatcherRoutesLayoutAskMinMaxSetAndDraw()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5A00);
		platform.WriteCString(source, "abcdefghijk");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		var renderInfo = APTR.FromPointer(0x5C00);
		platform.WriteUInt32(renderInfo, 20, 0x5D00);
		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			RenderInfo, renderInfo.Raw, false));
		var packet = APTR.FromPointer(0x5E00);
		platform.WriteUInt32(packet, 0, Layout);
		platform.WriteUInt32(packet, 4, 0);
		platform.WriteUInt32(packet, 8, 0);
		platform.WriteUInt32(packet, 12, 100);
		platform.WriteUInt32(packet, 16, 24);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		var minMax = APTR.FromPointer(0x5F00);
		platform.WriteUInt32(packet, 0, AskMinMax);
		platform.WriteUInt32(packet, 4, minMax.Raw);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		platform.WriteUInt32(packet, 0, Set);
		platform.WriteUInt32(packet, 4, MuiStringscrollCore.NoInput);
		platform.WriteUInt32(packet, 8, 1);
		Assert.Equal(0u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		platform.WriteUInt32(packet, 0, Draw);
		platform.WriteUInt32(packet, 4, 0);
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.Equal(1u, platform.TextCount);
		// The short string fits the 100x24 viewport, so no scrollbar track or
		// thumb is drawn.
		Assert.Equal(0u, platform.FillCount);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void HandleInputRoutesNamedPacketAndMovesBoundedScroll()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5A80);
		platform.WriteCString(source,
			"0123456789012345678901234567890123456789\nline two\nline three\nline four");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			48, 32));
		var packet = APTR.FromPointer(0x5B80);
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0x6100, KeyRight));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var rightX, out var rightY, out var maxX, out var maxY));
		Assert.Equal(8, rightX);
		Assert.Equal(0, rightY);
		Assert.True(maxX > rightX);
		Assert.True(maxY > 0);

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0x6100, KeyPageDown));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out _, out var pageY, out _, out _));
		Assert.True(pageY > 0);

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0x6100, KeyEnd));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var endX, out var endY, out var endMaxX, out var endMaxY));
		Assert.Equal(endMaxX, endX);
		Assert.Equal(endMaxY, endY);

		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0x6100, KeyHome));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var homeX, out var homeY, out _, out _));
		Assert.Equal(0, homeX);
		Assert.Equal(0, homeY);

		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.NoInput, 1));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, 0x6100, KeyRight));
		Assert.Equal(0u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var blockedX, out _, out _, out _));
		Assert.Equal(0, blockedX);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void HandleInputTrackClicksUseTypedPointerAndBoundedThumbMapping()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5C80);
		platform.WriteCString(source,
			"0123456789012345678901234567890123456789\nline two\nline three\nline four");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			48, 32));

		var intui = APTR.FromPointer(0x5D80);
		var packet = APTR.FromPointer(0x5E80);
		// Both axes overflow.  The horizontal track occupies y=20..31; a click
		// near its middle maps through the named thumb geometry into a bounded
		// pixel position rather than reading an ABI offset in this test.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, 24, 26));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var horizontalX, out var horizontalY, out var maxX, out var maxY));
		Assert.True(horizontalX > 0 && horizontalX < maxX);
		Assert.Equal(0, horizontalY);

		// The vertical track occupies x=36..47.  Its click is independently
		// clamped and leaves the horizontal position intact.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, 42, 15));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var verticalX, out var verticalY, out var verticalMaxX,
			out var verticalMaxY));
		Assert.Equal(horizontalX, verticalX);
		Assert.Equal(verticalMaxX, maxX);
		Assert.True(verticalY > 0 && verticalY <= verticalMaxY);

		// SELECTDOWN is not the commit edge and does not move either axis.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, 0x0068, 0, 0, 12, 26));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(0u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var unchangedX, out var unchangedY, out _, out _));
		Assert.Equal(verticalX, unchangedX);
		Assert.Equal(verticalY, unchangedY);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void HandleInputThumbDragUsesGuestResidentStructState()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x5F80);
		platform.WriteCString(source,
			"0123456789012345678901234567890123456789\nline two\nline three\nline four");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			48, 32));

		var intui = APTR.FromPointer(0x6080);
		var packet = APTR.FromPointer(0x6180);
		// The initial horizontal thumb is x=0..5.  Keep a grab offset of three
		// pixels, then move the pointer; the guest state must carry that offset
		// across the MOUSEMOVE and final SELECTUP packets.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectDown, 0, 0, 3, 26));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseMove, 0, 0, 0, 24, 26));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var movedX, out var movedY, out var maxX, out var maxY));
		Assert.True(movedX > 0 && movedX < maxX);
		Assert.Equal(0, movedY);

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, 30, 26));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var releasedX, out _, out _, out _));
		Assert.True(releasedX > movedX);

		// Repeat the same typed state transition on the vertical thumb.  The
		// bottom-right track overlap remains owned by the vertical axis.
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectDown, 0, 0, 42, 4));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseMove, 0, 0, 0, 42, 15));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var verticalX, out var verticalY, out _, out var verticalMaxY));
		Assert.Equal(releasedX, verticalX);
		Assert.True(verticalY > 0 && verticalY <= verticalMaxY);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, 42, 19));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void ThumbDragUsesNamedPointerCaptureCapability()
	{
		var platform = CreatePlatform(out var stringClass);
		platform.PointerCaptureSampleAvailable = true;
		var source = APTR.FromPointer(0x6580);
		platform.WriteCString(source,
			"0123456789012345678901234567890123456789\nline two\nline three");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			48, 32));

		var intui = APTR.FromPointer(0x6680);
		var packet = APTR.FromPointer(0x6780);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectDown, 0, 0, 3, 26));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.Equal(1u, platform.PointerCaptureCount);
		Assert.Equal(obj, platform.LastPointerCaptureObject);
		Assert.Equal(MuiPointerCaptureKind.HorizontalScroller,
			platform.LastPointerCaptureKind);
		Assert.Equal(3, platform.LastPointerCaptureStartX);
		Assert.Equal(26, platform.LastPointerCaptureStartY);

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectUp, 0, 0, 30, 26));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.Equal(1u, platform.PointerReleaseCount);
		Assert.Equal(obj, platform.LastPointerReleaseObject);
		Assert.Equal(MuiPointerCaptureKind.HorizontalScroller,
			platform.LastPointerReleaseKind);
		Assert.Equal(3, platform.LastPointerReleaseStartX);
		Assert.Equal(26, platform.LastPointerReleaseStartY);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void ThumbDragCaptureIsReleasedWhenObjectIsDisposed()
	{
		var platform = CreatePlatform(out var stringClass);
		platform.PointerCaptureSampleAvailable = true;
		var source = APTR.FromPointer(0x6780);
		platform.WriteCString(source,
			"0123456789012345678901234567890123456789\nline two\nline three");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			48, 32));

		var intui = APTR.FromPointer(0x6880);
		var packet = APTR.FromPointer(0x6980);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectDown, 0, 0, 3, 26));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.Equal(1u, platform.PointerCaptureCount);

		Dispose(ref platform, obj, stringClass);
		Assert.Equal(1u, platform.PointerReleaseCount);
		Assert.Equal(obj, platform.LastPointerReleaseObject);
		Assert.Equal(MuiPointerCaptureKind.HorizontalScroller,
			platform.LastPointerReleaseKind);
		Assert.Equal(3, platform.LastPointerReleaseStartX);
		Assert.Equal(26, platform.LastPointerReleaseStartY);
	}

	[Fact]
	public void ThumbDragCaptureIsReleasedByDirectHeadlessDisposal()
	{
		var platform = CreatePlatform(out var stringClass);
		platform.PointerCaptureSampleAvailable = true;
		var source = APTR.FromPointer(0x6980);
		platform.WriteCString(source,
			"0123456789012345678901234567890123456789\nline two\nline three");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			48, 32));

		var intui = APTR.FromPointer(0x6A80);
		var packet = APTR.FromPointer(0x6B80);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectDown, 0, 0, 3, 26));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.Equal(1u, platform.PointerCaptureCount);

		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, obj));
		Assert.Equal(1u, platform.PointerReleaseCount);
		Assert.Equal(obj, platform.LastPointerReleaseObject);
		Assert.Equal(MuiPointerCaptureKind.HorizontalScroller,
			platform.LastPointerReleaseKind);
		Assert.Equal(3, platform.LastPointerReleaseStartX);
		Assert.Equal(26, platform.LastPointerReleaseStartY);
	}

	[Fact]
	public void ThumbDragIsCancelledWhenOwningWindowBecomesInactive()
	{
		var platform = CreatePlatform(out var stringClass);
		platform.PointerCaptureSampleAvailable = true;
		var source = APTR.FromPointer(0x6280);
		platform.WriteCString(source,
			"0123456789012345678901234567890123456789\nline two\nline three");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			48, 32));

		var intui = APTR.FromPointer(0x6380);
		var packet = APTR.FromPointer(0x6480);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectDown, 0, 0, 3, 26));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));

		// The window event dispatcher reaches the same typed Stringscroll record
		// even though this focused test uses the object as the minimal root.
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, obj, APTR.Null, 0x00080000));
		Assert.Equal(1u, platform.PointerReleaseCount);
		Assert.Equal(obj, platform.LastPointerReleaseObject);
		Assert.Equal(MuiPointerCaptureKind.HorizontalScroller,
			platform.LastPointerReleaseKind);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseMove, 0, 0, 0, 30, 26));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(0u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var x, out var y, out _, out _));
		Assert.Equal(0, x);
		Assert.Equal(0, y);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void HandleInputReleaseCancelsThumbDragBeforeNoInputGate()
	{
		var platform = CreatePlatform(out var stringClass);
		var source = APTR.FromPointer(0x6200);
		platform.WriteCString(source,
			"0123456789012345678901234567890123456789\nline two\nline three");
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.True(MuiStringscrollCore.Layout(ref platform, State, obj, 0, 0,
			48, 32));

		var intui = APTR.FromPointer(0x6300);
		var packet = APTR.FromPointer(0x6400);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseButtons, SelectDown, 0, 0, 3, 26));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));

		Assert.True(MuiStringscrollCore.SetAttribute(ref platform, State, obj,
			MuiStringscrollCore.NoInput, 1));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyRelease));
		Assert.Equal(1u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intui,
			IdcmpMouseMove, 0, 0, 0, 30, 26));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intui.Raw, KeyNone));
		Assert.Equal(0u, MuiCollectionDispatcher.Dispatch(ref platform, State, obj,
			packet));
		Assert.True(MuiStringscrollCore.GetScrollState(ref platform, State, obj,
			out var x, out var y, out _, out _));
		Assert.Equal(0, x);
		Assert.Equal(0, y);
		Dispose(ref platform, obj, stringClass);
	}

	[Fact]
	public void UnterminatedSourceFailsAtomically()
	{
		var platform = CreatePlatform(out var stringClass, 0x30000, 0x20000);
		var source = APTR.FromPointer(0x21000);
		for (var i = 0; i < 0x10000; i++) platform.WriteUInt8(source, i, (byte)'x');
		var obj = MuiStringscrollCore.CreateStringscroll(ref platform, State,
			stringClass, Tags(ref platform, (MuiStringscrollCore.String, source.Raw)));
		Assert.Equal(APTR.Null, obj);
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State, stringClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			attribute, out var value));
		return value;
	}

	private static uint GetRaw(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, obj,
			attribute, out var value));
		return value;
	}

	private static APTR Tags(ref MuiHeadlessTestPlatform platform,
		(uint attribute, uint value) first)
	{
		var tags = APTR.FromPointer(0x3000);
		platform.WriteUInt32(tags, 0, first.attribute);
		platform.WriteUInt32(tags, 4, first.value);
		platform.WriteUInt32(tags, 8, 0);
		return tags;
	}

	private static string ReadCString(ref MuiHeadlessTestPlatform platform,
		APTR address)
	{
		if (address.IsNull) return string.Empty;
		var result = string.Empty;
		for (var i = 0; i < 4096; i++)
		{
			var ch = platform.ReadUInt8(address, i);
			if (ch == 0) break;
			result += (char)ch;
		}
		return result;
	}

	private static void Dispose(ref MuiHeadlessTestPlatform platform, APTR obj,
		APTR classRecord)
	{
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State, obj));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			classRecord));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR stringClass,
		int size = 0x80000, uint firstAllocation = 0x8000)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, size, firstAllocation,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Stringscroll.mui");
		MuiHeadlessObjectCore.Initialize(ref platform, State);
		stringClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
