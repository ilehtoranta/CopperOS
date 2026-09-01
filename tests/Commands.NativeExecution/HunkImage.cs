using System.Buffers.Binary;
using System.Security.Cryptography;

namespace CopperOS.Commands.NativeExecution;

internal sealed record HunkImage(byte[] Code, string Sha256)
{
    // The resident compiler currently emits one code/constant hunk. Reject
    // other layouts until their sharing/relocation rules are qualified too.
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

        if (Long() != 0x3f3 || Long() != 0 || Long() != 1 || Long() != 0 || Long() != 0)
            throw new InvalidDataException("Expected one unnamed HUNK.");
        var allocationLongs = Long();
        if ((allocationLongs & 0xc0000000) != 0 || Long() != 0x3e9)
            throw new InvalidDataException("Expected ordinary HUNK_CODE memory.");
        var codeLongs = Long();
        if (codeLongs != allocationLongs || codeLongs == 0)
            throw new InvalidDataException("Unqualified code/BSS allocation layout.");
        var codeBytes = checked((int)codeLongs * 4);
        var codeOffset = offset;
        Skip(codeBytes);
        var code = image.AsSpan(codeOffset, codeBytes).ToArray();
        var relocated = new HashSet<uint>();
        while (true)
        {
            var record = Long();
            if (record == 0x3f2) break;
            if (record == 0x3ec)
            {
                while (true)
                {
                    var count = Long();
                    if (count == 0) break;
                    if (Long() != 0) throw new InvalidDataException("Relocation targets another hunk.");
                    for (var index = 0u; index < count; index++)
                    {
                        var address = Long();
                        if ((address & 1) != 0 || address > codeBytes - 4 || !relocated.Add(address))
                            throw new InvalidDataException("Invalid or repeated HUNK relocation.");
                        var field = code.AsSpan(checked((int)address), 4);
                        var relative = BinaryPrimitives.ReadUInt32BigEndian(field);
                        if (relative >= codeBytes) throw new InvalidDataException("Relocation target outside image.");
                        BinaryPrimitives.WriteUInt32BigEndian(field, checked(relative + loadAddress));
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
                    if (Long() >= codeBytes) throw new InvalidDataException("Symbol outside image.");
                }
            }
            else throw new InvalidDataException($"Unqualified HUNK record ${record:X8}.");
        }
        if (offset != image.Length) throw new InvalidDataException("Trailing HUNK bytes.");
        return new HunkImage(code, Convert.ToHexStringLower(SHA256.HashData(image)));
    }
}
