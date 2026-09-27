# Native DOS admission into the production boot owner

Updated: 2026-09-09. This checkpoint advances the production runtime dependency
behind CC05/CC09. No command, profile, PURE gate or goal stage is promoted here.
Shipping remains **0/200 commands and 0/246 profiles**.

Current checkpoint: attempt 31 passes the combined native MakeLink run, parent
return, real Exec child removal, and absence of the child's native DOS process
record. Native publication supplies a DOS process-return entry to public AddTask;
that entry uses DOS Exit cleanup before terminal public Exec removal. Attempt 30
remains the retained failing control for the process-record audit. This proves
normal return in this scenario, not exact allocation balance, deferred cleanup,
exit callbacks, BCPL publication, resident reuse, or complete Shell boot.
No shipping or PURE gate is promoted.

2026-09-10 follow-up: attempt 32 also passes public CLI lookup retirement.
Before yielding the completed child, the test enumerates bounded `MaxCli` /
`FindCliProc` results and requires exactly one CLI number to resolve that child.
After removal, that same number resolves to zero. This proves observable lookup
behavior, not raw slot clearing: `FindCliProc` also rejects invalid task entries.
Exact registry-slot and allocation-balance checks therefore remain open.
The [attempt 32 receipt](../../../artifacts/copperstart-makelink-production-boot/attempt-32-command-public-cli-retirement/runner-receipt.json)
records build/test exit 0 and unchanged observed sources/inputs (1/1 test).

Attempt 33 closes that raw-slot gap for this single-child scenario: the first
`rn_TaskArray` slot contains the child's Process message port before retirement
and zero afterward. The RootNode and first array addresses remain unchanged,
and public MaxCli still validates the registry after removal. All preceding
command, parent-return and native process-record assertions also pass (1/1).
See the [attempt 33 receipt](../../../artifacts/copperstart-makelink-production-boot/attempt-33-command-cli-slot-retirement/runner-receipt.json).
This does not qualify extended CLI ranges or allocation balance. Source review
also identifies a separate reaper ordering concern: ExecTaskMemoryCore.Reap
initializes the task's memory-list header after freeing its entries, although
DOS places the Process allocation itself in that list. A focused ownership test
and correction remain required before claiming safe complete reclamation.

2026-09-10 reaper correction: the ordering concern is now reproduced and fixed.
Exec detaches the task list and snapshots/invalidates separate CSTK ownership
before freeing entries; it no longer initializes or probes the freed Task.
The poisoning regression plus existing task tests pass 18/18; the same compiled
tests with the preceding Exec assembly fail precisely that regression (17/18).
Attempt 34 passes the combined command/CLI/retirement scenario using the new
public Exec reaper. The DOS HUNK remains unchanged; this is not a native Exec
recompilation claim. See [reaper checkpoint](../../../artifacts/exec-task-reap-ownership-20260910/checkpoint.json).
Exact allocation balance, callbacks, deferred cleanup and resident reuse remain
open; no command or PURE gate is promoted.

Attempt 35 adds public `AvailMem(0)` checkpoints with driver/config allocations
held at both points. It fails: free bytes fall from 8,971,328 to 8,938,288, a
33,040-byte deficit, despite successful command, task removal and CLI cleanup.
The [attempt 35 receipt](../../../artifacts/copperstart-makelink-production-boot/attempt-35-command-memory-balance/runner-receipt.json)
records build exit 0, test exit 1 and unchanged observed inputs/sources. The
strict balance assertion remains in place. That deficit matches the rounded
DOS Process/32768-byte stack allocation (33016) plus its 24-byte MemList, but
this size match is a diagnosis lead, not an allocation-identity proof. Trace
Exec's deferred retirement barrier and exact retained spans next; do not bypass
its scheduler/stack safety checks merely to balance the test.

