/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.MuiMaster;

// Native drawing helpers keep all guest records named and declaration ordered.
// The only raw-vector arithmetic in this file is the ABI entry computation;
// public structures cross the boundary through the SDK codecs or the local
// sequential codecs below.
internal static class MuiNativeDrawingCalls
{
	private const short NewRegionVector = -516;
	private const short DisposeRegionVector = -534;
	private const short AndRegionRegionVector = -624;
	private const short OrRectRegionVector = -510;
	private const short ClipBlitVector = -552;
	private const short AllocBitMapVector = -918;
	private const short FreeBitMapVector = -924;
	private const short GetBitMapAttrVector = -960;

	internal static APTR AllocateBitmap(APTR graphics, uint width,
		uint height, uint depth, uint flags, APTR friendBitmap)
	{
		if (graphics.IsNull || width == 0 || height == 0 || depth == 0)
			return APTR.Null;
		return APTR.FromPointer(AllocateBitmapCall(Entry(graphics,
			AllocBitMapVector), graphics, width, height, depth, flags,
			friendBitmap));
	}

	internal static void FreeBitmap(APTR graphics, APTR bitmap)
	{
		if (graphics.IsNull || bitmap.IsNull) return;
		FreeBitmapCall(Entry(graphics, FreeBitMapVector), graphics, bitmap);
	}

	internal static uint GetBitMapAttribute(APTR graphics, APTR bitmap,
		BitMapAttribute attribute)
	{
		if (graphics.IsNull || bitmap.IsNull) return 0;
		return GetBitMapAttributeCall(Entry(graphics, GetBitMapAttrVector),
			graphics, bitmap, (uint)attribute);
	}

	internal static void ClipBlit(APTR graphics, APTR sourceRastPort,
		int sourceLeft, int sourceTop, APTR destinationRastPort,
		int destinationLeft, int destinationTop, int width, int height,
		uint minterm)
	{
		if (graphics.IsNull || sourceRastPort.IsNull ||
			destinationRastPort.IsNull || width <= 0 || height <= 0) return;
		ClipBlitCall(Entry(graphics, ClipBlitVector), graphics, sourceRastPort,
			sourceLeft, sourceTop, destinationRastPort, destinationLeft,
			destinationTop, width, height, minterm);
	}

	internal static APTR NewRegion(APTR graphics)
	{
		if (graphics.IsNull) return APTR.Null;
		return APTR.FromPointer(NewRegionCall(Entry(graphics,
			NewRegionVector), graphics));
	}

	internal static bool OrRectRegion(APTR graphics, APTR region,
		APTR rectangle) => graphics.IsNotNull && region.IsNotNull &&
		rectangle.IsNotNull && OrRectRegionCall(Entry(graphics,
			OrRectRegionVector), graphics, region, rectangle) != 0;

	internal static bool AndRegionRegion(APTR graphics, APTR region,
		APTR result) => graphics.IsNotNull && region.IsNotNull &&
		result.IsNotNull && AndRegionRegionCall(Entry(graphics,
			AndRegionRegionVector), graphics, region, result) != 0;

	internal static void DisposeRegion(APTR graphics, APTR region)
	{
		if (graphics.IsNull || region.IsNull) return;
		DisposeRegionCall(Entry(graphics, DisposeRegionVector), graphics, region);
	}

	internal static bool LockLayer(APTR layers, APTR layer)
	{
		if (layers.IsNull || layer.IsNull) return false;
		LockLayerCall(Entry(layers, -96), layers, 0, layer);
		return true;
	}

	internal static void UnlockLayer(APTR layers, APTR layer)
	{
		if (layers.IsNull || layer.IsNull) return;
		UnlockLayerCall(Entry(layers, -102), layers, layer);
	}

	internal static bool BeginUpdate(APTR layers, APTR layer) =>
		layers.IsNotNull && layer.IsNotNull &&
		BeginUpdateCall(Entry(layers, -78), layers, layer) != 0;

	internal static void EndUpdate(APTR layers, APTR layer, bool complete)
	{
		if (layers.IsNull || layer.IsNull) return;
		EndUpdateCall(Entry(layers, -84), layers, layer,
			complete ? 1u : 0u);
	}

	internal static APTR InstallClipRegion(APTR layers, APTR layer,
		APTR region)
	{
		if (layers.IsNull || layer.IsNull) return APTR.Null;
		return APTR.FromPointer(InstallClipRegionCall(Entry(layers, -174),
			layers, layer, region));
	}

