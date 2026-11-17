/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.MathTrans;

[AmigaLibrary("mathffp.library", AmigaLibraryBasePolicy.CallerProvided)]
internal static class NativeFfp
{
    [AmigaLvo(-30)] [return: M68kRegister(M68kRegister.D0)] internal static extern int Fix([M68kRegister(M68kRegister.A6)] uint b, [M68kRegister(M68kRegister.D0)] uint x);
    [AmigaLvo(-36)] [return: M68kRegister(M68kRegister.D0)] internal static extern uint Float([M68kRegister(M68kRegister.A6)] uint b, [M68kRegister(M68kRegister.D0)] int x);
    [AmigaLvo(-66)] [return: M68kRegister(M68kRegister.D0)] internal static extern uint Add([M68kRegister(M68kRegister.A6)] uint b, [M68kRegister(M68kRegister.D1)] uint a, [M68kRegister(M68kRegister.D0)] uint c);
    [AmigaLvo(-72)] [return: M68kRegister(M68kRegister.D0)] internal static extern uint Sub([M68kRegister(M68kRegister.A6)] uint b, [M68kRegister(M68kRegister.D1)] uint a, [M68kRegister(M68kRegister.D0)] uint c);
    [AmigaLvo(-78)] [return: M68kRegister(M68kRegister.D0)] internal static extern uint Mul([M68kRegister(M68kRegister.A6)] uint b, [M68kRegister(M68kRegister.D1)] uint a, [M68kRegister(M68kRegister.D0)] uint c);
    [AmigaLvo(-84)] [return: M68kRegister(M68kRegister.D0)] internal static extern uint Div([M68kRegister(M68kRegister.A6)] uint b, [M68kRegister(M68kRegister.D1)] uint a, [M68kRegister(M68kRegister.D0)] uint c);
}
public struct NativeArithmetic : IFfpArithmetic
{
    public uint Base;
    public uint Add(uint a, uint b) => NativeFfp.Add(Base, a, b);
    public uint Sub(uint a, uint b) => NativeFfp.Sub(Base, b, a);
    public uint Mul(uint a, uint b) => NativeFfp.Mul(Base, a, b);
    public uint Div(uint a, uint b) => NativeFfp.Div(Base, b, a);
    public uint Float(int a) => NativeFfp.Float(Base, a);
    public int Fix(uint a) => NativeFfp.Fix(Base, a);
}

public static class MathTransNativeLibrary
{
    public const uint PositiveBytes = 46, NegativeBytes = 126;
    private const uint Segment = 38, FfpBase = 42;
    private static uint Read32(uint a) => APTR.ReadUInt32(APTR.FromPointer(a), 0);
    private static ushort Read16(uint a) => APTR.ReadUInt16(APTR.FromPointer(a), 0);
    private static byte Read8(uint a) => APTR.ReadUInt8(APTR.FromPointer(a), 0);
    private static void Write32(uint a, uint v) => APTR.WriteUInt32(APTR.FromPointer(a), 0, v);
    private static void Write16(uint a, ushort v) => APTR.WriteUInt16(APTR.FromPointer(a), 0, v);
    private static void Write8(uint a, byte v) => APTR.WriteUInt8(APTR.FromPointer(a), 0, v);
    private static NativeArithmetic Bind(uint lib) => new NativeArithmetic { Base = Read32(lib + FfpBase) };
    public static uint LibraryImageEntry() => uint.MaxValue;

