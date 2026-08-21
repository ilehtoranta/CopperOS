/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Fixed guest-memory layout for the per-object MG09 menu specialist sidecar.
//
// Unlike the standalone Pop* / pen-color specialist families, the menu family
// (Menustrip.mui, Menu.mui, Menuitem.mui) are Family.mui subclasses whose
// defining behavior *is* an owned parent/child hierarchy. The clean-room design
// therefore reuses the existing frozen hierarchy machinery rather than
// re-implementing it: every menu specialist instance is a real headless object
// (created through MuiHeadlessObjectCore, exactly like MUI_MakeObjectA emits),
// its children are linked through the frozen MuiFamilyCore, and its scalar
// attributes are stored/notified through the frozen object-attribute path so
// runtime changes fire notifications with no second object system.
//
// The only additive per-object state that the frozen object cannot express is
// the menu-specific bookkeeping below: the exact class discriminator, the
// Menustrip change-nesting depth, the class-owned copied Title/Shortcut blocks
// (governed by CopyStrings), the [I..] CopyStrings / CaseSensitive latches, the
// Trigger publication token, and the runtime-change notification counters. That
// bookkeeping lives in this small guest-resident sidecar block, which is
// attached to its object through a single private attribute id and freed by the
// family lifecycle. The frozen headless/family/object cores, dispatchers and
// platform aggregates are not modified.
internal static class MuiMenuSpecialistLayout
{
	public const uint Magic = 0x4D4D4E55;   // "MMNU"

	// Private attribute id linking a headless object to its menu sidecar. It is
	// well outside the 0x8042xxxx MUI attribute range and is never a documented
	// attribute, so it cannot collide with a real Get/Set.
	public const uint SidecarAttribute = 0x7F4D4E55u;

	// The sidecar wire shape is represented by MuiMenuSpecialistState below;
	// field offsets are confined to its codec.
	public const uint InstanceSize = 52;

	// Flags.
	public const uint FlagCopyStrings = 1u << 0;    // MUIA_*_CopyStrings [I..]
	public const uint FlagCaseSensitive = 1u << 1;  // MUIA_Menustrip_CaseSensitive
	public const uint FlagWillOpen = 1u << 2;       // MUIM_Menustrip_WillOpen seen
	public const uint FlagPublished = 1u << 3;      // Trigger published

	// Bounded traversal for sibling/child walks.
	public const uint MaximumChildren = 4096;
	public const uint MaximumString = 4096;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMenuSpecialistState
{
	internal const uint Size = MuiMenuSpecialistLayout.InstanceSize;
	internal const uint Cookie = MuiMenuSpecialistLayout.Magic;

	internal uint Magic;
	internal uint Class;
	internal uint ChangeDepth;
	internal APTR TitleOwned;
	internal uint TitleOwnedSize;
	internal APTR ShortcutOwned;
	internal uint ShortcutOwnedSize;
	internal uint Flags;
	internal uint Trigger;
	internal uint NotifyAttribute;
	internal uint NotifyValue;
	internal uint NotifyCount;
	internal uint Reserved0;
}

internal enum MuiMenuRecordField : byte
{
	Magic,
	Class,
	ChangeDepth,
	TitleOwned,
	TitleOwnedSize,
	ShortcutOwned,
	ShortcutOwnedSize,
	Flags,
	Trigger,
	NotifyAttribute,
	NotifyValue,
	NotifyCount,
	Reserved0,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMenuRecordFieldCursor
{
	internal APTR Address;
	internal MuiMenuRecordField Field;
}

internal static class MuiMenuRecordFieldCursorCodec
{
	private static bool TryResolve(MuiMenuRecordField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiMenuRecordField.Magic => 0,
			MuiMenuRecordField.Class => 4,
			MuiMenuRecordField.ChangeDepth => 8,
			MuiMenuRecordField.TitleOwned => 12,
			MuiMenuRecordField.TitleOwnedSize => 16,
			MuiMenuRecordField.ShortcutOwned => 20,
			MuiMenuRecordField.ShortcutOwnedSize => 24,
			MuiMenuRecordField.Flags => 28,
			MuiMenuRecordField.Trigger => 32,
			MuiMenuRecordField.NotifyAttribute => 36,
			MuiMenuRecordField.NotifyValue => 40,
			MuiMenuRecordField.NotifyCount => 44,
			MuiMenuRecordField.Reserved0 => 48,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMenuRecordFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, MuiMenuSpecialistState.Size))
			return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiMenuRecordField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiMenuRecordFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiMenuRecordField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiMenuRecordFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiMenuSpecialistStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiMenuSpecialistState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiMenuSpecialistState.Size) ||
			!MuiMenuRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMenuRecordField.Magic, out var magic) ||
			magic != MuiMenuSpecialistState.Cookie)
			return false;
		value.Magic = MuiMenuSpecialistState.Cookie;
		if (!MuiMenuRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiMenuRecordField.Class, out value.Class) ||
			!MuiMenuRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMenuRecordField.ChangeDepth, out value.ChangeDepth) ||
			!MuiMenuRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMenuRecordField.TitleOwned, out var titleOwned) ||
			!MuiMenuRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMenuRecordField.TitleOwnedSize, out value.TitleOwnedSize) ||
			!MuiMenuRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMenuRecordField.ShortcutOwned, out var shortcutOwned) ||
			!MuiMenuRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMenuRecordField.ShortcutOwnedSize, out value.ShortcutOwnedSize) ||
			!MuiMenuRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMenuRecordField.Flags, out value.Flags) ||
			!MuiMenuRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMenuRecordField.Trigger, out value.Trigger) ||
			!MuiMenuRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMenuRecordField.NotifyAttribute, out value.NotifyAttribute) ||
			!MuiMenuRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMenuRecordField.NotifyValue, out value.NotifyValue) ||
			!MuiMenuRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMenuRecordField.NotifyCount, out value.NotifyCount) ||
			!MuiMenuRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiMenuRecordField.Reserved0, out value.Reserved0)) return false;
		value.TitleOwned = APTR.FromPointer(titleOwned);
		value.ShortcutOwned = APTR.FromPointer(shortcutOwned);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiMenuSpecialistState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiMenuSpecialistState.Size) || value.Magic !=
			MuiMenuSpecialistState.Cookie) return false;
		return MuiMenuRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiMenuRecordField.Magic, value.Magic) &&
			MuiMenuRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMenuRecordField.Class, value.Class) &&
			MuiMenuRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMenuRecordField.ChangeDepth, value.ChangeDepth) &&
			MuiMenuRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMenuRecordField.TitleOwned, value.TitleOwned.Raw) &&
			MuiMenuRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMenuRecordField.TitleOwnedSize, value.TitleOwnedSize) &&
			MuiMenuRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMenuRecordField.ShortcutOwned, value.ShortcutOwned.Raw) &&
			MuiMenuRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMenuRecordField.ShortcutOwnedSize, value.ShortcutOwnedSize) &&
			MuiMenuRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMenuRecordField.Flags, value.Flags) &&
			MuiMenuRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMenuRecordField.Trigger, value.Trigger) &&
			MuiMenuRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMenuRecordField.NotifyAttribute, value.NotifyAttribute) &&
			MuiMenuRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMenuRecordField.NotifyValue, value.NotifyValue) &&
			MuiMenuRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMenuRecordField.NotifyCount, value.NotifyCount) &&
			MuiMenuRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiMenuRecordField.Reserved0, value.Reserved0);
	}
}

