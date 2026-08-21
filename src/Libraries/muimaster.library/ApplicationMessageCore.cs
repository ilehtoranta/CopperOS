/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_AppMessage is a transient pointer to Exec's fixed AppMessage record.
// The record remains caller-owned guest memory; this named shape and its codec
// are the only place that crosses the packed Workbench ABI.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAppMessageNodeState
{
	internal const uint Size = 20;

	internal APTR Successor;
	internal APTR Predecessor;
	internal byte Type;
	internal sbyte Priority;
	internal APTR Name;
	internal APTR ReplyPort;
	internal ushort Length;
}

internal enum MuiAppMessageNodeField : byte
{
	Successor,
	Predecessor,
	Type,
	Priority,
	Name,
	ReplyPort,
	Length,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAppMessageNodeFieldCursor
{
	internal APTR Record;
	internal MuiAppMessageNodeField Field;
}

internal static class MuiAppMessageNodeFieldCursorCodec
{
	private static bool TryResolve(MuiAppMessageNodeField field,
		out uint offset, out uint size)
	{
		switch (field)
		{
			case MuiAppMessageNodeField.Successor:
				offset = 0;
				size = 4;
				break;
			case MuiAppMessageNodeField.Predecessor:
				offset = 4;
				size = 4;
				break;
			case MuiAppMessageNodeField.Type:
				offset = 8;
				size = 1;
				break;
			case MuiAppMessageNodeField.Priority:
				offset = 9;
				size = 1;
				break;
			case MuiAppMessageNodeField.Name:
				offset = 10;
				size = 4;
				break;
			case MuiAppMessageNodeField.ReplyPort:
				offset = 14;
				size = 4;
				break;
			case MuiAppMessageNodeField.Length:
				offset = 18;
				size = 2;
				break;
			default:
				offset = 0;
				size = 0;
				return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAppMessageNodeFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset, out var size) ||
			cursor.Record.IsNull || cursor.Record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, size);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAppMessageNodeFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address) ||
			!TryResolve(field, out _, out var size) || size != 4) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAppMessageNodeFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address) ||
			!TryResolve(field, out _, out var size) || size != 4) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAppMessageNodeFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address) ||
			!TryResolve(field, out _, out var size) || size != 2) return false;
		value = platform.ReadUInt16(address, 0);
		return true;
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAppMessageNodeFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address) ||
			!TryResolve(field, out _, out var size) || size != 2) return false;
		platform.WriteUInt16(address, 0, value);
		return true;
	}

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAppMessageNodeFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address) ||
			!TryResolve(field, out _, out var size) || size != 1) return false;
		value = platform.ReadUInt8(address, 0);
		return true;
	}

	internal static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAppMessageNodeFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address) ||
			!TryResolve(field, out _, out var size) || size != 1) return false;
		platform.WriteUInt8(address, 0, value);
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAppMessageRecord
{
	internal const uint Size = 86;

	internal MuiAppMessageNodeState Message;
	internal ushort Type;
	internal uint UserData;
	internal uint Id;
	internal int NumberOfArguments;
	internal APTR ArgumentList;
	internal ushort Version;
	internal ushort Class;
	internal short MouseX;
	internal short MouseY;
	internal uint Seconds;
	internal uint Micros;
	internal uint Reserved0;
	internal uint Reserved1;
	internal uint Reserved2;
	internal uint Reserved3;
	internal uint Reserved4;
	internal uint Reserved5;
	internal uint Reserved6;
	internal uint Reserved7;
}

