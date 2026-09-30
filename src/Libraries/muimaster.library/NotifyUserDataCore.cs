/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

// These packet records describe the 68k ABI explicitly.  The low-level packet
// reader below is the only place that translates guest bytes into fields; the
// UserData implementation itself works with these records rather than magic
// offsets scattered through the dispatcher.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFindUDataMessage
{
	public const uint Size = 8;
	public const uint FieldSize = 4;
	public uint MethodId;
	public uint UserData;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGetUDataMessage
{
	public const uint Size = 16;
	public const uint FieldSize = 4;
	public uint MethodId;
	public uint UserData;
	public uint Attribute;
	public uint Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSetUDataMessage
{
	public const uint Size = 16;
	public const uint FieldSize = 4;
	public uint MethodId;
	public uint UserData;
	public uint Attribute;
	public uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNotifyUserDataMethodMessage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal uint MethodId;
}

internal enum MuiNotifyUserDataPacketKind : byte
{
	Find,
	Get,
	Set,
}

internal enum MuiNotifyUserDataPacketField : byte
{
	MethodId,
	UserData,
	Attribute,
	Storage,
	Value,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNotifyUserDataPacketFieldCursor
{
	internal APTR Message;
	internal MuiNotifyUserDataPacketKind Packet;
	internal MuiNotifyUserDataPacketField Field;
}

// Struct-first guest-memory adapter for the fixed Find/Get/Set envelopes.
// Packet-specific named records own the wire layout; callers select fields by
// enum and the bounded cursor walks the declaration order.
internal static class MuiNotifyUserDataPacketMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiNotifyUserDataPacketKind packet,
		MuiNotifyUserDataPacketField field, out uint index, out uint recordSize)
	{
		index = 0;
		recordSize = 0;
		switch (packet)
		{
			case MuiNotifyUserDataPacketKind.Find:
				recordSize = MuiFindUDataMessage.Size;
				if (field == MuiNotifyUserDataPacketField.MethodId) { index = 0; return true; }
				if (field == MuiNotifyUserDataPacketField.UserData) { index = 1; return true; }
				break;
			case MuiNotifyUserDataPacketKind.Get:
				recordSize = MuiGetUDataMessage.Size;
				if (field == MuiNotifyUserDataPacketField.MethodId) { index = 0; return true; }
				if (field == MuiNotifyUserDataPacketField.UserData) { index = 1; return true; }
				if (field == MuiNotifyUserDataPacketField.Attribute) { index = 2; return true; }
				if (field == MuiNotifyUserDataPacketField.Storage) { index = 3; return true; }
				break;
			case MuiNotifyUserDataPacketKind.Set:
				recordSize = MuiSetUDataMessage.Size;
				if (field == MuiNotifyUserDataPacketField.MethodId) { index = 0; return true; }
				if (field == MuiNotifyUserDataPacketField.UserData) { index = 1; return true; }
				if (field == MuiNotifyUserDataPacketField.Attribute) { index = 2; return true; }
				if (field == MuiNotifyUserDataPacketField.Value) { index = 3; return true; }
				break;
		}
		index = uint.MaxValue;
		recordSize = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyUserDataPacketKind packet,
		MuiNotifyUserDataPacketField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiNotifyUserDataPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiNotifyUserDataPacketFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Packet, cursor.Field, out var index,
			out var recordSize) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Message,
				recordSize, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiFindUDataMessage.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiFindUDataMessage.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiNotifyUserDataPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyUserDataPacketKind packet,
		MuiNotifyUserDataPacketField field, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiNotifyUserDataPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyUserDataPacketKind packet,
		MuiNotifyUserDataPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryResolveFieldIndex(packet, field, out _, out var size) || message.IsNull ||
			!platform.IsMapped(message, size)) return false;
		switch (packet)
		{
			case MuiNotifyUserDataPacketKind.Find:
				if (!MuiFindUDataMessageCodec.TryRead(ref platform, message,
					out var find)) return false;
				switch (field)
				{
					case MuiNotifyUserDataPacketField.MethodId:
						value = find.MethodId;
						return true;
					case MuiNotifyUserDataPacketField.UserData:
						value = find.UserData;
						return true;
					default:
						return false;
				}
			case MuiNotifyUserDataPacketKind.Get:
				if (!MuiGetUDataMessageCodec.TryRead(ref platform, message,
					out var get)) return false;
				switch (field)
				{
					case MuiNotifyUserDataPacketField.MethodId:
						value = get.MethodId;
						return true;
					case MuiNotifyUserDataPacketField.UserData:
						value = get.UserData;
						return true;
					case MuiNotifyUserDataPacketField.Attribute:
						value = get.Attribute;
						return true;
					case MuiNotifyUserDataPacketField.Storage:
						value = get.Storage;
						return true;
					default:
						return false;
				}
			case MuiNotifyUserDataPacketKind.Set:
				if (!MuiSetUDataMessageCodec.TryRead(ref platform, message,
					out var set)) return false;
				switch (field)
				{
					case MuiNotifyUserDataPacketField.MethodId:
						value = set.MethodId;
						return true;
					case MuiNotifyUserDataPacketField.UserData:
						value = set.UserData;
						return true;
					case MuiNotifyUserDataPacketField.Attribute:
						value = set.Attribute;
						return true;
					case MuiNotifyUserDataPacketField.Value:
						value = set.Value;
						return true;
					default:
						return false;
				}
			default:
				return false;
		}
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyUserDataPacketKind packet,
		MuiNotifyUserDataPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryResolveFieldIndex(packet, field, out _, out var size) || message.IsNull ||
			!platform.IsMapped(message, size)) return false;
		switch (packet)
		{
			case MuiNotifyUserDataPacketKind.Find:
				if (!MuiFindUDataMessageCodec.TryRead(ref platform, message,
					out var find)) return false;
				switch (field)
				{
					case MuiNotifyUserDataPacketField.MethodId:
						find.MethodId = value;
						break;
					case MuiNotifyUserDataPacketField.UserData:
						find.UserData = value;
						break;
					default:
						return false;
				}
				return MuiFindUDataMessageCodec.Write(ref platform, message, find);
			case MuiNotifyUserDataPacketKind.Get:
				if (!MuiGetUDataMessageCodec.TryRead(ref platform, message,
					out var get)) return false;
				switch (field)
				{
					case MuiNotifyUserDataPacketField.MethodId:
						get.MethodId = value;
						break;
					case MuiNotifyUserDataPacketField.UserData:
						get.UserData = value;
						break;
					case MuiNotifyUserDataPacketField.Attribute:
						get.Attribute = value;
						break;
					case MuiNotifyUserDataPacketField.Storage:
						get.Storage = value;
						break;
					default:
						return false;
				}
				return MuiGetUDataMessageCodec.Write(ref platform, message, get);
			case MuiNotifyUserDataPacketKind.Set:
				if (!MuiSetUDataMessageCodec.TryRead(ref platform, message,
					out var set)) return false;
				switch (field)
				{
					case MuiNotifyUserDataPacketField.MethodId:
						set.MethodId = value;
						break;
					case MuiNotifyUserDataPacketField.UserData:
						set.UserData = value;
						break;
					case MuiNotifyUserDataPacketField.Attribute:
						set.Attribute = value;
						break;
					case MuiNotifyUserDataPacketField.Value:
						set.Value = value;
						break;
					default:
						return false;
				}
				return MuiSetUDataMessageCodec.Write(ref platform, message, set);
			default:
				return false;
		}
	}
}

