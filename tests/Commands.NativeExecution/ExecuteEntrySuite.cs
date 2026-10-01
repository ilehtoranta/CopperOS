using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amiga;
using Copper68k;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed partial class Invocation
{
    /// <summary>Execute turns DOS requesters off (pr_WindowPtr = -1) while it probes T:.</summary>
    public bool AllowsWindowPointerWrite { get; set; }
}

/// <summary>
/// Workbench 3.1 <c>C:Execute</c> entry (src/Commands/Execute/Entry/ExecuteEntry.cs)
/// against supplied Exec/DOS vectors, an in-memory file table and a CLI
/// structure. Checks the work file Execute writes, the CLI input handoff
/// (cli_CurrentInput, cli_Interactive, cli_CommandFile), nested-script
/// continuation, work-file naming and the T:/:T/ fallback with requesters
/// suppressed, error paths, and that every allocation, lock and handle it
/// does not hand to the CLI is released.
///
/// The fixture's ReadItem, ReadArgs and FindArg are models, not the ROM
/// parser; ReadArgs returns the case's values. PrintFault is recorded as
/// (code, header) because the fixture has no fault-string table. Parity with
/// the original binary is covered by the WinUAE reference replays, not here.
/// </summary>
internal static class ExecuteEntrySuite
{
    public const string Suite = "wb31-execute-native-entry-vector-fixture";

    private const string Script = "S:t1";
    private const string First = "T:Command-03-T01";

    internal sealed record Case(string Id, string Arguments)
    {
        public Dictionary<string, string> Files { get; init; } = new();
        public string? KeyTemplate { get; init; }
        public string?[] Values { get; init; } = [];
        public bool ReadArgsFails { get; init; }
        public int Result { get; init; }
        public int Error { get; init; }
        public string Output { get; init; } = "";
        public (int Code, string? Header)[] Faults { get; init; } = [];
        public string? WorkFile { get; init; }
        public string WorkText { get; init; } = "";
        public string[] Deleted { get; init; } = [];
        public bool TAssigned { get; init; } = true;
        public bool ColonT { get; init; } = true;
        public string? CallerInput { get; init; }
        public int CallerOffset { get; init; }
        public string CommandFile { get; init; } = "";
        public bool NoCli { get; init; }
        public bool MissingDos { get; init; }
        public bool Workbench { get; init; }
        public bool AllocationFailure { get; init; }
        public int? EntryLength { get; init; }
    }

    private static Case Ok(string id, string script, string work, string arguments = Script + "\n",
        string? key = null, string?[]? values = null) =>
        new(id, arguments)
        {
            Files = new() { [Script] = script }, KeyTemplate = key, Values = values ?? [],
            WorkFile = First, WorkText = work
        };

    private static Case Fails(string id, string script, string output, string? key = null,
        string?[]? values = null, string arguments = Script + "\n") =>
        new(id, arguments)
        {
            Files = new() { [Script] = script }, KeyTemplate = key, Values = values ?? [],
            Result = 10, Output = output
        };

