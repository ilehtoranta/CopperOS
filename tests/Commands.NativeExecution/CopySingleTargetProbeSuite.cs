using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;
namespace CopperOS.Commands.NativeExecution;
internal sealed partial class ProbeFixture
{
    private void VerifySingleTarget(Invocation i)
    {
        var l=i.CopyTraversalWorkLayout!;
        var scenario=i.Definition.CopyTraversalWork!.SingleFailure;
        var transferred=scenario is null or "write" or "move-copy";
        var noTransfer=scenario is "move" or "hardlink" or "softlink";
        var warning=scenario is "output" or "input" or "write" or "softlink";
        var flags=256u|(1u<<21)|(1u<<22)|(scenario=="softlink"?16u|(1u<<20):0u);
        if(!singleCommandRoot) Require(Bus.Long(l.Control+44)==(warning?5u:0u)&&Bus.Long(l.Control+48)==0&&Bus.Long(l.Control+52)==flags,
            "Single target result differs.");
        Require(l.TargetParses==2&&l.Classifies==(i.Definition.CopyTraversalWork!.Visible?3:2)&&l.ClassEnds==l.Classifies&&l.Unlocks==6,
            "Single target classification/lock lifecycle differs.");
        Require(l.Opens==(noTransfer?0:scenario=="output"?1:2)&&l.Closes==(noTransfer||scenario=="output"?0:scenario=="input"?1:2)&&
            l.Completed==(transferred?1:0)&&l.Nexts==(scenario is "input" or "write" or "move-copy"?1:0)&&
            l.Creates==(scenario is "move" or "move-copy"?1:0)&&l.Parts==(scenario=="hardlink"?1:0)&&
            i.Allocations==(transferred?3:2)+(singleCommandRoot?1:0)&&i.FreeMem==i.Allocations,"Single target operation lifecycle differs.");
        Require(Bus.CString(l.Control+160)=="RAM:file0","Target suffix not restored.");
        Require(Bus.CString(l.Control+80)=="SYS:file0","Quoted source was not normalized in place.");
        if(singleCommandRoot) Require(i.Reads==1&&i.FreeArgs==1&&l.Parser==0&&i.Events.IndexOf("FreeArgs")>
            i.Events.LastIndexOf("UnLock")&&i.Events.IndexOf("FreeDosObject")<i.Events.LastIndexOf("FreeMem"),
            "Normal target command parser/cleanup lifetime differs.");
        if(i.Definition.CopyTraversalWork!.Visible) Require(i.Events.Count(x=>x=="PrintFault")==1&&!i.Events.Contains("SetProtection"),
            "Visible failure must skip the quiet metadata tail and duplicate final fault.");
        i.CopyTraversalWorkLayout=null;
    }
    private void RegisterSingleTarget(uint b)
    {
        if(singleCommandRoot) RegisterSingleCommandParser(b);
        if(singleCommandRoot)
        {
            Register(b,DosLvo.VPrintf,"VPrintf",(s,i)=>{
                var operation=i.Definition.CopyTraversalWork!.SingleFailure switch {"output"=>"opened for output","softlink"=>"linked.",_=>"copied."};
                Require(i.Definition.CopyTraversalWork!.Visible&&Bus.CString(s.D[1])==" not %s: "&&
                    Bus.CString(Bus.Long(s.D[2]))==operation&&i.FreeArgs==0,
                    "Visible failure prefix/operation differs.");
                i.Output.Write(Encoding.Latin1.GetBytes(" not "+operation+": "));i.IoError=7777;return 0;
            });
            Register(b,DosLvo.PrintFault,"PrintFault",(s,i)=>{
                Require(i.Definition.CopyTraversalWork!.Visible&&s.D[1]==7777&&s.D[2]==0&&i.FreeArgs==0,
                    "Failure must report post-prefix IoErr before parser cleanup.");
                i.Output.Write(Encoding.Latin1.GetBytes("[DOS fault]\n"));return 1;
            });
        }
        Register(b,DosLvo.ParsePattern,"ParsePattern",(s,i)=>{var l=i.CopyTraversalWorkLayout!;Require(Bus.CString(s.D[1])==(l.TargetParses==0?"RAM:file0":i.Definition.CopyTraversalWork!.Source)&&s.D[3]==(l.TargetParses==0?21:i.Definition.CopyTraversalWork!.Source.Length*2+3),"Single target pattern parse differs.");l.TargetParses++;Encoding.Latin1.GetBytes((l.TargetParses==2?"SYS:file0":Bus.CString(s.D[1]))+"\0").CopyTo(Bus.Memory.AsSpan((int)s.D[2]));return 0;});
        Register(b,DosLvo.PathPart,"PathPart",(s,i)=>s.D[1]+4);
        Register(b,DosLvo.FilePart,"FilePart",(s,i)=>{Require(Bus.CString(s.D[1])=="RAM:file0","Single output name differs.");return s.D[1]+4;});
        Register(b,DosLvo.MatchFirst,"MatchFirst",(s,i)=>{Require(Bus.CString(s.D[1])==i.Definition.CopyTraversalWork!.Source,"Source classification differs.");i.CopyTraversalWorkLayout!.Classifies++;Bus.Memory[s.D[2]+(uint)DosLayout.AnchorPath.Flags]=0;return 0;});
        Register(b,DosLvo.MatchEnd,"MatchEnd",(_,i)=>{i.CopyTraversalWorkLayout!.ClassEnds++;return 0;});
        Register(b,DosLvo.IsFileSystem,"IsFileSystem",(s,i)=>{Require(Bus.CString(s.D[1]) is "RAM:" or "SYS:","Device prefix differs.");return 1;});
        Register(b,DosLvo.Lock,"Lock",(s,i)=>{var l=i.CopyTraversalWorkLayout!;var name=Bus.CString(s.D[1]);if(name=="RAM:file0")return 0;if(name=="RAM:")return ++l.DestinationLocks==1?0u:0x100;if(name=="SYS:file0")return ++l.Names<3?0x555u:0x456;throw new InvalidOperationException("Unexpected source/target lock: "+name);});
        Register(b,DosLvo.Examine,"Examine",(s,i)=>{Require(s.D[1]==0x555,"Source examination differs.");Bus.Long(s.D[2]+FileInfoBlock.DirEntryTypeOffset,unchecked((uint)-3));
            i.CopyTraversalWorkLayout!.Anchor=s.D[2];
            Bus.Long(s.D[2]+FileInfoBlock.SizeOffset,0x80000001);Bus.Long(s.D[2]+128,0xf0000001);
            Bus.Long(s.D[2]+FileInfoBlock.Size64Offset,uint.MaxValue);Bus.Long(s.D[2]+FileInfoBlock.NumBlocks64Offset,uint.MaxValue);
            return 1;});
        Register(b,-120,"CreateDir",(s,i)=>{Require(Bus.CString(s.D[1])=="RAM:","Parent creation differs.");return 0x222;});
        Register(b,DosLvo.UnLock,"UnLock",(s,i)=>{var l=i.CopyTraversalWorkLayout!;
            if(l.Unlocks==2)Require(Bus.Long(l.Anchor+FileInfoBlock.Size64Offset)==0&&Bus.Long(l.Anchor+FileInfoBlock.Size64Offset+4)==0x80000001&&
                Bus.Long(l.Anchor+FileInfoBlock.NumBlocks64Offset)==0&&Bus.Long(l.Anchor+FileInfoBlock.NumBlocks64Offset+4)==0xf0000001,
                "Classic source sizes must be zero-extended into FIB extension fields.");
            uint[] expected=[0x555,0x222,0x555,0x123,0x456,0x100];Require(l.Unlocks<expected.Length&&s.D[1]==expected[l.Unlocks],$"Single file unlock order differs: {s.D[1]:X}, index {l.Unlocks}.");l.Unlocks++;return 0;});
        Register(b,DosLvo.ParentDir,"ParentDir",(s,i)=>{Require(s.D[1]==0x456,"Worker source parent differs.");return 0x123;});
        Register(b,DosLvo.NameFromLock,"NameFromLock",(s,i)=>{Require(s.D[1]==0x100,"Worker destination parent differs.");Encoding.Latin1.GetBytes("RAM:\0").CopyTo(Bus.Memory.AsSpan((int)s.D[2]));return 1;});
        Register(b,DosLvo.AddPart,"AddPart",(s,i)=>{Require(Bus.CString(s.D[1])=="RAM:"&&Bus.CString(s.D[2])=="file0","Worker output path differs.");Encoding.Latin1.GetBytes("RAM:file0\0").CopyTo(Bus.Memory.AsSpan((int)s.D[1]));return 1;});
        Register(b,DosLvo.Rename,"Rename",(s,i)=>{var l=i.CopyTraversalWorkLayout!;Require(i.Definition.CopyTraversalWork!.SingleFailure is "move" or "move-copy"&&Bus.CString(s.D[1])=="SYS:file0"&&Bus.CString(s.D[2])=="RAM:file0"&&l.Unlocks==4&&l.Opens==0,"MOVE rename arguments/order differ.");l.Creates++;return i.Definition.CopyTraversalWork.SingleFailure=="move"?1u:0u;});
        Register(b,DosLvo.MakeLink,"MakeLink",(s,i)=>{var l=i.CopyTraversalWorkLayout!;Require(i.Definition.CopyTraversalWork!.SingleFailure=="hardlink"&&Bus.CString(s.D[1])=="RAM:file0"&&s.D[2]==0x456&&s.D[3]==0&&l.Unlocks==4,"Hard-link lock/type/order differ.");l.Parts++;return 1;});
        Register(b,DosLvo.Open,"Open",(s,i)=>{var l=i.CopyTraversalWorkLayout!;Require(Bus.CString(s.D[1])==(l.Opens==0?"RAM:file0":"SYS:file0"),"Normal copy open order differs.");l.Opens++;var failure=i.Definition.CopyTraversalWork!.SingleFailure;if(failure=="output"&&l.Opens==1||failure=="input"&&l.Opens==2){i.IoError=205;return 0;}return l.Opens==1?0x140u:0x130;});
        Register(b,DosLvo.ExamineFH,"ExamineFH",(s,i)=>{Bus.Long(s.D[2]+FileInfoBlock.SizeOffset,8);return 1;});
        Register(b,DosLvo.Read,"Read",(s,i)=>{Require(s.D[1]==0x130&&s.D[3]==512,"Single transfer read differs.");Bus.Memory.AsSpan((int)s.D[2],8).Fill(42);return 8;});
        Register(b,DosLvo.Write,"Write",(s,i)=>{Require(s.D[1]==0x140&&s.D[3]==8&&Bus.Memory.AsSpan((int)s.D[2],8).ToArray().All(x=>x==42),"Single transfer payload differs.");i.CopyTraversalWorkLayout!.Completed++;if(i.Definition.CopyTraversalWork!.SingleFailure=="write"){i.IoError=221;return 0;}return 8;});
        Register(b,DosLvo.Close,"Close",(s,i)=>{var l=i.CopyTraversalWorkLayout!;Require(s.D[1]==(l.Closes++==0?0x140u:0x130u),"Single close order differs.");i.IoError=777;return 1;});
        Register(b,DosLvo.DeleteFile,"DeleteFile",(s,i)=>{var l=i.CopyTraversalWorkLayout!;Require(i.Definition.CopyTraversalWork!.SingleFailure is "input" or "write" or "move-copy"&&Bus.CString(s.D[1])==(i.Definition.CopyTraversalWork.SingleFailure=="move-copy"?"SYS:file0":"RAM:file0")&&l.Closes==(i.Definition.CopyTraversalWork.SingleFailure=="input"?1:2)&&i.IoError==777,"Partial-output cleanup must follow closes.");l.Nexts++;i.IoError=999;return 1;});
        Register(b,DosLvo.IoErr,"IoErr",(_,i)=>unchecked((uint)i.IoError));
        Register(b,DosLvo.SetIoErr,"SetIoErr",(s,i)=>{i.IoError=unchecked((int)s.D[1]);return 0;});
    }
}
