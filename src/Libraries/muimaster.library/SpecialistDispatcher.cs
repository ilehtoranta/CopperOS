/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiColorSpecialistMethodMessage
{
	public const uint Size = 4;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiColorSpecialistGetMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiColorSpecialistSetMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiColorSpecialistPointerMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Pointer;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiColorSpecialistRgbMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint Red;
	public uint Green;
	public uint Blue;
}

// Routes MG09 pen/color specialist method packets to the guest-resident
// specialist core. This dispatcher is standalone: it operates on a validated
// specialist instance block and never chains into the frozen common-control,
// collection or generic dispatchers, so those frozen cores and dispatchers are
// left unmodified. A method is only claimed when the target instance is a valid
// specialist; everything else returns "not claimed" so an outer router (if any)
// can continue without a Specialist -> Common recursion.
//
// The set/get packets follow the established single-tag convention used across
// the library (method id, attribute id, value) plus the BOOPSI OM_GET storage
// form. The three Pendisplay methods carry their documented fixed argument
// frames.
public static class MuiColorSpecialistDispatcher
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
		if (!MuiColorSpecialistMessageCodec.TryReadMethodId(ref platform, message,
			out var methodHeader) ||
			!MuiColorSpecialistCore.Valid(ref platform, instance)) return false;
		var method = methodHeader.MethodId;

		switch (method)
		{
			case MuiColorSpecialistMessageCodec.OmDispose:
				if (!MuiColorSpecialistMessageCodec.IsValidMethod(ref platform,
					message, MuiColorSpecialistMessageCodec.OmDispose)) return true;
				result = MuiColorSpecialistLifecycle.Dispose(ref platform, instance)
					? 1u : 0u;
				return true;

			case MuiColorSpecialistMessageCodec.OmGet:
				// struct opGet { ULONG MethodID; ULONG opg_AttrID; ULONG *opg_Storage; }
				if (!MuiColorSpecialistMessageCodec.TryReadGet(ref platform, message,
					out var getPacket))
					return true;
				var storage = APTR.FromPointer(getPacket.Storage);
				if (MuiColorSpecialistCore.GetAttribute(ref platform, instance,
					getPacket.Attribute, out var value) &&
					storage.IsNotNull && platform.IsMapped(storage,
						MuiGuestUlongStorage.Size))
				{
					MuiGuestUlongStorageCodec.WriteValue(ref platform, storage, value);
					result = 1u;
				}
				return true;

			case MuiColorSpecialistMessageCodec.MethodSet:
			case MuiColorSpecialistMessageCodec.MethodNoNotifySet:
				// Single-tag set frame: { ULONG MethodID; ULONG attr; ULONG value }.
				if (!MuiColorSpecialistMessageCodec.TryReadSet(ref platform, message, method,
					out var setPacket)) return true;
				result = MuiColorSpecialistCore.SetAttribute(ref platform, instance,
					setPacket.Attribute, setPacket.Value, false,
					method == MuiColorSpecialistMessageCodec.MethodSet,
					out _) ? 1u : 0u;
				return true;

			case MuiColorSpecialistMessageCodec.SetColormap:
				if (!MuiColorSpecialistMessageCodec.TryReadPointer(ref platform, message,
					MuiColorSpecialistMessageCodec.SetColormap,
					out var colormapPacket)) return true;
				result = MuiColorSpecialistCore.SetColormap(ref platform, instance,
					colormapPacket.Pointer) ? 1u : 0u;
				return true;

			case MuiColorSpecialistMessageCodec.SetMUIPen:
				if (!MuiColorSpecialistMessageCodec.TryReadPointer(ref platform, message,
					MuiColorSpecialistMessageCodec.SetMUIPen,
					out var muiPenPacket)) return true;
				result = MuiColorSpecialistCore.SetMUIPen(ref platform, instance,
					muiPenPacket.Pointer) ? 1u : 0u;
				return true;

			case MuiColorSpecialistMessageCodec.SetRGB:
				if (!MuiColorSpecialistMessageCodec.TryReadRgb(ref platform, message,
					out var rgbPacket)) return true;
				result = MuiColorSpecialistCore.SetRGB(ref platform, instance,
					rgbPacket.Red, rgbPacket.Green, rgbPacket.Blue) ? 1u : 0u;
				return true;
		}
		return false;
	}



}
