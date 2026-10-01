using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Invocation-owned DOS ReadArgs result storage. Open DOS before TryRead and
/// release this lease before closing it. Never copy a live lease; pass it by
/// reference when another helper needs access to the same ownership record.
/// </summary>
public struct NativeCommandArguments
{
    private const uint ResultSlotBytes = sizeof(uint);

    private APTR _resultArray;
    private APTR _rdArgs;
    private uint _resultCount;
    private uint _allocationBytes;
    private int _returnLevel;
    private int _ioError;

    /// <summary>True while a successful parse still owns live DOS results.</summary>
    public bool IsSuccess => _rdArgs.IsNotNull;

    public uint ResultCount => _resultCount;
    public int ReturnLevel => _returnLevel;
    public int IoError => _ioError;

    /// <summary>
    /// Parses into caller-owned, cleared LONG storage using a caller-owned
    /// DOS_RDARGS object. Both must remain live until ReleaseBorrowed. This
    /// path deliberately leaves DOS failure/cleanup IoErr timing unchanged.
    /// </summary>
    public static bool TryReadBorrowed(CString template, APTR results,
        uint count, APTR rdArgs, out NativeCommandArguments arguments)
    {
        arguments = default;
        arguments._returnLevel = DOS.RETURN_FAIL;
        if (DOS.ReadArgs(template, results, rdArgs).IsNull)
        {
            arguments._ioError = (int)DOS.IoErr();
            return false;
        }
        arguments._resultArray = results;
        arguments._resultCount = count;
        arguments._rdArgs = rdArgs;
        arguments._returnLevel = DOS.RETURN_OK;
        return true;
    }

    /// <summary>
    /// Releases only DOS parser allocations from TryReadBorrowed. The caller
    /// then frees its DOS_RDARGS object and result storage in original order.
    /// Do not use Release on a borrowed lease or this method on an owned lease.
    /// </summary>
    public void ReleaseBorrowed()
    {
        var rdArgs = _rdArgs;
        _rdArgs = APTR.Null;
        _resultArray = APTR.Null;
        _resultCount = 0;
        if (rdArgs.IsNotNull) DOS.FreeArgs(rdArgs);
    }

    /// <summary>
    /// Parses the current DOS input using the supplied original template.
    /// resultCount must match its option count; this helper does not reparse
    /// DOS template grammar. Release any earlier lease before replacing it.
    /// An empty template may use zero results and gets one unused storage slot.
    /// </summary>
    public static bool TryRead(CString template, uint resultCount,
        out NativeCommandArguments arguments)
    {
        arguments = default;
        arguments._returnLevel = DOS.RETURN_ERROR;
        arguments._ioError = (int)DOS.Error.BadTemplate;

        // Guard both the ULONG-array multiplication and Exec's documented
        // memory-chunk rounding before submitting the allocation request.
        if (CString.ToUInt32(template) == 0 ||
            resultCount > (uint.MaxValue - ExecConstants.MemoryBlockMask) /
                ResultSlotBytes)
        {
            DOS.SetIoErr(DOS.Error.BadTemplate);
            return false;
        }

        var resultBytes = resultCount == 0
            ? ResultSlotBytes
            : resultCount * ResultSlotBytes;
        var resultArray = Exec.AllocMem(resultBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (resultArray.IsNull)
        {
            arguments._returnLevel = DOS.RETURN_FAIL;
            arguments._ioError = (int)DOS.Error.NoFreeStore;
            DOS.SetIoErr(DOS.Error.NoFreeStore);
            return false;
        }

        // Exec guarantees longword alignment. Reject an invalid provider span
        // before DOS can write LONG results through it, retaining exact free
        // ownership of the allocation that provider returned.
        if ((resultArray.Raw & (ResultSlotBytes - 1)) != 0 ||
            resultArray.Raw > uint.MaxValue - resultBytes)
        {
            arguments._returnLevel = DOS.RETURN_FAIL;
            arguments._ioError = (int)DOS.Error.NoFreeStore;
            Exec.FreeMem(resultArray, resultBytes);
            DOS.SetIoErr(DOS.Error.NoFreeStore);
            return false;
        }

        var rdArgs = DOS.ReadArgs(template, resultArray, APTR.Null);
        if (rdArgs.IsNull)
        {
            // Capture the parser's error before any cleanup can replace it.
            arguments._ioError = (int)DOS.IoErr();
            Exec.FreeMem(resultArray, resultBytes);
            DOS.SetIoErr((DOS.Error)arguments._ioError);
            return false;
        }

        arguments._resultArray = resultArray;
        arguments._rdArgs = rdArgs;
        arguments._resultCount = resultCount;
        arguments._allocationBytes = resultBytes;
        arguments._returnLevel = DOS.RETURN_OK;
        arguments._ioError = 0;
        return true;
    }

    /// <summary>
    /// Reads one raw ULONG result while its lease is live. A numeric option
    /// returns a pointer to a LONG, and a multiple option returns a pointer
    /// vector. Dereferenced data remains owned by DOS until Release.
    /// </summary>
    public bool TryGetResult(uint index, out uint value)
    {
        value = 0;
        if (_rdArgs.IsNull || _resultArray.IsNull || index >= _resultCount)
            return false;

        var slot = APTR.FromPointer(_resultArray.Raw + index * ResultSlotBytes);
        value = APTR.ReadUInt32(slot, 0);
        return true;
    }

    /// <summary>
    /// Returns one invocation-owned result LONG address while this lease is
    /// live. Callers may reuse a slot only after copying its parsed value and
    /// only for a DOS vector that borrows a raw argument array.
    /// </summary>
    public bool TryGetResultSlot(uint index, out APTR slot)
    {
        slot = APTR.Null;
        if (_rdArgs.IsNull || _resultArray.IsNull || index >= _resultCount)
            return false;
        slot = APTR.FromPointer(_resultArray.Raw + index * ResultSlotBytes);
        return true;
    }

    /// <summary>
    /// Invalidates and releases this lease, preserving the current DOS IoErr.
    /// Safe to repeat on the same instance. No borrowed stream, lock, library,
    /// input text, or template is closed or freed by this operation.
    /// </summary>
    public void Release()
    {
        if (_rdArgs.IsNull && _resultArray.IsNull)
            return;

        var error = DOS.IoErr();
        var rdArgs = _rdArgs;
        var resultArray = _resultArray;
        var allocationBytes = _allocationBytes;
        _rdArgs = APTR.Null;
        _resultArray = APTR.Null;
        _resultCount = 0;
        _allocationBytes = 0;

        if (rdArgs.IsNotNull)
            DOS.FreeArgs(rdArgs);
        if (resultArray.IsNotNull)
            Exec.FreeMem(resultArray, allocationBytes);
        DOS.SetIoErr(error);
    }
}
