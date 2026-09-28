using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopyResultPolicyProbeCase(int RetVal, int RetVal2, uint Flags,
    int ExpectedResult, int ExpectedError, int Faults);
internal sealed class CopyResultPolicyNativeLayout(uint control)
{ public uint Control { get; } = control; public int Faults; public bool Contains(uint a, int s) => a >= Control && (ulong)a + (uint)s <= (ulong)Control + 16; }
internal sealed partial class ProbeFixture
{
    public const string CopyCompletionSuite="copy-completion-native-entry-vector-fixture";
    public const string CopyResultPolicyProbeSuite = "copy-result-policy-native-entry-vector-fixture";
    private const uint CtrlC = 1, Quiet = 2, ErrWarn = 4;
    private List<object> RunCopyResultPolicyProbeCases()
    {
        var cases = new[] { C("ok",0,0,0,0,31337,0), C("break",0,0,CtrlC,5,(int)DOS.Error.Break,1),
            C("break-quiet",0,0,CtrlC|Quiet,5,(int)DOS.Error.Break,0), C("warn",0,5,0,5,77,1),
            C("primary",10,5,0,10,77,0), C("errwarn",0,5,ErrWarn,10,77,1), C("break-errwarn",0,0,CtrlC|ErrWarn,10,(int)DOS.Error.Break,1) };
        if (suite == CopyCompletionSuite) cases = cases.Concat(cases.Select(c=>c with { Name="cached-"+c.Name, Error=902, CopyResultPolicy=c.CopyResultPolicy! with { Flags=c.CopyResultPolicy!.Flags|8 } })).ToArray();
        var result=new List<object>(); foreach(var c in cases) result.AddRange(Execute([c],false)); result.AddRange(Execute([cases[1] with{Name="interleaved-break"},cases[4] with{Name="interleaved-primary"}],true)); Bus.AssertImageUnchanged(); return result;
    }
    private static ProbeCase C(string n,int r,int r2,uint f,int e,int io,int faults)=>new(n,"",e,io,""){EntryLength=16,CopyResultPolicy=new(r,r2,f,e,io,faults)};
    private void PrepareCopyResultPolicyProbe(Invocation i){var c=i.Arguments;Bus.Memory.AsSpan((int)c,16).Clear();i.CopyResultPolicyLayout=new(c);var p=i.Definition.CopyResultPolicy!;Bus.Long(c,unchecked((uint)p.RetVal));Bus.Long(c+4,unchecked((uint)p.RetVal2));Bus.Long(c+8,p.Flags);i.IoError=p.Flags==0&&p.RetVal2==0?31337:77;}
    private void VerifyCopyResultPolicyProbe(Invocation i){var p=i.Definition.CopyResultPolicy!;var l=i.CopyResultPolicyLayout!;Require(unchecked((int)Bus.Long(l.Control+12))==p.ExpectedResult&&l.Faults==p.Faults,$"{i.Definition.Name}: Copy result policy differs.");if (suite == CopyCompletionSuite) Require(i.Allocations==((p.Flags&8)!=0?1:0)&&i.FreeMem==i.Allocations,"Completion cache ownership differs.");i.CopyResultPolicyLayout=null;}
    private void RegisterCopyResultPolicyProbeDos(uint b){Register(b,DosLvo.IoErr,"IoErr",(_,i)=>unchecked((uint)i.IoError));Register(b,DosLvo.SetIoErr,"SetIoErr",(s,i)=>{i.IoError=unchecked((int)s.D[1]);return 0;});Register(b,DosLvo.PrintFault,"PrintFault",(s,i)=>{Require(i.FreeMem==0,"Completion freed cache before reporting.");Require(s.D[1]==unchecked((uint)i.IoError)&&s.D[2]==0,"Copy PrintFault ABI differs.");i.CopyResultPolicyLayout!.Faults++;return 0;});}
}
