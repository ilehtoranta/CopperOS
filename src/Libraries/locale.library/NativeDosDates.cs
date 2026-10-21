/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
using Amiga;
using CopperSharp.Compiler;
namespace CopperOS.Locale;

// The 24-byte workspace is on the caller's stack. Ordinary DOS date calls
// never allocate memory, and nested calls have independent cursors.
internal struct NativeDosDatePlatform : ILocalePlatform
{
    public uint Context;
    private NativeLocalePlatform Memory => default;
    private NativeLocalePlatform Dos => new() { DosBase = Read32(Context + 20) };
    public byte Read8(uint a) => Memory.Read8(a);
    public ushort Read16(uint a) => Memory.Read16(a);
    public uint Read32(uint a) => Memory.Read32(a);
    public void Write8(uint a, byte v) => Memory.Write8(a, v);
    public void Write16(uint a, ushort v) => Memory.Write16(a, v);
    public void Write32(uint a, uint v) => Memory.Write32(a, v);
    public uint Allocate(uint n) => Memory.Allocate(n);
    public void Free(uint a) => Memory.Free(a);
    public uint Literal(uint n) => Memory.Literal(n);
    public uint LoadFile(uint a, out uint n) => Dos.LoadFile(a, out n);
    public uint CatalogLocations() => Dos.CatalogLocations();
    public void Error(int n) => Dos.Error(n);
    public int LastError() => Dos.LastError();
    public uint OpenLanguage(uint a) => Memory.OpenLanguage(a);
    public void CloseLanguage(uint a) => Memory.CloseLanguage(a);
    public uint Language(uint a, uint f, uint d0, uint d1, uint a1, uint a2) => Memory.Language(a, f, d0, d1, a1, a2);
    public uint Hook(uint hook, uint locale, uint c)
    {
        uint at = Read32(Context + 12); Write32(Context + 12, at + 1);
        if (Read32(Context + 16) != 0) return Read8(at);
        Write8(at, (byte)c); return 0;
    }
}

public static partial class LocaleNativeLibrary
{
    private static uint DosDatePattern(ref NativeDosDatePlatform p, uint l, uint f) =>
        f == 1 ? CString.ToUInt32(CString.FromLiteral("%y-%b-%d")) :
        f == 2 ? CString.ToUInt32(CString.FromLiteral("%m-%d-%y")) :
        f == 3 ? CString.ToUInt32(CString.FromLiteral("%d-%m-%y")) :
        f == 4 ? p.Read32(l + LocaleLayout.ShortDate) : CString.ToUInt32(CString.FromLiteral("%d-%b-%y"));
    private static uint DosTimePattern(ref NativeDosDatePlatform p, uint l, uint f) =>
        f == 4 ? p.Read32(l + LocaleLayout.ShortTime) : CString.ToUInt32(CString.FromLiteral("%H:%M:%S"));
    private static void DosText(ref NativeDosDatePlatform p, uint text, uint target)
    { byte c; do { c = p.Read8(text++); p.Write8(target++, c); } while (c != 0); }
    [M68kExport("copperos.locale.LocDateToStr")]
    [return: M68kRegister(M68kRegister.D0)]
    public static int LocDateToStr([M68kRegister(M68kRegister.A6)] uint library,
        [M68kRegister(M68kRegister.D1)] uint dt, [M68kRegister(M68kRegister.A0)] uint workspace)
    {
        var p = new NativeDosDatePlatform { Context = workspace };
        p.Write32(workspace + 20, p.Read32(library + DosBase)); p.Write32(workspace + 16, 0);
        uint days = p.Read32(dt), f = p.Read8(dt + 12), l = p.Read32(library + DefaultLocale);
        if ((int)days < 0 || p.Read32(dt + 4) >= 1440 || p.Read32(dt + 8) >= 3000) return 0;
        uint target = p.Read32(dt + 14);
        if (target != 0) DosText(ref p, LocaleCore.String(ref p, l, 1 + days % 7), target);
        target = p.Read32(dt + 18);
        if (target != 0)
        {
            uint id = 0;
            if ((p.Read8(dt + 13) & 1) != 0)
            {
                LocaleDos.DateStamp(p.Read32(workspace + 20), workspace);
                int delta = (int)p.Read32(workspace) - (int)days;
                if (delta <= 7) id = delta == -1 ? 49u : delta == 0 ? 48u : delta == 1 ? 47u : delta > 1 ? 1 + days % 7 : 50u;
            }
            if (id != 0) DosText(ref p, LocaleCore.String(ref p, l, id), target);
            else { p.Write32(workspace + 12, target); DateCore.Format(ref p, l, DosDatePattern(ref p, l, f), dt, 0); }
        }
        target = p.Read32(dt + 22);
        if (target != 0) { p.Write32(workspace + 12, target); DateCore.Format(ref p, l, DosTimePattern(ref p, l, f), dt, 0); }
        return 1;
    }
    [M68kExport("copperos.locale.LocStrToDate")]
    [return: M68kRegister(M68kRegister.D0)]
    public static int LocStrToDate([M68kRegister(M68kRegister.A6)] uint library,
        [M68kRegister(M68kRegister.D1)] uint dt, [M68kRegister(M68kRegister.A0)] uint workspace)
    {
        var p = new NativeDosDatePlatform { Context = workspace };
        p.Write32(workspace + 20, p.Read32(library + DosBase)); p.Write32(workspace + 16, 1);
        uint l = p.Read32(library + DefaultLocale), f = p.Read8(dt + 12), text = p.Read32(dt + 18);
        if (text != 0)
        {
            LocaleDos.DateStamp(p.Read32(workspace + 20), workspace);
            uint today = p.Read32(workspace); bool relative = false;
            for (uint id = 1; id <= 49; id++)
            {
                if (id == 8) id = 47;
                if (CharacterCore.Compare(ref p, l, text, LocaleCore.String(ref p, l, id), -1, 0) != 0) continue;
                int delta = id == 47 ? -1 : id == 48 ? 0 : id == 49 ? 1 : (int)(id - 1) - (int)(today % 7);
                if (id < 8) { if ((p.Read8(dt + 13) & 2) != 0) { if (delta <= 0) delta += 7; } else if (delta >= 0) delta -= 7; }
                p.Write32(dt, today + (uint)delta); relative = true; break;
            }
            if (!relative)
            {
                p.Write32(workspace + 12, text);
                if (!DateCore.Parse(ref p, l, workspace, DosDatePattern(ref p, l, f), 0, 9)) return 0;
                p.Write32(dt, p.Read32(workspace));
            }
        }
        text = p.Read32(dt + 22);
        if (text != 0)
        {
            p.Write32(workspace + 12, text);
            if (!DateCore.Parse(ref p, l, workspace, DosTimePattern(ref p, l, f), 0, 7)) return 0;
            p.Write32(dt + 4, p.Read32(workspace + 4)); p.Write32(dt + 8, p.Read32(workspace + 8));
        }
        return 1;
    }
}
