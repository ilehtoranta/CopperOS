using Amiga;

namespace CopperOS.MuiMaster.NativeRoot;

public static class MuiNativeBoopsiBaseConstructionRegression
{
	public static uint BaseConstructionRoot()
	{
		var platform = default(MuiNativeHeadlessPlatform);
		platform.Reset();
		var state = APTR.FromPointer(0x00036000);
		var message = APTR.FromPointer(0x00036200);
		if (!MuiHeadlessObjectCore.Initialize(ref platform, state)) return 101;
		var baseName = APTR.FromPointer(CString.ToUInt32(CString.FromLiteral("Notify.mui")));
		var derivedName = APTR.FromPointer(CString.ToUInt32(CString.FromLiteral("ConstructorProbe.mui")));
		var baseClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, state,
			baseName, APTR.Null, 8, APTR.FromPointer(1));
		if (!MuiHeadlessClassCodec.TryRead(ref platform, baseClass, out var baseValue)) return 102;
		var derivedClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, state,
			derivedName, baseValue.Boopsi, 8, APTR.FromPointer(1));
		if (!MuiHeadlessClassCodec.TryRead(ref platform, derivedClass, out var derivedValue)) return 103;
		var capture = platform.Allocate(MuiNativeBoopsiLifecycleProbe.Size, 0);
		var probe = new MuiNativeBoopsiLifecycleProbe { Magic = MuiNativeBoopsiLifecycleProbe.Cookie };
		if (!MuiNativeBoopsiLifecycleProbeCodec.Write(ref platform, capture, probe)) return 104;
		platform.BoopsiLifecycleProbe = capture;
		BOOPSIGuestCodec.WriteOpSet(ref platform, message, new opSet { MethodID = BOOPSI.OM_NEW });
		if (!MuiBoopsiBaseConstructionCore.TryConstruct(ref platform, state, baseClass,
			derivedClass, derivedValue.Boopsi, message, out var created) ||
			created.NativeObject.IsNull || created.NativeObject != created.InitializedObject ||
			created.Ownership != MuiObjectAttachmentOwnership.Transferred) return 105;
		if (!MuiNativeBoopsiLifecycleProbeCodec.TryRead(ref platform, capture, out probe) ||
			probe.Calls != 1 || probe.LastClass != baseValue.Boopsi ||
			probe.LastOperand != derivedValue.Boopsi || probe.LastMessage != message ||
			probe.LastMethod != BOOPSI.OM_NEW || probe.LastResult != created.NativeObject) return 106;
		if (MuiHeadlessObjectCore.CompleteConstructedObject(ref platform, state,
			derivedClass, created.NativeObject, APTR.FromPointer(0xFFFFFFFC)) != created.NativeObject) return 107;
		BOOPSIGuestCodec.WriteMethodId(ref platform, message, BOOPSI.OM_DISPOSE);
		if (!MuiBoopsiBaseDisposalCore.TryDispose(ref platform, state, baseClass,
			created.NativeObject, message, out var disposed) || disposed != 0) return 108;
		if (!MuiNativeBoopsiLifecycleProbeCodec.TryRead(ref platform, capture, out probe) ||
			probe.Calls != 2 || probe.LastClass != baseValue.Boopsi ||
			probe.LastOperand != created.NativeObject || probe.LastMethod != BOOPSI.OM_DISPOSE) return 109;
		probe.FailNew = 1;
		if (!MuiNativeBoopsiLifecycleProbeCodec.Write(ref platform, capture, probe)) return 110;
		BOOPSIGuestCodec.WriteOpSet(ref platform, message, new opSet { MethodID = BOOPSI.OM_NEW });
		if (MuiBoopsiBaseConstructionCore.TryConstruct(ref platform, state, baseClass,
			derivedClass, derivedValue.Boopsi, message, out var failed) || failed.NativeObject.IsNotNull ||
			failed.InitializedObject.IsNotNull) return 111;
		if (!MuiNativeBoopsiLifecycleProbeCodec.TryRead(ref platform, capture, out probe) || probe.Calls != 3 ||
			!MuiHeadlessStateCodec.TryRead(ref platform, state, out var registry) || registry.Objects.IsNotNull ||
			!MuiHeadlessClassCodec.TryRead(ref platform, derivedClass, out derivedValue) || derivedValue.ObjectCount != 0) return 112;
		if (!MuiHeadlessObjectCore.DeleteClass(ref platform, state, derivedClass) ||
			!MuiHeadlessObjectCore.DeleteClass(ref platform, state, baseClass)) return 113;
		platform.BoopsiLifecycleProbe = APTR.Null;
		platform.Free(capture, MuiNativeBoopsiLifecycleProbe.Size);
		// Re-execute the earlier base-disposal retry root with the probe disabled,
		// qualifying both simulator modes and legacy attachment in this snapshot.
		return MuiNativeBoopsiBaseDisposalRegression.BaseDisposalRoot();
	}
}
