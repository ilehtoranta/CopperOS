using Amiga;
using static CopperOS.Desktop.DesktopMemory;
using static CopperOS.Desktop.NativeDesktopProcess;
using static CopperOS.Desktop.HomeView;
using E = CopperOS.Desktop.EditorBuffer;

namespace CopperOS.Desktop;

internal static class EditorView
{
    internal static void Show(uint s, uint opener = 0)
    {
        if (L(s, Document) == 0) New(s, 0);
        else { L(s, NativeDesktopProcess.View, 2); L(s, Dirty, 2); }
        L(s, EditorOpener, opener);
    }

    private static void Copy(uint from, uint to, int size)
    { for (var i = 0; i < size - 1; i++) { var b = B(from, i); B(to, i, b); if (b == 0) return; } B(to, size - 1, 0); }
    private static int Size(uint p, int limit)
    { var i = 0; while (i < limit && B(p, i) != 0) i++; return i; }
    private static void Text(uint s, int x, int y, uint pen, CString text, int count)
        => TextRaw(s, x, y, pen, CString.ToUInt32(text), count);
    private static void TextRaw(uint s, int x, int y, uint pen, uint text, int count)
    {
        HomeView.TextRaw(s, x, y, pen, text, count);
    }
    private static void Dispose(uint s, uint p)
    {
        if (p == 0) return;
        if (L(p, E.Directory) != 0) DesktopDos.Unlock(L(s, Dos), L(p, E.Directory));
        EditorSave.FreeSnapshot(L(s, Dos), L(p, E.Original));
        Exec.FreeMem(p, DesktopProfile.EditorBytes);
    }
    internal static void Release(uint s)
    { EditorSave.Release(s); Dispose(s, L(s, Document)); L(s, Document, 0); EditorFont.Release(s); }

    private static void Failure(uint s, uint candidate, uint origin, uint error)
    {
        Dispose(s, candidate); L(s, NativeDesktopProcess.View, origin);
        if (origin == 1) L(s, FileError, error);
        else L(s, EditorError, error);
        if (origin == 0) L(s, Status, 2);
        L(s, Dirty, 2);
    }

    private static void Commit(uint s, uint candidate, uint origin)
    {
        if (!EditorFont.Ensure(s)) { Failure(s, candidate, origin, 103); return; }
        EditorSave.Release(s);
        var old = L(s, Document); L(s, Document, candidate); L(s, NativeDesktopProcess.View, 2);
        L(s, EditorError, 0); L(s, EditorPrompt, 0); L(s, Pressed, uint.MaxValue); L(s, Dirty, 2);
        Dispose(s, old);
    }

    private static void New(uint s, uint origin)
    {
        var p = Allocate(DesktopProfile.EditorBytes); if (p == 0) { Failure(s, 0, origin, 103); return; }
        E.Initialize(p); Copy(CString.ToUInt32("Untitled"), p + E.Name, 108); Commit(s, p, origin);
    }

    internal static void OpenFromFiles(uint s, uint directory, uint name, uint opener = 0)
    {
        L(s, EditorOpener, opener);
        if (L(s, Document) != 0 && L(L(s, Document), E.Modified) != 0)
        {
            L(s, PendingName, name); L(s, PendingDirectory, directory); Prompt(s, 3, 1); return;
        }
        Open(s, directory, name, 1);
    }

    internal static void OpenRecovery(uint s, uint directory, uint name)
    {
        if (L(s, Document) != 0 && L(L(s, Document), E.Modified) != 0)
        { L(s, PendingName, name); L(s, PendingDirectory, directory); Prompt(s, 4, 3); return; }
        Open(s, directory, name, 3, true);
    }

    private static void Open(uint s, uint directory, uint name, uint origin, bool copy = false)
    {
        var owner = DesktopBusy.Begin(s, 3);
        OpenCore(s, directory, name, origin, copy);
        DesktopBusy.End(s, owner);
    }

