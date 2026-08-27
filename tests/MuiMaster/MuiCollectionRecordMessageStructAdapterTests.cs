using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiCollectionRecordMessageStructAdapterTests
{
	[Fact]
	public void CollectionRecordPacketsUseNamedFieldsAndCompleteBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var entryPoolAddress = APTR.FromPointer(0x3000);
		Assert.True(MuiCollectionRecordMessageCodec.WriteEntryPool(ref platform,
			entryPoolAddress, MuiCollectionRecordMessageCodec.Construct,
			0x3500, 0x3600));
		Assert.True(MuiCollectionRecordMessageCodec.TryReadEntryPool(ref platform,
			entryPoolAddress, MuiCollectionRecordMessageCodec.Construct,
			out var entryPool));
		Assert.Equal(0x3500u, entryPool.Entry);
		Assert.Equal(0x3600u, entryPool.Pool);

		var displayAddress = APTR.FromPointer(0x3040);
		Assert.True(MuiCollectionRecordMessageCodec.WriteDisplay(ref platform,
			displayAddress, 0x3500, 0x3700, 3));
		Assert.True(MuiCollectionRecordMessageCodec.TryReadDisplay(ref platform,
			displayAddress, out var display));
		Assert.Equal(0x3700u, display.Array);
		Assert.True(MuiCollectionRecordMessageMemoryCodec.TryGetAddress(
			ref platform, displayAddress, MuiCollectionRecordPacketKind.Display,
			MuiCollectionRecordField.Row, out var rowAddress));
		Assert.Equal(APTR.FromPointer(0x304C), rowAddress);

		var compareAddress = APTR.FromPointer(0x3080);
		Assert.True(MuiCollectionRecordMessageCodec.WriteCompare(ref platform,
			compareAddress, 0x3500, 0x3510, 2));
		Assert.True(MuiCollectionRecordMessageCodec.TryReadCompare(ref platform,
			compareAddress, out var compare));
		Assert.Equal(0x3510u, compare.Entry2);

		var testPosAddress = APTR.FromPointer(0x30C0);
		Assert.True(MuiCollectionRecordMessageCodec.WriteTestPos(ref platform,
			testPosAddress, 8, 9, 0x3800));
		Assert.True(MuiCollectionRecordMessageCodec.TryReadTestPos(ref platform,
			testPosAddress, out var testPos));
		Assert.Equal(8u, testPos.X);
		Assert.Equal(9u, testPos.Y);
		Assert.Equal(0x3800u, testPos.Result);

		Assert.False(MuiCollectionRecordMessageMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x20FF4),
			MuiCollectionRecordPacketKind.Display,
			MuiCollectionRecordField.Row, out _));
		Assert.False(MuiCollectionRecordMessageMemoryCodec.TryGetAddress(
			ref platform, displayAddress, MuiCollectionRecordPacketKind.Compare,
			MuiCollectionRecordField.Pool, out _));
		Assert.False(MuiCollectionRecordMessageMemoryCodec.TryGetAddress(
			ref platform, displayAddress, MuiCollectionRecordPacketKind.Display,
			(MuiCollectionRecordField)255, out _));
		Assert.False(MuiCollectionRecordMessageMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiCollectionRecordPacketKind.TestPos,
			MuiCollectionRecordField.Result, out _));
	}
}
