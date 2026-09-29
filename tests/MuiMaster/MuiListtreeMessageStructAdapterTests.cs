using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeMessageStructAdapterTests
{
	[Fact]
	public void ListtreeFieldWritesRoundTripThroughNamedPacketsAndPreserveSiblings()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));

		var methodAddress = APTR.FromPointer(0x3400);
		Assert.True(MuiListtreeMethodMessageCodec.TryWrite(ref platform,
			methodAddress, MuiListtreeMessageCodec.Get));
		Assert.True(MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform,
			methodAddress, MuiListtreePacketKind.Method,
			MuiListtreeField.MethodId, MuiListtreeMessageCodec.Set));
		Assert.True(MuiListtreeMethodMessageCodec.TryRead(ref platform,
			methodAddress, out var method));
		Assert.Equal(MuiListtreeMessageCodec.Set, method.MethodId);
		var fieldCursor = new MuiListtreeFieldCursor
		{
			Message = methodAddress,
			Packet = MuiListtreePacketKind.Method,
			Field = MuiListtreeField.MethodId,
		};
		Assert.True(MuiListtreeFieldCursorCodec.TryGetAddress(ref platform,
			fieldCursor, out var methodFieldAddress, out var methodFieldSize));
		Assert.Equal(methodAddress, methodFieldAddress);
		Assert.Equal(4u, methodFieldSize);

		var setAddress = APTR.FromPointer(0x3420);
		var set = new MuiListtreeSetMessage { MethodId = MuiListtreeMessageCodec.Set,
			Attribute = 0x120, Value = 0x456 };
		Assert.True(MuiListtreeSetMessageCodec.TryWrite(ref platform, setAddress, set));
		Assert.True(MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform,
			setAddress, MuiListtreePacketKind.Set, MuiListtreeField.Attribute, 0x121));
		Assert.True(MuiListtreeSetMessageCodec.TryRead(ref platform, setAddress,
			out var setAfter));
		Assert.Equal(set.MethodId, setAfter.MethodId);
		Assert.Equal(0x121u, setAfter.Attribute);
		Assert.Equal(set.Value, setAfter.Value);

		var getAddress = APTR.FromPointer(0x3440);
		var get = new MuiListtreeGetMessage { MethodId = MuiListtreeMessageCodec.Get,
			Attribute = 0x220, Storage = 0x3500 };
		Assert.True(MuiListtreeGetMessageCodec.TryWrite(ref platform, getAddress, get));
		Assert.True(MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform,
			getAddress, MuiListtreePacketKind.Get, MuiListtreeField.Storage, 0x3504));
		Assert.True(MuiListtreeGetMessageCodec.TryRead(ref platform, getAddress,
			out var getAfter));
		Assert.Equal(get.MethodId, getAfter.MethodId);
		Assert.Equal(get.Attribute, getAfter.Attribute);
		Assert.Equal(0x3504u, getAfter.Storage);

		var entryAddress = APTR.FromPointer(0x3460);
		var entry = new MuiListtreeGetEntryMessage { MethodId = MuiListtreeMessageCodec.GetEntry,
			Node = 0x3600, Position = 7, Flags = 3 };
		Assert.True(MuiListtreeGetEntryMessageCodec.TryWrite(ref platform, entryAddress,
			entry));
		Assert.True(MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform,
			entryAddress, MuiListtreePacketKind.GetEntry, MuiListtreeField.Position, 8));
		Assert.True(MuiListtreeGetEntryMessageCodec.TryRead(ref platform, entryAddress,
			out var entryAfter));
		Assert.Equal(entry.Node, entryAfter.Node);
		Assert.Equal(8u, entryAfter.Position);
		Assert.Equal(entry.Flags, entryAfter.Flags);

		var insertAddress = APTR.FromPointer(0x3480);
		var insert = new MuiListtreeInsertMessage { MethodId = MuiListtreeMessageCodec.Insert,
			Name = 0x3700, User = 0x3710, ListNode = 0x3720, PrevNode = 0x3730,
			Flags = 9 };
		Assert.True(MuiListtreeInsertMessageCodec.TryWrite(ref platform, insertAddress,
			insert));
		Assert.True(MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform,
			insertAddress, MuiListtreePacketKind.Insert, MuiListtreeField.Flags, 10));
		Assert.True(MuiListtreeInsertMessageCodec.TryRead(ref platform, insertAddress,
			out var insertAfter));
		Assert.Equal(insert.Name, insertAfter.Name);
		Assert.Equal(insert.User, insertAfter.User);
		Assert.Equal(insert.ListNode, insertAfter.ListNode);
		Assert.Equal(insert.PrevNode, insertAfter.PrevNode);
		Assert.Equal(10u, insertAfter.Flags);

		var removeAddress = APTR.FromPointer(0x34A0);
		var remove = new MuiListtreeRemoveMessage { MethodId = MuiListtreeMessageCodec.Remove,
			ListNode = 0x3740, TreeNode = 0x3750, Flags = 11 };
		Assert.True(MuiListtreeRemoveMessageCodec.TryWrite(ref platform, removeAddress,
			remove));
		Assert.True(MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform,
			removeAddress, MuiListtreePacketKind.Remove, MuiListtreeField.TreeNode, 0x3754));
		Assert.True(MuiListtreeRemoveMessageCodec.TryRead(ref platform, removeAddress,
			out var removeAfter));
		Assert.Equal(remove.ListNode, removeAfter.ListNode);
		Assert.Equal(0x3754u, removeAfter.TreeNode);
		Assert.Equal(remove.Flags, removeAfter.Flags);

		var openAddress = APTR.FromPointer(0x34C0);
		var open = new MuiListtreeOpenCloseMessage { MethodId = MuiListtreeMessageCodec.Open,
			ListNode = 0x3760, TreeNode = 0x3770, Flags = 12 };
		Assert.True(MuiListtreeOpenCloseMessageCodec.TryWrite(ref platform, openAddress,
			open));
		Assert.True(MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform,
			openAddress, MuiListtreePacketKind.OpenClose, MuiListtreeField.Flags, 13));
		Assert.True(MuiListtreeOpenCloseMessageCodec.TryRead(ref platform, openAddress,
			out var openAfter));
		Assert.Equal(open.ListNode, openAfter.ListNode);
		Assert.Equal(open.TreeNode, openAfter.TreeNode);
		Assert.Equal(13u, openAfter.Flags);

		var sortAddress = APTR.FromPointer(0x34E0);
		var sort = new MuiListtreeSortMessage { MethodId = MuiListtreeMessageCodec.Sort,
			ListNode = 0x3780, Flags = 14 };
		Assert.True(MuiListtreeSortMessageCodec.TryWrite(ref platform, sortAddress, sort));
		Assert.True(MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform,
			sortAddress, MuiListtreePacketKind.Sort, MuiListtreeField.ListNode, 0x3784));
		Assert.True(MuiListtreeSortMessageCodec.TryRead(ref platform, sortAddress,
			out var sortAfter));
		Assert.Equal(0x3784u, sortAfter.ListNode);
		Assert.Equal(sort.Flags, sortAfter.Flags);

		var getNrAddress = APTR.FromPointer(0x3500);
		var getNr = new MuiListtreeGetNrMessage { MethodId = MuiListtreeMessageCodec.GetNr,
			TreeNode = 0x3790, Flags = 15 };
		Assert.True(MuiListtreeGetNrMessageCodec.TryWrite(ref platform, getNrAddress,
			getNr));
		Assert.True(MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform,
			getNrAddress, MuiListtreePacketKind.GetNr, MuiListtreeField.Flags, 16));
		Assert.True(MuiListtreeGetNrMessageCodec.TryRead(ref platform, getNrAddress,
			out var getNrAfter));
		Assert.Equal(getNr.TreeNode, getNrAfter.TreeNode);
		Assert.Equal(16u, getNrAfter.Flags);

		var moveAddress = APTR.FromPointer(0x3520);
		var move = new MuiListtreeMoveExchangeMessage { MethodId = MuiListtreeMessageCodec.Move,
			OldListNode = 0x37A0, OldTreeNode = 0x37B0, NewListNode = 0x37C0,
			NewTreeNode = 0x37D0, Flags = 17 };
		Assert.True(MuiListtreeMoveExchangeMessageCodec.TryWrite(ref platform, moveAddress,
			move));
		Assert.True(MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform,
			moveAddress, MuiListtreePacketKind.MoveExchange,
			MuiListtreeField.NewTreeNode, 0x37D4));
		Assert.True(MuiListtreeMoveExchangeMessageCodec.TryRead(ref platform, moveAddress,
			out var moveAfter));
		Assert.Equal(move.OldListNode, moveAfter.OldListNode);
		Assert.Equal(move.OldTreeNode, moveAfter.OldTreeNode);
		Assert.Equal(move.NewListNode, moveAfter.NewListNode);
		Assert.Equal(0x37D4u, moveAfter.NewTreeNode);
		Assert.Equal(move.Flags, moveAfter.Flags);

		var renameAddress = APTR.FromPointer(0x3550);
		var rename = new MuiListtreeRenameMessage { MethodId = MuiListtreeMessageCodec.Rename,
			TreeNode = 0x37E0, NewName = 0x37F0, Flags = 18 };
		Assert.True(MuiListtreeRenameMessageCodec.TryWrite(ref platform, renameAddress,
			rename));
		Assert.True(MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform,
			renameAddress, MuiListtreePacketKind.Rename, MuiListtreeField.NewName, 0x37F4));
		Assert.True(MuiListtreeRenameMessageCodec.TryRead(ref platform, renameAddress,
			out var renameAfter));
		Assert.Equal(rename.TreeNode, renameAfter.TreeNode);
		Assert.Equal(0x37F4u, renameAfter.NewName);
		Assert.Equal(rename.Flags, renameAfter.Flags);

		var findAddress = APTR.FromPointer(0x3570);
		var find = new MuiListtreeFindNameMessage { MethodId = MuiListtreeMessageCodec.FindName,
			ListNode = 0x3800, Name = 0x3810, Flags = 19 };
		Assert.True(MuiListtreeFindNameMessageCodec.TryWrite(ref platform, findAddress,
			find));
		Assert.True(MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform,
			findAddress, MuiListtreePacketKind.FindName, MuiListtreeField.Name, 0x3814));
		Assert.True(MuiListtreeFindNameMessageCodec.TryRead(ref platform, findAddress,
			out var findAfter));
		Assert.Equal(find.ListNode, findAfter.ListNode);
		Assert.Equal(0x3814u, findAfter.Name);
		Assert.Equal(find.Flags, findAfter.Flags);

		var dropAddress = APTR.FromPointer(0x3590);
		var drop = new MuiListtreeDropMarkMessage { MethodId = MuiListtreeMessageCodec.SetDropMark,
			Entry = 0x3820, Values = 20 };
		Assert.True(MuiListtreeDropMarkMessageCodec.TryWrite(ref platform, dropAddress,
			drop));
		Assert.True(MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform,
			dropAddress, MuiListtreePacketKind.DropMark, MuiListtreeField.Values, 21));
		Assert.True(MuiListtreeDropMarkMessageCodec.TryRead(ref platform, dropAddress,
			out var dropAfter));
		Assert.Equal(drop.Entry, dropAfter.Entry);
		Assert.Equal(21u, dropAfter.Values);

		var testPosAddress = APTR.FromPointer(0x35B0);
		var testPos = new MuiListtreeTestPosMessage { MethodId = MuiListtreeMessageCodec.TestPos,
			X = 22, Y = 23, Result = 24 };
		Assert.True(MuiListtreeTestPosMessageCodec.TryWrite(ref platform, testPosAddress,
			testPos));
		Assert.True(MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform,
			testPosAddress, MuiListtreePacketKind.TestPos, MuiListtreeField.Result, 25));
		Assert.True(MuiListtreeTestPosMessageCodec.TryRead(ref platform, testPosAddress,
			out var testPosAfter));
		Assert.Equal(testPos.X, testPosAfter.X);
		Assert.Equal(testPos.Y, testPosAfter.Y);
		Assert.Equal(25u, testPosAfter.Result);
	}

	[Fact]
	public void ListtreeStructuralAdapterRejectsInvalidFieldsAndIncompletePackets()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3600);
		Assert.False(MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform,
			address, MuiListtreePacketKind.Set, (MuiListtreeField)255, out _));
		Assert.False(MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiListtreePacketKind.Get, (MuiListtreeField)255, 1));
		Assert.False(MuiListtreeMessageMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x20FF4), MuiListtreePacketKind.GetEntry,
			MuiListtreeField.Flags, out _));
		Assert.False(MuiListtreeMessageMemoryCodec.TryWriteUInt32(ref platform,
			APTR.Null, MuiListtreePacketKind.TestPos, MuiListtreeField.Result, 1));
	}
}