    private static void OpenCore(uint s, uint directory, uint name, uint origin, bool copy)
    {
        var dos = L(s, Dos); var p = Allocate(DesktopProfile.EditorBytes);
        if (p == 0) { Failure(s, 0, origin, 103); return; }
        E.Initialize(p); Copy(name, p + E.Name, 108);
        var owned = DesktopDos.Duplicate(dos, directory); L(p, E.Directory, owned);
        if (owned == 0) { Failure(s, p, origin, DesktopDos.Error(dos)); return; }
        if (DesktopDos.Name(dos, owned, p + E.Path, 256) == 0) { Failure(s, p, origin, DesktopDos.Error(dos)); return; }
        var previous = DesktopDos.Current(dos, owned);
        var file = DesktopDos.Open(dos, p + E.Name, 1005);
        var error = file == 0 ? DesktopDos.Error(dos) : 0;
        DesktopDos.Current(dos, previous);
        if (file == 0) { Failure(s, p, origin, error); return; }
        uint length = 0;
        while (error == 0)
        {
            var count = DesktopProfile.EditorCapacity + 1 - length; if (count > 1024) count = 1024;
            var got = DesktopDos.Read(dos, file, p + E.Data + length, (int)count);
            if (got < 0) { error = DesktopDos.Error(dos); if (error == 0) error = 1005; break; }
            if (got == 0) break;
            length += (uint)got;
            if (length > DesktopProfile.EditorCapacity) { error = 120; break; }
            DesktopBusy.Poll(s);
        }
        var closed = DesktopDos.Close(dos, file);
        if (closed == 0 && error == 0) { error = DesktopDos.Error(dos); if (error == 0) error = 1005; }
        L(p, E.Length, length);
        if (error == 0 && !E.Validate(p)) error = 1001;
        if (error == 0 && !copy) error = EditorSave.Remember(s, p);
        if (error != 0) { Failure(s, p, origin, error); return; }
        if (copy)
        {
            DesktopDos.Unlock(dos, L(p, E.Directory)); L(p, E.Directory, 0);
            B(p, E.Path, 0); Copy(CString.ToUInt32("Recovered"), p + E.Name, 108); L(p, E.Modified, 1);
        }
        B(p, E.Data + (int)length, 0); Commit(s, p, origin);
    }

    private static void Prompt(uint s, uint action, uint origin)
    { L(s, PendingAction, action); L(s, PromptOrigin, origin); L(s, EditorPrompt, 1); L(s, NativeDesktopProcess.View, 2); L(s, Dirty, 2); }

    internal static void RequestQuit(uint s)
    {
        if (L(s, EditorPrompt) != 0) return;
        var p = L(s, Document);
        if (p != 0 && L(p, E.Modified) != 0) Prompt(s, 1, L(s, NativeDesktopProcess.View));
        else L(s, Quit, 1);
    }

    internal static void BusyClose(uint s)
    {
        // A close arriving during destination lookup still takes effect if
        // lookup ends at Save As/overwrite consent rather than at a commit.
        if (L(s, EditorPrompt) >= 2) EditorSave.Cancel(s);
        if (L(s, EditorPrompt) == 1)
        { L(s, PendingAction, 1); L(s, PendingName, 0); L(s, PendingDirectory, 0); Changed(s); }
        else RequestQuit(s);
    }

    private static void Decide(uint s, bool discard)
    {
        var action = L(s, PendingAction); var origin = L(s, PromptOrigin);
        var name = L(s, PendingName); var directory = L(s, PendingDirectory);
        L(s, EditorPrompt, 0); L(s, PendingAction, 0); L(s, PendingName, 0); L(s, PendingDirectory, 0);
        if (!discard) { L(s, NativeDesktopProcess.View, origin); L(s, Dirty, 2); return; }
        if (action == 1) L(s, Quit, 1);
        else if (action == 2) New(s, origin);
        else if (action == 3) Open(s, directory, name, origin);
        else if (action == 4) Open(s, directory, name, origin, true);
    }

