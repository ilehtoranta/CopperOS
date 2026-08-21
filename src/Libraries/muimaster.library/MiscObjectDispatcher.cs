/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Object-aware service dispatcher for Misc sidecars attached by the public
// factories. The fixed Misc instance remains the packet target for the family
// core, but OM_DISPOSE must use the object-aware lifecycle so the sidecar is
// detached/freed before the headless object record is released. Unclaimed
// packets return zero so an outer generic dispatcher can continue without
// making this additive family closure depend on the frozen dispatcher.
public static class MuiMiscObjectDispatcher
{
	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message) where TPlatform : struct, IMuiServicePlatform
	{
		return TryDispatch(ref platform, state, obj, message, out var result)
			? result : 0u;
	}

	public static bool TryDispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message, out uint result)
		where TPlatform : struct, IMuiServicePlatform
	{
		result = 0;
		var instance = MuiMiscSpecialistCore.ObjectInstance(ref platform, state,
			obj);
		if (instance.IsNull || !MuiMiscSpecialistMessageCodec.TryReadMethodId(
			ref platform, message, out var header)) return false;
		var method = header.MethodId;
		if (method == MuiMiscSpecialistMessageCodec.OmDispose)
		{
			result = MuiMiscSpecialistLifecycle.Dispose(ref platform, state, obj)
				? 1u : 0u;
			return true;
		}
		if (method == MuiMiscAttributes.Setup ||
			method == MuiMiscAttributes.Cleanup)
		{
			// Object-aware route uses the same exact packed { MethodID } ABI as
			// the standalone route. Do not accept a partially mapped frame.
			if (!MuiMiscSpecialistMessageCodec.TryReadLifecycle(ref platform, message,
				method, out _)) return true;
			result = method == MuiMiscAttributes.Setup
				? (MuiMiscSpecialistCore.Setup(ref platform, instance) ? 1u : 0u)
				: (MuiMiscSpecialistCore.Cleanup(ref platform, instance) ? 1u : 0u);
			return true;
		}
		if (method == MuiMiscSpecialistMessageCodec.OmGet)
		{
			if (!MuiMiscSpecialistMessageCodec.TryReadGet(ref platform, message,
				out var packet)) return true;
			var storage = APTR.FromPointer(packet.Storage);
			if (MuiMiscSpecialistCore.GetAttribute(ref platform, instance,
				packet.Attribute, out var value) &&
				storage.IsNotNull && platform.IsMapped(storage,
					MuiGuestUlongStorage.Size))
			{
				MuiGuestUlongStorageCodec.WriteValue(ref platform, storage, value);
				result = 1u;
			}
			return true;
		}
		if (method == MuiMiscSpecialistMessageCodec.MethodSet ||
			method == MuiMiscSpecialistMessageCodec.MethodNoNotifySet)
		{
			if (!MuiMiscSpecialistMessageCodec.TryReadSet(ref platform, message,
				method, out var packet))
				return true;
			result = MuiMiscSpecialistCore.SetAttribute(ref platform, instance,
				packet.Attribute, packet.Value, false,
				method == MuiMiscSpecialistMessageCodec.MethodSet,
				out _) ? 1u : 0u;
			return true;
		}
		if (method == MuiMiscAttributes.Title_New)
		{
			result = MuiMiscSpecialistCore.TitleNew(ref platform, instance);
			return true;
		}
		if (method == MuiMiscAttributes.Title_Close)
		{
			if (!MuiMiscSpecialistMessageCodec.TryReadPointer(ref platform, message,
				method, out var packet))
				return true;
			result = MuiMiscSpecialistCore.TitleClose(ref platform, instance,
				packet.Pointer) ? 1u : 0u;
			return true;
		}
		if (method == MuiMiscAttributes.Title_FindPage)
		{
			if (!MuiMiscSpecialistMessageCodec.TryReadPointer(ref platform, message,
				method, out var packet))
				return true;
			result = MuiMiscSpecialistCore.TitleFindPage(ref platform, instance,
				packet.Pointer);
			return true;
		}
		if (method == MuiMiscAttributes.Panel_Run)
		{
			if (!MuiMiscSpecialistMessageCodec.TryReadPair(ref platform, message,
				method, out var packet))
				return true;
			result = MuiMiscSpecialistCore.PanelRun(ref platform, instance,
				APTR.FromPointer(packet.First), APTR.FromPointer(packet.Second))
				? 1u : 0u;
			return true;
		}
		if (method == MuiMiscAttributes.Filepanel_AddRow)
		{
			if (!MuiMiscSpecialistMessageCodec.TryReadPair(ref platform, message,
				method, out var packet))
				return true;
			result = MuiMiscSpecialistCore.FilepanelAddRow(ref platform, instance,
				APTR.FromPointer(packet.First), APTR.FromPointer(packet.Second))
				? 1u : 0u;
			return true;
		}
		if (method == MuiMiscAttributes.Mccprefs_RegisterGadget)
		{
			if (!MuiMiscSpecialistMessageCodec.TryReadRegisterGadget(ref platform,
				message,
				out var packet)) return true;
			result = MuiMiscSpecialistCore.MccprefsRegisterGadget(ref platform,
				instance, APTR.FromPointer(packet.Gadget), packet.Id,
				packet.Parameters, APTR.FromPointer(packet.Title), packet.Attribute,
				APTR.FromPointer(packet.Label)) ? 1u : 0u;
			return true;
		}
		if (method == MuiMiscAttributes.Mccprefs_ConfigToGadgets)
		{
			if (!MuiMiscSpecialistMessageCodec.TryReadPointer(ref platform, message,
				method, out var packet))
				return true;
			result = MuiMiscSpecialistCore.MccprefsConfigToGadgets(ref platform,
				instance, APTR.FromPointer(packet.Pointer))
				? 1u : 0u;
			return true;
		}
		if (method == MuiMiscAttributes.Mccprefs_GadgetsToConfig)
		{
			if (!MuiMiscSpecialistMessageCodec.TryReadPair(ref platform, message,
				method, out var packet))
				return true;
			result = MuiMiscSpecialistCore.MccprefsGadgetsToConfig(ref platform,
				instance, APTR.FromPointer(packet.First),
				APTR.FromPointer(packet.Second)) ? 1u : 0u;
			return true;
		}
		return false;
	}

}
