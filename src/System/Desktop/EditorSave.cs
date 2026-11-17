using Amiga;
using static CopperOS.Desktop.DesktopMemory;
using static CopperOS.Desktop.NativeDesktopProcess;
using E = CopperOS.Desktop.EditorBuffer;

namespace CopperOS.Desktop;

/// <summary>Bounded DOS replacement. The target is never opened with MODE_NEWFILE.</summary>
internal static class EditorSave
{
    internal const int Input = 64, InputLength = 20, Stage = 28, Drawer = 684;
    private const int Directory = 0, DrawerLock = 4, Expected = 8, OwnExpected = 12, Next = 16,
        Path = 320, Target = 576, NewPath = 716, OldPath = 764, Manifest = 812,
        Fib = 912, Scratch = 1176, SnapshotData = 272;

    private static void Copy(uint from, uint to, int bound)
    { for (var i = 0; i < bound - 1; i++) { var b = B(from, i); B(to, i, b); if (b == 0) return; } B(to, bound - 1, 0); }
    private static uint Length(uint p, uint bound)
    { uint n = 0; while (n < bound && B(p, (int)n) != 0) n++; return n; }
    private static uint Error(uint dos)
    { var error = DesktopDos.Error(dos); return error == 0 ? 1005u : error; }
    private static bool Equal(uint a, uint b, uint length)
    {
        for (var i = 0u; i < length; i++)
        {
            if (((uint)B(a, (int)i) & 255) != ((uint)B(b, (int)i) & 255)) return false;
        }
        return true;
    }
    private static bool Metadata(uint a, uint b)
        => L(a, 0) == L(b, 0) && L(a, 4) == L(b, 4) && L(a, 116) == L(b, 116) &&
           L(a, 124) == L(b, 124) && L(a, 132) == L(b, 132) && L(a, 136) == L(b, 136) &&
           L(a, 140) == L(b, 140) && Equal(a + 144, b + 144, 80);

    // Capture bytes and identity while retaining a shared lock. Re-examine after
    // reading; a changing file cannot establish a baseline for replacement.
    private static uint Capture(uint s, uint dos, uint name, uint snapshot, uint fib, bool retain = false)
    {
        var handle = DesktopDos.Lock(dos, name, -2); if (handle == 0) return Error(dos);
        uint error = 0;
        if (DesktopDos.Examine(dos, handle, snapshot + 8) == 0) error = Error(dos);
        else if ((int)L(snapshot, 12) != -3) error = 212;
        else if (L(snapshot, 132) > DesktopProfile.EditorCapacity) error = 120;
        uint file = 0;
        if (error == 0) { file = DesktopDos.Open(dos, name, 1005); if (file == 0) error = Error(dos); }
        uint length = 0;
        while (error == 0)
        {
            // One extra byte detects growth, including a full 16 KiB source.
            var count = DesktopProfile.EditorCapacity + 1 - length; if (count > 1024) count = 1024;
            var got = DesktopDos.Read(dos, file, snapshot + SnapshotData + length, (int)count);
            if (got < 0) { error = Error(dos); break; }
            if (got == 0) break;
            if ((uint)got > count) { error = 1005; break; }
            length += (uint)got;
            if (length > DesktopProfile.EditorCapacity) { error = 120; break; }
            DesktopBusy.Poll(s);
        }
        if (file != 0 && DesktopDos.Close(dos, file) == 0 && error == 0) error = Error(dos);
        if (error == 0)
        {
            if (DesktopDos.Examine(dos, handle, fib) == 0) error = Error(dos);
            else if (!Metadata(snapshot + 8, fib) || length != L(snapshot, 132)) error = 1009;
        }
        if (retain && error == 0) L(snapshot, 4, handle);
        else DesktopDos.Unlock(dos, handle);
        L(snapshot, 0, length); return error;
    }

    private static void DropLock(uint dos, uint snapshot)
    { if (L(snapshot, 4) != 0) { DesktopDos.Unlock(dos, L(snapshot, 4)); L(snapshot, 4, 0); } }
    internal static void FreeSnapshot(uint dos, uint snapshot)
    { if (snapshot != 0) { DropLock(dos, snapshot); Exec.FreeMem(snapshot, DesktopProfile.EditorSnapshotBytes); } }

