using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Guest capabilities required by Shell command semantics.
///
/// Implementations are value types so a native command can carry the DOS/Exec
/// boundary explicitly.  The production contract has no managed stream,
/// allocator, callback, or exception dependency.
/// </summary>
public interface IShellPlatform : IAmigaGuestMemory
{
    /// <summary>
    /// Reads the current CLI's default stack for future child commands.  This
    /// capability deliberately does not expose the already-running task stack.
    /// </summary>
    bool TryReadCliDefaultStack(APTR cli, out int stackBytes);

    /// <summary>
    /// Updates only the current CLI's default child-command stack.
    /// </summary>
    bool TryWriteCliDefaultStack(APTR cli, int stackBytes);

    /// <summary>
    /// Updates the failure limit owned by the current CLI's active command
    /// sequence. Sequence teardown, including restoration of the default
    /// limit, remains a DOS/Shell-frame responsibility.
    /// </summary>
    bool TryWriteCliFailureLimit(APTR cli, uint failureLimit);

    /// <summary>Reads the CLI-owned current directory into guest storage.</summary>
    bool TryGetCurrentDirectory(
        APTR cli,
        APTR path,
        uint pathCapacity,
        out uint pathLength);

    /// <summary>Changes the CLI-owned current directory.</summary>
    bool TryChangeCurrentDirectory(APTR cli, APTR path, uint pathLength);

    /// <summary>Creates or replaces a CLI-owned command alias.</summary>
    bool TrySetAlias(
        APTR cli,
        APTR name,
        uint nameLength,
        APTR replacement,
        uint replacementLength);

    /// <summary>Removes one CLI-owned command alias.</summary>
    bool TryRemoveAlias(APTR cli, APTR name, uint nameLength);

    /// <summary>Writes the DOS-owned alias listing for one CLI.</summary>
    bool TryWriteAliases(BPTR output, APTR cli);

    /// <summary>
    /// Applies a fully parsed command-path operation. Entries are packed in
    /// caller-owned guest memory as NUL-terminated strings; DOS owns path
    /// ordering, assigns, locks, and requester policy.
    /// </summary>
    bool TryUpdateCommandPath(
        APTR cli,
        APTR pathBuffer,
        uint pathBytes,
        uint pathCount,
        uint operation,
        uint quiet);

    /// <summary>Writes the DOS-owned command-path listing.</summary>
    bool TryWriteCommandPath(BPTR output, APTR cli, uint quiet);

    /// <summary>
    /// Binds the caller-owned guest script frame to its DOS-owned CLI for the
    /// duration of a Shell run.  This is the fixed-width lookup used by
    /// command callbacks which receive only the CLI pointer.
    /// </summary>
    bool TryBindScriptFrame(APTR cli, APTR frame);

    /// <summary>Removes a previously published script-frame binding.</summary>
    bool TryUnbindScriptFrame(APTR cli, APTR frame);

    /// <summary>
    /// Delivers a fixed-width control request to the active Shell/script
    /// frame. The frame owns block matching, CLI teardown, and quit-result
    /// propagation.
    /// </summary>
    bool TryRequestShellControl(
        APTR cli,
        ShellControlAction action,
        int returnCode);

    /// <summary>Registers a label in the active script frame.</summary>
    bool TryDefineScriptLabel(APTR cli, APTR label, uint labelLength);

    /// <summary>
    /// Requests a bounded forward or beginning-of-file label search. A null
    /// label selects the next label according to the Shell script rules.
    /// </summary>
    bool TrySkipToLabel(
        APTR cli,
        APTR label,
        uint labelLength,
        uint back);

    /// <summary>
    /// Displays a prompt, reads the user's bounded answer, and updates the
    /// active script condition flag. Input/output ownership remains DOS/CLI
    /// state rather than a Shell-managed stream.
    /// </summary>
    bool TryAsk(
        APTR cli,
        BPTR input,
        BPTR output,
        APTR prompt,
        uint promptLength);

