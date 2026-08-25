/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS MUIA_Floating is documented as an Area [ISG] BOOL.  MorphOS's
// public autodoc intentionally does not define the placement effect, so this
// record owns the normalized public value without inventing layout semantics.
public struct MuiAreaFloatingStateInput
{
	public uint Enabled;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaFloatingStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x41464C54u; // 'AFLT'

	internal uint Magic;
	internal uint Enabled;
	internal uint Generation;
}

internal enum MuiAreaFloatingStateField : byte
{
	Magic,
	Enabled,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaFloatingStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaFloatingStateField Field;
}

internal static class MuiAreaFloatingStateFieldCursorCodec
{
	private static bool TryResolve(MuiAreaFloatingStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaFloatingStateField.Magic:
				offset = 0;
				return true;
			case MuiAreaFloatingStateField.Enabled:
				offset = 4;
				return true;
			case MuiAreaFloatingStateField.Generation:
				offset = 8;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaFloatingStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiAreaFloatingStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFloatingStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaFloatingStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFloatingStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaFloatingStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaFloatingStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaFloatingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaFloatingStateRecord.Size) ||
			!MuiAreaFloatingStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaFloatingStateField.Magic, out var magic) ||
			magic != MuiAreaFloatingStateRecord.Cookie ||
			!MuiAreaFloatingStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaFloatingStateField.Enabled, out value.Enabled) ||
			!MuiAreaFloatingStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaFloatingStateField.Generation,
				out value.Generation)) return false;
		value.Magic = magic;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaFloatingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaFloatingStateRecord.Size) || value.Magic !=
			MuiAreaFloatingStateRecord.Cookie) return false;
		return MuiAreaFloatingStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaFloatingStateField.Magic, value.Magic) &&
			MuiAreaFloatingStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaFloatingStateField.Enabled, value.Enabled) &&
			MuiAreaFloatingStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaFloatingStateField.Generation,
				value.Generation);
	}
}

internal static class MuiAreaFloatingCore
{
	internal const uint StateKey = 0x7F070047u;

	internal static bool TryReadState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiAreaFloatingStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var enabled = 0u;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			MuiCommonControlCore.Floating, out var raw))
			enabled = raw == 0 ? 0u : 1u;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj, StateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj, StateKey) ==
			unchecked((int)MuiAreaFloatingStateRecord.Size) &&
			MuiAreaFloatingStateRecordCodec.TryRead(ref platform, block,
				out var record))
		{
			if (record.Enabled != enabled)
			{
				record.Enabled = enabled;
				record.Generation = record.Generation == uint.MaxValue ? 1u :
					record.Generation + 1u;
				if (!MuiAreaFloatingStateRecordCodec.Write(ref platform, block,
					record)) return false;
			}
			value.Enabled = record.Enabled;
			return true;
		}
		if (!WriteState(ref platform, state, obj, enabled, 1)) return false;
		value.Enabled = enabled;
		return true;
	}

	internal static bool WriteState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint enabled, uint generation)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiAreaFloatingStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiAreaFloatingStateRecord.Size);
		var record = default(MuiAreaFloatingStateRecord);
		record.Magic = MuiAreaFloatingStateRecord.Cookie;
		record.Enabled = enabled == 0 ? 0u : 1u;
		record.Generation = generation == 0 ? 1u : generation;
		var written = MuiAreaFloatingStateRecordCodec.Write(ref platform,
			scratch, record);
		var stored = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateKey, scratch, unchecked((int)MuiAreaFloatingStateRecord.Size));
		platform.Clear(scratch, MuiAreaFloatingStateRecord.Size);
		platform.Free(scratch, MuiAreaFloatingStateRecord.Size);
		return stored;
	}
}

public static class MuiAreaFloatingPacketCore
{
	public static bool Set<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint enabled)
		where TPlatform : struct, IMuiLayoutPlatform =>
		MuiCommonControlCore.SetControlAttribute(ref platform, state, obj,
			MuiCommonControlCore.Floating, enabled, true);

	public static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiAreaFloatingStateInput value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiAreaFloatingCore.TryReadState(ref platform, state, obj, out value);
}
