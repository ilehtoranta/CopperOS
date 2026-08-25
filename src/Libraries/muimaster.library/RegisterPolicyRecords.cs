/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Register.mui keeps its two class-specific construction policies together:
// Frame is a normalized BOOL and Titles is the caller-owned guest pointer to
// the title vector.  No managed array or host string is retained.
public struct MuiRegisterPolicyState
{
	public uint Frame;
	public APTR Titles;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRegisterPolicyStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x52504752u; // 'RPGR'

	internal uint Magic;
	internal uint Frame;
	internal APTR Titles;
}

internal enum MuiRegisterPolicyStateField : byte
{
	Magic,
	Frame,
	Titles,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRegisterPolicyStateFieldCursor
{
	internal APTR Record;
	internal MuiRegisterPolicyStateField Field;
}

internal static class MuiRegisterPolicyStateFieldCursorCodec
{
	private static bool TryResolve(MuiRegisterPolicyStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiRegisterPolicyStateField.Magic:
			case MuiRegisterPolicyStateField.Frame:
			case MuiRegisterPolicyStateField.Titles:
				offset = (uint)field * 4;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiRegisterPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiRegisterPolicyStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRegisterPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiRegisterPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRegisterPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiRegisterPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiRegisterPolicyStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiRegisterPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiRegisterPolicyStateRecord.Size) ||
			!MuiRegisterPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiRegisterPolicyStateField.Magic, out var magic) ||
			magic != MuiRegisterPolicyStateRecord.Cookie ||
			!MuiRegisterPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiRegisterPolicyStateField.Frame, out var frame) ||
			!MuiRegisterPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiRegisterPolicyStateField.Titles, out var titles))
			return false;
		value.Magic = magic;
		value.Frame = frame;
		value.Titles = APTR.FromPointer(titles);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiRegisterPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiRegisterPolicyStateRecord.Size) || value.Magic !=
			MuiRegisterPolicyStateRecord.Cookie) return false;
		return MuiRegisterPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiRegisterPolicyStateField.Magic, value.Magic) &&
			MuiRegisterPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiRegisterPolicyStateField.Frame, value.Frame) &&
			MuiRegisterPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiRegisterPolicyStateField.Titles, value.Titles.Raw);
	}
}
