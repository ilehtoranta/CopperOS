/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

internal static class MuiErrorServiceLayout
{
	public const uint Magic = 0x4D554945; // "MUIE"
	public const uint Version = 1;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiErrorServiceStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint VersionOffset = 4;
	internal const uint ErrorOffset = 8;
	internal const uint SequenceOffset = 12;
	internal uint Magic;
	internal uint Version;
	internal uint Error;
	internal uint Sequence;
}

internal enum MuiErrorServiceStateField : byte
{
	Magic,
	Version,
	Error,
	Sequence,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiErrorServiceStateFieldCursor
{
	internal APTR Record;
	internal MuiErrorServiceStateField Field;
}

// Struct-first guest-memory adapter for the retired provisional error record.
internal static class MuiErrorServiceStateMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiErrorServiceStateField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (field == MuiErrorServiceStateField.Magic)
			return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiErrorServiceStateRecord.FieldSize, out address);
		if (field == MuiErrorServiceStateField.Version)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiErrorServiceStateRecord.FieldSize, out _)) return false;
			return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiErrorServiceStateRecord.FieldSize, out address);
		}
		if (field == MuiErrorServiceStateField.Error)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiErrorServiceStateRecord.FieldSize, out _) ||
				!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiErrorServiceStateRecord.FieldSize, out _)) return false;
			return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiErrorServiceStateRecord.FieldSize, out address);
		}
		if (field == MuiErrorServiceStateField.Sequence)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiErrorServiceStateRecord.FieldSize, out _) ||
				!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiErrorServiceStateRecord.FieldSize, out _) ||
				!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiErrorServiceStateRecord.FieldSize, out _)) return false;
			return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiErrorServiceStateRecord.FieldSize, out address);
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiErrorServiceStateField field, out APTR address)
	where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, record,
			MuiErrorServiceStateRecord.Size, out var cursor) ||
			!TryTakeField(ref platform, ref cursor, field, out address)) return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiErrorServiceStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiErrorServiceStateStructCodec.TryRead(ref platform, record,
			out var state)) return false;
		if (field == MuiErrorServiceStateField.Magic)
			value = state.Magic;
		else if (field == MuiErrorServiceStateField.Version)
			value = state.Version;
		else if (field == MuiErrorServiceStateField.Error)
			value = state.Error;
		else if (field == MuiErrorServiceStateField.Sequence)
			value = state.Sequence;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiErrorServiceStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiErrorServiceStateStructCodec.TryRead(ref platform, record,
			out var state)) return false;
		if (field == MuiErrorServiceStateField.Magic)
			state.Magic = value;
		else if (field == MuiErrorServiceStateField.Version)
			state.Version = value;
		else if (field == MuiErrorServiceStateField.Error)
			state.Error = value;
		else if (field == MuiErrorServiceStateField.Sequence)
			state.Sequence = value;
		else return false;
		return MuiErrorServiceStateStructCodec.Write(ref platform, record, state);
	}
}

// Compatibility wrapper retained for typed cursor callers; production state
// access routes through the named-record adapter above.
internal static class MuiErrorServiceStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiErrorServiceStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiErrorServiceStateMemoryCodec.TryGetAddress(ref platform, cursor.Record,
			cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiErrorServiceStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiErrorServiceStateMemoryCodec.TryReadUInt32(ref platform, record, field,
			out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiErrorServiceStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiErrorServiceStateMemoryCodec.TryWriteUInt32(ref platform, record, field,
		value);
}

// Sequential codec for the complete legacy error record. The field cursor is
// retained for compatibility tests; the exported error vectors no longer use
// this storage and instead call DOS IoErr/SetIoErr.
internal static class MuiErrorServiceStateStructCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiErrorServiceStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiErrorServiceStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Version) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Error) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Sequence) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiErrorServiceStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiErrorServiceStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Magic) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Version) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Error) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Sequence)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiErrorServiceStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiErrorServiceStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiErrorServiceStateStructCodec.TryRead(ref platform, address,
			out record);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiErrorServiceStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiErrorServiceStateStructCodec.Write(ref platform, address, record);
}

// Scalar qualification surface for the process-local MUI error record.
public static class MuiErrorServiceRecordPacketCore
{
	public static bool WriteState<TPlatform>(ref TPlatform platform, APTR address,
		uint magic, uint version, uint error, uint sequence)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiErrorServiceStateRecord record = default;
		record.Magic = magic;
		record.Version = version;
		record.Error = error;
		record.Sequence = sequence;
		return MuiErrorServiceStateCodec.Write(ref platform, address, record);
	}

	public static uint DispatchState<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiErrorServiceStateCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Magic ^ record.Version ^ record.Error ^ record.Sequence;
	}
}

// Retained non-native model of the old provisional error record. The exported
// MorphOS vectors no longer call this helper; they forward to DOS IoErr and
// SetIoErr. Keep this state core isolated from native vector behavior.
public static class MuiErrorServiceCore
{
	public static bool Initialize<TPlatform>(ref TPlatform platform,
		APTR serviceState) where TPlatform : struct, IMuiGuestMemory
	{
		if (serviceState.IsNull ||
			!platform.IsMapped(serviceState, MuiErrorServiceStateRecord.Size))
			return false;
		if (MuiErrorServiceStateCodec.TryRead(ref platform, serviceState,
			out var current) && current.Magic == MuiErrorServiceLayout.Magic &&
			current.Version == MuiErrorServiceLayout.Version)
			return true;
		platform.Clear(serviceState, MuiErrorServiceStateRecord.Size);
		MuiErrorServiceStateRecord record = default;
		record.Magic = MuiErrorServiceLayout.Magic;
		record.Version = MuiErrorServiceLayout.Version;
		return MuiErrorServiceStateCodec.Write(ref platform, serviceState, record);
	}

	// An uninitialised service returns zero under the provisional local policy.
	public static int Error<TPlatform>(ref TPlatform platform, APTR serviceState)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!Ready(ref platform, serviceState) ||
			!MuiErrorServiceStateCodec.TryRead(ref platform, serviceState,
				out var record)) return 0;
		return unchecked((int)record.Error);
	}

	// Returning the previous value is provisional CopperOS policy, not an
	// established MorphOS contract. Sequence is diagnostic guest state only.
	public static int SetError<TPlatform>(ref TPlatform platform,
		APTR serviceState, int error) where TPlatform : struct, IMuiGuestMemory
	{
		if (!Ready(ref platform, serviceState)) return 0;
		if (!MuiErrorServiceStateCodec.TryRead(ref platform, serviceState,
			out var record)) return 0;
		var previous = unchecked((int)record.Error);
		record.Error = unchecked((uint)error);
		record.Sequence++;
		if (!MuiErrorServiceStateCodec.Write(ref platform, serviceState, record))
			return 0;
		return previous;
	}

	private static bool Ready<TPlatform>(ref TPlatform platform, APTR state)
		where TPlatform : struct, IMuiGuestMemory =>
		!state.IsNull && MuiErrorServiceStateCodec.TryRead(ref platform, state,
			out var record) && record.Magic == MuiErrorServiceLayout.Magic &&
		record.Version == MuiErrorServiceLayout.Version;
}
