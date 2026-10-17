using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Copper68k;
using CopperSharp.Compiler.NativeOsRunner;

if (args.Length == 3 && args[0] == "--bind") { Bind.Run(args[1], args[2]); return 0; }
if (args.Length != 5) throw new ArgumentException("Host STAGE ROM CPU CASES_JSON LABEL");
var stage = Path.GetFullPath(args[0]);
var cpu = args[2] switch
{
    "68000" => M68kCpuModel.M68000,
    "68020" => M68kCpuModel.M68020,
    "68040" => M68kCpuModel.M68040,
    _ => throw new ArgumentException("CPU must be 68000, 68020 or 68040")
};
var options = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };
var cases = JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[3]), options)!;
if (cases.Length is 0 or > 42) throw new ArgumentException("Require 1..42 cases");
var fixture = new byte[16 + cases.Length * 1552];
Put(fixture, 0, 0x45414631);
Put(fixture, 4, (uint)cases.Length);
for (var index = 0; index < cases.Length; index++)
{
    var item = cases[index];
    if (item.Arguments.Any(c => c > 127 || c is '\n' or '\r' or '\0'))
        throw new ArgumentException("Require single-line ASCII arguments");
    var text = Encoding.ASCII.GetBytes(item.Arguments + "\n");
    if (text.Length > 1019 || item.Image > 1 || item.Stack < 4096) throw new ArgumentException("Invalid case");
    var offset = 16 + index * 1552;
    Put(fixture, offset, (uint)index);
    Put(fixture, offset + 4, item.Image);
    Put(fixture, offset + 8, item.Stack);
    Put(fixture, offset + 12, item.CtrlC ? 1u : 0u);
    Put(fixture, offset + 16, (uint)text.Length);
    text.CopyTo(fixture, offset + 20);
    var path = Encoding.ASCII.GetBytes(item.OutputPath);
    if (path.Length is 0 or > 511 || item.OutputPath.Any(c => c > 127 || c is '\0' or '\n' or '\r')) throw new ArgumentException("Invalid output path");
    path.CopyTo(fixture, offset + 1040);
}
var inputFiles = new Dictionary<string, byte[]>
{
    ["EXE2ARCB"] = File.ReadAllBytes(Path.Combine(stage, "inputs", "Exe2Arc.baseline")),
    ["EXE2ARCC"] = File.ReadAllBytes(Path.Combine(stage, "inputs", "Exe2Arc.candidate")),
    ["CASES.FIX"] = fixture,

};
var sources = new[] { ("ZIP.IN","zip.exe"), ("ACE.IN","ace.exe"), ("RAR.IN","rar.exe"), ("CAB.IN","cab.exe"), ("ARJ.IN","arj.exe"), ("LHA.IN","lha.exe"), ("LZH.IN","lzh.exe"), ("RANDOM.IN","random.exe"), ("EMPTY.IN","empty.exe"), ("ZERO.IN","zero.exe"), ("BADZIP.IN","badzip.exe"), ("SHORTZIP.IN","shortzip.exe"), ("CROSS.IN","cross.exe"), ("BIG.IN","big.exe"), ("RAR.IN","quoted name.exe"), ("RAR.IN","nosuffix") };
uint inputMask = 0;
for (var index=0; index<sources.Length; index++)
    if (cases.Any(c => c.Arguments.Contains("RAM:Exe2ArcQ/"+sources[index].Item2, StringComparison.Ordinal)))
    {
        inputMask |= 1u << index;
        inputFiles[sources[index].Item1] = File.ReadAllBytes(Path.Combine(stage,"inputs",sources[index].Item1));
    }
