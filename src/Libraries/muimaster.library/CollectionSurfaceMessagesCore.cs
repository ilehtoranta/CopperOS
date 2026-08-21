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

internal static class MuiCollectionSurfaceFieldCursorCodec
{
	private static bool TryResolve(MuiCollectionSurfacePacketKind packet,
		MuiCollectionSurfaceField field, out uint offset)
	{
		switch (packet)
		{
			case MuiCollectionSurfacePacketKind.Layout:
				if (field == MuiCollectionSurfaceField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionSurfaceField.Left) { offset = 4; return true; }
				if (field == MuiCollectionSurfaceField.Top) { offset = 8; return true; }
				if (field == MuiCollectionSurfaceField.Width) { offset = 12; return true; }
				if (field == MuiCollectionSurfaceField.Height) { offset = 16; return true; }
				break;
			case MuiCollectionSurfacePacketKind.AskMinMax:
				if (field == MuiCollectionSurfaceField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionSurfaceField.Storage) { offset = 4; return true; }
				break;
			case MuiCollectionSurfacePacketKind.Draw:
				if (field == MuiCollectionSurfaceField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionSurfaceField.Flags) { offset = 4; return true; }
				break;
			case MuiCollectionSurfacePacketKind.HandleInput:
				if (field == MuiCollectionSurfaceField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionSurfaceField.IntuiMessage) { offset = 4; return true; }
				if (field == MuiCollectionSurfaceField.MuiKey) { offset = 8; return true; }
				break;
			case MuiCollectionSurfacePacketKind.Attribute:
				if (field == MuiCollectionSurfaceField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionSurfaceField.Attribute) { offset = 4; return true; }
				if (field == MuiCollectionSurfaceField.Value) { offset = 8; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCollectionSurfaceFieldCursor cursor, out APTR address)
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
		APTR message, MuiCollectionSurfacePacketKind packet,
		MuiCollectionSurfaceField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiCollectionSurfaceFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionSurfacePacketKind packet,
		MuiCollectionSurfaceField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiCollectionSurfaceFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
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
		if (!IsPacket(ref platform, message, MuiCollectionLayoutMessage.Size,
			Layout)) return false;
		packet.MethodId = Layout;
		return MuiCollectionSurfaceFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionSurfacePacketKind.Layout,
			MuiCollectionSurfaceField.Left, out packet.Left) &&
			MuiCollectionSurfaceFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionSurfacePacketKind.Layout,
				MuiCollectionSurfaceField.Top, out packet.Top) &&
			MuiCollectionSurfaceFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionSurfacePacketKind.Layout,
				MuiCollectionSurfaceField.Width, out packet.Width) &&
			MuiCollectionSurfaceFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionSurfacePacketKind.Layout,
				MuiCollectionSurfaceField.Height, out packet.Height);
	}

	internal static bool WriteLayout<TPlatform>(ref TPlatform platform,
		APTR message, uint left, uint top, uint width, uint height)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionLayoutMessage.Size)) return false;
		return MuiCollectionSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionSurfacePacketKind.Layout,
			MuiCollectionSurfaceField.MethodId, Layout) &&
			MuiCollectionSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionSurfacePacketKind.Layout,
				MuiCollectionSurfaceField.Left, left) &&
			MuiCollectionSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionSurfacePacketKind.Layout,
				MuiCollectionSurfaceField.Top, top) &&
			MuiCollectionSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionSurfacePacketKind.Layout,
				MuiCollectionSurfaceField.Width, width) &&
			MuiCollectionSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionSurfacePacketKind.Layout,
				MuiCollectionSurfaceField.Height, height);
	}

	internal static bool TryReadAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiCollectionAskMinMaxMessage.Size,
			AskMinMax)) return false;
		packet.MethodId = AskMinMax;
		return MuiCollectionSurfaceFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionSurfacePacketKind.AskMinMax,
			MuiCollectionSurfaceField.Storage, out packet.Storage);
	}

	internal static bool WriteAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionAskMinMaxMessage.Size)) return false;
		return MuiCollectionSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionSurfacePacketKind.AskMinMax,
			MuiCollectionSurfaceField.MethodId, AskMinMax) &&
			MuiCollectionSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionSurfacePacketKind.AskMinMax,
				MuiCollectionSurfaceField.Storage, storage);
	}

	internal static bool TryReadDraw<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionDrawMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiCollectionDrawMessage.Size,
			Draw)) return false;
		packet.MethodId = Draw;
		return MuiCollectionSurfaceFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionSurfacePacketKind.Draw,
			MuiCollectionSurfaceField.Flags, out packet.Flags);
	}

	internal static bool WriteDraw<TPlatform>(ref TPlatform platform,
		APTR message, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionDrawMessage.Size)) return false;
		return MuiCollectionSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionSurfacePacketKind.Draw,
			MuiCollectionSurfaceField.MethodId, Draw) &&
			MuiCollectionSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionSurfacePacketKind.Draw,
				MuiCollectionSurfaceField.Flags, flags);
	}

	internal static bool TryReadHandleInput<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionHandleInputMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message,
			MuiCollectionHandleInputMessage.Size, HandleInput)) return false;
		packet.MethodId = HandleInput;
		if (!MuiCollectionSurfaceFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionSurfacePacketKind.HandleInput,
			MuiCollectionSurfaceField.IntuiMessage, out packet.IntuiMessage) ||
			!MuiCollectionSurfaceFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionSurfacePacketKind.HandleInput,
				MuiCollectionSurfaceField.MuiKey, out var rawMuiKey)) return false;
		packet.MuiKey = unchecked((int)rawMuiKey);
		return true;
	}

	internal static bool WriteHandleInput<TPlatform>(ref TPlatform platform,
		APTR message, uint intuiMessage, int muiKey)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionHandleInputMessage.Size)) return false;
		return MuiCollectionSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionSurfacePacketKind.HandleInput,
			MuiCollectionSurfaceField.MethodId, HandleInput) &&
			MuiCollectionSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionSurfacePacketKind.HandleInput,
				MuiCollectionSurfaceField.IntuiMessage, intuiMessage) &&
			MuiCollectionSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionSurfacePacketKind.HandleInput,
				MuiCollectionSurfaceField.MuiKey, unchecked((uint)muiKey));
	}

	internal static bool TryReadAttribute<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCollectionAttributeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsAttributeMethod(method) || !IsPacket(ref platform, message,
			MuiCollectionAttributeMessage.Size, method)) return false;
		packet.MethodId = method;
		return MuiCollectionSurfaceFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionSurfacePacketKind.Attribute,
			MuiCollectionSurfaceField.Attribute, out packet.Attribute) &&
			MuiCollectionSurfaceFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionSurfacePacketKind.Attribute,
				MuiCollectionSurfaceField.Value, out packet.Value);
	}

	internal static bool WriteAttribute<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsAttributeMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiCollectionAttributeMessage.Size)) return false;
		return MuiCollectionSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionSurfacePacketKind.Attribute,
			MuiCollectionSurfaceField.MethodId, method) &&
			MuiCollectionSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionSurfacePacketKind.Attribute,
				MuiCollectionSurfaceField.Attribute, attribute) &&
			MuiCollectionSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionSurfacePacketKind.Attribute,
				MuiCollectionSurfaceField.Value, value);
	}

	private static bool IsAttributeMethod(uint method) => method == Set ||
		method == NoNotifySet;

	private static bool IsPacket<TPlatform>(ref TPlatform platform, APTR message,
		uint size, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message, size) ||
			!MuiCollectionBasicMessageCodec.TryReadMethodId(ref platform, message,
				out var header)) return false;
		return header.MethodId == method;
	}
}
