using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopyWorkPreparationProbeCase(int Mode,uint Flags,int Result,
    int Secondary,int Cached,bool NameSuccess,bool AddSuccess,bool Ready,int Names,int Adds,int Unlocks);
internal sealed class CopyWorkPreparationNativeLayout(uint control)
{
    public uint Control { get; }=control;
    public int Names,Adds,Unlocks;
    public bool Contains(uint address,int size)=>address>=Control&&
        (ulong)address+(uint)size<=(ulong)Control+4096;
}
internal sealed partial class ProbeFixture
{
    public const string CopyWorkPreparationProbeSuite="copy-work-preparation-native-entry-vector-fixture";
    private List<object> RunCopyWorkPreparationProbeCases()
    {
        ProbeCase[] cases=[
            WorkPreparationCase("populate",0,0,0,0,0,true,true,true,1,1,0),
            WorkPreparationCase("cached",0,0,0,0,4,true,true,true,0,1,0),
            WorkPreparationCase("name-failure",0,0,0,0,0,false,true,false,1,0,1),
            WorkPreparationCase("add-failure",0,0,0,0,0,true,false,true,1,1,0),
            WorkPreparationCase("delete",2,0,0,0,0,true,true,true,0,0,0),
            WorkPreparationCase("device",0,1u<<25,0,0,0,true,true,true,0,0,0),
            WorkPreparationCase("warning-allowed",0,0,5,0,0,true,true,true,1,1,0),
            WorkPreparationCase("errwarn",0,1024,5,0,0,true,true,false,0,0,0),
            WorkPreparationCase("primary-error",0,0,10,0,0,true,true,false,0,0,0),
            WorkPreparationCase("secondary-error",0,0,0,20,0,true,true,false,0,0,0),
        ];
        var r=new List<object>();foreach(var c in cases)r.AddRange(Execute([c],false));
        r.AddRange(Execute([cases[0] with{Name="interleaved-populate"},cases[1] with{Name="interleaved-cache"}],true));
        Bus.AssertImageUnchanged();return r;
    }
    private static ProbeCase WorkPreparationCase(string n,int mode,uint flags,int result,int secondary,int cached,
        bool nameSuccess,bool addSuccess,bool ready,int names,int adds,int unlocks)=>new(n,"",DOS.RETURN_OK,0,"") {
            EntryLength=40,CopyWorkPreparation=new(mode,flags,result,secondary,cached,nameSuccess,addSuccess,ready,names,adds,unlocks)};
    private void PrepareCopyWorkPreparationProbe(Invocation i)
    {
        var c=i.Arguments;var p=i.Definition.CopyWorkPreparation!;
        Bus.Memory.AsSpan((int)c,4096).Clear();i.CopyWorkPreparationLayout=new(c);
        Bus.Long(c,c+64);Bus.Long(c+4,c+128);Bus.Long(c+8,(uint)p.Mode);Bus.Long(c+12,p.Flags);
        Bus.Long(c+16,(uint)p.Result);Bus.Long(c+20,(uint)p.Secondary);Bus.Long(c+24,(uint)p.Cached);
        Encoding.Latin1.GetBytes("next\0").CopyTo(Bus.Memory.AsSpan((int)c+64));
        Encoding.Latin1.GetBytes("RAM:previous\0").CopyTo(Bus.Memory.AsSpan((int)c+128));
    }
    private void VerifyCopyWorkPreparationProbe(Invocation i)
    {
        var l=i.CopyWorkPreparationLayout!;var p=i.Definition.CopyWorkPreparation!;
        var size=p.Adds>0?4:p.Cached;
        Require(Bus.Long(l.Control+28)==(p.Ready?1u:0)&&Bus.Long(l.Control+32)==size&&
            Bus.Long(l.Control+36)==(p.Unlocks>0?20u:(uint)p.Secondary)&&
            l.Names==p.Names&&l.Adds==p.Adds&&l.Unlocks==p.Unlocks,
            $"{i.Definition.Name}: work preparation outcome differs.");
        Require(Bus.CString(l.Control+128)==(p.Adds>0?(p.AddSuccess?"RAM:next":"RAM:"):"RAM:previous"),"Destination path differs.");
        i.CopyWorkPreparationLayout=null;
    }
    private void RegisterCopyWorkPreparationProbeDos(uint b)
    {
        Register(b,DosLvo.NameFromLock,"NameFromLock",(s,i)=>{
            var l=i.CopyWorkPreparationLayout!;l.Names++;
            Require(s.D[1]==0x120&&s.D[2]==l.Control+128&&s.D[3]==2048,"NameFromLock ABI differs.");
            if(!i.Definition.CopyWorkPreparation!.NameSuccess)return 0;
            Encoding.Latin1.GetBytes("RAM:\0").CopyTo(Bus.Memory.AsSpan((int)s.D[2]));return 1;
        });
        Register(b,DosLvo.AddPart,"AddPart",(s,i)=>{
            var l=i.CopyWorkPreparationLayout!;l.Adds++;
            Require(s.D[1]==l.Control+128&&Bus.CString(s.D[1])=="RAM:"&&Bus.CString(s.D[2])=="next"&&s.D[3]==2048,"AddPart ABI or cache truncation differs.");
            if(!i.Definition.CopyWorkPreparation!.AddSuccess)return 0;
            Encoding.Latin1.GetBytes("RAM:next\0").CopyTo(Bus.Memory.AsSpan((int)s.D[1]));return 1;
        });
        Register(b,DosLvo.UnLock,"UnLock",(s,i)=>{Require(s.D[1]==0,"Failure unlock differs.");i.CopyWorkPreparationLayout!.Unlocks++;return 0;});
        Register(b,DosLvo.SetIoErr,"SetIoErr",(s,i)=>{i.IoError=unchecked((int)s.D[1]);return 0;});
    }
}
