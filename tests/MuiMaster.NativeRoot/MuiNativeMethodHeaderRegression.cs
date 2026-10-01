/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster.NativeRoot;

// Small native regressions for the out-struct field read used by Dispatch.
// Neither entry needs object lifecycle, class registration, or GUI providers.
public static class MuiNativeMethodHeaderRegression
{
	public static uint ScalarControlRoot()
	{
		if (!ReadScalar(out var method)) return 1;
		return method == 0x8042549A ? 42u : 2u;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool ReadScalar(out uint method)
	{
		method = 0x8042549A;
		return true;
	}

	public static uint LocalHeaderRoot()
	{
		if (!ReadLocalHeader(out var packet)) return 1;
		return packet.MethodId == 0x8042549A ? 42u : 2u;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool ReadLocalHeader(out MuiHeadlessMethodMessage packet)
	{
		packet = default;
		packet.MethodId = 0x8042549A;
		return true;
	}

	public static uint GuestHeaderRoot()
	{
		var platform = default(MuiNativeHeadlessPlatform);
		platform.Reset();
		var address = APTR.FromPointer(0x00036240);
		if (!MuiHeadlessMethodHeaderCodec.WriteValue(ref platform, address,
			0x8042549A)) return 1;
		if (!MuiHeadlessMessageCodec.TryReadMethodId(ref platform, address,
			out var packet)) return 2;
		return packet.MethodId == 0x8042549A ? 42u : 3u;
	}
}
