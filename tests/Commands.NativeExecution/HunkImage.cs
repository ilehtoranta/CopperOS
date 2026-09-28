using System.Buffers.Binary;
using System.Security.Cryptography;

namespace CopperOS.Commands.NativeExecution;

internal sealed record HunkImage(
    byte[] Code,
    string Sha256,
    IReadOnlyList<(int Offset, int Length)> ReadOnlyRanges,
    IReadOnlyList<(int Offset, int Length)> WritableRanges,
    byte[] UnrelocatedCode,
    IReadOnlyList<HunkRelocation> Relocations)
{
    // The native probes map all loaded hunks into one contiguous test image.
    // This supports the resident compiler's one-hunk output and ordinary
    // multi-hunk command binaries without pretending to model Exec allocation.
    public byte[] CreateLoadedImage(uint loadAddress)
    {
        var loaded = (byte[])UnrelocatedCode.Clone();
        foreach (var relocation in Relocations)
        {
            var targetAddress = checked(loadAddress + (uint)relocation.TargetOffset +
                relocation.TargetRelativeOffset);
            BinaryPrimitives.WriteUInt32BigEndian(
                loaded.AsSpan(relocation.FieldOffset, 4), targetAddress);
        }
        return loaded;
    }

    public static HunkImage Load(string path, uint loadAddress)
    {
        var image = File.ReadAllBytes(path);
        var offset = 0;
        uint Long()
        {
            if (offset > image.Length - 4) throw new InvalidDataException("Truncated HUNK.");
            var value = BinaryPrimitives.ReadUInt32BigEndian(image.AsSpan(offset, 4));
            offset += 4;
            return value;
        }
        void Skip(int bytes)
        {
            if (bytes < 0 || offset > image.Length - bytes)
                throw new InvalidDataException("Truncated HUNK record.");
            offset += bytes;
        }

        if (Long() != 0x3f3) throw new InvalidDataException("Expected HUNK_HEADER.");
        while (true)
        {
            var nameLongs = Long();
            if (nameLongs == 0) break;
            Skip(checked((int)nameLongs * 4));
        }
        var hunkCount = Long();
        var firstHunk = Long();
        var lastHunk = Long();
        if (hunkCount == 0 || lastHunk < firstHunk || lastHunk - firstHunk + 1 != hunkCount || hunkCount > 4096)
            throw new InvalidDataException("Invalid HUNK_HEADER table.");

        var allocationBytes = new int[hunkCount];
        for (var index = 0; index < allocationBytes.Length; index++)
        {
            var allocationLongs = Long() & 0x3fffffff;
            allocationBytes[index] = checked((int)allocationLongs * 4);
            if (allocationBytes[index] == 0)
                throw new InvalidDataException("Empty HUNK allocation is not qualified.");
        }

        var segments = new byte[hunkCount][];
        var writableSegments = new bool[hunkCount];
        var relocations = new List<(int Source, int Target, uint Address)>();
        var relocated = new HashSet<(int Source, uint Address)>();
        for (var source = 0; source < segments.Length; source++)
        {
            byte[]? segment = null;
            while (true)
            {
                var record = Long();
                if (record == 0x3f2) break;
                var type = record & 0x3fffffff;
                if (type is 0x3e9 or 0x3ea)
                {
                    if (segment is not null) throw new InvalidDataException("Multiple data records in one HUNK are not qualified.");
                    var dataLongs = Long() & 0x3fffffff;
                    var dataBytes = checked((int)dataLongs * 4);
                    if (dataBytes > allocationBytes[source])
                        throw new InvalidDataException("HUNK contents exceed its allocation.");
                    segment = new byte[allocationBytes[source]];
                    writableSegments[source] = type == 0x3ea;
                    var dataOffset = offset;
                    Skip(dataBytes);
                    image.AsSpan(dataOffset, dataBytes).CopyTo(segment);
                }
                else if (type == 0x3eb)
                {
                    if (segment is not null) throw new InvalidDataException("Multiple data records in one HUNK are not qualified.");
                    var bssLongs = Long() & 0x3fffffff;
                    if (checked((int)bssLongs * 4) > allocationBytes[source])
                        throw new InvalidDataException("HUNK BSS exceeds its allocation.");
                    segment = new byte[allocationBytes[source]];
                    writableSegments[source] = true;
                }
                else if (record == 0x3ec)
                {
                    while (true)
                    {
                        var count = Long();
                        if (count == 0) break;
                        var targetHunk = Long();
                        if (targetHunk < firstHunk || targetHunk > lastHunk)
                            throw new InvalidDataException("Relocation target is outside the HUNK table.");
                        var target = checked((int)(targetHunk - firstHunk));
                        for (var index = 0u; index < count; index++)
                        {
                            var address = Long();
                            if ((address & 1) != 0 || address > allocationBytes[source] - 4 || !relocated.Add((source, address)))
                                throw new InvalidDataException("Invalid or repeated HUNK relocation.");
                            relocations.Add((source, target, address));
                        }
                    }
                }
                else if (record == 0x3f0)
                {
                    while (true)
                    {
                        var nameLongs = Long();
                        if (nameLongs == 0) break;
                        Skip(checked((int)nameLongs * 4));
                        if (Long() > allocationBytes[source])
                            throw new InvalidDataException("Symbol lies outside its HUNK allocation.");
                    }
                }
                else if (record == 0x3f1)
                {
                    Skip(checked((int)Long() * 4));
                }
                else throw new InvalidDataException($"Unqualified HUNK record ${record:X8}.");
            }
            segments[source] = segment ?? throw new InvalidDataException("HUNK has no CODE, DATA, or BSS record.");
        }
        if (offset != image.Length) throw new InvalidDataException("Trailing HUNK bytes.");

        var segmentOffsets = new int[segments.Length];
        var totalBytes = 0;
        for (var index = 0; index < segments.Length; index++)
        {
            segmentOffsets[index] = totalBytes;
            totalBytes = checked(totalBytes + segments[index].Length);
        }
        var code = new byte[totalBytes];
        var readOnlyRanges = new List<(int Offset, int Length)>();
        var writableRanges = new List<(int Offset, int Length)>();
        for (var index = 0; index < segments.Length; index++)
        {
            segments[index].CopyTo(code, segmentOffsets[index]);
            var range = (segmentOffsets[index], segments[index].Length);
            (writableSegments[index] ? writableRanges : readOnlyRanges).Add(range);
        }
        var resolvedRelocations = new List<HunkRelocation>(relocations.Count);
        foreach (var (source, target, address) in relocations)
        {
            var field = code.AsSpan(checked(segmentOffsets[source] + (int)address), 4);
            var relative = BinaryPrimitives.ReadUInt32BigEndian(field);
            if (relative > segments[target].Length)
                throw new InvalidDataException("Relocation target offset lies outside its HUNK allocation.");
            resolvedRelocations.Add(new HunkRelocation(
                checked(segmentOffsets[source] + (int)address),
                segmentOffsets[target], relative));
        }
        if (readOnlyRanges.Count == 0 || readOnlyRanges[0].Offset != 0)
            throw new InvalidDataException("The entry HUNK must be read-only CODE.");
        var loaded = (byte[])code.Clone();
        foreach (var relocation in resolvedRelocations)
        {
            var targetAddress = checked(loadAddress + (uint)relocation.TargetOffset +
                relocation.TargetRelativeOffset);
            BinaryPrimitives.WriteUInt32BigEndian(
                loaded.AsSpan(relocation.FieldOffset, 4), targetAddress);
        }
        return new HunkImage(loaded, Convert.ToHexStringLower(SHA256.HashData(image)),
            readOnlyRanges, writableRanges, code, resolvedRelocations);
    }
}

internal sealed record HunkRelocation(
    int FieldOffset,
    int TargetOffset,
    uint TargetRelativeOffset);
