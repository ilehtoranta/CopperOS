using Amiga;
using CopperSharp.Compiler;
using static CopperOS.Desktop.DesktopMemory;

namespace CopperOS.Desktop;

public static class NativeDesktopProcess
{
    internal const string PortName = "CopperOS.Desktop";
    // Private app state; public OS structures below use the SDK ABI offsets.
    internal const int Dos = 0, Graphics = 4, Intuition = 8, Port = 12,
        Screen = 16, Window = 20, Reserve = 24, Selection = 28, Status = 32,
        About = 36, Pressed = 40, Dirty = 44, Claimed = 48, Quit = 52,
        Files = 164, View = 168, FileError = 172, PathEditing = 176, PathLength = 180,
        Document = 208, FontState = 212, EditorError = 216, EditorPrompt = 220,
        PendingAction = 224, PromptOrigin = 228, PendingName = 232, PendingDirectory = 236, EditorOpener = 240, SaveState = 244,
        Busy = 248, Recovery = 252;

    [M68kExport("copperos.desktop.process")]
    public static uint Entry()
    {
        var s = Allocate(DesktopProfile.StateBytes);
        if (s == 0) return 20;
        var task = Exec.FindTask(0).Raw;
        var previousData = L(task, ExecLayout.Task.UserData);
        L(task, ExecLayout.Task.UserData, s);
        var ok = Start(s, previousData);
        if (ok) Events(s);
        Stop(s);
        L(task, ExecLayout.Task.UserData, previousData);
        Exec.FreeMem(s, DesktopProfile.StateBytes);
        return ok ? 0u : 20u;
    }

    private static bool Start(uint s, uint segment)
    {
        L(s, 56, 1);
        L(s, Dos, Exec.OpenLibraryRaw("dos.library", 37).Raw);
        if (L(s, Dos) == 0) return false;
        L(s, 56, 2); L(s, Reserve, Allocate(DesktopProfile.CleanupReserveBytes));
        if (L(s, Reserve) == 0) return false;
        L(s, 56, 3); var port = Exec.CreateMsgPort().Raw; L(s, Port, port);
        if (port == 0) return false;
        L(port, 10, CString.ToUInt32(PortName));
        Exec.Forbid();
        var exists = Exec.FindPort(PortName).IsNotNull;
        if (!exists) { Exec.AddPort(APTR.FromPointer(port)); L(s, Claimed, 1); }
        Exec.Permit();
        if (exists) return false;
        L(s, 56, 4);
        if (segment == 0) return false;
        CallWrapper(s + 144);
        L(s, 56, 5); if (DesktopNative.Provider(s + 144, (segment << 2) + 4 + DesktopProfile.TextEntry, ExecBase()) == 0) return false;
        L(s, 56, 6); L(s, Intuition, Exec.OpenLibraryRaw("intuition.library", 40).Raw);
        if (L(s, Intuition) == 0) return false;
        L(s, 56, 7); L(s, Graphics, Exec.OpenLibraryRaw("graphics.library", 40).Raw);
        if (L(s, Graphics) == 0) return false;
        var spec = s + 64;
        H(spec, 4, 640); H(spec, 6, 256); H(spec, 8, 2);
        B(spec, 10, 1); B(spec, 11, 2); H(spec, 12, 0x8000); H(spec, 14, 15);
        L(spec, 20, CString.ToUInt32("CopperOS"));
        L(s, 56, 8); var screen = DesktopIntuition.Screen(L(s, Intuition), spec); L(s, Screen, screen);
        if (screen == 0) return false;
        spec = s + 96;
        H(spec, 0, 10); H(spec, 2, 12); H(spec, 4, 620); H(spec, 6, 232);
        B(spec, 8, 1); B(spec, 9, 2);
        L(spec, 14, (uint)(WindowFlags.SimpleRefresh | WindowFlags.CloseGadget | WindowFlags.Activate));
        L(spec, 26, CString.ToUInt32("CopperOS Desktop")); L(spec, 30, screen); H(spec, 46, 15);
        L(s, 56, 9); var window = DesktopIntuition.Window(L(s, Intuition), spec); L(s, Window, window);
        if (window == 0) return false;
        L(window, 86, port);
        L(s, 56, 10); if (DesktopIntuition.Modify(L(s, Intuition), window, (uint)(IDCMPFlags.RefreshWindow | IDCMPFlags.CloseWindow |
            IDCMPFlags.VanillaKey | IDCMPFlags.RawKey | IDCMPFlags.MouseButtons | IDCMPFlags.ActiveWindow | IDCMPFlags.InactiveWindow)) == 0) return false;
        L(s, Pressed, uint.MaxValue);
        HomeView.Draw(s);
        return true;
    }

