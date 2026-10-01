using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;
namespace CopperOS.Commands.NativeExecution;
internal sealed partial class ProbeFixture
{
    private void RegisterDirectoryCommandOutput(uint b)
    {
        Register(b,DosLvo.PutStr,"PutStr",(s,i)=>{
            Require(i.Definition.CopyTraversalWork!.Visible&&i.FreeArgs==0,"Unexpected/late indentation.");
            var text=Bus.CString(s.D[1]);Require(text is "   " or "     ","Indentation differs.");
            i.Output.Write(Encoding.Latin1.GetBytes(text));return 0;
        });
        Register(b,DosLvo.VPrintf,"VPrintf",(s,i)=>{
            var l=i.CopyTraversalWorkLayout!;var format=Bus.CString(s.D[1]);
            Require(i.Definition.CopyTraversalWork!.Visible&&i.FreeArgs==0&&s.D[2]==l.Workspace+2700&&
                format is "%s (Dir)" or "%s" or ".." or "%s\n" or "   [created]\n" or "No file was processed.\n" or " not %s: ","Verbose format/storage differs.");
            if(format=="No file was processed.\n") Require(l.Completed==0&&l.Ends==1&&l.DestinationUnlocks==2&&i.FreeArgs==0,
                "Nothing-processed notice must follow matcher and target cleanup but precede parser release.");
            var text=format.Contains("%s")?format.Replace("%s",Bus.CString(Bus.Long(s.D[2]))):format;
            i.Output.Write(Encoding.Latin1.GetBytes(text));return (uint)text.Length;
        });
        Register(b,DosLvo.PrintFault,"PrintFault",(s,i)=>{
            var p=i.Definition.CopyTraversalWork!;var l=i.CopyTraversalWorkLayout!;
            Require(p.Visible&&p.HasTransferFailure&&s.D[1]==221&&s.D[2]==0&&l.Deletes==1&&l.Completed==p.FailureIndex+1&&i.FreeArgs==0,"Transfer diagnostic error/order differs.");
            i.Output.Write(Encoding.Latin1.GetBytes("[DOS fault]\n"));return 1;
        });
        Register(b,DosLvo.Output,"Output",(_,i)=>{Require(i.Definition.CopyTraversalWork!.Visible,"Unexpected output handle query.");return 0x140;});
        Register(b,DosLvo.Flush,"Flush",(s,i)=>{
            var l=i.CopyTraversalWorkLayout!;
            Require(s.D[1]==0x140&&l.Opens==l.Completed*2&&l.Closes==l.Opens&&i.FreeArgs==0,"Name must be flushed before file work.");
            l.NameFlushes++;return 1;
        });
    }
}
