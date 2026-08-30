/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

public enum MuiPointerCaptureKind : uint
{
	ListDrag = 1,
	VerticalScroller = 2,
	HorizontalScroller = 3,
	AreaDrag = 4,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiPointerCaptureSample
{
	public APTR Object;
	public MuiPointerCaptureKind Kind;
	public int StartX;
	public int StartY;
}

// Listview.mui (autodoc MUI_Listview.doc). A listview is *not* a list: it is a
// group-like composite that attaches a scrollbar and input handling to a list
// child. The child list is supplied through MUIA_Listview_List or created
// internally; either way it is adopted through the Family seam so it is owned
// and released with the listview. Construction is failure-atomic: any failure
// after the child is adopted disposes the whole listview (and with it the
// child), and an internally created child that never gets adopted is disposed
// explicitly. No managed allocations are used; every step goes through the
// guest-memory object/family seams shared with MG07.
public static class MuiListviewCore
{
	// MUIA_Listview_List is a getter-only child relationship after
	// construction. Keep the adopted child in a named guest record so child
	// lookup and teardown do not depend on a raw public attribute word.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListviewChildState
	{
		internal const uint Size = 8;
		internal const uint Cookie = 0x4C564C53u; // 'LVLS'
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint ChildOffset = 4;

		internal uint Magic;
		internal APTR Child;
	}

	internal enum MuiListviewChildStateField : byte
	{
		Magic,
		Child,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListviewChildStateFieldCursor
	{
		internal APTR Record;
		internal MuiListviewChildStateField Field;
	}

	internal static class MuiListviewChildStateMemoryCodec
	{
		private static bool TryResolve(MuiListviewChildStateField field,
			out uint offset, out uint recordSize)
		{
			recordSize = MuiListviewChildState.Size;
			if (field == MuiListviewChildStateField.Magic)
				offset = MuiListviewChildState.MagicOffset;
			else if (field == MuiListviewChildStateField.Child)
				offset = MuiListviewChildState.ChildOffset;
			else
			{
				offset = 0;
				recordSize = 0;
				return false;
			}
			return true;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewChildStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(field, out var offset, out var recordSize) ||
				record.IsNull || record.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(record, recordSize))
				return false;
			address = APTR.FromPointer(record.Raw + offset);
			return platform.IsMapped(address, MuiListviewChildState.FieldSize);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewChildStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!TryGetAddress(ref platform, record, field, out var address))
				return false;
			value = platform.ReadUInt32(address, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewChildStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!TryGetAddress(ref platform, record, field, out var address))
				return false;
			platform.WriteUInt32(address, 0, value);
			return true;
		}
	}