    internal static void SaveCompleted(uint s)
    { if (L(s, PendingAction) != 0) Decide(s, true); }

    internal static void Key(uint s, uint code, uint qualifier)
    {
        var repeat = (qualifier & 0x200) != 0;
        if (!repeat) L(s, EditorOpener, 0);
        else if (L(s, EditorOpener) != 0 && (code | 32) == (L(s, EditorOpener) | 32)) return;
        if (L(s, EditorPrompt) >= 2)
        { EditorSave.Key(s, code, repeat); return; }
        if (L(s, EditorPrompt) == 1)
        {
            if (repeat) return;
            if (code == 27) Decide(s, false);
            else if (code == 'd' || code == 'D') { Decide(s, true); L(s, EditorOpener, code); }
            else if (code == 's' || code == 'S') { EditorSave.Request(s, false); L(s, EditorOpener, code); }
            return;
        }
        if (repeat && (code == 3 || code == 27 || code == 14 || code == 19)) return;
        if (code == 3) { RequestQuit(s); return; }
        if (code == 27) { Go(s, L(s, Files) != 0 ? 1u : 0); return; }
        if (code == 14) { NewRequest(s); return; }
        if (code == 19) { EditorSave.Request(s, (qualifier & 3) != 0); L(s, EditorOpener, 19); return; }
        if (code == 18) { RecoveryView.FromSave(s); return; }
        var p = L(s, Document); L(s, EditorError, 0);
        if (code == 1 || code == 5) E.HomeEnd(p, code == 5, false);
        else if (code == 20 || code == 2) E.HomeEnd(p, code == 2, true);
        else if (code == 8 || code == 127) E.Erase(p, code == 8);
        else if (code == 9 || code == 13 || code >= 32 && code <= 255 && (code < 127 || code >= 160))
        { if (!E.Insert(p, code)) L(s, EditorError, 120); }
        Changed(s);
    }

    internal static void Raw(uint s, uint code, uint qualifier)
    {
        if (L(s, EditorPrompt) != 0) return;
        var p = L(s, Document); var shift = (qualifier & 3) != 0;
        if (code == 0x4F || code == 0x4E)
        { if (shift) E.HomeEnd(p, code == 0x4E, false); else E.Horizontal(p, code == 0x4F ? -1 : 1); }
        else if (code == 0x4C || code == 0x4D) E.Vertical(p, (code == 0x4C ? -1 : 1) * (shift ? 16 : 1));
        else return;
        L(s, EditorError, 0); Changed(s);
    }

    private static void NewRequest(uint s)
    { if (L(L(s, Document), E.Modified) != 0) Prompt(s, 2, 2); else New(s, 2); L(s, EditorOpener, 14); }

    private static void Go(uint s, uint view)
    { L(s, NativeDesktopProcess.View, view); L(s, Status, 0); L(s, About, 0); L(s, Pressed, uint.MaxValue); L(s, Dirty, 2); }

    internal static void Draw(uint s)
    {
        EditorFont.Bind(s, true);
        Rect(s, 4, 14, 615, 227, 0); Rect(s, 12, 20, 607, 43, 3);
        Text(s, 20, 35, 2, "CopperOS Editor", 15);
        Text(s, 336, 35, 2, "Home", 4); Text(s, 384, 35, 2, "Files", 5); Text(s, 464, 35, 2, "New", 3);
        Text(s, 536, 35, 2, "Save", 4);
        L(L(s, Document), E.PaintedTop, uint.MaxValue);
        Paint(s); EditorFont.Bind(s, false);
    }

    internal static void DrawChanges(uint s)
    { EditorFont.Bind(s, true); Paint(s); EditorFont.Bind(s, false); }

