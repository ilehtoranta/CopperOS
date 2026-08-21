/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationMethodHeaderMessage
{
	internal const uint Size = 4;
	internal uint MethodId;
}

internal static class MuiApplicationMethodHeaderCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationMethodHeaderMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationMethodHeaderMessage.Size)) return false;
		value.MethodId = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationMethodHeaderMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationMethodHeaderMessage.Size)) return false;
		platform.WriteUInt32(address, 0, value.MethodId);
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationReturnIdMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint ReturnId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationInputMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint SignalStorage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationInputBufferedMessage
{
	public const uint Size = 4;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationInputHandlerMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Handler;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationPushMethodMessage
{
	public const uint Size = 12;
	public const uint ParametersOffset = Size;
	public const uint MaximumParameterCount = 7;
	public uint MethodId;
	public uint Destination;
	public uint Count;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationPushMethodParameter
{
	public const uint Size = 4;
	public uint Value;
}

// Named cursor for the caller-owned parameter vector appended to a
// MUIM_Application_PushMethod packet. The packet header remains a fixed wire
// record; this cursor owns the inline-tail boundary and element validation.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationPushMethodParameterCursor
{
	internal const uint EntrySize = MuiApplicationPushMethodParameter.Size;
	internal const uint MaximumEntries =
		MuiApplicationPushMethodMessage.MaximumParameterCount;
	internal APTR Message;
	internal uint Index;
}

internal static class MuiApplicationPushMethodParameterCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiApplicationPushMethodParameterCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Message.IsNull || cursor.Index >=
			MuiApplicationPushMethodParameterCursor.MaximumEntries ||
			!platform.IsMapped(cursor.Message,
			MuiApplicationPushMethodMessage.Size) || cursor.Message.Raw >
			uint.MaxValue - MuiApplicationPushMethodMessage.ParametersOffset)
			return false;
		var baseAddress = APTR.FromPointer(cursor.Message.Raw +
			MuiApplicationPushMethodMessage.ParametersOffset);
		if (cursor.Index > (uint.MaxValue - baseAddress.Raw) /
			MuiApplicationPushMethodParameterCursor.EntrySize) return false;
		var offset = cursor.Index *
			MuiApplicationPushMethodParameterCursor.EntrySize;
		if (baseAddress.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(baseAddress.Raw + offset);
		return platform.IsMapped(address,
			MuiApplicationPushMethodParameterCursor.EntrySize);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationUnpushMethodMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint TargetObject;
	public uint MethodIdSelector;
	public uint Method;
}

internal enum MuiApplicationQueuePacketKind : byte
{
	PushMethod,
	UnpushMethod,
}

internal enum MuiApplicationQueuePacketField : byte
{
	MethodId,
	Destination,
	Count,
	TargetObject,
	MethodIdSelector,
	Method,
}

// Named view of the fixed queue-control packet records. Packet kind selects
// the complete guest struct and field selects one of its members; numeric
// offsets remain confined to the ABI resolver below.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationQueuePacketFieldCursor
{
	internal APTR Message;
	internal MuiApplicationQueuePacketKind Packet;
	internal MuiApplicationQueuePacketField Field;
}

internal static class MuiApplicationQueuePacketFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationQueuePacketKind packet,
		MuiApplicationQueuePacketField field, out uint offset, out uint size)
	{
		size = 0;
		switch (packet)
		{
			case MuiApplicationQueuePacketKind.PushMethod:
				size = MuiApplicationPushMethodMessage.Size;
				if (field == MuiApplicationQueuePacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				if (field == MuiApplicationQueuePacketField.Destination)
				{
					offset = 4;
					return true;
				}
				if (field == MuiApplicationQueuePacketField.Count)
				{
					offset = 8;
					return true;
				}
				break;
			case MuiApplicationQueuePacketKind.UnpushMethod:
				size = MuiApplicationUnpushMethodMessage.Size;
				if (field == MuiApplicationQueuePacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				if (field == MuiApplicationQueuePacketField.TargetObject)
				{
					offset = 4;
					return true;
				}
				if (field == MuiApplicationQueuePacketField.MethodIdSelector)
				{
					offset = 8;
					return true;
				}
				if (field == MuiApplicationQueuePacketField.Method)
				{
					offset = 12;
					return true;
				}
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationQueuePacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Packet, cursor.Field, out var offset,
			out var packetSize) || cursor.Message.IsNull ||
			cursor.Message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Message, packetSize)) return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiApplicationQueuePacketKind packet,
		MuiApplicationQueuePacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationQueuePacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiApplicationQueuePacketKind packet,
		MuiApplicationQueuePacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationQueuePacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationShowHelpMessage
{
	public const uint Size = 20;
	public uint MethodId;
	public uint ReferenceWindow;
	public uint HelpFile;
	public uint Node;
	public uint Line;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationAboutMuiMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint ReferenceWindow;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationConfigIdMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint ConfigId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationSetConfigItemMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Item;
	public uint Data;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationOpenConfigWindowMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Flags;
	public uint ClassId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationBuildSettingsPanelMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Number;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationSettingsIoMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Name;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationCheckRefreshMessage
{
	public const uint Size = 4;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationLoopMessage
{
	public const uint Size = 4;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationMenuQueryMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint MenuId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationMenuSetMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint MenuId;
	public uint State;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowMenuQueryMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint MenuId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowMenuSetMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint MenuId;
	public uint State;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowEventHandlerMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Handler;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowSnapshotMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowMethodMessage
{
	public const uint Size = 4;
	public uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowCycleChainMessage
{
	public const uint Size = 8;
	public const uint VectorOffset = 4;
	public uint MethodId;
	public uint FirstObject;
}

// Named cursor for the inline NULL-terminated object vector carried by a
// MUIM_Window_SetCycleChain packet. The existing vector cursor handles the
// indexed slot; this packet view owns the message-to-vector boundary.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowCycleChainInlineVectorCursor
{
	internal APTR Message;
	internal uint Index;
}

internal static class MuiWindowCycleChainInlineVectorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiWindowCycleChainInlineVectorCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Message.IsNull || !platform.IsMapped(cursor.Message,
			MuiWindowCycleChainMessage.Size) || cursor.Message.Raw >
			uint.MaxValue - MuiWindowCycleChainMessage.VectorOffset) return false;
		var vector = APTR.FromPointer(cursor.Message.Raw +
			MuiWindowCycleChainMessage.VectorOffset);
		var vectorCursor = default(MuiApplicationWindowCycleChainCursor);
		vectorCursor.Base = vector;
		vectorCursor.Index = cursor.Index;
		return MuiApplicationWindowCycleChainVectorCodec.TryGetEntry(
			ref platform, vectorCursor, out address);
	}
}

internal enum MuiApplicationInputPacketKind : byte
{
	ReturnId,
	Input,
	InputHandler,
}

internal enum MuiApplicationInputPacketField : byte
{
	MethodId,
	ReturnId,
	SignalStorage,
	Handler,
}

// Named view of the fixed Application input-family records.  The packet kind
// selects the complete guest struct; the field selects a member within it.
// Numeric offsets are confined to the ABI resolver below.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationInputPacketFieldCursor
{
	internal APTR Message;
	internal MuiApplicationInputPacketKind Packet;
	internal MuiApplicationInputPacketField Field;
}

internal static class MuiApplicationInputPacketFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationInputPacketKind packet,
		MuiApplicationInputPacketField field, out uint offset, out uint size)
	{
		size = 0;
		switch (packet)
		{
			case MuiApplicationInputPacketKind.ReturnId:
				size = MuiApplicationReturnIdMessage.Size;
				if (field == MuiApplicationInputPacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				if (field == MuiApplicationInputPacketField.ReturnId)
				{
					offset = 4;
					return true;
				}
				break;
			case MuiApplicationInputPacketKind.Input:
				size = MuiApplicationInputMessage.Size;
				if (field == MuiApplicationInputPacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				if (field == MuiApplicationInputPacketField.SignalStorage)
				{
					offset = 4;
					return true;
				}
				break;
			case MuiApplicationInputPacketKind.InputHandler:
				size = MuiApplicationInputHandlerMessage.Size;
				if (field == MuiApplicationInputPacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				if (field == MuiApplicationInputPacketField.Handler)
				{
					offset = 4;
					return true;
				}
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationInputPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Packet, cursor.Field, out var offset,
			out var packetSize) || cursor.Message.IsNull ||
			cursor.Message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Message, packetSize)) return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiApplicationInputPacketKind packet,
		MuiApplicationInputPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationInputPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiApplicationInputPacketKind packet,
		MuiApplicationInputPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationInputPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationInputPacketCodec
{
	internal static bool TryReadReturnId<TPlatform>(ref TPlatform platform,
		APTR address, uint method, out MuiApplicationReturnIdMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationInputPacketFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiApplicationInputPacketKind.ReturnId,
			MuiApplicationInputPacketField.MethodId, out var methodId) ||
			methodId != method ||
			method != MuiApplicationDispatcher.ApplicationReturnIdMethod ||
			!MuiApplicationInputPacketFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationInputPacketKind.ReturnId,
				MuiApplicationInputPacketField.ReturnId, out value.ReturnId))
			return false;
		value.MethodId = methodId;
		return true;
	}

	internal static bool TryReadInput<TPlatform>(ref TPlatform platform,
		APTR address, uint method, out MuiApplicationInputMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationInputPacketFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiApplicationInputPacketKind.Input,
			MuiApplicationInputPacketField.MethodId, out var methodId) ||
			methodId != method ||
			(method != MuiApplicationDispatcher.ApplicationInputMethod &&
			 method != MuiApplicationDispatcher.ApplicationNewInputMethod) ||
			!MuiApplicationInputPacketFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationInputPacketKind.Input,
				MuiApplicationInputPacketField.SignalStorage,
				out value.SignalStorage))
			return false;
		value.MethodId = methodId;
		return true;
	}

	internal static bool TryReadInputBuffered<TPlatform>(
		ref TPlatform platform, APTR address, uint method,
		out MuiApplicationInputBufferedMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, address,
			out var header) || header.MethodId != method ||
			method != MuiApplicationDispatcher.ApplicationInputBufferedMethod ||
			!platform.IsMapped(address, MuiApplicationInputBufferedMessage.Size))
			return false;
		value.MethodId = header.MethodId;
		return true;
	}

	internal static bool TryReadInputHandler<TPlatform>(
		ref TPlatform platform, APTR address, uint method,
		out MuiApplicationInputHandlerMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationInputPacketFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiApplicationInputPacketKind.InputHandler,
			MuiApplicationInputPacketField.MethodId, out var methodId) ||
			methodId != method ||
			(method != MuiApplicationDispatcher.AddInputHandlerMethod &&
			 method != MuiApplicationDispatcher.RemoveInputHandlerMethod) ||
			!MuiApplicationInputPacketFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationInputPacketKind.InputHandler,
				MuiApplicationInputPacketField.Handler, out value.Handler))
			return false;
		value.MethodId = methodId;
		return true;
	}
}

internal static class MuiApplicationQueuePacketCodec
{
	internal struct QueuePacketAddress
	{
		public APTR Address;
		public uint Method;
	}

	internal static bool TryReadPush<TPlatform>(ref TPlatform platform,
		ref QueuePacketAddress request,
		out MuiApplicationPushMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationQueuePacketFieldCursorCodec.TryReadUInt32(
			ref platform, request.Address,
			MuiApplicationQueuePacketKind.PushMethod,
			MuiApplicationQueuePacketField.MethodId, out var methodId) ||
			methodId != request.Method ||
			!MuiApplicationQueuePacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiApplicationQueuePacketKind.PushMethod,
				MuiApplicationQueuePacketField.Destination, out value.Destination) ||
			!MuiApplicationQueuePacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiApplicationQueuePacketKind.PushMethod,
				MuiApplicationQueuePacketField.Count, out value.Count)) return false;
		value.MethodId = methodId;
		return true;
	}

	internal static bool TryReadUnpush<TPlatform>(ref TPlatform platform,
		ref QueuePacketAddress request,
		out MuiApplicationUnpushMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationQueuePacketFieldCursorCodec.TryReadUInt32(
			ref platform, request.Address,
			MuiApplicationQueuePacketKind.UnpushMethod,
			MuiApplicationQueuePacketField.MethodId, out var methodId) ||
			methodId != request.Method ||
			!MuiApplicationQueuePacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiApplicationQueuePacketKind.UnpushMethod,
				MuiApplicationQueuePacketField.TargetObject, out value.TargetObject) ||
			!MuiApplicationQueuePacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiApplicationQueuePacketKind.UnpushMethod,
				MuiApplicationQueuePacketField.MethodIdSelector,
				out value.MethodIdSelector) ||
			!MuiApplicationQueuePacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiApplicationQueuePacketKind.UnpushMethod,
				MuiApplicationQueuePacketField.Method, out value.Method)) return false;
		value.MethodId = methodId;
		return true;
	}

	internal static bool TryGetParameters<TPlatform>(ref TPlatform platform,
		APTR address, uint parameterCount, out APTR parameters)
		where TPlatform : struct, IMuiGuestMemory
	{
		parameters = APTR.Null;
		if (address.IsNull || parameterCount == 0 ||
			parameterCount > MuiApplicationPushMethodMessage.MaximumParameterCount ||
			!platform.IsMapped(address, MuiApplicationPushMethodMessage.Size))
			return false;
		var cursor = default(MuiApplicationPushMethodParameterCursor);
		cursor.Message = address;
		if (!MuiApplicationPushMethodParameterCursorCodec.TryGetEntry(
			ref platform, cursor, out var tail)) return false;
		var parameterBytes = parameterCount *
			MuiApplicationPushMethodParameter.Size;
		if (tail.Raw > uint.MaxValue - parameterBytes ||
			!platform.IsMapped(tail, parameterBytes)) return false;
		parameters = tail;
		return true;
	}
}

