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
        // FindTask(NULL) always returns the current task. Command entries run
        // in a DOS Process, so there is no missing-task path to handle here.
        var processRaw = Exec.FindTask(CString.FromPointer(0)).Raw;
        if (APTR.ReadUInt32(APTR.FromPointer(processRaw),
                DosLayout.Process.CommandLineInterface) != 0)
            return APTR.Null;

        var port = APTR.FromPointer(processRaw + (uint)DosLayout.Process.MessagePort);
        Exec.WaitPort(port);
        return Exec.GetMsg(port);
    }

    /// <summary>Opens real dos.library and records only this invocation's base.</summary>
    public static bool OpenDos(uint minimumVersion)
    {
        var dosRaw = Exec.OpenLibraryRaw("dos.library", minimumVersion).Raw;
        if (dosRaw == 0) return false;
        DOS.DOSLibraryBase = APTR.FromPointer(dosRaw);
        return true;
    }

    /// <summary>
    /// Terminal cleanup: the caller must return directly to the entry wrapper.
    /// Call only after releasing command-owned resources. Restore the selected
    /// DOS error after cleanup, close the owned library, then reply to Workbench
    /// under Forbid so it cannot unload this image before the entry returns.
    /// No matching Permit is made inside the command: original startup hands
    /// that final process-exit transition back to the loader.
    /// </summary>
    public static int Finish(int result, int ioError, APTR workbenchMessage)
    {
        var dosRaw = DOS.DOSLibraryBase.Raw;
        if (dosRaw != 0)
        {
            DOS.SetIoErr((DOS.Error)ioError);
            Exec.CloseLibrary(APTR.FromPointer(dosRaw));
            // The resident entry wrapper discards this invocation's base
            // slots on return. The closed DOS base is not used again.
        }

        if (workbenchMessage.IsNotNull)
        {
            Exec.Forbid();
            Exec.ReplyMsg(workbenchMessage);
        }

        return result;
    }

    /// <summary>
    /// Terminal cleanup for entries that never open DOS. Reply to Workbench
    /// under Forbid after all command-owned resources have been released.
    /// The caller must return directly to the entry wrapper.
    /// </summary>
    public static int FinishWithoutDos(int result, APTR workbenchMessage)
    {
        if (workbenchMessage.IsNotNull)
        {
            Exec.Forbid();
            Exec.ReplyMsg(workbenchMessage);
        }

        return result;
    }
}
