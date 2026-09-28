# MorphOS Shell Command Completion Ledger

Status snapshot: 2026-09-28. This is a live inventory of ownership and
remaining proof, not a compatibility sign-off. A source file, passing build, or
static compiler receipt alone does not mark a command complete.

## Scope and count

The goal's explicit MorphOS internal-command list and
`ShellInternalCommandResolver` cover 31 callable command identities. The enum
also has `Unknown = 0`, making 32 enum members; `Unknown` is a sentinel, not a
command. `Execute` has a separate wrapper in `src/Commands/ExecuteCommand.cs`
and is handled by the Shell/DOS script engine; it is not one of the 31 internal
identities or staged in `SYS:C` by this goal.

The MorphOS Shell command inventory is the command-set authority. Behavioral
completion still requires MorphOS 3.20 evidence for syntax, return levels,
`IoErr`, state ownership, and lifecycle. See the [main goal](MORPHOS_SHELL_INTERNAL_COMMANDS_GOAL.md)
and [progress log](MORPHOS_SHELL_INTERNAL_COMMANDS_PROGRESS.md).

## Internal command inventory

“Source present” means the command has a Shell-owned implementation and
dispatcher identity. It does not mean the documented behavior is complete.
Every row remains in progress until its command contract and relevant
integration gates are evidenced.

| Command | Implementation owner | Current disposition |
| --- | --- | --- |
| `Alias` | `src/System/Shell/AliasCommand.cs` | Source present; MorphOS behavior and failure-atomic ownership not fully qualified. |
| `Ask` | `src/System/Shell/AskCommand.cs` | Source present; MorphOS prose/example disagree on Y/N semantics; runtime trace pending. |
| `CD` | `src/System/Shell/CdCommand.cs` | Source present; path, lock ownership, and MorphOS error behavior remain to qualify. |
| `Cls` | `src/System/Shell/ClsCommand.cs` | Source present; reset-switch and console behavior remain to qualify. |
| `Echo` | `src/System/Shell/EchoCommand.cs` | Source present; documented ReadArgs template corrected; full MorphOS output and escape behavior remain to qualify. |
| `Else` | `src/System/Shell/ElseCommand.cs` | Source present; nested and malformed-control behavior needs full script qualification. |
| `EndCLI` | `src/System/Shell/EndCliCommand.cs` | Source present; process/window lifecycle and MorphOS return behavior remain to qualify. |
| `EndIf` | `src/System/Shell/EndIfCommand.cs` | Source present; nested and unmatched-control behavior needs full script qualification. |
| `EndShell` | `src/System/Shell/EndShellCommand.cs` | Source present; child-shell and process teardown remain to qualify. |
| `EndSkip` | `src/System/Shell/EndSkipCommand.cs` | Source present; skip-state edge cases need full script qualification. |
| `Failat` | `src/System/Shell/FailatCommand.cs` | Source present; optional `RCLIM/N` and positive-value validation corrected; omitted-argument display is only an AmigaOS baseline pending MorphOS evidence. |
| `Fault` | `src/System/Shell/FaultCommand.cs` | Source present; DOS-owned translation and formatting are not yet MorphOS-differentially qualified. |
| `Get` | `src/System/Shell/GetCommand.cs` | Source present; local-variable semantics and output/error behavior remain to qualify. |
| `Getenv` | `src/System/Shell/GetenvCommand.cs` | Source present; global-variable semantics and output/error behavior remain to qualify. |
| `If` | `src/System/Shell/IfCommand.cs` | Source present; `EXISTS` uses DOS locks for files/directories and `NOREQ` temporarily suppresses caller-process DOS requesters around the lock; file-system-internal requester behavior, MorphOS runtime qualification, and the `VAL` wording discrepancy remain open. |
| `Lab` | `src/System/Shell/LabCommand.cs` | Source present; duplicate labels and direction/selection behavior need full script qualification. |
| `NewCLI` | `src/System/Shell/NewCliCommand.cs` | Source present; child startup, stream inheritance, and teardown remain to qualify. |
| `NewShell` | `src/System/Shell/NewShellCommand.cs` | Source present; child startup, stream inheritance, and teardown remain to qualify. |
| `Path` | `src/System/Shell/PathCommand.cs` | `ADD`/`RESET`/`REMOVE` now stage and replace the live DOS `PathLock` chain; entries are verified as directories through the typed `FileInfoBlock` codec; `SHOW` reads the live chain, and `QUIET` scopes requester suppression to caller-Process lock resolution. File-system-internal requester and MorphOS missing-volume/runtime behavior remain open. |
| `Prompt` | `src/System/Shell/PromptCommand.cs` | Source present; documented substitutions and command substitution need MorphOS differential evidence. |
| `Quit` | `src/System/Shell/QuitCommand.cs` | Source present; nested script, return-code, and continuation behavior remain to qualify. |
| `Resident` | `src/System/Shell/ResidentCommand.cs` | DOS-owned admission, PURE/DEFER first-acquire loading, ALIAS resolution/target-removal protection, and immutable SYSTEM entries are implemented using typed records/codecs; MorphOS runtime/error-code traces, true cross-task exclusion, and full option/order behavior remain open. |
| `Run` | `src/System/Shell/RunCommand.cs` | Source present; process creation, detach, streams, and asynchronous completion remain to qualify. |
| `Set` | `src/System/Shell/SetCommand.cs` | Source present; local-variable listing, replacement, and failure atomicity remain to qualify. |
| `Setenv` | `src/System/Shell/SetenvCommand.cs` | Source present; global persistence, replacement, and failure atomicity remain to qualify. |
| `Skip` | `src/System/Shell/SkipCommand.cs` | Source present; labels, nested control, and end-of-file behavior need full script qualification. |
| `Stack` | `src/System/Shell/StackCommand.cs` | Source present; MorphOS `SIZE/N` template corrected; stack inheritance and boundary behavior remain to qualify. |
| `Unalias` | `src/System/Shell/UnaliasCommand.cs` | Source present; per-CLI listing/removal and failure atomicity remain to qualify. |
| `Unset` | `src/System/Shell/UnsetCommand.cs` | Source present; local-variable deletion/listing and failure atomicity remain to qualify. |
| `Unsetenv` | `src/System/Shell/UnsetenvCommand.cs` | Source present; global-variable deletion/listing and failure atomicity remain to qualify. |
| `Why` | `src/System/Shell/WhyCommand.cs` | Source present; public return-code/secondary-error presentation remains to qualify. |