// Compatibility wrapper retained for existing typed cursor diagnostics.
internal static class MuiNotifyUserDataPacketFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiNotifyUserDataPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiNotifyUserDataPacketMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiNotifyUserDataPacketFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiNotifyUserDataPacketMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyUserDataPacketKind packet,
		MuiNotifyUserDataPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiNotifyUserDataPacketMemoryCodec.TryReadUInt32(ref platform, message,
			packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyUserDataPacketKind packet,
		MuiNotifyUserDataPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiNotifyUserDataPacketMemoryCodec.TryWriteUInt32(ref platform, message,
			packet, field, value);
}

internal static class MuiNotifyUserDataMessageCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNotifyUserDataMethodMessage.Size, out var cursor) ||
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
			MuiNotifyUserDataMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiNotifyUserDataMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	// Keep native selector admission scalar while the named method record remains
	// the dispatcher-facing ABI type. The fixed record is consumed sequentially;
	// no field-offset adapter is needed on this production path.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadValue(ref platform, message, out methodId);
}

internal static class MuiFindUDataMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiFindUDataMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiFindUDataMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.UserData)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiFindUDataMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiFindUDataMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.UserData)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiGetUDataMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGetUDataMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGetUDataMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.UserData) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Storage)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGetUDataMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGetUDataMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.UserData) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Storage)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiSetUDataMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiSetUDataMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiSetUDataMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.UserData) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiSetUDataMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiSetUDataMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.UserData) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiUDataTraversalFrame
{
	public const uint Size = 8;
	public const uint FieldSize = 4;
	public APTR Object;
	public uint NextChild;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiUDataTraversalCursor
{
	internal const uint EntrySize = MuiUDataTraversalFrame.Size;
	internal const uint MaximumEntries = 256;
	internal APTR Base;
	internal uint Index;
}

internal enum MuiUDataTraversalField : byte
{
	Object,
	NextChild,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiUDataTraversalFieldCursor
{
	internal APTR Frame;
	internal MuiUDataTraversalField Field;
}

// Struct-first guest-memory adapter for one traversal frame.  The stack
// vector selects records; this codec owns the complete frame admission and
// named Object/NextChild field translation.
internal static class MuiUDataTraversalFrameMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiUDataTraversalField field,
		out uint index)
	{
		if (field == MuiUDataTraversalField.Object) { index = 0; return true; }
		if (field == MuiUDataTraversalField.NextChild) { index = 1; return true; }
		index = uint.MaxValue;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR frame, MuiUDataTraversalField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiUDataTraversalFieldCursor);
		cursor.Frame = frame;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiUDataTraversalFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Frame,
			MuiUDataTraversalFrame.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiUDataTraversalFrame.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiUDataTraversalFrame.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiUDataTraversalFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR frame, MuiUDataTraversalField field, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiUDataTraversalFieldCursor);
		cursor.Frame = frame;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR frame, MuiUDataTraversalField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryResolveFieldIndex(field, out _) || frame.IsNull ||
			!platform.IsMapped(frame, MuiUDataTraversalFrame.Size) ||
			!MuiUDataTraversalFrameRecordCodec.TryRead(ref platform, frame,
				out var record)) return false;
		switch (field)
		{
			case MuiUDataTraversalField.Object:
				value = record.Object.Raw;
				return true;
			case MuiUDataTraversalField.NextChild:
				value = record.NextChild;
				return true;
			default:
				return false;
		}
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR frame, MuiUDataTraversalField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryResolveFieldIndex(field, out _) || frame.IsNull ||
			!platform.IsMapped(frame, MuiUDataTraversalFrame.Size) ||
			!MuiUDataTraversalFrameRecordCodec.TryRead(ref platform, frame,
				out var record)) return false;
		switch (field)
		{
			case MuiUDataTraversalField.Object:
				record.Object = APTR.FromPointer(value);
				break;
			case MuiUDataTraversalField.NextChild:
				record.NextChild = value;
				break;
			default:
				return false;
		}
		return MuiUDataTraversalFrameRecordCodec.Write(ref platform, frame,
			record);
	}
}