	internal static int ObtainBestPen(APTR graphics, APTR colorMap,
		uint red, uint green, uint blue)
	{
		if (graphics.IsNull || colorMap.IsNull) return -1;
		return unchecked((int)ObtainBestPenCall(Entry(graphics, -840),
			graphics, colorMap, red, green, blue, APTR.Null));
	}

	internal static void ReleasePen(APTR graphics, APTR colorMap, uint pen)
	{
		if (graphics.IsNull || colorMap.IsNull) return;
		ReleasePenCall(Entry(graphics, -948), graphics, colorMap, pen);
	}

	internal static bool GetRGB32(APTR graphics, APTR viewPort, uint pen,
		APTR rgbColor)
	{
		if (graphics.IsNull || viewPort.IsNull || rgbColor.IsNull) return false;
		GetRGB32Call(Entry(graphics, -900), graphics, viewPort, pen, 1u,
			rgbColor);
		return true;
	}

	private static APTR Entry(APTR library, short lvo) =>
		APTR.FromPointer(unchecked(library.Raw - (uint)-lvo));

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint AllocateBitmapCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR graphics,
		[M68kRegister(M68kRegister.D0)] uint width,
		[M68kRegister(M68kRegister.D1)] uint height,
		[M68kRegister(M68kRegister.D2)] uint depth,
		[M68kRegister(M68kRegister.D3)] uint flags,
		[M68kRegister(M68kRegister.A0)] APTR friendBitmap);

	[AmigaIndirectCall(M68kRegister.A3)]
	private static extern void FreeBitmapCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR graphics,
		[M68kRegister(M68kRegister.A0)] APTR bitmap);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint GetBitMapAttributeCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR graphics,
		[M68kRegister(M68kRegister.A0)] APTR bitmap,
		[M68kRegister(M68kRegister.D1)] uint attribute);

	[AmigaIndirectCall(M68kRegister.A3)]
	private static extern void ClipBlitCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR graphics,
		[M68kRegister(M68kRegister.A0)] APTR sourceRastPort,
		[M68kRegister(M68kRegister.D0)] int sourceLeft,
		[M68kRegister(M68kRegister.D1)] int sourceTop,
		[M68kRegister(M68kRegister.A1)] APTR destinationRastPort,
		[M68kRegister(M68kRegister.D2)] int destinationLeft,
		[M68kRegister(M68kRegister.D3)] int destinationTop,
		[M68kRegister(M68kRegister.D4)] int width,
		[M68kRegister(M68kRegister.D5)] int height,
		[M68kRegister(M68kRegister.D6)] uint minterm);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint NewRegionCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR graphics);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint OrRectRegionCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR graphics,
		[M68kRegister(M68kRegister.A0)] APTR region,
		[M68kRegister(M68kRegister.A1)] APTR rectangle);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint AndRegionRegionCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR graphics,
		[M68kRegister(M68kRegister.A0)] APTR region,
		[M68kRegister(M68kRegister.A1)] APTR other);

	[AmigaIndirectCall(M68kRegister.A3)]
	private static extern void DisposeRegionCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR graphics,
		[M68kRegister(M68kRegister.A0)] APTR region);

	[AmigaIndirectCall(M68kRegister.A3)]
	private static extern void LockLayerCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] int dummy,
		[M68kRegister(M68kRegister.A1)] APTR layer);

	[AmigaIndirectCall(M68kRegister.A3)]
	private static extern void UnlockLayerCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] APTR layer);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint BeginUpdateCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] APTR layer);

	[AmigaIndirectCall(M68kRegister.A3)]
	private static extern void EndUpdateCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] APTR layer,
		[M68kRegister(M68kRegister.D0)] uint complete);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint InstallClipRegionCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] APTR layer,
		[M68kRegister(M68kRegister.A1)] APTR region);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint ObtainBestPenCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] APTR colorMap,
		[M68kRegister(M68kRegister.D1)] uint red,
		[M68kRegister(M68kRegister.D2)] uint green,
		[M68kRegister(M68kRegister.D3)] uint blue,
		[M68kRegister(M68kRegister.A1)] APTR tags);

	[AmigaIndirectCall(M68kRegister.A3)]
	private static extern void ReleasePenCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] APTR colorMap,
		[M68kRegister(M68kRegister.D0)] uint pen);

	[AmigaIndirectCall(M68kRegister.A3)]
	private static extern void GetRGB32Call(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] APTR viewPort,
		[M68kRegister(M68kRegister.D0)] uint pen,
		[M68kRegister(M68kRegister.D1)] uint count,
		[M68kRegister(M68kRegister.A1)] APTR table);
}