    [M68kExport("copperos.mathtrans.Init")] [return: M68kRegister(M68kRegister.D0)]
    public static uint Init([M68kRegister(M68kRegister.D0)] uint lib, [M68kRegister(M68kRegister.A0)] uint segment, [M68kRegister(M68kRegister.A6)] uint exec)
    {
        uint ffp = Exec.OpenLibraryRaw(CString.FromLiteral("mathffp.library"), 0).Raw;
        if (ffp == 0) { Exec.FreeMem(APTR.FromPointer(lib - NegativeBytes), PositiveBytes + NegativeBytes); return 0; }
        Write32(lib + Segment, segment); Write32(lib + FfpBase, ffp);
        Write8(lib + 8, 9); Write32(lib + 10, CString.ToUInt32(CString.FromLiteral("mathtrans.library")));
        Write8(lib + 14, 6); Write16(lib + 20, 40); Write16(lib + 22, 1);
        Write32(lib + 24, CString.ToUInt32(CString.FromLiteral(MathTransResidentMetadata.DevelopmentIdStringTail)));
        return lib;
    }
    [M68kExport("copperos.mathtrans.Open")] [return: M68kRegister(M68kRegister.D0)]
    public static uint Open([M68kRegister(M68kRegister.A6)] uint lib)
    { Exec.Forbid(); Write16(lib+32, (ushort)(Read16(lib+32)+1)); Write8(lib+14, (byte)(Read8(lib+14)&~8)); Exec.Permit(); return lib; }
    [M68kExport("copperos.mathtrans.Close")] [return: M68kRegister(M68kRegister.D0)]
    public static uint Close([M68kRegister(M68kRegister.A6)] uint lib)
    { Exec.Forbid(); ushort count = (ushort)(Read16(lib+32)-1); Write16(lib+32,count); bool remove = count == 0 && (Read8(lib+14)&8) != 0; Exec.Permit(); return remove ? Expunge(lib) : 0; }
    [M68kExport("copperos.mathtrans.Expunge")] [return: M68kRegister(M68kRegister.D0)]
    public static uint Expunge([M68kRegister(M68kRegister.A6)] uint lib)
    {
        Exec.Forbid(); if (Read16(lib+32) != 0) { Write8(lib+14,(byte)(Read8(lib+14)|8)); Exec.Permit(); return 0; }
        uint segment = Read32(lib+Segment), ffp = Read32(lib+FfpBase); Exec.Remove(APTR.FromPointer(lib)); Exec.Permit();
        Exec.CloseLibrary(APTR.FromPointer(ffp)); Exec.FreeMem(APTR.FromPointer(lib-NegativeBytes), PositiveBytes+NegativeBytes); return segment;
    }
    [M68kExport("copperos.mathtrans.Reserved")] [return: M68kRegister(M68kRegister.D0)] public static uint Reserved() => 0;

