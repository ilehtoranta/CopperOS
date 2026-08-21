/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Fixed guest-memory layout for the per-object MG09 Process/Slave specialist
// sidecar.
//
// Process.mui and Slave.mui are both Semaphore.mui subclasses. Like the menu
// family, their defining behavior is layered over a real headless object rather
// than a second object system: every instance is a real MuiHeadlessObjectCore
// object, its scalar attributes and runtime notifications flow through the
// frozen object-attribute path, and shared-instance locking is delegated to the
// frozen MuiSemaphoreCore (which uses the object record's own semaphore fields).
//
// The only additive per-object state the frozen object cannot express is the
// Process/Slave-specific bookkeeping below: the class discriminator, the legal
// Process state machine, the opaque scheduler task token, the class-owned
// copied Name block, the Slave setup/dispatch balance and reentrancy guard, the
// accumulated received-signal mask, and the last Error code. That bookkeeping
// lives in this small guest-resident sidecar, attached through a single private
// attribute id and freed by the lifecycle. The frozen headless/family/object/
// semaphore cores, dispatchers and platform aggregates are not modified.
internal static class MuiProcessSpecialistLayout
{
	public const uint Magic = 0x4D505243;   // "MPRC"
	public const uint InstanceSize = MuiProcessSpecialistRecord.Size;

	// Private attribute id linking a headless object to its process/slave
	// sidecar. Well outside the 0x8042xxxx MUI attribute range, so it can never
	// collide with a documented Get/Set.
	public const uint SidecarAttribute = 0x7F505243u;

	// Flags.
	public const uint FlagAutoLaunch = 1u << 0;   // MUIA_Process_AutoLaunch latch

	// Bounded traversal / argument marshalling.
	public const uint MaximumString = 4096;
	public const uint MaximumDispatchArgs = 16;   // documented automagic bound

	// Bounded stack / priority validation (grounded in exec task limits).
	public const int MinPriority = -128;
	public const int MaxPriority = 127;
	public const uint MinStackSize = 1024;
	public const uint MaxStackSize = 0x00100000;   // 1 MiB ceiling
	public const uint DefaultStackSize = 8192;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiProcessSpecialistRecord
{
	internal const uint Size = 52;
	internal uint Magic;
	internal uint Class;
	internal uint State;
	internal uint TaskToken;
	internal APTR NameOwned;
	internal uint NameOwnedSize;
	internal uint Error;
	internal uint SignalsReceived;
	internal uint Flags;
	internal uint DispatchDepth;
	internal uint SetupState;
	internal uint NotifyCount;
	internal uint NotifyAttribute;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiProcessDispatchPacketHeader
{
	internal const uint Size = 8;
	internal uint ArgumentCount;
	internal uint MethodId;
}

// Each dispatch argument is one ULONG in the caller-owned inline vector. Keep
// that element as a named wire record so packet consumers never duplicate the
// vector's four-byte value layout at call sites.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiProcessDispatchArgumentSlot
{
	internal const uint Size = 4;
	internal uint Value;
}

internal enum MuiProcessDispatchArgumentSlotField : byte
{
	Value,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiProcessDispatchArgumentSlotFieldCursor
{
	internal APTR Record;
	internal MuiProcessDispatchArgumentSlotField Field;
}

internal static class MuiProcessDispatchArgumentSlotFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiProcessDispatchArgumentSlotFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Field != MuiProcessDispatchArgumentSlotField.Value ||
			cursor.Record.IsNull || !platform.IsMapped(cursor.Record,
				MuiProcessDispatchArgumentSlot.Size)) return false;
		address = cursor.Record;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiProcessDispatchArgumentSlotField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiProcessDispatchArgumentSlotFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiProcessDispatchArgumentSlotField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiProcessDispatchArgumentSlotFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiProcessDispatchArgumentSlotCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiProcessDispatchArgumentSlot slot)
		where TPlatform : struct, IMuiGuestMemory
	{
		slot = default;
		if (!MuiProcessDispatchArgumentSlotFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiProcessDispatchArgumentSlotField.Value,
			out slot.Value)) return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiProcessDispatchArgumentSlot slot)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiProcessDispatchArgumentSlotFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiProcessDispatchArgumentSlotField.Value,
			slot.Value);
	}
}

// Process dispatch uses two related inline ULONG vectors: the caller packet
// starts after an {ArgumentCount, MethodID} header, while the generated
// BOOPSI method starts after MethodID alone. A semantic kind keeps both packet
// boundaries named and lets read/write paths share one cursor.
internal enum MuiProcessArgumentVectorKind : byte
{
	DispatchPacket,
	MethodMessage,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiProcessArgumentCursor
{
	internal APTR Message;
	internal uint Index;
	internal uint Count;
	internal MuiProcessArgumentVectorKind Kind;
}

internal enum MuiProcessRecordKind : byte
{
	DispatchHeader,
	Specialist,
}

internal enum MuiProcessRecordField : byte
{
	ArgumentCount,
	MethodId,
	Magic,
	Class,
	State,
	TaskToken,
	NameOwned,
	NameOwnedSize,
	Error,
	SignalsReceived,
	Flags,
	DispatchDepth,
	SetupState,
	NotifyCount,
	NotifyAttribute,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiProcessRecordFieldCursor
{
	internal APTR Address;
	internal MuiProcessRecordKind Record;
	internal MuiProcessRecordField Field;
}

internal static class MuiProcessRecordFieldCursorCodec
{
	private static bool TryResolve(MuiProcessRecordKind record,
		MuiProcessRecordField field, out uint offset, out uint size)
	{
		offset = 0;
		size = 0;
		switch (record)
		{
			case MuiProcessRecordKind.DispatchHeader:
				size = MuiProcessDispatchPacketHeader.Size;
				offset = field switch
				{
					MuiProcessRecordField.ArgumentCount => 0,
					MuiProcessRecordField.MethodId => 4,
					_ => uint.MaxValue,
				};
				break;
			case MuiProcessRecordKind.Specialist:
				size = MuiProcessSpecialistRecord.Size;
				offset = field switch
				{
					MuiProcessRecordField.Magic => 0,
					MuiProcessRecordField.Class => 4,
					MuiProcessRecordField.State => 8,
					MuiProcessRecordField.TaskToken => 12,
					MuiProcessRecordField.NameOwned => 16,
					MuiProcessRecordField.NameOwnedSize => 20,
					MuiProcessRecordField.Error => 24,
					MuiProcessRecordField.SignalsReceived => 28,
					MuiProcessRecordField.Flags => 32,
					MuiProcessRecordField.DispatchDepth => 36,
					MuiProcessRecordField.SetupState => 40,
					MuiProcessRecordField.NotifyCount => 44,
					MuiProcessRecordField.NotifyAttribute => 48,
					_ => uint.MaxValue,
				};
				break;
		}
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiProcessRecordFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Record, cursor.Field, out var offset,
			out var size) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, size)) return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiProcessRecordKind record, MuiProcessRecordField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiProcessRecordFieldCursor);
		cursor.Address = address;
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiProcessRecordKind record, MuiProcessRecordField field,
		uint value) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiProcessRecordFieldCursor);
		cursor.Address = address;
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiProcessArgumentCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiProcessArgumentCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Count == 0 || cursor.Count >
			MuiProcessSpecialistLayout.MaximumDispatchArgs || cursor.Index >=
			cursor.Count) return false;
		uint baseOffset;
		switch (cursor.Kind)
		{
			case MuiProcessArgumentVectorKind.DispatchPacket:
				baseOffset = MuiProcessDispatchPacketHeader.Size;
				break;
			case MuiProcessArgumentVectorKind.MethodMessage:
				baseOffset = 4;
				break;
			default:
				return false;
		}
		if (cursor.Message.IsNull || cursor.Message.Raw >
			uint.MaxValue - baseOffset) return false;
		var vector = APTR.FromPointer(cursor.Message.Raw + baseOffset);
		if (cursor.Index > (uint.MaxValue - vector.Raw) /
			MuiProcessDispatchArgumentSlot.Size) return false;
		var offset = cursor.Index * MuiProcessDispatchArgumentSlot.Size;
		if (vector.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector.Raw + offset);
		return platform.IsMapped(address, MuiProcessDispatchArgumentSlot.Size);
	}
}