// Complete sequential codec for the fixed 8-byte traversal frame.  The stack
// vector below remains an indexed address adapter; once an entry is selected,
// all frame fields travel through this named record codec.
internal static class MuiUDataTraversalFrameRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiUDataTraversalFrame value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiUDataTraversalFrame.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawObject) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var nextChild)) return false;
		value.Object = APTR.FromPointer(rawObject);
		value.NextChild = nextChild;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiUDataTraversalFrame value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiUDataTraversalFrame.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Object.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.NextChild)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Compatibility wrapper retained for existing typed cursor diagnostics.
internal static class MuiUDataTraversalFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiUDataTraversalFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiUDataTraversalFrameMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiUDataTraversalFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiUDataTraversalFrameMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR frame, MuiUDataTraversalField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiUDataTraversalFrameMemoryCodec.TryReadUInt32(ref platform, frame, field,
			out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR frame, MuiUDataTraversalField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiUDataTraversalFrameMemoryCodec.TryWriteUInt32(ref platform, frame, field,
			value);
}

// Struct-first guest-memory adapter for the bounded traversal-frame stack.
// Complete named frames and the 256-level MorphOS traversal bound are
// admitted here; the typed cursor remains a compatibility wrapper.
internal static class MuiUDataTraversalFrameVectorMemoryCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (vector.IsNull || index >= MuiUDataTraversalCursor.MaximumEntries ||
			index > (uint.MaxValue - vector.Raw) /
			MuiUDataTraversalFrame.Size) return false;
		var offset = index * MuiUDataTraversalFrame.Size;
		if (vector.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector.Raw + offset);
		return platform.IsMapped(address, MuiUDataTraversalFrame.Size);
	}
}

internal static class MuiUDataTraversalFrameCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiUDataTraversalCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiUDataTraversalFrameVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);

	// Traversal consumers exchange complete frame records through the typed
	// cursor. Stack-slot arithmetic remains private to the bounded adapter.
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		MuiUDataTraversalCursor cursor, out MuiUDataTraversalFrame value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryGetEntry(ref platform, cursor, out var address)) return false;
		return MuiUDataTraversalFrameRecordCodec.TryRead(ref platform, address,
			out value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		MuiUDataTraversalCursor cursor, MuiUDataTraversalFrame value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetEntry(ref platform, cursor, out var address)) return false;
		return MuiUDataTraversalFrameRecordCodec.Write(ref platform, address,
			value);
	}
}

internal static class MuiNotifyUserDataRecords
{
	public static bool TryReadFrame<TPlatform>(ref TPlatform platform,
		APTR address, ref MuiUDataTraversalFrame frame)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return MuiUDataTraversalFrameRecordCodec.TryRead(ref platform, address,
			out frame);
	}

	public static bool WriteFrame<TPlatform>(ref TPlatform platform,
		APTR address, MuiUDataTraversalFrame frame)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return MuiUDataTraversalFrameRecordCodec.Write(ref platform, address,
			frame);
	}
}

internal static class MuiNotifyUserDataCore
{
	private const uint UserDataAttribute = 0x80420313;
	private const uint NotVisited = uint.MaxValue;
	private const uint MaximumDepth = 256;

	public const uint FindUData = 0x8042C196;
	public const uint GetUData = 0x8042ED0C;
	public const uint SetUData = 0x8042C920;
	public const uint SetUDataOnce = 0x8042CA19;

