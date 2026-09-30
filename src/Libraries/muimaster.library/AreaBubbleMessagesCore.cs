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
	// Compatibility aliases for existing white-box callers. Production field
	// admission uses the named cursor below rather than these wire constants.
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
	// Compatibility aliases for existing white-box callers. Production field
	// admission uses the named cursor below rather than these wire constants.
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

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaBubbleMessageFieldCursor
{
	internal APTR Message;
	internal MuiAreaBubblePacketKind Packet;
	internal MuiAreaBubbleMessageField Field;
}

internal static class MuiAreaBubbleMessageFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaBubbleMessageFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaBubbleMessageFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaBubbleMessageMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaBubblePacketKind packet,
		MuiAreaBubbleMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaBubbleMessageMemoryCodec.TryReadUInt32(ref platform, message,
			packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaBubblePacketKind packet,
		MuiAreaBubbleMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaBubbleMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			packet, field, value);
}

// Struct-first guest-memory adapter for the fixed CreateBubble/DeleteBubble
// packets. The packet kind owns the complete record boundary; callers use the
// named field enum and never supply a literal ABI offset.
internal static class MuiAreaBubbleMessageMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiAreaBubblePacketKind packet,
		MuiAreaBubbleMessageField field, out uint index, out uint recordSize)
	{
		if (packet == MuiAreaBubblePacketKind.Create &&
			field == MuiAreaBubbleMessageField.MethodId)
		{
			index = 0;
			recordSize = MuiAreaCreateBubbleMessage.Size;
			return true;
		}
		if (packet == MuiAreaBubblePacketKind.Create &&
			field == MuiAreaBubbleMessageField.X)
		{
			index = 1;
			recordSize = MuiAreaCreateBubbleMessage.Size;
			return true;
		}
		if (packet == MuiAreaBubblePacketKind.Create &&
			field == MuiAreaBubbleMessageField.Y)
		{
			index = 2;
			recordSize = MuiAreaCreateBubbleMessage.Size;
			return true;
		}
		if (packet == MuiAreaBubblePacketKind.Create &&
			field == MuiAreaBubbleMessageField.Text)
		{
			index = 3;
			recordSize = MuiAreaCreateBubbleMessage.Size;
			return true;
		}
		if (packet == MuiAreaBubblePacketKind.Create &&
			field == MuiAreaBubbleMessageField.Flags)
		{
			index = 4;
			recordSize = MuiAreaCreateBubbleMessage.Size;
			return true;
		}
		if (packet == MuiAreaBubblePacketKind.Delete &&
			field == MuiAreaBubbleMessageField.MethodId)
		{
			index = 0;
			recordSize = MuiAreaDeleteBubbleMessage.Size;
			return true;
		}
		if (packet == MuiAreaBubblePacketKind.Delete &&
			field == MuiAreaBubbleMessageField.Bubble)
		{
			index = 1;
			recordSize = MuiAreaDeleteBubbleMessage.Size;
			return true;
		}
		index = uint.MaxValue;
		recordSize = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaBubblePacketKind packet,
		MuiAreaBubbleMessageField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaBubbleMessageFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaBubbleMessageFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Packet, cursor.Field, out var index,
			out var recordSize) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Message, recordSize,
				out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiAreaCreateBubbleMessage.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiAreaCreateBubbleMessage.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaBubblePacketKind packet,
		MuiAreaBubbleMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (packet == MuiAreaBubblePacketKind.Create)
		{
			if (!MuiAreaCreateBubbleMessageCodec.TryReadStructural(ref platform,
				message, out var create)) return false;
			if (field == MuiAreaBubbleMessageField.MethodId)
				value = create.MethodId;
			else if (field == MuiAreaBubbleMessageField.X)
				value = unchecked((uint)create.X);
			else if (field == MuiAreaBubbleMessageField.Y)
				value = unchecked((uint)create.Y);
			else if (field == MuiAreaBubbleMessageField.Text)
				value = create.Text.Raw;
			else if (field == MuiAreaBubbleMessageField.Flags)
				value = create.Flags;
			else
				return false;
			return true;
		}
		if (packet == MuiAreaBubblePacketKind.Delete)
		{
			if (!MuiAreaDeleteBubbleMessageCodec.TryReadStructural(ref platform,
				message, out var delete)) return false;
			if (field == MuiAreaBubbleMessageField.MethodId)
				value = delete.MethodId;
			else if (field == MuiAreaBubbleMessageField.Bubble)
				value = delete.Bubble.Raw;
			else
				return false;
			return true;
		}
		return false;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaBubblePacketKind packet,
		MuiAreaBubbleMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (packet == MuiAreaBubblePacketKind.Create)
		{
			if (!MuiAreaCreateBubbleMessageCodec.TryReadStructural(ref platform,
				message, out var create)) return false;
			if (field == MuiAreaBubbleMessageField.MethodId)
				create.MethodId = value;
			else if (field == MuiAreaBubbleMessageField.X)
				create.X = unchecked((int)value);
			else if (field == MuiAreaBubbleMessageField.Y)
				create.Y = unchecked((int)value);
			else if (field == MuiAreaBubbleMessageField.Text)
				create.Text = APTR.FromPointer(value);
			else if (field == MuiAreaBubbleMessageField.Flags)
				create.Flags = value;
			else
				return false;
			return MuiAreaCreateBubbleMessageCodec.WriteStructural(ref platform,
				message, create);
		}
		if (packet == MuiAreaBubblePacketKind.Delete)
		{
			if (!MuiAreaDeleteBubbleMessageCodec.TryReadStructural(ref platform,
				message, out var delete)) return false;
			if (field == MuiAreaBubbleMessageField.MethodId)
				delete.MethodId = value;
			else if (field == MuiAreaBubbleMessageField.Bubble)
				delete.Bubble = APTR.FromPointer(value);
			else
				return false;
			return MuiAreaDeleteBubbleMessageCodec.WriteStructural(ref platform,
				message, delete);
		}
		return false;
	}
}

internal static class MuiAreaCreateBubbleMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaCreateBubbleMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaCreateBubbleMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawX) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawY) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawText) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.X = unchecked((int)rawX);
		value.Y = unchecked((int)rawY);
		value.Text = APTR.FromPointer(rawText);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaCreateBubbleMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaCreateBubbleMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.X)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.Y)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Text.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaCreateBubbleMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryRead(ref platform, address, out value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaCreateBubbleMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		Write(ref platform, address, value);
}

internal static class MuiAreaDeleteBubbleMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaDeleteBubbleMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDeleteBubbleMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawBubble) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Bubble = APTR.FromPointer(rawBubble);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaDeleteBubbleMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDeleteBubbleMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Bubble.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaDeleteBubbleMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryRead(ref platform, address, out value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaDeleteBubbleMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		Write(ref platform, address, value);
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
		if (!MuiAreaCreateBubbleMessageCodec.TryRead(ref platform, message,
			out packet) || packet.MethodId != CreateBubble)
		{
			packet = default;
			return false;
		}
		return true;
	}

	internal static bool TryReadDelete<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDeleteBubbleMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaDeleteBubbleMessageCodec.TryRead(ref platform, message,
			out packet) || packet.MethodId != DeleteBubble)
		{
			packet = default;
			return false;
		}
		return true;
	}

	internal static bool WriteCreate<TPlatform>(ref TPlatform platform,
		APTR message, int x, int y, APTR text, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaCreateBubbleMessage);
		packet.MethodId = CreateBubble;
		packet.X = x;
		packet.Y = y;
		packet.Text = text;
		packet.Flags = flags;
		return MuiAreaCreateBubbleMessageCodec.Write(ref platform, message,
			packet);
	}

	internal static bool WriteDelete<TPlatform>(ref TPlatform platform,
		APTR message, APTR bubble) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaDeleteBubbleMessage);
		packet.MethodId = DeleteBubble;
		packet.Bubble = bubble;
		return MuiAreaDeleteBubbleMessageCodec.Write(ref platform, message,
			packet);
	}
}
