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
