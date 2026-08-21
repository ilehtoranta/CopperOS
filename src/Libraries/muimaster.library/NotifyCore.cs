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
	public uint MethodId;
	public uint TriggerAttribute;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiKillNotifyObjectMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint TriggerAttribute;
	public uint Destination;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSetAttributeMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Attribute;
	public uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMultiSetMessage
{
	public const uint Size = 16;
	public uint MethodId;
	public uint Attribute;
	public uint Value;
	public uint FirstObject;
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
	internal APTR Message;
	internal MuiNotifyInlineVectorKind Kind;
	internal uint Index;
}

internal static class MuiNotifyInlineVectorCursorCodec
{
	internal static bool TryGetAddress(MuiNotifyInlineVectorCursor cursor,
		out APTR address)
	{
		address = APTR.Null;
		uint baseOffset;
		switch (cursor.Kind)
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
		if (cursor.Message.IsNull || cursor.Message.Raw >
			uint.MaxValue - baseOffset) return false;
		var vector = APTR.FromPointer(cursor.Message.Raw + baseOffset);
		if (cursor.Index > (uint.MaxValue - vector.Raw) /
			MuiNotifyInlineVectorCursor.EntrySize) return false;
		var offset = cursor.Index * MuiNotifyInlineVectorCursor.EntrySize;
		if (vector.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector.Raw + offset);
		return true;
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

internal static class MuiMultiSetTargetVectorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiMultiSetTargetVectorCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >=
			MuiMultiSetTargetVectorCursor.MaximumEntries || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiMultiSetTargetVectorCursor.EntrySize) return false;
		var offset = cursor.Index *
			MuiMultiSetTargetVectorCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address,
			MuiMultiSetTargetVectorCursor.EntrySize);
	}
}

internal static class MuiMultiSetTargetEntryCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiMultiSetTargetEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiMultiSetTargetEntry.Size)) return false;
		value.Target = APTR.FromPointer(platform.ReadUInt32(address, 0));
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiMultiSetTargetEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiMultiSetTargetEntry.Size)) return false;
		platform.WriteUInt32(address, 0, value.Target.Raw);
		return true;
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

internal static class MuiNotifyFollowParameterVectorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiNotifyFollowParameterVectorCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >=
			MuiNotifyFollowParameterVectorCursor.MaximumEntries || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiNotifyFollowParameterVectorCursor.EntrySize) return false;
		var offset = cursor.Index *
			MuiNotifyFollowParameterVectorCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address,
			MuiNotifyFollowParameterVectorCursor.EntrySize);
	}
}

internal static class MuiNotifyFollowParameterSlotCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiNotifyFollowParameterSlot slot)
		where TPlatform : struct, IMuiGuestMemory
	{
		slot = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiNotifyFollowParameterSlot.Size)) return false;
		slot.Value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiNotifyFollowParameterSlot slot)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiNotifyFollowParameterSlot.Size)) return false;
		platform.WriteUInt32(address, 0, slot.Value);
		return true;
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
		if (address.IsNull || !platform.IsMapped(address,
			MuiNotifyConfigStorage.Size)) return false;
		value.Value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiNotifyConfigStorage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiNotifyConfigStorage.Size)) return false;
		platform.WriteUInt32(address, 0, value.Value);
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFindObjectMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint FindObject;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNotifyMethodMessage
{
	public const uint Size = 4;
	public uint MethodId;
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

internal static class MuiNotifyPacketFieldCursorCodec
{
	private static bool TryResolve(MuiNotifyPacketKind packet,
		MuiNotifyPacketField field, out uint offset)
	{
		switch (packet)
		{
			case MuiNotifyPacketKind.Notify:
				if (field == MuiNotifyPacketField.MethodId) { offset = 0; return true; }
				if (field == MuiNotifyPacketField.TriggerAttribute) { offset = 4; return true; }
				if (field == MuiNotifyPacketField.TriggerValue) { offset = 8; return true; }
				if (field == MuiNotifyPacketField.Destination) { offset = 12; return true; }
				if (field == MuiNotifyPacketField.FollowCount) { offset = 16; return true; }
				break;
			case MuiNotifyPacketKind.KillNotify:
				if (field == MuiNotifyPacketField.MethodId) { offset = 0; return true; }
				if (field == MuiNotifyPacketField.TriggerAttribute) { offset = 4; return true; }
				break;
			case MuiNotifyPacketKind.KillNotifyObject:
				if (field == MuiNotifyPacketField.MethodId) { offset = 0; return true; }
				if (field == MuiNotifyPacketField.TriggerAttribute) { offset = 4; return true; }
				if (field == MuiNotifyPacketField.Destination) { offset = 8; return true; }
				break;
			case MuiNotifyPacketKind.Set:
				if (field == MuiNotifyPacketField.MethodId) { offset = 0; return true; }
				if (field == MuiNotifyPacketField.Attribute) { offset = 4; return true; }
				if (field == MuiNotifyPacketField.Value) { offset = 8; return true; }
				break;
			case MuiNotifyPacketKind.MultiSet:
				if (field == MuiNotifyPacketField.MethodId) { offset = 0; return true; }
				if (field == MuiNotifyPacketField.Attribute) { offset = 4; return true; }
				if (field == MuiNotifyPacketField.Value) { offset = 8; return true; }
				if (field == MuiNotifyPacketField.FirstObject) { offset = 12; return true; }
				break;
			case MuiNotifyPacketKind.FindObject:
				if (field == MuiNotifyPacketField.MethodId) { offset = 0; return true; }
				if (field == MuiNotifyPacketField.FindObject) { offset = 4; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiNotifyPacketFieldCursor cursor, out APTR address)
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
		APTR message, MuiNotifyPacketKind packet, MuiNotifyPacketField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiNotifyPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyPacketKind packet, MuiNotifyPacketField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiNotifyPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
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
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiNotifyMethodMessage.Size)) return false;
		return MuiNotifyPacketFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiNotifyPacketKind.Notify, MuiNotifyPacketField.MethodId,
			out value.MethodId);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool TryReadNotify<TPlatform>(ref TPlatform platform,
		ref PacketAddress request, out MuiNotifyMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadMethodId(ref platform, request.Address, out var header) ||
			header.MethodId != request.Method || !platform.IsMapped(request.Address,
			MuiNotifyMessage.Size)) return false;
		if (!MuiNotifyPacketFieldCursorCodec.TryReadUInt32(ref platform,
			request.Address, MuiNotifyPacketKind.Notify,
			MuiNotifyPacketField.TriggerAttribute, out value.TriggerAttribute) ||
			!MuiNotifyPacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiNotifyPacketKind.Notify,
				MuiNotifyPacketField.TriggerValue, out value.TriggerValue) ||
			!MuiNotifyPacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiNotifyPacketKind.Notify,
				MuiNotifyPacketField.Destination, out value.Destination) ||
			!MuiNotifyPacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiNotifyPacketKind.Notify,
				MuiNotifyPacketField.FollowCount, out value.FollowCount)) return false;
		value.MethodId = header.MethodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool TryReadKillNotify<TPlatform>(
		ref TPlatform platform, ref PacketAddress request,
		out MuiKillNotifyMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadMethodId(ref platform, request.Address, out var header) ||
			header.MethodId != request.Method || !platform.IsMapped(request.Address,
			MuiKillNotifyMessage.Size)) return false;
		if (!MuiNotifyPacketFieldCursorCodec.TryReadUInt32(ref platform,
			request.Address, MuiNotifyPacketKind.KillNotify,
			MuiNotifyPacketField.TriggerAttribute, out value.TriggerAttribute))
			return false;
		value.MethodId = header.MethodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool TryReadKillNotifyObject<TPlatform>(
		ref TPlatform platform, ref PacketAddress request,
		out MuiKillNotifyObjectMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadMethodId(ref platform, request.Address, out var header) ||
			header.MethodId != request.Method || !platform.IsMapped(request.Address,
			MuiKillNotifyObjectMessage.Size)) return false;
		if (!MuiNotifyPacketFieldCursorCodec.TryReadUInt32(ref platform,
			request.Address, MuiNotifyPacketKind.KillNotifyObject,
			MuiNotifyPacketField.TriggerAttribute, out value.TriggerAttribute) ||
			!MuiNotifyPacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiNotifyPacketKind.KillNotifyObject,
				MuiNotifyPacketField.Destination, out value.Destination)) return false;
		value.MethodId = header.MethodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		ref PacketAddress request, out MuiSetAttributeMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadMethodId(ref platform, request.Address, out var header) ||
			header.MethodId != request.Method || !platform.IsMapped(request.Address,
			MuiSetAttributeMessage.Size) ||
			(request.Method != MuiNotifyCore.SetMethod &&
				request.Method != MuiNotifyCore.NoNotifySetMethod)) return false;
		if (!MuiNotifyPacketFieldCursorCodec.TryReadUInt32(ref platform,
			request.Address, MuiNotifyPacketKind.Set,
			MuiNotifyPacketField.Attribute, out value.Attribute) ||
			!MuiNotifyPacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiNotifyPacketKind.Set,
				MuiNotifyPacketField.Value, out value.Value)) return false;
		value.MethodId = header.MethodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool TryReadMultiSet<TPlatform>(ref TPlatform platform,
		ref PacketAddress request, out MuiMultiSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadMethodId(ref platform, request.Address, out var header) ||
			header.MethodId != request.Method || !platform.IsMapped(request.Address,
			MuiMultiSetMessage.Size) ||
			request.Method != MuiNotifyCore.MultiSetMethod) return false;
		if (!MuiNotifyPacketFieldCursorCodec.TryReadUInt32(ref platform,
			request.Address, MuiNotifyPacketKind.MultiSet,
			MuiNotifyPacketField.Attribute, out value.Attribute) ||
			!MuiNotifyPacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiNotifyPacketKind.MultiSet,
				MuiNotifyPacketField.Value, out value.Value) ||
			!MuiNotifyPacketFieldCursorCodec.TryReadUInt32(ref platform,
				request.Address, MuiNotifyPacketKind.MultiSet,
				MuiNotifyPacketField.FirstObject, out value.FirstObject)) return false;
		value.MethodId = header.MethodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool TryReadFindObject<TPlatform>(ref TPlatform platform,
		ref PacketAddress request, out MuiFindObjectMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadMethodId(ref platform, request.Address, out var header) ||
			header.MethodId != request.Method || !platform.IsMapped(request.Address,
			MuiFindObjectMessage.Size) ||
			request.Method != MuiNotifyCore.FindObjectMethod) return false;
		if (!MuiNotifyPacketFieldCursorCodec.TryReadUInt32(ref platform,
			request.Address, MuiNotifyPacketKind.FindObject,
			MuiNotifyPacketField.FindObject, out value.FindObject)) return false;
		value.MethodId = header.MethodId;
		return true;
	}

	internal static APTR FollowParameters(APTR address)
	{
		var cursor = default(MuiNotifyInlineVectorCursor);
		cursor.Message = address;
		cursor.Kind = MuiNotifyInlineVectorKind.FollowParameters;
		return MuiNotifyInlineVectorCursorCodec.TryGetAddress(cursor,
			out var parameters) ? parameters : APTR.Null;
	}

	internal static APTR MultiSetVector(APTR address)
	{
		var cursor = default(MuiNotifyInlineVectorCursor);
		cursor.Message = address;
		cursor.Kind = MuiNotifyInlineVectorKind.MultiSetTargets;
		return MuiNotifyInlineVectorCursorCodec.TryGetAddress(cursor,
			out var vector) ? vector : APTR.Null;
	}
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
		=> MuiNotifyPacketCodec.FollowParameters(message);

	internal static APTR MultiSetVector<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
		=> MuiNotifyPacketCodec.MultiSetVector(message);

	public static bool MultiSet<TPlatform>(ref TPlatform platform, APTR state,
		APTR executor, uint attribute, uint value, APTR firstObject,
		APTR vector) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, executor).IsNull)
			return false;
		if (firstObject.IsNull || vector.IsNull) return false;
		if (!CountMultiSetTargets(ref platform, state, firstObject, vector,
			out var count)) return false;
		for (var index = 0u; index < count; index++)
		{
			var target = firstObject;
			if (index != 0 && !TryReadMultiSetTarget(ref platform, vector,
				index - 1, out target)) return false;
			if (target.IsNull ||
				(target.Raw != executor.Raw &&
					!MuiHeadlessObjectCore.SetAttribute(ref platform, state, target,
						attribute, value, true))) return false;
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

	private static bool CountMultiSetTargets<TPlatform>(ref TPlatform platform,
		APTR state, APTR firstObject, APTR vector, out uint count)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		count = 0;
		var current = firstObject;
		while (count < MaximumMultiSetTargets && current.IsNotNull)
		{
			if (MuiHeadlessObjectCore.FindObject(ref platform, state, current).IsNull)
				return false;
			count++;
			if (!TryReadMultiSetTarget(ref platform, vector, count - 1,
				out current)) return false;
		}
		return count != 0 && current.IsNull;
	}

	private static bool TryReadMultiSetTarget<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR target)
		where TPlatform : struct, IMuiGuestMemory
	{
		target = APTR.Null;
		var cursor = default(MuiMultiSetTargetVectorCursor);
		cursor.Base = vector;
		cursor.Index = index;
		if (!MuiMultiSetTargetVectorCodec.TryGetEntry(ref platform, cursor,
			out var slot)) return false;
		if (!MuiMultiSetTargetEntryCodec.TryRead(ref platform, slot,
			out var entry)) return false;
		target = entry.Target;
		return true;
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
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, source);
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
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, source);
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

	internal static void DispatchAttributeChange<TPlatform>(ref TPlatform platform,
		APTR state, APTR sourceRecord, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return;
		var depth = stateValue.NotifyDepth;
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
			if (destination.IsNull || followCount == 0 || followCount > 256)
				continue;
			var bytes = followCount * 4u;
			var message = MuiHeadlessMemory.Allocate(ref platform, bytes);
			if (message.IsNull) continue;
			if (!MuiHeadlessNotificationCodec.TryGetPayload(ref platform, item,
				bytes, out var payload))
			{
				platform.Clear(message, bytes);
				platform.Free(message, bytes);
				continue;
			}
			platform.Copy(payload, message, bytes);
			var cursor = default(MuiNotifyFollowParameterVectorCursor);
			cursor.Base = message;
			for (var index = 0u; index < followCount; index++)
			{
				cursor.Index = index;
				if (!MuiNotifyFollowParameterVectorCodec.TryGetEntry(
					ref platform, cursor, out var slotAddress)) continue;
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
