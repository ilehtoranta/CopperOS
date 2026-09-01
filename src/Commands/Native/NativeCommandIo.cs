using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// A copied DOS error observation, with an explicit tag so a captured zero is
/// distinct from a successful transfer for which IoErr was not inspected.
/// This value owns no resource and may be copied freely.
/// </summary>
public readonly struct NativeCommandIoError
{
    private readonly uint _captured;
    private readonly int _value;

    internal NativeCommandIoError(int value)
    {
        _captured = 1;
        _value = value;
    }

    public bool IsCaptured => _captured != 0;

    /// <summary>
    /// The raw LONG returned by IoErr when IsCaptured is true. Otherwise zero
    /// is only this record's default value, not the process's current IoErr.
    /// </summary>
    public int Value => _value;
}

/// <summary>
/// Single native DOS transfers and a non-consuming Ctrl-C query. DOS must be
/// open through the invocation's startup owner. Handles and buffers remain
/// borrowed; the caller supplies a valid native transfer request.
/// </summary>
public static class NativeCommandIo
{
    // NDK 3.1 Includes&Libs/include_h/dos/dos.h: SIGBREAKF_CTRL_C = 1 << 12.
    // The current public SDK has no equivalent break-signal constant.
    private const uint CtrlCMask = 1u << 12;

    /// <summary>
    /// Calls Read exactly once and returns its raw signed count. Only -1
    /// captures IoErr; zero and positive short reads have no captured error.
    /// No retry, error reset, stream selection, or return-level policy is used.
    /// </summary>
    public static int ReadOnce(BPTR file, APTR buffer, int length,
        out NativeCommandIoError error)
    {
        error = default;
        var result = DOS.Read(file, buffer, length);
        if (result == -1)
            error = new NativeCommandIoError((int)DOS.IoErr());
        return result;
    }

    /// <summary>
    /// Calls Write exactly once and returns its raw signed count. Only -1
    /// captures IoErr; a positive short write or zero is left to the caller's
    /// command-specific policy. The helper neither retries nor flushes.
    /// </summary>
    public static int WriteOnce(BPTR file, APTR buffer, int length,
        out NativeCommandIoError error)
    {
        error = default;
        var result = DOS.Write(file, buffer, length);
        if (result == -1)
            error = new NativeCommandIoError((int)DOS.IoErr());
        return result;
    }

    /// <summary>
    /// Observes the current task's Ctrl-C signal without clearing it or any
    /// other signal. The command owns acknowledgement and cancellation policy.
    /// </summary>
    public static bool IsCtrlCPending() =>
        (Exec.SetSignal(0u, 0u) & CtrlCMask) != 0;
}
