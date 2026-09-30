/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Native realization of the narrow drawing capability. Layers are opened for
// the duration of each call so no second library base is cached in the class
// context. Graphics remains the already pinned provider owned by MUI. The
// implementation deliberately uses the SDK's named Region, Screen, DrawInfo,
// and ViewPort records; the only arithmetic is for array elements and ABI
// entry addresses.
internal static class MuiNativeDrawingPlatformCore
{
	private const uint NativePenTag = 0x0001_0000u;
	private const uint PenMask = 0x0000_FFFFu;
	private const uint LayersFlags = (uint)(Exec.MemoryFlags.Public |
		Exec.MemoryFlags.Clear);

	internal static bool LockLayer(ref MuiNativeClassPlatform platform,
		APTR layer)
	{
		var layers = OpenLayers();
		if (layers.IsNull) return false;
		var result = MuiNativeDrawingCalls.LockLayer(layers, layer);
		CloseLayers(layers);
		return result;
	}

	internal static void UnlockLayer(ref MuiNativeClassPlatform platform,
		APTR layer)
	{
		var layers = OpenLayers();
		if (layers.IsNull) return;
		MuiNativeDrawingCalls.UnlockLayer(layers, layer);
		CloseLayers(layers);
	}

	internal static bool BeginUpdate(ref MuiNativeClassPlatform platform,
		APTR layer)
	{
		var layers = OpenLayers();
		if (layers.IsNull) return false;
		var result = MuiNativeDrawingCalls.BeginUpdate(layers, layer);
		CloseLayers(layers);
		return result;
	}

	internal static void EndUpdate(ref MuiNativeClassPlatform platform,
		APTR layer, bool completed)
	{
		var layers = OpenLayers();
		if (layers.IsNull) return;
		MuiNativeDrawingCalls.EndUpdate(layers, layer, completed);
		CloseLayers(layers);
	}

	internal static APTR InstallClipRegion(
		ref MuiNativeClassPlatform platform, APTR layer, APTR region)
	{
		var layers = OpenLayers();
		if (layers.IsNull || layer.IsNull || region.IsNull)
		{
			if (layers.IsNotNull) CloseLayers(layers);
			return APTR.Null;
		}
		var previous = MuiNativeDrawingCalls.InstallClipRegion(layers, layer,
			region);
		CloseLayers(layers);
		return previous;
	}

	internal static void RestoreClipRegion(
		ref MuiNativeClassPlatform platform, APTR layer, APTR previousRegion)
	{
		var layers = OpenLayers();
		if (layers.IsNull || layer.IsNull)
		{
			if (layers.IsNotNull) CloseLayers(layers);
			return;
		}
		MuiNativeDrawingCalls.InstallClipRegion(layers, layer, previousRegion);
		CloseLayers(layers);
	}

