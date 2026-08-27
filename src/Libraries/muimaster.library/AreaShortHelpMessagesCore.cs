/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Fixed MorphOS ShortHelp packets. The packet structs are the consumer-facing
// shapes; only the named cursor codec knows their guest field boundaries.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaCreateShortHelpMessage
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint MouseXOffset = 4;
	internal const uint MouseYOffset = 8;
	internal uint MethodId;
	internal int MouseX;
	internal int MouseY;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDeleteShortHelpMessage
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint HelpOffset = 4;
	internal uint MethodId;
	internal APTR Help;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaCheckShortHelpMessage
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint HelpOffset = 4;
	internal const uint MouseXOffset = 8;
	internal const uint MouseYOffset = 12;
	internal uint MethodId;
	internal APTR Help;
	internal int MouseX;
	internal int MouseY;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaShortHelpMethodMessage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal uint MethodId;
}

internal enum MuiAreaShortHelpPacketKind : byte
{
	Check,
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

// The named record codecs own packet sizes and member boundaries. Callers
// select semantic packet/field enum values instead of passing raw offsets.
internal static class MuiAreaShortHelpMessageMemoryCodec
{
	private static bool TryGetPacketSize(MuiAreaShortHelpPacketKind packet,
		out uint size)
	{
		switch (packet)
		{
			case MuiAreaShortHelpPacketKind.Check:
				size = MuiAreaCheckShortHelpMessage.Size;
				return true;
			case MuiAreaShortHelpPacketKind.Create:
				size = MuiAreaCreateShortHelpMessage.Size;
				return true;
			case MuiAreaShortHelpPacketKind.Delete:
				size = MuiAreaDeleteShortHelpMessage.Size;
				return true;
		}
		size = 0;
		return false;
	}

	private static bool TryResolve(MuiAreaShortHelpPacketKind packet,
		MuiAreaShortHelpMessageField field, out uint offset)
	{
		switch (packet)
		{
			case MuiAreaShortHelpPacketKind.Check:
				if (field == MuiAreaShortHelpMessageField.MethodId)
				{
					offset = MuiAreaCheckShortHelpMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiAreaShortHelpMessageField.Help)
				{
					offset = MuiAreaCheckShortHelpMessage.HelpOffset;
					return true;
				}
				if (field == MuiAreaShortHelpMessageField.MouseX)
				{
					offset = MuiAreaCheckShortHelpMessage.MouseXOffset;
					return true;
				}
				if (field == MuiAreaShortHelpMessageField.MouseY)
				{
					offset = MuiAreaCheckShortHelpMessage.MouseYOffset;
					return true;
				}
				break;
			case MuiAreaShortHelpPacketKind.Create:
				if (field == MuiAreaShortHelpMessageField.MethodId)
				{
					offset = MuiAreaCreateShortHelpMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiAreaShortHelpMessageField.MouseX)
				{
					offset = MuiAreaCreateShortHelpMessage.MouseXOffset;
					return true;
				}
				if (field == MuiAreaShortHelpMessageField.MouseY)
				{
					offset = MuiAreaCreateShortHelpMessage.MouseYOffset;
					return true;
				}
				break;
			case MuiAreaShortHelpPacketKind.Delete:
				if (field == MuiAreaShortHelpMessageField.MethodId)
				{
					offset = MuiAreaDeleteShortHelpMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiAreaShortHelpMessageField.Help)
				{
					offset = MuiAreaDeleteShortHelpMessage.HelpOffset;
					return true;
				}
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
			!TryGetPacketSize(cursor.Packet, out var packetSize) ||
			cursor.Message.IsNull || cursor.Message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Message, packetSize))
			return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, MuiAreaShortHelpMethodMessage.FieldSize);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaShortHelpPacketKind packet,
		MuiAreaShortHelpMessageField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaShortHelpMessageFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address);
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

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaShortHelpPacketKind packet,
		MuiAreaShortHelpMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaShortHelpMessageFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

}

internal static class MuiAreaShortHelpMessageFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaShortHelpMessageFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaShortHelpMessageMemoryCodec.TryGetAddress(ref platform, cursor,
			out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaShortHelpPacketKind packet,
		MuiAreaShortHelpMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaShortHelpMessageMemoryCodec.TryReadUInt32(ref platform, message,
			packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaShortHelpPacketKind packet,
		MuiAreaShortHelpMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaShortHelpMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			packet, field, value);
}

internal static class MuiAreaShortHelpMessageCodec
{
	internal const uint CheckShortHelp = 0x80423C79u;
	internal const uint CreateShortHelp = 0x80428E93u;
	internal const uint DeleteShortHelp = 0x8042D35Au;

	private static uint PacketSize(MuiAreaShortHelpPacketKind packet)
	{
		return packet switch
		{
			MuiAreaShortHelpPacketKind.Check => MuiAreaCheckShortHelpMessage.Size,
			MuiAreaShortHelpPacketKind.Create => MuiAreaCreateShortHelpMessage.Size,
			MuiAreaShortHelpPacketKind.Delete => MuiAreaDeleteShortHelpMessage.Size,
			_ => 0u,
		};
	}

	// Keep the selector scalar at the fixed guest ABI boundary. Consumers still
	// receive the named method record or complete packet structs below.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaShortHelpPacketKind packet, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		var size = PacketSize(packet);
		if (size == 0 || message.IsNull || !platform.IsMapped(message, size))
			return false;
		return MuiAreaShortHelpMessageFieldCursorCodec.TryReadUInt32(ref platform,
			message, packet, MuiAreaShortHelpMessageField.MethodId, out methodId);
	}

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaShortHelpPacketKind packet,
		out MuiAreaShortHelpMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadMethodIdValue(ref platform, message, packet,
			out value.MethodId)) return false;
		return true;
	}

	internal static bool TryReadCheck<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaCheckShortHelpMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadMethodIdValue(ref platform, message,
			MuiAreaShortHelpPacketKind.Check, out packet.MethodId) ||
			packet.MethodId != CheckShortHelp ||
			!MuiAreaShortHelpMessageFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiAreaShortHelpPacketKind.Check,
				MuiAreaShortHelpMessageField.Help, out var help) ||
			!MuiAreaShortHelpMessageFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiAreaShortHelpPacketKind.Check,
				MuiAreaShortHelpMessageField.MouseX, out var mouseX) ||
			!MuiAreaShortHelpMessageFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiAreaShortHelpPacketKind.Check,
				MuiAreaShortHelpMessageField.MouseY, out var mouseY)) return false;
		packet.Help = APTR.FromPointer(help);
		packet.MouseX = unchecked((int)mouseX);
		packet.MouseY = unchecked((int)mouseY);
		return true;
	}

	internal static bool WriteCheck<TPlatform>(ref TPlatform platform,
		APTR message, APTR help, int mouseX, int mouseY)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiAreaCheckShortHelpMessage.Size)) return false;
		return MuiAreaShortHelpMessageFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiAreaShortHelpPacketKind.Check,
			MuiAreaShortHelpMessageField.MethodId, CheckShortHelp) &&
			MuiAreaShortHelpMessageFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiAreaShortHelpPacketKind.Check,
				MuiAreaShortHelpMessageField.Help, help.Raw) &&
			MuiAreaShortHelpMessageFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiAreaShortHelpPacketKind.Check,
				MuiAreaShortHelpMessageField.MouseX, unchecked((uint)mouseX)) &&
			MuiAreaShortHelpMessageFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiAreaShortHelpPacketKind.Check,
				MuiAreaShortHelpMessageField.MouseY, unchecked((uint)mouseY));
	}

	internal static bool TryReadCreate<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaCreateShortHelpMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadMethodIdValue(ref platform, message,
			MuiAreaShortHelpPacketKind.Create, out packet.MethodId) ||
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
		if (!TryReadMethodIdValue(ref platform, message,
			MuiAreaShortHelpPacketKind.Delete, out packet.MethodId) ||
			packet.MethodId != DeleteShortHelp ||
			!MuiAreaShortHelpMessageFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiAreaShortHelpPacketKind.Delete,
				MuiAreaShortHelpMessageField.Help, out var help)) return false;
		packet.Help = APTR.FromPointer(help);
		return true;
	}
}
