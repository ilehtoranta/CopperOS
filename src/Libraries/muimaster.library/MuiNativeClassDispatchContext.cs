/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperSharp.Sdk.Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Guest-resident identity for one invocation of an application's custom-class
// dispatcher. Named fields keep this transient authority independent from the
// layout of IClass and the native object.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeClassDispatchFrameRecord
{
	internal const uint Size = 28;
	internal const uint SignatureValue = 0x4D444631; // "MDF1"
	internal uint Signature;
	internal APTR Next;
	internal APTR OwnerTask;
	internal APTR Class;
	internal APTR Object;
	internal APTR Message;
	internal uint Method;
}

internal static class MuiNativeClassDispatchFrameCodec
{
	internal static bool Write<T>(ref T memory, APTR address,
		MuiNativeClassDispatchFrameRecord value) where T : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeClassDispatchFrameRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Signature) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Next.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.OwnerTask.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Class.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Object.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Message.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Method) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<T>(ref T memory, APTR address,
		out MuiNativeClassDispatchFrameRecord value)
		where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeClassDispatchFrameRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Signature) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var next) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var task) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var cls) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var obj) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var message) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Method) || !MuiGuestStructCursor.IsComplete(cursor))
		{
			value = default;
			return false;
		}
		value.Next = APTR.FromPointer(next);
		value.OwnerTask = APTR.FromPointer(task);
		value.Class = APTR.FromPointer(cls);
		value.Object = APTR.FromPointer(obj);
		value.Message = APTR.FromPointer(message);
		return true;
	}
}

// This list is modified only inside Exec.Forbid sections by the native bridge.
// It is task-aware: a callback in one task cannot authorize MUI_Redraw in a
// different task merely because both overlap in the global callback count.
internal static class MuiNativeClassDispatchContextCore
{
	private const uint MaximumFrames = 65536;

	internal static bool TryPush<T>(ref T memory, APTR ownerAddress,
		APTR frameAddress, APTR task, APTR cls, APTR obj, APTR message,
		uint method)
		where T : struct, IMuiGuestMemory
	{
		if (frameAddress.IsNull || task.IsNull || cls.IsNull || obj.IsNull ||
			message.IsNull ||
			!Disjoint(frameAddress, MuiNativeClassDispatchFrameRecord.Size,
				ownerAddress, MuiNativeClassOwnerRecord.Size) ||
			!TryReadOwner(ref memory, ownerAddress, out var owner) ||
			owner.DispatchFramesPoisoned != 0 ||
			!TryValidateFrames(ref memory, ownerAddress,
				owner.ActiveDispatchFrames,
				out var existingFrameCount)) return false;
		var current = owner.ActiveDispatchFrames;
		for (var visited = 0u; current.IsNotNull && visited < existingFrameCount;
			visited++)
		{
			if (!Disjoint(current, MuiNativeClassDispatchFrameRecord.Size,
				frameAddress, MuiNativeClassDispatchFrameRecord.Size)) return false;
			if (!MuiNativeClassDispatchFrameCodec.TryRead(ref memory, current,
				out var frame)) return false;
			current = frame.Next;
		}
		var value = new MuiNativeClassDispatchFrameRecord
		{
			Signature = MuiNativeClassDispatchFrameRecord.SignatureValue,
			Next = owner.ActiveDispatchFrames,
			OwnerTask = task,
			Class = cls,
			Object = obj,
			Message = message,
			Method = method,
		};
		return MuiNativeClassDispatchFrameCodec.Write(ref memory, frameAddress,
			value) && MuiNativeClassOwnerCodec.TryWriteDispatchContext(ref memory,
			ownerAddress, frameAddress, 0);
	}

	internal static bool TryPop<T>(ref T memory, APTR ownerAddress,
		APTR frameAddress) where T : struct, IMuiGuestMemory
	{
		if (frameAddress.IsNull ||
			!TryReadOwner(ref memory, ownerAddress, out var owner) ||
			owner.DispatchFramesPoisoned != 0 ||
			!TryValidateFrames(ref memory, ownerAddress,
				owner.ActiveDispatchFrames, out _) ||
			!TryFindFrame(ref memory, owner.ActiveDispatchFrames, frameAddress,
				out var previousAddress, out var previous, out var value))
		{
			Poison(ref memory, ownerAddress);
			return false;
		}

		var newHead = owner.ActiveDispatchFrames;
		if (previousAddress.IsNull)
			newHead = value.Next;
		else
		{
			previous.Next = value.Next;
			if (!MuiNativeClassDispatchFrameCodec.Write(ref memory,
				previousAddress, previous))
			{
				Poison(ref memory, ownerAddress);
				return false;
			}
		}
		if (!MuiNativeClassOwnerCodec.TryWriteDispatchContext(ref memory,
			ownerAddress, newHead, 0))
		{
			Poison(ref memory, ownerAddress);
			return false;
		}
		return true;
	}

	internal static bool HasActiveFrameForTask<T>(ref T memory,
		APTR ownerAddress, APTR task) where T : struct, IMuiGuestMemory
	{
		if (task.IsNull || !TryReadOwner(ref memory, ownerAddress, out var owner) ||
			owner.DispatchFramesPoisoned != 0) return false;
		var found = false;
		var current = owner.ActiveDispatchFrames;
		for (var visited = 0u; current.IsNotNull && visited < MaximumFrames;
			visited++)
		{
			if (!Disjoint(current, MuiNativeClassDispatchFrameRecord.Size,
				ownerAddress, MuiNativeClassOwnerRecord.Size) ||
				!MuiNativeClassDispatchFrameCodec.TryRead(ref memory, current,
				out var frame) ||
				frame.Signature != MuiNativeClassDispatchFrameRecord.SignatureValue ||
				frame.OwnerTask.IsNull || frame.Class.IsNull ||
				frame.Object.IsNull || frame.Message.IsNull) return false;
			if (frame.OwnerTask == task) found = true;
			current = frame.Next;
		}
		return current.IsNull && found;
	}