// The menu class discriminator. All three classes descend directly from
// Family.mui; there is no menu-internal inheritance chain.
public enum MuiMenuSpecialistClass : uint
{
	None = 0,
	Menustrip = 1,   // : Family
	Menu = 2,        // : Family
	Menuitem = 3,    // : Family
}

// The MG09 menu specialist. Every entry point works over a validated headless
// object plus its attached sidecar. Classes are classified by their exact,
// case-sensitive official class id. Hierarchy is delegated to MuiFamilyCore;
// scalar attributes and their notifications go through MuiHeadlessObjectCore.
public static class MuiMenuSpecialistCore
{
	private enum MuiMenuOwnedSlot : byte
	{
		Title,
		Shortcut,
	}

	private static APTR OwnedPointer(MuiMenuSpecialistState value,
		MuiMenuOwnedSlot slot) => slot == MuiMenuOwnedSlot.Title
		? value.TitleOwned : value.ShortcutOwned;

	private static uint OwnedSize(MuiMenuSpecialistState value,
		MuiMenuOwnedSlot slot) => slot == MuiMenuOwnedSlot.Title
		? value.TitleOwnedSize : value.ShortcutOwnedSize;

	private static void SetOwned(ref MuiMenuSpecialistState value,
		MuiMenuOwnedSlot slot, APTR pointer, uint size)
	{
		if (slot == MuiMenuOwnedSlot.Title)
		{
			value.TitleOwned = pointer;
			value.TitleOwnedSize = size;
		}
		else
		{
			value.ShortcutOwned = pointer;
			value.ShortcutOwnedSize = size;
		}
	}

	// ---- Classification ------------------------------------------------------

	// Classify a guest C-string class id against the exact official names. The
	// loader contract is case-sensitive, so the match is byte-exact against the
	// documented "<Name>.mui" ids with no managed strings, arrays or spans.
	public static MuiMenuSpecialistClass ClassifyName<TPlatform>(
		ref TPlatform platform, APTR classId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (classId.IsNull) return MuiMenuSpecialistClass.None;
		var c0 = B(ref platform, classId, 0);
		var c1 = B(ref platform, classId, 1);
		var c2 = B(ref platform, classId, 2);
		var c3 = B(ref platform, classId, 3);
		if (c0 != 'M' || c1 != 'e' || c2 != 'n' || c3 != 'u')
			return MuiMenuSpecialistClass.None;
		// Menu.mui
		if (Suffix(ref platform, classId, 4)) return MuiMenuSpecialistClass.Menu;
		var c4 = B(ref platform, classId, 4);
		// Menustrip.mui
		if (c4 == 's' && B(ref platform, classId, 5) == 't' &&
			B(ref platform, classId, 6) == 'r' &&
			B(ref platform, classId, 7) == 'i' &&
			B(ref platform, classId, 8) == 'p' && Suffix(ref platform, classId, 9))
			return MuiMenuSpecialistClass.Menustrip;
		// Menuitem.mui
		if (c4 == 'i' && B(ref platform, classId, 5) == 't' &&
			B(ref platform, classId, 6) == 'e' &&
			B(ref platform, classId, 7) == 'm' && Suffix(ref platform, classId, 8))
			return MuiMenuSpecialistClass.Menuitem;
		return MuiMenuSpecialistClass.None;
	}

	private static int B<TPlatform>(ref TPlatform platform, APTR text, int index)
		where TPlatform : struct, IMuiGuestMemory =>
		platform.IsMapped(text, (uint)index + 1) ? platform.ReadUInt8(text, index)
			: -1;

	private static bool Suffix<TPlatform>(ref TPlatform platform, APTR text,
		int offset) where TPlatform : struct, IMuiGuestMemory =>
		B(ref platform, text, offset) == '.' &&
		B(ref platform, text, offset + 1) == 'm' &&
		B(ref platform, text, offset + 2) == 'u' &&
		B(ref platform, text, offset + 3) == 'i' &&
		B(ref platform, text, offset + 4) == 0;

	// Every menu class descends directly from Family.mui; None is the Family
	// sentinel root.
	public static MuiMenuSpecialistClass Superclass(MuiMenuSpecialistClass cls) =>
		MuiMenuSpecialistClass.None;

	// ---- Sidecar attach / lookup ---------------------------------------------

