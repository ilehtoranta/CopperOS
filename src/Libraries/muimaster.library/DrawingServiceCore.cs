/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Fixed guest-memory layout for the MG09 drawing-service gateway
// (MUI_AddClipping/RemoveClipping, MUI_AddClipRegion/RemoveClipRegion,
// MUI_BeginRefresh/EndRefresh and MUI_ObtainPen/ReleasePen/GetRGBColor).
//
// The service owns a dedicated, initialized guest-resident state block plus
// three singly linked, strictly last-in/first-out record lists (clipping,
// refresh and pen acquisitions). It never allocates on the managed heap, holds
// no managed data, and interprets neither the black-box MUI_PenSpec nor the
// generated MUI_RGBColor; those cross the narrow MG09 pen capability untouched.
internal static class MuiDrawingServiceLayout
{
	public const uint Magic = 0x4D554944;   // "MUID"
	public const uint Version = 1;

	public const uint ClipKindRectangle = 1;
	public const uint ClipKindRegion = 2;

	// MUIMRI_RefreshMode (1<<3): set on mri_Flags while a refresh is active.
	public const uint RefreshModeFlag = 1u << 3;

	// struct MUI_PenSpec is an explicit black box of exactly 32 bytes.
	public const uint PenSpecSize = 32;

	// struct MUI_RGBColor (authoritative generated SDK): three ULONG components.
	public const uint RgbColorSize = 12;

	// MUIPEN() mask. Present for documentation and the "never mask a release"
	// test; the production release path deliberately never applies it.
	public const uint PenMask = 0xffff;

	public const uint MaximumTraversal = 65535;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDrawingServiceStateRecord
{
	internal const uint Size = 20;
	internal uint Magic;
	internal APTR ClipHead;
	internal APTR RefreshHead;
	internal APTR PenHead;
	internal uint Generation;
}

internal static class MuiDrawingServiceStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiDrawingServiceStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiDrawingServiceStateRecord.Size) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.State, MuiDrawingRecordField.Magic,
				out record.Magic) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.State, MuiDrawingRecordField.ClipHead,
				out var clipHead) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.State, MuiDrawingRecordField.RefreshHead,
				out var refreshHead) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.State, MuiDrawingRecordField.PenHead,
				out var penHead) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.State, MuiDrawingRecordField.Generation,
				out record.Generation)) return false;
		record.ClipHead = APTR.FromPointer(clipHead);
		record.RefreshHead = APTR.FromPointer(refreshHead);
		record.PenHead = APTR.FromPointer(penHead);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiDrawingServiceStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiDrawingServiceStateRecord.Size)) return false;
		return MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiDrawingRecordKind.State, MuiDrawingRecordField.Magic, record.Magic) &&
			MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiDrawingRecordKind.State, MuiDrawingRecordField.ClipHead,
				record.ClipHead.Raw) &&
			MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiDrawingRecordKind.State, MuiDrawingRecordField.RefreshHead,
				record.RefreshHead.Raw) &&
			MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiDrawingRecordKind.State, MuiDrawingRecordField.PenHead,
				record.PenHead.Raw) &&
			MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiDrawingRecordKind.State, MuiDrawingRecordField.Generation,
				record.Generation);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDrawingClipRecord
{
	internal const uint Size = 16;
	internal APTR Next;
	internal uint Kind;
	internal APTR Layer;
	internal APTR Token;
}

internal static class MuiDrawingClipCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiDrawingClipRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiDrawingClipRecord.Size)) return false;
		if (!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiDrawingRecordKind.Clip, MuiDrawingRecordField.Next, out var next) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.Clip, MuiDrawingRecordField.Kind,
				out record.Kind) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.Clip, MuiDrawingRecordField.Layer,
				out var layer) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.Clip, MuiDrawingRecordField.Token,
				out var token)) return false;
		record.Next = APTR.FromPointer(next);
		record.Layer = APTR.FromPointer(layer);
		record.Token = APTR.FromPointer(token);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiDrawingClipRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiDrawingClipRecord.Size)) return false;
		return MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiDrawingRecordKind.Clip, MuiDrawingRecordField.Next, record.Next.Raw) &&
			MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiDrawingRecordKind.Clip, MuiDrawingRecordField.Kind, record.Kind) &&
			MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiDrawingRecordKind.Clip, MuiDrawingRecordField.Layer,
				record.Layer.Raw) &&
			MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiDrawingRecordKind.Clip, MuiDrawingRecordField.Token,
				record.Token.Raw);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDrawingRefreshRecord
{
	internal const uint Size = 16;
	internal APTR Next;
	internal APTR RenderInfo;
	internal APTR Layer;
	internal uint SavedFlags;
}