    [M68kExport("copperos.mathtrans.SPAtan")] [return: M68kRegister(M68kRegister.D0)] public static uint Atan([M68kRegister(M68kRegister.A6)] uint b,[M68kRegister(M68kRegister.D0)] uint x) { var p=Bind(b); return TranscendentalCore.Atan(ref p,x); }
    [M68kExport("copperos.mathtrans.SPSin")] [return: M68kRegister(M68kRegister.D0)] public static uint Sin([M68kRegister(M68kRegister.A6)] uint b,[M68kRegister(M68kRegister.D0)] uint x) { var p=Bind(b); return TranscendentalCore.Sin(ref p,x); }
    [M68kExport("copperos.mathtrans.SPCos")] [return: M68kRegister(M68kRegister.D0)] public static uint Cos([M68kRegister(M68kRegister.A6)] uint b,[M68kRegister(M68kRegister.D0)] uint x) { var p=Bind(b); return TranscendentalCore.Cos(ref p,x); }
    [M68kExport("copperos.mathtrans.SPTan")] [return: M68kRegister(M68kRegister.D0)] public static uint Tan([M68kRegister(M68kRegister.A6)] uint b,[M68kRegister(M68kRegister.D0)] uint x) { var p=Bind(b); return TranscendentalCore.Tan(ref p,x); }
    [M68kExport("copperos.mathtrans.SPSincos")] [return: M68kRegister(M68kRegister.D0)] public static uint Sincos([M68kRegister(M68kRegister.A6)] uint b,[M68kRegister(M68kRegister.D1)] uint dst,[M68kRegister(M68kRegister.D0)] uint x) { var p=Bind(b); Write32(dst,TranscendentalCore.Cos(ref p,x)); return TranscendentalCore.Sin(ref p,x); }
    [M68kExport("copperos.mathtrans.SPSinh")] [return: M68kRegister(M68kRegister.D0)] public static uint Sinh([M68kRegister(M68kRegister.A6)] uint b,[M68kRegister(M68kRegister.D0)] uint x) { var p=Bind(b); return TranscendentalCore.Sinh(ref p,x); }
    [M68kExport("copperos.mathtrans.SPCosh")] [return: M68kRegister(M68kRegister.D0)] public static uint Cosh([M68kRegister(M68kRegister.A6)] uint b,[M68kRegister(M68kRegister.D0)] uint x) { var p=Bind(b); return TranscendentalCore.Cosh(ref p,x); }
    [M68kExport("copperos.mathtrans.SPTanh")] [return: M68kRegister(M68kRegister.D0)] public static uint Tanh([M68kRegister(M68kRegister.A6)] uint b,[M68kRegister(M68kRegister.D0)] uint x) { var p=Bind(b); return TranscendentalCore.Tanh(ref p,x); }
    [M68kExport("copperos.mathtrans.SPExp")] [return: M68kRegister(M68kRegister.D0)] public static uint Exp([M68kRegister(M68kRegister.A6)] uint b,[M68kRegister(M68kRegister.D0)] uint x) { var p=Bind(b); return TranscendentalCore.Exp(ref p,x); }
    [M68kExport("copperos.mathtrans.SPLog")] [return: M68kRegister(M68kRegister.D0)] public static uint Log([M68kRegister(M68kRegister.A6)] uint b,[M68kRegister(M68kRegister.D0)] uint x) { var p=Bind(b); return TranscendentalCore.Log(ref p,x); }
    [M68kExport("copperos.mathtrans.SPPow")] [return: M68kRegister(M68kRegister.D0)] public static uint Pow([M68kRegister(M68kRegister.A6)] uint b,[M68kRegister(M68kRegister.D1)] uint exponent,[M68kRegister(M68kRegister.D0)] uint x) { var p=Bind(b); return TranscendentalCore.Pow(ref p,x,exponent); }
    [M68kExport("copperos.mathtrans.SPSqrt")] [return: M68kRegister(M68kRegister.D0)] public static uint Sqrt([M68kRegister(M68kRegister.A6)] uint b,[M68kRegister(M68kRegister.D0)] uint x) { var p=Bind(b); return TranscendentalCore.Sqrt(ref p,x); }
    [M68kExport("copperos.mathtrans.SPTieee")] [return: M68kRegister(M68kRegister.D0)] public static uint Tieee([M68kRegister(M68kRegister.D0)] uint x) { FfpConversions.TryToIeee(x,out uint result); return result; }
    [M68kExport("copperos.mathtrans.SPFieee")] [return: M68kRegister(M68kRegister.D0)] public static uint Fieee([M68kRegister(M68kRegister.D0)] uint x) { if (FfpConversions.TryFromIeee(x,out uint result)) return result; if ((x & 0x7fffffff) > 0x7f800000) return 0; return ((x >> 23) & 255) > 189 ? 0xffffff7fu | ((x >> 24) & 128) : 0; }
    [M68kExport("copperos.mathtrans.SPAsin")] [return: M68kRegister(M68kRegister.D0)] public static uint Asin([M68kRegister(M68kRegister.A6)] uint b,[M68kRegister(M68kRegister.D0)] uint x) { var p=Bind(b); return TranscendentalCore.Asin(ref p,x); }
    [M68kExport("copperos.mathtrans.SPAcos")] [return: M68kRegister(M68kRegister.D0)] public static uint Acos([M68kRegister(M68kRegister.A6)] uint b,[M68kRegister(M68kRegister.D0)] uint x) { var p=Bind(b); return TranscendentalCore.Acos(ref p,x); }
    [M68kExport("copperos.mathtrans.SPLog10")] [return: M68kRegister(M68kRegister.D0)] public static uint Log10([M68kRegister(M68kRegister.A6)] uint b,[M68kRegister(M68kRegister.D0)] uint x) { var p=Bind(b); return TranscendentalCore.Log10(ref p,x); }
}
