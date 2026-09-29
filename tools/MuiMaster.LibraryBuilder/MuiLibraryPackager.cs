/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Amiga;
using CopperSharp.Compiler;

namespace CopperOS.MuiMaster.LibraryBuilder;

/// <summary>Offsets in the single, unrelocated code hunk, not file offsets.</summary>
public readonly record struct MuiLibraryImageLayout(
    int Resident, int AutoInit, int FunctionTable, int Name, int IdString,
    int CompiledCode, int EndMarker);

public sealed record MuiLibraryArtifact(byte[] Image, byte[] Code,
    MuiLibraryImageLayout Layout, IReadOnlyList<int> Relocations,
    IReadOnlyDictionary<string, uint> Exports);

/// <summary>
/// Host-only packaging boundary. The compiled image is linked at zero; moving
/// it after the descriptor shifts absolute addends as well as relocation sites.
/// Exec structures are serialized by the SDK, not hand-written field layouts.
/// </summary>
public static class MuiLibraryPackager
{
	// Distinct symbols compiled into the image. Unsupported public slots share
	// one typed entrypoint; the table itself is expanded separately below so
	// every MorphOS LVO retains its documented position.
    public static IReadOnlyList<string> ExportNames { get; } = Array.AsReadOnly(new[]
    {
        MuiNativeLibraryEntrypoints.InitExport,
        MuiNativeLibraryEntrypoints.OpenExport,
        MuiNativeLibraryEntrypoints.CloseExport,
        MuiNativeLibraryEntrypoints.ExpungeExport,
        MuiNativeLibraryEntrypoints.ReservedExport,
        MuiNativeLibraryEntrypoints.UnsupportedExport,
        MuiNativeLibraryEntrypoints.GetClassExport,
        MuiNativeLibraryEntrypoints.FreeClassExport,
        MuiNativeLibraryEntrypoints.RequestIDCMPExport,
        MuiNativeLibraryEntrypoints.RejectIDCMPExport,
        MuiNativeLibraryEntrypoints.CreateCustomClassExport,
        MuiNativeLibraryEntrypoints.DeleteCustomClassExport,
        MuiNativeLibraryEntrypoints.NewObjectAExport,
        MuiNativeLibraryEntrypoints.DisposeObjectExport,
        MuiNativeLibraryEntrypoints.MakeObjectAExport,
        MuiNativeLibraryEntrypoints.ErrorExport,
        MuiNativeLibraryEntrypoints.SetErrorExport,
        MuiNativeLibraryEntrypoints.AllocAslRequestExport,
        MuiNativeLibraryEntrypoints.AslRequestExport,
        MuiNativeLibraryEntrypoints.FreeAslRequestExport,
        MuiNativeLibraryEntrypoints.RequestAExport,
        MuiNativeLibraryEntrypoints.RequestObjectAExport,
        MuiNativeLibraryEntrypoints.RedrawExport,
        MuiNativeLibraryEntrypoints.LayoutExport,
        MuiNativeLibraryEntrypoints.ObtainPenExport,
        MuiNativeLibraryEntrypoints.ReleasePenExport,
        MuiNativeLibraryEntrypoints.AddClippingExport,
        MuiNativeLibraryEntrypoints.RemoveClippingExport,
        MuiNativeLibraryEntrypoints.AddClipRegionExport,
        MuiNativeLibraryEntrypoints.RemoveClipRegionExport,
        MuiNativeLibraryEntrypoints.BeginRefreshExport,
        MuiNativeLibraryEntrypoints.EndRefreshExport,
        MuiNativeLibraryEntrypoints.GetRGBColorExport,
        MuiNativeClassDispatcher.ExportName,
    });

	public static IReadOnlyList<string> FunctionTableExports { get; } =
		BuildFunctionTableExports();

