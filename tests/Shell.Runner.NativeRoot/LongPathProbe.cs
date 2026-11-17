using Amiga;
using CopperOS.Shell;
using CopperSharp.Compiler;

namespace CopperOS.Shell.Runner.NativeRoot;

/// <summary>Runs an extended CLI-path ownership transaction on the 68000.</summary>
public static class LongPathProbe
{
    [M68kEntryPoint]
    public static int Main()
    {
        var exec = APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(4), 0));
        var dos = ExecVectors.OpenLibrary(exec, Text("dos.library"), 37);
        if (dos.IsNull) return 20;
        var memory = new LongPathMemory();
        var cli = memory.Allocate(DosCommandLineInterfaceCodec.Size);
        var head = BPTR.Null;
        var ok = cli.IsNotNull;
        for (var n = 300u; ok && n != 0; n--)
        {
            var node = memory.AllocatePathNode();
            if (node.IsNull) { ok = false; break; }
            DosPathLockCodec.Write(ref memory, node, new PathLock { Next = head, Lock = BPTR.FromRaw(n) });
            head = BPTR.FromAddress(node);
        }
        var request = memory.AllocatePathNode();
        if (request.IsNull) ok = false;
        else DosPathLockCodec.Write(ref memory, request, new PathLock { Next = BPTR.Null, Lock = BPTR.FromRaw(301) });
        if (ok)
        {
            DosCommandLineInterfaceCodec.WriteCommandDirectory(ref memory, cli, head);
            ok = ShellCommandPath.TryUpdate(ref memory, cli, BPTR.FromAddress(request), PathCommand.OperationAdd);
            var cursor = new ShellCommandPathCursor(DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli));
            uint count = 0;
            while (ok && cursor.Current.IsNotNull)
            {
                ok = ShellCommandPath.TryAdvance(ref memory, ref cursor, out var directory) &&
                    directory.Raw == count + 1001;
                count++;
            }
            ok = ok && count == 301 && memory.Releases == 300;
            ok = ShellCommandPath.TryUpdate(ref memory, cli, BPTR.Null, PathCommand.OperationReset) &&
                DosCommandLineInterfaceCodec.ReadCommandDirectory(ref memory, cli).IsNull &&
                memory.Releases == 601 && ok;
        }
        if (request.IsNotNull) memory.FreePathNode(request);
        if (cli.IsNotNull) memory.Free(cli, DosCommandLineInterfaceCodec.Size);
        var marker = ok ? Text("SYS:runner-ok") : Text("SYS:runner-fail");
        var file = Vectors.Open(dos, marker, 1006);
        if (file.IsNotNull) Vectors.Close(dos, file);
        ExecVectors.CloseLibrary(exec, dos);
        return ok ? 0 : 10;
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
}

internal struct LongPathMemory : IShellCommandPathPlatform
{
    public uint Releases;
    public APTR Allocate(uint bytes) => APTR.FromPointer(global::Amiga.Exec.AllocMem(bytes,
        global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
    public void Free(APTR address, uint bytes) => global::Amiga.Exec.FreeMem(address.Raw, bytes);
    public APTR AllocatePathNode() => Allocate(PathLock.Size);
    public void FreePathNode(APTR node) => Free(node, PathLock.Size);
    public BPTR DuplicatePathLock(BPTR directory) => BPTR.FromRaw(directory.Raw + 1000);
    public void ReleasePathLock(BPTR directory) { Releases++; }
    public bool TrySamePathLock(BPTR first, BPTR second, out bool same)
    {
        var left = first.Raw >= 1000 ? first.Raw - 1000 : first.Raw;
        var right = second.Raw >= 1000 ? second.Raw - 1000 : second.Raw;
        same = left == right;
        return true;
    }
    public bool IsMapped(APTR address, uint size) => address.IsNotNull && address.Raw <= uint.MaxValue - size;
    public byte ReadUInt8(APTR address, int offset = 0) => APTR.ReadUInt8(address, offset);
    public ushort ReadUInt16(APTR address, int offset = 0) => APTR.ReadUInt16(address, offset);
    public uint ReadUInt32(APTR address, int offset = 0) => APTR.ReadUInt32(address, offset);
    public void WriteUInt8(APTR address, int offset, byte value) => APTR.WriteUInt8(address, offset, value);
    public void WriteUInt16(APTR address, int offset, ushort value) => APTR.WriteUInt16(address, offset, value);
    public void WriteUInt32(APTR address, int offset, uint value) => APTR.WriteUInt32(address, offset, value);
    public void Clear(APTR address, uint size) { for (var i = 0u; i < size; i++) WriteUInt8(address, (int)i, 0); }
    public void Copy(APTR source, APTR destination, uint size)
    { for (var i = 0u; i < size; i++) WriteUInt8(destination, (int)i, ReadUInt8(source, (int)i)); }
}