	internal static APTR PushClip(ref MuiNativeClassPlatform platform,
		APTR layer, int left, int top, int width, int height)
	{
		if (layer.IsNull || !FitsShort(left) || !FitsShort(top) ||
			!FitsPositiveShort(width) || !FitsPositiveShort(height))
			return APTR.Null;
		var layers = OpenLayers();
		if (layers.IsNull) return APTR.Null;
		var region = platform.Allocate(Region.Size, LayersFlags);
		var rectangle = platform.Allocate(RegionRectangle.Size, LayersFlags);
		var token = platform.Allocate(MuiNativeClipTokenRecord.Size, LayersFlags);
		if (region.IsNull || rectangle.IsNull || token.IsNull)
		{
			if (token.IsNotNull) platform.Free(token,
				MuiNativeClipTokenRecord.Size);
			if (rectangle.IsNotNull) platform.Free(rectangle,
				RegionRectangle.Size);
			if (region.IsNotNull) platform.Free(region, Region.Size);
			CloseLayers(layers);
			return APTR.Null;
		}

		var bounds = LayersRectangleCodec.Create((short)left, (short)top,
			(short)(left + width - 1), (short)(top + height - 1));
		LayersRegionCodec.WriteBounds(ref platform, region, bounds);
		LayersRegionCodec.WriteFirst(ref platform, region, rectangle);
		LayersRegionRectangleCodec.WriteNext(ref platform, rectangle,
			APTR.Null);
		LayersRegionRectangleCodec.WritePrevious(ref platform, rectangle,
			APTR.Null);
		LayersRegionRectangleCodec.WriteBounds(ref platform, rectangle,
			bounds);

		var previous = MuiNativeDrawingCalls.InstallClipRegion(layers, layer,
			region);
		MuiNativeClipTokenRecord tokenValue = default;
		tokenValue.Signature = MuiNativeClipTokenRecord.Magic;
		tokenValue.Layer = layer;
		tokenValue.PreviousRegion = previous;
		tokenValue.Region = region;
		tokenValue.RegionRectangle = rectangle;
		if (!MuiNativeClipTokenStructCodec.Write(ref platform, token, tokenValue))
		{
			MuiNativeDrawingCalls.InstallClipRegion(layers, layer, previous);
			platform.Free(token, MuiNativeClipTokenRecord.Size);
			platform.Free(rectangle, RegionRectangle.Size);
			platform.Free(region, Region.Size);
			CloseLayers(layers);
			return APTR.Null;
		}
		CloseLayers(layers);
		return token;
	}

	// Install a rectangle clip without discarding an existing region. Graphics
	// region algebra copies its inputs and writes AndRegionRegion's result to its
	// second region; the returned token owns that new library-created region.
	internal static APTR PushClipIntersection(
		ref MuiNativeClassPlatform platform, APTR layer, int left, int top,
		int width, int height)
	{
		if (layer.IsNull || !platform.IsMapped(layer, Layer.Size) ||
			!FitsShort(left) || !FitsShort(top) ||
			!FitsPositiveShort(width) || !FitsPositiveShort(height))
			return APTR.Null;
		var right = left + width - 1;
		var bottom = top + height - 1;
		if (!FitsShort(right) || !FitsShort(bottom)) return APTR.Null;

		var graphics = platform.GraphicsBase;
		if (graphics.IsNull) return APTR.Null;
		var layers = OpenLayers();
		if (layers.IsNull) return APTR.Null;
		var rectangle = platform.Allocate(Rectangle.Size, LayersFlags);
		var token = platform.Allocate(MuiNativeClipTokenRecord.Size, LayersFlags);
		var region = MuiNativeDrawingCalls.NewRegion(graphics);
		if (rectangle.IsNull || token.IsNull || region.IsNull ||
			!platform.IsMapped(region, Region.Size))
		{
			DisposeClipIntersectionResources(ref platform, layers, rectangle,
				token, region);
			return APTR.Null;
		}

		var bounds = LayersRectangleCodec.Create((short)left, (short)top,
			(short)right, (short)bottom);
		if (!LayersRectangleCodec.IsMapped(ref platform, rectangle))
		{
			DisposeClipIntersectionResources(ref platform, layers, rectangle,
				token, region);
			return APTR.Null;
		}
		LayersRectangleCodec.Write(ref platform, rectangle, bounds);
		if (!MuiNativeDrawingCalls.OrRectRegion(graphics, region, rectangle))
		{
			DisposeClipIntersectionResources(ref platform, layers, rectangle,
				token, region);
			return APTR.Null;
		}

		var expectedPrevious = LayersLayerCodec.ReadClipRegion(ref platform,
			layer);
		if (expectedPrevious.IsNotNull &&
			(!platform.IsMapped(expectedPrevious, Region.Size) ||
				!MuiNativeDrawingCalls.AndRegionRegion(graphics,
					expectedPrevious, region)))
		{
			DisposeClipIntersectionResources(ref platform, layers, rectangle,
				token, region);
			return APTR.Null;
		}

		var previous = MuiNativeDrawingCalls.InstallClipRegion(layers, layer,
			region);
		if (previous.Raw != expectedPrevious.Raw)
		{
			MuiNativeDrawingCalls.InstallClipRegion(layers, layer, previous);
			DisposeClipIntersectionResources(ref platform, layers, rectangle,
				token, region);
			return APTR.Null;
		}
		MuiNativeClipTokenRecord tokenValue = default;
		tokenValue.Signature = MuiNativeClipTokenRecord.Magic;
		tokenValue.Layer = layer;
		tokenValue.PreviousRegion = previous;
		tokenValue.Region = region;
		// Null marks Graphics.NewRegion ownership; the region's rectangle list
		// must be retired with DisposeRegion, not Exec.FreeMem.
		tokenValue.RegionRectangle = APTR.Null;
		if (!MuiNativeClipTokenStructCodec.Write(ref platform, token,
			tokenValue))
		{
			MuiNativeDrawingCalls.InstallClipRegion(layers, layer, previous);
			DisposeClipIntersectionResources(ref platform, layers, rectangle,
				token, region);
			return APTR.Null;
		}
		platform.Free(rectangle, Rectangle.Size);
		CloseLayers(layers);
		return token;
	}

