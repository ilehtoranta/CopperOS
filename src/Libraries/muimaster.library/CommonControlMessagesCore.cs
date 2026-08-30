/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Fixed MorphOS MUI method records used by the MG07 common-control dispatcher.
// These are value types deliberately kept independent from the object store;
// only the guest packet crosses this boundary and all pointers remain 32-bit
// APTR values until the capability layer consumes them.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCommonMethodMessage
{
	public const uint Size = 4;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCommonSignedValueMessage
{
	public const uint Size = 8;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint ValueOffset = 4;
	public uint MethodId;
	public int Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCommonScaleToValueMessage
{
	public const uint Size = 16;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint MinOffset = 4;
	public const uint MaxOffset = 8;
	public const uint ValueOffset = 12;
	public uint MethodId;
	public int Min;
	public int Max;
	public int Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCommonValueToScaleMessage
{
	public const uint Size = 12;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint MinOffset = 4;
	public const uint MaxOffset = 8;
	public uint MethodId;
	public int Min;
	public int Max;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCommonStringifyMessage
{
	public const uint Size = 8;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint ValueOffset = 4;
	public uint MethodId;
	public int Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCommonHandleEventMessage
{
	public const uint Size = 16;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint InputMessageOffset = 4;
	public const uint MuiKeyOffset = 8;
	public const uint EventHandlerNodeOffset = 12;
	public uint MethodId;
	public uint InputMessage;
	public int MuiKey;
	public uint EventHandlerNode;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCommonGetMessage
{
	public const uint Size = 12;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint AttributeOffset = 4;
	public const uint StorageOffset = 8;
	public uint MethodId;
	public uint Attribute;
	public uint Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCommonAttributeMessage
{
	public const uint Size = 12;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint AttributeOffset = 4;
	public const uint ValueOffset = 8;
	public uint MethodId;
	public uint Attribute;
	public uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCommonAskMinMaxMessage
{
	public const uint Size = 8;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint StorageOffset = 4;
	public uint MethodId;
	public uint Storage;
}

internal enum MuiCommonPacketKind : byte
{
	Method,
	Signed,
	ScaleToValue,
	ValueToScale,
	Stringify,
	HandleEvent,
	Get,
	Attribute,
	AskMinMax,
	Layout,
	Flags,
	RenderInfo,
}

internal enum MuiCommonField : byte
{
	MethodId,
	Value,
	Min,
	Max,
	InputMessage,
	MuiKey,
	EventHandlerNode,
	Attribute,
	Storage,
	Left,
	Top,
	Width,
	Height,
	Flags,
	RenderInfo,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCommonFieldCursor
{
	internal APTR Message;
	internal MuiCommonPacketKind Packet;
	internal MuiCommonField Field;
}

internal static class MuiCommonMessageMemoryCodec
{
	private static bool TryGetPacketSize(MuiCommonPacketKind packet,
		out uint size)
	{
		// Keep packet admission as a straight-line bounded chain. Besides being
		// easy to audit, this avoids a compiler-generated jump table in the
		// freestanding 68k path while the wire positions remain owned by the
		// named record adapter below.
		if (packet == MuiCommonPacketKind.Method)
			size = MuiCommonMethodMessage.Size;
		else if (packet == MuiCommonPacketKind.Signed)
			size = MuiCommonSignedValueMessage.Size;
		else if (packet == MuiCommonPacketKind.ScaleToValue)
			size = MuiCommonScaleToValueMessage.Size;
		else if (packet == MuiCommonPacketKind.ValueToScale)
			size = MuiCommonValueToScaleMessage.Size;
		else if (packet == MuiCommonPacketKind.Stringify)
			size = MuiCommonStringifyMessage.Size;
		else if (packet == MuiCommonPacketKind.HandleEvent)
			size = MuiCommonHandleEventMessage.Size;
		else if (packet == MuiCommonPacketKind.Get)
			size = MuiCommonGetMessage.Size;
		else if (packet == MuiCommonPacketKind.Attribute)
			size = MuiCommonAttributeMessage.Size;
		else if (packet == MuiCommonPacketKind.AskMinMax)
			size = MuiCommonAskMinMaxMessage.Size;
		else if (packet == MuiCommonPacketKind.Layout)
			size = MuiLayoutMessage.Size;
		else if (packet == MuiCommonPacketKind.Flags)
			size = MuiLayoutFlagsMessage.Size;
		else if (packet == MuiCommonPacketKind.RenderInfo)
			size = MuiLayoutRenderInfoMessage.Size;
		else
		{
			size = 0;
			return false;
		}
		return true;
	}

	private static bool TryResolve(MuiCommonPacketKind packet,
		MuiCommonField field, out uint offset)
	{
		// Keep the packed ABI selector straight-line for freestanding native
		// lowering. The offset table remains confined to this codec; consumers
		// receive the named common-control records below.
		if (packet == MuiCommonPacketKind.Method)
		{
			if (field == MuiCommonField.MethodId)
			{
				offset = MuiCommonMethodMessage.MethodIdOffset;
				return true;
			}
		}
		else if (packet == MuiCommonPacketKind.Signed ||
			packet == MuiCommonPacketKind.Stringify)
		{
			if (field == MuiCommonField.MethodId)
			{
				offset = packet == MuiCommonPacketKind.Signed
					? MuiCommonSignedValueMessage.MethodIdOffset
					: MuiCommonStringifyMessage.MethodIdOffset;
				return true;
			}
			if (field == MuiCommonField.Value)
			{
				offset = packet == MuiCommonPacketKind.Signed
					? MuiCommonSignedValueMessage.ValueOffset
					: MuiCommonStringifyMessage.ValueOffset;
				return true;
			}
		}
		else if (packet == MuiCommonPacketKind.ScaleToValue)
		{
			if (field == MuiCommonField.MethodId)
			{
				offset = MuiCommonScaleToValueMessage.MethodIdOffset;
				return true;
			}
			if (field == MuiCommonField.Min)
			{
				offset = MuiCommonScaleToValueMessage.MinOffset;
				return true;
			}
			if (field == MuiCommonField.Max)
			{
				offset = MuiCommonScaleToValueMessage.MaxOffset;
				return true;
			}
			if (field == MuiCommonField.Value)
			{
				offset = MuiCommonScaleToValueMessage.ValueOffset;
				return true;
			}
		}
		else if (packet == MuiCommonPacketKind.ValueToScale)
		{
			if (field == MuiCommonField.MethodId)
			{
				offset = MuiCommonValueToScaleMessage.MethodIdOffset;
				return true;
			}
			if (field == MuiCommonField.Min)
			{
				offset = MuiCommonValueToScaleMessage.MinOffset;
				return true;
			}
			if (field == MuiCommonField.Max)
			{
				offset = MuiCommonValueToScaleMessage.MaxOffset;
				return true;
			}
		}
		else if (packet == MuiCommonPacketKind.HandleEvent)
		{
			if (field == MuiCommonField.MethodId)
			{
				offset = MuiCommonHandleEventMessage.MethodIdOffset;
				return true;
			}
			if (field == MuiCommonField.InputMessage)
			{
				offset = MuiCommonHandleEventMessage.InputMessageOffset;
				return true;
			}
			if (field == MuiCommonField.MuiKey)
			{
				offset = MuiCommonHandleEventMessage.MuiKeyOffset;
				return true;
			}
			if (field == MuiCommonField.EventHandlerNode)
			{
				offset = MuiCommonHandleEventMessage.EventHandlerNodeOffset;
				return true;
			}
		}
		else if (packet == MuiCommonPacketKind.Get)
		{
			if (field == MuiCommonField.MethodId)
			{
				offset = MuiCommonGetMessage.MethodIdOffset;
				return true;
			}
			if (field == MuiCommonField.Attribute)
			{
				offset = MuiCommonGetMessage.AttributeOffset;
				return true;
			}
			if (field == MuiCommonField.Storage)
			{
				offset = MuiCommonGetMessage.StorageOffset;
				return true;
			}
		}
		else if (packet == MuiCommonPacketKind.Attribute)
		{
			if (field == MuiCommonField.MethodId)
			{
				offset = MuiCommonAttributeMessage.MethodIdOffset;
				return true;
			}
			if (field == MuiCommonField.Attribute)
			{
				offset = MuiCommonAttributeMessage.AttributeOffset;
				return true;
			}
			if (field == MuiCommonField.Value)
			{
				offset = MuiCommonAttributeMessage.ValueOffset;
				return true;
			}
		}
		else if (packet == MuiCommonPacketKind.AskMinMax)
		{
			if (field == MuiCommonField.MethodId)
			{
				offset = MuiCommonAskMinMaxMessage.MethodIdOffset;
				return true;
			}
			if (field == MuiCommonField.Storage)
			{
				offset = MuiCommonAskMinMaxMessage.StorageOffset;
				return true;
			}
		}
		else if (packet == MuiCommonPacketKind.Layout)
		{
			if (field == MuiCommonField.MethodId)
			{
				offset = MuiLayoutMessage.MethodIdOffset;
				return true;
			}
			if (field == MuiCommonField.Left)
			{
				offset = MuiLayoutMessage.LeftOffset;
				return true;
			}
			if (field == MuiCommonField.Top)
			{
				offset = MuiLayoutMessage.TopOffset;
				return true;
			}
			if (field == MuiCommonField.Width)
			{
				offset = MuiLayoutMessage.WidthOffset;
				return true;
			}
			if (field == MuiCommonField.Height)
			{
				offset = MuiLayoutMessage.HeightOffset;
				return true;
			}
			if (field == MuiCommonField.Flags)
			{
				offset = MuiLayoutMessage.FlagsOffset;
				return true;
			}
		}
		else if (packet == MuiCommonPacketKind.Flags)
		{
			if (field == MuiCommonField.MethodId)
			{
				offset = MuiLayoutFlagsMessage.MethodIdOffset;
				return true;
			}
			if (field == MuiCommonField.Flags)
			{
				offset = MuiLayoutFlagsMessage.FlagsOffset;
				return true;
			}
		}
		else if (packet == MuiCommonPacketKind.RenderInfo)
		{
			if (field == MuiCommonField.MethodId)
			{
				offset = MuiLayoutRenderInfoMessage.MethodIdOffset;
				return true;
			}
			if (field == MuiCommonField.RenderInfo)
			{
				offset = MuiLayoutRenderInfoMessage.RenderInfoOffset;
				return true;
			}
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCommonFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor.Message, cursor.Packet,
			cursor.Field, out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonPacketKind packet, MuiCommonField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset) ||
			!TryGetPacketSize(packet, out var packetSize) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, packetSize))
			return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiCommonMethodMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonPacketKind packet, MuiCommonField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonPacketKind packet, MuiCommonField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	// The common method header is the smallest fixed record and is used by
	// nearly every dispatcher. Keep its complete-record admission and named
	// field access in this specialised adapter so the native path does not
	// need to lower the zero-valued Method packet selector through a switch.
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (message.IsNull || message.Raw > uint.MaxValue -
			MuiCommonMethodMessage.Size ||
			!platform.IsMapped(message, MuiCommonMethodMessage.Size))
			return false;
		value = platform.ReadUInt32(message,
			(int)MuiCommonMethodMessage.MethodIdOffset);
		return true;
	}

	internal static bool TryWriteMethodId<TPlatform>(ref TPlatform platform,
		APTR message, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || message.Raw > uint.MaxValue -
			MuiCommonMethodMessage.Size ||
			!platform.IsMapped(message, MuiCommonMethodMessage.Size))
			return false;
		platform.WriteUInt32(message,
			(int)MuiCommonMethodMessage.MethodIdOffset, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// field cursor. Live common-control codecs use MuiCommonMessageMemoryCodec.
internal static class MuiCommonFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCommonFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonMessageMemoryCodec.TryGetAddress(ref platform, cursor,
			out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonPacketKind packet, MuiCommonField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonMessageMemoryCodec.TryGetAddress(ref platform, message, packet,
			field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonPacketKind packet, MuiCommonField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonMessageMemoryCodec.TryReadUInt32(ref platform, message, packet,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonPacketKind packet, MuiCommonField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonMessageMemoryCodec.TryWriteUInt32(ref platform, message, packet,
			field, value);
}

// Complete sequential codecs for the fixed CommonControl envelopes. Numeric
// positions remain owned by the declaration order of each packed record; the
// legacy field adapter above is retained only for compatibility diagnostics.
internal static class MuiCommonMessageStructCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCommonMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryWriteMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCommonMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadSigned<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonSignedValueMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCommonSignedValueMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawValue)) return false;
		value.MethodId = methodId;
		value.Value = unchecked((int)rawValue);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteSigned<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonSignedValueMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCommonSignedValueMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.Value))) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadScaleToValue<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonScaleToValueMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCommonScaleToValueMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMin) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMax) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawValue)) return false;
		value.MethodId = methodId;
		value.Min = unchecked((int)rawMin);
		value.Max = unchecked((int)rawMax);
		value.Value = unchecked((int)rawValue);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteScaleToValue<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonScaleToValueMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCommonScaleToValueMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.Min)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.Max)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.Value))) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadValueToScale<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonValueToScaleMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCommonValueToScaleMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMin) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMax)) return false;
		value.MethodId = methodId;
		value.Min = unchecked((int)rawMin);
		value.Max = unchecked((int)rawMax);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteValueToScale<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonValueToScaleMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCommonValueToScaleMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.Min)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.Max))) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStringify<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonStringifyMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCommonStringifyMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawValue)) return false;
		value.MethodId = methodId;
		value.Value = unchecked((int)rawValue);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteStringify<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonStringifyMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCommonStringifyMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.Value))) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadHandleEvent<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonHandleEventMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCommonHandleEventMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var inputMessage) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawKey) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var eventHandlerNode)) return false;
		value.MethodId = methodId;
		value.InputMessage = inputMessage;
		value.MuiKey = unchecked((int)rawKey);
		value.EventHandlerNode = eventHandlerNode;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteHandleEvent<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonHandleEventMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCommonHandleEventMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.InputMessage) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.MuiKey)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.EventHandlerNode)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonGetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCommonGetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var storage)) return false;
		value.MethodId = methodId;
		value.Attribute = attribute;
		value.Storage = storage;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonGetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCommonGetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Storage)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadAttribute<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonAttributeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCommonAttributeMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var valueWord)) return false;
		value.MethodId = methodId;
		value.Attribute = attribute;
		value.Value = valueWord;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteAttribute<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonAttributeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCommonAttributeMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonAskMinMaxMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCommonAskMinMaxMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var storage)) return false;
		value.MethodId = methodId;
		value.Storage = storage;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonAskMinMaxMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCommonAskMinMaxMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Storage)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Fixed common-control records cross the guest boundary through named packet
