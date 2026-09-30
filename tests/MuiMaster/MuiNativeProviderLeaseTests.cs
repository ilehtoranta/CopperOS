/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster.Tests;

// Host transaction proofs only. These handles model acquisition ownership,
// not native provider implementations or compatibility of their functions.
public sealed partial class MuiNativeProviderLeaseTests
{
	private static readonly APTR Root = APTR.FromPointer(0x1000);
	private static readonly APTR LibraryBase = APTR.FromPointer(0x1800);
	private const uint Acquiring = 1;
	private const uint Ready = 2;
	private const uint Closing = 3;

	[Fact]
	public void RequestAndAuthorityLayoutsArePackedAndThePlatformHasOnlyThirteenCapabilities()
	{
		Assert.Equal(30, Unsafe.SizeOf<MuiNativeProviderRequest>());
		Assert.Equal(40, Unsafe.SizeOf<MuiNativeClassLease>());
		Assert.Equal(36, Unsafe.SizeOf<MuiNativeProviderRetirement>());
		Assert.Equal(13, typeof(LeasePlatform).GetMethods(BindingFlags.Instance |
			BindingFlags.Public | BindingFlags.DeclaredOnly).Length);
		Assert.False(typeof(IMuiAllocationPlatform).IsAssignableFrom(typeof(LeasePlatform)));
		Assert.False(typeof(IMuiClassServicePlatform).IsAssignableFrom(typeof(LeasePlatform)));
	}

