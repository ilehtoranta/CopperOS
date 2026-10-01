using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record TouchEntryCase(
    string Name,
    bool Verbose = true,
    bool All = false,
    bool Match = true,
    bool Directory = false,
    bool SetFileDateSucceeds = true,
    bool CreateFallback = false,
    bool CtrlC = false,
    int Error = 205,
    int ParserError = 0);

internal sealed class TouchNativeLayout(uint control, uint names)
{
    public uint Control { get; } = control;
    public uint Names { get; } = names;
    public uint Workspace { get; set; }
    public uint Chain => Control + 0x1000;
    public uint Name { get; } = control + 0x400;
}

internal sealed partial class ProbeFixture
{
    public const string TouchEntrySuite =
        "touch-morphos-native-entry-vector-fixture";

    private List<object> RunTouchEntryCases()
    {
        ProbeCase[] cases =
        [
            TouchCase("success", "RAM:file"),
            TouchCase("quiet", "RAM:quiet", verbose: false),
            TouchCase("directory-all", "RAM:dir", all: true, directory: true),
            TouchCase("directory", "RAM:dir-no-all", directory: true),
            TouchCase("no-match-create", "RAM:missing", match: false,
                createFallback: true),
            TouchCase("setfiledate-failure", "RAM:bad",
                setFileDateSucceeds: false, result: DOS.RETURN_FAIL, error: 205),
            TouchCase("break", "RAM:break", ctrlC: true,
                result: DOS.RETURN_FAIL, error: (int)DOS.Error.Break),
            TouchCase("parser-failure", "RAM:file", verbose: true,
                result: DOS.RETURN_FAIL, error: 116, parserError: 116)
        ];
        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "interleaved-left" },
            cases[2] with { Name = "interleaved-right" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase TouchCase(string name, string path,
        bool verbose = true, bool all = false, bool match = true,
        bool directory = false, bool setFileDateSucceeds = true,
        bool createFallback = false, bool ctrlC = false,
        int result = DOS.RETURN_OK, int error = 0, int parserError = 0) =>
        new(name, path + "\n", result, error,
            parserError != 0 ? "" :
            !match && createFallback && verbose ? path + "...created\n" :
            !match || !setFileDateSucceeds ?
                (verbose ? (!match ? path : Leaf(path)) + "...failed\n" : "") :
            verbose ? (directory && all ? Leaf(path) + "...touched\n" :
                directory ? Leaf(path) + "...touched\n" :
                Leaf(path) + "...touched\n") : "")
        {
            Touch = new(path, verbose, all, match, directory,
                setFileDateSucceeds, createFallback, ctrlC, error, parserError)
        };

    private void PrepareTouchEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Touch ??
            throw new InvalidOperationException("Missing Touch definition.");
        var layout = new TouchNativeLayout(invocation.Arguments,
            invocation.Arguments + 0x300);
        invocation.TouchLayout = layout;
        PutTouch(layout.Name, definition.Name);
        Bus.Long(layout.Names, layout.Name);
        Bus.Long(layout.Names + 4, 0);
    }

