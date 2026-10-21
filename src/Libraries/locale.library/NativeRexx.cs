/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
using Amiga;
using CopperSharp.Compiler;
namespace CopperOS.Locale;

public static partial class LocaleNativeLibrary
{
    // The packager wraps this export to return the Argstring in A0 as well as
    // the status in D0. A1 points to the wrapper's four-byte result slot.
    [M68kExport("copperos.locale.RexxHost")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint RexxHost([M68kRegister(M68kRegister.A6)] uint library,
        [M68kRegister(M68kRegister.A0)] uint message, [M68kRegister(M68kRegister.A1)] uint resultSlot)
    {
        var p = Bind(library); p.Write32(resultSlot, 0);
        var rexx = Exec.OpenLibraryRaw(CString.FromLiteral("rexxsyslib.library"), 0);
        if (rexx.IsNull) return 14;
        if (message == 0 || LocaleRexx.IsRexxMsg(rexx.Raw, message) == 0) { Exec.CloseLibrary(rexx); return 10; }
        Exec.ObtainSemaphore(APTR.FromPointer(library + Gate));
        p.Write32(library + Busy, 1);
        uint head = p.Read32(library + Catalogs);
        uint error = RexxCore.Evaluate(ref p, p.Read32(library + DefaultLocale), message, ref head, out uint result, out uint temporary);
        p.Write32(library + Catalogs, head);
        if (error == 0)
        {
            uint arg = LocaleRexx.CreateArgstring(rexx.Raw, result, LocaleMemory.Length(ref p, result));
            if (arg == 0) error = 3; else p.Write32(resultSlot, arg);
        }
        p.Free(temporary); p.Write32(library + Busy, 0); Exec.ReleaseSemaphore(APTR.FromPointer(library + Gate)); Exec.CloseLibrary(rexx);
        return error;
    }
}
