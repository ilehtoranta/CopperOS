using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded directory-free lane of MorphOS Copy's PatCopy matcher loop. The
/// caller must select only regular-file entries; directory entry, recursive
/// walking, destination-parent movement, and DoWork itself stay with the
/// future coordinator.
/// </summary>
public static class NativeMorphOSCopyFlatTraversal
{
    // c/copy/copy.c FILEPATH_SIZE and the adjacent source-owned FIB snapshot.
    public const uint PathBytes = 2048;
    public const uint WorkRecordBytes = PathBytes + FileInfoBlock.SizeInBytes;
    private const uint CtrlCMask = 1u << 12;

    /// <summary>
    /// Iterates already-selected regular-file matches into caller-owned work records. Each record
    /// is a FILEPATH_SIZE path snapshot followed by the full FileInfoBlock
    /// snapshot used by the source before its deferred DoWork call. The caller
    /// must provide one full record for every match in this bounded flat lane.
    /// Returns the final MatchFirst/MatchNext result, including NO_MORE_ENTRIES.
    /// </summary>
    public static int Capture(CString name, APTR anchor, APTR workRecords,
        uint workRecordBytes, out uint delivered)
    {
        delivered = 0;
        if (anchor.IsNull || workRecords.IsNull ||
            workRecordBytes < WorkRecordBytes)
            return (int)DOS.Error.LineTooLong;

        // PatCopy allocates this AnchorPath with MEMF_CLEAR before setting its
        // two explicit matcher fields. Reproduce that observable header state
        // for caller-owned storage without taking ownership of its buffers.
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.Base, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.Current, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.FoundBreak, 0);
        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags, 0);
        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Reserved, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.BreakBits, CtrlCMask);
        APTR.WriteUInt16(anchor, DosLayout.AnchorPath.StringLength,
            unchecked((ushort)PathBytes));

        var captured = 0u;
        var pending = false;
        var result = DOS.MatchFirst(name, anchor);
        while (result == 0)
        {
            // PatCopy calls DoWork for the previous selected match at the top
            // of this iteration, before copying the current AnchorPath state.
            if (pending)
            {
                delivered = captured;
                pending = false;
            }

            if (captured >= workRecordBytes / WorkRecordBytes)
            {
                result = (int)DOS.Error.LineTooLong;
                break;
            }
            var record = APTR.FromPointer(workRecords.Raw +
                captured * WorkRecordBytes);
            Copy(anchor.Raw + (uint)DosLayout.AnchorPath.PathBuffer, record.Raw,
                PathBytes);
            Copy(anchor.Raw + (uint)DosLayout.AnchorPath.Info,
                record.Raw + PathBytes, FileInfoBlock.SizeInBytes);
            captured++;
            pending = true;
            result = DOS.MatchNext(anchor);
        }

        // The source owns MatchEnd after every MatchFirst attempt made with its
        // allocated AnchorPath, including an immediate matcher failure.
        DOS.MatchEnd(anchor);
        if (pending) delivered = captured;
        return result;
    }

    private static void Copy(uint source, uint destination, uint bytes)
    {
        for (var offset = 0u; offset < bytes; offset++)
            APTR.WriteUInt8(APTR.FromPointer(destination), unchecked((int)offset),
                APTR.ReadUInt8(APTR.FromPointer(source), unchecked((int)offset)));
    }
}
