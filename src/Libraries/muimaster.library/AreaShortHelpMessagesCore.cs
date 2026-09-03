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
		switch (packet)
		{
			case MuiAreaShortHelpPacketKind.Check:
				if (!MuiAreaCheckShortHelpMessageCodec.TryRead(ref platform,
					message, out var check)) return false;
				if (field == MuiAreaShortHelpMessageField.MethodId)
					value = check.MethodId;
				else if (field == MuiAreaShortHelpMessageField.Help)
					value = check.Help.Raw;
				else if (field == MuiAreaShortHelpMessageField.MouseX)
					value = unchecked((uint)check.MouseX);
				else if (field == MuiAreaShortHelpMessageField.MouseY)
					value = unchecked((uint)check.MouseY);
				else return false;
				return true;
			case MuiAreaShortHelpPacketKind.Create:
				if (!MuiAreaCreateShortHelpMessageCodec.TryRead(ref platform,
					message, out var create)) return false;
				if (field == MuiAreaShortHelpMessageField.MethodId)
					value = create.MethodId;
				else if (field == MuiAreaShortHelpMessageField.MouseX)
					value = unchecked((uint)create.MouseX);
				else if (field == MuiAreaShortHelpMessageField.MouseY)
					value = unchecked((uint)create.MouseY);
				else return false;
				return true;
			case MuiAreaShortHelpPacketKind.Delete:
				if (!MuiAreaDeleteShortHelpMessageCodec.TryRead(ref platform,
					message, out var delete)) return false;
				if (field == MuiAreaShortHelpMessageField.MethodId)
					value = delete.MethodId;
				else if (field == MuiAreaShortHelpMessageField.Help)
					value = delete.Help.Raw;
				else return false;
				return true;
		}
		return false;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaShortHelpPacketKind packet,
		MuiAreaShortHelpMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		switch (packet)
		{
			case MuiAreaShortHelpPacketKind.Check:
				if (!MuiAreaCheckShortHelpMessageCodec.TryRead(ref platform,
					message, out var check)) return false;
				if (field == MuiAreaShortHelpMessageField.MethodId)
					check.MethodId = value;
				else if (field == MuiAreaShortHelpMessageField.Help)
					check.Help = APTR.FromPointer(value);
				else if (field == MuiAreaShortHelpMessageField.MouseX)
					check.MouseX = unchecked((int)value);
				else if (field == MuiAreaShortHelpMessageField.MouseY)
					check.MouseY = unchecked((int)value);
				else return false;
				return MuiAreaCheckShortHelpMessageCodec.Write(ref platform,
					message, check);
			case MuiAreaShortHelpPacketKind.Create:
				if (!MuiAreaCreateShortHelpMessageCodec.TryRead(ref platform,
					message, out var create)) return false;
				if (field == MuiAreaShortHelpMessageField.MethodId)
					create.MethodId = value;
				else if (field == MuiAreaShortHelpMessageField.MouseX)
					create.MouseX = unchecked((int)value);
				else if (field == MuiAreaShortHelpMessageField.MouseY)
					create.MouseY = unchecked((int)value);
				else return false;
				return MuiAreaCreateShortHelpMessageCodec.Write(ref platform,
					message, create);
			case MuiAreaShortHelpPacketKind.Delete:
				if (!MuiAreaDeleteShortHelpMessageCodec.TryRead(ref platform,
					message, out var delete)) return false;
				if (field == MuiAreaShortHelpMessageField.MethodId)
					delete.MethodId = value;
				else if (field == MuiAreaShortHelpMessageField.Help)
					delete.Help = APTR.FromPointer(value);
				else return false;
				return MuiAreaDeleteShortHelpMessageCodec.Write(ref platform,
					message, delete);
		}
		return false;
	}

}

// Compatibility adapter retained for callers that still construct the typed
// field cursor. Live ShortHelp packet consumers use the named record adapter.
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

internal static class MuiAreaCheckShortHelpMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaCheckShortHelpMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaCheckShortHelpMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawHelp) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMouseX) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMouseY) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Help = APTR.FromPointer(rawHelp);
		value.MouseX = unchecked((int)rawMouseX);
		value.MouseY = unchecked((int)rawMouseY);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaCheckShortHelpMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaCheckShortHelpMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Help.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.MouseX)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.MouseY))) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiAreaCreateShortHelpMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaCreateShortHelpMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaCreateShortHelpMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMouseX) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMouseY) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.MouseX = unchecked((int)rawMouseX);
		value.MouseY = unchecked((int)rawMouseY);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaCreateShortHelpMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaCreateShortHelpMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.MouseX)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.MouseY))) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiAreaDeleteShortHelpMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaDeleteShortHelpMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDeleteShortHelpMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawHelp) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Help = APTR.FromPointer(rawHelp);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaDeleteShortHelpMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDeleteShortHelpMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Help.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiAreaShortHelpMessageCodec
{
	internal const uint CheckShortHelp = 0x80423C79u;
	internal const uint CreateShortHelp = 0x80428E93u;
	internal const uint DeleteShortHelp = 0x8042D35Au;

	// Selector admission is typed through the complete named packet records.
	// The field adapter remains available for explicit compatibility callers.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaShortHelpPacketKind packet, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		switch (packet)
		{
			case MuiAreaShortHelpPacketKind.Check:
				if (!MuiAreaCheckShortHelpMessageCodec.TryRead(ref platform,
					message, out var check)) return false;
				methodId = check.MethodId;
				return true;
			case MuiAreaShortHelpPacketKind.Create:
				if (!MuiAreaCreateShortHelpMessageCodec.TryRead(ref platform,
					message, out var create)) return false;
				methodId = create.MethodId;
				return true;
			case MuiAreaShortHelpPacketKind.Delete:
				if (!MuiAreaDeleteShortHelpMessageCodec.TryRead(ref platform,
					message, out var delete)) return false;
				methodId = delete.MethodId;
				return true;
			default:
				return false;
		}
	}

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaShortHelpPacketKind packet,
		out MuiAreaShortHelpMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadMethodIdValue(ref platform, message, packet,
			out var methodId)) return false;
		value.MethodId = methodId;
		return true;
	}

	internal static bool TryReadCheck<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaCheckShortHelpMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaCheckShortHelpMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == CheckShortHelp;
	}

	internal static bool WriteCheck<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaCheckShortHelpMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return packet.MethodId == CheckShortHelp &&
			MuiAreaCheckShortHelpMessageCodec.Write(ref platform, message, packet);
	}

	internal static bool WriteCheck<TPlatform>(ref TPlatform platform,
		APTR message, APTR help, int mouseX, int mouseY)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaCheckShortHelpMessage);
		packet.MethodId = CheckShortHelp;
		packet.Help = help;
		packet.MouseX = mouseX;
		packet.MouseY = mouseY;
		return WriteCheck(ref platform, message, packet);
	}

	internal static bool WriteCreate<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaCreateShortHelpMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return packet.MethodId == CreateShortHelp &&
			MuiAreaCreateShortHelpMessageCodec.Write(ref platform, message, packet);
	}

	internal static bool WriteCreate<TPlatform>(ref TPlatform platform,
		APTR message, int mouseX, int mouseY)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaCreateShortHelpMessage);
		packet.MethodId = CreateShortHelp;
		packet.MouseX = mouseX;
		packet.MouseY = mouseY;
		return WriteCreate(ref platform, message, packet);
	}

	internal static bool TryReadCreate<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaCreateShortHelpMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaCreateShortHelpMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == CreateShortHelp;
	}

	internal static bool TryReadDelete<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDeleteShortHelpMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDeleteShortHelpMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == DeleteShortHelp;
	}

	internal static bool WriteDelete<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaDeleteShortHelpMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return packet.MethodId == DeleteShortHelp &&
			MuiAreaDeleteShortHelpMessageCodec.Write(ref platform, message, packet);
	}

	internal static bool WriteDelete<TPlatform>(ref TPlatform platform,
		APTR message, APTR help)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaDeleteShortHelpMessage);
		packet.MethodId = DeleteShortHelp;
		packet.Help = help;
		return WriteDelete(ref platform, message, packet);
	}
}
