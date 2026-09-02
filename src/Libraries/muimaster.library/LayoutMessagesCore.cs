/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLayoutMethodMessage
{
	public const uint Size = 4;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLayoutAskMinMaxMessage
{
	public const uint Size = 8;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint StorageOffset = 4;
	public uint MethodId;
	public uint Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLayoutRelayoutMessage
{
	public const uint Size = 8;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint FlagsOffset = 4;
	public uint MethodId;
	public uint Flags;
}

// DrawBackground and Backfill use the same fixed rectangle-shaped packet.
// The trailing words are retained explicitly because the MorphOS ABI reserves
// them even though the current CopperOS drawing core consumes only the first
// four coordinates.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLayoutRectangleMessage
{
	public const uint Size = 32;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint LeftOffset = 4;
	public const uint TopOffset = 8;
	public const uint RightOrWidthOffset = 12;
	public const uint BottomOrHeightOffset = 16;
	public const uint Reserved0Offset = 20;
	public const uint Reserved1Offset = 24;
	public const uint Reserved2Offset = 28;
	public uint MethodId;
	public uint Left;
	public uint Top;
	public uint RightOrWidth;
	public uint BottomOrHeight;
	public uint Reserved0;
	public uint Reserved1;
	public uint Reserved2;

	// The packet remains ABI-compatible with MorphOS while exposing the
	// operation-specific trailing words as named values to the core.
	public int XOffset
	{
		get => unchecked((int)Reserved0);
		set => Reserved0 = unchecked((uint)value);
	}

	public int YOffset
	{
		get => unchecked((int)Reserved1);
		set => Reserved1 = unchecked((uint)value);
	}

	public uint Auxiliary
	{
		get => Reserved2;
		set => Reserved2 = value;
	}

	public uint DrawBackgroundFlags
	{
		get => Auxiliary;
		set => Auxiliary = value;
	}

	public int BackfillBrightness
	{
		get => unchecked((int)Auxiliary);
		set => Auxiliary = unchecked((uint)value);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLayoutTextMessage
{
	public const uint Size = 36;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint LeftOffset = 4;
	public const uint TopOffset = 8;
	public const uint WidthOffset = 12;
	public const uint HeightOffset = 16;
	public const uint TextOffset = 20;
	public const uint LengthOffset = 24;
	public const uint PreParseOffset = 28;
	public const uint TextFlagsOffset = 32;
	public uint MethodId;
	public uint Left;
	public uint Top;
	public uint Width;
	public uint Height;
	public uint Text;
	public uint Length;
	public uint PreParse;
	public uint Flags;
}

internal enum MuiLayoutPacketKind : byte
{
	Method,
	AskMinMax,
	Relayout,
	Rectangle,
	Text,
	RenderInfo,
	Flags,
	TextDimensions,
	Layout,
}

internal enum MuiLayoutField : byte
{
	MethodId,
	Storage,
	Flags,
	Left,
	Top,
	RightOrWidth,
	BottomOrHeight,
	Reserved0,
	Reserved1,
	Reserved2,
	Width,
	Height,
	Text,
	Length,
	RenderInfo,
	PreParse,
	TextFlags,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLayoutFieldCursor
{
	internal APTR Message;
	internal MuiLayoutPacketKind Packet;
	internal MuiLayoutField Field;
}

internal static class MuiLayoutMessageMemoryCodec
{
	private static bool TryGetPacketSize(MuiLayoutPacketKind packet,
		out uint size)
	{
		if (packet == MuiLayoutPacketKind.Method)
			size = MuiLayoutMethodMessage.Size;
		else if (packet == MuiLayoutPacketKind.AskMinMax)
			size = MuiLayoutAskMinMaxMessage.Size;
		else if (packet == MuiLayoutPacketKind.Relayout)
			size = MuiLayoutRelayoutMessage.Size;
		else if (packet == MuiLayoutPacketKind.Rectangle)
			size = MuiLayoutRectangleMessage.Size;
		else if (packet == MuiLayoutPacketKind.Text)
			size = MuiLayoutTextMessage.Size;
		else if (packet == MuiLayoutPacketKind.RenderInfo)
			size = MuiLayoutRenderInfoMessage.Size;
		else if (packet == MuiLayoutPacketKind.Flags)
			size = MuiLayoutFlagsMessage.Size;
		else if (packet == MuiLayoutPacketKind.TextDimensions)
			size = MuiLayoutTextDimensionsMessage.Size;
		else if (packet == MuiLayoutPacketKind.Layout)
			size = MuiLayoutMessage.Size;
		else
		{
			size = 0;
			return false;
		}
		return true;
	}

	private static bool TryResolve(MuiLayoutPacketKind packet,
		MuiLayoutField field, out uint offset)
	{
		// Keep this as a straight-line selector instead of a switch/jump table.
		// The native compiler can then lower every packet kind (including the
		// later TextDimensions/Layout variants) without importing a managed
		// dispatch helper.
		if (packet == MuiLayoutPacketKind.Method)
		{
			if (field == MuiLayoutField.MethodId) { offset = MuiLayoutMethodMessage.MethodIdOffset; return true; }
		}
		else if (packet == MuiLayoutPacketKind.AskMinMax)
		{
			if (field == MuiLayoutField.MethodId) { offset = MuiLayoutAskMinMaxMessage.MethodIdOffset; return true; }
			if (field == MuiLayoutField.Storage) { offset = MuiLayoutAskMinMaxMessage.StorageOffset; return true; }
		}
		else if (packet == MuiLayoutPacketKind.Relayout)
		{
			if (field == MuiLayoutField.MethodId) { offset = MuiLayoutRelayoutMessage.MethodIdOffset; return true; }
			if (field == MuiLayoutField.Flags) { offset = MuiLayoutRelayoutMessage.FlagsOffset; return true; }
		}
		else if (packet == MuiLayoutPacketKind.Rectangle)
		{
			if (field == MuiLayoutField.MethodId) { offset = MuiLayoutRectangleMessage.MethodIdOffset; return true; }
			if (field == MuiLayoutField.Left) { offset = MuiLayoutRectangleMessage.LeftOffset; return true; }
			if (field == MuiLayoutField.Top) { offset = MuiLayoutRectangleMessage.TopOffset; return true; }
			if (field == MuiLayoutField.RightOrWidth) { offset = MuiLayoutRectangleMessage.RightOrWidthOffset; return true; }
			if (field == MuiLayoutField.BottomOrHeight) { offset = MuiLayoutRectangleMessage.BottomOrHeightOffset; return true; }
			if (field == MuiLayoutField.Reserved0) { offset = MuiLayoutRectangleMessage.Reserved0Offset; return true; }
			if (field == MuiLayoutField.Reserved1) { offset = MuiLayoutRectangleMessage.Reserved1Offset; return true; }
			if (field == MuiLayoutField.Reserved2) { offset = MuiLayoutRectangleMessage.Reserved2Offset; return true; }
		}
		else if (packet == MuiLayoutPacketKind.Text)
		{
			if (field == MuiLayoutField.MethodId) { offset = MuiLayoutTextMessage.MethodIdOffset; return true; }
			if (field == MuiLayoutField.Left) { offset = MuiLayoutTextMessage.LeftOffset; return true; }
			if (field == MuiLayoutField.Top) { offset = MuiLayoutTextMessage.TopOffset; return true; }
			if (field == MuiLayoutField.Width) { offset = MuiLayoutTextMessage.WidthOffset; return true; }
			if (field == MuiLayoutField.Height) { offset = MuiLayoutTextMessage.HeightOffset; return true; }
			if (field == MuiLayoutField.Text) { offset = MuiLayoutTextMessage.TextOffset; return true; }
			if (field == MuiLayoutField.Length) { offset = MuiLayoutTextMessage.LengthOffset; return true; }
			if (field == MuiLayoutField.PreParse) { offset = MuiLayoutTextMessage.PreParseOffset; return true; }
			if (field == MuiLayoutField.TextFlags) { offset = MuiLayoutTextMessage.TextFlagsOffset; return true; }
		}
		else if (packet == MuiLayoutPacketKind.RenderInfo)
		{
			if (field == MuiLayoutField.MethodId) { offset = MuiLayoutRenderInfoMessage.MethodIdOffset; return true; }
			if (field == MuiLayoutField.RenderInfo) { offset = MuiLayoutRenderInfoMessage.RenderInfoOffset; return true; }
		}
		else if (packet == MuiLayoutPacketKind.Flags)
		{
			if (field == MuiLayoutField.MethodId) { offset = MuiLayoutFlagsMessage.MethodIdOffset; return true; }
			if (field == MuiLayoutField.Flags) { offset = MuiLayoutFlagsMessage.FlagsOffset; return true; }
		}
		else if (packet == MuiLayoutPacketKind.TextDimensions)
		{
			if (field == MuiLayoutField.MethodId) { offset = MuiLayoutTextDimensionsMessage.MethodIdOffset; return true; }
			if (field == MuiLayoutField.Text) { offset = MuiLayoutTextDimensionsMessage.TextOffset; return true; }
			if (field == MuiLayoutField.Length) { offset = MuiLayoutTextDimensionsMessage.LengthOffset; return true; }
			if (field == MuiLayoutField.PreParse) { offset = MuiLayoutTextDimensionsMessage.PreParseOffset; return true; }
			if (field == MuiLayoutField.TextFlags) { offset = MuiLayoutTextDimensionsMessage.TextFlagsOffset; return true; }
		}
		else if (packet == MuiLayoutPacketKind.Layout)
		{
			if (field == MuiLayoutField.MethodId) { offset = MuiLayoutMessage.MethodIdOffset; return true; }
			if (field == MuiLayoutField.Left) { offset = MuiLayoutMessage.LeftOffset; return true; }
			if (field == MuiLayoutField.Top) { offset = MuiLayoutMessage.TopOffset; return true; }
			if (field == MuiLayoutField.Width) { offset = MuiLayoutMessage.WidthOffset; return true; }
			if (field == MuiLayoutField.Height) { offset = MuiLayoutMessage.HeightOffset; return true; }
			if (field == MuiLayoutField.Flags) { offset = MuiLayoutMessage.FlagsOffset; return true; }
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiLayoutFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor.Message, cursor.Packet,
			cursor.Field, out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiLayoutPacketKind packet, MuiLayoutField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset) ||
			!TryGetPacketSize(packet, out var packetSize) || message.IsNull ||
			message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, packetSize)) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiLayoutPacketKind packet, MuiLayoutField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	// Method admission stays on the named packed header; the field adapter
	// retains only compatibility access for explicitly selected payload fields.
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiLayoutMethodHeaderCodec.TryReadValue(ref platform, message,
			out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiLayoutPacketKind packet, MuiLayoutField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool TryReadAt<TPlatform>(ref TPlatform platform,
		APTR message, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (message.IsNull || message.Raw > uint.MaxValue - offset)
			return false;
		var address = APTR.FromPointer(message.Raw + offset);
		if (!platform.IsMapped(address, 4)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// field cursor. Live layout packet codecs use MuiLayoutMessageMemoryCodec.
internal static class MuiLayoutFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiLayoutFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiLayoutMessageMemoryCodec.TryGetAddress(ref platform, cursor,
			out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiLayoutPacketKind packet, MuiLayoutField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiLayoutMessageMemoryCodec.TryGetAddress(ref platform, message, packet,
			field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiLayoutPacketKind packet, MuiLayoutField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message, packet,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiLayoutPacketKind packet, MuiLayoutField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiLayoutMessageMemoryCodec.TryWriteUInt32(ref platform, message, packet,
			field, value);

	internal static bool TryReadAt<TPlatform>(ref TPlatform platform,
		APTR message, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiLayoutMessageMemoryCodec.TryReadAt(ref platform, message, offset,
			out value);
}

// Struct-first codec for the method-only Layout header. The named one-ULONG
// record remains the ABI contract; shared guest storage keeps selector
// admission free of direct scalar lowering in freestanding 68k code.
internal static class MuiLayoutMethodHeaderCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiLayoutMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.TryReadValue(ref platform, valueAddress,
				out methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteValue<TPlatform>(ref TPlatform platform,
		APTR address, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiLayoutMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Complete sequential codecs for the fixed MorphOS layout packet records.
// Wire positions are defined by each packed declaration; the field adapter
// above remains only as a compatibility/diagnostic surface.
internal static class MuiLayoutMessageStructCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiLayoutMethodHeaderCodec.TryReadValue(ref platform, message,
			out methodId);

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiLayoutMethodHeaderCodec.WriteValue(ref platform, message,
			methodId);

	internal static bool TryReadAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutAskMinMaxMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiLayoutAskMinMaxMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var storage)) return false;
		value.MethodId = methodId;
		value.Storage = storage;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, MuiLayoutAskMinMaxMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiLayoutAskMinMaxMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Storage)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadRelayout<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutRelayoutMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiLayoutRelayoutMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var flags)) return false;
		value.MethodId = methodId;
		value.Flags = flags;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRelayout<TPlatform>(ref TPlatform platform,
		APTR message, MuiLayoutRelayoutMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiLayoutRelayoutMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadRectangle<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutRectangleMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiLayoutRectangleMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var left) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var top) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rightOrWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var bottomOrHeight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var reserved0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var reserved1) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var reserved2)) return false;
		value.MethodId = methodId;
		value.Left = left;
		value.Top = top;
		value.RightOrWidth = rightOrWidth;
		value.BottomOrHeight = bottomOrHeight;
		value.Reserved0 = reserved0;
		value.Reserved1 = reserved1;
		value.Reserved2 = reserved2;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRectangle<TPlatform>(ref TPlatform platform,
		APTR message, MuiLayoutRectangleMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiLayoutRectangleMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Left) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Top) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.RightOrWidth) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.BottomOrHeight) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Reserved0) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Reserved1) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Reserved2)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadText<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutTextMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiLayoutTextMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var left) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var top) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var width) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var height) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var text) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var length) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var preParse) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var flags)) return false;
		value.MethodId = methodId;
		value.Left = left;
		value.Top = top;
		value.Width = width;
		value.Height = height;
		value.Text = text;
		value.Length = length;
		value.PreParse = preParse;
		value.Flags = flags;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteText<TPlatform>(ref TPlatform platform,
		APTR message, MuiLayoutTextMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiLayoutTextMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Left) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Top) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Width) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Height) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Text) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Length) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.PreParse) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadRenderInfo<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutRenderInfoMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiLayoutRenderInfoMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var renderInfo)) return false;
		value.MethodId = methodId;
		value.RenderInfo = renderInfo;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRenderInfo<TPlatform>(ref TPlatform platform,
		APTR message, MuiLayoutRenderInfoMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiLayoutRenderInfoMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.RenderInfo)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadFlags<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutFlagsMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiLayoutFlagsMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var flags)) return false;
		value.MethodId = methodId;
		value.Flags = flags;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteFlags<TPlatform>(ref TPlatform platform,
		APTR message, MuiLayoutFlagsMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiLayoutFlagsMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadTextDimensions<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutTextDimensionsMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiLayoutTextDimensionsMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var text) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var length) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var preParse) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var flags)) return false;
		value.MethodId = methodId;
		value.Text = text;
		value.Length = length;
		value.PreParse = preParse;
		value.Flags = flags;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteTextDimensions<TPlatform>(ref TPlatform platform,
		APTR message, MuiLayoutTextDimensionsMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiLayoutTextDimensionsMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Text) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Length) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.PreParse) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadLayout<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiLayoutMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var left) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var top) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var width) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var height) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var flags)) return false;
		value.MethodId = methodId;
		value.Left = left;
		value.Top = top;
		value.Width = width;
		value.Height = height;
		value.Flags = flags;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteLayout<TPlatform>(ref TPlatform platform,
		APTR message, MuiLayoutMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiLayoutMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Left) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Top) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Width) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Height) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Central codec for the fixed MorphOS layout packets. Consumers use the
