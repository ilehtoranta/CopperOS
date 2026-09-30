using Amiga;
namespace CopperOS.Commands.Native;

/// <summary>Original Copy MAKEDIR source-list branch and directory reporting.</summary>
public static class NativeMorphOSCopyMakeDirectory
{
    public static int IsPattern(APTR name)
    {
        uint length = 0;
        while (APTR.ReadUInt8(name, (int)length) != 0) length++;
        var bytes = unchecked(length * 2 + 3);
        var buffer = Exec.AllocMem(bytes, Exec.MemoryFlags.Any);
        if (buffer.IsNull) return -1;
        var result = DOS.ParsePattern(CString.FromPointer(name.Raw), buffer, (int)bytes);
        Exec.FreeMem(buffer, bytes);
        return result;
    }

    public static void Run(ref NativeMorphOSCopyOptions options, ref NativeMorphOSCopyTraversalState state)
    {
        state.SecondaryResult = DOS.RETURN_OK;
        var offset = 0;
        uint source;
        while (state.Result == 0 && state.SecondaryResult == 0 &&
            (source = APTR.ReadUInt32(options.Sources, offset)) != 0)
        {
            var name = APTR.FromPointer(source);
            var pattern = IsPattern(name);
            if (pattern != 0)
            {
                if (pattern == -1) state.SecondaryResult = DOS.RETURN_FAIL;
                else
                {
                    state.Result = DOS.RETURN_ERROR;
                    if ((state.Flags & 256) == 0) DOS.VPrintf("Wildcard destination invalid.\n", state.WarningArguments);
                }
            }
            // The original still attempts this source after IsPattern fails.
            var directory = OpenDirectory(name, ref state);
            if (directory.IsNotNull)
            {
                DOS.UnLock(directory);
                state.Flags |= 1u << 22;
            }
            offset += 4;
        }
    }

    public static BPTR OpenDirectory(APTR name, ref NativeMorphOSCopyTraversalState state)
    {
        var offset = 0;
        var error = 0;
        var createdCount = 0;
        var quiet = (state.Flags & 256) != 0;
        while (error == 0 && APTR.ReadUInt8(name, offset) != 0)
        {
            while (APTR.ReadUInt8(name, offset) != 0 && APTR.ReadUInt8(name, offset) != (byte)'/') offset++;
            var separator = APTR.ReadUInt8(name, offset);
            APTR.WriteUInt8(name, offset, 0);
            var outcome = NativeMorphOSCopyDestination.Prepare(name, true,
                (state.Flags & 128) != 0, (state.Flags & 64) != 0, state.ExtendedExamine, state.ExamineTags, out _);
            if (outcome == NativeMorphOSCopyDestination.CantDelete)
            {
                if (!quiet) DOS.VPrintf("Destination must be a directory.\n", state.WarningArguments);
                error = 2;
            }
            else if (outcome < 0) error = 1;
            else if (outcome != NativeMorphOSCopyDestination.Directory)
            {
                var created = DOS.CreateDirRaw(CString.FromPointer(name.Raw));
                if (created.IsNotNull)
                {
                    createdCount++;
                    if ((state.Flags & 512) != 0)
                    {
                        NativeMorphOSCopyOutput.PrintName(name, 1, true, true, state.WarningArguments);
                        DOS.VPrintf("   [created]\n", state.WarningArguments);
                    }
                    DOS.UnLock(created);
                }
                else
                {
                    if (!quiet) NativeMorphOSCopyOutput.PrintNotDone("   [created]", state.WarningArguments);
                    error = 2;
                }
            }
            APTR.WriteUInt8(name, offset, separator);
            if (separator != 0) offset++;
        }
        if (error != 0)
        {
            state.Result = DOS.RETURN_ERROR;
            if (!quiet && error == 1) NativeMorphOSCopyOutput.PrintNotDone("opened for output", state.WarningArguments);
            return BPTR.Null;
        }
        if (state.Mode == NativeMorphOSCopyModeSelection.MakeDir && createdCount == 0 && !quiet)
        {
            DOS.SetIoErr(DOS.Error.ObjectExists);
            NativeMorphOSCopyOutput.PrintNotDone("   [created]", state.WarningArguments);
        }
        return DOS.LockRaw(CString.FromPointer(name.Raw), DOS.LockMode.Shared);
    }
}
