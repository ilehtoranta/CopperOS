# CopperOS Shell semantic core

This project owns the Shell segment and its internal commands. Internal
commands such as `Echo`, `Stack`, and `Resident` are not copied into
`filesystem/SYS/C` as separate files.

The shared public Exec/DOS refactor is accepted on CopperStart and Kickstart
3.1 for the v22 proof image. Current artifact hashes, all eight requirements,
the 31-command coverage map, original-V40 comparisons and the final independent
runtime audit are in [Shared Shell acceptance](../../../docs/SharedShellAcceptance.md).
The implementation notes below also describe extensions beyond stock V40;
acceptance does not imply exhaustive MorphOS language conformance.

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
`Ask` follows the Kickstart 3.1 convention: a yes answer returns WARN (5),
while no or Return returns OK (0).

The Shell also contains the first `Stack` state slice. It reads and updates
only a DOS-owned CLI default-stack field; the platform boundary exposes no
operation that could mutate the stack of the already-running command.

`ShellInternalCommandResolver` recognizes the 31 names in the frozen MorphOS
internal-command inventory before any resident or filesystem lookup. The
dispatcher has an implementation for each recognized name.

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
escapes, to that owner. Prompt storage and `%N`, `%S`, and `%R` expansion use
the CLI/DOS boundary rather than a managed Shell object.

The interactive runner emits one prompt before reading each command only when
the CLI is marked interactive and its current input is the standard input.
New DOS CLIs start with the MorphOS default `%N.%S>`; an explicitly empty
prompt stays empty, and `Prompt` without arguments restores the default.
Expansion supports `%N`, `%S`, and `%R` without appending a space. A bounded
prompt parser also handles backtick command substitutions through the normal
Shell alias, redirection, internal-command, and external-command paths. Their
default output is captured to a runner-owned temporary file and copied in
bounded reads; explicit command redirection remains in effect. The resumable
expansion cursor and capture handle are carried in the named
`ShellScriptPromptExpansionState` frame field. Captured bytes are currently
preserved verbatim; exact MorphOS newline normalization still needs
differential verification.

`FailatCommand` changes the active command-sequence failure threshold through
the CLI boundary; sequence teardown remains responsible for restoring the
default. `FaultCommand` collects bounded numeric error codes in guest memory
and delegates translation and formatting to DOS. Both now use their DOS
`ReadArgs` templates (`RCLIM/N` and `ERROR/N/M`). An argumentless `Failat`
displays the CLI-owned current limit; a supplied limit must be positive.
`Stack` and `Quit` use
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

`LabCommand` accepts source markers without runtime registration, as on V40.
`SkipCommand` delegates forward/from-start source searches to the active script frame. `Skip` accepts
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
case rules, numeric `VAL` mode, and script-branch state to the DOS/script
owner. `EXISTS` probes with a shared DOS lock so files and directories both
match; a missing object is treated as a false condition and its expected
lookup `IoErr` is cleared.

`RunCommand` copies the full, undecoded remainder for a background Shell.
On V40, words such as `DETACH`, `QUIET`, `STACK`, and `PRI` are part of that
command, not `Run` options. The native background launcher creates a
Shell-owned worker through public DOS and Exec interfaces.

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
DOS/Shell rather than in command code. The native DOS adapter accepts bounded
`CON:` WINDOW specifications and transfers duplicated child streams.
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

`ShellScriptFrameCodec` defines the guest-resident 152-byte version-5 frame used by the
script engine. It records inherited handles, line/offset position, failure and
last-result state, condition state, label count, pending control requests, and
guest pointers to the input metadata, nested control chain, label chain, and
optional signal record and a pending external-command continuation. Its
`ShellScriptDeferredCommandState` is a named record for the unconsumed right
side of a compound command; the byte layout remains private to the frame
codec.
`ShellScriptControlCodec`
encodes each 36-byte `If`/`Skip` record, including parent, branch flags, block
position, and skip target. `ShellScriptInputCodec` records only the current
bounded buffer span/cursor, while `ShellScriptLabelCodec` and its transitions
retain the bounded parent-linked label index for callers that supply records.
The public runner scans source text and does not populate that index when LAB executes.
These codecs perform bounded
transitions without owning block scheduling or process behavior.

