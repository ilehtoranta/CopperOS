/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Native MUI calls use the same declaration-ordered IClass dispatcher bridge
// as the drawing and layout services. This keeps DoMethod/CoerceMethod on the
// guest side of the ABI without importing a managed runtime or retaining a
// host callback. The object and class records are admitted before the indirect
// call; no private object field is read by numeric offset.
internal static class MuiNativeBoopsiDispatch
{
	internal static uint DoMethod(ref MuiNativeClassPlatform platform,
		APTR obj, APTR message)
	{
		var memory = default(MuiNativeClassMemory);
		if (!TryGetObjectClass(ref memory, obj, out var classPointer)) return 0;
		return Dispatch(ref platform, ref memory, classPointer, obj, message);
	}

	internal static uint CoerceMethod(ref MuiNativeClassPlatform platform,
		APTR classPointer, APTR obj, APTR message)
	{
		var memory = default(MuiNativeClassMemory);
		if (classPointer.IsNull || !memory.IsMapped(classPointer, IClass.Size) ||
			!memory.IsMapped(obj, _Object.Size) || message.IsNull) return 0;
		return Dispatch(ref platform, ref memory, classPointer, obj, message);
	}

	private static uint Dispatch(ref MuiNativeClassPlatform platform,
		ref MuiNativeClassMemory memory, APTR classPointer, APTR obj,
		APTR message)
	{
		if (message.IsNull || !memory.IsMapped(message,
			MuiHeadlessMethodMessage.Size)) return 0;
		var classValue = BOOPSIGuestCodec.ReadClass(ref memory, classPointer);
		var entry = classValue.cl_Dispatcher.Entry;
		if (entry.IsNull) return 0;
		var callbackBase = CallbackBase(ref platform, ref memory, obj);
		return MuiNativeRedrawCalls.Dispatch(entry, classPointer, obj, message,
			callbackBase);
	}

	private static bool TryGetObjectClass<TMemory>(ref TMemory memory,
		APTR obj, out APTR classPointer) where TMemory : struct, IMuiGuestMemory
	{
		classPointer = APTR.Null;
		if (obj.IsNull || !memory.IsMapped(obj, _Object.Size)) return false;
		var objectValue = BOOPSIGuestCodec.ReadObjectHeader(ref memory, obj);
		classPointer = objectValue.o_Class;
		return classPointer.IsNotNull && memory.IsMapped(classPointer,
			IClass.Size);
	}

	private static APTR CallbackBase(ref MuiNativeClassPlatform platform,
		ref MuiNativeClassMemory memory, APTR obj)
	{
		var callbackBase = APTR.Null;
		var ownerRoot = platform.OwnerRoot;
		if (!MuiMasterPrivateRootCodec.TryRead(ref memory, ownerRoot,
			out var root) || root.LoaderState == 0) return callbackBase;
		var owner = APTR.FromPointer(root.LoaderState);
		if (!MuiNativeClassOwnerCodec.TryRead(ref memory, owner,
			out var ownerValue)) return callbackBase;
		callbackBase = ownerValue.LibraryBase;
		if (!MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref memory,
			owner, out var publicObjects) ||
			!MuiNativePublicObjectCore.TryFindBinding(ref platform, publicObjects,
				ownerRoot, obj, out var binding) || binding.Lease.IsNull ||
			!MuiClassServiceLeaseCodec.TryRead(ref memory, binding.Lease,
				out var lease) || lease.LibraryBase.IsNull) return callbackBase;
		return lease.LibraryBase;
	}
}