    private static void Events(uint s)
    {
        var port = L(s, Port);
        var signals = (1u << B(port, 15)) | 0x1000; // SIGBREAKF_CTRL_C
        for (;;)
        {
            var message = Exec.GetMsg(APTR.FromPointer(port)).Raw;
            while (message != 0)
            {
                // Quit is final: keep replying to queued messages, but do not
                // accept fresh edits after Save-and-close has committed.
                var kind = L(s, Quit) == 0 ? L(message, 20) : 0u;
                var code = H(message, 24); var qualifier = H(message, 26);
                var x = (short)H(message, 32); var y = (short)H(message, 34);
                // No message storage may remain borrowed across a DOS call or
                // message drain. Everything used below is copied first.
                Exec.ReplyMsg(APTR.FromPointer(message));
                if (kind == (uint)IDCMPFlags.RefreshWindow)
                {
                    DesktopIntuition.Begin(L(s, Intuition), L(s, Window));
                    Draw(s);
                    DesktopIntuition.End(L(s, Intuition), L(s, Window), 1);
                }
                else if (kind == (uint)IDCMPFlags.CloseWindow) EditorView.RequestQuit(s);
                else if (kind == (uint)IDCMPFlags.VanillaKey)
                { if (L(s, View) == 3) RecoveryView.Key(s, code, qualifier); else if (L(s, View) == 2) EditorView.Key(s, code, qualifier); else if (L(s, View) == 1) FilesView.Key(s, code, qualifier); else HomeView.Key(s, code, qualifier); }
                else if (kind == (uint)IDCMPFlags.RawKey && code < 128)
                { if (L(s, View) == 3) RecoveryView.Raw(s, code); else if (L(s, View) == 2) EditorView.Raw(s, code, qualifier); else if (L(s, View) == 1) FilesView.Raw(s, code); else HomeView.Raw(s, code); }
                else if (kind == (uint)IDCMPFlags.MouseButtons)
                { if (L(s, View) == 3) RecoveryView.Mouse(s, code, x, y); else if (L(s, View) == 2) EditorView.Mouse(s, code, x, y); else if (L(s, View) == 1) FilesView.Mouse(s, code, x, y); else HomeView.Mouse(s, code, x, y); }
                else if (kind == (uint)IDCMPFlags.InactiveWindow) L(s, Pressed, uint.MaxValue);
                message = Exec.GetMsg(APTR.FromPointer(port)).Raw;
            }
            if (L(s, Quit) != 0) return;
            if (L(s, Dirty) != 0)
            {
                var full = L(s, Dirty) == 2; L(s, Dirty, 0);
                if (full) Draw(s);
                else if (L(s, View) == 3) RecoveryView.Draw(s);
                else if (L(s, View) == 2) EditorView.DrawChanges(s);
                else if (L(s, View) == 1) FilesView.DrawChanges(s);
                else HomeView.DrawChanges(s);
            }
            if ((Exec.Wait(signals) & 0x1000) != 0) EditorView.RequestQuit(s);
        }
    }

    internal static void Draw(uint s)
    { if (L(s, View) == 3) RecoveryView.Draw(s); else if (L(s, View) == 2) EditorView.Draw(s); else if (L(s, View) == 1) FilesView.Draw(s); else HomeView.Draw(s); }

    // Several view/input messages can arrive in one drain. A later selection
    // change must not discard the full repaint required by a view transition.
    internal static void Changed(uint s)
    { if (L(s, Dirty) != 2) L(s, Dirty, 1); }

    private static void Stop(uint s)
    {
        var reserve = L(s, Reserve);
        if (reserve != 0) Exec.FreeMem(reserve, DesktopProfile.CleanupReserveBytes);
        EditorView.Release(s);
        RecoveryView.Release(s);
        FilesView.Release(s);
        var lib = L(s, Intuition); var window = L(s, Window); var screen = L(s, Screen); var port = L(s, Port);
        if (window != 0)
        {
            Exec.Forbid();
            var message = Exec.GetMsg(APTR.FromPointer(port)).Raw;
            while (message != 0) { Exec.ReplyMsg(APTR.FromPointer(message)); message = Exec.GetMsg(APTR.FromPointer(port)).Raw; }
            L(window, 86, 0); DesktopIntuition.Modify(lib, window, 0);
            Exec.Permit();
            // The bounded provider's void CloseWindow can retain a Layer on
            // allocation refusal. Keep every owner until the public list drops it.
            while (Contains(screen, window)) { DesktopIntuition.CloseWindow(lib, window); if (Contains(screen, window)) DesktopGraphics.WaitFrame(L(s, Graphics)); }
        }
        if (screen != 0)
            while (DesktopIntuition.CloseScreen(lib, screen) == 0) DesktopGraphics.WaitFrame(L(s, Graphics));
        if (port != 0)
        {
            if (L(s, Claimed) != 0) Exec.RemPort(APTR.FromPointer(port));
            Exec.DeleteMsgPort(APTR.FromPointer(port));
        }
        if (L(s, Graphics) != 0) Exec.CloseLibrary(APTR.FromPointer(L(s, Graphics)));
        if (lib != 0)
        {
            Exec.CloseLibrary(APTR.FromPointer(lib));

        }
        if (L(s, Dos) != 0) Exec.CloseLibrary(APTR.FromPointer(L(s, Dos)));
    }

    private static bool Contains(uint screen, uint window)
    {
        var node = L(screen, 4);
        while (node != 0) { if (node == window) return true; node = L(node, 0); }
        return false;
    }
}
