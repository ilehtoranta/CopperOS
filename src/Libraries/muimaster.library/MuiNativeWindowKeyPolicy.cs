/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

internal static class MuiNativeWindowKeyPolicy
{
	internal static bool IsDisabled<TMemory>(ref TMemory memory,
		APTR sidecarAddress, APTR handleEventPacket)
		where TMemory : struct, IMuiGuestMemory
	{
		if (!MuiCommonControlPacketCore.TryReadHandleEvent(ref memory,
			handleEventPacket, out var handleEvent) || handleEvent.MuiKey < 0 ||
			handleEvent.MuiKey >= 32 ||
			!MuiNativeObjectStateCore.TryGetAttribute(ref memory, sidecarAddress,
				MuiWindowPublicCore.DisableKeys, out var disabledKeys)) return false;
		return (disabledKeys & (1u << handleEvent.MuiKey)) != 0;
	}
}
