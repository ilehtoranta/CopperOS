/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster.Tests;

// Deterministic storage-ownership tests, not a substitute native OS provider.
// Provider acquisition remains outside this owner-only fixture; public-vector
// lifecycle coverage lives in the independent native Exec fixture.
public sealed class MuiNativeClassOwnerTests
{
	private static readonly APTR Root = APTR.FromPointer(0x1000);
	private static readonly APTR OtherRoot = APTR.FromPointer(0x1100);
	private static readonly APTR Library = APTR.FromPointer(0x1800);
	private static readonly APTR OtherLibrary = APTR.FromPointer(0x1900);
	private static readonly APTR CodecAddress = APTR.FromPointer(0x4000);
	private const uint HeapStart = 0x4000;

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	[InlineData(6)]
	[InlineData(7)]
	[InlineData(8)]
	public void BusyOrMalformedServiceGatePreventsOwnerDetachment(int field)
	{
		var platform = NewPlatform();
		Assert.True(MuiNativeClassOwnerCore.TryAttach(ref platform, Library, Root, out var owner));
		Assert.True(MuiNativeClassOwnerCodec.TryGetGateAddress(ref platform, owner, out var gate));
		var value = default(SignalSemaphore);
		switch (field)
		{
			case 0: value.Owner = APTR.FromPointer(0x1234); break;
			case 1: value.NestCount = 1; break;
			case 2: value.QueueCount = 1; break;
			case 3: value.WaitQueue.Head = APTR.FromPointer(0x1234); break;
			case 4: value.WaitQueue.Tail = APTR.FromPointer(0x1234); break;
			case 5: value.MultipleLink.Waiter = APTR.FromPointer(0x1234); break;
			case 6: value.Link.Successor = APTR.FromPointer(0x1234); break;
			case 7: value.Link.Name = STRPTR.FromPointer(0x1234); break;
			case 8: value.Link.Type = (byte)NodeType.SignalSemaphore; break;
		}
		Assert.True(MuiSignalSemaphoreCodec.Write(ref platform, gate, value));
		AssertDetachRefused(ref platform, Library, Root);
		Assert.True(MuiSignalSemaphoreCodec.Write(ref platform, gate, default));
		Assert.True(MuiNativeClassOwnerCore.TryDetach(ref platform, Library, Root));
	}

	[Fact]
	public void ControlOnlyWritePreservesEveryNamedSemaphoreField()
	{
		var platform = NewPlatform();
		Assert.True(MuiNativeClassOwnerCore.TryAttach(ref platform, Library, Root, out var owner));
		Assert.True(MuiNativeClassOwnerCodec.TryGetGateAddress(ref platform, owner, out var gate));
		Assert.Equal(owner.Raw + (uint)Marshal.OffsetOf<MuiNativeClassOwnerRecord>(nameof(MuiNativeClassOwnerRecord.ServiceGate)).ToInt32(), gate.Raw);
		var semaphore = new SignalSemaphore
		{
			Link = new Node { Successor = APTR.FromPointer(1), Predecessor = APTR.FromPointer(2), Type = 3, Priority = -4, Name = STRPTR.FromPointer(5) },
			NestCount = -6,
			WaitQueue = new MinList { Head = APTR.FromPointer(7), Tail = APTR.FromPointer(8), TailPred = APTR.FromPointer(9) },
			MultipleLink = new SemaphoreRequest { Link = new MinNode { Successor = APTR.FromPointer(10), Predecessor = APTR.FromPointer(11) }, Waiter = APTR.FromPointer(12) },
			Owner = APTR.FromPointer(13), QueueCount = -14,
		};
		Assert.True(MuiSignalSemaphoreCodec.Write(ref platform, gate, semaphore));
		Assert.True(MuiNativeClassOwnerCodec.TryRead(ref platform, owner, out var record));
		Assert.Equal(semaphore, record.ServiceGate);
		record.ActiveOperations = 2;
		record.ServiceGate = default; // A stale snapshot must not overwrite Exec.
		Assert.True(MuiNativeClassOwnerCodec.TryWriteControl(ref platform, owner, record));
		Assert.True(MuiSignalSemaphoreCodec.TryRead(ref platform, gate, out var after));
		Assert.Equal(semaphore, after);
	}

