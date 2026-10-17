using System.Buffers.Binary;
using Amiga;

namespace CopperOS.Commands.Exe2ArcFrontendNativeExecution;

internal sealed record FrontendFault(string Operation, int Occurrence, int Result, int Error = 0);
internal sealed record FrontendCase(string Id, int? ReadArgsError = null,
    bool AllocationFailure = false, string? Type = null,
    int ExpectedResult = 20, int ExpectedIoError = 103)
{
    public byte[] Input { get; init; } = [];
    public byte[] Output { get; init; } = [];
    public string Text { get; init; } = "";
    public string From { get; init; } = "input.exe";
    public string? To { get; init; }
    public string[] OpenPaths { get; init; } = [];
    public FrontendFault[] Faults { get; init; } = [];
    public int[] ExpectedFaults { get; init; } = [];
    public string? FailAllocation { get; init; }
    public uint? DeclaredLength { get; init; }
    public bool MissingFrom { get; init; }
    public bool Workbench { get; init; }
    public bool MissingDos { get; init; }
    public int EntryLength { get; init; }
    public bool NullArgument { get; init; }
    public bool DeleteOutput { get; init; }
    public int BreakAt { get; init; }
    public uint StackBytes { get; init; } = 4096;
}

internal static class FrontendCases
{
    public const string Suite = "exe2arc-frontend-native-smoke";
    public const string TypeDiagnosticSuite = "exe2arc-type-diagnostic-native-probe";
    public const string RuntimeSuite = "exe2arc-native-entry-vector-fixture";
    private const string Valid = "Valid types are: zip (Zip), ace (Ace), rar (Rar), cab (Cabinet), arj (Arj), lha (LhA), lzh (Amiga-LhA)\n";
    private const string Found = " Found.\n";
    private const string Warning = "Be careful and test the file for correctness.\n";
    public static IReadOnlyList<FrontendCase> Smoke() =>
    [
        new("parser-error-bad-template", ReadArgsError: 119, ExpectedIoError: 119) { ExpectedFaults = [119] },
        new("parser-result-allocation-failure", AllocationFailure: true) { ExpectedFaults = [103] },
    ];

