/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

internal interface IMuiClassLeasePlatform : IMuiGuestMemory, IMuiLibraryLoaderCapability
{
	void EnterCritical();
	void LeaveCritical();
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeProviderRequest
{
	internal const uint Size = 30;
	internal APTR UtilityName;
	internal APTR DosName;
	internal APTR GraphicsName;
	internal APTR IntuitionName;
	internal APTR KeymapName;
	internal ushort UtilityVersion;
	internal ushort DosVersion;
	internal ushort GraphicsVersion;
	internal ushort IntuitionVersion;
	internal ushort KeymapVersion;
}

// A trusted internal operation token. Consume the same record once; copying a
// token does not create another operation or transfer ownership of its bases.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeClassLease
{
	internal const uint Size = 40;
	internal APTR LibraryBase;
	internal APTR OwnerRoot;
	internal APTR Owner;
	internal uint RegistryGeneration;
	internal APTR Service;
	internal MuiClassProviderBases Providers;
	internal APTR KeymapBase;
}

internal enum MuiNativeClassLeaseResult : uint
{
	Invalid = 0,
	Acquired = 1,
	Busy = 2,
	Unavailable = 3
}

// Only the initiating retirement frame owns completion authority. Provider
// callbacks may reenter normal library APIs, but do not receive this token.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeProviderRetirement
{
	internal const uint Size = 36;
	internal APTR LibraryBase;
	internal APTR OwnerRoot;
	internal APTR Owner;
	internal uint RegistryGeneration;
	internal MuiClassProviderBases Providers;
	internal APTR KeymapBase;
}

// Provider ownership only, not public MUI admission or class-list serialization.
// Critical sections protect short state transitions. Loader calls run outside
// this core's critical sections; an inherited caller restriction is not lifted.
// Stable owned mappings and non-failing admitted writes are required. Identity
// corruption leaves the owner pinned rather than reconstructing ownership.
internal static class MuiNativeClassLeaseCore
{
	internal const uint PhaseAcquiring = 1;
	internal const uint PhaseReady = 2;
	internal const uint PhaseClosing = 3;

	internal static MuiNativeClassLeaseResult Enter<T>(ref T platform,
		APTR library, APTR root, MuiNativeProviderRequest request,
		out MuiNativeClassLease lease) where T : struct, IMuiClassLeasePlatform
	{
		lease = default;
		platform.EnterCritical();
		var result = BeginOperation(ref platform, library, root, request,
			out var candidate, out var acquire);
		platform.LeaveCritical();
		if (result != MuiNativeClassLeaseResult.Acquired) return result;
		if (!acquire)
		{
			lease = candidate;
			return result;
		}

		var providers = default(MuiClassProviderBases);
		var keymapBase = APTR.Null;
		providers.UtilityBase = platform.OpenLibrary(request.UtilityName, request.UtilityVersion);
		if (providers.UtilityBase.IsNotNull)
			providers.DosBase = platform.OpenLibrary(request.DosName, request.DosVersion);
		if (providers.DosBase.IsNotNull)
			providers.GraphicsBase = platform.OpenLibrary(request.GraphicsName, request.GraphicsVersion);
		if (providers.GraphicsBase.IsNotNull)
			providers.IntuitionBase = platform.OpenLibrary(request.IntuitionName, request.IntuitionVersion);
		if (providers.IsComplete)
			keymapBase = platform.OpenLibrary(request.KeymapName, request.KeymapVersion);

		result = MuiNativeClassLeaseResult.Unavailable;
		if (providers.IsComplete)
		{
			platform.EnterCritical();
			var committed = CommitAcquisition(ref platform, candidate, providers, keymapBase);
			platform.LeaveCritical();
			if (committed)
			{
				candidate.Providers = providers;
				candidate.KeymapBase = keymapBase;
				lease = candidate;
				return MuiNativeClassLeaseResult.Acquired;
			}
			result = MuiNativeClassLeaseResult.Invalid;
		}

		CloseKeymap(ref platform, ref keymapBase);
		CloseProviders(ref platform, ref providers);
		platform.EnterCritical();
		var aborted = AbortAcquisition(ref platform, candidate);
		platform.LeaveCritical();
		return aborted ? result : MuiNativeClassLeaseResult.Invalid;
	}