internal enum MuiApplicationPresentationPacketKind : byte
{
	ShowHelp,
	AboutMui,
}

internal enum MuiApplicationPresentationPacketField : byte
{
	MethodId,
	ReferenceWindow,
	HelpFile,
	Node,
	Line,
}

// Named view of the fixed Application presentation packet records. Packet
// kind selects the complete guest struct; numeric offsets remain confined to
// this ABI resolver.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationPresentationPacketFieldCursor
{
	internal APTR Message;
	internal MuiApplicationPresentationPacketKind Packet;
	internal MuiApplicationPresentationPacketField Field;
}

internal static class MuiApplicationPresentationPacketFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationPresentationPacketKind packet,
		MuiApplicationPresentationPacketField field, out uint offset,
		out uint size)
	{
		size = 0;
		switch (packet)
		{
			case MuiApplicationPresentationPacketKind.ShowHelp:
				size = MuiApplicationShowHelpMessage.Size;
				if (field == MuiApplicationPresentationPacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				if (field == MuiApplicationPresentationPacketField.ReferenceWindow)
				{
					offset = 4;
					return true;
				}
				if (field == MuiApplicationPresentationPacketField.HelpFile)
				{
					offset = 8;
					return true;
				}
				if (field == MuiApplicationPresentationPacketField.Node)
				{
					offset = 12;
					return true;
				}
				if (field == MuiApplicationPresentationPacketField.Line)
				{
					offset = 16;
					return true;
				}
				break;
			case MuiApplicationPresentationPacketKind.AboutMui:
				size = MuiApplicationAboutMuiMessage.Size;
				if (field == MuiApplicationPresentationPacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				if (field == MuiApplicationPresentationPacketField.ReferenceWindow)
				{
					offset = 4;
					return true;
				}
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationPresentationPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Packet, cursor.Field, out var offset,
			out var packetSize) || cursor.Message.IsNull ||
			cursor.Message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Message, packetSize)) return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiApplicationPresentationPacketKind packet,
		MuiApplicationPresentationPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationPresentationPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiApplicationPresentationPacketKind packet,
		MuiApplicationPresentationPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationPresentationPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationPresentationPacketCodec
{
	internal struct PresentationPacketAddress
	{
		public APTR Address;
		public uint Method;
	}

	internal static bool TryReadShowHelp<TPlatform>(ref TPlatform platform,
		ref PresentationPacketAddress request,
		out MuiApplicationShowHelpMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationPresentationPacketFieldCursorCodec.TryReadUInt32(
			ref platform, request.Address,
			MuiApplicationPresentationPacketKind.ShowHelp,
			MuiApplicationPresentationPacketField.MethodId, out var methodId) ||
			methodId != request.Method ||
			!MuiApplicationPresentationPacketFieldCursorCodec.TryReadUInt32(
				ref platform, request.Address,
				MuiApplicationPresentationPacketKind.ShowHelp,
				MuiApplicationPresentationPacketField.ReferenceWindow,
				out value.ReferenceWindow) ||
			!MuiApplicationPresentationPacketFieldCursorCodec.TryReadUInt32(
				ref platform, request.Address,
				MuiApplicationPresentationPacketKind.ShowHelp,
				MuiApplicationPresentationPacketField.HelpFile,
				out value.HelpFile) ||
			!MuiApplicationPresentationPacketFieldCursorCodec.TryReadUInt32(
				ref platform, request.Address,
				MuiApplicationPresentationPacketKind.ShowHelp,
				MuiApplicationPresentationPacketField.Node, out value.Node) ||
			!MuiApplicationPresentationPacketFieldCursorCodec.TryReadUInt32(
				ref platform, request.Address,
				MuiApplicationPresentationPacketKind.ShowHelp,
				MuiApplicationPresentationPacketField.Line, out value.Line)) return false;
		value.MethodId = methodId;
		return true;
	}

	internal static bool TryReadAboutMui<TPlatform>(ref TPlatform platform,
		ref PresentationPacketAddress request,
		out MuiApplicationAboutMuiMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationPresentationPacketFieldCursorCodec.TryReadUInt32(
			ref platform, request.Address,
			MuiApplicationPresentationPacketKind.AboutMui,
			MuiApplicationPresentationPacketField.MethodId, out var methodId) ||
			methodId != request.Method ||
			!MuiApplicationPresentationPacketFieldCursorCodec.TryReadUInt32(
				ref platform, request.Address,
				MuiApplicationPresentationPacketKind.AboutMui,
				MuiApplicationPresentationPacketField.ReferenceWindow,
				out value.ReferenceWindow)) return false;
		value.MethodId = methodId;
		return true;
	}
}

internal enum MuiApplicationSettingsPacketKind : byte
{
	SetConfigItem,
	OpenConfigWindow,
	BuildSettingsPanel,
	SettingsIo,
}

internal enum MuiApplicationSettingsPacketField : byte
{
	MethodId,
	Item,
	Data,
	Flags,
	ClassId,
	Number,
	Name,
}

// Named view of the fixed Application settings packet records. Packet kind
// selects the complete guest struct; numeric offsets remain confined to this
// ABI resolver.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationSettingsPacketFieldCursor
{
	internal APTR Message;
	internal MuiApplicationSettingsPacketKind Packet;
	internal MuiApplicationSettingsPacketField Field;
}

internal static class MuiApplicationSettingsPacketFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationSettingsPacketKind packet,
		MuiApplicationSettingsPacketField field, out uint offset,
		out uint size)
	{
		size = 0;
		switch (packet)
		{
			case MuiApplicationSettingsPacketKind.SetConfigItem:
				size = MuiApplicationSetConfigItemMessage.Size;
				if (field == MuiApplicationSettingsPacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				if (field == MuiApplicationSettingsPacketField.Item)
				{
					offset = 4;
					return true;
				}
				if (field == MuiApplicationSettingsPacketField.Data)
				{
					offset = 8;
					return true;
				}
				break;
			case MuiApplicationSettingsPacketKind.OpenConfigWindow:
				size = MuiApplicationOpenConfigWindowMessage.Size;
				if (field == MuiApplicationSettingsPacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				if (field == MuiApplicationSettingsPacketField.Flags)
				{
					offset = 4;
					return true;
				}
				if (field == MuiApplicationSettingsPacketField.ClassId)
				{
					offset = 8;
					return true;
				}
				break;
			case MuiApplicationSettingsPacketKind.BuildSettingsPanel:
				size = MuiApplicationBuildSettingsPanelMessage.Size;
				if (field == MuiApplicationSettingsPacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				if (field == MuiApplicationSettingsPacketField.Number)
				{
					offset = 4;
					return true;
				}
				break;
			case MuiApplicationSettingsPacketKind.SettingsIo:
				size = MuiApplicationSettingsIoMessage.Size;
				if (field == MuiApplicationSettingsPacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				if (field == MuiApplicationSettingsPacketField.Name)
				{
					offset = 4;
					return true;
				}
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationSettingsPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Packet, cursor.Field, out var offset,
			out var packetSize) || cursor.Message.IsNull ||
			cursor.Message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Message, packetSize)) return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiApplicationSettingsPacketKind packet,
		MuiApplicationSettingsPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationSettingsPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiApplicationSettingsPacketKind packet,
		MuiApplicationSettingsPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationSettingsPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationSettingsPacketCodec
{
	internal struct SettingsPacketAddress
	{
		public APTR Address;
		public uint Method;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadSetConfigItem<TPlatform>(
		ref TPlatform platform, ref SettingsPacketAddress request,
		out MuiApplicationSetConfigItemMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationSettingsPacketFieldCursorCodec.TryReadUInt32(
			ref platform, request.Address,
			MuiApplicationSettingsPacketKind.SetConfigItem,
			MuiApplicationSettingsPacketField.MethodId, out var methodId) ||
			methodId != request.Method ||
			!MuiApplicationSettingsPacketFieldCursorCodec.TryReadUInt32(
				ref platform, request.Address,
				MuiApplicationSettingsPacketKind.SetConfigItem,
				MuiApplicationSettingsPacketField.Item, out value.Item) ||
			!MuiApplicationSettingsPacketFieldCursorCodec.TryReadUInt32(
				ref platform, request.Address,
				MuiApplicationSettingsPacketKind.SetConfigItem,
				MuiApplicationSettingsPacketField.Data, out value.Data)) return false;
		value.MethodId = methodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadOpenConfigWindow<TPlatform>(
		ref TPlatform platform, ref SettingsPacketAddress request,
		out MuiApplicationOpenConfigWindowMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationSettingsPacketFieldCursorCodec.TryReadUInt32(
			ref platform, request.Address,
			MuiApplicationSettingsPacketKind.OpenConfigWindow,
			MuiApplicationSettingsPacketField.MethodId, out var methodId) ||
			methodId != request.Method ||
			!MuiApplicationSettingsPacketFieldCursorCodec.TryReadUInt32(
				ref platform, request.Address,
				MuiApplicationSettingsPacketKind.OpenConfigWindow,
				MuiApplicationSettingsPacketField.Flags, out value.Flags) ||
			!MuiApplicationSettingsPacketFieldCursorCodec.TryReadUInt32(
				ref platform, request.Address,
				MuiApplicationSettingsPacketKind.OpenConfigWindow,
				MuiApplicationSettingsPacketField.ClassId, out value.ClassId)) return false;
		value.MethodId = methodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadBuildSettingsPanel<TPlatform>(
		ref TPlatform platform, ref SettingsPacketAddress request,
		out MuiApplicationBuildSettingsPanelMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationSettingsPacketFieldCursorCodec.TryReadUInt32(
			ref platform, request.Address,
			MuiApplicationSettingsPacketKind.BuildSettingsPanel,
			MuiApplicationSettingsPacketField.MethodId, out var methodId) ||
			methodId != request.Method ||
			!MuiApplicationSettingsPacketFieldCursorCodec.TryReadUInt32(
				ref platform, request.Address,
				MuiApplicationSettingsPacketKind.BuildSettingsPanel,
				MuiApplicationSettingsPacketField.Number, out value.Number)) return false;
		value.MethodId = methodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadSettingsIo<TPlatform>(
		ref TPlatform platform, ref SettingsPacketAddress request,
		out MuiApplicationSettingsIoMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationSettingsPacketFieldCursorCodec.TryReadUInt32(
			ref platform, request.Address,
			MuiApplicationSettingsPacketKind.SettingsIo,
			MuiApplicationSettingsPacketField.MethodId, out var methodId) ||
			methodId != request.Method ||
			!MuiApplicationSettingsPacketFieldCursorCodec.TryReadUInt32(
				ref platform, request.Address,
				MuiApplicationSettingsPacketKind.SettingsIo,
				MuiApplicationSettingsPacketField.Name, out value.Name)) return false;
		value.MethodId = methodId;
		return true;
	}
}

internal enum MuiWindowCycleChainPacketField : byte
{
	MethodId,
	FirstObject,
}

// Named view of the fixed SetCycleChain header. The inline object-vector tail
// remains owned by MuiWindowCycleChainInlineVectorCursor.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowCycleChainPacketFieldCursor
{
	internal APTR Message;
	internal MuiWindowCycleChainPacketField Field;
}

internal static class MuiWindowCycleChainPacketFieldCursorCodec
{
	private static bool TryResolve(MuiWindowCycleChainPacketField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiWindowCycleChainPacketField.MethodId:
				offset = 0;
				return true;
			case MuiWindowCycleChainPacketField.FirstObject:
				offset = 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowCycleChainPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Message.IsNull ||
			cursor.Message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Message, MuiWindowCycleChainMessage.Size))
			return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiWindowCycleChainPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiWindowCycleChainPacketFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiWindowCycleChainPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiWindowCycleChainPacketFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiWindowCycleChainPacketCodec
{
	internal struct CycleChainPacketAddress
	{
		public APTR Address;
		public uint Method;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		ref CycleChainPacketAddress request,
		out MuiWindowCycleChainMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiWindowCycleChainPacketFieldCursorCodec.TryReadUInt32(ref platform,
			request.Address, MuiWindowCycleChainPacketField.MethodId,
			out var methodId) || methodId != request.Method ||
			!MuiWindowCycleChainPacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiWindowCycleChainPacketField.FirstObject,
				out value.FirstObject) || request.Address.Raw > uint.MaxValue -
				MuiWindowCycleChainMessage.VectorOffset) return false;
		value.MethodId = methodId;
		return true;
	}

	internal static bool TryGetVector<TPlatform>(ref TPlatform platform,
		APTR address, out APTR vector)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiWindowCycleChainInlineVectorCursor);
		cursor.Message = address;
		return MuiWindowCycleChainInlineVectorCodec.TryGetEntry(ref platform,
			cursor, out vector);
	}
}

internal enum MuiApplicationMethodPacketKind : byte
{
	ConfigId,
	CheckRefresh,
	Loop,
	WindowMethod,
	Snapshot,
}

internal enum MuiApplicationMethodPacketField : byte
{
	MethodId,
	ConfigId,
	Flags,
}

// Named view of the fixed Application/window method records. Packet kind
// selects the complete guest struct; numeric offsets remain confined to this
// ABI resolver.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationMethodPacketFieldCursor
{
	internal APTR Message;
	internal MuiApplicationMethodPacketKind Packet;
	internal MuiApplicationMethodPacketField Field;
}

internal static class MuiApplicationMethodPacketFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationMethodPacketKind packet,
		MuiApplicationMethodPacketField field, out uint offset,
		out uint size)
	{
		size = 0;
		switch (packet)
		{
			case MuiApplicationMethodPacketKind.ConfigId:
				size = MuiApplicationConfigIdMessage.Size;
				if (field == MuiApplicationMethodPacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				if (field == MuiApplicationMethodPacketField.ConfigId)
				{
					offset = 4;
					return true;
				}
				break;
			case MuiApplicationMethodPacketKind.CheckRefresh:
				size = MuiApplicationCheckRefreshMessage.Size;
				if (field == MuiApplicationMethodPacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				break;
			case MuiApplicationMethodPacketKind.Loop:
				size = MuiApplicationLoopMessage.Size;
				if (field == MuiApplicationMethodPacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				break;
			case MuiApplicationMethodPacketKind.WindowMethod:
				size = MuiWindowMethodMessage.Size;
				if (field == MuiApplicationMethodPacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				break;
			case MuiApplicationMethodPacketKind.Snapshot:
				size = MuiWindowSnapshotMessage.Size;
				if (field == MuiApplicationMethodPacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				if (field == MuiApplicationMethodPacketField.Flags)
				{
					offset = 4;
					return true;
				}
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationMethodPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Packet, cursor.Field, out var offset,
			out var packetSize) || cursor.Message.IsNull ||
			cursor.Message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Message, packetSize)) return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiApplicationMethodPacketKind packet,
		MuiApplicationMethodPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationMethodPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiApplicationMethodPacketKind packet,
		MuiApplicationMethodPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationMethodPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationMethodPacketCodec
{
	internal struct MethodPacketAddress
	{
		public APTR Address;
		public uint Method;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadConfigId<TPlatform>(ref TPlatform platform,
		ref MethodPacketAddress request, out MuiApplicationConfigIdMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationMethodPacketFieldCursorCodec.TryReadUInt32(
			ref platform, request.Address,
			MuiApplicationMethodPacketKind.ConfigId,
			MuiApplicationMethodPacketField.MethodId, out var methodId) ||
			methodId != request.Method ||
			!MuiApplicationMethodPacketFieldCursorCodec.TryReadUInt32(
				ref platform, request.Address,
				MuiApplicationMethodPacketKind.ConfigId,
				MuiApplicationMethodPacketField.ConfigId, out value.ConfigId)) return false;
		value.MethodId = methodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadCheckRefresh<TPlatform>(
		ref TPlatform platform, ref MethodPacketAddress request,
		out MuiApplicationCheckRefreshMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationMethodPacketFieldCursorCodec.TryReadUInt32(
			ref platform, request.Address,
			MuiApplicationMethodPacketKind.CheckRefresh,
			MuiApplicationMethodPacketField.MethodId, out var methodId) ||
			methodId != request.Method) return false;
		value.MethodId = methodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadLoop<TPlatform>(ref TPlatform platform,
		ref MethodPacketAddress request, out MuiApplicationLoopMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationMethodPacketFieldCursorCodec.TryReadUInt32(
			ref platform, request.Address,
			MuiApplicationMethodPacketKind.Loop,
			MuiApplicationMethodPacketField.MethodId, out var methodId) ||
			methodId != request.Method) return false;
		value.MethodId = methodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadWindowMethod<TPlatform>(ref TPlatform platform,
		ref MethodPacketAddress request, out MuiWindowMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationMethodPacketFieldCursorCodec.TryReadUInt32(
			ref platform, request.Address,
			MuiApplicationMethodPacketKind.WindowMethod,
			MuiApplicationMethodPacketField.MethodId, out var methodId) ||
			methodId != request.Method) return false;
		value.MethodId = methodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadSnapshot<TPlatform>(ref TPlatform platform,
		ref MethodPacketAddress request, out MuiWindowSnapshotMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationMethodPacketFieldCursorCodec.TryReadUInt32(
			ref platform, request.Address,
			MuiApplicationMethodPacketKind.Snapshot,
			MuiApplicationMethodPacketField.MethodId, out var methodId) ||
			methodId != request.Method ||
			!MuiApplicationMethodPacketFieldCursorCodec.TryReadUInt32(
				ref platform, request.Address,
				MuiApplicationMethodPacketKind.Snapshot,
				MuiApplicationMethodPacketField.Flags, out value.Flags)) return false;
		value.MethodId = methodId;
		return true;
	}
}

internal enum MuiApplicationMenuPacketKind : byte
{
	ApplicationQuery,
	ApplicationSet,
	WindowQuery,
	WindowSet,
}

internal enum MuiApplicationMenuPacketField : byte
{
	MethodId,
	MenuId,
	State,
}

// Named view of the fixed Application/Window menu query and set records.
// Packet kind selects the complete guest struct; numeric offsets remain
// confined to this ABI resolver.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationMenuPacketFieldCursor
{
	internal APTR Message;
	internal MuiApplicationMenuPacketKind Packet;
	internal MuiApplicationMenuPacketField Field;
}

internal static class MuiApplicationMenuPacketFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationMenuPacketKind packet,
		MuiApplicationMenuPacketField field, out uint offset, out uint size)
	{
		size = 0;
		switch (packet)
		{
			case MuiApplicationMenuPacketKind.ApplicationQuery:
				size = MuiApplicationMenuQueryMessage.Size;
				if (field == MuiApplicationMenuPacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				if (field == MuiApplicationMenuPacketField.MenuId)
				{
					offset = 4;
					return true;
				}
				break;
			case MuiApplicationMenuPacketKind.ApplicationSet:
				size = MuiApplicationMenuSetMessage.Size;
				if (field == MuiApplicationMenuPacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				if (field == MuiApplicationMenuPacketField.MenuId)
				{
					offset = 4;
					return true;
				}
				if (field == MuiApplicationMenuPacketField.State)
				{
					offset = 8;
					return true;
				}
				break;
			case MuiApplicationMenuPacketKind.WindowQuery:
				size = MuiWindowMenuQueryMessage.Size;
				if (field == MuiApplicationMenuPacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				if (field == MuiApplicationMenuPacketField.MenuId)
				{
					offset = 4;
					return true;
				}
				break;
			case MuiApplicationMenuPacketKind.WindowSet:
				size = MuiWindowMenuSetMessage.Size;
				if (field == MuiApplicationMenuPacketField.MethodId)
				{
					offset = 0;
					return true;
				}
				if (field == MuiApplicationMenuPacketField.MenuId)
				{
					offset = 4;
					return true;
				}
				if (field == MuiApplicationMenuPacketField.State)
				{
					offset = 8;
					return true;
				}
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationMenuPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Packet, cursor.Field, out var offset,
			out var packetSize) || cursor.Message.IsNull ||
			cursor.Message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Message, packetSize)) return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiApplicationMenuPacketKind packet,
		MuiApplicationMenuPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationMenuPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiApplicationMenuPacketKind packet,
		MuiApplicationMenuPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationMenuPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal enum MuiWindowEventHandlerPacketKind : byte
{
	Add,
	Remove,
}

internal enum MuiWindowEventHandlerPacketField : byte
{
	MethodId,
	Handler,
}

// Named view of the fixed Window Add/RemoveEventHandler packet. Numeric
// offsets remain confined to this ABI resolver.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowEventHandlerPacketFieldCursor
{
	internal APTR Message;
	internal MuiWindowEventHandlerPacketKind Packet;
	internal MuiWindowEventHandlerPacketField Field;
}

internal static class MuiWindowEventHandlerPacketFieldCursorCodec
{
	private static bool TryResolve(MuiWindowEventHandlerPacketKind packet,
		MuiWindowEventHandlerPacketField field, out uint offset)
	{
		if (packet == MuiWindowEventHandlerPacketKind.Add ||
			packet == MuiWindowEventHandlerPacketKind.Remove)
		{
			if (field == MuiWindowEventHandlerPacketField.MethodId)
			{
				offset = 0;
				return true;
			}
			if (field == MuiWindowEventHandlerPacketField.Handler)
			{
				offset = 4;
				return true;
			}
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowEventHandlerPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Packet, cursor.Field, out var offset) ||
			cursor.Message.IsNull || cursor.Message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Message, MuiWindowEventHandlerMessage.Size))
			return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiWindowEventHandlerPacketKind packet,
		MuiWindowEventHandlerPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiWindowEventHandlerPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiWindowEventHandlerPacketKind packet,
		MuiWindowEventHandlerPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiWindowEventHandlerPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationMenuPacketCodec
{
	internal struct MenuPacketAddress
	{
		public APTR Address;
		public uint Method;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadApplicationQuery<TPlatform>(
		ref TPlatform platform, ref MenuPacketAddress request,
		out MuiApplicationMenuQueryMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationMenuPacketFieldCursorCodec.TryReadUInt32(
			ref platform, request.Address,
			MuiApplicationMenuPacketKind.ApplicationQuery,
			MuiApplicationMenuPacketField.MethodId, out var methodId) ||
			methodId != request.Method ||
			(request.Method != MuiApplicationDispatcher.ApplicationGetMenuCheckMethod &&
			 request.Method != MuiApplicationDispatcher.ApplicationGetMenuStateMethod) ||
			!MuiApplicationMenuPacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiApplicationMenuPacketKind.ApplicationQuery,
				MuiApplicationMenuPacketField.MenuId, out value.MenuId))
			return false;
		value.MethodId = methodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadApplicationSet<TPlatform>(
		ref TPlatform platform, ref MenuPacketAddress request,
		out MuiApplicationMenuSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationMenuPacketFieldCursorCodec.TryReadUInt32(
			ref platform, request.Address,
			MuiApplicationMenuPacketKind.ApplicationSet,
			MuiApplicationMenuPacketField.MethodId, out var methodId) ||
			methodId != request.Method ||
			(request.Method != MuiApplicationDispatcher.ApplicationSetMenuCheckMethod &&
			 request.Method != MuiApplicationDispatcher.ApplicationSetMenuStateMethod) ||
			!MuiApplicationMenuPacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiApplicationMenuPacketKind.ApplicationSet,
				MuiApplicationMenuPacketField.MenuId, out value.MenuId) ||
			!MuiApplicationMenuPacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiApplicationMenuPacketKind.ApplicationSet,
				MuiApplicationMenuPacketField.State, out value.State))
			return false;
		value.MethodId = methodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadWindowQuery<TPlatform>(
		ref TPlatform platform, ref MenuPacketAddress request,
		out MuiWindowMenuQueryMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationMenuPacketFieldCursorCodec.TryReadUInt32(
			ref platform, request.Address,
			MuiApplicationMenuPacketKind.WindowQuery,
			MuiApplicationMenuPacketField.MethodId, out var methodId) ||
			methodId != request.Method ||
			(request.Method != MuiApplicationDispatcher.WindowGetMenuCheckMethod &&
			 request.Method != MuiApplicationDispatcher.WindowGetMenuStateMethod) ||
			!MuiApplicationMenuPacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiApplicationMenuPacketKind.WindowQuery,
				MuiApplicationMenuPacketField.MenuId, out value.MenuId))
			return false;
		value.MethodId = methodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadWindowSet<TPlatform>(
		ref TPlatform platform, ref MenuPacketAddress request,
		out MuiWindowMenuSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationMenuPacketFieldCursorCodec.TryReadUInt32(
			ref platform, request.Address,
			MuiApplicationMenuPacketKind.WindowSet,
			MuiApplicationMenuPacketField.MethodId, out var methodId) ||
			methodId != request.Method ||
			(request.Method != MuiApplicationDispatcher.WindowSetMenuCheckMethod &&
			 request.Method != MuiApplicationDispatcher.WindowSetMenuStateMethod) ||
			!MuiApplicationMenuPacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiApplicationMenuPacketKind.WindowSet,
				MuiApplicationMenuPacketField.MenuId, out value.MenuId) ||
			!MuiApplicationMenuPacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiApplicationMenuPacketKind.WindowSet,
				MuiApplicationMenuPacketField.State, out value.State))
			return false;
		value.MethodId = methodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadWindowEventHandler<TPlatform>(
		ref TPlatform platform, ref MenuPacketAddress request,
		out MuiWindowEventHandlerMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		var packet = request.Method == MuiApplicationDispatcher.WindowAddEventHandlerMethod
			? MuiWindowEventHandlerPacketKind.Add
			: MuiWindowEventHandlerPacketKind.Remove;
		if (!MuiWindowEventHandlerPacketFieldCursorCodec.TryReadUInt32(
			ref platform, request.Address, packet,
			MuiWindowEventHandlerPacketField.MethodId, out var methodId) ||
			methodId != request.Method ||
			(request.Method != MuiApplicationDispatcher.WindowAddEventHandlerMethod &&
			 request.Method != MuiApplicationDispatcher.WindowRemoveEventHandlerMethod) ||
			!MuiWindowEventHandlerPacketFieldCursorCodec.TryReadUInt32(
				ref platform, request.Address, packet,
				MuiWindowEventHandlerPacketField.Handler, out value.Handler))
			return false;
		value.MethodId = methodId;
		return true;
	}
}

