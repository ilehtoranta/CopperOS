/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// A borrowed view of one live Exec-owned private root. This is an allocation
// claim, not an operating-system address probe. Only the unpublished owner
// and resident lifecycle create this view; it never admits application pointers
// or child data.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExecPrivateRootMemory : IMuiGuestMemory
{
	internal APTR Root;

	public bool IsMapped(APTR address, uint byteSize) =>
		!Root.IsNull && Root.Raw <= uint.MaxValue - MuiMasterPrivateRoot.Size &&
		byteSize != 0 && byteSize <= MuiMasterPrivateRoot.Size &&
		address.Raw >= Root.Raw &&
		address.Raw - Root.Raw <= MuiMasterPrivateRoot.Size - byteSize;

	public byte ReadUInt8(APTR address, int offset = 0) =>
		APTR.ReadUInt8(address, offset);
	public ushort ReadUInt16(APTR address, int offset = 0) =>
		APTR.ReadUInt16(address, offset);
	public uint ReadUInt32(APTR address, int offset = 0) =>
		APTR.ReadUInt32(address, offset);
	public void WriteUInt8(APTR address, int offset, byte value) =>
		APTR.WriteUInt8(address, offset, value);
	public void WriteUInt16(APTR address, int offset, ushort value) =>
		APTR.WriteUInt16(address, offset, value);
	public void WriteUInt32(APTR address, int offset, uint value) =>
		APTR.WriteUInt32(address, offset, value);

	public void Clear(APTR address, uint byteCount)
	{
		if (!IsMapped(address, byteCount)) return;
		for (var index = 0; (uint)index < byteCount; index++)
			APTR.WriteUInt8(address, index, 0);
	}

	public void Copy(APTR source, APTR destination, uint byteCount)
	{
		if (!IsMapped(source, byteCount) || !IsMapped(destination, byteCount))
			return;
		// Both ranges belong to this small record; preserve overlapping copies.
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

// First native library bootstrap boundary. All allocation goes through public
// Exec vectors; no CopperStart implementation is linked into muimaster.
// Ownership transfers to the resident base only after initialization succeeds.
// Destroy requires an unpublished or unlinked, quiescent root created here.
internal static class MuiExecPrivateRootOwner
{
	internal static bool TryCreate(out APTR root)
	{
		root = APTR.Null;
		var allocation = Exec.AllocMem(MuiMasterPrivateRoot.Size,
			Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
		if (allocation.IsNull) return false;
		var memory = default(MuiExecPrivateRootMemory);
		memory.Root = allocation;
		MuiMasterState.InitializeEmptyRoot(out var state);
		if (!MuiMasterPrivateRootCodec.Write(ref memory, allocation, state))
		{
			Exec.FreeMem(allocation, MuiMasterPrivateRoot.Size);
			return false;
		}
		root = allocation;
		return true;
	}

	internal static bool TryDestroy(ref APTR root)
	{
		if (root.IsNull) return true;
		if (!CanDestroy(root)) return false;
		ReleaseValidated(ref root);
		return true;
	}

	internal static bool CanDestroy(APTR root)
	{
		var memory = default(MuiExecPrivateRootMemory);
		memory.Root = root;
		if (!MuiMasterPrivateRootCodec.TryRead(ref memory, root, out var state) ||
			state.RegistryGeneration == 0 || state.ClassRegistry != 0 ||
			state.AllocationPolicy != 0 || state.ErrorState != 0 ||
			state.ApplicationHead != 0 || state.ExternalClassHead != 0 ||
			state.CallbackState != 0 || state.LoaderState != 0 ||
			state.ActiveDispatchDepth != 0 || state.ActiveCallbackDepth != 0 ||
			state.Flags != 0 || state.Reserved != 0) return false;
		return true;
	}

	// Used after CanDestroy while the owner excludes concurrent mutation. The
	// library lifecycle unlinks its Exec node between validation and release.
	internal static void ReleaseValidated(ref APTR root)
	{
		var allocation = root;
		root = APTR.Null;
		Exec.FreeMem(allocation, MuiMasterPrivateRoot.Size);
	}
}
