# Native Shell public-file probe

Current build and runtime status is maintained in
[Shared Shell acceptance](../../docs/SharedShellAcceptance.md). The evidence below
also retains historical builds and superseded investigations. V22 uses public
DOS polling with bounded delay backoff; the full owner probe now passes all
`0x3fff` stages on both systems after heap exhaustion with a lower-priority child.
The final v22 audit accepts all 50 planned runtime receipts and the separate
unmodified proof boot, with current source/artifact hashes, actual guest files
and process retirement. Earlier incomplete-status statements below are historical.
`CopperOSContinuationNoOomControl=true` instead expects `0x3bff` and cannot
qualify allocation failure. `CopperOSDelayHeapPressureProbe=true` adds public
heap-pressure observations to the independent timer probe; its aggregate pass
deliberately does not require child progress during the one-tick case.

The shared native platform now retains no CopperStart DOS-state pointer. Its
constructor and exported Begin/Poll/Park context arguments are ignored; an
initialized CLI uses Shell-owned process, runner, frame and continuation
records. The generic legacy host adapter remains separate. Native cleanup
does not reclaim allocations belonging to an opaque caller-supplied context.

`artifacts/shell-public-only-proof-v3/System.adf` (SHA-256
`eace6bcbc88551e6a0170ace494c880cff300b13fc73acfe3733c63a2d47c93d`)
contains the rebuilt 186772-byte Shell. Its symbol table contains no
DosCore, DosShellStateCore, DosShellNativeBridge, DosShellNativeContextCore,
DosShellNativeLaunchCore, queue-handler-task or private process-return
routines. The proof producer no longer roots those two legacy task exports.
Strict native runtime checks pass. The command host suite passes 1015 tests
and the path/runner ownership suite passes six tests with this refactor.

The unmodified image boots on original Kickstart 3.1 and loads staged
`SYS:Libs/mathtrans.library` at V40 (`RESULT 0`) in
`artifacts/shell-public-only-exact-ks-v3`. Its CopperStart packet-started
synchronous, asynchronous and supplied-input System tests pass in
`artifacts/shell-public-only-system-copper-v3`, including process retirement
and Shell exit. These results do not yet qualify the complete Shell.

The expanded runner probe creates a separate DOS Process for cases requiring
fresh Shell state, preserving the foreground caller's existing state. It
borrows T: or temporarily assigns the fixture directory for .KEY, then restores
that assignment. It also verifies that an opaque prior pr_ShellPrivate value
matching former private Execute flags is restored without consuming an extra
input command. The diagnostic record is `T/runner-state.bin`: result, private
restored, command-file restored, output present, unexpected tail present,
next input byte, two reserved words, and stage mask (expected `0x1f`). It
passes on Kickstart in `artifacts/shell-public-only-runner-ks-v5`.
The same HUNK passes on CopperStart in
`artifacts/shell-public-only-runner-copper-v5`; both diagnostic records contain
the full `0x1f` mask and the unread tail starts with `E`.
Earlier v3/v4 runner results used an invalid fixture that tried to install
fresh state in the already initialized foreground caller; their failures are
retained, not counted as passes. The old owner fixture likewise expected
native cleanup to free a supplied legacy context's allocations. The revised
fixture requires that chain to remain unchanged, then explicitly releases its
own allocations through the legacy fixture owner.
That revised owner probe passes on Kickstart in
`artifacts/shell-public-only-owner-ks-v3` and on CopperStart in
`artifacts/shell-public-only-owner-copper-v4` (mask `0x3fff`). Canary checks
pass in `artifacts/shell-public-only-canary-ks-v3` and
`artifacts/shell-public-only-canary-copper-v3`. Launch options pass in
`artifacts/shell-public-only-options-ks-v4` and
`artifacts/shell-public-only-options-copper-v3` (mask `0x4fffffff`, result and
error zero). The 60-second Kickstart options attempt completed those functional
checks but lacked its final Shell-exit marker; the 120-second attempt passed
the complete fixture without changing its HUNK.

The same final proof passes the CopperStart broad, path-isolation and
control-flow cases in `artifacts/shell-public-only-*-copper-v3`, and the
nonresident broad case in `artifacts/shell-public-only-nonresident-copper-v3`.
These traces verify child retirement, cleared runner/binding state and Shell
exit. The build/source/symbol audit is
`artifacts/shell-public-only-proof-v3/public-interface-audit.json`; its recorded
producer source hashes identify that historical build. Both CopperStart Shell
entry projects also build with the new ownership contract; the older A500
startup caller explicitly installs and retires Shell-owned process state.

The later `artifacts/shell-public-only-proof-v4/System.adf` has SHA-256
`dcaa7c1e74638ec1fb0ab9c7a98e71c5ffe6cc3b548b71f0b437f91e76e04fd5`.
Its Shell is byte-identical to v3; Execute adds checked caller-tail writes,
rollback of seekable caller input, checked close/reopen and an owned buffered
replacement reader. Its source/artifact/symbol audit is
`artifacts/shell-public-only-proof-v4/public-interface-audit.json`.
The unmodified image boots on original Kickstart 3.1 in
`artifacts/shell-public-only-exact-ks-v4`, executes its command and loads
`SYS:Libs/mathtrans.library` at V40 with `RESULT 0`.
The 128-comment-line control-flow script crosses 8 KiB before its assertions
and passes with Shell exit on Kickstart in
`artifacts/shell-execute-large-control-ks-v4`.
It also passes on CopperStart in
`artifacts/shell-execute-large-control-copper-v4`, with cleared runner/binding
state and no remaining child or background Shells.

`--execute-disk-full source|tail` in the Kickstart qualifier and
`--case execute-full --execute-failure-stage source|tail` in the CopperStart
qualifier construct genuinely near-full FFS media. The test command calls the
packaged C:Execute through DOS RunCommand with requesters temporarily disabled
through the public process record, then restores the window pointer. No DOS
vector or filesystem operation is replaced. Sentinels require the result
capture and caller's later commands to run; success also requires removal of
every Execute work file and eventual Shell exit. The source-copy failure passes
on Kickstart in `artifacts/shell-execute-full-source-ks-v2`, returning 10/221.
The caller-tail failure also passes in
`artifacts/shell-execute-full-tail-ks-v2`, including the caller's late commands
after more than 16 KiB of comments, result 10/221 and no temporary files.
The same caller-tail fixture rejects the earlier v3 Execute in
`artifacts/shell-execute-full-tail-ks-baseline-v1`: that binary reports 0/0
under the allocation pressure where v4 reports disk-full. Its caller and exit
markers are present, so this control distinguishes error handling from a
fixture that merely times out before running.
The first fixture omitted requester suppression and did not complete; that
attempt is retained separately and is not a pass. CopperStart source-copy
failure passes in `artifacts/shell-execute-full-source-copper-v2`, with result
10/221, caller continuation, no work files, cleared runner/binding state and
no remaining children. The caller-tail replay also passes in
`artifacts/shell-execute-full-tail-copper-v3`, with 10/221, both late caller
markers, no work files and complete process retirement.
Its fixture puts the console completion marker after the caller's late
commands, so the console observer's exit allowance cannot start prematurely.
The earlier CopperStart tail v2 attempt used an early marker and was rejected
by the host observer's `Shell prompt missing before endcli` guard while the
caller was still consuming its remaining lines. No EndCLI was injected by
that attempt; it supplies no successful tail-recovery or Shell-exit evidence.

The final-image command matrix passes Resident admission/removal/listing,
V40 rejection of resident extensions, NewShell, NewCLI, inherited prompts,
65 Path arguments, nested FailAt and append on Kickstart in
`artifacts/shell-public-only-final-*-ks-v4`.
CopperStart's corresponding SYSTEM-resident and deferred-resident cases pass
in `artifacts/shell-public-only-final-resident-system-short-copper-v4` and
`artifacts/shell-public-only-final-resident-defer-copper-v4`.
The added `--case cd` replay passes in
`artifacts/shell-public-only-final-cd-copper-v4`: canonical names, `/` parent
syntax, missing-directory result 10/205, unchanged directory after failure,
Shell exit and task retirement. The `--case path` replay covers ADD, REMOVE,
RESET, listing, search through a CLI path lock, removal from command lookup
and the final C: fallback; it passes on both systems in
`artifacts/shell-public-only-final-path-copper-v4` and
`artifacts/shell-public-only-final-path-ks-v4`. The v4 CD replay's 10/205
expectation was subsequently contradicted by an original-ROM Shell capture:
the correct V40 result is 20/205. The shared command and current qualifiers now
require 20/205; the old v4 CD result is historical evidence only.

The diagnostics audit found that the shared runner had lost the old adapter's
failure messages. `observe_diagnostics31.py --original --plain` uses the ROM
Shell, with L:Shell-Seg removed and no BootShell registration. The same packaged
Execute is used with both Shells. Original captures are
`artifacts/shell-public-only-diagnostics-original-plain-v2`: CD failure 20/205,
Stack failure 10/115, DOS catalogue text without a header, and a missing-command
System call's name-prefixed diagnostic followed by its return-code line.
The original V40 Shell does not recognize the `*>` extension; that syntax is
verified separately for this Shell rather than claimed to match the ROM parser.

The shared engine now owns those diagnostics, using public DOS Fault and Flush
services and the selected command streams before cleanup. Lookup failure keeps
V40's unopened ordinary stdout target; an explicit error target or *<> still
works. Diagnostic I/O cannot replace the captured command outcome. The v6
plain replay found reordered buffered fragments; the v7 writer flushes at
diagnostic boundaries, and its plain replay matches the original captures.
The v5 attempt failed the strict native gate because variable division emitted
two fatal trap sites. The bounded decimal formatter preserves that gate;
v6 and v7 have zero such sites.

`artifacts/shell-public-diagnostics-proof-v7/System.adf` has SHA-256
`20a000b7098680cec62ad0f899d649052e29bb686879a2bb84fccb895378110c`.
Its Kickstart plain and explicit-error-stream checks pass in
`artifacts/shell-public-diagnostics-plain-ks-v7b` and
`artifacts/shell-public-diagnostics-streams-ks-v7b`.
The earlier v6 explicit-error-stream case also passes on CopperStart in
`artifacts/shell-public-diagnostics-shared-copper-v6`; the v7 CopperStart
diagnostic and corrected 20/205 CD replays also pass in
`artifacts/shell-public-diagnostics-diagnostics-copper-v7` and
`artifacts/shell-public-diagnostics-cd-copper-v7`.

