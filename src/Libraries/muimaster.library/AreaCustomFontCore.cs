/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Struct-first projection of MorphOS MUIA_CustomFont. The font-spec string is
// caller-owned and remains an opaque guest C string at this boundary. Opening
// and interpreting the spec is delegated to the value-type native graphics
// capability below; this core deliberately does not create managed font or
// string objects.
internal static class MuiAreaCustomFontCore
{
	internal const uint StateKey = 0x7F070048u;
	internal const uint MaximumSpecLength = 512;
	internal const uint RuntimeStateKey = 0x7F07004Au;
	private const uint IsSetupAttribute = 0x7FFF0002u;

	internal static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaCustomFontStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var present = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			obj, MuiCommonControlCore.CustomFont, out var raw) ? 1u : 0u;
		var spec = APTR.FromPointer(raw);
		if (present != 0 && spec.IsNotNull && !CStringCodec.TryReadLength(
			ref platform, spec, MaximumSpecLength, out _)) return false;

		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj, StateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj, StateKey) ==
			unchecked((int)MuiAreaCustomFontStateRecord.Size) &&
			MuiAreaCustomFontStateRecordCodec.TryRead(ref platform, block,
				out var record))
		{
			if (record.Present != present || (present != 0 &&
				record.Spec.Raw != spec.Raw))
			{
				record.Spec = spec;
				record.Present = present;
				record.Generation = record.Generation == uint.MaxValue ? 1u :
					record.Generation + 1u;
				if (!MuiAreaCustomFontStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			value.Spec = record.Spec;
			value.Present = record.Present;
			value.Generation = record.Generation;
			return true;
		}

		if (!WriteState(ref platform, state, obj, spec, present, 1)) return false;
		value.Spec = spec;
		value.Present = present;
		value.Generation = 1;
		return true;
	}

	internal static bool WriteState<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR spec, uint present, uint generation)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaCustomFontStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaCustomFontStateRecord.Size);
		var record = default(MuiAreaCustomFontStateRecord);
		record.Magic = MuiAreaCustomFontStateRecord.Cookie;
		record.Spec = spec;
		record.Present = present == 0 ? 0u : 1u;
		record.Generation = generation == 0 ? 1u : generation;
		var written = MuiAreaCustomFontStateRecordCodec.Write(ref platform,
			scratch, record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, unchecked((int)MuiAreaCustomFontStateRecord.Size));
		platform.Clear(scratch, MuiAreaCustomFontStateRecord.Size);
		platform.Free(scratch, MuiAreaCustomFontStateRecord.Size);
		return stored;
	}

	internal static bool TryGetRuntime<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaCustomFontHandleState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			RuntimeStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			RuntimeStateKey) != unchecked((int)MuiAreaCustomFontRuntimeRecord.Size) ||
			!MuiAreaCustomFontRuntimeRecordCodec.TryRead(ref platform, block,
				out var record)) return false;
		value.Font = record.Font;
		value.Spec = record.Spec;
		value.Generation = record.Generation;
		value.Active = record.Active;
		return true;
	}

	internal static bool CloseRuntime<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			RuntimeStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			RuntimeStateKey) == 0) return true;
		if (!MuiAreaCustomFontRuntimeRecordCodec.TryRead(ref platform, block,
			out var record)) return false;
		if (record.Active != 0 && record.Font.IsNotNull &&
			!platform.CloseMuiCustomFont(record.Font)) return false;
		return MuiStoreCore.DataspaceRemove(ref platform, state, obj,
			RuntimeStateKey);
	}

	private static bool WriteRuntime<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaCustomFontHandleState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaCustomFontRuntimeRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaCustomFontRuntimeRecord.Size);
		var record = default(MuiAreaCustomFontRuntimeRecord);
		record.Magic = MuiAreaCustomFontRuntimeRecord.Cookie;
		record.Font = value.Font;
		record.Spec = value.Spec;
		record.Generation = value.Generation;
		record.Active = value.Active == 0 ? 0u : 1u;
		var written = MuiAreaCustomFontRuntimeRecordCodec.Write(ref platform,
			scratch, record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			RuntimeStateKey, scratch,
			unchecked((int)MuiAreaCustomFontRuntimeRecord.Size));
		platform.Clear(scratch, MuiAreaCustomFontRuntimeRecord.Size);
		platform.Free(scratch, MuiAreaCustomFontRuntimeRecord.Size);
		return stored;
	}

	private static bool OpenForSpec<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR spec, out MuiAreaCustomFontHandleState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (spec.IsNull || !MuiCustomFontSpecCore.TryParse(ref platform, spec,
			out var parsed)) return false;
		if (!MuiControlFontResolutionCore.TryResolveBaseFontForOpen(ref platform,
			state, obj, out var baseFont)) return false;
		var request = default(MuiCustomFontOpenRequest);
		request.Object = obj;
		request.BaseFont = baseFont;
		request.Spec = parsed;
		var font = platform.OpenMuiCustomFont(ref request);
		if (font.IsNull) return false;
		value.Font = font;
		value.Spec = spec;
		value.Generation = 1;
		value.Active = 1;
		if (WriteRuntime(ref platform, state, obj, value)) return true;
		platform.CloseMuiCustomFont(font);
		value = default;
		return false;
	}

	internal static bool Setup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiControlFontResolutionCore.TryResolveCustomSourceForOpen(
			ref platform, state, obj, out var source))
			return CloseRuntime(ref platform, state, obj);
		if (!MuiCustomFontSpecCore.TryParse(ref platform, source,
			out _)) return false;
		if (TryGetRuntime(ref platform, state, obj, out var current) &&
			current.Active != 0 && current.Spec.Raw == source.Raw)
			return true;
		if (!CloseRuntime(ref platform, state, obj)) return false;
		return OpenForSpec(ref platform, state, obj, source,
			out _);
	}

	internal static bool Refresh<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryGetRuntime(ref platform, state, obj, out var current) ||
			current.Active == 0)
		{
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				IsSetupAttribute, out var setup) || setup == 0) return true;
			return Setup(ref platform, state, obj);
		}
		return Setup(ref platform, state, obj);
	}

	internal static bool Open<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR spec, out MuiAreaCustomFontHandleState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!CloseRuntime(ref platform, state, obj))
		{
			value = default;
			return false;
		}
		return OpenForSpec(ref platform, state, obj, spec, out value);
	}

	internal static bool Close<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR font) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryGetRuntime(ref platform, state, obj, out var current))
			return false;
		if (current.Font.Raw != font.Raw) return false;
		return CloseRuntime(ref platform, state, obj);
	}
}

