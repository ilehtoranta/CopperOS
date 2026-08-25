using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaWindowRelationshipTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void RenderInfoRelationshipUsesNamedWindowFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1500);
		var input = new MuiAreaLayoutRenderInfoInput
		{
			WindowObject = APTR.FromPointer(0x1800),
			Screen = APTR.FromPointer(0x1810),
			DrawInfo = APTR.FromPointer(0x1820),
			Pens = APTR.FromPointer(0x1830),
			Window = APTR.FromPointer(0x1840),
			RastPort = APTR.FromPointer(0x1850),
			Flags = 0xA5,
		};

		Assert.True(MuiAreaLayoutRecordPacketCore.WriteRenderInfo(ref platform,
			address, input));
		Assert.True(MuiAreaWindowRelationshipPacketCore.TryReadRenderInfo(
			ref platform, address, out var relationship));
		Assert.Equal(input.WindowObject, relationship.WindowObject);
		Assert.Equal(input.Window, relationship.Window);
		Assert.False(MuiAreaWindowRelationshipPacketCore.TryReadRenderInfo(
			ref platform, APTR.FromPointer(0x21000u), out _));
	}

	[Fact]
	public void GetterOnlyRelationshipsAreNullBeforeSetup()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);

		Assert.True(MuiAreaWindowRelationshipPacketCore.TryGet(ref platform, State,
			obj, out var relationship));
		Assert.Equal(APTR.Null, relationship.WindowObject);
		Assert.Equal(APTR.Null, relationship.Window);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj,
			MuiCommonControlCore.WindowObject, out var value, out var handled));
		Assert.True(handled);
		Assert.Equal(0u, value);
	}

	[Fact]
	public void CommonGetterProjectsWindowAndWindowObjectFromRenderInfo()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var renderInfo = APTR.FromPointer(0x1600);
		var windowObject = APTR.FromPointer(0x1900);
		var window = APTR.FromPointer(0x1910);
		var input = new MuiAreaLayoutRenderInfoInput
		{
			WindowObject = windowObject,
			Window = window,
		};
		Assert.True(MuiAreaLayoutRecordPacketCore.WriteRenderInfo(ref platform,
			renderInfo, input));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiAreaWindowRelationshipCore.RenderInfoAttribute, renderInfo.Raw, false));

		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj,
			MuiCommonControlCore.WindowObject, out var value, out var handled));
		Assert.True(handled);
		Assert.Equal(windowObject.Raw, value);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj,
			MuiCommonControlCore.Window, out value, out handled));
		Assert.True(handled);
		Assert.Equal(window.Raw, value);
	}

	[Fact]
	public void GetterOnlyRelationshipsRejectCommonSet()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			obj, MuiCommonControlCore.Window, 0x1900, false));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			obj, MuiCommonControlCore.WindowObject, 0x1910, false));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR areaClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Text.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		areaClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
