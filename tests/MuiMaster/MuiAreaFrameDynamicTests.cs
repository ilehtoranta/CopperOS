using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaFrameDynamicTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void CommonControlFrameDynamicUsesNamedPolicyAndAllowsRuntimeSet()
	{
		var platform = CreatePlatform(out var rectangleClass);
		var rectangle = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			rectangleClass, APTR.Null);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.FrameDynamic, 2, false));
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, rectangle,
			MuiCommonControlCore.FrameDynamic, out var value, out var handled));
		Assert.True(handled);
		Assert.Equal(1u, value);
		Assert.True(MuiAreaLayoutCore.TryReadRenderPolicyState(ref platform, State,
			rectangle, out var policy));
		Assert.Equal(1u, policy.FrameDynamic);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.FrameDynamic, 0, false));
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, rectangle,
			MuiCommonControlCore.FrameDynamic, out value, out handled));
		Assert.True(handled);
		Assert.Equal(0u, value);
	}

	[Fact]
	public void AreaConstructionPublishesFrameDynamicInNamedPolicy()
	{
		var platform = CreatePlatform(out var areaClass);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, area,
			MuiCommonControlCore.FrameDynamic, 1, false));

		Assert.True(MuiAreaLayoutCore.TryReadRenderPolicyState(ref platform, State,
			area, out var policy));
		Assert.Equal(1u, policy.FrameDynamic);
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
