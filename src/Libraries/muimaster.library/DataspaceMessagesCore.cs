/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS Dataspace packets. APTR fields remain 32-bit on the 68k ABI and are
// exposed as named fields; the codec below is the only guest-wire boundary.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceMethodMessage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceAddMessage
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint DataOffset = 4;
	internal const uint LengthOffset = 8;
	internal const uint IdOffset = 12;
	internal uint MethodId;
	internal APTR Data;
	internal int Length;
	internal uint Id;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceFindMessage
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint IdOffset = 4;
	internal uint MethodId;
	internal uint Id;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceGetMessage
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint IdOffset = 4;
	internal const uint SizeStorageOffset = 8;
	internal uint MethodId;
	internal uint Id;
	internal APTR SizeStorage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceMergeMessage
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint DataspaceOffset = 4;
	internal uint MethodId;
	internal APTR Dataspace;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceRemoveMessage
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint IdOffset = 4;
	internal uint MethodId;
	internal uint Id;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceClearMessage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal uint MethodId;
}

internal enum MuiDataspacePacketKind : byte
{
	Method,
	Add,
	Find,
	Get,
	Merge,
	Remove,
	Clear,
}

internal enum MuiDataspaceField : byte
{
	MethodId,
	Data,
	Length,
	Id,
	SizeStorage,
	Dataspace,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDataspaceFieldCursor
{
	internal APTR Message;
	internal MuiDataspacePacketKind Packet;
	internal MuiDataspaceField Field;
}

// Struct-first guest-memory adapter for the fixed Dataspace packet family.
// Header reads admit the 4-byte method record; payload fields require their
// packet's complete named record before being accessed.
internal static class MuiDataspaceMessageMemoryCodec
{
	private static bool TryResolve(MuiDataspacePacketKind packet,
		MuiDataspaceField field, out uint offset, out uint recordSize)
	{
		recordSize = 0;
		switch (packet)
		{
			case MuiDataspacePacketKind.Method:
			case MuiDataspacePacketKind.Clear:
				if (field == MuiDataspaceField.MethodId)
				{
					offset = MuiDataspaceMethodMessage.MethodIdOffset;
					recordSize = MuiDataspaceMethodMessage.Size;
					return true;
				}
				break;
			case MuiDataspacePacketKind.Add:
				recordSize = MuiDataspaceAddMessage.Size;
				if (field == MuiDataspaceField.MethodId) { offset = MuiDataspaceAddMessage.MethodIdOffset; return true; }
				if (field == MuiDataspaceField.Data) { offset = MuiDataspaceAddMessage.DataOffset; return true; }
				if (field == MuiDataspaceField.Length) { offset = MuiDataspaceAddMessage.LengthOffset; return true; }
				if (field == MuiDataspaceField.Id) { offset = MuiDataspaceAddMessage.IdOffset; return true; }
				break;
			case MuiDataspacePacketKind.Find:
			case MuiDataspacePacketKind.Remove:
				recordSize = packet == MuiDataspacePacketKind.Find ?
					MuiDataspaceFindMessage.Size : MuiDataspaceRemoveMessage.Size;
				if (field == MuiDataspaceField.MethodId)
				{
					offset = MuiDataspaceFindMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiDataspaceField.Id)
				{
					offset = MuiDataspaceFindMessage.IdOffset;
					return true;
				}
				break;
			case MuiDataspacePacketKind.Get:
				recordSize = MuiDataspaceGetMessage.Size;
				if (field == MuiDataspaceField.MethodId) { offset = MuiDataspaceGetMessage.MethodIdOffset; return true; }
				if (field == MuiDataspaceField.Id) { offset = MuiDataspaceGetMessage.IdOffset; return true; }
				if (field == MuiDataspaceField.SizeStorage) { offset = MuiDataspaceGetMessage.SizeStorageOffset; return true; }
				break;
			case MuiDataspacePacketKind.Merge:
				recordSize = MuiDataspaceMergeMessage.Size;
				if (field == MuiDataspaceField.MethodId) { offset = MuiDataspaceMergeMessage.MethodIdOffset; return true; }
				if (field == MuiDataspaceField.Dataspace) { offset = MuiDataspaceMergeMessage.DataspaceOffset; return true; }
				break;
		}
		offset = 0;
		recordSize = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspacePacketKind packet, MuiDataspaceField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset, out var recordSize) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, recordSize))
			return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiDataspaceAddMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspacePacketKind packet, MuiDataspaceField field,
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
		APTR message, MuiDataspacePacketKind packet, MuiDataspaceField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for existing typed cursor callers. The live
