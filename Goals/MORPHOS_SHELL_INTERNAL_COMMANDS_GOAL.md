# Goal: MorphOS-Referenced Shell and Internal Commands for CopperOS

## Summary

Implement the CopperOS Shell engine and the internal commands documented by
MorphOS 3.20. This goal owns internal commands such as `Stack` and `Echo`; they
must not be duplicated as files in `SYS:C`.

The Shell is developed under `src/System/Shell`, built as freestanding
MC68000 code, and ultimately staged as `filesystem/SYS/L/Shell-Seg` and
registered as the system CLI/Shell segment. It provides the execution engine
used by disk commands, scripts, resident commands, startup files, and child
Shells.

Use the [MorphOS command inventory](https://library.morph.zone/Shell_Commands)
and MorphOS 3.20 SDK as behavioral authorities. The inventory explicitly marks
the commands in this goal as internal. Published source releases may support
research only after their licenses are verified; do not copy unlicensed
implementation text.

Production code uses constrained, C-like C# lowered by CopperSharp. It uses no
exceptions, managed runtime, managed allocation, CLR collections, delegates,
tasks, reflection, boxing, host services, or mandatory floating-point hardware.

## Goal Contract and Architecture

- Implement exactly these documented internal commands: `Alias`, `Ask`, `CD`,
  `Cls`, `Echo`, `Else`, `EndCLI`, `EndIf`, `EndShell`, `EndSkip`, `Failat`,
  `Fault`, `Get`, `Getenv`, `If`, `Lab`, `NewCLI`, `NewShell`, `Path`, `Prompt`,
  `Quit`, `Resident`, `Run`, `Set`, `Setenv`, `Skip`, `Stack`, `Unalias`,
  `Unset`, `Unsetenv`, and `Why`.
- Keep internal commands inside the Shell segment. The external command goal
  may call their shared semantics but must not stage internal-command binaries
  in `filesystem/SYS/C`.
- Store all Shell and CLI state in fixed-width guest memory owned by Exec or
  DOS. Do not use host dictionaries, strings, streams, tasks, or process state
  as the semantic owner.
- Use a constrained value-type Shell platform for guest memory, DOS/Exec calls,
  console I/O, process creation, segment loading, resident lookup, signals,
  scheduler waits, and command callbacks.
- Represent blocking operations and child-command execution with explicit
  fixed-width continuation records. Host code never recursively runs a guest
  command, handler, or callback.
- Expected failures use documented return levels and `IoErr`; production code
  contains no `throw`, `try`, or `catch`.
- Use integer or fixed-point operations internally. Carry public floating-point
  values only if an official ABI requires them and never require an FPU.
- Preserve independent input, output, error, current-directory, command-path,
  variable, alias, stack, failure-level, and resident state for each CLI.
- Use CopperStart DOS as the semantic owner for CLI objects, HUNK loading,
  resident segments, process creation, variables, file handles, and handler
  traffic. Correct missing CopperStart behavior rather than maintaining a
  second incompatible implementation.
- Use `CopperSharp.Sdk.Amiga` as the sole owner of public Amiga/MorphOS ABI
  declarations and codecs.
- Prefer named fixed-width structs and typed codecs for public and private
  records, including ReadArgs results and child-process ownership. Algorithms
  access fields by name; keep unavoidable guest ABI offsets inside the owning
  codec, with layout and bounds tests. Do not duplicate DOS-private layouts in
  Shell commands.

## Progressive Implementation Goals

### SG00 — Freeze authorities and Shell inventory

- Inventory MorphOS 3.20 command-line syntax, quoting, `*` escapes, variable
  expansion, aliases, redirection, comments, command lookup, scripts, return
  levels, signals, resident commands, stack selection, and startup behavior.
- Record the template, aliases, help behavior, state mutations, return codes,
  `IoErr`, break signals, minimum stack, and version expectations for every
  internal command.
- Create a Shell completion ledger, progress log, and qualification report.
- Give every observed MorphOS behavior an official-document, SDK, differential,
  or explicitly unknown provenance. Do not treat an unrelated Shell as semantic
  authority.

### SG01 — Complete CLI and execution prerequisites

- Audit CopperStart DOS support for CLI/process structures, streams, command
  paths, variables, resident segments, `LoadSeg`, `UnLoadSeg`, `AddSegment`,
  `FindSegment`, `RemSegment`, `RunCommand`, process creation, signals, and
  current-directory state.
- Add missing public layouts or constants to CopperSharp.Sdk.Amiga first, with
  packing, offset, numeric-value, signedness, and big-endian codec tests.
- Complete missing CopperStart semantics through the existing DOS scheduler and
  packet boundaries.
- Define fixed-width Shell invocation, parser, script-frame, redirection,
  resident-use, and continuation records.

### SG02 — Implement parsing and command resolution

- Implement bounded command-line tokenization, MorphOS/Amiga quoting and `*`
  escaping, variable and alias expansion, comments, redirection, and documented
  pipeline behavior.
- Resolve commands in this order: aliases, internal commands, resident
  commands, explicitly named files, current directory, and the CLI command
  path, matching the frozen MorphOS rules.
- Detect executable HUNK files, script-protected files, explicit `Execute`
  invocation, and documented `#!` scripts without guessing from file content
  beyond the admitted rules.
- Route resolved internal identities through one fixed-width command
  dispatcher and caller-owned workspace record; dispatcher code must not own
  option parsing, process state, or retained guest pointers.
- Preserve input/output/error handles across nested execution and close only
  resources owned by the current command frame.
- Store active script-frame state in the fixed-width guest record defined by
  `ShellScriptFrameCodec`; command wrappers may request transitions but must
  not retain a managed frame or implement a second scheduler.
- Represent retained parser, pipeline, process, and queue state with named
  fixed-width structs and enums. Keep byte offsets confined to the codecs
  that own an external or serialized memory layout.
- Migrate internal command options to the CopperStart DOS `ReadArgs` contract;
  do not duplicate template parsing in Shell commands. Keep explicit
  `FreeArgs` cleanup on every successful and failed command path.
- Apply explicit bounds to expansion depth, alias recursion, script nesting,
  token count, and temporary guest-memory use. Report deterministic failures
  rather than overflowing or looping.

Progress note: `ShellRedirectionParser` provides a bounded, caller-buffered
subset for `<`, `>`, `>>`, `*>`, `*>>`, and `*<>`, while preserving quoted
operators and rejecting duplicate or malformed targets. `*<>` shares the
output handle for errors without claiming a second owned handle. Operators are
recognized only at a whitespace-delimited boundary. This is currently a
candidate Amiga-style syntax subset, not yet verified against MorphOS 3.20.
`ShellScriptCompoundParser` also recognizes the whitespace-delimited
`&&`, `||`, and `|` operators outside quoted or star-escaped text and comments;
the operator is carried in a named enum field of `ShellScriptCompoundSplit`.
The runner executes `&&` by comparing the left command's result with `Failat`,
and currently executes a simple `||` pair sequentially through its inherited
output handle, even if the left command exceeds `Failat`. Deferred command
text, operator kind, and physical resume cursor live in the named
`ShellScriptDeferredCommandState` appended to frame version 5 (152 bytes), so
external children can finish before the chain resumes. This does not yet
provide the composable common stream required when concatenated output feeds a
later pipeline (as in the reference example), nor whole-expression redirection.
The runner still rejects `|` explicitly; pipeline scheduling and Queue-Handler
integration remain required work. Exact MorphOS 3.20 differential behavior
remains open. Compile-only regressions cover operator parsing, pipe rejection,
typed frame round-tripping, synchronous chaining, failure handling, and
asynchronous continuation.
The current operator baseline is the Amiga ROM Kernel Reference Manual, DOS
§15.1.2; MorphOS's official Shell command inventory does not establish these
operator semantics. MorphOS 3.20 differential verification remains a gate.
`ShellRedirectionTransaction` opens and closes only command-owned handles,
rolls back partial opens, and passes temporary streams through the existing
dispatcher and external-command boundary. The inherited frame streams remain
unchanged; exact filesystem-open and append semantics still need MorphOS
verification. `ShellScriptAliasWorkspace` now places
DOS-owned alias expansion before redirection and internal resolution, while
`ShellScriptLookupWorkspace` provides caller-owned path storage and
`ShellScriptLookupResult` carries named classification, search origin,
`FileProtection`, path, and path length through the external execution
boundary. `ShellScriptCommandInvocation` carries the decoded command name and
raw argument boundary as named fields. The DOS bridge resolves resident
entries, explicitly named paths, the current directory, and the CLI command
path in that order, using the typed process context and `PathLock` record
codec. Command lookup reads the DOS `FileInfoBlock` through its typed codec and
classifies files with the `FileProtection.Script` bit as scripts. S-protected
CLI-Path hits are routed through the ordinary `Execute` HUNK command with a
quoted resolved path and preserved raw argument tail; the script itself is
never passed to the HUNK loader. This routing is an implementation inference:
MorphOS documents the filename-only shortcut for S-protected files in its
search path, but not the Shell's internal dispatch mechanism. Explicit-path
and current-directory script behavior, argument edge cases, runtime traces,
and boot integration remain unverified. See the MorphOS
[Execute reference](https://library.morph.zone/Shell_Commands/Execute).

### SG03 — Implement basic state and output commands

- Implement `CD`, `Cls`, `Echo`, `Fault`, `Get`, `Getenv`, `Path`, `Prompt`,
  `Set`, `Setenv`, `Stack`, `Unalias`, `Unset`, `Unsetenv`, and `Why`.
- Preserve MorphOS templates, local-before-global lookup, path ordering,
  prompt formatting, environment persistence rules, escape processing, and
  return-level behavior.
- `Stack` updates only the current CLI's default child-command stack and never
  mutates an already executing stack.
- Variable, alias, and path changes must be failure-atomic under low memory.

Progress note: the basic state/output command wrappers now use exact
CopperStart `ReadArgs` templates, including DOS-owned no-argument listings for
`Set`, `SetEnv`, `UnSet`, and `UnSetEnv`. Enumeration and persistence remain
platform-owned; CopperOS keeps no managed variable map. `Ask` now returns
`WARN` for N/empty and success for Y, so the normal script `If WARN` path sees
the answer; it emits the supplied prompt without adding punctuation. The
MorphOS `Ask` prose and example disagree about the Y/N branch, so this follows
the prose and `If` threshold definition pending a MorphOS runtime trace.

### SG04 — Implement script control

- Implement `Ask`, `Else`, `EndIf`, `EndSkip`, `Failat`, `If`, `Lab`, `Quit`,
  and `Skip`.
- Maintain script control state in guest-resident frames owned by the executing
  CLI, including nested conditionals, current line, labels, skip targets,
  failure limit, last result, and quit result.
- Define deterministic behavior for missing labels, duplicate labels, malformed
  conditions, unmatched blocks, end-of-file while skipping, nested scripts,
  Ctrl-C, and child-command failure.
- Do not pre-load an unbounded script into memory; use bounded buffered input or
  an indexed representation with explicit allocation failure.

Progress note: `ShellScriptFrameCodec` now carries guest pointers for bounded
input metadata, nested `If`/`Skip` records, and a parent-linked label index.
`ShellScriptInputCodec` records one caller-owned buffer span at a time, while
`ShellScriptLabelTransitions` rejects duplicates/corrupt chains and resolves
nearest forward/backward targets. The `ShellScriptEngine.Step` boundary
consumes one bounded line, dispatches internal commands, suppresses ordinary
skipped lines, and delegates unknown lines to `IShellScriptPlatform`; it does
not preload scripts or schedule tasks. Nested control opening now validates
the existing parent chain before publishing a new control head and restores
the previous head if branch-state synchronization fails.
The frame is now 112 bytes, with a `ShellScriptSignalCodec` record and a
pending external-command continuation;
`ShellScriptEngine.Step` polls and acknowledges Ctrl-C/break, Ctrl-D, and task
termination before reading input, producing deterministic quit/end/inactive
state without a blocking wait. The current frame codec also carries the CLI's
failure threshold; `Step` synchronizes it from the named CLI record and stops
after synchronous or child return codes reach that threshold. Script teardown
resets the CLI to MorphOS's documented default of 10. See the progress ledger
for the current compile-only checkpoint; behavior still needs test and target
runtime validation.

The native DOS adapter now publishes a DOS-owned CLI-to-frame binding during
`ShellScriptEngine.Start`. Direct `Quit`, `EndCLI`, and `EndShell` requests and
previous-result/text/numeric/file-existence `If` evaluation update that frame
without managed state. Nested `Else`/`EndIf`/`EndSkip`, labels, and requester
handling now use DOS-owned control/label records; `Ask` is a bounded Y/N
requester backed by DOS console input.

### SG05 — Implement Shell and process lifecycle

- Implement `Alias`, `EndCLI`, `EndShell`, `NewCLI`, `NewShell`, and `Run`.
- Create child CLIs with documented inheritance of streams, directory, command
  path, variables, aliases, failure level, prompt, console, and stack size.
- Implement background execution through DOS processes and scheduler-visible
  completion rather than threads or async tasks.
- Define console ownership and close rules so child launch failure, Shell exit,
  detached execution, or process death cannot leak handles or close a parent's
  console.
- Handle Ctrl-C/Ctrl-D and task termination through Exec/DOS signals with
  deterministic cleanup.

Progress note: `Run` now has its exact MorphOS `ReadArgs` boundary and a
fixed-width `TryRunCommand` process-owner contract. Native file/resident child
creation uses typed prepare/publish/abort transactions: DOS owns copied text,
private streams and the live directory lock before the task becomes runnable.
`NewCLI`/`NewShell` share the `WINDOW,FROM` ReadArgs boundary and native child
entry for bounded scripts or console input. Boot execution remains unqualified.

The shared 52-byte continuation ABI is retained. A separate typed DOS completion
object, bound before publication, preserves the child's primary and secondary
results after its Process/CLI retire. Wait installation is no longer required
for recording exit; callbacks cannot overwrite the captured secondary error.
Only completed DOS resource retirement makes the outcome terminal. Parent
acknowledgement does not look up freed child state or close stale stream/lock
snapshots; owned continuation and exact wait storage are reclaimed through DOS.
Frame/parent teardown detaches live child references before reclaiming storage.

Normal-return directory ownership, wait races and deferred cleanup have focused
host coverage. Typed process-owned argument buffers now feed ordinary ReadArgs
without replacing Input, with independent startup strings, Shell LF normalization,
nested RunCommand masking and explicit close/retirement ownership. Native compiler
receipts are not native execution proof. Complete CLI inheritance, guest execution
and MorphOS buffer-interaction qualification, failed unpublished retirement recovery,
concurrent reset admission, full detach behavior, MorphOS differential qualification
and boot integration remain required. See the separate progress ledger for current
verification and limitations.

### SG06 — Implement resident command management

- Implement `Resident` listing, add, replace, remove, deferred loading, system
  entries, use counts, and documented `PURE`/force handling.
- Admit a normal resident command only when its command manifest and exact
  artifact hash have passed the external goal's purity qualification.
- Warn and explicitly mark an unqualified forced entry as unsafe. Never promote
  it to a verified Pure artifact.
- Prevent replacement or removal while a segment is in use and roll back
  failed loads or registry mutations without corrupting the resident list.
- Prove that two CLIs can run one verified resident segment concurrently with
  independent arguments, streams, directories, errors, and allocations.

Progress note: `ResidentCommand` now has the exact MorphOS `ReadArgs` result
boundary and delegates all registry and segment policy through the Shell
platform. Loading, use-count protection, verified Pure admission, deferred
segments, and concurrent resident execution are still pending.

`ShellResidentEntryCodec` and `ShellResidentPolicy` now provide the first
guest-resident registry boundary: verified-PURE and forced-unsafe admission
are distinct, deferred entries have no loaded segment, use counts are bounded,
and removal cannot be marked while an entry is in use. DOS still owns the list
head, HUNK allocation, purity manifest/hash qualification, and concurrent
process launch.

CopperStart now implements `DosResidentRegistryCore` on the existing DOS object
allocation chain. It provides case-insensitive lookup, use-count acquisition
and release, safe removal/replacement, deferred entries, and a `Manage` path
that loads new HUNK images through `LoadSeg` before publishing them and unloads
failed or removed images. The new `DosShellNativeBridge` exposes that registry,
the bounded startup-script cursor, explicit-span `ReadArgs`, and redirection
I/O through one fixed-width native handoff. The full Shell-side
`IShellPlatform` implementation and verified manifest/hash qualification for
normal PURE admission remain pending. `DosShellStateCore` now gives DOS
ownership to variables, aliases, and command paths, and
`DosShellPlatform<TDosPlatform>` forwards those plus CLI fields, redirection,
ReadArgs, resident management, and lookup. File-backed `Run` reaches the
existing Exec task creator when a live `ExecBase` is supplied and terminal
continuation teardown releases the corresponding DOS/Exec resources. Resident
commands bind their acquired use count to the child Process and release it on
task death. Script-image handoff, scheduler wake, and shell control signals
now cross explicit DOS/Exec boundaries rather than host callbacks.

### SG07 — Build and integrate the native Shell

- Add the Shell production, test, and native-root projects to `CopperOS.sln`.
- Build an exception-free MC68000 Shell segment and stage it as
  `filesystem/SYS/L/Shell-Seg`.
- Compile MC68020 and MC68040 variants without changing semantics or requiring
  an FPU.
- Register the Shell as the system CLI/Shell segment through CopperStart DOS
  and exercise interactive, startup-script, child-Shell, resident, and disk
  command execution.
- Ensure an external Kickstart/DOS profile retains ownership unless a CopperOS
  boot profile explicitly selects the CopperOS Shell.

Progress note: `CopperOS.Shell.Dos` and `CopperOS.Shell.Dos.NativeRoot` are now
in the solution. The adapter is standalone and compiled against the local
CopperSharp ABI so it cannot mix the host Shell's pinned SDK package with
CopperStart DOS. File-backed `Run` reaches the existing Exec task creator when
a live `ExecBase` is supplied, and terminal continuation teardown releases its
DOS/Exec resources. Resident use is bound to the child Process and released on
task death. Child Process termination now completes the matching foreground
continuation before DOS release and wakes its prepared parent wait. The
adapter consumes MorphOS `SIGBREAKF_CTRL_C`/`SIGBREAKF_CTRL_D` bits and maps
them to Shell break/Ctrl-C/Ctrl-D events without clearing unrelated signals.
`Execute` now runs bounded internal script lines through the
guest-resident frame engine and restores nested outer frames on return.
CopperStart now also has a DOS-owned fixed-width foreground-wait record that
retains the parent CLI, child continuation, frame, and resume cursor on the
existing Shell allocation chain. It is observable and reclaimable without
spinning. The persistent `Execute` runner now consumes it: pending external
lines retain the complete frame/workspace in DOS-owned storage and resume by
bounded continuation polls. The `copperos.shell.execute-park` entrypoint
bridges a prepared wait directly to the DOS record and its fixed-width native
root is qualified. Begin/Poll now use a four-byte DOS-state context, with
ExecBase derived from the owning native DOS envelope (and a legacy bare-state
fallback). The aggregate DOS item and resident
record builders have scalar native codecs, and the full Begin/Poll/Park export
set now compiles for MC68000, MC68020, and MC68040 with zero managed allocation
sites. Native wake delivery and process/signal integration are now wired;
the native root also compiles the file-backed Run handoff, including scalar
Process/CLI publication and segment-list ownership. Resident `Run` uses the
same fixed-width handoff with scalar registry lookup/acquire/release and
Process use-count binding. Its provider boundary now
routes scalar file/lock operations through DOS vectors; relative-lock and
packet services remain fail-closed. Bounded `CON:` WINDOW startup opens a
temporary native console and transfers duplicated child handles. The native
Execute path now also
launches explicit-file external script images through the scalar Process/CLI
continuation handoff for file and resident images, including resident
use-count binding. Native NewCLI/NewShell now have a fixed-width child-shell
startup handoff for bounded `FROM` scripts or inherited-console input: the child
owns its CLI, duplicates standard DOS handles/locks, converts the CLI BSTR to a
C string when present, and drives the DOS-owned runner with prepared waits.
Child completion/resource edge cases,
final runtime staging, and end-to-end boot qualification remain required
before shipping `Shell-Seg`;
requester completion remains fail-closed.

### SG08 — Final qualification

- Run parser and expansion fuzzing, malformed-state tests, low-memory and
  callback-failure injection, nested scripts, redirection, signals, concurrent
  Shells, resident replacement, task death, and resource-accounting suites.
- Execute the same deterministic Shell traces through pure semantics and
  generated MC68000 code, comparing return values, streams, CLI state,
  allocations, and resident lists.
- Record SDK/documentation conformance separately from MorphOS differential
  completion. Final MorphOS compatibility remains gated on later black-box
  traces from a licensed MorphOS 3.20 installation.

## Public Interfaces and Artifacts

- `CopperSharp.Sdk.Amiga` gains only validated missing CLI, process, resident,
  and Shell-facing ABI declarations and codecs.
- CopperStart DOS gains portable semantic behavior where the Shell exposes a
  missing library contract; it does not gain internal command implementations.
- CopperOS owns the Shell parser, execution engine, internal commands, native
  entry roots, and `Shell-Seg` artifact.
- The external commands goal owns the machine-readable command manifest and
  verified Pure status consumed by `Resident`.

## Test and Acceptance Contract

- All 32 inventoried internal commands have implemented, obsolete, or
  explicitly unsupported dispositions with zero unexplained gaps.
- Production native reports show zero managed allocations, exception regions,
  exception/runtime metadata, framework features, host imports, and unresolved
  relocations.
- Parser tests cover empty lines, quoting, escapes, expansions, aliases,
  recursion, redirection, patterns, comments, long inputs, malformed syntax,
  and allocation failure.
- Script tests cover nested `If`, `Skip`/`Lab`, `Failat`, `Quit`, missing
  terminators, child failures, return levels, and Ctrl-C.
- Shell lifecycle tests cover inheritance, stack selection, stream ownership,
  background processes, child launch failure, and cleanup after task death.
- Resident tests cover qualified Pure commands, unsafe forced commands,
  replacement/removal while active, deferred loading, repeated execution, and
  concurrent execution from one segment.
- No internal command is staged in `filesystem/SYS/C`.

## Assumptions

- MC68000 is the behavioral baseline; MC68020/MC68040 are compile-qualified.
- Host tests and build tools may use .NET, but production and native-reachable
  source remains inside the constrained freestanding subset.
- CopperOS, CopperStart, and CopperSharp68k may be updated while preserving
  unrelated work and existing external-ROM ownership.
- Pixel-level console presentation and terminal emulation belong to the console
  handler; the Shell owns only text and control-sequence output behavior.
