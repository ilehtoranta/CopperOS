using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaInnerSpacingDrawTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void CommonControlDrawingUsesNamedInnerSpacingContentRect()
	{
		var platform = CreatePlatform(out var textClass);
		var source = APTR.FromPointer(0x1500);
		platform.WriteCString(source, "X");
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, APTR.Null);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			text, MuiCommonControlCore.TextContents, source.Raw, false));
		SetRaw(ref platform, text, MuiCommonControlCore.InnerLeft, 3);
		SetRaw(ref platform, text, MuiCommonControlCore.InnerRight, 4);
		SetRaw(ref platform, text, MuiCommonControlCore.InnerTop, 2);
		SetRaw(ref platform, text, MuiCommonControlCore.InnerBottom, 1);

		var renderInfo = APTR.FromPointer(0x1300);
		platform.WriteUInt32(renderInfo, 20, 0x1400);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, text,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, text, 10, 5,
			40, 20));

		Assert.True(MuiCommonControlCore.DrawControl(ref platform, State, text,
			0));
		Assert.Equal(1u, platform.TextCount);
		Assert.Equal(13, platform.LastTextLeft);
		// Text.mui uses the content top plus its 8-pixel host font line height.
		Assert.Equal(15, platform.LastTextBaseline);
	}

	[Fact]
	public void ContentRectClampsInsetsToOuterGeometry()
	{
		var platform = CreatePlatform(out var textClass);
		var text = MuiCommonControlCore.CreateControl(ref platform, State,
			textClass, APTR.Null);
		SetRaw(ref platform, text, MuiCommonControlCore.InnerLeft, 10);
		SetRaw(ref platform, text, MuiCommonControlCore.InnerRight, 10);
		SetRaw(ref platform, text, MuiCommonControlCore.InnerTop, 5);
		SetRaw(ref platform, text, MuiCommonControlCore.InnerBottom, 5);
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, text, -10, 4,
			5, 3));
		Assert.True(MuiAreaLayoutCore.TryReadGeometryState(ref platform, State,
			text, out var geometry));
		Assert.True(MuiAreaLayoutCore.TryResolveContentRect(ref platform, State,
			text, geometry, out var content));
		Assert.Equal(-5, content.Left);
		Assert.Equal(7, content.Top);
		Assert.Equal(0, content.Width);
		Assert.Equal(0, content.Height);
	}

	private static void SetRaw(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute, uint value)
	{
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			attribute, value, false));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR textClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Text.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		textClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
