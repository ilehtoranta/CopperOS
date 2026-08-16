# CopperOS Shell semantic core

This project owns the Shell segment and its internal commands. Internal
commands such as `Echo`, `Stack`, and `Resident` are not copied into
`filesystem/SYS/C` as separate files.

The initial implementation slice defines the fixed-width
`CommandInvocation` and the explicit `IShellPlatform` guest boundary. The
MorphOS `Echo`, `Stack`, `Failat`, `Quit`, `Fault`, `Get`, `Getenv`, `Unset`,
`Unsetenv`, `Cls`, `Set`, `Setenv`, `Alias`, `Unalias`, `Ask`, `Prompt`, `Lab`,
`Skip`, `CD`, `Path`, `Run`, `Resident`, `NewCLI`, `NewShell`, and external
`Execute`
commands now delegate option parsing to the DOS-owned `ReadArgs` capability,
then copy only the typed result slots they need into caller-owned buffers
before calling `FreeArgs`. The broader
Shell lexer remains responsible for command separators, aliases, variables,
redirection syntax, and script state; it is not the option-parser authority.

The Shell also contains the first `Stack` state slice. It reads and updates
only a DOS-owned CLI default-stack field; the platform boundary exposes no
operation that could mutate the stack of the already-running command.

`ShellInternalCommandResolver` recognizes all 32 names in the frozen MorphOS
internal-command inventory before any resident or filesystem lookup. Commands
without an implementation yet remain explicitly identified as internal, so
they cannot accidentally become duplicate `SYS:C` binaries.

`GetCommand` is the first local-variable consumer. It asks the current CLI/DOS
boundary for a bounded value, writes it to the inherited output, and keeps
variable lifetime and child-Shell inheritance outside CopperOS host state.

`SetCommand` implements both the named `Set NAME STRING/F` form and the
no-argument local-variable listing form. Its final-string decoder preserves
internal spaces while handling quotes and star escapes, and the DOS/CLI
boundary owns replacement, enumeration, and allocation atomicity.

`GetenvCommand` and `SetenvCommand` provide the corresponding global
environment forms. `Setenv` recognizes the unquoted `SAVE` switch while
preserving a quoted `"SAVE ..."` as value text; persistent storage remains a
DOS environment responsibility.

`UnsetCommand` and `UnsetenvCommand` implement named deletion and their
no-argument variable-listing forms. They validate all arguments before
invoking the DOS-owned removal or enumeration capability, and `Unsetenv SAVE`
explicitly requests persistent-storage removal.

`ClsCommand` delegates normal and `RESET` console clearing to the console
boundary. `WhyCommand` delegates last-command diagnostic formatting to the
CLI/DOS owner; Shell does not embed an error-message database.

`PromptCommand` implements the bounded MorphOS `Prompt` form. With no
argument it asks the CLI/DOS owner to restore the default prompt; with an
argument it passes the decoded `/F` text, including quoted spaces and star
escapes, to that owner. Prompt substitution expansion and prompt storage stay
in the CLI/DOS boundary rather than in a managed Shell object.

`FailatCommand` changes the active command-sequence failure threshold through
the CLI boundary; sequence teardown remains responsible for restoring the
default. `FaultCommand` collects bounded numeric error codes in guest memory
and delegates translation and formatting to DOS. Both now use their DOS
`ReadArgs` templates (`RCLIM/A/N` and `ERROR/N/M`). `Stack` and `Quit` use
the corresponding optional numeric templates. The simple name/switch
commands use `NAME/A`, `NAME/A,SAVE/S`, and `RESET/S` templates. Named
full-argument commands use DOS `/F` templates and copy their results before
`FreeArgs`. `CdCommand` changes or
displays the DOS-owned current directory without keeping a Shell-side path
object.

`AliasCommand` and `UnaliasCommand` use the same boundary for per-CLI alias
creation, removal, and listing. Named alias replacement text is decoded in
caller-owned guest memory; no host alias map is retained by the Shell.
`Unalias` now consumes the optional `NAME` result from DOS `ReadArgs`.

`PathCommand` consumes the MorphOS `PATH/M,ADD/S,SHOW/S,RESET/S,REMOVE/S,QUIET/S`
result from DOS `ReadArgs`, copying the multiple path values into a bounded,
NUL-separated guest path list before `FreeArgs`. It validates mutually
exclusive operations and only then asks DOS to update or display the CLI
command path.

