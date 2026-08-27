using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeNodeSnapshotAdmissionTests
{
	[Fact]
	public void ListtreeNodeAndDisplaySnapshotRoundTripThroughStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var nodeAddress = APTR.FromPointer(0x1A00);
		var snapshotAddress = APTR.FromPointer(0x1A80);
		var node = new MuiListtreeCore.MuiListtreeNodeState
		{
			Private1 = MuiListtreeCore.MuiListtreeNodeState.Cookie,
			Private2 = APTR.FromPointer(0x1B00),
			Name = APTR.FromPointer(0x1B40),
			Flags = 0x12,
			User = APTR.FromPointer(0x1B80),
			PublicReserved = 0x34,
			Parent = APTR.FromPointer(0x1BC0),
			FirstChild = APTR.FromPointer(0x1C00),
			LastChild = APTR.FromPointer(0x1C40),
			Next = APTR.FromPointer(0x1C80),
			Previous = APTR.FromPointer(0x1CC0),
			ChildCount = 3,
			NameOwned = 1,
			NameSize = 8,
			UserOwned = 1,
			Reserved0 = 1,
			Reserved1 = 2,
		};
		var snapshot = new MuiListtreeCore.MuiListtreeDisplaySnapshotState
		{
			Magic = MuiListtreeCore.MuiListtreeDisplaySnapshotState.Cookie,
			Node = nodeAddress,
			Columns = 2,
			Values = APTR.FromPointer(0x1D00),
			DisplayFlags = MuiListtreeCore.MuiListtreeDisplaySnapshotState.DisplayOpen,
		};
		Assert.True(MuiListtreeCore.MuiListtreeNodeCodec.Write(ref platform,
			nodeAddress, node));
		Assert.True(MuiListtreeCore.MuiListtreeDisplaySnapshotStateCodec.Write(
			ref platform, snapshotAddress, snapshot));
		Assert.True(MuiListtreeCore.MuiListtreeNodeCodec.TryRead(ref platform,
			nodeAddress, out var readNode));
		Assert.Equal(node.Name, readNode.Name);
		Assert.Equal(node.Flags, readNode.Flags);
		Assert.Equal(node.ChildCount, readNode.ChildCount);
		Assert.Equal(node.Reserved1, readNode.Reserved1);
		Assert.True(MuiListtreeCore.MuiListtreeDisplaySnapshotStateCodec.TryRead(
			ref platform, snapshotAddress, out var readSnapshot));
		Assert.Equal(snapshot.Node, readSnapshot.Node);
		Assert.Equal(snapshot.Values, readSnapshot.Values);
		Assert.Equal(snapshot.DisplayFlags, readSnapshot.DisplayFlags);
	}

	[Fact]
	public void MalformedListtreeNodeAndSnapshotCookiesRemainStructural()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var nodeAddress = APTR.FromPointer(0x1B00);
		var snapshotAddress = APTR.FromPointer(0x1B80);
		Assert.True(MuiListtreeCore.MuiListtreeNodeCodec.Write(ref platform,
			nodeAddress, new MuiListtreeCore.MuiListtreeNodeState
			{
				Private1 = MuiListtreeCore.MuiListtreeNodeState.Cookie,
				ChildCount = 4,
			}));
		Assert.True(MuiListtreeCore.MuiListtreeDisplaySnapshotStateCodec.Write(
			ref platform, snapshotAddress, new MuiListtreeCore.MuiListtreeDisplaySnapshotState
			{
				Magic = MuiListtreeCore.MuiListtreeDisplaySnapshotState.Cookie,
				Columns = 2,
			}));
		Assert.True(MuiListtreeCore.MuiListtreeNodeFieldCursorCodec.TryWriteUInt32(
			ref platform, nodeAddress, MuiListtreeCore.MuiListtreeNodeField.Private1,
			0));
		Assert.True(MuiListtreeCore.MuiListtreeDisplaySnapshotFieldCursorCodec
			.TryWriteUInt32(ref platform, snapshotAddress,
				MuiListtreeCore.MuiListtreeDisplaySnapshotField.Magic, 0));
		Assert.True(MuiListtreeCore.MuiListtreeNodeCodec.TryReadStructural(ref platform,
			nodeAddress, out var node));
		Assert.Equal(0u, node.Private1);
		Assert.Equal(4u, node.ChildCount);
		Assert.False(MuiListtreeCore.MuiListtreeNodeCodec.TryRead(ref platform,
			nodeAddress, out _));
		Assert.True(MuiListtreeCore.MuiListtreeDisplaySnapshotStateCodec
			.TryReadStructural(ref platform, snapshotAddress, out var snapshot));
		Assert.Equal(0u, snapshot.Magic);
		Assert.Equal(2u, snapshot.Columns);
		Assert.False(MuiListtreeCore.MuiListtreeDisplaySnapshotStateCodec.TryRead(
			ref platform, snapshotAddress, out _));
	}
}
