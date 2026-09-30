using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Independent public-DOS implementation of the source-observed MorphOS MakeLink path.</summary>
public static class NativeMorphOSMakeLinkCommand
{
    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead("FROM/A,TO/A,HARD/S,FORCE/S", 4,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, "MakeLink");
            return DOS.RETURN_FAIL;
        }

        var result = DOS.RETURN_FAIL;
        do
        {
            if (!arguments.TryGetResult(0, out var from) || from == 0 ||
                !arguments.TryGetResult(1, out var to) || to == 0 ||
                !arguments.TryGetResult(2, out var hard) ||
                !arguments.TryGetResult(3, out var force))
            {
                ioError = (int)DOS.Error.BadTemplate;
                break;
            }

            if (hard == 0)
            {
                if (DOS.MakeLink(CString.FromPointer(from), unchecked((int)to), 1) != 0)
                {
                    result = DOS.RETURN_OK;
                }
                else
                {
                    ioError = (int)DOS.IoErr();
                    DOS.PrintFault((DOS.Error)ioError, "MakeLink");
                }
                break;
            }

            var targetLock = DOS.LockRaw(CString.FromPointer(to), DOS.LockMode.Shared);
            if (targetLock.IsNull)
            {
                ioError = (int)DOS.IoErr();
                DOS.PutStr(CString.FromPointer(to));
                DOS.PrintFault((DOS.Error)ioError, "");
                break;
            }

            var fib = DOS.AllocDosObject((uint)DosObjectType.FileInfoBlock, APTR.Null);
            if (fib.IsNotNull)
            {
                if (DOS.Examine(targetLock, fib) != 0)
                {
                    if (FileInfoBlock.GetDirEntryType(fib.Raw) >= 0 && force == 0)
                    {
                        DOS.PutStr("Hard-links to directories require the FORCE keyword\n");
                    }
                    else if (DOS.MakeLink(CString.FromPointer(from),
                                 unchecked((int)targetLock.Raw), 0) != 0)
                    {
                        result = DOS.RETURN_OK;
                    }
                    else
                    {
                        ioError = (int)DOS.IoErr();
                        DOS.PrintFault((DOS.Error)ioError, "MakeLink");
                    }
                }
                DOS.FreeDosObject((uint)DosObjectType.FileInfoBlock, fib);
            }
            else
            {
                ioError = (int)DOS.IoErr();
                DOS.PrintFault((DOS.Error)ioError, "MakeLink");
            }
            DOS.UnLock(targetLock);
        }
        while (false);

        arguments.Release();
        // The observed body does not impose a final SetIoErr policy. Preserve
        // the DOS value after its final cleanup rather than inventing one.
        ioError = (int)DOS.IoErr();
        return result;
    }
}
