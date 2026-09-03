using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiCollectionBasicMessageStructAdapterTests
{
	[Fact]
	public void CollectionBasicRecordsUseNamedFieldsAndCompleteBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var getEntryAddress = APTR.FromPointer(0x3000);
		Assert.True(MuiCollectionBasicMessageCodec.WriteGetEntry(ref platform,
			getEntryAddress, 7, 0x3500));
		Assert.True(MuiCollectionBasicMessageCodec.TryReadGetEntry(ref platform,
			getEntryAddress, out var getEntry));
		Assert.Equal(7u, getEntry.Position);
		Assert.Equal(0x3500u, getEntry.Storage);
		Assert.True(MuiCollectionBasicMessageMemoryCodec.TryGetAddress(ref platform,
			getEntryAddress, MuiCollectionBasicPacketKind.GetEntry,
			MuiCollectionBasicField.Storage, out var storageAddress));
		Assert.Equal(APTR.FromPointer(0x3008), storageAddress);
		Assert.True(MuiCollectionBasicMessageMemoryCodec.TryWriteUInt32(ref platform,
			getEntryAddress, MuiCollectionBasicPacketKind.GetEntry,
			MuiCollectionBasicField.Position, 9));
		Assert.True(MuiCollectionBasicMessageCodec.TryReadGetEntry(ref platform,
			getEntryAddress, out getEntry));
		Assert.Equal(MuiCollectionBasicMessageCodec.GetEntry, getEntry.MethodId);
		Assert.Equal(9u, getEntry.Position);
		Assert.Equal(0x3500u, getEntry.Storage);

		var selectAddress = APTR.FromPointer(0x3040);
		Assert.True(MuiCollectionBasicMessageCodec.WriteSelect(ref platform,
			selectAddress, 3, 1, 0x3600));
		Assert.True(MuiCollectionBasicMessageCodec.TryReadSelect(ref platform,
			selectAddress, out var select));
		Assert.Equal(3u, select.Position);
		Assert.Equal(1u, select.Select);
		Assert.Equal(0x3600u, select.Storage);
		Assert.True(MuiCollectionBasicMessageMemoryCodec.TryReadUInt32(ref platform,
			selectAddress, MuiCollectionBasicPacketKind.Select,
			MuiCollectionBasicField.Select, out var selected));
		Assert.Equal(1u, selected);
		Assert.True(MuiCollectionBasicMessageMemoryCodec.TryWriteUInt32(ref platform,
			selectAddress, MuiCollectionBasicPacketKind.Select,
			MuiCollectionBasicField.Storage, 0x3700));
		Assert.True(MuiCollectionBasicMessageCodec.TryReadSelect(ref platform,
			selectAddress, out select));
		Assert.Equal(MuiCollectionBasicMessageCodec.Select, select.MethodId);
		Assert.Equal(3u, select.Position);
		Assert.Equal(1u, select.Select);
		Assert.Equal(0x3700u, select.Storage);
		Assert.False(MuiCollectionBasicMessageMemoryCodec.TryReadUInt32(ref platform,
			selectAddress, MuiCollectionBasicPacketKind.GetEntry,
			MuiCollectionBasicField.Select, out _));
		Assert.False(MuiCollectionBasicMessageMemoryCodec.TryWriteUInt32(ref platform,
			selectAddress, MuiCollectionBasicPacketKind.Select,
			(MuiCollectionBasicField)255, 1));

		var methodAddress = APTR.FromPointer(0x3080);
		Assert.True(MuiCollectionBasicMessageCodec.WriteMethod(ref platform,
			methodAddress, MuiCollectionBasicMessageCodec.Clear));
		Assert.True(MuiCollectionBasicMessageCodec.TryReadMethod(ref platform,
			methodAddress, MuiCollectionBasicMessageCodec.Clear, out var method));
		Assert.Equal(MuiCollectionBasicMessageCodec.Clear, method.MethodId);
		Assert.True(MuiCollectionBasicMessageMemoryCodec.TryWriteUInt32(ref platform,
			methodAddress, MuiCollectionBasicPacketKind.Method,
			MuiCollectionBasicField.MethodId, MuiCollectionBasicMessageCodec.Sort));
		Assert.True(MuiCollectionBasicStructPacketCodec.TryReadMethodRecord(ref platform,
			methodAddress, out method));
		Assert.Equal(MuiCollectionBasicMessageCodec.Sort, method.MethodId);
		Assert.False(MuiCollectionBasicMessageMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x20FFF), MuiCollectionBasicPacketKind.Select,
			MuiCollectionBasicField.Storage, out _));
		Assert.True(MuiCollectionBasicStructPacketCodec.TryWriteMethodValue(
			ref platform, methodAddress, 0xF1234567u));
		Assert.True(MuiCollectionBasicStructPacketCodec.TryReadMethodValue(
			ref platform, methodAddress, out var scalarMethodId));
		Assert.Equal(0xF1234567u, scalarMethodId);
		Assert.False(MuiCollectionBasicStructPacketCodec.TryWriteMethodValue(
			ref platform, APTR.Null, 1));
		Assert.False(MuiCollectionBasicStructPacketCodec.TryReadMethodValue(
			ref platform, APTR.FromPointer(0x20FFFu), out _));

		Assert.False(MuiCollectionBasicMessageMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x20FF8), MuiCollectionBasicPacketKind.Select,
			MuiCollectionBasicField.Storage, out _));
		Assert.False(MuiCollectionBasicMessageMemoryCodec.TryGetAddress(ref platform,
			methodAddress, MuiCollectionBasicPacketKind.Method,
			MuiCollectionBasicField.Storage, out _));
		Assert.False(MuiCollectionBasicMessageMemoryCodec.TryGetAddress(ref platform,
			selectAddress, MuiCollectionBasicPacketKind.Select,
			(MuiCollectionBasicField)255, out _));
		Assert.False(MuiCollectionBasicMessageMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiCollectionBasicPacketKind.GetEntry,
			MuiCollectionBasicField.Position, out _));
	}
}
