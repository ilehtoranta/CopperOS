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
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint XOffset = 4;
	internal const uint YOffset = 8;
	internal const uint TextOffset = 12;
	internal const uint FlagsOffset = 16;
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
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint BubbleOffset = 4;
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

// Struct-first guest-memory adapter for the fixed CreateBubble/DeleteBubble
// packets. The packet kind owns the complete record boundary; callers use the
// named field enum and never supply a literal ABI offset.
internal static class MuiAreaBubbleMessageMemoryCodec
{
	private static bool TryResolve(MuiAreaBubblePacketKind packet,
		MuiAreaBubbleMessageField field, out uint offset, out uint size)
	{
		switch (packet)
		{
			case MuiAreaBubblePacketKind.Create:
				size = MuiAreaCreateBubbleMessage.Size;
				if (field == MuiAreaBubbleMessageField.MethodId)
					offset = MuiAreaCreateBubbleMessage.MethodIdOffset;
				else if (field == MuiAreaBubbleMessageField.X)
					offset = MuiAreaCreateBubbleMessage.XOffset;
				else if (field == MuiAreaBubbleMessageField.Y)
					offset = MuiAreaCreateBubbleMessage.YOffset;
				else if (field == MuiAreaBubbleMessageField.Text)
					offset = MuiAreaCreateBubbleMessage.TextOffset;
				else if (field == MuiAreaBubbleMessageField.Flags)
					offset = MuiAreaCreateBubbleMessage.FlagsOffset;
				else
				{
					offset = 0;
					size = 0;
					return false;
				}
				return true;
			case MuiAreaBubblePacketKind.Delete:
				size = MuiAreaDeleteBubbleMessage.Size;
				if (field == MuiAreaBubbleMessageField.MethodId)
					offset = MuiAreaDeleteBubbleMessage.MethodIdOffset;
				else if (field == MuiAreaBubbleMessageField.Bubble)
					offset = MuiAreaDeleteBubbleMessage.BubbleOffset;
				else
				{
					offset = 0;
					size = 0;
					return false;
				}
				return true;
		}
		offset = 0;
		size = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaBubblePacketKind packet,
		MuiAreaBubbleMessageField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset, out var size) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, size)) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiAreaCreateBubbleMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaBubblePacketKind packet,
		MuiAreaBubbleMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaBubblePacketKind packet,
		MuiAreaBubbleMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaBubbleMessageCodec
{
	internal const uint CreateBubble = 0x80421C41u;
	internal const uint DeleteBubble = 0x804211AFu;

	internal static bool IsMethod(uint method) => method == CreateBubble ||
		method == DeleteBubble;

	internal static bool TryReadCreate<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaCreateBubbleMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiAreaBubbleMessageMemoryCodec.TryReadUInt32(ref platform,
			message, MuiAreaBubblePacketKind.Create,
			MuiAreaBubbleMessageField.MethodId, out packet.MethodId) ||
			packet.MethodId != CreateBubble ||
			!MuiAreaBubbleMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiAreaBubblePacketKind.Create,
				MuiAreaBubbleMessageField.X, out var x) ||
			!MuiAreaBubbleMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiAreaBubblePacketKind.Create,
				MuiAreaBubbleMessageField.Y, out var y) ||
			!MuiAreaBubbleMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiAreaBubblePacketKind.Create,
				MuiAreaBubbleMessageField.Text, out var text) ||
			!MuiAreaBubbleMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiAreaBubblePacketKind.Create,
				MuiAreaBubbleMessageField.Flags, out packet.Flags)) return false;
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
		if (!MuiAreaBubbleMessageMemoryCodec.TryReadUInt32(ref platform,
			message, MuiAreaBubblePacketKind.Delete,
			MuiAreaBubbleMessageField.MethodId, out packet.MethodId) ||
			packet.MethodId != DeleteBubble ||
			!MuiAreaBubbleMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiAreaBubblePacketKind.Delete,
				MuiAreaBubbleMessageField.Bubble, out var bubble)) return false;
		packet.Bubble = APTR.FromPointer(bubble);
		return true;
	}

	internal static bool WriteCreate<TPlatform>(ref TPlatform platform,
		APTR message, int x, int y, APTR text, uint flags)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaBubbleMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiAreaBubblePacketKind.Create, MuiAreaBubbleMessageField.MethodId,
			CreateBubble) &&
		MuiAreaBubbleMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiAreaBubblePacketKind.Create, MuiAreaBubbleMessageField.X,
			unchecked((uint)x)) &&
		MuiAreaBubbleMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiAreaBubblePacketKind.Create, MuiAreaBubbleMessageField.Y,
			unchecked((uint)y)) &&
		MuiAreaBubbleMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiAreaBubblePacketKind.Create, MuiAreaBubbleMessageField.Text,
			text.Raw) &&
		MuiAreaBubbleMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiAreaBubblePacketKind.Create, MuiAreaBubbleMessageField.Flags, flags);

	internal static bool WriteDelete<TPlatform>(ref TPlatform platform,
		APTR message, APTR bubble) where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaBubbleMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiAreaBubblePacketKind.Delete, MuiAreaBubbleMessageField.MethodId,
			DeleteBubble) &&
		MuiAreaBubbleMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiAreaBubblePacketKind.Delete, MuiAreaBubbleMessageField.Bubble,
			bubble.Raw);
}
