using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiCollectionAdvancedMessageStructAdapterTests
{
	[Fact]
	public void CollectionAdvancedRecordsUseNamedFieldsAndCompleteBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var insertAddress = APTR.FromPointer(0x3000);
		Assert.True(MuiCollectionAdvancedMessageCodec.WriteInsert(ref platform,
			insertAddress, 0x3500, 4, 2));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadInsert(ref platform,
			insertAddress, out var insert));
		Assert.Equal(0x3500u, insert.Entries);
		Assert.Equal(4u, insert.Count);
		Assert.Equal(2u, insert.Position);
		Assert.True(MuiCollectionAdvancedMessageMemoryCodec.TryGetAddress(
			ref platform, insertAddress, MuiCollectionAdvancedPacketKind.Insert,
			MuiCollectionAdvancedField.Position, out var positionAddress));
		Assert.Equal(APTR.FromPointer(0x300C), positionAddress);

		var pairAddress = APTR.FromPointer(0x3040);
		Assert.True(MuiCollectionAdvancedMessageCodec.WritePair(ref platform,
			pairAddress, MuiCollectionAdvancedMessageCodec.Move, 1, 3));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadPair(ref platform,
			pairAddress, MuiCollectionAdvancedMessageCodec.Move, out var pair));
		Assert.Equal(1u, pair.First);
		Assert.Equal(3u, pair.Second);

		var createImageAddress = APTR.FromPointer(0x3080);
		Assert.True(MuiCollectionAdvancedMessageCodec.WriteCreateImage(
			ref platform, createImageAddress, 0x3600, 5));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadCreateImage(
			ref platform, createImageAddress, out var image));
		Assert.Equal(0x3600u, image.Image);
		Assert.Equal(5u, image.Flags);

		var positionAddress2 = APTR.FromPointer(0x30A0);
		Assert.True(MuiCollectionAdvancedMessageCodec.WritePosition(ref platform,
			positionAddress2, MuiCollectionAdvancedMessageCodec.Jump, 9));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadPosition(ref platform,
			positionAddress2, MuiCollectionAdvancedMessageCodec.Jump,
			out var position));
		Assert.Equal(9u, position.Position);

		Assert.False(MuiCollectionAdvancedMessageMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x20FF4),
			MuiCollectionAdvancedPacketKind.Insert,
			MuiCollectionAdvancedField.Position,
			out _));
		Assert.False(MuiCollectionAdvancedMessageMemoryCodec.TryGetAddress(
			ref platform, pairAddress, MuiCollectionAdvancedPacketKind.Pair,
			MuiCollectionAdvancedField.Flags, out _));
		Assert.False(MuiCollectionAdvancedMessageMemoryCodec.TryGetAddress(
			ref platform, insertAddress, MuiCollectionAdvancedPacketKind.Insert,
			(MuiCollectionAdvancedField)255, out _));
		Assert.False(MuiCollectionAdvancedMessageMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiCollectionAdvancedPacketKind.Method,
			MuiCollectionAdvancedField.MethodId, out _));
	}
}
