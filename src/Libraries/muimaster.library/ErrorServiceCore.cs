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

internal static class MuiErrorServiceStateFieldCursorCodec
{
	private static bool TryResolve(MuiErrorServiceStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiErrorServiceStateField.Magic => 0,
			MuiErrorServiceStateField.Version => 4,
			MuiErrorServiceStateField.Error => 8,
			MuiErrorServiceStateField.Sequence => 12,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiErrorServiceStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiErrorServiceStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiErrorServiceStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiErrorServiceStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiErrorServiceStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiErrorServiceStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiErrorServiceStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiErrorServiceStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiErrorServiceStateRecord.Size)) return false;
		if (!MuiErrorServiceStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiErrorServiceStateField.Magic, out record.Magic) ||
			!MuiErrorServiceStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiErrorServiceStateField.Version, out record.Version) ||
			!MuiErrorServiceStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiErrorServiceStateField.Error, out record.Error) ||
			!MuiErrorServiceStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiErrorServiceStateField.Sequence, out record.Sequence)) return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiErrorServiceStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiErrorServiceStateRecord.Size)) return false;
		return MuiErrorServiceStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiErrorServiceStateField.Magic, record.Magic) &&
			MuiErrorServiceStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiErrorServiceStateField.Version, record.Version) &&
			MuiErrorServiceStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiErrorServiceStateField.Error, record.Error) &&
			MuiErrorServiceStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiErrorServiceStateField.Sequence, record.Sequence);
	}
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

// Native-safe MUI_Error/MUI_SetError state. The public MUI API exposes a
// process-local error value; CopperOS stores that value in an explicit guest
// record so the production path has no managed static state or runtime service.
public static class MuiErrorServiceCore
{
	public static bool Initialize<TPlatform>(ref TPlatform platform,
		APTR serviceState) where TPlatform : struct, IMuiServicePlatform
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

	// MUI_Error(). An uninitialised service has the documented neutral value.
	public static int Error<TPlatform>(ref TPlatform platform, APTR serviceState)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) ||
			!MuiErrorServiceStateCodec.TryRead(ref platform, serviceState,
				out var record)) return 0;
		return unchecked((int)record.Error);
	}

	// MUI_SetError() returns the previous error value, then publishes the updated
	// value. The sequence is diagnostic guest state and is not part of the ABI.
	public static int SetError<TPlatform>(ref TPlatform platform,
		APTR serviceState, int error) where TPlatform : struct, IMuiServicePlatform
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
		where TPlatform : struct, IMuiServicePlatform =>
		!state.IsNull && MuiErrorServiceStateCodec.TryRead(ref platform, state,
			out var record) && record.Magic == MuiErrorServiceLayout.Magic &&
		record.Version == MuiErrorServiceLayout.Version;
}
