/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// One allocation owns the native context, class/service state, drawing record,
// and the separate native public-object registry.
// Context is a typed prefix: the existing scalar class-platform handle can
// borrow this address without changing its proven eight-byte context ABI.
// Provider acquisition has its own pinned lifecycle protocol. Allocation-only
// detachment still requires PhaseEmpty and null provider handles.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeClassOwnerRecord
{
	internal const uint Size = MuiNativeClassContext.Size +
		MuiNativeClassOwnerScalarRecord.Size +
		MuiClassServiceStateRecord.Size + MuiHeadlessStateRecord.Size +
		MuiAslServiceStateRecord.Size +
		MuiRequesterServiceStateRecord.Size +
		MuiNativePublicObjectRegistryRecord.Size +
		MuiDrawingServiceStateRecord.Size +
		SignalSemaphore.Size;
	internal MuiNativeClassContext Context;
	internal uint Magic;
	internal uint Version;
	internal APTR LibraryBase;
	internal uint RegistryGeneration;
	internal uint Phase;
	internal uint ActiveOperations;
	internal APTR UtilityBase;
	internal APTR DosBase;
	internal APTR GraphicsBase;
	internal APTR KeymapBase;
	internal APTR ActiveDispatchFrames;
	internal uint DispatchFramesPoisoned;
	internal MuiClassServiceStateRecord Service;
	internal MuiHeadlessStateRecord Registry;
	internal MuiAslServiceStateRecord Asl;
	internal MuiRequesterServiceStateRecord Requester;
	internal MuiNativePublicObjectRegistryRecord PublicObjects;
	internal MuiDrawingServiceStateRecord Drawing;
	internal SignalSemaphore ServiceGate;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeClassOwnerScalarRecord
{
	// The owner header is a declaration-ordered C record. Keep the ABI size
	// here, but carry the fields by name through the codecs below.
	internal const uint FieldSize = 4;
	internal const uint Size = 48;
	internal uint Magic;
	internal uint Version;
	internal APTR LibraryBase;
	internal uint RegistryGeneration;
	internal uint Phase;
	internal uint ActiveOperations;
	internal APTR UtilityBase;
	internal APTR DosBase;
	internal APTR GraphicsBase;
	internal APTR KeymapBase;
	internal APTR ActiveDispatchFrames;
	internal uint DispatchFramesPoisoned;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeClassOwnerScalarLocations
{
	internal APTR Magic;
	internal APTR Version;
	internal APTR LibraryBase;
	internal APTR RegistryGeneration;
	internal APTR Phase;
	internal APTR ActiveOperations;
	internal APTR UtilityBase;
	internal APTR DosBase;
	internal APTR GraphicsBase;
	internal APTR KeymapBase;
	internal APTR ActiveDispatchFrames;
	internal APTR DispatchFramesPoisoned;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeClassOwnerControlLocations
{
	internal APTR IntuitionBase;
	internal APTR Phase;
	internal APTR ActiveOperations;
	internal APTR UtilityBase;
	internal APTR DosBase;
	internal APTR GraphicsBase;
	internal APTR KeymapBase;
}

internal static class MuiNativeClassOwnerScalarCodec
{
	internal static bool TryRead<T>(ref T memory, ref MuiGuestStructCursor cursor,
		out MuiNativeClassOwnerScalarRecord value) where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.Version) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var library) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.RegistryGeneration) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.Phase) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.ActiveOperations) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var utility) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var dos) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var graphics) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var keymap) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var dispatchFrames) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.DispatchFramesPoisoned)) return false;
		value.LibraryBase = APTR.FromPointer(library);
		value.UtilityBase = APTR.FromPointer(utility);
		value.DosBase = APTR.FromPointer(dos);
		value.GraphicsBase = APTR.FromPointer(graphics);
		value.KeymapBase = APTR.FromPointer(keymap);
		value.ActiveDispatchFrames = APTR.FromPointer(dispatchFrames);
		return true;
	}

	internal static bool Write<T>(ref T memory, ref MuiGuestStructCursor cursor,
		MuiNativeClassOwnerScalarRecord value) where T : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Version) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.LibraryBase.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.RegistryGeneration) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Phase) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.ActiveOperations) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.UtilityBase.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.DosBase.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.GraphicsBase.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.KeymapBase.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.ActiveDispatchFrames.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.DispatchFramesPoisoned);

	internal static bool TryGetLocations<T>(ref T memory,
		ref MuiGuestStructCursor cursor,
		out MuiNativeClassOwnerScalarLocations value) where T : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryTake(ref memory, ref cursor,
			MuiNativeClassOwnerScalarRecord.FieldSize, out value.Magic) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeClassOwnerScalarRecord.FieldSize, out value.Version) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeClassOwnerScalarRecord.FieldSize, out value.LibraryBase) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeClassOwnerScalarRecord.FieldSize, out value.RegistryGeneration) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeClassOwnerScalarRecord.FieldSize, out value.Phase) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeClassOwnerScalarRecord.FieldSize, out value.ActiveOperations) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeClassOwnerScalarRecord.FieldSize, out value.UtilityBase) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeClassOwnerScalarRecord.FieldSize, out value.DosBase) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
			MuiNativeClassOwnerScalarRecord.FieldSize, out value.GraphicsBase) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeClassOwnerScalarRecord.FieldSize, out value.KeymapBase) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeClassOwnerScalarRecord.FieldSize, out value.ActiveDispatchFrames) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeClassOwnerScalarRecord.FieldSize, out value.DispatchFramesPoisoned);
	}
}

