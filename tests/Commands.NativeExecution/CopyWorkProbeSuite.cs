using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopyWorkProbeCase(string Kind,uint Flags,int Mode,int Type,int Result,
    bool Done,int Deletes,int Opens,int Unlocks,bool Extended=false,bool ExamineFailure=false,bool FibAllocationFailure=false,bool PosixDate=false);
internal sealed class CopyWorkNativeLayout(uint control)
{
    public uint Control { get; }=control;
    public uint DestinationFib; public int DestinationExaminations, FibAllocations; public int Dates, Comments;
    public int Deletes,Opens,Closes,Unlocks,Protections,Reads,Parents,Renames,Links;
    public bool Contains(uint a,int n)=>a>=Control&&(ulong)a+(uint)n<=(ulong)Control+4096;
}
internal sealed partial class ProbeFixture
{
    public const string CopyWorkProbeSuite="copy-work-native-entry-vector-fixture";
    private List<object> RunCopyWorkProbeCases()
    {
        ProbeCase[] cases=[
            WorkCase("copy",256,0,-3,0,true,0,2,2),
            WorkCase("copy-read-failure",256,0,-3,5,true,1,2,2),
            WorkCase("source-lock-failure",256,2,-3,5,false,0,0,0),
            WorkCase("parent-failure",256,2,-3,10,false,0,0,1),
            WorkCase("delete",256,2,-3,0,true,1,0,2),
            WorkCase("delete-failure",256,2,-3,5,true,1,0,2),
            WorkCase("force-delete",256|32,2,-3,0,true,1,0,2),
            WorkCase("directory-first",256|1,2,2,0,true,0,0,2),
            WorkCase("directory-second",256|1|(1u<<23),2,2,0,true,1,0,2),
            WorkCase("move-rename",256,1,-3,0,true,0,0,2),
            WorkCase("move-fallback",256,1,-3,0,true,1,2,2),
            WorkCase("hard-link",256,4,-3,0,true,0,0,2),
            WorkCase("soft-file-rejected",256|(1u<<20),4,-3,5,true,0,0,2),
            WorkCase("direct-output",256|(1u<<25),0,-3,0,true,0,2,0),
        ];
        cases=[..cases,cases[0] with {Name="copy-extended",CopyWork=cases[0].CopyWork! with {Extended=true}}];
        cases=[..cases,cases[^1] with {Name="extended-scratch-allocation-failure",AllocationFailure=true,
            Result=DOS.RETURN_FAIL,Error=(int)DOS.Error.NoFreeStore}];
        var existing=WorkCase("existing-extended",256|128,0,-3,0,true,0,0,3);
        cases=[..cases,existing with {CopyWork=existing.CopyWork! with {Extended=true}}];
        cases=[..cases,cases[^1] with {Name="existing-extended-examine-failure",CopyWork=cases[^1].CopyWork! with {ExamineFailure=true}}];
        cases=[..cases,cases[^1] with {Name="existing-extended-fib-allocation-failure",CopyWork=cases[^1].CopyWork! with {ExamineFailure=false,FibAllocationFailure=true}}];
        foreach(var baseline in cases.Where(c=>c.CopyWork!.Kind=="existing-extended").ToArray())
            cases=[..cases,baseline with {Name=baseline.Name+"-posix",CopyWork=baseline.CopyWork! with {PosixDate=true}}];
        var r=new List<object>();foreach(var c in cases)r.AddRange(Execute([c],false));
        r.AddRange(Execute([cases[0] with{Name="interleaved-copy"},cases[6] with{Name="interleaved-delete"}],true));
        Bus.AssertImageUnchanged();return r;
    }
    private static ProbeCase WorkCase(string n,uint flags,int mode,int type,int result,bool done,int deletes,int opens,int unlocks)=>
        new(n,"",DOS.RETURN_OK,0,""){EntryLength=64,CopyWork=new(n,flags,mode,type,result,done,deletes,opens,unlocks)};
    private void PrepareCopyWorkProbe(Invocation i)
    {
        var c=i.Arguments;var p=i.Definition.CopyWork!;Bus.Memory.AsSpan((int)c,4096).Clear();i.CopyWorkLayout=new(c);
        Bus.Long(c,c+64);Bus.Long(c+4,c+128);Bus.Long(c+8,c+256);Bus.Long(c+12,c+1024);
        Bus.Long(c+16,(uint)p.Mode);Bus.Long(c+20,p.Flags);Bus.Long(c+24,0x100);Bus.Long(c+28,0x100);Bus.Long(c+32,512);Bus.Long(c+40,p.Extended?1u:0);Bus.Long(c+36,p.Kind=="existing-extended"?25u:0);
        Bus.Memory[c+256+FileInfoBlock.ActualExtensionFlagsOffset]=p.PosixDate?(byte)FileInfoExtensionFlags.PosixDate:(byte)0;
        Bus.Long(c+256+FileInfoBlock.DateDaysOffset,123);
        Encoding.Latin1.GetBytes("source comment\0").CopyTo(Bus.Memory.AsSpan((int)(c+256+FileInfoBlock.CommentOffset)));
        Encoding.Latin1.GetBytes("entry\0").CopyTo(Bus.Memory.AsSpan((int)c+64));
        Encoding.Latin1.GetBytes("SYS:source\0").CopyTo(Bus.Memory.AsSpan((int)c+128));
        Encoding.Latin1.GetBytes("RAM:entry\0").CopyTo(Bus.Memory.AsSpan((int)c+1024));
        Bus.Long(c+256+FileInfoBlock.DirEntryTypeOffset,unchecked((uint)p.Type));
        Encoding.Latin1.GetBytes("entry\0").CopyTo(Bus.Memory.AsSpan((int)(c+256+FileInfoBlock.FileNameOffset)));
    }
    private void VerifyCopyWorkProbe(Invocation i)
    {
        var p=i.Definition.CopyWork!;var l=i.CopyWorkLayout!;
        if(i.Definition.AllocationFailure)
        {
            Require(p.Extended&&i.AllocationRequests.SequenceEqual(new uint[]{16})&&i.FreeMem==0&&
                l.Deletes==0&&l.Opens==0&&l.Closes==0&&l.Unlocks==0&&l.Parents==0&&l.Reads==0&&
                Bus.Long(l.Control+44)==0&&Bus.Long(l.Control+48)==0&&Bus.Long(l.Control+52)==0,
                "Failed scratch allocation must exit before worker effects or result writes.");
            i.CopyWorkLayout=null;return;
        }
        Require(Bus.Long(l.Control+44)==p.Result&&Bus.Long(l.Control+48)==0&&
            Bus.Long(l.Control+52)==(p.Flags|(p.Done?1u<<22:0))&&l.Deletes==p.Deletes&&l.Opens==p.Opens&&l.Unlocks==p.Unlocks&&
            l.Protections==((p.Flags&32)!=0||p.Kind=="existing-extended"?1:0)&&l.Closes==p.Opens&&i.FreeMem==(p.Opens>0?1:0)+(p.Extended?1:0),
            $"{i.Definition.Name}: combined worker state or ownership differs.");
        Require(l.Renames==(p.Mode==1?1:0)&&l.Links==(p.Kind=="hard-link"?1:0),"Worker MOVE/LINK selection differs.");
        Require(l.DestinationFib==0&&l.FibAllocations==(p.Kind=="existing-extended"?1:0)&&l.DestinationExaminations==(p.Kind=="existing-extended"&&!p.FibAllocationFailure?1:0),"Destination FIB lifecycle differs.");
        Require(l.Dates==(p.Kind=="existing-extended"?1:0)&&l.Comments==l.Dates,"Quiet metadata tail is incomplete.");
        i.CopyWorkLayout=null;
    }
    private void RegisterCopyWorkProbeExec()
    {
        Register(ExecBase,ExecLvo.SetSignal,"SetSignal",(s,i)=>{Require(s.D[0]==0&&s.D[1]==0,"Signal ABI differs.");return 0;});
    }
    private void RegisterCopyWorkProbeDos(uint b)
    {
        Register(b,DosLvo.AllocDosObject,"AllocDosObject",(s,i)=>{
            Require(i.Definition.CopyWork!.Kind=="existing-extended"&&s.D[1]==2&&s.D[2]==0,"Destination FIB allocation differs.");
            i.CopyWorkLayout!.FibAllocations++;
            if(i.Definition.CopyWork!.FibAllocationFailure){i.IoError=103;return 0;}
            return i.CopyWorkLayout!.DestinationFib=Bus.Allocate(i,260,"WorkerFIB",true);
        });
        Register(b,DosLvo.Examine64,"Examine64",(s,i)=>{
            var l=i.CopyWorkLayout!;
            Require(s.D[1]==0x789&&s.D[2]==l.DestinationFib&&s.D[3]!=0&&i.AllocationRequests.SequenceEqual(new uint[]{16}),"Destination examination storage differs.");
            Require(Bus.Long(s.D[3])==0x80000e11&&Bus.Long(s.D[3]+4)==1&&Bus.Long(s.D[3]+8)==0,"Destination tags differ.");
            Bus.Long(s.D[2]+FileInfoBlock.DirEntryTypeOffset,unchecked((uint)-3));l.DestinationExaminations++;return i.Definition.CopyWork!.ExamineFailure?0u:1u;
        });
        Register(b,DosLvo.FreeDosObject,"FreeDosObject",(s,i)=>{
            var l=i.CopyWorkLayout!;Require(s.D[1]==2&&s.D[2]==l.DestinationFib&&l.Unlocks==(i.Definition.CopyWork!.ExamineFailure?1:2),"Destination FIB release order differs.");
            Bus.Release(i,s.D[2],"WorkerFIB",260);l.DestinationFib=0;return 0;
        });
        Register(b,DosLvo.NameFromLock,"NameFromLock",(s,i)=>{Require(s.D[1]==0x100&&s.D[3]==2048,"Destination naming differs.");Encoding.Latin1.GetBytes("RAM:\0").CopyTo(Bus.Memory.AsSpan((int)s.D[2]));return 1;});
        Register(b,DosLvo.AddPart,"AddPart",(s,i)=>{Require(Bus.CString(s.D[1])=="RAM:"&&Bus.CString(s.D[2])=="entry","Destination append differs.");Encoding.Latin1.GetBytes("RAM:entry\0").CopyTo(Bus.Memory.AsSpan((int)s.D[1]));return 1;});
        Register(b,DosLvo.Lock,"Lock",(s,i)=>{
            var name=Bus.CString(s.D[1]);Require(name is "SYS:source" or "RAM:entry","Worker lock path differs.");
            if(name=="RAM:entry"&&i.Definition.CopyWork!.Kind=="existing-extended")return 0x789;
            return name=="RAM:entry"||i.Definition.CopyWork!.Kind=="source-lock-failure"?0u:0x456;
        });
        Register(b,DosLvo.ParentDir,"ParentDir",(s,i)=>{Require(s.D[1]==0x456,"Source parent lock differs.");i.CopyWorkLayout!.Parents++;return i.Definition.CopyWork!.Kind=="parent-failure"?0u:0x123;});
        Register(b,DosLvo.UnLock,"UnLock",(s,i)=>{var l=i.CopyWorkLayout!;Require(s.D[1]==(i.Definition.CopyWork!.Kind=="existing-extended"&&l.Unlocks==1?0x789u:l.Unlocks==0&&i.Definition.CopyWork!.Kind!="parent-failure"?0x123u:0x456u),"Worker source/parent unlock order differs.");l.Unlocks++;return 0;});
        Register(b,DosLvo.Open,"Open",(s,i)=>{
            var l=i.CopyWorkLayout!;var direct=(i.Definition.CopyWork!.Flags&(1u<<25))!=0;
            Require(Bus.CString(s.D[1])==(l.Opens==0?"RAM:entry":"SYS:source")&&l.Unlocks==(direct?0:l.Opens==0?1:2),"Worker open/unlock order differs.");return l.Opens++==0?0x140u:0x130;
        });
        Register(b,DosLvo.Rename,"Rename",(s,i)=>{Require(Bus.CString(s.D[1])=="SYS:source"&&Bus.CString(s.D[2])=="RAM:entry","Worker rename paths differ.");i.CopyWorkLayout!.Renames++;return i.Definition.CopyWork!.Kind=="move-rename"?1u:0;});
        Register(b,DosLvo.MakeLink,"MakeLink",(s,i)=>{Require(Bus.CString(s.D[1])=="RAM:entry"&&s.D[2]==0x456&&s.D[3]==0,"Worker hard-link target differs.");i.CopyWorkLayout!.Links++;return 1;});
        Register(b,DosLvo.ExamineFH,"ExamineFH",(s,i)=>{Require(!i.Definition.CopyWork!.Extended,"Classic examination selected in extended mode.");Bus.Long(s.D[2]+FileInfoBlock.SizeOffset,8);return 1;});
        Register(b,DosLvo.ExamineFH64,"ExamineFH64",(s,i)=>{
            Require(i.Definition.CopyWork!.Extended&&s.D[1]==0x130&&i.AllocationRequests.SequenceEqual(new uint[]{16,512}),"Extended worker allocation/handle differs.");
            Require(Bus.Long(s.D[3])==0x80000e11&&Bus.Long(s.D[3]+4)==1&&Bus.Long(s.D[3]+8)==0,"Extended worker tags differ.");
            Bus.Long(s.D[2]+FileInfoBlock.Size64Offset,0);Bus.Long(s.D[2]+FileInfoBlock.Size64Offset+4,8);return 1;
        });
        Register(b,DosLvo.Read,"Read",(s,i)=>{Require(s.D[1]==0x130&&s.D[3]==512,"Worker read differs.");i.CopyWorkLayout!.Reads++;return i.Definition.CopyWork!.Kind=="copy-read-failure"?unchecked((uint)-1):8;});
        Register(b,DosLvo.Write,"Write",(s,i)=>{Require(s.D[1]==0x140&&s.D[3]==8,"Worker write differs.");return 8;});
        Register(b,DosLvo.Close,"Close",(s,i)=>{var l=i.CopyWorkLayout!;Require(s.D[1]==(l.Closes==0?0x140u:0x130u),"Worker close differs.");l.Closes++;return 1;});
        Register(b,DosLvo.SetFileDate,"SetFileDate",(s,i)=>{
            var l=i.CopyWorkLayout!;Require(!i.Definition.CopyWork!.PosixDate&&i.Definition.CopyWork.Kind=="existing-extended"&&Bus.CString(s.D[1])=="RAM:entry"&&s.D[2]==l.Control+256+FileInfoBlock.DateDaysOffset&&Bus.Long(s.D[2])==123&&l.Protections==1&&l.Comments==0,"Quiet date metadata/order differs.");l.Dates++;return 0;
        });
        Register(b,DosLvo.SetFilePosixDate,"SetFilePosixDate",(s,i)=>{
            var l=i.CopyWorkLayout!;Require(i.Definition.CopyWork!.PosixDate&&Bus.CString(s.D[1])=="RAM:entry"&&s.D[2]==l.Control+256+FileInfoBlock.DateDaysOffset&&s.D[3]==0&&Bus.Long(s.D[2])==123&&l.Protections==1&&l.Dates==0&&l.Comments==0,"POSIX date metadata/order differs.");l.Dates++;return 0;
        });
        Register(b,DosLvo.SetComment,"SetComment",(s,i)=>{
            var l=i.CopyWorkLayout!;Require(Bus.CString(s.D[1])=="RAM:entry"&&Bus.CString(s.D[2])=="source comment"&&l.Dates==1,"Quiet comment metadata/order differs.");l.Comments++;return 0;
        });
        Register(b,DosLvo.SetProtection,"SetProtection",(s,i)=>{Require(Bus.CString(s.D[1])==(i.Definition.CopyWork!.Kind=="existing-extended"?"RAM:entry":"SYS:source")&&s.D[2]==0,"Worker protection target differs.");i.CopyWorkLayout!.Protections++;return 1;});
        Register(b,DosLvo.DeleteFile,"DeleteFile",(s,i)=>{var p=i.Definition.CopyWork!;Require(Bus.CString(s.D[1])==(p.Mode==0?"RAM:entry":"SYS:source")&&i.CopyWorkLayout!.Unlocks==2,"Worker deletion target/order differs.");i.CopyWorkLayout.Deletes++;return p.Kind=="delete-failure"?0u:1;});
        Register(b,DosLvo.IoErr,"IoErr",(_,i)=>unchecked((uint)i.IoError));
        Register(b,DosLvo.SetIoErr,"SetIoErr",(s,i)=>{i.IoError=unchecked((int)s.D[1]);return 0;});
    }
}
