using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>Snapshots resident names while the DOS list is protected, then writes outside Forbid.</summary>
internal static class PublicShellResidentListing
{
    private const uint OutputBytes = 1024;
    private const uint OutputHeaderBytes = 16;

    private struct BufferedWriter : IShellResidentListWriter
    {
        // Native generic values use a single four-byte guest pointer.
        public APTR Context;

        public int WriteByte(BPTR handle, byte value)
        {
            var count = APTR.ReadUInt32(Context, 8);
            if (handle.Raw != APTR.ReadUInt32(Context, 4) ||
                (count == OutputBytes && !Drain())) return -1;
            count = APTR.ReadUInt32(Context, 8);
            APTR.WriteUInt8(Context, (int)(OutputHeaderBytes + count), value);
            APTR.WriteUInt32(Context, 8, count + 1);
            return value;
        }
        public bool Drain()
        {
            var count = APTR.ReadUInt32(Context, 8);
            if (count == 0) return true;
            var dos = APTR.FromPointer(APTR.ReadUInt32(Context, 0));
            var output = BPTR.FromRaw(APTR.ReadUInt32(Context, 4));
            if (PublicShellFileServices.Write(dos, output,
                APTR.FromPointer(Context.Raw + OutputHeaderBytes), count) != (int)count) return false;
            APTR.WriteUInt32(Context, 8, 0);
            return true;
        }
        public bool TryGetInternalCommandEnabled(APTR cli, uint commandIdentity, out uint enabled)
        {
            var shell = new DosShellNativePlatform(APTR.Null);
            return shell.TryGetInternalCommandEnabled(cli, commandIdentity, out enabled);
        }
        public bool IsMapped(APTR address, uint size)
        { var memory = new CopperSharpNativeDosPlatform(); return memory.IsMapped(address, size); }
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

