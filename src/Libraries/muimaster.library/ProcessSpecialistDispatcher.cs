/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiProcessSpecialistMethodMessage
{
	public const uint Size = 4;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiProcessSpecialistGetMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiProcessSpecialistSetMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiProcessSpecialistSignalMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Signals;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiProcessSpecialistErrorMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint ErrorCode;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiProcessSpecialistDispatchMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Packet;
}

// Routes MG09 Process.mui / Slave.mui method packets to the guest-resident
// process/slave core. This dispatcher is standalone: it operates on a validated
// process/slave instance block and never chains into the frozen common-control,
// collection, menu, pen/color or generic dispatchers, so those frozen cores and
// dispatchers are left unmodified. A method is only claimed when the target
// instance is a valid process/slave specialist; everything else returns "not
// claimed" so an outer router (if any) can continue without a Specialist ->
// Common recursion.
//
// The set/get packets follow the established single-tag convention used across
// the library (method id, attribute id, value) plus the BOOPSI OM_GET storage
// form. The Semaphore.mui methods inherited by both classes are routed straight
// to the frozen MuiSemaphoreCore over the instance's own semaphore fields.
public static class MuiProcessSpecialistDispatcher
{
	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR instance, APTR message) where TPlatform : struct, IMuiServicePlatform
	{
		return TryDispatch(ref platform, state, instance, message, out var result)
			? result : 0u;
	}

	public static bool TryDispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR instance, APTR message, out uint result)
		where TPlatform : struct, IMuiServicePlatform
	{
		result = 0;
		if (!MuiProcessSpecialistMessageCodec.TryReadMethodId(ref platform, message,
			out var methodHeader) ||
			!MuiProcessSpecialistCore.Valid(ref platform, state, instance))
			return false;
		var method = methodHeader.MethodId;

		switch (method)
		{
			case MuiProcessSpecialistMessageCodec.OmDispose:
				if (!MuiProcessSpecialistMessageCodec.IsValidMethod(ref platform,
					message, MuiProcessSpecialistMessageCodec.OmDispose))
					return true;
				result = MuiProcessSpecialistLifecycle.Dispose(ref platform, state,
					instance) ? 1u : 0u;
				return true;

			case MuiProcessSpecialistMessageCodec.OmGet:
				if (!MuiProcessSpecialistMessageCodec.TryReadGet(ref platform, message,
					out var getPacket)) return true;
				var storage = APTR.FromPointer(getPacket.Storage);
				if (MuiProcessSpecialistCore.GetAttribute(ref platform, state,
					instance, getPacket.Attribute, out var value) &&
					storage.IsNotNull && platform.IsMapped(storage,
						MuiGuestUlongStorage.Size))
				{
					MuiGuestUlongStorageCodec.WriteValue(ref platform, storage, value);
					result = 1u;
				}
				return true;

			case MuiProcessSpecialistMessageCodec.MethodSet:
			case MuiProcessSpecialistMessageCodec.MethodNoNotifySet:
				if (!MuiProcessSpecialistMessageCodec.TryReadSet(ref platform, message,
					method,
					out var setPacket)) return true;
				result = MuiProcessSpecialistCore.SetAttribute(ref platform, state,
					instance, setPacket.Attribute, setPacket.Value, false,
					method == MuiProcessSpecialistMessageCodec.MethodSet,
					out _) ? 1u : 0u;
				return true;

			// ---- Process.mui methods --------------------------------------------
			case MuiProcessAttributes.Process_Launch:
				if (!MuiProcessSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiProcessAttributes.Process_Launch, out _)) return true;
				result = MuiProcessSpecialistCore.Launch(ref platform, state,
					instance) ? 1u : 0u;
				return true;

			case MuiProcessAttributes.Process_Kill:
				if (!MuiProcessSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiProcessAttributes.Process_Kill, out _)) return true;
				result = MuiProcessSpecialistCore.Kill(ref platform, state, instance)
					? 1u : 0u;
				return true;

			case MuiProcessAttributes.Process_Process:
				if (!MuiProcessSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiProcessAttributes.Process_Process, out _)) return true;
				result = MuiProcessSpecialistCore.Process(ref platform, state,
					instance);
				return true;

			case MuiProcessAttributes.Process_Signal:
				// MUIM_Process_Signal(ULONG sigs): { ULONG MethodID; ULONG sigs }.
				if (!MuiProcessSpecialistMessageCodec.TryReadSignal(ref platform, message,
					MuiProcessAttributes.Process_Signal, out var signalPacket)) return true;
				result = MuiProcessSpecialistCore.Signal(ref platform, state,
					instance, signalPacket.Signals) ? 1u : 0u;
				return true;

			// ---- Slave.mui methods ----------------------------------------------
			case MuiProcessAttributes.Slave_Setup:
				if (!MuiProcessSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiProcessAttributes.Slave_Setup, out _)) return true;
				result = MuiProcessSpecialistCore.Setup(ref platform, state, instance)
					? 1u : 0u;
				return true;

			case MuiProcessAttributes.Slave_Cleanup:
				if (!MuiProcessSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiProcessAttributes.Slave_Cleanup, out _)) return true;
				result = MuiProcessSpecialistCore.Cleanup(ref platform, state,
					instance) ? 1u : 0u;
				return true;

			case MuiProcessAttributes.Slave_Dispatch:
				// MUIM_Slave_Dispatch: { ULONG MethodID; ULONG *packet }. The packet
				// is the bounded automagic frame { argCount; methodId; args... }.
				if (!MuiProcessSpecialistMessageCodec.TryReadDispatch(ref platform,
					message, out var dispatchPacket))
					return true;
				var packet = APTR.FromPointer(dispatchPacket.Packet);
				result = MuiProcessSpecialistCore.Dispatch(ref platform, state,
					instance, packet, out var dispatchResult) ? dispatchResult : 0u;
				return true;

			case MuiProcessAttributes.Slave_Error:
				// MUIM_Slave_Error(LONG num): { ULONG MethodID; LONG num }.
				if (!MuiProcessSpecialistMessageCodec.TryReadError(ref platform, message,
					out var errorPacket)) return true;
				result = MuiProcessSpecialistCore.Error(ref platform, state, instance,
					errorPacket.ErrorCode, out var stored) ? stored : 0u;
				return true;

			case MuiProcessAttributes.Slave_SignalsReceived:
				// MUIM_Slave_SignalsReceived(ULONG sigs):
				//   { ULONG MethodID; ULONG sigs }.
				if (!MuiProcessSpecialistMessageCodec.TryReadSignal(ref platform, message,
					MuiProcessAttributes.Slave_SignalsReceived, out var receivedPacket))
					return true;
				result = MuiProcessSpecialistCore.SignalsReceived(ref platform, state,
					instance, receivedPacket.Signals);
				return true;

			// ---- Semaphore.mui methods (shared superclass) ----------------------
			case MuiProcessAttributes.Semaphore_Attempt:
				if (!MuiProcessSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiProcessAttributes.Semaphore_Attempt, out _)) return true;
				result = MuiSemaphoreCore.Attempt(ref platform, state, instance)
					? 1u : 0u;
				return true;
			case MuiProcessAttributes.Semaphore_AttemptShared:
				if (!MuiProcessSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiProcessAttributes.Semaphore_AttemptShared, out _)) return true;
				result = MuiSemaphoreCore.AttemptShared(ref platform, state, instance)
					? 1u : 0u;
				return true;
			case MuiProcessAttributes.Semaphore_Obtain:
				if (!MuiProcessSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiProcessAttributes.Semaphore_Obtain, out _)) return true;
				result = MuiSemaphoreCore.Obtain(ref platform, state, instance)
					? 1u : 0u;
				return true;
			case MuiProcessAttributes.Semaphore_ObtainShared:
				if (!MuiProcessSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiProcessAttributes.Semaphore_ObtainShared, out _)) return true;
				result = MuiSemaphoreCore.ObtainShared(ref platform, state, instance)
					? 1u : 0u;
				return true;
			case MuiProcessAttributes.Semaphore_Release:
				if (!MuiProcessSpecialistMessageCodec.TryReadMethod(ref platform, message,
					MuiProcessAttributes.Semaphore_Release, out _)) return true;
				result = MuiSemaphoreCore.Release(ref platform, state, instance)
					? 1u : 0u;
				return true;
		}
		return false;
	}
}
