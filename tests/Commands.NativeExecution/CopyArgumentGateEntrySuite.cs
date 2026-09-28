using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>
/// Supplied-DOS receipt for Copy's exact MorphOS ReadArgs/mode boundary.
/// Matcher, file effects, metadata, and command output are outside this scope.
/// </summary>
internal sealed record CopyArgumentGateEntryCase(string[]? Inputs, bool HasTarget,
    bool HasPattern, uint Switches, int ParserError = 0, uint[]? SetupValues = null, string? DirectKind = null, string? NormalKind = null, bool CompletionBreak = false, ushort DosVersion = 40, ushort DosRevision = 0, bool WorkspaceFailure = false, bool ParserAllocationFailure = false);

internal sealed class CopyArgumentGateNativeLayout(uint rdArgs, uint fromVector,
    uint target, uint pattern)
{
    public uint RdArgs { get; } = rdArgs;
    public uint FromVector { get; } = fromVector;
    public uint Target { get; } = target;
    public uint Pattern { get; } = pattern;
    public int Opens, Closes, Deletes, Writes, TransferReads, Matches, Ends, Unlocks;
    public uint DestinationFib;
    public uint CompiledPattern; public int Parses, Filters;
    public uint Anchor; public string CurrentSource = "";
}

internal sealed partial class ProbeFixture
{
    public const string CopyParsedMakeDirectorySuite = "copy-parsed-makedir-native-entry-vector-fixture";
    public const string CopyParsedDeleteSuite = "copy-parsed-delete-native-entry-vector-fixture";
    public const string CopyDirectSuite = "copy-direct-native-entry-vector-fixture";
    public const string CopyOptionSetupSuite = "copy-option-setup-native-entry-vector-fixture";
    public const string CopyArgumentGateEntrySuite = "copy-argument-gate-native-entry-vector-fixture";
    private const uint All = 1u << 4, Direct = 1u << 5, Clone = 1u << 6,
        MakeDirectory = 1u << 14, Move = 1u << 15, Delete = 1u << 16,
        HardLink = 1u << 17, SoftLink = 1u << 18, ForceLink = 1u << 19,
        ForceDelete = 1u << 20, ForceOverwrite = 1u << 21,
        DontOverwrite = 1u << 22, CompatibilityForce = 1u << 23;

