/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;
using Amiga.MUI;

namespace CopperOS.MuiMaster;

// These records describe the public 68k Notify packet headers. The packet
// readers below are the only place that translates guest bytes into fields;
// notification behavior consumes named fields and never repeats ABI offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNotifyMessage
{
	public const uint Size = 20;
	internal const uint MethodIdOffset = 0;
	internal const uint TriggerAttributeOffset = 4;
	internal const uint TriggerValueOffset = 8;
	internal const uint DestinationOffset = 12;
	internal const uint FollowCountOffset = 16;
	public uint MethodId;
	public uint TriggerAttribute;
	public uint TriggerValue;
	public uint Destination;
	public uint FollowCount;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiKillNotifyMessage
{
	public const uint Size = 8;
	internal const uint MethodIdOffset = 0;
	internal const uint TriggerAttributeOffset = 4;
	public uint MethodId;
	public uint TriggerAttribute;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiKillNotifyObjectMessage
{
	public const uint Size = 12;
	internal const uint MethodIdOffset = 0;
	internal const uint TriggerAttributeOffset = 4;
	internal const uint DestinationOffset = 8;
	public uint MethodId;
	public uint TriggerAttribute;
	public uint Destination;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSetAttributeMessage
{
	public const uint Size = 12;
	internal const uint MethodIdOffset = 0;
	internal const uint AttributeOffset = 4;
	internal const uint ValueOffset = 8;
	public uint MethodId;
	public uint Attribute;
	public uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMultiSetMessage
{
	public const uint Size = 16;
	internal const uint MethodIdOffset = 0;
	internal const uint AttributeOffset = 4;
	internal const uint ValueOffset = 8;
	internal const uint FirstObjectOffset = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Value;
	public uint FirstObject;
}

// Internal semantic request for the live MUIM_MultiSet walk. The guest packet
// keeps ULONG fields for ABI compatibility; production mutation code receives
// named APTR fields instead of a high-arity scalar argument list.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMultiSetRequest
{
	internal APTR Executor;
	internal uint Attribute;
	internal uint Value;
	internal APTR FirstObject;
	internal APTR Vector;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMultiSetDispatchRequest
{
	internal APTR State;
	internal APTR Executor;
	internal APTR Message;
}

// The Notify and MultiSet packets each carry a caller-owned inline ULONG
// vector immediately after their fixed header. A semantic kind keeps those
// two ABI boundaries named while sharing one overflow-safe address adapter.
internal enum MuiNotifyInlineVectorKind : byte
{
	FollowParameters,
	MultiSetTargets,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNotifyInlineVectorCursor
{
	internal const uint EntrySize = 4;
	internal const uint MaximumEntries = 256;
	internal APTR Message;
	internal MuiNotifyInlineVectorKind Kind;
	internal uint Index;
}

internal static class MuiNotifyInlineVectorCursorCodec
{
	internal static bool TryGetAddress(MuiNotifyInlineVectorCursor cursor,
		out APTR address)
		=> MuiNotifyInlineVectorMemoryCodec.TryGetAddress(cursor.Message,
			cursor.Kind, cursor.Index, out address);
}

// Struct-first guest-memory adapter for the caller-owned inline ULONG vectors
// carried by Notify and MultiSet packets. The selected slot and its complete
// fixed packet header must be mapped before a guest address is exposed; packet
// offsets remain private to this boundary.
internal static class MuiNotifyInlineVectorMemoryCodec
{
	private static bool TryGetPacketSize(MuiNotifyInlineVectorKind kind,
		out uint packetSize)
	{
		if (kind == MuiNotifyInlineVectorKind.FollowParameters)
		{
			packetSize = MuiNotifyMessage.Size;
			return true;
		}
		if (kind == MuiNotifyInlineVectorKind.MultiSetTargets)
		{
			packetSize = MuiMultiSetMessage.Size;
			return true;
		}
		packetSize = 0;
		return false;
	}

	// Resolve the inline vector address without touching guest memory. This is
	// the compatibility form used by the legacy address-only helpers; the
	// platform-aware overload below adds complete packet/slot mapping checks.
	internal static bool TryGetAddress(APTR message,
		MuiNotifyInlineVectorKind kind, uint index, out APTR address)
	{
		address = APTR.Null;
		uint baseOffset;
		switch (kind)
		{
			case MuiNotifyInlineVectorKind.FollowParameters:
				baseOffset = MuiNotifyMessage.Size;
				break;
			case MuiNotifyInlineVectorKind.MultiSetTargets:
				baseOffset = MuiMultiSetMessage.Size;
				break;
			default:
				return false;
		}
		if (message.IsNull || message.Raw > uint.MaxValue - baseOffset ||
			index >= MuiNotifyInlineVectorCursor.MaximumEntries) return false;
		var vector = message.Raw + baseOffset;
		if (index > (uint.MaxValue - vector) /
			MuiNotifyInlineVectorCursor.EntrySize) return false;
		var offset = index * MuiNotifyInlineVectorCursor.EntrySize;
		if (vector > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector + offset);
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyInlineVectorKind kind, uint index,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryGetPacketSize(kind, out var packetSize) || message.IsNull ||
			!platform.IsMapped(message, packetSize)) return false;
		if (!TryGetAddress(message, kind, index, out address)) return false;
		return platform.IsMapped(address,
			MuiNotifyInlineVectorCursor.EntrySize);
	}
}

// MUIM_MultiSet carries a NULL-terminated vector of target object pointers
// immediately after its fixed message header. Keep each pointer slot named so
// the mutation walk does not decode an anonymous ULONG at every index.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMultiSetTargetEntry
{
	internal const uint Size = 4;
	internal APTR Target;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMultiSetTargetVectorCursor
{
	internal const uint EntrySize = MuiMultiSetTargetEntry.Size;
	internal const uint MaximumEntries = 256;
	internal APTR Base;
	internal uint Index;
}

// Struct-first guest-memory adapter for a caller-owned MultiSet target
// vector. Each selected named target record is exposed only after bounded
// index, arithmetic, and complete guest-range checks.
internal static class MuiMultiSetTargetVectorMemoryCodec
{
	internal static APTR GetEntryAddress(APTR vector, uint index)
	{
		if (vector.IsNull || index >=
			MuiMultiSetTargetVectorCursor.MaximumEntries || index >
			(uint.MaxValue - vector.Raw) /
			MuiMultiSetTargetEntry.Size) return APTR.Null;
		var offset = index * MuiMultiSetTargetEntry.Size;
		if (vector.Raw > uint.MaxValue - offset) return APTR.Null;
		return APTR.FromPointer(vector.Raw + offset);
	}

	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = GetEntryAddress(vector, index);
		return address.IsNotNull && platform.IsMapped(address,
			MuiMultiSetTargetEntry.Size);
	}
}

internal static class MuiMultiSetTargetVectorCodec
{
	internal static APTR GetEntryAddress(MuiMultiSetTargetVectorCursor cursor)
		=> MuiMultiSetTargetVectorMemoryCodec.GetEntryAddress(cursor.Base,
			cursor.Index);

	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiMultiSetTargetVectorCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiMultiSetTargetVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);
}

internal static class MuiMultiSetTargetEntryCodec
{
	internal static bool TryReadInto<TPlatform>(ref TPlatform platform,
		APTR address, ref MuiMultiSetTargetEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiMultiSetTargetEntry.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var target)) return false;
		value.Target = APTR.FromPointer(target);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiMultiSetTargetEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return TryReadInto(ref platform, address, ref value);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiMultiSetTargetEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiMultiSetTargetEntry.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Target.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// MUIM_Notify follow parameters are caller-owned ULONG values copied into the
// notification record and replayed on each trigger. Keep each inline value as
// a named wire slot so the dispatch loop does not repeat anonymous offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNotifyFollowParameterSlot
{
	internal const uint Size = 4;
	internal uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNotifyFollowParameterVectorCursor
{
	internal const uint EntrySize = MuiNotifyFollowParameterSlot.Size;
	internal const uint MaximumEntries = 256;
	internal APTR Base;
	internal uint Index;
}

// Struct-first guest-memory adapter for a caller-owned Notify follow-value
// vector. Every named ULONG slot is exposed only after bounded index,
// arithmetic, and complete guest-range checks.
internal static class MuiNotifyFollowParameterVectorMemoryCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (vector.IsNull || index >=
			MuiNotifyFollowParameterVectorCursor.MaximumEntries || index >
			(uint.MaxValue - vector.Raw) /
			MuiNotifyFollowParameterSlot.Size) return false;
		var offset = index * MuiNotifyFollowParameterSlot.Size;
		if (vector.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector.Raw + offset);
		return platform.IsMapped(address, MuiNotifyFollowParameterSlot.Size);
	}
}

internal static class MuiNotifyFollowParameterVectorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiNotifyFollowParameterVectorCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNotifyFollowParameterVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);
}

internal static class MuiNotifyFollowParameterSlotCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiNotifyFollowParameterSlot slot)
		where TPlatform : struct, IMuiGuestMemory
	{
		slot = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNotifyFollowParameterSlot.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out slot.Value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiNotifyFollowParameterSlot slot)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNotifyFollowParameterSlot.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				slot.Value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// MUIM_GetConfigItem writes one caller-owned ULONG result. Keep the storage
