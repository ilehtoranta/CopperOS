/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Native Application ReturnIDs are owned by the MUI object sidecar.  Each
// queue element is a complete named record; the native object and caller
// message layouts are never used as storage for scheduler state.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeReturnIdRecord
{
	internal const uint Magic = 0x4D554952; // "MUIR"
	internal const uint Version = 1;
	internal const uint Size = 16;

	internal uint Signature;
	internal uint Revision;
	internal APTR Next;
	internal uint ReturnId;
}

internal static class MuiNativeReturnIdRecordCodec
{
	internal static bool TryRead<T>(ref T memory, APTR address,
		out MuiNativeReturnIdRecord value)
		where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeReturnIdRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Signature) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Revision) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var next) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.ReturnId) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Next = APTR.FromPointer(next);
		return value.Signature == MuiNativeReturnIdRecord.Magic &&
			value.Revision == MuiNativeReturnIdRecord.Version;
	}

	internal static bool Write<T>(ref T memory, APTR address,
		MuiNativeReturnIdRecord value)
		where T : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeReturnIdRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Signature) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Revision) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Next.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.ReturnId) && MuiGuestStructCursor.IsComplete(cursor);
}

// Queue validation and mutation are independent of allocation policy so the
// same struct codec can be exercised on host memory and used by the native
// Exec-backed adapter.
internal static class MuiNativeReturnIdQueue
{
	internal static bool Validate<T>(ref T memory,
		MuiNativeMuiObjectRecord sidecar)
		where T : struct, IMuiGuestMemory =>
		TryFindTail(ref memory, sidecar.ReturnIdQueue, APTR.Null, out _);

	internal static bool TryEnqueue<T>(ref T memory, APTR sidecarAddress,
		APTR newRecordAddress, uint returnId)
		where T : struct, IMuiGuestMemory
	{
		if (newRecordAddress.IsNull || !memory.IsMapped(newRecordAddress,
			MuiNativeReturnIdRecord.Size) ||
			!TryReadLiveSidecar(ref memory, sidecarAddress, out var sidecar) ||
			!TryFindTail(ref memory, sidecar.ReturnIdQueue,
				newRecordAddress, out var tail)) return false;

		var record = default(MuiNativeReturnIdRecord);
		record.Signature = MuiNativeReturnIdRecord.Magic;
		record.Revision = MuiNativeReturnIdRecord.Version;
		record.ReturnId = returnId;
		if (!MuiNativeReturnIdRecordCodec.Write(ref memory, newRecordAddress,
			record)) return false;

		if (tail.IsNull)
		{
			sidecar.ReturnIdQueue = newRecordAddress;
			return MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress,
				sidecar);
		}

		if (!MuiNativeReturnIdRecordCodec.TryRead(ref memory, tail,
			out var tailRecord) || !tailRecord.Next.IsNull) return false;
		tailRecord.Next = newRecordAddress;
		return MuiNativeReturnIdRecordCodec.Write(ref memory, tail, tailRecord);
	}

	// Returns false for malformed state. An empty, valid queue returns true with
	// hasValue=false; callers can then continue native window input processing.
	internal static bool TryDequeue<T>(ref T memory, APTR sidecarAddress,
		out bool hasValue, out uint returnId, out APTR removedRecord)
		where T : struct, IMuiGuestMemory
	{
		hasValue = false;
		returnId = 0;
		removedRecord = APTR.Null;
		if (!TryReadLiveSidecar(ref memory, sidecarAddress, out var sidecar) ||
			!TryFindTail(ref memory, sidecar.ReturnIdQueue, APTR.Null,
				out _)) return false;
		if (sidecar.ReturnIdQueue.IsNull) return true;

		var head = sidecar.ReturnIdQueue;
		if (!MuiNativeReturnIdRecordCodec.TryRead(ref memory, head,
			out var record)) return false;
		sidecar.ReturnIdQueue = record.Next;
		if (!MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress,
			sidecar)) return false;
		hasValue = true;
		returnId = record.ReturnId;
		removedRecord = head;
		return true;
	}

	private static bool TryReadLiveSidecar<T>(ref T memory, APTR address,
		out MuiNativeMuiObjectRecord sidecar)
		where T : struct, IMuiGuestMemory
	{
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, address,
			out sidecar) || sidecar.LifecycleState !=
			MuiNativeMuiObjectRecord.StateLive ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) == 0 ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectDisposing) != 0)
		{
			sidecar = default;
			return false;
		}
		return true;
	}

	private static bool TryFindTail<T>(ref T memory, APTR head,
		APTR exclude, out APTR tail)
		where T : struct, IMuiGuestMemory
	{
		tail = APTR.Null;
		var current = head;
		uint visited = 0;
		while (current.IsNotNull)
		{
			if (current == exclude || visited++ >=
				MuiHeadlessLayout.MaximumTraversal ||
				!MuiNativeReturnIdRecordCodec.TryRead(ref memory, current,
					out var record) || record.Next == current) return false;
			tail = current;
			current = record.Next;
		}
		return true;
	}
}

