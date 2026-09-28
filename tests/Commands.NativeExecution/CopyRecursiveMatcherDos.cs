using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed partial class ProbeFixture
{
    private void RegisterRecursiveMatcherDos(uint b)
    {
        Register(b,DosLvo.MatchFirst,"MatchFirst",(s,i)=>{
            var l=i.CopyTraversalWorkLayout!;var r=Recursive(i);
            Require(Bus.CString(s.D[1])==i.Definition.CopyTraversalWork!.Source,"Recursive source pattern differs.");
            if(s.D[2]==l.Workspace+108)
            {
                Require(Bus.Memory[s.D[2]+(uint)DosLayout.AnchorPath.Flags]==(byte)AnchorPathFlags.DoWild,
                    "Recursive classifier flags differ.");
                Bus.Memory[s.D[2]+(uint)DosLayout.AnchorPath.Flags]=(byte)(i.Definition.CopyTraversalWork!.SingleFailure=="literal"?0:AnchorPathFlags.IsWild);
                l.Classifies++;return 0;
            }
            Require(l.Anchor==0&&Bus.Long(s.D[2]+(uint)DosLayout.AnchorPath.BreakBits)==4096,
                "Recursive matcher startup differs.");
            l.Anchor=s.D[2];r.MatchIndex=0;
            r.MatcherLock=r.FileSystem.Lock("SYS:");
            // The chain belongs to the matcher fixture, not the command.
            var chain=l.Control+7200;
            Bus.Long(chain+(uint)DosLayout.AChain.Lock,r.MatcherLock);
            Bus.Long(l.Anchor+(uint)DosLayout.AnchorPath.Current,chain);
            CopyRecursiveTrace.Put(Bus,l.Anchor,0,r.Trace);return 0;
        });
        Register(b,DosLvo.MatchNext,"MatchNext",(s,i)=>{
            var l=i.CopyTraversalWorkLayout!;var r=Recursive(i);
            Require(s.D[1]==l.Anchor&&r.MatchIndex<r.Trace.Length,
                "Recursive matcher advanced past its end.");
            var record=r.Trace[r.MatchIndex];
            Require(r.CompletedFiles==record.CompletedBeforeNext,
                "Recursive deferred work did not consume the prior snapshot.");
            var flags=Bus.Memory[l.Anchor+(uint)DosLayout.AnchorPath.Flags];
            if(r.MatchIndex==0&&i.Definition.CopyTraversalWork!.SingleFailure=="probe-dangling")
                Require((flags&(byte)AnchorPathFlags.DoDirectory)==0,"Dangling link requested directory descent.");
            else if(record.Directory&&!record.Exited)
                Require((flags&(byte)AnchorPathFlags.DoDirectory)!=0,"ALL did not request directory descent.");
            if(record.Exited)
                Require((flags&(byte)AnchorPathFlags.DidDirectory)==0,"Directory exit flag was not consumed.");
            l.Nexts++;
            if(r.MatchIndex==2&&i.Definition.CopyTraversalWork!.SingleFailure=="parent-eof")
            {r.MatchIndex++;return (uint)DOS.Error.NoMoreEntries;}
            if(r.MatchIndex==1&&i.Definition.CopyTraversalWork!.SingleFailure is "pending-error" or "pending-break" or "pending-eof")
            {
                r.MatchIndex++;
                if(i.Definition.CopyTraversalWork.SingleFailure=="pending-eof")return (uint)DOS.Error.NoMoreEntries;
                i.IoError=(int)(i.Definition.CopyTraversalWork.SingleFailure=="pending-break"?DOS.Error.Break:DOS.Error.ObjectNotFound);
                return unchecked((uint)i.IoError);
            }
            if(++r.MatchIndex==r.Trace.Length)
            {
                var failure=i.Definition.CopyTraversalWork!.SingleFailure;
                if(failure is "matchnext-error" or "matchnext-break")
                {
                    i.IoError=(int)(failure=="matchnext-break"?DOS.Error.Break:DOS.Error.ObjectNotFound);
                    return unchecked((uint)i.IoError);
                }
                return (uint)DOS.Error.NoMoreEntries;
            }
            r.FileSystem.Unlock(r.MatcherLock);
            var nextPath=r.Trace[r.MatchIndex].Path;var slash=nextPath.LastIndexOf('/');
            r.MatcherLock=r.FileSystem.Lock(slash<0?"SYS:":nextPath[..slash]);
            Bus.Long(l.Control+7200+(uint)DosLayout.AChain.Lock,r.MatcherLock);
            CopyRecursiveTrace.Put(Bus,l.Anchor,r.MatchIndex,r.Trace);return 0;
        });
        Register(b,DosLvo.MatchEnd,"MatchEnd",(s,i)=>{
            var l=i.CopyTraversalWorkLayout!;var r=Recursive(i);
            if(s.D[1]==l.Workspace+108){l.ClassEnds++;return 0;}
            var pendingFailure=i.Definition.CopyTraversalWork!.SingleFailure is "pending-error" or "pending-break" or "pending-eof";
            var parentFailure=i.Definition.CopyTraversalWork!.SingleFailure is "parent-eof" or "parent-zero" or "parent-error";
            var danglingStop=i.Definition.CopyTraversalWork!.SingleFailure=="probe-dangling"&&i.Definition.CopyTraversalWork.StopOnError;
            Require(s.D[1]==l.Anchor&&l.Ends==0&&r.MatchIndex==(pendingFailure||danglingStop?2:parentFailure?3:r.Trace.Length),
                $"Recursive MatchEnd did not follow complete enumeration: index={r.MatchIndex}, count={r.Trace.Length}, completed={r.CompletedFiles}, ends={l.Ends}, anchor={s.D[1]:x}/{l.Anchor:x}, events={string.Join(",",i.Events.TakeLast(35))}.");
            r.FileSystem.Unlock(r.MatcherLock);r.MatcherLock=0;l.Ends++;return 0;
        });
    }
}
