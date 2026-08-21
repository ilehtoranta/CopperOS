/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

internal static class MuiRequesterServiceLayout
{
	public const uint Magic = 0x4D554952; // "MUIR"
	public const uint Version = 1;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRequesterServiceStateRecord
{
	internal const uint Size = 8;
	internal uint Magic;
	internal uint Generation;
}

internal enum MuiRequesterServiceStateField : byte
{
	Magic,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRequesterServiceStateFieldCursor
{
	internal APTR Record;
	internal MuiRequesterServiceStateField Field;
}

internal static class MuiRequesterServiceStateFieldCursorCodec
{
	private static bool TryResolve(MuiRequesterServiceStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiRequesterServiceStateField.Magic => 0,
			MuiRequesterServiceStateField.Generation => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiRequesterServiceStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiRequesterServiceStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRequesterServiceStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiRequesterServiceStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRequesterServiceStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiRequesterServiceStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiRequesterServiceStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiRequesterServiceStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiRequesterServiceStateRecord.Size)) return false;
		if (!MuiRequesterServiceStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiRequesterServiceStateField.Magic, out record.Magic) ||
			!MuiRequesterServiceStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiRequesterServiceStateField.Generation,
				out record.Generation)) return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiRequesterServiceStateRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiRequesterServiceStateRecord.Size)) return false;
		return MuiRequesterServiceStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiRequesterServiceStateField.Magic, record.Magic) &&
			MuiRequesterServiceStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiRequesterServiceStateField.Generation, record.Generation);
	}
}

// Scalar qualification surface for the synchronous requester service state.
public static class MuiRequesterServiceRecordPacketCore
{
	public static bool WriteState<TPlatform>(ref TPlatform platform, APTR address,
		uint magic, uint generation)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiRequesterServiceStateRecord record = default;
		record.Magic = magic;
		record.Generation = generation;
		return MuiRequesterServiceStateCodec.Write(ref platform, address, record);
	}

	public static uint DispatchState<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiRequesterServiceStateCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Magic ^ record.Generation;
	}
}

// Bounded synchronous MUI_RequestA/MUI_RequestObjectA gateway. Title and gadget
// arguments stay caller-owned guest pointers; the payload core validates their
// bounded representation and the formatter materializes supported conversions
// into a temporary guest C string before entry. The object form consumes one
// caller-owned reference after the modal request closes; callers that need the
// object afterwards must retain it before entry, as the autodoc requires.
// No managed state, exceptions, callbacks, or host UI are used in production.
public static class MuiRequesterServiceCore
{
	public static bool Initialize<TPlatform>(ref TPlatform platform,
		APTR serviceState) where TPlatform : struct, IMuiServicePlatform
	{
		if (serviceState.IsNull ||
			!platform.IsMapped(serviceState, MuiRequesterServiceStateRecord.Size))
			return false;
		if (MuiRequesterServiceStateCodec.TryRead(ref platform, serviceState,
			out var current) && current.Magic == MuiRequesterServiceLayout.Magic &&
			current.Generation == MuiRequesterServiceLayout.Version) return true;
		platform.Clear(serviceState, MuiRequesterServiceStateRecord.Size);
		MuiRequesterServiceStateRecord record = default;
		record.Magic = MuiRequesterServiceLayout.Magic;
		record.Generation = MuiRequesterServiceLayout.Version;
		return MuiRequesterServiceStateCodec.Write(ref platform, serviceState,
			record);
	}

	public static int Request<TPlatform>(ref TPlatform platform, APTR serviceState,
		APTR application, APTR window, uint flags, APTR title, APTR gadgets,
		APTR format, APTR parameters) where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || flags != 0 ||
			!MuiRequesterPayloadCore.Validate(ref platform, title, gadgets,
				format, parameters)) return 0;
		if (!MuiRequesterFormatCore.TryMaterialize(ref platform, format, parameters,
			out var preparedFormat, out var allocationSize)) return 0;
		APTR preparedParameters = parameters;
		if (allocationSize != 0) preparedParameters = APTR.Null;
		var result = platform.Request(application, window, flags, title, gadgets,
			preparedFormat, preparedParameters);
		if (allocationSize != 0) platform.Free(preparedFormat, allocationSize);
		return result;
	}

	public static int RequestObject<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR application, APTR window, uint flags, APTR title,
		APTR gadgets, APTR obj, APTR format, APTR parameters)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!Ready(ref platform, serviceState) || flags != 0 ||
			!MuiRequesterPayloadCore.Validate(ref platform, title, gadgets,
				format, parameters) || obj.IsNull) return 0;
		if (!MuiRequesterFormatCore.TryMaterialize(ref platform, format, parameters,
			out var preparedFormat, out var allocationSize)) return 0;
		APTR preparedParameters = parameters;
		if (allocationSize != 0) preparedParameters = APTR.Null;
		var result = platform.RequestObject(application, window, flags, title,
			gadgets, obj, preparedFormat, preparedParameters);
		if (allocationSize != 0) platform.Free(preparedFormat, allocationSize);
		// The requester consumes one object reference when it closes. A caller
		// that needs the object afterwards must issue OM_RETAIN before entering,
		// exactly as documented for MUI_RequestObjectA.
		platform.ReleaseObject(obj);
		return result;
	}

	private static bool Ready<TPlatform>(ref TPlatform platform, APTR state)
		where TPlatform : struct, IMuiServicePlatform =>
		!state.IsNull && MuiRequesterServiceStateCodec.TryRead(ref platform, state,
			out var record) && record.Magic == MuiRequesterServiceLayout.Magic &&
		record.Generation == MuiRequesterServiceLayout.Version;
}