	internal static void PopClip(ref MuiNativeClassPlatform platform,
		APTR layer, APTR token)
	{
		if (layer.IsNull || token.IsNull ||
			!MuiNativeClipTokenStructCodec.TryRead(ref platform, token,
				out var value) || value.Layer.Raw != layer.Raw) return;
		var layers = OpenLayers();
		if (layers.IsNull) return;
		var current = MuiNativeDrawingCalls.InstallClipRegion(layers, layer,
			value.PreviousRegion);
		if (current.Raw != value.Region.Raw)
		{
			MuiNativeDrawingCalls.InstallClipRegion(layers, layer, current);
			CloseLayers(layers);
			return;
		}
		CloseLayers(layers);
		if (value.RegionRectangle.IsNull)
		{
			MuiNativeDrawingCalls.DisposeRegion(platform.GraphicsBase,
				value.Region);
		}
		else
		{
			platform.Clear(value.RegionRectangle, RegionRectangle.Size);
			platform.Clear(value.Region, Region.Size);
			platform.Free(value.RegionRectangle, RegionRectangle.Size);
			platform.Free(value.Region, Region.Size);
		}
		platform.Clear(token, MuiNativeClipTokenRecord.Size);
		platform.Free(token, MuiNativeClipTokenRecord.Size);
	}

	private static void DisposeClipIntersectionResources(
		ref MuiNativeClassPlatform platform, APTR layers, APTR rectangle,
		APTR token, APTR region)
	{
		if (region.IsNotNull)
			MuiNativeDrawingCalls.DisposeRegion(platform.GraphicsBase, region);
		if (token.IsNotNull)
		{
			platform.Clear(token, MuiNativeClipTokenRecord.Size);
			platform.Free(token, MuiNativeClipTokenRecord.Size);
		}
		if (rectangle.IsNotNull) platform.Free(rectangle, Rectangle.Size);
		if (layers.IsNotNull) CloseLayers(layers);
	}

	internal static int ObtainPen(ref MuiNativeClassPlatform platform,
		APTR renderInfo, APTR penSpec, uint flags)
	{
		if (flags != 0 || !TryReadRenderInfo(ref platform, renderInfo,
			out var info) || penSpec.IsNull ||
			!platform.IsMapped(penSpec, MuiNativePenSpecRecord.Size) ||
			!MuiNativePenSpecStructCodec.TryRead(ref platform, penSpec,
				out var spec)) return -1;
		if (!TryReadSpec(ref platform, info, spec, out var kind, out var pen,
			out var red, out var green, out var blue)) return -1;
		if (kind != MuiNativePenKind.Rgb) return unchecked((int)pen);
		var colorMap = ColorMap(ref platform, info);
		var raw = MuiNativeDrawingCalls.ObtainBestPen(platform.GraphicsBase,
			colorMap, red, green, blue);
		if (raw < 0) return -1;
		return unchecked((int)(NativePenTag | (uint)raw));
	}

