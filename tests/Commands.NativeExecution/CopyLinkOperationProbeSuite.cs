using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopyLinkOperationProbeCase(bool Soft,bool Name,bool Link);
internal sealed class CopyLinkOperationNativeLayout(uint control)
{
    public uint Control { get; }=control;
    public int Names,Links;
    public bool Contains(uint a,int n)=>a>=Control&&(ulong)a+(uint)n<=(ulong)Control+128;
}
internal sealed partial class ProbeFixture
{
    public const string CopyLinkOperationProbeSuite="copy-link-operation-native-entry-vector-fixture";
    private List<object> RunCopyLinkOperationProbeCases()
    {
        ProbeCase[] cases=[LinkCase("hard",false,true,true),LinkCase("hard-failure",false,true,false),
            LinkCase("soft",true,true,true),LinkCase("name-failure",true,false,true),
            LinkCase("soft-failure",true,true,false),LinkCase("allocation-failure",true,true,true) with{AllocationFailure=true}];
        var r=new List<object>();foreach(var c in cases)r.AddRange(Execute([c],false));
        r.AddRange(Execute([cases[0] with{Name="interleaved-hard"},cases[2] with{Name="interleaved-soft"}],true));
        Bus.AssertImageUnchanged();return r;
    }
    private static ProbeCase LinkCase(string n,bool soft,bool name,bool link)=>new(n,"",DOS.RETURN_OK,0,""){
        EntryLength=16,CopyLinkOperation=new(soft,name,link)};
    private void PrepareCopyLinkOperationProbe(Invocation i)
    {
        var c=i.Arguments;Bus.Memory.AsSpan((int)c,128).Clear();i.CopyLinkOperationLayout=new(c);
        Bus.Long(c,0x456);Bus.Long(c+4,c+64);Bus.Long(c+8,i.Definition.CopyLinkOperation!.Soft?1u:0);
        Encoding.Latin1.GetBytes("RAM:link\0").CopyTo(Bus.Memory.AsSpan((int)c+64));
    }
    private void VerifyCopyLinkOperationProbe(Invocation i)
    {
        var p=i.Definition.CopyLinkOperation!;var l=i.CopyLinkOperationLayout!;
        var allocated=p.Soft&&!i.Definition.AllocationFailure;
        var linked=!p.Soft||allocated&&p.Name;
        Require(Bus.Long(l.Control+12)==(linked&&p.Link?1u:0)&&l.Names==(allocated?1:0)&&l.Links==(linked?1:0)&&
            i.Allocations==(p.Soft?1:0)&&i.FreeMem==(allocated?1:0),"Copy link ownership/result differs.");
        i.CopyLinkOperationLayout=null;
    }
    private void RegisterCopyLinkOperationProbeDos(uint b)
    {
        Register(b,DosLvo.NameFromLock,"NameFromLock",(s,i)=>{
            Require(s.D[1]==0x456&&s.D[3]==2048,"Copy link name ABI differs.");Bus.OwnedAllocation(i,s.D[2],"Exec");
            i.CopyLinkOperationLayout!.Names++;
            Encoding.Latin1.GetBytes("Work:dir/source\0").CopyTo(Bus.Memory.AsSpan((int)s.D[2]));
            return i.Definition.CopyLinkOperation!.Name?1u:0;
        });
        Register(b,DosLvo.MakeLink,"MakeLink",(s,i)=>{
            var p=i.Definition.CopyLinkOperation!;
            Require(Bus.CString(s.D[1])=="RAM:link"&&s.D[3]==(p.Soft?1u:0)&&
                (p.Soft?Bus.CString(s.D[2])=="Work:dir/source":s.D[2]==0x456),"Copy link target/mode differs.");
            i.CopyLinkOperationLayout!.Links++;return p.Link?1u:0;
        });
        Register(b,DosLvo.SetIoErr,"SetIoErr",(s,i)=>{i.IoError=unchecked((int)s.D[1]);return 0;});
    }
}
