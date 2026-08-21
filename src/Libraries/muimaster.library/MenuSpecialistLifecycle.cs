/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Class-owned teardown for the MG09 menu specialist family. The menu classes
// are Family.mui subclasses whose children form an owned tree, so their
// OM_DISPOSE-equivalent must release both the menu-specific class-owned state
// (the copied Title/Shortcut blocks and the per-object sidecar) and the object
// tree itself. The core performs a post-order free of every node's owned
// strings and sidecar first, then hands the root to the frozen object core,
// which recursively disposes the child objects and their records. A caller
// pointer that was referenced (CopyStrings FALSE) is never freed; a repeated
// disposal finds no sidecar and is a safe no-op.
public static class MuiMenuSpecialistLifecycle
{
	public static bool Dispose<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiMenuSpecialistCore.Dispose(ref platform, state, obj);
}
