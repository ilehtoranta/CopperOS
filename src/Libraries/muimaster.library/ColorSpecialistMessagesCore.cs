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
// packets. Packet kinds own complete MorphOS record spans; typed field
// addresses walk declaration-ordered named records without exposing numeric
// positions to dispatch code.
internal static class MuiColorSpecialistMessageMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiColorSpecialistPacketKind packet,
		MuiColorSpecialistField field, out uint index, out uint size)
	{
		index = 0;
		switch (packet)
		{
			case MuiColorSpecialistPacketKind.Method:
				size = MuiColorSpecialistMethodMessage.Size;
				return field == MuiColorSpecialistField.MethodId;
			case MuiColorSpecialistPacketKind.Get:
				size = MuiColorSpecialistGetMessage.Size;
				if (field == MuiColorSpecialistField.MethodId)
					index = 0;
				else if (field == MuiColorSpecialistField.Attribute)
					index = 1;
				else if (field == MuiColorSpecialistField.Storage)
					index = 2;
				else
				{
					size = 0;
					return false;
				}
				return true;
			case MuiColorSpecialistPacketKind.Set:
				size = MuiColorSpecialistSetMessage.Size;
				if (field == MuiColorSpecialistField.MethodId)
					index = 0;
				else if (field == MuiColorSpecialistField.Attribute)
					index = 1;
				else if (field == MuiColorSpecialistField.Value)
					index = 2;
				else
				{
					size = 0;
					return false;
				}
				return true;
			case MuiColorSpecialistPacketKind.Pointer:
				size = MuiColorSpecialistPointerMessage.Size;
				if (field == MuiColorSpecialistField.MethodId)
					index = 0;
				else if (field == MuiColorSpecialistField.Pointer)
					index = 1;
				else
				{
					size = 0;
					return false;
				}
				return true;
			case MuiColorSpecialistPacketKind.Rgb:
				size = MuiColorSpecialistRgbMessage.Size;
				if (field == MuiColorSpecialistField.MethodId)
					index = 0;
				else if (field == MuiColorSpecialistField.Red)
					index = 1;
				else if (field == MuiColorSpecialistField.Green)
					index = 2;
				else if (field == MuiColorSpecialistField.Blue)
					index = 3;
				else
				{
					size = 0;
					return false;
				}
				return true;
		}
		size = 0;
		return false;
	}

	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiColorSpecialistPacketKind packet,
		MuiColorSpecialistField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolveFieldIndex(packet, field, out var index, out _))
			return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiColorSpecialistMethodMessage.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				return true;
			}
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiColorSpecialistPacketKind packet,
		MuiColorSpecialistField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolveFieldIndex(packet, field, out _, out var size) ||
			!MuiGuestStructCursor.TryCreate(ref platform, message, size,
				out var cursor)) return false;
		return TryTakeField(ref platform, ref cursor, packet, field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiColorSpecialistPacketKind packet,
		MuiColorSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		switch (packet)
		{
			case MuiColorSpecialistPacketKind.Method:
				return field == MuiColorSpecialistField.MethodId &&
					MuiColorSpecialistMethodHeaderCodec.TryReadValue(ref platform,
						message, out value);
			case MuiColorSpecialistPacketKind.Get:
				if (!MuiColorSpecialistMessageStructCodec.TryReadGet(ref platform,
					message, out var get)) return false;
				value = field switch
				{
					MuiColorSpecialistField.MethodId => get.MethodId,
					MuiColorSpecialistField.Attribute => get.Attribute,
					MuiColorSpecialistField.Storage => get.Storage,
					_ => 0,
				};
				return field is MuiColorSpecialistField.MethodId or
					MuiColorSpecialistField.Attribute or MuiColorSpecialistField.Storage;
			case MuiColorSpecialistPacketKind.Set:
				if (!MuiColorSpecialistMessageStructCodec.TryReadSet(ref platform,
					message, out var set)) return false;
				value = field switch
				{
					MuiColorSpecialistField.MethodId => set.MethodId,
					MuiColorSpecialistField.Attribute => set.Attribute,
					MuiColorSpecialistField.Value => set.Value,
					_ => 0,
				};
				return field is MuiColorSpecialistField.MethodId or
					MuiColorSpecialistField.Attribute or MuiColorSpecialistField.Value;
			case MuiColorSpecialistPacketKind.Pointer:
				if (!MuiColorSpecialistMessageStructCodec.TryReadPointer(
					ref platform, message, out var pointer)) return false;
				value = field switch
				{
					MuiColorSpecialistField.MethodId => pointer.MethodId,
					MuiColorSpecialistField.Pointer => pointer.Pointer,
					_ => 0,
				};
				return field is MuiColorSpecialistField.MethodId or
					MuiColorSpecialistField.Pointer;
			case MuiColorSpecialistPacketKind.Rgb:
				if (!MuiColorSpecialistMessageStructCodec.TryReadRgb(ref platform,
					message, out var rgb)) return false;
				value = field switch
				{
					MuiColorSpecialistField.MethodId => rgb.MethodId,
					MuiColorSpecialistField.Red => rgb.Red,
					MuiColorSpecialistField.Green => rgb.Green,
					MuiColorSpecialistField.Blue => rgb.Blue,
					_ => 0,
				};
				return field is MuiColorSpecialistField.MethodId or
					MuiColorSpecialistField.Red or MuiColorSpecialistField.Green or
					MuiColorSpecialistField.Blue;
			default:
				return false;
		}
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiColorSpecialistPacketKind packet,
		MuiColorSpecialistField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		switch (packet)
		{
			case MuiColorSpecialistPacketKind.Method:
				return field == MuiColorSpecialistField.MethodId &&
					MuiColorSpecialistMethodHeaderCodec.WriteValue(ref platform,
						message, value);
			case MuiColorSpecialistPacketKind.Get:
				if (!MuiColorSpecialistMessageStructCodec.TryReadGet(ref platform,
					message, out var get)) return false;
				switch (field)
				{
					case MuiColorSpecialistField.MethodId: get.MethodId = value; break;
					case MuiColorSpecialistField.Attribute: get.Attribute = value; break;
					case MuiColorSpecialistField.Storage: get.Storage = value; break;
					default: return false;
				}
				return MuiColorSpecialistMessageStructCodec.WriteGet(ref platform,
					message, get);
			case MuiColorSpecialistPacketKind.Set:
				if (!MuiColorSpecialistMessageStructCodec.TryReadSet(ref platform,
					message, out var set)) return false;
				switch (field)
				{
					case MuiColorSpecialistField.MethodId: set.MethodId = value; break;
					case MuiColorSpecialistField.Attribute: set.Attribute = value; break;
					case MuiColorSpecialistField.Value: set.Value = value; break;
					default: return false;
				}
				return MuiColorSpecialistMessageStructCodec.WriteSet(ref platform,
					message, set);
			case MuiColorSpecialistPacketKind.Pointer:
				if (!MuiColorSpecialistMessageStructCodec.TryReadPointer(
					ref platform, message, out var pointer)) return false;
				switch (field)
				{
					case MuiColorSpecialistField.MethodId: pointer.MethodId = value; break;
					case MuiColorSpecialistField.Pointer: pointer.Pointer = value; break;
					default: return false;
				}
				return MuiColorSpecialistMessageStructCodec.WritePointer(ref platform,
					message, pointer);
			case MuiColorSpecialistPacketKind.Rgb:
				if (!MuiColorSpecialistMessageStructCodec.TryReadRgb(ref platform,
					message, out var rgb)) return false;
				switch (field)
				{
					case MuiColorSpecialistField.MethodId: rgb.MethodId = value; break;
					case MuiColorSpecialistField.Red: rgb.Red = value; break;
					case MuiColorSpecialistField.Green: rgb.Green = value; break;
					case MuiColorSpecialistField.Blue: rgb.Blue = value; break;
					default: return false;
				}
				return MuiColorSpecialistMessageStructCodec.WriteRgb(ref platform,
					message, rgb);
			default:
				return false;
		}
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
		=> MuiColorSpecialistMethodHeaderCodec.TryReadValue(ref platform,
			message, out methodId);

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryWriteMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiColorSpecialistMethodHeaderCodec.WriteValue(ref platform, message,
			methodId);

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

// Struct-first codec for the method-only pen/color specialist header. The
// named one-ULONG record remains the ABI contract while shared guest storage
// avoids direct scalar lowering at the freestanding 68k boundary.
internal static class MuiColorSpecialistMethodHeaderCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiColorSpecialistMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.TryReadValue(ref platform, valueAddress,
				out methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteValue<TPlatform>(ref TPlatform platform,
		APTR address, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiColorSpecialistMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
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
