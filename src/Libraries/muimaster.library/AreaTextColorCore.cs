/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS MUIA_TextColor is a getter-only, setup-scoped Area projection. The
// platform resolves its RGB value from the live render context; the core owns
// only the bounded guest record and the MUIM_Setup/MUIM_Cleanup lifetime.
internal static class MuiAreaTextColorCore
{
	internal const uint StateKey = 0x7F070045u;

	internal static bool Initialize<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		WriteState(ref platform, state, obj, 0, 0, 1);

	internal static bool Setup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR renderInfo) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (obj.IsNull || renderInfo.IsNull) return false;
		// Validate the existing setup-scoped record before asking the provider to
		// resolve the requested color. A malformed present block must fail closed
		// first.
		if (!TryReadState(ref platform, state, obj, out _)) return false;
		var request = default(MuiTextColorResolutionRequest);
		request.Object = obj;
		request.RenderInfo = renderInfo;
		if (MuiControlFontResolutionCore.TryResolve(ref platform, state, obj,
			out var fontResolution) && fontResolution.Custom != 0)
		{
			request.CustomFontAvailable = 1;
			request.CustomFontSpec = fontResolution.CustomSpec;
		}
		if (!platform.ResolveMuiTextColor(ref request)) return false;
		var color = request.Color & 0x00FFFFFFu;
		var active = request.Available == 0 ? 0u : 1u;
		var generation = 1u;
		if (TryReadRecord(ref platform, state, obj, out var current))
			generation = current.Generation == uint.MaxValue ? 1u :
				current.Generation + 1u;
		return WriteState(ref platform, state, obj, color, active, generation);
	}

	internal static bool Cleanup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadState(ref platform, state, obj, out _)) return false;
		var generation = 1u;
		if (TryReadRecord(ref platform, state, obj, out var current))
			generation = current.Generation == uint.MaxValue ? 1u :
				current.Generation + 1u;
		return WriteState(ref platform, state, obj, 0, 0, generation);
	}

	// Apply the current setup-scoped RGB value to one native render target.
	// The provider owns the actual palette/true-colour operation; MUI core never
	// treats the RGB value as a pen index and never keeps a host colour object.
	internal static void Apply<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR rastPort) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (rastPort.IsNull || !TryReadState(ref platform, state, obj,
			out var color) || color.Active == 0) return;
		var request = default(MuiTextColorRenderRequest);
		request.RastPort = rastPort;
		request.Color = color.Color & 0x00FFFFFFu;
		platform.ApplyMuiTextColor(ref request);
	}

	internal static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaTextColorState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj, StateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj, StateKey);
		MuiAreaTextColorStateRecord record;
		if (block.IsNotNull || length != 0)
		{
			// A present block is authoritative typed state. Do not rebuild a
			// malformed RGB/Active/generation record from defaults.
			if (length != unchecked((int)MuiAreaTextColorStateRecord.Size) ||
				!MuiAreaTextColorStateRecordCodec.TryReadStructural(ref platform, block,
					out record) || !MuiAreaTextColorStateAdmission.ValidateLive(ref platform,
					state, obj, record)) return false;
		}
		else
		{
			if (!Initialize(ref platform, state, obj) ||
				!TryReadRecord(ref platform, state, obj, out record)) return false;
		}
		value.Color = record.Color & 0x00FFFFFFu;
		value.Active = record.Active == 0 ? 0u : 1u;
		value.Generation = record.Generation;
		return true;
	}

	internal static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (!TryReadState(ref platform, state, obj, out var stateValue))
			return false;
		value = stateValue.Active == 0 ? 0u : stateValue.Color;
		return true;
	}

	private static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaTextColorStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj, StateKey) !=
			unchecked((int)MuiAreaTextColorStateRecord.Size)) return false;
		return MuiAreaTextColorStateRecordCodec.TryReadStructural(ref platform,
			block, out value) && MuiAreaTextColorStateAdmission.ValidateLive(
			ref platform, state, obj, value);
	}

	private static bool WriteState<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint color, uint active, uint generation)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var existing = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StateKey);
		var existingLength = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			StateKey);
		if (existing.IsNotNull || existingLength != 0)
		{
			if (existingLength != unchecked((int)MuiAreaTextColorStateRecord.Size) ||
				!MuiAreaTextColorStateRecordCodec.TryReadStructural(ref platform,
					existing, out var existingRecord) ||
				!MuiAreaTextColorStateAdmission.ValidateLive(ref platform, state, obj,
					existingRecord)) return false;
		}
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaTextColorStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaTextColorStateRecord.Size);
		var record = default(MuiAreaTextColorStateRecord);
		record.Magic = MuiAreaTextColorStateRecord.Cookie;
		record.Color = color & 0x00FFFFFFu;
		record.Active = active == 0 ? 0u : 1u;
		record.Generation = generation == 0 ? 1u : generation;
		var written = MuiAreaTextColorStateAdmission.ValidateLive(ref platform, state,
			obj, record) && MuiAreaTextColorStateRecordCodec.Write(ref platform,
			scratch, record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, unchecked((int)MuiAreaTextColorStateRecord.Size));
		if (!stored)
		{
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StateKey);
			stored = written && MuiAreaTextColorStateRecordCodec.Write(ref platform,
				block, record);
		}
		platform.Clear(scratch, MuiAreaTextColorStateRecord.Size);
		platform.Free(scratch, MuiAreaTextColorStateRecord.Size);
		return stored;
	}
}
