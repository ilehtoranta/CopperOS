/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Native Application_PushMethod entries own a complete copied method packet.
// The first word of the variable tail is the destination MethodID, followed
// by the caller's bounded ULONG arguments. DetachedNext is used only after an
// Unpush operation and keeps reclamation separate from the live queue link.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeApplicationPushMethodRecord
{
	internal const uint Magic = 0x4D55504D; // "MUPM"
	internal const uint Version = 1;
	internal const uint Size = 28;
	internal const uint FieldSize = 4;
	internal const uint MaximumParameterCount =
		MuiApplicationPushMethodMessage.MaximumParameterCount;

	internal uint Signature;
	internal uint Revision;
	internal APTR Next;
	internal APTR DetachedNext;
	internal APTR Destination;
	internal uint QueueId;
	internal uint ParameterCount;
}

internal static class MuiNativeApplicationPushMethodRecordCodec
{
	internal static bool TryRead<TMemory>(ref TMemory memory, APTR address,
		out MuiNativeApplicationPushMethodRecord value)
		where TMemory : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeApplicationPushMethodRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Signature) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Revision) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var next) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var detachedNext) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var destination) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.QueueId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.ParameterCount) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Next = APTR.FromPointer(next);
		value.DetachedNext = APTR.FromPointer(detachedNext);
		value.Destination = APTR.FromPointer(destination);
		return value.Signature == MuiNativeApplicationPushMethodRecord.Magic &&
			value.Revision == MuiNativeApplicationPushMethodRecord.Version &&
			value.Destination.IsNotNull && value.QueueId != 0 &&
			value.ParameterCount != 0 && value.ParameterCount <=
				MuiNativeApplicationPushMethodRecord.MaximumParameterCount &&
			TryGetPayload(ref memory, address, value.ParameterCount, out _);
	}

	internal static bool Write<TMemory>(ref TMemory memory, APTR address,
		MuiNativeApplicationPushMethodRecord value)
		where TMemory : struct, IMuiGuestMemory
	{
		if (address.IsNull || value.Signature !=
			MuiNativeApplicationPushMethodRecord.Magic || value.Revision !=
			MuiNativeApplicationPushMethodRecord.Version ||
			value.Destination.IsNull || value.QueueId == 0 ||
			value.ParameterCount == 0 || value.ParameterCount >
				MuiNativeApplicationPushMethodRecord.MaximumParameterCount ||
			!MuiGuestStructCursor.TryCreate(ref memory, address,
				MuiNativeApplicationPushMethodRecord.Size, out var cursor))
			return false;
		return MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Signature) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
				value.Revision) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
				value.Next.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
				value.DetachedNext.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
				value.Destination.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
				value.QueueId) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
				value.ParameterCount) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryGetPayload<TMemory>(ref TMemory memory,
		APTR address, uint parameterCount, out APTR payload)
		where TMemory : struct, IMuiGuestMemory
	{
		payload = APTR.Null;
		if (address.IsNull || parameterCount == 0 || parameterCount >
			MuiNativeApplicationPushMethodRecord.MaximumParameterCount)
			return false;
		var payloadSize = parameterCount * MuiNativeApplicationPushMethodRecord.FieldSize;
		var totalSize = MuiNativeApplicationPushMethodRecord.Size + payloadSize;
		if (totalSize < MuiNativeApplicationPushMethodRecord.Size ||
			!MuiGuestStructCursor.TryCreate(ref memory, address, totalSize,
				out var cursor)) return false;
		for (var field = 0u; field < 7u; field++)
			if (!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeApplicationPushMethodRecord.FieldSize, out _)) return false;
		return MuiGuestStructCursor.TryTake(ref memory, ref cursor, payloadSize,
			out payload) && MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Queue topology and packet-copy rules are pure guest-memory operations. The
// Exec allocation, task wake-up, DoMethod call, and final reclamation stay in
// the native adapter below so this core can be exercised over named test memory.
internal static class MuiNativeApplicationPushMethodQueue
{
	internal static bool Validate<TMemory>(ref TMemory memory,
		MuiNativeMuiObjectRecord sidecar)
		where TMemory : struct, IMuiGuestMemory =>
		TryValidate(ref memory, sidecar, out _);