// named so the capability bridge does not expose an anonymous offset.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNotifyConfigStorage
{
	internal const uint Size = 4;
	internal uint Value;
}

internal static class MuiNotifyConfigStorageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiNotifyConfigStorage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNotifyConfigStorage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiNotifyConfigStorage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNotifyConfigStorage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFindObjectMessage
{
	public const uint Size = 8;
	internal const uint MethodIdOffset = 0;
	internal const uint FindObjectOffset = 4;
	public uint MethodId;
	public uint FindObject;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNotifyMethodMessage
{
	public const uint Size = 4;
	internal const uint MethodIdOffset = 0;
	public uint MethodId;
}

// Struct-first codecs for the fixed Notify method header and Set/NoNotifySet
// packet. Production paths consume complete named records; the generic field
// adapter below remains available only to explicit compatibility callers.
internal static class MuiNotifyMethodMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiNotifyMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNotifyMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiNotifyMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNotifyMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiSetAttributeMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiSetAttributeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiSetAttributeMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiSetAttributeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiSetAttributeMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiNotifyMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiNotifyMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNotifyMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.TriggerAttribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.TriggerValue) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Destination) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.FollowCount)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiNotifyMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNotifyMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.TriggerAttribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.TriggerValue) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Destination) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.FollowCount)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiMultiSetMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiMultiSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiMultiSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Value) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.FirstObject)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiMultiSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiMultiSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Value) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.FirstObject)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiKillNotifyMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiKillNotifyMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiKillNotifyMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.TriggerAttribute)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiKillNotifyMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiKillNotifyMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.TriggerAttribute)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiKillNotifyObjectMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiKillNotifyObjectMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiKillNotifyObjectMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.TriggerAttribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Destination)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiKillNotifyObjectMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiKillNotifyObjectMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.TriggerAttribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Destination)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiFindObjectMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiFindObjectMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiFindObjectMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.FindObject)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiFindObjectMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiFindObjectMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.FindObject)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal enum MuiNotifyPacketKind : byte
{
	Notify,
	KillNotify,
	KillNotifyObject,
	Set,
	MultiSet,
	FindObject,
}

