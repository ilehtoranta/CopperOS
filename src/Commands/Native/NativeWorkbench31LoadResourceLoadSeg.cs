using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 LoadResource's one-shot LoadSeg cache and DOS vector hook.
/// The record shape, SameLock matching, one-shot consumption, unmatched
/// delegation, and compare-and-restore teardown follow the selected 40.42
/// HUNK. The resident worker/process that owns this object is a separate
/// integration step; this class does not by itself prove that its code image
/// remains loaded for the lifetime of an installed hook.
/// </summary>
public static class NativeWorkbench31LoadResourceLoadSeg
{
    public const string HookExport = "copperos.workbench31.loadresource.loadseg";
    public const string ServicePortName = "\u00ab LoadResource \u00bb";

    private const int ListBytes = 14;
    private const int SemaphoreOffset = ListBytes;
    private const int ActiveOperationsOffset = 60;
    private const int PreviousLoadSegOffset = 64;
    private const int DosBaseOffset = 68;
    private const int PreviousDosBaseOffset = 72;
    private const int PreviousTaskUserDataOffset = 76;
    private const int OwnerTaskOffset = 80;
    private const uint StateBytes = 84;

    private const int RecordBytes = 16;
    private const int RecordLockOffset = 8;
    private const int RecordSegListOffset = 12;

    /// <summary>
    /// Opens and retains DOS, initializes the cache, and installs the public
    /// DOS LoadSeg vector hook. The service owner must keep this code image
    /// alive until <see cref="TryUninstallWhenIdle"/> succeeds.
    /// Rejects a direct duplicate vector, but cannot detect our hook beneath
    /// a newer patch; worker integration must call this once per installation.
    /// </summary>
    public static bool TryInstall()
    {
        var ownerTask = Exec.FindTask(CString.FromPointer(0));
        if (ownerTask.IsNull || !IsPublishedWorkerTask(ownerTask))
            return false;

        var previousDosBase = DOS.DOSLibraryBase;
        var dosBase = Exec.OpenLibraryRaw(DOS.Name, 39);
        if (dosBase.IsNull)
            return false;

        DOS.DOSLibraryBase = dosBase;
        var state = Exec.AllocVec(StateBytes,
            (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear));
        if (state.IsNull)
        {
            DOS.DOSLibraryBase = previousDosBase;
            Exec.CloseLibrary(dosBase);
            return false;
        }

        InitializeList(state);
        Exec.InitSemaphore(APTR.FromPointer(state.Raw + SemaphoreOffset));
        APTR.WriteUInt32(state, ActiveOperationsOffset, 0);
        APTR.WriteUInt32(state, PreviousLoadSegOffset, 0);
        APTR.WriteUInt32(state, DosBaseOffset, dosBase.Raw);
        APTR.WriteUInt32(state, PreviousDosBaseOffset, previousDosBase.Raw);
        APTR.WriteUInt32(state, PreviousTaskUserDataOffset,
            APTR.ReadUInt32(ownerTask, ExecLayout.Task.UserData));
        APTR.WriteUInt32(state, OwnerTaskOffset, ownerTask.Raw);

        Exec.Forbid();
        var replacement = APTR.ExportAddress(HookExport);
        var previousLoadSeg = Exec.SetFunction(dosBase, DosLvo.LoadSeg,
            replacement);
        if (previousLoadSeg.IsNull || previousLoadSeg.Raw == replacement.Raw)
        {
            // A valid DOS LoadSeg vector has a non-null predecessor. If a
            // supplied vector is invalid, roll back before publishing state.
            if (previousLoadSeg.IsNull)
                Exec.SetFunction(dosBase, DosLvo.LoadSeg, previousLoadSeg);
            // For a direct duplicate, leave the existing hook and state live;
            // saving our own entry as its predecessor would recurse forever.
            Exec.Permit();
            Exec.FreeVec(state);
            DOS.DOSLibraryBase = previousDosBase;
            Exec.CloseLibrary(dosBase);
            return false;
        }
        APTR.WriteUInt32(state, PreviousLoadSegOffset, previousLoadSeg.Raw);
        APTR.WriteUInt32(ownerTask, ExecLayout.Task.UserData, state.Raw);
        Exec.Permit();
        return true;
    }

