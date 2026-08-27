/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

internal struct MuiMinMaxValues
{
	internal const uint Size = 12;
	public short MinWidth;
	public short MinHeight;
	public short MaxWidth;
	public short MaxHeight;
	public short DefWidth;
	public short DefHeight;
}

// Content geometry after applying the caller-selected Area inner spacing.
// Keeping this as a named value record lets every common control consume the
// same bounded rectangle without exposing private object offsets.
internal struct MuiAreaContentRect
{
	internal int Left;
	internal int Top;
	internal int Width;
	internal int Height;
}

internal enum MuiMinMaxField : byte
{
	MinWidth,
	MinHeight,
	MaxWidth,
	MaxHeight,
	DefWidth,
	DefHeight,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMinMaxFieldCursor
{
	internal APTR Record;
	internal MuiMinMaxField Field;
}

internal static class MuiMinMaxFieldCursorCodec
{
	private static bool TryResolve(MuiMinMaxField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiMinMaxField.MinWidth:
				offset = 0;
				break;
			case MuiMinMaxField.MinHeight:
				offset = 2;
				break;
			case MuiMinMaxField.MaxWidth:
				offset = 4;
				break;
			case MuiMinMaxField.MaxHeight:
				offset = 6;
				break;
			case MuiMinMaxField.DefWidth:
				offset = 8;
				break;
			case MuiMinMaxField.DefHeight:
				offset = 10;
				break;
			default:
				offset = 0;
				return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMinMaxFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 2);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiMinMaxField field, out short value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiMinMaxFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = unchecked((short)platform.ReadUInt16(address, 0));
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiMinMaxField field, short value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiMinMaxFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt16(address, 0, unchecked((ushort)value));
		return true;
	}
}

internal static class MuiMinMaxRecordCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiMinMaxValues values)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiMinMaxValues.Size)) return false;
		return MuiMinMaxFieldCursorCodec.TryWrite(ref platform, address,
			MuiMinMaxField.MinWidth, values.MinWidth) &&
			MuiMinMaxFieldCursorCodec.TryWrite(ref platform, address,
				MuiMinMaxField.MinHeight, values.MinHeight) &&
			MuiMinMaxFieldCursorCodec.TryWrite(ref platform, address,
				MuiMinMaxField.MaxWidth, values.MaxWidth) &&
			MuiMinMaxFieldCursorCodec.TryWrite(ref platform, address,
				MuiMinMaxField.MaxHeight, values.MaxHeight) &&
			MuiMinMaxFieldCursorCodec.TryWrite(ref platform, address,
				MuiMinMaxField.DefWidth, values.DefWidth) &&
			MuiMinMaxFieldCursorCodec.TryWrite(ref platform, address,
				MuiMinMaxField.DefHeight, values.DefHeight);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiMinMaxValues values)
		where TPlatform : struct, IMuiGuestMemory
	{
		values = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiMinMaxValues.Size)) return false;
		return MuiMinMaxFieldCursorCodec.TryRead(ref platform, address,
			MuiMinMaxField.MinWidth, out values.MinWidth) &&
			MuiMinMaxFieldCursorCodec.TryRead(ref platform, address,
				MuiMinMaxField.MinHeight, out values.MinHeight) &&
			MuiMinMaxFieldCursorCodec.TryRead(ref platform, address,
				MuiMinMaxField.MaxWidth, out values.MaxWidth) &&
			MuiMinMaxFieldCursorCodec.TryRead(ref platform, address,
				MuiMinMaxField.MaxHeight, out values.MaxHeight) &&
			MuiMinMaxFieldCursorCodec.TryRead(ref platform, address,
				MuiMinMaxField.DefWidth, out values.DefWidth) &&
			MuiMinMaxFieldCursorCodec.TryRead(ref platform, address,
				MuiMinMaxField.DefHeight, out values.DefHeight);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiMinMaxRecordInput
{
	public short MinWidth;
	public short MinHeight;
	public short MaxWidth;
	public short MaxHeight;
	public short DefWidth;
	public short DefHeight;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiAreaLayoutRenderInfoInput
{
	public APTR WindowObject;
	public APTR Screen;
	public APTR DrawInfo;
	public APTR Pens;
	public APTR Window;
	public APTR RastPort;
	public uint Flags;
}

public static class MuiAreaLayoutRecordPacketCore
{
	public static bool WriteMinMax<TPlatform>(ref TPlatform platform, APTR address,
		MuiMinMaxRecordInput input)
		where TPlatform : struct, IMuiGuestMemory
	{
		var values = default(MuiMinMaxValues);
		values.MinWidth = input.MinWidth;
		values.MinHeight = input.MinHeight;
		values.MaxWidth = input.MaxWidth;
		values.MaxHeight = input.MaxHeight;
		values.DefWidth = input.DefWidth;
		values.DefHeight = input.DefHeight;
		return MuiMinMaxRecordCodec.Write(ref platform, address, values);
	}

	public static uint DispatchMinMax<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiMinMaxRecordCodec.TryRead(ref platform, address,
			out var values)) return 0;
		return unchecked((uint)(ushort)values.MinWidth) ^
			unchecked((uint)(ushort)values.MinHeight) ^
			unchecked((uint)(ushort)values.MaxWidth) ^
			unchecked((uint)(ushort)values.MaxHeight) ^
			unchecked((uint)(ushort)values.DefWidth) ^
			unchecked((uint)(ushort)values.DefHeight);
	}

	public static bool WriteRenderInfo<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaLayoutRenderInfoInput input)
		where TPlatform : struct, IMuiGuestMemory
	{
		var record = default(MuiDrawingRenderInfoRecord);
		record.WindowObject = input.WindowObject;
		record.Screen = input.Screen;
		record.DrawInfo = input.DrawInfo;
		record.Pens = input.Pens;
		record.Window = input.Window;
		record.RastPort = input.RastPort;
		record.Flags = input.Flags;
		return MuiDrawingRenderInfoCodec.Write(ref platform, address, record);
	}

	public static uint DispatchRenderInfo<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiDrawingRenderInfoCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.WindowObject.Raw ^ record.Screen.Raw ^ record.DrawInfo.Raw ^
			record.Pens.Raw ^ record.Window.Raw ^ record.RastPort.Raw ^
			record.Flags;
	}
}

public static class MuiAreaLayoutCore
{
	private const uint Maximum = 10000;
	private const uint Weight = 0x80421D1F;
	private const uint HorizWeight = 0x80426DB9;
	private const uint VertWeight = 0x804298D0;
	private const uint Width = 0x8042B59C;
	private const uint Height = 0x80423237;
	private const uint MaxWidth = 0x8042F112;
	private const uint MaxHeight = 0x804293E4;
	private const uint FixWidth = 0x8042A3F1;
	private const uint FixHeight = 0x8042A92B;
	private const uint InnerLeft = 0x804228F8;
	private const uint InnerRight = 0x804297FF;
	private const uint InnerTop = 0x80421EB6;
	private const uint InnerBottom = 0x8042F2C0;
	private const uint ShowMe = 0x80429BA8;
	private const uint LeftEdge = 0x8042BEC6;
	private const uint TopEdge = 0x8042509B;
	private const uint RightEdge = 0x8042BA82;
	private const uint BottomEdge = 0x8042E552;
	private const uint Frame = 0x8042AC64;
	private const uint FrameDynamic = 0x804223C9;
	private const uint FrameVisible = 0x80426498;
	private const uint FramePhantomHoriz = 0x8042ED76;
	private const uint FrameTitle = 0x8042D1C7;
	private const uint Background = 0x8042545B;
	private const uint FillArea = 0x804294A3;
	private const uint Font = 0x8042BE50;
	private const uint RenderInfo = 0x7FFF0001;
	private const uint IsSetup = 0x7FFF0002;
	private const uint IsShown = 0x7FFF0003;
	private const uint AreaGeometryStateKey = 0x7F070035;
	internal const uint AreaLayoutPolicyStateKey = 0x7F070073;
	private const uint AreaRenderPolicyStateKey = 0x7F070037;