// MUIM_Slave_Dispatch carries a fixed header followed by a bounded inline
// argument vector. The header is a named record; only this codec knows that
// the vector begins after the two ULONG header fields.
internal static class MuiProcessDispatchPacketCodec
{
	internal static bool TryReadHeader<TPlatform>(ref TPlatform platform,
		APTR address, out MuiProcessDispatchPacketHeader packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiProcessDispatchPacketHeader.Size) ||
			!MuiProcessRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiProcessRecordKind.DispatchHeader,
				MuiProcessRecordField.ArgumentCount, out packet.ArgumentCount) ||
			!MuiProcessRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiProcessRecordKind.DispatchHeader,
				MuiProcessRecordField.MethodId, out packet.MethodId)) return false;
		if (packet.ArgumentCount > MuiProcessSpecialistLayout.MaximumDispatchArgs ||
			packet.MethodId == 0) return false;
		return platform.IsMapped(address, 8u + packet.ArgumentCount * 4u);
	}

	internal static bool TryReadArgument<TPlatform>(ref TPlatform platform,
		APTR address, MuiProcessDispatchPacketHeader packet, uint index,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (index >= packet.ArgumentCount || index >=
			MuiProcessSpecialistLayout.MaximumDispatchArgs || address.IsNull ||
			!platform.IsMapped(address, 8u + packet.ArgumentCount * 4u))
			return false;
		var cursor = default(MuiProcessArgumentCursor);
		cursor.Message = address;
		cursor.Index = index;
		cursor.Count = packet.ArgumentCount;
		cursor.Kind = MuiProcessArgumentVectorKind.DispatchPacket;
		if (!MuiProcessArgumentCursorCodec.TryGetEntry(ref platform, cursor,
			out var slot)) return false;
		if (!MuiProcessDispatchArgumentSlotCodec.TryRead(ref platform, slot,
			out var argument)) return false;
		value = argument.Value;
		return true;
	}
}

internal static class MuiProcessSpecialistCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiProcessSpecialistRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiProcessSpecialistRecord.Size)) return false;
		if (!MuiProcessRecordFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiProcessRecordKind.Specialist,
			MuiProcessRecordField.Magic, out record.Magic) ||
			!MuiProcessRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist, MuiProcessRecordField.Class,
				out record.Class) ||
			!MuiProcessRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist, MuiProcessRecordField.State,
				out record.State) ||
			!MuiProcessRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist, MuiProcessRecordField.TaskToken,
				out record.TaskToken) ||
			!MuiProcessRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist, MuiProcessRecordField.NameOwned,
				out var nameOwned) ||
			!MuiProcessRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist, MuiProcessRecordField.NameOwnedSize,
				out record.NameOwnedSize) ||
			!MuiProcessRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist, MuiProcessRecordField.Error,
				out record.Error) ||
			!MuiProcessRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist,
				MuiProcessRecordField.SignalsReceived,
				out record.SignalsReceived) ||
			!MuiProcessRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist, MuiProcessRecordField.Flags,
				out record.Flags) ||
			!MuiProcessRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist,
				MuiProcessRecordField.DispatchDepth,
				out record.DispatchDepth) ||
			!MuiProcessRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist, MuiProcessRecordField.SetupState,
				out record.SetupState) ||
			!MuiProcessRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist, MuiProcessRecordField.NotifyCount,
				out record.NotifyCount) ||
			!MuiProcessRecordFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist,
				MuiProcessRecordField.NotifyAttribute,
				out record.NotifyAttribute)) return false;
		record.NameOwned = APTR.FromPointer(nameOwned);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiProcessSpecialistRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiProcessSpecialistRecord.Size)) return false;
		return MuiProcessRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiProcessRecordKind.Specialist,
			MuiProcessRecordField.Magic, record.Magic) &&
			MuiProcessRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist, MuiProcessRecordField.Class,
				record.Class) &&
			MuiProcessRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist, MuiProcessRecordField.State,
				record.State) &&
			MuiProcessRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist,
				MuiProcessRecordField.TaskToken, record.TaskToken) &&
			MuiProcessRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist,
				MuiProcessRecordField.NameOwned, record.NameOwned.Raw) &&
			MuiProcessRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist,
				MuiProcessRecordField.NameOwnedSize, record.NameOwnedSize) &&
			MuiProcessRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist, MuiProcessRecordField.Error,
				record.Error) &&
			MuiProcessRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist,
				MuiProcessRecordField.SignalsReceived, record.SignalsReceived) &&
			MuiProcessRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist, MuiProcessRecordField.Flags,
				record.Flags) &&
			MuiProcessRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist,
				MuiProcessRecordField.DispatchDepth, record.DispatchDepth) &&
			MuiProcessRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist, MuiProcessRecordField.SetupState,
				record.SetupState) &&
			MuiProcessRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist, MuiProcessRecordField.NotifyCount,
				record.NotifyCount) &&
			MuiProcessRecordFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiProcessRecordKind.Specialist,
				MuiProcessRecordField.NotifyAttribute, record.NotifyAttribute);
	}
}

