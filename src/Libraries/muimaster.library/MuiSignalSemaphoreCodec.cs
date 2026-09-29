/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Compatibility adapter for the SDK-owned packed SignalSemaphore. The pinned
// SDK has no complete public semaphore codec. Lifecycle logic uses the named
// SDK struct; declaration-order wire traversal remains confined here.
internal static class MuiSignalSemaphoreCodec
{
	internal static bool IsIdle(APTR address, SignalSemaphore value)
	{
		if (value.Link.Successor.IsNotNull || value.Link.Predecessor.IsNotNull ||
			value.Link.Name.Raw != 0 || value.Link.Type != 0 || value.Link.Priority != 0 ||
			value.Owner.IsNotNull || value.NestCount != 0 || value.QueueCount != 0 ||
			value.MultipleLink.Waiter.IsNotNull || value.MultipleLink.Link.Successor.IsNotNull ||
			value.MultipleLink.Link.Predecessor.IsNotNull || value.WaitQueue.Tail.IsNotNull) return false;
		// Zero storage is valid before native InitSemaphore. Afterwards the
		// private wait queue must contain precisely its own empty sentinels.
		if (value.WaitQueue.Head.IsNull && value.WaitQueue.TailPred.IsNull) return true;
		var queue = address.Raw + Node.Size + sizeof(short);
		return value.WaitQueue.Head.Raw == queue + sizeof(uint) && value.WaitQueue.TailPred.Raw == queue;
	}

	internal static bool TryRead<T>(ref T memory, APTR address, out SignalSemaphore value)
		where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address, SignalSemaphore.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor, Node.Size, out var node) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor, out var nesting) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var head) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var tail) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var tailPred) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var successor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var predecessor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var waiter) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var owner) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor, out var queued) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Link = ExecNodeCodec.Read(ref memory, node);
		value.NestCount = unchecked((short)nesting);
		value.WaitQueue.Head = APTR.FromPointer(head);
		value.WaitQueue.Tail = APTR.FromPointer(tail);
		value.WaitQueue.TailPred = APTR.FromPointer(tailPred);
		value.MultipleLink.Link.Successor = APTR.FromPointer(successor);
		value.MultipleLink.Link.Predecessor = APTR.FromPointer(predecessor);
		value.MultipleLink.Waiter = APTR.FromPointer(waiter);
		value.Owner = APTR.FromPointer(owner);
		value.QueueCount = unchecked((short)queued);
		return true;
	}

	// Whole writes are for unpublished initialization and deterministic tests.
	// Live semaphore state belongs to Exec, never an owner-control snapshot.
	internal static bool Write<T>(ref T memory, APTR address, SignalSemaphore value)
		where T : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref memory, address, SignalSemaphore.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor, Node.Size, out var node)) return false;
		ExecNodeCodec.Write(ref memory, node, value.Link);
		return MuiGuestStructCursor.TryWriteUInt16(ref memory, ref cursor, unchecked((ushort)value.NestCount)) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.WaitQueue.Head.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.WaitQueue.Tail.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.WaitQueue.TailPred.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.MultipleLink.Link.Successor.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.MultipleLink.Link.Predecessor.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.MultipleLink.Waiter.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Owner.Raw) &&
			MuiGuestStructCursor.TryWriteUInt16(ref memory, ref cursor, unchecked((ushort)value.QueueCount)) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}
}
