using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed class CopyRecursiveIoState
{
    public CopyRecursiveFileSystem FileSystem { get; }
    public CopyRecursiveMatchRecord[] Trace { get; }
    public CopyRecursiveIoState(bool nested=false,bool literal=false,bool dangling=false){FileSystem=new(nested,dangling);Trace=dangling?CopyRecursiveTrace.Dangling:literal?CopyRecursiveTrace.Literal:nested?CopyRecursiveTrace.Nested:CopyRecursiveTrace.Siblings;}
    public readonly Dictionary<uint, (string Path, bool Output, MemoryStream Data)> Handles = [];
    public uint NextHandle = 0x2000;
    public int CompletedFiles, MatchIndex, Dates, Comments;
    public int FailedLinkProbes, DeviceProbes, ProbeRestores, LinkReads, DeviceReleases;
    public int DanglingWorkAttempts;
    public uint MatcherLock;
    public readonly List<string> MetadataPaths = [];
}

internal sealed partial class ProbeFixture
{
    private static bool RecursiveHasLinkDevice(string? scenario) => scenario is "probe-not-link" or "probe-empty-link" or "probe-buffer-failure" or "probe-dangling";
    // Registered by the recursive suite, independently of flat-file callbacks.
    private void RegisterRecursiveFileDos(uint b)
    {
        Register(b,DosLvo.Rename,"Rename",(s,i)=>{
            Require(i.Definition.CopyTraversalWork!.SingleFailure=="move"&&Bus.CString(s.D[1]).StartsWith("SYS:")&&Bus.CString(s.D[2]).StartsWith("RAM:"),"Recursive cross-device rename differs.");
            return 0;
        });
        Register(b,DosLvo.DeleteFile,"DeleteFile",(s,i)=>{
            Require(i.Definition.CopyTraversalWork!.SingleFailure is "move" or "delete","COPY must not delete source files.");
            var name=Bus.CString(s.D[1]);Recursive(i).FileSystem.Delete(name);
            if(i.Definition.CopyTraversalWork!.SingleFailure=="delete"&&name.EndsWith("/child"))Recursive(i).CompletedFiles++;return 1;
        });
        Register(b,DosLvo.Lock,"Lock",(s,i)=> {
            var r=Recursive(i);var name=Bus.CString(s.D[1]);var scenario=i.Definition.CopyTraversalWork!.SingleFailure;
            if(name=="SYS:first"&&scenario=="probe-dangling")
            {
                Require(r.MatchIndex==1&&r.DanglingWorkAttempts==0&&r.DeviceReleases==1,
                    "Dangling deferred source work ran at the wrong boundary.");
                r.DanglingWorkAttempts++;i.IoError=(int)DOS.Error.ObjectNotFound;
            }
            if(name=="first"&&(scenario is "probe-other-error" or "probe-no-device"||RecursiveHasLinkDevice(scenario)))
            {
                Require(r.MatchIndex==0&&r.FileSystem.Name(r.FileSystem.CurrentDirectory)=="SYS:"&&r.FailedLinkProbes==0,
                    "Soft-link probe did not use the matcher parent and relative name.");
                r.FailedLinkProbes++;i.IoError=(int)(scenario=="probe-other-error"?DOS.Error.InvalidLock:DOS.Error.ObjectNotFound);
                return 0;
            }
            return r.FileSystem.Lock(name);
        });
        Register(b,DosLvo.UnLock,"UnLock",(s,i)=> { Recursive(i).FileSystem.Unlock(s.D[1]);return 0; });
        Register(b,DosLvo.ParentDir,"ParentDir",(s,i)=> {
            var r=Recursive(i);var scenario=i.Definition.CopyTraversalWork!.SingleFailure;
            if(scenario is "parent-eof" or "parent-zero" or "parent-error"&&r.FileSystem.Name(s.D[1])=="RAM:first")
            {
                Require(r.MatchIndex==2&&r.CompletedFiles==1,"Parent failure occurred before the first exit.");
                i.IoError=scenario=="parent-zero"?0:(int)DOS.Error.ObjectNotFound;
                return 0;
            }
            return r.FileSystem.Parent(s.D[1]);
        });
        Register(b,DosLvo.CurrentDir,"CurrentDir",(s,i)=> {
            var r=Recursive(i);
            if(r.FailedLinkProbes>r.ProbeRestores)
            {
                Require(s.D[1]==CopyRecursiveFileSystem.BorrowedCurrentDirectory&&
                    i.IoError==(int)(i.Definition.CopyTraversalWork!.SingleFailure=="probe-other-error"?DOS.Error.InvalidLock:DOS.Error.ObjectNotFound),
                    "Soft-link probe failed to restore IoErr before CurrentDir.");
                r.ProbeRestores++;
            }
            return r.FileSystem.ChangeDirectory(s.D[1]);
        });
        Register(b,DosLvo.GetDeviceProc,"GetDeviceProc",(s,i)=> {
            var r=Recursive(i);
            Require((i.Definition.CopyTraversalWork!.SingleFailure=="probe-no-device"||RecursiveHasLinkDevice(i.Definition.CopyTraversalWork.SingleFailure))&&r.FailedLinkProbes==1&&
                r.DeviceProbes==0&&s.D[1]!=0&&Bus.CString(s.D[1])==""&&s.D[2]==0&&r.FileSystem.Name(r.FileSystem.CurrentDirectory)=="SYS:",
                "Soft-link device lookup ABI or ordering differs.");
            r.DeviceProbes++;i.IoError=901;
            if(!RecursiveHasLinkDevice(i.Definition.CopyTraversalWork.SingleFailure))return 0;
            var device=i.CopyTraversalWorkLayout!.Control+7300;
            Bus.Long(device,0x6780);Bus.Long(device+4,r.MatcherLock);return device;
        });
        Register(b,DosLvo.ReadLink,"ReadLink",(s,i)=> {
            var r=Recursive(i);
            Require(RecursiveHasLinkDevice(i.Definition.CopyTraversalWork!.SingleFailure)&&i.Definition.CopyTraversalWork.SingleFailure!="probe-buffer-failure"&&r.DeviceProbes==1&&r.LinkReads==0&&
                s.D[1]==0x6780&&s.D[2]==r.MatcherLock&&Bus.CString(s.D[3])=="first"&&s.D[5]==511&&
                Bus.OwnedAllocation(i,s.D[4],"Exec").Size==512,"Recursive ReadLink ABI/buffer differs.");
            r.LinkReads++;i.IoError=903;
            if(i.Definition.CopyTraversalWork.SingleFailure=="probe-dangling")
            {Encoding.Latin1.GetBytes("missing\0").CopyTo(Bus.Memory.AsSpan((int)s.D[4]));Bus.Memory[s.D[4]+511]=0xA5;return 7;}
            return i.Definition.CopyTraversalWork.SingleFailure=="probe-not-link"?uint.MaxValue:0;
        });
        Register(b,DosLvo.FreeDeviceProc,"FreeDeviceProc",(s,i)=> {
            var r=Recursive(i);
            var failedBuffer=i.Definition.CopyTraversalWork!.SingleFailure=="probe-buffer-failure";
            Require(RecursiveHasLinkDevice(i.Definition.CopyTraversalWork!.SingleFailure)&&r.LinkReads==(failedBuffer?0:1)&&r.DeviceReleases==0&&
                s.D[1]==i.CopyTraversalWorkLayout!.Control+7300&&
                (failedBuffer?i.Allocations==4&&i.FreeMem==1&&!i.Events.Contains("ReadLink"):i.Events.LastIndexOf("FreeMem")>i.Events.IndexOf("ReadLink")),
                "Recursive device release preceded link-buffer cleanup or used a wrong pointer.");
            r.DeviceReleases++;i.IoError=904;return 0;
        });
        Register(b,DosLvo.SameDevice,"SameDevice",(s,i)=> Recursive(i).FileSystem.SameDevice(s.D[1],s.D[2])?1u:0u);
        Register(b,DosLvo.CreateDir,"CreateDir",(s,i)=> Recursive(i).FileSystem.CreateDirectory(Bus.CString(s.D[1])));
        Register(b,DosLvo.NameFromLock,"NameFromLock",(s,i)=> {
            var bytes=Encoding.Latin1.GetBytes(Recursive(i).FileSystem.Name(s.D[1])+"\0");
            Require(bytes.Length<=s.D[3],"Recursive destination name buffer is too short.");
            bytes.CopyTo(Bus.Memory.AsSpan((int)s.D[2]));return 1;
        });
        Register(b,DosLvo.AddPart,"AddPart",(s,i)=> {
            var parent=Bus.CString(s.D[1]);var child=Bus.CString(s.D[2]);
            var bytes=Encoding.Latin1.GetBytes(parent+(parent.EndsWith(':')?"":"/")+child+"\0");
            Require(bytes.Length<=s.D[3],"Recursive AddPart buffer is too short.");
            bytes.CopyTo(Bus.Memory.AsSpan((int)s.D[1]));return 1;
        });
        Register(b,DosLvo.Open,"Open",(s,i)=> {
            var r=Recursive(i);var name=r.FileSystem.Resolve(Bus.CString(s.D[1]));
            if(i.Definition.CopyTraversalWork!.Visible)
                Require(i.CopyTraversalWorkLayout!.NameFlushes==2,"Recursive file opened before name flush.");
            var output=s.D[2]==(uint)DOS.FileMode.NewFile;
            Require(output||s.D[2]==(uint)DOS.FileMode.OldFile,"Unexpected recursive open mode.");
            var data=output?new MemoryStream():new MemoryStream(r.FileSystem.ReadSource(name),false);
            var handle=r.NextHandle++;r.Handles.Add(handle,(name,output,data));return handle;
        });
        Register(b,DosLvo.ExamineFH,"ExamineFH",(s,i)=> {
            var h=Recursive(i).Handles[s.D[1]];Require(!h.Output,"ExamineFH used output handle.");
            Bus.Long(s.D[2]+FileInfoBlock.SizeOffset,checked((uint)h.Data.Length));return 1;
        });
        Register(b,DosLvo.Read,"Read",(s,i)=> {
            var h=Recursive(i).Handles[s.D[1]];Require(!h.Output,"Read used output handle.");
            return (uint)h.Data.Read(Bus.Memory.AsSpan((int)s.D[2],checked((int)s.D[3])));
        });
        Register(b,DosLvo.Write,"Write",(s,i)=> {
            var h=Recursive(i).Handles[s.D[1]];Require(h.Output,"Write used input handle.");
            h.Data.Write(Bus.Memory.AsSpan((int)s.D[2],checked((int)s.D[3])));return s.D[3];
        });
        Register(b,DosLvo.Close,"Close",(s,i)=> {
            var r=Recursive(i);Require(r.Handles.Remove(s.D[1],out var h),"Duplicate recursive file close.");
            if(h.Output)r.FileSystem.WriteDestination(h.Path,h.Data.ToArray());
            else r.CompletedFiles++;
            h.Data.Dispose();return 1;
        });
    }

    private static CopyRecursiveIoState Recursive(Invocation i) =>
        i.CopyTraversalWorkLayout?.Recursive ?? throw new InvalidOperationException("Missing recursive invocation state.");
}
