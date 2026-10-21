/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
using System.Buffers.Binary;

namespace CopperOS.Locale.LibraryBuilder;

/// <summary>
/// Pure 68000 GetCatalogStr vector. Uses the same immutable index as the C#
/// core, without constructing a compiler invocation context for a RAM lookup.
/// No absolute addresses or relocations. D2-D7/A2-A6 are preserved.
/// </summary>
internal static class LocaleLookupEmitter
{
    internal static byte[] Build()
    {
        var words = new List<ushort>();
        var labels = new Dictionary<string, int>(StringComparer.Ordinal);
        var branches = new List<(int Offset, string Label)>();
        void Emit(params ushort[] op) => words.AddRange(op);
        void Label(string name) => labels.Add(name, words.Count * 2);
        void Branch(ushort opcode, string target) { Emit(opcode, 0); branches.Add((words.Count * 2 - 2, target)); }

        Label("catalog");
        Emit(0x2208);                            // move.l a0,d1
        Branch(0x6700, "fallback");
        Emit(0x4aa8, (ushort)CatalogCore.Dense);  // tst.l Dense(a0)
        Branch(0x6700, "sparse");
        Emit(0x90a8, (ushort)CatalogCore.Minimum);// sub.l Minimum(a0),d0
        Emit(0xb0a8, (ushort)CatalogCore.Count);  // cmp.l Count(a0),d0
        Branch(0x6400, "fallback");             // bcc (unsigned out of range)
        Emit(0xe588);                            // lsl.l #2,d0
        Emit(0xd0a8, (ushort)CatalogCore.Index);  // add.l Index(a0),d0
        Emit(0x2040, 0x2010);                    // movea.l d0,a0; move.l (a0),d0
        Branch(0x6600, "done");
        Label("fallback");
        Emit(0x2009);                            // move.l a1,d0
        Label("done");
        Emit(0x4e75);                            // rts

        Label("sparse");
        Emit(0x2f09, 0x2f00);                    // save fallback and original ID
        Emit(0x48e7, 0x3820);                    // movem.l d2-d4/a2,-(sp)
        Emit(0x7400);                            // moveq #0,d2 (low)
        Emit(0x2628, (ushort)CatalogCore.Count);  // move.l Count(a0),d3 (high)
        Emit(0x2468, (ushort)CatalogCore.Index);  // movea.l Index(a0),a2
        Label("search");
        Emit(0xb483);                            // cmp.l d3,d2
        Branch(0x6400, "absent");
        Emit(0x2803, 0x9882, 0xe28c, 0xd882);    // d4 = low + ((high-low) >> 1)
        Emit(0x2204, 0xe789);                    // d1 = mid << 3
        Emit(0x43f2, 0x1800);                    // lea (a2,d1.l),a1
        Emit(0x2211, 0xb081);                    // move.l (a1),d1; cmp.l d1,d0
        Branch(0x6500, "high");                 // bcs (unsigned ID < key)
        Branch(0x6200, "low");                  // bhi
        Emit(0x2029, 0x0004);                    // move.l 4(a1),d0
        Emit(0x4cdf, 0x041c, 0x4a80);            // restore; tst.l d0
        Branch(0x6600, "stackDone");
        Branch(0x6000, "root");
        Label("high"); Emit(0x2604);           // high = mid
        Branch(0x6000, "search");
        Label("low"); Emit(0x2404, 0x5282);    // low = mid + 1
        Branch(0x6000, "search");
        Label("absent"); Emit(0x4cdf, 0x041c);
        Branch(0x6000, "root");
        Label("root");
        Emit(0x2068, (ushort)CatalogCore.Root);   // movea.l Root(a0),a0
        Emit(0x201f, 0x225f);                    // restore original ID and fallback
        Branch(0x6000, "catalog");
        Label("stackDone"); Emit(0x508f, 0x4e75);

        foreach (var branch in branches)
        {
            // Word Bcc displacements are relative to the extension-word PC.
            int distance = labels[branch.Label] - branch.Offset;
            words[branch.Offset / 2] = unchecked((ushort)checked((short)distance));
        }
        var bytes = new byte[words.Count * 2];
        for (int i = 0; i < words.Count; i++) BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(i * 2, 2), words[i]);
        return bytes;
    }
}
