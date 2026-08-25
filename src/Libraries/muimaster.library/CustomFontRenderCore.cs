/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Projects the effective CustomFont selection into one provider-owned render
// request.  The core resolves only named guest/value records; glyph styling,
// outlines, glow, and underline rasterization remain native-provider policy.
internal static class MuiCustomFontRenderCore
{
	internal static bool Apply<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR rastPort) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (obj.IsNull || rastPort.IsNull ||
			!MuiControlFontResolutionCore.TryResolve(ref platform, state, obj,
				out var resolution) || resolution.Custom == 0) return false;
		var request = default(MuiCustomFontRenderRequest);
		request.Object = obj;
		request.RastPort = rastPort;
		request.Font = resolution.Font;
		request.Spec = resolution.CustomSpec;
		return platform.ApplyMuiCustomFont(ref request);
	}
}
