using Amiga;
using static CopperOS.Desktop.DesktopMemory;
using static CopperOS.Desktop.NativeDesktopProcess;
using static CopperOS.Desktop.HomeView;
using ViewState = CopperOS.Desktop.NativeDesktopProcess;

namespace CopperOS.Desktop;

/// <summary>Bounded, read-only DOS browser. A candidate owns its lock until commit or rollback.</summary>
internal static class FilesView
{
    internal const uint Bytes = DesktopProfile.FilesWorkspaceBytes, Capacity = DesktopProfile.FilesEntryCapacity;
    private const int Lock = 0, Count = 4, Selected = 8, Top = 12, More = 16,
        Path = 24, Edit = 280, Fib = 536, Entries = 800, Stride = 112, Rows = 8;

    internal static void Open(uint s)
    {
        if (L(s, Files) != 0) { L(s, ViewState.View, 1); L(s, Dirty, 2); return; }
        Navigate(s, CString.ToUInt32("SYS:"), false);
    }

    internal static void Release(uint s)
    { Dispose(L(s, Dos), L(s, Files)); L(s, Files, 0); }

    private static void Dispose(uint dos, uint p)
    {
        if (p == 0) return;
        if (L(p, Lock) != 0) DesktopDos.Unlock(dos, L(p, Lock));
        Exec.FreeMem(p, Bytes);
    }

    private static void Fail(uint s, uint candidate, uint error)
    { Dispose(L(s, Dos), candidate); L(s, FileError, error); Changed(s); }

    private static void Navigate(uint s, uint name, bool parent)
    {
        var owner = DesktopBusy.Begin(s, 2);
        NavigateCore(s, name, parent);
        DesktopBusy.End(s, owner);
    }

    private static void NavigateCore(uint s, uint name, bool parent)
    {
        var dos = L(s, Dos); var old = L(s, Files); var p = Allocate(Bytes);
        if (p == 0) { Fail(s, 0, 103); return; }
        uint handle;
        if (parent) handle = DesktopDos.Parent(dos, L(old, Lock));
        else
        {
            // CurrentDir borrows the retained lock. Restore even if Lock fails;
            // no view state or process directory is published during a scan.
            var previous = DesktopDos.Current(dos, old == 0 ? 0 : L(old, Lock));
            handle = DesktopDos.Lock(dos, name, -2);
            var error = handle == 0 ? DesktopDos.Error(dos) : 0;
            DesktopDos.Current(dos, previous);
            if (handle == 0) { Fail(s, p, error); return; }
        }
        // ParentDir(root) returns zero: keep the current root and selection.
        if (handle == 0) { Fail(s, p, DesktopDos.Error(dos)); return; }
        L(p, Lock, handle);
        if (DesktopDos.Examine(dos, handle, p + Fib) == 0)
        { Fail(s, p, DesktopDos.Error(dos)); return; }
        if ((int)L(p, Fib + 4) <= 0) { Fail(s, p, 212); return; }
        if (DesktopDos.Name(dos, handle, p + Path, 256) == 0)
        { Fail(s, p, DesktopDos.Error(dos)); return; }
        for (var i = 0u; i <= Capacity; i++)
        {
            if (DesktopDos.Next(dos, handle, p + Fib) == 0)
            {
                var error = DesktopDos.Error(dos);
                if (error != 232) { Fail(s, p, error); return; }
                break;
            }
            if (i == Capacity) { L(p, More, 1); break; }
            var entry = p + Entries + i * Stride;
            L(entry, 0, L(p, Fib + 4));
            Copy(p + Fib + 8, entry + 4, 108);
            L(p, Count, i + 1);
            DesktopBusy.Poll(s);
        }
        L(s, Files, p); L(s, ViewState.View, 1); L(s, FileError, 0); L(s, PathEditing, 0);
        L(s, Pressed, uint.MaxValue); L(s, Dirty, 2);
        Dispose(dos, old);
    }

    private static void Copy(uint from, uint to, int bound)
    {
        for (var i = 0; i < bound - 1; i++)
        { var b = B(from, i); B(to, i, b); if (b == 0) return; }
        B(to, bound - 1, 0);
    }

    private static int Length(uint text, int bound)
    { var i = 0; while (i < bound && B(text, i) != 0) i++; return i; }

