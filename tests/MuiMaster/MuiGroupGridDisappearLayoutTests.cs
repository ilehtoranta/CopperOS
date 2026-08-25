using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGroupGridDisappearLayoutTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint FixWidth = 0x8042A3F1;
	private const uint FixHeight = 0x8042A92B;
	private const uint Columns = 0x8042F416;
	private const uint Rows = 0x8042B68F;
	private const uint HorizontalSpacing = 0x8042C651;
	private const uint VerticalSpacing = 0x8042E1BF;
	private const uint LeftEdge = 0x8042BEC6;
	private const uint TopEdge = 0x8042509B;
	private const uint Width = 0x8042B59C;
	private const uint Height = 0x80423237;

	[Fact]
	public void HorizontalPriorityZeroesGridChildWithoutRepackingCells()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Set(ref platform, group, Columns, 2);
		Set(ref platform, group, Rows, 1);
		Set(ref platform, group, HorizontalSpacing, 5);
		Set(ref platform, first, FixWidth, 30);
		Set(ref platform, first, FixHeight, 10);
		Set(ref platform, second, FixWidth, 30);
		Set(ref platform, second, FixHeight, 10);
		Set(ref platform, first, MuiCommonControlCore.HorizDisappear, 1);

		var minMax = APTR.FromPointer(0x1800);
		Assert.True(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			minMax));
		Assert.Equal((ushort)35, platform.ReadUInt16(minMax, 0));

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 10, 20,
			35, 10));
		Assert.Equal(0u, Get(ref platform, first, Width));
		Assert.Equal(0u, Get(ref platform, first, Height));
		Assert.Equal(15u, Get(ref platform, second, Width));
		Assert.Equal(30u, Get(ref platform, second, LeftEdge));
	}

	[Fact]
	public void VerticalPriorityZeroesGridChildWithoutRepackingRows()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Set(ref platform, group, Columns, 1);
		Set(ref platform, group, Rows, 2);
		Set(ref platform, group, VerticalSpacing, 5);
		Set(ref platform, first, FixWidth, 10);
		Set(ref platform, first, FixHeight, 30);
		Set(ref platform, second, FixWidth, 10);
		Set(ref platform, second, FixHeight, 30);
		Set(ref platform, first, MuiCommonControlCore.VertDisappear, 1);

		var minMax = APTR.FromPointer(0x1800);
		Assert.True(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			minMax));
		Assert.Equal((ushort)35, platform.ReadUInt16(minMax, 2));

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 4, 8,
			10, 35));
		Assert.Equal(0u, Get(ref platform, first, Width));
		Assert.Equal(0u, Get(ref platform, first, Height));
		Assert.Equal(15u, Get(ref platform, second, Height));
		Assert.Equal(28u, Get(ref platform, second, TopEdge));
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
