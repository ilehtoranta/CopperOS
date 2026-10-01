/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.MuiMaster;

// Explicit-base native calls. The caller owns a live library lease and admits
// its executable vectors before entering this boundary; no OS base is cached.
// Public structure access belongs in the SDK codecs, not in this call bridge.
internal static class MuiNativeIntuitionCalls
{
	// MorphOS 3.20 mui/mccheader.c: the fifth library vector is MCC_Query.
	// Selector zero returns MUI_CustomClass*, whose Class field is the IClass.
	private const short CustomClassQueryLvo = -30;

	internal static APTR MakeClass(APTR library, APTR classId, APTR superId,
		APTR superClass, uint size) => APTR.FromPointer(MakeClassCall(
			Entry(library, IntuitionLvo.MakeClass), library, classId, superId,
			superClass, size, 0));

	internal static void AddClass(APTR library, APTR cls) =>
		PointerVoidCall(Entry(library, IntuitionLvo.AddClass), library, cls);

	internal static void RemoveClass(APTR library, APTR cls) =>
		PointerVoidCall(Entry(library, IntuitionLvo.RemoveClass), library, cls);

	internal static bool FreeClass(APTR library, APTR cls) =>
		PointerResultCall(Entry(library, IntuitionLvo.FreeClass), library, cls) != 0;

	internal static APTR NewObject(APTR library, APTR cls, APTR tags) =>
		APTR.FromPointer(NewObjectCall(Entry(library, IntuitionLvo.NewObjectA),
			library, cls, APTR.Null, tags));

	internal static void DisposeObject(APTR library, APTR obj) =>
		PointerVoidCall(Entry(library, IntuitionLvo.DisposeObject), library, obj);

	internal static uint GetAttr(APTR library, uint attribute, APTR obj,
		APTR storage) => GetAttrCall(Entry(library, IntuitionLvo.GetAttr), library,
		attribute, obj, storage);

	internal static APTR NextObject(APTR library, APTR objectState) =>
		APTR.FromPointer(PointerResultCall(Entry(library,
			IntuitionLvo.NextObject), library, objectState));

	internal static bool ModifyIDCMP(APTR library, APTR window, uint flags) =>
		ModifyIDCMPCall(Entry(library, IntuitionLvo.ModifyIDCMP), library,
			window, flags) != 0;

	internal static void SetWindowPointerA(APTR library, APTR window,
		APTR tags) => WindowPointerCall(Entry(library,
		IntuitionLvo.SetWindowPointerA), library, window, tags);

	internal static void ChangeWindowBox(APTR library, APTR window,
		int left, int top, int width, int height) => WindowBoxCall(
		Entry(library, IntuitionLvo.ChangeWindowBox), library, window,
		left, top, width, height);

	internal static void BeginRefresh(APTR library, APTR window) =>
		PointerVoidCall(Entry(library, IntuitionLvo.BeginRefresh), library,
			window);

	internal static void EndRefresh(APTR library, APTR window, bool complete) =>
		WindowRefreshCompleteCall(Entry(library, IntuitionLvo.EndRefresh),
			library, window, complete ? 1u : 0u);

	internal static APTR QueryCustomClass(APTR library) =>
		APTR.FromPointer(QueryCustomClassCall(Entry(library, CustomClassQueryLvo),
			library, 0));


	// Negative-vector address arithmetic is confined to this ABI boundary.
	private static APTR Entry(APTR library, short lvo) =>
		APTR.FromPointer(unchecked(library.Raw - (uint)-lvo));

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint MakeClassCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] APTR classId,
		[M68kRegister(M68kRegister.A1)] APTR superId,
		[M68kRegister(M68kRegister.A2)] APTR superClass,
		[M68kRegister(M68kRegister.D0)] uint size,
		[M68kRegister(M68kRegister.D1)] uint flags);

	[AmigaIndirectCall(M68kRegister.A3)]
	private static extern void PointerVoidCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] APTR value);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint PointerResultCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] APTR value);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint NewObjectCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] APTR cls,
		[M68kRegister(M68kRegister.A1)] APTR classId,
		[M68kRegister(M68kRegister.A2)] APTR tags);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint QueryCustomClassCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.D0)] int selector);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint GetAttrCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.D0)] uint attribute,
		[M68kRegister(M68kRegister.A0)] APTR obj,
		[M68kRegister(M68kRegister.A1)] APTR storage);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern int ModifyIDCMPCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] APTR window,
		[M68kRegister(M68kRegister.D0)] uint flags);

	[AmigaIndirectCall(M68kRegister.A3)]
	private static extern void WindowPointerCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] APTR window,
		[M68kRegister(M68kRegister.A1)] APTR tags);

	[AmigaIndirectCall(M68kRegister.A3)]
	private static extern void WindowBoxCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] APTR window,
		[M68kRegister(M68kRegister.D0)] int left,
		[M68kRegister(M68kRegister.D1)] int top,
		[M68kRegister(M68kRegister.D2)] int width,
		[M68kRegister(M68kRegister.D3)] int height);

	[AmigaIndirectCall(M68kRegister.A3)]
	private static extern void WindowRefreshCompleteCall(
		[M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR library,
		[M68kRegister(M68kRegister.A0)] APTR window,
		[M68kRegister(M68kRegister.D0)] uint complete);

}
