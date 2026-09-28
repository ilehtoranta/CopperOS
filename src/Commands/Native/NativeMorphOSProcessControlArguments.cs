using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// ReadArgs ownership for MorphOS process-control commands whose original
/// DOS_RDARGS object supplies RDA_ExtHelp. Other commands keep using the
/// simpler NativeCommandArguments path with a null RDArgs pointer.
/// </summary>
public struct NativeMorphOSProcessControlArguments
{
    private NativeCommandArguments _borrowedArguments;
    private APTR _resultArray;
    private APTR _rdArgsObject;
    private uint _resultBytes;
    private int _returnLevel;
    private int _ioError;

    public int ReturnLevel => _returnLevel;
    public int IoError => _ioError;

    public static bool TryRead(CString template, uint resultCount,
        CString extendedHelp, out NativeMorphOSProcessControlArguments arguments)
    {
        arguments = default;
        arguments._returnLevel = DOS.RETURN_ERROR;
        arguments._ioError = (int)DOS.Error.BadTemplate;
        if (CString.ToUInt32(template) == 0 ||
            resultCount > (uint.MaxValue - ExecConstants.MemoryBlockMask) /
                sizeof(uint) || CString.ToUInt32(extendedHelp) == 0)
        {
            DOS.SetIoErr(DOS.Error.BadTemplate);
            return false;
        }

        // MorphOS's original command allocates DOS_RDARGS before parsing so
        // the DOS question-mark path can print its command-specific help.
        var rdArgsObject = DOS.AllocDosObject((uint)DosObjectType.RdArgs,
            APTR.Null);
        if (rdArgsObject.IsNull)
        {
            arguments._returnLevel = DOS.RETURN_FAIL;
            arguments._ioError = (int)DOS.IoErr();
            return false;
        }
        APTR.WriteUInt32(rdArgsObject, DosLayout.RDArgs.ExtendedHelp,
            CString.ToUInt32(extendedHelp));

        var resultBytes = resultCount == 0
            ? sizeof(uint)
            : resultCount * sizeof(uint);
        var resultArray = Exec.AllocMem(resultBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (resultArray.IsNull)
        {
            DOS.FreeDosObject((uint)DosObjectType.RdArgs, rdArgsObject);
            arguments._returnLevel = DOS.RETURN_FAIL;
            arguments._ioError = (int)DOS.Error.NoFreeStore;
            DOS.SetIoErr(DOS.Error.NoFreeStore);
            return false;
        }

        if ((resultArray.Raw & (sizeof(uint) - 1)) != 0 ||
            resultArray.Raw > uint.MaxValue - resultBytes)
        {
            Exec.FreeMem(resultArray, resultBytes);
            DOS.FreeDosObject((uint)DosObjectType.RdArgs, rdArgsObject);
            arguments._returnLevel = DOS.RETURN_FAIL;
            arguments._ioError = (int)DOS.Error.NoFreeStore;
            DOS.SetIoErr(DOS.Error.NoFreeStore);
            return false;
        }

        var parsed = NativeCommandArguments.TryReadBorrowed(template,
            resultArray, resultCount, rdArgsObject, out var borrowedArguments);
        if (!parsed)
        {
            var error = borrowedArguments.IoError;
            Exec.FreeMem(resultArray, resultBytes);
            DOS.FreeDosObject((uint)DosObjectType.RdArgs, rdArgsObject);
            arguments._returnLevel = borrowedArguments.ReturnLevel;
            arguments._ioError = error;
            DOS.SetIoErr((DOS.Error)error);
            return false;
        }

        arguments._borrowedArguments = borrowedArguments;
        arguments._resultArray = resultArray;
        arguments._rdArgsObject = rdArgsObject;
        arguments._resultBytes = resultBytes;
        arguments._returnLevel = DOS.RETURN_OK;
        arguments._ioError = 0;
        return true;
    }

    public bool TryGetResult(uint index, out uint value) =>
        _borrowedArguments.TryGetResult(index, out value);

    /// <summary>
    /// Releases the DOS parser lease, its caller-owned RDArgs object and the
    /// cleared ULONG result array. Repeated release is safe and preserves the
    /// current DOS IoErr across all cleanup calls.
    /// </summary>
    public void Release()
    {
        if (_resultArray.IsNull && _rdArgsObject.IsNull &&
            !_borrowedArguments.IsSuccess) return;

        var error = DOS.IoErr();
        var borrowedArguments = _borrowedArguments;
        var resultArray = _resultArray;
        var rdArgsObject = _rdArgsObject;
        var resultBytes = _resultBytes;
        _borrowedArguments = default;
        _resultArray = APTR.Null;
        _rdArgsObject = APTR.Null;
        _resultBytes = 0;

        borrowedArguments.ReleaseBorrowed();
        if (rdArgsObject.IsNotNull)
            DOS.FreeDosObject((uint)DosObjectType.RdArgs, rdArgsObject);
        if (resultArray.IsNotNull)
            Exec.FreeMem(resultArray, resultBytes);
        DOS.SetIoErr(error);
    }
}