Attempt 36 identifies the retained owner through read-only diagnostics. The
pending retirement holds MemList [2591280,2591304) and Process allocation
[2558264,2591276), matching the deficit after allocator rounding. Its captured
storage is valid and unchanged, and frame pointers are clear; continuation
scanning fails. Parent USP/A7 are 520188, but ISP/MSP are 1024 while the public
Task bounds are [1024,520192). FrameIsClear scans all nonzero banks to the same
upper bound, so these boot supervisor banks cause scanning across the broad
boot memory range. This is not evidence that the child's own stack is executing.
Next establish the boot supervisor-bank ownership/bounds and identify the exact
blocking word before changing the scan contract. Keep the safety gate intact.
See [attempt 36 receipt](../../../artifacts/copperstart-makelink-production-boot/attempt-36-command-retirement-owner/runner-receipt.json);
build succeeds, the strict memory test still fails, observed inputs are unchanged.

Attempt 37 identifies the exact false match: ISP/MSP start at 1024 and scan the
child pointer 2558280 at address 519244, outside the system stack [0,1024).
The boot owner now supplies that separate range only while synthetic Exec owns
the session and the published system bounds still match. Retirement scans
supervisor banks within those bounds; user stacks, unknown banks, pointer
overlaps and in-range references retain their safety checks.

Attempt 38 passes: free memory is 8,971,328 bytes at both checkpoints and the
deferred-retirement owner list is empty. Existing command/task/CLI assertions
also pass. The component suite passes 24/24, including six positive/negative
stack-bound cases. The first component build's missing-import failure is retained.
See [attempt 38 receipt](../../../artifacts/copperstart-makelink-production-boot/attempt-38-command-system-stack-bounds/runner-receipt.json).
This proves balance for one bounded session, not repeated/concurrent resident
execution, all stack arrangements, callbacks, or deferred DOS providers.

Attempt 41 passes two consecutive command sessions in one boot/library instance,
using different target/alias names. Each independently verifies command effects,
task/CLI removal and inner memory balance. A new outer checkpoint also requires
the same free-memory baseline after freeing each driver/config allocation.
The current-source runner records build/test exit 0 and unchanged observed
inputs/sources. See [attempt 41 receipt](../../../artifacts/copperstart-makelink-production-boot/attempt-41-command-two-sessions-current/runner-receipt.json).
`--sessions 2` reproduces this mode; default remains one session. Each command
is freshly LoadSeg/UnLoadSeg'd, so this does not meet the same-SegList PURE gate.

Attempt 39 retains a transient unrelated graphics compilation failure; that
source was not changed by this work. Attempt 40 independently passes the same
two-session test against the complete production assemblies from attempt 38,
with a separate prebuilt receipt and explicit loaded-assembly hash checks.
It is historical-build evidence, not a replacement for attempt 41's current build.

Attempt 42 adds a compiled `NP_ExitCode` callback through public CreateNewProc
tags, with the result-record address passed as `NP_ExitData`. Both consecutive
sessions execute it exactly once, record the successful child's zero return
code, retire the child and recover inner/outer memory baselines. The new driver
HUNK has SHA-256 `9f52ca81f83d6f9edcee9fe0d33768c58ef022f18bdb0ade75e3b7a6607ecdec`.
See [attempt 42 receipt](../../../artifacts/copperstart-makelink-production-boot/attempt-42-command-native-exit-callback/runner-receipt.json)
and its per-session callback observations. Build/test exit 0 and unchanged
observed inputs/sources are recorded. This qualifies successful normal-return
callbacks only; nonzero codes, replacement results, explicit Exit, deferred
cleanup and shared-image concurrency remain required checks.

A separate CC06 probe now invokes two MakeLink processes from one loaded
segment. The verifier reports distinct tasks `2148712` and `2185728`, shared
segment `545849`, returns `0` and `20`, balanced per-process allocations, no
instruction-image writes, and matching parser/free-argument ownership. The
hash-bound report is
[workbench-makelink-same-seglist-20260911.json](../../../artifacts/workbench-makelink-same-seglist-20260911.json)
and the verifier is
[verify_workbench_makelink_same_seglist.py](../../../tools/Commands/verify_workbench_makelink_same_seglist.py).
This is a disposable derivative probe: it intentionally omits resident
registration/removal and therefore records `residentRemovalQualified=false`,
`fullCommandQualified=false`, and `pureAdmission=false`. It does not close the
production same-SegList, active-use, replacement, deferred-load, task-death,
failure-overlap, or per-command purity gates.

