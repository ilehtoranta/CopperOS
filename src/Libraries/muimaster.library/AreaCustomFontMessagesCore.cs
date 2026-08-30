/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS MUIP_OpenCustomFont and MUIP_CloseCustomFont are the two-word
// method packets documented by MUIArea. Keep both packets as named records;
// only the bounded guest-memory adapter below knows their packed positions.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaOpenCustomFontMessage
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint SpecOffset = 4;
	internal uint MethodId;
	internal APTR Spec;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaCloseCustomFontMessage
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint FontOffset = 4;
	internal uint MethodId;
	internal APTR Font;
}

internal enum MuiAreaCustomFontMessageKind : byte
{
	Open,
	Close,
}

internal enum MuiAreaCustomFontMessageField : byte
{
	MethodId,
	Pointer,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaCustomFontMessageFieldCursor
{
	internal APTR Message;
	internal MuiAreaCustomFontMessageKind Kind;
	internal MuiAreaCustomFontMessageField Field;
}

// The named record adapter owns selector-specific packet sizes and member
// boundaries. Consumers select the semantic Open/Close packet and Pointer
// field instead of supplying untyped offsets.
internal static class MuiAreaCustomFontMessageMemoryCodec
{
	private static bool TryGetPacketSize(MuiAreaCustomFontMessageKind kind,
		out uint size)
	{
		switch (kind)
		{
			case MuiAreaCustomFontMessageKind.Open:
				size = MuiAreaOpenCustomFontMessage.Size;
				return true;
			case MuiAreaCustomFontMessageKind.Close:
				size = MuiAreaCloseCustomFontMessage.Size;
				return true;
		}
		size = 0;
		return false;
	}

	private static bool TryResolve(MuiAreaCustomFontMessageKind kind,
		MuiAreaCustomFontMessageField field, out uint offset)
	{
		if (kind == MuiAreaCustomFontMessageKind.Open)
		{
			if (field == MuiAreaCustomFontMessageField.MethodId)
			{
				offset = MuiAreaOpenCustomFontMessage.MethodIdOffset;
				return true;
			}
			if (field == MuiAreaCustomFontMessageField.Pointer)
			{
				offset = MuiAreaOpenCustomFontMessage.SpecOffset;
				return true;
			}
		}
		else if (kind == MuiAreaCustomFontMessageKind.Close)
		{
			if (field == MuiAreaCustomFontMessageField.MethodId)
			{
				offset = MuiAreaCloseCustomFontMessage.MethodIdOffset;
				return true;
			}
			if (field == MuiAreaCustomFontMessageField.Pointer)
			{
				offset = MuiAreaCloseCustomFontMessage.FontOffset;
				return true;
			}
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaCustomFontMessageFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Kind, cursor.Field, out var offset) ||
			!TryGetPacketSize(cursor.Kind, out var packetSize) ||
			cursor.Message.IsNull || cursor.Message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Message, packetSize)) return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, MuiAreaOpenCustomFontMessage.FieldSize);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaCustomFontMessageKind kind,
		MuiAreaCustomFontMessageField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaCustomFontMessageFieldCursor);
		cursor.Message = message;
		cursor.Kind = kind;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaCustomFontMessageKind kind,
		MuiAreaCustomFontMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaCustomFontMessageFieldCursor);
		cursor.Message = message;
		cursor.Kind = kind;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaCustomFontMessageKind kind,
		MuiAreaCustomFontMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaCustomFontMessageFieldCursor);
		cursor.Message = message;
		cursor.Kind = kind;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaOpenCustomFontMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaOpenCustomFontMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaOpenCustomFontMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawSpec) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Spec = APTR.FromPointer(rawSpec);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaOpenCustomFontMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaOpenCustomFontMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Spec.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiAreaCloseCustomFontMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaCloseCustomFontMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaCloseCustomFontMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawFont) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Font = APTR.FromPointer(rawFont);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaCloseCustomFontMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaCloseCustomFontMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Font.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Compatibility adapter retained for callers that still construct the typed
// field cursor. Live custom-font packet consumers use the named record adapter.
internal static class MuiAreaCustomFontMessageFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaCustomFontMessageFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaCustomFontMessageMemoryCodec.TryGetAddress(ref platform, cursor,
			out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaCustomFontMessageKind kind,
		MuiAreaCustomFontMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaCustomFontMessageMemoryCodec.TryReadUInt32(ref platform, message,
			kind, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaCustomFontMessageKind kind,
		MuiAreaCustomFontMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaCustomFontMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			kind, field, value);
}

internal static class MuiAreaCustomFontMessageCodec
{
	internal const uint CloseCustomFont = 0x8042B27Cu;
	internal const uint OpenCustomFont = 0x8042F3DCu;

	internal static bool IsMethod(uint method) => method == CloseCustomFont ||
		method == OpenCustomFont;

	internal static bool TryReadOpen<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaOpenCustomFontMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaOpenCustomFontMessageCodec.TryRead(ref platform, message,
			out packet) || packet.MethodId != OpenCustomFont)
		{
			packet = default;
			return false;
		}
		return true;
	}

	internal static bool TryReadClose<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaCloseCustomFontMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaCloseCustomFontMessageCodec.TryRead(ref platform, message,
			out packet) || packet.MethodId != CloseCustomFont)
		{
			packet = default;
			return false;
		}
		return true;
	}

	internal static bool WriteOpen<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaOpenCustomFontMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaOpenCustomFontMessageCodec.Write(ref platform, message,
			packet);
	}

	internal static bool WriteClose<TPlatform>(ref TPlatform platform,
		APTR message, MuiAreaCloseCustomFontMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaCloseCustomFontMessageCodec.Write(ref platform, message,
			packet);
	}

	internal static bool WriteOpen<TPlatform>(ref TPlatform platform,
		APTR message, APTR spec) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaOpenCustomFontMessage);
		packet.MethodId = OpenCustomFont;
		packet.Spec = spec;
		return WriteOpen(ref platform, message, packet);
	}

	internal static bool WriteClose<TPlatform>(ref TPlatform platform,
		APTR message, APTR font) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiAreaCloseCustomFontMessage);
		packet.MethodId = CloseCustomFont;
		packet.Font = font;
		return WriteClose(ref platform, message, packet);
	}
}

public static class MuiAreaCustomFontMessageCore
{
	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!MuiAreaCustomFontMessageMemoryCodec.TryReadUInt32(
			ref platform, message,
			MuiAreaCustomFontMessageKind.Open,
			MuiAreaCustomFontMessageField.MethodId, out var method) ||
			!MuiAreaCustomFontMessageCodec.IsMethod(method)) return 0;
		if (method == MuiAreaCustomFontMessageCodec.OpenCustomFont)
		{
			if (!MuiAreaCustomFontMessageCodec.TryReadOpen(ref platform, message,
				out var open)) return 0;
			return MuiAreaCustomFontPacketCore.TryOpenCustomFont(ref platform,
				state, obj, open.Spec, out var opened) ? opened.Font.Raw : 0;
		}
		if (!MuiAreaCustomFontMessageCodec.TryReadClose(ref platform, message,
			out var close)) return 0;
		MuiAreaCustomFontPacketCore.CloseCustomFont(ref platform, state, obj,
			close.Font);
		return 0;
	}
}
