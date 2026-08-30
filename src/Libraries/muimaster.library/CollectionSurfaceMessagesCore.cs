/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Central codec for the fixed collection surface packets shared by List,
// Listview, Stringscroll, and Floattext. Consumers use named records; this
// adapter alone owns the packed MorphOS guest-memory offsets.
internal enum MuiCollectionSurfacePacketKind : byte
{
	Layout,
	AskMinMax,
	Draw,
	HandleInput,
	Attribute,
}

internal enum MuiCollectionSurfaceField : byte
{
	MethodId,
	Left,
	Top,
	Width,
	Height,
	Storage,
	Flags,
	IntuiMessage,
	MuiKey,
	Attribute,
	Value,
}

[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionSurfaceFieldCursor
{
	internal APTR Message;
	internal MuiCollectionSurfacePacketKind Packet;
	internal MuiCollectionSurfaceField Field;
}

// Named packet adapters keep collection surface payloads as semantic structs.
// Only this memory layer translates their fixed guest boundaries.
internal static class MuiCollectionSurfaceMessageMemoryCodec
{
	private static bool TryGetPacketSize(MuiCollectionSurfacePacketKind packet,
		out uint size)
	{
		switch (packet)
		{
			case MuiCollectionSurfacePacketKind.Layout:
				size = MuiCollectionLayoutMessage.Size;
				return true;
			case MuiCollectionSurfacePacketKind.AskMinMax:
				size = MuiCollectionAskMinMaxMessage.Size;
				return true;
			case MuiCollectionSurfacePacketKind.Draw:
				size = MuiCollectionDrawMessage.Size;
				return true;
			case MuiCollectionSurfacePacketKind.HandleInput:
				size = MuiCollectionHandleInputMessage.Size;
				return true;
			case MuiCollectionSurfacePacketKind.Attribute:
				size = MuiCollectionAttributeMessage.Size;
				return true;
		}
		size = 0;
		return false;
	}

	private static bool TryResolve(MuiCollectionSurfacePacketKind packet,
		MuiCollectionSurfaceField field, out uint offset)
	{
		switch (packet)
		{
			case MuiCollectionSurfacePacketKind.Layout:
				if (field == MuiCollectionSurfaceField.MethodId) { offset = MuiCollectionLayoutMessage.MethodIdOffset; return true; }
				if (field == MuiCollectionSurfaceField.Left) { offset = MuiCollectionLayoutMessage.LeftOffset; return true; }
				if (field == MuiCollectionSurfaceField.Top) { offset = MuiCollectionLayoutMessage.TopOffset; return true; }
				if (field == MuiCollectionSurfaceField.Width) { offset = MuiCollectionLayoutMessage.WidthOffset; return true; }
				if (field == MuiCollectionSurfaceField.Height) { offset = MuiCollectionLayoutMessage.HeightOffset; return true; }
				break;
			case MuiCollectionSurfacePacketKind.AskMinMax:
				if (field == MuiCollectionSurfaceField.MethodId) { offset = MuiCollectionAskMinMaxMessage.MethodIdOffset; return true; }
				if (field == MuiCollectionSurfaceField.Storage) { offset = MuiCollectionAskMinMaxMessage.StorageOffset; return true; }
				break;
			case MuiCollectionSurfacePacketKind.Draw:
				if (field == MuiCollectionSurfaceField.MethodId) { offset = MuiCollectionDrawMessage.MethodIdOffset; return true; }
				if (field == MuiCollectionSurfaceField.Flags) { offset = MuiCollectionDrawMessage.FlagsOffset; return true; }
				break;
			case MuiCollectionSurfacePacketKind.HandleInput:
				if (field == MuiCollectionSurfaceField.MethodId) { offset = MuiCollectionHandleInputMessage.MethodIdOffset; return true; }
				if (field == MuiCollectionSurfaceField.IntuiMessage) { offset = MuiCollectionHandleInputMessage.IntuiMessageOffset; return true; }
				if (field == MuiCollectionSurfaceField.MuiKey) { offset = MuiCollectionHandleInputMessage.MuiKeyOffset; return true; }
				break;
			case MuiCollectionSurfacePacketKind.Attribute:
				if (field == MuiCollectionSurfaceField.MethodId) { offset = MuiCollectionAttributeMessage.MethodIdOffset; return true; }
				if (field == MuiCollectionSurfaceField.Attribute) { offset = MuiCollectionAttributeMessage.AttributeOffset; return true; }
				if (field == MuiCollectionSurfaceField.Value) { offset = MuiCollectionAttributeMessage.ValueOffset; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCollectionSurfaceFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor.Message, cursor.Packet,
			cursor.Field, out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionSurfacePacketKind packet,
		MuiCollectionSurfaceField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset) ||
			!TryGetPacketSize(packet, out var packetSize) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, packetSize))
			return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiCollectionMethodMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionSurfacePacketKind packet,
		MuiCollectionSurfaceField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionSurfacePacketKind packet,
		MuiCollectionSurfaceField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper for existing dispatcher and host callers. New code
// should use the packet-named memory adapter above.
internal static class MuiCollectionSurfaceFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCollectionSurfaceFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCollectionSurfaceMessageMemoryCodec.TryGetAddress(ref platform,
			cursor, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionSurfacePacketKind packet,
		MuiCollectionSurfaceField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCollectionSurfaceMessageMemoryCodec.TryReadUInt32(ref platform,
			message, packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionSurfacePacketKind packet,
		MuiCollectionSurfaceField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCollectionSurfaceMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, packet, field, value);
}

// Live collection-surface packets use declaration-order named structs. Signed
// MuiKey is transported as its lossless ULONG representation.
internal static class MuiCollectionSurfaceStructPacketCodec
{
	private static bool TryCreate<TPlatform>(ref TPlatform platform,
		APTR message, uint size, out MuiGuestStructCursor cursor)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, message, size,
			out cursor);

	internal static bool TryReadLayout<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionLayoutMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiCollectionLayoutMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Left) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Top) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Width) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Height) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteLayout<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionLayoutMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionLayoutMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Left) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Top) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Width) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Height) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiCollectionAskMinMaxMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Storage) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionAskMinMaxMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Storage) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadDraw<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionDrawMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiCollectionDrawMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteDraw<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionDrawMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionDrawMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadHandleInput<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionHandleInputMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryCreate(ref platform, message, MuiCollectionHandleInputMessage.Size,
			out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.IntuiMessage) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMuiKey) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		packet.MuiKey = unchecked((int)rawMuiKey);
		return true;
	}

	internal static bool TryWriteHandleInput<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionHandleInputMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionHandleInputMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.IntuiMessage) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)packet.MuiKey)) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadAttribute<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionAttributeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiCollectionAttributeMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Attribute) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Value) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteAttribute<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionAttributeMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionAttributeMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Attribute) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Value) && MuiGuestStructCursor.IsComplete(cursor);
}