Evidence: [attempt 31 receipt](../../../artifacts/copperstart-makelink-production-boot/attempt-31-command-native-process-return/runner-receipt.json)
records build/test exit 0 and unchanged observed sources/inputs. The new native
DOS image is 680096 bytes, SHA-256
`64c100d23012526df36f7af0950be0aefe48a33f8d4b4684794d772b67bc2f76`.
The integration build retains one NU1902 dependency warning; the test passes 1/1.

## Intended execution path

The existing application-session boot owner supplies real Exec services and its
initial Task. `AmigaBoot.NativeDos.cs` loads the identified native DOS HUNK and
invokes its exported system installer. DOS initializes and registers itself
through production Exec. Public `OpenLibrary` must then resolve that installed
library; neither a forced base pointer nor the compatibility DOS gateway is an
acceptable substitute.

`DosServices.NativeVolumes.cs` publishes the existing configured-volume ports
through native public DOS list operations. It reuses the production filesystem
and packet queues described in
[the preceding integration checkpoint](copperstart-filesystem-integration.md).
The initial Task is a legitimate caller of these list operations; it is not
rewritten into a Process.

The [compiled session driver](../../../tools/Commands/CopperStartBootSessionLauncher/README.md)
then uses public `CreateNewProc` to create a real child with a CLI and owned
streams. That child loads MakeLink with native `LoadSeg`, calls `RunCommand`,
unloads it, deletes the original target and reads the alias through DOS. The
integration also checks the host filesystem independently. It waits for child
retirement before freeing the driver or shared result buffer.

This uses the full production emulator. The isolated test assembly contains
`tools/Commands/CopperStartBootIntegration/NativeDosBootIntegrationTests.cs`;
it does not compile a replacement subset of the emulator or install fixture
Exec/packet gateways. Instruction observation is read-only. Its bounded
application session is not a disk startup or the existing Shell command loop.

## Diagnosed failures retained as evidence

Reports are retained under
`artifacts/copperstart-makelink-production-boot/` in separate attempt directories.
Earlier failed results must not be overwritten or treated as successful runtime
qualification.

| Attempt | Observed result and implication |
| --- | --- |
| 01-02 | Integration scaffold compilation failures; no native execution. |
| 03-04 | Native DOS installer returns zero. Instruction trace proves CLI registry creation and validation succeed; allocation/free observations alone did not identify the failure. |
| 05-07 | Native initializer, binding and fast validation succeed, but the returned library has zero successor/predecessor links. The library continuation actually executes in attempt 07. Production application boot omitted its registration, and its address also collided with `RawDoFmt` at `0x00F08400`, allowing the formatter registration to replace it. |
| 08 | Giving the library initializer its own continuation at `0x00F08410` allows DOS installation to succeed. Public `OpenLibrary` still fails to resolve the installed image. |
| 09 | Read-only vector entry trace shows Exec enters the installed DOS Open vector with A6 equal to ExecBase (`0x00F10000`), while the library expects its own base (`0x0026DEC0` in this run). The Open callback returns zero. This is a production callback ABI defect; command execution has not yet been reached. |
| 10 | After correcting callback A6 setup/restoration, installation, native public `OpenLibrary`, native-vector checks and configured-volume publication pass in the same production application session. There is still no command execution in this installation-stage result. |
| 11-12 | The driver returns before creating a child. A 45-instruction trace proves its export transports the valid configuration pointer correctly, but seven generated zero-initialization instructions overwrite D0 before the method reads that argument. `Valid` consequently receives a null pointer. No command or filesystem packet executes. |
| 13 | The unchanged driver rebuilt with the corrected compiler accepts its configuration and calls native `CreateNewProc`, which returns a real child with IoErr 0. The parent returns to a busy loop; the equally prioritized child does not execute. This identifies a missing yield/join in the driver, not another demonstrated native DOS installation failure. |
| 14 | The revised driver allocates a signal and invokes public `Wait`, but no child executes within the bound. Exec's real Wait dispatcher is reached; `ExecSignalServices.BeginWait` selects its compatibility branch because the session is not ROM-owned. That branch returns a signal immediately instead of blocking. The missing production integration is per-task signal ownership and suspension through the existing application-session scheduler after native DOS admission. |
| 15 | Native-admitted Wait blocks once and requests dispatch at the existing instruction boundary. The scheduler finds Ready child `0x002703F0` but rejects it because no complete CPU context is registered. Child entry, command entry and filesystem packet counts remain zero. Native DOS constructs its task through direct shared `ExecTaskCore.AddTask`, bypassing the public Exec registration owner. This is a production publication dependency; the test must not register a context or fabricate a Process to bypass it. |
| 16 | Installation-stage rerun after the signal-result correction passes native DOS installation/public OpenLibrary readiness with 178,240 bootstrap instructions and unchanged observed sources. It uses the original identified DOS HUNK; it does not create a child or execute a command. |

