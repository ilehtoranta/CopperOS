/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Application lifecycle policy shared by initialization, iconification, and
// active-state dispatch.  The public MUI attributes remain the projection;
// application behavior consumes this one named guest-resident record.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationLifecycleStateRecord
{
	internal const uint Size = 28;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint InitializedOffset = 4;
	internal const uint IconifiedOffset = 8;
	internal const uint ActiveOffset = 12;
	internal const uint SingleTaskOffset = 16;
	internal const uint DoubleStartOffset = 20;
	internal const uint ForceQuitOffset = 24;
	internal const uint Cookie = 0x41505354u; // 'APST'

	internal uint Magic;
	internal uint Initialized;
	internal uint Iconified;
	internal uint Active;
	internal uint SingleTask;
	internal uint DoubleStart;
	internal uint ForceQuit;
}

// Application lifecycle flags are MorphOS BOOL attributes represented as
// canonical ULONG values. Keep this admission separate from the structural
// codec so malformed guest storage can still be inspected without becoming
// consumable by lifecycle dispatch.
internal static class MuiApplicationLifecycleStateAdmission
{
	internal static bool Validate(MuiApplicationLifecycleStateRecord value) =>
		value.Magic == MuiApplicationLifecycleStateRecord.Cookie &&
		value.Initialized <= 1 && value.Iconified <= 1 && value.Active <= 1 &&
		value.SingleTask <= 1 && value.DoubleStart <= 1 &&
		value.ForceQuit <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, MuiApplicationLifecycleStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !owner.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, owner).IsNull;
}

internal enum MuiApplicationLifecycleStateField : byte
{
	Magic,
	Initialized,
	Iconified,
	Active,
	SingleTask,
	DoubleStart,
	ForceQuit,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationLifecycleStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationLifecycleStateField Field;
}

internal static class MuiApplicationLifecycleStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationLifecycleStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationLifecycleStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationLifecycleStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationLifecycleStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationLifecycleStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationLifecycleStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Fixed application lifecycle state is transferred as a named record. Numeric
// guest positions are confined to this ABI adapter; the compatibility cursor
// above remains available only to legacy callers and malformed-state tests.
internal static class MuiApplicationLifecycleStateRecordMemoryCodec
{
	private static bool TryResolve(MuiApplicationLifecycleStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationLifecycleStateField.Magic:
				offset = MuiApplicationLifecycleStateRecord.MagicOffset;
				return true;
			case MuiApplicationLifecycleStateField.Initialized:
				offset = MuiApplicationLifecycleStateRecord.InitializedOffset;
				return true;
			case MuiApplicationLifecycleStateField.Iconified:
				offset = MuiApplicationLifecycleStateRecord.IconifiedOffset;
				return true;
			case MuiApplicationLifecycleStateField.Active:
				offset = MuiApplicationLifecycleStateRecord.ActiveOffset;
				return true;
			case MuiApplicationLifecycleStateField.SingleTask:
				offset = MuiApplicationLifecycleStateRecord.SingleTaskOffset;
				return true;
			case MuiApplicationLifecycleStateField.DoubleStart:
				offset = MuiApplicationLifecycleStateRecord.DoubleStartOffset;
				return true;
			case MuiApplicationLifecycleStateField.ForceQuit:
				offset = MuiApplicationLifecycleStateRecord.ForceQuitOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationLifecycleStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiApplicationLifecycleStateRecord.Size) &&
			platform.IsMapped(address, MuiApplicationLifecycleStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationLifecycleStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationLifecycleStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationLifecycleStateRecordCodec
{
	// Lifecycle policy is a fixed seven-ULONG record. Exchange all fields in
	// declaration order as one named struct; BOOL normalization/admission stays
	// separate so structural diagnostics preserve raw guest values.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationLifecycleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationLifecycleStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Initialized) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Iconified) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Active) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.SingleTask) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.DoubleStart) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.ForceQuit) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationLifecycleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationLifecycleStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Initialized) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Iconified) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Active) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.SingleTask) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.DoubleStart) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ForceQuit) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationLifecycleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationLifecycleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationLifecycleStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationLifecycleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationLifecycleStateRecord.Size) ||
			!MuiApplicationLifecycleStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}
