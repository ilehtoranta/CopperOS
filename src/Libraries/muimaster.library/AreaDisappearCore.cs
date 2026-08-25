/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS Area disappearance priorities.  This slice publishes the signed
// [ISG] state and keeps the actual parent-size selection algorithm separate so
// no undocumented layout heuristic is substituted for MorphOS behavior.
internal static class MuiAreaDisappearCore
{
	internal const uint StateKey = 0x7F070042u;

	internal static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaDisappearPolicyStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var horizontal = ReadRaw(ref platform, state, obj,
			MuiCommonControlCore.HorizDisappear, 0);
		var vertical = ReadRaw(ref platform, state, obj,
			MuiCommonControlCore.VertDisappear, 0);
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj, StateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj, StateKey) ==
			unchecked((int)MuiAreaDisappearPolicyStateRecord.Size) &&
			MuiAreaDisappearPolicyStateRecordCodec.TryRead(ref platform, block,
				out var record))
		{
			if (record.HorizDisappear != unchecked((int)horizontal) ||
				record.VertDisappear != unchecked((int)vertical))
			{
				record.HorizDisappear = unchecked((int)horizontal);
				record.VertDisappear = unchecked((int)vertical);
				if (!MuiAreaDisappearPolicyStateRecordCodec.Write(ref platform,
					block, record)) return false;
			}
			value.HorizDisappear = record.HorizDisappear;
			value.VertDisappear = record.VertDisappear;
			return true;
		}
		if (!WriteState(ref platform, state, obj, unchecked((int)horizontal),
			unchecked((int)vertical))) return false;
		value.HorizDisappear = unchecked((int)horizontal);
		value.VertDisappear = unchecked((int)vertical);
		return true;
	}

	internal static bool TryReadStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaDisappearPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj, StateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj, StateKey) !=
			unchecked((int)MuiAreaDisappearPolicyStateRecord.Size)) return false;
		return MuiAreaDisappearPolicyStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	internal static bool WriteState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int horizontal, int vertical)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaDisappearPolicyStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaDisappearPolicyStateRecord.Size);
		var record = default(MuiAreaDisappearPolicyStateRecord);
		record.Magic = MuiAreaDisappearPolicyStateRecord.Cookie;
		record.HorizDisappear = horizontal;
		record.VertDisappear = vertical;
		var written = MuiAreaDisappearPolicyStateRecordCodec.Write(ref platform,
			scratch, record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch,
			unchecked((int)MuiAreaDisappearPolicyStateRecord.Size));
		platform.Clear(scratch, MuiAreaDisappearPolicyStateRecord.Size);
		platform.Free(scratch, MuiAreaDisappearPolicyStateRecord.Size);
		return stored;
	}

	private static uint ReadRaw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint defaultValue)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			attribute, out var value) ? value : defaultValue;
}

// Public typed packet seam for signed disappearance priorities.
public static class MuiAreaDisappearPacketCore
{
	public static bool Set<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, MuiAreaDisappearPolicyStateInput value)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!MuiCommonControlCore.SetControlAttribute(ref platform, state, obj,
			MuiCommonControlCore.HorizDisappear,
			unchecked((uint)value.HorizDisappear), true)) return false;
		return MuiCommonControlCore.SetControlAttribute(ref platform, state, obj,
			MuiCommonControlCore.VertDisappear,
			unchecked((uint)value.VertDisappear), true);
	}

	public static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaDisappearPolicyStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaDisappearCore.TryReadState(ref platform, state, obj, out value);
}

