/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
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

// Struct-first guest-memory adapter for the fixed Process/Slave packets.
// Packet kinds own complete MorphOS record spans; field names select members
// without exposing numeric positions to dispatch code.
internal static class MuiProcessSpecialistMessageMemoryCodec
{
	private static bool TryResolve(MuiProcessSpecialistPacketKind packet,
		MuiProcessSpecialistField field, out uint offset, out uint size)
	{
		switch (packet)
		{
			case MuiProcessSpecialistPacketKind.Method:
				size = MuiProcessSpecialistMethodMessage.Size;
				if (field == MuiProcessSpecialistField.MethodId)
					offset = MuiProcessSpecialistMethodMessage.MethodIdOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiProcessSpecialistPacketKind.Get:
				size = MuiProcessSpecialistGetMessage.Size;
				if (field == MuiProcessSpecialistField.MethodId)
					offset = MuiProcessSpecialistGetMessage.MethodIdOffset;
				else if (field == MuiProcessSpecialistField.Attribute)
					offset = MuiProcessSpecialistGetMessage.AttributeOffset;
				else if (field == MuiProcessSpecialistField.Storage)
					offset = MuiProcessSpecialistGetMessage.StorageOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiProcessSpecialistPacketKind.Set:
				size = MuiProcessSpecialistSetMessage.Size;
				if (field == MuiProcessSpecialistField.MethodId)
					offset = MuiProcessSpecialistSetMessage.MethodIdOffset;
				else if (field == MuiProcessSpecialistField.Attribute)
					offset = MuiProcessSpecialistSetMessage.AttributeOffset;
				else if (field == MuiProcessSpecialistField.Value)
					offset = MuiProcessSpecialistSetMessage.ValueOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiProcessSpecialistPacketKind.Signal:
				size = MuiProcessSpecialistSignalMessage.Size;
				if (field == MuiProcessSpecialistField.MethodId)
					offset = MuiProcessSpecialistSignalMessage.MethodIdOffset;
				else if (field == MuiProcessSpecialistField.Signals)
					offset = MuiProcessSpecialistSignalMessage.SignalsOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiProcessSpecialistPacketKind.Error:
				size = MuiProcessSpecialistErrorMessage.Size;
				if (field == MuiProcessSpecialistField.MethodId)
					offset = MuiProcessSpecialistErrorMessage.MethodIdOffset;
				else if (field == MuiProcessSpecialistField.ErrorCode)
					offset = MuiProcessSpecialistErrorMessage.ErrorCodeOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiProcessSpecialistPacketKind.Dispatch:
				size = MuiProcessSpecialistDispatchMessage.Size;
				if (field == MuiProcessSpecialistField.MethodId)
					offset = MuiProcessSpecialistDispatchMessage.MethodIdOffset;
				else if (field == MuiProcessSpecialistField.Packet)
					offset = MuiProcessSpecialistDispatchMessage.PacketOffset;
				else { offset = 0; size = 0; return false; }
				return true;
		}
		offset = 0;
		size = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiProcessSpecialistPacketKind packet,
		MuiProcessSpecialistField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset, out var size) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, size)) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiProcessSpecialistMethodMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiProcessSpecialistPacketKind packet,
		MuiProcessSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiProcessSpecialistPacketKind packet,
		MuiProcessSpecialistField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// field cursor. The live message codecs route to the struct adapter above.
internal static class MuiProcessSpecialistFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiProcessSpecialistFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiProcessSpecialistMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Packet, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiProcessSpecialistPacketKind packet,
		MuiProcessSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiProcessSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiProcessSpecialistPacketKind packet,
		MuiProcessSpecialistField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiProcessSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, packet, field, value);
}

// Complete sequential codecs for the fixed Process/Slave packet envelopes.
// One-ULONG method-only packets use scalar cursor helpers so the native
// compiler does not materialize a temporary one-field value struct; every
// multi-field envelope is exchanged as its named declaration-ordered record.
internal static class MuiProcessSpecialistMessageStructCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiProcessSpecialistMethodHeaderCodec.TryReadValue(ref platform,
			message, out methodId);

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryWriteMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiProcessSpecialistMethodHeaderCodec.WriteValue(ref platform, message,
			methodId);

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiProcessSpecialistGetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiProcessSpecialistGetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var storage)) return false;
		value.MethodId = methodId;
		value.Attribute = attribute;
		value.Storage = storage;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform,
		APTR message, MuiProcessSpecialistGetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiProcessSpecialistGetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Storage)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiProcessSpecialistSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiProcessSpecialistSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var valueWord)) return false;
		value.MethodId = methodId;
		value.Attribute = attribute;
		value.Value = valueWord;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform,
		APTR message, MuiProcessSpecialistSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiProcessSpecialistSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadSignal<TPlatform>(ref TPlatform platform,
		APTR message, out MuiProcessSpecialistSignalMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiProcessSpecialistSignalMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var signals)) return false;
		value.MethodId = methodId;
		value.Signals = signals;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteSignal<TPlatform>(ref TPlatform platform,
		APTR message, MuiProcessSpecialistSignalMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiProcessSpecialistSignalMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Signals)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadError<TPlatform>(ref TPlatform platform,
		APTR message, out MuiProcessSpecialistErrorMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiProcessSpecialistErrorMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var errorCode)) return false;
		value.MethodId = methodId;
		value.ErrorCode = errorCode;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteError<TPlatform>(ref TPlatform platform,
		APTR message, MuiProcessSpecialistErrorMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiProcessSpecialistErrorMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.ErrorCode)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadDispatch<TPlatform>(ref TPlatform platform,
		APTR message, out MuiProcessSpecialistDispatchMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiProcessSpecialistDispatchMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var packet)) return false;
		value.MethodId = methodId;
		value.Packet = packet;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteDispatch<TPlatform>(ref TPlatform platform,
		APTR message, MuiProcessSpecialistDispatchMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiProcessSpecialistDispatchMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Packet)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Struct-first codec for the method-only Process/Slave specialist header.