// Wire positioning is confined to this declaration-ordered adapter. Lifecycle
// code carries named records and uses these helpers for embedded allocations.
internal static class MuiNativeClassOwnerCodec
{
	internal static bool TryRead<T>(ref T memory, APTR address,
		out MuiNativeClassOwnerRecord value) where T : struct, IMuiGuestMemory
	{
		value = default;
		var record = default(MuiNativeClassOwnerRecord);
		if (!MuiGuestStructCursor.TryCreate(ref memory, address, MuiNativeClassOwnerRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor, MuiNativeClassContext.Size, out var context) ||
			!MuiNativeClassContextCodec.TryRead(ref memory, context, out record.Context) ||
			!MuiNativeClassOwnerScalarCodec.TryRead(ref memory, ref cursor, out var scalars) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor, MuiClassServiceStateRecord.Size, out var service) ||
			!MuiClassServiceStateCodec.TryRead(ref memory, service, out record.Service) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor, MuiHeadlessStateRecord.Size, out var registry) ||
			!MuiHeadlessStateCodec.TryRead(ref memory, registry, out record.Registry) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiAslServiceStateRecord.Size, out var aslState) ||
			!MuiAslServiceStateCodec.TryRead(ref memory, aslState,
				out record.Asl) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiRequesterServiceStateRecord.Size, out var requesterState) ||
			!MuiRequesterServiceStateStructCodec.TryRead(ref memory,
				requesterState, out record.Requester) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativePublicObjectRegistryRecord.Size, out var publicObjects) ||
			!MuiNativePublicObjectRegistryCodec.TryRead(ref memory, publicObjects,
				out record.PublicObjects) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiDrawingServiceStateRecord.Size, out var drawing) ||
			!MuiDrawingServiceStateStructCodec.TryRead(ref memory, drawing,
				out record.Drawing) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor, SignalSemaphore.Size, out var gate) ||
			!MuiSignalSemaphoreCodec.TryRead(ref memory, gate, out record.ServiceGate) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		record.Magic = scalars.Magic;
		record.Version = scalars.Version;
		record.LibraryBase = scalars.LibraryBase;
		record.RegistryGeneration = scalars.RegistryGeneration;
		record.Phase = scalars.Phase;
		record.ActiveOperations = scalars.ActiveOperations;
		record.UtilityBase = scalars.UtilityBase;
		record.DosBase = scalars.DosBase;
		record.GraphicsBase = scalars.GraphicsBase;
		record.KeymapBase = scalars.KeymapBase;
		record.ActiveDispatchFrames = scalars.ActiveDispatchFrames;
		record.DispatchFramesPoisoned = scalars.DispatchFramesPoisoned;
		value = record;
		return true;
	}

	internal static bool Write<T>(ref T memory, APTR address,
		MuiNativeClassOwnerRecord value) where T : struct, IMuiGuestMemory
	{
		var scalars = default(MuiNativeClassOwnerScalarRecord);
		scalars.Magic = value.Magic;
		scalars.Version = value.Version;
		scalars.LibraryBase = value.LibraryBase;
		scalars.RegistryGeneration = value.RegistryGeneration;
		scalars.Phase = value.Phase;
		scalars.ActiveOperations = value.ActiveOperations;
		scalars.UtilityBase = value.UtilityBase;
		scalars.DosBase = value.DosBase;
		scalars.GraphicsBase = value.GraphicsBase;
		scalars.KeymapBase = value.KeymapBase;
		scalars.ActiveDispatchFrames = value.ActiveDispatchFrames;
		scalars.DispatchFramesPoisoned = value.DispatchFramesPoisoned;
		return MuiGuestStructCursor.TryCreate(ref memory, address, MuiNativeClassOwnerRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor, MuiNativeClassContext.Size, out var context) &&
			MuiNativeClassContextCodec.Write(ref memory, context, value.Context) &&
			MuiNativeClassOwnerScalarCodec.Write(ref memory, ref cursor, scalars) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor, MuiClassServiceStateRecord.Size, out var service) &&
			MuiClassServiceStateCodec.Write(ref memory, service, value.Service) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor, MuiHeadlessStateRecord.Size, out var registry) &&
			MuiHeadlessStateCodec.Write(ref memory, registry, value.Registry) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiAslServiceStateRecord.Size, out var aslState) &&
			MuiAslServiceStateCodec.Write(ref memory, aslState, value.Asl) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiRequesterServiceStateRecord.Size, out var requesterState) &&
			MuiRequesterServiceStateStructCodec.Write(ref memory, requesterState,
				value.Requester) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativePublicObjectRegistryRecord.Size, out var publicObjects) &&
			MuiNativePublicObjectRegistryCodec.Write(ref memory, publicObjects,
				value.PublicObjects) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiDrawingServiceStateRecord.Size, out var drawing) &&
			MuiDrawingServiceStateStructCodec.Write(ref memory, drawing,
				value.Drawing) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor, SignalSemaphore.Size, out var gate) &&
			MuiSignalSemaphoreCodec.Write(ref memory, gate, value.ServiceGate) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryGetServiceAddress<T>(ref T memory, APTR address,
		out APTR service) where T : struct, IMuiGuestMemory =>
		TryGetChildren(ref memory, address, out service, out _, out _, out _, out _);

	// Publish only the two fields owned by the dispatcher-frame protocol. In
	// particular, this leaves the embedded Exec semaphore untouched.
	internal static bool TryWriteDispatchContext<T>(ref T memory, APTR address,
		APTR frameHead, uint poisoned) where T : struct, IMuiGuestMemory
	{
		if (poisoned > 1 ||
			!MuiGuestStructCursor.TryCreate(ref memory, address,
				MuiNativeClassOwnerRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeClassContext.Size, out var context) ||
			!MuiNativeClassContextCodec.TryRead(ref memory, context, out _) ||
			!MuiNativeClassOwnerScalarCodec.TryGetLocations(ref memory, ref cursor,
				out var fields)) return false;
		memory.WriteUInt32(fields.ActiveDispatchFrames, 0, frameHead.Raw);
		memory.WriteUInt32(fields.DispatchFramesPoisoned, 0, poisoned);
		return true;
	}

	internal static bool TryGetGateAddress<T>(ref T memory, APTR address,
		out APTR gate) where T : struct, IMuiGuestMemory
	{
		gate = APTR.Null;
		return TryCreateOwnerTail(ref memory, address, out var cursor) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiClassServiceStateRecord.Size, out _) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiHeadlessStateRecord.Size, out _) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiAslServiceStateRecord.Size, out _) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiRequesterServiceStateRecord.Size, out _) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativePublicObjectRegistryRecord.Size, out _) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiDrawingServiceStateRecord.Size, out _) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor, SignalSemaphore.Size, out gate) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryGetRegistryAddress<T>(ref T memory, APTR address,
		out APTR registry) where T : struct, IMuiGuestMemory =>
		TryGetChildren(ref memory, address, out _, out registry, out _, out _, out _);

	internal static bool TryGetPublicObjectStateAddress<T>(ref T memory,
		APTR address, out APTR publicObjects) where T : struct, IMuiGuestMemory =>
		TryGetChildren(ref memory, address, out _, out _, out _, out _, out publicObjects);

	internal static bool TryGetAslStateAddress<T>(ref T memory, APTR address,
		out APTR aslState) where T : struct, IMuiGuestMemory =>
		TryGetChildren(ref memory, address, out _, out _, out aslState, out _, out _);

	internal static bool TryGetRequesterStateAddress<T>(ref T memory,
		APTR address, out APTR requesterState) where T : struct, IMuiGuestMemory =>
		TryGetChildren(ref memory, address, out _, out _, out _,
			out requesterState, out _);

	internal static bool TryGetDrawingStateAddress<T>(ref T memory,
		APTR address, out APTR drawingState) where T : struct, IMuiGuestMemory
	{
		drawingState = APTR.Null;
		if (!TryCreateOwnerTail(ref memory, address, out var cursor)) return false;

		return MuiGuestStructCursor.TryTake(ref memory, ref cursor,
			MuiClassServiceStateRecord.Size, out _) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiHeadlessStateRecord.Size, out _) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiAslServiceStateRecord.Size, out _) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiRequesterServiceStateRecord.Size, out _) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativePublicObjectRegistryRecord.Size, out _) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiDrawingServiceStateRecord.Size, out drawingState);
	}

	// Admit all three named fields before writing any of them. Unlike a whole
	// root rewrite with interleaved field admission, rejection cannot publish
	// or clear just one ownership alias. No unrelated root field is rewritten.
	internal static bool TryWriteRootLinks<T>(ref T memory, APTR root,
		MuiMasterPrivateRoot value) where T : struct, IMuiGuestMemory
	{
		if (!MuiMasterPrivateRootRecordMemoryCodec.TryGetAddress(ref memory, root,
			MuiMasterPrivateRootField.ClassRegistry, out var registry) ||
			!MuiMasterPrivateRootRecordMemoryCodec.TryGetAddress(ref memory, root,
			MuiMasterPrivateRootField.CallbackState, out var context) ||
			!MuiMasterPrivateRootRecordMemoryCodec.TryGetAddress(ref memory, root,
			MuiMasterPrivateRootField.LoaderState, out var owner)) return false;
		memory.WriteUInt32(registry, 0, value.ClassRegistry);
		memory.WriteUInt32(context, 0, value.CallbackState);
		memory.WriteUInt32(owner, 0, value.LoaderState);
		return true;
	}

	// Provider transitions update only these named fields, never a stale copy
	// of the embedded registries or identity. Admission precedes every write.
	internal static bool TryWriteControl<T>(ref T memory, APTR owner,
		MuiNativeClassOwnerRecord value) where T : struct, IMuiGuestMemory
	{
		if (!TryGetControlLocations(ref memory, owner, out var fields)) return false;
		memory.WriteUInt32(fields.IntuitionBase, 0, value.Context.IntuitionBase.Raw);
		memory.WriteUInt32(fields.Phase, 0, value.Phase);
		memory.WriteUInt32(fields.ActiveOperations, 0, value.ActiveOperations);
		memory.WriteUInt32(fields.UtilityBase, 0, value.UtilityBase.Raw);
		memory.WriteUInt32(fields.DosBase, 0, value.DosBase.Raw);
		memory.WriteUInt32(fields.GraphicsBase, 0, value.GraphicsBase.Raw);
		memory.WriteUInt32(fields.KeymapBase, 0, value.KeymapBase.Raw);
		return true;
	}

	private static bool TryGetControlLocations<T>(ref T memory, APTR owner,
		out MuiNativeClassOwnerControlLocations fields) where T : struct, IMuiGuestMemory
	{
		fields = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, owner, MuiNativeClassOwnerRecord.Size,
			out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor, MuiNativeClassContext.Size,
				out var context) ||
			!MuiNativeClassContextCodec.TryGetLocations(ref memory, context,
				out var contextFields) ||
			!MuiNativeClassOwnerScalarCodec.TryGetLocations(ref memory, ref cursor,
				out var scalarFields)) return false;
		fields.IntuitionBase = contextFields.IntuitionBase;
		fields.Phase = scalarFields.Phase;
		fields.ActiveOperations = scalarFields.ActiveOperations;
		fields.UtilityBase = scalarFields.UtilityBase;
		fields.DosBase = scalarFields.DosBase;
		fields.GraphicsBase = scalarFields.GraphicsBase;
		fields.KeymapBase = scalarFields.KeymapBase;
		return
			MuiGuestStructCursor.TryTake(ref memory, ref cursor, MuiClassServiceStateRecord.Size, out _) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor, MuiHeadlessStateRecord.Size, out _) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiAslServiceStateRecord.Size, out _) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiRequesterServiceStateRecord.Size, out _) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativePublicObjectRegistryRecord.Size, out _) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiDrawingServiceStateRecord.Size, out _) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor, SignalSemaphore.Size, out _) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}

	private static bool TryGetChildren<T>(ref T memory, APTR address,
		out APTR service, out APTR registry,
		out APTR aslState, out APTR requesterState, out APTR publicObjects)
		where T : struct, IMuiGuestMemory
	{
		service = APTR.Null;
		registry = APTR.Null;
		aslState = APTR.Null;
		requesterState = APTR.Null;
		publicObjects = APTR.Null;
		if (!TryCreateOwnerTail(ref memory, address, out var cursor)) return false;
		if (!MuiGuestStructCursor.TryTake(ref memory, ref cursor, MuiClassServiceStateRecord.Size, out var serviceAddress) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor, MuiHeadlessStateRecord.Size, out var registryAddress) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiAslServiceStateRecord.Size, out var aslStateAddress) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiRequesterServiceStateRecord.Size, out var requesterStateAddress) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativePublicObjectRegistryRecord.Size, out var publicObjectsAddress) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiDrawingServiceStateRecord.Size, out _) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor, SignalSemaphore.Size, out _) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		service = serviceAddress;
		registry = registryAddress;
		aslState = aslStateAddress;
		requesterState = requesterStateAddress;
		publicObjects = publicObjectsAddress;
		return true;
	}

	private static bool TryCreateOwnerTail<T>(ref T memory, APTR address,
		out MuiGuestStructCursor cursor) where T : struct, IMuiGuestMemory
	{
		cursor = default;
		return MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeClassOwnerRecord.Size, out cursor) &&
			MuiGuestStructCursor.TryTake(ref memory, ref cursor,
				MuiNativeClassContext.Size, out var context) &&
			MuiNativeClassContextCodec.TryRead(ref memory, context, out _) &&
			MuiNativeClassOwnerScalarCodec.TryRead(ref memory, ref cursor, out _);
	}
}

