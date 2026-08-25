/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaCreateBubbleMessage
{
	internal const uint Size = 20;
	internal uint MethodId;
	internal int X;
	internal int Y;
	internal APTR Text;
	internal uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDeleteBubbleMessage
{
	internal const uint Size = 8;
	internal uint MethodId;
	internal APTR Bubble;
}

internal enum MuiAreaBubblePacketKind : byte
{
	Create,
	Delete,
}

internal enum MuiAreaBubbleMessageField : byte
{
	MethodId,
	X,
	Y,
	Text,
	Flags,
	Bubble,
}

internal static class MuiAreaBubbleMessageCodec
{
	internal const uint CreateBubble = 0x80421C41u;
	internal const uint DeleteBubble = 0x804211AFu;

	private static bool TryReadAddress<TPlatform>(ref TPlatform platform,
		APTR message, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (message.IsNull || message.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	private static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryReadAddress(ref platform, message, offset, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	private static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, uint offset, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadAddress(ref platform, message, offset, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool IsMethod(uint method) => method == CreateBubble ||
		method == DeleteBubble;

	internal static bool TryReadCreate<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaCreateBubbleMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiAreaCreateBubbleMessage.Size) ||
			!TryReadUInt32(ref platform, message, 0, out packet.MethodId) ||
			packet.MethodId != CreateBubble ||
			!TryReadUInt32(ref platform, message, 4, out var x) ||
			!TryReadUInt32(ref platform, message, 8, out var y) ||
			!TryReadUInt32(ref platform, message, 12, out var text) ||
			!TryReadUInt32(ref platform, message, 16, out packet.Flags)) return false;
		packet.X = unchecked((int)x);
		packet.Y = unchecked((int)y);
		packet.Text = APTR.FromPointer(text);
		return true;
	}

	internal static bool TryReadDelete<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDeleteBubbleMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiAreaDeleteBubbleMessage.Size) ||
			!TryReadUInt32(ref platform, message, 0, out packet.MethodId) ||
			packet.MethodId != DeleteBubble ||
			!TryReadUInt32(ref platform, message, 4, out var bubble)) return false;
		packet.Bubble = APTR.FromPointer(bubble);
		return true;
	}

	internal static bool WriteCreate<TPlatform>(ref TPlatform platform,
		APTR message, int x, int y, APTR text, uint flags)
		where TPlatform : struct, IMuiGuestMemory =>
		message.IsNotNull && platform.IsMapped(message,
			MuiAreaCreateBubbleMessage.Size) &&
		TryWriteUInt32(ref platform, message, 0, CreateBubble) &&
		TryWriteUInt32(ref platform, message, 4, unchecked((uint)x)) &&
		TryWriteUInt32(ref platform, message, 8, unchecked((uint)y)) &&
		TryWriteUInt32(ref platform, message, 12, text.Raw) &&
		TryWriteUInt32(ref platform, message, 16, flags);

	internal static bool WriteDelete<TPlatform>(ref TPlatform platform,
		APTR message, APTR bubble) where TPlatform : struct, IMuiGuestMemory =>
		message.IsNotNull && platform.IsMapped(message,
			MuiAreaDeleteBubbleMessage.Size) &&
		TryWriteUInt32(ref platform, message, 0, DeleteBubble) &&
		TryWriteUInt32(ref platform, message, 4, bubble.Raw);
}
