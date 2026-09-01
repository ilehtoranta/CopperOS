using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Copper68k;
using CopperOS.Commands.NativeExecution;

namespace CopperOS.Commands.EvalNativeExecution;

internal static class Program
{
    internal const string Suite = "eval-numeric-component";

    public static int Main(string[] args)
    {
        if (args.Length is not (3 or 4))
        {
            Console.Error.WriteLine("usage: EvalNativeExecution <probe.hunk> <68000|68020|68040> <report.json> [eval-numeric-component]");
            return 2;
        }
        string? imageHash = null;
        try
        {
            if (args.Length == 4 && args[3] != Suite)
                throw new ArgumentException("Unsupported numeric component suite.");
            var model = args[1] switch
            {
                "68000" => M68kCpuModel.M68000,
                "68020" => M68kCpuModel.M68020,
                "68040" => M68kCpuModel.M68040,
                _ => throw new ArgumentException("Unsupported CPU qualification target.")
            };
            var image = HunkImage.Load(args[0], NumericFixture.LoadAddress);
            imageHash = image.Sha256;
            var fixture = new NumericFixture(image, model);
            var cases = fixture.Run();
            WriteReport(args[2], new
            {
                schemaVersion = 1, status = "passed", suite = Suite, cpu = args[1],
                imageSha256 = imageHash, imageBytes = new FileInfo(args[0]).Length,
                imageLoads = 1, instructionExecutor = "Copper68k 1.4.0",
                instructionCorePath = typeof(M68kCoreFactory).Assembly.Location,
                instructionCoreSha256 = AssemblyHash(typeof(M68kCoreFactory).Assembly.Location),
                managedExecutorPath = typeof(Program).Assembly.Location,
                managedExecutorSha256 = AssemblyHash(typeof(Program).Assembly.Location),
                hostRuntimeVersion = Environment.Version.ToString(),
                boundaryAddressBits = model == M68kCpuModel.M68000 ? 24 : 32,
                realKickstartExecution = false, realCopperStartExecution = false,
                realDosParser = false,
                originalEvalExecution = false, fullEvalCommand = false,
                shippingOrPureApproval = false, minimumStackQualified = false,
                hostGateways = 0, sharedImageWrites = 0,
                passed = cases.Count, cases
            });
            Console.WriteLine($"PASS {model}: {cases.Count} Eval numeric component invocations; one shared image, no gateways.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"FAIL Eval native component: {error.Message}");
            WriteReport(args[2], new
            {
                schemaVersion = 1, status = "failed", suite = Suite, cpu = args[1],
                imageSha256 = imageHash, failure = error.Message,
                shippingOrPureApproval = false
            });
            return 1;
        }
    }

    private static void WriteReport(string path, object report)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(report,
            new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }

    private static string AssemblyHash(string path) =>
        Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
}

internal sealed record NumericCase(string Name, int Format, ulong Value, uint Flags, string? Expected)
{
    public uint Capacity { get; init; } = 23;
    public uint? Destination { get; init; }
    public uint? MappedBytes { get; init; }
    public uint StackBytes { get; init; } = 4096;
    public uint? BoundaryRegion { get; init; }
}

internal sealed class NumericInvocation(NumericCase definition, int slot)
{
    public NumericCase Definition { get; } = definition;
    public uint Control { get; } = (uint)(0x20000 + slot * 0x1000);
    public uint DefaultDestination { get; } = (uint)(0x30001 + slot * 0x1000);
    public uint Destination => Definition.Destination ?? DefaultDestination;
    public uint Region => Definition.BoundaryRegion ?? DefaultDestination - 17;
    public uint RegionBytes => Definition.BoundaryRegion.HasValue ? 128u : 96u;
    public uint StackTop { get; } = (uint)(0x50000 + slot * 0x10000);
    public uint LowestStackWrite { get; set; } = (uint)(0x50000 + slot * 0x10000);
    public long Instructions { get; set; }
    public byte[] ControlSnapshot { get; set; } = [];
}

internal sealed class NumericFixture(HunkImage image, M68kCpuModel model)
{
    internal const uint LoadAddress = 0x100000;
    private const uint ReturnAddress = 0x2000;
    private readonly NumericTestBus bus = new(image, LoadAddress);