// named records above; explicit guest offsets are confined to this adapter.
internal static class MuiLayoutPacketCodec
{
	internal const uint AskMinMax = 0x80423874u;
	internal const uint Layout = 0x8042845Bu;
	internal const uint Relayout = 0x8042B381u;
	internal const uint DrawBackground = 0x804238CAu;
	internal const uint Backfill = 0x80428D73u;
	internal const uint Text = 0x8042EE70u;
	internal const uint TextDim = 0x80422AD7u;

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutMethodMessage packet)
	where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId)) return false;
		packet.MethodId = methodId;
		return true;
	}

	// Keep the native lowering path scalar while the public/internal consumer
	// surface remains the named MuiLayoutMethodMessage record above.
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiLayoutMessageStructCodec.TryReadMethodIdValue(ref platform,
			message, out methodId);
	}

	internal static bool TryReadAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiLayoutAskMinMaxMessage.Size,
			AskMinMax)) return false;
		return MuiLayoutMessageStructCodec.TryReadAskMinMax(ref platform,
			message, out packet);
	}

	internal static bool TryReadRelayout<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutRelayoutMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiLayoutRelayoutMessage.Size,
			Relayout)) return false;
		return MuiLayoutMessageStructCodec.TryReadRelayout(ref platform, message,
			out packet);
	}

	internal static bool TryReadRectangle<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiLayoutRectangleMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if ((method != DrawBackground && method != Backfill) ||
			!IsPacket(ref platform, message, MuiLayoutRectangleMessage.Size,
			method)) return false;
		return MuiLayoutMessageStructCodec.TryReadRectangle(ref platform, message,
			out packet);
	}

	internal static bool TryReadText<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutTextMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiLayoutTextMessage.Size, Text))
			return false;
		return MuiLayoutMessageStructCodec.TryReadText(ref platform, message,
			out packet);
	}

	internal static bool TryReadRenderInfo<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiLayoutRenderInfoMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiLayoutRenderInfoMessage.Size,
			method)) return false;
		return MuiLayoutMessageStructCodec.TryReadRenderInfo(ref platform,
			message, out packet);
	}

	internal static bool TryReadFlags<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiLayoutFlagsMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiLayoutFlagsMessage.Size, method))
			return false;
		return MuiLayoutMessageStructCodec.TryReadFlags(ref platform, message,
			out packet);
	}

	internal static bool TryReadTextDimensions<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutTextDimensionsMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiLayoutTextDimensionsMessage.Size,
			TextDim)) return false;
		return MuiLayoutMessageStructCodec.TryReadTextDimensions(ref platform,
			message, out packet);
	}

	internal static bool TryReadLayout<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiLayoutMessage.Size, Layout))
			return false;
		if (!MuiLayoutMessageStructCodec.TryReadLayout(ref platform, message,
			out packet)) return false;
		return packet.MethodId == Layout;
	}

	private static bool IsPacket<TPlatform>(ref TPlatform platform,
		APTR message, uint size, uint method)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadMethodIdValue(ref platform, message, out uint methodId) &&
		methodId == method && platform.IsMapped(message, size);
}

