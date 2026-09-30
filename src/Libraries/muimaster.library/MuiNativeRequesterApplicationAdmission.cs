/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Validated live MUI context for the future application-modal presenter. These
// are named sidecar identities, not views over guest object offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeRequesterApplicationAdmissionRecord
{
	internal APTR Application;
	internal APTR Window;
	internal APTR ApplicationSidecar;
	internal APTR WindowSidecar;
	internal uint HasReferenceWindow;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeRequesterAreaAdmissionRecord
{
	internal APTR Object;
	internal APTR Sidecar;
}

internal static class MuiNativeRequesterApplicationAdmissionCore
{
	internal static bool TryValidateAreaObject<TPlatform>(ref TPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj,
		out MuiNativeRequesterAreaAdmissionRecord admission)
		where TPlatform : struct, IMuiGuestMemory
	{
		admission = default;
		if (obj.IsNull || ownerRoot.IsNull || publicObjects.IsNull ||
			!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, obj, out var binding) || binding.Parent.IsNotNull ||
			!MuiNativeApplicationSleep.TryIsAreaClass(ref platform,
				binding.Class, out var isArea) || !isArea) return false;

		admission.Object = obj;
		admission.Sidecar = binding.Sidecar;
		return true;
	}

	internal static bool TryValidate<TPlatform>(ref TPlatform platform,
		MuiRequesterCallRecord request, APTR publicObjects, APTR ownerRoot,
		out MuiNativeRequesterApplicationAdmissionRecord admission)
		where TPlatform : struct, IMuiGuestMemory
	{
		admission = default;
		if (request.Application.IsNull || request.Flags != 0 || ownerRoot.IsNull ||
			publicObjects.IsNull ||
			!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, request.Application, out var application) ||
			!MuiNativeApplicationSleep.TryIsApplicationClass(ref platform,
				application.Class, out var isApplication) || !isApplication)
			return false;

		admission.Application = request.Application;
		admission.ApplicationSidecar = application.Sidecar;
		if (request.Window.IsNull)
		{
			admission.HasReferenceWindow = 0;
			return true;
		}

		if (!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
			ownerRoot, request.Window, out var window) ||
			!MuiNativeApplicationSleep.TryIsWindowClass(ref platform, window.Class,
				out var isWindow) || !isWindow || window.Parent != request.Application)
			return false;

		admission.Window = request.Window;
		admission.WindowSidecar = window.Sidecar;
		admission.HasReferenceWindow = 1;
		return true;
	}
}
