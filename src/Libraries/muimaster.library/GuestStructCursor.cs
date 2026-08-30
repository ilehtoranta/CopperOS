/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// A bounded cursor for fixed guest records.  Production codecs consume a
// named struct in declaration order through this cursor; only this small
// adapter performs the required guest-address arithmetic.  Keeping the
// cursor as a value type avoids managed allocations and exception-based
// bounds handling in the freestanding guest path.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGuestStructCursor
{
	internal APTR Base;
	internal uint Offset;
	internal uint Remaining;

	internal static bool TryCreate<TPlatform>(ref TPlatform platform,
		APTR address, uint byteSize, out MuiGuestStructCursor cursor)
		where TPlatform : struct, IMuiGuestMemory
	{
		cursor = default;
		if (address.IsNull || byteSize == 0 ||
			!platform.IsMapped(address, byteSize)) return false;
		cursor.Base = address;
		cursor.Remaining = byteSize;
		return true;
	}

	internal static bool TryTake<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, uint byteSize, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (byteSize == 0 || cursor.Remaining < byteSize ||
			cursor.Offset > uint.MaxValue - byteSize ||
			cursor.Base.Raw > uint.MaxValue - cursor.Offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + cursor.Offset);
		if (!platform.IsMapped(address, byteSize))
		{
			address = APTR.Null;
			return false;
		}
		cursor.Offset += byteSize;
		cursor.Remaining -= byteSize;
		return true;
	}

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryTake(ref platform, ref cursor, 1, out var address))
			return false;
		value = platform.ReadUInt8(address, 0);
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryTake(ref platform, ref cursor, 2, out var address))
			return false;
		value = platform.ReadUInt16(address, 0);
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryTake(ref platform, ref cursor, 4, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryTake(ref platform, ref cursor, 1, out var address)) return false;
		platform.WriteUInt8(address, 0, value);
		return true;
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryTake(ref platform, ref cursor, 2, out var address)) return false;
		platform.WriteUInt16(address, 0, value);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryTake(ref platform, ref cursor, 4, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool IsComplete(MuiGuestStructCursor cursor) =>
		cursor.Remaining == 0;
}
