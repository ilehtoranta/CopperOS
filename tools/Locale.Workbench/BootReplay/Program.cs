/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amiga;
using Copper68k;
using CopperMod.Amiga.Lightweight;
using CopperStart.SystemRom;
using CopperStart.Devices;

// Full-machine runner. Inspect public passive RAM/framebuffer views only;
// no host OS overlays, CPU call injection, patched guest RAM or forced returns.
var options = new Dictionary<string, string>();
for (int i = 0; i < args.Length; i += 2) options.Add(args[i], args[i + 1]);
string output = Path.GetFullPath(options["--output"]); Directory.CreateDirectory(output);
var json = new JsonSerializerOptions { WriteIndented = true };
string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
byte[] rom = File.ReadAllBytes(options["--rom"]);
var inputs = new Dictionary<string, string> { ["rom"] = Hash(rom) };
if (options.TryGetValue("--manifest", out var manifestPath))
{
    using var doc = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
    if (doc.RootElement.GetProperty("sha256").GetString() != Hash(rom)) throw new InvalidDataException("ROM hash differs from its manifest");
    foreach (var entry in doc.RootElement.GetProperty("inputs").EnumerateArray())
    {
        string name = entry.GetProperty("file").GetString()!;
        if (Hash(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, name))) != entry.GetProperty("sha256").GetString())
            throw new InvalidDataException("Frozen runtime mismatch: " + name);
    }
    inputs["manifest"] = Hash(File.ReadAllBytes(manifestPath));
}
bool replacement = options.ContainsKey("--manifest");
bool IsWorkbenchTask(TaskRow task) => task.Name == "Workbench" || replacement && task.Name == "CopperBench";
bool writeProtected = options.GetValueOrDefault("--write-protected", "false") == "true";
string ramProfile = options.GetValueOrDefault("--ram-profile", "fast-2m");
if (ramProfile != "fast-2m" && ramProfile != "one-mib") throw new ArgumentException("Unknown RAM profile: " + ramProfile);
int fastBytes = ramProfile == "one-mib" ? 0 : 2 * 1024 * 1024;
int slowBytes = ramProfile == "one-mib" ? 512 * 1024 : 0;
using var machine = new LightweightA500Machine(new LightweightA500Configuration
{ ChipRamBytes = 512 * 1024, FastRamBytes = fastBytes, SlowRamBytes = slowBytes,
  CpuModel = M68kCpuModel.M68000, FloppyDriveCount = 3, FramebufferWidth = 908 });
