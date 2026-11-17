using Amiga;
using static CopperOS.Desktop.DesktopMemory;
using static CopperOS.Desktop.NativeDesktopProcess;

namespace CopperOS.Desktop;

/// <summary>One task and one port, with bounded drains between synchronous DOS calls.</summary>
internal static class DesktopBusy
{
    internal static bool Begin(uint s, uint operation)
    {
        if (L(s, Busy) != 0) return false;
        L(s, Busy, operation); L(s, Pressed, uint.MaxValue);
        Paint(s); Poll(s); return true;
    }

    internal static void End(uint s, bool owner)
    {
        if (!owner) return;
        Poll(s);
        var close = (L(s, Busy) & 256) != 0;
        L(s, Busy, 0); L(s, Pressed, uint.MaxValue); L(s, Dirty, 2);
        if (close && L(s, Quit) == 0) EditorView.BusyClose(s);
    }

    internal static void Poll(uint s)
    {
        if (L(s, Busy) == 0) return;
        // The provider has bounded queues. Bound our drain too, so continuing
        // input cannot starve the disk operation. Input during busy is replied
        // and ignored, never replayed against a later document or directory.
        for (var i = 0; i < 32; i++)
        {
            var message = Exec.GetMsg(APTR.FromPointer(L(s, Port))).Raw;
            if (message == 0) break;
            var kind = L(message, 20); var code = H(message, 24);
            Exec.ReplyMsg(APTR.FromPointer(message));
            if (kind == (uint)IDCMPFlags.CloseWindow || kind == (uint)IDCMPFlags.VanillaKey && code == 3)
                L(s, Busy, L(s, Busy) | 256);
            else if (kind == (uint)IDCMPFlags.RefreshWindow)
            {
                DesktopIntuition.Begin(L(s, NativeDesktopProcess.Intuition), L(s, NativeDesktopProcess.Window));
                Draw(s); Paint(s);
                DesktopIntuition.End(L(s, NativeDesktopProcess.Intuition), L(s, NativeDesktopProcess.Window), 1);
            }
        }
    }

    private static void Paint(uint s)
    {
        // Use the normal ROM font even when opening the first Editor document.
        EditorFont.Bind(s, false);
        HomeView.Rect(s, 16, 198, 603, 224, 0);
        var operation = L(s, Busy) & 255;
        if (operation == 1) HomeView.Text(s, 20, 209, 1, "Saving document...", 18);
        else if (operation == 2) HomeView.Text(s, 20, 209, 1, "Reading directory...", 20);
        else if (operation == 3) HomeView.Text(s, 20, 209, 1, "Opening document...", 19);
        else HomeView.Text(s, 20, 209, 1, "Inspecting recovery...", 22);
        HomeView.Text(s, 20, 222, 1, "Edits paused. Close waits for disk work to finish.", 50);
    }
}