	public static bool Setup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR renderInfo) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (renderInfo.IsNull || !platform.IsMapped(renderInfo, 28)) return false;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			RenderInfo, renderInfo.Raw, false)) return false;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			IsSetup, 1, false)) return false;
		if (!MuiAreaTextColorCore.Setup(ref platform, state, obj, renderInfo))
			return false;
		if (MuiAreaCustomFontCore.Setup(ref platform, state, obj)) return true;
		MuiAreaTextColorCore.Cleanup(ref platform, state, obj);
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, IsSetup, 0,
			false);
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, RenderInfo, 0,
			false);
		return false;
	}

	public static bool Cleanup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiLayoutPlatform
	{
		var resize = MuiAreaResizeCore.Cleanup(ref platform, state, obj);
		var custom = MuiAreaCustomFontCore.CloseRuntime(ref platform, state, obj);
		var colors = MuiAreaTextColorCore.Cleanup(ref platform, state, obj);
		var shown = MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			IsShown, 0, false);
		var setup = MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			IsSetup, 0, false);
		var render = MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			RenderInfo, 0, false);
		return resize && custom && colors && shown && setup && render;
	}

	public static bool Show<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!Attribute(ref platform, state, obj, IsSetup, 0, out var setup) ||
			setup == 0) return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			IsShown, 1, false);
	}

	public static bool Hide<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiLayoutPlatform =>
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, IsShown, 0,
			false);

	public static bool AskMinMax<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR storage) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!platform.IsMapped(storage, 12)) return false;
		var values = ComputeMinMax(ref platform, state, obj);
		return WriteMinMax(ref platform, storage, values);
	}

	internal static bool WriteMinMax<TPlatform>(ref TPlatform platform,
		APTR storage, MuiMinMaxValues values)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiMinMaxRecordCodec.Write(ref platform, storage, values);

	public static bool Layout<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int left, int top, int width, int height)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (width < 0 || height < 0 || left > int.MaxValue - width ||
			top > int.MaxValue - height) return false;
		var geometry = default(MuiAreaGeometryState);
		geometry.Left = left;
		geometry.Top = top;
		geometry.Width = width;
		geometry.Height = height;
		if (!MuiAreaGeometryStateValidation.TryExpectedEdge(left, width,
			out geometry.Right) || !MuiAreaGeometryStateValidation.TryExpectedEdge(top,
				height, out geometry.Bottom)) return false;
		var record = default(MuiAreaGeometryStateRecord);
		record.Magic = MuiAreaGeometryStateRecord.Cookie;
		record.Left = geometry.Left;
		record.Top = geometry.Top;
		record.Width = geometry.Width;
		record.Height = geometry.Height;
		record.Right = geometry.Right;
		record.Bottom = geometry.Bottom;
		if (!MuiAreaGeometryStateValidation.IsValidRecord(record)) return false;
		return PublishGeometryState(ref platform, state, obj, geometry);
	}

	public static bool Draw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint flags) where TPlatform : struct, IMuiLayoutPlatform
	{
		// MUIA_ShowMe controls whether the object participates in display.  Keep
		// the visibility decision on the named layout-policy record so a hidden
		// Area is a successful no-op and cannot touch the render port.
		if (!TryReadLayoutPolicyState(ref platform, state, obj,
			out var layoutPolicy)) return false;
		if (layoutPolicy.ShowMe == 0) return true;
		// Group disappearance lays a child out as a zero-area rectangle without
		// mutating its public MUIA_ShowMe state. Treat that geometry as a
		// successful render no-op before touching RenderInfo or the graphics port.
		if (!TryReadGeometryState(ref platform, state, obj,
			out var geometry)) return false;
		if (geometry.Width <= 0 || geometry.Height <= 0) return true;
		if (!TryReadRenderPolicyState(ref platform, state, obj,
			out var renderPolicy)) return false;
		if (!Attribute(ref platform, state, obj, RenderInfo, 0,
			out var renderInfoRaw) || renderInfoRaw == 0) return false;
		var renderInfo = APTR.FromPointer(renderInfoRaw);
		if (!MuiDrawingRenderInfoCodec.TryRead(ref platform, renderInfo,
			out var renderInfoRecord)) return false;
		var rastPort = renderInfoRecord.RastPort;
		if (rastPort.IsNull) return false;
		var left = geometry.Left;
		var top = geometry.Top;
		var width = geometry.Width;
		var height = geometry.Height;
		var doubleBuffer = default(MuiAreaDoubleBufferLease);
		if (!MuiAreaDoubleBufferCore.Begin(ref platform, state, obj, renderInfo,
			rastPort, left, top, width, height, flags, out doubleBuffer)) return false;
		if (doubleBuffer.Active != 0)
		{
			rastPort = doubleBuffer.Request.TargetRastPort;
			left = doubleBuffer.Request.TargetLeft;
			top = doubleBuffer.Request.TargetTop;
			width = doubleBuffer.Request.TargetWidth;
			height = doubleBuffer.Request.TargetHeight;
		}
		MuiAreaTextColorCore.Apply(ref platform, state, obj, rastPort);
		// MUIM_Text is also the documented drawing path for an opened
		// CustomFont. Keep this provider request explicit and independent from
		// the optional TextColor capability.
		MuiCustomFontRenderCore.Apply(ref platform, state, obj, rastPort);
		if (!platform.LockLayer(rastPort))
		{
			MuiAreaDoubleBufferCore.End(ref platform, state, obj,
				ref doubleBuffer, false);
			return false;
		}
		if (!platform.BeginUpdate(rastPort))
		{
			platform.UnlockLayer(rastPort);
			MuiAreaDoubleBufferCore.End(ref platform, state, obj,
				ref doubleBuffer, false);
			return false;
		}
		var clip = platform.PushClip(rastPort, left, top, width, height);
		if (renderPolicy.FillArea != 0)
		{
			platform.SetPen(rastPort, renderPolicy.Background);
			platform.FillRectangle(rastPort, left, top, left + width - 1,
				top + height - 1);
		}
		if (renderPolicy.Frame != 0 && renderPolicy.FrameVisible != 0)
		{
			platform.SetPen(rastPort, 4);
			if (renderPolicy.FramePhantomHoriz == 0)
				platform.DrawLine(rastPort, left, top, left + width - 1, top);
			platform.DrawLine(rastPort, left, top, left, top + height - 1);
			if (renderPolicy.FramePhantomHoriz == 0)
				platform.DrawLine(rastPort, left, top + height - 1,
					left + width - 1, top + height - 1);
			platform.DrawLine(rastPort, left + width - 1, top,
				left + width - 1, top + height - 1);
			DrawFrameTitle(ref platform, rastPort, renderPolicy, left, top, width);
		}
		platform.PopClip(rastPort, clip);
		platform.EndUpdate(rastPort, true);
		platform.UnlockLayer(rastPort);
		return MuiAreaDoubleBufferCore.End(ref platform, state, obj,
			ref doubleBuffer, true);
	}

	private static void DrawFrameTitle<TPlatform>(ref TPlatform platform,
		APTR rastPort, MuiAreaRenderPolicyStateRecord renderPolicy, int left,
		int top, int width) where TPlatform : struct, IMuiLayoutPlatform
	{
		var title = renderPolicy.FrameTitle;
		if (title.IsNull || width <= 0) return;
		var length = CStringLength(ref platform, title);
		if (length <= 0) return;
		var font = APTR.FromPointer(renderPolicy.Font);
		var textWidth = platform.TextWidth(rastPort, font, title, length);
		var textHeight = platform.TextHeight(rastPort, font);
		if (textWidth < 0) textWidth = 0;
		if (textHeight < 1) textHeight = 1;
		var textLeft = left + (width - textWidth) / 2;
		if (textLeft < left) textLeft = left;
		platform.SetPen(rastPort, 4);
		platform.DrawText(rastPort, font, textLeft, top + textHeight, title,
			length);
	}

	private static int CStringLength<TPlatform>(ref TPlatform platform, APTR text)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (text.IsNull) return 0;
		for (var index = 0; index < 4096; index++)
		{
			if (!platform.IsMapped(text, (uint)index + 1)) return index;
			if (platform.ReadUInt8(text, index) == 0) return index;
		}
		return 4096;
	}

	public static bool DrawBackground<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int left, int top, int width, int height)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		return DrawBackground(ref platform, state, obj, left, top, width, height,
			0, 0, 0, 0, MuiBackfillRenderRequest.DrawBackground);
	}

	public static bool DrawBackground<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int left, int top, int width, int height,
		int xOffset, int yOffset, int brightness, uint flags, uint kind)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (width <= 0 || height <= 0 ||
			left > int.MaxValue - (width - 1) ||
			left < int.MinValue + (width - 1) ||
			top > int.MaxValue - (height - 1) ||
			top < int.MinValue + (height - 1) ||
			!TryReadLayoutPolicyState(ref platform, state, obj,
				out _) ||
			!MuiCommonControlCore.TryReadAreaPresentationState(ref platform, state,
				obj, out var presentation) ||
			!TryReadRenderPolicyState(ref platform, state, obj,
				out var renderPolicy)) return false;
		if (!GetRenderPort(ref platform, state, obj, out var renderInfo,
			out var rastPort)) return false;
		var right = left + width - 1;
		var bottom = top + height - 1;
		var request = default(MuiBackfillRenderRequest);
		request.Object = obj;
		request.RenderInfo = renderInfo;
		request.RastPort = rastPort;
		request.Left = left;
		request.Top = top;
		request.Right = right;
		request.Bottom = bottom;
		request.Width = width;
		request.Height = height;
		request.XOffset = xOffset;
		request.YOffset = yOffset;
		request.Brightness = brightness;
		request.Flags = flags;
		request.Kind = kind;
		request.CustomBackfill = presentation.CustomBackfill;
		request.Background = renderPolicy.Background;
		if (platform.ApplyMuiBackfill(ref request))
		{
			if (request.Object != obj || request.RenderInfo != renderInfo ||
				request.RastPort != rastPort || request.Left != left ||
				request.Top != top || request.Right != right ||
				request.Bottom != bottom || request.Width != width ||
				request.Height != height || request.XOffset != xOffset ||
				request.YOffset != yOffset || request.Brightness != brightness ||
				request.Flags != flags || request.Kind != kind) return false;
			return true;
		}
		platform.SetPen(rastPort, renderPolicy.Background);
		platform.FillRectangle(rastPort, left, top, right, bottom);
		return true;
	}

	public static bool DrawImage<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR image, int left, int top, int width, int height)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (image.IsNull || width <= 0 || height <= 0 ||
			!GetRenderPort(ref platform, state, obj, out var rastPort)) return false;
		platform.DrawImage(rastPort, image, left, top, width, height);
		return true;
	}

	public static uint TextDimensions<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR text, int length)
		where TPlatform : struct, IMuiLayoutPlatform
		=> TextDimensions(ref platform, state, obj, text, length,
			APTR.Null, 0);

	public static uint TextDimensions<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR text, int length, APTR preParse, uint flags)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (text.IsNull || length < 0 ||
			!TryReadLayoutPolicyState(ref platform, state, obj,
				out _) ||
			!TryReadRenderPolicyState(ref platform, state, obj,
				out var renderPolicy) ||
			!GetRenderPort(ref platform, state, obj, out var rastPort)) return 0;
		var font = renderPolicy.Font;
		var unicode = default(MuiTextUnicodeState);
		if (!MuiCommonControlCore.TryReadTextUnicodeState(ref platform, state, obj,
			out unicode)) return 0;
		var dimensionRequest = default(MuiTextDimensionRequest);
		dimensionRequest.Object = obj;
		dimensionRequest.RastPort = rastPort;
		dimensionRequest.Font = font;
		dimensionRequest.Text = text;
		dimensionRequest.PreParse = preParse;
		dimensionRequest.Length = length;
		dimensionRequest.Flags = flags;
		dimensionRequest.Unicode = unicode.Unicode == 0 ? 0u : 1u;
		dimensionRequest.Width = -1;
		dimensionRequest.Height = -1;
		dimensionRequest.Present = 1;
		var providerMetrics = platform.ApplyMuiTextDimensions(
			ref dimensionRequest);
		var useProviderMetrics = providerMetrics && dimensionRequest.Width >= 0 &&
			dimensionRequest.Height >= 0;
		var width = useProviderMetrics ? dimensionRequest.Width :
			platform.TextWidth(rastPort, font, text, length);
		var height = useProviderMetrics ? dimensionRequest.Height :
			platform.TextHeight(rastPort, font);
		if (!useProviderMetrics && (dimensionRequest.Unicode != 0 ||
			preParse.IsNotNull))
		{
			var measured = MuiCommonControlCore.TryMeasureTextSpan(ref platform,
				preParse, text, length, dimensionRequest.Unicode != 0,
				out var columns, out var lines);
			if (!measured) return 0;
			if (platform.TryGetMuiCustomFontMetrics(font, out var metrics) &&
				metrics.GlyphWidth > 0)
				width = columns > int.MaxValue / metrics.GlyphWidth ? int.MaxValue :
					columns * metrics.GlyphWidth;
			else
				width = columns > int.MaxValue / 8 ? int.MaxValue : columns * 8;
			var lineHeight = height;
			if (lineHeight > 0 && lines > int.MaxValue / lineHeight)
				height = int.MaxValue;
			else if (lineHeight > 0)
				height = lineHeight * lines;
		}
		if (width < 0) width = 0;
		if (height < 0) height = 0;
		if (width > 65535) width = 65535;
		if (height > 65535) height = 65535;
		return (uint)(height << 16) | (uint)width;
	}

	public static bool RequestRedraw<TPlatform>(ref TPlatform platform, APTR obj,
		uint flags) where TPlatform : struct, IMuiLayoutPlatform =>
		platform.ScheduleRedraw(obj, flags);

	public static bool DrawText<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int left, int top, int width, int height, APTR text, int length)
		where TPlatform : struct, IMuiLayoutPlatform
		=> DrawText(ref platform, state, obj, left, top, width, height, text,
			length, APTR.Null, 0);

	public static bool DrawText<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int left, int top, int width, int height, APTR text, int length,
		APTR preParse, uint flags) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (text.IsNull || length < 0 ||
			!TryReadLayoutPolicyState(ref platform, state, obj,
				out _) ||
			!TryReadRenderPolicyState(ref platform, state, obj,
				out var renderPolicy)) return false;
		if (!GetRenderPort(ref platform, state, obj, out var rastPort)) return false;
		MuiAreaTextColorCore.Apply(ref platform, state, obj, rastPort);
		// CustomFont rendering must not depend on TextColor being available; both
		// provider responsibilities cross named value records independently.
		MuiCustomFontRenderCore.Apply(ref platform, state, obj, rastPort);
		var font = renderPolicy.Font;
		var unicode = default(MuiTextUnicodeState);
		if (!MuiCommonControlCore.TryReadTextUnicodeState(ref platform, state, obj,
			out unicode)) return false;
		var methodRequest = default(MuiTextMethodRenderRequest);
		methodRequest.Object = obj;
		methodRequest.RastPort = rastPort;
		methodRequest.Font = font;
		methodRequest.Text = text;
		methodRequest.PreParse = preParse;
		methodRequest.Left = left;
		methodRequest.Top = top;
		methodRequest.Width = width;
		methodRequest.Height = height;
		methodRequest.Length = length;
		methodRequest.Flags = flags;
		methodRequest.Unicode = unicode.Unicode == 0 ? 0u : 1u;
		methodRequest.Present = 1;
		platform.ApplyMuiTextMethod(ref methodRequest);
		MuiCommonControlCore.ApplyLeadingTextPreParse(ref platform, obj,
			rastPort, font, preParse, left, top, width, height);
		var textHeight = platform.TextHeight(rastPort, font);
		var baseline = top + (height - textHeight) / 2 + textHeight;
		platform.DrawText(rastPort, font, left, baseline, text, length);
		return true;
	}

	internal static MuiMinMaxValues ComputeMinMax<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		MuiMinMaxValues result = default;
		// Layout policy is a named guest record.  A malformed present record must
		// fail closed before common-control or dimension consumers inspect it.
		if (!TryReadLayoutPolicyState(ref platform, state, obj,
			out var policy)) return result;
		if (MuiCommonControlCore.TryComputeMinMax(ref platform, state, obj,
			out var commonValues)) return commonValues;
		var shown = policy.ShowMe;
		if (shown == 0) return result;
		var il = policy.InnerLeft;
		var ir = policy.InnerRight;
		var it = policy.InnerTop;
		var ib = policy.InnerBottom;
		var fw = policy.FixWidth;
		var fh = policy.FixHeight;
		var mw = policy.MaxWidth;
		var mh = policy.MaxHeight;
		var horizontal = Clamp(il + ir, Maximum);
		var vertical = Clamp(it + ib, Maximum);
		var fixedWidth = fw == 0 ? 0u : Clamp(fw + horizontal, Maximum);
		var fixedHeight = fh == 0 ? 0u : Clamp(fh + vertical, Maximum);
		result.MinWidth = ToDimension(fixedWidth == 0 ? horizontal : fixedWidth);
		result.MinHeight = ToDimension(fixedHeight == 0 ? vertical : fixedHeight);
		result.MaxWidth = ToDimension(fixedWidth == 0 ? mw : fixedWidth);
		result.MaxHeight = ToDimension(fixedHeight == 0 ? mh : fixedHeight);
		result.DefWidth = result.MinWidth;
		result.DefHeight = result.MinHeight;
		return result;
	}

	internal static uint HorizontalWeight<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryReadLayoutPolicyState(ref platform, state, obj,
			out var policy) ? policy.HorizontalWeight : 0;
	}

	internal static uint VerticalWeight<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryReadLayoutPolicyState(ref platform, state, obj,
			out var policy) ? policy.VerticalWeight : 0;
	}

	internal static bool TryGetRenderPolicyState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaRenderPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (obj.IsNull || MuiHeadlessObjectCore.FindObject(ref platform, state,
			obj).IsNull) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AreaRenderPolicyStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			AreaRenderPolicyStateKey) !=
			unchecked((int)MuiAreaRenderPolicyStateRecord.Size)) return false;
		return MuiAreaRenderPolicyStateRecordCodec.TryReadStructural(ref platform,
			block, out value) && MuiAreaRenderPolicyStateAdmission.ValidateLive(
			ref platform, state, obj, value);
	}

	// Synchronize the named render-policy record from raw legacy attributes
	// without entering the public getter path. Common-control Get and OM_GET
	// use this boundary for the FillArea projection.
	internal static bool TryReadRenderPolicyState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaRenderPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (obj.IsNull || MuiHeadlessObjectCore.FindObject(ref platform, state,
			obj).IsNull) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AreaRenderPolicyStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			AreaRenderPolicyStateKey);
		var present = block.IsNotNull || length != 0;
		if (present)
		{
			if (length != unchecked((int)MuiAreaRenderPolicyStateRecord.Size) ||
				!MuiAreaRenderPolicyStateRecordCodec.TryReadStructural(ref platform,
					block, out var record) ||
				!MuiAreaRenderPolicyStateAdmission.ValidateLive(ref platform, state,
					obj, record)) return false;
			value = default;
			value.Magic = MuiAreaRenderPolicyStateRecord.Cookie;
			FillRenderPolicy(ref platform, state, obj, ref value);
			if (!MuiAreaRenderPolicyStateAdmission.ValidateLive(ref platform, state,
				obj, value)) return false;
			if (record.FillArea != value.FillArea || record.Background != value.Background ||
				record.Frame != value.Frame || record.Font != value.Font ||
				record.FrameVisible != value.FrameVisible ||
				record.FramePhantomHoriz != value.FramePhantomHoriz ||
				record.FrameTitle != value.FrameTitle ||
				record.FrameDynamic != value.FrameDynamic)
			{
				if (!MuiAreaRenderPolicyStateRecordCodec.Write(ref platform, block,
					value)) return false;
			}
			return true;
		}

		value = default;
		value.Magic = MuiAreaRenderPolicyStateRecord.Cookie;
		FillRenderPolicy(ref platform, state, obj, ref value);
		if (!MuiAreaRenderPolicyStateAdmission.ValidateLive(ref platform, state,
			obj, value)) return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaRenderPolicyStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaRenderPolicyStateRecord.Size);
		var written = MuiAreaRenderPolicyStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			AreaRenderPolicyStateKey, scratch,
			unchecked((int)MuiAreaRenderPolicyStateRecord.Size));
		platform.Clear(scratch, MuiAreaRenderPolicyStateRecord.Size);
		platform.Free(scratch, MuiAreaRenderPolicyStateRecord.Size);
		return added;
	}

	private static void FillRenderPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, ref MuiAreaRenderPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		// Render-policy publication is also used by the common-control getter
		// seam. Read legacy attributes raw-only so the named record cannot recurse
		// through Get/OM_GET.
		ReadRawRenderPolicy(ref platform, state, obj, FillArea, 1,
			out value.FillArea);
		ReadRawRenderPolicy(ref platform, state, obj, Background, 0,
			out value.Background);
		ReadRawRenderPolicy(ref platform, state, obj, Frame, 0, out value.Frame);
		// Font is the one Area render field with parent inheritance. Resolve it
		// from the guest Family topology instead of copying only this object's
		// raw attribute into the policy record.
		value.Font = 0;
		if (MuiCommonControlCore.TryResolveControlFontState(ref platform, state,
			obj, out var resolvedFont) && resolvedFont.Present)
			value.Font = resolvedFont.Font.Raw;
		ReadRawRenderPolicy(ref platform, state, obj, FrameVisible, 1,
			out value.FrameVisible);
		ReadRawRenderPolicy(ref platform, state, obj, FramePhantomHoriz, 0,
			out value.FramePhantomHoriz);
		ReadRawRenderPolicy(ref platform, state, obj, FrameTitle, 0,
			out var frameTitle);
		value.FrameTitle = APTR.FromPointer(frameTitle);
		ReadRawRenderPolicy(ref platform, state, obj, FrameDynamic, 0,
			out value.FrameDynamic);
	}

	private static bool ReadRawRenderPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint defaultValue, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			attribute, out value)) return true;
		value = defaultValue;
		return MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNotNull;
	}

	internal static bool TryGetLayoutPolicyState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (obj.IsNull || MuiHeadlessObjectCore.FindObject(ref platform, state,
			obj).IsNull) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AreaLayoutPolicyStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			AreaLayoutPolicyStateKey) !=
			unchecked((int)MuiAreaLayoutPolicyStateRecord.Size)) return false;
		return MuiAreaLayoutPolicyStateRecordCodec.TryReadStructural(ref platform,
			block, out value) && MuiAreaLayoutPolicyStateAdmission.ValidateLive(
			ref platform, state, obj, value);
	}

	// Synchronize the named policy record from raw legacy attributes without
	// entering the public getter path. Common-control Get and OM_GET use this
	// boundary so layout policy remains a struct-defined guest projection.
	internal static bool TryReadLayoutPolicyState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (obj.IsNull || MuiHeadlessObjectCore.FindObject(ref platform, state,
			obj).IsNull) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AreaLayoutPolicyStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			AreaLayoutPolicyStateKey);
		var present = block.IsNotNull || length != 0;
		if (present)
		{
			if (length != unchecked((int)MuiAreaLayoutPolicyStateRecord.Size) ||
				!MuiAreaLayoutPolicyStateRecordCodec.TryReadStructural(ref platform,
					block, out var record) ||
				!MuiAreaLayoutPolicyStateAdmission.ValidateLive(ref platform, state,
					obj, record)) return false;
			value = default;
			value.Magic = MuiAreaLayoutPolicyStateRecord.Cookie;
			FillLayoutPolicy(ref platform, state, obj, ref value);
			if (!MuiAreaLayoutPolicyStateAdmission.ValidateLive(ref platform, state,
				obj, value)) return false;
			if (record.ShowMe != value.ShowMe || record.FixWidth != value.FixWidth ||
				record.FixHeight != value.FixHeight || record.MaxWidth != value.MaxWidth ||
				record.MaxHeight != value.MaxHeight || record.InnerLeft != value.InnerLeft ||
				record.InnerRight != value.InnerRight || record.InnerTop != value.InnerTop ||
				record.InnerBottom != value.InnerBottom ||
				record.HorizontalWeight != value.HorizontalWeight ||
				record.VerticalWeight != value.VerticalWeight)
			{
				if (!MuiAreaLayoutPolicyStateRecordCodec.Write(ref platform, block,
					value)) return false;
			}
			return true;
		}

		value = default;
		value.Magic = MuiAreaLayoutPolicyStateRecord.Cookie;
		FillLayoutPolicy(ref platform, state, obj, ref value);
		if (!MuiAreaLayoutPolicyStateAdmission.ValidateLive(ref platform, state,
			obj, value)) return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaLayoutPolicyStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaLayoutPolicyStateRecord.Size);
		var written = MuiAreaLayoutPolicyStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			AreaLayoutPolicyStateKey, scratch,
			unchecked((int)MuiAreaLayoutPolicyStateRecord.Size));
		platform.Clear(scratch, MuiAreaLayoutPolicyStateRecord.Size);
		platform.Free(scratch, MuiAreaLayoutPolicyStateRecord.Size);
		return added;
	}

	private static void FillLayoutPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, ref MuiAreaLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		// Layout-policy publication is also used by the common-control getter
		// seam. Read the legacy attributes raw here so publishing the named record
		// cannot recurse back through Get/OM_GET.
		ReadRawPolicy(ref platform, state, obj, ShowMe, 1, out value.ShowMe);
		ReadRawPolicy(ref platform, state, obj, FixWidth, 0, out value.FixWidth);
		ReadRawPolicy(ref platform, state, obj, FixHeight, 0, out value.FixHeight);
		ReadRawPolicy(ref platform, state, obj, MaxWidth, Maximum, out value.MaxWidth);
		ReadRawPolicy(ref platform, state, obj, MaxHeight, Maximum,
			out value.MaxHeight);
		ReadRawPolicy(ref platform, state, obj, InnerLeft, 0, out value.InnerLeft);
		ReadRawPolicy(ref platform, state, obj, InnerRight, 0,
			out value.InnerRight);
		ReadRawPolicy(ref platform, state, obj, InnerTop, 0, out value.InnerTop);
		ReadRawPolicy(ref platform, state, obj, InnerBottom, 0,
			out value.InnerBottom);
		ReadRawPolicy(ref platform, state, obj, HorizWeight, 0,
			out value.HorizontalWeight);
		if (value.HorizontalWeight == 0)
			ReadRawPolicy(ref platform, state, obj, Weight, 100,
				out value.HorizontalWeight);
		ReadRawPolicy(ref platform, state, obj, VertWeight, 0,
			out value.VerticalWeight);
		if (value.VerticalWeight == 0)
			ReadRawPolicy(ref platform, state, obj, Weight, 100,
				out value.VerticalWeight);
	}

	private static bool ReadRawPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint defaultValue, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			attribute, out value)) return true;
		value = defaultValue;
		return MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNotNull;
	}

	private static bool GetRenderPort<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out APTR rastPort)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		return GetRenderPort(ref platform, state, obj, out _, out rastPort);
	}

	private static bool GetRenderPort<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out APTR renderInfo, out APTR rastPort)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		renderInfo = APTR.Null;
		rastPort = APTR.Null;
		if (!Attribute(ref platform, state, obj, RenderInfo, 0, out var raw) ||
			raw == 0) return false;
		renderInfo = APTR.FromPointer(raw);
		if (!MuiDrawingRenderInfoCodec.TryRead(ref platform, renderInfo,
			out var renderInfoRecord)) return false;
		rastPort = renderInfoRecord.RastPort;
		return rastPort.IsNotNull;
	}

	internal static bool TryReadGeometryState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaGeometryState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AreaGeometryStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			AreaGeometryStateKey);
		var present = block.IsNotNull || length != 0;
		MuiAreaGeometryStateRecord record = default;
		if (present && (length != unchecked((int)MuiAreaGeometryStateRecord.Size) ||
			!MuiAreaGeometryStateRecordCodec.TryReadStructural(ref platform, block,
				out record) || !MuiAreaGeometryStateAdmission.ValidateLive(ref platform,
					state, obj, record)))
			return false;
		var left = 0u;
		var top = 0u;
		var width = 0u;
		var height = 0u;
		var right = 0u;
		var bottom = 0u;
		var hasLeft = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			LeftEdge, out left);
		var hasTop = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			TopEdge, out top);
		var hasWidth = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			Width, out width);
		var hasHeight = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			Height, out height);
		var hasRight = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			RightEdge, out right);
		var hasBottom = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			BottomEdge, out bottom);
		var hasRawGeometry = hasLeft || hasTop || hasWidth || hasHeight || hasRight ||
			hasBottom;
		if (!hasRawGeometry)
		{
			if (present)
			{
				result.Left = record.Left;
				result.Top = record.Top;
				result.Width = record.Width;
				result.Height = record.Height;
				result.Right = record.Right;
				result.Bottom = record.Bottom;
			}
			return true;
		}
		var raw = default(MuiAreaGeometryStateRecord);
		raw.Magic = MuiAreaGeometryStateRecord.Cookie;
		raw.Left = unchecked((int)left);
		raw.Top = unchecked((int)top);
		raw.Width = unchecked((int)width);
		raw.Height = unchecked((int)height);
		raw.Right = unchecked((int)right);
		raw.Bottom = unchecked((int)bottom);
		var primaryChanged = present && (record.Left != raw.Left ||
			record.Top != raw.Top || record.Width != raw.Width ||
			record.Height != raw.Height);
		if (raw.Width > 0 && (!hasRight || primaryChanged) &&
			!MuiAreaGeometryStateValidation.TryExpectedEdge(raw.Left, raw.Width,
				out raw.Right)) return false;
		if (raw.Height > 0 && (!hasBottom || primaryChanged) &&
			!MuiAreaGeometryStateValidation.TryExpectedEdge(raw.Top, raw.Height,
				out raw.Bottom)) return false;
		if (primaryChanged)
		{
			// The six public attributes are legacy scalar projections. A caller
			// may update a primary coordinate directly, leaving the derived edge
			// slot stale; canonicalize that compatibility update into the named
			// record instead of treating the stale edge as a different geometry shape.
			if (raw.Width < 0 || raw.Height < 0) return false;
		}
		else if (!MuiAreaGeometryStateValidation.IsValidState(raw)) return false;
		if (present && (record.Left != raw.Left || record.Top != raw.Top ||
			record.Width != raw.Width || record.Height != raw.Height ||
			record.Right != raw.Right || record.Bottom != raw.Bottom) &&
			!MuiAreaGeometryStateRecordCodec.Write(ref platform, block, raw))
			return false;
		result.Left = raw.Left;
		result.Top = raw.Top;
		result.Width = raw.Width;
		result.Height = raw.Height;
		result.Right = raw.Right;
		result.Bottom = raw.Bottom;
		return true;
	}

	internal static bool TryResolveContentRect<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiAreaGeometryState geometry, out MuiAreaContentRect result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (!TryReadLayoutPolicyState(ref platform, state, obj,
			out var policy)) return false;
		var outerWidth = geometry.Width > 0 ? (uint)geometry.Width : 0u;
		var outerHeight = geometry.Height > 0 ? (uint)geometry.Height : 0u;
		var leftInset = policy.InnerLeft > outerWidth ? outerWidth : policy.InnerLeft;
		var rightSpace = outerWidth - leftInset;
		var rightInset = policy.InnerRight > rightSpace ? rightSpace :
			policy.InnerRight;
		var topInset = policy.InnerTop > outerHeight ? outerHeight : policy.InnerTop;
		var bottomSpace = outerHeight - topInset;
		var bottomInset = policy.InnerBottom > bottomSpace ? bottomSpace :
			policy.InnerBottom;
		result.Left = AddInset(geometry.Left, leftInset);
		result.Top = AddInset(geometry.Top, topInset);
		result.Width = unchecked((int)(outerWidth - leftInset - rightInset));
		result.Height = unchecked((int)(outerHeight - topInset - bottomInset));
		return true;
	}

	private static int AddInset(int origin, uint inset)
	{
		if (origin >= 0)
		{
			var positiveOrigin = unchecked((uint)origin);
			if (inset > unchecked((uint)int.MaxValue) - positiveOrigin)
				return int.MaxValue;
			return origin + unchecked((int)inset);
		}
		var magnitude = unchecked((uint)(-(origin + 1))) + 1u;
		if (inset >= magnitude)
		{
			var positive = inset - magnitude;
			return positive > unchecked((uint)int.MaxValue) ? int.MaxValue :
				unchecked((int)positive);
		}
		var remaining = magnitude - inset;
		return remaining == 0x80000000u ? int.MinValue :
			-unchecked((int)remaining);
	}

	private static bool TryReadGeometryStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaGeometryStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (obj.IsNull || MuiHeadlessObjectCore.FindObject(ref platform, state,
			obj).IsNull) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AreaGeometryStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			AreaGeometryStateKey) != unchecked((int)MuiAreaGeometryStateRecord.Size))
			return false;
		return MuiAreaGeometryStateRecordCodec.TryReadStructural(ref platform, block,
			out value) && MuiAreaGeometryStateAdmission.ValidateLive(ref platform,
			state, obj, value);
	}

	private static bool EnsureGeometryStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AreaGeometryStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			AreaGeometryStateKey);
		var present = block.IsNotNull || length != 0;
		if (present)
			return TryReadGeometryStateRecord(ref platform, state, obj, out _);
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaGeometryStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaGeometryStateRecord.Size);
		var value = default(MuiAreaGeometryStateRecord);
		value.Magic = MuiAreaGeometryStateRecord.Cookie;
		if (!MuiAreaGeometryStateValidation.IsValidRecord(value))
		{
			platform.Clear(scratch, MuiAreaGeometryStateRecord.Size);
			platform.Free(scratch, MuiAreaGeometryStateRecord.Size);
			return false;
		}
		var written = MuiAreaGeometryStateRecordCodec.Write(ref platform, scratch,
			value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			AreaGeometryStateKey, scratch,
			unchecked((int)MuiAreaGeometryStateRecord.Size));
		platform.Clear(scratch, MuiAreaGeometryStateRecord.Size);
		platform.Free(scratch, MuiAreaGeometryStateRecord.Size);
		return added;
	}

	private static bool PublishGeometryState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaGeometryState value)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var candidate = default(MuiAreaGeometryStateRecord);
		candidate.Magic = MuiAreaGeometryStateRecord.Cookie;
		candidate.Left = value.Left;
		candidate.Top = value.Top;
		candidate.Width = value.Width;
		candidate.Height = value.Height;
		candidate.Right = value.Right;
		candidate.Bottom = value.Bottom;
		if (!MuiAreaGeometryStateAdmission.ValidateLive(ref platform, state, obj,
			candidate)) return false;
		if (!EnsureGeometryStateRecord(ref platform, state, obj)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AreaGeometryStateKey);
		var stored = candidate;
		if (!MuiAreaGeometryStateRecordCodec.Write(ref platform, block, stored))
			return false;
		if (!(Set(ref platform, state, obj, LeftEdge, stored.Left) &&
			Set(ref platform, state, obj, TopEdge, stored.Top) &&
			Set(ref platform, state, obj, Width, stored.Width) &&
			Set(ref platform, state, obj, Height, stored.Height) &&
			Set(ref platform, state, obj, RightEdge, stored.Right) &&
			Set(ref platform, state, obj, BottomEdge, stored.Bottom))) return false;
		// String.mui exposes its scroll metrics as a notification-friendly
		// projection of content and Area geometry. Refresh only after the complete
		// named geometry record is coherent; other classes remain untouched.
		if (MuiCommonControlCore.Classify(ref platform, state, obj) ==
			MuiControlClass.String &&
			!MuiStringScrollAttributeCore.Refresh(ref platform, state, obj, true))
			return false;
		return true;
	}

	internal static bool TryGetGeometryStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaGeometryStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadGeometryStateRecord(ref platform, state, obj, out value);

	private static bool Set<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, int value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			unchecked((uint)value), false);

	private static bool Attribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint defaultValue, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out value)) return true;
		value = defaultValue;
		return MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNotNull;
	}

	private static uint Clamp(uint value, uint maximum) =>
		value > maximum ? maximum : value;

	private static short ToDimension(uint value) =>
		unchecked((short)(value > Maximum ? Maximum : value));
}

