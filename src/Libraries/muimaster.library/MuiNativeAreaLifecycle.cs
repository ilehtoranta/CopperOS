/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Projects native BOOPSI lifecycle callbacks into the MUI-owned public
// sidecar. Render-info is borrowed from MUI and is valid only between Setup
// and Cleanup; it is never read from or written to a private Intuition offset.
internal static class MuiNativeAreaLifecycleCore
{
	internal static bool TryProjectAfterClassCallback<TPlatform>(
		ref TPlatform platform, APTR sidecarAddress, APTR message, uint method,
		ref uint result)
		where TPlatform : struct, IMuiAllocationPlatform
	{
		if (method == MuiLayoutPacketCore.Setup)
		{
			if (result == 0)
			{
				MuiNativeObjectStateCore.SetBorrowedAttributeNoNotify(
					ref platform, sidecarAddress,
					MuiAreaWindowRelationshipCore.RenderInfoAttribute, 0);
				return true;
			}
			if (!MuiLayoutPacketCodec.TryReadRenderInfo(ref platform, message,
				method, out var packet) || packet.MethodId != method ||
				packet.RenderInfo == 0 ||
				!MuiDrawingRenderInfoCodec.TryRead(ref platform,
					APTR.FromPointer(packet.RenderInfo), out var renderInfo) ||
				renderInfo.RastPort.IsNull ||
				!MuiNativeObjectStateCore.SetBorrowedAttributeNoNotify(
					ref platform, sidecarAddress,
					MuiAreaWindowRelationshipCore.RenderInfoAttribute,
					packet.RenderInfo))
			{
				// A class callback cannot report successful setup when its live
				// render context could not be represented safely in the sidecar.
				result = 0;
				MuiNativeObjectStateCore.SetBorrowedAttributeNoNotify(
					ref platform, sidecarAddress,
					MuiAreaWindowRelationshipCore.RenderInfoAttribute, 0);
			}
			return true;
		}

		if (method != MuiLayoutPacketCore.Cleanup ||
			!MuiLayoutPacketCodec.TryReadMethodId(ref platform, message,
				out var methodHeader) || methodHeader.MethodId != method)
			return false;

		// Cleanup returns no value in the MUI contract. Preserve the original
		// class result while invalidating the borrowed pointer after its callback.
		return MuiNativeObjectStateCore.SetBorrowedAttributeNoNotify(
			ref platform, sidecarAddress,
			MuiAreaWindowRelationshipCore.RenderInfoAttribute, 0);
	}
}