The [attempt 15 result](../../../artifacts/copperstart-makelink-production-boot/attempt15-command-native-wait/result.json)
has SHA-256
`3dc2af1c97e40da175e17020d0804c08577ae4b09fd2d9a8100339af773e22b4`.
This failed run establishes blocking, not a completed wait/wake cycle. The native
DOS and command images are unchanged. Process construction currently also
publishes Ready before finishing DOS state and streams; admitting arbitrary
ready-list entries would risk running a partially initialized child. The next
fix belongs at the DOS prepare/commit/rollback boundary and public Exec owner,
with one cleanup owner and preserved BCPL D1 startup data.

The native admission boundary retains an unresolved call's image and borrowed
inputs until whole-machine reset. It cannot free them and continue using a
faulted stack. A returned, unpublished failed installer may release its owned
image allocations. Reset is not evidence of graceful library expunge.

The callback fix saves the caller's A6 on its real guest stack and restores it
at the common library continuation. Open/CloseLibrary and Open/CloseDevice use
the same frame contract; the native readiness run specifically exercises the
library calls. Pending AUTOINIT state is cleared alongside MakeLibrary state at
both existing reset owners. Other unrelated callback-address collisions found
in the wider boot source were not changed or qualified by this checkpoint.

The [attempt 10 result](../../../artifacts/copperstart-makelink-production-boot/attempt10-install-library-a6/result.json)
has SHA-256
`36ec2b27cb45d22d5b4103c5fe329abc49b403e33b0a5c6f6d4bebbec485cce6`.
The full production emulator builds without warnings or errors, and its single
installation-stage integration test passes. The native DOS HUNK remains
`675560c38cac37a679db980e0595b393ce9417e7ae7fd0a09d4d226999cbbeb7`;
the fixes are in the production boot/Exec owner, not an altered native image.

The [attempt 12 trace](../../../artifacts/copperstart-makelink-production-boot/attempt12-launcher-argument-trace/result.json)
keeps the valid configuration bytes and actual instruction/register sequence.
A fresh current-source CLI rebuild, with 453 unchanged source inputs before and
after, regenerates the **same** faulty driver HUNK in
`artifacts/copperstart-launcher-current-compiler-20260908/`; this is not explained
by a stale compiler artifact. Source inspection identifies
`M68kMemoryPromotionPass.AddZero`: promoted zero seeds are inserted before the
machine IR's incoming argument definitions, allowing their registers to overlap
with arguments already delivered by the emitted prologue. The correction and
its execution regression belong in that compiler owner, not in driver input
validation or a test-supplied register repair.

The compiler now places promotion seeds after incoming argument definitions and
their canonical copies. The same native execution regression fails against the
preserved old compiler (expected pointer `0x0026F198`, actual zero) and passes
against the fix; two simple controls pass on both. The
[compiler receipt](../../../artifacts/compiler-promoted-entry-regression/qualification.json)
has SHA-256
`7db1098feffa4a1104f78659d190018749b282990af990ea3d70ac3d67bbe125`;
root independently rechecked its 17 bindings. This is a focused correction,
not a full compiler-suite qualification.

Recompiling the unchanged driver with that fix produces HUNK SHA-256
`4e8277a0ca60f586f9ecccace2938e02ef4a482c5c9266c03050466f984d033d`.
Attempt 13 observes its corrected argument transport and native process creation.
The next driver revision adds public `AllocSignal`/`Wait` and child `Signal`,
using a 160-byte record. Its HUNK is
`1fd2e43cd7f6f032df94c2f86bf922361bf46e222ae58a02cd9338ea36e0538d`
under `artifacts/copperstart-launcher-public-join-20260908/native/`. It retains
zero managed allocation sites, runtime helpers and external native targets;
its one nullable-value feature is unchanged. Its build alone does not prove
that the new handoff or command succeeds.

