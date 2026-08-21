/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Fixed MorphOS Area drag method records.  The public method packets are
// value-type records; only this codec owns their packed guest representation.
// This keeps the first Area drag seam independent of managed tuples, arrays,
// or pointer arithmetic in the dispatcher.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDragMethodMessage
{
	public const uint Size = 4;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDragBeginMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Object;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDragDropMessage
{
	public const uint Size = 20;
	public uint MethodId;
	public uint Object;
	public int X;
	public int Y;
	public uint Qualifier;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDragEventMessage
{
	public const uint Size = 32;
	public uint MethodId;
	public uint Window;
	public uint Object;
	public uint DragImage;
	public uint IntuiMessage;
	public int MuiKey;
	public uint MousePointerType;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDragFinishMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Object;
	public int DropFollows;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDragQueryMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Object;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDragReportMessage
{
	public const uint Size = 24;
	public uint MethodId;
	public uint Object;
	public int X;
	public int Y;
	public int Update;
	public uint Qualifier;
}

internal enum MuiAreaDragPacketKind : byte
{
	Method,
	Begin,
	Drop,
	Event,
	Finish,
	Query,
	Report,
}

internal enum MuiAreaDragField : byte
{
	MethodId,
	Object,
	Window,
	DragImage,
	IntuiMessage,
	MuiKey,
	MousePointerType,
	Flags,
	X,
	Y,
	Qualifier,
	DropFollows,
	Update,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDragFieldCursor
{
	internal APTR Message;
	internal MuiAreaDragPacketKind Packet;
	internal MuiAreaDragField Field;
}

internal static class MuiAreaDragFieldCursorCodec
{
	private static bool TryResolve(MuiAreaDragPacketKind packet,
		MuiAreaDragField field, out uint offset)
	{
		switch (packet)
		{
			case MuiAreaDragPacketKind.Method:
				if (field == MuiAreaDragField.MethodId) { offset = 0; return true; }
				break;
			case MuiAreaDragPacketKind.Begin:
				if (field == MuiAreaDragField.MethodId) { offset = 0; return true; }
				if (field == MuiAreaDragField.Object) { offset = 4; return true; }
				break;
			case MuiAreaDragPacketKind.Drop:
				if (field == MuiAreaDragField.MethodId) { offset = 0; return true; }
				if (field == MuiAreaDragField.Object) { offset = 4; return true; }
				if (field == MuiAreaDragField.X) { offset = 8; return true; }
				if (field == MuiAreaDragField.Y) { offset = 12; return true; }
				if (field == MuiAreaDragField.Qualifier) { offset = 16; return true; }
				break;
			case MuiAreaDragPacketKind.Event:
				if (field == MuiAreaDragField.MethodId) { offset = 0; return true; }
				if (field == MuiAreaDragField.Window) { offset = 4; return true; }
				if (field == MuiAreaDragField.Object) { offset = 8; return true; }
				if (field == MuiAreaDragField.DragImage) { offset = 12; return true; }
				if (field == MuiAreaDragField.IntuiMessage) { offset = 16; return true; }
				if (field == MuiAreaDragField.MuiKey) { offset = 20; return true; }
				if (field == MuiAreaDragField.MousePointerType) { offset = 24; return true; }
				if (field == MuiAreaDragField.Flags) { offset = 28; return true; }
				break;
			case MuiAreaDragPacketKind.Finish:
				if (field == MuiAreaDragField.MethodId) { offset = 0; return true; }
				if (field == MuiAreaDragField.Object) { offset = 4; return true; }
				if (field == MuiAreaDragField.DropFollows) { offset = 8; return true; }
				break;
			case MuiAreaDragPacketKind.Query:
				if (field == MuiAreaDragField.MethodId) { offset = 0; return true; }
				if (field == MuiAreaDragField.Object) { offset = 4; return true; }
				break;
			case MuiAreaDragPacketKind.Report:
				if (field == MuiAreaDragField.MethodId) { offset = 0; return true; }
				if (field == MuiAreaDragField.Object) { offset = 4; return true; }
				if (field == MuiAreaDragField.X) { offset = 8; return true; }
				if (field == MuiAreaDragField.Y) { offset = 12; return true; }
				if (field == MuiAreaDragField.Update) { offset = 16; return true; }
				if (field == MuiAreaDragField.Qualifier) { offset = 20; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaDragFieldCursor cursor, out APTR address)
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
		APTR message, MuiAreaDragPacketKind packet, MuiAreaDragField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaDragFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaDragPacketKind packet, MuiAreaDragField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaDragFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaDragMessageCodec
{
	internal const uint DragBegin = 0x8042C03Au;
	internal const uint DragDrop = 0x8042C555u;
	internal const uint DragEvent = 0x8042B774u;
	internal const uint DragFinish = 0x804251F0u;
	internal const uint DragQuery = 0x80420261u;
	internal const uint DragReport = 0x8042EDADu;

	internal static bool IsMethod(uint method) => method == DragBegin ||
		method == DragDrop || method == DragEvent || method == DragFinish ||
		method == DragQuery || method == DragReport;

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDragMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiAreaDragMethodMessage.Size)) return false;
		if (!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiAreaDragPacketKind.Method, MuiAreaDragField.MethodId,
			out packet.MethodId)) return false;
		return true;
	}

	internal static bool TryReadBegin<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDragBeginMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiAreaDragBeginMessage.Size,
			DragBegin)) return false;
		packet.MethodId = DragBegin;
		return MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiAreaDragPacketKind.Begin, MuiAreaDragField.Object,
			out packet.Object);
	}

	internal static bool WriteBegin<TPlatform>(ref TPlatform platform,
		APTR message, uint source)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsStorage(ref platform, message, MuiAreaDragBeginMessage.Size))
			return false;
		return MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiAreaDragPacketKind.Begin, MuiAreaDragField.MethodId, DragBegin) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Begin, MuiAreaDragField.Object, source);
	}

	internal static bool TryReadDrop<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDragDropMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiAreaDragDropMessage.Size,
			DragDrop)) return false;
		packet.MethodId = DragDrop;
		if (!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiAreaDragPacketKind.Drop, MuiAreaDragField.Object,
			out packet.Object) ||
			!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiAreaDragPacketKind.Drop, MuiAreaDragField.X, out var rawX) ||
			!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiAreaDragPacketKind.Drop, MuiAreaDragField.Y, out var rawY) ||
			!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiAreaDragPacketKind.Drop, MuiAreaDragField.Qualifier,
				out packet.Qualifier)) return false;
		packet.X = unchecked((int)rawX);
		packet.Y = unchecked((int)rawY);
		return true;
	}

	internal static bool WriteDrop<TPlatform>(ref TPlatform platform,
		APTR message, uint source, int x, int y, uint qualifier)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsStorage(ref platform, message, MuiAreaDragDropMessage.Size))
			return false;
		return MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiAreaDragPacketKind.Drop, MuiAreaDragField.MethodId, DragDrop) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Drop, MuiAreaDragField.Object, source) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Drop, MuiAreaDragField.X,
				unchecked((uint)x)) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Drop, MuiAreaDragField.Y,
				unchecked((uint)y)) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Drop, MuiAreaDragField.Qualifier,
				qualifier);
	}

	internal static bool TryReadEvent<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDragEventMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiAreaDragEventMessage.Size,
			DragEvent)) return false;
		packet.MethodId = DragEvent;
		if (!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiAreaDragPacketKind.Event, MuiAreaDragField.Window,
			out packet.Window) ||
			!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiAreaDragPacketKind.Event, MuiAreaDragField.Object,
				out packet.Object) ||
			!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiAreaDragPacketKind.Event, MuiAreaDragField.DragImage,
				out packet.DragImage) ||
			!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiAreaDragPacketKind.Event, MuiAreaDragField.IntuiMessage,
				out packet.IntuiMessage) ||
			!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiAreaDragPacketKind.Event, MuiAreaDragField.MuiKey,
				out var rawMuiKey) ||
			!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiAreaDragPacketKind.Event, MuiAreaDragField.MousePointerType,
				out packet.MousePointerType) ||
			!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiAreaDragPacketKind.Event, MuiAreaDragField.Flags,
				out packet.Flags)) return false;
		packet.MuiKey = unchecked((int)rawMuiKey);
		return true;
	}

	internal static bool WriteEvent<TPlatform>(ref TPlatform platform,
		APTR message, uint window, uint source, uint dragImage, uint intuiMessage,
		int muiKey, uint mousePointerType, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsStorage(ref platform, message, MuiAreaDragEventMessage.Size))
			return false;
		return MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiAreaDragPacketKind.Event, MuiAreaDragField.MethodId, DragEvent) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Event, MuiAreaDragField.Window, window) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Event, MuiAreaDragField.Object, source) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Event, MuiAreaDragField.DragImage,
				dragImage) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Event, MuiAreaDragField.IntuiMessage,
				intuiMessage) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Event, MuiAreaDragField.MuiKey,
				unchecked((uint)muiKey)) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Event, MuiAreaDragField.MousePointerType,
				mousePointerType) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Event, MuiAreaDragField.Flags, flags);
	}

	internal static bool TryReadFinish<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDragFinishMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiAreaDragFinishMessage.Size,
			DragFinish)) return false;
		packet.MethodId = DragFinish;
		if (!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiAreaDragPacketKind.Finish, MuiAreaDragField.Object,
			out packet.Object) ||
			!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiAreaDragPacketKind.Finish, MuiAreaDragField.DropFollows,
				out var rawDropFollows)) return false;
		packet.DropFollows = unchecked((int)rawDropFollows);
		return true;
	}

	internal static bool WriteFinish<TPlatform>(ref TPlatform platform,
		APTR message, uint source, int dropFollows)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsStorage(ref platform, message, MuiAreaDragFinishMessage.Size))
			return false;
		return MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiAreaDragPacketKind.Finish, MuiAreaDragField.MethodId, DragFinish) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Finish, MuiAreaDragField.Object, source) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Finish, MuiAreaDragField.DropFollows,
				unchecked((uint)dropFollows));
	}

	internal static bool TryReadQuery<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDragQueryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiAreaDragQueryMessage.Size,
			DragQuery)) return false;
		packet.MethodId = DragQuery;
		return MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiAreaDragPacketKind.Query, MuiAreaDragField.Object,
			out packet.Object);
	}

	internal static bool WriteQuery<TPlatform>(ref TPlatform platform,
		APTR message, uint source)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsStorage(ref platform, message, MuiAreaDragQueryMessage.Size))
			return false;
		return MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiAreaDragPacketKind.Query, MuiAreaDragField.MethodId, DragQuery) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Query, MuiAreaDragField.Object, source);
	}

	internal static bool TryReadReport<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDragReportMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiAreaDragReportMessage.Size,
			DragReport)) return false;
		packet.MethodId = DragReport;
		if (!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiAreaDragPacketKind.Report, MuiAreaDragField.Object,
			out packet.Object) ||
			!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiAreaDragPacketKind.Report, MuiAreaDragField.X, out var rawX) ||
			!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiAreaDragPacketKind.Report, MuiAreaDragField.Y, out var rawY) ||
			!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiAreaDragPacketKind.Report, MuiAreaDragField.Update,
				out var rawUpdate) ||
			!MuiAreaDragFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiAreaDragPacketKind.Report, MuiAreaDragField.Qualifier,
				out packet.Qualifier)) return false;
		packet.X = unchecked((int)rawX);
		packet.Y = unchecked((int)rawY);
		packet.Update = unchecked((int)rawUpdate);
		return true;
	}

	internal static bool WriteReport<TPlatform>(ref TPlatform platform,
		APTR message, uint source, int x, int y, int update, uint qualifier)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsStorage(ref platform, message, MuiAreaDragReportMessage.Size))
			return false;
		return MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiAreaDragPacketKind.Report, MuiAreaDragField.MethodId, DragReport) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Report, MuiAreaDragField.Object, source) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Report, MuiAreaDragField.X,
				unchecked((uint)x)) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Report, MuiAreaDragField.Y,
				unchecked((uint)y)) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Report, MuiAreaDragField.Update,
				unchecked((uint)update)) &&
			MuiAreaDragFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiAreaDragPacketKind.Report, MuiAreaDragField.Qualifier,
				qualifier);
	}

	private static bool IsPacket<TPlatform>(ref TPlatform platform, APTR message,
		uint size, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsStorage(ref platform, message, size) ||
			!TryReadMethodId(ref platform, message, out var header)) return false;
		return header.MethodId == method;
	}

	private static bool IsStorage<TPlatform>(ref TPlatform platform, APTR message,
		uint size) where TPlatform : struct, IMuiGuestMemory =>
		message.IsNotNull && platform.IsMapped(message, size);
}
