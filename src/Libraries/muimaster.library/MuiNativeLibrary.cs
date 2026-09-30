/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperSharp.Compiler;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Development library base. The SDK owns the public Library prefix; these three
// private fields follow it. Exec rounds the positive area to LONG alignment.
// The complete MorphOS negative table is reserved even while individual public
// vectors are added progressively; unsupported slots point at one typed stub.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExecLibraryBaseRecord
{
	internal const uint Size = Library.Size + 12;
	internal const ushort PositiveBytes = (ushort)((Size + 3) & ~3u);
	internal const ushort NegativeBytes = MuiResidentMetadata.NegativeBytes;
	internal Library Header;
	internal APTR ExecBase;
	internal BPTR SegmentList;
	internal APTR PrivateRoot;
}

// Borrowed allocation claim for the positive area prepared by Exec AUTOINIT.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExecLibraryMemory : IMuiGuestMemory
{
	internal APTR Base;
	public bool IsMapped(APTR address, uint byteSize) =>
		!Base.IsNull && Base.Raw <= uint.MaxValue - MuiExecLibraryBaseRecord.PositiveBytes &&
		byteSize != 0 && byteSize <= MuiExecLibraryBaseRecord.PositiveBytes &&
		address.Raw >= Base.Raw &&
		address.Raw - Base.Raw <= MuiExecLibraryBaseRecord.PositiveBytes - byteSize;
	public byte ReadUInt8(APTR address, int offset = 0) => APTR.ReadUInt8(address, offset);
	public ushort ReadUInt16(APTR address, int offset = 0) => APTR.ReadUInt16(address, offset);
	public uint ReadUInt32(APTR address, int offset = 0) => APTR.ReadUInt32(address, offset);
	public void WriteUInt8(APTR address, int offset, byte value) => APTR.WriteUInt8(address, offset, value);
	public void WriteUInt16(APTR address, int offset, ushort value) => APTR.WriteUInt16(address, offset, value);
	public void WriteUInt32(APTR address, int offset, uint value) => APTR.WriteUInt32(address, offset, value);
	public void Clear(APTR address, uint byteCount)
	{
		if (!IsMapped(address, byteCount)) return;
		for (var index = 0; (uint)index < byteCount; index++) APTR.WriteUInt8(address, index, 0);
	}
	public void Copy(APTR source, APTR destination, uint byteCount)
	{
		if (!IsMapped(source, byteCount) || !IsMapped(destination, byteCount)) return;
		if (destination.Raw > source.Raw)
		{
			for (var index = (int)byteCount; index != 0;)
			{
				index--;
				APTR.WriteUInt8(destination, index, APTR.ReadUInt8(source, index));
			}
		}
		else
		{
			for (var index = 0; (uint)index < byteCount; index++)
				APTR.WriteUInt8(destination, index, APTR.ReadUInt8(source, index));
		}
	}
}

internal static class MuiExecLibraryBaseCodec
{
	internal static bool TryRead<T>(ref T memory, APTR address,
		out MuiExecLibraryBaseRecord value) where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiExecLibraryBaseRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor, Library.Size, out var header) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var execBase) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var segment) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var root)) return false;
		value.Header = ExecLibraryCodec.Read(ref memory, header);
		value.ExecBase = APTR.FromPointer(execBase);
		value.SegmentList = BPTR.FromRaw(segment);
		value.PrivateRoot = APTR.FromPointer(root);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<T>(ref T memory, APTR address,
		MuiExecLibraryBaseRecord value) where T : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiExecLibraryBaseRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref memory, ref cursor, Library.Size, out var header)) return false;
		ExecLibraryCodec.Write(ref memory, header, value.Header);
		return MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.ExecBase.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.SegmentList.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.PrivateRoot.Raw) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Call only through the exported ABI adapters when entering from guest code.
// CopperSharp's adapter preserves incoming registers and initializes SDK bases
// even when the image's program entry has never executed.
public static class MuiNativeLibraryEntrypoints
{
	public const ushort PositiveBytes = MuiExecLibraryBaseRecord.PositiveBytes;
	public const ushort NegativeBytes = MuiExecLibraryBaseRecord.NegativeBytes;
	// A library is not a command. The packager emits the equivalent inert entry
	// before its Resident; native clients enter only through exported adapters.
	public static uint LibraryImageEntry() => uint.MaxValue;