[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeClipTokenRecord
{
	internal const uint Magic = 0x4D554943; // "MUIC"
	internal const uint Size = 20;
	internal uint Signature;
	internal APTR Layer;
	internal APTR PreviousRegion;
	internal APTR Region;
	internal APTR RegionRectangle;
}

internal static class MuiNativeClipTokenStructCodec
{
	internal static bool TryRead<T>(ref T memory, APTR address,
		out MuiNativeClipTokenRecord value) where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeClipTokenRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Signature) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var layer) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var previous) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var region) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var rectangle) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Layer = APTR.FromPointer(layer);
		value.PreviousRegion = APTR.FromPointer(previous);
		value.Region = APTR.FromPointer(region);
		value.RegionRectangle = APTR.FromPointer(rectangle);
		return value.Signature == MuiNativeClipTokenRecord.Magic;
	}

	internal static bool Write<T>(ref T memory, APTR address,
		MuiNativeClipTokenRecord value) where T : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeClipTokenRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Signature) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Layer.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.PreviousRegion.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Region.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.RegionRectangle.Raw) &&
		MuiGuestStructCursor.IsComplete(cursor);
}

// The pen spec is a fixed 32-byte guest record containing a NUL-terminated
// MorphOS pen-spec string. Eight named ULONG words preserve it without a
// managed byte array or an unsafe runtime dependency.
[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativePenSpecRecord
{
	internal const uint Size = 32;
	internal uint Word0;
	internal uint Word1;
	internal uint Word2;
	internal uint Word3;
	internal uint Word4;
	internal uint Word5;
	internal uint Word6;
	internal uint Word7;
}

internal static class MuiNativePenSpecStructCodec
{
	internal static bool TryRead<T>(ref T memory, APTR address,
		out MuiNativePenSpecRecord value) where T : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativePenSpecRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Word0) &&
			MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Word1) &&
			MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Word2) &&
			MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Word3) &&
			MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Word4) &&
			MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Word5) &&
			MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Word6) &&
			MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Word7) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static byte ReadByte(MuiNativePenSpecRecord value, uint index)
	{
		if (index >= MuiNativePenSpecRecord.Size) return 0;
		var word = index >> 2;
		var shift = 24 - (int)((index & 3) * 8);
		var raw = word switch
		{
			0 => value.Word0,
			1 => value.Word1,
			2 => value.Word2,
			3 => value.Word3,
			4 => value.Word4,
			5 => value.Word5,
			6 => value.Word6,
			_ => value.Word7,
		};
		return (byte)(raw >> shift);
	}
}

// The public DrawInfo is an embedded Intuition record.  The native pen path
// only needs its declared pen count and pen-array pointer; the remaining
// fields are consumed as a bounded tail so no unmanaged layout is overlaid.
[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeDrawInfoRecord
{
	internal const uint Size = 50;
	internal ushort Version;
	internal ushort NumberOfPens;
	internal APTR Pens;
}

internal static class MuiNativeDrawInfoStructCodec
{
	internal static bool TryRead<T>(ref T memory, APTR address,
		out MuiNativeDrawInfoRecord value) where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeDrawInfoRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor,
				out value.Version) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor,
				out value.NumberOfPens) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var pens) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeDrawInfoRecord.Size - 8, out _) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Pens = APTR.FromPointer(pens);
		return true;
	}
}

// A screen embeds ViewPort after its declared header.  This adapter consumes
// that declaration order and publishes only the named ViewPort record needed
// by GetRGBColor; it never returns the SDK's aggregate Screen value through a
// freestanding method boundary.
[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeScreenRecord
{
	internal const uint Size = 346;
	internal APTR ViewPortAddress;
}

internal static class MuiNativeScreenStructCodec
{
	internal static bool TryRead<T>(ref T memory, APTR address,
		out MuiNativeScreenRecord value) where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeScreenRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 4, out _) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 4, out _))
			return false;
		for (var index = 0; index < 7; index++)
			if (!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 2, out _))
				return false;
		if (!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 4, out _) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 4, out _) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 9, out _) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 1, out _) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 4, out _) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeViewPortRecord.Size, out var viewport)) return false;
		value.ViewPortAddress = viewport;
		return MuiGuestStructCursor.TryTake(ref memory, ref cursor,
			MuiNativeScreenRecord.Size - 44 - MuiNativeViewPortRecord.Size,
			out _) && MuiGuestStructCursor.IsComplete(cursor);
	}
}
