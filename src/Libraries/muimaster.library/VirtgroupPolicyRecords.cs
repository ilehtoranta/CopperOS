/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Virtgroup policy keeps signed geometry values and BOOL policies in one
// guest-resident struct.  The existing layout record remains the layout
// scratch seam; this policy record is the public Get/Set projection.
public struct MuiVirtgroupPolicyState
{
	public uint Input;
	public int Width;
	public int Height;
	public int Left;
	public int Top;
	public uint TryFit;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiVirtgroupPolicyStateRecord
{
	internal const uint Size = 28;
	internal const uint Cookie = 0x56504750u; // 'VPGP'

	internal uint Magic;
	internal uint Input;
	internal int Width;
	internal int Height;
	internal int Left;
	internal int Top;
	internal uint TryFit;
}

internal enum MuiVirtgroupPolicyStateField : byte
{
	Magic,
	Input,
	Width,
	Height,
	Left,
	Top,
	TryFit,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiVirtgroupPolicyStateFieldCursor
{
	internal APTR Record;
	internal MuiVirtgroupPolicyStateField Field;
}

internal static class MuiVirtgroupPolicyStateFieldCursorCodec
{
	private static bool TryResolve(MuiVirtgroupPolicyStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiVirtgroupPolicyStateField.Magic:
			case MuiVirtgroupPolicyStateField.Input:
			case MuiVirtgroupPolicyStateField.Width:
			case MuiVirtgroupPolicyStateField.Height:
			case MuiVirtgroupPolicyStateField.Left:
			case MuiVirtgroupPolicyStateField.Top:
			case MuiVirtgroupPolicyStateField.TryFit:
				offset = (uint)field * 4;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiVirtgroupPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiVirtgroupPolicyStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiVirtgroupPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiVirtgroupPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiVirtgroupPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiVirtgroupPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiVirtgroupPolicyStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiVirtgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiVirtgroupPolicyStateRecord.Size) ||
			!MuiVirtgroupPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiVirtgroupPolicyStateField.Magic, out var magic) ||
			magic != MuiVirtgroupPolicyStateRecord.Cookie ||
			!MuiVirtgroupPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiVirtgroupPolicyStateField.Input, out var input) ||
			!MuiVirtgroupPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiVirtgroupPolicyStateField.Width, out var width) ||
			!MuiVirtgroupPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiVirtgroupPolicyStateField.Height, out var height) ||
			!MuiVirtgroupPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiVirtgroupPolicyStateField.Left, out var left) ||
			!MuiVirtgroupPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiVirtgroupPolicyStateField.Top, out var top) ||
			!MuiVirtgroupPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiVirtgroupPolicyStateField.TryFit, out var tryFit))
			return false;
		value.Magic = magic;
		value.Input = input == 0 ? 0u : 1u;
		value.Width = unchecked((int)width);
		value.Height = unchecked((int)height);
		value.Left = unchecked((int)left);
		value.Top = unchecked((int)top);
		value.TryFit = tryFit == 0 ? 0u : 1u;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiVirtgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiVirtgroupPolicyStateRecord.Size) || value.Magic !=
			MuiVirtgroupPolicyStateRecord.Cookie) return false;
		return MuiVirtgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiVirtgroupPolicyStateField.Magic, value.Magic) &&
			MuiVirtgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiVirtgroupPolicyStateField.Input,
				value.Input == 0 ? 0u : 1u) &&
			MuiVirtgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiVirtgroupPolicyStateField.Width,
				unchecked((uint)value.Width)) &&
			MuiVirtgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiVirtgroupPolicyStateField.Height,
				unchecked((uint)value.Height)) &&
			MuiVirtgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiVirtgroupPolicyStateField.Left,
				unchecked((uint)value.Left)) &&
			MuiVirtgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiVirtgroupPolicyStateField.Top,
				unchecked((uint)value.Top)) &&
			MuiVirtgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiVirtgroupPolicyStateField.TryFit,
				value.TryFit == 0 ? 0u : 1u);
	}
}