	// Detect an active derived-class callback whose exact message is currently
	// being passed to its superclass. A fresh/reentrant method call is kept
	// independent by matching the message pointer as well as task/object/method.
	internal static bool TryHasSuperDispatchFrame<T>(ref T memory,
		APTR ownerAddress, APTR task, APTR cls, APTR obj, APTR message,
		uint method, out bool hasSuperDispatchFrame)
		where T : struct, IMuiGuestMemory
	{
		hasSuperDispatchFrame = false;
		if (task.IsNull || cls.IsNull || obj.IsNull || message.IsNull ||
			!TryReadOwner(ref memory, ownerAddress, out var owner) ||
			owner.DispatchFramesPoisoned != 0 ||
			!TryValidateFrames(ref memory, ownerAddress,
				owner.ActiveDispatchFrames, out var frameCount)) return false;
		var current = owner.ActiveDispatchFrames;
		for (var visited = 0u; current.IsNotNull && visited < frameCount;
			visited++)
		{
			if (!MuiNativeClassDispatchFrameCodec.TryRead(ref memory, current,
				out var frame)) return false;
			if (frame.OwnerTask == task && frame.Object == obj &&
				frame.Message == message && frame.Method == method &&
				frame.Class != cls)
			{
				if (!TryIsSuperClass(ref memory, frame.Class, cls,
					out var isSuperClass)) return false;
				if (isSuperClass)
				{
					hasSuperDispatchFrame = true;
					return true;
				}
			}
			current = frame.Next;
		}
		return current.IsNull;
	}

	private static bool TryReadOwner<T>(ref T memory, APTR ownerAddress,
		out MuiNativeClassOwnerRecord owner) where T : struct, IMuiGuestMemory
	{
		owner = default;
		return !ownerAddress.IsNull &&
			MuiNativeClassOwnerCodec.TryRead(ref memory, ownerAddress, out owner) &&
			owner.Magic == MuiNativeClassOwnerCore.MagicValue &&
			owner.Version == MuiNativeClassOwnerCore.Version &&
			owner.Context.OwnerRoot.IsNotNull && owner.RegistryGeneration != 0 &&
			owner.DispatchFramesPoisoned <= 1;
	}

	private static bool TryValidateFrames<T>(ref T memory, APTR ownerAddress,
		APTR head,
		out uint count) where T : struct, IMuiGuestMemory
	{
		count = 0;
		var current = head;
		while (current.IsNotNull && count < MaximumFrames)
		{
			if (!Disjoint(current, MuiNativeClassDispatchFrameRecord.Size,
				ownerAddress, MuiNativeClassOwnerRecord.Size) ||
				!MuiNativeClassDispatchFrameCodec.TryRead(ref memory, current,
				out var frame) ||
				frame.Signature != MuiNativeClassDispatchFrameRecord.SignatureValue ||
				frame.OwnerTask.IsNull || frame.Class.IsNull ||
				frame.Object.IsNull || frame.Message.IsNull) return false;
			current = frame.Next;
			count++;
		}
		return current.IsNull;
	}

	private static bool TryFindFrame<T>(ref T memory, APTR head,
		APTR target, out APTR previousAddress,
		out MuiNativeClassDispatchFrameRecord previous,
		out MuiNativeClassDispatchFrameRecord value)
		where T : struct, IMuiGuestMemory
	{
		previousAddress = APTR.Null;
		previous = default;
		value = default;
		var current = head;
		for (var visited = 0u; current.IsNotNull && visited < MaximumFrames;
			visited++)
		{
			if (!MuiNativeClassDispatchFrameCodec.TryRead(ref memory, current,
				out var frame) ||
				frame.Signature != MuiNativeClassDispatchFrameRecord.SignatureValue ||
				frame.OwnerTask.IsNull || frame.Class.IsNull ||
				frame.Object.IsNull || frame.Message.IsNull) return false;
			if (current == target)
			{
				value = frame;
				return true;
			}
			previousAddress = current;
			previous = frame;
			current = frame.Next;
		}
		return false;
	}

	private static bool TryIsSuperClass<T>(ref T memory, APTR derivedClass,
		APTR candidateSuperClass, out bool isSuperClass)
		where T : struct, IMuiGuestMemory
	{
		isSuperClass = false;
		if (derivedClass.IsNull || candidateSuperClass.IsNull) return false;
		var current = derivedClass;
		for (uint visited = 0; current.IsNotNull && visited < MaximumFrames;
			visited++)
		{
			if (!memory.IsMapped(current, IClass.Size)) return false;
			if (current == candidateSuperClass)
			{
				isSuperClass = true;
				return true;
			}
			current = BOOPSIGuestCodec.ReadClass(ref memory, current).cl_Super;
		}
		return current.IsNull;
	}

	private static void Poison<T>(ref T memory, APTR ownerAddress)
		where T : struct, IMuiGuestMemory
	{
		if (TryReadOwner(ref memory, ownerAddress, out var owner))
			MuiNativeClassOwnerCodec.TryWriteDispatchContext(ref memory,
				ownerAddress, owner.ActiveDispatchFrames, 1);
	}

	private static bool Disjoint(APTR first, uint firstSize, APTR second,
		uint secondSize) => first.IsNotNull && second.IsNotNull &&
		first.Raw <= uint.MaxValue - firstSize &&
		second.Raw <= uint.MaxValue - secondSize &&
		(first.Raw + firstSize <= second.Raw ||
			second.Raw + secondSize <= first.Raw);
}
