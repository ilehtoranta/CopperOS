/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The result published by MUIM_List_TestPos.  The public MorphOS record is a
// fixed 12-byte value: a signed entry index followed by the selected column,
// outside-cell flags, and cell-relative offsets.  Keep the value named in the
// core; only the codec below knows its packed guest representation.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListTestPosResult
{
	internal const uint Size = 12;
	internal const ushort FlagAbove = 1;
	internal const ushort FlagBelow = 2;
	internal const ushort FlagLeft = 4;
	internal const ushort FlagRight = 8;

	internal int Entry;
	internal short Column;
	internal ushort Flags;
	internal short XOffset;
	internal short YOffset;
}

internal static class MuiListTestPosResultCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR storage,
		MuiListTestPosResult value) where TPlatform : struct, IMuiGuestMemory
	{
		if (storage.IsNull || !platform.IsMapped(storage,
			MuiListTestPosResult.Size)) return false;
		return MuiListInputRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			storage, MuiListInputRecordKind.TestPos,
			MuiListInputRecordField.Entry, unchecked((uint)value.Entry)) &&
			MuiListInputRecordFieldCursorCodec.TryWriteUInt16(ref platform, storage,
				MuiListInputRecordKind.TestPos, MuiListInputRecordField.Column,
				unchecked((ushort)value.Column)) &&
			MuiListInputRecordFieldCursorCodec.TryWriteUInt16(ref platform, storage,
				MuiListInputRecordKind.TestPos, MuiListInputRecordField.Flags,
				value.Flags) &&
			MuiListInputRecordFieldCursorCodec.TryWriteUInt16(ref platform, storage,
				MuiListInputRecordKind.TestPos, MuiListInputRecordField.XOffset,
				unchecked((ushort)value.XOffset)) &&
			MuiListInputRecordFieldCursorCodec.TryWriteUInt16(ref platform, storage,
				MuiListInputRecordKind.TestPos, MuiListInputRecordField.YOffset,
				unchecked((ushort)value.YOffset));
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR storage,
		out MuiListTestPosResult value) where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (storage.IsNull || !platform.IsMapped(storage,
			MuiListTestPosResult.Size)) return false;
		if (!MuiListInputRecordFieldCursorCodec.TryReadUInt32(ref platform,
			storage, MuiListInputRecordKind.TestPos,
			MuiListInputRecordField.Entry, out var entry) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt16(ref platform, storage,
				MuiListInputRecordKind.TestPos, MuiListInputRecordField.Column,
				out var column) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt16(ref platform, storage,
				MuiListInputRecordKind.TestPos, MuiListInputRecordField.Flags,
				out value.Flags) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt16(ref platform, storage,
				MuiListInputRecordKind.TestPos, MuiListInputRecordField.XOffset,
				out var xOffset) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt16(ref platform, storage,
				MuiListInputRecordKind.TestPos, MuiListInputRecordField.YOffset,
				out var yOffset)) return false;
		value.Entry = unchecked((int)entry);
		value.Column = unchecked((short)column);
		value.XOffset = unchecked((short)xOffset);
		value.YOffset = unchecked((short)yOffset);
		return true;
	}
}

// The selection and NextSelected APIs exchange a caller-owned LONG through a
// pointer. Keep that four-byte storage contract named at the boundary so list
// logic does not depend on an unexplained offset zero or a repeated size
// literal. The wire value remains a 68k ULONG; callers interpret signed
// sentinel values where the MorphOS API defines them.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListScalarStorageRecord
{
	internal const uint Size = 4;

	internal uint Value;
}

internal static class MuiListScalarStorageCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR storage,
		MuiListScalarStorageRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (storage.IsNull || !platform.IsMapped(storage,
			MuiListScalarStorageRecord.Size)) return false;
		return MuiListInputRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			storage, MuiListInputRecordKind.Scalar,
			MuiListInputRecordField.Value, value.Value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR storage,
		out MuiListScalarStorageRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (storage.IsNull || !platform.IsMapped(storage,
			MuiListScalarStorageRecord.Size)) return false;
		return MuiListInputRecordFieldCursorCodec.TryReadUInt32(ref platform,
			storage, MuiListInputRecordKind.Scalar,
			MuiListInputRecordField.Value, out value.Value);
	}
}