	private static bool TryValidate<TMemory>(ref TMemory memory,
		MuiNativeMuiObjectRecord sidecar, out APTR tail)
		where TMemory : struct, IMuiGuestMemory
	{
		tail = APTR.Null;
		var current = sidecar.ApplicationPushQueue;
		var previousQueueId = 0u;
		uint visited = 0;
		while (current.IsNotNull)
		{
			if (visited++ >= MuiHeadlessLayout.MaximumTraversal ||
				!MuiNativeApplicationPushMethodRecordCodec.TryRead(ref memory,
					current, out var record) || record.DetachedNext.IsNotNull ||
				record.Next == current || record.QueueId <= previousQueueId ||
				record.QueueId > sidecar.ApplicationPushGeneration ||
				!MuiNativeApplicationPushMethodRecordCodec.TryGetPayload(ref memory,
					current, record.ParameterCount, out var methodMessage) ||
				!MuiApplicationMethodHeaderCodec.TryReadValue(ref memory,
					methodMessage, out _)) return false;
			previousQueueId = record.QueueId;
			tail = current;
			current = record.Next;
		}
		return true;
	}

	internal static bool TryEnqueue<TMemory>(ref TMemory memory,
		APTR sidecarAddress, APTR recordAddress, APTR destination,
		uint parameterCount, APTR parameters, out uint queueId)
		where TMemory : struct, IMuiGuestMemory
	{
		queueId = 0;
		if (recordAddress.IsNull || destination.IsNull || parameters.IsNull ||
			parameterCount == 0 || parameterCount >
				MuiNativeApplicationPushMethodRecord.MaximumParameterCount ||
			!TryReadLiveSidecar(ref memory, sidecarAddress, out var sidecar) ||
			sidecar.ApplicationPushGeneration == uint.MaxValue ||
			!TryValidate(ref memory, sidecar, out var tail)) return false;

		var totalSize = MuiNativeApplicationPushMethodRecord.Size +
			parameterCount * MuiNativeApplicationPushMethodRecord.FieldSize;
		if (totalSize < MuiNativeApplicationPushMethodRecord.Size ||
			!memory.IsMapped(recordAddress, totalSize) ||
			!memory.IsMapped(parameters, parameterCount *
				MuiApplicationPushMethodParameter.Size)) return false;

		var current = sidecar.ApplicationPushQueue;
		uint visited = 0;
		while (current.IsNotNull)
		{
			if (current == recordAddress || visited++ >=
				MuiHeadlessLayout.MaximumTraversal ||
				!MuiNativeApplicationPushMethodRecordCodec.TryRead(ref memory,
					current, out var record)) return false;
			current = record.Next;
		}

		queueId = sidecar.ApplicationPushGeneration + 1u;
		var newRecord = new MuiNativeApplicationPushMethodRecord
		{
			Signature = MuiNativeApplicationPushMethodRecord.Magic,
			Revision = MuiNativeApplicationPushMethodRecord.Version,
			Destination = destination,
			QueueId = queueId,
			ParameterCount = parameterCount,
		};
		if (!MuiNativeApplicationPushMethodRecordCodec.Write(ref memory,
			recordAddress, newRecord) ||
			!MuiNativeApplicationPushMethodRecordCodec.TryGetPayload(ref memory,
				recordAddress, parameterCount, out var newPayload) ||
			!MuiApplicationPushMethodParameterMemoryCodec.TryCopy(ref memory,
				parameters, newPayload, parameterCount) ||
			!MuiNativeApplicationPushMethodRecordCodec.TryRead(ref memory,
				recordAddress, out _))
		{
			queueId = 0;
			return false;
		}

		// Commit the monotonic ID before linking onto a non-empty queue. If the
		// tail write is refused, a skipped ID is harmless and no live link changes.
		sidecar.ApplicationPushGeneration = queueId;
		if (tail.IsNull) sidecar.ApplicationPushQueue = recordAddress;
		if (!MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress, sidecar))
		{
			queueId = 0;
			return false;
		}
		if (tail.IsNotNull)
		{
			if (!MuiNativeApplicationPushMethodRecordCodec.TryRead(ref memory,
				tail, out var tailRecord))
			{
				queueId = 0;
				return false;
			}
			tailRecord.Next = recordAddress;
			if (!MuiNativeApplicationPushMethodRecordCodec.Write(ref memory,
				tail, tailRecord))
			{
				queueId = 0;
				return false;
			}
		}
		return true;
	}

	internal static bool TryTakeNext<TMemory>(ref TMemory memory,
		APTR sidecarAddress, out bool hasValue, out APTR recordAddress,
		out MuiNativeApplicationPushMethodRecord record, out APTR methodMessage)
		where TMemory : struct, IMuiGuestMemory
	{
		hasValue = false;
		recordAddress = APTR.Null;
		record = default;
		methodMessage = APTR.Null;
		if (!TryReadLiveSidecar(ref memory, sidecarAddress, out var sidecar) ||
			!TryValidate(ref memory, sidecar, out _)) return false;
		if (sidecar.ApplicationPushQueue.IsNull) return true;
		recordAddress = sidecar.ApplicationPushQueue;
		if (!MuiNativeApplicationPushMethodRecordCodec.TryRead(ref memory,
			recordAddress, out record) ||
			!MuiNativeApplicationPushMethodRecordCodec.TryGetPayload(ref memory,
				recordAddress, record.ParameterCount, out methodMessage))
			return false;
		sidecar.ApplicationPushQueue = record.Next;
		if (!MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress, sidecar))
		{
			recordAddress = APTR.Null;
			methodMessage = APTR.Null;
			record = default;
			return false;
		}
		hasValue = true;
		return true;
	}

	internal static bool TryUnpush<TMemory>(ref TMemory memory,
		APTR sidecarAddress, APTR destination, uint queueIdSelector,
		uint methodSelector, out APTR detachedHead, out uint removedCount)
		where TMemory : struct, IMuiGuestMemory
	{
		detachedHead = APTR.Null;
		removedCount = 0;
		if (!TryReadLiveSidecar(ref memory, sidecarAddress, out var sidecar) ||
			!TryValidate(ref memory, sidecar, out _)) return false;

		var current = sidecar.ApplicationPushQueue;
		var previousLive = APTR.Null;
		var detachedTail = APTR.Null;
		uint visited = 0;
		while (current.IsNotNull)
		{
			if (visited++ >= MuiHeadlessLayout.MaximumTraversal ||
				!MuiNativeApplicationPushMethodRecordCodec.TryRead(ref memory,
					current, out var record) ||
				!MuiNativeApplicationPushMethodRecordCodec.TryGetPayload(ref memory,
					current, record.ParameterCount, out var methodMessage) ||
				!MuiApplicationMethodHeaderCodec.TryReadValue(ref memory,
					methodMessage, out var methodId)) return false;
			var next = record.Next;
			var matches = (destination.IsNull || destination == record.Destination) &&
				(queueIdSelector == 0 || queueIdSelector == record.QueueId) &&
				(methodSelector == 0 || methodSelector == methodId);
			if (matches)
			{
				if (previousLive.IsNull)
				{
					sidecar.ApplicationPushQueue = next;
					if (!MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress,
						sidecar)) return false;
				}
				else
				{
					if (!MuiNativeApplicationPushMethodRecordCodec.TryRead(
						ref memory, previousLive, out var previousRecord)) return false;
					previousRecord.Next = next;
					if (!MuiNativeApplicationPushMethodRecordCodec.Write(ref memory,
						previousLive, previousRecord)) return false;
				}

				record.Next = APTR.Null;
				if (!MuiNativeApplicationPushMethodRecordCodec.Write(ref memory,
					current, record)) return false;
				if (detachedTail.IsNull) detachedHead = current;
				else
				{
					if (!MuiNativeApplicationPushMethodRecordCodec.TryRead(
						ref memory, detachedTail, out var previousDetached)) return false;
					previousDetached.DetachedNext = current;
					if (!MuiNativeApplicationPushMethodRecordCodec.Write(ref memory,
						detachedTail, previousDetached)) return false;
				}
				detachedTail = current;
				removedCount++;
			}
			else previousLive = current;
			current = next;
		}
		return true;
	}

	internal static void FreeValidated<TMemory>(ref TMemory memory, APTR head)
		where TMemory : struct, IMuiGuestMemory
	{
		var current = head;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativeApplicationPushMethodRecordCodec.TryRead(ref memory,
				current, out var record)) return;
			var size = MuiNativeApplicationPushMethodRecord.Size +
				record.ParameterCount * MuiNativeApplicationPushMethodRecord.FieldSize;
			memory.Clear(current, size);
			Exec.FreeMem(current, size);
			current = record.Next;
		}
	}

	private static bool TryReadLiveSidecar<TMemory>(ref TMemory memory,
		APTR address, out MuiNativeMuiObjectRecord sidecar)
		where TMemory : struct, IMuiGuestMemory
	{
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, address, out sidecar) ||
			sidecar.LifecycleState != MuiNativeMuiObjectRecord.StateLive ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) == 0 ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectDisposing) != 0)
		{
			sidecar = default;
			return false;
		}
		return true;
	}
}

