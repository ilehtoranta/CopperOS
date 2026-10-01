/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeApplicationPushMethodQueueTests
{
	private static readonly APTR Sidecar = APTR.FromPointer(0x1800);
	private static readonly APTR FirstRecord = APTR.FromPointer(0x1900);
	private static readonly APTR SecondRecord = APTR.FromPointer(0x1950);
	private static readonly APTR ThirdRecord = APTR.FromPointer(0x19A0);
	private static readonly APTR Parameters = APTR.FromPointer(0x1A00);
	private static readonly APTR DestinationOne = APTR.FromPointer(0x1B00);
	private static readonly APTR DestinationTwo = APTR.FromPointer(0x1B20);
	private const uint FirstMethod = 0x80420011;
	private const uint SecondMethod = 0x80420022;

	[Fact]
	public void NativePushQueueUsesCompleteNamedRecordAndDeliversFifo()
	{
		Assert.Equal((int)MuiNativeApplicationPushMethodRecord.Size,
			Unsafe.SizeOf<MuiNativeApplicationPushMethodRecord>());
		var memory = NewMemory();
		WriteLiveSidecar(ref memory);
		WriteParameters(ref memory, FirstMethod, 0x11111111);

		Assert.True(MuiNativeApplicationPushMethodQueue.TryEnqueue(ref memory,
			Sidecar, FirstRecord, DestinationOne, 2, Parameters, out var firstId));
		WriteParameters(ref memory, SecondMethod, 0x22222222);
		Assert.True(MuiNativeApplicationPushMethodQueue.TryEnqueue(ref memory,
			Sidecar, SecondRecord, DestinationTwo, 2, Parameters, out var secondId));
		Assert.Equal(1u, firstId);
		Assert.Equal(2u, secondId);
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref memory, Sidecar,
			out var sidecar));
		Assert.Equal(SecondRecord, ReadRecord(ref memory, FirstRecord).Next);
		Assert.Equal(2u, sidecar.ApplicationPushGeneration);
		Assert.True(MuiNativeApplicationPushMethodQueue.Validate(ref memory,
			sidecar));

		Assert.True(MuiNativeApplicationPushMethodQueue.TryTakeNext(ref memory,
			Sidecar, out var hasValue, out var removed, out var first,
			out var firstMessage));
		Assert.True(hasValue);
		Assert.Equal(FirstRecord, removed);
		Assert.Equal(DestinationOne, first.Destination);
		Assert.True(MuiApplicationMethodHeaderCodec.TryReadValue(ref memory,
			firstMessage, out var firstMethod));
		Assert.Equal(FirstMethod, firstMethod);

		Assert.True(MuiNativeApplicationPushMethodQueue.TryTakeNext(ref memory,
			Sidecar, out hasValue, out removed, out var second,
			out var secondMessage));
		Assert.True(hasValue);
		Assert.Equal(SecondRecord, removed);
		Assert.Equal(DestinationTwo, second.Destination);
		Assert.True(MuiApplicationMethodHeaderCodec.TryReadValue(ref memory,
			secondMessage, out var secondMethod));
		Assert.Equal(SecondMethod, secondMethod);
		Assert.True(MuiNativeApplicationPushMethodQueue.TryTakeNext(ref memory,
			Sidecar, out hasValue, out removed, out _, out _));
		Assert.False(hasValue);
		Assert.True(removed.IsNull);
	}

	[Fact]
	public void UnpushRemovesAllMatchingRecordsAndKeepsOtherDestinationsInOrder()
	{
		var memory = NewMemory();
		WriteLiveSidecar(ref memory);
		WriteParameters(ref memory, FirstMethod, 0xA1);
		Assert.True(MuiNativeApplicationPushMethodQueue.TryEnqueue(ref memory,
			Sidecar, FirstRecord, DestinationOne, 2, Parameters, out _));
		WriteParameters(ref memory, SecondMethod, 0xB2);
		Assert.True(MuiNativeApplicationPushMethodQueue.TryEnqueue(ref memory,
			Sidecar, SecondRecord, DestinationTwo, 2, Parameters, out _));
		WriteParameters(ref memory, FirstMethod, 0xC3);
		Assert.True(MuiNativeApplicationPushMethodQueue.TryEnqueue(ref memory,
			Sidecar, ThirdRecord, DestinationOne, 2, Parameters, out _));

		Assert.True(MuiNativeApplicationPushMethodQueue.TryUnpush(ref memory,
			Sidecar, DestinationOne, 0, FirstMethod, out var detached,
			out var removedCount));
		Assert.Equal(2u, removedCount);
		Assert.Equal(FirstRecord, detached);
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref memory, Sidecar,
			out var sidecar));
		Assert.Equal(SecondRecord, sidecar.ApplicationPushQueue);
		Assert.Equal(APTR.Null, ReadRecord(ref memory, FirstRecord).Next);
		Assert.Equal(ThirdRecord,
			ReadRecord(ref memory, FirstRecord).DetachedNext);
		Assert.Equal(APTR.Null, ReadRecord(ref memory, ThirdRecord).Next);
		Assert.True(ReadRecord(ref memory, ThirdRecord).DetachedNext.IsNull);
		Assert.True(MuiNativeApplicationPushMethodQueue.Validate(ref memory,
			sidecar));
		Assert.True(MuiNativeApplicationPushMethodQueue.TryTakeNext(ref memory,
			Sidecar, out var hasValue, out var removed, out var remaining,
			out _));
		Assert.True(hasValue);
		Assert.Equal(SecondRecord, removed);
		Assert.Equal(DestinationTwo, remaining.Destination);
	}

	[Fact]
	public void CorruptNativePushQueueIsRejectedWithoutChangingItsHead()
	{
		var memory = NewMemory();
		var sidecar = NewLiveSidecar();
		sidecar.ApplicationPushQueue = FirstRecord;
		sidecar.ApplicationPushGeneration = 1;
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, Sidecar, sidecar));
		WriteParameters(ref memory, FirstMethod, 0);
		var record = NewRecord(DestinationOne, 1, 2);
		record.Next = FirstRecord;
		Assert.True(MuiNativeApplicationPushMethodRecordCodec.Write(ref memory,
			FirstRecord, record));
		Assert.True(MuiApplicationPushMethodParameterMemoryCodec.TryCopy(ref memory,
			Parameters, Payload(ref memory, FirstRecord, 2), 2));

		Assert.False(MuiNativeApplicationPushMethodQueue.Validate(ref memory,
			sidecar));
		Assert.False(MuiNativeApplicationPushMethodQueue.TryTakeNext(ref memory,
			Sidecar, out var hasValue, out var removed, out _, out _));
		Assert.False(hasValue);
		Assert.True(removed.IsNull);
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref memory, Sidecar,
			out var unchanged));
		Assert.Equal(FirstRecord, unchanged.ApplicationPushQueue);
	}

	private static MuiHeadlessTestPlatform NewMemory() =>
		new(0x1800, 0x400, 0, Sidecar);

	private static MuiNativeMuiObjectRecord NewLiveSidecar() => new()
	{
		Signature = MuiNativeMuiObjectRecord.Magic,
		Revision = MuiNativeMuiObjectRecord.Version,
		Flags = MuiNativeMuiObjectRecord.ObjectInitialized,
		LifecycleState = MuiNativeMuiObjectRecord.StateLive,
	};

	private static void WriteLiveSidecar(ref MuiHeadlessTestPlatform memory) =>
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, Sidecar,
			NewLiveSidecar()));

	private static void WriteParameters(ref MuiHeadlessTestPlatform memory,
		uint method, uint value)
	{
		var first = default(MuiApplicationPushMethodParameterVectorCursor);
		first.Base = Parameters;
		first.Index = 0;
		var second = first;
		second.Index = 1;
		Assert.True(MuiApplicationPushMethodParameterVectorCodec.TryWriteValue(
			ref memory, first, method));
		Assert.True(MuiApplicationPushMethodParameterVectorCodec.TryWriteValue(
			ref memory, second, value));
	}

	private static MuiNativeApplicationPushMethodRecord NewRecord(
		APTR destination, uint queueId, uint parameterCount) => new()
	{
		Signature = MuiNativeApplicationPushMethodRecord.Magic,
		Revision = MuiNativeApplicationPushMethodRecord.Version,
		Destination = destination,
		QueueId = queueId,
		ParameterCount = parameterCount,
	};

	private static MuiNativeApplicationPushMethodRecord ReadRecord(
		ref MuiHeadlessTestPlatform memory, APTR address)
	{
		Assert.True(MuiNativeApplicationPushMethodRecordCodec.TryRead(ref memory,
			address, out var record));
		return record;
	}

	private static APTR Payload(ref MuiHeadlessTestPlatform memory, APTR address,
		uint parameterCount)
	{
		Assert.True(MuiNativeApplicationPushMethodRecordCodec.TryGetPayload(
			ref memory, address, parameterCount, out var payload));
		return payload;
	}
}
