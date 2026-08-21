/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS 3.20 String.mui scroll metrics.  This additive core is deliberately
// separate from CommonControlCore so existing MG07/MG09 closures do not acquire
// the metric implementation unless a caller reaches this attribute family.
public static class MuiStringScrollAttributeCore
{
	public const uint StringContents = 0x80428FFDu;
	public const uint ScrollHeight = 0x8042BE8Bu;
	public const uint ScrollLeft = 0x8042BD0Du;
	public const uint ScrollTop = 0x8042F4E5u;
	public const uint ScrollVisibleHeight = 0x8042791Eu;
	public const uint ScrollVisibleWidth = 0x8042D280u;
	public const uint ScrollWidth = 0x80420FB5u;

	private const uint Width = 0x8042B59Cu;
	private const uint Height = 0x80423237u;
	private const uint CharacterWidth = 8;
	private const uint CharacterHeight = 10;
	private const uint MetricsStateKey = 0x7F070037u;

	public static bool IsScrollAttribute(uint attribute) =>
		attribute == ScrollHeight || attribute == ScrollLeft ||
		attribute == ScrollTop || attribute == ScrollVisibleHeight ||
		attribute == ScrollVisibleWidth || attribute == ScrollWidth;

	public static bool Normalize<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryReadMetricsState(ref platform, state, obj, out _);
	}

	public static bool Set<TPlatform>(ref TPlatform platform, APTR state, APTR obj,
		uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (MuiCommonControlCore.Classify(ref platform, state, obj) !=
			MuiControlClass.String || (attribute != ScrollLeft &&
			attribute != ScrollTop)) return false;
		if (!TryReadMetricsState(ref platform, state, obj, out var metrics))
			return false;
		var limit = attribute == ScrollLeft ?
			(metrics.Width > metrics.VisibleWidth ?
				metrics.Width - metrics.VisibleWidth : 0u) :
			(metrics.Height > metrics.VisibleHeight ?
				metrics.Height - metrics.VisibleHeight : 0u);
		var target = value > limit ? limit : value;
		var current = attribute == ScrollLeft ? metrics.Left : metrics.Top;
		if (current == target) return true;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			target, notify)) return false;
		if (!TryReadMetricsState(ref platform, state, obj, out _)) return false;
		return platform.ScheduleRedraw(obj, 2);
	}

	public static bool Get<TPlatform>(ref TPlatform platform, APTR state, APTR obj,
		uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (MuiCommonControlCore.Classify(ref platform, state, obj) ==
			MuiControlClass.String && IsScrollAttribute(attribute))
		{
			if (!TryReadMetricsState(ref platform, state, obj, out var metrics))
				return false;
			value = attribute == ScrollWidth ? metrics.Width :
				attribute == ScrollHeight ? metrics.Height :
				attribute == ScrollVisibleWidth ? metrics.VisibleWidth :
				attribute == ScrollVisibleHeight ? metrics.VisibleHeight :
				attribute == ScrollLeft ? metrics.Left : metrics.Top;
			return true;
		}
		return MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			attribute, out value);
	}

	internal static bool TryReadMetricsState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringScrollMetricsState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (MuiCommonControlCore.Classify(ref platform, state, obj) !=
			MuiControlClass.String) return false;
		Metrics(ref platform, state, obj, out var contentWidth,
			out var contentHeight, out var visibleWidth, out var visibleHeight);
		var maxLeft = contentWidth > visibleWidth ? contentWidth - visibleWidth : 0u;
		var maxTop = contentHeight > visibleHeight ? contentHeight - visibleHeight : 0u;
		var left = ReadRaw(ref platform, state, obj, ScrollLeft, 0);
		var top = ReadRaw(ref platform, state, obj, ScrollTop, 0);
		if (left > maxLeft) left = maxLeft;
		if (top > maxTop) top = maxTop;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ScrollLeft, left, false) || !MuiHeadlessObjectCore.SetAttribute(
			ref platform, state, obj, ScrollTop, top, false)) return false;
		result.Width = contentWidth;
		result.Height = contentHeight;
		result.VisibleWidth = visibleWidth;
		result.VisibleHeight = visibleHeight;
		result.Left = left;
		result.Top = top;
		return PublishMetricsState(ref platform, state, obj, result);
	}

	internal static bool TryGetMetricsStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiStringScrollMetricsStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			MetricsStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			MetricsStateKey) != unchecked((int)MuiStringScrollMetricsStateRecord.Size))
			return false;
		return MuiStringScrollMetricsStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool PublishMetricsState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringScrollMetricsState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			MetricsStateKey);
		if (block.IsNull)
		{
			var scratch = MuiHeadlessMemory.Allocate(ref platform,
				MuiStringScrollMetricsStateRecord.Size);
			if (scratch.IsNull) return false;
			platform.Clear(scratch, MuiStringScrollMetricsStateRecord.Size);
			var initial = default(MuiStringScrollMetricsStateRecord);
			initial.Magic = MuiStringScrollMetricsStateRecord.Cookie;
			if (!MuiStringScrollMetricsStateRecordCodec.Write(ref platform, scratch,
				initial) || !MuiStoreCore.DataspaceAdd(ref platform, state, obj,
				MetricsStateKey, scratch,
				unchecked((int)MuiStringScrollMetricsStateRecord.Size)))
			{
				platform.Clear(scratch, MuiStringScrollMetricsStateRecord.Size);
				platform.Free(scratch, MuiStringScrollMetricsStateRecord.Size);
				return false;
			}
			platform.Clear(scratch, MuiStringScrollMetricsStateRecord.Size);
			platform.Free(scratch, MuiStringScrollMetricsStateRecord.Size);
			block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				MetricsStateKey);
		}
		var stored = default(MuiStringScrollMetricsStateRecord);
		stored.Magic = MuiStringScrollMetricsStateRecord.Cookie;
		stored.Width = value.Width;
		stored.Height = value.Height;
		stored.VisibleWidth = value.VisibleWidth;
		stored.VisibleHeight = value.VisibleHeight;
		stored.Left = value.Left;
		stored.Top = value.Top;
		return MuiStringScrollMetricsStateRecordCodec.Write(ref platform, block,
			stored);
	}

	private static void Metrics<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out uint contentWidth, out uint contentHeight,
		out uint visibleWidth, out uint visibleHeight)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var contents = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			StringContents, 0));
		if (!MuiStringscrollCore.TryMeasureUtf8(ref platform, contents,
			out var maxColumns, out var lines))
		{
			contentWidth = 0;
			contentHeight = 0;
		}
		else
		{
			contentWidth = Saturate(maxColumns * CharacterWidth);
			contentHeight = Saturate(lines * CharacterHeight);
		}
		if (MuiAreaLayoutCore.TryReadGeometryState(ref platform, state, obj,
			out var geometry))
		{
			visibleWidth = geometry.Width <= 0 ? 0u :
				unchecked((uint)geometry.Width);
			visibleHeight = geometry.Height <= 0 ? 0u :
				unchecked((uint)geometry.Height);
		}
		else
		{
			visibleWidth = ReadRaw(ref platform, state, obj, Width, 0);
			visibleHeight = ReadRaw(ref platform, state, obj, Height, 0);
		}
	}

	private static uint Saturate(uint value) =>
		value > 0x7FFFFFFFu ? 0x7FFFFFFFu : value;

	private static uint ReadRaw<TPlatform>(ref TPlatform platform, APTR state, APTR obj,
		uint attribute, uint fallback) where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, attribute,
			out var value) ? value : fallback;
}
