using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopyTraversalProbeCase(int Entries, int Terminal,
    int WorkerResult = 0, bool ErrorWarn = false);
internal sealed class CopyTraversalNativeLayout(uint control)
{
    public uint Control { get; } = control;
    public uint Anchor;
    public int Nexts, Ends, Works, Diagnostics;
    public bool Contains(uint address, int size) => address >= Control &&
        (ulong)address + (uint)size <= (ulong)Control + 4096;
}
internal sealed partial class ProbeFixture
{
    public const string CopyTraversalProbeSuite = "copy-traversal-native-entry-vector-fixture";
    private List<object> RunCopyTraversalProbeCases()
    {
        ProbeCase[] cases = [
            TraversalCase("two",2,232), TraversalCase("one",1,232),
            TraversalCase("empty",0,232), TraversalCase("first-error",0,205),
            TraversalCase("next-error",2,218),
            TraversalCase("allocation-failure",0,232) with {AllocationFailure=true},
            TraversalCase("worker-warning-continues",3,232,5),
            TraversalCase("worker-error-stops",3,232,10),
            TraversalCase("errwarn-stops-warning",3,232,5,true),
            TraversalCase("secondary-stops",3,232,-20),
        ];
        var r=new List<object>();foreach(var c in cases)r.AddRange(Execute([c],false));
        r.AddRange(Execute([cases[0] with{Name="interleaved-two"},cases[4] with{Name="interleaved-error"}],true));
        Bus.AssertImageUnchanged();return r;
    }
    private static ProbeCase TraversalCase(string n,int entries,int terminal,int workerResult=0,bool errorWarn=false)=>
        new(n,"",DOS.RETURN_OK,0,""){EntryLength=48,CopyTraversal=new(entries,terminal,workerResult,errorWarn)};
    private void PrepareCopyTraversalProbe(Invocation i)
    {
        var c=i.Arguments;Bus.Memory.AsSpan((int)c,4096).Clear();
        i.CopyTraversalLayout=new(c);Bus.Long(c,c+64);Bus.Long(c+4,c+256);Bus.Long(c+8,c+2304);
        Encoding.Latin1.GetBytes("SYS:#?\0").CopyTo(Bus.Memory.AsSpan((int)c+64));
        Bus.Long(c+24,i.Definition.CopyTraversal!.ErrorWarn?1024u:0);
    }
    private void VerifyCopyTraversalProbe(Invocation i)
    {
        var p=i.Definition.CopyTraversal!;var l=i.CopyTraversalLayout!;
        var allocated=!i.Definition.AllocationFailure;
        var error=allocated&&p.Terminal!=232;
        var works=allocated?Math.Max(0,p.Entries-(error?1:0)):0;
        var stopped=p.WorkerResult<0||p.WorkerResult>(p.ErrorWarn?0:5);
        if(stopped) works=1;
        Require(Bus.Long(l.Control+32)==(allocated?(uint)Math.Max(0,p.WorkerResult):20u)&&
            Bus.Long(l.Control+36)==(error?20u:(uint)Math.Max(0,-p.WorkerResult))&&
            l.Nexts==(allocated?(stopped?2:p.Entries):0)&&l.Ends==(allocated?1:0)&&l.Works==works&&
            l.Diagnostics==(error?2:0)&&i.FreeMem==(allocated?1:0),
            $"{i.Definition.Name}: traversal lifecycle differs: next={l.Nexts}, end={l.Ends}, work={l.Works}, diagnostics={l.Diagnostics}.");
        i.CopyTraversalLayout=null;
    }
    private void PutTraversalEntry(uint anchor,int index)
    {
        var fib=anchor+(uint)DosLayout.AnchorPath.Info;
        Bus.Long(fib+FileInfoBlock.DirEntryTypeOffset,unchecked((uint)-3));
        var name="file"+index;
        Encoding.Latin1.GetBytes(name+"\0").CopyTo(Bus.Memory.AsSpan((int)(fib+FileInfoBlock.FileNameOffset)));
        Encoding.Latin1.GetBytes("SYS:"+name+"\0").CopyTo(Bus.Memory.AsSpan((int)(anchor+(uint)DosLayout.AnchorPath.PathBuffer)));
    }
    private void RegisterCopyTraversalProbeDos(uint b)
    {
        Register(b,DosLvo.MatchFirst,"MatchFirst",(s,i)=>{
            var l=i.CopyTraversalLayout!;var p=i.Definition.CopyTraversal!;
            Require(Bus.CString(s.D[1])=="SYS:#?"&&Bus.Word(s.D[2]+(uint)DosLayout.AnchorPath.StringLength)==2048&&
                Bus.Long(s.D[2]+(uint)DosLayout.AnchorPath.BreakBits)==4096,"Traversal MatchFirst ABI differs.");
            Bus.OwnedAllocation(i,s.D[2],"Exec");l.Anchor=s.D[2];
            if(p.Entries>0){PutTraversalEntry(l.Anchor,0);return 0;}
            i.IoError=p.Terminal;return unchecked((uint)p.Terminal);
        });
        Register(b,DosLvo.MatchNext,"MatchNext",(s,i)=>{
            var l=i.CopyTraversalLayout!;var p=i.Definition.CopyTraversal!;
            Require(s.D[1]==l.Anchor&&l.Works==Math.Max(0,l.Nexts),"Deferred work must precede the next iteration snapshot.");
            l.Nexts++;if(l.Nexts<p.Entries){PutTraversalEntry(l.Anchor,l.Nexts);return 0;}
            i.IoError=p.Terminal;return unchecked((uint)p.Terminal);
        });
        Register(b,DosLvo.MatchEnd,"MatchEnd",(s,i)=>{
            var l=i.CopyTraversalLayout!;Require(s.D[1]==l.Anchor,"MatchEnd anchor differs.");l.Ends++;return 0;
        });
        Register(b,DosLvo.PutStr,"PutStr",(s,i)=>{
            var l=i.CopyTraversalLayout!;var p=i.Definition.CopyTraversal!;
            Require(Bus.CString(s.D[1])=="file"+l.Works&&
                Bus.CString(l.Control+256)=="SYS:file"+l.Works&&
                (l.Works==p.Entries-1?l.Ends==1:l.Ends==0),"Worker snapshot or final-work ordering differs.");
            l.Works++;return unchecked((uint)p.WorkerResult);
        });
        Register(b,DosLvo.IoErr,"IoErr",(_,i)=>unchecked((uint)i.IoError));
        Register(b,DosLvo.VPrintf,"VPrintf",(s,i)=>{
            Require(i.CopyTraversalLayout!.Ends==1&&Bus.CString(s.D[1])=="%s - "&&Bus.CString(Bus.Long(s.D[2]))=="SYS:#?","Matcher diagnostic differs.");
            i.CopyTraversalLayout.Diagnostics++;i.IoError=900;return 0;
        });
        Register(b,DosLvo.PrintFault,"PrintFault",(s,i)=>{
            Require(s.D[1]==i.Definition.CopyTraversal!.Terminal&&s.D[2]==0,"Matcher fault error preservation differs.");
            i.CopyTraversalLayout!.Diagnostics++;return 0;
        });
        Register(b,DosLvo.SetIoErr,"SetIoErr",(s,i)=>{i.IoError=unchecked((int)s.D[1]);return 0;});
    }
}