// MorphOS MUI V6 display hooks receive the zero-based row number in the ULONG
// immediately preceding the display-column pointer.  Keep that ABI value
// named rather than making ListCore pass an unexplained four-byte slot around.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListDisplayRowRecord
{
	internal const uint Size = 4;

	internal int Row;
}

internal static class MuiListDisplayRowRecordCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR storage,
		MuiListDisplayRowRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (storage.IsNull || !platform.IsMapped(storage,
			MuiListDisplayRowRecord.Size)) return false;
		return MuiListInputRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			storage, MuiListInputRecordKind.DisplayRow,
			MuiListInputRecordField.Row, unchecked((uint)value.Row));
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR storage,
		out MuiListDisplayRowRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (storage.IsNull || !platform.IsMapped(storage,
			MuiListDisplayRowRecord.Size)) return false;
		if (!MuiListInputRecordFieldCursorCodec.TryReadUInt32(ref platform,
			storage, MuiListInputRecordKind.DisplayRow,
			MuiListInputRecordField.Row, out var row)) return false;
		value.Row = unchecked((int)row);
		return true;
	}
}

// The stable IntuiMessage fields needed by a Listview mouse path.  The full
// Intuition envelope is larger, but these fields are fixed by the 68k ABI and
// are all that Listview consumes.  The decoder validates the complete prefix
// before exposing the typed value, so malformed guest pointers are rejected
// without a partial event.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiIntuiPointerMessage
{
	internal const uint MinimumSize = 0x24;

	internal uint Class;
	internal ushort Code;
	internal ushort Qualifier;
	internal uint IAddress;
	internal short MouseX;
	internal short MouseY;
}

internal static class MuiIntuiMessageCodec
{
	internal static bool TryReadPointer<TPlatform>(ref TPlatform platform,
		APTR message, out MuiIntuiPointerMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiIntuiPointerMessage.MinimumSize)) return false;
		if (!MuiListInputRecordFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiListInputRecordKind.IntuiMessage,
			MuiListInputRecordField.Class, out value.Class) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt16(ref platform,
				message, MuiListInputRecordKind.IntuiMessage,
				MuiListInputRecordField.Code, out value.Code) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt16(ref platform,
				message, MuiListInputRecordKind.IntuiMessage,
				MuiListInputRecordField.Qualifier, out value.Qualifier) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiListInputRecordKind.IntuiMessage,
				MuiListInputRecordField.IAddress, out value.IAddress) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt16(ref platform,
				message, MuiListInputRecordKind.IntuiMessage,
				MuiListInputRecordField.MouseX, out var mouseX) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt16(ref platform,
				message, MuiListInputRecordKind.IntuiMessage,
				MuiListInputRecordField.MouseY, out var mouseY)) return false;
		value.MouseX = unchecked((short)mouseX);
		value.MouseY = unchecked((short)mouseY);
		return true;
	}

	internal static bool WritePointer<TPlatform>(ref TPlatform platform,
		APTR message, uint messageClass, ushort code, ushort qualifier,
		uint iAddress, short mouseX, short mouseY)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiIntuiPointerMessage.MinimumSize)) return false;
		return MuiListInputRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiListInputRecordKind.IntuiMessage,
			MuiListInputRecordField.Class, messageClass) &&
			MuiListInputRecordFieldCursorCodec.TryWriteUInt16(ref platform,
				message, MuiListInputRecordKind.IntuiMessage,
				MuiListInputRecordField.Code, code) &&
			MuiListInputRecordFieldCursorCodec.TryWriteUInt16(ref platform,
				message, MuiListInputRecordKind.IntuiMessage,
				MuiListInputRecordField.Qualifier, qualifier) &&
			MuiListInputRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiListInputRecordKind.IntuiMessage,
				MuiListInputRecordField.IAddress, iAddress) &&
			MuiListInputRecordFieldCursorCodec.TryWriteUInt16(ref platform,
				message, MuiListInputRecordKind.IntuiMessage,
				MuiListInputRecordField.MouseX, unchecked((ushort)mouseX)) &&
			MuiListInputRecordFieldCursorCodec.TryWriteUInt16(ref platform,
				message, MuiListInputRecordKind.IntuiMessage,
				MuiListInputRecordField.MouseY, unchecked((ushort)mouseY));
	}
}

