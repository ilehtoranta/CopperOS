/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Bounded MorphOS Area activation baseline. The packet flags and active
// transition are retained as one guest Dataspace record so disposal cannot
// strand separately allocated activation state.
public static class MuiAreaActivationCore
{
	// A single numeric Dataspace key owns the complete activation packet. The
	// key is internal implementation state, not a public MUI attribute.
	internal const uint StateKey = 0x7F07003Fu;

	public static bool IsActivationMethod(uint method) =>
		MuiAreaActivationMessageCodec.IsMethod(method);

	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiAreaActivationMessageCodec.TryRead(ref platform, message,
			out var packet)) return 0;
		if (packet.MethodId == MuiAreaActivationMessageCodec.GoActive)
			return GoActive(ref platform, state, obj, packet.Flags) ? 1u : 0u;
		GoInactive(ref platform, state, obj, packet.Flags);
		return 0;
	}

	internal static bool GoActive<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint flags) where TPlatform : struct, IMuiHeadlessPlatform
		=> WriteState(ref platform, state, obj, 1, flags);

	internal static bool GoInactive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform
		=> WriteState(ref platform, state, obj, 0, flags);

	internal static bool IsActive<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		TryGetState(ref platform, state, obj, out var value) &&
		value.Active != 0;

	internal static uint Flags<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		TryGetState(ref platform, state, obj, out var value) ? value.Flags : 0;

	internal static bool TryGetState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaActivationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var data = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StateKey);
		if (data.IsNull || MuiStoreCore.DataspaceLength(ref platform, state, obj,
			StateKey) != (int)MuiAreaActivationStateRecord.Size)
			return false;
		return MuiAreaActivationStateCodec.TryRead(ref platform, data, out value);
	}

	private static bool WriteState<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint active, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var previous = default(MuiAreaActivationStateRecord);
		if (!TryGetState(ref platform, state, obj, out previous))
			previous = default;
		var next = default(MuiAreaActivationStateRecord);
		next.Signature = MuiAreaActivationStateRecord.Cookie;
		next.Active = active == 0 ? 0u : 1u;
		next.Flags = flags;
		next.Generation = previous.Generation == uint.MaxValue ? 1u :
			previous.Generation + 1u;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaActivationStateRecord.Size);
		if (scratch.IsNull) return false;
		var written = MuiAreaActivationStateCodec.Write(ref platform, scratch, next);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, (int)MuiAreaActivationStateRecord.Size);
		platform.Clear(scratch, MuiAreaActivationStateRecord.Size);
		platform.Free(scratch, MuiAreaActivationStateRecord.Size);
		return stored;
	}
}

// Public value-type projection for callers that need the Area activation state
// without knowing the internal Dataspace key or guest record layout.
public struct MuiAreaActivationStateInput
{
	public uint Active;
	public uint Flags;
}

public static class MuiAreaActivationPacketCore
{
	public static bool GoActive<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaActivationCore.GoActive(ref platform, state, obj, flags);

	public static bool GoInactive<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaActivationCore.GoInactive(ref platform, state, obj, flags);

	public static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaActivationStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (!MuiAreaActivationCore.TryGetState(ref platform, state, obj,
			out var record)) return false;
		value.Active = record.Active;
		value.Flags = record.Flags;
		return true;
	}
}