The recovery audit also found that a malformed console line ended the shared
Shell. The original ROM continued after both malformed redirection and quotes
in `artifacts/shell-public-only-recovery-original-v3`; the old shared image
failed the same fixture. The v7 common runner recovered only from a consumed
interactive line with a published next cursor, retaining terminal script
errors. The v7 recovery cases pass on Kickstart and CopperStart in
`artifacts/shell-public-diagnostics-recovery-ks-v7b` and
`artifacts/shell-public-diagnostics-recovery-copper-v7`. Both following commands
run and the Shell exits; the CopperStart trace contains both syntax messages
and no remaining processes. Syntax diagnostic wording has not been compared
with the original console output.

The updated command suite passes 1026 tests. The combined ROM rollback,
argument/completion and path ownership suite passes 65 tests in
`../CopperStart/artifacts/shell-diagnostics-regression-rom-host-v2.log`;
the separate non-ROM combination passes 64. Image/probe host checks pass 31.

The later script-recovery comparison found another ordinary-error boundary:
V40 continues after malformed command syntax inside a script, but a command
reaching FailAt stops that script and returns to the console. The original
capture `artifacts/shell-public-script-recovery-original-v2` also verifies that
IF ERROR sees the failed script's return code. CommandSyntaxError now separates
consumed command syntax from fatal directive errors such as `.DEF =value`;
ordinary stopped sequences retain their DOS result rather than being treated
as failed runner initialization. A resumed console frame takes its previous
result from the public CLI instead of resetting it to zero.

The current proof is `artifacts/shell-public-diagnostics-proof-v10/System.adf`,
SHA-256 `04f986b28f8cb5e4934184b62d7d6c4b99082676a96e965f23ac8cd18cab4216`.
Script recovery passes on both systems in
`artifacts/shell-public-diagnostics-script-recovery-ks-v10` and
`artifacts/shell-public-diagnostics-script-recovery-copper-v10`: both console
continuations, syntax-tail execution, no execution after the FailAt failure,
IF ERROR, and Shell exit. v9 reached the console but failed only IF ERROR;
that failed result is retained. The current command suite passes 1034 tests
in `artifacts/shell-diagnostics-full-host-v11.log`; the combined ownership and
rollback suite still passes 65 in
`../CopperStart/artifacts/shell-diagnostics-regression-rom-host-v3.log`.
The remaining v10 native matrix and complete-Shell acceptance audit remain open.
The unmodified v10 ADF separately boots on the original Kickstart ROM in
`artifacts/shell-public-diagnostics-exact-ks-v10`, executes the Shell command,
and loads staged `SYS:Libs/mathtrans.library` at V40 with `RESULT 0`.

`PublicFileNativeProbe::Main` exercises the production native Shell platform's
public DOS file boundary. It creates and appends a file, reads CRLF and LF
lines through `TryReadScriptLine`, checks the reported offsets and EOF, and
deletes the file. It also exercises public DOS argument parsing, quoted first
items, numeric and switch options, multi-value arguments, reused result slots,
missing arguments and error preservation. On a writable `DH0:` helper volume it creates `runner-ok`
or `runner-fail`. It does not exercise the complete Shell loop.

The same probe checks the current Shell number and command diagnostics against the live CLI and verifies
Ctrl-C/Ctrl-D consumption without clearing an unrelated pending signal. It
tests Shell-owned IF and label records with no CopperStart DOS state, including
`IF EXISTS` for a file, directory and missing path with requester restoration.
The probe verifies production frame unbinding and Shell-owned frame cleanup
without a CopperStart DOS-state pointer.

It also executes a tiny disk HUNK through the production foreground runner with
no CopperStart state. The fixture checks the command-tail registers; the probe
checks a mismatching tail, a missing executable, and restoration of the CLI
name, module, program directory, and selected streams. It registers and runs
the fixture as a resident segment and checks use counts, busy removal,
permanent entries and disabled entries. On systems exposing the V50 tag-list
vectors it also checks deferred loading. Lookup is checked for a resident,
an explicit file, the current directory, a CLI-owned path lock, a protected
script and a missing file. The probe separately exercises the Shell-owned
command-text reader and its runner cleanup, plus clearing the live CLI's
command-file BSTR. It checks local variable and alias mutation using public
DOS vectors, including binary values, truncation, empty values and deletion,
alias expansion and argument substitution, named alias queries, and
Kickstart-style alias and local-variable listings;
it also checks `Why` output against the public DOS fault catalogue and checks
its resulting `IoErr`;
it does not by itself exercise a
complete `System()` child. Generate the fixture before running the probe:

```powershell
python tests/Shell.Dos.NativeRoot/make_foreground_fixture.py <helper>/fixture
```

Build and compile it with the local CopperStart and CopperSharp checkouts. Use
the same artifacts directory for the probe and compiler so the resolver sees
matching compiler and SDK assemblies:

```powershell
dotnet build tests/Shell.Dos.NativeRoot/CopperOS.Shell.Dos.NativeRoot.csproj -c Release --artifacts-path artifacts/shell-native-platform-probe
dotnet build ../CopperSharp68k/Compiler.Cli/CopperSharp.Compiler.Cli.csproj -c Release --artifacts-path artifacts/shell-native-platform-probe -p:SkipCopperScreenHeadlessProjectReference=true
$p = 'artifacts/shell-native-platform-probe/bin/CopperOS.Shell.Dos.NativeRoot/release'
$compiler = 'artifacts/shell-native-platform-probe/bin/CopperSharp.Compiler.Cli/release/CopperSharp.Compiler.Cli.dll'
dotnet $compiler "$p/CopperOS.Shell.Dos.NativeRoot.dll" --entry 'CopperOS.Shell.Dos.NativeRoot.PublicFileNativeProbe::Main' --platform amiga --cpu 68000 --clr always --exceptions yolo --fpu disabled --format hunk --symbols off --runtime freestanding --memory none --peephole fixed-point --code-size-optimizations off --managed-assembly "$p/CopperSharp.Sdk.Amiga.dll" --managed-assembly "$p/CopperSharp.Sdk.Amiga.Support.dll" --managed-assembly "$p/CopperSharp.Compiler.dll" --managed-assembly "$p/CopperStart.Exec.dll" --managed-assembly "$p/CopperStart.Dos.dll" --include-export 'copperos.shell.public-file-probe' --output artifacts/shell-native-platform-probe/runner --compatibility-report artifacts/shell-native-platform-probe/runner.compatibility.json
```

The default `DH0:` variant can also run on a disposable Kickstart proof fixture:

```powershell
python tests/Shell.Dos.NativeRoot/make_foreground_fixture.py artifacts/shell-native-platform-probe/fixture
python tools/ImageBuilder/qualify_shared_shell31.py --image <System.adf> --rom <Kickstart-3.1.rom> --winuae <winuae64.exe> --output-directory <fresh-results-directory> --runner-hunk artifacts/shell-native-platform-probe/runner --runner-fixture artifacts/shell-native-platform-probe/fixture
```

The harness stages the fixture at `DH0:fixture` and records its hash alongside
the probe hash. It checks the result marker and Shell exit on a modified copy;
it does not qualify the complete Shell.
The focused probe passed with the rebuilt proof image on the Kickstart 3.1 ROM
in WinUAE, after the foreground runner stopped accepting the unused private
DOS-state pointer. The unmodified proof image also booted on the same ROM in
the lightweight emulator and opened the staged `mathtrans.library` at V40.
The corresponding CopperStart control-flow replay completed its script and
retired the Shell with no child processes remaining on that proof image.
After the memory-wrapper change, the next proof also passed the Kickstart
control-flow replay in WinUAE. Its unmodified ADF separately booted with the
Kickstart 3.1 ROM in the lightweight emulator and opened the staged V40 math
library successfully. A CopperStart fixture made from that proof ran the disk
foreground command and verified its output and Shell exit.

`PublicGlobalVariableNativeProbe::Main` is a separate probe for a disposable
boot. It installs `ENV:` and `ENVARC:` assigns backed by pre-created `DH0:env`
and `DH0:envarc` directories, then checks binary and empty global values,
truncation, SAVE files and deletion through the production Shell's public DOS
vectors. Compile its entry and export `copperos.shell.public-global-variable-probe`.
With `CopperOSProbeSys=true` the directories are `SYS:T/env` and `SYS:T/envarc`
and the marker is in `SYS:T`. The probe replaces and removes these assigns.

`PublicGlobalListingNativeProbe::Main` checks public `LIST` lookup and
execution for success, warning, failure, and a missing command, including the
original V40 argument tail, output, return level, and secondary error. It also
dispatches both `SetEnv` and `UnsetEnv` through the shared script engine.
Create `list-ok`, `list-warn`, `list-fail`, and `list-missing` directories on
the disposable helper, and generate `LIST` in the first three using
`make_list_fixture.py --result 0`, `--result 5`, and `--result 10`
respectively. With `CopperOSProbeSys=true`, use `SYS:T` for those directories
and generate the fixtures with `--argument-path SYS:T/listargs`. Compile the
entry with export `copperos.shell.public-global-listing-probe` and the same
Shell/process-return exports as the full runner probe below.

Use a fresh writable helper directory for each guest run. The helper's marker
records a focused platform test and is not evidence that the Shell itself
boots or implements all commands on the system under test.

For a shorter diagnostic-only run, compile `PublicFileNativeProbe::WhyMain`
and export `copperos.shell.public-why-probe`; it uses the same result marker.

`PublicPromptNativeProbe::Main` checks setting and resetting the live CLI prompt,
bounded input spans, prompt and directory BSTR reading, `%N`/`%S`/`%R`
rendering, literal spans, truncation and malformed backtick rejection without a
CopperStart DOS-state pointer. Compile its entry
and export `copperos.shell.public-prompt-probe`; it restores the borrowed CLI
fields and writes the same result marker.

`PublicFaultNativeProbe::Main` checks `Fault` output for multiple codes,
including zero and the minimum signed integer, as well as invalid-list error
handling. Compile its entry and export `copperos.shell.public-fault-probe`;
it uses the same result marker.

`PublicProcessStateNativeProbe::Main` installs the Shell-owned process state,
toggles a built-in across nested frame bindings, checks cleanup ordering, and
restores the prior process value without CopperStart DOS state. Compile its
entry and export `copperos.shell.public-process-state-probe`.

`PublicResidentMutationNativeProbe::Main` checks public resident add, duplicate
names, busy removal, replacement, missing entries, PURE enforcement and
cleanup. It uses the foreground fixture at `DH0:fixture` and writes
`DH0:resident-ok` or `DH0:resident-fail`; the `CopperOSProbeSys=true` variant
uses `SYS:C/fixture` and `SYS:T`. Compile its entry with export
`copperos.shell.public-resident-mutation-probe`. Its `EngineMain` variant runs
those mutations through the shared Shell command parser, requires the same
full-engine exports and stack gate as the runner probe below, and writes a
`DH0:resident-engine-ok`/`resident-engine-fail` marker plus a four-byte
diagnostic mask. These are focused tests, not resident-listing or full-Shell
qualification.

