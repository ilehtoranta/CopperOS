/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS MUI_BoopsiQuery / MUIP_BoopsiQuery packet.  The public SDK exposes
// this as a typed alias rather than a separately generated MUIP declaration;
// keep the complete guest record here so callers do not have to reconstruct
// it from ad-hoc byte offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBoopsiQueryMessage
{
	internal const uint Size = 40;
	internal const uint Method = 0x80427157;

	internal uint MethodId;
	internal APTR Screen;
	internal uint Flags;
	internal int MinWidth;
	internal int MinHeight;
	internal int MaxWidth;
	internal int MaxHeight;
	internal int DefaultWidth;
	internal int DefaultHeight;
	internal APTR RenderInfo;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBoopsiQueryMethodMessage
{
	internal const uint Size = 4;
	internal uint MethodId;
}

internal enum MuiBoopsiQueryPacketField : byte
{
	MethodId,
	Screen,
	Flags,
	MinWidth,
	MinHeight,
	MaxWidth,
	MaxHeight,
	DefaultWidth,
	DefaultHeight,
	RenderInfo,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBoopsiQueryPacketFieldCursor
{
	internal APTR Message;
	internal MuiBoopsiQueryPacketField Field;
}

internal static class MuiBoopsiQueryPacketFieldCursorCodec
{
	private static bool TryResolve(MuiBoopsiQueryPacketField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiBoopsiQueryPacketField.MethodId: offset = 0; return true;
			case MuiBoopsiQueryPacketField.Screen: offset = 4; return true;
			case MuiBoopsiQueryPacketField.Flags: offset = 8; return true;
			case MuiBoopsiQueryPacketField.MinWidth: offset = 12; return true;
			case MuiBoopsiQueryPacketField.MinHeight: offset = 16; return true;
			case MuiBoopsiQueryPacketField.MaxWidth: offset = 20; return true;
			case MuiBoopsiQueryPacketField.MaxHeight: offset = 24; return true;
			case MuiBoopsiQueryPacketField.DefaultWidth: offset = 28; return true;
			case MuiBoopsiQueryPacketField.DefaultHeight: offset = 32; return true;
			case MuiBoopsiQueryPacketField.RenderInfo: offset = 36; return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBoopsiQueryPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Message.IsNull ||
			cursor.Message.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiBoopsiQueryPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiBoopsiQueryPacketFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiBoopsiQueryPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiBoopsiQueryPacketFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// The packed guest record is decoded and encoded in one place.  Consumers use
// the named fields above; only this ABI codec carries the fixed byte offsets
// required by the 68k wire layout.
internal static class MuiBoopsiQueryMessageCodec
{
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiBoopsiQueryMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiBoopsiQueryMethodMessage.Size)) return false;
		return MuiBoopsiQueryPacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiBoopsiQueryPacketField.MethodId, out packet.MethodId);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiBoopsiQueryMessage record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiBoopsiQueryMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != MuiBoopsiQueryMessage.Method)
			return false;
		if (!MuiBoopsiQueryPacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiBoopsiQueryPacketField.Screen, out var rawScreen) ||
			!MuiBoopsiQueryPacketFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiBoopsiQueryPacketField.Flags, out record.Flags) ||
			!MuiBoopsiQueryPacketFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiBoopsiQueryPacketField.MinWidth, out var rawMinWidth) ||
			!MuiBoopsiQueryPacketFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiBoopsiQueryPacketField.MinHeight,
				out var rawMinHeight) ||
			!MuiBoopsiQueryPacketFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiBoopsiQueryPacketField.MaxWidth, out var rawMaxWidth) ||
			!MuiBoopsiQueryPacketFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiBoopsiQueryPacketField.MaxHeight,
				out var rawMaxHeight) ||
			!MuiBoopsiQueryPacketFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiBoopsiQueryPacketField.DefaultWidth,
				out var rawDefaultWidth) ||
			!MuiBoopsiQueryPacketFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiBoopsiQueryPacketField.DefaultHeight,
				out var rawDefaultHeight) ||
			!MuiBoopsiQueryPacketFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiBoopsiQueryPacketField.RenderInfo,
				out var rawRenderInfo)) return false;
		record.MethodId = header.MethodId;
		record.Screen = APTR.FromPointer(rawScreen);
		record.MinWidth = unchecked((int)rawMinWidth);
		record.MinHeight = unchecked((int)rawMinHeight);
		record.MaxWidth = unchecked((int)rawMaxWidth);
		record.MaxHeight = unchecked((int)rawMaxHeight);
		record.DefaultWidth = unchecked((int)rawDefaultWidth);
		record.DefaultHeight = unchecked((int)rawDefaultHeight);
		record.RenderInfo = APTR.FromPointer(rawRenderInfo);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiBoopsiQueryMessage record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiBoopsiQueryMessage.Size)) return false;
		return MuiBoopsiQueryPacketFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiBoopsiQueryPacketField.MethodId,
			MuiBoopsiQueryMessage.Method) &&
			MuiBoopsiQueryPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiBoopsiQueryPacketField.Screen, record.Screen.Raw) &&
			MuiBoopsiQueryPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiBoopsiQueryPacketField.Flags, record.Flags) &&
			MuiBoopsiQueryPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiBoopsiQueryPacketField.MinWidth,
				unchecked((uint)record.MinWidth)) &&
			MuiBoopsiQueryPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiBoopsiQueryPacketField.MinHeight,
				unchecked((uint)record.MinHeight)) &&
			MuiBoopsiQueryPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiBoopsiQueryPacketField.MaxWidth,
				unchecked((uint)record.MaxWidth)) &&
			MuiBoopsiQueryPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiBoopsiQueryPacketField.MaxHeight,
				unchecked((uint)record.MaxHeight)) &&
			MuiBoopsiQueryPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiBoopsiQueryPacketField.DefaultWidth,
				unchecked((uint)record.DefaultWidth)) &&
			MuiBoopsiQueryPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiBoopsiQueryPacketField.DefaultHeight,
				unchecked((uint)record.DefaultHeight)) &&
			MuiBoopsiQueryPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiBoopsiQueryPacketField.RenderInfo,
				record.RenderInfo.Raw);
	}
}

