using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.MuiMaster.NativeRoot;

// This adapter is included ONLY in the separate library image of the providers
// fixture. It adds no production export or library vector. The independent
// client below reaches it by its load-time address, never by a managed call.
public static class MuiNativeProviderIntegrationRoots
{
	public const string AdapterExport = "copperos.mui.test.provider-owner";
	public const string ClientExport = "copperos.mui.test.provider-client";

	[M68kExport(AdapterExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint OwnerAdapter([M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.D0)] uint operation)
	{
		if (operation == 1) return MuiNativeProviderOwner.TryRetireIdle(library) ? 1u : 0u;
		if (operation != 0) return 251;
		var result = MuiNativeProviderOwner.Enter(library, out var lease);
		if (result == MuiNativeClassLeaseResult.Acquired)
		{
			// A provider unexpectedly present is not an allowed substitute for
			// this missing-provider scenario. Still consume our actual lease.
			// Include the production service-admission closure without inventing
			// successful provider bases to execute it in this fixture.
			if (MuiNativeProviderOwner.TryEnterService(ref lease, out var service))
			{
				// Compile all owned consumers with invalid class inputs. This branch
				// remains unexpected, not successful-provider execution evidence.
				MuiNativeOwnedClassServices.GetClass(ref service, APTR.Null);
				MuiNativeOwnedClassServices.FreeClass(ref service, APTR.Null);
				MuiNativeOwnedClassServices.CreateCustomClass(ref service, APTR.Null,
					APTR.Null, APTR.Null, 0, APTR.Null);
				MuiNativeOwnedClassServices.DeleteCustomClass(ref service, APTR.Null);
				if (!MuiNativeProviderOwner.TryLeaveService(ref service, out lease)) return 255;
			}
			return MuiNativeProviderOwner.Leave(ref lease) ? 252u : 253u;
		}
		if (lease.LibraryBase.IsNotNull || lease.OwnerRoot.IsNotNull || lease.Owner.IsNotNull ||
			lease.RegistryGeneration != 0 || lease.Service.IsNotNull ||
			lease.Providers.UtilityBase.IsNotNull || lease.Providers.DosBase.IsNotNull ||
			lease.Providers.GraphicsBase.IsNotNull || lease.Providers.IntuitionBase.IsNotNull ||
			lease.KeymapBase.IsNotNull) return 254;
		return (uint)result;
	}

	[M68kExport(ClientExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint Client([M68kRegister(M68kRegister.A0)] APTR adapter)
	{
		var memory = default(MuiNativeClassMemory);
		var execBase = APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(4), 0));
		if (adapter.IsNull || !NormalContext(ref memory, execBase)) return 501;
		var flags = Exec.MemoryFlags.Public;
		var before = Exec.AvailMem(flags);
		var largest = Exec.AvailMem(flags | Exec.MemoryFlags.Largest);
		var library = Exec.InitResident(APTR.FromPointer(MuiLibraryIntegrationRoots.ResidentAddress),
			BPTR.FromRaw(MuiLibraryIntegrationRoots.SegmentToken));
		if (library.IsNull || !CheckEmptyOwner(ref memory, library, out var owner)) return 502;
		var ownedAvailable = Exec.AvailMem(flags);
		var ownedLargest = Exec.AvailMem(flags | Exec.MemoryFlags.Largest);
		// No operation may acquire providers without a held library open.
		if (Invoke(adapter, library, 0) != 0 || Invoke(adapter, APTR.Null, 0) != 0) return 503;
		if (Exec.OpenLibraryRaw(CString.FromPointer(MuiLibraryIntegrationRoots.NameAddress), 0) != library)
			return 504;
		if (!CheckRecursiveGate(ref memory, execBase, owner)) return 512;
		for (var attempt = 0; attempt < 2; attempt++)
		{
			if (Invoke(adapter, library, 0) != (uint)MuiNativeClassLeaseResult.Unavailable) return 505;
			if (!CheckUnchanged(ref memory, execBase, library, owner, ownedAvailable, ownedLargest)) return 506;
		}
		Exec.Forbid();
		var forbidden = Invoke(adapter, library, 0);
		var forbiddenRetire = Invoke(adapter, library, 1);
		var forbidDepth = ExecBaseCodec.ReadTaskDisableNesting(ref memory, execBase);
		Exec.Permit();
		if (forbidden != 0 || forbiddenRetire != 0 || forbidDepth != 0 ||
			!CheckUnchanged(ref memory, execBase, library, owner, ownedAvailable, ownedLargest)) return 507;
		Exec.Disable();
		var disabled = Invoke(adapter, library, 0);
		var disabledRetire = Invoke(adapter, library, 1);
		var disableDepth = ExecBaseCodec.ReadInterruptDisableNesting(ref memory, execBase);
		Exec.Enable();
		if (disabled != 0 || disabledRetire != 0 || disableDepth != 0 ||
			!CheckUnchanged(ref memory, execBase, library, owner, ownedAvailable, ownedLargest)) return 508;
		var originalStatus = Exec.SetSR(0x0100, 0x0700);
		var masked = Invoke(adapter, library, 0);
		var maskedRetire = Invoke(adapter, library, 1);
		var maskedStatus = Exec.SetSR(originalStatus, 0x0700);
		if (masked != 0) return 5091;
		if (maskedRetire != 0) return 5092;
		if ((maskedStatus & 0x0700) != 0x0100) return 0x05090000u | maskedStatus;
		if (!CheckUnchanged(ref memory, execBase, library, owner, ownedAvailable, ownedLargest)) return 5094;
		if (Invoke(adapter, library, 1) != 1 ||
			!CheckUnchanged(ref memory, execBase, library, owner, ownedAvailable, ownedLargest)) return 510;
		Exec.CloseLibrary(library);
		Exec.RemLibrary(library);
		if (Exec.AvailMem(flags) != before || Exec.AvailMem(flags | Exec.MemoryFlags.Largest) != largest ||
			!NormalContext(ref memory, execBase) ||
			Exec.OpenLibraryRaw(CString.FromPointer(MuiLibraryIntegrationRoots.NameAddress), 0).IsNotNull)
			return 511;
		return 42;
	}

	private static bool NormalContext(ref MuiNativeClassMemory memory, APTR execBase) =>
		ExecBaseCodec.ReadInterruptDisableNesting(ref memory, execBase) == -1 &&
		ExecBaseCodec.ReadTaskDisableNesting(ref memory, execBase) == -1 &&
		(Exec.SetSR(0, 0) & 0x2700) == 0;

	private static bool CheckRecursiveGate(ref MuiNativeClassMemory memory, APTR execBase, APTR owner)
	{
		if (!MuiNativeClassOwnerCodec.TryGetGateAddress(ref memory, owner, out var gate) ||
			!MuiSignalSemaphoreCodec.TryRead(ref memory, gate, out var before) ||
			before.WaitQueue.Head.IsNull || !MuiSignalSemaphoreCodec.IsIdle(gate, before)) return false;
		Exec.ObtainSemaphore(gate);
		Exec.ObtainSemaphore(gate);
		var read = MuiSignalSemaphoreCodec.TryRead(ref memory, gate, out var held);
		var nested = read && held.NestCount == 2 && held.Owner == ExecBaseCodec.ReadThisTask(ref memory, execBase);
		Exec.ReleaseSemaphore(gate);
		read = MuiSignalSemaphoreCodec.TryRead(ref memory, gate, out held);
		nested = nested && read && held.NestCount == 1;
		Exec.ReleaseSemaphore(gate);
		return nested && MuiSignalSemaphoreCodec.TryRead(ref memory, gate, out var after) &&
			MuiSignalSemaphoreCodec.IsIdle(gate, after) && NormalContext(ref memory, execBase);
	}

	private static bool CheckUnchanged(ref MuiNativeClassMemory memory, APTR execBase, APTR library,
		APTR owner, uint available, uint largest) =>
		CheckEmptyOwner(ref memory, library, out var currentOwner) && currentOwner == owner &&
		ExecLibraryCodec.ReadOpenCount(ref memory, library) == 1 && NormalContext(ref memory, execBase) &&
		Exec.AvailMem(Exec.MemoryFlags.Public) == available &&
		Exec.AvailMem(Exec.MemoryFlags.Public | Exec.MemoryFlags.Largest) == largest;

	private static bool CheckEmptyOwner(ref MuiNativeClassMemory memory, APTR library, out APTR owner)
	{
		owner = APTR.Null;
		if (!MuiExecLibraryBaseCodec.TryRead(ref memory, library, out var header) ||
			header.Header.Node.Successor.IsNull || header.Header.Node.Predecessor.IsNull ||
			!MuiMasterPrivateRootCodec.TryRead(ref memory, header.PrivateRoot, out var root)) return false;
		var address = APTR.FromPointer(root.LoaderState);
		if (address.IsNull || root.CallbackState != address.Raw ||
			!MuiNativeClassOwnerCodec.TryRead(ref memory, address, out var value) ||
			!MuiNativeClassOwnerCodec.TryGetRegistryAddress(ref memory, address, out var registry) ||
			value.Magic != MuiNativeClassOwnerCore.MagicValue || value.Version != MuiNativeClassOwnerCore.Version ||
			value.LibraryBase != library || value.Context.OwnerRoot != header.PrivateRoot ||
			value.RegistryGeneration != root.RegistryGeneration || root.RegistryGeneration == 0 ||
			value.Phase != MuiNativeClassOwnerCore.PhaseEmpty || value.ActiveOperations != 0 ||
			value.UtilityBase.IsNotNull || value.DosBase.IsNotNull || value.GraphicsBase.IsNotNull ||
			value.KeymapBase.IsNotNull ||
			value.Context.IntuitionBase.IsNotNull || root.ClassRegistry != registry.Raw ||
			value.Service.Headless != registry || value.Service.Head.IsNotNull ||
			value.Registry.Classes.IsNotNull || value.Registry.Objects.IsNotNull ||
			value.Registry.NotifyDepth != 0 || value.Registry.Reserved != 0 ||
			value.ServiceGate.WaitQueue.Head.IsNull ||
			!MuiNativeClassOwnerCodec.TryGetGateAddress(ref memory, address, out var gate) ||
			!MuiSignalSemaphoreCodec.IsIdle(gate, value.ServiceGate) ||
			root.ExternalClassHead != 0 || root.ActiveDispatchDepth != 0 || root.ActiveCallbackDepth != 0)
			return false;
		owner = address;
		return true;
	}

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint Invoke([M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.D0)] uint operation);
}