`tools/ImageBuilder/observe_resident_system31.py` creates a disposable image
and compares SYSTEM resident admission, removal, replacement, public segment
use count and listing order with an original Kickstart 3.1 ROM Shell. An
earlier proof image fails the ordering comparison. The updated proof matches
the observed return codes and three listings after excluding its extra `Cls`
internal command and registered `Shell` module; this is not full resident-list
qualification. A CopperStart proof run also passed the SYSTEM admission,
removal, replacement and process-exit checks before the separate listing-order
change. On the updated proof, both a shorter ordering check and the full
CopperStart admission, removal, replacement, interleaved ordering and Shell-exit
script passed.

`CliResultCaptureNativeProbe::Main` appends the preceding command's public
CLI return code and secondary result as two big-endian longwords to
`DH0:captured`. It can be run after a command in an original-ROM comparison
script to compare result levels without relying on a following Shell built-in
preserving them. Compile its entry with export
`copperos.shell.cli-result-capture-probe`.

`PublicRunnerNativeProbe::Main` runs the shared synchronous command engine
with a Shell-owned process record and no CopperStart DOS-state pointer. It
checks file-script execution with `.KEY` arguments and conditional control flow,
alias expansion, quoted arguments, output redirection and cleanup. On the
disposable `DH0:` diagnostic boot it temporarily assigns `T:` to `DH0:` for
the script's temporary reader and removes the assign afterward; do not run
that variant where an existing `T:` assign must be preserved. The probe also
runs the initialized-CLI entry on a borrowed input file and verifies its
output and restoration of the previous process-private value. It checks
standard-input handoff, an early `EndCLI`, and consuming an argument prefix
through public buffered DOS input, including mismatch and error preservation. Its
full-engine HUNK must include the same Shell and process-return exports as
`Shell-Seg`. This is not a test of the complete Shell startup or asynchronous
commands on an original ROM.

`PublicExportedRunnerNativeProbe::Main` creates a separate DOS CLI process,
installs Shell-owned process state there, and calls the `execute-begin` and
`execute-poll` exports through their 68000 register ABI with a null private
DOS-state pointer. It checks synchronous script output, the idle-poll result,
restoration of the CLI command-file and Shell-private fields, and child exit.
It passed on disposable Kickstart 3.1 and CopperStart proof fixtures. The
CopperStart run used a read-only DF1 helper disk for the probe HUNK and checked
that the Shell and child processes retired. It does not exercise a
pending continuation, an asynchronous child wait, or the complete Shell.
The proof rebuilt with those entry points also passed the Kickstart 3.1
control-flow replay in WinUAE and an unmodified-ADF boot with the staged V40
math library in the lightweight emulator.

`PublicExitLifecycleNativeProbe::Main` holds a newly created CLI process in
its `NP_ExitCode` callback until its parent has observed the child through
`FindTask` and `FindCliProc`. The parent then signals it, waits until the task
has retired, and writes `SYS:T/lifecycle.bin`. This deliberately distinguishes
entry into the callback from actual task retirement. The guest never unloads
its HUNK while its child can still execute it; the external emulator harness
provides the timeout. `tools/ImageBuilder/inspect_exit_lifecycle.py` validates
the binary observation in the resulting disk image.

The probe also exercises `PublicShellTaskIdentity`: it reserves two distinct
names for the same parent allocation, transfers one Exec `MemList` to a child
blocked on its startup message, and finds that child by the generated name
until it retires. The identity contains both the parent-owned allocation and
the child-owned reservation address; each remains reserved for its respective
side of the lifetime. This identity probe passed on Kickstart 3.1 in
`artifacts/shell-task-identity-ks-v1` and on CopperStart in
`artifacts/shell-task-identity-copper-v1`. A subsequent probe also verifies
that `CreateNewProc` copied the task name; that check passed on Kickstart 3.1
in `artifacts/shell-task-namecopy-ks-v1` and on CopperStart in
`artifacts/shell-task-namecopy-copper-v1`. This is a building block for the
pending continuation refactor.

`PublicContinuationOwnerNativeProbe::Main` separately checks a caller-owned
continuation while its child has published a result but remains alive, then
checks the result, `IoErr`, timed public wait and exactly-once release after
task retirement. A second child remains blocked until its owning Shell process
detaches the record and exits; an independent process then releases the child.
The initial owner probe and the later timed-wait version passed on both
Kickstart 3.1 and CopperStart. The timed-wait receipts are in
`artifacts/shell-continuation-wait-ks-v1` and
`artifacts/shell-continuation-wait-copper-v1`.
The extended probe also allocates frame-owned records and checks reclamation
after acknowledgement and detachment before child completion. It passed on
Kickstart 3.1 and CopperStart in `artifacts/shell-owned-continuation-ks-v1`
and `artifacts/shell-owned-continuation-copper-v1`. This probes the observer,
not a production launcher allocating these records.
The `PublicContinuationLaunchNativeProbe` also passes a frame-owned record to
the actual public background launcher, verifies the child output and result,
and checks reclamation after acknowledgement. The expanded probe passed on
Kickstart 3.1 and CopperStart in `artifacts/shell-owned-run-ks-v2` and
`artifacts/shell-owned-run-copper-v2`; both recorded stage `0x4fffffff`.
Ordinary `Run` still does not allocate such a record itself.
A standard V40 continuation-bearing Run now chooses the public launcher when
the current CLI has Shell-owned process state, even if the caller supplies a
non-null legacy context. The launch probe uses a sentinel context both for its
frame-owned record and for transferring an owned error stream. It passed on
Kickstart 3.1 and CopperStart in `artifacts/shell-mixed-launch-ks-v1` and
`artifacts/shell-mixed-launch-copper-v1`; the latter recorded stages
`0x4fffffff` and passed Shell exit. Extended scheduler options and callers
without Shell-owned state still retain the legacy fallback and were not newly
qualified by these cases.
Continuation-bearing NewShell and NewCLI now use the same Shell-owned-state
selection, regardless of a non-null legacy context. The expanded launch probe
runs both kinds with a sentinel context and an explicit startup file and
directory lock, checking startup output, relative output, continuation results,
and parent-directory restoration. It passed on both systems in
`artifacts/shell-mixed-newshell-ks-v1` and
`artifacts/shell-mixed-newshell-copper-v1`; the CopperStart stage record was
`0x4fffffff` and the Shell-exit checks passed. These are native platform tests,
not a new packaged proof-image build or complete-Shell qualification.
The public Run launcher also accepts explicit stack and priority options from
native platform callers. The options probe rejects a 63-byte stack, priority
128 and an invalid presence flag before transferring an owned error stream,
then successfully reuses that stream for a 98304-byte, priority -3 child. It
checks the child's actual task priority and stack bounds, CLI command stack,
unchanged parent stack, output and completion ownership. The probe passed on
Kickstart 3.1 in `artifacts/shell-public-options-ks-v1`. Its CopperStart run in
`artifacts/shell-public-options-copper-v1` did not finish within 150000 frames.
The read-only replay in `artifacts/shell-public-options-trace-v1` found the
priority -3 child on the ready list at the same initial PC in both its
50000-frame and 60000-frame captures. The parent used one-tick DOS delays;
the revised probe uses 25-tick delays for this lower-priority case. Neither
that revision nor the exact cause of the one-tick scheduling behavior is
qualified by the earlier run.
The revised 25-tick polling probe passed on Kickstart 3.1 in
`artifacts/shell-public-options-ks-v2`, recording stages `0x4fffffff` and zero
command result/error. Its CopperStart replay is
`artifacts/shell-public-options-copper-v2` also failed to finish within 150000
frames on the earlier ROM. The production
public parking loop's one-tick delays have not been changed by this test fix.

The exported Begin/Poll boundary now selects public execution before legacy
context initialization. The synchronous ABI probe runs once with a null
context and once with address 4 (which contains ExecBase, not DOS state),
checking completion, the no-runner Poll result, process-state restoration,
output and worker retirement. It passed on both systems in
`artifacts/shell-public-entry-context-ks-v2` and
`artifacts/shell-public-entry-context-copper-v2`; its runner HUNK is
`artifacts/shell-public-entry-context-native-v2/runner`. The owner probe also
calls both exported entries with null and sentinel contexts while a gated
public child is still pending. That expanded case passed on both systems in
`artifacts/shell-public-entry-pending-ks-v2` and
`artifacts/shell-public-entry-pending-copper-v2`; the latter recorded the full
`0x1fff` stage mask. The host command suite passed 1015 tests and the focused
path/runner ownership suite passed six tests with these changes. These probes
embed the modified Shell implementation. The later proof image
`artifacts/shell-public-entry-proof-v1/System.adf` also contains this boundary
change and retains the one-tick public parking loop.
An additional synchronous probe fills an allocated dummy context with `0xa5`
and checks every byte after Begin and Poll. This detects a legacy tail-word
write that checking ExecBase alone would miss. Its HUNK is
`artifacts/shell-public-entry-canary-native-v1/runner`.
The canary assertion passed on Kickstart 3.1 in
`artifacts/shell-public-entry-canary-ks-v1`; its CopperStart run is
`artifacts/shell-public-entry-canary-copper-v1` also passed.

An experimental production change used DOS delay intervals of at most 25 ticks
when parking a public continuation, capped by the caller's remaining finite
timeout. Its extended owner probe lowers the gated child to priority -3,
signals it, and parks indefinitely until the task has actually retired. That
adds stage `0x2000`, making the expected mask `0x3fff`; the existing one-tick
finite wait, zero-time completion, diagnostics and ownership checks remain.
The HUNK is `artifacts/shell-public-park-priority-native-v1/owner`, with native
runs in `artifacts/shell-public-park-priority-ks-v1` and
`artifacts/shell-public-park-priority-copper-v1`. The Kickstart run passed with
stages `0x3fff`; CopperStart did not finish within 40000 frames. The longer
interval was therefore reverted in production. The regression remains, and a
read-only task/RAM replay is retained in
`artifacts/shell-public-park-priority-trace-v1`. The command host suite passed all 1015 tests with the experiment;
it did not establish correct lower-priority completion on CopperStart.
The completed replay narrows this failure. At frames 20000 and 30000, the
priority-zero owner was in `TryParkScriptWait`, while the priority -3 child
remained ready at its saved Exec Wait return. The public owner record already
contained `Done = 1`; the child could not finish retiring. The 10000-frame
capture was earlier, during the allocation-pressure diagnostic's 20-byte disk
write, and does not describe the lower-priority wait.

