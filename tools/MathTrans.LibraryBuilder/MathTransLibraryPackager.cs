/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Buffers.Binary;
using System.Text;
using Amiga;
using CopperSharp.Compiler;

namespace CopperOS.MathTrans.LibraryBuilder;

/// <summary>Offsets in the single, unrelocated code hunk, not file offsets.</summary>
public readonly record struct MathTransLibraryImageLayout(
    int Resident, int AutoInit, int FunctionTable, int Name, int IdString,
    int CompiledCode, int EndMarker);

public sealed record MathTransLibraryArtifact(byte[] Image, byte[] Code,
    MathTransLibraryImageLayout Layout, IReadOnlyList<int> Relocations,
    IReadOnlyDictionary<string, uint> Exports);

/// <summary>
/// Host-only packaging boundary. The compiled image is linked at zero; moving
/// it after the descriptor shifts absolute addends as well as relocation sites.
/// Exec structures are serialized by the SDK, not hand-written field layouts.
/// </summary>
public static class MathTransLibraryPackager
{
    public static IReadOnlyList<string> FunctionTableExports { get; } = Array.AsReadOnly(new[]
    {
        "copperos.mathtrans.Open", "copperos.mathtrans.Close", "copperos.mathtrans.Expunge", "copperos.mathtrans.Reserved",
        "copperos.mathtrans.SPAtan", "copperos.mathtrans.SPSin", "copperos.mathtrans.SPCos", "copperos.mathtrans.SPTan",
        "copperos.mathtrans.SPSincos", "copperos.mathtrans.SPSinh", "copperos.mathtrans.SPCosh", "copperos.mathtrans.SPTanh",
        "copperos.mathtrans.SPExp", "copperos.mathtrans.SPLog", "copperos.mathtrans.SPPow", "copperos.mathtrans.SPSqrt",
        "copperos.mathtrans.SPTieee", "copperos.mathtrans.SPFieee", "copperos.mathtrans.SPAsin", "copperos.mathtrans.SPAcos", "copperos.mathtrans.SPLog10",
    });
    public static IReadOnlyList<string> ExportNames { get; } = Array.AsReadOnly(FunctionTableExports.Append("copperos.mathtrans.Init").ToArray());
    public static MathTransLibraryArtifact Build(byte[] linkedCode,
        IReadOnlyList<M68kRelocation> relocations, IReadOnlyList<M68kSymbol> symbols)
    {
        ArgumentNullException.ThrowIfNull(linkedCode);
        ArgumentNullException.ThrowIfNull(relocations);
        ArgumentNullException.ThrowIfNull(symbols);
        if (linkedCode.Length < 4 || (linkedCode.Length & 1) != 0)
            throw new InvalidDataException("Compiled code must contain an even-sized native image.");
        if (MathTransNativeLibrary.NegativeBytes !=
            FunctionTableExports.Count * MathTransResidentMetadata.VectorStride)
            throw new InvalidDataException("The production negative size does not match the complete vector table.");

        var name = Ascii(MathTransResidentMetadata.DevelopmentNameText);
        var id = Ascii(MathTransResidentMetadata.DevelopmentIdString);
        const int residentOffset = 4; // The only executable entry before metadata is MOVEQ #-1,D0; RTS.
        var autoInitOffset = Align4(checked(residentOffset + (int)Resident.Size));
        var tableOffset = checked(autoInitOffset + (int)ResidentAutoInit.Size);
        var nameOffset = checked(tableOffset +
            (FunctionTableExports.Count + 1) * sizeof(uint));
        var idOffset = checked(nameOffset + name.Length);
        var codeOffset = Align4(checked(idOffset + id.Length));
        // Public entry thunks restore N/Z from the returned FFP sign and zero.
        // Generated C# epilogues cannot promise any condition-code result.
        var thunkOffset = checked(codeOffset + linkedCode.Length);
        const int thunkSize = 128;
        var endOffset = checked(thunkOffset + 17 * thunkSize);
        var code = new byte[Align4(checked(endOffset + sizeof(uint)))];
        var layout = new MathTransLibraryImageLayout(residentOffset, autoInitOffset,
            tableOffset, nameOffset, idOffset, codeOffset, endOffset);
        code[0] = 0x70;
        code[1] = 0xFF;
        code[2] = 0x4E;
        code[3] = 0x75;
        linkedCode.CopyTo(code, codeOffset);
        name.CopyTo(code, nameOffset);
        id.CopyTo(code, idOffset);

        var exports = new SortedDictionary<string, uint>(StringComparer.Ordinal);
        foreach (var export in ExportNames)
        {
            var matches = symbols.Where(symbol => symbol.Name == export).ToArray();
            if (matches.Length != 1 || matches[0].Address >= linkedCode.Length ||
                (matches[0].Address & 1) != 0)
                throw new InvalidDataException($"Missing, ambiguous, or invalid ABI export: {export}");
            exports.Add(export, checked(matches[0].Address + (uint)codeOffset));
        }

        var outputRelocations = new List<int>();
        for (int index = 0; index < 17; index++)
        {
            string exportName = FunctionTableExports[index + 4];
            int offset = thunkOffset + index * thunkSize;
            bool log = exportName == "copperos.mathtrans.SPLog";
            bool sqrt = exportName == "copperos.mathtrans.SPSqrt";
            bool pow = exportName == "copperos.mathtrans.SPPow";
            bool trig = exportName is "copperos.mathtrans.SPSin" or "copperos.mathtrans.SPCos" or "copperos.mathtrans.SPTan" or "copperos.mathtrans.SPSincos";
            bool sincos = exportName == "copperos.mathtrans.SPSincos";
            bool inverse = exportName is "copperos.mathtrans.SPAsin" or "copperos.mathtrans.SPAcos";
            bool fieee = exportName == "copperos.mathtrans.SPFieee";
            bool clearFlags = inverse || exportName == "copperos.mathtrans.SPLog10";
            bool tanh = exportName == "copperos.mathtrans.SPTanh";
            bool saturation = exportName is "copperos.mathtrans.SPExp" or "copperos.mathtrans.SPSinh" or "copperos.mathtrans.SPCosh" or "copperos.mathtrans.SPPow";
            var bytes = new List<byte>();
            void Emit(params byte[] value) => bytes.AddRange(value);
            if (trig)
            {
                Emit(0x2f,0x01,0x22,0x00); // save D1; copy input
                Emit(0x02,0x81,0,0,0,0x7f,0x0c,0x81,0,0,0,85); // exponent versus 85
                Emit(0x65,0); int normalBranch=bytes.Count-1; // BCS normal
                Emit(0x22,0x1f,0xe3,0x08); // restore D1; LSL.B #1,D0
                if (sincos) Emit(0x20,0x41,0x20,0x80,0x20,0x06,0x4e,0x75); // write cosine; return incoming D6
                else Emit(0x02,0x3c,0x00,0xf1,0x00,0x3c,0x00,0x02,0x4e,0x75);
                int normal=bytes.Count; bytes[normalBranch]=checked((byte)(normal-normalBranch-1));
                Emit(0x22,0x1f); // restore D1
            }
            if (log || sqrt || pow || fieee) Emit(0x2f, 0x00); // MOVE.L D0,-(SP)
            Emit(0x4e, 0xb9, 0, 0, 0, 0); // JSR absolute
            int target = bytes.Count - 4;
            var branches = new List<int>();
            int logExponentBranch = -1;
            if (log || sqrt || pow)
            {
                Emit(0x22, 0x1f); // MOVE.L (SP)+,D1
                if (log)
                {
                    Emit(0x20,0x41,0x02,0x81,0,0,0,0x7f); // preserve input in A0; isolate exponent
                    Emit(0x0c,0x81,0,0,0,65,0x67,0); logExponentBranch=bytes.Count-1;
                    Emit(0x22,0x08); // MOVE.L A0,D1
                }
                if (log) { Emit(0x4a, 0x81, 0x67, 0); branches.Add(bytes.Count - 1); } // TST.L; BEQ
                Emit(0x08, 0x01, 0x00, 0x07, 0x66, 0); branches.Add(bytes.Count - 1); // BTST #7; BNE
            }
            if (fieee)
            {
                Emit(0x22,0x1f,0x02,0x81,0x7f,0xff,0xff,0xff); // restore input; clear sign
                Emit(0x0c,0x81,0x5f,0,0,0,0x65,0); int below=bytes.Count-1;
                Emit(0x0c,0x81,0x7f,0x80,0,0,0x62,0); int nan=bytes.Count-1;
                Emit(0x60,0); branches.Add(bytes.Count-1); // BRA overflow
                int normal=bytes.Count; bytes[below]=checked((byte)(normal-below-1)); bytes[nan]=checked((byte)(normal-nan-1));
            }
            if (saturation)
            {
                Emit(0x22, 0x00, 0x08, 0x81, 0x00, 0x07); // D1 = D0; BCLR #7,D1
                Emit(0x0c, 0x81, 0xff, 0xff, 0xff, 0x7f, 0x67, 0); branches.Add(bytes.Count - 1);
            }
            if (inverse)
            { Emit(0x0c,0x80,0xff,0xff,0xff,0xff,0x67,0); branches.Add(bytes.Count-1); }
            void Flags()
            {
                if (clearFlags) Emit(0x02,0x3c,0x00,0xf0); // Workbench 3.1 reference leaves NZVC clear
                else if (tanh) Emit(0x02,0x3c,0x00,0xf0,0x00,0x3c,0x00,0x04);
                else if (sincos || exportName == "copperos.mathtrans.SPTieee") Emit(0x4a, 0x80); // TST.L D0
                else Emit(0x22, 0x00, 0xe0, 0x99); // MOVE.L D0,D1; ROR.L #8,D1
            }
            Flags(); Emit(0x4e, 0x75);
            if (branches.Count != 0)
            {
                int bad = bytes.Count;
                foreach (int branch in branches) bytes[branch] = checked((byte)(bad - branch - 1));
                Flags(); Emit(0x00, 0x3c, 0x00, 0x02, 0x4e, 0x75); // ORI #2,CCR; RTS
            }
            if (logExponentBranch >= 0)
            {
                int exponentPath=bytes.Count; bytes[logExponentBranch]=checked((byte)(exponentPath-logExponentBranch-1));
                Emit(0x22,0x08,0x08,0x01,0,7,0x66,0); int negativeBranch=bytes.Count-1;
                Flags(); Emit(0x00,0x3c,0,4,0x4e,0x75);
                int negativePath=bytes.Count; bytes[negativeBranch]=checked((byte)(negativePath-negativeBranch-1));
                Flags(); Emit(0x00,0x3c,0,6,0x4e,0x75);
            }
            if (bytes.Count > thunkSize) throw new InvalidDataException("Math condition-code thunk is too large.");
            bytes.CopyTo(code, offset);
            BinaryPrimitives.WriteUInt32BigEndian(code.AsSpan(offset + target, 4), exports[exportName]);
            outputRelocations.Add(offset + target);
            exports[exportName] = (uint)offset;
        }
        var previous = -4;
        foreach (var relocation in relocations.OrderBy(item => item.Offset))
        {
            var offset = relocation.Offset;
            if (offset < 0 || (offset & 1) != 0 || offset > linkedCode.Length - 4 ||
                offset < previous + 4)
                throw new InvalidDataException($"Invalid, overlapping, or duplicate relocation at {offset}.");
            var addend = BinaryPrimitives.ReadUInt32BigEndian(linkedCode.AsSpan(offset, 4));
            if (addend >= linkedCode.Length)
                throw new InvalidDataException($"Relocation at {offset} targets outside the linked code image.");
            var outputOffset = checked(codeOffset + offset);
            BinaryPrimitives.WriteUInt32BigEndian(code.AsSpan(outputOffset, 4),
                checked(addend + (uint)codeOffset));
            outputRelocations.Add(outputOffset);
            previous = offset;
        }

        var memory = new ImageMemory(code);
        var resident = new Resident
        {
            MatchWord = 0x4AFC,
            MatchTag = APTR.FromPointer((uint)residentOffset),
            EndSkip = APTR.FromPointer((uint)endOffset),
            Flags = ResidentFlags.AutoInit,
            Version = checked((byte)MathTransResidentMetadata.DevelopmentVersion),
            Type = (byte)NodeType.Library,
            Name = STRPTR.FromPointer((uint)nameOffset),
            IdString = STRPTR.FromPointer((uint)idOffset),
            Init = APTR.FromPointer((uint)autoInitOffset),
        };
        var autoInit = new ResidentAutoInit
        {
            DataSize = MathTransNativeLibrary.PositiveBytes,
            FunctionTable = APTR.FromPointer((uint)tableOffset),
            StructureTable = APTR.Null,
            InitFunction = APTR.FromPointer(exports["copperos.mathtrans.Init"]),
        };
        ExecResidentCodec.Write(ref memory, resident.MatchTag, resident);
        ExecResidentAutoInitCodec.Write(ref memory, resident.Init, autoInit);
        for (var index = 0; index < FunctionTableExports.Count; index++)
        {
            var offset = tableOffset + index * sizeof(uint);
            memory.WriteUInt32(APTR.FromPointer((uint)offset), 0,
                exports[FunctionTableExports[index]]);
            outputRelocations.Add(offset);
        }
        memory.WriteUInt32(autoInit.FunctionTable,
            FunctionTableExports.Count * sizeof(uint), uint.MaxValue);

        // Pointer-field locations belong solely to this container relocation
        // boundary. Their definitions, like the records above, come from the SDK.
        foreach (var field in new[] { ExecLayout.Resident.MatchTag, ExecLayout.Resident.EndSkip,
            ExecLayout.Resident.Name, ExecLayout.Resident.IdString, ExecLayout.Resident.Init })
            outputRelocations.Add(residentOffset + field);
        outputRelocations.Add(autoInitOffset + ExecLayout.ResidentAutoInit.FunctionTable);
        outputRelocations.Add(autoInitOffset + ExecLayout.ResidentAutoInit.InitFunction);
        outputRelocations.Sort();
        return new MathTransLibraryArtifact(WriteHunk(code, outputRelocations), code, layout,
            outputRelocations.AsReadOnly(), exports);
    }