    private List<object> RunCopyArgumentGateEntryCases()
    {
        ProbeCase[] cases =
        [
            Case("copy-positional-target", ["Work:From", "RAM:To"], false, false, 0, DOS.RETURN_OK, 0),
            Case("direct-positional-target", ["Work:From", "RAM:To"], false, false, Direct, DOS.RETURN_OK, 0),
            Case("delete-force", ["Work:From"], false, false, Delete | CompatibilityForce, DOS.RETURN_OK, 0),
            Case("move-positional-target", ["Work:From", "RAM:To"], false, false, Move, DOS.RETURN_OK, 0),
            Case("hardlink-positional-target", ["Work:From", "RAM:To"], false, false, HardLink, DOS.RETURN_OK, 0),
            Case("softlink-positional-target", ["Work:From", "RAM:To"], false, false, SoftLink, DOS.RETURN_OK, 0),
            Case("direct-delete-multiple", ["Work:One", "Work:Two"], false, false, Direct | Delete, DOS.RETURN_OK, 0),
            Case("direct-copy-multiple", ["Work:One", "Work:Two", "RAM:To"], false, false, Direct, DOS.RETURN_FAIL, (int)DOS.Error.TooManyArguments),
            Case("move-needs-target", ["Work:From"], false, false, Move, DOS.RETURN_FAIL, (int)DOS.Error.TooManyArguments),
            Case("mode-conflict", ["Work:From"], false, false, Move | Delete, DOS.RETURN_FAIL, (int)DOS.Error.TooManyArguments),
            Case("direct-clone", ["Work:From"], true, false, Direct | Clone, DOS.RETURN_FAIL, (int)DOS.Error.TooManyArguments),
            Case("direct-delete-force", ["Work:From"], false, false, Direct | Delete | CompatibilityForce, DOS.RETURN_FAIL, (int)DOS.Error.TooManyArguments),
            Case("overwrite-conflict", ["Work:From"], true, false, ForceOverwrite | DontOverwrite, DOS.RETURN_FAIL, (int)DOS.Error.TooManyArguments),
            Case("copy-no-input-or-target", null, false, false, 0, DOS.RETURN_FAIL, (int)DOS.Error.TooManyArguments),
            Case("implicit-makedir", null, false, false, MakeDirectory, DOS.RETURN_FAIL, (int)DOS.Error.TooManyArguments),
            Case("parser-failure", null, false, false, 0, DOS.RETURN_ERROR, 116, 116),
        ];
        if (suite is CopyOptionSetupSuite or CopyDirectSuite or CopyParsedDeleteSuite or CopyParsedMakeDirectorySuite)
        {
            cases = [
                SetupCase("positional", ["Work:From","RAM:To"],false,0,[0,2048,1,524288,0,0,0,1]),
                SetupCase("move", ["Work:From","RAM:To"],false,Move,[1,2048,1,524288,0,0,0,1]),
                SetupCase("omitted-source",null,true,0,[0,2048,1,524288,0,0,1,1]),
                SetupCase("clone-quiet",["Work:From"],true,(1u<<6)|(1u<<11),[0,2314,25,524288,0,0,0,1]),
                SetupCase("prox-nopro-noreq",["Work:From"],true,(1u<<8)|(1u<<9)|(1u<<12),[0,6148,7,524288,0,1,0,1]),
                SetupCase("multiple-delete",["Work:One","Work:Two"],false,Delete|Direct,[2,2560,1,524288,1,0,0,2])
            ];
            if (suite == CopyDirectSuite)
                cases = [DirectCase("copy"),DirectCase("input-fail"),DirectCase("output-fail"),DirectCase("write-fail"),DirectCase("delete"),DirectCase("unknown-size"),DirectCase("read-fail"),DirectCase("premature-eof")];
            if (suite == CopyParsedDeleteSuite) cases = [NormalCase("one"),NormalCase("two"),NormalCase("failed-continue"),NormalCase("failed-stop"),PatternCase("pattern-accept"),PatternCase("pattern-reject"),PatternCase("pattern-invalid"),PatternCase("pattern-empty")];
            if (suite == CopyParsedMakeDirectorySuite) cases = [MakeDirectoryCase("mkdir-one"),MakeDirectoryCase("mkdir-two"),MakeDirectoryCase("mkdir-create-fail"),MakeDirectoryCase("mkdir-pattern-fail"),MakeDirectoryCase("mkdir-wildcard")];
            if (explicitCopyParser && suite == CopyDirectSuite)
                cases = [..cases, new("parser-failure","",DOS.RETURN_FAIL,116,""){EntryLength=32,CopyArgumentGate=new(null,false,false,0,116)}];
            if(explicitCopyParser && suite==CopyDirectSuite)
            {
                cases=cases.Select(c=>{
                    if(c.CopyArgumentGate!.DirectKind is not ("copy" or "output-fail")) return c;
                    var values=(uint[])c.CopyArgumentGate.SetupValues!.Clone();values[5]=1;
                    return c with {CopyArgumentGate=c.CopyArgumentGate with {Switches=c.CopyArgumentGate.Switches|(1u<<12),SetupValues=values}};
                }).ToArray();
            }
            if(explicitCopyParser && suite==CopyDirectSuite)
                cases=[..cases,new("rejected-direct-clone-noreq","",DOS.RETURN_FAIL,(int)DOS.Error.TooManyArguments,""){
                    EntryLength=32,CopyArgumentGate=new(["Work:From"],true,false,Direct|Clone|(1u<<12),0,
                        [0,2058,25,524288,1,1,0,20])}];
            if(copyCommandRoot) cases=cases.Select(c=>c with {
                EntryLength=null, Result=c.CopyArgumentGate!.SetupValues is {} v?(int)(v[6]!=0?v[6]==5&&(c.CopyArgumentGate.Switches&(1u<<13))!=0?10:v[6]:v[7]):DOS.RETURN_FAIL, Error=902
            }).ToArray();
            if(copyCommandRoot && suite==CopyDirectSuite)
                cases=[..cases,cases[0] with {Name="final-break-errwarn",Result=DOS.RETURN_ERROR,
                    CopyArgumentGate=cases[0].CopyArgumentGate! with {CompletionBreak=true,Switches=cases[0].CopyArgumentGate!.Switches|(1u<<13)}}];
            if(copyCommandRoot && suite==CopyDirectSuite)
                foreach(var version in new[]{(51,65),(51,66),(52,0)})
                    cases=[..cases,cases[0] with {Name=$"dos-{version.Item1}-{version.Item2}",
                        CopyArgumentGate=cases[0].CopyArgumentGate! with {DosVersion=(ushort)version.Item1,DosRevision=(ushort)version.Item2}}];
            if(copyCommandRoot && suite==CopyParsedMakeDirectorySuite)
                foreach(var kind in new[]{"mkdir-existing","mkdir-examine-fail"})
                    cases=[..cases,cases[0] with {Name=kind,Result=kind=="mkdir-existing"?0:10,
                        CopyArgumentGate=cases[0].CopyArgumentGate! with {NormalKind=kind,DosVersion=51,DosRevision=66}}];
            if(copyCommandRoot && suite==CopyDirectSuite)
                cases=[..cases,cases[0] with {Name="missing-dos",MissingDos=true,Result=DOS.RETURN_FAIL,Error=902}];
            if(copyCommandRoot && suite==CopyDirectSuite)
                foreach(var missing in new[]{false,true})
                    cases=[..cases,cases[0] with {Name=missing?"missing-dos-no-memory":"no-workspace",MissingDos=missing,
                        Result=DOS.RETURN_FAIL,Error=Invocation.InitialIoError,CopyArgumentGate=cases[0].CopyArgumentGate! with {WorkspaceFailure=true}}];
            if(copyCommandRoot && suite==CopyDirectSuite)
                cases=[..cases,cases[0] with {Name="workbench-rejected",Workbench=true,Result=DOS.RETURN_FAIL,Error=Invocation.InitialIoError}];
            if(copyCommandRoot && suite==CopyDirectSuite)
                cases=[..cases,cases[0] with {Name="no-parser-object",Result=DOS.RETURN_FAIL,Error=902,
                    CopyArgumentGate=cases[0].CopyArgumentGate! with {ParserAllocationFailure=true}}];
            if(copyCommandRoot && suite==CopyParsedMakeDirectorySuite)
                foreach(var kind in new[]{"mkdir-existing","mkdir-examine-fail"})
                    cases=[..cases,cases[0] with {Name=kind+"-classic",Result=kind=="mkdir-existing"?0:10,
                        CopyArgumentGate=cases[0].CopyArgumentGate! with {NormalKind=kind,DosVersion=40,DosRevision=0}}];
            if(copyCommandRoot && suite==CopyDirectSuite)
                foreach(var kind in new[]{"unknown-size","read-fail","premature-eof","write-fail"})
                {
                    var baseline=cases.Single(c=>c.Name==kind);
                    cases=[..cases,baseline with {Name=kind+"-extended",
                        CopyArgumentGate=baseline.CopyArgumentGate! with {DosVersion=51,DosRevision=66}}];
                }
            var setupReports = new List<object>();
            foreach (var test in cases) setupReports.AddRange(Execute([test],false));
            setupReports.AddRange(Execute([cases[0] with {Name="interleaved-positional"},cases[2] with {Name="interleaved-default"}],true));
            Bus.AssertImageUnchanged();return setupReports;
        }
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute(
        [
            Case("interleaved-makedir", ["RAM:New"], false, false, MakeDirectory, DOS.RETURN_OK, 0),
            Case("interleaved-soft-all", ["Work:Dir"], true, false, SoftLink | All, DOS.RETURN_FAIL, (int)DOS.Error.TooManyArguments),
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase MakeDirectoryCase(string kind)=>new(kind,"",DOS.RETURN_OK,0,""){
        EntryLength=32,CopyArgumentGate=new(kind=="mkdir-one"?["Work:One"]:["Work:One","Work:Two"],false,false,MakeDirectory|(1u<<11),0,
            [3,kind=="mkdir-create-fail"?2304u:4196608u,1,524288,0,0,kind is "mkdir-create-fail" or "mkdir-wildcard"?10u:0,kind=="mkdir-pattern-fail"?20u:0],null,kind)};
    private static ProbeCase PatternCase(string kind)=>new(kind,"",DOS.RETURN_OK,0,""){
        EntryLength=32,CopyArgumentGate=new(["Work:One","Work:Two"],false,true,Delete|(1u<<11),0,
            [2,kind=="pattern-accept"?4196608u:2304,1,524288,0,0,0,kind is "pattern-invalid" or "pattern-empty"?20u:0],null,kind)};
    private static ProbeCase NormalCase(string kind)=>new(kind,"",DOS.RETURN_OK,0,""){
        EntryLength=32,CopyArgumentGate=new(kind=="one"?["Work:One"]:["Work:One","Work:Two"],false,false,
            Delete|(1u<<11)|(kind=="failed-stop"?1u<<13:0),0,
            [2,4196608u|(kind=="failed-stop"?1024u:0),1,524288,0,0,kind.StartsWith("failed")?5u:0,0],null,kind)};
    private static ProbeCase DirectCase(string kind)=>new(kind,"",DOS.RETURN_OK,0,""){
        EntryLength=32,CopyArgumentGate=new(kind=="delete"?["Work:One","Work:Two"]:["Work:From"],kind!="delete",false,
            Direct|(kind=="delete"?Delete:0),0,[kind=="delete"?2u:0,kind=="delete"?2560u:2048,1,512,1,0,0,kind is "copy" or "delete" or "unknown-size"?0u:20],kind)};
    private static ProbeCase SetupCase(string name,string[]? inputs,bool target,uint switches,uint[] expected)=>
        new(name,"",DOS.RETURN_OK,0,""){EntryLength=32,CopyArgumentGate=new(inputs,target,false,switches,0,expected)};

    private static ProbeCase Case(string name, string[]? inputs, bool target,
        bool pattern, uint switches, int result, int error, int parserError = 0) =>
        new(name, "", result, error, "")
        {
            CopyArgumentGate = new(inputs, target, pattern, switches, parserError)
        };

    private void PrepareCopyArgumentGateEntry(Invocation invocation)
    {
        var definition = invocation.Definition.CopyArgumentGate ??
            throw new InvalidOperationException("Missing Copy argument-gate definition.");
        if(copyCommandRoot && (invocation.Definition.Workbench||invocation.Definition.MissingDos||definition.WorkspaceFailure||definition.ParserAllocationFailure)) return;
        var rdArgs = Bus.Allocate(invocation, 512, "CopyArgumentGateRDArgs", true);
        var layout = new CopyArgumentGateNativeLayout(rdArgs, rdArgs + 320,
            rdArgs + 256, rdArgs + 288);
        invocation.CopyArgumentGateLayout = layout;
        if(explicitCopyParser) Bus.Long(invocation.Process+(uint)DosLayout.Process.WindowPointer, invocation.Process+0x380);
        if (definition.Inputs is { } inputs)
        {
            var text = rdArgs + 32;
            for (var index = 0; index < inputs.Length; index++)
            {
                Bus.Long(layout.FromVector + (uint)(index * 4), text);
                Encoding.Latin1.GetBytes(inputs[index]).CopyTo(Bus.Memory.AsSpan((int)text));
                text += (uint)inputs[index].Length;
                Bus.Memory[text++] = 0;
            }
        }
        if (definition.HasTarget)
        {
            Encoding.Latin1.GetBytes("RAM:Target").CopyTo(Bus.Memory.AsSpan((int)layout.Target));
            Bus.Memory[layout.Target + 10] = 0;
        }
        if (definition.HasPattern)
        {
            Encoding.Latin1.GetBytes("#?").CopyTo(Bus.Memory.AsSpan((int)layout.Pattern));
            Bus.Memory[layout.Pattern + 2] = 0;
            if (definition.NormalKind == "pattern-empty") Bus.Memory[layout.Pattern] = 0;
        }
    }

    private void VerifyCopyArgumentGateEntry(Invocation invocation)
    {
        if(copyCommandRoot && invocation.Definition.CopyArgumentGate!.ParserAllocationFailure)
        {
            Require(invocation.Allocations==1&&invocation.FreeMem==1&&invocation.Reads==0&&invocation.FreeArgs==0&&
                invocation.Events.Count(x=>x=="AllocDosObject")==1&&!invocation.Events.Contains("FreeDosObject")&&
                invocation.Events.Count(x=>x=="PrintFault")==1&&!invocation.Events.Contains("Open")&&
                invocation.Events.IndexOf("PrintFault")<invocation.Events.IndexOf("FreeMem"),
                "Failed parser object allocation must report before workspace cleanup without parser/free calls.");
            return;
        }
        if(copyCommandRoot && invocation.Definition.Workbench)
        {
            Require(invocation.Allocations==0&&invocation.FreeMem==0&&invocation.Reads==0&&invocation.FreeArgs==0&&
                invocation.Events.IndexOf("WaitPort")<invocation.Events.IndexOf("Forbid")&&
                invocation.Events.IndexOf("Forbid")<invocation.Events.IndexOf("GetMsg")&&
                invocation.Events.IndexOf("GetMsg")<invocation.Events.IndexOf("ReplyMsg"),
                "Copy Workbench startup ownership/order differs.");
            return;
        }
        if(copyCommandRoot && invocation.Definition.CopyArgumentGate!.WorkspaceFailure)
        {
            Require(invocation.Allocations==1&&invocation.FreeMem==0&&invocation.Reads==0&&invocation.FreeArgs==0&&
                !invocation.Events.Contains("AllocDosObject")&&invocation.Events.Count(x=>x=="PrintFault")==
                (invocation.Definition.MissingDos?0:1),"Workspace failure cleanup/reporting differs.");
            return;
        }
        if(copyCommandRoot && invocation.Definition.MissingDos)
        {
            Require(invocation.Allocations==1&&invocation.FreeMem==1&&invocation.Reads==0&&invocation.FreeArgs==0&&
                invocation.Closes==0&&!invocation.Events.Contains("AllocDosObject")&&!invocation.Events.Contains("PrintFault")&&
                !invocation.Events.Contains("SetIoErr")&&!invocation.Events.Contains("Open"),
                "Missing DOS must allocate/free workspace without calling DOS.");
            return;
        }
        var definition = invocation.Definition.CopyArgumentGate ??
            throw new InvalidOperationException("Missing Copy argument-gate definition.");
        var parserFailed = definition.ParserError != 0;
        if(invocation.Definition.Name=="rejected-direct-clone-noreq")
            Require(invocation.Events.Count(x=>x=="MatchFirst")==1&&invocation.Events.Count(x=>x=="MatchEnd")==1&&
                !invocation.Events.Contains("Open")&&!invocation.Events.Contains("DeleteFile"),
                "Rejected admission must classify before rejecting without running operations.");
        if(copyCommandRoot)
        {
            if(definition.CompletionBreak) Require(invocation.Events.LastIndexOf("SetSignal")>invocation.Events.IndexOf("FreeDosObject"),
                "Final cancellation must be observed after parser teardown.");
            var faults=(invocation.Definition.Result==DOS.RETURN_FAIL||definition.CompletionBreak)&&(definition.Switches&(1u<<11))==0?1:0;
            Require(invocation.Events.Count(x=>x=="PrintFault")==faults,"Command final diagnostic count differs.");
            Require(invocation.Events.LastIndexOf("FreeDosObject")<invocation.Events.LastIndexOf("FreeMem"),"Command workspace/cache freed before parser.");
        }
        if (explicitCopyParser)
        {
            Require(invocation.Events.Count(x=>x=="AllocDosObject")==((definition.NormalKind is "mkdir-existing" or "mkdir-examine-fail")?2:1)&&invocation.Events.Count(x=>x=="FreeDosObject")==((definition.NormalKind is "mkdir-existing" or "mkdir-examine-fail")?2:1),
                "Explicit RDArgs object lifetime differs.");
            Require(invocation.Events.IndexOf("ReadArgs")<invocation.Events.IndexOf("FreeDosObject")&&
                (parserFailed||invocation.Events.IndexOf("FreeArgs")<invocation.Events.LastIndexOf("FreeDosObject")),
                "Explicit parser cleanup order differs.");
        }
        Require(invocation.Reads == 1 && invocation.FreeArgs == (parserFailed ? 0 : 1),
            "Copy ReadArgs lifetime differs.");
        var normalOperations = suite == CopyParsedMakeDirectorySuite ? definition.NormalKind=="mkdir-two"?2:1 : definition.NormalKind is "pattern-invalid" or "pattern-empty" ? 0 : definition.NormalKind is "one" or "failed-stop" ? 1 : 2;
        var expectedAllocations = definition.NormalKind is not null ? 2 + normalOperations + (definition.HasPattern && definition.NormalKind != "pattern-empty" ? 1 : 0) : definition.DirectKind is "copy" or "write-fail" or "unknown-size" or "read-fail" or "premature-eof" ? 3 : definition.SetupValues is null ? 1 : 2;
        if(copyCommandRoot && !parserFailed) expectedAllocations--;
        if (!copyCommandRoot && definition.SetupValues is { } values) for (var index=0;index<values.Length;index++)
            Require(Bus.Long(invocation.Arguments+(uint)index*4)==values[index], $"Option setup field {index} differs.");
        Require(invocation.Allocations == expectedAllocations && invocation.FreeMem == expectedAllocations,
            "Copy argument result storage lifetime differs.");
        Require(invocation.Events.IndexOf("ReadArgs") < invocation.Events.IndexOf("CloseLibrary"),
            "Copy parser did not run before DOS close.");
        if (suite == CopyParsedMakeDirectorySuite) { var l=invocation.CopyArgumentGateLayout!; var failed=definition.NormalKind=="mkdir-create-fail"; if(definition.NormalKind is "mkdir-existing" or "mkdir-examine-fail")
            Require(l.Parses==1&&l.Writes==0&&l.Opens==(definition.NormalKind=="mkdir-existing"?2:1)&&l.Unlocks==(definition.NormalKind=="mkdir-existing"?2:1)&&l.DestinationFib==0,"Existing destination lifecycle differs.");
            else Require(l.Parses==normalOperations&&l.Writes==normalOperations&&l.Opens==(failed?1:normalOperations*2)&&l.Unlocks==(failed?0:normalOperations*2),"MAKEDIR lifecycle differs."); }
        else if (definition.NormalKind is not null) { var l=invocation.CopyArgumentGateLayout!; Require(l.Matches==normalOperations&&l.Ends==normalOperations&&l.Deletes==(definition.NormalKind=="pattern-reject"?0:normalOperations)&&l.Unlocks==(definition.NormalKind=="pattern-reject"?0:normalOperations*2),"Parsed DELETE lifecycle differs."); Require(l.Parses==(definition.HasPattern&&definition.NormalKind!="pattern-empty"?1:0)&&l.Filters==(definition.NormalKind is "pattern-accept" or "pattern-reject"?2:0),"Pattern lifecycle differs."); }
        if (definition.DirectKind is { } kind)
        {
            var l=invocation.CopyArgumentGateLayout!;
            Require(l.Opens==(kind=="delete"?0:kind=="input-fail"?1:2)&&
                l.Closes==(kind is "delete" or "input-fail"?0:kind=="output-fail"?1:2)&&
                l.Deletes==(kind=="delete"?2:0)&&l.Writes==(kind is "unknown-size" or "premature-eof"?2:kind is "copy" or "write-fail"?1:0),"DIRECT operation lifecycle differs.");
        }
        invocation.CopyArgumentGateLayout = null;
    }

    private void RegisterCopyArgumentGateEntryDos(uint baseAddress)
    {
        if(copyCommandRoot)
        {

            Register(baseAddress,DosLvo.PrintFault,"PrintFault",(s,i)=>{
                if(i.Definition.CopyArgumentGate!.ParserAllocationFailure){Require(s.D[1]==103&&s.D[2]==0&&i.FreeMem==0,"Parser allocation fault differs.");return 0;}
                if(i.Definition.CopyArgumentGate!.WorkspaceFailure){Require(s.D[1]==unchecked((uint)Invocation.InitialIoError)&&s.D[2]==0&&i.FreeMem==0,"Workspace failure fault differs.");return 0;}
                Require(s.D[1]==(i.Definition.CopyArgumentGate!.CompletionBreak?304u:903u)&&s.D[2]==0&&i.FreeMem==0&&i.Events.Contains("FreeDosObject"),
                    "Command diagnostic must observe parser cleanup before freeing cache/workspace.");return 0;
            });
        }
        if (explicitCopyParser)
        {
            Register(baseAddress,DosLvo.AllocDosObject,"AllocDosObject",(s,i)=>{
                if(s.D[1]==2&&i.Definition.CopyArgumentGate!.NormalKind is "mkdir-existing" or "mkdir-examine-fail") {
                    Require(s.D[2]==0&&i.Reads==1,"Destination FIB allocation order differs.");
                    var fib=Bus.Allocate(i,260,"DestinationFIB",true);i.CopyArgumentGateLayout!.DestinationFib=fib;Bus.Memory[fib+FileInfoBlock.ActualExtensionFlagsOffset]=255;return fib;
                }
                Require(s.D[1]==5&&s.D[2]==0&&i.Reads==0,"Explicit RDArgs allocation ABI differs.");
                if(i.Definition.CopyArgumentGate!.ParserAllocationFailure){i.IoError=103;return 0;}
                return i.CopyArgumentGateLayout!.RdArgs;
            });
            Register(baseAddress,DosLvo.FreeDosObject,"FreeDosObject",(s,i)=>{
                if(s.D[1]==2) {
                    Require(s.D[2]==i.CopyArgumentGateLayout!.DestinationFib&&i.CopyArgumentGateLayout.Unlocks==(i.Definition.CopyArgumentGate!.NormalKind=="mkdir-examine-fail"?0:1),"Destination FIB release/order differs.");
                    if(i.Definition.CopyArgumentGate!.DosVersion==40) Require(
                        Bus.Long(s.D[2]+FileInfoBlock.Size64Offset)==0&&Bus.Long(s.D[2]+FileInfoBlock.Size64Offset+4)==0x80000001&&
                        Bus.Long(s.D[2]+FileInfoBlock.NumBlocks64Offset)==0&&Bus.Long(s.D[2]+FileInfoBlock.NumBlocks64Offset+4)==0xf0000001,
                        "Classic destination sizes must be zero-extended even on examination failure.");
                    Bus.Release(i,s.D[2],"DestinationFIB");i.CopyArgumentGateLayout.DestinationFib=0;return 0;
                }
                Require(s.D[1]==5&&s.D[2]==i.CopyArgumentGateLayout!.RdArgs&&
                    i.FreeArgs==(i.Definition.CopyArgumentGate!.ParserError==0?1:0),"Explicit RDArgs release ABI/order differs.");
                Bus.Release(i,s.D[2],"CopyArgumentGateRDArgs");i.IoError=903;return 0;
            });
        }
        if (suite is CopyOptionSetupSuite or CopyDirectSuite)
        {
            Register(baseAddress,DosLvo.MatchFirst,"MatchFirst",(s,i)=>{
                Require(i.FreeArgs==0&&i.CopyArgumentGateLayout is not null,"Source classified after parser release.");
                if(explicitCopyParser && i.Definition.Name=="rejected-direct-clone-noreq")
                    Require(i.IoError!=(int)DOS.Error.TooManyArguments,"Admission error published before classification.");
                if(explicitCopyParser) Require(Bus.Long(i.Process+(uint)DosLayout.Process.WindowPointer)==
                    ((i.Definition.CopyArgumentGate!.Switches&(1u<<12))!=0?uint.MaxValue:i.Process+0x380),"Requester state differs during pre-admission classification.");
                Bus.Memory[s.D[2]+(uint)DosLayout.AnchorPath.Flags]=0;return 0;});
            Register(baseAddress,DosLvo.MatchEnd,"MatchEnd",(_,i)=>{Require(i.FreeArgs==0,"Classifier cleanup outlived parser.");return 0;});
        }
        if (suite == CopyDirectSuite) RegisterCopyDirectDos(baseAddress);
        if (suite == CopyParsedDeleteSuite) RegisterCopyParsedDeleteDos(baseAddress);
        if (suite == CopyParsedMakeDirectorySuite) RegisterCopyParsedMakeDirectoryDos(baseAddress);
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            const string template = "FROM/M,TO,PAT=PATTERN/K,BUF=BUFFER/K/N,ALL/S,DIRECT/S,CLONE/S," +
                "DATES/S,NOPRO/S,PROX/S,COM=COMMENT/S,QUIET/S,NOREQ/S,ERRWARN/S," +
                "MAKEDIR/S,MOVE/S,DELETE/S,HARD=HARDLINK/S,SOFT=SOFTLINK/S," +
                "FOLNK=FORCELINK/S,FODEL=FORCEDELETE/S,FOOVR=FORCEOVERWRITE/S," +
                "DONTOVR=DONTOVERWRITE/S,FORCE/S";
            var definition = invocation.Definition.CopyArgumentGate!;
            Require(Bus.CString(state.D[1]) == template && state.D[3] == (explicitCopyParser?invocation.CopyArgumentGateLayout!.RdArgs:0),
                "Copy ReadArgs template or RDArgs ABI differs.");
            Require(Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == (copyCommandRoot?4772u:96u),
                "Copy ReadArgs result storage must have 24 IPTR slots.");
            if(explicitCopyParser)
            {
                var help=Bus.CString(Bus.Long(state.D[3]+24));
                Require(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.Latin1.GetBytes(help)))=="B8F3E28CFE0D87CFF8D54364CD4AFFDDBFBD987DFDA5546B3447C2650157B299",
                    "Original Copy extended help differs.");
                for(uint slot=0;slot<24;slot++)Require(Bus.Long(state.D[2]+slot*4)==0,"Copy result slot not cleared.");
            }
            invocation.Reads++;
            var layout = invocation.CopyArgumentGateLayout!;
            if (definition.ParserError != 0)
            {
                if(!explicitCopyParser){Bus.Release(invocation, layout.RdArgs, "CopyArgumentGateRDArgs");
                invocation.CopyArgumentGateLayout = null;}
                invocation.IoError = definition.ParserError;
                return 0;
            }
            if (definition.DirectKind is not null) { Bus.Long(layout.RdArgs+480,1); Bus.Long(state.D[2]+12,layout.RdArgs+480); }
            if (definition.Inputs is not null) Bus.Long(state.D[2], layout.FromVector);
            if (definition.HasTarget) Bus.Long(state.D[2] + 4, layout.Target);
            if (definition.HasPattern) Bus.Long(state.D[2] + 8, layout.Pattern);
            for (var index = 4; index < 24; index++)
                if ((definition.Switches & (1u << index)) != 0)
                    Bus.Long(state.D[2] + (uint)(index * 4), uint.MaxValue);
            return layout.RdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            if(explicitCopyParser) Require(state.D[1]==invocation.CopyArgumentGateLayout!.RdArgs&&
                Bus.Long(invocation.Process+(uint)DosLayout.Process.WindowPointer)==invocation.Process+0x380,
                "FreeArgs object or requester restoration differs.");
            else Bus.Release(invocation, state.D[1], "CopyArgumentGateRDArgs");
            invocation.FreeArgs++;
            return 0;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }
}