// Serialized ownership transaction. The caller owns the library/root and keeps
// all admitted memory stable for each transaction. Allocate returns fresh,
// disjoint storage and Free cannot fail. No loader calls, callbacks, scheduler
// waits or BOOPSI operations occur here. Init is unpublished; Expunge excludes
// admission under Exec.Forbid. This does not protect future provider loading.
internal static class MuiNativeClassOwnerCore
{
	internal const uint MagicValue = 0x4D554F31; // "MUO1"
	internal const uint Version = 6;
	internal const uint PhaseEmpty = 0;

	internal static bool TryAttach<T>(ref T platform, APTR library, APTR root,
		out APTR owner) where T : struct, IMuiAllocationPlatform
	{
		owner = APTR.Null;
		if (!TryReadRoot(ref platform, library, root, out var state) || !IsEmpty(state)) return false;
		var allocation = platform.Allocate(MuiNativeClassOwnerRecord.Size, MuiHeadlessLayout.AllocationFlags);
		if (allocation.IsNull) return false;
		if (!Disjoint(allocation, MuiNativeClassOwnerRecord.Size, root, MuiMasterPrivateRoot.Size) ||
			!Disjoint(allocation, MuiNativeClassOwnerRecord.Size, library, MuiExecLibraryBaseRecord.PositiveBytes))
			// A broken allocator returned borrowed storage. Never free that storage.
			return false;
		if (!MuiNativeClassOwnerCodec.TryGetRegistryAddress(ref platform, allocation, out var registry))
		{
			platform.Free(allocation, MuiNativeClassOwnerRecord.Size);
			return false;
		}
		var value = default(MuiNativeClassOwnerRecord);
		value.Context.OwnerRoot = root;
		value.Magic = MagicValue;
		value.Version = Version;
		value.LibraryBase = library;
		value.RegistryGeneration = state.RegistryGeneration;
		value.Phase = PhaseEmpty;
		value.Service.Magic = MuiClassServiceLayout.Magic;
		value.Service.Headless = registry;
		value.Service.Generation = 1;
		value.Registry.Magic = MuiHeadlessLayout.Magic;
		value.Registry.Version = MuiHeadlessLayout.Version;
		value.Registry.NextSequence = 1;
		value.Asl.Magic = MuiAslServiceLayout.Magic;
		value.Asl.Generation = MuiAslServiceLayout.Version;
		value.Requester.Magic = MuiRequesterServiceLayout.Magic;
		value.Requester.Generation = MuiRequesterServiceLayout.Version;
		value.PublicObjects.Signature = MuiNativePublicObjectRegistryRecord.Magic;
		value.PublicObjects.Revision = MuiNativePublicObjectRegistryRecord.Version;
		value.Drawing.Magic = MuiDrawingServiceLayout.Magic;
		value.Drawing.Generation = MuiDrawingServiceLayout.Version;
		if (!MuiNativeClassOwnerCodec.Write(ref platform, allocation, value) ||
			!TryReadRoot(ref platform, library, root, out var fresh) || !IsEmpty(fresh) ||
			fresh.RegistryGeneration != state.RegistryGeneration)
		{
			platform.Free(allocation, MuiNativeClassOwnerRecord.Size);
			return false;
		}
		fresh.LoaderState = allocation.Raw;
		fresh.CallbackState = allocation.Raw; // borrowed Context prefix, not a second allocation
		fresh.ClassRegistry = registry.Raw;
		if (!MuiNativeClassOwnerCodec.TryWriteRootLinks(ref platform, root, fresh))
		{
			platform.Free(allocation, MuiNativeClassOwnerRecord.Size);
			return false;
		}
		owner = allocation;
		return true;
	}