`QuitCommand` and the no-argument `Else`, `EndIf`, `EndSkip`, `EndCLI`, and
`EndShell` commands emit fixed-width control requests to the active script or
CLI frame. These commands, along with `Why`, validate their empty argument
form through the DOS `ReadArgs` contract before emitting or formatting
anything. The frame, not the command, owns block matching, teardown, and quit
result propagation.

`LabCommand` and `SkipCommand` similarly delegate label registration and
bounded forward/from-start searches to the active script frame. `Skip` accepts
the optional `BACK` switch and validates the complete line before requesting a
jump; its `LABEL,BACK/S` result now comes from DOS `ReadArgs`.

`CdCommand` consumes the optional `DIR` result from DOS `ReadArgs`, while
preserving the no-argument current-directory display form.

`AskCommand` decodes the required prompt in guest memory and delegates the
interactive read plus condition-flag update to the inherited CLI streams.

`IfCommand` consumes the MorphOS `NOT/S,WARN/S,ERROR/S,FAIL/S,,EQ/K,GT/K,GE/K,
VAL/S,EXISTS/K,NOREQ/S` result from DOS `ReadArgs`. The anonymous positional
entry carries the left comparison operand; the command copies both operands
into bounded guest buffers before `FreeArgs` and delegates previous-result,
case rules, filesystem existence, numeric `VAL` mode, and script-branch state
to the DOS/script owner.

`RunCommand` consumes `DETACH/S,QUIET/S,STACK/K/N,PRI/K/N,COMMAND/F` through
the same boundary. It copies the full command text and optional numeric
values before releasing `RDArgs`, then delegates process creation, inherited
streams, detachment, stack selection, and priority policy to DOS/Shell.

`ResidentCommand` consumes
`NAME,FILE,ALIAS/K,REMOVE/S,ADD/S,REPLACE/S,PURE=FORCE/S,SYSTEM/S,DEFER/S`.
It copies optional names and aliases before `FreeArgs`, then delegates
listing, HUNK loading, replacement/removal safety, deferred loading, and
purity qualification to the resident owner.
`ShellResidentEntryCodec` defines the fixed-width guest record used by that
owner. `ShellResidentPolicy` keeps verified-PURE and forced-unsafe admission
distinct, represents deferred entries without a loaded segment, and rejects
acquisition after removal is pending. Use counts are incremented and released
in the guest record; codec reads/writes also validate mapped spans for stored
names and paths. Segment allocation and registry-head ownership stay with DOS.
CopperStart DOS now supplies `DosResidentRegistryCore` for the corresponding
owner boundary: it stores entries on the DOS object chain, performs
case-insensitive lookup, protects active entries from removal/replacement, and
loads/unloads HUNK images transactionally for the `Resident` add/replace/remove
operations. Shell still receives only its `TryManageResident` capability and
does not own the registry.

`NewCliCommand` and `NewShellCommand` consume the shared optional
`WINDOW,FROM` template and copy both values before `FreeArgs`. The platform
boundary receives a `ShellLaunchKind`, leaving child-CLI inheritance,
console/window ownership, startup-script execution, and scheduler cleanup in
DOS/Shell rather than in command code.
Both launch commands now pass a fixed-width `ShellChildInheritance` record
with the parent input, output, error, and current-directory handles. `Run`
uses the same record for background process creation; DOS remains responsible
for copying variables, aliases, command paths, failure policy, and stack
defaults and for closing child-owned resources.
`ShellProcessContinuationCodec` supplies a 52-byte guest record with pending,
running, completed, aborted, and failed states plus the child handles,
command pointer, result, and flags. `CommandInvocation.Continuation` carries
that record pointer to the DOS launch boundary; Shell never waits on or owns a
managed task. `ShellProcessContinuationTransitions` rejects impossible
restarts and terminal-state rewrites while keeping every update bounded.
`Run`, `NewCLI`, and `NewShell` start a supplied pending continuation before
launch and mark it failed when DOS rejects creation; DOS later owns completion
and teardown.
`TryPollShellContinuation` and `ShellProcessContinuationPolling` provide the
non-blocking completion handoff: an owner-reported terminal state is accepted
only when it matches the guest continuation's current state and legal
transition. Polling never waits or reclaims the record.
`ShellProcessContinuationTeardown` passes only explicit ownership flags to DOS;
release failure preserves them for retry, while a successful release marks the
record closed and prevents double teardown.