    private static byte[] WriteHunk(byte[] code, IReadOnlyList<int> relocations)
    {
        // Exactly one CODE hunk. No executable startup is needed to initialize it.
        using var output = new MemoryStream();
        void Long(uint value)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
            output.Write(bytes);
        }
        Long(0x3F3); // HUNK_HEADER
        Long(0); // No resident-name string table (unrelated to Exec Resident records).
        Long(1);
        Long(0);
        Long(0);
        Long(checked((uint)code.Length / 4));
        Long(0x3E9); // HUNK_CODE
        Long(checked((uint)code.Length / 4));
        output.Write(code);
        Long(0x3EC); // HUNK_RELOC32
        Long(checked((uint)relocations.Count));
        Long(0); // All addresses are relative to this same hunk.
        foreach (var offset in relocations) Long(checked((uint)offset));
        Long(0);
        Long(0x3F2); // HUNK_END
        return output.ToArray();
    }

    private static int Align4(int value) => checked(value + 3) & ~3;
    private static byte[] Ascii(string value)
    {
        if (value.Any(character => character is '\0' or > '\x7F'))
            throw new InvalidDataException("Resident text must be non-NUL ASCII.");
        return Encoding.ASCII.GetBytes(value + '\0');
    }

    private readonly struct ImageMemory(byte[] bytes) : IAmigaGuestMemory
    {
        private Span<byte> Slice(APTR address, int offset, int count) =>
            bytes.AsSpan(checked((int)address.Raw + offset), count);
        public bool IsMapped(APTR address, uint byteSize) => byteSize != 0 &&
            (ulong)address.Raw + byteSize <= (uint)bytes.Length;
        public byte ReadUInt8(APTR address, int offset = 0) => Slice(address, offset, 1)[0];
        public ushort ReadUInt16(APTR address, int offset = 0) =>
            BinaryPrimitives.ReadUInt16BigEndian(Slice(address, offset, 2));
        public uint ReadUInt32(APTR address, int offset = 0) =>
            BinaryPrimitives.ReadUInt32BigEndian(Slice(address, offset, 4));
        public void WriteUInt8(APTR address, int offset, byte value) => Slice(address, offset, 1)[0] = value;
        public void WriteUInt16(APTR address, int offset, ushort value) =>
            BinaryPrimitives.WriteUInt16BigEndian(Slice(address, offset, 2), value);
        public void WriteUInt32(APTR address, int offset, uint value) =>
            BinaryPrimitives.WriteUInt32BigEndian(Slice(address, offset, 4), value);
        public void Clear(APTR address, uint byteCount) => Slice(address, 0, checked((int)byteCount)).Clear();
        public void Copy(APTR source, APTR destination, uint byteCount) =>
            Slice(source, 0, checked((int)byteCount)).CopyTo(Slice(destination, 0, checked((int)byteCount)));
    }
}
