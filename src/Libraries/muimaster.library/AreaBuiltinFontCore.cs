/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_BuiltinFont is the typed selector form of an Area font choice. The
// selector remains an opaque ULONG on the guest bus; the effective-font
// resolver turns non-zero selectors into the same concrete ABI value used by
// MUIA_Font, while zero continues the documented parent inheritance walk.
internal static class MuiAreaBuiltinFontCore
{
	internal const uint StateKey = 0x7F070046u;

	internal static bool Initialize<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var present = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			MuiCommonControlCore.BuiltinFont, out var selector);
		return WriteState(ref platform, state, obj, selector,
			present ? 1u : 0u, 1);
	}

	internal static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaBuiltinFontState value)
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
		var present = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			MuiCommonControlCore.BuiltinFont, out var selector);
		var normalizedPresent = present ? 1u : 0u;
		if (record.Present != normalizedPresent || (present &&
			record.Selector != selector))
		{
			record.Present = normalizedPresent;
			if (present) record.Selector = selector;
			record.Generation = record.Generation == uint.MaxValue ? 1u :
				record.Generation + 1u;
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StateKey);
			if (!MuiAreaBuiltinFontStateRecordCodec.Write(ref platform, block,
				record)) return false;
		}
		value.Selector = record.Selector;
		value.Present = record.Present;
		value.Generation = record.Generation;
		return true;
	}

	internal static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out uint selector)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		selector = 0;
		if (!TryReadState(ref platform, state, obj, out var value)) return false;
		selector = value.Present == 0 ? 0u : value.Selector;
		return true;
	}

	private static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaBuiltinFontStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj, StateKey) !=
			unchecked((int)MuiAreaBuiltinFontStateRecord.Size)) return false;
		return MuiAreaBuiltinFontStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	internal static bool WriteState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint selector, uint present, uint generation)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaBuiltinFontStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaBuiltinFontStateRecord.Size);
		var record = default(MuiAreaBuiltinFontStateRecord);
		record.Magic = MuiAreaBuiltinFontStateRecord.Cookie;
		record.Selector = selector;
		record.Present = present == 0 ? 0u : 1u;
		record.Generation = generation == 0 ? 1u : generation;
		var written = MuiAreaBuiltinFontStateRecordCodec.Write(ref platform, scratch,
			record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, unchecked((int)MuiAreaBuiltinFontStateRecord.Size));
		platform.Clear(scratch, MuiAreaBuiltinFontStateRecord.Size);
		platform.Free(scratch, MuiAreaBuiltinFontStateRecord.Size);
		return stored;
	}
}

// Public value-type seam for construction-independent callers. The underlying
// MUI selector remains a ULONG and is never represented by a managed enum.
public static class MuiAreaBuiltinFontPacketCore
{
	public static bool Set<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint selector, bool notify = true)
		where TPlatform : struct, IMuiLayoutPlatform =>
		MuiCommonControlCore.SetControlAttribute(ref platform, state, obj,
			MuiCommonControlCore.BuiltinFont, selector, notify);

	public static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaBuiltinFontState value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaBuiltinFontCore.TryReadState(ref platform, state, obj, out value);
}
