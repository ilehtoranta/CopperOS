using Amiga;
using CopperOS.Shell;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Shell.Runner.NativeRoot;

/// <summary>Exercises the shared CLI path transaction using public DOS vectors.</summary>
public static class PathProbe
{
    [M68kEntryPoint]
    public static int Main()
    {
        var exec = APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(4), 0));
        var dos = ExecVectors.OpenLibrary(exec, Text("dos.library"), 37);
        if (dos.IsNull) return 20;
        var result = Run(dos);
        var marker = result == 0 ? Text("DH0:path-ok") : Text("DH0:path-fail");
        var output = Vectors.Open(dos, marker, 1006);
        if (output.IsNotNull) Vectors.Close(dos, output);
        ExecVectors.CloseLibrary(exec, dos);
        return result;
    }

    private static int Run(APTR dos)
    {
        var platform = new PublicShellPathPlatform(dos);
        var cli = platform.Allocate(DosCommandLineInterfaceCodec.Size);
        var text = platform.Allocate(16);
        if (cli.IsNull || text.IsNull) return 20;
        platform.WriteUInt8(text, 0, (byte)'S'); platform.WriteUInt8(text, 1, (byte)'Y');
        platform.WriteUInt8(text, 2, (byte)'S'); platform.WriteUInt8(text, 3, (byte)':');
        platform.WriteUInt8(text, 4, 0);
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var previousWindow = DosProcessCodec.ReadWindowPointer(ref platform, task);
        var ok = ShellCommandPath.TryUpdatePacked(ref platform, cli, text, 5, 1,
            PathCommand.OperationReset, 1) &&
            DosProcessCodec.ReadWindowPointer(ref platform, task) == previousWindow;
        var head = DosCommandLineInterfaceCodec.ReadCommandDirectory(ref platform, cli);
        ok = ok && head.IsNotNull && ShellCommandPath.TryReadNode(ref platform, head,
            out var next, out var directory) && next.IsNull && directory.IsNotNull;
        ok = ok && ShellCommandPath.TryUpdatePacked(ref platform, cli, text, 5, 1,
            PathCommand.OperationAdd, 0);
        head = DosCommandLineInterfaceCodec.ReadCommandDirectory(ref platform, cli);
        ok = ok && head.IsNotNull && ShellCommandPath.TryReadNode(ref platform, head,
            out next, out directory) && next.IsNull;
        var listing = Vectors.Open(dos, Text("DH0:path-list"), 1006);
        if (listing.IsNull) ok = false;
        else
        {
            ok = platform.TryWriteCommandPath(listing, cli, 1) && ok &&
                DosProcessCodec.ReadWindowPointer(ref platform, task) == previousWindow;
            Vectors.Close(dos, listing);
        }
        platform.WriteUInt32(text, 0, 0x5359533a); // SYS:
        platform.WriteUInt32(text, 4, 0x5f5f6d69); // __mi
        platform.WriteUInt32(text, 8, 0x7373696e); // ssin
        platform.WriteUInt32(text, 12, 0x675f5f00); // g__\0
        ok = ok && !ShellCommandPath.TryUpdatePacked(ref platform, cli, text,
            16, 1, PathCommand.OperationAdd, 1) &&
            platform.GetPathError() == (int)DOS.Error.ObjectNotFound &&
            DosCommandLineInterfaceCodec.ReadCommandDirectory(ref platform, cli) == head &&
            DosProcessCodec.ReadWindowPointer(ref platform, task) == previousWindow;
        ok = ok && ShellCommandPath.TryUpdatePacked(ref platform, cli, APTR.Null, 0, 0,
            PathCommand.OperationReset, 0) &&
            DosCommandLineInterfaceCodec.ReadCommandDirectory(ref platform, cli).IsNull;
        ok = CheckDirectory(ref platform, dos, task) && ok;
        var cleanupName = Text("DH0:cleanup-test");
        var cleanupFile = Vectors.Open(dos, cleanupName, 1006);
        if (cleanupFile.IsNull) ok = false;
        else
        {
            var closed = PublicShellFileServices.Close(dos, cleanupFile) != 0;
            var deleted = PublicShellFileServices.Delete(dos, cleanupName) != 0;
            var reopened = Vectors.Open(dos, cleanupName, 1005);
            if (reopened.IsNotNull) Vectors.Close(dos, reopened);
            ok = closed && deleted && reopened.IsNull && ok;
        }
        platform.Free(cli, DosCommandLineInterfaceCodec.Size);
        platform.Free(text, 16);
        return ok ? 0 : 10;
    }

    private static bool CheckDirectory(ref PublicShellPathPlatform platform, APTR dos, APTR task)
    {
        var cli = DosProcessCodec.ReadCommandLineInterface(ref platform, task).Address;
        var previous = DosProcessCodec.ReadCurrentDirectory(ref platform, task);
        var saved = previous.IsNotNull ? platform.DuplicatePathLock(previous) : BPTR.Null;
        if (previous.IsNotNull && saved.IsNull) return false;
        var oldName = platform.Allocate(256);
        var currentName = platform.Allocate(256);
        if (oldName.IsNull || currentName.IsNull)
        {
            if (oldName.IsNotNull) platform.Free(oldName, 256);
            if (currentName.IsNotNull) platform.Free(currentName, 256);
            if (saved.IsNotNull) platform.ReleasePathLock(saved);
            return false;
        }
        var ok = platform.TryGetCurrentDirectory(cli, oldName, 256, out _) &&
            platform.TryChangeCurrentDirectory(cli, Text("SYS:"), 4);
        var changed = ok;
        if (changed)
        {
            var active = DosProcessCodec.ReadCurrentDirectory(ref platform, task);
            ok = active.IsNotNull && platform.TryGetCurrentDirectory(cli, currentName, 256, out var length) &&
                length != 0 && platform.ReadUInt8(currentName, (int)length - 1) == (byte)':';
            ok = !platform.TryChangeCurrentDirectory(cli, Text("SYS:__missing__"), 15) &&
                platform.GetPathError() == (int)DOS.Error.ObjectNotFound &&
                DosProcessCodec.ReadCurrentDirectory(ref platform, task) == active && ok;
            ok = !platform.TryChangeCurrentDirectory(cli, Text("SYS:S/Startup-Sequence"), 22) &&
                platform.GetPathError() == (int)DOS.Error.ObjectWrongType &&
                DosProcessCodec.ReadCurrentDirectory(ref platform, task) == active && ok;
            var replaced = ProbeDirectoryVectors.CurrentDir(dos, saved);
            if (replaced.IsNotNull) platform.ReleasePathLock(replaced);
            ok = ProbeDirectoryVectors.SetCurrentDirName(dos, oldName) != 0 && ok;
        }
        else if (saved.IsNotNull) platform.ReleasePathLock(saved);
        platform.Free(oldName, 256);
        platform.Free(currentName, 256);
        return ok;
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
}

[AmigaLibrary("dos.library::directory-probe", AmigaLibraryBasePolicy.CallerProvided)]
internal static class ProbeDirectoryVectors
{
    [AmigaLvo(DosLvo.CurrentDir)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR CurrentDir([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR directory);
    [AmigaLvo(DosLvo.SetCurrentDirName)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int SetCurrentDirName([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] APTR name);
}

