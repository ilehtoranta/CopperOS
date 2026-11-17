using Amiga;
using static CopperOS.Desktop.DesktopMemory;
using static CopperOS.Desktop.NativeDesktopProcess;
using static CopperOS.Desktop.HomeView;

namespace CopperOS.Desktop;

/// <summary>Read-only inspection. Neither archive nor destination is modified.</summary>
internal static class RecoveryView
{
    private const int Directory = 0, Available = 4, Selected = 8, Origin = 12,
        Path = 16, Target = 272, Fib = 304;

    internal static void Release(uint s)
    { Dispose(L(s, Dos), L(s, Recovery)); L(s, Recovery, 0); }
    private static void Dispose(uint dos, uint p)
    {
        if (p == 0) return;
        if (L(p, Directory) != 0) DesktopDos.Unlock(dos, L(p, Directory));
        Exec.FreeMem(p, DesktopProfile.RecoveryBytes);
    }
    private static int Length(uint p, int bound)
    { var n = 0; while (n < bound && B(p, n) != 0) n++; return n; }

    internal static void FromSave(uint s)
    {
        var w = L(s, SaveState);
        if (w == 0 || L(w, EditorSave.Stage) == 0) return;
        var owner = DesktopBusy.Begin(s, 4);
        // Path remains available after successful promotion transfers the
        // directory lock to the document. Reacquire without borrowing it.
        var dos = L(s, Dos); var parent = DesktopDos.Lock(dos, w + 320, -2);
        if (parent == 0) { L(s, EditorError, DesktopDos.Error(dos)); Changed(s); DesktopBusy.End(s, owner); return; }
        Open(s, parent, w + EditorSave.Drawer); DesktopDos.Unlock(dos, parent);
        DesktopBusy.End(s, owner);
    }

    internal static void Open(uint s, uint parent, uint name)
    {
        // This is an application naming convention, not a DOS ABI validator.
        var prefix = CString.ToUInt32("CopperSave00"); var valid = true;
        for (var i = 0; i < 12; i++) if ((B(name, i) | 32) != (B(prefix, i) | 32)) valid = false;
        if (B(name, 12) < '0' || B(name, 12) > '3' || B(name, 13) < '0' || B(name, 13) > '9' ||
            B(name, 12) == '3' && B(name, 13) > '1' || B(name, 14) != 0) valid = false;
        if (!valid) { L(s, FileError, 212); Changed(s); return; }
        var owner = DesktopBusy.Begin(s, 4);
        OpenCore(s, parent, name);
        DesktopBusy.End(s, owner);
    }

    private static uint Inspect(uint s, uint p, CString name, uint flag)
    {
        var dos = L(s, Dos); var file = DesktopDos.Lock(dos, CString.ToUInt32(name), -2);
        var error = file == 0 ? DesktopDos.Error(dos) : 0;
        if (file == 0) return error == 205 ? 0u : error;
        if (DesktopDos.Examine(dos, file, p + Fib) == 0) error = DesktopDos.Error(dos);
        else if ((int)L(p, Fib + 4) == -3) L(p, Available, L(p, Available) | flag);
        DesktopDos.Unlock(dos, file); DesktopBusy.Poll(s); return error;
    }

    private static void OpenCore(uint s, uint parent, uint name)
    {
        var dos = L(s, Dos); var p = Allocate(DesktopProfile.RecoveryBytes);
        if (p == 0) { Failed(s, 0, 103); return; }
        var previous = DesktopDos.Current(dos, parent);
        var directory = DesktopDos.Lock(dos, name, -2); L(p, Directory, directory);
        var error = directory == 0 ? DesktopDos.Error(dos) : 0;
        DesktopDos.Current(dos, previous);
        if (error == 0 && DesktopDos.Examine(dos, directory, p + Fib) == 0) error = DesktopDos.Error(dos);
        if (error == 0 && (int)L(p, Fib + 4) <= 0) error = 212;
        if (error == 0 && DesktopDos.Name(dos, directory, p + Path, 256) == 0) error = DesktopDos.Error(dos);
        if (error != 0) { Failed(s, p, error); return; }
        previous = DesktopDos.Current(dos, directory);
        error = Inspect(s, p, "New", 1);
        if (error == 0) error = Inspect(s, p, "Original", 2);
        if (error == 0)
        {
            var file = DesktopDos.Open(dos, CString.ToUInt32("Target"), 1005);
            if (file != 0)
            {
                var count = 0; var good = true;
                while (count < 32)
                {
                    var got = DesktopDos.Read(dos, file, p + Target + (uint)count, 32 - count);
                    if (got < 0 || got > 32 - count) { good = false; break; }
                    if (got == 0) break;
                    count += got; DesktopBusy.Poll(s);
                }
                if (DesktopDos.Close(dos, file) == 0) good = false;
                if (count == 0 || count > 30) good = false;
                for (var i = 0; i < count; i++)
                    if (B(p + Target, i) < 32 || B(p + Target, i) == ':' || B(p + Target, i) == '/') good = false;
                if (!good) B(p, Target, 0); else B(p + Target, count, 0);
            }
        }
        DesktopDos.Current(dos, previous);
        if (error != 0) { Failed(s, p, error); return; }
        L(p, Origin, L(s, NativeDesktopProcess.View)); L(p, Selected, (L(p, Available) & 1) != 0 ? 0u : 1u);
        Release(s); L(s, Recovery, p); L(s, NativeDesktopProcess.View, 3); L(s, EditorError, 0); L(s, Dirty, 2);
    }

