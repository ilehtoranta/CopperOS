/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

internal interface IMuiServiceGatePlatform : IMuiGuestMemory
{
	APTR CurrentTask();
	void ObtainGate(APTR gate);
	void ReleaseGate(APTR gate);
}

// Transfer the same trusted token, never copy it to create another reference.
// The operation pin remains owned while waiting and after gate release, until
// the caller explicitly returns it to the provider owner.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeServiceLease
{
	internal const uint Size = MuiNativeClassLease.Size + 8;
	internal MuiNativeClassLease Operation;
	internal APTR Gate;
	internal APTR OwnerTask;
}

internal static class MuiNativeServiceAccessCore
{
	internal static bool IsHeld<T>(ref T memory, MuiNativeServiceLease service, APTR currentTask)
		where T : struct, IMuiGuestMemory =>
		service.OwnerTask.IsNotNull && currentTask == service.OwnerTask &&
		TryReadOperation(ref memory, service.Operation, out var gate, out var owner) &&
		gate == service.Gate && owner.ServiceGate.Owner == currentTask && owner.ServiceGate.NestCount > 0;

	internal static bool TryEnter<T>(ref T platform, ref MuiNativeClassLease operation,
		out MuiNativeServiceLease service) where T : struct, IMuiServiceGatePlatform
	{
		service = default;
		var task = platform.CurrentTask();
		if (task.IsNull || !TryReadOperation(ref platform, operation, out var gate, out var owner) ||
			owner.ServiceGate.WaitQueue.Head.IsNull ||
			(owner.ServiceGate.Owner == task && owner.ServiceGate.NestCount == short.MaxValue)) return false;
		// The existing operation pin protects storage across a scheduler wait.
		// ObtainGate returns only after this task owns one further nesting level.
		platform.ObtainGate(gate);
		if (platform.CurrentTask() != task ||
			!TryReadOperation(ref platform, operation, out var freshGate, out owner) || freshGate != gate ||
			owner.ServiceGate.Owner != task || owner.ServiceGate.NestCount <= 0)
		{
			platform.ReleaseGate(gate);
			return false; // Original provider token remains available to its caller.
		}
		service.Operation = operation;
		service.Gate = gate;
		service.OwnerTask = task;
		operation = default;
		return true;
	}

	internal static bool TryLeave<T>(ref T platform, ref MuiNativeServiceLease service,
		out MuiNativeClassLease operation) where T : struct, IMuiServiceGatePlatform
	{
		operation = default;
		if (service.OwnerTask.IsNull || platform.CurrentTask() != service.OwnerTask ||
			!TryReadOperation(ref platform, service.Operation, out var gate, out var owner) ||
			gate != service.Gate || owner.ServiceGate.Owner != service.OwnerTask || owner.ServiceGate.NestCount <= 0)
			return false;
		var retained = service.Operation;
		service = default;
		platform.ReleaseGate(gate);
		operation = retained;
		return true;
	}

	private static bool TryReadOperation<T>(ref T memory, MuiNativeClassLease operation,
		out APTR gate, out MuiNativeClassOwnerRecord value) where T : struct, IMuiGuestMemory
	{
		gate = APTR.Null;
		value = default;
		return operation.Owner.IsNotNull && operation.RegistryGeneration != 0 &&
			MuiNativeClassOwnerCore.TryReadAttached(ref memory, operation.LibraryBase, operation.OwnerRoot,
				out _, out var owner, out value) && owner == operation.Owner &&
			value.RegistryGeneration == operation.RegistryGeneration &&
			value.Phase == MuiNativeClassLeaseCore.PhaseReady && value.ActiveOperations != 0 &&
			operation.Providers.IsComplete && value.UtilityBase == operation.Providers.UtilityBase &&
			value.DosBase == operation.Providers.DosBase && value.GraphicsBase == operation.Providers.GraphicsBase &&
			value.KeymapBase == operation.KeymapBase &&
			value.Context.IntuitionBase == operation.Providers.IntuitionBase &&
			MuiNativeClassOwnerCodec.TryGetServiceAddress(ref memory, owner, out var service) &&
			service == operation.Service && MuiNativeClassOwnerCodec.TryGetGateAddress(ref memory, owner, out gate);
	}
}

// SDK calls only; scheduler behavior stays in Exec. The scalar ABI carrier is
// the admitted Exec base used for named current-task reads.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeServiceGatePlatform : IMuiServiceGatePlatform
{
	internal APTR ExecBase;
	public APTR CurrentTask() => ExecBaseCodec.ReadThisTask(ref this, ExecBase);
	public void ObtainGate(APTR gate) => Exec.ObtainSemaphore(gate);
	public void ReleaseGate(APTR gate) => Exec.ReleaseSemaphore(gate);
	public bool IsMapped(APTR address, uint size) => default(MuiNativeClassMemory).IsMapped(address, size);
	public byte ReadUInt8(APTR address, int offset = 0) => APTR.ReadUInt8(address, offset);
	public ushort ReadUInt16(APTR address, int offset = 0) => APTR.ReadUInt16(address, offset);
	public uint ReadUInt32(APTR address, int offset = 0) => APTR.ReadUInt32(address, offset);
	public void WriteUInt8(APTR address, int offset, byte value) => APTR.WriteUInt8(address, offset, value);
	public void WriteUInt16(APTR address, int offset, ushort value) => APTR.WriteUInt16(address, offset, value);
	public void WriteUInt32(APTR address, int offset, uint value) => APTR.WriteUInt32(address, offset, value);
	public void Clear(APTR address, uint size) => default(MuiNativeClassMemory).Clear(address, size);
	public void Copy(APTR source, APTR destination, uint size) => default(MuiNativeClassMemory).Copy(source, destination, size);
}
