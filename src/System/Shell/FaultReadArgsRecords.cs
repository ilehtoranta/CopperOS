/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.Shell;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct FaultReadArgsCodeCursor
{
	internal APTR Base;
	internal uint Index;
	internal uint MaximumCodes;
}

internal static class FaultReadArgsCodeCursorCodec
{
	internal static bool TryReadCurrent<TPlatform>(ref TPlatform platform,
		ref FaultReadArgsCodeCursor cursor, out APTR numberAddress,
		out bool hasValue)
		where TPlatform : struct, IShellPlatform
	{
		numberAddress = APTR.Null;
		hasValue = false;
		if (cursor.Base.IsNull || cursor.Index > cursor.MaximumCodes ||
			cursor.Index >= uint.MaxValue / sizeof(uint)) return false;
		var offset = cursor.Index * sizeof(uint);
		var byteLength = offset + sizeof(uint);
		if (cursor.Base.Raw > uint.MaxValue - byteLength ||
			!platform.IsMapped(cursor.Base, byteLength)) return false;
		numberAddress = APTR.FromPointer(platform.ReadUInt32(cursor.Base,
			unchecked((int)offset)));
		hasValue = numberAddress.IsNotNull;
		return true;
	}

	internal static bool TryAdvance(ref FaultReadArgsCodeCursor cursor)
	{
		if (cursor.Index >= cursor.MaximumCodes) return false;
		cursor.Index++;
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct FaultErrorCodeBuffer
{
	internal APTR Base;
	internal uint CapacityBytes;
	internal uint Count;
}

internal static class FaultErrorCodeBufferCodec
{
	internal static bool TryCreate<TPlatform>(ref TPlatform platform,
		APTR address, uint capacityBytes, out FaultErrorCodeBuffer buffer)
		where TPlatform : struct, IShellPlatform
	{
		buffer = default;
		if (address.IsNull || capacityBytes < sizeof(uint) ||
			address.Raw > uint.MaxValue - capacityBytes ||
			!platform.IsMapped(address, capacityBytes)) return false;
		buffer.Base = address;
		buffer.CapacityBytes = capacityBytes;
		return true;
	}

	internal static bool TryAppend<TPlatform>(ref TPlatform platform,
		ref FaultErrorCodeBuffer buffer, uint value)
		where TPlatform : struct, IShellPlatform
	{
		if (buffer.Base.IsNull || buffer.Count >=
			buffer.CapacityBytes / sizeof(uint) ||
			buffer.Count >= uint.MaxValue / sizeof(uint)) return false;
		var offset = buffer.Count * sizeof(uint);
		platform.WriteUInt32(buffer.Base, unchecked((int)offset), value);
		buffer.Count++;
		return true;
	}
}
