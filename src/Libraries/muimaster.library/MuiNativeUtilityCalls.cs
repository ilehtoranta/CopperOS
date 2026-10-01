/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.MuiMaster;

// The native callback boundary is deliberately isolated from the semantic
// CallHook packet code.  Negative-vector arithmetic is an ABI detail of the
// utility.library call site; hook/message fields still cross through the
// named MUIM_CallHook record and its typed parameter cursor.
internal static class MuiNativeUtilityCalls
{
	internal static uint CallHookPkt(APTR library, APTR hook, APTR obj,
		APTR message)
	{
		if (library.IsNull || hook.IsNull || message.IsNull) return 0;
		return CallHookPktCall(Entry(library, UtilityLvo.CallHookPkt), hook,
			obj, message);
	}

	private static APTR Entry(APTR library, short lvo) =>
		APTR.FromPointer(unchecked(library.Raw - (uint)-lvo));

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint CallHookPktCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A0)] APTR hook,
		[M68kRegister(M68kRegister.A2)] APTR obj,
		[M68kRegister(M68kRegister.A1)] APTR message);
}