internal enum MuiAppMessageField : byte
{
	Type,
	UserData,
	Id,
	NumberOfArguments,
	ArgumentList,
	Version,
	Class,
	MouseX,
	MouseY,
	Seconds,
	Micros,
	Reserved0,
	Reserved1,
	Reserved2,
	Reserved3,
	Reserved4,
	Reserved5,
	Reserved6,
	Reserved7,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAppMessageFieldCursor
{
	internal APTR Record;
	internal MuiAppMessageField Field;
}

internal static class MuiAppMessageFieldCursorCodec
{
	private static bool TryResolve(MuiAppMessageField field,
		out uint offset, out uint size)
	{
		switch (field)
		{
			case MuiAppMessageField.Type:
				offset = 20;
				size = 2;
				break;
			case MuiAppMessageField.UserData:
				offset = 22;
				size = 4;
				break;
			case MuiAppMessageField.Id:
				offset = 26;
				size = 4;
				break;
			case MuiAppMessageField.NumberOfArguments:
				offset = 30;
				size = 4;
				break;
			case MuiAppMessageField.ArgumentList:
				offset = 34;
				size = 4;
				break;
			case MuiAppMessageField.Version:
				offset = 38;
				size = 2;
				break;
			case MuiAppMessageField.Class:
				offset = 40;
				size = 2;
				break;
			case MuiAppMessageField.MouseX:
				offset = 42;
				size = 2;
				break;
			case MuiAppMessageField.MouseY:
				offset = 44;
				size = 2;
				break;
			case MuiAppMessageField.Seconds:
				offset = 46;
				size = 4;
				break;
			case MuiAppMessageField.Micros:
				offset = 50;
				size = 4;
				break;
			case MuiAppMessageField.Reserved0:
				offset = 54;
				size = 4;
				break;
			case MuiAppMessageField.Reserved1:
				offset = 58;
				size = 4;
				break;
			case MuiAppMessageField.Reserved2:
				offset = 62;
				size = 4;
				break;
			case MuiAppMessageField.Reserved3:
				offset = 66;
				size = 4;
				break;
			case MuiAppMessageField.Reserved4:
				offset = 70;
				size = 4;
				break;
			case MuiAppMessageField.Reserved5:
				offset = 74;
				size = 4;
				break;
			case MuiAppMessageField.Reserved6:
				offset = 78;
				size = 4;
				break;
			case MuiAppMessageField.Reserved7:
				offset = 82;
				size = 4;
				break;
			default:
				offset = 0;
				size = 0;
				return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAppMessageFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset, out var size) ||
			cursor.Record.IsNull || cursor.Record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, size);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAppMessageFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address) ||
			!TryResolve(field, out _, out var size) || size != 4) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAppMessageFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address) ||
			!TryResolve(field, out _, out var size) || size != 4) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAppMessageFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address) ||
			!TryResolve(field, out _, out var size) || size != 2) return false;
		value = platform.ReadUInt16(address, 0);
		return true;
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAppMessageFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address) ||
			!TryResolve(field, out _, out var size) || size != 2) return false;
		platform.WriteUInt16(address, 0, value);
		return true;
	}
}

internal static class MuiAppMessageNodeCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAppMessageNodeState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAppMessageNodeState.Size)) return false;
		if (!MuiAppMessageNodeFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiAppMessageNodeField.Successor, out var rawSuccessor) ||
			!MuiAppMessageNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAppMessageNodeField.Predecessor, out var rawPredecessor) ||
			!MuiAppMessageNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAppMessageNodeField.Name, out var rawName) ||
			!MuiAppMessageNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAppMessageNodeField.ReplyPort, out var rawReplyPort)) return false;
		value.Successor = APTR.FromPointer(rawSuccessor);
		value.Predecessor = APTR.FromPointer(rawPredecessor);
		value.Name = APTR.FromPointer(rawName);
		value.ReplyPort = APTR.FromPointer(rawReplyPort);
		if (!MuiAppMessageNodeFieldCursorCodec.TryReadUInt8(ref platform, address,
			MuiAppMessageNodeField.Type, out value.Type) ||
			!MuiAppMessageNodeFieldCursorCodec.TryReadUInt8(ref platform, address,
				MuiAppMessageNodeField.Priority, out var rawPriority) ||
			!MuiAppMessageNodeFieldCursorCodec.TryReadUInt16(ref platform, address,
				MuiAppMessageNodeField.Length, out value.Length)) return false;
		value.Priority = unchecked((sbyte)rawPriority);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAppMessageNodeState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAppMessageNodeState.Size)) return false;
		return MuiAppMessageNodeFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAppMessageNodeField.Successor, value.Successor.Raw) &&
			MuiAppMessageNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAppMessageNodeField.Predecessor, value.Predecessor.Raw) &&
			MuiAppMessageNodeFieldCursorCodec.TryWriteUInt8(ref platform, address,
				MuiAppMessageNodeField.Type, value.Type) &&
			MuiAppMessageNodeFieldCursorCodec.TryWriteUInt8(ref platform, address,
				MuiAppMessageNodeField.Priority,
				unchecked((byte)value.Priority)) &&
			MuiAppMessageNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAppMessageNodeField.Name, value.Name.Raw) &&
			MuiAppMessageNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAppMessageNodeField.ReplyPort, value.ReplyPort.Raw) &&
			MuiAppMessageNodeFieldCursorCodec.TryWriteUInt16(ref platform, address,
				MuiAppMessageNodeField.Length, value.Length);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWorkbenchArgumentRecord
{
	internal const uint Size = 8;

	internal BPTR Lock;
	internal STRPTR Name;
}

