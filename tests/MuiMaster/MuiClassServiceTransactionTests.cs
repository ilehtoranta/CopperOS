using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

// These are deterministic transaction/failure tests, not native Intuition proof.
// Single rejected mapping admissions leave the fixture's owned memory alive so
// the core can restore snapshots and release its allocations without exceptions.
public sealed class MuiClassServiceTransactionTests
{
	private static readonly APTR Service = APTR.FromPointer(0x1000);
	private static readonly APTR Headless = APTR.FromPointer(0x1080);
	private static readonly APTR Name = APTR.FromPointer(0x1100);
	private static readonly APTR Dispatcher = APTR.FromPointer(0xD000);

	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	[InlineData(6)]
	[InlineData(7)]
	[InlineData(8)]
	[InlineData(9)]
	public void ExternalPreparationFailurePublishesNeitherListAndCanRetry(int stage)
	{
		var platform = NewPlatform();
		var externalName = APTR.FromPointer(0x1200);
		var libraryName = APTR.FromPointer(0x1280);
		platform.Fixture.WriteCString(externalName, "Example.mcc");
		platform.Fixture.WriteCString(libraryName, "mui/Example.mcc");
		platform.Fixture.LoadableLibraryName = libraryName;
		platform.Fixture.LoadableLibraryBase = APTR.FromPointer(0xD100);
		platform.Fixture.LoadablePublicClassId = externalName;
		platform.Fixture.LoadablePublicClass = APTR.FromPointer(0xD200);
		var live = platform.Fixture.AllocationCount - platform.Fixture.FreeCount;
		Assert.True(MuiHeadlessStateCodec.TryRead(ref platform, Headless, out var original));
		if (stage <= 4) platform.FailAllocation = platform.AllocationCalls + stage;
		else if (stage <= 6)
			platform.RejectAllocatedRecordSize = stage == 5 ? MuiHeadlessClassRecord.Size : MuiClassServiceLeaseRecord.Size;
		else
		{
			APTR field;
			if (stage == 7)
				Assert.True(MuiClassRecordMemoryCodec.TryGetAddress(ref platform, Service,
					MuiClassRecordKind.State, MuiClassRecordField.Head, out field));
			else
				Assert.True(MuiHeadlessStateMemoryCodec.TryGetAddress(ref platform, Headless,
					stage == 8 ? MuiHeadlessStateField.Classes : MuiHeadlessStateField.Mutation, out field));
			platform.ArmAfterAllocatedRecordWriteSize = MuiClassServiceLeaseRecord.Size;
			platform.RejectAddress = field;
			platform.RejectBytes = sizeof(uint);
		}
		Assert.True(MuiClassServiceCore.GetClass(ref platform, Service, externalName).IsNull);
		if (stage >= 5) Assert.True(platform.MappingRejected);
		Assert.True(Snapshot(ref platform).Head.IsNull);
		Assert.True(MuiHeadlessStateCodec.TryRead(ref platform, Headless, out var after));
		Assert.Equal(original, after);
		Assert.Equal(live, platform.Fixture.AllocationCount - platform.Fixture.FreeCount);
		Assert.Equal(stage == 1 ? 0u : 1u, platform.Fixture.CloseLibraryCount);
		var cls = MuiClassServiceCore.GetClass(ref platform, Service, externalName);
		Assert.True(cls.IsNotNull);
		Assert.True(MuiClassServiceCore.FreeClass(ref platform, Service, cls));
		Assert.Equal(live, platform.Fixture.AllocationCount - platform.Fixture.FreeCount);
	}

