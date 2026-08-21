/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Central codec for the fixed MorphOS Process.mui / Slave.mui packet family.
// Dispatch consumers use the named records declared at the public boundary;
// only this adapter owns their packed guest-memory layout.
internal enum MuiProcessSpecialistPacketKind : byte
{
	Method,
	Get,
	Set,
	Signal,
	Error,
	Dispatch,
}

internal enum MuiProcessSpecialistField : byte
{
	MethodId,
	Attribute,
	Storage,
	Value,
	Signals,
	ErrorCode,
	Packet,
}

[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiProcessSpecialistFieldCursor
{
	internal APTR Message;
	internal MuiProcessSpecialistPacketKind Packet;
	internal MuiProcessSpecialistField Field;
}

internal static class MuiProcessSpecialistFieldCursorCodec
{
	private static bool TryResolve(MuiProcessSpecialistPacketKind packet,
		MuiProcessSpecialistField field, out uint offset)
	{
		switch (packet)
		{
			case MuiProcessSpecialistPacketKind.Method:
				if (field == MuiProcessSpecialistField.MethodId) { offset = 0; return true; }
				break;
			case MuiProcessSpecialistPacketKind.Get:
				if (field == MuiProcessSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiProcessSpecialistField.Attribute) { offset = 4; return true; }
				if (field == MuiProcessSpecialistField.Storage) { offset = 8; return true; }
				break;
			case MuiProcessSpecialistPacketKind.Set:
				if (field == MuiProcessSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiProcessSpecialistField.Attribute) { offset = 4; return true; }
				if (field == MuiProcessSpecialistField.Value) { offset = 8; return true; }
				break;
			case MuiProcessSpecialistPacketKind.Signal:
				if (field == MuiProcessSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiProcessSpecialistField.Signals) { offset = 4; return true; }
				break;
			case MuiProcessSpecialistPacketKind.Error:
				if (field == MuiProcessSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiProcessSpecialistField.ErrorCode) { offset = 4; return true; }
				break;
			case MuiProcessSpecialistPacketKind.Dispatch:
				if (field == MuiProcessSpecialistField.MethodId) { offset = 0; return true; }
				if (field == MuiProcessSpecialistField.Packet) { offset = 4; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiProcessSpecialistFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Packet, cursor.Field, out var offset) ||
			cursor.Message.IsNull || cursor.Message.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiProcessSpecialistPacketKind packet,
		MuiProcessSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiProcessSpecialistFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiProcessSpecialistPacketKind packet,
		MuiProcessSpecialistField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiProcessSpecialistFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiProcessSpecialistMessageCodec
{
	internal const uint OmDispose = 0x00000102u;
	internal const uint OmGet = 0x00000104u;
	internal const uint MethodSet = 0x8042549Au;
	internal const uint MethodNoNotifySet = 0x8042216Fu;

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiProcessSpecialistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiProcessSpecialistMethodMessage.Size)) return false;
		return MuiProcessSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiProcessSpecialistPacketKind.Method,
			MuiProcessSpecialistField.MethodId, out packet.MethodId);
	}

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiProcessSpecialistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsValidMethod(ref platform, message, method)) return false;
		packet.MethodId = method;
		return true;
	}

	// Native method-only consumers use this scalar form to avoid materializing
	// a one-field out record in compiler paths where it can widen the branch.
	internal static bool IsValidMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		return IsMethod(method) &&
			TryReadMethodId(ref platform, message, out var header) &&
			header.MethodId == method;
	}

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMethod(method) || message.IsNull || !platform.IsMapped(message,
			MuiProcessSpecialistMethodMessage.Size)) return false;
		return MuiProcessSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiProcessSpecialistPacketKind.Method,
			MuiProcessSpecialistField.MethodId, method);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiProcessSpecialistGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiProcessSpecialistGetMessage.Size,
			OmGet)) return false;
		if (!TryReadMethodId(ref platform, message, out var header)) return false;
		packet.MethodId = header.MethodId;
		return MuiProcessSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiProcessSpecialistPacketKind.Get,
			MuiProcessSpecialistField.Attribute, out packet.Attribute) &&
			MuiProcessSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiProcessSpecialistPacketKind.Get,
				MuiProcessSpecialistField.Storage, out packet.Storage);
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform,
		APTR message, uint attribute, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiProcessSpecialistGetMessage.Size)) return false;
		return MuiProcessSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiProcessSpecialistPacketKind.Get,
			MuiProcessSpecialistField.MethodId, OmGet) &&
			MuiProcessSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiProcessSpecialistPacketKind.Get,
				MuiProcessSpecialistField.Attribute, attribute) &&
			MuiProcessSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiProcessSpecialistPacketKind.Get,
				MuiProcessSpecialistField.Storage, storage);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiProcessSpecialistSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsSetMethod(method) || !IsPacket(ref platform, message,
			MuiProcessSpecialistSetMessage.Size, method)) return false;
		if (!TryReadMethodId(ref platform, message, out var header)) return false;
		packet.MethodId = header.MethodId;
		return MuiProcessSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiProcessSpecialistPacketKind.Set,
			MuiProcessSpecialistField.Attribute, out packet.Attribute) &&
			MuiProcessSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiProcessSpecialistPacketKind.Set,
				MuiProcessSpecialistField.Value, out packet.Value);
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSetMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiProcessSpecialistSetMessage.Size)) return false;
		return MuiProcessSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiProcessSpecialistPacketKind.Set,
			MuiProcessSpecialistField.MethodId, method) &&
			MuiProcessSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiProcessSpecialistPacketKind.Set,
				MuiProcessSpecialistField.Attribute, attribute) &&
			MuiProcessSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiProcessSpecialistPacketKind.Set,
				MuiProcessSpecialistField.Value, value);
	}

	internal static bool TryReadSignal<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiProcessSpecialistSignalMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsSignalMethod(method) || !IsPacket(ref platform, message,
			MuiProcessSpecialistSignalMessage.Size, method)) return false;
		if (!TryReadMethodId(ref platform, message, out var header)) return false;
		packet.MethodId = header.MethodId;
		return MuiProcessSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiProcessSpecialistPacketKind.Signal,
			MuiProcessSpecialistField.Signals, out packet.Signals);
	}

	internal static bool WriteSignal<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint signals)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSignalMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiProcessSpecialistSignalMessage.Size)) return false;
		return MuiProcessSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiProcessSpecialistPacketKind.Signal,
			MuiProcessSpecialistField.MethodId, method) &&
			MuiProcessSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiProcessSpecialistPacketKind.Signal,
				MuiProcessSpecialistField.Signals, signals);
	}

	internal static bool TryReadError<TPlatform>(ref TPlatform platform,
		APTR message, out MuiProcessSpecialistErrorMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiProcessSpecialistErrorMessage.Size,
			MuiProcessAttributes.Slave_Error)) return false;
		if (!TryReadMethodId(ref platform, message, out var header)) return false;
		packet.MethodId = header.MethodId;
		return MuiProcessSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiProcessSpecialistPacketKind.Error,
			MuiProcessSpecialistField.ErrorCode, out packet.ErrorCode);
	}

	internal static bool WriteError<TPlatform>(ref TPlatform platform,
		APTR message, uint errorCode)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiProcessSpecialistErrorMessage.Size)) return false;
		return MuiProcessSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiProcessSpecialistPacketKind.Error,
			MuiProcessSpecialistField.MethodId,
			MuiProcessAttributes.Slave_Error) &&
			MuiProcessSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiProcessSpecialistPacketKind.Error,
				MuiProcessSpecialistField.ErrorCode, errorCode);
	}

	internal static bool TryReadDispatch<TPlatform>(ref TPlatform platform,
		APTR message, out MuiProcessSpecialistDispatchMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message,
			MuiProcessSpecialistDispatchMessage.Size,
			MuiProcessAttributes.Slave_Dispatch)) return false;
		if (!TryReadMethodId(ref platform, message, out var header)) return false;
		packet.MethodId = header.MethodId;
		return MuiProcessSpecialistFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiProcessSpecialistPacketKind.Dispatch,
			MuiProcessSpecialistField.Packet, out packet.Packet);
	}

	internal static bool WriteDispatch<TPlatform>(ref TPlatform platform,
		APTR message, uint packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiProcessSpecialistDispatchMessage.Size)) return false;
		return MuiProcessSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiProcessSpecialistPacketKind.Dispatch,
			MuiProcessSpecialistField.MethodId,
			MuiProcessAttributes.Slave_Dispatch) &&
			MuiProcessSpecialistFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiProcessSpecialistPacketKind.Dispatch,
				MuiProcessSpecialistField.Packet, packet);
	}

	private static bool IsMethod(uint method) => method == OmDispose ||
		method == MuiProcessAttributes.Process_Launch ||
		method == MuiProcessAttributes.Process_Kill ||
		method == MuiProcessAttributes.Process_Process ||
		method == MuiProcessAttributes.Slave_Setup ||
		method == MuiProcessAttributes.Slave_Cleanup ||
		method == MuiProcessAttributes.Semaphore_Attempt ||
		method == MuiProcessAttributes.Semaphore_AttemptShared ||
		method == MuiProcessAttributes.Semaphore_Obtain ||
		method == MuiProcessAttributes.Semaphore_ObtainShared ||
		method == MuiProcessAttributes.Semaphore_Release;

	private static bool IsSetMethod(uint method) => method == MethodSet ||
		method == MethodNoNotifySet;

	private static bool IsSignalMethod(uint method) =>
		method == MuiProcessAttributes.Process_Signal ||
		method == MuiProcessAttributes.Slave_SignalsReceived;

	private static bool IsPacket<TPlatform>(ref TPlatform platform, APTR message,
		uint size, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message, size) ||
			!TryReadMethodId(ref platform, message, out var header)) return false;
		return header.MethodId == method;
	}
}
