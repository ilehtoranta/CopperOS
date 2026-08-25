/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Native provider boundary for the MorphOS Area bubble methods.  The core
// keeps only named value fields; the provider owns the platform bubble handle
// and any native text/window resources.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiBubbleCreateSample
{
	public APTR Object;
	public int X;
	public int Y;
	public APTR Text;
	public uint Flags;
	public APTR Result;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiBubbleDeleteSample
{
	public APTR Object;
	public APTR Bubble;
}

internal static class MuiAreaBubbleCore
{
	internal static APTR Create<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int x, int y, APTR text, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull ||
			text.IsNull || !CStringCodec.TryReadLength(ref platform, text, 4096,
				out _)) return APTR.Null;
		var sample = default(MuiBubbleCreateSample);
		sample.Object = obj;
		sample.X = x;
		sample.Y = y;
		sample.Text = text;
		sample.Flags = flags;
		if (!platform.CreateMuiBubble(ref sample)) return APTR.Null;
		if (sample.Object != obj || sample.X != x || sample.Y != y ||
			sample.Text != text || sample.Flags != flags) return APTR.Null;
		return sample.Result;
	}

	internal static bool Delete<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR bubble) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull ||
			bubble.IsNull) return false;
		var sample = default(MuiBubbleDeleteSample);
		sample.Object = obj;
		sample.Bubble = bubble;
		if (!platform.DeleteMuiBubble(ref sample)) return false;
		return sample.Object == obj && sample.Bubble == bubble;
	}
}

public static class MuiAreaBubblePacketCore
{
	public static APTR Create<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int x, int y, APTR text, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaBubbleCore.Create(ref platform, state, obj, x, y, text, flags);

	public static bool Delete<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR bubble) where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaBubbleCore.Delete(ref platform, state, obj, bubble);
}
