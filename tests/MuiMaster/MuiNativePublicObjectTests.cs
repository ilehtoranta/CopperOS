/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativePublicObjectTests
{
	[Fact]
	public void IDCMPProjectionPreservesOtherClassesAndRequiredMUIEvents()
	{
		var initialMask = IDCMPFlags.ActiveWindow | IDCMPFlags.RawKey;
		var window = new Window { IDCMPFlags = initialMask };
		var requested = (uint)IDCMPFlags.MouseButtons;
		var rejected = (uint)IDCMPFlags.RawKey |
			(uint)IDCMPFlags.ChangeWindow | (uint)IDCMPFlags.MouseMove |
			(uint)IDCMPFlags.MenuPick | (uint)IDCMPFlags.MenuHelp;

		Assert.True(MuiNativePublicObjectCore.ProjectIDCMPMask(ref window,
			requested, rejected));

		var expected = IDCMPFlags.ActiveWindow | IDCMPFlags.MouseButtons |
			IDCMPFlags.ChangeWindow | IDCMPFlags.MouseMove |
			IDCMPFlags.MenuPick | IDCMPFlags.MenuHelp;
		Assert.Equal(expected, window.IDCMPFlags);
		Assert.False(MuiNativePublicObjectCore.ProjectIDCMPMask(ref window,
			requested, rejected));
	}

	[Fact]
	public void IDCMPProjectionAddsNewRequestsWithoutDroppingExistingClasses()
	{
		var initialMask = IDCMPFlags.GadgetUp | IDCMPFlags.MenuPick;
		var window = new Window { IDCMPFlags = initialMask };

		Assert.True(MuiNativePublicObjectCore.ProjectIDCMPMask(ref window,
			(uint)IDCMPFlags.RawKey, 0));

		var expected = initialMask | IDCMPFlags.RawKey |
			IDCMPFlags.ChangeWindow | IDCMPFlags.MouseMove |
			IDCMPFlags.MenuHelp;
		Assert.Equal(expected, window.IDCMPFlags);
	}

	[Fact]
	public void RetainedNativeChildSurvivesParentTeardownWithItsSubtree()
	{
		var memory = CreateReleaseTree(childRetainCount: 2,
			grandchildRetainCount: 1, out var registry, out var ownerRoot,
			out var root, out var child, out var grandchild);

		Assert.True(MuiNativePublicObjectCore.ReconcilePendingReleaseSnapshots(
			ref memory, ownerRoot, registry, root));
		Assert.True(MuiNativePublicObjectCore.TryFindBinding(ref memory,
			registry, ownerRoot, child, out var childBinding));
		Assert.Equal(MuiNativePublicObjectBinding.StateLive,
			childBinding.DisposeState);
		Assert.True(childBinding.Parent.IsNull);
		Assert.True(MuiNativePublicObjectCore.TryFindBinding(ref memory,
			registry, ownerRoot, grandchild, out var grandchildBinding));
		Assert.Equal(MuiNativePublicObjectBinding.StateLive,
			grandchildBinding.DisposeState);
		Assert.Equal(child, grandchildBinding.Parent);
	}

	[Fact]
	public void UnretainedNativeChildrenAreRetiredButRetainedGrandchildIsDetached()
	{
		var memory = CreateReleaseTree(childRetainCount: 1,
			grandchildRetainCount: 2, out var registry, out var ownerRoot,
			out var root, out var child, out var grandchild);

		Assert.True(MuiNativePublicObjectCore.ReconcilePendingReleaseSnapshots(
			ref memory, ownerRoot, registry, root));
		Assert.True(MuiNativePublicObjectCore.TryFindBinding(ref memory,
			registry, ownerRoot, child, out var childBinding));
		Assert.Equal(MuiNativePublicObjectBinding.StateNativeDisposed,
			childBinding.DisposeState);
		Assert.True(MuiNativePublicObjectCore.TryFindBinding(ref memory,
			registry, ownerRoot, grandchild, out var grandchildBinding));
		Assert.Equal(MuiNativePublicObjectBinding.StateLive,
			grandchildBinding.DisposeState);
		Assert.True(grandchildBinding.Parent.IsNull);
	}

	[Fact]
	public void ReconciliationRejectsMalformedPendingReleaseDepth()
	{
		var memory = CreateReleaseTree(childRetainCount: 2,
			grandchildRetainCount: 1, out var registry, out var ownerRoot,
			out var root, out var child, out _);
		Assert.True(MuiNativePublicObjectCore.TryFindBinding(ref memory,
			registry, ownerRoot, child, out var binding));
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref memory, binding.Sidecar,
			out var sidecar));
		sidecar.PendingReleaseDepth = MuiHeadlessLayout.MaximumTraversal + 1;
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, binding.Sidecar,
			sidecar));

		Assert.False(MuiNativePublicObjectCore.ReconcilePendingReleaseSnapshots(
			ref memory, ownerRoot, registry, root));
	}

	private static MuiHeadlessTestPlatform CreateReleaseTree(
		uint childRetainCount, uint grandchildRetainCount, out APTR registry,
		out APTR ownerRoot, out APTR root, out APTR child, out APTR grandchild)
	{
		registry = APTR.FromPointer(0x1000);
		var rootBindingAddress = APTR.FromPointer(0x1100);
		var childBindingAddress = APTR.FromPointer(0x1140);
		var grandchildBindingAddress = APTR.FromPointer(0x1180);
		var rootSidecarAddress = APTR.FromPointer(0x3000);
		var childSidecarAddress = APTR.FromPointer(0x3100);
		var grandchildSidecarAddress = APTR.FromPointer(0x3200);
		ownerRoot = APTR.FromPointer(0x2300);
		root = APTR.FromPointer(0x2000);
		child = APTR.FromPointer(0x2100);
		grandchild = APTR.FromPointer(0x2200);
		var objectClass = APTR.FromPointer(0x2400);
		var memory = new MuiHeadlessTestPlatform(registry.Raw, 0x5000, 0,
			registry);
		var registryRecord = new MuiNativePublicObjectRegistryRecord
		{
			Signature = MuiNativePublicObjectRegistryRecord.Magic,
			Revision = MuiNativePublicObjectRegistryRecord.Version,
			Head = rootBindingAddress,
		};
		var rootBinding = new MuiNativePublicObjectBinding
		{
			Signature = MuiNativePublicObjectBinding.Magic,
			Next = childBindingAddress,
			Object = root,
			Class = objectClass,
			OwnerRoot = ownerRoot,
			Sidecar = rootSidecarAddress,
			DisposeState = MuiNativePublicObjectBinding.StateNativeDisposed,
		};
		var childBinding = new MuiNativePublicObjectBinding
		{
			Signature = MuiNativePublicObjectBinding.Magic,
			Next = grandchildBindingAddress,
			Object = child,
			Class = objectClass,
			OwnerRoot = ownerRoot,
			Parent = root,
			Sidecar = childSidecarAddress,
		};
		var grandchildBinding = new MuiNativePublicObjectBinding
		{
			Signature = MuiNativePublicObjectBinding.Magic,
			Object = grandchild,
			Class = objectClass,
			OwnerRoot = ownerRoot,
			Parent = child,
			Sidecar = grandchildSidecarAddress,
		};
		var rootSidecar = new MuiNativeMuiObjectRecord
		{
			Signature = MuiNativeMuiObjectRecord.Magic,
			Revision = MuiNativeMuiObjectRecord.Version,
			Object = root,
			Class = objectClass,
			OwnerRoot = ownerRoot,
			LifecycleState = MuiNativeMuiObjectRecord.StateNativeDisposed,
		};
		var childSidecar = new MuiNativeMuiObjectRecord
		{
			Signature = MuiNativeMuiObjectRecord.Magic,
			Revision = MuiNativeMuiObjectRecord.Version,
			Object = child,
			Class = objectClass,
			OwnerRoot = ownerRoot,
			Parent = root,
			Flags = MuiNativeMuiObjectRecord.ObjectParentGroupChild,
			PendingReleaseCount = childRetainCount,
			PendingReleaseDepth = 1,
			PendingReleaseParent = root,
		};
		var grandchildSidecar = new MuiNativeMuiObjectRecord
		{
			Signature = MuiNativeMuiObjectRecord.Magic,
			Revision = MuiNativeMuiObjectRecord.Version,
			Object = grandchild,
			Class = objectClass,
			OwnerRoot = ownerRoot,
			Parent = child,
			Flags = MuiNativeMuiObjectRecord.ObjectParentGroupChild,
			PendingReleaseCount = grandchildRetainCount,
			PendingReleaseDepth = 2,
			PendingReleaseParent = child,
		};
		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref memory,
			registry, registryRecord));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			rootBindingAddress, rootBinding));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			childBindingAddress, childBinding));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			grandchildBindingAddress, grandchildBinding));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, rootSidecarAddress,
			rootSidecar));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, childSidecarAddress,
			childSidecar));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory,
			grandchildSidecarAddress, grandchildSidecar));
		return memory;
	}
}
