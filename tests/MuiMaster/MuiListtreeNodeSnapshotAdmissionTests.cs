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

	[Fact]
	public void ListtreeDisplaySnapshotMemoryAdapterUsesNamedStructFields()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1C00);
		Assert.True(MuiListtreeCore.MuiListtreeDisplaySnapshotMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeDisplaySnapshotField.Columns, 4));
		Assert.True(MuiListtreeCore.MuiListtreeDisplaySnapshotMemoryCodec
			.TryReadUInt32(ref platform, address,
				MuiListtreeCore.MuiListtreeDisplaySnapshotField.Columns,
				out var columns));
		Assert.Equal(4u, columns);
		Assert.True(MuiListtreeCore.MuiListtreeDisplaySnapshotMemoryCodec
			.TryGetAddress(ref platform, address,
				MuiListtreeCore.MuiListtreeDisplaySnapshotField.DisplayFlags,
				out var flagsAddress));
		Assert.Equal(0x1C10u, flagsAddress.Raw);
		Assert.False(MuiListtreeCore.MuiListtreeDisplaySnapshotMemoryCodec
			.TryGetAddress(ref platform, address,
				(MuiListtreeCore.MuiListtreeDisplaySnapshotField)255, out _));
	}

	[Fact]
	public void ListtreeNodeMemoryAdapterUsesNamedStructFields()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x1D80);
		Assert.True(MuiListtreeCore.MuiListtreeNodeMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeNodeField.ChildCount,
			3));
		Assert.True(MuiListtreeCore.MuiListtreeNodeMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeNodeField.ChildCount,
			out var childCount));
		Assert.Equal(3u, childCount);
		Assert.True(MuiListtreeCore.MuiListtreeNodeMemoryCodec.TryWriteUInt16(
			ref platform, address, MuiListtreeCore.MuiListtreeNodeField.Flags,
			0x12));
		Assert.True(MuiListtreeCore.MuiListtreeNodeMemoryCodec.TryReadUInt16(
			ref platform, address, MuiListtreeCore.MuiListtreeNodeField.Flags,
			out var flags));
		Assert.Equal((ushort)0x12, flags);
		Assert.True(MuiListtreeCore.MuiListtreeNodeMemoryCodec.TryGetAddress(
			ref platform, address, MuiListtreeCore.MuiListtreeNodeField.Reserved1,
			out var reservedAddress, out var fieldSize));
		Assert.Equal(0x1DBCu, reservedAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.False(MuiListtreeCore.MuiListtreeNodeMemoryCodec.TryGetAddress(
			ref platform, address,
			(MuiListtreeCore.MuiListtreeNodeField)255, out _, out _));
	}
}