internal enum MuiNotifyPacketField : byte
{
	MethodId,
	TriggerAttribute,
	TriggerValue,
	Destination,
	FollowCount,
	Attribute,
	Value,
	FirstObject,
	FindObject,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNotifyPacketFieldCursor
{
	internal APTR Message;
	internal MuiNotifyPacketKind Packet;
	internal MuiNotifyPacketField Field;
}

internal static class MuiNotifyPacketFieldMemoryCodec
{
	private static bool TryResolve(MuiNotifyPacketKind packet,
		MuiNotifyPacketField field, out uint offset, out uint size)
	{
		switch (packet)
		{
			case MuiNotifyPacketKind.Notify:
				size = MuiNotifyMessage.Size;
				if (field == MuiNotifyPacketField.MethodId) { offset = MuiNotifyMessage.MethodIdOffset; return true; }
				if (field == MuiNotifyPacketField.TriggerAttribute) { offset = MuiNotifyMessage.TriggerAttributeOffset; return true; }
				if (field == MuiNotifyPacketField.TriggerValue) { offset = MuiNotifyMessage.TriggerValueOffset; return true; }
				if (field == MuiNotifyPacketField.Destination) { offset = MuiNotifyMessage.DestinationOffset; return true; }
				if (field == MuiNotifyPacketField.FollowCount) { offset = MuiNotifyMessage.FollowCountOffset; return true; }
				break;
			case MuiNotifyPacketKind.KillNotify:
				size = MuiKillNotifyMessage.Size;
				if (field == MuiNotifyPacketField.MethodId) { offset = MuiKillNotifyMessage.MethodIdOffset; return true; }
				if (field == MuiNotifyPacketField.TriggerAttribute) { offset = MuiKillNotifyMessage.TriggerAttributeOffset; return true; }
				break;
			case MuiNotifyPacketKind.KillNotifyObject:
				size = MuiKillNotifyObjectMessage.Size;
				if (field == MuiNotifyPacketField.MethodId) { offset = MuiKillNotifyObjectMessage.MethodIdOffset; return true; }
				if (field == MuiNotifyPacketField.TriggerAttribute) { offset = MuiKillNotifyObjectMessage.TriggerAttributeOffset; return true; }
				if (field == MuiNotifyPacketField.Destination) { offset = MuiKillNotifyObjectMessage.DestinationOffset; return true; }
				break;
			case MuiNotifyPacketKind.Set:
				size = MuiSetAttributeMessage.Size;
				if (field == MuiNotifyPacketField.MethodId) { offset = MuiSetAttributeMessage.MethodIdOffset; return true; }
				if (field == MuiNotifyPacketField.Attribute) { offset = MuiSetAttributeMessage.AttributeOffset; return true; }
				if (field == MuiNotifyPacketField.Value) { offset = MuiSetAttributeMessage.ValueOffset; return true; }
				break;
			case MuiNotifyPacketKind.MultiSet:
				size = MuiMultiSetMessage.Size;
				if (field == MuiNotifyPacketField.MethodId) { offset = MuiMultiSetMessage.MethodIdOffset; return true; }
				if (field == MuiNotifyPacketField.Attribute) { offset = MuiMultiSetMessage.AttributeOffset; return true; }
				if (field == MuiNotifyPacketField.Value) { offset = MuiMultiSetMessage.ValueOffset; return true; }
				if (field == MuiNotifyPacketField.FirstObject) { offset = MuiMultiSetMessage.FirstObjectOffset; return true; }
				break;
			case MuiNotifyPacketKind.FindObject:
				size = MuiFindObjectMessage.Size;
				if (field == MuiNotifyPacketField.MethodId) { offset = MuiFindObjectMessage.MethodIdOffset; return true; }
				if (field == MuiNotifyPacketField.FindObject) { offset = MuiFindObjectMessage.FindObjectOffset; return true; }
				break;
		}
		offset = 0;
		size = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyPacketKind packet, MuiNotifyPacketField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset, out var size) || message.IsNull ||
			message.Raw > uint.MaxValue - offset)
			return false;
		if (!platform.IsMapped(message, size)) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyPacketKind packet, MuiNotifyPacketField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyPacketKind packet, MuiNotifyPacketField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// Notify packet cursor. New code passes the message address, packet kind, and
// named field directly to the struct-backed memory adapter above.
internal static class MuiNotifyPacketFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiNotifyPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNotifyPacketFieldMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Packet, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyPacketKind packet, MuiNotifyPacketField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNotifyPacketFieldMemoryCodec.TryReadUInt32(ref platform, message,
			packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyPacketKind packet, MuiNotifyPacketField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNotifyPacketFieldMemoryCodec.TryWriteUInt32(ref platform, message,
			packet, field, value);
}

// The request descriptor is host-side state, not a guest layout. It keeps the
// packet address and method selector together across the freestanding call
// boundary; the codec below is the only place that translates packet bytes
// into the named public message records.
internal static class MuiNotifyPacketCodec
{
	internal struct PacketAddress
	{
		public APTR Address;
		public uint Method;
	}

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR address, out MuiNotifyMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiNotifyMethodMessageCodec.TryRead(ref platform, address,
			out value);
	}