`PublicDelayPriorityNativeProbe` isolates public DOS Delay from the Shell
runner. It starts a priority -3 child, calls Delay, records whether the child
ran, then blocks in Exec WaitPort to recover the child and raises its priority
only for safe retirement. The first version passed on Kickstart 3.1 in
`artifacts/shell-delay-priority-ks-v1`: the child ran during all 1-, 25- and
50-tick delays. On CopperStart in `artifacts/shell-delay-priority-copper-v1`,
it ran during none of those delays, but did run when the parent blocked in
WaitPort. The fixture completed and both processes retired; the qualification
correctly rejected `runner-fail`. Its binary observation is
`T/delay-priority.bin` (DLAY header, pass mask 1 instead of 7, three completed
cases). This establishes a native DOS Delay dependency failure on the tested
ROM, rather than inability of Exec to dispatch the lower-priority task.
The expanded probe adds a direct timer.device OpenDevice/DoIO control and
records its open result and request flags/error. The identical v2 HUNK passed
on Kickstart 3.1 (`artifacts/shell-delay-priority-ks-v2`, mask `0xf`) and
failed on the earlier CopperStart ROM (mask `1`, OpenDevice returned `-1`).
The reset boot had not installed timer.device, although the desktop startup
could install it. The boot correction reuses the timer provider with immutable
ROM vector and interrupt adapters. With that correction alone, all delay cases
ran their child, but cleanup exposed Exec CheckIO reporting a request already
completed by DoIO as pending. CheckIO now recognizes non-NT_MESSAGE requests
as complete. Nine focused host I/O tests pass; their asynchronous fixture
initializes the Exec task lists before simulating a blocked task.

The v2 HUNK passes against both corrections in
`artifacts/shell-boot-timer-delay-copper-v2`: all four cases completed, mask
`0xf`, each child ran during its delay, timer OpenDevice and DoIO error were
zero, and task retirement and Shell exit passed. CopperStart leaves the
drained request's node type zero, while the reference retained NT_REPLYMSG;
the probe requires working completion/cleanup rather than equality of that
internal post-consumption value. The updated main ROM SHA-256 is
`60a4049a8970255c62799b1e77ac1b4c4745e61a18a37d9dda3a33bd24744235`;
its extension is
`c0bddf915d412ef5e60653e15ad1f3af5a7b60ec32d6493ab4ecae57add33b60`.
The production Shell proof is unchanged (SHA-256
`fcd6aceb7d00b0e88751cf6b6358e68e72fff072bf5c8b2a25089b88c2dab2c4`).
The owner regression also passes with this ROM pair in
`artifacts/shell-boot-timer-priority-copper-v2`, and with original Kickstart
in `artifacts/shell-public-entry-priority-ks-v2`. Both require mask `0x3fff`,
including the priority -3 task's actual retirement through the one-tick wait.
These are focused regressions; complete-Shell qualification remains open.
The same ROM pair and unchanged proof image also pass
`artifacts/shell-boot-timer-options-copper-v2` (stages `0x4fffffff`, command
result/error both zero), `artifacts/shell-boot-timer-broad-copper-v2`,
`artifacts/shell-boot-timer-path-isolation-copper-v2`, and
`artifacts/shell-boot-timer-control-flow-copper-v2`. Each trace records no
remaining children/background workers and cleared runner/binding state after
Shell exit. In the path case the child changes its inherited path from
`CopperOS:S` to `CopperOS:C`, while the parent's final list remains unchanged.
The corresponding original-Kickstart control-flow replay passes in
`artifacts/shell-public-entry-control-ks-v2`.
Original-Kickstart runs also pass in
`artifacts/shell-public-entry-path-isolation-ks-v2`,
`artifacts/shell-public-entry-cd-ks-v2`, and
`artifacts/shell-public-entry-substitution-ks-v2`. The CD replay checks
directory-name changes, parent-directory syntax, and restoration after a
missing-directory error (return 10, secondary result 205). The substitution
replay exercises the no-T:-assign capture fallback. All three observe the
interactive Shell's eventual exit.
The broader Exec adapter/system-ROM host run in
`../CopperStart/artifacts/shell-timer-core-host-v1.log` passed 129 tests and
failed three: `DirectTaskRuntimeMatchesCanonicalGuestWordImage`,
`UnifiedTableInstallsEveryAdmittedClassicMorphOsAndPrivateVector`, and
`CompiledGetMsgDispatchesDeferredWakeAndLaterReturnsItsOriginalResult`.
Those failures remain unresolved; the focused I/O and native boot results do
not qualify that broader gate.
An isolated MSBuild override compiling the repository HEAD version of
ExecIoCore reproduces all three with the same diagnostics in
`../CopperStart/artifacts/shell-public-only-exec-baseline-v1.log`. This
controls the CheckIO change without reverting the worktree; it does not
identify the failures' causes or claim a completely clean-checkout run.

The later combined image is
`artifacts/shell-public-launch-proof-v1/System.adf` (SHA-256
`4acf2ee2b3868c2e16d8b520da574a07ed6e97405daf83b34e1407313b116d42`).
It includes public continuation-bearing Run/NewShell/NewCLI dispatch and the
stack/priority options, alongside buffered Execute and the previous cleanup
changes. Its background-command test passed on Kickstart 3.1 in
`artifacts/shell-public-launch-run-ks-v1`, and its broad background-command
test passed on CopperStart in `artifacts/shell-public-launch-broad-copper-v1`.
Its unmodified-image Kickstart boot loaded the staged `SYS:Libs/mathtrans.library`
and returned `RESULT 0` in `artifacts/shell-public-launch-exact-ks-v1`.
These checks do not establish complete-Shell qualification.
The rebuilt image containing the Begin/Poll boundary change is
`artifacts/shell-public-entry-proof-v1/System.adf`, SHA-256
`fcd6aceb7d00b0e88751cf6b6358e68e72fff072bf5c8b2a25089b88c2dab2c4`.
The build's recorded source list differs from the preceding combined proof
only in `DosShellNativeEntrypoints.cs`; producer-source stability and native
runtime gates passed. Unmodified-image Kickstart/mathtrans and broad
CopperStart replays were started under
`artifacts/shell-public-entry-exact-ks-v1` and
`artifacts/shell-public-entry-broad-copper-v1`.
The unmodified-image Kickstart replay passed and loaded the staged math
library (`RESULT 0`); the broad CopperStart replay remains pending. This image
predates the subsequent 25-tick public-parking change.
The experimental image containing that parking change is
`artifacts/shell-public-park-proof-v1/System.adf`, SHA-256
`e4fcac3b726271d3796fa19ba461236604a0c0d32e989e9d2c0921fe74f06c71`.
It booted unmodified on Kickstart 3.1 and loaded the staged math library
(`artifacts/shell-public-park-exact-ks-v1`, `RESULT 0`), and its Kickstart
script-control case passed in `artifacts/shell-public-park-control-ks-v1`.
Its CopperStart path-isolation replay is
`artifacts/shell-public-park-path-copper-v1` and remains pending. This image
predates the failed interval change's reversion and is not the current-source
deliverable.