public static class MuiApplicationDispatcher
{
	internal const uint SetMethod = 0x8042549A;
	internal const uint NoNotifySetMethod = 0x8042216F;
	private const uint Set = SetMethod;
	private const uint NoNotifySet = NoNotifySetMethod;
	private const uint ApplicationAboutMUI = 0x8042D21D;
	private const uint ApplicationShowHelp = 0x80426479;
	private const uint ApplicationCheckRefresh = 0x80424D68;
	private const uint ApplicationDefaultConfigItem = 0x8042D934;
	private const uint ApplicationSetConfigItem = 0x80424A80;
	private const uint ApplicationOpenConfigWindow = 0x804299BA;
	private const uint ApplicationBuildSettingsPanel = 0x8042B58F;
	private const uint ApplicationLoad = 0x8042F90D;
	private const uint ApplicationSave = 0x804227EF;
	private const uint ApplicationIconified = 0x8042A07F;
	private const uint ApplicationActive = 0x804260AB;
	private const uint ApplicationDoubleStart = 0x80423BC6;
	private const uint ApplicationSingleTask = 0x8042A2C8;
	private const uint ApplicationForceQuit = 0x804257DF;
	private const uint ApplicationUseRexx = 0x80422387;
	private const uint ApplicationUseCommodities = 0x80425EE5;
	private const uint ApplicationDiskObject = 0x804235CB;
	private const uint ApplicationDropObject = 0x80421266;
	private const uint ApplicationMenustrip = 0x804252D9;
	private const uint ApplicationMenuAction = 0x80428961;
	private const uint ApplicationAuthor = 0x80424842;
	private const uint ApplicationBase = 0x8042E07A;
	private const uint ApplicationCopyright = 0x8042EF4D;
	private const uint ApplicationDescription = 0x80421FC6;
	private const uint ApplicationTitle = 0x804281B8;
	private const uint ApplicationVersion = 0x8042B33F;
	private const uint ApplicationHelpFile = 0x804293F4;
	private const uint ApplicationIconifyTitle = 0x80422CB8;
	private const uint ApplicationUseScreenNotify = 0x80420861;
	private const uint ApplicationWindow = 0x8042BFE0;
	private const uint ApplicationUsedClasses = 0x8042E9A7;
	private const uint ApplicationCommands = 0x80428648;
	private const uint ApplicationSleep = 0x80425711;
	private const uint WindowOpen = 0x80428AA0;
	private const uint WindowActiveObject = 0x80427925;
	private const uint WindowDefaultObject = 0x804294D7;
	private const uint WindowActivate = 0x80428D2F;
	private const uint WindowSleep = 0x8042E7DB;
	private const uint WindowDisableKeys = MuiWindowPublicCore.DisableKeys;
	private const uint WindowAppWindow = 0x804280CF;
	public const uint AddInputHandlerMethod = 0x8042F099;
	public const uint RemoveInputHandlerMethod = 0x8042E7AF;
	private const uint AddInputHandler = AddInputHandlerMethod;
	private const uint RemoveInputHandler = RemoveInputHandlerMethod;
	public const uint ApplicationInputMethod = 0x8042D0F5;
	public const uint ApplicationNewInputMethod = 0x80423BA6;
	private const uint ApplicationInput = ApplicationInputMethod;
	private const uint ApplicationNewInput = ApplicationNewInputMethod;
	public const uint ApplicationReturnIdMethod = 0x804276EF;
	private const uint ApplicationReturnId = ApplicationReturnIdMethod;
	public const uint ApplicationInputBufferedMethod = 0x80427E59;
	private const uint ApplicationInputBuffered = ApplicationInputBufferedMethod;
	private const uint ApplicationExecute = 0x804253F3;
	private const uint ApplicationRun = 0x90420103;
	private const uint ApplicationPushMethod = 0x80429EF8;
	private const uint ApplicationUnpushMethod = 0x804211DD;
	public const uint ApplicationGetMenuCheckMethod = 0x8042C0A7;
	public const uint ApplicationGetMenuStateMethod = 0x8042A58F;
	public const uint ApplicationSetMenuCheckMethod = 0x8042A707;
	public const uint ApplicationSetMenuStateMethod = 0x80428BEF;
	private const uint ApplicationGetMenuCheck = ApplicationGetMenuCheckMethod;
	private const uint ApplicationGetMenuState = ApplicationGetMenuStateMethod;
	private const uint ApplicationSetMenuCheck = ApplicationSetMenuCheckMethod;
	private const uint ApplicationSetMenuState = ApplicationSetMenuStateMethod;
	public const uint WindowAddEventHandlerMethod = 0x804203B7;
	public const uint WindowRemoveEventHandlerMethod = 0x8042679E;
	private const uint WindowAddEventHandler = WindowAddEventHandlerMethod;
	private const uint WindowRemoveEventHandler = WindowRemoveEventHandlerMethod;
	private const uint WindowSetup = 0x8042C34C;
	private const uint WindowCleanup = 0x8042AB26;
	private const uint WindowToBack = 0x8042152E;
	private const uint WindowToFront = 0x8042554F;
	private const uint WindowScreenToBack = 0x8042913D;
	private const uint WindowScreenToFront = 0x804227A4;
	private const uint WindowSetCycleChain = 0x80426510;
	private const uint WindowSnapshot = 0x8042945E;
	public const uint WindowSetMenuCheckMethod = 0x80422243;
	public const uint WindowSetMenuStateMethod = 0x80422B5E;
	public const uint WindowGetMenuCheckMethod = 0x80420414;
	public const uint WindowGetMenuStateMethod = 0x80420D2F;
	private const uint WindowSetMenuCheck = WindowSetMenuCheckMethod;
	private const uint WindowSetMenuState = WindowSetMenuStateMethod;
	private const uint WindowGetMenuCheck = WindowGetMenuCheckMethod;
	private const uint WindowGetMenuState = WindowGetMenuStateMethod;

	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message) where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		switch (method)
		{
			case ApplicationAboutMUI:
				if (!TryReadAboutMui(ref platform, message, method,
					out var aboutMuiPacket)) return 0;
				return MuiApplicationWindowCore.AboutMUI(ref platform, state, obj,
					APTR.FromPointer(aboutMuiPacket.ReferenceWindow)) ? 1u : 0u;
			case ApplicationShowHelp:
				if (!TryReadShowHelp(ref platform, message, method,
					out var showHelpPacket)) return 0;
				return MuiApplicationWindowCore.ShowHelp(ref platform, state, obj,
					APTR.FromPointer(showHelpPacket.ReferenceWindow),
					APTR.FromPointer(showHelpPacket.HelpFile),
					APTR.FromPointer(showHelpPacket.Node),
					unchecked((int)showHelpPacket.Line)) ? 1u : 0u;
			case ApplicationCheckRefresh:
				if (!TryReadCheckRefresh(ref platform, message, method,
					out _)) return 0;
				return MuiApplicationWindowCore.CheckRefresh(ref platform, state, obj)
					? 1u : 0u;
			case ApplicationDefaultConfigItem:
				if (!TryReadConfigId(ref platform, message, method,
					out var configPacket)) return 0;
				return MuiApplicationWindowCore.DefaultConfigItem(ref platform, state,
					obj, configPacket.ConfigId);
			case ApplicationSetConfigItem:
				if (!TryReadSetConfigItem(ref platform, message, method,
					out var setConfigPacket)) return 0;
				return MuiApplicationWindowCore.SetConfigItem(ref platform, state, obj,
					setConfigPacket.Item, APTR.FromPointer(setConfigPacket.Data))
					? 1u : 0u;
			case ApplicationOpenConfigWindow:
				if (!TryReadOpenConfigWindow(ref platform, message, method,
					out var openConfigPacket)) return 0;
				return MuiApplicationWindowCore.OpenConfigWindow(ref platform, state, obj,
					openConfigPacket.Flags,
					APTR.FromPointer(openConfigPacket.ClassId)) ? 1u : 0u;
			case ApplicationBuildSettingsPanel:
				if (!TryReadBuildSettingsPanel(ref platform, message, method,
					out var settingsPacket)) return 0;
				return MuiApplicationWindowCore.BuildSettingsPanel(ref platform, state,
					obj, settingsPacket.Number).Raw;
			case ApplicationLoad:
			case ApplicationSave:
				if (!TryReadSettingsIo(ref platform, message, method,
					out var settingsIoPacket)) return 0;
				return (method == ApplicationSave ?
					MuiApplicationWindowCore.SaveApplicationSettings(ref platform, state,
						obj, APTR.FromPointer(settingsIoPacket.Name)) :
					MuiApplicationWindowCore.LoadApplicationSettings(ref platform, state,
						obj, APTR.FromPointer(settingsIoPacket.Name))) ? 1u : 0u;
			case AddInputHandler:
			case RemoveInputHandler:
				if (!TryReadInputHandler(ref platform, message, method,
					out var handlerPacket)) return 0;
				return method == AddInputHandler ?
					(MuiApplicationWindowCore.AddInputHandler(ref platform, state, obj,
						APTR.FromPointer(handlerPacket.Handler)) ? 1u : 0u) :
					(MuiApplicationWindowCore.RemoveInputHandler(ref platform, state, obj,
						APTR.FromPointer(handlerPacket.Handler)) ? 1u : 0u);
			case ApplicationInput:
			case ApplicationNewInput:
				if (!TryReadInput(ref platform, message, method,
					out var inputPacket)) return 0;
				return MuiApplicationWindowCore.Input(ref platform, state, obj,
					APTR.FromPointer(inputPacket.SignalStorage));
			case ApplicationReturnId:
				if (!TryReadReturnId(ref platform, message, method,
					out var returnPacket)) return 0;
				return MuiApplicationWindowCore.ReturnId(ref platform, state, obj,
					returnPacket.ReturnId) ? 1u : 0u;
			case ApplicationPushMethod:
				if (!TryReadPushMethod(ref platform, message, method,
					out var pushPacket)) return 0;
				if (!MuiApplicationQueuePacketCodec.TryGetParameters(ref platform,
					message, pushPacket.Count, out var pushParameters)) return 0;
				return MuiApplicationWindowCore.PushMethod(ref platform, state, obj,
					APTR.FromPointer(pushPacket.Destination),
					unchecked((int)pushPacket.Count),
					pushParameters);
			case ApplicationUnpushMethod:
				if (!TryReadUnpushMethod(ref platform, message, method,
					out var unpushPacket)) return 0;
				return MuiApplicationWindowCore.UnpushMethod(ref platform, state, obj,
					APTR.FromPointer(unpushPacket.TargetObject),
					unpushPacket.MethodIdSelector, unpushPacket.Method);
			case ApplicationInputBuffered:
				if (!TryReadInputBuffered(ref platform, message, method,
					out _)) return 0;
				return MuiApplicationWindowCore.DispatchPushedMethod(ref platform,
					state, obj);
			case ApplicationExecute:
			case ApplicationRun:
				if (!TryReadApplicationLoop(ref platform, message, method,
					out _)) return 0;
				return MuiApplicationWindowCore.RunApplication(ref platform, state, obj,
					APTR.Null, APTR.Null);
			case ApplicationGetMenuCheck:
			case ApplicationGetMenuState:
				if (!TryReadMenuQuery(ref platform, message, method,
					out var menuQuery)) return 0;
				return MuiApplicationWindowCore.GetApplicationMenu(ref platform, state,
					obj, menuQuery.MenuId,
					method == ApplicationGetMenuCheck);
			case ApplicationSetMenuCheck:
				if (!TryReadMenuSet(ref platform, message, method,
					out var menuCheck)) return 0;
				return MuiApplicationWindowCore.SetApplicationMenu(ref platform, state,
					obj, menuCheck.MenuId, true, true, menuCheck.State != 0);
			case ApplicationSetMenuState:
				if (!TryReadMenuSet(ref platform, message, method,
					out var menuState)) return 0;
				return MuiApplicationWindowCore.SetApplicationMenu(ref platform, state,
					obj, menuState.MenuId, menuState.State != 0, false, false);
			case WindowAddEventHandler:
				if (!TryReadWindowEventHandler(ref platform, message, method,
					out var addEventHandler)) return 0;
				return MuiApplicationWindowCore.AddEventHandler(ref platform, state,
					obj, APTR.FromPointer(addEventHandler.Handler)) ? 1u : 0u;
			case WindowRemoveEventHandler:
				if (!TryReadWindowEventHandler(ref platform, message, method,
					out var removeEventHandler)) return 0;
				return MuiApplicationWindowCore.RemoveEventHandler(ref platform, state,
					obj, APTR.FromPointer(removeEventHandler.Handler)) ? 1u : 0u;
			case WindowSetup:
				if (!TryReadWindowMethod(ref platform, message, method,
					out _)) return 0;
				return MuiApplicationWindowCore.OpenWindow(ref platform, state, obj, 0) ?
					1u : 0u;
			case WindowCleanup:
				if (!TryReadWindowMethod(ref platform, message, method,
					out _)) return 0;
				return MuiApplicationWindowCore.CloseWindow(ref platform, state, obj) ?
					1u : 0u;
			case WindowToBack:
				if (!TryReadWindowMethod(ref platform, message, method,
					out _)) return 0;
				return MuiApplicationWindowCore.MoveWindow(ref platform, state, obj,
					false) ? 1u : 0u;
			case WindowToFront:
				if (!TryReadWindowMethod(ref platform, message, method,
					out _)) return 0;
				return MuiApplicationWindowCore.MoveWindow(ref platform, state, obj,
					true) ? 1u : 0u;
			case WindowScreenToBack:
				if (!TryReadWindowMethod(ref platform, message, method,
					out _)) return 0;
				return MuiApplicationWindowCore.MoveScreen(ref platform, state, obj,
					false) ? 1u : 0u;
			case WindowScreenToFront:
				if (!TryReadWindowMethod(ref platform, message, method,
					out _)) return 0;
				return MuiApplicationWindowCore.MoveScreen(ref platform, state, obj,
					true) ? 1u : 0u;
			case WindowSetCycleChain:
				if (!TryReadWindowCycleChain(ref platform, message, method,
					out _)) return 0;
				if (!MuiWindowCycleChainPacketCodec.TryGetVector(ref platform,
					message, out var cycleChainVector)) return 0;
				return MuiApplicationWindowCore.SetCycleChain(ref platform, state, obj,
					cycleChainVector) ? 1u : 0u;
			case WindowSnapshot:
				if (!TryReadWindowSnapshot(ref platform, message, method,
					out var windowSnapshot)) return 0;
				return MuiApplicationWindowCore.SnapshotWindow(ref platform, state, obj,
					windowSnapshot.Flags) ? 1u : 0u;
			case WindowGetMenuCheck:
			case WindowGetMenuState:
				if (!TryReadWindowMenuQuery(ref platform, message, method,
					out var windowMenuQuery)) return 0;
				return MuiApplicationWindowCore.GetMenu(ref platform, state, obj,
					windowMenuQuery.MenuId,
					method == WindowGetMenuCheck);
			case WindowSetMenuCheck:
				if (!TryReadWindowMenuSet(ref platform, message, method,
					out var windowMenuCheck)) return 0;
				return MuiApplicationWindowCore.SetMenu(ref platform, state, obj,
					windowMenuCheck.MenuId, true, true,
					windowMenuCheck.State != 0) ? 1u : 0u;
			case WindowSetMenuState:
				if (!TryReadWindowMenuSet(ref platform, message, method,
					out var windowMenuState)) return 0;
				return MuiApplicationWindowCore.SetMenu(ref platform, state, obj,
					windowMenuState.MenuId, windowMenuState.State != 0, false,
					false) ? 1u : 0u;
			case Set:
			case NoNotifySet:
				if (!TryReadSetAttribute(ref platform, message, method,
					out var setAttributePacket)) return 0;
				if (setAttributePacket.Attribute == ApplicationActive)
					return MuiApplicationWindowCore.SetApplicationActiveValue(
						ref platform, state, obj, setAttributePacket.Value) ? 1u : 0u;
				if (setAttributePacket.Attribute == ApplicationSingleTask)
					return MuiApplicationWindowCore.SetApplicationSingleTaskValue(
						ref platform, state, obj, setAttributePacket.Value) ? 1u : 0u;
				if (setAttributePacket.Attribute == ApplicationDoubleStart)
					return MuiApplicationWindowCore.SetApplicationDoubleStartValue(
						ref platform, state, obj, setAttributePacket.Value) ? 1u : 0u;
				if (setAttributePacket.Attribute == ApplicationForceQuit)
					return MuiApplicationWindowCore.SetApplicationForceQuitValue(
						ref platform, state, obj, setAttributePacket.Value) ? 1u : 0u;
				if (setAttributePacket.Attribute == ApplicationUseRexx)
					return MuiApplicationWindowCore.SetApplicationUseRexxValue(
						ref platform, state, obj, setAttributePacket.Value) ? 1u : 0u;
				if (setAttributePacket.Attribute == ApplicationUseCommodities)
					return MuiApplicationWindowCore.SetApplicationUseCommoditiesValue(
						ref platform, state, obj, setAttributePacket.Value) ? 1u : 0u;
				if (setAttributePacket.Attribute == ApplicationAuthor ||
					setAttributePacket.Attribute == ApplicationBase ||
					setAttributePacket.Attribute == ApplicationCopyright ||
					setAttributePacket.Attribute == ApplicationDescription ||
					setAttributePacket.Attribute == ApplicationTitle ||
					setAttributePacket.Attribute == ApplicationVersion)
					return MuiApplicationWindowCore.SetApplicationInitializerStringValue(
						ref platform, state, obj, setAttributePacket.Attribute,
						setAttributePacket.Value) ? 1u : 0u;
					if (setAttributePacket.Attribute == ApplicationHelpFile)
						return MuiApplicationWindowCore.SetApplicationHelpFileValue(
							ref platform, state, obj, setAttributePacket.Value) ? 1u : 0u;
					if (setAttributePacket.Attribute == ApplicationIconifyTitle)
						return MuiApplicationWindowCore.SetApplicationIconifyTitleValue(
							ref platform, state, obj, setAttributePacket.Value) ? 1u : 0u;
					if (setAttributePacket.Attribute == ApplicationUseScreenNotify)
						return MuiApplicationWindowCore.SetApplicationUseScreenNotifyValue(
							ref platform, state, obj, setAttributePacket.Value) ? 1u : 0u;
					if (setAttributePacket.Attribute == ApplicationDiskObject)
						return MuiApplicationWindowCore.SetApplicationDiskObjectValue(
							ref platform, state, obj, setAttributePacket.Value) ? 1u : 0u;
					if (setAttributePacket.Attribute == ApplicationDropObject)
						return MuiApplicationWindowCore.SetApplicationDropObjectValue(
							ref platform, state, obj, setAttributePacket.Value) ? 1u : 0u;
					if (setAttributePacket.Attribute == ApplicationMenustrip)
						return MuiApplicationWindowCore.SetApplicationMenustripValue(
							ref platform, state, obj, setAttributePacket.Value) ? 1u : 0u;
					if (setAttributePacket.Attribute == ApplicationMenuAction)
						return MuiApplicationWindowCore.SetApplicationMenuActionValue(
							ref platform, state, obj, setAttributePacket.Value) ? 1u : 0u;
					if (setAttributePacket.Attribute == ApplicationWindow)
						return MuiApplicationWindowCore.SetApplicationWindowValue(
							ref platform, state, obj, setAttributePacket.Value) ? 1u : 0u;
					if (setAttributePacket.Attribute == ApplicationUsedClasses)
						return MuiApplicationWindowCore.SetApplicationUsedClassesValue(
							ref platform, state, obj, setAttributePacket.Value) ? 1u : 0u;
					if (setAttributePacket.Attribute == ApplicationCommands)
						return MuiApplicationCommandsCore.SetApplicationCommandsValue(
							ref platform, state, obj, setAttributePacket.Value) ? 1u : 0u;
					if (setAttributePacket.Attribute == ApplicationIconified)
						return MuiApplicationWindowCore.SetIconified(ref platform, state, obj,
						setAttributePacket.Value != 0) ? 1u : 0u;
				if (setAttributePacket.Attribute == ApplicationSleep)
					return MuiApplicationWindowCore.SetApplicationSleepValue(ref platform,
						state, obj, setAttributePacket.Value) ? 1u : 0u;
				if (setAttributePacket.Attribute == WindowOpen)
					return MuiApplicationWindowCore.SetWindowOpenValue(ref platform,
						state, obj, setAttributePacket.Value) ? 1u : 0u;
				if (setAttributePacket.Attribute == WindowAppWindow)
					return MuiApplicationMessageCore.SetWindowAppWindowValue(
						ref platform, state, obj, setAttributePacket.Value) ? 1u : 0u;
				if (setAttributePacket.Attribute == WindowActiveObject)
					return MuiApplicationWindowCore.SetActiveObjectValue(ref platform,
					state, obj, setAttributePacket.Value) ? 1u : 0u;
				if (setAttributePacket.Attribute == WindowDefaultObject)
					return MuiApplicationWindowCore.SetDefaultObjectValue(ref platform,
					state, obj, APTR.FromPointer(setAttributePacket.Value)) ? 1u : 0u;
				if (setAttributePacket.Attribute == WindowActivate)
					return MuiApplicationWindowCore.SetActivateValue(ref platform,
					state, obj, setAttributePacket.Value) ? 1u : 0u;
				if (setAttributePacket.Attribute == WindowSleep)
					return MuiApplicationWindowCore.SetSleepValue(ref platform, state,
					obj, setAttributePacket.Value) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.Id)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.Id, setAttributePacket.Value, method == Set) ?
					1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.CloseRequest)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.CloseRequest, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.RootObject)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.RootObject, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.NoMenus)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.NoMenus, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.HasAlpha)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.HasAlpha, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.Opacity)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.Opacity, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.Title)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.Title, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.Screen)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.Screen, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.RefWindow)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.RefWindow, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.VisibleOnMaximize)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.VisibleOnMaximize, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.IsSubWindow)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.IsSubWindow, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.TabletMessages)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.TabletMessages, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.UseBottomBorderScroller)
					return MuiApplicationWindowCore.SetBorderScroller(ref platform,
					state, obj, MuiWindowPublicCore.UseBottomBorderScroller,
					setAttributePacket.Value, method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.UseLeftBorderScroller)
					return MuiApplicationWindowCore.SetBorderScroller(ref platform,
					state, obj, MuiWindowPublicCore.UseLeftBorderScroller,
					setAttributePacket.Value, method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.UseRightBorderScroller)
					return MuiApplicationWindowCore.SetBorderScroller(ref platform,
					state, obj, MuiWindowPublicCore.UseRightBorderScroller,
					setAttributePacket.Value, method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.AltHeight)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.AltHeight, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.AltWidth)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.AltWidth, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.AltLeftEdge)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.AltLeftEdge, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.AltTopEdge)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.AltTopEdge, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.Height)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.Height, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.Width)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.Width, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.LeftEdge)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.LeftEdge, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.TopEdge)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.TopEdge, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.CloseGadget)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.CloseGadget, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.DepthGadget)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.DepthGadget, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.DragBar)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.DragBar, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.SizeGadget)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.SizeGadget, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.SizeRight)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.SizeRight, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.AppWindow)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.AppWindow, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.Backdrop)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.Backdrop, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.Borderless)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.Borderless, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.PanelWindow)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.PanelWindow, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.Menustrip)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.Menustrip, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.FancyDrawing)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.FancyDrawing, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.MenuAction)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.MenuAction, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.NeedsMouseObject)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.NeedsMouseObject, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.ScreenTitle)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.ScreenTitle, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				if (setAttributePacket.Attribute == MuiWindowPublicCore.PublicScreen)
					return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiWindowPublicCore.PublicScreen, setAttributePacket.Value,
					method == Set) ? 1u : 0u;
				break;
		}
		return MuiLayoutDispatcher.Dispatch(ref platform, state, obj, message);
	}

	// Focused native-qualification seam for the MorphOS AboutMUI packet. The
	// public dispatcher above retains the complete Application method family;
	// this narrow entry keeps the independently qualified packet closure from
	// importing unrelated application/window methods.
	public static uint DispatchAboutMUI<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadAboutMui(ref platform, message, ApplicationAboutMUI,
			out var packet)) return 0;
		return MuiApplicationWindowCore.AboutMUI(ref platform, state, obj,
			APTR.FromPointer(packet.ReferenceWindow)) ? 1u : 0u;
	}

	// Focused native-qualification seam for the MorphOS ShowHelp packet. It
	// keeps guest C-string validation and the first-open-window sentinel in a
	// small closure independent of the broader Application dispatcher.
	public static uint DispatchShowHelp<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadShowHelp(ref platform, message, ApplicationShowHelp,
			out var packet)) return 0;
		return MuiApplicationWindowCore.ShowHelp(ref platform, state, obj,
			APTR.FromPointer(packet.ReferenceWindow),
			APTR.FromPointer(packet.HelpFile), APTR.FromPointer(packet.Node),
			unchecked((int)packet.Line)) ? 1u : 0u;
	}

	// Focused native-qualification seam for the zero-argument CheckRefresh
	// packet. It intentionally avoids importing the unrelated Application and
	// Window method families into this small native closure.
	public static uint DispatchCheckRefresh<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadCheckRefresh(ref platform, message, ApplicationCheckRefresh,
			out var packet)) return 0;
		return MuiApplicationWindowCore.CheckRefresh(ref platform, state, obj)
			? 1u : 0u;
	}

	// Focused native-qualification seam for the MorphOS DefaultConfigItem
	// override hook. The packet is exactly {MethodID, cfgid}.
	public static uint DispatchApplicationDefaultConfig<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadConfigId(ref platform, message, ApplicationDefaultConfigItem,
			out var packet)) return 0;
		return MuiApplicationWindowCore.DefaultConfigItem(ref platform, state, obj,
			packet.ConfigId);
	}

	// Focused native-qualification seam for the private MorphOS V11
	// SetConfigItem boundary. The exact frame is {MethodID, item, data}; the
	// core retains the opaque data pointer without interpreting PSI payloads.
	public static uint DispatchApplicationSetConfigItem<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadSetConfigItem(ref platform, message, ApplicationSetConfigItem,
			out var packet)) return 0;
		return MuiApplicationWindowCore.SetConfigItem(ref platform, state, obj,
			packet.Item, APTR.FromPointer(packet.Data)) ? 1u : 0u;
	}

	// Focused native-qualification seam for the MorphOS OpenConfigWindow
	// packet. The fixed frame is {MethodID, flags, classid}; the core performs
	// bounded guest-string validation and delegates presentation explicitly.
	public static uint DispatchApplicationConfig<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadOpenConfigWindow(ref platform, message,
			ApplicationOpenConfigWindow, out var packet)) return 0;
		return MuiApplicationWindowCore.OpenConfigWindow(ref platform, state, obj,
			packet.Flags, APTR.FromPointer(packet.ClassId)) ?
			1u : 0u;
	}

	// Focused native-qualification seam for the MorphOS
	// BuildSettingsPanel override hook. The packet is exactly
	// {MethodID, number}; the result is a guest MUI object or Null.
	public static uint DispatchApplicationSettings<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadBuildSettingsPanel(ref platform, message,
			ApplicationBuildSettingsPanel, out var packet)) return 0;
		return MuiApplicationWindowCore.BuildSettingsPanel(ref platform, state, obj,
			packet.Number).Raw;
	}

	// Focused native-qualification seam for MorphOS Application Save/Load.
	// Both packets are exactly {MethodID, name}; Null and -1 are the documented
	// ENV/ENVARC selectors and other names are bounded guest C strings.
	public static uint DispatchApplicationSettingsIO<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (!TryReadSettingsIo(ref platform, message, method,
			out var packet)) return 0;
		var name = APTR.FromPointer(packet.Name);
		if (method == ApplicationSave)
			return MuiApplicationWindowCore.SaveApplicationSettings(ref platform, state,
				obj, name) ? 1u : 0u;
		if (method == ApplicationLoad)
			return MuiApplicationWindowCore.LoadApplicationSettings(ref platform, state,
				obj, name) ? 1u : 0u;
		return 0;
	}

	// Focused native-qualification seam for the Application menu packet family.
	// It keeps first-match GetMenu and all-window SetMenu behavior out of the
	// unrelated Application/Window closure.
	public static uint DispatchApplicationMenu<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (method == ApplicationGetMenuCheck || method == ApplicationGetMenuState)
		{
			if (!TryReadMenuQuery(ref platform, message, method,
				out var query)) return 0;
			return MuiApplicationWindowCore.GetApplicationMenu(ref platform, state,
				obj, query.MenuId, method == ApplicationGetMenuCheck);
		}
		if (method == ApplicationSetMenuCheck || method == ApplicationSetMenuState)
		{
			if (!TryReadMenuSet(ref platform, message, method,
				out var set)) return 0;
			return MuiApplicationWindowCore.SetApplicationMenu(ref platform, state,
				obj, set.MenuId,
				method == ApplicationSetMenuState &&
					set.State != 0,
				method == ApplicationSetMenuCheck,
				method == ApplicationSetMenuCheck &&
					set.State != 0);
		}
		return 0;
	}

	// Focused native-qualification seam for the MorphOS PushMethod and
	// UnpushMethod packet family. Push returns the queue identifier; Unpush
	// treats each zero selector as a wildcard and removes all matches.
	public static uint DispatchApplicationQueue<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (method == ApplicationPushMethod)
		{
			if (!TryReadPushMethod(ref platform, message, method,
				out var pushPacket)) return 0;
			if (!MuiApplicationQueuePacketCodec.TryGetParameters(ref platform,
				message, pushPacket.Count, out var pushParameters)) return 0;
			return MuiApplicationWindowCore.PushMethod(ref platform, state, obj,
				APTR.FromPointer(pushPacket.Destination),
				unchecked((int)pushPacket.Count),
				pushParameters);
		}
		if (method == ApplicationUnpushMethod)
		{
			if (!TryReadUnpushMethod(ref platform, message, method,
				out var unpushPacket)) return 0;
			return MuiApplicationWindowCore.UnpushMethod(ref platform, state, obj,
				APTR.FromPointer(unpushPacket.TargetObject),
				unpushPacket.MethodIdSelector, unpushPacket.Method);
		}
		return 0;
	}

	// Focused native-qualification seam for the zero-argument Execute/Run
	// packet family. It drives the application scheduler loop and returns the
	// MorphOS ReturnID_Quit sentinel when the loop terminates normally.
	public static uint DispatchApplicationLoop<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (!TryReadApplicationLoop(ref platform, message, method,
			out _)) return 0;
		return MuiApplicationWindowCore.RunApplication(ref platform, state, obj,
			APTR.Null, APTR.Null);
	}

	// Focused native-qualification seam for the MorphOS ReturnID packet. The
	// fixed frame is decoded into a named struct before the queue is mutated or
	// the application task is signalled.
	public static uint DispatchApplicationReturnId<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadReturnId(ref platform, message, ApplicationReturnId,
			out var packet)) return 0;
		return MuiApplicationWindowCore.ReturnId(ref platform, state, obj,
			packet.ReturnId) ? 1u : 0u;
	}

	// Focused native-qualification seam for the MorphOS Input/NewInput packet
	// family. Both methods use the fixed `{MethodID, signal}` frame; the core
	// consumes one queued ReturnID first and otherwise publishes pending signals
	// through the caller-owned storage.
	public static uint DispatchApplicationInput<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (method != ApplicationInput && method != ApplicationNewInput ||
			!TryReadInput(ref platform, message, method, out var packet)) return 0;
		return MuiApplicationWindowCore.Input(ref platform, state, obj,
			APTR.FromPointer(packet.SignalStorage));
	}

	// Focused native-qualification seam for the exact zero-argument
	// MUIM_Application_InputBuffered packet. It dispatches one queued
	// PushMethod record and leaves an empty queue as a successful no-op result.
	public static uint DispatchApplicationInputBuffered<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadInputBuffered(ref platform, message,
			ApplicationInputBuffered, out _)) return 0;
		return MuiApplicationWindowCore.DispatchPushedMethod(ref platform,
			state, obj);
	}

	// Focused native-qualification seam for the MorphOS input-handler packet
	// pair. Add and Rem both carry the exact `{MethodID, ihnode}` frame and
	// mutate only the live application's guest-resident handler list.
	public static uint DispatchApplicationInputHandler<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if ((method != AddInputHandler && method != RemoveInputHandler) ||
			!TryReadInputHandler(ref platform, message, method,
				out var packet)) return 0;
		return method == AddInputHandler ?
			(MuiApplicationWindowCore.AddInputHandler(ref platform, state, obj,
				APTR.FromPointer(packet.Handler)) ? 1u : 0u) :
			(MuiApplicationWindowCore.RemoveInputHandler(ref platform, state, obj,
				APTR.FromPointer(packet.Handler)) ? 1u : 0u);
	}

	// Focused native-qualification seam for the four application menu packets.
	// Get methods use `{MethodID, MenuID}`; Set methods use
	// `{MethodID, MenuID, stat}` and preserve the obsolete distinction between
	// menu enabled state and checkmark state.
	public static uint DispatchApplicationMenuState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (method == ApplicationGetMenuCheck || method == ApplicationGetMenuState)
		{
			if (!TryReadMenuQuery(ref platform, message, method,
				out var query)) return 0;
			return MuiApplicationWindowCore.GetApplicationMenu(ref platform, state,
				obj, query.MenuId, method == ApplicationGetMenuCheck);
		}
		if ((method != ApplicationSetMenuCheck && method != ApplicationSetMenuState) ||
			!TryReadMenuSet(ref platform, message, method, out var set)) return 0;
		return MuiApplicationWindowCore.SetApplicationMenu(ref platform, state,
			obj, set.MenuId, method == ApplicationSetMenuState && set.State != 0,
			method == ApplicationSetMenuCheck,
			method == ApplicationSetMenuCheck && set.State != 0);
	}

	// Focused native-qualification seam for the four obsolete-but-ABI-visible
	// Window menu packets. Get methods use `{MethodID, MenuID}`; Set methods
	// use `{MethodID, MenuID, stat}` and preserve checkmark versus enabled state.
	public static uint DispatchWindowMenuState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (method == WindowGetMenuCheck || method == WindowGetMenuState)
		{
			if (!TryReadWindowMenuQuery(ref platform, message, method,
				out var query)) return 0;
			return MuiApplicationWindowCore.GetMenu(ref platform, state, obj,
				query.MenuId, method == WindowGetMenuCheck);
		}
		if ((method != WindowSetMenuCheck && method != WindowSetMenuState) ||
			!TryReadWindowMenuSet(ref platform, message, method, out var set))
			return 0;
		return MuiApplicationWindowCore.SetMenu(ref platform, state, obj,
			set.MenuId, method == WindowSetMenuState && set.State != 0,
			method == WindowSetMenuCheck,
			method == WindowSetMenuCheck && set.State != 0) ? 1u : 0u;
	}

	// Focused native-qualification seam for the MorphOS Window event-handler
	// packet pair. Both methods carry the exact `{MethodID, ehnode}` frame;
	// the handler remains guest-resident and is linked to the live window.
	public static uint DispatchWindowEventHandler<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if ((method != WindowAddEventHandler &&
			method != WindowRemoveEventHandler) ||
			!TryReadWindowEventHandler(ref platform, message, method,
				out var packet)) return 0;
		return method == WindowAddEventHandler ?
			(MuiApplicationWindowCore.AddEventHandler(ref platform, state, obj,
				APTR.FromPointer(packet.Handler)) ? 1u : 0u) :
			(MuiApplicationWindowCore.RemoveEventHandler(ref platform, state, obj,
				APTR.FromPointer(packet.Handler)) ? 1u : 0u);
	}

	// Focused native-qualification seam for the MorphOS Window Snapshot packet.
	// The fixed frame is {MethodID, flags}; the core validates the window ID,
	// accepted flags, and native settings capability.
	public static uint DispatchWindowSnapshot<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadWindowSnapshot(ref platform, message, WindowSnapshot,
			out var packet)) return 0;
		return MuiApplicationWindowCore.SnapshotWindow(ref platform, state, obj,
			packet.Flags) ? 1u : 0u;
	}

	// Focused native-qualification seam for the obsolete-but-ABI-visible
	// Window SetCycleChain packet. The core copies and validates the inline
	// Null-terminated MUI object vector in guest memory.
	public static uint DispatchWindowCycleChain<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadWindowCycleChain(ref platform, message, WindowSetCycleChain,
			out _)) return 0;
		if (!MuiWindowCycleChainPacketCodec.TryGetVector(ref platform, message,
			out var vector)) return 0;
		return MuiApplicationWindowCore.SetCycleChain(ref platform, state, obj,
			vector) ? 1u : 0u;
	}

	// Focused native-qualification seam for MUIA_Window_ActiveObject special
	// inputs carried by MUIM_Set. None, Next, and Prev are handled by the core;
	// spatial selectors remain an explicit unsupported result in this slice.
	public static uint DispatchWindowActiveObject<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set,
			out var packet) || packet.Attribute != WindowActiveObject) return 0;
		return MuiApplicationWindowCore.SetActiveObjectValue(ref platform, state,
			obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for MUIA_Window_DefaultObject. The
	// packet remains the named MUIM_Set record and the core validates the
	// window and target object before refreshing handler active state.
	public static uint DispatchWindowDefaultObject<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (method != Set && method != NoNotifySet) return 0;
		if (!TryReadSetAttribute(ref platform, message, method,
			out var packet) || packet.Attribute != WindowDefaultObject) return 0;
		return MuiApplicationWindowCore.SetDefaultObjectValue(ref platform, state,
			obj, APTR.FromPointer(packet.Value)) ? 1u : 0u;
	}

	// Focused native-qualification seam for MUIA_Window_Activate. FALSE is a
	// documented no-op; TRUE crosses the typed window capability and records
	// the state only after native activation succeeds.
	public static uint DispatchWindowActivate<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (method != Set && method != NoNotifySet) return 0;
		if (!TryReadSetAttribute(ref platform, message, method,
			out var packet) || packet.Attribute != WindowActivate) return 0;
		return MuiApplicationWindowCore.SetActivateValue(ref platform, state,
			obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for the nested MUIA_Window_Sleep
	// counter. The core owns the prior disabled state and event suppression.
	public static uint DispatchWindowSleep<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (method != Set && method != NoNotifySet) return 0;
		if (!TryReadSetAttribute(ref platform, message, method,
			out var packet) || packet.Attribute != WindowSleep) return 0;
		return MuiApplicationWindowCore.SetSleepValue(ref platform, state, obj,
			packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for the mutable ULONG keyboard mask.
	// The mask is retained as named Window state; event dispatch consumes the
	// same value before offering a preprocessed MUI key to handlers.
	public static uint DispatchWindowDisableKeys<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (method != Set && method != NoNotifySet) return 0;
		if (!TryReadSetAttribute(ref platform, message, method,
			out var packet) || packet.Attribute != WindowDisableKeys) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.DisableKeys, packet.Value, method == Set) ? 1u : 0u;
	}

	// Focused native-qualification seam for the mutable MUIA_Window_ID ULONG.
	// The public state route remains in MuiWindowPublicCore; this entry only
	// proves the named MUIM_Set packet reaches that route without an ABI offset.
	public static uint DispatchWindowId<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != MuiWindowPublicCore.Id) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.Id, packet.Value,
			packet.MethodId == Set) ? 1u : 0u;
	}

	// Focused native-qualification seam for the mutable MorphOS close-request
	// BOOL. The public core canonicalizes all non-zero writes to TRUE, while
	// event polling uses the same named state to publish user close requests.
	public static uint DispatchWindowCloseRequest<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != MuiWindowPublicCore.CloseRequest) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.CloseRequest, packet.Value,
			packet.MethodId == Set) ? 1u : 0u;
	}

	// Focused native-qualification seam for the single MUIA_Window_RootObject
	// relationship. FamilyCore owns the guest-resident parent/child record and
	// retains the child exactly once; no managed object graph is introduced.
	public static uint DispatchWindowRootObject<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != MuiWindowPublicCore.RootObject) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.RootObject, packet.Value,
			packet.MethodId == Set) ? 1u : 0u;
	}

	// Focused native-qualification seam for the mutable MorphOS NoMenus BOOL.
	// Menu presentation remains a platform capability; this route qualifies the
	// guest-visible state and both public packet forms without managed storage.
	public static uint DispatchWindowNoMenus<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != MuiWindowPublicCore.NoMenus) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.NoMenus, packet.Value,
			packet.MethodId == Set) ? 1u : 0u;
	}

	// Focused native-qualification seam for the mutable MorphOS HasAlpha BOOL.
	// Alpha forwarding remains a platform capability; this route qualifies the
	// canonical guest state and both public packet forms without managed state.
	public static uint DispatchWindowHasAlpha<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != MuiWindowPublicCore.HasAlpha) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.HasAlpha, packet.Value,
			packet.MethodId == Set) ? 1u : 0u;
	}

	// Focused native-qualification seam for the bounded MorphOS Opacity LONG.
	// Intuition alpha forwarding remains a platform capability; this route
	// qualifies the named 0..255 guest state and rejects malformed values.
	public static uint DispatchWindowOpacity<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != MuiWindowPublicCore.Opacity) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.Opacity, packet.Value,
			packet.MethodId == Set) ? 1u : 0u;
	}

	// Focused native-qualification seam for the mutable MorphOS window title.
	// The caller-owned guest C string is validated in place; no managed copy or
	// positional packet offset is introduced at this boundary.
	public static uint DispatchWindowTitle<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != MuiWindowPublicCore.Title) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.Title, packet.Value,
			packet.MethodId == Set) ? 1u : 0u;
	}

	// Focused native-qualification seam for the MorphOS Screen pointer. The
	// requested guest Screen remains named state; the public getter exposes it
	// only after OpenWindow has established the native window lifetime.
	public static uint DispatchWindowScreen<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var notify = TryReadSetAttribute(ref platform, message, Set,
			out var packet);
		if (!notify && !TryReadSetAttribute(ref platform, message, NoNotifySet,
			out packet))
			return 0;
		if (packet.Attribute != MuiWindowPublicCore.Screen) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.Screen, packet.Value,
			notify) ? 1u : 0u;
	}

	// Focused native-qualification seam for the caller-owned MorphOS reference
	// window object. The target is validated as a live guest object and retained
	// in named state; platform coordinate calculation remains a separate seam.
	public static uint DispatchWindowRefWindow<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var notify = TryReadSetAttribute(ref platform, message, Set,
			out var packet);
		if (!notify && !TryReadSetAttribute(ref platform, message, NoNotifySet,
			out packet)) return 0;
		if (packet.Attribute != MuiWindowPublicCore.RefWindow) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.RefWindow, packet.Value, notify) ? 1u : 0u;
	}

	// Focused native-qualification seam for the MorphOS
	// MUIA_Window_VisibleOnMaximize BOOL. The named state records the
	// canonical guest value; maximize presentation remains a platform seam.
	public static uint DispatchWindowVisibleOnMaximize<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var notify = TryReadSetAttribute(ref platform, message, Set,
			out var packet);
		if (!notify && !TryReadSetAttribute(ref platform, message, NoNotifySet,
			out packet)) return 0;
		if (packet.Attribute != MuiWindowPublicCore.VisibleOnMaximize) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.VisibleOnMaximize, packet.Value, notify) ? 1u : 0u;
	}

	// Focused native-qualification seam for the initializer-only MorphOS
	// MUIA_Window_IsSubWindow BOOL. Creation tags are accepted before the
	// named object record is marked initialized; later Set/NoNotifySet packets
	// are rejected while the guest-family disposal rule preserves the window.
	public static uint DispatchWindowIsSubWindow<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var notify = TryReadSetAttribute(ref platform, message, Set,
			out var packet);
		if (!notify && !TryReadSetAttribute(ref platform, message, NoNotifySet,
			out packet)) return 0;
		if (packet.Attribute != MuiWindowPublicCore.IsSubWindow) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.IsSubWindow, packet.Value, notify) ? 1u : 0u;
	}

	// Focused native-qualification seam for the initializer-only MorphOS
	// MUIA_Window_TabletMessages BOOL. The guest state is retained as a named
	// record and forwarded to Intuition only when OpenWindow crosses the typed
	// platform capability.
	public static uint DispatchWindowTabletMessages<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var notify = TryReadSetAttribute(ref platform, message, Set,
			out var packet);
		if (!notify && !TryReadSetAttribute(ref platform, message, NoNotifySet,
			out packet)) return 0;
		if (packet.Attribute != MuiWindowPublicCore.TabletMessages) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.TabletMessages, packet.Value, notify) ? 1u : 0u;
	}

	// Focused native-qualification seams for the mutable MorphOS
	// border-scroller policies. Each BOOL is retained in the named guest
	// record; changes to an open window are forwarded as one typed platform
	// capability.
	public static uint DispatchWindowUseBottomBorderScroller<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var notify = TryReadSetAttribute(ref platform, message, Set,
			out var packet);
		if (!notify && !TryReadSetAttribute(ref platform, message, NoNotifySet,
			out packet)) return 0;
		if (packet.Attribute != MuiWindowPublicCore.UseBottomBorderScroller) return 0;
		return MuiApplicationWindowCore.SetBorderScroller(ref platform, state, obj,
			MuiWindowPublicCore.UseBottomBorderScroller, packet.Value, notify) ? 1u : 0u;
	}

	public static uint DispatchWindowUseLeftBorderScroller<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var notify = TryReadSetAttribute(ref platform, message, Set,
			out var packet);
		if (!notify && !TryReadSetAttribute(ref platform, message, NoNotifySet,
			out packet)) return 0;
		if (packet.Attribute != MuiWindowPublicCore.UseLeftBorderScroller) return 0;
		return MuiApplicationWindowCore.SetBorderScroller(ref platform, state, obj,
			MuiWindowPublicCore.UseLeftBorderScroller, packet.Value, notify) ? 1u : 0u;
	}

	public static uint DispatchWindowUseRightBorderScroller<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var notify = TryReadSetAttribute(ref platform, message, Set,
			out var packet);
		if (!notify && !TryReadSetAttribute(ref platform, message, NoNotifySet,
			out packet)) return 0;
		if (packet.Attribute != MuiWindowPublicCore.UseRightBorderScroller) return 0;
		return MuiApplicationWindowCore.SetBorderScroller(ref platform, state, obj,
			MuiWindowPublicCore.UseRightBorderScroller, packet.Value, notify) ? 1u : 0u;
	}

	// Focused native-qualification seam for the initializer-only MorphOS
	// alternate geometry attributes. The four signed LONG values remain named
	// guest attributes; OpenWindow consumes them as one geometry record.
	public static uint DispatchWindowAlternateGeometry<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var notify = TryReadSetAttribute(ref platform, message, Set,
			out var packet);
		if (!notify && !TryReadSetAttribute(ref platform, message, NoNotifySet,
			out packet)) return 0;
		if (packet.Attribute != MuiWindowPublicCore.AltHeight &&
			packet.Attribute != MuiWindowPublicCore.AltWidth &&
			packet.Attribute != MuiWindowPublicCore.AltLeftEdge &&
			packet.Attribute != MuiWindowPublicCore.AltTopEdge) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			packet.Attribute, packet.Value, notify) ? 1u : 0u;
	}

	// Focused native-qualification seam for the initializer-only primary
	// geometry attributes. The four signed LONG values remain named guest
	// attributes; OpenWindow consumes them as one geometry record.
	public static uint DispatchWindowGeometry<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var notify = TryReadSetAttribute(ref platform, message, Set,
			out var packet);
		if (!notify && !TryReadSetAttribute(ref platform, message, NoNotifySet,
			out packet)) return 0;
		if (packet.Attribute != MuiWindowPublicCore.Height &&
			packet.Attribute != MuiWindowPublicCore.Width &&
			packet.Attribute != MuiWindowPublicCore.LeftEdge &&
			packet.Attribute != MuiWindowPublicCore.TopEdge) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			packet.Attribute, packet.Value, notify) ? 1u : 0u;
	}

	// Focused native-qualification seam for the initializer-only window gadget
	// policy. The five BOOL values remain named guest attributes; OpenWindow
	// consumes them as one ULONG policy record.
	public static uint DispatchWindowGadgetPolicy<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var notify = TryReadSetAttribute(ref platform, message, Set,
			out var packet);
		if (!notify && !TryReadSetAttribute(ref platform, message, NoNotifySet,
			out packet)) return 0;
		if (packet.Attribute != MuiWindowPublicCore.CloseGadget &&
			packet.Attribute != MuiWindowPublicCore.DepthGadget &&
			packet.Attribute != MuiWindowPublicCore.DragBar &&
			packet.Attribute != MuiWindowPublicCore.SizeGadget &&
			packet.Attribute != MuiWindowPublicCore.SizeRight) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			packet.Attribute, packet.Value, notify) ? 1u : 0u;
	}

	// Focused host qualification seam for the initializer-only window mode
	// policy. The four BOOL values remain named guest attributes; OpenWindow
	// consumes them as one ULONG policy record.
	public static uint DispatchWindowModePolicy<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var notify = TryReadSetAttribute(ref platform, message, Set,
			out var packet);
		if (!notify && !TryReadSetAttribute(ref platform, message, NoNotifySet,
			out packet)) return 0;
		if (packet.Attribute != MuiWindowPublicCore.AppWindow &&
			packet.Attribute != MuiWindowPublicCore.Backdrop &&
			packet.Attribute != MuiWindowPublicCore.Borderless &&
			packet.Attribute != MuiWindowPublicCore.PanelWindow) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			packet.Attribute, packet.Value, notify) ? 1u : 0u;
	}

	// Focused qualification seam for the owned Window Menustrip relationship.
	// The public setter validates the live Menustrip.mui object and updates the
	// guest family atomically before returning.
	public static uint DispatchWindowMenustrip<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var notify = TryReadSetAttribute(ref platform, message, Set,
			out var packet);
		if (!notify && !TryReadSetAttribute(ref platform, message, NoNotifySet,
			out packet)) return 0;
		if (packet.Attribute != MuiWindowPublicCore.Menustrip) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.Menustrip, packet.Value, notify) ? 1u : 0u;
	}

	// Focused qualification seam for the obsolete MorphOS FancyDrawing BOOL.
	// The named state is preserved for API compatibility; rendering remains
	// governed by the normal MUIM_Draw capability and is not invented here.
	public static uint DispatchWindowFancyDrawing<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var notify = TryReadSetAttribute(ref platform, message, Set,
			out var packet);
		if (!notify && !TryReadSetAttribute(ref platform, message, NoNotifySet,
			out packet)) return 0;
		if (packet.Attribute != MuiWindowPublicCore.FancyDrawing) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.FancyDrawing, packet.Value, notify) ? 1u : 0u;
	}

	// Focused qualification seam for the mutable Window MenuAction event ULONG.
	// Menu transport may use SetWindowMenuActionValue to publish UserData; the
	// packet route remains available to MorphOS-compatible callers.
	public static uint DispatchWindowMenuAction<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var notify = TryReadSetAttribute(ref platform, message, Set,
			out var packet);
		if (!notify && !TryReadSetAttribute(ref platform, message, NoNotifySet,
			out packet)) return 0;
		if (packet.Attribute != MuiWindowPublicCore.MenuAction) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.MenuAction, packet.Value, notify) ? 1u : 0u;
	}

	// Focused qualification seam for initializer-only NeedsMouseObject. Actual
	// hit testing remains a future platform capability; this route only owns
	// the documented guest BOOL state.
	public static uint DispatchWindowNeedsMouseObject<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var notify = TryReadSetAttribute(ref platform, message, Set,
			out var packet);
		if (!notify && !TryReadSetAttribute(ref platform, message, NoNotifySet,
			out packet)) return 0;
		if (packet.Attribute != MuiWindowPublicCore.NeedsMouseObject) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.NeedsMouseObject, packet.Value, notify) ? 1u : 0u;
	}

	// Focused qualification seam for the public MUIA_Window_Open BOOL. The
	// named packet is routed through the typed lifecycle core, which only
	// publishes TRUE after a native window exists and clears it on close.
	public static uint DispatchWindowOpen<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var notify = TryReadSetAttribute(ref platform, message, Set,
			out var packet);
		if (!notify && !TryReadSetAttribute(ref platform, message, NoNotifySet,
			out packet)) return 0;
		if (packet.Attribute != MuiWindowPublicCore.Open) return 0;
		return MuiApplicationWindowCore.SetWindowOpenValue(ref platform, state,
			obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for the mutable MorphOS screen title.
	// The caller-owned guest C string is validated in place; screen title
	// presentation remains a platform capability with no managed copy.
	public static uint DispatchWindowScreenTitle<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != MuiWindowPublicCore.ScreenTitle) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.ScreenTitle, packet.Value,
			packet.MethodId == Set) ? 1u : 0u;
	}

	// Focused native-qualification seam for the mutable MorphOS PublicScreen
	// name. The caller-owned guest C string is validated in place; screen lookup
	// and preference override remain a platform capability.
	public static uint DispatchWindowPublicScreen<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != MuiWindowPublicCore.PublicScreen) return 0;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiWindowPublicCore.PublicScreen, packet.Value,
			packet.MethodId == Set) ? 1u : 0u;
	}

	// Focused native-qualification seam for the application-wide nested sleep
	// counter. The core applies one named depth transition to each owned window
	// and keeps application sleep state in a guest attribute.
	public static uint DispatchApplicationSleep<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (method != Set && method != NoNotifySet) return 0;
		if (!TryReadSetAttribute(ref platform, message, method,
			out var packet) || packet.Attribute != ApplicationSleep) return 0;
		return MuiApplicationWindowCore.SetApplicationSleepValue(ref platform,
			state, obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for MUIA_Application_Iconified. The
	// core owns the guest-resident window reopen markers and only crosses the
	// platform iconification capability after the child-window transition is
	// ready.
	public static uint DispatchApplicationIconified<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (method != Set && method != NoNotifySet) return 0;
		if (!TryReadSetAttribute(ref platform, message, method,
			out var packet) || packet.Attribute != ApplicationIconified) return 0;
		return MuiApplicationWindowCore.SetIconified(ref platform, state, obj,
			packet.Value != 0) ? 1u : 0u;
	}

	// Focused native-qualification seam for MUIA_Application_Active.  The
	// attribute is a commodities-facing BOOL; MUI does not perform an external
	// action, so the core only canonicalizes and stores the named guest value.
	public static uint DispatchApplicationActive<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (method != Set && method != NoNotifySet) return 0;
		if (!TryReadSetAttribute(ref platform, message, method,
			out var packet) || packet.Attribute != ApplicationActive) return 0;
		return MuiApplicationWindowCore.SetApplicationActiveValue(ref platform,
			state, obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for the MorphOS single-task
	// initializer. A conflicting TRUE write marks the already-running
	// application and rejects the candidate without creating managed state.
	public static uint DispatchApplicationSingleTask<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (method != Set && method != NoNotifySet) return 0;
		if (!TryReadSetAttribute(ref platform, message, method,
			out var packet) || packet.Attribute != ApplicationSingleTask) return 0;
		return MuiApplicationWindowCore.SetApplicationSingleTaskValue(ref platform,
			state, obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for the MorphOS DoubleStart flag.
	public static uint DispatchApplicationDoubleStart<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (method != Set && method != NoNotifySet) return 0;
		if (!TryReadSetAttribute(ref platform, message, method,
			out var packet) || packet.Attribute != ApplicationDoubleStart) return 0;
		return MuiApplicationWindowCore.SetApplicationDoubleStartValue(ref platform,
			state, obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for the MorphOS ForceQuit flag. The
	// value is queried by the application after a quit ReturnID and is kept in
	// named guest storage without invoking a host exit path.
	public static uint DispatchApplicationForceQuit<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (method != Set && method != NoNotifySet) return 0;
		if (!TryReadSetAttribute(ref platform, message, method,
			out var packet) || packet.Attribute != ApplicationForceQuit) return 0;
		return MuiApplicationWindowCore.SetApplicationForceQuitValue(ref platform,
			state, obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for the initializer-only UseRexx policy.
	// The value is retained in named guest storage; ARexx transport remains a
	// separate platform service boundary.
	public static uint DispatchApplicationUseRexx<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != ApplicationUseRexx) return 0;
		return MuiApplicationWindowCore.SetApplicationUseRexxValue(ref platform,
			state, obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for the initializer-only
	// UseCommodities policy. Commodities transport remains a platform boundary;
	// this closure proves only the MorphOS BOOL and its initialization policy.
	public static uint DispatchApplicationUseCommodities<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != ApplicationUseCommodities) return 0;
		return MuiApplicationWindowCore.SetApplicationUseCommoditiesValue(
			ref platform, state, obj, packet.Value) ? 1u : 0u;
	}

	private static uint DispatchApplicationInitializerString<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message,
		uint attribute)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != attribute) return 0;
		return MuiApplicationWindowCore.SetApplicationInitializerStringValue(
			ref platform, state, obj, attribute, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for MUIA_Application_Title. The same
	// typed helper is used for the other [I.G] identity strings by broad dispatch.
	public static uint DispatchApplicationTitle<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform =>
		DispatchApplicationInitializerString(ref platform, state, obj, message,
			ApplicationTitle);

	public static uint DispatchApplicationAuthor<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform =>
		DispatchApplicationInitializerString(ref platform, state, obj, message,
			ApplicationAuthor);

	public static uint DispatchApplicationBase<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform =>
		DispatchApplicationInitializerString(ref platform, state, obj, message,
			ApplicationBase);

	public static uint DispatchApplicationCopyright<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform =>
		DispatchApplicationInitializerString(ref platform, state, obj, message,
			ApplicationCopyright);

	public static uint DispatchApplicationDescription<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform =>
		DispatchApplicationInitializerString(ref platform, state, obj, message,
			ApplicationDescription);

	public static uint DispatchApplicationVersion<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform =>
		DispatchApplicationInitializerString(ref platform, state, obj, message,
			ApplicationVersion);

	// Focused native-qualification seam for the mutable HelpFile guest pointer.
	public static uint DispatchApplicationHelpFile<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != ApplicationHelpFile) return 0;
		return MuiApplicationWindowCore.SetApplicationHelpFileValue(ref platform,
			state, obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for the mutable IconifyTitle guest
	// pointer. The title remains caller-owned guest memory.
	public static uint DispatchApplicationIconifyTitle<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != ApplicationIconifyTitle) return 0;
		return MuiApplicationWindowCore.SetApplicationIconifyTitleValue(
			ref platform, state, obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for the initializer-only
	// UseScreenNotify BOOL. The eventual screen-notify service remains outside
	// this guest-state dispatcher.
	public static uint DispatchApplicationUseScreenNotify<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != ApplicationUseScreenNotify) return 0;
		return MuiApplicationWindowCore.SetApplicationUseScreenNotifyValue(
			ref platform, state, obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for the caller-owned Workbench
	// DiskObject pointer. The fixed DiskObject record is validated in guest
	// memory; no managed mirror is created.
	public static uint DispatchApplicationDiskObject<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != ApplicationDiskObject) return 0;
		return MuiApplicationWindowCore.SetApplicationDiskObjectValue(
			ref platform, state, obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for the mutable DropObject MUI object
	// pointer. The target must remain a live guest object.
	public static uint DispatchApplicationDropObject<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != ApplicationDropObject) return 0;
		return MuiApplicationWindowCore.SetApplicationDropObjectValue(
			ref platform, state, obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for the mutable MenuAction ULONG. The
	// packet is decoded through the named SetAttribute record before state is
	// changed; MenuHelp remains publish-only because its MorphOS policy is [..G].
	public static uint DispatchApplicationMenuAction<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != ApplicationMenuAction) return 0;
		return MuiApplicationWindowCore.SetApplicationMenuActionValue(
			ref platform, state, obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for the initializer-only application
	// Menustrip relationship. The target is validated as a live named menu
	// object before family ownership is acquired.
	public static uint DispatchApplicationMenustrip<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != ApplicationMenustrip) return 0;
		return MuiApplicationWindowCore.SetApplicationMenustripValue(
			ref platform, state, obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for one MUIA_Application_Window
	// initializer tag. The object pointer is validated before AddWindow owns it.
	public static uint DispatchApplicationWindow<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != ApplicationWindow) return 0;
		return MuiApplicationWindowCore.SetApplicationWindowValue(ref platform,
			state, obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for the bounded UsedClasses STRPTR
	// vector. The vector itself remains a guest pointer; no managed copy is made.
	public static uint DispatchApplicationUsedClasses<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != ApplicationUsedClasses) return 0;
		return MuiApplicationWindowCore.SetApplicationUsedClassesValue(ref platform,
			state, obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for the bounded MUI_Command table.
	// The table remains caller-owned guest memory; only its fixed records and
	// NUL-terminated strings are validated before the pointer is retained.
	public static uint DispatchApplicationCommands<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != ApplicationCommands) return 0;
		return MuiApplicationCommandsCore.SetApplicationCommandsValue(
			ref platform, state, obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for the initializer-only AppWindow
	// gate. Actual Workbench registration remains a platform capability; this
	// seam proves the guest state and post-open rejection rules.
	public static uint DispatchWindowAppWindow<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadSetAttribute(ref platform, message, Set, out var packet) &&
			!TryReadSetAttribute(ref platform, message, NoNotifySet, out packet))
			return 0;
		if (packet.Attribute != WindowAppWindow) return 0;
		return MuiApplicationMessageCore.SetWindowAppWindowValue(ref platform,
			state, obj, packet.Value) ? 1u : 0u;
	}

	// Focused native-qualification seam for a synchronous AppMessage delivery.
	// The caller-owned message remains valid while notifications execute and is
	// cleared/restored before this method returns.
	public static uint DispatchAppMessage<TPlatform>(
		ref TPlatform platform, APTR state, APTR target, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform =>
		MuiApplicationMessageCore.PublishAppMessage(ref platform, state, target,
			message) ? 1u : 0u;

	// Focused native-qualification seam for the zero-argument screen depth
	// packets. The core requires an open native window before the capability.
	public static uint DispatchWindowScreenDepth<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR message)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, message,
			out var methodHeader)) return 0;
		var method = methodHeader.MethodId;
		if (!TryReadWindowMethod(ref platform, message, method,
			out _)) return 0;
		if (method == WindowScreenToBack)
			return MuiApplicationWindowCore.MoveScreen(ref platform, state, obj,
				false) ? 1u : 0u;
		if (method == WindowScreenToFront)
			return MuiApplicationWindowCore.MoveScreen(ref platform, state, obj,
				true) ? 1u : 0u;
		return 0;
	}

	private static APTR Pointer<TPlatform>(ref TPlatform platform, APTR packet,
		int offset) where TPlatform : struct, IMuiGuestMemory =>
		APTR.FromPointer(platform.ReadUInt32(packet, offset));

	private static bool TryReadReturnId<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiApplicationReturnIdMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiApplicationInputPacketCodec.TryReadReturnId(ref platform, message,
			method, out packet);

	private static bool TryReadInput<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiApplicationInputMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiApplicationInputPacketCodec.TryReadInput(ref platform, message,
			method, out packet);

	private static bool TryReadInputBuffered<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiApplicationInputBufferedMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiApplicationInputPacketCodec.TryReadInputBuffered(ref platform,
			message, method, out packet);

	private static bool TryReadInputHandler<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiApplicationInputHandlerMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiApplicationInputPacketCodec.TryReadInputHandler(ref platform,
			message, method, out packet);

	private static bool TryReadPushMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiApplicationPushMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (method != ApplicationPushMethod) return false;
		var request = default(MuiApplicationQueuePacketCodec.QueuePacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiApplicationQueuePacketCodec.TryReadPush(ref platform,
			ref request, out packet);
	}

	private static bool TryReadUnpushMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiApplicationUnpushMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (method != ApplicationUnpushMethod) return false;
		var request = default(MuiApplicationQueuePacketCodec.QueuePacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiApplicationQueuePacketCodec.TryReadUnpush(ref platform,
			ref request, out packet);
	}

	private static bool TryReadShowHelp<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiApplicationShowHelpMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (method != ApplicationShowHelp) return false;
		var request = default(
			MuiApplicationPresentationPacketCodec.PresentationPacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiApplicationPresentationPacketCodec.TryReadShowHelp(
			ref platform, ref request, out packet);
	}

	private static bool TryReadAboutMui<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiApplicationAboutMuiMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (method != ApplicationAboutMUI) return false;
		var request = default(
			MuiApplicationPresentationPacketCodec.PresentationPacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiApplicationPresentationPacketCodec.TryReadAboutMui(
			ref platform, ref request, out packet);
	}

	private static bool TryReadConfigId<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiApplicationConfigIdMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (method != ApplicationDefaultConfigItem) return false;
		var request = default(MuiApplicationMethodPacketCodec.MethodPacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiApplicationMethodPacketCodec.TryReadConfigId(ref platform,
			ref request, out packet);
	}

	private static bool TryReadSetConfigItem<TPlatform>(ref TPlatform platform,
		APTR message, uint method,
		out MuiApplicationSetConfigItemMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (method != ApplicationSetConfigItem) return false;
		var request = default(
			MuiApplicationSettingsPacketCodec.SettingsPacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiApplicationSettingsPacketCodec.TryReadSetConfigItem(
			ref platform, ref request, out packet);
	}

	private static bool TryReadOpenConfigWindow<TPlatform>(
		ref TPlatform platform, APTR message, uint method,
		out MuiApplicationOpenConfigWindowMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (method != ApplicationOpenConfigWindow) return false;
		var request = default(
			MuiApplicationSettingsPacketCodec.SettingsPacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiApplicationSettingsPacketCodec.TryReadOpenConfigWindow(
			ref platform, ref request, out packet);
	}

	private static bool TryReadBuildSettingsPanel<TPlatform>(
		ref TPlatform platform, APTR message, uint method,
		out MuiApplicationBuildSettingsPanelMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (method != ApplicationBuildSettingsPanel) return false;
		var request = default(
			MuiApplicationSettingsPacketCodec.SettingsPacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiApplicationSettingsPacketCodec.TryReadBuildSettingsPanel(
			ref platform, ref request, out packet);
	}

	private static bool TryReadSettingsIo<TPlatform>(
		ref TPlatform platform, APTR message, uint method,
		out MuiApplicationSettingsIoMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (method != ApplicationSave && method != ApplicationLoad) return false;
		var request = default(
			MuiApplicationSettingsPacketCodec.SettingsPacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiApplicationSettingsPacketCodec.TryReadSettingsIo(
			ref platform, ref request, out packet);
	}

	private static bool TryReadCheckRefresh<TPlatform>(
		ref TPlatform platform, APTR message, uint method,
		out MuiApplicationCheckRefreshMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		var request = default(MuiApplicationMethodPacketCodec.MethodPacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiApplicationMethodPacketCodec.TryReadCheckRefresh(ref platform,
			ref request, out packet);
	}

	private static bool TryReadApplicationLoop<TPlatform>(
		ref TPlatform platform, APTR message, uint method,
		out MuiApplicationLoopMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (method != ApplicationExecute && method != ApplicationRun) return false;
		var request = default(MuiApplicationMethodPacketCodec.MethodPacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiApplicationMethodPacketCodec.TryReadLoop(ref platform,
			ref request, out packet);
	}

	private static bool TryReadSetAttribute<TPlatform>(
		ref TPlatform platform, APTR message, uint method,
		out MuiSetAttributeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (method != Set && method != NoNotifySet) return false;
		var request = default(MuiNotifyPacketCodec.PacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiNotifyPacketCodec.TryReadSet(ref platform, ref request,
			out packet);
	}

	private static bool TryReadWindowMethod<TPlatform>(
		ref TPlatform platform, APTR message, uint method,
		out MuiWindowMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (method != WindowSetup && method != WindowCleanup &&
			method != WindowToBack && method != WindowToFront &&
			method != WindowScreenToBack && method != WindowScreenToFront)
			return false;
		var request = default(MuiApplicationMethodPacketCodec.MethodPacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiApplicationMethodPacketCodec.TryReadWindowMethod(ref platform,
			ref request, out packet);
	}

	private static bool TryReadWindowCycleChain<TPlatform>(
		ref TPlatform platform, APTR message, uint method,
		out MuiWindowCycleChainMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		var request = default(
			MuiWindowCycleChainPacketCodec.CycleChainPacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiWindowCycleChainPacketCodec.TryRead(ref platform,
			ref request, out packet);
	}

	private static bool TryReadMenuQuery<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiApplicationMenuQueryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		var request = default(MuiApplicationMenuPacketCodec.MenuPacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiApplicationMenuPacketCodec.TryReadApplicationQuery(ref platform,
			ref request, out packet);
	}

	private static bool TryReadMenuSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiApplicationMenuSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		var request = default(MuiApplicationMenuPacketCodec.MenuPacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiApplicationMenuPacketCodec.TryReadApplicationSet(ref platform,
			ref request, out packet);
	}

	private static bool TryReadWindowMenuQuery<TPlatform>(
		ref TPlatform platform, APTR message, uint method,
		out MuiWindowMenuQueryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		var request = default(MuiApplicationMenuPacketCodec.MenuPacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiApplicationMenuPacketCodec.TryReadWindowQuery(ref platform,
			ref request, out packet);
	}

	private static bool TryReadWindowMenuSet<TPlatform>(
		ref TPlatform platform, APTR message, uint method,
		out MuiWindowMenuSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		var request = default(MuiApplicationMenuPacketCodec.MenuPacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiApplicationMenuPacketCodec.TryReadWindowSet(ref platform,
			ref request, out packet);
	}

	private static bool TryReadWindowEventHandler<TPlatform>(
		ref TPlatform platform, APTR message, uint method,
		out MuiWindowEventHandlerMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		var request = default(MuiApplicationMenuPacketCodec.MenuPacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiApplicationMenuPacketCodec.TryReadWindowEventHandler(
			ref platform, ref request, out packet);
	}

	private static bool TryReadWindowSnapshot<TPlatform>(
		ref TPlatform platform, APTR message, uint method,
		out MuiWindowSnapshotMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (method != WindowSnapshot) return false;
		var request = default(MuiApplicationMethodPacketCodec.MethodPacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiApplicationMethodPacketCodec.TryReadSnapshot(ref platform,
			ref request, out packet);
	}

}
