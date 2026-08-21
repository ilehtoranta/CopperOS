/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

public static class MuiSemaphoreCore
{
	public static bool Attempt<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return false;
		var task = platform.CurrentTaskToken();
		if (task == 0) return false;
		var owner = objectValue.SemaphoreOwner.Raw;
		var shared = objectValue.SemaphoreShared;
		if (owner == task)
		{
			var depth = objectValue.SemaphoreDepth;
			if (depth == uint.MaxValue) return false;
			objectValue.SemaphoreDepth = depth + 1;
			if (!MuiHeadlessObjectCodec.Write(ref platform, record, objectValue))
				return false;
			return true;
		}
		if (owner != 0 || shared != 0) return false;
		objectValue.SemaphoreOwner = APTR.FromPointer(task);
		objectValue.SemaphoreDepth = 1;
		return MuiHeadlessObjectCodec.Write(ref platform, record, objectValue);
	}

	public static bool AttemptShared<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return false;
		var owner = objectValue.SemaphoreOwner.Raw;
		var task = platform.CurrentTaskToken();
		if (owner != 0 && owner != task) return false;
		var shared = objectValue.SemaphoreShared;
		if (shared == uint.MaxValue) return false;
		objectValue.SemaphoreShared = shared + 1;
		return MuiHeadlessObjectCodec.Write(ref platform, record, objectValue);
	}

	public static bool Obtain<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		Attempt(ref platform, state, obj);

	public static bool ObtainShared<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		AttemptShared(ref platform, state, obj);

	public static bool Release<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return false;
		var task = platform.CurrentTaskToken();
		var owner = objectValue.SemaphoreOwner.Raw;
		if (owner == task && owner != 0)
		{
			var depth = objectValue.SemaphoreDepth;
			if (depth == 0) return false;
			depth--;
			objectValue.SemaphoreDepth = depth;
			if (depth == 0) objectValue.SemaphoreOwner = APTR.Null;
			return MuiHeadlessObjectCodec.Write(ref platform, record,
				objectValue);
		}
		var shared = objectValue.SemaphoreShared;
		if (shared == 0) return false;
		objectValue.SemaphoreShared = shared - 1;
		return MuiHeadlessObjectCodec.Write(ref platform, record, objectValue);
	}
}

// Scalar qualification surface for the semaphore fields embedded in the
// headless object record. The live semaphore operations above use the same
// codec; this seam proves owner/depth/shared state without a managed
// synchronization object.
public static class MuiSemaphorePacketCore
{
	public static bool WriteState<TPlatform>(ref TPlatform platform,
		APTR address, APTR owner, uint depth, uint shared)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiHeadlessObjectRecord record = default;
		record.SemaphoreOwner = owner;
		record.SemaphoreDepth = depth;
		record.SemaphoreShared = shared;
		return MuiHeadlessObjectCodec.Write(ref platform, address, record);
	}

	public static uint DispatchState<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.SemaphoreOwner.Raw ^ record.SemaphoreDepth ^
			record.SemaphoreShared;
	}
}
