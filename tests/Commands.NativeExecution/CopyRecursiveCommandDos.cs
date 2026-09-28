using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;
namespace CopperOS.Commands.NativeExecution;
internal sealed partial class ProbeFixture
{
    private static DOS.Error RecursiveExpectedFault(string? scenario) => scenario switch
    {
        "matchnext-break" or "pending-break" => DOS.Error.Break,
        "parent-zero" => DOS.Error.InvalidLock,
        _ => DOS.Error.ObjectNotFound
    };
    private void RegisterRecursiveCommandDos(uint b)
    {
        RegisterRecursiveFileDos(b);
        RegisterRecursiveMatcherDos(b);
        Register(b,DosLvo.PutStr,"PutStr",(s,i)=>{
            var text=Bus.CString(s.D[1]);
            Require(i.Definition.CopyTraversalWork!.Visible&&!i.Definition.CopyTraversalWork.StopOnError&&i.FreeArgs==0&&
                text is "   " or "     " or "        ","Recursive progress indentation differs.");
            i.Output.Write(System.Text.Encoding.Latin1.GetBytes(text));return 0;
        });
        Register(b,DosLvo.Output,"Output",(_,i)=>{
            Require(i.Definition.CopyTraversalWork!.Visible&&i.FreeArgs==0,"Unexpected recursive output query.");return 0x140;
        });
        Register(b,DosLvo.Flush,"Flush",(s,i)=>{
            var r=Recursive(i);var l=i.CopyTraversalWorkLayout!;
            Require(i.Definition.CopyTraversalWork!.Visible&&s.D[1]==0x140&&i.FreeArgs==0&&r.Handles.Count==0&&
                r.CompletedFiles==0&&r.MatchIndex==(l.NameFlushes==0?2:3)&&l.NameFlushes<2,"Recursive name flush occurred after work.");
            l.NameFlushes++;return 1;
        });
        Register(b,DosLvo.VPrintf,"VPrintf",(s,i)=>{
            if(i.Definition.CopyTraversalWork!.Visible)
            {
                var l=i.CopyTraversalWorkLayout!;var r=Recursive(i);var format=Bus.CString(s.D[1]);
                Require(i.Definition.CopyTraversalWork.SingleFailure=="probe-dangling"&&s.D[2]==l.Workspace+2700&&i.FreeArgs==0,
                    "Visible dangling output storage/lifetime differs.");
                if(format=="Warning: Skipping dangling softlink %s -> %s\n")
                {
                    var link=Bus.Long(s.D[2]+4);
                    Require(r.MatchIndex==0&&r.LinkReads==1&&r.DeviceReleases==0&&Bus.CString(Bus.Long(s.D[2]))=="first"&&
                        Bus.CString(link)=="missing"&&Bus.OwnedAllocation(i,link,"Exec").Size==512&&Bus.Memory[link+511]==0,
                        "Dangling warning lost its live buffer, terminator or arguments.");
                    i.Output.Write(System.Text.Encoding.Latin1.GetBytes("Warning: Skipping dangling softlink first -> missing\n"));
                }
                else if(format==" not %s: ")
                {
                    Require(format==" not %s: "&&Bus.CString(Bus.Long(s.D[2]))=="read."&&r.MatchIndex==1&&r.DanglingWorkAttempts==1,
                        "Dangling source failure prefix differs.");
                    i.Output.Write(System.Text.Encoding.Latin1.GetBytes(" not read.: "));
                }
                else
                {
                    Require(!i.Definition.CopyTraversalWork.StopOnError&&format is "%s (Dir)" or "%s" or ".." or "%s\n",
                        "Unexpected recursive progress format.");
                    var text=format.Contains("%s")?format.Replace("%s",Bus.CString(Bus.Long(s.D[2]))):format;
                    i.Output.Write(System.Text.Encoding.Latin1.GetBytes(text));
                }
                i.IoError=901;return 0;
            }
            Require(i.Definition.CopyTraversalWork!.SingleFailure is "matchnext-error" or "matchnext-break" or "pending-error" or "pending-break" or "parent-zero" or "parent-error",
                "Unexpected recursive diagnostic.");
            Require(i.CopyTraversalWorkLayout!.Ends==1&&i.FreeArgs==0&&Bus.CString(s.D[1])=="%s - "&&
                Bus.CString(Bus.Long(s.D[2]))=="SYS:#?","Matcher diagnostic prefix/order differs.");
            i.Output.Write(System.Text.Encoding.Latin1.GetBytes("SYS:#? - "));
            i.IoError=901;return 0;
        });
        Register(b,DosLvo.PrintFault,"PrintFault",(s,i)=>{
            if(i.Definition.CopyTraversalWork!.Visible)
            {
                Require(i.Definition.CopyTraversalWork.SingleFailure=="probe-dangling"&&s.D[1]==901&&s.D[2]==0&&
                    i.FreeArgs==0&&Recursive(i).MatchIndex==1,"Visible source fault must use post-prefix IoErr.");
                i.Output.Write(System.Text.Encoding.Latin1.GetBytes("[DOS fault]\n"));i.IoError=900;return 1;
            }
            var error=RecursiveExpectedFault(i.Definition.CopyTraversalWork!.SingleFailure);
            Require(i.CopyTraversalWorkLayout!.Ends==1&&i.FreeArgs==0&&s.D[1]==(uint)error&&s.D[2]==0&&i.IoError==901,
                "Matcher fault lost saved error or cleanup ordering.");
            i.Output.Write(System.Text.Encoding.Latin1.GetBytes("[DOS fault]\n"));
            i.IoError=900;return 1;
        });
        Register(b,DosLvo.AllocDosObject,"AllocDosObject",(s,i)=>{
            Require(s.D[2]==0&&s.D[1] is 2 or 5,"Recursive DOS object type differs.");
            var value=Bus.Allocate(i,s.D[1]==5?32u:260u,s.D[1]==5?"RecursiveParser":"RecursiveFIB",true);
            if(s.D[1]==5)i.CopyTraversalWorkLayout!.Parser=value;
            return value;
        });
        Register(b,DosLvo.FreeDosObject,"FreeDosObject",(s,i)=>{
            Bus.Release(i,s.D[2],s.D[1]==5?"RecursiveParser":"RecursiveFIB");
            if(s.D[1]==5){Require(i.FreeArgs==1,"Parser freed before FreeArgs.");i.CopyTraversalWorkLayout!.Parser=0;}
            return 0;
        });
        Register(b,DosLvo.ReadArgs,"ReadArgs",(s,i)=>{
            var l=i.CopyTraversalWorkLayout!;l.Workspace=s.D[2];
            Require(s.D[3]==l.Parser&&Bus.OwnedAllocation(i,s.D[2],"Exec").Size==4772,"Recursive parser storage differs.");
            for(uint n=0;n<24;n++)Require(Bus.Long(s.D[2]+4*n)==0,"Recursive argument slot not cleared.");
            Bus.Long(s.D[2],l.Control+700);Bus.Long(s.D[2]+4,l.Control+160);
            Bus.Long(l.Control+760,1);Bus.Long(s.D[2]+12,l.Control+760);
            Bus.Long(s.D[2]+16,uint.MaxValue); // ALL
            if(!i.Definition.CopyTraversalWork!.Visible)Bus.Long(s.D[2]+44,uint.MaxValue); // QUIET
            if(i.Definition.CopyTraversalWork!.StopOnError)Bus.Long(s.D[2]+52,uint.MaxValue); // ERRWARN
            if(i.Definition.CopyTraversalWork!.SingleFailure=="move")Bus.Long(s.D[2]+60,uint.MaxValue);
            if(i.Definition.CopyTraversalWork!.SingleFailure=="clone")Bus.Long(s.D[2]+24,uint.MaxValue);
            if(i.Definition.CopyTraversalWork!.SingleFailure=="delete"){Bus.Long(s.D[2]+4,0);Bus.Long(s.D[2]+64,uint.MaxValue);}
            if(i.Definition.CopyTraversalWork!.SingleFailure=="nopro")Bus.Long(s.D[2]+32,uint.MaxValue);
            if(i.Definition.CopyTraversalWork!.SingleFailure=="prox")Bus.Long(s.D[2]+36,uint.MaxValue);
            i.Reads++;return l.Parser;
        });
        Register(b,DosLvo.FreeArgs,"FreeArgs",(s,i)=>{Require(s.D[1]==i.CopyTraversalWorkLayout!.Parser,"Recursive FreeArgs differs.");i.FreeArgs++;return 0;});
        Register(b,DosLvo.ParsePattern,"ParsePattern",(s,i)=>{Require(Bus.CString(s.D[1])=="RAM:","Recursive target differs.");return 0;});
        Register(b,DosLvo.PathPart,"PathPart",(s,i)=>s.D[1]+4);
        Register(b,DosLvo.IsFileSystem,"IsFileSystem",(s,i)=>1);
        Register(b,DosLvo.Examine,"Examine",(s,i)=>{
            Require(Recursive(i).FileSystem.Name(s.D[1])=="RAM:","Unexpected recursive destination examination.");
            Bus.Long(s.D[2]+FileInfoBlock.DirEntryTypeOffset,2);return 1;
        });
        Register(b,DosLvo.SetProtection,"SetProtection",(s,i)=>{
            var r=Recursive(i);
            Require(i.Definition.CopyTraversalWork!.SingleFailure!="nopro","NOPRO must suppress protection updates.");
            if(i.Definition.CopyTraversalWork!.SingleFailure=="delete")
            {
                Require(Bus.CString(s.D[1])==""&&s.D[2]==((uint)(FileProtection.Pure|FileProtection.Script)|(uint)r.Trace.Select((record,index)=>(record,index)).Where(x=>!x.record.Directory).ElementAt(r.MetadataPaths.Count).index),"Original DELETE metadata tail differs.");r.MetadataPaths.Add("");return 1;
            }
            var path=r.FileSystem.Resolve(Bus.CString(s.D[1]));
            var metadataRecords=r.Trace.Select((record,index)=>(record,index)).Where(x=>x.record.Path!="SYS:"&&(!x.record.Directory||x.record.Exited)).ToArray();
            var expected=metadataRecords.Select(x=>x.record.Destination).ToArray();
            var sourceBits=metadataRecords.Select(x=>(uint)x.index).ToArray();
            Require(r.MetadataPaths.Count<expected.Length&&path==expected[r.MetadataPaths.Count]&&s.D[2]==((uint)(FileProtection.Pure|FileProtection.Script)|sourceBits[r.MetadataPaths.Count])&&i.FreeArgs==0,
                "Recursive metadata target/order/protection differs.");
            Require(r.CompletedFiles==(i.Definition.CopyTraversalWork!.SingleFailure=="probe-dangling"?1:path.StartsWith("RAM:first")?1:2),"Metadata preceded completed child transfer.");
            if(!path.EndsWith("/child"))Require(r.FileSystem.Name(r.FileSystem.CurrentDirectory)==(path=="RAM:first/deep"?"RAM:first":"RAM:"),"Directory metadata is not relative to its parent.");
            r.MetadataPaths.Add(path);return 1;
        });
        Register(b,DosLvo.SetFileDate,"SetFileDate",(s,i)=>{
            var r=Recursive(i);var records=r.Trace.Select((record,index)=>(record,index)).Where(x=>x.record.Path!="SYS:"&&(!x.record.Directory||x.record.Exited)).Select(x=>x.index).ToArray();
            Require(i.Definition.CopyTraversalWork!.SingleFailure=="clone"&&r.Dates<records.Length&&r.MetadataPaths.Count==r.Dates+1&&r.Comments==r.Dates&&r.FileSystem.Resolve(Bus.CString(s.D[1]))==r.MetadataPaths[^1]&&Bus.Long(s.D[2])==100+records[r.Dates],"Recursive saved date/order differs.");r.Dates++;if(i.Definition.CopyTraversalWork!.FailFirst){i.IoError=223;return 0;}return 1;
        });
        Register(b,DosLvo.SetComment,"SetComment",(s,i)=>{
            var r=Recursive(i);var records=r.Trace.Select((record,index)=>(record,index)).Where(x=>x.record.Path!="SYS:"&&(!x.record.Directory||x.record.Exited)).Select(x=>x.index).ToArray();
            Require(r.Comments<records.Length&&r.Dates==r.Comments+1&&r.FileSystem.Resolve(Bus.CString(s.D[1]))==r.MetadataPaths[^1]&&Bus.CString(s.D[2])=="record-"+records[r.Comments],"Recursive saved comment/order differs.");r.Comments++;if(i.Definition.CopyTraversalWork!.FailFirst){i.IoError=223;return 0;}return 1;
        });
        Register(b,DosLvo.IoErr,"IoErr",(_,i)=>unchecked((uint)i.IoError));
        Register(b,DosLvo.SetIoErr,"SetIoErr",(s,i)=>{
            if(i.IoError==900&&i.Definition.CopyTraversalWork!.SingleFailure is "matchnext-error" or "matchnext-break" or "pending-error" or "pending-break" or "parent-zero" or "parent-error")
                Require(s.D[1]==(uint)RecursiveExpectedFault(i.Definition.CopyTraversalWork.SingleFailure),
                    "Matcher error was not restored after diagnostics.");
            i.IoError=unchecked((int)s.D[1]);return 0;
        });
    }
}