`ShellCommandDispatcher` is the fixed-width entry point for resolved internal
commands. It maps `ShellInternalCommand` identities to the command wrappers
above and supplies a caller-owned `ShellCommandWorkspace`; name resolution,
option parsing, command semantics, and DOS process ownership therefore remain
separate layers.

`ShellScriptFrameCodec` defines the guest-resident 96-byte frame used by the
script engine. It records inherited handles, line/offset position, failure and
last-result state, condition state, label count, pending control requests, and
guest pointers to the input metadata, nested control chain, label chain, and
optional signal record and a pending external-command continuation.
`ShellScriptControlCodec`
encodes each 36-byte `If`/`Skip` record, including parent, branch flags, block
position, and skip target. `ShellScriptInputCodec` records only the current
bounded buffer span/cursor, while `ShellScriptLabelCodec` and its transitions
maintain a bounded parent-linked label index. These codecs perform bounded
transitions without owning block scheduling or process behavior.

`ShellScriptEngine.Step` consumes one bounded line from the frame's input
through `IShellScriptPlatform`. It dispatches recognized internal commands
through `ShellCommandDispatcher`, suppresses ordinary commands while a frame
is skipping, and sends unknown/external lines back to the DOS/Shell owner. The
stepper advances only the guest line/offset record; it does not preload a
script, create a task, or retain a managed continuation.

Command-scoped redirection is represented by `ShellRedirectionSpec` and
`ShellRedirectionWorkspace`. The bounded parser recognizes `<`, `>`, `>>`,
`2>`, and `2>>` outside quotes, copies the cleaned command and target paths
into caller-owned guest buffers, and rejects duplicate or malformed streams.
`ShellRedirectionTransaction` opens only the requested streams, rolls back
already-opened handles on a later failure, passes the temporary handles to the
internal or external command, and closes them in reverse order. The inherited
frame streams are never replaced, so redirection ownership cannot escape the
single step.

When a caller supplies `ShellScriptAliasWorkspace`, `Step` asks the DOS/CLI
owner to expand one bounded alias before redirection and internal-name
resolution. The owner performs replacement, argument substitution, and alias
recursion limits; Shell retains no alias table. Non-internal names then pass
through `ShellScriptLookupWorkspace` and receive an explicit resident,
explicit-file, current-directory, command-path, script, or not-found
classification before the external execution callback is invoked.

`ShellScriptControlTransitions` connects the frame's `ControlTop` pointer to
the parent-linked records. It opens nested `If`/`Skip` blocks, toggles and
validates `Else`, and restores the parent branch state on close. Record storage
is supplied and reclaimed by the DOS owner.
Opening validates the existing parent chain before publishing a new head and
restores the prior head if branch-state synchronization cannot be committed.

`ShellScriptSignalCodec` records one pending signal, result, sequence, and
acknowledgement sequence in guest memory. `ShellScriptEngine.Step` polls and
acknowledges signals before consuming input: Ctrl-C/break requests a quit,
Ctrl-D requests end-of-CLI, and task termination clears `Active` and requests
end-of-frame. Exec/DOS owns delivery and task teardown; Shell performs no
blocking wait.

`ShellScriptEngine.Run` is the bounded startup-script runner built on the same
stepper. It repeatedly consumes the caller-owned frame/workspace until EOF,
terminal signal, platform failure, or an explicit step limit. The result is a
fixed-width status/result/count record; no managed command stack or preloaded
script is created.
`ShellScriptEngine.Start` is the corresponding startup handoff: it validates
the caller-owned workspace, publishes the initial 96-byte frame, and then
invokes `Run`. `ShellScriptStartRequest` contains only the guest frame pointer,
fixed-width initial frame state, reusable workspace, and step bound, so a DOS
adapter can pass its cursor/handles into Shell without a managed startup
object or retained script text.

Remaining command work is now focused on the unimplemented internal-command
behaviors and the Shell/DOS script-frame integration. Variable listings stay
DOS-owned: the Shell receives only an output boundary and never retains a
managed variable map. Any future option-bearing command must add an exact DOS
template/result-buffer boundary rather than a second command-local option
parser.

The external `CopperOS.Commands` project now contains `ExecuteCommand`. It
parses only the required `FILE/A` path and calls the active Shell engine; it
does not embed a second script parser or host file service.

