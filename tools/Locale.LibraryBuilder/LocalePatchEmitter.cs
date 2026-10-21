/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
using System.Buffers.Binary;
namespace CopperOS.Locale.LibraryBuilder;

// Only the ABI glue needs assembly: an on-stack Hook and writable DOS scratch.
internal static class LocalePatchEmitter
{
    internal const int Size = 174;
    internal static void Emit(byte[] code, int start, IDictionary<string, uint> exports, List<int> relocations)
    {
        int at = start;
        void Word(ushort v) { BinaryPrimitives.WriteUInt16BigEndian(code.AsSpan(at, 2), v); at += 2; }
        void Address(uint v) { relocations.Add(at); BinaryPrimitives.WriteUInt32BigEndian(code.AsSpan(at, 4), v); at += 4; }
        var labels = new Dictionary<string, int>(); var branches = new List<(int At, string Label)>();
        void Branch(ushort op, string label) { Word(op); branches.Add((at, label)); Word(0); }
        uint raw = exports["copperos.locale.LocRawDoFmt"];
        exports["copperos.locale.LocRawDoFmt"] = (uint)at;
        Word(0x48e7); Word(0x0030); // movem.l a2-a3,-(sp)
        Word(0x4fef); Word(0xffec); // lea -20(sp),sp
        Word(0x2f4a); Word(12); Word(0x2f4b); Word(16); // callback, data
        Word(0x2f7c); int callback = at; Address(0); Word(8); // hook.h_Entry
        Word(0x244f); // movea.l sp,a2
        Word(0x4eb9); Address(raw);
        Word(0x4fef); Word(20); Word(0x4cdf); Word(0x0c00); Word(0x4e75);
        BinaryPrimitives.WriteUInt32BigEndian(code.AsSpan(callback, 4), (uint)at);
        Word(0x2f0b); Word(0x2f0e); Word(0x2f08); // preserve a3,a6,hook
        Word(0x2009); // move.l a1,d0
        Word(0x2668); Word(16); Word(0x2268); Word(12); // h_Data -> a3, h_SubEntry -> a1
        Word(0x2c78); Word(4); // SysBase -> a6
        // MorphOS's register ABI also accepts STRING=0, SERIAL=1, COUNT=2.
        Word(0x2209); Branch(0x6700, "array");
        Word(0x5381); Branch(0x6700, "serial");
        Word(0x5381); Branch(0x6700, "count");
        Word(0x4e91); Branch(0x6000, "store");
        labels.Add("array", at); Word(0x16c0); Branch(0x6000, "store");
        labels.Add("serial", at); Word(0x4a00); Branch(0x6700, "store");
        Word(0x4eae); Word(0xfdfc); Branch(0x6000, "store");
        labels.Add("count", at); Word(0x5293);
        labels.Add("store", at);
        Word(0x205f); Word(0x214b); Word(16); // save updated callback a3 in hook
        Word(0x2c5f); Word(0x265f); Word(0x4e75);
        foreach (string name in new[] { "LocDateToStr", "LocStrToDate" })
        {
            string key = "copperos.locale." + name; uint target = exports[key]; exports[key] = (uint)at;
            Word(0x4fef); Word(0xffe8); Word(0x204f); // lea -24(sp),sp; movea.l sp,a0
            Word(0x4eb9); Address(target);
            Word(0x4fef); Word(24); Word(0x4e75);
        }
        uint prefs = exports["copperos.locale.LocalePrefsUpdate"]; exports["copperos.locale.LocalePrefsUpdate"] = (uint)at;
        Word(0x4fef); Word(0xffc0); Word(0x224f); // 64-byte language basename on stack, A1
        Word(0x4eb9); Address(prefs); Word(0x4fef); Word(64); Word(0x4e75);
        if (at != start + Size) throw new InvalidDataException("Patch ABI emitter size mismatch: " + (at - start));
        foreach (var branch in branches) BinaryPrimitives.WriteInt16BigEndian(code.AsSpan(branch.At, 2), checked((short)(labels[branch.Label] - branch.At)));
    }
}
