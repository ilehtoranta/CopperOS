/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
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
// packets. Packet kinds own complete MorphOS record spans; typed field
// addresses walk declaration-ordered named records without exposing numeric
// positions to dispatch code.
internal static class MuiExternalWrapperMessageMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiExternalWrapperPacketKind packet,
		MuiExternalWrapperField field, out uint index, out uint size)
	{
		index = 0;
		switch (packet)
		{
			case MuiExternalWrapperPacketKind.Update:
				size = MuiExternalUpdateMessage.Size;
				if (field == MuiExternalWrapperField.MethodId)
					index = 0;
				else if (field == MuiExternalWrapperField.AttributeList)
					index = 1;
				else if (field == MuiExternalWrapperField.GadgetInfo)
					index = 2;
				else if (field == MuiExternalWrapperField.Flags)
					index = 3;
				else { size = 0; return false; }
				return true;
			case MuiExternalWrapperPacketKind.Get:
				size = MuiExternalGetMessage.Size;
				if (field == MuiExternalWrapperField.MethodId)
					index = 0;
				else if (field == MuiExternalWrapperField.Attribute)
					index = 1;
				else if (field == MuiExternalWrapperField.Storage)
					index = 2;
				else { size = 0; return false; }
				return true;
			case MuiExternalWrapperPacketKind.Set:
				size = MuiExternalSetMessage.Size;
				if (field == MuiExternalWrapperField.MethodId)
					index = 0;
				else if (field == MuiExternalWrapperField.Attribute)
					index = 1;
				else if (field == MuiExternalWrapperField.Value)
					index = 2;
				else { size = 0; return false; }
				return true;
			case MuiExternalWrapperPacketKind.Method:
				size = MuiExternalMethodMessage.Size;
				if (field == MuiExternalWrapperField.MethodId)
					index = 0;
				else { size = 0; return false; }
				return true;
			case MuiExternalWrapperPacketKind.RenderInfo:
				size = MuiExternalRenderInfoMessage.Size;
				if (field == MuiExternalWrapperField.MethodId)
					index = 0;
				else if (field == MuiExternalWrapperField.RenderInfo)
					index = 1;
				else { size = 0; return false; }
				return true;
			case MuiExternalWrapperPacketKind.AskMinMax:
				size = MuiExternalAskMinMaxMessage.Size;
				if (field == MuiExternalWrapperField.MethodId)
					index = 0;
				else if (field == MuiExternalWrapperField.Storage)
					index = 1;
				else { size = 0; return false; }
				return true;
			case MuiExternalWrapperPacketKind.Layout:
				size = MuiExternalLayoutMessage.Size;
				if (field == MuiExternalWrapperField.MethodId)
					index = 0;
				else if (field == MuiExternalWrapperField.Left)
					index = 1;
				else if (field == MuiExternalWrapperField.Top)
					index = 2;
				else if (field == MuiExternalWrapperField.Width)
					index = 3;
				else if (field == MuiExternalWrapperField.Height)
					index = 4;
				else { size = 0; return false; }
				return true;
		}
		index = 0;
		size = 0;
		return false;
	}

	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiExternalWrapperPacketKind packet,
		MuiExternalWrapperField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolveFieldIndex(packet, field, out var index, out _))
			return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiExternalMethodMessage.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				return true;
			}
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiExternalWrapperPacketKind packet,
		MuiExternalWrapperField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolveFieldIndex(packet, field, out _, out var size) ||
			!MuiGuestStructCursor.TryCreate(ref platform, message, size,
				out var cursor)) return false;
		return TryTakeField(ref platform, ref cursor, packet, field, out address);
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

