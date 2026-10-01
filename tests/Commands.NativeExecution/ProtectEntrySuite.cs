using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record ProtectEntryCase(
    string File,
    string Flags,
    bool Add = false,
    bool Sub = false,
    bool All = false,
    bool Quiet = false,
    bool Match = true,
    bool Directory = false,
    int OldProtection = 0x0f,
    bool SetProtectionSucceeds = true,
    int Error = 205,
    int ParserError = 0,
    int NextError = (int)DOS.Error.NoMoreEntries,
    bool AllocationFailure = false);

internal sealed class ProtectNativeLayout(uint control, uint file, uint flags)
{
    public uint Control { get; } = control;
    public uint File { get; } = file;
    public uint Flags { get; } = flags;
    public uint Anchor { get; set; }
}

internal sealed partial class ProbeFixture
{
    public const string ProtectEntrySuite =
        "protect-morphos-native-entry-vector-fixture";
    public const string Workbench31ProtectEntrySuite =
        "workbench31-protect-native-entry-vector-fixture";

    public static bool IsProtectEntrySuite(string value) =>
        value == ProtectEntrySuite || value == Workbench31ProtectEntrySuite;

    private List<object> RunProtectEntryCases()
    {
        ProbeCase[] cases =
        [
            ProtectCase("success", "RAM:file", "R", DOS.RETURN_OK, 0),
            ProtectCase("add", "RAM:add", "+E", DOS.RETURN_OK, 0,
                add: true, oldProtection: 0x0f),
            ProtectCase("sub", "RAM:sub", "-W", DOS.RETURN_OK, 0,
                sub: true, oldProtection: 0),
            ProtectCase("replace", "RAM:replace", "PA", DOS.RETURN_OK, 0,
                oldProtection: 0x80f0),
            ProtectCase("directory-all", "RAM:dir", "P", DOS.RETURN_OK, 0,
                all: true, directory: true, oldProtection: 0x0f),
            ProtectCase("quiet-all", "RAM:quiet", "P", DOS.RETURN_OK, 0,
                all: true, quiet: true, directory: true),
            ProtectCase("no-match", "RAM:missing", "R", DOS.RETURN_FAIL, 205,
                match: false),
            ProtectCase("set-failure", "RAM:bad", "R", DOS.RETURN_FAIL, 205,
                setProtectionSucceeds: false),
            ProtectCase("break", "RAM:break", "R", DOS.RETURN_WARN,
                (int)DOS.Error.Break, nextError: (int)DOS.Error.Break),
            ProtectCase("invalid-flag", "RAM:invalid", "X", DOS.RETURN_FAIL,
                0),
            ProtectCase("add-and-sub", "RAM:both", "R", DOS.RETURN_FAIL,
                0, add: true, sub: true),
            ProtectCase("parser-failure", "RAM:file", "R", DOS.RETURN_FAIL,
                116, parserError: 116),
        ];
        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "interleaved-left" },
            cases[4] with { Name = "interleaved-right" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase ProtectCase(string name, string file, string flags,
        int result, int error, bool add = false, bool sub = false,
        bool all = false, bool quiet = false, bool match = true,
        bool directory = false, int oldProtection = 0x0f,
        bool setProtectionSucceeds = true, int parserError = 0,
        int nextError = (int)DOS.Error.NoMoreEntries) =>
        new(name, "", result, error,
            !IsValidFlagText(flags) ? "Invalid flag - must be one of HSPARWED\n" :
            add && sub ? "Can't specify both ADD (+) and SUB (-)\n" :
            !match || setProtectionSucceeds ? OutputFor(all, quiet, directory) :
            $"Can't set protection for {file} - ")
        {
            Protect = new(file, flags, add, sub, all, quiet, match, directory,
                oldProtection, setProtectionSucceeds, error, parserError,
                nextError)
        };

    private static string OutputFor(bool all, bool quiet, bool directory) =>
        all && !quiet ? directory ? "     dir (dir)..done\n" :
            "   file..done\n" : "";

    private void PrepareProtectEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Protect ??
            throw new InvalidOperationException("Missing Protect definition.");
        var layout = new ProtectNativeLayout(invocation.Arguments,
            invocation.Arguments + 0x400, invocation.Arguments + 0x500);
        invocation.ProtectLayout = layout;
        PutProtect(layout.File, definition.File);
        PutProtect(layout.Flags, definition.Flags);
    }