An additional owner probe detaches a frame-owned observation whose record was
never placed in that frame's pending slot, then allows its child to complete.
It passed on Kickstart 3.1 and CopperStart in
`artifacts/shell-owned-frame-cleanup-ks-v1` and
`artifacts/shell-owned-frame-cleanup-copper-v1`. This exercises the
frame-detachment helper directly; it does not force pending-slot publication
to fail in the complete runner.
The mixed-context version polls, reads diagnostics, and acknowledges a public
owned record through a native platform value carrying a non-null context. It
passed on Kickstart 3.1 (`artifacts/shell-mixed-continuation-ks-v1`). A short
CopperStart diagnostic (`artifacts/shell-mixed-continuation-copper-diag-v1`)
recorded the probe's `0xfff` success stages and `runner-ok`, but the script did
not return within that diagnostic's 10000-frame window. This is not a complete
CopperStart pass. The longer run also failed to return within 150000 frames.
The expanded mixed-context probe also prepares and parks the public wait before
and after child retirement with a non-null context. It passed on Kickstart 3.1
and on the later CopperStart process-list-fix ROM in
`artifacts/shell-mixed-wait-ks-v1` and `artifacts/shell-mixed-wait-copper-v1`.
The exported `execute-park` entry now also selects public owners before legacy
context initialization. Calls through the actual register ABI with a non-null
context passed on both systems before and after child retirement; see
`artifacts/shell-mixed-park-ks-v1` and `artifacts/shell-mixed-park-copper-v1`.
Frame cleanup now detaches public observations before releasing legacy records,
including when the platform carries a non-null context. The expanded owner
probe puts a public frame-owned continuation and a legacy-owned allocation on
one frame, unbinds it while its child waits, and then lets the detached child
finish. It passed on both systems in `artifacts/shell-mixed-cleanup-ks-v1`
and `artifacts/shell-mixed-cleanup-copper-v1` (success stages `0x1fff`). The
legacy context is a minimal record-owner fixture, not an installed DOS library;
this test does not qualify every legacy runner or process-return combination.
The reproducible combined proof is
`artifacts/shell-shared-cleanup-proof-v1/System.adf` (SHA-256
`3c7f97b308f99045876ea34521b116e2a0fae641fa6893f643e2aff13b433f63`).
It includes the buffered Execute command, exported public park dispatch, and
mixed frame cleanup. Its CLI-owned path-isolation and Shell-exit probes passed
on Kickstart 3.1 and CopperStart in `artifacts/shell-shared-cleanup-path-ks-v1`
and `artifacts/shell-shared-cleanup-path-copper-v1`. Both observed the child
inherit SYS:S and change to SYS:C while the parent retained SYS:S. These are
focused ownership checks, not complete-Shell qualification.
The underlying legacy rollback and completion host tests also passed (59
tests); those host tests do not exercise the native fallback dispatch itself.
The same probe passed on the previous v13 image in
`artifacts/shell-mixed-continuation-copper-v13`. A diagnostic capture of the
v14 run found the Shell in `DosNativeProcessReturn::Entry`, waiting in
`DosNativeEntrypoints::PrepareTaskRemoval`; its CLI return code was `0x184c8`
instead of the probe's zero result. See
`artifacts/shell-mixed-return-trace-build-v1/trace.log` and
`artifacts/shell-mixed-continuation-copper-ram-v1`. The cause is not yet
established, and v14 must not be treated as fully verified.
The frame-detachment call was isolated in two separately built diagnostic
images; both passed the same CopperStart probe, including the variant that
retains the call. A third reconstruction restored the three observer-dispatch
methods to their v14 source and produced a Shell HUNK byte-identical to v14.
The passing baseline contains the later public-observer dispatch change.
The controlled build therefore ties the difference to that source change or
its effect on code generation; it does not establish the underlying mechanism.
See `artifacts/shell-frame-detach-study-v1`,
`artifacts/shell-frame-detach-baseline-copper-v1`, and
`artifacts/shell-frame-detach-without-copper-v1`.
The normal v15 proof build subsequently produced a Shell HUNK identical to the
passing baseline. Its CopperStart mixed-context probe and Kickstart 3.1
control-flow probe passed in `artifacts/shell-proof-v15-mixed-copper` and
`artifacts/shell-proof-v15-control-ks`. This does not explain the v14 return
code corruption or qualify every Shell behavior.
The later `artifacts/shell-mixed-wait-proof-v1/System.adf` includes the public
wait dispatch change. Its Kickstart control-flow probe passed in
`artifacts/shell-mixed-wait-proof-ks-v1`; an unmodified-image boot executed a
Shell command and opened the staged `SYS:Libs/mathtrans.library` with result
zero in `artifacts/shell-mixed-wait-exact-ks-v1`. These remain focused checks.
The new proof's CopperStart control-flow script did not complete within 15000
frames on the process-list-fix ROM. A longer diagnostic reached 30000 frames
without completion before its diagnostic ADF export failed on an in-progress
track write. The previous v15 proof also did not complete within 15000 frames
with either the process-list-fix ROM or the earlier ROM. See
`artifacts/shell-mixed-wait-proof-copper-v1`,
`artifacts/shell-mixed-wait-proof-copper-diag-v1`,
`artifacts/shell-mixed-wait-v15-control-copper-v1`, and
`artifacts/shell-mixed-wait-v15-originalrom-control-v1`. These observations do
not establish the cause or a complete CopperStart control-flow pass.
A separate replay of the v15/earlier-ROM fixture captured the Shell at 10000
frames waiting during a DOS `Write` provider packet, with return code zero;
it remained runnable between such waits. See
`artifacts/shell-control-flow-trace-v1`. This is a point-in-time observation,
not proof that the whole script eventually completes.
That packet's payload was a line being copied by external `Execute` into its
expanded temporary script. Execute now buffers that output in bounded 4 KiB
blocks, checks short writes, and requests input buffering for its owned script
handle. The diagnostic replacement command passed the normal Kickstart
control-flow case (`artifacts/shell-execute-buffer-control-ks-v1`). At the
CopperStart 15000-frame bound it had produced argument, true-branch,
else-branch, and variable markers, whereas the earlier capture was still
preparing the temporary script. The whole CopperStart case remains incomplete.
See `artifacts/shell-execute-buffer-control-copper-v1`. These diagnostic images
replace only C:Execute and are not new reproducible proof builds.
An expanded control script larger than 8 KiB passed on Kickstart 3.1,
including final content markers and Shell exit, in
`artifacts/shell-execute-buffer-large-ks-v3`. The keyboard injector now waits
after the Execute line for the child-script marker before sending the remaining
commands. Earlier large-script runs used premature exit observation (v1) or
waited before Execute had been typed (v2); neither was a complete pass.
The large CopperStart run also exceeded 15000 frames; its saved temporary
file contained the first complete 4 KiB block. See
`artifacts/shell-execute-buffer-large-copper-v1`. CopperStart completion and
failure-path behavior of the buffered writer remain to be verified.
The normal buffered control-flow fixture subsequently completed on CopperStart
with the full 150000-frame allowance (script completion occurred after 20000
frames), including all expected content markers and Shell exit. See
`artifacts/shell-execute-buffer-control-copper-v2`. The earlier 15000-frame
failures therefore do not establish a hang. This later pass covers the normal
fixture, not the larger-than-8-KiB CopperStart fixture or write failures.
The unexpected v14 return value `0x184c8` was the live `dos.library` base in
the captured guest memory. A one-instruction diagnostic changing the probe's
non-null platform context to null still reproduced the v14 stall, so that
context value alone is not the trigger. A separate 68000 wrapper recorded a
zero result from an inner `RunCommand` on both v14 and v15, but the added
wrapper also prevented the original stall; this does not locate the fault.
See `artifacts/shell-v14-null-context-diagnostic-v1`,
`artifacts/shell-v14-raw-run-copper-v1`, and
`artifacts/shell-v15-raw-run-copper-v1`. The wrapper is generated by
`tools/ImageBuilder/make_run_result_probe.py`.
An instruction-level replay of the original v14 execution, without the
wrapper, observed D0 equal to `0x184c8` immediately after the foreground
Shell's `RunCommand` call and before the Shell stored the result. The CLI
return code later changed to that same value. This places the corruption at
or before the DOS return boundary. A second replay shows the probe returning
zero and the stack-switch callback preserving that zero. The DOS resume path
then rejects restoration of the RunCommand input state and returns the saved
original D0, the DOS base, instead. The rejection follows a failed process
record identity check; the reason for that failure remains unverified. The
bounded traces are in `artifacts/shell-instruction-trace-v1` and
`artifacts/shell-command-return-trace-v1`.
An additional capture at the failed comparison found two valid DOS process
records for the Shell task: the newly found record precedes the record saved
by `RunCommand` in the DOS process list. The newer record's stream and directory
fields were empty. This explains the identity mismatch but does not yet explain
why DOS created the duplicate. See `artifacts/shell-process-record-trace-v1`.
The duplicate appeared during a `Delay` call while the command was still
running. An instruction replay shows `GetProcess` failing validation of a
process-list node at `0x4d2f0`; the node had been cleared during another task's
retirement, while the surviving Shell record remained in the list. `SetError`
then allocated a new record for the Shell task. This is evidence of an
unsynchronized traversal and retirement interaction in the frozen CopperStart
ROM, not a justification for accepting duplicate records. See
`artifacts/shell-duplicate-record-trace-v1` and
`artifacts/shell-duplicate-instruction-v1`.
A subsequent CopperStart change protects process-list lookup, publication,
and removal with scheduler exclusion. A deterministic host regression forces
retirement at the vulnerable lookup, and 107 related DOS tests passed. A new
paired CopperStart ROM built from the current source completed the previously
stalling v14 probe and Shell exit; see
`artifacts/shell-process-list-lock-copper-v1/qualification.json`. The new ROM
also includes other changes since the frozen ROM, so this replay alone does not
isolate the fix. Other process-list traversals and complete Shell compatibility
remain to be reviewed and verified.
An additional probe calls the exported `execute-park` entry through its 68000
register ABI with a null DOS-state pointer; it passed on Kickstart 3.1 in
`artifacts/shell-public-continuation-park-ks-v1` and on CopperStart in
`artifacts/shell-public-continuation-park-copper-v1`.
The extended owner probe also checks invalid records and deliberately exhausts
public memory through allocations while scheduling is forbidden, then restores
it before resuming. The Kickstart 3.1 run in
`artifacts/shell-public-continuation-oom-ks-v3` and the CopperStart run in
`artifacts/shell-public-continuation-oom-copper-v3` recorded all checks
(`0x7ff`), including `ERROR_NO_FREE_STORE`.

`PublicContinuationLaunchNativeProbe::Main` exercises the actual public
continuation-bearing `Run` and `NewShell` launchers with a caller-owned record.
It runs a permanent resident copy of its own HUNK, waits for each worker's task
to retire, and verifies command output and results. The permanent test segment
remains installed until the disposable guest boot ends. The combined probe
passed on a Kickstart 3.1 fixture in
`artifacts/shell-public-continuation-newshell-ks-v3`; its stage word is `0xfff`.
The combined probe also passed on CopperStart in
`artifacts/shell-public-continuation-newshell-copper-v2`, with stage word `0xfff`.
The extended probe starts a real background worker with an owned error stream,
detaches its continuation, retires its parent process, and then runs a delayed
command that verifies the parent is gone and writes through that stream. It
passed on a Kickstart 3.1 fixture in `artifacts/shell-public-detached-ks-v1`,
with stage word `0x1ffff`. This covers one parent-exit stream combination; it
does not cover every launch form or the complete Shell.
The expanded parent-alive control and parent-exit probe also passed against
the updated image in `artifacts/shell-public-detached-control-ks-v1`, with stage
word `0x7fffff` and child result/IoErr both zero.
The added independently owned input case passed on Kickstart 3.1 with the
current no-tag Shell in `artifacts/shell-public-detached-input-ks-v2`, with
stage word `0x0fffffff`. The latest `NIL:` handle change also passed both
parent-exit input cases on Kickstart in
`artifacts/shell-public-detached-input-ks-v3`.
The corresponding CopperStart probe failed in
`artifacts/shell-public-detached-copper-v2`: the parent and worker retired,
but the transferred error file was empty and the delayed command did not write
its own diagnostic. A parent-alive control is being used to investigate where
the command stopped. In `artifacts/shell-public-detached-control-copper-v1`,
that control completed successfully with result and IoErr zero while the
parent-exit case still left an empty error file. A separate diagnostic build in
`artifacts/shell-public-detached-input-copper-v1` observed that an independently
owned `NIL:` input succeeded after parent exit, while the borrowed input case
failed (stage word `0x0ffeffff`). The same comparison with the previous no-tag
production build in `artifacts/shell-public-detached-input-copper-v2` produced
the same stage word. With borrowed `NIL:` input reopened for the worker, both
parent-exit input cases passed on CopperStart in
`artifacts/shell-public-detached-input-copper-v3` with stage word `0x0fffffff`.
The continuation probe now requests `SYS:S` while keeping the parent directory
unchanged, and checks the directory reported by both `Run` and `NewShell`.
It passed on Kickstart 3.1 in
`artifacts/shell-public-directory-continuation-ks-v10` and on CopperStart in
`artifacts/shell-public-directory-continuation-copper-v10`, both with stage
word `0x0fffffff`. These checks do not establish every directory or stream
combination.
The stronger probe also checks a relative file write under `SYS:S`. It passed
on Kickstart in `artifacts/shell-public-directory-continuation-ks-v11` and on
the rebuilt CopperStart ROM in
`artifacts/shell-public-directory-continuation-copper-v12`. The CopperStart
probe uses a longer wait to accommodate its floppy-based script execution.
The `v14` probe additionally checks a relative file write from `NewShell`;
it passed on Kickstart in
`artifacts/shell-public-directory-continuation-ks-v14`, with the corresponding
CopperStart replay still running.

`PublicRunnerDirectoryNativeProbe::Main` runs a script that changes directories
twice and invokes a small command after each change. The command checks that the
active Shell frame's borrowed directory lock equals the current Process lock.
It passed on Kickstart 3.1 in `artifacts/shell-public-runner-directory-ks-v1`
and on CopperStart in `artifacts/shell-public-runner-directory-copper-v1`.