    public static IReadOnlyList<FrontendCase> All()
    {
        var cases = new List<FrontendCase>(Smoke());
        foreach (var type in new[] { "zip", "ace", "rar", "cab", "arj", "lha", "lzh" })
        {
            var test = Success(type);
            cases.Add(test);
            cases.Add(test with { Id = "automatic-"+type, Type = null });
        }
        var rar = Success("rar"); var zip = Success("zip");
        cases.Add(rar with { Id = "type-case-insensitive", Type = "rAr" });
        cases.Add(Success("cab") with { Id = "type-cabinet-alias", Type = "Cabinet" });
        cases.Add(Success("lzh") with { Id = "type-amiga-lha-alias", Type = "Amiga-LhA" });
        cases.Add(rar with { Id = "literal-to", To = "out.data", OpenPaths = ["input.exe", "out.data"], Text = Found+"Saved 96 byte to out.data\n"+Warning });
        cases.Add(rar with { Id = "directory-fallback", To = "RAM:", OpenPaths = ["input.exe", "RAM:", "RAM:input.rar"], Faults = [new("Open", 2, 0, 212)], Text = Found+"Saved 96 byte to RAM:input.rar\n"+Warning });
        cases.Add(new("unknown-type", Type: "bogus", ExpectedIoError: 0) { Text = "\r\u001b[KDid not find archive of type 'bogus'.\n"+Valid });
        cases.Add(new("type-bound", Type: new string('x', 64), ExpectedIoError: 0) { Text = "\r\u001b[KDid not find archive of type '"+new string('x', 64)+"'.\n"+Valid });
        cases.Add(new("typed-no-match", Type: "rar", ExpectedIoError: 0) { Input = new byte[256], OpenPaths = ["input.exe"], Text = "\r\u001b[KDid not find archive of type 'rar'.\nMaybe it is an archive of another type?\n"+Valid });
        cases.Add(new("automatic-no-match", ExpectedIoError: 0) { Input = new byte[256], OpenPaths = ["input.exe"], Text = "\r\u001b[KDid not find archive data.\n" });
        cases.Add(new("empty-input", Type: "rar", ExpectedIoError: 0) { OpenPaths = ["input.exe"], Text = "\r\u001b[KDid not find archive of type 'rar'.\nMaybe it is an archive of another type?\n"+Valid });
        cases.Add(new("workspace-allocation-failure", ExpectedIoError: 0) { FailAllocation = "Workspace", Text = "Failed to open temporary buffer.\n" });
        cases.Add(new("fib-allocation-failure", Type: "rar", ExpectedIoError: 103) { FailAllocation = "FIB", OpenPaths = ["input.exe"], Text = "Failed to open file information object.\n" });
        cases.Add(new("input-open-failure", Type: "rar", ExpectedIoError: 205) { OpenPaths = ["input.exe"], Faults = [new("Open", 1, 0, 205)], Text = "Failed to open input file.\n" });
        cases.Add(new("examine-failure", Type: "rar", ExpectedIoError: 205) { OpenPaths = ["input.exe"], Faults = [new("ExamineFH", 1, 0, 205)], Text = "Failed to examine file.\n" });
        cases.Add(new("unsigned-file-length", Type: "rar", ExpectedIoError: (int)DOS.Error.LineTooLong) { OpenPaths = ["input.exe"], DeclaredLength = 0x80000000, ExpectedFaults = [(int)DOS.Error.LineTooLong] });
        cases.Add(new("missing-from-slot", ExpectedIoError: (int)DOS.Error.BadTemplate) { MissingFrom = true, ExpectedFaults = [(int)DOS.Error.BadTemplate] });
        cases.Add(rar with { Id = "output-open-failure", ExpectedResult = 20, ExpectedIoError = 203, Output = [], Faults = [new("Open", 2, 0, 203)], Text = Found+"\nFailed to open output file.\n" });
        cases.Add(rar with { Id = "fallback-addpart-failure", To = "RAM:", OpenPaths = ["input.exe", "RAM:"], ExpectedResult = 20, ExpectedIoError = 218, Output = [], Faults = [new("Open", 2, 0, 212), new("AddPart", 2, 0, 218)], Text = Found+"\nFailed to open output file.\n" });
        cases.Add(rar with { Id = "unsafe-offset-zero", Input = Archive("rar", 0), Output = [], OpenPaths = ["input.exe"], ExpectedResult = 20, Text = "\r\u001b[KArchive position is unsafe.\n" });
        cases.Add(rar with { Id = "dispatch-seek-failure", Output = [], OpenPaths = ["input.exe"], ExpectedResult = 20, ExpectedIoError = 219, Faults = [new("Seek", 1, -1, 219)], Text = "Failed to seek to file start.\n" });
        cases.Add(rar with { Id = "scanner-read-failure", Output = [], OpenPaths = ["input.exe"], ExpectedResult = 20, ExpectedIoError = 205, Faults = [new("Read", 1, -1, 205)], Text = "", ExpectedFaults = [205] });
        cases.Add(rar with { Id = "scanner-short-read", Output = [], OpenPaths = ["input.exe"], ExpectedResult = 20, Faults = [new("Read", 1, 3)], Text = "" });
        cases.Add(rar with { Id = "payload-position-failure", Output = [], OpenPaths = ["input.exe"], ExpectedResult = 20, ExpectedIoError = 219, Faults = [new("Seek", 4, -1, 219)], Text = "\r\u001b[KArchive position is unsafe.\n" });
        foreach (var fault in new[] { new FrontendFault("Read", 2, -1, 205), new("Read", 2, 3), new("Write", 1, -1, 221), new("Write", 1, 3) })
            cases.Add(rar with { Id = $"payload-{fault.Operation}-{fault.Result}", ExpectedResult = 20, ExpectedIoError = fault.Error, Faults = [fault], DeleteOutput = true, Output = fault.Operation == "Write" && fault.Result > 0 ? rar.Output[..fault.Result] : [], Text = Found, ExpectedFaults = fault.Error != 0 ? [fault.Error] : [] });
        var badZip = (byte[])zip.Input.Clone(); badZip[10] = 0x99;
        cases.Add(zip with { Id = "zip-illegal-record", Input = badZip, ExpectedResult = 20, Output = [], DeleteOutput = true, Text = Found+"Unknown or illegal data found.\n" });
        cases.Add(zip with { Id = "zip-unexpected-end", ExpectedResult = 20, Output = [], DeleteOutput = true, Faults = [new("Read", 3, 0)], Text = Found+"Unexpected end of data.\n" });
        cases.Add(zip with { Id = "zip-break-before-record", BreakAt = 1, ExpectedIoError = (int)DOS.Error.Break, Output = [], Text = Found+"Saved 150 byte to input.zip\n"+Warning });
        cases.Add(zip with { Id = "zip-break-after-local", BreakAt = 2, ExpectedIoError = (int)DOS.Error.Break, Output = zip.Output[..33], Text = Found+"Saved 150 byte to input.zip\n"+Warning });
        cases.Add(zip with { Id = "zip-break-after-eocd", BreakAt = 4, ExpectedIoError = (int)DOS.Error.Break, Text = Found+"Saved 150 byte to input.zip\n"+Warning });
        var large = Archive("rar", 102398, 102430);
        cases.Add(rar with { Id = "rar-window-crossing", Input = large, Output = large[102398..], Text = Found+"Saved 32 byte to input.rar\n"+Warning });
        var big = Archive("rar", 32, 102445);
        cases.Add(rar with { Id = "multi-chunk-payload", Input = big, Output = big[32..], Text = Found+"Saved 102413 byte to input.rar\n"+Warning });
        cases.Add(rar with { Id = "output-name-no-suffix", From = "SYS:dir/file", OpenPaths = ["SYS:dir/file", "SYS:dir/file.rar"], Text = Found+"Saved 96 byte to SYS:dir/file.rar\n"+Warning });
        cases.Add(rar with { Id = "output-name-bound", From = new string('p', 510), ExpectedResult = 20, ExpectedIoError = (int)DOS.Error.LineTooLong, OpenPaths = [new string('p', 510)], Output = [], Text = Found, ExpectedFaults = [(int)DOS.Error.LineTooLong] });
        cases.Add(new("negative-entry-length", ExpectedResult: 10, ExpectedIoError: (int)DOS.Error.LineTooLong) { EntryLength = -1 });
        cases.Add(new("null-entry-buffer", ExpectedResult: 10, ExpectedIoError: (int)DOS.Error.LineTooLong) { EntryLength = 4, NullArgument = true });
        cases.Add(new("missing-dos", ExpectedIoError: 0x13572468) { MissingDos = true });
        cases.Add(new("workbench-startup", ExpectedIoError: 0) { Workbench = true });
        cases.Add(new("workbench-missing-dos", ExpectedIoError: 0x13572468) { Workbench = true, MissingDos = true });
        return cases;
    }

