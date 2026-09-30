/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperSharp.Sdk.Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// A narrow named view of the public ExecBase fields needed for admission. It
// is not overlaid on ExecBase and does not change its published ABI.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeExecAdmissionRecord
{
	internal const uint Size = 6;
	internal APTR ThisTask;
	internal sbyte InterruptDisableNesting;
	internal sbyte TaskDisableNesting;
}

internal static class MuiNativeExecAdmissionCodec
{
	internal static bool TryRead<T>(ref T memory, APTR execBase,
		out MuiNativeExecAdmissionRecord value) where T : struct, IMuiGuestMemory
	{
		value = default;
		if (execBase.IsNull || execBase.Raw > uint.MaxValue - ExecBase.Size ||
			!memory.IsMapped(execBase, ExecBase.Size)) return false;
		value.ThisTask = ExecBaseCodec.ReadThisTask(ref memory, execBase);
		// These SDK-owned positions stay at this compatibility codec boundary:
		// the pinned SDK does not yet expose the newer named nesting readers.
		value.InterruptDisableNesting = unchecked((sbyte)memory.ReadUInt8(execBase,
			ExecLayout.ExecBase.IDNestCount));
		value.TaskDisableNesting = unchecked((sbyte)memory.ReadUInt8(execBase,
			ExecLayout.ExecBase.TaskDisableNestCount));
		return true;
	}
}

// Private task-context admission, not a public MUI vector. The caller holds a
// library open across Enter and its matching Leave. Provider retirement is
// deliberately separate from the nonwaiting Exec Expunge entrypoint.
internal static class MuiNativeProviderOwner
{
	// Prepare the scalar native capability without dereferencing its context.
	// The owned-service core validates gate authority before using the platform.
	internal static bool TryPrepareServicePlatform(ref MuiNativeServiceLease service,
		out MuiNativeClassPlatform classes, out APTR currentTask)
	{
		classes = default;
		currentTask = APTR.Null;
		if (!TryReadLibrary(service.Operation.LibraryBase, out var record) || !IsOrdinaryTask(record.ExecBase)) return false;
		var memory = default(MuiNativeClassMemory);
		currentTask = ExecBaseCodec.ReadThisTask(ref memory, record.ExecBase);
		classes.Context = service.Operation.Owner;
		return true;
	}

