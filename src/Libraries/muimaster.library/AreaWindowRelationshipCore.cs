/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_Window and MUIA_WindowObject are getter-only Area relationships. Their
// guest values live in the named render-info record while the object is setup;
// before setup, MorphOS exposes NULL rather than a stale relationship.
public struct MuiAreaWindowRelationshipStateInput
{
	public APTR WindowObject;
	public APTR Window;
}

internal static class MuiAreaWindowRelationshipCore
{
	internal const uint RenderInfoAttribute = 0x7FFF0001u;

	internal static bool TryReadRenderInfo<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaWindowRelationshipStateInput value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiDrawingRenderInfoCodec.TryRead(ref platform, address,
			out var renderInfo)) return false;
		value.WindowObject = renderInfo.WindowObject;
		value.Window = renderInfo.Window;
		return true;
	}

	internal static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaWindowRelationshipStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			RenderInfoAttribute, out var rawRenderInfo) || rawRenderInfo == 0)
			return true;
		return TryReadRenderInfo(ref platform, APTR.FromPointer(rawRenderInfo),
			out value);
	}
}

public static class MuiAreaWindowRelationshipPacketCore
{
	public static bool TryReadRenderInfo<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaWindowRelationshipStateInput value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaWindowRelationshipCore.TryReadRenderInfo(ref platform, address,
			out value);

	public static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaWindowRelationshipStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaWindowRelationshipCore.TryReadState(ref platform, state, obj,
			out value);
}
