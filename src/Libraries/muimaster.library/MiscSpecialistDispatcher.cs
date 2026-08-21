/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscLifecycleMessage
{
	public const uint Size = 4;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscSpecialistGetMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscSpecialistMethodMessage
{
	public const uint Size = 4;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscSpecialistSetMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscSpecialistPointerMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Pointer;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscSpecialistPairMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint First;
	public uint Second;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscSpecialistRegisterGadgetMessage
{
	public const uint Size = 28;
	public uint MethodId;
	public uint Gadget;
	public uint Id;
	public uint Parameters;
	public uint Title;
	public uint Attribute;
	public uint Label;
}

// Routes the final MG09 misc-family specialist method packets to the
// guest-resident misc core. This dispatcher is standalone: it operates on a
// validated misc instance block and never chains into the frozen common-control,
// collection, pen/color, Pop or generic dispatchers, so those frozen cores and
// dispatchers are left unmodified. A method is only claimed when the target
// instance is a valid misc specialist; everything else returns "not claimed" so
// an outer router can continue without a Specialist -> Common recursion.
//
// The set/get packets follow the established single-tag convention used across
// the library (method id, attribute id, value) plus the BOOPSI OM_GET storage
// form. The class-specific methods carry their documented fixed argument frames
// (MUIP_Panel_Run, MUIP_Filepanel_AddRow, MUIP_Mccprefs_*, MUIP_Title_*).
public static class MuiMiscSpecialistDispatcher
{
	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR instance,
		APTR message) where TPlatform : struct, IMuiServicePlatform
	{
		return TryDispatch(ref platform, instance, message, out var result)
			? result : 0u;
	}

	public static bool TryDispatch<TPlatform>(ref TPlatform platform,
		APTR instance, APTR message, out uint result)
		where TPlatform : struct, IMuiServicePlatform
	{
		result = 0;
		if (!MuiMiscSpecialistCore.Valid(ref platform, instance) ||
			!MuiMiscSpecialistMessageCodec.TryReadMethodId(ref platform, message,
				out var header)) return false;
		var method = header.MethodId;

			switch (method)
		{
			case MuiMiscSpecialistMessageCodec.OmDispose:
				if (!MuiMiscSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiMiscSpecialistMessageCodec.OmDispose,
					out _)) return true;
				result = MuiMiscSpecialistLifecycle.Dispose(ref platform, instance)
					? 1u : 0u;
				return true;

			case MuiMiscSpecialistMessageCodec.OmGet:
				// struct opGet { ULONG MethodID; ULONG opg_AttrID; ULONG *opg_Storage; }
				if (!MuiMiscSpecialistMessageCodec.TryReadGet(ref platform, message,
					out var getPacket))
					return true;
				var storage = APTR.FromPointer(getPacket.Storage);
				if (MuiMiscSpecialistCore.GetAttribute(ref platform, instance,
					getPacket.Attribute, out var value) &&
					storage.IsNotNull && platform.IsMapped(storage,
						MuiGuestUlongStorage.Size))
				{
					MuiGuestUlongStorageCodec.WriteValue(ref platform, storage, value);
					result = 1u;
				}
				return true;

			case MuiMiscSpecialistMessageCodec.MethodSet:
			case MuiMiscSpecialistMessageCodec.MethodNoNotifySet:
				// Single-tag set frame: { ULONG MethodID; ULONG attr; ULONG value }.
				if (!MuiMiscSpecialistMessageCodec.TryReadSet(ref platform, message, method,
					out var setPacket)) return true;
				result = MuiMiscSpecialistCore.SetAttribute(ref platform, instance,
					setPacket.Attribute, setPacket.Value, false,
					method == MuiMiscSpecialistMessageCodec.MethodSet,
					out _) ? 1u : 0u;
				return true;

			case MuiMiscAttributes.Setup:
			case MuiMiscAttributes.Cleanup:
				// Exact no-argument lifecycle frame: { ULONG MethodID }.
				if (!MuiMiscSpecialistMessageCodec.TryReadLifecycle(ref platform,
					message, method, out _))
					return true;
				result = method == MuiMiscAttributes.Setup
					? (MuiMiscSpecialistCore.Setup(ref platform, instance) ? 1u : 0u)
					: (MuiMiscSpecialistCore.Cleanup(ref platform, instance) ? 1u : 0u);
				return true;

			case MuiMiscAttributes.Panel_Run:
				// struct { ULONG MethodID; MUIApplication *app; MUIWindow *win; }
				if (!MuiMiscSpecialistMessageCodec.TryReadPair(ref platform, message,
					MuiMiscAttributes.Panel_Run,
					out var panelPacket)) return true;
				result = MuiMiscSpecialistCore.PanelRun(ref platform, instance,
					APTR.FromPointer(panelPacket.First),
					APTR.FromPointer(panelPacket.Second)) ? 1u : 0u;
				return true;

			case MuiMiscAttributes.Filepanel_AddRow:
				// struct { ULONG MethodID; MUIArea *label; MUIArea *contents; }
				if (!MuiMiscSpecialistMessageCodec.TryReadPair(ref platform, message,
					MuiMiscAttributes.Filepanel_AddRow, out var rowPacket)) return true;
				result = MuiMiscSpecialistCore.FilepanelAddRow(ref platform, instance,
					APTR.FromPointer(rowPacket.First),
					APTR.FromPointer(rowPacket.Second)) ? 1u : 0u;
				return true;

			case MuiMiscAttributes.Mccprefs_ConfigToGadgets:
				// struct { ULONG MethodID; Object *configdata; }
				if (!MuiMiscSpecialistMessageCodec.TryReadPointer(ref platform, message,
					MuiMiscAttributes.Mccprefs_ConfigToGadgets,
					out var configPacket)) return true;
				result = MuiMiscSpecialistCore.MccprefsConfigToGadgets(ref platform,
					instance, APTR.FromPointer(configPacket.Pointer))
					? 1u : 0u;
				return true;

			case MuiMiscAttributes.Mccprefs_GadgetsToConfig:
				// struct { ULONG MethodID; Object *configdata; Object *originator; }
				if (!MuiMiscSpecialistMessageCodec.TryReadPair(ref platform, message,
					MuiMiscAttributes.Mccprefs_GadgetsToConfig,
					out var gadgetsPacket)) return true;
				result = MuiMiscSpecialistCore.MccprefsGadgetsToConfig(ref platform,
					instance, APTR.FromPointer(gadgetsPacket.First),
					APTR.FromPointer(gadgetsPacket.Second)) ? 1u : 0u;
				return true;

			case MuiMiscAttributes.Mccprefs_RegisterGadget:
				// struct { ULONG MethodID; Object *gadget; ULONG id; ULONG params;
				//          STRPTR title; ULONG attr; Object *label; }
				if (!MuiMiscSpecialistMessageCodec.TryReadRegisterGadget(ref platform,
					message,
					out var gadgetPacket)) return true;
				result = MuiMiscSpecialistCore.MccprefsRegisterGadget(ref platform,
					instance, APTR.FromPointer(gadgetPacket.Gadget),
					gadgetPacket.Id, gadgetPacket.Parameters,
					APTR.FromPointer(gadgetPacket.Title), gadgetPacket.Attribute,
					APTR.FromPointer(gadgetPacket.Label)) ? 1u : 0u;
				return true;

			case MuiMiscAttributes.Title_New:
				// struct { ULONG MethodID; }  -> returns the fresh page handle.
				if (!MuiMiscSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiMiscAttributes.Title_New,
					out _)) return true;
				result = MuiMiscSpecialistCore.TitleNew(ref platform, instance);
				return true;

			case MuiMiscAttributes.Title_Close:
				// struct { ULONG MethodID; Object *tito; }
				if (!MuiMiscSpecialistMessageCodec.TryReadPointer(ref platform, message,
					MuiMiscAttributes.Title_Close, out var closePacket)) return true;
				result = MuiMiscSpecialistCore.TitleClose(ref platform, instance,
					closePacket.Pointer) ? 1u : 0u;
				return true;

			case MuiMiscAttributes.Title_FindPage:
				// struct { ULONG MethodID; Object *titlebutton; }
				if (!MuiMiscSpecialistMessageCodec.TryReadPointer(ref platform, message,
					MuiMiscAttributes.Title_FindPage, out var findPacket)) return true;
				result = MuiMiscSpecialistCore.TitleFindPage(ref platform, instance,
					findPacket.Pointer);
				return true;
		}
		return false;
	}

}
