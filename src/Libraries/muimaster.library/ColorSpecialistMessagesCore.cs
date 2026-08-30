/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

internal enum MuiColorSpecialistPacketKind : byte
{
	Method,
	Get,
	Set,
	Pointer,
	Rgb,
}

internal enum MuiColorSpecialistField : byte
{
	MethodId,
	Attribute,
	Storage,
	Value,
	Pointer,
	Red,
	Green,
	Blue,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiColorSpecialistFieldCursor
{
	internal APTR Message;
	internal MuiColorSpecialistPacketKind Packet;
	internal MuiColorSpecialistField Field;
}

// Struct-first guest-memory adapter for the fixed pen/color specialist
// packets. Packet kinds own complete MorphOS record spans; field names select
// members without exposing numeric positions to dispatch code.
internal static class MuiColorSpecialistMessageMemoryCodec
{
	private static bool TryResolve(MuiColorSpecialistPacketKind packet,
		MuiColorSpecialistField field, out uint offset, out uint size)
	{
		switch (packet)
		{
			case MuiColorSpecialistPacketKind.Method:
				size = MuiColorSpecialistMethodMessage.Size;
				if (field == MuiColorSpecialistField.MethodId)
					offset = MuiColorSpecialistMethodMessage.MethodIdOffset;
				else
				{
					offset = 0;
					size = 0;
					return false;
				}
				return true;
			case MuiColorSpecialistPacketKind.Get:
				size = MuiColorSpecialistGetMessage.Size;
				if (field == MuiColorSpecialistField.MethodId)
					offset = MuiColorSpecialistGetMessage.MethodIdOffset;
				else if (field == MuiColorSpecialistField.Attribute)
					offset = MuiColorSpecialistGetMessage.AttributeOffset;
				else if (field == MuiColorSpecialistField.Storage)
					offset = MuiColorSpecialistGetMessage.StorageOffset;
				else
				{
					offset = 0;
					size = 0;
					return false;
				}
				return true;
			case MuiColorSpecialistPacketKind.Set:
				size = MuiColorSpecialistSetMessage.Size;
				if (field == MuiColorSpecialistField.MethodId)
					offset = MuiColorSpecialistSetMessage.MethodIdOffset;
				else if (field == MuiColorSpecialistField.Attribute)
					offset = MuiColorSpecialistSetMessage.AttributeOffset;
				else if (field == MuiColorSpecialistField.Value)
					offset = MuiColorSpecialistSetMessage.ValueOffset;
				else
				{
					offset = 0;
					size = 0;
					return false;
				}
				return true;
			case MuiColorSpecialistPacketKind.Pointer:
				size = MuiColorSpecialistPointerMessage.Size;
				if (field == MuiColorSpecialistField.MethodId)
					offset = MuiColorSpecialistPointerMessage.MethodIdOffset;
				else if (field == MuiColorSpecialistField.Pointer)
					offset = MuiColorSpecialistPointerMessage.PointerOffset;
				else
				{
					offset = 0;
					size = 0;
					return false;
				}
				return true;
			case MuiColorSpecialistPacketKind.Rgb:
				size = MuiColorSpecialistRgbMessage.Size;
				if (field == MuiColorSpecialistField.MethodId)
					offset = MuiColorSpecialistRgbMessage.MethodIdOffset;
				else if (field == MuiColorSpecialistField.Red)
					offset = MuiColorSpecialistRgbMessage.RedOffset;
				else if (field == MuiColorSpecialistField.Green)
					offset = MuiColorSpecialistRgbMessage.GreenOffset;
				else if (field == MuiColorSpecialistField.Blue)
					offset = MuiColorSpecialistRgbMessage.BlueOffset;
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
		APTR message, MuiColorSpecialistPacketKind packet,
		MuiColorSpecialistField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset, out var size) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, size)) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address,
			MuiColorSpecialistMethodMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiColorSpecialistPacketKind packet,
		MuiColorSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiColorSpecialistPacketKind packet,
		MuiColorSpecialistField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// field cursor. The live message codecs route to the struct adapter above.
internal static class MuiColorSpecialistFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiColorSpecialistFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiColorSpecialistMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Packet, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiColorSpecialistPacketKind packet,
		MuiColorSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiColorSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiColorSpecialistPacketKind packet,
		MuiColorSpecialistField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiColorSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, packet, field, value);
}

