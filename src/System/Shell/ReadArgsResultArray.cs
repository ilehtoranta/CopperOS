/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.Shell;

// A dynamic ReadArgs template creates one ULONG slot per template entry. This
// view keeps the slot count and guest range attached to the typed array.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct ShellReadArgsResultArray
{
	internal APTR Base;
	internal uint SlotCount;
}

internal static class ShellReadArgsResultArrayCodec
{
	internal static bool TryCreate<TPlatform>(ref TPlatform platform,
		APTR address, uint byteLength, out ShellReadArgsResultArray results)
		where TPlatform : struct, IShellPlatform
	{
		results = default;
		if (address.IsNull || byteLength < sizeof(uint) ||
			byteLength % sizeof(uint) != 0 ||
			address.Raw > uint.MaxValue - byteLength ||
			!platform.IsMapped(address, byteLength)) return false;
		results.Base = address;
		results.SlotCount = byteLength / sizeof(uint);
		return true;
	}

	internal static bool TryReadPointer<TPlatform>(ref TPlatform platform,
		in ShellReadArgsResultArray results, uint index, out APTR value)
		where TPlatform : struct, IShellPlatform
	{
		value = APTR.Null;
		if (results.Base.IsNull || index >= results.SlotCount ||
			index > uint.MaxValue / sizeof(uint)) return false;
		var offset = index * sizeof(uint);
		if (results.Base.Raw > uint.MaxValue - offset - sizeof(uint))
			return false;
		var slot = APTR.FromPointer(results.Base.Raw + offset);
		value = APTR.FromPointer(platform.ReadUInt32(slot));
		return true;
	}
}