    public List<object> Run()
    {
        var reports = new List<object>();
        // Independent host integer formatting is used only for expected bytes;
        // the guest computes every result with the compiled production helper.
        ulong[] values = [0, 1, 7, 8, 9, 10, 15, 16, 63, 64, 127, 128,
            0xffffffff, 0x100000000, 0x20000000000001, 0x7fffffffffffffff,
            0x8000000000000000, ulong.MaxValue];
        foreach (var value in values)
        for (var format = 0; format < 3; format++)
        for (uint lineFeed = 0; lineFeed < 2; lineFeed++)
        for (uint prefix = 0; prefix < (format == 1 ? 2u : 1u); prefix++)
        {
            var flags = lineFeed | (prefix << 1);
            var expected = Expected(format, value, flags);
            reports.AddRange(Execute([new($"value-{value:x16}-{format}-{flags}",
                format, value, flags, expected)]));
        }

        for (var format = 0; format < 3; format++)
        {
            var value = format == 0 ? 0x8000000000000000UL : ulong.MaxValue;
            uint flags = format == 1 ? 3u : 1u;
            var expected = Expected(format, value, flags);
            var maximum = new NumericCase($"exact-{format}", format, value, flags, expected)
                { Capacity = (uint)expected.Length, StackBytes = 16384 };
            reports.AddRange(Execute([maximum]));
            reports.AddRange(Execute([maximum with { Name = $"short-{format}",
                Capacity = (uint)expected.Length - 1, Expected = null }]));
            reports.AddRange(Execute([maximum with { Name = $"zero-capacity-{format}",
                Capacity = 0, Expected = null }]));
            reports.AddRange(Execute([maximum with { Name = $"null-{format}",
                Destination = 0, Expected = null }]));
            reports.AddRange(Execute([maximum with { Name = $"unmapped-{format}",
                MappedBytes = (uint)expected.Length - 1, Expected = null }]));
            reports.AddRange(Execute([maximum with { Name = $"wrap-{format}",
                Destination = 0xfffffff0, Capacity = 64, Expected = null }]));
            reports.AddRange(Execute([maximum with { Name = $"whole-capacity-{format}",
                Capacity = 24, MappedBytes = 23, Expected = null }]));
        }

        // The MC68000 has a 24-bit physical address bus. Its successful edge
        // writes use that limit; 32-bit APTR wrap rejection still runs on it.
        uint addressLimit = model == M68kCpuModel.M68000 ? 0x00ffffff : uint.MaxValue;
        reports.AddRange(Execute([new("last-bus-address-digit", 0, 0, 0, "0")
            { Destination = addressLimit, Capacity = 1, BoundaryRegion = addressLimit - 127 }]));
        reports.AddRange(Execute([new("last-address-linefeed", 0, 0x8000000000000000UL,
            1, "-9223372036854775808\n")
            { Destination = addressLimit - 20, Capacity = 21, BoundaryRegion = addressLimit - 127 }]));
        reports.AddRange(Execute([new("wrapping-full-capacity", 0, 0, 0, null)
            { Destination = 2, Capacity = uint.MaxValue }]));

        for (var round = 0; round < 3; round++)
        {
            reports.AddRange(Execute([
                new($"overlap-short-{round}", 0, 0x8000000000000000UL, 1, null)
                    { Capacity = 20 },
                new($"overlap-hex-{round}", 1, ulong.MaxValue, 3, "0xffffffffffffffff\n")
                    { StackBytes = 16384 }
            ]));
            reports.AddRange(Execute([
                new($"overlap-decimal-{round}", 0, 0x7fffffffffffffff, 1,
                    "9223372036854775807\n"),
                new($"overlap-octal-{round}", 2, ulong.MaxValue, 1,
                    "1777777777777777777777\n") { StackBytes = 16384 }
            ]));
        }
        bus.AssertImageUnchanged();
        return reports;
    }