public static class MuiAreaCustomFontPacketCore
{
	public static bool TryGetEffective<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaCustomFontEffectiveState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (!MuiControlFontResolutionCore.TryResolve(ref platform, state, obj,
			out var resolution)) return false;
		value.Present = resolution.Custom;
		value.Inherited = resolution.Inherited ? 1u : 0u;
		value.Depth = resolution.Depth;
		value.BaseFont = resolution.Font;
		value.Spec = resolution.CustomSpec;
		return true;
	}

	public static bool TryParse<TPlatform>(ref TPlatform platform, APTR spec,
		out MuiCustomFontSpec value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCustomFontSpecCore.TryParse(ref platform, spec, out value);

	public static bool Set<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR spec, bool notify = true)
		where TPlatform : struct, IMuiLayoutPlatform =>
		MuiCommonControlCore.SetControlAttribute(ref platform, state, obj,
			MuiCommonControlCore.CustomFont, spec.Raw, notify);

	public static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaCustomFontStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaCustomFontCore.TryReadState(ref platform, state, obj, out value);

	public static bool TryGetRuntime<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaCustomFontHandleState value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaCustomFontCore.TryGetRuntime(ref platform, state, obj, out value);

	public static bool TryOpenCustomFont<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR spec,
		out MuiAreaCustomFontHandleState value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaCustomFontCore.Open(ref platform, state, obj, spec, out value);

	public static bool CloseCustomFont<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR font)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaCustomFontCore.Close(ref platform, state, obj, font);
}

// Public value-type projection of the effective CustomFont choice. BaseFont
// is the inherited TextFont fallback; Spec carries the parsed family/span and
// style/color overrides for a later graphics font capability.
public struct MuiAreaCustomFontEffectiveState
{
	public uint Present;
	public uint Inherited;
	public uint Depth;
	public APTR BaseFont;
	public MuiCustomFontSpec Spec;
}
