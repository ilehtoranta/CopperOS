/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.MuiMaster;

// Explicit-base commodities.library calls used to preserve MorphOS' own IX
// parser and matcher. Vector arithmetic and register placement stay isolated
// here; InputEvent and InputXpression remain named guest records elsewhere.
internal static class MuiNativeCommoditiesCalls
{
	private const short ParseIXLvo = -132;
	private const short MatchIXLvo = -204;

	internal static APTR Open()
	{
		var name = CString.FromLiteral("commodities.library");
		return Exec.OpenLibraryRaw(name, 0);
	}

	internal static int ParseIX(APTR library, APTR description,
		APTR inputExpression)
	{
		if (library.IsNull || description.IsNull || inputExpression.IsNull)
			return -2;
		return ParseIXCall(Entry(library, ParseIXLvo), library,
			CString.FromPointer(description.Raw), inputExpression);
	}

	internal static bool MatchIX(APTR library, APTR inputEvent,
		APTR inputExpression)
	{
		if (library.IsNull || inputEvent.IsNull || inputExpression.IsNull)
			return false;
		return MatchIXCall(Entry(library, MatchIXLvo), library, inputEvent,
			inputExpression) != 0;
	}

	private static APTR Entry(APTR library, short lvo) =>
		APTR.FromPointer(unchecked(library.Raw - (uint)-lvo));

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern int ParseIXCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] CString description,
		[M68kRegister(M68kRegister.A1)] APTR inputExpression);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern int MatchIXCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] APTR inputEvent,
		[M68kRegister(M68kRegister.A1)] APTR inputExpression);
}