The compiler correction also changes the live compiler source identity. Earlier
command build snapshots and receipts retain their exact historical inputs; they
must be refreshed before a later admission requiring current source bindings.
No existing MakeLink or native DOS HUNK is silently rebuilt, replaced or
requalified by the driver experiment.

## Independently completed supporting work

Configured-volume publication passes four focused owner tests: initial Task and
same-port idempotence, partial-add rollback, in-flight-call retention, and the
existing two-volume production queue regression. These use another real managed
DOS public-list owner as the callback target, not the native DOS image. The
[receipt](../../../artifacts/native-configured-volume-publication-20260908/qualification.json)
has SHA-256
`9be4b7d55367ab4a8183e8b62b6bb5d5b52e6df6d06aaa9cbbfbbaf0bff445d1`;
root independently rechecked all 56 file bindings without mismatches.

The session driver builds as a 68000 HUNK, SHA-256
`013d87f11d3613f34096c25a4476affab799bf747b3b3404a201f6b039ce5e46`.
Its [build receipt](../../../artifacts/copperstart-makelink-boot-launcher-20260908/native-third/build.json)
is explicitly `built-not-executed`; root independently rechecked its 20 file
bindings. It is not a shipping command or a PURE-qualified image. Invoke the
export aliases, not method-body symbols that bypass resident ABI trampolines.

Five existing default-boot regressions also pass after the callback and reset
fixes: application launch, startup/ReadArgs/LoadWB, MakeLibrary, and AUTOINIT
library/device rows. The latter use zero initializer callbacks and therefore do
not replace the native callback test above. The isolated project compiles the
unchanged selected test classes and their existing helpers against the complete
production emulator. The
[regression receipt](../../../artifacts/default-boot-continuation-regression-20260908/qualification.json)
has SHA-256
`f1026fcde4b686559b9b3c08ab344f37bbf507b1ea981239adf1bb488d3d452b`;
17 source inputs match before and after the build, and root independently
rechecked all 67 file bindings. Earlier failures and the intermediate five-pass
run remain separate historical results.

After enabling native-admitted blocking, the same five regressions plus the
existing PAL/NTSC WaitPort retry and five synthetic scheduler ownership/nesting
cases pass: **12 total**. The
[expanded regression receipt](../../../artifacts/default-boot-native-scheduler-regression-20260908/qualification.json)
has SHA-256
`d63f624d166843752d91788ede175fb9f08056c9d31ca4382e956b912ac0e462`.
Its 22 source inputs match before and after the build, and root independently
checked all 66 file bindings. These default/owner cases do not establish native
child execution, signal wake completion or retirement.

## Signal result ownership correction, 2026-09-09

The shared signal transition consumes matching signals when a waiting task
becomes Ready. It previously always wrote the result through native `tc_SPReg`,
even when the host scheduler owned a separate saved CPU context. That could
write an old guest stack frame. The host continuation then searched for the
already-consumed signals and blocked again. PutMsg/ReplyMsg delivery used the
same shared transition, so correcting public Signal alone would not suffice.

The shared signal and port cores now accept an explicit context-owner choice
and return the consumed result to that owner. Existing native callers retain
their canonical-frame behavior. Host signal/port services retain the result in
the existing pending-wait record and return it once; they do not write a stale
native frame or consume signals posted after that wake. The ROM-owned fallback
uses the restored result after native delivery clears SigWait.

The [component comparison](../../../artifacts/exec-host-wait-resume-20260909/qualification.json)
has SHA-256
`c90f19b16ab01724d59675cd883763f81f05b2950cee47f9b6c0139dabacb819`.
The same compiled 14-test assembly fails eight cases and passes six against
preserved old owners; all 14 pass against the correction. Ten cases exercise
host wait/port result ownership and four are unchanged shared native signal
state tests. Root verified all 97 receipt bindings. Controlled guest Tasks,
suspension callbacks and explicit selection make these component tests, not
actual CPU context-switch, native DOS or original-ROM executions. See the
[test project and usage](../../../tools/Commands/ExecWaitResumeRegression/README.md).