	[Fact]
	public void PackedOwnerRoundTripsEveryNamedFieldAndPreservesAdjacentStorage()
	{
		Assert.Equal(48, Unsafe.SizeOf<MuiNativeClassOwnerScalarRecord>());
		Assert.Equal(48u, MuiNativeClassOwnerScalarRecord.Size);
		Assert.Equal(206, Unsafe.SizeOf<MuiNativeClassOwnerRecord>());
		Assert.Equal(206u, MuiNativeClassOwnerRecord.Size);
		var memory = new MuiHeadlessTestPlatform(CodecAddress.Raw,
			(int)MuiNativeClassOwnerRecord.Size + sizeof(uint), 0, CodecAddress);
		memory.WriteUInt32(CodecAddress, (int)MuiNativeClassOwnerRecord.Size, 0xAABBCCDD);
		var expected = SampleRecord();
		Assert.True(MuiNativeClassOwnerCodec.Write(ref memory, CodecAddress, expected));
		Assert.True(MuiNativeClassOwnerCodec.TryRead(ref memory, CodecAddress, out var actual));
		Assert.Equal(expected, actual);
		Assert.Equal(0xAABBCCDDu, memory.ReadUInt32(CodecAddress, (int)MuiNativeClassOwnerRecord.Size));
		Assert.True(MuiNativeClassOwnerCodec.TryGetServiceAddress(ref memory, CodecAddress, out var service));
		Assert.True(MuiNativeClassOwnerCodec.TryGetRegistryAddress(ref memory, CodecAddress, out var registry));
		Assert.True(MuiNativeClassOwnerCodec.TryGetAslStateAddress(ref memory,
			CodecAddress, out var aslState));
		Assert.True(MuiNativeClassOwnerCodec.TryGetRequesterStateAddress(ref memory,
			CodecAddress, out var requesterState));
		Assert.True(MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref memory,
			CodecAddress, out var publicObjects));
		Assert.True(MuiNativeClassOwnerCodec.TryGetDrawingStateAddress(ref memory,
			CodecAddress, out var drawing));
		Assert.Equal(CodecAddress.Raw + (uint)Marshal.OffsetOf<MuiNativeClassOwnerRecord>(nameof(MuiNativeClassOwnerRecord.Service)).ToInt32(), service.Raw);
		Assert.Equal(CodecAddress.Raw + (uint)Marshal.OffsetOf<MuiNativeClassOwnerRecord>(nameof(MuiNativeClassOwnerRecord.Registry)).ToInt32(), registry.Raw);
		Assert.Equal(CodecAddress.Raw + (uint)Marshal.OffsetOf<MuiNativeClassOwnerRecord>(nameof(MuiNativeClassOwnerRecord.Asl)).ToInt32(), aslState.Raw);
		Assert.Equal(CodecAddress.Raw + (uint)Marshal.OffsetOf<MuiNativeClassOwnerRecord>(nameof(MuiNativeClassOwnerRecord.Requester)).ToInt32(), requesterState.Raw);
		Assert.Equal(CodecAddress.Raw + (uint)Marshal.OffsetOf<MuiNativeClassOwnerRecord>(nameof(MuiNativeClassOwnerRecord.PublicObjects)).ToInt32(), publicObjects.Raw);
		Assert.Equal(CodecAddress.Raw + (uint)Marshal.OffsetOf<MuiNativeClassOwnerRecord>(nameof(MuiNativeClassOwnerRecord.Drawing)).ToInt32(), drawing.Raw);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(7)]
	[InlineData(8)]
	[InlineData(19)]
	[InlineData(31)]
	[InlineData(43)]
	[InlineData(44)]
	[InlineData(59)]
	[InlineData(60)]
	[InlineData(91)]
	[InlineData(92)]
	[InlineData(137)]
	[InlineData(153)]
	[InlineData(169)]
	public void TruncatedOwnerRejectsWholeRecordAndAddressHelpersBeforeMutation(int mappedBytes)
	{
		var memory = new MuiHeadlessTestPlatform(CodecAddress.Raw, mappedBytes, 0, CodecAddress);
		for (var index = 0; index < mappedBytes; index++) memory.WriteUInt8(CodecAddress, index, 0xA5);
		Assert.False(MuiNativeClassOwnerCodec.Write(ref memory, CodecAddress, SampleRecord()));
		Assert.False(MuiNativeClassOwnerCodec.TryRead(ref memory, CodecAddress, out var rejected));
		Assert.Equal(default(MuiNativeClassOwnerRecord), rejected);
		Assert.False(MuiNativeClassOwnerCodec.TryGetServiceAddress(ref memory, CodecAddress, out var service));
		Assert.False(MuiNativeClassOwnerCodec.TryGetRegistryAddress(ref memory, CodecAddress, out var registry));
		Assert.Equal(APTR.Null, service);
		Assert.Equal(APTR.Null, registry);
		for (var index = 0; index < mappedBytes; index++) Assert.Equal(0xA5, memory.ReadUInt8(CodecAddress, index));
	}

	[Fact]
	public void AttachPublishesExactlyOneInitializedOwnerAndDetachRestoresTheRoot()
	{
		var platform = NewPlatform();
		var original = ReadRoot(ref platform, Root);
		Assert.True(MuiNativeClassOwnerCore.TryAttach(ref platform, Library, Root, out var owner));
		Assert.Single(platform.Live);
		Assert.Equal(MuiNativeClassOwnerRecord.Size, platform.Live[owner.Raw]);
		Assert.True(MuiNativeClassOwnerCodec.TryRead(ref platform, owner, out var value));
		Assert.True(MuiNativeClassOwnerCodec.TryGetRegistryAddress(ref platform, owner, out var registry));
		Assert.True(MuiNativeClassOwnerCodec.TryGetServiceAddress(ref platform, owner, out var service));
		Assert.Equal(Root, value.Context.OwnerRoot);
		Assert.Equal(APTR.Null, value.Context.IntuitionBase);
		Assert.Equal(0x4D554F31u, value.Magic);
		Assert.Equal(MuiNativeClassOwnerCore.Version, value.Version);
		Assert.Equal(Library, value.LibraryBase);
		Assert.Equal(original.RegistryGeneration, value.RegistryGeneration);
		Assert.Equal(0u, value.Phase);
		Assert.Equal(0u, value.ActiveOperations);
		Assert.Equal(APTR.Null, value.UtilityBase);
		Assert.Equal(APTR.Null, value.DosBase);
		Assert.Equal(APTR.Null, value.GraphicsBase);
		Assert.Equal(APTR.Null, value.KeymapBase);
		Assert.Equal(MuiClassServiceLayout.Magic, value.Service.Magic);
		Assert.Equal(1u, value.Service.Generation);
		Assert.Equal(APTR.Null, value.Service.Head);
		Assert.Equal(registry, value.Service.Headless);
		Assert.Equal(MuiHeadlessLayout.Magic, value.Registry.Magic);
		Assert.Equal(MuiHeadlessLayout.Version, value.Registry.Version);
		Assert.Equal(1u, value.Registry.NextSequence);
		Assert.Equal(APTR.Null, value.Registry.Classes);
		Assert.Equal(APTR.Null, value.Registry.Objects);
		Assert.True(MuiClassServiceStateCodec.TryRead(ref platform, service, out var embeddedService));
		Assert.Equal(value.Service, embeddedService);
		var attached = original;
		attached.LoaderState = owner.Raw;
		attached.CallbackState = owner.Raw;
		attached.ClassRegistry = registry.Raw;
		Assert.Equal(attached, ReadRoot(ref platform, Root));
		Assert.True(MuiNativeClassOwnerCore.CanDetach(ref platform, Library, Root));
		Assert.True(MuiNativeClassOwnerCore.TryDetach(ref platform, Library, Root));
		Assert.Equal(original, ReadRoot(ref platform, Root));
		Assert.Empty(platform.Live);
		Assert.Equal(new[] { owner }, platform.Freed.Select(item => item.Address));
		Assert.Equal(MuiNativeClassOwnerRecord.Size, platform.Freed[0].Bytes);
	}

	[Fact]
	public void OwnerContractNeedsOnlyElevenMemoryAndAllocationCapabilities()
	{
		Assert.Equal(11, typeof(OwnerPlatform).GetMethods(System.Reflection.BindingFlags.Public |
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly).Length);
		Assert.False(typeof(IMuiClassServicePlatform).IsAssignableFrom(typeof(OwnerPlatform)));
		Assert.False(typeof(IMuiLibraryLoaderCapability).IsAssignableFrom(typeof(OwnerPlatform)));
	}

	[Fact]
	public void FailedAllocationLeavesNoPublishedLinksOrOwnedMemory()
	{
		var platform = NewPlatform();
		platform.FailAllocation = 1;
		AssertFailedAttach(ref platform);
		Assert.Equal(1, platform.AllocationCalls);
		Assert.Empty(platform.Freed);
	}

	[Fact]
	public void FailedOwnerInitializationReleasesOnlyItsDetachedAllocation()
	{
		var platform = NewPlatform();
		platform.RejectFirstOwnerAdmission = true;
		AssertFailedAttach(ref platform);
		Assert.True(platform.MappingRejected);
		Assert.Single(platform.Freed);
		Assert.Equal(MuiNativeClassOwnerRecord.Size, platform.Freed[0].Bytes);
	}

	[Fact]
	public void RejectedFinalRootAdmissionRollsBackWithoutPartialPublication()
	{
		var platform = NewPlatform();
		platform.RejectAfterOwnerWriteAddress = Root;
		platform.RejectAfterOwnerWriteBytes = MuiMasterPrivateRoot.Size;
		AssertFailedAttach(ref platform);
		Assert.True(platform.MappingRejected);
		Assert.Single(platform.Freed);
		Assert.Equal(MuiNativeClassOwnerRecord.Size, platform.Freed[0].Bytes);
	}

	[Fact]
	public void LastNamedLinkAdmissionFailurePreventsAnyAttachRootWrite()
	{
		var platform = NewPlatform();
		Assert.True(MuiMasterPrivateRootRecordMemoryCodec.TryGetAddress(ref platform,
			Root, MuiMasterPrivateRootField.LoaderState, out var loader));
		platform.RejectAfterOwnerWriteAddress = loader;
		platform.RejectAfterOwnerWriteBytes = sizeof(uint);
		platform.SkipAfterOwnerWriteAdmissions = 1; // fresh root read precedes publication
		var writes = platform.RootWriteCount;
		AssertFailedAttach(ref platform);
		Assert.True(platform.MappingRejected);
		Assert.Equal(writes, platform.RootWriteCount);
		Assert.Single(platform.Freed);
	}

	[Theory]
	[InlineData("ClassRegistry")]
	[InlineData("AllocationPolicy")]
	[InlineData("ErrorState")]
	[InlineData("ApplicationHead")]
	[InlineData("ExternalClassHead")]
	[InlineData("CallbackState")]
	[InlineData("LoaderState")]
	[InlineData("RegistryGeneration")]
	[InlineData("ActiveDispatchDepth")]
	[InlineData("ActiveCallbackDepth")]
	[InlineData("Flags")]
	[InlineData("Reserved")]
	public void NonemptyOrInvalidUnownedRootRejectsAttachBeforeAllocation(string field)
	{
		var platform = NewPlatform();
		var root = ReadRoot(ref platform, Root);
		MutateRoot(ref root, field);
		Assert.True(MuiMasterPrivateRootCodec.Write(ref platform, Root, root));
		AssertFailedAttach(ref platform);
		Assert.Equal(0, platform.AllocationCalls);
		Assert.False(MuiNativeClassOwnerCore.CanDetach(ref platform, Library, Root));
		Assert.False(MuiNativeClassOwnerCore.TryDetach(ref platform, Library, Root));
		Assert.Equal(root, ReadRoot(ref platform, Root));
	}

	[Theory]
	[InlineData(0u, 0x1000u)]
	[InlineData(0x1800u, 0u)]
	[InlineData(0x1000u, 0x1000u)]
	[InlineData(0x102Eu, 0x1000u)]
	[InlineData(0x1800u, 0x1802u)]
	[InlineData(0x1800u, 0xFFFFF000u)]
	public void InvalidOrOverlappingBorrowedInputsRejectBeforeAllocation(uint library, uint root)
	{
		var platform = NewPlatform();
		Assert.False(MuiNativeClassOwnerCore.TryAttach(ref platform, APTR.FromPointer(library),
			APTR.FromPointer(root), out var owner));
		Assert.Equal(APTR.Null, owner);
		Assert.Equal(0, platform.AllocationCalls);
		Assert.Empty(platform.Live);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void TruncatedBorrowedRecordRejectsBeforeAllocation(bool library)
	{
		var platform = NewPlatform();
		platform.HiddenAddress = library ? Library : Root;
		platform.HiddenBytes = library ? MuiExecLibraryBaseRecord.Size : MuiMasterPrivateRoot.Size;
		AssertFailedAttach(ref platform);
		Assert.Equal(0, platform.AllocationCalls);
	}

	[Theory]
	[InlineData(0x1000u)]
	[InlineData(0x102Eu)]
	[InlineData(0x1800u)]
	[InlineData(0x182Eu)]
	public void BrokenAllocatorCannotMakeTheOwnerFreeOverlappingBorrowedStorage(uint reply)
	{
		var platform = NewPlatform();
		platform.AllocationReplyOverride = APTR.FromPointer(reply);
		var root = ReadRoot(ref platform, Root);
		Assert.False(MuiNativeClassOwnerCore.TryAttach(ref platform, Library, Root, out var owner));
		Assert.Equal(APTR.Null, owner);
		Assert.Equal(root, ReadRoot(ref platform, Root));
		Assert.Empty(platform.Freed);
		Assert.Empty(platform.Live);
		Assert.Equal(1, platform.AllocationCalls);
	}

	[Theory]
	[InlineData("Context.IntuitionBase")]
	[InlineData("Context.OwnerRoot")]
	[InlineData("Magic")]
	[InlineData("Version")]
	[InlineData("LibraryBase")]
	[InlineData("RegistryGeneration")]
	[InlineData("Phase")]
	[InlineData("ActiveOperations")]
	[InlineData("UtilityBase")]
	[InlineData("DosBase")]
	[InlineData("GraphicsBase")]
	[InlineData("KeymapBase")]
	[InlineData("ActiveDispatchFrames")]
	[InlineData("DispatchFramesPoisoned")]
	[InlineData("InvalidDispatchFramesPoison")]
	[InlineData("Service.Magic")]
	[InlineData("Service.Generation")]
	[InlineData("Service.Head")]
	[InlineData("Service.Headless")]
	[InlineData("Registry.Magic")]
	[InlineData("Registry.Version")]
	[InlineData("Registry.Classes")]
	[InlineData("Registry.Objects")]
			[InlineData("Registry.NotifyDepth")]
	[InlineData("Registry.Reserved")]
	[InlineData("Requester.Magic")]
	[InlineData("Requester.Generation")]
	public void BusyOrMalformedOwnerCannotDetachOrReleaseStorage(string field)
	{
		var platform = NewPlatform();
		Assert.True(MuiNativeClassOwnerCore.TryAttach(ref platform, Library, Root, out var owner));
		Assert.True(MuiNativeClassOwnerCodec.TryRead(ref platform, owner, out var value));
		var original = value;
		MutateOwner(ref value, field);
		if (field == "Registry.Magic" || field == "Registry.Version")
		{
			// The normal registry codec enforces these header fields. A malformed
			// record is installed through its explicit structural test seam.
			Assert.True(MuiNativeClassOwnerCodec.TryGetRegistryAddress(ref platform, owner, out var registry));
			Assert.True(MuiHeadlessStateCodec.WriteStructuralRecord(ref platform, registry, value.Registry));
		}
		else Assert.True(MuiNativeClassOwnerCodec.Write(ref platform, owner, value));
		var bytes = SnapshotOwner(ref platform, owner);
		AssertDetachRefused(ref platform, Library, Root);
		Assert.Equal(bytes, SnapshotOwner(ref platform, owner));
		Assert.True(MuiNativeClassOwnerCodec.Write(ref platform, owner, original));
		Assert.True(MuiNativeClassOwnerCore.TryDetach(ref platform, Library, Root));
		Assert.Empty(platform.Live);
	}

	[Theory]
	[InlineData("ClassRegistry")]
	[InlineData("AllocationPolicy")]
	[InlineData("ErrorState")]
	[InlineData("ApplicationHead")]
	[InlineData("ExternalClassHead")]
	[InlineData("CallbackState")]
	[InlineData("LoaderState")]
	[InlineData("RegistryGeneration")]
	[InlineData("ActiveDispatchDepth")]
	[InlineData("ActiveCallbackDepth")]
	[InlineData("Flags")]
	[InlineData("Reserved")]
	public void ChangedRootOwnershipOrBusyStateCannotBeForceCleared(string field)
	{
		var platform = NewPlatform();
		Assert.True(MuiNativeClassOwnerCore.TryAttach(ref platform, Library, Root, out _));
		var original = ReadRoot(ref platform, Root);
		var changed = original;
		MutateRoot(ref changed, field);
		Assert.True(MuiMasterPrivateRootCodec.Write(ref platform, Root, changed));
		AssertDetachRefused(ref platform, Library, Root);
		Assert.True(MuiMasterPrivateRootCodec.Write(ref platform, Root, original));
		Assert.True(MuiNativeClassOwnerCore.TryDetach(ref platform, Library, Root));
	}

	[Fact]
	public void CumulativeRegistryCountersDoNotMakeAnOtherwiseEmptyOwnerBusy()
	{
		var platform = NewPlatform();
		Assert.True(MuiNativeClassOwnerCore.TryAttach(ref platform, Library, Root, out var owner));
		Assert.True(MuiNativeClassOwnerCodec.TryRead(ref platform, owner, out var value));
		value.Registry.NextSequence = uint.MaxValue;
		value.Registry.Mutation = uint.MaxValue;
		Assert.True(MuiNativeClassOwnerCodec.Write(ref platform, owner, value));
		Assert.True(MuiNativeClassOwnerCore.CanDetach(ref platform, Library, Root));
		Assert.True(MuiNativeClassOwnerCore.TryDetach(ref platform, Library, Root));
		Assert.Empty(platform.Live);
	}

	[Fact]
	public void DuplicateAttachRejectsAndEmptyDetachIsIdempotent()
	{
		var platform = NewPlatform();
		Assert.True(MuiNativeClassOwnerCore.CanDetach(ref platform, Library, Root));
		Assert.True(MuiNativeClassOwnerCore.TryDetach(ref platform, Library, Root));
		Assert.Empty(platform.Freed);
		Assert.True(MuiNativeClassOwnerCore.TryAttach(ref platform, Library, Root, out var owner));
		var attached = ReadRoot(ref platform, Root);
		Assert.False(MuiNativeClassOwnerCore.TryAttach(ref platform, Library, Root, out var duplicate));
		Assert.Equal(APTR.Null, duplicate);
		Assert.Equal(1, platform.AllocationCalls);
		Assert.Equal(attached, ReadRoot(ref platform, Root));
		Assert.True(MuiNativeClassOwnerCore.TryDetach(ref platform, Library, Root));
		Assert.True(MuiNativeClassOwnerCore.TryDetach(ref platform, Library, Root));
		Assert.True(MuiNativeClassOwnerCore.CanDetach(ref platform, Library, Root));
		Assert.Equal(new[] { owner }, platform.Freed.Select(item => item.Address));
		Assert.Empty(platform.Live);
	}

	[Fact]
	public void IndependentRootsRejectCrossOwnershipAndReleaseOnlyTheirOwnStorage()
	{
		var platform = NewPlatform();
		InitializeRoot(ref platform, OtherRoot, 29);
		Assert.True(MuiNativeClassOwnerCore.TryAttach(ref platform, Library, Root, out var first));
		Assert.True(MuiNativeClassOwnerCore.TryAttach(ref platform, OtherLibrary, OtherRoot, out var second));
		Assert.NotEqual(first, second);
		AssertDetachRefused(ref platform, OtherLibrary, Root);
		AssertDetachRefused(ref platform, Library, OtherRoot);
		var original = ReadRoot(ref platform, Root);
		var other = ReadRoot(ref platform, OtherRoot);
		var crossed = original;
		crossed.LoaderState = other.LoaderState;
		crossed.CallbackState = other.CallbackState;
		crossed.ClassRegistry = other.ClassRegistry;
		Assert.True(MuiMasterPrivateRootCodec.Write(ref platform, Root, crossed));
		AssertDetachRefused(ref platform, Library, Root);
		Assert.True(MuiMasterPrivateRootCodec.Write(ref platform, Root, original));
		Assert.True(MuiNativeClassOwnerCore.TryDetach(ref platform, Library, Root));
		Assert.Single(platform.Live);
		Assert.True(platform.Live.ContainsKey(second.Raw));
		Assert.Equal(other, ReadRoot(ref platform, OtherRoot));
		Assert.True(MuiNativeClassOwnerCore.TryDetach(ref platform, OtherLibrary, OtherRoot));
		Assert.Empty(platform.Live);
		Assert.Equal(new[] { first, second }, platform.Freed.Select(item => item.Address));
	}

	[Fact]
	public void UnreadableOwnedRecordRejectsDetachBeforeAnyMutationOrFree()
	{
		var platform = NewPlatform();
		Assert.True(MuiNativeClassOwnerCore.TryAttach(ref platform, Library, Root, out var owner));
		platform.HiddenAddress = owner;
		platform.HiddenBytes = MuiNativeClassOwnerRecord.Size;
		AssertDetachRefused(ref platform, Library, Root);
		platform.HiddenAddress = APTR.Null;
		Assert.True(MuiNativeClassOwnerCore.TryDetach(ref platform, Library, Root));
	}

	[Fact]
	public void RejectedDetachRootAdmissionPreservesOwnedRecordForRetry()
	{
		var platform = NewPlatform();
		Assert.True(MuiNativeClassOwnerCore.TryAttach(ref platform, Library, Root, out var owner));
		var root = ReadRoot(ref platform, Root);
		Assert.True(MuiNativeClassOwnerCodec.TryRead(ref platform, owner, out var value));
		platform.RejectAddress = Root;
		platform.RejectBytes = MuiMasterPrivateRoot.Size;
		Assert.False(MuiNativeClassOwnerCore.TryDetach(ref platform, Library, Root));
		Assert.True(platform.MappingRejected);
		Assert.Equal(root, ReadRoot(ref platform, Root));
		Assert.True(MuiNativeClassOwnerCodec.TryRead(ref platform, owner, out var after));
		Assert.Equal(value, after);
		Assert.Single(platform.Live);
		Assert.Empty(platform.Freed);
		Assert.True(MuiNativeClassOwnerCore.TryDetach(ref platform, Library, Root));
	}

	[Fact]
	public void LastNamedLinkAdmissionFailurePreventsAnyDetachRootWriteOrFree()
	{
		var platform = NewPlatform();
		Assert.True(MuiNativeClassOwnerCore.TryAttach(ref platform, Library, Root, out var owner));
		Assert.True(MuiMasterPrivateRootRecordMemoryCodec.TryGetAddress(ref platform,
			Root, MuiMasterPrivateRootField.LoaderState, out var loader));
		var root = ReadRoot(ref platform, Root);
		var bytes = SnapshotOwner(ref platform, owner);
		var writes = platform.RootWriteCount;
		platform.RejectAddress = loader;
		platform.RejectBytes = sizeof(uint);
		platform.SkipRejectedAdmissions = 1; // quiescence read precedes removal
		Assert.False(MuiNativeClassOwnerCore.TryDetach(ref platform, Library, Root));
		Assert.True(platform.MappingRejected);
		Assert.Equal(root, ReadRoot(ref platform, Root));
		Assert.Equal(bytes, SnapshotOwner(ref platform, owner));
		Assert.Equal(writes, platform.RootWriteCount);
		Assert.Single(platform.Live);
		Assert.Empty(platform.Freed);
		Assert.True(MuiNativeClassOwnerCore.TryDetach(ref platform, Library, Root));
	}

	private static void AssertFailedAttach(ref OwnerPlatform platform)
	{
		var original = ReadRoot(ref platform, Root);
		Assert.False(MuiNativeClassOwnerCore.TryAttach(ref platform, Library, Root, out var owner));
		Assert.Equal(APTR.Null, owner);
		Assert.Equal(original, ReadRoot(ref platform, Root));
		Assert.Empty(platform.Live);
	}

	private static void AssertDetachRefused(ref OwnerPlatform platform, APTR library, APTR root)
	{
		var original = ReadRoot(ref platform, root);
		var live = platform.Live.OrderBy(pair => pair.Key).ToArray();
		var frees = platform.Freed.Count;
		Assert.False(MuiNativeClassOwnerCore.CanDetach(ref platform, library, root));
		Assert.False(MuiNativeClassOwnerCore.TryDetach(ref platform, library, root));
		Assert.Equal(original, ReadRoot(ref platform, root));
		Assert.Equal(live, platform.Live.OrderBy(pair => pair.Key).ToArray());
		Assert.Equal(frees, platform.Freed.Count);
	}

	private static OwnerPlatform NewPlatform()
	{
		var platform = new OwnerPlatform
		{
			Fixture = new MuiHeadlessTestPlatform(Root.Raw, 0x20000, HeapStart, Root),
			Live = new Dictionary<uint, uint>(),
			Freed = new List<FreedAllocation>(),
		};
		InitializeRoot(ref platform, Root, 17);
		return platform;
	}

	private static void InitializeRoot(ref OwnerPlatform platform, APTR address, uint generation)
	{
		MuiMasterState.InitializeEmptyRoot(out var value);
		value.RegistryGeneration = generation;
		Assert.True(MuiMasterPrivateRootCodec.Write(ref platform, address, value));
	}

	private static MuiMasterPrivateRoot ReadRoot(ref OwnerPlatform platform, APTR address)
	{
		// Observation bypasses fault injection so the assertion cannot consume
		// the one rejected admission intended for the production operation.
		Assert.True(MuiMasterPrivateRootCodec.TryRead(ref platform.Fixture, address, out var value));
		return value;
	}

	private static byte[] SnapshotOwner(ref OwnerPlatform platform, APTR owner)
	{
		var bytes = new byte[MuiNativeClassOwnerRecord.Size];
		for (var index = 0; index < bytes.Length; index++) bytes[index] = platform.Fixture.ReadUInt8(owner, index);
		return bytes;
	}

	private static void MutateRoot(ref MuiMasterPrivateRoot value, string field)
	{
		switch (field)
		{
			case "ClassRegistry": value.ClassRegistry = 1; break;
			case "AllocationPolicy": value.AllocationPolicy = 1; break;
			case "ErrorState": value.ErrorState = 1; break;
			case "ApplicationHead": value.ApplicationHead = 1; break;
			case "ExternalClassHead": value.ExternalClassHead = 1; break;
			case "CallbackState": value.CallbackState = 1; break;
			case "LoaderState": value.LoaderState = 1; break;
			case "RegistryGeneration": value.RegistryGeneration = 0; break;
			case "ActiveDispatchDepth": value.ActiveDispatchDepth = 1; break;
			case "ActiveCallbackDepth": value.ActiveCallbackDepth = 1; break;
			case "Flags": value.Flags = 1; break;
			case "Reserved": value.Reserved = 1; break;
			default: Assert.Fail("Unknown root field: " + field); break;
		}
	}

	private static void MutateOwner(ref MuiNativeClassOwnerRecord value, string field)
	{
		switch (field)
		{
			case "Context.IntuitionBase": value.Context.IntuitionBase = Library; break;
			case "Context.OwnerRoot": value.Context.OwnerRoot = OtherRoot; break;
			case "Magic": value.Magic = 0; break;
			case "Version": value.Version++; break;
			case "LibraryBase": value.LibraryBase = OtherLibrary; break;
			case "RegistryGeneration": value.RegistryGeneration++; break;
			case "Phase": value.Phase = 1; break;
			case "ActiveOperations": value.ActiveOperations = 1; break;
			case "UtilityBase": value.UtilityBase = Library; break;
			case "DosBase": value.DosBase = Library; break;
			case "GraphicsBase": value.GraphicsBase = Library; break;
			case "KeymapBase": value.KeymapBase = Library; break;
			case "ActiveDispatchFrames": value.ActiveDispatchFrames = Root; break;
			case "DispatchFramesPoisoned": value.DispatchFramesPoisoned = 1; break;
			case "InvalidDispatchFramesPoison": value.DispatchFramesPoisoned = 2; break;
			case "Service.Magic": value.Service.Magic = 0; break;
			case "Service.Generation": value.Service.Generation = 0; break;
			case "Service.Head": value.Service.Head = Root; break;
			case "Service.Headless": value.Service.Headless = Root; break;
			case "Registry.Magic": value.Registry.Magic = 0; break;
			case "Registry.Version": value.Registry.Version++; break;
			case "Registry.Classes": value.Registry.Classes = Root; break;
			case "Registry.Objects": value.Registry.Objects = Root; break;
			case "Registry.NotifyDepth": value.Registry.NotifyDepth = 1; break;
			case "Registry.Reserved": value.Registry.Reserved = 1; break;
			case "Requester.Magic": value.Requester.Magic = 0; break;
			case "Requester.Generation": value.Requester.Generation++; break;
			default: Assert.Fail("Unknown owner field: " + field); break;
		}
	}

	private static MuiNativeClassOwnerRecord SampleRecord() => new()
	{
		Context = new MuiNativeClassContext { IntuitionBase = APTR.FromPointer(0x11223344), OwnerRoot = Root },
		Magic = 0x4D554F31, Version = 1, LibraryBase = Library, RegistryGeneration = 17,
		Phase = 2, ActiveOperations = 3, UtilityBase = APTR.FromPointer(0x22334455),
		DosBase = APTR.FromPointer(0x33445566), GraphicsBase = APTR.FromPointer(0x44556677),
		KeymapBase = APTR.FromPointer(0x55667788),
		Service = new MuiClassServiceStateRecord { Magic = 0x55667788, Head = OtherRoot, Headless = Root, Generation = 4 },
		Registry = new MuiHeadlessStateRecord { Magic = MuiHeadlessLayout.Magic, Version = MuiHeadlessLayout.Version, Classes = Library,
			Objects = OtherLibrary, NextSequence = 6, NotifyDepth = 7, Mutation = 8, Reserved = 9 },
		Asl = new MuiAslServiceStateRecord
		{
			Magic = MuiAslServiceLayout.Magic,
			Head = OtherLibrary,
			Generation = MuiAslServiceLayout.Version,
		},
		Requester = new MuiRequesterServiceStateRecord
		{
			Magic = MuiRequesterServiceLayout.Magic,
			Generation = MuiRequesterServiceLayout.Version,
		},
		PublicObjects = new MuiNativePublicObjectRegistryRecord
		{
			Signature = MuiNativePublicObjectRegistryRecord.Magic,
			Revision = MuiNativePublicObjectRegistryRecord.Version,
			Head = OtherRoot, Mutation = 10,
		},
	};

	private readonly record struct FreedAllocation(APTR Address, uint Bytes);

	// Deliberately excludes library and class capabilities. The dictionary
	// checks exact allocation ownership, not just matching total free counts.
	private struct OwnerPlatform : IMuiAllocationPlatform
	{
		internal MuiHeadlessTestPlatform Fixture;
		internal Dictionary<uint, uint> Live;
		internal List<FreedAllocation> Freed;
		internal int AllocationCalls;
		internal int FailAllocation;
		internal APTR AllocationReplyOverride;
		internal bool RejectFirstOwnerAdmission;
		internal APTR RejectAfterOwnerWriteAddress;
		internal uint RejectAfterOwnerWriteBytes;
		internal int SkipAfterOwnerWriteAdmissions;
		internal APTR RejectAddress;
		internal uint RejectBytes;
		internal int SkipRejectedAdmissions;
		internal uint RootWriteCount;
		internal APTR HiddenAddress;
		internal uint HiddenBytes;
		internal bool MappingRejected;
		private APTR _pendingOwnerWrite;
		private uint _ownerWrittenBytes;

		public bool IsMapped(APTR address, uint bytes)
		{
			if (address == HiddenAddress && bytes == HiddenBytes) return false;
			if (address == RejectAddress && bytes == RejectBytes)
			{
				if (SkipRejectedAdmissions != 0) SkipRejectedAdmissions--;
				else
				{
					RejectAddress = APTR.Null;
					MappingRejected = true;
					return false;
				}
			}
			if (!Fixture.IsMapped(address, bytes)) return false;
			if (address.Raw < HeapStart) return true;
			foreach (var item in Live)
				if (address.Raw >= item.Key && bytes <= item.Value &&
					address.Raw - item.Key <= item.Value - bytes) return true;
			return false;
		}
		public byte ReadUInt8(APTR address, int offset) => Fixture.ReadUInt8(address, offset);
		public ushort ReadUInt16(APTR address, int offset) => Fixture.ReadUInt16(address, offset);
		public uint ReadUInt32(APTR address, int offset) => Fixture.ReadUInt32(address, offset);
		public void WriteUInt8(APTR address, int offset, byte value)
		{
			Fixture.WriteUInt8(address, offset, value);
			ObserveOwnerWrite(address, sizeof(byte));
		}
		public void WriteUInt16(APTR address, int offset, ushort value)
		{
			Fixture.WriteUInt16(address, offset, value);
			ObserveOwnerWrite(address, sizeof(ushort));
		}
		public void WriteUInt32(APTR address, int offset, uint value)
		{
			Fixture.WriteUInt32(address, offset, value);
			if (address.Raw >= Root.Raw && address.Raw - Root.Raw < MuiMasterPrivateRoot.Size) RootWriteCount++;
			ObserveOwnerWrite(address, sizeof(uint));
		}
		private void ObserveOwnerWrite(APTR address, uint bytes)
		{
			if (_pendingOwnerWrite.IsNotNull && address.Raw >= _pendingOwnerWrite.Raw &&
				address.Raw - _pendingOwnerWrite.Raw < MuiNativeClassOwnerRecord.Size)
			{
				_ownerWrittenBytes += bytes;
				if (_ownerWrittenBytes == MuiNativeClassOwnerRecord.Size)
				{
					_pendingOwnerWrite = APTR.Null;
					RejectAddress = RejectAfterOwnerWriteAddress;
					RejectBytes = RejectAfterOwnerWriteBytes;
					SkipRejectedAdmissions = SkipAfterOwnerWriteAdmissions;
				}
			}
		}
		public void Clear(APTR address, uint bytes) => Fixture.Clear(address, bytes);
		public void Copy(APTR source, APTR destination, uint bytes) => Fixture.Copy(source, destination, bytes);
		public APTR Allocate(uint bytes, uint flags)
		{
			AllocationCalls++;
			if (AllocationCalls == FailAllocation) return APTR.Null;
			// This adversarial reply is borrowed storage, not a ledger allocation.
			// Do not clear it or pretend the test allocator legitimately owns it.
			if (AllocationReplyOverride.IsNotNull) return AllocationReplyOverride;
			var result = Fixture.Allocate(bytes, flags);
			if (result.IsNull) return result;
			Assert.True(Live.TryAdd(result.Raw, bytes));
			if (RejectFirstOwnerAdmission)
			{
				RejectFirstOwnerAdmission = false;
				RejectAddress = result;
				RejectBytes = MuiNativeClassOwnerRecord.Size;
			}
			if (RejectAfterOwnerWriteAddress.IsNotNull)
			{
				_pendingOwnerWrite = result;
				_ownerWrittenBytes = 0;
			}
			return result;
		}
		public void Free(APTR address, uint bytes)
		{
			Assert.True(Live.Remove(address.Raw, out var expected), "Attempted to free unowned or already-freed storage.");
			Assert.Equal(expected, bytes);
			Fixture.Free(address, bytes);
			Freed.Add(new FreedAllocation(address, bytes));
		}
	}
}