	internal static class MuiListviewChildStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListviewChildStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewChildStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Record, cursor.Field, out address);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewChildStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewChildStateMemoryCodec.TryReadUInt32(ref platform, record,
				field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewChildStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewChildStateMemoryCodec.TryWriteUInt32(ref platform, record,
				field, value);
	}

	internal static class MuiListviewChildStateCodec
	{
		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListviewChildState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewChildState.Size) ||
				!MuiListviewChildStateMemoryCodec.TryReadUInt32(ref platform,
					address, MuiListviewChildStateField.Magic, out var magic) ||
				!MuiListviewChildStateMemoryCodec.TryReadUInt32(ref platform,
					address, MuiListviewChildStateField.Child, out var child))
				return false;
			value.Magic = magic;
			value.Child = APTR.FromPointer(child);
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListviewChildState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, address, out value) &&
			value.Magic == MuiListviewChildState.Cookie;

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListviewChildState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewChildState.Size) ||
				value.Magic != MuiListviewChildState.Cookie) return false;
			return MuiListviewChildStateMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiListviewChildStateField.Magic,
				value.Magic) &&
				MuiListviewChildStateMemoryCodec.TryWriteUInt32(ref platform,
					address, MuiListviewChildStateField.Child, value.Child.Raw);
		}

		internal static bool Clear<TPlatform>(ref TPlatform platform,
			APTR address) where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewChildState.Size)) return false;
			platform.Clear(address, MuiListviewChildState.Size);
			return true;
		}
	}

	// Click publication is state, not a collection of loosely related raw
	// attributes. Keep the current column, click count, and edge-triggered
	// flags in one guest-resident record so disposal and getters cannot drift
	// apart from the Listview input path.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListviewClickState
	{
		public const uint Size = 24;
		public const uint Cookie = 0x4C56434Bu; // 'LVCK'
		public const uint FieldSize = 4;
		public const uint MagicOffset = 0;
		public const uint ClickColumnOffset = 4;
		public const uint DoubleClickOffset = 8;
		public const uint AgainClickOffset = 12;
		public const uint ClicksOffset = 16;
		public const uint DefClickColumnOffset = 20;
		public uint Magic;
		public uint ClickColumn;
		public uint DoubleClick;
		public uint AgainClick;
		public uint Clicks;
		public uint DefClickColumn;
	}

	internal enum MuiListviewClickStateField : byte
	{
		Magic,
		ClickColumn,
		DoubleClick,
		AgainClick,
		Clicks,
		DefClickColumn,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListviewClickStateFieldCursor
	{
		internal APTR Record;
		internal MuiListviewClickStateField Field;
	}

	internal static class MuiListviewClickStateMemoryCodec
	{
		private static bool TryResolve(MuiListviewClickStateField field,
			out uint offset, out uint recordSize)
		{
			recordSize = MuiListviewClickState.Size;
			if (field == MuiListviewClickStateField.Magic)
				offset = MuiListviewClickState.MagicOffset;
			else if (field == MuiListviewClickStateField.ClickColumn)
				offset = MuiListviewClickState.ClickColumnOffset;
			else if (field == MuiListviewClickStateField.DoubleClick)
				offset = MuiListviewClickState.DoubleClickOffset;
			else if (field == MuiListviewClickStateField.AgainClick)
				offset = MuiListviewClickState.AgainClickOffset;
			else if (field == MuiListviewClickStateField.Clicks)
				offset = MuiListviewClickState.ClicksOffset;
			else if (field == MuiListviewClickStateField.DefClickColumn)
				offset = MuiListviewClickState.DefClickColumnOffset;
			else
			{
				offset = 0;
				recordSize = 0;
				return false;
			}
			return true;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewClickStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(field, out var offset, out var recordSize) ||
				record.IsNull || record.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(record, recordSize))
				return false;
			address = APTR.FromPointer(record.Raw + offset);
			return platform.IsMapped(address, MuiListviewClickState.FieldSize);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewClickStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!TryGetAddress(ref platform, record, field, out var address))
				return false;
			value = platform.ReadUInt32(address, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewClickStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!TryGetAddress(ref platform, record, field, out var address))
				return false;
			platform.WriteUInt32(address, 0, value);
			return true;
		}
	}

	internal static class MuiListviewClickStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListviewClickStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewClickStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Record, cursor.Field, out address);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewClickStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewClickStateMemoryCodec.TryReadUInt32(ref platform, record,
				field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewClickStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewClickStateMemoryCodec.TryWriteUInt32(ref platform, record,
				field, value);
	}

	internal static class MuiListviewClickStateCodec
	{
		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListviewClickState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewClickState.Size) ||
				!MuiListviewClickStateMemoryCodec.TryReadUInt32(ref platform,
					address, MuiListviewClickStateField.Magic, out var magic) ||
				!MuiListviewClickStateMemoryCodec.TryReadUInt32(ref platform,
					address, MuiListviewClickStateField.ClickColumn,
					out value.ClickColumn) ||
				!MuiListviewClickStateMemoryCodec.TryReadUInt32(ref platform,
					address, MuiListviewClickStateField.DoubleClick,
					out value.DoubleClick) ||
				!MuiListviewClickStateMemoryCodec.TryReadUInt32(ref platform,
					address, MuiListviewClickStateField.AgainClick,
					out value.AgainClick) ||
				!MuiListviewClickStateMemoryCodec.TryReadUInt32(ref platform,
					address, MuiListviewClickStateField.Clicks, out value.Clicks) ||
				!MuiListviewClickStateMemoryCodec.TryReadUInt32(ref platform,
					address, MuiListviewClickStateField.DefClickColumn,
					out value.DefClickColumn)) return false;
			value.Magic = magic;
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListviewClickState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!TryReadStructural(ref platform, address, out value) ||
				value.Magic != MuiListviewClickState.Cookie) return false;
			// Preserve the guest row exactly.  BOOL canonicalization belongs to
			// semantic admission, not to the struct codec; otherwise a corrupted
			// published record could be normalized before consumers can reject it.
			return true;
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListviewClickState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewClickState.Size) ||
				value.Magic != MuiListviewClickState.Cookie) return false;
			return MuiListviewClickStateMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiListviewClickStateField.Magic, value.Magic) &&
				MuiListviewClickStateMemoryCodec.TryWriteUInt32(ref platform,
					address, MuiListviewClickStateField.ClickColumn,
					value.ClickColumn) &&
				MuiListviewClickStateMemoryCodec.TryWriteUInt32(ref platform,
					address, MuiListviewClickStateField.DoubleClick,
					value.DoubleClick == 0 ? 0u : 1u) &&
				MuiListviewClickStateMemoryCodec.TryWriteUInt32(ref platform,
					address, MuiListviewClickStateField.AgainClick,
					value.AgainClick == 0 ? 0u : 1u) &&
				MuiListviewClickStateMemoryCodec.TryWriteUInt32(ref platform,
					address, MuiListviewClickStateField.Clicks, value.Clicks) &&
				MuiListviewClickStateMemoryCodec.TryWriteUInt32(ref platform,
					address, MuiListviewClickStateField.DefClickColumn,
					value.DefClickColumn);
		}

		internal static bool Clear<TPlatform>(ref TPlatform platform,
			APTR address) where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewClickState.Size)) return false;
			platform.Clear(address, MuiListviewClickState.Size);
			return true;
		}
	}

	// Listview interaction policy is one coherent construction/runtime state,
	// not four unrelated attribute slots.  Keep the MorphOS policy values in a
	// named guest record so pointer input, keyboard input, layout, and drag
	// teardown all consume the same normalized snapshot.  The public attributes
	// remain available for ABI compatibility; this record is the authoritative
	// implementation state and contains no managed references or hidden offsets.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListviewInteractionPolicyState
	{
		internal const uint Size = 20;
		internal const uint Cookie = 0x4C56504Fu; // 'LVPO'
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint InputOffset = 4;
		internal const uint MultiSelectOffset = 8;
		internal const uint ScrollerPosOffset = 12;
		internal const uint DragTypeOffset = 16;

		internal uint Magic;
		internal uint Input;
		internal uint MultiSelect;
		internal uint ScrollerPos;
		internal uint DragType;
	}

	internal enum MuiListviewInteractionPolicyField : byte
	{
		Magic,
		Input,
		MultiSelect,
		ScrollerPos,
		DragType,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListviewInteractionPolicyFieldCursor
	{
		internal APTR Record;
		internal MuiListviewInteractionPolicyField Field;
	}

	internal static class MuiListviewInteractionPolicyMemoryCodec
	{
		private static bool TryResolve(
			MuiListviewInteractionPolicyField field, out uint offset,
			out uint recordSize)
		{
			recordSize = MuiListviewInteractionPolicyState.Size;
			if (field == MuiListviewInteractionPolicyField.Magic)
				offset = MuiListviewInteractionPolicyState.MagicOffset;
			else if (field == MuiListviewInteractionPolicyField.Input)
				offset = MuiListviewInteractionPolicyState.InputOffset;
			else if (field == MuiListviewInteractionPolicyField.MultiSelect)
				offset = MuiListviewInteractionPolicyState.MultiSelectOffset;
			else if (field == MuiListviewInteractionPolicyField.ScrollerPos)
				offset = MuiListviewInteractionPolicyState.ScrollerPosOffset;
			else if (field == MuiListviewInteractionPolicyField.DragType)
				offset = MuiListviewInteractionPolicyState.DragTypeOffset;
			else
			{
				offset = 0;
				recordSize = 0;
				return false;
			}
			return true;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewInteractionPolicyField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(field, out var offset, out var recordSize) ||
				record.IsNull || record.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(record, recordSize)) return false;
			address = APTR.FromPointer(record.Raw + offset);
			return platform.IsMapped(address,
				MuiListviewInteractionPolicyState.FieldSize);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewInteractionPolicyField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!TryGetAddress(ref platform, record, field, out var address))
				return false;
			value = platform.ReadUInt32(address, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewInteractionPolicyField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!TryGetAddress(ref platform, record, field, out var address))
				return false;
			platform.WriteUInt32(address, 0, value);
			return true;
		}
	}

	internal static class MuiListviewInteractionPolicyFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListviewInteractionPolicyFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewInteractionPolicyMemoryCodec.TryGetAddress(ref platform,
				cursor.Record, cursor.Field, out address);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewInteractionPolicyField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewInteractionPolicyMemoryCodec.TryReadUInt32(ref platform,
				record, field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewInteractionPolicyField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewInteractionPolicyMemoryCodec.TryWriteUInt32(ref platform,
				record, field, value);
	}

	internal static class MuiListviewInteractionPolicyStateCodec
	{
		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform, APTR address,
			out MuiListviewInteractionPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewInteractionPolicyState.Size) ||
				!MuiListviewInteractionPolicyMemoryCodec.TryReadUInt32(
					ref platform, address,
					MuiListviewInteractionPolicyField.Magic, out var magic) ||
				!MuiListviewInteractionPolicyMemoryCodec.TryReadUInt32(
					ref platform, address,
					MuiListviewInteractionPolicyField.Input, out value.Input) ||
				!MuiListviewInteractionPolicyMemoryCodec.TryReadUInt32(
					ref platform, address,
					MuiListviewInteractionPolicyField.MultiSelect,
					out value.MultiSelect) ||
				!MuiListviewInteractionPolicyMemoryCodec.TryReadUInt32(
					ref platform, address,
					MuiListviewInteractionPolicyField.ScrollerPos,
					out value.ScrollerPos) ||
				!MuiListviewInteractionPolicyMemoryCodec.TryReadUInt32(
					ref platform, address,
					MuiListviewInteractionPolicyField.DragType,
					out value.DragType)) return false;
			value.Magic = magic;
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
			out MuiListviewInteractionPolicyState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, address, out value) &&
			value.Magic == MuiListviewInteractionPolicyState.Cookie;

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
			MuiListviewInteractionPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewInteractionPolicyState.Size) ||
				value.Magic != MuiListviewInteractionPolicyState.Cookie)
				return false;
			return MuiListviewInteractionPolicyMemoryCodec.TryWriteUInt32(
				ref platform, address,
				MuiListviewInteractionPolicyField.Magic, value.Magic) &&
				MuiListviewInteractionPolicyMemoryCodec.TryWriteUInt32(
					ref platform, address,
					MuiListviewInteractionPolicyField.Input, value.Input) &&
				MuiListviewInteractionPolicyMemoryCodec.TryWriteUInt32(
					ref platform, address,
					MuiListviewInteractionPolicyField.MultiSelect,
					value.MultiSelect) &&
				MuiListviewInteractionPolicyMemoryCodec.TryWriteUInt32(
					ref platform, address,
					MuiListviewInteractionPolicyField.ScrollerPos,
					value.ScrollerPos) &&
				MuiListviewInteractionPolicyMemoryCodec.TryWriteUInt32(
					ref platform, address,
					MuiListviewInteractionPolicyField.DragType, value.DragType);
		}
	}

	// MUIA_Listview_SelectChange is a getter-only edge signal mirrored from the
	// owned List. Keep that composite projection in a named guest record rather
	// than rereading the child's scalar attribute, so a Listview notification
	// remains stable even while the child is being mutated or torn down.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListviewSelectionSignalState
	{
		internal const uint Size = 8;
		internal const uint Cookie = 0x4C565343u; // 'LVSC'
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint ValueOffset = 4;

		internal uint Magic;
		internal uint Value;
	}

	internal enum MuiListviewSelectionSignalField : byte
	{
		Magic,
		Value,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListviewSelectionSignalFieldCursor
	{
		internal APTR Record;
		internal MuiListviewSelectionSignalField Field;
	}

	internal static class MuiListviewSelectionSignalMemoryCodec
	{
		private static bool TryResolve(MuiListviewSelectionSignalField field,
			out uint offset, out uint recordSize)
		{
			recordSize = MuiListviewSelectionSignalState.Size;
			if (field == MuiListviewSelectionSignalField.Magic)
				offset = MuiListviewSelectionSignalState.MagicOffset;
			else if (field == MuiListviewSelectionSignalField.Value)
				offset = MuiListviewSelectionSignalState.ValueOffset;
			else
			{
				offset = 0;
				recordSize = 0;
				return false;
			}
			return true;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewSelectionSignalField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(field, out var offset, out var recordSize) ||
				record.IsNull || record.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(record, recordSize)) return false;
			address = APTR.FromPointer(record.Raw + offset);
			return platform.IsMapped(address,
				MuiListviewSelectionSignalState.FieldSize);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewSelectionSignalField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!TryGetAddress(ref platform, record, field, out var address))
				return false;
			value = platform.ReadUInt32(address, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewSelectionSignalField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!TryGetAddress(ref platform, record, field, out var address))
				return false;
			platform.WriteUInt32(address, 0, value);
			return true;
		}
	}

	internal static class MuiListviewSelectionSignalFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListviewSelectionSignalFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewSelectionSignalMemoryCodec.TryGetAddress(ref platform,
				cursor.Record, cursor.Field, out address);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewSelectionSignalField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewSelectionSignalMemoryCodec.TryReadUInt32(ref platform,
				record, field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewSelectionSignalField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewSelectionSignalMemoryCodec.TryWriteUInt32(ref platform,
				record, field, value);
	}

	internal static class MuiListviewSelectionSignalStateCodec
	{
		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform, APTR address,
			out MuiListviewSelectionSignalState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewSelectionSignalState.Size) ||
				!MuiListviewSelectionSignalMemoryCodec.TryReadUInt32(
					ref platform, address,
					MuiListviewSelectionSignalField.Magic, out var magic) ||
				!MuiListviewSelectionSignalMemoryCodec.TryReadUInt32(
				ref platform, address,
				MuiListviewSelectionSignalField.Value, out value.Value)) return false;
			value.Magic = magic;
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
			out MuiListviewSelectionSignalState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, address, out value) &&
			value.Magic == MuiListviewSelectionSignalState.Cookie;

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
			MuiListviewSelectionSignalState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewSelectionSignalState.Size) ||
				value.Magic != MuiListviewSelectionSignalState.Cookie)
				return false;
			return MuiListviewSelectionSignalMemoryCodec.TryWriteUInt32(
				ref platform, address,
				MuiListviewSelectionSignalField.Magic, value.Magic) &&
				MuiListviewSelectionSignalMemoryCodec.TryWriteUInt32(
					ref platform, address,
					MuiListviewSelectionSignalField.Value, value.Value);
		}
	}

	// The composite and its adopted List have one effective rectangle after
	// layout. Keep both signed geometries together so scrollbar drawing, hit
	// testing, and drag targeting do not reread unrelated Area attributes.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListviewLayoutState
	{
		internal const uint Size = 36;
		internal const uint Cookie = 0x4C564754u; // 'LVGT'
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint LeftOffset = 4;
		internal const uint TopOffset = 8;
		internal const uint WidthOffset = 12;
		internal const uint HeightOffset = 16;
		internal const uint ChildLeftOffset = 20;
		internal const uint ChildTopOffset = 24;
		internal const uint ChildWidthOffset = 28;
		internal const uint ChildHeightOffset = 32;

		internal uint Magic;
		internal int Left;
		internal int Top;
		internal int Width;
		internal int Height;
		internal int ChildLeft;
		internal int ChildTop;
		internal int ChildWidth;
		internal int ChildHeight;
	}

	internal enum MuiListviewLayoutField : byte
	{
		Magic,
		Left,
		Top,
		Width,
		Height,
		ChildLeft,
		ChildTop,
		ChildWidth,
		ChildHeight,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListviewLayoutFieldCursor
	{
		internal APTR Record;
		internal MuiListviewLayoutField Field;
	}

	internal static class MuiListviewLayoutMemoryCodec
	{
		private static bool TryResolve(MuiListviewLayoutField field,
			out uint offset, out uint recordSize)
		{
			recordSize = MuiListviewLayoutState.Size;
			if (field == MuiListviewLayoutField.Magic)
				offset = MuiListviewLayoutState.MagicOffset;
			else if (field == MuiListviewLayoutField.Left)
				offset = MuiListviewLayoutState.LeftOffset;
			else if (field == MuiListviewLayoutField.Top)
				offset = MuiListviewLayoutState.TopOffset;
			else if (field == MuiListviewLayoutField.Width)
				offset = MuiListviewLayoutState.WidthOffset;
			else if (field == MuiListviewLayoutField.Height)
				offset = MuiListviewLayoutState.HeightOffset;
			else if (field == MuiListviewLayoutField.ChildLeft)
				offset = MuiListviewLayoutState.ChildLeftOffset;
			else if (field == MuiListviewLayoutField.ChildTop)
				offset = MuiListviewLayoutState.ChildTopOffset;
			else if (field == MuiListviewLayoutField.ChildWidth)
				offset = MuiListviewLayoutState.ChildWidthOffset;
			else if (field == MuiListviewLayoutField.ChildHeight)
				offset = MuiListviewLayoutState.ChildHeightOffset;
			else
			{
				offset = 0;
				recordSize = 0;
				return false;
			}
			return true;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewLayoutField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(field, out var offset, out var recordSize) ||
				record.IsNull || record.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(record, recordSize))
				return false;
			address = APTR.FromPointer(record.Raw + offset);
			return platform.IsMapped(address, MuiListviewLayoutState.FieldSize);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewLayoutField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!TryGetAddress(ref platform, record, field, out var address))
				return false;
			value = platform.ReadUInt32(address, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewLayoutField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!TryGetAddress(ref platform, record, field, out var address))
				return false;
			platform.WriteUInt32(address, 0, value);
			return true;
		}

		internal static bool TryReadInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewLayoutField field, out int value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!TryReadUInt32(ref platform, record, field, out var raw))
				return false;
			value = unchecked((int)raw);
			return true;
		}

		internal static bool TryWriteInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewLayoutField field, int value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryWriteUInt32(ref platform, record, field, unchecked((uint)value));
	}

	internal static class MuiListviewLayoutFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListviewLayoutFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewLayoutMemoryCodec.TryGetAddress(ref platform,
				cursor.Record, cursor.Field, out address);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewLayoutField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewLayoutMemoryCodec.TryReadUInt32(ref platform, record,
				field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewLayoutField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewLayoutMemoryCodec.TryWriteUInt32(ref platform, record,
				field, value);

		internal static bool TryReadInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewLayoutField field, out int value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewLayoutMemoryCodec.TryReadInt32(ref platform, record,
				field, out value);

		internal static bool TryWriteInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewLayoutField field, int value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewLayoutMemoryCodec.TryWriteInt32(ref platform, record,
				field, value);
	}

	internal static class MuiListviewLayoutStateCodec
	{
		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListviewLayoutState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewLayoutState.Size) ||
				!MuiListviewLayoutMemoryCodec.TryReadUInt32(ref platform,
					address, MuiListviewLayoutField.Magic, out var magic) ||
				!MuiListviewLayoutMemoryCodec.TryReadInt32(ref platform,
					address, MuiListviewLayoutField.Left, out value.Left) ||
				!MuiListviewLayoutMemoryCodec.TryReadInt32(ref platform,
					address, MuiListviewLayoutField.Top, out value.Top) ||
				!MuiListviewLayoutMemoryCodec.TryReadInt32(ref platform,
					address, MuiListviewLayoutField.Width, out value.Width) ||
				!MuiListviewLayoutMemoryCodec.TryReadInt32(ref platform,
					address, MuiListviewLayoutField.Height, out value.Height) ||
				!MuiListviewLayoutMemoryCodec.TryReadInt32(ref platform,
					address, MuiListviewLayoutField.ChildLeft,
					out value.ChildLeft) ||
				!MuiListviewLayoutMemoryCodec.TryReadInt32(ref platform,
					address, MuiListviewLayoutField.ChildTop,
					out value.ChildTop) ||
				!MuiListviewLayoutMemoryCodec.TryReadInt32(ref platform,
					address, MuiListviewLayoutField.ChildWidth,
					out value.ChildWidth) ||
				!MuiListviewLayoutMemoryCodec.TryReadInt32(ref platform,
					address, MuiListviewLayoutField.ChildHeight,
					out value.ChildHeight)) return false;
			value.Magic = magic;
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListviewLayoutState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, address, out value) &&
			value.Magic == MuiListviewLayoutState.Cookie;

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListviewLayoutState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewLayoutState.Size) || value.Magic !=
				MuiListviewLayoutState.Cookie) return false;
			return MuiListviewLayoutMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiListviewLayoutField.Magic, value.Magic) &&
				MuiListviewLayoutMemoryCodec.TryWriteInt32(ref platform,
					address, MuiListviewLayoutField.Left, value.Left) &&
				MuiListviewLayoutMemoryCodec.TryWriteInt32(ref platform,
					address, MuiListviewLayoutField.Top, value.Top) &&
				MuiListviewLayoutMemoryCodec.TryWriteInt32(ref platform,
					address, MuiListviewLayoutField.Width, value.Width) &&
				MuiListviewLayoutMemoryCodec.TryWriteInt32(ref platform,
					address, MuiListviewLayoutField.Height, value.Height) &&
				MuiListviewLayoutMemoryCodec.TryWriteInt32(ref platform,
					address, MuiListviewLayoutField.ChildLeft, value.ChildLeft) &&
				MuiListviewLayoutMemoryCodec.TryWriteInt32(ref platform,
					address, MuiListviewLayoutField.ChildTop, value.ChildTop) &&
				MuiListviewLayoutMemoryCodec.TryWriteInt32(ref platform,
					address, MuiListviewLayoutField.ChildWidth, value.ChildWidth) &&
				MuiListviewLayoutMemoryCodec.TryWriteInt32(ref platform,
					address, MuiListviewLayoutField.ChildHeight, value.ChildHeight);
		}
	}

	// RenderInfo is shared by the composite and its adopted List child. Keep
	// the decoded RastPort beside the public pointer so draw and child-binding
	// paths use one validated context rather than separate raw reads.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListviewRenderState
	{
		internal const uint Size = 12;
		internal const uint Cookie = 0x4C565254u; // 'LVRT'
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint RenderInfoOffset = 4;
		internal const uint RastPortOffset = 8;

		internal uint Magic;
		internal APTR RenderInfo;
		internal APTR RastPort;
	}

	internal enum MuiListviewRenderField : byte
	{
		Magic,
		RenderInfo,
		RastPort,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListviewRenderFieldCursor
	{
		internal APTR Record;
		internal MuiListviewRenderField Field;
	}

	internal static class MuiListviewRenderMemoryCodec
	{
		private static bool TryResolve(MuiListviewRenderField field,
			out uint offset, out uint recordSize)
		{
			recordSize = MuiListviewRenderState.Size;
			if (field == MuiListviewRenderField.Magic)
				offset = MuiListviewRenderState.MagicOffset;
			else if (field == MuiListviewRenderField.RenderInfo)
				offset = MuiListviewRenderState.RenderInfoOffset;
			else if (field == MuiListviewRenderField.RastPort)
				offset = MuiListviewRenderState.RastPortOffset;
			else
			{
				offset = 0;
				recordSize = 0;
				return false;
			}
			return true;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewRenderField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(field, out var offset, out var recordSize) ||
				record.IsNull || record.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(record, recordSize))
				return false;
			address = APTR.FromPointer(record.Raw + offset);
			return platform.IsMapped(address, MuiListviewRenderState.FieldSize);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewRenderField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!TryGetAddress(ref platform, record, field, out var address))
				return false;
			value = platform.ReadUInt32(address, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewRenderField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!TryGetAddress(ref platform, record, field, out var address))
				return false;
			platform.WriteUInt32(address, 0, value);
			return true;
		}
	}

	internal static class MuiListviewRenderFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListviewRenderFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewRenderMemoryCodec.TryGetAddress(ref platform,
				cursor.Record, cursor.Field, out address);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewRenderField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewRenderMemoryCodec.TryReadUInt32(ref platform, record,
				field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewRenderField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewRenderMemoryCodec.TryWriteUInt32(ref platform, record,
				field, value);
	}

	internal static class MuiListviewRenderStateCodec
	{
		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListviewRenderState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewRenderState.Size) ||
				!MuiListviewRenderMemoryCodec.TryReadUInt32(ref platform,
					address, MuiListviewRenderField.Magic, out var magic) ||
				!MuiListviewRenderMemoryCodec.TryReadUInt32(ref platform,
					address, MuiListviewRenderField.RenderInfo, out var info) ||
				!MuiListviewRenderMemoryCodec.TryReadUInt32(ref platform,
					address, MuiListviewRenderField.RastPort, out var rastPort))
				return false;
			value.Magic = magic;
			value.RenderInfo = APTR.FromPointer(info);
			value.RastPort = APTR.FromPointer(rastPort);
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListviewRenderState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, address, out value) &&
			value.Magic == MuiListviewRenderState.Cookie;

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListviewRenderState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewRenderState.Size) || value.Magic !=
				MuiListviewRenderState.Cookie) return false;
			return MuiListviewRenderMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiListviewRenderField.Magic, value.Magic) &&
				MuiListviewRenderMemoryCodec.TryWriteUInt32(ref platform,
					address, MuiListviewRenderField.RenderInfo,
					value.RenderInfo.Raw) &&
				MuiListviewRenderMemoryCodec.TryWriteUInt32(ref platform,
					address, MuiListviewRenderField.RastPort,
					value.RastPort.Raw);
		}
	}

	// Derived vertical scroller projection. Keep the child entry count and
	// bounded row cursor together so geometry, keyboard input, and pointer
	// dragging consume the same named state after each child/layout update.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListviewScrollerState
	{
		internal const uint Size = 20;
		internal const uint Cookie = 0x4C565352u; // 'LVSR'
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint EntriesOffset = 4;
		internal const uint VisibleOffset = 8;
		internal const uint FirstOffset = 12;
		internal const uint MaxFirstOffset = 16;

		internal uint Magic;
		internal uint Entries;
		internal uint Visible;
		internal uint First;
		internal uint MaxFirst;
	}

	// Pixel-range projection used by an application-owned MorphOS Prop.  The
	// Listview documentation connects that Prop to the child List's
	// TopPixel/VisiblePixel/TotalPixel attributes; keep the three values named so
	// the connection never depends on private attribute or object offsets.
	internal struct MuiListviewExternalScrollerState
	{
		internal uint Entries;
		internal uint Visible;
		internal uint First;
	}

	// A Listview may own a live MorphOS notification recipe for an
	// application-owned Prop/Scrollbar. Keep only the destination pointer in a
	// named guest record so disposal can remove the recipe without a managed
	// connection table or an object-layout offset convention.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListviewExternalScrollerConnectionState
	{
		internal const uint Size = 8;
		internal const uint Cookie = 0x4C564543u; // 'LVEC'
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint PropOffset = 4;

		internal uint Magic;
		internal APTR Prop;
	}

	internal enum MuiListviewExternalScrollerConnectionField : byte
	{
		Magic,
		Prop,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListviewExternalScrollerConnectionFieldCursor
	{
		internal APTR Record;
		internal MuiListviewExternalScrollerConnectionField Field;
	}

	internal static class MuiListviewExternalScrollerConnectionMemoryCodec
	{
		private static bool TryResolve(
			MuiListviewExternalScrollerConnectionField field,
			out uint offset, out uint recordSize)
		{
			recordSize = MuiListviewExternalScrollerConnectionState.Size;
			if (field == MuiListviewExternalScrollerConnectionField.Magic)
				offset = MuiListviewExternalScrollerConnectionState.MagicOffset;
			else if (field == MuiListviewExternalScrollerConnectionField.Prop)
				offset = MuiListviewExternalScrollerConnectionState.PropOffset;
			else
			{
				offset = 0;
				recordSize = 0;
				return false;
			}
			return true;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewExternalScrollerConnectionField field,
			out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(field, out var offset, out var recordSize) ||
				record.IsNull || record.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(record, recordSize)) return false;
			address = APTR.FromPointer(record.Raw + offset);
			return platform.IsMapped(address,
				MuiListviewExternalScrollerConnectionState.FieldSize);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record,
			MuiListviewExternalScrollerConnectionField field,
			out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!TryGetAddress(ref platform, record, field, out var address))
				return false;
			value = platform.ReadUInt32(address, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record,
			MuiListviewExternalScrollerConnectionField field,
			uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!TryGetAddress(ref platform, record, field, out var address))
				return false;
			platform.WriteUInt32(address, 0, value);
			return true;
		}
	}

	internal static class MuiListviewExternalScrollerConnectionFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListviewExternalScrollerConnectionFieldCursor cursor,
			out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewExternalScrollerConnectionMemoryCodec.TryGetAddress(
				ref platform, cursor.Record, cursor.Field, out address);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record,
			MuiListviewExternalScrollerConnectionField field,
			out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewExternalScrollerConnectionMemoryCodec.TryReadUInt32(
				ref platform, record, field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record,
			MuiListviewExternalScrollerConnectionField field,
			uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewExternalScrollerConnectionMemoryCodec.TryWriteUInt32(
				ref platform, record, field, value);
	}

	internal static class MuiListviewExternalScrollerConnectionStateCodec
	{
		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListviewExternalScrollerConnectionState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewExternalScrollerConnectionState.Size)) return false;
			if (!MuiListviewExternalScrollerConnectionMemoryCodec.TryReadUInt32(
				ref platform, address,
				MuiListviewExternalScrollerConnectionField.Magic,
				out value.Magic)) return false;
			if (!MuiListviewExternalScrollerConnectionMemoryCodec.TryReadUInt32(
				ref platform, address,
				MuiListviewExternalScrollerConnectionField.Prop,
				out var prop)) return false;
			value.Prop = APTR.FromPointer(prop);
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListviewExternalScrollerConnectionState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, address, out value) &&
			value.Magic == MuiListviewExternalScrollerConnectionState.Cookie;

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListviewExternalScrollerConnectionState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewExternalScrollerConnectionState.Size) ||
				value.Magic != MuiListviewExternalScrollerConnectionState.Cookie)
				return false;
			return MuiListviewExternalScrollerConnectionMemoryCodec
				.TryWriteUInt32(ref platform, address,
					MuiListviewExternalScrollerConnectionField.Magic, value.Magic) &&
				MuiListviewExternalScrollerConnectionMemoryCodec
				.TryWriteUInt32(ref platform, address,
					MuiListviewExternalScrollerConnectionField.Prop, value.Prop.Raw);
		}

		internal static void Clear<TPlatform>(ref TPlatform platform, APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNotNull && platform.IsMapped(address,
				MuiListviewExternalScrollerConnectionState.Size))
				platform.Clear(address,
					MuiListviewExternalScrollerConnectionState.Size);
		}
	}

	internal enum MuiListviewScrollerField : byte
	{
		Magic,
		Entries,
		Visible,
		First,
		MaxFirst,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListviewScrollerFieldCursor
	{
		internal APTR Record;
		internal MuiListviewScrollerField Field;
	}

	internal static class MuiListviewScrollerMemoryCodec
	{
		private static bool TryResolve(MuiListviewScrollerField field,
			out uint offset, out uint recordSize)
		{
			recordSize = MuiListviewScrollerState.Size;
			if (field == MuiListviewScrollerField.Magic)
				offset = MuiListviewScrollerState.MagicOffset;
			else if (field == MuiListviewScrollerField.Entries)
				offset = MuiListviewScrollerState.EntriesOffset;
			else if (field == MuiListviewScrollerField.Visible)
				offset = MuiListviewScrollerState.VisibleOffset;
			else if (field == MuiListviewScrollerField.First)
				offset = MuiListviewScrollerState.FirstOffset;
			else if (field == MuiListviewScrollerField.MaxFirst)
				offset = MuiListviewScrollerState.MaxFirstOffset;
			else
			{
				offset = 0;
				recordSize = 0;
				return false;
			}
			return true;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewScrollerField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(field, out var offset, out var recordSize) ||
				record.IsNull || record.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(record, recordSize))
				return false;
			address = APTR.FromPointer(record.Raw + offset);
			return platform.IsMapped(address, MuiListviewScrollerState.FieldSize);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewScrollerField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!TryGetAddress(ref platform, record, field, out var address))
				return false;
			value = platform.ReadUInt32(address, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewScrollerField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!TryGetAddress(ref platform, record, field, out var address))
				return false;
			platform.WriteUInt32(address, 0, value);
			return true;
		}
	}

	internal static class MuiListviewScrollerFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListviewScrollerFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewScrollerMemoryCodec.TryGetAddress(ref platform,
				cursor.Record, cursor.Field, out address);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewScrollerField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewScrollerMemoryCodec.TryReadUInt32(ref platform, record,
				field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewScrollerField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListviewScrollerMemoryCodec.TryWriteUInt32(ref platform, record,
				field, value);
	}

	internal static class MuiListviewScrollerStateCodec
	{
		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListviewScrollerState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewScrollerState.Size) ||
				!MuiListviewScrollerMemoryCodec.TryReadUInt32(ref platform,
					address, MuiListviewScrollerField.Magic, out var magic) ||
				!MuiListviewScrollerMemoryCodec.TryReadUInt32(ref platform,
				address, MuiListviewScrollerField.Entries, out value.Entries) ||
				!MuiListviewScrollerMemoryCodec.TryReadUInt32(ref platform,
					address, MuiListviewScrollerField.Visible, out value.Visible) ||
				!MuiListviewScrollerMemoryCodec.TryReadUInt32(ref platform,
					address, MuiListviewScrollerField.First, out value.First) ||
				!MuiListviewScrollerMemoryCodec.TryReadUInt32(ref platform,
					address, MuiListviewScrollerField.MaxFirst, out value.MaxFirst)) return false;
			value.Magic = magic;
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListviewScrollerState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, address, out value) &&
			value.Magic == MuiListviewScrollerState.Cookie;

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListviewScrollerState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewScrollerState.Size) || value.Magic !=
				MuiListviewScrollerState.Cookie) return false;
			return MuiListviewScrollerMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiListviewScrollerField.Magic, value.Magic) &&
				MuiListviewScrollerMemoryCodec.TryWriteUInt32(ref platform,
					address, MuiListviewScrollerField.Entries, value.Entries) &&
				MuiListviewScrollerMemoryCodec.TryWriteUInt32(ref platform,
					address, MuiListviewScrollerField.Visible, value.Visible) &&
				MuiListviewScrollerMemoryCodec.TryWriteUInt32(ref platform,
					address, MuiListviewScrollerField.First, value.First) &&
				MuiListviewScrollerMemoryCodec.TryWriteUInt32(ref platform,
					address, MuiListviewScrollerField.MaxFirst, value.MaxFirst);
		}
	}

	// ---- Listview attribute identifiers (autodoc MUI_Listview.doc) -----------
	private const uint AgainClick = 0x804214c2u;      // [I.G] BOOL
	private const uint ClickColumn = 0x8042d1b3u;     // [..G] LONG
	private const uint DefClickColumn = 0x8042b296u;  // [ISG] LONG
	private const uint DoubleClick = 0x80424635u;     // [I.G] BOOL
	private const uint DragType = 0x80425cd3u;        // [ISG] LONG
	private const uint Input = 0x8042682du;           // [I..] BOOL
	private const uint List = 0x8042bcceu;            // [I.G] Boopsiobject *
	private const uint MultiSelect = 0x80427e08u;     // [I..] LONG
	// MUIA_List_MultiTestHook lives on the child list (autodoc MUI_List.doc);
	// it gates whether a given entry may join a multiselection.
	private const uint MultiTestHook = 0x8042c2c6u;   // [IS.] struct Hook *
	private const uint ScrollerPos = 0x8042b1b4u;     // [I..] BOOL/LONG
	private const uint SelectChange = 0x8042178fu;    // [..G] BOOL (shared w/ List)
	private const uint ClickStateKey = 0x7F090001u;
	private const uint DragStateKey = 0x7F090002u;
	private const uint ScrollerDragStateKey = 0x7F090003u;
	private const uint HorizontalScrollerDragStateKey = 0x7F090004u;
	private const uint InteractionPolicyKey = 0x7F090005u;
	private const uint SelectionSignalKey = 0x7F090006u;
	private const uint ChildStateKey = 0x7F090007u;
	private const uint LayoutStateKey = 0x7F090008u;
	private const uint RenderStateKey = 0x7F090009u;
	private const uint ScrollerStateKey = 0x7F09000Au;
	private const uint HorizontalScrollerStateKey = 0x7F09000Bu;
	private const uint ExternalScrollerConnectionKey = 0x7F09000Cu;

	// ---- MUIV_Listview_* selectors -------------------------------------------
	private const uint MultiSelectNone = 0;
	private const uint MultiSelectDefault = 1;
	private const uint MultiSelectShifted = 2;
	private const uint MultiSelectAlways = 3;
	private const uint ScrollerPosDefault = 0;
	private const uint ScrollerPosLeft = 1;
	private const uint ScrollerPosRight = 2;
	private const uint ScrollerPosNone = 3;
	private const uint DragTypeNone = 0;
	private const uint DragTypeImmediate = 1;

	// List/Area attributes forwarded to or read from the child.
	private const uint ListActive = 0x8042391cu;      // MUIA_List_Active
	private const uint ListEntries = 0x80421654u;     // MUIA_List_Entries
	private const uint ListFirst = 0x804238d4u;       // MUIA_List_First
	private const uint ListVisible = 0x8042191fu;     // MUIA_List_Visible
	private const uint ListLeftEdge = 0x8042bec6u;
	private const uint ListTopEdge = 0x8042509bu;
	private const uint ListWidth = 0x8042b59cu;
	private const uint ListHeight = 0x80423237u;
	private const uint ListDragSortable = 0x80426099u;
	private const uint ListDragType = 0x80425cd3u;
	private const uint RenderInfo = 0x7fff0001u;
	private const uint ListRowHeight = 8;
	private const uint ScrollerWidth = 16;            // reserved scrollbar extent
	private const uint HScrollerHeight = 16;          // reserved bottom extent
	private const uint NotifyEveryTime = 1233727793u;
	private const uint NotifyTriggerValue = 1233727793u;
	private const uint ExternalNotifyFollowBytes = MuiSetAttributeMessage.Size;

	// ---- MUIM_List_* selectors reused for child forwarding -------------------
	private const int SelectAll = -2;
	private const uint SelectOff = 0;
	private const uint SelectOn = 1;
	private const uint SelectToggle = 2;

	// Preprocessed MUIKEY navigation values and the corresponding List active
	// selectors. These are the public MUI keyboard contract; ListCore owns the
	// selector normalization and viewport auto-visible policy.
	private const int KeyPress = 0;
	private const int KeyToggle = 1;
	private const int KeyNone = -1;
	// MUIKEY_RELEASE is the synthetic key MUI emits when the configured
	// MUIKEY_PRESS action is released.  MorphOS keeps it outside the
	// user-configurable range so controls can use it as a cancellation edge.
	private const int KeyRelease = -2;
	private const int KeyUp = 2;
	private const int KeyDown = 3;
	private const int KeyPageUp = 4;
	private const int KeyPageDown = 5;
	private const int KeyTop = 6;
	private const int KeyBottom = 7;
	private const int KeyLeft = 8;
	private const int KeyRight = 9;
	private const int ActiveTop = -2;
	private const int ActiveBottom = -3;
	private const int ActiveUp = -4;
	private const int ActiveDown = -5;
	private const int ActivePageUp = -6;
	private const int ActivePageDown = -7;

	// Intuition mouse-button envelope values used by the pointer part of
	// MUIM_HandleInput.  Selection is committed on SELECTUP, matching the
	// normal MUI gadget activation edge; SELECTDOWN starts the named drag or
	// scroller state machines when their policies allow it.
	private const uint IdcmpMouseButtons = 1u << 3;
	private const uint IdcmpMouseMove = 1u << 2;
	private const ushort SelectDown = 0x0068;
	private const ushort SelectUp = 0x0069;
	// MorphOS/Intuition NewMouse wheel codes from devices/inputevent.h.
	private const ushort WheelUp = 0x007A;
	private const ushort WheelDown = 0x007B;
	private const ushort WheelLeft = 0x007C;
	private const ushort WheelRight = 0x007D;
	private const uint HScrollerKeyStep = 8;
	private const uint VScrollerWheelStep = 1;
	private const ushort QualifierShift = 0x0003;
	private const ushort QualifierControl = 0x0008;
	private const ushort QualifierAlt = 0x0030;
	private const ushort QualifierMultiSelect = QualifierShift |
		QualifierControl | QualifierAlt;

	// ---- Construction / lifecycle --------------------------------------------

	// Create a listview and bind its child list, failure-atomically. Class-aware
	// defaults are applied. If MUIA_Listview_List names an existing list object,
	// that object is adopted; otherwise a fresh "List.mui" child is created and
	// adopted. On any failure the listview (and its adopted child) is disposed.
	public static APTR CreateListview<TPlatform>(ref TPlatform platform, APTR state,
		APTR classRecord, APTR tags) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiListCore.ClassifyRecord(ref platform, classRecord) !=
			MuiCollectionClass.Listview) return APTR.Null;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, state,
			classRecord, tags);
		if (obj.IsNull) return APTR.Null;

		// Resolve the child list: supplied via MUIA_Listview_List or created.
		var supplied = APTR.FromPointer(Read(ref platform, state, obj, List, 0));
		var child = supplied;
		var internallyCreated = false;
		if (child.IsNull)
		{
			child = CreateInternalList(ref platform, state);
			if (child.IsNull)
			{
				MuiCollectionLifecycle.DisposeObject(ref platform, state, obj);
				return APTR.Null;
			}
			internallyCreated = true;
		}
		else if (!IsListBacked(ref platform, state, child))
		{
			// A non-list object cannot serve as the child; fail atomically.
			MuiCollectionLifecycle.DisposeObject(ref platform, state, obj);
			return APTR.Null;
		}

		// Adopt through the Family seam so the child is owned and disposed with
		// the parent (autodoc: the list child is disposed with its parent).
		if (!MuiFamilyCore.AddTail(ref platform, state, obj, child))
		{
			if (internallyCreated)
				MuiCollectionLifecycle.DisposeObject(ref platform, state, child);
			MuiCollectionLifecycle.DisposeObject(ref platform, state, obj);
			return APTR.Null;
		}

		// From here the child is adopted; any failure disposes obj (which
		// disposes the adopted child), keeping the composite failure-atomic.
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, List,
				child.Raw, false) ||
			!SetListviewOwner(ref platform, state, child, obj) ||
			!EnsureChildState(ref platform, state, obj, child) ||
			!ApplyDefaults(ref platform, state, obj))
		{
			MuiCollectionLifecycle.DisposeObject(ref platform, state, obj);
			return APTR.Null;
		}
		return obj;
	}

	private static bool ApplyDefaults<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		EnsureDefault(ref platform, state, obj, Input, 1) &&          // read/write
		EnsureDefault(ref platform, state, obj, MultiSelect,
			MultiSelectDefault) &&
		EnsureDefault(ref platform, state, obj, ScrollerPos,
			ScrollerPosDefault) &&
		EnsureDefault(ref platform, state, obj, DragType, DragTypeNone) &&
		EnsureDefault(ref platform, state, obj, DefClickColumn, 0) &&
		SetInternal(ref platform, state, obj, ClickColumn, 0) &&
		SetInternal(ref platform, state, obj, DoubleClick, 0) &&
		SetInternal(ref platform, state, obj, AgainClick, 0) &&
		NormalizePolicies(ref platform, state, obj) &&
		EnsureClickState(ref platform, state, obj) &&
		EnsureSelectionSignal(ref platform, state, obj);

	private static bool EnsureChildState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR child)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadChildStateAdmission(ref platform, state, obj,
			out var value, out var present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ChildStateKey, 0));
		if (present)
		{
			if (value.Child.Raw != child.Raw) return false;
			return MuiListviewChildStateCodec.Write(ref platform, block, value);
		}
		var fresh = MuiHeadlessMemory.Allocate(ref platform,
			MuiListviewChildState.Size);
		if (fresh.IsNull) return false;
		value = default;
		value.Magic = MuiListviewChildState.Cookie;
		value.Child = child;
		if (!MuiListviewChildStateCodec.Write(ref platform, fresh, value) ||
			!SetInternal(ref platform, state, obj, ChildStateKey, fresh.Raw))
		{
			MuiListviewChildStateCodec.Clear(ref platform, fresh);
			platform.Free(fresh, MuiListviewChildState.Size);
			return false;
		}
		return true;
	}

	// A published child link is authoritative guest state. A non-NULL record
	// that fails the cookie/field contract is malformed, not absence; child
	// lookup must not replace it from the raw List alias.
	private static bool TryReadChildStateAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewChildState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			ChildStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return MuiListviewChildStateCodec.TryRead(ref platform, block,
			out value) && IsValidChildState(ref platform, state, value);
	}

	// The published child link must name a live List-backed object. Keeping this
	// relationship typed prevents a stale or unrelated BOOPSI pointer from
	// re-entering Listview selection, layout, or disposal through the legacy List
	// alias.
	private static bool IsValidChildState<TPlatform>(ref TPlatform platform,
		APTR state, MuiListviewChildState value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		IsLiveChildList(ref platform, state, value.Child);

	internal static bool TryGetChildState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListviewChildState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadChildStateAdmission(ref platform, state, obj, out value,
			out var present)) return false;
		return present;
	}

	private static void FreeChildState<TPlatform>(ref TPlatform platform,
		APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiListviewChildStateCodec.Clear(ref platform, block)) return;
		platform.Free(block, MuiListviewChildState.Size);
	}

	private static uint NormalizePolicy(uint attribute, uint value) =>
		attribute switch
		{
			Input => value == 0 ? 0u : 1u,
			MultiSelect => value <= MultiSelectAlways ? value : MultiSelectDefault,
			ScrollerPos => value <= ScrollerPosNone ? value : ScrollerPosDefault,
			DragType => value == DragTypeImmediate ? DragTypeImmediate : DragTypeNone,
			_ => value,
		};

	private static bool NormalizePolicies<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		EnsureInteractionPolicy(ref platform, state, obj);

	// A policy block is optional only before the Listview has materialised its
	// typed state. Once a non-NULL block is published, malformed bytes are an
	// authoritative failure and must not be repaired from raw scalar aliases.
	private static bool TryReadInteractionPolicyAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewInteractionPolicyState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			InteractionPolicyKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return MuiListviewInteractionPolicyStateCodec.TryRead(ref platform, block,
			out value) && IsValidInteractionPolicy(value);
	}

	// The MorphOS policy fields are bounded enumerations/BOOLs, not arbitrary
	// LONG payloads. Once the named record is published these bounds are part of
	// admission: rejecting a malformed row keeps EnsureInteractionPolicy from
	// normalizing it in place and accidentally hiding corruption behind the raw
	// compatibility attributes.
	private static bool IsValidInteractionPolicy(
		MuiListviewInteractionPolicyState value) =>
		value.Input <= 1 &&
		value.MultiSelect <= MultiSelectAlways &&
		value.ScrollerPos <= ScrollerPosNone &&
		(value.DragType == DragTypeNone ||
			value.DragType == DragTypeImmediate);

	private static bool EnsureInteractionPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadInteractionPolicyAdmission(ref platform, state, obj,
			out var value, out var present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			InteractionPolicyKey, 0));
		if (present)
		{
			// The named record is authoritative after publication. Normalize its
			// fields in place, without importing possibly stale raw aliases.
			value.Magic = MuiListviewInteractionPolicyState.Cookie;
			value.Input = NormalizePolicy(Input, value.Input);
			value.MultiSelect = NormalizePolicy(MultiSelect, value.MultiSelect);
			value.ScrollerPos = NormalizePolicy(ScrollerPos, value.ScrollerPos);
			value.DragType = NormalizePolicy(DragType, value.DragType);
		}
		else
		{
			value = default;
			value.Magic = MuiListviewInteractionPolicyState.Cookie;
			value.Input = NormalizePolicy(Input,
				Read(ref platform, state, obj, Input, 1));
			value.MultiSelect = NormalizePolicy(MultiSelect,
				Read(ref platform, state, obj, MultiSelect, MultiSelectDefault));
			value.ScrollerPos = NormalizePolicy(ScrollerPos,
				Read(ref platform, state, obj, ScrollerPos, ScrollerPosDefault));
			value.DragType = NormalizePolicy(DragType,
				Read(ref platform, state, obj, DragType, DragTypeNone));
		}

		if (!present)
		{
			var fresh = MuiHeadlessMemory.Allocate(ref platform,
				MuiListviewInteractionPolicyState.Size);
			if (fresh.IsNull) return false;
			if (!MuiListviewInteractionPolicyStateCodec.Write(ref platform, fresh,
				value) || !SetInternal(ref platform, state, obj,
					InteractionPolicyKey, fresh.Raw))
			{
				platform.Clear(fresh, MuiListviewInteractionPolicyState.Size);
				platform.Free(fresh, MuiListviewInteractionPolicyState.Size);
				return false;
			}
			block = fresh;
		}
		else if (!MuiListviewInteractionPolicyStateCodec.Write(ref platform, block,
			value)) return false;

		// Keep the public scalar attributes normalized for callers that inspect
		// them directly; behavior below reads the named record instead.
		return SetInternal(ref platform, state, obj, Input, value.Input) &&
			SetInternal(ref platform, state, obj, MultiSelect, value.MultiSelect) &&
			SetInternal(ref platform, state, obj, ScrollerPos, value.ScrollerPos) &&
			SetInternal(ref platform, state, obj, DragType, value.DragType);
	}

	internal static bool TryGetInteractionPolicy<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewInteractionPolicyState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadInteractionPolicyAdmission(ref platform, state, obj,
			out value, out var present)) return false;
		return present;
	}

	// SelectionChange is an optional edge-signal record until Listview setup
	// publishes it. A non-NULL record is authoritative typed state; malformed
	// present bytes must not be replaced from the raw signal alias.
	private static bool TryReadSelectionSignalAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewSelectionSignalState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			SelectionSignalKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return MuiListviewSelectionSignalStateCodec.TryRead(ref platform, block,
			out value) && IsValidSelectionSignal(value);
	}

	// SelectChange is a MorphOS BOOL edge signal. Keep the named row lossless
	// until this semantic boundary so a malformed published value cannot be
	// normalized into a phantom notification or toggle transition.
	private static bool IsValidSelectionSignal(
		MuiListviewSelectionSignalState value) => value.Value <= 1;

	private static bool EnsureSelectionSignal<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSelectionSignalAdmission(ref platform, state, obj,
			out var value, out var present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			SelectionSignalKey, 0));
		if (!present)
		{
			var fresh = MuiHeadlessMemory.Allocate(ref platform,
				MuiListviewSelectionSignalState.Size);
			if (fresh.IsNull) return false;
			value = default;
			value.Magic = MuiListviewSelectionSignalState.Cookie;
			value.Value = 0;
			if (!MuiListviewSelectionSignalStateCodec.Write(ref platform, fresh,
				value) || !SetInternal(ref platform, state, obj,
					SelectionSignalKey, fresh.Raw))
			{
				platform.Clear(fresh, MuiListviewSelectionSignalState.Size);
				platform.Free(fresh, MuiListviewSelectionSignalState.Size);
				return false;
			}
			block = fresh;
		}
		else if (!MuiListviewSelectionSignalStateCodec.Write(ref platform, block,
			value)) return false;

		// Keep the generic scalar projection coherent for legacy callers; the
		// public getter below reads the named record first.
		return SetInternal(ref platform, state, obj, SelectChange, value.Value);
	}

	internal static bool TryGetSelectionSignal<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewSelectionSignalState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSelectionSignalAdmission(ref platform, state, obj,
			out value, out var present)) return false;
		return present;
	}

	internal static bool ToggleSelectionSignal<TPlatform>(ref TPlatform platform,
		APTR state, APTR listview) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureSelectionSignal(ref platform, state, listview) ||
			!TryGetSelectionSignal(ref platform, state, listview, out var signal))
			return false;
		signal.Value = signal.Value == 0 ? 1u : 0u;
		var block = APTR.FromPointer(Read(ref platform, state, listview,
			SelectionSignalKey, 0));
		if (!MuiListviewSelectionSignalStateCodec.Write(ref platform, block,
			signal)) return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, listview,
			SelectChange, signal.Value, true);
	}

	private static uint PolicyValue<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint fallback)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadInteractionPolicyAdmission(ref platform, state, obj,
			out var value, out var present))
		{
			if (present)
			{
				if (attribute == Input) return value.Input;
				if (attribute == MultiSelect) return value.MultiSelect;
				if (attribute == ScrollerPos) return value.ScrollerPos;
				if (attribute == DragType) return value.DragType;
			}
			else
			{
				return Read(ref platform, state, obj, attribute, fallback);
			}
		}
		// Keep malformed present policy inert for all policy consumers. This is
		// distinct from true absence, where the raw compatibility value above is
		// still accepted during legacy bootstrap.
		return attribute == Input ? 0u :
			attribute == MultiSelect ? MultiSelectNone :
			attribute == ScrollerPos ? ScrollerPosNone : DragTypeNone;
	}

	private static bool UpdateInteractionPolicy<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint attribute,
		uint value) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureInteractionPolicy(ref platform, state, obj)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			InteractionPolicyKey, 0));
		if (!MuiListviewInteractionPolicyStateCodec.TryRead(ref platform, block,
			out var policy)) return false;
		if (attribute == Input) policy.Input = NormalizePolicy(Input, value);
		else if (attribute == MultiSelect)
			policy.MultiSelect = NormalizePolicy(MultiSelect, value);
		else if (attribute == ScrollerPos)
			policy.ScrollerPos = NormalizePolicy(ScrollerPos, value);
		else if (attribute == DragType)
			policy.DragType = NormalizePolicy(DragType, value);
		else return true;
		return MuiListviewInteractionPolicyStateCodec.Write(ref platform, block,
			policy);
}

	// Click state is optional before setup publishes it. A non-NULL block is
	// authoritative typed state; malformed present bytes must not be repaired
	// from the scalar click aliases after an input transition has begun.
	private static bool TryReadClickStateAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewClickState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			ClickStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return TryReadClickState(ref platform, block, out value) &&
			IsValidClickState(value);
	}

	// These two fields are MorphOS BOOLs in the published Listview record.  A
	// non-zero value is accepted by the public setter path and written back as
	// one, but an already-published guest row must contain a canonical BOOL so
	// corruption cannot be hidden by a read-side normalization.
	private static bool IsValidClickState(MuiListviewClickState value) =>
		value.DoubleClick <= 1 && value.AgainClick <= 1;

	private static bool EnsureClickState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadClickStateAdmission(ref platform, state, obj,
			out var value, out var present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ClickStateKey, 0));
		if (!present)
		{
			block = MuiHeadlessMemory.Allocate(ref platform,
				MuiListviewClickState.Size);
			if (block.IsNull) return false;
			value = default;
			value.Magic = MuiListviewClickState.Cookie;
			value.ClickColumn = Read(ref platform, state, obj, ClickColumn, 0);
			value.DoubleClick = Read(ref platform, state, obj, DoubleClick, 0) == 0
				? 0u : 1u;
			value.AgainClick = Read(ref platform, state, obj, AgainClick, 0) == 0
				? 0u : 1u;
			value.DefClickColumn = Read(ref platform, state, obj,
				DefClickColumn, 0);
			if (!WriteClickState(ref platform, block, value) ||
				!SetInternal(ref platform, state, obj, ClickStateKey, block.Raw))
			{
				FreeClickState(ref platform, block);
				return false;
			}
			return true;
		}
		return WriteClickState(ref platform, block, value);
	}

	private static bool UpdateDefaultClickColumnState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ClickStateKey, 0));
		if (!TryReadClickState(ref platform, block, out var clickState))
			return false;
		clickState.DefClickColumn = value;
		WriteClickState(ref platform, block, clickState);
		return true;
	}

	private static uint DefaultClickColumnValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadClickStateAdmission(ref platform, state, obj,
			out var clickState, out var present) && present)
			return clickState.DefClickColumn;
		return present ? 0u : Read(ref platform, state, obj, DefClickColumn, 0);
	}

	// Bind the child-to-composite notification projection through a named
	// attribute record.  Keeping the link in guest object state makes ownership
	// and teardown visible to the same struct-first memory seam as other state.
	internal static bool SetListviewOwner<TPlatform>(ref TPlatform platform,
		APTR state, APTR list, APTR owner)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiListCore.SetListviewOwner(ref platform, state, list, owner);

	private static bool WriteClickState<TPlatform>(ref TPlatform platform,
		APTR block, MuiListviewClickState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListviewClickStateCodec.Write(ref platform, block, value);

	private static bool TryReadClickState<TPlatform>(ref TPlatform platform,
		APTR block, out MuiListviewClickState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListviewClickStateCodec.TryRead(ref platform, block, out value);

	internal static bool TryGetClickState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListviewClickState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadClickStateAdmission(ref platform, state, obj,
			out value, out var present)) return false;
		return present;
	}

	private static void FreeClickState<TPlatform>(ref TPlatform platform,
		APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiListviewClickStateCodec.Clear(ref platform, block)) return;
		platform.Free(block, MuiListviewClickState.Size);
	}

	private static APTR EnsureHorizontalScrollerDragState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadHorizontalScrollerDragStateAdmission(ref platform, state,
			obj, out _, out var present)) return APTR.Null;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			HorizontalScrollerDragStateKey, 0));
		if (present) return block;
		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListviewHorizontalScrollerDragState.Size);
		if (block.IsNull) return APTR.Null;
		var value = default(MuiListviewHorizontalScrollerDragState);
		value.Magic = MuiListviewHorizontalScrollerDragState.Cookie;
		if (!MuiListviewHorizontalScrollerDragStateCodec.Write(ref platform, block,
			value) || !SetInternal(ref platform, state, obj,
			HorizontalScrollerDragStateKey, block.Raw))
		{
			if (platform.IsMapped(block,
				MuiListviewHorizontalScrollerDragState.Size))
				platform.Clear(block,
					MuiListviewHorizontalScrollerDragState.Size);
			platform.Free(block, MuiListviewHorizontalScrollerDragState.Size);
			return APTR.Null;
		}
		return block;
	}

	// A published horizontal drag record is authoritative guest state. A
	// non-NULL record that fails the cookie/field contract is malformed, not
	// absence; pointer admission must not free it and arm a replacement grab.
	private static bool TryReadHorizontalScrollerDragStateAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewHorizontalScrollerDragState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			HorizontalScrollerDragStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return MuiListviewHorizontalScrollerDragStateCodec.TryRead(ref platform,
			block, out value) && IsValidHorizontalScrollerDragState(
			ref platform, state, obj, value);
	}

	// The horizontal thumb record is a bounded pointer-grab state machine. Its
	// inactive storage is the all-zero value; an active record needs a
	// non-negative grab offset and a scroll origin no greater than the current
	// named horizontal projection. Keep LastPointer signed for guest coordinates
	// above or below the window origin, and reject unknown transition bits.
	private static bool IsValidHorizontalScrollerDragState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiListviewHorizontalScrollerDragState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		const uint knownFlags =
			MuiListviewHorizontalScrollerDragState.ActiveFlag |
			MuiListviewHorizontalScrollerDragState.CapturedFlag;
		if ((value.Flags & ~knownFlags) != 0) return false;
		if ((value.Flags & MuiListviewHorizontalScrollerDragState.ActiveFlag) == 0)
			return value.Flags == 0 && value.GrabOffset == 0 &&
				value.StartScroll == 0 && value.LastPointer == 0;
		if (value.GrabOffset < 0) return false;
		if (!TryReadHorizontalScrollerStateAdmission(ref platform, state, obj,
			out var projection, out var present) || !present) return false;
		return value.StartScroll <= projection.MaxScrollX;
	}

	private static void ReleaseHorizontalScrollerDragState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR block)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadHorizontalScrollerDragStateAdmission(ref platform, state, obj,
			out var value, out var present) && present && (value.Flags &
			MuiListviewHorizontalScrollerDragState.CapturedFlag) != 0)
			ReleasePointer(ref platform, obj,
				MuiPointerCaptureKind.HorizontalScroller, value.LastPointer, 0);
		if (block.IsNotNull && platform.IsMapped(block,
			MuiListviewHorizontalScrollerDragState.Size))
		{
			platform.Clear(block, MuiListviewHorizontalScrollerDragState.Size);
			platform.Free(block, MuiListviewHorizontalScrollerDragState.Size);
		}
		SetInternal(ref platform, state, obj, HorizontalScrollerDragStateKey, 0);
	}

	internal static void CleanupRecords<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		// Remove any live external Prop/Scrollbar recipe before the child and
		// generic notification records are retired. The destination pointer lives
		// in a named guest connection record, so teardown does not scan object
		// layout or depend on a managed side table.
		DisconnectExternalScrollerProp(ref platform, state, obj, APTR.Null);
		var childState = APTR.FromPointer(Read(ref platform, state, obj,
			ChildStateKey, 0));
		FreeChildState(ref platform, childState);
		SetInternal(ref platform, state, obj, ChildStateKey, 0);
		var policy = APTR.FromPointer(Read(ref platform, state, obj,
			InteractionPolicyKey, 0));
		if (policy.IsNotNull && platform.IsMapped(policy,
			MuiListviewInteractionPolicyState.Size))
		{
			platform.Clear(policy, MuiListviewInteractionPolicyState.Size);
			platform.Free(policy, MuiListviewInteractionPolicyState.Size);
		}
		SetInternal(ref platform, state, obj, InteractionPolicyKey, 0);
		var signal = APTR.FromPointer(Read(ref platform, state, obj,
			SelectionSignalKey, 0));
		if (signal.IsNotNull && platform.IsMapped(signal,
			MuiListviewSelectionSignalState.Size))
		{
			platform.Clear(signal, MuiListviewSelectionSignalState.Size);
			platform.Free(signal, MuiListviewSelectionSignalState.Size);
		}
		SetInternal(ref platform, state, obj, SelectionSignalKey, 0);
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ClickStateKey, 0));
		FreeClickState(ref platform, block);
		SetInternal(ref platform, state, obj, ClickStateKey, 0);
		var drag = APTR.FromPointer(Read(ref platform, state, obj,
			DragStateKey, 0));
		ReleaseDragState(ref platform, state, obj, drag);
		var scrollerDrag = APTR.FromPointer(Read(ref platform, state, obj,
			ScrollerDragStateKey, 0));
		ReleaseScrollerDragState(ref platform, state, obj, scrollerDrag);
		var horizontalDrag = APTR.FromPointer(Read(ref platform, state, obj,
			HorizontalScrollerDragStateKey, 0));
		ReleaseHorizontalScrollerDragState(ref platform, state, obj,
			horizontalDrag);
		var layout = APTR.FromPointer(Read(ref platform, state, obj,
			LayoutStateKey, 0));
		if (layout.IsNotNull && platform.IsMapped(layout,
			MuiListviewLayoutState.Size))
		{
			platform.Clear(layout, MuiListviewLayoutState.Size);
			platform.Free(layout, MuiListviewLayoutState.Size);
		}
		SetInternal(ref platform, state, obj, LayoutStateKey, 0);
		var render = APTR.FromPointer(Read(ref platform, state, obj,
			RenderStateKey, 0));
		if (render.IsNotNull && platform.IsMapped(render,
			MuiListviewRenderState.Size))
		{
			platform.Clear(render, MuiListviewRenderState.Size);
			platform.Free(render, MuiListviewRenderState.Size);
		}
		SetInternal(ref platform, state, obj, RenderStateKey, 0);
		var scrollerState = APTR.FromPointer(Read(ref platform, state, obj,
			ScrollerStateKey, 0));
		if (scrollerState.IsNotNull && platform.IsMapped(scrollerState,
			MuiListviewScrollerState.Size))
		{
			platform.Clear(scrollerState, MuiListviewScrollerState.Size);
			platform.Free(scrollerState, MuiListviewScrollerState.Size);
		}
		SetInternal(ref platform, state, obj, ScrollerStateKey, 0);
		var horizontalScrollerState = APTR.FromPointer(Read(ref platform, state,
			obj, HorizontalScrollerStateKey, 0));
		if (horizontalScrollerState.IsNotNull && platform.IsMapped(
			horizontalScrollerState, MuiListviewHorizontalScrollerState.Size))
		{
			platform.Clear(horizontalScrollerState,
				MuiListviewHorizontalScrollerState.Size);
			platform.Free(horizontalScrollerState,
				MuiListviewHorizontalScrollerState.Size);
		}
		SetInternal(ref platform, state, obj, HorizontalScrollerStateKey, 0);
	}

	private static APTR CreateInternalList<TPlatform>(ref TPlatform platform,
		APTR state) where TPlatform : struct, IMuiHeadlessPlatform
	{
		// Discover the registered List class by name and build a plain list.
		var listClass = FindListClass(ref platform, state);
		return listClass.IsNull ? APTR.Null
			: MuiListCore.CreateList(ref platform, state, listClass, APTR.Null);
	}

	private static APTR FindListClass<TPlatform>(ref TPlatform platform,
		APTR state) where TPlatform : struct, IMuiHeadlessPlatform
	{
		// Walk the registered class list looking for one classified as List.
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return APTR.Null;
		var current = stateValue.Classes;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (MuiListCore.ClassifyRecord(ref platform, current) ==
				MuiCollectionClass.List) return current;
			if (!MuiHeadlessClassCodec.TryRead(ref platform, current,
				out var classValue)) return APTR.Null;
			current = classValue.Next;
		}
		return APTR.Null;
	}

	private static bool IsListBacked<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var cls = MuiListCore.Classify(ref platform, state, obj);
		return MuiListCore.IsListBacked(cls);
	}

	// ---- Child resolution -----------------------------------------------------

	// The bound child list (MUIA_Listview_List). Null when the listview has been
	// torn down or was never fully constructed.  The relationship is a guest
	// record, not an ownership assumption: a caller may dispose a supplied List
	// directly, so every consumer validates the named child against the live
	// headless object table before returning it.  This keeps the raw compatibility
	// pointer from becoming a use-after-free source.
	private static bool IsLiveChildList<TPlatform>(ref TPlatform platform,
		APTR state, APTR child) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (child.IsNull || MuiHeadlessObjectCore.FindObject(ref platform, state,
			child).IsNull) return false;
		return MuiListCore.IsListBacked(MuiListCore.Classify(ref platform, state,
			child));
	}

	public static APTR ChildList<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		ResolveChildList(ref platform, state, obj);

	private static APTR ResolveChildList<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		APTR child;
		if (!TryReadChildStateAdmission(ref platform, state, obj,
			out var value, out var present))
			return APTR.Null;
		if (present)
			child = value.Child;
		else if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			List, out var raw))
			child = APTR.FromPointer(raw);
		else
			return APTR.Null;
		return IsLiveChildList(ref platform, state, child) ? child : APTR.Null;
	}

	// A published connection record is authoritative guest state. A non-NULL
	// record that fails the cookie/field contract is malformed, not absent; do
	// not free it or silently replace it while a notification recipe may still
	// refer to the recorded destination.
	private static bool TryReadExternalScrollerConnectionAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewExternalScrollerConnectionState value,
		out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			ExternalScrollerConnectionKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return MuiListviewExternalScrollerConnectionStateCodec.TryRead(
			ref platform, block, out value) &&
			IsValidExternalScrollerConnection(ref platform, state, value);
	}

	// A published connection owns one destination object identity. A null,
	// unmapped, or non-Prop/Scrollbar destination cannot participate in the
	// four notification edges and must not be used for teardown or reconnect.
	private static bool IsValidExternalScrollerConnection<TPlatform>(
		ref TPlatform platform, APTR state,
		MuiListviewExternalScrollerConnectionState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (value.Prop.IsNull) return false;
		return MuiCommonControlCore.IsPropClass(
			MuiCommonControlCore.Classify(ref platform, state, value.Prop));
	}

	private static bool EnsureExternalScrollerConnection<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadExternalScrollerConnectionAdmission(ref platform, state, obj,
			out _, out var present))
		{
			block = APTR.Null;
			return false;
		}
		block = APTR.FromPointer(Read(ref platform, state, obj,
			ExternalScrollerConnectionKey, 0));
		if (present) return block.IsNotNull;
		var fresh = MuiHeadlessMemory.Allocate(ref platform,
			MuiListviewExternalScrollerConnectionState.Size);
		if (fresh.IsNull) return false;
		var value = default(MuiListviewExternalScrollerConnectionState);
		value.Magic = MuiListviewExternalScrollerConnectionState.Cookie;
		if (!MuiListviewExternalScrollerConnectionStateCodec.Write(ref platform,
			fresh, value) || !SetInternal(ref platform, state, obj,
			ExternalScrollerConnectionKey, fresh.Raw))
		{
			MuiListviewExternalScrollerConnectionStateCodec.Clear(ref platform,
				fresh);
			platform.Free(fresh,
				MuiListviewExternalScrollerConnectionState.Size);
			return false;
		}
		block = fresh;
		return true;
	}

	internal static bool TryGetExternalScrollerConnection<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewExternalScrollerConnectionState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		return TryReadExternalScrollerConnectionAdmission(ref platform, state,
			obj, out value, out var present) && present;
	}

	private static void FreeExternalScrollerConnection<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ExternalScrollerConnectionKey, 0));
		MuiListviewExternalScrollerConnectionStateCodec.Clear(ref platform, block);
		if (block.IsNotNull && platform.IsMapped(block,
			MuiListviewExternalScrollerConnectionState.Size))
			platform.Free(block,
				MuiListviewExternalScrollerConnectionState.Size);
		SetInternal(ref platform, state, obj, ExternalScrollerConnectionKey, 0);
	}

	// A destination Prop/Scrollbar can be disposed before its Listview owner.
	// Walk the guest object chain and remove recipes that target that object
	// while both records are still discoverable. The walk uses the named headless
	// state/object codecs and retains no managed collection of connections.
	internal static void DisconnectExternalScrollerConnectionsToObject<TPlatform>(
		ref TPlatform platform, APTR state, APTR target)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (target.IsNull || !MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return;
		var current = stateValue.Objects;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessObjectCodec.TryRead(ref platform, current,
				out var objectValue)) return;
			var candidate = objectValue.Boopsi;
			if (candidate.Raw != target.Raw &&
				MuiListCore.Classify(ref platform, state, candidate) ==
				MuiCollectionClass.Listview &&
				TryGetExternalScrollerConnection(ref platform, state, candidate,
					out var connection) && connection.Prop.Raw == target.Raw)
				DisconnectExternalScrollerProp(ref platform, state, candidate,
					APTR.Null);
			current = objectValue.Next;
		}
	}

	// Publish the bounded viewport state that a real Prop child would expose.
	// The state is derived from the owned List child, so no second list model can
	// drift out of sync with MUIA_List_First/Visible/Entries.
	public static bool GetScrollerState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out uint entries, out uint visible, out uint first,
		out uint maxFirst) where TPlatform : struct, IMuiHeadlessPlatform
	{
		entries = 0;
		visible = 0;
		first = 0;
		maxFirst = 0;
		if (MuiListCore.Classify(ref platform, state, obj) !=
			MuiCollectionClass.Listview) return false;
		var child = ChildList(ref platform, state, obj);
		if (child.IsNull) return false;
		entries = MuiListCore.EntryCount(ref platform, state, child);
		visible = MuiListCore.VisibleCursor(ref platform, state, child);
		if (visible == MuiListCore.VisibleOff)
		{
			// A hidden/iconified child exposes MorphOS's -1 sentinels publicly;
			// the composite scroller has no usable viewport in this state.
			first = MuiListCore.VisibleOff;
			return PublishScrollerState(ref platform, state, obj, entries,
				visible, first, maxFirst, out entries, out visible, out first,
				out maxFirst);
		}
		if (visible == 0)
		{
			if (!MuiAreaLayoutCore.TryReadGeometryState(ref platform, state, child,
				out var childGeometry)) return false;
			var height = childGeometry.Height <= 0 ? 0u :
				unchecked((uint)childGeometry.Height);
			var lineHeight = ListRowHeight;
			if (MuiListCore.TryGetViewportState(ref platform, state, child,
				out var childViewport) && childViewport.LineHeight != 0)
				lineHeight = childViewport.LineHeight;
			var rows = height / lineHeight;
			if (rows == 0) rows = 1;
			var titleRows = MuiListCore.TitleRowCount(ref platform, state, child);
			visible = rows > titleRows ? rows - titleRows : 0;
			if (visible == 0 && titleRows == 0) visible = 1;
		}
		first = MuiListCore.FirstCursor(ref platform, state, child);
		maxFirst = entries > visible ? entries - visible : 0;
		if (first > maxFirst) first = maxFirst;
		return PublishScrollerState(ref platform, state, obj, entries, visible,
			first, maxFirst, out entries, out visible, out first, out maxFirst);
	}

	// Read the child List's pixel viewport as the range expected by an external
	// Prop.  This is an explicit integration seam: MorphOS applications connect
	// an independently created Prop with MUIM_Notify, so Listview does not own or
	// silently replace that application object.
	internal static bool TryGetExternalScrollerState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewExternalScrollerState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiListCore.Classify(ref platform, state, obj) !=
			MuiCollectionClass.Listview) return false;
		var child = ChildList(ref platform, state, obj);
		if (child.IsNull || !MuiListCore.TryGetViewportState(ref platform, state,
			child, out var viewport)) return false;
		value.Entries = viewport.TotalPixel;
		value.Visible = viewport.VisiblePixel;
		value.First = viewport.TopPixel;
		return true;
	}

	// Apply the documented Listview external-scroller recipe to a real Prop or
	// Scrollbar object.  SetControlAttribute keeps Prop range records, clamping,
	// redraw scheduling, and optional notifications on the class-aware path.
	// Entries, Visible, then First are written in that order so the final cursor
	// is bounded against the complete pixel range.
	internal static bool SyncExternalScrollerProp<TPlatform>(
		ref TPlatform platform, APTR state, APTR listview, APTR prop,
		bool notify = true)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (prop.IsNull || !MuiCommonControlCore.IsPropClass(
			MuiCommonControlCore.Classify(ref platform, state, prop))) return false;
		if (!TryGetExternalScrollerState(ref platform, state, listview,
			out var range)) return false;
		return MuiCommonControlCore.SetControlAttribute(ref platform, state, prop,
			MuiCommonControlCore.PropEntries, range.Entries, notify) &&
			MuiCommonControlCore.SetControlAttribute(ref platform, state, prop,
				MuiCommonControlCore.PropVisible, range.Visible, notify) &&
			MuiCommonControlCore.SetControlAttribute(ref platform, state, prop,
				MuiCommonControlCore.PropFirst, range.First, notify);
	}

	// Install the MorphOS external-scrollbar recipe on an application-owned
	// Prop/Scrollbar. The four notifications are copied into the notification
	// core from a temporary guest follow vector; no managed or offset-based
	// connection table is retained. Reconnecting first removes an earlier
	// recipe, keeping repeated setup calls deterministic.
	internal static bool ConnectExternalScrollerProp<TPlatform>(
		ref TPlatform platform, APTR state, APTR listview, APTR prop)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (prop.IsNull || !MuiCommonControlCore.IsPropClass(
			MuiCommonControlCore.Classify(ref platform, state, prop))) return false;
		var child = ChildList(ref platform, state, listview);
		if (child.IsNull) return false;
		// Admission precedes disconnect so malformed published state cannot be
		// mistaken for an empty recipe and partially mutate notifications.
		if (!TryReadExternalScrollerConnectionAdmission(ref platform, state,
			listview, out _, out _)) return false;
		// Disconnect the recorded destination, if any, before installing another
		// one. The destination is kept in a typed guest record so reconnecting an
		// different Prop cannot strand the old source notifications.
		DisconnectExternalScrollerProp(ref platform, state, listview, APTR.Null);
		if (!SyncExternalScrollerProp(ref platform, state, listview, prop))
			return false;
		if (!AddExternalScrollerNotification(ref platform, state, child,
			MuiListCore.TotalPixel, prop, MuiCommonControlCore.PropEntries) ||
			!AddExternalScrollerNotification(ref platform, state, child,
			MuiListCore.VisiblePixel, prop, MuiCommonControlCore.PropVisible) ||
			!AddExternalScrollerNotification(ref platform, state, child,
			MuiListCore.TopPixel, prop, MuiCommonControlCore.PropFirst) ||
			!AddExternalScrollerNotification(ref platform, state, prop,
			MuiCommonControlCore.PropFirst, child, MuiListCore.TopPixel))
		{
			DisconnectExternalScrollerProp(ref platform, state, listview, prop);
			return false;
		}
		var connectionValue = default(
			MuiListviewExternalScrollerConnectionState);
		connectionValue.Magic =
			MuiListviewExternalScrollerConnectionState.Cookie;
		connectionValue.Prop = prop;
		if (!EnsureExternalScrollerConnection(ref platform, state, listview,
			out var connection) ||
			!MuiListviewExternalScrollerConnectionStateCodec.Write(ref platform,
				connection, connectionValue))
		{
			DisconnectExternalScrollerProp(ref platform, state, listview, prop);
			return false;
		}
		return true;
	}

	// Remove the complete external-scrollbar recipe. Removal uses the same
	// source/destination pairs as installation, so it also cleans up a partial
	// connection after an allocation or notification failure.
	internal static bool DisconnectExternalScrollerProp<TPlatform>(
		ref TPlatform platform, APTR state, APTR listview, APTR prop)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var child = ChildList(ref platform, state, listview);
		if (child.IsNull)
		{
			FreeExternalScrollerConnection(ref platform, state, listview);
			return false;
		}
		var recorded = APTR.Null;
		if (TryGetExternalScrollerConnection(ref platform, state, listview,
			out var connection)) recorded = connection.Prop;
		// Keep the guest pointer in one named APTR local. An implicit conditional
		// expression makes the freestanding MC68000 lowering merge a scalar
		// condition with a managed-pointer-shaped value; explicit state keeps this
		// teardown path struct-first and compiler-stable.
		var target = recorded;
		if (target.IsNull) target = prop;
		if (target.IsNotNull)
		{
			MuiNotifyCore.Remove(ref platform, state, child,
				MuiListCore.TotalPixel, target, true);
			MuiNotifyCore.Remove(ref platform, state, child,
				MuiListCore.VisiblePixel, target, true);
			MuiNotifyCore.Remove(ref platform, state, child,
				MuiListCore.TopPixel, target, true);
			MuiNotifyCore.Remove(ref platform, state, target,
				MuiCommonControlCore.PropFirst, child, true);
		}
		// If a caller supplied a different explicit destination, remove that
		// recipe too; this keeps the public disconnect operation idempotent even
		// after a caller lost the recorded Prop pointer.
		if (prop.IsNotNull && prop.Raw != target.Raw)
		{
			MuiNotifyCore.Remove(ref platform, state, child,
				MuiListCore.TotalPixel, prop, true);
			MuiNotifyCore.Remove(ref platform, state, child,
				MuiListCore.VisiblePixel, prop, true);
			MuiNotifyCore.Remove(ref platform, state, child,
				MuiListCore.TopPixel, prop, true);
			MuiNotifyCore.Remove(ref platform, state, prop,
				MuiCommonControlCore.PropFirst, child, true);
		}
		if (recorded.IsNotNull || prop.IsNull)
			FreeExternalScrollerConnection(ref platform, state, listview);
		return target.IsNotNull || prop.IsNull;
	}

	private static bool AddExternalScrollerNotification<TPlatform>(
		ref TPlatform platform, APTR state, APTR source, uint trigger,
		APTR destination, uint targetAttribute)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var follow = MuiHeadlessMemory.Allocate(ref platform,
			ExternalNotifyFollowBytes);
		if (follow.IsNull) return false;
		var followMessage = default(MuiSetAttributeMessage);
		followMessage.MethodId = MuiNotifyCore.SetMethod;
		followMessage.Attribute = targetAttribute;
		followMessage.Value = NotifyTriggerValue;
		if (!MuiNotifyPacketCodec.TryWriteSet(ref platform, follow,
			followMessage))
		{
			platform.Clear(follow, ExternalNotifyFollowBytes);
			platform.Free(follow, ExternalNotifyFollowBytes);
			return false;
		}
		var added = MuiNotifyCore.Add(ref platform, state, source, trigger,
			NotifyEveryTime, destination, 3, follow);
		platform.Clear(follow, ExternalNotifyFollowBytes);
		platform.Free(follow, ExternalNotifyFollowBytes);
		return added;
	}

	// Scroll the child list by viewport row, with the same saturation rule as a
	// Prop_First value. This is the narrow input seam used by the future full
	// scrollbar gadget; it keeps all ownership in the existing List object.
	public static bool SetScrollerFirst<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int requested)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!GetScrollerState(ref platform, state, obj, out _, out var visible,
			out _, out var maxFirst) || visible == 0 ||
			visible == MuiListCore.VisibleOff)
			// A hidden or zero-height Listview has no usable Prop viewport. Keep the
			// child's MorphOS -1 cursor sentinel intact instead of turning a
			// programmatic scroller write into a latent row position that will be
			// applied when the object becomes visible again.
			return false;
		var target = requested < 0 ? 0 : requested;
		if ((uint)target > maxFirst) target = unchecked((int)maxFirst);
		var child = ChildList(ref platform, state, obj);
		if (!MuiListCore.SetAttribute(ref platform, state, child, ListFirst,
			unchecked((uint)target), true)) return false;
		if (!MuiListCore.RefreshViewportMetrics(ref platform, state, child))
			return false;
		return GetScrollerState(ref platform, state, obj, out _, out _, out _,
			out _);
	}

	// ---- Attribute forwarding -------------------------------------------------

	// Talk to the listview as if it were the list directly: listview-private
	// attributes stay local; everything else is forwarded to the child list.
	public static bool SetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiListCore.Classify(ref platform, state, obj) !=
			MuiCollectionClass.Listview)
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, value, notify);
		// These projections are not mutable application attributes. Listview
		// owns the List relationship, while click/selection values are published
		// by its input and child-selection paths. Internal publication uses the
		// raw named-record seam below, so rejecting these writes cannot strand
		// construction or notification state.
		if (attribute == List || attribute == AgainClick ||
			attribute == ClickColumn || attribute == DoubleClick ||
			attribute == SelectChange) return false;
		if (IsListviewAttribute(attribute))
		{
			var normalized = NormalizePolicy(attribute, value);
			// Establish the named policy before changing the public scalar so a
			// failed allocation cannot leave a behavior path without state.
			if (attribute == DefClickColumn &&
				!EnsureClickState(ref platform, state, obj)) return false;
			if ((attribute == Input || attribute == MultiSelect ||
				attribute == ScrollerPos || attribute == DragType) &&
				!EnsureInteractionPolicy(ref platform, state, obj)) return false;
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, normalized, notify)) return false;
			if ((attribute == Input || attribute == MultiSelect ||
				attribute == ScrollerPos || attribute == DragType) &&
				!UpdateInteractionPolicy(ref platform, state, obj, attribute,
					normalized)) return false;
			if (attribute == DefClickColumn &&
				!UpdateDefaultClickColumnState(ref platform, state, obj,
					normalized)) return false;
			// Input and pointer-policy changes are immediate ownership boundaries.
			// Do not leave a guest-resident drag or scroller record armed until a
			// later MUIKEY_RELEASE arrives; the next pointer packet must not commit
			// a gesture whose policy or geometry the caller has already disabled.
			// CancelDrag clears the child drop mark and releases all three named
			// pointer-grab records.
			if ((attribute == Input && normalized == 0) ||
				attribute == ScrollerPos ||
				(attribute == DragType && normalized != DragTypeImmediate))
			{
				var policyChild = ChildList(ref platform, state, obj);
				CancelDrag(ref platform, state, obj, policyChild);
			}
			// MorphOS exposes DragType on Listview while the owned List performs
			// the sortable-row validation. Keep both public projections coherent;
			// callers should not need to reach into the child object.
			if (attribute == DragType)
			{
				var dragChild = ChildList(ref platform, state, obj);
				return dragChild.IsNotNull && MuiListCore.SetAttribute(ref platform,
					state, dragChild, ListDragType, normalized, notify);
			}
			return true;
		}
		var child = ChildList(ref platform, state, obj);
		// Route through the List class-aware setter so normalized guest state
		// (FORMAT-derived SortColumn, AutoVisible, drag flags, and similar List
		// policies) is preserved when an application addresses the listview.
		return child.IsNotNull && MuiListCore.SetRuntimeAttribute(ref platform, state,
			child, attribute, value, notify);
	}

	// OM_SET cannot rewrite the adopted list relationship or the listview
	// construction-only interaction policy. Keep the lower-level SetAttribute
	// available for construction and internal forwarding, but make the public
	// dispatcher use this explicit runtime boundary.
	public static bool SetRuntimeAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiListCore.Classify(ref platform, state, obj) !=
			MuiCollectionClass.Listview)
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, value, notify);
		if (attribute == List || attribute == Input || attribute == MultiSelect ||
			attribute == ScrollerPos) return false;
		return SetAttribute(ref platform, state, obj, attribute, value, notify);
	}

	public static bool GetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (MuiListCore.Classify(ref platform, state, obj) !=
			MuiCollectionClass.Listview)
			return MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
				attribute, out value);
		if (attribute == AgainClick || attribute == ClickColumn ||
			attribute == DefClickColumn ||
			attribute == DoubleClick)
		{
			if (TryReadClickStateAdmission(ref platform, state, obj,
				out var clickState, out var present) && present)
			{
				value = attribute == AgainClick ? clickState.AgainClick :
					attribute == DoubleClick ? clickState.DoubleClick :
					attribute == DefClickColumn ? clickState.DefClickColumn :
					clickState.ClickColumn;
				return true;
			}
			if (present) return false;
			return MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out value);
		}
		if (attribute == SelectChange)
		{
			if (TryReadSelectionSignalAdmission(ref platform, state, obj,
				out var signal, out var present) && present)
			{
				value = signal.Value;
				return true;
			}
			if (present) return false;
			return MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
				attribute, out value);
		}
		// DragType is the only Listview interaction-policy attribute with a public
		// getter ([ISG]). Input, MultiSelect, and ScrollerPos are [I..] in the
		// MorphOS contract: their values remain in the named policy record for
		// input/layout code, but Get/OM_GET must not claim them.
		if (attribute == DragType)
		{
			if (TryReadInteractionPolicyAdmission(ref platform, state, obj,
				out var policy, out var present) && present)
			{
				value = policy.DragType;
				return true;
			}
			// True absence retains the raw compatibility scalar; malformed present
			// state is rejected instead of leaking a stale alias.
			if (present) return false;
			return MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out value);
		}
		if (attribute == List)
		{
			if (!TryReadChildStateAdmission(ref platform, state, obj,
				out var childState, out var present)) return false;
			if (present)
			{
				value = childState.Child.Raw;
				return true;
			}
			return MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
				attribute, out value);
		}
		if (IsListviewAttribute(attribute))
			return MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
				attribute, out value);
		var child = ChildList(ref platform, state, obj);
		return child.IsNotNull && MuiHeadlessObjectCore.GetAttribute(ref platform,
			state, child, attribute, out value);
	}

	// OM_GET on a Listview projects the public List attribute family from the
	// named owned child. Keep this seam separate from GetAttribute: the latter
	// also handles Listview-private click and policy records, whose internal
	// bootstrap reads must never recurse through the public getter router.
	internal static bool TryGetForwardedPublicAttribute<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint attribute,
		out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (!MuiListCore.IsPublicGetterAttribute(attribute) ||
			// Several List and Listview ABI IDs intentionally overlap (for
			// example DragType and the click signals). They are Listview-owned
			// policy/click projections and must not be mistaken for forwarded
			// child attributes at the generic getter boundary.
			IsListviewAttribute(attribute) ||
			MuiListCore.Classify(ref platform, state, obj) !=
			MuiCollectionClass.Listview)
			return false;
		var child = ChildList(ref platform, state, obj);
		if (child.IsNull)
		{
			// The Listview still owns the public projection even when a supplied
			// child was disposed directly.  Report the neutral value rather than
			// falling through to the stale parent compatibility scalar.
			value = 0;
			return true;
		}
		return MuiHeadlessObjectCore.GetAttribute(ref platform, state, child,
			attribute, out value);
	}

	// MUIA_Listview_List is a public relationship getter, but it is not part of
	// the List attribute family above. Resolve it from the named child-state
	// record before the generic parent attribute list so direct raw writes cannot
	// replace the adopted-child identity.
	internal static bool TryGetChildRelationAttribute<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint attribute,
		out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (attribute != List || MuiListCore.Classify(ref platform, state, obj) !=
			MuiCollectionClass.Listview) return false;
		// During CreateListview the named child record has not been installed
		// yet. Preserve the construction tag through the raw bootstrap seam so a
		// supplied List is adopted; once the record exists, the validated child
		// projection below becomes authoritative.
		if (!TryReadChildStateAdmission(ref platform, state, obj, out _,
			out var present))
		{
			// A present but malformed named relationship is still a handled
			// Listview projection.  Do not fall through to the raw List scalar,
			// which would expose a disposed or otherwise incoherent child pointer.
			if (present)
			{
				value = 0;
				return true;
			}
			return false;
		}
		if (!present)
			return MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				List, out value);
		var child = ChildList(ref platform, state, obj);
		// This is a handled relationship getter even when the caller disposed a
		// supplied child out of band.  Returning NULL prevents generic metadata
		// from exposing the now-dangling raw pointer.
		value = child.IsNull ? 0u : child.Raw;
		return true;
	}

	private static bool IsListviewAttribute(uint attribute) =>
		attribute == AgainClick || attribute == ClickColumn ||
		attribute == DefClickColumn || attribute == DoubleClick ||
		attribute == DragType || attribute == Input ||
		attribute == MultiSelect || attribute == ScrollerPos ||
		attribute == SelectChange;

	// Public attributes owned by the Listview projection. Keep this narrow so
	// HeadlessObjectCore can route OM_GET without intercepting Listview's
	// private state-key reads (which intentionally remain generic raw storage).
	internal static bool IsPublicAttribute(uint attribute) =>
		attribute == List || IsListviewAttribute(attribute);

	// Listview talks through to the owned List for the List attribute family.
	// Keep notification source selection on the same named-attribute boundary so
	// an application may install a List notification on the Listview object and
	// still observe the child List's authoritative state.
	internal static bool IsForwardedNotificationAttribute(uint attribute) =>
		MuiListCore.IsPublicGetterAttribute(attribute) &&
		!IsListviewAttribute(attribute);

	internal static bool IsInteractionPolicyAttribute(uint attribute) =>
		attribute == Input || attribute == MultiSelect ||
		attribute == ScrollerPos || attribute == DragType;

	// Public getter projection for the interaction-policy record. Keep this
	// separate from IsInteractionPolicyAttribute because three fields are
	// construction-only and must not leak through HeadlessObjectCore's generic
	// Get fallback.
	internal static bool IsGettableInteractionPolicyAttribute(uint attribute) =>
		attribute == DragType;

	internal static bool IsInitializeOnlyAttribute(uint attribute) =>
		attribute == Input || attribute == MultiSelect ||
		attribute == ScrollerPos;

	// ---- Input ----------------------------------------------------------------

	internal static bool TryMapInputKey(int muiKey, out int selector)
	{
		selector = muiKey switch
		{
			KeyUp => ActiveUp,
			KeyDown => ActiveDown,
			KeyPageUp => ActivePageUp,
			KeyPageDown => ActivePageDown,
			KeyTop => ActiveTop,
			KeyBottom => ActiveBottom,
			_ => int.MinValue,
		};
		return selector != int.MinValue;
	}

	internal static bool TryMapSelectionKey(int muiKey, out bool toggle)
	{
		toggle = muiKey == KeyToggle;
		return muiKey == KeyPress || toggle;
	}

	internal static bool IsDragCancelKey(int muiKey) => muiKey == KeyRelease;

	private static bool MoveHorizontalScroll<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR child, bool towardLeft)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (child.IsNull ||
			!MuiListCore.TryGetHScrollerState(ref platform, state, child,
				out var hState) || hState.Visible == 0) return false;
		var target = hState.ScrollX;
		if (towardLeft)
			target = target > HScrollerKeyStep
				? target - HScrollerKeyStep : 0;
		else
			target = hState.MaxScrollX <= target ||
				hState.MaxScrollX - target <= HScrollerKeyStep
			? hState.MaxScrollX : target + HScrollerKeyStep;
		if (!MuiListCore.SetHScrollerScroll(ref platform, state, child, target))
			return false;
		_ = TryBuildHorizontalScrollerGeometry(ref platform, state, obj,
			out _);
		return true;
	}

	private static bool HandleHorizontalKey<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR child, int muiKey)
		where TPlatform : struct, IMuiLayoutPlatform =>
		MoveHorizontalScroll(ref platform, state, obj, child, muiKey == KeyLeft);

	private static bool HandleHorizontalWheel<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR child, ushort code)
		where TPlatform : struct, IMuiLayoutPlatform
		=> MoveHorizontalScroll(ref platform, state, obj, child, code == WheelLeft);

	private static bool HandleVerticalWheel<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, ushort code)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (code != WheelUp && code != WheelDown ||
			!GetScrollerState(ref platform, state, obj, out var entries,
				out var visible, out var first, out var maxFirst) || entries == 0 ||
			visible == 0 || visible == MuiListCore.VisibleOff)
			return false;
		var target = first;
		if (code == WheelUp)
			target = target > VScrollerWheelStep
				? target - VScrollerWheelStep : 0;
		else if (maxFirst > target)
			target = maxFirst - target <= VScrollerWheelStep
				? maxFirst : target + VScrollerWheelStep;
		return SetScrollerFirst(ref platform, state, obj,
			unchecked((int)target));
	}

	private static bool HasActivePointerGrab<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (TryReadDragStateAdmission(ref platform, state, obj, out var drag,
			out var present) && present &&
			(drag.Flags & MuiListviewDragState.ActiveFlag) != 0)
			return true;
		if (TryReadScrollerDragStateAdmission(ref platform, state, obj,
			out var scroller, out present) && present &&
			(scroller.Flags & MuiListviewScrollerDragState.ActiveFlag) != 0)
			return true;
		return TryReadHorizontalScrollerDragStateAdmission(ref platform, state,
			obj, out var horizontal, out present) && present &&
			(horizontal.Flags &
				MuiListviewHorizontalScrollerDragState.ActiveFlag) != 0;
	}

	private static bool CapturePointer<TPlatform>(ref TPlatform platform,
		APTR obj, MuiPointerCaptureKind kind, int x, int y)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sample = default(MuiPointerCaptureSample);
		sample.Object = obj;
		sample.Kind = kind;
		sample.StartX = x;
		sample.StartY = y;
		return platform.CaptureMuiPointer(ref sample);
	}

	private static void ReleasePointer<TPlatform>(ref TPlatform platform,
		APTR obj, MuiPointerCaptureKind kind, int x, int y)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sample = default(MuiPointerCaptureSample);
		sample.Object = obj;
		sample.Kind = kind;
		sample.StartX = x;
		sample.StartY = y;
		_ = platform.ReleaseMuiPointer(ref sample);
	}

	// Handle the stable MorphOS Listview keyboard navigation set. The packet
	// itself is decoded by the collection surface codec; this control consumes
	// only the signed MUIKEY and forwards a named ListActive selector to its
	// child. ListCore then clamps the active row and keeps it visible by updating
	// the child's ListFirst value. The IntuiMessage remains part of the ABI but
	// is intentionally not needed for this key-only path.
	public static bool HandleInput<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR intuiMessage, int muiKey)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (MuiListCore.Classify(ref platform, state, obj) !=
			MuiCollectionClass.Listview)
			return false;
		var child = ChildList(ref platform, state, obj);
		// Cancellation is intentionally checked before the entry-count guard. A
		// list can be emptied or detached while a pointer drag is in flight, and
		// the guest-resident state must still be released without dereferencing a
		// now-invalid row. It also precedes the input-enabled gate so toggling
		// MUIA_Listview_Input off cannot strand an already-armed drag.
		if (IsDragCancelKey(muiKey))
			return CancelDrag(ref platform, state, obj, child);
		if (child.IsNull) return false;
		if (PolicyValue(ref platform, state, obj, Input, 1) == 0)
			return false;
		if (muiKey == KeyLeft || muiKey == KeyRight)
			return HandleHorizontalKey(ref platform, state, obj, child, muiKey);
		if (MuiListCore.EntryCount(ref platform, state, child) == 0)
			return false;
		if (muiKey == KeyNone)
		{
			if (!MuiIntuiMessageCodec.TryReadPointer(ref platform, intuiMessage,
				out var pointer)) return false;
			return HandlePointer(ref platform, state, obj, child, pointer);
		}
		if (muiKey == KeyPress)
		{
			if (!EnsureClickState(ref platform, state, obj)) return false;
			if (!SelectActive(ref platform, state, obj, child, false))
				return false;
			// MorphOS treats keyboard activation as a click on the active row.
			// The caller-selected default column is published through the same
			// named click-state record used by pointer activation; no parallel
			// scalar or packet offset is introduced.
			var column = DefaultClickColumnValue(ref platform, state, obj);
			return PublishClickState(ref platform, state, obj, column, 1, false,
				false);
		}
		if (muiKey == KeyToggle)
			return SelectActive(ref platform, state, obj, child, true);
		if (!TryMapInputKey(muiKey, out var selector)) return false;
		var oldActive = MuiListCore.ActiveRow(ref platform, state, child);
		var oldFirst = MuiListCore.FirstCursor(ref platform, state, child);
		if (!MuiListCore.SetAttribute(ref platform, state, child, ListActive,
			unchecked((uint)selector), true)) return false;
		// ListActive navigation may move First to keep the active row visible.
		// Republish the named viewport record through the List seam so keyboard,
		// wheel, and scroller paths expose the same pixel metrics.
		if (!MuiListCore.RefreshViewportMetrics(ref platform, state, child))
			return false;
		var newActive = MuiListCore.ActiveRow(ref platform, state, child);
		var newFirst = MuiListCore.FirstCursor(ref platform, state, child);
		return oldActive != newActive || oldFirst != newFirst;
	}

	// Translate the bounded Intuition pointer sequence into either the existing
	// click/selection seam or the child List's struct-backed drag-sort seam.
	// Hit-testing stays in ListCore so title rows, viewport origin, column order,
	// and cell offsets cannot diverge between MUIM_List_TestPos and Listview.
	internal static bool HandlePointer<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR child, MuiIntuiPointerMessage pointer)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (pointer.Class == IdcmpMouseButtons)
		{
			if (pointer.Code == WheelUp || pointer.Code == WheelDown ||
				pointer.Code == WheelLeft || pointer.Code == WheelRight)
			{
				// MorphOS 3.20 forwards wheel events while a drag owns the
				// pointer, allowing the eventual drop target to consume them.
				if (HasActivePointerGrab(ref platform, state, obj)) return false;
				if (pointer.Code == WheelUp || pointer.Code == WheelDown)
					return HandleVerticalWheel(ref platform, state, obj, pointer.Code);
				return HandleHorizontalWheel(ref platform, state, obj, child,
					pointer.Code);
			}
			if (pointer.Code == SelectDown)
			{
				if (BeginDrag(ref platform, state, obj, child, pointer)) return true;
				if (BeginScrollerDrag(ref platform, state, obj, pointer)) return true;
				return BeginHorizontalScrollerDrag(ref platform, state, obj, pointer);
			}
			if (pointer.Code != SelectUp) return false;
			if (FinishDrag(ref platform, state, obj, child, pointer)) return true;
			if (FinishScrollerDrag(ref platform, state, obj, pointer)) return true;
			if (FinishHorizontalScrollerDrag(ref platform, state, obj, pointer))
				return true;
			if (HandleScrollerTrackClick(ref platform, state, obj, pointer))
				return true;
			if (HandleHorizontalScrollerTrackClick(ref platform, state, obj,
				pointer)) return true;
		}
		else if (pointer.Class == IdcmpMouseMove)
		{
			if (UpdateDrag(ref platform, state, obj, child, pointer)) return true;
			if (UpdateScrollerDrag(ref platform, state, obj, pointer)) return true;
			if (UpdateHorizontalScrollerDrag(ref platform, state, obj, pointer))
				return true;
			// A passive mouse move is not a click.  Once all active pointer
			// grabs have declined the event, leave it unconsumed instead of
			// falling through to row hit-testing and changing List_Active or
			// selection merely because the pointer crossed a row.
			return false;
		}
		else return false;

		if (!MuiListCore.TryTestPos(ref platform, state, child, pointer.MouseX,
			pointer.MouseY, out var hit)) return false;
		var column = hit.Column < 0 ? uint.MaxValue : unchecked((uint)hit.Column);
		if (hit.Entry < 0)
			return hit.Flags == 0 && column != uint.MaxValue &&
				MuiListCore.HandleTitleClick(ref platform, state, child, column);
		var shifted = (pointer.Qualifier & QualifierMultiSelect) != 0;
		return HandleClick(ref platform, state, obj, hit.Entry, 1, column, shifted);
	}

	private static bool BeginDrag<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR child, MuiIntuiPointerMessage pointer)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		// This increment intentionally implements only the local Listview/List
		// drag-sort contract. External MUIM_Drag* routing remains a separate Area
		// capability and is not faked by claiming a pointer event here.
		if (PolicyValue(ref platform, state, obj, DragType, DragTypeNone) !=
			DragTypeImmediate || Read(ref platform, state, child, ListDragSortable,
				0) == 0 || Read(ref platform, state, child, ListDragType,
				DragTypeNone) != DragTypeImmediate) return false;
		if (!MuiListCore.TryTestPos(ref platform, state, child, pointer.MouseX,
			pointer.MouseY, out var hit) || hit.Entry < 0) return false;
		var block = EnsureDragState(ref platform, state, obj);
		if (block.IsNull) return false;
		var value = default(MuiListviewDragState);
		value.Magic = MuiListviewDragStateCodec.Cookie;
		value.Source = hit.Entry;
		value.Target = hit.Entry;
		value.StartX = pointer.MouseX;
		value.StartY = pointer.MouseY;
		value.LastX = pointer.MouseX;
		value.LastY = pointer.MouseY;
		value.Flags = MuiListviewDragState.ActiveFlag;
		if (CapturePointer(ref platform, obj, MuiPointerCaptureKind.ListDrag,
			pointer.MouseX, pointer.MouseY))
			value.Flags |= MuiListviewDragState.CapturedFlag;
		MuiListviewDragStateCodec.Write(ref platform, block, value);
		return true;
	}

	private static bool UpdateDrag<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR child, MuiIntuiPointerMessage pointer)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj, DragStateKey,
			0));
		if (!TryReadDragStateAdmission(ref platform, state, obj, out var value,
			out var present) || !present ||
			(value.Flags & MuiListviewDragState.ActiveFlag) == 0)
			return false;
		if (pointer.MouseX != value.StartX || pointer.MouseY != value.StartY)
			value.Flags |= MuiListviewDragState.MovedFlag;
		value.LastX = pointer.MouseX;
		value.LastY = pointer.MouseY;
		var target = -1;
		if (MuiListCore.TryTestPos(ref platform, state, child, pointer.MouseX,
			pointer.MouseY, out var hit))
			target = ResolveDragTarget(ref platform, state, child, hit,
			pointer.MouseX, pointer.MouseY);
		if (target >= 0)
		{
			if (target != value.Target)
			{
				value.Target = target;
				MuiListCore.SetDropMark(ref platform, state, child, target);
			}
		}
		else
		{
			// Leaving the child viewport invalidates the insertion target. Clear the
			// public marker immediately so SELECTUP cannot commit a stale row after
			// the pointer has moved outside the List geometry.
			if (value.Target >= 0)
				MuiListCore.SetDropMark(ref platform, state, child, -1);
			value.Target = -1;
		}
		MuiListviewDragStateCodec.Write(ref platform, block, value);
		return true;
	}

	private static bool FinishDrag<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR child, MuiIntuiPointerMessage pointer)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj, DragStateKey,
			0));
		if (!TryReadDragStateAdmission(ref platform, state, obj, out var value,
			out var present) || !present ||
			(value.Flags & MuiListviewDragState.ActiveFlag) == 0)
			return false;
		var moved = (value.Flags & MuiListviewDragState.MovedFlag) != 0;
		var changed = false;
		if (moved && value.Target >= 0 && value.Source != value.Target)
			changed = MuiListCore.DragMove(ref platform, state, child,
				value.Source, value.Target);
		MuiListCore.SetDropMark(ref platform, state, child, -1);
		ReleaseDragState(ref platform, state, obj, block);
		_ = pointer;
		// A moved drag is consumed even if the child rejected the final reorder;
		// otherwise SELECTUP would unexpectedly become a normal click.
		return moved || changed;
	}

	private static int ResolveDragTarget<TPlatform>(ref TPlatform platform,
		APTR state, APTR child, MuiListTestPosResult hit, int x, int y)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (hit.Entry >= 0) return hit.Entry;
		// A pointer outside the child viewport is cancellation, not an append
		// request.  TestPos can report the same Below flag for both cases, so use
		// the child geometry to keep the boundary insertion strictly in-viewport.
		if (!MuiAreaLayoutCore.TryReadGeometryState(ref platform, state, child,
			out var childGeometry)) return -1;
		var height = childGeometry.Height;
		var width = childGeometry.Width;
		if (x < 0 || y < 0 || width <= 0 || height <= 0 || x >= width ||
			y >= height) return -1;
		var count = MuiListCore.EntryCount(ref platform, state, child);
		if (count == 0) return -1;
		if ((hit.Flags & MuiListTestPosResult.FlagBelow) != 0)
			return unchecked((int)count);
		if ((hit.Flags & MuiListTestPosResult.FlagAbove) != 0)
			return 0;
		return -1;
	}

	private static bool CancelDrag<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR child)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var handled = false;
		var block = APTR.FromPointer(Read(ref platform, state, obj, DragStateKey,
			0));
		if (TryReadDragStateAdmission(ref platform, state, obj, out var value,
			out var present) && present &&
			(value.Flags & MuiListviewDragState.ActiveFlag) != 0)
		{
			// Clear the public child marker before releasing the private record.
			// This keeps a cancelled drag from leaving a stale insertion cue behind
			// even when no later SELECTUP arrives.
			if (child.IsNotNull)
				MuiListCore.SetDropMark(ref platform, state, child, -1);
			ReleaseDragState(ref platform, state, obj, block);
			handled = true;
		}
		var scrollerBlock = APTR.FromPointer(Read(ref platform, state, obj,
			ScrollerDragStateKey, 0));
		if (TryReadScrollerDragStateAdmission(ref platform, state, obj,
			out var scrollerValue, out present) && present &&
			(scrollerValue.Flags & MuiListviewScrollerDragState.ActiveFlag) != 0)
		{
			ReleaseScrollerDragState(ref platform, state, obj, scrollerBlock);
			handled = true;
		}
		var horizontalBlock = APTR.FromPointer(Read(ref platform, state, obj,
			HorizontalScrollerDragStateKey, 0));
		if (TryReadHorizontalScrollerDragStateAdmission(ref platform, state,
			obj, out var horizontalValue, out present) && present &&
			(horizontalValue.Flags &
				MuiListviewHorizontalScrollerDragState.ActiveFlag) != 0)
		{
			ReleaseHorizontalScrollerDragState(ref platform, state, obj,
				horizontalBlock);
			handled = true;
		}
		return handled;
	}

	// Window focus loss is a cancellation edge for every Listview or Stringscroll
	// pointer gesture below that window. Walk the guest-resident Family topology
	// with the bounded GetChild selector; do not build a managed object graph or
	// infer child addresses from private offsets. A Listview or Stringscroll is a
	// leaf from this traversal's perspective, so stop at the composite after
	// cancelling its typed record rather than descending into implementation
	// children.
	internal static uint CancelPointerDragsInWindow<TPlatform>(
		ref TPlatform platform, APTR state, APTR window)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		CancelPointerDragsInTree(ref platform, state, window, 0);

	private static uint CancelPointerDragsInTree<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint depth)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (obj.IsNull || depth >= MuiHeadlessLayout.MaximumTraversal)
			return 0;
		var collectionClass = MuiListCore.Classify(ref platform, state, obj);
		if (collectionClass == MuiCollectionClass.Listview)
		{
			var child = ChildList(ref platform, state, obj);
			return CancelDrag(ref platform, state, obj, child) ? 1u : 0u;
		}
		if (collectionClass == MuiCollectionClass.Stringscroll)
			return MuiStringscrollCore.CancelPointerDragForWindow(ref platform,
				state, obj) ? 1u : 0u;
		var cancelled = 0u;
		for (var index = 0u; index < MuiHeadlessLayout.MaximumTraversal;
			index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, obj,
				unchecked((int)index), APTR.Null);
			if (child.IsNull) break;
			cancelled += CancelPointerDragsInTree(ref platform, state, child,
				depth + 1);
		}
		return cancelled;
	}

	private static APTR EnsureDragState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadDragStateAdmission(ref platform, state, obj, out _,
			out var present)) return APTR.Null;
		var block = APTR.FromPointer(Read(ref platform, state, obj, DragStateKey,
			0));
		if (present) return block;
		block = MuiHeadlessMemory.Allocate(ref platform, MuiListviewDragState.Size);
		if (block.IsNull) return APTR.Null;
		var value = default(MuiListviewDragState);
		value.Magic = MuiListviewDragStateCodec.Cookie;
		value.Source = -1;
		value.Target = -1;
		if (MuiListviewDragStateCodec.TryWrite(ref platform, block, value) &&
			SetInternal(ref platform, state, obj, DragStateKey, block.Raw))
			return block;
		MuiListviewDragStateCodec.Clear(ref platform, block);
		platform.Free(block, MuiListviewDragState.Size);
		return APTR.Null;
	}

	// A published list drag record is authoritative guest state. A non-NULL
	// record that fails the cookie/field contract is malformed, not absence; a
	// fresh pointer gesture must not free it and arm a replacement drag.
	private static bool TryReadDragStateAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewDragState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			DragStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return MuiListviewDragStateCodec.TryRead(ref platform, block,
			out value) && IsValidDragState(value);
	}

	// The list-drag projection is a small state machine, not an arbitrary
	// coordinate snapshot.  Keep its named row sentinels and flag transitions
	// coherent before a following pointer gesture can reuse the published record.
	// Source is -1 only while the inactive storage block is parked; an active
	// drag always has a source row, while Target may be -1 when no in-viewport
	// drop target is present. A changed target must carry the Moved flag. Unknown
	// bits are rejected so future consumers cannot mistake unrelated state for a
	// capture or reorder transition.
	private static bool IsValidDragState(MuiListviewDragState value)
	{
		const uint knownFlags = MuiListviewDragState.ActiveFlag |
			MuiListviewDragState.MovedFlag |
			MuiListviewDragState.CapturedFlag;
		if ((value.Flags & ~knownFlags) != 0) return false;

		var active = (value.Flags & MuiListviewDragState.ActiveFlag) != 0;
		if (!active)
			return value.Flags == 0 && value.Source == -1 && value.Target == -1;

		if (value.Source < 0 || value.Target < -1) return false;
		return value.Target == value.Source ||
			(value.Flags & MuiListviewDragState.MovedFlag) != 0;
	}

	private static void ReleaseDragState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR block)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadDragStateAdmission(ref platform, state, obj, out var value,
			out var present) && present &&
			(value.Flags & MuiListviewDragState.CapturedFlag) != 0)
			ReleasePointer(ref platform, obj, MuiPointerCaptureKind.ListDrag,
				value.LastX, value.LastY);
		MuiListviewDragStateCodec.Clear(ref platform, block);
		if (block.IsNotNull && platform.IsMapped(block, MuiListviewDragState.Size))
			platform.Free(block, MuiListviewDragState.Size);
		SetInternal(ref platform, state, obj, DragStateKey, 0);
	}

	private static bool TryBuildScrollerGeometry<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListviewScrollerGeometry geometry)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		geometry = default;
		if (PolicyValue(ref platform, state, obj, ScrollerPos,
			ScrollerPosDefault) ==
			ScrollerPosNone || !GetScrollerState(ref platform, state, obj,
			out var entries, out var visible, out var first, out var maxFirst) ||
			entries <= visible) return false;
		if (!TryReadLayoutStateAdmission(ref platform, state, obj,
			out var layout, out var hasLayout)) return false;
		var areaGeometry = default(MuiAreaGeometryState);
		if (!hasLayout && !MuiAreaLayoutCore.TryReadGeometryState(ref platform,
			state, obj, out areaGeometry)) return false;
		var left = hasLayout ? layout.Left : areaGeometry.Left;
		var top = hasLayout ? layout.Top : areaGeometry.Top;
		var width = hasLayout ? layout.Width : areaGeometry.Width;
		var height = hasLayout ? layout.Height : areaGeometry.Height;
		// A visible horizontal scroller consumes the bottom part of the child
		// viewport, so the vertical track must stop above that named reserve.
		var child = ChildList(ref platform, state, obj);
		if (hasLayout && layout.ChildHeight < height)
			height = layout.ChildHeight;
		else if (child.IsNotNull)
		{
			var childHeight = height;
			if (MuiAreaLayoutCore.TryReadGeometryState(ref platform, state, child,
				out var childGeometry)) childHeight = childGeometry.Height;
			if (childHeight >= 0 && childHeight < height) height = childHeight;
		}
		if (width < (int)ScrollerWidth || height <= 0) return false;
		var position = PolicyValue(ref platform, state, obj, ScrollerPos,
			ScrollerPosDefault);
		var trackLeft = position == ScrollerPosLeft ? left :
			left + width - (int)ScrollerWidth;
		var trackRight = trackLeft + (int)ScrollerWidth - 1;
		var trackBottom = top + height - 1;
		var thumbHeight = ScaledRatio((uint)height, visible, entries);
		if (thumbHeight < 4) thumbHeight = 4;
		if (thumbHeight > (uint)height) thumbHeight = (uint)height;
		var travel = (uint)height - thumbHeight;
		var thumbTop = ScaledRatio(travel, first, maxFirst);
		geometry.TrackLeft = trackLeft;
		geometry.TrackTop = top;
		geometry.TrackRight = trackRight;
		geometry.TrackBottom = trackBottom;
		geometry.ThumbLeft = trackLeft + 2;
		geometry.ThumbTop = top + unchecked((int)thumbTop);
		geometry.ThumbRight = trackRight - 2;
		geometry.ThumbBottom = geometry.ThumbTop +
			unchecked((int)thumbHeight) - 1;
		geometry.First = first;
		geometry.MaxFirst = maxFirst;
		return true;
	}

	private static bool TryBuildHorizontalScrollerGeometry<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewHorizontalScrollerGeometry geometry)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		geometry = default;
		var child = ChildList(ref platform, state, obj);
		if (child.IsNull || !MuiListCore.TryGetHScrollerState(ref platform, state,
			child, out var hState) || hState.Visible == 0)
		{
			ReleaseHorizontalScrollerState(ref platform, state, obj);
			return false;
		}
		if (!TryReadLayoutStateAdmission(ref platform, state, obj,
			out var layout, out var hasLayout)) return false;
		var childGeometry = default(MuiAreaGeometryState);
		if (!hasLayout && !MuiAreaLayoutCore.TryReadGeometryState(ref platform,
			state, child, out childGeometry)) return false;
		var left = hasLayout ? layout.ChildLeft : childGeometry.Left;
		var top = hasLayout ? layout.ChildTop : childGeometry.Top;
		var width = hasLayout ? layout.ChildWidth : childGeometry.Width;
		var childHeight = hasLayout ? layout.ChildHeight : childGeometry.Height;
		if (width < 8 || childHeight < 0) return false;
		var trackTop = top + childHeight;
		var content = hState.ContentWidth;
		var view = hState.ViewWidth == 0 ? unchecked((uint)width) :
			hState.ViewWidth;
		if (content < view) content = view;
		var usableWidth = unchecked((uint)(width - 4));
		var thumbWidth = ScaledRatio(usableWidth, view, content);
		if (thumbWidth < 4) thumbWidth = 4;
		if (thumbWidth > usableWidth) thumbWidth = usableWidth;
		var travel = usableWidth - thumbWidth;
		var thumbOffset = ScaledRatio(travel, hState.ScrollX,
			hState.MaxScrollX);
		geometry.TrackLeft = left;
		geometry.TrackTop = trackTop;
		geometry.TrackRight = left + width - 1;
		geometry.TrackBottom = trackTop + unchecked((int)HScrollerHeight) - 1;
		geometry.ThumbLeft = left + 2 + unchecked((int)thumbOffset);
		geometry.ThumbTop = trackTop + 2;
		geometry.ThumbRight = geometry.ThumbLeft + unchecked((int)thumbWidth) - 1;
		geometry.ThumbBottom = geometry.TrackBottom - 2;
		geometry.ContentWidth = content;
		geometry.ViewWidth = view;
		geometry.ScrollX = hState.ScrollX;
		geometry.MaxScrollX = hState.MaxScrollX;
		return PublishHorizontalScrollerState(ref platform, state, obj,
			geometry, out geometry);
	}

	private static bool BeginScrollerDrag<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiIntuiPointerMessage pointer)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryBuildScrollerGeometry(ref platform, state, obj, out var geometry) ||
			!Contains(geometry.ThumbLeft, geometry.ThumbTop,
				geometry.ThumbRight, geometry.ThumbBottom, pointer.MouseX,
				pointer.MouseY)) return false;
		var block = EnsureScrollerDragState(ref platform, state, obj);
		if (block.IsNull) return false;
		var value = default(MuiListviewScrollerDragState);
		value.Magic = MuiListviewScrollerDragState.Cookie;
		value.GrabOffset = pointer.MouseY - geometry.ThumbTop;
		value.StartFirst = unchecked((int)geometry.First);
		value.LastPointer = pointer.MouseY;
		value.Flags = MuiListviewScrollerDragState.ActiveFlag;
		if (CapturePointer(ref platform, obj,
			MuiPointerCaptureKind.VerticalScroller, pointer.MouseX,
			pointer.MouseY))
			value.Flags |= MuiListviewScrollerDragState.CapturedFlag;
		if (MuiListviewScrollerDragStateCodec.Write(ref platform, block, value))
			return true;
		if ((value.Flags & MuiListviewScrollerDragState.CapturedFlag) != 0)
			ReleasePointer(ref platform, obj,
				MuiPointerCaptureKind.VerticalScroller, pointer.MouseX,
				pointer.MouseY);
		ReleaseScrollerDragState(ref platform, state, obj, block);
		return false;
	}

	private static bool UpdateScrollerDrag<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiIntuiPointerMessage pointer)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ScrollerDragStateKey, 0));
		if (!TryReadScrollerDragStateAdmission(ref platform, state, obj,
			out var value, out var present) || !present || (value.Flags &
			MuiListviewScrollerDragState.ActiveFlag) == 0) return false;
		if (!TryBuildScrollerGeometry(ref platform, state, obj,
			out var geometry)) return false;
		var thumbHeight = geometry.ThumbBottom - geometry.ThumbTop + 1;
		var travel = geometry.TrackBottom - geometry.TrackTop + 1 - thumbHeight;
		if (travel <= 0 || geometry.MaxFirst == 0) return true;
		var desired = pointer.MouseY - geometry.TrackTop - value.GrabOffset;
		if (desired < 0) desired = 0;
		if (desired > travel) desired = travel;
		var target = unchecked((int)((uint)desired * geometry.MaxFirst /
			unchecked((uint)travel)));
		value.LastPointer = pointer.MouseY;
		if (!MuiListviewScrollerDragStateCodec.Write(ref platform, block, value))
			return false;
		SetScrollerFirst(ref platform, state, obj, target);
		return true;
	}

	private static bool FinishScrollerDrag<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiIntuiPointerMessage pointer)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ScrollerDragStateKey, 0));
		if (!TryReadScrollerDragStateAdmission(ref platform, state, obj,
			out var value, out var present) || !present || (value.Flags &
			MuiListviewScrollerDragState.ActiveFlag) == 0) return false;
		UpdateScrollerDrag(ref platform, state, obj, pointer);
		ReleaseScrollerDragState(ref platform, state, obj, block);
		return true;
	}

	private static bool HandleScrollerTrackClick<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiIntuiPointerMessage pointer)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryBuildScrollerGeometry(ref platform, state, obj,
			out var geometry) || !Contains(geometry.TrackLeft, geometry.TrackTop,
				geometry.TrackRight, geometry.TrackBottom, pointer.MouseX,
				pointer.MouseY)) return false;
		var thumbHeight = geometry.ThumbBottom - geometry.ThumbTop + 1;
		var travel = geometry.TrackBottom - geometry.TrackTop + 1 - thumbHeight;
		if (travel <= 0 || geometry.MaxFirst == 0) return true;
		var desired = pointer.MouseY - geometry.TrackTop - thumbHeight / 2;
		if (desired < 0) desired = 0;
		if (desired > travel) desired = travel;
		var target = unchecked((int)((uint)desired * geometry.MaxFirst /
			unchecked((uint)travel)));
		SetScrollerFirst(ref platform, state, obj, target);
		return true;
	}

	private static APTR EnsureScrollerDragState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadScrollerDragStateAdmission(ref platform, state, obj,
			out _, out var present)) return APTR.Null;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ScrollerDragStateKey, 0));
		if (present) return block;
		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListviewScrollerDragState.Size);
		if (block.IsNull) return APTR.Null;
		var value = default(MuiListviewScrollerDragState);
		value.Magic = MuiListviewScrollerDragState.Cookie;
		if (!MuiListviewScrollerDragStateCodec.Write(ref platform, block, value) ||
			!SetInternal(ref platform, state, obj, ScrollerDragStateKey, block.Raw))
		{
			if (platform.IsMapped(block, MuiListviewScrollerDragState.Size))
				platform.Clear(block, MuiListviewScrollerDragState.Size);
			platform.Free(block, MuiListviewScrollerDragState.Size);
			return APTR.Null;
		}
		return block;
	}

	// A published vertical drag record is authoritative guest state. A
	// non-NULL record that fails the cookie/field contract is malformed, not
	// absence; pointer admission must not free it and arm a replacement grab.
	private static bool TryReadScrollerDragStateAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewScrollerDragState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			ScrollerDragStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return MuiListviewScrollerDragStateCodec.TryRead(ref platform, block,
			out value) && IsValidScrollerDragState(value);
	}

	// The vertical thumb record is a bounded pointer-grab state machine. Its
	// inactive storage is the all-zero value; an active record needs a
	// non-negative grab offset and first-row origin. Keep LastPointer signed so
	// controls positioned above the window origin remain representable. Unknown
	// flag bits are rejected before pointer movement can consume the record.
	private static bool IsValidScrollerDragState(
		MuiListviewScrollerDragState value)
	{
		const uint knownFlags = MuiListviewScrollerDragState.ActiveFlag |
			MuiListviewScrollerDragState.CapturedFlag;
		if ((value.Flags & ~knownFlags) != 0) return false;
		if ((value.Flags & MuiListviewScrollerDragState.ActiveFlag) == 0)
			return value.Flags == 0 && value.GrabOffset == 0 &&
				value.StartFirst == 0 && value.LastPointer == 0;
		return value.GrabOffset >= 0 && value.StartFirst >= 0;
	}

	private static void ReleaseScrollerDragState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR block)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadScrollerDragStateAdmission(ref platform, state, obj,
			out var value, out var present) && present && (value.Flags &
			MuiListviewScrollerDragState.CapturedFlag) != 0)
			ReleasePointer(ref platform, obj,
				MuiPointerCaptureKind.VerticalScroller, 0, value.LastPointer);
		if (block.IsNotNull && platform.IsMapped(block,
			MuiListviewScrollerDragState.Size))
			platform.Free(block, MuiListviewScrollerDragState.Size);
		SetInternal(ref platform, state, obj, ScrollerDragStateKey, 0);
	}

	private static bool Contains(int left, int top, int right, int bottom,
		int x, int y) => x >= left && x <= right && y >= top && y <= bottom;

	private static bool SelectActive<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR child, bool toggle)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var active = MuiListCore.ActiveRow(ref platform, state, child);
		var count = MuiListCore.EntryCount(ref platform, state, child);
		if (active < 0 || (uint)active >= count) return false;
		var multiSelect = PolicyValue(ref platform, state, obj, MultiSelect,
			MultiSelectDefault);
		if (toggle && multiSelect != MultiSelectNone)
			return MuiListCore.Select(ref platform, state, child, active,
				SelectToggle, APTR.Null);
		return MuiListCore.SelectExclusive(ref platform, state, child, active);
	}

	// Apply a click on a list entry. Honours MUIA_Listview_Input (a FALSE
	// listview is read-only), MUIA_Listview_MultiSelect (single vs shifted vs
	// always), and MUIA_Listview_DoubleClick / MUIA_Listview_ClickColumn.
	// Selection changes flow through the child list, which raises
	// MUIA_List(view)_SelectChange. Returns true when the click was handled.
	public static bool HandleClick<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int entry, int clicks, uint column, bool shift)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiListCore.Classify(ref platform, state, obj) !=
			MuiCollectionClass.Listview) return false;
		if (PolicyValue(ref platform, state, obj, Input, 1) == 0)
			return false; // readonly
		var child = ChildList(ref platform, state, obj);
		if (child.IsNull) return false;
		var count = MuiListCore.EntryCount(ref platform, state, child);
		if (entry < 0 || (uint)entry >= count) return false;
		// Admit click state before changing the child active/selection records;
		// malformed typed state must not leave a partial input transition.
		if (!EnsureClickState(ref platform, state, obj)) return false;

		if (!MuiListCore.SetAttribute(ref platform, state, child, ListActive,
			unchecked((uint)entry), true) ||
			!MuiListCore.RefreshViewportMetrics(ref platform, state, child))
			return false;

		var multiSelect = PolicyValue(ref platform, state, obj, MultiSelect,
			MultiSelectDefault);
		var multi = multiSelect == MultiSelectAlways ||
			(shift && multiSelect != MultiSelectNone);
		if (multi)
		{
			// MUIA_List_MultiTestHook (when present) decides per entry whether it
			// may join the multiselection. It is entered with A0 = hook base (so
			// h_Data is reachable), A2 = listview object, A1 = entry pointer, and
			// returns FALSE to deny. A denied entry leaves the existing selection
			// untouched; the active row still moves.
			var permitted = true;
			var testHook = MuiListCore.HookPolicyValue(ref platform, state,
				child, MultiTestHook);
			if (testHook != 0)
			{
				var entryPtr = MuiListCore.GetEntry(ref platform, state, child,
					entry, APTR.Null);
				permitted = platform.InvokeHook(APTR.FromPointer(testHook), obj,
					entryPtr) != 0;
			}
			if (permitted)
				MuiListCore.Select(ref platform, state, child, entry, SelectToggle,
					APTR.Null);
		}
		else
		{
			MuiListCore.SelectExclusive(ref platform, state, child, entry);
		}

		var resolvedColumn = column == 0xFFFFFFFFu
			? DefaultClickColumnValue(ref platform, state, obj) : column;
		var clickCount = clicks < 0 ? 0u : unchecked((uint)clicks);
		var doubleClick = clicks == 2;
		var againClick = clicks >= 3;
		return PublishClickState(ref platform, state, obj, resolvedColumn,
			clickCount, doubleClick, againClick);
	}

	private static bool PublishClickState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint column, uint clicks, bool doubleClick,
		bool againClick) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureClickState(ref platform, state, obj)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ClickStateKey, 0));
		if (!TryReadClickState(ref platform, block, out var value)) return false;
		value.ClickColumn = column;
		value.Clicks = clicks;
		value.DoubleClick = doubleClick ? 1u : 0u;
		value.AgainClick = againClick ? 1u : 0u;
		WriteClickState(ref platform, block, value);
		// The named click record is authoritative. Publish the event projections
		// only after it is complete so notification readers see one coherent
		// result, including repeated clicks with the same value.
		var parentPublished = MuiHeadlessObjectCore.SetAttribute(ref platform,
			state, obj, ClickColumn, column, true);
		if (!parentPublished) return false;
		parentPublished = againClick
			? MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				AgainClick, 1, true)
			: SetInternal(ref platform, state, obj, AgainClick, 0);
		if (!parentPublished) return false;
		parentPublished = doubleClick
			? MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				DoubleClick, 1, true)
			: SetInternal(ref platform, state, obj, DoubleClick, 0);
		if (!parentPublished) return false;
		var child = ChildList(ref platform, state, obj);
		return child.IsNotNull && MuiListCore.PublishClickState(ref platform,
			state, child, column, clicks, doubleClick, againClick, false);
	}

	// Struct-first qualification seam for the public click-result contract.
	// The full HandleClick path above owns selection and object notifications;
	// this bounded writer lets a freestanding root verify the exact guest record
	// transitions without pulling in the complete Listview composite closure.
	public static bool WriteClickResult<TPlatform>(ref TPlatform platform,
		APTR storage, uint column, uint clicks)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (storage.IsNull || !platform.IsMapped(storage,
			MuiListviewClickState.Size)) return false;
		var value = default(MuiListviewClickState);
		value.Magic = MuiListviewClickState.Cookie;
		value.ClickColumn = column;
		value.Clicks = clicks;
		value.DoubleClick = clicks == 2 ? 1u : 0u;
		value.AgainClick = clicks >= 3 ? 1u : 0u;
		WriteClickState(ref platform, storage, value);
		return true;
	}

	// ---- Group layout / draw / min-max ---------------------------------------

	// Lay out the composite: reserve scrollbar space per MUIA_Listview_ScrollerPos
	// and give the remaining rectangle to the child list, then record the
	// listview's own geometry.
	public static bool Layout<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int left, int top, int width, int height)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (MuiListCore.Classify(ref platform, state, obj) !=
			MuiCollectionClass.Listview || width < 0 || height < 0) return false;
		var child = ChildList(ref platform, state, obj);
		if (child.IsNull) return false;
		var scrollerPos = PolicyValue(ref platform, state, obj, ScrollerPos,
			ScrollerPosDefault);
		var scroller = scrollerPos == ScrollerPosNone ? 0 : (int)ScrollerWidth;
		var listWidth = width - scroller;
		if (listWidth < 0) listWidth = 0;
		var listLeft = scrollerPos == ScrollerPosLeft ? left + scroller : left;
		// The child List owns HScrollerVisibility and its named content-width
		// state. Resolve that policy before laying out the child so a visible
		// horizontal track consumes real bottom space instead of overlapping the
		// last data row. Auto remains conservative until a content measurement is
		// published; Always still reserves the track for an empty list.
		var childHeight = height;
		if (MuiListCore.TryGetHScrollerState(ref platform, state, child,
			out var hState))
		{
			if (!MuiListCore.SetHScrollerViewport(ref platform, state, child,
				hState.ContentWidth, unchecked((uint)listWidth)) ||
				!MuiListCore.TryGetHScrollerState(ref platform, state, child,
					out hState)) return false;
			if (hState.Visible != 0)
			{
				childHeight -= unchecked((int)HScrollerHeight);
				if (childHeight < 0) childHeight = 0;
			}
		}
		if (!MuiListCore.Layout(ref platform, state, child, listLeft, top,
			listWidth, childHeight)) return false;
		// The composite and its owned List share the same rastport, but the child
		// still needs its own Area render-info binding so its geometry/draw path
		// can run without a host callback or a second managed render object.
		if (!BindChildRenderInfo(ref platform, state, obj, child)) return false;
		// The child List has already refreshed its named viewport record. Use its
		// effective line height here instead of assuming the default eight-pixel
		// row, so Listview and List publish the same visible-row capacity when a
		// MorphOS MinLineHeight or AutoLineHeight policy is active.
		var lineHeight = ListRowHeight;
		if (MuiListCore.TryGetViewportState(ref platform, state, child,
			out var childViewport) && childViewport.LineHeight != 0)
			lineHeight = childViewport.LineHeight;
		var visibleRows = childHeight <= 0 ? MuiListCore.VisibleOff :
			unchecked((uint)childHeight) / lineHeight;
		if (visibleRows == 0) visibleRows = 1;
		var titleRows = MuiListCore.TitleRowCount(ref platform, state, child);
		if (visibleRows != MuiListCore.VisibleOff)
			visibleRows = visibleRows > titleRows ? visibleRows - titleRows : 0;
		if (!MuiListCore.SetAttribute(ref platform, state, child, ListVisible,
			visibleRows, false)) return false;
		// A resize can reduce the legal first-row range. Re-apply the bounded
		// List setter after publishing the updated viewport so the child state never
		// advertises a stale position even when no user scroll event occurred.
		var entries = MuiListCore.EntryCount(ref platform, state, child);
		var maxFirst = visibleRows != MuiListCore.VisibleOff &&
			entries > visibleRows ? entries - visibleRows : 0u;
		var currentFirst = MuiListCore.FirstCursor(ref platform, state, child);
		if (visibleRows == MuiListCore.VisibleOff)
		{
			if (!MuiListCore.SetAttribute(ref platform, state, child, ListFirst,
				MuiListCore.VisibleOff, false)) return false;
		}
		else if (currentFirst > maxFirst && !MuiListCore.SetAttribute(ref platform,
			state, child, ListFirst, maxFirst, false)) return false;
		// Resizing can alter both the visible row count and the bounded First
		// position. Publish the named viewport record even when First remains
		// legal, so pixel metrics never depend on a later input event.
		if (!MuiListCore.RefreshViewportMetrics(ref platform, state, child))
			return false;
		if (!MuiAreaLayoutCore.Layout(ref platform, state, obj, left, top, width,
			height)) return false;
		if (!PublishLayoutState(ref platform, state, obj, child) ||
			!PublishRenderState(ref platform, state, obj)) return false;
		if (!GetScrollerState(ref platform, state, obj, out _, out _, out _,
			out _)) return false;
		// Publish the horizontal projection after both child layout and the
		// composite rectangle are current.  A hidden/disabled track simply retires
		// its prior record and remains a normal no-scroller layout.
		_ = TryBuildHorizontalScrollerGeometry(ref platform, state, obj,
			out _);
		return true;
	}

	// Draw the listview surround, scrollbar, and owned child rows, then retain
	// the redraw scheduling seam used by the composite event loop.
	public static bool Draw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint flags) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (MuiListCore.Classify(ref platform, state, obj) !=
			MuiCollectionClass.Listview) return false;
		if (!MuiAreaLayoutCore.Draw(ref platform, state, obj, flags)) return false;
		if (!DrawScroller(ref platform, state, obj)) return false;
		if (!DrawHorizontalScroller(ref platform, state, obj)) return false;
		var child = ChildList(ref platform, state, obj);
		if (child.IsNotNull)
		{
			if (!BindChildRenderInfo(ref platform, state, obj, child) ||
				!MuiListCore.Draw(ref platform, state, child, flags)) return false;
			platform.ScheduleRedraw(child, flags);
		}
		return true;
	}

	private static bool DrawHorizontalScroller<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryBuildHorizontalScrollerGeometry(ref platform, state, obj,
			out var geometry)) return true;
		if (!TryGetRenderContext(ref platform, state, obj,
			out var renderContext)) return true;
		var rastPort = renderContext.RastPort;
		if (rastPort.IsNull || !platform.LockLayer(rastPort)) return false;
		if (!platform.BeginUpdate(rastPort))
		{
			platform.UnlockLayer(rastPort);
			return false;
		}
		platform.SetPen(rastPort, 4);
		platform.FillRectangle(rastPort, geometry.TrackLeft,
			geometry.TrackTop, geometry.TrackRight, geometry.TrackBottom);
		platform.SetPen(rastPort, 6);
		platform.FillRectangle(rastPort, geometry.ThumbLeft,
			geometry.ThumbTop, geometry.ThumbRight, geometry.ThumbBottom);
		platform.EndUpdate(rastPort, true);
		platform.UnlockLayer(rastPort);
		return true;
	}

	private static bool BeginHorizontalScrollerDrag<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiIntuiPointerMessage pointer)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryBuildHorizontalScrollerGeometry(ref platform, state, obj,
			out var geometry) || !Contains(geometry.ThumbLeft, geometry.ThumbTop,
				geometry.ThumbRight, geometry.ThumbBottom, pointer.MouseX,
				pointer.MouseY)) return false;
		var block = EnsureHorizontalScrollerDragState(ref platform, state, obj);
		if (block.IsNull) return false;
		var value = default(MuiListviewHorizontalScrollerDragState);
		value.Magic = MuiListviewHorizontalScrollerDragState.Cookie;
		value.GrabOffset = pointer.MouseX - geometry.ThumbLeft;
		value.StartScroll = geometry.ScrollX;
		value.LastPointer = pointer.MouseX;
		value.Flags = MuiListviewHorizontalScrollerDragState.ActiveFlag;
		if (CapturePointer(ref platform, obj,
			MuiPointerCaptureKind.HorizontalScroller, pointer.MouseX,
			pointer.MouseY))
			value.Flags |= MuiListviewHorizontalScrollerDragState.CapturedFlag;
		if (MuiListviewHorizontalScrollerDragStateCodec.Write(ref platform, block,
			value)) return true;
		if ((value.Flags &
			MuiListviewHorizontalScrollerDragState.CapturedFlag) != 0)
			ReleasePointer(ref platform, obj,
				MuiPointerCaptureKind.HorizontalScroller, pointer.MouseX,
				pointer.MouseY);
		ReleaseHorizontalScrollerDragState(ref platform, state, obj, block);
		return false;
	}

	private static bool UpdateHorizontalScrollerDrag<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiIntuiPointerMessage pointer)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			HorizontalScrollerDragStateKey, 0));
		if (!TryReadHorizontalScrollerDragStateAdmission(ref platform, state,
			obj, out var value, out var present) || !present || (value.Flags &
			MuiListviewHorizontalScrollerDragState.ActiveFlag) == 0) return false;
		if (!TryBuildHorizontalScrollerGeometry(ref platform, state, obj,
			out var geometry)) return false;
		var thumbWidth = geometry.ThumbRight - geometry.ThumbLeft + 1;
		var travel = geometry.TrackRight - geometry.TrackLeft + 1 - 4 - thumbWidth;
		if (travel <= 0 || geometry.MaxScrollX == 0) return true;
		var desired = pointer.MouseX - (geometry.TrackLeft + 2) -
			value.GrabOffset;
		if (desired < 0) desired = 0;
		if (desired > travel) desired = travel;
		var target = unchecked((uint)desired) * geometry.MaxScrollX /
			unchecked((uint)travel);
		value.LastPointer = pointer.MouseX;
		if (!MuiListviewHorizontalScrollerDragStateCodec.Write(ref platform,
			block, value)) return false;
		var child = ChildList(ref platform, state, obj);
		if (child.IsNull || !MuiListCore.SetHScrollerScroll(ref platform, state,
			child, target)) return false;
		_ = TryBuildHorizontalScrollerGeometry(ref platform, state, obj,
			out _);
		return true;
	}

	private static bool FinishHorizontalScrollerDrag<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiIntuiPointerMessage pointer)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			HorizontalScrollerDragStateKey, 0));
		if (!TryReadHorizontalScrollerDragStateAdmission(ref platform, state,
			obj, out var value, out var present) || !present || (value.Flags &
			MuiListviewHorizontalScrollerDragState.ActiveFlag) == 0) return false;
		UpdateHorizontalScrollerDrag(ref platform, state, obj, pointer);
		ReleaseHorizontalScrollerDragState(ref platform, state, obj, block);
		return true;
	}

	private static bool HandleHorizontalScrollerTrackClick<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiIntuiPointerMessage pointer)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryBuildHorizontalScrollerGeometry(ref platform, state, obj,
			out var geometry) || !Contains(geometry.TrackLeft, geometry.TrackTop,
				geometry.TrackRight, geometry.TrackBottom, pointer.MouseX,
				pointer.MouseY)) return false;
		if (Contains(geometry.ThumbLeft, geometry.ThumbTop,
			geometry.ThumbRight, geometry.ThumbBottom, pointer.MouseX,
			pointer.MouseY)) return true;
		var thumbWidth = geometry.ThumbRight - geometry.ThumbLeft + 1;
		var travel = geometry.TrackRight - geometry.TrackLeft + 1 - 4 - thumbWidth;
		if (travel <= 0 || geometry.MaxScrollX == 0) return true;
		var desired = pointer.MouseX - geometry.TrackLeft - 2 - thumbWidth / 2;
		if (desired < 0) desired = 0;
		if (desired > travel) desired = travel;
		var target = unchecked((uint)desired) * geometry.MaxScrollX /
			unchecked((uint)travel);
		var child = ChildList(ref platform, state, obj);
		if (child.IsNull || !MuiListCore.SetHScrollerScroll(ref platform, state,
			child, target)) return false;
		_ = TryBuildHorizontalScrollerGeometry(ref platform, state, obj,
			out _);
		return true;
	}

	private static bool DrawScroller<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryBuildScrollerGeometry(ref platform, state, obj,
			out var geometry)) return true;
		if (!TryGetRenderContext(ref platform, state, obj,
			out var renderContext)) return true;
		var rastPort = renderContext.RastPort;
		if (rastPort.IsNull || !platform.LockLayer(rastPort)) return false;
		if (!platform.BeginUpdate(rastPort))
		{
			platform.UnlockLayer(rastPort);
			return false;
		}
		platform.SetPen(rastPort, 4);
		platform.FillRectangle(rastPort, geometry.TrackLeft, geometry.TrackTop,
			geometry.TrackRight, geometry.TrackBottom);
		platform.SetPen(rastPort, 6);
		platform.FillRectangle(rastPort, geometry.ThumbLeft,
			geometry.ThumbTop, geometry.ThumbRight, geometry.ThumbBottom);
		platform.EndUpdate(rastPort, true);
		platform.UnlockLayer(rastPort);
		return true;
	}

	// Computes floor(extent * value / total) without 64-bit arithmetic. MUI
	// dimensions are bounded by MUI_MAXMAX, so the loop has a fixed 10,000-step
	// ceiling and every intermediate remains below total.
	private static uint ScaledRatio(uint extent, uint value, uint total)
	{
		if (extent == 0 || value == 0 || total == 0) return 0;
		var boundedExtent = extent > 10000 ? 10000u : extent;
		if (value >= total) return boundedExtent;
		var result = 0u;
		var remainder = 0u;
		var threshold = total - value;
		for (var i = 0u; i < boundedExtent; i++)
		{
			if (remainder >= threshold)
			{
				remainder -= threshold;
				result++;
			}
			else
			{
				remainder += value;
			}
		}
		return result;
	}


	// Report the composite min/max: the child list's requirements plus the
	// reserved scrollbar extent.
	public static bool AskMinMax<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR storage) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (MuiListCore.Classify(ref platform, state, obj) !=
			MuiCollectionClass.Listview || !platform.IsMapped(storage, 12))
			return false;
		var child = ChildList(ref platform, state, obj);
		var values = child.IsNull ? default
			: MuiAreaLayoutCore.ComputeMinMax(ref platform, state, child);
		if (PolicyValue(ref platform, state, obj, ScrollerPos,
			ScrollerPosDefault) !=
			ScrollerPosNone)
		{
			values.MinWidth = Grow(values.MinWidth, ScrollerWidth);
			values.MaxWidth = Grow(values.MaxWidth, ScrollerWidth);
			values.DefWidth = Grow(values.DefWidth, ScrollerWidth);
		}
		if (child.IsNotNull && MuiListCore.TryGetHScrollerState(ref platform,
			state, child, out var hState) && hState.Visible != 0)
		{
			values.MinHeight = Grow(values.MinHeight, HScrollerHeight);
			values.MaxHeight = Grow(values.MaxHeight, HScrollerHeight);
			values.DefHeight = Grow(values.DefHeight, HScrollerHeight);
		}
		return MuiAreaLayoutCore.WriteMinMax(ref platform, storage, values);
	}

	private static short Grow(short value, uint addition)
	{
		var result = (int)value + (int)addition;
		return unchecked((short)(result > 10000 ? 10000 : result));
	}

	// ---- Small helpers --------------------------------------------------------

	private static bool TryReadRenderState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListviewRenderState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadRenderStateAdmission(ref platform, state, obj, out value,
			out var present)) return false;
		return present;
	}

	// A published render context is authoritative guest state. A non-NULL
	// record that fails the cookie/field contract is malformed, not absence; a
	// layout or draw pass must not replace it from the raw RenderInfo alias.
	private static bool TryReadRenderStateAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewRenderState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			RenderStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		if (!MuiListviewRenderStateCodec.TryRead(ref platform, block,
			out value) || !IsCoherentRenderState(ref platform, value))
		{
			value = default;
			return false;
		}
		return true;
	}

	// RenderInfo is the authoritative graphics record for the decoded RastPort.
	// A published null pair is the valid pre-setup state; once RenderInfo is
	// present, the named RastPort projection must agree with it before draw or
	// child binding can consume the context.
	private static bool IsCoherentRenderState<TPlatform>(
		ref TPlatform platform, MuiListviewRenderState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (value.RenderInfo.IsNull) return value.RastPort.IsNull;
		return MuiDrawingRenderInfoCodec.TryRead(ref platform, value.RenderInfo,
			out var renderInfo) && renderInfo.RastPort.Raw == value.RastPort.Raw;
	}

	private static bool TryReadScrollerState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListviewScrollerState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadScrollerStateAdmission(ref platform, state, obj,
			out value, out var present)) return false;
		return present;
	}

	// A published viewport projection is authoritative guest state. A non-NULL
	// record that fails the cookie/field contract is malformed, not absence; a
	// recompute must not free it and silently publish a replacement record.
	private static bool TryReadScrollerStateAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewScrollerState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			ScrollerStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		if (!MuiListviewScrollerStateCodec.TryRead(ref platform, block,
			out value) || !IsValidScrollerState(value))
		{
			value = default;
			return false;
		}
		return true;
	}

	// The scroller projection is a bounded range, not an arbitrary snapshot.
	// Keep the hidden-state sentinels distinct from a visible viewport and reject
	// impossible cursor ranges before geometry or input can turn them into pixel
	// coordinates. The named record remains the only source for steady-state
	// consumers; raw attributes are used only while the record is absent.
	private static bool IsValidScrollerState(MuiListviewScrollerState value)
	{
		if (value.Visible == MuiListCore.VisibleOff)
			return value.First == MuiListCore.VisibleOff && value.MaxFirst == 0;
		return value.First <= value.MaxFirst && value.MaxFirst <= value.Entries;
	}

	private static bool TryReadHorizontalScrollerState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewHorizontalScrollerState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadHorizontalScrollerStateAdmission(ref platform, state, obj,
			out value, out var present)) return false;
		return present;
	}

	// A published horizontal viewport projection is authoritative guest state.
	// A non-NULL record that fails its cookie/field contract is malformed, not
	// absence; a geometry pass must not free it and publish a replacement.
	private static bool TryReadHorizontalScrollerStateAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewHorizontalScrollerState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			HorizontalScrollerStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		if (!MuiListviewHorizontalScrollerStateCodec.TryRead(ref platform,
			block, out value) || !IsValidHorizontalScrollerState(value))
		{
			value = default;
			return false;
		}
		return true;
	}

	// The horizontal projection is a bounded geometry/range record. Reject
	// inverted or out-of-track rectangles and impossible child-scroll values
	// before draw or pointer input turns them into coordinates. Content smaller
	// than the viewport is legal (Always visibility), so its maximum remains
	// zero rather than being treated as an error.
	private static bool IsValidHorizontalScrollerState(
		MuiListviewHorizontalScrollerState value)
	{
		if (value.TrackLeft > value.TrackRight ||
			value.TrackTop > value.TrackBottom ||
			value.ThumbLeft > value.ThumbRight ||
			value.ThumbTop > value.ThumbBottom ||
			value.ThumbLeft < value.TrackLeft ||
			value.ThumbRight > value.TrackRight ||
			value.ThumbTop < value.TrackTop ||
			value.ThumbBottom > value.TrackBottom ||
			value.ScrollX > value.MaxScrollX)
			return false;
		var expectedMax = value.ContentWidth > value.ViewWidth
			? value.ContentWidth - value.ViewWidth : 0u;
		return value.MaxScrollX == expectedMax;
	}

	private static void CopyHorizontalScrollerState(
		MuiListviewHorizontalScrollerState value,
		out MuiListviewHorizontalScrollerGeometry geometry)
	{
		geometry = default;
		geometry.TrackLeft = value.TrackLeft;
		geometry.TrackTop = value.TrackTop;
		geometry.TrackRight = value.TrackRight;
		geometry.TrackBottom = value.TrackBottom;
		geometry.ThumbLeft = value.ThumbLeft;
		geometry.ThumbTop = value.ThumbTop;
		geometry.ThumbRight = value.ThumbRight;
		geometry.ThumbBottom = value.ThumbBottom;
		geometry.ContentWidth = value.ContentWidth;
		geometry.ViewWidth = value.ViewWidth;
		geometry.ScrollX = value.ScrollX;
		geometry.MaxScrollX = value.MaxScrollX;
	}

	private static bool PublishHorizontalScrollerState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiListviewHorizontalScrollerGeometry geometry,
		out MuiListviewHorizontalScrollerGeometry published)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		published = default;
		var value = default(MuiListviewHorizontalScrollerState);
		value.Magic = MuiListviewHorizontalScrollerState.Cookie;
		value.TrackLeft = geometry.TrackLeft;
		value.TrackTop = geometry.TrackTop;
		value.TrackRight = geometry.TrackRight;
		value.TrackBottom = geometry.TrackBottom;
		value.ThumbLeft = geometry.ThumbLeft;
		value.ThumbTop = geometry.ThumbTop;
		value.ThumbRight = geometry.ThumbRight;
		value.ThumbBottom = geometry.ThumbBottom;
		value.ContentWidth = geometry.ContentWidth;
		value.ViewWidth = geometry.ViewWidth;
		value.ScrollX = geometry.ScrollX;
		value.MaxScrollX = geometry.MaxScrollX;
		if (!TryReadHorizontalScrollerStateAdmission(ref platform, state, obj,
			out _, out var present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			HorizontalScrollerStateKey, 0));
		if (present)
		{
			if (!MuiListviewHorizontalScrollerStateCodec.Write(ref platform, block,
				value)) return false;
		}
		else
		{
			block = MuiHeadlessMemory.Allocate(ref platform,
				MuiListviewHorizontalScrollerState.Size);
			if (block.IsNull) return false;
			platform.Clear(block, MuiListviewHorizontalScrollerState.Size);
			var written = MuiListviewHorizontalScrollerStateCodec.Write(
				ref platform, block, value);
			var stored = written && SetInternal(ref platform, state, obj,
				HorizontalScrollerStateKey, block.Raw);
			if (!stored)
			{
				platform.Clear(block, MuiListviewHorizontalScrollerState.Size);
				platform.Free(block, MuiListviewHorizontalScrollerState.Size);
				return false;
			}
		}
		if (!TryReadHorizontalScrollerState(ref platform, state, obj,
			out var canonical)) return false;
		CopyHorizontalScrollerState(canonical, out published);
		return true;
	}

	private static void ReleaseHorizontalScrollerState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		// A hidden child projection normally retires the composite geometry, but
		// a malformed present record is still guest-owned state. Admit it before
		// teardown so a visibility transition cannot silently free the record and
		// make the next pass publish a replacement behind the caller's back.
		if (!TryReadHorizontalScrollerStateAdmission(ref platform, state, obj,
			out _, out var present) || !present) return;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			HorizontalScrollerStateKey, 0));
		if (block.IsNotNull && platform.IsMapped(block,
			MuiListviewHorizontalScrollerState.Size))
		{
			platform.Clear(block, MuiListviewHorizontalScrollerState.Size);
			platform.Free(block, MuiListviewHorizontalScrollerState.Size);
		}
		SetInternal(ref platform, state, obj, HorizontalScrollerStateKey, 0);
	}

	internal static bool TryGetHorizontalScrollerState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewHorizontalScrollerState value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadHorizontalScrollerState(ref platform, state, obj, out value);

	private static bool PublishScrollerState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint entries, uint visible, uint first,
		uint maxFirst, out uint publishedEntries, out uint publishedVisible,
		out uint publishedFirst, out uint publishedMaxFirst)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		publishedEntries = 0;
		publishedVisible = 0;
		publishedFirst = 0;
		publishedMaxFirst = 0;
		var value = default(MuiListviewScrollerState);
		value.Magic = MuiListviewScrollerState.Cookie;
		value.Entries = entries;
		value.Visible = visible;
		value.First = first;
		value.MaxFirst = maxFirst;
		if (!TryReadScrollerStateAdmission(ref platform, state, obj,
			out _, out var present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ScrollerStateKey, 0));
		if (present)
		{
			if (!MuiListviewScrollerStateCodec.Write(ref platform, block, value))
				return false;
		}
		else
		{
			block = MuiHeadlessMemory.Allocate(ref platform,
				MuiListviewScrollerState.Size);
			if (block.IsNull) return false;
			platform.Clear(block, MuiListviewScrollerState.Size);
			var written = MuiListviewScrollerStateCodec.Write(ref platform, block,
				value);
			var stored = written && SetInternal(ref platform, state, obj,
				ScrollerStateKey, block.Raw);
			if (!stored)
			{
				platform.Clear(block, MuiListviewScrollerState.Size);
				platform.Free(block, MuiListviewScrollerState.Size);
				return false;
			}
		}
		if (!TryReadScrollerState(ref platform, state, obj, out var published))
			return false;
		publishedEntries = published.Entries;
		publishedVisible = published.Visible;
		publishedFirst = published.First;
		publishedMaxFirst = published.MaxFirst;
		return true;
	}

	internal static bool TryGetScrollerState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListviewScrollerState value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadScrollerState(ref platform, state, obj, out value);

	private static bool PublishRenderState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var value = default(MuiListviewRenderState);
		value.Magic = MuiListviewRenderState.Cookie;
		value.RenderInfo = APTR.FromPointer(Read(ref platform, state, obj,
			RenderInfo, 0));
		if (MuiDrawingRenderInfoCodec.TryRead(ref platform, value.RenderInfo,
			out var info)) value.RastPort = info.RastPort;
		if (!TryReadRenderStateAdmission(ref platform, state, obj, out _,
			out var present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			RenderStateKey, 0));
		if (present)
			return MuiListviewRenderStateCodec.Write(ref platform, block, value);
		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListviewRenderState.Size);
		if (block.IsNull) return false;
		platform.Clear(block, MuiListviewRenderState.Size);
		var written = MuiListviewRenderStateCodec.Write(ref platform, block, value);
		var stored = written && SetInternal(ref platform, state, obj,
			RenderStateKey, block.Raw);
		if (!stored)
		{
			platform.Clear(block, MuiListviewRenderState.Size);
			platform.Free(block, MuiListviewRenderState.Size);
		}
		return stored;
	}

	private static bool TryGetRenderContext<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListviewRenderState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadRenderStateAdmission(ref platform, state, obj, out value,
			out var present)) return false;
		if (present)
		{
			// Once the named render projection exists, even its valid pre-setup
			// null pair is authoritative. Do not revive a raw Area alias here:
			// draw consumers must either receive the admitted pair or no context.
			if (value.RenderInfo.IsNull || value.RastPort.IsNull) return false;
			if (!MuiDrawingRenderInfoCodec.TryRead(ref platform, value.RenderInfo,
				out var info) || info.RastPort.IsNull || info.RastPort.Raw !=
				value.RastPort.Raw) return false;
			return true;
		}
		// Before the first publication, the Area RenderInfo attribute is the
		// only available source. Decode it once into the named record shape;
		// steady-state consumers never use this fallback after publication.
		value = default;
		value.RenderInfo = APTR.FromPointer(Read(ref platform, state, obj,
			RenderInfo, 0));
		if (value.RenderInfo.IsNull) return false;
		if (!MuiDrawingRenderInfoCodec.TryRead(ref platform, value.RenderInfo,
			out var fallbackInfo)) return false;
		value.RastPort = fallbackInfo.RastPort;
		return !value.RastPort.IsNull;
	}

	internal static bool TryGetRenderState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListviewRenderState value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadRenderState(ref platform, state, obj, out value);

	private static bool TryReadLayoutState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListviewLayoutState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadLayoutStateAdmission(ref platform, state, obj, out value,
			out var present)) return false;
		return present;
	}

	// A published layout record is authoritative guest state. A non-NULL record
	// that fails the cookie/field contract is malformed, not absence; geometry
	// consumers must not bypass it through the raw Area record.
	private static bool TryReadLayoutStateAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewLayoutState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			LayoutStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return MuiListviewLayoutStateCodec.TryRead(ref platform, block,
			out value) && IsValidLayoutState(value);
	}

	// Layout receives signed coordinates, but dimensions are non-negative in
	// the MUI layout contract. Keep the named row authoritative while rejecting
	// impossible parent/child extents before geometry or pointer consumers turn
	// them into coordinates.
	private static bool IsValidLayoutState(MuiListviewLayoutState value) =>
		value.Width >= 0 && value.Height >= 0 &&
		value.ChildWidth >= 0 && value.ChildHeight >= 0;

	private static bool PublishLayoutState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR child)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var value = default(MuiListviewLayoutState);
		value.Magic = MuiListviewLayoutState.Cookie;
	if (!MuiAreaLayoutCore.TryReadGeometryState(ref platform, state, obj,
		out var geometry)) return false;
	value.Left = geometry.Left;
	value.Top = geometry.Top;
	value.Width = geometry.Width;
	value.Height = geometry.Height;
	if (child.IsNotNull)
	{
		if (!MuiAreaLayoutCore.TryReadGeometryState(ref platform, state, child,
			out var childGeometry)) return false;
		value.ChildLeft = childGeometry.Left;
		value.ChildTop = childGeometry.Top;
		value.ChildWidth = childGeometry.Width;
	value.ChildHeight = childGeometry.Height;
	}
		if (!TryReadLayoutStateAdmission(ref platform, state, obj, out _,
			out var present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			LayoutStateKey, 0));
		if (present)
			return MuiListviewLayoutStateCodec.Write(ref platform, block, value);
		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListviewLayoutState.Size);
		if (block.IsNull) return false;
		platform.Clear(block, MuiListviewLayoutState.Size);
		var written = MuiListviewLayoutStateCodec.Write(ref platform, block, value);
		var stored = written && SetInternal(ref platform, state, obj,
			LayoutStateKey, block.Raw);
		if (!stored)
		{
			platform.Clear(block, MuiListviewLayoutState.Size);
			platform.Free(block, MuiListviewLayoutState.Size);
		}
		return stored;
	}

	internal static bool TryGetLayoutState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListviewLayoutState value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadLayoutState(ref platform, state, obj, out value);

	private static uint Read<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint fallback)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		// The public getter intentionally rejects Listview [I..] policy fields,
		// but construction and input code still need their raw initialization
		// values. Keep that internal bootstrap read explicit and struct-safe.
		if (IsInitializeOnlyAttribute(attribute) &&
			MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out var raw)) return raw;
		return MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			attribute, out var value) ? value : fallback;
	}

	private static bool BindChildRenderInfo<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR child)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadRenderStateAdmission(ref platform, state, obj,
			out var renderState, out var present)) return false;
		// Once the parent has published its named render projection, its
		// complete record is authoritative for child binding too. In particular,
		// a valid null pair means the parent is not renderable yet; do not revive
		// a raw Area alias and hand stale context to the child.
		var renderInfo = present
			? renderState.RenderInfo
			: APTR.FromPointer(Read(ref platform, state, obj, RenderInfo, 0));
		if (renderInfo.IsNull) return true;
		return MuiAreaLayoutCore.Setup(ref platform, state, child, renderInfo);
	}

	private static bool SetInternal<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			value, false);

	private static bool EnsureDefault<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (IsInitializeOnlyAttribute(attribute) &&
			MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _)) return true;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out _))
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, value, false);
		return true;
	}
}
