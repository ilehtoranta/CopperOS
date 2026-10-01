using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// The Exe2Arc command's invocation-owned DOS ReadArgs boundary. This slice
/// owns only the parser lease and result slots; file, FIB, buffer, diagnostic
/// and extraction cleanup remain with the eventual command owner.
/// </summary>
public static class NativeExe2ArcArgumentGate
{
    public const uint ResultCount = 3;
    public const uint From = 0;
    public const uint To = 1;
    public const uint Type = 2;
    public const string Template = "FROM/A,TO,TYPE/K";

    /// <summary>
    /// Parses the exact source template through DOS ReadArgs. A successful
    /// result retains the invocation-owned lease until the caller releases it.
    /// </summary>
    public static int Read(out NativeCommandArguments arguments,
        out int ioError)
    {
        if (NativeCommandArguments.TryRead(Template, ResultCount,
                out arguments))
        {
            ioError = 0;
            return DOS.RETURN_OK;
        }

        ioError = arguments.IoError;
        return arguments.ReturnLevel;
    }

    /// <summary>Reads one live parser result slot without extending its lease.</summary>
    public static bool TryGet(ref NativeCommandArguments arguments,
        uint index, out uint value) => arguments.TryGetResult(index, out value);
}