    internal static uint Remember(uint s, uint document)
    {
        var snapshot = Allocate(DesktopProfile.EditorSnapshotBytes); if (snapshot == 0) return 103;
        // Re-examination needs a separate FIB while the captured one stays intact.
        var fib = Allocate(260);
        if (fib == 0) { Exec.FreeMem(snapshot, DesktopProfile.EditorSnapshotBytes); return 103; }
        var dos = L(s, Dos); var previous = DesktopDos.Current(dos, L(document, E.Directory));
        var error = Capture(s, dos, document + E.Name, snapshot, fib, true);
        DesktopDos.Current(dos, previous); Exec.FreeMem(fib, 260);
        if (error == 0 && (L(snapshot, 0) != L(document, E.Length) ||
            !Equal(snapshot + SnapshotData, document + E.Data, L(document, E.Length)))) error = 1009;
        if (error == 0) L(document, E.Original, snapshot);
        else FreeSnapshot(dos, snapshot);
        return error;
    }

    internal static void Release(uint s)
    {
        var w = L(s, SaveState); if (w == 0) return;
        var dos = L(s, Dos);
        if (L(w, DrawerLock) != 0) DesktopDos.Unlock(dos, L(w, DrawerLock));
        if (L(w, Directory) != 0) DesktopDos.Unlock(dos, L(w, Directory));
        if (L(w, OwnExpected) != 0) FreeSnapshot(dos, L(w, Expected));
        FreeSnapshot(dos, L(w, Next));
        Exec.FreeMem(w, DesktopProfile.EditorSaveBytes); L(s, SaveState, 0);
    }

    internal static void Cancel(uint s)
    {
        Release(s); L(s, EditorPrompt, L(s, PendingAction) != 0 ? 1u : 0);
        L(s, EditorError, 0); Changed(s);
    }
    private static void Failed(uint s, uint error)
    { L(s, EditorError, error); L(s, EditorPrompt, L(s, PendingAction) != 0 ? 1u : 0); Changed(s); }

    internal static void Request(uint s, bool saveAs)
    {
        var owner = DesktopBusy.Begin(s, 1);
        RequestCore(s, saveAs);
        DesktopBusy.End(s, owner);
    }

    private static void RequestCore(uint s, bool saveAs)
    {
        Release(s); var w = Allocate(DesktopProfile.EditorSaveBytes);
        if (w == 0) { Failed(s, 103); return; }
        L(s, SaveState, w); L(s, EditorError, 0);
        var p = L(s, Document);
        Copy(p + E.Name, w + Input, 108); L(w, InputLength, Length(w + Input, 255));
        if (saveAs || L(p, E.Directory) == 0)
        { L(s, EditorPrompt, 2); Changed(s); return; }
        L(w, Expected, L(p, E.Original));
        if (L(w, Expected) == 0 || L(L(w, Expected), 4) == 0) { Failed(s, 1009); return; }
        Prepare(s, false);
    }

    internal static void Key(uint s, uint code, bool repeat)
    {
        var w = L(s, SaveState);
        if (code == 27 && !repeat) { Cancel(s); return; }
        if (L(s, EditorPrompt) == 3)
        { if (!repeat && (code == 'y' || code == 'Y')) { var owner = DesktopBusy.Begin(s, 1); Execute(s); DesktopBusy.End(s, owner); L(s, EditorOpener, code); } return; }
        var n = L(w, InputLength);
        if (code == 13) { if (!repeat) { var owner = DesktopBusy.Begin(s, 1); Prepare(s, true); DesktopBusy.End(s, owner); L(s, EditorOpener, 13); } return; }
        if (code == 21) n = 0;
        else if (code == 8 || code == 127) { if (n != 0) n--; }
        else if (code >= 32 && code <= 255 && (code < 127 || code >= 160))
        {
            if (n == 255) { L(s, EditorError, 1014); Changed(s); return; }
            B(w + Input, (int)n, (byte)code); n++;
        }
        else return;
        B(w + Input, (int)n, 0); L(w, InputLength, n); L(s, EditorError, 0); Changed(s);
    }