public static class MuiGroupLayoutCore
{
	private const uint Horizontal = 0x8042536B;
	private const uint HorizontalSpacing = 0x8042C651;
	private const uint VerticalSpacing = 0x8042E1BF;
	private const uint Spacing = 0x8042866D;
	private const uint SameWidth = 0x8042B3EC;
	private const uint SameHeight = 0x8042037E;
	private const uint SameSize = 0x80420860;
	private const uint PageMode = 0x80421A5F;
	private const uint LayoutPolicyStateKey = 0x0D100013u;
	private const int NoDisappearPriority = 0;
	private const short Maximum = 10000;

	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == Horizontal || attribute == HorizontalSpacing ||
		attribute == VerticalSpacing || attribute == Spacing ||
		attribute == SameWidth || attribute == SameHeight || attribute == SameSize ||
		attribute == PageMode;

	private static bool IsShown<TPlatform>(ref TPlatform platform, APTR state,
		APTR child) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiAreaLayoutCore.TryReadLayoutPolicyState(ref platform, state,
			child, out var policy))
		{
			// A child that predates the named record is still visible while the
			// bootstrap projection is unavailable.  A present-but-malformed record,
			// however, is not allowed to opt into layout through this fallback.
			var block = MuiStoreCore.DataspaceFind(ref platform, state, child,
				MuiAreaLayoutCore.AreaLayoutPolicyStateKey);
			var length = MuiStoreCore.DataspaceLength(ref platform, state, child,
				MuiAreaLayoutCore.AreaLayoutPolicyStateKey);
			return block.IsNull && length == 0;
		}
		return policy.ShowMe != 0;
	}

	private static int ReadDisappearPriority<TPlatform>(ref TPlatform platform,
		APTR state, APTR child, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiAreaDisappearCore.TryReadState(ref platform, state, child,
			out var disappear)) return NoDisappearPriority;
		return horizontal ? disappear.HorizDisappear : disappear.VertDisappear;
	}

	private static bool IsHidden<TPlatform>(ref TPlatform platform, APTR state,
		APTR child, bool horizontal, int hiddenPriority)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!IsShown(ref platform, state, child)) return true;
		if (hiddenPriority <= NoDisappearPriority) return false;
		var priority = ReadDisappearPriority(ref platform, state, child,
			horizontal);
		return priority > NoDisappearPriority && priority <= hiddenPriority;
	}

	private static int CountCandidates<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var result = 0;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (child.IsNull || !IsShown(ref platform, state, child)) continue;
			if (ReadDisappearPriority(ref platform, state, child, horizontal) >
				NoDisappearPriority) result++;
		}
		return result;
	}

	private static int CountVisible<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, bool horizontal, int hiddenPriority)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var result = 0;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (child.IsNull || IsHidden(ref platform, state, child, horizontal,
				hiddenPriority)) continue;
			result++;
		}
		return result;
	}

	private static int FindNextPriority<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, bool horizontal, int after)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var next = int.MaxValue;
		var found = false;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (child.IsNull || !IsShown(ref platform, state, child)) continue;
			var priority = ReadDisappearPriority(ref platform, state, child,
				horizontal);
			if (priority > after && priority > NoDisappearPriority &&
				(!found || priority < next))
			{
				next = priority;
				found = true;
			}
		}
		return found ? next : NoDisappearPriority;
	}

	private static int AddExtent(int value, int addition)
	{
		if (addition <= 0) return value;
		return value > int.MaxValue - addition ? int.MaxValue : value + addition;
	}

	private static int RequiredExtent<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, bool horizontal, int hiddenPriority,
		int spacing)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var extent = 0;
		var visible = 0;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (child.IsNull || IsHidden(ref platform, state, child, horizontal,
				hiddenPriority)) continue;
			var item = MuiAreaLayoutCore.ComputeMinMax(ref platform, state, child);
			var childExtent = horizontal ? item.MinWidth : item.MinHeight;
			extent = AddExtent(extent, childExtent);
			visible++;
		}
		if (visible > 1) extent = AddExtent(extent, spacing * (visible - 1));
		return extent;
	}

	private static short MinimumExtent<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, bool horizontal, bool stacked,
		bool suppressDisappear, int spacing)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var hiddenPriority = suppressDisappear ? int.MaxValue :
			NoDisappearPriority;
		var extent = 0;
		var visible = 0;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (child.IsNull || IsHidden(ref platform, state, child, horizontal,
				hiddenPriority)) continue;
			var item = MuiAreaLayoutCore.ComputeMinMax(ref platform, state, child);
			var childExtent = horizontal ? item.MinWidth : item.MinHeight;
			if (stacked)
				extent = AddExtent(extent, childExtent);
			else if (childExtent > extent) extent = childExtent;
			visible++;
		}
		if (stacked && visible > 1)
			extent = AddExtent(extent, spacing * (visible - 1));
		return unchecked((short)(extent > 10000 ? 10000 : extent));
	}

	private static MuiGroupEqualExtentSelection
		ResolveEqualExtentSelection<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, int count, bool horizontal, int available, int spacing,
		int hiddenPriority)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var selection = default(MuiGroupEqualExtentSelection);
		selection.Available = available < 0 ? 0 : available;
		selection.Spacing = spacing < 0 ? 0 : spacing;
		selection.MinimumExtent = 0;
		selection.MaximumExtent = 0;
		selection.DefaultExtent = 0;
		var finiteMaximum = false;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (child.IsNull || IsHidden(ref platform, state, child, horizontal,
				hiddenPriority)) continue;
			var item = MuiAreaLayoutCore.ComputeMinMax(ref platform, state, child);
			var minimum = horizontal ? item.MinWidth : item.MinHeight;
			var maximum = horizontal ? item.MaxWidth : item.MaxHeight;
			var preferred = horizontal ? item.DefWidth : item.DefHeight;
			if (minimum > selection.MinimumExtent)
				selection.MinimumExtent = minimum;
			if (preferred > selection.DefaultExtent)
				selection.DefaultExtent = preferred;
			if (maximum > 0 && (!finiteMaximum || maximum < selection.MaximumExtent))
			{
				selection.MaximumExtent = maximum;
				finiteMaximum = true;
			}
			selection.VisibleCount++;
		}
		selection.HasFiniteMaximum = finiteMaximum ? 1u : 0u;
		selection.GapCount = selection.VisibleCount > 1 ?
			selection.VisibleCount - 1 : 0;
		if (selection.VisibleCount == 0) return selection;
		// Equal-size defaults are part of the same bounded contract as the
		// minimum and maximum.  A child can advertise a preferred extent larger
		// than its finite maximum; keep the named selection record ordered so
		// AskMinMax never publishes Default > Max (or Default < Min).
		MuiGroupEqualExtentCore.NormalizeDefault(ref selection);
		var equal = selection.Available / selection.VisibleCount;
		if (equal < selection.MinimumExtent) equal = selection.MinimumExtent;
		if (finiteMaximum && equal > selection.MaximumExtent)
			equal = selection.MaximumExtent;
		if (equal < 0) equal = 0;
		selection.EqualExtent = equal;
		selection.TailExtent = selection.Available - equal *
			(selection.VisibleCount - 1);
		if (selection.TailExtent < selection.MinimumExtent ||
			(finiteMaximum && selection.TailExtent > selection.MaximumExtent))
			selection.TailExtent = equal;
		return selection;
	}

	// SameWidth/SameHeight applies to the component named by the attribute,
	// regardless of whether that component is the Group's stacking axis.  The
	// regular helper above derives a per-child slot for the stacked axis; this
	// companion keeps the same typed bounds but resolves one common cross-axis
	// extent for every shown child.  No child pointers or managed collections
	// escape the guest-resident decision record.
	private static int CommonChildExtent(
		MuiGroupEqualExtentSelection selection, int available)
	{
		var extent = available < 0 ? 0 : available;
		if (extent < selection.MinimumExtent)
			extent = selection.MinimumExtent;
		if (selection.HasFiniteMaximum != 0 &&
			extent > selection.MaximumExtent)
			extent = selection.MaximumExtent;
		return extent < 0 ? 0 : extent;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static short AggregateEqualExtent(int extent, int count,
		int spacing)
	{
		if (count <= 0) return 0;
		var normalizedExtent = extent < 0 ? 0 : extent;
		var normalizedSpacing = spacing < 0 ? 0 : spacing;
		// Keep this path strictly MC68000-friendly. The result is capped at the
		// public layout extent limit, so a saturating 32-bit calculation avoids
		// both signed overflow and a hidden 64-bit multiply in freestanding code.
		const int maximum = 10000;
		var gaps = count - 1;
		var total = 0;
		if (normalizedSpacing != 0)
		{
			if (gaps > maximum / normalizedSpacing) return maximum;
			total = normalizedSpacing * gaps;
		}
		if (total >= maximum) return maximum;
		if (normalizedExtent != 0 && count >
			(maximum - total) / normalizedExtent)
			return maximum;
		total += normalizedExtent * count;
		return unchecked((short)(total > maximum ? maximum : total));
	}

	private static MuiGroupDisappearSelection ResolveDisappearSelection<TPlatform>(
		ref TPlatform platform, APTR state, APTR group, int count,
		bool horizontal, int available, int spacing)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var selection = default(MuiGroupDisappearSelection);
		selection.Axis = horizontal ? MuiGroupDisappearAxis.Horizontal :
			MuiGroupDisappearAxis.Vertical;
		selection.Available = available < 0 ? 0 : available;
		selection.Spacing = spacing < 0 ? 0 : spacing;
		selection.HiddenPriority = NoDisappearPriority;
		selection.CandidateCount = CountCandidates(ref platform, state, group,
			count, horizontal);
		selection.VisibleCount = CountVisible(ref platform, state, group, count,
			horizontal, selection.HiddenPriority);
		while (selection.CandidateCount > 0 && selection.VisibleCount > 0 &&
			RequiredExtent(ref platform, state, group, count, horizontal,
				selection.HiddenPriority, selection.Spacing) > selection.Available)
		{
			var next = FindNextPriority(ref platform, state, group, count,
				horizontal, selection.HiddenPriority);
			if (next <= NoDisappearPriority) break;
			selection.HiddenPriority = next;
			selection.VisibleCount = CountVisible(ref platform, state, group,
				count, horizontal, selection.HiddenPriority);
		}
		return selection;
	}

	internal static bool TryGetAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (!IsPublicGetterAttribute(attribute) ||
			!MuiGroupChangeCore.IsGroupObject(ref platform, state, group))
			return false;
		if (TryGetLayoutState(ref platform, state, group, out var policy))
		{
			value = attribute == Horizontal ? policy.Horizontal :
				attribute == HorizontalSpacing ? policy.HorizontalSpacing :
				attribute == VerticalSpacing ? policy.VerticalSpacing :
				attribute == Spacing ? policy.HorizontalSpacing :
				attribute == SameWidth ? policy.SameWidth :
				attribute == SameHeight ? policy.SameHeight :
				attribute == SameSize ? (policy.SameWidth != 0 &&
					policy.SameHeight != 0 ? 1u : 0u) : policy.PageMode;
			return true;
		}
		var policyBlock = MuiStoreCore.DataspaceFind(ref platform, state, group,
			LayoutPolicyStateKey);
		var policyLength = MuiStoreCore.DataspaceLength(ref platform, state, group,
			LayoutPolicyStateKey);
		// A present record is authoritative for the public getter seam.  Do not
		// fall back to raw attributes when that named struct is malformed.
		if (policyBlock.IsNotNull || policyLength != 0) return false;
		var hasSource = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			group, attribute, out _);
		if (!hasSource && (attribute == SameWidth || attribute == SameHeight) &&
			MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
				SameSize, out _)) hasSource = true;
		if (!hasSource && attribute == SameSize &&
			(MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
				SameWidth, out _) || MuiHeadlessObjectCore.GetRawAttribute(ref platform,
				state, group, SameHeight, out _))) hasSource = true;
		if (!hasSource && (attribute == HorizontalSpacing ||
			attribute == VerticalSpacing) &&
			!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
				Spacing, out _)) return false;
		if (!hasSource && attribute == Spacing &&
			!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
				Spacing, out _)) return false;
		if (!hasSource && attribute != HorizontalSpacing &&
			attribute != VerticalSpacing && attribute != Spacing) return false;
		if (!TryReadLayoutPolicyState(ref platform, state, group,
			out var resolved)) return false;
		value = attribute == Horizontal ? resolved.Horizontal :
			attribute == HorizontalSpacing ? resolved.HorizontalSpacing :
			attribute == VerticalSpacing ? resolved.VerticalSpacing :
			attribute == Spacing ? resolved.HorizontalSpacing :
			attribute == SameWidth ? resolved.SameWidth :
			attribute == SameHeight ? resolved.SameHeight :
			attribute == SameSize ? (resolved.SameWidth != 0 &&
				resolved.SameHeight != 0 ? 1u : 0u) : resolved.PageMode;
		return true;
	}

	public static bool AskMinMax<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, APTR storage) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadLayoutPolicyState(ref platform, state, group,
			out _)) return false;
		if (!MuiGroupLayoutHookCore.TryReadEffectiveState(ref platform, state,
			group, out var hookState)) return false;
		if (hookState.Hook.IsNotNull)
		{
			if (!MuiGroupLayoutHookCore.InvokeMinMax(ref platform, state, group,
				out var hooked)) return false;
			return MuiAreaLayoutCore.WriteMinMax(ref platform, storage, hooked);
		}
		if (!MuiGroupGridCore.TryRead(ref platform, state, group,
			out _)) return false;
		return MuiAreaLayoutCore.WriteMinMax(ref platform, storage,
			ComputeMinMax(ref platform, state, group));
	}

	internal static MuiMinMaxValues ComputeMinMax<TPlatform>(ref TPlatform platform,
		APTR state, APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadLayoutPolicyState(ref platform, state, group,
			out var policy)) return default;
		if (!MuiGroupLayoutHookCore.TryReadEffectiveState(ref platform, state,
			group, out var hookState)) return default;
		if (hookState.Hook.IsNotNull)
		{
			return MuiGroupLayoutHookCore.InvokeMinMax(ref platform, state, group,
				out var hooked) ? hooked : default;
		}
		MuiMinMaxValues result = default;
		var horizontal = policy.Horizontal;
		var pageMode = policy.PageMode;
		var count = CountChildren(ref platform, state, group);
		if (!MuiGroupGridCore.TryRead(ref platform, state, group,
			out var grid)) return default;
		if (MuiGroupGridCore.IsEnabled(grid, count))
			return MuiGroupGridCore.ComputeMinMax(ref platform, state, group,
				grid, count);
		// Percentage/default spacing has no parent extent during AskMinMax.
		// Resolve only concrete pixel values here; Layout resolves special
		// inputs against the actual group rectangle below.
		var spacing = ReadSpacing(policy, horizontal != 0);
		var horizontalSumMaximum = default(MuiGroupMaximumSumState);
		var verticalSumMaximum = default(MuiGroupMaximumSumState);
		var horizontalExtentMaximum = default(MuiGroupMaximumExtentState);
		var verticalExtentMaximum = default(MuiGroupMaximumExtentState);
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			var item = MuiAreaLayoutCore.ComputeMinMax(ref platform, state, child);
			if (pageMode != 0)
			{
				result.MinWidth = Larger(result.MinWidth, item.MinWidth);
				result.MinHeight = Larger(result.MinHeight, item.MinHeight);
				// MorphOS Page groups take the intersection of child maxima:
				// the smallest finite maximum is the group's maximum.  A zero
				// maximum remains the unbounded sentinel used by the MUI record.
				result.MaxWidth = SmallerMax(result.MaxWidth, item.MaxWidth);
				result.MaxHeight = SmallerMax(result.MaxHeight, item.MaxHeight);
				result.DefWidth = Larger(result.DefWidth, item.DefWidth);
				result.DefHeight = Larger(result.DefHeight, item.DefHeight);
			}
			else if (horizontal != 0)
			{
				result.MinWidth = Add(result.MinWidth, item.MinWidth);
				result.MinHeight = Larger(result.MinHeight, item.MinHeight);
				horizontalSumMaximum.Include(item.MaxWidth);
				verticalExtentMaximum.Include(item.MaxHeight);
				result.DefWidth = Add(result.DefWidth, item.DefWidth);
				result.DefHeight = Larger(result.DefHeight, item.DefHeight);
			}
			else
			{
				result.MinWidth = Larger(result.MinWidth, item.MinWidth);
				result.MinHeight = Add(result.MinHeight, item.MinHeight);
				horizontalExtentMaximum.Include(item.MaxWidth);
				verticalSumMaximum.Include(item.MaxHeight);
				result.DefWidth = Larger(result.DefWidth, item.DefWidth);
				result.DefHeight = Add(result.DefHeight, item.DefHeight);
			}
		}
		if (pageMode == 0 && count > 1)
		{
			var gaps = spacing * (count - 1);
			if (horizontal != 0)
			{
				result.MinWidth = Add(result.MinWidth, gaps);
				horizontalSumMaximum.IncludeGap(gaps);
				result.DefWidth = Add(result.DefWidth, gaps);
			}
			else
			{
				result.MinHeight = Add(result.MinHeight, gaps);
				verticalSumMaximum.IncludeGap(gaps);
				result.DefHeight = Add(result.DefHeight, gaps);
			}
		}
		if (pageMode == 0)
		{
			result.MaxWidth = horizontal != 0 ? horizontalSumMaximum.Value :
				horizontalExtentMaximum.Value;
			result.MaxHeight = horizontal != 0 ? verticalExtentMaximum.Value :
				verticalSumMaximum.Value;
		}
		// An empty Group has no child-provided bound. Keep the normal MUI
		// unbounded default instead of confusing an empty aggregate with an
		// explicit child maximum of zero.
		if (count == 0)
		{
			result.MaxWidth = Maximum;
			result.MaxHeight = Maximum;
		}
		// SameWidth/SameHeight applies one common main-axis extent to every
		// visible child. Derive the common min/max/default from the children so
		// a fixed maximum cannot be exceeded merely because the group is wider.
		if (pageMode == 0 && count > 0)
		{
			if (horizontal != 0 && policy.SameWidth != 0)
			{
				var bounds = ResolveEqualExtentSelection(ref platform, state, group,
					count, true, 0, spacing, NoDisappearPriority);
				result.MinWidth = AggregateEqualExtent(bounds.MinimumExtent,
					bounds.VisibleCount,
					spacing);
				result.MaxWidth = bounds.HasFiniteMaximum == 0 ? (short)0 :
					AggregateEqualExtent(bounds.MaximumExtent, bounds.VisibleCount,
						spacing);
				result.DefWidth = AggregateEqualExtent(bounds.DefaultExtent,
					bounds.VisibleCount,
					spacing);
			}
			else if (horizontal == 0 && policy.SameHeight != 0)
			{
				var bounds = ResolveEqualExtentSelection(ref platform, state, group,
					count, false, 0, spacing, NoDisappearPriority);
				result.MinHeight = AggregateEqualExtent(bounds.MinimumExtent,
					bounds.VisibleCount,
					spacing);
				result.MaxHeight = bounds.HasFiniteMaximum == 0 ? (short)0 :
					AggregateEqualExtent(bounds.MaximumExtent, bounds.VisibleCount,
						spacing);
				result.DefHeight = AggregateEqualExtent(bounds.DefaultExtent,
					bounds.VisibleCount,
					spacing);
			}
		}
		// The main axis minimum may be satisfied by removing positive
		// disappearance-priority children. Keep cross-axis minima unchanged, and
		// retain the full aggregate for Def/Max so a normally sized group still
		// requests all of its children. This lets a parent actually reach the
		// layout boundary where the selection policy can take effect.
		if (pageMode == 0 && count > 0)
		{
			if (horizontal != 0)
			{
				if (policy.SameWidth != 0)
				{
					var bounds = ResolveEqualExtentSelection(ref platform, state,
						group, count, true, 0, spacing, int.MaxValue);
					result.MinWidth = AggregateEqualExtent(bounds.MinimumExtent,
						bounds.VisibleCount, spacing);
				}
				else result.MinWidth = MinimumExtent(ref platform, state, group,
					count, true, true, true, spacing);
				if (policy.SameHeight != 0)
				{
					var bounds = ResolveEqualExtentSelection(ref platform, state,
						group, count, false, 0, 0, NoDisappearPriority);
					result.MinHeight = ClampDimension(bounds.MinimumExtent);
					result.MaxHeight = bounds.HasFiniteMaximum == 0 ? (short)0 :
						ClampDimension(bounds.MaximumExtent);
					result.DefHeight = ClampDimension(bounds.DefaultExtent);
				}
				else result.MinHeight = MinimumExtent(ref platform, state, group,
					count, false, false, false, 0);
			}
			else
			{
				if (policy.SameWidth != 0)
				{
					var bounds = ResolveEqualExtentSelection(ref platform, state,
						group, count, true, 0, 0, NoDisappearPriority);
					result.MinWidth = ClampDimension(bounds.MinimumExtent);
					result.MaxWidth = bounds.HasFiniteMaximum == 0 ? (short)0 :
						ClampDimension(bounds.MaximumExtent);
					result.DefWidth = ClampDimension(bounds.DefaultExtent);
				}
				else result.MinWidth = MinimumExtent(ref platform, state, group,
					count, true, false, false, 0);
				if (policy.SameHeight != 0)
				{
					var bounds = ResolveEqualExtentSelection(ref platform, state,
						group, count, false, 0, spacing, int.MaxValue);
					result.MinHeight = AggregateEqualExtent(bounds.MinimumExtent,
						bounds.VisibleCount, spacing);
				}
				else result.MinHeight = MinimumExtent(ref platform, state, group,
					count, false, true, true, spacing);
			}
		}
		return result;
	}

	public static bool Layout<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, int left, int top, int width, int height)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadLayoutPolicyState(ref platform, state, group,
			out var policy)) return false;
		if (!MuiGroupLayoutHookCore.TryReadEffectiveState(ref platform, state,
			group, out var hookState)) return false;
		if (hookState.Hook.IsNotNull)
		{
			if (!MuiAreaLayoutCore.Layout(ref platform, state, group, left, top,
				width, height)) return false;
			if (!MuiGroupLayoutHookCore.InvokeLayout(ref platform, state, group,
				width, height, out var dimensions)) return false;
			if (dimensions.Width < 0 || dimensions.Height < 0) return false;
			if (dimensions.Width != width || dimensions.Height != height)
				return MuiAreaLayoutCore.Layout(ref platform, state, group, left,
					top, dimensions.Width, dimensions.Height);
			return true;
		}
		var horizontal = policy.Horizontal;
		var count = CountChildren(ref platform, state, group);
		if (!MuiGroupGridCore.TryRead(ref platform, state, group,
			out var grid)) return false;
		if (count == 0) return MuiAreaLayoutCore.Layout(ref platform, state, group,
			left, top, width, height);
		if (MuiGroupGridCore.IsEnabled(grid, count))
			return MuiGroupGridCore.Layout(ref platform, state, group, left, top,
				width, height, grid, count);
		var pageMode = policy.PageMode;
		if (pageMode != 0)
			return LayoutPage(ref platform, state, group, left, top, width, height,
				count);
		var spacingSelection = MuiGroupSpacingCore.ResolveSelection(
			policy.HorizontalSpacing, policy.VerticalSpacing, width, height);
		if (horizontal != 0)
		{
			var spacing = spacingSelection.Horizontal.Pixels;
			var selection = ResolveDisappearSelection(ref platform, state, group,
				count, true, width, spacing);
			return LayoutHorizontal(ref platform, state, group, left, top, width,
				height, count, spacing, policy, selection, grid);
		}
		{
			var spacing = spacingSelection.Vertical.Pixels;
			var selection = ResolveDisappearSelection(ref platform, state, group,
				count, false, height, spacing);
			return LayoutVertical(ref platform, state, group, left, top, width,
				height, count, spacing, policy, selection, grid);
		}
	}

	private static bool LayoutHorizontal<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int left, int top, int width, int height, int count,
		int spacing, MuiGroupLayoutPolicyStateRecord policy,
		MuiGroupDisappearSelection selection, MuiGroupGridSpec alignment)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var visibleCount = selection.VisibleCount;
		var gapCount = visibleCount > 1 ? visibleCount - 1 : 0;
		var available = width - spacing * gapCount;
		if (available < 0) available = 0;
		var equal = policy.SameWidth;
		var equalSelection = equal == 0 ? default :
			ResolveEqualExtentSelection(ref platform, state, group, count, true,
				available, spacing, selection.HiddenPriority);
		var equalCross = policy.SameHeight == 0 ? default :
			ResolveEqualExtentSelection(ref platform, state, group, count, false,
				height, 0, NoDisappearPriority);
		var equalCrossExtent = policy.SameHeight == 0 ? 0 :
			CommonChildExtent(equalCross, height);
		var total = HorizontalWeightTotal(ref platform, state, group, count,
			selection);
		var totalMinimum = MainMinimumTotal(ref platform, state, group, count,
			selection, true);
		var allocation = MuiGroupAxisAllocationCore.Begin(available, total,
			totalMinimum);
		var cursor = left;
		var visibleIndex = 0;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (IsHidden(ref platform, state, child, true,
				selection.HiddenPriority))
			{
				if (!MuiAreaLayoutCore.Layout(ref platform, state, child, left, top,
					0, 0)) return false;
				continue;
			}
			var weight = MuiAreaLayoutCore.HorizontalWeight(ref platform, state,
				child);
			int extent;
			var allocated = false;
			var minMax = default(MuiMinMaxValues);
			if (equal != 0 && visibleCount > 0)
				extent = visibleIndex == visibleCount - 1 ?
					equalSelection.TailExtent : equalSelection.EqualExtent;
			else
			{
				minMax = ComputePlacementMinMax(ref platform, state, child);
				MuiGroupAxisAllocationCore.Take(ref allocation, weight,
					minMax.MinWidth, minMax.MaxWidth,
					visibleIndex == visibleCount - 1);
				extent = allocation.Slot;
				allocated = true;
			}
			if (!allocated)
				minMax = ComputePlacementMinMax(ref platform, state, child);
			var placedWidth = FitExtent(minMax.MinWidth, minMax.MaxWidth,
				extent);
			var placedHeight = policy.SameHeight != 0 &&
				equalCross.VisibleCount > 0
				? FitExtent(minMax.MinHeight, minMax.MaxHeight,
					equalCrossExtent)
				: FitExtent(minMax.MinHeight, minMax.MaxHeight, height);
			var childLeft = cursor + AlignFree(extent - placedWidth,
				alignment.HorizontalCenter);
			var childTop = top + AlignFree(height - placedHeight,
				alignment.VerticalCenter);
			if (!MuiAreaLayoutCore.Layout(ref platform, state, child, childLeft,
				childTop, placedWidth, placedHeight)) return false;
			cursor += extent + spacing;
			visibleIndex++;
		}
		return MuiAreaLayoutCore.Layout(ref platform, state, group, left, top,
			width, height);
	}

	private static bool LayoutVertical<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int left, int top, int width, int height, int count,
		int spacing, MuiGroupLayoutPolicyStateRecord policy,
		MuiGroupDisappearSelection selection, MuiGroupGridSpec alignment)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var visibleCount = selection.VisibleCount;
		var gapCount = visibleCount > 1 ? visibleCount - 1 : 0;
		var available = height - spacing * gapCount;
		if (available < 0) available = 0;
		var equal = policy.SameHeight;
		var equalSelection = equal == 0 ? default :
			ResolveEqualExtentSelection(ref platform, state, group, count, false,
				available, spacing, selection.HiddenPriority);
		var equalCross = policy.SameWidth == 0 ? default :
			ResolveEqualExtentSelection(ref platform, state, group, count, true,
				width, 0, NoDisappearPriority);
		var equalCrossExtent = policy.SameWidth == 0 ? 0 :
			CommonChildExtent(equalCross, width);
		var total = VerticalWeightTotal(ref platform, state, group, count,
			selection);
		var totalMinimum = MainMinimumTotal(ref platform, state, group, count,
			selection, false);
		var allocation = MuiGroupAxisAllocationCore.Begin(available, total,
			totalMinimum);
		var cursor = top;
		var visibleIndex = 0;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (IsHidden(ref platform, state, child, false,
				selection.HiddenPriority))
			{
				if (!MuiAreaLayoutCore.Layout(ref platform, state, child, left, top,
					0, 0)) return false;
				continue;
			}
			var weight = MuiAreaLayoutCore.VerticalWeight(ref platform, state, child);
			int extent;
			var allocated = false;
			var minMax = default(MuiMinMaxValues);
			if (equal != 0 && visibleCount > 0)
				extent = visibleIndex == visibleCount - 1 ?
					equalSelection.TailExtent : equalSelection.EqualExtent;
			else
			{
				minMax = ComputePlacementMinMax(ref platform, state, child);
				MuiGroupAxisAllocationCore.Take(ref allocation, weight,
					minMax.MinHeight, minMax.MaxHeight,
					visibleIndex == visibleCount - 1);
				extent = allocation.Slot;
				allocated = true;
			}
			if (!allocated)
				minMax = ComputePlacementMinMax(ref platform, state, child);
			var placedWidth = policy.SameWidth != 0 &&
				equalCross.VisibleCount > 0
				? FitExtent(minMax.MinWidth, minMax.MaxWidth,
					equalCrossExtent)
				: FitExtent(minMax.MinWidth, minMax.MaxWidth, width);
			var placedHeight = FitExtent(minMax.MinHeight, minMax.MaxHeight,
				extent);
			var childLeft = left + AlignFree(width - placedWidth,
				alignment.HorizontalCenter);
			var childTop = cursor + AlignFree(extent - placedHeight,
				alignment.VerticalCenter);
			if (!MuiAreaLayoutCore.Layout(ref platform, state, child, childLeft,
				childTop, placedWidth, placedHeight)) return false;
			cursor += extent + spacing;
			visibleIndex++;
		}
		return MuiAreaLayoutCore.Layout(ref platform, state, group, left, top,
			width, height);
	}

	// GroupGridCore already normalizes the MorphOS 0/1/2 alignment modes into
	// the named MuiGroupGridSpec. Reuse that value record for ordinary Groups so
	// the one-dimensional path has no second raw-offset policy representation.
	private static MuiMinMaxValues ComputePlacementMinMax<TPlatform>(
		ref TPlatform platform, APTR state, APTR child)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		// Radio and Scrollbar are Group-derived composite controls whose public
		// AskMinMax path deliberately owns their child topology. Calling the
		// generic Area/Common-control path here would re-enter that Group path;
		// treat their main/cross-axis bounds as unbounded for parent alignment.
		var control = MuiCommonControlCore.Classify(ref platform, state, child);
		if (control == MuiControlClass.Radio ||
			control == MuiControlClass.Scrollbar) return default;
		return MuiAreaLayoutCore.ComputeMinMax(ref platform, state, child);
	}

	private static int AlignFree(int free, uint mode)
	{
		if (free <= 0 || mode == 0) return 0;
		return mode == 2 ? free : free / 2;
	}

	private static int FitExtent(short minimum, short maximum, int available)
	{
		if (available <= 0) return 0;
		var lower = minimum > 0 ? (int)minimum : 0;
		var upper = maximum > 0 ? (int)maximum : int.MaxValue;
		if (upper < lower) lower = upper;
		var result = available;
		if (result < lower) result = lower;
		if (result > upper) result = upper;
		return result > available ? available : result;
	}

	private static int MainMinimumTotal<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count, MuiGroupDisappearSelection selection,
		bool horizontal) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var result = 0;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (IsHidden(ref platform, state, child, horizontal,
				selection.HiddenPriority)) continue;
			var minMax = ComputePlacementMinMax(ref platform, state, child);
			var minimum = horizontal ? minMax.MinWidth : minMax.MinHeight;
			result = AddExtent(result, minimum);
		}
		return result;
	}

	private static uint HorizontalWeightTotal<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count,
		MuiGroupDisappearSelection selection)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		uint total = 0;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (IsHidden(ref platform, state, child, true,
				selection.HiddenPriority)) continue;
			total += MuiAreaLayoutCore.HorizontalWeight(ref platform, state, child);
		}
		return total == 0 ? (uint)selection.VisibleCount : total;
	}

	private static uint VerticalWeightTotal<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count,
		MuiGroupDisappearSelection selection)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		uint total = 0;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (IsHidden(ref platform, state, child, false,
				selection.HiddenPriority)) continue;
			total += MuiAreaLayoutCore.VerticalWeight(ref platform, state, child);
		}
		return total == 0 ? (uint)selection.VisibleCount : total;
	}

	private static bool LayoutPage<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, int left, int top, int width, int height, int count)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!MuiGroupPageCore.TryResolveLayout(ref platform, state, group, count,
			left, top, width, height, out var selection)) return false;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			var active = index == selection.ActiveIndex &&
				selection.ActiveShown != 0;
			var childWidth = 0;
			var childHeight = 0;
			var childLeft = selection.Left;
			var childTop = selection.Top;
			if (active)
			{
				var minMax = MuiAreaLayoutCore.ComputeMinMax(ref platform, state,
					child);
				childWidth = PageExtent(minMax.MaxWidth, selection.Width);
				childHeight = PageExtent(minMax.MaxHeight, selection.Height);
				childLeft += (selection.Width - childWidth) / 2;
				childTop += (selection.Height - childHeight) / 2;
			}
			if (!MuiAreaLayoutCore.Layout(ref platform, state, child,
				childLeft, childTop, childWidth, childHeight)) return false;
		}
		return MuiAreaLayoutCore.Layout(ref platform, state, group,
			selection.Left, selection.Top, selection.Width, selection.Height);
	}

	private static int PageExtent(short maximum, int available)
	{
		if (available <= 0) return 0;
		var limit = (int)maximum;
		if (limit > 0 && limit < available) return limit;
		return limit < 0 ? 0 : available;
	}

	private static int ReadSpacing(MuiGroupLayoutPolicyStateRecord policy,
		bool horizontal)
	{
		var value = horizontal ? policy.HorizontalSpacing : policy.VerticalSpacing;
		return MuiGroupSpacingCore.ResolveForMinMax(value);
	}

	internal static bool TryGetLayoutState<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, out MuiGroupLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, group).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, group,
			LayoutPolicyStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, group,
			LayoutPolicyStateKey);
		return length == unchecked((int)MuiGroupLayoutPolicyStateRecord.Size) &&
			MuiGroupLayoutPolicyStateRecordCodec.TryReadStructural(ref platform, block,
				out value) && MuiGroupLayoutPolicyStateAdmission.ValidateLive(ref platform,
				state, group, value);
	}

	// Synchronize the named policy record from raw legacy attributes.  A
	// present record must already be a valid MorphOS policy struct; malformed
	// data is rejected before any layout getter or geometry consumer can use it.
	internal static bool TryReadLayoutPolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, out MuiGroupLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, group).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, group,
			LayoutPolicyStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, group,
			LayoutPolicyStateKey);
		var present = block.IsNotNull || length != 0;
		if (present)
		{
			if (length != unchecked((int)MuiGroupLayoutPolicyStateRecord.Size) ||
				!MuiGroupLayoutPolicyStateRecordCodec.TryReadStructural(ref platform, block,
					out var record) ||
				!MuiGroupLayoutPolicyStateAdmission.ValidateLive(ref platform, state,
					group, record)) return false;
			value = record;
			if (!TryFillLayoutPolicy(ref platform, state, group, ref value))
				return false;
			if (!MuiGroupLayoutPolicyStateAdmission.ValidateLive(ref platform, state,
				group, value)) return false;
			if (record.Horizontal != value.Horizontal ||
				record.HorizontalSpacing != value.HorizontalSpacing ||
				record.VerticalSpacing != value.VerticalSpacing ||
				record.SameWidth != value.SameWidth ||
				record.SameHeight != value.SameHeight ||
				record.PageMode != value.PageMode)
				return MuiGroupLayoutPolicyStateRecordCodec.Write(ref platform, block,
					value);
			return true;
		}

		value = default;
		value.Magic = MuiGroupLayoutPolicyStateRecord.Cookie;
		if (!TryFillLayoutPolicy(ref platform, state, group, ref value)) return false;
		if (!MuiGroupLayoutPolicyStateAdmission.ValidateLive(ref platform, state,
			group, value)) return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiGroupLayoutPolicyStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiGroupLayoutPolicyStateRecord.Size);
		var written = MuiGroupLayoutPolicyStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, group,
			LayoutPolicyStateKey, scratch,
			unchecked((int)MuiGroupLayoutPolicyStateRecord.Size));
		platform.Clear(scratch, MuiGroupLayoutPolicyStateRecord.Size);
		platform.Free(scratch, MuiGroupLayoutPolicyStateRecord.Size);
		return added;
	}

	private static bool TryFillLayoutPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, ref MuiGroupLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value.Horizontal = ReadAttribute(ref platform, state, group, Horizontal);
		value.HorizontalSpacing = ReadEffectiveSpacing(ref platform, state, group,
			HorizontalSpacing);
		value.VerticalSpacing = ReadEffectiveSpacing(ref platform, state, group,
			VerticalSpacing);
		value.SameWidth = ReadAttribute(ref platform, state, group, SameWidth);
		value.SameHeight = ReadAttribute(ref platform, state, group, SameHeight);
		// MorphOS defines SameSize as a shorthand that sets both component
		// policies. Keep the effective result in the existing named record so
		// ordinary Groups and Grid Groups share the same source of truth.
		var sameSize = ReadAttribute(ref platform, state, group, SameSize);
		if (sameSize > 1) return false;
		if (sameSize != 0)
		{
			value.SameWidth = 1;
			value.SameHeight = 1;
		}
		value.PageMode = ReadAttribute(ref platform, state, group, PageMode);
		return true;
	}

	private static uint ReadEffectiveSpacing<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, uint specific)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
			specific, out var value))
			return MuiGroupSpacingCore.NormalizeRaw(value);
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group, Spacing,
			out value);
		return MuiGroupSpacingCore.NormalizeRaw(value);
	}

	private static uint ReadAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, uint attribute)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group, attribute,
			out var value);
		return value;
	}

	private static short Larger(short left, short right) =>
		left > right ? left : right;

	private static short ClampDimension(int value)
	{
		if (value <= 0) return 0;
		return unchecked((short)(value > Maximum ? Maximum : value));
	}

	private static short SmallerMax(short left, short right)
	{
		if (left == 0) return right;
		if (right == 0) return left;
		return left < right ? left : right;
	}

	private static short Add(short value, int addition)
	{
		var result = (int)value + addition;
		return unchecked((short)(result > 10000 ? 10000 : result));
	}

	private static int CountChildren<TPlatform>(ref TPlatform platform, APTR state,
		APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = 0;
		while (count < 65535 && MuiFamilyCore.GetChild(ref platform, state, group,
			count, APTR.Null).IsNotNull) count++;
		return count;
	}
}