    /// <summary>
    /// Evaluates one parsed IF condition against CLI/script state and records
    /// the resulting branch condition in the active script frame.  The
    /// numeric flag selects VAL-mode comparison without changing the operator.
    /// </summary>
    bool TryEvaluateIf(
        APTR cli,
        uint condition,
        uint threshold,
        uint negate,
        uint noRequester,
        uint numeric,
        APTR left,
        uint leftLength,
        APTR right,
        uint rightLength);

    /// <summary>
    /// Executes one script through the active Shell engine. The command
    /// wrapper supplies only the bounded FILE/A path; script parsing and
    /// nested execution stay in the Shell/DOS owner.
    /// </summary>
    ShellScriptExecutionStatus TryExecuteScript(APTR cli, APTR file,
        uint fileLength, out int result);

    /// <summary>
    /// Resumes one DOS-owned script runner without waiting or spinning. A
    /// pending child leaves the runner and its frame resident for a later
    /// poll; a terminal result releases them through the DOS owner.
    /// </summary>
    bool TryPollScriptExecution(APTR cli,
        out ShellScriptExecutionStatus status, out int result);

    /// <summary>Arms the DOS prepared wait for a pending script child.</summary>
    bool TryPrepareScriptWait(APTR cli);

    /// <summary>
    /// Parks a previously prepared script wait. The native Shell entrypoint
    /// converts the returned result into its scheduler gateway disposition.
    /// </summary>
    bool TryParkScriptWait(APTR cli, uint timeoutTicks);

    /// <summary>
    /// Starts a background command through the DOS/Shell process owner. The
    /// command text is caller-owned and is consumed before this call returns;
    /// no Shell state retains the invocation pointer.
    /// </summary>
    bool TryRunCommand(
        APTR cli,
        BPTR input,
        BPTR output,
        BPTR error,
        BPTR currentDirectory,
        APTR continuation,
        APTR command,
        uint commandLength,
        uint detach,
        uint quiet,
        uint stack,
        uint stackPresent,
        int priority,
        uint priorityPresent);

    /// <summary>
    /// Lists or mutates the DOS-owned resident command registry. All strings
    /// are caller-owned and are consumed during the call; purity, segment
    /// lifetime, and use-count checks remain in the resident owner.
    /// </summary>
    bool TryManageResident(
        APTR cli,
        BPTR output,
        APTR name,
        uint nameLength,
        APTR file,
        uint fileLength,
        APTR alias,
        uint aliasLength,
        uint remove,
        uint add,
        uint replace,
        uint force,
        uint system,
        uint defer);

    /// <summary>
    /// Creates a child CLI/Shell through the DOS scheduler. The optional
    /// window and startup-script strings are caller-owned and are consumed
    /// during this call; inheritance and resource ownership stay with DOS.
    /// </summary>
    bool TryCreateShell(
        APTR parentCli,
        ShellLaunchKind kind,
        BPTR input,
        BPTR output,
        BPTR error,
        BPTR currentDirectory,
        APTR continuation,
        APTR window,
        uint windowLength,
        APTR from,
        uint fromLength);

    /// <summary>
    /// Performs one non-blocking completion poll for a DOS-owned child
    /// continuation. The owner reports its current lifecycle state and
    /// result; Shell validates and records terminal transitions without
    /// waiting or reclaiming the record.
    /// </summary>
    bool TryPollShellContinuation(
        APTR cli,
        APTR continuation,
        out ShellProcessContinuationState state,
        out int result);

    /// <summary>
    /// Releases only resources marked as owned by a terminal continuation.
    /// The DOS owner performs actual handle/record closure and may report a
    /// retryable failure; parent-owned streams must never be inferred here.
    /// </summary>
    bool TryReleaseShellContinuation(
        APTR cli,
        APTR continuation,
        uint ownedFlags);

    /// <summary>
    /// Runs the DOS-owned ReadArgs parser over the invocation's bounded
    /// argument source.  The command supplies a guest template and result
    /// array; allocation, template modifiers, IoErr, and source ownership
    /// remain in the DOS owner.
    /// </summary>
    bool TryReadArgs(
        APTR argumentText,
        uint argumentLength,
        APTR template,
        uint templateLength,
        APTR resultArray,
        uint resultBytes,
        out APTR rdArgs);

