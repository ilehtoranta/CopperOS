using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

// Behavioral oracle from the pinned 50.8 observations, not an execution of the
// original MorphOS binary. Provider gateways deliberately expose profile differences.
internal sealed record MorphOSRenameCase
{
    public string Failure { get; init; } = "";
    public int Fault { get; init; }
    public bool Directory { get; init; }
    public bool Multiple { get; init; }
    public bool Wild { get; init; }
    public bool Quiet { get; init; }
    public int EntryType { get; init; } = 2;
    public int SameLock { get; init; } = 1;
    public bool SourceLockMissing { get; init; }
    public string NameMode { get; init; } = "normal";
    public int Matches { get; init; } = 1;
    public int FailRenameAt { get; init; } = 1;
    public int FailAddPartAt { get; init; } = 1;
    public bool FoundBreak { get; init; }
    public int NextResult { get; init; } = 232;
    public string PathMode { get; init; } = "normal";
    public int FallbackLength { get; init; }
    public int? OutputIoError { get; init; }
    public int? FaultIoError { get; init; }
    public int? FreeArgsIoError { get; init; }
    public int ExpectedRenames { get; init; }
    public int ExpectedNext { get; init; }
    public string[] FaultCalls { get; init; } = [];
}

internal sealed partial class ProbeFixture
{
    public const string MorphOSRenameSuite = "morphos-rename-native-entry-vector-fixture";
    private sealed class MorphRenameLease
    {
        public uint Workspace, Diagnostic, Parser, Fib;
        public bool Search;
        public HashSet<uint> Locks = [];
        public List<string> Faults = [];
        public int PatternIndex, MatchIndex, Renames, Next;
    }
    private readonly Dictionary<Invocation, MorphRenameLease> morphRenameLeases = new();
    private MorphRenameLease MorphLease(Invocation i)
    {
        if (!morphRenameLeases.TryGetValue(i, out var lease))
            morphRenameLeases.Add(i, lease = new());
        return lease;
    }
    private static MorphOSRenameCase MorphCase(Invocation i) => i.Definition.MorphOSRename!;
    private void MorphText(uint pointer, string text) => Encoding.Latin1.GetBytes(text + "\0").CopyTo(Bus.Memory, (int)pointer);
    private static uint MorphDestLock(Invocation i) => i.Process + 0x340;
    private static uint MorphSourceLock(Invocation i) => i.Process + 0x344;
    private static bool MorphStack(Invocation i, uint p, uint size) => p >= i.StackTop - i.StackBytes && (ulong)p + size <= i.StackTop;
    private static string MorphPath(MorphRenameLease l) => l.MatchIndex == 0 ? "SRC:file" : "SRC:next";
    private static string MorphPrefix(MorphOSRenameCase c) => c.Failure=="name"||c.NameMode=="empty"
        ? c.FallbackLength>0 ? new string('x',c.FallbackLength) : "new"
        : c.NameMode is "capacity" or "overflow" ? new string('x',c.NameMode=="capacity"?2042:2043)+":" : "OUT:";