internal static class MuiCollectionSurfaceMessageCodec
{
	internal const uint AskMinMax = 0x80423874u;
	internal const uint Draw = 0x80426F3Fu;
	internal const uint HandleInput = 0x80422A1Au;
	internal const uint Layout = 0x8042845Bu;
	internal const uint NoNotifySet = 0x8042216Fu;
	internal const uint Set = 0x8042549Au;

	internal static bool TryReadLayout<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionLayoutMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCollectionSurfaceStructPacketCodec.TryReadLayout(ref platform,
			message, out packet) && packet.MethodId == Layout;
	}

	internal static bool WriteLayout<TPlatform>(ref TPlatform platform,
		APTR message, uint left, uint top, uint width, uint height)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCollectionLayoutMessage);
		packet.MethodId = Layout;
		packet.Left = left;
		packet.Top = top;
		packet.Width = width;
		packet.Height = height;
		return MuiCollectionSurfaceStructPacketCodec.TryWriteLayout(ref platform,
			message, packet);
	}

	internal static bool TryReadAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCollectionSurfaceStructPacketCodec.TryReadAskMinMax(ref platform,
			message, out packet) && packet.MethodId == AskMinMax;
	}

	internal static bool WriteAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCollectionAskMinMaxMessage);
		packet.MethodId = AskMinMax;
		packet.Storage = storage;
		return MuiCollectionSurfaceStructPacketCodec.TryWriteAskMinMax(ref platform,
			message, packet);
	}

	internal static bool TryReadDraw<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionDrawMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCollectionSurfaceStructPacketCodec.TryReadDraw(ref platform,
			message, out packet) && packet.MethodId == Draw;
	}

	internal static bool WriteDraw<TPlatform>(ref TPlatform platform,
		APTR message, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCollectionDrawMessage);
		packet.MethodId = Draw;
		packet.Flags = flags;
		return MuiCollectionSurfaceStructPacketCodec.TryWriteDraw(ref platform,
			message, packet);
	}

	internal static bool TryReadHandleInput<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionHandleInputMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCollectionSurfaceStructPacketCodec.TryReadHandleInput(
			ref platform, message, out packet) && packet.MethodId == HandleInput;
	}

	internal static bool WriteHandleInput<TPlatform>(ref TPlatform platform,
		APTR message, uint intuiMessage, int muiKey)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCollectionHandleInputMessage);
		packet.MethodId = HandleInput;
		packet.IntuiMessage = intuiMessage;
		packet.MuiKey = muiKey;
		return MuiCollectionSurfaceStructPacketCodec.TryWriteHandleInput(
			ref platform, message, packet);
	}

	internal static bool TryReadAttribute<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCollectionAttributeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsAttributeMethod(method) &&
			MuiCollectionSurfaceStructPacketCodec.TryReadAttribute(ref platform,
				message, out packet) && packet.MethodId == method;
	}

	internal static bool WriteAttribute<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsAttributeMethod(method)) return false;
		var packet = default(MuiCollectionAttributeMessage);
		packet.MethodId = method;
		packet.Attribute = attribute;
		packet.Value = value;
		return MuiCollectionSurfaceStructPacketCodec.TryWriteAttribute(ref platform,
			message, packet);
	}

	private static bool IsAttributeMethod(uint method) => method == Set ||
		method == NoNotifySet;

	private static bool IsPacket<TPlatform>(ref TPlatform platform, APTR message,
		uint size, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		uint methodId;
		if (message.IsNull || !platform.IsMapped(message, size) ||
			!MuiCollectionBasicMessageCodec.TryReadMethodIdValue(ref platform, message,
				out methodId)) return false;
		return methodId == method;
	}
}
