using Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>Shares Shell-owned stream handles across background descendants.</summary>
internal static class PublicShellStreamLeaseCore
{
    private const uint Magic = 0x534C5345; // SLSE
    private const uint Bytes = 12;

    public static bool TryPrepare(APTR dos, APTR cli, BPTR input, BPTR output,
        BPTR error, uint owned, out ShellStreamLeases leases, out uint created,
        out uint retained, out DOS.Error failure)
    {
        leases = default; created = 0; retained = 0; failure = DOS.Error.ObjectWrongType;
        var shell = new DosShellNativePlatform(APTR.Null);
        if ((owned & ~7u) != 0 ||
            ((owned & 1) != 0 && input.IsNull) ||
            ((owned & 2) != 0 && output.IsNull) ||
            ((owned & 4) != 0 && error.IsNull) ||
            ((owned & 3) == 3 && input == output) ||
            ((owned & 5) == 5 && input == error) ||
            ((owned & 6) == 6 && output == error) ||
            !ShellProcessState.TryGetStreamLeases(ref shell, cli, out var parent))
            return false;
        var memory = new CopperSharpNativeDosPlatform();
        if (!ValidOptional(ref memory, parent.Input) || !ValidOptional(ref memory, parent.Output) ||
            !ValidOptional(ref memory, parent.Error)) return false;
        if ((owned & 1) != 0)
        {
            leases.Input = New(input);
            if (leases.Input.IsNull) { failure = DOS.Error.NoFreeStore; return false; }
            created |= 1;
        }
        if ((owned & 2) != 0)
        {
            leases.Output = New(output);
            if (leases.Output.IsNull) { Abort(dos, in leases, created, retained); failure = DOS.Error.NoFreeStore; return false; }
            created |= 2;
        }
        if ((owned & 4) != 0)
        {
            leases.Error = New(error);
            if (leases.Error.IsNull) { Abort(dos, in leases, created, retained); failure = DOS.Error.NoFreeStore; return false; }
            created |= 4;
        }
        if (leases.Input.IsNull) leases.Input = Find(input, in leases, in parent);
        if (leases.Output.IsNull) leases.Output = Find(output, in leases, in parent);
        if (leases.Error.IsNull) leases.Error = Find(error, in leases, in parent);
        if (leases.Input.IsNotNull && !IsCreated(leases.Input, in leases, created))
        {
            if (!Retain(ref memory, leases.Input)) { Abort(dos, in leases, created, retained); return false; }
            retained |= 1;
        }
        if (leases.Output.IsNotNull && leases.Output != leases.Input && !IsCreated(leases.Output, in leases, created))
        {
            if (!Retain(ref memory, leases.Output)) { Abort(dos, in leases, created, retained); return false; }
            retained |= 2;
        }
        if (leases.Error.IsNotNull && leases.Error != leases.Input && leases.Error != leases.Output &&
            !IsCreated(leases.Error, in leases, created))
        {
            if (!Retain(ref memory, leases.Error)) { Abort(dos, in leases, created, retained); return false; }
            retained |= 4;
        }
        failure = DOS.Error.None;
        return true;
    }

    // New leases still belong to the caller until process creation succeeds.
    public static void Abort(APTR dos, in ShellStreamLeases leases, uint created, uint retained)
    {
        if ((retained & 4) != 0) ReleaseOne(dos, leases.Error);
        if ((retained & 2) != 0) ReleaseOne(dos, leases.Output);
        if ((retained & 1) != 0) ReleaseOne(dos, leases.Input);
        if ((created & 4) != 0) FreeNew(leases.Error);
        if ((created & 2) != 0) FreeNew(leases.Output);
        if ((created & 1) != 0) FreeNew(leases.Input);
    }

    public static void Release(APTR dos, in ShellStreamLeases leases)
    {
        if (leases.Error.IsNotNull && leases.Error != leases.Output && leases.Error != leases.Input)
            ReleaseOne(dos, leases.Error);
        if (leases.Output.IsNotNull && leases.Output != leases.Input) ReleaseOne(dos, leases.Output);
        if (leases.Input.IsNotNull) ReleaseOne(dos, leases.Input);
    }

    private static APTR New(BPTR handle)
    {
        if (handle.IsNull) return APTR.Null;
        var lease = APTR.FromPointer(global::Amiga.Exec.AllocMem(Bytes,
            global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
        if (lease.IsNull) return lease;
        APTR.WriteUInt32(lease, 0, Magic);
        APTR.WriteUInt32(lease, 4, 1);
        APTR.WriteUInt32(lease, 8, handle.Raw);
        return lease;
    }

    private static void FreeNew(APTR lease)
    { if (lease.IsNotNull) global::Amiga.Exec.FreeMem(lease.Raw, Bytes); }

    private static bool ValidOptional(ref CopperSharpNativeDosPlatform memory, APTR lease) =>
        lease.IsNull || ((lease.Raw & 3) == 0 && lease.Raw <= uint.MaxValue - Bytes &&
            memory.IsMapped(lease, Bytes) && global::Amiga.Exec.TypeOfMem(lease) != 0 &&
            global::Amiga.Exec.TypeOfMem(APTR.FromPointer(lease.Raw + Bytes - 4)) != 0 &&
            APTR.ReadUInt32(lease, 0) == Magic && APTR.ReadUInt32(lease, 4) != 0);

    private static APTR Find(BPTR handle, in ShellStreamLeases first, in ShellStreamLeases second)
    {
        if (handle.IsNull) return APTR.Null;
        if (Matches(first.Input, handle)) return first.Input;
        if (Matches(first.Output, handle)) return first.Output;
        if (Matches(first.Error, handle)) return first.Error;
        if (Matches(second.Input, handle)) return second.Input;
        if (Matches(second.Output, handle)) return second.Output;
        if (Matches(second.Error, handle)) return second.Error;
        return APTR.Null;
    }

    private static bool Matches(APTR lease, BPTR handle) =>
        lease.IsNotNull && APTR.ReadUInt32(lease, 8) == handle.Raw;

    private static bool IsCreated(APTR lease, in ShellStreamLeases leases, uint created) =>
        ((created & 1) != 0 && lease == leases.Input) ||
        ((created & 2) != 0 && lease == leases.Output) ||
        ((created & 4) != 0 && lease == leases.Error);

    private static bool Retain(ref CopperSharpNativeDosPlatform memory, APTR lease)
    {
        global::Amiga.Exec.Forbid();
        var valid = ValidOptional(ref memory, lease) && lease.IsNotNull;
        var count = valid ? APTR.ReadUInt32(lease, 4) : 0;
        if (count == uint.MaxValue) valid = false;
        if (valid) APTR.WriteUInt32(lease, 4, count + 1);
        global::Amiga.Exec.Permit();
        return valid;
    }

    private static void ReleaseOne(APTR dos, APTR lease)
    {
        var memory = new CopperSharpNativeDosPlatform();
        global::Amiga.Exec.Forbid();
        var valid = lease.IsNotNull && ValidOptional(ref memory, lease);
        var count = valid ? APTR.ReadUInt32(lease, 4) - 1 : 0;
        var handle = valid && count == 0 ? BPTR.FromRaw(APTR.ReadUInt32(lease, 8)) : BPTR.Null;
        if (valid) APTR.WriteUInt32(lease, 4, count);
        if (valid && count == 0) APTR.WriteUInt32(lease, 0, 0);
        global::Amiga.Exec.Permit();
        if (handle.IsNotNull)
        {
            _ = PublicShellFileServices.Close(dos, handle);
            global::Amiga.Exec.FreeMem(lease.Raw, Bytes);
        }
    }
}