internal static class MuiDrawingRefreshCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiDrawingRefreshRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiDrawingRefreshRecord.Size)) return false;
		if (!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiDrawingRecordKind.Refresh, MuiDrawingRecordField.Next,
			out var next) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.Refresh, MuiDrawingRecordField.RenderInfo,
				out var renderInfo) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.Refresh, MuiDrawingRecordField.Layer,
				out var layer) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.Refresh, MuiDrawingRecordField.SavedFlags,
				out record.SavedFlags)) return false;
		record.Next = APTR.FromPointer(next);
		record.RenderInfo = APTR.FromPointer(renderInfo);
		record.Layer = APTR.FromPointer(layer);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiDrawingRefreshRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiDrawingRefreshRecord.Size)) return false;
		return MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiDrawingRecordKind.Refresh, MuiDrawingRecordField.Next,
			record.Next.Raw) &&
			MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiDrawingRecordKind.Refresh, MuiDrawingRecordField.RenderInfo,
				record.RenderInfo.Raw) &&
			MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiDrawingRecordKind.Refresh, MuiDrawingRecordField.Layer,
				record.Layer.Raw) &&
			MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiDrawingRecordKind.Refresh, MuiDrawingRecordField.SavedFlags,
				record.SavedFlags);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDrawingPenRecord
{
	internal const uint Size = 12;
	internal APTR Next;
	internal APTR RenderInfo;
	internal uint Token;
}

internal static class MuiDrawingPenCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiDrawingPenRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiDrawingPenRecord.Size)) return false;
		if (!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiDrawingRecordKind.Pen, MuiDrawingRecordField.Next, out var next) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.Pen, MuiDrawingRecordField.RenderInfo,
				out var renderInfo) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.Pen, MuiDrawingRecordField.Token,
				out record.Token)) return false;
		record.Next = APTR.FromPointer(next);
		record.RenderInfo = APTR.FromPointer(renderInfo);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiDrawingPenRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiDrawingPenRecord.Size)) return false;
		return MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiDrawingRecordKind.Pen, MuiDrawingRecordField.Next, record.Next.Raw) &&
			MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiDrawingRecordKind.Pen, MuiDrawingRecordField.RenderInfo,
				record.RenderInfo.Raw) &&
			MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiDrawingRecordKind.Pen, MuiDrawingRecordField.Token,
				record.Token);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDrawingRenderInfoRecord
{
	internal const uint Size = 28;
	internal APTR WindowObject;
	internal APTR Screen;
	internal APTR DrawInfo;
	internal APTR Pens;
	internal APTR Window;
	internal APTR RastPort;
	internal uint Flags;
}

internal static class MuiDrawingRenderInfoCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiDrawingRenderInfoRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiDrawingRenderInfoRecord.Size)) return false;
		if (!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiDrawingRecordKind.RenderInfo, MuiDrawingRecordField.WindowObject,
			out var windowObject) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.RenderInfo, MuiDrawingRecordField.Screen,
				out var screen) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.RenderInfo, MuiDrawingRecordField.DrawInfo,
				out var drawInfo) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.RenderInfo, MuiDrawingRecordField.Pens,
				out var pens) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.RenderInfo, MuiDrawingRecordField.Window,
				out var window) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.RenderInfo, MuiDrawingRecordField.RastPort,
				out var rastPort) ||
			!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiDrawingRecordKind.RenderInfo, MuiDrawingRecordField.Flags,
				out record.Flags)) return false;
		record.WindowObject = APTR.FromPointer(windowObject);
		record.Screen = APTR.FromPointer(screen);
		record.DrawInfo = APTR.FromPointer(drawInfo);
		record.Pens = APTR.FromPointer(pens);
		record.Window = APTR.FromPointer(window);
		record.RastPort = APTR.FromPointer(rastPort);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiDrawingRenderInfoRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiDrawingRenderInfoRecord.Size)) return false;
		return MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiDrawingRecordKind.RenderInfo, MuiDrawingRecordField.WindowObject,
			record.WindowObject.Raw) &&
			MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiDrawingRecordKind.RenderInfo, MuiDrawingRecordField.Screen,
				record.Screen.Raw) &&
			MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiDrawingRecordKind.RenderInfo, MuiDrawingRecordField.DrawInfo,
				record.DrawInfo.Raw) &&
			MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiDrawingRecordKind.RenderInfo, MuiDrawingRecordField.Pens,
				record.Pens.Raw) &&
			MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiDrawingRecordKind.RenderInfo, MuiDrawingRecordField.Window,
				record.Window.Raw) &&
			MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiDrawingRecordKind.RenderInfo, MuiDrawingRecordField.RastPort,
				record.RastPort.Raw) &&
			MuiDrawingRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiDrawingRecordKind.RenderInfo, MuiDrawingRecordField.Flags,
				record.Flags);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDrawingRasterPortRecord
{
	internal const uint Size = 4;
	internal APTR Layer;
}

internal enum MuiDrawingRecordKind : byte
{
	State,
	Clip,
	Refresh,
	Pen,
	RenderInfo,
	RasterPort,
}

internal enum MuiDrawingRecordField : byte
{
	Magic,
	ClipHead,
	RefreshHead,
	PenHead,
	Generation,
	Next,
	Kind,
	Layer,
	Token,
	RenderInfo,
	SavedFlags,
	WindowObject,
	Screen,
	DrawInfo,
	Pens,
	Window,
	RastPort,
	Flags,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDrawingRecordFieldCursor
{
	internal APTR Address;
	internal MuiDrawingRecordKind Record;
	internal MuiDrawingRecordField Field;
}

internal static class MuiDrawingRecordFieldCursorCodec
{
	private static bool TryResolve(MuiDrawingRecordKind record,
		MuiDrawingRecordField field, out uint offset, out uint size)
	{
		offset = 0;
		size = 0;
		switch (record)
		{
			case MuiDrawingRecordKind.State:
				size = MuiDrawingServiceStateRecord.Size;
				offset = field switch
				{
					MuiDrawingRecordField.Magic => 0,
					MuiDrawingRecordField.ClipHead => 4,
					MuiDrawingRecordField.RefreshHead => 8,
					MuiDrawingRecordField.PenHead => 12,
					MuiDrawingRecordField.Generation => 16,
					_ => uint.MaxValue,
				};
				break;
			case MuiDrawingRecordKind.Clip:
				size = MuiDrawingClipRecord.Size;
				offset = field switch
				{
					MuiDrawingRecordField.Next => 0,
					MuiDrawingRecordField.Kind => 4,
					MuiDrawingRecordField.Layer => 8,
					MuiDrawingRecordField.Token => 12,
					_ => uint.MaxValue,
				};
				break;
			case MuiDrawingRecordKind.Refresh:
				size = MuiDrawingRefreshRecord.Size;
				offset = field switch
				{
					MuiDrawingRecordField.Next => 0,
					MuiDrawingRecordField.RenderInfo => 4,
					MuiDrawingRecordField.Layer => 8,
					MuiDrawingRecordField.SavedFlags => 12,
					_ => uint.MaxValue,
				};
				break;
			case MuiDrawingRecordKind.Pen:
				size = MuiDrawingPenRecord.Size;
				offset = field switch
				{
					MuiDrawingRecordField.Next => 0,
					MuiDrawingRecordField.RenderInfo => 4,
					MuiDrawingRecordField.Token => 8,
					_ => uint.MaxValue,
				};
				break;
			case MuiDrawingRecordKind.RenderInfo:
				size = MuiDrawingRenderInfoRecord.Size;
				offset = field switch
				{
					MuiDrawingRecordField.WindowObject => 0,
					MuiDrawingRecordField.Screen => 4,
					MuiDrawingRecordField.DrawInfo => 8,
					MuiDrawingRecordField.Pens => 12,
					MuiDrawingRecordField.Window => 16,
					MuiDrawingRecordField.RastPort => 20,
					MuiDrawingRecordField.Flags => 24,
					_ => uint.MaxValue,
				};
				break;
			case MuiDrawingRecordKind.RasterPort:
				size = MuiDrawingRasterPortRecord.Size;
				offset = field == MuiDrawingRecordField.Layer ? 0 : uint.MaxValue;
				break;
		}
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiDrawingRecordFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Record, cursor.Field, out var offset,
			out var size) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, size)) return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiDrawingRecordKind record, MuiDrawingRecordField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiDrawingRecordFieldCursor);
		cursor.Address = address;
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiDrawingRecordKind record, MuiDrawingRecordField field,
		uint value) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiDrawingRecordFieldCursor);
		cursor.Address = address;
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiDrawingRasterPortCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiDrawingRasterPortRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiDrawingRasterPortRecord.Size)) return false;
		if (!MuiDrawingRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiDrawingRecordKind.RasterPort, MuiDrawingRecordField.Layer,
			out var layer)) return false;
		record.Layer = APTR.FromPointer(layer);
		return true;
	}
}