	internal static bool CanDetach<T>(ref T memory, APTR library, APTR root)
		where T : struct, IMuiGuestMemory =>
		TryReadQuiescent(ref memory, library, root, out _, out _);

	internal static bool TryDetach<T>(ref T platform, APTR library, APTR root)
		where T : struct, IMuiAllocationPlatform
	{
		if (!TryReadQuiescent(ref platform, library, root, out var state, out var owner)) return false;
		if (owner.IsNull) return true;
		state.LoaderState = 0;
		state.CallbackState = 0;
		state.ClassRegistry = 0;
		if (!MuiNativeClassOwnerCodec.TryWriteRootLinks(ref platform, root, state)) return false;
		platform.Free(owner, MuiNativeClassOwnerRecord.Size);
		return true;
	}

	private static bool TryReadQuiescent<T>(ref T memory, APTR library, APTR root,
		out MuiMasterPrivateRoot state, out APTR owner) where T : struct, IMuiGuestMemory
	{
		owner = APTR.Null;
		if (!TryReadRoot(ref memory, library, root, out state) || !IsOtherwiseQuiescent(state)) return false;
		if (IsEmpty(state)) return true;
		if (!TryReadAttached(ref memory, library, root, out state, out var address, out var value) ||
			!IsQuiescent(ref memory, state, value, address) || value.Phase != PhaseEmpty ||
			value.Context.IntuitionBase.IsNotNull || value.UtilityBase.IsNotNull ||
			value.DosBase.IsNotNull || value.GraphicsBase.IsNotNull ||
			value.KeymapBase.IsNotNull) return false;
		owner = address;
		return true;
	}