// structs and the direct memory adapter. The cursor wrapper below is retained
// only for older callers; live codecs never construct a field cursor.
internal static class MuiCommonMethodMessageCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCommonMessageStructCodec.TryReadMethodIdValue(ref platform,
			message, out packet.MethodId);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiCommonMessageStructCodec.TryWriteMethodIdValue(ref platform,
			message, method);
	}
}

internal static class MuiCommonSignedValueMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonSignedValueMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiCommonMessageStructCodec.TryReadSigned(ref platform,
			message, out packet);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonSignedValueMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonMessageStructCodec.WriteSigned(ref platform, message, packet);
}

internal static class MuiCommonScaleToValueMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonScaleToValueMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiCommonMessageStructCodec.TryReadScaleToValue(ref platform,
			message, out packet);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonScaleToValueMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonMessageStructCodec.WriteScaleToValue(ref platform, message,
			packet);
}

internal static class MuiCommonValueToScaleMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonValueToScaleMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiCommonMessageStructCodec.TryReadValueToScale(ref platform,
			message, out packet);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonValueToScaleMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonMessageStructCodec.WriteValueToScale(ref platform, message,
			packet);
}

internal static class MuiCommonStringifyMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonStringifyMessage packet)
	where TPlatform : struct, IMuiGuestMemory
	{
		return MuiCommonMessageStructCodec.TryReadStringify(ref platform,
			message, out packet);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonStringifyMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonMessageStructCodec.WriteStringify(ref platform, message,
			packet);
}

