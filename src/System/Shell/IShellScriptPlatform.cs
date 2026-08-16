using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// DOS/Shell-owned operations needed to advance one bounded script line.
/// The engine supplies guest buffers and frame pointers; the platform owns
/// file I/O, external command lookup, and process creation.
/// </summary>
public interface IShellScriptPlatform
{
    /// <summary>
    /// Polls Exec/DOS signal state without blocking. The platform owns signal
    /// masks and task delivery; Shell receives only a fixed-width event.
    /// </summary>
    bool TryPollScriptSignal(
        APTR cli,
        out ShellScriptSignalEvent signal);

    /// <summary>Acknowledges one already-delivered signal event.</summary>
    bool TryAcknowledgeScriptSignal(
        APTR cli,
        in ShellScriptSignalEvent signal);

    /// <summary>
    /// Expands the command alias owned by the current CLI, if any. The DOS
    /// owner performs alias lookup, argument substitution, recursion bounds,
    /// and replacement lifetime. A zero <paramref name="expanded"/> leaves
    /// the source line unchanged; a one writes the replacement to the
    /// caller-owned destination buffer.
    /// </summary>
    bool TryExpandScriptAlias(
        APTR cli,
        APTR source,
        uint sourceLength,
        APTR destination,
        uint destinationCapacity,
        out uint expanded,
        out uint expandedLength);

    /// <summary>
    /// Resolves a non-internal command after alias expansion. The DOS owner
    /// applies the MorphOS order: resident entries, explicitly named files,
    /// the current directory, then the CLI command path. HUNK/script
    /// classification and lookup policy remain outside the Shell. A
    /// <see cref="ShellScriptLookupKind.NotFound"/> result is successful
    /// classification, not a platform-call failure.
    /// </summary>
    bool TryLookupScriptCommand(
        APTR cli,
        APTR name,
        uint nameLength,
        APTR path,
        uint pathCapacity,
        out ShellScriptLookupKind kind,
        out uint pathLength);

    bool TryReadScriptLine(
        APTR cli,
        BPTR input,
        uint currentLine,
        uint currentOffset,
        APTR destination,
        uint destinationCapacity,
        out uint lineLength,
        out uint nextLine,
        out uint nextOffset,
        out uint endOfFile);

    bool TryExecuteScriptCommand(
        APTR cli,
        APTR frame,
        APTR line,
        uint lineLength,
        ShellScriptLookupKind lookupKind,
        APTR resolvedPath,
        uint resolvedPathLength,
        BPTR input,
        BPTR output,
        BPTR error,
        out int result,
        out APTR continuation);

    bool TryOpenScriptInput(
        APTR cli,
        APTR path,
        uint pathLength,
        out BPTR handle);

    bool TryOpenScriptOutput(
        APTR cli,
        APTR path,
        uint pathLength,
        uint append,
        out BPTR handle);

    bool TryCloseScriptRedirection(APTR cli, BPTR handle);
}