	// Identity and structural validity, independent of a provider phase or
	// active class operation. Callers apply their own admission policy next.
	internal static bool TryReadAttached<T>(ref T memory, APTR library, APTR root,
		out MuiMasterPrivateRoot state, out APTR owner, out MuiNativeClassOwnerRecord value)
		where T : struct, IMuiGuestMemory
	{
		owner = APTR.Null;
		value = default;
		if (!TryReadRoot(ref memory, library, root, out state)) return false;
		var address = APTR.FromPointer(state.LoaderState);
		if (address.IsNull || state.CallbackState != address.Raw ||
			!Disjoint(address, MuiNativeClassOwnerRecord.Size, root, MuiMasterPrivateRoot.Size) ||
			!Disjoint(address, MuiNativeClassOwnerRecord.Size, library, MuiExecLibraryBaseRecord.PositiveBytes) ||
			!MuiNativeClassOwnerCodec.TryRead(ref memory, address, out value) ||
			!MuiNativeClassOwnerCodec.TryGetRegistryAddress(ref memory, address, out var registry) ||
			state.ClassRegistry != registry.Raw || value.Context.OwnerRoot.Raw != root.Raw ||
			value.Magic != MagicValue || value.Version != Version || value.LibraryBase.Raw != library.Raw ||
			value.RegistryGeneration != state.RegistryGeneration ||
			value.Service.Magic != MuiClassServiceLayout.Magic || value.Service.Generation == 0 ||
			value.Service.Headless.Raw != registry.Raw || value.Registry.Magic != MuiHeadlessLayout.Magic ||
		value.Registry.Version != MuiHeadlessLayout.Version ||
			value.Asl.Magic != MuiAslServiceLayout.Magic ||
			value.Asl.Generation != MuiAslServiceLayout.Version ||
			value.Requester.Magic != MuiRequesterServiceLayout.Magic ||
			value.Requester.Generation != MuiRequesterServiceLayout.Version ||
			value.PublicObjects.Signature != MuiNativePublicObjectRegistryRecord.Magic ||
			value.PublicObjects.Revision != MuiNativePublicObjectRegistryRecord.Version ||
			value.Drawing.Magic != MuiDrawingServiceLayout.Magic ||
			value.Drawing.Generation != MuiDrawingServiceLayout.Version ||
			value.DispatchFramesPoisoned > 1) return false;
		owner = address;
		return true;
	}

