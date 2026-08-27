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

// Struct-first guest-memory adapter for the fixed Boopsi/Dtpic wrapper
// packets. Packet kinds own complete MorphOS record spans; field names select
// members without exposing numeric positions to dispatch code.
internal static class MuiExternalWrapperMessageMemoryCodec
{
	private static bool TryResolve(MuiExternalWrapperPacketKind packet,
		MuiExternalWrapperField field, out uint offset, out uint size)
	{
		switch (packet)
		{
			case MuiExternalWrapperPacketKind.Update:
				size = MuiExternalUpdateMessage.Size;
				if (field == MuiExternalWrapperField.MethodId)
					offset = MuiExternalUpdateMessage.MethodIdOffset;
				else if (field == MuiExternalWrapperField.AttributeList)
					offset = MuiExternalUpdateMessage.AttributeListOffset;
				else if (field == MuiExternalWrapperField.GadgetInfo)
					offset = MuiExternalUpdateMessage.GadgetInfoOffset;
				else if (field == MuiExternalWrapperField.Flags)
					offset = MuiExternalUpdateMessage.FlagsOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiExternalWrapperPacketKind.Get:
				size = MuiExternalGetMessage.Size;
				if (field == MuiExternalWrapperField.MethodId)
					offset = MuiExternalGetMessage.MethodIdOffset;
				else if (field == MuiExternalWrapperField.Attribute)
					offset = MuiExternalGetMessage.AttributeOffset;
				else if (field == MuiExternalWrapperField.Storage)
					offset = MuiExternalGetMessage.StorageOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiExternalWrapperPacketKind.Set:
				size = MuiExternalSetMessage.Size;
				if (field == MuiExternalWrapperField.MethodId)
					offset = MuiExternalSetMessage.MethodIdOffset;
				else if (field == MuiExternalWrapperField.Attribute)
					offset = MuiExternalSetMessage.AttributeOffset;
				else if (field == MuiExternalWrapperField.Value)
					offset = MuiExternalSetMessage.ValueOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiExternalWrapperPacketKind.Method:
				size = MuiExternalMethodMessage.Size;
				if (field == MuiExternalWrapperField.MethodId)
					offset = MuiExternalMethodMessage.MethodIdOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiExternalWrapperPacketKind.RenderInfo:
				size = MuiExternalRenderInfoMessage.Size;
				if (field == MuiExternalWrapperField.MethodId)
					offset = MuiExternalRenderInfoMessage.MethodIdOffset;
				else if (field == MuiExternalWrapperField.RenderInfo)
					offset = MuiExternalRenderInfoMessage.RenderInfoOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiExternalWrapperPacketKind.AskMinMax:
				size = MuiExternalAskMinMaxMessage.Size;
				if (field == MuiExternalWrapperField.MethodId)
					offset = MuiExternalAskMinMaxMessage.MethodIdOffset;
				else if (field == MuiExternalWrapperField.Storage)
					offset = MuiExternalAskMinMaxMessage.StorageOffset;
				else { offset = 0; size = 0; return false; }
				return true;
			case MuiExternalWrapperPacketKind.Layout:
				size = MuiExternalLayoutMessage.Size;
				if (field == MuiExternalWrapperField.MethodId)
					offset = MuiExternalLayoutMessage.MethodIdOffset;
				else if (field == MuiExternalWrapperField.Left)
					offset = MuiExternalLayoutMessage.LeftOffset;
				else if (field == MuiExternalWrapperField.Top)
					offset = MuiExternalLayoutMessage.TopOffset;
				else if (field == MuiExternalWrapperField.Width)
					offset = MuiExternalLayoutMessage.WidthOffset;
				else if (field == MuiExternalWrapperField.Height)
					offset = MuiExternalLayoutMessage.HeightOffset;
				else { offset = 0; size = 0; return false; }
				return true;
		}
		offset = 0;
		size = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiExternalWrapperPacketKind packet,
		MuiExternalWrapperField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset, out var size) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, size)) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiExternalMethodMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiExternalWrapperPacketKind packet,
		MuiExternalWrapperField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiExternalWrapperPacketKind packet,
		MuiExternalWrapperField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// field cursor. The live message codecs route to the struct adapter above.
internal static class MuiExternalWrapperFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiExternalWrapperFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiExternalWrapperMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Packet, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiExternalWrapperPacketKind packet,
		MuiExternalWrapperField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiExternalWrapperMessageMemoryCodec.TryReadUInt32(ref platform,
			message, packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiExternalWrapperPacketKind packet,
		MuiExternalWrapperField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiExternalWrapperMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, packet, field, value);
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
		uint methodId;
		if (!IsMethod(method) || !TryReadMethodIdValue(ref platform, message,
			out methodId) || methodId != method) return false;
		packet.MethodId = methodId;
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
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId)) return false;
		packet.MethodId = methodId;
		return true;
	}

	// Native qualification keeps method-header admission scalar while the
	// dispatcher-facing overload above retains the named value-type record.
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (message.IsNull || !platform.IsMapped(message,
			MuiExternalMethodMessage.Size)) return false;
		return MuiExternalWrapperFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiExternalWrapperPacketKind.Method,
			MuiExternalWrapperField.MethodId, out methodId);
	}

	internal static bool IsValidMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMethod(method)) return false;
		uint methodId;
		return TryReadMethodIdValue(ref platform, message, out methodId) &&
			methodId == method;
	}

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
		TryReadMethodIdValue(ref platform, message, out uint methodId) &&
		methodId == method && platform.IsMapped(message, size);
}
