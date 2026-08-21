using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaFrameVisibleTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void CommonControlDrawHonorsNamedFrameVisiblePolicy()
	{
		var platform = CreatePlatform(out var rectangleClass);
		var rectangle = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			rectangleClass, APTR.Null);
		var renderInfo = APTR.FromPointer(0x1300);
		platform.WriteUInt32(renderInfo, 20, 0x1400);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, rectangle,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, rectangle, 2, 3,
			20, 10));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.Frame, 1, false));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.FrameVisible, 0, false));

		Assert.True(MuiCommonControlCore.DrawControl(ref platform, State, rectangle,
			0));
		Assert.Equal(1u, platform.FillCount);
		Assert.Equal(0u, platform.LineCount);
		Assert.Equal(0u, Get(ref platform, rectangle,
			MuiCommonControlCore.FrameVisible));

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.FrameVisible, 1, false));
		Assert.True(MuiCommonControlCore.DrawControl(ref platform, State, rectangle,
			0));
		Assert.Equal(4u, platform.LineCount);
	}

	[Fact]
	public void AreaDrawHonorsFrameVisibleInRenderPolicyRecord()
	{
		var platform = CreatePlatform(out var areaClass);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, area,
			MuiCommonControlCore.Frame, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, area,
			MuiCommonControlCore.FrameVisible, 0, false));
		var renderInfo = APTR.FromPointer(0x1300);
		platform.WriteUInt32(renderInfo, 20, 0x1400);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, area, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, area, 2, 3,
			20, 10));
		Assert.True(MuiAreaLayoutCore.Draw(ref platform, State, area, 0));
		Assert.Equal(1u, platform.FillCount);
		Assert.Equal(0u, platform.LineCount);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, area,
			MuiCommonControlCore.FrameVisible, 1, false));
		Assert.True(MuiAreaLayoutCore.Draw(ref platform, State, area, 0));
		Assert.Equal(4u, platform.LineCount);
	}

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj,
			attribute, out var value, out var handled));
		Assert.True(handled);
		return value;
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR rectangleClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Rectangle.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		rectangleClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
