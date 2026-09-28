using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;
namespace CopperOS.Commands.NativeExecution;
internal sealed partial class ProbeFixture
{
    private void RegisterCopyParsedMakeDirectoryDos(uint b)
    {
        Register(b,DosLvo.ParsePattern,"ParsePattern",(s,i)=>{var l=i.CopyArgumentGateLayout!;Require(i.FreeArgs==0&&s.D[3]==19&&Bus.CString(s.D[1])==(l.Parses==0?"Work:One":"Work:Two"),"MAKEDIR parser/source ownership differs.");l.CurrentSource=Bus.CString(s.D[1]);l.Parses++;return i.Definition.CopyArgumentGate!.NormalKind switch {"mkdir-pattern-fail"=>uint.MaxValue,"mkdir-wildcard"=>1u,_=>0};});
        Register(b,DosLvo.Lock,"Lock",(s,i)=>{var l=i.CopyArgumentGateLayout!;Require(i.FreeArgs==0&&Bus.CString(s.D[1])==l.CurrentSource&&s.D[2]==unchecked((uint)DOS.LockMode.Shared),"MAKEDIR target lock differs.");l.Opens++;return i.Definition.CopyArgumentGate!.NormalKind is "mkdir-existing" or "mkdir-examine-fail"?l.Opens==1?0x170u:0x150u:l.Opens%2==0?0x150u:0;});
        Register(b,DosLvo.Examine,"Examine",(s,i)=>{
            Require(i.Definition.CopyArgumentGate!.DosVersion==40&&s.D[1]==0x170&&s.D[2]==i.CopyArgumentGateLayout!.DestinationFib&&
                Bus.Memory[s.D[2]+FileInfoBlock.ActualExtensionFlagsOffset]==0,"Classic destination examination differs.");
            Bus.Long(s.D[2]+FileInfoBlock.SizeOffset,0x80000001);Bus.Long(s.D[2]+128,0xf0000001);
            Bus.Long(s.D[2]+FileInfoBlock.Size64Offset,uint.MaxValue);Bus.Long(s.D[2]+FileInfoBlock.NumBlocks64Offset,uint.MaxValue);
            Bus.Long(s.D[2]+FileInfoBlock.DirEntryTypeOffset,2);
            return i.Definition.CopyArgumentGate.NormalKind=="mkdir-examine-fail"?0u:1u;
        });
        Register(b,DosLvo.Examine64,"Examine64",(s,i)=>{
            var l=i.CopyArgumentGateLayout!;
            Require(s.D[1]==0x170&&s.D[2]==l.DestinationFib&&i.FreeArgs==0&&
                Bus.Memory[s.D[2]+FileInfoBlock.ActualExtensionFlagsOffset]==0&&
                Bus.Long(s.D[3])==0x80000e11&&Bus.Long(s.D[3]+4)==1&&Bus.Long(s.D[3]+8)==0,
                "Destination Examine64 ABI/tags differ.");
            Bus.Long(s.D[2]+FileInfoBlock.DirEntryTypeOffset,2);
            return i.Definition.CopyArgumentGate!.NormalKind=="mkdir-examine-fail"?0u:1u;
        });
        Register(b,-120,"CreateDir",(s,i)=>{var l=i.CopyArgumentGateLayout!;Require(i.FreeArgs==0&&Bus.CString(s.D[1])==l.CurrentSource&&l.Parses==l.Writes+1,"MAKEDIR skipped current source after pattern failure.");l.Writes++;return i.Definition.CopyArgumentGate!.NormalKind=="mkdir-create-fail"?0u:0x160;});
        Register(b,DosLvo.UnLock,"UnLock",(s,i)=>{var l=i.CopyArgumentGateLayout!;Require(i.FreeArgs==0&&s.D[1]==(l.Unlocks%2==0?i.Definition.CopyArgumentGate!.NormalKind is "mkdir-existing" or "mkdir-examine-fail"?0x170u:0x160u:0x150u),"Created/final directory lock release differs.");l.Unlocks++;return 0;});
    }
}
