using Amiga;
using Copper68k;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopyFileTransferProbeCase(bool Examined,bool Extended,uint High,uint Low,
    int[] Reads,int Result,int FailAlloc=0,bool Cached=false,bool Break=false,bool ShortWrite=false);
internal sealed class CopyFileTransferNativeLayout(uint control)
{
    public uint Control { get; }=control;
    public uint Buffer;
    public int Reads,Writes,Examines;
    public bool Contains(uint address,int size)=>address>=Control&&(ulong)address+(uint)size<=(ulong)Control+128;
}
internal sealed partial class ProbeFixture
{
    public const string CopyFileTransferProbeSuite="copy-file-transfer-native-entry-vector-fixture";
    // Compatibility alias retained for the central suite admission list.
    public const string CopyTransferProbeSuite=CopyFileTransferProbeSuite;
    private List<object> RunCopyFileTransferProbeCases()
    {
        ProbeCase[] cases=[
            TransferCase("examined",new(true,false,0,8,[8],0)),
            TransferCase("empty",new(true,false,0,0,[0],0)),
            TransferCase("premature",new(true,false,0,16,[8,0],20)),
            TransferCase("stream",new(false,false,0,0,[8,0],0)),
            TransferCase("retry",new(true,false,0,8,[8],0,2)),
            TransferCase("allocation-failure",new(false,false,0,0,[],20,3)),
            TransferCase("cached",new(true,false,0,8,[8],0,0,true)),
            TransferCase("extended",new(true,true,0,8,[8],0)),
            TransferCase("extended-high",new(true,true,1,8,[8,0],20)),
            TransferCase("break",new(true,false,0,8,[],20,0,false,true)),
            TransferCase("read-error",new(true,false,0,8,[-1],20)),
            TransferCase("short-write",new(true,false,0,8,[8],20,0,false,false,true)),
        ];
        var r=new List<object>();foreach(var c in cases)r.AddRange(Execute([c],false));
        r.AddRange(Execute([cases[0] with{Name="interleaved-examined"},cases[3] with{Name="interleaved-stream"}],true));
        Bus.AssertImageUnchanged();return r;
    }
    private static ProbeCase TransferCase(string name,CopyFileTransferProbeCase p)=>
        new(name,"",DOS.RETURN_OK,0,""){EntryLength=28,CopyFileTransfer=p};
    private void PrepareCopyFileTransferProbe(Invocation i)
    {
        var c=i.Arguments;var p=i.Definition.CopyFileTransfer!;
        Bus.Memory.AsSpan((int)c,128).Clear();i.CopyFileTransferLayout=new(c);
        Bus.Long(c,0x130);Bus.Long(c+4,0x140);Bus.Long(c+8,2048);Bus.Long(c+20,p.Extended?1u:0);
        if(p.Cached){var buffer=Bus.Allocate(i,512,"Exec",false);i.CopyFileTransferLayout.Buffer=buffer;Bus.Long(c+12,buffer);Bus.Long(c+16,512);}
    }
    private void VerifyCopyFileTransferProbe(Invocation i)
    {
        var p=i.Definition.CopyFileTransfer!;var l=i.CopyFileTransferLayout!;
        var hasBuffer=p.Cached||p.FailAlloc<3;
        Require(Bus.Long(l.Control+24)==p.Result&&l.Examines==(hasBuffer?1:0)&&l.Reads==p.Reads.Length&&
            l.Writes==p.Reads.Count(x=>x>=0)&&i.FreeMem==(hasBuffer?1:0)&&
            i.Allocations==(p.Cached?0:Math.Min(p.FailAlloc+1,3))&&
            Bus.Long(l.Control+16)==(hasBuffer?(p.Cached?512u:2048u>>p.FailAlloc):0),
            $"{i.Definition.Name}: transfer result, allocation or I/O counts differ.");
        i.CopyFileTransferLayout=null;
    }
    private void RegisterCopyFileTransferProbeExec()
    {
        Register(ExecBase,ExecLvo.SetSignal,"SetSignal",(s,i)=>{
            Require(s.D[0]==0&&s.D[1]==0,"Transfer Ctrl-C ABI differs.");return i.Definition.CopyFileTransfer!.Break?4096u:0;
        });
    }
    private void RegisterCopyFileTransferProbeDos(uint b)
    {
        Register(b,DosLvo.ExamineFH,"ExamineFH",(s,i)=>ExamineTransfer(s,i,false));
        Register(b,-1156,"ExamineFH64",(s,i)=>ExamineTransfer(s,i,true));
        Register(b,DosLvo.Read,"Read",(s,i)=>{
            var l=i.CopyFileTransferLayout!;var p=i.Definition.CopyFileTransfer!;
            Require(s.D[1]==0x130&&s.D[2]==l.Buffer&&s.D[3]==(p.Cached?512u:2048u>>p.FailAlloc)&&l.Reads<p.Reads.Length,"Transfer Read ABI differs.");
            return unchecked((uint)p.Reads[l.Reads++]);
        });
        Register(b,DosLvo.Write,"Write",(s,i)=>{
            var l=i.CopyFileTransferLayout!;var p=i.Definition.CopyFileTransfer!;
            Require(s.D[1]==0x140&&s.D[2]==l.Buffer&&s.D[3]==p.Reads[l.Writes],"Transfer Write ABI differs.");
            l.Writes++;return p.ShortWrite?s.D[3]-1:s.D[3];
        });
        Register(b,DosLvo.SetIoErr,"SetIoErr",(s,i)=>{i.IoError=unchecked((int)s.D[1]);return 0;});
    }
    private uint ExamineTransfer(M68kCpuState s,Invocation i,bool extended)
    {
        var p=i.Definition.CopyFileTransfer!;var l=i.CopyFileTransferLayout!;
        Require(extended==p.Extended&&s.D[1]==0x130&&s.D[2]==l.Buffer&&
            Bus.Memory[l.Buffer+FileInfoBlock.ActualExtensionFlagsOffset]==0,"Transfer examine ABI differs.");
        if(extended)Require(Bus.Long(s.D[3])==0x80000e11&&Bus.Long(s.D[3]+4)==1&&Bus.Long(s.D[3]+8)==0,"Examine tags differ.");
        Bus.Long(l.Buffer+FileInfoBlock.SizeOffset,p.Low);
        Bus.Long(l.Buffer+FileInfoBlock.Size64Offset,p.High);
        Bus.Long(l.Buffer+FileInfoBlock.Size64Offset+4,p.Low);
        l.Examines++;return p.Examined?1u:0;
    }
}
