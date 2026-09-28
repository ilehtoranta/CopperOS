using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;
namespace CopperOS.Commands.NativeExecution;
internal sealed partial class ProbeFixture
{
    private void RegisterCopyDirectDos(uint b)
    {
        Register(b,DosLvo.Open,"Open",(s,i)=>{var l=i.CopyArgumentGateLayout!;var k=i.Definition.CopyArgumentGate!.DirectKind;
            if(explicitCopyParser) Require(Bus.Long(i.Process+(uint)DosLayout.Process.WindowPointer)==
                ((i.Definition.CopyArgumentGate!.Switches&(1u<<12))!=0?uint.MaxValue:i.Process+0x380),"Requester suppression differs during operation.");
            Require(i.FreeArgs==0&&Bus.CString(s.D[1])==(l.Opens==0?"Work:From":"RAM:Target")&&s.D[2]==(uint)(l.Opens==0?DOS.FileMode.OldFile:DOS.FileMode.NewFile),"DIRECT input-first open/lease differs.");
            l.Opens++;return k=="input-fail"||k=="output-fail"&&l.Opens==2?0u:l.Opens==1?0x130u:0x140u;});
        Register(b,DosLvo.ExamineFH,"ExamineFH",(s,i)=>{Require(s.D[1]==0x130&&i.FreeArgs==0&&(!copyCommandRoot||i.Definition.CopyArgumentGate!.DosVersion<51||i.Definition.CopyArgumentGate.DosVersion==51&&i.Definition.CopyArgumentGate.DosRevision<66),"DIRECT examine lifetime/version differs.");Bus.Long(s.D[2]+FileInfoBlock.SizeOffset,8);
            Bus.Long(s.D[2]+128,0x80000001);Bus.Long(s.D[2]+FileInfoBlock.Size64Offset,uint.MaxValue);
            Bus.Long(s.D[2]+FileInfoBlock.NumBlocks64Offset,uint.MaxValue);return i.Definition.CopyArgumentGate!.DirectKind=="unknown-size"?0u:1u;});
        Register(b,DosLvo.ExamineFH64,"ExamineFH64",(s,i)=>{
            var d=i.Definition.CopyArgumentGate!;
            Require(copyCommandRoot&&(d.DosVersion>51||d.DosVersion==51&&d.DosRevision>=66)&&s.D[1]==0x130&&i.FreeArgs==0,
                "Extended examination version/handle differs.");
            Require(Bus.Long(s.D[3])==0x80000e11&&Bus.Long(s.D[3]+4)==1&&Bus.Long(s.D[3]+8)==0,"Extended examination tags differ.");
            Bus.Long(s.D[2]+FileInfoBlock.Size64Offset,0);Bus.Long(s.D[2]+FileInfoBlock.Size64Offset+4,8);return d.DirectKind=="unknown-size"?0u:1u;
        });
        Register(b,DosLvo.Read,"Read",(s,i)=>{Require(s.D[1]==0x130&&s.D[3]==512&&i.FreeArgs==0,"DIRECT read differs.");
            var d=i.Definition.CopyArgumentGate!;
            if(!copyCommandRoot||d.DosVersion<51||d.DosVersion==51&&d.DosRevision<66)
                Require(Bus.Long(s.D[2]+FileInfoBlock.Size64Offset)==0&&Bus.Long(s.D[2]+FileInfoBlock.Size64Offset+4)==8&&
                    Bus.Long(s.D[2]+FileInfoBlock.NumBlocks64Offset)==0&&Bus.Long(s.D[2]+FileInfoBlock.NumBlocks64Offset+4)==0x80000001,
                    "Classic handle examination must populate unsigned extension fields before reading.");
            if(i.CopyArgumentGateLayout!.TransferReads++!=0) { Require((d.DirectKind is "unknown-size" or "premature-eof")&&i.CopyArgumentGateLayout.TransferReads==2,"Unexpected extra transfer read.");return 0; }
            if(d.DirectKind=="read-fail") { i.IoError=221;return uint.MaxValue; }
            Bus.Memory.AsSpan((int)s.D[2],8).Fill(42);return d.DirectKind=="premature-eof"?4u:8u;});
        Register(b,DosLvo.Write,"Write",(s,i)=>{
            var l=i.CopyArgumentGateLayout!;var kind=i.Definition.CopyArgumentGate!.DirectKind;
            Require(kind!="read-fail","A failed Read must not reach Write.");
            uint expected=l.Writes==1&&(kind is "unknown-size" or "premature-eof")?0u:kind=="premature-eof"?4u:8u;
            Require(s.D[1]==0x140&&s.D[3]==expected&&Bus.Memory.AsSpan((int)s.D[2],(int)expected).ToArray().All(x=>x==42)&&i.FreeArgs==0,"DIRECT payload differs.");
            l.Writes++;return kind=="write-fail"?0u:s.D[3];});
        Register(b,DosLvo.Close,"Close",(s,i)=>{var l=i.CopyArgumentGateLayout!;Require(i.FreeArgs==0&&s.D[1]==(i.Definition.CopyArgumentGate!.DirectKind=="output-fail"||l.Closes==1?0x130u:0x140u),"DIRECT close order differs.");l.Closes++;i.IoError=777;return 0;});
        Register(b,DosLvo.DeleteFile,"DeleteFile",(s,i)=>{var l=i.CopyArgumentGateLayout!;Require(i.Definition.CopyArgumentGate!.DirectKind=="delete"&&i.FreeArgs==0&&Bus.CString(s.D[1])==(l.Deletes==0?"Work:One":"Work:Two"),"DIRECT deleted partial output or skipped a source.");l.Deletes++;return 0;});
    }
}