	// Drawing vectors use the same pinned class context but borrow the embedded
	// named drawing record. Initialization is idempotent and happens only after
	// the caller has admitted the live owner/library lease.
	internal static bool TryPrepareDrawingServicePlatform(
		ref MuiNativeServiceLease service, out MuiNativeClassPlatform drawing,
		out APTR serviceState)
	{
		drawing = default;
		serviceState = APTR.Null;
		if (!TryReadLibrary(service.Operation.LibraryBase, out var record) ||
			!IsOrdinaryTask(record.ExecBase)) return false;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeClassOwnerCore.TryReadAttached(ref memory,
			service.Operation.LibraryBase, service.Operation.OwnerRoot, out _,
			out var ownerAddress, out _) || ownerAddress != service.Operation.Owner ||
			!MuiNativeClassOwnerCodec.TryGetDrawingStateAddress(ref memory,
			ownerAddress, out serviceState)) return false;
		drawing.Context = ownerAddress;
		return MuiDrawingServiceCore.Initialize(ref drawing, serviceState);
	}

	// The resident ASL vectors use a deliberately smaller native capability than
	// the class platform. The owner record is initialized before publication;
	// admission here is read-only so a damaged live lease list is rejected rather
	// than cleared and leaked.
	internal static bool TryPrepareAslServicePlatform(ref MuiNativeServiceLease service,
		out MuiNativeAslPlatform asl, out APTR serviceState)
	{
		asl = default;
		serviceState = APTR.Null;
		if (!TryReadLibrary(service.Operation.LibraryBase, out var record) ||
			!IsOrdinaryTask(record.ExecBase)) return false;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeClassOwnerCodec.TryGetAslStateAddress(ref memory,
			service.Operation.Owner, out serviceState)) return false;
		asl.Owner = service.Operation.Owner;
		return MuiAslServiceAdmission.TryReadReadyState(ref asl, serviceState,
			out _);
	}

	// MUI_RequestA and MUI_RequestObjectA keep the same serialized provider
	// operation as the class vectors but use only guest memory, Exec allocation,
	// and the pinned intuition.library base. Readiness is admitted without
	// rewriting the owner, so a damaged requester state fails closed.
	internal static bool TryPrepareRequesterServicePlatform(
		ref MuiNativeServiceLease service, out MuiNativeRequesterPlatform requester,
		out APTR serviceState)
	{
		requester = default;
		serviceState = APTR.Null;
		if (!TryReadLibrary(service.Operation.LibraryBase, out var record) ||
			!IsOrdinaryTask(record.ExecBase)) return false;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeClassOwnerCore.TryReadAttached(ref memory,
			service.Operation.LibraryBase, service.Operation.OwnerRoot, out _,
			out var ownerAddress, out var owner) || ownerAddress != service.Operation.Owner ||
			owner.Context.IntuitionBase.IsNull ||
			!MuiNativeClassOwnerCodec.TryGetRequesterStateAddress(ref memory,
				ownerAddress, out serviceState) ||
			!MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref memory,
			ownerAddress, out _)) return false;
		requester.Classes.Context = ownerAddress;
		return MuiRequesterServiceCore.TryReadReadyState(ref requester,
			serviceState);
	}

	internal static bool TryEnterService(ref MuiNativeClassLease operation, out MuiNativeServiceLease service)
	{
		service = default;
		if (!TryReadLibrary(operation.LibraryBase, out var record) || !IsOrdinaryTask(record.ExecBase)) return false;
		var platform = default(MuiNativeServiceGatePlatform);
		platform.ExecBase = record.ExecBase;
		return MuiNativeServiceAccessCore.TryEnter(ref platform, ref operation, out service);
	}

	internal static bool TryLeaveService(ref MuiNativeServiceLease service, out MuiNativeClassLease operation)
	{
		operation = default;
		if (!TryReadLibrary(service.Operation.LibraryBase, out var record) || !IsOrdinaryTask(record.ExecBase)) return false;
		var platform = default(MuiNativeServiceGatePlatform);
		platform.ExecBase = record.ExecBase;
		return MuiNativeServiceAccessCore.TryLeave(ref platform, ref service, out operation);
	}

	internal static MuiNativeClassLeaseResult Enter(APTR library, out MuiNativeClassLease lease)
	{
		lease = default;
		if (!TryReadLibrary(library, out var record) || record.Header.OpenCount == 0 ||
			!IsOrdinaryTask(record.ExecBase)) return MuiNativeClassLeaseResult.Invalid;
		// Version zero is an initial local acquisition policy, not a declaration
		// that every version implements the eventual MorphOS provider surface.
		var request = default(MuiNativeProviderRequest);
		request.UtilityName = APTR.FromPointer(CString.ToUInt32(CString.FromLiteral("utility.library")));
		request.DosName = APTR.FromPointer(CString.ToUInt32(CString.FromLiteral("dos.library")));
		request.GraphicsName = APTR.FromPointer(CString.ToUInt32(CString.FromLiteral("graphics.library")));
		request.IntuitionName = APTR.FromPointer(CString.ToUInt32(CString.FromLiteral("intuition.library")));
		request.KeymapName = APTR.FromPointer(CString.ToUInt32(CString.FromLiteral("keymap.library")));
		var platform = default(MuiNativeClassLeasePlatform);
		return MuiNativeClassLeaseCore.Enter(ref platform, library, record.PrivateRoot, request, out lease);
	}

	internal static bool Leave(ref MuiNativeClassLease lease)
	{
		// A valid lease already pins this base. A nested Close may have consumed
		// the final open, so do not strand the pin by requiring OpenCount here.
		if (!TryReadLibrary(lease.LibraryBase, out var record) ||
			!IsOrdinaryTask(record.ExecBase)) return false;
		var platform = default(MuiNativeClassLeasePlatform);
		return MuiNativeClassLeaseCore.Leave(ref platform, ref lease);
	}

	internal static bool TryRetireIdle(APTR library)
	{
		if (!TryReadLibrary(library, out var record) || record.Header.OpenCount == 0 ||
			!IsOrdinaryTask(record.ExecBase)) return false;
		var platform = default(MuiNativeClassLeasePlatform);
		return MuiNativeClassLeaseCore.TryRetireIdle(ref platform, library, record.PrivateRoot);
	}

	private static bool TryReadLibrary(APTR library, out MuiExecLibraryBaseRecord record)
	{
		var memory = default(MuiExecLibraryMemory);
		memory.Base = library;
		return MuiExecLibraryBaseCodec.TryRead(ref memory, library, out record) &&
			record.Header.Node.Type == (byte)NodeType.Library &&
			record.Header.PositiveSize == MuiExecLibraryBaseRecord.PositiveBytes &&
			record.Header.NegativeSize == MuiExecLibraryBaseRecord.NegativeBytes &&
			record.Header.Node.Successor.IsNotNull && record.Header.Node.Predecessor.IsNotNull &&
			record.PrivateRoot.IsNotNull && record.ExecBase.IsNotNull;
	}

	private static bool IsOrdinaryTask(APTR execBase)
	{
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeExecAdmissionCodec.TryRead(ref memory, execBase, out var executive) ||
			execBase.Raw != APTR.ReadUInt32(APTR.FromPointer(4), 0)) return false;
		if (executive.InterruptDisableNesting >= 0 || executive.TaskDisableNesting >= 0) return false;
		var task = executive.ThisTask;
		if (!ExecTaskCodec.IsMapped(ref memory, task) ||
			ExecTaskCodec.ReadState(ref memory, task) != TaskState.Running) return false;
		var type = ExecNodeCodec.ReadType(ref memory, task);
		if (type != NodeType.Task && type != NodeType.Process) return false;
		// A zero mask observes the caller's SR without changing its mode/IPL.
		// This private loader contract rejects supervisor and masked-interrupt
		// callers, even if their Exec nesting bytes happen to be negative.
		return (Exec.SetSR(0, 0) & 0x2700) == 0;
	}
}

// Exactly thirteen capabilities: memory, real Exec library loading and short
// scheduling critical sections. The four-byte carrier keeps the native generic
// ABI scalar; the existing borrowed class context remains eight bytes.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeClassLeasePlatform : IMuiClassLeasePlatform
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
	public APTR OpenLibrary(APTR name, ushort version) => Exec.OpenLibraryRaw(CString.FromPointer(name.Raw), version);
	public void CloseLibrary(APTR library) => Exec.CloseLibrary(library);
	public void EnterCritical() => Exec.Forbid();
	public void LeaveCritical() => Exec.Permit();
}