internal enum MuiWorkbenchArgumentField : byte
{
	Lock,
	Name,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWorkbenchArgumentFieldCursor
{
	internal APTR Record;
	internal MuiWorkbenchArgumentField Field;
}

internal static class MuiWorkbenchArgumentFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWorkbenchArgumentFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiWorkbenchArgumentField.Lock:
				offset = 0;
				break;
			case MuiWorkbenchArgumentField.Name:
				offset = 4;
				break;
			default:
				return false;
		}
		if (cursor.Record.IsNull || cursor.Record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiWorkbenchArgumentField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiWorkbenchArgumentFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiWorkbenchArgumentField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiWorkbenchArgumentFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWorkbenchArgumentVectorCursor
{
	internal const uint EntrySize = MuiWorkbenchArgumentRecord.Size;
	internal APTR Base;
	internal uint Index;
}

internal static class MuiWorkbenchArgumentVectorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiWorkbenchArgumentVectorCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiWorkbenchArgumentVectorCursor.EntrySize) return false;
		var offset = cursor.Index *
			MuiWorkbenchArgumentVectorCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address,
			MuiWorkbenchArgumentVectorCursor.EntrySize);
	}
}

internal static class MuiWorkbenchArgumentRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWorkbenchArgumentRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiWorkbenchArgumentRecord.Size)) return false;
		if (!MuiWorkbenchArgumentFieldCursorCodec.TryRead(ref platform, address,
			MuiWorkbenchArgumentField.Lock, out var rawLock) ||
			!MuiWorkbenchArgumentFieldCursorCodec.TryRead(ref platform, address,
				MuiWorkbenchArgumentField.Name, out var rawName)) return false;
		value.Lock = BPTR.FromRaw(rawLock);
		value.Name = STRPTR.FromPointer(rawName);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWorkbenchArgumentRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiWorkbenchArgumentRecord.Size)) return false;
		return MuiWorkbenchArgumentFieldCursorCodec.TryWrite(ref platform, address,
			MuiWorkbenchArgumentField.Lock, value.Lock.Raw) &&
			MuiWorkbenchArgumentFieldCursorCodec.TryWrite(ref platform, address,
				MuiWorkbenchArgumentField.Name, value.Name.Raw);
	}
}