	[Theory]
	[InlineData(3)]
	[InlineData(4)]
	public void AllocationCallbackCannotObserveHalfPublishedExternalClass(int allocation)
	{
		var platform = NewPlatform();
		var externalName = APTR.FromPointer(0x1200);
		var libraryName = APTR.FromPointer(0x1280);
		platform.Fixture.WriteCString(externalName, "Example.mcc");
		platform.Fixture.WriteCString(libraryName, "mui/Example.mcc");
		platform.Fixture.LoadableLibraryName = libraryName;
		platform.Fixture.LoadableLibraryBase = APTR.FromPointer(0xD100);
		platform.Fixture.LoadablePublicClassId = externalName;
		platform.Fixture.LoadablePublicClass = APTR.FromPointer(0xD200);
		platform.OpenCallbackName = externalName;
		platform.AllocationCallbackNumber = platform.AllocationCalls + allocation;
		var cls = MuiClassServiceCore.GetClass(ref platform, Service, externalName);
		Assert.Equal(platform.Fixture.LoadablePublicClass, cls);
		Assert.Equal(cls, platform.OpenCallbackResult);
		var lease = FindLease(ref platform, cls);
		Assert.True(MuiClassServiceLeaseCodec.TryRead(ref platform, lease, out var value));
		Assert.True(value.Next.IsNull);
		Assert.Equal(2u, value.RefCount);
		Assert.Equal(platform.Fixture.LoadableLibraryBase, value.LibraryBase);
		Assert.Equal(2u, platform.Fixture.OpenLibraryCount);
		Assert.Equal(1u, platform.Fixture.CloseLibraryCount);
		Assert.True(MuiClassServiceCore.FreeClass(ref platform, Service, cls));
		Assert.True(MuiClassServiceCore.FreeClass(ref platform, Service, cls));
		Assert.Equal(2u, platform.Fixture.CloseLibraryCount);
		Assert.True(Snapshot(ref platform).Head.IsNull);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(true, true)]
	public void ReentrantExternalLoadUsesTheAlreadyPublishedLease(bool duringResolve, bool saturated)
	{
		var platform = NewPlatform();
		var externalName = APTR.FromPointer(0x1200);
		var libraryName = APTR.FromPointer(0x1280);
		platform.Fixture.WriteCString(externalName, "Example.mcc");
		platform.Fixture.WriteCString(libraryName, "mui/Example.mcc");
		platform.Fixture.LoadableLibraryName = libraryName;
		platform.Fixture.LoadableLibraryBase = APTR.FromPointer(0xD100);
		platform.Fixture.LoadablePublicClassId = externalName;
		platform.Fixture.LoadablePublicClass = APTR.FromPointer(0xD200);
		platform.OpenCallbackName = externalName;
		platform.LoadCallbackDuringResolve = duringResolve;
		platform.LoadCallbackSaturates = saturated;
		var cls = MuiClassServiceCore.GetClass(ref platform, Service, externalName);
		Assert.Equal(saturated ? APTR.Null : platform.Fixture.LoadablePublicClass, cls);
		var published = platform.OpenCallbackResult;
		Assert.Equal(platform.Fixture.LoadablePublicClass, published);
		Assert.Equal(saturated ? uint.MaxValue : 2u,
			MuiClassServiceCore.ReferenceCount(ref platform, Service, published));
		Assert.Equal(2u, platform.Fixture.OpenLibraryCount);
		Assert.Equal(1u, platform.Fixture.CloseLibraryCount);
		var lease = FindLease(ref platform, published);
		Assert.True(MuiClassServiceLeaseCodec.TryRead(ref platform, lease, out var value));
		Assert.True(value.Next.IsNull);
		if (saturated)
		{
			value.RefCount = 2;
			Assert.True(MuiClassServiceLeaseCodec.Write(ref platform, lease, value));
		}
		Assert.True(MuiClassServiceCore.FreeClass(ref platform, Service, published));
		Assert.Equal(1u, platform.Fixture.CloseLibraryCount);
		Assert.True(MuiClassServiceCore.FreeClass(ref platform, Service, published));
		Assert.Equal(2u, platform.Fixture.CloseLibraryCount);
		Assert.True(Snapshot(ref platform).Head.IsNull);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(true, true)]
	public void ExternalFinalCloseCannotExposeTheRetiredLeaseToItsCallback(bool reacquire, bool nonHead)
	{
		var platform = NewPlatform();
		var externalName = APTR.FromPointer(0x1200);
		var libraryName = APTR.FromPointer(0x1280);
		platform.Fixture.WriteCString(externalName, "Example.mcc");
		platform.Fixture.WriteCString(libraryName, "mui/Example.mcc");
		platform.Fixture.LoadableLibraryName = libraryName;
		platform.Fixture.LoadableLibraryBase = APTR.FromPointer(0xD100);
		platform.Fixture.LoadablePublicClassId = externalName;
		platform.Fixture.LoadablePublicClass = APTR.FromPointer(0xD200);
		var cls = MuiClassServiceCore.GetClass(ref platform, Service, externalName);
		Assert.True(cls.IsNotNull);
		var builtin = nonHead ? MuiClassServiceCore.GetClass(ref platform, Service, Name) : APTR.Null;
		platform.CloseCallbackClass = cls;
		platform.CloseCallbackName = reacquire ? externalName : APTR.Null;
		platform.CloseCallback = true;

		Assert.True(MuiClassServiceCore.FreeClass(ref platform, Service, cls));

		Assert.False(platform.CloseCallbackFreeSucceeded);
		Assert.Equal(0u, platform.CloseCallbackReferences);
		Assert.Equal(1u, platform.Fixture.CloseLibraryCount);
		Assert.Equal(reacquire ? 2u : 1u, platform.Fixture.OpenLibraryCount);
		Assert.Equal(reacquire ? 1u : 0u,
			MuiClassServiceCore.ReferenceCount(ref platform, Service, cls));
		if (reacquire)
		{
			Assert.Equal(cls, platform.CloseCallbackResult);
			Assert.True(MuiClassServiceCore.FreeClass(ref platform, Service, cls));
			Assert.Equal(2u, platform.Fixture.CloseLibraryCount);
		}
		if (nonHead) Assert.True(MuiClassServiceCore.FreeClass(ref platform, Service, builtin));
		Assert.True(Snapshot(ref platform).Head.IsNull);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ExternalRegistryRefusalRestoresDetachedServiceLease(bool rejectMapping)
	{
		var platform = NewPlatform();
		var externalName = APTR.FromPointer(0x1200);
		var libraryName = APTR.FromPointer(0x1280);
		platform.Fixture.WriteCString(externalName, "Example.mcc");
		platform.Fixture.WriteCString(libraryName, "mui/Example.mcc");
		platform.Fixture.LoadableLibraryName = libraryName;
		platform.Fixture.LoadableLibraryBase = APTR.FromPointer(0xD100);
		platform.Fixture.LoadablePublicClassId = externalName;
		platform.Fixture.LoadablePublicClass = APTR.FromPointer(0xD200);
		var cls = MuiClassServiceCore.GetClass(ref platform, Service, externalName);
		Assert.True(cls.IsNotNull);
		var lease = FindLease(ref platform, cls);
		Assert.True(MuiClassServiceLeaseCodec.TryRead(ref platform, lease, out var original));
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, original.HeadlessClass, out var registry));
		var freeCount = platform.Fixture.FreeCount;
		if (rejectMapping)
		{
			platform.RejectAddress = original.HeadlessClass;
			platform.RejectBytes = MuiHeadlessClassRecord.Size;
			platform.RejectionArmed = true;
		}
		else
		{
			registry.ObjectCount = 1;
			Assert.True(MuiHeadlessClassCodec.Write(ref platform, original.HeadlessClass, registry));
		}
		Assert.False(MuiClassServiceCore.FreeClass(ref platform, Service, cls));
		Assert.Equal(lease, Snapshot(ref platform).Head);
		Assert.Equal(1u, MuiClassServiceCore.ReferenceCount(ref platform, Service, cls));
		Assert.Equal(0u, platform.Fixture.CloseLibraryCount);
		Assert.Equal(freeCount, platform.Fixture.FreeCount);
		registry.ObjectCount = 0;
		Assert.True(MuiHeadlessClassCodec.Write(ref platform, original.HeadlessClass, registry));
		Assert.True(MuiClassServiceCore.FreeClass(ref platform, Service, cls));
		Assert.Equal(1u, platform.Fixture.CloseLibraryCount);
		Assert.True(Snapshot(ref platform).Head.IsNull);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void SaturatedParentRejectsBeforeNativeAllocationAndRestoresNamedReference(bool named)
	{
		var platform = NewPlatform();
		var parent = CreateParent(ref platform);
		Assert.True(MuiCustomClassCodec.TryRead(ref platform, parent, out var parentValue));
		var targetClass = named ? parentValue.Super : parentValue.Class;
		var lease = FindLease(ref platform, targetClass);
		Assert.True(MuiClassServiceLeaseCodec.TryRead(ref platform, lease, out var value));
		value.ChildCount = uint.MaxValue;
		Assert.True(MuiClassServiceLeaseCodec.Write(ref platform, lease, value));
		var allocationCount = platform.Fixture.AllocationCount;
		var freeCount = platform.Fixture.FreeCount;
		var makeCount = platform.Fixture.MakeCustomClassCount;

		Assert.Equal(APTR.Null, MuiClassServiceCore.CreateCustomClass(ref platform,
			Service, APTR.Null, named ? Name : APTR.Null,
			named ? APTR.Null : parent, 12, Dispatcher));

		Assert.Equal(allocationCount, platform.Fixture.AllocationCount);
		Assert.Equal(freeCount, platform.Fixture.FreeCount);
		Assert.Equal(makeCount, platform.Fixture.MakeCustomClassCount);
		Assert.True(MuiClassServiceLeaseCodec.TryRead(ref platform, lease, out var after));
		Assert.Equal(value.RefCount, after.RefCount);
		Assert.Equal(uint.MaxValue, after.ChildCount);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	public void FailedMccOrLeaseAllocationReleasesUnpublishedNativeClass(int failedAllocation)
	{
		var platform = NewPlatform();
		var parent = CreateParent(ref platform);
		var original = Snapshot(ref platform);
		var live = platform.Fixture.AllocationCount - platform.Fixture.FreeCount;
		var makeCount = platform.Fixture.MakeCustomClassCount;
		var freeClassCount = platform.Fixture.FreeCustomClassCount;
		platform.FailAllocation = platform.AllocationCalls + failedAllocation;

		Assert.Equal(APTR.Null, CreateChild(ref platform, parent));

		Assert.Equal(makeCount + 1, platform.Fixture.MakeCustomClassCount);
		Assert.Equal(freeClassCount + 1, platform.Fixture.FreeCustomClassCount);
		Assert.Equal(live, platform.Fixture.AllocationCount - platform.Fixture.FreeCount);
		Assert.Equal(original.Head, Snapshot(ref platform).Head);
		AssertParentChildren(ref platform, parent, 0);
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, parent));
	}

	[Theory]
	[InlineData(MuiCustomClassRecord.Size)]
	[InlineData(MuiClassServiceLeaseRecord.Size)]
	public void FailedDetachedRecordWriteLeavesNoPublishedLease(uint recordSize)
	{
		var platform = NewPlatform();
		var parent = CreateParent(ref platform);
		var original = Snapshot(ref platform);
		var live = platform.Fixture.AllocationCount - platform.Fixture.FreeCount;
		platform.RejectAllocatedRecordSize = recordSize;

		Assert.Equal(APTR.Null, CreateChild(ref platform, parent));

		Assert.True(platform.MappingRejected);
		Assert.Equal(live, platform.Fixture.AllocationCount - platform.Fixture.FreeCount);
		Assert.Equal(original.Head, Snapshot(ref platform).Head);
		AssertParentChildren(ref platform, parent, 0);
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, parent));
	}

	[Fact]
	public void FailedFinalPublicationRestoresParentCountAndServiceHead()
	{
		var platform = NewPlatform();
		var parent = CreateParent(ref platform);
		var original = Snapshot(ref platform);
		var live = platform.Fixture.AllocationCount - platform.Fixture.FreeCount;
		platform.ArmAfterAllocatedRecordWriteSize = MuiClassServiceLeaseRecord.Size;
		platform.RejectAddress = Service;
		platform.RejectBytes = MuiClassServiceStateRecord.Size;

		Assert.Equal(APTR.Null, CreateChild(ref platform, parent));

		Assert.True(platform.MappingRejected);
		Assert.Equal(live, platform.Fixture.AllocationCount - platform.Fixture.FreeCount);
		Assert.Equal(original.Head, Snapshot(ref platform).Head);
		AssertParentChildren(ref platform, parent, 0);
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, parent));
	}

	[Fact]
	public void UnreadableCustomRecordRejectsDeletionBeforeNativeFree()
	{
		var platform = NewPlatform();
		var parent = CreateParent(ref platform);
		var original = Snapshot(ref platform);
		var freeCount = platform.Fixture.FreeCount;
		platform.RejectAddress = parent;
		platform.RejectBytes = MuiCustomClassRecord.Size;
		platform.RejectionArmed = true;

		Assert.False(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, parent));

		Assert.True(platform.MappingRejected);
		Assert.Equal(0u, platform.NativeFreeCalls);
		Assert.Equal(freeCount, platform.Fixture.FreeCount);
		Assert.Equal(original.Head, Snapshot(ref platform).Head);
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, parent));
	}

	[Fact]
	public void UnreadableParentRejectsDeletionBeforeNativeFree()
	{
		var platform = NewPlatform();
		var parent = CreateParent(ref platform);
		var child = CreateChild(ref platform, parent);
		Assert.True(MuiCustomClassCodec.TryRead(ref platform, child, out var custom));
		var childLease = FindLease(ref platform, custom.Class);
		Assert.True(MuiClassServiceLeaseCodec.TryRead(ref platform, childLease, out var original));
		var invalid = original;
		invalid.SuperService = APTR.FromPointer(0x800000);
		Assert.True(MuiClassServiceLeaseCodec.Write(ref platform, childLease, invalid));

		Assert.False(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, child));
		Assert.Equal(0u, platform.NativeFreeCalls);
		AssertParentChildren(ref platform, parent, 1);

		Assert.True(MuiClassServiceLeaseCodec.Write(ref platform, childLease, original));
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, child));
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, parent));
	}

	[Fact]
	public void FailedParentWriteRejectsDeletionAndRestoresBeforeNativeFree()
	{
		var platform = NewPlatform();
		var parent = CreateParent(ref platform);
		var child = CreateChild(ref platform, parent);
		Assert.True(MuiCustomClassCodec.TryRead(ref platform, parent, out var custom));
		platform.ArmAfterRecordRead = FindLease(ref platform, custom.Class);
		platform.RejectAddress = platform.ArmAfterRecordRead;
		platform.RejectBytes = MuiClassServiceLeaseRecord.Size;
		var original = Snapshot(ref platform);

		Assert.False(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, child));

		Assert.True(platform.MappingRejected);
		Assert.Equal(0u, platform.NativeFreeCalls);
		Assert.Equal(original.Head, Snapshot(ref platform).Head);
		AssertParentChildren(ref platform, parent, 1);
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, child));
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, parent));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void NativeRefusalRestoresHeadOrInteriorLeaseAndParentCount(bool interior)
	{
		var platform = NewPlatform();
		var parent = CreateParent(ref platform);
		var child = CreateChild(ref platform, parent);
		var sibling = interior ? CreateChild(ref platform, parent) : APTR.Null;
		var original = Snapshot(ref platform);
		var freeCount = platform.Fixture.FreeCount;
		platform.RefuseNativeFree = true;

		Assert.False(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, child));

		Assert.Equal(1u, platform.NativeFreeCalls);
		Assert.Equal(freeCount, platform.Fixture.FreeCount);
		Assert.Equal(original.Head, Snapshot(ref platform).Head);
		AssertParentChildren(ref platform, parent, interior ? 2u : 1u);
		platform.RefuseNativeFree = false;
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, child));
		if (interior)
			Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, sibling));
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, parent));
	}

	[Fact]
	public void SaturatedObjectCountRejectsBeforeNewObject()
	{
		var platform = NewPlatform();
		var parent = CreateParent(ref platform);
		Assert.True(MuiCustomClassCodec.TryRead(ref platform, parent, out var custom));
		var lease = FindLease(ref platform, custom.Class);
		Assert.True(MuiClassServiceLeaseCodec.TryRead(ref platform, lease, out var value));
		value.ObjectCount = uint.MaxValue;
		Assert.True(MuiClassServiceLeaseCodec.Write(ref platform, lease, value));
		var allocations = platform.Fixture.AllocationCount;

		Assert.Equal(APTR.Null, MuiClassServiceCore.CreateCustomObject(ref platform,
			Service, parent, APTR.Null));

		Assert.Equal(0u, platform.NativeNewObjectCalls);
		Assert.Equal(allocations, platform.Fixture.AllocationCount);
	}

	[Fact]
	public void FailedObjectReservationRejectsBeforeNewObjectAndRestoresCount()
	{
		var platform = NewPlatform();
		var parent = CreateParent(ref platform);
		Assert.True(MuiCustomClassCodec.TryRead(ref platform, parent, out var custom));
		var lease = FindLease(ref platform, custom.Class);
		var live = platform.Fixture.AllocationCount - platform.Fixture.FreeCount;
		platform.RejectAddress = lease;
		platform.RejectBytes = MuiClassServiceLeaseRecord.Size;
		platform.ArmAfterRecordRead = lease;
		platform.SkipCompleteRecordReads = 1; // lookup, then reservation preflight

		Assert.Equal(APTR.Null, MuiClassServiceCore.CreateCustomObject(ref platform,
			Service, parent, APTR.Null));

		Assert.True(platform.MappingRejected);
		Assert.Equal(0u, platform.NativeNewObjectCalls);
		Assert.Equal(0u, platform.NativeDisposeCalls);
		Assert.Equal(live, platform.Fixture.AllocationCount - platform.Fixture.FreeCount);
		Assert.True(MuiClassServiceLeaseCodec.TryRead(ref platform, lease, out var after));
		Assert.Equal(0u, after.ObjectCount);
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, parent));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ConstructorReentryPreservesNestedObjectAndSubclassCounts(bool outerFails)
	{
		var platform = NewPlatform();
		var parent = CreateParent(ref platform);
		platform.CallbackClass = parent;
		platform.NewObjectCallback = outerFails ? (byte)2 : (byte)1;

		var outer = MuiClassServiceCore.CreateCustomObject(ref platform, Service,
			parent, APTR.Null);

		Assert.Equal(outerFails, outer.IsNull);
		Assert.True(platform.NestedObject.IsNotNull);
		Assert.True(platform.NestedClass.IsNotNull);
		Assert.False(platform.CallbackDeleteSucceeded);
		Assert.True(MuiCustomClassCodec.TryRead(ref platform, parent, out var custom));
		var lease = FindLease(ref platform, custom.Class);
		Assert.True(MuiClassServiceLeaseCodec.TryRead(ref platform, lease, out var value));
		Assert.Equal(outerFails ? 1u : 2u, value.ObjectCount);
		Assert.Equal(1u, value.ChildCount);
		Assert.False(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, parent));
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, platform.NestedClass));
		Assert.True(MuiClassServiceCore.DisposeCustomObject(ref platform, Service, parent, platform.NestedObject));
		if (!outerFails)
			Assert.True(MuiClassServiceCore.DisposeCustomObject(ref platform, Service, parent, outer));
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, parent));
	}

	[Fact]
	public void DestructorReentryPreservesNestedObjectAndSubclassCounts()
	{
		var platform = NewPlatform();
		var parent = CreateParent(ref platform);
		var outer = MuiClassServiceCore.CreateCustomObject(ref platform, Service, parent, APTR.Null);
		Assert.True(outer.IsNotNull);
		platform.CallbackClass = parent;
		platform.DisposeObjectCallback = true;

		Assert.True(MuiClassServiceCore.DisposeCustomObject(ref platform, Service, parent, outer));

		Assert.True(platform.NestedObject.IsNotNull);
		Assert.True(platform.NestedClass.IsNotNull);
		Assert.False(platform.CallbackDeleteSucceeded);
		Assert.True(MuiCustomClassCodec.TryRead(ref platform, parent, out var custom));
		var lease = FindLease(ref platform, custom.Class);
		Assert.True(MuiClassServiceLeaseCodec.TryRead(ref platform, lease, out var value));
		Assert.Equal(1u, value.ObjectCount);
		Assert.Equal(1u, value.ChildCount);
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, platform.NestedClass));
		Assert.True(MuiClassServiceCore.DisposeCustomObject(ref platform, Service, parent, platform.NestedObject));
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, parent));
	}

	private static TransactionPlatform NewPlatform()
	{
		var platform = new TransactionPlatform
		{
			Fixture = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, Headless),
		};
		platform.Fixture.WriteCString(Name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, Headless));
		Assert.True(MuiClassServiceCore.Initialize(ref platform, Service, Headless));
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			Headless, Name, APTR.Null, 8, Dispatcher).IsNotNull);
		return platform;
	}

	private static APTR CreateParent(ref TransactionPlatform platform)
	{
		var result = MuiClassServiceCore.CreateCustomClass(ref platform, Service,
			APTR.Null, Name, APTR.Null, 12, Dispatcher);
		Assert.True(result.IsNotNull);
		return result;
	}

	private static APTR CreateChild(ref TransactionPlatform platform, APTR parent) =>
		MuiClassServiceCore.CreateCustomClass(ref platform, Service, APTR.Null,
			APTR.Null, parent, 12, Dispatcher);

	private static MuiClassServiceStateRecord Snapshot(ref TransactionPlatform platform)
	{
		Assert.True(MuiClassServiceStateCodec.TryRead(ref platform, Service, out var state));
		return state;
	}

	private static APTR FindLease(ref TransactionPlatform platform, APTR cls)
	{
		var current = Snapshot(ref platform).Head;
		for (var visited = 0; visited < 16 && current.IsNotNull; visited++)
		{
			Assert.True(MuiClassServiceLeaseCodec.TryRead(ref platform, current, out var lease));
			if (lease.Boopsi == cls) return current;
			current = lease.Next;
		}
		Assert.Fail("Expected owned class lease.");
		return APTR.Null;
	}

	private static void AssertParentChildren(ref TransactionPlatform platform, APTR parent, uint count)
	{
		Assert.True(MuiCustomClassCodec.TryRead(ref platform, parent, out var custom));
		var lease = FindLease(ref platform, custom.Class);
		Assert.True(MuiClassServiceLeaseCodec.TryRead(ref platform, lease, out var value));
		Assert.Equal(count, value.ChildCount);
	}

	private struct TransactionPlatform : IMuiClassServicePlatform, IMuiBoopsiObjectLifetimeCapability
	{
		internal MuiHeadlessTestPlatform Fixture;
		internal int AllocationCalls;
		internal int AllocationCallbackNumber;
		internal int FailAllocation;
		internal uint RejectAllocatedRecordSize;
		internal uint ArmAfterAllocatedRecordWriteSize;
		internal APTR ArmAfterRecordRead;
		internal int SkipCompleteRecordReads;
		internal APTR CallbackClass;
		internal byte NewObjectCallback;
		internal bool DisposeObjectCallback;
		internal APTR NestedObject;
		internal APTR NestedClass;
		internal bool CallbackDeleteSucceeded;
		internal bool CloseCallback;
		internal APTR OpenCallbackName;
		internal APTR OpenCallbackResult;
		internal bool LoadCallbackDuringResolve;
		internal bool LoadCallbackSaturates;
		internal APTR CloseCallbackClass;
		internal APTR CloseCallbackName;
		internal APTR CloseCallbackResult;
		internal uint CloseCallbackReferences;
		internal bool CloseCallbackFreeSucceeded;
		internal APTR RejectAddress;
		internal uint RejectBytes;
		internal bool RejectionArmed;
		internal bool MappingRejected;
		internal bool RefuseNativeFree;
		internal uint NativeFreeCalls;
		internal uint NativeNewObjectCalls;
		internal uint NativeDisposeCalls;
		private int _skipAdmissions;
		private APTR _observedWriteAddress;
		private uint _observedWriteBytes;
		private uint _observedReadBytes;

		public bool IsMapped(APTR address, uint bytes)
		{
			if (RejectionArmed && address == RejectAddress && bytes == RejectBytes)
			{
				if (_skipAdmissions != 0) _skipAdmissions--;
				else
				{
					RejectionArmed = false;
					MappingRejected = true;
					return false;
				}
			}
			return Fixture.IsMapped(address, bytes);
		}

		public byte ReadUInt8(APTR address, int offset) => Fixture.ReadUInt8(address, offset);
		public ushort ReadUInt16(APTR address, int offset) => Fixture.ReadUInt16(address, offset);
		public uint ReadUInt32(APTR address, int offset)
		{
			var value = Fixture.ReadUInt32(address, offset);
			if (ArmAfterRecordRead.IsNotNull && address.Raw >= ArmAfterRecordRead.Raw &&
				address.Raw - ArmAfterRecordRead.Raw < MuiClassServiceLeaseRecord.Size)
			{
				_observedReadBytes += sizeof(uint);
				if (_observedReadBytes == MuiClassServiceLeaseRecord.Size)
				{
					_observedReadBytes = 0;
					if (SkipCompleteRecordReads != 0) SkipCompleteRecordReads--;
					else
					{
						ArmAfterRecordRead = APTR.Null;
						RejectionArmed = true;
					}
				}
			}
			return value;
		}

		public void WriteUInt8(APTR address, int offset, byte value) => Fixture.WriteUInt8(address, offset, value);
		public void WriteUInt16(APTR address, int offset, ushort value) => Fixture.WriteUInt16(address, offset, value);
		public void WriteUInt32(APTR address, int offset, uint value)
		{
			Fixture.WriteUInt32(address, offset, value);
			if (_observedWriteAddress.IsNotNull && address.Raw >= _observedWriteAddress.Raw &&
				address.Raw - _observedWriteAddress.Raw < ArmAfterAllocatedRecordWriteSize)
			{
				_observedWriteBytes += sizeof(uint);
				if (_observedWriteBytes == ArmAfterAllocatedRecordWriteSize)
				{
					_observedWriteAddress = APTR.Null;
					ArmAfterAllocatedRecordWriteSize = 0;
					RejectionArmed = true;
				}
			}
		}

		public void Clear(APTR address, uint bytes) => Fixture.Clear(address, bytes);
		public void Copy(APTR source, APTR destination, uint bytes) => Fixture.Copy(source, destination, bytes);
		public APTR Allocate(uint bytes, uint flags)
		{
			AllocationCalls++;
			if (AllocationCalls == FailAllocation) return APTR.Null;
			var allocation = Fixture.Allocate(bytes, flags);
			if (AllocationCallbackNumber == AllocationCalls)
			{
				AllocationCallbackNumber = 0;
				InvokeLoadCallback();
			}
			if (allocation.IsNotNull && RejectAllocatedRecordSize == bytes)
			{
				RejectAllocatedRecordSize = 0;
				RejectAddress = allocation;
				RejectBytes = bytes;
				_skipAdmissions = 1; // admit allocation; reject its first codec write
				RejectionArmed = true;
			}
			if (allocation.IsNotNull && ArmAfterAllocatedRecordWriteSize == bytes)
				_observedWriteAddress = allocation;
			return allocation;
		}

		public void Free(APTR address, uint bytes) => Fixture.Free(address, bytes);
		public APTR MakeClass(APTR classId, APTR superClass, ushort size, APTR dispatcher) =>
			Fixture.MakeClass(classId, superClass, size, dispatcher);
		public bool AddClass(APTR cls) => Fixture.AddClass(cls);
		public bool RemoveClass(APTR cls) => Fixture.RemoveClass(cls);
		public bool FreeClass(APTR cls) => Fixture.FreeClass(cls);
		public APTR OpenLibrary(APTR name, ushort version)
		{
			var result = Fixture.OpenLibrary(name, version);
			if (!LoadCallbackDuringResolve && AllocationCallbackNumber == 0) InvokeLoadCallback();
			return result;
		}
		private void InvokeLoadCallback()
		{
			var callback = OpenCallbackName;
			OpenCallbackName = APTR.Null;
			if (callback.IsNotNull)
			{
				OpenCallbackResult = MuiClassServiceCore.GetClass(ref this, Service, callback);
				if (LoadCallbackSaturates)
				{
					var lease = FindLease(ref this, OpenCallbackResult);
					Assert.True(MuiClassServiceLeaseCodec.TryRead(ref this, lease, out var value));
					value.RefCount = uint.MaxValue;
					Assert.True(MuiClassServiceLeaseCodec.Write(ref this, lease, value));
				}
			}
		}
		public void CloseLibrary(APTR library)
		{
			Fixture.CloseLibrary(library);
			if (!CloseCallback) return;
			CloseCallback = false;
			CloseCallbackReferences = MuiClassServiceCore.ReferenceCount(ref this,
				Service, CloseCallbackClass);
			CloseCallbackFreeSucceeded = MuiClassServiceCore.FreeClass(ref this,
				Service, CloseCallbackClass);
			if (CloseCallbackName.IsNotNull)
				CloseCallbackResult = MuiClassServiceCore.GetClass(ref this, Service, CloseCallbackName);
		}
		public APTR MakeCustomClass(APTR super, ushort size, APTR dispatcher, APTR library) =>
			Fixture.MakeCustomClass(super, size, dispatcher, library);
		public bool FreeCustomClass(APTR cls)
		{
			NativeFreeCalls++;
			return !RefuseNativeFree && Fixture.FreeCustomClass(cls);
		}
		public APTR ResolveExternalClass(APTR library, APTR classId)
		{
			var result = Fixture.ResolveExternalClass(library, classId);
			if (LoadCallbackDuringResolve) InvokeLoadCallback();
			return result;
		}
		public APTR NewObject(APTR cls, APTR tags)
		{
			NativeNewObjectCalls++;
			var callback = NewObjectCallback;
			if (callback != 0)
			{
				NewObjectCallback = 0;
				CallbackDeleteSucceeded = MuiClassServiceCore.DeleteCustomClass(ref this,
					Service, CallbackClass);
				NestedObject = MuiClassServiceCore.CreateCustomObject(ref this,
					Service, CallbackClass, APTR.Null);
				NestedClass = CreateChild(ref this, CallbackClass);
				if (callback == 2) return APTR.Null;
			}
			return Fixture.NewObject(cls, tags);
		}
		public void DisposeObject(APTR obj)
		{
			NativeDisposeCalls++;
			if (DisposeObjectCallback)
			{
				DisposeObjectCallback = false;
				CallbackDeleteSucceeded = MuiClassServiceCore.DeleteCustomClass(ref this,
					Service, CallbackClass);
				NestedObject = MuiClassServiceCore.CreateCustomObject(ref this,
					Service, CallbackClass, APTR.Null);
				NestedClass = CreateChild(ref this, CallbackClass);
			}
			Fixture.DisposeObject(obj);
		}
	}
}