    public static MuiLibraryArtifact Build(byte[] linkedCode,
        IReadOnlyList<M68kRelocation> relocations, IReadOnlyList<M68kSymbol> symbols)
    {
        ArgumentNullException.ThrowIfNull(linkedCode);
        ArgumentNullException.ThrowIfNull(relocations);
        ArgumentNullException.ThrowIfNull(symbols);
        if (linkedCode.Length < 4 || (linkedCode.Length & 1) != 0)
            throw new InvalidDataException("Compiled code must contain an even-sized native image.");
        if (MuiNativeLibraryEntrypoints.NegativeBytes !=
            FunctionTableExports.Count * MuiResidentMetadata.VectorStride)
            throw new InvalidDataException("The production negative size does not match the complete vector table.");

        var name = Ascii(MuiResidentMetadata.DevelopmentNameText);
        var id = Ascii(string.Create(CultureInfo.InvariantCulture,
            $"CopperOS MUI development {MuiResidentMetadata.DevelopmentVersion}.{MuiResidentMetadata.DevelopmentRevision}"));
        const int residentOffset = 4; // The only executable entry before metadata is MOVEQ #-1,D0; RTS.
        var autoInitOffset = Align4(checked(residentOffset + (int)Resident.Size));
        var tableOffset = checked(autoInitOffset + (int)ResidentAutoInit.Size);
        var nameOffset = checked(tableOffset +
            (FunctionTableExports.Count + 1) * sizeof(uint));
        var idOffset = checked(nameOffset + name.Length);
        var codeOffset = Align4(checked(idOffset + id.Length));
        var endOffset = checked(codeOffset + linkedCode.Length);
        var code = new byte[Align4(checked(endOffset + sizeof(uint)))];
        var layout = new MuiLibraryImageLayout(residentOffset, autoInitOffset,
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
            Version = checked((byte)MuiResidentMetadata.DevelopmentVersion),
            Type = (byte)NodeType.Library,
            Name = STRPTR.FromPointer((uint)nameOffset),
            IdString = STRPTR.FromPointer((uint)idOffset),
            Init = APTR.FromPointer((uint)autoInitOffset),
        };
        var autoInit = new ResidentAutoInit
        {
            DataSize = MuiNativeLibraryEntrypoints.PositiveBytes,
            FunctionTable = APTR.FromPointer((uint)tableOffset),
            StructureTable = APTR.Null,
            InitFunction = APTR.FromPointer(exports[MuiNativeLibraryEntrypoints.InitExport]),
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
        return new MuiLibraryArtifact(WriteHunk(code, outputRelocations), code, layout,
            outputRelocations.AsReadOnly(), exports);
    }

	private static IReadOnlyList<string> BuildFunctionTableExports()
	{
		var result = new string[MuiResidentMetadata.FunctionTableEntryCount];
		result[0] = MuiNativeLibraryEntrypoints.OpenExport;
		result[1] = MuiNativeLibraryEntrypoints.CloseExport;
		result[2] = MuiNativeLibraryEntrypoints.ExpungeExport;
		result[3] = MuiNativeLibraryEntrypoints.ReservedExport;
		for (var index = MuiResidentMetadata.ManagementVectorCount;
			index < result.Length; index++)
		{
			var lvo = MuiResidentMetadata.FirstLvo +
				(index - MuiResidentMetadata.ManagementVectorCount) * -MuiResidentMetadata.VectorStride;
			result[index] = lvo switch
			{
				-78 => MuiNativeLibraryEntrypoints.GetClassExport,
				-84 => MuiNativeLibraryEntrypoints.FreeClassExport,
				-90 => MuiNativeLibraryEntrypoints.RequestIDCMPExport,
				-96 => MuiNativeLibraryEntrypoints.RejectIDCMPExport,
				-108 => MuiNativeLibraryEntrypoints.CreateCustomClassExport,
				-114 => MuiNativeLibraryEntrypoints.DeleteCustomClassExport,
				-30 => MuiNativeLibraryEntrypoints.NewObjectAExport,
				-36 => MuiNativeLibraryEntrypoints.DisposeObjectExport,
				-66 => MuiNativeLibraryEntrypoints.ErrorExport,
				-72 => MuiNativeLibraryEntrypoints.SetErrorExport,
				-42 => MuiNativeLibraryEntrypoints.RequestAExport,
				-756 => MuiNativeLibraryEntrypoints.RequestObjectAExport,
				-120 => MuiNativeLibraryEntrypoints.MakeObjectAExport,
				-48 => MuiNativeLibraryEntrypoints.AllocAslRequestExport,
				-54 => MuiNativeLibraryEntrypoints.AslRequestExport,
				-60 => MuiNativeLibraryEntrypoints.FreeAslRequestExport,
				-102 => MuiNativeLibraryEntrypoints.RedrawExport,
				-126 => MuiNativeLibraryEntrypoints.LayoutExport,
				-156 => MuiNativeLibraryEntrypoints.ObtainPenExport,
				-162 => MuiNativeLibraryEntrypoints.ReleasePenExport,
				-168 => MuiNativeLibraryEntrypoints.AddClippingExport,
				-174 => MuiNativeLibraryEntrypoints.RemoveClippingExport,
				-180 => MuiNativeLibraryEntrypoints.AddClipRegionExport,
				-186 => MuiNativeLibraryEntrypoints.RemoveClipRegionExport,
				-192 => MuiNativeLibraryEntrypoints.BeginRefreshExport,
				-198 => MuiNativeLibraryEntrypoints.EndRefreshExport,
				-690 => MuiNativeLibraryEntrypoints.GetRGBColorExport,
				_ => MuiNativeLibraryEntrypoints.UnsupportedExport,
			};
		}
		return Array.AsReadOnly(result);
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
