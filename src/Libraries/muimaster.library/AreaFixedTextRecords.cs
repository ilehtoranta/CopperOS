/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_FixWidthTxt and MUIA_FixHeightTxt are initializer-only STRPTRs.  Keep
// the copied samples and their public projection in one named guest record so
// sizing never depends on a private object offset or a managed string.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaFixedTextStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x41465458u; // 'AFTX'

	internal uint Magic;
	internal APTR WidthText;
	internal APTR HeightText;
	internal uint Generation;
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
	private static bool TryResolve(MuiAreaFixedTextStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaFixedTextStateField.Magic:
			case MuiAreaFixedTextStateField.WidthText:
			case MuiAreaFixedTextStateField.HeightText:
			case MuiAreaFixedTextStateField.Generation:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaFixedTextStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiAreaFixedTextStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFixedTextStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaFixedTextStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFixedTextStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaFixedTextStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaFixedTextStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaFixedTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaFixedTextStateRecord.Size) ||
			!MuiAreaFixedTextStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaFixedTextStateField.Magic, out var magic) ||
			magic != MuiAreaFixedTextStateRecord.Cookie ||
			!MuiAreaFixedTextStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaFixedTextStateField.WidthText, out var width) ||
			!MuiAreaFixedTextStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaFixedTextStateField.HeightText, out var height) ||
			!MuiAreaFixedTextStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaFixedTextStateField.Generation, out value.Generation))
			return false;
		value.Magic = magic;
		value.WidthText = APTR.FromPointer(width);
		value.HeightText = APTR.FromPointer(height);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaFixedTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaFixedTextStateRecord.Size) || value.Magic !=
			MuiAreaFixedTextStateRecord.Cookie) return false;
		return MuiAreaFixedTextStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaFixedTextStateField.Magic, value.Magic) &&
			MuiAreaFixedTextStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaFixedTextStateField.WidthText, value.WidthText.Raw) &&
			MuiAreaFixedTextStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaFixedTextStateField.HeightText, value.HeightText.Raw) &&
			MuiAreaFixedTextStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaFixedTextStateField.Generation, value.Generation);
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
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
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
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj, StateKey) ==
			unchecked((int)MuiAreaFixedTextStateRecord.Size) &&
			MuiAreaFixedTextStateRecordCodec.TryRead(ref platform, block, out value))
		{
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

	internal static bool WriteState<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR width, APTR height, uint generation)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaFixedTextStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaFixedTextStateRecord.Size);
		var record = default(MuiAreaFixedTextStateRecord);
		record.Magic = MuiAreaFixedTextStateRecord.Cookie;
		record.WidthText = width;
		record.HeightText = height;
		record.Generation = generation == 0 ? 1u : generation;
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
			if (!platform.IsMapped(text, index + 1)) return false;
			var ch = platform.ReadUInt8(text, unchecked((int)index));
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
