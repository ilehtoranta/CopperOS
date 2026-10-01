/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// MorphOS uses a zero MUIA_Font value (MUIV_Font_Inherit) when an Area does
// not select its own font.  Keep the resolved result as a named value type so
// callers can distinguish an inherited font from an explicit local value
// without constructing a managed object hierarchy.
public struct MuiControlFontResolution
{
	public bool Present;
	public bool Inherited;
	public uint Depth;
	public APTR Font;
	// A valid CustomFont selection is reported separately from the fallback
	// TextFont pointer. This lets a renderer consume the parsed value later
	// without replacing the native ABI pointer with a managed font object.
	public uint Custom;
	public MuiCustomFontSpec CustomSpec;
}

// A bounded guest projection used by native qualification and future render
// caches. The resolver itself remains uncached because Family mutations can
// change the effective parent font at any time.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiControlFontResolutionRecord
{
	public const uint Size = 20;
	public const uint FieldSize = 4;
	// ABI/documentation aliases only. Field access advances the named record
	// with MuiGuestStructCursor below.
	public const uint MagicOffset = 0;
	public const uint PresentOffset = 4;
	public const uint InheritedOffset = 8;
	public const uint DepthOffset = 12;
	public const uint FontOffset = 16;
	public const uint Cookie = 0x4D434652u; // 'MCFR'

	public uint Magic;
	public uint Present;
	public uint Inherited;
	public uint Depth;
	public APTR Font;
}

internal enum MuiControlFontResolutionRecordField : byte
{
	Magic,
	Present,
	Inherited,
	Depth,
	Font,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiControlFontResolutionRecordFieldCursor
{
	internal APTR Record;
	internal MuiControlFontResolutionRecordField Field;
}

internal static class MuiControlFontResolutionRecordFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiControlFontResolutionRecordFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiControlFontResolutionRecordFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiControlFontResolutionRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiControlFontResolutionRecordField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiControlFontResolutionRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiControlFontResolutionRecordField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiControlFontResolutionRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
}

// Struct-first adapter for the effective-font projection. The resolver and
// callers exchange a named record; this bounded layer alone translates its
// five 68k LONG fields in guest memory.
internal static class MuiControlFontResolutionRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(
		MuiControlFontResolutionRecordField field, out uint index)
	{
		if (field == MuiControlFontResolutionRecordField.Magic)
			index = 0;
		else if (field == MuiControlFontResolutionRecordField.Present)
			index = 1;
		else if (field == MuiControlFontResolutionRecordField.Inherited)
			index = 2;
		else if (field == MuiControlFontResolutionRecordField.Depth)
			index = 3;
		else if (field == MuiControlFontResolutionRecordField.Font)
			index = 4;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiControlFontResolutionRecordField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiControlFontResolutionRecordFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiControlFontResolutionRecordFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiControlFontResolutionRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiControlFontResolutionRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiControlFontResolutionRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiControlFontResolutionRecordField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiControlFontResolutionRecordStructCodec.TryReadStructural(
			ref platform, record, out var state)) return false;
		if (field == MuiControlFontResolutionRecordField.Magic)
			value = state.Magic;
		else if (field == MuiControlFontResolutionRecordField.Present)
			value = state.Present;
		else if (field == MuiControlFontResolutionRecordField.Inherited)
			value = state.Inherited;
		else if (field == MuiControlFontResolutionRecordField.Depth)
			value = state.Depth;
		else if (field == MuiControlFontResolutionRecordField.Font)
			value = state.Font.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiControlFontResolutionRecordField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiControlFontResolutionRecordStructCodec.TryReadStructural(
			ref platform, record, out var state)) return false;
		if (field == MuiControlFontResolutionRecordField.Magic)
			state.Magic = value;
		else if (field == MuiControlFontResolutionRecordField.Present)
			state.Present = value;
		else if (field == MuiControlFontResolutionRecordField.Inherited)
			state.Inherited = value;
		else if (field == MuiControlFontResolutionRecordField.Depth)
			state.Depth = value;
		else if (field == MuiControlFontResolutionRecordField.Font)
			state.Font = APTR.FromPointer(value);
		else return false;
		return MuiControlFontResolutionRecordStructCodec.Write(ref platform,
			record, state);
	}
}

// Sequential codec for the complete effective-font projection. The record is
// five declaration-ordered ULONGs; each member uses the shared named ULONG
// record codec so the freestanding path preserves the complete 32-bit pointer
// range, including values with bit 31 set.
internal static class MuiControlFontResolutionRecordStructCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool TryReadUlong<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiGuestUlongStorage.Size, out var address)) return false;
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool TryWriteUlong<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiGuestUlongStorage.Size, out var address)) return false;
		return MuiGuestUlongStorageCodec.WriteValue(ref platform, address, value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiControlFontResolutionRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiControlFontResolutionRecord.Size, out var cursor) ||
			!TryReadUlong(ref platform, ref cursor, out var magic) ||
			!TryReadUlong(ref platform, ref cursor, out var present) ||
			!TryReadUlong(ref platform, ref cursor, out var inherited) ||
			!TryReadUlong(ref platform, ref cursor, out var depth) ||
			!TryReadUlong(ref platform, ref cursor, out var font) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Magic = magic;
		value.Present = present;
		value.Inherited = inherited;
		value.Depth = depth;
		value.Font = APTR.FromPointer(font);
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiControlFontResolutionRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryRead(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiControlFontResolutionRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiControlFontResolutionRecord.Size, out var cursor) ||
			!TryWriteUlong(ref platform, ref cursor, value.Magic) ||
			!TryWriteUlong(ref platform, ref cursor, value.Present) ||
			!TryWriteUlong(ref platform, ref cursor, value.Inherited) ||
			!TryWriteUlong(ref platform, ref cursor, value.Depth) ||
			!TryWriteUlong(ref platform, ref cursor, value.Font.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

public static class MuiControlFontResolutionRecordCodec
{
	public static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiControlFontResolutionRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiControlFontResolutionRecordStructCodec.TryRead(ref platform,
			address, out value)) return false;
		return value.Magic == MuiControlFontResolutionRecord.Cookie &&
			value.Present <= 1 && value.Inherited <= 1;
	}

	public static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiControlFontResolutionRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (value.Magic != MuiControlFontResolutionRecord.Cookie ||
			value.Present > 1 || value.Inherited > 1) return false;
		return MuiControlFontResolutionRecordStructCodec.Write(ref platform,
			address, value);
	}
}

