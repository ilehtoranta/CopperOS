using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record RelabelEntryCase(string Drive, string Name, bool Found = true,
    bool RelabelSucceeds = true, int Error = 0, int ParserError = 0);
internal sealed record RelabelNativeLayout(uint RdArgs);

internal sealed partial class ProbeFixture
{
    public const string RelabelEntrySuite = "relabel-native-entry-vector-fixture";
    private List<object> RunRelabelEntryCases()
    {
        ProbeCase[] cases =
        [
            Case("success", "DH0:", "Work", DOS.RETURN_OK, ""),
            Case("name-colon", "DH0:", "Bad:Name", DOS.RETURN_FAIL, "':' not legal character in volume name\n"),
            Case("drive-invalid", "DH0", "Work", DOS.RETURN_FAIL, "Invalid device or volume name\n"),
            Case("entry-missing", "DH0:", "Work", DOS.RETURN_FAIL, "Invalid device or volume name\n", false),
            Case("relabel-failure", "DH0:", "Work", DOS.RETURN_FAIL, "", true, false, 205),
            Case("parser-failure", "DH0:", "Work", DOS.RETURN_FAIL, "", true, true, 116, 116),
        ];
        var reports = new List<object>(); foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([cases[0] with { Name="repeat-success" },cases[3] with { Name="repeat-missing" }],true)); Bus.AssertImageUnchanged(); return reports;
    }
    private static ProbeCase Case(string label,string drive,string name,int result,string output,bool found=true,bool relabelSucceeds=true,int error=0,int parserError=0)=>new(label,"",result,error,output){Relabel=new(drive,name,found,relabelSucceeds,error,parserError)};
    private void PrepareRelabelEntry(Invocation invocation)
    {
        var d=invocation.Definition.Relabel!;var l=new RelabelNativeLayout(Bus.Allocate(invocation,128,"RelabelRDArgs",true));invocation.RelabelLayout=l; PutRelabel(l.RdArgs+32,d.Drive);PutRelabel(l.RdArgs+64,d.Name);
    }
    private void VerifyRelabelEntry(Invocation i)
    {
        var d=i.Definition.Relabel!; var parser=d.ParserError!=0; Require(i.Reads==1&&i.FreeArgs==(parser?0:1),"Relabel parser lifetime differs."); Require(i.Events.Count(x=>x=="LockDosList")==((parser||d.Name.Contains(':')||!d.Drive.EndsWith(':'))?0:1)&&i.Events.Count(x=>x=="UnLockDosList")==((parser||d.Name.Contains(':')||!d.Drive.EndsWith(':'))?0:1),"Relabel list lock lifetime differs."); Require(i.Events.Count(x=>x=="Relabel")==(!parser&&d.Found&&!d.Name.Contains(':')&&d.Drive.EndsWith(':')?1:0),"Relabel vector count differs."); i.RelabelLayout=null;
    }
    private void RegisterRelabelEntryDos(uint b)
    {
        Register(b,DosLvo.ReadArgs,"ReadArgs",(s,i)=>{var d=i.Definition.Relabel!;Require(Bus.CString(s.D[1])=="DRIVE/A,NAME/A","Relabel template differs.");Require(Bus.OwnedAllocation(i,s.D[2],"Exec").Size==8,"Relabel results differ.");i.Reads++;if(d.ParserError!=0){Bus.Release(i,i.RelabelLayout!.RdArgs,"RelabelRDArgs");i.RelabelLayout=null;i.IoError=d.ParserError;return 0;}var l=i.RelabelLayout!;Bus.Long(s.D[2],l.RdArgs+32);Bus.Long(s.D[2]+4,l.RdArgs+64);return l.RdArgs;});
        Register(b,DosLvo.FreeArgs,"FreeArgs",(s,i)=>{Bus.Release(i,s.D[1],"RelabelRDArgs");i.FreeArgs++;return 0;});
        Register(b,DosLvo.LockDosList,"LockDosList",(s,i)=>{Require(s.D[1]==((uint)DosListLockFlags.Read|(uint)DosListLockFlags.Devices|(uint)DosListLockFlags.Volumes),"Relabel lock flags differ.");return 0x32000;});
        Register(b,DosLvo.FindDosEntry,"FindDosEntry",(s,i)=>{var d=i.Definition.Relabel!;Require(Bus.CString(s.D[2])==d.Drive[..^1],"Relabel find name differs.");return d.Found?0x32100u:0;});
        Register(b,DosLvo.UnLockDosList,"UnLockDosList",(_,_)=>0);
        Register(b,DosLvo.Relabel,"Relabel",(s,i)=>{var d=i.Definition.Relabel!;Require(Bus.CString(s.D[1])==d.Drive&&Bus.CString(s.D[2])==d.Name,"Relabel ABI differs.");i.IoError=d.Error;return d.RelabelSucceeds?1u:0;});
        Register(b,DosLvo.PutStr,"PutStr",(s,i)=>{i.Output.Write(Encoding.Latin1.GetBytes(Bus.CString(s.D[1])));return 0;});Register(b,DosLvo.IoErr,"IoErr",(_,i)=>unchecked((uint)i.IoError));Register(b,DosLvo.PrintFault,"PrintFault",(_,_)=>0);Register(b,DosLvo.SetIoErr,"SetIoErr",(s,i)=>{i.IoError=unchecked((int)s.D[1]);return 0;});
    }
    private void PutRelabel(uint address,string value){Encoding.Latin1.GetBytes(value).CopyTo(Bus.Memory.AsSpan((int)address));Bus.Memory[address+(uint)value.Length]=0;}
}