    internal static void Draw(uint s)
    {
        Rect(s, 4, 14, 615, 227, 0); Rect(s, 12, 20, 607, 43, 3);
        Text(s, 20, 35, 2, "CopperOS Files", 14);
        Text(s, 336, 35, 2, "Home", 4); Text(s, 384, 35, 2, "Up", 2);
        Text(s, 432, 35, 2, "Root", 4); Text(s, 512, 35, 2, "Reload", 6);
        L(s, 184, uint.MaxValue); // Force rows when entering, navigating or refreshing.
        DrawChanges(s);
    }

    internal static void DrawChanges(uint s)
    {
        var p = L(s, Files); var editing = L(s, PathEditing) != 0;
        Rect(s, 16, 48, 603, 65, editing ? 2u : 0u);
        var text = p + (uint)(editing ? Edit : Path); var length = Length(text, 255);
        // Show the tail while editing long paths; bytes remain intact in memory.
        if (length > 71) text += (uint)(length - 71);
        TextRaw(s, 20, 61, 1, text, length > 71 ? 71 : length);
        var count = L(p, Count); var top = L(p, Top);
        var previousTop = L(s, 184); var previousSelection = L(s, 188);
        var selection = L(p, Selected);
        for (var row = 0; row < Rows; row++)
        {
            var index = top + (uint)row; var y = 70 + row * 14;
            if (previousTop == top && (previousSelection == selection || index != previousSelection && index != selection)) continue;
            var selected = index < count && index == selection;
            Rect(s, 16, y, 603, y + 13, selected ? 3u : 2u);
            if (index >= count) continue;
            var entry = p + Entries + index * Stride; var pen = selected ? 2u : 1u;
            if ((int)L(entry, 0) > 0) Text(s, 20, y + 10, pen, "DIR", 3);
            else Text(s, 20, y + 10, pen, "FILE", 4);
            TextRaw(s, 64, y + 10, pen, entry + 4, Length(entry + 4, 66));
        }
        L(s, 184, top); L(s, 188, selection);
        Rect(s, 16, 187, 603, 224, 0);
        if (L(s, FileError) == 1001) Text(s, 20, 200, 1, "Not a text file; current listing retained.", 42);
        else if (L(s, FileError) == 1005) Text(s, 20, 200, 1, "File I/O failed; current listing retained.", 42);
        else if (L(s, FileError) != 0)
        {
            Text(s, 20, 200, 1, "DOS error; current listing retained:", 36);
            Number(s, L(s, FileError), 312, 200);
        }
        else if (editing) Text(s, 20, 200, 1, "Path: type, Enter opens; Esc cancels", 36);
        else if (L(p, More) != 0) Text(s, 20, 200, 1, "First 32 entries shown; use Path for other drawers", 50);
        else if (count == 0) Text(s, 20, 200, 1, "Empty directory", 15);
        else Text(s, 20, 200, 1, "Enter opens drawer or text in Editor", 36);
        Text(s, 20, 218, 1, "P path | U up | R root | F reload | V recovery", 46);
        Number(s, count == 0 ? 0 : L(p, Selected) + 1, 500, 218);
        Text(s, 532, 218, 1, "/", 1); Number(s, count, 548, 218);
    }

    private static void Number(uint s, uint value, int x, int y)
    {
        var p = s + 192; var n = 10;
        do { n--; B(p, n, (byte)('0' + value % 10)); value /= 10; } while (value != 0);
        TextRaw(s, x, y, 1, p + (uint)n, 10 - n);
    }

    internal static void Key(uint s, uint code, uint qualifier = 0)
    {
        if (code == 3) { EditorView.RequestQuit(s); return; }
        if ((qualifier & 0x200) == 0) L(s, 204, 0);
        else if (L(s, PathEditing) != 0 && L(s, 204) != 0 && (code | 32) == (L(s, 204) | 32)) return;
        // Navigation is one action per press. DOS can block long enough for
        // repeat messages to queue before the physical release is drained.
        // Keep repeat for path text/backspace, never for Enter or view actions.
        if ((qualifier & 0x200) != 0 && (L(s, PathEditing) == 0 || code == 13)) return;
        var p = L(s, Files);
        if (L(s, PathEditing) != 0)
        {
            var n = L(s, PathLength);
            if (code == 27) { L(s, PathEditing, 0); L(s, FileError, 0); }
            else if (code == 21) { B(p + Edit, 0, 0); L(s, PathLength, 0); L(s, FileError, 0); }
            else if (code == 13) { Navigate(s, p + Edit, false); return; }
            else if (code == 8 || code == 127)
            { if (n != 0) { n--; B(p + Edit, (int)n, 0); L(s, PathLength, n); } }
            else if (code >= 32 && code <= 255)
            {
                if (n < 255) { B(p + Edit, (int)n, (byte)code); n++; B(p + Edit, (int)n, 0); L(s, PathLength, n); L(s, FileError, 0); }
                else L(s, FileError, 120);
            }
            Changed(s); return;
        }
        if (code == 27 || code == 'h' || code == 'H')
        { L(s, ViewState.View, 0); L(s, Status, 0); L(s, About, 0); L(s, Pressed, uint.MaxValue); L(s, Dirty, 2); }
        else if (code == 'p' || code == 'P') EditPath(s, code);
        else if (code == 'u' || code == 'U' || code == 8) Navigate(s, 0, true);
        else if (code == 'r' || code == 'R') Navigate(s, CString.ToUInt32("SYS:"), false);
        else if (code == 'f' || code == 'F') Navigate(s, CString.ToUInt32(""), false);
        else if (code == 'v' || code == 'V')
        {
            if (L(p, Count) != 0)
            {
                var entry = p + Entries + L(p, Selected) * Stride;
                RecoveryView.Open(s, L(p, Lock), entry + 4);
            }
        }
        else if (code == 13) Activate(s, code);
        else if (code == 9 || code == 32) Select(s, 1);
    }

