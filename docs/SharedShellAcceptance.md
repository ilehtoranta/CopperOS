# Shared Shell acceptance

Normal build integration and its fresh artifact/runtime receipts are recorded
in [Normal system build](NormalSystemBuild.md). The v22 qualification below
identifies its historical native artifact and build scripts; use the normal
build's retained manifest when auditing the current staged system.

The deliverable is one disk-loadable 68000 Shell using public Exec and DOS
interfaces on CopperStart and original Kickstart 3.1. The Shell owns its runner,
script frames, continuations, stream leases and CLI path mutations. A successful
boot or one command is not acceptance of the complete Shell.

The shared refactor is accepted for the v22 artifact below. The final audit
`artifacts/shell-v22-final-acceptance-audit.json` passes 60 checks, independently
reads the guest files for all 50 planned runtime receipts (31 Kickstart and
19 CopperStart), and separately checks the unmodified proof-image boot and
staged math library. Both runtime matrix processes terminated successfully.
The audit implementation is retained as `artifacts/audit_shell_v22_final.py`.

## Current normal-image replay

The normal CopperOS image has now passed the complete Shell replay with the
updated CopperStart FFP/loader ROM pair and original Kickstart 3.1. The
[normal-image acceptance](../artifacts/normal-full-verification-v1/acceptance.json)
independently accepts 20 CopperStart and 31 Kickstart Shell fixtures, plus an
untouched-normal-ADF full built-in/math corpus on each system. It rechecks all
31 command identities, ownership and ABI probes, original V40 comparisons,
actual guest files and clean task retirement. The command and ownership host
suites pass 1,177 tests.