## Cross-command gates

- All command options must continue through CopperStart DOS `ReadArgs`; the
  Shell must not acquire a parallel option parser. Result slots and retained
  state use named structs/codecs, with guest ABI offsets confined to their
  owning codecs.
- `ReadArgs` implements the modifiers used by the current templates and now
  accepts multiple equivalent keyword names in one template entry (for
  example `Fuh=Bar=Chicken/S`). Detailed MorphOS 3.20 parsing compatibility is
  not established by AmigaDOS 40.3 tests or SDK prototypes alone.
- Shell syntax and lifecycle are incomplete: single-pipe `|` is parsed into a
  typed plan but execution remains rejected. CopperStart DOS now has a
  guest-resident, struct-backed single-reader/single-writer channel and queue
  buffer primitive plus a bounded guest-resident channel registry. A typed
  READ/WRITE packet service now uses a typed request struct and guest-resident
  pending records attached to channel reader/writer slots, preserving partial
  progress across `WouldBlock`. `FindInput`/`FindOutput` now resolve typed
  BSTR names against that registry, acquire the endpoint, and return
  `DOSTRUE`/DOS error packet results; unsupported `FindUpdate` is replied to
  with `ActionNotKnown` until the channel model can represent duplex handles.
  A one-step dispatcher now polls and handles open, READ, WRITE, END, and
  unknown packets; it retains blocked I/O in guest records and pumps one
  bounded pass after packet handling or on idle. `WaitAndProcessNext` now polls,
  parks on the current task's typed signalling message port through Exec
  `WaitPort` when idle, then polls again. The DOS-owned
  `DosQueueHandlerTaskCore` now supplies an owning repeated loop, fixed-width
  task state, and an empty-registry stop-and-signal contract; the Shell exports
  its native task entry, which obtains the control record from typed
  `Task.UserData`. `DosQueueHandlerProcessCore` now allocates/initializes this
  state and the registry, publishes the entrypoint through DOS process
  creation, and gates record/registry retirement on terminal state and an empty
  channel list. `DosQueueHandlerChannelOwnerCore` can allocate/register
  channels before launch and the bounded handler pump now discards and reclaims
  fully closed channels. However, no Shell callsite yet creates channels from
  pipeline plans, wires child standard streams to them, routes `PIPE:` opens to
  the handler, waits for process return before reclamation, or schedules the
  pipeline. Concurrent Shell scheduling,
  composable output concatenation, and full-expression redirection also remain
  open. If a transfer
  accepts bytes before pending-state allocation fails, the dispatcher reports
  that partial count rather than allowing a caller to duplicate accepted data.
  Script/child lifecycle, boot handoff, resident/PURE admission, and resource
  accounting remain open.
- The MorphOS `If` page describes `VAL` as text identity, while the inherited
  AmigaDOS reference defines numeric comparison. Keep the current behavior as
  an explicit MorphOS differential question rather than silently treating the
  two sources as equivalent.
- `If NOREQ` and `Path QUIET` use stack-local save/restore of the caller
  Process's `WindowPointer` around their DOS lock operations. This cannot
  promise suppression of requesters raised inside a file-system task. MorphOS
  runtime behavior for both switches remains unqualified; `Path`'s missing or
  unmounted-volume behavior still needs differential evidence.
- Release compilation and MC68000/020/040 static qualification are useful
  evidence only. Recent command changes have compile-only/static evidence;
  no tests were run for those changes, and MorphOS guest execution and a
  shipping boot remain unqualified. `guestExecution` and
  `shippingAdmission` remain false.

References: [MorphOS Shell Commands](https://library.morph.zone/Shell_Commands),
[MorphOS Resident](https://library.morph.zone/Shell_Commands/Resident),
[MorphOS If](https://library.morph.zone/Shell_Commands/If), and the
[AmigaDOS command reference](https://wiki.amigaos.net/wiki/AmigaOS_Manual%3A_AmigaDOS_Command_Reference).
