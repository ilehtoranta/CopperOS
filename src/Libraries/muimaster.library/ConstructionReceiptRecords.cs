/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiConstructionReceiptRecord
{
	internal const uint Size = 48;
	internal const uint Cookie = 0x4D435231;
	internal const uint Reserved = 1;
	internal const uint Running = 2;
	internal const uint Returned = 3;
	internal const uint Cleaning = 4;
	internal const uint Done = 5;
	internal uint Magic;
	internal APTR Root;
	internal APTR State;
	internal APTR Next;
	internal APTR DispatchClass;
	internal APTR OwningClass;
	internal APTR NativeObject;
	internal APTR InitializedObject;
	internal uint Ownership;
	internal uint Phase;
	internal uint RootGeneration;
	internal uint DisposeMethod;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiConstructionReceiptLocations
{
	internal APTR Next;
	internal APTR NativeObject;
	internal APTR InitializedObject;
	internal APTR Ownership;
	internal APTR Phase;
	internal APTR DisposeMethod;
}

// Declaration-ordered big-endian codecs; no lifecycle caller reconstructs offsets.
internal static class MuiConstructionReceiptCodec
{
	internal static bool TryRead<T>(ref T memory, APTR address, out MuiConstructionReceiptRecord value)
		where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address, MuiConstructionReceiptRecord.Size, out var cursor)) return false;
		if (!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.Magic)) return false;
		if (!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var root)) return false;
		if (!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var state)) return false;
		if (!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var next)) return false;
		if (!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var dispatchclass)) return false;
		if (!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var owningclass)) return false;
		if (!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var nativeobject)) return false;
		if (!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var initializedobject)) return false;
		if (!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.Ownership)) return false;
		if (!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.Phase)) return false;
		if (!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.RootGeneration)) return false;
		if (!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.DisposeMethod)) return false;
		value.Root = APTR.FromPointer(root);
		value.State = APTR.FromPointer(state);
		value.Next = APTR.FromPointer(next);
		value.DispatchClass = APTR.FromPointer(dispatchclass);
		value.OwningClass = APTR.FromPointer(owningclass);
		value.NativeObject = APTR.FromPointer(nativeobject);
		value.InitializedObject = APTR.FromPointer(initializedobject);
		return MuiGuestStructCursor.IsComplete(cursor) && value.Magic == MuiConstructionReceiptRecord.Cookie &&
			value.Phase >= MuiConstructionReceiptRecord.Reserved && value.Phase <= MuiConstructionReceiptRecord.Done &&
			value.Ownership <= (uint)MuiObjectAttachmentOwnership.CallerOwned && value.DisposeMethod == BOOPSI.OM_DISPOSE;
	}

	internal static bool Write<T>(ref T memory, APTR address, MuiConstructionReceiptRecord value)
		where T : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref memory, address, MuiConstructionReceiptRecord.Size, out var cursor)) return false;
		if (!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Magic)) return false;
		if (!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Root.Raw)) return false;
		if (!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.State.Raw)) return false;
		if (!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Next.Raw)) return false;
		if (!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.DispatchClass.Raw)) return false;
		if (!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.OwningClass.Raw)) return false;
		if (!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.NativeObject.Raw)) return false;
		if (!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.InitializedObject.Raw)) return false;
		if (!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Ownership)) return false;
		if (!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Phase)) return false;
		if (!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.RootGeneration)) return false;
		if (!MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.DisposeMethod)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryGetLocations<T>(ref T memory, APTR address, out MuiConstructionReceiptLocations fields)
		where T : struct, IMuiGuestMemory
	{
		fields = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address, MuiConstructionReceiptRecord.Size, out var cursor)) return false;
		if (!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 4, out _)) return false; // Magic
		if (!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 4, out _)) return false; // Root
		if (!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 4, out _)) return false; // State
		if (!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 4, out fields.Next)) return false; // Next
		if (!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 4, out _)) return false; // DispatchClass
		if (!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 4, out _)) return false; // OwningClass
		if (!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 4, out fields.NativeObject)) return false; // NativeObject
		if (!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 4, out fields.InitializedObject)) return false; // InitializedObject
		if (!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 4, out fields.Ownership)) return false; // Ownership
		if (!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 4, out fields.Phase)) return false; // Phase
		if (!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 4, out _)) return false; // RootGeneration
		if (!MuiGuestStructCursor.TryTake(ref memory, ref cursor, 4, out fields.DisposeMethod)) return false; // DisposeMethod
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryGetRootHead<T>(ref T memory, APTR root, out APTR field)
		where T : struct, IMuiGuestMemory =>
		MuiMasterPrivateRootRecordMemoryCodec.TryGetAddress(ref memory, root,
			MuiMasterPrivateRootField.Reserved, out field);
}
