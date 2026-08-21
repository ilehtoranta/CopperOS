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
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCommonSignedValueMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public int Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCommonScaleToValueMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public int Min;
	public int Max;
	public int Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCommonValueToScaleMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public int Min;
	public int Max;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCommonStringifyMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public int Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCommonHandleEventMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint InputMessage;
	public int MuiKey;
	public uint EventHandlerNode;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCommonGetMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCommonAttributeMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCommonAskMinMaxMessage
{
	public const uint Size = 8;
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
	private static bool TryResolve(MuiCommonPacketKind packet,
		MuiCommonField field, out uint offset)
	{
		switch (packet)
		{
			case MuiCommonPacketKind.Method:
			case MuiCommonPacketKind.Signed:
			case MuiCommonPacketKind.Stringify:
				if (field == MuiCommonField.MethodId) { offset = 0; return true; }
				if (packet != MuiCommonPacketKind.Method &&
					field == MuiCommonField.Value) { offset = 4; return true; }
				break;
			case MuiCommonPacketKind.ScaleToValue:
				if (field == MuiCommonField.MethodId) { offset = 0; return true; }
				if (field == MuiCommonField.Min) { offset = 4; return true; }
				if (field == MuiCommonField.Max) { offset = 8; return true; }
				if (field == MuiCommonField.Value) { offset = 12; return true; }
				break;
			case MuiCommonPacketKind.ValueToScale:
				if (field == MuiCommonField.MethodId) { offset = 0; return true; }
				if (field == MuiCommonField.Min) { offset = 4; return true; }
				if (field == MuiCommonField.Max) { offset = 8; return true; }
				break;
			case MuiCommonPacketKind.HandleEvent:
				if (field == MuiCommonField.MethodId) { offset = 0; return true; }
				if (field == MuiCommonField.InputMessage) { offset = 4; return true; }
				if (field == MuiCommonField.MuiKey) { offset = 8; return true; }
				if (field == MuiCommonField.EventHandlerNode) { offset = 12; return true; }
				break;
			case MuiCommonPacketKind.Get:
				if (field == MuiCommonField.MethodId) { offset = 0; return true; }
				if (field == MuiCommonField.Attribute) { offset = 4; return true; }
				if (field == MuiCommonField.Storage) { offset = 8; return true; }
				break;
			case MuiCommonPacketKind.Attribute:
				if (field == MuiCommonField.MethodId) { offset = 0; return true; }
				if (field == MuiCommonField.Attribute) { offset = 4; return true; }
				if (field == MuiCommonField.Value) { offset = 8; return true; }
				break;
			case MuiCommonPacketKind.AskMinMax:
				if (field == MuiCommonField.MethodId) { offset = 0; return true; }
				if (field == MuiCommonField.Storage) { offset = 4; return true; }
				break;
			case MuiCommonPacketKind.Layout:
				if (field == MuiCommonField.MethodId) { offset = 0; return true; }
				if (field == MuiCommonField.Left) { offset = 4; return true; }
				if (field == MuiCommonField.Top) { offset = 8; return true; }
				if (field == MuiCommonField.Width) { offset = 12; return true; }
				if (field == MuiCommonField.Height) { offset = 16; return true; }
				if (field == MuiCommonField.Flags) { offset = 20; return true; }
				break;
			case MuiCommonPacketKind.Flags:
				if (field == MuiCommonField.MethodId) { offset = 0; return true; }
				if (field == MuiCommonField.Flags) { offset = 4; return true; }
				break;
			case MuiCommonPacketKind.RenderInfo:
				if (field == MuiCommonField.MethodId) { offset = 0; return true; }
				if (field == MuiCommonField.RenderInfo) { offset = 4; return true; }
				break;
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
			cursor.Message.IsNull || cursor.Message.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
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
		if (message.IsNull || !platform.IsMapped(message,
			MuiCommonMethodMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != method)
			return false;
		packet.MethodId = header.MethodId;
		return true;
	}

	// Read only the common fixed method header. Dispatchers use this named
	// record to select a decoder; method-specific validation remains in the
	// TryRead* methods below.
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCommonMethodMessage.Size)) return false;
		return MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.Method, MuiCommonField.MethodId,
			out packet.MethodId);
	}

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCommonMethodMessage.Size)) return false;
		return MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Method, MuiCommonField.MethodId, method);
	}

	internal static bool TryReadSigned<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCommonSignedValueMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCommonSignedValueMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != method)
			return false;
		packet.MethodId = header.MethodId;
		return MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.Signed, MuiCommonField.Value, out var rawValue)
			? SetSignedValue(out packet.Value, rawValue) : false;
	}

	internal static bool TryReadScaleToValue<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonScaleToValueMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCommonScaleToValueMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != NumericScaleToValue) return false;
		packet.MethodId = header.MethodId;
		if (!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.ScaleToValue, MuiCommonField.Min, out var rawMin) ||
			!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.ScaleToValue, MuiCommonField.Max, out var rawMax) ||
			!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.ScaleToValue, MuiCommonField.Value, out var rawValue))
			return false;
		packet.Min = unchecked((int)rawMin);
		packet.Max = unchecked((int)rawMax);
		packet.Value = unchecked((int)rawValue);
		return true;
	}

	internal static bool TryReadValueToScale<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonValueToScaleMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCommonValueToScaleMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != NumericValueToScale) return false;
		packet.MethodId = header.MethodId;
		if (!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.ValueToScale, MuiCommonField.Min, out var rawMin) ||
			!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.ValueToScale, MuiCommonField.Max, out var rawMax))
			return false;
		packet.Min = unchecked((int)rawMin);
		packet.Max = unchecked((int)rawMax);
		return true;
	}

	internal static bool TryReadStringify<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonStringifyMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCommonStringifyMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != NumericStringify) return false;
		packet.MethodId = header.MethodId;
		return MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.Stringify, MuiCommonField.Value, out var rawValue)
			? SetSignedValue(out packet.Value, rawValue) : false;
	}

	internal static bool TryReadHandleEvent<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonHandleEventMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCommonHandleEventMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != HandleEvent) return false;
		packet.MethodId = header.MethodId;
		if (!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.HandleEvent, MuiCommonField.InputMessage,
			out packet.InputMessage) ||
			!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.HandleEvent, MuiCommonField.MuiKey,
				out var rawKey) ||
			!MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.HandleEvent, MuiCommonField.EventHandlerNode,
				out packet.EventHandlerNode)) return false;
		packet.MuiKey = unchecked((int)rawKey);
		return true;
	}

	internal static bool WriteHandleEvent<TPlatform>(ref TPlatform platform,
		APTR message, uint inputMessage, int muiKey, uint eventHandlerNode)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCommonHandleEventMessage.Size)) return false;
		return MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.HandleEvent, MuiCommonField.MethodId, HandleEvent) &&
			MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiCommonPacketKind.HandleEvent, MuiCommonField.InputMessage,
				inputMessage) &&
				MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
					MuiCommonPacketKind.HandleEvent, MuiCommonField.MuiKey,
					unchecked((uint)muiKey)) &&
					MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
						MuiCommonPacketKind.HandleEvent,
						MuiCommonField.EventHandlerNode, eventHandlerNode);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCommonGetMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != OmGet)
			return false;
		packet.MethodId = header.MethodId;
		return MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			out packet.Attribute) &&
			MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.Get, MuiCommonField.Storage,
				out packet.Storage);
	}

	internal static bool TryReadAttribute<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCommonAttributeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCommonAttributeMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != method)
			return false;
		packet.MethodId = header.MethodId;
		return MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.Attribute, MuiCommonField.Attribute,
			out packet.Attribute) &&
			MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.Attribute, MuiCommonField.Value,
				out packet.Value);
	}

	internal static bool WriteAttribute<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if ((method != Set && method != NoNotifySet) || message.IsNull ||
			!platform.IsMapped(message, MuiCommonAttributeMessage.Size)) return false;
		var packet = default(MuiCommonAttributeMessage);
		packet.MethodId = method;
		packet.Attribute = attribute;
		packet.Value = value;
		return MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Attribute, MuiCommonField.MethodId,
			packet.MethodId) &&
			MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiCommonPacketKind.Attribute, MuiCommonField.Attribute,
				packet.Attribute) &&
				MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
					MuiCommonPacketKind.Attribute, MuiCommonField.Value,
					packet.Value);
	}

	internal static bool TryReadAskMinMax<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCommonAskMinMaxMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCommonAskMinMaxMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != AskMinMax) return false;
		packet.MethodId = header.MethodId;
		return MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.AskMinMax, MuiCommonField.Storage,
			out packet.Storage);
	}

	internal static bool TryReadLayout<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message, MuiLayoutMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != Layout) return false;
		packet.MethodId = header.MethodId;
		return MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.Layout, MuiCommonField.Left, out packet.Left) &&
			MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCommonPacketKind.Layout, MuiCommonField.Top, out packet.Top) &&
				MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
					MuiCommonPacketKind.Layout, MuiCommonField.Width,
					out packet.Width) &&
					MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
						MuiCommonPacketKind.Layout, MuiCommonField.Height,
						out packet.Height) &&
						MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
							MuiCommonPacketKind.Layout, MuiCommonField.Flags,
							out packet.Flags);
	}

	internal static bool TryReadDraw<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutFlagsMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiLayoutFlagsMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != Draw)
			return false;
		packet.MethodId = header.MethodId;
		return MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.Flags, MuiCommonField.Flags, out packet.Flags);
	}

	internal static bool TryReadSetup<TPlatform>(ref TPlatform platform,
		APTR message, out MuiLayoutRenderInfoMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiLayoutRenderInfoMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != Setup) return false;
		packet.MethodId = header.MethodId;
		return MuiCommonFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiCommonPacketKind.RenderInfo, MuiCommonField.RenderInfo,
			out packet.RenderInfo);
	}

	private static bool SetSignedValue(out int target, uint raw)
	{
		target = unchecked((int)raw);
		return true;
	}
}
