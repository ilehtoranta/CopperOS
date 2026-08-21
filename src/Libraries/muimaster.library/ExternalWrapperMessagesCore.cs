/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

internal enum MuiExternalWrapperPacketKind : byte
{
	Update,
	Get,
	Set,
	Method,
	RenderInfo,
	AskMinMax,
	Layout,
}

internal enum MuiExternalWrapperField : byte
{
	MethodId,
	AttributeList,
	GadgetInfo,
	Flags,
	Attribute,
	Storage,
	Value,
	RenderInfo,
	Left,
	Top,
	Width,
	Height,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiExternalWrapperFieldCursor
{
	internal APTR Message;
	internal MuiExternalWrapperPacketKind Packet;
	internal MuiExternalWrapperField Field;
}

internal static class MuiExternalWrapperFieldCursorCodec
{
	private static bool TryResolve(MuiExternalWrapperPacketKind packet,
		MuiExternalWrapperField field, out uint offset)
	{
		switch (packet)
		{
			case MuiExternalWrapperPacketKind.Update:
				if (field == MuiExternalWrapperField.MethodId) { offset = 0; return true; }
				if (field == MuiExternalWrapperField.AttributeList) { offset = 4; return true; }
				if (field == MuiExternalWrapperField.GadgetInfo) { offset = 8; return true; }
				if (field == MuiExternalWrapperField.Flags) { offset = 12; return true; }
				break;
			case MuiExternalWrapperPacketKind.Get:
				if (field == MuiExternalWrapperField.MethodId) { offset = 0; return true; }
				if (field == MuiExternalWrapperField.Attribute) { offset = 4; return true; }
				if (field == MuiExternalWrapperField.Storage) { offset = 8; return true; }
				break;
			case MuiExternalWrapperPacketKind.Set:
				if (field == MuiExternalWrapperField.MethodId) { offset = 0; return true; }
				if (field == MuiExternalWrapperField.Attribute) { offset = 4; return true; }
				if (field == MuiExternalWrapperField.Value) { offset = 8; return true; }
				break;
			case MuiExternalWrapperPacketKind.Method:
				if (field == MuiExternalWrapperField.MethodId) { offset = 0; return true; }
				break;
			case MuiExternalWrapperPacketKind.RenderInfo:
				if (field == MuiExternalWrapperField.MethodId) { offset = 0; return true; }
				if (field == MuiExternalWrapperField.RenderInfo) { offset = 4; return true; }
				break;
			case MuiExternalWrapperPacketKind.AskMinMax:
				if (field == MuiExternalWrapperField.MethodId) { offset = 0; return true; }
				if (field == MuiExternalWrapperField.Storage) { offset = 4; return true; }
				break;
			case MuiExternalWrapperPacketKind.Layout:
				if (field == MuiExternalWrapperField.MethodId) { offset = 0; return true; }
				if (field == MuiExternalWrapperField.Left) { offset = 4; return true; }
				if (field == MuiExternalWrapperField.Top) { offset = 8; return true; }
				if (field == MuiExternalWrapperField.Width) { offset = 12; return true; }
				if (field == MuiExternalWrapperField.Height) { offset = 16; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalWrapperFieldCursor cursor, out APTR address)
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
		APTR message, MuiExternalWrapperPacketKind packet,
		MuiExternalWrapperField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiExternalWrapperFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiExternalWrapperPacketKind packet,
		MuiExternalWrapperField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiExternalWrapperFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Central codec for the fixed MorphOS Boopsi.mui/Dtpic.mui wrapper packets.
// Wrapper consumers use named records; packed guest offsets are confined to
// this adapter and never repeated in the external-resource dispatcher.
internal static class MuiExternalWrapperMessageCodec
{
	internal const uint OmGet = 0x00000104u;
	internal const uint OmUpdate = 0x00000108u;
	internal const uint MethodSet = 0x8042549Au;
	internal const uint MethodNoNotifySet = 0x8042216Fu;
	internal const uint AskMinMax = 0x80423874u;
	internal const uint Layout = 0x8042845Bu;
	internal const uint Setup = 0x80428354u;
	internal const uint Cleanup = 0x8042D985u;
	internal const uint Show = 0x8042CC84u;
	internal const uint Hide = 0x8042F20Fu;
	internal const uint Draw = 0x80426F3Fu;

	internal static bool TryReadUpdate<TPlatform>(ref TPlatform platform,
		APTR message, out MuiExternalUpdateMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiExternalUpdateMessage.Size,
			OmUpdate)) return false;
		return MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiExternalWrapperPacketKind.Update,
			MuiExternalWrapperField.MethodId, out packet.MethodId) &&
			MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiExternalWrapperPacketKind.Update,
				MuiExternalWrapperField.AttributeList, out packet.AttributeList) &&
				MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiExternalWrapperPacketKind.Update,
					MuiExternalWrapperField.GadgetInfo, out packet.GadgetInfo) &&
					MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform, message,
						MuiExternalWrapperPacketKind.Update,
						MuiExternalWrapperField.Flags, out packet.Flags);
	}

	internal static bool WriteUpdate<TPlatform>(ref TPlatform platform,
		APTR message, uint attributeList, uint gadgetInfo, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiExternalUpdateMessage.Size)) return false;
		return MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiExternalWrapperPacketKind.Update,
			MuiExternalWrapperField.MethodId, OmUpdate) &&
			MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiExternalWrapperPacketKind.Update,
				MuiExternalWrapperField.AttributeList, attributeList) &&
				MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform, message,
					MuiExternalWrapperPacketKind.Update,
					MuiExternalWrapperField.GadgetInfo, gadgetInfo) &&
					MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform, message,
						MuiExternalWrapperPacketKind.Update,
						MuiExternalWrapperField.Flags, flags);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiExternalGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiExternalGetMessage.Size,
			OmGet)) return false;
		return MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiExternalWrapperPacketKind.Get,
			MuiExternalWrapperField.MethodId, out packet.MethodId) &&
			MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiExternalWrapperPacketKind.Get, MuiExternalWrapperField.Attribute,
				out packet.Attribute) &&
				MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiExternalWrapperPacketKind.Get, MuiExternalWrapperField.Storage,
					out packet.Storage);
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform, APTR message,
		uint attribute, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiExternalGetMessage.Size)) return false;
		return MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiExternalWrapperPacketKind.Get,
			MuiExternalWrapperField.MethodId, OmGet) &&
			MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiExternalWrapperPacketKind.Get, MuiExternalWrapperField.Attribute,
				attribute) &&
				MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform, message,
					MuiExternalWrapperPacketKind.Get, MuiExternalWrapperField.Storage,
					storage);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiExternalSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsSetMethod(method) || !IsPacket(ref platform, message,
			MuiExternalSetMessage.Size, method)) return false;
		return MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiExternalWrapperPacketKind.Set,
			MuiExternalWrapperField.MethodId, out packet.MethodId) &&
			MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiExternalWrapperPacketKind.Set, MuiExternalWrapperField.Attribute,
				out packet.Attribute) &&
				MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiExternalWrapperPacketKind.Set, MuiExternalWrapperField.Value,
					out packet.Value);
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform, APTR message,
		uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSetMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiExternalSetMessage.Size)) return false;
		return MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiExternalWrapperPacketKind.Set,
			MuiExternalWrapperField.MethodId, method) &&
			MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiExternalWrapperPacketKind.Set, MuiExternalWrapperField.Attribute,
				attribute) &&
				MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform, message,
					MuiExternalWrapperPacketKind.Set, MuiExternalWrapperField.Value,
					value);
	}

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiExternalMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsMethod(method) || !TryReadMethodId(ref platform, message,
			out var header) || header.MethodId != method) return false;
		packet.MethodId = header.MethodId;
		return true;
	}

	// Read the fixed wrapper method header without constraining the selector.
	// The standalone dispatcher uses this named record before selecting the
	// specialized Boopsi/Dtpic packet codec.
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiExternalMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiExternalMethodMessage.Size)) return false;
		return MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiExternalWrapperPacketKind.Method,
			MuiExternalWrapperField.MethodId, out packet.MethodId);
	}

	internal static bool IsValidMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory =>
		IsMethod(method) && TryReadMethodId(ref platform, message,
			out var header) && header.MethodId == method;

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMethod(method) || message.IsNull || !platform.IsMapped(message,
			MuiExternalMethodMessage.Size)) return false;
		return MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiExternalWrapperPacketKind.Method,
			MuiExternalWrapperField.MethodId, method);
	}

	internal static bool TryReadRenderInfo<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiExternalRenderInfoMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsRenderMethod(method) || !IsPacket(ref platform, message,
			MuiExternalRenderInfoMessage.Size, method)) return false;
		return MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiExternalWrapperPacketKind.RenderInfo,
			MuiExternalWrapperField.MethodId, out packet.MethodId) &&
			MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiExternalWrapperPacketKind.RenderInfo,
				MuiExternalWrapperField.RenderInfo, out packet.RenderInfo);
	}

	internal static bool WriteRenderInfo<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint renderInfo)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsRenderMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiExternalRenderInfoMessage.Size)) return false;
		return MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiExternalWrapperPacketKind.RenderInfo,
			MuiExternalWrapperField.MethodId, method) &&
			MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiExternalWrapperPacketKind.RenderInfo,
				MuiExternalWrapperField.RenderInfo, renderInfo);
	}

	internal static bool TryReadAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, out MuiExternalAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiExternalAskMinMaxMessage.Size,
			AskMinMax)) return false;
		return MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiExternalWrapperPacketKind.AskMinMax,
			MuiExternalWrapperField.MethodId, out packet.MethodId) &&
			MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiExternalWrapperPacketKind.AskMinMax,
				MuiExternalWrapperField.Storage, out packet.Storage);
	}

	internal static bool WriteAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiExternalAskMinMaxMessage.Size)) return false;
		return MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiExternalWrapperPacketKind.AskMinMax,
			MuiExternalWrapperField.MethodId, AskMinMax) &&
			MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiExternalWrapperPacketKind.AskMinMax,
				MuiExternalWrapperField.Storage, storage);
	}

	internal static bool TryReadLayout<TPlatform>(ref TPlatform platform,
		APTR message, out MuiExternalLayoutMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiExternalLayoutMessage.Size,
			Layout)) return false;
		return MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiExternalWrapperPacketKind.Layout,
			MuiExternalWrapperField.MethodId, out packet.MethodId) &&
			MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiExternalWrapperPacketKind.Layout, MuiExternalWrapperField.Left,
				out packet.Left) &&
				MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiExternalWrapperPacketKind.Layout, MuiExternalWrapperField.Top,
					out packet.Top) &&
					MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform, message,
						MuiExternalWrapperPacketKind.Layout, MuiExternalWrapperField.Width,
						out packet.Width) &&
						MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform, message,
							MuiExternalWrapperPacketKind.Layout, MuiExternalWrapperField.Height,
							out packet.Height);
	}

	internal static bool WriteLayout<TPlatform>(ref TPlatform platform,
		APTR message, uint left, uint top, uint width, uint height)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiExternalLayoutMessage.Size)) return false;
		return MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiExternalWrapperPacketKind.Layout,
			MuiExternalWrapperField.MethodId, Layout) &&
			MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiExternalWrapperPacketKind.Layout, MuiExternalWrapperField.Left, left) &&
				MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform, message,
					MuiExternalWrapperPacketKind.Layout, MuiExternalWrapperField.Top, top) &&
					MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform, message,
						MuiExternalWrapperPacketKind.Layout, MuiExternalWrapperField.Width, width) &&
						MuiExternalWrapperFieldCursorCodec.TryWriteUInt32(ref platform, message,
							MuiExternalWrapperPacketKind.Layout, MuiExternalWrapperField.Height, height);
	}

	private static bool IsSetMethod(uint method) => method == MethodSet ||
		method == MethodNoNotifySet;

	private static bool IsMethod(uint method) => method == Cleanup ||
		method == Show || method == Hide || method == Draw;

	private static bool IsRenderMethod(uint method) => method == Setup;

	private static bool IsPacket<TPlatform>(ref TPlatform platform, APTR message,
		uint size, uint method) where TPlatform : struct, IMuiGuestMemory =>
		TryReadMethodId(ref platform, message, out var header) &&
		header.MethodId == method && platform.IsMapped(message, size);
}
