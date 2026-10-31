using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.SystemImage;

// Original, public-DOS-only persistence fixture. No host callbacks or writes
// into emulator diagnostics. The caller observes real console output.
public static class StorageProbe
{
    private static APTR Text(CString text) => APTR.FromPointer(CString.ToUInt32(text));
    [M68kExport("copperos.system.storage-proof")]
    public static uint Entry()
    {
        var exec = APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(4), 0));
        var dos = ExecVectors.OpenLibrary(exec, Text("dos.library"), 0);
        if (dos.IsNull) return 20;
        var buffer = ExecVectors.AllocMem(exec, 128, 0x10001);
        if (buffer.IsNull) { ExecVectors.CloseLibrary(exec, dos); return 20; }
        var existing = Vectors.Open(dos, Text("S:User-Settings"), 1005);
        var reopened = existing.IsNotNull;
        if (reopened) Vectors.Close(dos, existing);
        var ok = true;
        CString stage = "initial settings lookup\n";
        if (!reopened)
        {
            ok = StorageVectors.IoErr(dos) == 205;
            if (ok) { stage = "write settings\n"; ok = Write(dos, "S:User-Settings", "volume=37\n"); }
            if (ok) { stage = "write data\n"; ok = Write(dos, "S:Persistent-Data", "CopperOS data survives restart\n"); }
            if (ok) { stage = "write scratch\n"; ok = Write(dos, "S:Write-Scratch", "original scratch data\n"); }
            if (ok) { stage = "rename scratch\n"; ok = StorageVectors.Rename(dos, Text("S:Write-Scratch"), Text("S:Renamed-Scratch")) != 0; }
            if (ok) { stage = "delete scratch\n"; ok = StorageVectors.Delete(dos, Text("S:Renamed-Scratch")) != 0; }
            if (ok)
            {
                stage = "create directory\n";
                var dir = StorageVectors.CreateDir(dos, Text("User"));
                ok = dir.IsNotNull;
                if (ok)
                {
                    var old = StorageVectors.CurrentDir(dos, dir);
                    stage = "write relative child\n"; ok = Write(dos, "Inner", "relative workspace data\n");
                    if (ok) { stage = "read relative child\n"; ok = Check(dos, buffer, "Inner", "relative workspace data\n"); }
                    if (ok) { stage = "delete relative child\n"; ok = StorageVectors.Delete(dos, Text("Inner")) != 0; }
                    StorageVectors.CurrentDir(dos, old); StorageVectors.UnLock(dos, dir);
                    if (ok) { stage = "delete directory\n"; ok = StorageVectors.Delete(dos, Text("User")) != 0; }
                }
            }
        }
        if (ok) { stage = "read settings\n"; ok = Check(dos, buffer, "S:User-Settings", "volume=37\n"); }
        if (ok) { stage = "read data\n"; ok = Check(dos, buffer, "S:Persistent-Data", "CopperOS data survives restart\n"); }
        if (ok) SystemBootstrap.Print(dos, reopened ? "CopperOS storage reopen verified\n" : "CopperOS storage created and verified\n");
        else
        {
            var error = StorageVectors.IoErr(dos);
            SystemBootstrap.Print(dos, stage);
            SystemBootstrap.Print(dos, error == 214 ? "CopperOS storage write protected\n" :
                error == 221 ? "CopperOS storage disk full\n" : "CopperOS storage proof failed\n");
        }
        ExecVectors.FreeMem(exec, buffer, 128); ExecVectors.CloseLibrary(exec, dos);
        return ok ? 0u : 20u;
    }
    private static bool Write(APTR dos, CString name, CString text)
    {
        var file = Vectors.Open(dos, Text(name), 1006);
        if (file.IsNull) return false;
        var bytes = Text(text); var length = 0;
        while (APTR.ReadUInt8(bytes, length) != 0) length++;
        var count = Vectors.Write(dos, file, bytes, length);
        var empty = Vectors.Write(dos, file, APTR.Null, 0);
        var closed = Vectors.Close(dos, file);
        return count == length && empty == 0 && closed != 0;
    }
    private static bool Check(APTR dos, APTR buffer, CString name, CString text)
    {
        var file = Vectors.Open(dos, Text(name), 1005);
        if (file.IsNull) return false;
        var count = Vectors.Read(dos, file, buffer, 128); Vectors.Close(dos, file);
        var expected = Text(text); var length = 0;
        while (APTR.ReadUInt8(expected, length) != 0) length++;
        if (count != length) return false;
        for (var at = 0; at < length; at++) if (APTR.ReadUInt8(expected, at) != APTR.ReadUInt8(buffer, at)) return false;
        return true;
    }
}
[AmigaLibrary("dos.library::storage-proof", AmigaLibraryBasePolicy.CallerProvided)]
internal static class StorageVectors
{
    [AmigaLvo(DosLvo.IoErr)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint IoErr([M68kRegister(M68kRegister.A6)] APTR dos);
    [AmigaLvo(DosLvo.Rename)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Rename([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] APTR from, [M68kRegister(M68kRegister.D2)] APTR to);
    [AmigaLvo(DosLvo.DeleteFile)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Delete([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] APTR name);
    [AmigaLvo(DosLvo.CreateDir)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR CreateDir([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] APTR name);
    [AmigaLvo(DosLvo.CurrentDir)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR CurrentDir([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR value);
    [AmigaLvo(DosLvo.UnLock)]
    internal static extern void UnLock([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR value);
}
