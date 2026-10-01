using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopyTraversalWorkProbeCase(int Entries, bool Routed = false, int Classification = 1, string Source = "SYS:#?", bool DirectoryTarget = false, int TargetPattern = 0, string? SingleFailure = null, int SourceCount = 1, bool FailFirst = false, bool StopOnError = false, bool CancelAfterFirst = false, bool Visible = false, bool Nested = false, int FailAt = -1, bool PartialWrite = false, bool CleanupDeleteFails = false, bool CancelDuringTransfer = false) { public bool HasTransferFailure => FailFirst || FailAt >= 0; public int FailureIndex => FailAt >= 0 ? FailAt : 0; public int ExpectedEntries => CancelAfterFirst ? 1 : HasTransferFailure && (StopOnError || CancelDuringTransfer) ? FailureIndex + 1 : Entries; public int ExpectedSources => CancelAfterFirst ? 1 : HasTransferFailure && (StopOnError || CancelDuringTransfer) ? FailureIndex + 1 : SourceCount; public string TargetName => TargetPattern==0?"RAM:":TargetPattern==1?"RAM:#?":"RAM:("; public bool Stream => Source.StartsWith("NET:"); }
internal sealed class CopyTraversalWorkNativeLayout(uint control)
{
    public uint Control { get; }=control;
    public uint Parser,Workspace;
    public Dictionary<int, byte[]> OutputFiles { get; } = new();
    public CopyRecursiveIoState? Recursive;
    public uint Anchor,Buffer; public int DestinationLocks,DestinationUnlocks,DestinationTests,Creates,TargetParses,PathParts;
    public int Nexts,Ends,Opens,Closes,Names,Parts,Unlocks,Completed,Classifies,ClassEnds,DeviceTests,Deletes,CancellationPolls,PreClassifications,PreClassEnds,NameFlushes;
    public bool Contains(uint a,int n)=>a>=Control&&(ulong)a+(uint)n<=(ulong)Control+8192;
}
internal sealed partial class ProbeFixture
{
    public const string CopySingleTargetSuite="copy-single-target-native-entry-vector-fixture";
    public const string CopyTargetDispatchSuite="copy-target-dispatch-native-entry-vector-fixture";
    public const string CopyDirectorySourcesSuite="copy-directory-sources-native-entry-vector-fixture";
    public const string CopySourceRoutingProbeSuite="copy-source-routing-native-entry-vector-fixture";
    public const string CopyTraversalWorkProbeSuite="copy-traversal-work-native-entry-vector-fixture";
    private List<object> RunCopyTraversalWorkProbeCases()
    {
        if(recursiveCommandRoot)
        {
            var test=DirectoryCase("recursive-siblings",2) with {EntryLength=null,Error=902};
            var reports=Execute([test],false).ToList();
            reports.AddRange(Execute([test with {Name="recursive-move",CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure="move"}}],false));
            reports.AddRange(Execute([test with {Name="recursive-clone",CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure="clone"}}],false));
            reports.AddRange(Execute([test with {Name="recursive-clone-metadata-failure",CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure="clone",FailFirst=true}}],false));
            reports.AddRange(Execute([test with {Name="recursive-delete",CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure="delete"}}],false));
            reports.AddRange(Execute([test with {Name="recursive-nested-move",CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure="move",Nested=true}}],false));
            reports.AddRange(Execute([test with {Name="recursive-nested-delete",CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure="delete",Nested=true}}],false));
            reports.AddRange(Execute([test with {Name="recursive-nested-clone",CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure="clone",Nested=true}}],false));
            reports.AddRange(Execute([test with {Name="recursive-literal",CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure="literal",Source="SYS:",Classification=0}}],false));
            foreach(var failure in new[]{"matchnext-error","matchnext-break","pending-error","pending-break","parent-zero","parent-error"})
                reports.AddRange(Execute([test with {Name="recursive-"+failure,Result=20,Output="SYS:#? - [DOS fault]\n",CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure=failure}}],false));
            reports.AddRange(Execute([test with {Name="recursive-pending-eof",CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure="pending-eof"}}],false));
            reports.AddRange(Execute([test with {Name="recursive-parent-eof",CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure="parent-eof"}}],false));
            foreach(var probe in new[]{"probe-other-error","probe-no-device","probe-not-link","probe-empty-link","probe-buffer-failure"})
                reports.AddRange(Execute([test with {Name="recursive-"+probe,CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure=probe}}],false));
            reports.AddRange(Execute([test with {Name="recursive-dangling",Result=5,CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure="probe-dangling"}}],false));
            reports.AddRange(Execute([test with {Name="recursive-dangling-errwarn",Result=10,CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure="probe-dangling",StopOnError=true}}],false));
            reports.AddRange(Execute([test with {Name="recursive-dangling-visible",Result=10,
                Output="Warning: Skipping dangling softlink first -> missing\n not read.: [DOS fault]\n",
                CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure="probe-dangling",StopOnError=true,Visible=true}}],false));
            reports.AddRange(Execute([test with {Name="recursive-dangling-visible-continue",Result=5,
                Output="Warning: Skipping dangling softlink first -> missing\n not read.: [DOS fault]\n        second (Dir)   [created]\n           child..copied.\n",
                CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure="probe-dangling",Visible=true}}],false));
            foreach(var option in new[]{"nopro","prox","nested"})reports.AddRange(Execute([test with {Name="recursive-"+option,CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure=option}}],false));
            reports.AddRange(Execute([test with {Name="recursive-interleaved-a"},test with {Name="recursive-interleaved-b"}],true));
            reports.AddRange(Execute([test with {Name="mixed-copy"},test with {Name="mixed-delete",CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure="delete"}}],true));
            reports.AddRange(Execute([test with {Name="mixed-move",CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure="move"}},test with {Name="mixed-clone",CopyTraversalWork=test.CopyTraversalWork! with {SingleFailure="clone"}}],true));
            Bus.AssertImageUnchanged();return reports;
        }
        if(suite==CopySingleTargetSuite)
        {
            var c=DirectoryCase("single",1) with {CopyTraversalWork=new(1,true,0,"SYS:file0",true)};
            var singleCases=new List<ProbeCase>{c};
            foreach(var scenario in new[]{"output","input","write","move","move-copy","hardlink","softlink"})
                singleCases.Add(c with {Name=scenario,CopyTraversalWork=c.CopyTraversalWork! with {SingleFailure=scenario}});
            if(singleCommandRoot) singleCases.Add(c with {Name="quoted-source",CopyTraversalWork=c.CopyTraversalWork! with {Source="SYS:'file0"}});
            if(singleCommandRoot) singleCases.Add(c with {Name="visible-output-failure",Output=" not opened for output: [DOS fault]\n",
                CopyTraversalWork=c.CopyTraversalWork! with {SingleFailure="output",Visible=true}});
            if(singleCommandRoot) foreach(var failure in new[]{"input","write","softlink"})
                singleCases.Add(c with {Name="visible-"+failure,Output=" not "+(failure=="softlink"?"linked.":"copied.")+": [DOS fault]\n",
                    CopyTraversalWork=c.CopyTraversalWork! with {SingleFailure=failure,Visible=true}});
            if(singleCommandRoot) singleCases=singleCases.Select(x=>x with {EntryLength=null,Error=902,
                Result=x.CopyTraversalWork!.SingleFailure is "output" or "input" or "write" or "softlink"?5:0}).ToList();
            var reports=new List<object>();foreach(var test in singleCases)reports.AddRange(Execute([test],false));
            reports.AddRange(Execute([singleCases[0] with {Name="interleaved-a"},singleCases[0] with {Name="interleaved-b"}],true));
            Bus.AssertImageUnchanged();return reports;
        }
        ProbeCase[] cases=[TraversalWorkCase("empty",0),TraversalWorkCase("one",1),TraversalWorkCase("two",2)];
        if (suite == CopySourceRoutingProbeSuite)
            cases = [SourceCase("empty",0,1,"SYS:#?"),SourceCase("wildcard-two",2,1,"SYS:#?"),
                SourceCase("literal",1,0,"SYS:file0"),SourceCase("classification-failure",1,-1,"SYS:file0"),
                SourceCase("relative",1,0,"file0"),SourceCase("stream",1,-1,"NET:file0")];
        if (suite == CopyDirectorySourcesSuite) cases=[DirectoryCase("empty",0),DirectoryCase("one",1),DirectoryCase("two",2)];
        if (suite == CopyTargetDispatchSuite) cases=[DirectoryCase("empty",0),DirectoryCase("one",1),DirectoryCase("two",2),new("wildcard-target","",0,0,""){EntryLength=68,CopyTraversalWork=new(0,true,1,"SYS:#?",true,1)},new("invalid-target","",0,0,""){EntryLength=68,CopyTraversalWork=new(0,true,1,"SYS:#?",true,-1)}];
        if(directoryCommandRoot) cases=[..cases,DirectoryCase("stream-source",1) with {CopyTraversalWork=new(1,true,-1,"NET:file0",true)}];
        if(directoryCommandRoot) cases=[..cases,DirectoryCase("two-source-patterns",2) with {CopyTraversalWork=new(2,true,1,"SYS:#?",true,SourceCount:2)}];
        if(directoryCommandRoot) foreach(var stop in new[]{false,true})
            cases=[..cases,DirectoryCase(stop?"first-failure-stop":"first-failure-continue",2) with {
                CopyTraversalWork=new(2,true,1,"SYS:#?",true,SourceCount:2,FailFirst:true,StopOnError:stop)}];
        if(directoryCommandRoot) foreach(var stop in new[]{false,true})
            cases=[..cases,DirectoryCase(stop?"middle-write-failure-stop":"middle-write-failure-continue",3) with {
                CopyTraversalWork=new(3,true,1,"SYS:#?",true,SourceCount:3,StopOnError:stop,FailAt:1)}];
        if(directoryCommandRoot) foreach(var stop in new[]{false,true})
            cases=[..cases,DirectoryCase(stop?"middle-partial-write-stop":"middle-partial-write-continue",3) with {
                CopyTraversalWork=new(3,true,1,"SYS:#?",true,SourceCount:3,StopOnError:stop,FailAt:1,PartialWrite:true)}];
        if(directoryCommandRoot) foreach(var stop in new[]{false,true})
            cases=[..cases,DirectoryCase(stop?"partial-cleanup-denied-stop":"partial-cleanup-denied-continue",3) with {
                CopyTraversalWork=new(3,true,1,"SYS:#?",true,SourceCount:3,StopOnError:stop,FailAt:1,PartialWrite:true,CleanupDeleteFails:true)}];
        if(directoryCommandRoot) foreach(var stop in new[]{false,true})
            cases=[..cases,DirectoryCase(stop?"visible-partial-write-stop":"visible-partial-write-continue",3) with {
                Output="        RAM: (Dir)   [created]\n   file0..copied.\n   file1.. not copied.: [DOS fault]\n"+(stop?"":"   file2..copied.\n"),
                CopyTraversalWork=new(3,true,1,"SYS:#?",true,SourceCount:3,StopOnError:stop,FailAt:1,PartialWrite:true,Visible:true)}];
        if(directoryCommandRoot) foreach(var stop in new[]{false,true})
            cases=[..cases,DirectoryCase(stop?"cancel-active-transfer-errwarn":"cancel-active-transfer",3) with {
                CopyTraversalWork=new(3,true,1,"SYS:#?",true,SourceCount:3,StopOnError:stop,FailAt:1,CancelDuringTransfer:true)}];
        if(directoryCommandRoot) foreach(var stop in new[]{false,true})
            cases=[..cases,DirectoryCase(stop?"cancel-between-errwarn":"cancel-between",2) with {
                CopyTraversalWork=new(2,true,1,"SYS:#?",true,SourceCount:2,StopOnError:stop,CancelAfterFirst:true)}];
        if(directoryCommandRoot) cases=[..cases,DirectoryCase("verbose-success",1) with {
            Output="        RAM: (Dir)   [created]\n   file0..copied.\n",CopyTraversalWork=new(1,true,1,"SYS:#?",true,Visible:true)}];
        if(directoryCommandRoot) cases=[..cases,DirectoryCase("verbose-empty",0) with {
            Output="        RAM: (Dir)   [created]\nNo file was processed.\n",CopyTraversalWork=new(0,true,1,"SYS:#?",true,Visible:true)}];
        if(directoryCommandRoot) cases=cases.Select(x=>x with {EntryLength=null,Error=902,Result=x.CopyTraversalWork!.HasTransferFailure||x.CopyTraversalWork.CancelAfterFirst?x.CopyTraversalWork.StopOnError?10:5:x.CopyTraversalWork.TargetPattern==0?0:x.CopyTraversalWork.TargetPattern==1?10:20}).ToArray();
        var r=new List<object>();foreach(var c in cases)r.AddRange(Execute([c],false));
        r.AddRange(Execute([cases[1] with{Name="interleaved-one"},cases[2] with{Name="interleaved-two"}],true));
        Bus.AssertImageUnchanged();return r;
    }
    private static ProbeCase DirectoryCase(string name,int entries)=>new(name,"",DOS.RETURN_OK,0,""){EntryLength=68,CopyTraversalWork=new(entries,true,1,"SYS:#?",true)};
    private static ProbeCase SourceCase(string n,int entries,int classification,string source)=>new(n,"",DOS.RETURN_OK,0,""){
        EntryLength=68,CopyTraversalWork=new(entries,true,classification,source)};
    private static ProbeCase TraversalWorkCase(string n,int entries)=>new(n,"",DOS.RETURN_OK,0,""){
        EntryLength=64,CopyTraversalWork=new(entries)};
    private void PrepareCopyTraversalWorkProbe(Invocation i)
    {
        var c=i.Arguments;Bus.Memory.AsSpan((int)c,8192).Clear();i.CopyTraversalWorkLayout=new(c);
        if(recursiveCommandRoot)i.CopyTraversalWorkLayout.Recursive=new(i.Definition.CopyTraversalWork!.SingleFailure=="nested"||i.Definition.CopyTraversalWork.Nested,i.Definition.CopyTraversalWork.SingleFailure=="literal",i.Definition.CopyTraversalWork.SingleFailure=="probe-dangling");
        Bus.Long(c,c+80);Bus.Long(c+4,c+1024);Bus.Long(c+8,c+256);Bus.Long(c+12,c+4096);Bus.Long(c+16,0x100);Bus.Long(c+24,256);Bus.Long(c+32,512);
        Bus.Long(c+64,c+6144);
        if(i.Definition.CopyTraversalWork!.DirectoryTarget){Bus.Long(c,c+700);Bus.Long(c+700,c+80);Bus.Long(c+28,c+160);Encoding.Latin1.GetBytes(i.Definition.CopyTraversalWork.TargetName+"\0").CopyTo(Bus.Memory.AsSpan((int)c+160));}
        if(suite==CopySingleTargetSuite)
        {
            Encoding.Latin1.GetBytes("RAM:file0\0").CopyTo(Bus.Memory.AsSpan((int)c+160));
            var scenario=i.Definition.CopyTraversalWork.SingleFailure;
            Bus.Long(c+20,scenario is "move" or "move-copy"?1u:scenario is "hardlink" or "softlink"?4u:0u);
            if(scenario=="softlink") Bus.Long(c+24,256u|16u|(1u<<20));
        }
        Encoding.Latin1.GetBytes(i.Definition.CopyTraversalWork!.Source+"\0").CopyTo(Bus.Memory.AsSpan((int)c+80));
    }
    private void VerifyCopyTraversalWorkProbe(Invocation i)
    {
        if(recursiveCommandRoot)
        {
            var recursiveLayout=i.CopyTraversalWorkLayout!;var r=Recursive(i);
            if(RecursiveHasLinkDevice(i.Definition.CopyTraversalWork!.SingleFailure))
                Require(r.FailedLinkProbes==1&&r.ProbeRestores==1&&r.DeviceProbes==1&&r.LinkReads==(i.Definition.CopyTraversalWork.SingleFailure=="probe-buffer-failure"?0:1)&&r.DeviceReleases==1&&
                    i.Allocations==(i.Definition.CopyTraversalWork.StopOnError?4:5),"ReadLink fallback did not release its device and temporary buffer.");
            if(i.Definition.CopyTraversalWork!.SingleFailure=="probe-dangling")
            {
                var stopped=i.Definition.CopyTraversalWork.StopOnError;
                r.FileSystem.VerifyDanglingSkipped(stopped);
                Require(r.DanglingWorkAttempts==1&&r.Handles.Count==0&&r.MetadataPaths.Count==(stopped?0:2)&&r.Dates==0&&r.Comments==0&&r.CompletedFiles==(stopped?0:1)&&
                    recursiveLayout.Ends==1&&recursiveLayout.Nexts==(stopped?2:4)&&i.Reads==1&&i.FreeArgs==1&&recursiveLayout.Parser==0&&
                    i.Allocations==i.FreeMem&&i.Events.Count(x=>x=="VPrintf")== (i.Definition.CopyTraversalWork.Visible?stopped?2:7:0)&&
                    recursiveLayout.NameFlushes==(i.Definition.CopyTraversalWork.Visible&&!stopped?2:0)&&
                    i.Events.Count(x=>x=="PrintFault")== (i.Definition.CopyTraversalWork.Visible?1:0)&&
                    recursiveLayout.Classifies==(i.Definition.CopyTraversalWork.Visible?2:1)&&recursiveLayout.ClassEnds==recursiveLayout.Classifies,
                    "Dangling-link result, verbosity or cleanup differs.");
                i.CopyTraversalWorkLayout=null;return;
            }
            if(i.Definition.CopyTraversalWork!.SingleFailure is "probe-other-error" or "probe-no-device")
                Require(r.FailedLinkProbes==1&&r.ProbeRestores==1&&r.DeviceProbes==(i.Definition.CopyTraversalWork.SingleFailure=="probe-no-device"?1:0)&&
                    !i.Events.Contains("ReadLink")&&!i.Events.Contains("FreeDeviceProc"),
                    "Unresolved soft-link probe changed descent or device ownership.");
            var pendingEof=i.Definition.CopyTraversalWork!.SingleFailure=="pending-eof";
            var pendingFailure=i.Definition.CopyTraversalWork!.SingleFailure is "pending-error" or "pending-break";
            var parentFailure=i.Definition.CopyTraversalWork!.SingleFailure is "parent-eof" or "parent-zero" or "parent-error";
            if(pendingFailure||pendingEof||parentFailure)r.FileSystem.VerifyStoppedInsideFirstDirectory(pendingEof||parentFailure);
            else if(i.Definition.CopyTraversalWork!.SingleFailure=="delete")r.FileSystem.VerifyDeleted();else r.FileSystem.VerifyComplete(i.Definition.CopyTraversalWork.SingleFailure=="move");
            if(i.Definition.CopyTraversalWork.SingleFailure is "matchnext-error" or "matchnext-break" or "pending-error" or "pending-break" or "parent-zero" or "parent-error")
                Require(i.Events.Count(x=>x=="PrintFault")==1&&i.Events[i.Events.IndexOf("PrintFault")+1]=="SetIoErr",
                    "QUIET matcher failure must print once and restore its error immediately.");
            if(parentFailure)
            {
                Require(r.Handles.Count==0&&r.Dates==0&&r.Comments==0&&r.MetadataPaths.Count==1&&r.CompletedFiles==1&&
                    recursiveLayout.Ends==1&&recursiveLayout.Nexts==3&&i.Reads==1&&i.FreeArgs==1&&recursiveLayout.Parser==0&&
                    i.Allocations==i.FreeMem&&i.Events.Count(x=>x=="Open")==2&&
                    i.Events.LastIndexOf("UnLock")<i.Events.IndexOf("FreeArgs"),
                    "Parent failure processed later entries, applied directory metadata or leaked state.");
                i.CopyTraversalWorkLayout=null;return;
            }
            if(pendingFailure||pendingEof)
            {
                Require(r.Handles.Count==0&&r.Dates==0&&r.Comments==0&&r.MetadataPaths.Count==(pendingEof?1:0)&&r.CompletedFiles==(pendingEof?1:0)&&
                    recursiveLayout.Ends==1&&recursiveLayout.Nexts==2&&i.Reads==1&&i.FreeArgs==1&&recursiveLayout.Parser==0&&
                    i.Allocations==i.FreeMem&&(pendingEof?i.Events.IndexOf("Open")>i.Events.LastIndexOf("MatchEnd"):!i.Events.Contains("Open"))&&
                    i.Events.LastIndexOf("UnLock")>i.Events.IndexOf("PrintFault")&&i.Events.LastIndexOf("UnLock")<i.Events.IndexOf("FreeArgs"),
                    "Pending failure performed file work or lost traversal/parser cleanup ordering.");
                i.CopyTraversalWorkLayout=null;return;
            }
            Require(r.Handles.Count==0&&r.Dates==(i.Definition.CopyTraversalWork!.SingleFailure=="clone"?(i.Definition.CopyTraversalWork!.Nested||i.Definition.CopyTraversalWork.SingleFailure=="nested"?5:4):0)&&r.Comments==r.Dates&&r.MetadataPaths.Count==(i.Definition.CopyTraversalWork!.SingleFailure=="delete"?2:i.Definition.CopyTraversalWork.SingleFailure=="nopro"?0:i.Definition.CopyTraversalWork!.Nested||i.Definition.CopyTraversalWork.SingleFailure=="nested"?5:4)&&r.CompletedFiles==2&&recursiveLayout.Ends==1&&recursiveLayout.Nexts==r.Trace.Length&&i.Reads==1&&i.FreeArgs==1&&recursiveLayout.Parser==0&&i.Allocations==i.FreeMem+(i.Definition.CopyTraversalWork.SingleFailure=="probe-buffer-failure"?1:0),"Recursive lifecycle incomplete.");
            i.CopyTraversalWorkLayout=null;return;
        }
        if(suite==CopySingleTargetSuite){VerifySingleTarget(i);return;}
        var p=i.Definition.CopyTraversalWork!;var n=p.ExpectedEntries;var l=i.CopyTraversalWorkLayout!;
        if(suite==CopyTargetDispatchSuite){Require(l.TargetParses==1&&l.PathParts==(p.TargetPattern==0?1:0),"Target parse/path dispatch differs.");if(p.TargetPattern!=0){Require((directoryCommandRoot||Bus.Long(l.Control+44)==(p.TargetPattern==1?10u:0)&&Bus.Long(l.Control+48)==20)&&l.DestinationLocks==0&&l.Classifies==0&&i.Allocations==(directoryCommandRoot?2:1)&&i.FreeMem==i.Allocations&&(!directoryCommandRoot||i.Reads==1&&i.FreeArgs==1&&l.Parser==0),"Rejected target performed work or lost failure.");i.CopyTraversalWorkLayout=null;return;}}
        Require((directoryCommandRoot||Bus.Long(l.Control+44)==0&&Bus.Long(l.Control+48)==0&&Bus.Long(l.Control+52)==(256u|(n>0?1u<<22:0)))&&
            l.Completed==n&&l.Nexts==(p.Stream?0:n)&&l.Ends==(p.Stream?0:p.ExpectedSources)&&l.Opens==n*2&&l.Closes==n*2&&l.Names==(n>0?p.ExpectedSources:0)&&l.Parts==n&&l.Unlocks==(p.Stream?0:n*2)&&
            i.Allocations==(p.Stream?1:n>0?2:1)+(suite==CopyTargetDispatchSuite?1:0)+(directoryCommandRoot?1:0)+(p.ExpectedSources-1)&&i.FreeMem==i.Allocations,"Combined traversal/worker lifecycle differs.");
        if(p.Visible)Require(l.PreClassifications==p.SourceCount&&l.PreClassEnds==p.SourceCount&&l.NameFlushes==1+n,"Verbose startup classification/flush lifecycle differs.");
        if(p.CancelAfterFirst)Require(l.CancellationPolls==2&&i.Events.LastIndexOf("SetSignal")>i.Events.IndexOf("FreeDosObject"),
            "Cancellation must stop the source loop and be finalized after parser cleanup.");
        if(p.CancelDuringTransfer)Require(l.CancellationPolls>0&&i.Events.Count(x=>x=="Read")==2&&i.Events.Count(x=>x=="Write")==2,"Cancellation must prevent further data transfer.");
        Require(l.Deletes==(p.HasTransferFailure?1:0),"Failed output deletion count differs.");
        Require(l.OutputFiles.Count==n-(p.HasTransferFailure&&!p.CleanupDeleteFails?1:0),"Unexpected surviving destination count.");
        for(var index=0;index<n;index++) {
            if(p.HasTransferFailure&&index==p.FailureIndex) {
                if(p.CleanupDeleteFails)Require(l.OutputFiles.TryGetValue(index,out var partial)&&partial.Length==3&&partial.All(x=>x==index+1),"Denied cleanup must retain the partial destination.");
                else Require(!l.OutputFiles.ContainsKey(index),"Partial destination survived failure.");
            }
            else Require(l.OutputFiles.TryGetValue(index,out var bytes)&&bytes.Length==8&&bytes.All(x=>x==index+1),"Successful destination contents changed.");
        }
        Require(Bus.CString(l.Control+80)==p.Source,"Source suffix was not restored.");
        Require(l.Classifies==(p.Routed?p.ExpectedSources:0)&&l.ClassEnds==(p.Routed&&p.Classification>=0?p.ExpectedSources:0)&&
            l.DeviceTests==(p.Routed&&p.Source.Contains(':')?p.ExpectedSources:0),"Source classifier/device lifecycle differs.");
        if(p.DirectoryTarget)Require(l.DestinationTests==1&&l.DestinationLocks==2&&l.Creates==1&&l.DestinationUnlocks==2,"Root destination lifecycle differs.");
        if(directoryCommandRoot) Require(i.Reads==1&&i.FreeArgs==1&&l.Parser==0&&
            i.Events.IndexOf("FreeArgs")>i.Events.LastIndexOf("UnLock")&&i.Events.IndexOf("FreeDosObject")<i.Events.LastIndexOf("FreeMem"),
            "Directory command parser must outlive target/traversal work.");
        i.CopyTraversalWorkLayout=null;
    }
    private void PutTraversalWorkEntry(uint anchor,int index)
    {
        var fib=anchor+(uint)DosLayout.AnchorPath.Info;Bus.Long(fib+FileInfoBlock.DirEntryTypeOffset,unchecked((uint)-3));
        Encoding.Latin1.GetBytes("file"+index+"\0").CopyTo(Bus.Memory.AsSpan((int)(fib+FileInfoBlock.FileNameOffset)));
        Encoding.Latin1.GetBytes("SYS:file"+index+"\0").CopyTo(Bus.Memory.AsSpan((int)(anchor+(uint)DosLayout.AnchorPath.PathBuffer)));
    }
    private void RegisterCopyTraversalWorkProbeExec()
    {
        Register(ExecBase,ExecLvo.SetSignal,"SetSignal",(s,i)=>{Require(s.D[0]==0&&s.D[1]==0,"Ctrl-C ABI differs.");
            if(directoryCommandRoot&&i.Definition.CopyTraversalWork!.CancelDuringTransfer) {
                var p=i.Definition.CopyTraversalWork;var l=i.CopyTraversalWorkLayout!;
                if(l.Completed>p.FailureIndex||l.OutputFiles.TryGetValue(p.FailureIndex,out var partial)&&partial.Length==8){l.CancellationPolls++;return 4096;}
            }
            if(directoryCommandRoot&&i.Definition.CopyTraversalWork!.CancelAfterFirst&&i.CopyTraversalWorkLayout!.Completed==1)
            {i.CopyTraversalWorkLayout.CancellationPolls++;return 4096;}
            return copyCommandRoot&&i.Definition.CopyArgumentGate!.CompletionBreak&&i.Events.Contains("FreeDosObject")?4096u:0;});
    }
    private void RegisterCopyTraversalWorkProbeDos(uint b)
    {
        if(recursiveCommandRoot){RegisterRecursiveCommandDos(b);return;}
        if(suite==CopySingleTargetSuite){RegisterSingleTarget(b);return;}
        if(directoryCommandRoot) {RegisterDirectoryCommandOutput(b);RegisterSingleCommandParser(b);Register(b,DosLvo.IoErr,"IoErr",(_,i)=>unchecked((uint)i.IoError));}
        if(suite==CopyTargetDispatchSuite){
            Register(b,DosLvo.ParsePattern,"ParsePattern",(s,i)=>{var l=i.CopyTraversalWorkLayout!;var p=i.Definition.CopyTraversalWork!;Require(Bus.CString(s.D[1])==p.TargetName&&s.D[3]==p.TargetName.Length*2+3&&l.DestinationTests==0,"Destination pattern ABI/order differs.");l.TargetParses++;return unchecked((uint)p.TargetPattern);});
            Register(b,DosLvo.PathPart,"PathPart",(s,i)=>{var l=i.CopyTraversalWorkLayout!;Require(Bus.CString(s.D[1])=="RAM:"&&l.TargetParses==1&&i.FreeMem==1,"PathPart preceded pattern cleanup.");l.PathParts++;return s.D[1]+4;});
        }
        Register(b,-120,"CreateDir",(s,i)=>{var l=i.CopyTraversalWorkLayout!;Require(i.Definition.CopyTraversalWork!.DirectoryTarget&&Bus.CString(s.D[1])=="RAM:"&&l.DestinationLocks==1,"Destination creation differs.");l.Creates++;return 0x222;});
        Register(b,DosLvo.IsFileSystem,"IsFileSystem",(s,i)=>{var l=i.CopyTraversalWorkLayout!;if(i.Definition.CopyTraversalWork!.DirectoryTarget&&Bus.CString(s.D[1])=="RAM:"){Require(l.Classifies==0,"Destination opened after source classification.");l.DestinationTests++;return 1;}Require(l.Classifies==l.DeviceTests+1&&i.Allocations==(suite==CopyTargetDispatchSuite?1:0)+(directoryCommandRoot?1:0)+l.DeviceTests+(l.DeviceTests>0?1:0)&&Bus.CString(s.D[1])==(i.Definition.CopyTraversalWork!.Stream?"NET:":"SYS:"),"Source device probe order/prefix differs.");l.DeviceTests++;return i.Definition.CopyTraversalWork!.Stream?0u:1;});
        Register(b,DosLvo.FilePart,"FilePart",(s,i)=>{var l=i.CopyTraversalWorkLayout!;var p=i.Definition.CopyTraversalWork!;
            Require(p.Stream&&Bus.CString(s.D[1])==p.Source&&Bus.CString(directoryCommandRoot?l.Workspace+392:l.Control+1024)==p.Source&&i.Allocations==(suite==CopyTargetDispatchSuite?1:0)+(directoryCommandRoot?1:0),"Direct source copy/FilePart routing differs.");return s.D[1]+4;});
        Register(b,DosLvo.MatchFirst,"MatchFirst",(s,i)=>{var l=i.CopyTraversalWorkLayout!;var p=i.Definition.CopyTraversalWork!;Require(Bus.CString(s.D[1])==(p.SourceCount>1&&(l.Ends>0||directoryCommandRoot&&p.Visible&&l.TargetParses==0&&l.PreClassifications>0)?"SYS:other#?":p.Source),"MatchFirst name differs.");
            if(directoryCommandRoot&&p.Visible&&l.TargetParses==0){l.PreClassifications++;Bus.Memory[s.D[2]+(uint)DosLayout.AnchorPath.Flags]=(byte)AnchorPathFlags.IsWild;return 0;}
            if(p.Routed&&s.D[2]==(directoryCommandRoot?l.Workspace+108:l.Control+6144)){Require(l.Classifies++==l.DeviceTests&&l.Completed==l.DeviceTests&&i.Allocations==(suite==CopyTargetDispatchSuite?1:0)+(directoryCommandRoot?1:0)+l.DeviceTests+(l.DeviceTests>0?1:0),"Classifier order differs.");
                Require(Bus.Long(s.D[2]+(uint)DosLayout.AnchorPath.BreakBits)==0&&Bus.Memory[s.D[2]+(uint)DosLayout.AnchorPath.Flags]==(byte)AnchorPathFlags.DoWild,"Classifier fields differ.");
                if(p.Classification<0)return 205;
                Bus.Memory[s.D[2]+(uint)DosLayout.AnchorPath.Flags]=(byte)(p.Classification==1?AnchorPathFlags.IsWild:0);return 0;}
            Require(!p.Routed||(l.Classifies==l.Ends+1&&l.DeviceTests==(p.Source.Contains(':')?l.Classifies:0)),"Traversal preceded source routing.");l.Anchor=s.D[2];if(i.Definition.CopyTraversalWork!.Entries==0)return 232;PutTraversalWorkEntry(l.Anchor,p.SourceCount>1?l.Completed:0);return 0;});
        Register(b,DosLvo.MatchNext,"MatchNext",(s,i)=>{var l=i.CopyTraversalWorkLayout!;Require(s.D[1]==l.Anchor&&l.Completed==l.Nexts,"Work must consume previous snapshot before matcher advances.");l.Nexts++;if(i.Definition.CopyTraversalWork!.SourceCount>1||l.Nexts==i.Definition.CopyTraversalWork.Entries)return 232;PutTraversalWorkEntry(l.Anchor,l.Nexts);return 0;});
        Register(b,DosLvo.MatchEnd,"MatchEnd",(s,i)=>{var l=i.CopyTraversalWorkLayout!;if(directoryCommandRoot&&i.Definition.CopyTraversalWork!.Visible&&l.TargetParses==0){l.PreClassEnds++;return 0;}if(i.Definition.CopyTraversalWork!.Routed&&s.D[1]==(directoryCommandRoot?l.Workspace+108:l.Control+6144)){l.ClassEnds++;return 0;}Require(s.D[1]==l.Anchor,"MatchEnd differs.");l.Ends++;return 0;});
        Register(b,DosLvo.NameFromLock,"NameFromLock",(s,i)=>{var l=i.CopyTraversalWorkLayout!;Require(s.D[1]==0x100&&s.D[2]==(directoryCommandRoot?l.Workspace+2708:l.Control+4096)&&s.D[3]==2048,"Destination cache naming differs.");l.Names++;Encoding.Latin1.GetBytes("RAM:\0").CopyTo(Bus.Memory.AsSpan((int)s.D[2]));return 1;});
        Register(b,DosLvo.AddPart,"AddPart",(s,i)=>{var l=i.CopyTraversalWorkLayout!;Require(Bus.CString(s.D[1])=="RAM:"&&Bus.CString(s.D[2])=="file"+l.Completed,"Cached destination or pending filename differs.");l.Parts++;Encoding.Latin1.GetBytes("RAM:file"+l.Completed+"\0").CopyTo(Bus.Memory.AsSpan((int)s.D[1]));return 1;});
        Register(b,DosLvo.Lock,"Lock",(s,i)=>{var l=i.CopyTraversalWorkLayout!;var name=Bus.CString(s.D[1]);if(i.Definition.CopyTraversalWork!.DirectoryTarget&&name=="RAM:"){l.DestinationLocks++;return l.DestinationLocks==1?0u:0x100u;}Require(name=="SYS:file"+l.Completed||name=="RAM:file"+l.Completed,"Lock selected wrong snapshot.");return name.StartsWith("SYS:")?0x456u:0;});
        Register(b,DosLvo.ParentDir,"ParentDir",(s,i)=>{Require(s.D[1]==0x456,"Source parent differs.");return 0x123;});
        Register(b,DosLvo.UnLock,"UnLock",(s,i)=>{var l=i.CopyTraversalWorkLayout!;if(i.Definition.CopyTraversalWork!.DirectoryTarget&&(s.D[1]==0x100||s.D[1]==0x222)){Require(s.D[1]==(l.DestinationUnlocks==0?0x222u:0x100u)&& (l.DestinationUnlocks==0||l.Completed==i.Definition.CopyTraversalWork.ExpectedEntries),"Root released before pending work.");l.DestinationUnlocks++;return 0;}Require(s.D[1]==(l.Unlocks%2==0?0x123u:0x456u),"Source ownership differs.");l.Unlocks++;return 0;});
        Register(b,DosLvo.Open,"Open",(s,i)=>{var l=i.CopyTraversalWorkLayout!;var output=l.Opens%2==0;Require(Bus.CString(s.D[1])==(output?"RAM:file":i.Definition.CopyTraversalWork!.Stream?"NET:file":"SYS:file")+l.Completed&&
            (i.Definition.CopyTraversalWork!.Stream?l.Ends==0:(i.Definition.CopyTraversalWork.SourceCount>1?l.Ends==l.Completed+1:l.Completed==i.Definition.CopyTraversalWork.Entries-1?l.Ends==1:l.Ends==0)),"Deferred/final file open differs.");l.Opens++;return output?0x140u:0x130;});
        Register(b,DosLvo.ExamineFH,"ExamineFH",(s,i)=>{Require(s.D[1]==0x130&&(i.CopyTraversalWorkLayout!.Buffer==0||i.CopyTraversalWorkLayout.Buffer==s.D[2]),"Examine input or transfer cache reuse differs.");i.CopyTraversalWorkLayout!.Buffer=s.D[2];Bus.Long(s.D[2]+FileInfoBlock.SizeOffset,i.Definition.CopyTraversalWork!.CancelDuringTransfer&&i.CopyTraversalWorkLayout.Completed==i.Definition.CopyTraversalWork.FailureIndex?16u:8u);return 1;});
        Register(b,DosLvo.Read,"Read",(s,i)=>{var l=i.CopyTraversalWorkLayout!;Require(s.D[1]==0x130&&s.D[2]==l.Buffer&&s.D[3]==512,"Cached transfer buffer differs.");Bus.Memory.AsSpan((int)s.D[2],8).Fill((byte)(l.Completed+1));return 8;});
        Register(b,DosLvo.Write,"Write",(s,i)=>{var l=i.CopyTraversalWorkLayout!;Require(s.D[1]==0x140&&s.D[2]==l.Buffer&&s.D[3]==8&&Bus.Memory.AsSpan((int)s.D[2],8).ToArray().All(x=>x==l.Completed+1),"Copied payload differs.");var p=i.Definition.CopyTraversalWork!;var failed=p.HasTransferFailure&&!p.CancelDuringTransfer&&l.Completed==p.FailureIndex;var accepted=failed?(p.PartialWrite?3:0):8;Require(!l.OutputFiles.ContainsKey(l.Completed),"Unexpected repeated write after a short result.");l.OutputFiles.Add(l.Completed,Bus.Memory.AsSpan((int)s.D[2],accepted).ToArray());if(failed)i.IoError=221;return (uint)accepted;});
        Register(b,DosLvo.Close,"Close",(s,i)=>{var l=i.CopyTraversalWorkLayout!;Require(s.D[1]==(l.Closes%2==0?0x140u:0x130u),"Close order differs.");if(++l.Closes%2==0)l.Completed++;return 1;});
        Register(b,DosLvo.DeleteFile,"DeleteFile",(s,i)=>{
            var l=i.CopyTraversalWorkLayout!;
            Require(i.Definition.CopyTraversalWork!.HasTransferFailure&&Bus.CString(s.D[1])=="RAM:file"+i.Definition.CopyTraversalWork.FailureIndex&&l.Closes==2*(i.Definition.CopyTraversalWork.FailureIndex+1)&&l.Completed==i.Definition.CopyTraversalWork.FailureIndex+1&&i.FreeArgs==0,
                "Failed output must be deleted after closes before parser release.");
            var p=i.Definition.CopyTraversalWork!;Require(l.OutputFiles.TryGetValue(p.FailureIndex,out var partial)&&partial.Length==(p.CancelDuringTransfer?8:p.PartialWrite?3:0)&&partial.All(x=>x==p.FailureIndex+1),"Failed destination contents differ.");l.Deletes++;
            if(p.CleanupDeleteFails){i.IoError=222;return 0;}
            Require(l.OutputFiles.Remove(p.FailureIndex),"Failed destination removal differs.");return 1;
        });
        Register(b,DosLvo.SetIoErr,"SetIoErr",(s,i)=>{if(i.Definition.CopyTraversalWork!.CleanupDeleteFails&&i.IoError==222)Require(s.D[1]==221,"Cleanup failure replaced the saved transfer error.");i.IoError=unchecked((int)s.D[1]);return 0;});
    }
}
