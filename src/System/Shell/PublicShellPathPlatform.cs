using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Shell;

/// <summary>Public Exec and DOS services for a CLI path transaction.</summary>
public struct PublicShellPathPlatform : IShellPackedCommandPathPlatform
{
    public PublicShellPathPlatform(APTR dos) => Dos = dos;
    private APTR Dos;
    public APTR Allocate(uint size) => APTR.FromPointer(global::Amiga.Exec.AllocMem(size,
        global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
    public void Free(APTR address, uint size) => global::Amiga.Exec.FreeMem(address.Raw, size);
    // DOS owns cli_CommandDir after publication and retires each PathLock
    // with FreeVec when the process exits. Use the same allocator when we
    // replace a path ourselves, including for DOS-created inherited nodes.
    public APTR AllocatePathNode() => global::Amiga.Exec.AllocVec(PathLock.Size,
        (uint)(global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
    public void FreePathNode(APTR node) => global::Amiga.Exec.FreeVec(node);
    public BPTR DuplicatePathLock(BPTR directory) => PublicShellPathVectors.DupLock(Dos, directory);
    public void ReleasePathLock(BPTR directory)
    {
        var error = GetPathError(); PublicShellPathVectors.UnLock(Dos, directory); SetPathError(error);
    }
    public bool TrySamePathLock(BPTR first, BPTR second, out bool same)
    {
        SetPathError(0);
        var value = PublicShellPathVectors.SameLock(Dos, first, second);
        same = value == 0;
        return GetPathError() == 0 && value is >= -1 and <= 1;
    }
    public bool TryEnterPathRequesters(uint quiet, out APTR process, out APTR previousWindow)
    {
        process = APTR.Null; previousWindow = APTR.Null;
        if (quiet == 0) return true;
        process = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        if (!DosProcessCodec.IsMapped(ref this, process)) return false;
        previousWindow = DosProcessCodec.ReadWindowPointer(ref this, process);
        DosProcessCodec.WriteWindowPointer(ref this, process, APTR.FromPointer(uint.MaxValue));
        return true;
    }
    public void ExitPathRequesters(APTR process, APTR previousWindow)
    {
        if (process.IsNotNull) DosProcessCodec.WriteWindowPointer(ref this, process, previousWindow);
    }
    public BPTR LockPathDirectory(APTR name)
    {
        var info = Allocate(FileInfoBlock.SizeInBytes);
        if (info.IsNull) { SetPathError((int)DOS.Error.NoFreeStore); return BPTR.Null; }
        var directory = PublicShellPathVectors.Lock(Dos, name, -2);
        var valid = directory.IsNotNull && PublicShellPathVectors.Examine(Dos, directory, info) != 0;
        var error = GetPathError();
        if (valid && DosFileInfoBlockCodec.ReadDirEntryType(ref this, info) <= 0)
        { valid = false; error = (int)DOS.Error.ObjectWrongType; }
        Free(info, FileInfoBlock.SizeInBytes);
        if (!valid && directory.IsNotNull) { PublicShellPathVectors.UnLock(Dos, directory); directory = BPTR.Null; }
        SetPathError(valid ? 0 : error);
        return directory;
    }
    public int GetPathError() => PublicShellPathVectors.IoErr(Dos);
    public void SetPathError(int error) => _ = PublicShellPathVectors.SetIoErr(Dos, error);

    /// <summary>Reads the current process's public CLI directory name.</summary>
    public bool TryGetCurrentDirectory(APTR cli, APTR buffer, uint capacity, out uint length)
    {
        length = 0;
        var process = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        if (cli.IsNull || process.IsNull ||
            DosProcessCodec.ReadCommandLineInterface(ref this, process).Address != cli ||
            buffer.IsNull || capacity == 0 || capacity > int.MaxValue || !IsMapped(buffer, capacity))
        { SetPathError((int)DOS.Error.ObjectWrongType); return false; }
        if (PublicShellPathVectors.GetCurrentDirName(Dos, buffer, (int)capacity) == 0) return false;
        while (length < capacity && ReadUInt8(buffer, (int)length) != 0) length++;
        if (length == capacity) { SetPathError((int)DOS.Error.BufferOverflow); return false; }
        SetPathError(0);
        return true;
    }

    /// <summary>Changes the current directory and the CLI's canonical name.</summary>
    public bool TryChangeCurrentDirectory(APTR cli, APTR path, uint length)
    {
        var process = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        if (cli.IsNull || process.IsNull ||
            DosProcessCodec.ReadCommandLineInterface(ref this, process).Address != cli ||
            path.IsNull || length == 0 || length > 65_535 ||
            !IsMapped(path, length + 1) || ReadUInt8(path, (int)length) != 0)
        { SetPathError((int)DOS.Error.ObjectWrongType); return false; }
        var directory = LockPathDirectory(path);
        if (directory.IsNull) return false;
        const uint capacity = 256;
        var canonical = Allocate(capacity);
        if (canonical.IsNull)
        { ReleasePathLock(directory); SetPathError((int)DOS.Error.NoFreeStore); return false; }
        var changed = PublicShellPathVectors.NameFromLock(Dos, directory, canonical, (int)capacity) != 0 &&
            PublicShellPathVectors.SetCurrentDirName(Dos, canonical) != 0;
        var error = GetPathError();
        Free(canonical, capacity);
        if (!changed)
        {
            ReleasePathLock(directory);
            SetPathError(error == 0 ? (int)DOS.Error.ObjectWrongType : error);
            return false;
        }
        var previous = PublicShellPathVectors.CurrentDir(Dos, directory);
        if (previous.IsNotNull) ReleasePathLock(previous);
        SetPathError(0);
        return true;
    }

    /// <summary>Writes the V40 implicit entries and the CLI's live lock list.</summary>
    public bool TryWriteCommandPath(BPTR output, APTR cli, uint quiet)
    {
        if (output.IsNull || cli.IsNull || quiet > 1 ||
            !DosCommandLineInterfaceCodec.IsMapped(ref this, cli) ||
            !TryEnterPathRequesters(quiet, out var process, out var previousWindow))
        { SetPathError((int)DOS.Error.ObjectWrongType); return false; }
        var path = new ShellCommandPathCursor(DosCommandLineInterfaceCodec.ReadCommandDirectory(ref this, cli));
        var wroteHeader = WriteLiteral(output, "Current_directory\n");
        var error = wroteHeader ? 0 : GetPathError();
        if (!wroteHeader && error == 0) error = (int)DOS.Error.ObjectWrongType;
        if (error == 0 && path.Current.IsNotNull) SetPathError(0);
        while (error == 0 && path.Current.IsNotNull)
        {
            if (!ShellCommandPath.TryAdvance(ref this, ref path, out var directory))
            { error = (int)DOS.Error.ObjectWrongType; break; }
            var capacity = 256u;
            var written = false;
            while (capacity <= 65536)
            {
                var buffer = Allocate(capacity + 1);
                if (buffer.IsNull) { error = (int)DOS.Error.NoFreeStore; break; }
                SetPathError(0);
                var named = PublicShellPathVectors.NameFromLock(Dos, directory, buffer, (int)capacity) != 0;
                error = GetPathError();
                if (named)
                {
                    var length = 0u;
                    while (length < capacity && ReadUInt8(buffer, (int)length) != 0) length++;
                    if (length == capacity) error = (int)DOS.Error.BufferOverflow;
                    else
                    {
                        WriteUInt8(buffer, (int)length, (byte)'\n');
                        written = PublicShellPathVectors.Write(Dos, output, buffer, (int)(length + 1)) == (int)(length + 1);
                        error = written ? 0 : GetPathError();
                    }
                }
                Free(buffer, capacity + 1);
                if (written) break;
                if (named || (error != (int)DOS.Error.LineTooLong && error != (int)DOS.Error.BufferOverflow)) break;
                capacity <<= 1;
            }
            if (!written)
            { if (error == 0) error = (int)DOS.Error.ObjectWrongType; break; }
        }
        if (error == 0 && !WriteLiteral(output, "C:\n"))
            error = GetPathError() != 0 ? GetPathError() : (int)DOS.Error.ObjectWrongType;
        ExitPathRequesters(process, previousWindow);
        SetPathError(error);
        return error == 0;
    }
    private bool WriteLiteral(BPTR output, CString text)
    {
        var address = APTR.FromPointer(CString.ToUInt32(text));
        var length = 0;
        while (ReadUInt8(address, length) != 0) length++;
        return PublicShellPathVectors.Write(Dos, output, address, length) == length;
    }
    public bool IsMapped(APTR address, uint size) => address.IsNotNull && address.Raw <= uint.MaxValue - size;
    public byte ReadUInt8(APTR address, int offset = 0) => APTR.ReadUInt8(address, offset);
    public ushort ReadUInt16(APTR address, int offset = 0) => APTR.ReadUInt16(address, offset);
    public uint ReadUInt32(APTR address, int offset = 0) => APTR.ReadUInt32(address, offset);
    public void WriteUInt8(APTR address, int offset, byte value) => APTR.WriteUInt8(address, offset, value);
    public void WriteUInt16(APTR address, int offset, ushort value) => APTR.WriteUInt16(address, offset, value);
    public void WriteUInt32(APTR address, int offset, uint value) => APTR.WriteUInt32(address, offset, value);
    public void Clear(APTR address, uint size) { for (uint i = 0; i < size; i++) WriteUInt8(address, (int)i, 0); }
    public void Copy(APTR source, APTR destination, uint size) { for (uint i = 0; i < size; i++) WriteUInt8(destination, (int)i, ReadUInt8(source, (int)i)); }
}

[AmigaLibrary("dos.library::shell-path-probe", AmigaLibraryBasePolicy.CallerProvided)]
internal static class PublicShellPathVectors
{
    [AmigaLvo(DosLvo.GetCurrentDirName)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int GetCurrentDirName([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] APTR buffer, [M68kRegister(M68kRegister.D2)] int size);
    [AmigaLvo(DosLvo.SetCurrentDirName)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int SetCurrentDirName([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] APTR name);
    [AmigaLvo(DosLvo.CurrentDir)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR CurrentDir([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR directory);
    [AmigaLvo(DosLvo.NameFromLock)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int NameFromLock([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR directory, [M68kRegister(M68kRegister.D2)] APTR buffer, [M68kRegister(M68kRegister.D3)] int size);
    [AmigaLvo(DosLvo.Write)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Write([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR file, [M68kRegister(M68kRegister.D2)] APTR buffer, [M68kRegister(M68kRegister.D3)] int size);
    [AmigaLvo(DosLvo.Lock)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR Lock([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] APTR name, [M68kRegister(M68kRegister.D2)] int mode);
    [AmigaLvo(DosLvo.DupLock)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR DupLock([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR directory);
    [AmigaLvo(DosLvo.UnLock)]
    internal static extern void UnLock([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR directory);
    [AmigaLvo(DosLvo.SameLock)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int SameLock([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR first, [M68kRegister(M68kRegister.D2)] BPTR second);
    [AmigaLvo(DosLvo.Examine)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Examine([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR directory, [M68kRegister(M68kRegister.D2)] APTR info);
    [AmigaLvo(DosLvo.IoErr)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int IoErr([M68kRegister(M68kRegister.A6)] APTR dos);
    [AmigaLvo(DosLvo.SetIoErr)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int SetIoErr([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] int error);
}
