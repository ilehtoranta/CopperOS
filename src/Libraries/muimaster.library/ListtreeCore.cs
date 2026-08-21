/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Listtree.mcc (autodoc MUI_Listtree.doc, header mui/Listtree_mcc.h).
//
// Packaging: docs/Libraries/MorphOs320Mui/packaging.md classifies Listtree.mcc
// as an `external-component`, NOT a built-in muimaster class. This core is a
// deliberately standalone seam: it is never folded into the List-backed
// collection classifier (MuiCollectionClass), it identifies its objects by the
// exact, case-sensitive external class id "Listtree.mcc", and it is registered
// through RegisterListtreeExternalClass (which flags the class record
// ClassExternal, never ClassBuiltin). It reuses none of the .mui List backbone;
// instead it owns fixed guest-resident tree-node records.
//
// A tree node is a fixed 64-byte guest record whose read-only public prefix is
// binary-compatible with struct MUIS_Listtree_TreeNode from the header:
//   0  LONG  tn_Private1   (validation cookie)
//   4  LONG  tn_Private2   (owning listtree object, for validation)
//   8  char* tn_Name       (owned copy or borrowed pointer)
//   12 UWORD tn_Flags      (TNF_OPEN / TNF_LIST / TNF_FROZEN / TNF_NOSIGN)
//   14 APTR  tn_User       (construct-hook result / user pointer)
// followed by a private topology region (parent/child/sibling links, counters,
// and ownership bookkeeping) that callers never see.
//
// There are two conceptual lists per the autodoc: the full tree (all inserted
// nodes) and the display list (the bounded visible pre-order traversal that
// descends into a node only when it is TNF_OPEN). Every mutation is expressed
// through the guest-memory platform seam; no managed allocations, arrays,
// collections, delegates, LINQ, or exceptions are used. Ownership is
// failure-atomic: a node whose name/user allocation or construct hook cannot be
// honoured is rolled back before it is ever linked, and disposal recursively
// destructs every surviving node before the header block is released.
public static class MuiListtreeCore
{
	// Guest-resident Listtree header. The public tree-node ABI is separate;
	// this fixed state owns the root links, counters, redraw coalescing, and
	// drop-mark values used by the external component.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeHeaderState
	{
		internal const uint Size = 48;
		internal const uint Cookie = 0x4C545245u; // 'LTRE'

		internal uint Magic;
		internal APTR RootFirst;
		internal APTR RootLast;
		internal uint RootCount;
		internal uint Total;
		internal uint Redraw;
		internal uint Dirty;
		internal int DropEntry;
		internal uint DropValue;
		internal uint Reserved0;
		internal uint Reserved1;
		internal uint Reserved2;
	}

	internal enum MuiListtreeHeaderField : byte
	{
		Magic,
		RootFirst,
		RootLast,
		RootCount,
		Total,
		Redraw,
		Dirty,
		DropEntry,
		DropValue,
		Reserved0,
		Reserved1,
		Reserved2,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeHeaderFieldCursor
	{
		internal APTR Address;
		internal MuiListtreeHeaderField Field;
	}

	internal static class MuiListtreeHeaderFieldCursorCodec
	{
		private static bool TryResolve(MuiListtreeHeaderField field,
			out uint offset)
		{
			switch (field)
			{
				case MuiListtreeHeaderField.Magic:
					offset = 0;
					return true;
				case MuiListtreeHeaderField.RootFirst:
					offset = 4;
					return true;
				case MuiListtreeHeaderField.RootLast:
					offset = 8;
					return true;
				case MuiListtreeHeaderField.RootCount:
					offset = 12;
					return true;
				case MuiListtreeHeaderField.Total:
					offset = 16;
					return true;
				case MuiListtreeHeaderField.Redraw:
					offset = 20;
					return true;
				case MuiListtreeHeaderField.Dirty:
					offset = 24;
					return true;
				case MuiListtreeHeaderField.DropEntry:
					offset = 28;
					return true;
				case MuiListtreeHeaderField.DropValue:
					offset = 32;
					return true;
				case MuiListtreeHeaderField.Reserved0:
					offset = 36;
					return true;
				case MuiListtreeHeaderField.Reserved1:
					offset = 40;
					return true;
				case MuiListtreeHeaderField.Reserved2:
					offset = 44;
					return true;
			}
			offset = 0;
			return false;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListtreeHeaderFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
				cursor.Address.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Address, MuiListtreeHeaderState.Size))
				return false;
			address = APTR.FromPointer(cursor.Address.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeHeaderField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListtreeHeaderFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			value = platform.ReadUInt32(fieldAddress, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeHeaderField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListtreeHeaderFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			platform.WriteUInt32(fieldAddress, 0, value);
			return true;
		}
	}

