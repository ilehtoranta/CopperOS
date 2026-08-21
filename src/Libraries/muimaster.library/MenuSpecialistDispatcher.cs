/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMenuSpecialistMethodMessage
{
	public const uint Size = 4;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMenuSpecialistGetMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMenuSpecialistSetMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMenuSpecialistPointerMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint ObjectPointer;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMenuSpecialistPairMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint First;
	public uint Second;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMenuSpecialistPopupMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint Window;
	public uint X;
	public uint Y;
}

// Routes MG09 menu specialist method packets (Menustrip.mui / Menu.mui /
// Menuitem.mui) to the guest-resident menu core. Unlike the standalone Pop* and
// pen/color dispatchers, the menu family is a genuine Family.mui hierarchy, so
// this dispatcher operates on a headless object plus the shared state and
// delegates hierarchy verbs to MuiFamilyCore through the core's containment
// rules. It is additive: the public headless route invokes TryDispatch before
// its existing generic switch, and this specialist only claims a method when
// the target object is a valid menu specialist. Unclaimed packets fall through
// to that generic route. The generic dispatcher never calls back into this
// specialist, preventing recursion.
//
// The set/get packets follow the established single-tag convention used across
// the library (method id, attribute id, value) plus the BOOPSI OM_GET storage
// form. The Family verbs and the four Menustrip methods carry their documented
// fixed argument frames.
public static class MuiMenuSpecialistDispatcher
{
	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message) where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryDispatch(ref platform, state, obj, message, out var result)
			? result : MuiHeadlessDispatcher.Dispatch(ref platform, state, obj,
				message);
	}

	public static bool TryDispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message, out uint result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = 0;
		if (!MuiMenuSpecialistMessageCodec.TryReadMethodId(ref platform, message,
			out var methodHeader) ||
			!MuiMenuSpecialistCore.Valid(ref platform, state, obj)) return false;
		var method = methodHeader.MethodId;

		switch (method)
		{
			case MuiMenuSpecialistMessageCodec.OmDispose:
				if (!MuiMenuSpecialistMessageCodec.IsValidMethod(ref platform, message,
					MuiMenuSpecialistMessageCodec.OmDispose)) return true;
				result = MuiMenuSpecialistLifecycle.Dispose(ref platform, state, obj)
					? 1u : 0u;
				return true;

			case MuiMenuSpecialistMessageCodec.OmGet:
				if (!MuiMenuSpecialistMessageCodec.TryReadGet(ref platform, message,
					out var getPacket))
					return true;
				var storage = APTR.FromPointer(getPacket.Storage);
				if (MuiMenuSpecialistCore.GetAttribute(ref platform, state, obj,
					getPacket.Attribute, out var value) &&
					storage.IsNotNull && platform.IsMapped(storage,
						MuiGuestUlongStorage.Size))
				{
					MuiGuestUlongStorageCodec.WriteValue(ref platform, storage, value);
					result = 1u;
				}
				return true;

			case MuiMenuSpecialistMessageCodec.MethodSet:
			case MuiMenuSpecialistMessageCodec.MethodNoNotifySet:
				if (!MuiMenuSpecialistMessageCodec.TryReadSet(ref platform, message,
					method,
					out var setPacket)) return true;
				result = MuiMenuSpecialistCore.SetAttribute(ref platform, state, obj,
					setPacket.Attribute, setPacket.Value, false,
					method == MuiMenuSpecialistMessageCodec.MethodSet,
					out _) ? 1u : 0u;
				return true;

			// -- Family hierarchy verbs -- { MethodID; Object *obj [; extra] } --
			case MuiMenuAttributes.Family_AddTail:
				if (!MuiMenuSpecialistMessageCodec.TryReadPointer(ref platform, message,
					MuiMenuAttributes.Family_AddTail, out var addTailPacket)) return true;
				result = MuiMenuSpecialistCore.AddChild(ref platform, state, obj,
					APTR.FromPointer(addTailPacket.ObjectPointer)) ? 1u : 0u;
				return true;
			case MuiMenuAttributes.Family_AddHead:
				if (!MuiMenuSpecialistMessageCodec.TryReadPointer(ref platform, message,
					MuiMenuAttributes.Family_AddHead, out var addHeadPacket)) return true;
				result = MuiMenuSpecialistCore.AddHeadChild(ref platform, state, obj,
					APTR.FromPointer(addHeadPacket.ObjectPointer)) ? 1u : 0u;
				return true;
			case MuiMenuAttributes.Family_Insert:
				// { MethodID; Object *obj; Object *predecessor }
				if (!MuiMenuSpecialistMessageCodec.TryReadPair(ref platform, message,
					MuiMenuAttributes.Family_Insert, out var insertPacket)) return true;
				result = MuiMenuSpecialistCore.InsertChild(ref platform, state, obj,
					APTR.FromPointer(insertPacket.First),
					APTR.FromPointer(insertPacket.Second)) ? 1u : 0u;
				return true;
			case MuiMenuAttributes.Family_Remove:
				if (!MuiMenuSpecialistMessageCodec.TryReadPointer(ref platform, message,
					MuiMenuAttributes.Family_Remove, out var removePacket)) return true;
				result = MuiMenuSpecialistCore.RemoveChild(ref platform, state, obj,
					APTR.FromPointer(removePacket.ObjectPointer)) ? 1u : 0u;
				return true;
			case MuiMenuAttributes.Family_Sort:
				if (!MuiMenuSpecialistMessageCodec.TryReadPointer(ref platform, message,
					MuiMenuAttributes.Family_Sort, out var sortPacket)) return true;
				result = MuiFamilyCore.Sort(ref platform, state, obj,
					APTR.FromPointer(sortPacket.ObjectPointer)) ? 1u : 0u;
				return true;
			case MuiMenuAttributes.Family_Reorder:
				// { MethodID; Object *after; Object **objects }
				if (!MuiMenuSpecialistMessageCodec.TryReadPair(ref platform, message,
					MuiMenuAttributes.Family_Reorder, out var reorderPacket)) return true;
				result = MuiFamilyCore.Reorder(ref platform, state, obj,
					APTR.FromPointer(reorderPacket.First),
					APTR.FromPointer(reorderPacket.Second)) ? 1u : 0u;
				return true;
			case MuiMenuAttributes.Family_Transfer:
				if (!MuiMenuSpecialistMessageCodec.TryReadPointer(ref platform, message,
					MuiMenuAttributes.Family_Transfer, out var transferPacket)) return true;
				result = MuiFamilyCore.Transfer(ref platform, state,
					APTR.FromPointer(transferPacket.ObjectPointer), obj) ? 1u : 0u;
				return true;

			// -- Menustrip methods --
			case MuiMenuAttributes.Menustrip_InitChange:
				if (!MuiMenuSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiMenuAttributes.Menustrip_InitChange, out _)) return true;
				result = MuiMenuSpecialistCore.InitChange(ref platform, state, obj)
					? 1u : 0u;
				return true;
			case MuiMenuAttributes.Menustrip_ExitChange:
				if (!MuiMenuSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiMenuAttributes.Menustrip_ExitChange, out _)) return true;
				result = MuiMenuSpecialistCore.ExitChange(ref platform, state, obj)
					? 1u : 0u;
				return true;
			case MuiMenuAttributes.Menustrip_WillOpen:
				if (!MuiMenuSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiMenuAttributes.Menustrip_WillOpen, out _)) return true;
				result = MuiMenuSpecialistCore.WillOpen(ref platform, state, obj)
					? 1u : 0u;
				return true;
			case MuiMenuAttributes.Menustrip_Popup:
				// { MethodID; Object *window; LONG x; LONG y } -- coordinates are
				// validated for frame shape but not otherwise interpreted here.
				if (!MuiMenuSpecialistMessageCodec.TryReadPopup(ref platform, message,
					out _)) return true;
				result = MuiMenuSpecialistCore.Popup(ref platform, state, obj)
					? 1u : 0u;
				return true;
		}
		return false;
	}
}