`ShellScriptEngine.Step` consumes one command step from a bounded line in the
frame's input through `IShellScriptPlatform`. It dispatches recognized internal commands
through `ShellCommandDispatcher`, suppresses ordinary commands while a frame
is skipping, and sends unknown/external lines back to the DOS/Shell owner. The
stepper advances only the guest line/offset record; it does not preload a
script, create a task, or retain a managed continuation.

`ShellScriptCompoundParser` recognizes whitespace-delimited `&&`, `||`, and
`|` outside quotes, star escapes, and semicolon comments. Operator identity is
carried as a named enum field in `ShellScriptCompoundSplit`. The runner
executes `&&` by comparing the left command's return level with the frame's
`Failat` threshold. A simple `||` pair currently runs sequentially through the
inherited output handle regardless of the left command's return level. The
operator kind, deferred text, and physical input cursor are retained in the
named `ShellScriptDeferredCommandState`, so external commands can complete
asynchronously before the chain resumes. This does not yet provide the
composable common stream needed when concatenated output feeds a later pipeline
(as in the reference example), or whole-expression redirection. A recognized
`|` line still fails explicitly instead of being passed as ordinary arguments;
pipeline scheduling and Queue-Handler integration remain required. Exact
MorphOS 3.20 behavior remains to be verified. The runner's caller-owned
workspace must remain mapped and stable until that logical line completes.