// The named one-ULONG record remains the ABI contract while shared guest
// storage keeps selector admission safe for freestanding lowering.
internal static class MuiProcessSpecialistMethodHeaderCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiProcessSpecialistMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.TryReadValue(ref platform, valueAddress,
				out methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteValue<TPlatform>(ref TPlatform platform,
		APTR address, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiProcessSpecialistMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
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
		if (!TryReadMethodIdValue(ref platform, message, out var methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	// Selector admission stays scalar for callers that only need MethodID, but
	// the value is read from the named one-ULONG record in declaration order.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiProcessSpecialistMessageStructCodec.TryReadMethodIdValue(
			ref platform, message, out methodId);
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
			TryReadMethodIdValue(ref platform, message, out var methodId) &&
			methodId == method;
	}

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		return IsMethod(method) &&
			MuiProcessSpecialistMessageStructCodec.TryWriteMethodIdValue(
				ref platform, message, method);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiProcessSpecialistGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiProcessSpecialistMessageStructCodec.TryReadGet(ref platform,
			message, out packet) && packet.MethodId == OmGet;
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform,
		APTR message, uint attribute, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiProcessSpecialistGetMessage);
		packet.MethodId = OmGet;
		packet.Attribute = attribute;
		packet.Storage = storage;
		return MuiProcessSpecialistMessageStructCodec.WriteGet(ref platform,
			message, packet);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiProcessSpecialistSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsSetMethod(method) &&
			MuiProcessSpecialistMessageStructCodec.TryReadSet(ref platform,
				message, out packet) && packet.MethodId == method;
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSetMethod(method)) return false;
		var packet = default(MuiProcessSpecialistSetMessage);
		packet.MethodId = method;
		packet.Attribute = attribute;
		packet.Value = value;
		return MuiProcessSpecialistMessageStructCodec.WriteSet(ref platform,
			message, packet);
	}

	internal static bool TryReadSignal<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiProcessSpecialistSignalMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsSignalMethod(method) &&
			MuiProcessSpecialistMessageStructCodec.TryReadSignal(ref platform,
				message, out packet) && packet.MethodId == method;
	}

	internal static bool WriteSignal<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint signals)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSignalMethod(method)) return false;
		var packet = default(MuiProcessSpecialistSignalMessage);
		packet.MethodId = method;
		packet.Signals = signals;
		return MuiProcessSpecialistMessageStructCodec.WriteSignal(ref platform,
			message, packet);
	}

	internal static bool TryReadError<TPlatform>(ref TPlatform platform,
		APTR message, out MuiProcessSpecialistErrorMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiProcessSpecialistMessageStructCodec.TryReadError(ref platform,
			message, out packet) && packet.MethodId ==
			MuiProcessAttributes.Slave_Error;
	}

	internal static bool WriteError<TPlatform>(ref TPlatform platform,
		APTR message, uint errorCode)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiProcessSpecialistErrorMessage);
		packet.MethodId = MuiProcessAttributes.Slave_Error;
		packet.ErrorCode = errorCode;
		return MuiProcessSpecialistMessageStructCodec.WriteError(ref platform,
			message, packet);
	}

	internal static bool TryReadDispatch<TPlatform>(ref TPlatform platform,
		APTR message, out MuiProcessSpecialistDispatchMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiProcessSpecialistMessageStructCodec.TryReadDispatch(ref platform,
			message, out packet) && packet.MethodId ==
			MuiProcessAttributes.Slave_Dispatch;
	}

	internal static bool WriteDispatch<TPlatform>(ref TPlatform platform,
		APTR message, uint packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		var dispatch = default(MuiProcessSpecialistDispatchMessage);
		dispatch.MethodId = MuiProcessAttributes.Slave_Dispatch;
		dispatch.Packet = packet;
		return MuiProcessSpecialistMessageStructCodec.WriteDispatch(ref platform,
			message, dispatch);
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
			!TryReadMethodIdValue(ref platform, message, out var methodId)) return false;
		return methodId == method;
	}
}
