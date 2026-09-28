using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;
namespace CopperOS.Commands.NativeExecution;
internal sealed partial class ProbeFixture
{
    private void RegisterSingleCommandParser(uint b)
    {
        Register(b,DosLvo.AllocDosObject,"AllocDosObject",(s,i)=>{
            Require(s.D[1]==5&&s.D[2]==0,"Single command RDArgs allocation differs.");
            var parser=Bus.Allocate(i,32,"SingleRDArgs",true);
            i.CopyTraversalWorkLayout!.Parser=parser;return parser;
        });
        Register(b,DosLvo.ReadArgs,"ReadArgs",(s,i)=>{
            var l=i.CopyTraversalWorkLayout!;var c=l.Control;l.Workspace=s.D[2];
            Require(s.D[3]==l.Parser&&Bus.OwnedAllocation(i,s.D[2],"Exec").Size==4772&&
                Bus.CString(s.D[1]).StartsWith("FROM/M,TO,PAT=PATTERN/K,BUF=BUFFER/K/N,"),"Single command ReadArgs ABI differs.");
            for(uint n=0;n<24;n++)Require(Bus.Long(s.D[2]+n*4)==0,"Parser slots not clear.");
            if(i.Definition.CopyTraversalWork!.SourceCount>1) {
                System.Text.Encoding.Latin1.GetBytes("SYS:other#?\0").CopyTo(Bus.Memory.AsSpan((int)c+800));
                for(uint n=1;n<i.Definition.CopyTraversalWork.SourceCount;n++) Bus.Long(c+700+n*4,c+800);
                Bus.Long(c+700+(uint)i.Definition.CopyTraversalWork.SourceCount*4,0);
            }
            Bus.Long(s.D[2],c+700);Bus.Long(s.D[2]+4,c+160);
            Bus.Long(c+760,1);Bus.Long(s.D[2]+12,c+760);if(!i.Definition.CopyTraversalWork!.Visible)Bus.Long(s.D[2]+44,uint.MaxValue);
            if(i.Definition.CopyTraversalWork!.StopOnError)Bus.Long(s.D[2]+52,uint.MaxValue);
            var scenario=i.Definition.CopyTraversalWork!.SingleFailure;
            if(scenario is "move" or "move-copy")Bus.Long(s.D[2]+60,uint.MaxValue);
            if(scenario=="hardlink")Bus.Long(s.D[2]+68,uint.MaxValue);
            if(scenario=="softlink")Bus.Long(s.D[2]+72,uint.MaxValue);
            i.Reads++;return l.Parser;
        });
        Register(b,DosLvo.FreeArgs,"FreeArgs",(s,i)=>{Require(s.D[1]==i.CopyTraversalWorkLayout!.Parser,"Single FreeArgs differs.");i.FreeArgs++;return 0;});
        Register(b,DosLvo.FreeDosObject,"FreeDosObject",(s,i)=>{
            var l=i.CopyTraversalWorkLayout!;Require(s.D[1]==5&&s.D[2]==l.Parser&&i.FreeArgs==1,"Single parser release differs.");
            Bus.Release(i,l.Parser,"SingleRDArgs");l.Parser=0;i.IoError=903;return 0;
        });
        Register(b,DosLvo.SetProtection,"SetProtection",(s,i)=>{
            Require((directoryCommandRoot?Bus.CString(s.D[1]).StartsWith("RAM:file"):Bus.CString(s.D[1])=="RAM:file0")&&s.D[2]==0&&i.FreeArgs==0,"Single target metadata differs.");return 1;
        });
    }
}
