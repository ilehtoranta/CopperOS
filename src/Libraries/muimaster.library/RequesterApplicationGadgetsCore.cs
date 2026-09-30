/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// One owned, NUL-terminated label and its MUI_RequestA result metadata. The
// label pointer targets a separate guest byte arena; fixed fields are always
// serialized through this named record codec.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRequesterApplicationButtonRecord
{
	internal const uint Size = 24;
	internal APTR Label;
	internal uint LabelLength;
	internal uint ReturnId;
	internal uint ControlChar;
	internal uint IsActive;
	internal APTR Object;
}

// Owned scratch for the app-backed MUI requester presenter. These buffers are
// valid only during the synchronous MUI_RequestA call and are released through
// one idempotent cleanup operation.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRequesterApplicationGadgetPlanRecord
{
	internal APTR Buttons;
	internal APTR Labels;
	internal uint ButtonsSize;
	internal uint LabelsSize;
	internal uint ButtonCount;
	internal uint ActiveReturnId;
	internal uint HasActiveButton;
}

internal static class MuiRequesterApplicationButtonArrayCodec
{
	internal static bool TryGetRecordAddress<TPlatform>(ref TPlatform platform,
		APTR array, uint count, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (array.IsNull || index >= count ||
			count > uint.MaxValue / MuiRequesterApplicationButtonRecord.Size)
			return false;
		var arraySize = count * MuiRequesterApplicationButtonRecord.Size;
		var byteIndex = index * MuiRequesterApplicationButtonRecord.Size;
		if (byteIndex > arraySize || MuiRequesterApplicationButtonRecord.Size >
			arraySize - byteIndex || !platform.IsMapped(array, arraySize) ||
			array.Raw > uint.MaxValue - byteIndex) return false;
		address = APTR.FromPointer(array.Raw + byteIndex);
		return platform.IsMapped(address,
			MuiRequesterApplicationButtonRecord.Size);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR array,
		uint count, uint index, out MuiRequesterApplicationButtonRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!TryGetRecordAddress(ref platform, array, count, index,
			out var address) ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiRequesterApplicationButtonRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var label) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.LabelLength) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.ReturnId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.ControlChar) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.IsActive) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var buttonObject) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		record.Label = APTR.FromPointer(label);
		record.Object = APTR.FromPointer(buttonObject);
		return record.Label.IsNotNull && record.IsActive <= 1 &&
			record.LabelLength != uint.MaxValue &&
			platform.IsMapped(record.Label, record.LabelLength + 1) &&
			(record.Object.IsNull || platform.IsMapped(record.Object, 1));
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform, APTR array,
		uint count, uint index, MuiRequesterApplicationButtonRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (record.Label.IsNull || record.LabelLength == uint.MaxValue ||
			record.IsActive > 1 ||
			!platform.IsMapped(record.Label, record.LabelLength + 1) ||
			!TryGetRecordAddress(ref platform, array, count, index,
				out var address) ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiRequesterApplicationButtonRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Label.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.LabelLength) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.ReturnId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.ControlChar) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.IsActive) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Object.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiRequesterApplicationGadgetPlanCore
{
	internal static bool TryPrepare<TPlatform>(ref TPlatform platform,
		APTR gadgets, out MuiRequesterApplicationGadgetPlanRecord plan)
		where TPlatform : struct, IMuiAllocationPlatform
	{
		plan = default;
		if (!MuiRequesterGadgetSetCore.TryParse(ref platform, gadgets,
			out var set)) return false;
		plan.ButtonCount = set.GadgetCount;
		plan.HasActiveButton = set.HasActiveGadget;
		if (set.HasActiveGadget != 0)
		{
			plan.ActiveReturnId = set.ActiveGadgetOrdinal + 1 ==
				set.GadgetCount ? 0u : set.ActiveGadgetOrdinal + 1;
		}
		if (set.GadgetCount == 0) return true;
		if (set.GadgetCount > uint.MaxValue /
			MuiRequesterApplicationButtonRecord.Size ||
			set.StringLength == uint.MaxValue) return Fail(ref platform,
				ref plan);

		plan.ButtonsSize = set.GadgetCount *
			MuiRequesterApplicationButtonRecord.Size;
		plan.LabelsSize = set.StringLength + 1;
		plan.Buttons = platform.Allocate(plan.ButtonsSize,
			MuiHeadlessLayout.AllocationFlags);
		if (plan.Buttons.IsNull || !platform.IsMapped(plan.Buttons,
			plan.ButtonsSize)) return Fail(ref platform, ref plan);
		plan.Labels = platform.Allocate(plan.LabelsSize,
			MuiHeadlessLayout.AllocationFlags);
		if (plan.Labels.IsNull || !platform.IsMapped(plan.Labels,
			plan.LabelsSize)) return Fail(ref platform, ref plan);

		var labels = default(MuiRequesterOutputByteCursor);
		labels.Base = plan.Labels;
		labels.Capacity = plan.LabelsSize;
		var segmentStart = 0u;
		var buttonOrdinal = 0u;
		var activeOrdinal = uint.MaxValue;
		var populated = false;
		for (var index = 0u; index <= set.StringLength; index++)
		{
			var atEnd = index == set.StringLength;
			var value = (byte)0;
			if (!atEnd && !TryReadSourceByte(ref platform, gadgets,
				set.StringLength, index, out value)) return Fail(ref platform,
					ref plan);
			if (!atEnd && value != (byte)'|') continue;

			var active = false;
			if (index > segmentStart)
			{
				if (!TryReadSourceByte(ref platform, gadgets, set.StringLength,
					segmentStart, out var first)) return Fail(ref platform, ref plan);
				active = first == (byte)'*';
			}
			if (active)
			{
				if (activeOrdinal != uint.MaxValue) return Fail(ref platform,
					ref plan);
				activeOrdinal = buttonOrdinal;
			}

			var labelStart = segmentStart + (active ? 1u : 0u);
			var labelLength = index - labelStart;
			var button = new MuiRequesterApplicationButtonRecord();
			if (labels.Index >= plan.LabelsSize ||
				plan.Labels.Raw > uint.MaxValue - labels.Index)
				return Fail(ref platform, ref plan);
			button.Label = APTR.FromPointer(plan.Labels.Raw + labels.Index);
			button.LabelLength = labelLength;
			button.ReturnId = buttonOrdinal + 1 == set.GadgetCount
				? 0u : buttonOrdinal + 1;
			button.IsActive = active ? 1u : 0u;
			if (!TryGetControlChar(ref platform, gadgets, set.StringLength,
				labelStart, labelLength, out button.ControlChar))
				return Fail(ref platform, ref plan);
			var labelOffset = labels.Index;
			for (var labelIndex = 0u; labelIndex < labelLength; labelIndex++)
			{
				if (!TryReadSourceByte(ref platform, gadgets, set.StringLength,
					labelStart + labelIndex, out value)) return Fail(ref platform,
						ref plan);
				labels.Index = labelOffset + labelIndex;
				if (!MuiRequesterOutputByteCursorCodec.TryWriteByte(ref platform,
					labels, value)) return Fail(ref platform, ref plan);
			}
			labels.Index = labelOffset + labelLength;
			if (!MuiRequesterOutputByteCursorCodec.TryWriteByte(ref platform,
				labels, 0) ||
				!MuiRequesterApplicationButtonArrayCodec.TryWrite(ref platform,
					plan.Buttons, plan.ButtonCount, buttonOrdinal, button))
				return Fail(ref platform, ref plan);
			labels.Index++;
			buttonOrdinal++;
			segmentStart = index + 1;
			populated = true;
		}

		if (!populated || buttonOrdinal != set.GadgetCount ||
			(activeOrdinal != uint.MaxValue) != (set.HasActiveGadget != 0) ||
			(activeOrdinal != uint.MaxValue &&
				activeOrdinal != set.ActiveGadgetOrdinal))
			return Fail(ref platform, ref plan);
		return true;
	}

	internal static void Release<TPlatform>(ref TPlatform platform,
		ref MuiRequesterApplicationGadgetPlanRecord plan)
		where TPlatform : struct, IMuiAllocationPlatform
	{
		if (plan.Buttons.IsNotNull && plan.ButtonsSize != 0)
			platform.Free(plan.Buttons, plan.ButtonsSize);
		if (plan.Labels.IsNotNull && plan.LabelsSize != 0)
			platform.Free(plan.Labels, plan.LabelsSize);
		plan = default;
	}

	private static bool Fail<TPlatform>(ref TPlatform platform,
		ref MuiRequesterApplicationGadgetPlanRecord plan)
		where TPlatform : struct, IMuiAllocationPlatform
	{
		Release(ref platform, ref plan);
		return false;
	}

	private static bool TryGetControlChar<TPlatform>(ref TPlatform platform,
		APTR source, uint sourceLength, uint labelStart, uint labelLength,
		out uint controlChar)
		where TPlatform : struct, IMuiGuestMemory
	{
		controlChar = 0;
		for (var index = 0u; index < labelLength; index++)
		{
			if (!TryReadSourceByte(ref platform, source, sourceLength,
				labelStart + index, out var value)) return false;
			if (value != (byte)'_') continue;
			if (index + 1 >= labelLength) return true;
			if (!TryReadSourceByte(ref platform, source, sourceLength,
				labelStart + index + 1, out value)) return false;
			controlChar = value;
			return true;
		}
		return true;
	}

	private static bool TryReadSourceByte<TPlatform>(ref TPlatform platform,
		APTR source, uint sourceLength, uint index, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (index >= sourceLength) return false;
		var cursor = default(MuiRequesterPayloadByteCursor);
		cursor.Base = source;
		cursor.Length = sourceLength;
		cursor.Index = index;
		return MuiRequesterPayloadByteCursorCodec.TryReadByte(ref platform,
			cursor, out value);
	}
}
