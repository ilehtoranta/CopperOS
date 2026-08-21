/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

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
	public uint UserData;
	public uint Parent;
	public uint Previous;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeRemoveMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint Parent;
	public uint Node;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeGetEntryMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint Parent;
	public uint Position;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeOpenCloseMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint Parent;
	public uint Node;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeSortMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Parent;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeMoveExchangeMessage
{
	public const uint Size = 24;
	public uint MethodId;
	public uint Parent;
	public uint Node;
	public uint NewParent;
	public uint Previous;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeRenameMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint Node;
	public uint Name;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeFindNameMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint Parent;
	public uint Name;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeDropMarkMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Position;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreeTestPosMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint X;
	public uint Y;
	public uint Entry;
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
	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiListtreeMessageCodec.TryReadMethodId(ref platform, message,
			out var methodHeader) ||
			!MuiListtreeCore.IsListtree(ref platform, state, obj)) return 0;
		var method = methodHeader.MethodId;
		switch (method)
		{
			case MuiListtreeMessageCodec.Insert:
				if (!MuiListtreeMessageCodec.TryReadInsert(ref platform, message,
					out var insertPacket))
					return 0;
				return MuiListtreeCore.Insert(ref platform, state, obj,
					APTR.FromPointer(insertPacket.Name),
					APTR.FromPointer(insertPacket.UserData),
					APTR.FromPointer(insertPacket.Parent),
					APTR.FromPointer(insertPacket.Previous),
					insertPacket.Flags).Raw;
			case MuiListtreeMessageCodec.Remove:
				if (!MuiListtreeMessageCodec.TryReadRemove(ref platform, message,
					out var removePacket))
					return 0;
				return MuiListtreeCore.Remove(ref platform, state, obj,
					APTR.FromPointer(removePacket.Parent),
					APTR.FromPointer(removePacket.Node),
					removePacket.Flags) ? 1u : 0u;
			case MuiListtreeMessageCodec.GetEntry:
				if (!MuiListtreeMessageCodec.TryReadGetEntry(ref platform, message,
					out var getEntryPacket))
					return 0;
				return MuiListtreeCore.GetEntry(ref platform, state, obj,
					APTR.FromPointer(getEntryPacket.Parent),
					unchecked((int)getEntryPacket.Position),
					getEntryPacket.Flags).Raw;
			case MuiListtreeMessageCodec.GetNr:
				if (!MuiListtreeMessageCodec.TryReadSort(ref platform, message,
					MuiListtreeMessageCodec.GetNr, out var getNrPacket)) return 0;
				return MuiListtreeCore.GetNr(ref platform, state, obj,
					APTR.FromPointer(getNrPacket.Parent), getNrPacket.Flags);
			case MuiListtreeMessageCodec.Open:
				if (!MuiListtreeMessageCodec.TryReadOpenClose(ref platform, message,
					MuiListtreeMessageCodec.Open, out var openPacket)) return 0;
				return MuiListtreeCore.Open(ref platform, state, obj,
					APTR.FromPointer(openPacket.Parent),
					APTR.FromPointer(openPacket.Node), openPacket.Flags) ? 1u : 0u;
			case MuiListtreeMessageCodec.Close:
				if (!MuiListtreeMessageCodec.TryReadOpenClose(ref platform, message,
					MuiListtreeMessageCodec.Close, out var closePacket)) return 0;
				return MuiListtreeCore.Close(ref platform, state, obj,
					APTR.FromPointer(closePacket.Parent),
					APTR.FromPointer(closePacket.Node), closePacket.Flags) ? 1u : 0u;
			case MuiListtreeMessageCodec.Sort:
				if (!MuiListtreeMessageCodec.TryReadSort(ref platform, message,
					MuiListtreeMessageCodec.Sort, out var sortPacket)) return 0;
				return MuiListtreeCore.Sort(ref platform, state, obj,
					APTR.FromPointer(sortPacket.Parent), sortPacket.Flags) ? 1u : 0u;
			case MuiListtreeMessageCodec.Move:
				if (!MuiListtreeMessageCodec.TryReadMoveExchange(ref platform, message,
					MuiListtreeMessageCodec.Move, out var movePacket)) return 0;
				return MuiListtreeCore.Move(ref platform, state, obj,
					APTR.FromPointer(movePacket.Parent),
					APTR.FromPointer(movePacket.Node),
					APTR.FromPointer(movePacket.NewParent),
					APTR.FromPointer(movePacket.Previous), movePacket.Flags) ? 1u : 0u;
			case MuiListtreeMessageCodec.Exchange:
				if (!MuiListtreeMessageCodec.TryReadMoveExchange(ref platform, message,
					MuiListtreeMessageCodec.Exchange, out var exchangePacket)) return 0;
				return MuiListtreeCore.Exchange(ref platform, state, obj,
					APTR.FromPointer(exchangePacket.Parent),
					APTR.FromPointer(exchangePacket.Node),
					APTR.FromPointer(exchangePacket.NewParent),
					APTR.FromPointer(exchangePacket.Previous), exchangePacket.Flags) ? 1u : 0u;
			case MuiListtreeMessageCodec.Rename:
				if (!MuiListtreeMessageCodec.TryReadRename(ref platform, message,
					out var renamePacket))
					return 0;
				return MuiListtreeCore.Rename(ref platform, state, obj,
					APTR.FromPointer(renamePacket.Node),
					APTR.FromPointer(renamePacket.Name), renamePacket.Flags) ? 1u : 0u;
			case MuiListtreeMessageCodec.FindName:
				if (!MuiListtreeMessageCodec.TryReadFindName(ref platform, message,
					out var findNamePacket))
					return 0;
				return MuiListtreeCore.FindName(ref platform, state, obj,
					APTR.FromPointer(findNamePacket.Parent),
					APTR.FromPointer(findNamePacket.Name), findNamePacket.Flags).Raw;
			case MuiListtreeMessageCodec.SetDropMark:
				if (!MuiListtreeMessageCodec.TryReadDropMark(ref platform, message,
					out var dropMarkPacket))
					return 0;
				return MuiListtreeCore.SetDropMark(ref platform, state, obj,
					unchecked((int)dropMarkPacket.Position), dropMarkPacket.Flags)
					? 1u : 0u;
			case MuiListtreeMessageCodec.TestPos:
				if (!MuiListtreeMessageCodec.TryReadTestPos(ref platform, message,
					out var testPosPacket))
					return 0;
				return MuiListtreeCore.TestPos(ref platform, state, obj,
					unchecked((int)testPosPacket.X), unchecked((int)testPosPacket.Y),
					APTR.FromPointer(testPosPacket.Entry)) ? 1u : 0u;
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
