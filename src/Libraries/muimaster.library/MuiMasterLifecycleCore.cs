/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

public static class MuiMasterLifecycleCore
{
	public static bool Create<TPlatform>(ref TPlatform platform, APTR privateRoot,
		APTR headlessState) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (privateRoot.IsNull ||
			!platform.IsMapped(privateRoot, MuiMasterPrivateRoot.Size) ||
			!MuiHeadlessObjectCore.Initialize(ref platform, headlessState))
			return false;
		var root = default(MuiMasterPrivateRoot);
		root.ClassRegistry = headlessState.Raw;
		return MuiMasterPrivateRootCodec.Write(ref platform, privateRoot, root);
	}

	public static bool Dispose<TPlatform>(ref TPlatform platform,
		APTR privateRoot) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (privateRoot.IsNull ||
			!platform.IsMapped(privateRoot, MuiMasterPrivateRoot.Size)) return false;
		if (!MuiMasterPrivateRootCodec.TryRead(ref platform, privateRoot,
			out var root)) return false;
		var state = APTR.FromPointer(root.ClassRegistry);
		if (state.IsNull || !MuiHeadlessStateCodec.TryRead(ref platform, state,
			out _))
			return false;

		uint visited = 0;
		while (true)
		{
			if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
				out var stateValue)) return false;
			if (stateValue.Objects.IsNull) break;
			if (visited++ >= MuiHeadlessLayout.MaximumTraversal) return false;
			var current = stateValue.Objects;
			if (!MuiHeadlessObjectCodec.TryRead(ref platform, current,
				out var objectValue)) return false;
			var boopsi = objectValue.Boopsi;
			if (!MuiHeadlessObjectCore.DisposeObject(ref platform, state, boopsi))
				return false;
		}

		visited = 0;
		while (true)
		{
			if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
				out var stateValue)) return false;
			if (stateValue.Classes.IsNull) break;
			if (visited++ >= MuiHeadlessLayout.MaximumTraversal) return false;
			var current = stateValue.Classes;
			if (!MuiHeadlessObjectCore.DeleteClass(ref platform, state, current))
				return false;
		}

		platform.Clear(state, MuiHeadlessStateRecord.Size);
		platform.Clear(privateRoot, MuiMasterPrivateRoot.Size);
		return true;
	}
}