    private static void Failed(uint s, uint p, uint error)
    {
        Dispose(L(s, Dos), p);
        if (L(s, NativeDesktopProcess.View) == 1) L(s, FileError, error); else L(s, EditorError, error);
        Changed(s);
    }

    internal static void Draw(uint s)
    {
        var p = L(s, Recovery);
        Rect(s, 4, 14, 615, 227, 0); Rect(s, 12, 20, 607, 43, 3);
        Text(s, 20, 35, 2, "CopperOS Recovery", 17); Text(s, 552, 35, 2, "Back", 4);
        var path = p + Path; var n = Length(path, 255); if (n > 71) path += (uint)(n - 71);
        TextRaw(s, 20, 61, 1, path, n > 71 ? 71 : n);
        Text(s, 20, 80, 1, "Destination hint: ", 18);
        if (B(p, Target) == 0) Text(s, 164, 80, 1, "unavailable", 11);
        else TextRaw(s, 164, 80, 1, p + Target, Length(p + Target, 30));
        for (var i = 0u; i < 2; i++)
        {
            var y = 94 + (int)i * 24; var selected = L(p, Selected) == i;
            Rect(s, 16, y, 603, y + 21, selected ? 3u : 2u);
            var pen = selected ? 2u : 1u;
            if (i == 0) Text(s, 20, y + 14, pen, "New - attempted saved text", 26);
            else Text(s, 20, y + 14, pen, "Original - previous file", 24);
            if ((L(p, Available) & (1u << (int)i)) == 0) Text(s, 440, y + 14, pen, "Unavailable", 11);
            else Text(s, 440, y + 14, pen, "Open copy", 9);
        }
        Text(s, 20, 161, 1, "Copies may be incomplete. Check text before saving.", 51);
        Text(s, 20, 179, 1, "Archives stay untouched. Binary files need DOS tools.", 53);
        if (L(s, EditorError) != 0) Text(s, 20, 200, 1, "Could not open text copy; archive retained.", 43);
        else Text(s, 20, 200, 1, "Opening creates unsaved text. Choose Save As to keep it.", 56);
        Text(s, 20, 222, 1, "Arrows / Tab select | Enter open copy | Esc back", 48);
    }

    internal static void Key(uint s, uint code, uint qualifier)
    {
        if ((qualifier & 0x200) != 0) return;
        var p = L(s, Recovery);
        if (code == 3) EditorView.RequestQuit(s);
        else if (code == 27) { L(s, NativeDesktopProcess.View, L(p, Origin)); L(s, Dirty, 2); }
        else if (code == 9 || code == 32) { L(p, Selected, L(p, Selected) ^ 1); Changed(s); }
        else if (code == 13)
        {
            var selected = L(p, Selected);
            if ((L(p, Available) & (1u << (int)selected)) != 0)
                EditorView.OpenRecovery(s, L(p, Directory), CString.ToUInt32(selected == 0 ? "New" : "Original"));
        }
    }
    internal static void Raw(uint s, uint code)
    {
        if (code == 0x4C || code == 0x4D) { L(L(s, Recovery), Selected, code == 0x4C ? 0u : 1u); Changed(s); }
    }
    private static uint Hit(int x, int y)
    {
        if (x >= 544 && x <= 607 && y >= 20 && y <= 43) return 2;
        if (x >= 16 && x <= 603 && y >= 94 && y <= 115) return 0;
        if (x >= 16 && x <= 603 && y >= 118 && y <= 139) return 1;
        return uint.MaxValue;
    }
    internal static void Mouse(uint s, uint code, int x, int y)
    {
        var hit = Hit(x, y);
        if (code == 0x68) { L(s, Pressed, hit); if (hit < 2) { L(L(s, Recovery), Selected, hit); Changed(s); } }
        else if (code == 0xE8)
        {
            var pressed = L(s, Pressed); L(s, Pressed, uint.MaxValue);
            if (hit == pressed) { if (hit < 2) Key(s, 13, 0); else if (hit == 2) Key(s, 27, 0); }
        }
    }
}