internal static class MuiNativeApplicationInputWaitState
{
	internal static bool TrySet<T>(ref T memory, APTR sidecarAddress,
		APTR task, uint signalMask)
		where T : struct, IMuiGuestMemory
	{
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
			out var sidecar) || sidecar.LifecycleState !=
			MuiNativeMuiObjectRecord.StateLive ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) == 0 ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectDisposing) != 0)
			return false;
		sidecar.InputSignalTask = task;
		sidecar.InputSignalMask = signalMask;
		return MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress,
			sidecar);
	}

	internal static bool TryGet<T>(ref T memory, APTR sidecarAddress,
		out APTR task, out uint signalMask)
		where T : struct, IMuiGuestMemory
	{
		task = APTR.Null;
		signalMask = 0;
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
			out var sidecar) || sidecar.LifecycleState !=
			MuiNativeMuiObjectRecord.StateLive ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) == 0 ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectDisposing) != 0)
			return false;
		task = sidecar.InputSignalTask;
		signalMask = sidecar.InputSignalMask;
		return true;
	}
}

// The public MUI_InputHandlerNode remains caller-owned. This sidecar queue
// stores typed registration records so lifetime, signal aggregation, and
// callback reentrancy are independent of caller MinNode links.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeInputHandlerEntryRecord
{
	internal const uint Size = 20;
	internal APTR Next;
	internal APTR Handler;
	internal uint Sequence;
	internal APTR TimerRequest;
	// This final, in-record header is also the packet passed to DoMethod.
	internal MuiApplicationMethodHeaderMessage Method;
}

internal static class MuiNativeInputHandlerEntryCodec
{
	internal static bool TryRead<TMemory>(ref TMemory memory, APTR address,
		out MuiNativeInputHandlerEntryRecord value)
		where TMemory : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeInputHandlerEntryRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var next) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var handler) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Sequence) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var timerRequest) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Method.MethodId) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Next = APTR.FromPointer(next);
		value.Handler = APTR.FromPointer(handler);
		value.TimerRequest = APTR.FromPointer(timerRequest);
		return true;
	}

	internal static bool Write<TMemory>(ref TMemory memory, APTR address,
		MuiNativeInputHandlerEntryRecord value)
		where TMemory : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeInputHandlerEntryRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Next.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Handler.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Sequence) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.TimerRequest.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Method.MethodId) &&
		MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryGetPayload<TMemory>(ref TMemory memory,
		APTR address, uint payloadSize, out APTR payload)
		where TMemory : struct, IMuiGuestMemory
	{
		payload = APTR.Null;
		if (payloadSize != MuiApplicationMethodHeaderMessage.Size ||
			!MuiGuestStructCursor.TryCreate(ref memory, address,
				MuiNativeInputHandlerEntryRecord.Size,
				out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiApplicationMethodHeaderMessage.Size, out _) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiApplicationMethodHeaderMessage.Size, out _) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiApplicationMethodHeaderMessage.Size, out _) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiApplicationMethodHeaderMessage.Size, out _) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiApplicationMethodHeaderMessage.Size, out payload) ||
			!MuiGuestStructCursor.IsComplete(cursor))
		{
			payload = APTR.Null;
			return false;
		}
		return true;
	}
}

internal static class MuiNativeApplicationInputHandlerQueue
{
	internal static bool Validate<TMemory>(ref TMemory memory, APTR head,
		uint generation)
		where TMemory : struct, IMuiGuestMemory
		=> Validate(ref memory, head, generation, APTR.Null);