	// Selector admission is decoded through the complete named method header;
	// public packet consumers still receive that typed record.
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiNotifyMethodMessageCodec.TryRead(ref platform, address,
			out var header)) return false;
		methodId = header.MethodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool TryReadNotify<TPlatform>(ref TPlatform platform,
		ref PacketAddress request, out MuiNotifyMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiNotifyMessageCodec.TryRead(ref platform, request.Address,
			out value) || value.MethodId != request.Method ||
			request.Method != MuiNotifyCore.NotifyMethod)
		{
			value = default;
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool TryReadKillNotify<TPlatform>(
		ref TPlatform platform, ref PacketAddress request,
		out MuiKillNotifyMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiKillNotifyMessageCodec.TryRead(ref platform, request.Address,
			out value) || value.MethodId != request.Method ||
			request.Method != MuiNotifyCore.KillNotifyMethod)
		{
			value = default;
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool TryReadKillNotifyObject<TPlatform>(
		ref TPlatform platform, ref PacketAddress request,
		out MuiKillNotifyObjectMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiKillNotifyObjectMessageCodec.TryRead(ref platform,
			request.Address, out value) || value.MethodId != request.Method ||
			request.Method != MuiNotifyCore.KillNotifyObjectMethod)
		{
			value = default;
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		ref PacketAddress request, out MuiSetAttributeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiSetAttributeMessageCodec.TryRead(ref platform, request.Address,
			out value) || value.MethodId != request.Method ||
			(request.Method != MuiNotifyCore.SetMethod &&
				request.Method != MuiNotifyCore.NoNotifySetMethod))
		{
			value = default;
			return false;
		}
		return true;
	}

	// Struct-first writer for the fixed Set/NoNotifySet packet used by follow
	// notifications. The complete named record is admitted before any field is
	// published; callers do not need to reproduce its wire offsets.
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool TryWriteSet<TPlatform>(ref TPlatform platform,
		APTR address, MuiSetAttributeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (value.MethodId != MuiNotifyCore.SetMethod &&
			value.MethodId != MuiNotifyCore.NoNotifySetMethod) return false;
		return MuiSetAttributeMessageCodec.Write(ref platform, address, value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool TryReadMultiSet<TPlatform>(ref TPlatform platform,
		ref PacketAddress request, out MuiMultiSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiMultiSetMessageCodec.TryRead(ref platform, request.Address,
			out value) || value.MethodId != request.Method ||
			request.Method != MuiNotifyCore.MultiSetMethod)
		{
			value = default;
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool TryReadFindObject<TPlatform>(ref TPlatform platform,
		ref PacketAddress request, out MuiFindObjectMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiFindObjectMessageCodec.TryRead(ref platform, request.Address,
			out value) || value.MethodId != request.Method ||
			request.Method != MuiNotifyCore.FindObjectMethod)
		{
			value = default;
			return false;
		}
		return true;
	}

	internal static APTR FollowParameters(APTR address)
		=> MuiNotifyInlineVectorMemoryCodec.TryGetAddress(address,
			MuiNotifyInlineVectorKind.FollowParameters, 0, out var parameters) ?
			parameters : APTR.Null;

	internal static APTR MultiSetVector(APTR address)
		=> MuiNotifyInlineVectorMemoryCodec.TryGetAddress(address,
			MuiNotifyInlineVectorKind.MultiSetTargets, 0, out var vector) ?
			vector : APTR.Null;
}

public static class MuiNotifyCore
{
	private const uint TriggerValue = 1233727793;
	private const uint NotTriggerValue = 1233727795;
	private const uint ConfigPublicScreen = 0x24;
	private const uint MaximumMultiSetTargets = 256;

	public const uint NotifyMethod = 0x8042C9CB;
	public const uint GetConfigItemMethod = 0x80423EDB;
	public const uint KillNotifyMethod = 0x8042D240;
	public const uint KillNotifyObjectMethod = 0x8042B145;
	public const uint MultiSetMethod = 0x8042D356;
	public const uint FindObjectMethod = 0x8042038F;
	public const uint SetMethod = 0x8042549A;
	public const uint NoNotifySetMethod = 0x8042216F;

	internal static bool TryReadNotify<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiNotifyMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		var request = default(MuiNotifyPacketCodec.PacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiNotifyPacketCodec.TryReadNotify(ref platform, ref request,
			out packet);
	}

	internal static bool TryReadKillNotify<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiKillNotifyMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		var request = default(MuiNotifyPacketCodec.PacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiNotifyPacketCodec.TryReadKillNotify(ref platform, ref request,
			out packet);
	}

	internal static bool TryReadKillNotifyObject<TPlatform>(
		ref TPlatform platform, APTR message, uint method,
		out MuiKillNotifyObjectMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		var request = default(MuiNotifyPacketCodec.PacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiNotifyPacketCodec.TryReadKillNotifyObject(ref platform,
			ref request, out packet);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiSetAttributeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		var request = default(MuiNotifyPacketCodec.PacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiNotifyPacketCodec.TryReadSet(ref platform, ref request,
			out packet);
	}

	internal static bool TryReadMultiSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMultiSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		var request = default(MuiNotifyPacketCodec.PacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiNotifyPacketCodec.TryReadMultiSet(ref platform, ref request,
			out packet);
	}

	internal static bool TryReadFindObject<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiFindObjectMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		var request = default(MuiNotifyPacketCodec.PacketAddress);
		request.Address = message;
		request.Method = method;
		return MuiNotifyPacketCodec.TryReadFindObject(ref platform, ref request,
			out packet);
	}

	internal static APTR FollowParameters<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
		=> MuiNotifyInlineVectorMemoryCodec.TryGetAddress(ref platform, message,
			MuiNotifyInlineVectorKind.FollowParameters, 0, out var parameters) ?
			parameters : APTR.Null;

	internal static APTR MultiSetVector<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
		=> MuiNotifyInlineVectorMemoryCodec.TryGetAddress(ref platform, message,
			MuiNotifyInlineVectorKind.MultiSetTargets, 0, out var vector) ?
			vector : APTR.Null;

	public static bool MultiSet<TPlatform>(ref TPlatform platform, APTR state,
		APTR executor, uint attribute, uint value, APTR firstObject,
		APTR vector) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var request = default(MuiMultiSetRequest);
		request.Executor = executor;
		request.Attribute = attribute;
		request.Value = value;
		request.FirstObject = firstObject;
		request.Vector = vector;
		return ApplyMultiSet(ref platform, state, request);
	}

	private static bool SetMultiSetAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiCommonControlCore.TrySetHeadlessPropAttribute(ref platform, state,
			obj, attribute, value, notify, out var handled)) return false;
		return handled || MuiHeadlessObjectCore.SetAttribute(ref platform, state,
			obj, attribute, value, notify);
	}

	internal static bool ApplyMultiSet<TPlatform>(ref TPlatform platform,
		APTR state, MuiMultiSetRequest request)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			request.Executor).IsNull)
			return false;
		if (request.FirstObject.IsNull || request.Vector.IsNull) return false;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			request.FirstObject).IsNull) return false;
		var count = 1u;
		MuiMultiSetTargetEntry targetEntry = default;
		if (!MuiMultiSetTargetVectorMemoryCodec.TryGetEntry(ref platform,
			request.Vector, 0,
			out var targetSlot) ||
			!MuiMultiSetTargetEntryCodec.TryReadInto(ref platform, targetSlot,
				ref targetEntry)) return false;
		var target = targetEntry.Target;
		if (target.IsNotNull)
		{
			if (MuiHeadlessObjectCore.FindObject(ref platform, state,
				target).IsNull) return false;
			count = 2;
			while (count <= MaximumMultiSetTargets)
			{
				targetEntry = default;
				if (!MuiMultiSetTargetVectorMemoryCodec.TryGetEntry(ref platform,
					request.Vector, count - 1,
					out targetSlot) ||
					!MuiMultiSetTargetEntryCodec.TryReadInto(ref platform, targetSlot,
						ref targetEntry)) return false;
				target = targetEntry.Target;
				if (target.IsNull) break;
				if (count == MaximumMultiSetTargets ||
					MuiHeadlessObjectCore.FindObject(ref platform, state,
						target).IsNull) return false;
				count++;
			}
		}
		if (count == 0 || count > MaximumMultiSetTargets) return false;
		if (request.FirstObject.Raw != request.Executor.Raw &&
			!SetMultiSetAttribute(ref platform, state, request.FirstObject,
				request.Attribute, request.Value, true))
			return false;
		for (var index = 1u; index < count; index++)
		{
			MuiMultiSetTargetEntry mutationEntry = default;
			if (!MuiMultiSetTargetVectorMemoryCodec.TryGetEntry(ref platform,
				request.Vector, index - 1,
				out var mutationSlot) ||
				!MuiMultiSetTargetEntryCodec.TryReadInto(ref platform, mutationSlot,
					ref mutationEntry) ||
				mutationEntry.Target.IsNull) return false;
			var mutationTarget = mutationEntry.Target;
			if (mutationTarget.Raw != request.Executor.Raw &&
				!SetMultiSetAttribute(ref platform, state, mutationTarget,
					request.Attribute, request.Value, true)) return false;
		}
		return true;
	}

	// Keep the public packet-facing route struct-first at one boundary. The
	// fixed message is decoded once into MuiMultiSetMessage; the mutation walk
	// then consumes its named fields and the named inline target-vector base.
	internal static bool DispatchMultiSet<TPlatform>(ref TPlatform platform,
		APTR state, APTR executor, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var dispatch = default(MuiMultiSetDispatchRequest);
		dispatch.State = state;
		dispatch.Executor = executor;
		dispatch.Message = message;
		return DispatchMultiSet(ref platform, dispatch);
	}

	internal static bool DispatchMultiSet<TPlatform>(ref TPlatform platform,
		MuiMultiSetDispatchRequest dispatch)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadMultiSet(ref platform, dispatch.Message, MultiSetMethod,
			out var packet)) return false;
		var firstObject = APTR.FromPointer(packet.FirstObject);
		// The inline vector starts immediately after the typed fixed header. This
		// is the guest ABI boundary; the target slots themselves remain typed
		// records below.
		var vector = MultiSetVector(ref platform, dispatch.Message);
		if (MuiHeadlessObjectCore.FindObject(ref platform, dispatch.State,
			dispatch.Executor).IsNull ||
			firstObject.IsNull || vector.IsNull) return false;
		if (MuiHeadlessObjectCore.FindObject(ref platform, dispatch.State,
			firstObject).IsNull) return false;
		var count = 1u;
		MuiMultiSetTargetEntry entry = default;
		if (!MuiMultiSetTargetVectorMemoryCodec.TryGetEntry(ref platform, vector, 0,
			out var targetSlot) ||
			!MuiMultiSetTargetEntryCodec.TryReadInto(ref platform, targetSlot,
				ref entry)) return false;
		var target = entry.Target;
		if (target.IsNotNull)
		{
			if (MuiHeadlessObjectCore.FindObject(ref platform, dispatch.State,
				target).IsNull) return false;
			count = 2;
			while (count <= MaximumMultiSetTargets)
			{
				entry = default;
				if (!MuiMultiSetTargetVectorMemoryCodec.TryGetEntry(ref platform,
					vector, count - 1,
					out targetSlot) ||
					!MuiMultiSetTargetEntryCodec.TryReadInto(ref platform, targetSlot,
						ref entry)) return false;
				target = entry.Target;
				if (target.IsNull) break;
				if (count == MaximumMultiSetTargets ||
					MuiHeadlessObjectCore.FindObject(ref platform, dispatch.State,
						target).IsNull) return false;
				count++;
			}
		}
		if (count == 0 || count > MaximumMultiSetTargets) return false;
		if (firstObject.Raw != dispatch.Executor.Raw &&
			!SetMultiSetAttribute(ref platform, dispatch.State, firstObject,
				packet.Attribute, packet.Value, true)) return false;
		for (var index = 1u; index < count; index++)
		{
			MuiMultiSetTargetEntry mutationEntry = default;
			if (!MuiMultiSetTargetVectorMemoryCodec.TryGetEntry(ref platform, vector,
				index - 1,
				out var mutationSlot) ||
				!MuiMultiSetTargetEntryCodec.TryReadInto(ref platform, mutationSlot,
					ref mutationEntry) || mutationEntry.Target.IsNull) return false;
			var mutationTarget = mutationEntry.Target;
			if (mutationTarget.Raw != dispatch.Executor.Raw &&
				!SetMultiSetAttribute(ref platform, dispatch.State, mutationTarget,
					packet.Attribute, packet.Value, true)) return false;
		}
		return true;
	}

	// MUIM_FindObject walks the guest-resident parent records rather than
	// allocating a managed traversal structure. The calling object itself is
	// considered contained, matching the object-tree interpretation used by
	// MorphOS MUI.
	public static bool FindObject<TPlatform>(ref TPlatform platform, APTR state,
		APTR root, APTR findme)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, root).IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, findme).IsNull)
			return false;
		var current = findme;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (current.Raw == root.Raw) return true;
			var currentRecord = MuiHeadlessObjectCore.FindObject(ref platform,
				state, current);
			if (currentRecord.IsNull || !MuiHeadlessObjectCodec.TryRead(
				ref platform, currentRecord, out var currentValue)) return false;
			var parentRecord = currentValue.Parent;
			if (parentRecord.IsNull || !MuiHeadlessObjectCodec.TryRead(
				ref platform, parentRecord, out var parentValue)) return false;
			var parent = parentValue.Boopsi;
			if (parent.IsNull || MuiHeadlessObjectCore.FindObject(ref platform,
				state, parent).IsNull) return false;
			current = parent;
		}
		return false;
	}

	// MUIM_GetConfigItem (V11).  MorphOS currently exposes only
	// MUICFG_PublicScreen through this method.  The result is written to the
	// caller-owned ULONG exactly once after the live-object, storage, and
	// platform capability checks have all succeeded.
	public static bool GetConfigItem<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint configId, APTR storage)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (configId != ConfigPublicScreen || storage.IsNull ||
			!platform.IsMapped(storage, MuiNotifyConfigStorage.Size)) return false;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		if (!platform.GetMuiConfigItem(obj, configId, out var value)) return false;
		var result = default(MuiNotifyConfigStorage);
		result.Value = value;
		return MuiNotifyConfigStorageCodec.Write(ref platform, storage, result);
	}

	public static bool Add<TPlatform>(ref TPlatform platform, APTR state,
		APTR source, uint triggerAttribute, uint triggerValue, APTR destination,
		uint followCount, APTR followParameters)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var notificationSource = ResolveNotificationSource(ref platform, state,
			source, triggerAttribute);
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state,
			notificationSource);
		if (record.IsNull || destination.IsNull || followCount == 0 ||
			followCount > 256 || followParameters.IsNull) return false;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var sourceValue)) return false;
		var payloadBytes = followCount * 4u;
		if (payloadBytes / 4u != followCount ||
			!platform.IsMapped(followParameters, payloadBytes)) return false;
		var size = MuiHeadlessNotificationRecord.Size + payloadBytes;
		if (size < MuiHeadlessNotificationRecord.Size) return false;
		var item = MuiHeadlessMemory.Allocate(ref platform, size);
		if (item.IsNull) return false;
		MuiHeadlessNotificationRecord notification = default;
		notification.Sequence = MuiHeadlessMemory.NextSequence(ref platform,
			state);
		notification.TriggerAttribute = triggerAttribute;
		notification.TriggerValue = triggerValue;
		notification.Destination = destination;
		notification.FollowCount = followCount;
		if (!MuiHeadlessNotificationCodec.TryGetPayload(ref platform, item,
			payloadBytes, out var payload))
		{
			FreeNotification(ref platform, item);
			return false;
		}
		platform.Copy(followParameters, payload, payloadBytes);
		var head = sourceValue.Notifications;
		notification.Next = head;
		if (!MuiHeadlessNotificationCodec.Write(ref platform, item,
			notification))
		{
			FreeNotification(ref platform, item);
			return false;
		}
		sourceValue.Notifications = item;
		if (!MuiHeadlessObjectCodec.Write(ref platform, record, sourceValue))
		{
			FreeNotification(ref platform, item);
			return false;
		}
		MuiHeadlessMemory.Mutated(ref platform, state);
		return true;
	}

	public static uint Remove<TPlatform>(ref TPlatform platform, APTR state,
		APTR source, uint triggerAttribute, APTR destination, bool matchDestination)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var notificationSource = ResolveNotificationSource(ref platform, state,
			source, triggerAttribute);
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state,
			notificationSource);
		if (record.IsNull) return 0;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var sourceValue)) return 0;
		uint removed = 0;
		var current = sourceValue.Notifications;
		var previous = APTR.Null;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessNotificationCodec.TryRead(ref platform, current,
				out var currentNotification)) break;
			var next = currentNotification.Next;
			var matches = currentNotification.TriggerAttribute == triggerAttribute;
			if (matches && matchDestination)
				matches = currentNotification.Destination.Raw == destination.Raw;
			if (matches)
			{
				if (previous.IsNull) sourceValue.Notifications = next;
				else
				{
					if (!MuiHeadlessNotificationCodec.TryRead(ref platform, previous,
						out var previousNotification)) break;
					previousNotification.Next = next;
					if (!MuiHeadlessNotificationCodec.Write(ref platform, previous,
						previousNotification)) break;
				}
				FreeNotification(ref platform, current);
				removed++;
			}
			else previous = current;
			current = next;
		}
		if (removed != 0)
		{
			if (!MuiHeadlessObjectCodec.Write(ref platform, record,
				sourceValue)) return 0;
			MuiHeadlessMemory.Mutated(ref platform, state);
		}
		return removed;
	}

	// Remove notifications whose resolved destination is the object being
	// disposed.  Destination values are not always literal guest pointers:
	// MorphOS also permits self/ancestor destination tokens.  Resolve each
	// named notification against its still-live source record before unlinking
	// it, so disposing a parent cannot leave a child notification that would
	// later cross the platform callback seam with a stale target.
	internal static uint RemoveAllToObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR destination)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (destination.IsNull || !MuiHeadlessStateCodec.TryRead(ref platform,
			state, out var stateValue)) return 0;
		var currentRecord = stateValue.Objects;
		uint removed = 0;
		uint visited = 0;
		while (currentRecord.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessObjectCodec.TryRead(ref platform, currentRecord,
				out var sourceValue)) break;
			var nextRecord = sourceValue.Next;
			removed += RemoveDestinationFromSource(ref platform, state,
				currentRecord, destination);
			currentRecord = nextRecord;
		}
		if (removed != 0) MuiHeadlessMemory.Mutated(ref platform, state);
		return removed;
	}

	private static uint RemoveDestinationFromSource<TPlatform>(
		ref TPlatform platform, APTR state, APTR sourceRecord,
		APTR destination) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, sourceRecord,
			out var sourceValue)) return 0;
		uint removed = 0;
		uint visited = 0;
		var current = sourceValue.Notifications;
		var previous = APTR.Null;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessNotificationCodec.TryRead(ref platform, current,
				out var notification)) break;
			var next = notification.Next;
			var resolved = ResolveDestination(ref platform, sourceRecord,
				notification.Destination);
			if (resolved.Raw == destination.Raw)
			{
				if (previous.IsNull) sourceValue.Notifications = next;
				else
				{
					if (!MuiHeadlessNotificationCodec.TryRead(ref platform, previous,
						out var previousNotification)) break;
					previousNotification.Next = next;
					if (!MuiHeadlessNotificationCodec.Write(ref platform, previous,
						previousNotification)) break;
				}
				FreeNotification(ref platform, current);
				removed++;
			}
			else previous = current;
			current = next;
		}
		if (removed != 0 && !MuiHeadlessObjectCodec.Write(ref platform,
			sourceRecord, sourceValue)) return 0;
		return removed;
	}

	// A Listview exposes the child List as its public List attribute surface.
	// Notifications installed on those forwarded attributes therefore belong to
	// the child record, while Listview-owned click and selection signals remain
	// on the composite record. No shadow notification table or private offset is
	// needed; removal resolves the same named source deterministically.
	private static APTR ResolveNotificationSource<TPlatform>(
		ref TPlatform platform, APTR state, APTR source, uint attribute)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		// Keep the APTR result in one named local. Besides making the guest
		// ownership rule explicit, this avoids a conditional-expression merge
		// that the freestanding MC68000 lowering cannot represent when the
		// generic collection classifier is inlined.
		var resolved = source;
		if (MuiListviewCore.IsForwardedNotificationAttribute(attribute))
		{
			var collection = MuiListCore.Classify(ref platform, state, source);
			if (collection == MuiCollectionClass.Listview)
			{
				var child = MuiListviewCore.ChildList(ref platform, state, source);
				if (child.IsNotNull) resolved = child;
			}
		}
		return resolved;
	}

	internal static void DispatchAttributeChange<TPlatform>(ref TPlatform platform,
		APTR state, APTR sourceRecord, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return;
		var depth = stateValue.NotifyDepth;
		// The state record's named NotifySuppressionMethod field is used as a transient,
		// operation-local MUIA_NoNotifyMethod selector while an OM_SET tag list
		// is being applied. A zero value means that every matching notification
		// method remains eligible.
		var suppressedMethod = stateValue.NotifySuppressionMethod;
		if (depth >= MuiHeadlessLayout.MaximumNotificationDepth) return;
		stateValue.NotifyDepth = depth + 1;
		if (!MuiHeadlessStateCodec.Write(ref platform, state, stateValue)) return;
		var maximum = stateValue.NextSequence;
		if (maximum != 0) maximum--;
		uint completed = 0;
		uint operations = 0;
		while (operations++ < MuiHeadlessLayout.MaximumTraversal)
		{
			var item = FindNext(ref platform, sourceRecord, completed, maximum);
			if (item.IsNull) break;
			if (!MuiHeadlessNotificationCodec.TryRead(ref platform, item,
				out var notification)) break;
			var sequence = notification.Sequence;
			completed = sequence;
			var triggerAttribute = notification.TriggerAttribute;
			var triggerValue = notification.TriggerValue;
			if (triggerAttribute != attribute ||
				(triggerValue != (uint)Value.EveryTime && triggerValue != value))
				continue;
			var followCount = notification.FollowCount;
			var destinationValue = notification.Destination;
			var destination = ResolveDestination(ref platform, sourceRecord,
				destinationValue);
			// Notification destinations are guest object pointers.  Reject stale
			// or malformed destinations before crossing the platform callback seam;
			// this keeps a bad notification from turning into an unmapped bus read.
			if (destination.IsNull || !platform.IsMapped(destination, 1) ||
				followCount == 0 || followCount > 256)
				continue;
			var bytes = followCount * 4u;
			if (!MuiHeadlessNotificationCodec.TryGetPayload(ref platform, item,
				bytes, out var payload)) continue;
			if (suppressedMethod != 0 &&
				MuiNotifyFollowParameterSlotCodec.TryRead(ref platform, payload,
					out var methodSlot) && methodSlot.Value == suppressedMethod)
				continue;
			var message = MuiHeadlessMemory.Allocate(ref platform, bytes);
			if (message.IsNull) continue;
			platform.Copy(payload, message, bytes);
			for (var index = 0u; index < followCount; index++)
			{
				if (!MuiNotifyFollowParameterVectorMemoryCodec.TryGetEntry(
					ref platform, message, index, out var slotAddress)) continue;
				if (!MuiNotifyFollowParameterSlotCodec.TryRead(ref platform,
					slotAddress, out var slot)) continue;
				if (slot.Value == TriggerValue)
				{
					slot.Value = value;
					MuiNotifyFollowParameterSlotCodec.Write(ref platform,
						slotAddress, slot);
				}
				else if (slot.Value == NotTriggerValue)
				{
					slot.Value = value == 0 ? 1u : 0u;
					MuiNotifyFollowParameterSlotCodec.Write(ref platform,
						slotAddress, slot);
				}
			}
			platform.DoMethod(destination, message);
			platform.Clear(message, bytes);
			platform.Free(message, bytes);
		}
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out stateValue)) return;
		depth = stateValue.NotifyDepth;
		if (depth != 0) depth--;
		stateValue.NotifyDepth = depth;
		MuiHeadlessStateCodec.Write(ref platform, state, stateValue);
	}

	internal static void RemoveAll<TPlatform>(ref TPlatform platform, APTR state,
		APTR sourceRecord) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, sourceRecord,
			out var sourceValue)) return;
		var current = sourceValue.Notifications;
		sourceValue.Notifications = APTR.Null;
		if (!MuiHeadlessObjectCodec.Write(ref platform, sourceRecord,
			sourceValue)) return;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessNotificationCodec.TryRead(ref platform, current,
				out var notification)) break;
			var next = notification.Next;
			FreeNotification(ref platform, current);
			current = next;
		}
		MuiHeadlessMemory.Mutated(ref platform, state);
	}

	private static APTR FindNext<TPlatform>(ref TPlatform platform,
		APTR sourceRecord, uint afterSequence, uint maximumSequence)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, sourceRecord,
			out var sourceValue)) return APTR.Null;
		var current = sourceValue.Notifications;
		var selected = APTR.Null;
		var selectedSequence = uint.MaxValue;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessNotificationCodec.TryRead(ref platform, current,
				out var notification)) return APTR.Null;
			var sequence = notification.Sequence;
			if (sequence > afterSequence && sequence <= maximumSequence &&
				sequence < selectedSequence)
			{
				selected = current;
				selectedSequence = sequence;
			}
			current = notification.Next;
		}
		return selected;
	}

	private static APTR ResolveDestination<TPlatform>(ref TPlatform platform,
		APTR sourceRecord, APTR destination)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, sourceRecord,
			out var sourceValue)) return APTR.Null;
		if (destination.Raw == 1)
			return sourceValue.Boopsi;
		if (destination.Raw >= 4 && destination.Raw <= 6)
		{
			var parent = sourceValue.Parent;
			var levels = destination.Raw - 3;
			while (levels-- != 0 && parent.IsNotNull)
			{
				if (!MuiHeadlessObjectCodec.TryRead(ref platform, parent,
					out var parentValue)) return APTR.Null;
				parent = parentValue.Parent;
			}
			if (parent.IsNull) return APTR.FromPointer(0);
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, parent,
			out var destinationValue)) return APTR.Null;
			return destinationValue.Boopsi;
		}
		if (destination.Raw <= 6) return APTR.FromPointer(0);
		return destination;
	}

	private static void FreeNotification<TPlatform>(ref TPlatform platform,
		APTR item) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessNotificationCodec.TryRead(ref platform, item,
			out var notification)) return;
		var size = MuiHeadlessNotificationRecord.Size +
			notification.FollowCount * 4u;
		platform.Clear(item, size);
		platform.Free(item, size);
	}
}