	internal static bool Leave<T>(ref T platform, ref MuiNativeClassLease lease)
		where T : struct, IMuiClassLeasePlatform
	{
		platform.EnterCritical();
		var consumed = EndOperation(ref platform, ref lease, out var retirement);
		platform.LeaveCritical();
		if (!consumed) return false;
		if (retirement.Owner.IsNull) return true;
		CloseRetiredProviders(ref platform, ref retirement);
		platform.EnterCritical();
		var ended = TryEndRetirement(ref platform, ref retirement);
		platform.LeaveCritical();
		return ended;
	}

	internal static bool TryRetireIdle<T>(ref T platform, APTR library, APTR root)
		where T : struct, IMuiClassLeasePlatform
	{
		platform.EnterCritical();
		var begun = TryBeginRetirement(ref platform, library, root, out var retirement);
		platform.LeaveCritical();
		if (!begun) return false;
		CloseRetiredProviders(ref platform, ref retirement);
		platform.EnterCritical();
		var ended = TryEndRetirement(ref platform, ref retirement);
		platform.LeaveCritical();
		return ended;
	}

	// Caller supplies the critical section. This retires idle provider leases,
	// not the library itself: open clients and the library's Exec link remain.
	internal static bool TryBeginRetirement<T>(ref T memory, APTR library,
		APTR root, out MuiNativeProviderRetirement retirement)
		where T : struct, IMuiGuestMemory
	{
		retirement = default;
		if (!TryReadLibrary(ref memory, library, root, out _) ||
			!MuiNativeClassOwnerCore.TryReadAttached(ref memory, library, root,
				out var state, out var owner, out var value) ||
			!MuiNativeClassOwnerCore.IsQuiescent(ref memory, state, value, owner)) return false;
		return BeginRetirement(ref memory, library, root, owner, value, out retirement);
	}

	internal static void CloseRetiredProviders<T>(ref T loader,
		ref MuiNativeProviderRetirement retirement)
		where T : struct, IMuiLibraryLoaderCapability
	{
		CloseKeymap(ref loader, ref retirement.KeymapBase);
		CloseProviders(ref loader, ref retirement.Providers);
	}

	// Caller supplies the critical section. The token retains completion
	// authority on failure; no library-header snapshot is restored here.
	internal static bool TryEndRetirement<T>(ref T memory,
		ref MuiNativeProviderRetirement retirement) where T : struct, IMuiGuestMemory
	{
		if (retirement.Owner.IsNull || retirement.RegistryGeneration == 0 ||
			!AreEmpty(retirement.Providers) || retirement.KeymapBase.IsNotNull ||
			!MuiNativeClassOwnerCore.TryReadAttached(ref memory,
				retirement.LibraryBase, retirement.OwnerRoot, out var state,
				out var owner, out var value) ||
			owner != retirement.Owner || value.RegistryGeneration != retirement.RegistryGeneration ||
			value.Phase != PhaseClosing || value.ActiveOperations != 1 ||
			!AreEmpty(ProviderBases(value)) || value.KeymapBase.IsNotNull) return false;
		value.ActiveOperations = 0;
		if (!MuiNativeClassOwnerCore.IsQuiescent(ref memory, state, value, owner)) return false;
		value.Phase = MuiNativeClassOwnerCore.PhaseEmpty;
		if (!MuiNativeClassOwnerCodec.TryWriteControl(ref memory, owner, value)) return false;
		retirement = default;
		return true;
	}