	public const string InitExport = "copperos.mui.library.init";
	public const string OpenExport = "copperos.mui.library.open";
	public const string CloseExport = "copperos.mui.library.close";
	public const string ExpungeExport = "copperos.mui.library.expunge";
	public const string ReservedExport = "copperos.mui.library.reserved";
	public const string UnsupportedExport = "copperos.mui.library.unsupported";
	public const string GetClassExport = "copperos.mui.library.get-class";
	public const string FreeClassExport = "copperos.mui.library.free-class";
	public const string RequestIDCMPExport = "copperos.mui.library.request-idcmp";
	public const string RejectIDCMPExport = "copperos.mui.library.reject-idcmp";
	public const string CreateCustomClassExport = "copperos.mui.library.create-custom-class";
	public const string DeleteCustomClassExport = "copperos.mui.library.delete-custom-class";
	public const string NewObjectAExport = "copperos.mui.library.new-object-a";
	public const string DisposeObjectExport = "copperos.mui.library.dispose-object";
	public const string MakeObjectAExport = "copperos.mui.library.make-object-a";
	public const string ErrorExport = "copperos.mui.library.error";
	public const string SetErrorExport = "copperos.mui.library.set-error";
	public const string AllocAslRequestExport = "copperos.mui.library.alloc-asl-request";
	public const string AslRequestExport = "copperos.mui.library.asl-request";
	public const string FreeAslRequestExport = "copperos.mui.library.free-asl-request";
	public const string RequestAExport = "copperos.mui.library.request-a";
	public const string RequestObjectAExport = "copperos.mui.library.request-object-a";
	public const string RedrawExport = "copperos.mui.library.redraw";
	public const string LayoutExport = "copperos.mui.library.layout";
	public const string ObtainPenExport = "copperos.mui.library.obtain-pen";
	public const string ReleasePenExport = "copperos.mui.library.release-pen";
	public const string AddClippingExport = "copperos.mui.library.add-clipping";
	public const string RemoveClippingExport = "copperos.mui.library.remove-clipping";
	public const string AddClipRegionExport = "copperos.mui.library.add-clip-region";
	public const string RemoveClipRegionExport = "copperos.mui.library.remove-clip-region";
	public const string BeginRefreshExport = "copperos.mui.library.begin-refresh";
	public const string EndRefreshExport = "copperos.mui.library.end-refresh";
	public const string GetRGBColorExport = "copperos.mui.library.get-rgb-color";