    /// <summary>
    /// Adds a preloaded segment to the one-shot cache. A successful matching
    /// LoadSeg call consumes the record and transfers the SegList to DOS.
    /// </summary>
    public static bool TryCache(CString path, BPTR segList)
    {
        var state = CurrentWorkerState();
        if (state.IsNull || segList.IsNull)
            return false;

        var record = Exec.AllocVec(RecordBytes, 0);
        if (record.IsNull)
            return false;

        var lock_ = DOS.LockRaw(path, DOS.LockMode.Shared);
        if (lock_.IsNull)
        {
            Exec.FreeVec(record);
            return false;
        }

        APTR.WriteUInt32(record, RecordLockOffset, lock_.Raw);
        APTR.WriteUInt32(record, RecordSegListOffset, segList.Raw);
        Exec.ObtainSemaphore(APTR.FromPointer(state.Raw + SemaphoreOffset));
        Exec.AddTail(state, record);
        Exec.ReleaseSemaphore(APTR.FromPointer(state.Raw + SemaphoreOffset));
        return true;
    }

    /// <summary>
    /// Removes a cached SegList, releases its lock and unloads it. If the hook
    /// already consumed the one-shot record, this operation leaves the segment
    /// loaded because ownership has passed to DOS.
    /// </summary>
    public static bool TryRemove(BPTR segList)
    {
        var state = CurrentWorkerState();
        if (state.IsNull || segList.IsNull)
            return false;

        var semaphore = APTR.FromPointer(state.Raw + SemaphoreOffset);
        Exec.ObtainSemaphore(semaphore);
        var record = FindBySegList(state, segList.Raw);
        if (record.IsNull)
        {
            Exec.ReleaseSemaphore(semaphore);
            return false;
        }

        Exec.Remove(record);
        var lock_ = BPTR.FromRaw(APTR.ReadUInt32(record, RecordLockOffset));
        Exec.ReleaseSemaphore(semaphore);

        DOS.UnLock(lock_);
        Exec.FreeVec(record);
        DOS.UnLoadSeg(segList);
        return true;
    }

    /// <summary>
    /// Attempts the registry semaphore without waiting, then restores the
    /// saved DOS vector only when the registry is empty and no hook invocation
    /// is active. If another vector was installed after ours,
    /// it is immediately put back and our state remains live: the newer hook
    /// may still delegate to ours or restore it during its own teardown.
    /// </summary>
    public static bool TryUninstallWhenIdle()
    {
        var state = CurrentWorkerState();
        var ownerTask = Exec.FindTask(CString.FromPointer(0));
        if (state.IsNull || ownerTask.IsNull ||
            APTR.ReadUInt32(state, OwnerTaskOffset) != ownerTask.Raw)
            return false;
        var dosBase = APTR.FromPointer(APTR.ReadUInt32(state, DosBaseOffset));

        var semaphore = APTR.FromPointer(state.Raw + SemaphoreOffset);
        if (Exec.AttemptSemaphore(semaphore) == 0)
            return false;
        Exec.Forbid();

        var idle = ListIsEmpty(state) &&
            APTR.ReadUInt32(state, ActiveOperationsOffset) == 0;
        if (!idle)
        {
            Exec.Permit();
            Exec.ReleaseSemaphore(semaphore);
            return false;
        }

        var previous = APTR.FromPointer(
            APTR.ReadUInt32(state, PreviousLoadSegOffset));
        var replacement = APTR.ExportAddress(HookExport);
        var displaced = Exec.SetFunction(dosBase, DosLvo.LoadSeg, previous);
        if (displaced.Raw != replacement.Raw)
        {
            Exec.SetFunction(dosBase, DosLvo.LoadSeg, displaced);
            Exec.Permit();
            Exec.ReleaseSemaphore(semaphore);
            return false;
        }

        var previousDosBase = APTR.FromPointer(
            APTR.ReadUInt32(state, PreviousDosBaseOffset));
        var previousTaskUserData = APTR.ReadUInt32(state,
            PreviousTaskUserDataOffset);
        APTR.WriteUInt32(ownerTask, ExecLayout.Task.UserData,
            previousTaskUserData);
        Exec.Permit();
        Exec.ReleaseSemaphore(semaphore);

        Exec.FreeVec(state);
        DOS.DOSLibraryBase = previousDosBase;
        Exec.CloseLibrary(dosBase);
        return true;
    }