    private void VerifyTouchEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Touch!;
        var parserFailure = definition.ParserError != 0;
        var matched = !parserFailure && definition.Match;
        var touched = matched && definition.SetFileDateSucceeds;
        var fallback = !parserFailure && !definition.Match &&
            definition.CreateFallback;
        var expectedMatchFirst = parserFailure ? 0 : 1;
        var expectedMatchEnd = parserFailure ? 0 : 1;
        var expectedNext = definition.CtrlC ? 0 : touched && definition.Directory && definition.All
            ? 2 : touched ? 1 : 0;
        Require(invocation.Reads == 1 &&
            invocation.FreeArgs == (parserFailure ? 0 : 1),
            "Touch parser lifetime differs.");
        Require(invocation.Allocations == 2 && invocation.FreeMem == 2,
            "Touch workspace/result cleanup differs.");
        Require(invocation.TouchDateStampCalls == (parserFailure ? 0 : 1),
            "Touch DateStamp count differs.");
        Require(invocation.TouchMatchFirstCalls == expectedMatchFirst &&
            invocation.TouchMatchEndCalls == expectedMatchEnd &&
            invocation.TouchMatchNextCalls == expectedNext,
            $"Touch matcher lifecycle differs for {definition.Name} (first={invocation.TouchMatchFirstCalls}, end={invocation.TouchMatchEndCalls}, next={invocation.TouchMatchNextCalls}, expected={expectedMatchFirst}/{expectedMatchEnd}/{expectedNext}).");
        Require(invocation.TouchSetFileDateCalls == (parserFailure ? 0 : 1),
            $"Touch SetFileDate count differs for {definition.Name} (actual={invocation.TouchSetFileDateCalls}).");
        var openAttempt = !parserFailure &&
            (fallback || !definition.SetFileDateSucceeds);
        Require(invocation.TouchOpenCalls == (openAttempt ? 1 : 0) &&
            invocation.TouchCloseCalls == (fallback ? 1 : 0),
            "Touch create fallback lifetime differs.");
        if (matched)
            Require(invocation.TouchCurrentDirCalls == 2,
                "Touch CurrentDir lifetime differs.");
        else
            Require(invocation.TouchCurrentDirCalls == 0,
                $"Touch unexpectedly changed CurrentDir for {definition.Name} (calls={invocation.TouchCurrentDirCalls}).");
        invocation.TouchLayout = null;
    }

    private void RegisterTouchEntryExec()
    {
        // Touch uses the common Exec AllocMem/FreeMem and SetSignal gateways.
    }

    private void RegisterTouchEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.Touch!;
            Require(Bus.CString(state.D[1]) == NativeMorphOSTouchCommand.Template &&
                state.D[3] == 0 && Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 12,
                "Touch ReadArgs ABI differs.");
            for (var offset = 0u; offset < 12; offset += 4)
                Require(Bus.Long(state.D[2] + offset) == 0,
                    "Touch result slots were not cleared.");
            invocation.Reads++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            var layout = invocation.TouchLayout!;
            Bus.Long(state.D[2], layout.Names);
            Bus.Long(state.D[2] + 4, definition.Verbose ? uint.MaxValue : 0);
            Bus.Long(state.D[2] + 8, definition.All ? uint.MaxValue : 0);
            return Bus.Allocate(invocation, 40, "RDArgs", true);
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FreeArgs++;
            return 0;
        });
        Register(baseAddress, DosLvo.DateStamp, "DateStamp", (state, invocation) =>
        {
            Require(state.D[1] == invocation.TouchLayout!.Workspace + 1318 - 12,
                "Touch DateStamp workspace differs.");
            Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Days, 1);
            Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Minutes, 2);
            Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Ticks, 3);
            invocation.TouchDateStampCalls++;
            return 1;
        });
        Register(baseAddress, DosLvo.MatchFirst, "MatchFirst", (state, invocation) =>
        {
            var definition = invocation.Definition.Touch!;
            var layout = invocation.TouchLayout!;
            var anchor = state.D[2];
            Require(Bus.CString(state.D[1]) == definition.Name &&
                Bus.Memory[anchor + (uint)DosLayout.AnchorPath.Flags] == 0 &&
                Bus.Word(anchor + (uint)DosLayout.AnchorPath.StringLength) == 1024 &&
                Bus.Long(anchor + (uint)DosLayout.AnchorPath.BreakBits) == 4096,
                "Touch MatchFirst AnchorPath policy differs.");
            layout.Workspace = anchor;
            invocation.TouchMatchFirstCalls++;
            if (!definition.Match)
            {
                invocation.IoError = (int)DOS.Error.ObjectNotFound;
                return (uint)DOS.Error.ObjectNotFound;
            }
            Bus.Long(anchor + (uint)DosLayout.AnchorPath.Current, layout.Chain);
            Bus.Long(layout.Chain + (uint)DosLayout.AChain.Lock, 0x5200);
            PutTouch(anchor + (uint)DosLayout.AnchorPath.PathBuffer,
                definition.Name);
            var fib = anchor + (uint)DosLayout.AnchorPath.Info;
            Bus.Long(fib + (uint)FileInfoBlock.DirEntryTypeOffset,
                definition.Directory ? 2u : unchecked((uint)-3));
            PutTouch(fib + (uint)FileInfoBlock.FileNameOffset,
                Leaf(definition.Name));
            invocation.IoError = 0;
            return 0;
        });
        Register(baseAddress, DosLvo.MatchNext, "MatchNext", (state, invocation) =>
        {
            var definition = invocation.Definition.Touch!;
            var anchor = invocation.TouchLayout!.Workspace;
            Require(state.D[1] == anchor, "Touch MatchNext anchor differs.");
            invocation.TouchMatchNextCalls++;
            if (definition.Directory && definition.All &&
                invocation.TouchMatchNextCalls == 1)
            {
                Require((Bus.Memory[anchor + (uint)DosLayout.AnchorPath.Flags] &
                    (byte)AnchorPathFlags.DoDirectory) != 0,
                    "Touch ALL did not request directory descent.");
                Bus.Memory[anchor + (uint)DosLayout.AnchorPath.Flags] =
                    (byte)AnchorPathFlags.DidDirectory;
                return 0;
            }
            invocation.IoError = (int)DOS.Error.NoMoreEntries;
            return (uint)DOS.Error.NoMoreEntries;
        });
        Register(baseAddress, DosLvo.MatchEnd, "MatchEnd", (state, invocation) =>
        {
            Require(state.D[1] == invocation.TouchLayout!.Workspace,
                "Touch MatchEnd anchor differs.");
            invocation.TouchMatchEndCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.CurrentDir, "CurrentDir", (state, invocation) =>
        {
            Require(state.D[1] is 0x5200u or 0x5400u,
                "Touch CurrentDir lock differs.");
            invocation.TouchCurrentDirCalls++;
            return 0x5400;
        });
        Register(baseAddress, DosLvo.SetFileDate, "SetFileDate", (state, invocation) =>
        {
            var definition = invocation.Definition.Touch!;
            Require(state.D[2] == invocation.TouchLayout!.Workspace + 1318 - 12,
                "Touch SetFileDate date pointer differs.");
            invocation.TouchSetFileDateCalls++;
            if (definition.CreateFallback || !definition.SetFileDateSucceeds)
            {
                invocation.IoError = definition.CreateFallback ?
                    (int)DOS.Error.ObjectNotFound : definition.Error;
                return 0;
            }
            invocation.IoError = 0;
            return 1;
        });
        Register(baseAddress, DosLvo.Open, "Open", (state, invocation) =>
        {
            var definition = invocation.Definition.Touch!;
            Require((definition.CreateFallback || !definition.SetFileDateSucceeds) &&
                state.D[2] == (uint)DOS.FileMode.ReadWrite,
                "Touch create fallback Open ABI differs.");
            invocation.TouchOpenCalls++;
            if (definition.CreateFallback)
            {
                invocation.IoError = 0;
                return 0x6100;
            }
            invocation.IoError = definition.Error;
            return 0;
        });
        Register(baseAddress, DosLvo.Close, "Close", (state, invocation) =>
        {
            Require(invocation.Definition.Touch!.CreateFallback &&
                state.D[1] == 0x6100,
                "Touch create fallback Close ABI differs.");
            invocation.TouchCloseCalls++;
            return 1;
        });
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state, invocation) =>
        {
            invocation.Output.Write(Encoding.Latin1.GetBytes(Bus.CString(state.D[1])));
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (_, _) => 0);
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void PutTouch(uint address, string value)
    {
        Encoding.Latin1.GetBytes(value).CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)value.Length] = 0;
    }

}
