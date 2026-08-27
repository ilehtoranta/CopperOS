/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Struct-first MorphOS MUIA_DoubleClick projection. The public attribute is a
// getter-only LONG; the event/input layer may publish a value through the
// explicit packet seam, while ordinary Set/OM_SET callers remain rejected.
internal static class MuiAreaDoubleClickCore
{
	internal const uint StateKey = 0x7F070064u;

	internal static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaDoubleClickStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var hasRaw = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			MuiCommonControlCore.DoubleClick, out var raw);
		var current = hasRaw ? unchecked((int)raw) : 0;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj, StateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			StateKey);
		MuiAreaDoubleClickStateRecord record;
		if (block.IsNotNull || length != 0)
		{
			// A present block is authoritative typed state. Reject malformed
			// generation state instead of rebuilding the signal from raw storage.
			if (length != unchecked((int)MuiAreaDoubleClickStateRecord.Size) ||
				!MuiAreaDoubleClickStateRecordCodec.TryReadStructural(ref platform,
					block, out record) ||
				!MuiAreaDoubleClickStateAdmission.ValidateLive(ref platform, state,
					obj, record)) return false;
			if (hasRaw && record.Value != current)
			{
				record.Value = current;
				record.Generation = record.Generation == uint.MaxValue ? 1u :
					record.Generation + 1u;
				if (!MuiAreaDoubleClickStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			value.Value = hasRaw ? current : record.Value;
			return true;
		}
		if (!WriteState(ref platform, state, obj, current, 1)) return false;
		value.Value = current;
		return true;
	}

	internal static bool WriteState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int value, uint generation)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaDoubleClickStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaDoubleClickStateRecord.Size);
		var record = default(MuiAreaDoubleClickStateRecord);
		record.Magic = MuiAreaDoubleClickStateRecord.Cookie;
		record.Value = value;
		record.Generation = generation == 0 ? 1u : generation;
		var written = MuiAreaDoubleClickStateAdmission.ValidateLive(ref platform,
			state, obj, record) && MuiAreaDoubleClickStateRecordCodec.Write(
			ref platform, scratch, record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, unchecked((int)MuiAreaDoubleClickStateRecord.Size));
		platform.Clear(scratch, MuiAreaDoubleClickStateRecord.Size);
		platform.Free(scratch, MuiAreaDoubleClickStateRecord.Size);
		return stored;
	}

	internal static bool Publish<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull ||
			MuiCommonControlCore.Classify(ref platform, state, obj) ==
			MuiControlClass.Unknown)
			return false;
		if (!TryReadState(ref platform, state, obj, out _)) return false;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiCommonControlCore.DoubleClick, unchecked((uint)value), notify))
			return false;
		return TryReadState(ref platform, state, obj, out _);
	}
}

// Public typed Area getter/publication seam. Publication is intentionally
// explicit so future input routing can carry the event without making the
// getter-only MUIA_DoubleClick attribute runtime-settable.
public static class MuiAreaDoubleClickPacketCore
{
	public static bool Publish<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int value, bool notify = true)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaDoubleClickCore.Publish(ref platform, state, obj, value, notify);

	public static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaDoubleClickStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaDoubleClickCore.TryReadState(ref platform, state, obj, out value);
}
