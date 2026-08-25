using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaFrameTitleTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void CommonControlDrawCentersNamedFrameTitle()
	{
		var platform = CreatePlatform(out var rectangleClass);
		var rectangle = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			rectangleClass, APTR.Null);
		var title = APTR.FromPointer(0x1500);
		platform.WriteCString(title, "Title");
		var renderInfo = APTR.FromPointer(0x1300);
		platform.WriteUInt32(renderInfo, 20, 0x1400);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, rectangle,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, rectangle, 2, 3,
			80, 10));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.Frame, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.FrameTitle, title.Raw, false));

		Assert.True(MuiCommonControlCore.DrawControl(ref platform, State, rectangle,
			0));
		Assert.Equal(1u, platform.TextCount);
		Assert.Equal(title, platform.LastText);
		Assert.Equal(5, platform.LastTextLength);
		Assert.Equal(22, platform.LastTextLeft);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, rectangle,
			MuiCommonControlCore.FrameTitle, out var value, out var handled));
		Assert.True(handled);
		Assert.Equal(title.Raw, value);
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.FrameTitle, 0, false));
	}

	[Fact]
	public void AreaDrawUsesTheSameNamedFrameTitlePolicy()
	{
		var platform = CreatePlatform(out var areaClass);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var title = APTR.FromPointer(0x1500);
		platform.WriteCString(title, "Title");
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, area,
			MuiCommonControlCore.Frame, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, area,
			MuiCommonControlCore.FrameTitle, title.Raw, false));
		var renderInfo = APTR.FromPointer(0x1300);
		platform.WriteUInt32(renderInfo, 20, 0x1400);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, area, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, area, 2, 3,
			80, 10));

		Assert.True(MuiAreaLayoutCore.Draw(ref platform, State, area, 0));
		Assert.Equal(1u, platform.TextCount);
		Assert.Equal(title, platform.LastText);
		Assert.Equal(5, platform.LastTextLength);
		Assert.Equal(22, platform.LastTextLeft);
		Assert.True(MuiAreaLayoutCore.TryGetRenderPolicyState(ref platform, State,
			area, out var policy));
		Assert.Equal(title, policy.FrameTitle);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR classRecord)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Rectangle.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		classRecord = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
