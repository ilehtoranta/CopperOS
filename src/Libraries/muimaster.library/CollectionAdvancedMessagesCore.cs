/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Central codec for the fixed MorphOS 3.20 List advanced packet family. The
// collection core consumes named position/pair/image fields; only this adapter
// owns packed guest-memory offsets and method validation.
internal enum MuiCollectionAdvancedPacketKind : byte
{
	Method,
	InsertSingle,
	Insert,
	Position,
	Pointer,
	Pair,
	CreateImage,
}

internal enum MuiCollectionAdvancedField : byte
{
	MethodId,
	Entry,
	Position,
	Column,
	Pointer,
	First,
	Second,
	Image,
	Flags,
}

[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionAdvancedFieldCursor
{
	internal APTR Message;
	internal MuiCollectionAdvancedPacketKind Packet;
	internal MuiCollectionAdvancedField Field;
}

internal static class MuiCollectionAdvancedFieldCursorCodec
{
	private static bool TryResolve(MuiCollectionAdvancedPacketKind packet,
		MuiCollectionAdvancedField field, out uint offset)
	{
		switch (packet)
		{
			case MuiCollectionAdvancedPacketKind.Method:
				if (field == MuiCollectionAdvancedField.MethodId) { offset = 0; return true; }
				break;
			case MuiCollectionAdvancedPacketKind.InsertSingle:
				if (field == MuiCollectionAdvancedField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionAdvancedField.Entry) { offset = 4; return true; }
				if (field == MuiCollectionAdvancedField.Position) { offset = 8; return true; }
				break;
			case MuiCollectionAdvancedPacketKind.Insert:
				if (field == MuiCollectionAdvancedField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionAdvancedField.Entry) { offset = 4; return true; }
				if (field == MuiCollectionAdvancedField.Position) { offset = 8; return true; }
				if (field == MuiCollectionAdvancedField.Column) { offset = 12; return true; }
				break;
			case MuiCollectionAdvancedPacketKind.Position:
				if (field == MuiCollectionAdvancedField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionAdvancedField.Position) { offset = 4; return true; }
				break;
			case MuiCollectionAdvancedPacketKind.Pointer:
				if (field == MuiCollectionAdvancedField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionAdvancedField.Pointer) { offset = 4; return true; }
				break;
			case MuiCollectionAdvancedPacketKind.Pair:
				if (field == MuiCollectionAdvancedField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionAdvancedField.First) { offset = 4; return true; }
				if (field == MuiCollectionAdvancedField.Second) { offset = 8; return true; }
				break;
			case MuiCollectionAdvancedPacketKind.CreateImage:
				if (field == MuiCollectionAdvancedField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionAdvancedField.Image) { offset = 4; return true; }
				if (field == MuiCollectionAdvancedField.Flags) { offset = 8; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCollectionAdvancedFieldCursor cursor, out APTR address)
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
		APTR message, MuiCollectionAdvancedPacketKind packet,
		MuiCollectionAdvancedField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiCollectionAdvancedFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionAdvancedPacketKind packet,
		MuiCollectionAdvancedField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiCollectionAdvancedFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiCollectionAdvancedMessageCodec
{
	internal const uint InsertSingle = 0x804254D5u;
	internal const uint Insert = 0x80426C87u;
	internal const uint Remove = 0x8042647Eu;
	internal const uint NextSelected = 0x80425F17u;
	internal const uint SortEntries = 0x80429E32u;
	internal const uint Move = 0x804253C2u;
	internal const uint Exchange = 0x8042468Cu;
	internal const uint Jump = 0x8042BAABu;
	internal const uint Redraw = 0x80427993u;
	internal const uint CreateImage = 0x80429804u;
	internal const uint DeleteImage = 0x80420F58u;
	internal const uint FloattextAppend = 0x8042A221u;

	private static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiCollectionAdvancedFieldCursorCodec.TryReadUInt32(
			ref platform, message, MuiCollectionAdvancedPacketKind.Method,
			MuiCollectionAdvancedField.MethodId, out method);
	}

	private static bool TryWriteMethodId<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(
			ref platform, message, MuiCollectionAdvancedPacketKind.Method,
			MuiCollectionAdvancedField.MethodId, method);
	}

	internal static bool TryReadInsertSingle<TPlatform>(
		ref TPlatform platform, APTR message,
		out MuiCollectionInsertSingleMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionInsertSingleMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var method) ||
			method != InsertSingle) return false;
		packet.MethodId = method;
		return MuiCollectionAdvancedFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionAdvancedPacketKind.InsertSingle,
			MuiCollectionAdvancedField.Entry, out packet.Entry) &&
			MuiCollectionAdvancedFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionAdvancedPacketKind.InsertSingle,
				MuiCollectionAdvancedField.Position, out packet.Position);
	}

	internal static bool WriteInsertSingle<TPlatform>(ref TPlatform platform,
		APTR message, uint entry, uint position)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionInsertSingleMessage.Size)) return false;
		return MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionAdvancedPacketKind.InsertSingle,
			MuiCollectionAdvancedField.MethodId, InsertSingle) &&
			MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionAdvancedPacketKind.InsertSingle,
				MuiCollectionAdvancedField.Entry, entry) &&
			MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionAdvancedPacketKind.InsertSingle,
				MuiCollectionAdvancedField.Position, position);
	}

	internal static bool TryReadInsert<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionInsertMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionInsertMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var method) ||
			method != Insert) return false;
		packet.MethodId = method;
		return MuiCollectionAdvancedFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionAdvancedPacketKind.Insert,
			MuiCollectionAdvancedField.Entry, out packet.Entry) &&
			MuiCollectionAdvancedFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionAdvancedPacketKind.Insert,
				MuiCollectionAdvancedField.Position, out packet.Position) &&
			MuiCollectionAdvancedFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionAdvancedPacketKind.Insert,
				MuiCollectionAdvancedField.Column, out packet.Column);
	}

	internal static bool WriteInsert<TPlatform>(ref TPlatform platform,
		APTR message, uint entry, uint position, uint column)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionInsertMessage.Size)) return false;
		return MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionAdvancedPacketKind.Insert,
			MuiCollectionAdvancedField.MethodId, Insert) &&
			MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionAdvancedPacketKind.Insert,
				MuiCollectionAdvancedField.Entry, entry) &&
			MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionAdvancedPacketKind.Insert,
				MuiCollectionAdvancedField.Position, position) &&
			MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionAdvancedPacketKind.Insert,
				MuiCollectionAdvancedField.Column, column);
	}

	internal static bool TryReadPosition<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCollectionPositionMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionPositionMessage.Size) || !IsPositionMethod(method) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header != method) return false;
		packet.MethodId = header;
		return MuiCollectionAdvancedFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionAdvancedPacketKind.Position,
			MuiCollectionAdvancedField.Position, out packet.Position);
	}

	internal static bool WritePosition<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint position)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionPositionMessage.Size) || !IsPositionMethod(method))
			return false;
		return MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionAdvancedPacketKind.Position,
			MuiCollectionAdvancedField.MethodId, method) &&
			MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionAdvancedPacketKind.Position,
				MuiCollectionAdvancedField.Position, position);
	}

	internal static bool TryReadPointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCollectionPointerMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionPointerMessage.Size) || !IsPointerMethod(method) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header != method) return false;
		packet.MethodId = header;
		return MuiCollectionAdvancedFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionAdvancedPacketKind.Pointer,
			MuiCollectionAdvancedField.Pointer, out packet.Pointer);
	}

	internal static bool WritePointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint pointer)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionPointerMessage.Size) || !IsPointerMethod(method))
			return false;
		return MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionAdvancedPacketKind.Pointer,
			MuiCollectionAdvancedField.MethodId, method) &&
			MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionAdvancedPacketKind.Pointer,
				MuiCollectionAdvancedField.Pointer, pointer);
	}

	internal static bool TryReadPair<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCollectionPairMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionPairMessage.Size) || !IsPairMethod(method) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header != method) return false;
		packet.MethodId = header;
		return MuiCollectionAdvancedFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionAdvancedPacketKind.Pair,
			MuiCollectionAdvancedField.First, out packet.First) &&
			MuiCollectionAdvancedFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionAdvancedPacketKind.Pair,
				MuiCollectionAdvancedField.Second, out packet.Second);
	}

	internal static bool WritePair<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint first, uint second)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionPairMessage.Size) || !IsPairMethod(method)) return false;
		return MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionAdvancedPacketKind.Pair,
			MuiCollectionAdvancedField.MethodId, method) &&
			MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionAdvancedPacketKind.Pair,
				MuiCollectionAdvancedField.First, first) &&
			MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionAdvancedPacketKind.Pair,
				MuiCollectionAdvancedField.Second, second);
	}

	internal static bool TryReadCreateImage<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionCreateImageMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionCreateImageMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var method) ||
			method != CreateImage) return false;
		packet.MethodId = method;
		return MuiCollectionAdvancedFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionAdvancedPacketKind.CreateImage,
			MuiCollectionAdvancedField.Image, out packet.Image) &&
			MuiCollectionAdvancedFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionAdvancedPacketKind.CreateImage,
				MuiCollectionAdvancedField.Flags, out packet.Flags);
	}

	internal static bool WriteCreateImage<TPlatform>(ref TPlatform platform,
		APTR message, uint image, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionCreateImageMessage.Size)) return false;
		return MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionAdvancedPacketKind.CreateImage,
			MuiCollectionAdvancedField.MethodId, CreateImage) &&
			MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionAdvancedPacketKind.CreateImage,
				MuiCollectionAdvancedField.Image, image) &&
			MuiCollectionAdvancedFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionAdvancedPacketKind.CreateImage,
				MuiCollectionAdvancedField.Flags, flags);
	}

	private static bool IsPositionMethod(uint method) => method == Remove ||
		method == Jump || method == Redraw;

	private static bool IsPointerMethod(uint method) => method == NextSelected ||
		method == SortEntries || method == DeleteImage || method == FloattextAppend;

	private static bool IsPairMethod(uint method) => method == Move ||
		method == Exchange;
}