	internal static void ReleasePen(ref MuiNativeClassPlatform platform,
		APTR renderInfo, int pen)
	{
		var token = unchecked((uint)pen);
		if ((token & NativePenTag) == 0) return;
		if (!TryReadRenderInfo(ref platform, renderInfo, out var info)) return;
		var colorMap = ColorMap(ref platform, info);
		if (colorMap.IsNull) return;
		MuiNativeDrawingCalls.ReleasePen(platform.GraphicsBase, colorMap,
			token & PenMask);
	}

	internal static bool GetRGBColor(ref MuiNativeClassPlatform platform,
		APTR renderInfo, APTR penSpec, APTR rgbColor)
	{
		if (!TryReadRenderInfo(ref platform, renderInfo, out var info) ||
			penSpec.IsNull || rgbColor.IsNull ||
			!platform.IsMapped(penSpec, MuiNativePenSpecRecord.Size) ||
			!platform.IsMapped(rgbColor, MuiColorRgbRecord.Size) ||
			!MuiNativePenSpecStructCodec.TryRead(ref platform, penSpec,
				out var spec) || !TryReadSpec(ref platform, info, spec,
				out var kind, out var pen, out var red, out var green,
				out var blue)) return false;
		if (kind == MuiNativePenKind.Rgb)
		{
			MuiColorRgbRecord rgb = default;
			rgb.Red = red;
			rgb.Green = green;
			rgb.Blue = blue;
			return MuiColorRgbCodec.WriteRecord(ref platform, rgbColor, rgb);
		}
		if (!TryReadScreen(ref platform, info.Screen, out var screen)) return false;
		var viewPort = platform.Allocate(ViewPort.Size, LayersFlags);
		if (viewPort.IsNull) return false;
		if (!MuiNativeViewPortStructCodec.Write(ref platform, viewPort,
			screen) || !MuiNativeDrawingCalls.GetRGB32(
			platform.GraphicsBase, viewPort, pen, rgbColor))
		{
			platform.Free(viewPort, ViewPort.Size);
			return false;
		}
		platform.Free(viewPort, ViewPort.Size);
		return true;
	}

	private static bool TryReadRenderInfo(ref MuiNativeClassPlatform platform,
		APTR address, out MuiDrawingRenderInfoRecord value)
	{
		value = default;
		return !address.IsNull &&
			MuiDrawingRenderInfoCodec.TryRead(ref platform, address, out value) &&
			!value.RastPort.IsNull;
	}