The [fresh 12-case default boot/scheduler regression](../../../artifacts/default-boot-wait-resume-regression-20260909/qualification.json)
also passes, SHA-256
`01f6260efd930f476e5d79f776dbb3b798184ca301ec1e91a9d41dc55a9fc577`.
Its 25 observed source inputs match before/after; root verified all 69 receipt
bindings. The [attempt 16 installation result](../../../artifacts/copperstart-makelink-production-boot/attempt16-install-wait-result-owner/result.json)
has SHA-256
`08584f9091c6d31878c2a97282b8b3bb9b00e54ff2e7891dc640bfa3425fd0a9`.
These scopes do not complete the child-publication or command gate. Builds
report the existing NU1902 advisory for Microsoft.Build.Tasks.Git 10.0.300;
dependency advisory remediation is not claimed here.

## Next executable steps

1. Finish native DOS process preparation before public Exec publication. Make
   publication failure and retirement release each resource exactly once,
   preserve caller stream ownership on failure and BCPL D1 startup data, and
   retain the managed process adapter's registration contract. Do not scan and
   adopt arbitrary ready-list tasks or register a context from the test.
2. Rerun the command stage with the compiled public-join driver. Require a real DOS child,
   CLI, streams, native public calls, production packets, command result,
   target-deletion/alias readback and process retirement in the same run.
   Qualify actual signal wake and GetMsg/WaitPort resumption through the same
   production scheduler; blocking alone does not establish completion.
3. If that run requires another production fix, retain its failed attempt and
   rerun installation and the combined stage against the final sources. Preserve
   native input identities, actual loaded assemblies and terminal results.
4. Rerun the affected 12 default boot, library, WaitPort and scheduler regressions
   against the final production sources, plus process publication/rollback
   coverage at the owning DOS boundary. The existing
   zero-initializer AUTOINIT tests alone do not cover guest callback completion.
5. Integrate and exercise the existing Shell/disk-startup owner, then complete
   the exact-profile, reference, minimum-stack, resource and original
   PURE/resident requirements. Positive release admission, actual command image
   staging and system execution remain subsequent gates.

No user permission, reference-media choice or other user input is needed for the
current callback and production-launch work. Broader command-family work remains
required by the goal; this checkpoint does not narrow its scope to MakeLink.

### 2026-09-09 - Prepared process publication reaches native LoadSeg

DOS now prepares ordinary/C-assembler Process state before publication through
an explicit platform capability. Native publication uses public Exec
Forbid/AddTask/Permit, with task-port output stored before scheduling resumes.
The managed adapter registers the CPU context before exposing that output.
Failure revokes transferred caller ownership before normal private cleanup.
BCPL's separate D1 publication path remains open for native integration.

Preparation tests also exposed initial-frame writes below the declared task
stack; Exec now checks stack bounds before writing. The final shared selection
passes 53 tests, and managed process registration/rollback passes 23 tests.
The callback-local correction removes the new CS8602 warning. These tests do
not prove native retirement or a minimum command stack.

The actual native producer emits 679788 bytes with SHA-256
683f8cd3151dc123cf059ac6f345937554b9f97a4632ecac55a67a8d3155fda2.
Its report has no managed allocations, runtime helpers or external targets.
All 11 snapshotted source inputs remain unchanged; this is a source subset,
not a hermetic full-source build assertion.

Combined attempts 17/18 prove public AddTask and child DOS calls. Attempt 18
shows the first filesystem request still queued: the command phase used the
boot-block loop, without the synthetic runtime host-device pump. Attempt 19
uses ContinueCopperStartRuntimeUntilCycle, the production runtime boundary.
The child gets past Open and reaches LoadSeg, then exhausts the full eight
million instructions. Its trace contains 594 action-82 reads, 602 action-26400
provider packets, one action-1005 and one action-1006; MakeLink is not entered.
The child remains running and the parent waiting. No cleanup/retirement claim.

The after-instruction observer misses the initial child entry despite the
child's recorded Process/CLI/stream fields and subsequent public calls; fix
that observation boundary before accepting its entry-count assertion. Final
packet-address snapshots can contain reused memory and are not completion
results. The current source deduplicates those addresses and labels this limit;
that diagnostic-only edit follows attempt 19 and is not covered by its run.