// Struct-only ABI bridge for the MorphOS BoopsiQuery envelope.  The actual
// external BOOPSI query callback remains a separate capability goal: this
// seam validates the complete fixed record without manufacturing dimensions or
// invoking a managed callback.
public static class MuiBoopsiQueryCore
{
	public const uint Method = MuiBoopsiQueryMessage.Method;
	public const uint PacketSize = MuiBoopsiQueryMessage.Size;

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiBoopsiQueryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiBoopsiQueryMessageCodec.TryRead(ref platform, message,
			out packet);

	public static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR message, APTR screen, uint flags, int minWidth, int minHeight,
		int maxWidth, int maxHeight, int defaultWidth, int defaultHeight,
		APTR renderInfo)
		where TPlatform : struct, IMuiGuestMemory
	{
		var record = default(MuiBoopsiQueryMessage);
		record.MethodId = MuiBoopsiQueryMessage.Method;
		record.Screen = screen;
		record.Flags = flags;
		record.MinWidth = minWidth;
		record.MinHeight = minHeight;
		record.MaxWidth = maxWidth;
		record.MaxHeight = maxHeight;
		record.DefaultWidth = defaultWidth;
		record.DefaultHeight = defaultHeight;
		record.RenderInfo = renderInfo;
		return MuiBoopsiQueryMessageCodec.TryWrite(ref platform, message,
			record);
	}

	// The result is intentionally the caller-supplied flags field.  This is a
	// packet qualification token only; MorphOS class-specific query results are
	// not inferred until the external BOOPSI callback capability is available.
	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
	{
		return TryRead(ref platform, message, out var packet) ? packet.Flags : 0;
	}
}
