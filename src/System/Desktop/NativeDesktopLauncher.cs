using Amiga;
using CopperSharp.Compiler;
using static CopperOS.Desktop.DesktopMemory;

namespace CopperOS.Desktop;

/// <summary>Foreground command waits for its separate DOS Process to exit.</summary>
public static class NativeDesktopLauncher
{
    private static uint callbacks;

    public static uint Entry()
    {
        var dos = Exec.OpenLibraryRaw("dos.library", 37).Raw;
        if (dos == 0) return 20;
        // Load large code before adding the child's stack and screen allocations.
        // The code remains registered even when later startup fails.
        var provider = ResidentModule(dos, CString.ToUInt32("CopperOS.GUI.Text.v8"), CString.ToUInt32("L:CopperGUI"));
        if (provider == 0) { Error(dos); Exec.CloseLibrary(APTR.FromPointer(dos)); return 20; }
        if (!EnsureCallbacks()) { Error(dos); Exec.CloseLibrary(APTR.FromPointer(dos)); return 20; }
        var port = Exec.CreateMsgPort().Raw;
        var state = Allocate(128);
        uint result = 20;
        if (port != 0 && state != 0)
        {
            L(state, 14, port); H(state, 18, 24);
            var tags = state + 24;
            Tag(tags, 0, DosNewProcessTag.Entry, callbacks + 16);
            Tag(tags, 1, DosNewProcessTag.StackSize, DesktopProfile.StackBytes);
            Tag(tags, 2, DosNewProcessTag.Name, CString.ToUInt32("CopperOS Desktop"));
            Tag(tags, 3, DosNewProcessTag.Input, DesktopDos.Input(dos));
            Tag(tags, 4, DosNewProcessTag.Output, DesktopDos.Output(dos));
            Tag(tags, 5, DosNewProcessTag.CloseInput, 0);
            Tag(tags, 6, DosNewProcessTag.CloseOutput, 0);
            Tag(tags, 7, DosNewProcessTag.ExitCode, callbacks);
            Tag(tags, 8, DosNewProcessTag.ExitData, state);
            Tag(tags, 9, DosNewProcessTag.UserData, provider);
            if (DesktopDos.Create(dos, tags) != 0)
            {
                while (Exec.GetMsg(APTR.FromPointer(port)).IsNull)
                    Exec.Wait(1u << B(port, 15));
                result = L(state, 20);
            }
        }
        if (result != 0) Error(dos);
        if (state != 0) Exec.FreeMem(state, 128);
        if (port != 0) Exec.DeleteMsgPort(APTR.FromPointer(port));
        Exec.CloseLibrary(APTR.FromPointer(dos));
        return result;
    }

    private static void Tag(uint tags, int index, DosNewProcessTag tag, uint value)
    { L(tags, index * 8, (uint)tag); L(tags, index * 8 + 4, value); }

    private static bool EnsureCallbacks()
    {
        Exec.Forbid();
        if (callbacks == 0)
        {
            var code = Allocate(32);
            if (code != 0)
            {
                CallbackWrapper(code, APTR.ExportAddress("copperos.desktop.exited").Raw);
                CallbackWrapper(code + 16, APTR.ExportAddress("copperos.desktop.process").Raw);
                callbacks = code;
            }
        }
        var ready = callbacks != 0; Exec.Permit(); return ready;
    }

    // The containing module is a reset-lived DOS system resident: this callback
    // can still be unwinding when the awakened parent returns to its CLI.
    [M68kExport("copperos.desktop.exited")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint Exited([M68kRegister(M68kRegister.D0)] uint result,
        [M68kRegister(M68kRegister.D1)] uint data)
    {
        L(data, 20, result);
        Exec.PutMsg(APTR.FromPointer(L(data, 14)), APTR.FromPointer(data));
        return result;
    }
}