    private static void Paint(uint s)
    {
        var p = L(s, Document); var top = L(p, E.Top); var left = L(p, E.Left);
        var line = L(p, E.Line); var column = L(p, E.Column);
        var full = top != L(p, E.PaintedTop) || left != L(p, E.PaintedLeft);
        Rect(s, 16, 48, 603, 63, 0);
        var name = p + E.Name; var nameX = 20; var nameLimit = 66; var nameBound = 107;
        var save = L(s, SaveState);
        if (L(s, EditorPrompt) >= 2 || save != 0 && L(save, EditorSave.Stage) != 0 && L(s, EditorError) != 0)
        {
            var recovery = L(s, EditorPrompt) < 2;
            Text(s, 20, 60, 1, recovery ? "Recovery in: " : "Save in: ", recovery ? 13 : 9);
            nameX = recovery ? 124 : 92; nameLimit = recovery ? 56 : 60; nameBound = 255;
            if (L(s, EditorPrompt) == 3 || recovery) name = save + 320;
            else if (L(p, E.Directory) != 0) name = p + E.Path;
            else name = L(s, Files) != 0 ? L(s, Files) + 24 : CString.ToUInt32("SYS:");
        }
        var count = Size(name, nameBound); if (count > nameLimit) { name += (uint)(count - nameLimit); count = nameLimit; }
        TextRaw(s, nameX, 60, 1, name, count);
        if (L(p, E.Modified) != 0) Text(s, 580, 60, 1, "*", 1);
        var at = E.LineStart(p, top);
        for (var row = 0u; row < E.Rows; row++)
        {
            var logical = top + row; var next = E.Row(p, at);
            if (full || logical == line || logical == L(p, E.PaintedLine) || logical >= L(p, E.PaintFrom) && logical <= L(p, E.PaintTo))
            {
                var y = 66 + (int)row * 8; Rect(s, 16, y, 603, y + 7, 2);
                TextRaw(s, 20, y + 6, 1, p + E.Scratch, (int)L(p, E.ScratchCount));
            }
            at = next;
        }
        if (L(s, EditorPrompt) == 0)
        { var x = 20 + (int)(column - left) * 8; var y = 66 + (int)(line - top) * 8; Rect(s, x, y, x, y + 7, 1); }
        L(p, E.PaintedTop, top); L(p, E.PaintedLeft, left); L(p, E.PaintedLine, line); L(p, E.PaintedColumn, column);
        L(p, E.PaintFrom, uint.MaxValue); L(p, E.PaintTo, 0);
        Rect(s, 16, 198, 603, 224, 0);
        if (L(s, EditorPrompt) == 1)
        {
            Text(s, 20, 209, 1, "Unsaved edits", 13);
            if (L(s, EditorError) != 0) { Text(s, 140, 209, 1, "Save failed", 11); Number(s, L(s, EditorError), 236, 209); }
            Text(s, 360, 218, 1, "Save", 4); Text(s, 440, 218, 1, "Discard", 7); Text(s, 528, 218, 1, "Cancel", 6);
        }
        else if (L(s, EditorPrompt) >= 2)
        {
            var w = L(s, SaveState); var n = (int)L(w, EditorSave.InputLength); var text = w + EditorSave.Input;
            if (n > 71) { text += (uint)(n - 71); n = 71; }
            TextRaw(s, 20, 208, 1, text, n);
            if (L(s, EditorPrompt) == 3) Text(s, 20, 222, 1, "Replace existing file? Y yes | Esc cancel", 41);
            else Text(s, 20, 222, 1, "Save As: path | Enter save | Ctrl-U clear | Esc cancel", 54);
        }
        else
        {
            Text(s, 20, 208, 1, L(p, E.Modified) != 0 ? "Modified" : "Unchanged", L(p, E.Modified) != 0 ? 8 : 9);
            if (L(s, EditorError) == 120) Text(s, 140, 208, 1, "16 KiB limit; edit refused", 26);
            else if (L(s, EditorError) == 214) Text(s, 140, 208, 1, "Disk is read-only; edits retained", 33);
            else if (L(s, EditorError) == 221) Text(s, 140, 208, 1, "Disk full; edits retained", 25);
            else if (L(s, EditorError) == 223) Text(s, 140, 208, 1, "File is write-protected; use Save As", 36);
            else if (L(s, EditorError) == 1009) Text(s, 140, 208, 1, "Source changed; use Save As", 27);
            else if (L(s, EditorError) == 1011) Text(s, 140, 208, 1, "Saved; recovery drawer retained", 31);
            else if (L(s, EditorError) == 1012) Text(s, 140, 208, 1, "Restore failed; recovery retained", 33);
            else if (L(s, EditorError) == 1013) Text(s, 140, 208, 1, "Saved; reopen before next Save", 30);
            else if (L(s, EditorError) == 1014) Text(s, 140, 208, 1, "Path/name limit: 30-byte names", 30);
            else if (L(s, EditorError) != 0) { Text(s, 140, 208, 1, "File I/O failed", 15); Number(s, L(s, EditorError), 272, 208); }
            if (L(s, SaveState) != 0 && L(L(s, SaveState), EditorSave.Stage) != 0)
            { Text(s, 20, 222, 1, "Recovery: ", 10); TextRaw(s, 100, 222, 1, L(s, SaveState) + EditorSave.Drawer, 14); Text(s, 232, 222, 1, "Ctrl-R inspect", 14); }
            else Text(s, 20, 222, 1, "Ctrl-S Save | Shift-Ctrl-S Save As | Esc back", 45);
            Number(s, line + 1, 456, 208); Text(s, 492, 208, 1, ":", 1); Number(s, column + 1, 508, 208);
            Number(s, L(p, E.Length), 548, 222);
        }
    }