	internal static bool Validate<TMemory>(ref TMemory memory, APTR head,
		uint generation, APTR timerPort)
		where TMemory : struct, IMuiGuestMemory
	{
		if (timerPort.IsNotNull)
		{
			if (!ExecMsgPortCodec.IsMapped(ref memory, timerPort)) return false;
			var port = ExecMsgPortCodec.Read(ref memory, timerPort);
			if (port.SignalTask.IsNull || port.SignalBit >= 32) return false;
		}
		var current = head;
		var hasPrevious = false;
		var previousSequence = 0u;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativeInputHandlerEntryCodec.TryRead(ref memory, current,
				out var node) || node.Handler.IsNull || node.Next == current ||
				node.Sequence == 0 ||
				!MuiInputHandlerCodec.TryRead(ref memory, node.Handler,
					out var handler) || handler.Object.IsNull ||
				handler.Method != node.Method.MethodId ||
				!MuiNativeInputHandlerEntryCodec.TryGetPayload(ref memory, current,
					MuiApplicationMethodHeaderMessage.Size, out var methodMessage) ||
				!MuiApplicationMethodHeaderCodec.TryReadValue(ref memory,
				methodMessage, out var methodId) ||
				methodId != node.Method.MethodId ||
				node.Sequence > generation || hasPrevious &&
					node.Sequence >= previousSequence)
				return false;
			if (handler.Flags == 0)
			{
				if (node.TimerRequest.IsNotNull) return false;
			}
			else
			{
				if (!MuiApplicationTimerInputHandlerCore.TryGetInterval(
					handler.Flags, handler.Value, out _) || timerPort.IsNull ||
					!ExecMsgPortCodec.IsMapped(ref memory, timerPort) ||
					!TimerRequestCodec.IsMapped(ref memory,
						node.TimerRequest)) return false;
				var timerRequest = TimerRequestCodec.Read(ref memory,
					node.TimerRequest);
				if (timerRequest.Request.Device.IsNull ||
					timerRequest.Request.Message.ReplyPort != timerPort) return false;
			}
			previousSequence = node.Sequence;
			hasPrevious = true;
			current = node.Next;
		}
		return current.IsNull;
	}

	internal static bool TryReadSignalMask<TMemory>(ref TMemory memory,
		APTR head, uint generation, out uint signalMask)
		where TMemory : struct, IMuiGuestMemory
		=> TryReadSignalMask(ref memory, head, generation, APTR.Null,
			APTR.Null, out signalMask);

	internal static bool TryReadSignalMask<TMemory>(ref TMemory memory,
		APTR head, uint generation, APTR timerPort, APTR task,
		out uint signalMask)
		where TMemory : struct, IMuiGuestMemory
	{
		signalMask = 0;
		if (!Validate(ref memory, head, generation, timerPort)) return false;
		var current = head;
		uint result = 0;
		var hasTimer = false;
		while (current.IsNotNull)
		{
			if (!MuiNativeInputHandlerEntryCodec.TryRead(ref memory, current,
				out var node) || !MuiInputHandlerCodec.TryRead(ref memory,
				node.Handler, out var handler)) return false;
			if ((handler.Flags & MuiApplicationTimerInputHandlerFlags.Timer) != 0)
				hasTimer = true;
			else result |= handler.Value.Signals;
			current = node.Next;
		}
		if (hasTimer)
		{
			if (timerPort.IsNull || task.IsNull ||
				!ExecMsgPortCodec.IsMapped(ref memory, timerPort)) return false;
			var port = ExecMsgPortCodec.Read(ref memory, timerPort);
			if (port.SignalTask != task || port.SignalBit >= 32) return false;
			result |= 1u << port.SignalBit;
		}
		signalMask = result;
		return true;
	}

	private static bool ContainsHandler<TMemory>(ref TMemory memory, APTR head,
		APTR handlerAddress)
		where TMemory : struct, IMuiGuestMemory
	{
		var current = head;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativeInputHandlerEntryCodec.TryRead(ref memory, current,
				out var node)) return true;
			if (node.Handler == handlerAddress) return true;
			current = node.Next;
		}
		return current.IsNotNull;
	}

	private static bool IsTimerPortForTask<TMemory>(ref TMemory memory,
		APTR portAddress, APTR task)
		where TMemory : struct, IMuiGuestMemory
	{
		if (task.IsNull || !ExecMsgPortCodec.IsMapped(ref memory, portAddress))
			return false;
		var port = ExecMsgPortCodec.Read(ref memory, portAddress);
		return port.SignalTask == task && port.SignalBit < 32;
	}

	private static bool TryEnsureTimerPort(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR application, out APTR timerPort)
	{
		timerPort = APTR.Null;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
			ownerRoot, application, out var binding) ||
			!MuiNativeMuiObjectCodec.TryRead(ref memory, binding.Sidecar,
				out var sidecar) || sidecar.Object != application ||
			sidecar.OwnerRoot != ownerRoot ||
			!MuiNativeApplicationInputHandlerQueue.Validate(ref memory,
				sidecar.InputHandlers, sidecar.InputHandlerGeneration,
				sidecar.InputTimerPort)) return false;
		var task = Exec.FindTask(CString.FromPointer(0));
		if (sidecar.InputSignalTask.IsNotNull &&
			sidecar.InputSignalTask != task) return false;
		if (sidecar.InputTimerPort.IsNotNull)
		{
			if (!IsTimerPortForTask(ref memory, sidecar.InputTimerPort, task))
				return false;
			timerPort = sidecar.InputTimerPort;
			return true;
		}

		if (!MuiNativeApplicationTimer.TryCreatePort(out var candidate))
			return false;
		Exec.Forbid();
		var attached = false;
		if (MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
			ownerRoot, application, out binding) &&
			MuiNativeMuiObjectCodec.TryRead(ref memory, binding.Sidecar,
				out sidecar) && sidecar.Object == application &&
			sidecar.OwnerRoot == ownerRoot &&
			MuiNativeApplicationInputHandlerQueue.Validate(ref memory,
				sidecar.InputHandlers, sidecar.InputHandlerGeneration,
				sidecar.InputTimerPort))
		{
			if (sidecar.InputTimerPort.IsNull)
			{
				sidecar.InputTimerPort = candidate;
				if (MuiNativeMuiObjectCodec.Write(ref memory, binding.Sidecar,
					sidecar))
				{
					timerPort = candidate;
					attached = true;
				}
			}
			else if (IsTimerPortForTask(ref memory, sidecar.InputTimerPort, task))
			{
				timerPort = sidecar.InputTimerPort;
				attached = true;
			}
		}
		Exec.Permit();
		if (timerPort != candidate) MuiNativeApplicationTimer.ReleasePort(candidate);
		return attached && timerPort.IsNotNull;
	}

	private static bool TryDetachEmptyTimerPort(
		ref MuiNativeClassPlatform platform, APTR publicObjects, APTR ownerRoot,
		APTR application, out APTR detachedPort)
	{
		detachedPort = APTR.Null;
		Exec.Forbid();
		var memory = default(MuiNativeClassMemory);
		if (MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
			ownerRoot, application, out var binding) &&
			MuiNativeMuiObjectCodec.TryRead(ref memory, binding.Sidecar,
				out var sidecar) && sidecar.Object == application &&
			sidecar.OwnerRoot == ownerRoot && sidecar.InputTimerPort.IsNotNull &&
			MuiNativeApplicationInputHandlerQueue.Validate(ref memory,
				sidecar.InputHandlers, sidecar.InputHandlerGeneration,
				sidecar.InputTimerPort))
		{
			var hasTimer = false;
			var current = sidecar.InputHandlers;
			while (current.IsNotNull)
			{
				if (!MuiNativeInputHandlerEntryCodec.TryRead(ref memory, current,
					out var node) || !MuiInputHandlerCodec.TryRead(ref memory,
						node.Handler, out var handler)) break;
				if ((handler.Flags & MuiApplicationTimerInputHandlerFlags.Timer) != 0)
				{
					hasTimer = true;
					break;
				}
				current = node.Next;
			}
			if (!hasTimer)
			{
				detachedPort = sidecar.InputTimerPort;
				sidecar.InputTimerPort = APTR.Null;
				if (!MuiNativeMuiObjectCodec.Write(ref memory, binding.Sidecar,
					sidecar)) detachedPort = APTR.Null;
			}
		}
		Exec.Permit();
		return detachedPort.IsNotNull;
	}

	internal static bool TryAdd(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR application, APTR handlerAddress)
	{
		if (handlerAddress.IsNull || !platform.IsMapped(handlerAddress,
			MuiInputHandlerRecord.Size) ||
			!MuiInputHandlerCodec.TryRead(ref platform, handlerAddress,
				out var requested) || requested.Object.IsNull ||
			!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, requested.Object, out _)) return false;
		var timerHandler = (requested.Flags &
			MuiApplicationTimerInputHandlerFlags.Timer) != 0;
		if (requested.Flags != 0 && !timerHandler || timerHandler &&
			!MuiApplicationTimerInputHandlerCore.TryGetInterval(requested.Flags,
				requested.Value, out _)) return false;

		var allocation = platform.Allocate(MuiNativeInputHandlerEntryRecord.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (allocation.IsNull) return false;
		var timerPort = APTR.Null;
		var timerRequest = APTR.Null;
		if (timerHandler && (!TryEnsureTimerPort(ref platform, publicObjects,
			ownerRoot, application, out timerPort) ||
			!MuiNativeApplicationTimer.TryCreateRequest(timerPort,
				requested.Flags, requested.Value, out timerRequest)))
		{
			platform.Clear(allocation, MuiNativeInputHandlerEntryRecord.Size);
			platform.Free(allocation, MuiNativeInputHandlerEntryRecord.Size);
			if (TryDetachEmptyTimerPort(ref platform, publicObjects, ownerRoot,
				application, out var unusedPort))
				MuiNativeApplicationTimer.ReleasePort(unusedPort);
			return false;
		}

		Exec.Forbid();
		var memory = default(MuiNativeClassMemory);
		var added = false;
		if (MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
			ownerRoot, application, out var applicationBinding) &&
			MuiNativeMuiObjectCodec.TryRead(ref memory,
				applicationBinding.Sidecar, out var sidecar) &&
			sidecar.Object == application &&
			sidecar.OwnerRoot == ownerRoot &&
			(!timerHandler || sidecar.InputTimerPort == timerPort) &&
			MuiNativeApplicationInputHandlerQueue.Validate(ref memory,
				sidecar.InputHandlers, sidecar.InputHandlerGeneration,
				sidecar.InputTimerPort) &&
			!MuiNativeApplicationInputHandlerQueue.ContainsHandler(ref memory,
				sidecar.InputHandlers, handlerAddress) &&
			MuiInputHandlerCodec.TryRead(ref memory, handlerAddress,
				out var handler) && handler.Object == requested.Object &&
			handler.Method == requested.Method &&
			handler.Flags == requested.Flags &&
			handler.Value.Raw == requested.Value.Raw &&
			MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, handler.Object, out _))
		{
			if (sidecar.InputHandlerGeneration != uint.MaxValue)
			{
				var sequence = sidecar.InputHandlerGeneration + 1u;
				var node = default(MuiNativeInputHandlerEntryRecord);
				node.Next = sidecar.InputHandlers;
				node.Handler = handlerAddress;
				node.Sequence = sequence;
				node.TimerRequest = timerRequest;
				node.Method.MethodId = handler.Method;
				var packetPrepared = MuiNativeInputHandlerEntryCodec.Write(
					ref memory, allocation, node);
				var handlerPrepared = true;
				if (packetPrepared && timerHandler)
				{
					var timerValue = handler.Value.Timer;
					timerValue.Current = 0;
					handler.Value.Timer = timerValue;
					handlerPrepared = MuiInputHandlerCodec.Write(ref memory,
						handlerAddress, handler);
				}
				if (packetPrepared && handlerPrepared)
				{
					sidecar.InputHandlerGeneration = sequence;
					sidecar.InputHandlers = allocation;
					added = MuiNativeMuiObjectCodec.Write(ref memory,
						applicationBinding.Sidecar, sidecar);
					if (!added && timerHandler)
					{
						handler.Value = requested.Value;
						MuiInputHandlerCodec.Write(ref memory, handlerAddress,
							handler);
					}
				}
			}
		}
		Exec.Permit();
		if (!added)
		{
			if (timerRequest.IsNotNull)
				MuiNativeApplicationTimer.ReleaseRequest(timerRequest);
			platform.Clear(allocation, MuiNativeInputHandlerEntryRecord.Size);
			platform.Free(allocation, MuiNativeInputHandlerEntryRecord.Size);
			if (timerHandler && TryDetachEmptyTimerPort(ref platform,
				publicObjects, ownerRoot, application, out var unusedPort))
				MuiNativeApplicationTimer.ReleasePort(unusedPort);
		}
		return added;
	}

	internal static bool TryRemove(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR application, APTR handlerAddress)
	{
		if (handlerAddress.IsNull) return false;
		var removed = APTR.Null;
		var removedTimerRequest = APTR.Null;
		Exec.Forbid();
		var memory = default(MuiNativeClassMemory);
		if (MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
			ownerRoot, application, out var applicationBinding) &&
			MuiNativeMuiObjectCodec.TryRead(ref memory,
				applicationBinding.Sidecar, out var sidecar) &&
			sidecar.Object == application && sidecar.OwnerRoot == ownerRoot &&
			MuiNativeApplicationInputHandlerQueue.Validate(ref memory,
				sidecar.InputHandlers, sidecar.InputHandlerGeneration,
				sidecar.InputTimerPort))
		{
			var current = sidecar.InputHandlers;
			var previous = APTR.Null;
			while (current.IsNotNull)
			{
				if (!MuiNativeInputHandlerEntryCodec.TryRead(ref memory, current,
					out var node)) break;
				if (node.Handler == handlerAddress)
				{
					if (MuiInputHandlerCodec.TryRead(ref memory, handlerAddress,
						out var handler) && (handler.Flags &
						MuiApplicationTimerInputHandlerFlags.Timer) != 0)
						removedTimerRequest = node.TimerRequest;
					var changed = false;
					if (previous.IsNull)
					{
						var nextSidecar = sidecar;
						nextSidecar.InputHandlers = node.Next;
						changed = MuiNativeMuiObjectCodec.Write(ref memory,
							applicationBinding.Sidecar, nextSidecar);
					}
					else if (MuiNativeInputHandlerEntryCodec.TryRead(ref memory,
						previous, out var previousNode))
					{
						previousNode.Next = node.Next;
						changed = MuiNativeInputHandlerEntryCodec.Write(ref memory,
							previous, previousNode);
					}
					if (changed) removed = current;
					break;
				}
				previous = current;
				current = node.Next;
			}
		}
		Exec.Permit();
		if (removed.IsNull) return false;
		MuiNativeApplicationTimer.ReleaseRequest(removedTimerRequest);
		platform.Clear(removed, MuiNativeInputHandlerEntryRecord.Size);
		platform.Free(removed, MuiNativeInputHandlerEntryRecord.Size);
		return true;
	}

	internal static uint Dispatch(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR application,
		APTR sidecarAddress, uint receivedSignals)
	{
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
			ownerRoot, application, out var applicationBinding) ||
			applicationBinding.Sidecar != sidecarAddress ||
			!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
				out var sidecar) ||
			!MuiNativeApplicationInputHandlerQueue.TryGetBounds(ref memory,
				sidecar.InputHandlers, sidecar.InputHandlerGeneration,
				sidecar.InputTimerPort,
				out var lastSequence))
			return 0;

		var afterSequence = 0u;
		uint dispatched = 0;
		for (var operation = 0u; operation < MuiHeadlessLayout.MaximumTraversal;
			operation++)
		{
			if (!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, application, out applicationBinding) ||
				applicationBinding.Sidecar != sidecarAddress ||
				!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
					out sidecar)) break;
			if (!MuiNativeApplicationInputHandlerQueue.TryFindNext(ref memory,
				sidecar.InputHandlers, afterSequence, lastSequence,
				out var entryAddress, out var entry)) break;
			afterSequence = entry.Sequence;
			if (!MuiInputHandlerCodec.TryRead(ref memory, entry.Handler,
				out var handler)) continue;
			var timerHandler = (handler.Flags &
				MuiApplicationTimerInputHandlerFlags.Timer) != 0;
			if (timerHandler)
			{
				var request = entry.TimerRequest;
				if (request.IsNull || Exec.CheckIO(request).IsNull) continue;
				var ioError = Exec.WaitIO(request);
				var timerValue = handler.Value.Timer;
				timerValue.Current = 0;
				handler.Value.Timer = timerValue;
				MuiInputHandlerCodec.Write(ref memory, entry.Handler, handler);
				// Re-arm before calling out. If the handler removes itself from its
				// method, removal sees a live request and can retire it safely.
				if (!MuiNativeApplicationTimer.Schedule(request, handler.Flags,
					handler.Value)) continue;
				if (ioError != 0) continue;
			}
			else if ((handler.Value.Signals & receivedSignals) == 0) continue;
			if (!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, handler.Object, out _) ||
				!MuiNativeInputHandlerEntryCodec.TryGetPayload(ref platform,
					entryAddress, MuiApplicationMethodHeaderMessage.Size,
					out var methodMessage)) continue;
			platform.DoMethod(handler.Object, methodMessage);
			dispatched++;
		}
		return dispatched;
	}

	internal static void FreeValidated<TMemory>(ref TMemory memory, APTR head)
		where TMemory : struct, IMuiGuestMemory
		=> FreeValidated(ref memory, head, APTR.Null);

	internal static void FreeValidated<TMemory>(ref TMemory memory, APTR head,
		APTR timerPort)
		where TMemory : struct, IMuiGuestMemory
	{
		var current = head;
		while (current.IsNotNull)
		{
			if (!MuiNativeInputHandlerEntryCodec.TryRead(ref memory, current,
				out var node)) return;
			if (MuiInputHandlerCodec.TryRead(ref memory, node.Handler,
				out var handler) && (handler.Flags &
				MuiApplicationTimerInputHandlerFlags.Timer) != 0)
				MuiNativeApplicationTimer.ReleaseRequest(
					node.TimerRequest);
			memory.Clear(current, MuiNativeInputHandlerEntryRecord.Size);
			Exec.FreeMem(current, MuiNativeInputHandlerEntryRecord.Size);
			current = node.Next;
		}
		MuiNativeApplicationTimer.ReleasePort(timerPort);
	}

	internal static bool TryGetBounds<TMemory>(ref TMemory memory, APTR head,
		uint generation, out uint lastSequence)
		where TMemory : struct, IMuiGuestMemory
		=> TryGetBounds(ref memory, head, generation, APTR.Null,
			out lastSequence);

	internal static bool TryGetBounds<TMemory>(ref TMemory memory, APTR head,
		uint generation, APTR timerPort, out uint lastSequence)
		where TMemory : struct, IMuiGuestMemory
	{
		lastSequence = 0;
		if (!Validate(ref memory, head, generation, timerPort)) return false;
		var current = head;
		while (current.IsNotNull)
		{
			if (!MuiNativeInputHandlerEntryCodec.TryRead(ref memory, current,
				out var node)) return false;
			if (node.Sequence > lastSequence) lastSequence = node.Sequence;
			current = node.Next;
		}
		return true;
	}

	internal static bool TryFindNext<TMemory>(ref TMemory memory, APTR head,
		uint afterSequence, uint lastSequence, out APTR selectedAddress,
		out MuiNativeInputHandlerEntryRecord selected)
		where TMemory : struct, IMuiGuestMemory
	{
		selectedAddress = APTR.Null;
		selected = default;
		var current = head;
		while (current.IsNotNull)
		{
			if (!MuiNativeInputHandlerEntryCodec.TryRead(ref memory, current,
				out var node)) return false;
			if (node.Sequence > afterSequence && node.Sequence <= lastSequence &&
				(selectedAddress.IsNull || node.Sequence < selected.Sequence))
			{
				selectedAddress = current;
				selected = node;
			}
			current = node.Next;
		}
		return selectedAddress.IsNotNull;
	}
}