internal static class MuiCommonHandleEventMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonHandleEventMessage packet)
	where TPlatform : struct, IMuiGuestMemory
	{
		return MuiCommonMessageStructCodec.TryReadHandleEvent(ref platform,
			message, out packet);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonHandleEventMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonMessageStructCodec.WriteHandleEvent(ref platform, message,
			packet);
}

internal static class MuiCommonGetMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiCommonMessageStructCodec.TryReadGet(ref platform, message,
			out packet);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonMessageStructCodec.WriteGet(ref platform, message, packet);
}

internal static class MuiCommonAttributeMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonAttributeMessage packet)
	where TPlatform : struct, IMuiGuestMemory
	{
		return MuiCommonMessageStructCodec.TryReadAttribute(ref platform,
			message, out packet);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonAttributeMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonMessageStructCodec.WriteAttribute(ref platform, message, packet);
}

internal static class MuiCommonAskMinMaxMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonAskMinMaxMessage packet)
	where TPlatform : struct, IMuiGuestMemory
	{
		return MuiCommonMessageStructCodec.TryReadAskMinMax(ref platform,
			message, out packet);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonMessageStructCodec.WriteAskMinMax(ref platform, message, packet);
}

public static class MuiCommonControlPacketCore
{
	public const uint OmGet = 0x00000104u;
	public const uint Set = 0x8042549Au;
	public const uint NoNotifySet = 0x8042216Fu;
	public const uint NumericDecrease = 0x804243A7u;
	public const uint NumericIncrease = 0x80426ECDu;
	public const uint NumericScaleToValue = 0x8042032Cu;
	public const uint NumericSetDefault = 0x8042AB0Au;
	public const uint NumericStringify = 0x80424891u;
	public const uint NumericValueToScale = 0x80423E4Fu;
	public const uint PropDecrease = 0x80420DD1u;
	public const uint PropIncrease = 0x8042CAC0u;
	public const uint HandleEvent = 0x80426D66u;
	public const uint AskMinMax = 0x80423874u;
	public const uint Layout = 0x8042845Bu;
	public const uint Draw = 0x80426F3Fu;
	public const uint Setup = 0x80428354u;
	public const uint Cleanup = 0x8042D985u;

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCommonMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiCommonMethodMessageCodec.TryRead(ref platform, message,
			out packet)) return false;
		return packet.MethodId == method;
	}

	// Read only the common fixed method header. Dispatchers use this named
	// record to select a decoder; method-specific validation remains in the
	// TryRead* methods below.
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	// Native qualification keeps method-header admission scalar while the
	// dispatcher-facing overload above retains the named value-type record.
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiCommonMessageStructCodec.TryReadMethodIdValue(ref platform,
			message, out methodId);
	}

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiCommonMethodMessageCodec.TryWrite(ref platform, message, method);
	}

	public static bool WriteSigned<TPlatform>(ref TPlatform platform,
		APTR message, uint method, int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCommonSignedValueMessage);
		packet.MethodId = method;
		packet.Value = value;
		return (method == NumericDecrease || method == NumericIncrease ||
			method == PropDecrease || method == PropIncrease) &&
			MuiCommonSignedValueMessageCodec.TryWrite(ref platform, message, packet);
	}

	public static bool WriteScaleToValue<TPlatform>(ref TPlatform platform,
		APTR message, int min, int max, int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCommonScaleToValueMessage);
		packet.MethodId = NumericScaleToValue;
		packet.Min = min;
		packet.Max = max;
		packet.Value = value;
		return MuiCommonScaleToValueMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	public static bool WriteValueToScale<TPlatform>(ref TPlatform platform,
		APTR message, int min, int max)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCommonValueToScaleMessage);
		packet.MethodId = NumericValueToScale;
		packet.Min = min;
		packet.Max = max;
		return MuiCommonValueToScaleMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	public static bool WriteStringify<TPlatform>(ref TPlatform platform,
		APTR message, int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCommonStringifyMessage);
		packet.MethodId = NumericStringify;
		packet.Value = value;
		return MuiCommonStringifyMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	internal static bool TryReadSigned<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCommonSignedValueMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiCommonSignedValueMessageCodec.TryRead(ref platform, message,
			out packet)) return false;
		return packet.MethodId == method;
	}

	internal static bool TryReadScaleToValue<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonScaleToValueMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCommonScaleToValueMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == NumericScaleToValue;
	}

	internal static bool TryReadValueToScale<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonValueToScaleMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCommonValueToScaleMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == NumericValueToScale;
	}

	internal static bool TryReadStringify<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonStringifyMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCommonStringifyMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == NumericStringify;
	}

	internal static bool TryReadHandleEvent<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonHandleEventMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCommonHandleEventMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == HandleEvent;
	}

	internal static bool WriteHandleEvent<TPlatform>(ref TPlatform platform,
		APTR message, uint inputMessage, int muiKey, uint eventHandlerNode)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCommonHandleEventMessage);
		packet.MethodId = HandleEvent;
		packet.InputMessage = inputMessage;
		packet.MuiKey = muiKey;
		packet.EventHandlerNode = eventHandlerNode;
		return MuiCommonHandleEventMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCommonGetMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == OmGet;
	}

	internal static bool TryReadAttribute<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCommonAttributeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCommonAttributeMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == method;
	}

	internal static bool WriteAttribute<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCommonAttributeMessage);
		packet.MethodId = method;
		packet.Attribute = attribute;
		packet.Value = value;
		return (method == Set || method == NoNotifySet) &&
			MuiCommonAttributeMessageCodec.TryWrite(ref platform, message, packet);
	}

	public static bool WriteGet<TPlatform>(ref TPlatform platform, APTR message,
		uint attribute, APTR storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCommonGetMessage);
		packet.MethodId = OmGet;
		packet.Attribute = attribute;
		packet.Storage = storage.Raw;
		return MuiCommonGetMessageCodec.TryWrite(ref platform, message, packet);
	}

	public static bool WriteAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, APTR storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCommonAskMinMaxMessage);
		packet.MethodId = AskMinMax;
		packet.Storage = storage.Raw;
		return MuiCommonAskMinMaxMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	internal static bool TryReadAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCommonAskMinMaxMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == AskMinMax;
	}

	internal static bool TryReadLayout<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (message.IsNull || !platform.IsMapped(message, MuiLayoutMessage.Size) ||
			!TryReadMethodIdValue(ref platform, message, out methodId) ||
			methodId != Layout) return false;
		return MuiLayoutPacketCodec.TryReadLayout(ref platform, message,
			out packet);
	}

	internal static bool TryReadDraw<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutFlagsMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (message.IsNull || !platform.IsMapped(message,
			MuiLayoutFlagsMessage.Size) ||
			!TryReadMethodIdValue(ref platform, message, out methodId) ||
			methodId != Draw)
			return false;
		return MuiLayoutPacketCodec.TryReadFlags(ref platform, message, Draw,
			out packet);
	}

	internal static bool TryReadSetup<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutRenderInfoMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (message.IsNull || !platform.IsMapped(message,
			MuiLayoutRenderInfoMessage.Size) ||
			!TryReadMethodIdValue(ref platform, message, out methodId) ||
			methodId != Setup) return false;
		return MuiLayoutPacketCodec.TryReadRenderInfo(ref platform, message, Setup,
			out packet);
	}

}
