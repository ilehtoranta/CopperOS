/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Scoped consumers, not public vectors. The caller retains the same service
// token across success or failure, then releases the gate and provider pin.
// No operation silently drops cleanup authority or chooses a caller-supplied
// service address instead of the validated library-owned one.
internal static class MuiOwnedClassServiceCore
{
	internal static APTR GetClass<T>(ref T platform, ref MuiNativeServiceLease service,
		APTR currentTask, APTR classId) where T : struct, IMuiClassServicePlatform
	{
		if (!MuiNativeServiceAccessCore.IsHeld(ref platform, service, currentTask)) return APTR.Null;
		return MuiClassServiceCore.GetClass(ref platform, service.Operation.Service, classId);
	}

	internal static bool FreeClass<T>(ref T platform, ref MuiNativeServiceLease service,
		APTR currentTask, APTR cls) where T : struct, IMuiClassServicePlatform
	{
		if (!MuiNativeServiceAccessCore.IsHeld(ref platform, service, currentTask)) return false;
		return MuiClassServiceCore.FreeClass(ref platform, service.Operation.Service, cls);
	}

	internal static APTR CreateCustomClass<T>(ref T platform, ref MuiNativeServiceLease service,
		APTR currentTask, APTR dispatcherLibrary, APTR superName, APTR superMcc, int dataSize, APTR dispatcher)
		where T : struct, IMuiClassServicePlatform
	{
		if (!MuiNativeServiceAccessCore.IsHeld(ref platform, service, currentTask)) return APTR.Null;
		return MuiClassServiceCore.CreateCustomClass(ref platform, service.Operation.Service,
			dispatcherLibrary, superName, superMcc, dataSize, dispatcher, service.Operation.Providers);
	}

	internal static bool DeleteCustomClass<T>(ref T platform, ref MuiNativeServiceLease service,
		APTR currentTask, APTR mcc) where T : struct, IMuiClassServicePlatform
	{
		if (!MuiNativeServiceAccessCore.IsHeld(ref platform, service, currentTask)) return false;
		return MuiClassServiceCore.DeleteCustomClass(ref platform, service.Operation.Service, mcc);
	}
}

internal static class MuiNativeOwnedClassServices
{
	internal static APTR GetClass(ref MuiNativeServiceLease service, APTR classId)
	{
		if (!MuiNativeProviderOwner.TryPrepareServicePlatform(ref service, out var classes, out var currentTask)) return APTR.Null;
		return MuiOwnedClassServiceCore.GetClass(ref classes, ref service, currentTask, classId);
	}

	internal static bool FreeClass(ref MuiNativeServiceLease service, APTR cls)
	{
		if (!MuiNativeProviderOwner.TryPrepareServicePlatform(ref service, out var classes, out var currentTask)) return false;
		return MuiOwnedClassServiceCore.FreeClass(ref classes, ref service, currentTask, cls);
	}

	internal static APTR CreateCustomClass(ref MuiNativeServiceLease service, APTR dispatcherLibrary,
		APTR superName, APTR superMcc, int dataSize, APTR dispatcher)
	{
		if (!MuiNativeProviderOwner.TryPrepareServicePlatform(ref service, out var classes, out var currentTask)) return APTR.Null;
		return MuiOwnedClassServiceCore.CreateCustomClass(ref classes, ref service, currentTask,
			dispatcherLibrary, superName, superMcc, dataSize, dispatcher);
	}

	internal static bool DeleteCustomClass(ref MuiNativeServiceLease service, APTR mcc)
	{
		if (!MuiNativeProviderOwner.TryPrepareServicePlatform(ref service, out var classes, out var currentTask)) return false;
		return MuiOwnedClassServiceCore.DeleteCustomClass(ref classes, ref service, currentTask, mcc);
	}

