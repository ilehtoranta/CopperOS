using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeApplicationRefreshTests
{
	[Fact]
	public void CheckRefreshSnapshotSkipsRemovedSiblingAfterRegistryMutation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x40000, 0x8000,
			APTR.FromPointer(0x1000));
		var publicObjects = APTR.FromPointer(0x1100);
		var ownerRoot = APTR.FromPointer(0x1200);
		var application = APTR.FromPointer(0x1800);
		var unrelatedParent = APTR.FromPointer(0x1840);
		var firstObject = APTR.FromPointer(0x1900);
		var secondObject = APTR.FromPointer(0x1940);
		var unrelatedObject = APTR.FromPointer(0x1980);
		var classPointer = APTR.FromPointer(0x1A00);
		var firstSidecar = APTR.FromPointer(0x1B00);
		var secondSidecar = APTR.FromPointer(0x1C00);
		var unrelatedSidecar = APTR.FromPointer(0x1D00);
		var firstBindingAddress = APTR.FromPointer(0x2000);
		var secondBindingAddress = APTR.FromPointer(0x2040);
		var unrelatedBindingAddress = APTR.FromPointer(0x2080);

		WriteLiveObject(ref platform, firstSidecar, firstObject, classPointer,
			ownerRoot);
		WriteLiveObject(ref platform, secondSidecar, secondObject, classPointer,
			ownerRoot);
		WriteLiveObject(ref platform, unrelatedSidecar, unrelatedObject,
			classPointer, ownerRoot);
		var firstBinding = LiveBinding(firstObject, classPointer, ownerRoot,
			application, firstSidecar, secondBindingAddress);
		var secondBinding = LiveBinding(secondObject, classPointer, ownerRoot,
			application, secondSidecar, unrelatedBindingAddress);
		var unrelatedBinding = LiveBinding(unrelatedObject, classPointer,
			ownerRoot, unrelatedParent, unrelatedSidecar, APTR.Null);
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref platform,
			firstBindingAddress, firstBinding));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref platform,
			secondBindingAddress, secondBinding));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref platform,
			unrelatedBindingAddress, unrelatedBinding));
		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref platform,
			publicObjects, new MuiNativePublicObjectRegistryRecord
			{
				Signature = MuiNativePublicObjectRegistryRecord.Magic,
				Revision = MuiNativePublicObjectRegistryRecord.Version,
				Head = firstBindingAddress,
				Mutation = 7,
			}));

		Assert.True(MuiNativeApplicationRefreshCore.TryBuildChildSnapshot(
			ref platform, publicObjects, ownerRoot, application,
			out var snapshotPlan));
		Assert.Equal(2u, snapshotPlan.Count);
		Assert.Equal(7u, snapshotPlan.RegistryMutation);
		Assert.Equal(2 * MuiNativeApplicationChildSnapshotRecord.Size,
			snapshotPlan.ByteSize);
		Assert.True(MuiGuestStructCursor.TryCreate(ref platform,
			snapshotPlan.Records, snapshotPlan.ByteSize, out var cursor));
		Assert.True(MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiNativeApplicationChildSnapshotRecord.Size, out var firstAddress));
		Assert.True(MuiNativeApplicationChildSnapshotCodec.TryRead(ref platform,
			firstAddress, out var firstSnapshot));
		Assert.True(MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiNativeApplicationChildSnapshotRecord.Size, out var secondAddress));
		Assert.True(MuiNativeApplicationChildSnapshotCodec.TryRead(ref platform,
			secondAddress, out var secondSnapshot));
		Assert.True(MuiGuestStructCursor.IsComplete(cursor));
		Assert.Equal(firstObject, firstSnapshot.Object);
		Assert.Equal(firstSidecar, firstSnapshot.Sidecar);
		Assert.Equal(classPointer, firstSnapshot.Class);
		Assert.Equal(application, firstSnapshot.Parent);
		Assert.Equal(secondObject, secondSnapshot.Object);
		Assert.Equal(secondSidecar, secondSnapshot.Sidecar);

		// Simulate a draw callback that removes the later child and rewrites the
		// live predecessor. The production revalidation helper consumes the
		// independent snapshot, never the predecessor's potentially stale Next.
		firstBinding.Next = APTR.Null;
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref platform,
			firstBindingAddress, firstBinding));
		var registry = new MuiNativePublicObjectRegistryRecord
		{
			Signature = MuiNativePublicObjectRegistryRecord.Magic,
			Revision = MuiNativePublicObjectRegistryRecord.Version,
			Head = firstBindingAddress,
			Mutation = 8,
		};
		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref platform,
			publicObjects, registry));
		Assert.True(MuiNativeApplicationChildSnapshotCodec.TryRead(ref platform,
			secondAddress, out var afterMutation));
		Assert.Equal(secondObject, afterMutation.Object);
		Assert.Equal(secondSidecar, afterMutation.Sidecar);
		var observedMutation = snapshotPlan.RegistryMutation;
		Assert.True(MuiNativeApplicationRefreshCore.TryResolveSnapshotEntry(
			ref platform, publicObjects, ownerRoot, application, afterMutation,
			ref observedMutation, out _, out var skip));
		Assert.True(skip);
		Assert.Equal(8u, observedMutation);

		MuiNativeApplicationRefreshCore.ReleaseChildSnapshot(ref platform,
			ref snapshotPlan);
		Assert.True(snapshotPlan.Records.IsNull);
		Assert.Equal(1u, platform.AllocationCount);
		Assert.Equal(1u, platform.FreeCount);
	}

	[Fact]
	public void CheckRefreshSnapshotSkipsChildReparentedByCallback()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x8000,
			APTR.FromPointer(0x1000));
		var publicObjects = APTR.FromPointer(0x1100);
		var ownerRoot = APTR.FromPointer(0x1200);
		var application = APTR.FromPointer(0x1800);
		var replacementParent = APTR.FromPointer(0x1840);
		var child = APTR.FromPointer(0x1900);
		var classPointer = APTR.FromPointer(0x1A00);
		var sidecar = APTR.FromPointer(0x1B00);
		var bindingAddress = APTR.FromPointer(0x2000);
		WriteLiveObject(ref platform, sidecar, child, classPointer, ownerRoot);
		var binding = LiveBinding(child, classPointer, ownerRoot, application,
			sidecar, APTR.Null);
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref platform,
			bindingAddress, binding));
		var registry = new MuiNativePublicObjectRegistryRecord
		{
			Signature = MuiNativePublicObjectRegistryRecord.Magic,
			Revision = MuiNativePublicObjectRegistryRecord.Version,
			Head = bindingAddress,
			Mutation = 3,
		};
		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref platform,
			publicObjects, registry));
		Assert.True(MuiNativeApplicationRefreshCore.TryBuildChildSnapshot(
			ref platform, publicObjects, ownerRoot, application,
			out var snapshotPlan));
		Assert.True(MuiGuestStructCursor.TryCreate(ref platform,
			snapshotPlan.Records, snapshotPlan.ByteSize, out var cursor));
		Assert.True(MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiNativeApplicationChildSnapshotRecord.Size, out var snapshotAddress));
		Assert.True(MuiNativeApplicationChildSnapshotCodec.TryRead(ref platform,
			snapshotAddress, out var snapshot));

		binding.Parent = replacementParent;
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref platform,
			bindingAddress, binding));
		registry.Mutation++;
		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref platform,
			publicObjects, registry));
		var observedMutation = snapshotPlan.RegistryMutation;
		Assert.True(MuiNativeApplicationRefreshCore.TryResolveSnapshotEntry(
			ref platform, publicObjects, ownerRoot, application, snapshot,
			ref observedMutation, out var liveBinding, out var skip));
		Assert.True(skip);
		Assert.Equal(replacementParent, liveBinding.Parent);
		Assert.Equal(registry.Mutation, observedMutation);

		MuiNativeApplicationRefreshCore.ReleaseChildSnapshot(ref platform,
			ref snapshotPlan);
		Assert.Equal(1u, platform.AllocationCount);
		Assert.Equal(1u, platform.FreeCount);
	}

	[Fact]
	public void CheckRefreshSnapshotFailsClosedForMalformedStillLinkedChild()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x8000,
			APTR.FromPointer(0x1000));
		var publicObjects = APTR.FromPointer(0x1100);
		var ownerRoot = APTR.FromPointer(0x1200);
		var application = APTR.FromPointer(0x1800);
		var child = APTR.FromPointer(0x1900);
		var classPointer = APTR.FromPointer(0x1A00);
		var sidecar = APTR.FromPointer(0x1B00);
		var bindingAddress = APTR.FromPointer(0x2000);
		WriteLiveObject(ref platform, sidecar, child, classPointer, ownerRoot);
		var binding = LiveBinding(child, classPointer, ownerRoot, application,
			sidecar, APTR.Null);
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref platform,
			bindingAddress, binding));
		var registry = new MuiNativePublicObjectRegistryRecord
		{
			Signature = MuiNativePublicObjectRegistryRecord.Magic,
			Revision = MuiNativePublicObjectRegistryRecord.Version,
			Head = bindingAddress,
			Mutation = 11,
		};
		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref platform,
			publicObjects, registry));
		Assert.True(MuiNativeApplicationRefreshCore.TryBuildChildSnapshot(
			ref platform, publicObjects, ownerRoot, application,
			out var snapshotPlan));
		Assert.True(MuiGuestStructCursor.TryCreate(ref platform,
			snapshotPlan.Records, snapshotPlan.ByteSize, out var cursor));
		Assert.True(MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiNativeApplicationChildSnapshotRecord.Size, out var snapshotAddress));
		Assert.True(MuiNativeApplicationChildSnapshotCodec.TryRead(ref platform,
			snapshotAddress, out var snapshot));

		var sidecarRecord = default(MuiNativeMuiObjectRecord);
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref platform, sidecar,
			out sidecarRecord));
		sidecarRecord.Class = APTR.FromPointer(0x1A40);
		Assert.True(MuiNativeMuiObjectCodec.Write(ref platform, sidecar,
			sidecarRecord));
		registry.Mutation++;
		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref platform,
			publicObjects, registry));
		var observedMutation = snapshotPlan.RegistryMutation;
		Assert.False(MuiNativeApplicationRefreshCore.TryResolveSnapshotEntry(
			ref platform, publicObjects, ownerRoot, application, snapshot,
			ref observedMutation, out _, out var skip));
		Assert.False(skip);
		Assert.Equal(registry.Mutation, observedMutation);

		MuiNativeApplicationRefreshCore.ReleaseChildSnapshot(ref platform,
			ref snapshotPlan);
		Assert.Equal(1u, platform.AllocationCount);
		Assert.Equal(1u, platform.FreeCount);
	}

	[Fact]
	public void CheckRefreshSnapshotRejectsRegistryMutationDuringAllocation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x8000,
			APTR.FromPointer(0x1000));
		var publicObjects = APTR.FromPointer(0x1100);
		var ownerRoot = APTR.FromPointer(0x1200);
		var application = APTR.FromPointer(0x1800);
		var child = APTR.FromPointer(0x1900);
		var classPointer = APTR.FromPointer(0x1A00);
		var sidecar = APTR.FromPointer(0x1B00);
		var bindingAddress = APTR.FromPointer(0x2000);
		WriteLiveObject(ref platform, sidecar, child, classPointer, ownerRoot);
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref platform,
			bindingAddress, LiveBinding(child, classPointer, ownerRoot,
				application, sidecar, APTR.Null)));
		var registry = new MuiNativePublicObjectRegistryRecord
		{
			Signature = MuiNativePublicObjectRegistryRecord.Magic,
			Revision = MuiNativePublicObjectRegistryRecord.Version,
			Head = bindingAddress,
			Mutation = 3,
		};
		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref platform,
			publicObjects, registry));
		platform.AllocationAdmission = (_, _) =>
		{
			registry.Mutation++;
			return MuiNativePublicObjectRegistryCodec.Write(ref platform,
				publicObjects, registry);
		};

		Assert.False(MuiNativeApplicationRefreshCore.TryBuildChildSnapshot(
			ref platform, publicObjects, ownerRoot, application, out var plan));
		Assert.True(plan.Records.IsNull);
		Assert.Equal(1u, platform.AllocationCount);
		Assert.Equal(1u, platform.FreeCount);
	}

	private static void WriteLiveObject(ref MuiHeadlessTestPlatform platform,
		APTR sidecarAddress, APTR obj, APTR classPointer, APTR ownerRoot)
	{
		var sidecar = new MuiNativeMuiObjectRecord
		{
			Signature = MuiNativeMuiObjectRecord.Magic,
			Revision = MuiNativeMuiObjectRecord.Version,
			Object = obj,
			Class = classPointer,
			OwnerRoot = ownerRoot,
			Flags = MuiNativeMuiObjectRecord.ObjectInitialized,
			LifecycleState = MuiNativeMuiObjectRecord.StateLive,
		};
		Assert.True(MuiNativeMuiObjectCodec.Write(ref platform, sidecarAddress,
			sidecar));
	}

	private static MuiNativePublicObjectBinding LiveBinding(APTR obj,
		APTR classPointer, APTR ownerRoot, APTR parent, APTR sidecar, APTR next)
	{
		return new MuiNativePublicObjectBinding
		{
			Signature = MuiNativePublicObjectBinding.Magic,
			Next = next,
			Object = obj,
			Class = classPointer,
			OwnerRoot = ownerRoot,
			Parent = parent,
			Sidecar = sidecar,
			DisposeState = MuiNativePublicObjectBinding.StateLive,
		};
	}
}
