using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>Owns public background-child observations for one Shell process.</summary>
internal static class PublicShellContinuationCore
{
    private const uint Bytes = 68;
    private const uint Magic = 0x53434f57; // SCOW
    private const int Next = 0, Record = 4, Cli = 8, Done = 12;
    private const int Result = 16, IoError = 20, Attached = 24, Marker = 28, Name = 32;
    private const int OwnedFrame = 64;
    private const uint MaximumEntries = 4096;

    public static bool Prepare(ref DosShellNativePlatform shell, APTR cli, APTR record,
        out APTR owner, out APTR reservation, out DOS.Error failure)
    {
        owner = APTR.Null;
        reservation = APTR.Null;
        failure = DOS.Error.ObjectWrongType;
        if (!ShellProcessState.TryGetContinuationHead(ref shell, cli, out var present, out var head) ||
            !present || !ShellProcessContinuationCodec.TryRead(ref shell, record, out var value) ||
            (value.ParentCli.IsNotNull && value.ParentCli != cli) ||
            value.State != ShellProcessContinuationState.Running ||
            (value.Flags & (uint)ShellProcessContinuationFlags.ResourcesClosed) != 0 ||
            !Find(ref shell, cli, record, out _, out var existing) || existing.IsNotNull)
            return false;
        var ownedFrame = APTR.Null;
        if ((value.Flags & (uint)ShellProcessContinuationFlags.RecordOwned) != 0 &&
            (!shell.TryFindFrame(cli, out ownedFrame) ||
             !ShellFrameResources.IsContinuationOwned(ref shell, ownedFrame, record))) return false;
        var node = APTR.FromPointer(global::Amiga.Exec.AllocMem(Bytes,
            global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
        if (node.IsNull) { failure = DOS.Error.NoFreeStore; return false; }
        var name = APTR.FromPointer(node.Raw + Name);
        if (!PublicShellTaskIdentity.TryReserve(node, name, PublicShellTaskIdentity.NameBytes, out reservation))
        {
            global::Amiga.Exec.FreeMem(node.Raw, Bytes);
            failure = DOS.Error.NoFreeStore;
            return false;
        }
        APTR.WriteUInt32(node, Next, head.Raw);
        APTR.WriteUInt32(node, Record, record.Raw);
        APTR.WriteUInt32(node, Cli, cli.Raw);
        APTR.WriteUInt32(node, Attached, 1);
        APTR.WriteUInt32(node, Marker, Magic);
        APTR.WriteUInt32(node, OwnedFrame, ownedFrame.Raw);
        if (!ShellProcessState.TrySetContinuationHead(ref shell, cli, node))
        {
            PublicShellTaskIdentity.Release(reservation);
            global::Amiga.Exec.FreeMem(node.Raw, Bytes);
            reservation = APTR.Null;
            return false;
        }
        owner = node;
        failure = DOS.Error.None;
        return true;
    }

    public static APTR TaskName(APTR owner) => APTR.FromPointer(owner.Raw + Name);

    /// <summary>Rolls back publication while no child can access the owner.</summary>
    public static bool AbortLaunch(ref DosShellNativePlatform shell, APTR cli,
        APTR owner, APTR reservation)
    {
        if (!Unlink(ref shell, cli, owner)) return false;
        PublicShellTaskIdentity.Release(reservation);
        global::Amiga.Exec.FreeMem(owner.Raw, Bytes);
        return true;
    }

    /// <summary>Publishes the worker outcome after its Shell resources are released.</summary>
    public static void Complete(APTR owner, int result, int ioError)
    {
        if (owner.IsNull) return;
        global::Amiga.Exec.Forbid();
        APTR.WriteUInt32(owner, Result, unchecked((uint)result));
        APTR.WriteUInt32(owner, IoError, unchecked((uint)ioError));
        APTR.WriteUInt32(owner, Done, 1);
        var detached = APTR.ReadUInt32(owner, Attached) == 0;
        if (detached) APTR.WriteUInt32(owner, Marker, 0);
        global::Amiga.Exec.Permit();
        if (detached) global::Amiga.Exec.FreeMem(owner.Raw, Bytes);
    }

    public static bool Poll(ref DosShellNativePlatform shell, APTR cli, APTR record,
        out bool found, out ShellProcessContinuationState state, out int result, out int ioError)
    {
        found = false;
        state = ShellProcessContinuationState.Failed;
        result = (int)ShellCommandResult.Error;
        ioError = 0;
        if (!Find(ref shell, cli, record, out _, out var owner)) return false;
        if (owner.IsNull) return true;
        found = true;
        global::Amiga.Exec.Forbid();
        var done = APTR.ReadUInt32(owner, Done) != 0;
        var live = done && global::Amiga.Exec.FindTask(CString.FromPointer(TaskName(owner).Raw)).IsNotNull;
        if (done && !live)
        {
            result = unchecked((int)APTR.ReadUInt32(owner, Result));
            ioError = unchecked((int)APTR.ReadUInt32(owner, IoError));
        }
        global::Amiga.Exec.Permit();
        if (!done || live)
        {
            state = ShellProcessContinuationState.Running;
            result = 0;
            return true;
        }
        if (!ShellProcessContinuationCodec.TryRead(ref shell, record, out var current) ||
            !ShellProcessContinuationCodec.TrySetFlags(ref shell, record,
                (ShellProcessContinuationFlags)(current.Flags | (uint)ShellProcessContinuationFlags.ResourcesClosed)))
            return false;
        state = ShellProcessContinuationState.Completed;
        return true;
    }

    public static bool Release(ref DosShellNativePlatform shell, APTR cli, APTR record, uint flags,
        out bool found)
    {
        found = false;
        if (!Find(ref shell, cli, record, out _, out var owner)) return false;
        if (owner.IsNull) return true;
        found = true;
        if (!Poll(ref shell, cli, record, out var observed, out var state, out var result, out _) ||
            !observed || state != ShellProcessContinuationState.Completed ||
            !ShellProcessContinuationCodec.TryRead(ref shell, record, out var current) ||
            current.State is ShellProcessContinuationState.Pending or ShellProcessContinuationState.Running ||
            current.State != ShellProcessContinuationState.Completed || current.Result != result ||
            (current.Flags & (uint)ShellProcessContinuationFlags.ResourcesClosed) == 0 ||
            flags != (current.Flags & ~(uint)ShellProcessContinuationFlags.ResourcesClosed) ||
            !CanFreeOwnedRecord(ref shell, owner) ||
            !Unlink(ref shell, cli, owner)) return false;
        if (!FreeOwnedRecord(ref shell, owner)) return false;
        APTR.WriteUInt32(owner, Marker, 0);
        global::Amiga.Exec.FreeMem(owner.Raw, Bytes);
        return true;
    }

    /// <summary>Detaches the parent; the worker releases any still-live owner.</summary>
    public static bool DetachAll(ref DosShellNativePlatform shell, APTR cli)
    {
        if (!ShellProcessState.TryGetContinuationHead(ref shell, cli, out var present, out var head) || !present)
            return false;
        // Validate the entire chain before removing its discoverable head.
        var current = head;
        for (var count = 0u; current.IsNotNull; count++)
        {
            if (count == MaximumEntries || !Valid(ref shell, cli, current) ||
                !CanFreeOwnedRecord(ref shell, current)) return false;
            current = APTR.FromPointer(APTR.ReadUInt32(current, Next));
        }
        if (!ShellProcessState.TrySetContinuationHead(ref shell, cli, APTR.Null)) return false;
        current = head;
        while (current.IsNotNull)
        {
            var next = APTR.FromPointer(APTR.ReadUInt32(current, Next));
            if (!FreeOwnedRecord(ref shell, current)) return false;
            global::Amiga.Exec.Forbid();
            APTR.WriteUInt32(current, Attached, 0);
            var done = APTR.ReadUInt32(current, Done) != 0;
            if (done) APTR.WriteUInt32(current, Marker, 0);
            global::Amiga.Exec.Permit();
            if (done) global::Amiga.Exec.FreeMem(current.Raw, Bytes);
            current = next;
        }
        return true;
    }

    /// <summary>Detaches one abandoned frame and releases its owned record.</summary>
    public static bool Detach(ref DosShellNativePlatform shell, APTR cli, APTR record)
    {
        if (!Find(ref shell, cli, record, out _, out var owner) || owner.IsNull ||
            !CanFreeOwnedRecord(ref shell, owner) ||
            !Unlink(ref shell, cli, owner)) return false;
        if (!FreeOwnedRecord(ref shell, owner)) return false;
        global::Amiga.Exec.Forbid();
        APTR.WriteUInt32(owner, Attached, 0);
        var done = APTR.ReadUInt32(owner, Done) != 0;
        if (done) APTR.WriteUInt32(owner, Marker, 0);
        global::Amiga.Exec.Permit();
        if (done) global::Amiga.Exec.FreeMem(owner.Raw, Bytes);
        return true;
    }

    /// <summary>Detaches owned observations even if their pending-frame publication failed.</summary>
    public static bool DetachFrame(ref DosShellNativePlatform shell, APTR cli, APTR frame)
    {
        if (frame.IsNull || !ShellProcessState.TryGetContinuationHead(ref shell, cli,
                out var present, out var current)) return false;
        if (!present) return true;
        // Validate before mutating, including owners belonging to other frames.
        for (var count = 0u; current.IsNotNull; count++)
        {
            if (count == MaximumEntries || !Valid(ref shell, cli, current) ||
                !CanFreeOwnedRecord(ref shell, current)) return false;
            current = APTR.FromPointer(APTR.ReadUInt32(current, Next));
        }
        if (!ShellProcessState.TryGetContinuationHead(ref shell, cli, out present, out current) || !present)
            return false;
        while (current.IsNotNull)
        {
            var next = APTR.FromPointer(APTR.ReadUInt32(current, Next));
            if (APTR.ReadUInt32(current, OwnedFrame) == frame.Raw &&
                !Detach(ref shell, cli, APTR.FromPointer(APTR.ReadUInt32(current, Record)))) return false;
            current = next;
        }
        return true;
    }

    private static bool Find(ref DosShellNativePlatform shell, APTR cli, APTR record,
        out APTR previous, out APTR found)
    {
        previous = APTR.Null;
        found = APTR.Null;
        if (record.IsNull || !ShellProcessState.TryGetContinuationHead(ref shell, cli, out _, out var current))
            return false;
        for (var count = 0u; current.IsNotNull; count++)
        {
            if (count == MaximumEntries || !Valid(ref shell, cli, current)) return false;
            if (APTR.ReadUInt32(current, Record) == record.Raw) { found = current; return true; }
            previous = current;
            current = APTR.FromPointer(APTR.ReadUInt32(current, Next));
        }
        return true;
    }

    private static bool Unlink(ref DosShellNativePlatform shell, APTR cli, APTR owner)
    {
        if (owner.IsNull || !Valid(ref shell, cli, owner) ||
            !Find(ref shell, cli, APTR.FromPointer(APTR.ReadUInt32(owner, Record)), out var previous, out var found) ||
            found != owner) return false;
        var next = APTR.FromPointer(APTR.ReadUInt32(owner, Next));
        if (previous.IsNull) return ShellProcessState.TrySetContinuationHead(ref shell, cli, next);
        APTR.WriteUInt32(previous, Next, next.Raw);
        return true;
    }

    private static bool Valid(ref DosShellNativePlatform shell, APTR cli, APTR owner) =>
        owner.IsNotNull && (owner.Raw & 3) == 0 && owner.Raw <= uint.MaxValue - Bytes &&
        shell.IsRunnerBindingMemory(owner, Bytes) && APTR.ReadUInt32(owner, Marker) == Magic &&
        APTR.ReadUInt32(owner, Cli) == cli.Raw && APTR.ReadUInt32(owner, Attached) == 1;

    private static bool CanFreeOwnedRecord(ref DosShellNativePlatform shell, APTR owner)
    {
        var frame = APTR.FromPointer(APTR.ReadUInt32(owner, OwnedFrame));
        var record = APTR.FromPointer(APTR.ReadUInt32(owner, Record));
        return frame.IsNull || ShellFrameResources.IsContinuationOwned(ref shell, frame, record);
    }

    private static bool FreeOwnedRecord(ref DosShellNativePlatform shell, APTR owner)
    {
        var frame = APTR.FromPointer(APTR.ReadUInt32(owner, OwnedFrame));
        var record = APTR.FromPointer(APTR.ReadUInt32(owner, Record));
        return frame.IsNull || ShellFrameResources.FreeContinuation(ref shell, frame, record);
    }
}

[AmigaLibrary("dos.library::shell-continuation", AmigaLibraryBasePolicy.CallerProvided)]
internal static class PublicShellContinuationDosVectors
{
    [AmigaLvo(DosLvo.Delay)]
    internal static extern void Delay([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] uint ticks);
}