// packet codecs route through the named-record adapter above.
internal static class MuiDataspaceFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiDataspaceFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDataspaceMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Packet, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspacePacketKind packet, MuiDataspaceField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform, message, packet,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspacePacketKind packet, MuiDataspaceField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform, message, packet,
			field, value);
}

// Central codec for the fixed Dataspace packet family. All consumers receive
// named records; the explicit offsets below are confined to this packed ABI
// adapter and are never repeated by dispatch or store code.
internal static class MuiDataspaceMessageCodec
{
	internal const uint AddMethod = 0x80423366;
	internal const uint ClearMethod = 0x8042B6C9;
	internal const uint FindMethod = 0x8042832C;
	internal const uint GetMethod = 0x8042483F;
	internal const uint MergeMethod = 0x80423E2B;
	internal const uint RemoveMethod = 0x8042DCE1;

	// Keep scalar method admission at the guest ABI boundary; Dataspace
	// consumers continue to use the named fixed-width method record.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiDataspacePacketKind.Method, MuiDataspaceField.MethodId, out methodId);
	}

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiDataspaceMethodMessage.Size)) return false;
		if (!TryReadMethodIdValue(ref platform, message, out packet.MethodId))
			return false;
		return true;
	}

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, out uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		method = 0;
		if (!TryReadMethodId(ref platform, message, out var packet)) return false;
		method = packet.MethodId;
		return true;
	}

	internal static bool TryReadAdd<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceAddMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiDataspaceAddMessage.Size,
			AddMethod)) return false;
		if (!MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiDataspacePacketKind.Add, MuiDataspaceField.MethodId,
			out packet.MethodId) ||
			!MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiDataspacePacketKind.Add, MuiDataspaceField.Data, out var rawData) ||
			!MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiDataspacePacketKind.Add, MuiDataspaceField.Length, out var rawLength) ||
			!MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiDataspacePacketKind.Add, MuiDataspaceField.Id, out packet.Id))
			return false;
		packet.Data = APTR.FromPointer(rawData);
		packet.Length = unchecked((int)rawLength);
		return true;
	}

	internal static bool TryReadFind<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceFindMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiDataspaceFindMessage.Size,
			FindMethod)) return false;
		return MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiDataspacePacketKind.Find, MuiDataspaceField.MethodId,
			out packet.MethodId) &&
			MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiDataspacePacketKind.Find, MuiDataspaceField.Id, out packet.Id);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiDataspaceGetMessage.Size,
			GetMethod)) return false;
		if (!MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiDataspacePacketKind.Get, MuiDataspaceField.MethodId,
			out packet.MethodId) ||
			!MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiDataspacePacketKind.Get, MuiDataspaceField.Id, out packet.Id) ||
			!MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiDataspacePacketKind.Get, MuiDataspaceField.SizeStorage,
				out var rawStorage)) return false;
		packet.SizeStorage = APTR.FromPointer(rawStorage);
		return true;
	}

	internal static bool TryReadMerge<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceMergeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiDataspaceMergeMessage.Size,
			MergeMethod)) return false;
		return MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiDataspacePacketKind.Merge, MuiDataspaceField.MethodId,
			out packet.MethodId) &&
			MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiDataspacePacketKind.Merge, MuiDataspaceField.Dataspace,
				out var rawDataspace) && SetDataspace(out packet.Dataspace, rawDataspace);
	}

	internal static bool TryReadRemove<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceRemoveMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiDataspaceRemoveMessage.Size,
			RemoveMethod)) return false;
		return MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiDataspacePacketKind.Remove, MuiDataspaceField.MethodId,
			out packet.MethodId) &&
			MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiDataspacePacketKind.Remove, MuiDataspaceField.Id, out packet.Id);
	}

	internal static bool TryReadClear<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceClearMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiDataspaceClearMessage.Size,
			ClearMethod)) return false;
		return MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiDataspacePacketKind.Clear, MuiDataspaceField.MethodId,
			out packet.MethodId);
	}

	internal static bool TryWriteAdd<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceAddMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMapped(ref platform, message, MuiDataspaceAddMessage.Size))
			return false;
		return MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiDataspacePacketKind.Add, MuiDataspaceField.MethodId, AddMethod) &&
			MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiDataspacePacketKind.Add, MuiDataspaceField.Data, packet.Data.Raw) &&
				MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform, message,
					MuiDataspacePacketKind.Add, MuiDataspaceField.Length,
					unchecked((uint)packet.Length)) &&
					MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform, message,
						MuiDataspacePacketKind.Add, MuiDataspaceField.Id, packet.Id);
	}

	internal static bool TryWriteFind<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceFindMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMapped(ref platform, message, MuiDataspaceFindMessage.Size))
			return false;
		return MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiDataspacePacketKind.Find, MuiDataspaceField.MethodId, FindMethod) &&
			MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiDataspacePacketKind.Find, MuiDataspaceField.Id, packet.Id);
	}

	internal static bool TryWriteGet<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMapped(ref platform, message, MuiDataspaceGetMessage.Size))
			return false;
		return MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiDataspacePacketKind.Get, MuiDataspaceField.MethodId, GetMethod) &&
			MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiDataspacePacketKind.Get, MuiDataspaceField.Id, packet.Id) &&
				MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform, message,
					MuiDataspacePacketKind.Get, MuiDataspaceField.SizeStorage,
					packet.SizeStorage.Raw);
	}

	internal static bool TryWriteMerge<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceMergeMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMapped(ref platform, message, MuiDataspaceMergeMessage.Size))
			return false;
		return MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiDataspacePacketKind.Merge, MuiDataspaceField.MethodId, MergeMethod) &&
			MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiDataspacePacketKind.Merge, MuiDataspaceField.Dataspace,
				packet.Dataspace.Raw);
	}

	internal static bool TryWriteRemove<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceRemoveMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMapped(ref platform, message, MuiDataspaceRemoveMessage.Size))
			return false;
		return MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiDataspacePacketKind.Remove, MuiDataspaceField.MethodId, RemoveMethod) &&
			MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiDataspacePacketKind.Remove, MuiDataspaceField.Id, packet.Id);
	}

	internal static bool TryWriteClear<TPlatform>(ref TPlatform platform,
		APTR message, MuiDataspaceClearMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMapped(ref platform, message, MuiDataspaceClearMessage.Size))
			return false;
		return MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiDataspacePacketKind.Clear, MuiDataspaceField.MethodId, ClearMethod);
	}

	private static bool IsPacket<TPlatform>(ref TPlatform platform,
		APTR message, uint size, uint method)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadMethodId(ref platform, message, out var header) &&
		header.MethodId == method && IsMapped(ref platform, message, size);

	private static bool IsMapped<TPlatform>(ref TPlatform platform,
		APTR message, uint size) where TPlatform : struct, IMuiGuestMemory =>
		message.IsNotNull && platform.IsMapped(message, size);

	private static bool SetDataspace(out APTR target, uint raw)
	{
		target = APTR.FromPointer(raw);
		return true;
	}
}

