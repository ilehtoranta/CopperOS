using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaTextColorTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void TextColorIsGetterOnlyAndSetupScoped()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);

		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiCommonControlCore.TextColor, out var beforeSetup));
		Assert.Equal(0u, beforeSetup);
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			obj, MuiCommonControlCore.TextColor, 0x00112233, false));

		platform.MuiTextColorValue = 0x12ABCDEFu;
		var renderInfo = APTR.FromPointer(0x1300);
		platform.WriteUInt32(renderInfo, 20, 0x1400);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, obj, renderInfo));
		Assert.Equal(1u, platform.MuiTextColorRequestCount);
		Assert.Equal(obj, platform.LastMuiTextColorObject);
		Assert.Equal(renderInfo, platform.LastMuiTextColorRenderInfo);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiCommonControlCore.TextColor, out var activeColor));
		Assert.Equal(0x00ABCDEFu, activeColor);

		Assert.True(MuiAreaLayoutCore.Cleanup(ref platform, State, obj));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiCommonControlCore.TextColor, out var afterCleanup));
		Assert.Equal(0u, afterCleanup);
	}

	[Fact]
	public void TextColorIsForwardedToGraphicsWhenAreaTextIsDrawn()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		platform.MuiTextColorValue = 0x00112233u;
		var renderInfo = APTR.FromPointer(0x1700);
		var rastPort = APTR.FromPointer(0x1800);
		var text = APTR.FromPointer(0x1900);
		platform.WriteUInt32(renderInfo, 20, rastPort.Raw);
		platform.WriteCString(text, "MUI");
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, obj, renderInfo));
		Assert.True(MuiAreaLayoutCore.DrawText(ref platform, State, obj, 2, 3,
			40, 12, text, 3));
		Assert.Equal(1u, platform.MuiTextColorApplyCount);
		Assert.Equal(rastPort.Raw, platform.LastMuiTextColorRastPort.Raw);
		Assert.Equal(0x00112233u, platform.LastAppliedMuiTextColor);
		Assert.True(MuiAreaLayoutCore.Cleanup(ref platform, State, obj));
	}

	[Fact]
	public void TextColorRecordUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1500);
		var expected = new MuiAreaTextColorStateRecord
		{
			Magic = MuiAreaTextColorStateRecord.Cookie,
			Color = 0x00C0FFEE,
			Active = 1,
			Generation = 9,
		};
		Assert.True(MuiAreaTextColorStateRecordCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiAreaTextColorStateRecordCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Color, actual.Color);
		Assert.Equal(expected.Active, actual.Active);
		Assert.Equal(expected.Generation, actual.Generation);
		var cursor = new MuiAreaTextColorStateFieldCursor
		{
			Record = address,
			Field = MuiAreaTextColorStateField.Generation,
		};
		Assert.True(MuiAreaTextColorStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 12, fieldAddress.Raw);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR areaClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Text.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		areaClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
