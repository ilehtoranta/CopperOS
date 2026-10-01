using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;
namespace CopperOS.Commands.NativeExecution;
internal sealed partial class ProbeFixture
{
    private void RegisterCopyParsedDeleteDos(uint b)
    {
        if (unifiedCopyRoot) Register(b,DosLvo.SetProtection,"SetProtection",(s,i)=>{Require(i.FreeArgs==0&&Bus.CString(s.D[1])==""&&s.D[2]==0,"Original DELETE metadata tail differs.");return 0;});
        Register(b,DosLvo.ParsePatternNoCase,"ParsePatternNoCase",(s,i)=>{var l=i.CopyArgumentGateLayout!;
            Require(i.FreeArgs==0&&Bus.CString(s.D[1])=="#?"&&s.D[3]==7,"Pattern parse ABI/lifetime differs.");l.CompiledPattern=s.D[2];l.Parses++;
            Bus.Memory[s.D[2]]=0x55;return i.Definition.CopyArgumentGate!.NormalKind=="pattern-invalid"?uint.MaxValue:1;});
        Register(b,DosLvo.MatchPatternNoCase,"MatchPatternNoCase",(s,i)=>{var l=i.CopyArgumentGateLayout!;
            Require(i.FreeArgs==0&&s.D[1]==l.CompiledPattern&&Bus.Memory[s.D[1]]==0x55&&Bus.CString(s.D[2])==l.CurrentSource.Substring(5),"Compiled filter lifetime/filename differs.");l.Filters++;
            return i.Definition.CopyArgumentGate!.NormalKind=="pattern-reject"?0u:1;});
        Register(b,DosLvo.MatchFirst,"MatchFirst",(s,i)=>{
            var l=i.CopyArgumentGateLayout!;Require(i.FreeArgs==0,"Parser released before traversal.");
            l.CurrentSource=Bus.CString(s.D[1]);Require(l.CurrentSource==(l.Matches==0?"Work:One":"Work:Two"),"Source vector advancement differs.");
            l.Matches++;l.Anchor=s.D[2];var fib=l.Anchor+(uint)DosLayout.AnchorPath.Info;
            Bus.Long(fib+FileInfoBlock.DirEntryTypeOffset,unchecked((uint)-3));
            Encoding.Latin1.GetBytes(l.CurrentSource.Substring(5)+"\0").CopyTo(Bus.Memory.AsSpan((int)(fib+FileInfoBlock.FileNameOffset)));
            Encoding.Latin1.GetBytes(l.CurrentSource+"\0").CopyTo(Bus.Memory.AsSpan((int)(l.Anchor+(uint)DosLayout.AnchorPath.PathBuffer)));return 0;});
        Register(b,DosLvo.MatchNext,"MatchNext",(s,i)=>{Require(s.D[1]==i.CopyArgumentGateLayout!.Anchor&&i.FreeArgs==0,"MatchNext lifetime differs.");return 232;});
        Register(b,DosLvo.MatchEnd,"MatchEnd",(s,i)=>{var l=i.CopyArgumentGateLayout!;Require(s.D[1]==l.Anchor&&l.Deletes==(i.Definition.CopyArgumentGate!.NormalKind=="pattern-reject"?0:l.Ends)&&i.FreeArgs==0,"Final pending deletion must follow MatchEnd.");l.Ends++;return 0;});
        Register(b,DosLvo.Lock,"Lock",(s,i)=>{var l=i.CopyArgumentGateLayout!;Require(i.FreeArgs==0&&Bus.CString(s.D[1])==l.CurrentSource&&l.Ends==l.Matches,"Delete source lock differs.");return 0x150;});
        Register(b,DosLvo.ParentDir,"ParentDir",(s,i)=>{Require(s.D[1]==0x150,"Delete parent lock differs.");return 0x160;});
        Register(b,DosLvo.UnLock,"UnLock",(s,i)=>{var l=i.CopyArgumentGateLayout!;Require(s.D[1]==(l.Unlocks%2==0?0x160u:0x150u)&&i.FreeArgs==0,$"Delete unlock order differs: lock={s.D[1]:X}, unlocks={l.Unlocks}, parser releases={i.FreeArgs}.");l.Unlocks++;return 0;});
        Register(b,DosLvo.DeleteFile,"DeleteFile",(s,i)=>{var l=i.CopyArgumentGateLayout!;Require(i.FreeArgs==0&&Bus.CString(s.D[1])==l.CurrentSource&&l.Unlocks==l.Matches*2,"Delete executed with wrong source/locks.");l.Deletes++;return i.Definition.CopyArgumentGate!.NormalKind!.StartsWith("failed")?0u:1;});
    }
}
