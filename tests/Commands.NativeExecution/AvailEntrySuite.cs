using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record AvailEntryCase(bool Chip, bool Fast, bool Total,
    bool Flush, uint ChipAvailable = 1234, uint ChipMaximum = 10000,
    uint ChipLargest = 777, uint FastAvailable = 2345,
    uint FastMaximum = 20000, uint FastLargest = 888,
    uint TotalAvailable = 3579, uint TotalMaximum = 30000,
    uint TotalLargest = 999, int ParserError = 0, bool Human = false,
    uint MaxLocalMemory = 1);

internal sealed partial class ProbeFixture
{
    public const string Workbench31AvailEntrySuite =
        "workbench31-avail-native-entry-vector-fixture";
    public const string MorphOSAvailEntrySuite =
        "morphos-avail-native-entry-vector-fixture";

    public static bool IsAvailEntrySuite(string value) =>
        value == Workbench31AvailEntrySuite || value == MorphOSAvailEntrySuite;

    private List<object> RunWorkbench31AvailEntryCases()
    {
        ProbeCase[] cases =
        [
            new("summary", "", DOS.RETURN_OK, 0,
                "Type Available In-Use Maximum Largest\n" +
                "chip 1234 8766 10000 777\n" +
                "fast 2345 17655 20000 888\n" +
                "total 3579 26421 30000 999\n")
            { Avail = new(false, false, false, false) },
            new("chip", "CHIP\n", DOS.RETURN_OK, 0, "1234\n")
            { Avail = new(true, false, false, false) },
            new("fast", "FAST\n", DOS.RETURN_OK, 0, "2345\n")
            { Avail = new(false, true, false, false) },
            new("total", "TOTAL\n", DOS.RETURN_OK, 0, "3579\n")
            { Avail = new(false, false, true, false) },
            new("chip-precedes-fast", "CHIP FAST\n", DOS.RETURN_OK, 0,
                "1234\n")
            { Avail = new(true, true, false, false) },
            new("flush-is-gated", "FLUSH\n", DOS.RETURN_FAIL,
                (int)DOS.Error.NotImplemented, "")
            { Avail = new(false, false, false, true) },
            new("parser-failure", "CHIP\n", DOS.RETURN_ERROR, 116, "")
            { Avail = new(true, false, false, false, ParserError: 116) },
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { Workbench = true, Avail = new(false, false, false, false) },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { MissingDos = true, Avail = new(false, false, false, false) },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "repeat-summary" },
            cases[2] with { Name = "repeat-fast" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private List<object> RunMorphOSAvailEntryCases()
    {
        ProbeCase[] cases
        = [
            new("summary", "", DOS.RETURN_OK, 0,
                "Type   Available    In-Use   Maximum   Largest\n" +
                "chip 1234 8766 10000 777\n" +
                "fast 2345 17655 20000 888\n" +
                "total 3579 26421 30000 999\n")
            { Avail = new(false, false, false, false) },
            new("chip", "CHIP\n", DOS.RETURN_OK, 0, "1234\n")
            { Avail = new(true, false, false, false) },
            new("fast", "FAST\n", DOS.RETURN_OK, 0, "2345\n")
            { Avail = new(false, true, false, false) },
            new("total", "TOTAL\n", DOS.RETURN_OK, 0, "3579\n")
            { Avail = new(false, false, true, false) },
            new("human-chip", "CHIP HUMAN\n", DOS.RETURN_OK, 0, "1.2K\n")
            { Avail = new(true, false, false, false, Human: true) },
            new("human-summary", "HUMAN\n", DOS.RETURN_OK, 0,
                "Type   Available    In-Use   Maximum   Largest\n" +
                "chip 1.2K 8.6K 9.8K 777B\n" +
                "fast 2.3K 17.2K 19.5K 888B\n" +
                "total 3.5K 25.8K 29.3K 999B\n")
            { Avail = new(false, false, false, false, Human: true) },
            new("fake-chip-memory", "", DOS.RETURN_OK, 0,
                "Type   Available    In-Use   Maximum   Largest\n" +
                "chip 0 0 0 0\n" +
                "fast 2345 17655 20000 888\n" +
                "total 3579 16389 19968 999\n")
            { Avail = new(false, false, false, false, MaxLocalMemory: 0) },
            new("multiple-selectors", "CHIP FAST\n", DOS.RETURN_WARN, 0,
                "only one of CHIP, FAST, or TOTAL allowed\n")
            { Avail = new(true, true, false, false) },
            new("flush", "FLUSH\n", DOS.RETURN_OK, 0,
                "Type   Available    In-Use   Maximum   Largest\n" +
                "chip 1234 8766 10000 777\n" +
                "fast 2345 17655 20000 888\n" +
                "total 3579 26421 30000 999\n")
            { Avail = new(false, false, false, true) },
            new("parser-failure", "CHIP\n", DOS.RETURN_ERROR, 116, "")
            { Avail = new(true, false, false, false, ParserError: 116) }
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "repeat-summary" },
            cases[2] with { Name = "repeat-fast" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void VerifyWorkbench31AvailEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Avail!;
        if (invocation.Definition.Workbench || invocation.Definition.MissingDos)
        {
            Require(invocation.Reads == 0 && invocation.FreeArgs == 0 &&
                invocation.Allocations == 0 && invocation.FreeMem == 0 &&
                invocation.Events.Count(x => x == "AvailMem") == 0 &&
                invocation.Events.Count(x => x == "PutStr") == 0 &&
                invocation.Events.Count(x => x == "VPrintf") == 0,
                "Avail startup boundary reached the command body.");
            return;
        }
        var parserFail = definition.ParserError != 0;
        var selection = definition.Chip || definition.Fast || definition.Total;
        var flush = definition.Flush;
        var summary = !selection && !flush && !parserFail;
        var expectedAllocations = parserFail ? 1 : flush ? 1 :
            selection ? 2 : 2;
        var expectedFrees = expectedAllocations;
        Require(invocation.Reads == 1 &&
            invocation.FreeArgs == (parserFail ? 0 : 1),
            "Avail parser ownership differs.");
        Require(invocation.Allocations == expectedAllocations &&
            invocation.FreeMem == expectedFrees,
            "Avail allocation cleanup differs.");
        Require(invocation.Events.Count(x => x == "AvailMem") ==
            (summary ? 9 : selection && !flush && !parserFail ? 1 : 0),
            "AvailMem query count differs.");
        Require(invocation.Events.Count(x => x == "PutStr") ==
            (summary ? 1 : 0), "Avail header output count differs.");
        Require(invocation.Events.Count(x => x == "VPrintf") ==
            (summary ? 3 : selection && !flush && !parserFail ? 1 : 0),
            "Avail formatted output count differs.");
    }

    private void VerifyMorphOSAvailEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Avail!;
        var parserFail = definition.ParserError != 0;
        var selection = definition.Chip || definition.Fast || definition.Total;
        var multiple = definition.Chip && definition.Fast ||
            definition.Chip && definition.Total || definition.Fast && definition.Total;
        var summary = !selection && !parserFail;
        Require(invocation.Reads == 1 &&
            invocation.FreeArgs == (parserFail ? 0 : 1),
            "MorphOS Avail parser ownership differs.");
        Require(invocation.Allocations == 1 && invocation.FreeMem == 1,
            $"MorphOS Avail allocation cleanup differs ({invocation.Definition.Name}: allocations={invocation.Allocations}, frees={invocation.FreeMem}).");
        Require(invocation.Events.Count(x => x == "AvailMem") ==
            (summary ? 9 : selection && !multiple && !parserFail ? 1 : 0),
            "MorphOS AvailMem query count differs.");
        Require(invocation.Events.Count(x => x == "Forbid") ==
            (summary ? 1 : 0) && invocation.Events.Count(x => x == "Permit") ==
            (summary ? 1 : 0), "MorphOS Avail lock scope differs.");
        Require(invocation.Events.Count(x => x == "VPrintf") ==
            (summary ? definition.Human ? 12 : 3 :
                selection && !multiple && !parserFail ? 1 : 0),
            "MorphOS Avail formatted output count differs.");
        var putStr = summary ? (definition.Human ? 19 : 1) :
            selection && !multiple && !parserFail ? 1 :
            multiple ? 1 : 0;
        Require(invocation.Events.Count(x => x == "PutStr") == putStr,
            $"MorphOS Avail output calls differ ({invocation.Definition.Name}: expected {putStr}, actual {invocation.Events.Count(x => x == "PutStr")}).");
        Require(invocation.Events.Count(x => x == "PrintFault") ==
            (parserFail ? 1 : 0),
            "MorphOS Avail diagnostic count differs.");
    }

    private void RegisterWorkbench31AvailDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.Avail!;
            Require(Bus.CString(state.D[1]) ==
                "CHIP/S,FAST/S,TOTAL/S,FLUSH/S" && state.D[3] == 0,
                "Avail template or ABI differs.");
            Require(Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 16,
                "Avail result storage differs.");
            invocation.Reads++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            var rdArgs = Bus.Allocate(invocation, 40, "AvailRDArgs", true);
            Bus.Long(state.D[2], definition.Chip ? 1u : 0u);
            Bus.Long(state.D[2] + 4, definition.Fast ? 1u : 0u);
            Bus.Long(state.D[2] + 8, definition.Total ? 1u : 0u);
            Bus.Long(state.D[2] + 12, definition.Flush ? 1u : 0u);
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "AvailRDArgs");
            invocation.FreeArgs++;
            return 0;
        });
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state, invocation) =>
        {
            Require(Bus.CString(state.D[1]) ==
                "Type Available In-Use Maximum Largest\n",
                "Avail header format differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(
                "Type Available In-Use Maximum Largest\n"));
            return 0;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) =>
        {
            var definition = invocation.Definition.Avail!;
            var format = Bus.CString(state.D[1]);
            var args = state.D[2];
            if (format == "%ld\n")
            {
                var value = Bus.Long(args);
                var expected = definition.Chip ? definition.ChipAvailable :
                    definition.Fast ? definition.FastAvailable :
                    definition.TotalAvailable;
                Require(value == expected, "Avail selected value differs.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    $"{value}\n"));
                return 0;
            }
            Require(format == "%s %ld %ld %ld %ld\n",
                "Avail row format differs.");
            var type = Bus.CString(Bus.Long(args));
            var available = Bus.Long(args + 4);
            var inUse = Bus.Long(args + 8);
            var maximum = Bus.Long(args + 12);
            var largest = Bus.Long(args + 16);
            var expectedRow = type switch
            {
                "chip" => (definition.ChipAvailable, definition.ChipMaximum,
                    definition.ChipLargest),
                "fast" => (definition.FastAvailable, definition.FastMaximum,
                    definition.FastLargest),
                "total" => (definition.TotalAvailable, definition.TotalMaximum,
                    definition.TotalLargest),
                _ => throw new InvalidOperationException("Unknown Avail row.")
            };
            Require(available == expectedRow.Item1 &&
                inUse == expectedRow.Item2 - expectedRow.Item1 &&
                maximum == expectedRow.Item2 && largest == expectedRow.Item3,
                "Avail row values differ.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(
                $"{type} {available} {inUse} {maximum} {largest}\n"));
            return 0;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr",
            (_, invocation) => unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void RegisterMorphOSAvailDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.Avail!;
            Require(Bus.CString(state.D[1]) == NativeMorphOSAvailCommand.Template &&
                state.D[3] == 0, "MorphOS Avail template or ABI differs.");
            Require(Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size ==
                NativeMorphOSAvailCommand.ResultCount * 4,
                "MorphOS Avail result storage differs.");
            invocation.Reads++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            var rdArgs = Bus.Allocate(invocation, 40, "AvailRDArgs", true);
            Bus.Long(state.D[2], definition.Chip ? 1u : 0u);
            Bus.Long(state.D[2] + 4, definition.Fast ? 1u : 0u);
            Bus.Long(state.D[2] + 8, definition.Total ? 1u : 0u);
            Bus.Long(state.D[2] + 12, definition.Flush ? 1u : 0u);
            Bus.Long(state.D[2] + 16, definition.Human ? 1u : 0u);
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "AvailRDArgs");
            invocation.FreeArgs++;
            return 0;
        });
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state, invocation) =>
        {
            var text = Bus.CString(state.D[1]);
            invocation.Output.Write(Encoding.Latin1.GetBytes(text));
            return 0;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) =>
        {
            var definition = invocation.Definition.Avail!;
            var format = Bus.CString(state.D[1]);
            var args = state.D[2];
            if (format == "%lu")
            {
                invocation.Output.Write(Encoding.Latin1.GetBytes($"{Bus.Long(args)}"));
                return 0;
            }
            if (format == "%lu%s")
            {
                var suffix = Bus.CString(Bus.Long(args + 8));
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    $"{Bus.Long(args)}{suffix}"));
                return 0;
            }
            if (format == "%lu.%lu%s")
            {
                var suffix = Bus.CString(Bus.Long(args + 8));
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    $"{Bus.Long(args)}.{Bus.Long(args + 4)}{suffix}"));
                return 0;
            }
            Require(format == "%s %lu %lu %lu %lu\n",
                "MorphOS Avail row format differs.");
            var type = Bus.CString(Bus.Long(args));
            invocation.Output.Write(Encoding.Latin1.GetBytes(
                $"{type} {Bus.Long(args + 4)} {Bus.Long(args + 8)} " +
                $"{Bus.Long(args + 12)} {Bus.Long(args + 16)}\n"));
            return 0;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr",
            (_, invocation) => unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                var definition = invocation.Definition.Avail!;
                Require(definition.ParserError != 0 &&
                    unchecked((int)state.D[1]) == definition.ParserError &&
                    Bus.CString(state.D[2]) == "Avail",
                    "MorphOS Avail PrintFault arguments differ.");
                return 0;
            });
    }
}