    public static bool List(ref DosShellNativePlatform shell, APTR cli, BPTR output, uint includeSystem)
    {
        if (cli.IsNull || output.IsNull || includeSystem > 1) return false;
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return false;
        var memory = new CopperSharpNativeDosPlatform();
        var modern = memory.ReadUInt16(dos, ExecLayout.Library.Version) >= 50 &&
            memory.ReadUInt16(dos, ExecLayout.Library.NegativeSize) >= 1008;
        var tags = APTR.Null;
        var error = DOS.Error.None;
        if (modern)
        {
            tags = APTR.FromPointer(global::Amiga.Exec.AllocMem(72,
                global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
            if (tags.IsNull) error = DOS.Error.NoFreeStore;
            else
            {
                var pattern = APTR.FromPointer(tags.Raw + 40);
                var expression = APTR.FromPointer(CString.ToUInt32("#?"));
                if (ResidentListingDosVectors.ParsePatternNoCase(dos, expression, pattern, 32) < 0)
                    error = (DOS.Error)PublicShellFileServices.GetError(dos);
                memory.WriteUInt32(tags, 0, (uint)DosFindSegmentTag.MatchPattern);
                memory.WriteUInt32(tags, 4, pattern.Raw);
                memory.WriteUInt32(tags, 8, (uint)DosFindSegmentTag.From);
                memory.WriteUInt32(tags, 16, (uint)DosFindSegmentTag.System);
                memory.WriteUInt32(tags, 24, (uint)DosFindSegmentTag.Load);
            }
        }
        var snapshot = APTR.Null;
        var capacity = 2048u;
        var used = 0u;
        while (error == DOS.Error.None)
        {
            snapshot = APTR.FromPointer(global::Amiga.Exec.AllocMem(capacity,
                global::Amiga.Exec.MemoryFlags.Public));
            if (snapshot.IsNull) { error = DOS.Error.NoFreeStore; break; }
            used = 0;
            global::Amiga.Exec.Forbid();
            var status = modern ? CaptureTagged(ref memory, dos, tags, snapshot, capacity, ref used) :
                CaptureV40(ref memory, dos, snapshot, capacity, ref used);
            var capturedError = PublicShellFileServices.GetError(dos);
            global::Amiga.Exec.Permit();
            if (status == 0) break;
            global::Amiga.Exec.FreeMem(snapshot.Raw, capacity);
            snapshot = APTR.Null;
            if (status != 1)
            { error = capturedError == 0 ? DOS.Error.ObjectWrongType : (DOS.Error)capturedError; break; }
            if (capacity > uint.MaxValue / 2) { error = DOS.Error.NoFreeStore; break; }
            capacity *= 2;
        }
        var success = false;
        if (error == DOS.Error.None)
        {
            var buffer = APTR.FromPointer(global::Amiga.Exec.AllocMem(OutputHeaderBytes + OutputBytes,
                global::Amiga.Exec.MemoryFlags.Public));
            if (buffer.IsNull) error = DOS.Error.NoFreeStore;
            else
            {
                // Reconcile any earlier buffered output before the bulk DOS
                // writes, preserving order on a caller-owned output handle.
                APTR.WriteUInt32(buffer, 0, dos.Raw);
                APTR.WriteUInt32(buffer, 4, output.Raw);
                APTR.WriteUInt32(buffer, 8, 0);
                APTR.WriteUInt32(buffer, 12, 0);
                var writer = new BufferedWriter { Context = buffer };
                success = ShellPacketDosVectors.Flush(dos, output) != 0 &&
                    ShellResidentListFormatter.WriteHeader(ref writer, output) &&
                    WriteEntries(ref shell, ref writer, cli, output, snapshot, used, includeSystem) &&
                    writer.Drain();
                var writeError = PublicShellFileServices.GetError(dos);
                global::Amiga.Exec.FreeMem(buffer.Raw, OutputHeaderBytes + OutputBytes);
                PublicShellFileServices.SetError(dos, (DOS.Error)writeError);
            }
            if (!success && error == DOS.Error.None) error = (DOS.Error)PublicShellFileServices.GetError(dos);
        }
        if (snapshot.IsNotNull) global::Amiga.Exec.FreeMem(snapshot.Raw, capacity);
        if (tags.IsNotNull) global::Amiga.Exec.FreeMem(tags.Raw, 72);
        if (!success && error == DOS.Error.None) error = DOS.Error.ObjectWrongType;
        PublicShellFileServices.SetError(dos, error);
        global::Amiga.Exec.CloseLibrary(dos);
        return success;
    }

    // V40 has no public way to enumerate unknown names. Its SDK RootNode and
    // DosInfo layouts expose the resident-list head at di_NetHand. Isolate
    // that compatibility fallback here; V50 uses FindSegmentTagList instead.
    private static int CaptureV40(ref CopperSharpNativeDosPlatform memory, APTR dos,
        APTR snapshot, uint capacity, ref uint used)
    {
        var root = APTR.FromPointer(memory.ReadUInt32(dos, DosLayout.DosLibrary.Root));
        if (root.IsNull || !memory.IsMapped(root, DosLayout.RootNode.Info + 4)) return 2;
        var info = BPTR.FromRaw(memory.ReadUInt32(root, DosLayout.RootNode.Info)).Address;
        if (info.IsNull || !memory.IsMapped(info, DosLayout.DosInfo.NetworkHandler + 4)) return 2;
        var next = BPTR.FromRaw(memory.ReadUInt32(info, DosLayout.DosInfo.NetworkHandler));
        while (next.IsNotNull)
        {
            var entry = next.Address;
            var result = Append(ref memory, entry, snapshot, capacity, ref used);
            if (result != 0) return result;
            next = DosSegmentCodec.ReadNext(ref memory, entry);
        }
        return 0;
    }

    private static int CaptureTagged(ref CopperSharpNativeDosPlatform memory, APTR dos,
        APTR tags, APTR snapshot, uint capacity, ref uint used)
    {
        // The tag API enumerates ordinary and system entries separately.
        // Find the first of each, then follow the public Segment links so
        // entries of the two kinds keep their actual list order.
        memory.WriteUInt32(tags, 12, 0);
        memory.WriteUInt32(tags, 20, 0);
        var ordinary = PublicShellResidentVectors.FindSegmentTagList(dos, tags);
        var error = PublicShellFileServices.GetError(dos);
        if (ordinary.IsNull && error != 0 && error != (int)DOS.Error.ObjectNotFound) return 2;
        memory.WriteUInt32(tags, 20, 1);
        var system = PublicShellResidentVectors.FindSegmentTagList(dos, tags);
        error = PublicShellFileServices.GetError(dos);
        if (system.IsNull && error != 0 && error != (int)DOS.Error.ObjectNotFound) return 2;
        var head = ordinary.IsNotNull ? ordinary : system;
        if (ordinary.IsNotNull && system.IsNotNull && ordinary != system)
        {
            if (!Follows(ref memory, ordinary, system, out var systemFollows)) return 2;
            if (!systemFollows)
            {
                if (!Follows(ref memory, system, ordinary, out var ordinaryFollows) || !ordinaryFollows) return 2;
                head = system;
            }
        }
        while (head.IsNotNull)
        {
            var result = Append(ref memory, head, snapshot, capacity, ref used);
            if (result != 0) return result;
            head = DosSegmentCodec.ReadNext(ref memory, head).Address;
        }
        return 0;
    }

    private static bool Follows(ref CopperSharpNativeDosPlatform memory, APTR first,
        APTR target, out bool found)
    {
        found = false;
        var current = first;
        while (current.IsNotNull)
        {
            if (current == target) { found = true; return true; }
            if (!DosSegmentCodec.IsMapped(ref memory, current)) return false;
            current = DosSegmentCodec.ReadNext(ref memory, current).Address;
        }
        return true;
    }

    // Records are aligned: LONG use count, UBYTE name length, name bytes.
    private static int Append(ref CopperSharpNativeDosPlatform memory, APTR entry,
        APTR snapshot, uint capacity, ref uint used)
    {
        if (!DosSegmentCodec.IsMapped(ref memory, entry)) return 2;
        var length = (uint)memory.ReadUInt8(entry, DosLayout.Segment.Name);
        if (entry.Raw > uint.MaxValue - DosLayout.Segment.Name - length - 1 ||
            !memory.IsMapped(entry, (uint)DosLayout.Segment.Name + length + 1)) return 2;
        var size = (length + 8) & ~3u;
        if (used > capacity || size > capacity - used) return 1;
        var record = APTR.FromPointer(snapshot.Raw + used);
        memory.WriteUInt32(record, 0, unchecked((uint)DosSegmentCodec.ReadUseCount(ref memory, entry)));
        memory.WriteUInt8(record, 4, (byte)length);
        for (var i = 0u; i < length; i++)
            memory.WriteUInt8(record, (int)i + 5, memory.ReadUInt8(entry, (int)i + DosLayout.Segment.Name + 1));
        used += size;
        return 0;
    }

    private static bool WriteEntries(ref DosShellNativePlatform shell, ref BufferedWriter writer,
        APTR cli, BPTR output, APTR snapshot, uint used, uint includeSystem)
    {
        var offset = 0u;
        var wroteInternals = false;
        while (offset < used)
        {
            var record = APTR.FromPointer(snapshot.Raw + offset);
            var count = unchecked((int)shell.ReadUInt32(record));
            var length = (uint)shell.ReadUInt8(record, 4);
            var name = APTR.FromPointer(record.Raw + 5);
            var ownInternal = count == -2 &&
                (ShellInternalCommandResolver.Resolve(ref shell, name, length) != ShellInternalCommand.Unknown ||
                 ShellTextParser.EqualsPacked(ref shell, name, length, 0x2e6b6574, 0) ||
                 ShellTextParser.EqualsPacked(ref shell, name, length, 0x2e627261, 0) ||
                 ShellTextParser.EqualsPacked(ref shell, name, length, 0x2e6b6579, 0));
            // The shared Shell keeps its built-ins in Shell-owned state. Place
            // them where DOS's built-in entries occur, or before the system
            // Shell module when this DOS has no such entries of its own.
            var shellModule = count == -1 && length == 5 &&
                ShellTextParser.EqualsPacked(ref shell, name, length, 0x5368656c, 0x6c000000);
            if (!wroteInternals && (ownInternal || shellModule))
            {
                if (!ShellResidentListFormatter.WriteInternals(ref writer, cli, output)) return false;
                wroteInternals = true;
            }
            var selected = !ownInternal && (count >= 0 || count == -2 || (includeSystem != 0 && count == -1));
            if (selected && !ShellResidentListFormatter.WriteEntry(ref writer, output, name, length, count))
                return false;
            offset += (length + 8) & ~3u;
        }
        return offset == used && (wroteInternals || ShellResidentListFormatter.WriteInternals(ref writer, cli, output));
    }
}

[AmigaLibrary("dos.library::shell-resident-list", AmigaLibraryBasePolicy.CallerProvided)]
internal static class ResidentListingDosVectors
{
    [AmigaLvo(DosLvo.ParsePatternNoCase)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int ParsePatternNoCase([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR source, [M68kRegister(M68kRegister.D2)] APTR target,
        [M68kRegister(M68kRegister.D3)] int size);
}