// Named input used by the scalar qualification surface. Keeping the
// qualification boundary struct-shaped avoids a long register-heavy argument
// list on 68k while preserving the same guest record layout in the codec.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiProcessSpecialistRecordInput
{
	public uint Class;
	public uint State;
	public uint TaskToken;
	public APTR NameOwned;
	public uint NameOwnedSize;
	public uint Error;
	public uint SignalsReceived;
	public uint Flags;
	public uint DispatchDepth;
	public uint SetupState;
	public uint NotifyCount;
	public uint NotifyAttribute;
}

// Struct-first qualification surface for the Process/Slave specialist sidecar.
public static class MuiProcessSpecialistRecordPacketCore
{
	public static bool WriteRecord<TPlatform>(ref TPlatform platform, APTR address,
		MuiProcessSpecialistRecordInput input)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiProcessSpecialistRecord record = default;
		record.Magic = MuiProcessSpecialistLayout.Magic;
		record.Class = input.Class;
		record.State = input.State;
		record.TaskToken = input.TaskToken;
		record.NameOwned = input.NameOwned;
		record.NameOwnedSize = input.NameOwnedSize;
		record.Error = input.Error;
		record.SignalsReceived = input.SignalsReceived;
		record.Flags = input.Flags;
		record.DispatchDepth = input.DispatchDepth;
		record.SetupState = input.SetupState;
		record.NotifyCount = input.NotifyCount;
		record.NotifyAttribute = input.NotifyAttribute;
		return MuiProcessSpecialistCodec.Write(ref platform, address, record);
	}

	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiProcessSpecialistCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Magic ^ record.Class ^ record.State ^ record.TaskToken ^
			record.NameOwned.Raw ^ record.NameOwnedSize ^ record.Error ^
			record.SignalsReceived ^ record.Flags ^ record.DispatchDepth ^
			record.SetupState ^ record.NotifyCount ^ record.NotifyAttribute;
	}
}

// The two scheduler-visible classes handled by this specialist. Both descend
// directly from Semaphore.mui; there is no Process/Slave-internal inheritance.
public enum MuiProcessSpecialistClass : uint
{
	None = 0,
	Process = 1,   // : Semaphore
	Slave = 2,     // : Semaphore
}

// The legal Process state machine. Transitions are strictly:
//   Pending -> Running        (successful Launch)
//   Pending -> Failed         (Launch rejected by the scheduler)
//   Running -> Completed      (Poll reports normal exit)
//   Running -> Failed         (Poll reports an error exit)
//   Running -> Killed         (successful Kill)
// No other transition is legal; a duplicate Launch from a non-Pending state is
// rejected, and Kill/Signal require the Running state.
public enum MuiProcessState : uint
{
	None = 0,
	Pending = 1,
	Running = 2,
	Completed = 3,
	Killed = 4,
	Failed = 5,
}

// Scheduler poll status codes reported by IMuiProcessCapability.ProcessPoll.
// These are the raw scheduler facts; the specialist maps them onto the legal
// MuiProcessState transitions and never invents a success the scheduler did not
// report.
public static class MuiProcessSchedulerStatus
{
	public const uint Unknown = 0;
	public const uint Running = 1;
	public const uint Completed = 2;
	public const uint Failed = 3;
}

// The MG09 Process/Slave specialist. Every entry point works over a validated
// headless object plus its attached sidecar. Classes are classified by their
// exact, case-sensitive official class id. Scalar attributes and their
// notifications go through MuiHeadlessObjectCore; shared-instance locking goes
// through MuiSemaphoreCore.
public static class MuiProcessSpecialistCore
{
	// ---- Classification ------------------------------------------------------

	// Classify a guest C-string class id against the exact official names. The
	// loader contract is case-sensitive, so the match is byte-exact against the
	// documented "<Name>.mui" ids with no managed strings, arrays or spans.
	public static MuiProcessSpecialistClass ClassifyName<TPlatform>(
		ref TPlatform platform, APTR classId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (classId.IsNull) return MuiProcessSpecialistClass.None;
		// Process.mui
		if (B(ref platform, classId, 0) == 'P' &&
			B(ref platform, classId, 1) == 'r' &&
			B(ref platform, classId, 2) == 'o' &&
			B(ref platform, classId, 3) == 'c' &&
			B(ref platform, classId, 4) == 'e' &&
			B(ref platform, classId, 5) == 's' &&
			B(ref platform, classId, 6) == 's' && Suffix(ref platform, classId, 7))
			return MuiProcessSpecialistClass.Process;
		// Slave.mui
		if (B(ref platform, classId, 0) == 'S' &&
			B(ref platform, classId, 1) == 'l' &&
			B(ref platform, classId, 2) == 'a' &&
			B(ref platform, classId, 3) == 'v' &&
			B(ref platform, classId, 4) == 'e' && Suffix(ref platform, classId, 5))
			return MuiProcessSpecialistClass.Slave;
		return MuiProcessSpecialistClass.None;
	}

	private static int B<TPlatform>(ref TPlatform platform, APTR text, int index)
		where TPlatform : struct, IMuiGuestMemory =>
		platform.IsMapped(text, (uint)index + 1) ? platform.ReadUInt8(text, index)
			: -1;

	private static bool Suffix<TPlatform>(ref TPlatform platform, APTR text,
		int offset) where TPlatform : struct, IMuiGuestMemory =>
		B(ref platform, text, offset) == '.' &&
		B(ref platform, text, offset + 1) == 'm' &&
		B(ref platform, text, offset + 2) == 'u' &&
		B(ref platform, text, offset + 3) == 'i' &&
		B(ref platform, text, offset + 4) == 0;

	// Both classes descend directly from Semaphore.mui; None is the sentinel
	// used for "not a Process/Slave specialist superclass root".
	public static MuiProcessSpecialistClass Superclass(
		MuiProcessSpecialistClass cls) => MuiProcessSpecialistClass.None;

	// ---- Sidecar attach / lookup ---------------------------------------------