    private void VerifyProtectEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Protect ??
            throw new InvalidOperationException("Missing Protect definition.");
        var allocationFailure = definition.AllocationFailure;
        var parserFailure = definition.ParserError != 0;
        var invalidFlags = !IsValidFlagText(definition.Flags);
        var matched = !allocationFailure && !parserFailure && !invalidFlags &&
            !(definition.Add && definition.Sub) && definition.Match;
        Require(invocation.ProtectAllocVecCalls == (allocationFailure ? 1 : 1) &&
            invocation.ProtectFreeVecCalls == 1,
            "Protect AnchorPath lifetime differs.");
        Require(invocation.Reads == (allocationFailure ? 0 : 1) &&
            invocation.FreeArgs == (allocationFailure || parserFailure ? 0 : 1),
            "Protect parser lifetime differs.");
        Require(invocation.Allocations == (allocationFailure ? 1 : 2) &&
            invocation.FreeMem == (allocationFailure ? 0 : 1),
            "Protect allocation lifetime differs.");
        var matcherStarted = !allocationFailure && !parserFailure &&
            !invalidFlags && !(definition.Add && definition.Sub);
        Require(invocation.Events.Count(x => x == "MatchFirst") ==
            (matcherStarted ? 1 : 0) &&
            invocation.Events.Count(x => x == "MatchEnd") ==
            (matcherStarted ? 1 : 0),
            "Protect matcher lifetime differs.");
        var expectedNext = matched && definition.SetProtectionSucceeds
            ? definition.All && definition.Directory ? 2 : 1 : 0;
        var actualNext = invocation.Events.Count(x => x == "MatchNext");
        Require(actualNext == expectedNext,
            $"Protect matcher advance count differs for {definition.File} (expected {expectedNext}, actual {actualNext}).");
        Require(invocation.ProtectSetProtectionCalls == (matched ? 1 : 0),
            "Protect SetProtection count differs.");
        invocation.ProtectLayout = null;
    }

    private void RegisterProtectEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.Protect!;
            var layout = invocation.ProtectLayout!;
            var resultAllocation = Bus.OwnedAllocation(invocation, state.D[2], "Exec");
            Require(Bus.CString(state.D[1]) ==
                NativeMorphOSProtectCommand.Template && state.D[3] == 0 &&
                resultAllocation.Size == 24,
                $"Protect ReadArgs template/result ABI differs (size={resultAllocation.Size}, d0={state.D[0]}, d3={state.D[3]}).");
            for (var offset = 0u; offset < 24; offset += 4)
                Require(Bus.Long(state.D[2] + offset) == 0,
                    "Protect result slots were not cleared.");
            invocation.Reads++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            Bus.Long(state.D[2], layout.File);
            Bus.Long(state.D[2] + 4, layout.Flags);
            Bus.Long(state.D[2] + 8, definition.Add ? uint.MaxValue : 0);
            Bus.Long(state.D[2] + 12, definition.Sub ? uint.MaxValue : 0);
            Bus.Long(state.D[2] + 16, definition.All ? uint.MaxValue : 0);
            Bus.Long(state.D[2] + 20, definition.Quiet ? uint.MaxValue : 0);
            return Bus.Allocate(invocation, 40, "RDArgs", true);
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FreeArgs++;
            return 0;
        });
        Register(baseAddress, DosLvo.MatchFirst, "MatchFirst", (state, invocation) =>
        {
            var definition = invocation.Definition.Protect!;
            var layout = invocation.ProtectLayout!;
            var anchor = state.D[2];
            Require(Bus.CString(state.D[1]) == definition.File &&
                Bus.Memory[anchor + (uint)DosLayout.AnchorPath.Flags] == 0 &&
                Bus.Word(anchor + (uint)DosLayout.AnchorPath.StringLength) == 512 &&
                Bus.Long(anchor + (uint)DosLayout.AnchorPath.BreakBits) == 4096,
                "Protect MatchFirst AnchorPath policy differs.");
            layout.Anchor = anchor;
            if (!definition.Match)
            {
                invocation.IoError = definition.Error;
                return unchecked((uint)definition.Error);
            }
            PutProtect(anchor + (uint)DosLayout.AnchorPath.PathBuffer,
                definition.File);
            var fib = anchor + (uint)DosLayout.AnchorPath.Info;
            Bus.Long(fib + (uint)FileInfoBlock.DirEntryTypeOffset,
                definition.Directory ? 2u : unchecked((uint)-3));
            Bus.Long(fib + (uint)FileInfoBlock.ProtectionOffset,
                unchecked((uint)definition.OldProtection));
            PutProtect(fib + (uint)FileInfoBlock.FileNameOffset,
                Leaf(definition.File));
            return 0;
        });
        Register(baseAddress, DosLvo.MatchNext, "MatchNext", (state, invocation) =>
        {
            var definition = invocation.Definition.Protect!;
            var anchor = state.D[1];
            Require(anchor == invocation.ProtectLayout!.Anchor,
                "Protect MatchNext anchor differs.");
            if (definition.All && definition.Directory &&
                invocation.Events.Count(x => x == "MatchNext") == 1)
            {
                Bus.Memory[anchor + (uint)DosLayout.AnchorPath.Flags] =
                    (byte)AnchorPathFlags.DidDirectory;
                return 0;
            }
            invocation.IoError = definition.NextError;
            return unchecked((uint)definition.NextError);
        });
        Register(baseAddress, DosLvo.MatchEnd, "MatchEnd", (state, invocation) =>
        {
            Require(state.D[1] == invocation.ProtectLayout!.Anchor,
                "Protect MatchEnd anchor differs.");
            return 0;
        });
        Register(baseAddress, DosLvo.SetProtection, "SetProtection",
            (state, invocation) =>
        {
            var definition = invocation.Definition.Protect!;
            var expected = ExpectedProtection((uint)definition.OldProtection,
                definition.Flags, definition.Add, definition.Sub);
            Require(Bus.CString(state.D[1]) == definition.File &&
                unchecked((uint)state.D[2]) == expected,
                "Protect SetProtection ABI or bit mapping differs.");
            invocation.ProtectSetProtectionCalls++;
            invocation.IoError = definition.Error;
            return definition.SetProtectionSucceeds ? 1u : 0u;
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

    private static uint ExpectedProtection(uint oldFlags, string flags,
        bool add, bool sub)
    {
        var parsed = (uint)(FileProtection.Read | FileProtection.Write |
            FileProtection.Delete | FileProtection.Execute);
        var offset = flags.Length > 0 && (flags[0] == '+' || flags[0] == '-') ? 1 : 0;
        foreach (var raw in flags[offset..])
        {
            var value = char.ToUpperInvariant(raw);
            parsed = value switch
            {
                'R' => parsed & ~(uint)FileProtection.Read,
                'W' => parsed & ~(uint)FileProtection.Write,
                'D' => parsed & ~(uint)FileProtection.Delete,
                'E' => parsed & ~(uint)FileProtection.Execute,
                'A' => parsed | (uint)FileProtection.Archive,
                'S' => parsed | (uint)FileProtection.Script,
                'P' => parsed | (uint)FileProtection.Pure,
                'H' => parsed | (1u << 7),
                _ => parsed
            };
        }
        if (parsed != 0x0f)
        {
            if (add) return ((~(~oldFlags | ~parsed) & 0x0f) |
                ((oldFlags | parsed) & ~0x0fu));
            if (sub) return (((oldFlags | ~parsed) & 0x0f) |
                ((oldFlags & ~parsed) & ~0x0fu));
            return (oldFlags & ~0xffu) | parsed;
        }
        return !add && !sub ? 0x0fu : oldFlags;
    }

    private static bool IsValidFlagText(string flags)
    {
        var offset = flags.Length > 0 && (flags[0] == '+' || flags[0] == '-') ? 1 : 0;
        if (offset == flags.Length) return false;
        foreach (var raw in flags[offset..])
            if (!"HSPARWED".Contains(char.ToUpperInvariant(raw))) return false;
        return true;
    }

    private void PutProtect(uint address, string value)
    {
        Encoding.Latin1.GetBytes(value).CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)value.Length] = 0;
    }

}
