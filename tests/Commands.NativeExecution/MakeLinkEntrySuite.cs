using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record MakeLinkEntryCase(string From, string To, bool Hard = false,
    bool Force = false, bool LockSucceeds = true, bool AllocateFib = true,
    bool ExamineSucceeds = true, bool Directory = false, bool LinkSucceeds = true,
    int Error = Invocation.InitialIoError, int ParserError = 0,
    int AncestorDepth = 1, bool Loop = false, bool ParentSucceeds = true);
internal sealed record MakeLinkNativeLayout(uint RdArgs);

internal sealed partial class ProbeFixture
{
    public const string MakeLinkEntrySuite = "makelink-native-entry-vector-fixture";

    private List<object> RunMakeLinkEntryCases()
    {
        ProbeCase[] cases =
        [
            Case("soft-success", "alias", "C:List", false, false, ""),
            Case("soft-failure", "alias", "C:List", false, false, "", linkSucceeds: false, error: 205),
            Case("hard-file-success", "alias", "Work:File", true, false, ""),
            Case("hard-cross-volume-failure", "alias", "Work:File", true, false, "", linkSucceeds: false, error: 215),
            Case("forced-directory-link-failure", "alias", "Work:Dir", true, true, "", directory: true, linkSucceeds: false, error: 203),
            Case("hard-directory-needs-force", "alias", "Work:Dir", true, false, "Hard-links to directories require the FORCE keyword\n", directory: true),
            Case("hard-directory-forced", "alias", "Work:Dir", true, true, "", directory: true),
            Case("hard-lock-failure", "alias", "Missing", true, false, "Missing", lockSucceeds: false, error: 205),
            Case("hard-fib-failure", "alias", "Work:File", true, false, "", allocateFib: false, error: 103),
            Case("hard-examine-failure", "alias", "Work:File", true, false, "", examineSucceeds: false, error: 222),
            Case("parser-failure", "alias", "Work:File", false, false, "", parserError: 116),
        ];
        cases = [..cases,
            cases[0] with { Name="missing-dos", MissingDos=true, Result=20, Error=Invocation.InitialIoError },
            cases[0] with { Name="workbench", Workbench=true, Result=10, Error=(int)DOS.Error.ObjectWrongType },
            cases[0] with { Name="workbench-missing-dos", Workbench=true, MissingDos=true, Result=20, Error=Invocation.InitialIoError },
            cases[0] with { Name="negative-entry-length", EntryLength=-1, Result=10, Error=(int)DOS.Error.LineTooLong },
            cases[0] with { Name="null-entry-buffer", EntryLength=5, NullArgumentPointer=true, Result=10, Error=(int)DOS.Error.LineTooLong }];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { MakeLink = cases[0].MakeLink! with { From = "left" } },
            cases[2] with { MakeLink = cases[2].MakeLink! with { From = "right" } }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase Case(string label, string from, string to, bool hard, bool force,
        string output, bool lockSucceeds = true, bool allocateFib = true,
        bool examineSucceeds = true, bool directory = false, bool linkSucceeds = true,
        int error = Invocation.InitialIoError, int parserError = 0)
    {
        var definition = new ProbeCase(label, "", DOS.RETURN_FAIL,
            parserError != 0 ? parserError : error, output)
        { MakeLink = new(from, to, hard, force, lockSucceeds, allocateFib, examineSucceeds, directory, linkSucceeds, error, parserError) };
        return definition with
        { Result = parserError == 0 && ((!hard && linkSucceeds) || (hard && lockSucceeds && allocateFib && examineSucceeds && (!directory || force) && linkSucceeds)) ? DOS.RETURN_OK : DOS.RETURN_FAIL };
    }

    private void PrepareMakeLinkEntry(Invocation invocation)
    {
        if (MakeLinkStartupOnly(invocation.Definition)) return;
        var value = invocation.Definition.MakeLink!;
        var layout = new MakeLinkNativeLayout(Bus.Allocate(invocation, 160, "MakeLinkRDArgs", true));
        invocation.MakeLinkLayout = layout;
        PutMakeLink(layout.RdArgs + 32, value.From);
        PutMakeLink(layout.RdArgs + 80, value.To);
    }

    private static bool MakeLinkStartupOnly(ProbeCase test) => test.MissingDos || test.Workbench || test.EntryLength < 0 || test.NullArgumentPointer;

    private void VerifyMakeLinkEntry(Invocation invocation)
    {
        if (MakeLinkStartupOnly(invocation.Definition))
        {
            Require(invocation.Reads == 0 && invocation.FreeArgs == 0 && invocation.Allocations == 0 && invocation.MakeLinkLayout is null,
                "Startup rejection entered MakeLink body or retained storage.");
            Require(!invocation.Events.Any(x => x is "Lock" or "MakeLink" or "AllocDosObject" or "PrintFault"), "Startup rejection performed body operations.");
            return;
        }
        var value = invocation.Definition.MakeLink!;
        var parser = value.ParserError != 0;
        Require(invocation.Reads == 1 && invocation.FreeArgs == (parser ? 0 : 1), "MakeLink parser lifetime differs.");
        Require(invocation.Events.Count(item => item == "Lock") == (!parser && value.Hard ? 1 : 0), "MakeLink lock count differs.");
        Require(invocation.Events.Count(item => item == "UnLock") == (!parser && value.Hard && value.LockSucceeds ? 1 : 0), "MakeLink unlock count differs.");
        Require(invocation.Events.Count(item => item == "AllocDosObject") == (!parser && value.Hard && value.LockSucceeds ? 1 : 0), "MakeLink FIB allocation differs.");
        Require(invocation.Events.Count(item => item == "FreeDosObject") == (!parser && value.Hard && value.LockSucceeds && value.AllocateFib ? 1 : 0), "MakeLink FIB cleanup differs.");
        var calls = !parser && (!value.Hard || value.LockSucceeds && value.AllocateFib && value.ExamineSucceeds && (!value.Directory || value.Force)) ? 1 : 0;
        Require(invocation.Events.Count(item => item == "MakeLink") == calls, $"MakeLink vector count differs for {invocation.Definition.Name}: expected {calls}, got {invocation.Events.Count(item => item == "MakeLink")}.");
        var fault = parser || (!value.Hard ? !value.LinkSucceeds : !value.LockSucceeds || !value.AllocateFib || value.ExamineSucceeds && (!value.Directory || value.Force) && !value.LinkSucceeds);
        Require(invocation.Events.Count(x => x == "PrintFault") == (fault ? 1 : 0), "MakeLink diagnostic count differs.");
        if (!parser && value.Hard && value.LockSucceeds && value.AllocateFib)
            Require(invocation.Events.IndexOf("FreeDosObject") < invocation.Events.IndexOf("UnLock") && invocation.Events.IndexOf("UnLock") < invocation.Events.IndexOf("FreeArgs"), "MakeLink cleanup order differs.");
        if (!parser && value.Hard && calls == 1 && !value.LinkSucceeds)
            Require(invocation.Events.IndexOf("MakeLink") < invocation.Events.IndexOf("PrintFault") && invocation.Events.IndexOf("PrintFault") < invocation.Events.IndexOf("FreeDosObject"), "Hard-link failure diagnostic preceded operation or followed cleanup.");
        invocation.MakeLinkLayout = null;
    }

    private void RegisterMakeLinkEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var value = invocation.Definition.MakeLink!;
            Require(Bus.CString(state.D[1]) == "FROM/A,TO/A,HARD/S,FORCE/S", "MakeLink template differs.");
            Require(Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 16, "MakeLink result storage differs.");
            invocation.Reads++;
            if (value.ParserError != 0)
            {
                Bus.Release(invocation, invocation.MakeLinkLayout!.RdArgs, "MakeLinkRDArgs");
                invocation.MakeLinkLayout = null;
                invocation.IoError = value.ParserError;
                return 0;
            }
            var layout = invocation.MakeLinkLayout!;
            Bus.Long(state.D[2], layout.RdArgs + 32);
            Bus.Long(state.D[2] + 4, layout.RdArgs + 80);
            Bus.Long(state.D[2] + 8, value.Hard ? 1u : 0u);
            Bus.Long(state.D[2] + 12, value.Force ? 1u : 0u);
            return layout.RdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        { Bus.Release(invocation, state.D[1], "MakeLinkRDArgs"); invocation.FreeArgs++; return 0; });
        Register(baseAddress, DosLvo.Lock, "Lock", (state, invocation) =>
        {
            var value = invocation.Definition.MakeLink!;
            Require(Bus.CString(state.D[1]) == value.To && state.D[2] == unchecked((uint)DOS.LockMode.Shared), "MakeLink lock ABI differs.");
            invocation.IoError = value.Error; return value.LockSucceeds ? 0x130u : 0;
        });
        Register(baseAddress, -228, "AllocDosObject", (state, invocation) =>
        {
            var value = invocation.Definition.MakeLink!;
            Require(state.D[1] == (uint)DosObjectType.FileInfoBlock && state.D[2] == 0, "MakeLink FIB request differs.");
            return value.AllocateFib ? Bus.Allocate(invocation, FileInfoBlock.SizeInBytes, "MakeLinkFIB", true) : 0;
        });
        Register(baseAddress, DosLvo.Examine, "Examine", (state, invocation) =>
        {
            var value = invocation.Definition.MakeLink!;
            Require(state.D[1] == 0x130, "MakeLink Examine lock differs.");
            var fib = Bus.OwnedAllocation(invocation, state.D[2], "MakeLinkFIB");
            Bus.Long(fib.Address + FileInfoBlock.DirEntryTypeOffset, value.Directory ? 2u : unchecked((uint)-3));
            invocation.IoError = value.Error;
            return value.ExamineSucceeds ? 1u : 0;
        });
        Register(baseAddress, -234, "FreeDosObject", (state, invocation) =>
        { Require(state.D[1] == (uint)DosObjectType.FileInfoBlock, "MakeLink FIB free type differs."); Bus.Release(invocation, state.D[2], "MakeLinkFIB"); return 0; });
        Register(baseAddress, DosLvo.UnLock, "UnLock", (state, invocation) =>
        { Require(state.D[1] == 0x130, "MakeLink unlock ABI differs."); return 0; });
        Register(baseAddress, DosLvo.MakeLink, "MakeLink", (state, invocation) =>
        {
            var value = invocation.Definition.MakeLink!;
            Require(Bus.CString(state.D[1]) == value.From, "MakeLink source differs.");
            Require(state.D[2] == (value.Hard ? 0x130u : invocation.MakeLinkLayout!.RdArgs + 80) && state.D[3] == (value.Hard ? 0u : 1u), "MakeLink vector ABI differs.");
            invocation.IoError = value.Error; return value.LinkSucceeds ? 1u : 0;
        });
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state, invocation) =>
        { invocation.Output.Write(Encoding.Latin1.GetBytes(Bus.CString(state.D[1]))); return 0; });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (state, invocation) => {
            var value = invocation.Definition.MakeLink!;
            Require(state.D[1] == unchecked((uint)(value.ParserError != 0 ? value.ParserError : value.Error)) &&
                Bus.CString(state.D[2]) == (value.ParserError == 0 && value.Hard && !value.LockSucceeds ? "" : "MakeLink"), "MakeLink fault error/header differs.");
            return 0;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) => unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) => { invocation.IoError = unchecked((int)state.D[1]); return 0; });
    }

    private void PutMakeLink(uint address, string value)
    {
        Encoding.Latin1.GetBytes(value).CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)value.Length] = 0;
    }
}
