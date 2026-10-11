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
if (cases.Length is 0 or > 63) throw new ArgumentException("Require 1..63 cases");
var fixture = new byte[16 + cases.Length * 1040];
Put(fixture, 0, 0x53514631);
Put(fixture, 4, (uint)cases.Length);
for (var index = 0; index < cases.Length; index++)
{
    var item = cases[index];
    if (item.Arguments.Any(c => c > 127 || c is '\n' or '\r' or '\0'))
        throw new ArgumentException("Require single-line ASCII arguments");
    var text = Encoding.ASCII.GetBytes(item.Arguments + "\n");
    if (text.Length > 1019 || item.Image > 1 || item.Stack < 4096) throw new ArgumentException("Invalid case");
    var offset = 16 + index * 1040;
    Put(fixture, offset, (uint)index);
    Put(fixture, offset + 4, item.Image);
    Put(fixture, offset + 8, item.Stack);
    Put(fixture, offset + 12, item.CtrlC ? 1u : 0u);
    Put(fixture, offset + 16, (uint)text.Length);
    text.CopyTo(fixture, offset + 20);
}
var inputFiles = new Dictionary<string, byte[]>
{
    ["SEARCHB"] = File.ReadAllBytes(Path.Combine(stage, "inputs", "Search.baseline")),
    ["SEARCHC"] = File.ReadAllBytes(Path.Combine(stage, "inputs", "Search.candidate")),
    ["CASES.FIX"] = fixture,
    ["TEXT.IN"] = File.ReadAllBytes(Path.Combine(stage, "inputs", "TEXT.IN")),
    ["LONG.IN"] = File.ReadAllBytes(Path.Combine(stage, "inputs", "LONG.IN")),
    ["EMPTY.IN"] = []
};
var label = args[4];
if (label.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-')) throw new ArgumentException("Invalid label");
var run = Path.Combine(stage, "runs", label + "-" + args[2]);
Directory.CreateDirectory(run);
if (File.Exists(Path.Combine(run, "result.json"))) throw new IOException("Retain existing results; use a new label");
File.WriteAllBytes(Path.Combine(run, "CASES.FIX"), fixture);
var adf = Path.Combine(run, "final.adf");
var result = await NativeOsSession.RunAsync(File.ReadAllBytes(Path.Combine(stage, "guest", "SearchQualification")),
    File.ReadAllBytes(args[1]), "", cpu, false, 3000, TimeSpan.FromSeconds(120), adf, inputFiles);
File.WriteAllText(Path.Combine(run, "result.json"), JsonSerializer.Serialize(result, options));
if (!File.Exists(adf)) throw new InvalidOperationException("No final disk snapshot: " + result.StopReason);
var receipt = await NativeOsSession.ReadDiskFileAsync(File.ReadAllBytes(adf), "PAYLOAD.OK");
if (receipt is null) throw new InvalidOperationException("Missing guest receipt: " + result.ReturnValue);
File.WriteAllBytes(Path.Combine(run, "receipt.bin"), receipt);
var observations = new List<object>();
var cursor = 8;
if (receipt.Length < 8 || Get(receipt, 0) != 0x53515231 || Get(receipt, 4) != cases.Length)
    throw new InvalidOperationException("Guest receipt header mismatch");
for (var index = 0; index < cases.Length; index++)
{
    if (cursor + 112 > receipt.Length) throw new InvalidOperationException($"Incomplete receipt at case {index}, driver return {result.ReturnValue}");
    var fields = Enumerable.Range(0, 28).Select(i => Get(receipt, cursor + 4 * i)).ToArray();
    var length = checked((int)fields[5]);
    if (fields[0] != index || fields[1] != cases[index].Image || fields[2] != cases[index].Stack ||
        length < 0 || cursor + 112 + length > receipt.Length) throw new InvalidOperationException("Guest row mismatch");
    var bytes = receipt.AsSpan(cursor + 112, length).ToArray();
    observations.Add(new
    {
        index, cases[index].Name, cases[index].Image, cases[index].Stack, cases[index].Arguments, cases[index].CtrlC,
        returnCode = unchecked((int)fields[3]), ioError = fields[4],
        output = Encoding.Latin1.GetString(bytes), outputSha256 = Hash(bytes),
        availableBefore = fields[6], availableAfter = fields[7],
        before = fields[8..18], after = fields[18..28],
        callerStateRestored = fields[8..18].SequenceEqual(fields[18..28])
    });
    cursor += 112 + length;
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
record Case(string Name, string Arguments, uint Image, uint Stack = 4096, bool CtrlC = false);