// Complete sequential codecs for the fixed Color specialist packet records.
// Method-only packets use scalar cursor helpers for their one-ULONG record;
// all payload envelopes exchange every named field in declaration order.
internal static class MuiColorSpecialistMessageStructCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiColorSpecialistMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMethodId)) return false;
		methodId = rawMethodId;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryWriteMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiColorSpecialistMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiColorSpecialistGetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiColorSpecialistGetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var storage)) return false;
		value.MethodId = methodId;
		value.Attribute = attribute;
		value.Storage = storage;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform,
		APTR message, MuiColorSpecialistGetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiColorSpecialistGetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Storage)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiColorSpecialistSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiColorSpecialistSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var valueWord)) return false;
		value.MethodId = methodId;
		value.Attribute = attribute;
		value.Value = valueWord;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform,
		APTR message, MuiColorSpecialistSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiColorSpecialistSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadPointer<TPlatform>(ref TPlatform platform,
		APTR message, out MuiColorSpecialistPointerMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiColorSpecialistPointerMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var pointer)) return false;
		value.MethodId = methodId;
		value.Pointer = pointer;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WritePointer<TPlatform>(ref TPlatform platform,
		APTR message, MuiColorSpecialistPointerMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiColorSpecialistPointerMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Pointer)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadRgb<TPlatform>(ref TPlatform platform,
		APTR message, out MuiColorSpecialistRgbMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiColorSpecialistRgbMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var red) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var green) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var blue)) return false;
		value.MethodId = methodId;
		value.Red = red;
		value.Green = green;
		value.Blue = blue;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRgb<TPlatform>(ref TPlatform platform,
		APTR message, MuiColorSpecialistRgbMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiColorSpecialistRgbMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Red) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Green) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Blue)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Central codec for the fixed MorphOS pen/color specialist packet family.
// Dispatch consumers use the named records declared at the public boundary;
// only this adapter owns their packed guest-memory layout.
internal static class MuiColorSpecialistMessageCodec
{
	internal const uint OmDispose = 0x00000102u;
	internal const uint OmGet = 0x00000104u;
	internal const uint MethodSet = 0x8042549Au;
	internal const uint MethodNoNotifySet = 0x8042216Fu;
	internal const uint SetColormap = 0x80426C80u;
	internal const uint SetMUIPen = 0x8042039Du;
	internal const uint SetRGB = 0x8042C131u;

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiColorSpecialistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadMethodIdValue(ref platform, message, out var methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	// Selector admission stays scalar for callers that only need MethodID, but
	// the value is read from the named one-ULONG record in declaration order.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiColorSpecialistMessageStructCodec.TryReadMethodIdValue(
			ref platform, message, out methodId);
	}

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiColorSpecialistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsValidMethod(ref platform, message, method)) return false;
		packet.MethodId = method;
		return true;
	}

	// Native method-only consumers use this scalar form to avoid materializing
	// a one-field out record in compiler paths where it can widen the branch.
	internal static bool IsValidMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		return IsMethod(method) &&
			TryReadMethodIdValue(ref platform, message, out var methodId) &&
			methodId == method;
	}

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		return IsMethod(method) &&
			MuiColorSpecialistMessageStructCodec.TryWriteMethodIdValue(
				ref platform, message, method);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiColorSpecialistGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiColorSpecialistMessageStructCodec.TryReadGet(ref platform,
			message, out packet) && packet.MethodId == OmGet;
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform,
		APTR message, uint attribute, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiColorSpecialistGetMessage);
		packet.MethodId = OmGet;
		packet.Attribute = attribute;
		packet.Storage = storage;
		return MuiColorSpecialistMessageStructCodec.WriteGet(ref platform,
			message, packet);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiColorSpecialistSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsSetMethod(method) &&
			MuiColorSpecialistMessageStructCodec.TryReadSet(ref platform,
				message, out packet) && packet.MethodId == method;
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSetMethod(method)) return false;
		var packet = default(MuiColorSpecialistSetMessage);
		packet.MethodId = method;
		packet.Attribute = attribute;
		packet.Value = value;
		return MuiColorSpecialistMessageStructCodec.WriteSet(ref platform,
			message, packet);
	}

	internal static bool TryReadPointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiColorSpecialistPointerMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsPointerMethod(method) &&
			MuiColorSpecialistMessageStructCodec.TryReadPointer(ref platform,
				message, out packet) && packet.MethodId == method;
	}

	internal static bool WritePointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint pointer)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsPointerMethod(method)) return false;
		var packet = default(MuiColorSpecialistPointerMessage);
		packet.MethodId = method;
		packet.Pointer = pointer;
		return MuiColorSpecialistMessageStructCodec.WritePointer(ref platform,
			message, packet);
	}

	internal static bool TryReadRgb<TPlatform>(ref TPlatform platform,
		APTR message, out MuiColorSpecialistRgbMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiColorSpecialistMessageStructCodec.TryReadRgb(ref platform,
			message, out packet) && packet.MethodId == SetRGB;
	}

	internal static bool WriteRgb<TPlatform>(ref TPlatform platform,
		APTR message, uint red, uint green, uint blue)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiColorSpecialistRgbMessage);
		packet.MethodId = SetRGB;
		packet.Red = red;
		packet.Green = green;
		packet.Blue = blue;
		return MuiColorSpecialistMessageStructCodec.WriteRgb(ref platform,
			message, packet);
	}

	private static bool IsMethod(uint method) => method == OmDispose;

	private static bool IsSetMethod(uint method) => method == MethodSet ||
		method == MethodNoNotifySet;

	private static bool IsPointerMethod(uint method) => method == SetColormap ||
		method == SetMUIPen;

	private static bool IsPacket<TPlatform>(ref TPlatform platform, APTR message,
		uint size, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message, size) ||
			!TryReadMethodIdValue(ref platform, message, out var methodId)) return false;
		return methodId == method;
	}
}