	[M68kExport(InitExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static APTR Init([M68kRegister(M68kRegister.D0)] APTR library,
		[M68kRegister(M68kRegister.A0)] BPTR segmentList,
		[M68kRegister(M68kRegister.A6)] APTR execBase)
	{
		var memory = default(MuiExecLibraryMemory);
		memory.Base = library;
		if (execBase.IsNull || !ExecLibraryCodec.IsMapped(ref memory, library)) return APTR.Null;
		var header = ExecLibraryCodec.Read(ref memory, library);
		if (header.NegativeSize != MuiExecLibraryBaseRecord.NegativeBytes ||
			header.PositiveSize != MuiExecLibraryBaseRecord.PositiveBytes)
		{
			ReleasePreparedBase(library, header);
			return APTR.Null;
		}
		if (!MuiExecPrivateRootOwner.TryCreate(out var root))
		{
			ReleasePreparedBase(library, header);
			return APTR.Null;
		}
		var ownerPlatform = default(MuiExecClassOwnerPlatform);
		if (!MuiNativeClassOwnerCore.TryAttach(ref ownerPlatform, library, root, out var owner))
		{
			MuiExecPrivateRootOwner.ReleaseValidated(ref root);
			ReleasePreparedBase(library, header);
			return APTR.Null;
		}
		// The owner is still unpublished. Exec initializes its embedded SDK
		// semaphore in place; no allocation or waiting is performed by this call.
		if (!MuiNativeClassOwnerCodec.TryGetGateAddress(ref ownerPlatform, owner, out var gate))
		{
			MuiNativeClassOwnerCore.TryDetach(ref ownerPlatform, library, root);
			MuiExecPrivateRootOwner.ReleaseValidated(ref root);
			ReleasePreparedBase(library, header);
			return APTR.Null;
		}
		Exec.InitSemaphore(gate);
		header.Node.Type = (byte)NodeType.Library;
		header.Node.Priority = 0;
		header.Node.Name = STRPTR.FromPointer(CString.ToUInt32(MuiResidentMetadata.DevelopmentName));
		header.Flags = LibraryFlags.Changed | LibraryFlags.SumUsed;
		header.Version = MuiResidentMetadata.DevelopmentVersion;
		header.Revision = MuiResidentMetadata.DevelopmentRevision;
		header.IdString = APTR.FromPointer(CString.ToUInt32(
			CString.FromLiteral("CopperOS MUI development 0.1")));
		header.OpenCount = 0;
		var record = default(MuiExecLibraryBaseRecord);
		record.Header = header;
		record.ExecBase = execBase;
		record.SegmentList = segmentList;
		record.PrivateRoot = root;
		if (!MuiExecLibraryBaseCodec.Write(ref memory, library, record))
		{
			MuiNativeClassOwnerCore.TryDetach(ref ownerPlatform, library, root);
			MuiExecPrivateRootOwner.ReleaseValidated(ref root);
			ReleasePreparedBase(library, header);
			return APTR.Null;
		}
		// Exec InitResident publishes the successful AUTOINIT result once.
		return library;
	}

	[M68kExport(OpenExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static APTR Open([M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.D0)] uint version)
	{
		Exec.Forbid();
		var memory = default(MuiExecLibraryMemory);
		memory.Base = library;
		var result = APTR.Null;
		if (TryReadLive(ref memory, library, out var record) &&
			version <= record.Header.Version && record.Header.OpenCount != ushort.MaxValue)
		{
			ExecLibraryCodec.WriteOpenCount(ref memory, library, (ushort)(record.Header.OpenCount + 1));
			ExecLibraryCodec.WriteFlags(ref memory, library, record.Header.Flags & ~LibraryFlags.DelayedExpunge);
			result = library;
		}
		Exec.Permit();
		return result;
	}

	[M68kExport(CloseExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static BPTR Close([M68kRegister(M68kRegister.A6)] APTR library)
	{
		Exec.Forbid();
		var memory = default(MuiExecLibraryMemory);
		memory.Base = library;
		var result = BPTR.Null;
		if (TryReadLive(ref memory, library, out var record) && record.Header.OpenCount != 0)
		{
			record.Header.OpenCount--;
			ExecLibraryCodec.WriteOpenCount(ref memory, library, record.Header.OpenCount);
			if (record.Header.OpenCount == 0 &&
				(record.Header.Flags & LibraryFlags.DelayedExpunge) != 0)
				result = ExpungeLocked(ref memory, library, record);
		}
		Exec.Permit();
		return result;
	}

	[M68kExport(ExpungeExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static BPTR Expunge([M68kRegister(M68kRegister.A6)] APTR library)
	{
		Exec.Forbid();
		var memory = default(MuiExecLibraryMemory);
		memory.Base = library;
		var result = BPTR.Null;
		if (TryReadLive(ref memory, library, out var record))
			result = ExpungeLocked(ref memory, library, record);
		Exec.Permit();
		return result;
	}

	[M68kExport(ReservedExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint Reserved([M68kRegister(M68kRegister.A6)] APTR library) => 0;

	// Public MUI vectors are installed by the host packager into the negative
	// table. Keeping the entrypoints as normal exported adapters means the same
	// code is callable from a direct native regression and from a real library
	// client; no vector arithmetic leaks into the service core.
	[M68kExport(UnsupportedExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint Unsupported([M68kRegister(M68kRegister.A6)] APTR library) => 0;

	[M68kExport(GetClassExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static APTR GetClass([M68kRegister(M68kRegister.A0)] APTR classId,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.GetClass(library, classId);

	[M68kExport(FreeClassExport)]
	public static void FreeClass([M68kRegister(M68kRegister.A0)] APTR classPointer,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.FreeClass(library, classPointer);

	[M68kExport(RequestIDCMPExport)]
	public static void RequestIDCMP(
		[M68kRegister(M68kRegister.A0)] APTR obj,
		[M68kRegister(M68kRegister.D0)] uint flags,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.RequestIDCMP(library, obj, flags);

	[M68kExport(RejectIDCMPExport)]
	public static void RejectIDCMP(
		[M68kRegister(M68kRegister.A0)] APTR obj,
		[M68kRegister(M68kRegister.D0)] uint flags,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.RejectIDCMP(library, obj, flags);

	[M68kExport(CreateCustomClassExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static APTR CreateCustomClass(
		[M68kRegister(M68kRegister.A0)] APTR libraryBase,
		[M68kRegister(M68kRegister.A1)] APTR superName,
		[M68kRegister(M68kRegister.A2)] APTR superClass,
		[M68kRegister(M68kRegister.D0)] int dataSize,
		[M68kRegister(M68kRegister.A3)] APTR dispatcher,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.CreateCustomClass(library, libraryBase,
			superName, superClass, dataSize, dispatcher);

	[M68kExport(DeleteCustomClassExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint DeleteCustomClass(
		[M68kRegister(M68kRegister.A0)] APTR customClass,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.DeleteCustomClass(library, customClass) ? 1u : 0u;

	[M68kExport(NewObjectAExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static APTR NewObjectA(
		[M68kRegister(M68kRegister.A0)] APTR className,
		[M68kRegister(M68kRegister.A1)] APTR tags,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.NewObjectA(library, className, tags);

	[M68kExport(DisposeObjectExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint DisposeObject(
		[M68kRegister(M68kRegister.A0)] APTR obj,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.DisposeObject(library, obj) ? 1u : 0u;

	[M68kExport(MakeObjectAExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static APTR MakeObjectA(
		[M68kRegister(M68kRegister.D0)] uint type,
		[M68kRegister(M68kRegister.A0)] APTR parameters,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.MakeObjectA(library, type, parameters);

	[M68kExport(ErrorExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static int Error([M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativeErrorGateway.Error(library);

	[M68kExport(SetErrorExport)]
	public static void SetError(
		[M68kRegister(M68kRegister.D0)] int error,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativeErrorGateway.SetError(library, error);

	[M68kExport(AllocAslRequestExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static APTR AllocAslRequest(
		[M68kRegister(M68kRegister.D0)] uint requestType,
		[M68kRegister(M68kRegister.A0)] APTR tags,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.AllocAslRequest(library, requestType, tags);

	[M68kExport(AslRequestExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static int AslRequest(
		[M68kRegister(M68kRegister.A0)] APTR requester,
		[M68kRegister(M68kRegister.A1)] APTR tags,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.AslRequest(library, requester, tags);

	[M68kExport(FreeAslRequestExport)]
	public static void FreeAslRequest(
		[M68kRegister(M68kRegister.A0)] APTR requester,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.FreeAslRequest(library, requester);

	[M68kExport(RequestAExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static int RequestA(
		[M68kRegister(M68kRegister.D0)] APTR application,
		[M68kRegister(M68kRegister.D1)] APTR window,
		[M68kRegister(M68kRegister.D2)] uint flags,
		[M68kRegister(M68kRegister.A0)] APTR title,
		[M68kRegister(M68kRegister.A1)] APTR gadgets,
		[M68kRegister(M68kRegister.A2)] APTR format,
		[M68kRegister(M68kRegister.A3)] APTR parameters,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.RequestA(library, application, window, flags,
			title, gadgets, format, parameters);

	[M68kExport(RequestObjectAExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static int RequestObjectA(
		[M68kRegister(M68kRegister.D0)] APTR application,
		[M68kRegister(M68kRegister.D1)] APTR window,
		[M68kRegister(M68kRegister.D2)] uint flags,
		[M68kRegister(M68kRegister.A0)] APTR title,
		[M68kRegister(M68kRegister.A1)] APTR gadgets,
		[M68kRegister(M68kRegister.A2)] APTR obj,
		[M68kRegister(M68kRegister.A3)] APTR format,
		[M68kRegister(M68kRegister.A4)] APTR parameters,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.RequestObjectA(library, application, window,
			flags, title, gadgets, obj, format, parameters);

	[M68kExport(RedrawExport)]
	public static void Redraw(
		[M68kRegister(M68kRegister.A0)] APTR obj,
		[M68kRegister(M68kRegister.D0)] uint flags,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		_ = MuiNativePublicClassGateway.Redraw(library, obj, flags);

	[M68kExport(LayoutExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint Layout(
		[M68kRegister(M68kRegister.A0)] APTR obj,
		[M68kRegister(M68kRegister.D0)] int left,
		[M68kRegister(M68kRegister.D1)] int top,
		[M68kRegister(M68kRegister.D2)] int width,
		[M68kRegister(M68kRegister.D3)] int height,
		[M68kRegister(M68kRegister.D4)] uint flags,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.Layout(library, obj, left, top, width, height,
			flags) ? 1u : 0u;

	[M68kExport(ObtainPenExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static int ObtainPen(
		[M68kRegister(M68kRegister.A0)] APTR renderInfo,
		[M68kRegister(M68kRegister.A1)] APTR penSpec,
		[M68kRegister(M68kRegister.D0)] uint flags,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.ObtainPen(library, renderInfo, penSpec, flags);

	[M68kExport(ReleasePenExport)]
	public static void ReleasePen(
		[M68kRegister(M68kRegister.A0)] APTR renderInfo,
		[M68kRegister(M68kRegister.D0)] int pen,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.ReleasePen(library, renderInfo, pen);

	[M68kExport(AddClippingExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static APTR AddClipping(
		[M68kRegister(M68kRegister.A0)] APTR renderInfo,
		[M68kRegister(M68kRegister.D0)] short left,
		[M68kRegister(M68kRegister.D1)] short top,
		[M68kRegister(M68kRegister.D2)] short width,
		[M68kRegister(M68kRegister.D3)] short height,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.AddClipping(library, renderInfo, left, top,
			width, height);

	[M68kExport(RemoveClippingExport)]
	public static void RemoveClipping(
		[M68kRegister(M68kRegister.A0)] APTR renderInfo,
		[M68kRegister(M68kRegister.A1)] APTR handle,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.RemoveClipping(library, renderInfo, handle);

	[M68kExport(AddClipRegionExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static APTR AddClipRegion(
		[M68kRegister(M68kRegister.A0)] APTR renderInfo,
		[M68kRegister(M68kRegister.A1)] APTR region,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.AddClipRegion(library, renderInfo, region);

	[M68kExport(RemoveClipRegionExport)]
	public static void RemoveClipRegion(
		[M68kRegister(M68kRegister.A0)] APTR renderInfo,
		[M68kRegister(M68kRegister.A1)] APTR handle,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.RemoveClipRegion(library, renderInfo, handle);

	[M68kExport(BeginRefreshExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static int BeginRefresh(
		[M68kRegister(M68kRegister.A0)] APTR renderInfo,
		[M68kRegister(M68kRegister.D0)] uint flags,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.BeginRefresh(library, renderInfo, flags) ? 1 : 0;

	[M68kExport(EndRefreshExport)]
	public static void EndRefresh(
		[M68kRegister(M68kRegister.A0)] APTR renderInfo,
		[M68kRegister(M68kRegister.D0)] uint flags,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.EndRefresh(library, renderInfo, flags);

	[M68kExport(GetRGBColorExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static int GetRGBColor(
		[M68kRegister(M68kRegister.A0)] APTR renderInfo,
		[M68kRegister(M68kRegister.A1)] APTR penSpec,
		[M68kRegister(M68kRegister.A2)] APTR rgbColor,
		[M68kRegister(M68kRegister.A6)] APTR library) =>
		MuiNativePublicClassGateway.GetRGBColor(library, renderInfo, penSpec,
			rgbColor) ? 1 : 0;

	private static bool TryReadLive(ref MuiExecLibraryMemory memory, APTR library,
		out MuiExecLibraryBaseRecord record) =>
		MuiExecLibraryBaseCodec.TryRead(ref memory, library, out record) &&
		record.Header.Node.Type == (byte)NodeType.Library &&
		record.Header.PositiveSize == MuiExecLibraryBaseRecord.PositiveBytes &&
		record.Header.NegativeSize == MuiExecLibraryBaseRecord.NegativeBytes &&
		!record.ExecBase.IsNull && !record.PrivateRoot.IsNull;

	private static BPTR ExpungeLocked(ref MuiExecLibraryMemory memory,
		APTR library, MuiExecLibraryBaseRecord record)
	{
		var ownerPlatform = default(MuiExecClassOwnerPlatform);
		if (record.Header.OpenCount != 0 ||
			!MuiNativeClassOwnerCore.CanDetach(ref ownerPlatform, library, record.PrivateRoot) ||
			record.Header.Node.Successor.IsNull || record.Header.Node.Predecessor.IsNull)
		{
			ExecLibraryCodec.WriteFlags(ref memory, library, record.Header.Flags | LibraryFlags.DelayedExpunge);
			return BPTR.Null;
		}
		// Only empty allocation-backed state reaches this path. Provider leases
		// retire earlier in normal task context; allocator-origin Expunge may
		// inherit Exec's Forbid and must never initiate a waiting loader call.
		if (!MuiNativeClassOwnerCore.TryDetach(ref ownerPlatform, library, record.PrivateRoot) ||
			!MuiExecPrivateRootOwner.CanDestroy(record.PrivateRoot))
		{
			ExecLibraryCodec.WriteFlags(ref memory, library, record.Header.Flags | LibraryFlags.DelayedExpunge);
			return BPTR.Null;
		}
		var segment = record.SegmentList;
		Exec.Remove(library);
		MuiExecPrivateRootOwner.ReleaseValidated(ref record.PrivateRoot);
		ReleasePreparedBase(library, record.Header);
		return segment;
	}

	private static void ReleasePreparedBase(APTR library, Library header)
	{
		var bytes = (uint)header.NegativeSize + header.PositiveSize;
		if (library.IsNull || library.Raw < header.NegativeSize || bytes == 0) return;
		var storage = APTR.FromPointer(library.Raw - header.NegativeSize);
		Exec.FreeMem(storage, bytes);
	}
}
