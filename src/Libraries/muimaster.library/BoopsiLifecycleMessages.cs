/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// BOOPSI keeps the method header and OM_NEW opSet packet deliberately small.
// Keep both wire shapes as named records so lifecycle code never depends on a
// caller-object offset or a managed overlay of guest memory.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBoopsiMethodMessage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal uint MethodId;
}

internal static class MuiBoopsiMethodMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiBoopsiMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiBoopsiMethodMessage.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiBoopsiMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiBoopsiMethodMessage.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.MethodId) &&
		MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!TryRead(ref platform, address, out var value)) return false;
		methodId = value.MethodId;
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBoopsiOpSetMessage
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal uint MethodId;
	internal APTR Attributes;
	internal APTR GadgetInfo;
}

internal static class MuiBoopsiOpSetMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiBoopsiOpSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiBoopsiOpSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var attributes) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var gadgetInfo) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Attributes = APTR.FromPointer(attributes);
		value.GadgetInfo = APTR.FromPointer(gadgetInfo);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiBoopsiOpSetMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiBoopsiOpSetMessage.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Attributes.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.GadgetInfo.Raw) &&
		MuiGuestStructCursor.IsComplete(cursor);
}
