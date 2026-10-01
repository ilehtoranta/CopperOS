/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// MorphOS 3.8 added these two lifetime methods after the classic BOOPSI
// method range. Keep the values named at this ABI boundary; the message sent
// to DoMethodA is still a declaration-ordered guest struct, not an anonymous
// offset packet.
internal static class MuiNativeBoopsiMethodId
{
	public const uint ObjectDummy = 0x100;
	public const uint Retain = ObjectDummy + 11;
	public const uint Release = ObjectDummy + 12;
	public const uint RetainCount = ObjectDummy + 13;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeObjectMethodMessage
{
	internal const uint Size = 4;
	internal uint MethodId;
}

internal static class MuiNativeObjectMethodMessageCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiNativeObjectMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNativeObjectMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}