Evidence: artifacts/dos-public-exec-publication-20260909/checkpoint.json,
SHA-256 799b2bb564ba3d9fd4403c93d64277da6dd6d34eb94f1f2521f3a44cc57b3496.
The checkpoint binds native compilation, 53/23 test reports and all three
combined-run receipts/results. Each combined run reports unchanged inputs.
Prior signal/default regressions are historical and have not been rerun on
this final publication change. Next: diagnose native LoadSeg progress through
actual packet results and loader state, then combined wake and retirement.
Shipping remains 0/200 commands and 0/246 profiles; all goal scope is retained.

### 2026-09-09 - Correct HUNK payload sizes; native LoadSeg reaches RunCommand

Attempts 20-22 establish successful byte-wise file delivery. Attempt 22's 3232
captured read bytes exactly match the unchanged MakeLink image, SHA-256
12d1f36504f5a0f7099bc419e4fffc5f43f138d7b4e8745a2ca907bcb73b5f45.
Eight million instructions were insufficient for 3232 separate seek/read pairs;
the diagnostic bound is now 256 x 250000 instructions, with 16384 packet events
allowed before explicit failure. This is not a throughput or minimum-stack gate.

The loader omitted the on-disk payload-length word after CODE/DATA/BSS and
rejected SYMBOL metadata. Both ordinary and external-callback loading now consume
and validate the payload length, zero the remaining allocation, and skip bounded
SYMBOL/DEBUG records. Seven shared test fixture files previously omitted the
length word; these now contain standard images. The disk-handler failure fixture
uses a 44-byte image so its size-based injection still targets the handler owner,
not the source buffer. A managed callback fixture was corrected but not rerun.

The expanded shared selection passes 54 tests. The same compiled metadata test
against the previous DOS assembly fails the valid image and rejects the malformed
image (1 fail/1 pass); corrected tests accept valid metadata and reject malformed
lengths. First intermediate expanded run had 53 pass/1 failure due to the fixture
allocation-size collision, retained in artifacts/dos-hunk-format-20260909.

