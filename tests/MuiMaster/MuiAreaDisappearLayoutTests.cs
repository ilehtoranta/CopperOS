using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaDisappearLayoutTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint FixWidth = 0x8042A3F1;
	private const uint FixHeight = 0x8042A92B;
	private const uint Horizontal = 0x8042536B;
	private const uint HorizontalSpacing = 0x8042C651;
	private const uint VerticalSpacing = 0x8042E1BF;
	private const uint LeftEdge = 0x8042BEC6;
	private const uint TopEdge = 0x8042509B;
	private const uint Width = 0x8042B59C;
	private const uint Height = 0x80423237;

	[Fact]
	public void HorizontalGroupHidesSmallestPositivePriorityFirst()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var third = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, third));
		Set(ref platform, group, Horizontal, 1);
		Set(ref platform, group, HorizontalSpacing, 5);
		foreach (var child in new[] { first, second, third })
		{
			Set(ref platform, child, FixWidth, 30);
			Set(ref platform, child, FixHeight, 10);
		}
		Set(ref platform, first, MuiCommonControlCore.HorizDisappear, 1);
		Set(ref platform, second, MuiCommonControlCore.HorizDisappear, 2);
		var minMax = APTR.FromPointer(0x1800);
		Assert.True(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			minMax));
		Assert.Equal((ushort)30, platform.ReadUInt16(minMax, 0));

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 10, 20,
			65, 10));

		Assert.Equal(0u, Get(ref platform, first, Width));
		Assert.Equal(0u, Get(ref platform, first, Height));
		Assert.Equal(30u, Get(ref platform, second, Width));
		Assert.Equal(30u, Get(ref platform, third, Width));
		Assert.Equal(10u, Get(ref platform, second, LeftEdge));
		Assert.Equal(45u, Get(ref platform, third, LeftEdge));
		Assert.Equal(20u, Get(ref platform, second, TopEdge));
	}

	[Fact]
	public void VerticalGroupUsesIndependentVerticalPriorities()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var third = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, third));
		Set(ref platform, group, VerticalSpacing, 5);
		foreach (var child in new[] { first, second, third })
		{
			Set(ref platform, child, FixWidth, 10);
			Set(ref platform, child, FixHeight, 20);
		}
		Set(ref platform, first, MuiCommonControlCore.VertDisappear, 7);
		Set(ref platform, second, MuiCommonControlCore.VertDisappear, 3);
		var minMax = APTR.FromPointer(0x1800);
		Assert.True(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			minMax));
		Assert.Equal((ushort)20, platform.ReadUInt16(minMax, 2));

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 4, 8,
			10, 45));

		Assert.Equal(20u, Get(ref platform, first, Height));
		Assert.Equal(0u, Get(ref platform, second, Height));
		Assert.Equal(20u, Get(ref platform, third, Height));
		Assert.Equal(8u, Get(ref platform, first, TopEdge));
		Assert.Equal(33u, Get(ref platform, third, TopEdge));
	}

	[Fact]
	public void ZeroAreaDisappearanceIsRenderNoOpBeforeRenderPortAccess()
	{
		var platform = CreatePlatform(out var cl);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, area, 2, 3, 0,
			0));
		Assert.True(MuiAreaLayoutCore.Draw(ref platform, State, area, 0));
		Assert.Equal(0u, platform.LayerDepth);
		Assert.Equal(0u, platform.FillCount);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR cl)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Area.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

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
