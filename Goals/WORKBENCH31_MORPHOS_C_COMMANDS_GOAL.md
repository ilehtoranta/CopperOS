# Goal plan: all Workbench 3.1 and MorphOS C: commands

Prepared: 2026-08-30. Status: implementation in progress. Execution checkboxes
remain the authoritative completion record.

## Objective and scope

Implement the complete external command set supplied in `C:` by original
Workbench 3.1 and MorphOS 3.20, preserving their options, behavior, and original
pure/resident requirements. Reuse the existing CopperOS Shell commands and
script engine. Prefer public Kickstart 3.1 APIs, particularly DOS `ReadArgs`,
over replacement parsers or command-private OS implementations.

The Shell's existing built-in commands are infrastructure for this work and
are not part of the C: executable inventory. Do not duplicate them; implement
the external C: commands and connect them to the existing Shell process,
stream, and script owners.

MorphOS 3.20 is the baseline already used by this repository. Workbench means
the original 3.1/v40 distribution, including installation-disk helpers; it does
not mean AmigaOS 3.1.4, 3.2, or 4.x. Record machine-specific original 3.1 disk
variants during inventory closure. Do not silently change either baseline.

This plan expands the scope of the
[existing core-command goal](D:/Koodit/GIT/CopperOS/Goals/MORPHOS_CORE_COMMANDS_GOAL.md).
Its blanket
deferrals of networking, archives, ARexx, hardware, editors, and other command
families do **not** satisfy this new objective. Those commands and their needed
service integrations are required work here. Retain the older goal and progress
files as history. The Shell goal retains ownership of Shell internals.

The initial, media-backed inventory contains **200 distinct external command
names**: 188 in the MorphOS 3.20 ISO and 58 across the two inspected Workbench
3.1 disks, with 46 names shared. There are 12 Workbench-only names. This is a
verified starting set, not a claim that every Workbench disk variant or final
installed directory has already been audited. CC00 closes that remaining gap.
`Freeze` is an additional documented command requiring the explicit CC39
disposition; it is not included in the 200 count.

## Current blockers and next executable gates

There is no single toolchain failure blocking this goal. The plan is active,
but shipping is still **0/200 commands and 0/246 profiles** because bounded
resident candidates do not pass the complete reference, guest-parity,
PURE/resident-lifecycle, licensing, and package gates. The blockers below are
scope gates, not reasons to reduce the inventory:

1. **Reference closure is incomplete.** The remaining Workbench disks,
   installed `MOSSYS:C`/`SYS:C` placement, exact media hashes, help/parser
   behavior, diagnostics, result levels, and profile-specific `PURE`/resident
   flags have not been captured for every command. A source or index entry is
   not sufficient evidence.
2. **Workbench parity remains partial for the first CC18 candidates.**
   Separate bounded Workbench 3.1 `Break` and `ChangeTaskPri` bodies pass
   refreshed three-CPU resident HUNK qualifications with twenty-four and
   thirteen supplied vectors per CPU, including Workbench-startup and
   missing-DOS boundaries. Nine bounded original/candidate guest cases now match output,
   return and caller post-System IoErr: missing required arguments,
   `ChangeTaskPri 0`, nonexistent CLI targets (`999999`), and both out-of-range
   `ChangeTaskPri` bounds (`-129` and `128`), range-error precedence over a
   missing CLI, and the `Break 0` target error (receipt:
   `artifacts/workbench31-guest-command-break-zero-candidate-20260927-v1/effect-comparison.json`).
   Twenty more comparisons verify effects in saved guest RAM:
   Break checks all sixteen C/D/E/F switch combinations, including the
   no-switch default, plus `ALL`;
   ChangeTaskPri changes CLI 1 to 42 and to both valid limits (`-128` and
   `127`). Receipts:
   `artifacts/workbench31-guest-command-cc18-command-pairs-20260927-v2/qualification.json`,
   `artifacts/workbench31-guest-command-break-self-signal-candidate-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-changetaskpri-self-priority-candidate-20260927-v1/effect-comparison-v2.json`,
   `artifacts/workbench31-guest-command-changetaskpri-128-self-candidate-fixed-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-changetaskpri-minus129-self-candidate-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-changetaskpri-range-missing-self-candidate-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-changetaskpri-pri127-candidate-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-changetaskpri-pri-minus128-candidate-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-break-default-candidate-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-break-all-candidate-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-break-df-candidate-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-break-d-candidate-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-break-e-candidate-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-break-f-candidate-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-break-de-candidate-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-break-cd-candidate-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-break-ce-candidate-20260927-v1/effect-comparison-v2.json`,
   `artifacts/workbench31-guest-command-break-cf-candidate-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-break-ef-candidate-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-break-cde-candidate-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-break-cdf-candidate-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-break-cef-candidate-20260927-v1/effect-comparison.json`,
   `artifacts/workbench31-guest-command-break-def-candidate-20260927-v1/effect-comparison.json`,
   and `artifacts/workbench31-guest-command-break-cdef-candidate-20260927-v1/effect-comparison.json`.
   The first five result cases are also bound by the 91-identity aggregate at
   `artifacts/workbench31-guest-command-cc18-command-pairs-20260927-v2/qualification.json`.
   Differential runs exposed and fixed parser return/header, missing-target,
   and Workbench range-error mismatches. Additional Break CLI targets and error
   ordering, other priority values and targets, MorphOS PID semantics, lifecycle
   and profile admission remain open.
3. **MorphOS PID semantics remain only partially closed.**
   `FindTaskByPID` is a pointer-indirect MorphOS slot at `ExecBase-994`. The
   SDK wrapper, supplied-vector fixtures, CopperStart production installer, and
   compiled 68000 guest call now verify its A0/D0 contract and publication in
   the installed vector image. TaskList's `TASKINFOTYPE_PID_CLI` selector is
   now implemented by the shared provider: CLI Processes report their DOS task
   number, while non-CLI tasks with a MorphOS ETask extension use its
   `UniqueID` field. Portable task allocations without that extension still use
   the guest-address fallback. `FindTaskByPID` searches the native unique ID
   when available, plus live CLI numbers, and resolves the current owner of a
   reused CLI number. MorphOS `NewCreateTaskA` and DOS-owned Process
   allocations retain inline ETask ownership. The installed generic `AddTask`
   vector now allocates a separately owned ETask sidecar for legacy MorphOS
   callers, while Workbench 3.1 version 40 retains the old Task prefix without
   an extension. Managed lifecycle and compiled 68000 AddTask/RemTask tests
   cover both layouts and deferred self-removal cleanup. The original MorphOS
   guest still needs to confirm task-ID sequence/reuse and race behavior before
   explicit non-CLI PID targeting can ship.
4. **Guest differential and lifecycle evidence is open.** Each command needs
   disposable Kickstart 3.1 and MorphOS executions, concurrent registry/target
   changes, process-owner cleanup, repeat/resident reuse, and failure-path
   checks. Supplied-vector tests remain bounded ABI evidence rather than OS
   parity.
5. **Packaging and licensing are open.** The final profile manifests must put
   normal-runtime, installation-media-only, and resident commands in the right
   images, while source reuse remains limited to files with cleared rights.

### Current blocker evidence (2026-09-27)

The MorphOS TaskList PID_CLI follow-up closed a concrete shared-provider gap:
its `NewGetTaskAttrsA` request used selector `0x24`, but CopperStart previously
did not recognize that selector and the command's documented fallback printed
zero for every task. The SDK now names the selector and CopperStart returns
`Process.pr_TaskNum` when `Process.pr_CLI` is present, with its existing live
MorphOS `ETask.UniqueID` for non-CLI MorphOS tasks when its extension is present;
portable tasks without an extension retain the guest-address fallback.
`NewCreateTaskA` and MorphOS DOS-owned Process creation allocate an ETask
extension and assign IDs from the ExecBase `ex_TaskID` counter. The DOS Process
path enables this layout only when Exec library version is at least 50; the
Workbench 3.1 version-40 path keeps its original Process/stack layout. The
installed generic `AddTask` vector now allocates an ETask sidecar with the
active ROM allocator policy, registers the exact allocation through a one-entry
`tc_MemEntry`, and rolls it back if task initialization fails. Foreign-task
`RemTask` retires only this owned sidecar; self-`RemTask` waits until the next
task context is selected on the supervisor continuation stack. Generic task
reaping removes the sidecar before walking remaining entries. The shared
initializer sets MorphOS `TF_ETASK` (bit 3), the official SDK validity flag for
`Task.tc_ETask`, after publishing the pointer and preserves caller-provided Task
flags. The PID provider reads the extension only when this flag is set; a
nonzero stale pointer with the flag clear retains the task-address fallback and
is covered by a focused regression test.
`FindTaskByPID` searches native unique IDs and live CLI numbers in the current,
ready, and waiting task records, with CLI-number reuse behavior covered.
Provider tests and compiled production-vector tests cover both identities and
direct `NewGetTaskAttrsA` dispatch for selector `0x24`. The refreshed
provider/creation filter passes 20 tests, the DOS child/publication/lifecycle
filter passes 42 cases, and the refreshed TaskList resident qualification
passes 54 supplied vectors per CPU on 68000/020/040 with 69 reachable methods, no managed
allocations/runtime helpers/external targets/exceptions/fatal sites, no leaks,
and no shared-image writes:
`artifacts/tasklist-morphos-native-pid-cli-20260927-v4/qualification.json`.
These tests include MorphOS Process ETask PID lookup, a stale-extension-pointer
fallback control, and the unchanged Workbench version-40 layout control. The
latest task lifecycle, versioned AddTask, MorphOS self-RemTask, and vector
classification filter passes 9 tests. Adjacent task-vector, memory ownership,
native-boundary, DOS lifecycle/publication, MorphOS extension, and vector
classification regression filters pass 72 tests. These source and compiled
68000 checks close the AddTask sidecar allocation/reaping gap; they do not
establish original MorphOS guest ID-sequence parity or behavior under
concurrent scheduler changes. Those guest comparisons remain open, as do the
rest of the commands and profile gates. Shipping totals and profile admission
status do not change.

The sidecar is tied to the installed direct `AddTask`/`RemTask` vectors and the
active ROM allocator policy. `AddExecNotify` remains unsuitable as an extension
initialization contract: MorphOS documents it as a monitor hook and explicitly
leaves pre-`AddTask` mutation undefined ([MorphOS Exec SDK: AddExecNotify](https://morphos-team.net/sdk/exec.html)).
The original guest must still confirm lifecycle and PID semantics before this
behavior can be admitted for shipping.

Normal Workbench startup now captures ten original Version invocations, and
the corrected candidate matches all ten in independent replacement guest runs:
`artifacts/workbench31-guest-version-pairs-20260926-v2/qualification.json`. These cover ordinary/case-insensitive Resident lookup, RES, FULL,
major/revision warning boundaries and numeric parser failure. Exact bytes,
return and caller post-System IoErr agree. The separate native qualification
`artifacts/version-wb31-native-20260926-resident-first-v2/qualification.json` passes 174 supplied invocations across 68000/020/040 with no shared
image writes or fixture leaks; source/compiled input hashes were rechecked.

The earlier candidate's ordinary-name mismatch and RES match remain retained
as positive/negative controls, alongside 39 passing tooling tests. Replacement
is limited to fresh external derivatives and preserves source metadata,
including Version's protection 0 (P clear). No original media or emulator
behavior was modified, and no shipping checkbox changes for bounded cases.

The 2026-09-27 Workbench Version native requalification now requires the
original DOS v37 startup minimum and passes 58 supplied invocations per CPU
across 68000/020/040:
`artifacts/version-wb31-native-20260927-dos37-v1/qualification.json`.
The previous DOS v36 entry mismatch is closed for this bounded candidate;
at that checkpoint, ordered providers, FILE and the complete guest, lifecycle,
rights and package gates remained open.

The 2026-09-27 Workbench Version FILE follow-up now passes 72 supplied
invocations per CPU (216 total) across 68000/020/040, including direct `$VER:`
scanning across `DOS.Read` boundaries, FULL date/extra output, provider
restriction, HUNK `LoadSeg` Resident lookup through linked segments, and
open/read/allocation cleanup:
`artifacts/version-wb31-native-20260927-file-v5/qualification.json`.
Fresh original-guest observations show `C:Version C:Avail FILE` prints
`avail 40.1`, FULL adds `(02/09/93)`, a failed minimum comparison prints before
returning 5, and `C:Version FILE` still prints the system line. Original
HUNK-provider captures also cover `DEVS:clipboard.device`; the receipts are
listed in the Version contract. Four bounded FILE original/replacement guest
pairs now pass, each with a 2,400-frame exact comparison:

- `C:Version C:Avail FILE` emits `avail 40.1\n` (11 bytes), returns 0, and
  leaves caller post-System IoErr at 0:
  `artifacts/workbench31-guest-command-version-file-candidate-20260927-v2/comparison.json`.
- `C:Version C:Avail FILE FULL` emits `avail 40.1 (02/09/93)\n` (22 bytes),
  returns 0, and leaves caller post-System IoErr at 0:
  `artifacts/workbench31-guest-command-version-file-full-candidate-20260927-v1/comparison.json`.
- `C:Version C:Avail FILE VERSION 999` prints `avail 40.1\n` (11 bytes),
  returns 5 after printing, and leaves caller post-System IoErr at 0:
  `artifacts/workbench31-guest-command-version-file-warning-candidate-20260927-v1/comparison.json`.
- `C:Version DEVS:clipboard.device FILE` emits `clipboard.device 38.8\n`
  (22 bytes), returns 0, and leaves caller post-System IoErr at 0:
  `artifacts/workbench31-guest-command-version-file-hunk-candidate-20260927-v1/comparison.json`.

The derivative receipts bind the original 4,764-byte `C/Version` identity and
preserved metadata (protection 0), plus the candidate HUNK SHA-256
`f7f33772232a836a1448f268f8e872e0f49ae7f9fe087852dd76c1ce186de88b`. These
pairs establish only the four captured invocations. Missing/no-tag errors,
other FILE cases and profile qualification remain open. The separate system
path candidate subsequently corrected the observed `C:Version FILE` mismatch;
the system receipts below use a new HUNK identity and do not retroactively
change these four FILE receipts.

The 2026-09-27 no-name Workbench system path now reads Kickstart values from
ExecBase, opens `version.library` through Exec, and formats/comparisons against
the Workbench version provider. A fresh native qualification passes 72 supplied
invocations per CPU across 68000/020/040, with no leaked fixture resources or
shared-image writes:
`artifacts/version-wb31-native-20260927-system-v4/qualification.json`.
Four separate 2,400-frame original/replacement guest pairs match exact output,
return and caller post-System IoErr:

- `C:Version FILE` emits `Kickstart 40.63, Workbench 40.42\n` (33 bytes),
  returns 0, and leaves caller IoErr 0:
  `artifacts/workbench31-guest-command-version-system-file-candidate-20260927-v2/comparison.json`.
- `C:Version FULL` emits `Kickstart 40.63, Workbench 40.42 (02/18/94)\n`
  (44 bytes), returns 0, and leaves caller IoErr 0:
  `artifacts/workbench31-guest-command-version-system-full-candidate-20260927-v2/comparison.json`.
- `C:Version VERSION 99` prints the same short system line and returns 5:
  `artifacts/workbench31-guest-command-version-system-min99-candidate-20260927-v2/comparison.json`.
- `C:Version VERSION 39` prints the same short system line and returns 0:
  `artifacts/workbench31-guest-command-version-system-min39-candidate-20260927-v2/comparison.json`.
- Revision-only `C:Version REVISION 42` prints that line and returns 0 at the
  equal boundary:
  `artifacts/workbench31-guest-command-version-system-rev42-candidate-20260927-v1/comparison.json`.
- Revision-only `C:Version REVISION 99` prints that line and returns 5 above
  the Workbench revision:
  `artifacts/workbench31-guest-command-version-system-rev99-candidate-20260927-v1/comparison.json`.

These system comparisons bind the 68000 candidate HUNK SHA-256
`59005e5e65b0c031bfadd1382b1f585c4af4f1f45c2bc6f71d6fcdb29b7449d2` and
the original command metadata. Other system `REVISION` interactions, missing
`version.library`, secondary-error/result ordering, additional FULL/FILE
combinations, and original PURE admission remain open. Revision-only 42/99
and three joint comparison cases now match original guest behavior:

- `VERSION 39 REVISION 99` returns 0 although the requested revision is high:
  `artifacts/workbench31-guest-command-version-system-v39-r99-candidate-20260927-v1/comparison.json`.
- `VERSION 40 REVISION 42` returns 0 at the exact equal boundary:
  `artifacts/workbench31-guest-command-version-system-v40-r42-candidate-20260927-v1/comparison.json`.
- `VERSION 40 REVISION 43` returns 5 above Workbench revision 42:
  `artifacts/workbench31-guest-command-version-system-v40-r43-candidate-20260927-v1/comparison.json`.

The 68000 HUNK in these comparisons is the same v4 candidate. Six FILE guest
pairs against it now cover normal/FULL output, a post-output minimum warning,
HUNK Resident lookup, missing-file error and no-tag error; see the Version
contract. More signed/parser and error-ordering cases remain open. Original
`C/Version` protection is zero; resident HUNK evidence does not establish PURE
safety.

The 2026-09-27 Workbench Version command-segment slice implements the default
command-segment provider after the first Resident miss. Its 77-vector-per-CPU
resident qualification passes on 68000/020/040, covering DOS `FindSegment`
system 0/1, ordinary and FULL `$VER:` segments, and internal/disabled fallback
through the `shell` Resident:
`artifacts/version-wb31-native-20260927-segments-v4/qualification.json`.
This is native fixture evidence only; original-guest parity for these names is
not yet captured. The following checkpoint covers a separate trailing-colon
DOS-list path; other LibList, DeviceList, direct-file and LIBS/DEVS ordering
and error cases remain open.

The next Workbench Version increment adds the original trailing-colon device
handler lookup using DOS `LockDosList`, `FindDosEntry` and `UnLockDosList`, then
the handler DeviceNode's startup and segment list. Its refreshed native
qualification passes 85 supplied invocations per CPU on 68000/020/040, including
FULL formatting, missing-entry/startup/segment/Resident fallthrough, balanced
list locks, and restoration of the temporary colon:
`artifacts/version-wb31-native-20260927-doslist-v6/qualification.json`.
The 68000 HUNK is 12,004 bytes with SHA-256
`c5ee16f55f25508eca21819fd4b7a000d0954292f15cd1f4c8ae2dc6f9667dee`.

Two original/replacement guest pairs now match the observed Workbench
`C:Version DF0:` and `C:Version DF0: RES` cases exactly: each emits
`filesystem 40.1\n` (16 bytes), returns 0, and leaves caller post-System IoErr
at 0. Receipts:
`artifacts/workbench31-guest-command-version-df0-candidate-20260927-v1/effect-comparison.json`
and
`artifacts/workbench31-guest-command-version-df0-res-candidate-20260927-v1/effect-comparison.json`.
The captures establish only these trailing-colon cases; they do not exercise
command-segment lookup or prove full Version behavior. Other named providers,
provider errors, broader guest parity, original PURE/resident lifecycle, rights
and package admission remain open.

CC18 has five exact parser, current-task no-op and missing-target pairs
bound by the 91-identity receipt
`artifacts/workbench31-guest-command-cc18-command-pairs-20260927-v2/qualification.json`.
Additional pairs verify both ChangeTaskPri range errors,
range-before-target precedence, valid signed endpoints, the exact `Break 0`
target error, and Break signal masks.
For `C:ChangeTaskPri 42 PROCESS
@SELF@`, the saved owner task is CLI 1 at priority 0 before System and priority
42 after, in both original and candidate guests; output is empty, return 0 and
caller IoErr is 0. For `C:Break @SELF@ C`, the same task's Ctrl-C received bit
changes from clear to set in both guests, with the same empty output and zero
result/error. `ChangeTaskPri -128` and `127` also return 0 and change the
observed CLI priority from 0 to the requested endpoint in both guests. Separate
Break pairs verify all sixteen C/D/E/F switch combinations, including the
no-switch default, and `ALL` on that same task. Exact comparison receipts are recorded in
the CC18 contracts and ledger. The capture path accepts named C-command
substitutions and passes 49 focused tests.

Next executable work includes Version's remaining ordered providers, broader
FILE success/error/provider parity beyond these six current-HUNK pairs,
additional signed/parser and system error-ordering cases;
Break's additional CLI targets/errors
and ChangeTaskPri's other targets/errors; and the still-open real
command lifecycle/profile gates. The caller IoErr observation is not general
child Result2 proof. The ten Version cases and bounded CC18 cases do not close
full commands, PURE/resident,
CopperStart, MorphOS, complete media/installed metadata, rights or package
gates.

The 2026-09-26 Workbench `SetFont` follow-up disassembled the selected HUNK's
SIZE/N path: it stores the LONG's low word into `TextAttr.ta_YSize`, then rejects
only when that unsigned word is at most four. The resident candidate previously
compared the full 32-bit value; it now matches the captured word behavior. Four
additional supplied result-vector cases cover low words 0, 4, 5 and `0xFFFF`.
The refreshed three-CPU HUNK fixture now passes 33 invocations per CPU,
including failure of the result array and each command buffer with cleanup,
zero leaks and no shared-image writes. This closes bounded candidate behavior,
not the Workbench guest, text parser, PURE/lifecycle, rights or package gates
for SetFont; see its contract and size-word audit.

The next ownership audit found that the captured HUNK leaves the opened font
unclosed on the successful no-window path; the candidate preserves that path.
It also found that some post-open failures in the HUNK reach shared cleanup
without `CloseFont`, while the candidate closes an untransferred font there to
avoid a leak. The new three-CPU receipt passes 33 supplied invocations per
CPU, including the no-window ownership assertion. This cleanup difference is
explicitly open for guest compatibility review; static disassembly cannot
measure Workbench diskfont reference counts. See the font-ownership audit and
SetFont contract.

The Workbench `LoadResource` client and hook slices now have executable native
evidence. The first 68000 transaction exposed incorrect native offsets despite
the earlier successful CLR `Pack=2` check. The client now writes the captured
54-byte message explicitly into stack-owned storage. Nineteen supplied-vector
transactions per CPU verify its fields, live `ReadArgs` pointers through reply,
returned result/`IoErr`, cleanup and instruction-interleaved callers. Receipt:
`artifacts/workbench31-loadresource-protocol-runtime-20260926-v4/qualification.json`.
The obsolete host-only layout check was removed.

Eleven hook lifecycle scenarios per CPU verify the 16-byte one-shot cache,
`SameLock` aliases, delegation through the saved vector, removal, duplicate
installation, failed acquisition and safe teardown refusal beneath a newer
patch. Native execution exposed and fixed the export's missing DOS-base setup.
The hook now takes the temporary DOS lock before its registry semaphore and
attempts teardown with `AttemptSemaphore`, matching the corrected source audit.
Receipt:
`artifacts/workbench31-loadresource-hook-runtime-service-name-20260926-v1/qualification.json`.
Both suites run on 68000/020/040 with no shared-image writes; they supply OS
responses and do not establish original-guest behavior or resident lifetime.

The new worker-startup audit records the exact `CreateNewProc` tags, forwarding
of the first real request, and original detached multi-HUNK ownership. It also
corrects earlier function offsets, catalog mappings and the teardown helper's
unconditional return. The full command's startup/parser orchestration, worker
process lifecycle, code-image lifetime, guest parity and
package gates remain open. `LoadResource` stays unchecked and retains its
source file's P-clear classification.

The opened-resource registry, resource actions, diagnostic defaults and
single-request worker dispatcher are now implemented and source-audited.
Separate native fixtures pass ten registry, thirty-four action, thirty-one
worker and fifteen launch cases per CPU on 68000/020/040. The worker checks borrowed context
restoration, exact request fields, matcher cleanup/error precedence, UNLOCK
priority, low-word LOCK retention and the source's listing text on a supplied
3000-byte stack. The source's lazy message-catalog open/close ordering, including
its non-cleared pointer and nested listing closes, is now implemented. The
launch transaction detaches a worker tail for public NP_Seglist/FreeSeglist
ownership and restores it on creation failure; a strict two-HUNK packer and
ownership design support the eventual full command. These slices are not a
complete coordinator. Guest catalog effects, malformed font-size fidelity,
actual child startup/exit, callback quiescence and retained code ownership
remain open. See the LoadResource
contract for current receipts and their explicit scope.

The verified reference set currently contains the MorphOS 3.20 ISO and only the
M10 40.42 Workbench 3.1 Install and Workbench disks. The remaining Extras,
Fonts, Locale, and Storage disks, other machine-specific disk variants, and a
clean installed system are not available in the captured set. The derived
inventory therefore still marks six closure items open: `WB31-DISKS-3-6`,
`WB31-MACHINE-VARIANTS`, `WB31-INSTALLED-METADATA`,
`MORPHOS320-INSTALLED-OVERLAY`, `COMMAND-RUNTIME-CONTRACTS`, and
`FREEZE-INDEX-MEDIA`. The 200-name external-command seed is a verified starting
set, not a closed inventory. This blocks inventory freeze, complete option and
runtime contracts, installed `PURE`/resident classification, and final package
placement. It does not block implementation work for commands whose references
and contracts are already captured.

Original Workbench media and ROM are partially available: the selected M10
Install and Workbench disks 1–2 are present, as is the hash-verified Kickstart
3.1 40.63 ROM. Missing disks 3–6 remain a separate inventory gap. The local
CopperScreen Lightweight runner's supported profile remains Kickstart 1.3,
but an exploratory clean run with the available 3.1 ROM and Workbench disk
now completes 2,000 frames without a reported unsupported feature. A private
passive slow-RAM runner resolves the prior visibility gap: snapshots identify
Exec 40.10, DOS 40.3, native Initial CLI startup command names and the Workbench
process. All 35 chip-RAM/framebuffer snapshots match the prior capture; the
only engine addition is a read-only SlowRam view, with a passing observer
regression. A separate authored probe now runs through the normal startup CLI
on verified disposable ADF derivatives and calls original DOS SystemTagList.
Four original C:Version queries have exact output and return-code receipts:
ordinary and minimum-39 queries return 0, minimum-999 returns 5 with output
retained, and invalid numeric input returns 20 with the Shell diagnostic.
All four publish stable guest-owned completion records; no host OS services or
PC/vector injection is used. Caller post-System IoErr is captured, but its
general correspondence to child Result2 remains unproven. Separate stderr,
replacement comparisons and lifecycle tests are still open. The framebuffer
remains striped/garbled but this capture route does not depend on it. See
`reference-captures/workbench31-guest-command-probe-20260926.md` under the command
documentation for receipts. The MedPlayer
application-session path still installs host services. Original command parity
remains open; the ROM and first two disks are not the missing prerequisite.
The evidence and exact capability boundaries are in
`docs/Commands/Workbench31MorphOS320/reference-execution-paths.md`.

The 2026-09-25 CC20 AddDataTypes update now passes profile-specific embedded-DTCD stack values through MorphOS and Workbench candidate paths. Its resident fixtures pass 42 MorphOS command vectors and fifteen Workbench vectors per CPU. This remains supplied-vector evidence; the exact historical MorphOS AROS header, original-guest parity, complete installed-system metadata, PURE/resident admission, rights and packaging remain open, so the 0/200 and 0/246 shipping counts are unchanged.

Latest bounded CC11 progress does not change those program-wide gates.
MorphOS `DOSList` now covers the captured device and volume verbose details,
plus assign target, lock, type and linked lock-list output; its refreshed
resident receipt passes 34 supplied vectors per CPU on 68000/020/040. Original-
guest comparison, PURE/resident, rights and package evidence remain open for
that command.

The latest `List` work is bounded fixture evidence only. Inclusive
`SINCE`/`UPTO` date-only filtering now uses public DOS `StrToDate` in both
profile candidates, with invalid-date cleanup covered. `QUICK` also now keeps
its documented names-only behavior when combined with `DATES`. Both profiles
use public DOS `APF_DODIR`/`APF_DIDDIR` flags for recursive `ALL`; the Workbench
candidate reads `ALL` from its shorter template's correct result slot. Nested
descent and return are fixture-covered. Original path rendering, diagnostics,
packed correspondence, lifecycle, and shipping admission remain unverified.

The latest bounded implementation evidence remains fixture-only. MorphOS `Dir`
now covers terminal `ExAll` errors, direct and recursive `RETURN_ERROR`
propagation, source-shaped generic/wrong-type diagnostics, dynamic row growth,
paged listings, representative low-memory cleanup, and the inspected source's
pattern `IoErr` save/restore order. The bounded fixture passes 39 supplied DOS
vectors per CPU across resident 68000/020/040 images. The full
`IoErr`/diagnostic/result-level matrix against original guests and behavior
under real handlers remain open. Workbench `Dir` has an independent syntax
candidate with bounded provider-error and large-listing coverage, but its
actual grammar and behavior are still unbound. MorphOS `Type` now uses the
MorphOS 50.67+ extended `AnchorPath` flag for literal soft links and rejects
older DOS providers; its bounded resident fixture passes 14 supplied vectors
per CPU with 26 reachable methods. Workbench `Type` retains the classic
`AnchorPath` layout and passes 15 vectors per CPU with 27 reachable methods.
Neither profile has original-guest parity, packed correspondence, or shipping
admission. These results do not change the
0/200 shipping count.

The MorphOS `Version` system path now passes 94 supplied DOS/Exec vectors per
CPU on resident 68000/020/040 images. The vectors cover the public SDK-backed
ARexx query and local `Ambient` variable, the source-ordered `ambient_path`,
`MOSSYS:`, and `SYS:` file providers, and both Ambient-provider and named-FILE
`IsFileSystem`/`LoadSeg` resident fallbacks, including unavailable and
missing-resident branches, file MD5 preservation, FULL resident formatting,
MD5-compatible diagnostic suppression, and balanced buffers, file handles and
loaded segments. A named MorphOS `RES` lookup now also searches DOS resident
command segments through `FindSegment` (both system lists), copies/parses
segment `$VER:` data, and routes internal/disabled segments through the
`shellcmd` resident. Default named lookups now use DOS `FilePart` before
checking the Exec Resident table. They then scan the Exec `LibList` under
`Forbid`/`Permit`, use Utility `Stricmp` for name matching, copy the library
node name before releasing the list lock, and support numeric and `FULL`
library `IdString` output. The caller-owned `utility.library` v37 lease closes
on success and lookup failure; a missing utility library reports the expected
error. `RES` also uses the source's `FilePart` basename. Default named lookup
now follows the source through Exec `LibList`, `MOSSYS:LIBS/`/`LIBS:` file
candidates, Exec `DeviceList`, and `MOSSYS:DEVS/`/`DEVS:` file candidates.
After those paths miss, non-`RES` named lookups try the original file path
before the final `FindSegment` lookup. Volume names bypass the ordinary
Resident, LibList, and Utility search, then use `GetDeviceProc`; volume tasks
are mapped to device nodes under the DOS-list read lock before scanning the
handler segment for a resident. The 94-vector suite exercises a direct device
node, a volume mapping after a nonmatching node, the missing-device path
through `FindSegment`, missing DevProc/handler-segment paths, a handler segment
without a Resident, requested-version comparison, MD5-unavailable output, and
terminal `ObjectNotFound` reporting when neither a handler nor command segment
exists.
The cases check balanced `FreeDeviceProc` and DOS-list unlocks. The direct-file
receipt is
`artifacts/version-morphos-native-20260924-directfile-v3/qualification.json`.
The volume receipt is
`artifacts/version-morphos-native-20260924-volume-v6/qualification.json`.
These are fixture results; original-guest parity, exact error/IoErr precedence,
remaining fallback differences, PURE/resident lifecycle, licensing and package
admission remain open. Shipping
stays at 0/200 commands and 0/246 profiles, and the Workbench media/installed-system reference gates remain open.

The 2026-09-27 Workbench Version candidate implements the observed default
lookup sequence through Resident, command segment, trailing-colon DOS device,
Exec `LibList`/`DeviceList`, direct-name file and `LIBS:`/`DEVS:` files. Its
latest fixture qualification passes 92 vectors per CPU across 68000/020/040
(276 total):
`artifacts/version-wb31-native-20260927-files-v5/qualification.json`.
The clean 68000 HUNK built from the current source has the same 14,552-byte
image and SHA-256
`48260077d3f997bd5262d4a348a3197cba8a5d015cccadd3c47b612a4ba8ec91`.
Original and candidate guests now match for three bounded default-provider
cases, including output, return and caller post-System IoErr:

- `C:Version LIBS:version.library`:
  `artifacts/workbench31-guest-command-version-default-files-candidate-20260927-v3/effect-comparison.json`.
- `C:Version DEVS:clipboard.device`:
  `artifacts/workbench31-guest-command-version-devs-explicit-candidate-20260927-v1/effect-comparison.json`.
- `C:Version clipboard.device` (3,600-frame capture; the shorter window ended
  before the HUNK `LoadSeg` fallback returned):
  `artifacts/workbench31-guest-command-version-clipboard-basename-candidate-20260927-v1/effect-comparison.json`.

The two device cases print `clipboard.device 38.8\n` (22 bytes), return 0,
and leave caller IoErr 0. A bounded miss case,
`C:Version missing-device-copper-test.device`, remains a concrete mismatch:
the original emits `object not found\n` with return 20 and caller IoErr 205;
the candidate emits `function not implemented\n` with return 20 and caller
IoErr 236. Receipt:
`artifacts/workbench31-guest-command-version-bare-miss-candidate-20260927-v1/effect-comparison.json`.
These comparisons do not close general lookup order, provider errors or the
full command. Remaining media/reference closure, command and profile coverage,
broader guest parity, PURE/resident lifecycle, rights and packaging still
block completion; shipping stays 0/200 commands and 0/246 profiles.

The 2026-09-27 Workbench `Which` increment removes the arbitrary 64-entry
CLI-path cap and implements the captured distinction between bare-name and
path-qualified lookup. Its current resident HUNK passes 21 supplied vectors
per CPU on 68000/020/040, including a 65-entry `ALL` walk, cyclic-list cleanup,
and C: fallback cases. Twenty exact original/candidate guest comparisons now
match, covering C: directory output, a missing C: entry, bare-name and
explicit-path `ALL`, and all eight classic switches for one found non-internal
name and one missing name. The detailed receipts are in the `Which` contract
and progress log. This remains bounded CC01/CC10 evidence: other name
categories, route/assign cases, MorphOS alias behavior, PURE/resident lifecycle,
packaging and the remaining profile gates stay open.
Shipping remains 0/200 commands and 0/246 profiles.

### Current blocker evidence (2026-09-25)

CC20's AddDataTypes investigation has hash-bound both command references and
the Workbench 3.1 companion `Libs/DataTypes.library` on the selected M10 disk.
Static disassembly confirms Workbench list-head and descriptor offsets, the
shared-list refresh-date field, dependency floors and refresh gate. A separate
Workbench profile body now exists. Its resident 68000/020/040 HUNK passes fourteen
supplied-vector cases per CPU for classic library leases, optional catalog,
existing-list reuse, named-list creation/publication, the three-result
`FILES/M,QUIET/S,REFRESH/S` `ReadArgs` contract, single and multiple FILES
patterns, DTHD registration, combined option slots and REFRESH precedence,
classic REFRESH date checks and wildcard scans, null `no_Object` cleanup, and
parser/open-failure cleanup.
The named-object tag vector and publication order are fixture-checked; a
partial-open `IoErr` preservation bug found by the fixture is fixed. The MorphOS
regression also passes 38 command cases plus its callback probe per CPU. These
are synthetic fixture results. Complete Workbench list ABI and ownership,
MorphOS 3.20 private-library correspondence, real guest comparisons, callback
loader/unload behavior, installed `PURE`/resident classification, rights and
package admission remain open. See the AddDataTypes contract and profile
reference audits; shipping remains 0/200 commands and 0/246 profiles.

### Current blocker evidence (2026-09-20)

The latest bounded work does not remove the shipping blockers. MorphOS
`SetClock` now has a source-bound contract and a resident 68000/68020/68040
receipt with seventeen supplied DOS/Exec/resource/timer invocations per CPU;
the refreshed receipt is
`artifacts/setclock-morphos-native-20260920-utc-v3/qualification.json`.
This proves the classic and MorphOS 52+ UTC vector ABIs and invocation-owned
cleanup under a fixture. It does not prove a real battclock/timer device,
original guest output or result timing, PURE/resident reuse, installed flags,
or package admission.

Both MorphOS and Workbench 3.1 `List` now have bounded source/media-candidate
resident stages. MorphOS passes seventeen supplied DOS-vector invocations per
CPU; the Workbench candidate passes eighteen per CPU after adding an explicit
missing-DOS boundary. Both pass on resident 68000/68020/68040 HUNKs, covering
flat matcher traversal, pattern/type filters, quick/block/no-header rendering,
`TO` output lifetime, parser/allocation/Ctrl-C, unsupported modes, startup
boundaries, and interleaved callers. This advances CC11 implementation evidence
but does not close either packed correspondence, Workbench runtime parity,
guest differential, PURE/resident lifecycle, or package gates.

The 2026-09-23 `List` refresh implements MorphOS literal case-insensitive
`SUB` filtering through public DOS pattern APIs, escaping DOS pattern
punctuation and applying it in conjunction with `P=PAT`. The shared row path
shows `KEYS` only when requested, renders documented size/protection/date/time/
comment fields, and implements `NODATES` by omitting date/time columns and
skipping DOS `DateToStr`. `SINCE` and `UPTO` use public DOS `StrToDate` for
inclusive date-only filters. Explicit `DATES` retains the full-row default;
`QUICK` takes precedence and renders names only, even when paired with
`DATES`. Fresh resident 68000/020/040 fixtures pass 31 MorphOS and 32
Workbench supplied-vector cases per CPU in
`artifacts/cc11-list-morphos-native-20260923-all-v1/` and
`artifacts/cc11-list-wb31-native-20260923-all-v1/`. The fixture covers nested
descent/return and suppressing the DOS directory-finished marker. It does not
establish original recursive path rendering, sort/owner/`LFORMAT` behavior,
exact header/summary output, guest parity, packed correspondence,
PURE/resident reuse, licensing, package placement or differential evidence.

MorphOS 3.20 `Dir` now has a bounded source-grammar resident stage. Its
three-CPU receipt passes fifteen supplied DOS-vector invocations per CPU for
ReadArgs ownership, `OPT` filtering, public `Lock`/`ExAll` enumeration, output,
allocation/parser/startup guards and interleaved calls. The receipt is
`artifacts/dir-morphos-native-20260917-qualified-v2/qualification.json`.
Recursive `ALL`, interactive `INTER`, wildcard matching, soft links, packed
correspondence, original guest behavior and all admission gates remain open.
The same worker is exposed through a separate DOS 36 Workbench 3.1 syntax
candidate; its refreshed receipt
`artifacts/dir-wb31-native-20260920-boundaries-v3/qualification.json` passes
sixteen supplied vectors per CPU, adding explicit startup and missing-DOS
boundaries. Classic parser/output behavior, packed correspondence and
admission remain unproven.

MorphOS 3.20 `MakeDir` now has a source-informed `NAME/M,ALL/S` body based on
the independently inspected 50.4 source release. Its DOS 37 resident entry
builds cleanly for 68000/020/040 with zero managed runtime features, helpers,
external targets, exception regions or fatal fault sites. An independent
compiled-HUNK fixture passes fifteen supplied public-DOS invocations per CPU,
covering ordinary and `ALL` path walking, current-directory restoration,
parser/allocation failures, diagnostics, startup boundaries and interleaved
callers; the receipt is
`artifacts/makedir-morphos-native-20260917-runtime-v10/qualification.json`.
The packed command's exact parser/output/diagnostics, real filesystem
lifecycle, guest parity and admission gates remain open.

Workbench 3.1 `MakeDir` now has a DOS 36 resident entry around the verified
`NAME/M` body. The entry handles the original missing-DOS secondary-result
boundary, rejects Workbench startup and malformed argument buffers, and uses
raw BPTR calls so its 68000/020/040 HUNK closure has zero managed runtime
features, helpers, external targets, exception regions or fatal fault sites.
The independent fixture passes fourteen supplied public-DOS invocations per
CPU, covering result ownership, empty and multiple vectors, existing/create/
failure ordering, command-owned diagnostics, parser/allocation failures,
startup boundaries and interleaved callers. Receipt:
`artifacts/makedir-wb31-native-20260917-runtime-v2/qualification.json`.
Packed correspondence, original guest behavior, real filesystem effects,
installed PURE/resident lifecycle, packaging and differential parity remain
open.

Workbench 3.1 `Assign` now has a bounded DOS 36 resident entry for the
hash-bound classic grammar. Its three-CPU HUNK receipt passes nineteen supplied
public-DOS invocations per CPU, covering replacement, ADD, REMOVE, DEFER and
PATH mutation, multi-target lock ownership, operation/lock failures,
parser/result-allocation failures, invalid vectors, startup boundaries and
interleaved callers; the HUNK closure has zero managed runtime features,
helpers, external targets, exception regions or fatal machine-fault sites and
the fixture reports no shared-image writes. Receipt:
`artifacts/assign-wb31-native-20260917-runtime-v4/qualification.json`.
LIST, EXISTS, DISMOUNT, VOLS/DIRS/DEVICES, packed correspondence, original
guest behavior, lifecycle/PURE admission, packaging and differential parity
remain open. MorphOS now has a separate three-CPU runtime receipt for the
same bounded mutation slice at
`artifacts/assign-morphos320-native-20260917-runtime-v3/qualification.json`;
its packed correspondence and full profile gates remain open.

Join now has supplied public-DOS runtime receipts for both profiles. The DOS 36
Workbench entry and DOS 37 MorphOS entry each pass eighteen cases per CPU on
68000/020/040 from one resident HUNK, covering ordered source streams,
exact read/write and EOF handling, destination cleanup, parser/open/workspace,
result/buffer-allocation and Ctrl-C failures, empty input, startup and
missing-DOS boundaries, malformed
argument buffers, and interleaved callers. Receipts:
`artifacts/join-wb31-native-20260918-runtime-v3/qualification.json` and
`artifacts/join-morphos320-native-20260918-runtime-v2/qualification.json`.
This closes only a bounded native ABI/ownership checkpoint; exact guest parser
and diagnostics, wildcard/no-match policy, real handler behavior, packed
correspondence, original PURE/resident lifecycle, packaging and differential
parity remain open.

MorphOS `RequestChoice` now has a source-bound DOS 37 resident body and an
independent Intuition/timer fixture. Its 68000/68020/68040 HUNK receipts pass
twelve supplied invocations per CPU, covering parser and Intuition-open
failures, percent-body input, no-timeout and timeout requester paths, Ctrl-C,
public-screen lock failure, temporary text allocation failure, timer-open
fallback, missing-DOS Result2 startup, Workbench startup, and repeat
ownership. A two-caller interleaving and timer-allocation fallback extension
now brings the receipt to sixteen supplied invocations per CPU. The receipt is
`artifacts/requestchoice-morphos-native-20260918-runtime-v1/qualification.json`.
This does not prove original guest UI behavior, Workbench correspondence,
interactive lifecycle, PURE/resident admission, packaging, or differential
parity.

The 2026-09-19 Date, Info, DevList, ModList, PortList, and ResList receipts,
together with the 2026-09-20 TaskList receipt,
advance the same bounded native checkpoint. They prove resident three-CPU
HUNK execution against supplied public vectors, with the recorded ownership
and shared-image checks, but they do not satisfy the four shipping rows below:
the reference media and installed `C:` placement are still incomplete, no
original-guest differential has run, resident/PURE lifecycle and full provider
ownership remain open, and no artifact has been admitted to the release
manifest. The source-backed Mount and Format contracts are intentionally also
blocked on real Expansion/trackdisk providers and destructive-media evidence;
they must not be represented by fixture-only command bodies.

MorphOS `DiskFree` now has a source-bound resident stage. The exact
`VOLUME,NOPOSTFIX/S,PERCENT/S` template is recorded, and the body uses public
DOS `Lock`/`Info`, invocation-owned `InfoData` and formatting storage, and
bounded byte/percentage output modes. Its three-CPU static receipt is
`artifacts/diskfree-morphos-native-20260919-static-v3/qualification.json`; the
supplied-vector runtime receipt is
`artifacts/diskfree-morphos-native-20260919-runtime-v3/qualification.json`,
with fifteen invocations per CPU covering current/explicit volumes, byte and
percentage modes, large byte and overflow-safe percentage arithmetic,
parser/provider/allocation failures, startup boundaries, cleanup and
interleaved callers. Localized output, provider behavior outside the fixture,
original-guest differential, PURE/resident lifecycle, and package admission
remain open.

The Workbench 3.1 Mount profile is now a separate bounded static checkpoint.
The packed 6,880-byte binary, exact outer `DEVICE/M,FROM/K` grammar, and
31-slot inner template are captured in the Mount contract; its resident
68000/020/040 HUNK receipt is
`artifacts/mount-wb31-native-20260919-wildcard/qualification.json`. The
profile-specific `FILESYSTEM` slot is preserved, but this does not close
Workbench source selection, handler/media execution, startup lifecycle,
failure precedence, PURE, original-guest differential, or package admission.

The shared Mount implementation now includes the source-observed `DEVS:MountList`
fallback for both profiles. After the five DOSDrivers probes miss, it reads
the file with public DOS `Seek`/`Read`, applies `preparefile` normalization,
selects the requested block with `ReadItem`, and parses that block through the
profile-specific inner `ReadArgs` template. Missing DOSDriver files also use
icon.library v37 to inspect the matching `.info` DiskObject, filter comment and
`IM?=` tool types, and aggregate eligible tool text through one guest-owned
`ReadArgs` transaction. Both resident entries also walk Workbench startup
argument lists, switch and restore each supplied lock, and reply the startup
message after cleanup. The MorphOS and Workbench receipts are static ABI
checkpoints only; real icon-library/handler/media providers, resident/PURE
lifecycle, original-guest differential behavior, and package admission remain
open.

The remaining closure items are therefore concrete work queues, not generic
implementation debt:

| Blocker | Required evidence before a profile can ship |
| --- | --- |
| Reference/media closure | Capture the remaining Workbench disks, installed `SYS:C`/`MOSSYS:C` placement, exact hashes, and per-profile templates, help, diagnostics, result levels, `IoErr`, flags, and PURE/resident events. |
| Guest parity | Run the replacement and the original command on disposable Kickstart 3.1 and MorphOS guests, comparing bytes, guest state, errors, and return ownership for every frozen option path. |
| Lifecycle and concurrency | Exercise resident reuse, same-segment calls, repeated and overlapping invocations, allocation/device/list failures, Ctrl-C, and target disappearance without leaks or shared-image writes. |
| Native/package admission | Compile each selected image for 68000/020/040, verify resident and PURE constraints against the original design, resolve source licenses/dependencies, and admit only the correct normal-install or installation-media package. |

The standalone `tests/Commands.NativeExecution/QualificationScriptRegressionTests.ps1`
is no longer a local blocker: its fail-closed, drift, framework-closure and
HUNK-tail checks pass on both Windows PowerShell 5.1 and PowerShell 7. The
qualification harness remains useful preflight evidence, but it does not
change the external reference, guest, lifecycle, purity or package gates.

Until those four rows are satisfied for all applicable profiles, the ledger
must remain at 0/200 shipping identities and 0/246 shipping profiles even when
an individual bounded receipt passes.

Goal-mode execution should clear these gates in order: close one command's
reference contract, implement both profile bodies using public Kickstart APIs,
qualify the native resident/pure artifact and real guest lifecycle, run the
differential matrix, then stage its package entry and update the ledger. Do not
count a command as shipping until every required profile row has passed.

Include installation-media-only tools even when a normal hard-disk installation
omits them. Record their proper distribution location and package them in the
installation-media profile. Do not install every maintenance helper into the
normal boot image merely to increase the command count. Unrelated third-party
additions and the optional MorphOS SDK's Unix toolchain are outside the baseline
unless actually supplied in its system `C:` directories. Exclusions require
path/version evidence, not an inference from a command's name or complexity.

## Existing work to preserve

The source currently implements and dispatches these **31** distinct internal
names in [ShellInternalCommand.cs](D:/Koodit/GIT/CopperOS/src/System/Shell/ShellInternalCommand.cs)
and [ShellCommandDispatcher.cs](D:/Koodit/GIT/CopperOS/src/System/Shell/ShellCommandDispatcher.cs):

```text
Alias Ask CD Cls Echo Else EndCLI EndIf EndShell EndSkip Failat Fault
Get Getenv If Lab NewCLI NewShell Path Prompt Quit Resident Run Set
Setenv Skip Stack Unalias Unset Unsetenv Why
```

Older prose says 32; the resolver tests include both `Echo` and `echo` as case
coverage. Reconcile the count without inventing a missing command. These names
are regression dependencies, not a new external implementation backlog. Do not
create duplicate executables unless verified reference media actually provides
an external entry with that name; such an entry must share existing semantics.

[ExecuteCommand.cs](D:/Koodit/GIT/CopperOS/src/Commands/ExecuteCommand.cs) is the existing external-command wrapper. It
uses `FILE/A` and delegates to the Shell. Preserve it, but verify script-tail
arguments and directives: the wrapper now separates FILE using the DOS
`ReadItem` boundary, applies `ReadArgs(FILE/A)` only to that prefix, and keeps
the untouched suffix in the invocation-owned script runner/frame. The script
engine binds a first-line `.KEY`/`.K` template, supports script-wide `.DEF`
defaults, and performs its current `<name>` substitution through `ReadArgs`.
The current source-ordered dot-directive behavior has a hash-bound Workbench
fixture and runner-owned temporary-input coverage; diagnostics, fault behavior,
MorphOS comparison and a standalone shipping executable remain open. There must
remain one Shell parser, one script engine, and one owner of aliases and CLI
state.

Reuse `CommandInvocation`, DOS-owned `TryReadArgs`/`FreeArgs`, guest workspaces,
redirection, script frames, process continuations, and resident use-count
tracking. The Shell progress ledger still records boot, requester, resource,
differential, and purity-manifest gaps. Fix only integration gaps needed here;
do not restart the Shell implementation or call those gaps completed.

`filesystem/SYS/C/` currently has no shipped command binaries. The existing
[Shell qualification script](D:/Koodit/GIT/CopperOS/tools/Shell/qualify_native.ps1)
produces MC68000 HUNK and MC68020/040 assembler
outputs; its compile reports are not evidence of three executable variants or
of successful resident execution. Existing unrelated MUI edits, logs, and
untracked goals must remain untouched.

## Reference evidence and remaining discovery

Use original media, matching SDK/autodocs, installation scripts, and controlled
reference executions together. Documentation indexes help locate contracts;
they do not establish release membership or protection flags on their own.

| Authority | Evidence available when this plan was written | Required follow-up |
| --- | --- | --- |
| Original Workbench 3.1 media under `D:/TestData/TestImages/` | `Workbench3.1:C` has 50 files; `Install3.1:C` has 20, adding eight distinct helpers. ADF directory checksums and protection fields were inspected read-only. | Record exact archive/member/image hashes; reconcile the other four distribution disks, machine variants, and a clean installation. |
| Original developer CD at `D:/TestData/AmigaDeveloperCD.iso` | `/NDK_3.1/DOCS/DOC/DOS.DOC`, `/NDK_3.1/INCLUDES&LIBS/STARTUPS37/STARTUP.ASM`, and `WRITINGREENTRANTC` provide original DOS/startup/reentrancy guidance. | Extract only needed licensed reference evidence; bind declarations to the original FD/header/autodoc versions. |
| [Official MorphOS 3.20 ISO](https://www.morphos-team.net/morphos-3.20.iso) and [downloads](https://www.morphos-team.net/downloads) | Read-only HTTP range inspection found 188 files in ISO `MorphOS/C/`; root `C/` is empty. The image advertises 471126016 bytes and published MD5 `70b84b8c0bb1cf9b10b7062fe8809c85`. | Validate a complete image hash before final reference qualification; range inspection did not establish whole-image integrity. Capture installed `MOSSYS:C`, `SYS:C`, assigns, filtering, and metadata. |
| ISO `hdinstall.fixc` and `MorphOS/S/startup-sequence` | Installer adds P to 88 named commands. Startup temporarily makes Assign and Execute resident. | Verify actual installed flags and resident lifecycle; do not equate installer intent with observed runtime state. |
| [MorphOS 3.20 release notes](https://www.morphos-team.net/releasenotes/3.20) | Release-specific additions and changed options prevent reliance on older templates. | Link each changed contract to its exact binary/help capture and relevant SDK API. |
| [MorphOS command index](https://library.morph.zone/Shell_Commands) | Useful documentation discovery index; differs from actual media for HunspellService and Freeze. | Reconcile every index entry with media and version evidence. It is not the authoritative file manifest. |
| [MorphOS source releases](https://www.morphos-team.net/sources) | The 3.20 command source archive is partial; licenses differ between files/components. | Audit licenses before reuse. Do not mistake partial source availability for complete command coverage or permission to copy everything. |

The modern [AmigaOS command reference](https://wiki.amigaos.net/wiki/AmigaOS_Manual:_AmigaDOS_Command_Reference)
contains later additions. Use it for navigation only until a statement is
confirmed against the original 3.1 media/manuals. In particular, do not import
later command options or assume a modern purity list describes v40 binaries.

Reference facts that need explicit treatment:

- `Filenote` on Workbench and `FileNote` on MorphOS are one case-insensitive
  command identity; retain each reference spelling as metadata.
- `HunspellService` is on the MorphOS ISO but absent from the command index.
  Its real spellchecking service and IPC contract require implementation.
- `Freeze` was introduced in the [MorphOS 2.1 release notes](https://www.morphos-team.net/releasenotes/2.1),
  but was not found anywhere in the inspected 3.20 ISO directory tree. Do not
  silently omit it or assert it is a 3.20 media file.
- MorphOS `CLI` has its own launch/window behavior. It is not automatically a
  synonym for the existing `NewCLI` or `NewShell` internal command.
- Capture `HDWrite` from installation media and verify its 3.20 option set;
  older documentation omits the SectorSkip, ASyncIO, and MD5 additions.

Do not run an unknown command with `?` on a live reference system merely to
discover its syntax. Inspect documentation, binary strings, or licensed source
first; dangerous, persistent, and hardware commands need disposable guests.
Never collect or commit ROMs, original binaries, passwords, keys, personal files,
or unrestricted reference dumps as test fixtures.

## Compatibility design

### Two behavior profiles, shared implementation

Provide `wb31` and `morphos320` command profiles. They select exact templates,
defaults, diagnostics, result rules, and packaging metadata where the references
differ. Share the implementation of common operations; do not fork two complete
codebases. Use build/install selection or explicit invocation metadata, not new
user-visible compatibility switches that change original command syntax.

The normal CopperOS distribution includes the union of normal-runtime names:
shared commands use the MorphOS profile by default, and Workbench-only commands
retain their Workbench contract. An exact classic distribution selects the
Workbench profile and original placement instead. Maintenance-only helpers belong
in the installation-media manifests. Build outputs for the profiles stay separate
before selecting what is staged into `filesystem/SYS/`.
An exact old `?` template and a newer extended template cannot both be advertised
as the same profile's contract.

For every option, preserve spelling, aliases, positional/keyword rules, accepted
values, defaults, precedence, interaction, side effects, output, cancellation,
return level, and `IoErr`. Test successful and unsuccessful behavior. Do not
replace an observable original behavior with a preferred modern interpretation.
Document unavoidable differences; unresolved differences remain open work.

### Kickstart 3.1 first

Every ordinary command is a standalone freestanding MC68000 Amiga HUNK using
public OS calls, not a binary linked with CopperStart DOS/Exec implementations.
Also build executable MC68020 and MC68040 variants without a mandatory FPU.
Do not infer PPC binary compatibility from reproducing MorphOS command behavior.

Qualify classic command paths against both actual Kickstart 3.1 and CopperStart.
MorphOS-only features can require additional guest libraries, handlers, devices,
or kernel services. Those dependencies must be implemented or supplied and tested
through their public interfaces. They are not Kickstart 3.1 features merely
because the front-end command is compiled for 68k. Failure when a dependency is
absent is necessary error coverage, not proof of the command's full functionality.

| Operation | Prefer the original public boundary | Implementation rule |
| --- | --- | --- |
| Options and help | DOS `ReadArgs`, `FreeArgs`, `AllocDosObject`, `FreeDosObject`; `ReadItem` only where appropriate | Native ReadArgs is the option authority for template-based commands. Preserve non-ReadArgs grammars of ported tools. |
| Streams and diagnostics | DOS `Input`, `Output`, `Read`, `Write`, buffered I/O, `VPrintf`/`VFPrintf`, `Fault`/`PrintFault`, `IoErr`/`SetIoErr` | Exact-write handling and command-specific results; no host console/filesystem substitute. |
| Paths and enumeration | DOS `Lock`/`UnLock`, `Examine`/`ExNext`, `ExAll` where supported, `MatchFirst`/`MatchNext`/`MatchEnd`, `ParsePatternNoCase`/`MatchPatternNoCase`, `FilePart`/`PathPart`/`AddPart` | Share traversal plumbing; retain each command's ordering, pattern, link, and interactive rules. |
| Namespace and metadata | DOS assigns, `CreateDir`, `Rename`, `DeleteFile`, `MakeLink`/`ReadLink`, `SetComment`, `SetProtection`, `SetFileDate`, `SetFileSize` | Validate handler capabilities and integer ranges. Never truncate a newer 64-bit operation through a classic 32-bit ABI. |
| Volumes and handlers | DOS `Info`, DOS-list locking, `GetDeviceProc`/`FreeDeviceProc`, `AddBuffers`, `Inhibit`, `Relabel`, `Format`, documented packets | Keep filesystem policy and on-disk formats with handlers, not individual commands. |
| Processes and resident segments | DOS `RunCommand`, `SystemTagList`, `CreateNewProc`, `LoadSeg`/`UnLoadSeg`, `FindSegment`/`AddSegment`/`RemSegment`; Exec signals | Use the current Shell/process owner and native loader contract; do not embed another scheduler or resident registry. |
| Time and waiting | DOS `DateStamp`, date conversion, `Delay`, notifications; Exec waits/signals and `timer.device` | Use guest time and correct break semantics. No host sleeps or busy loops. |
| Desktop and preferences | Version-appropriate Intuition, Workbench, icon, ASL, graphics, diskfont, locale, datatypes, IFFParse, clipboard/keymap/device interfaces | Verify which calls exist in 3.1. Later Workbench or MUI APIs require a real additional provider. |
| Hardware, network, media, languages | Validated SDK interfaces for the owning guest subsystem | Feature detection, ownership, licensing, and real success-path integration are required. |

CC02 records minimum library versions and availability for every consumed vector;
the table is an API-selection guide, not a declaration that every extended
operation exists in ROM. Public ABI declarations belong in
`../CopperSharp68k/Sdk.Amiga`; missing portable OS semantics belong in
`../CopperStart`; command behavior belongs in `src/Commands`. Follow each
sibling repository's instructions before modifying it.

### Capability-floor rule for system dependencies

For every consumed library, device, resource, or ROM vector, record two
separate versions: the minimum version requested by the captured original and
the lowest version that actually supplies the calls, structures, and semantics
used by the replacement. Admission and startup must use the latter capability
floor. A higher original `OpenLibrary` request is differential evidence, not an
automatic implementation requirement. For example, a command whose required
DOS calls are present in DOS 36 must request DOS 36 (or a lower version only
when the same calls and semantics are verified there), even if its original
binary requested DOS 37. Apply this rule equally to `dos.library`, other
libraries, devices, resources, and versioned provider extensions. A requested
minimum of `0` means “no minimum version requirement”; it is valid when the
replacement consumes no versioned vector from that provider. It does not claim
that a version-zero library exists. Never lower a floor speculatively: if a
required call or layout is unavailable, retain the higher floor and record the
supporting API evidence and unresolved parity.

### Native execution and state

Use constrained, fixed-width, C-like C# consistent with the existing project:
no managed allocation/runtime, exceptions, reflection, delegates, tasks, host
services, or hidden static mutable state in native-reachable code. Host tools
and test harnesses may use .NET. Use named packed ABI structures and bounded
guest-memory codecs; distinguish APTR, BPTR, BSTR, and C strings explicitly.
Where language or media contracts require floating-point values, preserve their
actual precision/ABI and provide freestanding software support when necessary;
do not silently replace them with approximate integer behavior.

Verify command entry registers, preserved registers, stack, CLI versus Workbench
startup, argument lifetime, return D0, and process result/error propagation.
Do **not** copy the older goal's unverified assumption that incoming A6 is
DOSBase. Original startup code obtains ExecBase and opens DOS explicitly. Any
CopperStart-specific entry adapter must remain distinct from the portable
Kickstart command entry.

Keep library bases, RDArgs/results, buffers, directory locks, streams, and error
state invocation-local. Audit compiler-generated library-base and BSS slots as
well as source fields. Borrowed resources stay borrowed; retained service state
must be explicitly transferred to an owning process/service. Restore temporary
current-directory, requester, signal, and stream changes on every exit.

Call actual `ReadArgs`/`FreeArgs` for DOS templates. Zero/init result slots,
interpret `/N` as a pointer to a number and `/M` as a terminated pointer vector,
and respect RDArgs-owned lifetimes. Explicit input buffers must match the
versioned newline/source rules; reused RDArgs need their buffer state reset.
Caller-allocated RDArgs require their own final release. Preserve interactive
`?` prompting rather than replacing it with print-and-exit help. See original
`DOS.DOC` and the [ReadArgs autodoc](https://amigadev.elowar.com/read/ADCD_2.1/Includes_and_Autodocs_3._guide/node01A1.html).

Commands with Unix-style flags, an editor language, expressions, an installer
language, ARexx, or Lua retain those grammars. Use ReadArgs for their outer DOS
template only when the reference actually has one. This exception must be
recorded per command; it is not permission to rewrite DOS option parsing.

## Pure and resident contract

Track these independently for **each profile and artifact**:

1. Observed original file protection bits, including P and S.
2. Original documented reentrancy/re-execution requirement.
3. Startup or installer resident registration, force policy, and removal.
4. Replacement binary's independently verified purity and execution lifetime.

All inspected Workbench C files have a clear P bit in their ADF metadata. That
does **not** make all classic commands non-pure: original startup explicitly
loads Assign and Execute with `Resident ... PURE`, then removes them. Install
scripts likewise force resident use of Delete, IconPos, Reboot,
ExtractKickstart and FindResident. Preserve both facts;
finish the original installed-metadata and design-contract audit before deciding
output protection policy.

MorphOS `hdinstall.fixc` adds P to 88 commands listed in Appendix B. This creates
an explicit requirement to keep those implementations safely reusable. Absence
from that script is **unknown**, not a non-pure classification. Installed flags,
documentation, and lifecycle evidence may establish more requirements. MorphOS
startup also temporarily makes Assign and Execute resident and later removes
them. A DOS resident command, an Exec resident module, and a long-lived background
process are different things; do not exchange one design for another.

For each required pure command:

- Code and constant hunks are shared and never written during invocation.
  Mutable state belongs to that invocation or a documented OS service owner.
- Use existing compiler support if it is sufficient. Otherwise add a validated
  pure-command mode with per-invocation writable data/library slots, relocation
  handling, and initialization/teardown. Do not require a particular A4 scheme
  without checking compiler/register constraints.
- Run repeatedly and concurrently from **one loaded SegList**, in different
  processes with different arguments, streams, directories, errors, signals,
  stack sizes, and failure injection. Loading separate copies is not this test.
- Verify shared-image integrity, register/stack preservation, no state carried
  from a prior run, and correct resource ownership. Failure then success and
  overlapping success/failure invocations are mandatory cases.
- Test add/use/remove/replace, deferred load where applicable, use counts,
  attempted removal while active, launch failure, and process termination using
  the existing Shell Resident/DOS implementation.

For commands that reboot, shut down, or transfer control permanently, the native
harness must observe the terminal handoff without forcing a false return into
production code. Prove repeated/concurrent image safety at that boundary using
isolated guest providers, and qualify the actual reset/shutdown separately in
disposable boots. Do not weaken the lifecycle contract merely to fit a return-based
test harness.

Preserve genuine original non-pure/singleton/daemon lifecycle behavior instead
of setting P indiscriminately. Service allocations and ports may survive only
with a documented live owner and a tested shutdown path. A pure launcher may
start a service; the launcher's shared segment must not retain its transient
state or be unloaded while a callback still points into it.

Bind purity reports to binary hash, profile, CPU, compiler revision/options, SDK,
and dependency closure. Packaging must refuse P when required evidence is absent
or stale. Do not weaken the existing verified-Pure admission policy to make tests
pass. Preserve documented forced/unsafe Resident behavior without promoting it
to verified status. An unqualified command that was originally pure remains
incomplete even if it runs when loaded afresh.

Stock Kickstart retains its own Resident admission behavior. Build-time reports
must produce ordinary compatible artifacts and protection metadata; they must
not add a host manifest dependency to a native command running on original DOS.

## Goal execution protocol

Use this file as the stable contract. Maintain execution state in the planned
`docs/Commands/Workbench31MorphOS320/` directory:

| Artifact | Required content |
| --- | --- |
| `authorities.md` | Versions, paths/URLs, media and SDK hashes, license decisions, capture method, and unresolved reference questions. |
| `command-inventory.json` | One canonical command identity; profile/source locations; aliases; grammar/template; all option contracts; dependency/API/version map; original flags and resident behavior; evidence links; owner step. |
| `build-manifest.json` | Per-profile/CPU executable or script identity, source/native root, output/install paths, minimum stack, version ID, dependencies, hash, required and proven purity, and report paths. |
| `completion-ledger.md` | Per-command/profile specification, options, semantic, native, purity, packaging, and differential status; dependency IDs; exact next slice. |
| `progress-log.md` | Dated completed slices, files/revisions, commands run, outcomes, failures, and next actions. |
| `qualification-report.md` | Coverage totals and uncovered cases, baseline matrix, resource/purity/native/boot results, and remaining blockers. |
| `fixtures/` and `reference-captures/` | Small redistributable input fixtures and sanitized expected results; metadata and hashes for private references kept elsewhere. |

Each command-family stage below is executed as **one command at a time**, using
stable slice IDs such as `CC13.Copy.1`:

1. **Contract:** establish that command/profile's complete option and lifecycle
   matrix and reference evidence. Unknown grammar is not an implementation spec.
2. **Behavior:** implement one bounded feature/option group through shared public
   adapters; add meaningful semantic cases. Large commands use numbered sub-slices.
3. **Failures and integration:** complete error, cancellation, ownership, and
   interaction cases against the owning guest subsystem and existing Shell.
4. **Native and packaging:** execute native code, qualify required purity, build
   the correct artifact/metadata, and record differential evidence or the precise
   reference gap. A command is complete only when all required gates pass.

Do not mark a family complete after its first command or an option parser. Do
not turn a missing device/library into an unconditional success stub. Keep open
dependencies and missing reference evidence visible, continue independent eligible
work, and split large provider work into named bounded slices. Progress on this
goal does not authorize changing unrelated active goals or replacing user edits.

The order is foundations CC00-CC09, command families CC10-CC38 according to
dependencies, explicit discrepancy closure CC39, then final gates CC40-CC44.
Specifications and independent families can be researched in parallel; shared
SDK/compiler/DOS mutations need one coordinated owner. A blocked advanced family
must not prevent completion of an independent classic command.

Each family's dependencies are a union to resolve **per command and mode** in the
ledger, not a blanket barrier. For example, Trashcan's desktop service does not
block Copy/Delete/Join, and CLI's launch prerequisites do not block Eval/Quote.
CC01 can likewise be completed command by command while wider reference discovery
continues. Final completion still requires all applicable dependencies and evidence.

Suggested later execution request:

> Start a goal using Goals/WORKBENCH31_MORPHOS_C_COMMANDS_GOAL.md. Execute the
> next eligible bounded slice, preserve the existing Shell, and continue through
> the plan. Record evidence and the next slice after every increment. Keep the
> goal incomplete until the full inventory, options, pure/resident behavior,
> native packaging, and required reference comparisons are qualified.

## Foundation stages

### CC00 - Close the versioned media inventory

- [ ] Freeze Workbench 3.1 and MorphOS 3.20 reference versions and image hashes.
- [ ] Enumerate all six original Workbench disks, relevant machine variants,
  installer helpers, clean installed directories, MorphOS ISO directories and
  installed `C:` overlay/search rules. Preserve file case, aliases, versions,
  protection flags, file type, hashes, and original placement.
- [ ] Seed the inventory with all 200 Appendix A rows and an explicit Freeze
  discrepancy. Compare it with both documentation indexes and media. Add every
  newly discovered in-scope command to a named family; retain an audit trail.
- [ ] Record original startup/installer Resident use and reconcile differing
  media/installed P bits. Establish the initial progress and qualification files.

**Exit:** every discovered file/index entry has an evidenced identity and owner;
unknown original-media coverage is visible and cannot count as final completion.
If a reference is unavailable, preserve its open closure item and work on verified
commands rather than guessing missing names or freezing an incomplete total.

### CC01 - Freeze per-command options and observable contracts

Depends on each command's CC00 evidence; can proceed incrementally.

- [ ] Capture exact templates or non-DOS grammars per profile, aliases, `?`/help
  behavior, all defaults and options, accepted numeric ranges, output streams,
  formatting, result levels, `IoErr`, break handling, and effects.
- [ ] Cover positional and keyword forms, equals forms, quoting/star escapes,
  empty/omitted input, repeated/conflicting options, `/M` and required tails,
  `/F`, `/N`, help continuation, and invalid/overflow input where applicable.
- [ ] Resolve Workbench/MorphOS differences explicitly. Map every option and
  mode to at least one success case and relevant failure/interaction cases.
- [ ] Use the 3.20 release notes to find stale templates, including HDWrite and
  Newer; preserve new commands and filesystem metadata enhancements.

**Exit per command:** a reviewable finite contract and option-to-test matrix,
with no invented help strings or unverified option list presented as exact.

### CC02 - Baseline the existing Shell and audit APIs

- [ ] Record current command-test results and Shell native qualification without
  altering existing semantics. Separate existing failures from this goal's work.
- [ ] Reconcile the 31 internal identities, external Execute boundary, lookup
  precedence, script-tail ownership, resident use counts, and required boot gaps.
- [ ] Audit consumed SDK declarations against original 3.1 FD/header/autodocs and
  versioned MorphOS SDK material: vectors, registers, layouts, widths, versions.
- [ ] Create an API gap ledger with a specific CopperSharp/CopperStart/service
  owner and a focused ABI or behavioral check for each required change.

- Progress checkpoint (2026-09-26): the existing `tests/Commands` project now
  passes 704 cases on HEAD `5dc8e7dde1787b85ceebd1379587921b6bc01980`, with zero
  failures or skips; result and test-assembly hash are recorded in
  `docs/Commands/Workbench31MorphOS320/baseline-and-api-audit.md`. This confirms
  the managed Shell/command regression baseline only. An isolated native build
  now emits the 68000 HUNK and 020/040 listings without changing shared outputs;
  its compatibility reports record three guarded divide-by-zero `ILLEGAL`
  sites per target in `StackCommand.WriteStackSize`. The divisor invariant
  appears to keep those trap paths unreachable, but this static output is not
  guest execution or release qualification. See `baseline-and-api-audit.md`;
  CC02 remains open for API/ownership reconciliation and behavioral evidence.

- Current-worktree regression check (2026-09-27): `dotnet test
  tests/Commands --no-restore` passes all 806 cases with no skips. The task-
  identity/reaping follow-up filter in CopperStart passes 15 focused tests.
  These checks cover the current dirty working trees, not a clean commit, and
  do not close native Shell qualification, API ownership, or command shipping.
  The test fixtures were aligned with their current contracts: nested records
  no longer overlap the expanded script frame; only `T:Prompt.*` output is
  routed into the prompt-capture mock; and compound Echo examples avoid using
  `FIRST/K/N` as a literal message.

- Current-worktree regression and native-foundation refresh (2026-09-28): the
  complete `tests/Commands` suite passes 843 cases with no failures or skips.
  The startup, argument-boundary, and I/O foundation probes then pass all 264
  supplied instruction-level vectors across 68000/020/040, with one shared
  image per CPU, no leaked resources, and zero shared-image writes. Receipt:
  `tests/Commands.NativeRoot/bin/Release/net10.0/qualification/3e1bbbbf271c43288b90a1ec8a141256/qualification.json`.
  The run uses mocked DOS vectors; original Kickstart, CopperStart, real DOS
  parsing/I/O, minimum-stack, PURE/resident admission, and shipping remain open.

**Exit:** an evidence-based baseline and dependency map. Existing behavior is
reused; compile-only and partial Shell results are not reclassified as shipping.

### CC03 - Implement portable native command entry and execution harness

Depends on CC02.

- [ ] Add standalone command roots and public-ABI adapters, including correct
  CLI/Workbench startup, library acquisition, process state and return handling.
- [ ] Preserve D0/A0 input and caller-preserved registers according to the actual
  loader ABI; verify A6 use rather than assuming DOSBase on entry.
- [ ] Build a small non-shipping probe and execute it under Kickstart 3.1 and
  CopperStart, with missing libraries, allocation failure, and varied stacks.
- [ ] Prove production dependency closure does not embed DOS/Exec, a host shim,
  a managed runtime, or test-only implementations. Establish native trace hooks.

2026-09-26 progress: an independently loaded 3,192-byte authored 68000 probe
now parses normal CLI input with original ReadArgs and captures ten original
Version commands through SystemTagList on clean Kickstart 3.1/Workbench
derivatives. Ten corrected-candidate pairs now match exactly; the earlier exact RES match
and ordinary-name mismatch remain retained as diagnostic controls.
Passive named-port readback preserves exact output, returned level and caller
post-System IoErr. The probe deliberately remains alive for capture;
this does not prove production return/teardown, varied stacks, fault injection,
CopperStart execution or child Result2 propagation, so this stage stays open.

**Exit:** an actual independently loadable HUNK, correct startup/return behavior,
and a reusable native execution harness; a managed method or assembly dump alone
does not pass.

### CC04 - Share argument, output, and ownership support

Depends on CC03.

- [ ] Adapt existing ReadArgs support to real DOS vectors and invocation-local
  guest buffers. Test parity against native DOS parsing and help continuation.
- [ ] Share output/fault formatting, signal polling, allocation and resource
  cleanup without erasing command-specific return/error rules.
- [ ] Restore `IoErr` after cleanup where required, distinguish EOF from errors,
  propagate partial writes and output failures, and never close borrowed streams.
- [ ] Keep future Unix/editor/language grammars separate and documented; do not
  route them through a fabricated ReadArgs template.

**Exit:** focused parser and failure/ownership cases pass through the actual
public adapter, including simultaneous invocation-local state.

### CC05 - Share filesystem and metadata support

Depends on CC04 and the relevant CC02 API gaps.

- [ ] Implement reusable DOS pattern iteration, path operations, iterative
  traversal, metadata views, link handling, and output ordering primitives.
- [ ] Handle multi-assigns, current/parent paths, case-only names, same-object
  detection, directory cycles, long paths, handler limitations, and break signals.
- [ ] Provide explicit buffers/ownership and bounded incremental work without
  arbitrary truncation or imposing a lower input limit than the reference.
- [ ] Separate classic 32-bit operations from optional larger-file/newer-date
  capabilities. Validate ranges and precision; never silently wrap or discard.

**Exit:** reusable traversal and mutation support works on guest DOS handlers,
with low-memory, link-cycle, short-write, stale-lock, and interruption coverage.

### CC06 - Qualify pure code and resident execution infrastructure

Depends on CC03-CC04 and the existing Shell/DOS resident owner.

- [ ] Audit current lowering for shared writable library-base/static/BSS slots;
  add the smallest compiler support needed for invocation-local state.
- [ ] Qualify initializer copying, pointer/data relocations, nested library calls,
  cleanup, and register/stack rules for one shared loaded image.
- [ ] Extend native traces and static checks to reject writes into shared code
  or constants and references to invocation state after that invocation ends.
- [ ] Exercise repeat/concurrent same-SegList calls, resident add/replace/remove,
  active-use protection, deferred loading, forced unsafe admission, and task death.

**Exit:** passing purity infrastructure and a hash-bound report format. Setting
a P bit, serial execution alone, or separate image copies does not pass.

### CC07 - Establish required extension-service work packages

Depends on CC00-CC02. Can run alongside core implementation.

- [ ] Inventory real providers for desktop/MUI, ARexx/Lua/spellchecking,
  networking/remote filesystems/TLS, archives/codecs, USB/media, hardware/debug,
  and filesystem-specific commands. Reuse existing owners where present.
- [ ] Give each missing API/provider an explicit dependency ID, source location,
  license review, implementation slices, real success fixture, and teardown test.
- [ ] Distinguish a command front end from an interpreter, filesystem handler,
  daemon, kernel facility, or driver. Implement missing facilities at that owner.
- [ ] Record classic-compatible fallback behavior only when it preserves the
  contract; unavailable extended functionality remains visibly incomplete.

**Exit:** every advanced command has a concrete path to its required services.
“Deferred to subsystem” without remaining implementation work is not an exit.

### CC08 - Implement reproducible command builds and packaging

Depends on CC03 and CC06.

- [ ] Extend `src/Commands` with bounded command groups and independent native
  roots. Add appropriate projects to the solution without coupling host tests
  to unrelated local SDK edits; keep native SDK assembly identities coherent.
- [ ] Generate per-profile/CPU manifests, version strings, minimum stack and
  executable/script metadata, output hashes, and purity-report references.
- [ ] Build actual 68000/020/040 HUNK files into separate artifact directories.
  Stage selected normal/install-media profiles into the intended runtime paths.
- [ ] Integrate the image builder/DiskBuilder at its real repository owner; fail
  closed on missing/duplicate/stale artifacts and unqualified required P bits.

**Exit:** reproducible build-to-image staging for a qualified probe. Windows
attributes are not Amiga protection metadata; verify the built image's fields.

### CC09 - Build the differential and fault-injection workflow

Depends on CC03-CC08; expand it as new command families need capabilities.

- [ ] Run identical small fixtures through managed semantics, generated native
  code, original Workbench, and licensed MorphOS where each profile applies.
- [ ] Capture stdout/stderr bytes, return levels, `IoErr`, filesystem contents
  and metadata, directory/CLI state, signals, resident entries, and allocations.
- [ ] Define narrow approved normalization for nondeterministic values such as
  addresses, time, device identity, and scheduling; never normalize a real mismatch.
- [ ] Inject allocation, handler, I/O, device, launch, and requester failures;
  isolate every destructive/system-control case in disposable guest fixtures.

**Exit:** trustworthy comparisons and resource accounting with named missing
reference cases. A skipped mandatory comparison is not a passing comparison.

## Command-family stages

Each stage inherits CC01's full option matrix and the four per-command execution
slices. The implementation details below highlight additional obligations;
they are not a reduced list of supported options. All 200 initial commands have
exactly one implementation owner in CC10-CC38.

### CC10 - Execution, lookup, arithmetic, and quoting

- [ ] Commands: `Execute`, `CLI`, `Which`, `Eval`, `Quote`, `PathPart`.
- Progress slice: Workbench 3.1 `Eval` has a separate literal-template resident
  entry using the captured five-slot candidate. The current three-CPU receipt
  passes 37 supplied vectors per CPU (111 total), covering arithmetic,
  operand reconstruction, formatting, `TO`, caret handling, parser/allocation
  failures, repeats, and interleaving, with 63 reachable methods and no
  managed allocation sites, fatal sites, helpers, or external targets. The
  bounded body retains the audited `nullable-values` feature for `TO`; real
  DOS parsing, original behavior, complete classic semantics, PURE/resident
  admission, packaging, and full-command differential evidence remain open;
  eighteen case-level Workbench guest pairs now match. Three refreshed-HUNK
  comparisons cover the plus-prefix and both captured multiplication cases.
- Progress slice: MorphOS `Eval` has been requalified through its resident entry
  on 68000/020/040. The latest receipt passes nineteen supplied vectors per CPU
  (57 total), covering the bounded expression, numeric/LFORMAT, `TO`, diagnostics,
  parser/allocation failure, Ctrl-C, interleaving paths, and all five lowercase
  two-letter operator prefixes observed in Eval 50.7 source, with 92 reachable
  methods and no managed allocation sites, fatal sites, helpers, or external
  targets. The audited `nullable-values` feature remains on `TO`; real DOS and
  filesystem behavior, complete 50.7 semantics, original comparison,
  PURE/resident admission, packaging, and full-command differential evidence remain open;
- Progress slice: MorphOS `Which` now calls public DOS `FindVar(...,LV_ALIAS)`
  through the six-slot `FILE/A,NOALIAS/S,ALIAS/S,NORES/S,RES/S,ALL/S` candidate
  boundary. Its three-CPU resident qualification passes 85 supplied vectors
  per CPU, including all 32 option combinations with candidate found/missing
  inputs, alias-only no-fallthrough, ordinary resident/path lookup,
  parser/allocation failures and interleaving. The option matrix exercises
  candidate assumptions only: `ALIAS <name>` formatting, conflicts and
  alias/resident/path precedence remain unverified against MorphOS. Original
  guest parity, PURE/resident, packaging and differential gates remain open.
- **Dependencies:** CC03-CC04, CC06-CC09; existing Shell execution/lookup owner.
- Reuse Execute and the existing script engine for script parameters, directives,
  nesting, inherited streams, failure limits, and foreground completion. Exercise
  original startup scripts and retain one script-argument owner. Implement CLI's
  own window/launch contract through the existing process owner.
- Which must follow the reference resolution order across internals, residents,
  paths, and script files. Eval preserves operator precedence, signed width,
  formatting, overflow, and division/error behavior. Quote and PathPart preserve
  their actual string/escaping/path modes; do not treat them as filesystem writes.
- **Exit:** all command/profile contracts pass, including lookup collisions,
  malformed expressions/quotes, empty results, and native script execution.

### CC11 - File and directory inspection

- [ ] Commands: `Dir`, `List`, `Type`, `Search`, `Info`, `DiskFree`, `DOSList`.
- Progress slice (2026-09-23): MorphOS `List` now applies `SUB` as a literal,
  case-insensitive file-name substring filter using `ParsePatternNoCase` and
  `MatchPatternNoCase`; DOS pattern punctuation is escaped, `P=PAT` combines
  with `SUB`, and the two parsers receive distinct invocation-owned buffers.
  Its shared row path makes `KEYS` opt-in and renders size/`Dir`, protection,
  DOS-formatted FIB date/time and optional comment fields. Date-conversion
  failure tests verify cleanup and error publication. `NODATES` now suppresses
  date/time columns without calling `DateToStr`; explicit `DATES` retains the
  documented full-row default. `QUICK` takes precedence over `DATES` and emits
  names only. `SINCE` and `UPTO` use public DOS `StrToDate` with inclusive
  date-only filtering; invalid date input exercises the candidate's cleanup
  path. Recursive `ALL` now follows public DOS `APF_DODIR`/`APF_DIDDIR`; the
  fixture verifies nested descent, return-marker suppression, and continuation.
  Workbench reads `ALL` from slot 15 in its shorter result template. The
  resident 68000/020/040 receipts pass thirty-one MorphOS and
  thirty-two Workbench supplied
  vectors per CPU at
  `artifacts/cc11-list-morphos-native-20260923-all-v1/qualification.json`
  and `artifacts/cc11-list-wb31-native-20260923-all-v1/qualification.json`.
  Original recursive output/path semantics, sort/owner/`LFORMAT`, exact header/summary and
  guest parity, packed correspondence, PURE/resident lifecycle, licensing,
  package placement and differential evidence remain open.
- Progress slice (2026-09-23): MorphOS `Search` preserves the active source's
  locale.library v37 minimum and now calls `IsCntrl` for source-compatible
  control delimiters (with TAB preserved) and `IsPrint` for output
  sanitization. LF alone advances displayed line numbers; literal and
  DOS-pattern modes cover control-delimited context. Its refreshed resident
  68000/020/040 receipt passes 45 supplied DOS/Exec/Locale invocations per CPU
  (135 total), with 40 reachable methods and no runtime helpers, external
  targets, exception regions, fatal sites, leaks, or shared-image writes. The
  MorphOS candidate follows the source's 512 KiB `MEMF_ANY` buffer sizing,
  allocation-halving fallback, LF-only maximum-line pre-scan, long-line buffer
  growth, seek-back/reread pass, and DOS 51.28+ `Seek64` absolute and signed
  relative rewinds for files above the 32-bit offset limit. Fixtures cover
  short reads and CTRL-D in the pre-scan, allocation exhaustion, a
  524,300-byte file, and `Seek64` success/failure with a synthetic DOS64 file
  size. Workbench's separate candidate passes 19 vectors per CPU and still
  renders a matching line longer than 8,192 bytes through bounded DOS writes.
  The synthetic vector fixture does not establish real >2 GiB handler behavior,
  real locale tables, packed correspondence, or original-guest output.
  PURE/resident lifecycle, licensing, packaging, and differential evidence
  remain open; this is not a shipping claim.
- Progress slice (2026-09-20): Workbench 3.1 `List` now has an explicit
  missing-DOS boundary in its separate DOS 36 resident syntax candidate. The
  refreshed three-CPU receipt passes eighteen supplied vectors per CPU (54
  total), covering the existing parser, flat matcher, pattern/type filters,
  quick/block/no-header rendering, `TO` output, allocation/Ctrl-C, startup and
  interleaved paths plus the missing-DOS failure path. Recursive/date/sort/
  owner/`LFORMAT`/`ALL`, exact Workbench output and diagnostics, packed
  correspondence, PURE/resident lifecycle, packaging and differential evidence
  remain open.
- Progress slice (2026-09-23): Workbench 3.1 `Search` keeps its captured
  eight-slot syntax candidate and explicit DOS 36 missing-DOS boundary. The
  refreshed resident 68000/020/040 receipt passes nineteen supplied vectors per
  CPU (57 total) with 38 reachable methods and no managed allocations/runtime
  helpers, external targets, exception regions, fatal sites, leaks, or shared-
  image writes. It now exercises file-size-based input allocation and a matching
  line longer than 8,192 bytes, written in bounded DOS chunks. The matrix also
  covers parser, bounded traversal, literal and DOS pattern modes, quiet/file
  modes, failure, startup and interleaved callers. The static binary strings
  remain syntax candidates, so exact runtime grammar/output, recursion, package
  identity, PURE/resident lifecycle, licensing, and differential gates remain
  open; this does not change shipping totals.
- Progress slice (2026-09-23): MorphOS `Type` now uses the documented MorphOS
  50.67+ extended `AnchorPath` layout to request literal soft links, preserving
  the source's behavior. Its resident 68000/020/040 receipt passes fourteen
  supplied vectors per CPU (42 total) with 26 reachable methods, including an
  explicit DOS 50.66 rejection before stream or matcher work. Packed/source
  correspondence, real DOS/handler behavior, original-guest output and result
  parity, PURE/resident lifecycle, licensing, and package admission remain open.
- Progress slice (2026-09-23): Workbench 3.1 `Type` retains the classic
  `AnchorPath` layout and its DOS 36 syntax candidate using the observed
  five-slot `FROM/A/M,TO/K,OPT/K,HEX/S,NUMBER/S` grammar. Its refreshed resident
  68000/020/040 receipt passes fifteen supplied vectors per CPU (45 total),
  with 27 reachable methods and no managed runtime, external native targets,
  exception regions, fatal fault sites, leaks or shared-image writes. Exact
  Workbench output/diagnostics, packed correspondence, PURE/resident lifecycle,
  licensing, packaging, and full-command differential evidence remain open.
- Progress slice (2026-09-20): Workbench 3.1 `Info` now has an explicit
  missing-DOS boundary in its separate DOS 36 resident syntax-candidate entry
  using the observed `DEVICE` template and classic mounted-disk headers/status
  row. Its refreshed three-CPU receipt passes fourteen supplied vectors per CPU
  (42 total), including one- and two-device DOS-list traversal, mixed
  mounted-volume output, startup and missing-DOS guards, with zero runtime
  features/helpers, exception/fatal sites, leaks or shared-image writes.
  Exact original output/IoErr precedence, complete device/volume behavior,
  installed metadata, PURE/resident lifecycle, packaging and differential
  evidence remain open.
- Progress slice (2026-09-20): MorphOS 3.20 `Info` now has a supplied-vector
  multi-node fixture. Its source-bound resident entry traverses zero, one, and
  two device/volume pairs under one DOS-list lock, then performs invocation-
  owned filesystem `Info` allocations after unlock. The three-CPU receipt
  passes eighteen vectors per CPU (54 total), including filtering, GOODONLY,
  wildcard matching, VERBOSE startup data, BLOCKS output, parser/allocation,
  Ctrl-C, startup boundaries and interleaved callers. Alternate providers,
  locale output, original parity, PURE/resident lifecycle, packaging and
  differential evidence remain open.
- Progress slice (2026-09-17): MorphOS 3.20 `Dir` now has a source-bound
  resident entry using the exact six-slot `DIR,OPT/K,ALL/S,DIRS/S,FILES/S,INTER/S`
  grammar. Its public-DOS body exercises invocation-owned `AllocVec`,
  `ExAllControl`, `Lock`/`ExAll`, directory/file filters, ignored `OPT` letters,
  output and parser/startup/allocation paths. The three-CPU receipt passes
  fifteen supplied vectors per CPU with balanced resource ownership and no
  shared-image writes or native runtime helpers. Recursive `ALL`, interactive
  `INTER`, wildcard matching, soft-link behavior, packed correspondence,
  original guest parity, PURE/resident lifecycle, packaging and differential
  evidence remain open.
- Progress slice (2026-09-20): Workbench 3.1 `Dir` now has a separate DOS 36
  resident syntax-candidate entry using the same bounded public-DOS body. Its
  three-CPU receipt passes sixteen supplied vectors per CPU (48 total), adding
  explicit Workbench startup and missing-DOS failure boundaries while retaining
  ReadArgs ownership, `OPT` D/F and ignored-option handling, Lock/ExAll
  enumeration, directory/file filtering, output, parser/allocation cleanup,
  and interleaved-call coverage. Complete Workbench grammar/output and guest
  parity, recursive `ALL`, interactive `INTER`, wildcard/soft-link behavior,
  packed correspondence, PURE/resident lifecycle, packaging and differential
  evidence remain open.
- Progress slice (2026-09-23): MorphOS `Dir` now has bounded source-shaped
  recursive `ALL` and `OPT A`, the legacy joined-line `OPT I` mode, directory
  order preservation, and empty-directory success. Its separate fixture models
  CurrentDir-relative child locks, ExAll buffers/control objects, recursive
  output, and balanced cleanup. The resident 68000/020/040 receipt passes
  nineteen supplied vectors per CPU at
  `artifacts/cc11-dir-morphos-native-20260923-recursive-all-v6/qualification.json`.
  Workbench remains a separate flat syntax candidate and passes seventeen vectors
  per CPU at
  `artifacts/cc11-dir-wb31-native-20260923-profile-split-v6/qualification.json`.
  These fixtures do not close MorphOS wildcard/soft-link/error parity or bind
  either packed binary; Workbench behavior is still unproven. Neither profile
  has original-guest comparison, actual PURE/resident reuse, licensing, or
  package admission, so CC11 and shipping totals remain open.
- Progress slice (2026-09-23): MorphOS `Dir` now routes wildcard paths through
  public DOS `ParsePattern` and `MatchFirst`/`MatchNext`/`MatchEnd`, including
  recursive wildcard `ALL` listings. Its refreshed resident HUNK receipt passes
  21 supplied vectors per CPU on 68000/020/040 at
  `artifacts/cc11-dir-morphos-native-20260923-wildcards-v2/qualification.json`.
  The separate Workbench syntax candidate passes 19 vectors per CPU at
  `artifacts/cc11-dir-wb31-native-20260923-wildcards-v2/qualification.json`,
  including wildcard listing and its currently modeled unsupported-`ALL`
  result. The Workbench result is not original-behavior evidence. MorphOS soft
  links, full error/diagnostic parity, both packed correspondences, guest
  comparisons, PURE/resident reuse, licensing and package admission remain
  open; the inventory and shipping totals are unchanged.
- Progress slice (2026-09-23): MorphOS `Dir` now classifies ExAll soft links by
  following target locks and `Examine`, handles wildcard links through the
  shared `SoftlinkDODIR` implementation, and reports dangling links through
  `GetDeviceProc`/`ReadLink` for wildcard, recursive, and direct-path cases.
  The refreshed resident receipt passes 27 supplied DOS-vector invocations per
  CPU on 68000/020/040 in
  `artifacts/cc11-dir-morphos-native-20260923-softlinks-v10/qualification.json`.
  A dangling non-link failure case confirms the device lookup does not convert
  ordinary lock failures into success. The separate Workbench candidate passes
  20 vectors per CPU at
  `artifacts/cc11-dir-wb31-native-20260923-softlinks-v4/qualification.json`;
  its unsupported `ALL` and unbound grammar remain explicit candidate limits.
  Full ExAll/IoErr, diagnostics, Ctrl-C and nested failure parity, original
  guest comparison, packed correspondence, PURE/resident reuse, licensing and
  package admission remain open; shipping counts remain unchanged.
- Progress slice (2026-09-23): MorphOS `Dir` now checks `IoErr` when `ExAll`
  ends, accepts `ERROR_NO_MORE_ENTRIES` as normal completion, and carries
  provider failures as `RETURN_ERROR` through recursive `ALL`. Its failure path
  now emits the source-shaped generic diagnostic and maps wrong-type faults to
  the directory-not-found `PrintFault`; the resident fixture covers direct,
  nested, and wrong-type provider failures. The refreshed 68000/020/040 receipt
  passes thirty vectors per CPU at
  `artifacts/cc11-dir-morphos-native-20260923-exall-errors-v2/qualification.json`.
  The separate Workbench syntax candidate passes twenty-two vectors per CPU at
  `artifacts/cc11-dir-wb31-native-20260923-exall-errors-v2/qualification.json`,
  including a bounded provider-error case. Full `IoErr` and diagnostic parity,
  Ctrl-C, original guest comparison, packed correspondence, PURE/resident
  reuse, licensing and package admission remain open; shipping counts remain
  unchanged.
- Progress slice (2026-09-23): MorphOS `Dir` no longer caps a directory or
  wildcard result at 256 rows or stores all names in a fixed workspace. It now
  grows invocation-owned row tables in 128-entry increments and owns a copy of
  each retained name until cleanup. The fixture paginates DOS `ExAll` in bounded
  batches and verifies both direct and wildcard listings of 300 files, sorted
  paired output, and balanced allocation cleanup. Its 68000/020/040 resident
  receipt passes 32 vectors per CPU at
  `artifacts/cc11-dir-morphos-native-20260923-dynamic-rows-v2/qualification.json`.
  The Workbench syntax candidate passes 24 vectors per CPU at
  `artifacts/cc11-dir-wb31-native-20260923-dynamic-rows-v1/qualification.json`.
  Actual handler limits, exhaustive allocation-point sweeps, guest output,
  original correspondence, PURE/resident reuse, licensing and packaging
  remain open.
- Progress slice (2026-09-23): MorphOS `Dir` now has supplied-vector coverage
  for Ctrl-C before listing output, after directory labels but before a file
  pair, during wildcard file-pair output, and a `MatchNext` break. These cases
  preserve `RETURN_WARN`, partial output, and match/resource cleanup. Direct
  listing interrupts and a matcher break preserve `ERROR_BREAK`; the later
  source-order follow-up below records wildcard output's saved-`IoErr` result.
  The 68000/020/040 resident receipt passes 36 vectors per CPU at
  `artifacts/cc11-dir-morphos-native-20260923-interrupt-v1/qualification.json`;
  the independent Workbench syntax candidate passes 28 at
  `artifacts/cc11-dir-wb31-native-20260923-interrupt-v1/qualification.json`.
  Original guest parity and real DOS-handler interruption behavior remain open.
- Progress slice (2026-09-23): `Dir` now injects allocation failure after ten
  copied names and at the 128-to-256 row-table growth. The tests verify
  `RETURN_FAIL`/`ERROR_NO_FREE_STORE`, the generic diagnostic, and cleanup of
  retained names, the prior table, ExAll control/buffer, and directory lock.
  Resident 68000/020/040 receipts pass 38 MorphOS vectors and 30 Workbench
  candidate vectors per CPU at
  `artifacts/cc11-dir-morphos-native-20260923-low-memory-v2/qualification.json`
  and `artifacts/cc11-dir-wb31-native-20260923-low-memory-v1/qualification.json`.
  Exhaustive allocation-point sweeps and original-guest comparison remain open.
- Progress slice (2026-09-23): MorphOS `Dir` now saves the DOS `IoErr` observed
  after processing each wildcard match, before `MatchNext`, and restores it
  after `MatchEnd` and matcher-buffer cleanup, following the inspected source.
  The fixture checks a nonzero saved value against terminal
  `ERROR_NO_MORE_ENTRIES` and a cleanup-time error change, plus the source's
  successful wildcard `IoErr` values for recursive enumeration and dangling
  soft links. It also verifies Ctrl-C during wildcard output returns
  `RETURN_WARN` while retaining the source-saved `IoErr` for diagnostics. The
  MorphOS resident receipt passes 39 vectors per CPU at
  `artifacts/cc11-dir-morphos-native-20260923-saved-ioerr-v3/qualification.json`;
  the independent Workbench syntax candidate remains at 30 vectors per CPU at
  `artifacts/cc11-dir-wb31-native-20260923-saved-ioerr-v1/qualification.json`.
  These are source and fixture results only; original guest comparison, the
  complete error matrix, Workbench grammar, PURE/resident lifecycle, licensing,
  and package admission remain open.
- Progress slice (2026-09-19): MorphOS 3.20 `DiskFree` now has a source-bound
  resident entry using the exact `VOLUME,NOPOSTFIX/S,PERCENT/S` grammar.
  Its bounded body uses public DOS `Lock`/`Info`, invocation-owned `InfoData`
  and formatting storage, wide byte arithmetic, and byte/KB/MB/GB or percentage
  output modes. The three-CPU static receipt passes with no managed runtime,
  external native targets, exception regions, fatal fault sites, or shared-image
  writes. Its supplied-vector runtime receipt passes fifteen invocations per
  CPU across current/explicit volumes, byte/percentage modes, large byte and
  overflow-safe percentage arithmetic, provider/parser/allocation failures,
  startup boundaries, cleanup and interleaved callers. Exact localized output,
  provider behavior outside the
  fixture, original-guest differential, PURE/resident lifecycle, and package
admission remain open.
- Progress slice (2026-09-20): MorphOS `DOSList` now has a source-bound
  resident entry whose fixture exercises zero, one, and three-node device,
  volume, and assign passes through the public `AttemptLockDosList`,
  `NextDosEntry`, and matching unlock vectors. Its three-CPU receipt passes
  eighteen supplied vectors per CPU (54 total), including NAME/ADDRESS filters,
  verbose mounted-state rows, lock refusal, parser/allocation/Ctrl-C,
  startup-boundary and interleaved calls. Provider-specific process/detail
  rows, original guest parity, PURE/resident lifecycle, packaging, licensing,
  and differential evidence remain open.
- **Dependencies:** CC05, CC09 and versioned DOS enumeration/metadata APIs.
- Implement every listing, format, filtering, ordering, pattern, recursion,
  interactive, text/binary/hex, line-number, search, and output-file mode in the
  frozen contracts. Preserve headers, summaries and filename presentation.
- Exercise assigns, inaccessible entries, sparse/large files where supported,
  comments/protection/date fields, links, invalid disk-info responses, Ctrl-C/D,
  input/output errors, and interactive command sequences for Dir.
- **Exit:** byte-level output and guest-state comparisons pass for each mode;
  underlying list locks and match contexts are released on every exit.

### CC12 - Directory, assignment, and namespace changes

- [ ] Commands: `AddBuffers`, `Assign`, `MakeDir`, `Rename`, `Relabel`, `MakeLink`.
- Progress slice: MorphOS `Relabel` now has a source-bound resident native
  entry preserving `DRIVE/A,NAME/A`, public DOS-list lookup, unlock-before-
  mutation ordering, validation diagnostics, parser/scratch ownership and
  handler-failure propagation. Its three-CPU HUNK receipt passes eight supplied
  parser/list/mutation vectors per CPU (24 total), including invalid names,
  missing entries, handler failure and interleaved calls. Keep CC12 open until
  the real handler mutation, Workbench grammar, packed/source correspondence,
  PURE/resident reuse, package admission and differential evidence pass.
- Progress slice (2026-09-17): Workbench Assign now has a bounded public-DOS
  mutation candidate using the observed classic `ReadArgs` template and
  `AssignLock`, `AssignLate`, `AssignPath`, `AssignAdd`, and `RemAssignList`.
  Invocation-owned `/M` targets and lock transfer/cleanup are explicit. The
  private entry compiles as clean resident HUNK images for 68000/020/040;
  receipt `artifacts/assign-wb31-native-20260917-qualified-v3/qualification.json`.
  A licensed-ROM parser-edge probe agrees on ReadArgs error 120, PrintFault
  120, FreeArgs(NULL), and no filesystem calls for two malformed inputs, but
  exposes an unresolved original-versus-candidate post-run IoErr delta; the
  durable receipt is `artifacts/assign-parser-edges-20260917/qualification.json`.
  Keep this as bounded evidence only until the startup/SetIoErr contract is
  closed.
   A MorphOS 3.20 public-DOS mutation candidate now uses the documented
   `NAME,TARGET/M,LIST/S,DISMOUNT/S,DEFER/S,PATH/S,ADD/S,REMOVE/S,VOLS/S,DIRS/S,DEVICES/S`
   grammar and compiles as a clean resident HUNK for 68000/020/040; receipt
   `artifacts/assign-morphos320-native-20260917-qualified-v1/qualification.json`.
   LIST, EXISTS, DISMOUNT, VOLS, DIRS, DEVICES, packed correspondence, original
   guest parity, handler/runtime behavior, lifecycle, PURE and packaging remain
   open.
- Progress slice (2026-09-17): Workbench MakeDir now has a DOS 36 resident
  entry around the verified `NAME/M` body. Its independent compiled-HUNK
  fixture passes fourteen supplied public-DOS invocations on each
  68000/020/040 image, covering result ownership, empty and multiple vectors,
  existing/create/failure ordering, diagnostics, parser/allocation failures,
  startup boundaries and interleaved callers. Receipt
  `artifacts/makedir-wb31-native-20260917-runtime-v2/qualification.json`.
  Raw BPTR calls keep the closure free of nullable-value runtime features;
  packed correspondence, original guest behavior, filesystem effects,
  lifecycle/PURE, packaging and differential gates stay open.
- Progress slice (2026-09-13): Workbench 3.1 `Relabel` now has an independent
  DOS 36 resident body after original 37.2 execution disproved the shared
  MorphOS candidate. It preserves assigns/device/volume lookup, last-byte
  drive handling, diagnostics, BOOL return and ambient cleanup errors. Both
  binaries pass 38 comparisons on each of 68000/020/040 (114 total); each
  replacement adds two allocation-failure cases and a long-drive storage case.
  Parser-buffer mutation is recorded separately: the replacement preserves
  borrowed storage and avoids the original's unsafe fixed-buffer writes.
  The unchanged 68000 HUNK now also passes eight paired original-DOS/Shell
  boot cases, four new-volume payload readbacks, four exact diagnostic readbacks
  and sequential resident registration/reuse/removal. Host Exec/device takeover
  remains part of the recorded boot provider. Eleven additional argument/help
  pairs pass, including duplicate/escaped arguments and ReadArgs `?`/EOF.
  Follow-up (2026-09-14): protected DF0 cancellation now completes on both
  binaries through the production host-keyboard entry point and original guest
  input/requester path. A boot-time input-service reinstallation bug was fixed;
  the regression fails before the fix and eighteen focused device tests pass
  after it. Both commands return 20/214 with the exact write-protection fault,
  balanced cleanup and subsequent help/recovery. See the hash-bound
  `artifacts/relabel-input-forward-fixed-20260914/qualified/comparison.json`.
  This is twenty distinct paired scenarios, not full original-OS qualification:
  the host Exec/device provider remains explicit. Keep the profile open until
  the remaining parser/handler cases, Workbench launch, concurrent resident lifecycle,
  original classification, package admission and full differential evidence pass.
  A further 2026-09-14 paired boot proves two actual tasks overlap in one loaded
  Relabel segment: protected DF0 remains in the requester while RAM relabeling
  completes, then cancellation and same-parent recovery succeed. Distinct live
  RDArgs and replacement allocations, task-owned cleanup, unchanged code and
  final segment removal/free pass. The receipt is
  `artifacts/relabel-concurrent-20260914/qualified-v2/comparison.json`; twelve
  corruption controls reject false ownership/lifetime/input evidence. This is
  bounded concurrency evidence, not full PURE admission. Next test registry
  changes/removal while active, task-death cleanup and remaining launch/platform
  cases without weakening the original classification or package gates.
  Follow-up (2026-09-15): both binaries now also survive an ordinary Resident
  REMOVE attempt while the background call is active. Original RemSegment
  refuses at observed use count 2, Resident returns 5/202 with exact in-use
  output, the same node/segment serves the second caller, and final removal
  succeeds at count 1 after all callers return. The final receipt is
  `artifacts/relabel-active-remove-20260915-v2/qualified/comparison.json`, with
  twenty negative controls. The first stalled console-output probe is retained
  as failed evidence. Next test active replacement/other registry changes,
  task-death cleanup and remaining launch/platform cases; this bounded refusal
  case does not establish forced removal, original PURE classification or
  whole-profile admission.
  Follow-up (2026-09-16): original Resident REPLACE/PURE refusal now passes
  on both binaries at 10/202 before a second load or registry mutation. The
  same node/count 2 remains usable; Cancel/recovery and final removal/free at
  count 1 pass. Evidence: `artifacts/relabel-active-replace-20260915/qualified/
  comparison.json`, with 21 negative controls. This qualifies neither
  successful replacement nor CopperOS's MorphOS Shell-owned Resident.
  Next cover successful replacement and task-death/lifecycle behavior, then
  remaining reference, launch/platform, purity and package gates.
  Further follow-up (2026-09-16): successful idle replacement passes on both
  binaries. A new copy loads before the old one is released; the same registry
  node is updated, REPLACE returns 0/0, and two later calls reuse the new image.
  Both image lifetimes, unchanged code, per-call ownership and final removal
  (0/202) pass. Evidence: `artifacts/relabel-idle-replace-20260916/qualified-v2/
  comparison.json`, with 24 corruption controls. Next test failed replacement
  loading and retention of the old usable command, then applicable task-death/
  failure lifecycle, original classification, launch/platform and package gates.
  Failure follow-up (2026-09-16): missing and invalid replacement sources now
  retain the same node/segment at count 1 on both binaries, allow two subsequent
  calls and final removal/free. Missing source fails at Lock/205; invalid text
  fails at LoadSeg/212 but original Resident reports 5/205 for both. Evidence:
  `artifacts/relabel-failed-replace-20260916/{missing,invalid}/qualified/`, with
  56 corruption controls. Next cover other loader failures and applicable
  task/process retirement, then remaining original classification, launch/
  platform and package gates. These helper workloads do not count as new
  Relabel options or qualify CopperOS's separate MorphOS Resident command.
  Retirement follow-up (2026-09-16): both fresh boots now observe normal
  background RemTask(NULL) after command return, resident count 1, a terminal
  switch with the caller unlinked, and later parent recovery using the retained
  image. Evidence: `artifacts/relabel-caller-retirement-20260916/qualified/`,
  with 20 retirement and 21 overlap/refusal controls. Complete process-storage
  reaping and abrupt/outstanding-resource termination are not covered. Next
  distinguish those obligations from normal unlinking, then close remaining
  original classification, launch/platform and packaging requirements.
  Memory follow-up (2026-09-16): all four captured caller tc_MemEntry spans,
  including Process/stack storage (3520 rounded bytes), are now matched to exact
  FreeMem calls and immediate free-list coverage before later reuse. Evidence:
  `artifacts/relabel-caller-memory-20260916-v2/qualified/`, with 17 memory,
  20 retirement and 21 concurrency/refusal controls. The original RemTask route
  uses host FreeMem; this does not qualify host RemTask/native handoff or waive
  its safety checks. Continue the broader process-resource/termination ledger
  and remaining original classification, launch/platform and package gates.
  Persistent-volume follow-up (2026-09-16): original and replacement each pass
  two device/volume-name renames on an explicitly mounted writable DOS1 floppy,
  both at 0/0 with empty output. Independently read exported sectors retain the
  final label and preserve every non-root block, payload, file protection and
  allocation bitmap. Only root label/date/checksum fields change. Evidence:
  `artifacts/relabel-persistent-20260916-v6/qualified/`, with twelve corruption
  controls. The private boot disk remains write-protected; data media are owned
  fixtures. Original Mount needs RAM-backed ENV: in this minimal startup.
  Cold-remount, auto-discovery, other filesystem/platform paths and full
  original classification/PURE/package admission remain open.
  Cold-remount follow-up (2026-09-16): fresh ROM guests now mount each exact
  persisted output read-only, resolve SavedDisk:, and read the proof and all
  1792 nested guard bytes. Neither executes Relabel, and disk bytes remain
  unchanged. Evidence: `artifacts/relabel-cold-remount-20260916/qualified/`,
  with twelve corruption controls. This closes bounded DOS1 cold-readback,
  not automatic discovery, other filesystem/platform paths or full admission.
  Continue filesystem/name boundaries and errors, original classification,
  process-resource ownership, launch/platform and package requirements.
  FFS name boundaries (2026-09-17): four original/replacement pairs verify
  thirty-byte success and rejection of thirty-one-byte, empty and slash names
  at 20/210 with exact fault output. Full arguments reach DOS unchanged;
  rejected names leave the disk label intact, and all four recovery calls
  succeed at 0/0. Intermediate sector observations and final exports verify
  label, payload and metadata preservation. Evidence:
  `artifacts/relabel-name-boundaries-20260916-v2/`, eight TRXs and sixteen
  corruption controls. OFS/other handler behavior and full original PURE,
  platform/launch, process-resource and package admission remain open.
- Progress slice (2026-09-17): the bounded Rename E02/E04/R03 policy matrix is
  now independently hash-bound for both `wb31` and `morphos320`. Sequential
  receipts on 68000/020/040 verify `Rename(FALSE)` with `IoErr=0`, output and
  fault-call poisoning, and cleanup-time secondary `IoErr` changes while
  preserving each profile's primary result and diagnostic policy. The verifier
  is `tools/Commands/verify_rename_error_policy.py`; its receipt is
  `artifacts/rename-error-policy-20260917/qualification.json`. These are
  supplied-vector/provider combinations, so real handler output, original
  MorphOS correspondence, lifecycle, purity, packaging and shipping gates stay
  open.
- Progress slice (2026-09-17): the next bounded Rename gate now has a dedicated
  original-DOS `RunCommand` harness in the sibling test project. It binds the
  private Workbench 37.2 reference, the source-generated 68000 HUNK/map, and
  the exact `FROM/A/M,TO=AS/A,QUIET/S` template. With the licensed V4063 ROM
  and explicit private paths, four invocations (original/generated image ×
  empty-line/unterminated-quote input) and two comparisons pass. The inputs
  return 20 with exact 116/120 `IoErr`/`PrintFault` codes, failed-parse
  ownership, no `FreeArgs`, and only the observed `UnLock(NULL)` cleanup
  vector. Receipt: `artifacts/rename-parser-edges-20260917/qualification.json`.
  Positional/alias/quoting/help/pattern coverage and all runtime, MorphOS
  correspondence, PURE, package and shipping gates remain open.
- Progress slice (2026-09-13): Workbench 3.1 `AddBuffers` now has an independent
  classic body after original 37.2 execution disproved the earlier shared
  candidate. It preserves change-then-query, omitted versus explicit zero,
  signed query results, null-header faults, parser FAIL versus handler OK,
  missing-DOS Result2 and ambient cleanup/output errors. The resident
  68000/020/040 artifacts each pass 27 comparisons against the original on
  68000, plus a generated-only allocation-failure case. Original 68020 execution
  has a retained unsupported emulator timing failure. Keep the profile open
  until real DOS/handler behavior, Workbench launch, PURE/resident lifecycle,
  package admission and full differential evidence pass.
- **Dependencies:** CC05; DOS assigns, namespace and handler support.
- Preserve assign listing/add/remove/deferred/path behavior, buffer query/change,
  multiple directories, wildcard renames, and original link options. Validate
  actual file identity before changing paths and handle cross-volume limitations.
- Cover case-only renames, destination collisions, partial multi-object results,
  empty paths, locked objects, missing link targets and link-capability errors.
  MorphOS soft-link behavior must not acquire an inappropriate target lock.
- **Exit:** visible namespace/assign state and failure/cleanup behavior match;
  every operation goes through DOS rather than host filesystem conveniences.

### CC13 - Copying, joining, and removal

- [ ] Commands: `Copy`, `Clone`, `Delete`, `Join`, `Trashcan`.
- Progress slice: Workbench 3.1 `Delete` now has a separate DOS 36 resident
  four-slot syntax-candidate entry sharing the bounded public-DOS deletion
  worker. Its three-CPU receipt passes eight supplied vectors per CPU (24
  total), with 43 reachable methods and no managed runtime, external native
  targets, exception regions, fatal fault sites, leaks, or shared-image writes.
Exact Workbench diagnostics, recursion/link policy, original parity,
PURE/resident lifecycle, packaging, and full-command differential evidence remain open.
- Progress slice: MorphOS 3.20 `Delete` has a bounded resident frontend using
  its five-slot source grammar and shared public-DOS matcher/protection/delete
  worker. The three-CPU receipt passes eight supplied vectors per CPU with
  balanced ownership and no native runtime helpers or shared-image writes.
  Parent-protection retry, full recursion/link behavior, diagnostics, packed
  correspondence, original parity and all admission gates remain open.
- Progress slice: Workbench 3.1 `Join` now has a separate DOS 36 resident
  syntax-candidate entry using the observed `FILE/M/A,AS=TO/K/A` boundary and
  the shared public-DOS body. Its three-CPU resident fixture now passes
  seventeen supplied public-DOS vectors per CPU with zero managed runtime,
  external native targets, exception regions, fatal fault sites, leaks or
  shared-image writes. The MorphOS DOS 37 frontend passes the same fixture;
  exact Workbench/MorphOS behavior, lifecycle, packaging, and differential
  evidence remain open.
- **Dependencies:** CC05, metadata support, and the real trash/desktop owner.
- Implement complete pattern, recursion, overwrite, requester, buffering,
  metadata, quiet and other documented modes. Clone and Trashcan retain their
  own contracts rather than becoming undocumented aliases for Copy/Delete.
- Test same-file/directory copies, destination inside source, links/cycles,
  multi-source inputs, interrupted overwrites, full media, denied metadata writes,
  and failures after some objects succeed. Preserve archive/comment/date/
  protection behavior, including extended precision when the provider supports it.
- **Exit:** copied/joined bytes and all promised metadata match; partial failures
  never report complete success and interrupted operations leave defined state.

### CC14 - Metadata, sizing, and file writes

- [ ] Commands: `FileNote`, `Protect`, `SetDate`, `Touch`, `SetFileSize`, `FileWrite`.
- Progress slice: MorphOS `FileNote` now has a source-bound resident native
  entry preserving `FILE/A,COMMENT,ALL/S,QUIET/S`, invocation-owned
  `ReadArgs`/AnchorPath/comment storage, recursive directory flags, comment
  truncation, `SetComment` failure continuation and Ctrl-C/cleanup behavior.
  Its three-CPU HUNK receipt passes ten supplied DOS vectors per CPU (30 total),
  including parser, no-match, truncation, quiet, recursive descent/exit,
  mutation failure and interleaved calls. Keep CC14 open until exact parser and
  diagnostics, soft-link/device policy, Workbench parity, PURE/resident reuse,
  package admission and differential evidence pass.
- Progress slice: Workbench 3.1 `Filenote` now has a separate DOS 36 resident
  syntax-candidate entry sharing the public-DOS body. Its three-CPU HUNK receipt
  passes ten supplied DOS vectors per CPU (30 total), with parser, matcher,
  recursive AnchorPath, comment mutation, cleanup and interleaving coverage.
  Keep the profile open until the v37.1 binary/guest contract, exact output and
  diagnostics, PURE/resident lifecycle, package admission and differential
  evidence pass.
- Progress slice: Workbench 3.1 `SetDate` now has explicit startup-boundary
  coverage in its resident fixture. The refreshed receipt
  `artifacts/setdate-native-entry-20260920/qualification.json` passes twelve
  supplied ReadArgs/date/matcher/lock/SetFileDate vectors per CPU (36 total),
  including Workbench startup and missing-DOS cases, balanced cleanup and
  interleaved callers. Keep CC14 open until original guest diagnostics,
  MorphOS parity, PURE/resident reuse, packaging and differential evidence pass.
- Progress slice: MorphOS `Protect` 50.6 now has a source-bound resident
  native body preserving `FILE/A,FLAGS,ADD/S,SUB/S,ALL/S,QUIET/S`, active-low
  and active-high protection mapping, recursive AnchorPath flags, quiet
  output, mutation/no-match/break/parser failures, and interleaved calls.
  Its three-CPU receipt passes fourteen supplied DOS/Exec vectors per CPU.
  Keep CC14 open until volume/device and soft-link policy, complete wildcard
  traversal, exact Workbench behavior, PURE/resident reuse, package admission,
  and differential evidence are qualified.
- Progress slice: Workbench 3.1 `Protect` now has a separate DOS 36 resident
  syntax-candidate entry using the observed six-slot
  `FILE/A,FLAGS,ADD/S,SUB/S,ALL/S,QUIET/S` boundary and the bounded public-DOS
  body. Its three-CPU receipt passes fourteen supplied DOS/Exec vectors per
  CPU. Keep the profile open until exact Workbench behavior, complete
  filesystem and diagnostic parity, PURE/resident lifecycle, package
  admission, and differential evidence are qualified.
- Progress slice: MorphOS `Touch` 50.9 now has a source-bound resident native
  body preserving `NAME/A/M,VERBOSE/S,ALL/S`, invocation-owned matcher and
  DateStamp storage, recursive directory flags, classic `DateStamp`/
  `SetFileDate`, direct-touch/create fallback, verbose output, Ctrl-C and
  interleaved calls. Its three-CPU receipt passes ten supplied DOS/Exec
  invocations per CPU. Keep CC14 open until the DOS 51.66 UTC/POSIX branch,
  soft-link policy, exact diagnostics, PURE/resident reuse, package admission,
  and differential evidence are qualified.
- **Dependencies:** CC05 and versioned metadata/size/date capabilities.
- Preserve protection-bit polarity, set/add/remove forms, comments, timestamps,
  creation versus modification, file-size/offset rules, and binary/text writing
  syntax. FileWrite must preserve repeated numeric/string patterns, zero-fill,
  append and embedded-NUL behavior. Distinguish unspecified values from explicitly
  empty or zero values; retain its larger-file capability where the provider has it.
- Test read-only media, object-in-use, expansion/truncation failures, numeric
  limits, time/locale parsing, and mixed success in wildcard operations. Protect
  must not reinterpret a P flag as a proof of actual executable reentrancy.
- **Exit:** exact contents, lengths, dates/comments/flags, results and error
  behavior are covered on both ordinary and capability-limited handlers.

### CC15 - Text transforms and comparisons

- [ ] Commands: `Sort`, `ConvertText`, `Replace`, `Newer`, `MirrorCheck`, `MirrorCopy`.
- **Dependencies:** CC05; required encoding, comparison and metadata providers.
- Preserve sort key/case/collation rules, conversion encodings, binary/text
  replacement modes, comparison rules, and full mirroring semantics. Newer's
  file-only selection must remain distinct from library/resident lookup.
- MirrorCheck must not modify the compared trees. MirrorCopy must follow its
  documented destination-change/deletion policy and avoid unintended source
  mutation. Handle identical roots, nested roots, links, metadata differences,
  invalid sequences and interruption partway through a destination update.
- **Exit:** deterministic byte/tree comparisons and error cases pass, including
  empty inputs, large incremental input, and safe replacement of original files.

### CC16 - Time and wait commands

- [ ] Commands: `Date`, `Time`, `Wait`, `WaitX`, `Uptime`, `WaitForPort`,
  `WaitForLib`, `WaitForNotification`.
- Progress slice: Workbench `Date` now has a hash-bound M10 contract, and its
  MorphOS 3.20 counterpart has a source-bound contract. Both have public
  DOS/Exec native bodies with resident-vector receipts across 68000/020/040:
  27 supplied invocations for Workbench and 39 for MorphOS; the Workbench
  receipt now includes explicit startup and missing-DOS boundaries. Workbench `Wait`
  now has a resident body and a 51-invocation receipt covering DOS delay,
  timer, `UNTIL`, Ctrl-C, startup and missing-DOS paths; MorphOS `Wait` has the corresponding
  source-bound body and receipt. Keep this checkbox open until
  original-guest parity, PURE reuse, differential behavior, and package
  admission are qualified.
- Progress slice: MorphOS `WaitForPort` now has a source-documented
  `PORTNAME/A,I=INTERVAL/K/N,L=LOOP/K/N,D=DISAPPEAR/S` contract and a separate
  resident native entry. The bounded body uses DOS `ReadArgs`/`FreeArgs`, Exec
  `FindPort`/`SetSignal`, and DOS `Delay` for presence/disappearance, loop,
  numeric-failure, parser, startup-boundary, Ctrl-C and interleaved paths.
  Receipt `artifacts/waitforport-morphos-native-6d77780fa9644fa2b68fd765d191f27d/qualification.json`
  passes ten supplied invocations per CPU on 68000/020/040. Keep CC16 open
  until exact MorphOS guest timing/diagnostics, packed correspondence, provider
  behavior, original differential, PURE/resident lifecycle, licensing and
  package admission are qualified.
- Progress slice: MorphOS `WaitForLib` now has a source-documented
  `LIBNAME/A,I=INTERVAL/K/N,L=LOOP/K/N` contract and a separate resident native
  entry. The bounded body uses DOS `ReadArgs`/`FreeArgs`, Exec
  `FindName`/`SetSignal`, and DOS `Delay` to poll the public library list, with
  default interval/eleven-check loop handling, loop exhaustion,
  numeric-failure, parser/startup-boundary, Ctrl-C and interleaved paths.
  Receipt `artifacts/waitforlib-morphos-native-912808104128434b81af17a62421d3de/qualification.json`
  passes ten supplied invocations per CPU on 68000/020/040. Keep CC16 open
  until exact MorphOS guest timing/diagnostics, packed correspondence, provider
  behavior, original differential, PURE/resident lifecycle, licensing and
  package admission are qualified.
- Progress slice: MorphOS `WaitForNotification` now has the documented
  `NAME/A/M,QUIET/S,CONTINUE=CNT/S` contract and a separate resident native
  entry. The bounded body owns the `StartNotify`/`EndNotify` request set and
  signal, supports failed-registration continuation, quiet fault handling,
  parser/startup-boundary, Ctrl-C, empty-registration and interleaved paths.
  Receipt `artifacts/waitfornotification-morphos-native-0a2f332649224908b505d5f230a1dc49/qualification.json`
  passes twelve supplied invocations per CPU on 68000/020/040. Keep CC16 open
  until exact original diagnostics/output, packed correspondence, provider
  behavior, original differential, PURE/resident lifecycle, licensing and
  package admission are qualified.
- Progress slice: MorphOS `Uptime` now has a hash-bound 50.5 identity and an
  explicit no-template contract. The ISO `hdinstall.fixc` `+P` event is recorded
  as design evidence, while output formatting, timer source, failure precedence,
  guest parity, PURE/lifecycle and package behavior remain open. See
  `docs/Commands/Workbench31MorphOS320/contracts/Uptime.md` and
  `reference-captures/uptime-morphos-binary-audit-20260923.json`; no guessed
  formatter is admitted.
- Progress slice (2026-09-23): MorphOS `Time` now has a hash-bound 1.0 ISO
  identity, documented command-timing purpose, and installer `+P` event in
  `docs/Commands/Workbench31MorphOS320/contracts/Time.md` and
  `reference-captures/time-morphos-binary-audit-20260923.json`. Its packed
  member does not provide a reliable template or diagnostics, so command-tail,
  timer, output, failure, guest-parity, PURE/lifecycle and package gates remain
  open; no guessed timer or formatter is admitted.
- Progress slice (2026-09-23): MorphOS `WaitX` now has a hash-bound 51.1 ISO
  identity, documented wait-then-execute purpose, and installer `+P` event in
  `docs/Commands/Workbench31MorphOS320/contracts/WaitX.md` and
  `reference-captures/waitx-morphos-binary-audit-20260923.json`. Its packed
  member provides no reliable grammar or diagnostics, so delay/timer,
  nested-launch, output, failure, guest-parity, PURE/lifecycle and package
  gates remain open; no guessed launch wrapper is admitted.
- **Dependencies:** CC04, guest timers/signals/notifications and process launch.
- Preserve every time/date format, unit, absolute/relative delay, command-timing,
  port/library readiness, and notification option. Capture locale, calendar,
  timeout and return-code behavior per profile.
- Use scheduler-visible guest waits with race-free readiness checks. Test a
  target arriving/disappearing at the wait boundary, clock changes, wrap/range
  limits, Ctrl-C, failure to allocate/register, and notification teardown.
- **Exit:** no busy loop, host clock dependency, lost notification, stale port,
  or leaked wait request; observed timing/result behavior meets the contract.

### CC17 - Memory, version, and task inspection

- [ ] Commands: `Avail`, `Version`, `Status`, `Stat`, `TaskList`, `ShowConfig`, `CPU`.
- Progress slice: MorphOS `Avail` now has a separate DOS 37 resident body with
  the source-observed `CHIP/S,FAST/S,TOTAL/S,FLUSH/S,H=HUMAN/S` grammar. Its
  three-CPU HUNK receipt passes eleven supplied DOS/Exec vectors per CPU (33
  total), covering numeric and human summary/selector output, the single-
  selector diagnostic, `FLUSH`, summary locking, parser failure, repeat and
  interleaved callers. Keep it open until packed 50.6 correspondence, exact
  guest output/diagnostics, expunge results, PURE/resident lifecycle, licensing,
  package admission and differential evidence pass.
- Progress slice: Workbench 3.1 `Avail` now has explicit startup-boundary
  coverage in its DOS 36 resident candidate. The receipt
  `artifacts/avail-wb31-native-20260920-boundaries/qualification.json` passes
  eleven supplied DOS/Exec/AvailMem vectors per CPU (33 total), including
  selector, summary, fail-closed `FLUSH`, Workbench startup and missing-DOS
  paths, balanced cleanup and interleaved callers. Keep CC17 open until exact
  expunge behavior, original diagnostics, PURE/resident reuse, packaging and
  differential evidence pass.
- Progress slice: MorphOS `Status` 50.6 now has a source-bound resident native
  entry preserving the `PROCESS/N,FULL/S,TCB/S,CLI=ALL/S,COM=COMMAND/K`
  `ReadArgs` boundary, legacy DOS CLI-list fallback, process/command filters,
  source-faithful case-insensitive legacy `COM` matching, TCB/FULL formatting
  and Ctrl-C/error paths. Its three-CPU HUNK receipt runs nineteen supplied
  DOS/Exec vectors per CPU, including mixed-case command matching, empty,
  one-process and three-process lists, parser/missing-process failures, Ctrl-C,
  Workbench/boundary rejection, DOS 51.51 provider filtering and interleaved
  repeat calls. Keep CC17 open
  until the DOS 51.51 snapshot path, complete task population, original guest
  parity, PURE/resident lifecycle, packaging and differential evidence pass.
- Progress slice: Workbench 3.1 `Status` now has a separate DOS 36 resident
  candidate using the public legacy CLI-list path. Its three-CPU HUNK receipt
  passes sixteen supplied DOS/Exec vectors per CPU, including case-insensitive
  `COM`, empty, one-process and three-process snapshots, filters, `TCB`/`FULL`, missing-process, parser,
  Ctrl-C, startup-boundary and interleaved calls. Keep the profile open until
  exact classic output and task population, original guest parity,
  PURE/resident lifecycle, packaging and differential evidence pass.
- Progress slice (2026-09-24): both `Status` qualification scripts now pass
  `--exports none`, preventing unrelated exported callbacks in the shared
  native-root assembly from becoming extra roots in the standalone command
  image. MorphOS passes nineteen and Workbench sixteen supplied vectors per
  CPU on 68000/020/040; refreshed receipts are recorded in
  `contracts/Status.md`. HUNK hashes are recorded with the refreshed receipts.
  Original guest parity, exact Workbench behavior, full task
  population, PURE/resident lifecycle, licensing, package placement and
  differential evidence remain open.
- Progress slice (2026-09-24): MorphOS `Status` now follows its released
  source's case-insensitive `strnicmp` comparison for legacy `COM=COMMAND/K`;
  the supplied fixture verifies `COM=rUn` matches the `Run` task. Both profile
  receipts cover this behavior: MorphOS passes nineteen and Workbench sixteen
  vectors per CPU on 68000/020/040. Updated HUNK identities and hashes are
  recorded in `contracts/Status.md`. Guest parity, complete task population,
  PURE/resident lifecycle, licensing, package admission and differential
  comparison remain open.
- Progress slice: the source-bound `Version` contract records the MorphOS
  50.30 template and system/file/resident/MD5 comparison paths together with
  the Workbench 3.1 syntax candidate. Its MorphOS system/RES resident entry
  now follows the source's greater-than comparison policy and prints before
  returning `RETURN_WARN`; the refreshed three-CPU receipt passes twenty
  supplied vectors per CPU, including `$VER:`-prefixed resident `FULL` parsing
  and lower/equal/higher version and revision comparisons, at
  `artifacts/version-morphos-native-20260924-compare-v1/qualification.json`. A
  separate DOS36 Workbench candidate preserves the classic eight-slot
  `NAME,VERSION/N,REVISION/N,FILE/S,FULL/S,UNIT/N,INTERNAL/S,RES/S` grammar,
  now parses the resident `$VER:` tail for `FULL`, and passes fifteen supplied
  vectors per CPU, including explicit fail-closed FILE/UNIT/INTERNAL provider
  paths; latest regression receipt
  `artifacts/version-wb31-native-20260924-md5-regression-v1/qualification.json`.
  FILE fallback/LoadSeg, version.library and ARexx providers, exact classic
  output and positional behavior, and all guest,
  PURE/resident, lifecycle, differential and package gates remain open.
- Progress slice (2026-09-24): MorphOS `Version FILE` now scans for `$VER:`
  directly in the file through public DOS `OpenRaw`, `Read`, and `Seek64`,
  preserving the source's cross-read overlap and MorphOS v0 end-seek path.
  FULL date/extra-text formatting, comparison ordering, multiple names, the
  split-tag boundary, no-tag output and cleanup failures are covered by 32
  supplied DOS/Exec vectors per CPU on 68000/020/040. Receipt
  `artifacts/version-morphos-native-20260924-file-v4/qualification.json` records
  the HUNK hashes and sizes in `contracts/Version.md`. FILE fallback/LoadSeg
  resolution, other provider paths, guest parity, packed correspondence,
  original PURE/resident lifecycle, licensing and package admission remain
  open; the following progress slice records MD5SUM. Workbench
  FILE/UNIT/INTERNAL remain fail-closed.
- Progress slice (2026-09-24): MorphOS `Version MD5SUM` now streams the whole
  file through a resident incremental MD5 implementation while locating
  `$VER:`, prints the uppercase digest before the parsed version, and uses the
  source's unavailable-digest prefix for system and resident output. The
  v0 end-seek optimization is disabled for digest requests. Receipt
  `artifacts/version-morphos-native-20260924-md5-v4/qualification.json` passes
  forty supplied DOS/Exec vectors per CPU on 68000/020/040, including a
  multi-block digest with a split-read `$VER:` tag, checked against the
  independent host MD5 implementation, FULL/date formatting, post-tag read
  failure, Ctrl-C and context-allocation failures, unavailable-digest paths,
  and cleanup.
  Its HUNK hashes and compatibility
  evidence are in `contracts/Version.md`. FILE fallback/LoadSeg, Ambient file
  providers, original guest parity, packed correspondence, exact PURE/resident
  lifecycle, licensing and package admission remain open; CC17 remains
  unchecked.
- Progress slice (2026-09-24): MorphOS `Version` now emits a supplied-vector
  system line from MorphOS, Kickstart and `version.library` fields, supports
  system `FULL`, compares against the Workbench version when the provider opens,
  and preserves the source-shaped Kickstart fallback if it does not. The
  three-CPU resident receipt passes 42 vectors per CPU at
  `artifacts/version-morphos-native-20260924-system-v5/qualification.json`.
  This remains an earlier checkpoint: the follow-ups below close source
  `LibList` lookup and Ambient ARexx request coverage. Ambient file providers
  remain open. The Workbench DOS36
  candidate passes its 15-vector/CPU regression at
  `artifacts/version-wb31-native-20260924-system-regression-v1/`; this is not
  guest parity. Exact providers/output, packed correspondence, original guest,
  PURE/resident lifecycle, licensing and package admission remain open; CC17
  remains unchecked.
- Progress slice (2026-09-24): MorphOS `Version` now sends the source-shaped
  `VERSION` request to the `AMBIENT` ARexx port, parses a successful
  `major.revision` result with DOS `StrToLong`, sets the local `Ambient`
  variable and preserves the source output/result behavior. It uses
  caller-provided A6 rexxsyslib vectors to keep the shared resident image
  unchanged. The refreshed receipt
  `artifacts/version-morphos-native-20260924-ambient-arexx-v1/qualification.json`
  passes 58 supplied DOS/Exec vectors per CPU on 68000/020/040 with zero
  shared-image writes. The separate Workbench candidate still passes fifteen
  vectors per CPU at
  `artifacts/version-wb31-native-20260924-ambient-regression-v1/qualification.json`.
  ABI details are in
  `docs/Commands/Workbench31MorphOS320/reference-captures/version-morphos-ambient-arexx-sdk-abi-20260924.json`.
- Progress slice (2026-09-24): after ARexx failure, MorphOS `Version` now
  follows the captured direct-file fallback order: DOS `GetVar("ambient_path")`,
  `mossys:ambient/ambient`, then `sys:system/ambient/ambient`. It reuses the
  source-backed `$VER:` scanner and balances file/memory ownership. The
  refreshed receipt at
  `artifacts/version-morphos-native-20260924-ambient-files-v1/qualification.json`
  passes 61 supplied vectors per CPU on resident 68000/020/040; Workbench's
  separate 15-vector/CPU regression passes at
  `artifacts/version-wb31-native-20260924-ambient-files-regression-v1/`.
- Progress slice (2026-09-24): the no-`$VER:` Ambient file branch now checks
  DOS `IsFileSystem`, closes the file before calling public DOS `LoadSeg`,
  searches the loaded segment for a Resident, parses its name/`IdString`, and
  unloads the segment on both resident-found and resident-missing paths. The
  refreshed three-CPU resident receipt at
  `artifacts/version-morphos-native-20260924-ambient-loadseg-v1/qualification.json`
  passes 66 vectors per CPU with zero runtime features and zero shared-image
  writes. Coverage includes a resident in a linked segment, non-filesystem,
  LoadSeg-failure and no-resident fallback to the next Ambient provider.
  Workbench's separate 15-vector/CPU regression passes at
  `artifacts/version-wb31-native-20260924-ambient-loadseg-regression-v1/`.
  Original guest comparison, packed/source correspondence, PURE/resident
  lifecycle, licensing and package admission remain open; CC17 remains
  unchecked.
- Progress slice: MorphOS `TaskList` now has a source-bound contract for the
  complete `NAME,ADDRESS/N,VERBOSE/S,STACKTRACE/S,STACKLEVEL/N,NORUN/S,NOWAIT/S,
  NOREADY/S,INTERNAL/S,REGDUMP/S,REGCHECK/S` grammar. Its resident body
  snapshots public current/ready/waiting task lists under `Forbid`/`Permit`, obtains
  public MorphOS task attributes for PID, type, priority, state, stack sizes
  and bounds, signal masks and saved PPC registers, matches `NAME` against the task name or CLI
  command name, supports address filters and list exclusions, classifies CLI
  processes as ` cli` and prints their command names, and passes fifty-four
  supplied DOS/Exec vectors per CPU on 68000/020/040, including the six public
  `NewGetSystemAttrsA` selectors, sysdebug v0 lease and failed-open path, maximum-length
  CLI names, three-node ready/waiting traversal, a 100-ready-task case that
  exhausts the initial 128 KiB buffer and retries at 256 KiB, stopped-task
  `VERBOSE` output, PPC `REGDUMP` formatting, bounded PPC backchain `STACKTRACE`
  with default `STACKLEVEL`, optional SegTracker symbol names, and
  `INTERNAL` emulation/module ABOX offsets and task-exit classification, and
  current-task `VERBOSE` live A7 capture through the compiler's
  `m68k-read-stack-pointer` intrinsic. Partial snapshot cleanup, protected
  foreign-task reads, active-stack bounds for the live sample, and balanced
  `Forbid`/`Permit` cycles are checked. Current receipt:
  `artifacts/tasklist-morphos-native-live-a7-v3/qualification.json`.
  real-guest `REGCHECK` provider parity, complete task population, original
  guest parity, Workbench behavior, PURE/resident lifecycle,
  differential and package gates remain open.
- Progress slice (2026-09-24): The local MorphOS 3.20 SDK archive binds
  `SysDebugFindSeg` to a PPC SysV interface with no packaged 68k inline entry.
  Do not call it from the resident 68k HUNK through a guessed LVO. A qualified
  `SegTracker` semaphore/function-pointer path is implemented as the resident
  candidate for `STACKTRACE` and `REGCHECK`; a guest comparison or verified PPC
  bridge is still needed to establish source-provider parity. Evidence is in
  `docs/Commands/Workbench31MorphOS320/reference-captures/tasklist-sysdebug-sdk-20260924.json`.
- Progress slice (2026-09-24): Rechecking the extracted MorphOS 3.20 TaskList
  and `stackdump.c` corrected the proposed stack-history route: the source does
  not use `TASKINFOTYPE_PPC_STACKHISTORY`. It walks saved PPC frames from GPR1,
  validating each frame and return address with `TypeOfMem` (or captured
  INTERNAL ranges), then resolves addresses through `SysDebugFindSeg`. Do not
  substitute the newer SDK selector without parity evidence. The resident
  candidate uses the documented `SegTracker` semaphore path and now has fixture
  coverage for `ShowReg` classification, bounded foreign-stack reads, and
  INTERNAL labels. The unresolved TaskList gate is guest comparison of that
  path with the source PPC SysDebug provider. Existing task snapshots remain
  protected with `Forbid`/`Permit`; the three-CPU fixture verifies protected
  attribute reads and balanced release.
- Progress slice (2026-09-24): MorphOS `TaskList` now implements bounded saved
  PPC backchain walking for stopped tasks, optional documented `SegTracker`
  symbol resolution, and `INTERNAL` ABOX/task-exit frame labels. A source audit
  caught and fixed a distinction the first fixture missed: TaskList's saved
  stack-frame printer subtracts the emulation start for emulation frames,
  although `stackdump.c`'s separate `ShowPtr` helper subtracts module start for
  both initial-register labels. The refreshed resident receipt passes 32
  supplied vectors per CPU on 68000/020/040, including both ABOX offsets:
  `artifacts/tasklist-morphos-native-stacktrace-v3/qualification.json`.
  This fixture evidence does not establish output/provider parity on a real
  MorphOS guest. Running-task `VERBOSE` was subsequently completed with a
  compiler A7-read intrinsic and is covered by the 54-vector receipt below.
  Original guest comparison, lifecycle, PURE/resident evidence, licensing and
  package admission remain open.
- Progress slice (2026-09-24): MorphOS `TaskList REGCHECK` now follows the
  inspected `stackdump.c` classification order for valid/internal/symbol
  pointers, exact Exec library/device/resource nodes, library/device function
  and base ranges, current/ready/wait tasks, Process stream/lock/CLI handles,
  ports and semaphores. The fixture covers these categories, ETask interior
  offsets, PPC and 68k stacks, all five Process pointer fields using raw and
  BADDR forms, SegTracker symbol output, and INTERNAL module/emulation labels.
  Its resident HUNK qualification passes 54 supplied DOS/Exec vectors per CPU
  on 68000/020/040 with no managed allocations, runtime helpers, external
  native targets, exception regions or fatal machine-fault sites:
  `artifacts/tasklist-morphos-native-regcheck-v1/qualification.json`.
- ETask range note (2026-09-24): The implementation uses a 114-byte compiled
  extent derived from `exec/tasks.h` in the pinned SDK archive under its
  `#pragma pack(2)` layout, because `stackdump.c` itself bounds the interior
  check with `sizeof(struct ETask)`. The bound is recorded in
  `docs/Commands/Workbench31MorphOS320/reference-captures/tasklist-etask-sdk-20260924.json`;
  it is version-bound source-layout evidence, not proof of every allocation's
  actual size or of compatibility with later headers. The original guest must
  still confirm boundary behavior and absence of unsafe reads before admission.
- Blocker detail (2026-09-24): REGCHECK's source-shaped fixture is complete,
  but the candidate pointer provider and classification have not yet been
  compared against a clean MorphOS 3.20 guest. In particular, the legacy
  SegTracker semaphore/function-pointer route substitutes for the source's PPC
  SysDebug provider in this resident 68k candidate; guest output and failure
  behavior still need to establish parity.
- Progress slice (2026-09-23): MorphOS `CPU`, `Stat`, and `ShowConfig` now have
  hash-bound ISO identities and explicit CC17 contracts/audits. CPU and Stat
  have literal installer `+P` evidence; ShowConfig has no observed protection
  event, so purity remains unresolved. Their packed templates, providers,
  output, guest parity, lifecycle and package gates remain open; no guessed
  implementation is admitted.
- **Dependencies:** Exec/DOS inspection APIs and appropriate CPU/config providers.
- Preserve memory classes, totals/largest-block and flush/query modes, version
  sources and comparisons, process/CLI filters, requested columns, and CPU/cache
  query/control options. Inspect guest state, not the host environment.
- Keep list critical sections short; snapshot under the documented protection,
  release protection, then format output. No blocking I/O or requester while
  Exec/DOS list protection is held. Handle disappearing tasks and malformed data.
- **Exit:** accurate guest output, version parsing and unavailable-feature errors;
  CPU changes use valid guest control APIs. Preserve requested persistent changes;
  restore temporary state and perform failure rollback only where appropriate.

### CC18 - Process control and system registries

- [ ] Commands: `Break`, `ChangeTaskPri`, `iKill`, `DevList`, `LibList`, `LoadLib`,
  `FlushLib`, `ModList`, `ResList`, `PortList`.
- Reference slice (2026-09-27): MorphOS Library documents provisional
  ReadArgs-shaped contracts for the remaining `LoadLib`, `FlushLib`, and
  `iKill` candidates. Per-command evidence boundaries and the next binary/guest
  probes are in `docs/Commands/Workbench31MorphOS320/contracts/LoadLib.md`,
  `FlushLib.md`, and `iKill.md`. These pages do not substitute for the frozen
  MorphOS 3.20 binary or guest captures; keep all three implementation and
  admission rows open.
- Progress slice: MorphOS `ResList` 50.4 now has a source-bound resident native
  entry preserving the no-option DOS 37 startup, `Forbid`-protected Exec
  resource-list snapshot, growable buffer, source formatting and per-row
  Ctrl-C polling. Its three-CPU HUNK receipt passes eleven supplied DOS/Exec
  vectors per CPU (33 total), including empty, one-resource and three-resource
  lists, allocation, break and missing-DOS paths, Workbench/boundary rejection,
  and interleaved repeat calls. Keep CC18 open until complete list population,
  original guest parity, PURE/resident lifecycle, packaging, and differential
  evidence pass. Receipt: `artifacts/reslist-morphos-native-20260919-multinode-v2/qualification.json`.
- Progress slice: MorphOS `LibList` 50.6 now has a source-bound resident native
  entry preserving the no-option DOS 37 startup, `Forbid`-protected library
  snapshot, address/name/version/revision/open-count/flags fields, growable
  buffer, source formatting and per-row Ctrl-C polling. Its three-CPU HUNK
  receipt passes eleven supplied DOS/Exec vectors per CPU (33 total), including
  empty, one-library and three-library lists, allocation and break paths,
  Workbench/boundary rejection, missing-DOS startup failure, and interleaved
  repeat calls. Keep CC18 open until complete list population,
  packed-binary/source correspondence, original
  guest parity, PURE/resident lifecycle, packaging, and differential evidence
  pass. Receipt: `artifacts/liblist-morphos-native-20260919-multinode-v2/qualification.json`.
- Progress slice: MorphOS `DevList` 50.3 now has a source-bound resident native
  entry preserving the no-option DOS 37 startup, `Forbid`-protected device
  snapshot, address/name/version/revision/open-count/flags fields, growable
  buffer, source formatting and per-row Ctrl-C polling. Its three-CPU HUNK
  receipt passes eleven supplied DOS/Exec vectors per CPU (33 total), including
  empty, one-device and three-device lists, allocation, break and missing-DOS
  paths, Workbench/boundary rejection, and interleaved repeat calls. Keep CC18
  open until complete device population, packed-binary/source correspondence,
  original guest parity, PURE/resident lifecycle, packaging, and differential
  evidence pass. Receipt: `artifacts/devlist-morphos-native-20260919-multinode-v2/qualification.json`.
- Progress slice: MorphOS `PortList` 50.2 now has a source-bound resident native
  entry preserving the no-option DOS 37 startup, `Forbid`-protected message-
  port snapshot, copied port/task names, source table bytes and per-row Ctrl-C
  polling. Its three-CPU HUNK receipt passes eleven supplied DOS/Exec vectors
  per CPU (33 total), including empty/one-port/three-port lists, allocation and
  break paths, missing DOS, Workbench/boundary rejection, and interleaved repeat
  calls. Keep CC18 open until complete list population, packed-binary/source
  correspondence, original guest parity, PURE/resident lifecycle, packaging,
  and differential evidence pass.
- Progress slice: MorphOS `ModList` 50.4 now has a source-bound resident native
  entry preserving the `VERBOSE/S` `ReadArgs` boundary, DOS 37 startup,
  `Forbid`-protected resident-module indirection traversal, revision parsing,
  source table/flag ordering and per-row Ctrl-C polling. Its three-CPU HUNK
  receipt passes thirteen supplied DOS/Exec vectors per CPU (39 total), including
  empty, one-resident and three-resident lists, ID-string revision parsing,
  allocation/parser/Ctrl-C/missing-DOS paths, Workbench/boundary rejection, and
  interleaved repeat calls. Keep CC18 open until complete resident-table
  population, packed-binary/source correspondence, original guest parity,
  PURE/resident lifecycle, packaging, and differential evidence pass. Receipt:
  `artifacts/modlist-morphos-native-20260919-multinode-v2/qualification.json`.
- Progress slice: Workbench 3.1 `Break` and `ChangeTaskPri` now have separate
  DOS 36 resident syntax-candidate entries with explicit startup boundaries.
  Their current three-CPU receipts pass ten and thirteen supplied Exec/DOS vectors per
  CPU respectively, covering target resolution, signal masks, signed priority
  bounds, parser failures, Workbench startup rejection, missing-DOS startup
  failure and interleaved reuse with zero runtime features/helpers,
  exception/fatal sites, leaks or shared-image writes. Receipts:
  `artifacts/break-wb31-native-20260920-boundaries/qualification.json` and
  `artifacts/changetaskpri-wb31-native-20260927-range-precedence-v1/qualification.json`.
  Exact classic diagnostics/effects, PURE/resident lifecycle, packaging and
  differential evidence remain open.
- Progress slice (2026-09-23): MorphOS `ChangeTaskPri` now routes its
  missing-process diagnostic to the current process's `pr_CES` stream when
  present and falls back to public DOS `Output()` otherwise, matching the
  inspected source. The refreshed resident 68000/020/040 receipt passes twelve
  supplied vectors per CPU, including both stream paths, at
  `artifacts/changetaskpri-morphos-native-20260923-error-stream-v2/qualification.json`.
  Exact guest diagnostics, PID numbering/reuse, task liveness/races,
  PURE/resident lifecycle, package placement and differential evidence remain
  open.
- Original guest parser baseline (2026-09-26): normal Workbench startup now
  runs `C:Break` and `C:ChangeTaskPri` with their required positional
  arguments omitted. Original DOS ReadArgs returns level 20 for each; both
  exact streams and caller post-System IoErr 116 are bound in
  `artifacts/workbench31-guest-command-cc18-parser-baselines-20260926-v1/qualification.json`. These cases perform no signal or priority effect. Native
  candidate diagnostics were compared against these originals, corrected,
  and now match for these exact invocations. A third guest pair records the
  valid current-task `C:ChangeTaskPri 0` success result in
  `artifacts/workbench31-guest-command-cc18-command-pairs-20260927-v1/qualification.json`;
  it does not read the changed priority back. Other target behavior and option
  or error combinations still need original/replacement cases.
- Current-source requalification (2026-09-23): Workbench `ChangeTaskPri`
  still passes twelve supplied vectors per CPU after the shared runner change.
  Its resident HUNK now reports eleven reachable methods with no runtime
  helpers/features, external targets, exceptions or fatal sites at
  `artifacts/changetaskpri-wb31-native-20260923-error-stream-fixture-v2/qualification.json`.
  Exact original behavior and profile admission remain open.
- **Dependencies:** process/signal and system-registry owners.
- Implement exact targeting, signals, priority limits, library open/close/
  expunge behavior and registry filters, including each command's result codes.
- Protect active references and resident use counts; avoid unsafe termination,
  races with node removal, or a loader handle that keeps a supposedly flushed
  library alive. Completion and resource reclamation must use the process owner.
- **Exit:** success/failure cases include concurrent registry changes and target
  death, without corrupt lists, leaked opens, or host process manipulation.

## 2026-09-27 implementation checkpoint - Workbench Break and ChangeTaskPri bounded guest parity

The generalized disposable Workbench guest-capture path now supports one named
existing `C/<command>` HUNK substitution while preserving the original member
metadata. The first paired runs exposed that both native `ReadArgs` failures
used a command-prefixed `PrintFault` and returned level 10. Workbench 3.1 emits
the raw fault and returns 20, so both command bodies now pass a null fault header
and return `DOS.RETURN_FAIL`. Their supplied-vector fixtures assert the null
header and return 20 on parser failure without weakening the command-header
checks for other ChangeTaskPri faults.

Fresh resident receipts pass ten Break and twelve ChangeTaskPri invocations per
CPU across 68000/020/040. Normal-startup original/candidate guest pairs match
command text, every output byte and length, return20, and caller post-System
IoErr116 for `C:Break` without PROCESS and `C:ChangeTaskPri` without PRIORITY.
Each parser case fails in DOS `ReadArgs` before target lookup, Signal or
SetTaskPri. A third pair runs valid `C:ChangeTaskPri 0` with empty output,
return0 and caller IoErr0. Two further pairs use nonexistent CLI target 999999:
both original commands print the unprefixed `Process 999999 does not exist`
diagnostic, return20 and leave caller IoErr0; candidate bytes match exactly.
No target is signaled and no priority is changed in these failure cases. The
combined 91-identity receipt is
`artifacts/workbench31-guest-command-cc18-command-pairs-20260927-v2/qualification.json`;
the parser-only receipt remains retained. The generalized probe tooling passes
41 tests.

This closes two missing-required-argument cases, two missing-target diagnostics
and one bounded ChangeTaskPri success result. The probe does not read the
changed task priority back. Break signal effects, other valid targets, option
forms, remaining errors/result precedence, MorphOS behavior, resident/PURE lifecycle,
CopperStart integration, media/rights and packaging remain open; shipping
stays 0/200 commands and 0/246 profiles.

## 2026-09-27 implementation checkpoint - Workbench Break and ChangeTaskPri effects

The disposable guest probe now expands a single `@SELF@` marker to its own DOS
CLI task number and waits 150 DOS ticks before System, creating a stage-3
pre-effect snapshot. The passive analyzer records the owner's CLI number,
signed priority and received signal mask from saved RAM. The comparison tool
can require a before/after priority pair or signal bit and rejects missing
states or mismatched effects.

Original and candidate `C:ChangeTaskPri 42 PROCESS @SELF@` guests both change
CLI task 1 from priority 0 to 42. Original and candidate
`C:Break @SELF@ C` guests both set the Ctrl-C signal bit on that task: its
received mask changes from `0x00000004` to `0x00001104`. Each pair has empty
output, return 0 and caller post-System IoErr 0. The comparison receipts are
`artifacts/workbench31-guest-command-changetaskpri-self-priority-candidate-20260927-v1/effect-comparison-v2.json`
and
`artifacts/workbench31-guest-command-break-self-signal-candidate-20260927-v1/effect-comparison.json`.
Both candidates preserve original protection word 0; the Workbench resident
HUNKs still pass ten and twelve supplied vectors per CPU, respectively. The 47
focused probe-tool tests pass.

This verifies one selected CLI target, one ChangeTaskPri value and Break's C
mask. It does not close other target forms, priority boundaries, Break's
default/ALL/D/E/F/combined masks, MorphOS PID behavior, actual installed
resident/PURE lifecycle, licensing, packaging or shipping. The guest priority
change and pending signal exist only in disposable diagnostic guests.

## 2026-09-27 follow-up checkpoint - Workbench ChangeTaskPri range error

The original and refreshed candidate were run as
`C:ChangeTaskPri 128 PROCESS @SELF@`. Original Workbench prints
`Priority out of range (-128 to +127)` followed by the Shell failure footer,
returns 20 and leaves caller post-System IoErr at 0. The resident candidate
initially used `PrintFault(ObjectTooLarge)`, producing different text and
IoErr 207. Its Workbench-only range branch now emits the captured message and
clears IoErr; MorphOS retains its separate `PrintFault` path.

A separate pair requested an out-of-range priority and a nonexistent CLI
target. Original Workbench still reports the priority-range error before it
tries target lookup; the candidate initially reported the missing process.
The Workbench body now checks range before `Forbid` and `FindCliProc`. The
[precedence comparison](../artifacts/workbench31-guest-command-changetaskpri-range-missing-self-candidate-20260927-v1/effect-comparison.json)
matches the original's 74-byte output, return 20, caller IoErr 0 and unchanged
owner priority. The fresh resident receipt
`artifacts/changetaskpri-wb31-native-20260927-range-precedence-v1/qualification.json`
passes thirteen supplied vectors per CPU across 68000/020/040.

The [guest comparison](../artifacts/workbench31-guest-command-changetaskpri-128-self-candidate-fixed-20260927-v1/effect-comparison.json)
is `captured-case-equal`: exact 74-byte output, return 20, caller IoErr 0, and
CLI task 1 still at priority 0 before and after. The updated
`artifacts/changetaskpri-wb31-native-20260927-range-fix-v1/qualification.json`
passes twelve supplied vectors per CPU on resident 68000/020/040 HUNKs with
13 reachable methods and no runtime helpers/features, external targets,
exceptions, fatal sites, leaks or shared-image writes. The guest tooling suite
passes 47 tests.

This closes both captured out-of-range values with an existing CLI target.
Other target/priority precedence, MorphOS behavior, installed PURE/resident
lifecycle, licensing, packaging and full profile closure remain open; the
replacement remains diagnostic-only.

## 2026-09-27 follow-up checkpoint - Workbench valid priority endpoints

Original and candidate guests ran `C:ChangeTaskPri 127 PROCESS @SELF@` and
`C:ChangeTaskPri -128 PROCESS @SELF@` in fresh derivatives. Both invocations
returned 0 with empty output and caller IoErr 0. Saved guest RAM confirms CLI
task 1 changed from priority 0 to 127 and from 0 to -128, respectively, on
both sides. The comparisons are
`artifacts/workbench31-guest-command-changetaskpri-pri127-candidate-20260927-v1/effect-comparison.json`
and
`artifacts/workbench31-guest-command-changetaskpri-pri-minus128-candidate-20260927-v1/effect-comparison.json`.

This verifies both legal signed endpoints as guest effects, in addition to the
two rejected endpoints and range-before-missing-target precedence above. It
does not close other task targets, all parser/result combinations, MorphOS PID
semantics, resident/PURE lifetime, licensing or packaging.

## 2026-09-27 follow-up checkpoint - Workbench ChangeTaskPri keyword alias

Fresh original and candidate guests now match for
`C:ChangeTaskPri PRIORITY=42 PROCESS @SELF@`. The original accepts the full
`PRIORITY=` alias and equals form; both guests change CLI task 1 from priority
0 to 42, return 0, emit no output and leave caller post-System IoErr at 0.
The effect-aware comparison is
`artifacts/workbench31-guest-command-changetaskpri-priority-alias-candidate-20260927-v1/effect-comparison.json`.
The separate `C:ChangeTaskPri 42 PROCESS=@SELF@` pair also matches and confirms
the `PROCESS/K/N` equals form, with the same observed 0-to-42 effect and exact
return/output/IoErr. Its receipt is
`artifacts/workbench31-guest-command-changetaskpri-process-equals-candidate-20260927-v1/effect-comparison.json`.
These add two bounded keyword/equal-form success cases; other target forms
and priority values, MorphOS PID parity, complete parser/result coverage,
resident/PURE lifetime, rights and package admission remain open.

## 2026-09-27 follow-up checkpoint - Workbench ChangeTaskPri numeric parse error

The original and current candidate guests also match for
`C:ChangeTaskPri nope`: both emit the exact 48-byte
`bad number\nC:ChangeTaskPri failed returncode 20\n`, return 20 and leave
caller post-System IoErr at 115. The paired receipt is
`artifacts/workbench31-guest-command-changetaskpri-priority-nonnumeric-candidate-20260927-v1/effect-comparison.json`.
This confirms the nonnumeric required argument fails during DOS parsing before
target lookup or priority mutation. Other numeric boundaries and the broader
parser/result matrix remain open.

## 2026-09-27 follow-up checkpoint - Workbench ChangeTaskPri oversized input

Original and candidate guests match for priority strings `2147483648` and
`-2147483649`: each emits the exact 74-byte Workbench range diagnostic plus
failure line, returns 20 and leaves caller post-System IoErr at 0. The paired
receipts are
`artifacts/workbench31-guest-command-changetaskpri-priority-overflow-positive-candidate-20260927-v1/effect-comparison.json`
and
`artifacts/workbench31-guest-command-changetaskpri-priority-overflow-negative-candidate-20260927-v1/effect-comparison.json`.
This proves only those two oversized strings' observed outputs/results, not a
general conversion rule or complete malformed-input behavior.

## 2026-09-27 follow-up checkpoint - Workbench ChangeTaskPri minimum PROCESS

For `C:ChangeTaskPri 0 PROCESS=-2147483648`, original and candidate guests
emit the exact 72-byte missing-process diagnostic, return 20 and leave caller
post-System IoErr at 0. The paired receipt is
`artifacts/workbench31-guest-command-changetaskpri-process-min-signed-candidate-20260927-v1/effect-comparison.json`.
This adds the minimum signed target-number case only; other target identities,
MorphOS PID semantics, target races, full command lifecycle and packaging
remain open.

## 2026-09-27 follow-up checkpoint - Workbench Break masks

Original and candidate guests now match for the default Ctrl-C mask, explicit
C, each individual D/E/F switch, `ALL`, and combined D/E and D/F. The requested
bits were clear before each run and changed to the expected masks: C/default
`0x1000`, D `0x2000`, E `0x4000`, F `0x8000`, ALL `0xF000`, D/E `0x6000`, and
D/F `0xA000`.
Each pair has empty output, return 0, caller IoErr 0, and the same observed
signal effect. The comparator checks complete required masks; its 49 focused
tests pass. Receipts and profile limits are recorded in `contracts/Break.md`
and the CC18 evidence table. Other mask combinations/targets, MorphOS behavior,
installed PURE/resident lifetime, licensing and packaging remain open.

### CC19 - Handler and volume lifecycle

- [ ] Commands: `DiskChange`, `Lock`, `Mount`, `UnMount`, `FSList`, `FSDie`,
  `FSPrefs`, `RunFS`, `DiskCache`, `RemRAD`.
- Progress slice: MorphOS `Lock` 50.5 now has a source-bound resident native entry
  with the original `DRIVE/A,ON/S,OFF/S,PASSKEY` contract. Its three-CPU HUNK
  receipt passes eleven supplied DOS vectors per CPU (33 total), covering
  write-protect packet arguments, decimal passkey conversion, device-type and
  missing-device paths, diagnostics, parser failure, Workbench startup, and
  interleaved repeat calls. Keep CC19 open until real guest handler transitions,
  PURE/resident lifecycle, packaging, and differential evidence pass.
- Progress slice: Workbench 3.1 `Lock` now has a separate DOS 36 resident syntax
  candidate using the Disk 1/2 `DRIVE/A,ON/S,OFF/S,PASSKEY` boundary. Its
  three-CPU receipt passes eleven supplied DOS vectors per CPU, including packet
  arguments, passkey conversion, parser/device failures, startup rejection and
  repeat use: `artifacts/lock-wb31-native-20260917-qualified-v2/qualification.json`.
  Classic guest parity, packet/result diagnostics, PURE/resident lifecycle,
  packaging and differential evidence remain open.
- Progress slice: Workbench 3.1 `DiskChange` now has a separate DOS 36 resident
  syntax candidate using the Disk 1/2 `DEVICE/A` boundary. Its three-CPU receipt
  passes eleven supplied DOS vectors per CPU, including two-stage inhibit
  handling, parser/device failures, startup and missing-DOS rejection and
  repeat use:
  `artifacts/diskchange-wb31-native-20260920-boundaries/qualification.json`.
  Classic guest parity, packet/result diagnostics, PURE/resident lifecycle,
  packaging and differential evidence remain open.
- Progress slice: MorphOS `DiskChange` 50.3 now has a source-bound resident
  native entry with the `DEVICE/A` contract, DOS 50 startup, `DeviceProc`, the
  two-stage `ACTION_INHIBIT` packet sequence, source diagnostics, invalid-entry
  rejection and ordered cleanup. Its three-CPU HUNK receipt passes ten supplied
  DOS vectors per CPU (30 total), including parser/device/inhibit failures,
  Workbench startup and interleaved repeat calls. Keep CC19 open until real
  guest handler transitions, PURE/resident lifecycle, packaging, and differential
  evidence pass.
- **Dependencies:** DOS handler startup/packets, removable/RAD/cache services.
- Preserve mountlist/DOSDriver search, configuration parsing, activation,
  preferences and cache controls, write-protection and unmount/termination rules.
  FSPrefs uses handler-owned HELP/GET/SET; RunFS needs background launch, filesystem
  readiness, command-tail preservation and optional desktop opening. Verify each
  command's actual grammar before sharing a helper.
- Handle devices with outstanding locks, active I/O, startup failure, malformed
  configuration, missing handler, disk-change races and refused operations.
  Keep filesystem implementations separate and preserve original warnings.
- **Exit:** real guest handler transitions and rollback are proven with both
  accepting and rejecting handlers; no host mount shortcut or false success.

### CC20 - Boot services and classic preferences

- [ ] Commands: `BindDrivers`, `SetPatch`, `LoadResource`, `AddDataTypes`, `IPrefs`,
  `LoadMonDrvs`, `SetKeyboard`, `SetFont`.
- Progress slice (2026-09-26): Workbench `LoadResource` now has the bounded
  production request protocol, `LoadSeg` hook/cache, opened-resource registry,
  library/device/font/catalog actions, diagnostics and single-request worker.
  Native fixtures pass nineteen client, eleven hook, ten registry, thirty-four
  action, thirty-one worker and fifteen launch scenarios per CPU on 68000/020/040. They caught
  and fixed packed-message/export-DOS defects, hook ownership/ordering issues
  and unsupported worker argument assignment. Worker checks use 3000-byte
  stacks and preserve sender arguments, stored errors and borrowed context.
  The corrected startup/action audits bind original process tags, detached
  segment ownership, resource classification and error/diagnostic ordering.
  Lazy catalog helper calls now preserve the source's saved pointer and nested
  closes; hook discovery uses the original Latin-1 service name. Launch passes
  the detached worker chain through classic NP_Seglist/FreeSeglist, with rollback
  on failure. A strict two-HUNK packer has independent tests, but its combined
  probe artifacts are not runnable commands. The current receipts are linked
  in `contracts/LoadResource.md`. These slices
  do not yet implement the full command or satisfy the
  catalog/font edge-case parity, worker-lifecycle, guest, `PURE`/resident admission, rights, or
  package gates; keep the command checkbox open.
- Progress slice (2026-09-25): MorphOS `AddDataTypes` now has resident shared-
  list/LIST behavior, four-slot `ReadArgs` ownership and mapping, required
  library floors with reverse partial-open cleanup, FILES/IFF/DTHD/DTCD
  handling, and a source-ordered REFRESH path. The latest three-CPU resident
  receipt `artifacts/adddatatypes-morphos-files-iff-20260925-v13/qualification.json`
  passes twenty-five command-fixture cases plus one callback-wrapper probe per
  CPU; the command root has 44 reachable methods and the probe has five, with
  no managed/runtime helpers, external targets, faults, leaks or image writes.
  It verifies DTCD copy/state and loader arguments plus REFRESH lock alias
  deduplication, unchanged-date skips, empty-directory scans, date-stamp
  handling, scan order and `pr_WindowPtr` restoration. FILES cases also verify
  first-directory entry and clearing `APF_DIDDIR` without losing unrelated
  anchor flags. The wrapper probe caught
  and fixed the read adapter's buffer/count mapping to the published A0/D0
  registers. All callback and REFRESH evidence is synthetic CPU-fixture work;
  the real MorphOS DOS loader and guest date behavior remain unverified.
  Deeper nonrecursive directory traversal, malformed/duplicate/open descriptor cases, exact
  `AROS_STACKSIZE`, segment unload, actual CLI/Workbench entry paths, the
  Workbench profile, private ABI confirmation, guest comparison, PURE,
  licensing and package gates remain open. The earlier v2 receipt was
  invalidated after its 68000 HUNK was overwritten by a failing
  requalification attempt.
- Follow-up (2026-09-25): MorphOS `AddDataTypes` now owns its CLI orchestration
  in production `Run` and its Workbench startup processing in
  `RunWorkbenchStartup`; the resident test entry delegates to those methods
  instead of duplicating their library, list and option flow. The refreshed
  receipt `artifacts/adddatatypes-morphos-cli-run-20260925-v18/qualification.json`
  passes 38 command-fixture cases plus one callback-wrapper probe per CPU on
  resident 68000/020/040 HUNKs. The command root has 48 reachable methods; the
  images have no managed allocations, runtime helpers, external targets,
  exceptions, fatal sites, leaks or shared-image writes. This exercises the
  production lifecycle against supplied vectors, not an authentic MorphOS
  guest; original parity, callback loader/unload behavior, Workbench guest
  parity, PURE/resident classification, licensing and package admission remain
  open.
- Follow-up (2026-09-25): added a separate Workbench 3.1 `AddDataTypes` body
  using the captured classic dependency floors and a three-slot Kickstart DOS
  `ReadArgs` template, with no MorphOS `LIST` option. Its five supplied-vector
  cases per CPU cover dependency open/close order, optional `sys/c.catalog`,
  existing-list reuse, the `AllocNamedObjectA` tag vector and `no_Object`
  publication path, `InitSemaphore`, parser failure, and partial-open cleanup.
  The fixture exposed and fixed preservation of DOS `IoErr` after partial
  library cleanup. The receipt
  `artifacts/adddatatypes-workbench31-native-20260925-v1/qualification.json`
  passes resident 68000/020/040 images with 47 reachable methods and zero
  shared-image writes. MorphOS regression passes its 38 command cases plus a
  callback-wrapper probe per CPU. These fixtures do not prove real Workbench or
  MorphOS guest semantics, full classic list ownership/ABI, DOS callback
  unloading, `PURE`/resident classification, licensing, or package admission.
  The CC20 family remains unchecked and contributes no shipping profile.
- Follow-up (2026-09-25): expanded the Workbench fixture with four production
  `REFRESH` cases for a changed date, unchanged date, empty directory, and
  missing `DEVS:DataTypes` lock. The assertions check the classic
  `DEVS:DataTypes/#?` scan gate, date-stamp update, DOS lock/FIB ownership,
  requester-window restoration, and matcher cleanup. The refreshed receipt
  `artifacts/adddatatypes-workbench31-native-20260925-v2/qualification.json`
  passes nine cases per resident 68000/020/040 image with 47 reachable methods
  and zero shared-image writes. The MorphOS regression remains green at 38
  command cases plus its callback probe per CPU. These supplied-vector fixtures
  do not replace original-guest comparisons or close classic list ownership,
  loader lifetime, `PURE`/resident, licensing, or packaging gates.
- Follow-up (2026-09-25): the Workbench `FILES/M` slot now has resident cases
  for one and multiple patterns, plus a valid DTHD match and registration. A
  combined FILES/QUIET/REFRESH vector checks that all three result slots retain
  their captured positions and that REFRESH takes the classic wildcard path in
  precedence to FILES. The refreshed receipt
  `artifacts/adddatatypes-workbench31-native-20260925-v4/qualification.json`
  passes thirteen vectors per 68000/020/040 image, with 47 reachable methods,
  no shared-image writes and no resource leaks. MorphOS regression remains
  green; these remain supplied-vector results rather than guest parity.
- Follow-up (2026-09-25): added a named-object bootstrap failure case where
  `AllocNamedObjectA` returns an object with a null `no_Object`. The Workbench
  body releases that object before returning failure and does not initialize or
  publish the list; the fixture checks the lease and allocation boundary. The
  refreshed receipt
  `artifacts/adddatatypes-workbench31-native-20260925-v5/qualification.json`
  passes fourteen cases per resident CPU. Guest Utility semantics and full
  classic object/list lifetime rules remain open.
- Follow-up (2026-09-25): MorphOS `AddDataTypes` now explicitly qualifies
  datatype code-segment ownership across duplicate handling. Three new cases
  prove, within supplied vectors, that an open descriptor retains its segment,
  a same/open duplicate unloads only its newly loaded candidate segment after
  releasing the copied DTCD buffer, and replacement of a closed descriptor
  unloads its old segment before freeing the record. Receipt
  `artifacts/adddatatypes-morphos-segment-lifetime-20260925-v19/qualification.json`
  passes 41 command cases and one callback-wrapper probe per CPU on resident
  68000/020/040 HUNKs, with 48 reachable methods and no shared-image writes or
  fixture leaks. This checks candidate ownership ordering, not real DOS
  `UnLoadSeg` behavior or guest safety; exact `AROS_STACKSIZE`, authentic
  callback/unload behavior, private ABI confirmation, guest parity, PURE,
  licensing, and package admission remain open.
- Follow-up (2026-09-25): kept the two profiles' embedded-DTCD loader stacks
  distinct. The Workbench 3.1 HUNK passes 4096 at the captured embedded
  `InternalLoadSeg` site; the MorphOS source uses `AROS_STACKSIZE`, and the
  pinned official ppc-morphos header defines it as 32768. The native engine
  now passes these values through CLI scanning, refresh, and Workbench startup
  according to the selected profile. Added resident cases for MorphOS DTCD
  loading during Workbench startup and Workbench 3.1 CLI DTCD loading. The
  MorphOS three-CPU receipt
  `artifacts/adddatatypes-morphos-profile-stacks-20260925-v20/qualification.json`
  passes 42 command vectors plus a callback-wrapper probe per CPU; the
  separate Workbench receipt
  `artifacts/adddatatypes-workbench31-native-20260925-v6/qualification.json`
  passes fifteen vectors per CPU. The source/header/binary provenance and
  limits are recorded in
  `docs/Commands/Workbench31MorphOS320/reference-captures/adddatatypes-stacksize-audit-20260925.json`.
  These fixtures prove the candidate arguments only. The exact historical
  header used to build MorphOS AddDataTypes 50.6, authentic MorphOS loader
  behavior, guest parity, PURE, licensing, and package admission remain open;
  shipping stays 0/200 commands and 0/246 profiles.
- Progress slice (2026-09-24): MorphOS `BindDrivers` now matches the inspected
  source's whole-string `PRODUCT` `=` search and signed decimal-prefix `atoi`
  parsing, including its slash/pipe search positions and repeated
  `FindConfigDev` order. The refreshed resident receipt
  `artifacts/binddrivers-morphos-native-entry-20260924-product-parser-v6/qualification.json`
  compiles 68000/020/040 images with fourteen reachable command methods and no
  managed runtime features/helpers, external native targets, exception regions,
  fatal fault sites or shared-image writes. Seven scanner-boundary vectors per
  CPU still cover library and matcher ownership only. A separate resident entry
  invokes the production PRODUCT parser on five strings and verifies seven
  pairs across nine `FindConfigDev` calls, including signed/whitespace numeric
  prefixes, embedded `=`, multiple pairs, empty input, repeated provider nodes
  and `cd_NextCD` prepend order; its three-CPU receipt also has zero shared
  writes and no managed/runtime helpers.
- Follow-up (2026-09-24): a provider-backed successful MorphOS scan now runs
  through MatchFirst, NameFromLock, AddPart, Icon `GetDiskObjectNew` and
  `FindToolType`, PRODUCT/ConfigDev chain construction, `.info` removal, raw
  LoadSeg, resident discovery, `SetCurrentBinding`, and successful
  `InitResident`. The three-CPU receipt
  `artifacts/binddrivers-morphos-native-entry-20260924-driver-success-v7/qualification.json`
  passes eight scanner vectors plus the separate parser vector per CPU, with
  fourteen reachable command methods and no managed runtime helpers/features,
  external targets, exception regions, fatal sites or shared-image writes.
  Failure-provider branches, end-to-end multiple PRODUCT pairs, Workbench
  correspondence, original-guest parity, PURE/resident lifecycle, licensing
  and package admission remain open.
- Follow-up (2026-09-24): the MorphOS scanner now also qualifies directory
  entries; missing Icon objects, PRODUCT and ConfigDev; missing residents;
  LoadSeg and InitResident failures; and NameFromLock/AddPart warnings with
  source-specific PrintFault and IoErr checks. The refreshed three-CPU receipt
  `artifacts/binddrivers-morphos-native-entry-20260924-failure-paths-v8/qualification.json`
  passes seventeen scanner vectors plus the separate parser vector per CPU,
  with no shared-image writes or fixture leaks. Malformed PRODUCT/HUNK bounds,
  end-to-end multiple PRODUCT pairs, repeated provider-backed scans, Workbench
  correspondence, original-guest parity, installed PURE/resident metadata,
  licensing and package admission remain open.
- Follow-up (2026-09-24): MorphOS resident discovery now checks segment
  address arithmetic and declared HUNK extents with public Exec `TypeOfMem`,
  bounds each Resident-record read, walks a resident in the second HUNK, and
  rejects overflowing/truncated extents and cyclic segment links. Cycle
  detection uses a constant-space slow/fast walk, so valid segment lists are
  not capped by an arbitrary count. The refreshed resident receipt
  `artifacts/binddrivers-morphos-native-entry-20260924-hunk-bounds-v9/qualification.json`
  passes twenty-three scanner vectors plus the parser vector per CPU on
  68000/020/040, with fifteen reachable methods and zero managed runtime
  features/helpers, external targets, exception regions, fatal sites, leaks
  or shared-image writes. End-to-end multiple PRODUCT pairs, malformed
  unterminated tool-type strings, repeated/interleaved provider scans,
  Workbench correspondence, real guest parity, installed PURE/resident
  metadata, licensing and package admission remain open.
- Follow-up (2026-09-24): the full MorphOS scan now exercises two PRODUCT
  pairs through `SetCurrentBinding`. The first pair resolves two `ConfigDev`
  nodes and the second resolves a third; the fixture checks all five ordered
  `FindConfigDev` calls and the resulting third-to-second-to-first prepend
  chain. Receipt
  `artifacts/binddrivers-morphos-native-entry-20260924-multipair-v10/qualification.json`
  passes twenty-four scanner vectors plus the separate parser vector on
  68000/020/040, with fifteen reachable methods, no managed runtime features
  or helpers, no external targets, exception regions or fatal sites, and no
  leaks or shared-image writes. Malformed unterminated tool-type strings,
  provider-backed repeated/interleaved calls, Workbench correspondence, real
  guest parity, installed PURE/resident metadata, licensing and package
  admission remain open.
- Follow-up (2026-09-24): the fixture now interleaves a one-pair and a
  multiple-pair successful scan instruction by instruction, with invocation-
  local Icon objects, PRODUCT strings, matcher records, ConfigDev nodes and
  HUNK fixtures. Directory-path checks seed `APF_DIDDIR` alongside unrelated
  flags and verify that only `APF_DIDDIR` is cleared before `MatchNext`; empty
  scans retain a zeroed AnchorPath. Receipt
  `artifacts/binddrivers-morphos-native-entry-20260924-anchor-flags-v12/qualification.json`
  passes twenty-six scanner vectors plus the parser vector on 68000/020/040,
  with no leaks or shared-image writes. Unterminated tool-type responses,
  Workbench correspondence, real guest behavior, installed PURE/resident
  metadata, licensing and package admission remain open.
- Follow-up (2026-09-25): the native probe HUNK loader now accepts ordinary
  multi-HUNK CODE/DATA/BSS images, can clone a command image per invocation,
  and applies cross-segment `HUNK_RELOC32` relocations. A one-invocation probe
  of the original 38.2 Workbench binary still does not yield a valid DOS
  contract: the first fixture mapping used inconsistent DOS base/vector
  addresses; after aligning those synthetic addresses, the binary reached
  `SetCurrentBinding` with A0 and D0 both `$E000`, and returned 5. Those values
  expose a harness/layout mismatch, not a Workbench behavior result. The probe
  report is explicitly observational and grants no parity or shipping credit.
  The resident MorphOS qualification passes 26 scanner vectors plus its parser
  vector on all three CPUs with the generalized loader; receipt:
  `artifacts/binddrivers-morphos-native-entry-loader-regression-20260925/qualification.json`.
  The single-run exploratory trace is
  `artifacts/wb31-binddrivers-reference-single-no-match.json`. Next: run the
  Workbench binary under an authentic Kickstart 3.1 DOS/Expansion vector layout
  or establish the original library bases and structures from direct machine
  evidence before interpreting its calls. The current workspace has no ROM or
  original bootable Workbench disk image (`CopperStart/rom` is empty); the
  `.adf`/`.hdf` files found are generated disk-builder and filesystem test
  fixtures. Recorded Workbench protection is zero; PURE is not assumed.
- Follow-up (2026-09-25): `BindDrivers` now has a separate Workbench 3.1
  resident body derived from the hash-bound offline disassembly, rather than
  reusing the MorphOS source-shaped implementation. It preserves the Workbench
  library-open order, `Lock`/`Examine`/`CurrentDir`/`ExNext` scan,
  `GetDiskObject`, strict decimal `PRODUCT` fields via DOS `StrToLong`, and
  current-HUNK Resident search. Its static receipt
  `artifacts/workbench31-binddrivers-native-static-20260925-v1/qualification.json`
  compiles 68000/020/040 HUNKs with 21 reachable methods and zero managed
  runtime features/helpers, external native targets, exception regions or
  fatal sites. It has no runtime-provider fixture yet. The MorphOS candidate's
  `CurrentBinding` writes were also corrected to the public structure order
  (`cb_ProductString` at offset 8, `cb_ToolTypes` at 12); the refreshed receipt
  `artifacts/binddrivers-morphos-native-entry-20260925-currentbinding-order-v1/qualification.json`
  passes 26 scanner invocations and one parser invocation per CPU, with no
  shared-image writes or fixture resource leaks. Neither receipt proves guest
  parity. Next: add Workbench provider-backed vectors, then run both reference
  programs and replacements on authentic OS guests. The absent ROM and complete
  Workbench media set still block that comparison and broader reference
  closure.
- Follow-up (2026-09-25): a second review found that the Workbench body had the
  `CurrentBinding` product and tool-type pointers reversed. The assignments
  now match offsets 8 and 12 respectively; receipt
  `artifacts/workbench31-binddrivers-native-static-20260925-currentbinding-order-v2/qualification.json`
  recompiles all three CPU targets with 21 reachable methods and zero reported
  runtime helpers/features, external targets, exceptions, or fatal sites. The
  receipt supersedes v1 for the current body but remains static-only. Runtime
  provider vectors and authentic guest comparison are still open.
- Follow-up (2026-09-25): Workbench `BindDrivers` now has a repeatable native
  provider-fixture qualification at
  `artifacts/workbench31-binddrivers-native-entry-20260925-provider-v3/qualification.json`.
  It passes nine invocations per 68000/020/040 resident image: library-open
  failures and order, failed and valid expansion directory checks, empty scan,
  matching `.INFO` scan, strict signed-field rejection, and successful
  PRODUCT/ConfigDev resolution through `SetCurrentBinding`, LoadSeg, Resident
  discovery, and `InitResident`. A Workbench launch also confirms the startup
  message is replied to after command cleanup. The runtime fixture exposed and fixed a
  case-folding defect in suffix matching. Reports show no leaks or shared-HUNK
  writes and the static reports have no managed helpers, external targets,
  exceptions, or fatal sites. This is supplied-provider evidence, not authentic
  Workbench parity; failing startup/provider boundaries, original binary
  comparison, PURE/lifecycle, licensing and packaging remain open.
- Follow-up (2026-09-25): Workbench `BindDrivers` now also passes supplied
  `LoadSeg` failure, loaded-HUNK-without-Resident unload, and rejected
  `InitResident` unload paths. The refreshed static/runtime receipt is
  `artifacts/workbench31-binddrivers-native-entry-20260925-provider-v4/qualification.json`;
  all three resident CPU images pass twelve vectors each with no fixture leaks
  or shared-image writes. The MorphOS 26 scanner plus PRODUCT parser vectors
  still pass on all three CPUs. These remain supplied-provider checks; original
  guest parity, missing media/installed metadata, PURE/lifecycle, licensing,
  differential, and package gates continue to block shipping qualification.
- Regression (2026-09-25): the shared fixture changes leave MorphOS
  `BindDrivers` green: 26 scanner invocations plus its parser vector pass on
  each CPU at
  `artifacts/binddrivers-morphos-native-entry-20260925-currentbinding-order-regression-v2/qualification.json`.
  This preserves the `CurrentBinding` field-order assertion after adding the
  Workbench-specific profile.
- Follow-up (2026-09-25): the MorphOS `LoadMonDrvs` candidate now checks
  segment-list addresses and declared HUNK extents with public Exec
  `TypeOfMem`, bounds each Resident match/tag read to its HUNK, follows valid
  multi-HUNK lists and rejects undersized, overflowing, unmapped and cyclic
  segment chains. The refreshed three-CPU receipt
  `artifacts/loadmondrvs-morphos-native-entry-20260925-hunk-safety-v4/qualification.json`
  passes fourteen supplied invocations per CPU, including resident discovery
  in a later HUNK and every malformed-chain case, with 19 reachable methods,
  zero managed runtime features/helpers, external native targets, exception
  regions, fatal sites, leaks or shared-image writes. This does not establish
  packed-command correspondence, real monitor loading, guest behavior, PURE,
  licensing or package admission.
- Follow-up (2026-09-25): MorphOS `SetKeyboard` now bounds resident scanning
  with Exec `TypeOfMem`, checked HUNK arithmetic, six-byte Resident-tag bounds,
  later-HUNK traversal and constant-space cycle detection. Its three-CPU
  receipt `artifacts/setkeyboard-morphos-native-entry-20260925-hunk-safety-v2/qualification.json`
  passes sixteen supplied invocations per CPU, including undersized,
  overflowing, unmapped and cyclic segment fixtures, with zero shared-image
  writes or leaks and no managed runtime features/helpers, external targets,
  exception regions or fatal sites. The separate Workbench `SetKeyboard`
  entry remains green on ten vectors per CPU at
  `artifacts/setkeyboard-wb31-native-entry-regression-20260925-morphos-hunk-safety/qualification.json`.
  These replacement-side checks do not close packed MorphOS syntax, guest
  parity, PURE/resident, licensing or package gates.
- **Dependencies:** applicable expansion/resource/ROM, datatypes, preferences,
  graphics, keymap, diskfont and notification owners.
- Preserve startup ordering, resident/singleton lifetimes, options, environment/
  preference persistence, resource selection, and repeated-start behavior.
  IPrefs is a real continuing service where the reference is one.
- Scope SetPatch and installer-era behavior to the original ROM/machine contract;
  never patch unknown ROM addresses or fake a successful patch on CopperStart.
  Distinguish already-satisfied behavior from a missing required facility.
- **Exit:** cold/warm startup and failure/restart cases pass on supported guest
  configurations; live services own resources and have tested termination paths.

### CC21 - Clock, power, and boot media

- [ ] Commands: `SetClock`, `Install`, `Format`, `Reboot`, `ShutDown`, `MagTape`.
- Progress slice: MorphOS `Reboot` 50.3 now has a source-bound resident native
  entry preserving the empty `ReadArgs` call, Ctrl-C `ERROR_BREAK` handling,
  `ColdReboot`, DOS-open failure, and the source's returning value `666`. Its
  three-CPU HUNK receipt passes thirty supplied DOS/Exec vectors, including
  ignored tails, parser/signal/open failures, Workbench and boundary rejection,
  and interleaved repeat calls. Keep CC21 open until a real reboot transition,
  PURE/resident reuse, packaging, and differential evidence pass.
- Progress slice: Workbench 3.1 `Reboot` now has a separate DOS 36 resident
  syntax candidate using the Disk 1/2 empty-template boundary. Its three-CPU
  receipt passes ten supplied DOS/Exec vectors per CPU, including Ctrl-C,
  parser/open failures, startup rejection and repeat use:
  `artifacts/reboot-wb31-native-20260917-qualified-v1/qualification.json`.
  Classic reboot parity, PURE/resident lifecycle, packaging and differential
  evidence remain open.
- Progress slice: MorphOS `SetClock` 50.4 now has a source-bound contract and
  bounded resident native body preserving `LOAD/S,SAVE/RESET/S`, shared DOS
  `ReadArgs` ownership, `battclock.resource`, VBlank `timer.device`, classic
  and MorphOS 52+ UTC battery-clock vectors, timer `GetSysTime`/
  `GetUTCSysTime`, and reverse cleanup. Three CPU HUNKs pass static
  compatibility checks with no managed allocation sites, runtime
  helpers/features, external targets, exception regions or fatal machine-fault
  sites; the refreshed supplied-vector fixture passes seventeen invocations per
  CPU with balanced ownership and unchanged images. Keep CC21 open until the
  real battclock/timer guest fixture, Workbench parity, exact help/diagnostics,
  PURE/resident reuse, package admission and differential evidence pass.
- Progress slice: Workbench 3.1 `SetClock` now has a separate DOS 36 resident
  classic-vector syntax candidate for the captured `LOAD/SAVE/RESET` grammar.
  Its three-CPU receipt
  `artifacts/setclock-wb31-native-20260920-candidate/qualification.json`
  passes sixteen supplied DOS/Exec/resource/timer vectors per CPU, including
  parser/resource/allocation/device failures, timer-I/O diagnostics, Workbench
  startup and missing-DOS boundaries, repeat ownership and two interleaved callers.
  Original clock behavior, exact help
  and diagnostics, PURE/resident classification, package admission, licensing
  and guest differential evidence remain open.
- **Dependencies:** timer/battery-clock, filesystem/device, bootblock and power
  providers. Check each profile's actual distribution placement.
- Preserve clock read/write/reset, media/bootblock options and signatures,
  formatting/requester behavior, tape command grammar, and shutdown/reboot modes.
  Original confirmation rules are part of compatibility, not extra prompts to
  invent during implementation.
- Validate device/unit/volume identity, size and capability before mutation;
  cover busy media, write protection, partial I/O and device failure. Reboot and
  shutdown must complete through the guest machine lifecycle.
- **Exit:** all destructive success paths run only on disposable guest images or
  simulated devices, with exact data/boot effects and failure behavior recorded.

### CC22 - Workbench installation helpers

- [ ] Commands: `Check2090`, `ExtractKickstart`, `FindResident`, `GuessBootDev`,
  `IconPos`, `Prod_Prep`, `UpdateWBFiles`.
- Progress slice (2026-09-21): Workbench 3.1 `FindResident` now uses the
  lowest verified DOS 36 capability floor for its separate resident syntax
  candidate, while retaining the captured DOS 37 request as differential
  evidence. Its three-CPU receipt passes eleven supplied DOS 36/Exec vectors
  per CPU, including found and missing residents, parser and result-slot
  failures, Workbench-startup and missing-DOS guards, and interleaved callers.
  The receipt is
  `artifacts/findresident-wb31-native-20260921-v4/qualification.json`; exact
  original help and diagnostics, guest parity, PURE/resident lifecycle,
  packaging and differential evidence remain open.
- Contract slice (2026-09-21): the remaining Workbench installation helpers
  `Prod_Prep` and `UpdateWBFiles` now have source-independent media contracts
  at `docs/Commands/Workbench31MorphOS320/contracts/Prod_Prep.md` and
  `contracts/UpdateWBFiles.md`. `Prod_Prep` is recorded as a custom interactive
  RDB/partition and low-level device utility with destructive format/verify
  paths; `UpdateWBFiles` is recorded as the `NEWWB:` preference/icon IFF update
  utility with DOS, iffparse, icon and graphics dependencies. Their exact
  launch grammar, guest providers, native bodies, PURE/resident lifecycle,
  package and differential gates remain open; no template was inferred from
  usage strings.
- Progress slice (2026-09-20): Workbench 3.1 `Check2090` and `IconPos` now
  have separate resident candidates. `Check2090` passes ten supplied
  Exec/Expansion/DOS vectors per CPU in
  `artifacts/check2090-wb31-native-20260920-v2/qualification.json`.
  `IconPos` uses the captured twelve-slot grammar and passes fifteen supplied
  Exec/DOS/icon.library vectors per CPU in
  `artifacts/iconpos-wb31-native-20260920-v2/qualification.json`, covering
  DiskObject mutation, default creation, image copying, parser/type and
  storage failures, startup boundaries, missing DOS and interleaved callers.
  Both remain bounded evidence only: exact original behavior, guest parity,
  PURE/resident lifecycle, licensing, packaging and differential gates remain
  open.
- Progress slice (2026-09-20): Workbench 3.1 `GuessBootDev` now has a
  separate DOS 36 resident candidate using the captured `BOOTDISKNAME`
  template. Its three-CPU receipt passes sixteen supplied
  Exec/DOS/utility.library/expansion.library vectors per CPU, covering signed
  boot priorities, disabled and unusable filesystems, lock fallback, parser
  and library-open failures, startup boundaries, missing DOS and interleaved
  callers. Receipt:
  `artifacts/guessbootdev-wb31-native-20260920-qual/qualification.json`;
  exact original boot-node semantics, guest parity, PURE/resident lifecycle,
  licensing, packaging and differential gates remain open.
- Progress slice (2026-09-21): Workbench 3.1 `ExtractKickstart` now has a
  source-derived contract and separate resident native candidate at
  `docs/Commands/Workbench31MorphOS320/contracts/ExtractKickstart.md`.
  The candidate preserves the captured `DEVICE/A,TO/A,1.3/S` boundary,
  `DF0:`-style validation, raw `trackdisk.device` access, `KICKSUP0` boot-block
  check, source layout reads and invocation-owned I/O leases. Three-CPU HUNK
  compilation passes with no managed allocation/runtime features, and the
  bounded native trackdisk/DOS fixture passes seventeen vectors per CPU for both
  media layouts, parser/library failures, diagnostics, cleanup and
  interleaving. Exact image layout, guest differential, PURE/resident,
  packaging and licensing gates remain open. Receipt:
  `artifacts/extractkickstart-wb31-native-20260921-v5/qualification.json`.
- **Dependencies:** original installer contracts and the relevant device, ROM,
  resident-list, boot-device, icon and preference-conversion APIs.
- Derive full behavior from matching binaries, scripts and original references;
  their uncommon names do not justify dropping them or guessing templates.
- Test discovery ambiguity, supported/unsupported hardware, malformed data,
  repeated execution and interrupted updates. Preserve resident use of
  ExtractKickstart, FindResident and IconPos in original installer flows.
- **Exit:** installer scenarios pass with redistributable synthetic ROM/data
  fixtures and local licensed references. Package in the correct installation
  profile; do not redistribute extracted Kickstart contents.

### CC23 - Desktop launch and URL handling

- [ ] Commands: `LoadWB`, `IconX`, `WBRun`, `Open`, `OpenURL`, `Beep`, `SendBeacon`.
- Progress slice: MorphOS `Beep` 50.2 now has a source-bound resident native
  entry. Its three-CPU HUNK receipt passes fifteen supplied Exec/Intuition
  vectors covering v33 open, `DisplayBeep(NULL)`, close, ignored command-tail
  bytes, Workbench startup, repeat use, and open failure. Keep the family open until desktop
  guest behavior, PURE reuse, lifecycle, differential and package gates pass.
- **Dependencies:** existing Shell, Workbench/Ambient-equivalent launch owner,
  icon, Intuition, URL and MagicBeacon notification services.
- Preserve CLI versus Workbench startup, tooltypes, argument locks, command-tail
  quoting, current directories, streams, asynchronous/synchronous completion,
  object/type dispatch, and the documented launch/requester/beep behavior.
- Use 3.1 APIs where available; newer open-object/URL functionality needs its
  proper provider. LoadWB must keep the appropriate original lifecycle/purity
  contract per profile rather than inheriting a blanket classification.
- SendBeacon uses desktop notifications, not DOS filesystem notifications.
  Preserve response selection, signal/cleanup rules and nonstandard result values
  verified against the [MagicBeacon SDK](https://morphos-team.net/sdk/magicbeacon.html).
- **Exit:** real desktop launches, duplicate starts, missing desktop, rejected
  launches and cancellation behave correctly without leaked WBStartup messages.

### CC24 - Requesters and clipboard

- [ ] Commands: `RequestChoice`, `RequestFile`, `RequestString`, `Clip`, `ConClip`.
- Progress slice: Workbench 3.1 `RequestChoice` now has a separate DOS36/
  Intuition candidate from the captured four-slot `TITLE/A,BODY/A,GADGETS/M,
  PUBSCREEN/K` boundary. Its three-CPU receipt
  `artifacts/requestchoice-wb31-native-20260920-candidate/qualification.json`
  passes eleven supplied vectors per CPU, including parser/requester failure,
  startup and two interleaved callers. Exact original UI/diagnostics, timeout
  and type policy, PURE/resident lifecycle, packaging and differential gates
  remain open.
- Progress slice: MorphOS `RequestFile` now has a resident DOS/ASL candidate
  with the source-derived fourteen-slot boundary (including `INITIALVOLUMES`),
  invocation-owned requester/tag storage, single and multi-select output,
  cancellation and allocation/parser/startup failure handling. The three-CPU
  receipt `artifacts/requestfile-morphos-native-20260920-interleaved/qualification.json`
  passes twelve supplied option/tag, startup, ownership and interleaving vectors per CPU. Exact original
  requester behavior, Workbench correspondence, PURE/resident lifecycle,
  minimum stack, packaging, licensing and differential gates remain open.
- Progress slice: Workbench 3.1 `RequestFile` now has a separate DOS 36/ASL 36
  resident syntax candidate using the captured thirteen-slot boundary and
  omitting MorphOS-only `INITIALVOLUMES/S`. The three-CPU receipt
  `artifacts/requestfile-wb31-native-20260920-candidate/qualification.json`
  passes twelve supplied option/tag, startup, ownership and interleaving
  vectors per CPU. Original requester parity, diagnostics, PURE/resident
  classification, minimum stack, packaging, licensing and differential gates
  remain open.
- **Dependencies:** ASL/Intuition/MUI as actually required, clipboard/console owner.
- Preserve titles/body text, buttons, selected result values, defaults, masks,
  multi-selection, path/string formatting, clipboard units and file modes.
  ConClip's persistent integration is not equivalent to a one-shot Clip command.
- Cover cancel versus empty string, escaped content, redirected output, no
  screen/console, library failure, clipboard encoding/IFF rules and unit changes.
- **Exit:** requester and clipboard success/cancel/error paths work through real
  guest services, and every requester/message/buffer has the correct owner.

### CC25 - Editors, installer, and spelling service

- [ ] Commands: `Ed`, `Edit`, `Installer`, `HunspellService`.
- **Dependencies:** console/editor facilities, installer language/GUI, and
  spellchecker.library-compatible IPC/service support.
- Ed and Edit need their respective editing languages, movement/edit/search,
  file/save, scripting and error behavior; merely opening a buffer is incomplete.
  Installer needs its supported script language and interaction modes.
- Discover HunspellService's actual service entry, dictionary handling, protocol,
  startup/shutdown and failure behavior. Review engine and dictionary licenses;
  do not invent a command template or substitute host spellchecking.
- **Exit:** finite script/interaction corpora exercise every supported mode,
  including invalid scripts, file/save failures, service races and cleanup.

### CC26 - ARexx commands and host

- [ ] Commands: `RexxMast`, `RX`, `RXC`, `RXCmd`, `RXLIB`, `RXSET`, `HI`,
  `TCC`, `TCO`, `TE`, `TS`.
- **Dependencies:** real compatible interpreter, rexxsyslib, ports and messaging.
- Preserve host startup/lifetime, script/command submission, argument/result
  passing, variables, library registration and each utility's control behavior.
  Decode exact command grammar rather than expanding names into guessed actions.
- HI sends interpreter halt requests; TCC/TCO manage its trace console and TE/TS
  manage global interactive tracing. Preserve handling of active scripts and
  pending reads. Validate the [ARexx utility contracts](https://wiki.amigaos.net/wiki/AmigaOS_Manual:_ARexx_Command_Utilities)
  against the selected release and [Rexx SDK messages](https://morphos-team.net/sdk/includes/rexx/storage.html).
- Verify interpreter/host absence, duplicate starts, message ownership, results,
  asynchronous replies, cancellation, nested invocations and shutdown during use.
- **Exit:** commands drive the guest ARexx owner and execute meaningful scripts;
  a message-accepting stub or a dependency-missing error is not full implementation.

### CC27 - Lua and MUI automation

- [ ] Commands: `LuaX`, `Automator`.
- **Dependencies:** the versioned Lua execution/extension contract and the current
  MUI replacement's application-inspection/automation interfaces.
- Implement LuaX's actual invocation, script/module/environment and result
  semantics. Automator must inspect and act on real application objects using
  stable public facilities; coordinate required MUI work with its current owner.
- Test missing objects, changing application state, callback lifetime, script
  errors, large strings, invalid methods, cancellation and client/server teardown.
- **Exit:** real interpreter and MUI interaction scenarios pass; do not count a
  parser, fake object tree, or unrelated host GUI automation as compatibility.

### CC28 - Network setup and identity

- [ ] Commands: `NetConfig`, `NetworksHelper`, `IfConfig`, `ShowInterface`,
  `Online`, `Offline`, `Login`, `Passwd`, `WhoAmI`, `ID`.
- **Dependencies:** guest network stack/interface services and actual identity
  and credential stores required by the reference.
- Preserve config sources, persistent versus temporary changes, interface and
  connection transitions, status output and authentication semantics. Do not map
  guest accounts or interfaces to host accounts or networking shortcuts. ID
  reports user/group identity using its own Unix-style options, not a DOS template.
- Test absent stack/interface, refused configuration, invalid credentials,
  cancelled password entry, persistence failures, and repeated service startup.
  Keep secrets out of logs, argument captures and committed fixtures.
- **Exit:** supported success paths alter/query real guest state, with race and
  ownership coverage and reference-compatible diagnostics.

### CC29 - Network diagnostics, name service, and HTTP

- [ ] Commands: `ARP`, `AskHost`, `Ping`, `Route`, `TraceRoute`, `SetClockNTP`,
  `WakeOnLAN`, `OFDNS`, `OFHTTP`.
- **Dependencies:** guest sockets/protocol stack, resolver, clock and TLS provider.
- Preserve each tool's actual DOS or Unix grammar, packet/address/query options,
  timeouts, output formats, exit values and transfer semantics. Use existing
  protocol/TLS implementations with audited licensing rather than ad hoc crypto.
- Test controlled local endpoints, unreachable hosts, cancellation, short reads,
  redirects, malformed replies, certificate failures and clock-update failures.
  Network tests must not send packets to arbitrary public targets.
- **Exit:** actual guest networking and encrypted-transfer success/error paths
  meet the reference contract; host HTTP/DNS calls are not production substitutes.

### CC30 - Remote filesystems and capture

- [ ] Commands: `SmbFS`, `Smb2FS`, `Ssh2FS`, `tcpdump`.
- **Dependencies:** protocol/authentication libraries, DOS handlers and packet
  capture provider. Split large missing handlers into explicit prerequisite slices.
- Preserve mount/configuration, credentials, path mapping, permissions,
  reconnection, unmount/lifetime and packet-filter/capture formats as applicable.
- Use isolated disposable servers and synthetic traffic. Exercise disconnected
  reads/writes, partial operations, busy unmount, malformed network data,
  credential failure and capture cancellation without exposing user traffic.
- **Exit:** real handler I/O and capture/filtering work through the guest system;
  a local mirror directory or empty capture file does not satisfy the contract.

### CC31 - Compression and archives

- [ ] Commands: `Bz2`, `LhA`, `LZMADec`, `LZMAInfo`, `XZ`, `XZDec`, `UnRAR`,
  `OFArc`, `OFHash`.
- **Dependencies:** versioned codecs/archive/hash services with license approval.
- Preserve all original switches, input/output modes, archive formats/versions,
  integrity/list/extract/create behavior, metadata, stdout streaming and exit codes.
  Do not impose ReadArgs syntax on a tool that uses another grammar.
- Test known-answer hashes, reference-generated archives, round trips, malformed
  headers, truncation, unsupported features, output errors and bounded-memory
  operation. Resolve archive path traversal/overwrite hazards explicitly without
  hiding compatibility or safety decisions.
- **Exit:** format/mode matrices and negative corpora pass in native code with no
  host codec process or managed library in the production dependency closure.

### CC32 - XAD archive utilities

- [ ] Commands: `XAD2LhA`, `XADLibInfo`, `XADList`, `XADUnDisk`, `XADUnFile`,
  `XADUnTar`, `Exe2Arc`.
- **Dependencies:** XAD library/client APIs, required archive clients, and DOS/
  device integration; share codecs with CC31 where their contracts match.
- Preserve client selection, format listing, extraction/conversion, password,
  destination, overwrite, disk image and progress/cancel behavior. Exe2Arc recovers
  archive payloads from self-extracting executables; never run the input executable.
- Verify version/client absence, corrupt archives, partial extraction, metadata
  failure and password cancellation. Disk extraction uses disposable block
  devices; reject any test configuration that resolves to a host disk.
- **Exit:** real XAD calls and every advertised format/mode work with a qualified
  client set; simply recognizing an archive signature is not completion.

### CC33 - USB stack and device tools

- [ ] Commands: `AddUSBClasses`, `AddUSBHardware`, `PsdStackloaderToMOSPrefs`,
  `USBDevLister`, `USBErrorLog`, `DRadioTool`, `PenCamTool`, `SonixCamTool`, `RocketTool`.
- **Dependencies:** Poseidon-compatible stack/configuration plus applicable
  device-class protocols and drivers.
- Preserve add/load/list/log/config-conversion and per-device operation modes.
  Reuse the actual stack owner; device commands must submit their real protocols.
- Test hotplug, missing and multiple devices, unsupported revisions, short
  transfers, failed configuration writes, pending requests at disconnect, and
  operation cancellation with emulated or explicitly designated test devices.
- **Exit:** required device operations work through the guest stack; device-absent
  handling alone is recorded as partial coverage, not complete functionality.

### CC34 - Audio, MIDI, and mixer

- [ ] Commands: `AddAudioModes`, `Play`, `PlayMidi`, `SetMixer`.
- **Dependencies:** audio mode registry, AHI/multimedia/MIDI and mixer providers
  actually used by each reference command.
- Preserve format selection, device/mode/unit, playback/volume/timing, looping,
  control and mode-list operations in the frozen contracts.
- Exercise deterministic short audio/MIDI fixtures, unsupported formats, mode
  removal, device contention, underruns, interruption and asynchronous completion.
  Device buffers and callbacks must outlive every pending request correctly.
- **Exit:** guest audio/MIDI/control operations and ownership are verified; host
  playback or returning before unowned device I/O completes does not qualify.

### CC35 - Hardware inspection and power management

- [ ] Commands: `Battery`, `PowManTool`, `PowerMac7FanControl`, `PCIScan`, `PCIWrite`,
  `ShowCGXConfig`, `IDEStandby`, `Eject`, `HFSSetMacBoot`.
- **Dependencies:** guest PCI, graphics, battery/power/fan, storage and HFS boot
  metadata providers. Match hardware-specific interfaces to their real owners.
- Preserve all query/control modes, output and platform checks. Machine-specific
  commands need the supported guest machine/device model; never manipulate host
  fans, PCI configuration, power policy or mounted storage.
- Test unsupported devices and values, privilege/capability failures, hot removal,
  partial writes, temporary-state restoration and appropriate failure rollback.
  Preserve requested persistent changes. Unsafe real hardware is not a test fixture.
- **Exit:** supported success paths and rejected operations pass against correct
  guest providers, with capability-dependent coverage reported explicitly.

### CC36 - Raw media and diagnostic tests

- [ ] Commands: `HDMBRClear`, `HDRead`, `HDWrite`, `HDTest`, `FSTest`, `MemTest`.
- **Dependencies:** safe guest block-device, memory-allocation and filesystem
  test facilities; CC31 compressed-input support where required.
- Preserve offsets, sector sizes, counts, skip/asynchronous/checksum modes,
  verification patterns and diagnostic results. Explicitly close HDWrite's 3.20
  template delta and command placement on installation media.
- Prove arithmetic bounds and exact target selection before reads/writes;
  simulate media defects, partial transfers, device disappearance, cancellation,
  low memory and attempts to touch memory outside an owned test region.
- **Exit:** destructive behavior is verified entirely within disposable guest
  media/owned buffers; tests cannot resolve a physical host device or host memory.

### CC37 - PFS filesystem utilities

- [ ] Commands: `PFSDiskValid`, `PFSFormat`, `PFSList`, `PFSMakeRollover`,
  `PFSSetDeldir`, `PFSSetFileNameSize`, `PFSSetRollover`.
- **Dependencies:** a version-compatible PFS handler and maintenance interfaces;
  license and media-format evidence must be recorded before implementation reuse.
- Preserve validation/repair, formatting, deleted-directory, filename-size and
  rollover-file semantics through the owning filesystem component.
- Test valid/damaged disposable volumes, incompatible versions, busy objects,
  boundary sizes, interrupted maintenance and errors after partial changes.
  Do not implement a second filesystem inside every command front end.
- **Exit:** real PFS state/content changes match the contract, with repeatable
  validation/repair fixtures and no host-mounted media operations.

### CC38 - Debugging, tracing, and translation services

- [ ] Commands: `Debug`, `ClearRAMDebugLog`, `GetRAMDebugLog`, `SegTracker`, `Trance`.
- **Dependencies:** each command's actual debugger, trace, debug-log, segment-tracker
  or translation owner as established by CC01/CC07.
- Preserve target selection, enable/disable/query, output/logging, registration,
  and shutdown rules. Similar short names do not imply a shared kernel protocol.
- Trance activates the 68k JIT service and needs that provider's real lifecycle;
  it is not a synonym for Debug. Kernel/translator controls must act on the guest
  facility, not host debugging flags or unconditional success on a 68k-only setup.
- **Exit:** meaningful success and failure traces cover each actual service,
  including target disappearance, concurrent readers, teardown and absent-provider
  behavior. Required missing kernel/translation features remain open dependencies.

### CC39 - Reconcile Freeze and late inventory discoveries

- [ ] Resolve the historical/documentation-only `Freeze` entry against the frozen
  3.20 distribution, installation transformations, SDK material and licensed
  reference behavior. Record the evidence and release scope explicitly.
- [ ] If supplied/generated by the selected baseline, implement its exact
  suspension/resumption/control contract through the process owner and apply the
  standard command gates. If historical-only, record that evidence as a scope
  exclusion; do not count it as an implemented 3.20 command.
- [ ] Add every newly found in-scope Workbench variant or installation command to
  a numbered owner slice, inventory and manifest. Re-run the set comparison.
- **Exit:** no undocumented omissions and no unexplained difference between the
  reference file sets, documentation candidates and implementation ledger. If
  Freeze's status remains uncertain, keep that discrepancy open through CC44.

## Final qualification stages

### CC40 - Close option and behavior coverage

- [ ] Reconcile every inventory command/profile/option/mode with its completed
  implementation, dependency and tests. Require zero placeholder/no-op modes,
  swallowed failures or omitted options.
- [ ] Run integration scripts spanning files, dates, variables, result codes,
  redirection, pipelines where supported by the existing Shell, resident commands,
  child/background execution, startup and installer workflows.
- [ ] Exercise aliases, explicit `C:` paths, current-directory/path collisions,
  Help/ReadArgs continuation and quoted command/script tails without Shell regressions.
- **Exit:** complete semantic coverage of the frozen scope. Any unavailable
  success path, required reference capture or provider remains an open item.

### CC41 - Close pure/resident and lifecycle qualification

- [ ] For every originally pure/resident command, run the full static and native
  shared-image/repeated/concurrent test matrix against the exact release artifacts.
- [ ] Verify original temporary startup registrations/removals, resident use
  counts, deferred loads where applicable, active replacement/removal refusal,
  task death, malformed input and injected failures.
- [ ] Verify each intentionally non-pure, singleton or persistent service's
  lifecycle separately; audit every retained allocation/callback/port by owner.
- [ ] Compare installed protection metadata with the per-profile reference policy;
  fail stale hashes or any attempted downgrade of an original purity requirement.
- **Exit:** all required reentrancy/re-execution and lifecycle contracts pass.
  Preserving an original P-clear file is not permission to break its resident use.

### CC42 - Qualify native artifacts and runtime images

- [ ] Build and execute the required MC68000/020/040 HUNK matrix. Check forbidden
  runtime features, imports, relocations, shared writable data, stack use, library
  versions, command IDs and dependency closure.
- [ ] Produce selected normal and installation-media profiles. Inspect the final
  guest images for every expected path, hash, version, protection bit and script
  flag; verify no internal-only Shell command was accidentally duplicated.
- [ ] Check version/build provenance without falsely presenting replacements as
  original Commodore/MorphOS binaries. Preserve every copyright, attribution and
  license notice required by any legitimately reused source.
- **Exit:** reproducible complete images and independent loadable executables,
  not just C# test results, compiler compatibility reports or assembler listings.

### CC43 - Boot and differential acceptance

- [ ] Boot a licensed original Kickstart 3.1 test configuration with the classic
  commands, and the intended CopperStart/CopperOS configuration with the selected
  command profile. Exercise cold/warm startup, interactive/script launches,
  installation helpers, missing resources and low memory.
- [ ] Finish Workbench and licensed MorphOS 3.20 differential comparisons with
  approved nondeterminism handling. Hardware/provider tests use supported guest
  fixtures or designated reference devices; report their actual coverage.
- [ ] Repeat the existing Shell regression suite and affected SDK/compiler/DOS/
  service suites. Investigate regressions rather than normalizing them away.
- **Exit:** real boot and functional compatibility evidence. Emulated semantics,
  SDK conformance and original-binary differential results remain distinct claims.

### CC44 - Final inventory and delivery audit

- [ ] Verify set equality between required reference commands, implementation
  ledger, qualified artifacts and appropriate distribution manifests; every
  exclusion and alias has provenance. Count canonical names and profile variants
  separately, and record any increase from the initial 200-command seed.
- [ ] Require complete option coverage, original pure/resident preservation,
  successful required providers, all native/boot gates and resolved reference
  discrepancies. `NeedsReference`/`DependencyOpen`/`Partial` are not completion.
- [ ] Deliver the final inventories, build/purity manifests, qualification report,
  progress evidence, license notices and reproducible image/build instructions.
- **Exit:** the full objective is achieved. Do not close the goal because the
  core subset works, a work session ends, or only documentation remains missing.

## Verification commands and safety

At execution time, start with the existing focused Shell/command tests:

```powershell
dotnet test tests/Commands/CopperOS.Commands.Tests.csproj
```

The existing native Shell compile check is:

```powershell
./tools/Shell/qualify_native.ps1 -IncludeFullExecute
```

These commands do not qualify all new external commands. During CC03/CC08/CC09,
add command-specific native/build/qualification scripts under `tools/Commands/`
and record their actual interfaces in the progress log. Do not claim those tools
already exist or prescribe an unimplemented CLI as if it were runnable today.
Run only relevant tests after a slice; run the full affected matrix at final gates.

Reference probes and destructive tests must resolve to a disposable guest/image
allowlist before execution. Keep original media read-only. Avoid executing unknown
default/help behavior, changing the host clock/network, using host credentials,
or allowing guest disk tools to reach host physical devices. Do not request user
approval for routine reversible coding, local tests or documentation; obtain any
needed permission only for a concrete external/destructive action outside this
safe test boundary, after all independent preparation is complete.

## Appendix A - Complete initial command ownership table

This table is the seed for CC00, not a substitute for full-media closure.
`W` = inspected Workbench3.1 disk C; `I` = inspected Install3.1 disk C;
`M` = official MorphOS 3.20 ISO MorphOS/C. A dash means absent from those
inspected C directories, not proof of absence from every possible disk variant.
Source spelling and original placement belong in the versioned inventory.

Every one of these 200 canonical names has exactly one implementation owner.
Existing Shell-only commands are listed separately above and are not duplicated
here. Freeze's additional documentation discrepancy belongs to CC39.

| Command | Workbench media | MorphOS ISO | Owner stage |
| --- | --- | --- | --- |
| `AddAudioModes` | — | M | CC34 |
| `AddBuffers` | W, I | M | CC12 |
| `AddDataTypes` | W | M | CC20 |
| `AddUSBClasses` | — | M | CC33 |
| `AddUSBHardware` | — | M | CC33 |
| `ARP` | — | M | CC29 |
| `AskHost` | — | M | CC29 |
| `Assign` | W, I | M | CC12 |
| `Automator` | — | M | CC27 |
| `Avail` | W | M | CC17 |
| `Battery` | — | M | CC35 |
| `Beep` | — | M | CC23 |
| `BindDrivers` | W, I | M | CC20 |
| `Break` | W | M | CC18 |
| `Bz2` | — | M | CC31 |
| `ChangeTaskPri` | W | M | CC18 |
| `Check2090` | I | — | CC22 |
| `ClearRAMDebugLog` | — | M | CC38 |
| `CLI` | — | M | CC10 |
| `Clip` | — | M | CC24 |
| `Clone` | — | M | CC13 |
| `ConClip` | W | M | CC24 |
| `ConvertText` | — | M | CC15 |
| `Copy` | W, I | M | CC13 |
| `CPU` | W | M | CC17 |
| `Date` | W | M | CC16 |
| `Debug` | — | M | CC38 |
| `Delete` | W, I | M | CC13 |
| `DevList` | — | M | CC18 |
| `Dir` | W | M | CC11 |
| `DiskCache` | — | M | CC19 |
| `DiskChange` | W | M | CC19 |
| `DiskFree` | — | M | CC11 |
| `DOSList` | — | M | CC11 |
| `DRadioTool` | — | M | CC33 |
| `Ed` | W | M | CC25 |
| `Edit` | W | — | CC25 |
| `Eject` | — | M | CC35 |
| `Eval` | W | M | CC10 |
| `Exe2Arc` | — | M | CC32 |
| `Execute` | W, I | M | CC10 |
| `ExtractKickstart` | I | — | CC22 |
| `FileNote` | W | M | CC14 |
| `FileWrite` | — | M | CC14 |
| `FindResident` | I | — | CC22 |
| `FlushLib` | — | M | CC18 |
| `Format` | — | M | CC21 |
| `FSDie` | — | M | CC19 |
| `FSList` | — | M | CC19 |
| `FSPrefs` | — | M | CC19 |
| `FSTest` | — | M | CC36 |
| `GetRAMDebugLog` | — | M | CC38 |
| `GuessBootDev` | I | — | CC22 |
| `HDMBRClear` | — | M | CC36 |
| `HDRead` | — | M | CC36 |
| `HDTest` | — | M | CC36 |
| `HDWrite` | — | M | CC36 |
| `HFSSetMacBoot` | — | M | CC35 |
| `HI` | — | M | CC26 |
| `HunspellService` | — | M | CC25 |
| `IconPos` | I | — | CC22 |
| `IconX` | W | M | CC23 |
| `ID` | — | M | CC28 |
| `IDEStandby` | — | M | CC35 |
| `IfConfig` | — | M | CC28 |
| `iKill` | — | M | CC18 |
| `Info` | W | M | CC11 |
| `Install` | W | M | CC21 |
| `Installer` | — | M | CC25 |
| `IPrefs` | W | M | CC20 |
| `Join` | W | M | CC13 |
| `LhA` | — | M | CC31 |
| `LibList` | — | M | CC18 |
| `List` | W, I | M | CC11 |
| `LoadLib` | — | M | CC18 |
| `LoadMonDrvs` | — | M | CC20 |
| `LoadResource` | W | — | CC20 |
| `LoadWB` | W, I | M | CC23 |
| `Lock` | W | M | CC19 |
| `Login` | — | M | CC28 |
| `LuaX` | — | M | CC27 |
| `LZMADec` | — | M | CC31 |
| `LZMAInfo` | — | M | CC31 |
| `MagTape` | W | — | CC21 |
| `MakeDir` | W, I | M | CC12 |
| `MakeLink` | W | M | CC12 |
| `MemTest` | — | M | CC36 |
| `MirrorCheck` | — | M | CC15 |
| `MirrorCopy` | — | M | CC15 |
| `ModList` | — | M | CC18 |
| `Mount` | W | M | CC19 |
| `NetConfig` | — | M | CC28 |
| `NetworksHelper` | — | M | CC28 |
| `Newer` | — | M | CC15 |
| `OFArc` | — | M | CC31 |
| `OFDNS` | — | M | CC29 |
| `Offline` | — | M | CC28 |
| `OFHash` | — | M | CC31 |
| `OFHTTP` | — | M | CC29 |
| `Online` | — | M | CC28 |
| `Open` | — | M | CC23 |
| `OpenURL` | — | M | CC23 |
| `Passwd` | — | M | CC28 |
| `PathPart` | — | M | CC10 |
| `PCIScan` | — | M | CC35 |
| `PCIWrite` | — | M | CC35 |
| `PenCamTool` | — | M | CC33 |
| `PFSDiskValid` | — | M | CC37 |
| `PFSFormat` | — | M | CC37 |
| `PFSList` | — | M | CC37 |
| `PFSMakeRollover` | — | M | CC37 |
| `PFSSetDeldir` | — | M | CC37 |
| `PFSSetFileNameSize` | — | M | CC37 |
| `PFSSetRollover` | — | M | CC37 |
| `Ping` | — | M | CC29 |
| `Play` | — | M | CC34 |
| `PlayMidi` | — | M | CC34 |
| `PortList` | — | M | CC18 |
| `PowerMac7FanControl` | — | M | CC35 |
| `PowManTool` | — | M | CC35 |
| `Prod_Prep` | I | — | CC22 |
| `Protect` | W | M | CC14 |
| `PsdStackloaderToMOSPrefs` | — | M | CC33 |
| `Quote` | — | M | CC10 |
| `Reboot` | I | M | CC21 |
| `Relabel` | W | M | CC12 |
| `RemRAD` | W | M | CC19 |
| `Rename` | W | M | CC12 |
| `Replace` | — | M | CC15 |
| `RequestChoice` | W | M | CC24 |
| `RequestFile` | W | M | CC24 |
| `RequestString` | — | M | CC24 |
| `ResList` | — | M | CC18 |
| `RexxMast` | — | M | CC26 |
| `RocketTool` | — | M | CC33 |
| `Route` | — | M | CC29 |
| `RunFS` | — | M | CC19 |
| `RX` | — | M | CC26 |
| `RXC` | — | M | CC26 |
| `RXCmd` | — | M | CC26 |
| `RXLIB` | — | M | CC26 |
| `RXSET` | — | M | CC26 |
| `Search` | W | M | CC11 |
| `SegTracker` | — | M | CC38 |
| `SendBeacon` | — | M | CC23 |
| `SetClock` | W | M | CC21 |
| `SetClockNTP` | — | M | CC29 |
| `SetDate` | W | M | CC14 |
| `SetFileSize` | — | M | CC14 |
| `SetFont` | W | — | CC20 |
| `SetKeyboard` | W | M | CC20 |
| `SetMixer` | — | M | CC34 |
| `SetPatch` | W, I | — | CC20 |
| `ShowCGXConfig` | — | M | CC35 |
| `ShowConfig` | — | M | CC17 |
| `ShowInterface` | — | M | CC28 |
| `ShutDown` | — | M | CC21 |
| `Smb2FS` | — | M | CC30 |
| `SmbFS` | — | M | CC30 |
| `SonixCamTool` | — | M | CC33 |
| `Sort` | W | M | CC15 |
| `Ssh2FS` | — | M | CC30 |
| `Stat` | — | M | CC17 |
| `Status` | W | M | CC17 |
| `TaskList` | — | M | CC17 |
| `TCC` | — | M | CC26 |
| `TCO` | — | M | CC26 |
| `tcpdump` | — | M | CC30 |
| `TE` | — | M | CC26 |
| `Time` | — | M | CC16 |
| `Touch` | — | M | CC14 |
| `TraceRoute` | — | M | CC29 |
| `Trance` | — | M | CC38 |
| `Trashcan` | — | M | CC13 |
| `TS` | — | M | CC26 |
| `Type` | W | M | CC11 |
| `UnMount` | — | M | CC19 |
| `UnRAR` | — | M | CC31 |
| `UpdateWBFiles` | I | — | CC22 |
| `Uptime` | — | M | CC16 |
| `USBDevLister` | — | M | CC33 |
| `USBErrorLog` | — | M | CC33 |
| `Version` | W, I | M | CC17 |
| `Wait` | W, I | M | CC16 |
| `WaitForLib` | — | M | CC16 |
| `WaitForNotification` | — | M | CC16 |
| `WaitForPort` | — | M | CC16 |
| `WaitX` | — | M | CC16 |
| `WakeOnLAN` | — | M | CC29 |
| `WBRun` | — | M | CC23 |
| `Which` | W | M | CC10 |
| `WhoAmI` | — | M | CC28 |
| `XAD2LhA` | — | M | CC32 |
| `XADLibInfo` | — | M | CC32 |
| `XADList` | — | M | CC32 |
| `XADUnDisk` | — | M | CC32 |
| `XADUnFile` | — | M | CC32 |
| `XADUnTar` | — | M | CC32 |
| `XZ` | — | M | CC31 |
| `XZDec` | — | M | CC31 |

Required starting-set checks: W = 50; I = 20; W union I = 58; M = 188;
case-insensitive combined union = 200. The 12 Workbench-only names are
`Check2090`, `Edit`, `ExtractKickstart`, `FindResident`, `GuessBootDev`,
`IconPos`, `LoadResource`, `MagTape`, `Prod_Prep`, `SetFont`, `SetPatch`,
and `UpdateWBFiles`. If CC00 discovers more required commands, extend this
table, command-family slices, fixtures and manifests together.

Some inspected Workbench executables are outside C: `System/DiskCopy`,
`System/FixFonts`, `System/NoFastMem`, `Utilities/More`, `Utilities/MultiView`,
`Utilities/Clock`, `HDTools/HDToolBox`, `HDBackup` and `BRU`. They are not
additional C entries just because Shell paths can find them. Other non-C
Workbench programs, including Format, CLI, RexxMast, Installer and several
Rexxc utilities, have independently verified MorphOS C counterparts already
covered above. Preserve these source-location differences in each profile.

## Appendix B - Original pure/resident evidence to preserve

### MorphOS installer adds P to these 88 commands

The following list comes from ISO `hdinstall.fixc`, not an assumption based
on command behavior. Every name exists in the 188-file MorphOS directory.
`+P` adds the bit without replacing other flags. Verify resulting installed
metadata; commands missing from this list still need their own purity audit.

```text
AddBuffers AskHost Assign Avail Battery Beep Break Bz2
CLI CPU ChangeTaskPri ClearRAMDebugLog Clip Clone Copy DOSList
Date Debug Delete DevList Dir DiskChange DiskFree Execute
FSDie FSList FileNote GetRAMDebugLog HDMBRClear HDRead HDWrite HI
ID IconX Install Join LibList List LoadMonDrvs LoadWB
Lock MakeDir MakeLink MemTest ModList Offline Online OpenURL
Passwd PathPart Play PortList Protect Quote RX RXC
RXCmd RXLIB RXSET Reboot Relabel RemRAD Rename RequestChoice
RequestFile ResList SetClock SetDate SetMixer ShutDown Sort Stat
Status TCC TCO TE TS TaskList Time Touch
Type UnMount Uptime WBRun Wait WaitForPort WaitX Which
```

These are required pure candidates, not already qualified CopperOS binaries.
Run CC06 and each command's CC41 checks before emitting the required metadata.

### Original resident lifecycle evidence

| Profile and original script | Registration/use | Required lifecycle |
| --- | --- | --- |
| Workbench `S/Startup-Sequence`, lines 12-13 and 76-77 | Assign and Execute, explicitly forced PURE | Load early; preserve use throughout startup; remove at the original later points. |
| Install `Install/Install`, lines 3411-3413 | IconPos, Delete, Reboot, explicitly forced PURE | Preserve installer resident use and determine the corresponding end/reboot cleanup from the full script. |
| Install `HDSetup/HDSetup`, lines 1931-1933 | Reboot, ExtractKickstart, FindResident, explicitly forced PURE | Preserve setup use and terminal cleanup/reboot ownership. |
| Install `Update/Startup-HardDrive`, lines 13-14 and 55-56 | Assign and Execute, explicitly forced PURE | Register early and remove later in the update startup. |
| MorphOS `S/startup-sequence` | MOSSYS:C/Assign and MOSSYS:C/Execute, explicitly forced PURE | Register early and remove after their startup work. |

The inspected Workbench file headers have P clear, including the resident-used
commands. Keep that observation separate from the required reentrant/re-executable
implementation. Do not derive a blanket NonPure classification from it, nor
silently replace the scripts' original forced-resident behavior.

## Appendix C - Reproducible evidence identifiers

Local Workbench archives are under `D:/TestData/TestImages/`; each contains an
ADF member with the same basename and the `.adf` extension.

| Item | Identifier |
| --- | --- |
| Install archive | `Workbench v3.1 rev 40.42 (1994)(Commodore)(M10)(Disk 1 of 6)(Install)[!].zip` |
| Install ZIP SHA-256 | `e320dbbcb2b8e34da7d3e37a2953c623c26755191a74c2512169b2e6f81f2e78` |
| Install ADF SHA-256 | `8f54e735925d733719a321a0ddabd8fd6d1c1c3d3c0a516ed6b77dee89ac9e1e` |
| Workbench archive | `Workbench v3.1 rev 40.42 (1994)(Commodore)(M10)(Disk 2 of 6)(Workbench)[!].zip` |
| Workbench ZIP SHA-256 | `d93611887acf91f68f5608a5b7812f03eb16f743f40451b67c93067b04c967ed` |
| Workbench ADF SHA-256 | `a8f167bad2897e8c7f2cfe73de7a9f8dad10c7a5b1f78ca17f818ab17278b985` |
| Both disks' version.library | `version 40.42 (18.2.94)`; SHA-256 `b39739aab1efcda75be831b4fa2355eed2e726f427a2e55187a9dee3b44da9ee` |
| Workbench S/Startup-Sequence SHA-256 | `64cb5972947dba207e852ad69a1a84f0aeb84e3f8b7a1f45ffde91d61de2546c` |
| Install Install/Install SHA-256 | `46b0602de5eb15709f098bf909859d8ae111e2648a66ebadb9c8014a09e9dddc` |
| Install HDSetup/HDSetup SHA-256 | `ba74e25b8f4db10c24c09c2b1251bcdcafcd316125b9572b4e03afef1db2f4ac` |
| Install Update/Startup-HardDrive SHA-256 | `53e571b725434dd2e5a875accf38407b079382ec9aeab376384dfeb017df3edf` |
| Original NDK CD | `D:/TestData/AmigaDeveloperCD.iso`; volume `Amiga_Dev_CD_v1.1` |
| NDK CD SHA-256 | `5d6bfcb213f1395d4c95584dc94d0e36265be355076dae1710bd36fb4dfdbff3` |
| NDK_3.1/DOCS/DOC/DOS.DOC SHA-256 | `2e22d1d1b7c00fac5757eead0ef4d49a01c7666f0db5850735193c9597a7cee2` |

ADF enumeration checked directory hash chains, header entry types and zero-sum
header checksums, and read the big-endian protection field at byte 320. This
is file-metadata evidence, not execution evidence.

For the [official MorphOS 3.20 ISO](https://www.morphos-team.net/morphos-3.20.iso),
the following byte ranges were read with HTTP 206 and matching response lengths.
Sector offsets use 2048-byte ISO sectors; filenames were decoded from directory
records, including Rock Ridge names.

| Item | Observed range | SHA-256 of the observed bytes |
| --- | --- | --- |
| MorphOS/C directory | Sector 837, 24576 bytes | `e2f5505b52251b8e5f99874c42c8751f382bc367e95f8475c2c2dad611f79a8f` |
| hdinstall.fixc | Sector 6348, 2478 bytes | `46751fd064329077fe3f1ebeff6ea47c13159dd84841fac83d2043fb270ca8df` |
| MorphOS/S/startup-sequence | Sector 213484, 2158 bytes | `407bc8480dba6acf11c19966d67689996015aab27a6eca04d0af2d3d244168b4` |

Directory inspection traversed 772 directories and found no Freeze file. These
range hashes are not a checksum of the complete ISO; CC00 must still validate
the complete selected image and CC43 must obtain runtime comparison evidence.

The partial [3.20 command source archive](https://www.morphos-team.net/files/src/3.20/c.tar.bz2)
has SHA-256 `db099324cbf4b07de70bcafc626af0b168903a714911f840652e1a364578b5ba`.
It is useful only with per-file license/provenance checks and independent
behavioral validation. Preserve this plan's evidence boundaries in the final
qualification report.


## Latest execution checkpoint (2026-09-23)

MorphOS 3.20 `Search` follows its source-observed `ALL` branch through public
DOS `AnchorPath`, handles nested and sibling directory traversal, uses the
default locale for literal case folding, and applies the captured soft-link
policy: directory links are descended, file links are read as files, and
dangling links are warned about unless `FILE`, `QUIET`, or `QUICK` suppresses
the warning. `QUICK` emits each file's full path and the source's terminal
control sequence, separates the first match, and clears the line after
traversal. CTRL-D prints the captured abandon message and skips the current
file while traversal continues. Its refreshed 68000/020/040 receipt passes 45
supplied vectors per CPU, including those behaviors and the source's 512 KiB
`MEMF_ANY` buffer, failed-allocation halving and recovery, LF-only maximum-line
pre-scan, long-line resize, incomplete-line rewind/reread, short reads,
CTRL-D during pre-scan, and DOS 51.28+ `Size64`/`Seek64` handling above the
classic 32-bit offset limit. See
`artifacts/cc11-search-morphos-native-20260923-seek64-v12/qualification.json`
and `docs/Commands/Workbench31MorphOS320/contracts/Search.md`. The separate
Workbench candidate passes 19 supplied vectors per CPU in
`artifacts/cc11-search-wb31-native-20260923-source-prepass-v1/qualification.json`;
its captured syntax remains a candidate pending binary and guest comparison.
These are supplied-vector results; real >2 GiB handler behavior, larger real
trees, exact original guest output/results, Workbench guest parity,
PURE/resident lifecycle, and packaging remain open.

Workbench 3.1 `C:SetFont` is now hash-bound to the selected 1,092-byte HUNK
(`setfont 39.1 (2.6.92)`, SHA-256
`c3e1e763b12fd4f710a414443facea49479aa723a94828828dbaee214fe4ad8c`). Its
bounded audit records the seven-slot syntax candidate, version-37 library
imports, TextAttr/style construction, case-insensitive `.font` suffix,
console-task `ACTION_DISK_INFO`/`InfoData` boundary, console `Window` font
replacement, and `tf_Style`-gated escape output. Exact behavior of the selected
Workbench handler, output/result precedence, guest parity, PURE/resident
lifecycle, package admission, and differential comparison remain open. See
`docs/Commands/Workbench31MorphOS320/contracts/SetFont.md` and
`reference-captures/setfont-wb31-binary-audit-20260922.json`.
The provisional native resident entry now compiles on 68000/020/040 with the
static receipt `artifacts/setfont-wb31-native-static-20260922-v1/qualification.json`;
its supplied native entry fixture now passes sixteen invocations per CPU at
`artifacts/setfont-wb31-native-entry-20260922-v2/qualification.json`.
Original guest packet/layout parity, PURE/resident admission, installed
placement, package and differential shipping gates remain open.

Workbench `C:LoadResource` is now hash-bound to the selected 3,972-byte HUNK
(`loadresource 40.2 (17.3.93)`, SHA-256
`51c8d6da726d5d1429e84f36e323218eb94750ab60b650efe768fc2907d16f38`). Its
bounded audit records the `NAME/M,LOCK/S,UNLOCK/S` candidate, dependency and
resource/catalog surfaces; exact lock state, diagnostics and runtime behavior
remain open. See `contracts/LoadResource.md` and
`reference-captures/loadresource-wb31-binary-audit-20260922.json`.
Follow-up (2026-09-25): offline HUNK disassembly now confirms the three segment
sizes, the ordered `locale.library` v38, `dos.library` v39,
`utility.library` v39 and `graphics.library` v39 opens, and the exact
`NAME/M,LOCK/S,UNLOCK/S` string passed to DOS `ReadArgs` with three result
slots. It also binds the `sys/c.catalog` argument at the Locale `OpenCatalogA`
call site. The hash-bound addendum is
`reference-captures/loadresource-wb31-code-audit-20260925.json`. Resource
selection and lock transitions remain unknown at runtime; this does not yet
justify a replacement body, and the profile remains unqualified.
Follow-up (2026-09-25): the disassembly audit now records that DOS 39, Utility
39, and Graphics 39 are required startup dependencies, while Locale 38 is
opened optionally for `sys/c.catalog`; it also binds the embedded fallback
catalog pointer/ID table. The LoadResource contract and ledger distinguish
the statically established three-slot ReadArgs grammar from still-open parser
edge behavior and resource state transitions. Its selected Workbench file has
P clear, so a replacement must not claim PURE. Original guest lifecycle,
exact lock/unlock behavior, diagnostics, package and differential evidence
remain outstanding.

Follow-up (2026-09-25): the helper disassembly now records startup
coordination (`Cli`, `Forbid`, `FindPort`, `CreateNewProc`), a semaphore-
protected 16-byte lock-record list, and an active-operation counter. This
narrows the implementation risk to a shared lock coordinator, but does not
establish its owner, persistence across command return, concurrent handoff, or
shutdown protocol. Original guest lifecycle and lock/unlock captures remain a
prerequisite to implementing those transitions; this remains reference
evidence only.

Follow-up (2026-09-26): a hash-bound offline HUNK audit now traces the
coordinator's Exec `SetFunction` installation and conditional restoration of
DOS `LoadSeg`, alongside the saved previous vector and active-operation guard.
The two PC-relative `LEA` instructions resolve to the same HUNK2 hook entry at
offset `0x0000`, clearing the apparent install/restore pointer discrepancy.
The hook takes a `SameLock` match from the 16-byte registry, returns its cached
SegList once, consumes the record, and delegates unmatched names to the saved
DOS vector. A separate opened-resource list backs `LOCK`/`UNLOCK`; a singleton
worker process handles requests and an active-operation count guards hook
teardown. These static findings define an implementable protocol, while guest
scheduling and cross-invocation lifecycle remain unverified. See
`docs/Commands/Workbench31MorphOS320/reference-captures/loadresource-wb31-loadseg-lifecycle-audit-20260926.json`.

Both `AddDataTypes` references are now hash-bound. Workbench 3.1 `C:AddDataTypes`
is a 5,880-byte HUNK (`adddatatypes 39.2 (27.7.92)`, SHA-256
`391da11b39bfc7c492c1b58aa9e442f1c1712bd507380903a88a4534d27c264d`) with the
binary `FILES/M,QUIET/S,REFRESH/S` candidate and a non-PURE protection word.
MorphOS 3.20 `MorphOS/C/AddDataTypes` is a 7,751-byte packed member at ISO
extent 175124 (`AddDataTypes 50.6 (27.11.04)`, SHA-256
`afe23d74f3c3b18cd45f892189a6a26bf1bc8d42a426ec7db816e0c9995d11dd`). Its
released-source contract freezes `FILES/M,QUIET/S,REFRESH/S,LIST/S`, the named
datatype-list semaphore, IFF/DTYP/DTCD loading, refresh directory precedence,
one-directory matcher descent, Workbench startup reply and reverse cleanup.
See `contracts/AddDataTypes.md` and
`reference-captures/adddatatypes-reference-audit-20260922.json`. This is a
reference/specification checkpoint only; exact diagnostics/results, guest
execution, lifecycle/PURE, package and differential gates remain open.

Workbench 3.1 `C:SetPatch` is now hash-bound to the identical 13,484-byte HUNK
found on both selected Install3.1 and Workbench3.1 members (`setpatch 40.16
(14.2.94)`, SHA-256
`745b2f90fabcdeccba099b3302593eff89f393d65bd84a9c6bad1b46063ab5cc`). The
audit and contract record the `QUIET/S,NOCACHE/S,REVERSE/S,NOAGA/S` candidate,
non-PURE protection word and machine-specific patch families. No native
success/no-op path is introduced: exact parser, ROM/machine predicates,
byte-level effects, rollback, lifecycle and package gates remain open. See
`contracts/SetPatch.md` and
`reference-captures/setpatch-wb31-binary-audit-20260922.json`.

Both `IPrefs` references are now hash-bound. Workbench `C:IPrefs` is a
13,848-byte `iprefs 40.7 (2.6.93)` HUNK (SHA-256
`14341be81f06506852204a33e7bfe7eb6fa2067d7f1e55810007bd94b021f0ef`) and
MorphOS `MorphOS/C/IPrefs` is a 43,798-byte packed member at ISO extent 175924
(SHA-256
`2c4f6bf00b8f917fe30675fa7c758850583dc712f64b8bf1fed40c646b38cccf`). The
new contract records the no-template startup service, duplicate guard,
long-lived child/completion protocol, preference table and handler/provider
surface, while keeping the Workbench and MorphOS implementations separate. No
transient no-op service is admitted; exact guest ordering, provider failures,
lifecycle/PURE, package and differential gates remain open. See
`contracts/IPrefs.md` and
`reference-captures/iprefs-reference-audit-20260922.json`.

MorphOS 3.20 `MorphOS/C/LoadMonDrvs` is now hash-bound to its 2,279-byte packed
member at ISO extent 176144 (`LoadMonDrvs 50.1 (14.7.03)`, SHA-256
`c33c5263f6edd1c1ff11738558492d5cbfe371e6117383459b4c319082dea384`). The
contract records the documented `FROM/K,EXCEPT` candidate, DEVS:Monitors
default, alternate directory and excluded-driver behavior, plus the installed
PURE requirement. The packed parser, monitor-driver initialization, exact
diagnostics, lifecycle, package and differential gates remain open; no
host-backed or no-op loader is introduced. Its resident candidate compiles for
68000/020/040 with 17 reachable methods and zero runtime features/helpers,
external targets, exception regions or fatal fault sites in
`artifacts/loadmondrvs-morphos-native-static-20260922-v1/qualification.json`.
Its supplied-vector resident fixture also passes nine default/FROM/EXCEPT,
parser-failure, missing-DOS, matcher-cleanup, successful resident-init and
failed-init/unload and failed-LoadSeg invocations per CPU with zero shared-image
writes in
`artifacts/loadmondrvs-morphos-native-entry-20260922-v3/qualification.json`.
See `contracts/LoadMonDrvs.md` and
`reference-captures/loadmondrvs-morphos-binary-audit-20260922.json`.

The LoadMonDrvs resident fixture now exercises nine supplied vectors per CPU,
including successful resident discovery/initialization and failed
initialization with segment unload, and passes on 68000/020/040 without shared
image writes. The receipt is
`artifacts/loadmondrvs-morphos-native-entry-20260922-v3/qualification.json`.
This remains invocation-boundary evidence; packed parser, real monitor-driver
effects, guest parity, PURE/lifecycle, package and differential gates remain
open.

MorphOS 3.20 `C:SetKeyboard` is now hash-bound to ISO extent 178200 (2,856
bytes, SHA-256 `f12a418b372b7e2d724a2ba56235e58bcd9135466c26fea8031e12f1af3c8fa4`).
The bounded packed-member inspection records the `7f4d4f53` format and version
tag but no plaintext template or diagnostic candidates. A separate DOS 37
resident candidate follows the released `IPrefs` `SetKeyMap` correspondence:
`KEYMAP/A` provisional grammar, resource reuse, absolute paths,
`KEYMAPS:`/`MOSSYS:Devs/Keymaps` fallback, resident/extended-node publication,
duplicate-race serialization, default selection and private-segment rollback.
The three-CPU static and 12-vector-per-CPU receipt is
`artifacts/setkeyboard-morphos-native-entry-20260922-v1/qualification.json`.
Exact packed syntax/diagnostics, original guest behavior, PURE/lifecycle,
package admission and differential comparison remain open.

Workbench disk 2 is now hash-verified from the externally supplied M10 ADF and
all 50 listed `C:` members have durable size/SHA-256 metadata in
`docs/Commands/Workbench31MorphOS320/reference-captures/wb31-disk2-c-command-captures-20260923.json`.
This advances reference identity closure only; disks 3-6, installed placement,
protection flags, guest behavior, PURE/resident lifecycle, packaging and
differential gates remain open.

Workbench Install disk 1 is also hash-verified from the externally supplied M10
ADF (`8f54e735925d733719a321a0ddabd8fd6d1c1c3d3c0a516ed6b77dee89ac9e1e`),
with all 20 listed `C:` members, including installation-only helpers, recorded
in `docs/Commands/Workbench31MorphOS320/reference-captures/wb31-disk1-c-command-captures-20260923.json`.
The three Install/HDSetup/Update startup and installer members are separately
hash-bound in `docs/Commands/Workbench31MorphOS320/reference-captures/wb31-disk1-install-script-captures-20260923.json`.
These records establish intended script evidence only; they do not substitute
for installed-state metadata or guest execution.
This adds reference identity evidence but does not close disks 3-6, machine
variants, installed placement/protection, guest behavior, PURE/resident
lifecycle, packaging or differential gates.

Workbench 3.1 `SetKeyboard` now has a source-bound DOS 36 resident entry for
the captured `KEYMAP/A` grammar. The body reuses resident `keymap.resource`
nodes (including a matching list-tail node), joins `DEVS:Keymaps` with
`AddPart`, loads missing maps with `LoadSeg`, calls
`keymap.library/SetKeyMapDefault`, and retains successful segments while
unloading failed private segments. The three-CPU static receipt is
`artifacts/setkeyboard-wb31-native-static-20260922-v2/qualification.json`;
the executable receipt passes ten supplied vectors per CPU with no shared
image writes or leaked resources at
`artifacts/setkeyboard-wb31-native-entry-20260922-v2/qualification.json`.
The packed MorphOS command remains open because its template/diagnostics are
not captured; related `IPrefs` source behavior (`KEYMAPS:`/
`MOSSYS:Devs/Keymaps`, resident/extended nodes, charset updates) is recorded
as correspondence evidence rather than assumed behavior.

Workbench 3.1 `GuessBootDev` now has a separate DOS 36 resident entry based on
the captured 680-byte installer helper. Its three-CPU receipt passes sixteen
supplied Exec/DOS/utility.library/expansion.library vectors per CPU, covering
signed boot priorities, disabled and unusable filesystems, lock fallback,
parser and library-open failures, Workbench and malformed-startup guards,
missing DOS, and interleaved callers. The receipt is
`artifacts/guessbootdev-wb31-native-20260920-qual/qualification.json`; exact
original boot-node semantics, guest parity, PURE/resident lifecycle, packaging
and differential evidence remain open.

Workbench 3.1 `Check2090` now has a separate DOS 37 resident entry based on the captured 220-byte installation helper. Its three-CPU receipt passes ten supplied Exec/Expansion/DOS vectors per CPU, covering A2090 controller present/absent and flag states, expansion-open failure, Workbench and malformed-startup guards, missing DOS, and interleaved callers. The receipt is `artifacts/check2090-wb31-native-20260920-v2/qualification.json`; exact original result/IoErr behavior, guest parity, PURE/resident lifecycle, packaging and differential evidence remain open.

Workbench 3.1 `IconPos` now has a separate DOS 36 resident entry based on the captured installer command. Its three-CPU receipt passes fifteen supplied Exec/DOS/icon.library vectors per CPU, covering DiskObject position/type and drawer mutation, free-position switches, image copying, default creation, parser/type and storage failures, Workbench and malformed-startup guards, missing DOS, and interleaved callers. The receipt is `artifacts/iconpos-wb31-native-20260920-v2/qualification.json`; exact original icon.library semantics, guest parity, PURE/resident lifecycle, packaging and differential evidence remain open.

## Search MorphOS QUICK and CTRL-D qualification follow-up (2026-09-23)

The hash-bound MorphOS 3.20 Search source and shared `SoftlinkDODIR` helper
confirm the intended behavior: follow directory links, treat links to files as
files, and warn for dangling links unless `FILE`, `QUIET`, or `QUICK` suppresses
the warning. A resident helper now uses invocation-owned scratch and public DOS
calls for `CurrentDir`, `Lock`, `Examine64`/`Examine`, `GetDeviceProc`,
`ReadLink`, `VPrintf`, and cleanup.

The failure reported by the preceding attempt was in the native fixture, not
the Search path lifetime. Its `OpenRaw` callback inspected A1 even though the
SDK declares the filename argument in D1. Correcting that ABI assertion
verified the existing `NameFromLock`/`AddPart` path construction without a
Search implementation workaround. MorphOS `QUICK` now implements the
source-observed full-path prefix, control bytes, first-match line break, and
final line clear. CTRL-D now abandons only the current file and emits the
source message; CTRL-C remains a command failure. The expanded fixture checks
matching, no-match, QUIET, recursive, dangling-link, and file-abandon cases.
The preceding MorphOS checkpoint passed 35 vectors. Source review confirmed
that `LOCALE_VERSION 38` is unused and the active `OpenLibrary` call requests
v37, so the candidate retains v37 while replacing ASCII-only delimiting and
rendering with the bound `Locale.IsCntrl` and `Locale.IsPrint` vectors. The
current receipt passes 37
supplied vectors per CPU on 68000/020/040 in
`artifacts/cc11-search-morphos-native-20260923-locale-classes-v2/qualification.json`.
Workbench Search was rerun against the changed shared renderer and separately
passes 18 supplied vectors per CPU in
`artifacts/cc11-search-wb31-native-20260923-locale-safeguard-v2/qualification.json`;
Workbench QUICK behavior remains unproven. These receipts establish
resident-HUNK/fixture behavior only. Binary and guest comparison, larger trees,
exact Workbench behavior, PURE/resident reuse, package admission, and
differential evidence are still open.

## Search MorphOS source-buffer follow-up (2026-09-23)

MorphOS `FindString` now follows the inspected buffer algorithm: start at up to
512 KiB plus the NUL byte, halve failed `MEMF_ANY` allocations, pre-scan LF-only
line lengths when the file exceeds the buffer, grow to the longest line within
the source ceiling, seek to the beginning, and rewind incomplete trailing lines
for the next match pass. CTRL-D during the pre-scan abandons only the current
file; the match pass preserves line number, context and found state across
chunks. Workbench remains a separate syntax candidate and keeps its bounded
file-sized input path.

The three-CPU resident MorphOS fixture passes 45 invocations per CPU in
`artifacts/cc11-search-morphos-native-20260923-seek64-v12/qualification.json`.
It checks failed-first-allocation recovery, incomplete-line rewind after a
small halved buffer, partial reads in the pre-scan, long-line buffer growth
and rewind/reread using a 524,300-byte fixture, allocation-halving exhaustion,
CTRL-D during pre-scan, and the DOS 51.28+ `Seek64` success and error paths for
a synthetic file size above the 32-bit offset limit. The Workbench fixture passes 19 invocations per CPU in
`artifacts/cc11-search-wb31-native-20260923-source-prepass-v1/qualification.json`,
including a matching line over 8,192 bytes through bounded DOS writes.

These are bounded resident-fixture results, not guest parity or tests against
an actual file larger than 2 GiB. Real handler and memory-pressure behavior,
exact signal/output ordering, packed correspondence, original guest behavior,
PURE/resident lifecycle, and package admission remain open.

## Search MorphOS DOS64 qualification follow-up (2026-09-23)

MorphOS `Search` now reads the public DOS `FileInfoBlock.Size64` fields when
DOS is at least 51.28, and calls `DOS.Seek64` when the file size exceeds the
classic 32-bit offset limit. It seeks to offset zero before matching and uses
signed high/low relative offsets to rewind incomplete lines. The return pair
uses D0:D1; the command detects the `-1` pair and publishes `IoErr` without
reading more data after a failed seek. Smaller files and older DOS providers
retain `FileInfoBlock.Size` and public DOS `Seek`.

The refreshed resident 68000/020/040 fixture passes 45 supplied DOS/Exec/Locale
invocations per CPU (135 total), with 40 reachable methods, no runtime helpers,
external targets, exception regions, fatal sites, leaks or shared-image writes.
It covers absolute and signed relative `Seek64` requests, the 2 GiB threshold,
and seek failure cleanup using a synthetic DOS64 file-size fixture. The
receipt is
`artifacts/cc11-search-morphos-native-20260923-seek64-v12/qualification.json`.
This proves the command's bounded resident path and pair-return ABI against
supplied vectors; it does not prove behavior with real large files or original
MorphOS output. Workbench Search remains a separate 19-vector-per-CPU syntax
candidate. Exact guest parity, packed correspondence, PURE/resident lifecycle,
licensing, packaging and differential evidence remain open.

## Dir MorphOS ALL qualification follow-up (2026-09-23)

The hash-bound MorphOS 3.20 source confirms `DIR,OPT/K,ALL/S,DIRS/S,FILES/S,
INTER/S`, with `OPT A/D/F/I`. The native candidate now descends recursively for
`ALL`/`OPT A` through public DOS `CurrentDir`, `Lock`, and `ExAll` calls, keeps
directory encounter order, sorts file pairs, and implements `OPT I`'s
source-observed line joining. It also models empty-directory success and the
source's `RETURN_ERROR` for explicit `INTER/S`; the interactive command reader
itself is disabled in the inspected source.

The expanded fixture exposed and corrected directory-order, odd-file-pairing,
and deeper ExAll-row storage mistakes. Three-CPU resident receipts now pass
nineteen MorphOS supplied DOS-vector calls per CPU and seventeen Workbench
syntax-candidate calls per CPU. Recursive lock, buffer and ExAllControl
ownership is balanced, with no shared-image writes. The receipts are
`artifacts/cc11-dir-morphos-native-20260923-recursive-all-v6/qualification.json`
and
`artifacts/cc11-dir-wb31-native-20260923-profile-split-v6/qualification.json`.
Wildcard traversal, soft-link behavior, complete error/IoErr and diagnostic
parity, packed correspondence, original guest comparison, PURE/resident reuse,
licensing and packaging remain open. The 0/200 and 0/246 shipping counts do
not change.

## 2026-09-24 implementation checkpoint - MorphOS `Version`

**Completed slice:** correct the system `Version` candidate to follow captured
MorphOS lookup/output semantics, add focused vectors, and retain a stable
three-CPU resident receipt. The implementation now checks Exec `LibList` under
balanced `Forbid()`/`Permit()`, handles missing MorphOS resident and missing
`version.library` cases, and parses a copied dotted `IdString` tail for `FULL`.

**Evidence:**
`artifacts/version-morphos-native-20260924-system-v6/qualification.json` passes
49 supplied DOS/Exec vectors per CPU and the resident static-compatibility gate
on 68000/020/040. The Workbench `Version` candidate's existing 15-vector
regression remains independent.

**Next executable slices:** implement and fixture-test the remaining file-based
Ambient provider paths; resolve MorphOS `RTF_EXTENDED` resident revision layout
from an authoritative ABI; implement source-backed FILE fallback/LoadSeg and
object lookup; then compare the command against the original guest, verify
source/packed correspondence, and close PURE/resident lifecycle, rights,
package and install gates. `Version` remains partial until all applicable
gates are closed.
## 2026-09-24 implementation checkpoint - MorphOS `Version` RES

**Completed slice:** parse named MorphOS resident versions from the resident's
name and `IdString`, preserving normal version/revision/date/extras behavior.
On the source fallback path, read MorphOS `rt_Revision` only when
`RTF_EXTENDED` is set. ABI evidence is pinned in
`docs/Commands/Workbench31MorphOS320/reference-captures/version-morphos-resident-sdk-abi-20260924.json`.

**Evidence:** MorphOS resident HUNK and static compatibility pass on
68000/020/040 with 53 supplied vectors per CPU at
`artifacts/version-morphos-native-20260924-resident-v1/qualification.json`.
The independent Workbench candidate still passes 15 vectors per CPU at
`artifacts/version-wb31-native-20260924-resident-regression-v1/qualification.json`.

**Next executable slices:** complete MorphOS FILE fallback/LoadSeg and object
lookup; then compare against original guests and close source/packed
correspondence, PURE/resident lifecycle, rights, packaging and installed-state
gates. The Ambient providers are recorded in the later follow-ups. `Version`
remains partial until all applicable gates close.

## 2026-09-24 implementation checkpoint - MorphOS `Version` Ambient ARexx

**Completed slice:** add the source-shaped `AMBIENT` `VERSION` ARexx request,
reply parsing, local `Ambient` variable, source result/output behavior and
resource cleanup. The implementation uses invocation-local A6 for
rexxsyslib vectors so it leaves the shared resident image unchanged. SDK
layout/vector evidence is recorded in
`docs/Commands/Workbench31MorphOS320/reference-captures/version-morphos-ambient-arexx-sdk-abi-20260924.json`.

**Evidence:**
`artifacts/version-morphos-native-20260924-ambient-arexx-v1/qualification.json`
passes 58 supplied DOS/Exec vectors per CPU and resident compatibility checks
on 68000/020/040. Each receipt reports zero shared-image writes. The separate
Workbench DOS 36 candidate remains green at fifteen vectors per CPU in
`artifacts/version-wb31-native-20260924-ambient-regression-v1/qualification.json`.

**Remaining gates:** Ambient `IsFileSystem`/`LoadSeg` executable fallback, FILE
fallback and object lookup, original guest comparison, packed/source
correspondence, original PURE/resident classification and lifecycle, rights,
packaging, and installed-state evidence. `Version` and CC17 remain
partial/open until all applicable gates close.

## 2026-09-24 implementation checkpoint - MorphOS `Version` Ambient files

**Completed slice:** after an unsuccessful ARexx query, query DOS `ambient_path`
and try that file first, then `mossys:ambient/ambient`, then
`sys:system/ambient/ambient`. The providers reuse the `$VER:` scanner and set
the local `Ambient` variable only when a parsed version is found. Restore
`pr_WindowPtr` and release opened files and public buffers across the paths.

**Evidence:**
`artifacts/version-morphos-native-20260924-ambient-files-v1/qualification.json`
passes 61 supplied DOS/Exec vectors per CPU on resident 68000/020/040 with
zero shared-image writes. The Workbench DOS 36 candidate remains green at
fifteen vectors per CPU in
`artifacts/version-wb31-native-20260924-ambient-files-regression-v1/`.

**Remaining gates at this checkpoint:** the source's `IsFileSystem`/`LoadSeg`
executable provider was still open here and is now covered by the following
checkpoint. MorphOS FILE fallback/object lookup, original guest comparison,
packed/source correspondence, PURE/resident lifecycle, licensing, package
admission, and installed-state evidence remain open. The supplied vectors
close only the direct Ambient file-provider slice; `Version` and CC17 remain
partial/open.

## 2026-09-24 implementation checkpoint - MorphOS `Version` Ambient LoadSeg

**Completed slice:** when a direct Ambient file has no `$VER:` tag, test it
with DOS `IsFileSystem`; for filesystem-backed paths, close the file, load the
segment, locate its Resident record, parse the resident name and `IdString`,
and always unload the segment. Continue through the captured provider order
when the path is not a filesystem, loading fails, or no Resident is present.

**Evidence:**
`artifacts/version-morphos-native-20260924-ambient-loadseg-v1/qualification.json`
passes 66 supplied vectors per CPU on resident 68000/020/040 with no managed
runtime features, external targets, shared-image writes or leaked resources.
Workbench's separate DOS 36 candidate remains green at fifteen vectors per CPU
in `artifacts/version-wb31-native-20260924-ambient-loadseg-regression-v1/`.

**Remaining gates:** the separate MorphOS FILE `LoadSeg` fallback and object
lookup paths, original guest comparison, packed/source correspondence,
PURE/resident lifecycle, licensing, package admission and installed-state
evidence. The Ambient loaded-segment vectors are fixture evidence only;
`Version` and CC17 remain partial/open.

## 2026-09-24 implementation checkpoint - MorphOS `Version` FILE LoadSeg

**Completed slice:** the direct MorphOS `FILE` scan now follows the captured
filesystem fallback when no `$VER:` tag is found. It closes the open file,
loads the segment, locates a linked Resident, builds and parses resident name
and `IdString` version text, preserves full-file MD5 output, prints the normal
`FULL` fields, compares requested version/revision, and unloads/frees every
owned resource. Non-filesystem, load-failure and missing-Resident cases retain
the source-shaped not-found result; a completed MD5 scan suppresses that
diagnostic as the captured command does.

**Evidence:**
`artifacts/version-morphos-native-20260924-file-loadseg-v2/qualification.json`
passes 70 supplied DOS/Exec vectors per CPU on resident 68000/020/040 images,
with no shared-image writes or leaked resources. Each static compatibility
report has no managed runtime features, external native targets, exception
regions or fatal fault sites. The separate Workbench DOS 36 Version profile
is unchanged and its prior 15-vector-per-CPU regression remains valid.

**Remaining gates:** original guest comparison, packed/source correspondence,
PURE/resident lifecycle, licensing, package admission and installed-state
evidence. Default MorphOS name lookup still lacks its source-ordered resident,
library, device-list and filesystem-priority dispatch. The MorphOS `Version`
command and CC17 remain partial/open.

## 2026-09-24 implementation checkpoint - MorphOS `Version` command-segment lookup

**Completed slice:** if a requested MorphOS `RES` resident is absent, the
command now calls DOS `FindSegment` under a balanced Exec `Forbid`/`Permit`
interval, trying the normal list before the system list. For an ordinary
segment, it follows the segment list, searches each loaded hunk for `$VER:`,
copies the bounded tag payload before parsing, formats and compares the result,
and releases the copied text and scratch memory. Missing segment/tag cases
preserve the object-not-found result. Parsing leaves the loaded command image
unchanged.

**Evidence:**
`artifacts/version-morphos-native-20260924-segment-lookup-v4/qualification.json`
passes 74 supplied DOS/Exec vectors per CPU on resident 68000/020/040 images,
with no shared-image writes or leaked resources. The new vectors cover a
system-list segment hit, a normal-list miss followed by system-list success,
the absent-segment path, and internal/disabled segment fallback to `shellcmd`.
Static compatibility gates remain green.

**Remaining gates:** full automatic named lookup through resident, library and
device lists plus filesystem priority, original guest comparison,
packed/source correspondence,
PURE/resident lifecycle, licensing, package admission and installed-state
evidence. The MorphOS `Version` command and CC17 remain partial/open.

## 2026-09-24 implementation checkpoint - MorphOS `Version` default resident lookup

**Completed slice:** default named `Version` lookups no longer fail closed with
`NotImplemented`. They derive the command/module basename with DOS `FilePart`,
query the Exec Resident table, and share the resident formatter/comparison
path. Resident `MD5SUM` requests retain the source's “no md5sum available”
prefix. At this v1 checkpoint `RES` still used the supplied name directly;
the later LibList checkpoint below corrected it to the captured `FilePart`
basename while keeping command-segment lookup on the original name.

**Evidence:**
`artifacts/version-morphos-native-20260924-default-resident-v1/qualification.json`
passes 75 supplied DOS/Exec vectors per CPU on resident 68000/020/040 images,
with no shared-image writes or leaked resources. Vectors include a path whose
resident match depends on `FilePart`, default resident MD5 behavior, and the
prior command-segment cases. The independent Workbench 3.1 candidate still
passes 15 vectors per CPU in
`artifacts/version-wb31-native-20260924-default-resident-regression-v2/`.

**Remaining gates:** the default named search still lacks the source-ordered
filesystem library candidates, `DeviceList`/filesystem device candidates, and
terminal direct-file/volume ordering. The existing command-segment fallback
must move to the source's final lookup position after those candidates are
implemented. Original guest comparison, packed/source correspondence,
PURE/resident lifecycle, licensing, package admission and installed-state
evidence also remain open; MorphOS `Version` and CC17 remain partial/open.

## 2026-09-24 implementation checkpoint - MorphOS `Version` Exec LibList lookup

**Completed slice:** after DOS `FilePart` and Exec Resident lookup miss, the
default named path scans the Exec `LibList` under `Forbid`/`Permit` and matches
node names with Utility `Stricmp`. It captures numeric version/revision,
copies the node name before releasing the list lock, uses the library
`IdString` for matching `FULL` date/extra formatting, and falls back to
numeric node fields when that string cannot provide matching values. A
caller-provided `utility.library` v37 base is held for the lookup and closed
even when lookup fails. `RES` uses `FilePart` too, consistent with the
captured source. The command-segment fallback remains after this new LibList
stage; completing and ordering the intervening filesystem and DeviceList
candidates is still required.

**Evidence:**
`artifacts/version-morphos-native-20260924-liblist-v17/qualification.json`
passes 78 supplied DOS/Exec vectors per CPU on resident 68000/020/040 images,
with no shared-image writes or leaked resources. The utility-call comparison
ABI, unavailable-library path and cleanup are included. The independent Workbench 3.1 regression
passes 15 vectors per CPU at
`artifacts/version-wb31-native-20260924-liblist-regression-v4/`.

**Remaining gates:** MorphOS `MOSSYS:LIBS`/`LIBS:` file candidates,
`DeviceList`, `MOSSYS:DEVS`/`DEVS:` file candidates, direct file/volume
resolution and moving command-segment lookup to the source's final position.
Original guest comparison, packed/source correspondence, PURE/resident
lifecycle, licensing, package admission, installed-state evidence, and CC17
completion also remain open.

## 2026-09-24 implementation checkpoint - MorphOS `Version` DeviceList and DEVS paths

**Completed slice:** after the Exec `LibList` and `MOSSYS:LIBS/`/`LIBS:` candidates miss, default named lookup now scans Exec `DeviceList` under `Forbid`/`Permit`, compares names through the already-owned Utility `Stricmp` lease, copies node names while the list lock is held, and supports numeric and `FULL` `IdString` output. When no device node matches, it searches `MOSSYS:DEVS/` and then `DEVS:` with the captured lowercase `.elf` suffix preference. The command-segment lookup remains after these candidates.

**Evidence:** `artifacts/version-morphos-native-20260924-devlist-v10/qualification.json` passes 85 supplied DOS/Exec vectors per CPU on resident 68000/020/040 images, with no shared-image writes or leaked resources. New vectors check numeric and `FULL` DeviceList output and confirm the `MOSSYS:DEVS/` file candidate follows a DeviceList miss.

The independent Workbench 3.1 regression remains green at 15 vectors per CPU in `artifacts/version-wb31-native-20260924-devlist-regression-v5/`.

**Remaining gates:** terminal direct-file/volume resolution must precede `FindSegment` as in the source. Device and file error/result precedence, exact original-guest comparison, packed/source correspondence, PURE/resident lifecycle, licensing, package admission, installed-state evidence, and CC17 completion also remain open.

## 2026-09-24 implementation checkpoint - MorphOS `Version` direct-file fallback

**Completed slice:** after the source-ordered LIBS candidates, Exec `DeviceList`, and DEVS candidates miss, non-`RES` lookups now try the original named path as a file. The DOS `OpenRaw`/`Read`/`Close` scan reuses the existing `$VER:` and MD5 path. A successful direct file skips `FindSegment`; a missing direct file continues to the final resident command-segment lookup. `RES` skips the direct-file attempt, matching the source order.

**Evidence:** `artifacts/version-morphos-native-20260924-directfile-v3/qualification.json` passes 87 supplied DOS/Exec vectors per CPU on resident 68000/020/040 images, with no shared-image writes or leaked resources. New vectors cover a direct-file hit after LIBS/DEVS misses and a direct-file miss before the final `FindSegment` hit. The independent Workbench 3.1 regression is in `artifacts/version-wb31-native-20260924-directfile-regression-v6/`.

**Remaining gates:** MorphOS volume resolution through `GetDeviceProc`, DOSList device lookup, and handler segment resident scanning still precedes `FindSegment` in the source and is not implemented. Verify status, `IoErr`, diagnostics, and MD5 precedence across all remaining paths; original-guest comparison, packed/source correspondence, PURE/resident lifecycle, licensing, package admission, installed-state evidence, and CC17 completion also remain open.

## 2026-09-24 implementation checkpoint - MorphOS `Version` volume fallback

**Completed slice:** volume names now skip the ordinary Exec Resident,
LibList, Utility, and filesystem-name search. The command calls
`GetDeviceProc`; for a device node it reads that handler directly, while for a
volume it walks `LDF_DEVICES` under `LDF_READ` and matches the handler task.
The matched node's handler segment is searched with the existing resident
scanner. `DevProc` and any held DOS-list lock are released on every normal
branch. If no device node or handler segment is available, lookup continues to
the final `FindSegment` path.

**Evidence:** `artifacts/version-morphos-native-20260924-volume-v5/qualification.json`
passes 93 supplied DOS/Exec invocations per CPU on resident 68000/020/040
images, with no shared-image writes or leaked resources. The new vectors cover
a direct device node, a volume whose task matches the second device-list node,
and volume lookups with missing DevProc, device mapping, handler segment, and
Resident branches. They also cover a requested-version mismatch and the
unavailable MD5 output. The direct-device vector proves that volume lookup does
not require `utility.library`.
The separate Workbench 3.1 candidate remains green at 15 vectors per CPU in
`artifacts/version-wb31-native-20260924-volume-regression-v7/`.

**Remaining gates:** verify exact status, `IoErr`, and diagnostic precedence
against the original MorphOS guest. Also verify PURE/resident lifecycle and
close packed/source correspondence, licensing, package admission,
installed-state evidence, and CC17 completion.

## 2026-09-25 implementation checkpoint - MorphOS `AddDataTypes` DTCD loader

**Completed slice:** the resident qualification root now opens the required
`utility.library` v37, `iffparse.library` v37, `locale.library` v37 and
`datatypes.library` v44 in the MorphOS source order, with reverse cleanup and
preserved `IoErr` when a dependency is missing. It also calls the production
`NativeCommandArguments` ReadArgs lease with MorphOS's four-slot template and
maps FILES, QUIET, REFRESH and LIST. The DOS-owned RDArgs and result storage
stay live while options are consumed and are freed before releasing the shared
datatype-list semaphore. The test root exercises LIST output, FILES
no-match/matched cleanup, a missing-DTHD IFF EOC path, a synthetic valid DTHD
copy/registration, and both successful and failed DTCD loads. Exported resident
read/allocate/free adapters declare the DOS callback registers, with a
per-descriptor loader state and a raw DOS LVO binding that introduces no
nullable/runtime helper dependency.

**Evidence:** `artifacts/adddatatypes-morphos-files-iff-20260925-v13/qualification.json`
passes twenty-five supplied command-fixture invocations and one
callback-wrapper probe per CPU on resident 68000/020/040 HUNKs. The command
root has 44 reachable methods; both roots have no managed allocations/runtime helpers, external
targets, exceptions, fatal sites, leaks, or shared-image writes. The fixtures
check copied DTCD bytes, callback addresses, `InternalLoadSeg` argument
registers and stack cell, segment/function publication, failed-load cleanup,
and execution of read/allocate/free adapters through the SDK's indirect-call
wrappers. The probe exposed and fixed a read-callback register mismatch: the
buffer and byte count now use the published A0 and D0 registers. These tests
still do not invoke callbacks through a real MorphOS loader. The FILES fixture
now also verifies entry into the first directory and clearing `APF_DIDDIR`.
New REFRESH cases
cover same-lock alias de-duplication, unchanged-date skips, empty-directory
scans, date-stamp updates and `pr_WindowPtr` restoration. This remains
synthetic fixture evidence; it does not confirm the directory dates against a
MorphOS guest. The earlier v2 directory is marked invalidated because an
attempted requalification overwrote its 68000 HUNK before the method-count
preflight failed.

**Remaining gates:** deeper nonrecursive directory traversal,
malformed/duplicate/open DTHD cases,
real DOS callback execution and segment unload, exact `AROS_STACKSIZE`, guest
confirmation of REFRESH/date behavior, actual CLI/Workbench startup, and the
separate Workbench implementation remain open. MorphOS private offsets need
packed-reference and authentic guest confirmation; both profile rows still
need guest parity, lifecycle/PURE, rights, installed metadata, and package
admission. Program-wide inventory and reference closure remain open, so
shipping stays at 0/200 commands and 0/246 profiles.

## 2026-09-25 implementation checkpoint - MorphOS `AddDataTypes` duplicate descriptors

**Completed slice:** the native fixture now seeds an existing DTHD compound
descriptor and exercises the three source-defined name-collision paths:
preserve an open descriptor without changing its IDs; retain a closed,
case-insensitive same descriptor while refreshing GroupID/ID; and unlink/free a
changed closed descriptor before inserting its replacement. Assertions inspect
both datatype-list links, sorted-list membership, preserved or updated header
fields, and the corresponding candidate/existing allocation releases. No
production behavior was changed; these cases qualify the existing
`AddDatatype`/`DeleteDatatype` behavior against the extracted MorphOS C source.

**Evidence:** the refreshed receipt
`artifacts/adddatatypes-morphos-files-iff-20260925-v14/qualification.json`
passes 28 command-fixture invocations and one callback-wrapper probe for each
resident 68000/020/040 image. Each command image has 44 reachable methods; the
callback probe has five. Compatibility reports and execution receipts show no
managed allocations, runtime helpers, external native targets, exceptions,
fatal sites, leaks, or shared-image writes. The fixture build has four existing
nullable warnings in unrelated fixture files.

**Remaining gates:** malformed DTHD/IFF bounds, deeper nonrecursive traversal,
real DOS callback execution and segment unload, exact `AROS_STACKSIZE`, guest
confirmation of duplicate replacement and REFRESH dates, actual CLI/Workbench
startup, and a separate Workbench implementation. MorphOS private offsets still
need packed-reference and authentic guest confirmation; both profile rows still
need guest parity, lifecycle/PURE, rights, installed metadata, and package
admission. Program-wide inventory and reference closure remain open, so
shipping stays at 0/200 commands and 0/246 profiles.

## 2026-09-25 implementation checkpoint - MorphOS `AddDataTypes` malformed DTHD bounds

**Completed slice:** the resident command fixture now checks that DTHD parsing
rejects a property shorter than the public header, a name offset outside the
declared header, a pattern with no terminator inside that header, and a mask
whose word range crosses the header boundary. Assertions cover the early
return versus DTCD-property lookup boundary, absence of descriptor allocation
or registration, and cleanup of the surrounding IFF/file/matcher resources.
This adds defensive parser evidence; it does not assert that malformed inputs
match the original source's behavior where the source trusts its offsets.

**Evidence:** the three-CPU receipt
`artifacts/adddatatypes-morphos-files-iff-20260925-v15/qualification.json`
passes 32 supplied command-fixture invocations and one callback-wrapper probe
per resident 68000/020/040 image, with 44 command-root methods, five callback
probe methods, no shared-image writes, and no leaks. The qualified command
source is unchanged from v14; only its fixture matrix and receipt metadata grew.

**Remaining gates:** broader malformed-property combinations and authentic
guest behavior for boundary inputs; deeper nonrecursive traversal; real DOS
callback execution and segment unload; exact `AROS_STACKSIZE`; guest
confirmation of duplicate replacement and REFRESH dates; CLI/Workbench startup;
and the separate Workbench implementation. MorphOS private offsets and both
profile rows still need packed-reference/guest comparison, lifecycle/PURE,
rights, installed metadata, and package admission. Program-wide inventory and
reference closure remain open, so shipping stays at 0/200 commands and 0/246
profiles.

## 2026-09-25 implementation checkpoint - MorphOS `AddDataTypes` startup and flat traversal

**Completed slice:** the resident entry now receives a Workbench startup
message before opening DOS, skips CLI ReadArgs for Workbench launches, processes
each file argument under its `WBArg` lock, and replies only after releasing the
descriptor file/IFF, shared-list semaphore, secondary libraries, and DOS. It
duplicates the first argument's directory lock with the raw DOS `DupLock` LVO,
restores the original process directory, and releases only that duplicate.
Workbench fixtures cover zero file arguments, one descriptor argument, a
missing argument list, and utility-library open failure; they check message
reply after command cleanup. The FILES fixture also supplies a directory,
descriptor, later nested directory, and another descriptor; it verifies the
command does not request a second directory entry but continues to the later
file, with balanced current-directory and IFF cleanup.

**Evidence:**
`artifacts/adddatatypes-morphos-files-iff-20260925-v17/qualification.json`
passes 38 supplied command-fixture invocations plus one callback-wrapper probe
for each resident 68000/020/040 image. The Workbench fixture covers one file
argument, rejects a wide `sm_NumArgs` value whose `WBArg` range wraps, and
checks the duplicated startup lock, per-argument directory switch,
restoration, unlock, zero ReadArgs allocations, and message reply after cleanup.
Each command image has 46 reachable methods; each callback probe has five.
Compatibility and execution receipts report no managed allocations, runtime
features or helpers, external native targets, exception regions, fatal sites,
leaks, or shared-image writes. Builds report existing unrelated nullable and
unused-field warnings.

**Remaining gates:** these startup and traversal checks are synthetic; they do
not establish authentic MorphOS guest behavior. More malformed Workbench
structure cases, deeper traversal and malformed IFF combinations, real DOS
callback execution and segment unload, exact `AROS_STACKSIZE`, authoritative
version-specific MorphOS private ABI confirmation, guest checks for REFRESH/dates and duplicate
replacement, the separate Workbench profile, original guest comparison,
PURE/resident classification, rights, installed metadata, and package
admission remain open. Program-wide inventory and reference closure remain
open; shipping stays at 0/200 commands and 0/246 profiles.

## 2026-09-25 evidence checkpoint - MorphOS datatype-list ABI source

The official MorphOS source index lists `datatypes.library` under MorphOS
3.20 and links a source archive at a 3.19 path. Its `datatypes_intern.h`
declares the private `DataTypesList` and `CompoundDatatype` field orders used
by the resident candidate. The archive and header hashes plus recovered field
orders are recorded in
`docs/Commands/Workbench31MorphOS320/reference-captures/morphos-datatypes-library-source-audit-20260925.json`.
This narrows the MorphOS layout uncertainty but does not prove that the linked
3.19 archive matches the installed 3.20 library, establish segment ownership
or concurrency behavior, or provide the Workbench 3.1 library ABI. Guest
comparison and shipping gates remain open.

## 2026-09-25 implementation checkpoint - Workbench argument count width

**Completed slice:** `WBStartup.sm_NumArgs` is read as its full 32-bit field.
The `WBArg` array boundary is checked by division before pointer arithmetic,
so a large count cannot wrap a multiply and pass the range check. The startup
fixture supplies `sm_NumArgs = 0x00010001` with an argument-list address near
the end of the 32-bit space and verifies rejection before `DupLock` or any
argument access.

**Evidence:**
`artifacts/adddatatypes-morphos-files-iff-20260925-v17/qualification.json`
passes 38 command fixtures and one callback probe per CPU (68000, 68020,
68040), with 46 reachable command methods and zero shared-image writes. The
receipt hash matches the current `NativeMorphOSAddDataTypesCommand.cs` source.

**Remaining gates:** this is synthetic resident evidence. It does not close
original MorphOS guest comparison, DOS-loader callback and segment-lifetime
behavior, Workbench 3.1 ABI/profile coverage, PURE classification, or package
admission.

## 2026-09-25 implementation checkpoint - MorphOS `LoadMonDrvs` selection vectors

**Completed slice:** corrected the supplied-vector fixture so a successful
`MatchFirst` populates its actual matched entry even when `EXCEPT` is present.
The resident candidate now has executable fixture coverage for excluding a
matching filename case-insensitively, allowing another driver through
`EXCEPT` from an alternate `FROM` directory, and skipping a directory entry
before `LoadSeg`. The alternate-path vector also checks the path returned by
`NameFromLock` is joined with the matched file name.

**Evidence:**
`artifacts/loadmondrvs-morphos-native-entry-20260925-except-directory-v6/qualification.json`
passes sixteen supplied DOS/Exec vectors on each resident 68000/020/040 HUNK.
The compiled root has 22 reachable methods and zero managed runtime features
or helpers, external native targets, exception regions or fatal fault sites;
the runner reports zero shared-image writes and no fixture leaks. The
qualification script now enforces these current counts.

**Remaining gates:** these vectors establish replacement-side branches only.
They do not establish the packed command's parser, original MorphOS guest
directory ordering, diagnostic/result precedence, provider effects, lifecycle,
PURE metadata, licensing, or package admission. `CC20.LoadMonDrvs.morphos320`
remains partial/open and the goal-wide inventory and shipping counts are
unchanged.

## 2026-09-25 implementation checkpoint - MorphOS `LoadMonDrvs` multi-entry scan

**Completed slice:** the resident fixture now models successive DOS matcher
results instead of stopping after one. A new two-file vector returns the
excluded `PAL` match first and then `NTSC`; it verifies the candidate calls
`MatchNext`, skips only the excluded entry, builds the second file's path,
loads its segment, finds its resident and calls `InitResident`, then completes
the matcher lifecycle. Per-entry DOS paths and call counts are asserted.

**Evidence:**
`artifacts/loadmondrvs-morphos-native-entry-20260925-multiple-match-v7/qualification.json`
passes seventeen supplied DOS/Exec vectors per CPU on resident 68000/020/040
HUNKs. The root has 22 reachable methods, zero managed runtime features or
helpers, external native targets, exception regions or fatal fault sites, no
fixture leaks, and zero shared-image writes.

**Remaining gates:** multiple-entry behavior is still replacement-side
fixture evidence. Driver ordering and resident effects need confirmation on an
original MorphOS guest; packed parser correspondence, diagnostic/result
precedence, lifecycle/PURE, rights, installation and package admission remain
open. The profile and goal-wide shipping counts remain unchanged.

## 2026-09-25 implementation checkpoint - Workbench `SetFont` protection window

**Completed slice:** bounded disassembly of the hash-bound Workbench 3.1
`C/SetFont` HUNK confirms that the original command protects the window font
replacement with `Exec.Forbid`/`Exec.Permit`: it calls `SetFont`, conditionally
closes the old font, publishes the replacement pointer at `Window.Font` offset
`0x80`, then permits scheduling. The native candidate now follows this order,
and its supplied fixture asserts the balanced protection window and zero
protection calls on pre-install failures.

**Evidence:**
`docs/Commands/Workbench31MorphOS320/reference-captures/setfont-console-forbid-permit-audit-20260925.json`
records the code offsets and LVOs against the 1,092-byte binary hash.
`artifacts/setfont-wb31-native-entry-20260925-forbid-permit-v5/qualification.json`
passes sixteen supplied vectors on each resident 68000/020/040 HUNK, with
nineteen reachable methods, zero managed allocations/runtime features/helpers,
external targets, exception regions or fatal machine-fault sites, zero shared
image writes, and no fixture leaks.

**Remaining gates:** static disassembly and supplied vectors do not prove real
Workbench console-window concurrency, handler-specific packet/result behavior,
original output, PURE/resident lifecycle, licensing, package admission, or
differential parity. `CC20.SetFont.wb31` remains partial/open; goal-wide
inventory and shipping counts are unchanged.

## 2026-09-25 implementation checkpoint - Workbench `SetFont` style output

**Completed slice:** the captured HUNK reads `TextFont.tf_Style` and emits the
requested italic, bold, and underline console escapes only when the opened
font does not already supply that style. The resident candidate now follows
those three independent bit checks. The fixture covers each pre-styled case
and a mixed-style case where only the missing bold style is emitted.

**Evidence:** the hash-bound static observations are recorded in
`docs/Commands/Workbench31MorphOS320/reference-captures/setfont-style-output-audit-20260925.json`.
The refreshed resident receipt
`artifacts/setfont-wb31-native-entry-20260925-style-gating-v6/qualification.json`
passes twenty supplied vectors per CPU on 68000/020/040, with nineteen
reachable methods, no managed allocation/runtime feature/helper/external
target/exception/fatal-site findings, no shared-image writes, and no fixture
leaks.

**Remaining gates:** these checks establish candidate branch behavior against
static HUNK observations, not rendered output or result behavior on an original
Workbench guest. Handler-specific packet/result behavior, output failures,
PURE/resident lifecycle, rights, packaging, and differential comparison remain
open; no shipping status is promoted.

## 2026-09-25 implementation checkpoint - Workbench `SetFont` console window

**Completed slice:** a full pass over the captured HUNK identified action 25
as `ACTION_DISK_INFO`, not a console-unit query. It passes a BPTR to
`InfoData`, reads the console `Window` from `id_VolumeNode`, and uses public
`Window.RastPort` and `Window.Font` fields. A missing window takes a successful
no-op path. The candidate and fixture now model the 36-byte InfoData buffer,
raw Window pointer, field offsets, no-window result, and ignored `DoPkt` D0.

**Evidence:** the HUNK trace and public structure offsets are recorded in
`docs/Commands/Workbench31MorphOS320/reference-captures/setfont-console-window-audit-20260925.json`.
The official DOS reference specifies action 25 as `ACTION_DISK_INFO` with a
BPTR to InfoData and documents the console Window result. The refreshed
resident receipt
`artifacts/setfont-wb31-native-entry-20260925-console-window-v7/qualification.json`
passes twenty-one supplied vectors per CPU on 68000/020/040, with nineteen
reachable methods, no managed allocations/runtime features/helpers/external
targets/exception/fatal sites, no shared-image writes, and no fixture leaks.

**Remaining gates:** this closes replacement-side packet/layout interpretation
against static HUNK and public DOS documentation. It does not verify the
selected Workbench 3.1 console handler, returned Window lifetime, output and
error behavior, PURE/resident lifecycle, rights, packaging, or guest
differential parity. The `SetFont` shipping row remains partial/open.

## 2026-09-25 implementation checkpoint - Workbench `SetFont` proportional fonts

**Completed slice:** disassembly shows that the `PROP` switch both sets
`FPF_PROPORTIONAL` in the `TextAttr` and skips a post-open rejection check.
Without `PROP`, the original checks the returned `TextFont.Flags`; if the font
is proportional, it closes that font and fails with error 212. The resident
candidate now preserves this rule, with supplied cases for rejection and
acceptance.

**Evidence:** the code offsets and flag mapping are recorded in
`docs/Commands/Workbench31MorphOS320/reference-captures/setfont-proportional-font-audit-20260925.json`.
The refreshed receipt
`artifacts/setfont-wb31-native-entry-20260925-prop-font-v8/qualification.json`
passes twenty-three supplied vectors per CPU on resident 68000/020/040 HUNKs,
with nineteen reachable methods, zero managed allocations/runtime
features/helpers/external targets/exception/fatal sites, no shared-image
writes, and no fixture leaks.

**Remaining gates:** diskfont selection and diagnostics need comparison on an
original Workbench 3.1 guest. Console handler behavior, output/result
precedence, PURE/resident lifecycle, rights, package admission, and
differential parity also remain open; `SetFont` remains partial/open.

## 2026-09-25 implementation checkpoint - Workbench `SetFont` result policy

**Completed slice:** static disassembly recovers the return and IoErr branches:
`ReadArgs` and size failures return `RETURN_FAIL`; OpenDiskFont failure and a
missing console task use error 205; a proportional font without `PROP` is
closed and rejected with error 212; a missing console Window succeeds without
changing the font or writing output; completed output returns success without
checking `PutStr` or `Flush` results. The candidate and fixture now cover these
branches.

**Evidence:** observations are recorded in
`docs/Commands/Workbench31MorphOS320/reference-captures/setfont-result-policy-audit-20260925.json`.
The latest resident receipt
`artifacts/setfont-wb31-native-entry-20260925-result-policy-v11/qualification.json`
passes twenty-three vectors per CPU on 68000/020/040, with eighteen reachable
methods, no managed allocation sites, runtime helpers/features, external
targets, exception/fatal sites, or shared image writes.

**Remaining gates:** the HUNK audit does not establish the selected Workbench
handler's effects, failed output side-effects, real concurrency, original guest
differential parity, PURE/resident lifecycle, rights, package admission, or
installed placement. `CC20.SetFont.wb31` remains partial/open; shipping stays
at 0/200 commands and 0/246 profiles.

## 2026-09-26 implementation checkpoint - Workbench `SetFont` output failures

**Completed slice:** the selected HUNK does not branch on `PutStr` or `Flush`
results; it completes with success after attempting console reset/style
output. Added a supplied vector in which all output and flush operations fail,
and verified that the resident candidate retains the success result.

**Evidence:** the updated three-CPU receipt
`artifacts/setfont-wb31-native-entry-20260926-output-failure-v12/qualification.json`
passes twenty-four vectors per resident 68000/020/040 HUNK, with eighteen
reachable methods, no managed allocation sites, runtime features/helpers,
external targets, exception/fatal sites, leaks, or shared-image writes.

**Remaining gates:** original handler/output behavior, real concurrency,
complete option and diagnostic parity, PURE/resident lifecycle, rights,
installed placement, packaging, and differential acceptance remain open.
`CC20.SetFont.wb31` remains partial/open; shipping stays at 0/200 commands and
0/246 profiles.

## 2026-09-26 implementation checkpoint - Workbench `SetFont` TextAttr flags

**Completed slice:** disassembly resolves the `SCALE` and `PROP` option effects
on `TextAttr.ta_Flags`: default `0x43`, `PROP` adds `FPF_PROPORTIONAL` (`0x20`),
and `SCALE` clears `FPF_DESIGNED` (`0x40`). Added an independent SCALE-only
case and recorded all four option combinations.

**Evidence:**
`docs/Commands/Workbench31MorphOS320/reference-captures/setfont-textattr-flags-audit-20260926.json`
binds the observations to the captured HUNK. The refreshed receipt
`artifacts/setfont-wb31-native-entry-20260926-textattr-flags-v13/qualification.json`
passes twenty-five resident vectors per CPU on 68000/020/040, with eighteen
reachable methods, no managed allocation sites/runtime helpers/features,
external targets, exception/fatal sites, leaks, or shared-image writes.

**Remaining gates:** actual diskfont selection, guest output and diagnostics,
complete parser/result parity, PURE/resident lifecycle, original protection
metadata, guest differential comparisons, installed placement, licensing and
package admission remain open. `CC20.SetFont.wb31` remains partial/open;
shipping stays at 0/200 commands and 0/246 profiles.

## 2026-09-26 implementation checkpoint - Workbench `SetFont` SIZE/N word

The selected `SetFont` HUNK stores the parsed SIZE/N LONG's low word in
`TextAttr.ta_YSize`, then uses unsigned `CMP.W`/`BLS` to reject values at or
below four. The candidate previously compared all 32 bits; it now masks the
low word before comparison and storage. The hash-bound disassembly and derived
boundaries are recorded in
`docs/Commands/Workbench31MorphOS320/reference-captures/setfont-size-word-audit-20260926.json`.

The refreshed receipt
`artifacts/setfont-wb31-native-entry-20260926-allocation-cleanup-v19/qualification.json`
passes 33 supplied invocations per resident 68000/020/040 image, including
low-word 0/4 rejection and 5/`0xFFFF` continuation plus cleanup when each
owned allocation fails. Each image has eighteen reachable methods, no managed
runtime features/helpers, external targets, exception/fatal sites, leaks, or
shared-image writes. The managed Commands suite also remains green at 704/704.
This is source-bound candidate evidence; actual ReadArgs text parsing and
Workbench guest behavior, installed PURE/resident state, licensing, packaging
and differential acceptance remain open. `CC20.SetFont.wb31` remains
partial/open.

## 2026-09-26 implementation checkpoint - Workbench `SetFont` font ownership

The hash-bound HUNK keeps its `OpenDiskFont` result in A2 through shared
cleanup. On the NULL-window success path it clears result/IoErr and does not
call `CloseFont`; selected post-open lookup failures also skip `CloseFont`.
The candidate preserves the no-window success behavior, and closes an
untransferred font on failure as a cleanup difference. The audit records the
relevant HUNK offsets and limits; real guest reference counts and compatibility
still need an original Workbench run.

The refreshed receipt
`artifacts/setfont-wb31-native-entry-20260926-font-ownership-v20/qualification.json`
passes 33 supplied invocations per resident 68000/020/040 image, with eighteen
reachable methods, zero managed runtime features/helpers, external targets,
exception/fatal sites, resource leaks, or shared-image writes. It explicitly
checks that the no-window path opens but does not close the font. SetFont
remains partial/open; the overall shipping counts remain 0/200 commands and
0/246 profiles.

## 2026-09-27 implementation checkpoint - Workbench Which current-HUNK parity

The Workbench `Which` classic parser now matches two original `ReadArgs`
failures through DOS `PrintFault`: missing required `FILE` returns WARN/5,
prints `required argument missing`, and leaves caller IoErr 116; an unknown
switch returns WARN/5, prints `wrong number of arguments`, and leaves caller
IoErr 118. The fix is Workbench-specific; MorphOS is unchanged pending
original parser evidence.

The new Workbench resident HUNK passes 22 supplied vectors per CPU on
68000/020/040, and all 22 bounded Workbench original/candidate guest cases
now match using the current HUNK (four route cases, sixteen switch cases, two
parser failures). MorphOS shared code passes its 17-vector-per-CPU regression.
See the `Which` contract, progress log and completion ledger for receipts.

This removes stale-HUNK evidence as an immediate qualification concern; it does
not close the command. Remaining `Which` gates include other route/name and
assign collisions, complete errors/break behavior, MorphOS alias syntax and
semantics, actual installed pure/resident lifecycle, rights and package
admission. Overall shipping acceptance remains 0/200 command identities and
0/246 profile rows.

## 2026-09-27 implementation checkpoint - Workbench Which parser help

Original and current-candidate Workbench guests were compared for two classic
`?` inputs. `C:Which ?` emits the exact template plus
`required argument missing`, returns 5 and leaves caller IoErr 116.
`C:Which Execute ?` emits the template and the normal C: match, returns 0
and leaves caller IoErr 0. The supplied filename proceeds through lookup after
the syntax text. Both pairs match byte-for-byte using the current 68000 HUNK;
aggregate receipt:
`artifacts/workbench31-guest-command-which-readargs-help-20260927-v1/evidence-summary.json`.

This closes two observed Workbench parser-help cases only. MorphOS grammar and
help behavior, full `Which` lookup/error behavior, original pure/resident
lifecycle and package admission remain open.

## 2026-09-27 implementation checkpoint - Workbench Which internal matrix

Fresh original/current-candidate Workbench comparisons now cover all eight
classic `NORES`/`RES`/`ALL` combinations for internal `CD`. All exact output,
return and caller IoErr observations match. `ALL` prints `INTERNAL CD` but
returns WARN/5 with IoErr 205; `NORES RES` conflicts return WARN/5 with
IoErr 0. The current 4,932-byte HUNK is bound by the aggregate
`artifacts/workbench31-guest-command-which-current-hunk-20260927-v1/evidence-summary.json`,
which now covers 32 bounded Workbench original/candidate cases. These results
do not establish child Result2 or MorphOS alias/help behavior, other name
categories, pure/resident lifecycle or package admission.

## 2026-09-27 implementation checkpoint - Workbench Version device fallback

At this checkpoint, the Workbench Version HUNK had three exact default-provider guest
comparisons: `LIBS:version.library`, explicit `DEVS:clipboard.device`, and the
unqualified basename `clipboard.device`. The two device cases emit
`clipboard.device 38.8\n` (22 bytes), return 0 and leave caller post-System
IoErr 0. The unqualified case uses a 3,600-frame capture because the original
2,400-frame window ended before the candidate's DOS `LoadSeg` fallback
completed; the longer original and candidate runs both publish stable complete
records and compare equal. That 68000 HUNK was 14,552 bytes with SHA-256
`48260077d3f997bd5262d4a348a3197cba8a5d015cccadd3c47b612a4ba8ec91`, matching
the previous candidate image byte-for-byte.

Receipts:

- `artifacts/workbench31-guest-command-version-default-files-candidate-20260927-v3/effect-comparison.json`
- `artifacts/workbench31-guest-command-version-devs-explicit-candidate-20260927-v1/effect-comparison.json`
- `artifacts/workbench31-guest-command-version-clipboard-basename-candidate-20260927-v1/effect-comparison.json`

This closes only these three bounded default lookup cases. The initial
2,400-frame incomplete capture is retained as a timing control; extending the
window resolved it without changing command source or HUNK behavior. Other
provider names and failures, broader Version behavior, MorphOS parity,
PURE/resident lifecycle, media closure, rights and package admission remain
open. Shipping remains 0/200 commands and 0/246 profiles.

## 2026-09-27 implementation checkpoint - Workbench Version provider miss

The Workbench Version candidate no longer overwrites the final default
provider's DOS `IoErr` with `ERROR_NOT_IMPLEMENTED`. When the direct-name,
`LIBS:` and `DEVS:` candidates all miss, it now propagates the final DOS error
to `PrintFault`, falling back to `ERROR_OBJECT_NOT_FOUND` only if DOS leaves
IoErr zero. This matches the observed bare-name miss and preserves specific
last-provider errors.

The supplied resident qualification adds that exact bare-name miss and updates
the exhausted-provider miss vectors. It passes 93 invocations per CPU on
68000/020/040 (279 total), with no shared-image writes or fixture leaks:
`artifacts/version-wb31-native-76d9914aa0f840b6a26a5fa43e7bc723/qualification.json`.
The current 68000 HUNK is 14,568 bytes with SHA-256
`6e3092af7a356097bd3929c8ba0a93e65e1260d288372a42cb2fa4096429c527`.

The original/candidate guest comparisons now match all observed fields for
three missing-provider forms: bare-name
`C:Version missing-device-copper-test.device`, explicit `LIBS:missing.library`,
and explicit `DEVS:missing.device`. Each prints
`object not found\nC:Version failed returncode 20\n`, returns 20 and leaves
caller post-System IoErr 205. Regression pairs also remain exact for
`LIBS:version.library`, `DEVS:clipboard.device`, and the unqualified
`clipboard.device` HUNK fallback. Receipts:

- `artifacts/workbench31-guest-command-version-bare-miss-candidate-fix-20260927-v1/effect-comparison.json`
- `artifacts/workbench31-guest-command-version-liblist-regress-candidate-20260927-v1/effect-comparison.json`
- `artifacts/workbench31-guest-command-version-devs-explicit-regress-candidate-20260927-v1/effect-comparison.json`
- `artifacts/workbench31-guest-command-version-clipboard-basename-regress-candidate-20260927-v1/effect-comparison.json`
- `artifacts/workbench31-guest-command-version-liblist-miss-candidate-20260927-v1/effect-comparison.json`
- `artifacts/workbench31-guest-command-version-devs-miss-candidate-20260927-v1/effect-comparison.json`

This closes three bounded provider error cases and retains three bounded
provider success cases. Other Version lookup failures, command-wide and MorphOS
parity, original PURE classification, lifecycle, reference closure, rights and
package admission remain open; shipping remains 0/200 commands and 0/246 profiles.

## 2026-09-27 implementation checkpoint - Workbench Break signed target

The original and current Workbench `Break` candidate were compared with
`C:Break -1`. Both emit the exact 55-byte diagnostic
`Process -1 does not exist\nC:Break failed returncode 20\n`, return 20, and
leave caller post-System IoErr 0; no task is signaled. This closes one signed
negative `PROCESS/A/N` error case alongside the existing zero and large
missing-target cases. Receipt:
`artifacts/workbench31-guest-command-break-negative-process-candidate-20260927-v1/effect-comparison.json`.

The 68000 candidate is the 3,452-byte HUNK with SHA-256
`cd094ef9e367a60cf3225cca2d0deef9fbeee604e41df6bc1f98b96ccc55acdc`, bound by
`artifacts/break-wb31-native-20260927-all-flags-v1/qualification.json`. Other
`Break` target/error cases and MorphOS behavior, command-wide reference and
guest coverage, PURE/resident lifecycle, media closure, rights, and package
admission remain open. Shipping remains 0/200 commands and 0/246 profiles.

## 2026-09-27 implementation checkpoint - Workbench ChangeTaskPri negative target

Original and candidate Workbench guests were compared with
`C:ChangeTaskPri 0 PROCESS -1`. Both emit the exact 63-byte diagnostic
`Process -1 does not exist\nC:ChangeTaskPri failed returncode 20\n`, return 20,
and leave caller post-System IoErr 0. No task is changed. Receipt:
`artifacts/workbench31-guest-command-changetaskpri-negative-process-candidate-20260927-v1/effect-comparison.json`.

The 68000 candidate is the 3,456-byte HUNK with SHA-256
`3751abac703732a3b86ebe10aa40bb8b8b9e39fc878b8544ce3e48ea45a8e6c1`, bound by
`artifacts/changetaskpri-wb31-native-20260927-range-fix-v1/qualification.json`.
This adds one signed negative process-ID case to CC18. Remaining `ChangeTaskPri`
targets and priority/parser cases, MorphOS PID semantics, full command and
profile coverage, PURE/resident lifecycle, media closure, rights and package
admission remain open; shipping stays 0/200 commands and 0/246 profiles.

## 2026-09-27 implementation checkpoint - Workbench Break signed upper bound

The original and current Workbench `Break` candidate were compared with
`C:Break 2147483647`, the largest positive signed LONG accepted by the captured
`PROCESS/A/N` grammar. Both emitted the exact 63-byte stream
`Process 2147483647 does not exist\nC:Break failed returncode 20\n`, returned
20 and left caller post-System IoErr at 0. Receipt:
`artifacts/workbench31-guest-command-break-max-process-candidate-20260927-v1/effect-comparison.json`.

This closes one upper-bound missing-target diagnostic only. Other valid CLI
targets, MorphOS PID numbering/reuse and task-liveness races, full command and
profile parity, PURE/resident lifecycle, missing Workbench media, source rights
and package admission remain open; shipping stays 0/200 commands and 0/246
profiles.

## 2026-09-27 implementation checkpoint - Workbench ChangeTaskPri signed upper bound

The original and current Workbench `ChangeTaskPri` candidate were compared
with `C:ChangeTaskPri 0 PROCESS 2147483647`, the largest positive signed LONG
accepted by the captured `PROCESS/K/N` grammar. Both emitted the exact 71-byte
stream
`Process 2147483647 does not exist\nC:ChangeTaskPri failed returncode 20\n`,
returned 20 and left caller post-System IoErr at 0. Receipt:
`artifacts/workbench31-guest-command-changetaskpri-max-process-current-candidate-20260927-v1/effect-comparison.json`.
The tested current 68000 candidate SHA-256 is
`14722d2ab7675ff3cda9507f282274edcdb0f92e42ec82b15dcfc228ea5ce7f0`, bound by
`artifacts/changetaskpri-wb31-native-20260927-range-precedence-v1/qualification.json`.

This closes one upper-bound missing-process diagnostic only. Other target
forms, MorphOS PID numbering/reuse and task-liveness races, full command and
profile parity, PURE/resident lifecycle, missing Workbench media, source rights
and package admission remain open; shipping stays 0/200 commands and 0/246
profiles.

## 2026-09-27 implementation checkpoint - CC18 Forbid-protected target lookup

The supplied-vector fixtures for MorphOS and Workbench `Break` and
`ChangeTaskPri` now fail if classic `FindCliProc` runs outside an active Exec
`Forbid` region. Existing callbacks also require protection for MorphOS
`FindTaskByPID`, Break port lookup and `Signal`, and `SetTaskPri`. At that
checkpoint, the 68000/020/040 HUNKs passed 13/24 Break and 12/13 ChangeTaskPri
vectors per profile and CPU: 12 fixture/CPU cases and 186 native invocations
total. Receipt:
`artifacts/cc18-forbid-lookup-regression-20260927-v1/qualification.json`.

The refreshed MorphOS Break receipt now passes 32 vectors per CPU, including
all signal masks and PROCESS/PORT precedence, at
`artifacts/break-morphos-native-20260927-mask-precedence-v1/qualification.json`.
Workbench Break was rerun at 24 vectors per CPU in
`artifacts/break-wb31-native-20260927-morphos-matrix-regression-v1/qualification.json`.
Those current Break receipts retain the Forbid-required lookup callbacks. The
older aggregate remains tied to its captured HUNK identities and is not the
current Break vector count.

This makes the race-protective lookup boundary regression-testable; it does not
simulate concurrent task death or establish PPC guest semantics. MorphOS PID
numbering/reuse and real liveness races, original guest parity, PURE/resident
lifecycle, missing media, rights and package admission remain open. Shipping
stays 0/200 commands and 0/246 profiles.

## 2026-09-27 implementation checkpoint - Workbench Which regular-file lookup

The original Workbench `Which` command and the current 4,932-byte resident
candidate were compared in separate disposable guests for
`C:Which S/Startup-Sequence`. Both emitted the exact 32-byte line
`Workbench3.1:S/Startup-Sequence\n`, returned 0 and left caller post-System
IoErr at 0. The exact pair is recorded in
`artifacts/workbench31-guest-command-which-startup-file-candidate-20260927-v1/effect-comparison.json`;
the original record is at
`artifacts/workbench31-guest-command-which-startup-file-original-20260927-v1/probe-analysis.json`.
The candidate identity is SHA-256
`be283d619f183267e4b1b25f7d2ecb280815a373e4960550b66fc45a79c5d189`.

This guest establishes one regular file in a subdirectory and its output path.
The diagnostic derivative edits `S/Startup-Sequence` to insert the probe, so
the observation does not establish original file-content or metadata parity.
It is a separate 33rd bounded Workbench `Which` pair, not part of the existing
32-case aggregate. Other command/profile behavior, original PURE/resident
lifecycle, remaining Workbench media, rights and package admission stay open;
shipping remains 0/200 command identities and 0/246 profile rows.

## 2026-09-27 implementation checkpoint - Workbench Which directory filtering

The current Workbench candidate now rejects directory lookup results using
DOS `Examine`, while retaining assign roots such as `C:` and regular files.
Fresh comparisons against the original Workbench command match for `C:Which S`
(empty output, return 5, caller IoErr 205), `C:Which S/` (empty output, return
5, caller IoErr 0), `C:Which S/Startup-Sequence` (exact path line, return 0,
caller IoErr 0), and `C:Which C:` (exact `Workbench3.1:C\n`, return 0, caller
IoErr 0), and `C:Which SYS:` (exact `Workbench3.1:\n`, return 0, caller
IoErr 0), `C:Which SYS:S` (empty output, return 5, caller IoErr 0), and
`C:Which SYS:S/Startup-Sequence` (the same exact file path line, return 0,
caller IoErr 0), and `C:Which SYS:S/..` (empty output, return 5, caller IoErr
205). Receipts are respectively:

- `artifacts/workbench31-guest-command-which-subdir-directory-filter-candidate-20260927-v1/effect-comparison.json`
- `artifacts/which-explicit-subdir-directory-filter-candidate-20260927-v1/effect-comparison.json`
- `artifacts/which-startup-file-directory-filter-candidate-20260927-v1/effect-comparison.json`
- `artifacts/which-c-directory-filter-candidate-20260927-v1/effect-comparison.json`
- `artifacts/workbench31-guest-command-which-sys-assign-candidate-20260927-v1/effect-comparison.json`
- `artifacts/workbench31-guest-command-which-sys-subdir-candidate-20260927-v1/effect-comparison.json`
- `artifacts/workbench31-guest-command-which-sys-startup-file-candidate-20260927-v1/effect-comparison.json`
- `artifacts/workbench31-guest-command-which-sys-parent-candidate-20260927-v1/effect-comparison.json`

The candidate is a 5,316-byte resident 68000 HUNK,
`3c7a61a5d84b313dc91874360ba3caa26e7f284c189c8b1c985753a30af21f57`; native
qualification passes 31 vectors per CPU on 68000/020/040 with no shared image
writes at `artifacts/which-wb31-native-20260927-sys-parent-v10/qualification.json`.
The existing 32-case aggregate remains unchanged; these are eight separate
bounded Workbench pairs. Old directory-positive captures remain as
negative controls. MorphOS code was not changed. This does not close other
`Which` categories, full command behavior, pure/resident lifecycle or any of
the 200-command/246-profile shipping gates.

## 2026-09-27 implementation checkpoint - MorphOS Which alias provider

The MorphOS Which candidate no longer rejects its `ALIAS` and `NOALIAS`
switches with `ERROR_NOT_IMPLEMENTED`. It now calls public DOS
`FindVar(file, LocalVariableType.Alias)`, searches an exact-name alias before
resident/filesystem results by default, lets `NOALIAS` skip that lookup, and
restricts `ALIAS` to alias lookup. The current candidate output is
`ALIAS <name>`; the ordering and format are not present in the available
MorphOS command documentation and remain unverified.

Resident native qualification passes 20 supplied vectors per CPU on
68000/020/040, including alias-only hit/miss, default alias hit, `NOALIAS`
suppression, file-path fall-through, and interleaving:
`artifacts/which-morphos-native-20260927-findvar-v2/qualification.json`.
The official MorphOS DOS prototype reference and MorphOS Which option page are
linked from the [Which contract](../docs/Commands/Workbench31MorphOS320/contracts/Which.md).

No MorphOS guest comparison or exact alias result/status capture is available.
This is a candidate implementation slice only; command grammar/output,
conflicting switches, pure/resident lifecycle, package placement and all
required profile gates remain open.

## 2026-09-27 implementation checkpoint - Workbench Which rebuild identity

The MorphOS alias-provider edit changed the shared source build identity. The
Workbench entry was requalified for 31 supplied vectors per CPU on resident
68000/020/040 at
`artifacts/which-wb31-native-20260927-findvar-regression-v1/qualification.json`.
The 6,308-byte 68000 HUNK SHA-256 is
`cb7a3a3aa26e1d12381da5280f7281265ad5b0e5b52648f2f8d94c8918ce7294`. Existing
Workbench guest comparisons remain tied to earlier HUNK identities except for
one fresh `C:Which SYS:S/..` comparison. The current HUNK matches empty output,
return 5 and caller IoErr 205 at
`artifacts/workbench31-guest-command-which-crossprofile-current-candidate-20260927-v1/effect-comparison.json`.
This remains one bounded parity case, not a refreshed matrix, and closes no
shipping gate.

## 2026-09-27 implementation checkpoint - MorphOS Which option matrix

The MorphOS candidate now passes 85 supplied vectors per CPU on resident
68000/020/040 at
`artifacts/which-morphos-native-20260927-switch-matrix-v3/qualification.json`.
The matrix covers all 32 combinations of `NOALIAS`, `ALIAS`, `NORES`, `RES`,
and `ALL`, with candidate found/missing inputs for each, plus alias-only miss
without fallthrough. The fixture model now treats `RES` as suppressing alias
lookup unless `ALIAS` selects aliases only. The shared Workbench entry passes
31 regression vectors per CPU at
`artifacts/which-wb31-native-20260927-switchmatrix-regression-v1/qualification.json`.

These supplied vectors test current candidate assumptions; they do not verify
MorphOS behavior. `ALIAS <name>` formatting, conflicting switches, alias and
resident/path precedence, exact grammar/result/IoErr, original guest parity,
PURE/resident lifecycle and package admission remain open.

## 2026-09-27 implementation checkpoint - CC11 Type I/O failures

MorphOS Type's supplied resident matrix now includes DOS Read failure and a
Write error after a positive short write. The fixture verifies the source-
shaped error diagnostic, RETURN_ERROR, selected IoErr, partial output, and
matcher/input-handle cleanup. The three-CPU receipt passes sixteen invocations
per CPU at
`artifacts/cc11-type-morphos-native-20260927-io-failures-v1/qualification.json`.
The Workbench five-slot syntax candidate passes seventeen invocations per CPU
at
`artifacts/cc11-type-wb31-native-20260927-io-failures-v1/qualification.json`;
these failure outcomes are candidate assumptions, not Workbench reference
evidence. Neither receipt closes original parser/handler behavior, output
parity, packed/source correspondence, PURE/resident lifecycle, licensing, or
package admission.

## 2026-09-27 implementation checkpoint - CC11 Search per-file failures

The hash-bound MorphOS 3.20 Search source treats Open, Read, and Seek failures
for an individual file as misses, continues matcher traversal, and clears
IoErr after successful completion. Recursive dangling soft links are warned
about and skipped. The MorphOS resident body now models those outcomes and
passes 46 supplied vectors per CPU on 68000/020/040 at
`artifacts/cc11-search-morphos-native-20260927-source-file-errors-v1/qualification.json`.
The Workbench syntax candidate retains separate behavior and passes 19
regression vectors per CPU at
`artifacts/cc11-search-wb31-native-20260927-source-error-regression-v1/qualification.json`.
Both receipts report 43 reachable methods without image writes or fixture
leaks. Workbench outcomes remain candidate assumptions. Neither profile has
yet established original guest parity, packed correspondence, PURE/resident
lifecycle, licensing, or package admission; the command-scope closure rows
remain open.

## 2026-09-27 implementation checkpoint - CC18 MorphOS Break option matrix

The released MorphOS 50.6 `Break` source confirms that nonzero PROCESS takes
precedence over PORT, while an absent or zero PROCESS falls through to PORT
lookup. If the zero-valued process fallback and port lookup both fail, the
source diagnoses PROCESS because the parsed PROCESS argument is still present.
The resident fixture now covers that precedence and all sixteen C/D/E/F
combinations. Its current 68000/020/040 receipt passes 32 vectors per CPU with
16 reachable methods and no shared-image writes or leaks at
`artifacts/break-morphos-native-20260927-mask-precedence-v1/qualification.json`.
Workbench Break was requalified for 24 vectors per CPU after the shared fixture
change at
`artifacts/break-wb31-native-20260927-pid-version-floor-regression-v1/qualification.json`;
its 68000 HUNK hash remains unchanged from the guest-effect receipts.
This advances the MorphOS source-backed behavioral matrix only; original guest
effect parity, exact PID behavior, target races, PURE/resident lifecycle and
package gates remain open.

## 2026-09-27 implementation checkpoint - CC18 ChangeTaskPri PID version floor

Released MorphOS 3.20 `ChangeTaskPri` source confirms the ReadArgs template
`PRI=PRIORITY/A/N,PROCESS/K/N` and gates PID fallback on Exec 50.45 or newer,
including major versions above 50. The supplied-Exec fixture now tests both
sides of that boundary: Exec 50.44 must not call `FindTaskByPID`, even when
the fixture can return a PID task, while Exec 51.0 may use the fallback. The
MorphOS resident receipt passes 14 vectors per CPU on 68000/020/040 with 16
reachable methods and no managed allocation sites or unsupported runtime
features/helpers in
`artifacts/changetaskpri-morphos-native-20260927-pid-version-floor-v1/qualification.json`.
The Workbench profile was also requalified after the shared fixture update and
passes 13 vectors per CPU at
`artifacts/changetaskpri-wb31-native-20260927-current-regression-v1/qualification.json`.
This is source-shaped native coverage, not original MorphOS guest proof. Exact
PID numbering/reuse, task liveness/races, broad profile parity, PURE/resident
lifecycle and proper-image package admission remain open.

## 2026-09-27 implementation checkpoint - CC18 MorphOS current-task PID identity

The official MorphOS Exec SDK documents `FindTaskByPID(0)` as a current-task
selector, separately from `TASKINFOTYPE_PID`, which returns a unique task ID.
CopperStart had returned zero for the current task's PID attribute. Its
provider now returns the live guest task identity and retains zero only as the
lookup selector. The focused provider suite passes all ten tests, including
lookup through both values. This corrects one PID API distinction but does not
establish exact MorphOS numeric assignment or reuse. Original guest PID
behavior, target lifetime stress, complete command parity, PURE/resident
lifecycle and package admission remain open. See the
[MorphOS Exec SDK reference](https://morphos-team.net/sdk/exec.html).

## 2026-09-27 implementation checkpoint - CC18 MorphOS Break PID version floor

Released MorphOS 50.6 `Break` source gates `FindTaskByPID` on Exec 50.45 or
newer, including major versions above 50. The supplied fixture now tests both
sides of that boundary: at Exec 50.44 it forbids the fallback even when the
fixture can return a PID task, while Exec 51.0 may use the fallback. The
MorphOS resident receipt passes 34 vectors per CPU on 68000/020/040 with 16
reachable methods and no managed allocation sites or unsupported runtime
features/helpers at
`artifacts/break-morphos-native-20260927-pid-version-floor-v1/qualification.json`.
This is source-shaped native coverage only; original MorphOS PID numbering and
reuse, target liveness/races, full guest parity, PURE/resident lifecycle and
package admission remain open.

## 2026-09-27 implementation checkpoint - CC18 extended ReadArgs help

Released MorphOS 3.20 `Break` and `ChangeTaskPri` sources allocate DOS_RDARGS,
set `RDA_ExtHelp`, and pass it to `ReadArgs`. Their isolated MorphOS argument
helper now preserves that question-mark help behavior and returns the source's
RETURN_FAIL level for parser failures. Release resident qualifications pass
34 Break and 14 ChangeTaskPri supplied vectors per CPU on 68000/020/040, with
the corresponding MorphOS extended-help fixture checks and no managed runtime
features, helpers, external targets, exception regions, or fatal sites. The
separate Workbench 3.1 bodies retain their original null-RDArgs behavior and
pass Release regressions with 24 Break and 13 ChangeTaskPri vectors per CPU;
all six profile/CPU results report no leaks or shared-image writes. Receipts:
`artifacts/cc18-break-extended-help-20260927-v6/`,
`artifacts/cc18-changetaskpri-extended-help-20260927-v3/`,
`artifacts/cc18-break-wb31-extended-help-regression-20260927-v3/`, and
`artifacts/cc18-changetaskpri-wb31-extended-help-regression-20260927-v3/`.
This closes a bounded source-derived parser/help mismatch only. It does not
establish MorphOS guest parity, exact PID sequence/reuse or liveness, full
command behavior, PURE/resident lifecycle, source-rights clearance, or package
admission; shipping remains 0/200 commands and 0/246 profiles.

## 2026-09-27 implementation checkpoint - CC18 open-command contract discovery

The MorphOS Library now supplies documented syntax leads for the remaining
CC18 commands without resident candidates: `LoadLib` uses
`PATH/A/M,VERBOSE/S`; `FlushLib` uses `LIBRARY/A/M,ONCE/S,QUIET/S`; and `iKill`
accepts an optional `TASKNAME`, entering crosshair selection when omitted.
The new per-command contract files preserve these leads and enumerate the
original 3.20 HUNK, guest, diagnostics/result/IoErr, side-effect, lifecycle,
rights, and package checks still required. The documentation is not being
treated as exact guest proof: the local released-source extraction has no
source for these three commands. Their original packed binaries are available
in the selected ISO outside the repository and hash-bound in the generated
inventory, but their code/help and guest behavior remain undecoded. The media
inventory was regenerated and `extract`, `verify`, and `verify-media` all pass
against the current plan and private media. This creates a concrete next
implementation tranche while keeping CC18 and the full 0/200-command,
0/246-profile shipping counts open.

## 2026-09-28 implementation checkpoint - CC10 Workbench Eval leading-zero parity

Three additional original/candidate Workbench guest pairs match exactly for
`C:Eval 08`, `C:Eval 08+1`, and `C:Eval 09+1`. Each emits bytes `30 0A`, returns
0, and leaves caller post-System IoErr at 0. The symbol-free candidate HUNK is
bound to SHA-256
`5d51031ab980e334e5cd2064974d7ed9eeeabbf524a2558bdb2f7a09ce51f74e`.
Together with the previously recorded cases, 21 distinct bounded Workbench Eval
invocation cases now match across two symbols-off HUNK identities (23 passing receipts include a duplicate `08 + 1` capture and a `2+` comparison on both HUNK identities). This adds
captured-case evidence only: the complete evaluator,
ReadArgs behavior, diagnostics, PURE/resident lifecycle, licensing, package
admission, and all shipping gates remain open; shipping remains 0/200 commands
and 0/246 profiles.