public static class MuiLayoutPacketCore
{
	public const uint AskMinMax = MuiLayoutPacketCodec.AskMinMax;
	public const uint Layout = MuiLayoutPacketCodec.Layout;
	public const uint Relayout = MuiLayoutPacketCodec.Relayout;
	public const uint DrawBackground = MuiLayoutPacketCodec.DrawBackground;
	public const uint Backfill = MuiLayoutPacketCodec.Backfill;
	public const uint Text = MuiLayoutPacketCodec.Text;
	public const uint TextDimensions = MuiLayoutPacketCodec.TextDim;
	public const uint Draw = 0x80426F3Fu;
	public const uint Setup = 0x80428354u;
	public const uint Cleanup = 0x8042D985u;

	public static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiLayoutMessageStructCodec.WriteMethodIdValue(ref platform, message,
			method);

	public static bool WriteAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, APTR storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiLayoutAskMinMaxMessage);
		packet.MethodId = AskMinMax;
		packet.Storage = storage.Raw;
		return MuiLayoutMessageStructCodec.WriteAskMinMax(ref platform, message,
			packet);
	}

	public static bool WriteRelayout<TPlatform>(ref TPlatform platform,
		APTR message, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiLayoutRelayoutMessage);
		packet.MethodId = Relayout;
		packet.Flags = flags;
		return MuiLayoutMessageStructCodec.WriteRelayout(ref platform, message,
			packet);
	}

	public static bool WriteRectangle<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint left, uint top, uint rightOrWidth,
		uint bottomOrHeight, uint reserved0, uint reserved1, uint reserved2)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiLayoutRectangleMessage);
		packet.MethodId = method;
		packet.Left = left;
		packet.Top = top;
		packet.RightOrWidth = rightOrWidth;
		packet.BottomOrHeight = bottomOrHeight;
		packet.Reserved0 = reserved0;
		packet.Reserved1 = reserved1;
		packet.Reserved2 = reserved2;
		return (method == DrawBackground || method == Backfill) &&
			MuiLayoutMessageStructCodec.WriteRectangle(ref platform, message, packet);
	}

	public static bool WriteText<TPlatform>(ref TPlatform platform,
		APTR message, uint left, uint top, uint width, uint height, APTR text,
		uint length, uint preParse, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiLayoutTextMessage);
		packet.MethodId = Text;
		packet.Left = left;
		packet.Top = top;
		packet.Width = width;
		packet.Height = height;
		packet.Text = text.Raw;
		packet.Length = length;
		packet.PreParse = preParse;
		packet.Flags = flags;
		return MuiLayoutMessageStructCodec.WriteText(ref platform, message, packet);
	}

	public static bool WriteRenderInfo<TPlatform>(ref TPlatform platform,
		APTR message, uint method, APTR renderInfo)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiLayoutRenderInfoMessage);
		packet.MethodId = method;
		packet.RenderInfo = renderInfo.Raw;
		return MuiLayoutMessageStructCodec.WriteRenderInfo(ref platform, message,
			packet);
	}

	public static bool WriteFlags<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiLayoutFlagsMessage);
		packet.MethodId = method;
		packet.Flags = flags;
		return MuiLayoutMessageStructCodec.WriteFlags(ref platform, message,
			packet);
	}

	public static bool WriteTextDimensions<TPlatform>(ref TPlatform platform,
		APTR message, APTR text, uint length, uint preParse, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiLayoutTextDimensionsMessage);
		packet.MethodId = TextDimensions;
		packet.Text = text.Raw;
		packet.Length = length;
		packet.PreParse = preParse;
		packet.Flags = flags;
		return MuiLayoutMessageStructCodec.WriteTextDimensions(ref platform,
			message, packet);
	}

	public static bool WriteLayout<TPlatform>(ref TPlatform platform,
		APTR message, uint left, uint top, uint width, uint height, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiLayoutMessage);
		packet.MethodId = Layout;
		packet.Left = left;
		packet.Top = top;
		packet.Width = width;
		packet.Height = height;
		packet.Flags = flags;
		return MuiLayoutMessageStructCodec.WriteLayout(ref platform, message,
			packet);
	}

	internal static bool TryReadAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiLayoutPacketCodec.TryReadAskMinMax(ref platform, message, out packet);

	internal static bool TryReadRelayout<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutRelayoutMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiLayoutPacketCodec.TryReadRelayout(ref platform, message, out packet);

	internal static bool TryReadRectangle<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiLayoutRectangleMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiLayoutPacketCodec.TryReadRectangle(ref platform, message, method,
			out packet);

	internal static bool TryReadText<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutTextMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiLayoutPacketCodec.TryReadText(ref platform, message, out packet);
}
