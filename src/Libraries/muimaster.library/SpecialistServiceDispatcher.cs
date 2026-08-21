/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Family-neutral packet boundaries for standalone MG09 specialist instances.
// The route is deliberately additive: it does not call the frozen headless
// dispatcher and it does not make the specialist families depend on one
// another. Each family validates its own guest-resident magic/layout before
// claiming the packet.
public static class MuiSpecialistServiceDispatcher
{
	public static uint DispatchStandalone<TPlatform>(ref TPlatform platform,
		APTR instance, APTR message) where TPlatform : struct, IMuiServicePlatform
	{
		if (MuiPopSpecialistCore.Valid(ref platform, instance))
			return MuiPopSpecialistDispatcher.Dispatch(ref platform, instance, message);
		if (MuiColorSpecialistCore.Valid(ref platform, instance))
			return MuiColorSpecialistDispatcher.Dispatch(ref platform, instance,
				message);
		if (MuiMiscSpecialistCore.Valid(ref platform, instance))
			return MuiMiscSpecialistDispatcher.Dispatch(ref platform, instance,
				message);
		return 0;
	}

	// Stable service-vector spelling used by the public MG09 qualification
	// surface. Keep DispatchStandalone as the implementation name so native
	// roots can use the shorter form without introducing another route.
	public static uint DispatchStandaloneService<TPlatform>(
		ref TPlatform platform, APTR instance, APTR message)
		where TPlatform : struct, IMuiServicePlatform =>
		DispatchStandalone(ref platform, instance, message);

	public static uint DispatchExternal<TPlatform>(ref TPlatform platform,
		APTR instance, APTR message) where TPlatform : struct, IMuiServicePlatform =>
		MuiExternalWrapperCore.Valid(ref platform, instance)
			? MuiExternalWrapperDispatcher.Dispatch(ref platform, instance, message)
			: 0u;

	public static uint DispatchExternalService<TPlatform>(ref TPlatform platform,
		APTR instance, APTR message)
		where TPlatform : struct, IMuiServicePlatform =>
		DispatchExternal(ref platform, instance, message);
}