    private static void EditPath(uint s, uint opener = 0)
    {
        var p = L(s, Files); Copy(p + Path, p + Edit, 256);
        L(s, 204, opener);
        L(s, PathEditing, 1); L(s, PathLength, (uint)Length(p + Edit, 255)); L(s, FileError, 0); Changed(s);
    }

    internal static void Raw(uint s, uint code)
    {
        if (L(s, PathEditing) != 0) return;
        if (code == 0x4C) Select(s, -1);
        else if (code == 0x4D) Select(s, 1);
        else if (code == 0x4F) Select(s, -Rows);
        else if (code == 0x4E) Select(s, Rows);
    }

    private static void Select(uint s, int delta)
    {
        var p = L(s, Files); var count = L(p, Count); if (count == 0) return;
        var index = (int)L(p, Selected) + delta;
        if (index < 0) index = 0; if ((uint)index >= count) index = (int)count - 1;
        L(p, Selected, (uint)index);
        var top = L(p, Top);
        if ((uint)index < top) L(p, Top, (uint)index);
        else if ((uint)index >= top + Rows) L(p, Top, (uint)index - Rows + 1);
        L(s, FileError, 0); Changed(s);
    }

    private static void Activate(uint s, uint opener = 0)
    {
        var p = L(s, Files); if (L(p, Count) == 0) return;
        var entry = p + Entries + L(p, Selected) * Stride;
        if ((int)L(entry, 0) > 0) Navigate(s, entry + 4, false);
        else { L(s, FileError, 0); EditorView.OpenFromFiles(s, L(p, Lock), entry + 4, opener); }
    }

    private static uint Hit(uint s, int x, int y)
    {
        if (x >= 304 && x <= 400 && y >= 205 && y <= 224) return 105;
        if (x >= 336 && x < 432 && y >= 20 && y <= 43) return 100u + (uint)((x - 336) / 48);
        if (x >= 432 && x < 512 && y >= 20 && y <= 43) return 102;
        if (x >= 512 && x <= 607 && y >= 20 && y <= 43) return 103;
        if (x >= 16 && x <= 603 && y >= 48 && y <= 65) return 104;
        if (x >= 16 && x <= 603 && y >= 70 && y < 182)
        { var i = L(L(s, Files), Top) + (uint)((y - 70) / 14); if (i < L(L(s, Files), Count)) return i; }
        return uint.MaxValue;
    }

    internal static void Mouse(uint s, uint code, int x, int y)
    {
        var hit = Hit(s, x, y);
        if (code == 0x68)
        {
            L(s, Pressed, hit);
            if (hit < Capacity)
            { L(s, PathEditing, 0); L(s, FileError, 0); L(L(s, Files), Selected, hit); Changed(s); }
        }
        else if (code == 0xE8)
        {
            var pressed = L(s, Pressed); L(s, Pressed, uint.MaxValue);
            if (hit != pressed) return;
            // Header actions remain actions while the path field has focus.
            if (hit >= 100 && hit <= 103 || hit == 105) L(s, PathEditing, 0);
            if (hit < Capacity) Activate(s);
            else if (hit == 100) Key(s, 27);
            else if (hit == 101) Key(s, 'u');
            else if (hit == 102) Key(s, 'r');
            else if (hit == 103) Key(s, 'f');
            else if (hit == 104) EditPath(s);
            else if (hit == 105) Key(s, 'v');
        }
    }
}
