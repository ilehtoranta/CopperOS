/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

internal static class MuiAslServiceLayout
{
	public const uint Magic = 0x4D554941; // "MUIA"
	public const uint Version = 1;
	public const uint MaximumTraversal = 65535;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAslServiceStateRecord
{
	internal const uint Size = 12;
	internal uint Magic;
	internal APTR Head;
	internal uint Generation;
}

internal enum MuiAslRecordKind : byte
{
	State,
	Lease,
}

internal enum MuiAslRecordField : byte
{
	Magic,
	Head,
	Generation,
	Next,
	Requester,
	Type,
	Tags,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAslRecordFieldCursor
{
	internal APTR Record;
	internal MuiAslRecordKind Kind;
	internal MuiAslRecordField Field;
}

internal static class MuiAslRecordFieldCursorCodec
{
	private static bool TryResolve(MuiAslRecordKind kind,
		MuiAslRecordField field, out uint offset, out uint size,
		out uint fieldSize)
	{
		offset = 0;
		size = 0;
		fieldSize = 0;
		switch (kind)
		{
			case MuiAslRecordKind.State:
				size = MuiAslServiceStateRecord.Size;
				offset = field switch
				{
					MuiAslRecordField.Magic => 0,
					MuiAslRecordField.Head => 4,
					MuiAslRecordField.Generation => 8,
					_ => uint.MaxValue,
				};
				fieldSize = 4;
				break;
			case MuiAslRecordKind.Lease:
				size = MuiAslRequestLeaseRecord.Size;
				offset = field switch
				{
					MuiAslRecordField.Next => 0,
					MuiAslRecordField.Requester => 4,
					MuiAslRecordField.Type => 8,
					MuiAslRecordField.Tags => 12,
					_ => uint.MaxValue,
				};
				fieldSize = 4;
				break;
			default:
				offset = uint.MaxValue;
				break;
		}
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAslRecordFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolve(cursor.Kind, cursor.Field, out var offset,
			out var recordSize, out fieldSize) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, recordSize)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAslRecordKind kind, MuiAslRecordField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAslRecordFieldCursor);
		cursor.Record = record;
		cursor.Kind = kind;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address,
			out var fieldSize) || fieldSize != 4) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAslRecordKind kind, MuiAslRecordField field,
		uint value) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAslRecordFieldCursor);
		cursor.Record = record;
		cursor.Kind = kind;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address,
			out var fieldSize) || fieldSize != 4) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAslServiceStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAslServiceStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAslServiceStateRecord.Size)) return false;
		if (!MuiAslRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiAslRecordKind.State, MuiAslRecordField.Magic, out record.Magic) ||
			!MuiAslRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAslRecordKind.State, MuiAslRecordField.Head, out var head) ||
			!MuiAslRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAslRecordKind.State, MuiAslRecordField.Generation,
				out record.Generation)) return false;
		record.Head = APTR.FromPointer(head);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAslServiceStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAslServiceStateRecord.Size)) return false;
		return MuiAslRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiAslRecordKind.State, MuiAslRecordField.Magic, record.Magic) &&
			MuiAslRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAslRecordKind.State, MuiAslRecordField.Head, record.Head.Raw) &&
			MuiAslRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAslRecordKind.State, MuiAslRecordField.Generation,
				record.Generation);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAslRequestLeaseRecord
{
	internal const uint Size = 16;
	internal APTR Next;
	internal APTR Requester;
	internal uint Type;
	internal APTR Tags;
}

internal static class MuiAslRequestLeaseCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAslRequestLeaseRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAslRequestLeaseRecord.Size)) return false;
		if (!MuiAslRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiAslRecordKind.Lease, MuiAslRecordField.Next, out var next) ||
			!MuiAslRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAslRecordKind.Lease, MuiAslRecordField.Requester,
				out var requester) ||
			!MuiAslRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAslRecordKind.Lease, MuiAslRecordField.Type, out record.Type) ||
			!MuiAslRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAslRecordKind.Lease, MuiAslRecordField.Tags, out var tags))
			return false;
		record.Next = APTR.FromPointer(next);
		record.Requester = APTR.FromPointer(requester);
		record.Tags = APTR.FromPointer(tags);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAslRequestLeaseRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAslRequestLeaseRecord.Size)) return false;
		return MuiAslRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiAslRecordKind.Lease, MuiAslRecordField.Next, record.Next.Raw) &&
			MuiAslRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAslRecordKind.Lease, MuiAslRecordField.Requester,
				record.Requester.Raw) &&
			MuiAslRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAslRecordKind.Lease, MuiAslRecordField.Type, record.Type) &&
			MuiAslRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAslRecordKind.Lease, MuiAslRecordField.Tags, record.Tags.Raw);
	}
}

// Scalar qualification surface for the guest-resident ASL state and lease
// records. The production service remains responsible for capability calls;
// this seam proves that the fixed layouts round-trip without managed state.
public static class MuiAslServiceRecordPacketCore
{
	public static bool WriteState<TPlatform>(ref TPlatform platform, APTR address,
		uint magic, APTR head, uint generation)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiAslServiceStateRecord record = default;
		record.Magic = magic;
		record.Head = head;
		record.Generation = generation;
		return MuiAslServiceStateCodec.Write(ref platform, address, record);
	}

	public static uint DispatchState<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAslServiceStateCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Magic ^ record.Head.Raw ^ record.Generation;
	}

	public static bool WriteLease<TPlatform>(ref TPlatform platform, APTR address,
		APTR next, APTR requester, uint type, APTR tags)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiAslRequestLeaseRecord record = default;
		record.Next = next;
		record.Requester = requester;
		record.Type = type;
		record.Tags = tags;
		return MuiAslRequestLeaseCodec.Write(ref platform, address, record);
	}

	public static uint DispatchLease<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAslRequestLeaseCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Next.Raw ^ record.Requester.Raw ^ record.Type ^
			record.Tags.Raw;
	}
}

