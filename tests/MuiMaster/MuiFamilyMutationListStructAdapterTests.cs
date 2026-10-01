using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiFamilyMutationListStructAdapterTests
{
	[Fact]
	public void FamilyMutationListFieldsRoundTripThroughNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiFamilyMutationListRecord
		{
			Head = APTR.FromPointer(0x36200),
			Tail = APTR.FromPointer(0x36300),
		};
		Assert.True(MuiFamilyMutationListStructCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiFamilyMutationListMemoryCodec.TryWrite(ref platform, address,
			MuiFamilyMutationListField.Head, 0x36400));
		Assert.True(MuiFamilyMutationListMemoryCodec.TryRead(ref platform, address,
			MuiFamilyMutationListField.Tail, out var tail));
		Assert.Equal(0x36300u, tail);
		Assert.True(MuiFamilyMutationListStructCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(0x36400u, decoded.Head.Raw);
		Assert.Equal(value.Tail.Raw, decoded.Tail.Raw);
		Assert.True(MuiFamilyMutationListMemoryCodec.TryGetAddress(ref platform,
			address, MuiFamilyMutationListField.Tail, out var tailAddress));
		Assert.Equal(0x3504u, tailAddress.Raw);
		var typedCursor = new MuiFamilyMutationListFieldCursor
		{
			List = address,
			Field = MuiFamilyMutationListField.Tail,
		};
		Assert.True(MuiFamilyMutationListFieldCursorCodec.TryGetAddress(ref platform,
			typedCursor, out var typedTailAddress, out var typedTailSize));
		Assert.Equal(tailAddress, typedTailAddress);
		Assert.Equal(4u, typedTailSize);
	}

	[Fact]
	public void FamilyMutationListStructAdapterRejectsIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		Assert.False(MuiFamilyMutationListMemoryCodec.TryRead(ref platform,
			APTR.FromPointer(0x30FFC), MuiFamilyMutationListField.Head, out _));
		Assert.False(MuiFamilyMutationListMemoryCodec.TryWrite(ref platform,
			APTR.Null, MuiFamilyMutationListField.Tail, 1));
		Assert.False(MuiFamilyMutationListMemoryCodec.TryRead(ref platform,
			APTR.FromPointer(0x3500), (MuiFamilyMutationListField)0xFF, out _));
		var typedCursor = new MuiFamilyMutationListFieldCursor
		{
			List = APTR.FromPointer(0x3500),
			Field = (MuiFamilyMutationListField)0xFF,
		};
		Assert.False(MuiFamilyMutationListFieldCursorCodec.TryGetAddress(ref platform,
			typedCursor, out _, out _));
		typedCursor.List = APTR.Null;
		typedCursor.Field = MuiFamilyMutationListField.Head;
		Assert.False(MuiFamilyMutationListFieldCursorCodec.TryGetAddress(ref platform,
			typedCursor, out _, out _));
	}
}