`PublicCreateProcessLockNativeProbe::Main` supplies an explicit shared lock to
`CreateNewProc` and forces an early process creation failure with an
unattainable stack size. It verifies that the lock remains held until the
caller releases it, by attempting an exclusive lock before and after release.
The Kickstart 3.1 test passed in `artifacts/shell-lock-failure-ks-v1`, and
the CopperStart test passed in `artifacts/shell-lock-failure-copper-v1`. This
tests one failure point; it does not prove DOS behavior at every failure point.
The `CreateNewProc` autodoc and DOS manual give conflicting instructions for
the supplied lock on failure, so this observation is deliberately not assumed
to cover all failure paths.
The expanded probe additionally starts a short-lived child with an explicit
lock and checks its eventual release. It passed on Kickstart 3.1 in
`artifacts/shell-lock-transfer-ks-v2`. On the tested CopperStart ROM,
`artifacts/shell-lock-transfer-copper-v2` recorded `0x16f`: the caller's lock
remained held after child retirement and the exclusive lock succeeded only
after the probe released it. The Shell's new inheritance path avoids passing an
explicit `NP_CurrentDir`. A CopperStart DOS fix is tested separately below.
An additional version of the probe holds the child open long enough to check
that its public `pr_CurrentDir` contains the exact supplied lock. The original
Kickstart in `artifacts/shell-lock-transfer-ks-v3` and the rebuilt CopperStart
ROM in `artifacts/shell-lock-transfer-copper-v3` both recorded all checks
(`0x2ef`), including release after child exit. Host tests separately cover
rollback when process publication fails and an explicit zero directory.
Those v1–v3 guest probes used a file lock in the directory field, so they are
diagnostic evidence rather than a valid-directory conformance check. The
updated probe creates a directory; it passed on Kickstart in
`artifacts/shell-lock-directory-ks-v4` and on the rebuilt CopperStart ROM in
`artifacts/shell-lock-directory-copper-v4`, each recording `0x2ef`.

On an original Kickstart 3.1 ROM and on CopperStart, the callback received return code 7 and ran
in the child, which was still visible as both a task and a CLI. After the task
retired, its CLI number was absent. This focused observation does not prove
the ordering of every DOS resource release or qualify a continuation owner.
The results are in `artifacts/shell-exit-lifecycle-ks-v1/run/candidate.hdf`
and `artifacts/shell-exit-lifecycle-copper-v1/guest.result.adf`; the fixtures
use the shared Shell from the rebuilt proof image and injected guest input.

Using the `$p` and `$compiler` variables from the build instructions above,
the lifecycle probe can be compiled without linking the complete Shell engine:

```powershell
dotnet $compiler "$p/CopperOS.Shell.Dos.NativeRoot.dll" --entry 'CopperOS.Shell.Dos.NativeRoot.PublicExitLifecycleNativeProbe::Main' --platform amiga --cpu 68000 --clr always --exceptions yolo --fpu disabled --format hunk --symbols off --runtime freestanding --memory none --peephole fixed-point --code-size-optimizations on --managed-assembly "$p/CopperSharp.Sdk.Amiga.dll" --managed-assembly "$p/CopperSharp.Sdk.Amiga.Support.dll" --managed-assembly "$p/CopperSharp.Compiler.dll" --managed-assembly "$p/CopperStart.Exec.dll" --managed-assembly "$p/CopperStart.Dos.dll" --include-export 'copperos.shell.exit-lifecycle-probe' --include-export 'copperos.shell.exit-lifecycle-worker' --include-export 'copperos.shell.exit-lifecycle-callback' --output artifacts/shell-native-platform-probe/lifecycle --compatibility-report artifacts/shell-native-platform-probe/lifecycle.compatibility.json
python tools/ImageBuilder/qualify_shared_shell31.py --image <System.adf> --rom <Kickstart-3.1.rom> --winuae <winuae64.exe> --output-directory <fresh-results-directory> --runner-hunk artifacts/shell-native-platform-probe/lifecycle
python tools/ImageBuilder/inspect_exit_lifecycle.py <fresh-results-directory>/run/candidate.hdf
```

Using the `$p` and `$compiler` variables above, build this probe as follows:

```powershell
dotnet $compiler "$p/CopperOS.Shell.Dos.NativeRoot.dll" --entry 'CopperOS.Shell.Dos.NativeRoot.PublicRunnerNativeProbe::Main' --platform amiga --cpu 68000 --clr always --exceptions yolo --fpu disabled --format hunk --symbols off --runtime freestanding --memory none --peephole fixed-point --code-size-optimizations on --managed-assembly "$p/CopperSharp.Sdk.Amiga.dll" --managed-assembly "$p/CopperSharp.Sdk.Amiga.Support.dll" --managed-assembly "$p/CopperSharp.Compiler.dll" --managed-assembly "$p/CopperStart.Exec.dll" --managed-assembly "$p/CopperStart.Dos.dll" --include-export 'copperos.shell.public-runner-probe' --include-export 'copperos.shell.entry' --include-export 'copperos.shell.child' --include-export 'copperos.shell.background' --include-export 'copperos.shell.execute-begin' --include-export 'copperos.shell.execute-poll' --include-export 'copperos.shell.execute-park' --include-export 'copperos.shell.queue-handler-task' --include-export 'copperstart.dos.process-return' --output artifacts/shell-native-platform-probe/runner --compatibility-report artifacts/shell-native-platform-probe/runner.compatibility.json
```

The same probe can exercise the proof Shell's 68000 stack gate on a real
Kickstart by first wrapping its HUNK:

```powershell
python tools/ImageBuilder/wrap_shell_stack.py artifacts/shell-native-platform-probe/runner <helper>/runner
```

Launch it from a Shell with a small command stack, check `runner-ok`, and run
another command afterward to check that the caller's stack was restored. This
is a focused entry-gate test; it does not qualify the complete Shell.

For a writable CopperOS proof-disk diagnostic, build with
`-p:CopperOSProbeSys=true`; the probe then uses `SYS:T` for its temporary file
and result marker and `SYS:C/fixture` for its foreground command. A test image
must contain a writable `T` directory.

`PublicChildLaunchNativeProbe::Main` creates a DOS CLI child using public
`CreateNewProc` tags and sends it a public Exec message. It checks that DOS
duplicates the parent's explicit CLI path node and directory lock, that both
locks name the same directory, and that a borrowed output handle remains open
after the child exits. Run it only after adding a directory to the CLI path.
It writes `SYS:T/child-ok` or `SYS:T/child-fail`, and the output file must be
`before`, `child`, and `after` on separate lines. Compile with both exports
`copperos.shell.public-child-launch-probe` and
`copperos.shell.public-child-launch-worker`, and apply the stack gate above.
The probe also records the child's CLI and process input/output fields,
default stack and error stream as eight big-endian longwords in
`SYS:T/child-cli.bin`, for inspecting how DOS initializes a public CLI child.
The proof test can stage this HUNK with `qualify_shared_shell31.py
--child-launch --child-hunk <path>`. This measures the public primitives; it
does not qualify the Shell's own asynchronous launcher or guarantee the
lifetime of a borrowed handle after its owner exits. Add `--child-keep-shell`
to run a diagnostic variant that leaves the Shell alive instead of testing
`EndCLI`; it records a separate follow-up command marker. The
normal variant also checks that the Shell task exits.

