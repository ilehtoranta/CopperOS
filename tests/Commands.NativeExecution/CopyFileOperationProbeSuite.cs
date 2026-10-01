using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopyFileOperationProbeCase(bool Out,bool In,bool Transfer,
    bool Move,bool Force,bool Delete,bool Release);
internal sealed class CopyFileOperationNativeLayout(uint control)
{
    public uint Control { get; }=control;
    public int Opens,Closes,Unlocks,Deletes,Protections,Reads;
    public bool Contains(uint a,int n)=>a>=Control&&(ulong)a+(uint)n<=(ulong)Control+256;
}
internal sealed partial class ProbeFixture
{
    public const string CopyFileOperationProbeSuite="copy-file-operation-native-entry-vector-fixture";
    private List<object> RunCopyFileOperationProbeCases()
    {
        ProbeCase[] cases=[
            OperationCase("copy",true,true,true,false,false,true,true),
            OperationCase("destination-failure",false,true,true,false,false,true,true),
            OperationCase("source-failure",true,false,true,false,false,true,true),
            OperationCase("transfer-failure",true,true,false,false,false,true,true),
            OperationCase("move",true,true,true,true,false,true,true),
            OperationCase("force-move-failure",true,true,true,true,true,false,true),
            OperationCase("direct",true,true,true,false,false,true,false),
        ];
        var r=new List<object>();foreach(var c in cases)r.AddRange(Execute([c],false));
        r.AddRange(Execute([cases[0] with{Name="interleaved-copy"},cases[5] with{Name="interleaved-move"}],true));
        Bus.AssertImageUnchanged();return r;
    }
    private static ProbeCase OperationCase(string n,bool o,bool input,bool transfer,bool move,bool force,bool delete,bool release)=>
        new(n,"",DOS.RETURN_OK,0,""){EntryLength=36,CopyFileOperation=new(o,input,transfer,move,force,delete,release)};
    private void PrepareCopyFileOperationProbe(Invocation i)
    {
        var c=i.Arguments;var p=i.Definition.CopyFileOperation!;
        Bus.Memory.AsSpan((int)c,256).Clear();i.CopyFileOperationLayout=new(c);
        Bus.Long(c,c+64);Bus.Long(c+4,c+96);Bus.Long(c+8,p.Move?1u:0);Bus.Long(c+12,p.Force?1u:0);
        Bus.Long(c+16,p.Release?0x456u:0);Bus.Long(c+20,p.Release?1u:0);
        Encoding.Latin1.GetBytes("source\0").CopyTo(Bus.Memory.AsSpan((int)c+64));
        Encoding.Latin1.GetBytes("destination\0").CopyTo(Bus.Memory.AsSpan((int)c+96));
    }
    private void VerifyCopyFileOperationProbe(Invocation i)
    {
        var p=i.Definition.CopyFileOperation!;var l=i.CopyFileOperationLayout!;
        var copied=p.Out&&p.In&&p.Transfer;
        var deleted=p.Out&&(!p.In||!p.Transfer||p.Move);
        Require(Bus.Long(l.Control+24)==(copied&&(!p.Move||p.Delete)?1u:0)&&
            Bus.Long(l.Control+28)==(p.Out?1u:0)&&Bus.Long(l.Control+32)==(!p.Out&&p.Release?0x456u:0)&&
            l.Opens==(p.Out?2:1)&&l.Closes==(p.Out?(p.In?2:1):0)&&l.Unlocks==(p.Release?1:0)&&
            l.Deletes==(deleted?1:0)&&l.Protections==(copied&&p.Move&&p.Force?1:0)&&
            i.FreeMem==(p.Out&&p.In?1:0),"File-operation ownership or result differs.");
        i.CopyFileOperationLayout=null;
    }
    private void RegisterCopyFileOperationProbeExec()
    {
        Register(ExecBase,ExecLvo.SetSignal,"SetSignal",(s,i)=>{Require(s.D[0]==0&&s.D[1]==0,"Signal ABI differs.");return 0;});
    }
    private void RegisterCopyFileOperationProbeDos(uint b)
    {
        Register(b,DosLvo.Open,"Open",(s,i)=>{
            var p=i.Definition.CopyFileOperation!;var l=i.CopyFileOperationLayout!;
            var first=l.Opens++==0;
            Require(Bus.CString(s.D[1])==(first?"destination":"source")&&s.D[2]==(uint)(first?DOS.FileMode.NewFile:DOS.FileMode.OldFile)&&
                (first||l.Unlocks==(p.Release?1:0)),"Open order or source-lock release differs.");
            i.IoError=205;return first?(p.Out?0x140u:0):(p.In?0x130u:0);
        });
        Register(b,DosLvo.Close,"Close",(s,i)=>{
            var l=i.CopyFileOperationLayout!;Require(s.D[1]==(l.Closes==0?0x140u:0x130u),"Close order differs.");l.Closes++;i.IoError=902;return 1;
        });
        Register(b,DosLvo.UnLock,"UnLock",(s,i)=>{Require(s.D[1]==0x456,"Source lock differs.");i.CopyFileOperationLayout!.Unlocks++;return 0;});
        Register(b,DosLvo.ExamineFH,"ExamineFH",(s,i)=>{Require(s.D[1]==0x130,"Examine handle differs.");Bus.Long(s.D[2]+FileInfoBlock.SizeOffset,8);return 1;});
        Register(b,DosLvo.Read,"Read",(s,i)=>{Require(s.D[1]==0x130&&s.D[3]==512,"Read ABI differs.");i.CopyFileOperationLayout!.Reads++;i.IoError=207;return i.Definition.CopyFileOperation!.Transfer?8u:unchecked((uint)-1);});
        Register(b,DosLvo.Write,"Write",(s,i)=>{Require(s.D[1]==0x140&&s.D[3]==8,"Write ABI differs.");return 8;});
        Register(b,DosLvo.SetProtection,"SetProtection",(s,i)=>{Require(Bus.CString(s.D[1])=="source"&&s.D[2]==0,"Forced source protection differs.");i.CopyFileOperationLayout!.Protections++;return 0;});
        Register(b,DosLvo.DeleteFile,"DeleteFile",(s,i)=>{
            var p=i.Definition.CopyFileOperation!;var l=i.CopyFileOperationLayout!;
            var moved=p.In&&p.Transfer&&p.Move;
            Require(l.Closes==(p.In?2:1)&&Bus.CString(s.D[1])==(moved?"source":"destination")&&l.Protections==(moved&&p.Force?1:0),"Deletion target or order differs.");
            l.Deletes++;i.IoError=903;return p.Delete?1u:0;
        });
        Register(b,DosLvo.IoErr,"IoErr",(_,i)=>unchecked((uint)i.IoError));
        Register(b,DosLvo.SetIoErr,"SetIoErr",(s,i)=>{
            if(s.D[1]!=0)Require(s.D[1]==902,"Cleanup must preserve post-close error.");i.IoError=unchecked((int)s.D[1]);return 0;
        });
    }
}
