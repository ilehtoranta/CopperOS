using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiConstructedObjectAttachmentTests
{
	[Fact]
	public void BusyOwnerRejectsObjectmapMutationAndWholeStoreCleanup()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, state);
		var name = APTR.FromPointer(0x1100);
		var key = APTR.FromPointer(0x1200);
		platform.WriteCString(name, "Notify.mui");
		platform.WriteCString(key, "owned");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, state));
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, state,
			name, APTR.Null, 8, APTR.FromPointer(0xD000));
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, state, cls, APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, state, cls, APTR.Null);
		Assert.True(MuiStoreCore.ObjectmapSet(ref platform, state, obj, key, child));
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform, owner, out var original));
		var busy = original;
		busy.Flags |= MuiHeadlessObjectCore.ObjectProviderBusy;
		Assert.True(MuiHeadlessObjectCodec.Write(ref platform, owner, busy));
		var allocations = platform.AllocationCount;
		var frees = platform.FreeCount;
		Assert.False(MuiStoreCore.ObjectmapSet(ref platform, state, obj, key, APTR.Null));
		Assert.True(MuiStoreCore.ObjectmapRemoveObject(ref platform, state, obj, key).IsNull);
		MuiStoreCore.ClearAll(ref platform, state, owner);
		Assert.Equal(allocations, platform.AllocationCount);
		Assert.Equal(frees, platform.FreeCount);
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform, owner, out var after));
		Assert.Equal(busy, after);
		Assert.True(MuiHeadlessObjectCodec.Write(ref platform, owner, original));
		Assert.Equal(child, MuiStoreCore.ObjectmapRemoveObject(ref platform, state, obj, key));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, state, child));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj));
	}

	[Fact]
	public void DisposalFlagAdmissionFailurePreservesObjectAndCanRetry()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, state);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, state));
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, state,
			name, APTR.Null, 8, APTR.FromPointer(0xD000));
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, state, cls, APTR.Null);
		var sidecar = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform, sidecar, out var before));
		Assert.True(MuiHeadlessObjectMemoryCodec.TryGetAddress(ref platform, sidecar,
			MuiHeadlessObjectField.Flags, out var flags));
		var admissions = 0;
		platform.MappingAdmission = (address, bytes) =>
			address != flags || bytes != 4 || ++admissions != 3;
		var frees = platform.FreeCount;
		Assert.False(MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj));
		Assert.Equal(3, admissions); // Lookup read, snapshot read, then flag destination.
		Assert.Equal(frees, platform.FreeCount);
		platform.MappingAdmission = null;
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform, sidecar, out var after));
		Assert.Equal(before, after);
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, state, cls));
	}

	[Fact]
	public void DestructorSidecarCleanupLeavesNativeObjectForSuperclassDisposal()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, state);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, state));
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, state,
			name, APTR.Null, 8, APTR.FromPointer(0xD000));
		var live = platform.AllocationCount - platform.FreeCount;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, state, cls, APTR.Null);
		Assert.True(obj.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.DisposeObjectState(ref platform, state, obj, false));
		Assert.True(MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull);
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, cls, out var classValue));
		Assert.Equal(0u, classValue.ObjectCount);
		Assert.Equal(live + 1, platform.AllocationCount - platform.FreeCount);
		var frees = platform.FreeCount;
		Assert.False(MuiHeadlessObjectCore.DisposeObjectState(ref platform, state, obj, false));
		Assert.Equal(frees, platform.FreeCount);
		platform.DisposeObject(obj); // Models the later superclass destructor.
		Assert.Equal(live, platform.AllocationCount - platform.FreeCount);
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, state, cls));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void FactoryInitializationFailureBalancesOwnershipWithOrWithoutConstructorAttachment(bool inConstructor)
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, state);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, state));
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, state,
			name, APTR.Null, 8, APTR.FromPointer(0xD000));
		if (inConstructor) platform.ConstructorAttachmentClass = cls;
		var live = platform.AllocationCount - platform.FreeCount;
		Assert.True(MuiHeadlessObjectCore.CreateObjectA(ref platform, state, cls,
			APTR.FromPointer(0xFFFFFFFC)).IsNull);
		Assert.Equal(1u, platform.ConstructorCalls);
		Assert.Equal(live, platform.AllocationCount - platform.FreeCount);
		Assert.True(MuiHeadlessStateCodec.TryRead(ref platform, state, out var registry));
		Assert.True(registry.Objects.IsNull);
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, cls, out var classValue));
		Assert.Equal(0u, classValue.ObjectCount);
		// A subsequent valid construction must still succeed and dispose cleanly.
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, state, cls, APTR.Null);
		Assert.True(obj.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj));
		Assert.Equal(live, platform.AllocationCount - platform.FreeCount);
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, state, cls));
	}

	[Fact]
	public void FactoryReusesSidecarAttachedInsideConstructorWithoutSecondConstruction()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, state);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, state));
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, state,
			name, APTR.Null, 8, APTR.FromPointer(0xD000));
		platform.ConstructorAttachmentClass = cls;
		var live = platform.AllocationCount - platform.FreeCount;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, state, cls, APTR.Null);
		Assert.True(obj.IsNotNull);
		Assert.Equal(1u, platform.ConstructorCalls);
		var sidecar = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform, sidecar, out var value));
		Assert.True(value.Next.IsNull);
		Assert.Equal(cls, value.Class);
		Assert.True(MuiHeadlessObjectCore.IsObjectInitialized(ref platform, sidecar));
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, cls, out var classValue));
		Assert.Equal(1u, classValue.ObjectCount);
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj));
		Assert.Equal(live, platform.AllocationCount - platform.FreeCount);
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, state, cls));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void FactoryRejectsIncompleteOrDisposingSidecarWithoutChangingOwnership(bool disposing)
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, state);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, state));
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, state,
			name, APTR.Null, 8, APTR.FromPointer(0xD000));
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, state, cls, APTR.Null);
		Assert.True(obj.IsNotNull);
		var sidecar = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform, sidecar, out var original));
		var pending = original;
		if (disposing) pending.Flags |= MuiHeadlessObjectCore.ObjectDisposing;
		else pending.Flags &= ~MuiHeadlessObjectCore.ObjectInitialized;
		Assert.True(MuiHeadlessObjectCodec.Write(ref platform, sidecar, pending));
		var allocations = platform.AllocationCount;
		var frees = platform.FreeCount;
		Assert.True(MuiHeadlessObjectCore.CompleteConstructedObject(ref platform, state, cls, obj, APTR.Null).IsNull);
		Assert.Equal(allocations, platform.AllocationCount);
		Assert.Equal(frees, platform.FreeCount);
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform, sidecar, out var after));
		Assert.Equal(pending, after);
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, cls, out var classValue));
		Assert.Equal(1u, classValue.ObjectCount);
		Assert.True(MuiHeadlessObjectCodec.Write(ref platform, sidecar, original));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, state, cls));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void RejectedPublicationFieldDoesNotLeakObjectOrClassReference(bool count)
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, state);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, state));
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, state,
			name, APTR.Null, 8, APTR.FromPointer(0xD000));
		var obj = platform.NewObject(MuiHeadlessObjectCore.ClassPointer(ref platform, cls), APTR.Null);
		APTR destination;
		if (count) Assert.True(MuiHeadlessClassMemoryCodec.TryGetAddress(ref platform, cls,
			MuiHeadlessClassField.ObjectCount, out destination));
		else Assert.True(MuiHeadlessStateMemoryCodec.TryGetAddress(ref platform, state,
			MuiHeadlessStateField.Objects, out destination));
		var rejected = false;
		platform.MappingAdmission = (address, bytes) =>
		{
			if (address != destination || bytes != 4) return true;
			rejected = true;
			return false;
		};
		var live = platform.AllocationCount - platform.FreeCount;
		Assert.True(MuiHeadlessObjectCore.AttachConstructedObject(ref platform, state, cls, obj, APTR.Null).IsNull);
		Assert.True(rejected);
		platform.MappingAdmission = null;
		Assert.Equal(count ? live - 1 : live, platform.AllocationCount - platform.FreeCount);
		if (!count) platform.DisposeObject(obj); // Ownership could not be established.
		Assert.True(MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull);
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, cls, out var value));
		Assert.Equal(0u, value.ObjectCount);
	}

	[Fact]
	public void SidecarAllocationFailureReleasesObjectWithoutPublishingOrIncrementingClass()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x2000, 0x1800, state);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, state));
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, state,
			name, APTR.Null, 8, APTR.FromPointer(0xD000));
		Assert.True(cls.IsNotNull);
		var obj = platform.NewObject(MuiHeadlessObjectCore.ClassPointer(ref platform, cls), APTR.Null);
		Assert.True(obj.IsNotNull);
		// Exhaust the monotonic host allocator, preserving mapped object storage.
		while (platform.Allocate(16, 0).IsNotNull) { }
		Assert.True(MuiHeadlessStateCodec.TryRead(ref platform, state, out var before));
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, cls, out var beforeClass));
		var allocations = platform.AllocationCount;
		var frees = platform.FreeCount;
		Assert.True(MuiHeadlessObjectCore.AttachConstructedObject(ref platform, state, cls, obj, APTR.Null).IsNull);
		Assert.Equal(allocations, platform.AllocationCount);
		Assert.Equal(frees + 1, platform.FreeCount);
		Assert.True(MuiHeadlessStateCodec.TryRead(ref platform, state, out var after));
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, cls, out var afterClass));
		Assert.Equal(before, after);
		Assert.Equal(beforeClass, afterClass);
		Assert.True(MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull);
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, state, cls));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void InvalidOrSaturatedClassRejectsBeforePublicationAndDisposesTransferredObject(bool saturated)
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, state);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, state));
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, state,
			name, APTR.Null, 8, APTR.FromPointer(0xD000));
		var obj = platform.NewObject(MuiHeadlessObjectCore.ClassPointer(ref platform, cls), APTR.Null);
		Assert.True(obj.IsNotNull);
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, cls, out var originalClass));
		if (saturated)
		{
			var value = originalClass;
			value.ObjectCount = uint.MaxValue;
			Assert.True(MuiHeadlessClassCodec.Write(ref platform, cls, value));
		}
		var allocations = platform.AllocationCount;
		var frees = platform.FreeCount;
		Assert.True(MuiHeadlessStateCodec.TryRead(ref platform, state, out var before));
		Assert.True(MuiHeadlessObjectCore.AttachConstructedObject(ref platform, state,
			saturated ? cls : APTR.Null, obj, APTR.Null).IsNull);
		Assert.Equal(allocations, platform.AllocationCount);
		Assert.Equal(frees + 1, platform.FreeCount);
		Assert.True(MuiHeadlessStateCodec.TryRead(ref platform, state, out var after));
		Assert.Equal(before, after);
		Assert.True(MuiHeadlessClassCodec.Write(ref platform, cls, originalClass));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, state, cls));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void AttachmentRegistersExistingObjectOnceAndRejectsDuplicateWithoutMutation(bool unreadable)
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, state);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, state));
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, state,
			name, APTR.Null, 8, APTR.FromPointer(0xD000));
		Assert.True(cls.IsNotNull);
		var obj = platform.NewObject(MuiHeadlessObjectCore.ClassPointer(ref platform, cls), APTR.Null);
		Assert.True(obj.IsNotNull);
		Assert.Equal(obj, MuiHeadlessObjectCore.AttachConstructedObject(ref platform, state, cls, obj, APTR.Null, out var ownership));
		Assert.Equal(MuiObjectAttachmentOwnership.Transferred, ownership);
		var sidecar = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		Assert.True(sidecar.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.IsObjectInitialized(ref platform, sidecar));
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform, sidecar, out var before));
		var allocations = platform.AllocationCount;
		var frees = platform.FreeCount;
		var refused = false;
		// Factory completion accepts constructor-side initialization without
		// allocating, changing counts, or applying the deliberately invalid tags.
		Assert.Equal(obj, MuiHeadlessObjectCore.CompleteConstructedObject(ref platform,
			state, cls, obj, APTR.FromPointer(0xFFFFFFFC)));
		Assert.True(MuiHeadlessObjectCore.CompleteConstructedObject(ref platform,
			state, APTR.Null, obj, APTR.Null).IsNull);
		if (unreadable) platform.MappingAdmission = (address, bytes) =>
		{
			if (refused || address != sidecar) return true;
			refused = true;
			return false;
		};
		Assert.True(MuiHeadlessObjectCore.AttachConstructedObject(ref platform, state, cls, obj, APTR.Null, out ownership).IsNull);
		Assert.Equal(unreadable ? MuiObjectAttachmentOwnership.Unresolved :
			MuiObjectAttachmentOwnership.AlreadyRegistered, ownership);
		platform.MappingAdmission = null;
		Assert.Equal(unreadable, refused);
		Assert.Equal(allocations, platform.AllocationCount);
		Assert.Equal(frees, platform.FreeCount);
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform, sidecar, out var after));
		Assert.Equal(before, after);
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, cls, out var classValue));
		Assert.Equal(1u, classValue.ObjectCount);
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj));
		Assert.True(MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull);
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, cls, out classValue));
		Assert.Equal(0u, classValue.ObjectCount);
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, state, cls));
	}
}