	private static bool TryReadScreen(ref MuiNativeClassPlatform platform,
		APTR address, out MuiNativeViewPortRecord value)
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiNativeScreenRecord.Size) ||
			!MuiNativeScreenStructCodec.TryRead(ref platform, address,
				out var screen) || screen.ViewPortAddress.IsNull ||
			!MuiNativeViewPortStructCodec.TryRead(ref platform,
				screen.ViewPortAddress, out value)) return false;
		return true;
	}

	private static APTR ColorMap(ref MuiNativeClassPlatform platform,
		MuiDrawingRenderInfoRecord info)
	{
		if (!TryReadScreen(ref platform, info.Screen, out var screen))
			return APTR.Null;
		return screen.ColorMap;
	}

	private enum MuiNativePenKind : byte
	{
		Static,
		Rgb,
	}

	private static bool TryReadSpec(ref MuiNativeClassPlatform platform,
		MuiDrawingRenderInfoRecord info, MuiNativePenSpecRecord spec,
		out MuiNativePenKind kind, out uint pen, out uint red,
		out uint green, out uint blue)
	{
		kind = MuiNativePenKind.Static;
		pen = 0;
		red = green = blue = 0;
		var first = MuiNativePenSpecStructCodec.ReadByte(spec, 0);
		if (first == 0)
			return ReadMruPen(ref platform, info.Pens, 5, out pen);
		if (first == (byte)'s')
		{
			if (!TryDecimal(spec, 1, out var index)) return false;
			if (ReadDrawInfoPen(ref platform, info.DrawInfo, index, out pen)) return true;
			return ReadDrawInfoPen(ref platform, info.DrawInfo,
				(byte)DrawInfoPen.Background, out pen);
		}
		if (first == (byte)'m')
		{
			if (!TryDecimal(spec, 1, out var index) || index >= 8)
				index = 2;
			return ReadMruPen(ref platform, info.Pens, index, out pen);
		}
		if (first == (byte)'p')
		{
			if (!TryDecimal(spec, 1, out var index) || index > ushort.MaxValue)
				return false;
			pen = index;
			return true;
		}
		if (first != (byte)'r' ||
			!TryRgb(spec, out red, out green, out blue)) return false;
		kind = MuiNativePenKind.Rgb;
		return true;
	}

	private static bool ReadDrawInfoPen(ref MuiNativeClassPlatform platform,
		APTR drawInfoAddress, uint index, out uint pen)
	{
		pen = 0;
		if (drawInfoAddress.IsNull || !platform.IsMapped(drawInfoAddress,
			DrawInfo.Size)) return false;
		if (!MuiNativeDrawInfoStructCodec.TryRead(ref platform, drawInfoAddress,
			out var drawInfo)) return false;
		if (index >= drawInfo.NumberOfPens || drawInfo.Pens.IsNull ||
			!platform.IsMapped(drawInfo.Pens, (index + 1) * 2)) return false;
		pen = platform.ReadUInt16(drawInfo.Pens, checked((int)(index * 2)));
		return true;
	}

	private static bool ReadMruPen(ref MuiNativeClassPlatform platform,
		APTR pens, uint index, out uint pen)
	{
		pen = 0;
		if (pens.IsNull || !platform.IsMapped(pens, (index + 1) * 2)) return false;
		pen = platform.ReadUInt16(pens, checked((int)(index * 2)));
		return true;
	}

	private static bool TryDecimal(MuiNativePenSpecRecord value, uint start,
		out uint number)
	{
		number = 0;
		var seen = false;
		for (var index = start; index < MuiNativePenSpecRecord.Size; index++)
		{
			var c = MuiNativePenSpecStructCodec.ReadByte(value, index);
			if (c == 0) return seen;
			if (c < (byte)'0' || c > (byte)'9') return false;
			seen = true;
			number = number * 10u + (uint)(c - (byte)'0');
			if (number > ushort.MaxValue) return false;
		}
		return seen;
	}

	private static bool TryRgb(MuiNativePenSpecRecord value, out uint red,
		out uint green, out uint blue)
	{
		red = green = blue = 0;
		var length = 0u;
		while (length < MuiNativePenSpecRecord.Size &&
			MuiNativePenSpecStructCodec.ReadByte(value, length) != 0) length++;
		if (length == 7)
		{
			if (!Hex(value, 1, 2, out var r) || !Hex(value, 3, 2, out var g) ||
				!Hex(value, 5, 2, out var b)) return false;
			red = r * 0x01010101u;
			green = g * 0x01010101u;
			blue = b * 0x01010101u;
			return true;
		}
		return length == 27 && Hex(value, 1, 8, out red) &&
			MuiNativePenSpecStructCodec.ReadByte(value, 9) == (byte)',' &&
			Hex(value, 10, 8, out green) &&
			MuiNativePenSpecStructCodec.ReadByte(value, 18) == (byte)',' &&
			Hex(value, 19, 8, out blue);
	}

	private static bool Hex(MuiNativePenSpecRecord value, uint start,
		uint count, out uint result)
	{
		result = 0;
		for (var index = 0u; index < count; index++)
		{
			var c = MuiNativePenSpecStructCodec.ReadByte(value, start + index);
			var digit = c >= (byte)'0' && c <= (byte)'9' ? c - (byte)'0' :
				c >= (byte)'a' && c <= (byte)'f' ? c - (byte)'a' + 10 :
				c >= (byte)'A' && c <= (byte)'F' ? c - (byte)'A' + 10 : 0xff;
			if (digit > 0x0f) return false;
			result = (result << 4) | (uint)digit;
		}
		return true;
	}

	private static bool FitsShort(int value) => value >= short.MinValue &&
		value <= short.MaxValue;
	private static bool FitsPositiveShort(int value) => value > 0 &&
		value <= short.MaxValue;

	private static APTR OpenLayers() => Exec.OpenLibraryRaw(
		CString.FromLiteral("layers.library"), 0);
	private static void CloseLayers(APTR layers) => Exec.CloseLibrary(layers);
}