One earlier proof build repeatedly left the injector's `input-sent.txt`
entry incomplete after the child-and-exit sequence, while the keep-Shell
variant and the later proof build passed. Additional timing diagnostics did
not establish a cause. Treat an incomplete filesystem or missing exit marker
as a failed probe, not as evidence that the Shell exited.
An independent observer started before the Shell and reached its intermediate
timer checkpoint in both a live-Shell control and an EndCLI run. The live
control recorded the Shell as present at its later checkpoint; the EndCLI run
had no later observer or injector marker and did contain `second-boot.txt`,
indicating that the startup injector ran again. A separate EndCLI run lacked
both the later observer marker and the second-boot marker. These observations
do not establish the cause of the missing markers or repeated startup.
The same baseline subsequently passed both observers with a 120-second
window and no second-start marker, so the 60-second timeout alone does not
establish a Shell failure.
Earlier receipts predate the second-start check and must be audited before use:
for example, the `kickstart-run-split-v7b` output contains the marker and is
not valid evidence of a single boot. The v8 path, broad Run, delayed nested,
multi-descendant, and nested-lifetime outputs have no such marker.
A v8 `--run --run-keep-shell --independent-exit-observer` run also produced
an extra newline after `AFTER` in the empty-Run notice capture; its other
captured command outputs matched. The cause was not established. A v9 `--run`
run with the independent exit observer subsequently matched the expected
capture and did not report a second start; one pass does not establish that
the earlier difference has been eliminated.
The v9 `--run-leading-split` run produced the expected output and exit
markers but also recorded a second start, so its combined result is a failure.
In a v9 split-redirection control without `EndCLI`, the expected redirection
outputs appeared, the independent observer found the Shell alive, and no
second-start marker appeared; the follow-up command's marker was missing. This
also fails qualification and does not by itself establish why input or execution
did not progress.
On the same v9 image, each isolated split case (empty, child output, error,
input, and shared output/error) passed once with the Shell kept open, including
the follow-up command and an independent live-Shell observation. These single
runs do not distinguish timing-dependent failures from interactions between
cases.
After the v11 console-input change, the combined keep-Shell case passed; the
combined case followed by `EndCLI` again recorded a second startup despite
the expected command and exit markers. The second-start cause is unresolved.
An `EndCLI` comparison with independent `NIL:` inputs for all applicable
workers also recorded a second startup, so sharing the script reader alone is
not sufficient to explain that observation.
Both input variants passed once with a 300-tick pause between injected
command lines, including the independent exit observer and second-start check;
this indicates timing sensitivity without identifying its cause.
The combined keep-Shell case also passed once when the keyboard injector
waited 300 DOS ticks after each Return. That observation does not establish
the cause of the immediate-input failure.
The synchronized interactive-input probe supplies stronger evidence for a
separate problem: the v9 worker and Shell reported the same input handle,
and the follow-up did not finish while the child was in `RunCommand`. On v11,
the handles differed and the follow-up finished before the child returned;
the independent `NIL:` control also passed. Repeated v11 launches using
console input returned to the same free-public-memory measurement after
warmup and two further batches.
The v12 shared launcher also passed the synchronized check on CopperStart,
including distinct input handles, the follow-up before child completion, and
Shell teardown. The test verified that the process's original console-task
field was restored after opening the worker's handle. Kickstart passed the
same handle-identity and ordering check, and repeated console-input launches
returned to the same free-public-memory measurement after warmup and two
further batches.
The earlier CopperStart input-overlap receipt did not check the handler behind
the worker's distinct input handle. The later trace showed that `Open("*")`
could resolve through the current directory to a file named `*`; consequently
that receipt does not establish that the worker could read from the console.
The reusable overlap probe now checks the handler identity as well; against
the old ROM it rejects the worker's filesystem handler as intended.
A broader CopperStart v12 script did not pass: its leading-output `Run` left
the notice file empty and did not produce the child output; the trace recorded
result 20 and IoErr 202 at the next command. Other recorded outputs matched.
The cause and whether this result is timing-dependent remain under investigation.
A second v12 run of the same broad script reproduced the missing leading-output
files using the reusable fixture, without the earlier run's separate interactive
command prelude. Its other recorded outputs matched, and the Shell task exited.
A reduced sequence containing only a background `Execute` followed by that
leading-output `Run` reproduced result 20 and DOS error 202. Giving the second
worker an independently opened `NIL:` input made the reduced sequence pass.
This implicates the interactive-input path or its timing; it does not establish
which public DOS operation produced the error. A subsequent instruction and
stack trace located error 202 in CopperStart's `Open("*")` provider call: its
relative-path fast path selected the current filesystem directory before
checking the special console name. With the corrected ROM, the strengthened
overlap probe observed distinct handles attached to the same console handler
and completed the follow-up before the background command returned. The same
corrected ROM also passed the reduced two-command reproduction with return
code and DOS error both zero. A native ROM regression that sets a current
directory and opens two console aliases passed with the corrected ROM and
failed against the prior ROM. The native regression also verifies that a
longer relative filename beginning with `*` remains a filesystem file. The
corrected ROM passed the broad background-command script on the v12, v13,
and shared-bootstrap v14 proof images; the result disks had no newly created
`*` file. The path-vector ROM and v15 proof passed the interactive input and
teardown test. Wider proof-image checks remain under verification.
The broad sequence's first three or first four background commands, followed
by the leading-output `Run` without the nested `Execute`, also passed.
A shorter v12 script that starts with the same leading-output `Run` passed
once on CopperStart. It also checked independently redirected error, input,
and shared output/error streams and observed the Shell runner retire. This
does not establish why the longer sequence failed. The reusable
`qualify_copperstart_run.py` fixture provides both sequences for further
comparisons; its receipts are focused results, not complete Shell qualification.
For the immediate Kickstart `EndCLI` restart, an isolated emulator log showed
out-of-range reads during a ROM stack restore before reset. The subsequent
guest capture reported `0xffffffff` as the retained alert code, so it did not
identify the failing task or establish the cause. A later read-only capture
from the verified test-owned WinUAE process, while using the original ROM,
observed alert `0x81000005` before the restart. The recorded task was
`CopperOS Shell`, and its stack showed DOS freeing the CLI path nodes during
process cleanup. The Shell had allocated those nodes with `AllocMem`, whereas
the V40 cleanup uses `FreeVec`. The shared native path implementation now
allocates and releases path nodes with `AllocVec`/`FreeVec`; CopperStart's DOS
path copying and cleanup use the corresponding vector layout.
The fresh v13 proof reproduced that WinUAE reset with the same out-of-range
ROM stack read and a second-start marker. Its separate broad background-command
probe passed; the reset remains a failure of the combined exit case.
Separate v13 tests for each leading-redirection case, and a control with no
background command at all, also reset after the script and `EndCLI`. The
no-background and combined cases still reset when the injector's extra
exit-state file writes were suppressed. These observations do not by themselves
identify the failing task or distinguish Shell from the remaining test setup.
On v14, the `Path` and `FailAt` keyboard probes passed. Separate `CD`,
`System()`, substitution, and resident probes produced their expected command
outputs but recorded a second boot and therefore did not qualify. After the
allocation fix, the v15 no-background and combined-redirection exit probes
passed with an independent observer and no second boot; the broad Run, Path,
CD, FailAt, System(), substitution with and without T:, and resident probes
also passed. The unmodified v15
proof ADF separately booted under the original Kickstart 3.1 ROM, accepted a
Shell command, and loaded the staged `SYS:Libs/mathtrans.library` V40 probe.
The v18 image repeats the unmodified boot and combined exit checks. Its
`NewShell` and `NewCLI` probes run startup scripts in independent children,
check inherited aliases, variables, path, directory and stack, and observe the
parent exit. Separate `NewShell` probes cover the default startup file and an
explicit console window. The v18 proof also passes CopperStart `NewShell` and
`NewCLI` startup and inherited-state probes, with both tasks retired and no
stray `*` file. A separate CopperStart `NewShell` probe checks an inherited
prompt using the same DOS `GetPrompt` guest program as the Kickstart test.
The rebuilt v19 proof has the same Shell HUNK as v18. Its unmodified ADF also
booted with the original Kickstart 3.1 ROM in the Lightweight emulator,
accepted a Shell command and loaded `SYS:Libs/mathtrans.library` (V40). Its
Kickstart `NewShell`, `System()`, path management, background-command and
combined-redirection shutdown checks passed in WinUAE. CopperStart also passed
the overlapping-input, Shell teardown and `NewShell` inheritance probes on
v19. A v19 WinUAE test of repeated background workers receiving an owned
output stream observed the same free public memory after warmup and both
subsequent batches (1,299,288 bytes each).
A repeated `NewShell` startup and teardown probe initially missed a keyboard
follow-up and recorded a 512-byte difference in the last sample. A repeat
without that follow-up, with three warmup launches and a 50-tick settling
period after each child, passed with 1,299,960 bytes in all three samples. The
first run does not establish the cause of the difference.
Separate disposable WinUAE controls booted the original ROM Shell without an
`L:Shell-Seg`. With either a present or a missing `FROM` file, keyboard input
reached the new Shell and then its parent after `EndCLI`; the present file also
ran its startup command. The v18 launcher passed the equivalent missing-file
input and exit test. This does not compare every launch failure or window option.
A diagnostic copy of that test's disk, converted from HDF to ADF and booted
with the same Kickstart ROM in the Lightweight emulator, produced the expected
redirected output files and exit-observer markers without a second-start
marker. Its injector did not record the initial Shell-present marker, however,
so this is a comparison rather than full qualification. The emulator and disk
transport differ from the WinUAE HDF test that reset; neither difference has
been isolated as the cause.
An additional WinUAE HDF control started Kickstart's original Shell with the
same keyboard injector. It reached the test script (`runfinish` and the input
receipt were present) without a second-start marker. Some redirection outputs
differed under the original Shell's `Run` behavior, and the observer searched
for the CopperOS task name, so this is a partial injector control rather than
an identical workload or an original-Shell exit qualification. A repeat with
no `L:Shell-Seg` on the disposable disk produced the same observations.

The separate `qualify_shared_shell31.py --run` probe executes background
built-ins, an external command, a nested script, inherited paths, quoting,
and output/error redirection on a disposable Kickstart 3.1 HDF. It checks
the CLI notice and an empty `Run` as well. `--run-lifetime` waits for each
background worker to exit and compares public free memory after warmup and
two further batches. `--run-owned-lifetime` repeats the check with an output
stream transferred to the worker. These modes inject keyboard input and do not
qualify unmodified image startup or the complete Shell.
`--run-nested-delayed` checks that a delayed descendant can still write to a
transferred standard output. `--run-nested-control` runs the same command while
the stream owner remains alive; `--run-nested-lifetime` measures repeated
nested launches after all background Shell tasks have retired.

