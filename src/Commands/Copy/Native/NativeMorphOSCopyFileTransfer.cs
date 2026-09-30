using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>CopyFile's cached buffer, examination and EOF policy.</summary>
public static class NativeMorphOSCopyFileTransfer
{
    /// <summary>
    /// requestedBytes is the validated command buffer size (at least 512).
    /// The caller owns and eventually frees cachedBuffer using cachedBytes.
    /// extendedExamine requires dos.library 51.66 or later, as in the source.
    /// Handles remain owned by DoWork. Handler IoErr is left unchanged except
    /// for Ctrl-C, which explicitly sets ERROR_BREAK.
    /// </summary>
    public static int Run(BPTR from, BPTR to, uint requestedBytes,
        bool extendedExamine, ref APTR cachedBuffer, ref uint cachedBytes)
    {
        var buffer = cachedBuffer;
        var size = cachedBytes;
        if (buffer.IsNull)
        {
            size = requestedBytes;
            do
            {
                buffer = Exec.AllocMem(size, Exec.MemoryFlags.Public);
                if (buffer.IsNotNull)
                {
                    cachedBuffer = buffer;
                    cachedBytes = size;
                    break;
                }
                size >>= 1;
            } while (size >= 512);
        }
        if (buffer.IsNull) return DOS.RETURN_FAIL;

        APTR.WriteUInt8(buffer, FileInfoBlock.ActualExtensionFlagsOffset, 0);
        int examined;
        uint high, low;
        if (extendedExamine)
        {
            // Keep the tag list outside the 260-byte FIB and inside the
            // invocation-owned buffer (whose minimum size is 512 bytes).
            var tags = APTR.FromPointer(buffer.Raw + 264);
            APTR.WriteUInt32(tags, 0, 0x80000e11); // EX64TAG_PosixDate
            APTR.WriteUInt32(tags, 4, 1);
            APTR.WriteUInt32(tags, 8, 0);
            APTR.WriteUInt32(tags, 12, 0);
            examined = DOS.ExamineFH64(from, buffer, tags);
            high = APTR.ReadUInt32(buffer, FileInfoBlock.Size64Offset);
            low = APTR.ReadUInt32(buffer, FileInfoBlock.Size64Offset + 4);
        }
        else
        {
            examined = DOS.ExamineFH(from, buffer);
            high = 0;
            low = APTR.ReadUInt32(buffer, FileInfoBlock.SizeOffset);
            APTR.WriteUInt32(buffer, FileInfoBlock.Size64Offset, 0);
            APTR.WriteUInt32(buffer, FileInfoBlock.Size64Offset + 4, low);
            APTR.WriteUInt32(buffer, FileInfoBlock.NumBlocks64Offset, 0);
            APTR.WriteUInt32(buffer, FileInfoBlock.NumBlocks64Offset + 4,
                APTR.ReadUInt32(buffer, 128)); // fib_NumBlocks
        }

        uint copiedHigh = 0, copiedLow = 0;
        do
        {
            if (NativeCommandIo.IsCtrlCPending())
            {
                DOS.SetIoErr(DOS.Error.Break);
                return DOS.RETURN_FAIL;
            }
            var count = DOS.Read(from, buffer, unchecked((int)size));
            if (count == -1 || DOS.Write(to, buffer, count) != count)
                return DOS.RETURN_FAIL;
            if (examined == 0)
            {
                if (count == 0) return DOS.RETURN_OK;
            }
            else
            {
                if (count == 0 && (copiedHigh < high ||
                    (copiedHigh == high && copiedLow < low)))
                    return DOS.RETURN_FAIL;
                var previous = copiedLow;
                copiedLow += unchecked((uint)count);
                if (copiedLow < previous) copiedHigh++;
                if (copiedHigh > high || (copiedHigh == high && copiedLow >= low))
                    return DOS.RETURN_OK;
            }
        } while (true);
    }
}
