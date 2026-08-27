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
		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeHeaderState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeHeaderState.Size) ||
				!MuiListtreeHeaderFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeHeaderField.Magic, out var magic) ||
				!MuiListtreeHeaderFieldCursorCodec.TryReadUInt32(ref platform,
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
			value.Magic = magic;
			value.RootFirst = APTR.FromPointer(rootFirst);
			value.RootLast = APTR.FromPointer(rootLast);
			value.DropEntry = unchecked((int)dropEntry);
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeHeaderState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, address, out value) &&
			value.Magic == MuiListtreeHeaderState.Cookie;

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

	// One entry in the temporary MorphOS DisplayHook column vector.  The hook
	// ABI describes A2 as a pointer to an array of STRPTR values; representing
	// each slot as a named guest record keeps the production path typed while
	// retaining the wire-compatible four-byte pointer layout.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeDisplayColumnRecord
	{
		internal const uint Size = 4;

		internal APTR Text;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeDisplayColumnCursor
	{
		internal const uint EntrySize = MuiListtreeDisplayColumnRecord.Size;
		internal const uint MaximumEntries = 256;

		internal APTR Base;
		internal uint Index;
	}

	internal static class MuiListtreeDisplayColumnCursorCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			MuiListtreeDisplayColumnCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (cursor.Base.IsNull || cursor.Index >=
				MuiListtreeDisplayColumnCursor.MaximumEntries || cursor.Index >
				(uint.MaxValue - cursor.Base.Raw) /
				MuiListtreeDisplayColumnCursor.EntrySize) return false;
			var offset = cursor.Index *
				MuiListtreeDisplayColumnCursor.EntrySize;
			if (cursor.Base.Raw > uint.MaxValue - offset) return false;
			address = APTR.FromPointer(cursor.Base.Raw + offset);
			return platform.IsMapped(address,
				MuiListtreeDisplayColumnCursor.EntrySize);
		}
	}

	internal static class MuiListtreeDisplayColumnCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeDisplayColumnRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeDisplayColumnRecord.Size)) return false;
			value.Text = APTR.FromPointer(platform.ReadUInt32(address, 0));
			return true;
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeDisplayColumnRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeDisplayColumnRecord.Size)) return false;
			platform.WriteUInt32(address, 0, value.Text.Raw);
			return true;
		}
	}

	// One derived FORMAT column.  The record is guest-resident only for the
	// duration of a pointer hit-test: it keeps the parser and geometry path
	// freestanding while still giving every value a named field.  DELTA is the
	// inter-column gap and WEIGHT participates in the remaining-width split.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeColumnGeometryRecord
	{
		internal const uint Size = 24;
		internal const uint MinPixel = 1;
		internal const uint MaxPixel = 2;
		internal const uint MinContent = 4;
		internal const uint MaxContent = 8;

		internal uint Width;
		internal uint Delta;
		internal uint Weight;
		internal uint MinWidth;
		internal uint MaxWidth;
		internal uint Flags;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeColumnGeometryCursor
	{
		internal const uint EntrySize = MuiListtreeColumnGeometryRecord.Size;
		internal const uint MaximumEntries = MaximumFormatColumns;

		internal APTR Base;
		internal uint Index;
	}

	internal static class MuiListtreeColumnGeometryCursorCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			MuiListtreeColumnGeometryCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (cursor.Base.IsNull || cursor.Index >=
				MuiListtreeColumnGeometryCursor.MaximumEntries || cursor.Index >
				(uint.MaxValue - cursor.Base.Raw) /
				MuiListtreeColumnGeometryCursor.EntrySize) return false;
			var offset = cursor.Index *
				MuiListtreeColumnGeometryCursor.EntrySize;
			if (cursor.Base.Raw > uint.MaxValue - offset) return false;
			address = APTR.FromPointer(cursor.Base.Raw + offset);
			return platform.IsMapped(address,
				MuiListtreeColumnGeometryCursor.EntrySize);
		}
	}

	internal static class MuiListtreeColumnGeometryCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeColumnGeometryRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeColumnGeometryRecord.Size)) return false;
			// These three reads are the one fixed wire boundary for the packed
			// guest vector; callers consume the named record fields below.
			value.Width = platform.ReadUInt32(address, 0);
			value.Delta = platform.ReadUInt32(address, 4);
			value.Weight = platform.ReadUInt32(address, 8);
			value.MinWidth = platform.ReadUInt32(address, 12);
			value.MaxWidth = platform.ReadUInt32(address, 16);
			value.Flags = platform.ReadUInt32(address, 20);
			return true;
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeColumnGeometryRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeColumnGeometryRecord.Size)) return false;
			platform.WriteUInt32(address, 0, value.Width);
			platform.WriteUInt32(address, 4, value.Delta);
			platform.WriteUInt32(address, 8, value.Weight);
			platform.WriteUInt32(address, 12, value.MinWidth);
			platform.WriteUInt32(address, 16, value.MaxWidth);
			platform.WriteUInt32(address, 20, value.Flags);
			return true;
		}
	}

	// The neutral Draw seam retains the most recently hook-produced row in a
	// guest snapshot.  The snapshot owns only the temporary pointer vector; the
	// strings remain caller-owned pointers returned by DisplayHook.  Retaining
	// the vector until the next draw/cleanup lets a later typed renderer or
	// qualification probe consume the hook's result without introducing a
	// managed row object or array.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeDisplaySnapshotState
	{
		internal const uint Size = 24;
		internal const uint Cookie = 0x4C545653u; // 'LTVS'
		internal const uint DisplayListNode = 1u << 0;
		internal const uint DisplayIndicator = 1u << 1;
		internal const uint DisplayOpen = 1u << 2;
		internal const uint DisplayFrozen = 1u << 3;
		internal const uint DisplayTitle = 1u << 4;

		internal uint Magic;
		internal APTR Node;
		internal uint Columns;
		internal APTR Values;
		internal uint DisplayFlags;
	}

	internal enum MuiListtreeDisplaySnapshotField : byte
	{
		Magic,
		Node,
		Columns,
		Values,
		DisplayFlags,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeDisplaySnapshotFieldCursor
	{
		internal APTR Address;
		internal MuiListtreeDisplaySnapshotField Field;
	}

	internal static class MuiListtreeDisplaySnapshotFieldCursorCodec
	{
		private static bool TryResolve(MuiListtreeDisplaySnapshotField field,
			out uint offset)
		{
			switch (field)
			{
				case MuiListtreeDisplaySnapshotField.Magic: offset = 0; return true;
				case MuiListtreeDisplaySnapshotField.Node: offset = 4; return true;
				case MuiListtreeDisplaySnapshotField.Columns: offset = 8; return true;
				case MuiListtreeDisplaySnapshotField.Values: offset = 12; return true;
				case MuiListtreeDisplaySnapshotField.DisplayFlags: offset = 16; return true;
			}
			offset = 0;
			return false;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListtreeDisplaySnapshotFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
				cursor.Address.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Address,
					MuiListtreeDisplaySnapshotState.Size)) return false;
			address = APTR.FromPointer(cursor.Address.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeDisplaySnapshotField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListtreeDisplaySnapshotFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			value = platform.ReadUInt32(fieldAddress, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeDisplaySnapshotField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListtreeDisplaySnapshotFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			platform.WriteUInt32(fieldAddress, 0, value);
			return true;
		}
	}

	internal static class MuiListtreeDisplaySnapshotStateCodec
	{
		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeDisplaySnapshotState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeDisplaySnapshotState.Size) ||
				!MuiListtreeDisplaySnapshotFieldCursorCodec.TryReadUInt32(
					ref platform, address, MuiListtreeDisplaySnapshotField.Magic,
					out var magic) ||
				!MuiListtreeDisplaySnapshotFieldCursorCodec.TryReadUInt32(
					ref platform, address, MuiListtreeDisplaySnapshotField.Node,
					out var node) ||
				!MuiListtreeDisplaySnapshotFieldCursorCodec.TryReadUInt32(
					ref platform, address,
					MuiListtreeDisplaySnapshotField.Columns, out value.Columns) ||
				!MuiListtreeDisplaySnapshotFieldCursorCodec.TryReadUInt32(
					ref platform, address, MuiListtreeDisplaySnapshotField.Values,
					out var values) ||
				!MuiListtreeDisplaySnapshotFieldCursorCodec.TryReadUInt32(
					ref platform, address,
					MuiListtreeDisplaySnapshotField.DisplayFlags,
					out value.DisplayFlags)) return false;
			value.Magic = magic;
			value.Node = APTR.FromPointer(node);
			value.Values = APTR.FromPointer(values);
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeDisplaySnapshotState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, address, out value) &&
			value.Magic == MuiListtreeDisplaySnapshotState.Cookie;

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeDisplaySnapshotState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeDisplaySnapshotState.Size) || value.Magic !=
				MuiListtreeDisplaySnapshotState.Cookie) return false;
			return MuiListtreeDisplaySnapshotFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiListtreeDisplaySnapshotField.Magic,
				value.Magic) &&
				MuiListtreeDisplaySnapshotFieldCursorCodec.TryWriteUInt32(
					ref platform, address, MuiListtreeDisplaySnapshotField.Node,
					value.Node.Raw) &&
				MuiListtreeDisplaySnapshotFieldCursorCodec.TryWriteUInt32(
					ref platform, address,
				MuiListtreeDisplaySnapshotField.Columns, value.Columns) &&
				MuiListtreeDisplaySnapshotFieldCursorCodec.TryWriteUInt32(
					ref platform, address, MuiListtreeDisplaySnapshotField.Values,
					value.Values.Raw) &&
				MuiListtreeDisplaySnapshotFieldCursorCodec.TryWriteUInt32(
					ref platform, address,
					MuiListtreeDisplaySnapshotField.DisplayFlags,
					value.DisplayFlags);
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
		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreePolicyStateRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreePolicyStateRecord.Size) ||
				!MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreePolicyField.Magic, out var magic) ||
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
			value.Magic = magic;
			value.Active = APTR.FromPointer(active);
			value.CloseHook = APTR.FromPointer(closeHook);
			value.ConstructHook = APTR.FromPointer(constructHook);
			value.DestructHook = APTR.FromPointer(destructHook);
			value.DisplayHook = APTR.FromPointer(displayHook);
			value.OpenHook = APTR.FromPointer(openHook);
			value.SortHook = APTR.FromPointer(sortHook);
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreePolicyStateRecord value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, address, out value) &&
			value.Magic == MuiListtreePolicyStateRecord.Cookie;

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

	// The standard Exec memory pool supplied to MorphOS Listtree hooks is kept
	// as a named guest record for the lifetime of the Listtree object.  The pool
	// header itself is opaque and belongs to IMuiExecCapability; MUI stores only
	// the handle and the creation contract needed for native inspection.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeHookPoolStateRecord
	{
		internal const uint Size = 24;
		internal const uint Cookie = 0x4C54504Fu; // 'LTPO'

		internal uint Magic;
		internal APTR Pool;
		internal uint Requirements;
		internal uint PuddleSize;
		internal uint ThresholdSize;
		internal uint Owned;
	}

	internal enum MuiListtreeHookPoolField : byte
	{
		Magic,
		Pool,
		Requirements,
		PuddleSize,
		ThresholdSize,
		Owned,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeHookPoolFieldCursor
	{
		internal APTR Address;
		internal MuiListtreeHookPoolField Field;
	}

	internal static class MuiListtreeHookPoolFieldCursorCodec
	{
		private static bool TryResolve(MuiListtreeHookPoolField field,
			out uint offset)
		{
			switch (field)
			{
				case MuiListtreeHookPoolField.Magic: offset = 0; return true;
				case MuiListtreeHookPoolField.Pool: offset = 4; return true;
				case MuiListtreeHookPoolField.Requirements: offset = 8; return true;
				case MuiListtreeHookPoolField.PuddleSize: offset = 12; return true;
				case MuiListtreeHookPoolField.ThresholdSize: offset = 16; return true;
				case MuiListtreeHookPoolField.Owned: offset = 20; return true;
			}
			offset = 0;
			return false;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListtreeHookPoolFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
				cursor.Address.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Address,
					MuiListtreeHookPoolStateRecord.Size)) return false;
			address = APTR.FromPointer(cursor.Address.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeHookPoolField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListtreeHookPoolFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			value = platform.ReadUInt32(fieldAddress, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeHookPoolField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListtreeHookPoolFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			platform.WriteUInt32(fieldAddress, 0, value);
			return true;
		}
	}

	internal static class MuiListtreeHookPoolStateRecordCodec
	{
		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeHookPoolStateRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeHookPoolStateRecord.Size) ||
				!MuiListtreeHookPoolFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeHookPoolField.Magic, out var magic) ||
				!MuiListtreeHookPoolFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeHookPoolField.Pool, out var pool) ||
				!MuiListtreeHookPoolFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeHookPoolField.Requirements,
					out value.Requirements) ||
				!MuiListtreeHookPoolFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeHookPoolField.PuddleSize,
					out value.PuddleSize) ||
				!MuiListtreeHookPoolFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeHookPoolField.ThresholdSize,
					out value.ThresholdSize) ||
				!MuiListtreeHookPoolFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeHookPoolField.Owned, out value.Owned))
				return false;
			value.Magic = magic;
			value.Pool = APTR.FromPointer(pool);
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeHookPoolStateRecord value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, address, out value) &&
			value.Magic == MuiListtreeHookPoolStateRecord.Cookie &&
			value.Pool.IsNotNull && value.Owned != 0;

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeHookPoolStateRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeHookPoolStateRecord.Size) ||
				value.Magic != MuiListtreeHookPoolStateRecord.Cookie ||
				value.Pool.IsNull || value.Owned == 0) return false;
			return MuiListtreeHookPoolFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreeHookPoolField.Magic, value.Magic) &&
				MuiListtreeHookPoolFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreeHookPoolField.Pool, value.Pool.Raw) &&
				MuiListtreeHookPoolFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreeHookPoolField.Requirements,
				value.Requirements) &&
				MuiListtreeHookPoolFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreeHookPoolField.PuddleSize,
				value.PuddleSize) &&
				MuiListtreeHookPoolFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreeHookPoolField.ThresholdSize,
				value.ThresholdSize) &&
				MuiListtreeHookPoolFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreeHookPoolField.Owned, value.Owned);
		}
	}

	// Pointer double-click tracking is a guest-resident record rather than a
	// managed timer or event object.  Intuition timestamps are retained only
	// when the incoming message supplies the optional Seconds/Micros suffix;
	// untimestamped synthetic messages therefore cannot manufacture a second
	// click.  The node pointer and result flag stay named for native inspection.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeClickState
	{
		internal const uint Size = 28;
		internal const uint Cookie = 0x4C54434Bu; // 'LTCK'

		internal uint Magic;
		internal APTR LastNode;
		internal uint LastSeconds;
		internal uint LastMicros;
		internal uint Clicks;
		internal uint TimestampValid;
		internal uint DoubleClick;
	}

	internal enum MuiListtreeClickStateField : byte
	{
		Magic,
		LastNode,
		LastSeconds,
		LastMicros,
		Clicks,
		TimestampValid,
		DoubleClick,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeClickStateFieldCursor
	{
		internal APTR Address;
		internal MuiListtreeClickStateField Field;
	}

	internal static class MuiListtreeClickStateFieldCursorCodec
	{
		private static bool TryResolve(MuiListtreeClickStateField field,
			out uint offset)
		{
			offset = field switch
			{
				MuiListtreeClickStateField.Magic => 0,
				MuiListtreeClickStateField.LastNode => 4,
				MuiListtreeClickStateField.LastSeconds => 8,
				MuiListtreeClickStateField.LastMicros => 12,
				MuiListtreeClickStateField.Clicks => 16,
				MuiListtreeClickStateField.TimestampValid => 20,
				MuiListtreeClickStateField.DoubleClick => 24,
				_ => uint.MaxValue,
			};
			return offset != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListtreeClickStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
				cursor.Address.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Address, MuiListtreeClickState.Size))
				return false;
			address = APTR.FromPointer(cursor.Address.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeClickStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListtreeClickStateFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			value = platform.ReadUInt32(fieldAddress, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeClickStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListtreeClickStateFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			platform.WriteUInt32(fieldAddress, 0, value);
			return true;
		}
	}

	internal static class MuiListtreeClickStateCodec
	{
		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeClickState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeClickState.Size) ||
				!MuiListtreeClickStateFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeClickStateField.Magic, out var magic) ||
				!MuiListtreeClickStateFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeClickStateField.LastNode, out var lastNode) ||
				!MuiListtreeClickStateFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeClickStateField.LastSeconds,
					out value.LastSeconds) ||
				!MuiListtreeClickStateFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeClickStateField.LastMicros,
					out value.LastMicros) ||
				!MuiListtreeClickStateFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeClickStateField.Clicks, out value.Clicks) ||
				!MuiListtreeClickStateFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeClickStateField.TimestampValid,
					out value.TimestampValid) ||
				!MuiListtreeClickStateFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeClickStateField.DoubleClick,
					out value.DoubleClick)) return false;
			value.Magic = magic;
			value.LastNode = APTR.FromPointer(lastNode);
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeClickState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!TryReadStructural(ref platform, address, out value) ||
				value.Magic != MuiListtreeClickState.Cookie) return false;
			value.TimestampValid = value.TimestampValid == 0 ? 0u : 1u;
			value.DoubleClick = value.DoubleClick == 0 ? 0u : 1u;
			return true;
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeClickState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeClickState.Size) ||
				value.Magic != MuiListtreeClickState.Cookie) return false;
			return MuiListtreeClickStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreeClickStateField.Magic, value.Magic) &&
				MuiListtreeClickStateFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListtreeClickStateField.LastNode, value.LastNode.Raw) &&
				MuiListtreeClickStateFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListtreeClickStateField.LastSeconds,
					value.LastSeconds) &&
				MuiListtreeClickStateFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListtreeClickStateField.LastMicros, value.LastMicros) &&
				MuiListtreeClickStateFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListtreeClickStateField.Clicks, value.Clicks) &&
				MuiListtreeClickStateFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListtreeClickStateField.TimestampValid,
					value.TimestampValid == 0 ? 0u : 1u) &&
				MuiListtreeClickStateFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListtreeClickStateField.DoubleClick,
					value.DoubleClick == 0 ? 0u : 1u);
		}
	}

	// Column history is a separate guest record so the already-qualified click
	// record keeps its stable 28-byte ABI.  The current hit column is named and
	// persisted beside the timestamp record; it is not hidden in a managed
	// object or inferred from private byte positions.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeClickColumnState
	{
		internal const uint Size = 12;
		internal const uint Cookie = 0x4C544343u; // 'LTCC'

		internal uint Magic;
		internal uint LastColumn;
		internal uint Valid;
	}

	internal enum MuiListtreeClickColumnField : byte
	{
		Magic,
		LastColumn,
		Valid,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeClickColumnFieldCursor
	{
		internal APTR Address;
		internal MuiListtreeClickColumnField Field;
	}

	internal static class MuiListtreeClickColumnFieldCursorCodec
	{
		private static bool TryResolve(MuiListtreeClickColumnField field,
			out uint offset)
		{
			offset = field switch
			{
				MuiListtreeClickColumnField.Magic => 0,
				MuiListtreeClickColumnField.LastColumn => 4,
				MuiListtreeClickColumnField.Valid => 8,
				_ => uint.MaxValue,
			};
			return offset != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListtreeClickColumnFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
				cursor.Address.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Address, MuiListtreeClickColumnState.Size))
				return false;
			address = APTR.FromPointer(cursor.Address.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeClickColumnField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListtreeClickColumnFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			value = platform.ReadUInt32(fieldAddress, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeClickColumnField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListtreeClickColumnFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			platform.WriteUInt32(fieldAddress, 0, value);
			return true;
		}
	}

	internal static class MuiListtreeClickColumnStateCodec
	{
		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeClickColumnState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeClickColumnState.Size) ||
				!MuiListtreeClickColumnFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeClickColumnField.Magic, out var magic) ||
				!MuiListtreeClickColumnFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeClickColumnField.LastColumn,
					out value.LastColumn) ||
				!MuiListtreeClickColumnFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeClickColumnField.Valid, out value.Valid))
				return false;
			value.Magic = magic;
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeClickColumnState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!TryReadStructural(ref platform, address, out value) ||
				value.Magic != MuiListtreeClickColumnState.Cookie) return false;
			value.Valid = value.Valid == 0 ? 0u : 1u;
			return true;
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeClickColumnState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeClickColumnState.Size) ||
				value.Magic != MuiListtreeClickColumnState.Cookie) return false;
			return MuiListtreeClickColumnFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreeClickColumnField.Magic, value.Magic) &&
				MuiListtreeClickColumnFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListtreeClickColumnField.LastColumn,
					value.LastColumn) &&
				MuiListtreeClickColumnFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListtreeClickColumnField.Valid,
					value.Valid == 0 ? 0u : 1u);
		}
	}

	// External Listtree surface geometry is kept in a guest-resident named
	// record.  It deliberately does not borrow the built-in Area state: the
	// external class has its own lifecycle and must remain usable in a small
	// freestanding closure.  The row metric is a neutral fallback until the
	// MorphOS font/display policy is implemented.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeSurfaceStateRecord
	{
		internal const uint Size = 28;
		internal const uint Cookie = 0x4C545347u; // 'LTSG'

		internal uint Magic;
		internal int Left;
		internal int Top;
		internal int Width;
		internal int Height;
		internal uint RowHeight;
		internal uint FirstVisible;
	}

	internal enum MuiListtreeSurfaceField : byte
	{
		Magic,
		Left,
		Top,
		Width,
		Height,
		RowHeight,
		FirstVisible,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeSurfaceFieldCursor
	{
		internal APTR Address;
		internal MuiListtreeSurfaceField Field;
	}

	internal static class MuiListtreeSurfaceFieldCursorCodec
	{
		private static bool TryResolve(MuiListtreeSurfaceField field,
			out uint offset)
		{
			switch (field)
			{
				case MuiListtreeSurfaceField.Magic: offset = 0; return true;
				case MuiListtreeSurfaceField.Left: offset = 4; return true;
				case MuiListtreeSurfaceField.Top: offset = 8; return true;
				case MuiListtreeSurfaceField.Width: offset = 12; return true;
				case MuiListtreeSurfaceField.Height: offset = 16; return true;
				case MuiListtreeSurfaceField.RowHeight: offset = 20; return true;
				case MuiListtreeSurfaceField.FirstVisible: offset = 24; return true;
			}
			offset = 0;
			return false;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListtreeSurfaceFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) ||
				cursor.Address.IsNull || cursor.Address.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Address,
					MuiListtreeSurfaceStateRecord.Size)) return false;
			address = APTR.FromPointer(cursor.Address.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeSurfaceField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListtreeSurfaceFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			value = platform.ReadUInt32(fieldAddress, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeSurfaceField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListtreeSurfaceFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			platform.WriteUInt32(fieldAddress, 0, value);
			return true;
		}
	}

	internal static class MuiListtreeSurfaceStateRecordCodec
	{
		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeSurfaceStateRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeSurfaceStateRecord.Size) ||
				!MuiListtreeSurfaceFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeSurfaceField.Magic, out var magic) ||
				!MuiListtreeSurfaceFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeSurfaceField.Left, out var left) ||
				!MuiListtreeSurfaceFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeSurfaceField.Top, out var top) ||
				!MuiListtreeSurfaceFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeSurfaceField.Width, out var width) ||
				!MuiListtreeSurfaceFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeSurfaceField.Height, out var height) ||
				!MuiListtreeSurfaceFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeSurfaceField.RowHeight, out value.RowHeight) ||
				!MuiListtreeSurfaceFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeSurfaceField.FirstVisible,
					out value.FirstVisible))
				return false;
			value.Magic = magic;
			value.Left = unchecked((int)left);
			value.Top = unchecked((int)top);
			value.Width = unchecked((int)width);
			value.Height = unchecked((int)height);
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeSurfaceStateRecord value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, address, out value) &&
			value.Magic == MuiListtreeSurfaceStateRecord.Cookie;

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeSurfaceStateRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeSurfaceStateRecord.Size) ||
				value.Magic != MuiListtreeSurfaceStateRecord.Cookie) return false;
			return MuiListtreeSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreeSurfaceField.Magic, value.Magic) &&
				MuiListtreeSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListtreeSurfaceField.Left, unchecked((uint)value.Left)) &&
				MuiListtreeSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListtreeSurfaceField.Top, unchecked((uint)value.Top)) &&
				MuiListtreeSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListtreeSurfaceField.Width, unchecked((uint)value.Width)) &&
				MuiListtreeSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListtreeSurfaceField.Height, unchecked((uint)value.Height)) &&
				MuiListtreeSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListtreeSurfaceField.RowHeight, value.RowHeight) &&
				MuiListtreeSurfaceFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListtreeSurfaceField.FirstVisible, value.FirstVisible);
		}
	}

	// Area lifecycle state for the external class.  Keep it separate from the
	// rectangle so Setup/Cleanup/Show/Hide can evolve without changing the
	// public surface geometry record.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeLifecycleStateRecord
	{
		internal const uint Size = 16;
		internal const uint Cookie = 0x4C544C43u; // 'LTLC'

		internal uint Magic;
		internal APTR RenderInfo;
		internal uint Setup;
		internal uint Shown;
	}

	internal enum MuiListtreeLifecycleField : byte
	{
		Magic,
		RenderInfo,
		Setup,
		Shown,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListtreeLifecycleFieldCursor
	{
		internal APTR Address;
		internal MuiListtreeLifecycleField Field;
	}

	internal static class MuiListtreeLifecycleFieldCursorCodec
	{
		private static bool TryResolve(MuiListtreeLifecycleField field,
			out uint offset)
		{
			switch (field)
			{
				case MuiListtreeLifecycleField.Magic: offset = 0; return true;
				case MuiListtreeLifecycleField.RenderInfo: offset = 4; return true;
				case MuiListtreeLifecycleField.Setup: offset = 8; return true;
				case MuiListtreeLifecycleField.Shown: offset = 12; return true;
			}
			offset = 0;
			return false;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListtreeLifecycleFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) ||
				cursor.Address.IsNull || cursor.Address.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Address,
					MuiListtreeLifecycleStateRecord.Size)) return false;
			address = APTR.FromPointer(cursor.Address.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeLifecycleField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListtreeLifecycleFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			value = platform.ReadUInt32(fieldAddress, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeLifecycleField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListtreeLifecycleFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			platform.WriteUInt32(fieldAddress, 0, value);
			return true;
		}
	}

	internal static class MuiListtreeLifecycleStateRecordCodec
	{
		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeLifecycleStateRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeLifecycleStateRecord.Size) ||
				!MuiListtreeLifecycleFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeLifecycleField.Magic, out var magic) ||
				!MuiListtreeLifecycleFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeLifecycleField.RenderInfo, out var renderInfo) ||
				!MuiListtreeLifecycleFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeLifecycleField.Setup, out value.Setup) ||
				!MuiListtreeLifecycleFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListtreeLifecycleField.Shown, out value.Shown))
				return false;
			value.Magic = magic;
			value.RenderInfo = APTR.FromPointer(renderInfo);
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeLifecycleStateRecord value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, address, out value) &&
			value.Magic == MuiListtreeLifecycleStateRecord.Cookie;

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListtreeLifecycleStateRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeLifecycleStateRecord.Size) ||
				value.Magic != MuiListtreeLifecycleStateRecord.Cookie) return false;
			return MuiListtreeLifecycleFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreeLifecycleField.Magic, value.Magic) &&
				MuiListtreeLifecycleFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListtreeLifecycleField.RenderInfo,
					value.RenderInfo.Raw) &&
				MuiListtreeLifecycleFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListtreeLifecycleField.Setup, value.Setup) &&
				MuiListtreeLifecycleFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListtreeLifecycleField.Shown, value.Shown);
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
		// The private words are kept named in the fixed record so selection state
		// remains guest-resident without changing the public 18-byte node prefix.
		// Reserved0 is the selected bit and Reserved1 marks the range anchor.
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
		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeNodeState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListtreeNodeState.Size) ||
				!MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
					MuiListtreeNodeField.Private1, out var private1) ||
				!MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
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
			value.Private1 = private1;
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

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListtreeNodeState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, address, out value) &&
			value.Private1 == MuiListtreeNodeState.Cookie;

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
	public const uint Title = 0x80020015u;           // [ISG] CONST_STRPTR (MorphOS BOOL compatibility)
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
	public const uint MethodSetup = 0x80428354u;
	public const uint MethodCleanup = 0x8042D985u;
	public const uint MethodShow = 0x8042CC84u;
	public const uint MethodHide = 0x8042F20Fu;
	// MUIM_HandleInput is inherited from Area/Listview.  Listtree consumes
	// the same named collection packet, with the preprocessed MorphOS MUIKEY
	// value in the signed MuiKey field.
	public const uint MethodHandleInput = 0x80422A1Au;

	// ---- Selectors (signed) --------------------------------------------------
	// MUIA_Listtree_DoubleClick policy values from the MorphOS header/docs.
	// A non-negative value names the FORMAT column that reacts; the three
	// negative values are the documented Off/All/Tree selectors.
	public const int DoubleClickOff = -1;
	public const int DoubleClickAll = -2;
	public const int DoubleClickTree = -3;
	// ListNode: Root == 0, Parent == -1 (Open only), Active == -2.
	private const int ListNodeRoot = 0;
	private const int ListNodeParent = -1;
	private const int ListNodeActive = -2;
	// TreeNode: Head == 0, Tail == -1, Active == -2, All == -3.
	private const int TreeNodeHead = 0;
	private const int TreeNodeTail = -1;
	private const int TreeNodeActive = -2;
	private const int TreeNodeAll = -3;
	// MUIM_Listtree_Exchange relative selectors for the second entry.  The
	// selector is resolved against TreeNode1's sibling list and then flows
	// through the same named topology rewrite as an explicit TreeNode2.
	public const int ExchangeTreeNode2Up = -5;
	public const int ExchangeTreeNode2Down = -6;
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
	// Preprocessed MorphOS MUIKEY values used by the inherited Area/Listview
	// input path.  Press and Toggle select the active node, Right/Left open and
	// close it, and the vertical set walks the visible preorder display list
	// without exposing topology offsets.
	private const int KeyPress = 0;
	private const int KeyToggle = 1;
	private const int KeyLeft = 8;
	private const int KeyRight = 9;
	private const int KeyUp = 2;
	private const int KeyDown = 3;
	private const int KeyPageUp = 4;
	private const int KeyPageDown = 5;
	private const int KeyTop = 6;
	private const int KeyBottom = 7;
	private const int KeyNone = -1;
	private const uint IdcmpMouseButtons = 1u << 3;
	private const uint IdcmpMouseMove = 1u << 4;
	private const ushort SelectDown = 0x0068;
	private const ushort SelectUp = 0x0069;
	private const ushort MouseMove = 0;
	// Intuition qualifier bits used by the MorphOS list selection convention.
	// Shift extends a range, while Control toggles one entry when MultiSelect is
	// enabled.  The values are decoded from the named IntuiMessage field.
	private const ushort QualifierShift = 0x0003;
	private const ushort QualifierControl = 0x0008;
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

	// Typed release plan for the bounded pointer-drag path.  The plan keeps
	// the public Move arguments named until the single mutation call; it is not
	// a guest record and therefore needs no private byte-offset contract.
	private struct MuiListtreeDragCommitPlan
	{
		internal APTR OldListNode;
		internal APTR OldTreeNode;
		internal APTR NewListNode;
		internal APTR NewTreeNode;
		internal uint DropMark;
	}

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
	private const uint PresentationStateKey = 0x7F090003u;
	private const uint PresentationStateSize =
		MuiListtreePresentationStateRecord.Size;
	private const uint SurfaceStateKey = 0x7F090004u;
	private const uint SurfaceStateSize = MuiListtreeSurfaceStateRecord.Size;
	private const uint LifecycleStateKey = 0x7F090005u;
	private const uint LifecycleStateSize = MuiListtreeLifecycleStateRecord.Size;
	private const uint ClickStateKey = 0x7F090006u;
	private const uint ClickStateSize = MuiListtreeClickState.Size;
	private const uint ClickColumnStateKey = 0x7F090007u;
	private const uint ClickColumnStateSize = MuiListtreeClickColumnState.Size;
	private const uint DisplaySnapshotKey = 0x7F090008u;
	private const uint DisplaySnapshotSize =
		MuiListtreeDisplaySnapshotState.Size;
	private const uint HookPoolStateKey = 0x7F090009u;
	private const uint HookPoolStateSize = MuiListtreeHookPoolStateRecord.Size;
	private const uint DoubleClickWindowMicros = 500000;

	private const uint MaximumNodes = 0x00040000u;
	private const uint MaximumDepth = 512;
	private const uint MaximumTraversal = 0x00040000u;
	private const uint MaximumStringLength = 4096;
	private const uint MaximumFormatColumns = 256;
	private const uint DefaultRowHeight = 16;
	private const uint DefaultHookPoolRequirements = 0;
	private const uint DefaultHookPoolPuddleSize = 2008;
	private const uint DefaultHookPoolThreshold = 1024;
	private const int MaximumSurfaceDimension = 10000;
	private const uint NodeSelectedFlag = 1;
	private const uint NodeAnchorFlag = 1;

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

	// MUIM_AskMinMax for the external tree.  This is intentionally a neutral
	// row model: the visible preorder count is exact, while font and column
	// policy are deferred to the later MorphOS display implementation.
	public static bool AskMinMax<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR storage) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (Header(ref platform, state, obj).IsNull ||
			!platform.IsMapped(storage, MuiMinMaxValues.Size) ||
			!TryReadSurfaceStateRecord(ref platform, state, obj, out var surface))
			return false;
		var rowHeight = surface.RowHeight == 0 ? DefaultRowHeight :
			surface.RowHeight;
		var rows = VisibleCount(ref platform, state, obj);
		if (ReadRaw(ref platform, state, obj, Title, 0) != 0 &&
			rows < uint.MaxValue) rows++;
		var requestedRows = rows == 0 ? 1u : rows;
		var height = requestedRows > MaximumSurfaceDimension / rowHeight
			? MaximumSurfaceDimension
			: unchecked((int)(requestedRows * rowHeight));
		if (height > short.MaxValue) height = short.MaxValue;
		var values = default(MuiMinMaxValues);
		values.MinWidth = 1;
		values.MinHeight = unchecked((short)rowHeight);
		values.MaxWidth = unchecked((short)MaximumSurfaceDimension);
		values.MaxHeight = unchecked((short)MaximumSurfaceDimension);
		values.DefWidth = 64;
		values.DefHeight = unchecked((short)height);
		return MuiMinMaxRecordCodec.Write(ref platform, storage, values);
	}

	// MUIM_Layout records the external class rectangle in its own typed guest
	// state.  No Area policy or managed geometry object is pulled into the
	// external closure; the Draw hook remains the presentation boundary.
	public static bool Layout<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int left, int top, int width, int height)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (Header(ref platform, state, obj).IsNull || width < 0 || height < 0 ||
			left > int.MaxValue - width || top > int.MaxValue - height ||
			!TryReadSurfaceStateRecord(ref platform, state, obj, out var surface))
			return false;
		surface.Left = left;
		surface.Top = top;
		surface.Width = width;
		surface.Height = height;
		ClampSurfaceViewport(ref platform, state, obj, ref surface);
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			SurfaceStateKey);
		return MuiListtreeSurfaceStateRecordCodec.Write(ref platform, block,
			surface);
	}

	public static bool Setup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR renderInfo) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (Header(ref platform, state, obj).IsNull ||
			!MuiDrawingRenderInfoCodec.TryRead(ref platform, renderInfo,
				out _ ) ||
			!TryReadLifecycleStateRecord(ref platform, state, obj, out var value))
			return false;
		value.RenderInfo = renderInfo;
		value.Setup = 1;
		value.Shown = 1;
		return WriteLifecycleStateRecord(ref platform, state, obj, value);
	}

	public static bool Cleanup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (Header(ref platform, state, obj).IsNull ||
			!TryReadLifecycleStateRecord(ref platform, state, obj, out var value))
			return false;
		value.RenderInfo = APTR.Null;
		value.Setup = 0;
		value.Shown = 0;
		return WriteLifecycleStateRecord(ref platform, state, obj, value);
	}

	public static bool Show<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadLifecycleStateRecord(ref platform, state, obj, out var value) ||
			value.Setup == 0) return false;
		value.Shown = 1;
		return WriteLifecycleStateRecord(ref platform, state, obj, value);
	}

	public static bool Hide<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadLifecycleStateRecord(ref platform, state, obj, out var value))
			return false;
		value.Shown = 0;
		return WriteLifecycleStateRecord(ref platform, state, obj, value);
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
		// Reserved fields are the named private drag snapshot: no active source
		// or target is represented until SELECTDOWN arms the capture.
		headerValue.Reserved0 = 0xFFFFFFFFu;
		headerValue.Reserved1 = 0xFFFFFFFFu;
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
		EnsureDefault(ref platform, state, obj, EmptyNodes, 0);
		EnsureDefault(ref platform, state, obj, Format, 0);
		EnsureDefault(ref platform, state, obj, MultiSelect, 0);
		EnsureDefault(ref platform, state, obj, NList, 0);
		EnsureDefault(ref platform, state, obj, Title, 0);
		EnsureDefault(ref platform, state, obj, TreeColumn, 0);
		return EnsurePolicyStateRecord(ref platform, state, obj) &&
			EnsurePresentationStateRecord(ref platform, state, obj) &&
			EnsureSurfaceStateRecord(ref platform, state, obj) &&
			EnsureLifecycleStateRecord(ref platform, state, obj) &&
			EnsureClickStateRecord(ref platform, state, obj);
	}

	// Retire every guest-resident node and the header during disposal. Invoked
	// from MuiCollectionLifecycle.DisposeObject; a no-op for non-Listtree objects.
	internal static void CleanupRecords<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		FreeDisplaySnapshot(ref platform, state, obj);
		var hasHookPool = TryGetHookPool(ref platform, state, obj,
			out var hookPool);
		var header = Header(ref platform, state, obj);
		if (header.IsNotNull)
		{
			var node = ReadHeaderRootFirst(ref platform, header);
			while (node.IsNotNull)
			{
				var next = ReadNodeNext(ref platform, node);
				FreeSubtree(ref platform, state, obj, node, 0);
				node = next;
			}
			platform.Clear(header, HeaderSize);
			platform.Free(header, HeaderSize);
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				TreeHeaderKey, 0, false);
		}
		// DestructHook calls above must see a live pool. Retire it only after the
		// final node has been delivered, then remove the copied state record.
		if (hasHookPool)
		{
			platform.DeletePool(hookPool);
			MuiStoreCore.DataspaceRemove(ref platform, state, obj,
				HookPoolStateKey);
		}
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

	internal static bool IsPresentationAttribute(uint attribute) =>
		attribute == EmptyNodes || attribute == Format ||
		attribute == MultiSelect || attribute == NList ||
		attribute == Title || attribute == TreeColumn;

	// Public Listtree policy getters are projected from the canonical guest
	// record. Bootstrap helpers below intentionally use GetRawAttribute so this
	// class-gated route cannot recurse while the record is being created.
	internal static bool IsPublicGetterAttribute(uint attribute) =>
		(attribute != Quiet && IsPolicyAttribute(attribute)) ||
		IsPresentationAttribute(attribute);

	// Quiet is documented as [.S.]: it is a runtime control written by the
	// caller, but it is neither a construction tag nor a public getter. Keep a
	// separate set predicate so generic MUIM_Set still reaches the named policy
	// record without widening the Get/constructor surface.
	internal static bool IsPublicSetAttribute(uint attribute) =>
		IsPolicyAttribute(attribute) || IsPresentationAttribute(attribute);

	internal static bool IsRuntimeSetOnlyAttribute(uint attribute) =>
		attribute == Quiet;

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

	private static bool TryReadHookPoolStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListtreeHookPoolStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			HookPoolStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			HookPoolStateKey) != unchecked((int)HookPoolStateSize)) return false;
		return MuiListtreeHookPoolStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureHookPoolStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadHookPoolStateRecord(ref platform, state, obj, out _))
			return true;
		// A malformed pre-existing record is not replaced: doing so could leak an
		// unknown native pool. The object remains construct-failure-safe instead.
		var existing = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			HookPoolStateKey);
		if (existing.IsNotNull || MuiStoreCore.DataspaceLength(ref platform, state,
			obj, HookPoolStateKey) != 0) return false;
		var pool = platform.CreatePool(DefaultHookPoolRequirements,
			DefaultHookPoolPuddleSize, DefaultHookPoolThreshold);
		if (pool.IsNull) return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform, HookPoolStateSize);
		if (scratch.IsNull)
		{
			platform.DeletePool(pool);
			return false;
		}
		platform.Clear(scratch, HookPoolStateSize);
		var value = default(MuiListtreeHookPoolStateRecord);
		value.Magic = MuiListtreeHookPoolStateRecord.Cookie;
		value.Pool = pool;
		value.Requirements = DefaultHookPoolRequirements;
		value.PuddleSize = DefaultHookPoolPuddleSize;
		value.ThresholdSize = DefaultHookPoolThreshold;
		value.Owned = 1;
		var written = MuiListtreeHookPoolStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			HookPoolStateKey, scratch, unchecked((int)HookPoolStateSize));
		platform.Clear(scratch, HookPoolStateSize);
		platform.Free(scratch, HookPoolStateSize);
		if (!added)
		{
			MuiStoreCore.DataspaceRemove(ref platform, state, obj,
				HookPoolStateKey);
			platform.DeletePool(pool);
			return false;
		}
		return true;
	}

	private static bool TryGetHookPool<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out APTR pool)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		pool = APTR.Null;
		if (!TryReadHookPoolStateRecord(ref platform, state, obj, out var value))
			return false;
		pool = value.Pool;
		return pool.IsNotNull;
	}

	internal static bool TryGetHookPoolStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListtreeHookPoolStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadHookPoolStateRecord(ref platform, state, obj, out value);

	internal static bool TryGetPolicyStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListtreePolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadPolicyStateRecord(ref platform, state, obj, out value);

	private static bool TryReadPresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListtreePresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PresentationStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PresentationStateKey) != unchecked((int)PresentationStateSize))
			return false;
		return MuiListtreePresentationStateRecordCodec.TryRead(ref platform,
			block, out value);
	}

	private static void FillPresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		ref MuiListtreePresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value.Magic = MuiListtreePresentationStateRecord.Cookie;
		value.EmptyNodes = ReadRaw(ref platform, state, obj, EmptyNodes, 0) == 0
			? 0u : 1u;
		value.Format = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			Format, 0));
		value.MultiSelect = ReadRaw(ref platform, state, obj, MultiSelect, 0) == 0
			? 0u : 1u;
		value.NList = ReadRaw(ref platform, state, obj, NList, 0) == 0
			? 0u : 1u;
		value.Title = ReadRaw(ref platform, state, obj, Title, 0) == 0
			? 0u : 1u;
		value.TreeColumn = ReadRaw(ref platform, state, obj, TreeColumn, 0) == 0
			? 0u : 1u;
	}

	private static bool EnsurePresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadPresentationStateRecord(ref platform, state, obj, out _))
			return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			PresentationStateSize);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, PresentationStateSize);
		var value = default(MuiListtreePresentationStateRecord);
		FillPresentationStateRecord(ref platform, state, obj, ref value);
		var written = MuiListtreePresentationStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			PresentationStateKey, scratch, unchecked((int)PresentationStateSize));
		platform.Clear(scratch, PresentationStateSize);
		platform.Free(scratch, PresentationStateSize);
		return added;
	}

	private static bool SyncPresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsurePresentationStateRecord(ref platform, state, obj) ||
			!TryReadPresentationStateRecord(ref platform, state, obj, out var value))
			return false;
		FillPresentationStateRecord(ref platform, state, obj, ref value);
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PresentationStateKey);
		return MuiListtreePresentationStateRecordCodec.Write(ref platform,
			block, value);
	}

	internal static bool TryGetPresentationStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListtreePresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadPresentationStateRecord(ref platform, state, obj, out value);

	private static bool TryReadClickStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListtreeClickState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ClickStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ClickStateKey) != unchecked((int)ClickStateSize)) return false;
		return MuiListtreeClickStateCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureClickStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadClickStateRecord(ref platform, state, obj, out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform, ClickStateSize);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, ClickStateSize);
		var value = default(MuiListtreeClickState);
		value.Magic = MuiListtreeClickState.Cookie;
		var written = MuiListtreeClickStateCodec.Write(ref platform, scratch,
			value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			ClickStateKey, scratch, unchecked((int)ClickStateSize));
		platform.Clear(scratch, ClickStateSize);
		platform.Free(scratch, ClickStateSize);
		return added;
	}

	private static bool WriteClickStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiListtreeClickState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureClickStateRecord(ref platform, state, obj)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ClickStateKey);
		return MuiListtreeClickStateCodec.Write(ref platform, block, value);
	}

	internal static bool TryGetClickStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListtreeClickState value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadClickStateRecord(ref platform, state, obj, out value);

	internal static bool TryGetClickColumnStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListtreeClickColumnState value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadClickColumnStateRecord(ref platform, state, obj, out value);

	private static bool TryReadClickColumnStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListtreeClickColumnState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ClickColumnStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ClickColumnStateKey) != unchecked((int)ClickColumnStateSize))
			return false;
		return MuiListtreeClickColumnStateCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureClickColumnStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadClickColumnStateRecord(ref platform, state, obj, out _))
			return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			ClickColumnStateSize);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, ClickColumnStateSize);
		var value = default(MuiListtreeClickColumnState);
		value.Magic = MuiListtreeClickColumnState.Cookie;
		var written = MuiListtreeClickColumnStateCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			ClickColumnStateKey, scratch, unchecked((int)ClickColumnStateSize));
		platform.Clear(scratch, ClickColumnStateSize);
		platform.Free(scratch, ClickColumnStateSize);
		return added;
	}

	private static bool WriteClickColumnStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiListtreeClickColumnState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureClickColumnStateRecord(ref platform, state, obj)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ClickColumnStateKey);
		return MuiListtreeClickColumnStateCodec.Write(ref platform, block, value);
	}

	private static bool TryReadSurfaceStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListtreeSurfaceStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			SurfaceStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			SurfaceStateKey) != unchecked((int)SurfaceStateSize)) return false;
		return MuiListtreeSurfaceStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureSurfaceStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadSurfaceStateRecord(ref platform, state, obj, out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform, SurfaceStateSize);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, SurfaceStateSize);
		var value = default(MuiListtreeSurfaceStateRecord);
		value.Magic = MuiListtreeSurfaceStateRecord.Cookie;
		value.RowHeight = DefaultRowHeight;
		var written = MuiListtreeSurfaceStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			SurfaceStateKey, scratch, unchecked((int)SurfaceStateSize));
		platform.Clear(scratch, SurfaceStateSize);
		platform.Free(scratch, SurfaceStateSize);
		return added;
	}

	internal static bool TryGetSurfaceStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListtreeSurfaceStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadSurfaceStateRecord(ref platform, state, obj, out value);

	private static bool TryReadDisplaySnapshotState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListtreeDisplaySnapshotState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			DisplaySnapshotKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			DisplaySnapshotKey) != unchecked((int)DisplaySnapshotSize)) return false;
		return MuiListtreeDisplaySnapshotStateCodec.TryRead(ref platform, block,
			out value);
	}

	internal static bool TryGetDisplaySnapshotStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListtreeDisplaySnapshotState value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadDisplaySnapshotState(ref platform, state, obj, out value);

	private static void FreeDisplaySnapshot<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadDisplaySnapshotState(ref platform, state, obj,
			out var value))
		{
			MuiStoreCore.DataspaceRemove(ref platform, state, obj,
				DisplaySnapshotKey);
			return;
		}
		if (value.Columns != 0 && value.Columns <=
			MuiListtreeDisplayColumnCursor.MaximumEntries &&
			value.Columns <= uint.MaxValue /
			MuiListtreeDisplayColumnRecord.Size)
		{
			var bytes = value.Columns *
				MuiListtreeDisplayColumnRecord.Size;
			if (value.Values.IsNotNull && platform.IsMapped(value.Values, bytes))
			{
				platform.Clear(value.Values, bytes);
				platform.Free(value.Values, bytes);
			}
		}
		MuiStoreCore.DataspaceRemove(ref platform, state, obj,
			DisplaySnapshotKey);
	}

	private static bool StoreDisplaySnapshot<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR node,
		uint columns, APTR values, uint displayFlags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (columns == 0 || columns >
			MuiListtreeDisplayColumnCursor.MaximumEntries || values.IsNull)
			return false;
		FreeDisplaySnapshot(ref platform, state, obj);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			DisplaySnapshotSize);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, DisplaySnapshotSize);
		var value = default(MuiListtreeDisplaySnapshotState);
		value.Magic = MuiListtreeDisplaySnapshotState.Cookie;
		value.Node = node;
		value.Columns = columns;
		value.Values = values;
		value.DisplayFlags = displayFlags;
		var written = MuiListtreeDisplaySnapshotStateCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			DisplaySnapshotKey, scratch, unchecked((int)DisplaySnapshotSize));
		platform.Clear(scratch, DisplaySnapshotSize);
		platform.Free(scratch, DisplaySnapshotSize);
		return added;
	}

	private static bool TryReadLifecycleStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListtreeLifecycleStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LifecycleStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			LifecycleStateKey) != unchecked((int)LifecycleStateSize)) return false;
		return MuiListtreeLifecycleStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureLifecycleStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadLifecycleStateRecord(ref platform, state, obj, out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform, LifecycleStateSize);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, LifecycleStateSize);
		var value = default(MuiListtreeLifecycleStateRecord);
		value.Magic = MuiListtreeLifecycleStateRecord.Cookie;
		// Keep direct Draw callers compatible while still allowing Hide/Cleanup to
		// suppress the neutral renderer once lifecycle packets are used.
		value.Shown = 1;
		var written = MuiListtreeLifecycleStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			LifecycleStateKey, scratch, unchecked((int)LifecycleStateSize));
		platform.Clear(scratch, LifecycleStateSize);
		platform.Free(scratch, LifecycleStateSize);
		return added;
	}

	internal static bool TryGetLifecycleStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListtreeLifecycleStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadLifecycleStateRecord(ref platform, state, obj, out value);

	private static bool WriteLifecycleStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiListtreeLifecycleStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LifecycleStateKey);
		return MuiListtreeLifecycleStateRecordCodec.Write(ref platform, block,
			value);
	}

	// The typed drag snapshot is hosted in the three named private header fields
	// rather than another dataspace allocation. This keeps the external object
	// closure small and avoids retaining a second guest record solely for the
	// SELECTDOWN/MOUSEMOVE/SELECTUP transition.
	private static bool TryReadDragStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewDragState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var header = Header(ref platform, state, obj);
		if (header.IsNull || !MuiListtreeHeaderCodec.TryRead(ref platform,
			header, out var headerValue)) return false;
		value.Magic = MuiListviewDragStateCodec.Cookie;
		value.Source = unchecked((int)headerValue.Reserved0);
		value.Target = unchecked((int)headerValue.Reserved1);
		value.Flags = headerValue.Reserved2;
		return true;
	}

	internal static bool TryGetDragStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListviewDragState value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadDragStateRecord(ref platform, state, obj, out value);

	private static bool WriteDragStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiListviewDragState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull || !MuiListtreeHeaderCodec.TryRead(ref platform,
			header, out var headerValue)) return false;
		headerValue.Reserved0 = unchecked((uint)value.Source);
		headerValue.Reserved1 = unchecked((uint)value.Target);
		headerValue.Reserved2 = value.Flags;
		return MuiListtreeHeaderCodec.Write(ref platform, header, headerValue);
	}

	private static bool TryReadPresentationValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint attribute,
		out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (!IsPresentationAttribute(attribute) ||
			!TryReadPresentationStateRecord(ref platform, state, obj, out var record))
			return false;
		switch (attribute)
		{
			case EmptyNodes: value = record.EmptyNodes; return true;
			case Format: value = record.Format.Raw; return true;
			case MultiSelect: value = record.MultiSelect; return true;
			case NList: value = record.NList; return true;
			case Title: value = record.Title; return true;
			case TreeColumn: value = record.TreeColumn; return true;
		}
		return false;
	}

	private static uint NormalizeConstructionValue(uint attribute, uint value)
	{
		// BOOL tags are normalized at the owner boundary. LONG selector values,
		// hook pointers, and the caller-owned Format string pointer retain their
		// wire value.
		return attribute == DuplicateNodeName || attribute == Quiet ||
			attribute == DragDropSort || attribute == EmptyNodes ||
			attribute == MultiSelect || attribute == NList ||
			attribute == TreeColumn || attribute == Title
			? value == 0 ? 0u : 1u : value;
	}

	private static bool TryReadPolicyValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (!IsPolicyAttribute(attribute)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PolicyStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PolicyStateKey) != unchecked((int)PolicyStateSize) ||
			!MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListtreePolicyField.Magic, out var magic) ||
			magic != MuiListtreePolicyStateRecord.Cookie) return false;
		switch (attribute)
		{
			case Active:
				return MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform,
					block, MuiListtreePolicyField.Active, out value);
			case DuplicateNodeName:
				return MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform,
					block, MuiListtreePolicyField.DuplicateNodeName, out value);
			case Quiet:
				return MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform,
					block, MuiListtreePolicyField.Quiet, out value);
			case DragDropSort:
				return MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform,
					block, MuiListtreePolicyField.DragDropSort, out value);
			case DoubleClick:
				return MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform,
					block, MuiListtreePolicyField.DoubleClick, out value);
			case CloseHook:
				return MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform,
					block, MuiListtreePolicyField.CloseHook, out value);
			case ConstructHook:
				return MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform,
					block, MuiListtreePolicyField.ConstructHook, out value);
			case DestructHook:
				return MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform,
					block, MuiListtreePolicyField.DestructHook, out value);
			case DisplayHook:
				return MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform,
					block, MuiListtreePolicyField.DisplayHook, out value);
			case OpenHook:
				return MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform,
					block, MuiListtreePolicyField.OpenHook, out value);
			case SortHook:
				return MuiListtreePolicyFieldCursorCodec.TryReadUInt32(ref platform,
					block, MuiListtreePolicyField.SortHook, out value);
		}
		return false;
	}

	public static bool SetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull)
		{
			// CreateObjectA applies construction tags before the external class
			// header exists. Preserve all public Listtree tags in the generic
			// attribute record; Construct will publish them into named records
			// later. This keeps generic OM_SET and construction tags on one path.
			if (!IsPublicGetterAttribute(attribute)) return false;
			var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
			return !record.IsNull && MuiHeadlessObjectCore.SetRecordAttributeRaw(
				ref platform, state, record, attribute,
				NormalizeConstructionValue(attribute, value), notify);
		}
		if (attribute == Quiet)
		{
			var wasQuiet = ReadPolicy(ref platform, state, obj, Quiet, 0) != 0;
			var nowQuiet = value != 0;
			if (!SetRawAttribute(ref platform, state, obj, Quiet,
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
			// MorphOS only moves the cursor onto a visible node.  A valid node
			// hidden below a closed parent remains addressable by topology methods,
			// but cannot become Active until its ancestors are opened.  Resolve the
			// visible-preorder index through the named tree records rather than
			// inspecting a private List/Listview offset.
			if (value != ActiveOff)
			{
				var target = APTR.FromPointer(value);
				var headerForActive = Header(ref platform, state, obj);
				if (!IsValidNode(ref platform, obj, target) ||
					headerForActive.IsNull ||
					VisibleIndexOf(ref platform, headerForActive, target) ==
						uint.MaxValue) return false;
			}
			if (!SetActive(ref platform, state, obj, APTR.FromPointer(value),
				notify)) return false;
			return SyncPolicyStateRecord(ref platform, state, obj);
		}
		if (IsPresentationAttribute(attribute))
		{
			var normalized = attribute == Format
				? value
				: value == 0 ? 0u : 1u;
			var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
			if (record.IsNull || !MuiHeadlessObjectCore.SetRecordAttributeRaw(
				ref platform, state, record, attribute, normalized, notify))
				return false;
			return SyncPresentationStateRecord(ref platform, state, obj);
		}
		if (!IsPolicyAttribute(attribute))
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, value, notify);
		var normalizedPolicy = NormalizeConstructionValue(attribute, value);
		if (!SetRawAttribute(ref platform, state, obj, attribute,
			normalizedPolicy, notify)) return false;
		return SyncPolicyStateRecord(ref platform, state, obj);
	}

	public static bool GetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (attribute == Quiet) return false;
		if (TryReadPolicyValue(ref platform, state, obj, attribute, out value) ||
			TryReadPresentationValue(ref platform, state, obj, attribute,
				out value))
			return true;
		if (IsPolicyAttribute(attribute) || IsPresentationAttribute(attribute))
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
		var stored = ConstructInsertUser(ref platform, state, obj, name, user,
			listNode, prevNode, flags, out var userOwned);
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
		_ = RefreshSurfaceViewportAfterMutation(ref platform, state, obj);
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
			var anchor = (flags & FlagsVisible) != 0
				? NthVisibleChild(ref platform, header, parent, (uint)sv)
				: NthChild(ref platform, header, parent, (uint)sv);
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
			if (removedAny)
			{
				_ = RefreshSurfaceViewportAfterMutation(ref platform, state, obj);
				Redraw(ref platform, state, obj, header);
			}
			return removedAny;
		}
		var node = ResolveTreeNode(ref platform, state, obj, header, parent,
			treeNode, flags);
		if (node.IsNull) return false;
		RemoveNode(ref platform, state, obj, header, node);
		_ = RefreshSurfaceViewportAfterMutation(ref platform, state, obj);
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
			if (any)
			{
				_ = RefreshSurfaceViewportAfterMutation(ref platform, state, obj);
				Redraw(ref platform, state, obj, header);
			}
			return any;
		}
		var node = ResolveTreeNode(ref platform, state, obj, header, parent,
			treeNode, flags);
		if (node.IsNull || (ReadFlags(ref platform, node) & TNF_LIST) == 0)
			return false;
		if (!ApplyOpen(ref platform, state, obj, node, open, openParents))
			return false;
		_ = RefreshSurfaceViewportAfterMutation(ref platform, state, obj);
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

	// MUIM_Draw for the external Listtree class. The neutral renderer keeps
	// presentation policy out of the tree core, but the public DisplayHook still
	// observes the rows in the named visible viewport when one has been laid out.
	// Without a surface rectangle (the pre-layout state), preserve the complete
	// visible-preorder hook walk used by headless callers. No managed render list
	// or private object offsets are introduced; graphics-specific drawing remains
	// a later surface.
	public static bool Draw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint flags) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadLifecycleStateRecord(ref platform, state, obj,
			out var lifecycle)) return false;
		if (lifecycle.Shown == 0) return true;
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		// A Draw packet starts a fresh presentation pass. The previous hook vector
		// is caller-owned guest memory retained only until this pass replaces it.
		FreeDisplaySnapshot(ref platform, state, obj);
		var hasViewport = TryReadSurfaceStateRecord(ref platform, state, obj,
			out var surface) && surface.Height > 0;
		var node = ListFirst(ref platform, header, APTR.Null);
		var remaining = uint.MaxValue;
		if (hasViewport)
		{
			remaining = SurfaceVisibleRows(surface);
		}
		// MorphOS's boolean Listtree Title form is a real title row. It is
		// delivered through DisplayHook with A1 == NULL, just like List's
		// title-hook form, and consumes one row of the laid-out viewport.
		if (ReadRaw(ref platform, state, obj, Title, 0) != 0)
		{
			if (remaining == 0) return true;
			if (!CallDisplayHook(ref platform, state, obj, APTR.Null))
				return false;
			if (hasViewport) remaining--;
		}
		if (hasViewport)
		{
			if (remaining == 0) return true;
			var firstVisible = surface.FirstVisible;
			var skipped = 0u;
			while (node.IsNotNull && skipped++ < MaximumTraversal &&
				firstVisible != 0)
			{
				node = PreorderNext(ref platform, header, node, true);
				firstVisible--;
			}
		}
		uint guard = 0;
		while (node.IsNotNull && guard++ < MaximumTraversal &&
			(!hasViewport || remaining != 0))
		{
			if (!CallDisplayHook(ref platform, state, obj, node)) return false;
			node = PreorderNext(ref platform, header, node, true);
			if (hasViewport) remaining--;
		}
		return true;
	}

	// MUIM_HandleInput for the external Listtree class.  MorphOS delivers a
	// standard collection input packet containing an IntuiMessage pointer and a
	// preprocessed MUIKEY. Right/Left open and close the active node, while the
	// vertical keys walk the visible preorder display list. The IntuiMessage
	// remains a named ABI field in the packet; the MUIKEY_NONE path decodes that
	// field into a named pointer record for the SELECTUP hit-test below.
	public static bool HandleInput<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR intuiMessage, int muiKey)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		_ = intuiMessage;
		if (TryReadLifecycleStateRecord(ref platform, state, obj,
			out var lifecycle) && lifecycle.Shown == 0)
			return false;
		if (muiKey == KeyNone)
		{
			if (!MuiIntuiMessageCodec.TryReadPointer(ref platform, intuiMessage,
				out var pointer)) return false;
			return HandlePointerInput(ref platform, state, obj, pointer);
		}
		if (muiKey == KeyPress || muiKey == KeyToggle)
			return ApplyKeyboardSelection(ref platform, state, obj,
				muiKey == KeyToggle);
		if (muiKey == KeyUp || muiKey == KeyDown || muiKey == KeyPageUp ||
			muiKey == KeyPageDown || muiKey == KeyTop || muiKey == KeyBottom)
			return NavigateVisibleInput(ref platform, state, obj, muiKey);
		if (muiKey != KeyLeft && muiKey != KeyRight) return false;
		var active = ActiveNode(ref platform, state, obj);
		if (active.IsNull) return false;
		var flags = NodeFlags(ref platform, active);
		if ((flags & TNF_LIST) == 0 || (flags & TNF_FROZEN) != 0)
			return false;
		var open = muiKey == KeyRight;
		if (open == ((flags & TNF_OPEN) != 0)) return false;
		var selector = APTR.FromPointer(unchecked((uint)ListNodeRoot));
		var activeSelector = APTR.FromPointer(unchecked((uint)TreeNodeActive));
		return open
			? Open(ref platform, state, obj, selector, activeSelector, 0)
			: Close(ref platform, state, obj, selector, activeSelector, 0);
	}

	// Inherited Listview navigation moves the Listtree cursor through the
	// visible preorder display list. The tree remains the source of truth: the
	// target is resolved from named node records and published through the same
	// Active setter used by pointer selection and Open/Close.
	internal static bool NavigateVisibleInput<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int muiKey)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		var count = VisibleCount(ref platform, state, obj);
		if (count == 0) return false;
		var active = ActiveNode(ref platform, state, obj);
		var current = active.IsNull ? -1 : unchecked((int)
			VisibleIndexOf(ref platform, header, active));
		if (current < 0 || (uint)current >= count) current = -1;
		var page = 1;
		if (TryReadSurfaceStateRecord(ref platform, state, obj, out var surface) &&
			surface.Height > 0)
		{
			var pageRows = SurfaceDataRows(ref platform, state, obj, surface);
			page = pageRows == 0 ? 1 : pageRows > int.MaxValue
				? int.MaxValue : unchecked((int)pageRows);
		}
		var target = current < 0 ? 0 : muiKey switch
		{
			KeyUp => current - 1,
			KeyDown => current + 1,
			KeyPageUp => current - page,
			KeyPageDown => current + page,
			KeyTop => 0,
			KeyBottom => unchecked((int)count) - 1,
			_ => current,
		};
		if (target < 0) target = 0;
		if ((uint)target >= count) target = unchecked((int)count) - 1;
		var node = NthVisible(ref platform, header, unchecked((uint)target));
		return node.IsNotNull && (active.IsNull || active.Raw != node.Raw) &&
			SetActive(ref platform, state, obj, node, true);
	}

	// Keyboard activation follows the inherited Listview convention: PRESS
	// selects the active node exclusively, while TOGGLE flips that node only
	// when MultiSelect is enabled.  Reuse the same typed node state and anchor
	// policy as pointer selection so keyboard and pointer actions cannot drift.
	private static bool ApplyKeyboardSelection<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, bool toggle)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var active = ActiveNode(ref platform, state, obj);
		if (active.IsNull) return false;
		return ApplyPointerSelection(ref platform, state, obj, active,
			toggle ? QualifierControl : (ushort)0);
	}

	// Focused freestanding seam for native qualification. The live dispatcher
	// reaches this through HandleInput; keeping the typed operation available
	// separately lets a small closure prove keyboard selection without pulling
	// in the complete pointer packet decoder.
	public static bool HandleKeyboardSelection<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, bool toggle)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		ApplyKeyboardSelection(ref platform, state, obj, toggle);

	private static APTR SelectionAnchor<TPlatform>(ref TPlatform platform,
		APTR header) where TPlatform : struct, IMuiGuestMemory
	{
		var node = ListFirst(ref platform, header, APTR.Null);
		uint guard = 0;
		while (node.IsNotNull && guard++ < MaximumTraversal)
		{
			if (ReadNodeNumber(ref platform, node, NodeField.Reserved1) != 0)
				return node;
			node = PreorderNext(ref platform, header, node, false);
		}
		return APTR.Null;
	}

	private static bool ClearTreeSelection<TPlatform>(ref TPlatform platform,
		APTR header) where TPlatform : struct, IMuiGuestMemory
	{
		var node = ListFirst(ref platform, header, APTR.Null);
		uint guard = 0;
		var changed = false;
		while (node.IsNotNull && guard++ < MaximumTraversal)
		{
			if (ReadNodeNumber(ref platform, node, NodeField.Reserved0) != 0 ||
				ReadNodeNumber(ref platform, node, NodeField.Reserved1) != 0)
				changed = true;
			WriteSelectionNumber(ref platform, node, 0, NodeField.Reserved0);
			WriteSelectionNumber(ref platform, node, 0, NodeField.Reserved1);
			node = PreorderNext(ref platform, header, node, false);
		}
		return changed;
	}

	private static bool SetSelectionAnchor<TPlatform>(ref TPlatform platform,
		APTR header, APTR node) where TPlatform : struct, IMuiGuestMemory
	{
		var changed = ClearSelectionAnchors(ref platform, header);
		if (node.IsNotNull && ReadNodeNumber(ref platform, node,
			NodeField.Reserved1) == 0)
		{
			WriteSelectionNumber(ref platform, node, NodeAnchorFlag,
				NodeField.Reserved1);
			changed = true;
		}
		return changed;
	}

	private static bool ClearSelectionAnchors<TPlatform>(ref TPlatform platform,
		APTR header) where TPlatform : struct, IMuiGuestMemory
	{
		var node = ListFirst(ref platform, header, APTR.Null);
		uint guard = 0;
		var changed = false;
		while (node.IsNotNull && guard++ < MaximumTraversal)
		{
			if (ReadNodeNumber(ref platform, node, NodeField.Reserved1) != 0)
			{
				WriteSelectionNumber(ref platform, node, 0, NodeField.Reserved1);
				changed = true;
			}
			node = PreorderNext(ref platform, header, node, false);
		}
		return changed;
	}

	private static bool ApplyPointerSelection<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR node, ushort qualifier)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull || node.IsNull) return false;
		var multi = ReadRaw(ref platform, state, obj, MultiSelect, 0) != 0;
		var shift = (qualifier & QualifierShift) != 0;
		var control = (qualifier & QualifierControl) != 0;
		var changed = false;
		var anchor = SelectionAnchor(ref platform, header);

		if (!multi || (!shift && !control))
		{
			changed = ClearTreeSelection(ref platform, header);
			WriteSelectionNumber(ref platform, node, NodeSelectedFlag,
				NodeField.Reserved0);
			changed = true;
			changed |= SetSelectionAnchor(ref platform, header, node);
		}
		else if (control && !shift)
		{
			var selected = IsNodeSelected(ref platform, node);
			WriteSelectionNumber(ref platform, node, selected ? 0u : NodeSelectedFlag,
				NodeField.Reserved0);
			changed = true;
			changed |= SetSelectionAnchor(ref platform, header, node);
		}
		else if (shift && anchor.IsNotNull)
		{
			var anchorIndex = VisibleIndexOf(ref platform, header, anchor);
			var targetIndex = VisibleIndexOf(ref platform, header, node);
			if (anchorIndex == uint.MaxValue || targetIndex == uint.MaxValue)
			{
				changed = ClearTreeSelection(ref platform, header);
				WriteSelectionNumber(ref platform, node, NodeSelectedFlag,
					NodeField.Reserved0);
				changed = true;
				changed |= SetSelectionAnchor(ref platform, header, node);
			}
			else
			{
				if (!control) changed |= ClearTreeSelection(ref platform, header);
				var first = anchorIndex < targetIndex ? anchorIndex : targetIndex;
				var last = anchorIndex > targetIndex ? anchorIndex : targetIndex;
				for (var index = first; index <= last; index++)
				{
					var item = NthVisible(ref platform, header, index);
					if (item.IsNull) break;
					if (ReadNodeNumber(ref platform, item,
						NodeField.Reserved0) == 0)
					{
						WriteSelectionNumber(ref platform, item, NodeSelectedFlag,
							NodeField.Reserved0);
						changed = true;
					}
				}
			}
		}
		else
		{
			changed = ClearTreeSelection(ref platform, header);
			WriteSelectionNumber(ref platform, node, NodeSelectedFlag,
				NodeField.Reserved0);
			changed = true;
			changed |= SetSelectionAnchor(ref platform, header, node);
		}

		if (changed) Redraw(ref platform, state, obj, header);
		return true;
	}

	// Intuition double-click recognition for Listtree.  Recognition is
	// deliberately timestamp based and bounded: a message without Seconds/
	// Micros is always a single click and a third click starts the following pair.
	private static bool IsDoubleClickWindow(MuiListtreeClickState previous,
		uint previousColumn, uint previousColumnValid,
		MuiIntuiPointerMessage pointer, APTR node, uint column)
	{
		if (previous.TimestampValid == 0 || pointer.TimestampValid == 0 ||
			previous.Clicks != 1 ||
			previous.LastNode.IsNull || previous.LastNode.Raw != node.Raw ||
			previousColumnValid == 0 || previousColumn != column)
			return false;
		var seconds = pointer.Seconds - previous.LastSeconds;
		if (seconds > 1) return false;
		if (seconds == 0)
			return pointer.Micros >= previous.LastMicros &&
				pointer.Micros - previous.LastMicros <= DoubleClickWindowMicros;
		// A one-second boundary is valid only when the microsecond counter wraps.
		if (pointer.Micros >= previous.LastMicros) return false;
		return 1000000u - previous.LastMicros + pointer.Micros <=
			DoubleClickWindowMicros;
	}

	private static bool IsDoubleClickColumn<TPlatform>(int policy,
		uint column, ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (policy == DoubleClickOff) return false;
		if (policy == DoubleClickAll) return true;
		if (policy == DoubleClickTree)
		{
			// MorphOS exposes TreeColumn as a BOOL in the MorphOS 3.20 header.
			// Its normalized value therefore selects the first or second FORMAT
			// column; a one-column format always resolves back to column zero.
			var treeColumn = ReadRaw(ref platform, state, obj, TreeColumn, 0) == 0
				? 0u : 1u;
			var columns = ListtreeFormatColumnCount(ref platform, state, obj);
			if (columns == 0 || treeColumn >= columns) treeColumn = 0;
			return column == treeColumn;
		}
		if (policy < 0) return false;
		return column == unchecked((uint)policy);
	}

	private static void NotifyDoubleClick<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR node)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sourceRecord = MuiHeadlessObjectCore.FindObject(ref platform,
			state, obj);
		if (sourceRecord.IsNull) return;
		MuiNotifyCore.DispatchAttributeChange(ref platform, state,
			sourceRecord, DoubleClick, node.Raw);
	}

	private static bool ApplyPointerDoubleClick<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		APTR node, uint column, MuiIntuiPointerMessage pointer)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureClickStateRecord(ref platform, state, obj) ||
			!TryReadClickStateRecord(ref platform, state, obj, out var previous) ||
			!EnsureClickColumnStateRecord(ref platform, state, obj) ||
			!TryReadClickColumnStateRecord(ref platform, state, obj,
				out var previousColumnState))
			return false;
		var policy = unchecked((int)ReadPolicy(ref platform, state, obj,
			DoubleClick, unchecked((uint)DoubleClickOff)));
		var withinWindow = IsDoubleClickWindow(previous,
			previousColumnState.LastColumn, previousColumnState.Valid, pointer,
			node, column);
		var selectedColumn = withinWindow && IsDoubleClickColumn(policy, column,
			ref platform, state, obj);
		var isDouble = selectedColumn;
		var value = previous;
		value.Magic = MuiListtreeClickState.Cookie;
		value.LastNode = node;
		value.LastSeconds = pointer.Seconds;
		value.LastMicros = pointer.Micros;
		value.TimestampValid = pointer.TimestampValid;
		value.Clicks = isDouble ? 2u : 1u;
		value.DoubleClick = isDouble ? 1u : 0u;
		var columnValue = previousColumnState;
		columnValue.Magic = MuiListtreeClickColumnState.Cookie;
		columnValue.LastColumn = column;
		columnValue.Valid = 1;
		if (!WriteClickStateRecord(ref platform, state, obj, value) ||
			!WriteClickColumnStateRecord(ref platform, state, obj, columnValue))
			return false;
		if (!withinWindow) return true;

		var flags = NodeFlags(ref platform, node);
		if ((flags & TNF_FROZEN) != 0) return true;
		// MorphOS Listtree reports the double-clicked node through the
		// MUIA_Listtree_DoubleClick notification on leaves and on node columns
		// that are not selected by the double-click policy.  A selected list
		// column keeps the traditional open/close action below.  The trigger is
		// the guest node pointer, so delivery crosses the existing typed
		// notification seam without introducing an offset-based ABI record.
		if (!selectedColumn || (flags & TNF_LIST) == 0)
		{
			NotifyDoubleClick(ref platform, state, obj, node);
			return true;
		}

		var open = (flags & TNF_OPEN) == 0;
		var changed = ApplyOpen(ref platform, state, obj, node, open, false);
		if (changed)
		{
			var header = Header(ref platform, state, obj);
			if (header.IsNull) return false;
			Redraw(ref platform, state, obj, header);
		}
		return true;
	}

	internal static bool HandlePointerInput<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiIntuiPointerMessage pointer)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (pointer.Class == IdcmpMouseMove && pointer.Code == MouseMove)
			return TrackPointerDrag(ref platform, state, obj, pointer);
		if (pointer.Class != IdcmpMouseButtons) return false;
		if (pointer.Code == SelectDown)
		{
			if (!TryHitVisibleNode(ref platform, state, obj, pointer.MouseX,
				pointer.MouseY, out var source, out _)) return false;
			var active = SetAttribute(ref platform, state, obj, Active,
				source.Raw, true);
			if (!active) return false;
			if (ReadPolicy(ref platform, state, obj, DragDropSort, 1) == 0)
				return true;
			if (!ApplyPointerSelection(ref platform, state, obj, source,
				pointer.Qualifier)) return false;
			var drag = default(MuiListviewDragState);
			drag.Magic = MuiListviewDragStateCodec.Cookie;
			drag.Source = unchecked((int)VisibleIndexOf(ref platform,
				Header(ref platform, state, obj), source));
			drag.Target = drag.Source;
			drag.Flags = MuiListviewDragState.ActiveFlag |
				MuiListviewDragState.CapturedFlag;
			return WriteDragStateRecord(ref platform, state, obj, drag);
		}
		if (pointer.Code != SelectUp) return false;
		if (TryReadDragStateRecord(ref platform, state, obj, out var dragState) &&
			(dragState.Flags & MuiListviewDragState.ActiveFlag) != 0)
		{
			var clickHandled = true;
			// Resolve and commit the typed plan before clearing the capture. The
			// source/target remain row indices in the compact header snapshot, so
			// all node pointers are resolved from the named tree records while the
			// topology is still unchanged.
			if ((dragState.Flags & MuiListviewDragState.MovedFlag) != 0 &&
				TryBuildDragCommitPlan(ref platform, state, obj, dragState,
				out var plan))
				_ = CommitDragPlan(ref platform, state, obj, plan);
			else if ((dragState.Flags & MuiListviewDragState.MovedFlag) == 0)
			{
				var captureHeader = Header(ref platform, state, obj);
				var clickNode = captureHeader.IsNull || dragState.Source < 0
					? APTR.Null
					: NthVisible(ref platform, captureHeader,
						unchecked((uint)dragState.Source));
				if (clickNode.IsNull) clickHandled = false;
				else if (!TryReadSurfaceStateRecord(ref platform, state, obj,
					out var clickSurface) || !TryGetPointerColumn(ref platform, state,
					obj, clickSurface, pointer.MouseX, out var clickColumn)) clickHandled = false;
				else clickHandled = ApplyPointerDoubleClick(ref platform, state,
					obj, clickNode, clickColumn, pointer);
			}
			dragState.Flags = 0;
			dragState.Source = -1;
			dragState.Target = -1;
			var cleared = WriteDragStateRecord(ref platform, state, obj, dragState);
			var header = Header(ref platform, state, obj);
			var cueCleared = !header.IsNull && WriteHeaderDrop(ref platform,
				header, -1, DropMarkNone);
			// A captured pointer-up is consumed even when the requested move is
			// rejected (for example, a cycle); the tree remains unchanged in that
			// case, but the transient state is always released.
			return cleared && cueCleared && clickHandled;
		}
		if (!TryHitVisibleNode(ref platform, state, obj, pointer.MouseX,
			pointer.MouseY, out var hit, out var hitColumn)) return false;
		if (!SetAttribute(ref platform, state, obj, Active, hit.Raw, true))
			return false;
		if (!ApplyPointerSelection(ref platform, state, obj, hit,
			pointer.Qualifier)) return false;
		return ApplyPointerDoubleClick(ref platform, state, obj, hit,
			hitColumn, pointer);
	}

	private static bool TryBuildDragCommitPlan<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiListviewDragState drag,
		out MuiListtreeDragCommitPlan plan)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		plan = default;
		if (drag.Source < 0 || drag.Target < 0) return false;
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		var source = NthVisible(ref platform, header, unchecked((uint)drag.Source));
		var target = NthVisible(ref platform, header, unchecked((uint)drag.Target));
		if (source.IsNull || target.IsNull || source.Raw == target.Raw)
			return false;
		if (!TryGetHeaderStateRecord(ref platform, state, obj,
			out var headerState)) return false;
		var mark = headerState.DropValue;
		if (mark != DropMarkAbove && mark != DropMarkBelow &&
			mark != DropMarkOnto) return false;

		var sourceParent = ReadNodeParent(ref platform, source);
		var targetParent = ReadNodeParent(ref platform, target);
		plan.OldListNode = sourceParent;
		plan.OldTreeNode = source;
		plan.NewListNode = targetParent;
		plan.DropMark = mark;
		if (mark == DropMarkOnto)
		{
			plan.NewListNode = target;
			plan.NewTreeNode = APTR.FromPointer(unchecked((uint)PrevNodeTail));
			return true;
		}

		if (mark == DropMarkBelow)
		{
			plan.NewTreeNode = target;
			return true;
		}

		// Move inserts after NewTreeNode. For an Above mark, use the target's
		// previous sibling; if the source is that sibling, use its own previous
		// sibling so the immediate-next drop remains a stable no-op.
		var anchor = ReadNodePrevious(ref platform, target);
		if (anchor.Raw == source.Raw)
			anchor = ReadNodePrevious(ref platform, source);
		plan.NewTreeNode = anchor.IsNull
			? APTR.FromPointer(unchecked((uint)PrevNodeHead))
			: anchor;
		return true;
	}

	private static bool CommitDragPlan<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiListtreeDragCommitPlan plan)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Move(ref platform, state, obj, plan.OldListNode, plan.OldTreeNode,
			plan.NewListNode, plan.NewTreeNode, 0);

	// MOUSEMOVE updates a guest-resident capture record and publishes the same
	// drop mark that MUIM_Listtree_SetDropMark consumes. Every traversal remains
	// bounded by the existing visible-list walk.
	private static bool TrackPointerDrag<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiIntuiPointerMessage pointer)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadDragStateRecord(ref platform, state, obj, out var drag) ||
			(drag.Flags & MuiListviewDragState.ActiveFlag) == 0) return false;
		// The compact header snapshot deliberately retains source/target/flags
		// only. The first MOUSEMOVE is therefore the bounded transition that
		// marks the capture as moved; precise distance thresholds can be added
		// when the full MorphOS drag policy is implemented.
		drag.Flags |= MuiListviewDragState.MovedFlag;
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		if (!TryHitVisibleNode(ref platform, state, obj, pointer.MouseX,
			pointer.MouseY, out var target, out _))
		{
			drag.Target = -1;
			var clear = WriteHeaderDrop(ref platform, header, -1, DropMarkNone);
			return clear && WriteDragStateRecord(ref platform, state, obj, drag);
		}
		var index = unchecked((int)VisibleIndexOf(ref platform, header, target));
		var mark = DropMarkForPointer(ref platform, state, obj, pointer.MouseY);
		drag.Target = index;
		var written = WriteHeaderDrop(ref platform, header, index, mark);
		return written && WriteDragStateRecord(ref platform, state, obj, drag);
	}

	private static uint DropMarkForPointer<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int y)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSurfaceStateRecord(ref platform, state, obj,
			out var surface)) return DropMarkOnto;
		var rowHeight = surface.RowHeight == 0 ? DefaultRowHeight :
			surface.RowHeight;
		var offset = unchecked((uint)y) % rowHeight;
		var edge = rowHeight / 4;
		if (edge != 0 && offset < edge) return DropMarkAbove;
		if (edge != 0 && offset >= rowHeight - edge) return DropMarkBelow;
		return DropMarkOnto;
	}

	// Pointer coordinates are object-local, matching the shared List hit-test
	// contract.  The named Listtree surface record supplies the viewport and
	// row metric; no private geometry offsets are introduced at this boundary.
	private static bool TryGetPointerColumn<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiListtreeSurfaceStateRecord surface, int x,
		out uint column)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		column = 0;
		if (surface.Width <= 0 || x < 0 || x >= surface.Width)
			return false;
		return TryResolvePointerColumn(ref platform, state, obj,
			unchecked((uint)surface.Width), unchecked((uint)x), out column);
	}

	// FORMAT is a bounded, caller-owned ReadArgs string.  Listtree reserves one
	// column for the tree indicator/name, but the remaining visible columns are
	// still separated by commas.  Count those separators without retaining a
	// managed string or array; quoted commas and DOS '*' escapes are data.
	private static uint ListtreeFormatColumnCount<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var format = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			Format, 0));
		if (format.IsNull) return 1;
		if (!TryReadCStringLength(ref platform, format, MaximumStringLength,
			out var length)) return 1;
		var count = 1u;
		var quoted = 0u;
		for (var index = 0u; index < length; index++)
		{
			var value = platform.ReadUInt8(format, unchecked((int)index));
			if (quoted != 0)
			{
				if (value == (byte)'*')
				{
					if (index + 1 >= length) return 1;
					index++;
					continue;
				}
				if (value == (byte)'"') quoted = 0;
				continue;
			}
			if (value == (byte)'"')
			{
				quoted = 1;
				continue;
			}
			if (value == (byte)',' && count < MaximumFormatColumns) count++;
		}
		return quoted == 0 ? count : 1u;
	}

	private static bool IsListtreeFormatSpace(byte value) =>
		value == (byte)' ' || value == (byte)'\t';

	private static byte UpperAscii(byte value) => value >= (byte)'a' &&
		value <= (byte)'z' ? (byte)(value - ((byte)'a' - (byte)'A')) : value;

	private static bool IsListtreeDeltaKey<TPlatform>(ref TPlatform platform,
		APTR format, int start, int end)
		where TPlatform : struct, IMuiGuestMemory
	{
		var length = end - start;
		if (length == 1)
			return UpperAscii(platform.ReadUInt8(format, start)) == (byte)'D';
		if (length != 5) return false;
		return UpperAscii(platform.ReadUInt8(format, start)) == (byte)'D' &&
			UpperAscii(platform.ReadUInt8(format, start + 1)) == (byte)'E' &&
			UpperAscii(platform.ReadUInt8(format, start + 2)) == (byte)'L' &&
			UpperAscii(platform.ReadUInt8(format, start + 3)) == (byte)'T' &&
			UpperAscii(platform.ReadUInt8(format, start + 4)) == (byte)'A';
	}

	private static bool IsListtreeWeightKey<TPlatform>(ref TPlatform platform,
		APTR format, int start, int end)
		where TPlatform : struct, IMuiGuestMemory
	{
		var length = end - start;
		if (length == 1)
			return UpperAscii(platform.ReadUInt8(format, start)) == (byte)'W';
		if (length != 6) return false;
		return UpperAscii(platform.ReadUInt8(format, start)) == (byte)'W' &&
			UpperAscii(platform.ReadUInt8(format, start + 1)) == (byte)'E' &&
			UpperAscii(platform.ReadUInt8(format, start + 2)) == (byte)'I' &&
			UpperAscii(platform.ReadUInt8(format, start + 3)) == (byte)'G' &&
			UpperAscii(platform.ReadUInt8(format, start + 4)) == (byte)'H' &&
			UpperAscii(platform.ReadUInt8(format, start + 5)) == (byte)'T';
	}

	private static bool IsListtreeMinWidthKey<TPlatform>(ref TPlatform platform,
		APTR format, int start, int end)
		where TPlatform : struct, IMuiGuestMemory
	{
		var length = end - start;
		if (length == 3)
			return UpperAscii(platform.ReadUInt8(format, start)) == (byte)'M' &&
				UpperAscii(platform.ReadUInt8(format, start + 1)) == (byte)'I' &&
				UpperAscii(platform.ReadUInt8(format, start + 2)) == (byte)'W';
		if (length != 8) return false;
		return UpperAscii(platform.ReadUInt8(format, start)) == (byte)'M' &&
			UpperAscii(platform.ReadUInt8(format, start + 1)) == (byte)'I' &&
			UpperAscii(platform.ReadUInt8(format, start + 2)) == (byte)'N' &&
			UpperAscii(platform.ReadUInt8(format, start + 3)) == (byte)'W' &&
			UpperAscii(platform.ReadUInt8(format, start + 4)) == (byte)'I' &&
			UpperAscii(platform.ReadUInt8(format, start + 5)) == (byte)'D' &&
			UpperAscii(platform.ReadUInt8(format, start + 6)) == (byte)'T' &&
			UpperAscii(platform.ReadUInt8(format, start + 7)) == (byte)'H';
	}

	private static bool IsListtreeMaxWidthKey<TPlatform>(ref TPlatform platform,
		APTR format, int start, int end)
		where TPlatform : struct, IMuiGuestMemory
	{
		var length = end - start;
		if (length == 3)
			return UpperAscii(platform.ReadUInt8(format, start)) == (byte)'M' &&
				UpperAscii(platform.ReadUInt8(format, start + 1)) == (byte)'A' &&
				UpperAscii(platform.ReadUInt8(format, start + 2)) == (byte)'W';
		if (length != 8) return false;
		return UpperAscii(platform.ReadUInt8(format, start)) == (byte)'M' &&
			UpperAscii(platform.ReadUInt8(format, start + 1)) == (byte)'A' &&
			UpperAscii(platform.ReadUInt8(format, start + 2)) == (byte)'X' &&
			UpperAscii(platform.ReadUInt8(format, start + 3)) == (byte)'W' &&
			UpperAscii(platform.ReadUInt8(format, start + 4)) == (byte)'I' &&
			UpperAscii(platform.ReadUInt8(format, start + 5)) == (byte)'D' &&
			UpperAscii(platform.ReadUInt8(format, start + 6)) == (byte)'T' &&
			UpperAscii(platform.ReadUInt8(format, start + 7)) == (byte)'H';
	}

	private static bool TryReadListtreeFormatValueEnd<TPlatform>(
		ref TPlatform platform, APTR format, int start, int end,
		out int valueEnd)
		where TPlatform : struct, IMuiGuestMemory
	{
		valueEnd = start;
		if (start >= end) return false;
		if (platform.ReadUInt8(format, start) == (byte)'"')
		{
			var cursor = start + 1;
			while (cursor < end)
			{
				var value = platform.ReadUInt8(format, cursor++);
				if (value == (byte)'*')
				{
					if (cursor >= end) return false;
					cursor++;
					continue;
				}
				if (value == (byte)'"')
				{
					valueEnd = cursor;
					return true;
				}
			}
			return false;
		}
		var cursorUnquoted = start;
		while (cursorUnquoted < end && !IsListtreeFormatSpace(
			platform.ReadUInt8(format, cursorUnquoted))) cursorUnquoted++;
		if (cursorUnquoted == start) return false;
		valueEnd = cursorUnquoted;
		return true;
	}

	private static bool TryParseListtreeFormatNumber<TPlatform>(
		ref TPlatform platform, APTR format, int start, int end,
		out int number)
		where TPlatform : struct, IMuiGuestMemory
	{
		number = 0;
		while (start < end && IsListtreeFormatSpace(platform.ReadUInt8(
			format, start))) start++;
		if (start >= end) return false;
		var negative = platform.ReadUInt8(format, start) == (byte)'-';
		if (negative) start++;
		if (start >= end) return false;
		var digits = 0;
		var value = 0;
		while (start < end)
		{
			var digit = platform.ReadUInt8(format, start);
			if (digit < (byte)'0' || digit > (byte)'9') break;
			if (value > (int.MaxValue - (digit - (byte)'0')) / 10)
				return false;
			value = value * 10 + digit - (byte)'0';
			digits++;
			start++;
		}
		while (start < end && IsListtreeFormatSpace(platform.ReadUInt8(
			format, start))) start++;
		if (digits == 0 || start != end) return false;
		number = negative ? -value : value;
		return true;
	}

	private static bool TryParseListtreeWidth<TPlatform>(ref TPlatform platform,
		APTR format, int start, int end, out uint width, out uint flags,
		uint pixelFlag, uint contentFlag)
		where TPlatform : struct, IMuiGuestMemory
	{
		width = uint.MaxValue;
		flags = 0;
		var pixels = end - start >= 2 &&
			UpperAscii(platform.ReadUInt8(format, end - 2)) == (byte)'P' &&
			UpperAscii(platform.ReadUInt8(format, end - 1)) == (byte)'X';
		var numberEnd = pixels ? end - 2 : end;
		if (!TryParseListtreeFormatNumber(ref platform, format, start,
			numberEnd, out var number) || number < -1) return false;
		if (number == -1)
		{
			flags = contentFlag;
			width = uint.MaxValue;
			return true;
		}
		width = unchecked((uint)number);
		if (pixels) flags = pixelFlag;
		return true;
	}

	private static bool ParseListtreeFormatSegment<TPlatform>(
		ref TPlatform platform, APTR format, int start, int end,
		ref MuiListtreeColumnGeometryRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = start;
		while (cursor < end)
		{
			while (cursor < end && IsListtreeFormatSpace(platform.ReadUInt8(
				format, cursor))) cursor++;
			if (cursor >= end) break;
			var keyStart = cursor;
			while (cursor < end && !IsListtreeFormatSpace(platform.ReadUInt8(
				format, cursor)) && platform.ReadUInt8(format, cursor) !=
				(byte)'=') cursor++;
			var keyEnd = cursor;
			while (cursor < end && IsListtreeFormatSpace(platform.ReadUInt8(
				format, cursor))) cursor++;
			var hasValue = false;
			var valueStart = cursor;
			var valueEnd = cursor;
			if (cursor < end && platform.ReadUInt8(format, cursor) == (byte)'=')
			{
				hasValue = true;
				valueStart = ++cursor;
				while (valueStart < end && IsListtreeFormatSpace(
					platform.ReadUInt8(format, valueStart))) valueStart++;
				if (!TryReadListtreeFormatValueEnd(ref platform, format,
					valueStart, end, out valueEnd)) return false;
			}
			else if (IsListtreeDeltaKey(ref platform, format, keyStart, keyEnd) ||
				IsListtreeWeightKey(ref platform, format, keyStart, keyEnd) ||
				IsListtreeMinWidthKey(ref platform, format, keyStart, keyEnd) ||
				IsListtreeMaxWidthKey(ref platform, format, keyStart, keyEnd))
			{
				hasValue = true;
				valueStart = cursor;
				if (!TryReadListtreeFormatValueEnd(ref platform, format,
					valueStart, end, out valueEnd)) return false;
			}
			else
			{
				// Bare labels and switches (for example BAR or SORTABLE) do not
				// affect Listtree hit geometry. Leave their value untouched.
				cursor = keyEnd;
				continue;
			}
			if (hasValue)
			{
				var numberEnd = valueEnd;
				var numberStart = valueStart;
				if (numberStart < numberEnd && platform.ReadUInt8(format,
					numberStart) == (byte)'"')
				{
					numberStart++;
					numberEnd--;
				}
				if (IsListtreeDeltaKey(ref platform, format, keyStart, keyEnd))
				{
					if (!TryParseListtreeFormatNumber(ref platform, format,
						numberStart, numberEnd, out var delta) || delta < 0)
						return false;
					value.Delta = unchecked((uint)delta);
				}
				else if (IsListtreeWeightKey(ref platform, format,
					keyStart, keyEnd))
				{
					if (!TryParseListtreeFormatNumber(ref platform, format,
						numberStart, numberEnd, out var weight) || weight < -1)
						return false;
					// WEIGHT=-1 is the content-weight mode. Until Listtree has a
					// font/measurement seam, retain a stable proportional fallback.
					value.Weight = weight < 0 ? 100u : unchecked((uint)weight);
				}
				else if (IsListtreeMinWidthKey(ref platform, format,
					keyStart, keyEnd))
				{
					if (!TryParseListtreeWidth(ref platform, format, numberStart,
						numberEnd, out value.MinWidth, out var minFlags,
						MuiListtreeColumnGeometryRecord.MinPixel,
						MuiListtreeColumnGeometryRecord.MinContent)) return false;
					value.Flags &= ~(MuiListtreeColumnGeometryRecord.MinPixel |
						MuiListtreeColumnGeometryRecord.MinContent);
					value.Flags |= minFlags;
				}
				else if (IsListtreeMaxWidthKey(ref platform, format,
					keyStart, keyEnd))
				{
					if (!TryParseListtreeWidth(ref platform, format, numberStart,
						numberEnd, out value.MaxWidth, out var maxFlags,
						MuiListtreeColumnGeometryRecord.MaxPixel,
						MuiListtreeColumnGeometryRecord.MaxContent)) return false;
					value.Flags &= ~(MuiListtreeColumnGeometryRecord.MaxPixel |
						MuiListtreeColumnGeometryRecord.MaxContent);
					value.Flags |= maxFlags;
				}
			}
			cursor = hasValue ? valueEnd : keyEnd;
		}
		return true;
	}

	private static uint ResolveListtreePercentageWidth(uint width,
		uint percentage)
	{
		if (width == 0 || percentage >= 100) return width;
		var whole = width / 100u;
		var remainder = width % 100u;
		return whole * percentage + remainder * percentage / 100u;
	}

	private static uint ResolveListtreeWidthLimit(
		MuiListtreeColumnGeometryRecord value, bool minimum, uint width)
	{
		var raw = minimum ? value.MinWidth : value.MaxWidth;
		var pixelFlag = minimum ? MuiListtreeColumnGeometryRecord.MinPixel :
			MuiListtreeColumnGeometryRecord.MaxPixel;
		var contentFlag = minimum ? MuiListtreeColumnGeometryRecord.MinContent :
			MuiListtreeColumnGeometryRecord.MaxContent;
		if (raw == uint.MaxValue)
		{
			if ((value.Flags & contentFlag) != 0)
				return minimum ? 0u : width;
			return minimum ? 0u : uint.MaxValue;
		}
		if ((value.Flags & pixelFlag) != 0) return raw;
		return ResolveListtreePercentageWidth(width, raw);
	}

	private static uint EffectiveListtreeWeight(uint weight) =>
		weight == 0 ? 1u : weight;

	private static bool TryBuildColumnGeometry<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint width, out APTR block, out uint columns)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		block = APTR.Null;
		columns = ListtreeFormatColumnCount(ref platform, state, obj);
		if (columns == 0 || columns > MuiListtreeColumnGeometryCursor.MaximumEntries)
			columns = 1;
		if (columns > uint.MaxValue / MuiListtreeColumnGeometryRecord.Size)
			return false;
		var bytes = columns * MuiListtreeColumnGeometryRecord.Size;
		block = MuiHeadlessMemory.Allocate(ref platform, bytes);
		if (block.IsNull) return false;
		platform.Clear(block, bytes);
		var cursor = default(MuiListtreeColumnGeometryCursor);
		cursor.Base = block;
		for (var index = 0u; index < columns; index++)
		{
			cursor.Index = index;
			if (!MuiListtreeColumnGeometryCursorCodec.TryGetEntry(ref platform,
				cursor, out var entry))
			{
				platform.Clear(block, bytes);
				platform.Free(block, bytes);
				block = APTR.Null;
				return false;
			}
			var value = default(MuiListtreeColumnGeometryRecord);
			value.Delta = 4;
			value.Weight = 100;
			value.MaxWidth = uint.MaxValue;
			if (!MuiListtreeColumnGeometryCodec.Write(ref platform, entry, value))
			{
				platform.Clear(block, bytes);
				platform.Free(block, bytes);
				block = APTR.Null;
				return false;
			}
		}
		var format = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			Format, 0));
		if (format.IsNotNull && TryReadCStringLength(ref platform, format,
			MaximumStringLength, out var length))
		{
			var segmentStart = 0;
			var ordinal = 0u;
			var quoted = 0u;
			for (var index = 0u; index <= length && ordinal < columns; index++)
			{
				var separator = index == length;
				if (!separator)
				{
					var current = platform.ReadUInt8(format,
						unchecked((int)index));
					if (quoted != 0)
					{
						if (current == (byte)'*')
						{
							if (index + 1 >= length) break;
							index++;
						}
						else if (current == (byte)'"') quoted = 0;
					}
					else if (current == (byte)'"') quoted = 1;
					else if (current == (byte)',') separator = true;
				}
				if (!separator) continue;
				cursor.Index = ordinal;
				if (!MuiListtreeColumnGeometryCursorCodec.TryGetEntry(ref platform,
					cursor, out var entry)) break;
				var value = default(MuiListtreeColumnGeometryRecord);
				if (!MuiListtreeColumnGeometryCodec.TryRead(ref platform, entry,
					out value) || !ParseListtreeFormatSegment(ref platform, format,
					segmentStart, unchecked((int)index), ref value)) break;
				if (!MuiListtreeColumnGeometryCodec.Write(ref platform, entry, value))
					break;
				ordinal++;
				segmentStart = unchecked((int)index) + 1;
			}
		}
		var totalDelta = 0u;
		var totalWeight = 0u;
		for (var index = 0u; index < columns; index++)
		{
			cursor.Index = index;
			if (!MuiListtreeColumnGeometryCursorCodec.TryGetEntry(ref platform,
				cursor, out var entry) ||
				!MuiListtreeColumnGeometryCodec.TryRead(ref platform, entry,
				out var value))
			{
				FreeColumnGeometry(ref platform, block, columns);
				block = APTR.Null;
				return false;
			}
			if (index + 1 < columns)
				totalDelta = totalDelta > uint.MaxValue - value.Delta
					? uint.MaxValue : totalDelta + value.Delta;
			var effectiveWeight = EffectiveListtreeWeight(value.Weight);
			totalWeight = totalWeight > uint.MaxValue - effectiveWeight
				? uint.MaxValue : totalWeight + effectiveWeight;
		}
		if (totalWeight == 0) totalWeight = columns;
		var remaining = width > totalDelta ? width - totalDelta : 0u;
		var remainingWeight = totalWeight;
		for (var index = 0u; index < columns; index++)
		{
			cursor.Index = index;
			if (!MuiListtreeColumnGeometryCursorCodec.TryGetEntry(ref platform,
				cursor, out var entry) ||
				!MuiListtreeColumnGeometryCodec.TryRead(ref platform, entry,
				out var value))
			{
				FreeColumnGeometry(ref platform, block, columns);
				block = APTR.Null;
				return false;
			}
			var effectiveWeight = EffectiveListtreeWeight(value.Weight);
			var share = index + 1 == columns || remainingWeight == 0
				? remaining : remaining * effectiveWeight / remainingWeight;
			var minimum = ResolveListtreeWidthLimit(value, true, width);
			var maximum = ResolveListtreeWidthLimit(value, false, width);
			if (share < minimum) share = minimum;
			if (maximum != uint.MaxValue && share > maximum) share = maximum;
			if (share > remaining) share = remaining;
			value.Width = share;
			if (!MuiListtreeColumnGeometryCodec.Write(ref platform, entry, value))
			{
				FreeColumnGeometry(ref platform, block, columns);
				block = APTR.Null;
				return false;
			}
			remaining -= share;
			remainingWeight = remainingWeight > effectiveWeight
				? remainingWeight - effectiveWeight : 0;
		}
		return true;
	}

	private static void FreeColumnGeometry<TPlatform>(ref TPlatform platform,
		APTR block, uint columns) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull || columns == 0) return;
		var bytes = columns * MuiListtreeColumnGeometryRecord.Size;
		if (platform.IsMapped(block, bytes)) platform.Clear(block, bytes);
		platform.Free(block, bytes);
	}

	private static bool TryResolvePointerColumn<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint width, uint offset, out uint column)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		column = 0;
		if (!TryBuildColumnGeometry(ref platform, state, obj, width,
			out var block, out var columns)) return false;
		var cursor = default(MuiListtreeColumnGeometryCursor);
		cursor.Base = block;
		var boundary = 0u;
		var selected = false;
		for (var index = 0u; index < columns; index++)
		{
			cursor.Index = index;
			if (!MuiListtreeColumnGeometryCursorCodec.TryGetEntry(ref platform,
				cursor, out var entry) ||
				!MuiListtreeColumnGeometryCodec.TryRead(ref platform, entry,
				out var value)) break;
			if (value.Width != 0 && offset < boundary + value.Width)
			{
				column = index;
				selected = true;
				break;
			}
			boundary += value.Width;
			if (index + 1 < columns)
			{
				// DELTA is a visual gap. Treat a pointer in the gap as part of
				// the preceding column, matching List's forgiving hit policy.
				if (offset < boundary + value.Delta)
				{
					column = index;
					selected = true;
					break;
				}
				boundary += value.Delta;
			}
		}
		if (!selected && columns != 0) column = columns - 1;
		FreeColumnGeometry(ref platform, block, columns);
		return selected || columns != 0;
	}

	private static bool TryHitVisibleNode<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int x, int y, out APTR node, out uint column)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		node = APTR.Null;
		column = 0;
		if (!TryReadSurfaceStateRecord(ref platform, state, obj,
			out var surface)) return false;
		var width = surface.Width;
		var height = surface.Height;
		var rowHeight = surface.RowHeight == 0 ? DefaultRowHeight :
			surface.RowHeight;
		if (width <= 0 || height <= 0 || x < 0 || y < 0 ||
			x >= width || y >= height) return false;
		var row = unchecked((uint)y) / rowHeight;
		var titleRows = ReadRaw(ref platform, state, obj, Title, 0) != 0
			? 1u : 0u;
		if (row < titleRows) return false;
		row -= titleRows;
		var count = VisibleCount(ref platform, state, obj);
		if (row >= count || surface.FirstVisible > uint.MaxValue - row ||
			surface.FirstVisible + row >= count) return false;
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		node = NthVisible(ref platform, header, surface.FirstVisible + row);
		if (node.IsNull) return false;
		return TryResolvePointerColumn(ref platform, state, obj,
			unchecked((uint)surface.Width), unchecked((uint)x), out column);
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
			var nodeFlags = ReadFlags(ref platform, node);
			WriteNodePointer(ref platform, node, APTR.Null, NodeField.Next);
			WriteNodePointer(ref platform, node, APTR.Null, NodeField.Previous);
			LinkSorted(ref platform, state, obj, header, parent, node);
			// The topology codec intentionally rewrites a named node record on
			// each link operation. Restore the public flag word through its typed
			// field cursor so Sort never changes TNF_OPEN/TNF_LIST state.
			MuiListtreeNodeFieldCursorCodec.TryWriteUInt16(ref platform, node,
				MuiListtreeNodeField.Flags, (ushort)nodeFlags);
			node = next;
		}
		_ = RefreshSurfaceViewportAfterMutation(ref platform, state, obj);
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
		if ((flags & FlagsNr) != 0 && sv >= 0)
		{
			// Move applies its ordinal flags to both tree-node arguments. The
			// destination is resolved after unlinking, so the ordinal describes
			// the resulting sibling list and a closed visible list has no anchor.
			var anchor = (flags & FlagsVisible) != 0
				? NthVisibleChild(ref platform, header, newParent, (uint)sv)
				: NthChild(ref platform, header, newParent, (uint)sv);
			if (anchor.IsNotNull) LinkAfter(ref platform, header, newParent,
				anchor, node);
			else LinkAsLastChild(ref platform, header, newParent, node);
		}
		else switch (sv)
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
		_ = RefreshSurfaceViewportAfterMutation(ref platform, state, obj);
		Redraw(ref platform, state, obj, header);
		return true;
	}

	public static bool Exchange<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR listNode1, APTR treeNode1, APTR listNode2, APTR treeNode2,
		uint flags) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		if (!ResolveList(ref platform, state, obj, listNode1, out var parent1))
			return false;
		var relative = unchecked((int)treeNode2.Raw);
		APTR parent2 = APTR.Null;
		if (relative != ExchangeTreeNode2Up &&
			relative != ExchangeTreeNode2Down &&
			!ResolveList(ref platform, state, obj, listNode2, out parent2))
			return false;
		var n1 = ResolveTreeNode(ref platform, state, obj, header, parent1,
			treeNode1, flags);
		if (n1.IsNull) return false;
		APTR n2;
		if (relative == ExchangeTreeNode2Up ||
			relative == ExchangeTreeNode2Down)
		{
			// Relative exchange selectors intentionally ignore ListNode2: the
			// sibling list is the one containing the first resolved node.
			var sibling = relative == ExchangeTreeNode2Up
				? ReadNodePrevious(ref platform, n1)
				: ReadNodeNext(ref platform, n1);
			n2 = sibling;
			if (n2.IsNull || ReadNodeParent(ref platform, n2).Raw !=
				ReadNodeParent(ref platform, n1).Raw)
				return false;
		}
		else
		{
			n2 = ResolveTreeNode(ref platform, state, obj, header, parent2,
				treeNode2, flags);
		}
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
		_ = RefreshSurfaceViewportAfterMutation(ref platform, state, obj);
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
			var stored = ConstructUserValue(ref platform, state, obj, newName,
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
		var node = APTR.Null;
		var dropFlags = DropMarkNone;
		// Once Layout has published a real rectangle, TestPos uses the same
		// bounded pixel hit-test as pointer input. This keeps X, viewport origin,
		// row height, and FORMAT column geometry consistent across both entry
		// points. Before Layout, retain the historical headless row-index fallback
		// so callers can still probe a freshly constructed external object.
		if (TryReadSurfaceStateRecord(ref platform, state, obj,
			out var surface) && surface.Width > 0 && surface.Height > 0)
		{
			if (TryHitVisibleNode(ref platform, state, obj, x, y,
				out node, out _))
				dropFlags = DropMarkForPointer(ref platform, state, obj, y);
		}
		else
		{
			var firstVisible = 0u;
			if (TryReadSurfaceStateRecord(ref platform, state, obj,
				out surface)) firstVisible = surface.FirstVisible;
			var row = y < 0 ? -1 : y;
			var titleRows = ReadRaw(ref platform, state, obj, Title, 0) != 0
				? 1 : 0;
			if (row >= 0 && row < titleRows) row = -1;
			else if (row >= titleRows) row -= titleRows;
			node = row < 0 ? APTR.Null
				: firstVisible > uint.MaxValue - unchecked((uint)row)
					? APTR.Null
				: NthVisible(ref platform, header,
					firstVisible + unchecked((uint)row));
			dropFlags = node.IsNull ? DropMarkNone : DropMarkOnto;
		}
		var value = default(MuiListtreeTestPosResult);
		value.TreeNode = node;
		value.Flags = (ushort)dropFlags;
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

	internal static bool TryGetHeaderStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListtreeHeaderState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var header = Header(ref platform, state, obj);
		return !header.IsNull && MuiListtreeHeaderCodec.TryRead(ref platform,
			header, out value);
	}

	public static uint NodeFlags<TPlatform>(ref TPlatform platform, APTR node)
		where TPlatform : struct, IMuiGuestMemory =>
		node.IsNull ? 0 : ReadFlags(ref platform, node);

	// Selection is deliberately kept out of the read-only public node prefix.
	// These typed query helpers expose the state to the host/native qualification
	// seam while leaving the MorphOS TreeNode ABI unchanged.
	public static bool IsNodeSelected<TPlatform>(ref TPlatform platform,
		APTR node) where TPlatform : struct, IMuiGuestMemory =>
		node.IsNotNull && ReadNodeNumber(ref platform, node, NodeField.Reserved0) != 0;

	public static uint SelectedCount<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return 0;
		var node = ListFirst(ref platform, header, APTR.Null);
		uint count = 0;
		uint guard = 0;
		while (node.IsNotNull && guard++ < MaximumTraversal)
		{
			if (IsNodeSelected(ref platform, node)) count++;
			node = PreorderNext(ref platform, header, node, false);
		}
		return count;
	}

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
		// A root list is always visible. A child list contributes visible entries
		// only while its owning node is open; otherwise a visible ordinal has no
		// anchor and callers append according to the public method contract.
		if (parent.IsNotNull && (ReadFlags(ref platform, parent) & TNF_OPEN) == 0)
			return APTR.Null;
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

	private static APTR ConstructInsertUser<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR name, APTR user, APTR listNode, APTR prevNode,
		uint flags, out uint ownership)
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
		// MorphOS passes the typed MUIP_Listtree_Insert packet in A1 and a
		// standard Exec memory-pool handle in A2. The pool is retained in the
		// object's named guest record so every construct/destruct call observes
		// the same native capability for the object's lifetime.
		if (!EnsureHookPoolStateRecord(ref platform, state, obj) ||
			!TryGetHookPool(ref platform, state, obj, out var pool))
			return APTR.Null;
		var message = MuiHeadlessMemory.Allocate(ref platform,
			MuiListtreeInsertMessage.Size);
		if (message.IsNull) return APTR.Null;
		var written = MuiListtreeMessageCodec.WriteInsert(ref platform, message,
			name.Raw, user.Raw, listNode.Raw, prevNode.Raw, flags);
		if (!written)
		{
			platform.Clear(message, MuiListtreeInsertMessage.Size);
			platform.Free(message, MuiListtreeInsertMessage.Size);
			return APTR.Null;
		}
		var result = platform.InvokeHook(APTR.FromPointer(hook), pool,
			message);
		platform.Clear(message, MuiListtreeInsertMessage.Size);
		platform.Free(message, MuiListtreeInsertMessage.Size);
		return APTR.FromPointer(result);
	}

	// Rename's User form is not an Insert method and therefore has no
	// MUIP_Listtree_Insert packet to expose. Preserve its direct value callback
	// contract while the Insert path above publishes the full typed packet.
	private static APTR ConstructUserValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR user, out uint ownership)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		ownership = 0;
		var hook = ReadPolicy(ref platform, state, obj, ConstructHook, 0);
		if (hook == 0) return user;
		if (hook == ConstructHookString)
		{
			if (user.IsNull) return APTR.Null;
			var dup = DuplicateString(ref platform, user, out _);
			ownership = dup.IsNotNull ? 1u : 0u;
			return dup;
		}
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
		// Arbitrary destruct hook: A0 = hook base, A2 = the same standard Exec
		// pool supplied to ConstructHook, A1 = user data. A NULL pool is retained
		// only as a malformed-state fallback; a valid construct path always owns
		// the record below.
		TryGetHookPool(ref platform, state, obj, out var pool);
		platform.InvokeHook(APTR.FromPointer(hook), pool, user);
	}

	private static void CallNodeHook<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint hookAttribute, APTR node)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var hook = ReadPolicy(ref platform, state, obj, hookAttribute, 0);
		if (hook == 0) return;
		// Open/close hooks: A0 = hook base, A2 = object, A1 = node.
		platform.InvokeHook(APTR.FromPointer(hook), obj, node);
	}

	private static uint DisplayFlagsForNode<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR node)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (node.IsNull) return MuiListtreeDisplaySnapshotState.DisplayTitle;
		var nodeFlags = ReadFlags(ref platform, node);
		var displayFlags = 0u;
		var listNode = (nodeFlags & TNF_LIST) != 0;
		if (listNode)
		{
			displayFlags |= MuiListtreeDisplaySnapshotState.DisplayListNode;
			var empty = ReadNodeChildCount(ref platform, node) == 0;
			var emptyNodes = ReadRaw(ref platform, state, obj, EmptyNodes, 0) != 0;
			if ((!emptyNodes || !empty) && (nodeFlags & TNF_NOSIGN) == 0)
				displayFlags |= MuiListtreeDisplaySnapshotState.DisplayIndicator;
		}
		if ((nodeFlags & TNF_OPEN) != 0)
			displayFlags |= MuiListtreeDisplaySnapshotState.DisplayOpen;
		if ((nodeFlags & TNF_FROZEN) != 0)
			displayFlags |= MuiListtreeDisplaySnapshotState.DisplayFrozen;
		return displayFlags;
	}

	private static bool CallDisplayHook<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR node)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var hook = ReadPolicy(ref platform, state, obj, DisplayHook, 0);
		if (hook == 0)
		{
			FreeDisplaySnapshot(ref platform, state, obj);
			return true;
		}

		var columnCount = ListtreeFormatColumnCount(ref platform, state, obj);
		if (columnCount == 0 || columnCount > MaximumFormatColumns)
			columnCount = 1;
		if (columnCount > uint.MaxValue /
			MuiListtreeDisplayColumnRecord.Size) return false;
		var vectorSize = columnCount * MuiListtreeDisplayColumnRecord.Size;
		var vector = MuiHeadlessMemory.Allocate(ref platform, vectorSize);
		if (vector.IsNull) return false;
		var populated = true;
		var cursor = default(MuiListtreeDisplayColumnCursor);
		cursor.Base = vector;
		for (var column = 0u; column < columnCount; column++)
		{
			cursor.Index = column;
			if (!MuiListtreeDisplayColumnCursorCodec.TryGetEntry(ref platform,
				cursor, out var slot))
			{
				populated = false;
				break;
			}
			var value = default(MuiListtreeDisplayColumnRecord);
			// MorphOS uses NULL in the tree-column slot to request the
			// built-in node name. A DisplayHook may replace it with a caller-
			// owned string, so do not pre-populate a managed or copied value.
			if (!MuiListtreeDisplayColumnCodec.Write(ref platform, slot, value))
			{
				populated = false;
				break;
			}
		}
		if (populated)
		{
			// MorphOS DisplayHook ABI: A0 = hook, A1 = tree node, A2 =
			// STRPTR array containing one slot per FORMAT column.
			platform.InvokeHook(APTR.FromPointer(hook), vector, node);
		}
		if (!populated)
		{
			platform.Clear(vector, vectorSize);
			platform.Free(vector, vectorSize);
			return false;
		}
		var displayFlags = DisplayFlagsForNode(ref platform, state, obj, node);
		if (StoreDisplaySnapshot(ref platform, state, obj, node, columnCount,
			vector, displayFlags)) return true;
		platform.Clear(vector, vectorSize);
		platform.Free(vector, vectorSize);
		return false;
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
		if (!SetRawAttribute(ref platform, state, obj, Active, value, notify))
			return false;
		if (!SyncPolicyStateRecord(ref platform, state, obj)) return false;
		return KeepActiveVisible(ref platform, state, obj, node);
	}

	private static uint SurfaceVisibleRows(MuiListtreeSurfaceStateRecord surface)
	{
		if (surface.Height <= 0) return 0;
		var rowHeight = surface.RowHeight == 0 ? DefaultRowHeight :
			surface.RowHeight;
		return unchecked((uint)surface.Height) / rowHeight;
	}

	private static uint SurfaceDataRows<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiListtreeSurfaceStateRecord surface)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var rows = SurfaceVisibleRows(surface);
		if (rows != 0 && ReadRaw(ref platform, state, obj, Title, 0) != 0)
			rows--;
		return rows;
	}

	private static void ClampSurfaceViewport<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, ref MuiListtreeSurfaceStateRecord surface)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var rows = SurfaceDataRows(ref platform, state, obj, surface);
		var count = VisibleCount(ref platform, state, obj);
		var maximum = rows != 0 && count > rows ? count - rows : 0;
		if (surface.FirstVisible > maximum) surface.FirstVisible = maximum;
	}

	// Keep the active visible row inside the named viewport. This is the
	// Listtree equivalent of the child List's First cursor, but remains a typed
	// external-class record so no built-in List offsets leak into Listtree.
	private static bool KeepActiveVisible<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR node)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSurfaceStateRecord(ref platform, state, obj, out var surface))
			return true;
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		var index = node.IsNull ? uint.MaxValue :
			VisibleIndexOf(ref platform, header, node);
		var rows = SurfaceDataRows(ref platform, state, obj, surface);
		if (index != uint.MaxValue && rows != 0)
		{
			if (index < surface.FirstVisible)
				surface.FirstVisible = index;
			else if (index - surface.FirstVisible >= rows)
				surface.FirstVisible = index - rows + 1;
		}
		ClampSurfaceViewport(ref platform, state, obj, ref surface);
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			SurfaceStateKey);
		return MuiListtreeSurfaceStateRecordCodec.Write(ref platform, block,
			surface);
	}

	// Topology mutations can move the active node or change the visible
	// preorder count without changing the Area rectangle. Re-apply the same
	// named surface policy after each mutation so FirstVisible never points past
	// the legal range and the active row remains visible. The surface record is
	// the only state exchanged here; no List/Listview layout offsets are needed.
	private static bool RefreshSurfaceViewportAfterMutation<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSurfaceStateRecord(ref platform, state, obj,
			out var surface)) return true;
		var active = ActiveNode(ref platform, state, obj);
		if (active.IsNotNull)
			return KeepActiveVisible(ref platform, state, obj, active);
		ClampSurfaceViewport(ref platform, state, obj, ref surface);
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			SurfaceStateKey);
		return MuiListtreeSurfaceStateRecordCodec.Write(ref platform, block,
			surface);
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

	private static uint ReadNodeNumber<TPlatform>(ref TPlatform platform,
		APTR node, NodeField field) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListtreeNodeCodec.TryRead(ref platform, node, out var value))
			return 0;
		return field switch
		{
			NodeField.ChildCount => value.ChildCount,
			NodeField.NameOwned => value.NameOwned,
			NodeField.NameSize => value.NameSize,
			NodeField.UserOwned => value.UserOwned,
			NodeField.Reserved0 => value.Reserved0,
			NodeField.Reserved1 => value.Reserved1,
			_ => 0,
		};
	}

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
		Reserved0,
		Reserved1,
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
			case NodeField.Reserved0: value.Reserved0 = number; break;
			case NodeField.Reserved1: value.Reserved1 = number; break;
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

	// Selection bookkeeping changes only the named private words. Keeping this
	// narrow write out of the general topology updater prevents a native closure
	// from reconstructing the public flag prefix while a click is being applied.
	private static bool WriteSelectionNumber<TPlatform>(ref TPlatform platform,
		APTR node, uint value, NodeField field)
		where TPlatform : struct, IMuiGuestMemory
	{
		var target = field == NodeField.Reserved0
			? MuiListtreeNodeField.Reserved0
			: field == NodeField.Reserved1
				? MuiListtreeNodeField.Reserved1
				: MuiListtreeNodeField.Private1;
		if (field != NodeField.Reserved0 && field != NodeField.Reserved1)
			return false;
		return MuiListtreeNodeFieldCursorCodec.TryWriteUInt32(ref platform,
			node, target, value);
	}

	private static bool IsValidNode<TPlatform>(ref TPlatform platform, APTR obj,
		APTR node) where TPlatform : struct, IMuiGuestMemory
	{
		// Admission is intentionally scalar: the node remains a named guest
		// record, but the freestanding 68k compiler must not materialize the
		// larger out-record merely to validate its cookie and owning object.
		return node.IsNotNull && platform.IsMapped(node, NodeSize) &&
			MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, node,
				MuiListtreeNodeField.Private1, out var cookie) &&
			cookie == MuiListtreeNodeState.Cookie &&
			MuiListtreeNodeFieldCursorCodec.TryReadUInt32(ref platform, node,
				MuiListtreeNodeField.Private2, out var owner) && owner == obj.Raw;
	}

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
		MuiListtreeNodeFieldCursorCodec.TryReadUInt16(ref platform, node,
			MuiListtreeNodeField.Flags, out var flags) ? flags : 0u;

	private static void SetFlagBits<TPlatform>(ref TPlatform platform, APTR node,
		uint bits) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListtreeNodeFieldCursorCodec.TryReadUInt16(ref platform, node,
			MuiListtreeNodeField.Flags, out var flags))
			return;
		MuiListtreeNodeFieldCursorCodec.TryWriteUInt16(ref platform, node,
			MuiListtreeNodeField.Flags, (ushort)(flags | bits));
	}

	private static void ClearFlagBits<TPlatform>(ref TPlatform platform, APTR node,
		uint bits) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListtreeNodeFieldCursorCodec.TryReadUInt16(ref platform, node,
			MuiListtreeNodeField.Flags, out var flags))
			return;
		MuiListtreeNodeFieldCursorCodec.TryWriteUInt16(ref platform, node,
			MuiListtreeNodeField.Flags, (ushort)(flags & ~bits));
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

	private static bool SetRawAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		return !record.IsNull && MuiHeadlessObjectCore.SetRecordAttributeRaw(
			ref platform, state, record, attribute, value, notify);
	}

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
			SetRawAttribute(ref platform, state, obj, attribute, value, false);
	}
}
