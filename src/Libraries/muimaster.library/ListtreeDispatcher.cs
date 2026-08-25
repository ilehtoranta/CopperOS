/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// These records mirror the MorphOS 3.20 Listtree.mcc MUIP_* declarations.
// Keep the ABI field names here (ListNode, TreeNode, Result, and so on) so
// dispatch code consumes a typed packet rather than an anonymous offset list.

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeMethodMessage
{
	public const uint Size = 4;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeSetMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeGetMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeInsertMessage
{
	public const uint Size = 24;
	public uint MethodId;
	public uint Name;
	public uint User;
	public uint ListNode;
	public uint PrevNode;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeRemoveMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint ListNode;
	public uint TreeNode;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeGetEntryMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint Node;
	public uint Position;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeOpenCloseMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint ListNode;
	public uint TreeNode;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeSortMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint ListNode;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeGetNrMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint TreeNode;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeMoveExchangeMessage
{
	public const uint Size = 24;
	public uint MethodId;
	public uint OldListNode;
	public uint OldTreeNode;
	public uint NewListNode;
	public uint NewTreeNode;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeRenameMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint TreeNode;
	public uint NewName;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeFindNameMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint ListNode;
	public uint Name;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeDropMarkMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Entry;
	public uint Values;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeTestPosMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint X;
	public uint Y;
	public uint Result;
}

// Method dispatch for the external Listtree.mcc component. This is a standalone
// entry point, deliberately NOT wired into MuiCollectionDispatcher or
// MuiLayoutDispatcher: Listtree is an external, loader-discoverable class per
// docs/Libraries/MorphOs320Mui/packaging.md, so it never composes into the
// built-in .mui method graph. The dispatcher only claims a method when the
// target object is exactly a "Listtree.mcc" instance; otherwise it returns 0
// (unclaimed) so a caller can continue elsewhere without pulling the built-in
// collection classes into the external component's graph.
public static class MuiListtreeDispatcher
{
	// The external Listtree path keeps its pointer decode local to the
	// dispatcher so the freestanding closure can materialize one named event
	// record without pulling the larger Listview input graph into the external
	// component. The field cursor remains the sole owner of Intuition ABI
	// boundaries; callers receive a typed struct.
	private static bool TryReadPointerInput<TPlatform>(ref TPlatform platform,
		APTR message, out MuiIntuiPointerMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiIntuiPointerMessage.MinimumSize)) return false;
		uint messageClass;
		ushort code;
		ushort qualifier;
		uint iAddress;
		if (!MuiListInputRecordFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiListInputRecordKind.IntuiMessage,
			MuiListInputRecordField.Class, out messageClass) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt16(ref platform,
				message, MuiListInputRecordKind.IntuiMessage,
				MuiListInputRecordField.Code, out code) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt16(ref platform,
				message, MuiListInputRecordKind.IntuiMessage,
				MuiListInputRecordField.Qualifier, out qualifier) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiListInputRecordKind.IntuiMessage,
				MuiListInputRecordField.IAddress, out iAddress) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt16(ref platform,
				message, MuiListInputRecordKind.IntuiMessage,
				MuiListInputRecordField.MouseX, out var mouseX) ||
			!MuiListInputRecordFieldCursorCodec.TryReadUInt16(ref platform,
				message, MuiListInputRecordKind.IntuiMessage,
				MuiListInputRecordField.MouseY, out var mouseY)) return false;
		value.Class = messageClass;
		value.Code = code;
		value.Qualifier = qualifier;
		value.IAddress = iAddress;
		value.MouseX = unchecked((short)mouseX);
		value.MouseY = unchecked((short)mouseY);
		return true;
	}

	// Native-safe fixed packet subset. Keep the bounded tree mutations and
	// mixed-width TestPos result in an explicit typed-record route so
	// freestanding targets do not need to carry callback-bearing messages when
	// qualifying the core Listtree ABI. The public Dispatch method below remains
	// the complete MorphOS selector surface; this helper is also useful to a
	// loader that has already separated fixed packets from hook callbacks.
	public static uint DispatchTreePacket<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		// Read the four-byte selector through the scalar codec. The full message
		// payloads remain named structs; keeping this header scalar avoids asking
		// the freestanding compiler to materialize a one-field out-record before
		// the first packet branch.
		if (!MuiListtreeMessageCodec.TryReadMethodIdValue(ref platform, message,
			out var method) || !MuiListtreeCore.IsListtree(ref platform, state, obj))
			return 0;
		if (method == MuiListtreeCore.MethodSetup)
		{
			if (!MuiLayoutPacketCodec.TryReadRenderInfo(ref platform, message,
				MuiListtreeCore.MethodSetup, out var setup)) return 0;
			return MuiListtreeCore.Setup(ref platform, state, obj,
				APTR.FromPointer(setup.RenderInfo)) ? 1u : 0u;
		}
		if (method == MuiListtreeCore.MethodCleanup)
			return MuiListtreeCore.Cleanup(ref platform, state, obj) ? 1u : 0u;
		if (method == MuiListtreeCore.MethodShow)
			return MuiListtreeCore.Show(ref platform, state, obj) ? 1u : 0u;
		if (method == MuiListtreeCore.MethodHide)
			return MuiListtreeCore.Hide(ref platform, state, obj) ? 1u : 0u;
		if (method == MuiListtreeCore.MethodHandleInput)
		{
			if (!MuiCollectionSurfaceMessageCodec.TryReadHandleInput(ref platform,
				message, out var input)) return 0;
			if (input.MuiKey == -1)
			{
				if (!TryReadPointerInput(ref platform,
					APTR.FromPointer(input.IntuiMessage), out var pointer)) return 0;
				return MuiListtreeCore.HandlePointerInput(ref platform, state, obj,
					pointer) ? 1u : 0u;
			}
			return MuiListtreeCore.HandleInput(ref platform, state, obj,
				APTR.FromPointer(input.IntuiMessage), input.MuiKey) ? 1u : 0u;
		}
		if (method == MuiCollectionSurfaceMessageCodec.Layout)
		{
			if (!MuiCollectionSurfaceMessageCodec.TryReadLayout(ref platform,
				message, out var layout)) return 0;
			return MuiListtreeCore.Layout(ref platform, state, obj,
				unchecked((int)layout.Left), unchecked((int)layout.Top),
				unchecked((int)layout.Width), unchecked((int)layout.Height)) ? 1u : 0u;
		}
		if (method == MuiCollectionSurfaceMessageCodec.AskMinMax)
		{
			if (!MuiCollectionSurfaceMessageCodec.TryReadAskMinMax(ref platform,
				message, out var askMinMax)) return 0;
			return MuiListtreeCore.AskMinMax(ref platform, state, obj,
				APTR.FromPointer(askMinMax.Storage)) ? 1u : 0u;
		}
		if (method == MuiCollectionSurfaceMessageCodec.Draw)
		{
			if (!MuiCollectionSurfaceMessageCodec.TryReadDraw(ref platform,
				message, out var draw)) return 0;
			return MuiListtreeCore.Draw(ref platform, state, obj, draw.Flags)
				? 1u : 0u;
		}
		if (method == MuiListtreeMessageCodec.Set ||
			method == MuiListtreeMessageCodec.NoNotifySet)
		{
			if (!platform.IsMapped(message, MuiListtreeSetMessage.Size)) return 0;
			var packet = default(MuiListtreeSetMessage);
			packet.MethodId = method;
			if (!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Set, MuiListtreeField.Attribute,
				out packet.Attribute) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.Set, MuiListtreeField.Value,
					out packet.Value)) return 0;
			return MuiListtreeCore.SetAttribute(ref platform, state, obj,
				packet.Attribute, packet.Value,
				method == MuiListtreeMessageCodec.Set) ? 1u : 0u;
		}
		if (method == MuiListtreeMessageCodec.Get)
		{
			if (!platform.IsMapped(message, MuiListtreeGetMessage.Size)) return 0;
			var packet = default(MuiListtreeGetMessage);
			packet.MethodId = method;
			if (!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Get, MuiListtreeField.Attribute,
				out packet.Attribute) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.Get, MuiListtreeField.Storage,
					out packet.Storage)) return 0;
			uint value;
			if (!MuiListtreeCore.GetAttribute(ref platform, state, obj,
				packet.Attribute, out value)) return 0;
			var storage = APTR.FromPointer(packet.Storage);
			if (storage.IsNotNull)
				MuiGuestUlongStorageFieldCursorCodec.TryWrite(ref platform, storage,
					MuiGuestUlongStorageField.Value, value);
			return 1;
		}
		if (method == MuiListtreeMessageCodec.Insert)
		{
			if (!platform.IsMapped(message, MuiListtreeInsertMessage.Size)) return 0;
			var packet = default(MuiListtreeInsertMessage);
			packet.MethodId = method;
			if (!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Insert, MuiListtreeField.Name,
				out packet.Name) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.Insert, MuiListtreeField.User,
					out packet.User) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.Insert, MuiListtreeField.ListNode,
					out packet.ListNode) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.Insert, MuiListtreeField.PrevNode,
					out packet.PrevNode) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.Insert, MuiListtreeField.Flags,
					out packet.Flags)) return 0;
			var inserted = MuiListtreeCore.Insert(ref platform, state, obj,
				APTR.FromPointer(packet.Name), APTR.FromPointer(packet.User),
				APTR.FromPointer(packet.ListNode), APTR.FromPointer(packet.PrevNode),
				packet.Flags);
			return inserted.Raw;
		}
		if (method == MuiListtreeMessageCodec.GetEntry)
		{
			if (!platform.IsMapped(message, MuiListtreeGetEntryMessage.Size)) return 0;
			var packet = default(MuiListtreeGetEntryMessage);
			packet.MethodId = method;
			if (!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.GetEntry, MuiListtreeField.Node,
				out packet.Node) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.GetEntry, MuiListtreeField.Position,
					out packet.Position) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.GetEntry, MuiListtreeField.Flags,
					out packet.Flags)) return 0;
			var entry = MuiListtreeCore.GetEntry(ref platform, state, obj,
				APTR.FromPointer(packet.Node), unchecked((int)packet.Position),
				packet.Flags);
			return entry.Raw;
		}
		if (method == MuiListtreeMessageCodec.Open ||
			method == MuiListtreeMessageCodec.Close)
		{
			if (!platform.IsMapped(message, MuiListtreeOpenCloseMessage.Size)) return 0;
			var packet = default(MuiListtreeOpenCloseMessage);
			packet.MethodId = method;
			if (!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.OpenClose, MuiListtreeField.ListNode,
				out packet.ListNode) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.OpenClose, MuiListtreeField.TreeNode,
					out packet.TreeNode) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.OpenClose, MuiListtreeField.Flags,
					out packet.Flags)) return 0;
			var listNode = APTR.FromPointer(packet.ListNode);
			var treeNode = APTR.FromPointer(packet.TreeNode);
			if (method == MuiListtreeMessageCodec.Open)
				return MuiListtreeCore.Open(ref platform, state, obj,
					listNode, treeNode, packet.Flags) ? 1u : 0u;
			return MuiListtreeCore.Close(ref platform, state, obj,
				listNode, treeNode, packet.Flags) ? 1u : 0u;
		}
		if (method == MuiListtreeMessageCodec.Sort)
		{
			if (!platform.IsMapped(message, MuiListtreeSortMessage.Size)) return 0;
			var packet = default(MuiListtreeSortMessage);
			packet.MethodId = method;
			if (!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Sort, MuiListtreeField.ListNode,
				out packet.ListNode) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.Sort, MuiListtreeField.Flags,
					out packet.Flags)) return 0;
			return MuiListtreeCore.Sort(ref platform, state, obj,
				APTR.FromPointer(packet.ListNode), packet.Flags) ? 1u : 0u;
		}
		if (method == MuiListtreeMessageCodec.Rename)
		{
			if (!platform.IsMapped(message, MuiListtreeRenameMessage.Size)) return 0;
			var packet = default(MuiListtreeRenameMessage);
			packet.MethodId = method;
			if (!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Rename, MuiListtreeField.TreeNode,
				out packet.TreeNode) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.Rename, MuiListtreeField.NewName,
					out packet.NewName) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.Rename, MuiListtreeField.Flags,
					out packet.Flags)) return 0;
			return MuiListtreeCore.Rename(ref platform, state, obj,
				APTR.FromPointer(packet.TreeNode), APTR.FromPointer(packet.NewName),
				packet.Flags) ? 1u : 0u;
		}
		if (method == MuiListtreeMessageCodec.FindName)
		{
			if (!platform.IsMapped(message, MuiListtreeFindNameMessage.Size)) return 0;
			var packet = default(MuiListtreeFindNameMessage);
			packet.MethodId = method;
			if (!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.FindName, MuiListtreeField.ListNode,
				out packet.ListNode) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.FindName, MuiListtreeField.Name,
					out packet.Name) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.FindName, MuiListtreeField.Flags,
					out packet.Flags)) return 0;
			return MuiListtreeCore.FindName(ref platform, state, obj,
				APTR.FromPointer(packet.ListNode), APTR.FromPointer(packet.Name),
				packet.Flags).Raw;
		}
		if (method == MuiListtreeMessageCodec.Move ||
			method == MuiListtreeMessageCodec.Exchange)
		{
			if (!platform.IsMapped(message, MuiListtreeMoveExchangeMessage.Size))
				return 0;
			var packet = default(MuiListtreeMoveExchangeMessage);
			packet.MethodId = method;
			if (!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.MoveExchange, MuiListtreeField.OldListNode,
				out packet.OldListNode) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.MoveExchange, MuiListtreeField.OldTreeNode,
					out packet.OldTreeNode) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.MoveExchange, MuiListtreeField.NewListNode,
					out packet.NewListNode) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.MoveExchange, MuiListtreeField.NewTreeNode,
					out packet.NewTreeNode) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.MoveExchange, MuiListtreeField.Flags,
					out packet.Flags)) return 0;
			if (method == MuiListtreeMessageCodec.Move)
				return MuiListtreeCore.Move(ref platform, state, obj,
					APTR.FromPointer(packet.OldListNode),
					APTR.FromPointer(packet.OldTreeNode),
					APTR.FromPointer(packet.NewListNode),
					APTR.FromPointer(packet.NewTreeNode), packet.Flags) ? 1u : 0u;
			return MuiListtreeCore.Exchange(ref platform, state, obj,
				APTR.FromPointer(packet.OldListNode),
				APTR.FromPointer(packet.OldTreeNode),
				APTR.FromPointer(packet.NewListNode),
				APTR.FromPointer(packet.NewTreeNode), packet.Flags) ? 1u : 0u;
		}
		if (method == MuiListtreeMessageCodec.SetDropMark)
		{
			if (!platform.IsMapped(message, MuiListtreeDropMarkMessage.Size))
				return 0;
			var packet = default(MuiListtreeDropMarkMessage);
			packet.MethodId = method;
			if (!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.DropMark, MuiListtreeField.Entry,
				out packet.Entry) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.DropMark, MuiListtreeField.Values,
					out packet.Values)) return 0;
			return MuiListtreeCore.SetDropMark(ref platform, state, obj,
				unchecked((int)packet.Entry), packet.Values) ? 1u : 0u;
		}
		if (method == MuiListtreeMessageCodec.TestPos)
		{
			if (!platform.IsMapped(message, MuiListtreeTestPosMessage.Size))
				return 0;
			var packet = default(MuiListtreeTestPosMessage);
			packet.MethodId = method;
			if (!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.TestPos, MuiListtreeField.X,
				out packet.X) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.TestPos, MuiListtreeField.Y,
					out packet.Y) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.TestPos, MuiListtreeField.Result,
					out packet.Result)) return 0;
			return MuiListtreeCore.TestPos(ref platform, state, obj,
				unchecked((int)packet.X), unchecked((int)packet.Y),
				APTR.FromPointer(packet.Result)) ? 1u : 0u;
		}
		if (method == MuiListtreeMessageCodec.GetNr)
		{
			if (!platform.IsMapped(message, MuiListtreeGetNrMessage.Size)) return 0;
			var packet = default(MuiListtreeGetNrMessage);
			packet.MethodId = method;
			if (!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.GetNr, MuiListtreeField.TreeNode,
				out packet.TreeNode) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.GetNr, MuiListtreeField.Flags,
					out packet.Flags)) return 0;
			return MuiListtreeCore.GetNr(ref platform, state, obj,
				APTR.FromPointer(packet.TreeNode), packet.Flags);
		}
		if (method == MuiListtreeMessageCodec.Remove)
		{
			if (!platform.IsMapped(message, MuiListtreeRemoveMessage.Size)) return 0;
			var packet = default(MuiListtreeRemoveMessage);
			packet.MethodId = method;
			if (!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiListtreePacketKind.Remove, MuiListtreeField.ListNode,
				out packet.ListNode) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.Remove, MuiListtreeField.TreeNode,
					out packet.TreeNode) ||
				!MuiListtreeFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiListtreePacketKind.Remove, MuiListtreeField.Flags,
					out packet.Flags)) return 0;
			var removed = MuiListtreeCore.Remove(ref platform, state, obj,
				APTR.FromPointer(packet.ListNode), APTR.FromPointer(packet.TreeNode),
				packet.Flags);
			return removed ? 1u : 0u;
		}
		return 0;
	}

	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiListtreeMessageCodec.TryReadMethodId(ref platform, message,
			out var methodHeader) ||
			!MuiListtreeCore.IsListtree(ref platform, state, obj)) return 0;
		var method = methodHeader.MethodId;
		switch (method)
		{
			case MuiListtreeCore.MethodSetup:
				if (!MuiLayoutPacketCodec.TryReadRenderInfo(ref platform, message,
					MuiListtreeCore.MethodSetup, out var setupPacket)) return 0;
				return MuiListtreeCore.Setup(ref platform, state, obj,
					APTR.FromPointer(setupPacket.RenderInfo)) ? 1u : 0u;
			case MuiListtreeCore.MethodCleanup:
				return MuiListtreeCore.Cleanup(ref platform, state, obj) ? 1u : 0u;
			case MuiListtreeCore.MethodShow:
				return MuiListtreeCore.Show(ref platform, state, obj) ? 1u : 0u;
			case MuiListtreeCore.MethodHide:
				return MuiListtreeCore.Hide(ref platform, state, obj) ? 1u : 0u;
			case MuiListtreeCore.MethodHandleInput:
				if (!MuiCollectionSurfaceMessageCodec.TryReadHandleInput(ref platform,
					message, out var inputPacket)) return 0;
				if (inputPacket.MuiKey == -1)
				{
					if (!TryReadPointerInput(ref platform,
						APTR.FromPointer(inputPacket.IntuiMessage),
						out var pointerPacket)) return 0;
					return MuiListtreeCore.HandlePointerInput(ref platform, state, obj,
						pointerPacket) ? 1u : 0u;
				}
				return MuiListtreeCore.HandleInput(ref platform, state, obj,
					APTR.FromPointer(inputPacket.IntuiMessage), inputPacket.MuiKey)
					? 1u : 0u;
			case MuiCollectionSurfaceMessageCodec.Layout:
				if (!MuiCollectionSurfaceMessageCodec.TryReadLayout(ref platform,
					message, out var layoutPacket)) return 0;
				return MuiListtreeCore.Layout(ref platform, state, obj,
					unchecked((int)layoutPacket.Left), unchecked((int)layoutPacket.Top),
					unchecked((int)layoutPacket.Width),
					unchecked((int)layoutPacket.Height)) ? 1u : 0u;
			case MuiCollectionSurfaceMessageCodec.AskMinMax:
				if (!MuiCollectionSurfaceMessageCodec.TryReadAskMinMax(ref platform,
					message, out var askMinMaxPacket)) return 0;
				return MuiListtreeCore.AskMinMax(ref platform, state, obj,
					APTR.FromPointer(askMinMaxPacket.Storage)) ? 1u : 0u;
			case MuiCollectionSurfaceMessageCodec.Draw:
				if (!MuiCollectionSurfaceMessageCodec.TryReadDraw(ref platform,
					message, out var drawPacket)) return 0;
				return MuiListtreeCore.Draw(ref platform, state, obj,
					drawPacket.Flags) ? 1u : 0u;
			case MuiListtreeMessageCodec.Insert:
				if (!MuiListtreeMessageCodec.TryReadInsert(ref platform, message,
					out var insertPacket))
					return 0;
				return MuiListtreeCore.Insert(ref platform, state, obj,
					APTR.FromPointer(insertPacket.Name),
					APTR.FromPointer(insertPacket.User),
					APTR.FromPointer(insertPacket.ListNode),
					APTR.FromPointer(insertPacket.PrevNode),
					insertPacket.Flags).Raw;
			case MuiListtreeMessageCodec.Remove:
				if (!MuiListtreeMessageCodec.TryReadRemove(ref platform, message,
					out var removePacket))
					return 0;
				return MuiListtreeCore.Remove(ref platform, state, obj,
					APTR.FromPointer(removePacket.ListNode),
					APTR.FromPointer(removePacket.TreeNode),
					removePacket.Flags) ? 1u : 0u;
			case MuiListtreeMessageCodec.GetEntry:
				if (!MuiListtreeMessageCodec.TryReadGetEntry(ref platform, message,
					out var getEntryPacket))
					return 0;
				return MuiListtreeCore.GetEntry(ref platform, state, obj,
					APTR.FromPointer(getEntryPacket.Node),
					unchecked((int)getEntryPacket.Position),
					getEntryPacket.Flags).Raw;
			case MuiListtreeMessageCodec.GetNr:
				if (!MuiListtreeMessageCodec.TryReadGetNr(ref platform, message,
					MuiListtreeMessageCodec.GetNr, out var getNrPacket)) return 0;
				return MuiListtreeCore.GetNr(ref platform, state, obj,
					APTR.FromPointer(getNrPacket.TreeNode), getNrPacket.Flags);
			case MuiListtreeMessageCodec.Open:
				if (!MuiListtreeMessageCodec.TryReadOpenClose(ref platform, message,
					MuiListtreeMessageCodec.Open, out var openPacket)) return 0;
				return MuiListtreeCore.Open(ref platform, state, obj,
					APTR.FromPointer(openPacket.ListNode),
					APTR.FromPointer(openPacket.TreeNode), openPacket.Flags) ? 1u : 0u;
			case MuiListtreeMessageCodec.Close:
				if (!MuiListtreeMessageCodec.TryReadOpenClose(ref platform, message,
					MuiListtreeMessageCodec.Close, out var closePacket)) return 0;
				return MuiListtreeCore.Close(ref platform, state, obj,
					APTR.FromPointer(closePacket.ListNode),
					APTR.FromPointer(closePacket.TreeNode), closePacket.Flags) ? 1u : 0u;
			case MuiListtreeMessageCodec.Sort:
				if (!MuiListtreeMessageCodec.TryReadSort(ref platform, message,
					MuiListtreeMessageCodec.Sort, out var sortPacket)) return 0;
				return MuiListtreeCore.Sort(ref platform, state, obj,
					APTR.FromPointer(sortPacket.ListNode), sortPacket.Flags) ? 1u : 0u;
			case MuiListtreeMessageCodec.Move:
				if (!MuiListtreeMessageCodec.TryReadMoveExchange(ref platform, message,
					MuiListtreeMessageCodec.Move, out var movePacket)) return 0;
				return MuiListtreeCore.Move(ref platform, state, obj,
					APTR.FromPointer(movePacket.OldListNode),
					APTR.FromPointer(movePacket.OldTreeNode),
					APTR.FromPointer(movePacket.NewListNode),
					APTR.FromPointer(movePacket.NewTreeNode), movePacket.Flags) ? 1u : 0u;
			case MuiListtreeMessageCodec.Exchange:
				if (!MuiListtreeMessageCodec.TryReadMoveExchange(ref platform, message,
					MuiListtreeMessageCodec.Exchange, out var exchangePacket)) return 0;
				return MuiListtreeCore.Exchange(ref platform, state, obj,
					APTR.FromPointer(exchangePacket.OldListNode),
					APTR.FromPointer(exchangePacket.OldTreeNode),
					APTR.FromPointer(exchangePacket.NewListNode),
					APTR.FromPointer(exchangePacket.NewTreeNode), exchangePacket.Flags) ? 1u : 0u;
			case MuiListtreeMessageCodec.Rename:
				if (!MuiListtreeMessageCodec.TryReadRename(ref platform, message,
					out var renamePacket))
					return 0;
				return MuiListtreeCore.Rename(ref platform, state, obj,
					APTR.FromPointer(renamePacket.TreeNode),
					APTR.FromPointer(renamePacket.NewName), renamePacket.Flags) ? 1u : 0u;
			case MuiListtreeMessageCodec.FindName:
				if (!MuiListtreeMessageCodec.TryReadFindName(ref platform, message,
					out var findNamePacket))
					return 0;
				return MuiListtreeCore.FindName(ref platform, state, obj,
					APTR.FromPointer(findNamePacket.ListNode),
					APTR.FromPointer(findNamePacket.Name), findNamePacket.Flags).Raw;
			case MuiListtreeMessageCodec.SetDropMark:
				if (!MuiListtreeMessageCodec.TryReadDropMark(ref platform, message,
					out var dropMarkPacket))
					return 0;
				return MuiListtreeCore.SetDropMark(ref platform, state, obj,
					unchecked((int)dropMarkPacket.Entry), dropMarkPacket.Values)
					? 1u : 0u;
			case MuiListtreeMessageCodec.TestPos:
				if (!MuiListtreeMessageCodec.TryReadTestPos(ref platform, message,
					out var testPosPacket))
					return 0;
				return MuiListtreeCore.TestPos(ref platform, state, obj,
					unchecked((int)testPosPacket.X), unchecked((int)testPosPacket.Y),
					APTR.FromPointer(testPosPacket.Result)) ? 1u : 0u;
			case MuiListtreeMessageCodec.Set:
			case MuiListtreeMessageCodec.NoNotifySet:
				if (!MuiListtreeMessageCodec.TryReadSet(ref platform, message, method,
					out var setPacket)) return 0;
				return MuiListtreeCore.SetAttribute(ref platform, state, obj,
					setPacket.Attribute, setPacket.Value,
					method == MuiListtreeMessageCodec.Set) ? 1u : 0u;
			case MuiListtreeMessageCodec.Get:
				if (!MuiListtreeMessageCodec.TryReadGet(ref platform, message,
					out var getPacket)) return 0;
				if (!MuiListtreeCore.GetAttribute(ref platform, state, obj,
					getPacket.Attribute, out var value)) return 0;
				var storage = APTR.FromPointer(getPacket.Storage);
				if (storage.IsNotNull)
					MuiGuestUlongStorageCodec.WriteValue(ref platform, storage, value);
				return 1;
		}
		return 0;
	}














}
