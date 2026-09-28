using Amiga;

namespace CopperOS.Shell;

/// <summary>Completed command status, captured before DOS cleanup changes IoErr.</summary>
public struct ShellCommandDiagnostics
{
    public int ReturnCode;
    public int IoError;
}

/// <summary>
/// DOS/Shell-owned operations needed to advance one bounded script line.
/// The engine supplies guest buffers and frame pointers; the platform owns
/// file I/O, external command lookup, and process creation.
/// </summary>
public interface IShellScriptPlatform
{
    /// <summary>Clears process IoErr for a new command, retaining prior public CLI status.</summary>
    bool TryBeginCommandDiagnostics(APTR cli);

    /// <summary>Captures a synchronous command's result; success clears stale IoErr.</summary>
    bool TryCaptureCommandDiagnostics(APTR cli, int returnCode,
        out ShellCommandDiagnostics diagnostics);

    /// <summary>
    /// Reads a retired child's saved outcome before its continuation is
    /// acknowledged. This must not use the parent's current IoErr or live
    /// child handles, which may already have been reclaimed and reused.
    /// </summary>
    bool TryReadContinuationDiagnostics(APTR cli, APTR continuation,
        out ShellCommandDiagnostics diagnostics);

    /// <summary>Reads the last published CLI outcome without changing process IoErr.</summary>
    bool TryReadPublishedCommandDiagnostics(APTR cli,
        out ShellCommandDiagnostics diagnostics);

    /// <summary>Publishes a completed command's status to its public CLI record.</summary>
    bool TryPublishCommandDiagnostics(APTR cli,
        in ShellCommandDiagnostics diagnostics);

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
        in ShellScriptLookupWorkspace workspace,
        out ShellScriptLookupResult lookup);

    /// <summary>
    /// Writes one DOS-owned prompt for an interactive CLI. Prompt storage and
    /// substitutions remain owned by the CLI/DOS boundary.
    /// </summary>
    bool TryWriteScriptPrompt(APTR cli, BPTR output);

    /// <summary>Copies a bounded snapshot of the active prompt template.</summary>
    bool TryCopyScriptPromptTemplate(APTR cli, APTR destination,
        uint destinationCapacity, out ShellScriptPromptTemplate template);

    /// <summary>Writes one literal prompt segment with DOS substitutions.</summary>
    bool TryWriteScriptPromptLiteral(APTR cli, BPTR output,
        in ShellScriptPromptSegment segment);

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
        in ShellScriptCommandInvocation command,
        in ShellScriptLookupResult lookup,
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

    /// <summary>
    /// Closes the validated source reader and publishes a pre-opened Execute
    /// work-file reader as one DOS-runner transaction.
    /// </summary>
    bool TryPublishScriptInput(
        APTR cli,
        APTR frame,
        BPTR source,
        BPTR replacement,
        APTR temporaryPath,
        uint temporaryPathLength);

    /// <summary>Removes an unpublished Execute work-file after its handle closes.</summary>
    bool TryDeleteScriptPath(APTR cli, APTR path, uint pathLength);

	/// <summary>Tracks prompt-capture path and reader ownership in the runner.</summary>
	bool TrySetScriptPromptCapture(APTR cli, APTR path, uint pathLength,
		BPTR input);

    bool TryCloseScriptRedirection(APTR cli, BPTR handle);
}