	// NextSequence and Mutation are cumulative counters, not active work.
	internal static bool IsQuiescent(MuiMasterPrivateRoot state,
		MuiNativeClassOwnerRecord value, APTR gate) =>
		IsOtherwiseQuiescent(state) && value.ActiveOperations == 0 && value.Service.Head.IsNull &&
		value.Asl.Head.IsNull &&
		value.Registry.Classes.IsNull && value.Registry.Objects.IsNull &&
		value.PublicObjects.Head.IsNull &&
		value.Drawing.ClipHead.IsNull && value.Drawing.RefreshHead.IsNull &&
		value.Drawing.PenHead.IsNull &&
		value.ActiveDispatchFrames.IsNull && value.DispatchFramesPoisoned == 0 &&
		value.Registry.NotifyDepth == 0 && value.Registry.Reserved == 0 &&
		MuiSignalSemaphoreCodec.IsIdle(gate, value.ServiceGate);

	internal static bool IsQuiescent<T>(ref T memory, MuiMasterPrivateRoot state,
		MuiNativeClassOwnerRecord value, APTR owner) where T : struct, IMuiGuestMemory =>
		MuiNativeClassOwnerCodec.TryGetGateAddress(ref memory, owner, out var gate) &&
		IsQuiescent(state, value, gate);