// Guest-resident state for the bounded Listview drag-sort path.  Source and
// target are list row indices; coordinates are retained only to make the
// current drag transition observable without keeping a managed event object.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListviewDragState
{
	internal const uint Size = 32;
	internal const uint ActiveFlag = 1;
	internal const uint MovedFlag = 2;

	internal uint Magic;
	internal int Source;
	internal int Target;
	internal int StartX;
	internal int StartY;
	internal int LastX;
	internal int LastY;
	internal uint Flags;
}

internal enum MuiListInputRecordKind : byte
{
	TestPos,
	Scalar,
	DisplayRow,
	IntuiMessage,
	DragState,
}

internal enum MuiListInputRecordField : byte
{
	Entry,
	Column,
	Flags,
	XOffset,
	YOffset,
	Value,
	Row,
	Class,
	Code,
	Qualifier,
	IAddress,
	MouseX,
	MouseY,
	Magic,
	Source,
	Target,
	StartX,
	StartY,
	LastX,
	LastY,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListInputRecordFieldCursor
{
	internal APTR Address;
	internal MuiListInputRecordKind Record;
	internal MuiListInputRecordField Field;
}

internal static class MuiListInputRecordFieldCursorCodec
{
	private static bool TryResolve(MuiListInputRecordKind record,
		MuiListInputRecordField field, out uint offset, out uint size,
		out uint fieldSize)
	{
		offset = 0;
		size = 0;
		fieldSize = 0;
		switch (record)
		{
			case MuiListInputRecordKind.TestPos:
				size = MuiListTestPosResult.Size;
				offset = field switch
				{
					MuiListInputRecordField.Entry => 0,
					MuiListInputRecordField.Column => 4,
					MuiListInputRecordField.Flags => 6,
					MuiListInputRecordField.XOffset => 8,
					MuiListInputRecordField.YOffset => 10,
					_ => uint.MaxValue,
				};
				fieldSize = field == MuiListInputRecordField.Entry ? 4u : 2u;
				break;
			case MuiListInputRecordKind.Scalar:
				size = MuiListScalarStorageRecord.Size;
				offset = field == MuiListInputRecordField.Value ? 0u :
					uint.MaxValue;
				fieldSize = 4;
				break;
			case MuiListInputRecordKind.DisplayRow:
				size = MuiListDisplayRowRecord.Size;
				offset = field == MuiListInputRecordField.Row ? 0u :
					uint.MaxValue;
				fieldSize = 4;
				break;
			case MuiListInputRecordKind.IntuiMessage:
				size = MuiIntuiPointerMessage.MinimumSize;
				offset = field switch
				{
					MuiListInputRecordField.Class => 0x14,
					MuiListInputRecordField.Code => 0x18,
					MuiListInputRecordField.Qualifier => 0x1A,
					MuiListInputRecordField.IAddress => 0x1C,
					MuiListInputRecordField.MouseX => 0x20,
					MuiListInputRecordField.MouseY => 0x22,
					_ => uint.MaxValue,
				};
				fieldSize = field == MuiListInputRecordField.Class ||
					field == MuiListInputRecordField.IAddress ? 4u : 2u;
				break;
			case MuiListInputRecordKind.DragState:
				size = MuiListviewDragState.Size;
				offset = field switch
				{
					MuiListInputRecordField.Magic => 0,
					MuiListInputRecordField.Source => 4,
					MuiListInputRecordField.Target => 8,
					MuiListInputRecordField.StartX => 12,
					MuiListInputRecordField.StartY => 16,
					MuiListInputRecordField.LastX => 20,
					MuiListInputRecordField.LastY => 24,
					MuiListInputRecordField.Flags => 28,
					_ => uint.MaxValue,
				};
				fieldSize = 4;
				break;
		}
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListInputRecordFieldCursor cursor, out APTR address,
		out uint fieldSize) where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolve(cursor.Record, cursor.Field, out var offset,
			out var size, out fieldSize) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, size)) return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListInputRecordKind record,
		MuiListInputRecordField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiListInputRecordFieldCursor);
		cursor.Address = address;
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
			out var fieldSize) || fieldSize != 4) return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListInputRecordKind record,
		MuiListInputRecordField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiListInputRecordFieldCursor);
		cursor.Address = address;
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
			out var fieldSize) || fieldSize != 4) return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR address, MuiListInputRecordKind record,
		MuiListInputRecordField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiListInputRecordFieldCursor);
		cursor.Address = address;
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
			out var fieldSize) || fieldSize != 2) return false;
		value = platform.ReadUInt16(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR address, MuiListInputRecordKind record,
		MuiListInputRecordField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiListInputRecordFieldCursor);
		cursor.Address = address;
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
			out var fieldSize) || fieldSize != 2) return false;
		platform.WriteUInt16(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiListviewDragStateCodec
{
	internal const uint Cookie = 0x4C564447u; // 'LVDG'

	internal static void Write<TPlatform>(ref TPlatform platform, APTR storage,
		MuiListviewDragState value) where TPlatform : struct, IMuiGuestMemory
	{
		_ = MuiListInputRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			storage, MuiListInputRecordKind.DragState,
			MuiListInputRecordField.Magic, value.Magic);
		_ = MuiListInputRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			storage, MuiListInputRecordKind.DragState,
			MuiListInputRecordField.Source, unchecked((uint)value.Source));
		_ = MuiListInputRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			storage, MuiListInputRecordKind.DragState,
			MuiListInputRecordField.Target, unchecked((uint)value.Target));
		_ = MuiListInputRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			storage, MuiListInputRecordKind.DragState,
			MuiListInputRecordField.StartX, unchecked((uint)value.StartX));
		_ = MuiListInputRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			storage, MuiListInputRecordKind.DragState,
			MuiListInputRecordField.StartY, unchecked((uint)value.StartY));
		_ = MuiListInputRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			storage, MuiListInputRecordKind.DragState,
			MuiListInputRecordField.LastX, unchecked((uint)value.LastX));
		_ = MuiListInputRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			storage, MuiListInputRecordKind.DragState,
			MuiListInputRecordField.LastY, unchecked((uint)value.LastY));
		_ = MuiListInputRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			storage, MuiListInputRecordKind.DragState,
			MuiListInputRecordField.Flags, value.Flags);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR storage,
		out MuiListviewDragState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (storage.IsNull || !platform.IsMapped(storage,
			MuiListviewDragState.Size) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt32(ref platform,
				storage, MuiListInputRecordKind.DragState,
				MuiListInputRecordField.Magic, out var magic) || magic != Cookie ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt32(ref platform,
				storage, MuiListInputRecordKind.DragState,
				MuiListInputRecordField.Source, out var source) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt32(ref platform,
				storage, MuiListInputRecordKind.DragState,
				MuiListInputRecordField.Target, out var target) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt32(ref platform,
				storage, MuiListInputRecordKind.DragState,
				MuiListInputRecordField.StartX, out var startX) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt32(ref platform,
				storage, MuiListInputRecordKind.DragState,
				MuiListInputRecordField.StartY, out var startY) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt32(ref platform,
				storage, MuiListInputRecordKind.DragState,
				MuiListInputRecordField.LastX, out var lastX) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt32(ref platform,
				storage, MuiListInputRecordKind.DragState,
				MuiListInputRecordField.LastY, out var lastY) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt32(ref platform,
				storage, MuiListInputRecordKind.DragState,
				MuiListInputRecordField.Flags, out var flags)) return false;
		value.Magic = magic;
		value.Source = unchecked((int)source);
		value.Target = unchecked((int)target);
		value.StartX = unchecked((int)startX);
		value.StartY = unchecked((int)startY);
		value.LastX = unchecked((int)lastX);
		value.LastY = unchecked((int)lastY);
		value.Flags = flags;
		return true;
	}

	internal static void Clear<TPlatform>(ref TPlatform platform, APTR storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (storage.IsNull || !platform.IsMapped(storage,
			MuiListviewDragState.Size)) return;
		platform.Clear(storage, MuiListviewDragState.Size);
	}
}
