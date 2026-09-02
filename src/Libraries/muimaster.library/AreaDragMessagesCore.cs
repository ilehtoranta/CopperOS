/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
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
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDragBeginMessage
{
	public const uint Size = 8;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint ObjectOffset = 4;
	public uint MethodId;
	public uint Object;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDoDragMessage
{
	public const uint Size = 16;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint TouchXOffset = 4;
	public const uint TouchYOffset = 8;
	public const uint FlagsOffset = 12;
	public uint MethodId;
	public int TouchX;
	public int TouchY;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaCreateDragImageMessage
{
	public const uint Size = 16;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint TouchXOffset = 4;
	public const uint TouchYOffset = 8;
	public const uint FlagsOffset = 12;
	public uint MethodId;
	public int TouchX;
	public int TouchY;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDeleteDragImageMessage
{
	public const uint Size = 8;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint DragImageOffset = 4;
	public uint MethodId;
	public uint DragImage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDragDropMessage
{
	public const uint Size = 20;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint ObjectOffset = 4;
	public const uint XOffset = 8;
	public const uint YOffset = 12;
	public const uint QualifierOffset = 16;
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
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint WindowOffset = 4;
	public const uint ObjectOffset = 8;
	public const uint DragImageOffset = 12;
	public const uint IntuiMessageOffset = 16;
	public const uint MuiKeyOffset = 20;
	public const uint MousePointerTypeOffset = 24;
	public const uint FlagsOffset = 28;
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
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint ObjectOffset = 4;
	public const uint DropFollowsOffset = 8;
	public uint MethodId;
	public uint Object;
	public int DropFollows;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDragQueryMessage
{
	public const uint Size = 8;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint ObjectOffset = 4;
	public uint MethodId;
	public uint Object;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDragReportMessage
{
	public const uint Size = 24;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint ObjectOffset = 4;
	public const uint XOffset = 8;
	public const uint YOffset = 12;
	public const uint UpdateOffset = 16;
	public const uint QualifierOffset = 20;
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
	DoDrag,
	Drop,
	Event,
	Finish,
	Query,
	Report,
	CreateImage,
	DeleteImage,
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
	TouchX,
	TouchY,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDragFieldCursor
{
	internal APTR Message;
	internal MuiAreaDragPacketKind Packet;
	internal MuiAreaDragField Field;
}

// Named packet adapters keep the MorphOS ABI layout in one place. Callers
// select packet/field enums and receive bounded addresses; raw offsets never
// cross this boundary.
internal static class MuiAreaDragMessageMemoryCodec
{
	private static bool TryGetPacketSize(MuiAreaDragPacketKind packet,
		out uint size)
	{
		switch (packet)
		{
			case MuiAreaDragPacketKind.Method:
				size = MuiAreaDragMethodMessage.Size;
				return true;
			case MuiAreaDragPacketKind.Begin:
				size = MuiAreaDragBeginMessage.Size;
				return true;
			case MuiAreaDragPacketKind.DoDrag:
			case MuiAreaDragPacketKind.CreateImage:
				size = MuiAreaDoDragMessage.Size;
				return true;
			case MuiAreaDragPacketKind.Drop:
				size = MuiAreaDragDropMessage.Size;
				return true;
			case MuiAreaDragPacketKind.Event:
				size = MuiAreaDragEventMessage.Size;
				return true;
			case MuiAreaDragPacketKind.Finish:
				size = MuiAreaDragFinishMessage.Size;
				return true;
			case MuiAreaDragPacketKind.Query:
				size = MuiAreaDragQueryMessage.Size;
				return true;
			case MuiAreaDragPacketKind.Report:
				size = MuiAreaDragReportMessage.Size;
				return true;
			case MuiAreaDragPacketKind.DeleteImage:
				size = MuiAreaDeleteDragImageMessage.Size;
				return true;
		}
		size = 0;
		return false;
	}

	private static bool TryResolve(MuiAreaDragPacketKind packet,
		MuiAreaDragField field, out uint offset)
	{
		switch (packet)
		{
			case MuiAreaDragPacketKind.Method:
				if (field == MuiAreaDragField.MethodId)
				{
					offset = MuiAreaDragMethodMessage.MethodIdOffset;
					return true;
				}
				break;
			case MuiAreaDragPacketKind.Begin:
				if (field == MuiAreaDragField.MethodId)
				{
					offset = MuiAreaDragBeginMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiAreaDragField.Object)
				{
					offset = MuiAreaDragBeginMessage.ObjectOffset;
					return true;
				}
				break;
			case MuiAreaDragPacketKind.DoDrag:
				if (field == MuiAreaDragField.MethodId)
				{
					offset = MuiAreaDoDragMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiAreaDragField.TouchX)
				{
					offset = MuiAreaDoDragMessage.TouchXOffset;
					return true;
				}
				if (field == MuiAreaDragField.TouchY)
				{
					offset = MuiAreaDoDragMessage.TouchYOffset;
					return true;
				}
				if (field == MuiAreaDragField.Flags)
				{
					offset = MuiAreaDoDragMessage.FlagsOffset;
					return true;
				}
				break;
			case MuiAreaDragPacketKind.Drop:
				if (field == MuiAreaDragField.MethodId)
				{
					offset = MuiAreaDragDropMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiAreaDragField.Object)
				{
					offset = MuiAreaDragDropMessage.ObjectOffset;
					return true;
				}
				if (field == MuiAreaDragField.X)
				{
					offset = MuiAreaDragDropMessage.XOffset;
					return true;
				}
				if (field == MuiAreaDragField.Y)
				{
					offset = MuiAreaDragDropMessage.YOffset;
					return true;
				}
				if (field == MuiAreaDragField.Qualifier)
				{
					offset = MuiAreaDragDropMessage.QualifierOffset;
					return true;
				}
				break;
			case MuiAreaDragPacketKind.Event:
				if (field == MuiAreaDragField.MethodId)
				{
					offset = MuiAreaDragEventMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiAreaDragField.Window)
				{
					offset = MuiAreaDragEventMessage.WindowOffset;
					return true;
				}
				if (field == MuiAreaDragField.Object)
				{
					offset = MuiAreaDragEventMessage.ObjectOffset;
					return true;
				}
				if (field == MuiAreaDragField.DragImage)
				{
					offset = MuiAreaDragEventMessage.DragImageOffset;
					return true;
				}
				if (field == MuiAreaDragField.IntuiMessage)
				{
					offset = MuiAreaDragEventMessage.IntuiMessageOffset;
					return true;
				}
				if (field == MuiAreaDragField.MuiKey)
				{
					offset = MuiAreaDragEventMessage.MuiKeyOffset;
					return true;
				}
				if (field == MuiAreaDragField.MousePointerType)
				{
					offset = MuiAreaDragEventMessage.MousePointerTypeOffset;
					return true;
				}
				if (field == MuiAreaDragField.Flags)
				{
					offset = MuiAreaDragEventMessage.FlagsOffset;
					return true;
				}
				break;
			case MuiAreaDragPacketKind.Finish:
				if (field == MuiAreaDragField.MethodId)
				{
					offset = MuiAreaDragFinishMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiAreaDragField.Object)
				{
					offset = MuiAreaDragFinishMessage.ObjectOffset;
					return true;
				}
				if (field == MuiAreaDragField.DropFollows)
				{
					offset = MuiAreaDragFinishMessage.DropFollowsOffset;
					return true;
				}
				break;
			case MuiAreaDragPacketKind.Query:
				if (field == MuiAreaDragField.MethodId)
				{
					offset = MuiAreaDragQueryMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiAreaDragField.Object)
				{
					offset = MuiAreaDragQueryMessage.ObjectOffset;
					return true;
				}
				break;
			case MuiAreaDragPacketKind.Report:
				if (field == MuiAreaDragField.MethodId)
				{
					offset = MuiAreaDragReportMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiAreaDragField.Object)
				{
					offset = MuiAreaDragReportMessage.ObjectOffset;
					return true;
				}
				if (field == MuiAreaDragField.X)
				{
					offset = MuiAreaDragReportMessage.XOffset;
					return true;
				}
				if (field == MuiAreaDragField.Y)
				{
					offset = MuiAreaDragReportMessage.YOffset;
					return true;
				}
				if (field == MuiAreaDragField.Update)
				{
					offset = MuiAreaDragReportMessage.UpdateOffset;
					return true;
				}
				if (field == MuiAreaDragField.Qualifier)
				{
					offset = MuiAreaDragReportMessage.QualifierOffset;
					return true;
				}
				break;
			case MuiAreaDragPacketKind.CreateImage:
				if (field == MuiAreaDragField.MethodId)
				{
					offset = MuiAreaCreateDragImageMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiAreaDragField.TouchX)
				{
					offset = MuiAreaCreateDragImageMessage.TouchXOffset;
					return true;
				}
				if (field == MuiAreaDragField.TouchY)
				{
					offset = MuiAreaCreateDragImageMessage.TouchYOffset;
					return true;
				}
				if (field == MuiAreaDragField.Flags)
				{
					offset = MuiAreaCreateDragImageMessage.FlagsOffset;
					return true;
				}
				break;
			case MuiAreaDragPacketKind.DeleteImage:
				if (field == MuiAreaDragField.MethodId)
				{
					offset = MuiAreaDeleteDragImageMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiAreaDragField.DragImage)
				{
					offset = MuiAreaDeleteDragImageMessage.DragImageOffset;
					return true;
				}
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
			!TryGetPacketSize(cursor.Packet, out var packetSize) ||
			cursor.Message.IsNull || cursor.Message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Message, packetSize))
			return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, MuiAreaDragMethodMessage.FieldSize);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaDragPacketKind packet, MuiAreaDragField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaDragFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address);
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

// Compatibility adapter retained for callers that still construct the typed
// drag field cursor. Live drag packet consumers use the named packet memory
// codec directly.
internal static class MuiAreaDragFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaDragFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaDragMessageMemoryCodec.TryGetAddress(ref platform, cursor,
			out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaDragPacketKind packet, MuiAreaDragField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaDragMessageMemoryCodec.TryReadUInt32(ref platform, message, packet,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaDragPacketKind packet, MuiAreaDragField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaDragMessageMemoryCodec.TryWriteUInt32(ref platform, message, packet,
		field, value);
}

internal static class MuiAreaCreateDragImageMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaCreateDragImageMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaCreateDragImageMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawTouchX) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawTouchY) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.TouchX = unchecked((int)rawTouchX);
		value.TouchY = unchecked((int)rawTouchY);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaCreateDragImageMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaCreateDragImageMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.TouchX)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.TouchY)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiAreaDragBeginMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaDragBeginMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDragBeginMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Object) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaDragBeginMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDragBeginMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Object)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiAreaDragDropMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaDragDropMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDragDropMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Object) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawX) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawY) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Qualifier) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.X = unchecked((int)rawX);
		value.Y = unchecked((int)rawY);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaDragDropMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDragDropMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Object) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.X)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.Y)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Qualifier)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiAreaDragQueryMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaDragQueryMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDragQueryMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Object) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaDragQueryMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDragQueryMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Object)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiAreaDragFinishMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaDragFinishMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDragFinishMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Object) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawDropFollows) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.DropFollows = unchecked((int)rawDropFollows);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaDragFinishMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDragFinishMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Object) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.DropFollows))) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiAreaDragReportMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaDragReportMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDragReportMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Object) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawX) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawY) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawUpdate) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Qualifier) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.X = unchecked((int)rawX);
		value.Y = unchecked((int)rawY);
		value.Update = unchecked((int)rawUpdate);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaDragReportMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDragReportMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Object) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.X)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.Y)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.Update)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Qualifier)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiAreaDragEventMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaDragEventMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDragEventMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Window) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Object) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.DragImage) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.IntuiMessage) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMuiKey) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MousePointerType) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.MuiKey = unchecked((int)rawMuiKey);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaDragEventMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDragEventMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Window) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Object) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.DragImage) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.IntuiMessage) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.MuiKey)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MousePointerType) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
}
}