	// Attach a menu sidecar to an already-created headless object of `cls`,
	// establishing the documented creation defaults (Enabled TRUE). Fails
	// atomically: a failed sidecar allocation or attribute link frees the block
	// and leaves the object untouched. Returns the sidecar block or Null.
	public static APTR Attach<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, MuiMenuSpecialistClass cls)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (obj.IsNull || cls == MuiMenuSpecialistClass.None) return APTR.Null;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return APTR.Null;
		if (Sidecar(ref platform, state, obj).IsNotNull) return APTR.Null;
		var sc = MuiHeadlessMemory.Allocate(ref platform,
			MuiMenuSpecialistState.Size);
		if (sc.IsNull) return APTR.Null;
		var initial = default(MuiMenuSpecialistState);
		initial.Magic = MuiMenuSpecialistState.Cookie;
		initial.Class = (uint)cls;
		if (!MuiMenuSpecialistStateCodec.Write(ref platform, sc, initial))
		{
			platform.Clear(sc, MuiMenuSpecialistState.Size);
			platform.Free(sc, MuiMenuSpecialistState.Size);
			return APTR.Null;
		}
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiMenuSpecialistLayout.SidecarAttribute, sc.Raw, false))
		{
			platform.Clear(sc, MuiMenuSpecialistState.Size);
			platform.Free(sc, MuiMenuSpecialistState.Size);
			return APTR.Null;
		}
		// Documented creation default: the whole family is enabled.
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			EnabledAttribute(cls), 1, false);
		// MUI_MakeObjectA/MUI_NewObjectA apply their initial tags before the
		// specialist sidecar is attached.  Import the init-only CopyStrings latch
		// and re-run the string setters so direct factory construction has the same
		// ownership semantics as a class-native OM_NEW path.
		if (!AdoptInitialCopyStrings(ref platform, state, obj, sc, cls))
		{
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				MuiMenuSpecialistLayout.SidecarAttribute, 0, false);
			platform.Clear(sc, MuiMenuSpecialistState.Size);
			platform.Free(sc, MuiMenuSpecialistState.Size);
			return APTR.Null;
		}
		return sc;
	}

	private static bool AdoptInitialCopyStrings<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR sc, MuiMenuSpecialistClass cls)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var copyAttribute = cls == MuiMenuSpecialistClass.Menu
			? MuiMenuAttributes.Menu_CopyStrings
			: cls == MuiMenuSpecialistClass.Menuitem
				? MuiMenuAttributes.Menuitem_CopyStrings : 0u;
		if (copyAttribute == 0 || !MuiHeadlessObjectCore.GetAttribute(ref platform,
			state, obj, copyAttribute, out var copy) || copy == 0) return true;
		if (!MuiMenuSpecialistStateCodec.TryRead(ref platform, sc,
			out var sidecar)) return false;
		sidecar.Flags |= MuiMenuSpecialistLayout.FlagCopyStrings;
		if (!MuiMenuSpecialistStateCodec.Write(ref platform, sc, sidecar))
			return false;
		if (cls == MuiMenuSpecialistClass.Menu)
		{
			if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
				MuiMenuAttributes.Menu_Title, out var title)) return true;
			return title == 0 || CopyInitialString(ref platform, state, obj, sc,
				MuiMenuAttributes.Menu_Title, MuiMenuOwnedSlot.Title, title);
		}
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			MuiMenuAttributes.Menuitem_Title, out var itemTitle) ||
			(itemTitle != 0 && itemTitle != 0xFFFFFFFFu) &&
				!CopyInitialString(ref platform, state, obj, sc,
					MuiMenuAttributes.Menuitem_Title,
					MuiMenuOwnedSlot.Title, itemTitle)) return false;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			MuiMenuAttributes.Menuitem_Shortcut, out var shortcut)) return true;
		if (shortcut == 0) return true;
		if (CopyInitialString(ref platform, state, obj, sc,
			MuiMenuAttributes.Menuitem_Shortcut, MuiMenuOwnedSlot.Shortcut,
			shortcut)) return true;
		FreeOwned(ref platform, sc, MuiMenuOwnedSlot.Title);
		return false;
	}

	private static bool CopyInitialString<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR sc, uint attribute, MuiMenuOwnedSlot slot,
		uint value) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (value == 0 || value == 0xFFFFFFFFu) return true;
		if (!CopyString(ref platform, APTR.FromPointer(value), out var block,
			out var size)) return false;
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			block.Raw, false);
		if (!MuiMenuSpecialistStateCodec.TryRead(ref platform, sc,
			out var sidecar)) return false;
		SetOwned(ref sidecar, slot, block, size);
		return MuiMenuSpecialistStateCodec.Write(ref platform, sc, sidecar);
	}

	// Classify an existing object by its registered class-record name so trees
	// produced by MUI_MakeObjectA can be adopted through Attach without the
	// caller re-supplying the class id.
	public static MuiMenuSpecialistClass ClassifyObject<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var classRecord = MuiHeadlessObjectCore.ObjectClassRecord(ref platform,
			state, obj);
		if (!MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
			out var classValue)) return MuiMenuSpecialistClass.None;
		return ClassifyName(ref platform, classValue.Name);
	}

	// Attach by classifying the object's registered class record (MakeObject
	// interop path). Returns the sidecar or Null.
	public static APTR AttachByObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		Attach(ref platform, state, obj,
			ClassifyObject(ref platform, state, obj));

	private static APTR Sidecar<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			MuiMenuSpecialistLayout.SidecarAttribute, out var raw) || raw == 0)
			return APTR.Null;
		var sc = APTR.FromPointer(raw);
		if (!MuiMenuSpecialistStateCodec.TryRead(ref platform, sc,
			out _))
			return APTR.Null;
		return sc;
	}

	public static bool Valid<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		Sidecar(ref platform, state, obj).IsNotNull;

	public static MuiMenuSpecialistClass Classify<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sc = Sidecar(ref platform, state, obj);
		return sc.IsNull || !MuiMenuSpecialistStateCodec.TryRead(ref platform,
			sc, out var value) ? MuiMenuSpecialistClass.None :
			(MuiMenuSpecialistClass)value.Class;
	}

	private static uint EnabledAttribute(MuiMenuSpecialistClass cls) => cls switch
	{
		MuiMenuSpecialistClass.Menustrip => MuiMenuAttributes.Menustrip_Enabled,
		MuiMenuSpecialistClass.Menu => MuiMenuAttributes.Menu_Enabled,
		_ => MuiMenuAttributes.Menuitem_Enabled,
	};

	// ---- Owned hierarchy (delegated to MuiFamilyCore) ------------------------

	// Add `child` to `parent`, enforcing the exact menu containment rules and
	// the one-level Menuitem nesting rule, then delegating the actual link to
	// the frozen MuiFamilyCore. Rules:
	//   Menustrip <- Menu
	//   Menu      <- Menuitem
	//   Menuitem  <- Menuitem, but only when `parent` is a top-level item (its
	//                own parent is a Menu, or it is not yet parented); a
	//                sub-item may not gain its own sub-items.
	public static bool AddChild<TPlatform>(ref TPlatform platform, APTR state,
		APTR parent, APTR child) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!CanContain(ref platform, state, parent, child)) return false;
		return MuiFamilyCore.AddTail(ref platform, state, parent, child);
	}

	// Add at head, same validation.
	public static bool AddHeadChild<TPlatform>(ref TPlatform platform, APTR state,
		APTR parent, APTR child) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!CanContain(ref platform, state, parent, child)) return false;
		return MuiFamilyCore.AddHead(ref platform, state, parent, child);
	}

	public static bool InsertChild<TPlatform>(ref TPlatform platform, APTR state,
		APTR parent, APTR child, APTR predecessor)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!CanContain(ref platform, state, parent, child)) return false;
		return MuiFamilyCore.Insert(ref platform, state, parent, child,
			predecessor, false);
	}

	public static bool RemoveChild<TPlatform>(ref TPlatform platform, APTR state,
		APTR parent, APTR child) where TPlatform : struct, IMuiHeadlessPlatform =>
		Valid(ref platform, state, parent) &&
		MuiFamilyCore.Remove(ref platform, state, parent, child);

	private static bool CanContain<TPlatform>(ref TPlatform platform, APTR state,
		APTR parent, APTR child) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var pk = Classify(ref platform, state, parent);
		var ck = Classify(ref platform, state, child);
		if (pk == MuiMenuSpecialistClass.None || ck == MuiMenuSpecialistClass.None)
			return false;
		switch (pk)
		{
			case MuiMenuSpecialistClass.Menustrip:
				return ck == MuiMenuSpecialistClass.Menu;
			case MuiMenuSpecialistClass.Menu:
				return ck == MuiMenuSpecialistClass.Menuitem;
			case MuiMenuSpecialistClass.Menuitem:
				// One-level nesting: only a top-level item may hold sub-items.
				return ck == MuiMenuSpecialistClass.Menuitem &&
					!ParentIsMenuitem(ref platform, state, parent);
			default:
				return false;
		}
	}

	private static bool ParentIsMenuitem<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var parentObj = ParentObject(ref platform, state, obj);
		return parentObj.IsNotNull && Classify(ref platform, state, parentObj) ==
			MuiMenuSpecialistClass.Menuitem;
	}

	private static APTR ParentObject<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull || !MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return APTR.Null;
		var parentRecord = objectValue.Parent;
		if (parentRecord.IsNull || !MuiHeadlessObjectCodec.TryRead(
			ref platform, parentRecord, out var parentValue))
			return APTR.Null;
		return parentValue.Boopsi;
	}

	public static uint ChildCount<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Valid(ref platform, state, obj)) return 0;
		uint count = 0;
		while (count < MuiMenuSpecialistLayout.MaximumChildren)
		{
			if (MuiFamilyCore.GetChild(ref platform, state, obj, (int)count,
				APTR.Null).IsNull) break;
			count++;
		}
		return count;
	}

	// ---- Menustrip change brackets / open behavior ---------------------------

	// MUIM_Menustrip_InitChange: open a change bracket. Nesting is allowed; the
	// depth simply increments.
	public static bool InitChange<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sc = MenustripSidecar(ref platform, state, obj);
		if (sc.IsNull) return false;
		if (!MuiMenuSpecialistStateCodec.TryRead(ref platform, sc,
			out var sidecar)) return false;
		var depth = sidecar.ChangeDepth;
		if (depth == uint.MaxValue) return false;   // overflow protection
		sidecar.ChangeDepth = depth + 1;
		return MuiMenuSpecialistStateCodec.Write(ref platform, sc, sidecar);
	}

	// MUIM_Menustrip_ExitChange: close a change bracket. Underflow protected: an
	// ExitChange with no matching InitChange leaves the depth at zero and fails
	// rather than wrapping.
	public static bool ExitChange<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sc = MenustripSidecar(ref platform, state, obj);
		if (sc.IsNull) return false;
		if (!MuiMenuSpecialistStateCodec.TryRead(ref platform, sc,
			out var sidecar)) return false;
		var depth = sidecar.ChangeDepth;
		if (depth == 0) return false;               // underflow protection
		sidecar.ChangeDepth = depth - 1;
		return MuiMenuSpecialistStateCodec.Write(ref platform, sc, sidecar);
	}

	public static uint ChangeDepth<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sc = MenustripSidecar(ref platform, state, obj);
		return sc.IsNull || !MuiMenuSpecialistStateCodec.TryRead(ref platform,
			sc, out var sidecar) ? 0 : sidecar.ChangeDepth;
	}

	// MUIM_Menustrip_WillOpen: prepare the strip for display. Fails if the strip
	// is disabled or a change bracket is still open (mid-change strips must not
	// open). Records that a WillOpen was accepted.
	public static bool WillOpen<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sc = MenustripSidecar(ref platform, state, obj);
		if (sc.IsNull) return false;
		if (!MuiMenuSpecialistStateCodec.TryRead(ref platform, sc,
			out var sidecar) || sidecar.ChangeDepth != 0)
			return false;
		if (!BoolAttribute(ref platform, state, obj,
			MuiMenuAttributes.Menustrip_Enabled)) return false;
		SetFlag(ref platform, sc, MuiMenuSpecialistLayout.FlagWillOpen, true);
		return true;
	}

	// MUIM_Menustrip_Popup: open the strip as a context menu. Bounded: it
	// requires an enabled, settled (not mid-change) strip; the window/x/y
	// coordinates are validated by the dispatcher and not otherwise interpreted
	// here (no frozen application aggregate is touched, and no host menu is
	// published).
	public static bool Popup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		WillOpen(ref platform, state, obj);

	public static bool IsWillOpen<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sc = MenustripSidecar(ref platform, state, obj);
		return sc.IsNotNull && MuiMenuSpecialistStateCodec.TryRead(ref platform,
			sc, out var sidecar) && (sidecar.Flags &
			MuiMenuSpecialistLayout.FlagWillOpen) != 0;
	}

	private static APTR MenustripSidecar<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sc = Sidecar(ref platform, state, obj);
		return sc.IsNotNull && MuiMenuSpecialistStateCodec.TryRead(ref platform,
			sc, out var sidecar) && (MuiMenuSpecialistClass)sidecar.Class ==
			MuiMenuSpecialistClass.Menustrip ? sc : APTR.Null;
	}

	// ---- Menuitem trigger / toggle -------------------------------------------

	// Runtime selection of a menu item. A Toggle item flips its Checked state; a
	// non-Toggle checkmark item is set checked. Becoming checked runs the mutual
	// exclusion sweep across siblings. The item publishes itself as the Trigger.
	// A disabled item ignores the trigger entirely.
	public static bool TriggerItem<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		TriggerItem(ref platform, state, obj, false);

	// Trigger a menu item and publish its UserData to the owning application.
	// `help` selects MUIA_Application_MenuHelp; normal activation selects
	// MUIA_Application_MenuAction. The existing trigger notification remains
	// unchanged and is emitted before the application event publication.
	public static bool TriggerItem<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, bool help) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sc = Sidecar(ref platform, state, obj);
		if (sc.IsNull || !MuiMenuSpecialistStateCodec.TryRead(ref platform, sc,
			out var sidecar) || (MuiMenuSpecialistClass)sidecar.Class !=
			MuiMenuSpecialistClass.Menuitem)
			return false;
		if (!BoolAttribute(ref platform, state, obj,
			MuiMenuAttributes.Menuitem_Enabled)) return false;

		var checkit = BoolAttribute(ref platform, state, obj,
			MuiMenuAttributes.Menuitem_Checkit);
		var toggle = BoolAttribute(ref platform, state, obj,
			MuiMenuAttributes.Menuitem_Toggle);
		if (checkit)
		{
			var current = BoolAttribute(ref platform, state, obj,
				MuiMenuAttributes.Menuitem_Checked);
			var next = toggle ? !current : true;
			SetChecked(ref platform, state, obj, next, true);
		}

		sidecar.Trigger = obj.Raw;
		if (!MuiMenuSpecialistStateCodec.Write(ref platform, sc, sidecar))
			return false;
		SetFlag(ref platform, sc, MuiMenuSpecialistLayout.FlagPublished, true);
		Notify(ref platform, state, sc, MuiMenuAttributes.Menuitem_Trigger,
			obj.Raw);
		MuiApplicationWindowCore.PublishApplicationMenuItemSelection(ref platform,
			state, obj, help);
		return true;
	}

	public static uint Trigger<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sc = Sidecar(ref platform, state, obj);
		return sc.IsNull || !MuiMenuSpecialistStateCodec.TryRead(ref platform,
			sc, out var sidecar) ? 0 : sidecar.Trigger;
	}

	// ---- Attribute get -------------------------------------------------------

	// Read a menu attribute honoring the official I/S/G policy. Init-only
	// attributes (CopyStrings, CaseSensitive) are not exposed here per their
	// [I..] policy and are read through their dedicated accessors instead.
	public static bool GetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		var cls = Classify(ref platform, state, obj);
		if (cls == MuiMenuSpecialistClass.None) return false;

		switch (attribute)
		{
			case MuiMenuAttributes.Family_ChildCount:
				value = ChildCount(ref platform, state, obj);
				return true;
			case MuiMenuAttributes.Family_List:
				// The exec MinList backing the family is not modelled headless.
				value = 0;
				return true;

			case MuiMenuAttributes.Menustrip_Enabled:
				if (cls != MuiMenuSpecialistClass.Menustrip) return false;
				return ReadStored(ref platform, state, obj, attribute, out value);

			case MuiMenuAttributes.Menu_Enabled:
			case MuiMenuAttributes.Menu_Title:
				if (cls != MuiMenuSpecialistClass.Menu) return false;
				return ReadStored(ref platform, state, obj, attribute, out value);

			case MuiMenuAttributes.Menuitem_Title:
			case MuiMenuAttributes.Menuitem_Shortcut:
			case MuiMenuAttributes.Menuitem_Checkit:
			case MuiMenuAttributes.Menuitem_Checked:
			case MuiMenuAttributes.Menuitem_Toggle:
			case MuiMenuAttributes.Menuitem_CommandString:
			case MuiMenuAttributes.Menuitem_Enabled:
			case MuiMenuAttributes.Menuitem_Exclude:
				if (cls != MuiMenuSpecialistClass.Menuitem) return false;
				return ReadStored(ref platform, state, obj, attribute, out value);

			case MuiMenuAttributes.Menuitem_Trigger:
				if (cls != MuiMenuSpecialistClass.Menuitem) return false;
				value = Trigger(ref platform, state, obj);
				return true;
		}
		return false;
	}

	// Init-only accessors (honor [I..]: not exposed through OM_GET).
	public static bool CopyStringsFlag<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sc = Sidecar(ref platform, state, obj);
		return sc.IsNotNull && MuiMenuSpecialistStateCodec.TryRead(ref platform,
			sc, out var sidecar) && (sidecar.Flags &
			MuiMenuSpecialistLayout.FlagCopyStrings) != 0;
	}

	public static bool CaseSensitiveFlag<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sc = MenustripSidecar(ref platform, state, obj);
		return sc.IsNotNull && MuiMenuSpecialistStateCodec.TryRead(ref platform,
			sc, out var sidecar) && (sidecar.Flags &
			MuiMenuSpecialistLayout.FlagCaseSensitive) != 0;
	}

	// ---- Attribute set -------------------------------------------------------

	// Apply a menu attribute honoring the official I/S/G policy. `isInit`
	// selects the construction path (init-only latches are only honored then);
	// `notify` requests a runtime-change notification. `changed` reports whether
	// the runtime value actually moved so callers can gate notifications.
	public static bool SetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value, bool isInit, bool notify,
		out bool changed) where TPlatform : struct, IMuiHeadlessPlatform
	{
		changed = false;
		var sc = Sidecar(ref platform, state, obj);
		if (sc.IsNull) return false;
		if (!MuiMenuSpecialistStateCodec.TryRead(ref platform, sc,
			out var sidecar)) return false;
		var cls = (MuiMenuSpecialistClass)sidecar.Class;

		switch (attribute)
		{
			// -- Menustrip -- [ISG] Enabled; [I..] CaseSensitive
			case MuiMenuAttributes.Menustrip_Enabled:
				if (cls != MuiMenuSpecialistClass.Menustrip) return false;
				changed = SetBool(ref platform, state, obj, sc, attribute, value,
					isInit, notify);
				return true;
			case MuiMenuAttributes.Menustrip_CaseSensitive:
				if (cls != MuiMenuSpecialistClass.Menustrip) return false;
				if (isInit) changed = SetFlag(ref platform, sc,
					MuiMenuSpecialistLayout.FlagCaseSensitive, value != 0);
				return true;

			// -- Menu -- [ISG] Enabled/Title; [I..] CopyStrings
			case MuiMenuAttributes.Menu_Enabled:
				if (cls != MuiMenuSpecialistClass.Menu) return false;
				changed = SetBool(ref platform, state, obj, sc, attribute, value,
					isInit, notify);
				return true;
			case MuiMenuAttributes.Menu_Title:
				if (cls != MuiMenuSpecialistClass.Menu) return false;
				return SetString(ref platform, state, obj, sc, attribute,
					MuiMenuOwnedSlot.Title, value, isInit, notify,
					out changed);
			case MuiMenuAttributes.Menu_CopyStrings:
				if (cls != MuiMenuSpecialistClass.Menu) return false;
				if (isInit) changed = SetFlag(ref platform, sc,
					MuiMenuSpecialistLayout.FlagCopyStrings, value != 0);
				return true;

			// -- Menuitem -- [ISG] Title/Shortcut/Checkit/Checked/Toggle/
			//    CommandString/Enabled/Exclude; [I..] CopyStrings/Menuitem
			case MuiMenuAttributes.Menuitem_Title:
				if (cls != MuiMenuSpecialistClass.Menuitem) return false;
				return SetString(ref platform, state, obj, sc, attribute,
					MuiMenuOwnedSlot.Title, value, isInit, notify,
					out changed);
			case MuiMenuAttributes.Menuitem_Shortcut:
				if (cls != MuiMenuSpecialistClass.Menuitem) return false;
				return SetString(ref platform, state, obj, sc, attribute,
					MuiMenuOwnedSlot.Shortcut, value, isInit, notify,
					out changed);
			case MuiMenuAttributes.Menuitem_Checkit:
			case MuiMenuAttributes.Menuitem_Toggle:
			case MuiMenuAttributes.Menuitem_CommandString:
			case MuiMenuAttributes.Menuitem_Enabled:
				if (cls != MuiMenuSpecialistClass.Menuitem) return false;
				changed = SetBool(ref platform, state, obj, sc, attribute, value,
					isInit, notify);
				return true;
			case MuiMenuAttributes.Menuitem_Exclude:
				if (cls != MuiMenuSpecialistClass.Menuitem) return false;
				changed = SetScalar(ref platform, state, obj, sc, attribute, value,
					isInit, notify);
				return true;
			case MuiMenuAttributes.Menuitem_Checked:
				if (cls != MuiMenuSpecialistClass.Menuitem) return false;
				changed = SetChecked(ref platform, state, obj, value != 0,
					!isInit && notify);
				if (changed && !isInit) Notify(ref platform, state, sc, attribute,
					value != 0 ? 1u : 0u);
				return true;
			case MuiMenuAttributes.Menuitem_CopyStrings:
				if (cls != MuiMenuSpecialistClass.Menuitem) return false;
				if (isInit) changed = SetFlag(ref platform, sc,
					MuiMenuSpecialistLayout.FlagCopyStrings, value != 0);
				return true;
			case MuiMenuAttributes.Menuitem_Menuitem:
				// [I..] convenience: adopt an already-created sub-item.
				if (cls != MuiMenuSpecialistClass.Menuitem || !isInit) return false;
				changed = AddChild(ref platform, state, obj, APTR.FromPointer(value));
				return true;

			// -- Family -- [I..] Child (adopt at construction)
			case MuiMenuAttributes.Family_Child:
				if (!isInit) return false;
				changed = AddChild(ref platform, state, obj, APTR.FromPointer(value));
				return true;
		}
		return false;
	}

	// Set MUIA_Menuitem_Checked, applying mutual exclusion across siblings when
	// the item becomes checked. Returns whether the item's own checked state
	// changed.
	private static bool SetChecked<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, bool value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var current = BoolAttribute(ref platform, state, obj,
			MuiMenuAttributes.Menuitem_Checked);
		if (current == value)
			return false;
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiMenuAttributes.Menuitem_Checked, value ? 1u : 0u, notify);
		if (value) ApplyExclusion(ref platform, state, obj);
		return true;
	}

	// When `obj` becomes checked, uncheck every sibling whose position bit is
	// set in this item's MUIA_Menuitem_Exclude mask.
	private static void ApplyExclusion<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			MuiMenuAttributes.Menuitem_Exclude, out var mask) || mask == 0)
			return;
		var parent = ParentObject(ref platform, state, obj);
		if (parent.IsNull) return;
		for (var index = 0u; index < 32; index++)
		{
			if ((mask & (1u << (int)index)) == 0) continue;
			var sibling = MuiFamilyCore.GetChild(ref platform, state, parent,
				(int)index, APTR.Null);
			if (sibling.IsNull || sibling.Raw == obj.Raw) continue;
			if (Classify(ref platform, state, sibling) !=
				MuiMenuSpecialistClass.Menuitem) continue;
			if (BoolAttribute(ref platform, state, sibling,
				MuiMenuAttributes.Menuitem_Checked))
				MuiHeadlessObjectCore.SetAttribute(ref platform, state, sibling,
					MuiMenuAttributes.Menuitem_Checked, 0, true);
		}
	}

	// Failure-atomic string set governed by CopyStrings. When copying, the
	// incoming value is duplicated into a fresh class-owned block *before*
	// anything is
	// released; a failed copy leaves the previous value and owned block intact
	// and returns false. On success the previous owned block (if any) is freed.
	// When not copying, the caller pointer is referenced directly and any
	// previous owned copy is released.
	private static bool SetString<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR sc, uint attribute, MuiMenuOwnedSlot slot,
		uint value, bool isInit, bool notify, out bool changed)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		changed = false;
		if (!MuiMenuSpecialistStateCodec.TryRead(ref platform, sc,
			out var sidecar)) return false;
		var copy = (sidecar.Flags &
			MuiMenuSpecialistLayout.FlagCopyStrings) != 0;

		APTR effective;
		APTR ownedCopy = APTR.Null;
		uint ownedCopySize = 0;
		if (copy && value != 0)
		{
				if (!CopyString(ref platform, APTR.FromPointer(value), out ownedCopy,
					out ownedCopySize)) return false;   // atomic: nothing touched
				effective = ownedCopy;
		}
		else
		{
			effective = APTR.FromPointer(value);
		}

		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out var previous);
		changed = previous != effective.Raw;
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			effective.Raw, !isInit && notify && changed);

		var oldOwned = OwnedPointer(sidecar, slot);
		var oldOwnedSize = OwnedSize(sidecar, slot);
		SetOwned(ref sidecar, slot, ownedCopy, ownedCopySize);
		if (!MuiMenuSpecialistStateCodec.Write(ref platform, sc, sidecar))
		{
			if (ownedCopy.IsNotNull)
			{
				platform.Clear(ownedCopy, ownedCopySize);
				platform.Free(ownedCopy, ownedCopySize);
			}
			return false;
		}
		if (oldOwned.IsNotNull)
		{
			platform.Clear(oldOwned, oldOwnedSize);
			platform.Free(oldOwned, oldOwnedSize);
		}
		if (!isInit && changed) Notify(ref platform, state, sc, attribute,
			effective.Raw);
		return true;
	}

	private static bool SetBool<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR sc, uint attribute, uint value, bool isInit, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		SetScalar(ref platform, state, obj, sc, attribute, value != 0 ? 1u : 0u,
			isInit, notify);

	private static bool SetScalar<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR sc, uint attribute, uint value, bool isInit, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out var previous);
		var changed = previous != value;
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			value, !isInit && notify && changed);
		if (!isInit && changed) Notify(ref platform, state, sc, attribute, value);
		return changed;
	}

	private static bool BoolAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out var value) && value != 0;

	private static bool ReadStored<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out value);
		return true;
	}

	// ---- Notification accessors ----------------------------------------------

	public static uint NotificationCount<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sc = Sidecar(ref platform, state, obj);
		return sc.IsNull || !MuiMenuSpecialistStateCodec.TryRead(ref platform,
			sc, out var sidecar) ? 0 : sidecar.NotifyCount;
	}

	public static uint LastNotifiedAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sc = Sidecar(ref platform, state, obj);
		return sc.IsNull || !MuiMenuSpecialistStateCodec.TryRead(ref platform,
			sc, out var sidecar) ? 0 : sidecar.NotifyAttribute;
	}

	public static uint LastNotifiedValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sc = Sidecar(ref platform, state, obj);
		return sc.IsNull || !MuiMenuSpecialistStateCodec.TryRead(ref platform,
			sc, out var sidecar) ? 0 : sidecar.NotifyValue;
	}

	// ---- Recursive class-owned disposal --------------------------------------

	// Free every menu-owned resource in the subtree rooted at `obj` (copied
	// Title/Shortcut blocks and the sidecar of each node, post-order), then tear
	// down the object graph through the frozen object core, which recursively
	// disposes the child objects and their records. A repeated disposal finds no
	// sidecar and is a safe no-op. Caller-owned (non-copied) strings are never
	// freed.
	public static bool Dispose<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Valid(ref platform, state, obj)) return false;
		FreeSubtreeResources(ref platform, state, obj, 0);
		return MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj);
	}

	private static void FreeSubtreeResources<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint depth)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (depth > 8) return;   // menu trees are shallow; bound the recursion
		var index = 0u;
		while (index < MuiMenuSpecialistLayout.MaximumChildren)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, obj,
				(int)index, APTR.Null);
			if (child.IsNull) break;
			if (Valid(ref platform, state, child))
				FreeSubtreeResources(ref platform, state, child, depth + 1);
			index++;
		}
		FreeOwnResources(ref platform, state, obj);
	}

	private static void FreeOwnResources<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sc = Sidecar(ref platform, state, obj);
		if (sc.IsNull) return;
		FreeOwned(ref platform, sc, MuiMenuOwnedSlot.Title);
		FreeOwned(ref platform, sc, MuiMenuOwnedSlot.Shortcut);
		// Detach and invalidate so a repeated disposal is a no-op.
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiMenuSpecialistLayout.SidecarAttribute, 0, false);
		platform.Clear(sc, MuiMenuSpecialistState.Size);
		platform.Free(sc, MuiMenuSpecialistState.Size);
	}

	private static void FreeOwned<TPlatform>(ref TPlatform platform, APTR sc,
		MuiMenuOwnedSlot slot)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiMenuSpecialistStateCodec.TryRead(ref platform, sc,
			out var sidecar)) return;
		var block = OwnedPointer(sidecar, slot);
		if (block.IsNull) return;
		var size = OwnedSize(sidecar, slot);
		platform.Clear(block, size);
		platform.Free(block, size);
		SetOwned(ref sidecar, slot, APTR.Null, 0);
		MuiMenuSpecialistStateCodec.Write(ref platform, sc, sidecar);
	}

	// ---- Internals -----------------------------------------------------------

	private static bool CopyString<TPlatform>(ref TPlatform platform, APTR source,
		out APTR block, out uint size)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		block = APTR.Null;
		size = 0;
		if (source.IsNull) return false;
		if (!CStringCodec.TryReadLength(ref platform, source,
			MuiMenuSpecialistLayout.MaximumString + 1, out var length))
			return false;
		var total = length + 1;
		var b = MuiHeadlessMemory.Allocate(ref platform, total);
		if (b.IsNull) return false;
		for (var i = 0u; i < total; i++)
			platform.WriteUInt8(b, (int)i, platform.ReadUInt8(source, (int)i));
		block = b;
		size = total;
		return true;
	}

	private static void Notify<TPlatform>(ref TPlatform platform, APTR state,
		APTR sc, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiMenuSpecialistStateCodec.TryRead(ref platform, sc,
			out var sidecar)) return;
		sidecar.NotifyAttribute = attribute;
		sidecar.NotifyValue = value;
		sidecar.NotifyCount = sidecar.NotifyCount == uint.MaxValue
			? uint.MaxValue : sidecar.NotifyCount + 1;
		MuiMenuSpecialistStateCodec.Write(ref platform, sc, sidecar);
	}

	private static bool SetFlag<TPlatform>(ref TPlatform platform, APTR sc,
		uint bit, bool set) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiMenuSpecialistStateCodec.TryRead(ref platform, sc,
			out var sidecar)) return false;
		var flags = sidecar.Flags;
		var updated = set ? flags | bit : flags & ~bit;
		if (updated == flags) return false;
		sidecar.Flags = updated;
		return MuiMenuSpecialistStateCodec.Write(ref platform, sc, sidecar);
	}
}

