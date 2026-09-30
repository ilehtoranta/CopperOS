/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster.NativeRoot;

[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativePublicObjectReleaseFixture
{
	internal APTR Registry;
	internal APTR RootBinding;
	internal APTR ChildBinding;
	internal APTR GrandchildBinding;
	internal APTR RootSidecar;
	internal APTR ChildSidecar;
	internal APTR GrandchildSidecar;
	internal APTR OwnerRoot;
	internal APTR RootObject;
	internal APTR ChildObject;
	internal APTR GrandchildObject;
	internal APTR Class;
}

public static class MuiNativePublicObjectOwnershipRoots
{
	// Exercise the real snapshot reconciliation routine on guest memory. Each
	// graph is expressed through named binding/sidecar records; fixed byte
	// positioning remains inside the production codecs.
	public static uint RetainedDescendantReleaseRoot()
	{
		var platform = new MuiNativeHeadlessPlatform();
		platform.Reset();

		var retainedChild = RetainedChildFixture();
		if (!WriteFixture(ref platform, retainedChild, 2, 1) ||
			!MuiNativePublicObjectCore.ReconcilePendingReleaseSnapshots(ref platform,
				retainedChild.OwnerRoot, retainedChild.Registry,
				retainedChild.RootObject)) return 1;
		if (!MuiNativePublicObjectCore.TryFindBinding(ref platform,
			retainedChild.Registry, retainedChild.OwnerRoot,
			retainedChild.ChildObject, out var child) ||
			child.DisposeState != MuiNativePublicObjectBinding.StateLive ||
			child.Parent.IsNotNull) return 2;
		if (!MuiNativePublicObjectCore.TryFindBinding(ref platform,
			retainedChild.Registry, retainedChild.OwnerRoot,
			retainedChild.GrandchildObject, out var grandchild) ||
			grandchild.DisposeState != MuiNativePublicObjectBinding.StateLive ||
			grandchild.Parent != retainedChild.ChildObject ||
			!SnapshotsCleared(ref platform, child.Sidecar) ||
			!SnapshotsCleared(ref platform, grandchild.Sidecar)) return 3;

		var retainedGrandchild = RetainedGrandchildFixture();
		if (!WriteFixture(ref platform, retainedGrandchild, 1, 2) ||
			!MuiNativePublicObjectCore.ReconcilePendingReleaseSnapshots(ref platform,
				retainedGrandchild.OwnerRoot, retainedGrandchild.Registry,
				retainedGrandchild.RootObject)) return 4;
		if (!MuiNativePublicObjectCore.TryFindBinding(ref platform,
			retainedGrandchild.Registry, retainedGrandchild.OwnerRoot,
			retainedGrandchild.ChildObject, out child) ||
			child.DisposeState !=
				MuiNativePublicObjectBinding.StateNativeDisposed) return 5;
		if (!MuiNativePublicObjectCore.TryFindBinding(ref platform,
			retainedGrandchild.Registry, retainedGrandchild.OwnerRoot,
			retainedGrandchild.GrandchildObject, out grandchild) ||
			grandchild.DisposeState != MuiNativePublicObjectBinding.StateLive ||
			grandchild.Parent.IsNotNull ||
			!SnapshotsCleared(ref platform, child.Sidecar) ||
			!SnapshotsCleared(ref platform, grandchild.Sidecar)) return 6;

		return 42;
	}

	private static MuiNativePublicObjectReleaseFixture RetainedChildFixture() =>
		new()
		{
			Registry = APTR.FromPointer(0x00044000),
			RootBinding = APTR.FromPointer(0x00044020),
			ChildBinding = APTR.FromPointer(0x00044060),
			GrandchildBinding = APTR.FromPointer(0x000440A0),
			RootSidecar = APTR.FromPointer(0x00044100),
			ChildSidecar = APTR.FromPointer(0x00044180),
			GrandchildSidecar = APTR.FromPointer(0x00044200),
			OwnerRoot = APTR.FromPointer(0x00044300),
			RootObject = APTR.FromPointer(0x00044400),
			ChildObject = APTR.FromPointer(0x00044440),
			GrandchildObject = APTR.FromPointer(0x00044480),
			Class = APTR.FromPointer(0x00044500),
		};

	private static MuiNativePublicObjectReleaseFixture RetainedGrandchildFixture() =>
		new()
		{
			Registry = APTR.FromPointer(0x00044600),
			RootBinding = APTR.FromPointer(0x00044620),
			ChildBinding = APTR.FromPointer(0x00044660),
			GrandchildBinding = APTR.FromPointer(0x000446A0),
			RootSidecar = APTR.FromPointer(0x00044700),
			ChildSidecar = APTR.FromPointer(0x00044780),
			GrandchildSidecar = APTR.FromPointer(0x00044800),
			OwnerRoot = APTR.FromPointer(0x00044900),
			RootObject = APTR.FromPointer(0x00044A00),
			ChildObject = APTR.FromPointer(0x00044A40),
			GrandchildObject = APTR.FromPointer(0x00044A80),
			Class = APTR.FromPointer(0x00044B00),
		};

	private static bool WriteFixture(ref MuiNativeHeadlessPlatform platform,
		MuiNativePublicObjectReleaseFixture fixture, uint childRetainCount,
		uint grandchildRetainCount)
	{
		var registry = new MuiNativePublicObjectRegistryRecord
		{
			Signature = MuiNativePublicObjectRegistryRecord.Magic,
			Revision = MuiNativePublicObjectRegistryRecord.Version,
			Head = fixture.RootBinding,
		};
		var root = Binding(fixture, fixture.RootObject,
			fixture.RootSidecar, fixture.ChildBinding, APTR.Null,
			MuiNativePublicObjectBinding.StateNativeDisposed);
		var child = Binding(fixture, fixture.ChildObject,
			fixture.ChildSidecar, fixture.GrandchildBinding, fixture.RootObject,
			MuiNativePublicObjectBinding.StateLive);
		var grandchild = Binding(fixture, fixture.GrandchildObject,
			fixture.GrandchildSidecar, APTR.Null,
			fixture.ChildObject, MuiNativePublicObjectBinding.StateLive);
		var rootState = Sidecar(fixture, fixture.RootObject, APTR.Null,
			MuiNativeMuiObjectRecord.StateNativeDisposed, 0, 0, 0, APTR.Null);
		var childState = Sidecar(fixture, fixture.ChildObject, fixture.RootObject,
			MuiNativeMuiObjectRecord.StateLive,
			MuiNativeMuiObjectRecord.ObjectParentGroupChild,
			childRetainCount, 1, fixture.RootObject);
		var grandchildState = Sidecar(fixture, fixture.GrandchildObject,
			fixture.ChildObject, MuiNativeMuiObjectRecord.StateLive,
			MuiNativeMuiObjectRecord.ObjectParentGroupChild,
			grandchildRetainCount, 2, fixture.ChildObject);

		return MuiNativePublicObjectRegistryCodec.Write(ref platform,
			fixture.Registry, registry) &&
			MuiNativePublicObjectBindingCodec.Write(ref platform,
				fixture.RootBinding, root) &&
			MuiNativePublicObjectBindingCodec.Write(ref platform,
				fixture.ChildBinding, child) &&
			MuiNativePublicObjectBindingCodec.Write(ref platform,
				fixture.GrandchildBinding, grandchild) &&
			MuiNativeMuiObjectCodec.Write(ref platform, fixture.RootSidecar,
				rootState) &&
			MuiNativeMuiObjectCodec.Write(ref platform, fixture.ChildSidecar,
				childState) &&
			MuiNativeMuiObjectCodec.Write(ref platform,
				fixture.GrandchildSidecar, grandchildState);
	}

	private static MuiNativePublicObjectBinding Binding(
		MuiNativePublicObjectReleaseFixture fixture, APTR obj,
		APTR sidecar, APTR next, APTR parent, uint disposeState) => new()
	{
		Signature = MuiNativePublicObjectBinding.Magic,
		Next = next,
		Object = obj,
		Class = fixture.Class,
		OwnerRoot = fixture.OwnerRoot,
		Parent = parent,
		Sidecar = sidecar,
		DisposeState = disposeState,
	};

	private static MuiNativeMuiObjectRecord Sidecar(
		MuiNativePublicObjectReleaseFixture fixture, APTR obj, APTR parent,
		uint lifecycleState, uint flags, uint pendingCount, uint pendingDepth,
		APTR pendingParent) => new()
	{
		Signature = MuiNativeMuiObjectRecord.Magic,
		Revision = MuiNativeMuiObjectRecord.Version,
		Object = obj,
		Class = fixture.Class,
		OwnerRoot = fixture.OwnerRoot,
		Parent = parent,
		Flags = flags,
		LifecycleState = lifecycleState,
		PendingReleaseCount = pendingCount,
		PendingReleaseDepth = pendingDepth,
		PendingReleaseParent = pendingParent,
	};

	private static bool SnapshotsCleared(ref MuiNativeHeadlessPlatform platform,
		APTR address) => MuiNativeMuiObjectCodec.TryRead(ref platform, address,
			out var sidecar) && sidecar.PendingReleaseCount == 0 &&
			sidecar.PendingReleaseDepth == 0 && sidecar.PendingReleaseParent.IsNull;
}
