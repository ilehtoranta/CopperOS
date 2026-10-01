/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Local handoff record, not a public ABI packet or an automatically persisted
// cleanup journal. The native gateway must retain unresolved/partial ownership
// before returning NULL to a public caller.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBoopsiConstructionResult
{
	internal APTR NativeObject;
	internal APTR InitializedObject;
	internal MuiObjectAttachmentOwnership Ownership;
}

// Entered once at the base MUI constructor. Derived-class construction follows
// successful generic attachment; it is not performed by this base stage.
internal static class MuiBoopsiBaseConstructionCore
{
	// The gateway holds the service/class scope and immutable opSet/tag storage
	// throughout the superclass callback and initialization. constructorOperand is
	// forwarded unchanged: the base stage does not invent a preallocated object.
	internal static bool TryConstruct<T>(ref T platform, APTR state,
		APTR dispatchClassRecord, APTR owningClassRecord, APTR constructorOperand,
		APTR message, out MuiBoopsiConstructionResult result)
		where T : struct, IMuiHeadlessPlatform
		=> TryConstructRetained(ref platform, state, dispatchClassRecord, owningClassRecord,
			constructorOperand, message, APTR.Null, out result);

	// Only a root-linked, Running receipt may supply this optional handoff.
	// Its owner holds the complete named record stable across callbacks;
	// publication cannot allocate or invoke further admission after the
	// superclass has returned a native object.
	internal static bool TryConstructRetained<T>(ref T platform, APTR state,
		APTR dispatchClassRecord, APTR owningClassRecord, APTR constructorOperand,
		APTR message, APTR receipt,
		out MuiBoopsiConstructionResult result) where T : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (message.IsNull || (message.Raw & 1u) != 0 ||
			!MuiBoopsiOpSetMessageCodec.TryRead(ref platform, message,
				out var packet) || packet.MethodId != BOOPSI.OM_NEW ||
			!MuiHeadlessMemory.Ensure(ref platform, state) ||
			!MuiAslTagListCore.Validate(ref platform, packet.Attributes) ||
			!MuiBoopsiRegisteredClassCore.TryResolveDispatchClass(ref platform, state,
				owningClassRecord, dispatchClassRecord, out var dispatchClass)) return false;
		result.NativeObject = APTR.FromPointer(platform.DoSuperMethod(
			dispatchClass.Boopsi, constructorOperand, message));
		if (receipt.IsNotNull)
		{
			// Keep the pending native pointer in the complete named receipt.  The
			// receipt remains the recovery authority while attachment may allocate
			// or re-enter; no caller infers this field from an offset.
			if (!MuiConstructionReceiptCodec.TryRead(ref platform, receipt,
				out var receiptValue)) return false;
			receiptValue.NativeObject = result.NativeObject;
			if (!MuiConstructionReceiptCodec.Write(ref platform, receipt,
				receiptValue)) return false;
		}
		if (result.NativeObject.IsNull) return false;
		// No automatic most-derived DisposeObject call while its OM_NEW stack is
		// still active. The handoff distinguishes caller-owned native storage from
		// a registered partial sidecar and from an existing/unresolved alias.
		result.InitializedObject = MuiHeadlessObjectCore.AttachConstructedObjectForDispatcher(
			ref platform, state, owningClassRecord, result.NativeObject,
			packet.Attributes, out result.Ownership);
		return result.InitializedObject.IsNotNull;
	}
}