internal static class MuiNativeApplicationWindowSignals
{
	// MorphOS NewInput combines registered input-handler signals with open
	// application-window message-port signals. Cache only Window pointers
	// observed through MUIA_Window_Window; read Window and MsgPort through SDK
	// structs and keep window aggregation scoped to the application parent links.
	internal static bool TryBuildMask<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR application, APTR task,
		out uint signalMask)
		where TMemory : struct, IMuiGuestMemory
	{
		signalMask = 0;
		if (publicObjects.IsNull || ownerRoot.IsNull || task.IsNull ||
			application.IsNull ||
			!MuiNativePublicObjectRegistryCodec.TryRead(ref memory, publicObjects,
				out var registry)) return false;
		var result = 0u;
		var current = registry.Head;
		var foundApplication = false;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, current,
				out var binding) || binding.Signature !=
				MuiNativePublicObjectBinding.Magic || binding.OwnerRoot != ownerRoot)
				return false;
			if (binding.DisposeState != MuiNativePublicObjectBinding.StateLive &&
				binding.DisposeState !=
					MuiNativePublicObjectBinding.StateNativeDisposed &&
				binding.DisposeState !=
					MuiNativePublicObjectBinding.StateLeaseReleased) return false;
			if (binding.DisposeState ==
				MuiNativePublicObjectBinding.StateLive)
			{
				if (binding.Object.IsNull || binding.Sidecar.IsNull ||
					!MuiNativeMuiObjectCodec.TryRead(ref memory, binding.Sidecar,
						out var sidecar) || sidecar.Object != binding.Object ||
					sidecar.Class != binding.Class || sidecar.OwnerRoot != ownerRoot ||
					sidecar.Parent != binding.Parent ||
					sidecar.LifecycleState != MuiNativeMuiObjectRecord.StateLive ||
					(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) == 0 ||
					(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectDisposing) != 0)
					return false;
				if (binding.Object == application)
				{
					if (foundApplication || !MuiNativeApplicationInputHandlerQueue
						.TryReadSignalMask(ref memory, sidecar.InputHandlers,
							sidecar.InputHandlerGeneration,
							sidecar.InputTimerPort, task,
							out var inputHandlerSignals)) return false;
					foundApplication = true;
					result |= inputHandlerSignals;
				}
				if (binding.Parent == application &&
					sidecar.NativeWindow.IsNotNull)
				{
					if (!memory.IsMapped(sidecar.NativeWindow, Window.Size))
						return false;
					var window = IntuitionScreenWindowGuestCodec.ReadWindow(ref memory,
						sidecar.NativeWindow);
					if (window.UserPort.IsNotNull)
					{
						if (!ExecMsgPortCodec.IsMapped(ref memory, window.UserPort))
							return false;
						var port = ExecMsgPortCodec.Read(ref memory, window.UserPort);
						if (port.SignalTask == task && port.SignalBit < 32)
							result |= 1u << port.SignalBit;
					}
				}
			}
			current = binding.Next;
		}
		if (current.IsNotNull || !foundApplication) return false;
		signalMask = result;
		return true;
	}

	// InputBuffered is a poll, not a wait: sample the current task's pending
	// signal ULONG and keep only bits owned by this application's registered
	// windows and input handlers. The signal value is carried as a named scalar;
	// guest Window/MsgPort/InputHandler records are decoded by TryBuildMask.
	internal static bool TryGetPendingInputSignals<TMemory>(
		ref TMemory memory, APTR publicObjects, APTR ownerRoot,
		APTR application, APTR task, uint pendingSignals,
		out uint inputSignals)
		where TMemory : struct, IMuiGuestMemory
	{
		inputSignals = 0;
		if (!TryBuildMask(ref memory, publicObjects, ownerRoot, application,
			task, out var registeredSignals)) return false;
		inputSignals = pendingSignals & registeredSignals;
		return true;
	}
}