	internal static class MuiListtreeHeaderCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeHeaderState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeHeaderState.Size) ||
				!MuiListtreeHeaderFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeHeaderField.Magic, out var magic) ||
				magic !=
				MuiListtreeHeaderState.Cookie) return false;
			value.Magic = MuiListtreeHeaderState.Cookie;
			if (!MuiListtreeHeaderFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListtreeHeaderField.RootFirst, out var rootFirst) ||
				!MuiListtreeHeaderFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeHeaderField.RootLast, out var rootLast) ||
				!MuiListtreeHeaderFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeHeaderField.RootCount, out value.RootCount) ||
				!MuiListtreeHeaderFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeHeaderField.Total, out value.Total) ||
				!MuiListtreeHeaderFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeHeaderField.Redraw, out value.Redraw) ||
				!MuiListtreeHeaderFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeHeaderField.Dirty, out value.Dirty) ||
				!MuiListtreeHeaderFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeHeaderField.DropEntry, out var dropEntry) ||
				!MuiListtreeHeaderFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeHeaderField.DropValue, out value.DropValue) ||
				!MuiListtreeHeaderFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeHeaderField.Reserved0, out value.Reserved0) ||
				!MuiListtreeHeaderFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeHeaderField.Reserved1, out value.Reserved1) ||
				!MuiListtreeHeaderFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeHeaderField.Reserved2, out value.Reserved2)) return false;
			value.RootFirst = APTR.FromPointer(rootFirst);
			value.RootLast = APTR.FromPointer(rootLast);
			value.DropEntry = unchecked((int)dropEntry);
			return true;
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeHeaderState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeHeaderState.Size) ||
				value.Magic != MuiListtreeHeaderState.Cookie) return false;
			return MuiListtreeHeaderFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreeHeaderField.Magic, value.Magic) &&
				MuiListtreeHeaderFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeHeaderField.RootFirst, value.RootFirst.Raw) &&
				MuiListtreeHeaderFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeHeaderField.RootLast, value.RootLast.Raw) &&
				MuiListtreeHeaderFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeHeaderField.RootCount, value.RootCount) &&
				MuiListtreeHeaderFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeHeaderField.Total, value.Total) &&
				MuiListtreeHeaderFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeHeaderField.Redraw, value.Redraw) &&
				MuiListtreeHeaderFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeHeaderField.Dirty, value.Dirty) &&
				MuiListtreeHeaderFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeHeaderField.DropEntry,
					unchecked((uint)value.DropEntry)) &&
				MuiListtreeHeaderFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeHeaderField.DropValue, value.DropValue) &&
				MuiListtreeHeaderFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeHeaderField.Reserved0, value.Reserved0) &&
				MuiListtreeHeaderFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeHeaderField.Reserved1, value.Reserved1) &&
				MuiListtreeHeaderFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeHeaderField.Reserved2, value.Reserved2);
		}
	}

	// Guest-resident Listtree object policy. The object attribute store remains
	// the public projection, while the mutation/query paths consume this named
	// record so policy reads do not depend on ad-hoc attribute offsets.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreePolicyStateRecord
	{
		internal const uint Size = 48;
		internal const uint Cookie = 0x4C54504Cu; // 'LTPL'

		internal uint Magic;
		internal APTR Active;
		internal uint DuplicateNodeName;
		internal uint Quiet;
		internal uint DragDropSort;
		internal uint DoubleClick;
		internal APTR CloseHook;
		internal APTR ConstructHook;
		internal APTR DestructHook;
		internal APTR DisplayHook;
		internal APTR OpenHook;
		internal APTR SortHook;
	}

	internal enum MuiListtreePolicyField : byte
	{
		Magic,
		Active,
		DuplicateNodeName,
		Quiet,
		DragDropSort,
		DoubleClick,
		CloseHook,
		ConstructHook,
		DestructHook,
		DisplayHook,
		OpenHook,
		SortHook,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreePolicyFieldCursor
	{
		internal APTR Address;
		internal MuiListtreePolicyField Field;
	}

	internal static class MuiListtreePolicyFieldCursorCodec
	{
		private static bool TryResolve(MuiListtreePolicyField field,
			out uint offset)
		{
			switch (field)
			{
				case MuiListtreePolicyField.Magic:
				case MuiListtreePolicyField.Active:
				case MuiListtreePolicyField.DuplicateNodeName:
				case MuiListtreePolicyField.Quiet:
				case MuiListtreePolicyField.DragDropSort:
				case MuiListtreePolicyField.DoubleClick:
				case MuiListtreePolicyField.CloseHook:
				case MuiListtreePolicyField.ConstructHook:
				case MuiListtreePolicyField.DestructHook:
				case MuiListtreePolicyField.DisplayHook:
				case MuiListtreePolicyField.OpenHook:
				case MuiListtreePolicyField.SortHook:
					offset = (uint)field * 4;
					return true;
			}
			offset = 0;
			return false;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListtreePolicyFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) ||
				cursor.Address.IsNull || cursor.Address.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Address,
					MuiListtreePolicyStateRecord.Size)) return false;
			address = APTR.FromPointer(cursor.Address.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreePolicyField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListtreePolicyFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			value = platform.ReadUInt32(fieldAddress, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreePolicyField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListtreePolicyFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			platform.WriteUInt32(fieldAddress, 0, value);
			return true;
		}
	}

	internal static class MuiListtreePolicyStateRecordCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreePolicyStateRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreePolicyStateRecord.Size) ||
				!MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreePolicyField.Magic, out var magic) ||
				magic != MuiListtreePolicyStateRecord.Cookie ||
				!MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreePolicyField.Active, out var active) ||
				!MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreePolicyField.DuplicateNodeName, out value.DuplicateNodeName) ||
				!MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreePolicyField.Quiet, out value.Quiet) ||
				!MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreePolicyField.DragDropSort, out value.DragDropSort) ||
				!MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreePolicyField.DoubleClick, out value.DoubleClick) ||
				!MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreePolicyField.CloseHook, out var closeHook) ||
				!MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreePolicyField.ConstructHook, out var constructHook) ||
				!MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreePolicyField.DestructHook, out var destructHook) ||
				!MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreePolicyField.DisplayHook, out var displayHook) ||
				!MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreePolicyField.OpenHook, out var openHook) ||
				!MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreePolicyField.SortHook, out var sortHook)) return false;
			value.Magic = MuiListtreePolicyStateRecord.Cookie;
			value.Active = APTR.FromPointer(active);
			value.CloseHook = APTR.FromPointer(closeHook);
			value.ConstructHook = APTR.FromPointer(constructHook);
			value.DestructHook = APTR.FromPointer(destructHook);
			value.DisplayHook = APTR.FromPointer(displayHook);
			value.OpenHook = APTR.FromPointer(openHook);
			value.SortHook = APTR.FromPointer(sortHook);
			return true;
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreePolicyStateRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreePolicyStateRecord.Size) ||
				value.Magic != MuiListtreePolicyStateRecord.Cookie) return false;
			return MuiListtreePolicyFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreePolicyField.Magic, value.Magic) &&
				MuiListtreePolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreePolicyField.Active, value.Active.Raw) &&
				MuiListtreePolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreePolicyField.DuplicateNodeName, value.DuplicateNodeName) &&
				MuiListtreePolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreePolicyField.Quiet, value.Quiet) &&
				MuiListtreePolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreePolicyField.DragDropSort, value.DragDropSort) &&
				MuiListtreePolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreePolicyField.DoubleClick, value.DoubleClick) &&
				MuiListtreePolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreePolicyField.CloseHook, value.CloseHook.Raw) &&
				MuiListtreePolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreePolicyField.ConstructHook, value.ConstructHook.Raw) &&
				MuiListtreePolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreePolicyField.DestructHook, value.DestructHook.Raw) &&
				MuiListtreePolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreePolicyField.DisplayHook, value.DisplayHook.Raw) &&
				MuiListtreePolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreePolicyField.OpenHook, value.OpenHook.Raw) &&
				MuiListtreePolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreePolicyField.SortHook, value.SortHook.Raw);
		}
	}

	// Complete guest-resident tree node. The first 18 bytes are the public
	// MUIS_Listtree_TreeNode prefix; the remaining fields are private topology
	// and ownership state. All node layout knowledge is contained here.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeNodeState
	{
		internal const uint Size = 64;
		internal const uint Cookie = 0x4C54544Eu; // 'LTTN'

		internal uint Private1;
		internal APTR Private2;
		internal APTR Name;
		internal ushort Flags;
		internal APTR User;
		internal ushort PublicReserved;
		internal APTR Parent;
		internal APTR FirstChild;
		internal APTR LastChild;
		internal APTR Next;
		internal APTR Previous;
		internal uint ChildCount;
		internal uint NameOwned;
		internal uint NameSize;
		internal uint UserOwned;
		internal uint Reserved0;
		internal uint Reserved1;
	}

	internal enum MuiListtreeNodeField : byte
	{
		Private1,
		Private2,
		Name,
		Flags,
		User,
		PublicReserved,
		Parent,
		FirstChild,
		LastChild,
		Next,
		Previous,
		ChildCount,
		NameOwned,
		NameSize,
		UserOwned,
		Reserved0,
		Reserved1,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeNodeFieldCursor
	{
		internal APTR Address;
		internal MuiListtreeNodeField Field;
	}

	internal static class MuiListtreeNodeFieldCursorCodec
	{
		private static bool TryResolve(MuiListtreeNodeField field,
			out uint offset, out uint size)
		{
			switch (field)
			{
				case MuiListtreeNodeField.Private1:
					offset = 0;
					size = 4;
					return true;
				case MuiListtreeNodeField.Private2:
					offset = 4;
					size = 4;
					return true;
				case MuiListtreeNodeField.Name:
					offset = 8;
					size = 4;
					return true;
				case MuiListtreeNodeField.Flags:
					offset = 12;
					size = 2;
					return true;
				case MuiListtreeNodeField.User:
					offset = 14;
					size = 4;
					return true;
				case MuiListtreeNodeField.PublicReserved:
					offset = 18;
					size = 2;
					return true;
				case MuiListtreeNodeField.Parent:
					offset = 20;
					size = 4;
					return true;
				case MuiListtreeNodeField.FirstChild:
					offset = 24;
					size = 4;
					return true;
				case MuiListtreeNodeField.LastChild:
					offset = 28;
					size = 4;
					return true;
				case MuiListtreeNodeField.Next:
					offset = 32;
					size = 4;
					return true;
				case MuiListtreeNodeField.Previous:
					offset = 36;
					size = 4;
					return true;
				case MuiListtreeNodeField.ChildCount:
					offset = 40;
					size = 4;
					return true;
				case MuiListtreeNodeField.NameOwned:
					offset = 44;
					size = 4;
					return true;
				case MuiListtreeNodeField.NameSize:
					offset = 48;
					size = 4;
					return true;
				case MuiListtreeNodeField.UserOwned:
					offset = 52;
					size = 4;
					return true;
				case MuiListtreeNodeField.Reserved0:
					offset = 56;
					size = 4;
					return true;
				case MuiListtreeNodeField.Reserved1:
					offset = 60;
					size = 4;
					return true;
			}
			offset = 0;
			size = 0;
			return false;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListtreeNodeFieldCursor cursor, out APTR address, out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolve(cursor.Field, out var offset, out size) ||
				cursor.Address.IsNull || cursor.Address.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Address, MuiListtreeNodeState.Size))
				return false;
			address = APTR.FromPointer(cursor.Address.Raw + offset);
			return platform.IsMapped(address, size);
		}

		internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeNodeField field, out ushort value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListtreeNodeFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
				out var size) || size != 2) return false;
			value = platform.ReadUInt16(fieldAddress, 0);
			return true;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeNodeField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListtreeNodeFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
				out var size) || size != 4) return false;
			value = platform.ReadUInt32(fieldAddress, 0);
			return true;
		}

		internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeNodeField field, ushort value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListtreeNodeFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
				out var size) || size != 2) return false;
			platform.WriteUInt16(fieldAddress, 0, value);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeNodeField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListtreeNodeFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
				out var size) || size != 4) return false;
			platform.WriteUInt32(fieldAddress, 0, value);
			return true;
		}
	}

	internal static class MuiListtreeNodeCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeNodeState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeNodeState.Size) ||
				!MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeNodeField.Private1, out var private1) ||
				private1 !=
				MuiListtreeNodeState.Cookie) return false;
			value.Private1 = MuiListtreeNodeState.Cookie;
			if (!MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiListtreeNodeField.Private2, out var private2) ||
				!MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeNodeField.Name, out var name) ||
				!MuiListtreeNodeFieldCursorCodec.TryReadUInt16(ref platform, address,
					MuiListtreeNodeField.Flags, out value.Flags) ||
				!MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeNodeField.User, out var user) ||
				!MuiListtreeNodeFieldCursorCodec.TryReadUInt16(ref platform, address,
					MuiListtreeNodeField.PublicReserved, out value.PublicReserved) ||
				!MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeNodeField.Parent, out var parent) ||
				!MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeNodeField.FirstChild, out var firstChild) ||
				!MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeNodeField.LastChild, out var lastChild) ||
				!MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeNodeField.Next, out var next) ||
				!MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeNodeField.Previous, out var previous) ||
				!MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeNodeField.ChildCount, out value.ChildCount) ||
				!MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeNodeField.NameOwned, out value.NameOwned) ||
				!MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeNodeField.NameSize, out value.NameSize) ||
				!MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeNodeField.UserOwned, out value.UserOwned) ||
				!MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeNodeField.Reserved0, out value.Reserved0) ||
				!MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeNodeField.Reserved1, out value.Reserved1)) return false;
			value.Private2 = APTR.FromPointer(private2);
			value.Name = APTR.FromPointer(name);
			value.User = APTR.FromPointer(user);
			value.Parent = APTR.FromPointer(parent);
			value.FirstChild = APTR.FromPointer(firstChild);
			value.LastChild = APTR.FromPointer(lastChild);
			value.Next = APTR.FromPointer(next);
			value.Previous = APTR.FromPointer(previous);
			return true;
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeNodeState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeNodeState.Size) ||
				value.Private1 != MuiListtreeNodeState.Cookie) return false;
			return MuiListtreeNodeFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreeNodeField.Private1, value.Private1) &&
				MuiListtreeNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeNodeField.Private2, value.Private2.Raw) &&
				MuiListtreeNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeNodeField.Name, value.Name.Raw) &&
				MuiListtreeNodeFieldCursorCodec.TryWriteUInt16(ref platform, address,
					MuiListtreeNodeField.Flags, value.Flags) &&
				MuiListtreeNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeNodeField.User, value.User.Raw) &&
				MuiListtreeNodeFieldCursorCodec.TryWriteUInt16(ref platform, address,
					MuiListtreeNodeField.PublicReserved, value.PublicReserved) &&
				MuiListtreeNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeNodeField.Parent, value.Parent.Raw) &&
				MuiListtreeNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeNodeField.FirstChild, value.FirstChild.Raw) &&
				MuiListtreeNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeNodeField.LastChild, value.LastChild.Raw) &&
				MuiListtreeNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeNodeField.Next, value.Next.Raw) &&
				MuiListtreeNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeNodeField.Previous, value.Previous.Raw) &&
				MuiListtreeNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeNodeField.ChildCount, value.ChildCount) &&
				MuiListtreeNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeNodeField.NameOwned, value.NameOwned) &&
				MuiListtreeNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeNodeField.NameSize, value.NameSize) &&
				MuiListtreeNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeNodeField.UserOwned, value.UserOwned) &&
				MuiListtreeNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeNodeField.Reserved0, value.Reserved0) &&
				MuiListtreeNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeNodeField.Reserved1, value.Reserved1);
		}
	}

	// Named view of the public MUIS_Listtree_TreeNode prefix. The private
	// topology is represented by MuiListtreeNodeState above; this projection
	// preserves the small public codec surface for callers and tests.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeNodePublicState
	{
		internal const uint Size = 18;
		internal const uint Cookie = 0x4C54544Eu; // 'LTTN'

		internal uint Private1;
		internal APTR Private2;
		internal APTR Name;
		internal ushort Flags;
		internal APTR User;
	}

	internal static class MuiListtreeNodePublicCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeNodePublicState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiListtreeNodeCodec.TryRead(ref platform, address, out var node))
				return false;
			value.Private1 = node.Private1;
			value.Private2 = node.Private2;
			value.Name = node.Name;
			value.Flags = node.Flags;
			value.User = node.User;
			return true;
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeNodePublicState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var node = default(MuiListtreeNodeState);
			if (MuiListtreeNodeCodec.TryRead(ref platform, address,
				out var current)) node = current;
			node.Private1 = value.Private1;
			node.Private2 = value.Private2;
			node.Name = value.Name;
			node.Flags = value.Flags;
			node.User = value.User;
			return MuiListtreeNodeCodec.Write(ref platform, address, node);
		}
	}

	// MUIM_Listtree_TestPos publishes a packed 12-byte result. Keep the mixed
	// APTR/UWORD/LONG/UWORD fields named so the method body never writes public
	// ABI offsets directly.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeTestPosResult
	{
		internal const uint Size = 12;
		internal APTR TreeNode;
		internal ushort Flags;
		internal int ListEntry;
		internal ushort ListFlags;
	}

	internal enum MuiListtreeTestPosField : byte
	{
		TreeNode,
		Flags,
		ListEntry,
		ListFlags,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeTestPosFieldCursor
	{
		internal APTR Address;
		internal MuiListtreeTestPosField Field;
	}

	internal static class MuiListtreeTestPosFieldCursorCodec
	{
		private static bool TryResolve(MuiListtreeTestPosField field,
			out uint offset, out uint size)
		{
			switch (field)
			{
				case MuiListtreeTestPosField.TreeNode:
					offset = 0;
					size = 4;
					return true;
				case MuiListtreeTestPosField.Flags:
					offset = 4;
					size = 2;
					return true;
				case MuiListtreeTestPosField.ListEntry:
					offset = 6;
					size = 4;
					return true;
				case MuiListtreeTestPosField.ListFlags:
					offset = 10;
					size = 2;
					return true;
			}
			offset = 0;
			size = 0;
			return false;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListtreeTestPosFieldCursor cursor, out APTR address, out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolve(cursor.Field, out var offset, out size) ||
				cursor.Address.IsNull || cursor.Address.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Address, MuiListtreeTestPosResult.Size))
				return false;
			address = APTR.FromPointer(cursor.Address.Raw + offset);
			return platform.IsMapped(address, size);
		}

		internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeTestPosField field, out ushort value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListtreeTestPosFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
				out var size) || size != 2) return false;
			value = platform.ReadUInt16(fieldAddress, 0);
			return true;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeTestPosField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListtreeTestPosFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
				out var size) || size != 4) return false;
			value = platform.ReadUInt32(fieldAddress, 0);
			return true;
		}

		internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeTestPosField field, ushort value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListtreeTestPosFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
				out var size) || size != 2) return false;
			platform.WriteUInt16(fieldAddress, 0, value);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeTestPosField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListtreeTestPosFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
				out var size) || size != 4) return false;
			platform.WriteUInt32(fieldAddress, 0, value);
			return true;
		}
	}

	internal static class MuiListtreeTestPosResultCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeTestPosResult value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeTestPosResult.Size)) return false;
			if (!MuiListtreeTestPosFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListtreeTestPosField.TreeNode, out var treeNode) ||
				!MuiListtreeTestPosFieldCursorCodec.TryReadUInt16(ref platform, address,
					MuiListtreeTestPosField.Flags, out value.Flags) ||
				!MuiListtreeTestPosFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeTestPosField.ListEntry, out var listEntry) ||
				!MuiListtreeTestPosFieldCursorCodec.TryReadUInt16(ref platform, address,
					MuiListtreeTestPosField.ListFlags, out value.ListFlags)) return false;
			value.TreeNode = APTR.FromPointer(treeNode);
			value.ListEntry = unchecked((int)listEntry);
			return true;
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeTestPosResult value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeTestPosResult.Size)) return false;
			return MuiListtreeTestPosFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreeTestPosField.TreeNode, value.TreeNode.Raw) &&
				MuiListtreeTestPosFieldCursorCodec.TryWriteUInt16(ref platform, address,
					MuiListtreeTestPosField.Flags, value.Flags) &&
				MuiListtreeTestPosFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListtreeTestPosField.ListEntry,
					unchecked((uint)value.ListEntry)) &&
				MuiListtreeTestPosFieldCursorCodec.TryWriteUInt16(ref platform, address,
					MuiListtreeTestPosField.ListFlags, value.ListFlags);
		}
	}

	// ---- Public attribute identifiers (header mui/Listtree_mcc.h) ------------
	public const uint Active = 0x80020020u;          // [ISG] LONG (0 == Off)
	public const uint CloseHook = 0x80020033u;       // [ISG] struct Hook *
	public const uint ConstructHook = 0x80020016u;   // [ISG] struct Hook *
	public const uint DestructHook = 0x80020017u;    // [ISG] struct Hook *
	public const uint DisplayHook = 0x80020018u;     // [ISG] struct Hook *
	public const uint DoubleClick = 0x8002000du;     // [ISG] LONG
	public const uint DragDropSort = 0x80020031u;    // [ISG] LONG (BOOL)
	public const uint DuplicateNodeName = 0x8002003du; // [ISG] BOOL
	public const uint EmptyNodes = 0x80020030u;      // [ISG] BOOL
	public const uint Format = 0x80020014u;          // [ISG] CONST_STRPTR
	public const uint MultiSelect = 0x800200c3u;     // [ISG] BOOL
	public const uint NList = 0x800200c4u;           // [ISG] BOOL
	public const uint OpenHook = 0x80020032u;        // [ISG] struct Hook *
	public const uint Quiet = 0x8002000au;           // [.S.] BOOL
	public const uint SortHook = 0x80020010u;        // [ISG] struct Hook *
	public const uint Title = 0x80020015u;           // [ISG] CONST_STRPTR
	public const uint TreeColumn = 0x80020013u;      // [ISG] BOOL

	// ---- Method identifiers --------------------------------------------------
	public const uint MethodClose = 0x8002001fu;
	public const uint MethodExchange = 0x80020008u;
	public const uint MethodFindName = 0x8002003cu;
	public const uint MethodGetEntry = 0x8002002bu;
	public const uint MethodGetNr = 0x8002000eu;
	public const uint MethodInsert = 0x80020011u;
	public const uint MethodMove = 0x80020009u;
	public const uint MethodOpen = 0x8002001eu;
	public const uint MethodRemove = 0x80020012u;
	public const uint MethodRename = 0x8002000cu;
	public const uint MethodSetDropMark = 0x8002004cu;
	public const uint MethodSort = 0x80020029u;
	public const uint MethodTestPos = 0x8002004bu;

	// ---- Selectors (signed) --------------------------------------------------
	// ListNode: Root == 0, Parent == -1 (Open only), Active == -2.
	private const int ListNodeRoot = 0;
	private const int ListNodeParent = -1;
	private const int ListNodeActive = -2;
	// TreeNode: Head == 0, Tail == -1, Active == -2, All == -3.
	private const int TreeNodeHead = 0;
	private const int TreeNodeTail = -1;
	private const int TreeNodeActive = -2;
	private const int TreeNodeAll = -3;
	// PrevNode (Insert): Head == 0, Tail == -1, Active == -2, Sorted == -4.
	private const int PrevNodeHead = 0;
	private const int PrevNodeTail = -1;
	private const int PrevNodeActive = -2;
	private const int PrevNodeSorted = -4;
	// GetEntry positions.
	private const int PositionHead = 0;
	private const int PositionTail = -1;
	private const int PositionActive = -2;
	private const int PositionNext = -3;
	private const int PositionPrevious = -4;
	private const int PositionParent = -5;
	// Move NewTreeNode: Head/Tail/Active/Sorted mirror the Prev selectors.
	private const int NewTreeNodeSorted = -4;
	// Sort/SortHook builtin selectors.
	private const uint SortHookHead = 0x00000000u;         // 0
	private const uint SortHookTail = 0xFFFFFFFFu;         // -1
	private const uint SortHookLeavesTop = 0xFFFFFFFEu;    // -2
	private const uint SortHookLeavesMixed = 0xFFFFFFFDu;  // -3
	private const uint SortHookLeavesBottom = 0xFFFFFFFCu; // -4 (default)
	private const uint ConstructHookString = 0xFFFFFFFFu;  // -1
	private const uint ActiveOff = 0;

	// ---- Method flags --------------------------------------------------------
	private const uint FlagsNr = 1u << 15;
	private const uint FlagsVisible = 1u << 14;
	private const uint InsertFlagsActive = 1u << 13;
	private const uint InsertFlagsNextNode = 1u << 12;
	private const uint FindSameLevel = 1u << 15;
	private const uint GetEntrySameLevel = 1u << 15;
	private const uint GetNrListEmpty = 1u << 12;
	private const uint GetNrCountList = 1u << 13;
	private const uint GetNrCountLevel = 1u << 14;
	private const uint GetNrCountAll = 1u << 15;
	private const uint RenameFlagsUser = 1u << 8;
	private const uint RenameFlagsNoRefresh = 1u << 9;

	// ---- TestPos / SetDropMark values ---------------------------------------
	public const uint DropMarkNone = 0;
	public const uint DropMarkAbove = 1;
	public const uint DropMarkBelow = 2;
	public const uint DropMarkOnto = 3;
	public const uint DropMarkSorted = 4;

	// ---- Tree node flags (header) -------------------------------------------
	public const uint TNF_OPEN = 1u << 0;
	public const uint TNF_LIST = 1u << 1;
	public const uint TNF_FROZEN = 1u << 2;
	public const uint TNF_NOSIGN = 1u << 3;

	// ---- Public node record layout (MUIS_Listtree_TreeNode compatible) -------
	public const int TreeNodePrivate1 = 0;
	public const int TreeNodePrivate2 = 4;
	public const int TreeNodeNameOffset = 8;   // char *tn_Name
	public const int TreeNodeFlagsOffset = 12; // UWORD tn_Flags
	public const int TreeNodeUserOffset = 14;  // APTR  tn_User

	// ---- Private node topology ----------------------------------------------
	private const uint NodeSize = MuiListtreeNodeState.Size;

	// ---- Header block (parked in a reserved object attribute) ----------------
	private const uint TreeHeaderKey = 0x7F090001u;
	private const uint HeaderSize = MuiListtreeHeaderState.Size;
	private const uint PolicyStateKey = 0x7F090002u;
	private const uint PolicyStateSize = MuiListtreePolicyStateRecord.Size;

	private const uint MaximumNodes = 0x00040000u;
	private const uint MaximumDepth = 512;
	private const uint MaximumTraversal = 0x00040000u;
	private const uint MaximumStringLength = 4096;

	// =========================================================================
	// Class identity / registration (external component)
	// =========================================================================

	// Register "Listtree.mcc" as an external class. Mirrors real external-
	// component semantics: the caller supplies the loaded class' BOOPSI pointer
	// (e.g. from the loader); the registry does not own or free it. The record is
	// flagged ClassExternal (never ClassBuiltin), keeping the packaging
	// disposition intact: Listtree is loader-discoverable, not part of the master
	// library's built-in set. The name MUST be exactly "Listtree.mcc" (case
	// sensitive per the loader contract); any other id is rejected.
	public static APTR RegisterListtreeExternalClass<TPlatform>(ref TPlatform platform,
		APTR state, APTR className, APTR boopsiClass, APTR superClass)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!NameIsListtree(ref platform, className) || boopsiClass.IsNull)
			return APTR.Null;
		return MuiHeadlessObjectCore.RegisterExternalClass(ref platform, state,
			className, boopsiClass, superClass);
	}

	public static bool IsListtree<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return false;
		return ClassRecordIsListtree(ref platform, objectValue.Class);
	}

	public static bool ClassRecordIsListtree<TPlatform>(ref TPlatform platform,
		APTR classRecord) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
			out var classValue))
			return false;
		return NameIsListtree(ref platform, classValue.Name);
	}

	// Case-sensitive bounded compare against "Listtree.mcc". Spelled out byte by
	// byte (no span/u8 literal) so the freestanding native closure stays free of
	// static-data relocations.
	private static bool NameIsListtree<TPlatform>(ref TPlatform platform,
		APTR name) where TPlatform : struct, IMuiGuestMemory
	{
		if (name.IsNull || !platform.IsMapped(name, 13)) return false;
		return platform.ReadUInt8(name, 0) == (byte)'L' &&
			platform.ReadUInt8(name, 1) == (byte)'i' &&
			platform.ReadUInt8(name, 2) == (byte)'s' &&
			platform.ReadUInt8(name, 3) == (byte)'t' &&
			platform.ReadUInt8(name, 4) == (byte)'t' &&
			platform.ReadUInt8(name, 5) == (byte)'r' &&
			platform.ReadUInt8(name, 6) == (byte)'e' &&
			platform.ReadUInt8(name, 7) == (byte)'e' &&
			platform.ReadUInt8(name, 8) == (byte)'.' &&
			platform.ReadUInt8(name, 9) == (byte)'m' &&
			platform.ReadUInt8(name, 10) == (byte)'c' &&
			platform.ReadUInt8(name, 11) == (byte)'c' &&
			platform.ReadUInt8(name, 12) == 0;
	}

	// =========================================================================
	// Lifecycle
	// =========================================================================

	public static APTR CreateListtree<TPlatform>(ref TPlatform platform, APTR state,
		APTR classRecord, APTR tags) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!ClassRecordIsListtree(ref platform, classRecord)) return APTR.Null;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, state,
			classRecord, tags);
		if (obj.IsNull) return APTR.Null;
		if (!Construct(ref platform, state, obj))
		{
			MuiCollectionLifecycle.DisposeObject(ref platform, state, obj);
			return APTR.Null;
		}
		return obj;
	}

	// Attach and initialise the fixed header/state. Safe to call once.
	public static bool Construct<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (Header(ref platform, state, obj).IsNotNull) return true;
		var header = MuiHeadlessMemory.Allocate(ref platform, HeaderSize);
		if (header.IsNull) return false;
		var headerValue = default(MuiListtreeHeaderState);
		headerValue.Magic = MuiListtreeHeaderState.Cookie;
		if (!MuiListtreeHeaderCodec.Write(ref platform, header, headerValue))
		{
			platform.Clear(header, HeaderSize);
			platform.Free(header, HeaderSize);
			return false;
		}
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			TreeHeaderKey, header.Raw, false))
		{
			platform.Clear(header, HeaderSize);
			platform.Free(header, HeaderSize);
			return false;
		}
		EnsureDefault(ref platform, state, obj, DuplicateNodeName, 1);
		EnsureDefault(ref platform, state, obj, Active, ActiveOff);
		EnsureDefault(ref platform, state, obj, Quiet, 0);
		EnsureDefault(ref platform, state, obj, DragDropSort, 1);
		EnsureDefault(ref platform, state, obj, DoubleClick, 0xFFFFFFFFu);
		return EnsurePolicyStateRecord(ref platform, state, obj);
	}

	// Retire every guest-resident node and the header during disposal. Invoked
	// from MuiCollectionLifecycle.DisposeObject; a no-op for non-Listtree objects.
	internal static void CleanupRecords<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return;
		var node = ReadHeaderRootFirst(ref platform, header);
		while (node.IsNotNull)
		{
			var next = ReadNodeNext(ref platform, node);
			FreeSubtree(ref platform, state, obj, node, 0);
			node = next;
		}
		platform.Clear(header, HeaderSize);
		platform.Free(header, HeaderSize);
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, TreeHeaderKey,
			0, false);
	}

	// =========================================================================
	// Attributes
	// =========================================================================

	internal static bool IsPolicyAttribute(uint attribute) =>
		attribute == Active || attribute == DuplicateNodeName ||
		attribute == Quiet || attribute == DragDropSort ||
		attribute == DoubleClick || attribute == CloseHook ||
		attribute == ConstructHook || attribute == DestructHook ||
		attribute == DisplayHook || attribute == OpenHook ||
		attribute == SortHook;

	// Public Listtree policy getters are projected from the canonical guest
	// record. Bootstrap helpers below intentionally use GetRawAttribute so this
	// class-gated route cannot recurse while the record is being created.
	internal static bool IsPublicGetterAttribute(uint attribute) =>
		IsPolicyAttribute(attribute);

	private static bool TryReadPolicyStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListtreePolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PolicyStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PolicyStateKey) != unchecked((int)PolicyStateSize)) return false;
		return MuiListtreePolicyStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static void FillPolicyStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, ref MuiListtreePolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value.Magic = MuiListtreePolicyStateRecord.Cookie;
		value.Active = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			Active, ActiveOff));
		value.DuplicateNodeName = ReadRaw(ref platform, state, obj,
			DuplicateNodeName, 1);
		value.Quiet = ReadRaw(ref platform, state, obj, Quiet, 0);
		value.DragDropSort = ReadRaw(ref platform, state, obj, DragDropSort, 1);
		value.DoubleClick = ReadRaw(ref platform, state, obj, DoubleClick,
			0xFFFFFFFFu);
		value.CloseHook = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			CloseHook, 0));
		value.ConstructHook = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			ConstructHook, 0));
		value.DestructHook = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			DestructHook, 0));
		value.DisplayHook = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			DisplayHook, 0));
		value.OpenHook = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			OpenHook, 0));
		value.SortHook = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			SortHook, SortHookLeavesBottom));
	}

	private static bool EnsurePolicyStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadPolicyStateRecord(ref platform, state, obj, out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform, PolicyStateSize);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, PolicyStateSize);
		var value = default(MuiListtreePolicyStateRecord);
		FillPolicyStateRecord(ref platform, state, obj, ref value);
		var written = MuiListtreePolicyStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			PolicyStateKey, scratch, unchecked((int)PolicyStateSize));
		platform.Clear(scratch, PolicyStateSize);
		platform.Free(scratch, PolicyStateSize);
		return added;
	}

	private static bool SyncPolicyStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsurePolicyStateRecord(ref platform, state, obj) ||
			!TryReadPolicyStateRecord(ref platform, state, obj, out var value))
			return false;
		FillPolicyStateRecord(ref platform, state, obj, ref value);
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PolicyStateKey);
		return MuiListtreePolicyStateRecordCodec.Write(ref platform, block, value);
	}

	internal static bool TryGetPolicyStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListtreePolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadPolicyStateRecord(ref platform, state, obj, out value);

	private static bool TryReadPolicyValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (!IsPolicyAttribute(attribute) ||
			!TryReadPolicyStateRecord(ref platform, state, obj, out var record))
			return false;
		switch (attribute)
		{
			case Active: value = record.Active.Raw; return true;
			case DuplicateNodeName: value = record.DuplicateNodeName; return true;
			case Quiet: value = record.Quiet; return true;
			case DragDropSort: value = record.DragDropSort; return true;
			case DoubleClick: value = record.DoubleClick; return true;
			case CloseHook: value = record.CloseHook.Raw; return true;
			case ConstructHook: value = record.ConstructHook.Raw; return true;
			case DestructHook: value = record.DestructHook.Raw; return true;
			case DisplayHook: value = record.DisplayHook.Raw; return true;
			case OpenHook: value = record.OpenHook.Raw; return true;
			case SortHook: value = record.SortHook.Raw; return true;
		}
		return false;
	}

	public static bool SetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		if (attribute == Quiet)
		{
			var wasQuiet = ReadPolicy(ref platform, state, obj, Quiet, 0) != 0;
			var nowQuiet = value != 0;
			if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, Quiet,
				nowQuiet ? 1u : 0u, false)) return false;
			// Turning Quiet off flushes exactly one coalesced refresh.
			if (wasQuiet && !nowQuiet &&
				ReadHeaderDirty(ref platform, header) != 0)
			{
				WriteHeaderDirty(ref platform, header, 0);
				WriteHeaderRedraw(ref platform, header,
					ReadHeaderRedraw(ref platform, header) + 1);
			}
			return SyncPolicyStateRecord(ref platform, state, obj);
		}
		if (attribute == Active)
		{
			// Only move the cursor onto a valid node of this tree, or Off.
			if (value != ActiveOff && !IsValidNode(ref platform, obj,
				APTR.FromPointer(value))) return false;
			if (!SetActive(ref platform, state, obj, APTR.FromPointer(value),
				notify)) return false;
			return SyncPolicyStateRecord(ref platform, state, obj);
		}
		if (!IsPolicyAttribute(attribute))
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, value, notify);
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			attribute, value, notify)) return false;
		return SyncPolicyStateRecord(ref platform, state, obj);
	}

	public static bool GetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadPolicyValue(ref platform, state, obj, attribute, out value))
			return true;
		if (IsPolicyAttribute(attribute))
			return MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out value);
		return MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			attribute, out value);
	}

	// =========================================================================
	// Insert
	// =========================================================================

	// MUIM_Listtree_Insert. Returns the fresh tree node, or Null when nothing was
	// added (bad target, a construct hook that returned NULL, or an allocation
	// failure). Failure is atomic: an allocated record/name is rolled back
	// before anything is linked into the tree.
	public static APTR Insert<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR name, APTR user, APTR listNode, APTR prevNode, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return APTR.Null;
		if (ReadHeaderTotal(ref platform, header) >= MaximumNodes) return APTR.Null;
		if (!ResolveList(ref platform, state, obj, listNode, out var parent))
			return APTR.Null;

		var node = MuiHeadlessMemory.Allocate(ref platform, NodeSize);
		if (node.IsNull) return APTR.Null;
		var publicState = default(MuiListtreeNodePublicState);
		publicState.Private1 = MuiListtreeNodePublicState.Cookie;
		publicState.Private2 = obj;
		publicState.Name = name;
		publicState.Flags = 0;
		publicState.User = APTR.Null;
		if (!MuiListtreeNodePublicCodec.Write(ref platform, node, publicState))
		{
			FreeNodeRecord(ref platform, node);
			return APTR.Null;
		}

		// Name: buffered unless MUIA_Listtree_DuplicateNodeName is FALSE.
		var duplicate = ReadPolicy(ref platform, state, obj, DuplicateNodeName, 1) != 0;
		if (duplicate && name.IsNotNull)
		{
			var copy = DuplicateString(ref platform, name, out var size);
			if (copy.IsNull) { FreeNodeRecord(ref platform, node); return APTR.Null; }
			WriteNodeName(ref platform, node, copy);
			WriteNodeNumber(ref platform, node, 1, NodeField.NameOwned);
			WriteNodeNumber(ref platform, node, size, NodeField.NameSize);
		}
		else
		{
			WriteNodeName(ref platform, node, name);
		}

		// User: construct seam. A hook that returns NULL adds nothing.
		var stored = ConstructUser(ref platform, state, obj, user,
			out var userOwned);
		if (HasConstructHook(ref platform, state, obj) && stored.IsNull)
		{
			DestructOwnedName(ref platform, node);
			FreeNodeRecord(ref platform, node);
			return APTR.Null;
		}
		WriteNodeUser(ref platform, node, stored);
		WriteNodeNumber(ref platform, node, userOwned, NodeField.UserOwned);

		LinkForInsert(ref platform, state, obj, header, parent, node, prevNode,
			flags);
		// A populated parent is a node; mark it so leaf/node sort rules apply.
		if (parent.IsNotNull)
			SetFlagBits(ref platform, parent, TNF_LIST);
		WriteHeaderTotal(ref platform, header,
			ReadHeaderTotal(ref platform, header) + 1);
		if ((flags & InsertFlagsActive) != 0)
			SetActive(ref platform, state, obj, node, true);
		Redraw(ref platform, state, obj, header);
		return node;
	}

	private static void LinkForInsert<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR header, APTR parent, APTR node, APTR prevNode, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sv = unchecked((int)prevNode.Raw);
		if ((flags & FlagsNr) != 0 && sv >= 0)
		{
			var anchor = NthChild(ref platform, header, parent, (uint)sv);
			if ((flags & InsertFlagsNextNode) != 0)
			{
				if (anchor.IsNull) LinkAsFirstChild(ref platform, header, parent, node);
				else LinkBefore(ref platform, header, parent, anchor, node);
			}
			else
			{
				if (anchor.IsNull) LinkAsLastChild(ref platform, header, parent, node);
				else LinkAfter(ref platform, header, parent, anchor, node);
			}
			return;
		}
		switch (sv)
		{
			case PrevNodeHead:
				LinkAsFirstChild(ref platform, header, parent, node);
				return;
			case PrevNodeTail:
				LinkAsLastChild(ref platform, header, parent, node);
				return;
			case PrevNodeActive:
			{
				var active = ActiveNode(ref platform, state, obj);
				if (active.IsNotNull && SameParent(ref platform, active, parent))
					LinkAfter(ref platform, header, parent, active, node);
				else LinkAsLastChild(ref platform, header, parent, node);
				return;
			}
			case PrevNodeSorted:
				LinkSorted(ref platform, state, obj, header, parent, node);
				return;
			default:
			{
				var anchor = APTR.FromPointer(prevNode.Raw);
				if (!IsValidNode(ref platform, obj, anchor) ||
					!SameParent(ref platform, anchor, parent))
				{
					LinkAsLastChild(ref platform, header, parent, node);
					return;
				}
				if ((flags & InsertFlagsNextNode) != 0)
					LinkBefore(ref platform, header, parent, anchor, node);
				else LinkAfter(ref platform, header, parent, anchor, node);
				return;
			}
		}
	}

	private static void LinkSorted<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR header, APTR parent, APTR node)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var hook = SortHookValue(ref platform, state, obj);
		if (hook == SortHookHead)
		{
			LinkAsFirstChild(ref platform, header, parent, node);
			return;
		}
		if (hook == SortHookTail)
		{
			LinkAsLastChild(ref platform, header, parent, node);
			return;
		}
		var child = ListFirst(ref platform, header, parent);
		uint guard = 0;
		while (child.IsNotNull && guard++ < MaximumTraversal)
		{
			if (Compare(ref platform, state, obj, child, node) > 0)
			{
				LinkBefore(ref platform, header, parent, child, node);
				return;
			}
			child = ReadNodeNext(ref platform, child);
		}
		LinkAsLastChild(ref platform, header, parent, node);
	}

	// =========================================================================
	// Remove (recursive)
	// =========================================================================

	public static bool Remove<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR listNode, APTR treeNode, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		if (!ResolveList(ref platform, state, obj, listNode, out var parent))
			return false;
		var sv = unchecked((int)treeNode.Raw);
		if (sv == TreeNodeAll && (flags & FlagsNr) == 0)
		{
			var child = ListFirst(ref platform, header, parent);
			var removedAny = false;
			uint guard = 0;
			while (child.IsNotNull && guard++ < MaximumTraversal)
			{
				var next = ReadNodeNext(ref platform, child);
				RemoveNode(ref platform, state, obj, header, child);
				removedAny = true;
				child = next;
			}
			if (removedAny) Redraw(ref platform, state, obj, header);
			return removedAny;
		}
		var node = ResolveTreeNode(ref platform, state, obj, header, parent,
			treeNode, flags);
		if (node.IsNull) return false;
		RemoveNode(ref platform, state, obj, header, node);
		Redraw(ref platform, state, obj, header);
		return true;
	}

	// Detach a node (and its subtree) and free it, keeping the active cursor and
	// counters consistent.
	private static void RemoveNode<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR header, APTR node)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var active = ActiveNode(ref platform, state, obj);
		if (active.IsNotNull && (active.Raw == node.Raw ||
			IsAncestor(ref platform, node, active)))
		{
			var successor = ReadNodeNext(ref platform, node);
			if (successor.IsNull)
				successor = ReadNodePrevious(ref platform, node);
			if (successor.IsNull)
				successor = ReadNodeParent(ref platform, node);
			SetActive(ref platform, state, obj, successor, true);
		}
		Unlink(ref platform, state, header, node);
		FreeSubtree(ref platform, state, obj, node, 0);
	}

	// =========================================================================
	// GetEntry / GetNr
	// =========================================================================

	public static APTR GetEntry<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR node, int position, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return APTR.Null;
		var sameLevel = (flags & GetEntrySameLevel) != 0;
		var visible = (flags & FlagsVisible) != 0;
		switch (position)
		{
			case PositionActive:
				return ActiveNode(ref platform, state, obj);
			case PositionParent:
			{
				var n = ResolveNodeArgument(ref platform, state, obj, node);
				return n.IsNull ? APTR.Null
					: ReadNodeParent(ref platform, n);
			}
			case PositionNext:
			{
				var n = ResolveNodeArgument(ref platform, state, obj, node);
				if (n.IsNull) return APTR.Null;
				return sameLevel
					? ReadNodeNext(ref platform, n)
					: PreorderNext(ref platform, header, n, visible);
			}
			case PositionPrevious:
			{
				var n = ResolveNodeArgument(ref platform, state, obj, node);
				if (n.IsNull) return APTR.Null;
				return sameLevel
					? ReadNodePrevious(ref platform, n)
					: PreorderPrevious(ref platform, header, n, visible);
			}
			case PositionHead:
			{
				if (!ResolveList(ref platform, state, obj, node, out var parent))
					return APTR.Null;
				return ListFirst(ref platform, header, parent);
			}
			case PositionTail:
			{
				if (!ResolveList(ref platform, state, obj, node, out var parent))
					return APTR.Null;
				return ListLast(ref platform, header, parent);
			}
			default:
			{
				if (position < 0) return APTR.Null;
				if (!ResolveList(ref platform, state, obj, node, out var parent))
					return APTR.Null;
				return visible
					? NthVisibleChild(ref platform, header, parent, (uint)position)
					: NthChild(ref platform, header, parent, (uint)position);
			}
		}
	}

	public static uint GetNr<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR treeNode, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return 0;
		var node = unchecked((int)treeNode.Raw) == TreeNodeActive
			? ActiveNode(ref platform, state, obj)
			: APTR.FromPointer(treeNode.Raw);
		if ((flags & GetNrCountAll) != 0)
			return ReadHeaderTotal(ref platform, header);
		if ((flags & GetNrListEmpty) != 0)
		{
			if (node.IsNull || !IsValidNode(ref platform, obj, node)) return 1;
			return ReadNodeChildCount(ref platform, node) == 0 ? 1u : 0u;
		}
		if ((flags & GetNrCountList) != 0)
			return node.IsNull || !IsValidNode(ref platform, obj, node)
				? ReadHeaderRootCount(ref platform, header)
				: ReadNodeChildCount(ref platform, node);
		if ((flags & GetNrCountLevel) != 0)
		{
			var parent = node.IsNull ? APTR.Null
				: ReadNodeParent(ref platform, node);
			return ListCount(ref platform, header, parent);
		}
		if (node.IsNull || !IsValidNode(ref platform, obj, node))
			return 0xFFFFFFFFu;
		return VisibleIndexOf(ref platform, header, node);
	}

	// =========================================================================
	// Open / Close
	// =========================================================================

	public static bool Open<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR listNode, APTR treeNode, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		OpenClose(ref platform, state, obj, listNode, treeNode, flags, true);

	public static bool Close<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR listNode, APTR treeNode, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		OpenClose(ref platform, state, obj, listNode, treeNode, flags, false);

	private static bool OpenClose<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR listNode, APTR treeNode, uint flags, bool open)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		var openParents = open && unchecked((int)listNode.Raw) == ListNodeParent;
		APTR parent;
		if (openParents) parent = APTR.Null;
		else if (!ResolveList(ref platform, state, obj, listNode, out parent))
			return false;
		var sv = unchecked((int)treeNode.Raw);
		if (sv == TreeNodeAll && (flags & FlagsNr) == 0)
		{
			var child = ListFirst(ref platform, header, parent);
			var any = false;
			uint guard = 0;
			while (child.IsNotNull && guard++ < MaximumTraversal)
			{
				if ((ReadFlags(ref platform, child) & TNF_LIST) != 0)
					any |= ApplyOpen(ref platform, state, obj, child, open, false);
			child = ReadNodeNext(ref platform, child);
			}
			if (any) Redraw(ref platform, state, obj, header);
			return any;
		}
		var node = ResolveTreeNode(ref platform, state, obj, header, parent,
			treeNode, flags);
		if (node.IsNull || (ReadFlags(ref platform, node) & TNF_LIST) == 0)
			return false;
		if (!ApplyOpen(ref platform, state, obj, node, open, openParents))
			return false;
		Redraw(ref platform, state, obj, header);
		return true;
	}

	private static bool ApplyOpen<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR node, bool open, bool openParents)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var flags = ReadFlags(ref platform, node);
		var alreadyOpen = (flags & TNF_OPEN) != 0;
		if (open)
		{
			CallNodeHook(ref platform, state, obj, OpenHook, node); // before open
			SetFlagBits(ref platform, node, TNF_OPEN);
			if (openParents)
			{
				var p = ReadNodeParent(ref platform, node);
				uint guard = 0;
				while (p.IsNotNull && guard++ < MaximumDepth)
				{
					SetFlagBits(ref platform, p, TNF_OPEN);
					p = ReadNodeParent(ref platform, p);
				}
			}
			return !alreadyOpen || openParents;
		}
		ClearFlagBits(ref platform, node, TNF_OPEN);
		// When the active entry was a child of the closed node, the closed node
		// becomes active (autodoc).
		var active = ActiveNode(ref platform, state, obj);
		if (active.IsNotNull && IsAncestor(ref platform, node, active))
			SetActive(ref platform, state, obj, node, true);
		CallNodeHook(ref platform, state, obj, CloseHook, node); // after close
		return alreadyOpen;
	}

	// =========================================================================
	// Sort (one level)
	// =========================================================================

	public static bool Sort<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR listNode, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		if (!ResolveList(ref platform, state, obj, listNode, out var parent))
			return false;
		var hook = SortHookValue(ref platform, state, obj);
		// Head/Tail hooks do not define an alphabetical order: preserve order.
		if (hook == SortHookHead || hook == SortHookTail) return true;
		// Detach the whole child list, then re-insert each node in sorted order.
		var node = ListFirst(ref platform, header, parent);
		SetListFirst(ref platform, header, parent, APTR.Null);
		SetListLast(ref platform, header, parent, APTR.Null);
		SetListCount(ref platform, header, parent, 0);
		uint guard = 0;
		while (node.IsNotNull && guard++ < MaximumTraversal)
		{
			var next = ReadNodeNext(ref platform, node);
			WriteNodePointer(ref platform, node, APTR.Null, NodeField.Next);
			WriteNodePointer(ref platform, node, APTR.Null, NodeField.Previous);
			LinkSorted(ref platform, state, obj, header, parent, node);
			node = next;
		}
		Redraw(ref platform, state, obj, header);
		return true;
	}

	// =========================================================================
	// Move / Exchange
	// =========================================================================

	public static bool Move<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR oldListNode, APTR oldTreeNode, APTR newListNode,
		APTR newTreeNode, uint flags) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		if (!ResolveList(ref platform, state, obj, oldListNode, out var oldParent))
			return false;
		if (!ResolveList(ref platform, state, obj, newListNode, out var newParent))
			return false;
		var node = ResolveTreeNode(ref platform, state, obj, header, oldParent,
			oldTreeNode, flags);
		if (node.IsNull) return false;
		// A node can never be moved into itself or into its own subtree.
		if (newParent.IsNotNull && (newParent.Raw == node.Raw ||
			IsAncestor(ref platform, node, newParent))) return false;

		Unlink(ref platform, state, header, node);
		var sv = unchecked((int)newTreeNode.Raw);
		switch (sv)
		{
			case PrevNodeHead:
				LinkAsFirstChild(ref platform, header, newParent, node);
				break;
			case PrevNodeTail:
				LinkAsLastChild(ref platform, header, newParent, node);
				break;
			case NewTreeNodeSorted:
				LinkSorted(ref platform, state, obj, header, newParent, node);
				break;
			case PrevNodeActive:
			{
				var active = ActiveNode(ref platform, state, obj);
				if (active.IsNotNull && SameParent(ref platform, active, newParent))
					LinkAfter(ref platform, header, newParent, active, node);
				else LinkAsLastChild(ref platform, header, newParent, node);
				break;
			}
			default:
			{
				var anchor = APTR.FromPointer(newTreeNode.Raw);
				if (IsValidNode(ref platform, obj, anchor) &&
					SameParent(ref platform, anchor, newParent))
					LinkAfter(ref platform, header, newParent, anchor, node);
				else LinkAsLastChild(ref platform, header, newParent, node);
				break;
			}
		}
		if (newParent.IsNotNull) SetFlagBits(ref platform, newParent, TNF_LIST);
		Redraw(ref platform, state, obj, header);
		return true;
	}

	public static bool Exchange<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR listNode1, APTR treeNode1, APTR listNode2, APTR treeNode2,
		uint flags) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		if (!ResolveList(ref platform, state, obj, listNode1, out var parent1) ||
			!ResolveList(ref platform, state, obj, listNode2, out var parent2))
			return false;
		var n1 = ResolveTreeNode(ref platform, state, obj, header, parent1,
			treeNode1, flags);
		var n2 = ResolveTreeNode(ref platform, state, obj, header, parent2,
			treeNode2, flags);
		if (n1.IsNull || n2.IsNull || n1.Raw == n2.Raw) return false;
		if (IsAncestor(ref platform, n1, n2) || IsAncestor(ref platform, n2, n1))
			return false;
		var p1 = ReadNodeParent(ref platform, n1);
		var p2 = ReadNodeParent(ref platform, n2);
		var i1 = ChildIndexOf(ref platform, header, p1, n1);
		var i2 = ChildIndexOf(ref platform, header, p2, n2);
		Unlink(ref platform, state, header, n1);
		Unlink(ref platform, state, header, n2);
		if (p1.Raw == p2.Raw)
		{
			if (i1 <= i2)
			{
				InsertAtIndex(ref platform, header, p1, n2, i1);
				InsertAtIndex(ref platform, header, p1, n1, i2);
			}
			else
			{
				InsertAtIndex(ref platform, header, p1, n1, i2);
				InsertAtIndex(ref platform, header, p1, n2, i1);
			}
		}
		else
		{
			InsertAtIndex(ref platform, header, p1, n2, i1);
			InsertAtIndex(ref platform, header, p2, n1, i2);
		}
		Redraw(ref platform, state, obj, header);
		return true;
	}

	// =========================================================================
	// Rename
	// =========================================================================

	public static bool Rename<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR treeNode, APTR newName, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		var node = unchecked((int)treeNode.Raw) == TreeNodeActive
			? ActiveNode(ref platform, state, obj)
			: APTR.FromPointer(treeNode.Raw);
		if (node.IsNull || !IsValidNode(ref platform, obj, node)) return false;

		if ((flags & RenameFlagsUser) != 0)
		{
			// Rebuild tn_User through the construct/destruct hooks.
			var stored = ConstructUser(ref platform, state, obj, newName,
				out var userOwned);
			if (HasConstructHook(ref platform, state, obj) && stored.IsNull)
				return false;
			DestructUser(ref platform, state, obj,
				ReadNodeUser(ref platform, node),
				ReadNodeUserOwned(ref platform, node));
			WriteNodeUser(ref platform, node, stored);
		WriteNodeNumber(ref platform, node, userOwned, NodeField.UserOwned);
		}
		else
		{
			var duplicate = ReadPolicy(ref platform, state, obj, DuplicateNodeName, 1)
				!= 0;
			if (duplicate && newName.IsNotNull)
			{
				// Allocate the replacement before releasing the old buffer so a
				// failure leaves the node's original name intact.
				var copy = DuplicateString(ref platform, newName, out var size);
				if (copy.IsNull) return false;
				DestructOwnedName(ref platform, node);
				WriteNodeName(ref platform, node, copy);
			WriteNodeNumber(ref platform, node, 1, NodeField.NameOwned);
			WriteNodeNumber(ref platform, node, size, NodeField.NameSize);
			}
			else
			{
				DestructOwnedName(ref platform, node);
				WriteNodeName(ref platform, node, newName);
				WriteNodeNumber(ref platform, node, 0, NodeField.NameOwned);
				WriteNodeNumber(ref platform, node, 0, NodeField.NameSize);
			}
		}
		if ((flags & RenameFlagsNoRefresh) == 0)
			Redraw(ref platform, state, obj, header);
		return true;
	}

	// =========================================================================
	// FindName
	// =========================================================================

	// MUIM_Listtree_FindName: locate a node by name in the list of ListNode.
	// SameLevel restricts the search to that immediate list; otherwise the
	// search descends recursively (pre-order). Visible restricts to the display
	// list.
	public static APTR FindName<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR listNode, APTR name, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull || name.IsNull) return APTR.Null;
		if (!ResolveList(ref platform, state, obj, listNode, out var parent))
			return APTR.Null;
		var sameLevel = (flags & FindSameLevel) != 0;
		var visible = (flags & FlagsVisible) != 0;
		var child = ListFirst(ref platform, header, parent);
		return FindNameIn(ref platform, header, child, name, sameLevel, visible, 0);
	}

	private static APTR FindNameIn<TPlatform>(ref TPlatform platform, APTR header,
		APTR first, APTR name, bool sameLevel, bool visible, uint depth)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (depth > MaximumDepth) return APTR.Null;
		var node = first;
		uint guard = 0;
		while (node.IsNotNull && guard++ < MaximumTraversal)
		{
			var open = (ReadFlags(ref platform, node) & TNF_OPEN) != 0;
			if (!visible || open || depth == 0)
			{
				var candidate = ReadNodeName(ref platform, node);
				if (EqualStrings(ref platform, candidate, name)) return node;
			}
			if (!sameLevel && (!visible || open))
			{
				var found = FindNameIn(ref platform, header,
				ReadNodeFirstChild(ref platform, node),
					name, false, visible, depth + 1);
				if (found.IsNotNull) return found;
			}
			node = ReadNodeNext(ref platform, node);
		}
		return APTR.Null;
	}

	// =========================================================================
	// SetDropMark / TestPos
	// =========================================================================

	public static bool SetDropMark<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int entry, uint values) where TPlatform : struct,
		IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		return WriteHeaderDrop(ref platform, header, entry, values);
	}

	// MUIM_Listtree_TestPos. Without a real rendering surface, the Y coordinate
	// is interpreted as a display-list row (bounded); the result struct is filled
	// with the entry under the position and a documented drop-position flag.
	public static bool TestPos<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int x, int y, APTR result) where TPlatform : struct,
		IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull || result.IsNull ||
			!platform.IsMapped(result, MuiListtreeTestPosResult.Size))
			return false;
		var row = y < 0 ? -1 : y;
		var node = row < 0 ? APTR.Null
			: NthVisible(ref platform, header, (uint)row);
		var value = default(MuiListtreeTestPosResult);
		value.TreeNode = node;
		value.Flags = (ushort)(node.IsNull ? DropMarkNone : DropMarkOnto);
		value.ListEntry = node.IsNull ? -1 : unchecked((int)
			VisibleIndexOf(ref platform, header, node));
		value.ListFlags = 0;
		return MuiListtreeTestPosResultCodec.Write(ref platform, result, value);
	}

	// =========================================================================
	// Public query helpers (test/introspection)
	// =========================================================================

	public static APTR ActiveNode<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		APTR.FromPointer(ReadPolicy(ref platform, state, obj, Active, ActiveOff));

	public static uint RootCount<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		return header.IsNull ? 0 : ReadHeaderRootCount(ref platform, header);
	}

	public static uint TotalNodes<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		return header.IsNull ? 0 : ReadHeaderTotal(ref platform, header);
	}

	public static uint VisibleCount<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return 0;
		uint count = 0;
		var node = ListFirst(ref platform, header, APTR.Null);
		uint guard = 0;
		while (node.IsNotNull && guard++ < MaximumTraversal)
		{
			count++;
			node = PreorderNext(ref platform, header, node, true);
		}
		return count;
	}

	public static uint ChildCount<TPlatform>(ref TPlatform platform, APTR node)
		where TPlatform : struct, IMuiGuestMemory =>
			node.IsNull ? 0 : ReadNodeChildCount(ref platform, node);

	public static uint RedrawRequests<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		return header.IsNull ? 0 : ReadHeaderRedraw(ref platform, header);
	}

	public static uint NodeFlags<TPlatform>(ref TPlatform platform, APTR node)
		where TPlatform : struct, IMuiGuestMemory =>
		node.IsNull ? 0 : ReadFlags(ref platform, node);

	// =========================================================================
	// Argument resolution
	// =========================================================================

	// Resolve a "ListNode" argument to the parent whose child list it names.
	// Root(0) -> APTR.Null (the header root list); Active(-2) -> the active node;
	// otherwise a node pointer. Returns false for an invalid pointer.
	private static bool ResolveList<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR listNode, out APTR parent)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		parent = APTR.Null;
		var sv = unchecked((int)listNode.Raw);
		if (sv == ListNodeRoot) return true;
		if (sv == ListNodeActive)
		{
			parent = ActiveNode(ref platform, state, obj);
			return true; // a Null active resolves to the root list
		}
		if (!IsValidNode(ref platform, obj, listNode)) return false;
		parent = listNode;
		return true;
	}

	// Resolve a node argument for node-relative navigation (Parent/Next/Prev).
	private static APTR ResolveNodeArgument<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR node) where TPlatform : struct,
		IMuiHeadlessPlatform
	{
		var sv = unchecked((int)node.Raw);
		if (sv == ListNodeActive) return ActiveNode(ref platform, state, obj);
		if (IsValidNode(ref platform, obj, node)) return node;
		return APTR.Null;
	}

	private static APTR ResolveTreeNode<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR header, APTR parent, APTR treeNode, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sv = unchecked((int)treeNode.Raw);
		if ((flags & FlagsNr) != 0 && sv >= 0)
			return (flags & FlagsVisible) != 0
				? NthVisibleChild(ref platform, header, parent, (uint)sv)
				: NthChild(ref platform, header, parent, (uint)sv);
		switch (sv)
		{
			case TreeNodeHead: return ListFirst(ref platform, header, parent);
			case TreeNodeTail: return ListLast(ref platform, header, parent);
			case TreeNodeActive: return ActiveNode(ref platform, state, obj);
			default:
				if (IsValidNode(ref platform, obj, treeNode)) return treeNode;
				return APTR.Null;
		}
	}

	// =========================================================================
	// Linked-list primitives (root list lives in the header)
	// =========================================================================

	private static APTR ListFirst<TPlatform>(ref TPlatform platform, APTR header,
		APTR parent) where TPlatform : struct, IMuiGuestMemory =>
		APTR.FromPointer(parent.IsNull
			? ReadHeaderRootFirst(ref platform, header)
			: ReadNodeFirstChild(ref platform, parent));

	private static APTR ListLast<TPlatform>(ref TPlatform platform, APTR header,
		APTR parent) where TPlatform : struct, IMuiGuestMemory =>
		APTR.FromPointer(parent.IsNull
			? ReadHeaderRootLast(ref platform, header)
			: ReadNodeLastChild(ref platform, parent));

	private static uint ListCount<TPlatform>(ref TPlatform platform, APTR header,
		APTR parent) where TPlatform : struct, IMuiGuestMemory =>
		parent.IsNull ? ReadHeaderRootCount(ref platform, header)
			: ReadNodeChildCount(ref platform, parent);

	private static void SetListFirst<TPlatform>(ref TPlatform platform, APTR header,
		APTR parent, APTR value) where TPlatform : struct, IMuiGuestMemory
	{
		if (parent.IsNull) WriteHeaderRootFirst(ref platform, header, value);
		else WriteNodePointer(ref platform, parent, value, NodeField.FirstChild);
	}

	private static void SetListLast<TPlatform>(ref TPlatform platform, APTR header,
		APTR parent, APTR value) where TPlatform : struct, IMuiGuestMemory
	{
		if (parent.IsNull) WriteHeaderRootLast(ref platform, header, value);
		else WriteNodePointer(ref platform, parent, value, NodeField.LastChild);
	}

	private static void SetListCount<TPlatform>(ref TPlatform platform, APTR header,
		APTR parent, uint value) where TPlatform : struct, IMuiGuestMemory
	{
		if (parent.IsNull) WriteHeaderRootCount(ref platform, header, value);
		else WriteNodeNumber(ref platform, parent, value, NodeField.ChildCount);
	}

	private static void LinkAsFirstChild<TPlatform>(ref TPlatform platform,
		APTR header, APTR parent, APTR node) where TPlatform : struct,
		IMuiGuestMemory
	{
		var first = ListFirst(ref platform, header, parent);
		WriteNodePointer(ref platform, node, parent, NodeField.Parent);
		WriteNodePointer(ref platform, node, APTR.Null, NodeField.Previous);
		WriteNodePointer(ref platform, node, first, NodeField.Next);
		if (first.IsNull) SetListLast(ref platform, header, parent, node);
		else WriteNodePointer(ref platform, first, node, NodeField.Previous);
		SetListFirst(ref platform, header, parent, node);
		SetListCount(ref platform, header, parent,
			ListCount(ref platform, header, parent) + 1);
	}

	private static void LinkAsLastChild<TPlatform>(ref TPlatform platform,
		APTR header, APTR parent, APTR node) where TPlatform : struct,
		IMuiGuestMemory
	{
		var last = ListLast(ref platform, header, parent);
		WriteNodePointer(ref platform, node, parent, NodeField.Parent);
		WriteNodePointer(ref platform, node, APTR.Null, NodeField.Next);
		WriteNodePointer(ref platform, node, last, NodeField.Previous);
		if (last.IsNull) SetListFirst(ref platform, header, parent, node);
		else WriteNodePointer(ref platform, last, node, NodeField.Next);
		SetListLast(ref platform, header, parent, node);
		SetListCount(ref platform, header, parent,
			ListCount(ref platform, header, parent) + 1);
	}

	private static void LinkAfter<TPlatform>(ref TPlatform platform, APTR header,
		APTR parent, APTR anchor, APTR node) where TPlatform : struct,
		IMuiGuestMemory
	{
		var next = ReadNodeNext(ref platform, anchor);
		WriteNodePointer(ref platform, node, parent, NodeField.Parent);
		WriteNodePointer(ref platform, node, anchor, NodeField.Previous);
		WriteNodePointer(ref platform, node, next, NodeField.Next);
		WriteNodePointer(ref platform, anchor, node, NodeField.Next);
		if (next.IsNull) SetListLast(ref platform, header, parent, node);
		else WriteNodePointer(ref platform, next, node, NodeField.Previous);
		SetListCount(ref platform, header, parent,
			ListCount(ref platform, header, parent) + 1);
	}

	private static void LinkBefore<TPlatform>(ref TPlatform platform, APTR header,
		APTR parent, APTR anchor, APTR node) where TPlatform : struct,
		IMuiGuestMemory
	{
		var prev = ReadNodePrevious(ref platform, anchor);
		WriteNodePointer(ref platform, node, parent, NodeField.Parent);
		WriteNodePointer(ref platform, node, anchor, NodeField.Next);
		WriteNodePointer(ref platform, node, prev, NodeField.Previous);
		WriteNodePointer(ref platform, anchor, node, NodeField.Previous);
		if (prev.IsNull) SetListFirst(ref platform, header, parent, node);
		else WriteNodePointer(ref platform, prev, node, NodeField.Next);
		SetListCount(ref platform, header, parent,
			ListCount(ref platform, header, parent) + 1);
	}

	private static void InsertAtIndex<TPlatform>(ref TPlatform platform, APTR header,
		APTR parent, APTR node, uint index) where TPlatform : struct,
		IMuiGuestMemory
	{
		var anchor = NthChild(ref platform, header, parent, index);
		if (anchor.IsNull) LinkAsLastChild(ref platform, header, parent, node);
		else LinkBefore(ref platform, header, parent, anchor, node);
	}

	private static void Unlink<TPlatform>(ref TPlatform platform, APTR state,
		APTR header, APTR node) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var parent = ReadNodeParent(ref platform, node);
		var prev = ReadNodePrevious(ref platform, node);
		var next = ReadNodeNext(ref platform, node);
		if (prev.IsNull) SetListFirst(ref platform, header, parent, next);
		else WriteNodePointer(ref platform, prev, next, NodeField.Next);
		if (next.IsNull) SetListLast(ref platform, header, parent, prev);
		else WriteNodePointer(ref platform, next, prev, NodeField.Previous);
		var count = ListCount(ref platform, header, parent);
		if (count != 0) SetListCount(ref platform, header, parent, count - 1);
		WriteNodePointer(ref platform, node, APTR.Null, NodeField.Parent);
		WriteNodePointer(ref platform, node, APTR.Null, NodeField.Previous);
		WriteNodePointer(ref platform, node, APTR.Null, NodeField.Next);
	}

	private static APTR NthChild<TPlatform>(ref TPlatform platform, APTR header,
		APTR parent, uint index) where TPlatform : struct, IMuiGuestMemory
	{
		var node = ListFirst(ref platform, header, parent);
		uint i = 0;
		uint guard = 0;
		while (node.IsNotNull && guard++ < MaximumTraversal)
		{
			if (i == index) return node;
			i++;
			node = ReadNodeNext(ref platform, node);
		}
		return APTR.Null;
	}

	private static APTR NthVisibleChild<TPlatform>(ref TPlatform platform,
		APTR header, APTR parent, uint index) where TPlatform : struct,
		IMuiGuestMemory
	{
		// Visible children of a list: the top-level entries are always visible;
		// deeper lists count only when their owner is open. This walks the list's
		// own entries in order.
		var node = ListFirst(ref platform, header, parent);
		uint i = 0;
		uint guard = 0;
		while (node.IsNotNull && guard++ < MaximumTraversal)
		{
			if (i == index) return node;
			i++;
			node = ReadNodeNext(ref platform, node);
		}
		return APTR.Null;
	}

	private static uint ChildIndexOf<TPlatform>(ref TPlatform platform, APTR header,
		APTR parent, APTR target) where TPlatform : struct, IMuiGuestMemory
	{
		var node = ListFirst(ref platform, header, parent);
		uint i = 0;
		uint guard = 0;
		while (node.IsNotNull && guard++ < MaximumTraversal)
		{
			if (node.Raw == target.Raw) return i;
			i++;
			node = ReadNodeNext(ref platform, node);
		}
		return 0;
	}

	// =========================================================================
	// Display-list (visible) traversal
	// =========================================================================

	private static APTR PreorderNext<TPlatform>(ref TPlatform platform, APTR header,
		APTR node, bool visible) where TPlatform : struct, IMuiGuestMemory
	{
		var flags = ReadFlags(ref platform, node);
		var descend = !visible || (flags & TNF_OPEN) != 0;
		if (descend)
		{
			var child = ReadNodeFirstChild(ref platform, node);
			if (child.IsNotNull) return child;
		}
		var current = node;
		uint guard = 0;
		while (current.IsNotNull && guard++ < MaximumDepth)
		{
			var next = ReadNodeNext(ref platform, current);
			if (next.IsNotNull) return next;
			current = ReadNodeParent(ref platform, current);
		}
		return APTR.Null;
	}

	private static APTR PreorderPrevious<TPlatform>(ref TPlatform platform,
		APTR header, APTR node, bool visible) where TPlatform : struct,
		IMuiGuestMemory
	{
		var prev = ReadNodePrevious(ref platform, node);
		if (prev.IsNull)
			return ReadNodeParent(ref platform, node);
		// Descend to the deepest last-open descendant of the previous sibling.
		var current = prev;
		uint guard = 0;
		while (guard++ < MaximumDepth)
		{
			var flags = ReadFlags(ref platform, current);
			var descend = !visible || (flags & TNF_OPEN) != 0;
			var last = ReadNodeLastChild(ref platform, current);
			if (!descend || last.IsNull) return current;
			current = last;
		}
		return current;
	}

	private static APTR NthVisible<TPlatform>(ref TPlatform platform, APTR header,
		uint index) where TPlatform : struct, IMuiGuestMemory
	{
		var node = ListFirst(ref platform, header, APTR.Null);
		uint i = 0;
		uint guard = 0;
		while (node.IsNotNull && guard++ < MaximumTraversal)
		{
			if (i == index) return node;
			i++;
			node = PreorderNext(ref platform, header, node, true);
		}
		return APTR.Null;
	}

	private static uint VisibleIndexOf<TPlatform>(ref TPlatform platform,
		APTR header, APTR target) where TPlatform : struct, IMuiGuestMemory
	{
		var node = ListFirst(ref platform, header, APTR.Null);
		uint i = 0;
		uint guard = 0;
		while (node.IsNotNull && guard++ < MaximumTraversal)
		{
			if (node.Raw == target.Raw) return i;
			i++;
			node = PreorderNext(ref platform, header, node, true);
		}
		return 0xFFFFFFFFu;
	}

	// =========================================================================
	// Sort comparison
	// =========================================================================

	private static uint SortHookValue<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		ReadPolicy(ref platform, state, obj, SortHook, SortHookLeavesBottom);

	private static int Compare<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR a, APTR b) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var hook = SortHookValue(ref platform, state, obj);
		switch (hook)
		{
			case SortHookHead:
			case SortHookTail:
				return 0;
			case SortHookLeavesMixed:
				return CompareNames(ref platform, a, b);
			case SortHookLeavesTop:
			{
				var la = IsLeaf(ref platform, a);
				var lb = IsLeaf(ref platform, b);
				if (la != lb) return la ? -1 : 1;
				return CompareNames(ref platform, a, b);
			}
			case SortHookLeavesBottom:
			{
				var la = IsLeaf(ref platform, a);
				var lb = IsLeaf(ref platform, b);
				if (la != lb) return la ? 1 : -1;
				return CompareNames(ref platform, a, b);
			}
			default:
			{
				// Arbitrary sort hook. A0 = hook base (so h_Data is reachable),
				// A2 = node a, A1 = node b.
				return unchecked((int)platform.InvokeHook(APTR.FromPointer(hook), a,
					b));
			}
		}
	}

	private static bool IsLeaf<TPlatform>(ref TPlatform platform, APTR node)
		where TPlatform : struct, IMuiGuestMemory =>
		(ReadFlags(ref platform, node) & TNF_LIST) == 0;

	private static int CompareNames<TPlatform>(ref TPlatform platform, APTR a,
		APTR b) where TPlatform : struct, IMuiGuestMemory =>
		CompareStrings(ref platform,
			ReadNodeName(ref platform, a), ReadNodeName(ref platform, b));

	// =========================================================================
	// Construct / destruct / hook seams
	// =========================================================================

	private static bool HasConstructHook<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		ReadPolicy(ref platform, state, obj, ConstructHook, 0) != 0;

	private static APTR ConstructUser<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR user, out uint ownership)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		ownership = 0;
		var hook = ReadPolicy(ref platform, state, obj, ConstructHook, 0);
		if (hook == 0) return user; // pointer used directly
		if (hook == ConstructHookString)
		{
			if (user.IsNull) return APTR.Null;
			var dup = DuplicateString(ref platform, user, out _);
			ownership = dup.IsNotNull ? 1u : 0u;
			return dup;
		}
		// Arbitrary construct hook: A0 = hook base, A2 = NULL, A1 = user data.
		return APTR.FromPointer(platform.InvokeHook(APTR.FromPointer(hook),
			APTR.Null, user));
	}

	private static void DestructUser<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR user, uint ownership)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (ownership == 1)
		{
			FreeOwnedString(ref platform, user);
			return;
		}
		var hook = ReadPolicy(ref platform, state, obj, DestructHook, 0);
		if (hook == 0 || hook == ConstructHookString || user.IsNull) return;
		// Arbitrary destruct hook: A0 = hook base, A2 = NULL, A1 = user data.
		platform.InvokeHook(APTR.FromPointer(hook), APTR.Null, user);
	}

	private static void CallNodeHook<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint hookAttribute, APTR node)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var hook = ReadPolicy(ref platform, state, obj, hookAttribute, 0);
		if (hook == 0) return;
		// Node hooks (display/open/close): A0 = hook base, A2 = object, A1 = node.
		platform.InvokeHook(APTR.FromPointer(hook), obj, node);
	}

	// =========================================================================
	// Node lifetime
	// =========================================================================

	private static void FreeSubtree<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR node, uint depth) where TPlatform : struct,
		IMuiHeadlessPlatform
	{
		if (node.IsNull || depth > MaximumDepth) return;
			var child = ReadNodeFirstChild(ref platform, node);
		uint guard = 0;
		while (child.IsNotNull && guard++ < MaximumTraversal)
		{
				var next = ReadNodeNext(ref platform, child);
			FreeSubtree(ref platform, state, obj, child, depth + 1);
			child = next;
		}
		DestructOwnedName(ref platform, node);
		DestructUser(ref platform, state, obj,
			ReadNodeUser(ref platform, node),
				ReadNodeUserOwned(ref platform, node));
		var header = Header(ref platform, state, obj);
		if (header.IsNotNull)
		{
			var total = ReadHeaderTotal(ref platform, header);
			if (total != 0) WriteHeaderTotal(ref platform, header, total - 1);
		}
		FreeNodeRecord(ref platform, node);
	}

	private static void FreeNodeRecord<TPlatform>(ref TPlatform platform, APTR node)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		platform.Clear(node, NodeSize);
		platform.Free(node, NodeSize);
	}

	private static void DestructOwnedName<TPlatform>(ref TPlatform platform,
		APTR node) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (ReadNodeNameOwned(ref platform, node) == 0) return;
		var name = ReadNodeName(ref platform, node);
		var size = ReadNodeNameSize(ref platform, node);
		if (name.IsNotNull && size != 0 && platform.IsMapped(name, size))
		{
			platform.Clear(name, size);
			platform.Free(name, size);
		}
		WriteNodeName(ref platform, node, APTR.Null);
				WriteNodeNumber(ref platform, node, 0, NodeField.NameOwned);
				WriteNodeNumber(ref platform, node, 0, NodeField.NameSize);
	}

	// =========================================================================
	// State / active
	// =========================================================================

	private static bool SetActive<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR node, bool notify) where TPlatform : struct,
		IMuiHeadlessPlatform
	{
		var value = node.Raw;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, Active,
			out var current) && current == value) return true;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, Active,
			value, notify)) return false;
		return SyncPolicyStateRecord(ref platform, state, obj);
	}

	private static void Redraw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR header) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (ReadPolicy(ref platform, state, obj, Quiet, 0) != 0)
		{
			WriteHeaderDirty(ref platform, header, 1);
			return;
		}
		WriteHeaderRedraw(ref platform, header,
			ReadHeaderRedraw(ref platform, header) + 1);
	}

	private static APTR ReadHeaderRootFirst<TPlatform>(ref TPlatform platform,
		APTR header) where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeHeaderCodec.TryRead(ref platform, header, out var value)
			? value.RootFirst : APTR.Null;

	private static APTR ReadHeaderRootLast<TPlatform>(ref TPlatform platform,
		APTR header) where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeHeaderCodec.TryRead(ref platform, header, out var value)
			? value.RootLast : APTR.Null;

	private static uint ReadHeaderRootCount<TPlatform>(ref TPlatform platform,
		APTR header) where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeHeaderCodec.TryRead(ref platform, header, out var value)
			? value.RootCount : 0;

	private static uint ReadHeaderTotal<TPlatform>(ref TPlatform platform,
		APTR header) where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeHeaderCodec.TryRead(ref platform, header, out var value)
			? value.Total : 0;

	private static uint ReadHeaderRedraw<TPlatform>(ref TPlatform platform,
		APTR header) where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeHeaderCodec.TryRead(ref platform, header, out var value)
			? value.Redraw : 0;

	private static uint ReadHeaderDirty<TPlatform>(ref TPlatform platform,
		APTR header) where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeHeaderCodec.TryRead(ref platform, header, out var value)
			? value.Dirty : 0;

	private static bool WriteHeaderRootFirst<TPlatform>(ref TPlatform platform,
		APTR header, APTR value) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListtreeHeaderCodec.TryRead(ref platform, header, out var state))
			return false;
		state.RootFirst = value;
		return MuiListtreeHeaderCodec.Write(ref platform, header, state);
	}

	private static bool WriteHeaderRootLast<TPlatform>(ref TPlatform platform,
		APTR header, APTR value) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListtreeHeaderCodec.TryRead(ref platform, header, out var state))
			return false;
		state.RootLast = value;
		return MuiListtreeHeaderCodec.Write(ref platform, header, state);
	}

	private static bool WriteHeaderRootCount<TPlatform>(ref TPlatform platform,
		APTR header, uint value) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListtreeHeaderCodec.TryRead(ref platform, header, out var state))
			return false;
		state.RootCount = value;
		return MuiListtreeHeaderCodec.Write(ref platform, header, state);
	}

	private static bool WriteHeaderTotal<TPlatform>(ref TPlatform platform,
		APTR header, uint value) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListtreeHeaderCodec.TryRead(ref platform, header, out var state))
			return false;
		state.Total = value;
		return MuiListtreeHeaderCodec.Write(ref platform, header, state);
	}

	private static bool WriteHeaderRedraw<TPlatform>(ref TPlatform platform,
		APTR header, uint value) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListtreeHeaderCodec.TryRead(ref platform, header, out var state))
			return false;
		state.Redraw = value;
		return MuiListtreeHeaderCodec.Write(ref platform, header, state);
	}

	private static bool WriteHeaderDirty<TPlatform>(ref TPlatform platform,
		APTR header, uint value) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListtreeHeaderCodec.TryRead(ref platform, header, out var state))
			return false;
		state.Dirty = value;
		return MuiListtreeHeaderCodec.Write(ref platform, header, state);
	}

	private static bool WriteHeaderDrop<TPlatform>(ref TPlatform platform,
		APTR header, int entry, uint value) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListtreeHeaderCodec.TryRead(ref platform, header, out var state))
			return false;
		state.DropEntry = entry;
		state.DropValue = value;
		return MuiListtreeHeaderCodec.Write(ref platform, header, state);
	}

	// =========================================================================
	// Node helpers
	// =========================================================================

	private static APTR Header<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			TreeHeaderKey, out var value) || value == 0) return APTR.Null;
		var header = APTR.FromPointer(value);
		if (!MuiListtreeHeaderCodec.TryRead(ref platform, header, out _))
			return APTR.Null;
		return header;
	}

	private static APTR ReadNodeName<TPlatform>(ref TPlatform platform, APTR node)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeNodePublicCodec.TryRead(ref platform, node, out var value)
			? value.Name : APTR.Null;

	private static APTR ReadNodeUser<TPlatform>(ref TPlatform platform, APTR node)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeNodePublicCodec.TryRead(ref platform, node, out var value)
			? value.User : APTR.Null;

	private static bool WriteNodeName<TPlatform>(ref TPlatform platform,
		APTR node, APTR value) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListtreeNodePublicCodec.TryRead(ref platform, node, out var state))
			return false;
		state.Name = value;
		return MuiListtreeNodePublicCodec.Write(ref platform, node, state);
	}

	private static bool WriteNodeUser<TPlatform>(ref TPlatform platform,
		APTR node, APTR value) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListtreeNodePublicCodec.TryRead(ref platform, node, out var state))
			return false;
		state.User = value;
		return MuiListtreeNodePublicCodec.Write(ref platform, node, state);
	}

	private static APTR ReadNodeParent<TPlatform>(ref TPlatform platform,
		APTR node) where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeNodeCodec.TryRead(ref platform, node, out var value)
			? value.Parent : APTR.Null;

	private static APTR ReadNodeFirstChild<TPlatform>(ref TPlatform platform,
		APTR node) where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeNodeCodec.TryRead(ref platform, node, out var value)
			? value.FirstChild : APTR.Null;

	private static APTR ReadNodeLastChild<TPlatform>(ref TPlatform platform,
		APTR node) where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeNodeCodec.TryRead(ref platform, node, out var value)
			? value.LastChild : APTR.Null;

	private static APTR ReadNodeNext<TPlatform>(ref TPlatform platform, APTR node)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeNodeCodec.TryRead(ref platform, node, out var value)
			? value.Next : APTR.Null;

	private static APTR ReadNodePrevious<TPlatform>(ref TPlatform platform,
		APTR node) where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeNodeCodec.TryRead(ref platform, node, out var value)
			? value.Previous : APTR.Null;

	private static uint ReadNodeChildCount<TPlatform>(ref TPlatform platform,
		APTR node) where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeNodeCodec.TryRead(ref platform, node, out var value)
			? value.ChildCount : 0;

	private static uint ReadNodeNameOwned<TPlatform>(ref TPlatform platform,
		APTR node) where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeNodeCodec.TryRead(ref platform, node, out var value)
			? value.NameOwned : 0;

	private static uint ReadNodeNameSize<TPlatform>(ref TPlatform platform,
		APTR node) where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeNodeCodec.TryRead(ref platform, node, out var value)
			? value.NameSize : 0;

	private static uint ReadNodeUserOwned<TPlatform>(ref TPlatform platform,
		APTR node) where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeNodeCodec.TryRead(ref platform, node, out var value)
			? value.UserOwned : 0;

	private enum NodeField : byte
	{
		Parent,
		FirstChild,
		LastChild,
		Next,
		Previous,
		ChildCount,
		NameOwned,
		NameSize,
		UserOwned,
	}

	private static bool UpdateNodeState<TPlatform>(ref TPlatform platform,
		APTR node, APTR pointer, NodeField field)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListtreeNodeCodec.TryRead(ref platform, node, out var value))
			return false;
		switch (field)
		{
			case NodeField.Parent: value.Parent = pointer; break;
			case NodeField.FirstChild: value.FirstChild = pointer; break;
			case NodeField.LastChild: value.LastChild = pointer; break;
			case NodeField.Next: value.Next = pointer; break;
			case NodeField.Previous: value.Previous = pointer; break;
			default: return false;
		}
		return MuiListtreeNodeCodec.Write(ref platform, node, value);
	}

	private static bool UpdateNodeState<TPlatform>(ref TPlatform platform,
		APTR node, uint number, NodeField field)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListtreeNodeCodec.TryRead(ref platform, node, out var value))
			return false;
		switch (field)
		{
			case NodeField.ChildCount: value.ChildCount = number; break;
			case NodeField.NameOwned: value.NameOwned = number; break;
			case NodeField.NameSize: value.NameSize = number; break;
			case NodeField.UserOwned: value.UserOwned = number; break;
			default: return false;
		}
		return MuiListtreeNodeCodec.Write(ref platform, node, value);
	}

	private static bool WriteNodePointer<TPlatform>(ref TPlatform platform,
		APTR node, APTR value, NodeField field)
		where TPlatform : struct, IMuiGuestMemory =>
		UpdateNodeState(ref platform, node, value, field);

	private static bool WriteNodeNumber<TPlatform>(ref TPlatform platform,
		APTR node, uint value, NodeField field)
		where TPlatform : struct, IMuiGuestMemory =>
		UpdateNodeState(ref platform, node, value, field);

	private static bool IsValidNode<TPlatform>(ref TPlatform platform, APTR obj,
		APTR node) where TPlatform : struct, IMuiGuestMemory =>
		node.IsNotNull && platform.IsMapped(node, NodeSize) &&
		MuiListtreeNodePublicCodec.TryRead(ref platform, node, out var value) &&
		value.Private2.Raw == obj.Raw;

	private static bool SameParent<TPlatform>(ref TPlatform platform, APTR node,
		APTR parent) where TPlatform : struct, IMuiGuestMemory =>
		ReadNodeParent(ref platform, node).Raw == parent.Raw;

	private static bool IsAncestor<TPlatform>(ref TPlatform platform, APTR ancestor,
		APTR node) where TPlatform : struct, IMuiGuestMemory
	{
		var p = ReadNodeParent(ref platform, node);
		uint guard = 0;
		while (p.IsNotNull && guard++ < MaximumDepth)
		{
			if (p.Raw == ancestor.Raw) return true;
			p = ReadNodeParent(ref platform, p);
		}
		return false;
	}

	private static uint ReadFlags<TPlatform>(ref TPlatform platform, APTR node)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreeNodePublicCodec.TryRead(ref platform, node, out var value)
			? value.Flags : 0u;

	private static void SetFlagBits<TPlatform>(ref TPlatform platform, APTR node,
		uint bits) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListtreeNodePublicCodec.TryRead(ref platform, node, out var value))
			return;
		value.Flags = (ushort)(value.Flags | bits);
		MuiListtreeNodePublicCodec.Write(ref platform, node, value);
	}

	private static void ClearFlagBits<TPlatform>(ref TPlatform platform, APTR node,
		uint bits) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListtreeNodePublicCodec.TryRead(ref platform, node, out var value))
			return;
		value.Flags = (ushort)(value.Flags & ~bits);
		MuiListtreeNodePublicCodec.Write(ref platform, node, value);
	}

	// =========================================================================
	// Strings
	// =========================================================================

	private static APTR DuplicateString<TPlatform>(ref TPlatform platform,
		APTR source, out uint size) where TPlatform : struct, IMuiHeadlessPlatform
	{
		size = 0;
		if (!TryReadCStringLength(ref platform, source, MaximumStringLength,
			out var length)) return APTR.Null;
		var bytes = length + 1;
		var copy = MuiHeadlessMemory.Allocate(ref platform, bytes);
		if (copy.IsNull) return APTR.Null;
		platform.Copy(source, copy, bytes);
		size = bytes;
		return copy;
	}

	private static void FreeOwnedString<TPlatform>(ref TPlatform platform,
		APTR entry) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (entry.IsNull) return;
		if (!TryReadCStringLength(ref platform, entry, MaximumStringLength,
			out var length)) return;
		var bytes = length + 1;
		platform.Clear(entry, bytes);
		platform.Free(entry, bytes);
	}

	private static bool TryReadCStringLength<TPlatform>(ref TPlatform platform,
		APTR value, uint maximumLength, out uint length)
		where TPlatform : struct, IMuiGuestMemory
	{
		length = 0;
		if (value.IsNull) return false;
		for (var index = 0u; index < maximumLength; index++)
		{
			if (value.Raw > uint.MaxValue - index) return false;
			var address = APTR.FromPointer(value.Raw + index);
			if (!platform.IsMapped(address, 1)) return false;
			if (platform.ReadUInt8(address, 0) != 0) continue;
			length = index;
			return true;
		}
		return false;
	}

	private static bool EqualStrings<TPlatform>(ref TPlatform platform, APTR a,
		APTR b) where TPlatform : struct, IMuiGuestMemory =>
		CompareStrings(ref platform, a, b) == 0;

	private static int CompareStrings<TPlatform>(ref TPlatform platform, APTR left,
		APTR right) where TPlatform : struct, IMuiGuestMemory
	{
		if (left.Raw == right.Raw) return 0;
		if (left.IsNull) return right.IsNull ? 0 : -1;
		if (right.IsNull) return 1;
		for (var i = 0u; i < MaximumStringLength; i++)
		{
			var la = APTR.FromPointer(left.Raw + i);
			var ra = APTR.FromPointer(right.Raw + i);
			if (!platform.IsMapped(la, 1) || !platform.IsMapped(ra, 1)) return 0;
			var lb = platform.ReadUInt8(la, 0);
			var rb = platform.ReadUInt8(ra, 0);
			if (lb != rb) return lb < rb ? -1 : 1;
			if (lb == 0) return 0;
		}
		return 0;
	}

	// =========================================================================
	// Generic attribute helpers
	// =========================================================================

	private static uint ReadRaw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint fallback)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, attribute,
			out var value) ? value : fallback;

	private static uint Read<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint fallback)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadPolicyValue(ref platform, state, obj, attribute, out var value)
			? value : ReadRaw(ref platform, state, obj, attribute, fallback);

	private static uint ReadPolicy<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint fallback)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Read(ref platform, state, obj, attribute, fallback);

	private static void EnsureDefault<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, attribute,
			out _))
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
				value, false);
	}
}
