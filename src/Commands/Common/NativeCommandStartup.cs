using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Original DOS command startup/exit ownership. Compile command entries with
/// the resident runtime profile so library-base properties are invocation-local.
/// No incoming A6 value is assumed to be a library base.
/// </summary>
public static class NativeCommandStartup
{
    /// <summary>
    /// Receives the Workbench message before opening libraries or using DOS.
    /// A CLI process has no Workbench startup message. The caller retains the
    /// returned message until Finish and must not reply to it independently.
    /// </summary>
    public static APTR ReceiveWorkbenchMessage()
    {
        var process = Exec.FindTask(CString.FromPointer(0));
        if (process.IsNull ||
            APTR.ReadUInt32(process, DosLayout.Process.CommandLineInterface) != 0)
            return APTR.Null;

        var port = APTR.FromPointer(process.Raw + (uint)DosLayout.Process.MessagePort);
        Exec.WaitPort(port);
        return Exec.GetMsg(port);
    }

    /// <summary>Opens real dos.library and records only this invocation's base.</summary>
    public static bool OpenDos(uint minimumVersion)
    {
        var dos = Exec.OpenLibraryRaw("dos.library", minimumVersion);
        if (dos.IsNull) return false;
        DOS.DOSLibraryBase = dos;
        return true;
    }

    /// <summary>
    /// Call only after releasing command-owned resources. Restore the selected
    /// DOS error after cleanup, close the owned library, then reply to Workbench
    /// under Forbid so it cannot unload this image before the entry returns.
    /// No matching Permit is made inside the command: original startup hands
    /// that final process-exit transition back to the loader.
    /// </summary>
    public static int Finish(int result, int ioError, APTR workbenchMessage)
    {
        var dos = DOS.DOSLibraryBase;
        if (dos.IsNotNull)
        {
            DOS.SetIoErr((DOS.Error)ioError);
            Exec.CloseLibrary(dos);
            DOS.DOSLibraryBase = APTR.Null;
        }

        if (workbenchMessage.IsNotNull)
        {
            Exec.Forbid();
            Exec.ReplyMsg(workbenchMessage);
        }

        return result;
    }
}
