using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record AddBuffersEntryCase(string Drive, int? Buffers, int Changed,
    int HandlerError = 0, int ParserError = 0);
internal sealed record AddBuffersNativeLayout(uint RdArgs);

internal sealed partial class ProbeFixture
{
    public const string AddBuffersEntrySuite = "addbuffers-native-entry-vector-fixture";

    private List<object> RunAddBuffersEntryCases()
    {
        ProbeCase[] cases =
        [
            Add("changed", "DF0:", 3, 7, "DF0: has 7 buffers\n"),
            new ProbeCase("query", "", DOS.RETURN_OK, 0, "DF0: has 12 buffers\n") { AddBuffers = new("DF0:", null, -1, 12) },
            Add("handler-failure", "BAD:", 1, 0, "", 205, DOS.RETURN_FAIL),
            new ProbeCase("parser-failure", "", DOS.RETURN_ERROR, 116, "") { AddBuffers = new("DF0:", null, 0, 0, 116) },
        ];
        var reports = new List<object>(); foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([cases[0] with { Name = "repeat-changed" }, cases[1] with { Name = "repeat-query" }], true));
        Bus.AssertImageUnchanged(); return reports;
    }

    private static ProbeCase Add(string name, string drive, int? buffers, int changed,
        string output, int handlerError = 0, int result = DOS.RETURN_OK) => new(name, "", result, handlerError, output) { AddBuffers = new(drive, buffers, changed, handlerError) };

    private void PrepareAddBuffersEntry(Invocation invocation)
    {
        var layout = new AddBuffersNativeLayout(Bus.Allocate(invocation, 128, "AddBuffersRDArgs", true));
        invocation.AddBuffersLayout = layout;
        var definition = invocation.Definition.AddBuffers!;
        Encoding.Latin1.GetBytes(definition.Drive).CopyTo(Bus.Memory.AsSpan((int)(layout.RdArgs + 32))); Bus.Memory[layout.RdArgs + 32 + (uint)definition.Drive.Length] = 0;
        if (definition.Buffers is int value) Bus.Long(layout.RdArgs + 96, unchecked((uint)value));
    }

    private void VerifyAddBuffersEntry(Invocation invocation)
    {
        var d = invocation.Definition.AddBuffers!; var parserFail = d.ParserError != 0;
        Require(invocation.Reads == 1 && invocation.FreeArgs == (parserFail ? 0 : 1), "AddBuffers parser ownership differs.");
        var formatPath = !parserFail && d.Changed != 0;
        Require(invocation.Allocations == (formatPath ? 2 : 1) && invocation.FreeMem == (formatPath ? 2 : 1), "AddBuffers allocation cleanup differs.");
        Require(invocation.Events.Count(x => x == "AddBuffers") == (parserFail ? 0 : 1), "AddBuffers handler vector count differs.");
        Require(invocation.Events.IndexOf("ReadArgs") < invocation.Events.IndexOf("CloseLibrary"), "AddBuffers parser order differs.");
        invocation.AddBuffersLayout = null;
    }

    private void RegisterAddBuffersEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var d = invocation.Definition.AddBuffers!; Require(Bus.CString(state.D[1]) == "DRIVE/A,BUFFERS/N" && state.D[3] == 0, "AddBuffers ReadArgs ABI differs.");
            Require(Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 8, "AddBuffers result storage differs."); invocation.Reads++;
            if (d.ParserError != 0) { Bus.Release(invocation, invocation.AddBuffersLayout!.RdArgs, "AddBuffersRDArgs"); invocation.AddBuffersLayout = null; invocation.IoError = d.ParserError; return 0; }
            var l = invocation.AddBuffersLayout!; Bus.Long(state.D[2], l.RdArgs + 32); if (d.Buffers is not null) Bus.Long(state.D[2] + 4, l.RdArgs + 96); return l.RdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) => { Bus.Release(invocation, state.D[1], "AddBuffersRDArgs"); invocation.FreeArgs++; return 0; });
        Register(baseAddress, DosLvo.AddBuffers, "AddBuffers", (state, invocation) => { var d = invocation.Definition.AddBuffers!; Require(Bus.CString(state.D[1]) == d.Drive && unchecked((int)state.D[2]) == (d.Buffers ?? 0), "AddBuffers vector ABI differs."); invocation.IoError = d.HandlerError; return unchecked((uint)d.Changed); });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) => { Require(Bus.CString(state.D[1]) == "%s has %ld buffers\n", "AddBuffers display format differs."); var args = state.D[2]; invocation.Output.Write(Encoding.Latin1.GetBytes($"{Bus.CString(Bus.Long(args))} has {unchecked((int)Bus.Long(args + 4))} buffers\n")); return 0; });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) => unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (state, invocation) => { Require(Bus.CString(state.D[2]) == "AddBuffers", "AddBuffers fault name differs."); return 0; });
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) => { invocation.IoError = unchecked((int)state.D[1]); return 0; });
    }
}