	public static APTR Find<TPlatform>(ref TPlatform platform, APTR state,
		APTR root, uint userData)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var stackRaw = Begin(ref platform, state, root);
		if (stackRaw == 0) return APTR.Null;
		var stack = APTR.FromPointer(stackRaw);
		var stackBytes = MaximumDepth * MuiUDataTraversalFrame.Size;
		if (stack.IsNull)
			return APTR.Null;
		var depth = 1u;
		var visited = 0u;
		while (depth != 0)
		{
			if (!TryReadFrame(ref platform, state, stack, depth,
				out var frameCursor, out var current, out var nextChild))
				return Finish(ref platform, stack, stackBytes, APTR.Null);
			if (nextChild == NotVisited)
			{
				if (visited++ >= MuiHeadlessLayout.MaximumTraversal)
					return Finish(ref platform, stack, stackBytes, APTR.Null);
				if (Matches(ref platform, state, current, userData))
					return Finish(ref platform, stack, stackBytes, current);
				if (!MuiUDataTraversalFrameCodec.TryWrite(ref platform, frameCursor,
					CreateFrame(current, 0)))
					return Finish(ref platform, stack, stackBytes, APTR.Null);
				continue;
			}
			if (!Descend(ref platform, state, current, nextChild,
				ref depth, stack))
				return Finish(ref platform, stack, stackBytes, APTR.Null);
		}
		return Finish(ref platform, stack, stackBytes, APTR.Null);
	}

	public static bool Get<TPlatform>(ref TPlatform platform, APTR state,
		APTR root, uint userData, uint attribute, APTR storage)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (storage.IsNull || !platform.IsMapped(storage,
			MuiGuestUlongStorage.Size))
			return false;
		var stackRaw = Begin(ref platform, state, root);
		if (stackRaw == 0) return false;
		var stack = APTR.FromPointer(stackRaw);
		var stackBytes = MaximumDepth * MuiUDataTraversalFrame.Size;
		var depth = 1u;
		var visited = 0u;
		while (depth != 0)
		{
			if (!TryReadFrame(ref platform, state, stack, depth,
				out var frameCursor, out var current, out var nextChild))
				return Finish(ref platform, stack, stackBytes, false);
			if (nextChild == NotVisited)
			{
				if (visited++ >= MuiHeadlessLayout.MaximumTraversal)
					return Finish(ref platform, stack, stackBytes, false);
				if (Matches(ref platform, state, current, userData))
				{
					if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state,
						current, attribute, out var value))
						return Finish(ref platform, stack, stackBytes, false);
					if (!MuiGuestUlongStorageCodec.WriteValue(ref platform, storage,
						value))
						return Finish(ref platform, stack, stackBytes, false);
					return Finish(ref platform, stack, stackBytes, true);
				}
				if (!MuiUDataTraversalFrameCodec.TryWrite(ref platform, frameCursor,
					CreateFrame(current, 0)))
					return Finish(ref platform, stack, stackBytes, false);
				continue;
			}
			if (!Descend(ref platform, state, current, nextChild,
				ref depth, stack))
				return Finish(ref platform, stack, stackBytes, false);
		}
		return Finish(ref platform, stack, stackBytes, false);
	}

	public static bool Set<TPlatform>(ref TPlatform platform, APTR state,
		APTR root, uint userData, uint attribute, uint value, bool once)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var stackRaw = Begin(ref platform, state, root);
		if (stackRaw == 0) return false;
		var stack = APTR.FromPointer(stackRaw);
		var stackBytes = MaximumDepth * MuiUDataTraversalFrame.Size;
		var depth = 1u;
		var visited = 0u;
		var matched = false;
		while (depth != 0)
		{
			if (!TryReadFrame(ref platform, state, stack, depth,
				out var frameCursor, out var current, out var nextChild))
				return Finish(ref platform, stack, stackBytes, false);
			if (nextChild == NotVisited)
			{
				if (visited++ >= MuiHeadlessLayout.MaximumTraversal)
					return Finish(ref platform, stack, stackBytes, false);
				if (Matches(ref platform, state, current, userData))
				{
					if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state,
						current, attribute, value, true))
						return Finish(ref platform, stack, stackBytes, false);
					matched = true;
					if (once) return Finish(ref platform, stack, stackBytes, true);
				}
				if (!MuiUDataTraversalFrameCodec.TryWrite(ref platform, frameCursor,
					CreateFrame(current, 0)))
					return Finish(ref platform, stack, stackBytes, false);
				continue;
			}
			if (!Descend(ref platform, state, current, nextChild,
				ref depth, stack))
				return Finish(ref platform, stack, stackBytes, false);
		}
		return Finish(ref platform, stack, stackBytes, matched);
	}

	internal static bool TryReadFind<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiFindUDataMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiFindUDataMessageCodec.TryRead(ref platform, message,
			out packet) || packet.MethodId != method)
		{
			packet = default;
			return false;
		}
		return true;
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiGetUDataMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGetUDataMessageCodec.TryRead(ref platform, message,
			out packet) || packet.MethodId != method)
		{
			packet = default;
			return false;
		}
		return true;
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiSetUDataMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiSetUDataMessageCodec.TryRead(ref platform, message,
			out packet) || packet.MethodId != method)
		{
			packet = default;
			return false;
		}
		return true;
	}

	private static uint Begin<TPlatform>(ref TPlatform platform, APTR state,
		APTR root)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, root).IsNull)
			return 0;
		var stackBytes = MaximumDepth * MuiUDataTraversalFrame.Size;
		var stack = MuiHeadlessMemory.Allocate(ref platform, stackBytes);
		if (stack.IsNull) return 0;
		var cursor = default(MuiUDataTraversalCursor);
		cursor.Base = stack;
		cursor.Index = 0;
		if (!MuiUDataTraversalFrameCodec.TryWrite(ref platform, cursor,
			CreateFrame(root, NotVisited)))
		{
			platform.Free(stack, stackBytes);
			return 0;
		}
		return stack.Raw;
	}

	private static bool TryReadFrame<TPlatform>(ref TPlatform platform, APTR state,
		APTR stack, uint depth, out MuiUDataTraversalCursor frameCursor,
		out APTR current,
		out uint nextChild) where TPlatform : struct, IMuiHeadlessPlatform
	{
		frameCursor = default;
		current = APTR.Null;
		nextChild = 0;
		if (depth == 0 || depth > MaximumDepth) return false;
		frameCursor.Base = stack;
		frameCursor.Index = depth - 1;
		var frameRecord = default(MuiUDataTraversalFrame);
		if (!MuiUDataTraversalFrameCodec.TryRead(ref platform, frameCursor,
			out frameRecord)) return false;
		current = frameRecord.Object;
		nextChild = frameRecord.NextChild;
		if (current.IsNull || MuiHeadlessObjectCore.FindObject(ref platform,
			state, current).IsNull) return false;
		return true;
	}

	private static bool Descend<TPlatform>(ref TPlatform platform, APTR state,
		APTR current, uint nextChild, ref uint depth, APTR stack)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var child = MuiFamilyCore.GetChild(ref platform, state, current,
			unchecked((int)nextChild), APTR.Null);
		if (child.IsNull)
		{
			depth--;
			return true;
		}
		if (depth >= MaximumDepth || nextChild == uint.MaxValue) return false;
		var cursor = default(MuiUDataTraversalCursor);
		cursor.Base = stack;
		cursor.Index = depth - 1;
		if (!MuiUDataTraversalFrameCodec.TryWrite(ref platform, cursor,
			CreateFrame(current, nextChild + 1))) return false;
		cursor.Index = depth;
		if (!MuiUDataTraversalFrameCodec.TryWrite(ref platform, cursor,
			CreateFrame(child, NotVisited))) return false;
		depth++;
		return true;
	}

	private static MuiUDataTraversalFrame CreateFrame(APTR obj, uint nextChild)
	{
		var frame = default(MuiUDataTraversalFrame);
		frame.Object = obj;
		frame.NextChild = nextChild;
		return frame;
	}

	private static bool Matches<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint userData) where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			UserDataAttribute, out var value) && value == userData;

	private static T Finish<TPlatform, T>(ref TPlatform platform, APTR stack,
		uint stackBytes, T result) where TPlatform : struct, IMuiHeadlessPlatform
	{
		platform.Clear(stack, stackBytes);
		platform.Free(stack, stackBytes);
		return result;
	}
}
