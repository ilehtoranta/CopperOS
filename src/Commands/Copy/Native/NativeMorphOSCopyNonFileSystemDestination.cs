using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS Copy's bounded non-filesystem OpenDestDir fallback. It deliberately
/// separates device classification and empty-name lock ownership from normal
/// filesystem prefix creation.
/// </summary>
public static class NativeMorphOSCopyNonFileSystemDestination
{
    public static BPTR TryOpen(APTR name, APTR destinationName, uint destinationBytes,
        bool copyOrMove, out bool destinationNoFileSystem, out int ioError)
    {
        destinationNoFileSystem = false;
        ioError = 0;
        if (name.IsNull || destinationName.IsNull || destinationBytes == 0)
        {
            ioError = (int)DOS.Error.BadTemplate;
            DOS.SetIoErr((DOS.Error)ioError);
            return BPTR.Null;
        }

        var index = 0;
        while (APTR.ReadUInt8(name, index) != 0 && APTR.ReadUInt8(name, index) != (byte)':')
            index++;
        if (APTR.ReadUInt8(name, index) != (byte)':')
            return BPTR.Null;

        var suffix = index + 1;
        var saved = APTR.ReadUInt8(name, suffix);
        APTR.WriteUInt8(name, suffix, 0);
        var isFileSystem = DOS.IsFileSystem(CString.FromPointer(name.Raw)) != 0;
        APTR.WriteUInt8(name, suffix, saved);
        if (!copyOrMove || isFileSystem)
            return BPTR.Null;

        var length = 0u;
        while (APTR.ReadUInt8(name, unchecked((int)length)) != 0)
            length++;
        if (length + 1 > destinationBytes)
        {
            ioError = (int)DOS.Error.LineTooLong;
            DOS.SetIoErr((DOS.Error)ioError);
            return BPTR.Null;
        }
        for (var offset = 0u; offset <= length; offset++)
            APTR.WriteUInt8(destinationName, unchecked((int)offset),
                APTR.ReadUInt8(name, unchecked((int)offset)));

        // The copied name's terminator is an invocation-owned empty string.
        var lock_ = DOS.LockRaw(CString.FromPointer(destinationName.Raw + length), DOS.LockMode.Shared);
        if (lock_.IsNull)
        {
            ioError = (int)DOS.IoErr();
            return BPTR.Null;
        }
        destinationNoFileSystem = true;
        return lock_;
    }
}