	private static MuiNativeClassLeaseResult BeginOperation<T>(ref T memory,
		APTR library, APTR root, MuiNativeProviderRequest request,
		out MuiNativeClassLease candidate, out bool acquire)
		where T : struct, IMuiGuestMemory
	{
		candidate = default;
		acquire = false;
		if (!TryReadLibrary(ref memory, library, root, out var header) || header.Header.OpenCount == 0 ||
			!MuiNativeClassOwnerCore.TryReadAttached(ref memory, library, root,
				out var state, out var owner, out var value)) return MuiNativeClassLeaseResult.Invalid;
		if (value.Phase == PhaseAcquiring || value.Phase == PhaseClosing)
			return MuiNativeClassLeaseResult.Busy;
		var providers = ProviderBases(value);
		var keymapBase = value.KeymapBase;
		if (value.Phase == PhaseReady)
		{
			if (!providers.IsComplete) return MuiNativeClassLeaseResult.Invalid;
			if (value.ActiveOperations == uint.MaxValue) return MuiNativeClassLeaseResult.Busy;
			value.ActiveOperations++;
		}
		else if (value.Phase == MuiNativeClassOwnerCore.PhaseEmpty)
		{
			if (!MuiNativeClassOwnerCore.IsQuiescent(ref memory, state, value, owner) || !AreEmpty(providers) ||
				request.UtilityName.IsNull || request.DosName.IsNull ||
				request.GraphicsName.IsNull || request.IntuitionName.IsNull || request.KeymapName.IsNull)
				return MuiNativeClassLeaseResult.Invalid;
			value.Phase = PhaseAcquiring;
			value.ActiveOperations = 1;
			acquire = true;
		}
		else return MuiNativeClassLeaseResult.Invalid;
		if (!MuiNativeClassOwnerCodec.TryGetServiceAddress(ref memory, owner, out var service) ||
			!MuiNativeClassOwnerCodec.TryWriteControl(ref memory, owner, value))
			return MuiNativeClassLeaseResult.Invalid;
		candidate.LibraryBase = library;
		candidate.OwnerRoot = root;
		candidate.Owner = owner;
		candidate.RegistryGeneration = value.RegistryGeneration;
		candidate.Service = service;
		candidate.Providers = providers;
		candidate.KeymapBase = keymapBase;
		return MuiNativeClassLeaseResult.Acquired;
	}

	private static bool CommitAcquisition<T>(ref T memory,
		MuiNativeClassLease candidate, MuiClassProviderBases providers,
		APTR keymapBase)
		where T : struct, IMuiGuestMemory
	{
		if (!TryReadAcquiring(ref memory, candidate, out var value)) return false;
		SetProviders(ref value, providers);
		value.KeymapBase = keymapBase;
		value.Phase = PhaseReady;
		return MuiNativeClassOwnerCodec.TryWriteControl(ref memory, candidate.Owner, value);
	}

	private static bool AbortAcquisition<T>(ref T memory, MuiNativeClassLease candidate)
		where T : struct, IMuiGuestMemory
	{
		if (!TryReadAcquiring(ref memory, candidate, out var value)) return false;
		value.Phase = MuiNativeClassOwnerCore.PhaseEmpty;
		value.ActiveOperations = 0;
		return MuiNativeClassOwnerCodec.TryWriteControl(ref memory, candidate.Owner, value);
	}

	private static bool TryReadAcquiring<T>(ref T memory,
		MuiNativeClassLease candidate, out MuiNativeClassOwnerRecord value)
		where T : struct, IMuiGuestMemory =>
		MuiNativeClassOwnerCore.TryReadAttached(ref memory, candidate.LibraryBase,
			candidate.OwnerRoot, out _, out var owner, out value) &&
		owner == candidate.Owner && value.RegistryGeneration == candidate.RegistryGeneration &&
		value.Phase == PhaseAcquiring && value.ActiveOperations == 1 &&
		AreEmpty(ProviderBases(value)) && value.KeymapBase.IsNull;

	private static bool EndOperation<T>(ref T memory, ref MuiNativeClassLease lease,
		out MuiNativeProviderRetirement retirement) where T : struct, IMuiGuestMemory
	{
		retirement = default;
		if (lease.Owner.IsNull || lease.RegistryGeneration == 0 ||
			!MuiNativeClassOwnerCore.TryReadAttached(ref memory, lease.LibraryBase,
				lease.OwnerRoot, out var state, out var owner, out var value) ||
			owner != lease.Owner || value.RegistryGeneration != lease.RegistryGeneration ||
			value.Phase != PhaseReady || value.ActiveOperations == 0 ||
			!ProviderBases(value).IsComplete ||
			value.KeymapBase != lease.KeymapBase ||
			!MuiNativeClassOwnerCodec.TryGetServiceAddress(ref memory, owner, out var service) ||
			service != lease.Service) return false;
		value.ActiveOperations--;
		if (value.ActiveOperations == 0 &&
			MuiNativeClassOwnerCore.IsQuiescent(ref memory, state, value, owner))
		{
			if (!BeginRetirement(ref memory, lease.LibraryBase, lease.OwnerRoot,
				owner, value, out retirement)) return false;
		}
		else if (!MuiNativeClassOwnerCodec.TryWriteControl(ref memory, owner, value)) return false;
		lease = default;
		return true;
	}