internal static class MuiNativeApplicationPushMethodDispatch
{
	internal static bool TryDispatch(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR application, APTR message,
		uint method, out uint result)
	{
		result = 0;
		if (method == MuiApplicationPushMethodMessage.Id)
			return TryPush(ref platform, publicObjects, ownerRoot, application,
				message, method, out result);
		if (method == MuiApplicationUnpushMethodMessage.Id)
			return TryUnpush(ref platform, publicObjects, ownerRoot, application,
				message, method, out result);
		return false;
	}

	internal static uint DispatchNext(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR application)
	{
		if (!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
			ownerRoot, application, out var binding)) return 0;
		var hasValue = false;
		var recordAddress = APTR.Null;
		var record = default(MuiNativeApplicationPushMethodRecord);
		var methodMessage = APTR.Null;
		Exec.Forbid();
		var memory = default(MuiNativeClassMemory);
		var taken = MuiNativePublicObjectCore.TryFindLive(ref platform,
			publicObjects, ownerRoot, application, out var currentBinding) &&
			currentBinding.Sidecar == binding.Sidecar &&
			MuiNativeApplicationPushMethodQueue.TryTakeNext(ref memory,
				binding.Sidecar, out hasValue, out recordAddress,
				out record, out methodMessage);
		Exec.Permit();
		if (!taken || !hasValue) return 0;
		var result = MuiNativeBoopsiDispatch.DoMethod(ref platform,
			record.Destination, methodMessage);
		Free(ref platform, recordAddress, record.ParameterCount);
		return result;
	}

	private static bool TryPush(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR application, APTR message,
		uint method, out uint result)
	{
		result = 0;
		if (!MuiApplicationPushMethodMessageCodec.TryRead(ref platform, message,
			out var packet) || packet.MethodId != method ||
			method != MuiApplicationPushMethodMessage.Id ||
			!MuiApplicationQueuePacketCodec.TryGetParameters(ref platform, message,
				packet.Count, out var parameters)) return false;
		var destination = APTR.FromPointer(packet.Destination);
		var size = MuiNativeApplicationPushMethodRecord.Size + packet.Count *
			MuiNativeApplicationPushMethodRecord.FieldSize;
		var allocation = platform.Allocate(size,
			MuiHeadlessLayout.AllocationFlags);
		if (allocation.IsNull) return true;

		var wakeTask = APTR.Null;
		var wakeMask = 0u;
		var queued = false;
		Exec.Forbid();
		var memory = default(MuiNativeClassMemory);
		if (MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
			ownerRoot, application, out var binding) &&
			MuiNativeApplicationPushMethodQueue.TryEnqueue(ref memory,
				binding.Sidecar, allocation, destination, packet.Count, parameters,
				out var queueId))
		{
			queued = true;
			result = queueId;
			MuiNativeApplicationInputWaitState.TryGet(ref memory,
				binding.Sidecar, out wakeTask, out wakeMask);
			var currentTask = Exec.FindTask(CString.FromPointer(0));
			if (wakeTask.IsNotNull && wakeMask != 0 &&
				currentTask != wakeTask) Exec.Signal(wakeTask, wakeMask);
		}
		Exec.Permit();
		if (!queued) Free(ref platform, allocation, packet.Count);
		return true;
	}

	private static bool TryUnpush(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR application, APTR message,
		uint method, out uint result)
	{
		result = 0;
		if (!MuiApplicationUnpushMethodMessageCodec.TryRead(ref platform,
			message, out var packet) || packet.MethodId != method ||
			method != MuiApplicationUnpushMethodMessage.Id) return false;
		var detached = APTR.Null;
		var removed = 0u;
		Exec.Forbid();
		var memory = default(MuiNativeClassMemory);
		var valid = MuiNativePublicObjectCore.TryFindLive(ref platform,
			publicObjects, ownerRoot, application, out var binding) &&
			MuiNativeApplicationPushMethodQueue.TryUnpush(ref memory,
				binding.Sidecar, APTR.FromPointer(packet.TargetObject),
				packet.MethodIdSelector, packet.Method, out detached, out removed);
		Exec.Permit();
		if (!valid) return true;
		result = removed;
		FreeDetached(ref platform, detached);
		return true;
	}

	private static void FreeDetached(ref MuiNativeClassPlatform platform,
		APTR head)
	{
		var memory = default(MuiNativeClassMemory);
		var current = head;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativeApplicationPushMethodRecordCodec.TryRead(ref memory,
				current, out var record)) return;
			var next = record.DetachedNext;
			Free(ref platform, current, record.ParameterCount);
			current = next;
		}
	}

	private static void Free(ref MuiNativeClassPlatform platform, APTR address,
		uint parameterCount)
	{
		if (address.IsNull || parameterCount == 0 || parameterCount >
			MuiNativeApplicationPushMethodRecord.MaximumParameterCount) return;
		var size = MuiNativeApplicationPushMethodRecord.Size + parameterCount *
			MuiNativeApplicationPushMethodRecord.FieldSize;
		platform.Clear(address, size);
		platform.Free(address, size);
	}
}
