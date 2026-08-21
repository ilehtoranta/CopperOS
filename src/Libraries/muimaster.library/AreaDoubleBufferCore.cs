/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS MUIArea double-buffer policy. This slice publishes and mutates the
// documented BOOL state; allocating and blitting a native off-screen bitmap is
// intentionally a later rendering capability and is not hidden behind a
// managed object or exception path.
internal static class MuiAreaDoubleBufferCore
{
	internal const uint StateKey = 0x7F070040u;

	internal static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaDoubleBufferStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var enabled = 0u;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			MuiCommonControlCore.DoubleBuffer, out var raw))
			enabled = raw == 0 ? 0u : 1u;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj, StateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj, StateKey) ==
			unchecked((int)MuiAreaDoubleBufferStateRecord.Size) &&
			MuiAreaDoubleBufferStateRecordCodec.TryRead(ref platform, block,
				out var record))
		{
			if (record.Enabled != enabled)
			{
				record.Enabled = enabled;
				record.Generation = record.Generation == uint.MaxValue ? 1u :
					record.Generation + 1u;
				if (!MuiAreaDoubleBufferStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			value.Enabled = record.Enabled;
			return true;
		}
		if (!WriteState(ref platform, state, obj, enabled, 1)) return false;
		value.Enabled = enabled;
		return true;
	}

	internal static bool WriteState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint enabled, uint generation)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaDoubleBufferStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaDoubleBufferStateRecord.Size);
		var record = default(MuiAreaDoubleBufferStateRecord);
		record.Magic = MuiAreaDoubleBufferStateRecord.Cookie;
		record.Enabled = enabled == 0 ? 0u : 1u;
		record.Generation = generation == 0 ? 1u : generation;
		var written = MuiAreaDoubleBufferStateRecordCodec.Write(ref platform,
			scratch, record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, unchecked((int)MuiAreaDoubleBufferStateRecord.Size));
		platform.Clear(scratch, MuiAreaDoubleBufferStateRecord.Size);
		platform.Free(scratch, MuiAreaDoubleBufferStateRecord.Size);
		return stored;
	}
}

// Public typed seam for the Area double-buffer policy. The input/output is a
// value type; callers do not receive a pointer into the private Dataspace.
public static class MuiAreaDoubleBufferPacketCore
{
	public static bool Set<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint enabled)
		where TPlatform : struct, IMuiLayoutPlatform =>
		MuiCommonControlCore.SetControlAttribute(ref platform, state, obj,
			MuiCommonControlCore.DoubleBuffer, enabled, true);

	public static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaDoubleBufferStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaDoubleBufferCore.TryReadState(ref platform, state, obj, out value);
}
