using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Invocation-local traversal of one DOS list kind. The caller owns rendering;
/// this primitive owns exactly one optional read lock for its duration.
/// </summary>
public static class NativeDosListTraversal
{
    private const uint CtrlCMask = 1u << 12;

    /// <summary>
    /// Counts entries returned by DOS while holding the matching read lock.
    /// A lock that cannot be acquired is reported separately from a traversal
    /// error, matching the source-observed optional list-pass behavior.
    /// </summary>
    public static int TryCount(DosListLockFlags kind, out uint count,
        out bool acquired, out int ioError)
    {
        count = 0;
        acquired = false;
        ioError = 0;
        var lockFlags = (uint)(kind | DosListLockFlags.Read);
        var cursor = DOS.AttemptLockDosList(lockFlags);
        if (cursor.IsNull) return DOS.RETURN_OK;
        acquired = true;
        var result = DOS.RETURN_OK;
        do
        {
            cursor = DOS.NextDosEntry(cursor, (uint)kind);
            if (cursor.IsNull) break;
            count++;
            if ((Exec.SetSignal(0, CtrlCMask) & CtrlCMask) == 0) continue;
            ioError = (int)DOS.Error.Break;
            result = DOS.RETURN_ERROR;
            break;
        }
        while (true);
        DOS.UnLockDosList(lockFlags);
        if (ioError != 0) DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }
}