internal static class MuiAreaDoDragMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaDoDragMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDoDragMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawTouchX) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawTouchY) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.TouchX = unchecked((int)rawTouchX);
		value.TouchY = unchecked((int)rawTouchY);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaDoDragMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDoDragMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.TouchX)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.TouchY)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiAreaDeleteDragImageMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaDeleteDragImageMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDeleteDragImageMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.DragImage) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaDeleteDragImageMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDeleteDragImageMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.DragImage)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiAreaDragMessageCodec
{
	// MorphOS MUIV_DoDrag_Async requests provider-owned asynchronous drag
	// scheduling. The core remains freestanding and records the flag in the
	// typed route sample; no managed task or exception path is introduced.
	internal const uint DoDragAsync = 1u;
	internal const uint DoDrag = 0x804216BBu;
	internal const uint DragBegin = 0x8042C03Au;
	internal const uint DragDrop = 0x8042C555u;
	internal const uint DragEvent = 0x8042B774u;
	internal const uint DragFinish = 0x804251F0u;
	internal const uint DragQuery = 0x80420261u;
	internal const uint DragReport = 0x8042EDADu;
	internal const uint CreateDragImage = 0x8042EB6Fu;
	internal const uint DeleteDragImage = 0x80423037u;

	internal static bool IsMethod(uint method) => method == DoDrag ||
		method == DragBegin ||
		method == DragDrop || method == DragEvent || method == DragFinish ||
		method == DragQuery || method == DragReport ||
		method == CreateDragImage || method == DeleteDragImage;

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDragMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadMethodIdValue(ref platform, message, out var methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	// The method-only packet is a complete one-ULONG named record. Keep a
	// scalar-safe pair beside the typed record so freestanding callers avoid
	// passing a one-field struct through the native ABI.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestUlongStorageCodec.WriteValue(ref platform, message, methodId);

	// Selector admission stays scalar for callers that only need MethodID, but
	// it is read from the named one-ULONG method record in declaration order.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, message,
			out methodId);
	}

	internal static bool TryReadBegin<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDragBeginMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDragBeginMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == DragBegin;
	}

	internal static bool WriteBegin<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaDragBeginMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return packet.MethodId == DragBegin &&
			MuiAreaDragBeginMessageCodec.Write(ref platform, message, packet);
	}

	internal static bool WriteBegin<TPlatform>(ref TPlatform platform,
		APTR message, uint source)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaDragBeginMessage);
		packet.MethodId = DragBegin;
		packet.Object = source;
		return WriteBegin(ref platform, message, packet);
	}

	internal static bool TryReadDoDrag<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDoDragMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDoDragMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == DoDrag;
	}

	internal static bool WriteDoDrag<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaDoDragMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return packet.MethodId == DoDrag &&
			MuiAreaDoDragMessageCodec.Write(ref platform, message, packet);
	}

	internal static bool WriteDoDrag<TPlatform>(ref TPlatform platform,
		APTR message, int touchX, int touchY, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaDoDragMessage);
		packet.MethodId = DoDrag;
		packet.TouchX = touchX;
		packet.TouchY = touchY;
		packet.Flags = flags;
		return WriteDoDrag(ref platform, message, packet);
	}

	internal static bool TryReadCreateDragImage<TPlatform>(
		ref TPlatform platform, APTR message,
		out MuiAreaCreateDragImageMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaCreateDragImageMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == CreateDragImage;
	}

	internal static bool WriteCreateDragImage<TPlatform>(
		ref TPlatform platform, APTR message,
		MuiAreaCreateDragImageMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return packet.MethodId == CreateDragImage &&
			MuiAreaCreateDragImageMessageCodec.Write(ref platform, message, packet);
	}

	internal static bool WriteCreateDragImage<TPlatform>(
		ref TPlatform platform, APTR message, int touchX, int touchY,
		uint flags) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaCreateDragImageMessage);
		packet.MethodId = CreateDragImage;
		packet.TouchX = touchX;
		packet.TouchY = touchY;
		packet.Flags = flags;
		return WriteCreateDragImage(ref platform, message, packet);
	}

	internal static bool TryReadDeleteDragImage<TPlatform>(
		ref TPlatform platform, APTR message,
		out MuiAreaDeleteDragImageMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDeleteDragImageMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == DeleteDragImage;
	}

	internal static bool WriteDeleteDragImage<TPlatform>(
		ref TPlatform platform, APTR message,
		MuiAreaDeleteDragImageMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return packet.MethodId == DeleteDragImage &&
			MuiAreaDeleteDragImageMessageCodec.Write(ref platform, message, packet);
	}

	internal static bool WriteDeleteDragImage<TPlatform>(
		ref TPlatform platform, APTR message, uint dragImage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaDeleteDragImageMessage);
		packet.MethodId = DeleteDragImage;
		packet.DragImage = dragImage;
		return WriteDeleteDragImage(ref platform, message, packet);
	}

	internal static bool TryReadDrop<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDragDropMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDragDropMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == DragDrop;
	}

	internal static bool WriteDrop<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaDragDropMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return packet.MethodId == DragDrop &&
			MuiAreaDragDropMessageCodec.Write(ref platform, message, packet);
	}

	internal static bool WriteDrop<TPlatform>(ref TPlatform platform,
		APTR message, uint source, int x, int y, uint qualifier)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaDragDropMessage);
		packet.MethodId = DragDrop;
		packet.Object = source;
		packet.X = x;
		packet.Y = y;
		packet.Qualifier = qualifier;
		return WriteDrop(ref platform, message, packet);
	}

	internal static bool TryReadEvent<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDragEventMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDragEventMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == DragEvent;
	}

	internal static bool WriteEvent<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaDragEventMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return packet.MethodId == DragEvent &&
			MuiAreaDragEventMessageCodec.Write(ref platform, message, packet);
	}

	internal static bool WriteEvent<TPlatform>(ref TPlatform platform,
		APTR message, uint window, uint source, uint dragImage, uint intuiMessage,
		int muiKey, uint mousePointerType, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaDragEventMessage);
		packet.MethodId = DragEvent;
		packet.Window = window;
		packet.Object = source;
		packet.DragImage = dragImage;
		packet.IntuiMessage = intuiMessage;
		packet.MuiKey = muiKey;
		packet.MousePointerType = mousePointerType;
		packet.Flags = flags;
		return WriteEvent(ref platform, message, packet);
	}

	internal static bool TryReadFinish<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDragFinishMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDragFinishMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == DragFinish;
	}

	internal static bool WriteFinish<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaDragFinishMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return packet.MethodId == DragFinish &&
			MuiAreaDragFinishMessageCodec.Write(ref platform, message, packet);
	}

	internal static bool WriteFinish<TPlatform>(ref TPlatform platform,
		APTR message, uint source, int dropFollows)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaDragFinishMessage);
		packet.MethodId = DragFinish;
		packet.Object = source;
		packet.DropFollows = dropFollows;
		return WriteFinish(ref platform, message, packet);
	}

	internal static bool TryReadQuery<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDragQueryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDragQueryMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == DragQuery;
	}

	internal static bool WriteQuery<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaDragQueryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return packet.MethodId == DragQuery &&
			MuiAreaDragQueryMessageCodec.Write(ref platform, message, packet);
	}

	internal static bool WriteQuery<TPlatform>(ref TPlatform platform,
		APTR message, uint source)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaDragQueryMessage);
		packet.MethodId = DragQuery;
		packet.Object = source;
		return WriteQuery(ref platform, message, packet);
	}

	internal static bool TryReadReport<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaDragReportMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDragReportMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == DragReport;
	}

	internal static bool WriteReport<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaDragReportMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return packet.MethodId == DragReport &&
			MuiAreaDragReportMessageCodec.Write(ref platform, message, packet);
	}

	internal static bool WriteReport<TPlatform>(ref TPlatform platform,
		APTR message, uint source, int x, int y, int update, uint qualifier)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaDragReportMessage);
		packet.MethodId = DragReport;
		packet.Object = source;
		packet.X = x;
		packet.Y = y;
		packet.Update = update;
		packet.Qualifier = qualifier;
		return WriteReport(ref platform, message, packet);
	}

	private static bool IsPacket<TPlatform>(ref TPlatform platform, APTR message,
		uint size, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsStorage(ref platform, message, size) ||
			!TryReadMethodIdValue(ref platform, message, out var methodId)) return false;
		return methodId == method;
	}

	private static bool IsStorage<TPlatform>(ref TPlatform platform, APTR message,
		uint size) where TPlatform : struct, IMuiGuestMemory =>
		message.IsNotNull && platform.IsMapped(message, size);
}