	// Attach a sidecar to an already-created headless object of `cls`,
	// establishing the documented creation defaults (Process: Pending, default
	// stack, priority 0; Slave: not set up). Fails atomically: a failed
	// allocation or attribute link frees the block and leaves the object
	// untouched. Returns the sidecar block or Null.
	public static APTR Attach<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, MuiProcessSpecialistClass cls)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (obj.IsNull || cls == MuiProcessSpecialistClass.None) return APTR.Null;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return APTR.Null;
		if (Sidecar(ref platform, state, obj).IsNotNull) return APTR.Null;
		var sc = MuiHeadlessMemory.Allocate(ref platform,
			MuiProcessSpecialistLayout.InstanceSize);
		if (sc.IsNull) return APTR.Null;
		MuiProcessSpecialistRecord record = default;
		record.Magic = MuiProcessSpecialistLayout.Magic;
		record.Class = (uint)cls;
		if (!MuiProcessSpecialistCodec.Write(ref platform, sc, record))
		{
			platform.Clear(sc, MuiProcessSpecialistLayout.InstanceSize);
			platform.Free(sc, MuiProcessSpecialistLayout.InstanceSize);
			return APTR.Null;
		}
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiProcessSpecialistLayout.SidecarAttribute, sc.Raw, false))
		{
			platform.Clear(sc, MuiProcessSpecialistLayout.InstanceSize);
			platform.Free(sc, MuiProcessSpecialistLayout.InstanceSize);
			return APTR.Null;
		}
		if (cls == MuiProcessSpecialistClass.Process)
		{
			if (!MuiProcessSpecialistCodec.TryRead(ref platform, sc,
				out record))
			{
				MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiProcessSpecialistLayout.SidecarAttribute, 0, false);
				platform.Clear(sc, MuiProcessSpecialistLayout.InstanceSize);
				platform.Free(sc, MuiProcessSpecialistLayout.InstanceSize);
				return APTR.Null;
			}
			record.State = (uint)MuiProcessState.Pending;
			if (!MuiProcessSpecialistCodec.Write(ref platform, sc, record))
			{
				MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiProcessSpecialistLayout.SidecarAttribute, 0, false);
				platform.Clear(sc, MuiProcessSpecialistLayout.InstanceSize);
				platform.Free(sc, MuiProcessSpecialistLayout.InstanceSize);
				return APTR.Null;
			}
			if (!AdoptInitialProcessAttributes(ref platform, state, obj, sc))
			{
				MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiProcessSpecialistLayout.SidecarAttribute, 0, false);
				platform.Clear(sc, MuiProcessSpecialistLayout.InstanceSize);
				platform.Free(sc, MuiProcessSpecialistLayout.InstanceSize);
				return APTR.Null;
			}
		}
		return sc;
	}

	// OM_NEW/tag application happens before the specialist sidecar exists. Import
	// the Process-only init attributes here so factory construction has the same
	// defaults, validation, name ownership, and AutoLaunch latch as a native
	// Process.mui instance. AutoLaunch is only latched here; setup-time launch is
	// deliberately a separate explicit operation through AutoLaunchIfRequested.
	private static bool AdoptInitialProcessAttributes<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, APTR sc)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			MuiProcessAttributes.Process_StackSize, out var stackSize))
		{
			stackSize = MuiProcessSpecialistLayout.DefaultStackSize;
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				MuiProcessAttributes.Process_StackSize, stackSize, false);
		}
		if (stackSize < MuiProcessSpecialistLayout.MinStackSize ||
			stackSize > MuiProcessSpecialistLayout.MaxStackSize) return false;

		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			MuiProcessAttributes.Process_Priority, out var priority))
		{
			priority = 0;
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				MuiProcessAttributes.Process_Priority, priority, false);
		}
		if (!ValidPriority(priority)) return false;

		if (MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			MuiProcessAttributes.Process_AutoLaunch, out var autoLaunch) &&
			autoLaunch != 0)
			SetFlag(ref platform, sc, MuiProcessSpecialistLayout.FlagAutoLaunch,
				true);

		if (MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			MuiProcessAttributes.Process_Name, out var name) && name != 0)
			return SetOwnedName(ref platform, state, obj, sc, name,
				out _);
		return true;
	}

	// Classify an existing object by its registered class-record name and attach
	// (MakeObject/factory interop path). Returns the sidecar or Null.
	public static MuiProcessSpecialistClass ClassifyObject<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiServicePlatform
	{
		var classRecord = MuiHeadlessObjectCore.ObjectClassRecord(ref platform,
			state, obj);
		if (!MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
			out var classValue)) return MuiProcessSpecialistClass.None;
		return ClassifyName(ref platform, classValue.Name);
	}

	public static APTR AttachByObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiServicePlatform =>
		Attach(ref platform, state, obj,
			ClassifyObject(ref platform, state, obj));

	private static APTR Sidecar<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			MuiProcessSpecialistLayout.SidecarAttribute, out var raw) || raw == 0)
			return APTR.Null;
		var sc = APTR.FromPointer(raw);
		if (!MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record) || record.Magic != MuiProcessSpecialistLayout.Magic)
			return APTR.Null;
		return sc;
	}

	public static bool Valid<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiServicePlatform =>
		Sidecar(ref platform, state, obj).IsNotNull;

	public static MuiProcessSpecialistClass Classify<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiServicePlatform
	{
		var sc = Sidecar(ref platform, state, obj);
		return sc.IsNull || !MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record) ? MuiProcessSpecialistClass.None
			: (MuiProcessSpecialistClass)record.Class;
	}

	private static APTR ProcessSidecar<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var sc = Sidecar(ref platform, state, obj);
		if (sc.IsNull || !MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record)) return APTR.Null;
		return (MuiProcessSpecialistClass)record.Class ==
			MuiProcessSpecialistClass.Process ? sc : APTR.Null;
	}

	private static APTR SlaveSidecar<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var sc = Sidecar(ref platform, state, obj);
		if (sc.IsNull || !MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record)) return APTR.Null;
		return (MuiProcessSpecialistClass)record.Class ==
			MuiProcessSpecialistClass.Slave ? sc : APTR.Null;
	}

	// ---- Process state accessors ---------------------------------------------

	public static MuiProcessState ProcessStateOf<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var sc = ProcessSidecar(ref platform, state, obj);
		return sc.IsNull || !MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record) ? MuiProcessState.None
			: (MuiProcessState)record.State;
	}

	public static uint TaskToken<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var sc = ProcessSidecar(ref platform, state, obj);
		return sc.IsNull || !MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record) ? 0 : record.TaskToken;
	}

	// ---- Process methods -----------------------------------------------------

	// MUIM_Process_Launch: launch the process. Legal only from Pending; a
	// duplicate launch from any other state is rejected. Failure-atomic: a
	// scheduler that returns a zero token leaves the process in the Failed state
	// with no task token and no owned resources leaked, and the call reports
	// failure.
	public static bool Launch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var sc = ProcessSidecar(ref platform, state, obj);
		if (sc.IsNull) return false;
		if (!MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record) || (MuiProcessState)record.State !=
			MuiProcessState.Pending)
			return false;   // duplicate-launch rejection

		var name = OwnedNameOrCaller(ref platform, state, sc, obj);
		var priority = SignedAttribute(ref platform, state, obj,
			MuiProcessAttributes.Process_Priority);
		var stackSize = UnsignedAttribute(ref platform, state, obj,
			MuiProcessAttributes.Process_StackSize);
		var sourceClass = APTR.FromPointer(UnsignedAttribute(ref platform, state,
			obj, MuiProcessAttributes.Process_SourceClass));
		var sourceObject = APTR.FromPointer(UnsignedAttribute(ref platform, state,
			obj, MuiProcessAttributes.Process_SourceObject));

		var token = platform.ProcessLaunch(name, priority, stackSize,
			sourceClass, sourceObject);
		if (token == 0)
		{
			// Failure-atomic: no task token published, transition to Failed.
			record.TaskToken = 0;
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				MuiProcessAttributes.Process_Task, 0, false);
			record.State = (uint)MuiProcessState.Failed;
			MuiProcessSpecialistCodec.Write(ref platform, sc, record);
			return false;
		}
		record.TaskToken = token;
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiProcessAttributes.Process_Task, token, false);
		record.State = (uint)MuiProcessState.Running;
		MuiProcessSpecialistCodec.Write(ref platform, sc, record);
		return true;
	}

	// MUIM_Process_Kill: terminate a running process. Legal only from Running.
	public static bool Kill<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var sc = ProcessSidecar(ref platform, state, obj);
		if (sc.IsNull) return false;
		if (!MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record) || (MuiProcessState)record.State !=
			MuiProcessState.Running)
			return false;
		var token = record.TaskToken;
		var killed = platform.ProcessKill(token);
		if (!killed) return false;
		record.TaskToken = 0;
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiProcessAttributes.Process_Task, 0, false);
		record.State = (uint)MuiProcessState.Killed;
		MuiProcessSpecialistCodec.Write(ref platform, sc, record);
		return true;
	}

	// MUIM_Process_Process: advance/poll the running process body. The
	// documented method runs in the process context; headless we express only
	// its state-machine effect: while Running we poll the scheduler and move to
	// Completed (normal exit) or Failed (error exit) exactly as reported. No
	// success is invented — an Unknown/Running status leaves the state at
	// Running. Returns the resulting state code.
	public static uint Process<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var sc = ProcessSidecar(ref platform, state, obj);
		if (sc.IsNull) return (uint)MuiProcessState.None;
		if (!MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record)) return (uint)MuiProcessState.None;
		var current = (MuiProcessState)record.State;
		if (current != MuiProcessState.Running)
			return (uint)current;
		var token = record.TaskToken;
		var status = platform.ProcessPoll(token);
		switch (status)
		{
			case MuiProcessSchedulerStatus.Completed:
				record.State = (uint)MuiProcessState.Completed;
				MuiProcessSpecialistCodec.Write(ref platform, sc, record);
				MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiProcessAttributes.Process_Task, 0, false);
				return (uint)MuiProcessState.Completed;
			case MuiProcessSchedulerStatus.Failed:
				record.State = (uint)MuiProcessState.Failed;
				MuiProcessSpecialistCodec.Write(ref platform, sc, record);
				MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					MuiProcessAttributes.Process_Task, 0, false);
				return (uint)MuiProcessState.Failed;
			default:
				return (uint)MuiProcessState.Running;
		}
	}

	// MUIM_Process_Signal(ULONG sigs): deliver signals to the running process.
	// Legal only while Running.
	public static bool Signal<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint signalMask) where TPlatform : struct, IMuiServicePlatform
	{
		var sc = ProcessSidecar(ref platform, state, obj);
		if (sc.IsNull) return false;
		if (!MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record) || (MuiProcessState)record.State !=
			MuiProcessState.Running)
			return false;
		var token = record.TaskToken;
		platform.ProcessSignal(token, signalMask);
		return true;
	}

	// If MUIA_Process_AutoLaunch was latched at construction, launch now. Used by
	// a host that models MUI's automatic launch at object setup. A no-op (and
	// success) when AutoLaunch is not set.
	public static bool AutoLaunchIfRequested<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var sc = ProcessSidecar(ref platform, state, obj);
		if (sc.IsNull || !MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record)) return false;
		if ((record.Flags &
			MuiProcessSpecialistLayout.FlagAutoLaunch) == 0) return true;
		return Launch(ref platform, state, obj);
	}

	// ---- Slave setup / dispatch / signals ------------------------------------

	public static bool SlaveIsSetup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var sc = SlaveSidecar(ref platform, state, obj);
		return sc.IsNotNull && MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record) && record.SetupState != 0;
	}

	// MUIM_Slave_Setup: prepare the slave for dispatching. Requires the
	// referenced Application to still be a live headless object and an idempotent
	// balance (a second Setup with no intervening Cleanup is rejected).
	public static bool Setup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var sc = SlaveSidecar(ref platform, state, obj);
		if (sc.IsNull) return false;
		if (!MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record) || record.SetupState != 0)
			return false;   // balance: not set up twice
		if (!ApplicationAlive(ref platform, state, obj)) return false;
		record.SetupState = 1;
		MuiProcessSpecialistCodec.Write(ref platform, sc, record);
		return true;
	}

	// MUIM_Slave_Cleanup: tear down after dispatching. Underflow protected: a
	// Cleanup with no matching Setup is rejected. A clean shutdown requires no
	// dispatch to be in flight.
	public static bool Cleanup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var sc = SlaveSidecar(ref platform, state, obj);
		if (sc.IsNull) return false;
		if (!MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record) || record.SetupState == 0)
			return false;   // underflow protection
		if (record.DispatchDepth != 0)
			return false;   // no clean shutdown while a dispatch is in flight
		record.SetupState = 0;
		MuiProcessSpecialistCodec.Write(ref platform, sc, record);
		return true;
	}

	// MUIM_Slave_Dispatch: dispatch a bounded automagic method packet to the
	// slave's target object exactly like MUI's Call/DoMethod path.
	//
	// The packet is { ULONG argCount; ULONG methodId; ULONG args[argCount] }.
	// argCount is bounded to <= 16 (the documented automagic argument limit);
	// a malformed packet (unmapped, zero method id, or an over-length count) is
	// rejected without dispatching. The slave must be set up and its Application
	// alive. Shared-instance access to the target object is serialized through
	// the frozen MuiSemaphoreCore for the duration of the call, and a reentrancy
	// guard rejects a re-entered dispatch. On a well-formed packet the target
	// object receives exactly one DoMethod of the reconstructed message and its
	// result is returned in `result`.
	public static bool Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR packet, out uint result)
		where TPlatform : struct, IMuiServicePlatform
	{
		result = 0;
		var sc = SlaveSidecar(ref platform, state, obj);
		if (sc.IsNull) return false;
		if (!MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record) || record.SetupState == 0)
			return false;
		if (!ApplicationAlive(ref platform, state, obj)) return false;
		if (record.DispatchDepth != 0)
			return false;   // reentrancy guard: no re-entered dispatch

		// Validate the named packet header (argCount + methodId) and its bounded
		// inline argument vector.
		if (!MuiProcessDispatchPacketCodec.TryReadHeader(ref platform, packet,
			out var dispatchPacket)) return false;
		var argCount = dispatchPacket.ArgumentCount;
		var methodId = dispatchPacket.MethodId;
		var messageBytes = 4u + argCount * 4u;

		var target = APTR.FromPointer(UnsignedAttribute(ref platform, state, obj,
			MuiProcessAttributes.Slave_Object));
		if (target.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, target).IsNull)
			return false;

		// Serialize shared-instance access to the target for the whole call.
		if (!MuiSemaphoreCore.Obtain(ref platform, state, target)) return false;
		record.DispatchDepth = 1;
		MuiProcessSpecialistCodec.Write(ref platform, sc, record);

		// Reconstruct the exact BOOPSI message { ULONG MethodID; ULONG args... }
		// in bounded guest scratch and deliver a single DoMethod, exactly like
		// MUI's automagic Call.
		var message = MuiHeadlessMemory.Allocate(ref platform, messageBytes);
		var dispatched = false;
		if (message.IsNotNull)
		{
			platform.WriteUInt32(message, 0, methodId);
			for (var i = 0u; i < argCount; i++)
			{
				if (!MuiProcessDispatchPacketCodec.TryReadArgument(ref platform,
					packet, dispatchPacket, i, out var argument))
				{
					platform.Clear(message, messageBytes);
					platform.Free(message, messageBytes);
					record.DispatchDepth = 0;
					MuiProcessSpecialistCodec.Write(ref platform, sc, record);
					MuiSemaphoreCore.Release(ref platform, state, target);
					return false;
				}
				var argumentCursor = default(MuiProcessArgumentCursor);
				argumentCursor.Message = message;
				argumentCursor.Index = i;
				argumentCursor.Count = argCount;
				argumentCursor.Kind = MuiProcessArgumentVectorKind.MethodMessage;
				if (!MuiProcessArgumentCursorCodec.TryGetEntry(ref platform,
					argumentCursor, out var argumentSlot))
				{
					platform.Clear(message, messageBytes);
					platform.Free(message, messageBytes);
					record.DispatchDepth = 0;
					MuiProcessSpecialistCodec.Write(ref platform, sc, record);
					MuiSemaphoreCore.Release(ref platform, state, target);
					return false;
				}
				var argumentRecord = default(MuiProcessDispatchArgumentSlot);
				argumentRecord.Value = argument;
				if (!MuiProcessDispatchArgumentSlotCodec.Write(ref platform,
					argumentSlot, argumentRecord))
				{
					platform.Clear(message, messageBytes);
					platform.Free(message, messageBytes);
					record.DispatchDepth = 0;
					MuiProcessSpecialistCodec.Write(ref platform, sc, record);
					MuiSemaphoreCore.Release(ref platform, state, target);
					return false;
				}
			}
			result = platform.DoMethod(target, message);
			dispatched = true;
			platform.Clear(message, messageBytes);
			platform.Free(message, messageBytes);
		}

		record.DispatchDepth = 0;
		MuiProcessSpecialistCodec.Write(ref platform, sc, record);
		MuiSemaphoreCore.Release(ref platform, state, target);
		return dispatched;
	}

	// MUIM_Slave_Error(LONG num): record and report the slave error code.
	public static bool Error<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint errorCode, out uint stored)
		where TPlatform : struct, IMuiServicePlatform
	{
		stored = 0;
		var sc = SlaveSidecar(ref platform, state, obj);
		if (sc.IsNull || !MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record)) return false;
		record.Error = errorCode;
		MuiProcessSpecialistCodec.Write(ref platform, sc, record);
		stored = errorCode;
		return true;
	}

	public static uint LastError<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var sc = SlaveSidecar(ref platform, state, obj);
		return sc.IsNull || !MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record) ? 0 : record.Error;
	}

	// MUIM_Slave_SignalsReceived(ULONG sigs): the slave learns which coordinated
	// signals arrived. The documented SIGBREAKF_CTRL_C..CTRL_F break mask is
	// always reserved and coordinated explicitly: the poll is widened to include
	// the reserved mask so a break is never lost, and every received bit is
	// accumulated into the sidecar. Returns the received set for this poll.
	public static uint SignalsReceived<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint requestedMask)
		where TPlatform : struct, IMuiServicePlatform
	{
		var sc = SlaveSidecar(ref platform, state, obj);
		if (sc.IsNull) return 0;
		var mask = requestedMask | MuiProcessAttributes.SIGBREAKF_CTRL;
		var received = platform.ProcessSignalsReceived(mask);
		if (!MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record)) return 0;
		record.SignalsReceived |= received;
		MuiProcessSpecialistCodec.Write(ref platform, sc, record);
		return received;
	}

	public static uint AccumulatedSignals<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var sc = SlaveSidecar(ref platform, state, obj);
		return sc.IsNull || !MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record) ? 0 : record.SignalsReceived;
	}

	private static bool ApplicationAlive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var app = APTR.FromPointer(UnsignedAttribute(ref platform, state, obj,
			MuiProcessAttributes.Slave_Application));
		return app.IsNotNull &&
			MuiHeadlessObjectCore.FindObject(ref platform, state, app).IsNotNull;
	}

	// ---- Attribute get -------------------------------------------------------

	// Read a Process/Slave attribute honoring the official I/S/G policy.
	// Init-only attributes (AutoLaunch) are read through their dedicated
	// accessor and are not exposed here.
	public static bool GetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiServicePlatform
	{
		value = 0;
		var cls = Classify(ref platform, state, obj);
		if (cls == MuiProcessSpecialistClass.None) return false;
		switch (attribute)
		{
			case MuiProcessAttributes.Process_Name:
			case MuiProcessAttributes.Process_Priority:
			case MuiProcessAttributes.Process_StackSize:
			case MuiProcessAttributes.Process_SourceClass:
			case MuiProcessAttributes.Process_SourceObject:
			case MuiProcessAttributes.Process_Task:
				if (cls != MuiProcessSpecialistClass.Process) return false;
				return ReadStored(ref platform, state, obj, attribute, out value);
			case MuiProcessAttributes.Slave_Application:
			case MuiProcessAttributes.Slave_Class:
			case MuiProcessAttributes.Slave_Object:
				if (cls != MuiProcessSpecialistClass.Slave) return false;
				return ReadStored(ref platform, state, obj, attribute, out value);
		}
		return false;
	}

	public static bool AutoLaunchFlag<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var sc = ProcessSidecar(ref platform, state, obj);
		return sc.IsNotNull && MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record) && (record.Flags &
			MuiProcessSpecialistLayout.FlagAutoLaunch) != 0;
	}

	// ---- Attribute set -------------------------------------------------------

	// Apply a Process/Slave attribute honoring the official I/S/G policy.
	// `isInit` selects the construction path; `notify` requests a runtime-change
	// notification. `changed` reports whether the runtime value moved.
	public static bool SetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value, bool isInit, bool notify,
		out bool changed) where TPlatform : struct, IMuiServicePlatform
	{
		changed = false;
		var sc = Sidecar(ref platform, state, obj);
		if (sc.IsNull || !MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record)) return false;
		var cls = (MuiProcessSpecialistClass)record.Class;

		switch (attribute)
		{
			// -- Process --
			case MuiProcessAttributes.Process_AutoLaunch:
				if (cls != MuiProcessSpecialistClass.Process || !isInit)
					return false;   // [I..]
				changed = SetFlag(ref platform, sc,
					MuiProcessSpecialistLayout.FlagAutoLaunch, value != 0);
				return true;

			case MuiProcessAttributes.Process_Name:
				// [I.G] STRPTR: always kept as a class-owned copy so the process
				// name outlives the caller's transient string. Failure-atomic.
				if (cls != MuiProcessSpecialistClass.Process || !isInit)
					return false;
				return SetOwnedName(ref platform, state, obj, sc, value,
					out changed);

			case MuiProcessAttributes.Process_Priority:
				// [ISG] LONG bounded to the exec task priority range.
				if (cls != MuiProcessSpecialistClass.Process) return false;
				if (!ValidPriority(value)) return false;
				changed = SetScalar(ref platform, state, obj, sc, attribute, value,
					isInit, notify);
				return true;

			case MuiProcessAttributes.Process_StackSize:
				// [I.G] LONG bounded to [MinStack, MaxStack].
				if (cls != MuiProcessSpecialistClass.Process || !isInit)
					return false;
				if (value < MuiProcessSpecialistLayout.MinStackSize ||
					value > MuiProcessSpecialistLayout.MaxStackSize) return false;
				changed = SetScalar(ref platform, state, obj, sc, attribute, value,
					true, false);
				return true;

			case MuiProcessAttributes.Process_SourceClass:
			case MuiProcessAttributes.Process_SourceObject:
				// [I.G] caller-owned reference; never copied or freed.
				if (cls != MuiProcessSpecialistClass.Process || !isInit)
					return false;
				changed = SetScalar(ref platform, state, obj, sc, attribute, value,
					true, false);
				return true;

			case MuiProcessAttributes.Process_Task:
				// [..G] published only by the launch machinery, never by a caller.
				return false;

			// -- Slave --
			case MuiProcessAttributes.Slave_Application:
			case MuiProcessAttributes.Slave_Class:
			case MuiProcessAttributes.Slave_Object:
				// [I.G] caller/app-owned references; never copied or freed.
				if (cls != MuiProcessSpecialistClass.Slave || !isInit)
					return false;
				changed = SetScalar(ref platform, state, obj, sc, attribute, value,
					true, false);
				return true;
		}
		return false;
	}

	// Copy the caller's Name into a fresh class-owned block before releasing the
	// previous copy (failure-atomic). A Null value clears the owned name.
	private static bool SetOwnedName<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR sc, uint value, out bool changed)
		where TPlatform : struct, IMuiServicePlatform
	{
		changed = false;
		APTR ownedCopy = APTR.Null;
		uint ownedCopySize = 0;
		if (value != 0)
		{
			if (!CopyString(ref platform, APTR.FromPointer(value), out ownedCopy,
				out ownedCopySize)) return false;   // atomic: nothing touched
		}

		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			MuiProcessAttributes.Process_Name, out var previous);
		changed = previous != ownedCopy.Raw;
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiProcessAttributes.Process_Name, ownedCopy.Raw, false);

		if (!MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record)) return false;
		var oldOwned = record.NameOwned;
		var oldOwnedSize = record.NameOwnedSize;
		record.NameOwned = ownedCopy;
		record.NameOwnedSize = ownedCopySize;
		if (!MuiProcessSpecialistCodec.Write(ref platform, sc, record))
			return false;
		if (oldOwned.IsNotNull)
		{
			platform.Clear(oldOwned, oldOwnedSize);
			platform.Free(oldOwned, oldOwnedSize);
		}
		return true;
	}

	private static APTR OwnedNameOrCaller<TPlatform>(ref TPlatform platform,
		APTR state, APTR sc, APTR obj)
		where TPlatform : struct, IMuiServicePlatform
	{
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			MuiProcessAttributes.Process_Name, out var stored);
		return APTR.FromPointer(stored);
	}

	private static bool ValidPriority(uint value)
	{
		var signed = unchecked((int)value);
		return signed >= MuiProcessSpecialistLayout.MinPriority &&
			signed <= MuiProcessSpecialistLayout.MaxPriority;
	}

	private static int SignedAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute)
		where TPlatform : struct, IMuiServicePlatform
	{
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out var value);
		return unchecked((int)value);
	}

	private static uint UnsignedAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute)
		where TPlatform : struct, IMuiServicePlatform
	{
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out var value);
		return value;
	}

	private static bool ReadStored<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiServicePlatform
	{
		value = 0;
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out value);
		return true;
	}

	private static bool SetScalar<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR sc, uint attribute, uint value, bool isInit, bool notify)
		where TPlatform : struct, IMuiServicePlatform
	{
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out var previous);
		var changed = previous != value;
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			value, !isInit && notify && changed);
		if (!isInit && changed) Notify(ref platform, sc, attribute);
		return changed;
	}

	// ---- Notification accessors ----------------------------------------------

	public static uint NotificationCount<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var sc = Sidecar(ref platform, state, obj);
		return sc.IsNull || !MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record) ? 0 : record.NotifyCount;
	}

	public static uint LastNotifiedAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var sc = Sidecar(ref platform, state, obj);
		return sc.IsNull || !MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record) ? 0 : record.NotifyAttribute;
	}

	// ---- Class-owned disposal ------------------------------------------------

	// Free every class-owned resource (the copied Name block and the sidecar),
	// then tear down the object through the frozen object core. If a Process is
	// still Running it is killed first so a launched task is never orphaned. A
	// repeated disposal finds no sidecar and is a safe no-op. Caller-owned
	// references (SourceClass/SourceObject/Application/Class/Object) are never
	// freed.
	public static bool Dispose<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var sc = Sidecar(ref platform, state, obj);
		if (sc.IsNull) return false;
		if (!MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record)) return false;
		var cls = (MuiProcessSpecialistClass)record.Class;
		if (cls == MuiProcessSpecialistClass.Process &&
			(MuiProcessState)record.State == MuiProcessState.Running)
		{
			var token = record.TaskToken;
			platform.ProcessKill(token);
			record.State = (uint)MuiProcessState.Killed;
			MuiProcessSpecialistCodec.Write(ref platform, sc, record);
		}
		FreeOwned(ref platform, sc, ref record);
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiProcessSpecialistLayout.SidecarAttribute, 0, false);
		platform.Clear(sc, MuiProcessSpecialistLayout.InstanceSize);
		platform.Free(sc, MuiProcessSpecialistLayout.InstanceSize);
		return MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj);
	}

	private static void FreeOwned<TPlatform>(ref TPlatform platform, APTR sc,
		ref MuiProcessSpecialistRecord record)
		where TPlatform : struct, IMuiServicePlatform
	{
		var block = record.NameOwned;
		if (block.IsNull) return;
		var size = record.NameOwnedSize;
		platform.Clear(block, size);
		platform.Free(block, size);
		record.NameOwned = APTR.Null;
		record.NameOwnedSize = 0;
		MuiProcessSpecialistCodec.Write(ref platform, sc, record);
	}

	// ---- Internals -----------------------------------------------------------

	private static bool CopyString<TPlatform>(ref TPlatform platform, APTR source,
		out APTR block, out uint size)
		where TPlatform : struct, IMuiServicePlatform
	{
		block = APTR.Null;
		size = 0;
		if (source.IsNull) return false;
		if (!CStringCodec.TryReadLength(ref platform, source,
			MuiProcessSpecialistLayout.MaximumString + 1, out var length))
			return false;
		var total = length + 1;
		var b = MuiHeadlessMemory.Allocate(ref platform, total);
		if (b.IsNull) return false;
		for (var i = 0u; i < total; i++)
			platform.WriteUInt8(b, (int)i, platform.ReadUInt8(source, (int)i));
		block = b;
		size = total;
		return true;
	}

	private static void Notify<TPlatform>(ref TPlatform platform, APTR sc,
		uint attribute) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record)) return;
		record.NotifyAttribute = attribute;
		record.NotifyCount++;
		MuiProcessSpecialistCodec.Write(ref platform, sc, record);
	}

	private static bool SetFlag<TPlatform>(ref TPlatform platform, APTR sc,
		uint bit, bool set) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiProcessSpecialistCodec.TryRead(ref platform, sc,
			out var record)) return false;
		var updated = set ? record.Flags | bit : record.Flags & ~bit;
		if (updated == record.Flags) return false;
		record.Flags = updated;
		MuiProcessSpecialistCodec.Write(ref platform, sc, record);
		return true;
	}
}

