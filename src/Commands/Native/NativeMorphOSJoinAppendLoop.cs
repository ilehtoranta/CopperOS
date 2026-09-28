using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Source-observed byte-transfer stage of MorphOS Join's append path. The
/// command owns opening the source, allocating its 262144-byte buffer, output
/// diagnostics, and partial-destination cleanup; this stage owns none of them.
/// </summary>
public static class NativeMorphOSJoinAppendLoop
{
    /// <summary>
    /// Appends source bytes to destination with Join's exact read/write and
    /// Ctrl-C behavior. Handles and buffer are invocation-local caller state.
    /// </summary>
    public static int Run(BPTR destination, BPTR source, APTR buffer, int bufferSize,
        out int ioError) => NativeMorphOSCopyLoop.Run(source, destination, buffer,
            bufferSize, out ioError);
}