// Sequential codecs for the fixed Boopsi/Dtpic wrapper records.  The legacy
// field adapter remains available for compatibility diagnostics; production
// packet consumers use these declaration-ordered named records instead.
internal static class MuiExternalWrapperMessageStructCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWrapperMethodHeaderCodec.TryReadValue(ref platform,
			message, out methodId);

	internal static bool TryReadUpdate<TPlatform>(ref TPlatform platform,
		APTR message, out MuiExternalUpdateMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiExternalUpdateMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.AttributeList) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.GadgetInfo) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Flags) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		return true;
	}

	internal static bool TryWriteUpdate<TPlatform>(ref TPlatform platform,
		APTR message, uint attributeList, uint gadgetInfo, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiExternalUpdateMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				MuiExternalWrapperMessageCodec.OmUpdate) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				attributeList) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				gadgetInfo) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				flags)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiExternalGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiExternalGetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Storage) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		return true;
	}

	internal static bool TryWriteGet<TPlatform>(ref TPlatform platform,
		APTR message, uint attribute, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiExternalGetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				MuiExternalWrapperMessageCodec.OmGet) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				storage)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiExternalSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiExternalSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Value) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		return true;
	}

	internal static bool TryWriteSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiExternalSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, method) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWrapperMethodHeaderCodec.WriteValue(ref platform, message,
			method);

	internal static bool TryReadRenderInfo<TPlatform>(ref TPlatform platform,
		APTR message, out MuiExternalRenderInfoMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiExternalRenderInfoMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.RenderInfo) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool TryWriteRenderInfo<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint renderInfo)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiExternalRenderInfoMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, method) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				renderInfo)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, out MuiExternalAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiExternalAskMinMaxMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Storage) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool TryWriteAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiExternalAskMinMaxMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				MuiExternalWrapperMessageCodec.AskMinMax) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				storage)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadLayout<TPlatform>(ref TPlatform platform,
		APTR message, out MuiExternalLayoutMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiExternalLayoutMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Left) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Top) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Width) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Height) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool TryWriteLayout<TPlatform>(ref TPlatform platform,
		APTR message, uint left, uint top, uint width, uint height)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiExternalLayoutMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				MuiExternalWrapperMessageCodec.Layout) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, left) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, top) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, width) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, height))
			return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Struct-first codec for the method-only external-wrapper header. The named
// one-ULONG record remains the ABI contract; shared guest storage keeps
// selector admission free of direct scalar lowering in freestanding 68k code.
internal static class MuiExternalWrapperMethodHeaderCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiExternalMethodMessage.Size, out var cursor) ||
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
			MuiExternalMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
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
		return MuiExternalWrapperMessageStructCodec.TryReadUpdate(ref platform,
			message, out packet) && packet.MethodId == OmUpdate;
	}

	internal static bool WriteUpdate<TPlatform>(ref TPlatform platform,
		APTR message, uint attributeList, uint gadgetInfo, uint flags)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWrapperMessageStructCodec.TryWriteUpdate(ref platform,
			message, attributeList, gadgetInfo, flags);

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiExternalGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiExternalWrapperMessageStructCodec.TryReadGet(ref platform,
			message, out packet) && packet.MethodId == OmGet;
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform, APTR message,
		uint attribute, uint storage)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWrapperMessageStructCodec.TryWriteGet(ref platform, message,
			attribute, storage);

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiExternalSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsSetMethod(method) &&
			MuiExternalWrapperMessageStructCodec.TryReadSet(ref platform, message,
				out packet) && packet.MethodId == method;
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform, APTR message,
		uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> IsSetMethod(method) &&
			MuiExternalWrapperMessageStructCodec.TryWriteSet(ref platform, message,
				method, attribute, value);

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
		return MuiExternalWrapperMessageStructCodec.TryReadMethodIdValue(
			ref platform, message, out methodId);
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
		=> IsMethod(method) &&
			MuiExternalWrapperMessageStructCodec.TryWriteMethod(ref platform,
				message, method);

	internal static bool TryReadRenderInfo<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiExternalRenderInfoMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsRenderMethod(method) &&
			MuiExternalWrapperMessageStructCodec.TryReadRenderInfo(ref platform,
				message, out packet) && packet.MethodId == method;
	}

	internal static bool WriteRenderInfo<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint renderInfo)
		where TPlatform : struct, IMuiGuestMemory
		=> IsRenderMethod(method) &&
			MuiExternalWrapperMessageStructCodec.TryWriteRenderInfo(ref platform,
				message, method, renderInfo);

	internal static bool TryReadAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, out MuiExternalAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiExternalWrapperMessageStructCodec.TryReadAskMinMax(ref platform,
			message, out packet) && packet.MethodId == AskMinMax;
	}

	internal static bool WriteAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, uint storage)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWrapperMessageStructCodec.TryWriteAskMinMax(ref platform,
			message, storage);

	internal static bool TryReadLayout<TPlatform>(ref TPlatform platform,
		APTR message, out MuiExternalLayoutMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiExternalWrapperMessageStructCodec.TryReadLayout(ref platform,
			message, out packet) && packet.MethodId == Layout;
	}

	internal static bool WriteLayout<TPlatform>(ref TPlatform platform,
		APTR message, uint left, uint top, uint width, uint height)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiExternalWrapperMessageStructCodec.TryWriteLayout(ref platform,
			message, left, top, width, height);

	private static bool IsSetMethod(uint method) => method == MethodSet ||
		method == MethodNoNotifySet;

	private static bool IsMethod(uint method) => method == Cleanup ||
		method == Show || method == Hide || method == Draw;

	private static bool IsRenderMethod(uint method) => method == Setup;
}