Production code is kept value-type based and contains no managed allocation,
exceptions, delegates, tasks, host streams, or mandatory floating point.

CopperStart DOS mirrors the Shell child continuation at its native boundary as
`DosChildContinuationRecord` (same 52-byte layout, magic, state values, and
ownership bits) without referencing the Shell assembly. Its terminal release
path closes only flagged child resources, hands task/process teardown to
CopperStart Exec/DOS, and leaves the continuation allocation with its caller.

`DosChildProcessLaunchCore` now creates a Process-sized Exec task from guest
tags, installs a DOS process record with duplicated inherited streams and
directory state, and publishes a DOS-owned `CommandLineInterface` pointer in
the guest Process. Optional guest allocation and attribute tag lists populate
the CLI strings and scalars without managed command text. Failure rolls back
the CLI object, DOS process state, and Exec task as one bounded handoff;
terminal teardown frees the CLI before reclaiming the Process allocation.
The launch boundary also publishes the child CLI, duplicated streams, and
directory in the fixed-width continuation record so later polling/teardown
cannot accidentally close a parent stream.
The task's `ProgramCounter`/`FinalProgramCounter` tags remain the command-image
entry/finalizer boundary. `DosCommandImageCore` now validates an already
loaded HUNK segment list, confirms that the Exec entry tag matches its first
segment, and records the segment list in the child Process. `LoadSeg` and
`UnLoadSeg` now use `DosSegmentLoaderCore`: bounded native HUNK CODE/DATA/BSS
loading, HUNK_RELOC32 application, owner-tracked guest allocations, and
validated deterministic unload. Both long and compact `HUNK_RELOC32SHORT`
groups are supported. Loading goes through DOS `Open`/`Read`/`Close`,
so assigns and normal path resolution remain in the DOS owner.
`DosStartupScriptCore` provides the matching
DOS-owned startup file cursor, normalizing CR/LF lines into caller-owned
buffers; `ShellScriptEngine.Run` consumes those lines through the normal Shell
stepper. A fixed-width `DosChildCliStartup` handoff copies bounded
`WINDOW`/`FROM`-style C strings into DOS-owned CLI BSTR fields with rollback on
overflow or invalid guest memory. Command wrappers retain no host text or
loader object.

`DosShellNativeBridge` is the first concrete adapter slice. It connects the
startup cursor, explicit-span `ReadArgs`/`FreeArgs`, resident registry
lookup/use-count/management, and command-owned output handles to DOS without
crossing managed state into Shell. The remaining adapter work is the broader
`IShellPlatform` capability set: external script-image launch and requester
extensions. Child Process completion, prepared scheduler wake, and Shell
control-signal delivery now cross the DOS/Exec boundary.

`CopperOS.Shell.Dos` now supplies the concrete value-type adapter for the
implemented portion of that boundary. `DosShellPlatform<TDosPlatform>` uses
DOS-owned fixed-width records for CLI state, aliases, variables, paths,
resident entries, handles, startup cursors, and active CLI-to-frame bindings.
The project is compiled as a
standalone local-SDK unit to keep its CopperStart ABI coherent; the host
`CopperOS.Shell` project remains separate. Unsupported requester and
external-image operations return explicit failure until their owning DOS/Exec
mechanisms are available. File-backed `Run` uses the existing Exec task creator when the DOS
adapter has a live `ExecBase`, and terminal teardown releases the corresponding
DOS/Exec resources; resident commands bind and release their use counts through
the child Process, while external script-image launch remains an explicit
boundary. `Execute` runs bounded internal script lines through the
same guest frame engine. Foreground external lines publish a DOS-owned
continuation and yield a `Waiting` step; the final DOS scheduler park/wake
entrypoint parks the native task between polls. Direct
quit/end controls, nested control records, labels, and bounded `If` evaluation
are forwarded through DOS-owned frame bindings. The `copperos.shell.execute-park`
ABI bridges the prepared DOS wait record; Begin/Poll use a four-byte DOS-state
context, and the full Execute export set is freestanding-qualified for
MC68000/020/040 with no managed allocation sites. Child task death completes
the matching continuation before DOS release and signals the prepared parent
wait; `SIGBREAKF_CTRL_C`/`SIGBREAKF_CTRL_D` map to Shell events while
unrelated Exec signals remain pending.