    /// <summary>Releases the DOS-owned allocations returned by ReadArgs.</summary>
    void FreeArgs(APTR rdArgs);

    /// <summary>
    /// Reads a local variable owned by the current CLI into caller-owned guest
    /// storage. Child-Shell inheritance and variable lifetime remain DOS/CLI
    /// responsibilities rather than Shell-managed maps.
    /// </summary>
    bool TryGetLocalVariable(
        APTR cli,
        APTR name,
        uint nameLength,
        APTR value,
        uint valueCapacity,
        out uint valueLength);

    /// <summary>
    /// Creates or replaces a local variable in the current CLI. The DOS/CLI
    /// owner performs allocation and inheritance handling atomically.
    /// </summary>
    bool TrySetLocalVariable(
        APTR cli,
        APTR name,
        uint nameLength,
        APTR value,
        uint valueLength);

    /// <summary>Writes the current CLI's local-variable listing.</summary>
    bool TryWriteLocalVariables(BPTR output, APTR cli);

    /// <summary>
    /// Reads a global environment variable into caller-owned guest storage.
    /// </summary>
    bool TryGetGlobalVariable(
        APTR name,
        uint nameLength,
        APTR value,
        uint valueCapacity,
        out uint valueLength);

    /// <summary>
    /// Creates or replaces a global environment variable. The save flag is
    /// passed to the DOS owner so persistence policy is not held in Shell
    /// state.
    /// </summary>
    bool TrySetGlobalVariable(
        APTR name,
        uint nameLength,
        APTR value,
        uint valueLength,
        uint save);

    /// <summary>Writes the DOS-owned global environment listing.</summary>
    bool TryWriteGlobalVariables(BPTR output);

    /// <summary>Removes a local CLI variable.</summary>
    bool TryRemoveLocalVariable(APTR cli, APTR name, uint nameLength);

    /// <summary>
    /// Removes a global variable; SAVE requests removal of persistent storage
    /// as well as the active environment entry.
    /// </summary>
    bool TryRemoveGlobalVariable(APTR name, uint nameLength, uint save);

    /// <summary>Clears the console; RESET also resets display/scrollback state.</summary>
    bool ClearConsole(BPTR output, uint reset);

    /// <summary>
    /// Writes the DOS-owned diagnostic for the current CLI's last command.
    /// </summary>
    bool TryWriteWhy(BPTR output, APTR cli);

    /// <summary>
    /// Translates the caller-owned numeric DOS error list and writes the
    /// resulting diagnostics. Formatting and the error catalogue belong to
    /// DOS rather than to a Shell-side host table.
    /// </summary>
    bool TryWriteFault(BPTR output, APTR errorCodes, uint errorCount);

    /// <summary>
    /// Sets a CLI prompt. RESET selects the DOS-defined default prompt;
    /// otherwise the caller-owned decoded text becomes the prompt template.
    /// </summary>
    bool TrySetPrompt(APTR cli, APTR value, uint valueLength, uint reset);

    /// <summary>
    /// Writes exactly the requested guest bytes to a DOS file handle, or a
    /// negative value on failure.  A short non-negative write is also failure
    /// for command output because it would silently truncate the result.
    /// </summary>
    int Write(BPTR handle, APTR source, uint length);

    /// <summary>
    /// Writes one byte to a DOS file handle.  The result is one on success and
    /// negative on failure.
    /// </summary>
    int WriteByte(BPTR handle, byte value);

    /// <summary>
    /// Opens a command-owned output file.  A null handle reports failure.
    /// </summary>
    BPTR OpenOutput(APTR path, uint pathLength);

    /// <summary>
    /// Closes a handle opened by <see cref="OpenOutput"/>.  The result reports
    /// whether the close completed successfully.
    /// </summary>
    bool CloseOutput(BPTR handle);
}
