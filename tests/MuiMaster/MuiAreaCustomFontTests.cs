using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaCustomFontTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void CustomFontProjectsCallerOwnedSpecAndTracksRuntimeChanges()
	{
		var platform = CreatePlatform(out var areaClass);
		var firstSpec = APTR.FromPointer(0x1300);
		var secondSpec = APTR.FromPointer(0x1400);
		platform.WriteCString(firstSpec, "Noto Sans UI/20");
		platform.WriteCString(secondSpec, "/+10/o");
		var tags = APTR.FromPointer(0x1200);
		platform.WriteUInt32(tags, 0, MuiCommonControlCore.CustomFont);
		platform.WriteUInt32(tags, 4, firstSpec.Raw);
		platform.WriteUInt32(tags, 8, 0);
		var obj = MuiCommonControlCore.CreateControl(ref platform, State,
			areaClass, tags);

		Assert.True(MuiAreaCustomFontPacketCore.TryGet(ref platform, State, obj,
			out var initial));
		Assert.Equal(firstSpec.Raw, initial.Spec.Raw);
		Assert.Equal(1u, initial.Present);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiCommonControlCore.CustomFont, out var generic));
		Assert.Equal(firstSpec.Raw, generic);

		Assert.True(MuiAreaCustomFontPacketCore.Set(ref platform, State, obj,
			secondSpec, false));
		Assert.True(MuiAreaCustomFontPacketCore.TryGet(ref platform, State, obj,
			out var changed));
		Assert.Equal(secondSpec.Raw, changed.Spec.Raw);
		Assert.Equal(2u, changed.Generation);

		// A present NULL is distinct from an omitted attribute, but the generic
		// ULONG getter still returns the NULL pointer value.
		Assert.True(MuiAreaCustomFontPacketCore.Set(ref platform, State, obj,
			APTR.Null, false));
		Assert.True(MuiAreaCustomFontPacketCore.TryGet(ref platform, State, obj,
			out var cleared));
		Assert.Equal(1u, cleared.Present);
		Assert.Equal(0u, cleared.Spec.Raw);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiCommonControlCore.CustomFont, out generic));
		Assert.Equal(0u, generic);
	}

	[Fact]
	public void CustomFontRejectsUnmappedSpecWithoutChangingState()
	{
		var platform = CreatePlatform(out var areaClass);
		var spec = APTR.FromPointer(0x1300);
		platform.WriteCString(spec, "Noto Sans UI/20");
		var obj = MuiCommonControlCore.CreateControl(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiAreaCustomFontPacketCore.Set(ref platform, State, obj,
			spec, false));
		Assert.False(MuiAreaCustomFontPacketCore.Set(ref platform, State, obj,
			APTR.FromPointer(0x1), false));
		Assert.True(MuiAreaCustomFontPacketCore.TryGet(ref platform, State, obj,
			out var value));
		Assert.Equal(spec.Raw, value.Spec.Raw);
		Assert.Equal(1u, value.Present);
	}

	[Fact]
	public void CustomFontRecordUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1500);
		var expected = new MuiAreaCustomFontStateRecord
		{
			Magic = MuiAreaCustomFontStateRecord.Cookie,
			Spec = APTR.FromPointer(0x1600),
			Present = 1,
			Generation = 3,
		};
		Assert.True(MuiAreaCustomFontStateRecordCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiAreaCustomFontStateRecordCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Spec.Raw, actual.Spec.Raw);
		Assert.Equal(expected.Present, actual.Present);
		Assert.Equal(expected.Generation, actual.Generation);
		var cursor = new MuiAreaCustomFontStateFieldCursor
		{
			Record = address,
			Field = MuiAreaCustomFontStateField.Generation,
		};
		Assert.True(MuiAreaCustomFontStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 12, fieldAddress.Raw);
	}

	[Fact]
	public void CustomFontSpecParsesNamedFamilySizeStylesAndColors()
	{
		var platform = CreatePlatform(out _);
		var spec = APTR.FromPointer(0x1700);
		platform.WriteCString(spec, "Noto Sans UI/+10/o/Cff0000/c112233");
		Assert.True(MuiAreaCustomFontPacketCore.TryParse(ref platform, spec,
			out var parsed));
		Assert.Equal(spec.Raw, parsed.Source.Raw);
		Assert.Equal(spec.Raw, parsed.Family.Raw);
		Assert.Equal(12u, parsed.FamilyLength);
		Assert.Equal(10, parsed.Size);
		Assert.Equal(MuiCustomFontSpecFlags.SizeRelative, parsed.SizeMode);
		Assert.Equal(0x00FF0000u, parsed.OutlineColor);
		Assert.Equal(0x00112233u, parsed.TextColor);
		Assert.True((parsed.StyleFlags & MuiCustomFontSpecFlags.Outline) != 0);
		Assert.True((parsed.ValueFlags & MuiCustomFontSpecFlags.HasSize) != 0);
		Assert.True((parsed.ValueFlags & MuiCustomFontSpecFlags.HasTextColor) != 0);
		Assert.True((parsed.ValueFlags & MuiCustomFontSpecFlags.HasOutlineColor) != 0);
	}

	[Fact]
	public void CustomFontSpecAcceptsEmptyFamilyAndRejectsUnknownTokens()
	{
		var platform = CreatePlatform(out _);
		var relative = APTR.FromPointer(0x1800);
		platform.WriteCString(relative, "//s");
		Assert.True(MuiAreaCustomFontPacketCore.TryParse(ref platform, relative,
			out var parsed));
		Assert.Equal(0u, parsed.FamilyLength);
		Assert.True((parsed.StyleFlags & MuiCustomFontSpecFlags.Shadow) != 0);

		var invalid = APTR.FromPointer(0x1900);
		platform.WriteCString(invalid, "Noto Sans UI/q");
		Assert.False(MuiAreaCustomFontPacketCore.TryParse(ref platform, invalid,
			out _));
	}

	[Fact]
	public void FontAndCustomFontUseLastSetterPrecedenceAndExposeEffectiveSpec()
	{
		var platform = CreatePlatform(out var areaClass);
		var font = APTR.FromPointer(0x1A00);
		var spec = APTR.FromPointer(0x1B00);
		platform.WriteCString(spec, "/+10/o");
		var obj = MuiCommonControlCore.CreateControl(ref platform, State,
			areaClass, APTR.Null);

		Assert.True(MuiAreaCustomFontPacketCore.Set(ref platform, State, obj,
			spec, false));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			obj, MuiCommonControlCore.Font, font.Raw, false));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiCommonControlCore.Font, out var effectiveFont));
		Assert.Equal(font.Raw, effectiveFont);
		Assert.True(MuiAreaCustomFontPacketCore.TryGetEffective(ref platform,
			State, obj, out var effective));
		Assert.Equal(0u, effective.Present);

		Assert.True(MuiAreaCustomFontPacketCore.Set(ref platform, State, obj,
			spec, false));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiCommonControlCore.Font, out effectiveFont));
		Assert.Equal(font.Raw, effectiveFont);
		Assert.True(MuiAreaCustomFontPacketCore.TryGetEffective(ref platform,
			State, obj, out effective));
		Assert.Equal(1u, effective.Present);
		Assert.Equal(font.Raw, effective.BaseFont.Raw);
		Assert.Equal(spec.Raw, effective.Spec.Source.Raw);
		Assert.Equal(10, effective.Spec.Size);
		Assert.True((effective.Spec.StyleFlags &
			MuiCustomFontSpecFlags.Outline) != 0);
	}

	[Fact]
	public void FontSelectionRecordUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1C00);
		var expected = new MuiAreaFontSelectionStateRecord
		{
			Magic = MuiAreaFontSelectionStateRecord.Cookie,
			Active = (uint)MuiAreaFontSelectionKind.CustomFont,
			Source = APTR.FromPointer(0x1D00),
			Generation = 4,
		};
		Assert.True(MuiAreaFontSelectionStateRecordCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiAreaFontSelectionStateRecordCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Active, actual.Active);
		Assert.Equal(expected.Source.Raw, actual.Source.Raw);
		Assert.Equal(expected.Generation, actual.Generation);
		var cursor = new MuiAreaFontSelectionStateFieldCursor
		{
			Record = address,
			Field = MuiAreaFontSelectionStateField.Source,
		};
		Assert.True(MuiAreaFontSelectionStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 8, fieldAddress.Raw);
	}

	[Fact]
	public void DirectRawFontWritesReconcileTheSelectionRecord()
	{
		var platform = CreatePlatform(out var areaClass);
		var spec = APTR.FromPointer(0x1E00);
		platform.WriteCString(spec, "/+10");
		var obj = MuiCommonControlCore.CreateControl(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiCommonControlCore.CustomFont, spec.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiCommonControlCore.Font, 0x00001F00, false));
		Assert.True(MuiAreaCustomFontPacketCore.TryGetEffective(ref platform,
			State, obj, out var effective));
		Assert.Equal(0u, effective.Present);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiCommonControlCore.CustomFont, spec.Raw, false));
		Assert.True(MuiAreaCustomFontPacketCore.TryGetEffective(ref platform,
			State, obj, out effective));
		Assert.Equal(1u, effective.Present);
		Assert.Equal(0x00001F00u, effective.BaseFont.Raw);
	}

	[Fact]
	public void CustomFontOpensOnSetupAndClosesOnCleanup()
	{
		var platform = CreatePlatform(out var areaClass);
		var spec = APTR.FromPointer(0x1F00);
		platform.WriteCString(spec, "Noto Sans UI/+10/o");
		var obj = MuiCommonControlCore.CreateControl(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiAreaCustomFontPacketCore.Set(ref platform, State, obj,
			spec, false));
		var renderInfo = APTR.FromPointer(0x2000);
		platform.WriteUInt32(renderInfo, 20, 0x2100);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, obj, renderInfo));
		Assert.Equal(1u, platform.CustomFontOpenCount);
		Assert.Equal(spec.Raw, platform.LastCustomFontSpec.Source.Raw);
		Assert.True(MuiAreaCustomFontPacketCore.TryGetRuntime(ref platform, State,
			obj, out var runtime));
		Assert.Equal(platform.LastCustomFontHandle.Raw, runtime.Font.Raw);
		Assert.True(MuiAreaCustomFontPacketCore.TryGetEffective(ref platform,
			State, obj, out var effective));
		Assert.Equal(runtime.Font.Raw, effective.BaseFont.Raw);

		Assert.True(MuiAreaLayoutCore.Cleanup(ref platform, State, obj));
		Assert.Equal(1u, platform.CustomFontCloseCount);
		Assert.Equal(runtime.Font.Raw, platform.LastClosedCustomFont.Raw);
		Assert.False(MuiAreaCustomFontPacketCore.TryGetRuntime(ref platform, State,
			obj, out _));
	}

	[Fact]
	public void CustomFontMetricsReachLayoutAndDrawingThroughTheOpenedHandle()
	{
		var platform = CreatePlatform(out var areaClass);
		var spec = APTR.FromPointer(0x2E00);
		var text = APTR.FromPointer(0x2F00);
		platform.WriteCString(spec, "/+10");
		platform.WriteCString(text, "MUI!");
		var obj = MuiCommonControlCore.CreateControl(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiAreaCustomFontPacketCore.Set(ref platform, State, obj,
			spec, false));
		var renderInfo = APTR.FromPointer(0x3000);
		var rastPort = APTR.FromPointer(0x3100);
		platform.WriteUInt32(renderInfo, 20, rastPort.Raw);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, obj, renderInfo));
		Assert.True(MuiAreaCustomFontPacketCore.TryGetRuntime(ref platform, State,
			obj, out var runtime));
		Assert.True(platform.TryGetMuiCustomFontMetrics(runtime.Font,
			out var metrics));
		Assert.Equal(9, metrics.GlyphWidth);
		Assert.Equal(18, metrics.Height);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			obj, MuiCommonControlCore.TextContents, text.Raw, false));
		Assert.True(MuiCommonControlCore.TryComputeMinMax(ref platform, State,
			obj, out var minMax));
		Assert.Equal(36, minMax.MinWidth);
		Assert.Equal(18, minMax.MinHeight);
		Assert.Equal(36, minMax.DefWidth);
		Assert.Equal(18, minMax.DefHeight);

		var dimensions = MuiAreaLayoutCore.TextDimensions(ref platform, State, obj,
			text, 4);
		Assert.Equal((uint)18, dimensions >> 16);
		Assert.Equal((uint)36, dimensions & 0xFFFFu);
		Assert.True(MuiAreaLayoutCore.DrawText(ref platform, State, obj, 2, 4,
			100, 30, text, 4));
		Assert.Equal(runtime.Font.Raw, platform.LastTextFont.Raw);
		Assert.Equal(28, platform.LastTextBaseline);
		Assert.True(MuiAreaLayoutCore.Cleanup(ref platform, State, obj));
		Assert.False(platform.TryGetMuiCustomFontMetrics(runtime.Font, out _));
	}

	[Fact]
	public void CustomFontTextColorOverrideReachesGetterAndDrawing()
	{
		var platform = CreatePlatform(out var areaClass);
		var spec = APTR.FromPointer(0x3200);
		var text = APTR.FromPointer(0x3300);
		platform.WriteCString(spec, "/c112233");
		platform.WriteCString(text, "MUI");
		platform.MuiTextColorValue = 0x00FFFFFFu;
		var obj = MuiCommonControlCore.CreateControl(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiAreaCustomFontPacketCore.Set(ref platform, State, obj,
			spec, false));
		var renderInfo = APTR.FromPointer(0x3400);
		var rastPort = APTR.FromPointer(0x3500);
		platform.WriteUInt32(renderInfo, 20, rastPort.Raw);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, obj, renderInfo));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiCommonControlCore.TextColor, out var color));
		Assert.Equal(0x00112233u, color);
		Assert.True(MuiAreaLayoutCore.DrawText(ref platform, State, obj, 2, 3,
			40, 12, text, 3));
		Assert.Equal(0x00112233u, platform.LastAppliedMuiTextColor);
		Assert.True(MuiAreaLayoutCore.Cleanup(ref platform, State, obj));
	}

	[Fact]
	public void CustomFontStyleAndOutlineReachTheNamedRenderRequest()
	{
		var platform = CreatePlatform(out var areaClass);
		var spec = APTR.FromPointer(0x3600);
		var text = APTR.FromPointer(0x3700);
		platform.WriteCString(spec, "Noto Sans UI/o/C445566");
		platform.WriteCString(text, "MUI");
		var obj = MuiCommonControlCore.CreateControl(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiAreaCustomFontPacketCore.Set(ref platform, State, obj,
			spec, false));
		var renderInfo = APTR.FromPointer(0x3800);
		var rastPort = APTR.FromPointer(0x3900);
		platform.WriteUInt32(renderInfo, 20, rastPort.Raw);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, obj, renderInfo));
		Assert.True(MuiAreaLayoutCore.DrawText(ref platform, State, obj, 1, 2,
			40, 12, text, 3));
		Assert.Equal(1u, platform.MuiCustomFontRenderCount);
		Assert.Equal(obj.Raw, platform.LastMuiCustomFontRender.Object.Raw);
		Assert.Equal(rastPort.Raw, platform.LastMuiCustomFontRender.RastPort.Raw);
		Assert.Equal(platform.LastCustomFontHandle.Raw,
			platform.LastMuiCustomFontRender.Font.Raw);
		Assert.Equal(MuiCustomFontSpecFlags.Outline,
			platform.LastMuiCustomFontRender.Spec.StyleFlags);
		Assert.Equal(MuiCustomFontSpecFlags.HasOutlineColor,
			platform.LastMuiCustomFontRender.Spec.ValueFlags &
			MuiCustomFontSpecFlags.HasOutlineColor);
		Assert.Equal(0x00445566u,
			platform.LastMuiCustomFontRender.Spec.OutlineColor);
		Assert.True(MuiAreaLayoutCore.Cleanup(ref platform, State, obj));
	}

	[Fact]
	public void CustomFontRenderDoesNotDependOnTextColorAvailability()
	{
		var platform = CreatePlatform(out var areaClass);
		var spec = APTR.FromPointer(0x3A00);
		var text = APTR.FromPointer(0x3B00);
		platform.WriteCString(spec, "Noto Sans UI/b");
		platform.WriteCString(text, "MUI");
		platform.MuiTextColorAvailable = false;
		var obj = MuiCommonControlCore.CreateControl(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiAreaCustomFontPacketCore.Set(ref platform, State, obj,
			spec, false));
		var renderInfo = APTR.FromPointer(0x3C00);
		var rastPort = APTR.FromPointer(0x3D00);
		platform.WriteUInt32(renderInfo, 20, rastPort.Raw);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, obj, renderInfo));
		Assert.Equal(0u, platform.MuiCustomFontRenderCount);
		Assert.True(MuiAreaLayoutCore.DrawText(ref platform, State, obj, 1, 2,
			40, 12, text, 3));
		Assert.Equal(1u, platform.MuiCustomFontRenderCount);
		Assert.Equal(rastPort.Raw, platform.LastMuiCustomFontRender.RastPort.Raw);
		Assert.Equal(MuiCustomFontSpecFlags.Bold,
			platform.LastMuiCustomFontRender.Spec.StyleFlags);
		Assert.Equal(0u, platform.MuiTextColorApplyCount);
		Assert.True(MuiAreaLayoutCore.Cleanup(ref platform, State, obj));
	}

	[Fact]
	public void CustomFontSetterRefreshesAnActiveRuntime()
	{
		var platform = CreatePlatform(out var areaClass);
		var first = APTR.FromPointer(0x2200);
		var second = APTR.FromPointer(0x2300);
		platform.WriteCString(first, "/+10");
		platform.WriteCString(second, "/+12/b");
		var obj = MuiCommonControlCore.CreateControl(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiAreaCustomFontPacketCore.Set(ref platform, State, obj,
			first, false));
		var renderInfo = APTR.FromPointer(0x2400);
		platform.WriteUInt32(renderInfo, 20, 0x2500);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, obj, renderInfo));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			obj, MuiCommonControlCore.Font, 0x2900, false));
		Assert.Equal(1u, platform.CustomFontOpenCount);
		Assert.Equal(1u, platform.CustomFontCloseCount);
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, obj,
			0x7FFF0002u, out var setupState));
		Assert.Equal(1u, setupState);
		Assert.True(MuiAreaCustomFontPacketCore.Set(ref platform, State, obj,
			second, false));
		Assert.Equal(2u, platform.CustomFontOpenCount);
		Assert.Equal(1u, platform.CustomFontCloseCount);
		Assert.Equal(second.Raw, platform.LastCustomFontSpec.Source.Raw);
		Assert.True(MuiAreaCustomFontPacketCore.CloseCustomFont(ref platform,
			State, obj, platform.LastCustomFontHandle));
		Assert.Equal(2u, platform.CustomFontCloseCount);
	}

	[Fact]
	public void CustomFontRuntimeRecordUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x2600);
		var expected = new MuiAreaCustomFontRuntimeRecord
		{
			Magic = MuiAreaCustomFontRuntimeRecord.Cookie,
			Font = APTR.FromPointer(0x2700),
			Spec = APTR.FromPointer(0x2800),
			Generation = 3,
			Active = 1,
		};
		Assert.True(MuiAreaCustomFontRuntimeRecordCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiAreaCustomFontRuntimeRecordCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Font.Raw, actual.Font.Raw);
		Assert.Equal(expected.Spec.Raw, actual.Spec.Raw);
		Assert.Equal(expected.Generation, actual.Generation);
		var cursor = new MuiAreaCustomFontRuntimeFieldCursor
		{
			Record = address,
			Field = MuiAreaCustomFontRuntimeField.Active,
		};
		Assert.True(MuiAreaCustomFontRuntimeFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 16, fieldAddress.Raw);
	}

	[Fact]
	public void CustomFontOpenAndCloseMethodsUseNamedPackets()
	{
		var platform = CreatePlatform(out var areaClass);
		var spec = APTR.FromPointer(0x2A00);
		platform.WriteCString(spec, "/+10/o");
		var obj = MuiCommonControlCore.CreateControl(ref platform, State,
			areaClass, APTR.Null);
		var message = APTR.FromPointer(0x2B00);
		Assert.True(MuiAreaCustomFontMessageCodec.WriteOpen(ref platform, message,
			spec));
		var opened = MuiAreaCustomFontMessageCore.Dispatch(ref platform, State,
			obj, message);
		Assert.NotEqual(0u, opened);
		Assert.Equal(1u, platform.CustomFontOpenCount);
		Assert.Equal(spec.Raw, platform.LastCustomFontSpec.Source.Raw);

		Assert.True(MuiAreaCustomFontMessageCodec.WriteClose(ref platform,
			message, APTR.FromPointer(opened)));
		Assert.Equal(0u, MuiAreaCustomFontMessageCore.Dispatch(ref platform,
			State, obj, message));
		Assert.Equal(1u, platform.CustomFontCloseCount);
		Assert.Equal(opened, platform.LastClosedCustomFont.Raw);
		Assert.False(MuiAreaCustomFontPacketCore.TryGetRuntime(ref platform, State,
			obj, out _));
	}

	[Fact]
	public void CustomFontMethodCodecKeepsPacketFieldsNamed()
	{
		var platform = CreatePlatform(out _);
		var message = APTR.FromPointer(0x2C00);
		var spec = APTR.FromPointer(0x2D00);
		Assert.True(MuiAreaCustomFontMessageCodec.WriteOpen(ref platform, message,
			spec));
		Assert.True(MuiAreaCustomFontMessageCodec.TryReadOpen(ref platform,
			message, out var open));
		Assert.Equal(MuiAreaCustomFontMessageCodec.OpenCustomFont,
			open.MethodId);
		Assert.Equal(spec.Raw, open.Spec.Raw);
		var cursor = new MuiAreaCustomFontMessageFieldCursor
		{
			Message = message,
			Kind = MuiAreaCustomFontMessageKind.Open,
			Field = MuiAreaCustomFontMessageField.Pointer,
		};
		Assert.True(MuiAreaCustomFontMessageFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(message.Raw + 4, fieldAddress.Raw);
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
