/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS MUIP_OpenCustomFont and MUIP_CloseCustomFont are the two-word
// method packets documented by MUIArea. Keep both packets as named records;
// only the guest ABI cursor below knows their packed wire positions.
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
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiAreaOpenCustomFontMessage.Size) ||
			!MuiAreaCustomFontMessageFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiAreaCustomFontMessageKind.Open,
				MuiAreaCustomFontMessageField.MethodId, out packet.MethodId) ||
			packet.MethodId != OpenCustomFont ||
			!MuiAreaCustomFontMessageFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiAreaCustomFontMessageKind.Open,
				MuiAreaCustomFontMessageField.Pointer, out var spec)) return false;
		packet.Spec = APTR.FromPointer(spec);
		return true;
	}

	internal static bool TryReadClose<TPlatform>(ref TPlatform platform,
		APTR message, out MuiAreaCloseCustomFontMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiAreaCloseCustomFontMessage.Size) ||
			!MuiAreaCustomFontMessageFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiAreaCustomFontMessageKind.Close,
				MuiAreaCustomFontMessageField.MethodId, out packet.MethodId) ||
			packet.MethodId != CloseCustomFont ||
			!MuiAreaCustomFontMessageFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiAreaCustomFontMessageKind.Close,
				MuiAreaCustomFontMessageField.Pointer, out var font)) return false;
		packet.Font = APTR.FromPointer(font);
		return true;
	}

	internal static bool WriteOpen<TPlatform>(ref TPlatform platform,
		APTR message, APTR spec) where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiAreaOpenCustomFontMessage.Size)) return false;
		return MuiAreaCustomFontMessageFieldCursorCodec.TryWriteUInt32(
			ref platform, message, MuiAreaCustomFontMessageKind.Open,
			MuiAreaCustomFontMessageField.MethodId, OpenCustomFont) &&
			MuiAreaCustomFontMessageFieldCursorCodec.TryWriteUInt32(
				ref platform, message, MuiAreaCustomFontMessageKind.Open,
				MuiAreaCustomFontMessageField.Pointer, spec.Raw);
	}

	internal static bool WriteClose<TPlatform>(ref TPlatform platform,
		APTR message, APTR font) where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiAreaCloseCustomFontMessage.Size)) return false;
		return MuiAreaCustomFontMessageFieldCursorCodec.TryWriteUInt32(
			ref platform, message, MuiAreaCustomFontMessageKind.Close,
			MuiAreaCustomFontMessageField.MethodId, CloseCustomFont) &&
			MuiAreaCustomFontMessageFieldCursorCodec.TryWriteUInt32(
				ref platform, message, MuiAreaCustomFontMessageKind.Close,
				MuiAreaCustomFontMessageField.Pointer, font.Raw);
	}
}

public static class MuiAreaCustomFontMessageCore
{
	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!MuiAreaCustomFontMessageFieldCursorCodec.TryReadUInt32(
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