// Fixed packet codecs for the Dataspace superclass. The live store remains
// in MuiStoreCore; this type owns only ABI validation and named field decode.
public static class MuiDataspaceMessageCore
{
	public const uint AddMethod = MuiDataspaceMessageCodec.AddMethod;
	public const uint ClearMethod = MuiDataspaceMessageCodec.ClearMethod;
	public const uint FindMethod = MuiDataspaceMessageCodec.FindMethod;
	public const uint GetMethod = MuiDataspaceMessageCodec.GetMethod;
	public const uint MergeMethod = MuiDataspaceMessageCodec.MergeMethod;
	public const uint RemoveMethod = MuiDataspaceMessageCodec.RemoveMethod;

	internal static bool TryReadAdd<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceAddMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDataspaceMessageCodec.TryReadAdd(ref platform, message, out packet);

	internal static bool TryReadFind<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceFindMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDataspaceMessageCodec.TryReadFind(ref platform, message, out packet);

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDataspaceMessageCodec.TryReadGet(ref platform, message, out packet);

	internal static bool TryReadMerge<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceMergeMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDataspaceMessageCodec.TryReadMerge(ref platform, message, out packet);

	internal static bool TryReadRemove<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDataspaceRemoveMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDataspaceMessageCodec.TryReadRemove(ref platform, message, out packet);