// Scalar qualification surface for the drawing service's state and owned
// LIFO records. Public rendering structures remain opaque to this seam.
public static class MuiDrawingServiceRecordPacketCore
{
	public static bool WriteState<TPlatform>(ref TPlatform platform, APTR address,
		uint magic, APTR clipHead, APTR refreshHead, APTR penHead,
		uint generation) where TPlatform : struct, IMuiGuestMemory
	{
		MuiDrawingServiceStateRecord record = default;
		record.Magic = magic;
		record.ClipHead = clipHead;
		record.RefreshHead = refreshHead;
		record.PenHead = penHead;
		record.Generation = generation;
		return MuiDrawingServiceStateCodec.Write(ref platform, address, record);
	}

	public static uint DispatchState<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiDrawingServiceStateCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Magic ^ record.ClipHead.Raw ^ record.RefreshHead.Raw ^
			record.PenHead.Raw ^ record.Generation;
	}

	public static bool WriteClip<TPlatform>(ref TPlatform platform, APTR address,
		APTR next, uint kind, APTR layer, APTR token)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiDrawingClipRecord record = default;
		record.Next = next;
		record.Kind = kind;
		record.Layer = layer;
		record.Token = token;
		return MuiDrawingClipCodec.Write(ref platform, address, record);
	}

	public static uint DispatchClip<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiDrawingClipCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Next.Raw ^ record.Kind ^ record.Layer.Raw ^ record.Token.Raw;
	}

	public static bool WriteRefresh<TPlatform>(ref TPlatform platform,
		APTR address, APTR next, APTR renderInfo, APTR layer, uint savedFlags)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiDrawingRefreshRecord record = default;
		record.Next = next;
		record.RenderInfo = renderInfo;
		record.Layer = layer;
		record.SavedFlags = savedFlags;
		return MuiDrawingRefreshCodec.Write(ref platform, address, record);
	}

	public static uint DispatchRefresh<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiDrawingRefreshCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Next.Raw ^ record.RenderInfo.Raw ^ record.Layer.Raw ^
			record.SavedFlags;
	}

	public static bool WritePen<TPlatform>(ref TPlatform platform, APTR address,
		APTR next, APTR renderInfo, uint token)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiDrawingPenRecord record = default;
		record.Next = next;
		record.RenderInfo = renderInfo;
		record.Token = token;
		return MuiDrawingPenCodec.Write(ref platform, address, record);
	}

	public static uint DispatchPen<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiDrawingPenCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Next.Raw ^ record.RenderInfo.Raw ^ record.Token;
	}
}