	internal static APTR NewObject(ref MuiNativeServiceLease service, APTR className,
		APTR tags)
	{
		if (!MuiNativeProviderOwner.TryPrepareServicePlatform(ref service, out var classes, out _))
			return APTR.Null;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref memory,
				service.Operation.Owner, out var publicObjects)) return APTR.Null;
		return MuiNativePublicObjectCore.NewObject(ref classes,
			service.Operation.Service, service.Operation.OwnerRoot, publicObjects,
			className, tags);
	}

	internal static APTR MakeObjectA(ref MuiNativeServiceLease service,
		uint type, APTR parameters)
	{
		if (!MuiNativeProviderOwner.TryPrepareServicePlatform(ref service, out var classes, out _))
			return APTR.Null;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref memory,
			service.Operation.Owner, out var publicObjects)) return APTR.Null;
		return MuiMakeObjectServiceCore.MakeNativeObjectA(ref classes,
			service.Operation.Service, service.Operation.OwnerRoot, publicObjects,
			type, parameters);
	}

	internal static bool DisposeObject(ref MuiNativeServiceLease service, APTR obj)
	{
		if (!MuiNativeProviderOwner.TryPrepareServicePlatform(ref service, out var classes, out _))
			return false;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref memory,
			service.Operation.Owner, out var publicObjects)) return false;
		return MuiNativePublicObjectCore.DisposeObject(ref classes,
			service.Operation.Service, service.Operation.OwnerRoot, publicObjects, obj);
	}

	internal static void ChangeIDCMP(ref MuiNativeServiceLease service,
		APTR obj, uint flags, bool request)
	{
		if (!MuiNativeProviderOwner.TryPrepareServicePlatform(ref service,
			out var classes, out _)) return;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref memory,
			service.Operation.Owner, out var publicObjects)) return;
		MuiNativePublicObjectCore.ChangeIDCMP(ref classes, publicObjects,
			service.Operation.OwnerRoot, obj, flags, request);
	}

	internal static APTR AllocAslRequest(ref MuiNativeServiceLease service,
		uint requestType, APTR tags)
	{
		if (!MuiNativeProviderOwner.TryPrepareAslServicePlatform(ref service,
			out var asl, out var serviceState)) return APTR.Null;
		return MuiAslServiceCore.AllocAslRequest(ref asl, serviceState,
			requestType, tags);
	}

	internal static int AslRequest(ref MuiNativeServiceLease service,
		APTR requester, APTR tags)
	{
		if (!MuiNativeProviderOwner.TryPrepareAslServicePlatform(ref service,
			out var asl, out var serviceState)) return 0;
		return MuiAslServiceCore.AslRequest(ref asl, serviceState,
			requester, tags);
	}

	internal static bool FreeAslRequest(ref MuiNativeServiceLease service,
		APTR requester)
	{
		if (!MuiNativeProviderOwner.TryPrepareAslServicePlatform(ref service,
			out var asl, out var serviceState)) return false;
		return MuiAslServiceCore.FreeAslRequest(ref asl, serviceState,
			requester);
	}

	internal static int RequestA(ref MuiNativeServiceLease service,
		MuiRequesterCallRecord request)
	{
		if (!MuiNativeProviderOwner.TryPrepareRequesterServicePlatform(ref service,
			out var requester, out var serviceState)) return 0;
		return MuiRequesterServiceCore.Request(ref requester, serviceState,
			request);
	}

	internal static int RequestObjectA(ref MuiNativeServiceLease service,
		MuiRequesterCallRecord request)
	{
		if (!MuiNativeProviderOwner.TryPrepareRequesterServicePlatform(ref service,
			out var requester, out var serviceState)) return 0;
		return MuiRequesterServiceCore.RequestObject(ref requester, serviceState,
			request);
	}

	internal static bool Redraw(ref MuiNativeServiceLease service, APTR obj,
		uint flags)
	{
		if (!MuiNativeProviderOwner.TryPrepareServicePlatform(ref service,
			out var classes, out _)) return false;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref memory,
			service.Operation.Owner, out var publicObjects)) return false;
		return MuiNativeRedrawServiceCore.Redraw(ref classes, publicObjects,
			service.Operation.OwnerRoot, service.Operation.LibraryBase, obj, flags);
	}

	internal static bool Layout(ref MuiNativeServiceLease service, APTR obj,
		int left, int top, int width, int height, uint flags)
	{
		if (!MuiNativeProviderOwner.TryPrepareServicePlatform(ref service,
			out var classes, out _)) return false;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref memory,
			service.Operation.Owner, out var publicObjects)) return false;
		return MuiNativeLayoutServiceCore.Layout(ref classes, publicObjects,
			service.Operation.OwnerRoot, service.Operation.LibraryBase, obj,
			left, top, width, height, flags);
	}

	internal static APTR AddClipping(ref MuiNativeServiceLease service,
		APTR renderInfo, int left, int top, int width, int height)
	{
		if (!MuiNativeProviderOwner.TryPrepareDrawingServicePlatform(ref service,
			out var drawing, out var serviceState)) return APTR.Null;
		return MuiDrawingServiceCore.AddClipping(ref drawing, serviceState,
			renderInfo, left, top, width, height);
	}

	internal static bool RemoveClipping(ref MuiNativeServiceLease service,
		APTR renderInfo, APTR handle)
	{
		if (!MuiNativeProviderOwner.TryPrepareDrawingServicePlatform(ref service,
			out var drawing, out var serviceState)) return false;
		return MuiDrawingServiceCore.RemoveClipping(ref drawing, serviceState,
			renderInfo, handle);
	}

	internal static APTR AddClipRegion(ref MuiNativeServiceLease service,
		APTR renderInfo, APTR region)
	{
		if (!MuiNativeProviderOwner.TryPrepareDrawingServicePlatform(ref service,
			out var drawing, out var serviceState)) return APTR.Null;
		return MuiDrawingServiceCore.AddClipRegion(ref drawing, serviceState,
			renderInfo, region);
	}

	internal static bool RemoveClipRegion(ref MuiNativeServiceLease service,
		APTR renderInfo, APTR handle)
	{
		if (!MuiNativeProviderOwner.TryPrepareDrawingServicePlatform(ref service,
			out var drawing, out var serviceState)) return false;
		return MuiDrawingServiceCore.RemoveClipRegion(ref drawing, serviceState,
			renderInfo, handle);
	}

	internal static bool BeginRefresh(ref MuiNativeServiceLease service,
		APTR renderInfo, uint flags)
	{
		if (!MuiNativeProviderOwner.TryPrepareDrawingServicePlatform(ref service,
			out var drawing, out var serviceState)) return false;
		return MuiDrawingServiceCore.BeginRefresh(ref drawing, serviceState,
			renderInfo, flags);
	}

	internal static bool EndRefresh(ref MuiNativeServiceLease service,
		APTR renderInfo, uint flags)
	{
		if (!MuiNativeProviderOwner.TryPrepareDrawingServicePlatform(ref service,
			out var drawing, out var serviceState)) return false;
		return MuiDrawingServiceCore.EndRefresh(ref drawing, serviceState,
			renderInfo, flags);
	}

	internal static int ObtainPen(ref MuiNativeServiceLease service,
		APTR renderInfo, APTR penSpec, uint flags)
	{
		if (!MuiNativeProviderOwner.TryPrepareDrawingServicePlatform(ref service,
			out var drawing, out var serviceState)) return -1;
		return MuiDrawingServiceCore.ObtainPen(ref drawing, serviceState,
			renderInfo, penSpec, flags);
	}

	internal static bool ReleasePen(ref MuiNativeServiceLease service,
		APTR renderInfo, int pen)
	{
		if (!MuiNativeProviderOwner.TryPrepareDrawingServicePlatform(ref service,
			out var drawing, out var serviceState)) return false;
		return MuiDrawingServiceCore.ReleasePen(ref drawing, serviceState,
			renderInfo, pen);
	}

	internal static bool GetRGBColor(ref MuiNativeServiceLease service,
		APTR renderInfo, APTR penSpec, APTR rgbColor)
	{
		if (!MuiNativeProviderOwner.TryPrepareDrawingServicePlatform(ref service,
			out var drawing, out var serviceState)) return false;
		return MuiDrawingServiceCore.GetRGBColor(ref drawing, serviceState,
			renderInfo, penSpec, rgbColor);
	}
}
