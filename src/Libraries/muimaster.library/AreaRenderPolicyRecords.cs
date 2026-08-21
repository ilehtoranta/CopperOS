/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared Area drawing policy.  Generic Area drawing and text helpers use one
// named guest-resident record instead of independently projecting the same
// public attributes at each call site.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaRenderPolicyStateRecord
{
	internal const uint Size = 28;
	internal const uint Cookie = 0x41525052u; // 'ARPR'

	internal uint Magic;
	internal uint FillArea;
	internal uint Background;
	internal uint Frame;
	internal uint Font;
	internal uint FrameVisible;
	internal uint FramePhantomHoriz;
}

internal enum MuiAreaRenderPolicyStateField : byte
{
	Magic,
	FillArea,
	Background,
	Frame,
	Font,
	FrameVisible,
	FramePhantomHoriz,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaRenderPolicyStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaRenderPolicyStateField Field;
}

internal static class MuiAreaRenderPolicyStateFieldCursorCodec
{
	private static bool TryResolve(MuiAreaRenderPolicyStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaRenderPolicyStateField.Magic:
			case MuiAreaRenderPolicyStateField.FillArea:
			case MuiAreaRenderPolicyStateField.Background:
			case MuiAreaRenderPolicyStateField.Frame:
			case MuiAreaRenderPolicyStateField.Font:
			case MuiAreaRenderPolicyStateField.FrameVisible:
			case MuiAreaRenderPolicyStateField.FramePhantomHoriz:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaRenderPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiAreaRenderPolicyStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaRenderPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaRenderPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaRenderPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaRenderPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaRenderPolicyStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaRenderPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaRenderPolicyStateRecord.Size) ||
			!MuiAreaRenderPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaRenderPolicyStateField.Magic, out var magic) ||
			magic != MuiAreaRenderPolicyStateRecord.Cookie ||
			!MuiAreaRenderPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaRenderPolicyStateField.FillArea, out value.FillArea) ||
			!MuiAreaRenderPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaRenderPolicyStateField.Background,
				out value.Background) ||
			!MuiAreaRenderPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaRenderPolicyStateField.Frame, out value.Frame) ||
			!MuiAreaRenderPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaRenderPolicyStateField.Font, out value.Font) ||
			!MuiAreaRenderPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaRenderPolicyStateField.FrameVisible,
				out value.FrameVisible) ||
			!MuiAreaRenderPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaRenderPolicyStateField.FramePhantomHoriz,
				out value.FramePhantomHoriz))
			return false;
		value.Magic = magic;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaRenderPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaRenderPolicyStateRecord.Size) || value.Magic !=
			MuiAreaRenderPolicyStateRecord.Cookie) return false;
		return MuiAreaRenderPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaRenderPolicyStateField.Magic, value.Magic) &&
			MuiAreaRenderPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaRenderPolicyStateField.FillArea, value.FillArea) &&
			MuiAreaRenderPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaRenderPolicyStateField.Background, value.Background) &&
			MuiAreaRenderPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaRenderPolicyStateField.Frame, value.Frame) &&
			MuiAreaRenderPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaRenderPolicyStateField.Font, value.Font) &&
			MuiAreaRenderPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaRenderPolicyStateField.FrameVisible,
				value.FrameVisible) &&
			MuiAreaRenderPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaRenderPolicyStateField.FramePhantomHoriz,
				value.FramePhantomHoriz);
	}
}