    /// <summary>The replacement installed at DOS LVO LoadSeg (-150).</summary>
    [M68kExport(HookExport)]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint LoadSegHook(
        [M68kRegister(M68kRegister.D1)] CString name)
    {
        var state = FindStateAndBeginOperation();
        if (state.IsNull)
            return 0;

        // An exported resident entry has its own library-base context. The
        // worker's initialized DOS slot is not inherited by a vector caller.
        var previousDosBase = DOS.DOSLibraryBase;
        var dosBase = APTR.FromPointer(APTR.ReadUInt32(state, DosBaseOffset));
        DOS.DOSLibraryBase = dosBase;

        var inputLock = DOS.LockRaw(name, DOS.LockMode.Shared);
        var semaphore = APTR.FromPointer(state.Raw + SemaphoreOffset);
        Exec.ObtainSemaphore(semaphore);

        var matchedRecord = APTR.Null;
        var result = 0u;
        if (inputLock.IsNotNull)
        {
            var record = FirstRecord(state);
            var tail = APTR.FromPointer(state.Raw + 4);
            while (record.IsNotNull && record.Raw != tail.Raw)
            {
                var next = APTR.FromPointer(APTR.ReadUInt32(record, 0));
                var recordLock = BPTR.FromRaw(
                    APTR.ReadUInt32(record, RecordLockOffset));
                if (DOS.SameLock(inputLock, recordLock) == 0)
                {
                    result = APTR.ReadUInt32(record, RecordSegListOffset);
                    Exec.Remove(record);
                    matchedRecord = record;
                    break;
                }
                record = next;
            }
        }

        Exec.ReleaseSemaphore(semaphore);

        if (matchedRecord.IsNotNull)
        {
            var recordLock = BPTR.FromRaw(
                APTR.ReadUInt32(matchedRecord, RecordLockOffset));
            DOS.UnLock(recordLock);
            Exec.FreeVec(matchedRecord);
        }
        if (inputLock.IsNotNull)
            DOS.UnLock(inputLock);

        if (matchedRecord.IsNull)
        {
            var previous = APTR.FromPointer(
                APTR.ReadUInt32(state, PreviousLoadSegOffset));
            result = InvokeLoadSeg(previous, dosBase, name);
        }

        DOS.DOSLibraryBase = previousDosBase;
        EndOperation(state);
        return result;
    }

    [AmigaIndirectCall(M68kRegister.A3)]
    [return: M68kRegister(M68kRegister.D0)]
    private static extern uint InvokeLoadSeg(
        [M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A6)] APTR dosBase,
        [M68kRegister(M68kRegister.D1)] CString name);

    private static APTR FindStateAndBeginOperation()
    {
        Exec.Forbid();
        var port = Exec.FindPort(CString.FromLiteral(ServicePortName));
        var state = APTR.Null;
        if (port.IsNotNull)
        {
            var ownerTask = APTR.FromPointer(APTR.ReadUInt32(port,
                ExecLayout.MsgPort.SignalTask));
            if (ownerTask.IsNotNull)
                state = APTR.FromPointer(APTR.ReadUInt32(ownerTask,
                    ExecLayout.Task.UserData));
        }
        if (state.IsNotNull)
            APTR.WriteUInt32(state, ActiveOperationsOffset,
                APTR.ReadUInt32(state, ActiveOperationsOffset) + 1);
        Exec.Permit();
        return state;
    }

    private static void EndOperation(APTR state)
    {
        Exec.Forbid();
        APTR.WriteUInt32(state, ActiveOperationsOffset,
            APTR.ReadUInt32(state, ActiveOperationsOffset) - 1);
        Exec.Permit();
    }

    private static APTR FindBySegList(APTR state, uint segList)
    {
        var record = FirstRecord(state);
        var tail = APTR.FromPointer(state.Raw + 4);
        while (record.IsNotNull && record.Raw != tail.Raw)
        {
            var next = APTR.FromPointer(APTR.ReadUInt32(record, 0));
            if (APTR.ReadUInt32(record, RecordSegListOffset) == segList)
                return record;
            record = next;
        }
        return APTR.Null;
    }

    private static APTR FirstRecord(APTR state) =>
        APTR.FromPointer(APTR.ReadUInt32(state, 0));

    private static APTR CurrentWorkerState()
    {
        var task = Exec.FindTask(CString.FromPointer(0));
        return task.IsNull ? APTR.Null : APTR.FromPointer(
            APTR.ReadUInt32(task, ExecLayout.Task.UserData));
    }

    private static bool IsPublishedWorkerTask(APTR task)
    {
        var publishedTask = APTR.Null;
        Exec.Forbid();
        var port = Exec.FindPort(CString.FromLiteral(ServicePortName));
        if (port.IsNotNull)
            publishedTask = APTR.FromPointer(APTR.ReadUInt32(port,
                ExecLayout.MsgPort.SignalTask));
        Exec.Permit();
        return publishedTask.Raw == task.Raw;
    }

    private static bool ListIsEmpty(APTR state) =>
        APTR.ReadUInt32(state, 0) == state.Raw + 4;

    private static void InitializeList(APTR list)
    {
        APTR.WriteUInt32(list, 0, list.Raw + 4);
        APTR.WriteUInt32(list, 4, 0);
        APTR.WriteUInt32(list, 8, list.Raw);
        APTR.WriteUInt8(list, 12, (byte)NodeType.Unknown);
        APTR.WriteUInt8(list, 13, 0);
    }
}
