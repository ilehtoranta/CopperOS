/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
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

	// The smallest MorphOS method record is handled directly so native
	// lowering never has to materialize a packet-kind/field cursor for it.
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (message.IsNull || !platform.IsMapped(message,
			MuiLayoutMethodMessage.Size)) return false;
		value = platform.ReadUInt32(message,
			(int)MuiLayoutMethodMessage.MethodIdOffset);
		return true;
	}

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
		methodId = 0;
		return MuiLayoutMessageMemoryCodec.TryReadMethodId(ref platform, message,
			out methodId);
	}

	internal static bool TryReadAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiLayoutAskMinMaxMessage.Size,
			AskMinMax)) return false;
		return MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.AskMinMax, MuiLayoutField.MethodId,
			out packet.MethodId) &&
			MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiLayoutPacketKind.AskMinMax, MuiLayoutField.Storage,
				out packet.Storage);
	}

	internal static bool TryReadRelayout<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutRelayoutMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiLayoutRelayoutMessage.Size,
			Relayout)) return false;
		return MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.Relayout, MuiLayoutField.MethodId,
			out packet.MethodId) &&
			MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiLayoutPacketKind.Relayout, MuiLayoutField.Flags,
				out packet.Flags);
	}

	internal static bool TryReadRectangle<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiLayoutRectangleMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if ((method != DrawBackground && method != Backfill) ||
			!IsPacket(ref platform, message, MuiLayoutRectangleMessage.Size,
			method)) return false;
		return MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.Rectangle, MuiLayoutField.MethodId,
			out packet.MethodId) &&
			MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiLayoutPacketKind.Rectangle, MuiLayoutField.Left,
				out packet.Left) &&
				MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
					MuiLayoutPacketKind.Rectangle, MuiLayoutField.Top,
					out packet.Top) &&
					MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
						MuiLayoutPacketKind.Rectangle, MuiLayoutField.RightOrWidth,
						out packet.RightOrWidth) &&
						MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
							MuiLayoutPacketKind.Rectangle, MuiLayoutField.BottomOrHeight,
							out packet.BottomOrHeight) &&
							MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
								MuiLayoutPacketKind.Rectangle, MuiLayoutField.Reserved0,
								out packet.Reserved0) &&
								MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
									MuiLayoutPacketKind.Rectangle, MuiLayoutField.Reserved1,
									out packet.Reserved1) &&
									MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
										MuiLayoutPacketKind.Rectangle, MuiLayoutField.Reserved2,
										out packet.Reserved2);
	}

	internal static bool TryReadText<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutTextMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiLayoutTextMessage.Size, Text))
			return false;
		return MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.Text, MuiLayoutField.MethodId,
			out packet.MethodId) &&
			MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiLayoutPacketKind.Text, MuiLayoutField.Left, out packet.Left) &&
				MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
					MuiLayoutPacketKind.Text, MuiLayoutField.Top, out packet.Top) &&
					MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
						MuiLayoutPacketKind.Text, MuiLayoutField.Width,
						out packet.Width) &&
						MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
							MuiLayoutPacketKind.Text, MuiLayoutField.Height,
							out packet.Height) &&
							MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
								MuiLayoutPacketKind.Text, MuiLayoutField.Text,
								out packet.Text) &&
								MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
									MuiLayoutPacketKind.Text, MuiLayoutField.Length,
									out packet.Length) &&
									MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
						MuiLayoutPacketKind.Text, MuiLayoutField.PreParse,
						out packet.PreParse) &&
					MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
					MuiLayoutPacketKind.Text, MuiLayoutField.TextFlags,
					out packet.Flags);
	}

	internal static bool TryReadRenderInfo<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiLayoutRenderInfoMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiLayoutRenderInfoMessage.Size,
			method)) return false;
		return MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.RenderInfo, MuiLayoutField.MethodId,
			out packet.MethodId) &&
			MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiLayoutPacketKind.RenderInfo, MuiLayoutField.RenderInfo,
				out packet.RenderInfo);
	}

	internal static bool TryReadFlags<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiLayoutFlagsMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiLayoutFlagsMessage.Size, method))
			return false;
		return MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.Flags, MuiLayoutField.MethodId,
			out packet.MethodId) &&
			MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiLayoutPacketKind.Flags, MuiLayoutField.Flags,
				out packet.Flags);
	}

	internal static bool TryReadTextDimensions<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutTextDimensionsMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiLayoutTextDimensionsMessage.Size,
			TextDim)) return false;
	return MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
		MuiLayoutPacketKind.TextDimensions, MuiLayoutField.MethodId,
		out packet.MethodId) &&
		MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.TextDimensions, MuiLayoutField.Text,
			out packet.Text) &&
		MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.TextDimensions, MuiLayoutField.Length,
			out packet.Length) &&
		MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.TextDimensions, MuiLayoutField.PreParse,
			out packet.PreParse) &&
		MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.TextDimensions, MuiLayoutField.TextFlags,
			out packet.Flags);
	}

	internal static bool TryReadLayout<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiLayoutMessage.Size, Layout))
			return false;
		uint methodId;
		uint left;
		uint top;
		uint width;
		uint height;
		uint flags;
		if (!MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.Layout, MuiLayoutField.MethodId,
			out methodId) || !MuiLayoutMessageMemoryCodec.TryReadUInt32(
			ref platform, message, MuiLayoutPacketKind.Layout,
			MuiLayoutField.Left, out left) ||
			!MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiLayoutPacketKind.Layout, MuiLayoutField.Top, out top) ||
			!MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiLayoutPacketKind.Layout, MuiLayoutField.Width, out width) ||
			!MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiLayoutPacketKind.Layout, MuiLayoutField.Height, out height) ||
			!MuiLayoutMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiLayoutPacketKind.Layout, MuiLayoutField.Flags, out flags))
			return false;
		packet.MethodId = methodId;
		packet.Left = left;
		packet.Top = top;
		packet.Width = width;
		packet.Height = height;
		packet.Flags = flags;
		return true;
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
