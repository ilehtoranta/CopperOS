/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessMethodMessage
{
	public const uint Size = 4;
	public uint MethodId;
}

// BOOPSI OM_SET carries a standard TagItem list and an optional GadgetInfo
// pointer. Keep that fixed packet named at the MUI boundary; only this codec
// knows the packed guest positions of the three ULONG fields.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessOmSetMessage
{
	internal const uint Size = 12;
	internal uint MethodId;
	internal APTR Attributes;
	internal APTR GadgetInfo;
}

internal static class MuiHeadlessOmSetMessageCodec
{
	internal const uint Method = 0x00000103u;

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiHeadlessOmSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiHeadlessOmSetMessage.Size) ||
			platform.ReadUInt32(address, 0) != Method) return false;
		value.MethodId = Method;
		value.Attributes = APTR.FromPointer(platform.ReadUInt32(address, 4));
		value.GadgetInfo = APTR.FromPointer(platform.ReadUInt32(address, 8));
		return true;
	}
}

// BOOPSI OM_UPDATE carries the same caller-owned TagItem list as OM_SET, plus
// the documented update flags. Keep the four guest ULONG fields in a named
// record so the dispatcher never treats opUpdate as an ad-hoc offset tuple.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiHeadlessOmUpdateMessage
{
	internal const uint Size = 16;
	internal uint MethodId;
	internal APTR Attributes;
	internal APTR GadgetInfo;
	internal uint Flags;
}

internal static class MuiHeadlessOmUpdateMessageCodec
{
	internal const uint Method = 0x00000108u;

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiHeadlessOmUpdateMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiHeadlessOmUpdateMessage.Size) ||
			platform.ReadUInt32(address, 0) != Method) return false;
		value.MethodId = Method;
		value.Attributes = APTR.FromPointer(platform.ReadUInt32(address, 4));
		value.GadgetInfo = APTR.FromPointer(platform.ReadUInt32(address, 8));
		value.Flags = platform.ReadUInt32(address, 12);
		return true;
	}
}

// Shared codec for the fixed method header used by the headless dispatcher
// entry points. Specialized packet codecs remain responsible for their full
// records; this seam only owns the common method-word boundary.
internal static class MuiHeadlessMessageCodec
{
	// Keep scalar selector admission at the guest ABI boundary; dispatcher
	// consumers continue to receive the named fixed-width header record.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (message.IsNull || !platform.IsMapped(message,
			MuiHeadlessMethodMessage.Size)) return false;
		methodId = platform.ReadUInt32(message, 0);
		return true;
	}

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiHeadlessMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryReadMethodIdValue(ref platform, message, out packet.MethodId);
	}
}
