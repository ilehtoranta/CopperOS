using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed partial class ProbeFixture
{
    public const string WorkbenchMakeLinkEntrySuite = "workbench-makelink-native-entry-vector-fixture";
    private readonly Dictionary<Invocation, HashSet<uint>> workbenchLinkLocks = new();

    private static uint WorkbenchTarget(Invocation i) => i.MakeLinkLayout!.RdArgs + 0x130;
    private static uint WorkbenchAncestor(Invocation i) => i.MakeLinkLayout!.RdArgs + 0x200;

    private List<object> RunWorkbenchMakeLinkCases()
    {
        var file = new MakeLinkEntryCase("Work:Parent/alias", "Work:File", Error: 0);
        var directory = file with { To = "Work:Dir", Directory = true, AncestorDepth = 3 };
        ProbeCase Make(string name, MakeLinkEntryCase value, int result, string output = "") =>
            new(name, "", result, value.ParserError != 0 ? value.ParserError : value.Error, output) { MakeLink = value };
        ProbeCase[] cases = [
            Make("default-hard-file", file, 0),
            Make("explicit-hard-file", file with { Hard = true }, 0),
            Make("three-ancestor-directory", directory with { Force = true }, 0),
            Make("directory-needs-force", directory, 20, "Links to directories require use of the FORCE keyword\n"),
            Make("three-ancestor-loop", directory with { Force = true, Loop = true }, 20, "Link loop from Work:Parent/alias to Work:Dir not allowed\n"),
            Make("missing-parent", directory with { Force = true, ParentSucceeds = false, Error = 205 }, 20),
            Make("missing-target", file with { LockSucceeds = false, Error = 205 }, 20, "Can't find Work:File "),
            Make("fib-failure", file with { AllocateFib = false, Error = 103 }, 20),
            Make("examine-failure", file with { ExamineSucceeds = false, Error = 222 }, 20),
            Make("link-failure", file with { LinkSucceeds = false, Error = 203 }, 20),
            Make("parser-failure", file with { ParserError = 116 }, 20)
        ];
        cases = [..cases,
            cases[0] with { Name="missing-dos", MissingDos=true, WritesOwnProcessError=true, Result=20, Error=122 },
            cases[0] with { Name="workbench", Workbench=true, Result=10, Error=(int)DOS.Error.ObjectWrongType },
            cases[0] with { Name="workbench-missing-dos", Workbench=true, MissingDos=true, WritesOwnProcessError=true, Result=20, Error=122 },
            cases[0] with { Name="negative-entry-length", EntryLength=-1, Result=10, Error=(int)DOS.Error.LineTooLong },
            cases[0] with { Name="null-entry-buffer", EntryLength=5, NullArgumentPointer=true, Result=10, Error=(int)DOS.Error.LineTooLong }];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([cases[2], cases[4]], true));
        reports.AddRange(Execute([cases[11], cases[0]], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void VerifyWorkbenchMakeLink(Invocation invocation)
    {
        if (MakeLinkStartupOnly(invocation.Definition))
        {
            Require(invocation.Reads == 0 && invocation.FreeArgs == 0 && invocation.Allocations == 0 &&
                    invocation.MakeLinkLayout is null && !workbenchLinkLocks.ContainsKey(invocation), "Startup entered body.");
            Require(!invocation.Events.Any(x => x is "Lock" or "MakeLink" or "AllocDosObject" or "PrintFault"), "Startup performed body operations.");
            return;
        }
        var value = invocation.Definition.MakeLink!;
        Require(invocation.Reads == 1 && invocation.FreeArgs == (value.ParserError == 0 ? 1 : 0), "Workbench parser lifetime.");
        Require(workbenchLinkLocks.Remove(invocation, out var locks) && locks.Count == 0, "Workbench lock leak.");
        var directoryCheck = value.ParserError == 0 && value.LockSucceeds && value.AllocateFib && value.ExamineSucceeds && value.Directory;
        Require(invocation.Events.Count(x => x == "SameLock") == (directoryCheck && value.ParentSucceeds ? value.AncestorDepth : 0), "Ancestor comparison count.");
        Require(invocation.Events.Count(x => x == "ParentDir") == (directoryCheck && value.ParentSucceeds ? value.AncestorDepth - (value.Loop ? 1 : 0) : 0), "Ancestor traversal count.");
        Require(invocation.Events.Count(x => x == "MakeLink") == (invocation.Definition.Result == 0 || !value.LinkSucceeds ? 1 : 0), "Workbench link count.");
        Require(invocation.Events.Count(x => x == "PrintFault") == (invocation.Definition.Result == 0 ? 0 : 1), "Workbench fault count.");
        invocation.MakeLinkLayout = null;
    }

    private void RegisterWorkbenchMakeLinkDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (s, i) =>
        {
            var v = i.Definition.MakeLink!;
            Require(Bus.CString(s.D[1]) == "FROM/A,TO/A,HARD/S,FORCE/S" && Bus.OwnedAllocation(i, s.D[2], "Exec").Size == 16, "Workbench ReadArgs ABI.");
            i.Reads++; workbenchLinkLocks.Add(i, new());
            var layout = i.MakeLinkLayout!;
            if (v.ParserError != 0) { Bus.Release(i, layout.RdArgs, "MakeLinkRDArgs"); i.MakeLinkLayout = null; i.IoError = v.ParserError; return 0; }
            Bus.Long(s.D[2], layout.RdArgs + 32); Bus.Long(s.D[2]+4, layout.RdArgs+80);
            Bus.Long(s.D[2]+8, v.Hard ? 1u : 0u); Bus.Long(s.D[2]+12, v.Force ? 1u : 0u);
            return layout.RdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (s,i) =>
        {
            Require(workbenchLinkLocks[i].Count == 0 && Bus.CString(i.MakeLinkLayout!.RdArgs+32) == i.Definition.MakeLink!.From, "Parser freed with live locks or truncated FROM.");
            Bus.Release(i,s.D[1],"MakeLinkRDArgs");i.FreeArgs++;return 0;
        });
        Register(baseAddress, DosLvo.Lock, "Lock", (s,i) =>
        {
            var v=i.Definition.MakeLink!;var name=Bus.CString(s.D[1]);
            Require(s.D[2]==0xfffffffe, "Lock mode."); i.IoError=v.Error;
            if(name==v.To) { if(!v.LockSucceeds)return 0; Require(workbenchLinkLocks[i].Add(WorkbenchTarget(i)),"Repeated target lock.");return WorkbenchTarget(i); }
            Require(name=="Work:Parent" && v.Directory,"Destination parent path.");
            if(!v.ParentSucceeds)return 0;
            Require(workbenchLinkLocks[i].Add(WorkbenchAncestor(i)),"Repeated parent lock.");return WorkbenchAncestor(i);
        });
        Register(baseAddress, -228, "AllocDosObject", (s,i) =>
        { Require(s.D[1]==2 && s.D[2]==0,"FIB allocation ABI."); i.IoError=i.Definition.MakeLink!.Error;return i.Definition.MakeLink.AllocateFib ? Bus.Allocate(i,FileInfoBlock.SizeInBytes,"MakeLinkFIB",true):0; });
        Register(baseAddress, DosLvo.Examine, "Examine", (s,i) =>
        { var v=i.Definition.MakeLink!;Require(s.D[1]==WorkbenchTarget(i) && workbenchLinkLocks[i].Contains(s.D[1]),"Examine target.");var fib=Bus.OwnedAllocation(i,s.D[2],"MakeLinkFIB");Bus.Long(fib.Address+FileInfoBlock.DirEntryTypeOffset,v.Directory?2u:0xfffffffdu);i.IoError=v.Error;return v.ExamineSucceeds?1u:0; });
        Register(baseAddress, -234, "FreeDosObject", (s,i) =>
        { Require(s.D[1]==2,"FIB type.");Bus.Release(i,s.D[2],"MakeLinkFIB");return 0; });
        Register(baseAddress, -876, "PathPart", (s,i) =>
        { Require(Bus.CString(s.D[1])==i.Definition.MakeLink!.From,"Original FROM path.");return s.D[1]+11; });
        Register(baseAddress, DosLvo.SameLock, "SameLock", (s,i) =>
        { var v=i.Definition.MakeLink!;Require(s.D[1]==WorkbenchTarget(i) && workbenchLinkLocks[i].Contains(s.D[2]),"SameLock ownership.");return v.Loop && s.D[2]==WorkbenchAncestor(i)+(uint)(v.AncestorDepth-1)*4 ? 0u : 0xffffffff; });
        Register(baseAddress, DosLvo.ParentDir, "ParentDir", (s,i) =>
        { Require(workbenchLinkLocks[i].Contains(s.D[1]),"ParentDir ownership.");var next=s.D[1]+4;if(next>=WorkbenchAncestor(i)+(uint)i.Definition.MakeLink!.AncestorDepth*4)return 0;Require(workbenchLinkLocks[i].Add(next),"Repeated ancestor.");return next; });
        Register(baseAddress, DosLvo.UnLock, "UnLock", (s,i) =>
        { Require(workbenchLinkLocks[i].Remove(s.D[1]),"Wrong or double UnLock.");return 0; });
        Register(baseAddress, DosLvo.MakeLink, "MakeLink", (s,i) =>
        { var v=i.Definition.MakeLink!;Require(Bus.CString(s.D[1])==v.From && s.D[2]==WorkbenchTarget(i) && s.D[3]==0 && workbenchLinkLocks[i].SetEquals([WorkbenchTarget(i)]),"Workbench hard-link ABI/restored path.");i.IoError=v.Error;return v.LinkSucceeds?1u:0; });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (s,i) =>
        {
            var format=Bus.CString(s.D[1]); var text=format;
            if(format=="Can't find %s ") text="Can't find "+Bus.CString(Bus.Long(s.D[2]))+" ";
            else if(format=="Link loop from %s to %s not allowed\n") text="Link loop from "+Bus.CString(Bus.Long(s.D[2]))+" to "+Bus.CString(Bus.Long(s.D[2]+4))+" not allowed\n";
            else Require(format=="Links to directories require use of the FORCE keyword\n","Workbench format.");
            i.Output.Write(Encoding.Latin1.GetBytes(text));return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (s,i) =>
        { Require(s.D[1]==unchecked((uint)i.Definition.Error) && s.D[2]==0,"Workbench fault/header.");return 0; });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_,i)=>unchecked((uint)i.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (s,i)=> { i.IoError=unchecked((int)s.D[1]);return 0; });
    }
}