[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeViewPortRecord
{
	internal const uint Size = ViewPort.Size;
	internal APTR Next;
	internal APTR ColorMap;
	internal APTR DisplayInstructions;
	internal APTR SpriteInstructions;
	internal APTR ColorInstructions;
	internal APTR UserCopperInstructions;
	internal short DisplayWidth;
	internal short DisplayHeight;
	internal short DisplayXOffset;
	internal short DisplayYOffset;
	internal ushort Modes;
	internal byte SpritePriorities;
	internal byte ExtendedModes;
	internal APTR RasInfo;
}

internal static class MuiNativeViewPortStructCodec
{
	internal static bool TryRead<T>(ref T memory, APTR address,
		out MuiNativeViewPortRecord value)
		where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeViewPortRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var next) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var colorMap) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var displayInstructions) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var spriteInstructions) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var colorInstructions) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var userCopperInstructions) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor,
				out var displayWidth) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor,
				out var displayHeight) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor,
				out var displayXOffset) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor,
				out var displayYOffset) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor,
				out value.Modes) ||
			!MuiGuestStructCursor.TryReadUInt8(ref memory, ref cursor,
				out value.SpritePriorities) ||
			!MuiGuestStructCursor.TryReadUInt8(ref memory, ref cursor,
				out value.ExtendedModes) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var rasInfo) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Next = APTR.FromPointer(next);
		value.ColorMap = APTR.FromPointer(colorMap);
		value.DisplayInstructions = APTR.FromPointer(displayInstructions);
		value.SpriteInstructions = APTR.FromPointer(spriteInstructions);
		value.ColorInstructions = APTR.FromPointer(colorInstructions);
		value.UserCopperInstructions = APTR.FromPointer(userCopperInstructions);
		value.DisplayWidth = unchecked((short)displayWidth);
		value.DisplayHeight = unchecked((short)displayHeight);
		value.DisplayXOffset = unchecked((short)displayXOffset);
		value.DisplayYOffset = unchecked((short)displayYOffset);
		value.RasInfo = APTR.FromPointer(rasInfo);
		return true;
	}

	internal static bool Write<T>(ref T memory, APTR address,
		MuiNativeViewPortRecord value)
		where T : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeViewPortRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
				value.Next.Raw) || !MuiGuestStructCursor.TryWriteUInt32(ref memory,
				ref cursor, value.ColorMap.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
				value.DisplayInstructions.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
				value.SpriteInstructions.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
				value.ColorInstructions.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
				value.UserCopperInstructions.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref memory, ref cursor,
				unchecked((ushort)value.DisplayWidth)) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref memory, ref cursor,
				unchecked((ushort)value.DisplayHeight)) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref memory, ref cursor,
				unchecked((ushort)value.DisplayXOffset)) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref memory, ref cursor,
				unchecked((ushort)value.DisplayYOffset)) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref memory, ref cursor,
				value.Modes) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref memory, ref cursor,
				value.SpritePriorities) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref memory, ref cursor,
				value.ExtendedModes) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
				value.RasInfo.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}
