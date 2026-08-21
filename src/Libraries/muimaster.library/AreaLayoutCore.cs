/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

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
	private const uint FrameVisible = 0x80426498;
	private const uint FramePhantomHoriz = 0x8042ED76;
	private const uint Background = 0x8042545B;
	private const uint FillArea = 0x804294A3;
	private const uint Font = 0x8042BE50;
	private const uint RenderInfo = 0x7FFF0001;
	private const uint IsSetup = 0x7FFF0002;
	private const uint IsShown = 0x7FFF0003;
	private const uint AreaGeometryStateKey = 0x7F070035;
	private const uint AreaLayoutPolicyStateKey = 0x7F070036;
	private const uint AreaRenderPolicyStateKey = 0x7F070037;

	public static bool Setup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR renderInfo) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (renderInfo.IsNull || !platform.IsMapped(renderInfo, 28)) return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			RenderInfo, renderInfo.Raw, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, IsSetup, 1,
				false);
	}

	public static bool Cleanup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiLayoutPlatform =>
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, IsShown, 0,
			false) && MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			IsSetup, 0, false) && MuiHeadlessObjectCore.SetAttribute(ref platform,
			state, obj, RenderInfo, 0, false);

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
		geometry.Right = left + width - 1;
		geometry.Bottom = top + height - 1;
		return PublishGeometryState(ref platform, state, obj, geometry);
	}

	public static bool Draw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint flags) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!Attribute(ref platform, state, obj, RenderInfo, 0,
			out var renderInfoRaw) || renderInfoRaw == 0) return false;
		var renderInfo = APTR.FromPointer(renderInfoRaw);
		if (!MuiDrawingRenderInfoCodec.TryRead(ref platform, renderInfo,
			out var renderInfoRecord)) return false;
		var rastPort = renderInfoRecord.RastPort;
		if (rastPort.IsNull) return false;
		if (!TryReadGeometryState(ref platform, state, obj,
			out var geometry)) return false;
		var left = geometry.Left;
		var top = geometry.Top;
		var width = geometry.Width;
		var height = geometry.Height;
		if (width <= 0 || height <= 0 || !platform.LockLayer(rastPort)) return false;
		if (!platform.BeginUpdate(rastPort))
		{
			platform.UnlockLayer(rastPort);
			return false;
		}
		var clip = platform.PushClip(rastPort, left, top, width, height);
		var renderPolicy = ReadRenderPolicy(ref platform, state, obj);
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
		}
		platform.PopClip(rastPort, clip);
		platform.EndUpdate(rastPort, true);
		platform.UnlockLayer(rastPort);
		return true;
	}

	public static bool DrawBackground<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int left, int top, int width, int height)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (width <= 0 || height <= 0 ||
			!GetRenderPort(ref platform, state, obj, out var rastPort)) return false;
		var renderPolicy = ReadRenderPolicy(ref platform, state, obj);
		platform.SetPen(rastPort, renderPolicy.Background);
		platform.FillRectangle(rastPort, left, top, left + width - 1,
			top + height - 1);
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
	{
		if (text.IsNull || length < 0 ||
			!GetRenderPort(ref platform, state, obj, out var rastPort)) return 0;
		var font = APTR.FromPointer(ReadRenderPolicy(ref platform, state, obj).Font);
		var width = platform.TextWidth(rastPort, font, text, length);
		var height = platform.TextHeight(rastPort, font);
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
	{
		if (text.IsNull || length < 0) return false;
		if (!GetRenderPort(ref platform, state, obj, out var rastPort)) return false;
		var font = APTR.FromPointer(ReadRenderPolicy(ref platform, state, obj).Font);
		var textHeight = platform.TextHeight(rastPort, font);
		var baseline = top + (height - textHeight) / 2 + textHeight;
		platform.DrawText(rastPort, font, left, baseline, text, length);
		return true;
	}

	internal static MuiMinMaxValues ComputeMinMax<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var policy = ReadLayoutPolicy(ref platform, state, obj);
		if (MuiCommonControlCore.TryComputeMinMax(ref platform, state, obj,
			out var commonValues)) return commonValues;
		MuiMinMaxValues result = default;
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
		return ReadLayoutPolicy(ref platform, state, obj).HorizontalWeight;
	}

	internal static uint VerticalWeight<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		return ReadLayoutPolicy(ref platform, state, obj).VerticalWeight;
	}

	internal static bool TryGetRenderPolicyState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaRenderPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AreaRenderPolicyStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			AreaRenderPolicyStateKey) !=
			unchecked((int)MuiAreaRenderPolicyStateRecord.Size)) return false;
		return MuiAreaRenderPolicyStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	// Synchronize the named render-policy record from raw legacy attributes
	// without entering the public getter path. Common-control Get and OM_GET
	// use this boundary for the FillArea projection.
	internal static bool TryReadRenderPolicyState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaRenderPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		PublishRenderPolicy(ref platform, state, obj, out value);

	private static MuiAreaRenderPolicyStateRecord ReadRenderPolicy<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishRenderPolicy(ref platform, state, obj, out var value))
			return value;
		value = default;
		value.Magic = MuiAreaRenderPolicyStateRecord.Cookie;
		FillRenderPolicy(ref platform, state, obj, ref value);
		return value;
	}

	private static bool PublishRenderPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaRenderPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AreaRenderPolicyStateKey);
		if (TryGetRenderPolicyState(ref platform, state, obj, out value))
		{
			FillRenderPolicy(ref platform, state, obj, ref value);
			return MuiAreaRenderPolicyStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiAreaRenderPolicyStateRecord.Cookie;
		FillRenderPolicy(ref platform, state, obj, ref value);
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
		ReadRawRenderPolicy(ref platform, state, obj, Font, 0, out value.Font);
		ReadRawRenderPolicy(ref platform, state, obj, FrameVisible, 1,
			out value.FrameVisible);
		ReadRawRenderPolicy(ref platform, state, obj, FramePhantomHoriz, 0,
			out value.FramePhantomHoriz);
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
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AreaLayoutPolicyStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			AreaLayoutPolicyStateKey) !=
			unchecked((int)MuiAreaLayoutPolicyStateRecord.Size)) return false;
		return MuiAreaLayoutPolicyStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	// Synchronize the named policy record from raw legacy attributes without
	// entering the public getter path. Common-control Get and OM_GET use this
	// boundary so layout policy remains a struct-defined guest projection.
	internal static bool TryReadLayoutPolicyState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		PublishLayoutPolicy(ref platform, state, obj, out value);

	private static MuiAreaLayoutPolicyStateRecord ReadLayoutPolicy<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishLayoutPolicy(ref platform, state, obj, out var value))
			return value;
		value = default;
		value.Magic = MuiAreaLayoutPolicyStateRecord.Cookie;
		FillLayoutPolicy(ref platform, state, obj, ref value);
		return value;
	}

	private static bool PublishLayoutPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AreaLayoutPolicyStateKey);
		if (TryGetLayoutPolicyState(ref platform, state, obj, out value))
		{
			FillLayoutPolicy(ref platform, state, obj, ref value);
			return MuiAreaLayoutPolicyStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiAreaLayoutPolicyStateRecord.Cookie;
		FillLayoutPolicy(ref platform, state, obj, ref value);
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
		rastPort = APTR.Null;
		if (!Attribute(ref platform, state, obj, RenderInfo, 0, out var raw) ||
			raw == 0) return false;
		var renderInfo = APTR.FromPointer(raw);
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
		var left = 0u;
		var top = 0u;
		var width = 0u;
		var height = 0u;
		var right = 0u;
		var bottom = 0u;
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, LeftEdge,
			out left);
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, TopEdge,
			out top);
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, Width,
			out width);
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, Height,
			out height);
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, RightEdge,
			out right);
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, BottomEdge,
			out bottom);
		if (TryReadGeometryStateRecord(ref platform, state, obj, out var record))
		{
			if (record.Left != unchecked((int)left) ||
				record.Top != unchecked((int)top) ||
				record.Width != unchecked((int)width) ||
				record.Height != unchecked((int)height) ||
				record.Right != unchecked((int)right) ||
				record.Bottom != unchecked((int)bottom))
			{
				record.Left = unchecked((int)left);
				record.Top = unchecked((int)top);
				record.Width = unchecked((int)width);
				record.Height = unchecked((int)height);
				record.Right = unchecked((int)right);
				record.Bottom = unchecked((int)bottom);
				var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					AreaGeometryStateKey);
				if (!MuiAreaGeometryStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
		}
		result.Left = unchecked((int)left);
		result.Top = unchecked((int)top);
		result.Width = unchecked((int)width);
		result.Height = unchecked((int)height);
		result.Right = unchecked((int)right);
		result.Bottom = unchecked((int)bottom);
		return true;
	}

	private static bool TryReadGeometryStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiAreaGeometryStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AreaGeometryStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			AreaGeometryStateKey) != unchecked((int)MuiAreaGeometryStateRecord.Size))
			return false;
		return MuiAreaGeometryStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureGeometryStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadGeometryStateRecord(ref platform, state, obj, out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaGeometryStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaGeometryStateRecord.Size);
		var value = default(MuiAreaGeometryStateRecord);
		value.Magic = MuiAreaGeometryStateRecord.Cookie;
		TryReadGeometryState(ref platform, state, obj, out var geometry);
		value.Left = geometry.Left;
		value.Top = geometry.Top;
		value.Width = geometry.Width;
		value.Height = geometry.Height;
		value.Right = geometry.Right;
		value.Bottom = geometry.Bottom;
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
		if (!EnsureGeometryStateRecord(ref platform, state, obj)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AreaGeometryStateKey);
		var stored = default(MuiAreaGeometryStateRecord);
		stored.Magic = MuiAreaGeometryStateRecord.Cookie;
		stored.Left = value.Left;
		stored.Top = value.Top;
		stored.Width = value.Width;
		stored.Height = value.Height;
		stored.Right = value.Right;
		stored.Bottom = value.Bottom;
		if (!MuiAreaGeometryStateRecordCodec.Write(ref platform, block, stored))
			return false;
		return Set(ref platform, state, obj, LeftEdge, stored.Left) &&
			Set(ref platform, state, obj, TopEdge, stored.Top) &&
			Set(ref platform, state, obj, Width, stored.Width) &&
			Set(ref platform, state, obj, Height, stored.Height) &&
			Set(ref platform, state, obj, RightEdge, stored.Right) &&
			Set(ref platform, state, obj, BottomEdge, stored.Bottom);
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
	private const uint PageMode = 0x80421A5F;
	private const uint LayoutPolicyStateKey = 0x0D100013u;

	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == Horizontal || attribute == HorizontalSpacing ||
		attribute == VerticalSpacing || attribute == Spacing ||
		attribute == SameWidth || attribute == SameHeight ||
		attribute == PageMode;

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
				attribute == SameHeight ? policy.SameHeight : policy.PageMode;
			return true;
		}
		var hasSource = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			group, attribute, out _);
		if (!hasSource && (attribute == HorizontalSpacing ||
			attribute == VerticalSpacing) &&
			!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
				Spacing, out _)) return false;
		if (!hasSource && attribute == Spacing &&
			!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
				Spacing, out _)) return false;
		if (!hasSource && attribute != HorizontalSpacing &&
			attribute != VerticalSpacing && attribute != Spacing) return false;
		var resolved = ReadLayoutPolicy(ref platform, state, group);
		value = attribute == Horizontal ? resolved.Horizontal :
			attribute == HorizontalSpacing ? resolved.HorizontalSpacing :
			attribute == VerticalSpacing ? resolved.VerticalSpacing :
			attribute == Spacing ? resolved.HorizontalSpacing :
			attribute == SameWidth ? resolved.SameWidth :
			attribute == SameHeight ? resolved.SameHeight : resolved.PageMode;
		return true;
	}

	public static bool AskMinMax<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, APTR storage) where TPlatform : struct, IMuiLayoutPlatform
	{
		PublishLayoutPolicy(ref platform, state, group, out _);
		if (MuiGroupLayoutHookCore.IsInstalled(ref platform, state, group))
		{
			if (!MuiGroupLayoutHookCore.InvokeMinMax(ref platform, state, group,
				out var hooked)) return false;
			return MuiAreaLayoutCore.WriteMinMax(ref platform, storage, hooked);
		}
		return MuiAreaLayoutCore.WriteMinMax(ref platform, storage,
			ComputeMinMax(ref platform, state, group));
	}

	internal static MuiMinMaxValues ComputeMinMax<TPlatform>(ref TPlatform platform,
		APTR state, APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var policy = ReadLayoutPolicy(ref platform, state, group);
		if (MuiGroupLayoutHookCore.IsInstalled(ref platform, state, group))
		{
			return MuiGroupLayoutHookCore.InvokeMinMax(ref platform, state, group,
				out var hooked) ? hooked : default;
		}
		MuiMinMaxValues result = default;
		var horizontal = policy.Horizontal;
		var pageMode = policy.PageMode;
		var count = CountChildren(ref platform, state, group);
		var grid = MuiGroupGridCore.Read(ref platform, state, group);
		if (MuiGroupGridCore.IsEnabled(grid, count))
			return MuiGroupGridCore.ComputeMinMax(ref platform, state, group,
				grid, count);
		var spacing = ReadSpacing(policy, horizontal != 0);
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			var item = MuiAreaLayoutCore.ComputeMinMax(ref platform, state, child);
			if (pageMode != 0)
			{
				result.MinWidth = Larger(result.MinWidth, item.MinWidth);
				result.MinHeight = Larger(result.MinHeight, item.MinHeight);
				result.MaxWidth = Larger(result.MaxWidth, item.MaxWidth);
				result.MaxHeight = Larger(result.MaxHeight, item.MaxHeight);
				result.DefWidth = Larger(result.DefWidth, item.DefWidth);
				result.DefHeight = Larger(result.DefHeight, item.DefHeight);
			}
			else if (horizontal != 0)
			{
				result.MinWidth = Add(result.MinWidth, item.MinWidth);
				result.MinHeight = Larger(result.MinHeight, item.MinHeight);
				result.MaxWidth = Add(result.MaxWidth, item.MaxWidth);
				result.MaxHeight = Larger(result.MaxHeight, item.MaxHeight);
				result.DefWidth = Add(result.DefWidth, item.DefWidth);
				result.DefHeight = Larger(result.DefHeight, item.DefHeight);
			}
			else
			{
				result.MinWidth = Larger(result.MinWidth, item.MinWidth);
				result.MinHeight = Add(result.MinHeight, item.MinHeight);
				result.MaxWidth = Larger(result.MaxWidth, item.MaxWidth);
				result.MaxHeight = Add(result.MaxHeight, item.MaxHeight);
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
				result.MaxWidth = Add(result.MaxWidth, gaps);
				result.DefWidth = Add(result.DefWidth, gaps);
			}
			else
			{
				result.MinHeight = Add(result.MinHeight, gaps);
				result.MaxHeight = Add(result.MaxHeight, gaps);
				result.DefHeight = Add(result.DefHeight, gaps);
			}
		}
		return result;
	}

	public static bool Layout<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, int left, int top, int width, int height)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var policy = ReadLayoutPolicy(ref platform, state, group);
		if (MuiGroupLayoutHookCore.IsInstalled(ref platform, state, group))
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
		if (count == 0) return MuiAreaLayoutCore.Layout(ref platform, state, group,
			left, top, width, height);
		var grid = MuiGroupGridCore.Read(ref platform, state, group);
		if (MuiGroupGridCore.IsEnabled(grid, count))
			return MuiGroupGridCore.Layout(ref platform, state, group, left, top,
				width, height, grid, count);
		var pageMode = policy.PageMode;
		if (pageMode != 0)
			return LayoutPage(ref platform, state, group, left, top, width, height,
				count);
		if (horizontal != 0)
			return LayoutHorizontal(ref platform, state, group, left, top, width,
				height, count, ReadSpacing(policy, true), policy);
		return LayoutVertical(ref platform, state, group, left, top, width, height,
			count, ReadSpacing(policy, false), policy);
	}

	private static bool LayoutHorizontal<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int left, int top, int width, int height, int count,
		int spacing, MuiGroupLayoutPolicyStateRecord policy)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var available = width - spacing * (count - 1);
		if (available < 0) available = 0;
		var equal = policy.SameWidth;
		var total = HorizontalWeightTotal(ref platform, state, group, count);
		var cursor = left;
		var remaining = available;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			var weight = MuiAreaLayoutCore.HorizontalWeight(ref platform, state,
				child);
			int extent;
			if (index == count - 1) extent = remaining;
			else if (equal != 0) extent = available / count;
			else
			{
				var normalizedWeight = weight == 0 ? 1u : weight;
				extent = (int)((uint)available * normalizedWeight / total);
				if (extent > remaining) extent = remaining;
			}
			if (!MuiAreaLayoutCore.Layout(ref platform, state, child, cursor, top,
				extent, height)) return false;
			cursor += extent + spacing;
			remaining -= extent;
		}
		return MuiAreaLayoutCore.Layout(ref platform, state, group, left, top,
			width, height);
	}

	private static bool LayoutVertical<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int left, int top, int width, int height, int count,
		int spacing, MuiGroupLayoutPolicyStateRecord policy)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var available = height - spacing * (count - 1);
		if (available < 0) available = 0;
		var equal = policy.SameHeight;
		var total = VerticalWeightTotal(ref platform, state, group, count);
		var cursor = top;
		var remaining = available;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			var weight = MuiAreaLayoutCore.VerticalWeight(ref platform, state, child);
			int extent;
			if (index == count - 1) extent = remaining;
			else if (equal != 0) extent = available / count;
			else
			{
				var normalizedWeight = weight == 0 ? 1u : weight;
				extent = (int)((uint)available * normalizedWeight / total);
				if (extent > remaining) extent = remaining;
			}
			if (!MuiAreaLayoutCore.Layout(ref platform, state, child, left, cursor,
				width, extent)) return false;
			cursor += extent + spacing;
			remaining -= extent;
		}
		return MuiAreaLayoutCore.Layout(ref platform, state, group, left, top,
			width, height);
	}

	private static uint HorizontalWeightTotal<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		uint total = 0;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			total += MuiAreaLayoutCore.HorizontalWeight(ref platform, state, child);
		}
		return total == 0 ? (uint)count : total;
	}

	private static uint VerticalWeightTotal<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, int count)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		uint total = 0;
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			total += MuiAreaLayoutCore.VerticalWeight(ref platform, state, child);
		}
		return total == 0 ? (uint)count : total;
	}

	private static bool LayoutPage<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, int left, int top, int width, int height, int count)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var active = (int)MuiGroupPageCore.ReadActivePage(ref platform, state,
			group, (uint)count);
		for (var index = 0; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group, index,
				APTR.Null);
			if (!MuiAreaLayoutCore.Layout(ref platform, state, child, left, top,
				index == active ? width : 0, index == active ? height : 0)) return false;
		}
		return MuiAreaLayoutCore.Layout(ref platform, state, group, left, top,
			width, height);
	}

	private static int ReadSpacing(MuiGroupLayoutPolicyStateRecord policy,
		bool horizontal)
	{
		var value = horizontal ? policy.HorizontalSpacing : policy.VerticalSpacing;
		var result = unchecked((int)value);
		return result < 0 ? 0 : result;
	}

	internal static bool TryGetLayoutState<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, out MuiGroupLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, group,
			LayoutPolicyStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, group,
			LayoutPolicyStateKey) !=
			unchecked((int)MuiGroupLayoutPolicyStateRecord.Size)) return false;
		return MuiGroupLayoutPolicyStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static MuiGroupLayoutPolicyStateRecord ReadLayoutPolicy<TPlatform>(
		ref TPlatform platform, APTR state, APTR group)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishLayoutPolicy(ref platform, state, group, out var value))
			return value;
		value = default;
		value.Magic = MuiGroupLayoutPolicyStateRecord.Cookie;
		FillLayoutPolicy(ref platform, state, group, ref value);
		return value;
	}

	private static bool PublishLayoutPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, out MuiGroupLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, group,
			LayoutPolicyStateKey);
		if (TryGetLayoutState(ref platform, state, group, out value))
		{
			FillLayoutPolicy(ref platform, state, group, ref value);
			return MuiGroupLayoutPolicyStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiGroupLayoutPolicyStateRecord.Cookie;
		FillLayoutPolicy(ref platform, state, group, ref value);
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

	private static void FillLayoutPolicy<TPlatform>(ref TPlatform platform,
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
		value.PageMode = ReadAttribute(ref platform, state, group, PageMode);
	}

	private static uint ReadEffectiveSpacing<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, uint specific)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
			specific, out var value)) return value;
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group, Spacing,
			out value);
		return value;
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
