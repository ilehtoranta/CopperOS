using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiFamilyMutationStructAdapterTests
{
	[Fact]
	public void FamilyMutationFieldsRoundTripThroughNamedRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);

		Assert.True(MuiFamilyMutationMessageStructCodec.WriteRecord(ref platform,
			address, new MuiFamilyChildMessage
			{
				MethodId = MuiFamilyMutationCore.AddTailMethod,
				Object = APTR.FromPointer(0x36200),
			}));
		Assert.True(MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiFamilyPacketKind.Child, MuiFamilyPacketField.Object, 0x36300));
		Assert.True(MuiFamilyPacketMemoryCodec.TryReadUInt32(ref platform, address,
			MuiFamilyPacketKind.Child, MuiFamilyPacketField.Object, out var childObject));
		Assert.Equal(0x36300u, childObject);

		Assert.True(MuiFamilyMutationMessageStructCodec.WriteRecord(ref platform,
			address, new MuiFamilyInsertMessage
			{
				MethodId = MuiFamilyMutationCore.InsertMethod,
				Object = APTR.FromPointer(0x36400),
				Predecessor = APTR.FromPointer(0x36500),
			}));
		Assert.True(MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiFamilyPacketKind.Insert, MuiFamilyPacketField.Predecessor, 0x36600));
		Assert.True(MuiFamilyMutationMessageStructCodec.TryReadInsert(ref platform,
			address, out var insert));
		Assert.Equal(0x36400u, insert.Object.Raw);
		Assert.Equal(0x36600u, insert.Predecessor.Raw);

		Assert.True(MuiFamilyMutationMessageStructCodec.WriteRecord(ref platform,
			address, new MuiFamilyTransferMessage
			{
				MethodId = MuiFamilyMutationCore.TransferMethod,
				Family = APTR.FromPointer(0x36700),
			}));
		Assert.True(MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiFamilyPacketKind.Transfer, MuiFamilyPacketField.Family, 0x36800));
		Assert.True(MuiFamilyMutationMessageStructCodec.TryReadTransfer(ref platform,
			address, out var transfer));
		Assert.Equal(MuiFamilyMutationCore.TransferMethod, transfer.MethodId);
		Assert.Equal(0x36800u, transfer.Family.Raw);

		Assert.True(MuiFamilyMutationMessageStructCodec.WriteRecord(ref platform,
			address, new MuiFamilyReorderMessage
			{
				MethodId = MuiFamilyMutationCore.ReorderMethod,
				After = APTR.FromPointer(0x36900),
			}));
		platform.WriteUInt32(address, 8, 0xA5A5A5A5u);
		Assert.True(MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiFamilyPacketKind.Reorder, MuiFamilyPacketField.After, 0x36A00));
		Assert.True(MuiFamilyMutationMessageStructCodec.TryReadReorder(ref platform,
			address, out var reorder));
		Assert.Equal(0x36A00u, reorder.After.Raw);
		Assert.Equal(0xA5A5A5A5u, platform.ReadUInt32(address, 8));

		Assert.True(MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiFamilyPacketKind.Method, MuiFamilyPacketField.MethodId, 0xF1234567u));
		Assert.True(MuiFamilyPacketMemoryCodec.TryReadUInt32(ref platform, address,
			MuiFamilyPacketKind.Method, MuiFamilyPacketField.MethodId, out var method));
		Assert.Equal(0xF1234567u, method);
		Assert.True(MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiFamilyPacketKind.Sort, MuiFamilyPacketField.MethodId, 0xF7654321u));
		Assert.True(MuiFamilyPacketMemoryCodec.TryReadUInt32(ref platform, address,
			MuiFamilyPacketKind.Sort, MuiFamilyPacketField.MethodId, out var sort));
		Assert.Equal(0xF7654321u, sort);
	}

	[Fact]
	public void FamilyMutationStructAdapterRejectsUnsupportedOrIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiFamilyPacketMemoryCodec.TryReadUInt32(ref platform, address,
			MuiFamilyPacketKind.Child, MuiFamilyPacketField.Family, out _));
		Assert.False(MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiFamilyPacketKind.Insert, MuiFamilyPacketField.Family, 1));
		Assert.False(MuiFamilyPacketMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x30FFC), MuiFamilyPacketKind.Insert,
			MuiFamilyPacketField.Predecessor, out _));
		Assert.False(MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform,
			APTR.Null, MuiFamilyPacketKind.Transfer, MuiFamilyPacketField.Family, 1));
		Assert.False(MuiFamilyPacketMemoryCodec.TryReadUInt32(ref platform, address,
			(MuiFamilyPacketKind)0xFF, MuiFamilyPacketField.MethodId, out _));
	}
}
