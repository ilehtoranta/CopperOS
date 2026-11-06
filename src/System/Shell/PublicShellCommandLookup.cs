using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using CopperOS.Shell.Dos;

namespace CopperOS.Shell;

/// <summary>Searches the public resident list and the current CLI's path locks.</summary>
internal static class PublicShellCommandLookup
{
    public static bool Find<TMemory>(ref TMemory memory, APTR dos, APTR cli,
        APTR name, uint nameLength, in ShellScriptLookupWorkspace workspace,
        out ShellScriptLookupResult result) where TMemory : struct, IAmigaGuestMemory
        => Find(ref memory, dos, cli, name, nameLength, in workspace, false, out result);

    public static bool Find<TMemory>(ref TMemory memory, APTR dos, APTR cli,
        APTR name, uint nameLength, in ShellScriptLookupWorkspace workspace,
        bool ignoreInternal, out ShellScriptLookupResult result) where TMemory : struct, IAmigaGuestMemory
    {
        result = new ShellScriptLookupResult { Kind = ShellScriptLookupKind.Malformed,
            ResolvedPath = workspace.Path };
        var process = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        if (cli.IsNull || process.IsNull || !DosProcessCodec.IsMapped(ref memory, process) ||
            DosProcessCodec.ReadCommandLineInterface(ref memory, process).Address != cli ||
            !DosCommandLineInterfaceCodec.IsMapped(ref memory, cli) ||
            name.IsNull || nameLength == 0 || nameLength >= uint.MaxValue ||
            name.Raw > uint.MaxValue - nameLength - 1 || !memory.IsMapped(name, nameLength + 1) ||
            memory.ReadUInt8(name, (int)nameLength) != 0 || workspace.Path.IsNull ||
            workspace.Capacity < 2 || workspace.Capacity > int.MaxValue ||
            workspace.Path.Raw > uint.MaxValue - workspace.Capacity ||
            !memory.IsMapped(workspace.Path, workspace.Capacity) || nameLength >= workspace.Capacity)
        {
            PublicShellFileServices.SetError(dos, DOS.Error.ObjectWrongType);
            return false;
        }
        if (PublicShellResidentServices.Contains(ref memory, dos, name, ignoreInternal))
        {
            Copy(ref memory, name, nameLength, workspace.Path);
            result.Kind = ShellScriptLookupKind.Resident;
            result.Origin = ShellScriptLookupOrigin.Resident;
            result.PathLength = nameLength;
            PublicShellFileServices.SetError(dos, DOS.Error.None);
            return true;
        }

        var explicitPath = false;
        for (var i = 0u; i < nameLength; i++)
            if (memory.ReadUInt8(name, (int)i) is (byte)':' or (byte)'/') explicitPath = true;
        if (explicitPath)
        {
            Copy(ref memory, name, nameLength, workspace.Path);
            return FinishCandidate(ref memory, dos, workspace.Path, nameLength,
                ShellScriptLookupOrigin.ExplicitFile, ref result);
        }

        // Lock("") duplicates the current directory without changing it.
        var empty = APTR.FromPointer(CString.ToUInt32(""));
        var current = PublicShellFileServices.Lock(dos, empty);
        if (current.IsNotNull)
        {
            var built = BuildPath(ref memory, dos, current, name, nameLength, in workspace, out var length);
            var savedError = PublicShellFileServices.GetError(dos);
            PublicShellFileServices.UnLock(dos, current);
            PublicShellFileServices.SetError(dos, (DOS.Error)savedError);
            if (!built) return false;
            if (!FinishCandidate(ref memory, dos, workspace.Path, length,
                ShellScriptLookupOrigin.CurrentDirectory, ref result)) return false;
        }
        else
        {
            Copy(ref memory, name, nameLength, workspace.Path);
            if (!FinishCandidate(ref memory, dos, workspace.Path, nameLength,
                ShellScriptLookupOrigin.CurrentDirectory, ref result)) return false;
        }
        if (result.Kind != ShellScriptLookupKind.NotFound) return true;

        var path = new ShellCommandPathCursor(DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli));
        while (path.Current.IsNotNull)
        {
            if (!ShellCommandPath.TryAdvance(ref memory, ref path, out var directory))
            {
                result.Kind = ShellScriptLookupKind.Malformed;
                PublicShellFileServices.SetError(dos, DOS.Error.ObjectWrongType);
                return false;
            }
            if (!BuildPath(ref memory, dos, directory, name, nameLength, in workspace, out var length) ||
                !FinishCandidate(ref memory, dos, workspace.Path, length,
                    ShellScriptLookupOrigin.CommandPath, ref result)) return false;
            if (result.Kind != ShellScriptLookupKind.NotFound) return true;
        }
        // V40 always searches C: after the caller's lock list, even after
        // PATH RESET removes every explicit node from cli_CommandDir.
        if (workspace.Capacity - nameLength <= 2)
        {
            PublicShellFileServices.SetError(dos, DOS.Error.BufferOverflow);
            return false;
        }
        memory.WriteUInt8(workspace.Path, 0, (byte)'C');
        memory.WriteUInt8(workspace.Path, 1, (byte)':');
        for (var i = 0u; i < nameLength; i++)
            memory.WriteUInt8(workspace.Path, (int)(i + 2), memory.ReadUInt8(name, (int)i));
        memory.WriteUInt8(workspace.Path, (int)(nameLength + 2), 0);
        if (!FinishCandidate(ref memory, dos, workspace.Path, nameLength + 2,
                ShellScriptLookupOrigin.CommandPath, ref result)) return false;
        if (result.Kind != ShellScriptLookupKind.NotFound) return true;
        PublicShellFileServices.SetError(dos, DOS.Error.ObjectNotFound);
        return true;
    }

    private static bool BuildPath<TMemory>(ref TMemory memory, APTR dos, BPTR directory,
        APTR name, uint nameLength, in ShellScriptLookupWorkspace workspace, out uint length)
        where TMemory : struct, IAmigaGuestMemory
    {
        length = 0;
        if (PublicShellPathVectors.NameFromLock(dos, directory, workspace.Path,
            (int)workspace.Capacity) == 0) return false;
        while (length < workspace.Capacity && memory.ReadUInt8(workspace.Path, (int)length) != 0) length++;
        if (length == 0 || length >= workspace.Capacity)
        { PublicShellFileServices.SetError(dos, DOS.Error.BufferOverflow); return false; }
        var tail = memory.ReadUInt8(workspace.Path, (int)length - 1);
        var separator = tail is (byte)':' or (byte)'/' ? 0u : 1u;
        if (nameLength >= workspace.Capacity - length || separator >= workspace.Capacity - length - nameLength)
        { PublicShellFileServices.SetError(dos, DOS.Error.BufferOverflow); return false; }
        if (separator != 0) memory.WriteUInt8(workspace.Path, (int)length++, (byte)'/');
        for (var i = 0u; i < nameLength; i++)
            memory.WriteUInt8(workspace.Path, (int)(length + i), memory.ReadUInt8(name, (int)i));
        length += nameLength;
        memory.WriteUInt8(workspace.Path, (int)length, 0);
        return true;
    }

    private static bool FinishCandidate<TMemory>(ref TMemory memory, APTR dos, APTR path,
        uint length, ShellScriptLookupOrigin origin, ref ShellScriptLookupResult result)
        where TMemory : struct, IAmigaGuestMemory
    {
        result.Kind = ShellScriptLookupKind.NotFound;
        result.Origin = ShellScriptLookupOrigin.None;
        result.PathLength = 0;
        var file = PublicShellFileServices.OpenInput(dos, path, length);
        if (file.IsNull) return true;
        var info = APTR.FromPointer(global::Amiga.Exec.AllocMem(FileInfoBlock.SizeInBytes,
            global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
        if (info.IsNull)
        {
            _ = PublicShellFileServices.Close(dos, file);
            PublicShellFileServices.SetError(dos, DOS.Error.NoFreeStore);
            result.Kind = ShellScriptLookupKind.Malformed;
            return false;
        }
        var examined = PublicShellLookupVectors.ExamineFH(dos, file, info) != 0;
        var error = PublicShellFileServices.GetError(dos);
        var protection = examined ? (FileProtection)DosFileInfoBlockCodec.ReadProtection(ref memory, info) : 0;
        var closed = PublicShellFileServices.Close(dos, file) != 0;
        global::Amiga.Exec.FreeMem(info.Raw, FileInfoBlock.SizeInBytes);
        if (!examined || !closed)
        {
            if (!examined) PublicShellFileServices.SetError(dos, (DOS.Error)error);
            result.Kind = ShellScriptLookupKind.Malformed;
            return false;
        }
        result.Protection = protection;
        result.PathLength = length;
        result.Origin = origin;
        result.Kind = (protection & FileProtection.Script) != 0 ? ShellScriptLookupKind.Script :
            origin == ShellScriptLookupOrigin.ExplicitFile ? ShellScriptLookupKind.ExplicitFile :
            origin == ShellScriptLookupOrigin.CurrentDirectory ? ShellScriptLookupKind.CurrentDirectory :
            ShellScriptLookupKind.CommandPath;
        PublicShellFileServices.SetError(dos, DOS.Error.None);
        return true;
    }

    private static void Copy<TMemory>(ref TMemory memory, APTR source, uint length, APTR destination)
        where TMemory : struct, IAmigaGuestMemory
    {
        for (var i = 0u; i < length; i++) memory.WriteUInt8(destination, (int)i, memory.ReadUInt8(source, (int)i));
        memory.WriteUInt8(destination, (int)length, 0);
    }
}

[AmigaLibrary("dos.library::shell-lookup", AmigaLibraryBasePolicy.CallerProvided)]
internal static class PublicShellLookupVectors
{
    [AmigaLvo(DosLvo.SetProtection)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int SetProtection([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR name, [M68kRegister(M68kRegister.D2)] int protection);
    [AmigaLvo(DosLvo.ExamineFH)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int ExamineFH([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] BPTR file, [M68kRegister(M68kRegister.D2)] APTR info);
}
