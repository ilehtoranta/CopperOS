using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Search's MorphOS 3.20 soft-link-aware AnchorPath directory test.</summary>
public static class NativeMorphOSSearchSoftLinks
{
    private const uint LinkBufferBytes = 512;
    private const int LinkNameBytes = 108;
    private const uint WarningArgumentBytes = 8;

    /// <summary>
    /// Mirrors MorphOS libsupport SoftlinkDODIR: classify links in the
    /// containing directory, follow links to directories, and report dangling
    /// links unless Search is in a quiet-output mode. All scratch storage is
    /// borrowed from this Search invocation's workspace.
    /// </summary>
    public static uint ShouldDescend(APTR anchor, APTR targetInfo,
        APTR linkBuffer, APTR warningArguments, bool quiet)
    {
        var info = APTR.FromPointer(anchor.Raw +
            (uint)DosLayout.AnchorPath.Info);
        var entryType = unchecked((int)APTR.ReadUInt32(info,
            FileInfoBlock.DirEntryTypeOffset));
        if (entryType < 0) return 0;
        if (entryType != (int)DosConstants.SoftLink) return 1;

        var current = APTR.FromPointer(APTR.ReadUInt32(anchor,
            DosLayout.AnchorPath.Current));
        var directoryLock = BPTR.FromRaw(APTR.ReadUInt32(current,
            DosLayout.AChain.Lock));
        var previousDirectory = DOS.CurrentDirRaw(directoryLock);
        var namePointer = APTR.FromPointer(info.Raw +
            (uint)FileInfoBlock.FileNameOffset);
        var name = CString.FromPointer(namePointer.Raw);
        var targetLock = DOS.LockRaw(name, DOS.LockMode.Shared);
        var enter = true;
        if (targetLock.IsNotNull)
        {
            APTR.WriteUInt8(targetInfo,
                FileInfoBlock.ActualExtensionFlagsOffset, 0);
            var examined = SupportsExamine64()
                ? DOS.Examine64(targetLock, targetInfo, APTR.Null)
                : DOS.Examine(targetLock, targetInfo);
            if (examined != 0)
            {
                var targetEntryType = APTR.ReadUInt32(targetInfo,
                    FileInfoBlock.DirEntryTypeOffset);
                if ((targetEntryType & 0x80000000u) != 0)
                    enter = false;
                CopyInfoExceptName(targetInfo, info);
            }
            DOS.UnLock(targetLock);
        }
        else
        {
            var error = DOS.IoErr();
            if (error == DOS.Error.ObjectNotFound)
            {
                var device = DOS.GetDeviceProc("", APTR.Null);
                if (device.IsNotNull)
                {
                    var port = APTR.FromPointer(APTR.ReadUInt32(device,
                        DosLayout.DevProc.Port));
                    var deviceLock = BPTR.FromRaw(APTR.ReadUInt32(device,
                        DosLayout.DevProc.Lock));
                    if (DOS.ReadLink(port, deviceLock, name, linkBuffer,
                            unchecked((int)(LinkBufferBytes - 1))) > 0)
                    {
                        if (!quiet)
                        {
                            APTR.WriteUInt8(linkBuffer,
                                unchecked((int)(LinkBufferBytes - 1)), 0);
                            APTR.WriteUInt32(warningArguments, 0,
                                namePointer.Raw);
                            APTR.WriteUInt32(warningArguments, 4,
                                linkBuffer.Raw);
                            DOS.VPrintf(
                                "Warning: Skipping dangling softlink %s -> %s\n",
                                warningArguments);
                        }
                        enter = false;
                    }
                    DOS.FreeDeviceProc(device);
                }
            }
            DOS.SetIoErr(error);
        }
        DOS.CurrentDirRaw(previousDirectory);
        return enter ? 1u : 0u;
    }

    private static bool SupportsExamine64()
    {
        var library = DOS.DOSLibraryBase;
        var version = APTR.ReadUInt8(library, ExecLayout.Library.Version);
        var revision = APTR.ReadUInt8(library, ExecLayout.Library.Revision);
        return version > 51 || version == 51 && revision >= 28;
    }

    private static void CopyInfoExceptName(APTR source, APTR destination)
    {
        for (var offset = 0; offset < FileInfoBlock.FileNameOffset; offset++)
            APTR.WriteUInt8(destination, offset, APTR.ReadUInt8(source, offset));
        for (var offset = FileInfoBlock.FileNameOffset + LinkNameBytes;
             offset < FileInfoBlock.SizeInBytes; offset++)
            APTR.WriteUInt8(destination, offset, APTR.ReadUInt8(source, offset));
    }
}
