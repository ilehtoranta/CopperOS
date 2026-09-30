using Amiga;

namespace CopperOS.MuiMaster.NativeRoot;

// Freestanding recovery-state exercise using simulated BOOPSI/allocator services.
// Neither native Intuition dispatch nor heap release is asserted by this fixture.
public static class MuiNativeConstructionReceiptRegression
{
	public static uint ConstructionReceiptRoot()
	{
		var platform = default(MuiNativeHeadlessPlatform);
		platform.Reset();
		var state = APTR.FromPointer(0x00036000);
		var root = APTR.FromPointer(0x00036300);
		var message = APTR.FromPointer(0x00036200);
		if (!MuiMasterLifecycleCore.Create(ref platform, root, state)) return 201;
		var name = APTR.FromPointer(CString.ToUInt32(CString.FromLiteral("Notify.mui")));
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, state,
			name, APTR.Null, 8, APTR.FromPointer(1));
		if (!MuiHeadlessClassCodec.TryRead(ref platform, cls, out var classValue)) return 202;
		var capture = platform.Allocate(MuiNativeBoopsiLifecycleProbe.Size, 0);
		MuiNativeBoopsiLifecycleProbe probe = default;
		probe.Magic = MuiNativeBoopsiLifecycleProbe.Cookie;
		if (!MuiNativeBoopsiLifecycleProbeCodec.Write(ref platform, capture, probe)) return 203;
		platform.BoopsiLifecycleProbe = capture;
		opSet packet = default;
		packet.MethodID = BOOPSI.OM_NEW;
		BOOPSIGuestCodec.WriteOpSet(ref platform, message, packet);
		if (!MuiConstructionReceiptCore.TryConstruct(ref platform, root, state, cls, cls,
			classValue.Boopsi, message, out var receipt, out var created)) return 204;
		if (!MuiConstructionReceiptCore.TryRead(ref platform, root, receipt, out var pending) ||
			pending.NativeObject.IsNull || pending.NativeObject != created.NativeObject ||
			pending.InitializedObject != created.NativeObject ||
			pending.Phase != MuiConstructionReceiptRecord.Returned ||
			pending.Ownership != (uint)MuiObjectAttachmentOwnership.Transferred ||
			MuiMasterLifecycleCore.Dispose(ref platform, root)) return 205;
		if (!MuiConstructionReceiptCore.TryCommit(ref platform, root, receipt) ||
			MuiConstructionReceiptCore.TryCommit(ref platform, root, receipt) ||
			!MuiMasterPrivateRootCodec.TryRead(ref platform, root, out var owner) ||
			owner.PendingConstructionHead.IsNotNull) return 206;
		BOOPSIGuestCodec.WriteMethodId(ref platform, message, BOOPSI.OM_DISPOSE);
		if (!MuiBoopsiBaseDisposalCore.TryDispose(ref platform, state, cls,
			created.NativeObject, message, out _)) return 207;

