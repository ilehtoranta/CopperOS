/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupChangeMessage
{
	public const uint Size = 4;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupExitChange2Message
{
	public const uint Size = 8;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint FlagsOffset = 4;
	public uint MethodId;
	public uint Flags;
}

internal enum MuiGroupChangeRecordKind : byte
{
	Message,
	ExitChange2,
	State,
}

internal enum MuiGroupChangeRecordField : byte
{
	MethodId,
	Flags,
	Cookie,
	Depth,
	ExitFlags,
	ExitRequests,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupChangeRecordFieldCursor
{
	internal APTR Address;
	internal MuiGroupChangeRecordKind Record;
	internal MuiGroupChangeRecordField Field;
}

// The fixed Group change packet and state records own their packed positions
// in this bounded adapter. Every typed field address walks the complete named
// record with a guest struct cursor; no caller supplies a numeric offset.
internal static class MuiGroupChangeRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiGroupChangeRecordKind record,
		MuiGroupChangeRecordField field, out uint index, out uint size)
	{
		index = 0;
		size = 0;
		switch (record)
		{
			case MuiGroupChangeRecordKind.Message:
				size = MuiGroupChangeMessage.Size;
				return field == MuiGroupChangeRecordField.MethodId;
			case MuiGroupChangeRecordKind.ExitChange2:
				size = MuiGroupExitChange2Message.Size;
				if (field == MuiGroupChangeRecordField.MethodId)
				{
					index = 0;
					return true;
				}
				if (field == MuiGroupChangeRecordField.Flags)
				{
					index = 1;
					return true;
				}
				return false;
			case MuiGroupChangeRecordKind.State:
				size = MuiGroupChangeState.Size;
				if (field == MuiGroupChangeRecordField.Cookie)
				{
					index = 0;
					return true;
				}
				if (field == MuiGroupChangeRecordField.Depth)
				{
					index = 1;
					return true;
				}
				if (field == MuiGroupChangeRecordField.ExitFlags)
				{
					index = 2;
					return true;
				}
				if (field == MuiGroupChangeRecordField.ExitRequests)
				{
					index = 3;
					return true;
				}
				return false;
			default:
				return false;
		}
	}

	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiGroupChangeRecordKind record,
		MuiGroupChangeRecordField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolveFieldIndex(record, field, out var index, out _))
			return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGroupChangeMessage.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				return true;
			}
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR recordAddress, MuiGroupChangeRecordKind record,
		MuiGroupChangeRecordField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolveFieldIndex(record, field, out _, out var size) ||
			!MuiGuestStructCursor.TryCreate(ref platform, recordAddress, size,
				out var cursor)) return false;
		return TryTakeField(ref platform, ref cursor, record, field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupChangeRecordKind record,
		MuiGroupChangeRecordField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		switch (record)
		{
			case MuiGroupChangeRecordKind.Message:
				if (field != MuiGroupChangeRecordField.MethodId ||
					!MuiGroupChangeMessageCodec.TryReadRecord(ref platform, address,
						out var message)) return false;
				value = message.MethodId;
				return true;
			case MuiGroupChangeRecordKind.ExitChange2:
				if (!MuiGroupExitChange2MessageCodec.TryReadRecord(ref platform,
					address, out var exit2)) return false;
				if (field == MuiGroupChangeRecordField.MethodId)
					value = exit2.MethodId;
				else if (field == MuiGroupChangeRecordField.Flags)
					value = exit2.Flags;
				else return false;
				return true;
			case MuiGroupChangeRecordKind.State:
				if (!MuiGroupChangeStateCodec.TryReadRecord(ref platform, address,
					out var state)) return false;
				if (field == MuiGroupChangeRecordField.Cookie)
					value = state.Cookie;
				else if (field == MuiGroupChangeRecordField.Depth)
					value = state.Depth;
				else if (field == MuiGroupChangeRecordField.ExitFlags)
					value = state.ExitFlags;
				else if (field == MuiGroupChangeRecordField.ExitRequests)
					value = state.ExitRequests;
				else return false;
				return true;
			default:
				return false;
		}
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupChangeRecordKind record,
		MuiGroupChangeRecordField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		switch (record)
		{
			case MuiGroupChangeRecordKind.Message:
				if (field != MuiGroupChangeRecordField.MethodId ||
					!MuiGroupChangeMessageCodec.TryReadRecord(ref platform, address,
						out var message)) return false;
				message.MethodId = value;
				return MuiGroupChangeMessageCodec.WriteRecord(ref platform, address,
					message);
			case MuiGroupChangeRecordKind.ExitChange2:
				if (!MuiGroupExitChange2MessageCodec.TryReadRecord(ref platform,
					address, out var exit2)) return false;
				if (field == MuiGroupChangeRecordField.MethodId)
					exit2.MethodId = value;
				else if (field == MuiGroupChangeRecordField.Flags)
					exit2.Flags = value;
				else return false;
				return MuiGroupExitChange2MessageCodec.WriteRecord(ref platform,
					address, exit2);
			case MuiGroupChangeRecordKind.State:
				if (!MuiGroupChangeStateCodec.TryReadRecord(ref platform, address,
					out var state)) return false;
				if (field == MuiGroupChangeRecordField.Cookie)
					state.Cookie = value;
				else if (field == MuiGroupChangeRecordField.Depth)
					state.Depth = value;
				else if (field == MuiGroupChangeRecordField.ExitFlags)
					state.ExitFlags = value;
				else if (field == MuiGroupChangeRecordField.ExitRequests)
					state.ExitRequests = value;
				else return false;
				return MuiGroupChangeStateCodec.WriteRecord(ref platform, address,
					state);
			default:
				return false;
		}
	}
}

internal static class MuiGroupChangeRecordFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupChangeRecordFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGroupChangeRecordMemoryCodec.TryGetAddress(ref platform, cursor.Address,
			cursor.Record, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupChangeRecordKind record,
		MuiGroupChangeRecordField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGroupChangeRecordMemoryCodec.TryReadUInt32(ref platform, address, record,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupChangeRecordKind record,
		MuiGroupChangeRecordField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGroupChangeRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			record, field, value);
}

internal static class MuiGroupChangeMessageCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValueStruct<TPlatform>(
		ref TPlatform platform, APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupChangeMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.TryReadValue(ref platform, valueAddress,
				out methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteMethodIdValueStruct<TPlatform>(
		ref TPlatform platform, APTR address, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupChangeMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	// The complete method envelope is decoded in declaration order.  The
	// scalar selector helper below remains only for method admission, where a
	// four-byte header is all the caller is allowed to provide.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupChangeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return TryReadMethodIdValueStruct(ref platform, address,
			out value.MethodId);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupChangeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return WriteMethodIdValueStruct(ref platform, address, value.MethodId);
	}

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupChangeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return TryReadRecord(ref platform, address, out value);
	}

	// Native selector admission stays scalar so compiler paths do not need to
	// materialize a temporary one-field record. Public packet consumers still
	// receive the named struct above.
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadMethodIdValueStruct(ref platform, address, out methodId);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		uint method) where TPlatform : struct, IMuiGuestMemory
	{
		return WriteMethodIdValueStruct(ref platform, address, method);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		uint method, out MuiGroupChangeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (method != MuiGroupChangeCore.InitChangeMethod &&
			method != MuiGroupChangeCore.ExitChangeMethod ||
			!TryReadRecord(ref platform, address, out value) ||
			value.MethodId != method) return false;
		return true;
	}
}

internal static class MuiGroupExitChange2MessageCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupExitChange2Message value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupExitChange2Message.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupExitChange2Message value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupExitChange2Message.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		uint method, uint flags) where TPlatform : struct, IMuiGuestMemory
	{
		var value = default(MuiGroupExitChange2Message);
		value.MethodId = method;
		value.Flags = flags;
		return WriteRecord(ref platform, address, value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		uint method, out MuiGroupExitChange2Message value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return method == MuiGroupChangeCore.ExitChange2Method &&
			TryReadRecord(ref platform, address, out value) &&
			value.MethodId == method;
	}
}