    private static void Prepare(uint s, bool saveAs)
    {
        var w = L(s, SaveState); var dos = L(s, Dos); var p = L(s, Document);
        var length = L(w, InputLength); uint split = 0, component = 0;
        for (var i = 0u; i < length; i++)
        {
            var b = (uint)B(w + Input, (int)i) & 255;
            if (b == ':' || b == '/') { if (component > 30) { Failed(s, 1014); return; } split = i + 1; component = 0; }
            else component++;
        }
        if (component == 0 || component > 30) { Failed(s, 1014); return; }
        Copy(w + Input + split, w + Target, 108);
        var anchor = L(p, E.Directory); if (anchor == 0 && L(s, Files) != 0) anchor = L(L(s, Files), 0);
        var previous = DesktopDos.Current(dos, anchor);
        uint directory;
        if (split == 0) directory = anchor == 0 ? DesktopDos.Lock(dos, CString.ToUInt32("SYS:"), -2) : DesktopDos.Duplicate(dos, anchor);
        else
        {
            Copy(w + Input, w + Path, 256); B(w + Path, (int)split, 0);
            directory = DesktopDos.Lock(dos, w + Path, -2);
        }
        var error = directory == 0 ? Error(dos) : 0;
        DesktopDos.Current(dos, previous); L(w, Directory, directory);
        if (error == 0 && DesktopDos.Examine(dos, directory, w + Fib) == 0) error = Error(dos);
        if (error == 0 && (int)L(w, Fib + 4) <= 0) error = 212;
        if (error == 0 && DesktopDos.Name(dos, directory, w + Path, 256) == 0) error = Error(dos);
        if (error != 0) { Failed(s, error); return; }
        if (!saveAs) { Execute(s); return; }
        previous = DesktopDos.Current(dos, directory);
        var target = DesktopDos.Lock(dos, w + Target, -2);
        error = target == 0 ? DesktopDos.Error(dos) : 0;
        if (target != 0) DesktopDos.Unlock(dos, target);
        if (target == 0 && error == 205) error = 0;
        else if (target != 0)
        {
            var snapshot = Allocate(DesktopProfile.EditorSnapshotBytes);
            if (snapshot == 0) error = 103;
            else { L(w, Expected, snapshot); L(w, OwnExpected, 1); error = Capture(s, dos, w + Target, snapshot, w + Fib, true); }
        }
        DesktopDos.Current(dos, previous);
        if (error != 0) { Failed(s, error); return; }
        if (target != 0) { L(s, EditorPrompt, 3); Changed(s); }
        else Execute(s);
    }

    private static void Child(uint w, int at, CString leaf)
    {
        Copy(w + Drawer, w + (uint)at, 32); var n = Length(w + Drawer, 31);
        B(w + (uint)at, (int)n, (byte)'/'); Copy(CString.ToUInt32(leaf), w + (uint)at + n + 1, 16);
    }
    private static uint WriteFile(uint s, uint dos, uint name, uint data, uint length)
    {
        var file = DesktopDos.Open(dos, name, 1006); if (file == 0) return Error(dos);
        uint at = 0, error = 0;
        while (at < length)
        {
            var count = length - at; if (count > 1024) count = 1024;
            var wrote = DesktopDos.Write(dos, file, data + at, (int)count);
            if (wrote <= 0 || (uint)wrote > count) { error = Error(dos); break; }
            at += (uint)wrote; DesktopBusy.Poll(s);
        }
        if (DesktopDos.Close(dos, file) == 0 && error == 0) error = Error(dos);
        return error;
    }
    private static uint Matches(uint s, uint dos, uint name, uint expected, uint fresh, uint fib)
    {
        var error = Capture(s, dos, name, fresh, fib, true); if (error != 0) return error;
        var same = L(expected, 4) == 0 || DesktopDos.Same(dos, L(expected, 4), L(fresh, 4)) == 0; // LOCK_SAME
        var matches = same && Metadata(expected + 8, fresh + 8) && L(expected, 0) == L(fresh, 0) &&
            Equal(expected + SnapshotData, fresh + SnapshotData, L(expected, 0));
        DropLock(dos, fresh); return matches ? 0u : 1009;
    }

