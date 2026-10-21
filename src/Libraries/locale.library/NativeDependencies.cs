/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
namespace CopperOS.Locale;

// These imports deliberately carry A6 explicitly. SDK manual-base setters
// would store shared state in the code hunk, which a disk library cannot own.
[AmigaLibrary("dos.library", AmigaLibraryBasePolicy.CallerProvided)]
internal static class LocaleDos
{
    [AmigaLvo(-900)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int SetVar([M68kRegister(M68kRegister.A6)] uint library,
        [M68kRegister(M68kRegister.D1)] uint name, [M68kRegister(M68kRegister.D2)] uint buffer,
        [M68kRegister(M68kRegister.D3)] int size, [M68kRegister(M68kRegister.D4)] uint flags);
    [AmigaLvo(-192)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint DateStamp([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.D1)] uint stamp);
    [AmigaLvo(-84)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint Lock([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.D1)] uint name, [M68kRegister(M68kRegister.D2)] int mode);
    [AmigaLvo(-90)]
    internal static extern void UnLock([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.D1)] uint file);
    [AmigaLvo(-30)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint Open([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.D1)] uint name, [M68kRegister(M68kRegister.D2)] int mode);
    [AmigaLvo(-36)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Close([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.D1)] uint file);
    [AmigaLvo(-42)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Read([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.D1)] uint file,
        [M68kRegister(M68kRegister.D2)] uint buffer, [M68kRegister(M68kRegister.D3)] int count);
    [AmigaLvo(-66)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Seek([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.D1)] uint file,
        [M68kRegister(M68kRegister.D2)] int offset, [M68kRegister(M68kRegister.D3)] int mode);
    [AmigaLvo(-132)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int IoErr([M68kRegister(M68kRegister.A6)] uint library);
    [AmigaLvo(-462)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int SetIoErr([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.D1)] int error);
}
[AmigaLibrary("rexxsyslib.library", AmigaLibraryBasePolicy.CallerProvided)]
internal static class LocaleRexx
{
    [AmigaLvo(-168)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int IsRexxMsg([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint message);
    [AmigaLvo(-126)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint CreateArgstring([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint text, [M68kRegister(M68kRegister.D0)] uint length);
}
