using Amiga;

namespace CopperOS.Commands;

/// <summary>Borrowed DOS output operations used by Exe2Arc's naming path.</summary>
public interface IExe2ArcOutputIo : IAmigaGuestMemory
{
    BPTR OpenNewFile(APTR path);
    int AddPart(APTR buffer, APTR part, uint capacity);
}

public enum Exe2ArcOutputStatus : uint
{
    OpenedGenerated = 0,
    OpenedLiteralTo = 1,
    OpenedDirectoryFallback = 2,
    NotOpened = 3,
    InvalidBuffer = 4,
}

/// <summary>
/// Implements the source's output-open ordering without owning the returned
/// BPTR. A literal TO is tried first; only its failure permits the 512-byte
/// AddPart fallback using the generated basename. The caller closes/deletes
/// the selected path according to the command result.
/// </summary>
public static class Exe2ArcOutputSelection
{
    public static Exe2ArcOutputStatus TryOpen<TIo>(ref TIo io, APTR to,
        APTR generatedPath, APTR fallbackPath, APTR generatedBasename,
        uint fallbackCapacity, out BPTR output, out APTR selectedPath)
        where TIo : struct, IExe2ArcOutputIo
    {
        output = BPTR.Null;
        selectedPath = APTR.Null;
        if (generatedPath.IsNull)
            return Exe2ArcOutputStatus.InvalidBuffer;

        if (to.IsNull)
        {
            output = io.OpenNewFile(generatedPath);
            if (output.IsNull)
                return Exe2ArcOutputStatus.NotOpened;
            selectedPath = generatedPath;
            return Exe2ArcOutputStatus.OpenedGenerated;
        }

        output = io.OpenNewFile(to);
        if (output.IsNotNull)
        {
            selectedPath = to;
            return Exe2ArcOutputStatus.OpenedLiteralTo;
        }

        if (fallbackPath.IsNull || generatedBasename.IsNull || fallbackCapacity == 0 ||
            !io.IsMapped(fallbackPath, fallbackCapacity))
            return Exe2ArcOutputStatus.InvalidBuffer;

        io.WriteUInt8(fallbackPath, 0, 0);
        if (io.AddPart(fallbackPath, to, fallbackCapacity) == 0 ||
            io.AddPart(fallbackPath, generatedBasename, fallbackCapacity) == 0)
            return Exe2ArcOutputStatus.NotOpened;

        output = io.OpenNewFile(fallbackPath);
        if (output.IsNull)
            return Exe2ArcOutputStatus.NotOpened;
        selectedPath = fallbackPath;
        return Exe2ArcOutputStatus.OpenedDirectoryFallback;
    }
}
