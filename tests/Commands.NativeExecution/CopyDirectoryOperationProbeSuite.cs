using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopyDirectoryOperationProbeCase(int Mode,uint Flags,bool Exists,bool Create,bool Enter,
    bool Rename,bool Link,bool Loop,int Outcome,int Result,uint Current,int Unlocks);
internal sealed class CopyDirectoryOperationNativeLayout(uint control)
{
    public uint Control { get; }=control;
    public int Locks,Creates,Unlocks,Renames,Links;
    public bool Contains(uint a,int n)=>a>=Control&&(ulong)a+(uint)n<=(ulong)Control+256;
}
internal sealed partial class ProbeFixture
{
    public const string CopyDirectoryOperationProbeSuite="copy-directory-operation-native-entry-vector-fixture";
    private List<object> RunCopyDirectoryOperationProbeCases()
    {
        ProbeCase[] cases=[
            DirectoryOperationCase("create",0,1,false,true,true,true,true,false,2,0,0x220,2),
            DirectoryOperationCase("enter",0,1,true,true,true,true,true,false,1,0,0x220,2),
            DirectoryOperationCase("create-failure",0,1,false,false,true,true,true,false,6,10,0x120,0),
            DirectoryOperationCase("relock-failure",0,1,false,true,false,true,true,false,7,10,0x120,1),
            DirectoryOperationCase("enter-failure",0,1,true,true,false,true,true,false,7,10,0x120,1),
            DirectoryOperationCase("rename",1,0,false,true,true,true,true,false,3,0,0x120,0),
            DirectoryOperationCase("rename-failure",1,0,false,true,true,false,true,false,8,5,0x120,0),
            DirectoryOperationCase("force-required",4,0,false,true,true,true,true,false,9,5,0x120,0),
            DirectoryOperationCase("hard-link",4,16,false,true,true,true,true,false,4,0,0x120,0),
            DirectoryOperationCase("link-failure",4,16,false,true,true,true,false,false,10,5,0x120,0),
            DirectoryOperationCase("display",0,0,false,true,true,true,true,false,0,0,0x120,0),
            DirectoryOperationCase("loop",0,1,false,true,true,true,true,true,5,10,0x120,0),
        ];
        var r=new List<object>();foreach(var c in cases)r.AddRange(Execute([c],false));
        r.AddRange(Execute([cases[0] with{Name="interleaved-create"},cases[3] with{Name="interleaved-failure"}],true));
        Bus.AssertImageUnchanged();return r;
    }
    private static ProbeCase DirectoryOperationCase(string n,int mode,uint flags,bool exists,bool create,bool enter,bool rename,bool link,bool loop,int outcome,int result,uint current,int unlocks)=>
        new(n,"",DOS.RETURN_OK,0,""){EntryLength=44,CopyDirectoryOperation=new(mode,flags,exists,create,enter,rename,link,loop,outcome,result,current,unlocks)};
    private void PrepareCopyDirectoryOperationProbe(Invocation i)
    {
        var c=i.Arguments;var p=i.Definition.CopyDirectoryOperation!;
        Bus.Memory.AsSpan((int)c,256).Clear();i.CopyDirectoryOperationLayout=new(c);
        Bus.Long(c,c+64);Bus.Long(c+4,c+96);Bus.Long(c+8,0x100);Bus.Long(c+12,0x120);
        Bus.Long(c+16,(uint)p.Mode);Bus.Long(c+20,p.Flags);Bus.Long(c+24,0x456);
        Encoding.Latin1.GetBytes("source\0").CopyTo(Bus.Memory.AsSpan((int)c+64));
        Encoding.Latin1.GetBytes("destination\0").CopyTo(Bus.Memory.AsSpan((int)c+96));
    }
    private void VerifyCopyDirectoryOperationProbe(Invocation i)
    {
        var p=i.Definition.CopyDirectoryOperation!;var l=i.CopyDirectoryOperationLayout!;
        Require(Bus.Long(l.Control+28)==p.Outcome&&Bus.Long(l.Control+32)==p.Result&&Bus.Long(l.Control+36)==p.Current&&
            Bus.Long(l.Control+40)==((p.Flags&1)!=0&&!p.Loop?0u:99u)&&l.Unlocks==p.Unlocks&&
            l.Creates==((p.Flags&1)!=0&&!p.Exists&&!p.Loop?1:0)&&
            l.Renames==(p.Mode==1?1:0)&&l.Links==(p.Mode==4&&(p.Flags&16)!=0?1:0),"Directory operation result or ownership differs.");
        i.CopyDirectoryOperationLayout=null;
    }
    private void RegisterCopyDirectoryOperationProbeDos(uint b)
    {
        Register(b,DosLvo.SameDevice,"SameDevice",(s,i)=>{Require(s.D[1]==0x456&&s.D[2]==0x120,"Loop device ABI differs.");return i.Definition.CopyDirectoryOperation!.Loop?1u:0;});
        Register(b,DosLvo.SameLock,"SameLock",(s,i)=>{Require(i.Definition.CopyDirectoryOperation!.Loop,"Unexpected SameLock.");return 0;});
        Register(b,DosLvo.Lock,"Lock",(s,i)=>{
            var p=i.Definition.CopyDirectoryOperation!;var l=i.CopyDirectoryOperationLayout!;
            Require(Bus.CString(s.D[1])=="destination"&&s.D[2]==unchecked((uint)DOS.LockMode.Shared),"Directory lock ABI differs.");
            return l.Locks++==0?(p.Exists?0x130u:0):(p.Enter?0x220u:0);
        });
        Register(b,-228,"AllocDosObject",(s,i)=>{Require(s.D[1]==(uint)DosObjectType.FileInfoBlock,"FIB type differs.");return Bus.Allocate(i,260,"DirectoryFIB",true);});
        Register(b,DosLvo.Examine,"Examine",(s,i)=>{Require(s.D[1]==0x130,"Examine lock differs.");Bus.Long(s.D[2]+FileInfoBlock.DirEntryTypeOffset,2);return 1;});
        Register(b,-234,"FreeDosObject",(s,i)=>{Bus.Release(i,s.D[2],"DirectoryFIB");return 0;});
        Register(b,DosLvo.UnLock,"UnLock",(s,i)=>{
            Require(s.D[1] is 0x130 or 0x210 or 0x120,"Released wrong directory lock.");i.CopyDirectoryOperationLayout!.Unlocks++;return 0;
        });
        Register(b,-120,"CreateDir",(s,i)=>{Require(Bus.CString(s.D[1])=="destination","CreateDir name differs.");i.CopyDirectoryOperationLayout!.Creates++;return i.Definition.CopyDirectoryOperation!.Create?0x210u:0;});
        Register(b,DosLvo.Rename,"Rename",(s,i)=>{Require(Bus.CString(s.D[1])=="source"&&Bus.CString(s.D[2])=="destination","Rename paths differ.");i.CopyDirectoryOperationLayout!.Renames++;return i.Definition.CopyDirectoryOperation!.Rename?1u:0;});
        Register(b,DosLvo.MakeLink,"MakeLink",(s,i)=>{Require(Bus.CString(s.D[1])=="destination"&&s.D[2]==0x456&&s.D[3]==0,"Hard link differs.");i.CopyDirectoryOperationLayout!.Links++;return i.Definition.CopyDirectoryOperation!.Link?1u:0;});
        Register(b,DosLvo.IoErr,"IoErr",(_,i)=>unchecked((uint)i.IoError));
        Register(b,DosLvo.SetIoErr,"SetIoErr",(s,i)=>{i.IoError=unchecked((int)s.D[1]);return 0;});
    }
}
