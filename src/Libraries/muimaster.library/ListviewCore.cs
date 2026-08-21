/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

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

	internal static class MuiListviewChildStateFieldCursorCodec
	{
		private static bool TryResolve(MuiListviewChildStateField field,
			out uint offset)
		{
			offset = field switch
			{
				MuiListviewChildStateField.Magic => 0,
				MuiListviewChildStateField.Child => 4,
				_ => uint.MaxValue,
			};
			return offset != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListviewChildStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
				cursor.Record.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Record, MuiListviewChildState.Size))
				return false;
			address = APTR.FromPointer(cursor.Record.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewChildStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListviewChildStateFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
			value = platform.ReadUInt32(address, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewChildStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListviewChildStateFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
			platform.WriteUInt32(address, 0, value);
			return true;
		}
	}

	internal static class MuiListviewChildStateCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListviewChildState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewChildState.Size) ||
				!MuiListviewChildStateFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListviewChildStateField.Magic, out var magic) ||
				magic != MuiListviewChildState.Cookie ||
				!MuiListviewChildStateFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListviewChildStateField.Child, out var child))
				return false;
			value.Magic = magic;
			value.Child = APTR.FromPointer(child);
			return true;
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListviewChildState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewChildState.Size) ||
				value.Magic != MuiListviewChildState.Cookie) return false;
			return MuiListviewChildStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiListviewChildStateField.Magic,
				value.Magic) &&
				MuiListviewChildStateFieldCursorCodec.TryWriteUInt32(ref platform,
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

	internal static class MuiListviewClickStateFieldCursorCodec
	{
		private static bool TryResolve(MuiListviewClickStateField field,
			out uint offset)
		{
			offset = field switch
			{
				MuiListviewClickStateField.Magic => 0,
				MuiListviewClickStateField.ClickColumn => 4,
				MuiListviewClickStateField.DoubleClick => 8,
				MuiListviewClickStateField.AgainClick => 12,
				MuiListviewClickStateField.Clicks => 16,
				MuiListviewClickStateField.DefClickColumn => 20,
				_ => uint.MaxValue,
			};
			return offset != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListviewClickStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
				cursor.Record.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Record, MuiListviewClickState.Size))
				return false;
			address = APTR.FromPointer(cursor.Record.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewClickStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListviewClickStateFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
			value = platform.ReadUInt32(address, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewClickStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListviewClickStateFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
			platform.WriteUInt32(address, 0, value);
			return true;
		}
	}

	internal static class MuiListviewClickStateCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListviewClickState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewClickState.Size) ||
				!MuiListviewClickStateFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListviewClickStateField.Magic, out var magic) ||
				magic != MuiListviewClickState.Cookie)
				return false;
			value.Magic = MuiListviewClickState.Cookie;
			if (!MuiListviewClickStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListviewClickStateField.ClickColumn,
				out value.ClickColumn) ||
				!MuiListviewClickStateFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListviewClickStateField.DoubleClick,
					out var doubleClick) ||
				!MuiListviewClickStateFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListviewClickStateField.AgainClick,
					out var againClick) ||
				!MuiListviewClickStateFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListviewClickStateField.Clicks, out value.Clicks) ||
				!MuiListviewClickStateFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListviewClickStateField.DefClickColumn,
					out value.DefClickColumn))
				return false;
			value.DoubleClick = doubleClick == 0 ? 0u : 1u;
			value.AgainClick = againClick == 0 ? 0u : 1u;
			return true;
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListviewClickState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewClickState.Size) ||
				value.Magic != MuiListviewClickState.Cookie) return false;
			return MuiListviewClickStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiListviewClickStateField.Magic, value.Magic) &&
				MuiListviewClickStateFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListviewClickStateField.ClickColumn,
					value.ClickColumn) &&
				MuiListviewClickStateFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListviewClickStateField.DoubleClick,
					value.DoubleClick == 0 ? 0u : 1u) &&
				MuiListviewClickStateFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListviewClickStateField.AgainClick,
					value.AgainClick == 0 ? 0u : 1u) &&
				MuiListviewClickStateFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListviewClickStateField.Clicks, value.Clicks) &&
				MuiListviewClickStateFieldCursorCodec.TryWriteUInt32(ref platform,
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

	internal static class MuiListviewInteractionPolicyFieldCursorCodec
	{
		private static bool TryResolve(
			MuiListviewInteractionPolicyField field, out uint offset)
		{
			offset = field switch
			{
				MuiListviewInteractionPolicyField.Magic => 0,
				MuiListviewInteractionPolicyField.Input => 4,
				MuiListviewInteractionPolicyField.MultiSelect => 8,
				MuiListviewInteractionPolicyField.ScrollerPos => 12,
				MuiListviewInteractionPolicyField.DragType => 16,
				_ => uint.MaxValue,
			};
			return offset != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListviewInteractionPolicyFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
				cursor.Record.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Record,
					MuiListviewInteractionPolicyState.Size)) return false;
			address = APTR.FromPointer(cursor.Record.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewInteractionPolicyField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListviewInteractionPolicyFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
			value = platform.ReadUInt32(address, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewInteractionPolicyField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListviewInteractionPolicyFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			return TryGetAddress(ref platform, cursor, out var address) &&
				Write(ref platform, address, value);
		}

		private static bool Write<TPlatform>(ref TPlatform platform, APTR address,
			uint value) where TPlatform : struct, IMuiGuestMemory
		{
			platform.WriteUInt32(address, 0, value);
			return true;
		}
	}

	internal static class MuiListviewInteractionPolicyStateCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
			out MuiListviewInteractionPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewInteractionPolicyState.Size) ||
				!MuiListviewInteractionPolicyFieldCursorCodec.TryReadUInt32(
					ref platform, address,
					MuiListviewInteractionPolicyField.Magic, out var magic) ||
				magic != MuiListviewInteractionPolicyState.Cookie)
				return false;
			value.Magic = MuiListviewInteractionPolicyState.Cookie;
			return MuiListviewInteractionPolicyFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiListviewInteractionPolicyField.Input, out value.Input) &&
				MuiListviewInteractionPolicyFieldCursorCodec.TryReadUInt32(
					ref platform, address,
					MuiListviewInteractionPolicyField.MultiSelect,
					out value.MultiSelect) &&
				MuiListviewInteractionPolicyFieldCursorCodec.TryReadUInt32(
					ref platform, address,
					MuiListviewInteractionPolicyField.ScrollerPos,
					out value.ScrollerPos) &&
				MuiListviewInteractionPolicyFieldCursorCodec.TryReadUInt32(
					ref platform, address,
					MuiListviewInteractionPolicyField.DragType,
					out value.DragType);
		}

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
			MuiListviewInteractionPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewInteractionPolicyState.Size) ||
				value.Magic != MuiListviewInteractionPolicyState.Cookie)
				return false;
			return MuiListviewInteractionPolicyFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiListviewInteractionPolicyField.Magic, value.Magic) &&
				MuiListviewInteractionPolicyFieldCursorCodec.TryWriteUInt32(
					ref platform, address,
					MuiListviewInteractionPolicyField.Input, value.Input) &&
				MuiListviewInteractionPolicyFieldCursorCodec.TryWriteUInt32(
					ref platform, address,
					MuiListviewInteractionPolicyField.MultiSelect,
					value.MultiSelect) &&
				MuiListviewInteractionPolicyFieldCursorCodec.TryWriteUInt32(
					ref platform, address,
					MuiListviewInteractionPolicyField.ScrollerPos,
					value.ScrollerPos) &&
				MuiListviewInteractionPolicyFieldCursorCodec.TryWriteUInt32(
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

	internal static class MuiListviewSelectionSignalFieldCursorCodec
	{
		private static bool TryResolve(MuiListviewSelectionSignalField field,
			out uint offset)
		{
			offset = field switch
			{
				MuiListviewSelectionSignalField.Magic => 0,
				MuiListviewSelectionSignalField.Value => 4,
				_ => uint.MaxValue,
			};
			return offset != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListviewSelectionSignalFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
				cursor.Record.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Record,
					MuiListviewSelectionSignalState.Size)) return false;
			address = APTR.FromPointer(cursor.Record.Raw +
				offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewSelectionSignalField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListviewSelectionSignalFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
			value = platform.ReadUInt32(address, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewSelectionSignalField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListviewSelectionSignalFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
			platform.WriteUInt32(address, 0, value);
			return true;
		}
	}

	internal static class MuiListviewSelectionSignalStateCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
			out MuiListviewSelectionSignalState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewSelectionSignalState.Size) ||
				!MuiListviewSelectionSignalFieldCursorCodec.TryReadUInt32(
					ref platform, address,
					MuiListviewSelectionSignalField.Magic, out var magic) ||
				magic != MuiListviewSelectionSignalState.Cookie)
				return false;
			value.Magic = MuiListviewSelectionSignalState.Cookie;
			return MuiListviewSelectionSignalFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiListviewSelectionSignalField.Value, out value.Value);
		}

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
			MuiListviewSelectionSignalState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewSelectionSignalState.Size) ||
				value.Magic != MuiListviewSelectionSignalState.Cookie)
				return false;
			return MuiListviewSelectionSignalFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiListviewSelectionSignalField.Magic, value.Magic) &&
				MuiListviewSelectionSignalFieldCursorCodec.TryWriteUInt32(
					ref platform, address,
					MuiListviewSelectionSignalField.Value,
					value.Value == 0 ? 0u : 1u);
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

	internal static class MuiListviewLayoutFieldCursorCodec
	{
		private static bool TryResolve(MuiListviewLayoutField field,
			out uint offset)
		{
			offset = field switch
			{
				MuiListviewLayoutField.Magic => 0,
				MuiListviewLayoutField.Left => 4,
				MuiListviewLayoutField.Top => 8,
				MuiListviewLayoutField.Width => 12,
				MuiListviewLayoutField.Height => 16,
				MuiListviewLayoutField.ChildLeft => 20,
				MuiListviewLayoutField.ChildTop => 24,
				MuiListviewLayoutField.ChildWidth => 28,
				MuiListviewLayoutField.ChildHeight => 32,
				_ => uint.MaxValue,
			};
			return offset != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListviewLayoutFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
				cursor.Record.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Record, MuiListviewLayoutState.Size))
				return false;
			address = APTR.FromPointer(cursor.Record.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewLayoutField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListviewLayoutFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
			value = platform.ReadUInt32(address, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewLayoutField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListviewLayoutFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
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

	internal static class MuiListviewLayoutStateCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListviewLayoutState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewLayoutState.Size) ||
				!MuiListviewLayoutFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListviewLayoutField.Magic, out var magic) ||
				magic != MuiListviewLayoutState.Cookie)
				return false;
			value.Magic = magic;
			return MuiListviewLayoutFieldCursorCodec.TryReadInt32(ref platform,
				address, MuiListviewLayoutField.Left, out value.Left) &&
				MuiListviewLayoutFieldCursorCodec.TryReadInt32(ref platform,
					address, MuiListviewLayoutField.Top, out value.Top) &&
				MuiListviewLayoutFieldCursorCodec.TryReadInt32(ref platform,
					address, MuiListviewLayoutField.Width, out value.Width) &&
				MuiListviewLayoutFieldCursorCodec.TryReadInt32(ref platform,
					address, MuiListviewLayoutField.Height, out value.Height) &&
				MuiListviewLayoutFieldCursorCodec.TryReadInt32(ref platform,
					address, MuiListviewLayoutField.ChildLeft,
					out value.ChildLeft) &&
				MuiListviewLayoutFieldCursorCodec.TryReadInt32(ref platform,
					address, MuiListviewLayoutField.ChildTop,
					out value.ChildTop) &&
				MuiListviewLayoutFieldCursorCodec.TryReadInt32(ref platform,
					address, MuiListviewLayoutField.ChildWidth,
					out value.ChildWidth) &&
				MuiListviewLayoutFieldCursorCodec.TryReadInt32(ref platform,
					address, MuiListviewLayoutField.ChildHeight,
					out value.ChildHeight);
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListviewLayoutState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewLayoutState.Size) || value.Magic !=
				MuiListviewLayoutState.Cookie) return false;
			return MuiListviewLayoutFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListviewLayoutField.Magic, value.Magic) &&
				MuiListviewLayoutFieldCursorCodec.TryWriteInt32(ref platform,
					address, MuiListviewLayoutField.Left, value.Left) &&
				MuiListviewLayoutFieldCursorCodec.TryWriteInt32(ref platform,
					address, MuiListviewLayoutField.Top, value.Top) &&
				MuiListviewLayoutFieldCursorCodec.TryWriteInt32(ref platform,
					address, MuiListviewLayoutField.Width, value.Width) &&
				MuiListviewLayoutFieldCursorCodec.TryWriteInt32(ref platform,
					address, MuiListviewLayoutField.Height, value.Height) &&
				MuiListviewLayoutFieldCursorCodec.TryWriteInt32(ref platform,
					address, MuiListviewLayoutField.ChildLeft, value.ChildLeft) &&
				MuiListviewLayoutFieldCursorCodec.TryWriteInt32(ref platform,
					address, MuiListviewLayoutField.ChildTop, value.ChildTop) &&
				MuiListviewLayoutFieldCursorCodec.TryWriteInt32(ref platform,
					address, MuiListviewLayoutField.ChildWidth, value.ChildWidth) &&
				MuiListviewLayoutFieldCursorCodec.TryWriteInt32(ref platform,
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

	internal static class MuiListviewRenderFieldCursorCodec
	{
		private static bool TryResolve(MuiListviewRenderField field,
			out uint offset)
		{
			offset = field switch
			{
				MuiListviewRenderField.Magic => 0,
				MuiListviewRenderField.RenderInfo => 4,
				MuiListviewRenderField.RastPort => 8,
				_ => uint.MaxValue,
			};
			return offset != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListviewRenderFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
				cursor.Record.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Record, MuiListviewRenderState.Size))
				return false;
			address = APTR.FromPointer(cursor.Record.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewRenderField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListviewRenderFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
			value = platform.ReadUInt32(address, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewRenderField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListviewRenderFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
			platform.WriteUInt32(address, 0, value);
			return true;
		}
	}

	internal static class MuiListviewRenderStateCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListviewRenderState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewRenderState.Size) ||
				!MuiListviewRenderFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListviewRenderField.Magic, out var magic) ||
				magic != MuiListviewRenderState.Cookie ||
				!MuiListviewRenderFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListviewRenderField.RenderInfo, out var info) ||
				!MuiListviewRenderFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListviewRenderField.RastPort, out var rastPort))
				return false;
			value.Magic = magic;
			value.RenderInfo = APTR.FromPointer(info);
			value.RastPort = APTR.FromPointer(rastPort);
			return true;
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListviewRenderState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewRenderState.Size) || value.Magic !=
				MuiListviewRenderState.Cookie) return false;
			return MuiListviewRenderFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListviewRenderField.Magic, value.Magic) &&
				MuiListviewRenderFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListviewRenderField.RenderInfo,
					value.RenderInfo.Raw) &&
				MuiListviewRenderFieldCursorCodec.TryWriteUInt32(ref platform,
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

		internal uint Magic;
		internal uint Entries;
		internal uint Visible;
		internal uint First;
		internal uint MaxFirst;
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

	internal static class MuiListviewScrollerFieldCursorCodec
	{
		private static bool TryResolve(MuiListviewScrollerField field,
			out uint offset)
		{
			offset = field switch
			{
				MuiListviewScrollerField.Magic => 0,
				MuiListviewScrollerField.Entries => 4,
				MuiListviewScrollerField.Visible => 8,
				MuiListviewScrollerField.First => 12,
				MuiListviewScrollerField.MaxFirst => 16,
				_ => uint.MaxValue,
			};
			return offset != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListviewScrollerFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
				cursor.Record.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Record, MuiListviewScrollerState.Size))
				return false;
			address = APTR.FromPointer(cursor.Record.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewScrollerField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListviewScrollerFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
			value = platform.ReadUInt32(address, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListviewScrollerField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListviewScrollerFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
			platform.WriteUInt32(address, 0, value);
			return true;
		}
	}

	internal static class MuiListviewScrollerStateCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListviewScrollerState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewScrollerState.Size) ||
				!MuiListviewScrollerFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListviewScrollerField.Magic, out var magic) ||
				magic != MuiListviewScrollerState.Cookie)
				return false;
			value.Magic = magic;
			return MuiListviewScrollerFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListviewScrollerField.Entries, out value.Entries) &&
				MuiListviewScrollerFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListviewScrollerField.Visible, out value.Visible) &&
				MuiListviewScrollerFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListviewScrollerField.First, out value.First) &&
				MuiListviewScrollerFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListviewScrollerField.MaxFirst, out value.MaxFirst);
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListviewScrollerState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListviewScrollerState.Size) || value.Magic !=
				MuiListviewScrollerState.Cookie) return false;
			return MuiListviewScrollerFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListviewScrollerField.Magic, value.Magic) &&
				MuiListviewScrollerFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListviewScrollerField.Entries, value.Entries) &&
				MuiListviewScrollerFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListviewScrollerField.Visible, value.Visible) &&
				MuiListviewScrollerFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListviewScrollerField.First, value.First) &&
				MuiListviewScrollerFieldCursorCodec.TryWriteUInt32(ref platform,
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
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ChildStateKey, 0));
		if (MuiListviewChildStateCodec.TryRead(ref platform, block,
			out var value))
		{
			value.Child = child;
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
		if (block.IsNotNull && platform.IsMapped(block,
			MuiListviewChildState.Size))
		{
			MuiListviewChildStateCodec.Clear(ref platform, block);
			platform.Free(block, MuiListviewChildState.Size);
		}
		return true;
	}

	internal static bool TryGetChildState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListviewChildState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ChildStateKey, 0));
		return MuiListviewChildStateCodec.TryRead(ref platform, block, out value);
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

	private static bool EnsureInteractionPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			InteractionPolicyKey, 0));
		var value = default(MuiListviewInteractionPolicyState);
		value.Magic = MuiListviewInteractionPolicyState.Cookie;
		value.Input = NormalizePolicy(Input,
			Read(ref platform, state, obj, Input, 1));
		value.MultiSelect = NormalizePolicy(MultiSelect,
			Read(ref platform, state, obj, MultiSelect, MultiSelectDefault));
		value.ScrollerPos = NormalizePolicy(ScrollerPos,
			Read(ref platform, state, obj, ScrollerPos, ScrollerPosDefault));
		value.DragType = NormalizePolicy(DragType,
			Read(ref platform, state, obj, DragType, DragTypeNone));

		if (!MuiListviewInteractionPolicyStateCodec.TryRead(ref platform, block,
			out _))
		{
			// Allocate and validate the replacement before retiring an invalid
			// record, so a failure never leaves the object with a dangling policy
			// pointer or a partially updated public state.
			var stale = block;
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
			if (stale.IsNotNull && platform.IsMapped(stale,
				MuiListviewInteractionPolicyState.Size))
			{
				platform.Clear(stale, MuiListviewInteractionPolicyState.Size);
				platform.Free(stale, MuiListviewInteractionPolicyState.Size);
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
		value = default;
		// This is an internal state-key lookup. Bypass the public getter router
		// so the HeadlessObjectCore -> ListviewCore policy projection cannot
		// recurse while resolving its own record address.
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			InteractionPolicyKey, out var rawBlock)) return false;
		var block = APTR.FromPointer(rawBlock);
		return MuiListviewInteractionPolicyStateCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureSelectionSignal<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			SelectionSignalKey, 0));
		if (!MuiListviewSelectionSignalStateCodec.TryRead(ref platform, block,
			out var value))
		{
			// Publish a fully initialized replacement before retiring a stale or
			// malformed record; a failed allocation must preserve the old pointer.
			var stale = block;
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
			if (stale.IsNotNull && platform.IsMapped(stale,
				MuiListviewSelectionSignalState.Size))
			{
				platform.Clear(stale, MuiListviewSelectionSignalState.Size);
				platform.Free(stale, MuiListviewSelectionSignalState.Size);
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
		value = default;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			SelectionSignalKey, 0));
		return MuiListviewSelectionSignalStateCodec.TryRead(ref platform, block,
			out value);
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
		if (TryGetInteractionPolicy(ref platform, state, obj, out var value))
		{
			if (attribute == Input) return value.Input;
			if (attribute == MultiSelect) return value.MultiSelect;
			if (attribute == ScrollerPos) return value.ScrollerPos;
			if (attribute == DragType) return value.DragType;
		}
		return Read(ref platform, state, obj, attribute, fallback);
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

	private static bool EnsureClickState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ClickStateKey, 0));
		if (TryReadClickState(ref platform, block, out _)) return true;
		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListviewClickState.Size);
		if (block.IsNull) return false;
		var value = default(MuiListviewClickState);
		value.Magic = MuiListviewClickState.Cookie;
		value.ClickColumn = Read(ref platform, state, obj, ClickColumn, 0);
		value.DoubleClick = Read(ref platform, state, obj, DoubleClick, 0) == 0
			? 0u : 1u;
		value.AgainClick = Read(ref platform, state, obj, AgainClick, 0) == 0
			? 0u : 1u;
		value.DefClickColumn = Read(ref platform, state, obj,
			DefClickColumn, 0);
		WriteClickState(ref platform, block, value);
		if (SetInternal(ref platform, state, obj, ClickStateKey, block.Raw))
			return true;
		FreeClickState(ref platform, block);
		return false;
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
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ClickStateKey, 0));
		return TryReadClickState(ref platform, block, out var clickState)
			? clickState.DefClickColumn
			: Read(ref platform, state, obj, DefClickColumn, 0);
	}

	// Bind the child-to-composite notification projection through a named
	// attribute record.  Keeping the link in guest object state makes ownership
	// and teardown visible to the same struct-first memory seam as other state.
	internal static bool SetListviewOwner<TPlatform>(ref TPlatform platform,
		APTR state, APTR list, APTR owner)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, list,
			MuiListCore.ListviewOwnerKey, owner.Raw, false);

	private static void WriteClickState<TPlatform>(ref TPlatform platform,
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
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ClickStateKey, 0));
		return TryReadClickState(ref platform, block, out value);
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
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			HorizontalScrollerDragStateKey, 0));
		if (MuiListviewHorizontalScrollerDragStateCodec.TryRead(ref platform,
			block, out _)) return block;
		if (block.IsNotNull && platform.IsMapped(block,
			MuiListviewHorizontalScrollerDragState.Size))
			platform.Free(block, MuiListviewHorizontalScrollerDragState.Size);
		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListviewHorizontalScrollerDragState.Size);
		if (block.IsNull) return APTR.Null;
		var value = default(MuiListviewHorizontalScrollerDragState);
		value.Magic = MuiListviewHorizontalScrollerDragState.Cookie;
		if (!MuiListviewHorizontalScrollerDragStateCodec.Write(ref platform, block,
			value))
		{
			platform.Free(block, MuiListviewHorizontalScrollerDragState.Size);
			return APTR.Null;
		}
		SetInternal(ref platform, state, obj, HorizontalScrollerDragStateKey,
			block.Raw);
		return block;
	}

	private static void ReleaseHorizontalScrollerDragState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR block)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
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
	// torn down or was never fully constructed.
	public static APTR ChildList<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		TryGetChildState(ref platform, state, obj, out var value)
			? value.Child
			: APTR.FromPointer(Read(ref platform, state, obj, List, 0));

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
			var rows = height / ListRowHeight;
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

	// Scroll the child list by viewport row, with the same saturation rule as a
	// Prop_First value. This is the narrow input seam used by the future full
	// scrollbar gadget; it keeps all ownership in the existing List object.
	public static bool SetScrollerFirst<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int requested)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!GetScrollerState(ref platform, state, obj, out _, out _, out _,
			out var maxFirst)) return false;
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
			var block = APTR.FromPointer(Read(ref platform, state, obj,
				ClickStateKey, 0));
			if (TryReadClickState(ref platform, block, out var clickState))
			{
				value = attribute == AgainClick ? clickState.AgainClick :
					attribute == DoubleClick ? clickState.DoubleClick :
					attribute == DefClickColumn ? clickState.DefClickColumn :
					clickState.ClickColumn;
				return true;
			}
		}
		if (attribute == SelectChange)
		{
			if (TryGetSelectionSignal(ref platform, state, obj, out var signal))
			{
				value = signal.Value;
				return true;
			}
			return MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
				attribute, out value);
		}
		// The four interaction-policy attributes are published in a named
		// guest-resident record. Keep that record authoritative for getters so a
		// policy read never depends on the generic attribute-list storage or on a
		// duplicated private offset path. The scalar projection remains in the
		// generic list for legacy callers, but behavior and public reads share this
		// fixed-width record.
		if (attribute == Input || attribute == MultiSelect ||
			attribute == ScrollerPos || attribute == DragType)
		{
			if (TryGetInteractionPolicy(ref platform, state, obj, out var policy))
			{
				value = attribute == Input ? policy.Input :
					attribute == MultiSelect ? policy.MultiSelect :
					attribute == ScrollerPos ? policy.ScrollerPos : policy.DragType;
				return true;
			}
			// A malformed or legacy object may not yet have the typed policy
			// block. Use the raw compatibility scalar as a terminal fallback;
			// never re-enter the public getter router from this class-gated path.
			return MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out value);
		}
		if (attribute == List && TryGetChildState(ref platform, state, obj,
			out var childState))
		{
			value = childState.Child.Raw;
			return true;
		}
		if (attribute == List || IsListviewAttribute(attribute))
			return MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
				attribute, out value);
		var child = ChildList(ref platform, state, obj);
		return child.IsNotNull && MuiHeadlessObjectCore.GetAttribute(ref platform,
			state, child, attribute, out value);
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

	internal static bool IsInteractionPolicyAttribute(uint attribute) =>
		attribute == Input || attribute == MultiSelect ||
		attribute == ScrollerPos || attribute == DragType;

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
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			DragStateKey, 0));
		if (MuiListviewDragStateCodec.TryRead(ref platform, block,
			out var drag) && (drag.Flags & MuiListviewDragState.ActiveFlag) != 0)
			return true;
		block = APTR.FromPointer(Read(ref platform, state, obj,
			ScrollerDragStateKey, 0));
		if (MuiListviewScrollerDragStateCodec.TryRead(ref platform, block,
			out var scroller) &&
			(scroller.Flags & MuiListviewScrollerDragState.ActiveFlag) != 0)
			return true;
		block = APTR.FromPointer(Read(ref platform, state, obj,
			HorizontalScrollerDragStateKey, 0));
		return MuiListviewHorizontalScrollerDragStateCodec.TryRead(ref platform,
			block, out var horizontal) &&
			(horizontal.Flags &
				MuiListviewHorizontalScrollerDragState.ActiveFlag) != 0;
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
		MuiListviewDragStateCodec.Write(ref platform, block, value);
		return true;
	}

	private static bool UpdateDrag<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR child, MuiIntuiPointerMessage pointer)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj, DragStateKey,
			0));
		if (!MuiListviewDragStateCodec.TryRead(ref platform, block,
			out var value) || (value.Flags & MuiListviewDragState.ActiveFlag) == 0)
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
		if (!MuiListviewDragStateCodec.TryRead(ref platform, block,
			out var value) || (value.Flags & MuiListviewDragState.ActiveFlag) == 0)
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
		if (MuiListviewDragStateCodec.TryRead(ref platform, block,
			out var value) && (value.Flags & MuiListviewDragState.ActiveFlag) != 0)
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
		if (MuiListviewScrollerDragStateCodec.TryRead(ref platform,
			scrollerBlock, out var scrollerValue) &&
			(scrollerValue.Flags & MuiListviewScrollerDragState.ActiveFlag) != 0)
		{
			ReleaseScrollerDragState(ref platform, state, obj, scrollerBlock);
			handled = true;
		}
		var horizontalBlock = APTR.FromPointer(Read(ref platform, state, obj,
			HorizontalScrollerDragStateKey, 0));
		if (MuiListviewHorizontalScrollerDragStateCodec.TryRead(ref platform,
			horizontalBlock, out var horizontalValue) &&
			(horizontalValue.Flags &
				MuiListviewHorizontalScrollerDragState.ActiveFlag) != 0)
		{
			ReleaseHorizontalScrollerDragState(ref platform, state, obj,
				horizontalBlock);
			handled = true;
		}
		return handled;
	}

	private static APTR EnsureDragState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj, DragStateKey,
			0));
		if (MuiListviewDragStateCodec.TryRead(ref platform, block, out _))
			return block;
		if (block.IsNotNull && platform.IsMapped(block, MuiListviewDragState.Size))
			platform.Free(block, MuiListviewDragState.Size);
		block = MuiHeadlessMemory.Allocate(ref platform, MuiListviewDragState.Size);
		if (block.IsNull) return APTR.Null;
		var value = default(MuiListviewDragState);
		value.Magic = MuiListviewDragStateCodec.Cookie;
		value.Source = -1;
		value.Target = -1;
		MuiListviewDragStateCodec.Write(ref platform, block, value);
		if (SetInternal(ref platform, state, obj, DragStateKey, block.Raw))
			return block;
		MuiListviewDragStateCodec.Clear(ref platform, block);
		platform.Free(block, MuiListviewDragState.Size);
		return APTR.Null;
	}

	private static void ReleaseDragState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR block)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
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
		var hasLayout = TryReadLayoutState(ref platform, state, obj,
			out var layout);
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
		var hasLayout = TryReadLayoutState(ref platform, state, obj,
			out var layout);
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
		if (MuiListviewScrollerDragStateCodec.Write(ref platform, block, value))
			return true;
		ReleaseScrollerDragState(ref platform, state, obj, block);
		return false;
	}

	private static bool UpdateScrollerDrag<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiIntuiPointerMessage pointer)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ScrollerDragStateKey, 0));
		if (!MuiListviewScrollerDragStateCodec.TryRead(ref platform, block,
			out var value) || (value.Flags &
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
		if (!MuiListviewScrollerDragStateCodec.TryRead(ref platform, block,
			out var value) || (value.Flags &
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
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ScrollerDragStateKey, 0));
		if (MuiListviewScrollerDragStateCodec.TryRead(ref platform, block,
			out _)) return block;
		if (block.IsNotNull && platform.IsMapped(block,
			MuiListviewScrollerDragState.Size))
			platform.Free(block, MuiListviewScrollerDragState.Size);
		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListviewScrollerDragState.Size);
		if (block.IsNull) return APTR.Null;
		var value = default(MuiListviewScrollerDragState);
		value.Magic = MuiListviewScrollerDragState.Cookie;
		if (!MuiListviewScrollerDragStateCodec.Write(ref platform, block, value) ||
			!SetInternal(ref platform, state, obj, ScrollerDragStateKey, block.Raw))
		{
			platform.Free(block, MuiListviewScrollerDragState.Size);
			return APTR.Null;
		}
		return block;
	}

	private static void ReleaseScrollerDragState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR block)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
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
		SetInternal(ref platform, state, obj, ClickColumn, column);
		SetInternal(ref platform, state, obj, AgainClick,
			againClick ? 1u : 0u);
		var parentPublished = doubleClick
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
		var visibleRows = childHeight <= 0 ? MuiListCore.VisibleOff :
			unchecked((uint)childHeight) / ListRowHeight;
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
		if (MuiListviewHorizontalScrollerDragStateCodec.Write(ref platform, block,
			value)) return true;
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
		if (!MuiListviewHorizontalScrollerDragStateCodec.TryRead(ref platform,
			block, out var value) || (value.Flags &
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
		if (!MuiListviewHorizontalScrollerDragStateCodec.TryRead(ref platform,
			block, out var value) || (value.Flags &
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
		value = default;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			RenderStateKey, 0));
		return MuiListviewRenderStateCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool TryReadScrollerState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListviewScrollerState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ScrollerStateKey, 0));
		return MuiListviewScrollerStateCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool TryReadHorizontalScrollerState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewHorizontalScrollerState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			HorizontalScrollerStateKey, 0));
		return MuiListviewHorizontalScrollerStateCodec.TryRead(ref platform,
			block, out value);
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
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			HorizontalScrollerStateKey, 0));
		if (MuiListviewHorizontalScrollerStateCodec.TryRead(ref platform, block,
			out _))
		{
			if (!MuiListviewHorizontalScrollerStateCodec.Write(ref platform, block,
				value)) return false;
		}
		else
		{
			if (block.IsNotNull && platform.IsMapped(block,
				MuiListviewHorizontalScrollerState.Size))
			{
				platform.Clear(block, MuiListviewHorizontalScrollerState.Size);
				platform.Free(block, MuiListviewHorizontalScrollerState.Size);
			}
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
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ScrollerStateKey, 0));
		if (MuiListviewScrollerStateCodec.TryRead(ref platform, block, out _))
		{
			if (!MuiListviewScrollerStateCodec.Write(ref platform, block, value))
				return false;
		}
		else
		{
			if (block.IsNotNull && platform.IsMapped(block,
				MuiListviewScrollerState.Size))
			{
				platform.Clear(block, MuiListviewScrollerState.Size);
				platform.Free(block, MuiListviewScrollerState.Size);
			}
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
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			RenderStateKey, 0));
		if (MuiListviewRenderStateCodec.TryRead(ref platform, block, out _))
			return MuiListviewRenderStateCodec.Write(ref platform, block, value);
		if (block.IsNotNull && platform.IsMapped(block,
			MuiListviewRenderState.Size))
		{
			platform.Clear(block, MuiListviewRenderState.Size);
			platform.Free(block, MuiListviewRenderState.Size);
		}
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
		if (!TryReadRenderState(ref platform, state, obj, out value) ||
			value.RenderInfo.IsNull || value.RastPort.IsNull)
		{
			value = default;
			value.RenderInfo = APTR.FromPointer(Read(ref platform, state, obj,
				RenderInfo, 0));
			if (value.RenderInfo.IsNull) return false;
			if (!MuiDrawingRenderInfoCodec.TryRead(ref platform, value.RenderInfo,
				out var fallbackInfo)) return false;
			value.RastPort = fallbackInfo.RastPort;
			return !value.RastPort.IsNull;
		}
		if (value.RenderInfo.IsNull || value.RastPort.IsNull) return false;
		if (!MuiDrawingRenderInfoCodec.TryRead(ref platform, value.RenderInfo,
			out var info) || info.RastPort.IsNull || info.RastPort.Raw !=
			value.RastPort.Raw) return false;
		return true;
	}

	internal static bool TryGetRenderState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListviewRenderState value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadRenderState(ref platform, state, obj, out value);

	private static bool TryReadLayoutState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListviewLayoutState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			LayoutStateKey, 0));
		return MuiListviewLayoutStateCodec.TryRead(ref platform, block,
			out value);
	}

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
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			LayoutStateKey, 0));
		if (MuiListviewLayoutStateCodec.TryRead(ref platform, block, out _))
			return MuiListviewLayoutStateCodec.Write(ref platform, block, value);
		if (block.IsNotNull && platform.IsMapped(block,
			MuiListviewLayoutState.Size))
		{
			platform.Clear(block, MuiListviewLayoutState.Size);
			platform.Free(block, MuiListviewLayoutState.Size);
		}
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
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
		out var value) ? value : fallback;

	private static bool BindChildRenderInfo<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR child)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var hasTypedRender = TryReadRenderState(ref platform, state, obj,
			out var renderState) && renderState.RenderInfo.IsNotNull;
		var renderInfo = hasTypedRender ? renderState.RenderInfo :
			APTR.FromPointer(Read(ref platform, state, obj, RenderInfo, 0));
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
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out _))
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, value, false);
		return true;
	}
}
