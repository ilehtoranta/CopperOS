using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>PatCopy's directory-entry probe using public DOS v37 calls.</summary>
public static class NativeMorphOSCopySoftLinkCheck
{
    /// <summary>
    /// Call for ordinary directory entries under ALL. warningArguments is
    /// eight bytes owned by this invocation. The first-directory branch does
    /// not use this probe. Failure to resolve or allocate remains enterable,
    /// as in the original; only a positive ReadLink result denies entry.
    /// </summary>
    public static bool CanEnter(BPTR directory, APTR name, bool quiet,
        APTR warningArguments)
    {
        var enter = true;
        var previous = DOS.CurrentDirRaw(directory);
        var target = DOS.LockRaw(CString.FromPointer(name.Raw), DOS.LockMode.Shared);
        if (target.IsNotNull)
        {
            DOS.UnLock(target);
        }
        else
        {
            var error = DOS.IoErr();
            if (error == DOS.Error.ObjectNotFound)
            {
                var device = DOS.GetDeviceProc("", APTR.Null);
                if (device.IsNotNull)
                {
                    var buffer = Exec.AllocMem(512, Exec.MemoryFlags.Public);
                    if (buffer.IsNotNull)
                    {
                        var port = APTR.FromPointer(APTR.ReadUInt32(device, 0));
                        var lock_ = BPTR.FromRaw(APTR.ReadUInt32(device, 4));
                        if (DOS.ReadLink(port, lock_, CString.FromPointer(name.Raw),
                            buffer, 511) > 0)
                        {
                            if (!quiet)
                            {
                                APTR.WriteUInt8(buffer, 511, 0);
                                APTR.WriteUInt32(warningArguments, 0, name.Raw);
                                APTR.WriteUInt32(warningArguments, 4, buffer.Raw);
                                DOS.VPrintf("Warning: Skipping dangling softlink %s -> %s\n",
                                    warningArguments);
                            }
                            enter = false;
                        }
                        Exec.FreeMem(buffer, 512);
                    }
                    DOS.FreeDeviceProc(device);
                }
            }
            DOS.SetIoErr(error);
        }
        DOS.CurrentDirRaw(previous);
        return enter;
    }
}