// The MG09 drawing-service gateway. Implements the documented behaviour of the
// public clipping, refresh and pen entry points over guest-resident state with
// no managed allocations or runtime dependencies. Rectangle clipping and
// refresh route the existing frozen IMuiLayersCapability (PushClip/PopClip and
// BeginUpdate/EndUpdate); clip regions and pens route the MG09-only region
// and pen capabilities. The frozen cores, dispatchers and aggregate platform
// interfaces are not modified.
public static class MuiDrawingServiceCore
{
	// Initialize the dedicated service state. Idempotent: an already-initialized
	// state is preserved so outstanding clips, refreshes and pens are not
	// dropped.
	public static bool Initialize<TPlatform>(ref TPlatform platform,
		APTR serviceState) where TPlatform : struct, IMuiServicePlatform
	{
		if (serviceState.IsNull ||
			!platform.IsMapped(serviceState, MuiDrawingServiceStateRecord.Size))
			return false;
		if (MuiDrawingServiceStateCodec.TryRead(ref platform, serviceState,
			out var current) && current.Magic == MuiDrawingServiceLayout.Magic &&
			current.Generation == MuiDrawingServiceLayout.Version) return true;
		platform.Clear(serviceState, MuiDrawingServiceStateRecord.Size);
		MuiDrawingServiceStateRecord record = default;
		record.Magic = MuiDrawingServiceLayout.Magic;
		record.Generation = MuiDrawingServiceLayout.Version;
		return MuiDrawingServiceStateCodec.Write(ref platform, serviceState, record);
	}

	// ---- Clipping ------------------------------------------------------------

