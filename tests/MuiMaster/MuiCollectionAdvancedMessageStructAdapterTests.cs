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
		Assert.True(MuiCollectionAdvancedMessageMemoryCodec.TryWriteUInt32(ref platform,
			insertAddress, MuiCollectionAdvancedPacketKind.Insert,
			MuiCollectionAdvancedField.Count, 5));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadInsert(ref platform,
			insertAddress, out insert));
		Assert.Equal(MuiCollectionAdvancedMessageCodec.Insert, insert.MethodId);
		Assert.Equal(0x3500u, insert.Entries);
		Assert.Equal(5u, insert.Count);
		Assert.Equal(2u, insert.Position);

		var pairAddress = APTR.FromPointer(0x3040);
		Assert.True(MuiCollectionAdvancedMessageCodec.WritePair(ref platform,
			pairAddress, MuiCollectionAdvancedMessageCodec.Move, 1, 3));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadPair(ref platform,
			pairAddress, MuiCollectionAdvancedMessageCodec.Move, out var pair));
		Assert.Equal(1u, pair.First);
		Assert.Equal(3u, pair.Second);
		Assert.True(MuiCollectionAdvancedMessageMemoryCodec.TryWriteUInt32(ref platform,
			pairAddress, MuiCollectionAdvancedPacketKind.Pair,
			MuiCollectionAdvancedField.Second, 4));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadPair(ref platform,
			pairAddress, MuiCollectionAdvancedMessageCodec.Move, out pair));
		Assert.Equal(MuiCollectionAdvancedMessageCodec.Move, pair.MethodId);
		Assert.Equal(1u, pair.First);
		Assert.Equal(4u, pair.Second);

		var createImageAddress = APTR.FromPointer(0x3080);
		Assert.True(MuiCollectionAdvancedMessageCodec.WriteCreateImage(
			ref platform, createImageAddress, 0x3600, 5));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadCreateImage(
			ref platform, createImageAddress, out var image));
		Assert.Equal(0x3600u, image.Image);
		Assert.Equal(5u, image.Flags);
		Assert.True(MuiCollectionAdvancedMessageMemoryCodec.TryWriteUInt32(ref platform,
			createImageAddress, MuiCollectionAdvancedPacketKind.CreateImage,
			MuiCollectionAdvancedField.Flags, 6));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadCreateImage(
			ref platform, createImageAddress, out image));
		Assert.Equal(MuiCollectionAdvancedMessageCodec.CreateImage, image.MethodId);
		Assert.Equal(0x3600u, image.Image);
		Assert.Equal(6u, image.Flags);

		var positionAddress2 = APTR.FromPointer(0x30A0);
		Assert.True(MuiCollectionAdvancedMessageCodec.WritePosition(ref platform,
			positionAddress2, MuiCollectionAdvancedMessageCodec.Jump, 9));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadPosition(ref platform,
			positionAddress2, MuiCollectionAdvancedMessageCodec.Jump,
			out var position));
		Assert.Equal(9u, position.Position);
		Assert.True(MuiCollectionAdvancedMessageMemoryCodec.TryWriteUInt32(ref platform,
			positionAddress2, MuiCollectionAdvancedPacketKind.Position,
			MuiCollectionAdvancedField.Position, 10));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadPosition(ref platform,
			positionAddress2, MuiCollectionAdvancedMessageCodec.Jump,
			out position));
		Assert.Equal(MuiCollectionAdvancedMessageCodec.Jump, position.MethodId);
		Assert.Equal(10u, position.Position);

		var insertSingleAddress = APTR.FromPointer(0x30C0);
		Assert.True(MuiCollectionAdvancedMessageCodec.WriteInsertSingle(ref platform,
			insertSingleAddress, 0x3700, 11));
		Assert.True(MuiCollectionAdvancedMessageMemoryCodec.TryWriteUInt32(ref platform,
			insertSingleAddress, MuiCollectionAdvancedPacketKind.InsertSingle,
			MuiCollectionAdvancedField.Entry, 0x3710));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadInsertSingle(ref platform,
			insertSingleAddress, out var insertSingle));
		Assert.Equal(MuiCollectionAdvancedMessageCodec.InsertSingle,
			insertSingle.MethodId);
		Assert.Equal(0x3710u, insertSingle.Entry);
		Assert.Equal(11u, insertSingle.Position);

		var redrawAddress = APTR.FromPointer(0x30E0);
		Assert.True(MuiCollectionAdvancedMessageCodec.WriteRedraw(ref platform,
			redrawAddress, 12, 0x3720));
		Assert.True(MuiCollectionAdvancedMessageMemoryCodec.TryWriteUInt32(ref platform,
			redrawAddress, MuiCollectionAdvancedPacketKind.Redraw,
			MuiCollectionAdvancedField.Position, 13));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadRedraw(ref platform,
			redrawAddress, out var redraw));
		Assert.Equal(MuiCollectionAdvancedMessageCodec.Redraw, redraw.MethodId);
		Assert.Equal(13u, redraw.Position);
		Assert.Equal(0x3720u, redraw.Entry);

		var pointerAddress = APTR.FromPointer(0x3100);
		Assert.True(MuiCollectionAdvancedMessageCodec.WritePointer(ref platform,
			pointerAddress, MuiCollectionAdvancedMessageCodec.NextSelected, 0x3730));
		Assert.True(MuiCollectionAdvancedMessageMemoryCodec.TryWriteUInt32(ref platform,
			pointerAddress, MuiCollectionAdvancedPacketKind.Pointer,
			MuiCollectionAdvancedField.Pointer, 0x3740));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadPointer(ref platform,
			pointerAddress, MuiCollectionAdvancedMessageCodec.NextSelected,
			out var pointer));
		Assert.Equal(MuiCollectionAdvancedMessageCodec.NextSelected, pointer.MethodId);
		Assert.Equal(0x3740u, pointer.Pointer);

		var methodAddress = APTR.FromPointer(0x3120);
		Assert.True(MuiCollectionAdvancedMessageMemoryCodec.TryWriteUInt32(ref platform,
			methodAddress, MuiCollectionAdvancedPacketKind.Method,
			MuiCollectionAdvancedField.MethodId, MuiCollectionAdvancedMessageCodec.Remove));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadMethodIdValue(ref platform,
			methodAddress, out var methodId));
		Assert.Equal(MuiCollectionAdvancedMessageCodec.Remove, methodId);
		Assert.False(MuiCollectionAdvancedMessageMemoryCodec.TryReadUInt32(ref platform,
			methodAddress, MuiCollectionAdvancedPacketKind.Method,
			MuiCollectionAdvancedField.Entry, out _));
		Assert.False(MuiCollectionAdvancedMessageMemoryCodec.TryWriteUInt32(ref platform,
			pointerAddress, MuiCollectionAdvancedPacketKind.Pointer,
			(MuiCollectionAdvancedField)255, 1));

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

	[Fact]
	public void CollectionAdvancedMethodHeaderUsesSharedUlongStorageBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2FFFC);
		const uint methodId = 0xC2468ACEu;

		Assert.True(MuiCollectionAdvancedMethodHeaderCodec.WriteValue(
			ref platform, address, methodId));
		Assert.True(MuiCollectionAdvancedMethodHeaderCodec.TryReadValue(
			ref platform, address, out var decoded));
		Assert.Equal(methodId, decoded);
		Assert.True(MuiCollectionAdvancedStructPacketCodec.TryReadMethod(
			ref platform, address, out var packet));
		Assert.Equal(methodId, packet.MethodId);
		Assert.True(MuiCollectionAdvancedStructPacketCodec.TryWriteMethod(
			ref platform, address, 0xF1020304u));
		Assert.True(MuiCollectionAdvancedMessageCodec.TryReadMethodIdValue(
			ref platform, address, out decoded));
		Assert.Equal(0xF1020304u, decoded);

		var nearEnd = APTR.FromPointer(0x40FFC);
		Assert.True(MuiCollectionAdvancedMethodHeaderCodec.WriteValue(
			ref platform, nearEnd, methodId));
		Assert.True(MuiCollectionAdvancedMethodHeaderCodec.TryReadValue(
			ref platform, nearEnd, out decoded));
		Assert.Equal(methodId, decoded);
		var truncated = APTR.FromPointer(0x40FFD);
		Assert.False(MuiCollectionAdvancedMethodHeaderCodec.TryReadValue(
			ref platform, truncated, out _));
		Assert.False(MuiCollectionAdvancedMethodHeaderCodec.WriteValue(
			ref platform, truncated, methodId));
		Assert.False(MuiCollectionAdvancedMethodHeaderCodec.TryReadValue(
			ref platform, APTR.Null, out _));
	}
}
