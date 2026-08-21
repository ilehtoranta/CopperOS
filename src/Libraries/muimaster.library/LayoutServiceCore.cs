/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLayoutMessage
{
	public const uint Size = 24;
	public uint MethodId;
	public uint Left;
	public uint Top;
	public uint Width;
	public uint Height;
	public uint Flags;
}

// The public MUI_Layout() helper used by custom layout hooks.  MorphOS sends
// the six-word MUIP_Layout packet to the target object; this core exposes both
// the scalar ABI-shaped entry point and a packet seam for callers that already
// have a guest-resident message.  All state remains in guest memory and the
// flags are forwarded through the seam even though the current area/layout
// implementations do not yet consume any flag bits.
public static class MuiLayoutServiceCore
{
	public static bool Layout<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int left, int top, int width, int height, uint flags)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		// The MorphOS MUIP_Layout flags are part of the method contract; no
		// current CopperOS layout class defines a flag-dependent branch yet.
		if (obj.IsNull) return false;

		// MUI_Layout is normally called by a custom group hook for one child.
		// Preserve class-specific geometry where the corresponding bounded core
		// already exists, then use the Area fallback for every other object.
		var collection = MuiListCore.Classify(ref platform, state, obj);
		switch (collection)
		{
			case MuiCollectionClass.List:
			case MuiCollectionClass.Floattext:
			case MuiCollectionClass.Dirlist:
			case MuiCollectionClass.Volumelist:
				return MuiListCore.Layout(ref platform, state, obj, left, top, width,
					height);
			case MuiCollectionClass.Listview:
				return MuiListviewCore.Layout(ref platform, state, obj, left, top, width,
					height);
			case MuiCollectionClass.Stringscroll:
				return MuiStringscrollCore.Layout(ref platform, state, obj, left, top,
					width, height);
		}

		var control = MuiCommonControlCore.Classify(ref platform, state, obj);
		if (control == MuiControlClass.Radio)
			return MuiGroupLayoutCore.Layout(ref platform, state, obj, left, top,
				width, height);
		if (control == MuiControlClass.Scrollbar)
			return MuiCommonControlCore.LayoutScrollbar(ref platform, state, obj,
				left, top, width, height);
		return MuiAreaLayoutCore.Layout(ref platform, state, obj, left, top, width,
			height);
	}

	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadLayout(ref platform, message, out var packet)) return 0;
		return Layout(ref platform, state, obj,
			unchecked((int)packet.Left), unchecked((int)packet.Top),
			unchecked((int)packet.Width), unchecked((int)packet.Height),
			packet.Flags) ? 1u : 0u;
	}

	private static bool TryReadLayout<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiLayoutPacketCodec.TryReadLayout(ref platform, message, out packet);
}
