/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupChangeMessage
{
	public const uint Size = 4;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupExitChange2Message
{
	public const uint Size = 8;
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

internal static class MuiGroupChangeRecordFieldCursorCodec
{
	private static bool TryResolve(MuiGroupChangeRecordKind record,
		MuiGroupChangeRecordField field, out uint offset, out uint size)
	{
		offset = 0;
		size = 0;
		switch (record)
		{
			case MuiGroupChangeRecordKind.Message:
				size = MuiGroupChangeMessage.Size;
				offset = field == MuiGroupChangeRecordField.MethodId ? 0u :
					uint.MaxValue;
				break;
			case MuiGroupChangeRecordKind.ExitChange2:
				size = MuiGroupExitChange2Message.Size;
				offset = field switch
				{
					MuiGroupChangeRecordField.MethodId => 0,
					MuiGroupChangeRecordField.Flags => 4,
					_ => uint.MaxValue,
				};
				break;
			case MuiGroupChangeRecordKind.State:
				size = MuiGroupChangeState.Size;
				offset = field switch
				{
					MuiGroupChangeRecordField.Cookie => 0,
					MuiGroupChangeRecordField.Depth => 4,
					MuiGroupChangeRecordField.ExitFlags => 8,
					MuiGroupChangeRecordField.ExitRequests => 12,
					_ => uint.MaxValue,
				};
				break;
		}
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupChangeRecordFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Record, cursor.Field, out var offset,
			out var size) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, size)) return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupChangeRecordKind record,
		MuiGroupChangeRecordField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiGroupChangeRecordFieldCursor);
		cursor.Address = address;
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupChangeRecordKind record,
		MuiGroupChangeRecordField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGroupChangeRecordFieldCursor);
		cursor.Address = address;
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiGroupChangeMessageCodec
{
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupChangeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupChangeMessage.Size)) return false;
		if (!MuiGroupChangeRecordFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiGroupChangeRecordKind.Message,
			MuiGroupChangeRecordField.MethodId, out value.MethodId)) return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		uint method) where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupChangeMessage.Size)) return false;
		return MuiGroupChangeRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGroupChangeRecordKind.Message,
			MuiGroupChangeRecordField.MethodId, method);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		uint method, out MuiGroupChangeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (method != MuiGroupChangeCore.InitChangeMethod &&
			method != MuiGroupChangeCore.ExitChangeMethod ||
			!TryReadMethodId(ref platform, address, out var header) ||
			header.MethodId != method) return false;
		value.MethodId = header.MethodId;
		return true;
	}
}

internal static class MuiGroupExitChange2MessageCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		uint method, uint flags) where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupExitChange2Message.Size)) return false;
		return MuiGroupChangeRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGroupChangeRecordKind.ExitChange2,
			MuiGroupChangeRecordField.MethodId, method) &&
			MuiGroupChangeRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiGroupChangeRecordKind.ExitChange2,
				MuiGroupChangeRecordField.Flags, flags);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		uint method, out MuiGroupExitChange2Message value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGroupChangeMessageCodec.TryReadMethodId(ref platform, address,
			out var header) ||
			header.MethodId != method || !platform.IsMapped(address,
			MuiGroupExitChange2Message.Size)) return false;
		value.MethodId = header.MethodId;
		if (!MuiGroupChangeRecordFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiGroupChangeRecordKind.ExitChange2,
			MuiGroupChangeRecordField.Flags, out value.Flags)) return false;
		return true;
	}
}

internal static class MuiGroupPacketDispatchCodec
{
	internal static uint Dispatch<TPlatform>(ref TPlatform platform,
		APTR address, uint initMethod, uint exitMethod, uint exit2Method)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGroupChangeMessageCodec.TryReadMethodId(ref platform, address,
			out var header)) return 0;
		if (header.MethodId == initMethod || header.MethodId == exitMethod)
			return 1;
		if (header.MethodId != exit2Method ||
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
	public uint Cookie;
	public uint Depth;
	public uint ExitFlags;
	public uint ExitRequests;
}

internal static class MuiGroupChangeStateCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupChangeState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupChangeState.Size)) return false;
		return MuiGroupChangeRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGroupChangeRecordKind.State,
			MuiGroupChangeRecordField.Cookie, MuiGroupChangeState.Magic) &&
			MuiGroupChangeRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiGroupChangeRecordKind.State,
				MuiGroupChangeRecordField.Depth, value.Depth) &&
			MuiGroupChangeRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiGroupChangeRecordKind.State,
				MuiGroupChangeRecordField.ExitFlags, value.ExitFlags) &&
			MuiGroupChangeRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiGroupChangeRecordKind.State,
				MuiGroupChangeRecordField.ExitRequests, value.ExitRequests);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupChangeState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupChangeState.Size) ||
			!MuiGroupChangeRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiGroupChangeRecordKind.State,
				MuiGroupChangeRecordField.Cookie, out var cookie) ||
			cookie != MuiGroupChangeState.Magic ||
			!MuiGroupChangeRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiGroupChangeRecordKind.State,
				MuiGroupChangeRecordField.Depth, out value.Depth) ||
			!MuiGroupChangeRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiGroupChangeRecordKind.State,
				MuiGroupChangeRecordField.ExitFlags, out value.ExitFlags) ||
			!MuiGroupChangeRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiGroupChangeRecordKind.State,
				MuiGroupChangeRecordField.ExitRequests,
				out value.ExitRequests)) return false;
		value.Cookie = MuiGroupChangeState.Magic;
		return true;
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
		if (value.Depth == uint.MaxValue) return 0;
		value.Depth++;
		WriteState(ref platform, block, value);
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
		WriteState(ref platform, block, value);
		MuiHeadlessMemory.Mutated(ref platform, state);
		return true;
	}

	private static APTR EnsureState<TPlatform>(ref TPlatform platform, APTR state,
		APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, group,
			StateAttribute));
		if (TryReadState(ref platform, block, out _)) return block;
		block = MuiHeadlessMemory.Allocate(ref platform, MuiGroupChangeState.Size);
		if (block.IsNull) return APTR.Null;
		var value = default(MuiGroupChangeState);
		value.Cookie = MuiGroupChangeState.Magic;
		WriteState(ref platform, block, value);
		if (Set(ref platform, state, group, StateAttribute, block.Raw)) return block;
		platform.Clear(block, MuiGroupChangeState.Size);
		platform.Free(block, MuiGroupChangeState.Size);
		return APTR.Null;
	}

	private static void WriteState<TPlatform>(ref TPlatform platform, APTR block,
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
		if (name.IsNull || !platform.IsMapped(name, 9)) return false;
		return platform.ReadUInt8(name, 0) == (byte)'G' &&
			platform.ReadUInt8(name, 1) == (byte)'r' &&
			platform.ReadUInt8(name, 2) == (byte)'o' &&
			platform.ReadUInt8(name, 3) == (byte)'u' &&
			platform.ReadUInt8(name, 4) == (byte)'p' &&
			platform.ReadUInt8(name, 5) == (byte)'.' &&
			platform.ReadUInt8(name, 6) == (byte)'m' &&
			platform.ReadUInt8(name, 7) == (byte)'u' &&
			platform.ReadUInt8(name, 8) == (byte)'i' &&
			platform.IsMapped(name, 10) && platform.ReadUInt8(name, 9) == 0;
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
