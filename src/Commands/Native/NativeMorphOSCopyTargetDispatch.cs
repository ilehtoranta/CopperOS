using Amiga;
namespace CopperOS.Commands.Native;

/// <summary>Normal COPY/MOVE/LINK destination selection after PATTERN setup.</summary>
public static class NativeMorphOSCopyTargetDispatch
{
    public static void Run(ref NativeMorphOSCopyOptions options, APTR classifier,
        ref NativeMorphOSCopyTraversalState state)
    {
        var classification = NativeMorphOSCopyMakeDirectory.IsPattern(options.Target);
        if (classification != 0)
        {
            if (classification != -1)
            {
                state.Result = DOS.RETURN_ERROR;
                if ((state.Flags & 256) == 0) DOS.VPrintf("Wildcard destination invalid.\n", state.WarningArguments);
            }
            return;
        }
        var path = DOS.PathPart(CString.FromPointer(options.Target.Raw)).Address;
        if (APTR.ReadUInt8(path, 0) == (byte)'/') path = APTR.FromPointer(path.Raw + 1);
        var source = APTR.FromPointer(APTR.ReadUInt32(options.Sources, 0));
        if (APTR.ReadUInt8(path, 0) != 0 && APTR.ReadUInt32(options.Sources, 4) == 0 &&
            (classification = NativeMorphOSCopyPatternClassifier.IsMatchPattern(CString.FromPointer(source.Raw), classifier)) == 0)
        {
            // Retain the source's lock value even after its release; the final
            // stream fallback tests that value, not ownership of the lock.
            var sourceLock = DOS.LockRaw(CString.FromPointer(options.Target.Raw), DOS.LockMode.Shared);
            if (sourceLock.IsNotNull)
            {
                if (Examine(sourceLock, ref state) != 0)
                {
                    if (FileInfoBlock.GetDirEntryType(state.Fib.Raw) > 0) state.SecondaryResult = DOS.RETURN_OK;
                }
                else classification = 1;
                DOS.UnLock(sourceLock);
            }
            if (classification == 0 && state.SecondaryResult != 0 &&
                NativeMorphOSCopyPatternClassifier.IsMatchPattern(CString.FromPointer(source.Raw), classifier) == 0)
                NormalizeQuotedSource(source);
            if (classification == 0 && state.SecondaryResult != 0)
            {
                sourceLock = DOS.LockRaw(CString.FromPointer(source.Raw), DOS.LockMode.Shared);
                if (sourceLock.IsNotNull)
                {
                    if (Examine(sourceLock, ref state) != 0)
                    {
                        state.SecondaryResult = DOS.RETURN_OK;
                        if (state.Mode != NativeMorphOSCopyModeSelection.Copy || FileInfoBlock.GetDirEntryType(state.Fib.Raw) < 0)
                        {
                            state.Flags |= 1u << 21;
                            var separator = APTR.ReadUInt8(path, 0);
                            APTR.WriteUInt8(path, 0, 0);
                            state.CurrentDestination = NativeMorphOSCopyOpenDestination.Open(options.Target, ref state);
                            if (state.CurrentDestination.IsNotNull)
                            {
                                APTR.WriteUInt8(path, 0, separator);
                                DOS.UnLock(sourceLock);
                                sourceLock = BPTR.Null;
                                DispatchFile(source, options.Target, ref state);
                                DOS.UnLock(state.CurrentDestination);
                            }
                        }
                    }
                    if (sourceLock.IsNotNull) DOS.UnLock(sourceLock);
                }
            }
            var savedError = DOS.IoErr();
            if (sourceLock.IsNull && state.Mode == NativeMorphOSCopyModeSelection.Copy &&
                !NativeMorphOSCopyTraversal.IsFileSystem(source))
            {
                state.Flags |= (1u << 21) | (1u << 24);
                state.SecondaryResult = DOS.RETURN_OK;
                var separator = APTR.ReadUInt8(path, 0);
                APTR.WriteUInt8(path, 0, 0);
                state.CurrentDestination = NativeMorphOSCopyOpenDestination.Open(options.Target, ref state);
                if (state.CurrentDestination.IsNotNull)
                {
                    APTR.WriteUInt8(path, 0, separator);
                    DispatchFile(source, options.Target, ref state);
                    DOS.UnLock(state.CurrentDestination);
                }
            }
            else DOS.SetIoErr(savedError);
        }
        else if (classification != -1) state.SecondaryResult = DOS.RETURN_OK;
        NativeMorphOSCopyOpenDestination.RunDirectorySources(ref options, classifier, ref state);
    }

    private static int Examine(BPTR source, ref NativeMorphOSCopyTraversalState state)
    {
        APTR.WriteUInt8(state.Fib, FileInfoBlock.ActualExtensionFlagsOffset, 0);
        if (!state.ExtendedExamine)
        {
            var result = DOS.Examine(source, state.Fib);
            // _genExamine zero-extends the classic ULONG values even when
            // Examine fails; do not retain stale extended fields in the FIB.
            APTR.WriteUInt32(state.Fib, FileInfoBlock.Size64Offset, 0);
            APTR.WriteUInt32(state.Fib, FileInfoBlock.Size64Offset + 4,
                APTR.ReadUInt32(state.Fib, FileInfoBlock.SizeOffset));
            APTR.WriteUInt32(state.Fib, FileInfoBlock.NumBlocks64Offset, 0);
            APTR.WriteUInt32(state.Fib, FileInfoBlock.NumBlocks64Offset + 4,
                APTR.ReadUInt32(state.Fib, 128));
            return result;
        }
        // Path is not populated until dispatch. Its first sixteen bytes can
        // hold invocation-owned tags during destination/source examination.
        APTR.WriteUInt32(state.Path, 0, 0x80000e11);
        APTR.WriteUInt32(state.Path, 4, 1);
        APTR.WriteUInt32(state.Path, 8, 0);
        APTR.WriteUInt32(state.Path, 12, 0);
        return DOS.Examine64(source, state.Fib, state.Path);
    }

    private static void DispatchFile(APTR source, APTR target, ref NativeMorphOSCopyTraversalState state)
    {
        var offset = 0;
        byte value;
        do { value = APTR.ReadUInt8(source, offset); APTR.WriteUInt8(state.Path, offset, value); offset++; } while (value != 0);
        var worker = new NativeMorphOSCopyWork { Reserved = 0 };
        worker.Execute(DOS.FilePart(CString.FromPointer(target.Raw)).Address, ref state);
    }

    private static void NormalizeQuotedSource(APTR source)
    {
        var length = 0;
        while (APTR.ReadUInt8(source, length) != 0) length++;
        var bytes = unchecked((uint)length * 2 + 3);
        var parsed = Exec.AllocMem(bytes, Exec.MemoryFlags.Any);
        if (parsed.IsNull) return;
        if (DOS.ParsePattern(CString.FromPointer(source.Raw), parsed, (int)bytes) >= 0)
        {
            var parsedLength = 0;
            while (APTR.ReadUInt8(parsed, parsedLength) != 0) parsedLength++;
            if (parsedLength <= length)
            {
                var found = DOS.LockRaw(CString.FromPointer(parsed.Raw), DOS.LockMode.Shared);
                if (found.IsNotNull)
                {
                    DOS.UnLock(found);
                    for (var offset = 0; offset <= parsedLength; offset++) APTR.WriteUInt8(source, offset, APTR.ReadUInt8(parsed, offset));
                }
            }
        }
        Exec.FreeMem(parsed, bytes);
    }
}
