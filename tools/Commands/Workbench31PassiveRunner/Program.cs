using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text.Json;
using CopperMod.Amiga.Lightweight;

// Reads only public passive views on the machine owner thread. No host DOS
// services, guest bus accesses, input injection, reflection or guest writes.
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
if (args.Length == 4 && args[0] == "--bind-pdb")
{
    // Portable-PDB document metadata binds source bytes, not private machine state.
    using var stream = File.OpenRead(args[1]);
    using var provider = MetadataReaderProvider.FromPortablePdbStream(stream);
    var reader = provider.GetMetadataReader();
    var rows = reader.Documents.Select(handle =>
    {
        var doc = reader.GetDocument(handle);
        var name = reader.GetString(doc.Name);
        var localPath = Path.Combine(args[2], Path.GetFileName(name));
        var expected = Convert.ToHexString(reader.GetBlobBytes(doc.Hash)).ToLowerInvariant();
        var actual = File.Exists(localPath) ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(localPath))).ToLowerInvariant() : null;
        return new { document = name, hashAlgorithm = reader.GetGuid(doc.HashAlgorithm), expected, localPath, actual, matches = actual == expected };
    }).ToArray();
    File.WriteAllText(args[3], JsonSerializer.Serialize(rows, jsonOptions));
    return;
}
if (args.Length != 4 || !int.TryParse(args[2], out var frames) || frames < 1)
    throw new ArgumentException("Usage: Workbench31PassiveRunner <rom> <adf> <frames> <output-directory>");
var output = Path.GetFullPath(args[3]);
Directory.CreateDirectory(output);
using var machine = new LightweightA500Machine(new LightweightA500Configuration { FramebufferWidth = 908 });
machine.LoadKickstart(File.ReadAllBytes(args[0]));
machine.MountAdf(File.ReadAllBytes(args[1]));
var outputChecksum = 1469598103934665603UL;
for (var frame = 0; frame < frames; frame++)
{
    machine.ExecuteFrame();
    outputChecksum = AccumulateOutputChecksum(machine, outputChecksum, frame);
    if (frame == 0 || (frame + 1) % 60 == 0 || frame + 1 == frames)
    {
        var stem = Path.Combine(output, $"frame-{frame + 1:D6}");
        File.WriteAllBytes(stem + ".chipram", machine.ChipRam.ToArray());
        File.WriteAllBytes(stem + ".slowram", machine.SlowRam.ToArray());
        WriteBitmap(stem + ".bmp", machine.Framebuffer.Span, machine.FramebufferWidth, machine.FramebufferHeight);
        File.WriteAllText(stem + ".json", JsonSerializer.Serialize(new
        {
            frame = frame + 1, machine.Cycle, machine.CompletedFrames,
            machine.BeamLine, machine.BeamColorClock, machine.RomOverlayEnabled,
            pc = machine.Cpu.ProgramCounter, sr = machine.Cpu.StatusRegister,
            cpuCycles = machine.Cpu.Cycles, d = machine.Cpu.D.ToArray(), a = machine.Cpu.A.ToArray(),
            machine.DriveState, machine.UnsupportedActiveFeature,
        }, jsonOptions));
    }
}
var summary = new
{
    frames, machine.Cycle, machine.CompletedFrames,
    cpuFingerprint = CreateCpuChecksum(machine).ToString("X16"),
    outputFingerprint = outputChecksum.ToString("X16"),
    hardwareFingerprint = (string?)null,
    hardwareFingerprintReason = "Existing runner checksum uses internal device state; this runner has only public passive APIs.",
    machine.UnsupportedActiveFeature,
};
File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(summary, jsonOptions));
Console.WriteLine(JsonSerializer.Serialize(summary, jsonOptions));

static ulong CreateCpuChecksum(LightweightA500Machine machine)
{
    const ulong prime = 1099511628211UL;
    var cpu = machine.Cpu;
    var hash = 1469598103934665603UL;
    hash = (hash ^ cpu.ProgramCounter) * prime;
    hash = (hash ^ cpu.StatusRegister) * prime;
    hash = (hash ^ (ulong)cpu.Cycles) * prime;
    foreach (var value in cpu.D) hash = (hash ^ value) * prime;
    foreach (var value in cpu.A) hash = (hash ^ value) * prime;
    return hash;
}

static ulong AccumulateOutputChecksum(LightweightA500Machine machine, ulong hash, int frame)
{
    const ulong prime = 1099511628211UL;
    var pixels = machine.Framebuffer.Span;
    var audio = machine.AudioSamples.Span;
    hash = (hash ^ (uint)pixels.Length) * prime;
    hash = (hash ^ (uint)audio.Length) * prime;
    var pixelIndex = (frame * 131) % pixels.Length;
    for (var sample = 0; sample < 257; sample++)
    {
        hash = (hash ^ (uint)pixels[pixelIndex]) * prime;
        pixelIndex += 557;
        if (pixelIndex >= pixels.Length) pixelIndex -= pixels.Length;
    }
    var audioIndex = (frame * 17) % audio.Length;
    for (var sample = 0; sample < 61; sample++)
    {
        hash = (hash ^ (ushort)audio[audioIndex]) * prime;
        audioIndex += 31;
        if (audioIndex >= audio.Length) audioIndex -= audio.Length;
    }
    return hash;
}

static void WriteBitmap(string path, ReadOnlySpan<int> pixels, int width, int height)
{
    using var writer = new BinaryWriter(File.Create(path));
    var pixelBytes = width * height * 4;
    writer.Write((ushort)0x4d42);
    writer.Write(54 + pixelBytes);
    writer.Write(0);
    writer.Write(54);
    writer.Write(40);
    writer.Write(width);
    writer.Write(-height);
    writer.Write((ushort)1);
    writer.Write((ushort)32);
    writer.Write(0);
    writer.Write(pixelBytes);
    writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
    foreach (var pixel in pixels) writer.Write(pixel);
}
