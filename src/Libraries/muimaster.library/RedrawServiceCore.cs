/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Native-safe MUI_Redraw gateway. The public function is valid for an object
// known by the guest object registry and accepts only the two documented draw
// intent bits. Actual rendering remains behind ScheduleRedraw.
public static class MuiRedrawServiceCore
{
	public const uint DrawObject = 0x00000001;
	public const uint DrawUpdate = 0x00000002;
	private const uint AllowedFlags = DrawObject | DrawUpdate;

	public static bool Redraw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint flags) where TPlatform : struct, IMuiServicePlatform,
		IMuiGraphicsCapability
	{
		if (obj.IsNull || flags == 0 || (flags & ~AllowedFlags) != 0)
			return false;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		return platform.ScheduleRedraw(obj, flags);
	}
}
