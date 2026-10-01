using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// One regular-file Copy operation over caller-owned source/destination names
/// and buffer storage. Traversal, destination preparation, metadata, and
/// partial-destination policy belong to later Copy stages.
/// </summary>
public static class NativeMorphOSCopyFilePair
{
    /// <summary>
    /// Opens destination before source, streams with Copy's normal loop, then
    /// closes destination before source. The first failing Open or transfer
    /// error survives close cleanup unchanged.
    /// </summary>
    public static int Run(APTR sourceName, APTR destinationName, APTR buffer,
        int bufferSize, out int ioError)
    {
        ioError = 0;
        if (sourceName.IsNull || destinationName.IsNull || buffer.IsNull ||
            bufferSize <= 0)
        {
            ioError = (int)DOS.Error.BadTemplate;
            DOS.SetIoErr((DOS.Error)ioError);
            return DOS.RETURN_FAIL;
        }

        var destination = DOS.OpenRaw(CString.FromPointer(destinationName.Raw),
            DOS.FileMode.NewFile);
        if (destination.IsNull)
        {
            ioError = (int)DOS.IoErr();
            return DOS.RETURN_FAIL;
        }

        var source = DOS.OpenRaw(CString.FromPointer(sourceName.Raw),
            DOS.FileMode.OldFile);
        if (source.IsNull)
        {
            ioError = (int)DOS.IoErr();
            DOS.Close(destination);
            DOS.DeleteFile(CString.FromPointer(destinationName.Raw));
            DOS.SetIoErr((DOS.Error)ioError);
            return DOS.RETURN_FAIL;
        }

        var result = NativeMorphOSCopyLoop.Run(source, destination, buffer,
            bufferSize, out ioError);
        DOS.Close(destination);
        DOS.Close(source);
        // Copy's normal path removes a newly created destination after a
        // failed transfer. The cleanup result must never replace the transfer
        // error that determines Copy's later diagnostic/result policy.
        if (result != DOS.RETURN_OK)
            DOS.DeleteFile(CString.FromPointer(destinationName.Raw));
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }
}