public static class MuiControlFontResolutionCore
{
	public const uint MUIV_Font_Inherit = 0;
	private const uint Font = 0x8042BE50;

	// Resolve the effective Area font through guest-resident Family Parent
	// links. MUIA_Font and MUIA_CustomFont use the named last-writer record; a
	// valid CustomFont spec is returned as a value and its TextFont pointer
	// remains a safe fallback until the graphics font capability can open the
	// requested family. No managed tree or exception path is involved.
	public static bool TryResolve<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiControlFontResolution result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;

		var current = obj;
		var inherited = false;
		uint depth = 0;
		while (current.IsNotNull && depth < MuiHeadlessLayout.MaximumTraversal)
		{
			if (MuiAreaFontSelectionCore.TryReadState(ref platform, state, current,
				out var selection) && selection.Active ==
				MuiAreaFontSelectionKind.CustomFont && selection.Source.IsNotNull &&
				MuiCustomFontSpecCore.TryParse(ref platform, selection.Source,
					out var customSpec))
			{
				result.Present = true;
				result.Inherited = inherited;
				result.Depth = depth;
				result.Custom = 1;
				result.CustomSpec = customSpec;
				TryResolveBaseFont(ref platform, state, current,
					out result.Font);
				if (MuiAreaCustomFontCore.TryGetRuntime(ref platform, state, current,
					out var runtime) && runtime.Active != 0 && runtime.Font.IsNotNull)
					result.Font = runtime.Font;
				return true;
			}
			if (TryReadBaseFontAt(ref platform, state, current, out var font))
			{
				result.Present = true;
				result.Inherited = inherited;
				result.Depth = depth;
				result.Font = font;
				return true;
			}

			var parent = MuiHeadlessObjectCore.ParentObject(ref platform, state,
				current);
			if (parent.IsNull || parent.Raw == current.Raw) break;
			current = parent;
			inherited = true;
			depth++;
		}

		result.Inherited = inherited;
		result.Depth = depth;
		return true;
	}

	private static bool TryReadBaseFontAt<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out APTR font)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		font = APTR.Null;
		// MUIA_BuiltinFont is the selector form of MUIA_Font. An explicit zero
		// selector means inherit and suppresses a stale raw Font scalar on the
		// same object; non-zero selectors are concrete ABI values.
		if (MuiAreaBuiltinFontCore.TryReadState(ref platform, state, obj,
			out var builtin) && builtin.Present != 0)
		{
			if (builtin.Selector == MUIV_Font_Inherit) return false;
			font = APTR.FromPointer(builtin.Selector);
			return true;
		}
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			Font, out var rawFont) && rawFont != MUIV_Font_Inherit)
		{
			font = APTR.FromPointer(rawFont);
			return true;
		}
		return false;
	}

	private static void TryResolveBaseFont<TPlatform>(ref TPlatform platform,
		APTR state, APTR start, out APTR font)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		font = APTR.Null;
		var current = start;
		uint depth = 0;
		while (current.IsNotNull && depth < MuiHeadlessLayout.MaximumTraversal)
		{
			if (TryReadBaseFontAt(ref platform, state, current, out font)) return;
			var parent = MuiHeadlessObjectCore.ParentObject(ref platform, state,
				current);
			if (parent.IsNull || parent.Raw == current.Raw) return;
			current = parent;
			depth++;
		}
	}

	internal static bool TryResolveBaseFontForOpen<TPlatform>(
		ref TPlatform platform, APTR state, APTR start, out APTR font)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		TryResolveBaseFont(ref platform, state, start, out font);
		return true;
	}

	internal static bool TryResolveCustomSourceForOpen<TPlatform>(
		ref TPlatform platform, APTR state, APTR start, out APTR source)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		source = APTR.Null;
		var current = start;
		uint depth = 0;
		while (current.IsNotNull && depth < MuiHeadlessLayout.MaximumTraversal)
		{
			if (MuiAreaFontSelectionCore.TryReadState(ref platform, state, current,
				out var selection))
			{
				if (selection.Active == MuiAreaFontSelectionKind.CustomFont &&
					selection.Source.IsNotNull)
				{
					source = selection.Source;
					return true;
				}
				if (selection.Active == MuiAreaFontSelectionKind.Font) return false;
			}
			var parent = MuiHeadlessObjectCore.ParentObject(ref platform, state,
				current);
			if (parent.IsNull || parent.Raw == current.Raw) return false;
			current = parent;
			depth++;
		}
		return false;
	}
}