    internal static readonly Case[] Cases =
    [
        Ok("key-positional", ".KEY a,b\necho <a> <b>\n", "echo x y\n",
            Script + " x y\n", "a,b", ["x", "y"]),
        Ok("no-key-copies-verbatim", "echo <a>\necho two\n", "echo <a>\necho two\n"),
        Ok("default-space", ".KEY a\n.DEF a dflt\necho <a>\n", "echo dflt\n", key: "a", values: [null]),
        Ok("default-equals", ".KEY a\n.DEF a=dflt\necho <a>\n", "echo dflt\n", key: "a", values: [null]),
        Ok("argument-overrides-default", ".KEY a\n.DEF a dflt\necho <a>\n", "echo given\n",
            Script + " given\n", "a", ["given"]),
        Ok("first-default-wins", ".KEY a\n.DEF a one\n.DEF a two\necho <a>\n", "echo one\n",
            key: "a", values: [null]),
        Ok("undeclared-default-ignored", ".KEY a\n.DEF zz q\necho <a>|<zz>\n", "echo |<zz>\n",
            key: "a", values: [null]),
        Ok("bare-default-ignored", ".KEY a\n.DEF\necho <a>x\n", "echo x\n", key: "a", values: [null]),
        Ok("default-first-token", ".KEY a\n.DEF a one two\necho <a>\n", "echo one\n",
            key: "a", values: [null]),
        Ok("empty-default-drops-line", ".KEY a\n.DEF a \"\"\necho <a>\necho kept\n", "echo kept\n",
            key: "a", values: [null]),
        Ok("bra-ket-later-lines", ".KEY a\necho <a>\n.BRA {\n.KET }\necho {a} <a>\n",
            "echo x\necho x <a>\n", Script + " x\n", "a", ["x"]),
        Ok("inline-dollar-default", ".KEY a\necho <a$zz>\n", "echo zz\n", key: "a", values: [null]),
        Ok("dollar-directive", ".KEY a\n.DOLLAR #\necho <a#zz> <a$q>\n", "echo zz <a$q>\n",
            key: "a", values: [null]),
        Ok("dot-directive", ".KEY a\n.DOT ;\n;DEF a v\n.x stays\necho <a>\n", ".x stays\necho v\n",
            key: "a", values: [null]),
        Ok("dot-comments-dropped", ". comment\n.\necho hi\n", "echo hi\n"),
        Ok("switch-number-multi", ".KEY f/S,n/N,m/M\necho <f> <n> <m>\n", "echo f -7 p q\n",
            Script + " f -7 p q\n", "f/S,n/N,m/M", ["-", "-7", "p q"]),
        Ok("key-alias", ".KEY name=n\necho <n> <NAME>\n", "echo v v\n", Script + " v\n",
            "name=n", ["v"]),
        Ok("tail-without-newline", ".KEY a\necho [<a>]\n", "echo []\n", Script, "a", [null]),
        Fails("default-equals-x-fails", ".KEY a\n.DEF =x\necho no\n",
            "Invalid directive argument\n", "a", [null]),
        Fails("invalid-directive", "echo before\n.FOO\necho after\n", "Invalid directive\n"),
        Fails("second-key-fails", ".KEY a\n.KEY b\necho no\n", "More than one .KEY directive\n",
            "a", [null]),
        Fails("readargs-failure", ".KEY a/A\necho <a>\n",
            "Parameters unsuitable for key \"a/A\"\n", "a/A") with { ReadArgsFails = true },
        new("missing-script", "S:nope\n")
        {
            Result = 20, Error = 205, Faults = [(205, "S:nope")]
        },
        new("missing-file-argument", "\n")
        {
            Result = 20, Error = 116, Faults = [(116, null)]
        },
        Ok("t-unassigned-uses-colon-t", "echo hi\n", "echo hi\n") with
        {
            TAssigned = false, WorkFile = ":T/Command-03-T01"
        },
        new("existing-work-name-skipped", Script + "\n")
        {
            Files = new() { [Script] = "echo hi\n", [First] = "old\n" },
            WorkFile = "T:Command-03-T02", WorkText = "echo hi\n"
        },
        new("work-file-create-failure", Script + "\n")
        {
            Files = new() { [Script] = "echo hi\n" }, TAssigned = false, ColonT = false,
            Result = 20, Error = 205, Faults = [(205, "Execute: can't create work file")]
        },
        new("nested-work-file-continues-and-is-deleted", Script + "\n")
        {
            Files = new()
            {
                [Script] = "echo inner\n",
                ["T:Command-03-T05"] = "Execute S:t1\necho after\n"
            },
            CallerInput = "T:Command-03-T05", CallerOffset = 13, CommandFile = "T:Command-03-T05",
            WorkFile = First, WorkText = "echo inner\necho after\n", Deleted = ["T:Command-03-T05"]
        },
        new("nested-user-script-continues-and-is-kept", Script + "\n")
        {
            Files = new()
            {
                [Script] = "echo inner\n",
                ["S:caller"] = "Execute S:t1\necho after\n"
            },
            CallerInput = "S:caller", CallerOffset = 13, CommandFile = "S:caller",
            WorkFile = First, WorkText = "echo inner\necho after\n"
        },
        new("no-cli", Script + "\n")
        {
            Files = new() { [Script] = "echo hi\n" }, NoCli = true, Result = 20, Error = 212
        },
        new("allocation-failure", Script + "\n")
        {
            Files = new() { [Script] = "echo hi\n" }, AllocationFailure = true,
            Result = 20, Error = 103, Faults = [(103, null)]
        },
        new("missing-dos", Script + "\n")
        {
            Files = new() { [Script] = "echo hi\n" }, MissingDos = true, Result = 20, Error = 122
        },
        new("workbench", "")
        {
            Files = new() { [Script] = "echo hi\n" }, Workbench = true, Result = 10, Error = 212
        },
        new("negative-entry-length", "")
        {
            Files = new() { [Script] = "echo hi\n" }, EntryLength = -1, Result = 10, Error = 120
        },
    ];

