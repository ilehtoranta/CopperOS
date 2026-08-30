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
	public const uint MethodIdOffset = 0;
	public const uint UserDataOffset = 4;
	public uint MethodId;
	public uint UserData;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGetUDataMessage
{
	public const uint Size = 16;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint UserDataOffset = 4;
	public const uint AttributeOffset = 8;
	public const uint StorageOffset = 12;
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
	public const uint MethodIdOffset = 0;
	public const uint UserDataOffset = 4;
	public const uint AttributeOffset = 8;
	public const uint ValueOffset = 12;
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
	internal const uint MethodIdOffset = 0;
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
// Packet-specific record constants own the wire layout; callers select named
// fields rather than carrying numeric offsets through the dispatcher.
internal static class MuiNotifyUserDataPacketMemoryCodec
{
	private static bool TryResolve(MuiNotifyUserDataPacketKind packet,
		MuiNotifyUserDataPacketField field, out uint size, out uint offset)
	{
		size = 0;
		switch (packet)
		{
			case MuiNotifyUserDataPacketKind.Find:
				size = MuiFindUDataMessage.Size;
				if (field == MuiNotifyUserDataPacketField.MethodId) { offset = MuiFindUDataMessage.MethodIdOffset; return true; }
				if (field == MuiNotifyUserDataPacketField.UserData) { offset = MuiFindUDataMessage.UserDataOffset; return true; }
				break;
			case MuiNotifyUserDataPacketKind.Get:
				size = MuiGetUDataMessage.Size;
				if (field == MuiNotifyUserDataPacketField.MethodId) { offset = MuiGetUDataMessage.MethodIdOffset; return true; }
				if (field == MuiNotifyUserDataPacketField.UserData) { offset = MuiGetUDataMessage.UserDataOffset; return true; }
				if (field == MuiNotifyUserDataPacketField.Attribute) { offset = MuiGetUDataMessage.AttributeOffset; return true; }
				if (field == MuiNotifyUserDataPacketField.Storage) { offset = MuiGetUDataMessage.StorageOffset; return true; }
				break;
			case MuiNotifyUserDataPacketKind.Set:
				size = MuiSetUDataMessage.Size;
				if (field == MuiNotifyUserDataPacketField.MethodId) { offset = MuiSetUDataMessage.MethodIdOffset; return true; }
				if (field == MuiNotifyUserDataPacketField.UserData) { offset = MuiSetUDataMessage.UserDataOffset; return true; }
				if (field == MuiNotifyUserDataPacketField.Attribute) { offset = MuiSetUDataMessage.AttributeOffset; return true; }
				if (field == MuiNotifyUserDataPacketField.Value) { offset = MuiSetUDataMessage.ValueOffset; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyUserDataPacketKind packet,
		MuiNotifyUserDataPacketField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var size, out var offset) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, size))
			return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiFindUDataMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyUserDataPacketKind packet,
		MuiNotifyUserDataPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyUserDataPacketKind packet,
		MuiNotifyUserDataPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for existing typed cursor diagnostics.
internal static class MuiNotifyUserDataPacketFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiNotifyUserDataPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiNotifyUserDataPacketMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Packet, cursor.Field, out address);

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
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiNotifyUserDataMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMethodId)) return false;
		methodId = rawMethodId;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
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
	public const uint ObjectOffset = 0;
	public const uint NextChildOffset = 4;
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
	private static bool TryResolve(MuiUDataTraversalField field,
		out uint offset)
	{
		if (field == MuiUDataTraversalField.Object) { offset = MuiUDataTraversalFrame.ObjectOffset; return true; }
		if (field == MuiUDataTraversalField.NextChild) { offset = MuiUDataTraversalFrame.NextChildOffset; return true; }
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR frame, MuiUDataTraversalField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || frame.IsNull ||
			frame.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(frame, MuiUDataTraversalFrame.Size)) return false;
		address = APTR.FromPointer(frame.Raw + offset);
		return platform.IsMapped(address, MuiUDataTraversalFrame.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR frame, MuiUDataTraversalField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, frame, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR frame, MuiUDataTraversalField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, frame, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
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
			cursor.Frame, cursor.Field, out address);

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
				out var frameRaw, out var current, out var nextChild))
				return Finish(ref platform, stack, stackBytes, APTR.Null);
			var frame = APTR.FromPointer(frameRaw);
			if (nextChild == NotVisited)
			{
				if (visited++ >= MuiHeadlessLayout.MaximumTraversal)
					return Finish(ref platform, stack, stackBytes, APTR.Null);
				if (Matches(ref platform, state, current, userData))
					return Finish(ref platform, stack, stackBytes, current);
				if (!MuiNotifyUserDataRecords.WriteFrame(ref platform, frame,
					CreateFrame(current, 0)))
					return Finish(ref platform, stack, stackBytes, APTR.Null);
				continue;
			}
			if (!Descend(ref platform, state, current, frame, nextChild,
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
				out var frameRaw, out var current, out var nextChild))
				return Finish(ref platform, stack, stackBytes, false);
			var frame = APTR.FromPointer(frameRaw);
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
				if (!MuiNotifyUserDataRecords.WriteFrame(ref platform, frame,
					CreateFrame(current, 0)))
					return Finish(ref platform, stack, stackBytes, false);
				continue;
			}
			if (!Descend(ref platform, state, current, frame, nextChild,
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
				out var frameRaw, out var current, out var nextChild))
				return Finish(ref platform, stack, stackBytes, false);
			var frame = APTR.FromPointer(frameRaw);
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
				if (!MuiNotifyUserDataRecords.WriteFrame(ref platform, frame,
					CreateFrame(current, 0)))
					return Finish(ref platform, stack, stackBytes, false);
				continue;
			}
			if (!Descend(ref platform, state, current, frame, nextChild,
				ref depth, stack))
				return Finish(ref platform, stack, stackBytes, false);
		}
		return Finish(ref platform, stack, stackBytes, matched);
	}

	internal static bool TryReadFind<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiFindUDataMessage packet)
		where TPlatform : struct, IMuiHeadlessPlatform
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
		where TPlatform : struct, IMuiHeadlessPlatform
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
		where TPlatform : struct, IMuiHeadlessPlatform
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
		if (!MuiNotifyUserDataRecords.WriteFrame(ref platform, stack,
			CreateFrame(root, NotVisited)))
		{
			platform.Free(stack, stackBytes);
			return 0;
		}
		return stack.Raw;
	}

	private static bool TryReadFrame<TPlatform>(ref TPlatform platform, APTR state,
		APTR stack, uint depth, out uint frameRaw, out APTR current,
		out uint nextChild) where TPlatform : struct, IMuiHeadlessPlatform
	{
		frameRaw = 0;
		current = APTR.Null;
		nextChild = 0;
		if (depth == 0 || depth > MaximumDepth) return false;
		if (!MuiUDataTraversalFrameVectorMemoryCodec.TryGetEntry(ref platform,
			stack, depth - 1, out var frame)) return false;
		var frameRecord = default(MuiUDataTraversalFrame);
		if (!MuiNotifyUserDataRecords.TryReadFrame(ref platform, frame,
			ref frameRecord)) return false;
		current = frameRecord.Object;
		nextChild = frameRecord.NextChild;
		if (current.IsNull || MuiHeadlessObjectCore.FindObject(ref platform,
			state, current).IsNull) return false;
		frameRaw = frame.Raw;
		return true;
	}

	private static bool Descend<TPlatform>(ref TPlatform platform, APTR state,
		APTR current, APTR frame, uint nextChild, ref uint depth, APTR stack)
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
		if (!MuiNotifyUserDataRecords.WriteFrame(ref platform, frame,
			CreateFrame(current, nextChild + 1))) return false;
		if (!MuiUDataTraversalFrameVectorMemoryCodec.TryGetEntry(ref platform,
			stack, depth, out var childFrame)) return false;
		if (!MuiNotifyUserDataRecords.WriteFrame(ref platform, childFrame,
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
