using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListviewExternalScrollerTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	private const uint ListviewList = 0x8042bcceu;
	private const uint First = 0x804238d4u;
	private const uint PropEntries = 0x8042fbdbu;
	private const uint PropVisible = 0x8042fea6u;
	private const uint PropFirst = 0x8042d4b2u;
	private const uint TotalPixel = 0x8042a8f5u;
	private const uint SelectChange = 0x8042178fu;
	private const uint ListVisible = 0x8042191fu;
	private const uint MinLineHeight = 0x8042d1c3u;
	private const uint ClickColumn = 0x8042d1b3u;
	private const uint AgainClick = 0x804214c2u;
	private const uint DoubleClick = 0x80424635u;
	private const uint EveryTime = 1233727793u;

	[Fact]
	public void ExternalPropProjectionUsesNamedListPixelViewport()
	{
		var platform = NewPlatform();
		var listClass = Register(ref platform, 0x1100, "List.mui");
		var listviewClass = Register(ref platform, 0x1140, "Listview.mui");
		var propClass = Register(ref platform, 0x1180, "Prop.mui");
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		for (var index = 0; index < 3; index++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x2000u + (uint)index * 0x20u), -3));

		var listviewTags = BuildTags(ref platform, 0x1900,
			new[] { (ListviewList, list.Raw) });
		var listview = MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, listviewTags);
		Assert.NotEqual(APTR.Null, listview);
		var prop = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, APTR.Null);
		Assert.NotEqual(APTR.Null, prop);

		Assert.True(MuiListviewCore.Layout(ref platform, State, listview,
			0, 0, 80, 16));
		Assert.True(MuiListviewCore.TryGetExternalScrollerState(ref platform,
			State, listview, out var viewport));
		Assert.Equal(24u, viewport.Entries);
		Assert.Equal(16u, viewport.Visible);
		Assert.Equal(0u, viewport.First);

		Assert.True(MuiListviewCore.SyncExternalScrollerProp(ref platform,
			State, listview, prop));
		Assert.True(MuiCommonControlCore.TryGetPropRangeStateRecord(ref platform,
			State, prop, out var range));
		Assert.Equal(24u, range.Entries);
		Assert.Equal(16u, range.Visible);
		Assert.Equal(0u, range.First);
		Assert.Equal(24u, Get(ref platform, prop, PropEntries));
		Assert.Equal(16u, Get(ref platform, prop, PropVisible));

		Assert.True(MuiListCore.SetAttribute(ref platform, State, list, First, 1,
			true));
		Assert.True(MuiListviewCore.SyncExternalScrollerProp(ref platform,
			State, listview, prop));
		Assert.True(MuiCommonControlCore.TryGetPropRangeStateRecord(ref platform,
			State, prop, out range));
		Assert.Equal(8u, range.First);
		Assert.Equal(8u, Get(ref platform, prop, PropFirst));

		var redraws = platform.RedrawCount;
		Assert.True(MuiListviewCore.SyncExternalScrollerProp(ref platform,
			State, listview, prop));
		Assert.Equal(redraws, platform.RedrawCount);

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			listview));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			prop));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listviewClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			propClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void ListviewForwardedNotificationsResolveToOwnedListRecord()
	{
		var platform = NewPlatform();
		var listClass = Register(ref platform, 0x1100, "List.mui");
		var listviewClass = Register(ref platform, 0x1140, "Listview.mui");
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x2400), -3));
		var tags = BuildTags(ref platform, 0x2500,
			new[] { (ListviewList, list.Raw) });
		var listview = MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, tags);
		Assert.NotEqual(APTR.Null, listview);
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview,
			0, 0, 80, 16));
		Assert.True(MuiListviewCore.IsForwardedNotificationAttribute(TotalPixel));
		Assert.False(MuiListviewCore.IsForwardedNotificationAttribute(
			SelectChange));

		var follow = APTR.FromPointer(0x2600);
		platform.WriteUInt32(follow, 0, 0x90000010u);
		platform.WriteUInt32(follow, 4, EveryTime);
		Assert.True(MuiNotifyCore.Add(ref platform, State, listview,
			TotalPixel, EveryTime, listview, 2, follow));
		var before = platform.DispatchCount;
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x2440), -3));
		Assert.Equal(before + 1, platform.DispatchCount);
		Assert.Equal(1u, MuiNotifyCore.Remove(ref platform, State, listview,
			TotalPixel, listview, true));
		var afterRemove = platform.DispatchCount;
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x2480), -3));
		Assert.Equal(afterRemove, platform.DispatchCount);

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			listview));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listviewClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void ListviewClickResultsPublishEventProjections()
	{
		var platform = NewPlatform();
		var listClass = Register(ref platform, 0x1100, "List.mui");
		var listviewClass = Register(ref platform, 0x1140, "Listview.mui");
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x2800), -3));
		var tags = BuildTags(ref platform, 0x2900,
			new[] { (ListviewList, list.Raw) });
		var listview = MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, tags);
		Assert.NotEqual(APTR.Null, listview);

		var clickFollow = APTR.FromPointer(0x2A00);
		var againFollow = APTR.FromPointer(0x2A20);
		var doubleFollow = APTR.FromPointer(0x2A40);
		platform.WriteUInt32(clickFollow, 0, 0x90000010u);
		platform.WriteUInt32(clickFollow, 4, EveryTime);
		platform.WriteUInt32(againFollow, 0, 0x90000010u);
		platform.WriteUInt32(againFollow, 4, EveryTime);
		platform.WriteUInt32(doubleFollow, 0, 0x90000010u);
		platform.WriteUInt32(doubleFollow, 4, EveryTime);
		Assert.True(MuiNotifyCore.Add(ref platform, State, listview,
			ClickColumn, EveryTime, listview, 2, clickFollow));
		Assert.True(MuiNotifyCore.Add(ref platform, State, listview,
			AgainClick, EveryTime, listview, 2, againFollow));
		Assert.True(MuiNotifyCore.Add(ref platform, State, listview,
			DoubleClick, EveryTime, listview, 2, doubleFollow));

		var before = platform.DispatchCount;
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview,
			0, 1, 2, false));
		Assert.Equal(before + 1, platform.DispatchCount);
		Assert.Equal(2u, Get(ref platform, listview, ClickColumn));
		Assert.Equal(0u, Get(ref platform, listview, AgainClick));
		Assert.Equal(0u, Get(ref platform, listview, DoubleClick));

		before = platform.DispatchCount;
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview,
			0, 1, 2, false));
		Assert.Equal(before + 1, platform.DispatchCount);

		before = platform.DispatchCount;
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview,
			0, 3, 2, false));
		Assert.Equal(before + 2, platform.DispatchCount);
		Assert.Equal(1u, Get(ref platform, listview, AgainClick));

		before = platform.DispatchCount;
		Assert.True(MuiListviewCore.HandleClick(ref platform, State, listview,
			0, 2, 1, false));
		Assert.Equal(before + 2, platform.DispatchCount);
		Assert.Equal(1u, Get(ref platform, listview, ClickColumn));
		Assert.Equal(1u, Get(ref platform, listview, DoubleClick));

		Assert.Equal(1u, MuiNotifyCore.Remove(ref platform, State, listview,
			ClickColumn, listview, true));
		Assert.Equal(1u, MuiNotifyCore.Remove(ref platform, State, listview,
			AgainClick, listview, true));
		Assert.Equal(1u, MuiNotifyCore.Remove(ref platform, State, listview,
			DoubleClick, listview, true));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			listview));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listviewClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void ListviewVisibleRowsFollowNamedChildLineHeight()
	{
		var platform = NewPlatform();
		var listClass = Register(ref platform, 0x1100, "List.mui");
		var listviewClass = Register(ref platform, 0x1140, "Listview.mui");
		var listTags = BuildTags(ref platform, 0x2B00,
			new[] { (MinLineHeight, 16u) });
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			listTags);
		Assert.NotEqual(APTR.Null, list);
		for (var index = 0; index < 4; index++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x2C00u + (uint)index * 0x20u), -3));
		var listviewTags = BuildTags(ref platform, 0x2D00,
			new[] { (ListviewList, list.Raw) });
		var listview = MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, listviewTags);
		Assert.NotEqual(APTR.Null, listview);

		Assert.True(MuiListviewCore.Layout(ref platform, State, listview,
			0, 0, 80, 40));
		Assert.True(MuiListCore.TryGetViewportState(ref platform, State, list,
			out var viewport));
		Assert.Equal(16u, viewport.LineHeight);
		Assert.Equal(2u, viewport.Visible);
		Assert.Equal(2u, Get(ref platform, list, ListVisible));

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			listview));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listviewClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void ListviewScrollerFallbackUsesNamedChildLineHeight()
	{
		var platform = NewPlatform();
		var listClass = Register(ref platform, 0x1100, "List.mui");
		var listviewClass = Register(ref platform, 0x1140, "Listview.mui");
		var listTags = BuildTags(ref platform, 0x2E00,
			new[] { (MinLineHeight, 16u) });
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			listTags);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x2F00), -3));
		var listviewTags = BuildTags(ref platform, 0x3000,
			new[] { (ListviewList, list.Raw) });
		var listview = MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, listviewTags);
		Assert.NotEqual(APTR.Null, listview);

		// Establish the child geometry without publishing a List viewport. This
		// exercises the Listview scroller's early-layout fallback path.
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, list,
			0, 0, 80, 40));
		Assert.True(MuiListviewCore.GetScrollerState(ref platform, State,
			listview, out _, out var visible, out _, out _));
		Assert.Equal(2u, visible);

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			listview));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listviewClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void ExternalScrollerConnectionInstallsAndRemovesMorphosRecipe()
	{
		var platform = NewPlatform();
		var listClass = Register(ref platform, 0x1100, "List.mui");
		var listviewClass = Register(ref platform, 0x1140, "Listview.mui");
		var propClass = Register(ref platform, 0x1180, "Prop.mui");
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		for (var index = 0; index < 3; index++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x3100u + (uint)index * 0x20u), -3));
		var listviewTags = BuildTags(ref platform, 0x3200,
			new[] { (ListviewList, list.Raw) });
		var listview = MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, listviewTags);
		var prop = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, APTR.Null);
		Assert.NotEqual(APTR.Null, listview);
		Assert.NotEqual(APTR.Null, prop);
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview,
			0, 0, 80, 16));

		Assert.True(MuiListviewCore.ConnectExternalScrollerProp(ref platform,
			State, listview, prop));
		var before = platform.DispatchCount;
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x3180), -3));
		Assert.Equal(before + 1, platform.DispatchCount);

		before = platform.DispatchCount;
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list, First, 1,
			true));
		Assert.Equal(before + 1, platform.DispatchCount);

		before = platform.DispatchCount;
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform,
			State, prop, PropFirst, 8, true));
		Assert.Equal(before + 1, platform.DispatchCount);

		Assert.True(MuiListviewCore.DisconnectExternalScrollerProp(ref platform,
			State, listview, prop));
		before = platform.DispatchCount;
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x31C0), -3));
		Assert.Equal(before, platform.DispatchCount);

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			listview));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			prop));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listviewClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			propClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void ExternalScrollerConnectionIsRemovedDuringListviewDisposal()
	{
		var platform = NewPlatform();
		var listClass = Register(ref platform, 0x1200, "List.mui");
		var listviewClass = Register(ref platform, 0x1240, "Listview.mui");
		var propClass = Register(ref platform, 0x1280, "Prop.mui");
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x3400), -3));
		var tags = BuildTags(ref platform, 0x3500,
			new[] { (ListviewList, list.Raw) });
		var listview = MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, tags);
		var prop = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, APTR.Null);
		Assert.NotEqual(APTR.Null, listview);
		Assert.NotEqual(APTR.Null, prop);
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview,
			0, 0, 80, 16));
		Assert.True(MuiListviewCore.ConnectExternalScrollerProp(ref platform,
			State, listview, prop));
		Assert.True(MuiListviewCore.TryGetExternalScrollerConnection(
			ref platform, State, listview, out var connection));
		Assert.Equal(prop.Raw, connection.Prop.Raw);

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			listview));
		var before = platform.DispatchCount;
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform,
			State, prop, PropFirst, 1, true));
		Assert.Equal(before, platform.DispatchCount);

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			prop));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listviewClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			propClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void ExternalScrollerConnectionIsRemovedWhenPropDisposesFirst()
	{
		var platform = NewPlatform();
		var listClass = Register(ref platform, 0x1300, "List.mui");
		var listviewClass = Register(ref platform, 0x1340, "Listview.mui");
		var propClass = Register(ref platform, 0x1380, "Prop.mui");
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x3600), -3));
		var tags = BuildTags(ref platform, 0x3700,
			new[] { (ListviewList, list.Raw) });
		var listview = MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, tags);
		var prop = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, APTR.Null);
		Assert.NotEqual(APTR.Null, listview);
		Assert.NotEqual(APTR.Null, prop);
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview,
			0, 0, 80, 16));
		Assert.True(MuiListviewCore.ConnectExternalScrollerProp(ref platform,
			State, listview, prop));

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			prop));
		var before = platform.DispatchCount;
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x3680), -3));
		Assert.Equal(before, platform.DispatchCount);

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			listview));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listviewClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			propClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void ExternalScrollerConnectionTargetsScrollbarGroupThroughNamedPropState()
	{
		var platform = NewPlatform();
		var listClass = Register(ref platform, 0x1400, "List.mui");
		var listviewClass = Register(ref platform, 0x1440, "Listview.mui");
		var propClass = Register(ref platform, 0x1480, "Prop.mui");
		var gadgetClass = Register(ref platform, 0x14C0, "Gadget.mui");
		var scrollbarClass = Register(ref platform, 0x1500, "Scrollbar.mui");
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		for (var index = 0; index < 3; index++)
			Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
				APTR.FromPointer(0x3A00u + (uint)index * 0x20u), -3));
		var tags = BuildTags(ref platform, 0x3B00,
			new[] { (ListviewList, list.Raw) });
		var listview = MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, tags);
		var scrollbar = MuiCommonControlCore.CreateControl(ref platform, State,
			scrollbarClass, APTR.Null);
		Assert.NotEqual(APTR.Null, listview);
		Assert.NotEqual(APTR.Null, scrollbar);
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview,
			0, 0, 80, 16));
		Assert.True(MuiListviewCore.ConnectExternalScrollerProp(ref platform,
			State, listview, scrollbar));
		Assert.True(MuiCommonControlCore.TryGetPropRangeStateRecord(
			ref platform, State, scrollbar, out var range));
		Assert.Equal(24u, range.Entries);
		Assert.Equal(16u, range.Visible);

		var before = platform.DispatchCount;
		Assert.True(MuiListCore.SetAttribute(ref platform, State, list, First, 1,
			true));
		// The host fixture records the guest DoMethod seam; a native provider
		// dispatches this Set packet through Scrollbar.mui's class-aware setter.
		Assert.Equal(before + 1, platform.DispatchCount);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform,
			State, scrollbar, PropFirst, 0, true));
		Assert.Equal(0u, Get(ref platform, scrollbar, PropFirst));

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			listview));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			scrollbar));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listviewClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			propClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			gadgetClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			scrollbarClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void MalformedExternalScrollerConnectionFailsClosedBeforeReconnect()
	{
		var platform = NewPlatform();
		var listClass = Register(ref platform, 0x1540, "List.mui");
		var listviewClass = Register(ref platform, 0x1580, "Listview.mui");
		var propClass = Register(ref platform, 0x15C0, "Prop.mui");
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x3E00), -3));
		var tags = BuildTags(ref platform, 0x3F00,
			new[] { (ListviewList, list.Raw) });
		var listview = MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, tags);
		var prop = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, APTR.Null);
		Assert.NotEqual(APTR.Null, listview);
		Assert.NotEqual(APTR.Null, prop);
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview,
			0, 0, 80, 16));
		Assert.True(MuiListviewCore.ConnectExternalScrollerProp(ref platform,
			State, listview, prop));
		const uint connectionKey = 0x7F09000Cu;
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			listview, connectionKey, out var rawBlock));
		var block = APTR.FromPointer(rawBlock);
		Assert.NotEqual(APTR.Null, block);
		var beforeDispatch = platform.DispatchCount;
		platform.WriteUInt32(block, 0, 0);

		Assert.False(MuiListviewCore.TryGetExternalScrollerConnection(
			ref platform, State, listview, out _));
		Assert.False(MuiListviewCore.ConnectExternalScrollerProp(ref platform,
			State, listview, prop));
		Assert.Equal(beforeDispatch, platform.DispatchCount);
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			listview, connectionKey, out var retainedRawBlock));
		Assert.Equal(rawBlock, retainedRawBlock);

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			listview));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			prop));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listviewClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			propClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	[Fact]
	public void IncoherentExternalScrollerDestinationFailsClosedBeforeReconnect()
	{
		var platform = NewPlatform();
		var listClass = Register(ref platform, 0x1600, "List.mui");
		var listviewClass = Register(ref platform, 0x1640, "Listview.mui");
		var propClass = Register(ref platform, 0x1680, "Prop.mui");
		var list = MuiListCore.CreateList(ref platform, State, listClass,
			APTR.Null);
		Assert.NotEqual(APTR.Null, list);
		Assert.True(MuiListCore.InsertSingle(ref platform, State, list,
			APTR.FromPointer(0x4100), -3));
		var tags = BuildTags(ref platform, 0x4200,
			new[] { (ListviewList, list.Raw) });
		var listview = MuiListviewCore.CreateListview(ref platform, State,
			listviewClass, tags);
		var prop = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, APTR.Null);
		Assert.NotEqual(APTR.Null, listview);
		Assert.NotEqual(APTR.Null, prop);
		Assert.True(MuiListviewCore.Layout(ref platform, State, listview,
			0, 0, 80, 16));
		Assert.True(MuiListviewCore.ConnectExternalScrollerProp(ref platform,
			State, listview, prop));

		const uint connectionKey = 0x7F09000Cu;
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			listview, connectionKey, out var rawBlock));
		Assert.NotEqual(0u, rawBlock);
		var block = APTR.FromPointer(rawBlock);
		Assert.True(MuiListviewCore.MuiListviewExternalScrollerConnectionFieldCursorCodec
			.TryWriteUInt32(ref platform, block,
				MuiListviewCore.MuiListviewExternalScrollerConnectionField.Prop,
				0xDEAD0000u));
		var beforeDispatch = platform.DispatchCount;

		Assert.False(MuiListviewCore.TryGetExternalScrollerConnection(
			ref platform, State, listview, out _));
		Assert.False(MuiListviewCore.ConnectExternalScrollerProp(ref platform,
			State, listview, prop));
		Assert.Equal(beforeDispatch, platform.DispatchCount);
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			listview, connectionKey, out var retainedRawBlock));
		Assert.Equal(rawBlock, retainedRawBlock);

		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			listview));
		Assert.True(MuiCollectionLifecycle.DisposeObject(ref platform, State,
			prop));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			listviewClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State,
			propClass));
		Assert.Equal(platform.AllocationCount, platform.FreeCount);
	}

	private static MuiHeadlessTestPlatform NewPlatform()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x8000,
			State);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		return platform;
	}

	private static APTR Register(ref MuiHeadlessTestPlatform platform,
		uint address, string name)
	{
		platform.WriteCString(APTR.FromPointer(address), name);
		return MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			APTR.FromPointer(address), APTR.Null, 1, APTR.FromPointer(1), false);
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform,
		uint address, (uint tag, uint data)[] pairs)
	{
		var offset = 0;
		foreach (var pair in pairs)
		{
			platform.WriteUInt32(APTR.FromPointer(address), offset, pair.tag);
			platform.WriteUInt32(APTR.FromPointer(address), offset + 4,
				pair.data);
			offset += 8;
		}
		platform.WriteUInt32(APTR.FromPointer(address), offset, 0);
		platform.WriteUInt32(APTR.FromPointer(address), offset + 4, 0);
		return APTR.FromPointer(address);
	}

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			attribute, out var value));
		return value;
	}
}