    public static int Run(string[] args)
    {
        var path = Path.GetFullPath(args[2]);
        var observations = new List<object>();
        string? failure = null, hash = null;
        try
        {
            hash = Hash(args[0]);
            var model = args[1] switch
            {
                "68000" => M68kCpuModel.M68000, "68020" => M68kCpuModel.M68020,
                "68040" => M68kCpuModel.M68040, _ => throw new ArgumentException("Unknown CPU.")
            };
            var image = HunkImage.Load(args[0], Fixture.Load);
            foreach (var test in Cases) observations.Add(new Fixture(image, model, test).Execute());
            Require(Hash(args[0]) == hash, "HUNK changed during execution.");
        }
        catch (Exception error) { failure = error.ToString(); }
        var passed = failure is null && observations.Count == Cases.Length;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            schemaVersion = 1, suite = Suite, status = passed ? "passed" : "failed", failure,
            cpu = args[1], imageSha256 = hash,
            managedExecutorSha256 = Hash(typeof(Program).Assembly.Location),
            instructionCoreSha256 = Hash(typeof(M68kCoreFactory).Assembly.Location),
            realKickstartExecution = false, realDosParser = false, realDosIo = false,
            referenceCommandBehavior = false, shippingOrPureApproval = false,
            passed = passed ? observations.Count : 0,
            observations
        }, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        if (passed)
            Console.WriteLine($"PASS {args[1]} {Suite}: {observations.Count} native invocations; no leaked memory, locks or handles; no image writes.");
        else
            Console.Error.WriteLine($"FAIL native command qualification: {Suite} {observations.Count}/{Cases.Length}\n{failure}");
        return passed ? 0 : 1;
    }

    private static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

    private sealed class Fixture
    {
        public const uint Load = 0x100000;
        private const uint Return = 0x2000, ExecBase = 0x4000;
        private const uint WorkBytes = 4672, MemPublicClear = 0x10001;
        private const uint StandardInput = 0x3bfff, InitialWindow = 0x00012340;
        private const uint TaskNumber = 3;

        private readonly CommandTestBus bus = new();
        private readonly Invocation owner;
        private readonly M68kCpuModel model;
        private readonly Case test;
        private readonly int codeLength;
        private readonly HashSet<uint> gateways = [];
        private readonly List<string> events = [];
        private readonly List<(int Code, string? Header)> faults = [];
        private readonly StringBuilder output = new();
        private readonly Dictionary<string, List<byte>> files = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<uint, Handle> handles = [];
        private readonly Dictionary<uint, string> locks = [];
        private readonly uint cli, commandFile, dosBase;
        private uint nextHandle = 0x3c000, nextLock = 0x3d000;
        private uint work, rdArgs, argValues, callerHandle;
        private int itemEnd = -1, opens, closes, forbids, replies, readArgs, freeArgs;

        private sealed class Handle(string name, bool write, int position)
        {
            public string Name { get; } = name;
            public bool Write { get; } = write;
            public int Position { get; set; } = position;
        }

        public Fixture(HunkImage image, M68kCpuModel model, Case test)
        {
            this.model = model;
            this.test = test;
            codeLength = image.Code.Length;
            owner = new Invocation(new ProbeCase(test.Id, test.Arguments, test.Result, test.Error, test.Output)
            {
                StackBytes = 4096, MissingDos = test.MissingDos, WritesOwnProcessError = test.MissingDos,
                Workbench = test.Workbench
            }, 0) { AllowsWindowPointerWrite = true };
            dosBase = owner.DosBase;
            bus.Current = owner;
            bus.LoadImage(Load, image.Code, image.ReadOnlyRanges, image.WritableRanges);
            bus.Long(4, ExecBase);
            foreach (var (name, text) in test.Files) files[name] = [.. Encoding.Latin1.GetBytes(text)];

            cli = bus.Allocate(owner, 64, "Cli", true);
            commandFile = bus.Allocate(owner, 40, "CommandFile", true);
            PutBstr(commandFile, test.CommandFile);
            bus.Long(cli + (uint)DosLayout.CommandLineInterface.CommandFile, commandFile >> 2);
            bus.Long(cli + (uint)DosLayout.CommandLineInterface.StandardInput, StandardInput);
            if (test.CallerInput is { } caller)
            {
                callerHandle = NewHandle(caller, false, test.CallerOffset);
                bus.Long(cli + (uint)DosLayout.CommandLineInterface.CurrentInput, callerHandle);
                bus.Long(cli + (uint)DosLayout.CommandLineInterface.Interactive, 0);
            }
            else
            {
                bus.Long(cli + (uint)DosLayout.CommandLineInterface.CurrentInput, StandardInput);
                bus.Long(cli + (uint)DosLayout.CommandLineInterface.Interactive, uint.MaxValue);
            }

            var process = owner.Process;
            bus.Long(process + (uint)DosLayout.Process.CommandLineInterface, test.Workbench ? 0 : cli >> 2);
            bus.Long(process + (uint)DosLayout.Process.TaskNumber, TaskNumber);
            bus.Long(process + (uint)DosLayout.Process.WindowPointer, InitialWindow);
            bus.Long(process + (uint)DosLayout.Process.Result2, Invocation.InitialIoError);
            var arguments = Encoding.Latin1.GetBytes(test.Arguments);
            arguments.CopyTo(bus.Memory.AsSpan((int)owner.Arguments));
            bus.Memory[owner.Arguments + (uint)arguments.Length] = 0;
            RegisterExec();
            RegisterDos();
        }

        // ---- Vectors ------------------------------------------------------

        private void RegisterExec()
        {
            Register(ExecBase, ExecLvo.FindTask, "FindTask", s =>
            {
                Require(s.A[1] == 0, "FindTask is not FindTask(NULL).");
                return owner.Process;
            });
            Register(ExecBase, ExecLvo.WaitPort, "WaitPort", s =>
            {
                Require(test.Workbench && s.A[0] == owner.Port, "WaitPort outside Workbench startup.");
                return owner.Message;
            });
            Register(ExecBase, ExecLvo.GetMsg, "GetMsg", s =>
            {
                Require(test.Workbench && s.A[0] == owner.Port, "GetMsg outside Workbench startup.");
                return owner.Message;
            });
            Register(ExecBase, ExecLvo.Forbid, "Forbid", _ => { forbids++; return 0; });
            Register(ExecBase, ExecLvo.ReplyMsg, "ReplyMsg", s =>
            {
                Require(s.A[1] == owner.Message && forbids == 1 && closes == opens,
                    "WBStartup reply is not the final step under Forbid.");
                replies++;
                return 0;
            });
            Register(ExecBase, ExecLvo.OpenLibrary, "OpenLibrary", s =>
            {
                Require(bus.CString(s.A[1]) == "dos.library" && s.D[0] == 36, "OpenLibrary ABI differs.");
                if (test.MissingDos) return 0;
                opens++;
                return dosBase;
            });
            Register(ExecBase, ExecLvo.CloseLibrary, "CloseLibrary", s =>
            {
                Require(s.A[1] == dosBase && closes++ == 0, "CloseLibrary differs or repeats.");
                Require(work == 0 && rdArgs == 0 && locks.Count == 0,
                    "dos.library closed while command resources are live.");
                return 0;
            });
            Register(ExecBase, ExecLvo.AllocMem, "AllocMem", s =>
            {
                Require(s.D[0] == WorkBytes && s.D[1] == MemPublicClear && work == 0,
                    "Execute work-area allocation differs.");
                if (test.AllocationFailure) return 0;
                return work = bus.Allocate(owner, WorkBytes, "Work", true);
            });
            Register(ExecBase, ExecLvo.FreeMem, "FreeMem", s =>
            {
                Require(s.A[1] == work && rdArgs == 0, "Work area freed early or wrongly.");
                bus.Release(owner, work, "Work", s.D[0]);
                work = 0;
                return 0;
            });
        }

        private void RegisterDos()
        {
            Register(dosBase, DosLvo.Cli, "Cli", _ => test.NoCli ? 0 : cli);
            Register(dosBase, DosLvo.IoErr, "IoErr", _ => unchecked((uint)owner.IoError));
            Register(dosBase, DosLvo.SetIoErr, "SetIoErr", s =>
            {
                var old = owner.IoError;
                owner.IoError = unchecked((int)s.D[1]);
                return unchecked((uint)old);
            });
            Register(dosBase, DosLvo.ReadItem, "ReadItem", s => ReadItem(s.D[1], s.D[2], s.D[3]));
            Register(dosBase, DosLvo.AllocDosObject, "AllocDosObject", s =>
            {
                Require(s.D[1] == 5 && s.D[2] == 0 && rdArgs == 0, "AllocDosObject(DOS_RDARGS) differs.");
                return rdArgs = bus.Allocate(owner, 32, "RDArgs", true);
            });
            Register(dosBase, DosLvo.FreeDosObject, "FreeDosObject", s =>
            {
                Require(s.D[1] == 5 && s.D[2] == rdArgs && argValues == 0,
                    "FreeDosObject differs, or ReadArgs results are still live.");
                bus.Release(owner, rdArgs, "RDArgs");
                rdArgs = 0;
                return 0;
            });
            Register(dosBase, DosLvo.ReadArgs, "ReadArgs", s => ReadArgs(s.D[1], s.D[2], s.D[3]));
            Register(dosBase, DosLvo.FreeArgs, "FreeArgs", s =>
            {
                Require(s.D[1] == rdArgs && argValues != 0 && freeArgs++ == 0, "FreeArgs differs or repeats.");
                bus.Release(owner, argValues, "ArgValues");
                argValues = 0;
                return 0;
            });
            Register(dosBase, DosLvo.FindArg, "FindArg", s =>
                unchecked((uint)FindArg(bus.CString(s.D[1]), bus.CString(s.D[2]))));
            Register(dosBase, DosLvo.Open, "Open", s => Open(bus.CString(s.D[1]), unchecked((int)s.D[2])));
            Register(dosBase, DosLvo.Close, "Close", s =>
            {
                Require(handles.Remove(s.D[1]), $"Close of a handle that is not open (${s.D[1]:X8}).");
                return uint.MaxValue;
            });
            Register(dosBase, DosLvo.Lock, "Lock", s =>
            {
                var name = bus.CString(s.D[1]);
                Require(unchecked((int)s.D[2]) == -2, "Lock mode is not SHARED_LOCK.");
                RequireQuietProbe(name);
                if (!Reachable(name, out var error) || !files.ContainsKey(name))
                {
                    owner.IoError = error != 0 ? error : 205;
                    return 0;
                }
                locks.Add(++nextLock, name);
                return nextLock;
            });
            Register(dosBase, DosLvo.UnLock, "UnLock", s =>
            {
                Require(locks.Remove(s.D[1]), "UnLock of a lock that is not held.");
                return 0;
            });
            Register(dosBase, DosLvo.DeleteFile, "DeleteFile", s =>
            {
                var name = bus.CString(s.D[1]);
                Require(handles.Values.All(h => !string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase)),
                    $"DeleteFile({name}) while the file is still open.");
                if (!files.Remove(name)) { owner.IoError = 205; return 0; }
                return uint.MaxValue;
            });
            Register(dosBase, DosLvo.FGets, "FGets", s =>
            {
                var handle = ReadHandle(s.D[1]);
                bus.OwnedAllocationContaining(owner, s.D[2], "Work");
                var data = files[handle.Name];
                if (handle.Position >= data.Count) return 0;
                var count = 0u;
                while (count + 1 < s.D[3] && handle.Position < data.Count)
                {
                    var c = data[handle.Position++];
                    bus.Memory[s.D[2] + count++] = c;
                    if (c == (byte)'\n') break;
                }
                bus.Memory[s.D[2] + count] = 0;
                return s.D[2];
            });
            Register(dosBase, DosLvo.FRead, "FRead", s =>
            {
                var handle = ReadHandle(s.D[1]);
                Require(s.D[3] == 1, "FRead block length differs.");
                bus.OwnedAllocationContaining(owner, s.D[2], "Work");
                var data = files[handle.Name];
                var count = (int)Math.Min(s.D[4], (uint)Math.Max(0, data.Count - handle.Position));
                for (var i = 0; i < count; i++) bus.Memory[s.D[2] + (uint)i] = data[handle.Position++];
                return (uint)count;
            });
            Register(dosBase, DosLvo.FWrite, "FWrite", s =>
            {
                Require(handles.TryGetValue(s.D[1], out var handle) && handle!.Write, "FWrite to a non-write handle.");
                Require(s.D[3] == 1, "FWrite block length differs.");
                var data = files[handle!.Name];
                for (var i = 0u; i < s.D[4]; i++) data.Add(bus.Memory[s.D[2] + i]);
                return s.D[4];
            });
            Register(dosBase, DosLvo.VPrintf, "VPrintf", s =>
            {
                output.Append(Format(bus.CString(s.D[1]), s.D[2]));
                return 0;
            });
            Register(dosBase, DosLvo.PrintFault, "PrintFault", s =>
            {
                faults.Add((unchecked((int)s.D[1]), s.D[2] == 0 ? null : bus.CString(s.D[2])));
                return uint.MaxValue;
            });
        }

        // ---- Models -------------------------------------------------------

        /// <summary>ReadItem over a CSource: blanks skipped, quoted or unquoted item, CurChr left at the terminator.</summary>
        private uint ReadItem(uint buffer, uint max, uint source)
        {
            Require(itemEnd < 0 && max == 255 && source != 0, "ReadItem ABI differs or is repeated.");
            bus.OwnedAllocationContaining(owner, buffer, "Work");
            bus.OwnedAllocationContaining(owner, source, "Work");
            var text = bus.Long(source);
            var length = (int)bus.Long(source + 4);
            var position = (int)bus.Long(source + 8);
            Require(text == owner.Arguments && length == (test.EntryLength ?? test.Arguments.Length) &&
                position == 0, "ReadItem CSource does not describe the command tail.");
            while (position < length && bus.Memory[text + (uint)position] is (byte)' ' or (byte)'\t') position++;
            uint result;
            var count = 0u;
            if (position >= length || bus.Memory[text + (uint)position] == (byte)'\n') result = 0;
            else if (bus.Memory[text + (uint)position] == (byte)'"')
            {
                position++;
                while (position < length && bus.Memory[text + (uint)position] is not ((byte)'"' or (byte)'\n'))
                    bus.Memory[buffer + count++] = bus.Memory[text + (uint)position++];
                if (position < length && bus.Memory[text + (uint)position] == (byte)'"') position++;
                result = 2;
            }
            else
            {
                while (position < length && bus.Memory[text + (uint)position] is not
                           ((byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'='))
                    bus.Memory[buffer + count++] = bus.Memory[text + (uint)position++];
                result = 1;
            }
            bus.Memory[buffer + count] = 0;
            bus.Long(source + 8, (uint)position);
            itemEnd = position;
            return result;
        }

        private uint ReadArgs(uint template, uint array, uint args)
        {
            Require(test.KeyTemplate is not null && readArgs++ == 0, "Unexpected or repeated ReadArgs.");
            Require(bus.CString(template) == test.KeyTemplate, $"ReadArgs template \"{bus.CString(template)}\" differs.");
            Require(args == rdArgs && args != 0, "ReadArgs did not get the AllocDosObject RDArgs.");
            bus.OwnedAllocationContaining(owner, array, "Work");
            var items = test.KeyTemplate!.Split(',');
            for (var i = 0u; i < items.Length; i++)
                Require(bus.Long(array + i * 4) == 0, "ReadArgs result slots were not cleared.");

            // RDA_Source must be the script's own argument line: the tail after FILE.
            var buffer = bus.Long(args);
            var length = (int)bus.Long(args + 4);
            var expected = test.Arguments[itemEnd..];
            if (expected.Length == 0) expected = "\n";
            Require(bus.Long(args + 8) == 0 && length == expected.Length &&
                Encoding.Latin1.GetString(bus.Memory, (int)buffer, length) == expected,
                "ReadArgs RDA_Source is not the script argument line.");
            if (expected.Length == test.Arguments.Length - itemEnd)
                Require(buffer == owner.Arguments + (uint)itemEnd, "RDA_Source does not point into the command tail.");

            if (test.ReadArgsFails) { owner.IoError = 116; return 0; }
            argValues = bus.Allocate(owner, 1024, "ArgValues", true);
            var cursor = argValues;
            for (var i = 0; i < items.Length; i++)
            {
                var value = i < test.Values.Length ? test.Values[i] : null;
                if (value is null) continue;
                var modifiers = items[i].Split('/').Skip(1).Select(m => m.ToUpperInvariant()).ToArray();
                uint slot;
                if (modifiers.Contains("S")) slot = uint.MaxValue;
                else if (modifiers.Contains("N"))
                {
                    slot = cursor;
                    bus.Long(cursor, unchecked((uint)int.Parse(value)));
                    cursor += 4;
                }
                else if (modifiers.Contains("M"))
                {
                    var words = value.Split(' ');
                    slot = cursor;
                    cursor += (uint)(words.Length + 1) * 4;
                    for (var w = 0; w < words.Length; w++)
                    {
                        bus.Long(slot + (uint)w * 4, cursor);
                        cursor = PutString(cursor, words[w]);
                    }
                    bus.Long(slot + (uint)words.Length * 4, 0);
                }
                else
                {
                    slot = cursor;
                    cursor = PutString(cursor, value);
                }
                bus.Long(array + (uint)i * 4, slot);
            }
            return args;
        }

        private static int FindArg(string template, string keyword)
        {
            var items = template.Split(',');
            for (var i = 0; i < items.Length; i++)
                if (items[i].Split('/')[0].Split('=')
                    .Any(alias => string.Equals(alias, keyword, StringComparison.OrdinalIgnoreCase)))
                    return i;
            return -1;
        }

        private uint Open(string name, int mode)
        {
            Require(mode is 1005 or 1006, $"Open mode {mode} differs.");
            if (mode == 1006) RequireQuietProbe(name);
            if (!Reachable(name, out var error)) { owner.IoError = error; return 0; }
            if (mode == 1005)
            {
                if (!files.ContainsKey(name)) { owner.IoError = 205; return 0; }
                return NewHandle(name, false, 0);
            }
            files[name] = [];
            return NewHandle(name, true, 0);
        }

        private bool Reachable(string name, out int error)
        {
            error = 0;
            if (name.StartsWith("T:", StringComparison.OrdinalIgnoreCase) && !test.TAssigned) error = 218;
            else if (name.StartsWith(":T/", StringComparison.OrdinalIgnoreCase) && !test.ColonT) error = 205;
            return error == 0;
        }

        private void RequireQuietProbe(string name)
        {
            if (name.StartsWith("T:", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith(":T/", StringComparison.OrdinalIgnoreCase))
                Require(bus.Long(owner.Process + (uint)DosLayout.Process.WindowPointer) == uint.MaxValue,
                    $"Work-file probe of {name} can raise a requester (pr_WindowPtr is not -1).");
        }

        private Handle ReadHandle(uint value)
        {
            Require(value != StandardInput, "Execute read the CLI's standard input.");
            Require(handles.TryGetValue(value, out var handle) && !handle!.Write, "Read from a handle that is not open for reading.");
            return handle!;
        }

        private uint NewHandle(string name, bool write, int position)
        {
            handles.Add(++nextHandle, new Handle(name, write, position));
            return nextHandle;
        }

        private string Format(string format, uint args)
        {
            var text = new StringBuilder();
            var index = 0u;
            for (var i = 0; i < format.Length; i++)
            {
                if (format[i] == '%' && i + 1 < format.Length && format[i + 1] == 's')
                {
                    Require(args != 0, "VPrintf %s without an argument vector.");
                    text.Append(bus.CString(bus.Long(args + index++ * 4)));
                    i++;
                }
                else text.Append(format[i]);
            }
            return text.ToString();
        }

        private uint PutString(uint address, string value)
        {
            Encoding.Latin1.GetBytes(value + '\0').CopyTo(bus.Memory.AsSpan((int)address));
            return address + (uint)value.Length + 1;
        }

        private void PutBstr(uint address, string value)
        {
            bus.Memory[address] = (byte)value.Length;
            Encoding.Latin1.GetBytes(value).CopyTo(bus.Memory.AsSpan((int)address + 1));
        }

        private string Bstr(uint address) =>
            Encoding.Latin1.GetString(bus.Memory, (int)address + 1, bus.Memory[address]);

        // ---- Run and verify -------------------------------------------------

        public object Execute()
        {
            bus.Memory.AsSpan((int)(owner.StackTop - owner.StackBytes - 16), (int)owner.StackBytes + 32).Fill(0xb6);
            using var cpu = M68kCoreFactory.Default.Create(model, bus);
            cpu.State.StatusRegister = 0;
            cpu.BeginSubroutine(Load, owner.StackTop, Return);
            for (var i = 0; i < 8; i++) cpu.State.D[i] = (uint)(0xde000000 + i * 16);
            for (var i = 0; i < 7; i++) cpu.State.A[i] = (uint)(0xae000000 + i * 16);
            cpu.State.D[0] = unchecked((uint)(test.EntryLength ?? test.Arguments.Length));
            cpu.State.A[0] = owner.Arguments;
            try
            {
                while (cpu.State.ProgramCounter != Return)
                {
                    var pc = cpu.State.ProgramCounter;
                    Require(pc >= Load && pc < Load + codeLength || gateways.Contains(pc), $"Unexpected PC ${pc:X8}.");
                    Require(++owner.Instructions < 2_000_000 && !cpu.State.Halted && !cpu.State.Stopped,
                        "Execution did not return.");
                    cpu.ExecuteInstruction();
                }
                Verify(cpu);
                return new
                {
                    id = test.Id, result = test.Result, ioErr = test.Error, instructions = owner.Instructions,
                    workFile = test.WorkFile, stackBytesWritten = owner.StackTop - owner.LowestStackWrite, events
                };
            }
            catch (Exception error)
            {
                throw new InvalidOperationException(
                    $"{test.Id}: PC=${cpu.State.ProgramCounter:X8}; output={output}; events={string.Join(',', events)}",
                    error);
            }
        }

        private void Verify(IM68kCore cpu)
        {
            var result = unchecked((int)cpu.State.D[0]);
            var error = test.MissingDos
                ? unchecked((int)bus.Long(owner.Process + (uint)DosLayout.Process.Result2))
                : owner.IoError;
            Require(result == test.Result && error == test.Error,
                $"RC {result} / Result2 {error}, expected {test.Result} / {test.Error}.");
            Require(cpu.State.A[7] == owner.StackTop, "Stack pointer not restored.");
            Require(bus.Memory.AsSpan((int)owner.StackTop, 16).IndexOfAnyExcept((byte)0xb6) < 0 &&
                bus.Memory.AsSpan((int)(owner.StackTop - owner.StackBytes - 16), 16).IndexOfAnyExcept((byte)0xb6) < 0,
                "Stack guard changed.");
            Require(output.ToString() == test.Output, $"Output \"{output}\", expected \"{test.Output}\".");
            Require(faults.SequenceEqual(test.Faults),
                $"PrintFault calls [{string.Join(';', faults)}], expected [{string.Join(';', test.Faults)}].");
            Require(opens == (test.MissingDos ? 0 : 1) && closes == opens, "Unbalanced dos.library lifetime.");
            Require(replies == (test.Workbench ? 1 : 0) && forbids == replies, "Unbalanced WBStartup reply.");
            Require(work == 0 && rdArgs == 0 && argValues == 0 && locks.Count == 0,
                "Work area, RDArgs, ReadArgs results or a lock leaked.");
            Require(bus.Long(owner.Process + (uint)DosLayout.Process.WindowPointer) == InitialWindow,
                "pr_WindowPtr was not restored.");
            Require(readArgs == (test.KeyTemplate is null ? 0 : 1), "ReadArgs call count differs.");

            var currentInput = bus.Long(cli + (uint)DosLayout.CommandLineInterface.CurrentInput);
            var interactive = bus.Long(cli + (uint)DosLayout.CommandLineInterface.Interactive);
            var expectedFiles = test.Files
                .Where(f => !test.Deleted.Contains(f.Key, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(f => f.Key, f => f.Value, StringComparer.OrdinalIgnoreCase);
            if (test.WorkFile is { } workFile)
            {
                expectedFiles[workFile] = test.WorkText;
                Require(handles.Count == 1 && handles.TryGetValue(currentInput, out var input) &&
                    string.Equals(input!.Name, workFile, StringComparison.Ordinal) && !input.Write &&
                    input.Position == 0, "cli_CurrentInput is not a fresh read handle on the work file.");
                Require(interactive == 0, "cli_Interactive was not cleared.");
                Require(Bstr(commandFile) == workFile, $"cli_CommandFile is \"{Bstr(commandFile)}\".");
                Require(bus.Long(cli + (uint)DosLayout.CommandLineInterface.CommandFile) == commandFile >> 2,
                    "cli_CommandFile buffer pointer changed.");
                handles.Remove(currentInput);
            }
            else
            {
                var callerOpen = test.CallerInput is not null ? 1 : 0;
                Require(handles.Count == callerOpen, "A file handle leaked.");
                Require(currentInput == (test.CallerInput is null ? StandardInput : callerHandle) &&
                    Bstr(commandFile) == test.CommandFile, "CLI input state changed on a failed Execute.");
            }
            Require(files.Count == expectedFiles.Count && expectedFiles.All(f =>
                    files.TryGetValue(f.Key, out var data) && Encoding.Latin1.GetString([.. data]) == f.Value),
                "Files after Execute differ: " + string.Join(" | ", files.Select(f =>
                    $"{f.Key}={Encoding.Latin1.GetString([.. f.Value]).Replace("\n", "\\n")}")));

            bus.Release(owner, commandFile, "CommandFile");
            bus.Release(owner, cli, "Cli");
            bus.AssertReleased(owner);
            bus.AssertImageUnchanged();
        }

        private void Register(uint library, short offset, string name, Func<M68kCpuState, uint> handler)
        {
            var address = checked((uint)(library + offset));
            gateways.Add(address);
            bus.RegisterGateway(address, s =>
            {
                Require(s.A[6] == library, $"{name}: wrong library base.");
                events.Add(name);
                var result = handler(s);
                s.D[0] = result; s.D[1] = 0xd1d1d1d1; s.A[0] = 0xa0a0a0a0; s.A[1] = 0xa1a1a1a1;
                s.StatusRegister = (ushort)((s.StatusRegister & 0xffe0) | 0x001f);
            });
        }
    }
}
