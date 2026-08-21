/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPopSpecialistMethodMessage
{
	public const uint Size = 4;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPopSpecialistGetMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPopSpecialistSetMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPopSpecialistCloseMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Result;
}

// Routes MG09 Pop* specialist method packets to the guest-resident Pop core.
// This dispatcher is standalone: it operates on a validated Pop instance block
// and never chains into the frozen common-control, collection, pen/color or
// generic dispatchers, so those frozen cores and dispatchers are left
// unmodified. A method is only claimed when the target instance is a valid Pop
// specialist; everything else returns "not claimed" so an outer router (if any)
// can continue without a Specialist -> Common recursion.
//
// The set/get packets follow the established single-tag convention used across
// the library (method id, attribute id, value) plus the BOOPSI OM_GET storage
// form. The Popstring Open/Close and the shared Setup/Cleanup/HandleInput
// methods carry no arguments beyond their method id.
public static class MuiPopSpecialistDispatcher
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
		if (!MuiPopSpecialistMessageCodec.TryReadMethodId(ref platform, message,
			out var methodHeader) ||
			!MuiPopSpecialistCore.Valid(ref platform, instance)) return false;
		var method = methodHeader.MethodId;

		switch (method)
		{
			case MuiPopSpecialistMessageCodec.OmDispose:
				if (!MuiPopSpecialistMessageCodec.IsValidMethod(ref platform, message,
					MuiPopSpecialistMessageCodec.OmDispose)) return true;
				result = MuiPopSpecialistLifecycle.Dispose(ref platform, instance)
					? 1u : 0u;
				return true;

			case MuiPopSpecialistMessageCodec.OmGet:
				if (!MuiPopSpecialistMessageCodec.TryReadGet(ref platform, message,
					out var getPacket)) return true;
				var storage = APTR.FromPointer(getPacket.Storage);
				if (MuiPopSpecialistCore.GetAttribute(ref platform, instance,
					getPacket.Attribute, out var value) &&
					storage.IsNotNull && platform.IsMapped(storage,
						MuiGuestUlongStorage.Size))
				{
					MuiGuestUlongStorageCodec.WriteValue(ref platform, storage, value);
					result = 1u;
				}
				return true;

			case MuiPopSpecialistMessageCodec.MethodSet:
			case MuiPopSpecialistMessageCodec.MethodNoNotifySet:
				if (!MuiPopSpecialistMessageCodec.TryReadSet(ref platform, message,
					method,
					out var setPacket)) return true;
				result = MuiPopSpecialistCore.SetAttribute(ref platform, instance,
					setPacket.Attribute, setPacket.Value, false,
					method == MuiPopSpecialistMessageCodec.MethodSet,
					out _) ? 1u : 0u;
				return true;

			case MuiPopAttributes.Popstring_Open:
				if (!MuiPopSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiPopAttributes.Popstring_Open, out _)) return true;
				result = MuiPopSpecialistCore.Open(ref platform, instance) ? 1u : 0u;
				return true;

			case MuiPopAttributes.Popstring_Close:
				// MUIM_Popstring_Close(BOOL result): { ULONG MethodID; LONG result }.
				if (!MuiPopSpecialistMessageCodec.TryReadClose(ref platform, message,
					out var closePacket))
					return true;
				result = MuiPopSpecialistCore.Close(ref platform, instance,
					closePacket.Result) ? 1u : 0u;
				return true;

			case MuiPopAttributes.HandleInput:
				if (!MuiPopSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiPopAttributes.HandleInput, out _)) return true;
				result = MuiPopSpecialistCore.HandleInput(ref platform, instance)
					? 1u : 0u;
				return true;

			case MuiPopAttributes.Setup:
				if (!MuiPopSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiPopAttributes.Setup, out _)) return true;
				result = MuiPopSpecialistCore.Setup(ref platform, instance) ? 1u : 0u;
				return true;

			case MuiPopAttributes.Cleanup:
				if (!MuiPopSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiPopAttributes.Cleanup, out _)) return true;
				result = MuiPopSpecialistCore.Cleanup(ref platform, instance)
					? 1u : 0u;
				return true;
		}
		return false;
	}
}
