/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStorePacketSequentialStructCodecTests
{
	[Fact]
	public void StorePacketsRoundTripThroughNamedSequentialStructs()
	{
		Assert.Equal(4, Unsafe.SizeOf<MuiStoreMethodMessage>());
		Assert.Equal(4, Unsafe.SizeOf<MuiStoreClearMessage>());
		Assert.Equal(8, Unsafe.SizeOf<MuiStoreKeyMessage>());
		Assert.Equal(8, Unsafe.SizeOf<MuiStoreCounterMessage>());
		Assert.Equal(16, Unsafe.SizeOf<MuiDatamapSetMessage>());
		Assert.Equal(12, Unsafe.SizeOf<MuiDatamapGetMessage>());
		Assert.Equal(12, Unsafe.SizeOf<MuiObjectmapSetMessage>());
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));

		var method = new MuiStoreMethodMessage
		{
			MethodId = MuiStoreMessageCore.DatamapGetMethod,
		};
		Assert.True(MuiStorePacketSequentialStructCodec.WriteMethod(ref platform,
			APTR.FromPointer(0x2800), method));
		Assert.True(MuiStorePacketCodec.TryReadMethod(ref platform,
			APTR.FromPointer(0x2800), out var methodDecoded));
		Assert.Equal(method.MethodId, methodDecoded.MethodId);

		var clear = new MuiStoreClearMessage
		{
			MethodId = MuiStoreMessageCore.ObjectmapClearMethod,
		};
		Assert.True(MuiStorePacketSequentialStructCodec.WriteClear(ref platform,
			APTR.FromPointer(0x2810), clear));
		Assert.True(MuiStorePacketCodec.TryReadClear(ref platform,
			APTR.FromPointer(0x2810), out var clearDecoded));
		Assert.Equal(clear.MethodId, clearDecoded.MethodId);

		var key = new MuiStoreKeyMessage
		{
			MethodId = MuiStoreMessageCore.DatamapFindMethod,
			Key = APTR.FromPointer(0xFEDCBA98u),
		};
		Assert.True(MuiStorePacketSequentialStructCodec.WriteKey(ref platform,
			APTR.FromPointer(0x2820), key));
		Assert.True(MuiStorePacketCodec.TryReadKey(ref platform,
			APTR.FromPointer(0x2820), out var keyDecoded));
		Assert.Equal(key.MethodId, keyDecoded.MethodId);
		Assert.Equal(key.Key, keyDecoded.Key);

		var counter = new MuiStoreCounterMessage
		{
			MethodId = MuiStoreMessageCore.ObjectmapIterateMethod,
			Counter = APTR.FromPointer(0x81234567u),
		};
		Assert.True(MuiStorePacketSequentialStructCodec.WriteCounter(ref platform,
			APTR.FromPointer(0x2830), counter));
		Assert.True(MuiStorePacketCodec.TryReadCounter(ref platform,
			APTR.FromPointer(0x2830), out var counterDecoded));
		Assert.Equal(counter.MethodId, counterDecoded.MethodId);
		Assert.Equal(counter.Counter, counterDecoded.Counter);

		var set = new MuiDatamapSetMessage
		{
			MethodId = MuiStoreMessageCore.DatamapSetMethod,
			Data = APTR.FromPointer(0xF1234567u),
			Length = int.MinValue + 17,
			Key = APTR.FromPointer(0x87654321u),
		};
		Assert.True(MuiStorePacketSequentialStructCodec.WriteDatamapSet(ref platform,
			APTR.FromPointer(0x2840), set));
		Assert.True(MuiStorePacketCodec.TryReadDatamapSet(ref platform,
			APTR.FromPointer(0x2840), out var setDecoded));
		Assert.Equal(set.MethodId, setDecoded.MethodId);
		Assert.Equal(set.Data, setDecoded.Data);
		Assert.Equal(set.Length, setDecoded.Length);
		Assert.Equal(set.Key, setDecoded.Key);

		var get = new MuiDatamapGetMessage
		{
			MethodId = MuiStoreMessageCore.DatamapGetMethod,
			Key = APTR.FromPointer(0xFEDCBA98u),
			SizeStorage = APTR.FromPointer(0x81234567u),
		};
		Assert.True(MuiStorePacketSequentialStructCodec.WriteDatamapGet(ref platform,
			APTR.FromPointer(0x2850), get));
		Assert.True(MuiStorePacketCodec.TryReadDatamapGet(ref platform,
			APTR.FromPointer(0x2850), out var getDecoded));
		Assert.Equal(get.MethodId, getDecoded.MethodId);
		Assert.Equal(get.Key, getDecoded.Key);
		Assert.Equal(get.SizeStorage, getDecoded.SizeStorage);

		var objectmap = new MuiObjectmapSetMessage
		{
			MethodId = MuiStoreMessageCore.ObjectmapSetMethod,
			Object = APTR.FromPointer(0xF1234567u),
			Key = APTR.FromPointer(0x87654321u),
		};
		Assert.True(MuiStorePacketSequentialStructCodec.WriteObjectmapSet(ref platform,
			APTR.FromPointer(0x2860), objectmap));
		Assert.True(MuiStorePacketCodec.TryReadObjectmapSet(ref platform,
			APTR.FromPointer(0x2860), out var objectDecoded));
		Assert.Equal(objectmap.MethodId, objectDecoded.MethodId);
		Assert.Equal(objectmap.Object, objectDecoded.Object);
		Assert.Equal(objectmap.Key, objectDecoded.Key);
	}

	[Fact]
	public void StorePacketStructCodecsRejectNullAndIncompleteGuestRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var method = new MuiStoreMethodMessage
		{
			MethodId = MuiStoreMessageCore.DatamapGetMethod,
		};
		var key = new MuiStoreKeyMessage
		{
			MethodId = MuiStoreMessageCore.DatamapFindMethod,
			Key = APTR.Null,
		};
		var set = new MuiDatamapSetMessage
		{
			MethodId = MuiStoreMessageCore.DatamapSetMethod,
		};

		Assert.False(MuiStorePacketSequentialStructCodec.WriteMethod(ref platform,
			APTR.Null, method));
		Assert.False(MuiStorePacketSequentialStructCodec.TryReadMethod(ref platform,
			APTR.FromPointer(0x30FFF), out _));
		Assert.False(MuiStorePacketSequentialStructCodec.WriteKey(ref platform,
			APTR.FromPointer(0x30FF9), key));
		Assert.False(MuiStorePacketSequentialStructCodec.TryReadKey(ref platform,
			APTR.FromPointer(0x30FF9), out _));
		Assert.False(MuiStorePacketSequentialStructCodec.WriteDatamapSet(ref platform,
			APTR.FromPointer(0x30FF1), set));
		Assert.False(MuiStorePacketSequentialStructCodec.TryReadDatamapSet(ref platform,
			APTR.FromPointer(0x30FF1), out _));
		Assert.False(MuiStoreMessageCodec.TryReadMethodIdValue(ref platform,
			APTR.FromPointer(0x30FFF), out _));
	}
}
