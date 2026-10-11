using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;

static class Bind
{
    public static void Run(string directory, string output)
    {
        var records = new List<object>();
        foreach (var name in new[] { "CopperSharp.Compiler.NativeOsRunner", "CopperSharp.Compiler.Tests.NativeOsFixture", "CopperMod.Amiga.Lightweight" })
        {
            var dll = Path.Combine(directory, name + ".dll");
            var pdb = Path.Combine(directory, name + ".pdb");
            using var pe = new PEReader(File.OpenRead(dll));
            var entries = pe.ReadDebugDirectory();
            var checksumEntry = entries.Single(e => e.Type == DebugDirectoryEntryType.PdbChecksum);
            var expected = pe.ReadPdbChecksumDebugDirectoryData(checksumEntry);
            var pdbBytes = File.ReadAllBytes(pdb);
            // Bind the portable PDB identity to the executed module's CodeView ID.
            using var provider = MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(pdb));
            var reader = provider.GetMetadataReader();
            var codeView = pe.ReadCodeViewDebugDirectoryData(entries.Single(e => e.Type == DebugDirectoryEntryType.CodeView));
            var id = reader.DebugMetadataHeader!.Id.ToArray();
            if (new Guid(id.AsSpan(0, 16)) != codeView.Guid) throw new InvalidOperationException("PDB ID mismatch");
            var documents = new List<object>();
            foreach (var handle in reader.Documents)
            {
                var document = reader.GetDocument(handle);
                var path = reader.GetString(document.Name);
                var hash = reader.GetBlobBytes(document.Hash);
                var algorithm = reader.GetGuid(document.HashAlgorithm);
                var exists = File.Exists(path);
                var actual = exists ? algorithm == new Guid("8829d00f-11b8-4213-878b-770e8597ac16")
                    ? SHA256.HashData(File.ReadAllBytes(path)) : SHA1.HashData(File.ReadAllBytes(path)) : [];
                var matches = exists && hash.SequenceEqual(actual);
                documents.Add(new { path, exists, matches, checksum = Convert.ToHexString(hash) });
            }
            records.Add(new { name, dllSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dll))),
                pdbSha256 = Convert.ToHexString(SHA256.HashData(pdbBytes)), codeViewGuid = codeView.Guid,
                pePdbChecksumAlgorithm = expected.AlgorithmName, pePdbChecksum = Convert.ToHexString(expected.Checksum.ToArray()), documents });
        }
        File.WriteAllText(output, JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }));
    }
}
