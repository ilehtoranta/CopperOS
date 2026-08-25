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
	// Keep the String.mui metrics record in its own dataspace-key namespace.
	// 0x7F070037 is already owned by CommonControlCore's Text Unicode record;
	// sharing a key would make a valid String record appear malformed whenever
	// both state seams are materialised on the same object.
	internal const uint MetricsStateKey = 0x7F070070u;

	public static bool IsScrollAttribute(uint attribute) =>
		attribute == ScrollHeight || attribute == ScrollLeft ||
		attribute == ScrollTop || attribute == ScrollVisibleHeight ||
		attribute == ScrollVisibleWidth || attribute == ScrollWidth;

	public static bool Normalize<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryReadMetricsState(ref platform, state, obj, out _);
	}

	// Recompute and optionally publish the String.mui scroll metric transition.
	// The previous and current values are named guest-record fields; no managed
	// shadow tuple or object-layout offset is used. Getter paths continue to use
	// TryReadMetricsState directly, so reading an attribute never raises a
	// notification. Layout/content mutation paths call this seam with notify
	// enabled to drive MorphOS-style Prop connections.
	internal static bool Refresh<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, bool notify)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (MuiCommonControlCore.Classify(ref platform, state, obj) !=
			MuiControlClass.String) return false;
		var hasPrevious = TryGetMetricsStateRecord(ref platform, state, obj,
			out var previous);
		if (!TryReadMetricsState(ref platform, state, obj, out var current))
			return false;
		if (!notify || !hasPrevious) return true;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		if (previous.Width != current.Width)
			MuiNotifyCore.DispatchAttributeChange(ref platform, state, record,
				ScrollWidth, current.Width);
		if (previous.Height != current.Height)
			MuiNotifyCore.DispatchAttributeChange(ref platform, state, record,
				ScrollHeight, current.Height);
		if (previous.VisibleWidth != current.VisibleWidth)
			MuiNotifyCore.DispatchAttributeChange(ref platform, state, record,
				ScrollVisibleWidth, current.VisibleWidth);
		if (previous.VisibleHeight != current.VisibleHeight)
			MuiNotifyCore.DispatchAttributeChange(ref platform, state, record,
				ScrollVisibleHeight, current.VisibleHeight);
		if (previous.Left != current.Left)
			MuiNotifyCore.DispatchAttributeChange(ref platform, state, record,
				ScrollLeft, current.Left);
		if (previous.Top != current.Top)
			MuiNotifyCore.DispatchAttributeChange(ref platform, state, record,
				ScrollTop, current.Top);
		return true;
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
		// A malformed present record is not the same as an absent record.  Admit
		// the named struct before clamping raw projections so a failed typed
		// transition cannot silently mutate ScrollLeft/ScrollTop.
		if (!TryReadMetricsAdmission(ref platform, state, obj, out _,
			out _)) return false;
		Metrics(ref platform, state, obj, out var contentWidth,
			out var contentHeight, out var visibleWidth, out var visibleHeight);
		var maxLeft = contentWidth > visibleWidth ? contentWidth - visibleWidth : 0u;
		var maxTop = contentHeight > visibleHeight ? contentHeight - visibleHeight : 0u;
		var left = ReadRaw(ref platform, state, obj, ScrollLeft, 0);
		var top = ReadRaw(ref platform, state, obj, ScrollTop, 0);
		var previousLeft = left;
		var previousTop = top;
		if (left > maxLeft) left = maxLeft;
		if (top > maxTop) top = maxTop;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ScrollLeft, left, false)) return false;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ScrollTop, top, false))
		{
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				ScrollLeft, previousLeft, false);
			return false;
		}
		result.Width = contentWidth;
		result.Height = contentHeight;
		result.VisibleWidth = visibleWidth;
		result.VisibleHeight = visibleHeight;
		result.Left = left;
		result.Top = top;
		if (PublishMetricsState(ref platform, state, obj, result)) return true;
		// Keep the public scalar projections atomic with the named metrics
		// publication.  This is a value-type rollback; no managed shadow state
		// or positional object field is involved.
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, ScrollLeft,
			previousLeft, false);
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, ScrollTop,
			previousTop, false);
		return false;
	}

	internal static bool TryGetMetricsStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiStringScrollMetricsStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryReadMetricsAdmission(ref platform, state, obj, out value,
			out var present) && present;
	}

	// Shared admission keeps the absent-record construction path distinct from
	// malformed present state.  Only the former may be materialised by the
	// publisher; every consumer fails closed for the latter.
	private static bool TryReadMetricsAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiStringScrollMetricsStateRecord value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			MetricsStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			MetricsStateKey);
		present = block.IsNotNull || length != 0;
		if (!present) return true;
		if (block.IsNull || length != unchecked((int)MuiStringScrollMetricsStateRecord.Size))
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
