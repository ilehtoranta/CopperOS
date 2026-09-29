using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListtreeNodeStructAdapterTests
{
	[Fact]
	public void NodeFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListtreeCore.MuiListtreeNodeState
		{
			Private1 = MuiListtreeCore.MuiListtreeNodeState.Cookie,
			Private2 = APTR.FromPointer(0x36200),
			Name = APTR.FromPointer(0x36240),
			Flags = 0x12,
			User = APTR.FromPointer(0x36280),
			PublicReserved = 0x34,
			Parent = APTR.FromPointer(0x36300),
			FirstChild = APTR.FromPointer(0x36340),
			LastChild = APTR.FromPointer(0x36380),
			Next = APTR.FromPointer(0x363C0),
			Previous = APTR.FromPointer(0x36400),
			ChildCount = 3,
			NameOwned = 1,
			NameSize = 16,
			UserOwned = 0,
			Reserved0 = 0x55,
			Reserved1 = 0x66,
		};

		Assert.True(MuiListtreeCore.MuiListtreeNodeCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiListtreeCore.MuiListtreeNodeMemoryCodec.TryWriteUInt16(
			ref platform, address, MuiListtreeCore.MuiListtreeNodeField.Flags, 0x22));
		Assert.True(MuiListtreeCore.MuiListtreeNodeMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeNodeField.NameOwned, 2));
		Assert.True(MuiListtreeCore.MuiListtreeNodeMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeNodeField.Next,
			0x36500));
		Assert.True(MuiListtreeCore.MuiListtreeNodeMemoryCodec.TryReadUInt16(
			ref platform, address, MuiListtreeCore.MuiListtreeNodeField.Flags,
			out var flags));
		Assert.Equal((ushort)0x22, flags);
		Assert.True(MuiListtreeCore.MuiListtreeNodeMemoryCodec.TryReadUInt32(
			ref platform, address, MuiListtreeCore.MuiListtreeNodeField.Next,
			out var next));
		Assert.Equal(0x36500u, next);
		Assert.True(MuiListtreeCore.MuiListtreeNodeCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Private1, decoded.Private1);
		Assert.Equal(value.Private2.Raw, decoded.Private2.Raw);
		Assert.Equal(value.Name.Raw, decoded.Name.Raw);
		Assert.Equal((ushort)0x22, decoded.Flags);
		Assert.Equal(value.User.Raw, decoded.User.Raw);
		Assert.Equal(value.PublicReserved, decoded.PublicReserved);
		Assert.Equal(value.Parent.Raw, decoded.Parent.Raw);
		Assert.Equal(value.FirstChild.Raw, decoded.FirstChild.Raw);
		Assert.Equal(value.LastChild.Raw, decoded.LastChild.Raw);
		Assert.Equal(0x36500u, decoded.Next.Raw);
		Assert.Equal(value.Previous.Raw, decoded.Previous.Raw);
		Assert.Equal(value.ChildCount, decoded.ChildCount);
		Assert.Equal(2u, decoded.NameOwned);
		Assert.Equal(value.NameSize, decoded.NameSize);
		Assert.Equal(value.UserOwned, decoded.UserOwned);
		Assert.Equal(value.Reserved0, decoded.Reserved0);
		Assert.Equal(value.Reserved1, decoded.Reserved1);
		var fieldCursor = new MuiListtreeCore.MuiListtreeNodeFieldCursor
		{
			Address = address,
			Field = MuiListtreeCore.MuiListtreeNodeField.Flags,
		};
		Assert.True(MuiListtreeCore.MuiListtreeNodeFieldCursorCodec.TryGetAddress(
			ref platform, fieldCursor, out var flagsAddress, out var flagsSize));
		Assert.Equal(0x350Cu, flagsAddress.Raw);
		Assert.Equal(2u, flagsSize);
		Assert.True(MuiListtreeCore.MuiListtreeNodeMemoryCodec.TryGetAddress(
			ref platform, address, MuiListtreeCore.MuiListtreeNodeField.NameOwned,
			out var nameOwnedAddress, out var nameOwnedSize));
		Assert.Equal(0x352Cu, nameOwnedAddress.Raw);
		Assert.Equal(4u, nameOwnedSize);
	}

	[Fact]
	public void NodeAdapterRejectsInvalidWidthsOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListtreeCore.MuiListtreeNodeMemoryCodec.TryReadUInt16(
			ref platform, address, MuiListtreeCore.MuiListtreeNodeField.ChildCount,
			out _));
		Assert.False(MuiListtreeCore.MuiListtreeNodeMemoryCodec.TryWriteUInt16(
			ref platform, address, MuiListtreeCore.MuiListtreeNodeField.Parent, 1));
		Assert.False(MuiListtreeCore.MuiListtreeNodeMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeNodeField)0xFF,
			out _));
		Assert.False(MuiListtreeCore.MuiListtreeNodeMemoryCodec.TryWriteUInt32(
			ref platform, address, (MuiListtreeCore.MuiListtreeNodeField)0xFF,
			1));
		Assert.False(MuiListtreeCore.MuiListtreeNodeMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x30FF0),
			MuiListtreeCore.MuiListtreeNodeField.ChildCount, out _));
		Assert.False(MuiListtreeCore.MuiListtreeNodeMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null, MuiListtreeCore.MuiListtreeNodeField.Private1,
			1));
	}
}
