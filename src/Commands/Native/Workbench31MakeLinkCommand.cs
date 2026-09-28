using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 MakeLink body. The caller owns DOS/startup lifetime.
/// This profile always makes hard links, including when HARD is omitted.
/// Native execution and packaging qualification remain open.
/// </summary>
public static class Workbench31MakeLinkCommand
{
    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead("FROM/A,TO/A,HARD/S,FORCE/S", 4, out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }

        var result = DOS.RETURN_FAIL;
        var target = BPTR.Null;
        do
        {
            if (!arguments.TryGetResult(0, out var from) || from == 0 ||
                !arguments.TryGetResult(1, out var to) || to == 0 ||
                !arguments.TryGetResult(3, out var force) ||
                !arguments.TryGetResultSlot(0, out var pair) ||
                !arguments.TryGetResultSlot(1, out var targetArgument))
            {
                ioError = (int)DOS.Error.BadTemplate;
                break;
            }

            target = DOS.LockRaw(CString.FromPointer(to), DOS.LockMode.Shared);
            if (target.IsNull)
            {
                ioError = (int)DOS.IoErr();
                DOS.VPrintf("Can't find %s ", targetArgument);
                break;
            }

            var classification = CheckDestination(target, from, out ioError);
            if (classification == -1)
            {
                DOS.VPrintf("Link loop from %s to %s not allowed\n", pair);
                ioError = 0;
                break;
            }
            if (classification == 1 && force == 0)
            {
                DOS.VPrintf("Links to directories require use of the FORCE keyword\n", targetArgument);
                ioError = 0;
                break;
            }
            if (classification == 2)
                break;

            // HARD is accepted by ReadArgs but does not select a soft mode in
            // this original profile. MorphOS has its own separate body.
            var linked = DOS.MakeLink(CString.FromPointer(from), unchecked((int)target.Raw), 0);
            ioError = (int)DOS.IoErr();
            if (linked != 0)
                result = DOS.RETURN_OK;
        }
        while (false);

        if (result != DOS.RETURN_OK)
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
        // Release our own locks even on reference paths that leave cleanup to
        // process exit. A resident invocation must not retain another call's lock.
        if (!target.IsNull)
            DOS.UnLock(target);
        arguments.Release();
        return result;
    }

    // 0: regular file; 1: safe directory; -1: loop; 2: DOS failure.
    private static int CheckDestination(BPTR target, uint from, out int error)
    {
        error = 0;
        var fib = DOS.AllocDosObject((uint)DosObjectType.FileInfoBlock, APTR.Null);
        if (fib.IsNull)
        {
            error = (int)DOS.IoErr();
            return 2;
        }
        var examined = DOS.Examine(target, fib);
        error = (int)DOS.IoErr();
        var directory = examined != 0 && FileInfoBlock.GetDirEntryType(fib.Raw) >= 0;
        DOS.FreeDosObject((uint)DosObjectType.FileInfoBlock, fib);
        if (examined == 0)
            return 2;
        if (!directory)
            return 0;

        // ReadArgs owns this mutable string for this invocation. Restore its
        // path separator immediately after Lock, including the failure path.
        var boundary = APTR.FromPointer(STRPTR.ToUInt32(DOS.PathPart(CString.FromPointer(from))));
        var saved = APTR.ReadUInt8(boundary, 0);
        APTR.WriteUInt8(boundary, 0, 0);
        var ancestor = DOS.LockRaw(CString.FromPointer(from), DOS.LockMode.Shared);
        error = (int)DOS.IoErr();
        APTR.WriteUInt8(boundary, 0, saved);
        if (ancestor.IsNull)
            return 2;
        while (!ancestor.IsNull)
        {
            if (DOS.SameLock(target, ancestor) == 0)
            {
                DOS.UnLock(ancestor);
                error = 0;
                return -1;
            }
            var parent = DOS.ParentDirRaw(ancestor);
            DOS.UnLock(ancestor);
            ancestor = parent;
        }
        error = 0;
        return 1;
    }
}
