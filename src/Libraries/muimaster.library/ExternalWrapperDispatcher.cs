/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalUpdateMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint AttributeList;
	public uint GadgetInfo;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalGetMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalSetMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalMethodMessage
{
	public const uint Size = 4;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalRenderInfoMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint RenderInfo;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalAskMinMaxMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalLayoutMessage
{
	public const uint Size = 20;
	public uint MethodId;
	public uint Left;
	public uint Top;
	public uint Width;
	public uint Height;
}

// Routes MG09 external-resource wrapper (Boopsi.mui / Dtpic.mui) method packets
// to the guest-resident wrapper core. This dispatcher is standalone: it
// operates on a validated wrapper instance block and never chains into the
// frozen common-control, collection, layout or generic dispatchers, so those
// frozen cores and dispatchers are left unmodified. A method is only claimed
// when the target instance is a valid wrapper; everything else returns "not
// claimed" so an outer router (if any) can continue without a Specialist ->
// Common recursion.
//
// The set/get packets follow the established single-tag convention used across
// the library (method id, attribute id, value) plus the BOOPSI OM_GET storage
// form. An unrecognized set/get on a Boopsi instance is passed through to the
// wrapped boopsi object, matching MUI's documented transparency. OM_UPDATE
// carries a changed-attribute tag list that is mapped to MUI notifications.
public static class MuiExternalWrapperDispatcher
{
	private const uint OmDispose = 0x00000102u;
	private const uint OmGet = 0x00000104u;
	private const uint OmUpdate = 0x00000108u;
	private const uint MethodSet = 0x8042549au;      // MUIM_Set
	private const uint MethodNoNotifySet = 0x8042216fu; // MUIM_NoNotifySet

	private const uint AskMinMax = 0x80423874u;
	private const uint Layout = 0x8042845bu;
	private const uint Setup = 0x80428354u;
	private const uint Cleanup = 0x8042d985u;
	private const uint Show = 0x8042cc84u;
	private const uint Hide = 0x8042f20fu;
	private const uint Draw = 0x80426f3fu;

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
		if (!MuiExternalWrapperMessageCodec.TryReadMethodId(ref platform, message,
			out var methodHeader) ||
			!MuiExternalWrapperCore.Valid(ref platform, instance)) return false;
		var method = methodHeader.MethodId;

		switch (method)
		{
			case OmDispose:
				result = MuiExternalWrapperLifecycle.Dispose(ref platform, instance)
					? 1u : 0u;
				return true;

			case OmGet:
				// struct opGet { ULONG MethodID; ULONG opg_AttrID; ULONG *opg_Storage }
				if (!TryReadGet(ref platform, message, out var getPacket))
					return true;
				var attr = getPacket.Attribute;
				var storage = APTR.FromPointer(getPacket.Storage);
				var got = MuiExternalWrapperCore.GetAttribute(ref platform, instance,
					attr, out var value);
				if (!got)
					got = MuiExternalWrapperCore.PassThroughGet(ref platform, instance,
						attr, out value);
				if (got && storage.IsNotNull && platform.IsMapped(storage,
					MuiGuestUlongStorage.Size))
				{
					MuiGuestUlongStorageCodec.WriteValue(ref platform, storage, value);
					result = 1u;
				}
				return true;

			case MethodSet:
			case MethodNoNotifySet:
				// Single-tag set frame: { ULONG MethodID; ULONG attr; ULONG value }.
				if (!TryReadSet(ref platform, message, method,
					out var setPacket)) return true;
				var setAttr = setPacket.Attribute;
				var setValue = setPacket.Value;
				MuiExternalWrapperCore.SetAttribute(ref platform, instance, setAttr,
					setValue, false, method == MethodSet, out _, out var handled);
				if (!handled)
					result = MuiExternalWrapperCore.PassThroughSet(ref platform,
						instance, setAttr, setValue) ? 1u : 0u;
				else
					result = 1u;
				return true;

			case OmUpdate:
				// struct opUpdate { ULONG MethodID; struct TagItem *opu_AttrList;
				//                   struct GadgetInfo *opu_GInfo; ULONG opu_Flags }
				if (!TryReadUpdate(ref platform, message, out var updatePacket))
					return true;
				result = MuiExternalWrapperCore.HandleUpdate(ref platform, instance,
					APTR.FromPointer(updatePacket.AttributeList));
				return true;

			case Setup:
				// struct MUIP_Setup { ULONG MethodID; struct MUI_RenderInfo *ri; }
				if (!TryReadRenderInfo(ref platform, message, Setup,
					out var setupPacket)) return true;
				var renderInfo = APTR.FromPointer(setupPacket.RenderInfo);
				result = MuiExternalWrapperCore.Setup(ref platform, instance,
					renderInfo) ? 1u : 0u;
				return true;

			case Cleanup:
				if (!IsValidMethod(ref platform, message, Cleanup)) return true;
				result = MuiExternalWrapperCore.Cleanup(ref platform, instance)
					? 1u : 0u;
				return true;

			case Show:
				if (!IsValidMethod(ref platform, message, Show)) return true;
				result = MuiExternalWrapperCore.Show(ref platform, instance)
					? 1u : 0u;
				return true;

			case Hide:
				if (!IsValidMethod(ref platform, message, Hide)) return true;
				result = MuiExternalWrapperCore.Hide(ref platform, instance)
					? 1u : 0u;
				return true;

			case Draw:
				if (!IsValidMethod(ref platform, message, Draw)) return true;
				result = MuiExternalWrapperCore.Draw(ref platform, instance)
					? 1u : 0u;
				return true;

			case AskMinMax:
				// struct MUIP_AskMinMax { ULONG MethodID; struct MUI_MinMax *MinMaxInfo }
				if (!TryReadAskMinMax(ref platform, message, out var minMaxPacket))
					return true;
				result = MuiExternalWrapperCore.AskMinMax(ref platform, instance,
					APTR.FromPointer(minMaxPacket.Storage)) ? 1u : 0u;
				return true;

			case Layout:
				// { ULONG MethodID; LONG left; LONG top; LONG width; LONG height }
				if (!TryReadLayout(ref platform, message, out var layoutPacket))
					return true;
				result = MuiExternalWrapperCore.ApplyGeometry(ref platform, instance,
					unchecked((int)layoutPacket.Left),
					unchecked((int)layoutPacket.Top),
					unchecked((int)layoutPacket.Width),
					unchecked((int)layoutPacket.Height)) ? 1u : 0u;
				return true;
		}
		return false;
	}

	private static bool TryReadUpdate<TPlatform>(ref TPlatform platform,
		APTR message, out MuiExternalUpdateMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWrapperMessageCodec.TryReadUpdate(ref platform, message,
			out packet);

	private static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiExternalGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWrapperMessageCodec.TryReadGet(ref platform, message,
			out packet);

	private static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiExternalSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWrapperMessageCodec.TryReadSet(ref platform, message, method,
			out packet);

	private static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiExternalMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWrapperMessageCodec.TryReadMethod(ref platform, message,
			method, out packet);

	private static bool IsValidMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWrapperMessageCodec.IsValidMethod(ref platform, message,
			method);

	private static bool TryReadRenderInfo<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiExternalRenderInfoMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWrapperMessageCodec.TryReadRenderInfo(ref platform, message,
			method, out packet);

	private static bool TryReadAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, out MuiExternalAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWrapperMessageCodec.TryReadAskMinMax(ref platform, message,
			out packet);

	private static bool TryReadLayout<TPlatform>(ref TPlatform platform,
		APTR message, out MuiExternalLayoutMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWrapperMessageCodec.TryReadLayout(ref platform, message,
			out packet);
}