The fingerprinted nonresident proof package was exercised with the Kickstart
3.1 ROM in WinUAE. `--nonresident-shell --nonresident-retire` starts the parent
from a DOS-owned segment without registering it permanently, ends the parent,
and checks that a delayed background command sees no parent task and still
writes its result. `--nonresident-memory` samples free public memory after a
warmup and two more sequential worker exits; all three samples were 1,288,448
bytes in that run. A separate `--new-shell` run checked the nonresident
launcher, inherited CLI state, and independent exit. A nested `Run` test also
started a worker from a copied worker and checked its output and both notices.
These are focused tests
using an injected startup and do not establish complete Shell compatibility.
The `--nonresident-mismatch` fixture substitutes a copy with a changed build
fingerprint after the parent starts. Kickstart rejected the launch with result
20 and secondary error 122, produced no child output, and restored the original
installed file. `--nonresident-missing` likewise checks error 205 and restores
the temporarily renamed file.
The unmodified ADF from the same build also booted under Kickstart 3.1 and
opened the staged `SYS:Libs/mathtrans.library` in the Lightweight emulator.
On CopperStart the same ADF passed the permanent-Shell `Resident SYSTEM` and
exit control. The separate nonresident boot did not reach its prompt within
30,000 frames; diagnostic runs observed its public `LoadSeg` loading the
large Shell through the floppy reader. This does not qualify the nonresident
worker path on CopperStart.
The original V40 guest segment-array probe recorded a count of 2, two nonzero
ROM resident segment references, a program segment in entry 3, and zero in
entry 4. The corresponding CopperStart nonresident probe exposed the HUNK
directly in `pr_SegList` and failed `Run` with result 20 and IoErr 122. The
DOS-side fix now publishes an owned classic array for ordinary process
launches; CopperStart leaves its two resident BCPL entries zero until it has
equivalent resident segments. The public structure is described in the
[AmigaDOS process documentation](https://wiki.amigaos.net/wiki/AmigaDOS_Data_Structures).
The host launch, failure, retry, and reset tests cover its ownership. With a
rebuilt CopperStart ROM, the nonresident `Run` replay produced the child output,
reported return code and IoErr 0, and observed both worker and parent retire.
This is a focused replay; it does not qualify the complete Shell.
With the same corrected CopperStart ROM, a separate nonresident `NewShell`
replay passed its startup script, inherited alias, variable, command path,
directory and stack checks, and observed the child and parent retire. A later
source-checked proof build also passed the Kickstart nonresident `Run` test in
which a delayed child writes after its parent Shell exits. Its Kickstart tests
also passed `NewShell` inheritance, CLI path operations, synchronous and
asynchronous `System()`, background commands, resident commands, and command
substitution. The unmodified proof ADF booted under the original Kickstart 3.1
ROM in the Lightweight emulator, accepted a Shell command, and loaded the
staged `SYS:Libs/mathtrans.library` with result 0. These focused tests do not
resolve the remaining private legacy continuation paths described in the
Shell README.
The newer CopperStart legacy entry, using Shell-owned boot-runner state and
the CLI-owned path, passed `System`/`Execute`, `NewShell`, nested background
launch, interactive `Execute`, and a one-megabyte boot. A combined OFS script
test exceeded its original 48,000-frame boot allowance; the identical test
passed with an extended allowance and reached the prompt just after 52,000
frames. The FFS variant passed at the original limit. The test now allows
64,000 frames by default without changing its assertions, and the OFS case
passed again under that default. An older build
passed the OFS case but exceeded the old limit on the FFS case, so the old
timeout alone does not isolate a Shell regression.
An additional CopperStart proof-image `SystemTagList` probe failed after its
150,000-frame limit: at both 20,000 and 30,000 script frames a marked guest
snapshot shows the parent Shell waiting and a child named `Shell` waiting with
an empty process message port. The child
has CopperStart's legacy synchronous-System marker in its Shell-private field.
The public proof entry waits for a DOS startup packet. CopperStart now queues
that packet before publishing the child, retains it through its reply, and
provides the command in a separate CLI input stream. On the rebuilt paired
CopperStart ROMs, the proof-image replay completed synchronous and asynchronous
`System`, passed an explicit input stream to a command, and verified that the
synchronous caller could still use its input handle after the call. The same
input and lifetime probe passed on Kickstart 3.1; the previous proof build
returned from `System` but delivered an empty input stream to the command.
An extended replay on the rebuilt, paired CopperStart ROMs also passed
`System(NULL)` without `SYS_Async`: the call returned and the child executed
commands from the supplied input. The same guest probe passed under Kickstart
3.1. Host tests separately check the startup flags for the null-command case.
The legacy CopperStart boot now registers a packet-started Shell segment that
uses the same shared startup runner, with the legacy image's temporary stack.
Its native `System`/`Execute`, interactive `Execute`, two `NewShell`, OFS and
FFS script, and one-megabyte replays all passed after that change; the direct
worker entry remains available for independently created Shell workers.
The newer unmodified proof ADF also booted with the Kickstart 3.1 ROM, accepted
a Shell command, and opened `SYS:Libs/mathtrans.library` successfully. Focused
Kickstart probes passed path and CD operations, `NewShell` inheritance,
resident commands, command substitution, and background worker lifetime.
These checks do not qualify the complete Shell or remove the private legacy
continuations described in the Shell README. The bounded CopperStart probe
leaves its guest disk and optional RAM snapshots for diagnosis.

A combined Kickstart 3.1 proof-image script also passed `.KEY`/`.DEF` argument
substitution with custom delimiters, both `If` branches, `Skip`/`Lab`, local
variables, an alias, output and append redirection, and return from a nested
`Execute`. A separate interactive check wrote `alpha` and appended `beta` to
the same file. These tests use injected guest input and are focused checks,
not a complete compatibility qualification.
The corresponding CopperStart guest produced the same expected control-flow
files and retired the Shell and its children. Its original harness run reported
a false failure because it required a background `Run` notice in this case;
the recorded disk and trace were rechecked after correcting that assertion.

The legacy CopperStart boot wrapper now uses public DOS vectors to register
the packet-started Shell, check the startup file, and find the initial `C:`
path. With the rebuilt 68000 Shell and paired CopperStart ROMs, guest replays
passed both `System`/`Execute` through the permanent Shell segment and an
interactive boot without a startup sequence. Both OFS and FFS startup scripts
also passed their variable, alias, dot-script, and disk-command checks.
Platform-service installation still uses CopperStart internals.

On Kickstart 3.1 and CopperStart, the proof Shell accepted a `Path` command
containing 65 directory arguments and exited. Kickstart produced the same
deduplicated listing as the original ROM Shell; the previous proof completed
the script but left only the implicit `C:` path. A separate 68000 probe exercised allocation and release
of a 301-entry CLI path using synthetic lock identities on both Kickstart 3.1
and CopperStart; real DOS locks were covered by the Path command tests. These comparisons do not remove the
remaining Shell compatibility limitations.
The rebuilt disk-loaded legacy CopperStart Shell also passed a native boot
script containing 65 Path arguments followed by a disk command.
The same final, unmodified proof ADF booted under Kickstart 3.1, accepted a
Shell command, and opened the staged `SYS:Libs/mathtrans.library` at V40 with
result 0. The library probe was supplied on a read-only DF1 helper disk.
The final proof also passed the packet-started `System()` input, handle lifetime,
asynchronous completion, and null-command checks on both Kickstart 3.1 and
CopperStart.

A later freestanding 68000 Shell comparison used the original Kickstart 3.1
Shell and the same ROM and WinUAE build for both sides. A delayed `Run` child
matched the reference for a leading output file, a leading `NIL:`, and a
`System()` caller-supplied output. In the latter case the child used a
noninteractive NIL-style stream and the caller successfully wrote to its own
handle afterward. The diagnostic image contained the same Shell HUNK as the
subsequent proof build; these focused checks do not qualify the whole Shell.
A further original-ROM comparison covered nested `Run` and a `System()` call
with interactive input and file output. The descendant used `NIL:` in the
former case and the console in the latter; the caller could still write to its
file. The current Shell matched both observations. Four repeated-worker
measurements, including nested and console-input runs, returned to the same
free-public-memory level across the measured batches.
After opening a separate console output for each worker, the current 68000
proof passed reference comparisons for a leading file, explicit `NIL:`, nested
`Run`, and `System()` with both detached and interactive input, plus the four
memory tests.
The rebuilt disk-loaded CopperStart Shell passed the 65-argument Path startup,
packet-started System/Execute, and a background command launched by NewShell.
The unmodified proof ADF booted with the original 3.1 ROM and opened the
staged library at V40 using the read-only DF1 probe.
An unbuffered legacy Shell rebuild timed out while reading the long startup
line. After the Shell requested a DOS-owned buffer for its own script input,
the same disk-loaded 68000 Shell test completed at approximately frame 35,465
under the same 96,000-frame bound. The original timeout and the trace control
are retained as diagnostic artifacts; this is a focused regression test.

The public-resident deferred `ADD` path has a separate native guest test on
CopperStart: the public segment is initially unloaded, is loaded by its first
command invocation, and is absent after removal. The test also checks the
forced-resident warning and process retirement. On the updated proof it first
checks that a non-pure file without `FORCE` returns warning 5 and error 122 and
does not create a resident. On Kickstart 3.1 the shared
Shell returns warning level 5 and `ERROR_NOT_IMPLEMENTED` for the newer deferred
and resident-alias forms, then continues and exits normally. This does not
qualify the remaining private resident replacement and alias implementations.
The rebuilt proof ADF separately passed the exact-boot V40 math-library check,
script-control replay, background `Run` replay, and a path-isolation replay on
Kickstart. The latter recorded the parent's inherited path and the child's
replacement path; its original version did not synchronize the parent's final
listing with the child's change. The same unsynchronized test passed on
CopperStart with the preceding proof payload. The updated fixture waits for a
child marker before the parent's final listing; it passed on Kickstart 3.1 with
the updated proof. A polling version timed out on the floppy-backed CopperStart
fixture even though the child subsequently produced its files. A guest snapshot
showed both Shell processes waiting while CopperStart's floppy transfer routine
was active; that single snapshot does not establish a deadlock or its cause.
The current fixture uses a reserved public Exec signal after the child closes
its marker, so it does not repeatedly poll the disk. On the updated proof's
CopperStart diagnostic, the 20,000- and 30,000-frame Chip RAM captures showed
distinct parent and child CLI path nodes and lock BPTRs. Their public lock key,
handler, and volume fields matched; this is not a guest `SameLock` result. At 30,000 frames the
parent was waiting on the reserved signal, the child was waiting on a DOS
signal, and the running task was in floppy positioning; the child had not yet
written its path marker. The captured addresses do not by themselves prove
the post-mutation behavior. A longer run of the signal fixture on that proof
subsequently completed: the child listed its inherited path, changed its own
list and signaled the parent; the parent then listed its unchanged path, and
the harness observed both processes retire. These remain focused tests, not
full Shell qualification.

Original-ROM controls show that the shared-error probe runs as a plain command
and under `Run` with a leading file redirection when `*<>` is omitted. The
corresponding `Run ... *<>` reference did not reach its entry marker. This
narrows the unresolved discrepancy but does not establish the original
parser's diagnostics or the expected candidate behavior. The
[AmigaOS DOS reference, section 15.1.1](https://developer.amigaos3.net/sites/default/files/downloads/2024-10/Amiga_ROM_Kernel_Reference_Manual_DOS.pdf)
states that explicit `*>`, `*>>`, and `*<>` error redirection were added in
AmigaDOS 45; Kickstart 3.1's DOS is V40. Their availability in this Shell is
therefore an extension rather than evidence of a V40-compatible syntax.
# Compiler isolation observation

The November 2026 Shell proof investigation found a startup regression when
the same previously working managed Shell assemblies were compiled with the
newer frozen 68000 compiler. The resulting diagnostic Shell did not reach its
prompt on CopperStart; the original Shell from the same managed inputs did.
The boot program, disk contents outside `L:Shell-Seg`, and ROM were held fixed.
The failing image and boot disk are retained under
`artifacts/shell-compiler-isolation-image-v1` and
`artifacts/shell-compiler-isolation-copper-v1`. This isolates the change to
native code generation, but does not identify a particular compiler pass.
Compiling those same inputs with `--code-size-optimizations off` also failed
to reach the CopperStart prompt (`artifacts/shell-sizeoff-copper-v1`), so
disabling that option alone does not avoid the regression. A boot trace of
the original failing image shows the Shell image fully loaded before the
Shell task disappears (`artifacts/shell-compiler-isolation-copper-v1/boot-trace.txt`).
An isolated snapshot of the subsequently modified compiler was also built
twice, once with its current register-preservation analysis and once with the
repository version from the earlier working compiler. Compiling the same
frozen managed Shell input produced byte-identical packaged Shells (SHA-256
`97a963793704be81556759a1594ecc507aad55815ae903f73e53b631144aa1f3`).
Thus that analysis change does not affect this output in the tested snapshot;
this observation does not establish which remaining compiler change causes
the startup failure. See `artifacts/shell-compiler-preservation-study-v1`.
These diagnostic images are not release or complete-Shell qualification.

A later compiler source snapshot rebuilt the proof image as
`artifacts/shell-public-continuation-proof-v13/System.adf`. On CopperStart,
the image reached the Shell prompt, passed the focused `Ask` cases, and exited
without remaining child processes. On Kickstart 3.1, the same cases matched
the original ROM Shell's prompt text, return codes, and secondary results.
See `artifacts/shell-ask-proof-v13-copper` and
`artifacts/shell-ask-proof-v13-ks`. The particular compiler change responsible
for the earlier regression remains unidentified; these checks do not qualify
the complete Shell.

The same v13 image also passed the CLI path-isolation probe on both systems:
the background child inherited the initial path and changed its own path
without changing its parent's. An unmodified-image Kickstart 3.1 boot probe
ran a Shell command and loaded the staged `SYS:Libs/mathtrans.library`
(`RESULT 0`). See `artifacts/shell-proof-v13-path-ks`,
`artifacts/shell-proof-v13-path-copper`, and
`artifacts/shell-proof-v13-exact-ks-v2`. These are focused guest checks.

The later v14 proof image includes frame-based continuation cleanup. Its
unmodified image booted on Kickstart 3.1 and loaded the staged math library;
the Kickstart control-flow probe and CopperStart broad background-command probe
also passed. Receipts are in `artifacts/shell-proof-v14-exact-ks`,
`artifacts/shell-proof-v14-control-ks`, and
`artifacts/shell-proof-v14-broad-copper`. These do not qualify the complete
Shell or force a continuation pending-slot publication failure.

