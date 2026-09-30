using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiConstructionReceiptTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private static readonly APTR Root = APTR.FromPointer(0x1800);
	private static readonly APTR Message = APTR.FromPointer(0x1300);
	private static readonly APTR Tags = APTR.FromPointer(0x1400);

	[Fact]
	public void ReceiptIsRootedBeforeSuperAndNativePointerPrecedesAttachment()
	{
		var platform = Create(out var cls, out var native);
		var sawSuper = false;
		var sawAttachment = false;
		platform.BeforeSuperMethod = (_, _, _) =>
		{
			var head = Head(ref platform);
			Assert.True(MuiConstructionReceiptCore.TryRead(ref platform, Root, head, out var pending));
			Assert.Equal(MuiConstructionReceiptRecord.Running, pending.Phase);
			Assert.True(pending.NativeObject.IsNull);
			Assert.False(MuiMasterLifecycleCore.Dispose(ref platform, Root));
			sawSuper = true;
		};
		platform.AllocationAdmission = (size, _) =>
		{
			if (size != MuiHeadlessObjectRecord.Size) return true;
			var head = Head(ref platform);
			Assert.True(MuiConstructionReceiptCore.TryRead(ref platform, Root, head, out var pending));
			Assert.Equal(native, pending.NativeObject);
			Assert.Equal(MuiConstructionReceiptRecord.Running, pending.Phase);
			Assert.False(MuiConstructionReceiptCore.TryCommit(ref platform, Root, head));
			Assert.False(MuiConstructionReceiptCore.TryDiscard(ref platform, Root, head));
			Assert.False(MuiConstructionReceiptCore.TryCleanup(ref platform, Root, head));
			sawAttachment = true;
			return true;
		};
		Assert.True(Construct(ref platform, cls, out var receipt, out var result));
		Assert.True(sawSuper && sawAttachment);
		Assert.Equal(native, result.InitializedObject);
		Assert.Equal(receipt, Head(ref platform));
		Assert.False(MuiMasterLifecycleCore.Dispose(ref platform, Root));
		platform.AllocationAdmission = null;
		Assert.True(MuiConstructionReceiptCore.TryCommit(ref platform, Root, receipt));
		Assert.True(Head(ref platform).IsNull);
		Assert.True(MuiMasterLifecycleCore.Dispose(ref platform, Root));
		Assert.Equal(1u, platform.NativeObjectDisposalCount);
	}

	[Fact]
	public void ReceiptAllocationFailurePreventsSuperclassCall()
	{
		var platform = Create(out var cls, out var native);
		platform.AllocationAdmission = (size, _) => size != MuiConstructionReceiptRecord.Size;
		Assert.False(Construct(ref platform, cls, out var receipt, out var result));
		Assert.True(receipt.IsNull && result.NativeObject.IsNull);
		Assert.Equal(0u, platform.SuperMethodCalls);
		Assert.True(Head(ref platform).IsNull);
		platform.AllocationAdmission = null;
		platform.DisposeObject(native);
		Assert.True(MuiMasterLifecycleCore.Dispose(ref platform, Root));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void FailedAttachmentRetainsOwnershipUntilExplicitCleanup(bool published)
	{
		var platform = Create(out var cls, out var native);
		if (published)
			platform.BeforeSuperMethod = (_, _, _) => platform.MappingAdmission = (address, _) => address != Tags;
		else platform.AllocationAdmission = (size, _) => size != MuiHeadlessObjectRecord.Size;
		Assert.False(Construct(ref platform, cls, out var receipt, out _));
		platform.MappingAdmission = null;
		platform.AllocationAdmission = null;
		Assert.True(MuiConstructionReceiptCore.TryRead(ref platform, Root, receipt, out var pending));
		Assert.Equal(native, pending.NativeObject);
		Assert.Equal((uint)(published ? MuiObjectAttachmentOwnership.Transferred : MuiObjectAttachmentOwnership.CallerOwned), pending.Ownership);
		Assert.False(MuiConstructionReceiptCore.TryCommit(ref platform, Root, receipt));
		Assert.False(MuiConstructionReceiptCore.TryDiscard(ref platform, Root, receipt));
		Assert.False(MuiMasterLifecycleCore.Dispose(ref platform, Root));
		Assert.Equal(0u, platform.NativeObjectDisposalCount);
		platform.BeforeSuperMethod = (_, _, _) =>
		{
			Assert.True(MuiConstructionReceiptCore.TryRead(ref platform, Root, receipt, out var cleaning));
			Assert.Equal(MuiConstructionReceiptRecord.Cleaning, cleaning.Phase);
			Assert.False(MuiConstructionReceiptCore.TryCleanup(ref platform, Root, receipt));
			Assert.False(MuiMasterLifecycleCore.Dispose(ref platform, Root));
		};
		platform.SuperDisposeNative = true;
		Assert.True(MuiConstructionReceiptCore.TryCleanup(ref platform, Root, receipt));
		Assert.Equal(1u, platform.NativeObjectDisposalCount);
		Assert.Equal(2u, platform.SuperMethodCalls);
		Assert.True(Head(ref platform).IsNull);
		Assert.True(MuiMasterLifecycleCore.Dispose(ref platform, Root));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void SuccessfulCleanupSurvivesUnlinkRefusalWithoutRepeatingSuper(bool published)
	{
		var platform = Create(out var cls, out _);
		if (published)
			platform.BeforeSuperMethod = (_, _, _) => platform.MappingAdmission = (address, _) => address != Tags;
		else platform.AllocationAdmission = (size, _) => size != MuiHeadlessObjectRecord.Size;
		Assert.False(Construct(ref platform, cls, out var receipt, out _));
		platform.AllocationAdmission = null;
		platform.MappingAdmission = null;
		Assert.True(MuiConstructionReceiptCodec.TryGetRootHead(ref platform, Root, out var headField));
		platform.SuperDisposeNative = true;
		platform.MappingAdmission = (address, _) => address != headField ||
			!MuiConstructionReceiptCodec.TryRead(ref platform, receipt, out var value) || value.Phase != MuiConstructionReceiptRecord.Done;
		Assert.False(MuiConstructionReceiptCore.TryCleanup(ref platform, Root, receipt));
		Assert.True(MuiConstructionReceiptCodec.TryRead(ref platform, receipt, out var completed));
		Assert.Equal(MuiConstructionReceiptRecord.Done, completed.Phase);
		Assert.True(completed.NativeObject.IsNull);
		Assert.Equal(1u, platform.NativeObjectDisposalCount);
		platform.MappingAdmission = null;
		Assert.Equal(receipt, Head(ref platform));
		Assert.True(MuiConstructionReceiptCore.TryCleanup(ref platform, Root, receipt));
		Assert.Equal(1u, platform.NativeObjectDisposalCount);
		Assert.Equal(2u, platform.SuperMethodCalls);
		Assert.True(MuiMasterLifecycleCore.Dispose(ref platform, Root));
	}

	[Fact]
	public void NullSuperclassResultCanDiscardReceiptWithoutNativeDisposal()
	{
		var platform = Create(out var cls, out var unused);
		platform.DispatchResult = 0;
		Assert.False(Construct(ref platform, cls, out var receipt, out _));
		Assert.True(MuiConstructionReceiptCore.TryCleanup(ref platform, Root, receipt));
		Assert.Equal(0u, platform.NativeObjectDisposalCount);
		Assert.Equal(1u, platform.SuperMethodCalls);
		platform.DisposeObject(unused);
		Assert.True(MuiMasterLifecycleCore.Dispose(ref platform, Root));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ExistingAndUnresolvedAliasesAreNeverDisposedByReceipt(bool unreadable)
	{
		var platform = Create(out var cls, out var native);
		Assert.Equal(native, MuiHeadlessObjectCore.AttachConstructedObject(ref platform, State, cls, native, APTR.Null));
		var sidecar = MuiHeadlessObjectCore.FindObject(ref platform, State, native);
		if (unreadable)
			platform.BeforeSuperMethod = (_, _, _) => platform.MappingAdmission = (address, _) => address != sidecar;
		Assert.False(Construct(ref platform, cls, out var receipt, out _));
		platform.MappingAdmission = null;
		Assert.Equal(!unreadable, MuiConstructionReceiptCore.TryCleanup(ref platform, Root, receipt));
		Assert.Equal(0u, platform.NativeObjectDisposalCount);
		Assert.True(MuiHeadlessObjectCore.FindObject(ref platform, State, native).IsNotNull);
		if (unreadable)
		{
			Assert.Equal(receipt, Head(ref platform));
			Assert.False(MuiConstructionReceiptCore.TryDiscard(ref platform, Root, receipt));
			Assert.False(MuiMasterLifecycleCore.Dispose(ref platform, Root));
		}
		else Assert.True(MuiMasterLifecycleCore.Dispose(ref platform, Root));
	}

	[Fact]
	public void NonHeadDiscardPreservesOtherReceiptAndRejectsStaleHandle()
	{
		var platform = Create(out var cls, out var unused);
		Assert.True(MuiConstructionReceiptCore.TryBegin(ref platform, Root, State, cls, cls, out var first));
		Assert.True(MuiConstructionReceiptCore.TryBegin(ref platform, Root, State, cls, cls, out var second));
		Assert.True(MuiConstructionReceiptCore.TryDiscard(ref platform, Root, first));
		Assert.Equal(second, Head(ref platform));
		Assert.True(MuiConstructionReceiptCore.TryRead(ref platform, Root, second, out var remaining));
		Assert.True(remaining.Next.IsNull);
		Assert.False(MuiConstructionReceiptCore.TryDiscard(ref platform, Root, first));
		Assert.True(MuiConstructionReceiptCore.TryDiscard(ref platform, Root, second));
		platform.DisposeObject(unused);
		Assert.True(MuiMasterLifecycleCore.Dispose(ref platform, Root));
	}

	[Fact]
	public void AllocationReentryPreservesNewHead()
	{
		var platform = Create(out var cls, out var unused);
		var nested = APTR.Null;
		platform.AllocationAdmission = (size, _) =>
		{
			if (size != MuiConstructionReceiptRecord.Size) return true;
			platform.AllocationAdmission = null;
			Assert.True(MuiConstructionReceiptCore.TryBegin(ref platform, Root, State, cls, cls, out nested));
			return true;
		};
		Assert.True(MuiConstructionReceiptCore.TryBegin(ref platform, Root, State, cls, cls, out var outer));
		Assert.Equal(outer, Head(ref platform));
		Assert.True(MuiConstructionReceiptCore.TryRead(ref platform, Root, outer, out var value));
		Assert.Equal(nested, value.Next);
		Assert.True(MuiConstructionReceiptCore.TryDiscard(ref platform, Root, outer));
		Assert.Equal(nested, Head(ref platform));
		Assert.True(MuiConstructionReceiptCore.TryDiscard(ref platform, Root, nested));
		platform.DisposeObject(unused);
		Assert.True(MuiMasterLifecycleCore.Dispose(ref platform, Root));
	}

	[Fact]
	public void CleanupCallbackCanPrependReceiptWithoutLosingIt()
	{
		var platform = Create(out var cls, out _);
		platform.AllocationAdmission = (size, _) => size != MuiHeadlessObjectRecord.Size;
		Assert.False(Construct(ref platform, cls, out var receipt, out _));
		platform.AllocationAdmission = null;
		var nested = APTR.Null;
		platform.BeforeSuperMethod = (_, _, _) =>
			Assert.True(MuiConstructionReceiptCore.TryBegin(ref platform, Root, State, cls, cls, out nested));
		platform.SuperDisposeNative = true;
		Assert.True(MuiConstructionReceiptCore.TryCleanup(ref platform, Root, receipt));
		Assert.Equal(nested, Head(ref platform));
		Assert.True(MuiConstructionReceiptCore.TryRead(ref platform, Root, nested, out var value));
		Assert.True(value.Next.IsNull);
		Assert.Equal(1u, platform.NativeObjectDisposalCount);
		Assert.True(MuiConstructionReceiptCore.TryDiscard(ref platform, Root, nested));
		Assert.True(MuiMasterLifecycleCore.Dispose(ref platform, Root));
	}

	[Fact]
	public void RootTeardownRejectsReceiptCreationAndRecursiveTeardownFromCallback()
	{
		var platform = Create(out var cls, out var native);
		Assert.Equal(native, MuiHeadlessObjectCore.AttachConstructedObject(ref platform, State, cls, native, APTR.Null));
		var called = false;
		platform.AllocationFreed = (_, _) =>
		{
			platform.AllocationFreed = null;
			called = true;
			Assert.False(MuiConstructionReceiptCore.TryBegin(ref platform, Root, State, cls, cls, out var receipt));
			Assert.True(receipt.IsNull);
			Assert.False(MuiMasterLifecycleCore.Dispose(ref platform, Root));
		};
		Assert.True(MuiMasterLifecycleCore.Dispose(ref platform, Root));
		Assert.True(called);
	}

	[Fact]
	public void RefusedRootTeardownAllowsLaterReceiptCreationAndDisposalRetry()
	{
		var platform = Create(out var cls, out var native);
		Assert.Equal(native, MuiHeadlessObjectCore.AttachConstructedObject(ref platform, State, cls, native, APTR.Null));
		var sidecar = MuiHeadlessObjectCore.FindObject(ref platform, State, native);
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform, sidecar, out var obj));
		obj.Flags |= MuiHeadlessObjectCore.ObjectProviderBusy;
		Assert.True(MuiHeadlessObjectCodec.Write(ref platform, sidecar, obj));
		Assert.False(MuiMasterLifecycleCore.Dispose(ref platform, Root));
		Assert.True(MuiConstructionReceiptCore.TryBegin(ref platform, Root, State, cls, cls, out var receipt));
		Assert.True(MuiConstructionReceiptCore.TryDiscard(ref platform, Root, receipt));
		obj.Flags &= ~MuiHeadlessObjectCore.ObjectProviderBusy;
		Assert.True(MuiHeadlessObjectCodec.Write(ref platform, sidecar, obj));
		Assert.True(MuiMasterLifecycleCore.Dispose(ref platform, Root));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void AttachmentAllocationReentryCannotPublishDuplicateSidecars(bool unreadableAlias)
	{
		var platform = Create(out var cls, out var native);
		var nestedSidecar = APTR.Null;
		platform.AllocationAdmission = (size, _) =>
		{
			if (size != MuiHeadlessObjectRecord.Size) return true;
			platform.AllocationAdmission = null;
			Assert.Equal(native, MuiHeadlessObjectCore.AttachConstructedObject(ref platform,
				State, cls, native, APTR.Null));
			nestedSidecar = MuiHeadlessObjectCore.FindObject(ref platform, State, native);
			if (unreadableAlias)
				platform.MappingAdmission = (address, _) => address != nestedSidecar;
			return true;
		};
		Assert.False(Construct(ref platform, cls, out var receipt, out var result));
		platform.MappingAdmission = null;
		Assert.Equal(unreadableAlias ? MuiObjectAttachmentOwnership.Unresolved :
			MuiObjectAttachmentOwnership.AlreadyRegistered, result.Ownership);
		Assert.Equal(nestedSidecar, MuiHeadlessObjectCore.FindObject(ref platform, State, native));
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, cls, out var classValue));
		Assert.Equal(1u, classValue.ObjectCount);
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform, nestedSidecar, out var obj));
		Assert.True(obj.Next.IsNull);
		Assert.Equal(!unreadableAlias, MuiConstructionReceiptCore.TryCleanup(ref platform, Root, receipt));
		Assert.Equal(0u, platform.NativeObjectDisposalCount);
		Assert.Equal(1u, platform.SuperMethodCalls);
		// Unresolved provenance is intentionally retained, not guessed from a
		// later lookup. The known alias can be discarded and disposed by its owner.
		if (!unreadableAlias) Assert.True(MuiMasterLifecycleCore.Dispose(ref platform, Root));
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public void FailedAllocationReentryRetainsExistingNativeObject(bool dispatcher, bool unreadableAlias)
	{
		var platform = Create(out var cls, out var native);
		var nestedSidecar = APTR.Null;
		platform.AllocationAdmission = (size, _) =>
		{
			if (size != MuiHeadlessObjectRecord.Size) return true;
			platform.AllocationAdmission = null;
			Assert.Equal(native, MuiHeadlessObjectCore.AttachConstructedObject(ref platform,
				State, cls, native, APTR.Null));
			nestedSidecar = MuiHeadlessObjectCore.FindObject(ref platform, State, native);
			if (unreadableAlias)
				platform.MappingAdmission = (address, _) => address != nestedSidecar;
			return false;
		};
		MuiObjectAttachmentOwnership ownership;
		var receipt = APTR.Null;
		if (dispatcher)
		{
			Assert.False(Construct(ref platform, cls, out receipt, out var result));
			ownership = result.Ownership;
		}
		else
			Assert.True(MuiHeadlessObjectCore.AttachConstructedObject(ref platform, State,
				cls, native, APTR.Null, out ownership).IsNull);
		platform.MappingAdmission = null;
		Assert.Equal(unreadableAlias ? MuiObjectAttachmentOwnership.Unresolved :
			MuiObjectAttachmentOwnership.AlreadyRegistered, ownership);
		Assert.Equal(0u, platform.NativeObjectDisposalCount);
		Assert.Equal(nestedSidecar, MuiHeadlessObjectCore.FindObject(ref platform, State, native));
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, cls, out var classValue));
		Assert.Equal(1u, classValue.ObjectCount);
		if (dispatcher)
			Assert.Equal(!unreadableAlias, MuiConstructionReceiptCore.TryCleanup(ref platform, Root, receipt));
		if (!dispatcher || !unreadableAlias) Assert.True(MuiMasterLifecycleCore.Dispose(ref platform, Root));
	}

	private static bool Construct(ref MuiHeadlessTestPlatform platform, APTR cls,
		out APTR receipt, out MuiBoopsiConstructionResult result) =>
		MuiConstructionReceiptCore.TryConstruct(ref platform, Root, State, cls, cls,
			MuiHeadlessObjectCore.ClassPointer(ref platform, cls), Message, out receipt, out result);

	private static APTR Head(ref MuiHeadlessTestPlatform platform)
	{
		Assert.True(MuiMasterPrivateRootCodec.TryRead(ref platform, Root, out var value));
		return value.PendingConstructionHead;
	}

	private static MuiHeadlessTestPlatform Create(out APTR cls, out APTR native)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, State);
		Assert.True(MuiMasterLifecycleCore.Create(ref platform, Root, State));
		platform.WriteCString(APTR.FromPointer(0x1100), "Notify.mui");
		cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			APTR.FromPointer(0x1100), APTR.Null, 8, APTR.FromPointer(0xD000));
		native = platform.NewObject(MuiHeadlessObjectCore.ClassPointer(ref platform, cls), APTR.Null);
		platform.DispatchResult = native.Raw;
		BOOPSIGuestCodec.WriteOpSet(ref platform, Message, new opSet { MethodID = BOOPSI.OM_NEW, ops_AttrList = Tags });
		return platform;
	}
}
