/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessMethodMessage
{
	public const uint Size = 4;
	public uint MethodId;
}

// Shared codec for the fixed method header used by the headless dispatcher
// entry points. Specialized packet codecs remain responsible for their full
// records; this seam only owns the common method-word boundary.
internal static class MuiHeadlessMessageCodec
{
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiHeadlessMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiHeadlessMethodMessage.Size)) return false;
		packet.MethodId = platform.ReadUInt32(message, 0);
		return true;
	}
}