		// Retain a successfully attached base object while derived failure cleanup
		// is waiting for an owned child. No superclass disposal may occur early.
		BOOPSIGuestCodec.WriteOpSet(ref platform, message, packet);
		if (!MuiConstructionReceiptCore.TryConstruct(ref platform, root, state, cls, cls,
			classValue.Boopsi, message, out receipt, out created)) return 208;
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, state, cls, APTR.Null);
		if (child.IsNull || !MuiFamilyCore.AddTail(ref platform, state, created.NativeObject, child)) return 209;
		var childRecord = MuiHeadlessObjectCore.FindObject(ref platform, state, child);
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, childRecord, out var childValue)) return 210;
		childValue.Flags |= MuiHeadlessObjectCore.ObjectProviderBusy;
		if (!MuiHeadlessObjectCodec.Write(ref platform, childRecord, childValue)) return 211;
		if (MuiConstructionReceiptCore.TryCleanup(ref platform, root, receipt) ||
			!MuiConstructionReceiptCore.TryRead(ref platform, root, receipt, out pending) ||
			pending.Phase != MuiConstructionReceiptRecord.Returned ||
			pending.NativeObject != created.NativeObject ||
			MuiConstructionReceiptCore.TryCommit(ref platform, root, receipt) ||
			MuiMasterLifecycleCore.Dispose(ref platform, root) ||
			!MuiNativeBoopsiLifecycleProbeCodec.TryRead(ref platform, capture, out probe) || probe.Calls != 3) return 212;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, childRecord, out childValue)) return 213;
		childValue.Flags &= ~MuiHeadlessObjectCore.ObjectProviderBusy;
		if (!MuiHeadlessObjectCodec.Write(ref platform, childRecord, childValue) ||
			!MuiConstructionReceiptCore.TryCleanup(ref platform, root, receipt) ||
			MuiConstructionReceiptCore.TryCleanup(ref platform, root, receipt)) return 214;
		if (!MuiNativeBoopsiLifecycleProbeCodec.TryRead(ref platform, capture, out probe) ||
			probe.Calls != 4 || probe.LastMethod != BOOPSI.OM_DISPOSE ||
			probe.LastOperand != created.NativeObject ||
			!MuiHeadlessStateCodec.TryRead(ref platform, state, out var registry) || registry.Objects.IsNotNull ||
			!MuiHeadlessClassCodec.TryRead(ref platform, cls, out classValue) || classValue.ObjectCount != 0) return 215;

		probe.FailNew = 1;
		if (!MuiNativeBoopsiLifecycleProbeCodec.Write(ref platform, capture, probe)) return 216;
		if (MuiConstructionReceiptCore.TryConstruct(ref platform, root, state, cls, cls,
			classValue.Boopsi, message, out receipt, out created) || receipt.IsNull ||
			created.NativeObject.IsNotNull || !MuiConstructionReceiptCore.TryCleanup(ref platform, root, receipt)) return 217;
		if (!MuiNativeBoopsiLifecycleProbeCodec.TryRead(ref platform, capture, out probe) || probe.Calls != 5) return 218;

		// Unlink a non-head reserved receipt through its named predecessor field.
		if (!MuiConstructionReceiptCore.TryBegin(ref platform, root, state, cls, cls, out var first) ||
			!MuiConstructionReceiptCore.TryBegin(ref platform, root, state, cls, cls, out var second) ||
			!MuiConstructionReceiptCore.TryDiscard(ref platform, root, first) ||
			!MuiConstructionReceiptCore.TryRead(ref platform, root, second, out pending) || pending.Next.IsNotNull ||
			!MuiConstructionReceiptCore.TryDiscard(ref platform, root, second)) return 219;
		// Admission while teardown owns the root, followed by a genuinely refused
		// disposal that must release its operation bit for a later retry.
		if (!MuiMasterPrivateRootCodec.TryRead(ref platform, root, out owner)) return 220;
		owner.Flags |= MuiMasterLifecycleCore.RootDisposing;
		if (!MuiMasterPrivateRootCodec.Write(ref platform, root, owner) ||
			MuiConstructionReceiptCore.TryBegin(ref platform, root, state, cls, cls, out _) ||
			MuiMasterLifecycleCore.Dispose(ref platform, root)) return 221;
		owner.Flags &= ~MuiMasterLifecycleCore.RootDisposing;
		if (!MuiMasterPrivateRootCodec.Write(ref platform, root, owner)) return 222;
		var busy = MuiHeadlessObjectCore.CreateObjectA(ref platform, state, cls, APTR.Null);
		var busyRecord = MuiHeadlessObjectCore.FindObject(ref platform, state, busy);
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, busyRecord, out var busyValue)) return 223;
		busyValue.Flags |= MuiHeadlessObjectCore.ObjectProviderBusy;
		if (!MuiHeadlessObjectCodec.Write(ref platform, busyRecord, busyValue) ||
			MuiMasterLifecycleCore.Dispose(ref platform, root) ||
			!MuiMasterPrivateRootCodec.TryRead(ref platform, root, out owner) ||
			(owner.Flags & MuiMasterLifecycleCore.RootDisposing) != 0 ||
			!MuiConstructionReceiptCore.TryBegin(ref platform, root, state, cls, cls, out receipt) ||
			!MuiConstructionReceiptCore.TryDiscard(ref platform, root, receipt)) return 224;
		busyValue.Flags &= ~MuiHeadlessObjectCore.ObjectProviderBusy;
		if (!MuiHeadlessObjectCodec.Write(ref platform, busyRecord, busyValue) ||
			!MuiMasterLifecycleCore.Dispose(ref platform, root)) return 225;

		MuiNativeAllocationReentryStatusCodec.Write(0);
		_ = RunAllocationReentry();
		return MuiNativeAllocationReentryStatusCodec.Read() == 42 ? 42u : 230u;
	}

	private static uint RunAllocationReentry()
	{
		var platform = default(MuiNativeHeadlessPlatform);
		platform.Reset();
		var state = APTR.FromPointer(0x00036000);
		var root = APTR.FromPointer(0x00036300);
		var message = APTR.FromPointer(0x00036200);
		if (!MuiMasterLifecycleCore.Create(ref platform, root, state)) return ReentryFailure(226);
		var name = APTR.FromPointer(CString.ToUInt32(CString.FromLiteral("Notify.mui")));
		var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, state,
			name, APTR.Null, 8, APTR.FromPointer(1));
		if (!MuiHeadlessClassCodec.TryRead(ref platform, cls, out var classValue)) return ReentryFailure(227);
		var capture = platform.Allocate(MuiNativeBoopsiLifecycleProbe.Size, 0);
		platform.BoopsiLifecycleProbe = capture;
		var probe = default(MuiNativeBoopsiLifecycleProbe);
		probe.Magic = MuiNativeBoopsiLifecycleProbe.Cookie;
		if (!MuiNativeBoopsiLifecycleProbeCodec.Write(ref platform, capture, probe)) return ReentryFailure(228);
		var packet = default(opSet);
		packet.MethodID = BOOPSI.OM_NEW;
		BOOPSIGuestCodec.WriteOpSet(ref platform, message, packet);
		var reentry = default(MuiNativeAllocationReentryRecord);
		reentry.Magic = MuiNativeAllocationReentryRecord.Cookie;
		reentry.Mode = MuiNativeAllocationReentryRecord.RunAndSucceed;
		reentry.State = state;
		reentry.Class = cls;
		if (!MuiNativeAllocationReentryCodec.Write(ref platform, reentry)) return ReentryFailure(229);
		var control = default(MuiNativeAllocationReentryControl);
		control.Mode = reentry.Mode;
		control.State = state;
		control.Class = cls;
		MuiNativeAllocationReentryControlCodec.Write(control);
		var reentryComplete = MuiConstructionReceiptCore.TryConstruct(ref platform, root, state, cls, cls,
			classValue.Boopsi, message, out var receipt, out var created);
		reentry = MuiNativeAllocationReentryProjection.ToRecord(
			MuiNativeAllocationReentryControlCodec.Read());
		if (!MuiNativeAllocationReentryCodec.Write(ref platform, reentry)) return ReentryFailure(229);
		var reentryFailure = 0u;
		if (reentryComplete) reentryFailure |= 1;
		if (receipt.IsNull) reentryFailure |= 2;
		if (created.NativeObject.IsNull) reentryFailure |= 4;
		if (created.InitializedObject.IsNotNull) reentryFailure |= 8;
		if (created.Ownership != MuiObjectAttachmentOwnership.AlreadyRegistered) reentryFailure |= 16;
		if (!MuiNativeAllocationReentryCodec.TryRead(ref platform, out reentry)) reentryFailure |= 32;
		if (reentry.NativeObject != created.NativeObject) reentryFailure |= 64;
		if (reentry.NestedObject.IsNull) reentryFailure |= 128;
		if (reentry.NestedOwnership != (uint)MuiObjectAttachmentOwnership.Transferred) reentryFailure |= 256;
		if (!MuiHeadlessClassCodec.TryRead(ref platform, cls, out classValue)) reentryFailure |= 512;
		if (classValue.ObjectCount != 1) reentryFailure |= 1024;
		if (reentryFailure != 0) return ReentryFailure(230 + reentryFailure);
		if (!MuiConstructionReceiptCore.TryCleanup(ref platform, root, receipt)) return ReentryFailure(1300);
		if (!MuiMasterLifecycleCore.Dispose(ref platform, root)) return ReentryFailure(1301);

		if (!MuiMasterLifecycleCore.Create(ref platform, root, state)) return ReentryFailure(231);
		cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, state,
			name, APTR.Null, 8, APTR.FromPointer(1));
		if (!MuiHeadlessClassCodec.TryRead(ref platform, cls, out classValue)) return ReentryFailure(232);
		reentry = default;
		reentry.Magic = MuiNativeAllocationReentryRecord.Cookie;
		reentry.Mode = MuiNativeAllocationReentryRecord.RunAndRefuseOuter;
		reentry.State = state;
		reentry.Class = cls;
		if (!MuiNativeAllocationReentryCodec.Write(ref platform, reentry)) return ReentryFailure(233);
		control = default;
		control.Mode = reentry.Mode;
		control.State = state;
		control.Class = cls;
		MuiNativeAllocationReentryControlCodec.Write(control);
		var outerRefused = MuiConstructionReceiptCore.TryConstruct(ref platform, root, state, cls, cls,
			classValue.Boopsi, message, out receipt, out created) || receipt.IsNull ||
			created.NativeObject.IsNull || created.InitializedObject.IsNotNull ||
			created.Ownership != MuiObjectAttachmentOwnership.AlreadyRegistered;
		reentry = MuiNativeAllocationReentryProjection.ToRecord(
			MuiNativeAllocationReentryControlCodec.Read());
		if (!MuiNativeAllocationReentryCodec.Write(ref platform, reentry) || outerRefused ||
			reentry.NativeObject != created.NativeObject || reentry.NestedObject.IsNull ||
			reentry.NestedOwnership != (uint)MuiObjectAttachmentOwnership.Transferred ||
			!MuiHeadlessClassCodec.TryRead(ref platform, cls, out classValue) || classValue.ObjectCount != 1 ||
			!MuiConstructionReceiptCore.TryCleanup(ref platform, root, receipt) ||
			!MuiMasterLifecycleCore.Dispose(ref platform, root)) return ReentryFailure(234);
		platform.BoopsiLifecycleProbe = APTR.Null;
		platform.Free(capture, MuiNativeBoopsiLifecycleProbe.Size);
		// Base construction/disposal remains independently qualified by its own
		// native root; keep this entry's result focused on both re-entry modes.
		MuiNativeAllocationReentryStatusCodec.Write(42);
		return 42;
	}

	private static uint ReentryFailure(uint result)
	{
		MuiNativeAllocationReentryStatusCodec.Write(result);
		return result;
	}
}