Both unchanged-image boots also load the staged mathtrans.library and execute
all 17 vectors; the fresh detailed math probe agrees on 20 raw/defined-CCR
samples and passes six exception stages and library lifecycle checks. This
supersedes the earlier math-only replacement-ROM qualification. See
[Normal system build](NormalSystemBuild.md#complete-normal-image-verification-with-updated-copperstart)
for input identities, receipt locations and emulator scope. The v22 evidence
below remains the historical refactor baseline.

## Requirements and evidence

| Requirement | Required evidence | Accepted current evidence |
| --- | --- | --- |
| Public interfaces on both systems | Native linked-method audit, original-ROM execution, CopperStart execution | v22 source/artifact audit passes using the recorded compiler-source snapshot. The only linked CopperStart methods are reviewed memory, public Exec allocation and public ExecBase.ThisTask operations. |
| Shell-owned runner state | Ignored legacy context arguments, ownership and rollback tests, native entry and continuation probes | Current 65-test ownership/rollback regression passes. V22 full owner and register-ABI probes pass on both systems; their result audits bind the actual helper bytes, heap-failure records, markers and retirement. |
| CLI-owned command path | AllocVec/FreeVec nodes, duplicated locks, ordered search, child isolation and cleanup | Current host ownership regression and v22 Path, 65-argument Path, parent/child isolation and CD cases pass on both systems, including retirement. Source uses public Exec AllocVec/FreeVec and DOS DupLock/UnLock, with transactional replacement and rollback. |
| Built-ins and script execution | Command tests plus actual interpreter runs for built-ins, arguments, quoting, control flow and failure recovery | Current 1,078 command tests and 31 image-builder tests pass. V22 full corpora and 128-padding-line control-flow cases pass on both systems. Kickstart's independent same-ROM comparison matches all 57 stock-V40 shared checks plus three extension contracts; CopperStart also passes physical console reset. All 31 command identities are mapped to accepted coverage. |
| Diagnostics and command outcomes | Original-Shell controls and candidate default/explicit-error-stream tests | All four v22 Kickstart parser/service variants and both CopperStart diagnostic cases pass, including Resident 5/205, argument failure 10/118 and global listing 0/0. Current script recovery passes on both systems, and Kickstart interactive recovery passes. Actual output files/status records are independently reread by the final audit. |
| Process and stream lifetime | Foreground/background execution, NewCLI/NewShell, owned stream inheritance, child retirement and Shell exit on both systems | All four v22 Kickstart memory-lifetime variants pass with equal samples within each warmed fixture. Full owner and ABI probes pass on both systems, including heap-pressure/lower-priority retirement. Launch, owned-stream, inherited-state, explicit Prompt and EndShell checks pass on both systems; CopperStart traces report zero remaining Shell/child/background tasks. |
| Native packaging | 68000 freestanding HUNK, zero unsupported runtime sites, stack gate, source and staged-file hashes | V22 build/public-interface audit passes. Independent packaged-HUNK parsing validates all 1,697 self-relocations at two load bases and confirms absence of the named private bridge/context/queue/return symbols. |
| Booted proof image and math library | Unmodified proof boot on original Kickstart, Shell command, open staged SYS:Libs/mathtrans.library V40 | V22 passes with RESULT 0 (`shell-final-exact-ks-v22/qualification.json`). DF1 contains only the read-only test helper; the Shell and library come from the booted SYS: image. |

The final audit covers all eight requirements above. Individual build and focused
runtime receipts deliberately retain `runtimeQualified: false`; their scope is
unchanged. The aggregate acceptance uses the source, packaging, host tests,
original controls and independent runtime evidence together.

This accepts the requested public Exec/DOS refactor and tested built-in, script,
quoting, redirection, path and process behavior. It does not claim exhaustive
MorphOS language conformance or physical-hardware testing. Error redirection,
Cls and SAVE forms retained beyond stock V40 behavior have separate candidate
contracts. V40 lacks public creation APIs for deferred/alias resident extensions;
those forms return ERROR_NOT_IMPLEMENTED. CopperStart's monochrome console
implements clear/reset without additional rendition modes.

The built-in inventory is checked against `ShellInternalCommand`: all 31 names
are mapped, without duplicates, in `shell-v22-builtin-coverage-plan.json`.

| Command group | Native coverage |
| --- | --- |
| If, Else, EndIf, Lab, Skip, EndSkip, Quit | Control flow, full corpus and script recovery |
| Alias, Unalias, Set, Get, Unset, Setenv, Getenv, Unsetenv | Full corpus, control flow and child inheritance |
| Echo, Stack, Failat, Fault, Why, Cls, Ask, Prompt | Full corpus, ASK and explicit inherited-Prompt case |
| CD, Path | CD, Path add/remove/reset/search, 65 arguments and parent/child isolation |
| Run, NewCLI, NewShell, Resident | Launch, stream ownership, inherited state and resident cases |
| EndCLI, EndShell | Parent exits plus NewCLI child EndShell with an absent post-exit marker |

The explicit Prompt case uses the existing `new-shell-prompt` fixture on both
systems. NewCLI's child executes EndShell under FailAt 21, then has a forbidden
output marker: an unknown-command error alone cannot satisfy this exit check.
The earlier Kickstart NewCLI fixture had already been captured before this
coverage adjustment. Its separate `shell-final-new-cli-exit-alias-ks-v22`
replay passes the discriminating EndShell check. Explicit Prompt inheritance
passes in `shell-final-new-shell-prompt-ks-v22` and its CopperStart counterpart.
These verification changes do not change the packaged Shell.

CopperStart's v22 NewCLI case now passes that discriminating EndShell check
(`shell-final-new-cli-copper-v22/exit-alias-result-audit.json`): the actual
fixture sets FailAt 21, the post-exit marker is absent, the child completion
file is present, and child and parent Shell tasks are fully retired.

The v22 ordinary-Run lifetime fixture also passes on Kickstart, with three
equal public-memory measurements of 1,528,432 bytes after warmup and repeated
worker retirement. `shell-final-run-lifetime-ks-v22/memory-result-audit.json`
checks the actual guest binary measurement record against its receipt; other
launch/stream lifetime variants remain separate requirements.

The v22 owned-output Run and NewShell lifetime fixtures also pass. Their
actual guest records contain three equal samples of 1,527,904 and 1,528,064
bytes respectively (`shell-final-run-owned-lifetime-ks-v22` and
`shell-final-new-shell-lifetime-ks-v22`, each with `memory-result-audit.json`).
The System lifetime variant remains separately checked.

All four current v22 Kickstart lifetime variants are now accepted in
`shell-v22-lifetime-audit.json`. The System case records 1,528,520 bytes in all
three samples. The audit independently reads each actual 12-byte guest record,
checks equality within its warmed fixture and verifies its input/completion
markers. These fixtures do not export an explicit final Shell-exit marker;
owner, ABI and command cases verify Shell exit separately. No equality between
different fixtures is claimed.

Current-image receipts are independently checked against actual fixture/result
files, staged Shell bytes, guest observations and launch-ROM identities in
`shell-v22-final-acceptance-audit.json`, with
`allGoalRequirementsSatisfied: true`. The earlier
`shell-v22-completed-receipt-audit.json` remains a partial historical snapshot.
The final audit also rechecks all 46 device sources against the live checkout,
the frozen managed ROM inputs and the main/extension ROM hashes.

## Current reproducible artifact

The November 2026 v22 proof is
`artifacts/shell-public-wait-backoff-proof-v22/System.adf`:

- ADF SHA-256: `758f16c0d8b97bc8bf7aef73ec21440928759a4ef90de383ff18c36ca0a6682c`.
- Packaged Shell: 187,216 bytes, SHA-256
  `9c1489e7067a9ccb3b9b47d94cd8fe7a2b511b2b6c2d6af9fa839a656611bf72`.
- Staged math library SHA-256:
  `ada9a6d6f07019a7a2c56d2a5c7721fa00e974b679364fea4fbc66db83ee9000`.

Recheck sources, frozen managed inputs, native reports, linked service methods
and staged files with:

```powershell
python tools/ImageBuilder/audit_shared_shell.py `
  --manifest artifacts/shell-public-wait-backoff-proof-v22/native-manifest.json `
  --image artifacts/shell-public-wait-backoff-proof-v22/System.adf `
  --output artifacts/shell-public-wait-backoff-proof-v22/public-interface-audit-current.json
```

The audit checks individual linked methods, because the legacy DOS memory
transport type also contains private services that must not become reachable.
It does not establish guest behavior or replace the independent HUNK relocation
and execution probes.

`hunk-relocation-audit.json` independently parses the packaged v22 HUNK,
checks its 1,697 unique aligned self-relocation sites and addends, and relocates
the code at `0x20000` and `0x100000`. It also verifies that the symbol inventory
contains none of the named private DOS context, bridge, launch, queue-task or
process-return routines. Native execution remains separately verified through
the two operating systems' loaders.

## Investigation history (superseded status)

The following records explain earlier failures, controls and corrections.
References to pending work or a current version in this history describe that
stage of the investigation. The accepted v22 status and final audit above
supersede those intermediate status statements; failed or cancelled runs are
not acceptance evidence.

### Original-Shell diagnostic controls

`observe_diagnostics31.py --original --plain` removes the custom Shell segment
and boot registration from a disposable image. It preserves the source image,
ROM and emulator and records their hashes. `--resident-errors` includes
Resident-owned reporting. `--global-errors` omits LIST, while `--list-result 0`
or `--list-result 10` installs a deterministic external LIST command.

The original Shell reports a missing Resident file on selected stdout and
publishes 5/205. Both no-argument global-listing built-ins publish 0/0 for a
missing LIST and fixtures returning 0 or 10. Their text includes the missing
command or failed-return-code message when appropriate. This supersedes the
earlier warning-level-5 assumption in the listing implementation.

The candidate uses the same default diagnostic destination. Explicit `*>`,
`*>>` and `*<>` are extensions to V40 syntax and have separate candidate tests.
Resident listing data and LIST's ordinary data remain on standard output.

Native observations are under `artifacts/shell-public-global-diagnostic-original*`
and `artifacts/shell-public-service-diagnostics-*-ks-v12`. CopperStart service
diagnostic runs use the matching `*-copper-v12` directories. A missing receipt
or a failed verifier is incomplete coverage, not a pass.

The subsequent `shell-resident-parse-original-v1` control establishes that
`Resident one two three` prints `wrong number of arguments` and publishes
10/118. The v12 candidate reproduced the return codes but omitted the message;
v13 removes the blanket Resident exclusion from Shell-owned ERROR diagnostics.
Resident's own admission/removal diagnostics remain WARN and are not duplicated.
The current native matrix is under `artifacts/shell-resident-parser-*-ks-v13`
and `artifacts/shell-resident-parser-*-copper-v13`.

### Complementary built-in corpus

`shell_builtin_fixture.py` supplies a shared guest script for remaining command
coverage: Echo quoting/slicing/NOLINE, Stack, FailAt, Fault, Cls, local variables,
Unalias, Why, real ENV:/ENVARC: files, unnamed Skip with EndSkip/Lab, and nested
Execute/Quit followed by a console command. Its helpers only create DOS assigns
and capture public CLI results; the proof Shell executes the commands.

Run the ROM control with `observe_diagnostics31.py --original --builtins` and
the candidate with `--builtins`. CopperStart uses
`qualify_copperstart_run.py --case builtins` and the v8 input probe, which types
the post-script Quit check through the console. `--observe-only` collects a
candidate baseline without claiming its output contracts passed.

The reference profile omits Cls and the retained SAVE switches. Candidate Cls
and persistence are extension checks, while version-specific shared behavior
is established by the original ROM capture. The
[AmigaDOS command reference](https://wiki.amigaos.net/wiki/AmigaOS_Manual:_AmigaDOS_Command_Reference)
documents the common commands and the NAME,STRING/F SetEnv template. The test
does not infer a V40 SAVE-switch contract from the newer DOS API.

The initial exploratory nested-Quit fixture was cancelled because it put its
completion marker in the caller tail discarded by Quit. The corrected fixture
checks that tail is absent and obtains completion from a later console command.
Its input and cancellation record are retained under
`artifacts/shell-builtins-observe-copper-v13b`; it is not qualification evidence.
The next observation, `artifacts/shell-builtins-observe-copper-v13c`, reached
the end of the main script but stopped in the keyboard harness because its
key map lacked digit 4. The v6 probe includes all digits and also supports
post-EOF result capture. Neither exploratory run qualifies the Shell.

The original-backed fixes packaged in v14 cover Stack's full output text,
DOS TRUE normalization for SAVE, BACK and CLS RESET, WHY's WARN result with
the previous secondary error, forward SKIP stopping at ENDSKIP (5/0), and
missing-label/end-of-file SKIP consuming the script (10/205). Standalone
ENDSKIP returns 0/0 and does not discard an unrelated IF control record.
The current host receipts are `shell-builtin-reset-host-v1.log` (1,064 tests),
`shell-builtin-reset-ownership-host-v1.log` (65 tests), and
`shell-builtin-setup-image-host-v1.log` (31 image-builder tests).

Backward-search controls initially stalled before reaching their markers.
The fixture now assigns ENV:, ENVARC: and T: using public AssignLock calls
before starting the script. This avoids missing-volume requesters for an
undefined variable or the original Execute's temporary file. With the locally
owned original Execute installed, the named BACK control completes under the
original ROM (`shell-builtin-skip-back-named-original-v2`). The earlier stalls
are fixture failures and do not establish a different SKIP contract.

The v14 candidate's `bi-skip-wrong` marker exposed a separate implementation
regression. Native SKIP tested the interactivity of the input inherited by
commands, which can remain the console while Execute's work file is the
runner's command source. V15 uses the script frame's Interactive flag, as
the generic implementation does; TryReadScriptLine resolves the Shell-owned
source. The v14 corpus otherwise matches the checked text, return codes,
secondary errors and environment-file contracts, including CLS RESET.
This failure is retained in `shell-builtins-shared-ks-v14/observations.json`.

V15 passes the full complementary Kickstart corpus, including environment
persistence, diagnostics, ENDSKIP, nested QUIT and actual Shell exit
(`shell-builtins-shared-ks-v15/qualification.json`). The superseded v14
CopperStart run was cancelled after the Kickstart regression had already
been isolated; its cancellation receipt records that reason. It is not a pass.

The completed original controls `shell-builtin-skip-lab-empty-original-v2`,
`shell-builtin-skip-lab-command-original-v2` and
`shell-builtin-skip-lab-original-v2` establish a V40 detail not captured by
the newer manual's next-LAB description: an omitted SKIP target matches a
bare LAB, passes named LAB statements, and reaches EOF with 10/205 if no
bare LAB or ENDSKIP exists. A standalone bare LAB succeeds with 0/0. The
guarded BACK control `shell-builtin-skip-back-original-v7` reaches the
BACK command and then returns 10/205 without executing any target marker,
consistent with its named-only source. The current source implements those
rules. The new interpreter regression also verifies that a matched LAB is
consumed instead of being executed again after a backward jump.
Host verification is `shell-unnamed-lab-host-v2.log` (1,069 tests); v16 native
packaging completed but its audit found changed compiler inputs. V15 receipts prove that artifact,
not these subsequent source changes.

`shell-builtin-lab-command-original-v3` also establishes 0/0 for repeated,
quoted and multiword LAB markers. LAB now accepts source markers without
runtime registration or per-label allocation. The final source host check
is `shell-lab-marker-host-v2.log` (1,069 tests).

The v16 and v17 current-checkout audits detected independent compiler edits
after the input snapshots were built. V19 instead uses
`artifacts/shell-builtin-toolchain-source-v19`, a double-read-checked copy of
481 compiler/SDK source files with individual hashes in `snapshot.json`.
The receipt SHA-256 is
`2f23bc1016c2a4a9a0e41580ac4068a8a9ae5818deb67420942f971f12dbf6e2`.
The build hashes SDK targets, metadata and embedded JSON inputs as well as C#.
This pins a toolchain for reproducibility; it does not assert that a concurrently
edited compiler checkout remains identical. Shell and CopperStart producer
sources are still checked against their live paths.

The v18 isolated build omitted the SDK publish-target directory and produced
no Execute native report. That unsuccessful build is retained; the corrected
snapshot includes the SDK targets. V19 passes the native gates and source,
linked-method and staged-file audit. Its expanded corpus combines WHY success,
ENDSKIP status, LAB marker cases, named BACK, unnamed LAB, missing/named-only
EOF skips, unnamed BACK termination, nested QUIT and console recovery. Runs
are under `shell-builtins-shared-ks-v19`, `shell-builtins-shared-copper-v19`
and `shell-builtins-exact-ks-v19`; completion is not assumed from their presence.

V19's expanded candidate corpus exposed SKIP executing inside an inactive IF
branch. The earlier control-flow matrix used ordinary commands in those
branches and did not cover this interaction. V20 allows only IF/ELSE/ENDIF
balancing while a branch is discarded; SKIP, LAB and ENDSKIP are suppressed.
The new regression fails four cases before the fix
(`shell-inactive-if-regression-before.log`) and the complete 1,073-test suite
passes afterward (`shell-inactive-if-host-v1.log`). The final ownership
regression passes all 65 tests (`shell-final-label-ownership-host-v1.log`).
The stock expanded corpus completes under the original ROM with the original
Execute (`shell-builtins-original-ks-v19`). V20 native replay and the independent
`compare_shell_builtins.py` report both pass on Kickstart
(`shell-builtins-shared-ks-v20/qualification.json` and `original-comparison.json`).
The latter verifies 57 common outputs/status pairs, the three extension checks,
the stock-Shell startup, original Execute's hash, and identical helper/subscript
payloads. These establish the tested interaction on the candidate under Kickstart.

The superseded v19 CopperStart run was cancelled after the v20 Kickstart corpus
passed; its reason is recorded in its cancellation receipt. Current verification
uses `shell-builtins-shared-copper-v20`, `shell-builtins-exact-ks-v20` and the
`shell-final-*-ks-v20` / `shell-final-*-copper-v20` matrices. The Kickstart matrix
starts with control flow (128 padding lines), path isolation, Path, CD, System,
Resident, NewShell and NewCLI. The CopperStart matrix also includes 65 Path
arguments, ASK, script recovery and parser/service diagnostics. A running
process, existing output directory or intermediate disk snapshot is not a pass.

V21 changes the shared console writer so `Cls RESET` emits the public console
reset sequence ESC c; ordinary `Cls` still emits form feed. The native and
generic platforms use the same writer. Host tests cover boolean normalization,
invalid input and failed writes (`shell-console-reset-host-v2.log`, 1,078 tests).
The image-builder suite also passes (`shell-console-reset-image-host-v2.log`,
31 tests). The current linked-method/source/staged-file audit passes with
`sourceHashesMatchCurrent: true` in `shell-public-console-reset-proof-v21/`.

The v21 Kickstart built-in corpus and original comparison both pass
(`shell-builtins-shared-ks-v21`), as does the native continuation-owner probe
(`shell-final-owner-ks-v21`). The latter checks allocation failure, pending and
completed waits, ignored legacy context registers, lower-priority workers,
mixed record ownership and parent exit. It is a standalone probe containing
the shared Shell source, not a claim that its fixture-only legacy allocation
helpers are linked into the packaged Shell.
Its independent `owner-result-audit.json` checks the full `0x3fff` stage mask,
the genuine allocation failure (32 reserved blocks, error 103, no published
continuation), installed probe bytes, result-image hash and final Shell exit.

The unmodified v21 image boots on Kickstart and opens the staged V40 math
library with `RESULT 0` (`shell-builtins-exact-ks-v21`). Current-image core and
lifetime replays are separate and remain open until their own verifiers pass.

The CopperStart v20 built-in replay exposed an observation problem: an optional
standard-ADF snapshot during active disk DMA found only ten valid sectors on
one track. Observer v7 records this intermediate failure and continues; final
disk export is still strict. Observer v8 additionally captures the console
immediately after the reset test, before typing follow-up commands. Its emulator
dependencies are byte-identical to v6; this change does not alter emulation or
waive the final disk/output checks. CopperStart v21 acceptance remains pending.

The v20 CopperStart continuation-owner run terminated without completing its
script (`shell-final-owner-copper-v20/failure.json`); it is not accepted. Its
allocation-failure diagnostic was present but its final stage mask was absent.
The same native HUNK passes all stages on Kickstart. A fresh observation-only
replay uses v9 to retain RAM and task state before final disk export. No cause
is inferred from elapsed time. Separately, the broader v20 CopperStart matrix
stopped when v6 attempted an intermediate ADF export during a track update
(`shell-final-broad-copper-v20.log`, track 20.1, ten valid sectors). That
observation failure does not establish a Shell failure or a successful run.

The v7 built-in replay subsequently passes on CopperStart with a strict final
ADF export, all checked output/status files, Shell exit and no remaining child
or background Shell (`shell-builtins-shared-copper-v20-observer-v7`). This is
v20 evidence; the v21 physical console-reset replay is separate.

V9's incomplete-state capture reproduces the owner failure after 30,000 frames.
At 10,000 and 20,000 frames the priority-zero owner is current and its
priority-minus-three continuation worker remains ready. Public wait/timer
cleanup appears on the saved stack; the capture does not by itself establish
which call prevents progress. The independent public Delay/priority probe then
passes all four cases (`shell-final-delay-priority-copper-v21-v9`, stage mask
`0xf`), so a general Delay failure is not established.

`CopperOSContinuationNoOomControl=true` builds a diagnostic owner variant that
omits deliberate heap exhaustion and expects `0x3bff`, rather than the full
`0x3fff`. Its result cannot qualify allocation-failure handling or replace the
full ownership probe. The native input pool, build gates and source/layout
limits are recorded under `shell-final-owner-no-oom-control-v1`; its CopperStart
replay is `shell-final-owner-no-oom-copper-v20-v9`.

All nine v21 Kickstart core/lifetime cases pass. The repeated-process public
memory samples are exactly equal within each case: Run 1,528,320 bytes,
owned-output Run 1,527,792 bytes, NewShell 1,527,952 bytes, and asynchronous
System 1,528,408 bytes. These are separate warmed fixtures, not cross-case
memory comparisons. The v21 extended Kickstart matrix and remaining
CopperStart matrix continue in fresh artifact directories.

The v21 CopperStart built-in replay completes its script and all process cleanup
but fails physical console-reset acceptance: `RESET SENTINEL` remains visible
after ESC c (`shell-builtins-shared-copper-v21/failure.json`). The console core
previously ignored both form feed and RIS. Two new bitmap/input regressions
fail before the CopperStart console correction; all 20 console tests pass after
it (`shell-copper-console-reset-before.log` / `shell-copper-console-reset-after.log`).
The correction clears and homes the display while retaining queued keyboard
input. It does not add rendition modes that the monochrome console lacks.

`shell-console-reset-rom-v3` rebuilds only the Devices managed input against
the frozen ROM input pool. Its main ROM SHA-256 is
`3e3f6b06efbc339b8b1ef697280ecfcde7bf123e890600173a6725f7de33fb3e`;
the extension remains byte-identical. The inherited 19 ROM fault sites and
string intrinsic are unchanged; the packaged Shell's separate strict zero-site
audit still passes against current sources. Physical clear/reset and full
built-in replays use fresh `*-rom-v3` directories. Their completion is pending.

The owner control without heap exhaustion completes with the distinct `0x3bff`
mask. This narrows the investigation but does not prove an allocation bug:
its native layout also differs. The public Delay probe's optional
`CopperOSDelayHeapPressureProbe=true` variant repeats its four cases after
exhausting and releasing public memory, records before/after memory values and
the `HEAP` marker, and contains no Shell polling. Its build gates pass in
`shell-final-delay-heap-pressure-v1`; its CopperStart replay remains separate.

The first ROM-v3 console-clear guest capture proves that the sentinel is erased,
the completion marker is visible and Shell/children retire. Its host verifier
incorrectly required a background Run notice for the new console-only case, so
it has no qualification receipt. `requires_run_notice` now explicitly names
the five cases that capture that notice; the existing host regression includes
both console-only cases. Fresh clear/reset replays use `*-rom-v3-v2` directories.

The initial pressure-probe build reused a diagnostic control output path. The
mistaken outputs are retained under `shell-final-delay-heap-pressure-v1/build-path-error`.
Recompiling the saved control command restored both its HUNK and compatibility
report to their recorded hashes. The pressure build now uses explicit absolute
paths and verifies that the reachable exhaustion routine and HEAP marker are
present. Its fresh replay is `shell-final-delay-heap-pressure-copper-v21-v2`.
Neither the packaged Shell nor ROM inputs were affected by the path mistake.

Both fresh physical console cases pass on ROM v3: `console-clear` and
`console-reset` under `shell-final-*-copper-v21-rom-v3-v2`. The captured display
contains SCRIPT DONE without the sentinel and the Shell and all workers retire.

The pressure-only public Delay probe completes with mask `0xf`, but its raw
record establishes the distinction its aggregate mask intentionally permits:
the one-tick delay returned before its lower-priority child ran. The 25-tick,
50-tick and direct-timer cases observed child progress. Its result audit records
49 reserved blocks and public memory 1,563,748 before / 1,570,608 after pressure
(unused libraries may be reclaimed). A passing aggregate is not proof that
Delay(1) yielded. The native continuation loop previously assumed repeated
one-tick delays would let every ready child retire.

V22 backs off pending continuation polls from one tick to a bounded 25-tick
interval, clipping each delay to a finite caller's remaining budget. This uses
the same public DOS Delay interface on both systems, preserves IoErr and does
not add a CopperStart adapter. Zero and one-tick finite waits retain their
budgets. The existing full native owner probe is the regression: it failed
before the correction after heap exhaustion with a ready priority-minus-three
child. The 1,078 command tests pass (`shell-wait-backoff-host-v1.log`); native
owner replay and v22 packaged-image acceptance remain open.

The v22 build passes the source/link/staged-file audit with current hashes,
zero unsupported native runtime sites and the same seven allowed public
support methods. The ownership regression also passes all 65 tests
(`shell-wait-backoff-ownership-host-v1.log`). The full owner HUNK is 220,012
bytes, SHA-256 `f6fe18375a8b62689a0ad69be9bc111b115eba64fb0f6a316b268e896ec8170c`.
It runs against both the exact v20/ROM-v2 failure baseline and the current
v22/ROM-v3 combination. The unmodified v22 Kickstart proof/math replay is
separate (`shell-final-exact-ks-v22`). All remain pending until inspected.

Both CopperStart full owner replays now pass with `0x3fff`, genuine allocation
failure 103 and no runner-fail marker: `shell-final-owner-backoff-copper-v20-baseline`
and `shell-final-owner-copper-v22`. Their independent result audits bind the
full native HUNK, input image, ROM and final disk hashes. This restores the
previously failing lower-priority wait without weakening the full owner mask
or substituting the no-OOM diagnostic control. The current v22 CopperStart
command matrix and full built-in corpus run separately on ROM v3.

The unmodified v22 proof also boots on original Kickstart 3.1, executes its
command and opens `SYS:Libs/mathtrans.library` at V40 with `RESULT 0`
(`shell-final-exact-ks-v22/qualification.json`). The full command matrices,
current native Begin/Poll ABI probe and current original-Shell built-in
comparison still need their own runtime receipts.

The completed v21 baseline now includes all 17 extended Kickstart cases and
the independent original ASK comparison (`shell-final-ask-ks-v21`). The
CopperStart v21 remaining matrix also passes broad Run, NewShell, NewCLI, ASK,
script recovery, parser diagnostics and service diagnostics. Its full built-in
corpus passes after the console correction on ROM v3
(`shell-builtins-shared-copper-v21-console-rom-v3`). These are v21 receipts;
v22 has a separate 29-case Kickstart plan
(`shell-final-ks-v22-matrix-plan.json`), current CopperStart matrix, ABI probe,
full owner probe and full built-in replay. No earlier image is substituted for
the current runtime checks.

The current v22 CopperStart full built-in corpus now passes
(`shell-builtins-shared-copper-v22`). Its result audit checks all 60 candidate
contracts, matches the 57 common output/status values from the stock V40
capture, verifies the physical reset sentinel is erased and confirms that
the Shell and all child/background Shells have retired. This cross-system
value check supplements the separate same-ROM original/candidate comparison
still required for the current Kickstart corpus.

The current full owner probe also passes on original Kickstart
(`shell-final-owner-ks-v22/owner-result-audit.json`, `0x3fff`, allocation error
103 and Shell exit). Launch metadata binds the actual original ROM and WinUAE
hashes to the fixture and exported result image. The current 195,124-byte
Begin/Poll/Park ABI probe passes on both systems
(`shell-final-abi-ks-v22` / `shell-final-abi-copper-v22`), including absent,
opaque and address-four context arguments, preserved caller state, real script
output and retirement. Their result audits check the actual embedded helper
and final disk, not only a host summary.
