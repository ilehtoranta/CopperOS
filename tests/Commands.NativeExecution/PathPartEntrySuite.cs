using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>
/// Supplied post-ReadArgs vectors for the bounded PathPart native entry. The
/// suite validates native DOS-vector ABI and per-invocation ownership only; it
/// is not a MorphOS parser, filesystem handler, or reference comparison.
/// </summary>
internal sealed record PathPartEntryCase(string? Directory, string? File,
    string[] Additions, string Output)
{
    public int Result { get; init; } = DOS.RETURN_OK;
    public int IoError { get; init; }
}

internal sealed partial class ProbeFixture
{
    public const string PathPartEntrySuite = "pathpart-native-entry-vector-fixture";

    private List<object> RunPathPartEntryCases()
    {
        ProbeCase[] cases =
        [
            PathPart("directory", "VOL:one/two", null, [], "VOL:one\n"),
            PathPart("file", null, "VOL:one/two", [], "two\n"),
            PathPart("add", null, null, ["VOL:", "one", "two"], "VOL:one/two\n"),
            PathPart("combined", "VOL:one/two", "VOL:one/two",
                ["VOL:", "one", "two"], "VOL:one\ntwo\nVOL:one/two\n"),
            PathPart("no-mode", null, null, [], ""),
            new ProbeCase("readargs-failure", "", DOS.RETURN_ERROR, 118, "")
            {
                PathPart = new(null, null, [], ""), ParserError = 118
            },
            new ProbeCase("result-allocation-failure", "", DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore, "")
            {
                PathPart = new(null, null, [], ""), AllocationFailure = true
            },
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "repeat-directory" },
            cases[2] with { Name = "repeat-add" }
        ], true));
        reports.AddRange(Execute([
            cases[5] with { Name = "interleaved-readargs-failure", StackBytes = 4096 },
            cases[1] with { Name = "interleaved-file" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase PathPart(string name, string? directory, string? file,
        string[] additions, string output) => new(name, "", DOS.RETURN_OK, 0, output)
    {
        PathPart = new PathPartEntryCase(directory, file, additions, output)
    };

    private void VerifyPathPartEntry(Invocation invocation)
    {
        var definition = invocation.Definition.PathPart ??
            throw new InvalidOperationException("Missing PathPart definition.");
        var parsed = invocation.Definition.ParserError == 0 &&
            !invocation.Definition.AllocationFailure;
        Require(invocation.Reads == (invocation.Definition.AllocationFailure ? 0 : 1),
            "PathPart ReadArgs call count differs from the supplied path.");
        Require(invocation.FreeArgs == (parsed ? 1 : 0),
            "PathPart RDArgs lifetime differs from the supplied path.");
        if (!parsed)
        {
            Require(invocation.Allocations == 1,
                "PathPart parser setup allocation count differs from the supplied path.");
            Require(invocation.FreeMem == (invocation.Definition.ParserError == 0 ? 0 : 1),
                "PathPart parser setup cleanup differs from the supplied path.");
            return;
        }

        Require(invocation.Allocations == 2 && invocation.FreeMem == 2,
            "PathPart must release result and scratch allocations exactly once.");
        Require(CountEvent(invocation, "PathPart") == (definition.Directory is null ? 0 : 1) &&
            CountEvent(invocation, "FilePart") == (definition.File is null ? 0 : 1) &&
            CountEvent(invocation, "AddPart") == definition.Additions.Length,
            "PathPart DOS helper call count differs from the supplied mode vector.");
        Require(invocation.Events.IndexOf("ReadArgs") < invocation.Events.IndexOf("FreeArgs") &&
            invocation.Events.IndexOf("FreeArgs") < invocation.Events.IndexOf("CloseLibrary"),
            "PathPart released arguments after parser use or after closing DOS.");
    }

    private static int CountEvent(Invocation invocation, string name) =>
        invocation.Events.Count(value => value == name);

    private void RegisterPathPartEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.PathPart ??
                throw new InvalidOperationException("Missing PathPart definition.");
            Require(Bus.CString(state.D[1]) == "DIR/K,FILE/K,ADD/K/M" && state.D[3] == 0,
                "PathPart ReadArgs template/source ABI mismatch.");
            var results = state.D[2];
            Require((results & 3) == 0 &&
                Bus.OwnedAllocation(invocation, results, "Exec").Size == 12,
                "PathPart result slots must be a three-LONG owned allocation.");
            for (var offset = 0u; offset < 12; offset += 4)
                Require(Bus.Long(results + offset) == 0,
                    "PathPart result slots were not zeroed before ReadArgs.");
            invocation.Reads++;
            if (invocation.Definition.ParserError != 0)
            {
                invocation.IoError = invocation.Definition.ParserError;
                return 0;
            }

            var rdArgs = Bus.Allocate(invocation, 512, "RDArgs", true);
            var text = rdArgs + 128;
            uint Put(string value)
            {
                var bytes = Encoding.Latin1.GetBytes(value);
                Require(text + (uint)bytes.Length + 1 <= rdArgs + 512,
                    "PathPart fixture RDArgs string storage overflow.");
                bytes.CopyTo(Bus.Memory.AsSpan((int)text));
                Bus.Memory[text + (uint)bytes.Length] = 0;
                var address = text;
                text += (uint)bytes.Length + 1;
                return address;
            }

            if (definition.Directory is not null) Bus.Long(results, Put(definition.Directory));
            if (definition.File is not null) Bus.Long(results + 4, Put(definition.File));
            if (definition.Additions.Length != 0)
            {
                var vector = rdArgs + 64;
                Require(definition.Additions.Length < 12,
                    "PathPart fixture vector bound exceeded.");
                for (var index = 0; index < definition.Additions.Length; index++)
                    Bus.Long(vector + (uint)(index * 4), Put(definition.Additions[index]));
                Bus.Long(vector + (uint)(definition.Additions.Length * 4), 0);
                Bus.Long(results + 8, vector);
            }
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FreeArgs++;
            invocation.IoError = 901;
            return 0;
        });
        Register(baseAddress, DosLvo.PathPart, "PathPart", (state, invocation) =>
        {
            var source = state.D[1];
            var text = Bus.CString(source);
            var slash = text.LastIndexOf('/');
            if (slash >= 0) return source + (uint)slash;
            var colon = text.LastIndexOf(':');
            return colon >= 0 ? source + (uint)(colon + 1) : source;
        });
        Register(baseAddress, DosLvo.FilePart, "FilePart", (state, invocation) =>
        {
            var source = state.D[1];
            var text = Bus.CString(source);
            var separator = Math.Max(text.LastIndexOf('/'), text.LastIndexOf(':'));
            return source + (uint)(separator + 1);
        });
        Register(baseAddress, DosLvo.AddPart, "AddPart", (state, invocation) =>
        {
            var destination = state.D[1];
            var part = Bus.CString(state.D[2]);
            var current = Bus.CString(destination);
            var combined = part.Contains(':') ? part : current.Length == 0 ? part :
                current.EndsWith(':') || current.EndsWith('/') ? current + part :
                current + "/" + part;
            Require(state.D[3] == 1024, "PathPart AddPart capacity ABI differs from its owned buffer.");
            if (combined.Length + 1 > state.D[3])
            {
                invocation.IoError = (int)DOS.Error.LineTooLong;
                return 0;
            }
            Encoding.Latin1.GetBytes(combined).CopyTo(Bus.Memory.AsSpan((int)destination));
            Bus.Memory[destination + (uint)combined.Length] = 0;
            return 1;
        });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) => invocation.OutputBptr);
        Register(baseAddress, DosLvo.Write, "Write", (state, invocation) =>
        {
            Require(state.D[1] == invocation.OutputBptr,
                "PathPart changed its borrowed output handle.");
            invocation.Output.Write(Bus.Memory, checked((int)state.D[2]), checked((int)state.D[3]));
            return state.D[3];
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            var previous = invocation.IoError;
            invocation.IoError = unchecked((int)state.D[1]);
            Bus.Long(invocation.Process + (uint)DosLayout.Process.Result2, state.D[1]);
            return unchecked((uint)previous);
        });
    }
}