	private static bool BeginRetirement<T>(ref T memory, APTR library, APTR root,
		APTR owner, MuiNativeClassOwnerRecord value,
		out MuiNativeProviderRetirement retirement) where T : struct, IMuiGuestMemory
	{
		retirement = default;
		var providers = ProviderBases(value);
		var keymapBase = value.KeymapBase;
		if (value.ActiveOperations != 0 ||
			(value.Phase != MuiNativeClassOwnerCore.PhaseEmpty && value.Phase != PhaseReady) ||
			(value.Phase == MuiNativeClassOwnerCore.PhaseEmpty &&
				(!AreEmpty(providers) || keymapBase.IsNotNull)) ||
			(value.Phase == PhaseReady && !providers.IsComplete)) return false;
		value.Phase = PhaseClosing;
		value.ActiveOperations = 1;
		SetProviders(ref value, default);
		value.KeymapBase = APTR.Null;
		if (!MuiNativeClassOwnerCodec.TryWriteControl(ref memory, owner, value)) return false;
		retirement.LibraryBase = library;
		retirement.OwnerRoot = root;
		retirement.Owner = owner;
		retirement.RegistryGeneration = value.RegistryGeneration;
		retirement.Providers = providers;
		retirement.KeymapBase = keymapBase;
		return true;
	}

	private static bool TryReadLibrary<T>(ref T memory, APTR library, APTR root,
		out MuiExecLibraryBaseRecord value) where T : struct, IMuiGuestMemory =>
		MuiExecLibraryBaseCodec.TryRead(ref memory, library, out value) &&
		value.Header.Node.Type == (byte)NodeType.Library &&
		value.Header.PositiveSize == MuiExecLibraryBaseRecord.PositiveBytes &&
		value.Header.NegativeSize == MuiExecLibraryBaseRecord.NegativeBytes &&
		value.ExecBase.IsNotNull && value.PrivateRoot == root && root.IsNotNull;

	private static MuiClassProviderBases ProviderBases(MuiNativeClassOwnerRecord value)
	{
		var providers = default(MuiClassProviderBases);
		providers.UtilityBase = value.UtilityBase;
		providers.DosBase = value.DosBase;
		providers.GraphicsBase = value.GraphicsBase;
		providers.IntuitionBase = value.Context.IntuitionBase;
		return providers;
	}

	private static void SetProviders(ref MuiNativeClassOwnerRecord value, MuiClassProviderBases providers)
	{
		value.UtilityBase = providers.UtilityBase;
		value.DosBase = providers.DosBase;
		value.GraphicsBase = providers.GraphicsBase;
		value.Context.IntuitionBase = providers.IntuitionBase;
	}

	private static bool AreEmpty(MuiClassProviderBases providers) =>
		providers.UtilityBase.IsNull && providers.DosBase.IsNull &&
		providers.GraphicsBase.IsNull && providers.IntuitionBase.IsNull;

	// Consume each successful open separately before its callback. Reentry with
	// the same record cannot close a consumed slot; coincident pointers still
	// represent separate references. No caller-visible MUI_CustomClass is read.
	private static void CloseProviders<T>(ref T loader, ref MuiClassProviderBases providers)
		where T : struct, IMuiLibraryLoaderCapability
	{
		var library = providers.IntuitionBase;
		providers.IntuitionBase = APTR.Null;
		if (library.IsNotNull) loader.CloseLibrary(library);
		library = providers.GraphicsBase;
		providers.GraphicsBase = APTR.Null;
		if (library.IsNotNull) loader.CloseLibrary(library);
		library = providers.DosBase;
		providers.DosBase = APTR.Null;
		if (library.IsNotNull) loader.CloseLibrary(library);
		library = providers.UtilityBase;
		providers.UtilityBase = APTR.Null;
		if (library.IsNotNull) loader.CloseLibrary(library);
	}

	private static void CloseKeymap<T>(ref T loader, ref APTR keymapBase)
		where T : struct, IMuiLibraryLoaderCapability
	{
		var library = keymapBase;
		keymapBase = APTR.Null;
		if (library.IsNotNull) loader.CloseLibrary(library);
	}
}
