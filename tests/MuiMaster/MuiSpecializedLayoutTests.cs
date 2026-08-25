using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiSpecializedLayoutTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint Width = 0x8042B59C;
	private const uint Height = 0x80423237;
	private const uint Left = 0x8042BEC6;
	private const uint FixWidth = 0x8042A3F1;
	private const uint FixHeight = 0x8042A92B;

	[Fact]
	public void RegisterAndSelectgroupSwitchPageGeometryDeterministically()
	{
		var platform = CreatePlatform(out var cl);
		var pages = Object(ref platform, cl);
		var first = Object(ref platform, cl);
		var second = Object(ref platform, cl);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, pages, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, pages, second));
		Assert.True(MuiRegisterCore.Initialize(ref platform, State, pages));
		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, pages, 5, 6,
			100, 40));
		Assert.Equal(100u, Get(ref platform, first, Width));
		Assert.Equal(0u, Get(ref platform, second, Width));
		Assert.True(MuiSelectgroupCore.SetActive(ref platform, State, pages, -1));
		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, pages, 5, 6,
			100, 40));
		Assert.Equal(0u, Get(ref platform, first, Width));
		Assert.Equal(100u, Get(ref platform, second, Width));
	}

	[Fact]
	public void ScrollgroupAndVirtgroupExposeViewportAndContentGeometry()
	{
		var platform = CreatePlatform(out var cl);
		var scroll = Object(ref platform, cl);
		var contents = Object(ref platform, cl);
		var child = Object(ref platform, cl);
		var horizontalBar = Object(ref platform, cl);
		var verticalBar = Object(ref platform, cl);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, contents, child));
		Set(ref platform, child, FixWidth, 200);
		Set(ref platform, child, FixHeight, 100);
		Set(ref platform, scroll, 0x80421261, contents.Raw);
		Set(ref platform, scroll, 0x804292F3, 1);
		Set(ref platform, scroll, 0x804224F2, 1);
		Set(ref platform, scroll, 0x8042B63D, horizontalBar.Raw);
		Set(ref platform, scroll, 0x8042CDC0, verticalBar.Raw);
		Assert.True(MuiScrollgroupCore.Layout(ref platform, State, scroll, 10, 20,
			100, 80));
		Assert.Equal(200u, Get(ref platform, contents, Width));
		Assert.Equal(100u, Get(ref platform, contents, Height));
		Assert.Equal(88u, Get(ref platform, horizontalBar, Width));
		Assert.Equal(68u, Get(ref platform, verticalBar, Height));

		var virt = Object(ref platform, cl);
		var virtualChild = Object(ref platform, cl);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, virt, virtualChild));
		Set(ref platform, virt, 0x80427C49, 300);
		Set(ref platform, virt, 0x80423038, 150);
		Set(ref platform, virt, 0x80429371, 25);
		Set(ref platform, virt, 0x80425200, 10);
		Assert.True(MuiVirtgroupCore.Layout(ref platform, State, virt, 10, 20,
			100, 60));
		Assert.Equal(unchecked((uint)-15), Get(ref platform, virt, Left));
		Assert.Equal(300u, Get(ref platform, virt, Width));
		Assert.True(MuiVirtgroupCore.TryGetLayoutState(ref platform, State, virt,
			out var layoutState));
		Assert.Equal(MuiVirtgroupLayoutStateRecord.Cookie, layoutState.Magic);
		Assert.Equal(300, layoutState.Width);
		Assert.Equal(150, layoutState.Height);
		Assert.Equal(25, layoutState.Left);
		Assert.Equal(10, layoutState.Top);
		Assert.Equal(0u, layoutState.TryFit);

		Set(ref platform, virt, 0x80429371, 40);
		Assert.True(MuiVirtgroupCore.Layout(ref platform, State, virt, 10, 20,
			100, 60));
		Assert.True(MuiVirtgroupCore.TryGetLayoutState(ref platform, State, virt,
			out layoutState));
		Assert.Equal(40, layoutState.Left);
	}

	[Fact]
	public void VirtgroupPublishesNamedLayoutRecord()
	{
		var platform = CreatePlatform(out var cl);
		var virt = Object(ref platform, cl);
		Set(ref platform, virt, 0x80427C49, 320);
		Set(ref platform, virt, 0x80423038, 180);
		Set(ref platform, virt, 0x80429371, 12);
		Set(ref platform, virt, 0x80425200, 7);
		Set(ref platform, virt, 0x80429427, 1);
		Assert.True(MuiVirtgroupCore.Layout(ref platform, State, virt, 0, 0,
			100, 80));
		Assert.True(MuiVirtgroupCore.TryGetLayoutState(ref platform, State, virt,
			out var value));
		Assert.Equal(MuiVirtgroupLayoutStateRecord.Cookie, value.Magic);
		Assert.Equal(320, value.Width);
		Assert.Equal(180, value.Height);
		Assert.Equal(12, value.Left);
		Assert.Equal(7, value.Top);
		Assert.Equal(1u, value.TryFit);
	}

	[Fact]
	public void VirtgroupPointerInputDragUsesNamedStateAndDispatcher()
	{
		var platform = CreatePlatform(out var groupClass);
		var virtgroupName = APTR.FromPointer(0x1140);
		platform.WriteCString(virtgroupName, "Virtgroup.mui");
		var virtgroupClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, virtgroupName, groupClass, 0, APTR.FromPointer(1), false);
		var virtgroup = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			virtgroupClass, APTR.Null);
		var child = Object(ref platform, groupClass);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, virtgroup, child));
		var virtgroupRecord = MuiHeadlessObjectCore.FindObject(ref platform, State,
			virtgroup);
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			virtgroupRecord, MuiVirtgroupCore.VirtualWidth, 300, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			virtgroupRecord, MuiVirtgroupCore.VirtualHeight, 180, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			virtgroupRecord, MuiVirtgroupCore.VirtualLeft, 30, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			virtgroupRecord, MuiVirtgroupCore.VirtualTop, 20, false));
		Assert.True(MuiVirtgroupCore.Layout(ref platform, State, virtgroup, 10,
			15, 100, 80));
		Assert.True(MuiVirtgroupCore.TryGetDisplayState(ref platform, State,
			virtgroup, out var display));
		Assert.True(MuiVirtgroupCore.TryGetPolicyState(ref platform, State,
			virtgroup, out var policy));
		Assert.Equal(1u, policy.Input);
		Assert.Equal(10, display.Left);
		Assert.Equal(15, display.Top);
		Assert.Equal(100, display.Width);
		Assert.Equal(80, display.Height);

		var intuiMessage = APTR.FromPointer(0x1E00);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intuiMessage,
			(uint)IDCMPFlags.MouseButtons, 0x0068, 0, 0, 50, 40));
		Assert.True(MuiVirtgroupCore.HandleInput(ref platform, State, virtgroup,
			intuiMessage, -1));
		Assert.True(MuiVirtgroupCore.TryGetPointerState(ref platform, State,
			virtgroup, out var dragState));
		Assert.Equal(MuiVirtgroupPointerStateRecord.Cookie, dragState.Magic);
		Assert.Equal(MuiVirtgroupPointerStateRecord.ActiveFlag |
			MuiVirtgroupPointerStateRecord.CapturedFlag, dragState.Flags);
		Assert.Equal(30, dragState.StartLeft);
		Assert.Equal(20, dragState.StartTop);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intuiMessage,
			(uint)IDCMPFlags.MouseMove, 0, 0, 0, 42, 31));
		Assert.True(MuiVirtgroupCore.HandleInput(ref platform, State, virtgroup,
			intuiMessage, -1));
		Assert.Equal(38u, Get(ref platform, virtgroup,
			MuiVirtgroupCore.VirtualLeft));
		Assert.Equal(29u, Get(ref platform, virtgroup,
			MuiVirtgroupCore.VirtualTop));

		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intuiMessage,
			(uint)IDCMPFlags.MouseMove, 0, 0, 0, -1000, -1000));
		Assert.True(MuiVirtgroupCore.HandleInput(ref platform, State, virtgroup,
			intuiMessage, -1));
		Assert.Equal(200u, Get(ref platform, virtgroup,
			MuiVirtgroupCore.VirtualLeft));
		Assert.Equal(100u, Get(ref platform, virtgroup,
			MuiVirtgroupCore.VirtualTop));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intuiMessage,
			(uint)IDCMPFlags.MouseButtons, 0x0069, 0, 0, -1000, -1000));
		Assert.True(MuiVirtgroupCore.HandleInput(ref platform, State, virtgroup,
			intuiMessage, -1));
		Assert.False(MuiVirtgroupCore.TryGetPointerState(ref platform, State,
			virtgroup, out _));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intuiMessage,
			(uint)IDCMPFlags.MouseMove, 0, 0, 0, 0, 0));
		Assert.False(MuiVirtgroupCore.HandleInput(ref platform, State, virtgroup,
			intuiMessage, -1));

		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			virtgroupRecord, MuiVirtgroupCore.VirtualLeft, 0, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			virtgroupRecord, MuiVirtgroupCore.VirtualTop, 0, false));
		Assert.True(MuiVirtgroupCore.Layout(ref platform, State, virtgroup, 10,
			15, 100, 80));
		var packet = APTR.FromPointer(0x1F00);
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intuiMessage,
			(uint)IDCMPFlags.MouseButtons, 0x0068, 0, 0, 50, 40));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intuiMessage.Raw, -1));
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State,
			virtgroup, packet));
		Assert.True(MuiIntuiMessageCodec.WritePointer(ref platform, intuiMessage,
			(uint)IDCMPFlags.MouseMove, 0, 0, 0, 40, 40));
		Assert.True(MuiCollectionSurfaceMessageCodec.WriteHandleInput(ref platform,
			packet, intuiMessage.Raw, -1));
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State,
			virtgroup, packet));
		Assert.Equal(10u, Get(ref platform, virtgroup,
			MuiVirtgroupCore.VirtualLeft));
	}

	[Fact]
	public void ScrollgroupPublishesNamedLayoutRecord()
	{
		var platform = CreatePlatform(out var cl);
		var scroll = Object(ref platform, cl);
		var contents = Object(ref platform, cl);
		var horizontalBar = Object(ref platform, cl);
		var verticalBar = Object(ref platform, cl);
		Set(ref platform, scroll, 0x80421261, contents.Raw);
		Set(ref platform, scroll, 0x804292F3, 1);
		Set(ref platform, scroll, 0x804224F2, 1);
		Set(ref platform, scroll, 0x8042B63D, horizontalBar.Raw);
		Set(ref platform, scroll, 0x8042CDC0, verticalBar.Raw);
		Set(ref platform, scroll, 0x8042CAB1, 0);
		Set(ref platform, scroll, 0x804264C3, 1);
		Assert.True(MuiScrollgroupCore.Layout(ref platform, State, scroll, 0, 0,
			120, 80));
		Assert.True(MuiScrollgroupCore.TryGetLayoutState(ref platform, State,
			scroll, out var value));
		Assert.Equal(MuiScrollgroupLayoutStateRecord.Cookie, value.Magic);
		Assert.Equal(contents.Raw, value.Contents.Raw);
		Assert.Equal(1u, value.FreeHorizontal);
		Assert.Equal(1u, value.FreeVertical);
		Assert.Equal(horizontalBar.Raw, value.HorizontalBar.Raw);
		Assert.Equal(verticalBar.Raw, value.VerticalBar.Raw);
		Assert.Equal(0u, value.NoHorizontalBar);
		Assert.Equal(1u, value.NoVerticalBar);
	}

	[Fact]
	public void ScrollgroupPublishesViewportAndProjectsVirtgroupPropRanges()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var groupName = APTR.FromPointer(0x1100);
		var virtgroupName = APTR.FromPointer(0x1140);
		var scrollgroupName = APTR.FromPointer(0x1180);
		var propName = APTR.FromPointer(0x11C0);
		platform.WriteCString(groupName, "Group.mui");
		platform.WriteCString(virtgroupName, "Virtgroup.mui");
		platform.WriteCString(scrollgroupName, "Scrollgroup.mui");
		platform.WriteCString(propName, "Prop.mui");
		var groupClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			groupName, APTR.Null, 0, APTR.FromPointer(1), false);
		var virtgroupClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, virtgroupName, groupClass, 0, APTR.FromPointer(1), false);
		var scrollgroupClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, scrollgroupName, groupClass, 0, APTR.FromPointer(1), false);
		var propClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			propName, APTR.Null, 0, APTR.FromPointer(1), false);
		var virtgroup = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			virtgroupClass, APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, virtgroup, child));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			MuiHeadlessObjectCore.FindObject(ref platform, State, virtgroup),
			MuiVirtgroupCore.VirtualWidth, 300, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			MuiHeadlessObjectCore.FindObject(ref platform, State, virtgroup),
			MuiVirtgroupCore.VirtualHeight, 150, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			MuiHeadlessObjectCore.FindObject(ref platform, State, virtgroup),
			MuiVirtgroupCore.VirtualLeft, 40, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			MuiHeadlessObjectCore.FindObject(ref platform, State, virtgroup),
			MuiVirtgroupCore.VirtualTop, 10, false));
		var horizontalBar = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, APTR.Null);
		var verticalBar = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, APTR.Null);
		var scrollgroup = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			scrollgroupClass, APTR.Null);
		var scrollgroupRecord = MuiHeadlessObjectCore.FindObject(ref platform, State,
			scrollgroup);
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			scrollgroupRecord, MuiScrollgroupCore.Contents, virtgroup.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			scrollgroupRecord, MuiScrollgroupCore.FreeHorizontal, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			scrollgroupRecord, MuiScrollgroupCore.FreeVertical, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			scrollgroupRecord, MuiScrollgroupCore.HorizontalBar,
			horizontalBar.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			scrollgroupRecord, MuiScrollgroupCore.VerticalBar,
			verticalBar.Raw, false));

		Assert.True(MuiScrollgroupCore.Layout(ref platform, State, scrollgroup, 10,
			20, 100, 80));
		Assert.True(MuiScrollgroupCore.TryGetViewportState(ref platform, State,
			scrollgroup, out var viewport));
		Assert.Equal(MuiScrollgroupViewportStateRecord.Cookie, viewport.Magic);
		Assert.Equal(88, viewport.ViewportWidth);
		Assert.Equal(68, viewport.ViewportHeight);
		Assert.Equal(300, viewport.ContentWidth);
		Assert.Equal(150, viewport.ContentHeight);
		Assert.Equal(212, viewport.MaximumScrollX);
		Assert.Equal(82, viewport.MaximumScrollY);
		Assert.Equal(40, viewport.ScrollLeft);
		Assert.Equal(10, viewport.ScrollTop);
		Assert.Equal(1u, viewport.HorizontalBarVisible);
		Assert.Equal(1u, viewport.VerticalBarVisible);
		Assert.True(MuiCommonControlCore.TryReadPropRangeState(ref platform, State,
			horizontalBar, out var horizontalRange));
		Assert.Equal(300u, horizontalRange.Entries);
		Assert.Equal(88u, horizontalRange.Visible);
		Assert.Equal(40u, horizontalRange.First);
		Assert.True(MuiCommonControlCore.TryReadPropRangeState(ref platform, State,
			verticalBar, out var verticalRange));
		Assert.Equal(150u, verticalRange.Entries);
		Assert.Equal(68u, verticalRange.Visible);
		Assert.Equal(10u, verticalRange.First);
		Assert.Equal(unchecked((uint)-30), Get(ref platform, virtgroup,
			Left));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			MuiHeadlessObjectCore.FindObject(ref platform, State, virtgroup),
			MuiVirtgroupCore.VirtualLeft, 999, false));
		Assert.True(MuiScrollgroupCore.Layout(ref platform, State, scrollgroup, 10,
			20, 100, 80));
		Assert.True(MuiScrollgroupCore.TryGetViewportState(ref platform, State,
			scrollgroup, out viewport));
		Assert.Equal(212, viewport.ScrollLeft);
		Assert.True(MuiCommonControlCore.TryReadPropRangeState(ref platform, State,
			horizontalBar, out horizontalRange));
		Assert.Equal(212u, horizontalRange.First);
	}

	[Fact]
	public void ScrollgroupAutoBarsHidesFittingBarsAndReevaluatesTheOtherAxis()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var groupName = APTR.FromPointer(0x1100);
		var virtgroupName = APTR.FromPointer(0x1140);
		var scrollgroupName = APTR.FromPointer(0x1180);
		var propName = APTR.FromPointer(0x11C0);
		platform.WriteCString(groupName, "Group.mui");
		platform.WriteCString(virtgroupName, "Virtgroup.mui");
		platform.WriteCString(scrollgroupName, "Scrollgroup.mui");
		platform.WriteCString(propName, "Prop.mui");
		var groupClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			groupName, APTR.Null, 0, APTR.FromPointer(1), false);
		var virtgroupClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, virtgroupName, groupClass, 0, APTR.FromPointer(1), false);
		var scrollgroupClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, scrollgroupName, groupClass, 0, APTR.FromPointer(1), false);
		var propClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			propName, APTR.Null, 0, APTR.FromPointer(1), false);
		var virtgroup = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			virtgroupClass, APTR.Null);
		var horizontalBar = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, APTR.Null);
		var verticalBar = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, APTR.Null);
		var scrollgroup = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			scrollgroupClass, APTR.Null);
		var virtgroupRecord = MuiHeadlessObjectCore.FindObject(ref platform, State,
			virtgroup);
		var scrollgroupRecord = MuiHeadlessObjectCore.FindObject(ref platform, State,
			scrollgroup);
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			virtgroupRecord, MuiVirtgroupCore.VirtualWidth, 80, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			virtgroupRecord, MuiVirtgroupCore.VirtualHeight, 50, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			scrollgroupRecord, MuiScrollgroupCore.AutoBars, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			scrollgroupRecord, MuiScrollgroupCore.Contents, virtgroup.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			scrollgroupRecord, MuiScrollgroupCore.FreeHorizontal, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			scrollgroupRecord, MuiScrollgroupCore.FreeVertical, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			scrollgroupRecord, MuiScrollgroupCore.HorizontalBar,
			horizontalBar.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			scrollgroupRecord, MuiScrollgroupCore.VerticalBar,
			verticalBar.Raw, false));

		Assert.True(MuiScrollgroupCore.Layout(ref platform, State, scrollgroup, 0, 0,
			100, 80));
		Assert.True(MuiScrollgroupCore.TryGetViewportState(ref platform, State,
			scrollgroup, out var viewport));
		Assert.Equal(100, viewport.ViewportWidth);
		Assert.Equal(80, viewport.ViewportHeight);
		Assert.Equal(0u, viewport.HorizontalBarVisible);
		Assert.Equal(0u, viewport.VerticalBarVisible);
		Assert.Equal(0u, Get(ref platform, horizontalBar, Width));
		Assert.Equal(0u, Get(ref platform, verticalBar, Height));

		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			virtgroupRecord, MuiVirtgroupCore.VirtualWidth, 130, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			virtgroupRecord, MuiVirtgroupCore.VirtualHeight, 50, false));
		Assert.True(MuiScrollgroupCore.Layout(ref platform, State, scrollgroup, 0, 0,
			100, 80));
		Assert.True(MuiScrollgroupCore.TryGetViewportState(ref platform, State,
			scrollgroup, out viewport));
		Assert.Equal(100, viewport.ViewportWidth);
		Assert.Equal(68, viewport.ViewportHeight);
		Assert.Equal(1u, viewport.HorizontalBarVisible);
		Assert.Equal(0u, viewport.VerticalBarVisible);
		Assert.Equal(100u, Get(ref platform, horizontalBar, Width));
		Assert.Equal(0u, Get(ref platform, verticalBar, Height));

		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			virtgroupRecord, MuiVirtgroupCore.VirtualWidth, 90, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			virtgroupRecord, MuiVirtgroupCore.VirtualHeight, 100, false));
		Assert.True(MuiScrollgroupCore.Layout(ref platform, State, scrollgroup, 0, 0,
			100, 80));
		Assert.True(MuiScrollgroupCore.TryGetViewportState(ref platform, State,
			scrollgroup, out viewport));
		Assert.Equal(88, viewport.ViewportWidth);
		Assert.Equal(68, viewport.ViewportHeight);
		Assert.Equal(1u, viewport.HorizontalBarVisible);
		Assert.Equal(1u, viewport.VerticalBarVisible);
		Assert.Equal(1u, MuiScrollgroupCore.HandleEvent(ref platform, State,
			scrollgroup, APTR.Null, 9));
		Assert.Equal(1u, Get(ref platform, virtgroup,
			MuiVirtgroupCore.VirtualLeft));
		Assert.Equal(1u, MuiScrollgroupCore.HandleEvent(ref platform, State,
			scrollgroup, APTR.Null, 3));
		Assert.Equal(1u, Get(ref platform, virtgroup,
			MuiVirtgroupCore.VirtualTop));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			virtgroupRecord, MuiVirtgroupCore.VirtualLeft, 0, false));
		Assert.True(MuiScrollgroupCore.Layout(ref platform, State, scrollgroup, 0,
			0, 100, 80));
		var eventPacket = APTR.FromPointer(0x1E00);
		Assert.True(MuiCommonControlPacketCore.WriteHandleEvent(ref platform,
			eventPacket, 0, 9, 0));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			scrollgroup, eventPacket));
		Assert.Equal(1u, Get(ref platform, virtgroup,
			MuiVirtgroupCore.VirtualLeft));
	}

	[Fact]
	public void BalanceResizesAdjacentMembersWithoutChangingTotalExtent()
	{
		var platform = CreatePlatform(out var cl);
		var group = Object(ref platform, cl);
		var first = Object(ref platform, cl);
		var balance = Object(ref platform, cl);
		var second = Object(ref platform, cl);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, balance));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, first, 0, 0,
			40, 20));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, balance, 40, 0,
			4, 20));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, second, 44, 0,
			60, 20));
		Assert.True(MuiBalanceCore.ResizeAdjacent(ref platform, State, group,
			balance, 10, true));
		Assert.Equal(50u, Get(ref platform, first, Width));
		Assert.Equal(50u, Get(ref platform, second, Width));
		Assert.Equal(54u, Get(ref platform, second, Left));
		Assert.True(MuiAreaLayoutCore.TryGetGeometryStateRecord(ref platform,
			State, first, out var firstGeometry));
		Assert.Equal(50, firstGeometry.Width);
		Assert.True(MuiAreaLayoutCore.TryGetGeometryStateRecord(ref platform,
			State, second, out var secondGeometry));
		Assert.Equal(50, secondGeometry.Width);
		Assert.Equal(54, secondGeometry.Left);
	}

	[Fact]
	public void BalancePublishesNamedGeometryForVerticalResize()
	{
		var platform = CreatePlatform(out var cl);
		var group = Object(ref platform, cl);
		var first = Object(ref platform, cl);
		var balance = Object(ref platform, cl);
		var second = Object(ref platform, cl);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, balance));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, first, 0, 0,
			20, 40));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, balance, 0, 40,
			20, 4));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, second, 0, 44,
			20, 60));

		Assert.True(MuiBalanceCore.ResizeAdjacent(ref platform, State, group,
			balance, 10, false));
		Assert.True(MuiAreaLayoutCore.TryGetGeometryStateRecord(ref platform,
			State, first, out var firstGeometry));
		Assert.True(MuiAreaLayoutCore.TryGetGeometryStateRecord(ref platform,
			State, second, out var secondGeometry));
		Assert.Equal(50, firstGeometry.Height);
		Assert.Equal(50, secondGeometry.Height);
		Assert.Equal(54, secondGeometry.Top);
	}

	[Fact]
	public void RegisterPoliciesUseNamedStateAndRemainInitializerOnly()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var groupName = APTR.FromPointer(0x1100);
		var registerName = APTR.FromPointer(0x1140);
		platform.WriteCString(groupName, "Group.mui");
		platform.WriteCString(registerName, "Register.mui");
		var groupClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			groupName, APTR.Null, 0, APTR.FromPointer(1), false);
		var registerClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			registerName, groupClass, 0, APTR.FromPointer(1), false);
		var titles = APTR.FromPointer(0x1180);
		platform.WriteCString(titles, "First");
		var tags = APTR.FromPointer(0x1200);
		platform.WriteUInt32(tags, 0, MuiRegisterCore.Frame);
		platform.WriteUInt32(tags, 4, 7);
		platform.WriteUInt32(tags, 8, MuiRegisterCore.Titles);
		platform.WriteUInt32(tags, 12, titles.Raw);
		platform.WriteUInt32(tags, 16, 0);
		platform.WriteUInt32(tags, 20, 0);
		var register = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			registerClass, tags);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, register, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, register, second));
		Assert.True(MuiRegisterCore.Initialize(ref platform, State, register));

		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			register, MuiRegisterCore.Frame, out var frame));
		Assert.Equal(1u, frame);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			register, MuiRegisterCore.Titles, out var titlePointer));
		Assert.Equal(titles.Raw, titlePointer);
		Assert.True(MuiRegisterCore.TryGetAttribute(ref platform, State, register,
			MuiRegisterCore.Frame, out var projected));
		Assert.Equal(1u, projected);
		Assert.True(MuiRegisterCore.TryGetAttribute(ref platform, State, register,
			MuiRegisterCore.Titles, out projected));
		Assert.Equal(titles.Raw, projected);
		Assert.True(MuiRegisterCore.TryGetPolicyState(ref platform, State, register,
			out var policy));
		Assert.Equal(MuiRegisterPolicyStateRecord.Cookie, policy.Magic);
		Assert.Equal(1u, policy.Frame);
		Assert.Equal(titles.Raw, policy.Titles.Raw);

		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			register, MuiRegisterCore.Frame, 0, false));
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			register, MuiRegisterCore.Titles, 0, false));
	}

	[Fact]
	public void SelectgroupActiveUsesNamedStateAndSignedCyclingSelectors()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var groupName = APTR.FromPointer(0x1100);
		var selectgroupName = APTR.FromPointer(0x1140);
		platform.WriteCString(groupName, "Group.mui");
		platform.WriteCString(selectgroupName, "Selectgroup.mui");
		var groupClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			groupName, APTR.Null, 0, APTR.FromPointer(1), false);
		var selectgroupClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, selectgroupName, groupClass, 0, APTR.FromPointer(1), false);
		var tags = APTR.FromPointer(0x1200);
		platform.WriteUInt32(tags, 0, MuiSelectgroupCore.Active);
		platform.WriteUInt32(tags, 4, 1);
		platform.WriteUInt32(tags, 8, 0);
		var selectgroup = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			selectgroupClass, tags);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, selectgroup, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, selectgroup, second));
		Assert.True(MuiRegisterCore.Initialize(ref platform, State, selectgroup));

		Assert.Equal(1u, Get(ref platform, selectgroup, MuiSelectgroupCore.Active));
		Assert.True(MuiSelectgroupCore.TryGetPolicyState(ref platform, State,
			selectgroup, out var policy));
		Assert.Equal(MuiSelectgroupActiveStateRecord.Cookie, policy.Magic);
		Assert.Equal(1u, policy.Active);
		var message = APTR.FromPointer(0x1800);
		var storage = APTR.FromPointer(0x1900);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Storage, storage.Raw));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			MuiSelectgroupCore.Active));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			selectgroup, message));
		Assert.True(MuiGuestUlongStorageCodec.TryRead(ref platform, storage,
			out var stored));
		Assert.Equal(1u, stored.Value);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			selectgroup, MuiSelectgroupCore.Active, unchecked((uint)-1), true));
		Assert.Equal(0u, Get(ref platform, selectgroup,
			MuiSelectgroupCore.Active));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			selectgroup, MuiSelectgroupCore.Active, unchecked((uint)-2), false));
		Assert.Equal(1u, Get(ref platform, selectgroup,
			MuiSelectgroupCore.Active));
	}

	[Fact]
	public void ScrollgroupPoliciesUseNamedStateAndAccessRules()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var groupName = APTR.FromPointer(0x1100);
		var scrollgroupName = APTR.FromPointer(0x1140);
		platform.WriteCString(groupName, "Group.mui");
		platform.WriteCString(scrollgroupName, "Scrollgroup.mui");
		var groupClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			groupName, APTR.Null, 0, APTR.FromPointer(1), false);
		var scrollgroupClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, scrollgroupName, groupClass, 0, APTR.FromPointer(1), false);
		var contents = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var horizontalBar = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var verticalBar = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var tags = APTR.FromPointer(0x1200);
		platform.WriteUInt32(tags, 0, MuiScrollgroupCore.AutoBars);
		platform.WriteUInt32(tags, 4, 7);
		platform.WriteUInt32(tags, 8, MuiScrollgroupCore.Contents);
		platform.WriteUInt32(tags, 12, contents.Raw);
		platform.WriteUInt32(tags, 16, MuiScrollgroupCore.FreeHorizontal);
		platform.WriteUInt32(tags, 20, 5);
		platform.WriteUInt32(tags, 24, MuiScrollgroupCore.FreeVertical);
		platform.WriteUInt32(tags, 28, 0);
		platform.WriteUInt32(tags, 32, MuiScrollgroupCore.NoHorizontalBar);
		platform.WriteUInt32(tags, 36, 9);
		platform.WriteUInt32(tags, 40, MuiScrollgroupCore.NoVerticalBar);
		platform.WriteUInt32(tags, 44, 0);
		platform.WriteUInt32(tags, 48, MuiScrollgroupCore.UseWindowBorder);
		platform.WriteUInt32(tags, 52, 3);
		platform.WriteUInt32(tags, 56, 0);
		var scrollgroup = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			scrollgroupClass, tags);
		var record = MuiHeadlessObjectCore.FindObject(ref platform, State, scrollgroup);
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			record, MuiScrollgroupCore.HorizontalBar, horizontalBar.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			record, MuiScrollgroupCore.VerticalBar, verticalBar.Raw, false));

		Assert.Equal(1u, Get(ref platform, scrollgroup, MuiScrollgroupCore.AutoBars));
		Assert.Equal(contents.Raw, Get(ref platform, scrollgroup,
			MuiScrollgroupCore.Contents));
		Assert.Equal(1u, Get(ref platform, scrollgroup,
			MuiScrollgroupCore.NoHorizontalBar));
		Assert.Equal(horizontalBar.Raw, Get(ref platform, scrollgroup,
			MuiScrollgroupCore.HorizontalBar));
		Assert.Equal(verticalBar.Raw, Get(ref platform, scrollgroup,
			MuiScrollgroupCore.VerticalBar));
		Assert.True(MuiScrollgroupCore.TryGetPolicyState(ref platform, State,
			scrollgroup, out var policy));
		Assert.Equal(MuiScrollgroupPolicyStateRecord.Cookie, policy.Magic);
		Assert.Equal(1u, policy.FreeHorizontal);
		Assert.Equal(0u, policy.FreeVertical);
		Assert.Equal(1u, policy.NoHorizontalBar);
		Assert.Equal(1u, policy.UseWindowBorder);

		var message = APTR.FromPointer(0x1800);
		var storage = APTR.FromPointer(0x1900);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Storage, storage.Raw));
		foreach (var item in new[]
		{
			(MuiScrollgroupCore.AutoBars, 1u),
			(MuiScrollgroupCore.Contents, contents.Raw),
			(MuiScrollgroupCore.NoHorizontalBar, 1u),
			(MuiScrollgroupCore.HorizontalBar, horizontalBar.Raw),
		})
		{
			Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiCommonPacketKind.Get, MuiCommonField.Attribute, item.Item1));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
				scrollgroup, message));
			Assert.True(MuiGuestUlongStorageCodec.TryRead(ref platform, storage,
				out var stored));
			Assert.Equal(item.Item2, stored.Value);
		}

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			scrollgroup, MuiScrollgroupCore.AutoBars, 0, false));
		Assert.Equal(0u, Get(ref platform, scrollgroup, MuiScrollgroupCore.AutoBars));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			scrollgroup, MuiScrollgroupCore.NoVerticalBar, 4, false));
		Assert.Equal(1u, Get(ref platform, scrollgroup,
			MuiScrollgroupCore.NoVerticalBar));
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			scrollgroup, MuiScrollgroupCore.Contents, 0, false));
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			scrollgroup, MuiScrollgroupCore.FreeHorizontal, 0, false));
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			scrollgroup, MuiScrollgroupCore.HorizontalBar, 0, false));
	}

	[Fact]
	public void ScrollgroupUseWindowBorderPropagatesTypedScrollerPolicy()
	{
		var platform = CreatePlatform(out var groupClass);
		var windowName = APTR.FromPointer(0x1180);
		var scrollgroupName = APTR.FromPointer(0x11c0);
		platform.WriteCString(windowName, "Window.mui");
		platform.WriteCString(scrollgroupName, "Scrollgroup.mui");
		var windowClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			windowName, APTR.Null, 0, APTR.FromPointer(1), false);
		var scrollgroupClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, scrollgroupName, groupClass, 0, APTR.FromPointer(1), false);
		var window = Object(ref platform, windowClass);
		var scrollgroupTags = APTR.FromPointer(0x1200);
		platform.WriteUInt32(scrollgroupTags, 0, MuiScrollgroupCore.UseWindowBorder);
		platform.WriteUInt32(scrollgroupTags, 4, 1);
		platform.WriteUInt32(scrollgroupTags, 8, MuiScrollgroupCore.Contents);
		platform.WriteUInt32(scrollgroupTags, 12, 0);
		platform.WriteUInt32(scrollgroupTags, 16, MuiScrollgroupCore.FreeHorizontal);
		platform.WriteUInt32(scrollgroupTags, 20, 1);
		platform.WriteUInt32(scrollgroupTags, 24, MuiScrollgroupCore.FreeVertical);
		platform.WriteUInt32(scrollgroupTags, 28, 1);
		var scrollgroup = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			scrollgroupClass, scrollgroupTags);
		var contents = Object(ref platform, groupClass);
		var child = Object(ref platform, groupClass);
		var horizontalBar = Object(ref platform, groupClass);
		var verticalBar = Object(ref platform, groupClass);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, contents, child));
		Set(ref platform, child, FixWidth, 240);
		Set(ref platform, child, FixHeight, 180);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, window, scrollgroup));
		var scrollgroupRecord = MuiHeadlessObjectCore.FindObject(ref platform, State,
			scrollgroup);
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			scrollgroupRecord, MuiScrollgroupCore.Contents, contents.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			scrollgroupRecord, MuiScrollgroupCore.HorizontalBar,
			horizontalBar.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			scrollgroupRecord, MuiScrollgroupCore.VerticalBar,
			verticalBar.Raw, false));

		Assert.True(MuiLayoutServiceCore.Layout(ref platform, State, scrollgroup, 0,
			0, 100, 80, 0));
		Assert.True(MuiScrollgroupCore.TryGetBorderScrollerState(ref platform, State,
			scrollgroup, out var border));
		Assert.Equal(MuiScrollgroupBorderScrollerStateRecord.Cookie, border.Magic);
		Assert.Equal(window.Raw, border.Window.Raw);
		Assert.Equal(1u, border.UseWindowBorder);
		Assert.Equal(1u, border.HorizontalRequested);
		Assert.Equal(1u, border.VerticalRequested);
		Assert.Equal(1u, border.Applied);
		Assert.Equal(1u, Get(ref platform, window,
			MuiWindowPublicCore.UseBottomBorderScroller));
		Assert.Equal(1u, Get(ref platform, window,
			MuiWindowPublicCore.UseRightBorderScroller));
		Assert.Equal(100u, Get(ref platform, scrollgroup, Width));
		Assert.Equal(0u, Get(ref platform, horizontalBar, Width));
		Assert.Equal(0u, Get(ref platform, verticalBar, Width));
		Assert.True(MuiScrollgroupCore.TryGetViewportState(ref platform, State,
			scrollgroup, out var viewport));
		Assert.Equal(100, viewport.ViewportWidth);
		Assert.Equal(80, viewport.ViewportHeight);
		Assert.True(MuiApplicationWindowCore.OpenWindow(ref platform, State, window,
			0));
		Assert.True(platform.WindowBorderScrollerOperationCount > 0);
		Assert.True(platform.WindowUseBottomBorderScroller);
		Assert.True(platform.WindowUseRightBorderScroller);
		MuiApplicationWindowCore.CloseWindow(ref platform, State, window);
	}

	[Fact]
	public void ScrollgroupPropProjectionNotifiesOnlyWhenRangeChanges()
	{
		var platform = CreatePlatform(out var groupClass);
		var virtgroupName = APTR.FromPointer(0x1140);
		var scrollgroupName = APTR.FromPointer(0x1180);
		var propName = APTR.FromPointer(0x11c0);
		platform.WriteCString(virtgroupName, "Virtgroup.mui");
		platform.WriteCString(scrollgroupName, "Scrollgroup.mui");
		platform.WriteCString(propName, "Prop.mui");
		var virtgroupClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			virtgroupName, groupClass, 0, APTR.FromPointer(1), false);
		var scrollgroupClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, scrollgroupName, groupClass, 0, APTR.FromPointer(1), false);
		var propClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			propName, groupClass, 0, APTR.FromPointer(1), false);
		var contents = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			virtgroupClass, APTR.Null);
		var horizontalBar = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, APTR.Null);
		var verticalBar = MuiCommonControlCore.CreateControl(ref platform, State,
			propClass, APTR.Null);
		var scrollgroupTags = APTR.FromPointer(0x1200);
		platform.WriteUInt32(scrollgroupTags, 0, MuiScrollgroupCore.Contents);
		platform.WriteUInt32(scrollgroupTags, 4, contents.Raw);
		platform.WriteUInt32(scrollgroupTags, 8, MuiScrollgroupCore.FreeHorizontal);
		platform.WriteUInt32(scrollgroupTags, 12, 1);
		platform.WriteUInt32(scrollgroupTags, 16, MuiScrollgroupCore.FreeVertical);
		platform.WriteUInt32(scrollgroupTags, 20, 1);
		platform.WriteUInt32(scrollgroupTags, 24, 0);
		var scrollgroup = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			scrollgroupClass, scrollgroupTags);
		var contentsRecord = MuiHeadlessObjectCore.FindObject(ref platform, State,
			contents);
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			contentsRecord, MuiVirtgroupCore.VirtualWidth, 300, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			contentsRecord, MuiVirtgroupCore.VirtualHeight, 200, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			contentsRecord, MuiVirtgroupCore.VirtualLeft, 20, false));
		var scrollgroupRecord = MuiHeadlessObjectCore.FindObject(ref platform, State,
			scrollgroup);
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			scrollgroupRecord, MuiScrollgroupCore.HorizontalBar,
			horizontalBar.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			scrollgroupRecord, MuiScrollgroupCore.VerticalBar,
			verticalBar.Raw, false));
		var destination = Object(ref platform, groupClass);
		var follow = APTR.FromPointer(0x1800);
		platform.WriteUInt32(follow, 0, 0x90000001);
		Assert.True(MuiNotifyCore.Add(ref platform, State, horizontalBar,
			MuiCommonControlCore.PropFirst, 20, destination, 1, follow));
		var before = platform.DispatchCount;
		Assert.True(MuiLayoutServiceCore.Layout(ref platform, State, scrollgroup, 0,
			0, 100, 80, 0));
		Assert.Equal(20u, Get(ref platform, horizontalBar,
			MuiCommonControlCore.PropFirst));
		Assert.Equal(before + 1, platform.DispatchCount);
		Assert.True(MuiLayoutServiceCore.Layout(ref platform, State, scrollgroup, 0,
			0, 100, 80, 0));
		Assert.Equal(before + 1, platform.DispatchCount);
	}

	[Fact]
	public void VirtgroupPoliciesUseNamedStateAndSignedGeometryAccessRules()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var groupName = APTR.FromPointer(0x1100);
		var virtgroupName = APTR.FromPointer(0x1140);
		platform.WriteCString(groupName, "Group.mui");
		platform.WriteCString(virtgroupName, "Virtgroup.mui");
		var groupClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			groupName, APTR.Null, 0, APTR.FromPointer(1), false);
		var virtgroupClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, virtgroupName, groupClass, 0, APTR.FromPointer(1), false);
		var tags = APTR.FromPointer(0x1200);
		platform.WriteUInt32(tags, 0, MuiVirtgroupCore.Input);
		platform.WriteUInt32(tags, 4, 9);
		platform.WriteUInt32(tags, 8, MuiVirtgroupCore.VirtualLeft);
		platform.WriteUInt32(tags, 12, unchecked((uint)-12));
		platform.WriteUInt32(tags, 16, MuiVirtgroupCore.VirtualTop);
		platform.WriteUInt32(tags, 20, 7);
		platform.WriteUInt32(tags, 24, MuiVirtgroupCore.TryFit);
		platform.WriteUInt32(tags, 28, 5);
		platform.WriteUInt32(tags, 32, 0);
		platform.WriteUInt32(tags, 36, 0);
		var virtgroup = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			virtgroupClass, tags);
		var record = MuiHeadlessObjectCore.FindObject(ref platform, State, virtgroup);
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			record, MuiVirtgroupCore.VirtualWidth, unchecked((uint)320), false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			record, MuiVirtgroupCore.VirtualHeight, unchecked((uint)180), false));

		Assert.Equal(320u, Get(ref platform, virtgroup,
			MuiVirtgroupCore.VirtualWidth));
		Assert.Equal(180u, Get(ref platform, virtgroup,
			MuiVirtgroupCore.VirtualHeight));
		Assert.Equal(unchecked((uint)-12), Get(ref platform, virtgroup,
			MuiVirtgroupCore.VirtualLeft));
		Assert.Equal(7u, Get(ref platform, virtgroup,
			MuiVirtgroupCore.VirtualTop));
		Assert.Equal(1u, Get(ref platform, virtgroup, MuiVirtgroupCore.TryFit));
		Assert.True(MuiVirtgroupCore.TryGetPolicyState(ref platform, State,
			virtgroup, out var policy));
		Assert.Equal(MuiVirtgroupPolicyStateRecord.Cookie, policy.Magic);
		Assert.Equal(1u, policy.Input);
		Assert.Equal(320, policy.Width);
		Assert.Equal(180, policy.Height);
		Assert.Equal(-12, policy.Left);
		Assert.Equal(7, policy.Top);

		var message = APTR.FromPointer(0x1800);
		var storage = APTR.FromPointer(0x1900);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Storage, storage.Raw));
		foreach (var item in new[]
		{
			(MuiVirtgroupCore.VirtualWidth, 320u),
			(MuiVirtgroupCore.VirtualLeft, unchecked((uint)-12)),
			(MuiVirtgroupCore.TryFit, 1u),
		})
		{
			Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiCommonPacketKind.Get, MuiCommonField.Attribute, item.Item1));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
				virtgroup, message));
			Assert.True(MuiGuestUlongStorageCodec.TryRead(ref platform, storage,
				out var stored));
			Assert.Equal(item.Item2, stored.Value);
		}

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			virtgroup, MuiVirtgroupCore.VirtualLeft, unchecked((uint)-20), false));
		Assert.Equal(unchecked((uint)-20), Get(ref platform, virtgroup,
			MuiVirtgroupCore.VirtualLeft));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			virtgroup, MuiVirtgroupCore.TryFit, 0, false));
		Assert.Equal(0u, Get(ref platform, virtgroup, MuiVirtgroupCore.TryFit));
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			virtgroup, MuiVirtgroupCore.Input, 0, false));
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			virtgroup, MuiVirtgroupCore.VirtualWidth, 0, false));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR cl)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Group.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static APTR Object(ref MuiHeadlessTestPlatform platform, APTR cl) =>
		MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl, APTR.Null);

	private static void Set(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute, uint value) => Assert.True(
		MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj, attribute,
			value, false));

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			attribute, out var value));
		return value;
	}
}