internal static class MuiGroupPacketDispatchCodec
{
	internal static uint Dispatch<TPlatform>(ref TPlatform platform,
		APTR address, uint initMethod, uint exitMethod, uint exit2Method)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGroupChangeMessageCodec.TryReadMethodIdValue(ref platform,
			address, out var methodId)) return 0;
		if (methodId == initMethod || methodId == exitMethod)
			return 1;
		if (methodId != exit2Method ||
			!MuiGroupExitChange2MessageCodec.TryRead(ref platform, address,
				exit2Method, out var packet)) return 0;
		return packet.Flags;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupChangeState
{
	public const uint Magic = 0x47524348; // "GRCH"
	public const uint Size = 16;
	public const uint FieldSize = 4;
	public const uint CookieOffset = 0;
	public const uint DepthOffset = 4;
	public const uint ExitFlagsOffset = 8;
	public const uint ExitRequestsOffset = 12;
	public uint Cookie;
	public uint Depth;
	public uint ExitFlags;
	public uint ExitRequests;
}

internal static class MuiGroupChangeStateValidation
{
	internal static bool IsValidRecord(MuiGroupChangeState value) =>
		value.Cookie == MuiGroupChangeState.Magic &&
		value.Depth <= MuiHeadlessLayout.MaximumTraversal;

	internal static bool IsValidState(MuiGroupChangeState value) =>
		IsValidRecord(value);
}

internal static class MuiGroupChangeStateCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupChangeState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupChangeState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Cookie) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Depth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.ExitFlags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.ExitRequests) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupChangeState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupChangeState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Cookie) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Depth) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.ExitFlags) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.ExitRequests)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupChangeState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiGroupChangeStateValidation.IsValidRecord(value) ||
			!WriteRecord(ref platform, address, value)) return false;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupChangeState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return TryReadRecord(ref platform, address, out value) &&
			MuiGroupChangeStateValidation.IsValidState(value);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiGroupChangeRecordInput
{
	public uint Depth;
	public uint ExitFlags;
	public uint ExitRequests;
}

// Guest-resident state for the documented Group change bracket. The bracket
// itself suppresses no object ownership: Family methods remain usable inside
// it, while the typed depth/flag record makes nesting and malformed underflow
// observable without a managed counter.
public static class MuiGroupChangeCore
{
	private const uint StateAttribute = 0x7FFE0040;
	public const uint InitChangeMethod = 0x80420887;
	public const uint ExitChangeMethod = 0x8042D1CC;
	public const uint ExitChange2Method = 0x8042E541;

	// Struct-first packet writers for the three public Group change methods.
	// The live dispatcher consumes the same records; no caller needs to know
	// field offsets beyond this guest-memory codec boundary.
	public static bool WriteInitChangeRecord<TPlatform>(ref TPlatform platform,
		APTR storage) where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupChangeMessageCodec.Write(ref platform, storage,
			InitChangeMethod);

	public static bool WriteExitChangeRecord<TPlatform>(ref TPlatform platform,
		APTR storage) where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupChangeMessageCodec.Write(ref platform, storage,
			ExitChangeMethod);

	public static bool WriteExitChange2Record<TPlatform>(ref TPlatform platform,
		APTR storage, uint flags) where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupExitChange2MessageCodec.Write(ref platform, storage,
			ExitChange2Method, flags);

	internal static bool TryReadChange<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiGroupChangeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupChangeMessageCodec.TryRead(ref platform, message, method,
			out packet);

