using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>DoWork's COPY/MOVE byte-transfer branch and handle ownership.</summary>
public static class NativeMorphOSCopyFileOperation
{
    /// <summary>
    /// Destination policy and source/parent checks have already succeeded.
    /// For the filesystem path releaseSourceLock is true; direct-device mode
    /// passes false. The caller owns the cached buffer and any unreleased lock.
    /// outputOpened selects the original diagnostic phrase. No metadata or
    /// diagnostics are emitted here; DoWork performs those after this branch.
    /// </summary>
    public static bool Run(APTR sourceName, APTR destinationName, bool move,
        bool forceDelete, bool releaseSourceLock, ref BPTR sourceLock,
        uint requestedBytes, bool extendedExamine, ref APTR cachedBuffer,
        ref uint cachedBytes, out bool outputOpened)
    {
        var destination = CString.FromPointer(destinationName.Raw);
        var source = CString.FromPointer(sourceName.Raw);
        var output = DOS.OpenRaw(destination, DOS.FileMode.NewFile);
        outputOpened = output.IsNotNull;
        if (!outputOpened) return false;
        if (releaseSourceLock)
        {
            DOS.UnLock(sourceLock);
            sourceLock = BPTR.Null;
        }

        var success = false;
        var killDestination = true;
        var input = DOS.OpenRaw(source, DOS.FileMode.OldFile);
        if (input.IsNotNull)
        {
            var result = NativeMorphOSCopyFileTransfer.Run(input, output,
                requestedBytes, extendedExamine, ref cachedBuffer, ref cachedBytes);
            DOS.Close(output);
            output = BPTR.Null;
            DOS.Close(input);
            if (result == DOS.RETURN_OK)
            {
                killDestination = false;
                if (move)
                {
                    if (forceDelete) DOS.SetProtection(source, 0);
                    success = DOS.DeleteFile(source) != 0;
                }
                else success = true;
            }
        }
        if (output.IsNotNull) DOS.Close(output);
        if (killDestination)
        {
            // KillFileKeepErr captures IoErr here, after Close calls, exactly
            // as the original. Do not invent earlier error preservation.
            var error = DOS.IoErr();
            DOS.DeleteFile(destination);
            DOS.SetIoErr(error);
        }
        return success;
    }
}
