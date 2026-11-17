using Amiga;
using static CopperOS.Desktop.DesktopMemory;
using static CopperOS.Desktop.NativeDesktopProcess;

namespace CopperOS.Desktop;

/// <summary>Small fixed-layout four-colour Home; all drawing uses the Window RastPort.</summary>
internal static class HomeView
{
    internal static void Draw(uint s)
    {
        Rect(s, 4, 14, 615, 227, 0);
        Rect(s, 12, 20, 607, 43, 3);
        Text(s, 20, 35, 2, "CopperOS", 8); Text(s, 556, 35, 2, "Home", 4);
        for (var i = 0u; i < 3; i++) Tile(s, i);
        StatusPanel(s);
        Rect(s, 16, 193, 603, 193, 1);
        Text(s, 20, 216, 1, "512 KiB ROM | 1 MiB A500", 24);
        Rect(s, 520, 201, 604, 222, 1); Rect(s, 522, 203, 602, 220, 2);
        Text(s, 534, 216, 1, "Console", 7);
        L(s, 156, L(s, Selection)); L(s, 160, EffectiveStatus(s));
    }

    internal static void DrawChanges(uint s)
    {
        var previous = L(s, 156); var current = L(s, Selection);
        if (previous != current)
        { Tile(s, previous); Tile(s, current); L(s, 156, current); }
        var status = EffectiveStatus(s);
        if (L(s, 160) != status) { StatusPanel(s); L(s, 160, status); }
    }

    private static uint EffectiveStatus(uint s) => L(s, About) != 0 ? 3u : L(s, Status) < 3 ? L(s, Status) : 0;

    private static void Tile(uint s, uint i)
    {
            var x = 16 + (int)i * 200;
            var selected = i == L(s, Selection);
            Rect(s, x, 64, x + 183, 139, 1);
            Rect(s, x + 2, 66, x + 181, 137, selected ? 3u : 2u);
            // Original compact folder / page / information glyphs.
            Rect(s, x + 12, 83, x + 35, 105, 1);
            Rect(s, x + 15, 86, x + 32, 102, 2);
            if (i == 0) Rect(s, x + 12, 79, x + 25, 86, 1);
            else if (i == 1)
            { Rect(s, x + 18, 90, x + 29, 91, 1); Rect(s, x + 18, 96, x + 29, 97, 1); }
            else Text(s, x + 21, 100, 1, "i", 1);
            var ink = selected ? 2u : 1u;
            if (i == 0) Text(s, x + 48, 95, ink, "Files", 5);
            else if (i == 1) Text(s, x + 48, 95, ink, "Editor", 6);
            else Text(s, x + 48, 95, ink, "About", 5);
            if (i == 0) Text(s, x + 48, 118, ink, "Browse disk", 11);
            else if (i == 1) Text(s, x + 48, 118, ink, "Write text", 10);
            else Text(s, x + 48, 118, ink, "System info", 11);
    }

    private static void StatusPanel(uint s)
    {
        Rect(s, 16, 150, 603, 188, 0);
        if (L(s, About) != 0)
        {
            Text(s, 20, 165, 1, "CopperOS Home - native desktop preview", 38);
            Text(s, 20, 182, 1, "CopperStart / 68000 / PAL / four colours", 40);
        }
        else if (L(s, Status) == 1)
        {
            Text(s, 20, 165, 1, "Files could not open. The Home view is ready.", 45);
            Text(s, 20, 182, 1, "Try Files again or use the console.", 35);
        }
        else if (L(s, Status) == 2)
        {
            Text(s, 20, 165, 1, "Editor could not open; your view is retained.", 45);
            Text(s, 20, 182, 1, "Try again after freeing memory.", 31);
        }
        else
        {
            Text(s, 20, 165, 1, "Welcome. Choose Files, Editor or About.", 39);
            Text(s, 20, 182, 1, "Tab / arrows select; Enter opens; Esc exits.", 44);
        }
    }

    internal static void Rect(uint s, int x0, int y0, int x1, int y1, uint pen)
    {
        var gfx = L(s, NativeDesktopProcess.Graphics); var rp = L(L(s, NativeDesktopProcess.Window), 50);
        // One bounded graphics operation owns clipping and blitter completion.
        if (y0 > y1) return;
        DesktopGraphics.Pen(gfx, rp, pen);
        DesktopGraphics.Rect(gfx, rp, x0, y0, x1, y1);
    }

    internal static void Text(uint s, int x, int y, uint pen, CString text, int count)
        => TextRaw(s, x, y, pen, CString.ToUInt32(text), count);

    internal static void TextRaw(uint s, int x, int y, uint pen, uint text, int count)
    {
        var gfx = L(s, NativeDesktopProcess.Graphics); var rp = L(L(s, NativeDesktopProcess.Window), 50);
        DesktopGraphics.Mode(gfx, rp, 0); DesktopGraphics.Pen(gfx, rp, pen);
        DesktopGraphics.Move(gfx, rp, x, y); DesktopGraphics.Text(gfx, text, rp, count);
    }

    internal static void Key(uint s, uint code, uint qualifier)
    {
        if ((qualifier & 0x200) != 0 && code != 9) return;
        if (code == 3) EditorView.RequestQuit(s);
        else if (code == 27)
        { if (L(s, About) != 0) { L(s, About, 0); Changed(s); } else EditorView.RequestQuit(s); }
        else if (code == 9) Select(s, (qualifier & 3) != 0 ? -1 : 1);
        else if (code == 13 || code == 32) Activate(s, L(s, Selection), code);
        else if (code == 'f' || code == 'F') Activate(s, 0, code);
        else if (code == 'e' || code == 'E') Activate(s, 1, code);
        else if (code == 'a' || code == 'A') Activate(s, 2, code);
        else if (code == 'h' || code == 'H') { L(s, About, 0); L(s, Status, 0); Changed(s); }
    }

    internal static void Raw(uint s, uint code)
    {
        if (code == 0x4F || code == 0x4C) Select(s, -1);
        else if (code == 0x4E || code == 0x4D) Select(s, 1);
    }

    private static void Select(uint s, int delta)
    { L(s, Selection, (uint)(((int)L(s, Selection) + delta + 3) % 3)); Changed(s); }

    internal static void Mouse(uint s, uint code, int x, int y)
    {
        var hit = Hit(x, y);
        if (code == 0x68)
        {
            L(s, Pressed, hit);
            if (hit < 3) { L(s, Selection, hit); Changed(s); }
        }
        else if (code == 0xE8)
        {
            if (hit == L(s, Pressed))
            { if (hit < 3) Activate(s, hit); else if (hit == 3) EditorView.RequestQuit(s); }
            L(s, Pressed, uint.MaxValue);
        }
    }

    private static uint Hit(int x, int y)
    {
        if (y >= 64 && y <= 139)
            for (var i = 0u; i < 3; i++)
            { var left = 16 + (int)i * 200; if (x >= left && x <= left + 183) return i; }
        if (x >= 520 && x <= 604 && y >= 201 && y <= 222) return 3;
        return uint.MaxValue;
    }

    private static void Activate(uint s, uint item, uint opener = 0)
    {
        L(s, Selection, item); L(s, Status, item + 1); L(s, About, item == 2 ? 1u : 0u); Changed(s);
        if (item == 0) FilesView.Open(s);
        else if (item == 1) EditorView.Show(s, opener);
    }
}
