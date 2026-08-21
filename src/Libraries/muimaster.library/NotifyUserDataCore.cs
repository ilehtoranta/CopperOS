/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
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
	public uint MethodId;
	public uint UserData;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGetUDataMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint UserData;
	public uint Attribute;
	public uint Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSetUDataMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint UserData;
	public uint Attribute;
	public uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNotifyUserDataMethodMessage
{
	internal const uint Size = 4;
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

internal static class MuiNotifyUserDataPacketFieldCursorCodec
{
	private static bool TryResolve(MuiNotifyUserDataPacketKind packet,
		MuiNotifyUserDataPacketField field, out uint offset)
	{
		switch (packet)
		{
			case MuiNotifyUserDataPacketKind.Find:
				if (field == MuiNotifyUserDataPacketField.MethodId) { offset = 0; return true; }
				if (field == MuiNotifyUserDataPacketField.UserData) { offset = 4; return true; }
				break;
			case MuiNotifyUserDataPacketKind.Get:
				if (field == MuiNotifyUserDataPacketField.MethodId) { offset = 0; return true; }
				if (field == MuiNotifyUserDataPacketField.UserData) { offset = 4; return true; }
				if (field == MuiNotifyUserDataPacketField.Attribute) { offset = 8; return true; }
				if (field == MuiNotifyUserDataPacketField.Storage) { offset = 12; return true; }
				break;
			case MuiNotifyUserDataPacketKind.Set:
				if (field == MuiNotifyUserDataPacketField.MethodId) { offset = 0; return true; }
				if (field == MuiNotifyUserDataPacketField.UserData) { offset = 4; return true; }
				if (field == MuiNotifyUserDataPacketField.Attribute) { offset = 8; return true; }
				if (field == MuiNotifyUserDataPacketField.Value) { offset = 12; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiNotifyUserDataPacketFieldCursor cursor, out APTR address)
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
		APTR message, MuiNotifyUserDataPacketKind packet,
		MuiNotifyUserDataPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiNotifyUserDataPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyUserDataPacketKind packet,
		MuiNotifyUserDataPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiNotifyUserDataPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiNotifyUserDataMessageCodec
{
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiNotifyUserDataMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiNotifyUserDataMethodMessage.Size)) return false;
		return MuiNotifyUserDataPacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiNotifyUserDataPacketKind.Find,
			MuiNotifyUserDataPacketField.MethodId, out packet.MethodId);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiUDataTraversalFrame
{
	public const uint Size = 8;
	public APTR Object;
	public uint NextChild;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiUDataTraversalCursor
{
	internal const uint EntrySize = MuiUDataTraversalFrame.Size;
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

internal static class MuiUDataTraversalFieldCursorCodec
{
	private static bool TryResolve(MuiUDataTraversalField field,
		out uint offset)
	{
		if (field == MuiUDataTraversalField.Object) { offset = 0; return true; }
		if (field == MuiUDataTraversalField.NextChild) { offset = 4; return true; }
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiUDataTraversalFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Frame.IsNull ||
			cursor.Frame.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Frame.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR frame, MuiUDataTraversalField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiUDataTraversalFieldCursor);
		cursor.Frame = frame;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR frame, MuiUDataTraversalField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiUDataTraversalFieldCursor);
		cursor.Frame = frame;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiUDataTraversalFrameCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiUDataTraversalCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiUDataTraversalCursor.EntrySize) return false;
		var offset = cursor.Index * MuiUDataTraversalCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address,
			MuiUDataTraversalCursor.EntrySize);
	}
}

internal static class MuiNotifyUserDataRecords
{
	public static bool TryReadFrame<TPlatform>(ref TPlatform platform,
		APTR address, ref MuiUDataTraversalFrame frame)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiUDataTraversalFrame.Size)) return false;
		if (!MuiUDataTraversalFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiUDataTraversalField.Object, out var rawObject) ||
			!MuiUDataTraversalFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiUDataTraversalField.NextChild, out frame.NextChild)) return false;
		frame.Object = APTR.FromPointer(rawObject);
		return true;
	}

	public static bool WriteFrame<TPlatform>(ref TPlatform platform,
		APTR address, MuiUDataTraversalFrame frame)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiUDataTraversalFrame.Size)) return false;
		return MuiUDataTraversalFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiUDataTraversalField.Object, frame.Object.Raw) &&
			MuiUDataTraversalFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiUDataTraversalField.NextChild, frame.NextChild);
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
		packet = default;
		if (message.IsNull || !platform.IsMapped(message, MuiFindUDataMessage.Size) ||
			!MuiNotifyUserDataMessageCodec.TryReadMethodId(ref platform, message,
				out var header) || header.MethodId != method) return false;
		if (!MuiNotifyUserDataPacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiNotifyUserDataPacketKind.Find,
			MuiNotifyUserDataPacketField.UserData, out packet.UserData)) return false;
		packet.MethodId = header.MethodId;
		return true;
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiGetUDataMessage packet)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message, MuiGetUDataMessage.Size) ||
			!MuiNotifyUserDataMessageCodec.TryReadMethodId(ref platform, message,
				out var header) || header.MethodId != method) return false;
		if (!MuiNotifyUserDataPacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiNotifyUserDataPacketKind.Get,
			MuiNotifyUserDataPacketField.UserData, out packet.UserData) ||
			!MuiNotifyUserDataPacketFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiNotifyUserDataPacketKind.Get,
				MuiNotifyUserDataPacketField.Attribute, out packet.Attribute) ||
			!MuiNotifyUserDataPacketFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiNotifyUserDataPacketKind.Get,
				MuiNotifyUserDataPacketField.Storage, out packet.Storage)) return false;
		packet.MethodId = header.MethodId;
		return true;
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiSetUDataMessage packet)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message, MuiSetUDataMessage.Size) ||
			!MuiNotifyUserDataMessageCodec.TryReadMethodId(ref platform, message,
				out var header) || header.MethodId != method) return false;
		if (!MuiNotifyUserDataPacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiNotifyUserDataPacketKind.Set,
			MuiNotifyUserDataPacketField.UserData, out packet.UserData) ||
			!MuiNotifyUserDataPacketFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiNotifyUserDataPacketKind.Set,
				MuiNotifyUserDataPacketField.Attribute, out packet.Attribute) ||
			!MuiNotifyUserDataPacketFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiNotifyUserDataPacketKind.Set,
				MuiNotifyUserDataPacketField.Value, out packet.Value)) return false;
		packet.MethodId = header.MethodId;
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
		var cursor = default(MuiUDataTraversalCursor);
		cursor.Base = stack;
		cursor.Index = depth - 1;
		if (!MuiUDataTraversalFrameCodec.TryGetEntry(ref platform, cursor,
			out var frame)) return false;
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
		var cursor = default(MuiUDataTraversalCursor);
		cursor.Base = stack;
		cursor.Index = depth;
		if (!MuiUDataTraversalFrameCodec.TryGetEntry(ref platform, cursor,
			out var childFrame)) return false;
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
