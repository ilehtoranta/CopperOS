/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Window interaction state shared by Snapshot and cycle-chain/active-object
// methods. Public attributes remain projections; the remembered position
// request and copied cycle chain are kept in one named guest record.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowInteractionStateRecord
{
	internal const uint Size = 24;
	internal const uint Cookie = 0x57495354u; // 'WIST'

	internal uint Magic;
	internal uint SnapshotFlags;
	internal uint SnapshotRequests;
	internal APTR CycleChainHead;
	internal uint CycleChainCount;
	internal uint CycleChainRequests;
}

internal enum MuiWindowInteractionStateField : byte
{
	Magic,
	SnapshotFlags,
	SnapshotRequests,
	CycleChainHead,
	CycleChainCount,
	CycleChainRequests,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowInteractionStateFieldCursor
{
	internal APTR Record;
	internal MuiWindowInteractionStateField Field;
}

internal static class MuiWindowInteractionStateFieldCursorCodec
{
	private static bool TryResolve(MuiWindowInteractionStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiWindowInteractionStateField.Magic:
			case MuiWindowInteractionStateField.SnapshotFlags:
			case MuiWindowInteractionStateField.SnapshotRequests:
			case MuiWindowInteractionStateField.CycleChainHead:
			case MuiWindowInteractionStateField.CycleChainCount:
			case MuiWindowInteractionStateField.CycleChainRequests:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowInteractionStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiWindowInteractionStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInteractionStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiWindowInteractionStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInteractionStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiWindowInteractionStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiWindowInteractionStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiWindowInteractionStateRecord.Size) ||
			!MuiWindowInteractionStateFieldCursorCodec.TryReadUInt32(
				ref platform, address, MuiWindowInteractionStateField.Magic,
				out var magic) || magic != MuiWindowInteractionStateRecord.Cookie ||
			!MuiWindowInteractionStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiWindowInteractionStateField.SnapshotFlags,
				out value.SnapshotFlags) ||
			!MuiWindowInteractionStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiWindowInteractionStateField.SnapshotRequests,
				out value.SnapshotRequests) ||
			!MuiWindowInteractionStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiWindowInteractionStateField.CycleChainHead,
				out var cycleHead) ||
			!MuiWindowInteractionStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiWindowInteractionStateField.CycleChainCount,
				out value.CycleChainCount) ||
			!MuiWindowInteractionStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiWindowInteractionStateField.CycleChainRequests,
				out value.CycleChainRequests)) return false;
		value.Magic = magic;
		value.CycleChainHead = APTR.FromPointer(cycleHead);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiWindowInteractionStateRecord.Size) || value.Magic !=
			MuiWindowInteractionStateRecord.Cookie) return false;
		return MuiWindowInteractionStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiWindowInteractionStateField.Magic,
			value.Magic) &&
			MuiWindowInteractionStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowInteractionStateField.SnapshotFlags,
				value.SnapshotFlags) &&
			MuiWindowInteractionStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowInteractionStateField.SnapshotRequests,
				value.SnapshotRequests) &&
			MuiWindowInteractionStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowInteractionStateField.CycleChainHead,
				value.CycleChainHead.Raw) &&
			MuiWindowInteractionStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowInteractionStateField.CycleChainCount,
				value.CycleChainCount) &&
			MuiWindowInteractionStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowInteractionStateField.CycleChainRequests,
				value.CycleChainRequests);
	}
}
