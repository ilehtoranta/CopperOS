using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopyOutputProbeCase(uint Depth,bool Directory,bool Follows,bool Error,string Expected);
internal sealed class CopyOutputNativeLayout(uint control)
{
    public uint Control { get; }=control;
    public StringBuilder Text { get; }=new();
    public int Outputs,Flushes,Faults;
    public bool Contains(uint a,int n)=>a>=Control&&(ulong)a+(uint)n<=(ulong)Control+128;
}
internal sealed partial class ProbeFixture
{
    public const string CopyOutputProbeSuite="copy-output-native-entry-vector-fixture";
    private List<object> RunCopyOutputProbeCases()
    {
        ProbeCase[] cases=[
            OutputCase("zero-depth",0,false,false,false,"   entry"),
            OutputCase("one-depth",1,false,true,false,"   entry.."),
            OutputCase("three-depth",3,false,true,false,"                   entry.."),
            OutputCase("directory",2,true,true,false,"                entry (Dir)"),
            OutputCase("error",0,false,false,true," not entry: [fault]"),
        ];
        var r=new List<object>();foreach(var c in cases)r.AddRange(Execute([c],false));
        r.AddRange(Execute([cases[3] with{Name="interleaved-directory"},cases[4] with{Name="interleaved-error"}],true));
        Bus.AssertImageUnchanged();return r;
    }
    private static ProbeCase OutputCase(string n,uint depth,bool directory,bool follows,bool error,string expected)=>
        new(n,"",DOS.RETURN_OK,0,""){EntryLength=24,CopyOutput=new(depth,directory,follows,error,expected)};
    private void PrepareCopyOutputProbe(Invocation i)
    {
        var c=i.Arguments;var p=i.Definition.CopyOutput!;Bus.Memory.AsSpan((int)c,128).Clear();i.CopyOutputLayout=new(c);
        Bus.Long(c,c+64);Bus.Long(c+4,p.Depth);Bus.Long(c+8,p.Directory?1u:0);Bus.Long(c+12,p.Follows?1u:0);Bus.Long(c+16,p.Error?1u:0);
        Encoding.Latin1.GetBytes("entry\0").CopyTo(Bus.Memory.AsSpan((int)c+64));
    }
    private void VerifyCopyOutputProbe(Invocation i)
    {
        var p=i.Definition.CopyOutput!;var l=i.CopyOutputLayout!;
        Require(l.Text.ToString()==p.Expected&&l.Outputs==(p.Error?0:1)&&l.Flushes==(p.Error?0:1)&&l.Faults==(p.Error?1:0),
            $"{i.Definition.Name}: Copy output differs: [{l.Text}].");i.CopyOutputLayout=null;
    }
    private void RegisterCopyOutputProbeDos(uint b)
    {
        Register(b,DosLvo.PutStr,"PutStr",(s,i)=>{i.CopyOutputLayout!.Text.Append(Bus.CString(s.D[1]));return 0;});
        Register(b,DosLvo.VPrintf,"VPrintf",(s,i)=>{
            var format=Bus.CString(s.D[1]);var l=i.CopyOutputLayout!;
            Require(s.D[2]==l.Control+20,"Output argument storage differs.");
            var name=Bus.CString(Bus.Long(s.D[2]));
            Require(format is "%s" or "%s (Dir)" or ".." or " not %s: ","Unexpected Copy format.");
            l.Text.Append(format.Replace("%s",name));i.IoError=902;return 0;
        });
        Register(b,DosLvo.Output,"Output",(_,i)=>{i.CopyOutputLayout!.Outputs++;return 0x140;});
        Register(b,DosLvo.Flush,"Flush",(s,i)=>{Require(s.D[1]==0x140&&i.CopyOutputLayout!.Text.ToString()==i.Definition.CopyOutput!.Expected,"Flush handle or timing differs.");i.CopyOutputLayout.Flushes++;return 1;});
        Register(b,DosLvo.IoErr,"IoErr",(_,i)=>{Require(i.CopyOutputLayout!.Text.ToString()==" not entry: ","Error read must follow prefix.");return unchecked((uint)i.IoError);});
        Register(b,DosLvo.PrintFault,"PrintFault",(s,i)=>{Require(s.D[1]==902&&s.D[2]==0,"PrintNotDone must use post-prefix IoErr.");i.CopyOutputLayout!.Text.Append("[fault]");i.CopyOutputLayout.Faults++;return 1;});
        Register(b,DosLvo.SetIoErr,"SetIoErr",(s,i)=>{i.IoError=unchecked((int)s.D[1]);return 0;});
    }
}
