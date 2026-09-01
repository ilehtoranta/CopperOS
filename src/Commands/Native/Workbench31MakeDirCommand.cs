using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 MakeDir 37.2 CLI body, independently written from its verified
/// command contract. The caller must already own an open DOS library base and
/// prepare the current CLI input. This body neither opens/closes libraries nor
/// receives/replies to a Workbench startup message. Native qualification is open.
/// </summary>
public static class Workbench31MakeDirCommand
{
    /// <summary>
    /// Reads NAME/M through DOS, processes every name and returns the selected
    /// command level and secondary error. The caller retains library/startup
    /// ownership and must preserve these results through its final cleanup.
    /// DOS must provide a valid, aligned, null-terminated /M pointer vector and
    /// strings for the lifetime of the returned RDArgs lease.
    /// </summary>
    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead("NAME/M", 1, out var arguments))
        {
            // The shared lease reports ordinary parser failure as ERROR;
            // this command's original policy is FAIL for every parse failure.
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }

        var result = DOS.RETURN_FAIL;
        if (!arguments.TryGetResult(0, out var names))
        {
            // A successful one-slot lease must expose slot zero. Keep an
            // internal ownership-contract failure out of the directory loop.
            ioError = (int)DOS.Error.BadTemplate;
        }
        else if (names == 0)
        {
            DOS.VPrintf("No name given\n", APTR.Null);
        }
        else
        {
            // A present but empty vector succeeds. Do not normalize it to
            // the absent /M slot, whose command result remains FAIL above.
            result = DOS.RETURN_OK;
            var nameSlot = APTR.FromPointer(names);
            while (true)
            {
                var nameAddress = APTR.ReadUInt32(nameSlot, 0);
                if (nameAddress == 0)
                    break;

                var name = CString.FromPointer(nameAddress);
                var existing = DOS.Lock(name, DOS.LockMode.Read);
                if (existing.HasValue)
                {
                    result = DOS.RETURN_ERROR;
                    ioError = 0;
                    // The live /M cell is already the one-LONG format argument
                    // array. VPrintf borrows it without mutation or copying.
                    DOS.VPrintf("%s already exists\n", nameSlot);
                    DOS.UnLock(existing.Value);
                }
                else
                {
                    var created = DOS.CreateDir(name);
                    if (created.HasValue)
                    {
                        DOS.UnLock(created.Value);
                    }
                    else
                    {
                        // Preserve the original first-error selection. An
                        // existing name above also clears any earlier error.
                        if (result != DOS.RETURN_ERROR)
                        {
                            ioError = (int)DOS.IoErr();
                            result = DOS.RETURN_ERROR;
                        }

                        DOS.VPrintf("Can't create directory %s\n", nameSlot);
                    }
                }

                nameSlot = APTR.FromPointer(nameSlot.Raw + sizeof(uint));
            }
        }

        arguments.Release();
        DOS.SetIoErr((DOS.Error)ioError);
        if (ioError != 0)
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));

        return result;
    }
}
