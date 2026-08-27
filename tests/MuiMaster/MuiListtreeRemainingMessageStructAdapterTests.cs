using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeRemainingMessageStructAdapterTests
{
	[Fact]
	public void ListtreeMutationAndQueryPacketsUseNamedFields()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));

		var insertAddress = APTR.FromPointer(0x3100);
		Assert.True(MuiListtreeMessageCodec.WriteInsert(ref platform,
			insertAddress, 0x1100, 0x1200, 0x1300, 0x1400, 0x55));
		Assert.True(MuiListtreeMessageCodec.TryReadInsert(ref platform,
			insertAddress, out var insert));
		Assert.Equal(0x1100u, insert.Name);
		Assert.Equal(0x55u, insert.Flags);

		var removeAddress = APTR.FromPointer(0x3130);
		Assert.True(MuiListtreeMessageCodec.WriteRemove(ref platform,
			removeAddress, 0x1500, 0x1600, 0x66));
		Assert.True(MuiListtreeMessageCodec.TryReadRemove(ref platform,
			removeAddress, out var remove));
		Assert.Equal(0x1600u, remove.TreeNode);

		var openAddress = APTR.FromPointer(0x3150);
		Assert.True(MuiListtreeMessageCodec.WriteOpenClose(ref platform,
			openAddress, MuiListtreeMessageCodec.Open, 0x1700, 0x1800, 1));
		Assert.True(MuiListtreeMessageCodec.TryReadOpenClose(ref platform,
			openAddress, MuiListtreeMessageCodec.Open, out var open));
		Assert.Equal(0x1700u, open.ListNode);
		Assert.Equal(1u, open.Flags);

		var sortAddress = APTR.FromPointer(0x3170);
		Assert.True(MuiListtreeMessageCodec.WriteSort(ref platform,
			sortAddress, MuiListtreeMessageCodec.Sort, 0x1900, 2));
		Assert.True(MuiListtreeMessageCodec.TryReadSort(ref platform,
			sortAddress, MuiListtreeMessageCodec.Sort, out var sort));
		Assert.Equal(0x1900u, sort.ListNode);

		var getNrAddress = APTR.FromPointer(0x3190);
		Assert.True(MuiListtreeMessageCodec.WriteGetNr(ref platform,
			getNrAddress, 0x1A00, 3));
		Assert.True(MuiListtreeMessageCodec.TryReadGetNr(ref platform,
			getNrAddress, MuiListtreeMessageCodec.GetNr, out var getNr));
		Assert.Equal(0x1A00u, getNr.TreeNode);

		var moveAddress = APTR.FromPointer(0x31B0);
		Assert.True(MuiListtreeMessageCodec.WriteMoveExchange(ref platform,
			moveAddress, MuiListtreeMessageCodec.Move, 0x1B00, 0x1C00,
			0x1D00, 0x1E00, 4));
		Assert.True(MuiListtreeMessageCodec.TryReadMoveExchange(ref platform,
			moveAddress, MuiListtreeMessageCodec.Move, out var move));
		Assert.Equal(0x1E00u, move.NewTreeNode);
		Assert.Equal(4u, move.Flags);

		var renameAddress = APTR.FromPointer(0x31E0);
		Assert.True(MuiListtreeMessageCodec.WriteRename(ref platform,
			renameAddress, 0x1F00, 0x2000, 5));
		Assert.True(MuiListtreeMessageCodec.TryReadRename(ref platform,
			renameAddress, out var rename));
		Assert.Equal(0x2000u, rename.NewName);

		var findAddress = APTR.FromPointer(0x3200);
		Assert.True(MuiListtreeMessageCodec.WriteFindName(ref platform,
			findAddress, 0x2100, 0x2200, 6));
		Assert.True(MuiListtreeMessageCodec.TryReadFindName(ref platform,
			findAddress, out var find));
		Assert.Equal(0x2100u, find.ListNode);

		var dropAddress = APTR.FromPointer(0x3220);
		Assert.True(MuiListtreeMessageCodec.WriteDropMark(ref platform,
			dropAddress, 0x2300, 7));
		Assert.True(MuiListtreeMessageCodec.TryReadDropMark(ref platform,
			dropAddress, out var drop));
		Assert.Equal(0x2300u, drop.Entry);
		Assert.Equal(7u, drop.Values);

		var testPosAddress = APTR.FromPointer(0x3240);
		Assert.True(MuiListtreeMessageCodec.WriteTestPos(ref platform,
			testPosAddress, 8, 9, 10));
		Assert.True(MuiListtreeMessageCodec.TryReadTestPos(ref platform,
			testPosAddress, out var testPos));
		Assert.Equal(8u, testPos.X);
		Assert.Equal(10u, testPos.Result);

		Assert.True(MuiListtreeMessageMemoryCodec.TryGetAddress(ref platform,
			insertAddress, MuiListtreePacketKind.Insert,
			MuiListtreeField.PrevNode, out var prevNode));
		Assert.Equal(APTR.FromPointer(0x3110), prevNode);
		Assert.False(MuiListtreeMessageMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x20FF0), MuiListtreePacketKind.MoveExchange,
			MuiListtreeField.Flags, out _));
		Assert.False(MuiListtreeMessageCodec.TryReadOpenClose(ref platform,
			openAddress, MuiListtreeMessageCodec.Close, out _));
	}
}
