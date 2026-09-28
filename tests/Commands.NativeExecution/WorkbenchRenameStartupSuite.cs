using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed partial class ProbeFixture
{
    public const string WorkbenchRenameStartupSuite = "workbench-rename-startup-vector-fixture";
    private readonly Dictionary<Invocation, List<uint>> renameVectors = new();
    private static int RenameFailedAllocation(Invocation i) => i.Definition.Name.StartsWith("allocation-")
        ? int.Parse(i.Definition.Name[11..]) : 0;

    private readonly Dictionary<Invocation,uint> renameParserStorage = new();
    private static bool RenameSearchCase(Invocation i) => i.Definition.Name.StartsWith("matcher-") || RenameDirectCase(i) || RenameDirectoryCase(i);
    private readonly Dictionary<Invocation,uint> renameFibs = new();
    private readonly HashSet<Invocation> renameDirectoryLocks = new();
    private static bool RenameExamineFailure(Invocation i) => i.Definition.Name=="directory-examine-direct";
    private static bool RenameDirectoryCase(Invocation i) => i.Definition.Name.StartsWith("directory-");
    private static bool RenameCompose(Invocation i) => i.Definition.Name.StartsWith("directory-compose-") && i.Definition.Name!="directory-compose-single-zero";
    private readonly HashSet<Invocation> renameSourceLocks = new();
    private static bool RenameMulti(Invocation i) => i.Definition.Name is "directory-compose-single-multi" or "directory-compose-single-multi-failure";
    private static bool RenameSingle(Invocation i) => i.Definition.Name.StartsWith("directory-compose-single-");
    private static string RenamePrefix(Invocation i) => i.Definition.Name switch {
        "directory-compose-single-multi-failure" or "directory-compose-single-multi" or "directory-compose-single-one" or "directory-compose-single-minus" or "directory-compose-single-missing" or "directory-compose-bad-name" or "directory-compose-bad-path" or "directory-compose-normal" or "directory-compose-progress" or "directory-compose-second-failure" or "directory-compose-next-break" or "directory-compose-second-zero" or "directory-compose-next-error" or "directory-compose-later-missing" => "OUT:",
        "directory-compose-slash-capacity" => new string('x',250),
        "directory-compose-slash-overflow" => new string('x',251),
        "directory-compose-slash-no-room" => new string('x',255),
        _ => new string('x',i.Definition.Name=="directory-compose-capacity"?250:251)+":"
    };
    private static uint RenameDirectoryLock(Invocation i) => i.Process+0x300;
    private static bool RenameDirectCase(Invocation i) => i.Definition.Name.StartsWith("direct-") || RenameExamineFailure(i) || i.Definition.Name=="directory-compose-single-zero";
    private static bool RenameParsedCase(Invocation i) => RenameParserFailure(i) || RenameSearchCase(i);
    private static bool RenameParserFailure(Invocation i) => i.Definition.Name.StartsWith("parser-");

    private static int RenameSelectedError(Invocation i) => i.Definition.Name=="direct-cleanup-error" ? 0 : i.Definition.Name is "parser-fault-fails" or "parser-cleanup-error" ? 116 : i.Definition.Error;

    private List<object> RunWorkbenchRenameStartupCases()
    {
        ProbeCase[] cases = [
            new("missing-dos", "", 20, 122, "") { MissingDos=true, WritesOwnProcessError=true },
            new("workbench", "", 10, (int)DOS.Error.ObjectWrongType, "") { Workbench=true },
            new("workbench-missing-dos", "", 20, 122, "") { Workbench=true, MissingDos=true, WritesOwnProcessError=true },
            new("negative-entry-length", "", 10, (int)DOS.Error.LineTooLong, "") { EntryLength=-1 },
            new("null-entry-buffer", "", 10, (int)DOS.Error.LineTooLong, "") { EntryLength=5, NullArgumentPointer=true },
            new("pending-break", "", 20, 304, "")
        ];
        cases = [..cases, ..Enumerable.Range(1,4).Select(n=>new ProbeCase($"allocation-{n}", "",20,103,""))];
        cases = [..cases, new("parser-116", "",20,116,""), new("parser-zero", "",20,0,"")];
        cases = [..cases, new("matcher-214", "",20,214,""), new("matcher-zero", "",0,0,"")];
        cases = [..cases, new("matcher-205", "",20,205,"Can't rename old as new because ")];
        cases = [..cases, new("direct-success", "",0,0,""), new("direct-pattern-failure", "",20,120,""), new("direct-pattern-zero", "",20,0,""), new("direct-unterminated", "",20,120,"")];
        cases = [..cases, new("direct-capacity", "",0,0,"")];
        cases = [..cases, new("directory-name-failure", "",20,120,""), new("directory-name-zero", "",20,0,""), new("directory-name-empty", "",20,210,""), new("directory-name-unterminated", "",20,120,"")];
        cases = [..cases, new("directory-compose-normal", "",0,0,""), new("directory-compose-capacity", "",0,0,""), new("directory-compose-overflow", "",20,120,"")];
        cases = [..cases, new("directory-compose-slash-capacity", "",0,0,""), new("directory-compose-slash-overflow", "",20,120,""), new("directory-compose-slash-no-room", "",20,120,"")];
        cases = [..cases, new("directory-compose-progress", "",0,0,"Renaming SRC:file as OUT:file\nRenaming SRC:file as OUT:file\n")];
        cases = [..cases, new("directory-compose-second-failure", "",20,203,"Can't rename SRC:file as OUT:file because ")];
        cases = [..cases, new("directory-fib-failure", "",20,103,""), new("directory-fib-zero", "",20,0,"")];
        cases = [..cases, new("directory-compose-next-break", "",0,0,""), new("directory-compose-next-error", "",0,0,"")];
        cases = [..cases, new("directory-compose-later-missing", "",20,205,"")];
        cases = [..cases, new("directory-compose-single-one", "",0,0,""), new("directory-compose-single-minus", "",0,0,""), new("directory-compose-single-missing", "",0,0,"")];
        cases = [..cases, new("directory-compose-single-zero", "",0,0,"")];
        cases = [..cases, new("directory-compose-bad-name", "",20,120,""), new("directory-compose-bad-path", "",20,120,"")];
        cases = [..cases, new("directory-compose-single-multi", "",0,0,"")];
        cases = [..cases, new("direct-false-zero", "",0,0,"Can't rename old as new because ")];
        cases = [..cases, new("parser-fault-fails", "",20,999,""), new("parser-cleanup-error", "",20,777,"")];
        cases = [..cases, new("directory-compose-second-zero", "",0,0,"Can't rename SRC:file as OUT:file because ")];
        cases = [..cases, new("direct-cleanup-error", "",0,777,"")];
        cases = [..cases, new("directory-compose-single-multi-failure", "",20,203,"Can't rename SRC:next as OUT:next because ")];
        cases = [..cases, new("directory-examine-direct", "",0,0,"")];
        // Candidate command budget only: supplied vectors do not consume real DOS stack.
        cases = cases.Select(c=>c with { StackBytes=4096 }).ToArray();
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([cases[0], cases[5]], true));
        reports.AddRange(Execute([cases[2], cases[1]], true));
        reports.AddRange(Execute([cases[6], cases[9]], true));
        reports.AddRange(Execute([cases[10], cases[11]], true));
        reports.AddRange(Execute([cases[12], cases[13]], true));
        reports.AddRange(Execute([cases[14], cases[13]], true));
        reports.AddRange(Execute([cases[15], cases[18]], true));
        reports.AddRange(Execute([cases[19], cases[18]], true));
        reports.AddRange(Execute([cases[20], cases[23]], true));
        reports.AddRange(Execute([cases[24], cases[25]], true));
        reports.AddRange(Execute([cases[27], cases[28]], true));
        reports.AddRange(Execute([cases[30], cases[24]], true));
        reports.AddRange(Execute([cases[31], cases[24]], true));
        reports.AddRange(Execute([cases[32], cases[24]], true));
        reports.AddRange(Execute([cases[34], cases[35]], true));
        reports.AddRange(Execute([cases[36], cases[24]], true));
        reports.AddRange(Execute([cases[37], cases[38]], true));
        reports.AddRange(Execute([cases[40], cases[37]], true));
        reports.AddRange(Execute([cases[41], cases[42]], true));
        reports.AddRange(Execute([cases[43], cases[37]], true));
        reports.AddRange(Execute([cases[44], cases[15]], true));
        reports.AddRange(Execute([cases[45], cases[46]], true));
        reports.AddRange(Execute([cases[47], cases[24]], true));
        reports.AddRange(Execute([cases[48], cases[46]], true));
        reports.AddRange(Execute([cases[49], cases[43]], true));
        reports.AddRange(Execute([cases[50], cases[24]], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void VerifyWorkbenchRenameStartup(Invocation i)
    {
        var failure=RenameFailedAllocation(i);
        Require(i.Reads==(RenameParsedCase(i)?1:0) && i.FreeArgs==(RenameSearchCase(i)?1:0) && i.Allocations==(RenameParsedCase(i)?4:Math.Max(0,failure-1)) && i.FreeMem==0,
            "Startup/break path unexpectedly entered argument allocation.");
        Require(i.Events.Count(v=>v=="VPrintf")== (i.Definition.Name=="directory-compose-progress"?2:i.Definition.Name is "direct-false-zero" or "matcher-205" or "directory-compose-second-failure" or "directory-compose-second-zero" or "directory-compose-single-multi-failure"?1:0),"Missing/unexpected failure prefix.");
        if(RenameDirectCase(i)) Require(i.Events.Count(v=>v=="ParsePattern")==1 &&
            i.Events.Count(v=>v=="Rename")== (i.Definition.Name is "directory-examine-direct" or "direct-cleanup-error" or "direct-false-zero" or "direct-success" or "direct-capacity" or "directory-compose-single-zero"?1:0),"Unsafe/missing direct mutation.");
        if(RenameDirectoryCase(i)) Require(!renameDirectoryLocks.Contains(i) && !renameFibs.ContainsKey(i) &&
            i.Events.Count(v=>v=="NameFromLock")== (i.Definition.Name.StartsWith("directory-fib-")||RenameExamineFailure(i)||i.Definition.Name=="directory-compose-single-zero"?0:1) && i.Events.Count(v=>v=="Rename")== (RenameMulti(i)?2:RenameExamineFailure(i)||RenameSingle(i)||i.Definition.Name=="directory-compose-later-missing"?1:RenameCompose(i)&&(i.Definition.Error==0||i.Definition.Name=="directory-compose-second-failure")?2:0) && i.Events.Contains("ParsePattern")== (RenameExamineFailure(i)||i.Definition.Name=="directory-compose-single-zero"),"Directory failure cleanup/mutation.");
        if(i.Definition.Name.StartsWith("directory-fib-")) Require(!i.Events.Contains("Examine") && !i.Events.Contains("FreeDosObject"),"Failed FIB used or freed.");
        var pendingBreak=i.Definition.Name=="pending-break";
        Require(i.Events.Count(v=>v=="CheckSignal")== (pendingBreak || failure>0 || RenameParsedCase(i) ? 1 : 0) &&
                i.Events.Count(v=>v=="PrintFault")== (pendingBreak || failure>0 || RenameParsedCase(i)&&RenameSelectedError(i)!=0 ? 1 : 0), "Startup/break dispatch mismatch.");
        if(failure>0 || RenameParsedCase(i)) Require(i.Events.Count(v=>v=="AllocVec")== (RenameParsedCase(i)?4:failure) &&
            i.Events.Count(v=>v=="FreeVec")==4 && i.Events.Count(v=>v=="UnLock")== (RenameSingle(i)&&i.Definition.Name!="directory-compose-single-missing"?2:1),"Allocation failure cleanup counts.");
    }

    private void RegisterRenameAllocationExec()
    {
        Register(ExecBase, ExecLvo.AllocVec,"AllocVec",(s,i)=>
        {
            var failure=RenameFailedAllocation(i);
            var index=i.Events.Count(v=>v=="AllocVec")-1;
            Require((failure>0 && index<failure || RenameParsedCase(i)&&index<4) && s.D[0]==new uint[]{80,538,256,256}[index] &&
                s.D[1]==new uint[]{0,0x10000,0,0x10000}[index],"Allocation request order/flags.");
            if(!renameVectors.TryGetValue(i,out var owned)) renameVectors[i]=owned=new();
            if(index+1==failure) return 0;
            var pointer=Bus.Allocate(i,s.D[0],"RenameVector",s.D[1]!=0);
            owned.Add(pointer); i.Allocations++; return pointer;
        });
        Register(ExecBase,ExecLvo.FreeVec,"FreeVec",(s,i)=>
        {
            var index=i.Events.Count(v=>v=="FreeVec")-1;
            Require(index<4 && i.Events.Contains("UnLock"),"FreeVec cleanup phase.");
            var slot=new int[]{1,0,3,2}[index]; var owned=renameVectors[i];
            var expected=slot<owned.Count ? owned[slot] : 0;
            Require(s.A[1]==expected,"FreeVec foreign pointer/order.");
            if(expected!=0) Bus.Release(i,expected,"RenameVector");
            if(i.Definition.Name is "parser-cleanup-error" or "direct-cleanup-error") i.IoError=774+index;
            return 0;
        });
    }

    private void RegisterWorkbenchRenameStartupDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_,i)=>unchecked((uint)i.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (s,i)=>
        {
            var old=i.IoError; i.IoError=unchecked((int)s.D[1]); return unchecked((uint)old);
        });
        Register(baseAddress, DosLvo.CheckSignal, "CheckSignal", (s,i)=>
        {
            Require((i.Definition.Name=="pending-break" || RenameFailedAllocation(i)>0 || RenameParsedCase(i)) && s.D[1]==0x1000,"Wrong break check.");
            return i.Definition.Name=="pending-break" ? 0x1000u : 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (s,i)=>
        {
            var error=RenameParsedCase(i) ? RenameSelectedError(i) : RenameFailedAllocation(i)>0 ? 103 : 304;
            Require(s.D[1]==error && s.D[2]==0 && i.Events.Count(v=>v=="CheckSignal")==1,"Wrong fault/header.");
            if(i.Definition.Name=="matcher-205") Require(i.IoError==205 &&
                i.Events.IndexOf("VPrintf")<i.Events.IndexOf("MatchEnd") && i.Events.IndexOf("MatchEnd")<i.Events.IndexOf("PrintFault"),
                "Failure prefix clobbered selected error or cleanup order.");
            if(i.Definition.Name=="directory-compose-second-failure") Require(i.IoError==203 &&
                i.Events.Count(v=>v=="Rename")==2 && i.Events.LastIndexOf("Rename")<i.Events.IndexOf("VPrintf") &&
                i.Events.IndexOf("VPrintf")<i.Events.LastIndexOf("MatchEnd"),"Partial failure selected error/order.");
            if(i.Definition.Name=="parser-fault-fails") { i.IoError=999; return 0; }
            i.IoError=error; return 1;
        });
        Register(baseAddress,DosLvo.ReadArgs,"ReadArgs",(s,i)=>
        {
            Require(RenameParsedCase(i) && i.Reads==0 && renameVectors[i].Count==4 &&
                Bus.CString(s.D[1])=="FROM/A/M,TO=AS/A,QUIET/S" && s.D[2]==renameVectors[i][0] && s.D[3]==0,
                "ReadArgs template/storage ownership.");
            Require(Bus.Long(s.D[2])==0 && Bus.Long(s.D[2]+4)==0 && Bus.Long(s.D[2]+8)==0,
                "Uninitialized ReadArgs result cells.");
            i.Reads++; i.IoError=RenameSelectedError(i);
            if(!RenameSearchCase(i)) return 0;
            var storage=Bus.Allocate(i,64,"RenameRDArgs",true); renameParserStorage[i]=storage;
            Bus.Long(s.D[2],storage); Bus.Long(s.D[2]+4,storage+40);
            Bus.Long(storage,storage+16);
            if(RenameDirectoryCase(i)&&!RenameSingle(i)&&!RenameExamineFailure(i)) Bus.Long(storage+4,storage+16);
            if(RenameCompose(i) && i.Definition.Name!="directory-compose-progress") Bus.Long(s.D[2]+8,1);
            System.Text.Encoding.ASCII.GetBytes("old\0").CopyTo(Bus.Memory,(int)storage+16);
            System.Text.Encoding.ASCII.GetBytes("new\0").CopyTo(Bus.Memory,(int)storage+40);
            return storage;
        });
        Register(baseAddress,DosLvo.MatchFirst,"MatchFirst",(s,i)=>
        {
            Require(RenameSearchCase(i) && Bus.CString(s.D[1])=="old" && s.D[2]==renameVectors[i][1],"MatchFirst ownership.");
            Require(Bus.Long(s.D[2]+8)==0x1000 && Bus.Memory[s.D[2]+16]==1,"Anchor configuration.");
            if(RenameCompose(i)) {
                System.Text.Encoding.ASCII.GetBytes("file\0").CopyTo(Bus.Memory,(int)s.D[2]+28);
                System.Text.Encoding.ASCII.GetBytes("SRC:file\0").CopyTo(Bus.Memory,(int)s.D[2]+280);
                if(i.Definition.Name=="directory-compose-bad-name") Bus.Memory.AsSpan((int)s.D[2]+28,108).Fill((byte)'x');
                if(i.Definition.Name=="directory-compose-bad-path") Bus.Memory.AsSpan((int)s.D[2]+280,256).Fill((byte)'x');
            }
            i.IoError=i.Definition.Error;
            if(i.Definition.Name=="directory-compose-later-missing" && i.Events.Count(v=>v=="MatchFirst")==3) return 205;
            return RenameDirectCase(i)||RenameDirectoryCase(i)?0u:103u;
        });
        Register(baseAddress,DosLvo.MatchEnd,"MatchEnd",(s,i)=>
        {
            Require(RenameSearchCase(i) && s.D[1]==renameVectors[i][1] &&
                i.Events.Count(v=>v=="MatchFirst")==i.Events.Count(v=>v=="MatchEnd"),"Search cleanup ownership/count.");
            return 0;
        });
        Register(baseAddress,DosLvo.Lock,"Lock",(s,i)=>
        {
            if(RenameSingle(i) && Bus.CString(s.D[1])=="old") {
                Require(s.D[2]==0xfffffffe && renameDirectoryLocks.Contains(i),"Source Lock mode/order.");
                if(i.Definition.Name=="directory-compose-single-missing") { i.IoError=205; return 0; }
                Require(renameSourceLocks.Add(i),"Repeated source lock."); return i.Process+0x304;
            }
            Require((RenameDirectCase(i)||RenameDirectoryCase(i)) && Bus.CString(s.D[1])=="new" && s.D[2]==0xfffffffe,"Direct destination lock.");
            if(RenameDirectoryCase(i)) { Require(renameDirectoryLocks.Add(i),"Repeated destination lock."); return RenameDirectoryLock(i); }
            i.IoError=205; return 0;
        });
        Register(baseAddress,DosLvo.SameLock,"SameLock",(s,i)=>
        {
            Require(RenameSingle(i) && renameSourceLocks.Contains(i) && s.D[1]==i.Process+0x304 && s.D[2]==RenameDirectoryLock(i),"SameLock pointer ownership.");
            return i.Definition.Name=="directory-compose-single-zero"?0u:i.Definition.Name=="directory-compose-single-minus"?0xffffffff:1u;
        });
        Register(baseAddress,-228,"AllocDosObject",(s,i)=>
        {
            Require(RenameDirectoryCase(i) && renameDirectoryLocks.Contains(i) && s.D[1]==2 && s.D[2]==0,"Directory FIB allocation.");
            if(i.Definition.Name.StartsWith("directory-fib-")) { i.IoError=i.Definition.Error; return 0; }
            var fib=Bus.Allocate(i,260,"RenameFIB",true); renameFibs.Add(i,fib); return fib;
        });
        Register(baseAddress,DosLvo.Examine,"Examine",(s,i)=>
        {
            Require(s.D[1]==RenameDirectoryLock(i) && s.D[2]==renameFibs[i],"Directory Examine ownership.");
            Bus.Long(s.D[2]+4,2);
            if(RenameExamineFailure(i)) { i.IoError=222; return 0; }
            return 0xffffffff;
        });
        Register(baseAddress,-234,"FreeDosObject",(s,i)=>
        {
            Require(s.D[1]==2 && s.D[2]==renameFibs[i],"Directory FIB release.");
            Bus.Release(i,s.D[2],"RenameFIB"); renameFibs.Remove(i); return 0;
        });
        Register(baseAddress,DosLvo.NameFromLock,"NameFromLock",(s,i)=>
        {
            Require(RenameDirectoryCase(i) && renameDirectoryLocks.Contains(i) && s.D[1]==RenameDirectoryLock(i) &&
                s.D[2]==renameVectors[i][3] && s.D[3]==256 && i.Events.Count(v=>v=="MatchEnd")==1,"Destination name ownership/capacity.");
            i.IoError=i.Definition.Error;
            if(i.Definition.Name is "directory-name-failure" or "directory-name-zero") return 0;
            if(RenameCompose(i)) System.Text.Encoding.ASCII.GetBytes(RenamePrefix(i)+"\0").CopyTo(Bus.Memory,(int)s.D[2]);
            else if(i.Definition.Name=="directory-name-empty") Bus.Memory[s.D[2]]=0;
            else Bus.Memory.AsSpan((int)s.D[2],256).Fill((byte)'x');
            return 0xffffffff;
        });
        Register(baseAddress,DosLvo.ParsePattern,"ParsePattern",(s,i)=>
        {
            Require(RenameDirectCase(i) && Bus.CString(s.D[1])=="old" && s.D[2]==renameVectors[i][2] && s.D[3]==256 &&
                i.Events.Count(v=>v=="MatchEnd")==1,"Pattern buffer or search lifetime.");
            i.IoError=i.Definition.Error;
            if(i.Definition.Name is "direct-pattern-failure" or "direct-pattern-zero") return 0xffffffff;
            if(i.Definition.Name=="direct-unterminated") Bus.Memory.AsSpan((int)s.D[2],256).Fill((byte)'x');
            else if(i.Definition.Name=="direct-capacity") { Bus.Memory.AsSpan((int)s.D[2],255).Fill((byte)'x'); Bus.Memory[s.D[2]+255]=0; }
            else System.Text.Encoding.ASCII.GetBytes("parsed-old\0").CopyTo(Bus.Memory,(int)s.D[2]);
            return 0;
        });
        Register(baseAddress,DosLvo.MatchNext,"MatchNext",(s,i)=>
        {
            Require(RenameCompose(i) && (i.Definition.Error==0||i.Definition.Name is "directory-compose-second-failure" or "directory-compose-later-missing" or "directory-compose-single-multi-failure") && s.D[1]==renameVectors[i][1],"Unexpected traversal advance.");
            if(RenameMulti(i) && i.Events.Count(v=>v=="MatchNext")==1) {
                System.Text.Encoding.ASCII.GetBytes("next\0").CopyTo(Bus.Memory,(int)s.D[1]+28);
                System.Text.Encoding.ASCII.GetBytes("SRC:next\0").CopyTo(Bus.Memory,(int)s.D[1]+280);
                i.IoError=0; return 0;
            }
            Bus.Memory.AsSpan((int)s.D[1]+28,5).Fill((byte)'z');
            Bus.Memory.AsSpan((int)s.D[1]+280,9).Fill((byte)'z');
            var next=i.Definition.Name=="directory-compose-next-break"?304:i.Definition.Name=="directory-compose-next-error"?103:232;
            i.IoError=next; return (uint)next;
        });
        Register(baseAddress,DosLvo.Rename,"Rename",(s,i)=>
        {
            if(RenameCompose(i)) {
                var name=RenameMulti(i) && i.Events.Count(v=>v=="Rename")==2?"next":"file";
                Require((i.Definition.Error==0||i.Definition.Name is "directory-compose-second-failure" or "directory-compose-later-missing" or "directory-compose-single-multi-failure") && s.D[1]==renameVectors[i][2] && s.D[2]==renameVectors[i][3] &&
                    Bus.CString(s.D[1])=="SRC:"+name && Bus.CString(s.D[2])==RenamePrefix(i)+(RenamePrefix(i).EndsWith(":")?"":"/")+name &&
                    i.Events.Count(v=>v=="MatchNext")==i.Events.Count(v=>v=="Rename"),"Composed paths or advance-before-rename.");
                if(i.Definition.Name=="directory-compose-second-zero" && i.Events.Count(v=>v=="Rename")==2) { i.IoError=0; return 0; }
                if((i.Definition.Name is "directory-compose-second-failure" or "directory-compose-single-multi-failure") && i.Events.Count(v=>v=="Rename")==2) { i.IoError=203; return 0; }
                i.IoError=0; return 0xffffffff;
            }
            Require((i.Definition.Name is "directory-examine-direct" or "direct-cleanup-error" or "direct-false-zero" or "direct-success" or "direct-capacity" or "directory-compose-single-zero") && s.D[1]==renameVectors[i][2] &&
                Bus.CString(s.D[1])==(i.Definition.Name=="direct-capacity"?new string('x',255):"parsed-old") && Bus.CString(s.D[2])=="new" && i.Events.Contains("ParsePattern"),"Direct rename arguments.");
            i.IoError=0; return i.Definition.Name=="direct-false-zero"?0u:0xffffffff;
        });
        Register(baseAddress,DosLvo.VPrintf,"VPrintf",(s,i)=>
        {
            if(i.Definition.Name=="directory-compose-progress") {
                Require(Bus.CString(s.D[1])=="Renaming %s as %s\n" && s.D[2]==renameVectors[i][0]+16 &&
                    Bus.CString(Bus.Long(s.D[2]))=="SRC:file" && Bus.CString(Bus.Long(s.D[2]+4))=="OUT:file" &&
                    i.Events.Count(v=>v=="MatchNext")==i.Events.Count(v=>v=="VPrintf") &&
                    i.Events.Count(v=>v=="Rename")+1==i.Events.Count(v=>v=="VPrintf"),"Directory progress arguments/order.");
                i.Output.Write(System.Text.Encoding.Latin1.GetBytes("Renaming SRC:file as OUT:file\n"));
                i.IoError=999; return 0xffffffff;
            }
            Require((i.Definition.Name is "direct-false-zero" or "matcher-205" or "directory-compose-second-failure" or "directory-compose-second-zero" or "directory-compose-single-multi-failure") && Bus.CString(s.D[1])=="Can't rename %s as %s because " &&
                s.D[2]==renameVectors[i][0]+16,"Failure prefix template/storage.");
            var from=Bus.CString(Bus.Long(s.D[2])); var to=Bus.CString(Bus.Long(s.D[2]+4));
            Require(from==(i.Definition.Name is "matcher-205" or "direct-false-zero"?"old":i.Definition.Name=="directory-compose-single-multi-failure"?"SRC:next":"SRC:file") && to==(i.Definition.Name is "matcher-205" or "direct-false-zero"?"new":i.Definition.Name=="directory-compose-single-multi-failure"?"OUT:next":"OUT:file"),"Failure prefix arguments.");
            i.Output.Write(System.Text.Encoding.Latin1.GetBytes($"Can't rename {from} as {to} because "));
            i.IoError=999; return 0xffffffff;
        });
        Register(baseAddress,DosLvo.FreeArgs,"FreeArgs",(s,i)=>
        {
            Require(RenameSearchCase(i) && s.D[1]==renameParserStorage[i] && i.Events.Contains("MatchEnd"),"Parser cleanup identity/order.");
            Bus.Release(i,s.D[1],"RenameRDArgs"); i.FreeArgs++; return 0;
        });
        Register(baseAddress,DosLvo.UnLock,"UnLock",(s,i)=>
        {
            if(RenameSingle(i) && s.D[1]==i.Process+0x304) {
                Require(renameSourceLocks.Remove(i) && i.Events.Contains("SameLock"),"Source unlock ownership/order."); return 0;
            }
            Require(!renameSourceLocks.Contains(i),"Leaked temporary source lock.");
            Require((RenameFailedAllocation(i)>0 || RenameParsedCase(i)) && s.D[1]==(RenameDirectoryCase(i)?RenameDirectoryLock(i):0),"Unexpected cleanup lock.");
            if(RenameDirectoryCase(i)) Require(renameDirectoryLocks.Remove(i),"Foreign/repeated directory unlock."); return 0;
        });
    }
}
