/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;

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