	[Fact]
	public void CoreProvidersAndOptionalKeymapPublishTogetherAndFinalLeaveRetiresThemOutsideCriticalSections()
	{
		var platform = NewPlatform();
		platform.State.ProbeReentry = true;
		var original = Owner(ref platform);
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref platform, out var lease));
		AssertProviderRequest(platform.State);
		Assert.Equal(5, platform.State.LiveHandles.Values.Sum());
		Assert.Empty(platform.State.Closed);
		var value = Owner(ref platform);
		Assert.Equal(Ready, value.Phase);
		Assert.Equal(1u, value.ActiveOperations);
		Assert.Equal(ExpectedProviders(platform.State), lease.Providers);
		Assert.Equal(platform.State.Replies[4], lease.KeymapBase);
		Assert.Equal(lease.Providers, Providers(value));
		Assert.Equal(lease.KeymapBase, value.KeymapBase);
		Assert.Equal(LibraryBase, lease.LibraryBase);
		Assert.Equal(Root, lease.OwnerRoot);
		Assert.Equal(platform.State.Owner, lease.Owner);
		Assert.Equal(original.RegistryGeneration, lease.RegistryGeneration);
		Assert.True(MuiNativeClassOwnerCodec.TryGetServiceAddress(ref platform, platform.State.Owner, out var service));
		Assert.Equal(service, lease.Service);
		Assert.False(MuiNativeClassOwnerCore.CanDetach(ref platform, LibraryBase, Root));
		Assert.True(MuiNativeClassLeaseCore.Leave(ref platform, ref lease));
		Assert.Equal(default(MuiNativeClassLease), lease);
		Assert.Equal(original, Owner(ref platform));
		AssertReverseCloses(platform.State);
		Assert.Equal(10, platform.State.ReentrySamples);
		Assert.True(MuiNativeClassOwnerCore.CanDetach(ref platform, LibraryBase, Root));
		AssertBalanced(platform.State);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	public void EveryFailedOpenClosesOnlySuccessfulHandlesInReverseOrder(int failedOpen)
	{
		var platform = NewPlatform();
		platform.State.FailOpen = failedOpen;
		platform.State.ProbeReentry = true;
		var original = Owner(ref platform);
		Assert.Equal(MuiNativeClassLeaseResult.Unavailable, Enter(ref platform, out var lease));
		Assert.Equal(default(MuiNativeClassLease), lease);
		Assert.Equal(failedOpen, platform.State.Opened.Count);
		Assert.Equal(failedOpen - 1, platform.State.Successful.Count);
		AssertReverseCloses(platform.State);
		Assert.Equal(original, Owner(ref platform));
		Assert.Equal(0, platform.State.NonzeroProviderWrites);
		AssertBalanced(platform.State);
	}

	[Fact]
	public void MissingOptionalKeymapKeepsCoreProvidersAvailableAndRetiresTheFourRequiredBases()
	{
		var platform = NewPlatform();
		platform.State.FailOpen = 5;
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref platform, out var lease));
		Assert.Equal(APTR.Null, lease.KeymapBase);
		Assert.Equal(APTR.Null, Owner(ref platform).KeymapBase);
		Assert.Equal(5, platform.State.Opened.Count);
		Assert.Equal(4, platform.State.LiveHandles.Values.Sum());
		Assert.True(MuiNativeClassLeaseCore.Leave(ref platform, ref lease));
		Assert.Equal(new[] { platform.State.Replies[3], platform.State.Replies[2],
			platform.State.Replies[1], platform.State.Replies[0] }, platform.State.Closed);
		Assert.Empty(platform.State.LiveHandles);
		AssertBalanced(platform.State);
	}

	[Fact]
	public void ReadyReusePinsTheSameProvidersWithoutRequiringNamesOrOpeningAgain()
	{
		var platform = NewPlatform();
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref platform, out var first));
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, MuiNativeClassLeaseCore.Enter(ref platform,
			LibraryBase, Root, default, out var second));
		Assert.Equal(first.Providers, second.Providers);
		Assert.Equal(2u, Owner(ref platform).ActiveOperations);
		Assert.Equal(5, platform.State.Opened.Count);
		Assert.True(MuiNativeClassLeaseCore.Leave(ref platform, ref first));
		Assert.Equal(1u, Owner(ref platform).ActiveOperations);
		Assert.Equal(Ready, Owner(ref platform).Phase);
		Assert.Empty(platform.State.Closed);
		Assert.False(MuiNativeClassLeaseCore.Leave(ref platform, ref first));
		Assert.Equal(1u, Owner(ref platform).ActiveOperations);
		Assert.True(MuiNativeClassLeaseCore.Leave(ref platform, ref second));
		AssertReverseCloses(platform.State);
		AssertBalanced(platform.State);
	}

	[Fact]
	public void SameValuedHandlesRepresentFiveDistinctOpenReferences()
	{
		var platform = NewPlatform();
		Array.Fill(platform.State.Replies, APTR.FromPointer(0x9000));
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref platform, out var lease));
		Assert.Equal(5, platform.State.LiveHandles[0x9000]);
		Assert.True(MuiNativeClassLeaseCore.Leave(ref platform, ref lease));
		Assert.Equal(5, platform.State.Closed.Count);
		Assert.All(platform.State.Closed, handle => Assert.Equal(APTR.FromPointer(0x9000), handle));
		Assert.Empty(platform.State.LiveHandles);
		AssertBalanced(platform.State);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	public void MissingProviderNameRejectsBeforeAcquiringAnything(int missing)
	{
		var platform = NewPlatform();
		var request = platform.State.Request;
		if (missing == 0) request.UtilityName = APTR.Null;
		if (missing == 1) request.DosName = APTR.Null;
		if (missing == 2) request.GraphicsName = APTR.Null;
		if (missing == 3) request.IntuitionName = APTR.Null;
		if (missing == 4) request.KeymapName = APTR.Null;
		var before = Owner(ref platform);
		Assert.Equal(MuiNativeClassLeaseResult.Invalid, MuiNativeClassLeaseCore.Enter(ref platform,
			LibraryBase, Root, request, out var lease));
		Assert.Equal(default(MuiNativeClassLease), lease);
		Assert.Equal(before, Owner(ref platform));
		Assert.Empty(platform.State.Opened);
		AssertBalanced(platform.State);
	}

	[Theory]
	[InlineData("Service.Head")]
	[InlineData("Registry.Classes")]
	[InlineData("Registry.Objects")]
	[InlineData("ActiveOperations")]
	[InlineData("Root.ApplicationHead")]
	[InlineData("Root.ExternalClassHead")]
	[InlineData("Root.ActiveDispatchDepth")]
	[InlineData("Root.ActiveCallbackDepth")]
	public void EmptyPhaseCannotAcquireProvidersOverExistingWork(string field)
	{
		var platform = NewPlatform();
		var owner = Owner(ref platform);
		var root = RootValue(ref platform);
		if (field == "Service.Head") owner.Service.Head = Root;
		if (field == "Registry.Classes") owner.Registry.Classes = Root;
		if (field == "Registry.Objects") owner.Registry.Objects = Root;
		if (field == "ActiveOperations") owner.ActiveOperations = 1;
		if (field == "Root.ApplicationHead") root.ApplicationHead = 1;
		if (field == "Root.ExternalClassHead") root.ExternalClassHead = 1;
		if (field == "Root.ActiveDispatchDepth") root.ActiveDispatchDepth = 1;
		if (field == "Root.ActiveCallbackDepth") root.ActiveCallbackDepth = 1;
		WriteOwner(ref platform, owner);
		Assert.True(MuiMasterPrivateRootCodec.Write(ref platform, Root, root));
		Assert.Equal(MuiNativeClassLeaseResult.Invalid, Enter(ref platform, out var lease));
		Assert.Equal(default(MuiNativeClassLease), lease);
		Assert.Equal(owner, Owner(ref platform));
		Assert.Equal(root, RootValue(ref platform));
		Assert.Empty(platform.State.Opened);
		AssertBalanced(platform.State);
	}

	[Theory]
	[InlineData("Type")]
	[InlineData("NegativeSize")]
	[InlineData("PositiveSize")]
	[InlineData("PrivateRoot")]
	[InlineData("ExecBase")]
	[InlineData("OpenCount")]
	public void InvalidLibraryAdmissionDoesNotOpenProviders(string field)
	{
		var platform = NewPlatform();
		var library = Library(ref platform);
		switch (field)
		{
			case "Type": library.Header.Node.Type = 0; break;
			case "NegativeSize": library.Header.NegativeSize = 0; break;
			case "PositiveSize": library.Header.PositiveSize = 0; break;
			case "PrivateRoot": library.PrivateRoot = APTR.FromPointer(0x1100); break;
			case "ExecBase": library.ExecBase = APTR.Null; break;
			case "OpenCount": library.Header.OpenCount = 0; break;
		}
		Assert.True(MuiExecLibraryBaseCodec.Write(ref platform, LibraryBase, library));
		var before = Owner(ref platform);
		Assert.Equal(MuiNativeClassLeaseResult.Invalid, Enter(ref platform, out var lease));
		Assert.Equal(default(MuiNativeClassLease), lease);
		Assert.Equal(before, Owner(ref platform));
		Assert.Empty(platform.State.Opened);
		AssertBalanced(platform.State);
	}

	[Theory]
	[InlineData(Acquiring)]
	[InlineData(Closing)]
	public void AcquisitionAndClosingAreBusyRatherThanMissingProviders(uint phase)
	{
		var platform = NewPlatform();
		var value = Owner(ref platform);
		value.Phase = phase;
		value.ActiveOperations = 1;
		WriteOwner(ref platform, value);
		Assert.Equal(MuiNativeClassLeaseResult.Busy, MuiNativeClassLeaseCore.Enter(ref platform,
			LibraryBase, Root, default, out var lease));
		Assert.Equal(default(MuiNativeClassLease), lease);
		Assert.Equal(value, Owner(ref platform));
		Assert.Empty(platform.State.Opened);
		AssertBalanced(platform.State);
	}

	[Fact]
	public void SaturatedReadyPinCountRefusesWithoutOpeningOrWrapping()
	{
		var platform = NewPlatform();
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref platform, out var first));
		var value = Owner(ref platform);
		value.ActiveOperations = uint.MaxValue;
		WriteOwner(ref platform, value);
		Assert.Equal(MuiNativeClassLeaseResult.Busy, Enter(ref platform, out var rejected));
		Assert.Equal(default(MuiNativeClassLease), rejected);
		Assert.Equal(value, Owner(ref platform));
		Assert.Equal(5, platform.State.Opened.Count);
		value.ActiveOperations = 1;
		WriteOwner(ref platform, value);
		Assert.True(MuiNativeClassLeaseCore.Leave(ref platform, ref first));
		AssertReverseCloses(platform.State);
		AssertBalanced(platform.State);
	}

	[Theory]
	[InlineData("LibraryBase")]
	[InlineData("OwnerRoot")]
	[InlineData("Owner")]
	[InlineData("RegistryGeneration")]
	[InlineData("Service")]
	public void ChangedLeaseIdentityCannotReleaseAnotherOperationPin(string field)
	{
		var platform = NewPlatform();
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref platform, out var lease));
		var invalid = lease;
		if (field == "LibraryBase") invalid.LibraryBase = APTR.FromPointer(0x1900);
		if (field == "OwnerRoot") invalid.OwnerRoot = APTR.FromPointer(0x1100);
		if (field == "Owner") invalid.Owner = APTR.FromPointer(lease.Owner.Raw + 4);
		if (field == "RegistryGeneration") invalid.RegistryGeneration++;
		if (field == "Service") invalid.Service = APTR.FromPointer(0x1100);
		var original = invalid;
		Assert.False(MuiNativeClassLeaseCore.Leave(ref platform, ref invalid));
		Assert.Equal(original, invalid);
		Assert.Equal(1u, Owner(ref platform).ActiveOperations);
		Assert.Empty(platform.State.Closed);
		Assert.True(MuiNativeClassLeaseCore.Leave(ref platform, ref lease));
		AssertReverseCloses(platform.State);
		AssertBalanced(platform.State);
	}

	[Fact]
	public void CallerChangedLeaseProviderCopiesDoNotControlRetirementHandles()
	{
		var platform = NewPlatform();
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref platform, out var lease));
		lease.Providers = ForeignProviders();
		Assert.True(MuiNativeClassLeaseCore.Leave(ref platform, ref lease));
		AssertReverseCloses(platform.State);
		AssertBalanced(platform.State);
	}

	[Fact]
	public void FinalLeaveKeepsProvidersWhileClassWorkRemainsAndIdleReleaseKeepsLibraryLinked()
	{
		var platform = NewPlatform();
		MakeReadyIdle(ref platform);
		Assert.Equal(Ready, Owner(ref platform).Phase);
		Assert.Equal(0u, Owner(ref platform).ActiveOperations);
		Assert.Equal(5, platform.State.LiveHandles.Values.Sum());
		var library = Library(ref platform);
		Assert.True(library.Header.OpenCount > 0);
		platform.State.ProbeReentry = true;
		Assert.True(MuiNativeClassLeaseCore.TryRetireIdle(ref platform, LibraryBase, Root));
		Assert.Equal(library, Library(ref platform));
		Assert.Equal(0u, Owner(ref platform).Phase);
		AssertReverseCloses(platform.State);
		AssertBalanced(platform.State);
	}

	[Theory]
	[InlineData("ActiveOperations")]
	[InlineData("Service.Head")]
	[InlineData("Registry.Classes")]
	[InlineData("Registry.Objects")]
	[InlineData("Registry.NotifyDepth")]
	[InlineData("Registry.Reserved")]
	public void BusyOwnerRefusesRetirementWithoutClosingAnyProvider(string field)
	{
		var platform = NewPlatform();
		MakeReadyIdle(ref platform);
		var original = Owner(ref platform);
		var value = original;
		switch (field)
		{
			case "ActiveOperations": value.ActiveOperations = 1; break;
			case "Service.Head": value.Service.Head = Root; break;
			case "Registry.Classes": value.Registry.Classes = Root; break;
			case "Registry.Objects": value.Registry.Objects = Root; break;
			case "Registry.NotifyDepth": value.Registry.NotifyDepth = 1; break;
			case "Registry.Reserved": value.Registry.Reserved = 1; break;
		}
		WriteOwner(ref platform, value);
		Assert.False(BeginRetirement(ref platform));
		Assert.False(MuiNativeClassLeaseCore.TryRetireIdle(ref platform, LibraryBase, Root));
		Assert.Equal(value, Owner(ref platform));
		Assert.Equal(5, platform.State.Opened.Count);
		Assert.Empty(platform.State.Closed);
		WriteOwner(ref platform, original);
		Assert.True(MuiNativeClassLeaseCore.TryRetireIdle(ref platform, LibraryBase, Root));
		AssertReverseCloses(platform.State);
		AssertBalanced(platform.State);
	}

	[Theory]
	[InlineData("ApplicationHead")]
	[InlineData("ExternalClassHead")]
	[InlineData("ActiveDispatchDepth")]
	[InlineData("ActiveCallbackDepth")]
	[InlineData("Flags")]
	public void BusyRootRefusesRetirementWithoutAnyExternalCalls(string field)
	{
		var platform = NewPlatform();
		MakeReadyIdle(ref platform);
		var original = RootValue(ref platform);
		var value = original;
		if (field == "ApplicationHead") value.ApplicationHead = 1;
		if (field == "ExternalClassHead") value.ExternalClassHead = 1;
		if (field == "ActiveDispatchDepth") value.ActiveDispatchDepth = 1;
		if (field == "ActiveCallbackDepth") value.ActiveCallbackDepth = 1;
		if (field == "Flags") value.Flags = 1;
		Assert.True(MuiMasterPrivateRootCodec.Write(ref platform, Root, value));
		Assert.False(BeginRetirement(ref platform));
		Assert.False(MuiNativeClassLeaseCore.TryRetireIdle(ref platform, LibraryBase, Root));
		Assert.Equal(value, RootValue(ref platform));
		Assert.Empty(platform.State.Closed);
		Assert.Equal(5, platform.State.Opened.Count);
		Assert.True(MuiMasterPrivateRootCodec.Write(ref platform, Root, original));
		Assert.True(MuiNativeClassLeaseCore.TryRetireIdle(ref platform, LibraryBase, Root));
		AssertBalanced(platform.State);
	}

	[Fact]
	public void RetirementKeepsTheOwnerPinnedThroughEveryCloseAndConsumesItsTicket()
	{
		var platform = NewPlatform();
		MakeReadyIdle(ref platform);
		Assert.True(BeginRetirement(ref platform));
		var value = Owner(ref platform);
		Assert.Equal(Closing, value.Phase);
		Assert.Equal(1u, value.ActiveOperations);
		Assert.Equal(default(MuiClassProviderBases), Providers(value));
		Assert.Equal(ExpectedProviders(platform.State), platform.State.Ticket.Providers);
		Assert.False(EndRetirement(ref platform));
		platform.State.ProbeReentry = true;
		MuiNativeClassLeaseCore.CloseRetiredProviders(ref platform, ref platform.State.Ticket);
		AssertReverseCloses(platform.State);
		Assert.Equal(default(MuiClassProviderBases), platform.State.Ticket.Providers);
		Assert.False(MuiNativeClassOwnerCore.CanDetach(ref platform, LibraryBase, Root));
		Assert.Equal(Closing, Owner(ref platform).Phase);
		Assert.Equal(1u, Owner(ref platform).ActiveOperations);
		Assert.True(EndRetirement(ref platform));
		Assert.Equal(default(MuiNativeProviderRetirement), platform.State.Ticket);
		Assert.Equal(0u, Owner(ref platform).Phase);
		Assert.Equal(0u, Owner(ref platform).ActiveOperations);
		Assert.False(EndRetirement(ref platform));
		MuiNativeClassLeaseCore.CloseRetiredProviders(ref platform, ref platform.State.Ticket);
		Assert.Equal(5, platform.State.Closed.Count);
		AssertBalanced(platform.State);
	}

	[Fact]
	public void SameRefRetirementCleanupCanReenterWithoutClosingAnySlotTwice()
	{
		var platform = NewPlatform();
		MakeReadyIdle(ref platform);
		Assert.True(BeginRetirement(ref platform));
		platform.State.ProbeReentry = true;
		platform.State.ReenterTicketClose = true;
		MuiNativeClassLeaseCore.CloseRetiredProviders(ref platform, ref platform.State.Ticket);
		AssertReverseCloses(platform.State);
		Assert.True(EndRetirement(ref platform));
		AssertBalanced(platform.State);
	}

	[Fact]
	public void FailedRetirementPublicationKeepsReadyHandlesAndLeavesNoTicket()
	{
		var platform = NewPlatform();
		MakeReadyIdle(ref platform);
		var before = Owner(ref platform);
		platform.State.RejectAddress = OwnerField(platform.State.Owner, nameof(MuiNativeClassOwnerRecord.GraphicsBase));
		platform.State.SkipAdmissions = 1;
		Assert.False(BeginRetirement(ref platform));
		Assert.True(platform.State.MappingRejected);
		Assert.Equal(default(MuiNativeProviderRetirement), platform.State.Ticket);
		Assert.Equal(before, Owner(ref platform));
		Assert.Empty(platform.State.Closed);
		Assert.Equal(5, platform.State.LiveHandles.Values.Sum());
		Assert.True(MuiNativeClassLeaseCore.TryRetireIdle(ref platform, LibraryBase, Root));
		AssertReverseCloses(platform.State);
		AssertBalanced(platform.State);
	}

	[Fact]
	public void FailedRetirementCompletionRetainsSameTicketForSafeRetryWithoutReclosing()
	{
		var platform = NewPlatform();
		MakeReadyIdle(ref platform);
		Assert.True(BeginRetirement(ref platform));
		MuiNativeClassLeaseCore.CloseRetiredProviders(ref platform, ref platform.State.Ticket);
		var before = Owner(ref platform);
		var ticket = platform.State.Ticket;
		platform.State.RejectAddress = OwnerField(platform.State.Owner, nameof(MuiNativeClassOwnerRecord.GraphicsBase));
		platform.State.SkipAdmissions = 1;
		Assert.False(EndRetirement(ref platform));
		Assert.True(platform.State.MappingRejected);
		Assert.Equal(ticket, platform.State.Ticket);
		Assert.Equal(before, Owner(ref platform));
		MuiNativeClassLeaseCore.CloseRetiredProviders(ref platform, ref platform.State.Ticket);
		Assert.Equal(5, platform.State.Closed.Count);
		Assert.True(EndRetirement(ref platform));
		AssertReverseCloses(platform.State);
		AssertBalanced(platform.State);
	}

	[Fact]
	public void RetirementUsesSavedHandlesRatherThanMutableOwnerOrMccCopies()
	{
		var platform = NewPlatform();
		MakeReadyIdle(ref platform);
		Assert.True(BeginRetirement(ref platform));
		var closing = Owner(ref platform);
		var changed = closing;
		var foreign = ForeignProviders();
		changed.UtilityBase = foreign.UtilityBase;
		changed.DosBase = foreign.DosBase;
		changed.GraphicsBase = foreign.GraphicsBase;
		changed.Context.IntuitionBase = foreign.IntuitionBase;
		WriteOwner(ref platform, changed);
		var mcc = new MuiCustomClassRecord { UtilityBase = foreign.UtilityBase, DosBase = foreign.DosBase,
			GfxBase = foreign.GraphicsBase, IntuitionBase = foreign.IntuitionBase };
		Assert.True(MuiCustomClassCodec.Write(ref platform, APTR.FromPointer(0x2500), mcc));
		MuiNativeClassLeaseCore.CloseRetiredProviders(ref platform, ref platform.State.Ticket);
		AssertReverseCloses(platform.State);
		Assert.True(MuiCustomClassCodec.TryRead(ref platform, APTR.FromPointer(0x2500), out var unchanged));
		Assert.Equal(mcc, unchanged);
		WriteOwner(ref platform, closing);
		Assert.True(EndRetirement(ref platform));
		AssertBalanced(platform.State);
	}

	[Fact]
	public void EmptyOwnerCanRetireIdempotentlyWithoutOpeningOrClosingAnything()
	{
		var platform = NewPlatform();
		var before = Owner(ref platform);
		Assert.True(MuiNativeClassLeaseCore.TryRetireIdle(ref platform, LibraryBase, Root));
		Assert.True(MuiNativeClassLeaseCore.TryRetireIdle(ref platform, LibraryBase, Root));
		Assert.Equal(before, Owner(ref platform));
		Assert.Empty(platform.State.Opened);
		Assert.Empty(platform.State.Closed);
		AssertBalanced(platform.State);
	}

	[Fact]
	public void FailedFinalProviderPublicationClosesSavedHandlesWithoutPublishingAnySubset()
	{
		var platform = NewPlatform();
		var before = Owner(ref platform);
		platform.State.RejectAfterKeymapOpen = OwnerField(platform.State.Owner, nameof(MuiNativeClassOwnerRecord.GraphicsBase));
		platform.State.SkipBeforeCommitAdmissions = 1; // owner identity read precedes control-field preflight
		platform.State.ProbeReentry = true;
		Assert.Equal(MuiNativeClassLeaseResult.Invalid, Enter(ref platform, out var lease));
		Assert.Equal(default(MuiNativeClassLease), lease);
		Assert.True(platform.State.MappingRejected);
		Assert.Equal(5, platform.State.Successful.Count);
		AssertReverseCloses(platform.State);
		Assert.Equal(0, platform.State.NonzeroProviderWrites);
		Assert.Equal(before, Owner(ref platform));
		AssertBalanced(platform.State);
	}

	[Fact]
	public void OwnerGenerationChangeDuringOpenRejectsCommitAndClosesEveryReturnedHandle()
	{
		var platform = NewPlatform();
		platform.State.ChangeGenerationDuringOpen = true;
		Assert.Equal(MuiNativeClassLeaseResult.Invalid, Enter(ref platform, out var lease));
		Assert.Equal(default(MuiNativeClassLease), lease);
		Assert.Equal(18u, RootValue(ref platform).RegistryGeneration);
		Assert.NotEqual(Ready, Owner(ref platform).Phase);
		Assert.Equal(default(MuiClassProviderBases), Providers(Owner(ref platform)));
		AssertReverseCloses(platform.State);
		AssertBalanced(platform.State);
	}

	[Fact]
	public void MissingProviderRollbackCanBeRetriedWithoutReusingOrLeakingOldReferences()
	{
		var platform = NewPlatform();
		platform.State.FailOpen = 3;
		Assert.Equal(MuiNativeClassLeaseResult.Unavailable, Enter(ref platform, out var rejected));
		Assert.Equal(default(MuiNativeClassLease), rejected);
		AssertReverseCloses(platform.State);
		platform.State.FailOpen = 0;
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref platform, out var lease));
		Assert.Equal(5, platform.State.LiveHandles.Values.Sum());
		Assert.True(MuiNativeClassLeaseCore.Leave(ref platform, ref lease));
		Assert.Equal(new[] { platform.State.Replies[1], platform.State.Replies[0],
			platform.State.Replies[4], platform.State.Replies[3], platform.State.Replies[2],
			platform.State.Replies[1], platform.State.Replies[0] },
			platform.State.Closed);
		Assert.Empty(platform.State.LiveHandles);
		AssertBalanced(platform.State);
	}

	private static MuiNativeClassLeaseResult Enter(ref LeasePlatform platform, out MuiNativeClassLease lease) =>
		MuiNativeClassLeaseCore.Enter(ref platform, LibraryBase, Root, platform.State.Request, out lease);

	private static void MakeReadyIdle(ref LeasePlatform platform)
	{
		Assert.Equal(MuiNativeClassLeaseResult.Acquired, Enter(ref platform, out var lease));
		var value = Owner(ref platform);
		value.Service.Head = Root; // a named nonempty-head state, not a fake class implementation
		WriteOwner(ref platform, value);
		Assert.True(MuiNativeClassLeaseCore.Leave(ref platform, ref lease));
		Assert.Equal(default(MuiNativeClassLease), lease);
		Assert.Equal(Ready, Owner(ref platform).Phase);
		Assert.Equal(0u, Owner(ref platform).ActiveOperations);
		Assert.Empty(platform.State.Closed);
		value = Owner(ref platform);
		value.Service.Head = APTR.Null;
		WriteOwner(ref platform, value);
	}

	private static bool BeginRetirement(ref LeasePlatform platform)
	{
		platform.EnterCritical();
		var result = MuiNativeClassLeaseCore.TryBeginRetirement(ref platform, LibraryBase, Root, out platform.State.Ticket);
		platform.LeaveCritical();
		return result;
	}

	private static bool EndRetirement(ref LeasePlatform platform)
	{
		platform.EnterCritical();
		var result = MuiNativeClassLeaseCore.TryEndRetirement(ref platform, ref platform.State.Ticket);
		platform.LeaveCritical();
		return result;
	}

	private static LeasePlatform NewPlatform()
	{
		var state = new LeaseState
		{
			Memory = new MuiHeadlessTestPlatform(Root.Raw, 0x20000, 0x4000, Root),
			Request = new MuiNativeProviderRequest
			{
				UtilityName = APTR.FromPointer(0x2000), DosName = APTR.FromPointer(0x2040),
				GraphicsName = APTR.FromPointer(0x2080), IntuitionName = APTR.FromPointer(0x20C0),
				KeymapName = APTR.FromPointer(0x2100),
				UtilityVersion = 11, DosVersion = 22, GraphicsVersion = 33, IntuitionVersion = 44,
				KeymapVersion = 55,
			},
		};
		state.Memory.WriteCString(state.Request.UtilityName, "utility.library");
		state.Memory.WriteCString(state.Request.DosName, "dos.library");
		state.Memory.WriteCString(state.Request.GraphicsName, "graphics.library");
		state.Memory.WriteCString(state.Request.IntuitionName, "intuition.library");
		state.Memory.WriteCString(state.Request.KeymapName, "keymap.library");
		MuiMasterState.InitializeEmptyRoot(out var root);
		root.RegistryGeneration = 17;
		Assert.True(MuiMasterPrivateRootCodec.Write(ref state.Memory, Root, root));
		var library = default(MuiExecLibraryBaseRecord);
		library.Header.Node.Type = (byte)NodeType.Library;
		library.Header.Node.Successor = APTR.FromPointer(0x1A00);
		library.Header.Node.Predecessor = APTR.FromPointer(0x1A10);
		library.Header.NegativeSize = MuiExecLibraryBaseRecord.NegativeBytes;
		library.Header.PositiveSize = MuiExecLibraryBaseRecord.PositiveBytes;
		library.Header.OpenCount = 2;
		library.ExecBase = APTR.FromPointer(0x10000);
		library.PrivateRoot = Root;
		Assert.True(MuiExecLibraryBaseCodec.Write(ref state.Memory, LibraryBase, library));
		Assert.True(MuiNativeClassOwnerCore.TryAttach(ref state.Memory, LibraryBase, Root, out state.Owner));
		return new LeasePlatform { State = state };
	}

	private static MuiNativeClassOwnerRecord Owner(ref LeasePlatform platform)
	{
		Assert.True(MuiNativeClassOwnerCodec.TryRead(ref platform.State.Memory, platform.State.Owner, out var value));
		return value;
	}
	private static void WriteOwner(ref LeasePlatform platform, MuiNativeClassOwnerRecord value) =>
		Assert.True(MuiNativeClassOwnerCodec.Write(ref platform, platform.State.Owner, value));
	private static MuiMasterPrivateRoot RootValue(ref LeasePlatform platform)
	{
		Assert.True(MuiMasterPrivateRootCodec.TryRead(ref platform.State.Memory, Root, out var value));
		return value;
	}
	private static MuiExecLibraryBaseRecord Library(ref LeasePlatform platform)
	{
		Assert.True(MuiExecLibraryBaseCodec.TryRead(ref platform.State.Memory, LibraryBase, out var value));
		return value;
	}
	private static APTR OwnerField(APTR owner, string field) =>
		APTR.FromPointer(owner.Raw + (uint)Marshal.OffsetOf<MuiNativeClassOwnerRecord>(field).ToInt32());
	private static MuiClassProviderBases Providers(MuiNativeClassOwnerRecord value) => new()
	{
		UtilityBase = value.UtilityBase, DosBase = value.DosBase,
		GraphicsBase = value.GraphicsBase, IntuitionBase = value.Context.IntuitionBase,
	};
	private static MuiClassProviderBases ExpectedProviders(LeaseState state) => new()
	{
		UtilityBase = state.Replies[0], DosBase = state.Replies[1],
		GraphicsBase = state.Replies[2], IntuitionBase = state.Replies[3],
	};
	private static MuiClassProviderBases ForeignProviders() => new()
	{
		UtilityBase = APTR.FromPointer(0xD001), DosBase = APTR.FromPointer(0xD002),
		GraphicsBase = APTR.FromPointer(0xD003), IntuitionBase = APTR.FromPointer(0xD004),
	};
	private static void AssertProviderRequest(LeaseState state) => Assert.Equal(new[]
	{
		new OpenRequest(state.Request.UtilityName, state.Request.UtilityVersion),
		new OpenRequest(state.Request.DosName, state.Request.DosVersion),
		new OpenRequest(state.Request.GraphicsName, state.Request.GraphicsVersion),
		new OpenRequest(state.Request.IntuitionName, state.Request.IntuitionVersion),
		new OpenRequest(state.Request.KeymapName, state.Request.KeymapVersion),
	}, state.Opened);
	private static void AssertReverseCloses(LeaseState state)
	{
		Assert.Equal(state.Successful.AsEnumerable().Reverse(), state.Closed);
		Assert.Empty(state.LiveHandles);
	}
	private static void AssertBalanced(LeaseState state)
	{
		Assert.Equal(0, state.CriticalDepth);
		Assert.Equal(state.EnterCount, state.LeaveCount);
		Assert.Equal(0, state.LoaderCallsInsideCritical);
	}

	private readonly record struct OpenRequest(APTR Name, ushort Version);
	private sealed class LeaseState
	{
		internal MuiHeadlessTestPlatform Memory;
		internal APTR Owner;
		internal MuiNativeProviderRequest Request;
		internal MuiNativeProviderRetirement Ticket;
		internal readonly APTR[] Replies = { APTR.FromPointer(0x9000), APTR.FromPointer(0x9100),
			APTR.FromPointer(0x9200), APTR.FromPointer(0x9300), APTR.FromPointer(0x9400) };
		internal readonly List<OpenRequest> Opened = new();
		internal readonly List<APTR> Successful = new();
		internal readonly List<APTR> Closed = new();
		internal readonly Dictionary<uint, int> LiveHandles = new();
		internal int FailOpen;
		internal int CriticalDepth;
		internal int EnterCount;
		internal int LeaveCount;
		internal int LoaderCallsInsideCritical;
		internal bool ProbeReentry;
		internal int ReentrySamples;
		internal bool ReenterTicketClose;
		internal bool ChangeGenerationDuringOpen;
		internal APTR RejectAfterKeymapOpen;
		internal int SkipBeforeCommitAdmissions;
		internal APTR RejectAddress;
		internal int SkipAdmissions;
		internal bool MappingRejected;
		internal int NonzeroProviderWrites;
	}

	private struct LeasePlatform : IMuiClassLeasePlatform
	{
		internal LeaseState State;
		public bool IsMapped(APTR address, uint bytes)
		{
			if (address == State.RejectAddress && bytes == sizeof(uint))
			{
				if (State.SkipAdmissions != 0) State.SkipAdmissions--;
				else
				{
					State.RejectAddress = APTR.Null;
					State.MappingRejected = true;
					return false;
				}
			}
			return State.Memory.IsMapped(address, bytes);
		}
		public byte ReadUInt8(APTR address, int offset) => State.Memory.ReadUInt8(address, offset);
		public ushort ReadUInt16(APTR address, int offset) => State.Memory.ReadUInt16(address, offset);
		public uint ReadUInt32(APTR address, int offset) => State.Memory.ReadUInt32(address, offset);
		public void WriteUInt8(APTR address, int offset, byte value) => State.Memory.WriteUInt8(address, offset, value);
		public void WriteUInt16(APTR address, int offset, ushort value) => State.Memory.WriteUInt16(address, offset, value);
		public void WriteUInt32(APTR address, int offset, uint value)
		{
			State.Memory.WriteUInt32(address, offset, value);
			if (value != 0 && (address == State.Owner ||
				address == OwnerField(State.Owner, nameof(MuiNativeClassOwnerRecord.UtilityBase)) ||
				address == OwnerField(State.Owner, nameof(MuiNativeClassOwnerRecord.DosBase)) ||
				address == OwnerField(State.Owner, nameof(MuiNativeClassOwnerRecord.GraphicsBase)) ||
				address == OwnerField(State.Owner, nameof(MuiNativeClassOwnerRecord.KeymapBase))))
				State.NonzeroProviderWrites++;
		}
		public void Clear(APTR address, uint bytes) => State.Memory.Clear(address, bytes);
		public void Copy(APTR source, APTR destination, uint bytes) => State.Memory.Copy(source, destination, bytes);
		public void EnterCritical() { State.CriticalDepth++; State.EnterCount++; }
		public void LeaveCritical()
		{
			Assert.True(State.CriticalDepth > 0);
			State.CriticalDepth--;
			State.LeaveCount++;
		}
		public APTR OpenLibrary(APTR name, ushort version)
		{
			AssertLoaderOutsideCritical();
			State.Opened.Add(new OpenRequest(name, version));
			ProbeLoaderReentry();
			if (State.ChangeGenerationDuringOpen)
			{
				State.ChangeGenerationDuringOpen = false;
				var root = RootValue(ref this);
				root.RegistryGeneration++;
				Assert.True(MuiMasterPrivateRootCodec.Write(ref this, Root, root));
			}
			if (State.Opened.Count == State.FailOpen) return APTR.Null;
			var index = -1;
			if (name == State.Request.UtilityName) index = 0;
			if (name == State.Request.DosName) index = 1;
			if (name == State.Request.GraphicsName) index = 2;
			if (name == State.Request.IntuitionName) index = 3;
			if (name == State.Request.KeymapName) index = 4;
			Assert.InRange(index, 0, State.Replies.Length - 1);
			var handle = State.Replies[index];
			State.Successful.Add(handle);
			State.LiveHandles.TryGetValue(handle.Raw, out var references);
			State.LiveHandles[handle.Raw] = references + 1;
			if (State.Opened.Count == 5 && State.RejectAfterKeymapOpen.IsNotNull)
			{
				State.RejectAddress = State.RejectAfterKeymapOpen;
				State.SkipAdmissions = State.SkipBeforeCommitAdmissions;
			}
			return handle;
		}
		public void CloseLibrary(APTR library)
		{
			AssertLoaderOutsideCritical();
			Assert.True(State.LiveHandles.TryGetValue(library.Raw, out var references) && references > 0,
				"Attempted to close an unowned or already-closed provider reference.");
			if (references == 1) State.LiveHandles.Remove(library.Raw);
			else State.LiveHandles[library.Raw] = references - 1;
			State.Closed.Add(library);
			ProbeLoaderReentry();
			if (State.ReenterTicketClose)
			{
				State.ReenterTicketClose = false;
				MuiNativeClassLeaseCore.CloseRetiredProviders(ref this, ref State.Ticket);
			}
		}
		private void AssertLoaderOutsideCritical()
		{
			if (State.CriticalDepth != 0) State.LoaderCallsInsideCritical++;
			Assert.Equal(0, State.CriticalDepth);
		}
		private void ProbeLoaderReentry()
		{
			if (!State.ProbeReentry) return;
			var value = Owner(ref this);
			Assert.True(value.Phase == Acquiring || value.Phase == Closing);
			Assert.Equal(1u, value.ActiveOperations);
			Assert.Equal(default(MuiClassProviderBases), Providers(value));
			var count = State.Opened.Count;
			Assert.Equal(MuiNativeClassLeaseResult.Busy, MuiNativeClassLeaseCore.Enter(ref this,
				LibraryBase, Root, default, out var rejected));
			Assert.Equal(default(MuiNativeClassLease), rejected);
			Assert.False(MuiNativeClassOwnerCore.CanDetach(ref this, LibraryBase, Root));
			Assert.False(MuiNativeClassLeaseCore.TryRetireIdle(ref this, LibraryBase, Root));
			Assert.Equal(count, State.Opened.Count);
			State.ReentrySamples++;
		}
	}
}
