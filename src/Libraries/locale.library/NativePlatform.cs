/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Locale;

public struct NativeLocalePlatform : ILocalePlatform
{
    public uint DosBase; // Explicit instance state, never SDK globals in the shared image.
    public byte Read8(uint a) => APTR.ReadUInt8(APTR.FromPointer(a), 0);
    public ushort Read16(uint a) => APTR.ReadUInt16(APTR.FromPointer(a), 0);
    public uint Read32(uint a) => APTR.ReadUInt32(APTR.FromPointer(a), 0);
    public void Write8(uint a, byte v) => APTR.WriteUInt8(APTR.FromPointer(a), 0, v);
    public void Write16(uint a, ushort v) => APTR.WriteUInt16(APTR.FromPointer(a), 0, v);
    public void Write32(uint a, uint v) => APTR.WriteUInt32(APTR.FromPointer(a), 0, v);
    public uint Allocate(uint bytes) => Exec.AllocVec(bytes, 0x10001).Raw;
    public void Free(uint a) { if (a != 0) Exec.FreeVec(APTR.FromPointer(a)); }
    public uint Literal(uint text) => LocaleText.Native(text);
    public void Error(int error) => LocaleDos.SetIoErr(DosBase, error);
    public int LastError() => LocaleDos.IoErr(DosBase);
    public uint CatalogLocations()
    {
        uint task = Read32(Read32(4) + (uint)ExecLayout.ExecBase.ThisTask);
        if (task == 0 || Read8(task + 8) != 13) return 3;
        uint window = Read32(task + (uint)DosLayout.Process.WindowPointer);
        int error = LocaleDos.IoErr(DosBase);
        Write32(task + (uint)DosLayout.Process.WindowPointer, uint.MaxValue);
        uint result = 0;
        uint program = LocaleDos.Lock(DosBase, CString.ToUInt32(CString.FromLiteral("PROGDIR:")), -2);
        if (program != 0) { result |= 1; LocaleDos.UnLock(DosBase, program); }
        uint locale = LocaleDos.Lock(DosBase, CString.ToUInt32(CString.FromLiteral("LOCALE:")), -2);
        if (locale != 0) { result |= 2; LocaleDos.UnLock(DosBase, locale); }
        Write32(task + (uint)DosLayout.Process.WindowPointer, window); Error(error);
        return result;
    }
    public uint LoadFile(uint name, out uint bytes)
    {
        bytes = 0;
        uint file = LocaleDos.Open(DosBase, name, 1005);
        if (file == 0) return 0;
        uint buffer = 0; int error = 0;
        if (LocaleDos.Seek(DosBase, file, 0, 1) < 0) error = LocaleDos.IoErr(DosBase);
        int length = error == 0 ? LocaleDos.Seek(DosBase, file, 0, -1) : -1;
        if (length <= 0) { if (error == 0) error = length < 0 ? LocaleDos.IoErr(DosBase) : 212; }
        else
        {
            buffer = Allocate((uint)length);
            if (buffer == 0) error = 103;
            else
            {
                uint total = 0;
                while (total < (uint)length)
                {
                    int n = LocaleDos.Read(DosBase, file, buffer + total, length - (int)total);
                    if (n <= 0) { error = n < 0 ? LocaleDos.IoErr(DosBase) : 212; break; }
                    total += (uint)n;
                }
                if (error == 0) bytes = (uint)length;
            }
        }
        LocaleDos.Close(DosBase, file);
        if (error != 0) { Free(buffer); Error(error); return 0; }
        return buffer;
    }
    public uint OpenLanguage(uint name) => Exec.OpenLibraryRaw(CString.FromPointer(name), 0).Raw;
    public void CloseLanguage(uint language) => Exec.CloseLibrary(APTR.FromPointer(language));
    public uint Language(uint language, uint function, uint d0, uint d1, uint a1, uint a2) =>
        NativeLocaleCallbacks.Language(APTR.FromPointer(function == uint.MaxValue ? language - 30 : language - 36 - function * 6),
            APTR.FromPointer(language), d0, d1, a1, a2);
    public uint Hook(uint hook, uint locale, uint character) =>
        NativeLocaleCallbacks.Hook(APTR.FromPointer(Read32(hook + 8)), hook, locale, character);
}

internal static class NativeLocaleCallbacks
{
    [AmigaIndirectCall(M68kRegister.A3)]
    [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint DosString([M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.D1)] int id);
    [AmigaIndirectCall(M68kRegister.A3)]
    [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint Language([M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A6)] APTR language,
        [M68kRegister(M68kRegister.D0)] uint d0, [M68kRegister(M68kRegister.D1)] uint d1,
        [M68kRegister(M68kRegister.A1)] uint a1, [M68kRegister(M68kRegister.A2)] uint a2);
    [AmigaIndirectCall(M68kRegister.A3)]
    [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint Hook([M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A0)] uint hook, [M68kRegister(M68kRegister.A2)] uint locale,
        [M68kRegister(M68kRegister.A1)] uint character);
}