	public static bool WriteAddRecord<TPlatform>(ref TPlatform platform,
		APTR message, APTR data, int length, uint id)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiDataspaceAddMessage);
		packet.MethodId = AddMethod;
		packet.Data = data;
		packet.Length = length;
		packet.Id = id;
		return MuiDataspaceMessageCodec.TryWriteAdd(ref platform, message,
			packet);
	}

	public static bool WriteFindRecord<TPlatform>(ref TPlatform platform,
		APTR message, uint id) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiDataspaceFindMessage);
		packet.MethodId = FindMethod;
		packet.Id = id;
		return MuiDataspaceMessageCodec.TryWriteFind(ref platform, message,
			packet);
	}

	public static bool WriteGetRecord<TPlatform>(ref TPlatform platform,
		APTR message, uint id, APTR sizeStorage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiDataspaceGetMessage);
		packet.MethodId = GetMethod;
		packet.Id = id;
		packet.SizeStorage = sizeStorage;
		return MuiDataspaceMessageCodec.TryWriteGet(ref platform, message,
			packet);
	}

	public static bool WriteMergeRecord<TPlatform>(ref TPlatform platform,
		APTR message, APTR dataspace) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiDataspaceMergeMessage);
		packet.MethodId = MergeMethod;
		packet.Dataspace = dataspace;
		return MuiDataspaceMessageCodec.TryWriteMerge(ref platform, message,
			packet);
	}

	public static bool WriteRemoveRecord<TPlatform>(ref TPlatform platform,
		APTR message, uint id) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiDataspaceRemoveMessage);
		packet.MethodId = RemoveMethod;
		packet.Id = id;
		return MuiDataspaceMessageCodec.TryWriteRemove(ref platform, message,
			packet);
	}

	public static bool WriteClearRecord<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiDataspaceClearMessage);
		packet.MethodId = ClearMethod;
		return MuiDataspaceMessageCodec.TryWriteClear(ref platform, message,
			packet);
	}

	// Struct-only native qualification seam. It returns the decoded selector
	// field so a freestanding fixture can verify every packet without pulling
	// the larger object/store lifecycle into its closure.
	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiDataspaceMessageCodec.TryReadMethod(ref platform, message,
			out var method)) return 0;
		switch (method)
		{
			case AddMethod:
				return TryReadAdd(ref platform, message, out var add) ? add.Id : 0;
			case FindMethod:
				return TryReadFind(ref platform, message, out var find) ? find.Id : 0;
			case GetMethod:
				return TryReadGet(ref platform, message, out var get) ?
					get.SizeStorage.Raw : 0;
			case MergeMethod:
				return TryReadMerge(ref platform, message, out var merge) ?
					merge.Dataspace.Raw : 0;
			case RemoveMethod:
				return TryReadRemove(ref platform, message, out var remove) ?
					remove.Id : 0;
			case ClearMethod:
				return MuiDataspaceMessageCodec.TryReadClear(ref platform,
					message, out _) ? 1u : 0u;
		}
		return 0;
	}
}
