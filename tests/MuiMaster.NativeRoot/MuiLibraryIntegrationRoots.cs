using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.MuiMaster.NativeRoot;

// The library exports live in a third image. Only Exec calls enter that image;
// neither its ordinary entry nor a client-local copy of its lifecycle runs.
public static class MuiLibraryIntegrationRoots
{
	public const uint ResidentAddress = 0x00032000;
	public const uint AutoInitAddress = 0x00032040;
	public const uint FunctionTableAddress = 0x00032060;
	// The complete -30..-756 public table occupies 504 bytes plus the
	// terminator. Keep resident strings outside that named table block.
	public const uint NameAddress = 0x00032280;
	public const uint IdAddress = 0x000322C0;
	public const uint SegmentToken = 0x00012000;
	public const uint LibraryPositiveBytes = MuiExecLibraryBaseRecord.PositiveBytes;
	public const uint LibraryOwnerBytes = MuiNativeClassOwnerRecord.Size;

	public static uint LibraryImageEntry() => uint.MaxValue;

	public static uint LibraryLifecycle() => RunLifecycle(
		APTR.FromPointer(ResidentAddress), CString.FromPointer(NameAddress));

	public const string PackagedExport = "copperos.mui.test.packaged-lifecycle";

	// This callable client starts cold as well. The only host inputs are the
	// relocated HUNK extent; its Resident, AUTOINIT and name are actual artifact
	// bytes, discovered here by guest code rather than synthesized by a fixture.
	[M68kExport(PackagedExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint PackagedLibraryLifecycle(
		[M68kRegister(M68kRegister.A0)] APTR image,
		[M68kRegister(M68kRegister.D0)] uint imageBytes)
	{
		if (image.IsNull || (image.Raw & 1) != 0 || imageBytes < Resident.Size ||
			imageBytes > 0x00100000 || image.Raw > uint.MaxValue - imageBytes)
			return 171;
		var memory = new PackagedImageMemory { Image = image, Bytes = imageBytes };
		var found = APTR.Null;
		var name = APTR.Null;
		for (uint offset = 0; offset <= imageBytes - Resident.Size; offset += 2)
		{
			var address = APTR.FromPointer(image.Raw + offset);
			if (memory.ReadUInt16(address, 0) != 0x4AFC) continue;
			var resident = ExecResidentCodec.Read(ref memory, address);
			if (resident.MatchTag != address) continue;
			if (found.IsNotNull || resident.Flags != ResidentFlags.AutoInit ||
				resident.Type != (byte)NodeType.Library || resident.Version != 0 ||
				resident.EndSkip.Raw <= address.Raw ||
				resident.EndSkip.Raw > image.Raw + imageBytes ||
				(resident.Init.Raw & 1) != 0 || !memory.IsMapped(resident.Init, ResidentAutoInit.Size) ||
				!HasTerminatedText(ref memory, resident.Name.Address) ||
				!HasTerminatedText(ref memory, resident.IdString.Address)) return 172;
			var autoInit = ExecResidentAutoInitCodec.Read(ref memory, resident.Init);
			if (autoInit.DataSize != LibraryPositiveBytes ||
				autoInit.StructureTable.IsNotNull || (autoInit.InitFunction.Raw & 1) != 0 ||
				!memory.IsMapped(autoInit.InitFunction, 2) ||
				(autoInit.FunctionTable.Raw & 1) != 0 ||
				!memory.IsMapped(autoInit.FunctionTable,
					(uint)MuiResidentMetadata.FunctionTableEntryCount * sizeof(uint) + sizeof(uint))) return 173;
			for (var index = 0; index < MuiResidentMetadata.FunctionTableEntryCount; index++)
			{
				var target = memory.ReadUInt32(autoInit.FunctionTable, index * 4);
				if ((target & 1) != 0 || !memory.IsMapped(APTR.FromPointer(target), 2)) return 174;
			}
			if (memory.ReadUInt32(autoInit.FunctionTable,
				MuiResidentMetadata.FunctionTableEntryCount * 4) != uint.MaxValue)
				return 175;
			found = address;
			name = resident.Name.Address;
		}
		if (found.IsNull) return 176;
		return RunLifecycle(found, CString.FromPointer(name.Raw));
	}

	private static bool HasTerminatedText(ref PackagedImageMemory memory, APTR text)
	{
		for (var index = 0; index < 128; index++)
		{
			var address = APTR.FromPointer(text.Raw + (uint)index);
			if (!memory.IsMapped(address, 1)) return false;
			if (memory.ReadUInt8(address, 0) == 0) return index != 0;
		}
		return false;
	}

	private static uint RunLifecycle(APTR resident, CString name)
	{
		var flags = Exec.MemoryFlags.Public;
		var available = Exec.AvailMem(flags);
		var largest = Exec.AvailMem(flags | Exec.MemoryFlags.Largest);
		var segment = BPTR.FromRaw(SegmentToken);
		if (available == 0 || largest == 0 ||
			!Exec.OpenLibraryRaw(name, 0).IsNull) return 101;
		var library = Exec.InitResident(resident, segment);
		if (library.IsNull) return 102;
		var memory = default(MuiExecLibraryMemory);
		memory.Base = library;
		if (!MuiExecLibraryBaseCodec.TryRead(ref memory, library, out var record) ||
			record.PrivateRoot.IsNull || record.SegmentList.Raw != SegmentToken ||
			record.ExecBase.Raw != APTR.ReadUInt32(APTR.FromPointer(4), 0) ||
			record.Header.OpenCount != 0 ||
			record.Header.NegativeSize != MuiExecLibraryBaseRecord.NegativeBytes ||
			record.Header.PositiveSize != MuiExecLibraryBaseRecord.PositiveBytes ||
			record.Header.Node.Successor.IsNull || record.Header.Node.Predecessor.IsNull)
			return 103;
		if (!CheckAttachedOwner(library, record.PrivateRoot)) return 134;
		// Error vectors are DOS aliases. The isolated lifecycle kernel has no
		// dos.library, so it can prove the absent-provider result but not task
		// IoErr persistence; retain the round-trip when a DOS provider is present.
		var dos = Exec.OpenLibraryRaw(CString.FromLiteral("dos.library"), 0);
		if (dos.IsNull)
		{
			if (InvokeError(APTR.FromPointer(library.Raw - 66), library) != 0)
				return 136;
			InvokeSetError(APTR.FromPointer(library.Raw - 72), 7, library);
			if (InvokeError(APTR.FromPointer(library.Raw - 66), library) != 0)
				return 137;
		}
		else
		{
			Exec.CloseLibrary(dos);
			if (InvokeError(APTR.FromPointer(library.Raw - 66), library) != 0 ||
				InvokeSetError(APTR.FromPointer(library.Raw - 72), 7, library) != 0 ||
				InvokeError(APTR.FromPointer(library.Raw - 66), library) != 7 ||
				InvokeSetError(APTR.FromPointer(library.Raw - 72), 0, library) != 7 ||
				InvokeError(APTR.FromPointer(library.Raw - 66), library) != 0)
				return 138;
		}
		// No provider is open in this cold fixture. The MakeObjectA vector must
		// therefore admit the call boundary and fail closed without allocation.
		if (InvokeMakeObjectA(APTR.FromPointer(library.Raw - 120),
			MuiMakeObjectServiceCore.MUIO_HSpace, APTR.Null, library).IsNotNull)
			return 139;
		var unboundObject = APTR.FromPointer(0x00005000);
		InvokeRequestIDCMP(APTR.FromPointer(library.Raw - 90), unboundObject,
			0x00000200, library);
		InvokeRejectIDCMP(APTR.FromPointer(library.Raw - 96), unboundObject,
			0x00000200, library);
		if (!Exec.OpenLibraryRaw(name, 65535).IsNull ||
			ExecLibraryCodec.ReadOpenCount(ref memory, library) != 0) return 104;
		if (Exec.OpenLibraryRaw(name, 0) != library) return 1051;
		if (Exec.OpenLibraryRaw(name, 0) != library) return 1052;
		if (ExecLibraryCodec.ReadOpenCount(ref memory, library) != 2) return 1053;
		Exec.RemLibrary(library);
		if (ExecLibraryCodec.ReadOpenCount(ref memory, library) != 2 ||
			(ExecLibraryCodec.ReadFlags(ref memory, library) & LibraryFlags.DelayedExpunge) == 0)
			return 106;
		if (Exec.OpenLibraryRaw(name, 0) != library ||
			ExecLibraryCodec.ReadOpenCount(ref memory, library) != 3 ||
			(ExecLibraryCodec.ReadFlags(ref memory, library) & LibraryFlags.DelayedExpunge) != 0)
			return 107;
		Exec.CloseLibrary(library);
		Exec.CloseLibrary(library);
		if (ExecLibraryCodec.ReadOpenCount(ref memory, library) != 1) return 108;
		Exec.RemLibrary(library);
		if (InvokeLifecycle(APTR.FromPointer(library.Raw - 12), library) != SegmentToken)
			return 109;
		if (!Exec.OpenLibraryRaw(name, 0).IsNull ||
			Exec.AvailMem(flags) != available ||
			Exec.AvailMem(flags | Exec.MemoryFlags.Largest) != largest) return 110;

		// A live private-root operation keeps the library linked and allocated.
		library = Exec.InitResident(resident, segment);
		if (library.IsNull) return 111;
		memory.Base = library;
		if (!MuiExecLibraryBaseCodec.TryRead(ref memory, library, out record)) return 112;
		var rootMemory = default(MuiExecPrivateRootMemory);
		rootMemory.Root = record.PrivateRoot;
		if (!MuiMasterPrivateRootCodec.TryRead(ref rootMemory, record.PrivateRoot, out var root))
			return 113;
		root.ActiveDispatchDepth = 1;
		if (!MuiMasterPrivateRootCodec.Write(ref rootMemory, record.PrivateRoot, root)) return 114;
		var allocated = Exec.AvailMem(flags);
		Exec.RemLibrary(library);
		if (Exec.AvailMem(flags) != allocated ||
			(ExecLibraryCodec.ReadFlags(ref memory, library) & LibraryFlags.DelayedExpunge) == 0 ||
			Exec.OpenLibraryRaw(name, 0) != library) return 115;
		Exec.CloseLibrary(library);
		root.ActiveDispatchDepth = 0;
		if (!MuiMasterPrivateRootCodec.Write(ref rootMemory, record.PrivateRoot, root)) return 116;
		if (!CheckBusyOwner(library, record.PrivateRoot)) return 135;
		if (InvokeLifecycle(APTR.FromPointer(library.Raw - 18), library) != SegmentToken ||
			!Exec.OpenLibraryRaw(name, 0).IsNull || Exec.AvailMem(flags) != available ||
			Exec.AvailMem(flags | Exec.MemoryFlags.Largest) != largest) return 117;

		// Also return through Exec itself after the MUI base was freed, covering
		// both its CloseLibrary callback and its accepted RemLibrary callback.
		for (var operation = 0; operation < 2; operation++)
		{
			library = Exec.InitResident(resident, segment);
			if (library.IsNull) return 125;
			memory.Base = library;
			if (InvokeLifecycle(APTR.FromPointer(library.Raw - 12), library) != 0 ||
				ExecLibraryCodec.ReadOpenCount(ref memory, library) != 0) return 126;
			ExecLibraryCodec.WriteOpenCount(ref memory, library, ushort.MaxValue);
			if (!Exec.OpenLibraryRaw(name, 0).IsNull ||
				ExecLibraryCodec.ReadOpenCount(ref memory, library) != ushort.MaxValue) return 127;
			ExecLibraryCodec.WriteOpenCount(ref memory, library, 0);
			if (operation == 0)
			{
				if (Exec.OpenLibraryRaw(name, 0) != library) return 128;
				Exec.RemLibrary(library);
				Exec.CloseLibrary(library);
			}
			else Exec.RemLibrary(library);
			if (!Exec.OpenLibraryRaw(name, 0).IsNull || Exec.AvailMem(flags) != available ||
				Exec.AvailMem(flags | Exec.MemoryFlags.Largest) != largest) return 129;
		}

		// Failure before the initializer: no library allocation fits.
		var held = Exec.AllocMem(largest, flags);
		if (held.IsNull) return 118;
		var exhausted = Exec.AvailMem(flags);
		if (!Exec.InitResident(resident, segment).IsNull ||
			Exec.AvailMem(flags) != exhausted || !Exec.OpenLibraryRaw(name, 0).IsNull)
			return 119;
		Exec.FreeMem(held, largest);
		if (Exec.AvailMem(flags) != available) return 120;

		// Reserve all other memory, leaving exactly the base allocation free.
		// AUTOINIT can allocate its vectors/base, but the private root cannot fit.
		var baseBytes = (uint)MuiExecLibraryBaseRecord.NegativeBytes +
			MuiExecLibraryBaseRecord.PositiveBytes;
		var baseRoom = Exec.AllocMem(baseBytes, flags);
		if (baseRoom.IsNull) return 121;
		var remaining = Exec.AvailMem(flags | Exec.MemoryFlags.Largest);
		held = Exec.AllocMem(remaining, flags);
		if (held.IsNull) return 122;
		Exec.FreeMem(baseRoom, baseBytes);
		var baseOnly = Exec.AvailMem(flags);
		if (baseOnly == 0 || !Exec.InitResident(resident, segment).IsNull ||
			Exec.AvailMem(flags) != baseOnly || !Exec.OpenLibraryRaw(name, 0).IsNull)
			return 123;
		Exec.FreeMem(held, remaining);
		if (Exec.AvailMem(flags) != available ||
			Exec.AvailMem(flags | Exec.MemoryFlags.Largest) != largest) return 124;

		// Leave enough actual allocator storage for both base and private root,
		// but none for the additional class-owner allocation. The runner observes
		// the real owner-sized AllocMem attempt to distinguish this from the
		// preceding root-allocation failure without assuming allocator rounding.
		baseRoom = Exec.AllocMem(baseBytes, flags);
		var rootRoom = Exec.AllocMem(MuiMasterPrivateRoot.Size, flags);
		if (baseRoom.IsNull || rootRoom.IsNull) return 130;
		remaining = Exec.AvailMem(flags | Exec.MemoryFlags.Largest);
		held = Exec.AllocMem(remaining, flags);
		if (held.IsNull) return 131;
		Exec.FreeMem(baseRoom, baseBytes);
		Exec.FreeMem(rootRoom, MuiMasterPrivateRoot.Size);
		var baseAndRootOnly = Exec.AvailMem(flags);
		var baseAndRootLargest = Exec.AvailMem(flags | Exec.MemoryFlags.Largest);
		if (baseAndRootOnly == 0 || !Exec.InitResident(resident, segment).IsNull ||
			Exec.AvailMem(flags) != baseAndRootOnly ||
			Exec.AvailMem(flags | Exec.MemoryFlags.Largest) != baseAndRootLargest ||
			!Exec.OpenLibraryRaw(name, 0).IsNull) return 132;
		Exec.FreeMem(held, remaining);
		if (Exec.AvailMem(flags) != available ||
			Exec.AvailMem(flags | Exec.MemoryFlags.Largest) != largest) return 133;
		return 42;
	}

	private static bool CheckAttachedOwner(APTR library, APTR rootAddress)
	{
		var memory = default(MuiNativeClassMemory);
		if (!MuiMasterPrivateRootCodec.TryRead(ref memory, rootAddress, out var root)) return false;
		var owner = APTR.FromPointer(root.LoaderState);
		if (owner.IsNull || root.CallbackState != owner.Raw ||
			!MuiNativeClassOwnerCodec.TryRead(ref memory, owner, out var value) ||
			!MuiNativeClassOwnerCodec.TryGetServiceAddress(ref memory, owner, out var service) ||
			!MuiNativeClassOwnerCodec.TryGetRegistryAddress(ref memory, owner, out var registry) ||
			!MuiNativeClassContextCodec.TryRead(ref memory, owner, out var context)) return false;
		return value.Magic == MuiNativeClassOwnerCore.MagicValue &&
			value.Version == MuiNativeClassOwnerCore.Version && value.LibraryBase == library &&
			value.RegistryGeneration == root.RegistryGeneration &&
			value.Phase == MuiNativeClassOwnerCore.PhaseEmpty && value.ActiveOperations == 0 &&
			context.OwnerRoot == rootAddress && context.IntuitionBase.IsNull &&
			value.Context.OwnerRoot == rootAddress && value.Context.IntuitionBase.IsNull &&
			value.UtilityBase.IsNull && value.DosBase.IsNull && value.GraphicsBase.IsNull &&
			value.KeymapBase.IsNull &&
			service.IsNotNull && registry.IsNotNull &&
			service != registry &&
			root.ClassRegistry == registry.Raw &&
			value.Service.Magic == MuiClassServiceLayout.Magic && value.Service.Head.IsNull &&
			value.Service.Headless == registry && value.Service.Generation != 0 &&
			value.Registry.Magic == MuiHeadlessLayout.Magic && value.Registry.Version == MuiHeadlessLayout.Version &&
			value.Registry.Classes.IsNull && value.Registry.Objects.IsNull && value.Registry.NotifyDepth == 0 &&
			value.Registry.Reserved == 0 &&
			value.ServiceGate.WaitQueue.Head.IsNotNull && value.ServiceGate.WaitQueue.TailPred.IsNotNull &&
			MuiNativeClassOwnerCodec.TryGetGateAddress(ref memory, owner, out var gate) &&
			MuiSignalSemaphoreCodec.IsIdle(gate, value.ServiceGate);
	}

	private static bool CheckBusyOwner(APTR library, APTR rootAddress)
	{
		var memory = default(MuiNativeClassMemory);
		if (!CheckAttachedOwner(library, rootAddress) ||
			!MuiMasterPrivateRootCodec.TryRead(ref memory, rootAddress, out var originalRoot)) return false;
		var owner = APTR.FromPointer(originalRoot.LoaderState);
		if (!MuiNativeClassOwnerCodec.TryRead(ref memory, owner, out var original)) return false;
		var available = Exec.AvailMem(Exec.MemoryFlags.Public);
		var largest = Exec.AvailMem(Exec.MemoryFlags.Public | Exec.MemoryFlags.Largest);
		for (uint guard = 0; guard < 9; guard++)
		{
			var busy = original;
			var busyRoot = originalRoot;
			if (guard == 0) busy.ActiveOperations = 1;
			else if (guard == 1) busy.Service.Head = owner;
			else if (guard == 2) busy.Registry.Classes = owner;
			else if (guard == 3) busy.Registry.Objects = owner;
			else if (guard == 4) busy.Registry.NotifyDepth = 1;
			else if (guard == 5) busy.Registry.Reserved = BOOPSI.OM_SET;
			else if (guard == 6) busyRoot.ActiveCallbackDepth = 1;
			else if (guard == 7) busyRoot.ExternalClassHead = owner.Raw;
			else busyRoot.ApplicationHead = owner.Raw;
			if (!MuiNativeClassOwnerCodec.Write(ref memory, owner, busy) ||
				!MuiMasterPrivateRootCodec.Write(ref memory, rootAddress, busyRoot)) return false;
			Exec.RemLibrary(library);
			var header = ExecLibraryCodec.Read(ref memory, library);
			if (Exec.AvailMem(Exec.MemoryFlags.Public) != available ||
				Exec.AvailMem(Exec.MemoryFlags.Public | Exec.MemoryFlags.Largest) != largest ||
				header.OpenCount != 0 || header.Node.Successor.IsNull || header.Node.Predecessor.IsNull ||
				(header.Flags & LibraryFlags.DelayedExpunge) == 0 ||
				!MuiMasterPrivateRootCodec.TryRead(ref memory, rootAddress, out var after) ||
				after.LoaderState != owner.Raw || after.CallbackState != owner.Raw ||
				after.ClassRegistry != originalRoot.ClassRegistry) return false;
		}
		// Sequence/mutation values are cumulative metadata, not active work.
		// They must not prevent otherwise-quiescent final owner destruction.
		original.Registry.NextSequence = 7;
		original.Registry.Mutation = 3;
		return MuiNativeClassOwnerCodec.Write(ref memory, owner, original) &&
			MuiMasterPrivateRootCodec.Write(ref memory, rootAddress, originalRoot) &&
			CheckAttachedOwner(library, rootAddress);
	}

	private struct PackagedImageMemory : IAmigaGuestMemory
	{
		internal APTR Image;
		internal uint Bytes;
		public bool IsMapped(APTR address, uint bytes) => bytes != 0 &&
			address.Raw >= Image.Raw && address.Raw - Image.Raw <= Bytes &&
			bytes <= Bytes - (address.Raw - Image.Raw);
		public byte ReadUInt8(APTR address, int offset) => APTR.ReadUInt8(address, offset);
		public ushort ReadUInt16(APTR address, int offset) => APTR.ReadUInt16(address, offset);
		public uint ReadUInt32(APTR address, int offset) => APTR.ReadUInt32(address, offset);
		public void WriteUInt8(APTR address, int offset, byte value) => APTR.WriteUInt8(address, offset, value);
		public void WriteUInt16(APTR address, int offset, ushort value) => APTR.WriteUInt16(address, offset, value);
		public void WriteUInt32(APTR address, int offset, uint value) => APTR.WriteUInt32(address, offset, value);
		public void Clear(APTR address, uint bytes)
		{
			for (uint index = 0; index < bytes; index++)
				APTR.WriteUInt8(address, (int)index, 0);
		}
		public void Copy(APTR source, APTR destination, uint bytes)
		{
			for (uint index = 0; index < bytes; index++)
				APTR.WriteUInt8(destination, (int)index, APTR.ReadUInt8(source, (int)index));
		}
	}

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint InvokeLifecycle(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern int InvokeError(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern int InvokeSetError(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.D0)] int error,
		[M68kRegister(M68kRegister.A6)] APTR library);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern APTR InvokeMakeObjectA(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.D0)] uint type,
		[M68kRegister(M68kRegister.A0)] APTR parameters,
		[M68kRegister(M68kRegister.A6)] APTR library);

	[AmigaIndirectCall(M68kRegister.A3)]
	private static extern void InvokeRequestIDCMP(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A0)] APTR obj,
		[M68kRegister(M68kRegister.D0)] uint flags,
		[M68kRegister(M68kRegister.A6)] APTR library);

	[AmigaIndirectCall(M68kRegister.A3)]
	private static extern void InvokeRejectIDCMP(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A0)] APTR obj,
		[M68kRegister(M68kRegister.D0)] uint flags,
		[M68kRegister(M68kRegister.A6)] APTR library);
}