    private static string Expected(int format, ulong value, uint flags)
    {
        var number = format switch
        {
            0 => unchecked((long)value).ToString(CultureInfo.InvariantCulture),
            1 => ((flags & 2) != 0 ? "0x" : "") + value.ToString("x", CultureInfo.InvariantCulture),
            2 => Convert.ToString(unchecked((long)value), 8),
            _ => throw new ArgumentException("Unknown numeric format.")
        };
        return number + ((flags & 1) != 0 ? "\n" : "");
    }

    private List<object> Execute(NumericCase[] definitions)
    {
        var invocations = definitions.Select((test, index) => new NumericInvocation(test, index)).ToArray();
        var cores = new List<IM68kCore>();
        try
        {
            foreach (var invocation in invocations)
            {
                bus.Current = invocation;
                bus.Initialize(invocation);
                var core = M68kCoreFactory.Default.Create(model, bus);
                cores.Add(core);
                core.State.StatusRegister = 0;
                core.BeginSubroutine(LoadAddress, invocation.StackTop, ReturnAddress);
                for (var register = 0; register < 8; register++)
                    core.State.D[register] = (uint)(0xde000000 + register * 16);
                for (var register = 0; register < 7; register++)
                    core.State.A[register] = (uint)(0xae000000 + register * 16);
                core.State.D[0] = 36;
                core.State.A[0] = invocation.Control;
            }
            while (cores.Any(core => core.State.ProgramCounter != ReturnAddress))
            for (var index = 0; index < cores.Count; index++)
            {
                var core = cores[index];
                if (core.State.ProgramCounter == ReturnAddress) continue;
                var invocation = invocations[index];
                bus.Current = invocation;
                NumericTestBus.Require(!core.State.Halted && !core.State.Stopped &&
                    ++invocation.Instructions <= 500_000, "CPU stopped or exceeded the instruction bound.");
                try { core.ExecuteInstruction(); }
                catch (Exception error)
                {
                    throw new InvalidOperationException($"{invocation.Definition.Name} PC=${core.State.ProgramCounter:X8}: {error.Message}", error);
                }
            }
            var reports = new List<object>();
            for (var index = 0; index < cores.Count; index++)
            {
                var invocation = invocations[index];
                var core = cores[index];
                var test = invocation.Definition;
                NumericTestBus.Require(core.State.D[0] == (test.Expected is null ? 10u : 0u),
                    $"{test.Name}: wrong result ${core.State.D[0]:X8}.");
                NumericTestBus.Require(core.State.A[7] == invocation.StackTop,
                    $"{test.Name}: unbalanced stack.");
                bus.AssertOutput(invocation);
                reports.Add(new
                {
                    name = test.Name, interleaved = definitions.Length > 1,
                    inputBits = test.Value.ToString("x16", CultureInfo.InvariantCulture),
                    format = test.Format, flags = test.Flags, capacity = test.Capacity,
                    destination = invocation.Destination.ToString("x8", CultureInfo.InvariantCulture),
                    result = core.State.D[0], bytesWritten = test.Expected?.Length ?? 0,
                    outputHex = Convert.ToHexStringLower(Encoding.ASCII.GetBytes(test.Expected ?? "")),
                    instructions = invocation.Instructions, configuredStackBytes = test.StackBytes,
                    stackBytesWritten = invocation.StackTop - invocation.LowestStackWrite
                });
            }
            bus.AssertImageUnchanged();
            return reports;
        }
        finally
        {
            bus.Current = null;
            foreach (var core in cores) core.Dispose();
        }
    }
}