Format reference: [AmigaDOS RKM, section 11.2](https://developer.amigaos3.net/sites/default/files/downloads/2024-10/Amiga_ROM_Kernel_Reference_Manual_DOS.pdf).
This change is not full executable-format compatibility qualification.

New native DOS HUNK: 680408 bytes, SHA-256
3502625f1d86bdade00a3abcf3c180a95aa364e71938f85e0e30fb2a7645c583.
Attempt 23 loads it through the production owner, obtains a nonzero command
SegList and reaches public RunCommand. It then halts with
AMIGA_BOOT_TASK_TRAP_INVALID: SP=0xFFFFFFF6, frame=0xFFFFFFFA.
Command entry/completion, caller restoration and successful-child retirement
remain open. The prior loader-failure path returned to the parent and removed
the child; that does not qualify successful execution cleanup. Next: trace the
RunCommand stack switch and native call boundary without fabricated contexts.

Evidence: artifacts/dos-hunk-format-20260909/checkpoint.json binds tests, native
image and attempts 20-23. Each combined receipt reports unchanged source inputs.
No goal stage is complete; shipping remains 0/200 commands and 0/246 profiles.

### 2026-09-09 - Public StackSwap returns correctly; retirement check corrected

Attempt 24 shows public StackSwap returning into its descriptor instead of the
saved PC. The host adapter had used the raw stack-exchange primitive without
moving the return address. ExecCpuStateCore.StackSwapCall now saves the old
post-return SP, transfers only the return PC to new SP-4, and leaves caller
frames untouched. ExecTaskServices uses that public-call adapter. Raw frame
exchange remains unchanged for the separately framed native Exec paths.

18 selected shared tests pass. Two new host component cases cover the public
adapter and real ready/wait-list FindTask lookup: current host passes all 16;
the identical test DLL with the old emulator fails those two and passes the
original 14. The default boot/scheduler selection plus the full managed
RunCommand callback class passes 27 cases, including the corrected HUNK fixture.

Attempt 25 completes MakeLink through native RunCommand/ReadArgs/Lock/Examine/
MakeLink/UnLoadSeg. Result and IoErr are zero, streams are restored, the original
is deleted and the alias reads back 24 bytes. Its child-entry assertion failed
because the after-instruction observer skipped the first PC on scheduling.
The observer now checks completion of the bound export's first PEA (A5), with
its opcode verified before execution.

A more important check was also wrong: synthetic FindTask(name) returned only
compatibility names, so prior negative lookups did NOT prove child retirement.
FindTask now searches the real current/ready/wait tasks before the existing
compatibility fallback. Earlier attempts reporting childRemoved=true must not
be cited as retirement evidence. In attempt 26, one child and one command entry
are observed, the command succeeds, but real lookup returns child 0x270898,
still Ready. The parent has resumed before the child's return/retirement path.
The test fails and keeps driver/config allocations quarantined. No successful
process-lifecycle or cleanup qualification is claimed.

Independent inspection of attempt 26's host volume confirms the alias contains
production-boot-payload plus newline, target is absent, caller is unchanged and
diagnostics is empty. This proves only this command invocation's filesystem
slice, not profile, CPU matrix, resident reuse or packaging completion.

Evidence: artifacts/exec-stack-swap-call-20260909/checkpoint.json binds the
18/16/27 reports, old-host control and combined attempts 24-26. All three
combined runners report unchanged inputs. Native DOS HUNK remains
3502625f1d86bdade00a3abcf3c180a95aa364e71938f85e0e30fb2a7645c583;
this fix is in the public host Exec adapter, not a replacement fixture gateway.
Next: follow actual child continuation after the completion signal and provide
correct process exit/retirement and DOS resource ownership before any release.
Shipping remains 0/200 commands and 0/246 profiles; every goal family remains.

### 2026-09-09 - Default task finalizer fixed; native DOS record retained

Attempts 27/28 use public SetTaskPri to yield after the completion signal,
allowing the child to return. The diagnostic tail shows the child entering the
raw RemTask vector with A1 still equal to the parent and A6 zero. That removes
the parent and then falls through a nonexistent task return address, faulting.
The test does not directly remove tasks or inject scheduler contexts.

Public AddTask now supplies an Exec-owned default finalizer at 0x00F08520 when
its final-PC argument is null. The installed terminal entry establishes A1=0
and the active Exec base, calls the real removal owner, and cannot ordinary-RTS
back onto the exhausted initial stack. Explicit finalizers are unchanged. A
normalized source scan found no preexisting use of this address in the emulator.
This fixes the public host path; raw shared/native frame constructors are not
claimed fully qualified by these tests.

18 host component cases pass, including the default-versus-explicit final-PC
rows. The selected default boot/scheduler and RunCommand callback suite passes
27 cases against this change. Neither component result alone proves native
process cleanup.

Attempt 29 reaches real child removal and restores the parent without a trap.
Its remaining packet assertion expected classic ExamineObject (23), while the
native provider deliberately sends supported ExamineObject64 (26408) first.
The test now expects that actual provider protocol while still requiring public
Examine and checking the command's filesystem effects.

Attempt 30 adds a read-only native DOS process-list audit (state +8, owned-header
Next +16, Task +20, from DosObjectRecords.cs). Exec FindTask returns zero and the
child is Removed, but native DOS retains process record 2591400 (decimal) for
that task. The test fails with childDosResourcesReleased=false and keeps driver
and config allocations quarantined. Exec removal is NOT complete process cleanup.
The default finalizer currently bypasses native DOS process-state release; the
managed DOS retirement barrier owns a different DOS state and cannot replace it.

Next implementation: provide an actual native DOS process-exit owner that
releases native Process/CLI/streams/path/text/resident and deferred provider
ownership before terminal public Exec removal. Do not fix this by deleting the
record in the test, invoking managed cleanup on native state, or freeing an
executing stack. Preserve pending cancellation and BCPL D1 requirements. Then
repeat the combined run, exact allocation/registration cleanup and resident reuse
checks. All command families, profiles, PURE and packaging gates remain required.

Evidence: artifacts/exec-default-task-finalizer-20260909/checkpoint.json binds
18/27 reports and attempts 27-30. The four runners report unchanged inputs.
No command, profile, PURE gate or stage is complete: shipping stays 0/200 and 0/246.
