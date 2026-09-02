/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Named cursor for bounded FixWidthTxt/FixHeightTxt sample strings. The
// measurement consumer carries only a guest STRPTR and logical byte index;
// this adapter owns the 4 KiB bound, overflow guard, and mapped-byte check.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaFixedTextStringByteCursor
{
	internal const uint MaximumLength = 4096;
	internal APTR Text;
	internal uint Index;
}

internal static class MuiAreaFixedTextStringByteCursorCodec
{
	internal static bool TryReadAt<TPlatform>(ref TPlatform platform,
		APTR text, int index, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (index < 0) return false;
		var cursor = default(MuiAreaFixedTextStringByteCursor);
		cursor.Text = text;
		cursor.Index = (uint)index;
		return TryReadByte(ref platform, cursor, out value);
	}

	internal static bool TryGetByte<TPlatform>(ref TPlatform platform,
		MuiAreaFixedTextStringByteCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Text;
		shared.Index = cursor.Index;
		shared.Limit = MuiAreaFixedTextStringByteCursor.MaximumLength;
		return MuiCStringByteCursorCodec.TryGetAddress(ref platform, shared,
			out address);
	}

	internal static bool TryReadByte<TPlatform>(ref TPlatform platform,
		MuiAreaFixedTextStringByteCursor cursor, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetByte(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt8(address, 0);
		return true;
	}
}

// MUIA_FixWidthTxt and MUIA_FixHeightTxt are initializer-only STRPTRs.  Keep
// the copied samples and their public projection in one named guest record so
// sizing never depends on a private object offset or a managed string.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaFixedTextStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint WidthTextOffset = 4;
	internal const uint HeightTextOffset = 8;
	internal const uint GenerationOffset = 12;
	internal const uint Cookie = 0x41465458u; // 'AFTX'

	internal uint Magic;
	internal APTR WidthText;
	internal APTR HeightText;
	internal uint Generation;
}

internal static class MuiAreaFixedTextStateAdmission
{
	internal static bool Validate(MuiAreaFixedTextStateRecord value) =>
		value.Magic == MuiAreaFixedTextStateRecord.Cookie &&
		value.Generation != 0;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaFixedTextStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(value) || obj.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		return (value.WidthText.IsNull || CStringCodec.TryReadLength(ref platform,
			value.WidthText, 4096, out _)) &&
			(value.HeightText.IsNull || CStringCodec.TryReadLength(ref platform,
				value.HeightText, 4096, out _));
	}
}

internal enum MuiAreaFixedTextStateField : byte
{
	Magic,
	WidthText,
	HeightText,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaFixedTextStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaFixedTextStateField Field;
}

internal static class MuiAreaFixedTextStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaFixedTextStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaFixedTextStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFixedTextStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaFixedTextStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFixedTextStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaFixedTextStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area FixWidthTxt/FixHeightTxt state is transferred as a named record.
// Numeric guest positions are confined to this ABI adapter; the compatibility
// cursor above remains available only to legacy callers and malformed-state
// diagnostics.
internal static class MuiAreaFixedTextStateRecordMemoryCodec
{
	private static bool TryResolve(MuiAreaFixedTextStateField field,
		out uint offset)
	{
		if (field == MuiAreaFixedTextStateField.Magic)
			offset = MuiAreaFixedTextStateRecord.MagicOffset;
		else if (field == MuiAreaFixedTextStateField.WidthText)
			offset = MuiAreaFixedTextStateRecord.WidthTextOffset;
		else if (field == MuiAreaFixedTextStateField.HeightText)
			offset = MuiAreaFixedTextStateRecord.HeightTextOffset;
		else if (field == MuiAreaFixedTextStateField.Generation)
			offset = MuiAreaFixedTextStateRecord.GenerationOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFixedTextStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiAreaFixedTextStateRecord.Size) &&
			platform.IsMapped(address, MuiAreaFixedTextStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFixedTextStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaFixedTextStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaFixedTextStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaFixedTextStateField.WidthText)
			value = state.WidthText.Raw;
		else if (field == MuiAreaFixedTextStateField.HeightText)
			value = state.HeightText.Raw;
		else if (field == MuiAreaFixedTextStateField.Generation)
			value = state.Generation;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFixedTextStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaFixedTextStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaFixedTextStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaFixedTextStateField.WidthText)
			state.WidthText = APTR.FromPointer(value);
		else if (field == MuiAreaFixedTextStateField.HeightText)
			state.HeightText = APTR.FromPointer(value);
		else if (field == MuiAreaFixedTextStateField.Generation)
			state.Generation = value;
		else return false;
		return MuiAreaFixedTextStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiAreaFixedTextStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaFixedTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaFixedTextStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var widthText) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var heightText) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Generation)) return false;
		value.WidthText = APTR.FromPointer(widthText);
		value.HeightText = APTR.FromPointer(heightText);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaFixedTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaFixedTextStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.WidthText.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.HeightText.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Generation) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaFixedTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaFixedTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadStructural(ref platform, address, out value) &&
			MuiAreaFixedTextStateAdmission.Validate(value);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaFixedTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiAreaFixedTextStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}

internal static class MuiAreaFixedTextCore
{
	internal const uint StateKey = 0x7F070044u;
	// Keep owned sample buffers separate from Area Text/Font records. These
	// keys deliberately do not overlap the BuiltinFont state key (0x...46).
	private const uint WidthCopyKey = 0x7F07004Bu;
	private const uint HeightCopyKey = 0x7F07004Cu;
	private const uint MaximumTextLength = 4096;

