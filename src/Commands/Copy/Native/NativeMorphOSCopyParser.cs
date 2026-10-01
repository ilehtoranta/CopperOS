using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Copy's explicit DOS_RDARGS lifetime; result storage belongs to the command.</summary>
public static class NativeMorphOSCopyParser
{
    // Preserve the original help, including its stale BUFFER default and QUIET wording.
    public const string ExtendedHelp =
        "FROM     multiple input files\n" +
        "TO       destination file or directory\n" +
        "PATTERN  a pattern the filenames must match\n" +
        "BUFFER   buffersize for copy buffer (default 200 [100K])\n" +
        "ALL      deep scan into sub directories\n" +
        "DIRECT   copy/delete only: work without any tests or options\n" +
        "CLONE    copy comment, protection bits and date as well\n" +
        "DATES    copy dates\n" +
        "NOPRO    do not copy protection bits\n" +
        "PROX     only copy X,S,P protection bits\n" +
        "COMMENT  copy filecomment\n" +
        "QUIET    suppress all output and requesters\n" +
        "NOREQ    suppress requesters\n" +
        "ERRWARN  do not proceed, when one file failed\n" +
        "MAKEDIR  produce directories\n" +
        "MOVE     delete source files after copying successful\n" +
        "DELETE   do not copy, but delete the source files\n" +
        "HARDLINK make a hardlink to source instead of copying\n" +
        "SOFTLINK make a softlink to source instead of copying\n" +
        "FOLNK    also makes links to directories\n" +
        "FODEL    delete protected files also\n" +
        "FOOVR    also overwrite protected files\n" +
        "DONTOVR  do never overwrite destination\n" +
        "FORCE    DO NOT USE. Call compatibility only.\n";

    public static bool Read(APTR clearedResults, out APTR parser,
        out NativeCommandArguments arguments)
    {
        arguments = default;
        parser = DOS.AllocDosObject((uint)DosObjectType.RdArgs, APTR.Null);
        if (parser.IsNull) return false;
        APTR.WriteUInt32(parser, DosLayout.RDArgs.ExtendedHelp,
            CString.ToUInt32(ExtendedHelp));
        return NativeCommandArguments.TryReadBorrowed(
            NativeMorphOSCopyArgumentGate.Template, clearedResults,
            NativeMorphOSCopyArgumentGate.ResultCount, parser, out arguments);
    }

    public static void Release(ref NativeCommandArguments arguments, ref APTR parser)
    {
        arguments.ReleaseBorrowed();
        var owned = parser;
        parser = APTR.Null;
        if (owned.IsNotNull) DOS.FreeDosObject((uint)DosObjectType.RdArgs, owned);
    }
}