// Official MG09 Process.mui / Slave.mui attribute and method identifiers,
// resolved from the authority (libraries/mui.h in the frozen MorphOS 3.20 SDK,
// mirrored in the abi-inventory) and kept beside the core so classification and
// dispatch stay byte-exact.
//
// Inheritance (from the MUI class autodocs):
//   Process.mui : Semaphore.mui
//     MUIA_Process_AutoLaunch   [I..] BOOL
//     MUIA_Process_Name         [I.G] STRPTR   (kept as a class-owned copy)
//     MUIA_Process_Priority     [ISG] LONG     (bounded exec priority range)
//     MUIA_Process_StackSize    [I.G] LONG     (bounded stack range)
//     MUIA_Process_SourceClass  [I.G] struct IClass *
//     MUIA_Process_SourceObject [I.G] Object *
//     MUIA_Process_Task         [..G] scheduler task pointer
//     MUIM_Process_Launch / _Kill / _Process / _Signal
//   Slave.mui : Semaphore.mui
//     MUIA_Slave_Application     [I.G] Object *
//     MUIA_Slave_Class           [I.G] struct IClass *
//     MUIA_Slave_Object          [I.G] Object *
//     MUIM_Slave_Setup / _Cleanup / _Dispatch / _Error / _SignalsReceived
//     MUIF_Slave_Delegate_ForceSlave
//   Semaphore.mui (shared superclass)
//     MUIM_Semaphore_Attempt / _AttemptShared / _Obtain / _ObtainShared /
//     _Release
public static class MuiProcessAttributes
{
	// Semaphore.mui methods (shared superclass).
	public const uint Semaphore_Attempt = 0x80426ce2u;
	public const uint Semaphore_AttemptShared = 0x80422551u;
	public const uint Semaphore_Obtain = 0x804276f0u;
	public const uint Semaphore_ObtainShared = 0x8042ea02u;
	public const uint Semaphore_Release = 0x80421f2du;

