using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Source-ordered guard and destination-path prelude of DoWork.</summary>
public static class NativeMorphOSCopyWorkPreparation
{
    /// <summary>
    /// DestinationName is a caller-owned 2048-byte buffer. On success the
    /// worker continues into direct-device or filesystem object handling.
    /// False means the original DoWork would return before either branch.
    /// </summary>
    public static bool Prepare(APTR name, ref NativeMorphOSCopyTraversalState state)
    {
        if (state.Result > ((state.Flags & 1024) != 0 ? DOS.RETURN_OK : DOS.RETURN_WARN) ||
            state.SecondaryResult != 0)
            return false;
        if (state.Mode == NativeMorphOSCopyModeSelection.Delete ||
            (state.Flags & (1u << 25)) != 0)
            return true;

        if (state.DestinationPathSize == 0)
        {
            if (DOS.NameFromLock(state.CurrentDestination, state.DestinationName, 2048) == 0)
            {
                state.SecondaryResult = DOS.RETURN_FAIL;
                DOS.UnLock(BPTR.Null);
                return false;
            }
            var size = 0;
            while (APTR.ReadUInt8(state.DestinationName, size) != 0) size++;
            state.DestinationPathSize = size;
        }
        APTR.WriteUInt8(state.DestinationName, state.DestinationPathSize, 0);
        // The original ignores AddPart's result and lets the later object
        // operation determine the outcome; preserve that behavior here.
        DOS.AddPart(CString.FromPointer(state.DestinationName.Raw),
            CString.FromPointer(name.Raw), 2048);
        return true;
    }
}
