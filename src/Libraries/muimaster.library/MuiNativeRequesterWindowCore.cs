/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Both sides of this translation are explicit: the public MUI argument is a
// Window object, while Intuition.EasyRequestArgs requires its native Window.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeRequesterWindowResolutionRecord
{
	internal APTR MuiWindow;
	internal APTR IntuitionWindow;
	internal uint Resolved;
}

internal static class MuiNativeRequesterWindowCore
{
	internal static bool TryResolve<TPlatform>(ref TPlatform platform,
		APTR muiWindow, out MuiNativeRequesterWindowResolutionRecord result)
		where TPlatform : struct, IMuiRequesterWindowCapability
	{
		result = default;
		result.MuiWindow = muiWindow;
		if (muiWindow.IsNull)
		{
			result.Resolved = 1;
			return true;
		}
		if (!platform.TryGetNativeWindow(muiWindow, out var intuitionWindow))
		{
			result = default;
			return false;
		}
		result.IntuitionWindow = intuitionWindow;
		result.Resolved = 1;
		return true;
	}
}
