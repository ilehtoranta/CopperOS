/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiMasterLibraryState
{
	public const uint Size = 16;
	public uint LibraryBase;
	public uint PrivateRoot;
	public ushort OpenCount;
	public ushort Flags;
	public uint Generation;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiMasterPrivateRoot
{
	public const uint Size = 48;
	internal const uint FieldSize = 4;
	internal const uint ClassRegistryOffset = 0;
	internal const uint AllocationPolicyOffset = 4;
	internal const uint ErrorStateOffset = 8;
	internal const uint ApplicationHeadOffset = 12;
	internal const uint ExternalClassHeadOffset = 16;
	internal const uint CallbackStateOffset = 20;
	internal const uint LoaderStateOffset = 24;
	internal const uint RegistryGenerationOffset = 28;
	internal const uint ActiveDispatchDepthOffset = 32;
	internal const uint ActiveCallbackDepthOffset = 36;
	internal const uint FlagsOffset = 40;
	internal const uint ReservedOffset = 44;
	public uint ClassRegistry;
	public uint AllocationPolicy;
	public uint ErrorState;
	public uint ApplicationHead;
	public uint ExternalClassHead;
	public uint CallbackState;
	public uint LoaderState;
	public uint RegistryGeneration;
	public uint ActiveDispatchDepth;
	public uint ActiveCallbackDepth;
	public uint Flags;
	public uint Reserved;
}

internal enum MuiMasterPrivateRootField : byte
{
	ClassRegistry,
	AllocationPolicy,
	ErrorState,
	ApplicationHead,
	ExternalClassHead,
	CallbackState,
	LoaderState,
	RegistryGeneration,
	ActiveDispatchDepth,
	ActiveCallbackDepth,
	Flags,
	Reserved,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMasterPrivateRootFieldCursor
{
	internal APTR Record;
	internal MuiMasterPrivateRootField Field;
}

// Struct-first guest-memory adapter for the fixed muimaster private root.
// Wire positions are confined here; lifecycle code exchanges the complete
// named MuiMasterPrivateRoot value instead of addressing a scalar offset.
internal static class MuiMasterPrivateRootRecordMemoryCodec
{
	private static bool TryResolve(MuiMasterPrivateRootField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiMasterPrivateRootField.ClassRegistry =>
				MuiMasterPrivateRoot.ClassRegistryOffset,
			MuiMasterPrivateRootField.AllocationPolicy =>
				MuiMasterPrivateRoot.AllocationPolicyOffset,
			MuiMasterPrivateRootField.ErrorState =>
				MuiMasterPrivateRoot.ErrorStateOffset,
			MuiMasterPrivateRootField.ApplicationHead =>
				MuiMasterPrivateRoot.ApplicationHeadOffset,
			MuiMasterPrivateRootField.ExternalClassHead =>
				MuiMasterPrivateRoot.ExternalClassHeadOffset,
			MuiMasterPrivateRootField.CallbackState =>
				MuiMasterPrivateRoot.CallbackStateOffset,
			MuiMasterPrivateRootField.LoaderState =>
				MuiMasterPrivateRoot.LoaderStateOffset,
			MuiMasterPrivateRootField.RegistryGeneration =>
				MuiMasterPrivateRoot.RegistryGenerationOffset,
			MuiMasterPrivateRootField.ActiveDispatchDepth =>
				MuiMasterPrivateRoot.ActiveDispatchDepthOffset,
			MuiMasterPrivateRootField.ActiveCallbackDepth =>
				MuiMasterPrivateRoot.ActiveCallbackDepthOffset,
			MuiMasterPrivateRootField.Flags => MuiMasterPrivateRoot.FlagsOffset,
			MuiMasterPrivateRootField.Reserved =>
				MuiMasterPrivateRoot.ReservedOffset,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiMasterPrivateRootField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, MuiMasterPrivateRoot.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiMasterPrivateRoot.FieldSize);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMasterPrivateRootFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		TryGetAddress(ref platform, cursor.Record, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiMasterPrivateRootField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiMasterPrivateRootField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiMasterPrivateRootFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMasterPrivateRootFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMasterPrivateRootRecordMemoryCodec.TryGetAddress(ref platform, cursor,
			out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiMasterPrivateRootField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMasterPrivateRootRecordMemoryCodec.TryReadUInt32(ref platform, record,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiMasterPrivateRootField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMasterPrivateRootRecordMemoryCodec.TryWriteUInt32(ref platform, record,
			field, value);
}

internal static class MuiMasterPrivateRootCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiMasterPrivateRoot value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiMasterPrivateRoot.Size)) return false;
		if (!MuiMasterPrivateRootRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiMasterPrivateRootField.ClassRegistry,
			out value.ClassRegistry) ||
			!MuiMasterPrivateRootRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiMasterPrivateRootField.AllocationPolicy,
				out value.AllocationPolicy) ||
			!MuiMasterPrivateRootRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiMasterPrivateRootField.ErrorState,
				out value.ErrorState) ||
			!MuiMasterPrivateRootRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiMasterPrivateRootField.ApplicationHead,
				out value.ApplicationHead) ||
			!MuiMasterPrivateRootRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiMasterPrivateRootField.ExternalClassHead,
				out value.ExternalClassHead) ||
			!MuiMasterPrivateRootRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiMasterPrivateRootField.CallbackState,
				out value.CallbackState) ||
			!MuiMasterPrivateRootRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiMasterPrivateRootField.LoaderState,
				out value.LoaderState) ||
			!MuiMasterPrivateRootRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiMasterPrivateRootField.RegistryGeneration,
				out value.RegistryGeneration) ||
			!MuiMasterPrivateRootRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiMasterPrivateRootField.ActiveDispatchDepth,
				out value.ActiveDispatchDepth) ||
			!MuiMasterPrivateRootRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiMasterPrivateRootField.ActiveCallbackDepth,
				out value.ActiveCallbackDepth) ||
			!MuiMasterPrivateRootRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiMasterPrivateRootField.Flags, out value.Flags) ||
			!MuiMasterPrivateRootRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiMasterPrivateRootField.Reserved, out value.Reserved))
			return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiMasterPrivateRoot value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiMasterPrivateRoot.Size)) return false;
		return MuiMasterPrivateRootRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiMasterPrivateRootField.ClassRegistry, value.ClassRegistry) &&
			MuiMasterPrivateRootRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiMasterPrivateRootField.AllocationPolicy,
				value.AllocationPolicy) &&
			MuiMasterPrivateRootRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiMasterPrivateRootField.ErrorState, value.ErrorState) &&
			MuiMasterPrivateRootRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiMasterPrivateRootField.ApplicationHead,
				value.ApplicationHead) &&
			MuiMasterPrivateRootRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiMasterPrivateRootField.ExternalClassHead,
				value.ExternalClassHead) &&
			MuiMasterPrivateRootRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiMasterPrivateRootField.CallbackState,
				value.CallbackState) &&
			MuiMasterPrivateRootRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiMasterPrivateRootField.LoaderState,
				value.LoaderState) &&
			MuiMasterPrivateRootRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiMasterPrivateRootField.RegistryGeneration,
				value.RegistryGeneration) &&
			MuiMasterPrivateRootRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiMasterPrivateRootField.ActiveDispatchDepth,
				value.ActiveDispatchDepth) &&
			MuiMasterPrivateRootRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiMasterPrivateRootField.ActiveCallbackDepth,
				value.ActiveCallbackDepth) &&
			MuiMasterPrivateRootRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiMasterPrivateRootField.Flags, value.Flags) &&
			MuiMasterPrivateRootRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiMasterPrivateRootField.Reserved, value.Reserved);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiErrorState
{
	public const uint Size = 16;
	public int MuiError;
	public int IoError;
	public int FailingLvo;
	public uint Sequence;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiClassRegistryState
{
	public const uint Size = 24;
	public uint BuiltinHead;
	public uint BuiltinTail;
	public uint ExternalHead;
	public uint ExternalTail;
	public ushort BuiltinCount;
	public ushort ExternalCount;
	public uint Generation;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiAllocationPolicy
{
	public const uint Size = 24;
	public uint MaximumSingleAllocation;
	public uint MaximumOwnedBytes;
	public uint CurrentOwnedBytes;
	public uint AllocationCount;
	public uint FailureSequence;
	public uint Flags;
}

public static class MuiMasterState
{
	public static MuiMasterPrivateRoot CreateEmptyRoot()
	{
		MuiMasterPrivateRoot root = default;
		root.RegistryGeneration = 1;
		return root;
	}

	public static void InitializeEmptyRoot(out MuiMasterPrivateRoot root)
	{
		root.ClassRegistry = 0;
		root.AllocationPolicy = 0;
		root.ErrorState = 0;
		root.ApplicationHead = 0;
		root.ExternalClassHead = 0;
		root.CallbackState = 0;
		root.LoaderState = 0;
		root.RegistryGeneration = 1;
		root.ActiveDispatchDepth = 0;
		root.ActiveCallbackDepth = 0;
		root.Flags = 0;
		root.Reserved = 0;
	}

	public static MuiErrorState SetError(MuiErrorState state, int muiError,
		int ioError, int failingLvo)
	{
		state.MuiError = muiError;
		state.IoError = ioError;
		state.FailingLvo = failingLvo;
		state.Sequence++;
		return state;
	}
}