// Official MG09 menu class attribute and method identifiers, resolved from the
// authority (libraries/mui.h in the frozen MorphOS 3.20 SDK, mirrored in the
// abi-inventory) and kept beside the core so classification and dispatch stay
// byte-exact.
//
// I/S/G policy (from the MUI class autodocs):
//   Menustrip.mui : Family.mui
//     MUIA_Menustrip_CaseSensitive [I..] BOOL  (default FALSE)
//     MUIA_Menustrip_Enabled       [ISG] BOOL  (default TRUE)
//     MUIM_Menustrip_InitChange / _ExitChange / _WillOpen / _Popup
//   Menu.mui : Family.mui
//     MUIA_Menu_CopyStrings        [I..] BOOL
//     MUIA_Menu_Enabled            [ISG] BOOL  (default TRUE)
//     MUIA_Menu_Title              [ISG] STRPTR
//   Menuitem.mui : Family.mui
//     MUIA_Menuitem_Title          [ISG] STRPTR
//     MUIA_Menuitem_Shortcut       [ISG] STRPTR
//     MUIA_Menuitem_Checkit        [ISG] BOOL
//     MUIA_Menuitem_Checked        [ISG] BOOL
//     MUIA_Menuitem_Toggle         [ISG] BOOL
//     MUIA_Menuitem_Exclude        [ISG] LONG
//     MUIA_Menuitem_Enabled        [ISG] BOOL  (default TRUE)
//     MUIA_Menuitem_CommandString  [ISG] BOOL
//     MUIA_Menuitem_CopyStrings    [I..] BOOL
//     MUIA_Menuitem_Menuitem       [I..] Object *
//     MUIA_Menuitem_Trigger        [..G] struct MenuItem *
//   Family.mui (shared superclass)
//     MUIA_Family_Child            [I..] Object *
//     MUIA_Family_ChildCount       [..G] LONG
//     MUIA_Family_List             [..G] struct MinList *
public static class MuiMenuAttributes
{
	// Family.mui methods.
	public const uint Family_AddHead = 0x8042e200u;
	public const uint Family_AddTail = 0x8042d752u;
	public const uint Family_DoChildMethods = 0x80429a3cu;
	public const uint Family_GetChild = 0x8042c556u;
	public const uint Family_Insert = 0x80424d34u;
	public const uint Family_Remove = 0x8042f8a9u;
	public const uint Family_Reorder = 0x80426008u;
	public const uint Family_Sort = 0x80421c49u;
	public const uint Family_Transfer = 0x8042c14au;