internal static class MuiAppMessageRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAppMessageRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAppMessageRecord.Size)) return false;
		if (!MuiAppMessageNodeCodec.TryRead(ref platform, address,
			out value.Message)) return false;
		if (!MuiAppMessageFieldCursorCodec.TryReadUInt16(ref platform, address,
			MuiAppMessageField.Type, out value.Type) ||
			!MuiAppMessageFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAppMessageField.UserData, out value.UserData) ||
			!MuiAppMessageFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAppMessageField.Id, out value.Id) ||
			!MuiAppMessageFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAppMessageField.NumberOfArguments, out var rawArguments) ||
			!MuiAppMessageFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAppMessageField.ArgumentList, out var rawArgumentList) ||
			!MuiAppMessageFieldCursorCodec.TryReadUInt16(ref platform, address,
				MuiAppMessageField.Version, out value.Version) ||
			!MuiAppMessageFieldCursorCodec.TryReadUInt16(ref platform, address,
				MuiAppMessageField.Class, out value.Class) ||
			!MuiAppMessageFieldCursorCodec.TryReadUInt16(ref platform, address,
				MuiAppMessageField.MouseX, out var rawMouseX) ||
			!MuiAppMessageFieldCursorCodec.TryReadUInt16(ref platform, address,
				MuiAppMessageField.MouseY, out var rawMouseY) ||
			!MuiAppMessageFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAppMessageField.Seconds, out value.Seconds) ||
			!MuiAppMessageFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAppMessageField.Micros, out value.Micros) ||
			!MuiAppMessageFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAppMessageField.Reserved0, out value.Reserved0) ||
			!MuiAppMessageFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAppMessageField.Reserved1, out value.Reserved1) ||
			!MuiAppMessageFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAppMessageField.Reserved2, out value.Reserved2) ||
			!MuiAppMessageFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAppMessageField.Reserved3, out value.Reserved3) ||
			!MuiAppMessageFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAppMessageField.Reserved4, out value.Reserved4) ||
			!MuiAppMessageFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAppMessageField.Reserved5, out value.Reserved5) ||
			!MuiAppMessageFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAppMessageField.Reserved6, out value.Reserved6) ||
			!MuiAppMessageFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAppMessageField.Reserved7, out value.Reserved7)) return false;
		value.NumberOfArguments = unchecked((int)rawArguments);
		value.ArgumentList = APTR.FromPointer(rawArgumentList);
		value.MouseX = unchecked((short)rawMouseX);
		value.MouseY = unchecked((short)rawMouseY);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAppMessageRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAppMessageRecord.Size)) return false;
		if (!MuiAppMessageNodeCodec.Write(ref platform, address,
			value.Message)) return false;
		return MuiAppMessageFieldCursorCodec.TryWriteUInt16(ref platform, address,
			MuiAppMessageField.Type, value.Type) &&
			MuiAppMessageFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAppMessageField.UserData, value.UserData) &&
			MuiAppMessageFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAppMessageField.Id, value.Id) &&
			MuiAppMessageFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAppMessageField.NumberOfArguments,
				unchecked((uint)value.NumberOfArguments)) &&
			MuiAppMessageFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAppMessageField.ArgumentList, value.ArgumentList.Raw) &&
			MuiAppMessageFieldCursorCodec.TryWriteUInt16(ref platform, address,
				MuiAppMessageField.Version, value.Version) &&
			MuiAppMessageFieldCursorCodec.TryWriteUInt16(ref platform, address,
				MuiAppMessageField.Class, value.Class) &&
			MuiAppMessageFieldCursorCodec.TryWriteUInt16(ref platform, address,
				MuiAppMessageField.MouseX, unchecked((ushort)value.MouseX)) &&
			MuiAppMessageFieldCursorCodec.TryWriteUInt16(ref platform, address,
				MuiAppMessageField.MouseY, unchecked((ushort)value.MouseY)) &&
			MuiAppMessageFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAppMessageField.Seconds, value.Seconds) &&
			MuiAppMessageFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAppMessageField.Micros, value.Micros) &&
			MuiAppMessageFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAppMessageField.Reserved0, value.Reserved0) &&
			MuiAppMessageFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAppMessageField.Reserved1, value.Reserved1) &&
			MuiAppMessageFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAppMessageField.Reserved2, value.Reserved2) &&
			MuiAppMessageFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAppMessageField.Reserved3, value.Reserved3) &&
			MuiAppMessageFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAppMessageField.Reserved4, value.Reserved4) &&
			MuiAppMessageFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAppMessageField.Reserved5, value.Reserved5) &&
			MuiAppMessageFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAppMessageField.Reserved6, value.Reserved6) &&
			MuiAppMessageFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAppMessageField.Reserved7, value.Reserved7);
	}

}

public static class MuiApplicationMessageCore
{
	public const uint ApplicationObject = 0x8042D3EE;
	public const uint AppMessage = 0x80421955;
	public const uint WindowAppWindow = 0x804280CF;

	// These attributes are owned by the application/message routing record even
	// when the object is a custom or otherwise unknown MUI class. Keep the
	// admission predicate next to the typed getter so direct Get and generic
	// OM_GET cannot drift apart.
	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == ApplicationObject || attribute == AppMessage ||
		attribute == WindowAppWindow;

	private const uint ApplicationInitialized = 0x7FFE0044;
	private const uint WindowOpen = 0x80428AA0;
	private const uint RoutingStateKey = 0x7F0A001Au;
	private const int MaximumArguments = 65535;
	private const uint MaximumStringLength = 65536;

