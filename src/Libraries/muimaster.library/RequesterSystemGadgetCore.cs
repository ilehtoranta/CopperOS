/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Prepared view of the MUI gadget string for the MorphOS system-requester
// fallback. GadgetFormat is borrowed unless AllocationSize is nonzero. The
// return convention matches MUI/EES_ActiveButton: 1..N for buttons from left
// to right, with the last button represented by zero.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRequesterSystemGadgetPlan
{
	internal APTR GadgetFormat;
	internal uint AllocationSize;
	internal uint GadgetCount;
	internal uint ActiveButton;
	internal uint HasActiveButton;
}

internal static class MuiRequesterSystemGadgetCore
{
	internal static bool TryPrepare<TPlatform>(ref TPlatform platform,
		APTR gadgets, out MuiRequesterSystemGadgetPlan plan)
		where TPlatform : struct, IMuiAllocationPlatform
	{
		plan = default;
		plan.GadgetFormat = gadgets;
		if (!MuiRequesterGadgetSetCore.TryParse(ref platform, gadgets,
			out var set)) return false;
		plan.GadgetCount = set.GadgetCount;
		if (gadgets.IsNull) return true;
		var length = set.StringLength;
		if (length == 0) return true;
		if (set.HasActiveGadget == 0) return true;
		plan.HasActiveButton = 1;
		plan.ActiveButton = set.ActiveGadgetOrdinal + 1 == plan.GadgetCount
			? 0u : set.ActiveGadgetOrdinal + 1;
		if (length == uint.MaxValue) return false;
		var allocationSize = length + 1;
		var output = platform.Allocate(allocationSize,
			MuiHeadlessLayout.AllocationFlags);
		if (output.IsNull) return false;
		if (!platform.IsMapped(output, allocationSize))
		{
			platform.Free(output, allocationSize);
			return false;
		}

		var outputCursor = default(MuiRequesterOutputByteCursor);
		outputCursor.Base = output;
		outputCursor.Capacity = allocationSize;
		var segmentStart = true;
		var outputIndex = 0u;
		for (var index = 0u; index < length; index++)
		{
			if (!TryReadGadgetByte(ref platform, gadgets, length, index,
				out var value))
			{
				platform.Free(output, allocationSize);
				return false;
			}
			if (segmentStart && value == (byte)'*')
			{
				segmentStart = false;
				continue;
			}
			outputCursor.Index = outputIndex++;
			if (!MuiRequesterOutputByteCursorCodec.TryWriteByte(ref platform,
				outputCursor, value))
			{
				platform.Free(output, allocationSize);
				return false;
			}
			segmentStart = value == (byte)'|';
		}
		outputCursor.Index = outputIndex;
		if (!MuiRequesterOutputByteCursorCodec.TryWriteByte(ref platform,
			outputCursor, 0))
		{
			platform.Free(output, allocationSize);
			return false;
		}
		plan.GadgetFormat = output;
		plan.AllocationSize = allocationSize;
		return true;
	}

	internal static void Release<TPlatform>(ref TPlatform platform,
		MuiRequesterSystemGadgetPlan plan)
		where TPlatform : struct, IMuiAllocationPlatform
	{
		if (plan.AllocationSize != 0 && plan.GadgetFormat.IsNotNull)
			platform.Free(plan.GadgetFormat, plan.AllocationSize);
	}

	private static bool TryReadGadgetByte<TPlatform>(ref TPlatform platform,
		APTR gadgets, uint length, uint index, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiRequesterPayloadByteCursor);
		cursor.Base = gadgets;
		cursor.Length = length;
		cursor.Index = index;
		return MuiRequesterPayloadByteCursorCodec.TryReadByte(ref platform,
			cursor, out value);
	}
}