Command-scoped redirection is represented by `ShellRedirectionSpec` and
`ShellRedirectionWorkspace`. The bounded parser recognizes `<`, `>`, `>>`,
`*>`, `*>>`, and `*<>` outside quotes, copies the cleaned command and target
paths into caller-owned guest buffers, and rejects duplicate or malformed
streams.
`*<>` aliases the error handle to the selected output handle without creating
a second ownership claim. Operators must begin at a whitespace-delimited
boundary; the former `2>` and `2>>` spellings are not MorphOS redirection
operators. The explicit error-stream operators are later AmigaDOS extensions:
the [DOS reference, section 15.1.1](https://developer.amigaos3.net/sites/default/files/downloads/2024-10/Amiga_ROM_Kernel_Reference_Manual_DOS.pdf)
dates them to version 45, after Kickstart 3.1's V40 DOS. They are provided by
this Shell on V40 as extensions and should not be assumed to work in the
original V40 Shell.
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
the caller-owned workspace, publishes the initial 152-byte frame, and then
invokes `Run`. `ShellScriptStartRequest` contains only the guest frame pointer,
fixed-width initial frame state, reusable workspace, and step bound, so a DOS
adapter can pass its cursor/handles into Shell without a managed startup
object or retained script text.

Behavioral coverage and the shared public-interface implementation are described
below. Variable listings use the DOS/Shell boundary; the Shell retains no
managed variable map. Option-bearing commands use DOS templates and result
buffers rather than a second command-local option parser.

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

The native Shell owns its process command state, command-file BSTR, runner
records, workspace, frame resources, command sources, and the CLI command-path
lock list. Its public DOS/Exec operations
include command lookup and synchronous foreground execution, file and argument
services, prompt setting, template reading and rendering, local variables and aliases,
global variable read/write/delete, and `Why`/`Fault` diagnostics.
The V40 command search also checks the current directory and the implicit
`C:` location around the CLI lock list. `Path` displays these two implicit
entries even when `Path RESET` clears the explicit list. `Path` arguments use
the runner's bounded workspace; traversal of the live CLI list has no fixed
entry limit and rejects cycles before changing the list.
The generic host platform remains useful for host regression tests.
Before each native runner step, the frame refreshes its borrowed current-directory
lock from the public Process record. `CD` or a foreground command can replace
and release the previous lock; retaining the initial snapshot across steps would
otherwise leave later command invocations with a stale lock.

Ordinary and SYSTEM resident command add, replace and remove operations use
public DOS segment and file vectors. SYSTEM admission creates a permanent
segment with use count -1. A system entry cannot be removed; explicit REPLACE
can add another entry with the same name while preserving the system entry.
An original Kickstart 3.1 Shell comparison checks these cases, their DOS
errors, and the order of interleaved normal and system entries. The proof's
extra `Cls` internal and registered `Shell` module remain visible in its
listing. Operations check the PURE bit unless forced, preserve an
old segment if publication of a replacement fails, and protect the segment
use count under Exec `Forbid`. Duplicate names are permitted for `ADD`, as on
the V40 Shell; an explicit `REPLACE` requires an existing name. The command
reports ordinary resident-operation failures at warning level 5 and preserves
the DOS error as the CLI secondary result. If DOS refuses to remove an old
normal entry after a replacement SYSTEM entry is published, the old entry is
retained and the permanent new entry also remains; public `RemSegment` cannot
roll back a SYSTEM entry. The command
source leaves the CLI's `FailAt` limit in effect when it completes, including
when a nested script changes the limit.
The native `Resident` listing copies segment names and counts under Exec
`Forbid` and writes the snapshot afterward. DOS V50 and newer use
`FindSegmentTagList` to locate the first entries, then walk the public Segment
links to retain their order. V40 has no public name-independent iterator, so
its isolated compatibility path reads the SDK RootNode/DosInfo resident-list head.
That V40 path depends on the 3.1 layout and is tested against the original ROM.
Backtick capture suppresses disk requesters while looking for `T:` and falls
back to the current volume's `:T/` directory if the assign is unavailable. The
Shell owns the capture file and removes it after reading.

For a launcher that starts a background Shell, the command stepper leaves
`Run` redirections in the child command text. The existing private direct
executable launcher instead retains its parent-opened redirection handles;
this temporary distinction avoids breaking it before conversion. Like the
original 3.1 Shell, `Run` passes quotation and star escapes through and treats
`DETACH`, `QUIET`, `STACK`, and `PRI` as command text. An empty `Run` starts
an empty child, as on V40. A worker can use a permanently registered system
`Shell` segment directly. For the fingerprinted proof-image Shell package,
when the caller is not permanent the launcher loads a matching `L:Shell-Seg`
and transfers its ownership to DOS for the new process. The package header
contains a build fingerprint; the installed file must have the same fingerprint
as the running package. This is a build identity check, not file authentication.
Each such worker needs a separate Shell image in memory. Other package formats
or an absent or mismatched `L:Shell-Seg` cannot use this fallback. It can borrow
its parent's input and error handles, so their owner must keep them alive
while the worker uses them. A worker that executes a command flushes output
before it exits; an empty worker does not touch its borrowed output.
With the earlier, larger Shell on the 2 MiB Kickstart 3.1 proof configuration,
a burst of concurrent nonresident workers exhausted public memory: the diagnostic in
`artifacts/shell-public-nonresident-run-results-ks-v4` recorded return code 20
and `ERROR_NO_FREE_STORE` for one launch. That broad replay did not pass.
After removing private DOS roots, the 186772-byte shared Shell passes the
nonresident broad workload on both systems in
`artifacts/shell-public-only-nonresident-ks-v5` and
`artifacts/shell-public-only-nonresident-copper-v3`, including worker retirement
and Shell exit. Separate worker images still consume memory; these results
establish the tested workload rather than an arbitrary concurrency limit.
When borrowed input is interactive, the launcher temporarily selects its
handler as the process's console task, opens `*` through DOS, restores the
previous console task, and leases the separate handle to the worker. This
prevents `RunCommand` from replacing the interactive Shell's input buffer while
the parent is reading. The clone is closed after its last Shell worker releases
it. A borrowed `NIL:` input is also reopened and leased to the worker, so its
parent may close its own handle. Other non-interactive shared input handles are
not isolated by this path.
Redirections immediately following `Run` apply to the launcher, while later
ones apply to the child. Kickstart 3.1 comparisons show that a leading output
file receives the `[CLI n]` notice, while an unredirected child of the tested
interactive launcher uses an interactive output. A worker opens its own console
handle when the launcher has a console task, or `NIL:` when it has none; the
caller retains its output handle. An explicit leading `NIL:` is
passed to the worker. A launcher-opened ordinary output is closed after the
notice unless it is also the child's error handle. These comparisons cover
ordinary files and `NIL:`; special devices and all input/error redirection
combinations have not been compared with the original.
Shell-owned stream leases are retained by nested background workers and closed
after the last holder exits. Streams borrowed from an external owner remain
that owner's responsibility. A delayed nested-worker probe checks the worker's
own standard output and redirected output after its launcher can retire.

`NewCLI` and `NewShell` launched by the shared command runner use public DOS
process creation. They open independent console handles, copy the parent's
prompt and current-directory name, inherit its DOS-owned path and local
variables, and run the requested startup file before reading the console.
When a continuation requests a particular directory, the launcher temporarily
selects its own duplicate and lets `CreateNewProc` inherit a separate lock. It
then restores the parent directory and releases its temporary lock, including
when process creation fails. This avoids depending on ownership of an explicit
`NP_CurrentDir` on failure.
This launcher uses the same permanent-segment or fingerprinted-copy lifetime
rules as the background worker. Kickstart 3.1 tests exercise both `Run` and
`NewShell` from a nonresident proof-image Shell. With no `WINDOW`, it opens
`CON:0/0/640/200/CopperOS Shell/CLOSE`; the original's default window geometry
has not been compared. Kickstart 3.1 guest tests cover both
command names, startup execution, inherited aliases, variables, path,
directory and stack, and process exit. Separate Kickstart 3.1 tests cover the
default startup file, an explicit console window, and an inherited prompt.
CopperStart tests cover both command names and the same inherited state,
including the prompt, and exit behavior. A missing `FROM` file leaves the new
Shell interactive, as in a separate Kickstart ROM Shell control.

The shared native platform uses public Exec/DOS services and Shell-owned
records for runner discovery, command state, script cleanup, child launch,
continuation ownership and waiting. It never retains a supplied CopperStart
DOS-state pointer. Its constructor keeps an ignored argument for existing
callers and a reserved four-byte native transport slot. The generic
`DosShellPlatform<TPlatform>` adapter remains for legacy host fixtures; it is
not the native interpreter's service owner. A standard V40 `Run`, `NewShell`
or `NewCLI` uses public DOS process creation. Caller-provided continuation
observations remain owned until their tasks have actually exited.
Native platform callers can also request a command stack (64 bytes through
16 MiB) and a signed task priority (-128 through 127) on the public Run path.
The worker keeps at least its own 64 KiB stack, while the requested command
stack is stored in the child CLI. Invalid sizes, priorities and presence flags
fail before stream ownership transfers. The V40 Run command still passes its
unparsed remainder to the child; these platform options add no command syntax.
Caller-owned continuation records can be polled and released. Public observers
also accept frame-owned records allocated by ShellFrameResources; acknowledgement
or detachment releases them, while a detached worker retains its own observation.
Native frame teardown detaches its public observations and leaves allocations
belonging to a supplied legacy context with their caller. The legacy generic
adapter retains its own cleanup protocol. The mixed-owner regression verifies
both ownership boundaries before its fixture releases the legacy records.
The public wait path polls completion through DOS Delay, increasing its interval
from one tick to at most 25 ticks while a worker remains pending. Each delay is
clipped to a finite caller's remaining tick budget, and IoErr is restored. A
one-tick timer can expire during call overhead before the caller blocks;
repeating that interval can starve a ready lower-priority worker. The same public
implementation runs on both systems, without a scheduler adapter. The full
heap-pressure/priority regression passes on CopperStart with all `0x3fff` stages.

Earlier CopperStart fixes installed timer.device at reset using ROM-resident
dispatch and corrected CheckIO for requests already consumed by DoIO or WaitIO.
Those historical missing-device failures are separate from the later observed
one-tick nonblocking delay on a working timer. Current artifacts, original-ROM
comparisons and completed acceptance checks are recorded in
[Shared Shell acceptance](../../../docs/SharedShellAcceptance.md).
Wait preparation and parking accept Shell-owned continuations. Ordinary
V40 Run remains asynchronous and does not allocate a parent wait record.
Callers requesting observation can provide a continuation; frame-owned
records are allocated and reclaimed through ShellFrameResources. Extended
native DETACH/QUIET options return ERROR_NOT_IMPLEMENTED before transferring
stream ownership; they are not V40 Run command syntax.
Deferred `ADD` uses the public V50 `AddSegmentTagList` API when the installed DOS
exports it. The Shell checks both the DOS version and vector extent first; on a
V40 DOS, deferred and resident-alias extensions return `ERROR_NOT_IMPLEMENTED`
instead of calling a CopperStart-private registry. The V50 deferred path retains
the forced-resident warning and leaves loading and segment ownership with DOS.
The native foreground runner, environment listing and resident management
use public services. The exported `execute-begin`, `execute-poll` and
`execute-park` entries preserve their register signatures but ignore the old
context and ExecBase arguments. Begin requires an initialized CLI with
Shell-owned process state. No error or pending-command path initializes a
private DOS context. Child entry also leaves an opaque previous
`pr_ShellPrivate` value uninterpreted: packet startup's public CliInit result
explicitly controls whether to resume standard input.
The synchronous script path has a native register-ABI probe; the caller-owned
Run/NewShell continuations and public wait path have focused native probes.
Probes on the original Kickstart 3.1 ROM and CopperStart held a
`CreateNewProc` child inside its exit callback and observed that it was still
visible as both an Exec task and a CLI.
An exit callback can therefore notify an owner, but it is not evidence that
the child and its DOS resources have already retired. `FindCliProc` numbers
can also be reused, so a replacement wait protocol must retain a separate
identity and synchronize ownership when the parent exits. See the native-root
test README for the probe and its limits.
The public Exec task-identity reservation provides distinct names while either
the polling owner or its child remains alive, and is used by the public `Run`
continuation path. Its guest probe passes on both systems.
The proof's directly created Shell process and DOS packet startup both enter
the shared initialized-CLI runner on CopperStart and an original ROM. The
bootstrap identifies its directly created child through the public task user
data field; it does not inspect CopperStart's private DOS context. Published
CLI path nodes use Exec vectors so both DOS implementations can release the
live path when the process exits. Focused native probes do not verify the
complete shared Shell.
For script files the Shell opens itself, the public DOS `SetVBuf` call requests
a full buffer on noninteractive handles; if DOS cannot allocate one, reading
continues without requiring that request to succeed. The DOS error from opening is
preserved. Borrowed command input and interactive handles are not rebuffered.
This avoids the slow per-character disk reads observed in a legacy CopperStart
boot: the 65-argument Path startup script then completed within its native test
bound. Script control flow and seeking were also replayed on Kickstart 3.1.
For packet-started `System` calls, the initialized CLI can supply a command
stream distinct from the standard input inherited by commands. The shared
runner reads the command stream while passing the standard input to the
command. A proof-image test on both CopperStart and Kickstart 3.1 checks that a
synchronous command receives the supplied input and leaves its caller's
handle usable; it also covers synchronous and asynchronous completion. The
earlier proof image returned from `System` on Kickstart but supplied an empty
input to the command. See `tests/Shell.Dos.NativeRoot/README.md` for the guest
tests and their limits. The legacy CopperStart boot registers the same
packet-started runner for DOS-launched Shells; its independently created
workers continue to use the direct worker entry.
The proof build no longer roots the legacy queue-handler task or DOS process-
return exports in the portable Shell image. See `tests/Shell.Dos.NativeRoot/README.md`
for the scope and limitations of the standalone native probes and boot replays.
The v22 packaged image passes the recorded host, native, original-ROM and
CopperStart acceptance matrix; the linked acceptance document defines its scope.

The shared runner owns failure diagnostics as well as status publication.
It obtains catalogue text through public DOS Fault, uses the command's selected
streams, and flushes diagnostic boundaries so direct Write and buffered FPutC
fragments retain their order. Missing-command lookup does not create an ordinary
stdout target, as on V40; explicit error redirection and *<> remain available.
CD directory and ReadArgs failures return 20, matching original V40 captures.
Consumed command syntax errors recover in both scripts and the console, while
malformed script directives remain terminal. A FailAt stop completes the command
sequence with its DOS result and can return to the console. The new console
frame retains the CLI return code for IF ERROR. Current native evidence is
summarized in `docs/SharedShellAcceptance.md`; the native-root README retains
the probe contracts and investigation history.

On the original V40 Shell, a no-argument `SetEnv` invoked the external `LIST`
command with `env: QUICK NOHEAD FILES`. With `LIST` available it printed file
names in `ENV:` (not their contents or subdirectory names); without it, the
Shell reported the missing command. Fresh original-ROM CLI captures publish
return code zero and secondary error zero for both no-argument commands,
including a missing LIST and deterministic LIST fixtures returning 0 and 10. This
supersedes the earlier warning-level-5 assumption.
The native Shell now uses public command lookup and foreground execution for
that listing; native verification is described in `tests/Shell.Dos.NativeRoot`.

Built-in service diagnostics have a separate selected destination in
`CommandInvocation`. It defaults to the command's standard output, as observed
on V40, and changes to the error stream only for explicit error redirection.
Resident mutation messages and global-listing failure messages use that
destination; Resident listings and LIST's ordinary output retain standard
output. Error-stream inheritance for child processes is independent of this
built-in reporting policy. The `*>`, `*>>`, and `*<>` forms are extensions to
the V40 syntax, and are tested separately from the original-Shell controls.
Resident ReadArgs failures also use the Shell's catalogue diagnostic path:
an extra positional argument reports `wrong number of arguments` with result
10/118, matching the original V40 Shell. Resident admission/removal failures
retain their service-owned message and WARN result, so they are not reported
twice by that parser-error path.

Additional original-ROM controls establish that WHY publishes WARN (5), even
after a successful command, and retains the preceding CLI secondary error.
The runner preserves that error across diagnostic formatting and cleanup.
Forward SKIP scans source text rather than using a cached future label:
ENDSKIP stops the scan with WARN, while a missing target consumes the rest of
the command sequence and returns ERROR with object-not-found. Standalone
ENDSKIP succeeds without closing an unrelated IF record. BACK searches from
the beginning of the current source. Accepted native comparison evidence is
tracked in `docs/SharedShellAcceptance.md`.
An omitted SKIP target matches an unnamed LAB on the tested V40 ROM; it does
not match an arbitrary named LAB. Successful scans consume the matched LAB
line, so a backward jump resumes at its following command. An unnamed LAB
is also a successful command in ordinary execution.
ReadArgs switches passed to boolean-valued services are normalized from DOS
TRUE as well as 1, including SAVE, BACK and CLS RESET. Stack displays the full
`Current stack size is ... bytes` line observed on V40.

CLS writes a form feed to clear the selected console output. CLS RESET uses
the console device's Reset to Initial State sequence, ESC followed by lower
case c, so character modes and display defaults are reset as well. The shared
formatter is used by both platforms and reports a failed byte write. This
uses the documented public console output protocol through DOS; it does not
require a private console service. See the
[console device reference](https://wiki.amigaos.net/wiki/Console_Device).
