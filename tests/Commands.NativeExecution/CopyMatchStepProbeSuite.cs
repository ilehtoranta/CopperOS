using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopyMatchStepProbeCase(int Type, bool First, byte Flags,
    bool All, int Pattern, bool PriorDeep, int ExpectedDepth, uint ExpectedFlags,
    byte ExpectedAnchorFlags, bool Work, bool Deep, int InitialDepth = 3);
internal sealed class CopyMatchStepNativeLayout(uint control)
{
    public uint Control { get; } = control;
    public int Patterns, Directories, Locks, Unlocks;
    public bool Contains(uint address, int size) => address >= Control &&
        (ulong)address + (uint)size <= (ulong)Control + 8192;
}
internal sealed partial class ProbeFixture
{
    public const string CopyMatchStepProbeSuite = "copy-match-step-native-entry-vector-fixture";
    private List<object> RunCopyMatchStepProbeCases()
    {
        var did = (byte)AnchorPathFlags.DidDirectory;
        var dod = (byte)AnchorPathFlags.DoDirectory;
        ProbeCase[] cases = [
            StepCase("file", -3, true, 0, false, 0, false, 3, 1, 0, true, false),
            StepCase("pattern-match", -3, false, 0, false, 1, false, 3, 1, 0, true, false),
            StepCase("pattern-reject", -3, false, 0, false, -1, false, 3, 1, 0, false, false),
            StepCase("first-directory", 2, true, 0, false, -1, false, 3, 513, dod, false, false),
            StepCase("first-before-exit", 2, true, did, true, -1, false, 3, 513, (byte)(did|dod), false, false),
            StepCase("delete-exit", 2, false, did, false, -1, false, 2, 1|(1u<<23), 0, true, false),
            StepCase("ordinary-directory", 2, false, 0, false, -1, false, 3, 1, 0, true, false),
            StepCase("recursive-directory", 2, false, 0, true, -1, false, 3, 1, dod, true, true),
            StepCase("deferred-depth", -3, false, 0, false, 0, true, 4, 1, 0, true, false),
            StepCase("depth-overflow", -3, false, 0, false, 0, true, 0, 1, 0, true, false, 255),
            StepCase("depth-underflow", 2, false, did, false, -1, false, 255, 1|(1u<<23), 0, true, false, 0),
        ];
        var results = new List<object>();
        foreach (var item in cases) results.AddRange(Execute([item], false));
        results.AddRange(Execute([cases[4] with {Name="interleaved-first"},
            cases[7] with {Name="interleaved-recursive"}],true));
        Bus.AssertImageUnchanged(); return results;
    }
    private static ProbeCase StepCase(string name, int type, bool first, byte flags,
        bool all, int pattern, bool priorDeep, int depth, uint commandFlags,
        byte anchorFlags, bool work, bool deep, int initialDepth = 3) => new(name,"",DOS.RETURN_OK,0,"") {
            EntryLength=80, CopyMatchStep=new(type,first,flags,all,pattern,priorDeep,
                depth,commandFlags,anchorFlags,work,deep,initialDepth) };
    private void PrepareCopyMatchStepProbe(Invocation i)
    {
        var c=i.Arguments; var p=i.Definition.CopyMatchStep!;
        Bus.Memory.AsSpan((int)c,8192).Clear(); i.CopyMatchStepLayout=new(c);
        Bus.Long(c,c+256); Bus.Long(c+4,c+4096); Bus.Long(c+8,c+6144);
        Bus.Long(c+12,p.Pattern==0?0:c+100); Bus.Memory[c+100]=(byte)'P';
        Bus.Long(c+16,2); Bus.Long(c+20,p.All?1u:0); Bus.Long(c+24,1);
        Bus.Long(c+40,p.First?1u:0); Bus.Long(c+44,(uint)p.InitialDepth); Bus.Long(c+52,1|(1u<<23));
        Bus.Long(c+56,99); Bus.Long(c+60,p.PriorDeep?1u:0);
        var a=c+256; Bus.Long(a+(uint)DosLayout.AnchorPath.Current,c+3072);
        Bus.Long(c+3072+(uint)DosLayout.AChain.Lock,0x120);
        Bus.Memory[a+(uint)DosLayout.AnchorPath.Flags]=p.Flags;
        var fib=a+(uint)DosLayout.AnchorPath.Info;
        Bus.Long(fib+FileInfoBlock.DirEntryTypeOffset,unchecked((uint)p.Type));
        Encoding.Latin1.GetBytes("entry\0").CopyTo(Bus.Memory.AsSpan((int)(fib+FileInfoBlock.FileNameOffset)));
        for(var n=0;n<2048;n++) Bus.Memory[a+(uint)DosLayout.AnchorPath.PathBuffer+n]=(byte)(n%251);
    }
    private void VerifyCopyMatchStepProbe(Invocation i)
    {
        var c=i.Arguments; var p=i.Definition.CopyMatchStep!;var l=i.CopyMatchStepLayout!;
        Require(Bus.Long(c+40)==0 && Bus.Long(c+44)==p.ExpectedDepth &&
            Bus.Long(c+52)==p.ExpectedFlags && Bus.Long(c+56)==99 &&
            Bus.Long(c+60)==(p.Deep?1u:0) && Bus.Long(c+64)==(p.Work?1u:0) && Bus.Long(c+68)==0 &&
            Bus.Memory[c+256+(uint)DosLayout.AnchorPath.Flags]==p.ExpectedAnchorFlags,
            $"{i.Definition.Name}: combined match state differs.");
        Require(Bus.Memory.AsSpan((int)c+4096,2048).SequenceEqual(Bus.Memory.AsSpan((int)(c+256+(uint)DosLayout.AnchorPath.PathBuffer),2048)) &&
            Bus.Memory.AsSpan((int)c+6144,(int)FileInfoBlock.SizeInBytes).SequenceEqual(Bus.Memory.AsSpan((int)(c+256+(uint)DosLayout.AnchorPath.Info),(int)FileInfoBlock.SizeInBytes)),
            "Complete path/FIB snapshot differs.");
        var probe=p.Type>0&&!p.First&&p.All&&(p.Flags&(byte)AnchorPathFlags.DidDirectory)==0;
        Require(l.Patterns==(p.Type<0&&p.Pattern!=0?1:0) && l.Directories==(probe?2:0)&&l.Locks==(probe?1:0)&&l.Unlocks==(probe?1:0),"Match branch calls differ.");
        i.CopyMatchStepLayout=null;
    }
    private void RegisterCopyMatchStepProbeDos(uint b)
    {
        Register(b,DosLvo.MatchPatternNoCase,"MatchPatternNoCase",(s,i)=>{
            Require(Bus.CString(s.D[1])=="P"&&Bus.CString(s.D[2])=="entry","Pattern ABI differs.");
            i.CopyMatchStepLayout!.Patterns++; return i.Definition.CopyMatchStep!.Pattern>0?1u:0;
        });
        Register(b,DosLvo.CurrentDir,"CurrentDir",(s,i)=>{
            var l=i.CopyMatchStepLayout!;Require(s.D[1]==(l.Directories==0?0x120u:0x321u),"Probe directory differs.");l.Directories++;return 0x321;
        });
        Register(b,DosLvo.Lock,"Lock",(s,i)=>{Require(Bus.CString(s.D[1])=="entry","Probe filename differs.");i.CopyMatchStepLayout!.Locks++;return 0x456;});
        Register(b,DosLvo.UnLock,"UnLock",(s,i)=>{Require(s.D[1]==0x456,"Probe unlock differs.");i.CopyMatchStepLayout!.Unlocks++;return 0;});
        Register(b,DosLvo.SetIoErr,"SetIoErr",(s,i)=>{i.IoError=unchecked((int)s.D[1]);return 0;});
    }
}