    private static void Execute(uint s)
    {
        var w = L(s, SaveState); var dos = L(s, Dos); var p = L(s, Document);
        var expected = L(w, Expected);
        // Replacement must not bypass a file's write protection by using
        // Rename instead of writing its existing handle.
        if (expected != 0 && (L(expected, 8 + 116) & 4) != 0) { Failed(s, 223); return; }
        var next = Allocate(DesktopProfile.EditorSnapshotBytes);
        if (next == 0) { Failed(s, 103); return; }
        L(w, Next, next);
        var previous = DesktopDos.Current(dos, L(w, Directory));
        uint error = 0;
        // CreateDir claims a fresh namespace; Open never truncates another
        // process's temporary file after a check-then-create race.
        Copy(CString.ToUInt32("CopperSave0000"), w + Drawer, 32);
        for (var attempt = 0u; attempt < 32; attempt++)
        {
            B(w + Drawer, 12, (byte)('0' + attempt / 10)); B(w + Drawer, 13, (byte)('0' + attempt % 10));
            var handle = DesktopDos.CreateDirectory(dos, w + Drawer);
            if (handle != 0) { L(w, DrawerLock, handle); error = 0; break; }
            error = Error(dos); if (error != 203) break;
            DesktopBusy.Poll(s);
        }
        if (L(w, DrawerLock) == 0) { DesktopDos.Current(dos, previous); Failed(s, error); return; }
        Child(w, NewPath, "New"); Child(w, OldPath, "Original"); Child(w, Manifest, "Target");
        L(w, Stage, 1);
        DesktopBusy.Poll(s);
        error = WriteFile(s, dos, w + NewPath, p + E.Data, L(p, E.Length));
        if (error == 0 && expected != 0)
        {
            if (B(expected, 8 + 144) != 0 && DesktopDos.Comment(dos, w + NewPath, expected + 8 + 144) == 0) error = Error(dos);
            if (error == 0 && L(expected, 8 + 116) != 0 && DesktopDos.Protection(dos, w + NewPath, L(expected, 8 + 116)) == 0) error = Error(dos);
        }
        if (error == 0) error = Capture(s, dos, w + NewPath, next, w + Fib);
        if (error == 0 && (L(next, 0) != L(p, E.Length) || !Equal(next + SnapshotData, p + E.Data, L(p, E.Length)))) error = 1005;
        // Target is published only after New has closed and read back exactly.
        // It names the destination and also marks the verified recovery copy.
        if (error == 0) { L(w, Stage, 2); error = WriteFile(s, dos, w + Manifest, w + Target, Length(w + Target, 107)); }
        if (error == 0) L(w, Stage, 3);
        // Verify the source immediately before moving it, and verify the moved
        // object again. An intervening replacement is restored, never deleted.
        // The fresh comparison uses its own buffer; Next remains the new baseline.
        var check = error == 0 && expected != 0 ? Allocate(DesktopProfile.EditorSnapshotBytes) : 0;
        if (error == 0 && expected != 0 && check == 0) error = 103;
        if (error == 0 && expected != 0) error = Matches(s, dos, w + Target, expected, check, w + Fib);
        if (error == 0 && expected != 0)
        {
            // DOS cannot rename an in-use file. This is the only interval in
            // which the baseline's identity lock is released before commit.
            var original = L(p, E.Original);
            if (original != 0 && original != expected && L(original, 4) != 0 &&
                DesktopDos.Same(dos, L(original, 4), L(expected, 4)) == 0)
            { DropLock(dos, original); L(w, 32, 1); }
            DropLock(dos, expected);
            if (DesktopDos.Rename(dos, w + Target, w + OldPath) == 0) error = Error(dos);
            else { L(w, Stage, 4); error = Matches(s, dos, w + OldPath, expected, check, w + Fib); }
        }
        FreeSnapshot(dos, check);
        if (error == 0 && DesktopDos.Rename(dos, w + NewPath, w + Target) == 0) error = Error(dos);
        if (error != 0)
        {
            if (L(w, Stage) == 4)
            {
                if (DesktopDos.Rename(dos, w + OldPath, w + Target) == 0) error = 1012;
                else L(w, Stage, 3);
            }
            // A failed restoration leaves the original in the recovery drawer.
            // Do not lock that archive as though it were still the source:
            // recovery tools must be able to move it back. Ordinary Save then
            // requires reopening or an explicit Save As overwrite decision.
            if (expected != 0 && L(expected, 4) == 0 && L(w, Stage) != 4)
                L(expected, 4, DesktopDos.Lock(dos, w + Target, -2));
            if (L(w, 32) != 0 && L(expected, 4) != 0)
                L(L(p, E.Original), 4, DesktopDos.Duplicate(dos, L(expected, 4)));
            DesktopDos.Current(dos, previous); Failed(s, error); return;
        }
        L(w, Stage, 5);
        L(next, 4, DesktopDos.Lock(dos, w + Target, -2));
        if (L(next, 4) == 0) error = 1013;
        // Successful promotion is the commit point. Backup cleanup has a
        // separate warning: the saved document already exists and is complete.
        if (expected != 0 && DesktopDos.Delete(dos, w + OldPath) == 0) error = 1011;
        if (error == 0 && DesktopDos.Delete(dos, w + Manifest) == 0) error = 1011;
        DesktopDos.Unlock(dos, L(w, DrawerLock)); L(w, DrawerLock, 0);
        if (error == 0 && DesktopDos.Delete(dos, w + Drawer) == 0) error = 1011;
        DesktopDos.Current(dos, previous);
        FreeSnapshot(dos, L(p, E.Original));
        L(p, E.Original, next); L(w, Next, 0);
        DesktopDos.Unlock(dos, L(p, E.Directory)); L(p, E.Directory, L(w, Directory)); L(w, Directory, 0);
        Copy(w + Target, p + E.Name, 108); Copy(w + Path, p + E.Path, 256);
        L(p, E.Modified, 0); L(s, EditorError, error); L(s, EditorPrompt, 0); Changed(s);
        if (error == 0) Release(s);
        EditorView.SaveCompleted(s);
    }
}
