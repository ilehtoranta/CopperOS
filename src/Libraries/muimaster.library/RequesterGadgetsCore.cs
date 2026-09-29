/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Parsed view of one MUI requester button. The label remains in the caller's
// guest string; its range, result ID, active marker, and keyboard shortcut are
// carried as named fields for a presenter to consume without a second ad-hoc
// string walk.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRequesterButtonRecord
{
	internal APTR Source;
	internal uint SourceLength;
	internal uint LabelStart;
	internal uint LabelLength;
	internal uint ReturnId;
	internal uint ControlChar;
	internal uint IsActive;
}

// One bounded scan of the MUI Request gadget grammar. Button order is the
// source order; the final button maps to ReturnID zero, while earlier buttons
// map to one-based IDs. The active marker is allowed once and only at the
// beginning of a button segment.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRequesterGadgetSetRecord
{
	internal APTR Source;
	internal uint StringLength;
	internal uint GadgetCount;
	internal uint ActiveGadgetOrdinal;
	internal uint HasActiveGadget;
}

internal static class MuiRequesterGadgetSetCore
{
	internal static bool TryParse<TPlatform>(ref TPlatform platform,
		APTR gadgets, out MuiRequesterGadgetSetRecord set)
		where TPlatform : struct, IMuiGuestMemory
	{
		set = default;
		set.Source = gadgets;
		if (gadgets.IsNull) return true;
		if (!CStringCodec.TryReadLength(ref platform, gadgets,
			MuiRequesterPayloadCore.MaximumStringLength, out var length))
			return false;
		set.StringLength = length;
		if (length == 0) return true;

		set.GadgetCount = 1;
		var segmentStart = true;
		var activeSeen = false;
		for (var index = 0u; index < length; index++)
		{
			if (!TryReadByte(ref platform, gadgets, length, index, out var value))
				return false;
			if (value == (byte)'|')
			{
				if (set.GadgetCount == uint.MaxValue) return false;
				set.GadgetCount++;
				segmentStart = true;
				continue;
			}
			if (segmentStart && value == (byte)'*')
			{
				if (activeSeen) return false;
				activeSeen = true;
				set.HasActiveGadget = 1;
				set.ActiveGadgetOrdinal = set.GadgetCount - 1;
			}
			segmentStart = false;
		}
		return true;
	}

	internal static bool TryGetButton<TPlatform>(ref TPlatform platform,
		MuiRequesterGadgetSetRecord set, uint ordinal,
		out MuiRequesterButtonRecord button)
		where TPlatform : struct, IMuiGuestMemory
	{
		button = default;
		if (set.Source.IsNull || ordinal >= set.GadgetCount ||
			!TryParse(ref platform, set.Source, out var current) ||
			!SameSet(set, current)) return false;

		var segmentOrdinal = 0u;
		var segmentStart = 0u;
		var active = false;
		for (var index = 0u; index <= current.StringLength; index++)
		{
			var delimiter = index == current.StringLength;
			var value = (byte)0;
			if (!delimiter)
			{
				if (!TryReadByte(ref platform, current.Source,
					current.StringLength, index, out value)) return false;
				delimiter = value == (byte)'|';
			}
			if (delimiter)
			{
				if (segmentOrdinal == ordinal)
				{
					var labelStart = segmentStart + (active ? 1u : 0u);
					var buttonLength = index - segmentStart;
					var labelLength = buttonLength - (active ? 1u : 0u);
					button.Source = current.Source;
					button.SourceLength = current.StringLength;
					button.LabelStart = labelStart;
					button.LabelLength = labelLength;
					button.ReturnId = ordinal + 1 == current.GadgetCount
						? 0u : ordinal + 1;
					button.IsActive = active ? 1u : 0u;
					return TryGetControlChar(ref platform, button,
						out button.ControlChar);
				}
				segmentOrdinal++;
				segmentStart = index + 1;
				active = false;
				continue;
			}
			if (index == segmentStart && value == (byte)'*') active = true;
		}
		return false;
	}

	internal static bool TryReadLabelByte<TPlatform>(ref TPlatform platform,
		MuiRequesterButtonRecord button, uint labelOffset, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (labelOffset >= button.LabelLength ||
			labelOffset > uint.MaxValue - button.LabelStart) return false;
		return TryReadByte(ref platform, button.Source,
			button.SourceLength, button.LabelStart + labelOffset, out value);
	}

	private static bool TryGetControlChar<TPlatform>(ref TPlatform platform,
		MuiRequesterButtonRecord button, out uint controlChar)
		where TPlatform : struct, IMuiGuestMemory
	{
		controlChar = 0;
		for (var index = 0u; index < button.LabelLength; index++)
		{
			if (!TryReadLabelByte(ref platform, button, index, out var value))
				return false;
			if (value != (byte)'_') continue;
			if (index == uint.MaxValue || index + 1 >= button.LabelLength)
				return true;
			if (!TryReadLabelByte(ref platform, button, index + 1,
				out var key)) return false;
			controlChar = key;
			return true;
		}
		return true;
	}

	private static bool TryReadByte<TPlatform>(ref TPlatform platform,
		APTR source, uint length, uint index, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiRequesterPayloadByteCursorCodec.TryCreate(ref platform, source,
			length, out var cursor)) return false;
		cursor.Index = index;
		return MuiRequesterPayloadByteCursorCodec.TryReadByte(ref platform,
			cursor, out value);
	}

	private static bool SameSet(MuiRequesterGadgetSetRecord left,
		MuiRequesterGadgetSetRecord right) => left.Source == right.Source &&
		left.StringLength == right.StringLength &&
		left.GadgetCount == right.GadgetCount &&
		left.ActiveGadgetOrdinal == right.ActiveGadgetOrdinal &&
		left.HasActiveGadget == right.HasActiveGadget;
}
