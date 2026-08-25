/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident last-writer state for the two Area font selectors. The raw
// attributes remain the ABI storage; this record carries only the semantic
// choice needed by the MorphOS precedence rule.
internal static class MuiAreaFontSelectionCore
{
	internal const uint StateKey = 0x7F070049u;

	internal static bool Initialize<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		SelectFromRaw(ref platform, state, obj, out var active, out var source);
		return WriteState(ref platform, state, obj, active, source, 1);
	}

	internal static bool Mark<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, MuiAreaFontSelectionKind active, APTR source)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (active > MuiAreaFontSelectionKind.CustomFont) return false;
		var generation = 1u;
		if (TryReadRecord(ref platform, state, obj, out var current))
			generation = current.Generation == uint.MaxValue ? 1u : current.Generation + 1u;
		return WriteState(ref platform, state, obj, active, source, generation);
	}

	internal static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaFontSelectionState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		if (!TryReadRecord(ref platform, state, obj, out var record))
		{
			if (!Initialize(ref platform, state, obj) ||
				!TryReadRecord(ref platform, state, obj, out record)) return false;
		}

		// Reconcile caller-owned raw writes that bypass the typed setter. Preserve
		// an explicit typed choice while its source pointer still matches; this is
		// important when the same CustomFont pointer is set again after Font and
		// therefore has no additional raw Attribute generation.
		var matchesRaw = MatchesRaw(ref platform, state, obj,
			(MuiAreaFontSelectionKind)record.Active, record.Source);
		SelectFromRaw(ref platform, state, obj, out var rawActive, out var rawSource);
		if (!matchesRaw && ((MuiAreaFontSelectionKind)record.Active != rawActive ||
			record.Source.Raw != rawSource.Raw))
		{
			record.Active = (uint)rawActive;
			record.Source = rawSource;
			record.Generation = record.Generation == uint.MaxValue ? 1u :
				record.Generation + 1u;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StateKey);
			if (!MuiAreaFontSelectionStateRecordCodec.Write(ref platform, block,
				record)) return false;
		}
		value.Active = (MuiAreaFontSelectionKind)record.Active;
		value.Source = record.Source;
		value.Generation = record.Generation;
		return true;
	}

	private static bool MatchesRaw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, MuiAreaFontSelectionKind active, APTR source)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (active == MuiAreaFontSelectionKind.CustomFont)
			return MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				MuiCommonControlCore.CustomFont, out var custom) && custom != 0 &&
				custom == source.Raw;
		if (active == MuiAreaFontSelectionKind.Font)
			return MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				MuiCommonControlCore.Font, out var font) && font == source.Raw;
		var customPresent = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, MuiCommonControlCore.CustomFont, out var customValue) &&
			customValue != 0;
		return !customPresent && source.IsNull;
	}

	private static void SelectFromRaw<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaFontSelectionKind active, out APTR source)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		active = MuiAreaFontSelectionKind.None;
		source = APTR.Null;
		var customFound = MuiHeadlessObjectCore.GetRawAttributeGeneration(
			ref platform, state, obj, MuiCommonControlCore.CustomFont,
			out var custom, out var customGeneration);
		var fontFound = MuiHeadlessObjectCore.GetRawAttributeGeneration(
			ref platform, state, obj, MuiCommonControlCore.Font, out var font,
			out var fontGeneration);
		if (customFound && custom != 0 && (!fontFound ||
			customGeneration >= fontGeneration))
		{
			active = MuiAreaFontSelectionKind.CustomFont;
			source = APTR.FromPointer(custom);
			return;
		}
		if (fontFound && (!customFound || fontGeneration > customGeneration))
		{
			active = MuiAreaFontSelectionKind.Font;
			source = APTR.FromPointer(font);
		}
	}

	private static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaFontSelectionStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj, StateKey) !=
			unchecked((int)MuiAreaFontSelectionStateRecord.Size)) return false;
		return MuiAreaFontSelectionStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool WriteState<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, MuiAreaFontSelectionKind active, APTR source, uint generation)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaFontSelectionStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaFontSelectionStateRecord.Size);
		var record = default(MuiAreaFontSelectionStateRecord);
		record.Magic = MuiAreaFontSelectionStateRecord.Cookie;
		record.Active = (uint)active;
		record.Source = source;
		record.Generation = generation == 0 ? 1u : generation;
		var written = MuiAreaFontSelectionStateRecordCodec.Write(ref platform,
			scratch, record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, unchecked((int)MuiAreaFontSelectionStateRecord.Size));
		if (!stored)
		{
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StateKey);
			stored = written && MuiAreaFontSelectionStateRecordCodec.Write(ref platform,
				block, record);
		}
		platform.Clear(scratch, MuiAreaFontSelectionStateRecord.Size);
		platform.Free(scratch, MuiAreaFontSelectionStateRecord.Size);
		return stored;
	}
}
