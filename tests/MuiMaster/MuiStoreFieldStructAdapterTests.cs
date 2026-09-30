/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStoreFieldStructAdapterTests
{
	[Fact]
	public void StoreFieldAdapterUsesNamedRecordsAndPreservesSiblings()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var packet = APTR.FromPointer(0x2400);

		Assert.True(MuiStorePacketSequentialStructCodec.WriteMethod(ref platform,
			packet, new MuiStoreMethodMessage { MethodId = 0x1010 }));
		Assert.True(MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiStorePacketKind.Method, MuiStoreField.MethodId, 0x2020));
		Assert.True(MuiStorePacketSequentialStructCodec.TryReadMethod(ref platform,
			packet, out var method));
		Assert.Equal(0x2020u, method.MethodId);

		Assert.True(MuiStorePacketSequentialStructCodec.WriteClear(ref platform,
			packet, new MuiStoreClearMessage { MethodId = 0x3030 }));
		Assert.True(MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, packet,
			MuiStorePacketKind.Clear, MuiStoreField.MethodId, out var clearMethod));
		Assert.Equal(0x3030u, clearMethod);

		Assert.True(MuiStorePacketSequentialStructCodec.WriteKey(ref platform,
			packet, new MuiStoreKeyMessage { MethodId = 0x4040,
				Key = APTR.FromPointer(0x4100) }));
		Assert.True(MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, packet,
			MuiStorePacketKind.Key, MuiStoreField.Key, 0x4200));
		Assert.True(MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, packet,
			MuiStorePacketKind.Key, MuiStoreField.MethodId, out var keyMethod));
		Assert.True(MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, packet,
			MuiStorePacketKind.Key, MuiStoreField.Key, out var key));
		Assert.Equal(0x4040u, keyMethod);
		Assert.Equal(0x4200u, key);

		Assert.True(MuiStorePacketSequentialStructCodec.WriteCounter(ref platform,
			packet, new MuiStoreCounterMessage { MethodId = 0x5050,
				Counter = APTR.FromPointer(0x5100) }));
		Assert.True(MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, packet,
			MuiStorePacketKind.Counter, MuiStoreField.MethodId, 0x5151));
		Assert.True(MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, packet,
			MuiStorePacketKind.Counter, MuiStoreField.Counter, out var counter));
		Assert.Equal(0x5100u, counter);

		Assert.True(MuiStorePacketSequentialStructCodec.WriteDatamapSet(
			ref platform, packet, new MuiDatamapSetMessage { MethodId = 0x6060,
				Data = APTR.FromPointer(0x6100), Length = 12,
				Key = APTR.FromPointer(0x6200) }));
		Assert.True(MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, packet,
			MuiStorePacketKind.DatamapSet, MuiStoreField.Data, 0x6300));
		Assert.True(MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, packet,
			MuiStorePacketKind.DatamapSet, MuiStoreField.Length,
			unchecked((uint)-24)));
		Assert.True(MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, packet,
			MuiStorePacketKind.DatamapSet, MuiStoreField.Key, 0x6400));
		Assert.True(MuiStorePacketSequentialStructCodec.TryReadDatamapSet(
			ref platform, packet, out var datamapSet));
		Assert.Equal(0x6060u, datamapSet.MethodId);
		Assert.Equal(0x6300u, datamapSet.Data.Raw);
		Assert.Equal(-24, datamapSet.Length);
		Assert.Equal(0x6400u, datamapSet.Key.Raw);

		Assert.True(MuiStorePacketSequentialStructCodec.WriteDatamapGet(
			ref platform, packet, new MuiDatamapGetMessage { MethodId = 0x7070,
				Key = APTR.FromPointer(0x7100),
				SizeStorage = APTR.FromPointer(0x7200) }));
		Assert.True(MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, packet,
			MuiStorePacketKind.DatamapGet, MuiStoreField.SizeStorage, 0x7300));
		Assert.True(MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, packet,
			MuiStorePacketKind.DatamapGet, MuiStoreField.MethodId,
			out var getMethod));
		Assert.True(MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, packet,
			MuiStorePacketKind.DatamapGet, MuiStoreField.Key, out var getKey));
		Assert.Equal(0x7070u, getMethod);
		Assert.Equal(0x7100u, getKey);
		Assert.True(MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, packet,
			MuiStorePacketKind.DatamapGet, MuiStoreField.SizeStorage,
			out var sizeStorage));
		Assert.Equal(0x7300u, sizeStorage);

		Assert.True(MuiStorePacketSequentialStructCodec.WriteObjectmapSet(
			ref platform, packet, new MuiObjectmapSetMessage { MethodId = 0x8080,
				Object = APTR.FromPointer(0x8100), Key = APTR.FromPointer(0x8200) }));
		Assert.True(MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform, packet,
			MuiStorePacketKind.ObjectmapSet, MuiStoreField.Object, 0x8300));
		Assert.True(MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, packet,
			MuiStorePacketKind.ObjectmapSet, MuiStoreField.MethodId,
			out var objectMethod));
		Assert.True(MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, packet,
			MuiStorePacketKind.ObjectmapSet, MuiStoreField.Key, out var objectKey));
		Assert.Equal(0x8080u, objectMethod);
		Assert.Equal(0x8200u, objectKey);
	}

	[Fact]
	public void StoreFieldAdapterRejectsMismatchedAndTruncatedRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var packet = APTR.FromPointer(0x2400);
		Assert.True(MuiStorePacketSequentialStructCodec.WriteDatamapSet(
			ref platform, packet, new MuiDatamapSetMessage { MethodId = 1,
				Data = APTR.FromPointer(2), Length = 3, Key = APTR.FromPointer(4) }));
		Assert.False(MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, packet,
			MuiStorePacketKind.Clear, MuiStoreField.Key, out _));
		Assert.False(MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform, packet,
			MuiStorePacketKind.DatamapSet, (MuiStoreField)255, out _));
		Assert.False(MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform,
			APTR.Null, MuiStorePacketKind.DatamapSet, MuiStoreField.Data, 1));
		Assert.False(MuiStoreMessageMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x20FFC), MuiStorePacketKind.DatamapSet,
			MuiStoreField.Key, out _));
		Assert.False(MuiStoreMessageMemoryCodec.TryWriteUInt32(ref platform,
			APTR.FromPointer(0x20FFC), MuiStorePacketKind.DatamapSet,
			MuiStoreField.Key, 1));
	}
}