// Bounded MUI_AllocAslRequest/MUI_AslRequest/MUI_FreeAslRequest gateway.
// The ASL capability owns the platform requester, while this service owns a
// guest-resident lease list so only requesters allocated through MUI can be
// submitted or released. No managed state, exceptions, or host allocation is
// used by the production path.
public static class MuiAslServiceCore
{
	public static bool Initialize<TPlatform>(ref TPlatform platform,
		APTR serviceState) where TPlatform : struct, IMuiServicePlatform
	{
		if (serviceState.IsNull ||
			!platform.IsMapped(serviceState, MuiAslServiceStateRecord.Size))
			return false;
		if (MuiAslServiceStateCodec.TryRead(ref platform, serviceState,
			out var current) && current.Magic == MuiAslServiceLayout.Magic &&
			current.Generation == MuiAslServiceLayout.Version) return true;
		platform.Clear(serviceState, MuiAslServiceStateRecord.Size);
		MuiAslServiceStateRecord record = default;
		record.Magic = MuiAslServiceLayout.Magic;
		record.Generation = MuiAslServiceLayout.Version;
		return MuiAslServiceStateCodec.Write(ref platform, serviceState, record);
	}

	public static APTR AllocAslRequest<TPlatform>(ref TPlatform platform,
		APTR serviceState, uint requestType, APTR tags)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) ||
			!MuiAslTagListCore.Validate(ref platform, tags)) return APTR.Null;
		var requester = platform.AllocateRequest(requestType, tags);
		if (requester.IsNull) return APTR.Null;
		var record = platform.Allocate(MuiAslRequestLeaseRecord.Size, 0);
		if (record.IsNull || !platform.IsMapped(record,
			MuiAslRequestLeaseRecord.Size))
		{
			if (record.IsNotNull) platform.Free(record,
				MuiAslRequestLeaseRecord.Size);
			platform.FreeRequest(requester);
			return APTR.Null;
		}
		platform.Clear(record, MuiAslRequestLeaseRecord.Size);
		if (!MuiAslServiceStateCodec.TryRead(ref platform, serviceState,
			out var state))
		{
			platform.Free(record, MuiAslRequestLeaseRecord.Size);
			platform.FreeRequest(requester);
			return APTR.Null;
		}
		MuiAslRequestLeaseRecord lease = default;
		lease.Next = state.Head;
		lease.Requester = requester;
		lease.Type = requestType;
		lease.Tags = tags;
		if (!MuiAslRequestLeaseCodec.Write(ref platform, record, lease))
		{
			platform.Free(record, MuiAslRequestLeaseRecord.Size);
			platform.FreeRequest(requester);
			return APTR.Null;
		}
		state.Head = record;
		if (!MuiAslServiceStateCodec.Write(ref platform, serviceState, state))
		{
			platform.Free(record, MuiAslRequestLeaseRecord.Size);
			platform.FreeRequest(requester);
			return APTR.Null;
		}
		return requester;
	}

	public static int AslRequest<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR requester, APTR tags)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || requester.IsNull ||
			Find(ref platform, serviceState, requester).IsNull ||
			!MuiAslTagListCore.Validate(ref platform, tags)) return 0;
		return platform.Request(requester, tags);
	}

	// Returns true when a MUI-owned requester lease was released. The public
	// vector is void-returning, but the boolean is useful to dispatchers and
	// keeps double-free behavior observable in host/native tests.
	public static bool FreeAslRequest<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR requester)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || requester.IsNull) return false;
		var record = Find(ref platform, serviceState, requester);
		if (record.IsNull) return false;
		if (!MuiAslServiceStateCodec.TryRead(ref platform, serviceState,
			out var state)) return false;
		var current = state.Head;
		APTR previous = APTR.Null;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiAslServiceLayout.MaximumTraversal)
		{
			if (!MuiAslRequestLeaseCodec.TryRead(ref platform, current,
				out var currentRecord))
				return false;
			if (current.Raw == record.Raw)
			{
				var next = currentRecord.Next;
				if (previous.IsNull)
				{
					state.Head = next;
					if (!MuiAslServiceStateCodec.Write(ref platform, serviceState,
						state)) return false;
				}
				else
				{
					if (!MuiAslRequestLeaseCodec.TryRead(ref platform, previous,
						out var previousRecord)) return false;
					previousRecord.Next = next;
					if (!MuiAslRequestLeaseCodec.Write(ref platform, previous,
						previousRecord)) return false;
				}
				platform.Free(record, MuiAslRequestLeaseRecord.Size);
				platform.FreeRequest(requester);
				return true;
			}
			previous = current;
			current = currentRecord.Next;
		}
		return false;
	}

	private static bool Ready<TPlatform>(ref TPlatform platform, APTR state)
		where TPlatform : struct, IMuiServicePlatform =>
		!state.IsNull && MuiAslServiceStateCodec.TryRead(ref platform, state,
			out var record) && record.Magic == MuiAslServiceLayout.Magic &&
		record.Generation == MuiAslServiceLayout.Version;

	private static APTR Find<TPlatform>(ref TPlatform platform, APTR state,
		APTR requester) where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiAslServiceStateCodec.TryRead(ref platform, state,
			out var service)) return APTR.Null;
		var current = service.Head;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiAslServiceLayout.MaximumTraversal)
		{
			if (!MuiAslRequestLeaseCodec.TryRead(ref platform, current,
				out var record))
				return APTR.Null;
			if (record.Requester.Raw == requester.Raw) return current;
			current = record.Next;
		}
		return APTR.Null;
	}
}
