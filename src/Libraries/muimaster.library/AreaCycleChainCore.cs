/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Struct-first MUIA_CycleChain Area projection. Window_SetCycleChain remains
// the independent vector-based window focus operation.
internal static class MuiAreaCycleChainCore
{
	internal const uint StateKey = 0x7F070063u;

	internal static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaCycleChainStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var cycleChain = 0;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			MuiCommonControlCore.CycleChain, out var raw))
			cycleChain = unchecked((int)raw);
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj, StateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj, StateKey) ==
			unchecked((int)MuiAreaCycleChainStateRecord.Size) &&
			MuiAreaCycleChainStateRecordCodec.TryRead(ref platform, block,
				out var record))
		{
			if (record.Value != cycleChain)
			{
				record.Value = cycleChain;
				record.Generation = record.Generation == uint.MaxValue ? 1u :
					record.Generation + 1u;
				if (!MuiAreaCycleChainStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			value.Value = record.Value;
			return true;
		}
		if (!WriteState(ref platform, state, obj, cycleChain, 1)) return false;
		value.Value = cycleChain;
		return true;
	}

	internal static bool WriteState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int cycleChain, uint generation)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaCycleChainStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaCycleChainStateRecord.Size);
		var record = default(MuiAreaCycleChainStateRecord);
		record.Magic = MuiAreaCycleChainStateRecord.Cookie;
		record.Value = cycleChain;
		record.Generation = generation == 0 ? 1u : generation;
		var written = MuiAreaCycleChainStateRecordCodec.Write(ref platform, scratch,
			record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, unchecked((int)MuiAreaCycleChainStateRecord.Size));
		platform.Clear(scratch, MuiAreaCycleChainStateRecord.Size);
		platform.Free(scratch, MuiAreaCycleChainStateRecord.Size);
		return stored;
	}
}

public static class MuiAreaCycleChainPacketCore
{
	public static bool Set<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int value) where TPlatform : struct, IMuiLayoutPlatform =>
		MuiCommonControlCore.SetControlAttribute(ref platform, state, obj,
			MuiCommonControlCore.CycleChain, unchecked((uint)value), true);

	public static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaCycleChainStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaCycleChainCore.TryReadState(ref platform, state, obj, out value);
}
