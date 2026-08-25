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
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLayoutAskMinMaxMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLayoutRelayoutMessage
{
	public const uint Size = 8;
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

internal static class MuiLayoutFieldCursorCodec
{
	private static bool TryResolve(MuiLayoutPacketKind packet,
		MuiLayoutField field, out uint offset)
	{
		// Keep this as a straight-line selector instead of a switch/jump table.
		// The native compiler can then lower every packet kind (including the
		// later TextDimensions/Layout variants) without importing a managed
		// dispatch helper.
		if (packet == MuiLayoutPacketKind.Method)
		{
			if (field == MuiLayoutField.MethodId) { offset = 0; return true; }
		}
		else if (packet == MuiLayoutPacketKind.AskMinMax)
		{
			if (field == MuiLayoutField.MethodId) { offset = 0; return true; }
			if (field == MuiLayoutField.Storage) { offset = 4; return true; }
		}
		else if (packet == MuiLayoutPacketKind.Relayout)
		{
			if (field == MuiLayoutField.MethodId) { offset = 0; return true; }
			if (field == MuiLayoutField.Flags) { offset = 4; return true; }
		}
		else if (packet == MuiLayoutPacketKind.Rectangle)
		{
			if (field == MuiLayoutField.MethodId) { offset = 0; return true; }
			if (field == MuiLayoutField.Left) { offset = 4; return true; }
			if (field == MuiLayoutField.Top) { offset = 8; return true; }
			if (field == MuiLayoutField.RightOrWidth) { offset = 12; return true; }
			if (field == MuiLayoutField.BottomOrHeight) { offset = 16; return true; }
			if (field == MuiLayoutField.Reserved0) { offset = 20; return true; }
			if (field == MuiLayoutField.Reserved1) { offset = 24; return true; }
			if (field == MuiLayoutField.Reserved2) { offset = 28; return true; }
		}
		else if (packet == MuiLayoutPacketKind.Text)
		{
			if (field == MuiLayoutField.MethodId) { offset = 0; return true; }
			if (field == MuiLayoutField.Left) { offset = 4; return true; }
			if (field == MuiLayoutField.Top) { offset = 8; return true; }
			if (field == MuiLayoutField.Width) { offset = 12; return true; }
			if (field == MuiLayoutField.Height) { offset = 16; return true; }
			if (field == MuiLayoutField.Text) { offset = 20; return true; }
			if (field == MuiLayoutField.Length) { offset = 24; return true; }
			if (field == MuiLayoutField.PreParse) { offset = 28; return true; }
			if (field == MuiLayoutField.TextFlags) { offset = 32; return true; }
		}
		else if (packet == MuiLayoutPacketKind.RenderInfo)
		{
			if (field == MuiLayoutField.MethodId) { offset = 0; return true; }
			if (field == MuiLayoutField.RenderInfo) { offset = 4; return true; }
		}
		else if (packet == MuiLayoutPacketKind.Flags)
		{
			if (field == MuiLayoutField.MethodId) { offset = 0; return true; }
			if (field == MuiLayoutField.Flags) { offset = 4; return true; }
		}
		else if (packet == MuiLayoutPacketKind.TextDimensions)
		{
			if (field == MuiLayoutField.MethodId) { offset = 0; return true; }
			if (field == MuiLayoutField.Text) { offset = 4; return true; }
			if (field == MuiLayoutField.Length) { offset = 8; return true; }
			if (field == MuiLayoutField.PreParse) { offset = 12; return true; }
			if (field == MuiLayoutField.TextFlags) { offset = 16; return true; }
		}
		else if (packet == MuiLayoutPacketKind.Layout)
		{
			if (field == MuiLayoutField.MethodId) { offset = 0; return true; }
			if (field == MuiLayoutField.Left) { offset = 4; return true; }
			if (field == MuiLayoutField.Top) { offset = 8; return true; }
			if (field == MuiLayoutField.Width) { offset = 12; return true; }
			if (field == MuiLayoutField.Height) { offset = 16; return true; }
			if (field == MuiLayoutField.Flags) { offset = 20; return true; }
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiLayoutFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Packet, cursor.Field, out var offset) ||
			cursor.Message.IsNull || cursor.Message.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiLayoutPacketKind packet, MuiLayoutField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiLayoutFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
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
		if (message.IsNull || !platform.IsMapped(message,
			MuiLayoutMethodMessage.Size)) return false;
		return MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.Method, MuiLayoutField.MethodId,
			out methodId);
	}

	internal static bool TryReadAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiLayoutAskMinMaxMessage.Size,
			AskMinMax)) return false;
		return MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.AskMinMax, MuiLayoutField.MethodId,
			out packet.MethodId) &&
			MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
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
		return MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.Relayout, MuiLayoutField.MethodId,
			out packet.MethodId) &&
			MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
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
		return MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.Rectangle, MuiLayoutField.MethodId,
			out packet.MethodId) &&
			MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiLayoutPacketKind.Rectangle, MuiLayoutField.Left,
				out packet.Left) &&
				MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiLayoutPacketKind.Rectangle, MuiLayoutField.Top,
					out packet.Top) &&
					MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
						MuiLayoutPacketKind.Rectangle, MuiLayoutField.RightOrWidth,
						out packet.RightOrWidth) &&
						MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
							MuiLayoutPacketKind.Rectangle, MuiLayoutField.BottomOrHeight,
							out packet.BottomOrHeight) &&
							MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
								MuiLayoutPacketKind.Rectangle, MuiLayoutField.Reserved0,
								out packet.Reserved0) &&
								MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
									MuiLayoutPacketKind.Rectangle, MuiLayoutField.Reserved1,
									out packet.Reserved1) &&
									MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
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
		return MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.Text, MuiLayoutField.MethodId,
			out packet.MethodId) &&
			MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiLayoutPacketKind.Text, MuiLayoutField.Left, out packet.Left) &&
				MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiLayoutPacketKind.Text, MuiLayoutField.Top, out packet.Top) &&
					MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
						MuiLayoutPacketKind.Text, MuiLayoutField.Width,
						out packet.Width) &&
						MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
							MuiLayoutPacketKind.Text, MuiLayoutField.Height,
							out packet.Height) &&
							MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
								MuiLayoutPacketKind.Text, MuiLayoutField.Text,
								out packet.Text) &&
								MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
									MuiLayoutPacketKind.Text, MuiLayoutField.Length,
									out packet.Length) &&
									MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
						MuiLayoutPacketKind.Text, MuiLayoutField.PreParse,
						out packet.PreParse) &&
					MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
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
		return MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.RenderInfo, MuiLayoutField.MethodId,
			out packet.MethodId) &&
			MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
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
		return MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.Flags, MuiLayoutField.MethodId,
			out packet.MethodId) &&
			MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
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
	return MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
		MuiLayoutPacketKind.TextDimensions, MuiLayoutField.MethodId,
		out packet.MethodId) &&
		MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.TextDimensions, MuiLayoutField.Text,
			out packet.Text) &&
		MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.TextDimensions, MuiLayoutField.Length,
			out packet.Length) &&
		MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiLayoutPacketKind.TextDimensions, MuiLayoutField.PreParse,
			out packet.PreParse) &&
		MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform, message,
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
		if (!MuiLayoutFieldCursorCodec.TryReadAt(ref platform, message, 0,
			out methodId) || !MuiLayoutFieldCursorCodec.TryReadAt(ref platform,
			message, 4, out left) || !MuiLayoutFieldCursorCodec.TryReadAt(
			ref platform, message, 8, out top) ||
			!MuiLayoutFieldCursorCodec.TryReadAt(ref platform, message, 12,
				out width) || !MuiLayoutFieldCursorCodec.TryReadAt(ref platform,
				message, 16, out height) || !MuiLayoutFieldCursorCodec.TryReadAt(
				ref platform, message, 20, out flags)) return false;
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