	internal static bool TryGetApplicationMessageRoutingState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiApplicationMessageRoutingStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			RoutingStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			RoutingStateKey) != unchecked((int)
			MuiApplicationMessageRoutingStateRecord.Size)) return false;
		return MuiApplicationMessageRoutingStateRecordCodec.TryRead(ref platform,
			block, out value);
	}

	private static MuiApplicationMessageRoutingStateRecord ReadRoutingState<
		TPlatform>(ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishRoutingState(ref platform, state, obj, out var value))
			return value;
		value = default;
		value.Magic = MuiApplicationMessageRoutingStateRecord.Cookie;
		FillRoutingState(ref platform, state, obj, ref value);
		return value;
	}

	private static bool PublishRoutingState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj,
		out MuiApplicationMessageRoutingStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			RoutingStateKey);
		if (TryGetApplicationMessageRoutingState(ref platform, state, obj,
			out value))
		{
			FillRoutingState(ref platform, state, obj, ref value);
			return MuiApplicationMessageRoutingStateRecordCodec.Write(ref platform,
				block, value);
		}

		value = default;
		value.Magic = MuiApplicationMessageRoutingStateRecord.Cookie;
		FillRoutingState(ref platform, state, obj, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationMessageRoutingStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiApplicationMessageRoutingStateRecord.Size);
		var written = MuiApplicationMessageRoutingStateRecordCodec.Write(
			ref platform, scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			RoutingStateKey, scratch,
			unchecked((int)MuiApplicationMessageRoutingStateRecord.Size));
		platform.Clear(scratch, MuiApplicationMessageRoutingStateRecord.Size);
		platform.Free(scratch, MuiApplicationMessageRoutingStateRecord.Size);
		return added;
	}

	private static void FillRoutingState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj,
		ref MuiApplicationMessageRoutingStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			AppMessage, out var appMessage)) appMessage = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			WindowAppWindow, out var appWindow)) appWindow = 0;
		value.AppMessage = APTR.FromPointer(appMessage);
		value.WindowAppWindow = appWindow == 0 ? 0u : 1u;
	}

	private static bool SetRoutingAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return false;
		var obj = objectValue.Boopsi;
		if (obj.IsNull) return false;
		var previous = 0u;
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			attribute, out previous);
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, attribute, value, notify)) return false;
		if (PublishRoutingState(ref platform, state, obj, out _)) return true;
		MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
			attribute, previous, notify);
		return false;
	}

	internal static bool TrySet<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint attribute, uint value, bool notify, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		handled = IsPublicGetterAttribute(attribute);
		if (!handled) return false;
		if (attribute == WindowAppWindow)
		{
			if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
				out var objectValue)) return false;
			return SetWindowAppWindowValue(ref platform, state, objectValue.Boopsi,
				value);
		}
		// ApplicationObject and AppMessage are getter-only. AppMessage is
		// changed only by PublishAppMessage while a notification is executing.
		return false;
	}

	internal static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		handled = IsPublicGetterAttribute(attribute);
		if (!handled) return false;
		if (attribute == ApplicationObject)
		{
			value = FindApplication(ref platform, state, obj).Raw;
			return true;
		}
		if (attribute == AppMessage || attribute == WindowAppWindow)
		{
			var routing = ReadRoutingState(ref platform, state, obj);
			value = attribute == AppMessage ? routing.AppMessage.Raw :
				routing.WindowAppWindow;
			return true;
		}
		return true;
	}

	public static bool SetWindowAppWindowValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR window, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!IsWindowObject(ref platform, state, window)) return false;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			WindowOpen, out var open) && open != 0) return false;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, window);
		return SetRoutingAttribute(ref platform, state, record, WindowAppWindow,
			value == 0 ? 0u : 1u, false);
	}

	// Publish an AppMessage to a target object in an AppWindow subtree. The
	// pointer is valid only during synchronous notification dispatch; the
	// previous transient value is restored afterwards without a managed copy.
	public static bool PublishAppMessage<TPlatform>(ref TPlatform platform,
		APTR state, APTR target, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!IsAppWindowTarget(ref platform, state, target) ||
			!ValidateMessage(ref platform, message)) return false;
		return PublishToTarget(ref platform, state, target, message);
	}

	// App icon drops use the application's caller-owned DropObject target even
	// though that object is not itself a Window.mui instance.
	public static bool PublishApplicationDropMessage<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			application, 0x8042A07F, out var iconified) || iconified == 0 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, state, application,
				0x80421266, out var target) || target == 0 ||
			!ValidateMessage(ref platform, message)) return false;
		return PublishToTarget(ref platform, state, APTR.FromPointer(target),
			message);
	}

	internal static bool ValidateMessage<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAppMessageRecordCodec.TryRead(ref platform, message,
			out var value) || value.NumberOfArguments < 0 ||
			value.NumberOfArguments > MaximumArguments) return false;
		if (value.NumberOfArguments == 0) return true;
		if (value.ArgumentList.IsNull ||
			(uint)value.NumberOfArguments > uint.MaxValue /
			MuiWorkbenchArgumentRecord.Size) return false;
		var bytes = (uint)value.NumberOfArguments *
			MuiWorkbenchArgumentRecord.Size;
		if (!platform.IsMapped(value.ArgumentList, bytes)) return false;
		var cursor = default(MuiWorkbenchArgumentVectorCursor);
		cursor.Base = value.ArgumentList;
		for (var index = 0u; index < (uint)value.NumberOfArguments; index++)
		{
			cursor.Index = index;
			if (!MuiWorkbenchArgumentVectorCodec.TryGetEntry(ref platform, cursor,
				out var address)) return false;
			if (!MuiWorkbenchArgumentRecordCodec.TryRead(ref platform, address,
				out var argument)) return false;
			var name = APTR.FromPointer(argument.Name.Raw);
			if (name.IsNotNull && !CStringCodec.TryReadLength(ref platform, name,
				MaximumStringLength, out _)) return false;
		}
		return true;
	}

	private static bool PublishToTarget<TPlatform>(ref TPlatform platform,
		APTR state, APTR target, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, target);
		if (record.IsNull) return false;
		var hadPrevious = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, target, AppMessage, out var previous);
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, AppMessage, message.Raw, true)) return false;
		if (!PublishRoutingState(ref platform, state, target, out _))
		{
			MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
				AppMessage, hadPrevious ? previous : 0, false);
			PublishRoutingState(ref platform, state, target, out _);
			return false;
		}
		var restored = MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform,
			state, record, AppMessage, hadPrevious ? previous : 0, false);
		return restored && PublishRoutingState(ref platform, state, target, out _);
	}

	private static bool IsAppWindowTarget<TPlatform>(ref TPlatform platform,
		APTR state, APTR target) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var current = target;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, current,
				WindowAppWindow, out var enabled) && enabled != 0) return true;
			current = MuiHeadlessObjectCore.ParentObject(ref platform, state,
				current);
		}
		return false;
	}

	private static APTR FindApplication<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var current = obj;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, current,
				ApplicationInitialized, out var initialized) && initialized != 0)
				return current;
			current = MuiHeadlessObjectCore.ParentObject(ref platform, state,
				current);
		}
		return APTR.Null;
	}

	internal static bool IsWindowObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull || !MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue) || !MuiHeadlessClassCodec.TryRead(ref platform,
			objectValue.Class, out var classValue)) return false;
		var name = classValue.Name;
		if (name.IsNull || !platform.IsMapped(name, 11)) return false;
		if (platform.ReadUInt8(name, 0) != (byte)'W' ||
			platform.ReadUInt8(name, 1) != (byte)'i' ||
			platform.ReadUInt8(name, 2) != (byte)'n' ||
			platform.ReadUInt8(name, 3) != (byte)'d' ||
			platform.ReadUInt8(name, 4) != (byte)'o' ||
			platform.ReadUInt8(name, 5) != (byte)'w' ||
			platform.ReadUInt8(name, 6) != (byte)'.' ||
			platform.ReadUInt8(name, 7) != (byte)'m' ||
			platform.ReadUInt8(name, 8) != (byte)'u' ||
			platform.ReadUInt8(name, 9) != (byte)'i') return false;
		return platform.ReadUInt8(name, 10) == 0;
	}
}