// The pinned engine exposes Chip/Fast views but no passive A501 view. Resolve
// its backing storage once, as in NativeBootImage.ReadMemory; never read the bus.
var slowRam = (byte[])typeof(LightweightA500Machine).GetField("_slowRam",
    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(machine)!;
machine.LoadKickstart(rom);
for (int drive = 0; drive < 3; drive++)
    if (options.TryGetValue("--df" + drive, out var path))
    { var bytes = File.ReadAllBytes(path); inputs["df" + drive] = Hash(bytes); machine.MountAdf(drive, bytes); machine.SetDriveWriteProtected(drive, writeProtected); }
int limit = int.Parse(options.GetValueOrDefault("--frames", "65000"));
int minimumFrames = int.Parse(options.GetValueOrDefault("--minimum-frames", "0"));
bool memoryCheckpoints = options.GetValueOrDefault("--memory-checkpoints", "false") == "true";
var checkpointNames = new HashSet<string>(); int firstWorkbenchFrame = -1;
var history = new List<string>(); var failures = new HashSet<string>(); string previous = "", lastCommand = "";
var scriptHistory = new List<ScriptRow>(); ScriptRow? previousScript = null;
bool workbenchSeen = false, iprefsSeen = false, lowPcFailure = false, bootFailure = false, startupCommandSeen = false, startupReturnedToInteractive = false;
int settled = 0, interactiveSettled = 0, frame = 0;
using var progress = new StreamWriter(Path.Combine(output, "progress.txt")) { AutoFlush = true };
for (; frame < limit; frame++)
{
    machine.ExecuteFrame();
    // This profile has no executable code in its exception-vector/diagnostic
    // page. An unhandled native vector must fail the run before subsequent
    // instructions can corrupt the public lists being inspected below.
    if (frame > 1000 && machine.Cpu.ProgramCounter < 0x400) { lowPcFailure = true; break; }
    if (frame % 8 != 0) continue;
    var tasks = Tasks(); workbenchSeen |= tasks.Any(IsWorkbenchTask); iprefsSeen |= tasks.Any(t => t.Name.Contains("IPrefs", StringComparison.OrdinalIgnoreCase));
    uint control = replacement ? Read(NativeDosBoot.ReceiptAddress) : 0;
    if (control != 0 && Read(control) == uint.MaxValue) { bootFailure = true; break; }
    uint process = control == 0 ? 0 : Read(control + NativeDosBoot.ProcessOffset);
    uint cli = process == 0 ? 0 : Read(process + DosLayout.Process.CommandLineInterface) << 2;
    string command = cli == 0 ? "" : Bstr(Read(cli + DosLayout.CommandLineInterface.CommandName) << 2);
    uint rc = cli == 0 ? 0 : Read(cli + DosLayout.CommandLineInterface.ReturnCode);
    var script = Script(control, cli);
    if (script != previousScript)
    {
        if (script != null) scriptHistory.Add(script);
        previousScript = script;
    }
    if (memoryCheckpoints)
    {
        if (command.Equals("LoadWB", StringComparison.OrdinalIgnoreCase) || command.EndsWith(":LoadWB", StringComparison.OrdinalIgnoreCase)) Checkpoint("loadwb");
        if (firstWorkbenchFrame < 0 && tasks.Any(IsWorkbenchTask)) firstWorkbenchFrame = frame;
        if (firstWorkbenchFrame >= 0 && frame - firstWorkbenchFrame <= 32) Checkpoint("workbench-" + (frame - firstWorkbenchFrame));
    }
    if (command.Length != 0) { lastCommand = command; startupCommandSeen = true; }
    if (command.Length == 0 && rc >= 10) failures.Add($"{lastCommand}: RC={rc}");
    string status = $"command={command} RC={rc} tasks={string.Join(';', tasks.Select(t => t.Name))} libraries={string.Join(';', Libraries().Select(l => l.Name + ':' + l.Version + '.' + l.Revision))}";
    if (previous != status || frame % 1000 == 0)
    { string row = $"frame={frame} PC={machine.Cpu.ProgramCounter:X8} cycles={machine.Cycle} {status}"; history.Add(row); progress.WriteLine(row); previous = status; }
    bool ended = replacement ? control != 0 && Read(control + NativeDosBoot.ShellPhaseOffset) == 3 : !tasks.Any(t => t.Name == "Initial CLI");
    bool returnedToPrompt = replacement && startupCommandSeen && control != 0 && Read(control) == 4 && !ended && command.Length == 0 &&
        cli != 0 && Read(cli + DosLayout.CommandLineInterface.CurrentInput) == Read(cli + DosLayout.CommandLineInterface.StandardInput);
    interactiveSettled = returnedToPrompt ? interactiveSettled + 1 : 0;
    if (interactiveSettled >= 64) { startupReturnedToInteractive = true; break; }
    var wbLibrary = replacement ? Libraries().FirstOrDefault(l => l.Name == "workbench.library") : null;
    bool desktopInitializing = wbLibrary != null && Read(wbLibrary.Address + 112) == 0 && tasks.Any(IsWorkbenchTask);
    if (ended && !desktopInitializing) settled++; else settled = 0;
    if (settled >= 64 && frame >= minimumFrames || machine.Cpu.Halted) break;
}
var finalTasks = Tasks(); var libraries = Libraries();
File.WriteAllText(Path.Combine(output, "script-progress.json"), JsonSerializer.Serialize(scriptHistory, json));
var nativeWorkbench = replacement ? libraries.FirstOrDefault(l => l.Name == "workbench.library") : null;
uint nativeWorkbenchPhase = nativeWorkbench == null ? 0 : Read(nativeWorkbench.Address + 112);
uint nativeDesktopWindow = nativeWorkbench == null ? 0 : Read(nativeWorkbench.Address + 136);
bool nativeDesktopReady = nativeWorkbenchPhase == 1 && nativeDesktopWindow != 0;
var observations = new List<object>();
uint portList = Read(4) + ExecLayout.ExecBase.PortList, port = Read(portList);
for (int i = 0; i < 128 && port != 0 && port != portList + 4; i++, port = Read(port))
    if (Text(Read(port + 10)).StartsWith("LocaleProof", StringComparison.Ordinal) && Read(port + 34) == 0x4c504631)
        observations.Add(new { name = Text(Read(port + 10)), status = Read(port + 38), localeBase = Read(port + 42), locale = Read(port + 46),
            environmentLanguage = Text(port + 64), january = Text(port + 128), fault205 = Text(port + 192), fault103 = Text(port + 320), languageName = Text(port + 448), utilityLanguage = Read(port + 54) });
uint finalControl = replacement ? Read(NativeDosBoot.ReceiptAddress) : 0;
var receipt = new { kind = "full-machine-workbench-locale-attempt", shipping = false,
    machineProfile = ramProfile == "one-mib" ? "PAL OCS 68000, 512 KiB Chip, 512 KiB slow, no Fast, three DD drives" : "PAL OCS 68000, 512 KiB Chip, 2 MiB Fast, three DD drives",
    memory = new { chipBytes = machine.ChipRam.Length, slowBytes = slowRam.Length, fastBytes = machine.FastRam.Length }, writeProtected,
    hostOsOverlays = false, scalarDiagnostic = false, memoryCheckpoints, replacement, inputs, frame,
    pc = machine.Cpu.ProgramCounter, halted = machine.Cpu.Halted, lowPcFailure, bootFailure,
    bootError = finalControl == 0 ? 0 : Read(finalControl + NativeDosBoot.ErrorOffset), startupReturnedToInteractive, machine.UnsupportedActiveFeature,
    registers = new { d = machine.Cpu.D, a = machine.Cpu.A, sr = machine.Cpu.StatusRegister, usp = machine.Cpu.UserStackPointer, ssp = machine.Cpu.SupervisorStackPointer },
    workbenchSeen, nativeWorkbenchPhase, nativeDesktopWindow, nativeDesktopReady, workbenchTaskPresent = finalTasks.Any(IsWorkbenchTask),
    workbenchTaskName = finalTasks.FirstOrDefault(IsWorkbenchTask)?.Name, iprefsSeen,
    phase = finalControl == 0 ? 0 : Read(finalControl + NativeDosBoot.ShellPhaseOffset),
    commandFailures = failures, tasks = finalTasks, libraries, observations, console = ConsoleText(finalControl), history,
    sourceUnchanged = Enumerable.Range(0, 3).Where(d => options.ContainsKey("--df" + d)).All(d => Hash(File.ReadAllBytes(options["--df" + d])) == inputs["df" + d]) };
File.WriteAllText(Path.Combine(output, "boot.json"), JsonSerializer.Serialize(receipt, json));
Png(Path.Combine(output, "screen.png"));
File.WriteAllBytes(Path.Combine(output, "chip.bin"), machine.ChipRam.ToArray());
File.WriteAllBytes(Path.Combine(output, "fast.bin"), machine.FastRam.ToArray());
File.WriteAllBytes(Path.Combine(output, "slow.bin"), slowRam);
Console.WriteLine(JsonSerializer.Serialize(new { output, frame, receipt.workbenchTaskPresent, iprefsSeen, receipt.phase, receipt.commandFailures, libraries, observations }, json));
uint expectedVersion = uint.Parse(options.GetValueOrDefault("--locale-version", "53"));
return !lowPcFailure && !bootFailure && !startupReturnedToInteractive && receipt.workbenchTaskPresent && iprefsSeen &&
    (!replacement || receipt.phase == 3 && nativeDesktopReady) && libraries.Any(l => l.Name == "locale.library" && l.Version == expectedVersion) ? 0 : 2;

void Checkpoint(string name)
{
    if (!checkpointNames.Add(name)) return;
    var directory = Path.Combine(output, "checkpoints", name); Directory.CreateDirectory(directory);
    uint control = replacement ? Read(NativeDosBoot.ReceiptAddress) : 0;
    File.WriteAllText(Path.Combine(directory, "boot.json"), JsonSerializer.Serialize(new {
        frame, memory = new { chipBytes = machine.ChipRam.Length, slowBytes = slowRam.Length, fastBytes = machine.FastRam.Length },
        phase = control == 0 ? 0 : Read(control + NativeDosBoot.ShellPhaseOffset),
        bootError = control == 0 ? 0 : Read(control + NativeDosBoot.ErrorOffset),
        pc = machine.Cpu.ProgramCounter, tasks = Tasks(), libraries = Libraries()
    }, json));
    File.WriteAllBytes(Path.Combine(directory, "chip.bin"), machine.ChipRam.ToArray());
    File.WriteAllBytes(Path.Combine(directory, "slow.bin"), slowRam);
    File.WriteAllBytes(Path.Combine(directory, "fast.bin"), machine.FastRam.ToArray());
}

uint Read(uint a)
{
    if (a <= machine.ChipRam.Length - 4) return BinaryPrimitives.ReadUInt32BigEndian(machine.ChipRam.Span.Slice((int)a, 4));
    if (machine.FastRamBase is uint start && a >= start && a - start <= machine.FastRam.Length - 4) return BinaryPrimitives.ReadUInt32BigEndian(machine.FastRam.Span.Slice((int)(a - start), 4));
    if (a >= 0xC00000 && (long)a - 0xC00000 <= slowRam.Length - 4) return BinaryPrimitives.ReadUInt32BigEndian(slowRam.AsSpan((int)(a - 0xC00000), 4));
    if (a >= 0xf80000 && a - 0xf80000 <= rom.Length - 4) return BinaryPrimitives.ReadUInt32BigEndian(rom.AsSpan((int)(a - 0xf80000), 4));
    return 0;
}
byte Byte(uint a) => (byte)(Read(a) >> 24);
ScriptRow? Script(uint control, uint cli)
{
    uint state = control == 0 ? 0 : Read(control + NativeDosBoot.DosStateOffset);
    uint allocation = state == 0 ? 0 : Read(state + 88);
    for (int n = 0; allocation != 0 && n < 512; n++, allocation = Read(allocation + 16))
    {
        uint runner = allocation + 28;
        if (Read(allocation) != 0x444F424A || Read(allocation + 20) != 0x53485255 ||
            Read(runner) != 0x5352554E || Read(runner + 8) != cli) continue;
        uint address = Read(runner + 12);
        return new(address, Read(address + 32), Read(address + 36), unchecked((int)Read(address + 44)),
            Read(address + 84), Text(Read(runner + 20)));
    }
    return null;
}
string Text(uint a)
{ if (a == 0) return ""; var b = new StringBuilder(); for (uint i = 0; i < 256 && Byte(a + i) != 0; i++) b.Append((char)Byte(a + i)); return b.ToString(); }
string Bstr(uint a)
{ if (a == 0) return ""; var b = new StringBuilder(); for (uint i = 1; i <= Byte(a); i++) b.Append((char)Byte(a + i)); return b.ToString(); }
List<TaskRow> Tasks()
{
    var result = new List<TaskRow>(); var seen = new HashSet<uint>(); uint exec = Read(4);
    void Add(uint a) { if (a != 0 && seen.Add(a)) result.Add(new(a, Text(Read(a + 10)))); }
    Add(Read(exec + ExecLayout.ExecBase.ThisTask));
    foreach (int offset in new[] { ExecLayout.ExecBase.TaskReady, ExecLayout.ExecBase.TaskWait })
    { uint list = exec + (uint)offset, node = Read(list); for (int i = 0; i < 128 && node != 0 && node != list + 4; i++) { Add(node); node = Read(node); } }
    return result;
}
List<LibraryRow> Libraries()
{
    var result = new List<LibraryRow>(); uint list = Read(4) + ExecLayout.ExecBase.LibraryList, node = Read(list);
    for (int i = 0; i < 128 && node != 0 && node != list + 4; i++)
    { result.Add(new(node, Text(Read(node + 10)), Read(node + 20) >> 16, Read(node + 22) >> 16)); node = Read(node); }
    return result;
}
string ConsoleText(uint control)
{
    uint console = control == 0 ? 0 : Read(control + NativeDosBoot.ConsoleOffset);
    uint bitmap = console == 0 ? 0 : Read(console + NativeBootConsole.CoreOffset + NativeConsoleCore.Bitmap);
    if (bitmap == 0 || bitmap + 16000 > machine.ChipRam.Length) return "";
    var b = new StringBuilder();
    for (int row = 0; row < 25; row++)
    {
        var line = new StringBuilder();
        for (int col = 0; col < 80; col++)
        {
            char value = '?';
            for (byte c = 32; c < 127; c++)
            { bool match = true; for (int y = 0; y < 8; y++) if (machine.ChipRam.Span[(int)bitmap + (row * 8 + y) * 80 + col] != NativeConsoleFont.Row(c, y)) { match = false; break; } if (match) { value = (char)c; break; } }
            line.Append(value);
        }
        string text = line.ToString().TrimEnd(); if (text.Length != 0) b.AppendLine(text);
    }
    return b.ToString();
}
void Png(string path)
{
    using var file = File.Create(path); file.Write(new byte[] { 137,80,78,71,13,10,26,10 });
    void Chunk(string name, byte[] data)
    { byte[] tag = Encoding.ASCII.GetBytes(name), size = new byte[4]; BinaryPrimitives.WriteUInt32BigEndian(size, (uint)data.Length); file.Write(size); file.Write(tag); file.Write(data); uint crc = uint.MaxValue; foreach (byte b in tag.Concat(data)) { crc ^= b; for (int i = 0; i < 8; i++) crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xedb88320u : 0); } BinaryPrimitives.WriteUInt32BigEndian(size, ~crc); file.Write(size); }
    byte[] header = new byte[13]; BinaryPrimitives.WriteUInt32BigEndian(header, (uint)machine.FramebufferWidth); BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), (uint)machine.FramebufferHeight); header[8] = 8; header[9] = 2; Chunk("IHDR", header);
    using var compressed = new MemoryStream();
    using (var z = new ZLibStream(compressed, CompressionLevel.Fastest, true))
        for (int y = 0; y < machine.FramebufferHeight; y++) { z.WriteByte(0); for (int x = 0; x < machine.FramebufferWidth; x++) { int color = machine.Framebuffer.Span[y * machine.FramebufferWidth + x]; z.WriteByte((byte)(color >> 16)); z.WriteByte((byte)(color >> 8)); z.WriteByte((byte)color); } }
    Chunk("IDAT", compressed.ToArray()); Chunk("IEND", Array.Empty<byte>());
}
internal sealed record TaskRow(uint Address, string Name);
internal sealed record LibraryRow(uint Address, string Name, uint Version, uint Revision);
internal sealed record ScriptRow(uint Address, uint Line, uint Offset, int Result, uint Pending, string Text);