    private static void Number(uint s, uint value, int x, int y)
    {
        var p = s + 192; var n = 10;
        do { n--; B(p, n, (byte)('0' + value % 10)); value /= 10; } while (value != 0);
        TextRaw(s, x, y, 1, p + (uint)n, 10 - n);
    }

    private static uint Hit(uint s, int x, int y)
    {
        if (L(s, EditorPrompt) >= 2) return uint.MaxValue;
        if (L(s, EditorPrompt) == 1)
        { if (y >= 200 && y <= 224) { if (x >= 352 && x <= 415) return 2005; if (x >= 440 && x <= 511) return 2003; if (x >= 520 && x <= 603) return 2004; } return uint.MaxValue; }
        if (y >= 214 && y <= 224 && x >= 20 && x < 448 && L(s, SaveState) != 0 && L(L(s, SaveState), EditorSave.Stage) != 0) return 2006;
        if (y >= 20 && y <= 43)
        { if (x >= 336 && x <= 383) return 2000; if (x >= 384 && x <= 447) return 2001; if (x >= 464 && x <= 511) return 2002; if (x >= 520 && x <= 603) return 2005; }
        if (x >= 20 && x < 596 && y >= 66 && y < 194) return 1 + (uint)((y - 66) / 8) * E.Columns + (uint)((x - 20) / 8);
        return uint.MaxValue;
    }

    internal static void Mouse(uint s, uint code, int x, int y)
    {
        var hit = Hit(s, x, y);
        if (code == 0x68)
        {
            L(s, Pressed, hit);
            if (hit >= 1 && hit <= E.Rows * E.Columns)
            { var p = L(s, Document); E.Place(p, L(p, E.Top) + (hit - 1) / E.Columns, L(p, E.Left) + (hit - 1) % E.Columns); L(p, E.Preferred, L(p, E.Column)); Changed(s); }
        }
        else if (code == 0xE8)
        {
            var pressed = L(s, Pressed); L(s, Pressed, uint.MaxValue); if (hit != pressed) return;
            if (hit == 2000) Go(s, 0);
            else if (hit == 2001)
            {
                FilesView.Open(s);
                if (L(s, Files) == 0) { L(s, EditorError, L(s, FileError)); Changed(s); }
            }
            else if (hit == 2002) NewRequest(s);
            else if (hit == 2003) Decide(s, true);
            else if (hit == 2004) Decide(s, false);
            else if (hit == 2005) EditorSave.Request(s, false);
            else if (hit == 2006) RecoveryView.FromSave(s);
        }
    }
}