	internal static bool Initialize<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (obj.IsNull || MuiHeadlessObjectCore.FindObject(ref platform, state,
			obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj, StateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj, StateKey);
		if ((block.IsNotNull || length != 0) &&
			!TryReadStateRecord(ref platform, state, obj, out _)) return false;
		if (!CopySample(ref platform, state, obj,
			MuiCommonControlCore.FixWidthTxt, WidthCopyKey, out var width) ||
			!CopySample(ref platform, state, obj,
			MuiCommonControlCore.FixHeightTxt, HeightCopyKey, out var height))
			return false;
		if (width.IsNotNull && !MuiHeadlessObjectCore.SetAttribute(ref platform,
			state, obj, MuiCommonControlCore.FixWidthTxt, width.Raw, false))
			return false;
		if (height.IsNotNull && !MuiHeadlessObjectCore.SetAttribute(ref platform,
			state, obj, MuiCommonControlCore.FixHeightTxt, height.Raw, false))
			return false;
		return WriteState(ref platform, state, obj, width, height, 1);
	}

	internal static bool TryReadState<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaFixedTextStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (obj.IsNull || MuiHeadlessObjectCore.FindObject(ref platform, state,
			obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj, StateKey);
		if (block.IsNotNull || length != 0)
		{
			if (length != unchecked((int)MuiAreaFixedTextStateRecord.Size) ||
				!MuiAreaFixedTextStateRecordCodec.TryReadStructural(ref platform, block,
					out value) || !MuiAreaFixedTextStateAdmission.ValidateLive(ref platform,
					state, obj, value)) return false;
			var width = ReadRaw(ref platform, state, obj,
				MuiCommonControlCore.FixWidthTxt);
			var height = ReadRaw(ref platform, state, obj,
				MuiCommonControlCore.FixHeightTxt);
			if (width == value.WidthText.Raw && height == value.HeightText.Raw)
				return true;
			return Initialize(ref platform, state, obj) &&
				TryReadState(ref platform, state, obj, out value);
		}
		return Initialize(ref platform, state, obj) &&
			TryReadState(ref platform, state, obj, out value);
	}

	private static bool TryReadStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaFixedTextStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (obj.IsNull || MuiHeadlessObjectCore.FindObject(ref platform, state,
			obj).IsNull) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj, StateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj, StateKey);
		if (block.IsNull && length == 0) return false;
		return length == unchecked((int)MuiAreaFixedTextStateRecord.Size) &&
			MuiAreaFixedTextStateRecordCodec.TryReadStructural(ref platform, block,
				out value) && MuiAreaFixedTextStateAdmission.ValidateLive(ref platform,
				state, obj, value);
	}

	internal static bool WriteState<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR width, APTR height, uint generation)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = default(MuiAreaFixedTextStateRecord);
		record.Magic = MuiAreaFixedTextStateRecord.Cookie;
		record.WidthText = width;
		record.HeightText = height;
		record.Generation = generation;
		if (!MuiAreaFixedTextStateAdmission.ValidateLive(ref platform, state, obj,
			record)) return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaFixedTextStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaFixedTextStateRecord.Size);
		var written = MuiAreaFixedTextStateRecordCodec.Write(ref platform, scratch,
			record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, unchecked((int)MuiAreaFixedTextStateRecord.Size));
		platform.Clear(scratch, MuiAreaFixedTextStateRecord.Size);
		platform.Free(scratch, MuiAreaFixedTextStateRecord.Size);
		return stored;
	}

	internal static bool TryMeasure<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, bool width, out int extent)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		extent = 0;
		if (!TryReadState(ref platform, state, obj, out var value)) return false;
		var text = width ? value.WidthText : value.HeightText;
		if (text.IsNull) return false;
		var maxWidth = 0;
		var lineLength = 0;
		var lineCount = 1;
		for (var index = 0u; index < MaximumTextLength; index++)
		{
			if (!MuiAreaFixedTextStringByteCursorCodec.TryReadAt(ref platform,
				text, unchecked((int)index), out var ch)) return false;
			if (ch == 0)
			{
				maxWidth = Larger(maxWidth, lineLength);
				break;
			}
			if (ch == (byte)'\n')
			{
				maxWidth = Larger(maxWidth, lineLength);
				lineCount++;
				lineLength = 0;
				continue;
			}
			lineLength++;
		}
		extent = width ? maxWidth * 8 : lineCount * 10;
		return extent > 0;
	}

	private static int Larger(int left, int right) => left > right ? left : right;

	private static bool CopySample<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint copyKey, out APTR copy)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		copy = APTR.Null;
		var raw = ReadRaw(ref platform, state, obj, attribute);
		if (raw == 0) return true;
		var source = APTR.FromPointer(raw);
		if (!CStringCodec.TryReadLength(ref platform, source, MaximumTextLength,
			out var length)) return false;
		if (!MuiStoreCore.DataspaceAdd(ref platform, state, obj, copyKey, source,
			unchecked((int)length + 1))) return false;
		copy = MuiStoreCore.DataspaceFind(ref platform, state, obj, copyKey);
		return copy.IsNotNull;
	}

	private static uint ReadRaw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute) where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, attribute,
			out var value) ? value : 0;
}