	private static bool TryReadRoot<T>(ref T memory, APTR library, APTR root,
		out MuiMasterPrivateRoot state) where T : struct, IMuiGuestMemory
	{
		state = default;
		return library.IsNotNull && memory.IsMapped(library, MuiExecLibraryBaseRecord.Size) &&
			Disjoint(library, MuiExecLibraryBaseRecord.PositiveBytes, root, MuiMasterPrivateRoot.Size) &&
			MuiMasterPrivateRootCodec.TryRead(ref memory, root, out state) && state.RegistryGeneration != 0;
	}

	private static bool IsEmpty(MuiMasterPrivateRoot state) =>
		IsOtherwiseQuiescent(state) && state.ClassRegistry == 0 &&
		state.CallbackState == 0 && state.LoaderState == 0;

	private static bool IsOtherwiseQuiescent(MuiMasterPrivateRoot state) =>
		state.AllocationPolicy == 0 && state.ErrorState == 0 && state.ApplicationHead == 0 &&
		state.ExternalClassHead == 0 && state.ActiveDispatchDepth == 0 &&
		state.ActiveCallbackDepth == 0 && state.Flags == 0 && state.Reserved == 0;

	private static bool Disjoint(APTR first, uint firstSize, APTR second, uint secondSize) =>
		first.IsNotNull && second.IsNotNull && first.Raw <= uint.MaxValue - firstSize &&
		second.Raw <= uint.MaxValue - secondSize &&
		(first.Raw + firstSize <= second.Raw || second.Raw + secondSize <= first.Raw);
}

