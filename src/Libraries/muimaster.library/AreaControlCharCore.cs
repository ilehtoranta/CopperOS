/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Struct-first MUIA_ControlChar Area projection. This stores and publishes
// the shared character policy; Text.mui keeps its documented
// MUIA_Text_ControlChar presentation policy in its own named record.
internal static class MuiAreaControlCharCore
{
	internal const uint StateKey = 0x7F070062u;

	internal static uint Normalize(uint value) => value & 0xFFu;

	internal static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaControlCharStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var character = 0u;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			MuiCommonControlCore.ControlChar, out var raw))
			character = Normalize(raw);
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj, StateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj, StateKey) ==
			unchecked((int)MuiAreaControlCharStateRecord.Size) &&
			MuiAreaControlCharStateRecordCodec.TryRead(ref platform, block,
				out var record))
		{
			if (record.Character != character)
			{
				record.Character = character;
				record.Generation = record.Generation == uint.MaxValue ? 1u :
					record.Generation + 1u;
				if (!MuiAreaControlCharStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			value.Character = record.Character;
			return true;
		}
		if (!WriteState(ref platform, state, obj, character, 1)) return false;
		value.Character = character;
		return true;
	}

	internal static bool WriteState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint character, uint generation)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaControlCharStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaControlCharStateRecord.Size);
		var record = default(MuiAreaControlCharStateRecord);
		record.Magic = MuiAreaControlCharStateRecord.Cookie;
		record.Character = Normalize(character);
		record.Generation = generation == 0 ? 1u : generation;
		var written = MuiAreaControlCharStateRecordCodec.Write(ref platform, scratch,
			record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, unchecked((int)MuiAreaControlCharStateRecord.Size));
		platform.Clear(scratch, MuiAreaControlCharStateRecord.Size);
		platform.Free(scratch, MuiAreaControlCharStateRecord.Size);
		return stored;
	}
}

public static class MuiAreaControlCharPacketCore
{
	public static bool Set<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint character) where TPlatform : struct, IMuiLayoutPlatform =>
		MuiCommonControlCore.SetControlAttribute(ref platform, state, obj,
			MuiCommonControlCore.ControlChar,
			MuiAreaControlCharCore.Normalize(character), true);

	public static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaControlCharStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaControlCharCore.TryReadState(ref platform, state, obj, out value);
}
