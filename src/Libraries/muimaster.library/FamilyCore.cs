/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

public static class MuiFamilyCore
{
	// MorphOS documents Family.mui as the common child-owning class. Group,
	// Application, and Window also own Family-style children even though the
	// historical BOOPSI hierarchy does not expose all of those relationships as
	// a literal Family superclass. Keep this predicate on the guest class chain
	// so generic Family getters work for built-ins and subclasses without a
	// host-side class table.
	internal static bool IsFamilyObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var classRecord = MuiHeadlessObjectCore.ObjectClassRecord(ref platform,
			state, obj);
		for (var depth = 0u; classRecord.IsNotNull &&
			depth < MuiHeadlessLayout.MaximumTraversal; depth++)
		{
			if (!MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
				out var classValue)) return false;
			if (IsFamilyClassName(ref platform, classValue.Name)) return true;
			if (classValue.Super.IsNull) return false;
			classRecord = FindClassByBoopsi(ref platform, state,
				classValue.Super);
		}
		return false;
	}

	private static APTR FindClassByBoopsi<TPlatform>(ref TPlatform platform,
		APTR state, APTR boopsi) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return APTR.Null;
		var current = stateValue.Classes;
		for (var depth = 0u; current.IsNotNull &&
			depth < MuiHeadlessLayout.MaximumTraversal; depth++)
		{
			if (!MuiHeadlessClassCodec.TryRead(ref platform, current,
				out var classValue)) return APTR.Null;
			if (classValue.Boopsi.Raw == boopsi.Raw) return current;
			current = classValue.Next;
		}
		return APTR.Null;
	}

	private static bool IsFamilyClassName<TPlatform>(ref TPlatform platform,
		APTR name) where TPlatform : struct, IMuiGuestMemory
	{
		if (name.IsNull) return false;
		uint hash = 2166136261u;
		var length = 0;
		for (; length < 64; length++)
		{
			if (!platform.IsMapped(name, (uint)length + 1)) return false;
			var ch = platform.ReadUInt8(name, length);
			if (ch == 0) break;
			if (ch >= (byte)'A' && ch <= (byte)'Z') ch = unchecked((byte)(ch + 32));
			hash = (hash ^ ch) * 16777619u;
		}
		if (length == 64 || platform.ReadUInt8(name, length) != 0) return false;
		return hash == 0x118A9B7Au || // Family.mui
			hash == 0x48A3473Fu || // Group.mui
			hash == 0xC243A52Eu || // Application.mui
			hash == 0x61DACF36u || // Window.mui
			hash == 0xEB5623B7u || // Menustrip.mui
			hash == 0xFA7E17F1u || // Menu.mui
			hash == 0x19A57D72u; // Menuitem.mui
	}

	public static bool AddHead<TPlatform>(ref TPlatform platform, APTR state,
		APTR family, APTR child) where TPlatform : struct, IMuiHeadlessPlatform =>
		Insert(ref platform, state, family, child, APTR.Null, true);

	public static bool AddTail<TPlatform>(ref TPlatform platform, APTR state,
		APTR family, APTR child) where TPlatform : struct, IMuiHeadlessPlatform =>
		Insert(ref platform, state, family, child, APTR.Null, false);

	public static bool Insert<TPlatform>(ref TPlatform platform, APTR state,
		APTR family, APTR child, APTR predecessor, bool atHead)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var familyRecord = MuiHeadlessObjectCore.FindObject(ref platform, state,
			family);
		var childRecord = MuiHeadlessObjectCore.FindObject(ref platform, state,
			child);
		if (familyRecord.IsNull || childRecord.IsNull ||
			familyRecord.Raw == childRecord.Raw ||
			!MuiHeadlessObjectCodec.TryRead(ref platform, childRecord,
				out var childValue) || childValue.Parent.IsNotNull ||
			!MuiHeadlessObjectCodec.TryRead(ref platform, familyRecord,
				out var familyValue))
			return false;
		var node = MuiHeadlessMemory.Allocate(ref platform,
			MuiHeadlessChildRecord.Size);
		if (node.IsNull || !platform.RetainObject(child))
		{
			if (node.IsNotNull)
			{
				platform.Clear(node, MuiHeadlessChildRecord.Size);
				platform.Free(node, MuiHeadlessChildRecord.Size);
			}
			return false;
		}
		MuiHeadlessChildRecord nodeValue = default;
		nodeValue.Object = childRecord;
		nodeValue.Owner = familyRecord;
		var head = familyValue.ChildrenHead;
		var tail = familyValue.ChildrenTail;
		APTR previous;
		APTR next;
		if (atHead)
		{
			previous = APTR.Null;
			next = head;
		}
		else if (predecessor.IsNotNull)
		{
			previous = FindChildNode(ref platform, familyRecord, predecessor);
			if (previous.IsNull)
			{
				platform.ReleaseObject(child);
				platform.Free(node, MuiHeadlessChildRecord.Size);
				return false;
			}
			if (!MuiHeadlessChildCodec.TryRead(ref platform, previous,
				out var previousValue))
			{
				platform.ReleaseObject(child);
				platform.Free(node, MuiHeadlessChildRecord.Size);
				return false;
			}
			next = previousValue.Next;
		}
		else
		{
			previous = tail;
			next = APTR.Null;
		}
		nodeValue.Previous = previous;
		nodeValue.Next = next;
		if (!MuiHeadlessChildCodec.Write(ref platform, node, nodeValue))
		{
			platform.ReleaseObject(child);
			platform.Free(node, MuiHeadlessChildRecord.Size);
			return false;
		}
		if (previous.IsNotNull)
		{
			if (!MuiHeadlessChildCodec.TryRead(ref platform, previous,
				out var previousLink)) return false;
			previousLink.Next = node;
			if (!MuiHeadlessChildCodec.Write(ref platform, previous,
				previousLink)) return false;
		}
		else familyValue.ChildrenHead = node;
		if (next.IsNotNull)
		{
			if (!MuiHeadlessChildCodec.TryRead(ref platform, next,
				out var nextLink)) return false;
			nextLink.Previous = node;
			if (!MuiHeadlessChildCodec.Write(ref platform, next, nextLink))
				return false;
		}
		else familyValue.ChildrenTail = node;
		if (!MuiHeadlessObjectCodec.Write(ref platform, familyRecord,
			familyValue)) return false;
		childValue.Parent = familyRecord;
		if (!MuiHeadlessObjectCodec.Write(ref platform, childRecord,
			childValue)) return false;
		// MUIA_HandledEvents registrations follow the named Parent topology.
		// Reconcile after the child is visible in the guest graph; failure to
		// allocate a scheduler node leaves the persisted mask intact for a later
		// retry and does not corrupt the family mutation.
		MuiAreaEventHandlerCore.Reconcile(ref platform, state, child);
		MuiHeadlessMemory.Mutated(ref platform, state);
		return true;
	}

	public static bool Remove<TPlatform>(ref TPlatform platform, APTR state,
		APTR family, APTR child) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var familyRecord = MuiHeadlessObjectCore.FindObject(ref platform, state,
			family);
		var childRecord = MuiHeadlessObjectCore.FindObject(ref platform, state,
			child);
		if (familyRecord.IsNull || childRecord.IsNull) return false;
		var node = FindChildNode(ref platform, familyRecord, child);
		if (node.IsNull) return false;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, childRecord,
			out var childValue)) return false;
		UnlinkNode(ref platform, familyRecord, node);
		childValue.Parent = APTR.Null;
		if (!MuiHeadlessObjectCodec.Write(ref platform, childRecord,
			childValue)) return false;
		MuiAreaEventHandlerCore.Reconcile(ref platform, state, child);
		platform.Clear(node, MuiHeadlessChildRecord.Size);
		platform.Free(node, MuiHeadlessChildRecord.Size);
		platform.ReleaseObject(child);
		MuiHeadlessMemory.Mutated(ref platform, state);
		return true;
	}

	public static APTR GetChild<TPlatform>(ref TPlatform platform, APTR state,
		APTR family, int index, APTR reference)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var familyRecord = MuiHeadlessObjectCore.FindObject(ref platform, state,
			family);
		if (familyRecord.IsNull) return APTR.Null;
		APTR node;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, familyRecord,
			out var familyValue)) return APTR.Null;
		if (index == -1)
			node = familyValue.ChildrenTail;
		else if (index == -2 || index == -4)
		{
			if (reference.IsNull)
				node = familyValue.ChildrenHead;
			else
			{
				node = FindChildNode(ref platform, familyRecord, reference);
				if (node.IsNotNull && MuiHeadlessChildCodec.TryRead(ref platform,
					node, out var nodeValue)) node = nodeValue.Next;
			}
		}
		else if (index == -3)
		{
			if (reference.IsNull)
				node = familyValue.ChildrenTail;
			else
			{
				node = FindChildNode(ref platform, familyRecord, reference);
				if (node.IsNotNull && MuiHeadlessChildCodec.TryRead(ref platform,
					node, out var nodeValue)) node = nodeValue.Previous;
			}
		}
		else if (index < 0) return APTR.Null;
		else
		{
			node = familyValue.ChildrenHead;
			var remaining = index < 0 ? 0 : index;
			while (node.IsNotNull && remaining-- != 0)
			{
				if (!MuiHeadlessChildCodec.TryRead(ref platform, node,
					out var nodeValue)) return APTR.Null;
				node = nodeValue.Next;
			}
		}
		if (node.IsNull || !platform.IsMapped(node, MuiHeadlessChildRecord.Size))
			return APTR.Null;
		if (!MuiHeadlessChildCodec.TryRead(ref platform, node,
			out var childNode)) return APTR.Null;
		var childRecord = childNode.Object;
		if (childRecord.IsNull || !MuiHeadlessObjectCodec.TryRead(ref platform,
			childRecord, out var childValue)) return APTR.Null;
		return childValue.Boopsi;
	}

	public static bool Transfer<TPlatform>(ref TPlatform platform, APTR state,
		APTR destination, APTR source)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (destination.Raw == source.Raw) return true;
		var child = GetChild(ref platform, state, source, 0, APTR.Null);
		uint moved = 0;
		while (child.IsNotNull && moved++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!Remove(ref platform, state, source, child) ||
				!AddTail(ref platform, state, destination, child)) return false;
			child = GetChild(ref platform, state, source, 0, APTR.Null);
		}
		return child.IsNull;
	}

	public static bool Reorder<TPlatform>(ref TPlatform platform, APTR state,
		APTR family, APTR after, APTR objects)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (objects.IsNull) return false;
		var predecessor = after;
		for (var index = 0u; index < MuiHeadlessLayout.MaximumTraversal; index++)
		{
			var cursor = default(MuiFamilyMutationVectorCursor);
			cursor.Base = objects;
			cursor.Index = index;
			if (!MuiFamilyMutationVectorCodec.TryGetEntry(ref platform, cursor,
				out var itemAddress)) return false;
			if (!MuiFamilyMutationVectorCodec.TryRead(ref platform, itemAddress,
				out var item)) return false;
			var child = item.Object;
			if (child.IsNull) return true;
			if (!MoveAfter(ref platform, state, family, child, predecessor))
				return false;
			predecessor = child;
		}
		return false;
	}

	public static bool Sort<TPlatform>(ref TPlatform platform, APTR state,
		APTR family, APTR objects)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Reorder(ref platform, state, family, APTR.Null, objects);

	internal static bool MoveAfter<TPlatform>(ref TPlatform platform, APTR state,
		APTR family, APTR child, APTR predecessor)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var familyRecord = MuiHeadlessObjectCore.FindObject(ref platform, state,
			family);
		if (familyRecord.IsNull) return false;
		var node = FindChildNode(ref platform, familyRecord, child);
		if (node.IsNull) return false;
		var previous = APTR.Null;
		if (predecessor.IsNotNull)
		{
			previous = FindChildNode(ref platform, familyRecord, predecessor);
			if (previous.IsNull) return false;
			if (previous.Raw == node.Raw) return true;
		}
		UnlinkNode(ref platform, familyRecord, node);
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, familyRecord,
			out var familyValue)) return false;
		var next = familyValue.ChildrenHead;
		if (previous.IsNotNull)
		{
			if (!MuiHeadlessChildCodec.TryRead(ref platform, previous,
				out var previousValue)) return false;
			next = previousValue.Next;
		}
		if (!MuiHeadlessChildCodec.TryRead(ref platform, node,
			out var nodeValue)) return false;
		nodeValue.Previous = previous;
		nodeValue.Next = next;
		if (!MuiHeadlessChildCodec.Write(ref platform, node, nodeValue))
			return false;
		if (previous.IsNotNull)
		{
			if (!MuiHeadlessChildCodec.TryRead(ref platform, previous,
				out var previousLink)) return false;
			previousLink.Next = node;
			if (!MuiHeadlessChildCodec.Write(ref platform, previous,
				previousLink)) return false;
		}
		else familyValue.ChildrenHead = node;
		if (next.IsNotNull)
		{
			if (!MuiHeadlessChildCodec.TryRead(ref platform, next,
				out var nextValue)) return false;
			nextValue.Previous = node;
			if (!MuiHeadlessChildCodec.Write(ref platform, next, nextValue))
				return false;
		}
		else familyValue.ChildrenTail = node;
		if (!MuiHeadlessObjectCodec.Write(ref platform, familyRecord,
			familyValue)) return false;
		MuiHeadlessMemory.Mutated(ref platform, state);
		return true;
	}

	internal static void RemoveAllChildren<TPlatform>(ref TPlatform platform,
		APTR state, APTR familyRecord, bool disposeChildren)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, familyRecord,
			out var familyValue)) return;
		var current = familyValue.ChildrenHead;
		familyValue.ChildrenHead = APTR.Null;
		familyValue.ChildrenTail = APTR.Null;
		if (!MuiHeadlessObjectCodec.Write(ref platform, familyRecord,
			familyValue)) return;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!platform.IsMapped(current, MuiHeadlessChildRecord.Size)) break;
			if (!MuiHeadlessChildCodec.TryRead(ref platform, current,
				out var childNode)) break;
			var next = childNode.Next;
			var childRecord = childNode.Object;
			APTR child = APTR.Null;
			if (childRecord.IsNotNull && MuiHeadlessObjectCodec.TryRead(
				ref platform, childRecord, out var childValue))
			{
				childValue.Parent = APTR.Null;
				MuiHeadlessObjectCodec.Write(ref platform, childRecord,
					childValue);
				MuiAreaEventHandlerCore.Reconcile(ref platform, state,
					childValue.Boopsi);
				child = childValue.Boopsi;
			}
			platform.Clear(current, MuiHeadlessChildRecord.Size);
			platform.Free(current, MuiHeadlessChildRecord.Size);
			if (child.IsNotNull)
			{
				var preserveSubWindow = disposeChildren &&
					MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, child,
						MuiWindowPublicCore.IsSubWindow, out var isSubWindow) &&
					isSubWindow != 0;
				if (disposeChildren && !preserveSubWindow)
					MuiHeadlessObjectCore.DisposeObject(ref platform, state, child);
				else platform.ReleaseObject(child);
			}
			current = next;
		}
	}

	internal static void DetachFromParent<TPlatform>(ref TPlatform platform,
		APTR state, APTR childRecord)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, childRecord,
			out var childValue)) return;
		var parent = childValue.Parent;
		if (parent.IsNull || !MuiHeadlessObjectCodec.TryRead(ref platform,
			parent, out _)) return;
		var child = childValue.Boopsi;
		var node = FindChildNode(ref platform, parent, child);
		if (node.IsNull) return;
		UnlinkNode(ref platform, parent, node);
		childValue.Parent = APTR.Null;
		if (!MuiHeadlessObjectCodec.Write(ref platform, childRecord,
			childValue)) return;
		platform.Clear(node, MuiHeadlessChildRecord.Size);
		platform.Free(node, MuiHeadlessChildRecord.Size);
		MuiHeadlessMemory.Mutated(ref platform, state);
	}

	private static APTR FindChildNode<TPlatform>(ref TPlatform platform,
		APTR familyRecord, APTR child)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, familyRecord,
			out var familyValue)) return APTR.Null;
		var current = familyValue.ChildrenHead;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!platform.IsMapped(current, MuiHeadlessChildRecord.Size))
				return APTR.Null;
			if (!MuiHeadlessChildCodec.TryRead(ref platform, current,
				out var childNode)) return APTR.Null;
			var childRecord = childNode.Object;
			if (childRecord.IsNotNull && MuiHeadlessObjectCodec.TryRead(
				ref platform, childRecord, out var childValue) &&
				childValue.Boopsi.Raw == child.Raw) return current;
			current = childNode.Next;
		}
		return APTR.Null;
	}

	private static void UnlinkNode<TPlatform>(ref TPlatform platform,
		APTR familyRecord, APTR node)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessChildCodec.TryRead(ref platform, node,
			out var nodeValue)) return;
		var previous = nodeValue.Previous;
		var next = nodeValue.Next;
		if (previous.IsNotNull && MuiHeadlessChildCodec.TryRead(ref platform,
			previous, out var previousValue))
		{
			previousValue.Next = next;
			MuiHeadlessChildCodec.Write(ref platform, previous, previousValue);
		}
		else if (MuiHeadlessObjectCodec.TryRead(ref platform, familyRecord,
			out var familyValue))
		{
			familyValue.ChildrenHead = next;
			if (next.IsNull) familyValue.ChildrenTail = APTR.Null;
			MuiHeadlessObjectCodec.Write(ref platform, familyRecord,
				familyValue);
		}
		if (next.IsNotNull && MuiHeadlessChildCodec.TryRead(ref platform,
			next, out var nextValue))
		{
			nextValue.Previous = previous;
			MuiHeadlessChildCodec.Write(ref platform, next, nextValue);
		}
		else if (MuiHeadlessObjectCodec.TryRead(ref platform, familyRecord,
			out var tailValue))
		{
			tailValue.ChildrenTail = previous;
			if (previous.IsNull) tailValue.ChildrenHead = APTR.Null;
			MuiHeadlessObjectCodec.Write(ref platform, familyRecord,
				tailValue);
		}
	}
}