// Exactly the memory/allocation capabilities required by the owner transaction;
// no dummy GUI/class/loader methods. Native addresses are private owned storage,
// with the same C-pointer validity contract as MuiNativeClassMemory.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExecClassOwnerPlatform : IMuiAllocationPlatform
{
	private uint _abiSlot;
	public bool IsMapped(APTR address, uint size)
	{
		_ = _abiSlot;
		return default(MuiNativeClassMemory).IsMapped(address, size);
	}
	public byte ReadUInt8(APTR address, int offset = 0) => APTR.ReadUInt8(address, offset);
	public ushort ReadUInt16(APTR address, int offset = 0) => APTR.ReadUInt16(address, offset);
	public uint ReadUInt32(APTR address, int offset = 0) => APTR.ReadUInt32(address, offset);
	public void WriteUInt8(APTR address, int offset, byte value) => APTR.WriteUInt8(address, offset, value);
	public void WriteUInt16(APTR address, int offset, ushort value) => APTR.WriteUInt16(address, offset, value);
	public void WriteUInt32(APTR address, int offset, uint value) => APTR.WriteUInt32(address, offset, value);
	public void Clear(APTR address, uint size) => default(MuiNativeClassMemory).Clear(address, size);
	public void Copy(APTR source, APTR destination, uint size) => default(MuiNativeClassMemory).Copy(source, destination, size);
	public APTR Allocate(uint size, uint flags) => Exec.AllocMem(size, (Exec.MemoryFlags)flags);
	public void Free(APTR address, uint size) => Exec.FreeMem(address, size);
}