internal static class MuiNativeApplicationInputCore
{
	internal static bool ReturnId(ref MuiNativeClassPlatform platform,
		APTR sidecarAddress, uint returnId)
	{
		var allocation = platform.Allocate(MuiNativeReturnIdRecord.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (allocation.IsNull) return false;

		var wakeTask = APTR.Null;
		var wakeMask = 0u;
		Exec.Forbid();
		var memory = default(MuiNativeClassMemory);
		var queued = MuiNativeReturnIdQueue.TryEnqueue(ref memory,
			sidecarAddress, allocation, returnId);
		if (queued && MuiNativeApplicationInputWaitState.TryGet(ref memory,
			sidecarAddress, out wakeTask, out wakeMask) &&
			wakeTask.IsNotNull && wakeMask != 0)
		{
			var currentTask = Exec.FindTask(CString.FromPointer(0));
			if (currentTask != wakeTask) Exec.Signal(wakeTask, wakeMask);
		}
		Exec.Permit();
		if (!queued)
		{
			platform.Clear(allocation, MuiNativeReturnIdRecord.Size);
			platform.Free(allocation, MuiNativeReturnIdRecord.Size);
		}
		return queued;
	}

	internal static bool TryTakeReturnId(ref MuiNativeClassPlatform platform,
		APTR sidecarAddress, out bool hasValue, out uint returnId)
	{
		hasValue = false;
		returnId = 0;
		Exec.Forbid();
		var memory = default(MuiNativeClassMemory);
		var valid = MuiNativeReturnIdQueue.TryDequeue(ref memory,
			sidecarAddress, out hasValue, out returnId, out var removedRecord);
		Exec.Permit();
		if (valid && hasValue)
		{
			platform.Clear(removedRecord, MuiNativeReturnIdRecord.Size);
			platform.Free(removedRecord, MuiNativeReturnIdRecord.Size);
		}
		return valid;
	}

	internal static bool TryProjectAfterClassCallback(
		ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR application, APTR sidecarAddress, APTR message,
		uint method, uint receivedSignals,
		ref uint result)
	{
		if (method == MuiApplicationDispatcher.ApplicationInputBufferedMethod)
		{
			if (!MuiApplicationInputBufferedMessageCodec.TryRead(ref platform,
				message, out var buffered) || buffered.MethodId != method)
				return false;
			var bufferedTask = Exec.FindTask(CString.FromPointer(0));
			var signalMemory = default(MuiNativeClassMemory);
			if (bufferedTask.IsNotNull &&
				MuiNativeApplicationWindowSignals.TryGetPendingInputSignals(
					ref signalMemory, publicObjects, ownerRoot, application,
					bufferedTask, Exec.SetSignal(0, 0), out var inputSignals))
			{
				MuiNativeApplicationWindowEvents.Dispatch(ref platform,
					publicObjects, ownerRoot, application, bufferedTask,
					inputSignals, newInput: true);
				MuiNativeApplicationInputHandlerQueue.Dispatch(ref platform,
					publicObjects, ownerRoot, application, sidecarAddress,
					inputSignals);
			}
			MuiNativeApplicationPushMethodDispatch.DispatchNext(ref platform,
				publicObjects, ownerRoot, application);
			// InputBuffered never consumes a queued ReturnID or replaces the
			// application's ordinary Input wait task/mask.
			result = 0;
			return true;
		}
		if ((method != MuiApplicationDispatcher.ApplicationInputMethod &&
			method != MuiApplicationDispatcher.ApplicationNewInputMethod) ||
			!MuiApplicationInputMessageCodec.TryRead(ref platform, message,
				out var input) || input.MethodId != method) return false;

		var signalMask = 0u;
		var currentTask = Exec.FindTask(CString.FromPointer(0));
		if (input.SignalStorage.IsNotNull &&
			MuiApplicationWindowSignalStorageCodec.TryRead(ref platform,
				input.SignalStorage, out var signalStorage))
		{
			signalMask = signalStorage.Signals;
			MuiNativeApplicationWindowEvents.Dispatch(ref platform,
				publicObjects, ownerRoot, application, currentTask,
				receivedSignals,
				method == MuiApplicationDispatcher.ApplicationNewInputMethod);
			MuiNativeApplicationInputHandlerQueue.Dispatch(ref platform,
				publicObjects, ownerRoot, application, sidecarAddress,
				receivedSignals);
			var signalMemory = default(MuiNativeClassMemory);
			if (MuiNativeApplicationWindowSignals.TryBuildMask(ref signalMemory,
				publicObjects, ownerRoot, application, currentTask,
				out var windowSignals))
			{
				signalMask |= windowSignals;
				signalStorage.Signals = signalMask;
				MuiApplicationWindowSignalStorageCodec.Write(ref platform,
					input.SignalStorage, signalStorage);
			}
		}
		Exec.Forbid();
		var memory = default(MuiNativeClassMemory);
		MuiNativeApplicationInputWaitState.TrySet(ref memory, sidecarAddress,
			currentTask, signalMask);
		Exec.Permit();

		if (!TryTakeReturnId(ref platform, sidecarAddress, out var hasValue,
			out var returnId))
		{
			result = 0;
			return true;
		}
		if (!hasValue) return false;
		// Keep the class callback's signal output intact. It is the wait mask
		// the caller needs after processing this input packet, even when a queued
		// ReturnID takes precedence as the method result.
		result = returnId;
		return true;
	}

	internal static bool TryDispatch(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj, APTR message,
		uint method, out uint result)
	{
		result = 0;
		if (!MuiNativePublicObjectCore.TryFindLive(ref platform,
			publicObjects, ownerRoot, obj, out var binding)) return false;
		if (method == MuiApplicationPushMethodMessage.Id || method ==
			MuiApplicationUnpushMethodMessage.Id)
			return MuiNativeApplicationPushMethodDispatch.TryDispatch(ref platform,
				publicObjects, ownerRoot, obj, message, method, out result);
		if (method == MuiApplicationDispatcher.WindowAddEventHandlerMethod ||
			method == MuiApplicationDispatcher.WindowRemoveEventHandlerMethod)
		{
			if (!MuiWindowEventHandlerMessageCodec.TryRead(ref platform, message,
				out var eventHandler) || eventHandler.MethodId != method) return false;
			var handler = APTR.FromPointer(eventHandler.Handler);
			var sidecarAddress = binding.Sidecar;
			Exec.Forbid();
			var memory = default(MuiNativeClassMemory);
			var changed = MuiNativePublicObjectCore.TryFindLive(ref platform,
				publicObjects, ownerRoot, obj, out var currentBinding) &&
				currentBinding.Sidecar == sidecarAddress &&
				MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
					out var currentSidecar) && currentSidecar.Object == obj &&
				currentSidecar.Class == currentBinding.Class &&
				currentSidecar.OwnerRoot == ownerRoot &&
				currentSidecar.Parent == currentBinding.Parent &&
				currentSidecar.LifecycleState ==
					MuiNativeMuiObjectRecord.StateLive &&
				(currentSidecar.Flags &
					MuiNativeMuiObjectRecord.ObjectInitialized) != 0 &&
				(currentSidecar.Flags &
					MuiNativeMuiObjectRecord.ObjectDisposing) == 0 &&
				(method == MuiApplicationDispatcher.WindowAddEventHandlerMethod
					? MuiNativeWindowEventHandlerQueue.TryAdd(ref memory,
						sidecarAddress, handler)
					: MuiNativeWindowEventHandlerQueue.TryRemove(ref memory,
						sidecarAddress, handler));
			Exec.Permit();
			result = changed ? 1u : 0u;
			return true;
		}
		if (method == MuiApplicationDispatcher.AddInputHandlerMethod ||
			method == MuiApplicationDispatcher.RemoveInputHandlerMethod)
		{
			if (!MuiApplicationInputPacketCodec.TryReadInputHandler(ref platform,
				message, method, out var inputHandler)) return false;
			var handler = APTR.FromPointer(inputHandler.Handler);
			result = method == MuiApplicationDispatcher.AddInputHandlerMethod
				? (MuiNativeApplicationInputHandlerQueue.TryAdd(ref platform,
					publicObjects, ownerRoot, obj, handler) ? 1u : 0u)
				: (MuiNativeApplicationInputHandlerQueue.TryRemove(ref platform,
					publicObjects, ownerRoot, obj, handler) ? 1u : 0u);
			return true;
		}
		if (method != MuiApplicationDispatcher.ApplicationReturnIdMethod)
			return false;

		if (!MuiApplicationReturnIdMessageCodec.TryRead(ref platform,
			message, out var packet) || packet.MethodId != method) return false;
		result = ReturnId(ref platform, binding.Sidecar,
			packet.ReturnId) ? 1u : 0u;
		return true;
	}
}
