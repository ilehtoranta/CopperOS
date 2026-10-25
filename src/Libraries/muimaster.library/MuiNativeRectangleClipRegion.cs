/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

internal static class MuiNativeRectangleClipRegion
{
	internal static bool ValidGeometry(int left, int top, int width, int height)
	{
		return left >= short.MinValue && left <= short.MaxValue &&
			top >= short.MinValue && top <= short.MaxValue &&
			width > 0 && width <= short.MaxValue &&
			height > 0 && height <= short.MaxValue &&
			left + width - 1 <= short.MaxValue &&
			top + height - 1 <= short.MaxValue;
	}

	internal static bool TryInitialize<TMemory>(ref TMemory memory, APTR region,
		APTR rectangle, int left, int top, int width, int height)
		where TMemory : struct, IMuiGuestMemory
	{
		if (!ValidGeometry(left, top, width, height) || region.IsNull ||
			rectangle.IsNull || (region.Raw & 1) != 0 || (rectangle.Raw & 1) != 0 ||
			!memory.IsMapped(region, Region.Size) ||
			!memory.IsMapped(rectangle, RegionRectangle.Size)) return false;

		var bounds = LayersRectangleCodec.Create((short)left, (short)top,
			(short)(left + width - 1), (short)(top + height - 1));
		var relative = LayersRectangleCodec.Create(0, 0,
			(short)(width - 1), (short)(height - 1));
		LayersRegionCodec.WriteBounds(ref memory, region, bounds);
		LayersRegionCodec.WriteFirst(ref memory, region, rectangle);
		LayersRegionRectangleCodec.WriteNext(ref memory, rectangle, APTR.Null);
		LayersRegionRectangleCodec.WritePrevious(ref memory, rectangle,
			LayersRegionCodec.HeadAnchor(region));
		LayersRegionRectangleCodec.WriteBounds(ref memory, rectangle, relative);
		return true;
	}
}