    private static FrontendCase Success(string type)
    {
        var input = type == "zip" ? Zip() : Archive(type, type == "lha" ? 100 : 32, type == "lha" ? 200 : 128);
        var output = type == "zip" ? input[10..111] : input[(type == "lha" ? 100 : 32)..(type == "cab" ? 68 : input.Length)];
        if (type == "zip") { output = (byte[])output.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(75), 10); BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(95), 43); }
        return new("extract-"+type, Type: type, ExpectedResult: 0, ExpectedIoError: 0) { Input = input, Output = output, OpenPaths = ["input.exe", "input."+type], Text = Found+$"Saved {output.Length} byte to input.{type}\n"+Warning };
    }

    // Inert byte fixtures, constructed independently of production scanners.
    private static byte[] Archive(string type, int at, int length = 128)
    {
        var bytes = Enumerable.Repeat((byte)0x55, length).ToArray();
        switch (type)
        {
            case "rar": new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1a, 7, 0 }.CopyTo(bytes, at); break;
            case "cab": "MSCF"u8.CopyTo(bytes.AsSpan(at)); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at+8), 36); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at+16), 20); break;
            case "ace": "**ACE**"u8.CopyTo(bytes.AsSpan(at+7)); break;
            case "arj":
                bytes[at] = 0x60; bytes[at+1] = 0xea; bytes[at+2] = 6; bytes[at+3] = 0;
                uint crc = uint.MaxValue;
                for (int i=0; i<6; i++) { bytes[at+4+i] = (byte)(i+1); crc ^= (byte)(i+1); for(int b=0;b<8;b++) crc = (crc&1)!=0 ? (crc>>1)^0xedb88320u : crc>>1; }
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at+10), ~crc); break;
            case "lha": BinaryPrimitives.WriteUInt32BigEndian(bytes, 0x3f3); "SFX!"u8.CopyTo(bytes.AsSpan(44)); BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(52), (uint)at); break;
            case "lzh": bytes[20] = 2; "-lh5-"u8.CopyTo(bytes.AsSpan(at+2)); break;
        }
        return bytes;
    }
    private static byte[] Zip()
    {
        var bytes = Enumerable.Repeat((byte)0x55, 160).ToArray();
        "PK\x03\x04"u8.CopyTo(bytes.AsSpan(10)); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(28), 3); bytes.AsSpan(36,4).Clear(); new byte[] { 0xa1, 0xb2, 0xc3 }.CopyTo(bytes, 40);
        "PK\x01\x02"u8.CopyTo(bytes.AsSpan(43)); bytes.AsSpan(71,6).Clear(); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(85), 0);
        "PK\x05\x06"u8.CopyTo(bytes.AsSpan(89)); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(101), 46); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(105), 33); bytes.AsSpan(109,2).Clear();
        return bytes;
    }
}
