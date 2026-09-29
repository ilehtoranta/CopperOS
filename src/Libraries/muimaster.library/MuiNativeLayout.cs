/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.MuiMaster;

// Native MUI_Layout uses the declaration-ordered MUIP_Layout record already
// owned by LayoutMessagesCore. Keep the public helper on that named struct and
// let the existing codec be the only guest-wire adapter.
internal static class MuiNativeLayoutServiceCore
{
	internal static bool Layout(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR muiLibrary, APTR obj,
		int left, int top, int width, int height, uint flags)
	{
		if (obj.IsNull || ownerRoot.IsNull || publicObjects.IsNull ||
			muiLibrary.IsNull ||
			!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, obj, out var binding)) return false;

		var memory = default(MuiNativeClassMemory);
		if (!memory.IsMapped(obj, _Object.Size)) return false;
		var objectValue = BOOPSIGuestCodec.ReadObjectHeader(ref memory, obj);
		var classPointer = objectValue.o_Class;
		if (classPointer.IsNull || !memory.IsMapped(classPointer, IClass.Size))
			return false;
		var classValue = BOOPSIGuestCodec.ReadClass(ref memory, classPointer);
		var entry = classValue.cl_Dispatcher.Entry;
		if (entry.IsNull) return false;
		if (!TryResolveCallbackBase(ref memory, binding.Lease, classPointer,
			muiLibrary, out var callbackBase)) return false;

		var message = platform.Allocate(MuiLayoutMessage.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (message.IsNull || !memory.IsMapped(message, MuiLayoutMessage.Size))
		{
			if (message.IsNotNull) platform.Free(message, MuiLayoutMessage.Size);
			return false;
		}

		var packet = default(MuiLayoutMessage);
		packet.MethodId = MuiLayoutPacketCodec.Layout;
		packet.Left = unchecked((uint)left);
		packet.Top = unchecked((uint)top);
		packet.Width = unchecked((uint)width);
		packet.Height = unchecked((uint)height);
		packet.Flags = flags;
		if (!MuiLayoutMessageStructCodec.WriteLayout(ref memory, message, packet))
		{
			platform.Free(message, MuiLayoutMessage.Size);
			return false;
		}

		var result = MuiNativeLayoutCalls.Dispatch(entry, classPointer, obj,
			message, callbackBase);
		platform.Free(message, MuiLayoutMessage.Size);
		return result != 0;
	}

	// A missing lease denotes an owned custom-class dispatcher, whose entry is
	// the library's exported wrapper. Builtin classes use the MUI base; external
	// classes must use their retained provider base. A present but malformed,
	// mismatched, or custom-class lease must never silently fall back to MUI's
	// base, since that would enter foreign code with the wrong A6 value.
	internal static bool TryResolveCallbackBase<TMemory>(ref TMemory memory,
		APTR leaseAddress, APTR classPointer, APTR muiLibrary,
		out APTR callbackBase) where TMemory : struct, IMuiGuestMemory
	{
		callbackBase = APTR.Null;
		if (classPointer.IsNull || muiLibrary.IsNull) return false;
		if (leaseAddress.IsNull)
		{
			callbackBase = muiLibrary;
			return true;
		}
		if (!MuiClassServiceLeaseCodec.TryRead(ref memory, leaseAddress,
			out var lease) || lease.Boopsi != classPointer) return false;

		var kind = lease.Flags & (MuiClassServiceLayout.FlagBuiltin |
			MuiClassServiceLayout.FlagExternal | MuiClassServiceLayout.FlagCustom);
		if (kind == MuiClassServiceLayout.FlagBuiltin)
		{
			if (lease.LibraryBase.IsNull)
				callbackBase = muiLibrary;
			else
				callbackBase = lease.LibraryBase;
			return true;
		}
		if (kind != MuiClassServiceLayout.FlagExternal || lease.LibraryBase.IsNull)
			return false;
		callbackBase = lease.LibraryBase;
		return true;
	}
}

// The ABI bridge is intentionally limited to the BOOPSI dispatcher call. The
// MUIP_Layout message itself remains a named packed struct, not an offset table.
internal static class MuiNativeLayoutCalls
{
	internal static uint Dispatch(APTR entry, APTR cls, APTR obj, APTR message,
		APTR libraryBase) => DispatchCall(entry, cls, obj, message, libraryBase);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint DispatchCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A0)] APTR cls,
		[M68kRegister(M68kRegister.A2)] APTR obj,
		[M68kRegister(M68kRegister.A1)] APTR message,
		[M68kRegister(M68kRegister.A6)] APTR libraryBase);
}
