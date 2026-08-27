/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
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

internal static class MuiCommonFieldCursorCodec
{
	private static bool TryGetPacketSize(MuiCommonPacketKind packet,
		out uint size)
	{
		switch (packet)
		{
			case MuiCommonPacketKind.Method:
				size = MuiCommonMethodMessage.Size;
				return true;
			case MuiCommonPacketKind.Signed:
				size = MuiCommonSignedValueMessage.Size;
				return true;
			case MuiCommonPacketKind.ScaleToValue:
				size = MuiCommonScaleToValueMessage.Size;
				return true;
			case MuiCommonPacketKind.ValueToScale:
				size = MuiCommonValueToScaleMessage.Size;
				return true;
			case MuiCommonPacketKind.Stringify:
				size = MuiCommonStringifyMessage.Size;
				return true;
			case MuiCommonPacketKind.HandleEvent:
				size = MuiCommonHandleEventMessage.Size;
				return true;
			case MuiCommonPacketKind.Get:
				size = MuiCommonGetMessage.Size;
				return true;
			case MuiCommonPacketKind.Attribute:
				size = MuiCommonAttributeMessage.Size;
				return true;
			case MuiCommonPacketKind.AskMinMax:
				size = MuiCommonAskMinMaxMessage.Size;
				return true;
			case MuiCommonPacketKind.Layout:
				size = MuiLayoutMessage.Size;
				return true;
			case MuiCommonPacketKind.Flags:
				size = MuiLayoutFlagsMessage.Size;
				return true;
			case MuiCommonPacketKind.RenderInfo:
				size = MuiLayoutRenderInfoMessage.Size;
				return true;
		}
		size = 0;
		return false;
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
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Packet, cursor.Field, out var offset) ||
			!TryGetPacketSize(cursor.Packet, out var packetSize) ||
			cursor.Message.IsNull || cursor.Message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Message, packetSize))
			return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, MuiCommonMethodMessage.FieldSize);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonPacketKind packet, MuiCommonField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiCommonFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonPacketKind packet, MuiCommonField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiCommonFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonPacketKind packet, MuiCommonField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiCommonFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Fixed common-control records cross the guest boundary through named field
// cursors. Each cursor resolves against its semantic packet kind, so codec
// callers never pass an untyped packet size or numeric member offset.
internal static class MuiCommonMethodMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.Method, MuiCommonField.MethodId,
			out packet.MethodId);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Method, MuiCommonField.MethodId, method);
	}
}

internal static class MuiCommonSignedValueMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonSignedValueMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.Signed, MuiCommonField.MethodId,
			out packet.MethodId) ||
			!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.Signed, MuiCommonField.Value, out var rawValue))
			return false;
		packet.Value = unchecked((int)rawValue);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonSignedValueMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Signed, MuiCommonField.MethodId, packet.MethodId) &&
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Signed, MuiCommonField.Value,
			unchecked((uint)packet.Value));
}

internal static class MuiCommonScaleToValueMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonScaleToValueMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.ScaleToValue, MuiCommonField.MethodId,
			out packet.MethodId) ||
			!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.ScaleToValue, MuiCommonField.Min,
				out var rawMin) ||
			!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.ScaleToValue, MuiCommonField.Max,
				out var rawMax) ||
			!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.ScaleToValue, MuiCommonField.Value,
				out var rawValue))
			return false;
		packet.Min = unchecked((int)rawMin);
		packet.Max = unchecked((int)rawMax);
		packet.Value = unchecked((int)rawValue);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonScaleToValueMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.ScaleToValue, MuiCommonField.MethodId,
			packet.MethodId) &&
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.ScaleToValue, MuiCommonField.Min,
			unchecked((uint)packet.Min)) &&
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.ScaleToValue, MuiCommonField.Max,
			unchecked((uint)packet.Max)) &&
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.ScaleToValue, MuiCommonField.Value,
			unchecked((uint)packet.Value));
}

internal static class MuiCommonValueToScaleMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonValueToScaleMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.ValueToScale, MuiCommonField.MethodId,
			out packet.MethodId) ||
			!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.ValueToScale, MuiCommonField.Min,
				out var rawMin) ||
			!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.ValueToScale, MuiCommonField.Max,
				out var rawMax))
			return false;
		packet.Min = unchecked((int)rawMin);
		packet.Max = unchecked((int)rawMax);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonValueToScaleMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.ValueToScale, MuiCommonField.MethodId,
			packet.MethodId) &&
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.ValueToScale, MuiCommonField.Min,
			unchecked((uint)packet.Min)) &&
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.ValueToScale, MuiCommonField.Max,
			unchecked((uint)packet.Max));
}

internal static class MuiCommonStringifyMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonStringifyMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.Stringify, MuiCommonField.MethodId,
			out packet.MethodId) ||
			!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.Stringify, MuiCommonField.Value,
				out var rawValue))
			return false;
		packet.Value = unchecked((int)rawValue);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonStringifyMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Stringify, MuiCommonField.MethodId,
			packet.MethodId) &&
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Stringify, MuiCommonField.Value,
			unchecked((uint)packet.Value));
}

internal static class MuiCommonHandleEventMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonHandleEventMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.HandleEvent, MuiCommonField.MethodId,
			out packet.MethodId) ||
			!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.HandleEvent, MuiCommonField.InputMessage,
				out packet.InputMessage) ||
			!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.HandleEvent, MuiCommonField.MuiKey,
				out var rawKey) ||
			!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.HandleEvent, MuiCommonField.EventHandlerNode,
				out packet.EventHandlerNode))
			return false;
		packet.MuiKey = unchecked((int)rawKey);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonHandleEventMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.HandleEvent, MuiCommonField.MethodId,
			packet.MethodId) &&
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.HandleEvent, MuiCommonField.InputMessage,
			packet.InputMessage) &&
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.HandleEvent, MuiCommonField.MuiKey,
			unchecked((uint)packet.MuiKey)) &&
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.HandleEvent, MuiCommonField.EventHandlerNode,
			packet.EventHandlerNode);
}

internal static class MuiCommonGetMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			out packet.MethodId) &&
			MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.Get, MuiCommonField.Attribute,
				out packet.Attribute) &&
			MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.Get, MuiCommonField.Storage,
				out packet.Storage);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.MethodId, packet.MethodId) &&
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			packet.Attribute) &&
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Storage, packet.Storage);
}

internal static class MuiCommonAttributeMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonAttributeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.Attribute, MuiCommonField.MethodId,
			out packet.MethodId) &&
			MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.Attribute, MuiCommonField.Attribute,
				out packet.Attribute) &&
			MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.Attribute, MuiCommonField.Value,
				out packet.Value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonAttributeMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Attribute, MuiCommonField.MethodId,
			packet.MethodId) &&
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Attribute, MuiCommonField.Attribute,
			packet.Attribute) &&
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Attribute, MuiCommonField.Value, packet.Value);
}

internal static class MuiCommonAskMinMaxMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.AskMinMax, MuiCommonField.MethodId,
			out packet.MethodId) &&
			MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.AskMinMax, MuiCommonField.Storage,
				out packet.Storage);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiCommonAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.AskMinMax, MuiCommonField.MethodId,
			packet.MethodId) &&
		MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.AskMinMax, MuiCommonField.Storage,
			packet.Storage);
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
		methodId = 0;
		if (!MuiCommonMethodMessageCodec.TryRead(ref platform, message,
			out var packet)) return false;
		methodId = packet.MethodId;
		return true;
	}

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiCommonMethodMessageCodec.TryWrite(ref platform, message, method);
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
