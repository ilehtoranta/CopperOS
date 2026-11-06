using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Shell.Dos;

/// <summary>Acquire a public DOS resident Segment for one Shell command.</summary>
internal static class PublicShellResidentServices
{
    public static bool Contains<TMemory>(ref TMemory memory, APTR dos, APTR name,
        bool ignoreInternal = false)
        where TMemory : struct, IAmigaGuestMemory
    {
        global::Amiga.Exec.Forbid();
        var found = PublicShellResidentVectors.FindSegment(dos, name, APTR.Null, 0);
        if (found.IsNull) found = PublicShellResidentVectors.FindSegment(dos, name, APTR.Null, 1);
        var usable = false;
        if (found.IsNotNull && DosSegmentCodec.IsMapped(ref memory, found))
        {
            var count = DosSegmentCodec.ReadUseCount(ref memory, found);
            usable = count != -1 && count > -999 && (!ignoreInternal || count != -2);
        }
        global::Amiga.Exec.Permit();
        return usable;
    }
    public static BPTR Acquire<TMemory>(ref TMemory memory, APTR dos,
        APTR name, out APTR resident) where TMemory : struct, IAmigaGuestMemory
    {
        resident = APTR.Null;
        var needsLoad = false;
        var system = 0;
        var segment = FindAndAcquire(ref memory, dos, name, out resident,
            out needsLoad, out system);
        if (!needsLoad || segment.IsNotNull) return segment;

        // Classic V40 has no deferred resident segments. DOS implementations
        // that expose the V50 tag-list extension may materialize one before
        // we re-find and acquire it under Forbid. The load can perform I/O,
        // so it must not run while scheduling is forbidden.
        if (memory.ReadUInt16(dos, ExecLayout.Library.Version) < 50 ||
            memory.ReadUInt16(dos, ExecLayout.Library.NegativeSize) < 1008)
        {
            PublicShellFileServices.SetError(dos, DOS.Error.InvalidResidentLibrary);
            return BPTR.Null;
        }
        var tags = APTR.FromPointer(global::Amiga.Exec.AllocMem(32,
            global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
        if (tags.IsNull)
        {
            PublicShellFileServices.SetError(dos, DOS.Error.NoFreeStore);
            return BPTR.Null;
        }
        memory.WriteUInt32(tags, 0, (uint)DosFindSegmentTag.Name);
        memory.WriteUInt32(tags, 4, name.Raw);
        memory.WriteUInt32(tags, 8, (uint)DosFindSegmentTag.System);
        memory.WriteUInt32(tags, 12, unchecked((uint)system));
        memory.WriteUInt32(tags, 16, (uint)DosFindSegmentTag.Load);
        memory.WriteUInt32(tags, 20, 1);
        memory.WriteUInt32(tags, 24, 0);
        memory.WriteUInt32(tags, 28, 0);
        var loaded = PublicShellResidentVectors.FindSegmentTagList(dos, tags);
        var error = PublicShellFileServices.GetError(dos);
        global::Amiga.Exec.FreeMem(tags.Raw, 32);
        if (loaded.IsNull)
        {
            PublicShellFileServices.SetError(dos, (DOS.Error)error);
            return BPTR.Null;
        }
        segment = FindAndAcquire(ref memory, dos, name, out resident,
            out needsLoad, out system);
        if (segment.IsNull && needsLoad)
            PublicShellFileServices.SetError(dos, DOS.Error.InvalidResidentLibrary);
        return segment;
    }

    private static BPTR FindAndAcquire<TMemory>(ref TMemory memory,
        APTR dos, APTR name, out APTR resident, out bool needsLoad, out int system)
        where TMemory : struct, IAmigaGuestMemory
    {
        resident = APTR.Null; needsLoad = false; system = 0;
        var result = BPTR.Null;
        var error = DOS.Error.ObjectNotFound;
        global::Amiga.Exec.Forbid();
        var found = PublicShellResidentVectors.FindSegment(dos, name, APTR.Null, 0);
        if (found.IsNull)
        {
            system = 1;
            found = PublicShellResidentVectors.FindSegment(dos, name, APTR.Null, 1);
        }
        if (found.IsNotNull && DosSegmentCodec.IsMapped(ref memory, found))
        {
            var useCount = DosSegmentCodec.ReadUseCount(ref memory, found);
            var candidate = DosSegmentCodec.ReadSegmentList(ref memory, found);
            // -1 is a DOS system module, -999 is disabled; negative command
            // counts are permanent and must never be modified.
            if (useCount == -1 || useCount <= -999 || useCount == int.MaxValue)
                error = DOS.Error.InvalidResidentLibrary;
            else if (candidate.IsNull)
            {
                needsLoad = true;
                error = DOS.Error.InvalidResidentLibrary;
            }
            else
            {
                if (useCount >= 0) DosSegmentCodec.WriteUseCount(ref memory, found, useCount + 1);
                resident = found;
                result = candidate;
                error = DOS.Error.None;
            }
        }
        else if (found.IsNotNull) error = DOS.Error.ObjectWrongType;
        global::Amiga.Exec.Permit();
        PublicShellFileServices.SetError(dos, error);
        return result;
    }

    public static void Release<TMemory>(ref TMemory memory, APTR resident)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (resident.IsNull) return;
        global::Amiga.Exec.Forbid();
        if (DosSegmentCodec.IsMapped(ref memory, resident))
        {
            var count = DosSegmentCodec.ReadUseCount(ref memory, resident);
            if (count > 0) DosSegmentCodec.WriteUseCount(ref memory, resident, count - 1);
        }
        global::Amiga.Exec.Permit();
    }
}

[AmigaLibrary("dos.library::shell-resident", AmigaLibraryBasePolicy.CallerProvided)]
internal static class PublicShellResidentVectors
{
    [AmigaLvo(DosLvo.AddSegment)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int AddSegment([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR name,
        [M68kRegister(M68kRegister.D2)] BPTR segment,
        [M68kRegister(M68kRegister.D3)] int useCount);
    [AmigaLvo(DosLvo.FindSegment)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR FindSegment([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR name,
        [M68kRegister(M68kRegister.D2)] APTR start,
        [M68kRegister(M68kRegister.D3)] int system);
    [AmigaLvo(DosLvo.RemSegment)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int RemSegment([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR segment);
    [AmigaLvo(DosLvo.FindSegmentTagList)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR FindSegmentTagList([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.A0)] APTR tags);
    [AmigaLvo(DosLvo.AddSegmentTagList)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int AddSegmentTagList([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.A0)] APTR tags);
}