    private List<object> RunMorphOSRenameCases()
    {
        var cases = new List<ProbeCase>();
        void Add(string name, int result, int error, MorphOSRenameCase spec, string output = "") =>
            cases.Add(new(name, "old TO new", result, error, output) { MorphOSRename=spec, StackBytes=4096 });
        var direct = new MorphOSRenameCase { ExpectedRenames=1 };
        var directory = new MorphOSRenameCase { Directory=true, ExpectedRenames=1, ExpectedNext=1 };
        var multi = directory with { Multiple=true, ExpectedRenames=2, ExpectedNext=2 };
        Add("direct-success", 0, 0, direct);
        Add("parser-error", 10, 116, new() { Failure="parser", Fault=116, FaultCalls=["Rename:116"] });
        Add("parser-zero", 10, 0, new() { Failure="parser", FaultCalls=["Rename:0"] });
        Add("workspace-error", 20, 103, new() { Failure="workspace", Fault=103, FaultCalls=[":103"] });
        Add("workspace-zero", 20, 0, new() { Failure="workspace" });
        Add("preflight-missing", 20, 205, new() { Failure="preflight", Fault=205, FaultCalls=[":205"] }, "Can't rename old as new because ");
        Add("preflight-other", 20, 214, new() { Failure="preflight", Fault=214, FaultCalls=[":214"] });
        Add("preflight-zero", 20, 0, new() { Failure="preflight" });
        Add("nondirectory-multiple", 20, 205, new() { Multiple=true }, "Destination \"new\" is not a directory.\n");
        Add("nondirectory-wild-quiet", 20, 205, new() { Wild=true, Quiet=true }, "Destination \"new\" is not a directory.\n");
        Add("fib-error", 20, 103, new() { Directory=true, Failure="fib", Fault=103, FaultCalls=["Rename:103"] });
        Add("fib-zero", 20, 0, new() { Directory=true, Failure="fib", FaultCalls=["Rename:0"] });
        Add("examine-error", 20, 222, new() { Directory=true, Failure="examine", Fault=222, FaultCalls=["Rename:222"] });
        Add("pattern-error", 20, 120, new() { Failure="pattern", Fault=120, FaultCalls=[":120"] }, "Can't rename old as new because ");
        Add("pattern-zero", 20, 0, new() { Failure="pattern" }, "Can't rename old as new because ");
        Add("direct-failure", 20, 203, direct with { Failure="rename", Fault=203, FaultCalls=[":203"] }, "Can't rename old as new because ");
        Add("direct-false-zero", 20, 0, direct with { Failure="rename" }, "Can't rename old as new because ");
        Add("single-forced-quiet", 0, 232, directory);
        Add("single-source-lock-missing", 0, 232, directory with { SourceLockMissing=true });
        Add("single-same-lock-direct", 0, 0, direct with { Directory=true, SameLock=0 });
        Add("entry-type-zero-is-directory", 0, 232, directory with { EntryType=0 });
        Add("entry-type-negative-is-file", 0, 0, direct with { Directory=true, EntryType=-3 });
        Add("multiple-progress", 0, 232, multi, "Renaming SRC:file as OUT:file\nRenaming SRC:file as OUT:file\n");
        Add("multiple-quiet", 0, 232, multi with { Quiet=true });
        Add("wild-two-matches", 0, 232, directory with { Matches=2, Wild=true, ExpectedRenames=2, ExpectedNext=2 });
        Add("name-failure-120", 20, 120, new() { Directory=true, Failure="name", Fault=120, FaultCalls=["Rename:120"] });
        Add("name-other-fallback", 0, 232, directory with { Failure="name", Fault=212 });
        Add("name-zero-fallback", 0, 232, directory with { Failure="name" });
        Add("name-empty-fallback", 0, 232, directory with { NameMode="empty" });
        Add("diagnostic-allocation-error", 20, 103, new() { Directory=true, Failure="diagnostic", Fault=103, FaultCalls=[":103"] });
        Add("diagnostic-allocation-zero", 20, 0, new() { Directory=true, Failure="diagnostic" });
        Add("addpart-failure", 20, 120, new() { Directory=true, Failure="addpart", FaultCalls=["Rename:120"] });
        Add("addpart-second-match-failure", 20, 120, directory with { Matches=2, Failure="addpart", FailAddPartAt=2, FaultCalls=["Rename:120"] });
        Add("directory-second-failure", 20, 203, directory with { Matches=2, Failure="rename", Fault=203, FailRenameAt=2, ExpectedRenames=2, ExpectedNext=1, FaultCalls=[":203"] }, "Can't rename SRC:next as OUT:next because ");
        Add("directory-false-zero", 20, 0, directory with { Failure="rename", ExpectedNext=0 }, "Can't rename SRC:file as OUT:file because ");
        Add("found-break", 5, 304, directory with { FoundBreak=true, NextResult=304, FaultCalls=[":304"] });
        Add("matcher-error-without-break", 0, 103, directory with { NextResult=103 });
        Add("later-pattern-failure", 0, 205, multi with { Quiet=true, Failure="later", Fault=205, ExpectedRenames=1, ExpectedNext=1 });
        Add("direct-2047-byte-path", 0, 0, direct with { PathMode="capacity" });
        Add("direct-unterminated-path", 20, 120, new() { PathMode="unterminated", FaultCalls=[":120"] });
        Add("directory-2047-byte-destination", 0, 232, directory with { NameMode="capacity" });
        Add("directory-composition-overflow", 20, 120, new() { Directory=true, NameMode="overflow", FaultCalls=["Rename:120"] });
        Add("directory-unterminated-name", 20, 120, new() { Directory=true, NameMode="unterminated", FaultCalls=[":120"] });
        Add("fallback-complete-but-too-long-to-compose", 20, 120, new() { Directory=true, NameMode="empty", FallbackLength=2047, FaultCalls=["Rename:120"] });
        Add("fallback-unterminated-not-truncated", 20, 120, new() { Directory=true, NameMode="empty", FallbackLength=2048, FaultCalls=[":120"] });
        Add("matcher-unterminated-path", 20, 120, new() { Directory=true, PathMode="unterminated", FaultCalls=[":120"] });
        Add("addpart-unterminated-output", 20, 120, new() { Directory=true, Failure="addpart-output", FaultCalls=[":120"] });
        Add("selected-error-survives-output", 20, 203, direct with { Failure="rename", Fault=203, OutputIoError=999, FaultCalls=[":203"] }, "Can't rename old as new because ");
        Add("zero-selected-error-keeps-output-error", 20, 999, direct with { Failure="rename", OutputIoError=999 }, "Can't rename old as new because ");
        Add("parser-fault-ambient-error", 10, 777, new() { Failure="parser", Fault=116, FaultIoError=777, FaultCalls=["Rename:116"] });
        Add("successful-cleanup-ambient-error", 0, 777, direct with { FreeArgsIoError=777 });
        Add("found-break-flag-separate-from-IoErr", 5, 304, directory with { FoundBreak=true, FaultCalls=[":304"] });
        cases.Add(new("missing-dos", "", 20, Invocation.InitialIoError, "") { MorphOSRename=new(), MissingDos=true, StackBytes=4096 });
        cases.Add(new("workbench", "", 10, 212, "") { MorphOSRename=new(), Workbench=true, StackBytes=4096 });
        cases.Add(new("negative-entry-length", "", 10, 120, "") { MorphOSRename=new(), EntryLength=-1, StackBytes=4096 });
        cases.Add(new("null-entry-buffer", "", 10, 120, "") { MorphOSRename=new(), EntryLength=3, NullArgumentPointer=true, StackBytes=4096 });
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        // Every behavior also executes interleaved with a different invocation.
        for (var n=0; n<cases.Count; n+=2)
            reports.AddRange(Execute([cases[n], cases[(n+1)%cases.Count]], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void VerifyMorphOSRename(Invocation i)
    {
        var l=MorphLease(i); var c=MorphCase(i);
        Require(l.Renames==c.ExpectedRenames && l.Next==c.ExpectedNext, $"{i.Definition.Name}: mutation/advance counts {l.Renames}/{l.Next}.");
        Require(l.Faults.SequenceEqual(c.FaultCalls), $"{i.Definition.Name}: fault calls {string.Join(',',l.Faults)}.");
        Require(!l.Search && l.Locks.Count==0 && l.Fib==0 && l.Parser==0 && l.Workspace==0 && l.Diagnostic==0, "MorphOS Rename leaked ownership.");
        Require(!i.Events.Contains("CheckSignal"), "MorphOS acquired classic early-break policy.");
    }

    private void RegisterMorphOSRenameExec()
    {
        Register(ExecBase, ExecLvo.AllocVec, "AllocVec", (s,i) =>
        {
            var l=MorphLease(i); var c=MorphCase(i);
            Require(l.Parser!=0, "Workspace allocated before parser success.");
            var first=i.Events.Count(x=>x=="AllocVec")==1;
            Require(s.D[0]==(first?4378u:2048u) && s.D[1]==(first?0x10000u:0u), "MorphOS Rename workspace request/flags.");
            if (c.Failure==(first?"workspace":"diagnostic")) { i.IoError=c.Fault; return 0; }
            var p=Bus.Allocate(i,s.D[0],first?"MorphRenameWorkspace":"MorphRenameDiagnostic",first);
            if(first) l.Workspace=p; else { Require(l.Workspace!=0 && l.Diagnostic==0, "Diagnostic lifetime."); l.Diagnostic=p; }
            i.Allocations++; return p;
        });
        Register(ExecBase, ExecLvo.FreeVec, "FreeVec", (s,i) =>
        {
            var l=MorphLease(i);
            Require(!l.Search && l.Locks.Count==0, "Storage freed before search/lock cleanup.");
            if(s.A[1]==0) return 0;
            if(s.A[1]==l.Diagnostic) { Bus.Release(i,l.Diagnostic,"MorphRenameDiagnostic"); l.Diagnostic=0; }
            else { Require(s.A[1]==l.Workspace && l.Diagnostic==0,"Foreign workspace or diagnostic still live."); Bus.Release(i,l.Workspace,"MorphRenameWorkspace"); l.Workspace=0; }
            return 0;
        });
    }

    private void RegisterMorphOSRenameDos(uint baseAddress)
    {
        Register(baseAddress,DosLvo.IoErr,"IoErr",(_,i)=>unchecked((uint)i.IoError));
        Register(baseAddress,DosLvo.SetIoErr,"SetIoErr",(s,i)=> { var old=i.IoError; i.IoError=unchecked((int)s.D[1]); return unchecked((uint)old); });
        Register(baseAddress,DosLvo.ReadArgs,"ReadArgs",(s,i)=>
        {
            var c=MorphCase(i); var l=MorphLease(i);
            Require(Bus.CString(s.D[1])=="FROM/A/M,TO=AS/A,QUIET/S" && s.D[3]==0 && MorphStack(i,s.D[2],12), "MorphOS template/private parser cells.");
            Require(Bus.Long(s.D[2])==0 && Bus.Long(s.D[2]+4)==0 && Bus.Long(s.D[2]+8)==0 && i.Reads++==0,"ReadArgs cells not cleared/repeated parse.");
            i.IoError=c.Failure=="parser"?c.Fault:0;
            if(c.Failure=="parser") return 0;
            l.Parser=Bus.Allocate(i,c.FallbackLength>0?4096u:128u,"MorphRenameRDArgs",true);
            Bus.Long(s.D[2],l.Parser); Bus.Long(s.D[2]+4,l.Parser+64); Bus.Long(s.D[2]+8,c.Quiet?1u:0);
            Bus.Long(l.Parser,l.Parser+32); if(c.Multiple) Bus.Long(l.Parser+4,l.Parser+40);
            MorphText(l.Parser+32,"old"); MorphText(l.Parser+40,"other");
            MorphText(l.Parser+64,c.FallbackLength>0?new string('x',c.FallbackLength):"new");
            return l.Parser;
        });
        Register(baseAddress,DosLvo.FreeArgs,"FreeArgs",(s,i)=>
        {
            var l=MorphLease(i); Require(s.D[1]==l.Parser && l.Parser!=0 && l.Workspace==0 && l.Diagnostic==0,"Parser released before body cleanup/foreign lease.");
            Bus.Release(i,l.Parser,"MorphRenameRDArgs"); l.Parser=0; i.FreeArgs++;
            if(MorphCase(i).FreeArgsIoError is int error) i.IoError=error;
            return 0;
        });
        Register(baseAddress,DosLvo.PrintFault,"PrintFault",(s,i)=>
        {
            var l=MorphLease(i); l.Faults.Add((s.D[2]==0?"":Bus.CString(s.D[2]))+":"+unchecked((int)s.D[1]));
            // Public PrintFault sets IoErr to its requested code. Explicit
            // overrides are synthetic diagnostic-provider failure injection.
            i.IoError=MorphCase(i).FaultIoError ?? unchecked((int)s.D[1]);
            return 1;
        });
        Register(baseAddress,DosLvo.MatchFirst,"MatchFirst",(s,i)=>
        {
            var l=MorphLease(i); var c=MorphCase(i);
            Require(s.D[2]==l.Workspace && !l.Search && l.Workspace!=0, "Foreign/repeated matcher start.");
            Require(Bus.Long(l.Workspace+8)==0x1000 && Bus.Word(l.Workspace+18)==2048 && (Bus.Memory[l.Workspace+16]&1)!=0,"MorphOS AnchorPath configuration.");
            var preflight=l.PatternIndex++==0;
            Require(Bus.CString(s.D[1])==(l.PatternIndex<=2?"old":"other"), "Source vector iteration.");
            l.Search=true; l.MatchIndex=0;
            Bus.Memory[l.Workspace+16]=(byte)(c.Wild?3:1);
            MorphText(l.Workspace+280,"SRC:file"); MorphText(l.Workspace+28,"WRONG-FIB-NAME");
            if(c.PathMode=="unterminated") Bus.Memory.AsSpan((int)l.Workspace+280,2048).Fill((byte)'x');
            if(c.Failure=="preflight" && preflight || c.Failure=="later" && l.PatternIndex==3) { i.IoError=c.Fault; return 103; }
            i.IoError=0; return 0;
        });
        Register(baseAddress,DosLvo.MatchEnd,"MatchEnd",(s,i)=>
        {
            var l=MorphLease(i); Require(s.D[1]==l.Workspace && l.Search,"Unstarted/double/foreign MatchEnd.");
            l.Search=false;
            // Diagnostic source must have been saved before destroying matcher buffers.
            Bus.Memory.AsSpan((int)l.Workspace+280,2048).Fill(0xdd);
            return 0;
        });
        Register(baseAddress,DosLvo.Lock,"Lock",(s,i)=>
        {
            var l=MorphLease(i); var c=MorphCase(i); var name=Bus.CString(s.D[1]);
            Require(s.D[2]==0xfffffffe && !l.Search && i.Events.Contains("MatchEnd"), "Preflight must end before destination acquisition.");
            if(name==(c.FallbackLength>0?new string('x',c.FallbackLength):"new")) { if(!c.Directory) { i.IoError=205; return 0; } Require(l.Locks.Add(MorphDestLock(i)),"Repeated destination Lock."); return MorphDestLock(i); }
            Require(name=="old" && c.Directory && !c.Multiple && l.Locks.Contains(MorphDestLock(i)), "Unexpected source Lock.");
            if(c.SourceLockMissing) { i.IoError=205; return 0; }
            Require(l.Locks.Add(MorphSourceLock(i)),"Repeated source Lock."); return MorphSourceLock(i);
        });
        Register(baseAddress,DosLvo.UnLock,"UnLock",(s,i)=>
        {
            var l=MorphLease(i); if(s.D[1]!=0) Require(l.Locks.Remove(s.D[1]),"Foreign/double UnLock."); return 0;
        });
        Register(baseAddress,DosLvo.SameLock,"SameLock",(s,i)=>
        {
            var l=MorphLease(i); Require(s.D[1]==MorphSourceLock(i) && s.D[2]==MorphDestLock(i) && l.Locks.Count==2,"SameLock ownership.");
            return unchecked((uint)MorphCase(i).SameLock);
        });
        Register(baseAddress,-228,"AllocDosObject",(s,i)=>
        {
            var l=MorphLease(i); var c=MorphCase(i); Require(s.D[1]==2 && s.D[2]==0 && l.Fib==0 && l.Locks.Contains(MorphDestLock(i)),"FIB allocation ownership/type.");
            if(c.Failure=="fib") { i.IoError=c.Fault; return 0; }
            return l.Fib=Bus.Allocate(i,260,"MorphRenameFIB",true);
        });
        Register(baseAddress,DosLvo.Examine,"Examine",(s,i)=>
        {
            var l=MorphLease(i); var c=MorphCase(i); Require(s.D[1]==MorphDestLock(i) && s.D[2]==l.Fib && l.Fib%4==0,"Examine FIB/lock ABI.");
            // Distinct fields detect accidental reuse of the classic dispatch field.
            Bus.Long(l.Fib+4,unchecked((uint)-3)); Bus.Long(l.Fib+120,unchecked((uint)c.EntryType));
            if(c.Failure=="examine") { i.IoError=c.Fault; return 0; }
            return 1;
        });
        Register(baseAddress,-234,"FreeDosObject",(s,i)=>
        {
            var l=MorphLease(i); Require(s.D[1]==2 && s.D[2]==l.Fib && l.Fib!=0,"FIB release ownership.");
            Bus.Release(i,l.Fib,"MorphRenameFIB"); l.Fib=0; return 0;
        });
        Register(baseAddress,DosLvo.ParsePattern,"ParsePattern",(s,i)=>
        {
            var l=MorphLease(i); var c=MorphCase(i); Require(!l.Search && Bus.CString(s.D[1])=="old" && s.D[2]==l.Workspace+280 && s.D[3]==2048,"Direct pattern buffer/capacity.");
            if(c.Failure=="pattern") { i.IoError=c.Fault; return uint.MaxValue; }
            if(c.PathMode=="unterminated") Bus.Memory.AsSpan((int)s.D[2],2048).Fill((byte)'x');
            else MorphText(s.D[2],c.PathMode=="capacity"?new string('x',2047):"parsed-old");
            return 0;
        });
        Register(baseAddress,DosLvo.NameFromLock,"NameFromLock",(s,i)=>
        {
            var l=MorphLease(i); var c=MorphCase(i); Require(s.D[1]==MorphDestLock(i) && s.D[2]==l.Workspace+2330 && s.D[3]==2048 && !l.Search,"Destination path layout/capacity.");
            if(c.NameMode=="unterminated") Bus.Memory.AsSpan((int)s.D[2],2048).Fill((byte)'x');
            else MorphText(s.D[2],c.NameMode=="empty"?"":MorphPrefix(c));
            if(c.Failure=="name") { i.IoError=c.Fault; return 0; }
            return 1;
        });
        Register(baseAddress,-870,"FilePart",(s,i)=>
        {
            var l=MorphLease(i); Require(l.Search && s.D[1]==l.Workspace+280 && Bus.CString(s.D[1])==MorphPath(l),"FilePart must use live matcher path, not FIB name.");
            return s.D[1]+4;
        });
        Register(baseAddress,-882,"AddPart",(s,i)=>
        {
            var l=MorphLease(i); var c=MorphCase(i); var prefix=MorphPrefix(c);
            Require(l.Search && s.D[1]==l.Workspace+2330 && s.D[2]==l.Workspace+284 && s.D[3]==2048 && Bus.CString(s.D[1])==prefix,"AddPart ABI/prefix restoration.");
            if(c.Failure=="addpart" && i.Events.Count(e=>e=="AddPart")==c.FailAddPartAt) { i.IoError=0; return 0; }
            var composed=prefix+(prefix.EndsWith(':')?"":"/")+Bus.CString(s.D[2]);
            if(composed.Length>=2048) { i.IoError=120; return 0; }
            if(c.Failure=="addpart-output") Bus.Memory.AsSpan((int)s.D[1],2048).Fill((byte)'x');
            else MorphText(s.D[1],composed);
            return 1;
        });
        Register(baseAddress,DosLvo.Rename,"Rename",(s,i)=>
        {
            var l=MorphLease(i); var c=MorphCase(i); l.Renames++;
            if(l.Search)
            {
                var prefix=MorphPrefix(c); if(!prefix.EndsWith(':')) prefix+="/";
                Require(l.Renames==l.Next+1 && s.D[1]==l.Workspace+280 && Bus.CString(s.D[1])==MorphPath(l) && s.D[2]==l.Workspace+2330 && Bus.CString(s.D[2])==prefix+(l.MatchIndex==0?"file":"next"),"MorphOS mutation must precede MatchNext with correct current paths.");
            }
            else Require(s.D[1]==l.Workspace+280 && Bus.CString(s.D[1])==(c.PathMode=="capacity"?new string('x',2047):"parsed-old") && Bus.CString(s.D[2])=="new", "Direct Rename paths.");
            i.IoError=c.Failure=="rename" && l.Renames==c.FailRenameAt?c.Fault:0;
            return c.Failure=="rename" && l.Renames==c.FailRenameAt?0u:1u;
        });
        Register(baseAddress,DosLvo.MatchNext,"MatchNext",(s,i)=>
        {
            var l=MorphLease(i); var c=MorphCase(i); l.Next++;
            Require(l.Search && s.D[1]==l.Workspace && l.Renames==l.Next,"Advance before successful current mutation.");
            if(++l.MatchIndex<c.Matches) { MorphText(l.Workspace+280,"SRC:next"); i.IoError=0; return 0; }
            if(c.FoundBreak) Bus.Long(l.Workspace+12,0x1000);
            i.IoError=c.NextResult; return unchecked((uint)c.NextResult);
        });
        Register(baseAddress,DosLvo.VPrintf,"VPrintf",(s,i)=>
        {
            var format=Bus.CString(s.D[1]); var l=MorphLease(i);
            Require(MorphStack(i,s.D[2],format.StartsWith("Destination")?4u:8u),"Format arguments not private stack cells.");
            string text;
            if(format=="Destination \"%s\" is not a directory.\n") text=$"Destination \"{Bus.CString(Bus.Long(s.D[2]))}\" is not a directory.\n";
            else
            {
                var from=Bus.CString(Bus.Long(s.D[2])); var to=Bus.CString(Bus.Long(s.D[2]+4));
                Require(format is "Renaming %s as %s\n" or "Can't rename %s as %s because ","Unexpected progress/failure format.");
                if(format.StartsWith("Renaming")) Require(l.Search && l.Renames==l.Next,"Progress must precede current mutation.");
                else if(l.Diagnostic!=0) Require(!l.Search && Bus.Long(s.D[2])==l.Diagnostic,"Failure path must be saved before MatchEnd.");
                text=format.StartsWith("Renaming")?$"Renaming {from} as {to}\n":$"Can't rename {from} as {to} because ";
            }
            i.Output.Write(Encoding.Latin1.GetBytes(text));
            if(MorphCase(i).OutputIoError is int error) i.IoError=error;
            return unchecked((uint)text.Length);
        });
    }
}