	internal static bool TryReadExitChange2<TPlatform>(ref TPlatform platform,
		APTR message, out MuiGroupExitChange2Message packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupExitChange2MessageCodec.TryRead(ref platform, message,
			ExitChange2Method, out packet);

	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, APTR message, uint method)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (method == InitChangeMethod)
			return TryReadChange(ref platform, message, method, out _)
				? InitChange(ref platform, state, group) : 0;
		if (method == ExitChangeMethod)
			return TryReadChange(ref platform, message, method, out _)
				&& ExitChange(ref platform, state, group) ? 1u : 0u;
		if (method == ExitChange2Method)
		{
			if (!TryReadExitChange2(ref platform, message, out var packet)) return 0;
			return ExitChange2(ref platform, state, group, packet.Flags) ? 1u : 0u;
		}
		return 0;
	}

	// Packet-only native qualification seam. Init/Exit return a success token;
	// ExitChange2 returns its decoded flags so the fixed header is observable.
	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupPacketDispatchCodec.Dispatch(ref platform, message,
			InitChangeMethod, ExitChangeMethod, ExitChange2Method);

	public static uint InitChange<TPlatform>(ref TPlatform platform, APTR state,
		APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!IsGroupObject(ref platform, state, group)) return 0;
		var block = EnsureState(ref platform, state, group);
		if (block.IsNull || !TryReadState(ref platform, block, out var value))
			return 0;
		if (value.Depth >= MuiHeadlessLayout.MaximumTraversal) return 0;
		value.Depth++;
		if (!WriteState(ref platform, block, value)) return 0;
		MuiHeadlessMemory.Mutated(ref platform, state);
		// MorphOS documents NULL as failure; a live group pointer is the stable
		// non-null success token and avoids inventing a separate handle format.
		return group.Raw;
	}

	public static bool ExitChange<TPlatform>(ref TPlatform platform, APTR state,
		APTR group) where TPlatform : struct, IMuiHeadlessPlatform =>
		Exit(ref platform, state, group, 0);

	public static bool ExitChange2<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, uint flags) where TPlatform : struct, IMuiHeadlessPlatform =>
		Exit(ref platform, state, group, flags);

	public static uint ChangeDepth<TPlatform>(ref TPlatform platform, APTR state,
		APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!IsGroupObject(ref platform, state, group)) return 0;
		var block = APTR.FromPointer(Read(ref platform, state, group,
			StateAttribute));
		return TryReadState(ref platform, block, out var value) ? value.Depth : 0;
	}

	public static uint ChangeExitFlags<TPlatform>(ref TPlatform platform,
		APTR state, APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!IsGroupObject(ref platform, state, group)) return 0;
		var block = APTR.FromPointer(Read(ref platform, state, group,
			StateAttribute));
		return TryReadState(ref platform, block, out var value) ? value.ExitFlags : 0;
	}

	// Struct-first native qualification seam for the guest record. The method
	// entry points above own Group validation and bracket transitions.
	public static bool WriteChangeRecord<TPlatform>(ref TPlatform platform,
		APTR storage, uint depth, uint flags, uint exits)
		where TPlatform : struct, IMuiGuestMemory
	{
		var input = default(MuiGroupChangeRecordInput);
		input.Depth = depth;
		input.ExitFlags = flags;
		input.ExitRequests = exits;
		return WriteChangeRecord(ref platform, storage, input);
	}

	public static bool WriteChangeRecord<TPlatform>(ref TPlatform platform,
		APTR storage, MuiGroupChangeRecordInput input)
		where TPlatform : struct, IMuiGuestMemory
	{
		var value = default(MuiGroupChangeState);
		value.Cookie = MuiGroupChangeState.Magic;
		value.Depth = input.Depth;
		value.ExitFlags = input.ExitFlags;
		value.ExitRequests = input.ExitRequests;
		return MuiGroupChangeStateCodec.Write(ref platform, storage, value);
	}

	public static uint DispatchChangeStateRecord<TPlatform>(
		ref TPlatform platform, APTR storage) where TPlatform : struct,
		IMuiGuestMemory
	{
		if (!MuiGroupChangeStateCodec.TryRead(ref platform, storage,
			out var value)) return 0;
		return value.Depth ^ value.ExitFlags ^ value.ExitRequests;
	}

	internal static void CleanupRecords<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			StateAttribute));
		if (!MuiGroupChangeStateCodec.TryRead(ref platform, block, out _)) return;
		platform.Clear(block, MuiGroupChangeState.Size);
		platform.Free(block, MuiGroupChangeState.Size);
		Set(ref platform, state, obj, StateAttribute, 0);
	}

	private static bool Exit<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, uint flags) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!IsGroupObject(ref platform, state, group)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, group,
			StateAttribute));
		if (!TryReadState(ref platform, block, out var value) || value.Depth == 0)
			return false;
		value.Depth--;
		value.ExitFlags = flags;
		value.ExitRequests = value.ExitRequests == uint.MaxValue
			? uint.MaxValue : value.ExitRequests + 1;
		if (!WriteState(ref platform, block, value)) return false;
		MuiHeadlessMemory.Mutated(ref platform, state);
		return true;
	}

	private static APTR EnsureState<TPlatform>(ref TPlatform platform, APTR state,
		APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, group,
			StateAttribute, out var existing) && existing != 0)
		{
			var existingBlock = APTR.FromPointer(existing);
			if (TryReadState(ref platform, existingBlock, out _))
				return existingBlock;
			return APTR.Null;
		}
		var block = APTR.Null;
		block = MuiHeadlessMemory.Allocate(ref platform, MuiGroupChangeState.Size);
		if (block.IsNull) return APTR.Null;
		var value = default(MuiGroupChangeState);
		value.Cookie = MuiGroupChangeState.Magic;
		if (!WriteState(ref platform, block, value))
		{
			platform.Clear(block, MuiGroupChangeState.Size);
			platform.Free(block, MuiGroupChangeState.Size);
			return APTR.Null;
		}
		if (Set(ref platform, state, group, StateAttribute, block.Raw)) return block;
		platform.Clear(block, MuiGroupChangeState.Size);
		platform.Free(block, MuiGroupChangeState.Size);
		return APTR.Null;
	}

	private static bool WriteState<TPlatform>(ref TPlatform platform, APTR block,
		MuiGroupChangeState value) where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupChangeStateCodec.Write(ref platform, block, value);

	private static bool TryReadState<TPlatform>(ref TPlatform platform, APTR block,
		out MuiGroupChangeState value) where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupChangeStateCodec.TryRead(ref platform, block, out value);

	internal static bool IsGroupObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var classRecord = MuiHeadlessObjectCore.ObjectClassRecord(ref platform,
			state, obj);
		if (classRecord.IsNull) return false;
		for (var depth = 0u; depth < MuiHeadlessLayout.MaximumTraversal;
			depth++)
		{
			if (!MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
				out var classValue))
				return false;
			if (IsGroupName(ref platform, classValue.Name)) return true;
			if (classValue.Super.IsNull) return false;
			classRecord = FindClassByBoopsi(ref platform, state,
				classValue.Super);
			if (classRecord.IsNull) return false;
		}
		return false;
	}

	private static APTR FindClassByBoopsi<TPlatform>(ref TPlatform platform,
		APTR state, APTR boopsi) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return APTR.Null;
		var current = stateValue.Classes;
		for (var depth = 0u; current.IsNotNull &&
			depth < MuiHeadlessLayout.MaximumTraversal; depth++)
		{
			if (!MuiHeadlessClassCodec.TryRead(ref platform, current,
				out var classValue))
				return APTR.Null;
			if (classValue.Boopsi.Raw == boopsi.Raw) return current;
			current = classValue.Next;
		}
		return APTR.Null;
	}

	private static bool IsGroupName<TPlatform>(ref TPlatform platform, APTR name)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGroupClassNameRecordCodec.TryRead(ref platform, name,
			out var value)) return false;
		return value.Word0 == 0x47726F75 && // Grou
			value.Word1 == 0x702E6D75 && // p.mu
			value.Character == (byte)'i' && value.Terminator == 0;
	}

	private static uint Read<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute) where TPlatform : struct, IMuiHeadlessPlatform
	{
		return MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			attribute, out var value) ? value : 0;
	}

	private static bool Set<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			value, false);
}
