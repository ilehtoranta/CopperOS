using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaShowMeDrawTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void CommonControlHiddenByShowMeIsAZeroRenderNoOp()
	{
		var platform = CreatePlatform(out var rectangleClass);
		var rectangle = MuiCommonControlCore.CreateControl(ref platform, State,
			rectangleClass, APTR.Null);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.Frame, 1, false));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.ShowMe, 0, false));

		// No setup/render-info is required for a hidden object.  The named
		// visibility record is consulted before the render-port boundary.
		Assert.True(MuiCommonControlCore.DrawControl(ref platform, State,
			rectangle, 0));
		Assert.Equal(0u, platform.FillCount);
		Assert.Equal(0u, platform.LineCount);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.ShowMe, 1, false));
		var renderInfo = APTR.FromPointer(0x1300);
		platform.WriteUInt32(renderInfo, 20, 0x1400);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, rectangle,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, rectangle, 2,
			3, 20, 10));
		Assert.True(MuiCommonControlCore.DrawControl(ref platform, State,
			rectangle, 0));
		Assert.Equal(1u, platform.FillCount);
		Assert.Equal(4u, platform.LineCount);
	}

	[Fact]
	public void AreaHiddenByShowMeIsAZeroRenderNoOp()
	{
		var platform = CreatePlatform(out var areaClass);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, area,
			MuiCommonControlCore.Frame, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, area,
			MuiCommonControlCore.ShowMe, 0, false));

		Assert.True(MuiAreaLayoutCore.Draw(ref platform, State, area, 0));
		Assert.Equal(0u, platform.FillCount);
		Assert.Equal(0u, platform.LineCount);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, area,
			MuiCommonControlCore.ShowMe, 1, false));
		var renderInfo = APTR.FromPointer(0x1300);
		platform.WriteUInt32(renderInfo, 20, 0x1400);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, area,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, area, 2, 3,
			20, 10));
		Assert.True(MuiAreaLayoutCore.Draw(ref platform, State, area, 0));
		Assert.Equal(1u, platform.FillCount);
		Assert.Equal(4u, platform.LineCount);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR areaClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Rectangle.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		areaClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
