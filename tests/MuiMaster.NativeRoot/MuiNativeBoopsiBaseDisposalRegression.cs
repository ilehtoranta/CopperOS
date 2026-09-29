using Amiga;

namespace CopperOS.MuiMaster.NativeRoot;

// Compiler/execution coverage for the base-disposal stage with the existing
// simulated providers. DoSuperMethod returns 1 in that provider; this is not
// real Intuition dispatch, native allocation release, or public vector proof.
public static class MuiNativeBoopsiBaseDisposalRegression
{
	public static uint BaseDisposalRoot()
	{
		var platform = default(MuiNativeHeadlessPlatform);
		platform.Reset();
		var state = APTR.FromPointer(0x00036000);
		var message = APTR.FromPointer(0x00036200);
		if (!MuiHeadlessObjectCore.Initialize(ref platform, state)) return 1;
		var baseName = APTR.FromPointer(CString.ToUInt32(CString.FromLiteral("Notify.mui")));
		var derivedName = APTR.FromPointer(CString.ToUInt32(CString.FromLiteral("DisposalProbe.mui")));
		var baseClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, state,
			baseName, APTR.Null, 8, APTR.FromPointer(1));
		if (!MuiHeadlessClassCodec.TryRead(ref platform, baseClass, out var baseValue)) return 2;
		var derivedClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, state,
			derivedName, baseValue.Boopsi, 8, APTR.FromPointer(1));
		if (derivedClass.IsNull) return 3;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, state, derivedClass, APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, state, baseClass, APTR.Null);
		if (obj.IsNull || child.IsNull || !MuiFamilyCore.AddTail(ref platform, state, obj, child)) return 4;
		BOOPSIGuestCodec.WriteMethodId(ref platform, message, BOOPSI.OM_SET);
		if (MuiBoopsiBaseDisposalCore.TryDispose(ref platform, state, baseClass, obj, message, out _)) return 5;
		BOOPSIGuestCodec.WriteMethodId(ref platform, message, BOOPSI.OM_DISPOSE);
		var childRecord = MuiHeadlessObjectCore.FindObject(ref platform, state, child);
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, childRecord, out var childValue)) return 6;
		childValue.Flags |= MuiHeadlessObjectCore.ObjectProviderBusy;
		if (!MuiHeadlessObjectCodec.Write(ref platform, childRecord, childValue)) return 7;
		if (MuiBoopsiBaseDisposalCore.TryDispose(ref platform, state, baseClass, obj,
			message, out var result) || result != 0 ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull) return 8;
		if (MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj)) return 9;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, childRecord, out childValue)) return 10;
		childValue.Flags &= ~MuiHeadlessObjectCore.ObjectProviderBusy;
		if (!MuiHeadlessObjectCodec.Write(ref platform, childRecord, childValue)) return 11;
		if (!MuiBoopsiBaseDisposalCore.TryDispose(ref platform, state, baseClass, obj,
			message, out result) || result != 1) return 12;
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state, out var registry) ||
			registry.Objects.IsNotNull ||
			!MuiHeadlessClassCodec.TryRead(ref platform, baseClass, out baseValue) ||
			baseValue.ObjectCount != 0 ||
			!MuiHeadlessClassCodec.TryRead(ref platform, derivedClass, out var derivedValue) ||
			derivedValue.ObjectCount != 0) return 13;
		if (MuiBoopsiBaseDisposalCore.TryDispose(ref platform, state, baseClass, obj,
			message, out result) || result != 0) return 14;
		if (!MuiHeadlessObjectCore.DeleteClass(ref platform, state, derivedClass) ||
			!MuiHeadlessObjectCore.DeleteClass(ref platform, state, baseClass)) return 15;
		return 42;
	}
}