	// MUI_AddClipping(mri, left, top, width, height). Installs a rectangle clip
	// on the render info's layer through the frozen PushClip seam and pushes an
	// opaque guest-resident handle onto the unified LIFO clip stack. Returns the
	// handle, or Null on a malformed render info / allocation failure (atomic).
	public static APTR AddClipping<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR renderInfo, int left, int top, int width,
		int height) where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState)) return APTR.Null;
		var layer = Layer(ref platform, renderInfo);
		if (layer.IsNull) return APTR.Null;
		var previous = platform.PushClip(layer, left, top, width, height);
		var record = MuiHeadlessMemory.Allocate(ref platform,
			MuiDrawingClipRecord.Size);
		if (record.IsNull)
		{
			platform.PopClip(layer, previous);   // atomic rollback
			return APTR.Null;
		}
		PushClipRecord(ref platform, serviceState, record,
			MuiDrawingServiceLayout.ClipKindRectangle, layer, previous);
		return record;
	}

	// MUI_RemoveClipping(mri, handle). Removes a rectangle clip. Enforces strict
	// LIFO: the handle must be the current top of the clip stack and must be a
	// rectangle-clip record. Returns success.
	public static bool RemoveClipping<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR renderInfo, APTR handle)
		where TPlatform : struct, IMuiServicePlatform =>
		RemoveClip(ref platform, serviceState, renderInfo, handle,
			MuiDrawingServiceLayout.ClipKindRectangle);

	// MUI_AddClipRegion(mri, region). Installs a clip region on the layer through
	// the MG09 region capability and pushes an opaque handle onto the same LIFO
	// clip stack. Returns the handle, or Null on a malformed render info / null
	// region / allocation failure (atomic).
	public static APTR AddClipRegion<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR renderInfo, APTR region)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || region.IsNull) return APTR.Null;
		var layer = Layer(ref platform, renderInfo);
		if (layer.IsNull) return APTR.Null;
		var previous = platform.InstallClipRegion(layer, region);
		var record = MuiHeadlessMemory.Allocate(ref platform,
			MuiDrawingClipRecord.Size);
		if (record.IsNull)
		{
			platform.RestoreClipRegion(layer, previous);   // atomic rollback
			return APTR.Null;
		}
		PushClipRecord(ref platform, serviceState, record,
			MuiDrawingServiceLayout.ClipKindRegion, layer, previous);
		return record;
	}

	// MUI_RemoveClipRegion(mri, handle). Removes a clip region. Enforces strict
	// LIFO: the handle must be the top of the clip stack and a region record.
	public static bool RemoveClipRegion<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR renderInfo, APTR handle)
		where TPlatform : struct, IMuiServicePlatform =>
		RemoveClip(ref platform, serviceState, renderInfo, handle,
			MuiDrawingServiceLayout.ClipKindRegion);

	private static bool RemoveClip<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR renderInfo, APTR handle, uint kind)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || handle.IsNull) return false;
		if (!MuiDrawingServiceStateCodec.TryRead(ref platform, serviceState,
			out var state)) return false;
		var top = state.ClipHead;
		// Strict LIFO: only the most-recently added handle may be removed.
		if (top.IsNull || top.Raw != handle.Raw ||
			!MuiDrawingClipCodec.TryRead(ref platform, top, out var clip))
			return false;
		if (clip.Kind != kind)
			return false;
		var layer = clip.Layer;
		var token = clip.Token;
		if (kind == MuiDrawingServiceLayout.ClipKindRectangle)
			platform.PopClip(layer, token);
		else
			platform.RestoreClipRegion(layer, token);
		state.ClipHead = clip.Next;
		if (!MuiDrawingServiceStateCodec.Write(ref platform, serviceState, state))
			return false;
		platform.Clear(top, MuiDrawingClipRecord.Size);
		platform.Free(top, MuiDrawingClipRecord.Size);
		return true;
	}

	private static void PushClipRecord<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR record, uint kind, APTR layer, APTR token)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiDrawingServiceStateCodec.TryRead(ref platform, serviceState,
			out var state)) return;
		MuiDrawingClipRecord clip = default;
		clip.Next = state.ClipHead;
		clip.Kind = kind;
		clip.Layer = layer;
		clip.Token = token;
		if (!MuiDrawingClipCodec.Write(ref platform, record, clip)) return;
		state.ClipHead = record;
		MuiDrawingServiceStateCodec.Write(ref platform, serviceState, state);
	}

	// ---- Refresh -------------------------------------------------------------

	// MUI_BeginRefresh(mri, flags). Validates the reserved flags argument is 0,
	// sets MUIMRI_RefreshMode on mri_Flags, and brackets the layer with
	// BeginUpdate. Every failure path is atomic: the flag is restored and the
	// begun update is closed so no half-open refresh is left behind. Pushes a
	// refresh record for balanced EndRefresh. Returns success.
	public static bool BeginRefresh<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR renderInfo, uint flags)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || flags != 0) return false;
		var layer = Layer(ref platform, renderInfo);
		if (layer.IsNull) return false;
		if (!MuiDrawingRenderInfoCodec.TryRead(ref platform, renderInfo,
			out var renderInfoValue)) return false;
		var saved = renderInfoValue.Flags;
		renderInfoValue.Flags = saved | MuiDrawingServiceLayout.RefreshModeFlag;
		if (!MuiDrawingRenderInfoCodec.Write(ref platform, renderInfo,
			renderInfoValue)) return false;
		if (!platform.BeginUpdate(layer))
		{
			renderInfoValue.Flags = saved;
			MuiDrawingRenderInfoCodec.Write(ref platform, renderInfo,
				renderInfoValue);
			return false;
		}
		var record = MuiHeadlessMemory.Allocate(ref platform,
			MuiDrawingRefreshRecord.Size);
		if (record.IsNull)
		{
			platform.EndUpdate(layer, false);
			renderInfoValue.Flags = saved;
			MuiDrawingRenderInfoCodec.Write(ref platform, renderInfo,
				renderInfoValue);
			return false;
		}
		if (!MuiDrawingServiceStateCodec.TryRead(ref platform, serviceState,
			out var state))
		{
			platform.EndUpdate(layer, false);
			renderInfoValue.Flags = saved;
			MuiDrawingRenderInfoCodec.Write(ref platform, renderInfo,
				renderInfoValue);
			platform.Free(record, MuiDrawingRefreshRecord.Size);
			return false;
		}
		MuiDrawingRefreshRecord refresh = default;
		refresh.Next = state.RefreshHead;
		refresh.RenderInfo = renderInfo;
		refresh.Layer = layer;
		refresh.SavedFlags = saved;
		if (!MuiDrawingRefreshCodec.Write(ref platform, record, refresh))
		{
			platform.EndUpdate(layer, false);
			renderInfoValue.Flags = saved;
			MuiDrawingRenderInfoCodec.Write(ref platform, renderInfo,
				renderInfoValue);
			platform.Free(record, MuiDrawingRefreshRecord.Size);
			return false;
		}
		state.RefreshHead = record;
		if (!MuiDrawingServiceStateCodec.Write(ref platform, serviceState, state))
		{
			platform.EndUpdate(layer, false);
			renderInfoValue.Flags = saved;
			MuiDrawingRenderInfoCodec.Write(ref platform, renderInfo,
				renderInfoValue);
			platform.Free(record, MuiDrawingRefreshRecord.Size);
			return false;
		}
		return true;
	}

	// MUI_EndRefresh(mri, flags). Validates flags == 0, closes the layer with
	// EndUpdate, restores mri_Flags to its pre-refresh value and pops the LIFO
	// refresh record. The handle-free MUI signature is balanced against the top
	// of the refresh stack, which must belong to this render info.
	public static bool EndRefresh<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR renderInfo, uint flags)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || flags != 0 ||
			renderInfo.IsNull) return false;
		if (!MuiDrawingServiceStateCodec.TryRead(ref platform, serviceState,
			out var state)) return false;
		var top = state.RefreshHead;
		if (top.IsNull ||
			!MuiDrawingRefreshCodec.TryRead(ref platform, top,
				out var refresh))
			return false;
		if (refresh.RenderInfo.Raw != renderInfo.Raw) return false;
		var layer = refresh.Layer;
		var saved = refresh.SavedFlags;
		platform.EndUpdate(layer, true);
		if (!MuiDrawingRenderInfoCodec.TryRead(ref platform, renderInfo,
			out var renderInfoValue)) return false;
		renderInfoValue.Flags = saved;
		if (!MuiDrawingRenderInfoCodec.Write(ref platform, renderInfo,
			renderInfoValue)) return false;
		state.RefreshHead = refresh.Next;
		if (!MuiDrawingServiceStateCodec.Write(ref platform, serviceState, state))
			return false;
		platform.Clear(top, MuiDrawingRefreshRecord.Size);
		platform.Free(top, MuiDrawingRefreshRecord.Size);
		return true;
	}

	// ---- Pens ----------------------------------------------------------------

	// MUI_ObtainPen(mri, spec, flags). Validates the render info and the 32-byte
	// black-box pen spec, obtains a pen through the MG09 pen capability and
	// records the FULL returned token so the release can be balanced. A negative
	// capability result is a failure and reserves no record; an allocation
	// failure releases the just-obtained pen (atomic). Returns the full token or
	// a negative value on failure.
	public static int ObtainPen<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR renderInfo, APTR penSpec, uint flags)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState)) return -1;
		if (renderInfo.IsNull ||
			!platform.IsMapped(renderInfo, MuiDrawingRenderInfoRecord.Size))
			return -1;
		if (penSpec.IsNull ||
			!platform.IsMapped(penSpec, MuiDrawingServiceLayout.PenSpecSize))
			return -1;
		var token = platform.ObtainPen(renderInfo, penSpec, flags);
		if (token < 0) return token;
		var record = MuiHeadlessMemory.Allocate(ref platform,
			MuiDrawingPenRecord.Size);
		if (record.IsNull)
		{
			platform.ReleasePen(renderInfo, token);   // atomic rollback
			return -1;
		}
		if (!MuiDrawingServiceStateCodec.TryRead(ref platform, serviceState,
			out var state))
		{
			platform.ReleasePen(renderInfo, token);
			platform.Free(record, MuiDrawingPenRecord.Size);
			return -1;
		}
		MuiDrawingPenRecord penRecord = default;
		penRecord.Next = state.PenHead;
		penRecord.RenderInfo = renderInfo;
		penRecord.Token = unchecked((uint)token);
		if (!MuiDrawingPenCodec.Write(ref platform, record, penRecord))
		{
			platform.ReleasePen(renderInfo, token);
			platform.Free(record, MuiDrawingPenRecord.Size);
			return -1;
		}
		state.PenHead = record;
		if (!MuiDrawingServiceStateCodec.Write(ref platform, serviceState, state))
		{
			platform.ReleasePen(renderInfo, token);
			platform.Free(record, MuiDrawingPenRecord.Size);
			return -1;
		}
		return token;
	}

	// MUI_ReleasePen(mri, pen). Releases a pen acquired through ObtainPen. The
	// caller must supply the full token (never a MUIPEN-masked value); the
	// matching record is located by render info and full token, the capability
	// is called with the full token, and the record is retired. Returns success;
	// an unknown token or a duplicate release fails.
	public static bool ReleasePen<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR renderInfo, int pen)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || renderInfo.IsNull) return false;
		var wanted = unchecked((uint)pen);
		if (!MuiDrawingServiceStateCodec.TryRead(ref platform, serviceState,
			out var state)) return false;
		var current = state.PenHead;
		APTR previous = APTR.Null;
		uint visited = 0;
		while (current.IsNotNull &&
			visited++ < MuiDrawingServiceLayout.MaximumTraversal)
		{
			if (!MuiDrawingPenCodec.TryRead(ref platform, current,
				out var record))
				return false;
			if (record.RenderInfo.Raw == renderInfo.Raw && record.Token == wanted)
			{
				var next = record.Next;
				if (previous.IsNull)
				{
					state.PenHead = next;
					if (!MuiDrawingServiceStateCodec.Write(ref platform, serviceState,
						state)) return false;
				}
				else
				{
					if (!MuiDrawingPenCodec.TryRead(ref platform, previous,
						out var previousRecord)) return false;
					previousRecord.Next = next;
					if (!MuiDrawingPenCodec.Write(ref platform, previous,
						previousRecord)) return false;
				}
				platform.ReleasePen(renderInfo, pen);   // FULL token
				platform.Clear(current, MuiDrawingPenRecord.Size);
				platform.Free(current, MuiDrawingPenRecord.Size);
				return true;
			}
			previous = current;
			current = record.Next;
		}
		return false;
	}

	// MUI_GetRGBColor(mri, spec, rgb). Validates the render info, the black-box
	// pen spec and the generated RGB output block, then routes the mapping
	// through the MG09 pen capability. The spec is never interpreted here.
	public static bool GetRGBColor<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR renderInfo, APTR penSpec, APTR rgbColor)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState)) return false;
		if (renderInfo.IsNull ||
			!platform.IsMapped(renderInfo, MuiDrawingRenderInfoRecord.Size))
			return false;
		if (penSpec.IsNull ||
			!platform.IsMapped(penSpec, MuiDrawingServiceLayout.PenSpecSize))
			return false;
		if (rgbColor.IsNull ||
			!platform.IsMapped(rgbColor, MuiDrawingServiceLayout.RgbColorSize))
			return false;
		return platform.GetRGBColor(renderInfo, penSpec, rgbColor);
	}

	// ---- Internals -----------------------------------------------------------

	// Resolve mri_RastPort->rp_Layer from a validated 28-byte render info. A
	// malformed render info, a null/unmapped rast port or a null layer yields
	// Null so the clipping/refresh entry points can fail cleanly.
	private static APTR Layer<TPlatform>(ref TPlatform platform, APTR renderInfo)
		where TPlatform : struct, IMuiServicePlatform
	{
		MuiDrawingRenderInfoRecord renderInfoValue = default;
		if (!MuiDrawingRenderInfoCodec.TryRead(ref platform, renderInfo,
			out renderInfoValue))
			return APTR.Null;
		MuiDrawingRasterPortRecord rastPort = default;
		if (!MuiDrawingRasterPortCodec.TryRead(ref platform,
			renderInfoValue.RastPort, out rastPort)) return APTR.Null;
		var layerRaw = rastPort.Layer.Raw;
		return APTR.FromPointer(layerRaw);
	}

	private static bool Ready<TPlatform>(ref TPlatform platform, APTR serviceState)
		where TPlatform : struct, IMuiServicePlatform =>
		MuiDrawingServiceStateCodec.TryRead(ref platform, serviceState,
			out var record) && record.Magic == MuiDrawingServiceLayout.Magic &&
		record.Generation == MuiDrawingServiceLayout.Version;
}
