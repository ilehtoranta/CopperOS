using Exec = Amiga.Exec;
using CString = Amiga.CString;
using static CopperOS.Desktop.DesktopMemory;
using static CopperOS.Desktop.NativeDesktopProcess;

namespace CopperOS.Desktop;

internal static class EditorFont
{
    // Public 52-byte TextFont, saved original pointer, row data and CharLoc.
    internal static bool Ensure(uint s)
    {
        if (L(s, FontState) != 0) return true;
        var p = Allocate(DesktopProfile.EditorFontBytes); if (p == 0) return false;
        L(p, 10, CString.ToUInt32("CopperOS.Editor8")); B(p, 8, 12);
        H(p, 20, 8); H(p, 24, 8); H(p, 26, 6); H(p, 28, 1);
        B(p, 32, 32); B(p, 33, 255); L(p, 34, p + 64); H(p, 38, 226); L(p, 40, p + 1872);
        for (var i = 0; i < 225; i++)
        {
            L(p, 1872 + i * 4, ((uint)i * 8 << 16) | 8);
            for (var row = 0; row < 8; row++) B(p, 64 + row * 226 + i, EditorGlyphs.Row(i == 224 ? 63u : (uint)i + 32, row));
        }
        var rp = L(L(s, Window), 50); var old = L(rp, 52); L(p, 52, old);
        DesktopGraphics.Font(L(s, Graphics), rp, p);
        var ok = L(rp, 52) == p && H(rp, 58) == 8 && H(rp, 60) == 8 && H(rp, 62) == 6;
        DesktopGraphics.Font(L(s, Graphics), rp, old);
        if (!ok) { Exec.FreeMem(p, DesktopProfile.EditorFontBytes); return false; }
        L(s, FontState, p); return true;
    }

    internal static void Bind(uint s, bool editor)
    {
        var p = L(s, FontState);
        if (p == 0) return;
        DesktopGraphics.Font(L(s, Graphics), L(L(s, Window), 50), editor ? p : L(p, 52));
    }

    internal static void Release(uint s)
    {
        var p = L(s, FontState); if (p == 0) return;
        if (L(s, Window) != 0 && L(L(L(s, Window), 50), 52) == p) Bind(s, false);
        Exec.FreeMem(p, DesktopProfile.EditorFontBytes); L(s, FontState, 0);
    }
}