	// Family.mui attributes.
	public const uint Family_Child = 0x8042c696u;
	public const uint Family_ChildCount = 0x8042b25au;
	public const uint Family_List = 0x80424b9eu;

	// Menustrip.mui.
	public const uint Menustrip_ExitChange = 0x8042ce4du;
	public const uint Menustrip_InitChange = 0x8042dcd9u;
	public const uint Menustrip_Popup = 0x80420e76u;
	public const uint Menustrip_WillOpen = 0x804230e9u;
	public const uint Menustrip_CaseSensitive = 0x8042d718u;
	public const uint Menustrip_Enabled = 0x8042815bu;

	// Menu.mui.
	public const uint Menu_CopyStrings = 0x8042dbe2u;
	public const uint Menu_Enabled = 0x8042ed48u;
	public const uint Menu_Title = 0x8042a0e3u;

	// Menuitem.mui.
	public const uint Menuitem_Checked = 0x8042562au;
	public const uint Menuitem_Checkit = 0x80425aceu;
	public const uint Menuitem_CommandString = 0x8042b9ccu;
	public const uint Menuitem_CopyStrings = 0x8042dc1bu;
	public const uint Menuitem_Enabled = 0x8042ae0fu;
	public const uint Menuitem_Exclude = 0x80420bc6u;
	public const uint Menuitem_Menuitem = 0x80424b21u;
	public const uint Menuitem_Shortcut = 0x80422030u;
	public const uint Menuitem_Title = 0x804218beu;
	public const uint Menuitem_Toggle = 0x80424d5cu;
	public const uint Menuitem_Trigger = 0x80426f32u;

	// MUIV_Menuitem_Shortcut_Check.
	public const int Menuitem_Shortcut_Check = -1;
}
