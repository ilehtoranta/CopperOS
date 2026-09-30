using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Typed form of the shared DRIVE/A,BUFFERS/N ReadArgs result. Buffers is the
/// signed LONG value produced by dereferencing ReadArgs' optional /N pointer.
/// </summary>
internal struct NativeAddBuffersReadArgsRecord
{
    internal APTR Drive;
    internal bool HasBuffers;
    internal int Buffers;
}

/// <summary>Bounded decoder for the two-ULONG AddBuffers ReadArgs result.</summary>
internal static class NativeAddBuffersReadArgsRecordCodec
{
    internal const string Template = "DRIVE/A,BUFFERS/N";
    internal const uint ResultCount = 2;
    internal const uint ResultArrayBytes = 8;
    private const uint LongBytes = sizeof(uint);

    internal static bool TryRead(ref NativeCommandArguments arguments,
        out NativeAddBuffersReadArgsRecord value)
    {
        value = default;
        if (!arguments.TryGetResult(0, out var drive) ||
            !arguments.TryGetResult(1, out var buffersNumber)) return false;
        return TryCreate(drive, buffersNumber, out value);
    }

    internal static bool TryRead(APTR resultArray, uint resultArrayBytes,
        out NativeAddBuffersReadArgsRecord value)
    {
        value = default;
        if (resultArray.IsNull || resultArrayBytes < ResultArrayBytes ||
            (resultArray.Raw & (LongBytes - 1)) != 0 ||
            resultArray.Raw > uint.MaxValue - ResultArrayBytes) return false;

        var drive = APTR.ReadUInt32(resultArray, 0);
        var buffersNumber = APTR.ReadUInt32(resultArray, (int)LongBytes);
        return TryCreate(drive, buffersNumber, out value);
    }

    private static bool TryCreate(uint drive, uint buffersNumber,
        out NativeAddBuffersReadArgsRecord value)
    {
        value = default;
        if (drive == 0) return false;

        var buffers = 0;
        if (buffersNumber != 0)
        {
            if ((buffersNumber & (LongBytes - 1)) != 0 ||
                buffersNumber > uint.MaxValue - LongBytes) return false;
            buffers = unchecked((int)APTR.ReadUInt32(
                APTR.FromPointer(buffersNumber), 0));
        }

        value.Drive = APTR.FromPointer(drive);
        value.HasBuffers = buffersNumber != 0;
        value.Buffers = buffers;
        return true;
    }
}

/// <summary>Two-long VPrintf argument block for AddBuffers output.</summary>
internal struct NativeAddBuffersOutputFormatRecord
{
    internal APTR Drive;
    internal int Buffers;
}

/// <summary>Bounded writer for the AddBuffers VPrintf argument block.</summary>
internal static class NativeAddBuffersOutputFormatRecordCodec
{
    internal const uint Size = 8;
    private const uint LongBytes = sizeof(uint);

    internal static bool TryWrite(APTR address, uint capacity,
        NativeAddBuffersOutputFormatRecord value)
    {
        if (address.IsNull || value.Drive.IsNull || capacity < Size ||
            (address.Raw & (LongBytes - 1)) != 0 ||
            address.Raw > uint.MaxValue - Size) return false;

        APTR.WriteUInt32(address, 0, value.Drive.Raw);
        APTR.WriteUInt32(address, (int)LongBytes,
            unchecked((uint)value.Buffers));
        return true;
    }
}
