/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Fixed MorphOS ShortHelp packets. The packet structs are the consumer-facing
// shapes; only the named cursor codec knows their guest field boundaries.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaCreateShortHelpMessage
{
	internal const uint Size = 12;
	internal uint MethodId;
	internal int MouseX;
	internal int MouseY;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDeleteShortHelpMessage
{
	internal const uint Size = 8;
	internal uint MethodId;
	internal APTR Help;
}

internal enum MuiAreaShortHelpPacketKind : byte
{
	Create,
	Delete,
}

internal enum MuiAreaShortHelpMessageField : byte
{
	MethodId,
	MouseX,
	MouseY,
	Help,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaShortHelpMessageFieldCursor
{
	internal APTR Message;
	internal MuiAreaShortHelpPacketKind Packet;
	internal MuiAreaShortHelpMessageField Field;
}

internal static class MuiAreaShortHelpMessageFieldCursorCodec
{
	private static bool TryResolve(MuiAreaShortHelpPacketKind packet,
		MuiAreaShortHelpMessageField field, out uint offset)
	{
		switch (packet)
		{
			case MuiAreaShortHelpPacketKind.Create:
				if (field == MuiAreaShortHelpMessageField.MethodId) { offset = 0; return true; }
				if (field == MuiAreaShortHelpMessageField.MouseX) { offset = 4; return true; }
				if (field == MuiAreaShortHelpMessageField.MouseY) { offset = 8; return true; }
				break;
			case MuiAreaShortHelpPacketKind.Delete:
				if (field == MuiAreaShortHelpMessageField.MethodId) { offset = 0; return true; }
				if (field == MuiAreaShortHelpMessageField.Help) { offset = 4; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaShortHelpMessageFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Packet, cursor.Field, out var offset) ||
			cursor.Message.IsNull || cursor.Message.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaShortHelpPacketKind packet,
		MuiAreaShortHelpMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaShortHelpMessageFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}
}

internal static class MuiAreaShortHelpMessageCodec
{
	internal const uint CheckShortHelp = 0x80423C79u;
	internal const uint CreateShortHelp = 0x80428E93u;
	internal const uint DeleteShortHelp = 0x8042D35Au;

	internal static bool TryReadCreate<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaCreateShortHelpMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiAreaCreateShortHelpMessage.Size) ||
			!MuiAreaShortHelpMessageFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiAreaShortHelpPacketKind.Create,
				MuiAreaShortHelpMessageField.MethodId, out packet.MethodId) ||
			packet.MethodId != CreateShortHelp ||
			!MuiAreaShortHelpMessageFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiAreaShortHelpPacketKind.Create,
				MuiAreaShortHelpMessageField.MouseX, out var mouseX) ||
			!MuiAreaShortHelpMessageFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiAreaShortHelpPacketKind.Create,
				MuiAreaShortHelpMessageField.MouseY, out var mouseY)) return false;
		packet.MouseX = unchecked((int)mouseX);
		packet.MouseY = unchecked((int)mouseY);
		return true;
	}

	internal static bool TryReadDelete<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDeleteShortHelpMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiAreaDeleteShortHelpMessage.Size) ||
			!MuiAreaShortHelpMessageFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiAreaShortHelpPacketKind.Delete,
				MuiAreaShortHelpMessageField.MethodId, out packet.MethodId) ||
			packet.MethodId != DeleteShortHelp ||
			!MuiAreaShortHelpMessageFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiAreaShortHelpPacketKind.Delete,
				MuiAreaShortHelpMessageField.Help, out var help)) return false;
		packet.Help = APTR.FromPointer(help);
		return true;
	}
}