	// Process.mui methods.
	public const uint Process_Kill = 0x804264cfu;
	public const uint Process_Launch = 0x80425df7u;
	public const uint Process_Process = 0x804230aau;
	public const uint Process_Signal = 0x8042e791u;

	// Process.mui attributes.
	public const uint Process_AutoLaunch = 0x80428855u;
	public const uint Process_Name = 0x8042732bu;
	public const uint Process_Priority = 0x80422a54u;
	public const uint Process_SourceClass = 0x8042cf8bu;
	public const uint Process_SourceObject = 0x804212a2u;
	public const uint Process_StackSize = 0x804230d0u;
	public const uint Process_Task = 0x8042b123u;

	// Slave.mui methods.
	public const uint Slave_Cleanup = 0x80425e72u;
	public const uint Slave_Dispatch = 0x8042361fu;
	public const uint Slave_Error = 0x8042e544u;
	public const uint Slave_Setup = 0x80429faau;
	public const uint Slave_SignalsReceived = 0x8042d21au;

	// Slave.mui attributes.
	public const uint Slave_Application = 0x80427767u;
	public const uint Slave_Class = 0x80420f8cu;
	public const uint Slave_Object = 0x804202abu;

	// MUIF_Slave_Delegate_ForceSlave.
	public const uint Slave_Delegate_ForceSlave = 1u;

	// Documented exec break-signal mask reserved and coordinated by the Slave
	// (SIGBREAKF_CTRL_C..CTRL_F). These are exec/dos constants, not MUI ids, so
	// they are defined here directly rather than sourced from the MUI inventory.
	public const uint SIGBREAKF_CTRL_C = 0x00001000u;
	public const uint SIGBREAKF_CTRL_D = 0x00002000u;
	public const uint SIGBREAKF_CTRL_E = 0x00004000u;
	public const uint SIGBREAKF_CTRL_F = 0x00008000u;
	public const uint SIGBREAKF_CTRL = SIGBREAKF_CTRL_C | SIGBREAKF_CTRL_D |
		SIGBREAKF_CTRL_E | SIGBREAKF_CTRL_F;
}