Put(fixture, 8, inputMask);
var label = args[4];
if (label.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-')) throw new ArgumentException("Invalid label");
var run = Path.Combine(stage, "runs", label + "-" + args[2]);
Directory.CreateDirectory(run);
if (File.Exists(Path.Combine(run, "result.json"))) throw new IOException("Retain existing results; use a new label");
File.WriteAllBytes(Path.Combine(run, "CASES.FIX"), fixture);
var adf = Path.Combine(run, "final.adf");
var result = await NativeOsSession.RunAsync(File.ReadAllBytes(Path.Combine(stage, "guest", "Exe2ArcQualification")),
    File.ReadAllBytes(args[1]), "", cpu, false, 3000, TimeSpan.FromSeconds(120), adf, inputFiles);
File.WriteAllText(Path.Combine(run, "result.json"), JsonSerializer.Serialize(result, options));
if (!File.Exists(adf)) throw new InvalidOperationException("No final disk snapshot: " + result.StopReason);
var receipt = await NativeOsSession.ReadDiskFileAsync(File.ReadAllBytes(adf), "PAYLOAD.OK");
if (receipt is null) throw new InvalidOperationException("Missing guest receipt: " + result.ReturnValue);
File.WriteAllBytes(Path.Combine(run, "receipt.bin"), receipt);
var observations = new List<object>();
var cursor = 8;
if (receipt.Length < 8 || Get(receipt, 0) != 0x45415231 || Get(receipt, 4) != cases.Length)
    throw new InvalidOperationException("Guest receipt header mismatch");
for (var index = 0; index < cases.Length; index++)
{
    if (cursor + 128 > receipt.Length) throw new InvalidOperationException($"Incomplete receipt at case {index}, driver return {result.ReturnValue}");
    var fields = Enumerable.Range(0, 32).Select(i => Get(receipt, cursor + 4 * i)).ToArray();
    var length = checked((int)fields[5]);
    if (fields[0] != index || fields[1] != cases[index].Image || fields[2] != cases[index].Stack ||
        length < 0 || cursor + 128 + length > receipt.Length) throw new InvalidOperationException("Guest row mismatch");
    var bytes = receipt.AsSpan(cursor + 128, length).ToArray();
    var payloadLength = checked((int)fields[29]);
    if (payloadLength < 0 || cursor + 128 + length + payloadLength > receipt.Length) throw new InvalidOperationException("Incomplete extracted payload");
    var payload = receipt.AsSpan(cursor + 128 + length, payloadLength).ToArray();
    observations.Add(new
    {
        index, cases[index].Name, cases[index].Image, cases[index].Stack, cases[index].Arguments, cases[index].CtrlC, cases[index].OutputPath,
        returnCode = unchecked((int)fields[3]), ioError = fields[4],
        output = Encoding.Latin1.GetString(bytes), outputSha256 = Hash(bytes),
        availableBefore = fields[6], availableAfter = fields[7],
        fileExists = fields[28] == 1, payloadLength, payloadHex = Convert.ToHexStringLower(payload), payloadSha256 = Hash(payload),
        payloadClosed = fields[30], payloadDeleted = fields[31],
        before = fields[8..18], after = fields[18..28],
        callerStateRestored = fields[8..18].SequenceEqual(fields[18..28])
    });
    cursor += 128 + length + payloadLength;
}
if (cursor != receipt.Length) throw new InvalidOperationException("Unexpected guest receipt suffix");
var completed = result.StopReason == "ProgramReturned" && result.LaunchStatus == 0 &&
    result.ReturnValue == 0 && result.DiskSnapshotError is null;
var report = new { completed, cpu = args[2], result, inputs = inputFiles.ToDictionary(p => p.Key, p => Hash(p.Value)), observations };
File.WriteAllText(Path.Combine(run, "observations.json"), JsonSerializer.Serialize(report, options));
Console.WriteLine(JsonSerializer.Serialize(new { run, completed, cases = cases.Length, result.ReturnValue, result.StopReason }));
return completed ? 0 : 1;

static void Put(byte[] data, int offset, uint value) => BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(offset), value);
static uint Get(byte[] data, int offset) => BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset));
static string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
record Case(string Name, string Arguments, uint Image, string OutputPath, uint Stack = 4096, bool CtrlC = false);
