/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
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
	internal const uint FieldSize = 4;
	internal const uint Method = 0x80427157;
	internal const uint MethodIdOffset = 0;
	internal const uint ScreenOffset = 4;
	internal const uint FlagsOffset = 8;
	internal const uint MinWidthOffset = 12;
	internal const uint MinHeightOffset = 16;
	internal const uint MaxWidthOffset = 20;
	internal const uint MaxHeightOffset = 24;
	internal const uint DefaultWidthOffset = 28;
	internal const uint DefaultHeightOffset = 32;
	internal const uint RenderInfoOffset = 36;

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
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
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

// Struct-first guest-memory adapter for the fixed BoopsiQuery envelope. The
// complete 40-byte record is validated before any named member is accessed.
internal static class MuiBoopsiQueryMessageMemoryCodec
{
	private static bool TryResolve(MuiBoopsiQueryPacketField field,
		out uint offset, out uint size)
	{
		size = MuiBoopsiQueryMessage.Size;
		switch (field)
		{
			case MuiBoopsiQueryPacketField.MethodId:
				offset = MuiBoopsiQueryMessage.MethodIdOffset;
				size = MuiBoopsiQueryMethodMessage.Size;
				return true;
			case MuiBoopsiQueryPacketField.Screen:
				offset = MuiBoopsiQueryMessage.ScreenOffset; return true;
			case MuiBoopsiQueryPacketField.Flags:
				offset = MuiBoopsiQueryMessage.FlagsOffset; return true;
			case MuiBoopsiQueryPacketField.MinWidth:
				offset = MuiBoopsiQueryMessage.MinWidthOffset; return true;
			case MuiBoopsiQueryPacketField.MinHeight:
				offset = MuiBoopsiQueryMessage.MinHeightOffset; return true;
			case MuiBoopsiQueryPacketField.MaxWidth:
				offset = MuiBoopsiQueryMessage.MaxWidthOffset; return true;
			case MuiBoopsiQueryPacketField.MaxHeight:
				offset = MuiBoopsiQueryMessage.MaxHeightOffset; return true;
			case MuiBoopsiQueryPacketField.DefaultWidth:
				offset = MuiBoopsiQueryMessage.DefaultWidthOffset; return true;
			case MuiBoopsiQueryPacketField.DefaultHeight:
				offset = MuiBoopsiQueryMessage.DefaultHeightOffset; return true;
			case MuiBoopsiQueryPacketField.RenderInfo:
				offset = MuiBoopsiQueryMessage.RenderInfoOffset; return true;
		}
		offset = 0;
		size = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiBoopsiQueryPacketField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset, out var size) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, size)) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiBoopsiQueryMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiBoopsiQueryPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiBoopsiQueryPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// field cursor. The live message codec routes to the struct adapter above.
internal static class MuiBoopsiQueryPacketFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBoopsiQueryPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiBoopsiQueryMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiBoopsiQueryPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiBoopsiQueryMessageMemoryCodec.TryReadUInt32(ref platform,
			message, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiBoopsiQueryPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiBoopsiQueryMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, field, value);
}

// The packed guest record is decoded and encoded in one place.  Consumers use
// the named fields above; only this ABI codec carries the fixed byte offsets
// required by the 68k wire layout.
internal static class MuiBoopsiQueryMessageCodec
{
	// The method-only envelope is a complete one-ULONG named record. Keep
	// scalar-safe entry points beside the typed record so freestanding callers
	// avoid passing a one-field struct through the native ABI.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestUlongStorageCodec.WriteValue(ref platform, message, methodId);

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiBoopsiQueryMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	// Keep selector admission scalar for callers that only need the method ID,
	// but obtain it through the named method-header record in declaration order.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, message,
			out methodId);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiBoopsiQueryMessage record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		uint methodId;
		if (message.IsNull || !platform.IsMapped(message,
			MuiBoopsiQueryMessage.Size) ||
			!TryReadMethodIdValue(ref platform, message, out methodId) ||
			methodId != MuiBoopsiQueryMessage.Method)
			return false;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiBoopsiQueryMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawScreen) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var flags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMinWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMinHeight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMaxWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMaxHeight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawDefaultWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawDefaultHeight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawRenderInfo) ||
			!MuiGuestStructCursor.IsComplete(cursor) ||
			rawMethodId != MuiBoopsiQueryMessage.Method) return false;
		record.MethodId = methodId;
		record.Screen = APTR.FromPointer(rawScreen);
		record.Flags = flags;
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
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiBoopsiQueryMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				MuiBoopsiQueryMessage.Method) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Screen.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Flags) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)record.MinWidth)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)record.MinHeight)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)record.MaxWidth)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)record.MaxHeight)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)record.DefaultWidth)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)record.DefaultHeight)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.RenderInfo.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
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

	// Forward a validated MorphOS query packet to the wrapped BOOPSI object.
	// The packet remains caller-owned guest memory; the existing typed
	// IMuiBoopsiCapability owns the class dispatcher call and returns its D0
	// result. No managed callback or shadow packet is introduced.
	public static uint DispatchToObject<TPlatform>(ref TPlatform platform,
		APTR obj, APTR message)
		where TPlatform : struct, IMuiGuestMemory, IMuiBoopsiCapability
	{
		if (obj.IsNull || !TryRead(ref platform, message, out _)) return 0;
		return platform.DoMethod(obj, message);
	}
}
