using Amiga;

namespace CopperOS.MuiMaster.NativeRoot;

// This client contains MUI and SDK calls only. Its runner supplies a separately
// compiled CopperStart Exec image and checks the real allocator after each run.
public static class MuiExecIntegrationRoots
{
	public static uint PrivateRootLifecycle()
	{
		var flags = Exec.MemoryFlags.Public;
		var available = Exec.AvailMem(flags);
		var largest = Exec.AvailMem(flags | Exec.MemoryFlags.Largest);
		if (available < MuiMasterPrivateRoot.Size || largest == 0) return 1;
		for (var iteration = 0; iteration < 16; iteration++)
		{
			if (!MuiExecPrivateRootOwner.TryCreate(out var root) || root.IsNull)
				return 2;
			var memory = default(MuiExecPrivateRootMemory);
			memory.Root = root;
			if (!memory.IsMapped(root, MuiMasterPrivateRoot.Size) ||
				memory.IsMapped(root, MuiMasterPrivateRoot.Size + 1) ||
				memory.IsMapped(root, 0) || memory.IsMapped(APTR.Null, 4) ||
				memory.IsMapped(APTR.FromPointer(uint.MaxValue - 1), 4)) return 3;
			if (!MuiMasterPrivateRootCodec.TryRead(ref memory, root, out var state) ||
				state.RegistryGeneration != 1 || state.ClassRegistry != 0 ||
				state.AllocationPolicy != 0 || state.ErrorState != 0 ||
				state.ApplicationHead != 0 || state.ExternalClassHead != 0 ||
				state.CallbackState != 0 || state.LoaderState != 0 ||
				state.ActiveDispatchDepth != 0 || state.ActiveCallbackDepth != 0 ||
				state.Flags != 0 || state.Reserved != 0) return 4;
			var allocated = Exec.AvailMem(flags);
			if (allocated >= available) return 5;
			// Active work and owned children prevent premature release.
			state.ActiveDispatchDepth = 1;
			if (!MuiMasterPrivateRootCodec.Write(ref memory, root, state) ||
				MuiExecPrivateRootOwner.TryDestroy(ref root) || root.IsNull ||
				Exec.AvailMem(flags) != allocated) return 6;
			state.ActiveDispatchDepth = 0;
			state.ClassRegistry = 1;
			if (!MuiMasterPrivateRootCodec.Write(ref memory, root, state) ||
				MuiExecPrivateRootOwner.TryDestroy(ref root) || root.IsNull ||
				Exec.AvailMem(flags) != allocated) return 7;
			state.ClassRegistry = 0;
			if (!MuiMasterPrivateRootCodec.Write(ref memory, root, state) ||
				!MuiExecPrivateRootOwner.TryDestroy(ref root) || !root.IsNull ||
				!MuiExecPrivateRootOwner.TryDestroy(ref root)) return 8;
			if (Exec.AvailMem(flags) != available ||
				Exec.AvailMem(flags | Exec.MemoryFlags.Largest) != largest) return 9;
		}
		// Exhaust the real heap, then prove failure publishes no owner and leaks
		// nothing when the held allocation is returned to Exec.
		var held = Exec.AllocMem(largest, flags);
		if (held.IsNull) return 10;
		var created = MuiExecPrivateRootOwner.TryCreate(out var failedRoot);
		if (created) MuiExecPrivateRootOwner.TryDestroy(ref failedRoot);
		Exec.FreeMem(held, largest);
		if (created || !failedRoot.IsNull) return 11;
		if (Exec.AvailMem(flags) != available ||
			Exec.AvailMem(flags | Exec.MemoryFlags.Largest) != largest) return 12;
		return 42;
	}
}
