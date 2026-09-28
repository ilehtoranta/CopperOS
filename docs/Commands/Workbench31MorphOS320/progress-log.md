# Workbench 3.1 and MorphOS 3.20 command progress log

## 2026-09-27 - Workbench ChangeTaskPri keyword-alias guest parity

Original and current candidate guests now match for
`C:ChangeTaskPri PRIORITY=42 PROCESS @SELF@`. This confirms the captured
Workbench command accepts the full `PRIORITY=` alias and equals form, changes
CLI task 1 from priority 0 to 42, emits no output, returns 0, and leaves caller
post-System IoErr at 0. The effect-aware comparison is
`artifacts/workbench31-guest-command-changetaskpri-priority-alias-candidate-20260927-v1/effect-comparison.json`.
The separate `C:ChangeTaskPri 42 PROCESS=@SELF@` pair also matches exactly and
confirms the `PROCESS/K/N` equals form; it has the same 0-to-42 task effect,
empty output, return 0 and caller IoErr 0. Receipt:
`artifacts/workbench31-guest-command-changetaskpri-process-equals-candidate-20260927-v1/effect-comparison.json`.
It uses the 3,452-byte current 68000 candidate with SHA-256
`14722d2ab7675ff3cda9507f282274edcdb0f92e42ec82b15dcfc228ea5ce7f0`, with
the original command metadata preserved. This is one bounded option-form
priority/target pair; other keyword/positional combinations, target races,
MorphOS PID parity, resident/PURE lifecycle, licensing and package admission
remain open.

The same original/candidate route now matches `C:ChangeTaskPri nope`: both
produce `bad number\nC:ChangeTaskPri failed returncode 20\n` (48 bytes),
return 20 and leave caller post-System IoErr at 115. The comparison is
`artifacts/workbench31-guest-command-changetaskpri-priority-nonnumeric-candidate-20260927-v1/effect-comparison.json`.
It exercises the DOS numeric parse failure before target lookup or mutation;
other overflow and parser/error combinations remain open.

Two oversized priority strings, `2147483648` and `-2147483649`, also match the
original's exact 74-byte range diagnostic, return 20 and caller post-System
IoErr 0. Their effect comparisons are
`artifacts/workbench31-guest-command-changetaskpri-priority-overflow-positive-candidate-20260927-v1/effect-comparison.json`
and
`artifacts/workbench31-guest-command-changetaskpri-priority-overflow-negative-candidate-20260927-v1/effect-comparison.json`.
This records only those two oversized strings; it does not claim a general
numeric-conversion rule.

The minimum signed PROCESS target, `C:ChangeTaskPri 0 PROCESS=-2147483648`,
also matches the original's exact 72-byte missing-process diagnostic, return
20 and caller post-System IoErr 0. Receipt:
`artifacts/workbench31-guest-command-changetaskpri-process-min-signed-candidate-20260927-v1/effect-comparison.json`.
This closes that numeric target boundary only; other target identities,
MorphOS PID semantics and target-race behavior remain open.

## 2026-09-27 - MorphOS TaskList PID_CLI provider

TaskList requests MorphOS `TASKINFOTYPE_PID_CLI` (`0x24`) for each row, but
the shared CopperStart Exec provider had no matching selector and returned
failure, causing the command's fallback PID field to be zero. Added the
selector to the SDK and provider. A Process with `pr_CLI` now reports
`pr_TaskNum`; non-CLI tasks with a valid ETask extension report its native
`UniqueID`. The official MorphOS SDK header binds `Task.tc_ETask` at byte 34,
`TF_ETASK` to bit 3, and `ETask.UniqueID` at byte 24; `ExecBase.ex_TaskID` is
at byte 576. MorphOS
`NewCreateTaskA` now allocates an 86-byte ETask in the existing owned block,
initializes its child list and assigns a nonzero ID from that ExecBase counter
under `Forbid`/`Permit`. Reap frees it with its task. Other direct Exec task
paths without ETask still use the task-address fallback. DOS-owned Process
allocation now checks the Exec library version, appends an ETask before the
stack for version 50 or later, and assigns the native ID. The version-40
Workbench path keeps the existing Process and stack layout.
`FindTaskByPID` accepts native IDs and live CLI numbers, including CLI-number
reuse after task removal. Provider and compiled production-vector tests pass
14/14, including native ETask-ID lookup and direct installed-vector execution.
The refreshed DOS child/publication/lifecycle regression filter passes 42 cases,
including MorphOS Process ETask placement and PID lookup plus the unchanged
version-40 layout.
The refreshed 68000/020/040 resident qualification passes 54 supplied vectors
per CPU with 69 reachable methods,
no allocations/helpers/external targets/exceptions/fatal sites, and no leaks or
shared-image writes. Receipt:
`artifacts/tasklist-morphos-native-pid-cli-20260927-v1/qualification.json`.
The refreshed receipt at
`artifacts/tasklist-morphos-native-pid-cli-20260927-v3/qualification.json`
requalifies the same TaskList entry after the shared task-allocation change:
54 supplied vectors per CPU on 68000/020/040, 69 reachable methods, and no
managed allocations, runtime helpers/features, external targets, exceptions,
fatal sites, leaks, or shared-image writes.
Original guest/provider parity, ETask initialization for other standard Exec
task paths, MorphOS guest ID-sequence parity, and concurrent task-death
semantics remain open.

## 2026-09-27 - MorphOS DOS-owned Process ETask identity

DOS-owned Process allocation now reads the Exec library version before choosing
its layout. Version 50 and later reserves an inline 86-byte ETask after the
Process body and before the stack, stores its pointer in `tc_ETask`, initializes
the parent and child list, and assigns a nonzero ID from `ex_TaskID` under
`Forbid`/`Permit`. Version 40 keeps the prior allocation and stack boundary, so
the MorphOS extension does not overwrite classic Task trap fields.

The focused provider/production-vector and DOS process-publication/lifecycle
filters pass 14 and 42 tests respectively. Coverage proves the new Process ID
resolves through `FindTaskByPID` and that release owns the inline extension.
It remains unit evidence: other standard Exec task paths, actual MorphOS guest
ID sequence/reuse, concurrent task death, and original TaskList parity remain
open.

## 2026-09-27 - MorphOS ETask validity flag

The official MorphOS 3.20 `exec/tasks.h` defines `Task.tc_ETask` in the former
trap-allocation words and says the pointer is valid when `TF_ETASK` is set
(bit 3). The shared ETask initializer now preserves existing Task flags while
setting this bit for both `NewCreateTaskA` and DOS-owned MorphOS Processes.
Focused tests assert the flag on both creation paths and confirm the Workbench
version-40 Process path leaves the bit clear. The SDK capture now records the
flag value alongside the structure offsets. Guest-side MorphOS creation and
reaping semantics remain open.

The PID provider now reads `tc_ETask` only when `TF_ETASK` is set. A regression
case supplies a nonzero stale pointer with the flag clear and proves that
lookup uses the portable task-address fallback instead of reading the
extension ID. The focused MorphOS provider and process-creation tests pass.

TaskList's 68000/020/040 resident qualification was refreshed after this shared
provider change. It passes 54 supplied vectors per CPU with 69 reachable
methods and no shared-image writes or fixture leaks at
`artifacts/tasklist-morphos-native-pid-cli-20260927-v4/qualification.json`.
The HUNK hashes match the prior receipt because the command binary itself did
not change; the receipt confirms its resident vector suite still passes.

The compiled Exec 68000 parity root now exercises both a valid ETask PID and
the task-address fallback after clearing `TF_ETASK`; host and guest memory
traces match in
`MorphOsExtendedProfileMatchesMc68000`.

## 2026-09-24 - MorphOS TaskList bounded feature matrix

The current MorphOS `TaskList` candidate combines the source-shaped task-list
snapshot, public `NewGetTaskAttrsA` task fields, six `NewGetSystemAttrsA`
boundaries, `Forbid`/`Permit` protection, and growing `AllocVec` storage. Its
resident entry includes live A7 for current-task `VERBOSE`, saved PPC
backchain `STACKTRACE` with `STACKLEVEL`, `SegTracker` symbol lookup,
`INTERNAL` ABOX/task-exit labels, PPC `REGDUMP`, and source-order `REGCHECK`
classification. The 54-vector fixture includes a 100-task buffer growth/retry,
the running-task active-stack bounds check, and no shared-image writes.

Receipt `artifacts/tasklist-morphos-native-live-a7-v3/qualification.json`
passes 54 invocations per CPU on 68000/020/040 (162 total). Original MorphOS
guest output/provider parity, complete task population, exact package
correspondence, PURE/resident lifecycle, licensing, and package admission
remain open.

## 2026-09-24 - Status qualification export closure

The local compiler CLI defaults to including every `M68kExport` in the shared
native-root assembly. The Status qualifier had not
selected the standalone-command export mode, so MorphOS Info's unrelated
`FormatDatePutChar` callback was included as a second root and the one-root
gate rejected the refreshed HUNK. Both MorphOS and Workbench Status qualifiers
now pass `--exports none`. This reproduces the previous bounded command images
and keeps unrelated entry exports out of these standalone binaries.

The refreshed resident receipts pass eighteen MorphOS and fifteen Workbench
vectors per CPU on 68000/020/040, with no runtime features/helpers, external
targets, exceptions, fatal sites, leaks or shared-image writes. HUNK identities
match the preceding receipts. The updated receipts are now superseded by the
case-insensitive `COM` slice recorded below.

## 2026-09-24 - Status case-insensitive COM matching

MorphOS `c/status/status.c` compares the requested `COM=COMMAND/K` text with
the BSTR command name using `strnicmp`. The legacy-list matcher now folds ASCII
letter case before comparing the source-length-bounded bytes. A mixed-case
`COM=rUn` fixture confirms that command `Run` is selected; Workbench uses the
same legacy matcher.

Both standalone three-CPU resident receipts pass: MorphOS covers nineteen
vectors and Workbench sixteen vectors per CPU on 68000/020/040. The fixture
checks repeated/interleaved callers and zero shared-image writes; remaining
guest parity, exact Workbench behavior, complete provider population,
PURE/resident lifecycle, licensing, package admission and differential gates
remain open. Receipts:
`artifacts/status-morphos-native-20260924-com-casefold-v1/qualification.json`
and `artifacts/status-wb31-native-20260924-com-casefold-v1/qualification.json`.

## 2026-09-23 - MorphOS ChangeTaskPri error-stream routing

The inspected MorphOS source sends a missing-process diagnostic to DOS
`Output()` unless the current process has a non-null `pr_CES`, in which case
that stream takes precedence. `NativeMorphOSChangeTaskPriCommand` now reads the
current process's `DosLayout.Process.CurrentError` after releasing the task
list protection and applies that fallback order. The supplied-vector fixture
checks both the ordinary `Output()` path and a distinct redirected error BPTR.

The refreshed resident receipt
`artifacts/changetaskpri-morphos-native-20260923-error-stream-v2/qualification.json`
passes twelve vectors per CPU on 68000/020/040, with thirteen reachable
methods, no runtime helpers/features, external targets, exceptions, fatal
sites, leaks or shared-image writes. The first qualification attempt is
retained at `artifacts/changetaskpri-morphos-native-20260923-error-stream-v1/`;
its script stopped on the stale expected method count after the report showed
the candidate remained compatible. PID numbering/reuse, real guest output and
priority effects, lifecycle/PURE, packaging, and differential evidence remain
open.

The Workbench 3.1 profile was rebuilt and rerun after the shared fixture
change. Its current HUNKs pass twelve vectors per CPU with eleven reachable
methods and no runtime features/helpers, external targets, exceptions, fatal
sites, leaks or shared-image writes. Receipt:
`artifacts/changetaskpri-wb31-native-20260923-error-stream-fixture-v2/qualification.json`.
The first compile exposed a stale ten-method check; its failed artifact remains
under `artifacts/changetaskpri-wb31-native-20260923-error-stream-fixture-v1/`.
No Workbench command behavior was inferred from the MorphOS-only `pr_CES`
fixture case.

## 2026-09-23 - Workbench Which internal switch matrix

The Workbench `Which` resident fixture now covers all eight `NORES`/`RES`/`ALL`
combinations for the captured internal-only `CD` case. It also builds a two-node
CLI path list, verifies that `ALL` reports the resident match first, and
preserves both identical path results. The MorphOS extended-syntax fixture
exercises the same path ordering. These vectors close fixture gaps against the
existing disposable Workbench captures; they do not replace original-guest
comparison.

Both resident HUNK qualifications pass 17 supplied DOS/Exec vectors per CPU
(51 each) on 68000/020/040, with two reachable methods, no runtime helpers,
external targets, exception regions or fatal sites, and no shared-image writes
or fixture leaks. Receipts:
`artifacts/which-wb31-native-20260923-all-order-v1/qualification.json` and
`artifacts/which-morphos-native-20260923-all-order-v1/qualification.json`.
The fixture still supplies post-ReadArgs values; full command parsing, other
lookup categories, original-guest parity, the required PURE lifecycle, rights
and package admission remain open. Next: close MorphOS `ALIAS`/`NOALIAS`
behavior against the shipped 3.20 command or an original guest before
replacing the candidate's fail-closed branch.

The alias provider audit found public `FindVar(name,type)` in the original
Workbench 3.1 `DOS_LIB.FD` and the MorphOS 3.20 SDK. The Workbench FD copy is
hash-bound in the contract; the MorphOS SDK exposes a public `LocalVar` result
and alias type. This establishes a candidate API, not command semantics. Next:
capture alias-hit/miss output, return/`IoErr`, option precedence, and alias
values from MorphOS before wiring this vector into `Which`.

## 2026-09-23 - CC00 media evidence regenerated and revalidated

The inventory JSON had a stale goal-plan hash, so I regenerated the two
derived evidence files from the selected private references with
`python tools/Commands/Inventory/inventory.py extract`. Both `verify-media` and
`verify` now pass against the current goal plan. The verified scope remains 200
external identities (70 Workbench C entries/58 identities and 188 MorphOS C
entries), with the complete MorphOS ISO checksum confirmed. The strict check
returns its documented incomplete-coverage status because six items remain
open: Workbench disks 3-6, machine variants, installed Workbench metadata,
installed MorphOS overlay, command runtime contracts, and Freeze/index/media
reconciliation. This closes the local evidence-refresh step only; it does not
close reference coverage or change the shipping count.

Evidence: `docs/Commands/Workbench31MorphOS320/command-inventory.json`,
`docs/Commands/Workbench31MorphOS320/media-evidence.json`; commands run:
`extract`, `verify-media`, `verify`, and `verify --strict-complete`.
PowerShell observed process status 2 for the strict check. Next: continue the
next independently evidenced command slice while keeping the six closure IDs
open; no missing disk or installed-system behavior is inferred.

## 2026-09-23 - MorphOS DOSList volume verbose rows

MorphOS `DOSList VOLUMES VERBOSE` now captures the source's volume stamp,
formats it with public DOS `DateToStr` while the volume list is locked, and
preserves both formatted and illegal-date output. It also captures optional
disk type and traverses the linked volume lock list in source order, resolving
each lock name before unlock. The strings and lock rows are rendered from the
invocation buffer after releasing the DOS read lock.

The refreshed resident 68000/020/040 receipt passes 34 supplied DOS-vector
invocations per CPU (102 total), including formatter failure, optional
attribute failures and one- and three-lock volume lists. All HUNKs have 34
reachable methods, no managed allocations/runtime features/helpers, external
targets, exception regions or fatal sites, and no fixture leaks or shared-image
writes. Receipt:
`artifacts/doslist-morphos-native-20260923-volume-v1/qualification.json`.
Original guest comparison, PURE/resident lifecycle, source reuse rights,
package admission and differential behavior remain open.

## 2026-09-23 - MorphOS DOSList assign target and lock-list rows

MorphOS `DOSList` now follows the captured AssignNode branches: it emits
deferred and null-target rows, conditionally resolves the assign lock, reports
the assign type under `VERBOSE`, and traverses linked assign locks in source
order. Lock names and FileLock detail are copied into invocation-owned storage
before unlocking, then rendered after the list lock is released.

The resident 68000/020/040 receipt passes 30 supplied DOS-vector invocations
per CPU (90 total), including deferred and null targets, optional lock-query
and lock-name failures, a mounted assign process, and a verbose linked lock
record. The HUNKs have 32 reachable methods, no managed allocations,
runtime features/helpers, external targets, exception regions or fatal sites,
and no fixture leaks or shared-image writes. Receipt:
`artifacts/doslist-morphos-native-20260923-assign-v1/qualification.json`.
Volume verbose details, original guest comparison, PURE/resident lifecycle,
source reuse rights, package admission and differential behavior remain open.

## 2026-09-23 - MorphOS DOSList device verbose rows

MorphOS `DOSList VERBOSE` now snapshots the source's device handler,
stack/priority/segment/global-vector, serial ID, startup-message, environment,
control and startup attributes through public `GetDosObjectAttrTagList`
queries while holding the matching DOS read lock. It copies returned strings
before unlocking, preserves the source output order, and omits rows whose
optional attribute query fails. The output uses `VPrintf` only after the lock
has been released.

The resident 68000/020/040 receipt passes 24 supplied DOS-vector invocations
per CPU (72 total), including a missing optional attribute; all HUNKs have 28
reachable methods, zero managed allocations/runtime features/helpers/external
targets/exception regions/fatal sites, and no fixture leaks or shared-image
writes. Receipt:
`artifacts/doslist-morphos-native-20260923-verbose-device-v1/qualification.json`.
Volume and assign verbose details, original guest comparison, PURE/resident
lifecycle, source reuse rights, package admission and differential behavior
remain open.

## 2026-09-23 - MorphOS DOSList public object attributes and mounted rows

MorphOS `DOSList` now obtains node names and message ports independently
through the public DOS `GetDosObjectAttrTagList` call. It follows the source
fallback when either attribute query fails, filters by the copied name, and
resolves mounted processes for embedded and signal message ports using
`Exec.TypeOfMem` and public task/message-port fields. Tests cover mounted and
unmounted rows, missing name/port attributes, and NAME filtering when the name
attribute is unavailable.

The resident 68000/020/040 qualification passes 23 supplied DOS-vector
invocations per CPU (69 total), with 19 reachable methods and no managed
allocations, runtime features/helpers, external targets, exception regions,
fatal sites, leaked resources or shared-image writes. Receipt:
`artifacts/doslist-morphos-native-20260923-object-attrs-v1/qualification.json`.
This closes only common names and mounted-state output; provider-specific
verbose rows, original guest comparison, PURE/resident lifecycle, source reuse
rights, package admission and differential behavior remain open.

## 2026-09-23 - Workbench and MorphOS List recursive ALL

Both resident `List` candidates now implement recursive `ALL` with public DOS
`AnchorPath` traversal: they set `APF_DODIR` for each discovered directory,
clear `APF_DIDDIR` on return, and suppress the directory-finished marker from
duplicate rows. The Workbench profile's shorter `ReadArgs` result array places
`ALL` in slot 15, while MorphOS uses slot 18; the shared implementation now
maps both correctly.

The fixture simulates a nested directory, a child file, the return marker, and
continued parent enumeration. Three-CPU resident qualification passes 31
MorphOS vectors per CPU and 32 Workbench vectors per CPU in
`artifacts/cc11-list-morphos-native-20260923-all-v1/qualification.json` and
`artifacts/cc11-list-wb31-native-20260923-all-v1/qualification.json`. This does
not establish original path rendering, link policy, exact ordering, or parity
against either guest. Sorting, owner fields, `LFORMAT`, PURE/resident lifecycle,
licensing, and packaging remain open.

## 2026-09-23 - Workbench and MorphOS List date filters and QUICK precedence

The bounded MorphOS and Workbench `List` candidates now parse `SINCE` and
`UPTO` through public DOS `StrToDate` and compare the resulting DOS day stamp
inclusively with each matched file's date. This is date-only filtering. Invalid
input takes the candidate's `ERROR_BAD_TEMPLATE` cleanup path. `QUICK` keeps
its names-only output when combined with `DATES` and avoids `DateToStr`, as
documented by the Workbench reference. Tests cover since-only, upto-only and
combined ranges, both inclusive bounds, and the `DATES QUICK` combination.

The resident 68000/020/040 receipts pass 31 MorphOS vectors per CPU and 32
Workbench vectors per CPU. They are
`artifacts/cc11-list-morphos-native-20260923-date-range-quick-v1/qualification.json`
and `artifacts/cc11-list-wb31-native-20260923-date-range-quick-v1/qualification.json`.
The fixture validates supplied DOS vectors and synthetic dates only; date
syntax, diagnostics and results still need comparison with each original OS.
At this earlier date-range checkpoint, recursive traversal was still open;
the later `List recursive ALL` entry above records its bounded implementation
and fixture qualification. Sorting, owner fields, `LFORMAT`, original-guest
parity, PURE/resident lifecycle, licensing and package admission remain open.

## 2026-09-23 - Workbench and MorphOS List NODATES rendering

The shared `List` row formatter now accepts `DATES` for its documented default
full-row output and implements `NODATES` by omitting both date and time columns.
That mode does not call DOS `DateToStr`, while ordinary full rows still convert
the current FIB stamp. At this earlier checkpoint, the explicit `DATES QUICK`
combination remained fail-closed; the later date-range/QUICK entry above
records the bounded names-only precedence implementation.

The resident fixture passes 26 MorphOS vectors per CPU and 27 Workbench vectors
per CPU on 68000/020/040. The receipts are
`artifacts/cc11-list-morphos-native-20260923-nodates-v1/qualification.json` and
`artifacts/cc11-list-wb31-native-20260923-nodates-v1/qualification.json`.
These supplied-vector checks verify output and DateToStr cadence only; original
guest parity, date filters, recursive traversal, PURE/resident lifecycle,
licensing, and package admission remain open.

## 2026-09-23 - MorphOS Search DOS64 Seek64

MorphOS Search now reads the extended `FileInfoBlock.Size64` fields when DOS
is at least 51.28 and uses `DOS.Seek64` when the file exceeds the classic
32-bit offset limit. The candidate seeks to offset zero before matching and
uses signed high/low relative offsets to rewind incomplete lines. It detects
the SDK's D0:D1 `-1` return pair and propagates `IoErr` without another read;
older providers and smaller files retain classic `Size`/`Seek` behavior.

The supplied three-CPU resident fixture passes 45 vectors per CPU (135 total),
including absolute/signed-relative requests and a synthetic DOS64 size/failure
case. During debugging, the generic vector gateway's normal D1 poison was
found to corrupt this pair-returning call; the fixture now preserves D1 only
for `Seek64`, keeping ordinary volatile-register checks unchanged. Receipt:
`artifacts/cc11-search-morphos-native-20260923-seek64-v12/qualification.json`.
This does not verify actual >2 GiB file handlers, real locale tables, original
guest output, packed correspondence, PURE/resident lifecycle, or packaging.

## 2026-09-23 - MorphOS Search source buffer and pre-scan

MorphOS `FindString` now follows the inspected 512 KiB `MEMF_ANY` buffer
algorithm: failed initial requests are halved; when the file exceeds the
buffer, an LF-only pre-scan finds the longest line, the buffer grows when
needed, and the match pass seeks back and rewinds incomplete trailing lines.
Fixtures cover partial pre-scan reads, allocation exhaustion, failed-first-
allocation recovery, incomplete-line rewind with a small halved buffer, CTRL-D
during the pre-scan, and long-line growth/reread with a 524,300-byte file. The
68000/020/040 resident receipt passes 43 invocations per CPU with 40 reachable
methods and no helper/runtime features, leaks, or image writes:
`artifacts/cc11-search-morphos-native-20260923-source-prepass-v9/qualification.json`.
The Workbench syntax candidate passes 19 vectors per CPU, including a matching
line over 8,192 bytes:
`artifacts/cc11-search-wb31-native-20260923-source-prepass-v1/qualification.json`.
At this earlier 43-vector checkpoint, the MorphOS `Seek64` path above 2 GiB
remained open because the candidate still used classic DOS `Seek`. The newer
DOS64 qualification at the top of this log supersedes that limitation. Real
large-file handler and memory-pressure behavior, original output, packed
correspondence, PURE/resident lifecycle, and package admission remain open.

## 2026-09-23 - MorphOS Search locale character classes

The inspected Search source declares `LOCALE_VERSION 38`, but the macro is
unused and the active entry opens locale.library v37. The MorphOS candidate
preserves v37, splits lines with `IsCntrl(locale, ch)` except for TAB, and
renders nonprintable bytes using `IsPrint(locale, ch)`. The resident body calls
those existing SDK bindings. The fixture checks a high-byte
control delimiter (0x85), a high-byte printable byte (0xe9), literal and
DOS-pattern context output, TAB preservation, and LF-only line numbering.
The refreshed resident receipt passes 37 invocations per CPU on 68000/020/040
with no shared-image writes or leaked resources:
`artifacts/cc11-search-morphos-native-20260923-locale-classes-v2/qualification.json`.
Workbench Search was rerun against the shared renderer and passes 18
invocations per CPU:
`artifacts/cc11-search-wb31-native-20260923-locale-safeguard-v2/qualification.json`.
Fixture-defined locale classes do not verify the real default-locale table or
original guest output. The 8,192-byte bound, guest parity, PURE/resident
lifecycle and package admission remain open.

## 2026-09-23 - MorphOS Search nested and sibling ALL traversal

Extended the supplied MorphOS Search DOS-vector fixture beyond one directory
descent. It now models a grandchild directory, two sibling branches, five
matched files and all nested/root `DidDirectory` exits. Each file is opened
through the lock for its containing directory; the fixture checks every
`NameFromLock`/`AddPart` result and the complete indentation/output sequence.
The refreshed resident HUNKs pass 21 invocations per CPU on 68000/020/040
without shared-image writes in
`artifacts/cc11-search-morphos-native-20260923-nested-v2/qualification.json`.
The shared runner now correctly requires the two declared profile roots while
the compiler's explicit `--entry` selects the HUNK under test. Workbench Search
was rerun and passes 18 vectors per CPU in
`artifacts/cc11-search-wb31-native-20260923-nested-check-v1/qualification.json`.
The fixture does not prove larger real filesystem traversal, soft-link policy,
packed correspondence, exact original guest output, PURE/resident lifecycle or
package admission.

## 2026-09-23 - MorphOS Info filter validation and result publication

MorphOS Info now validates each `DEVICES/M` pattern with the source's
128-byte `ParsePatternNoCase` buffer before allocating the snapshot or locking
the DOS list. Malformed patterns return `RETURN_ERROR`, retain `IoErr`, and
publish the source entry's final `PrintFault(IoErr(), NULL)` without touching
the list. The same outer fault publication now handles other non-OK body
results. Ctrl-C is polled during output after the current device row rather
than during the DOS-list snapshot, preserving the source-shaped partial output
and one final fault publication. The 68000/020/040 resident receipt passes 66
vectors per CPU with no leaked resources or shared-image writes:
`artifacts/cc11-info-morphos-native-20260923-pattern-validation-v3/qualification.json`.
Workbench Info was rerun and passes fourteen vectors per CPU in
`artifacts/cc11-info-wb31-native-20260923-pattern-v14/qualification.json`;
the combined test-root HUNK remains a fixture, not a shipping artifact.

## 2026-09-23 - MorphOS Info source ordering

The MorphOS Info snapshot now follows the source's case-insensitive
`utility.library` `Stricmp` insertion order and renders device records before
the volume section, independent of DOS-list traversal order. A reversed-list
fixture covers the order and output boundary. The refreshed resident HUNKs
pass 65 supplied vectors per CPU on 68000/020/040 with no leaked resources or
shared-image writes:
`artifacts/cc11-info-morphos-native-20260923-sort-v4/qualification.json`.
Workbench Info was also rerun and still passes fourteen vectors per CPU in
`artifacts/cc11-info-wb31-native-20260923-sort-v13/qualification.json`; its
combined test-root HUNK is not a shipping artifact. The inspected MorphOS
source compiles out `OpenCatalogA`/`GetCatalogStr`, so default labels are the
observed source behavior; packed-binary output, guest parity, PURE/resident
lifecycle and package admission remain open.

## 2026-09-23 - MorphOS Info locale dates, errors and verbose startup fallback

MorphOS Info now copies mounted volume dates under the DOS-list read lock,
uses `DateToStr`, and renders the complete ordered 3.20 `GetFSysStr` table,
including duplicate-ID precedence and unknown-type fallback. `DEVICES/M`
selects device rows with `VOLS`, volume-only execution returns the source's
`WARN`, and device rows render all three disk-state labels, filesystem type,
and the `NameFromLock` volume name with a copied-name fallback. Filesystem type
also honors the startup `DosEnvec` override when its table-size and unit guards
pass. The latest source comparison adds per-device Lock/Info fault handling:
WARN persists until a device row succeeds, `GOODONLY` suppresses the row fault
only, and the selected `IoErr` is retained. `VERBOSE` follows both `GetDevStr`
and BSTR-style `GetStartupStr` fallback paths. Volume output now queries
`info_datetime` with DOS `GetVar`, uses `locale.library` `FormatDate` through
an invocation-owned Hook when configured, and retains `DateToStr` as the
default path. Fresh resident 68000/020/040 qualification passes 64 supplied
vectors per CPU with no leaked guest resources or shared-image writes:
`artifacts/cc11-info-morphos-native-20260923-locale-datetime-v4/qualification.json`.
Workbench Info was rerun against the shared fixture and passes fourteen vectors
per CPU in
`artifacts/cc11-info-wb31-native-20260923-harness-v12/qualification.json`;
that combined test-root HUNK includes the MorphOS hook export and is not an
isolated Workbench artifact. The inspected source compiles catalog opening out;
packed-binary output confirmation, exact provider output, original guest
parity, PURE/resident lifecycle, packaging and shipping admission remain open.

## 2026-09-23 - MorphOS Info startup library ownership

The MorphOS Info resident entry now follows the inspected startup provider
order: it requires `utility.library` v37, then attempts optional
`locale.library` v38 and opens the default locale before entering the body.
If utility cannot open, it returns before `ReadArgs`; absent locale support does
not prevent Info from running. When locale.library opens, `CloseLocale` is
paired during cleanup even when `OpenLocale` returned null, matching the source
path. The refreshed 68000/020/040 receipt passes 21 supplied vectors per CPU,
including provider failures and cleanup, in
`artifacts/cc11-info-morphos-native-20260923-startup-libs-v1/qualification.json`.
This adds startup ownership evidence only; localized number/date/provider
output, original guest parity, PURE/resident lifecycle, and package admission
remain open.
The independent Workbench Info candidate was also rerun after the harness
change and retains its 14-vector-per-CPU result with unchanged 68000/020/040
HUNK identities in
`artifacts/cc11-info-wb31-native-20260923-harness-check-v1/qualification.json`.

## 2026-09-23 - MorphOS Search ALL AnchorPath traversal

Implemented the source-observed MorphOS `ALL` directory path through public
DOS `AnchorPath`: the body requests `DoDirectory`, consumes directory-exit
records, renders directory/file headings with source indentation, and resolves
matched paths using `NameFromLock` plus `AddPart`. The initial locale stage
opened locale.library v37 and used `ConvToUpper`; a later source-version audit
confirmed the active v37 floor (the `LOCALE_VERSION 38` macro is unused) and
added `IsCntrl`/`IsPrint`. This earlier resident receipt passes 20
supplied vectors per CPU on 68000/020/040, including a non-ASCII literal pair,
locale-open failure cleanup, and one directory entry/descent/file/exit path, in
`artifacts/cc11-search-morphos-native-20260923-locale-v4/qualification.json`.
Workbench Search is bound to a captured disk 2 binary and its eight-slot
template candidate; its separate DOS 36 resident entry passes 18 vectors per
CPU, including the bounded directory traversal candidate, in
`artifacts/cc11-search-wb31-native-20260923-locale-v2/qualification.json`.
Static strings do not prove Workbench runtime behavior. Deep/multiple trees,
MorphOS soft-link and dangling-link behavior, QUICK output, packed
correspondence, guest parity, PURE/resident lifecycle, and packaging remain
open.

## 2026-09-23 - CC11 Type, Search, and Dir resident requalification

Rebuilt current-source CC11 inspection entries on 68000/020/040. Workbench
Type passes 15 supplied vectors per CPU and MorphOS Type passes 13; Workbench
Search passes 18 and MorphOS Search 17; MorphOS Dir passes 15. Receipts:
`artifacts/cc11-type-wb31-native-20260922-v1/qualification.json`,
`artifacts/cc11-type-morphos-native-20260922-v1/qualification.json`,
`artifacts/cc11-search-wb31-native-20260922-v1/qualification.json`,
`artifacts/cc11-search-morphos-native-20260922-v1/qualification.json`, and
`artifacts/cc11-dir-morphos-native-20260922-v1/qualification.json`, plus
`artifacts/cc11-dir-wb31-native-20260922-v1/qualification.json` with 16
Workbench vectors per CPU. This is resident/static and supplied-vector
evidence only. Exact original output, complete recursion/interactive behavior,
packed correspondence, guest parity, PURE/resident admission, packaging, and
differential gates remain open.

## 2026-09-22 - CC10 PathPart, Which, and Quote resident requalification

Regenerated the current-source MorphOS CC10 resident receipts. PathPart passes
11 supplied vectors per CPU, MorphOS Which passes 16, and the bounded Quote
STR entry passes 12 on 68000/020/040. Receipts:
`artifacts/cc10-pathpart-native-20260922-v1/qualification.json`,
`artifacts/cc10-which-morphos-native-20260922-v1/qualification.json`, and
`artifacts/cc10-quote-native-20260922-v1/qualification.json`. All report
balanced fixture ownership and zero shared-image writes. This refreshes only
resident ABI/ownership evidence; exact shipped grammar and output, real DOS
parser/filesystem behavior, original guest parity, PURE/resident admission,
packaging, licensing, and differential gates remain open.

## 2026-09-22 - CC10 Eval resident requalification

Rebuilt both current-source Eval resident entries. MorphOS Eval passes 14
supplied vectors per CPU (42 total); the Workbench literal-template entry
passes 12 per CPU (36 total) on 68000/020/040. Receipts:
`artifacts/cc10-eval-morphos-native-20260922-v1/qualification.json` and
`artifacts/cc10-eval-wb31-native-20260922-v1/qualification.json`. Both use one
loaded image per CPU and report zero shared-image writes while retaining only
the audited `nullable-values` feature. Real DOS parsing/filesystem behavior,
original parity, PURE/resident admission, packaging, licensing, and
differential gates remain open.

## 2026-09-23 - Exe2Arc scanner dispatch owner

Added `Exe2ArcScannerSelection`, the allocation-free command-level dispatch
owner for the source scanner table. Explicit `TYPE` values route to exactly
one scanner; an unspecified type follows the recorded ZIP, ACE, RAR, CAB,
ARJ, LhA, LZH order. Only `NoMatch` falls through. Offset-zero detections,
I/O stops, and matched-but-not-positioned results remain terminal, and the
LhA fixed-prefix probe is explicitly rewound to the input start after earlier
forward scans. Five focused tests cover source-order precedence, explicit
selection, offset-zero termination, all-scanner no-match clearing, and the
initial-seek failure terminal path. The managed Commands suite now passes 704
tests, and the native project links and
builds this owner with zero warnings or errors. This is still selection
evidence: ReadArgs/input/FIB/buffer ownership, extraction, diagnostics,
cleanup, original guest parity, PURE/resident lifecycle, and packaging remain
open.

## 2026-09-23 - Exe2Arc result and cleanup policy owner

Added `Exe2ArcResultPolicy` as the command-level precedence owner. A completed
scan reports detection without claiming saved output; no-match, offset-zero,
unsafe-position and I/O statuses are terminal. A nonzero completed extraction
returns success and requests the source saved-byte warning, while zero-byte,
short and malformed-record outcomes request close-then-delete cleanup of the
selected output. The source-compatible ZIP Ctrl-C boundary retains partial
output with `ERROR_BREAK` and a nonzero saved-count result. Input, buffer and
pre-open output failures never
request deletion. Twelve focused cases cover these decisions and the native
project links the owner cleanly. The complete managed Commands suite is now
704 tests. This policy does not claim original diagnostic bytes or final
`IoErr` behavior; those remain guest/reference gates.

## 2026-09-23 - Exe2Arc MorphOS frontend composition

Added `NativeMorphOSExe2ArcCommand`, which composes the exact three-slot DOS
`ReadArgs` gate, invocation-owned input handle/FIB/103424-byte workspace,
source-order scanner selector, safe generated-name/output selector, ZIP record
extractor or exact-chunk payload copier, and close-then-delete cleanup policy.
The input is treated only as data; no LoadSeg/RunCommand path exists. The
native project builds this entry with zero warnings or errors. Diagnostics and
saved-byte reporting now use the bounded policy and emit the source saved-byte
warning, no-match/type-list, and output-open messages. Exact redirected bytes,
guest transcript parity and final IoErr remain open, so this is still a
frontend composition checkpoint rather than original-guest or shipping
qualification.

## 2026-09-22 - Exe2Arc resident frontend static HUNK

Added the source-linked `tests/Commands.Exe2ArcNativeRoot` resident entry and
the reproducible `tools/Commands/qualify_exe2arc_native_static.ps1` gate. The
final three HUNKs compile for 68000/020/040 at 27164/27656/27132 bytes; the receipt
is `artifacts/exe2arc-native-static-20260922-v8/qualification.json`. All three
reports are compatible with zero managed allocations, runtime features or
helpers, external native targets, exception regions and fatal fault sites,
and the managed Commands suite remains at 704 passes. This advances static
resident and bounded parser-boundary evidence only; guest/filesystem execution,
exact output and final `IoErr`, PURE/resident lifecycle, packaging and shipping
remain open. The supplied-vector fixture passes two parser/result-failure cases
per CPU with balanced DOS open/close, restored stack and expected result and
`IoErr` values; its receipts are the three `frontend-smoke-*-parser.json` files
under the same v8 artifact directory.

## 2026-09-22 - Exe2Arc ZIP Ctrl-C result boundary

Aligned the ZIP record owner with the released `ExtractZIP` loop: an optional
signal adapter polls Ctrl-C only at record boundaries, assigns `ERROR_BREAK`,
and returns the source-visible nonzero `filesize-start` count without forcing
failure or deleting partial output. The observation tags this signal result
separately from a captured `IoErr` so the frontend preserves the break error
through cleanup. Sixteen focused ZIP/policy tests pass; exact guest signal
timing, redirected transcript bytes and post-cleanup `IoErr` still require
original-reference capture.

## 2026-09-22 - Exe2Arc bounded source diagnostics

The MorphOS frontend now emits the released fixed diagnostics for parser-free
acquisition failures (input, FIB, `ExamineFH`, temporary buffer), initial
dispatch seek failure, output-open failure, and ZIP unknown-record/EOF paths.
It uses a null `PrintFault` header like the source and suppresses duplicate
fault output after an explicit message. The managed suite remains at 704
passes and the native project builds with zero warnings/errors. Redirected
stdout/stderr bytes, guest transcript parity and final post-cleanup `IoErr`
remain reference gates.

## 2026-09-23 - Exe2Arc ACE scanner/copy component extension

Extended the source-backed Exe2Arc component slice with the MorphOS source's
ACE path: `**ACE**` at candidate offset +7, strict `size > 14` admission,
thirteen-byte window overlap, offset-zero stop, relative candidate seek, final
absolute seek, EOF payload length and the shared exact-chunk copy path. Managed
coverage now includes the ACE header and scanner cases. Added the allocation-
free `Exe2ArcOutputName` guest-memory builder for safe final-component suffix
replacement, with bounded/unterminated-name tests. The isolated resident
native fixture passes 84 supplied-DOS invocations per CPU (252 total) on
68000/020/040. The receipt is
`artifacts/exe2arc-ace-native-20260923/qualification.json`, with HUNK bytes
6380/6428/6364. This is still component evidence: the ReadArgs/FIB/input
owner, naming and cleanup frontend, original guest differential, packed
correspondence, PURE/resident admission, and package gates remain open.

The same Exe2Arc frontend slice now has a borrowed output selector. It opens
literal `TO` first, then performs the two source-order `AddPart` operations in
the 512-byte fallback buffer only after that open fails; failures stop before
another open and no handle/path is closed or deleted by the component. Six
focused cases cover direct generated output, literal success, fallback success,
both AddPart failures, and invalid fallback storage.

The slice also has an allocation-free `TYPE/K` selector. It bounds the guest
string at 64 bytes and accepts only the source's exact case-insensitive
extension/display-name pairs (`zip`, `ace`, `rar`, `cab`/`cabinet`, `arj`,
`lha`, `lzh`/`amiga-lha`), with focused controls for unknown, prefix and
unterminated values. The Commands suite now passes 685 tests; the native
component project remains warning-free.

The frontend now also has a native `ReadArgs` ownership boundary for the exact
`FROM/A,TO,TYPE/K` template and three result slots. It retains the DOS parser
lease for the caller and leaves input, FIB, buffer, diagnostics and extraction
cleanup to later command-owner slices; the native project builds with zero
warnings or errors.

The source-backed scanner slice now includes ARJ. It validates the `60 ea`
marker, variable little-endian header length and reflected `edb88320` CRC,
rejecting an incomplete CRC tail before guest reads. Managed cases cover valid,
bad-CRC, later-candidate and truncated-tail paths; the resident supplied-DOS
receipt remains the earlier RAR4/CAB/ACE-only checkpoint.

The same scanner now includes LZH's source marker and scan-window byte-20
level predicate. Managed cases cover the paired candidate-byte/window-byte
behavior and high-level rejection; this remains host component evidence until
it is added to the resident supplied-DOS receipt.

The LhA HUNK/SFX path is now bounded as a fixed 100-byte probe with big-endian
start extraction and the source's repeated absolute seek ordering. Positive
short reads, out-of-file starts and final-seek failure are covered without
reading uninitialized scratch bytes; it remains host component evidence.

The ZIP detector now performs the source's backward EOCD scan and first-central
entry lookup, returning the prefix correction and rebased local-header start
for a later record-rewriter owner. Latest-EOCD stop ordering and wrapped or
incomplete central-tail rejection are covered; ZIP rewriting and extraction
remain open.

The ZIP slice now also has an allocation-free record rewriter. It recognizes
local, central and EOCD signatures, leaves local records unchanged, and adds
the scanner correction to the two source offsets only after bounded length,
mapping and overflow checks. Five focused tests cover all record kinds,
unknown/short records and overflow rejection.

The ZIP slice now composes those mutations with a streaming record extractor.
It reads exact local, central and EOCD tails, copies member/comment bytes via
the shared chunk copier, and stops on the EOCD comment. Two focused cases cover
rebased local/data/central/EOCD output and unknown-record rejection; diagnostics,
break polling and output cleanup remain command-owner work.

## 2026-09-23 - MorphOS Status source-aligned break boundary and identity

Bound `MorphOS/C/Status` at ISO extent `178472` (3,679 bytes, SHA-256
`9d5c3aca0a9f21d91f9ad4b0565ba5ac2fa4a964af07c9540ee58e6b7d24a168`) to the
5,709-byte released source in
`reference-captures/status-morphos-binary-audit-20260923.json`. The resident
body now polls `SetSignal(0, SIGBREAKF_CTRL_C)` before each CLI snapshot entry,
matching the source's no-partial-row boundary. The MorphOS fixture now also
supplies DOS 51.51 `QueryCLIDataTagList`/`FreeCLIData` provider vectors,
validates the tag ABI, and exercises provider-side process and command
filters; the 68000/020/040 receipt passes eighteen invocations per CPU. The
refreshed receipts are
`artifacts/status-morphos-native-20260923-provider-v4/qualification.json` and
`artifacts/status-wb31-native-20260923-provider-v2/qualification.json`.
Guest parity, complete provider task population, PURE/lifecycle, package and
differential gates remain open.

The Workbench `C/Status` identity is now bound in
`reference-captures/status-wb31-binary-audit-20260923.json` and the selected
disk-2 capture: 828 bytes, SHA-256
`fd7b386f103bafba80add97523991389880f9b6987f4534280abb61b8193580c`, version
37.2, and the candidate five-slot template. The disk capture records the
original protection word as zero, so no PURE requirement is inferred from this
media. Exact Workbench execution and installed placement remain open.

## 2026-09-23 - MorphOS Date packed identity closure

Added the hash-bound `MorphOS/C/Date` audit at ISO extent `175288` (3,748
bytes, SHA-256 `5419ee6b3f3cd9a18168c1eafe6d373f5b832d1fb9d0c9ec856f51a6c0597dac`)
and tied it to the 9,453-byte released source (SHA-256
`c63217b0383ad0861bdd167838dc03c22dab74555d96819fdc0f229b148d86ee`).
Inventory extraction, media verification and the 21-test inventory suite pass.
The existing bounded resident receipt remains adapter evidence; exact guest
diagnostics/IoErr, timer/provider behavior, PURE/resident lifecycle, package
and differential gates remain open.

## 2026-09-23 - MorphOS Avail diagnostics and packed identity

Aligned the MorphOS `Avail` body with the released source's diagnostic policy:
parser and output failures now call `PrintFault(IoErr(), "Avail")`, while the
multiple-selector warning remains diagnostic-free. The body also applies the
released Pegasos fake-chip `ExecBase.MaxLocMem` adjustment. The fixture checks
the parser fault ABI and fake-chip summary, and the refreshed resident receipts
pass twelve supplied vectors per CPU with no shared-image writes or leaked
resources: `artifacts/avail-morphos-native-20260923-fakechipp-v3/qualification.json`.

Added the hash-bound `MorphOS/C/Avail` audit at ISO extent `175208` (3,999
bytes, SHA-256 `8c7c34f6b714e0a6cde7b66d153e65fdff6e9681558335bfee830e2fe2a5f9f7`)
and bound it to the 9,191-byte released source. Inventory extraction,
media verification and the inventory suite pass; real guest output and
fake-chip memory-provider behavior, lifecycle, PURE and package gates remain
open.

## 2026-09-23 - Reboot empty-template failure parity

Corrected the shared Reboot body and native fixture after comparing the
released MorphOS source: a failed empty-template `ReadArgs` is ignored, the
command still queries `SetSignal`, and it still requests `ColdReboot`; only a
pending Ctrl-C changes the return path. The parser `IoErr` is retained when a
mock reboot returns. Both Workbench and MorphOS entries use this body, and the
fixture now checks the corrected parser-failure transition.

The refreshed three-CPU resident receipts are
`artifacts/reboot-morphos-native-20260923-parity-v2/qualification.json` and
`artifacts/reboot-wb31-native-20260923-parity-v2/qualification.json`. Each
profile passes ten supplied invocations per 68000/020/040, including the
parser-failure path, with no shared-image writes or leaked fixture resources.

## 2026-09-23 - MorphOS Reboot packed identity closure

Added `reference-captures/reboot-morphos-binary-audit-20260923.json` and
registered it with the inventory validator. The audit binds `MorphOS/C/Reboot`
at ISO extent `178088`, 1,206 bytes, SHA-256
`6173f32fed7ab0cff36fb0c0f5e87f907525877ae67469e001f511c822199024` to the
released 955-byte source (`Reboot 50.3`, source SHA-256
`9e3cc8dd4f9fba123fb3507f86333c09164cf05e26b64ec237125d1c10990fc5`). It
records the empty template, `ERROR_BREAK`/`ColdReboot` boundary, returning
value `666`, and the installer `+P` event without claiming guest or terminal
reboot qualification. Inventory extraction/verification passes and the suite
now passes 21 tests; the six reference-closure IDs and shipping count remain
unchanged.

## 2026-09-23 - ConClip MorphOS worker architecture gate

Recorded the source's explicit `NP_CodeType=MACHINE_PPC` worker requirement in
the reference audit and contract. The current CopperSharp native toolchain
emits 68k resident bodies, so the normal MorphOS service cannot be replaced by
an unqualified 68k worker without changing the original design. The 68k `OFF`
control plane remains valid and isolated; PPC worker production and its
cross-architecture lifecycle evidence are now the explicit next gate.

## 2026-09-23 - ConClip ABI audit regression

Added an inventory regression that requires the private-console audit to carry
the pinned developer-CD and FD hashes, the four exact LVOs, and the A0
argument contracts. The inventory suite now passes 16 tests, so future edits
cannot silently turn the ABI scaffold back into guessed vectors.

## 2026-09-23 - ConClip OFF control plane

Added `NativeConClipControl.TryStopExistingWorker`, which exactly preserves
the source's `OFF` boundary: protect Exec's port list, find the named
`ConClip.rendezvous` port, read its signal task, send `SIGBREAKF_CTRL_C`, and
release the protection. A missing port or signal task is a no-op and never
starts a worker. The normal persistent startup path is still intentionally
separate and unclaimed.

## 2026-09-23 - ConClip private console binding scaffold

Added `NativeConClipConsoleRaw`, a deliberately isolated raw-call surface for
the four NDK-bound private `console.device` vectors. It carries the explicit
A6 device base and A0 hook/data registers and builds cleanly with the native
command project. No command entry or transient clipboard substitute was added;
the persistent worker, guest Hook/SGWork callbacks, IFF transactions and
lifecycle qualification remain required before this binding can be promoted.

## 2026-09-23 - ConClip private console ABI evidence

Captured the NDK 3.1 `console.device` FD and bound the private vectors used by
the MorphOS source: `GetConSnip` `-54`, `SetConSnip` `-60`,
`AddConSnipHook` `-66`, and `RemConSnipHook` `-72`, including their A0 and
return-value ABI. This removes guessed vector numbers from the implementation
plan. It does not qualify the persistent worker, hook ordering, failure paths,
guest parity, PURE/lifecycle behavior, or package placement.

## 2026-09-23 - ConClip persistent-service reference contract

Added the hash-bound Workbench and MorphOS `ConClip` identities plus the
released MorphOS source audit. The contract freezes the candidate
`CLIPUNIT=UNIT/N,OFF/S` boundary, the `ConClip.rendezvous` worker, duplicate
start and `OFF` signaling, Intuition edit-hook and console snip-hook lifetime,
clipboard `FORM FTXT/CHRS` IFF transactions, message replies, and reverse
cleanup. Because this is a persistent service, no transient `Clip`-style body
was added. Exact parser/help behavior, hook/device failures, worker retirement,
original guest parity, PURE/lifecycle and package gates remain open.

## 2026-09-23 - MorphOS capture-audit drift regression

Added an inventory regression that mutates a synthetic MorphOS capture audit's
SHA-256 and requires `validate_morphos_capture_audit` to reject it. The test
now protects the same fail-closed path used for the pinned `CPU`, `Stat`,
`ShowConfig`, `Time`, `Uptime`, and `WaitX` ISO audits; the inventory remains
reference evidence, not runtime qualification.

## 2026-09-23 - MorphOS WaitX reference contract

Hash-bound `MorphOS/C/WaitX` to its 4,230-byte ISO member (extent 178824,
SHA-256 `88f59afb972fb0bc410eb720b3b25e5baa50e401cb9ab1cc7ac331568e075e03`)
and recorded its `WaitX 51.1 (26.03.2018)` version tag, documented
wait-then-execute purpose, and installer `+P` event. The packed member exposes
no reliable template or diagnostics; [contract](contracts/WaitX.md) leaves the
delay grammar, guest timer/scheduler behavior, nested-launch result/`IoErr`
precedence, guest parity, PURE/lifecycle and package admission open. No guessed
timer or launch wrapper was added. Inventory extraction rechecks the audit
against the pinned ISO extent, byte count and SHA-256.

## 2026-09-23 - MorphOS CPU, Stat and ShowConfig reference contracts

Hash-bound MorphOS `CPU` (4,123 bytes, extent 175236, version 50.10), `Stat`
(4,319 bytes, extent 178468, version 50.2), and `ShowConfig` (15,811 bytes,
extent 178220, version 50.17) to the pinned ISO and recorded their documented
purposes and printable-scan boundaries. The installer records literal `+P`
events for CPU and Stat; no such event was observed for ShowConfig, so its
purity remains unresolved. The contracts deliberately leave packed templates,
providers, output, diagnostics, guest parity, lifecycle and package admission
open. Inventory extraction now rechecks all three audits against the pinned
ISO extents, byte counts and SHA-256 values.

## 2026-09-23 - MorphOS Time reference contract

Hash-bound `MorphOS/C/Time` to its 4,999-byte ISO member (extent 178504,
SHA-256 `03e00e1a8b35a2b31f785d17c1189228a3960bf345abef4493c3ac2abbe347a5`)
and recorded its `Time 1.0 (24.05.26)` version tag and installer `+P` event.
The MorphOS Library index supplies the purpose description (measure command
execution time), but the packed binary exposes no reliable template or
diagnostic strings. The new [contract](contracts/Time.md) deliberately leaves
the command-tail grammar, guest timer source, units, output, failure precedence,
guest parity, PURE/lifecycle and package admission open; no guessed timer or
formatter was added. Inventory extraction now rechecks the audit against the
pinned ISO extent, byte count and SHA-256 during every extraction.

## 2026-09-23 - MorphOS Uptime reference contract

Hash-bound `MorphOS/C/Uptime` to its 2,733-byte ISO member (extent 178788,
SHA-256 `1b1c87202fca5c789a1c34c700d72d879367bb9fb76bd2f23c168d60bdc60c7e`)
and recorded its 50.5 version tag, no-template boundary, and installer `+P`
event. The new [contract](contracts/Uptime.md) deliberately leaves output,
timer source, failure precedence, guest parity, PURE/lifecycle and package
admission open; no guessed implementation was added.
The inventory extractor now rechecks the audit against the pinned ISO extent,
byte count and SHA-256 during every extraction; a changed or relocated member
fails closed.

## 2026-09-23 - Workbench capture-audit integrity gate

The inventory extractor now verifies both durable Workbench `C:` capture audits
against the selected external ADFs: source identity, pinned ADF hash, member
membership, byte counts and SHA-256 values are checked before generated
inventory evidence is accepted. A hand-edited or stale metadata record now
fails closed instead of being treated as reference evidence.

## 2026-09-23 - Workbench disk 1 C: command capture

Verified the externally supplied Workbench 3.1 M10 Install disk 1 ADF
(`8f54e735925d733719a321a0ddabd8fd6d1c1c3d3c0a516ed6b77dee89ac9e1e`) against
its source ZIP (`e320dbbcb2b8e34da7d3e37a2953c623c26755191a74c2512169b2e6f81f2e78`)
and captured hashes and sizes for all 20 listed `C:` members, including the
installation-only helpers. The durable metadata audit is
`reference-captures/wb31-disk1-c-command-captures-20260923.json`.
The three Install/HDSetup/Update startup and installer members are separately
hash-bound in `reference-captures/wb31-disk1-install-script-captures-20260923.json`;
this records intended script evidence only and does not claim installed-state
metadata or execution.
Disks 3-6, installed placement/protection, and runtime behavior remain open;
original files remain outside the repository.

## 2026-09-23 - Workbench disk 2 C: command capture

Verified the externally supplied Workbench 3.1 M10 disk 2 ADF
(`a8f167bad2897e8c7f2cfe73de7a9f8dad10c7a5b1f78ca17f818ab17278b985`) against
its source ZIP (`d93611887acf91f68f5608a5b7812f03eb16f743f40451b67c93067b04c967ed`)
and captured hashes and sizes for all 50 listed `C:` members. The durable
metadata audit is
`reference-captures/wb31-disk2-c-command-captures-20260923.json`.
This closes neither disks 3-6 nor installed placement/protection/runtime
behavior; original files remain outside the repository.

## 2026-09-22 - MorphOS LoadMonDrvs reference boundary

Hash-bound MorphOS 3.20 `MorphOS/C/LoadMonDrvs` to its 2,279-byte packed member
at ISO extent 176144 (`LoadMonDrvs 50.1 (14.7.03)`, SHA-256
`c33c5263f6edd1c1ff11738558492d5cbfe371e6117383459b4c319082dea384`). The
new [contract](contracts/LoadMonDrvs.md) and
[audit](reference-captures/loadmondrvs-morphos-binary-audit-20260922.json)
record the documented `FROM/K,EXCEPT` candidate, DEVS:Monitors default,
alternate-directory and excluded-driver behavior, and the installation
evidence requiring PURE. The packed member has no plaintext template or
diagnostics; no host-backed/no-op loader is added pending guest driver,
resident, lifecycle and package evidence.

The bounded resident candidate now compiles for 68000/020/040 from
`NativeMorphOSLoadMonDrvsCommand` and its private entry. The static receipt
`artifacts/loadmondrvs-morphos-native-static-20260922-v1/qualification.json`
records 17 reachable methods per CPU with zero managed runtime features,
helpers, external targets, exception regions or fatal fault sites. It covers
the documented parser/workspace/matcher/guest-segment ownership boundary only;
no packed, provider-backed, guest, lifecycle, PURE or package qualification is
claimed.

The LoadResource contract now also records the contemporaneous AmigaDOS
documentation boundary: path-based library/device/font/catalog selection,
LOCK/UNLOCK residency semantics, and no-name locked-resource listing. These
are documentation facts only; they do not close the selected Workbench guest
parser, catalog, diagnostic or IoErr gates.

## 2026-09-22 - IPrefs startup-service contract boundary

Hash-bound Workbench `C:IPrefs` to the 13,848-byte `iprefs 40.7 (2.6.93)` HUNK
(`14341be81f06506852204a33e7bfe7eb6fa2067d7f1e55810007bd94b021f0ef`) and
MorphOS `MorphOS/C/IPrefs` to its 43,798-byte packed member at ISO extent
175924 (`2c4f6bf00b8f917fe30675fa7c758850583dc712f64b8bf1fed40c646b38cccf`).
The new [contract](contracts/IPrefs.md) and
[audit](reference-captures/iprefs-reference-audit-20260922.json) record the
no-`ReadArgs` startup boundary, MorphOS duplicate-service guard and completion
message, library floors, preference-table/handler surface, notification and
installation-validation ownership. The Workbench member is non-PURE by its
protection word. No transient no-op service is added; exact guest ordering,
provider failures, lifecycle, PURE, package and differential gates remain open.

## 2026-09-22 - Workbench SetPatch machine-contract boundary

Hash-bound the identical 13,484-byte `C:SetPatch` HUNK on the selected
Install3.1 and Workbench3.1 members (`setpatch 40.16 (14.2.94)`, SHA-256
`745b2f90fabcdeccba099b3302593eff89f393d65bd84a9c6bad1b46063ab5cc`). The
new [contract](contracts/SetPatch.md) and
[audit](reference-captures/setpatch-wb31-binary-audit-20260922.json) freeze
the candidate `QUIET/S,NOCACHE/S,REVERSE/S,NOAGA/S` grammar and the observed
machine-specific patch surface, including A3000/68040, graphics/Intuition,
DOS, SCSI/trackdisk, NMI, cache and Nu* families. Both media protection words
are zero, so PURE is not assumed. No native success/no-op stub is added:
unknown ROM addresses and unsupported machine profiles remain explicitly
blocked pending guest byte-diff, rollback, lifecycle and package evidence.

## 2026-09-22 - AddDataTypes reference contract boundary

Hash-bound both AddDataTypes references: Workbench 3.1 `C:AddDataTypes` is a
5,880-byte HUNK (`adddatatypes 39.2 (27.7.92)`, SHA-256
`391da11b39bfc7c492c1b58aa9e442f1c1712bd507380903a88a4534d27c264d`) and
MorphOS 3.20 `MorphOS/C/AddDataTypes` is a 7,751-byte packed member at ISO
extent 175124 (`AddDataTypes 50.6 (27.11.04)`, SHA-256
`afe23d74f3c3b18cd45f892189a6a26bf1bc8d42a426ec7db816e0c9995d11dd`). The
new [contract](contracts/AddDataTypes.md) freezes the profile-specific
ReadArgs grammars, datatype-list semaphore ownership, IFF/DTCD descriptor
loading, refresh/list scanning, Workbench startup reply and reverse cleanup.
The source audit is
`reference-captures/adddatatypes-reference-audit-20260922.json`. This is a
reference/specification checkpoint only: no guest execution, exact
diagnostics/result precedence, resident/PURE lifecycle or package admission is
claimed, and no shipping count changes.

## 2026-09-22 - Workbench LoadResource binary contract boundary

Hash-bound the selected Workbench `C:LoadResource` HUNK (3,972 bytes,
`loadresource 40.2 (17.3.93)`, SHA-256
`51c8d6da726d5d1429e84f36e323218eb94750ab60b650efe768fc2907d16f38`). The
bounded audit recovered `NAME/M,LOCK/S,UNLOCK/S`, the Exec/DOS/locale/utility/
graphics/diskfont dependency surface, resource lock/catalog strings, and the
installer-era resource-management boundary. Exact lookup/state transitions,
catalog behavior, diagnostics, guest parity, PURE/lifecycle and package
admission remain open. Contract:
`contracts/LoadResource.md`; audit:
`reference-captures/loadresource-wb31-binary-audit-20260922.json`.

## 2026-09-22 - Workbench SetFont native entry fixture

The source-informed SetFont candidate now has a reproducible three-CPU
resident entry receipt at
`artifacts/setfont-wb31-native-entry-20260922-v2/qualification.json`.
Sixteen supplied invocations per CPU pass with one shared image, no image
writes and no leaked fixture resources. The cases cover parser/result
ownership, version-37 graphics/diskfont/utility floors, allocation and font
failure cleanup, TextAttr name/size/style/flags, console-task `DoPkt` BPTR
transport, RastPort replacement, old-font close order, escape output,
Workbench/missing-DOS startup guards and interleaved callers. The exact
original guest packet/layout and result/IoErr behavior remains a differential
gate.

## 2026-09-22 - Workbench SetFont resident static candidate

Added the provisional Workbench `SetFont` native entry and a repeatable
three-CPU resident HUNK qualification. The candidate keeps the captured
seven-slot grammar, opens the four version-37 libraries, builds the observed
`TextAttr`, and follows the console-task packet/font replacement boundary.
`artifacts/setfont-wb31-native-static-20260922-v1/qualification.json` passes
68000/020/040 with no managed allocations, runtime helpers/features,
exception regions, external native targets or fatal machine-fault sites.
Runtime fixture work was still open at this static-candidate checkpoint because
the exact guest handler behavior, diagnostics/result precedence and output
behavior had not been captured. Later bounded HUNK analysis identified the
ACTION_DISK_INFO/InfoData path and public Window fields, but guest comparison
remains open.

## 2026-09-22 - Workbench SetFont binary contract boundary

Hash-bound the selected Workbench 3.1 `C:SetFont` HUNK (1,092 bytes,
`setfont 39.1 (2.6.92)`, SHA-256
`c3e1e763b12fd4f710a414443facea49479aa723a94828828dbaee214fe4ad8c`). The
bounded audit records the seven-slot
`NAME/A,SIZE/N/A,SCALE/S,PROP/S,ITALIC/S,BOLD/S,UNDERLINE/S` candidate,
version-37 DOS/graphics/diskfont/utility imports, TextAttr/style setup,
case-insensitive `.font` suffix handling, console-task `ACTION_DISK_INFO`
handoff through InfoData, console Window `RastPort`/font replacement, and
`tf_Style`-gated escape output. Exact selected-handler behavior,
output/result precedence, guest parity, PURE/resident lifecycle, and package
admission remain open.
Contract: `contracts/SetFont.md`; audit:
`reference-captures/setfont-wb31-binary-audit-20260922.json`.

## 2026-09-22 - MorphOS SetKeyboard source-informed resident boundary

Read and hash-verified the authoritative `MorphOS/C/SetKeyboard` member at
ISO extent 178200 (`7f4d4f53` packed-native, 2,856 bytes, SHA-256
`f12a418b372b7e2d724a2ba56235e58bcd9135466c26fea8031e12f1af3c8fa4`). The
bounded inspection recovered the version tag but no plaintext template or
diagnostic candidates; the result is recorded in
`reference-captures/setkeyboard-morphos-packed-inspection-20260922.json`.

Added an independently written MorphOS DOS 37 resident candidate from the
hash-bound `IPrefs` `SetKeyMap` source correspondence. It keeps the provisional
`KEYMAP/A` grammar, reuses `keymap.resource` nodes, supports absolute paths and
the `KEYMAPS:` then `MOSSYS:Devs/Keymaps` fallback, handles resident/extended
nodes and duplicate races under `Forbid`/`Permit`, calls
`keymap.library/SetKeyMapDefault`, and rolls back a private segment on later
failure. Three resident HUNKs pass 12 supplied vectors per CPU with no managed
allocations, runtime helpers, external targets, exception/fatal sites, shared
image writes, or leaked resources. Receipt:
`artifacts/setkeyboard-morphos-native-entry-20260922-v1/qualification.json`.
The packed command's exact template/diagnostics, guest parity, PURE/lifecycle,
package and differential gates remain open.

## 2026-09-22 - Workbench SetKeyboard runtime checkpoint

Added the Workbench 3.1 `SetKeyboard` contract and resident entry for the
captured `KEYMAP/A` grammar. The body joins `DEVS:Keymaps` with DOS `AddPart`,
loads the keymap through `LoadSeg`, converts the segment to the `KeyMapNode`
layout documented by the original API, calls `keymap.library/SetKeyMapDefault`,
and deliberately retains a successful segment because the selected keymap is
cached by the system. The repeatable three-CPU static receipt is
`artifacts/setkeyboard-wb31-native-static-20260922-v2/qualification.json`;
all reachable paths compile with zero managed allocations, runtime helpers,
external native targets, exception regions, and fatal machine-fault sites.
The executable three-CPU boundary receipt is
`artifacts/setkeyboard-wb31-native-entry-20260922-v2/qualification.json`;
ten vectors per CPU cover parser failure, keymap-resource reuse, load and
library failures, startup, missing DOS, repeated ownership and interleaving,
including a resident resource node at the list tail after correcting the
lookup traversal. There are no shared-image writes or leaked resources.
Exact original diagnostics,
guest behavior, resident/PURE metadata, package placement, and differential
evidence remain open.

The MorphOS correspondence audit also records the related `IPrefs` source
behavior: `FindKeyMap` uses the keymap resource list, then `KEYMAPS:` and
`MOSSYS:Devs/Keymaps`, handles resident/extended nodes under `Forbid`/`Permit`,
and updates charset preferences. The packed `C:SetKeyboard` entry still needs
its own template and call-path capture before those effects can be transferred.

## 2026-09-21 - BindDrivers reference contract checkpoint

Added the shared [`BindDrivers`](contracts/BindDrivers.md) contract for the
Workbench 3.1 and MorphOS 3.20 profiles. It binds both original member
identities and records the source-observed no-option automatic
`SYS:Expansion/#?.info` scan, v37 DOS/icon/Expansion floors,
`PRODUCT`/`ConfigDev` parsing, HUNK resident discovery, `InitResident` load
ownership, and reverse cleanup. The Workbench binary still lacks source
correspondence and both profiles lack runtime, installed-overlay, PURE,
package, and differential evidence; no shipping gate is claimed.

The first source-bound MorphOS resident body is now compiled for 68000/020/040
with zero managed runtime features, helpers, external native targets,
exception regions, and fatal fault sites. Static receipt:
`artifacts/binddrivers-morphos-native-static-20260921-v3/qualification.json`.
A repeatable resident boundary suite now also passes seven supplied vectors per
CPU for no-match/empty scans, library-open failures, repeat/interleaving, and
cleanup ownership; runtime receipt:
`artifacts/binddrivers-morphos-native-entry-20260921-v2/qualification.json`.
Provider-backed file/icon/resident execution, packed correspondence, guest
behavior, PURE, package placement, and differential gates remain open.

## 2026-09-21 - MorphOS Format synthetic disposable-media boundary checkpoint

Added `FormatEntrySuite` and the repeatable
`qualify_morphos_format_native_entry.ps1` receipt for the MorphOS `Format`
resident entry. The exact source `ReadArgs` template now has ten supplied
vectors per MC68000/68020/040 (30 total), covering startup and missing-DOS
guards, parser/result-allocation failures, banned system name, missing device,
repeat/interleaving, and full/quick formatting through a synthetic disposable
DOS-handler/trackdisk queue. Full formatting performs one format write/readback
and update/clear/motor cleanup; quick formatting performs no trackdisk media
I/O. The receipt is
`artifacts/format-morphos-native-entry-20260921-v4/qualification.json`.
Original-guest parity, production provider binding, TD64/SFS/icon behavior,
PURE, and package admission remain open in `contracts/Format.md` and the
completion ledger.

## 2026-09-21 - MorphOS Mount outer-entry boundary checkpoint

Added the repeatable `MountEntrySuite` and
`qualify_morphos_mount_native_entry.ps1` receipt for the resident MorphOS
`Mount` entry. The exact outer `DEVICE/M,FROM/K,DEBUG/S` grammar is exercised
on resident MC68000/68020/040 HUNKs with an empty multiple vector, parser
failure, result allocation failure, and repeat/interleaved cleanup (15
invocations total). The receipt is
`artifacts/mount-morphos-native-entry-20260921-v2/qualification.json`.
It proves only DOS startup, result/RDArgs ownership, IoErr preservation and
resource/image cleanup; real DOSDrivers/MountList, icon/Expansion and
handler/provider behavior, Workbench startup, PURE, differential evidence and
package admission remain open in `contracts/Mount.md` and the completion
ledger.

## 2026-09-21 - Workbench Mount outer-entry boundary checkpoint

Added the Workbench 3.1 counterpart to the bounded Mount entry fixture. The
exact `DEVICE/M,FROM/K` grammar now passes nine supplied invocations per CPU
on resident MC68000/68020/040 HUNKs (27 total), covering empty `DEVICE/M`,
parser and allocation failures, repeat/interleaving, DOS36 startup and
cleanup, empty and single-argument Workbench startup messages that return
before CLI parsing, a missing startup argument list, and a startup source-not-found path that exercises argument-list iteration, `CurrentDir` save/restore, `FilePart`, Expansion cleanup and icon fallback failure. The receipt is
`artifacts/mount-wb31-native-entry-20260921-v5/qualification.json`.
successful Workbench startup source processing, real source/provider behavior, PURE,
differential evidence and package admission remain open in `contracts/Mount.md`
and the completion ledger.

## 2026-09-21 - Workbench installation-helper contract checkpoint

Added source-independent contracts for the two remaining CC22 Workbench
installation utilities. `Prod_Prep` is bound to the 37,008-byte Install3.1
member and its custom interactive RDB/partition, format, verify, filesystem,
SCSI/XT and bad-block operation surface. `UpdateWBFiles` is bound to the
6,764-byte Install3.1 member and its `NEWWB:` preference/icon update surface,
IFF chunk families, and DOS/iffparse/icon/graphics dependencies. Neither
binary exposes a verified `ReadArgs` template, so no parser was invented. Both
contracts require disposable guest/device fixtures, exact mutation/error
captures, native implementation, PURE/resident, package and differential
qualification before admission.

## 2026-09-21 - Workbench library capability-floor checkpoint

Revised the dependency policy so captured `OpenLibrary` minimums are retained
as differential evidence, while native entries request the lowest verified
capability floor for every consumed library, device, resource, and ROM vector.
For `FindResident`, the original DOS 37 request is preserved in the contract,
but the replacement uses DOS 36 because its required DOS calls are available
there and the lookup itself is the Exec `FindResident` vector. The fresh
three-CPU receipt `artifacts/findresident-wb31-native-20260921-v4/qualification.json`
passes eleven supplied DOS 36/Exec vectors per CPU, including successful and
missing resident lookups, parser/result-slot failures, startup and missing-DOS
guards, and interleaved callers. Exact original diagnostics/result precedence,
guest parity, PURE/resident lifecycle, packaging and differential evidence
remain open. Contract: `contracts/FindResident.md`.

The same audit now covers the other bounded Workbench library consumers. The
`Check2090` entry uses DOS36 and expansion33 (`FindConfigDev` is a V33 public
vector), while `GuessBootDev` uses DOS36, utility0 because the replacement
does not call a utility vector, and expansion33 for the boot-node ABI. `Date`
now requests utility36 because its `UMult32`/`UDivMod32` calls are V36
vectors, despite the captured utility v0 import. Refreshed receipts are
`artifacts/check2090-wb31-native-20260921-v3/qualification.json`,
`artifacts/guessbootdev-wb31-native-20260921-v2/qualification.json`, and
`artifacts/date-native-entry-20260921-v2/qualification.json`. The original
minimums remain recorded as differential evidence and no lower floor is used
without API authority.

The MorphOS `RequestFile` entry now applies the same rule to `asl.library`:
its requester vectors are V36 authority calls, so the replacement requests
ASL36 while retaining the captured ASL37 request as differential evidence.
The refreshed three-CPU receipt
`artifacts/requestfile-morphos-native-20260921-v4/qualification.json` passes
twelve invocations per CPU with balanced ownership and unchanged images.

MorphOS `RequestChoice` now also opens DOS36 for its parser and cleanup path;
the replacement retains Intuition37 because the additional requester vectors
still need a lower-floor authority record. Its refreshed receipt is
`artifacts/requestchoice-morphos-native-20260921-v2/qualification.json`.

The MorphOS `Mount` candidate also lowers its expansion dependency to V33,
the authority floor for `MakeDosNode` and `AddDosNode`; its refreshed receipt
is `artifacts/mount-morphos-native-20260921-v2/qualification.json`. The
captured expansion V37 request remains differential evidence.

## 2026-09-21 - Workbench ExtractKickstart native fixture checkpoint

Recovered the source-derived boundary for the Workbench 3.1
`ExtractKickstart` helper: `DEVICE/A,TO/A,1.3/S`, case-insensitive `DF0:`-style
device validation, raw `trackdisk.device` access, `KICKSUP0` boot-block
validation, layout-specific image reads, and DOS destination writes. Added
`contracts/ExtractKickstart.md` with the observed 1,216-byte source identity,
required public APIs, invocation-owned resource rules, and the next fixture and
guest-validation steps. Added a separate resident native candidate and a
bounded native trackdisk/DOS fixture. The three-CPU receipt
`artifacts/extractkickstart-wb31-native-20260921-v6/qualification.json` records
seventeen vectors per CPU covering both media layouts, parser and library
failures, diagnostics, cleanup and interleaving, with no managed
allocation/runtime features. Exact guest differential, PURE/resident admission
and package gates remain open.

## 2026-09-20 - Workbench GuessBootDev native candidate checkpoint

Added the Workbench 3.1 `GuessBootDev` installer command as a separate DOS36
resident entry using the captured `BOOTDISKNAME` template. The body keeps the
parser, utility/Expansion/DOS leases, 300-byte name buffer, process-window
sentinel, locks, and boot-node traversal invocation-local; it selects the
highest signed usable boot priority and preserves the `SYS:` fallback. The
three-CPU receipt
`artifacts/guessbootdev-wb31-native-20260921-v2/qualification.json` passes
seventeen supplied vectors per CPU with thirteen reachable methods, no managed
allocation sites, runtime features/helpers, external targets,
exception/fatal sites, leaks or shared-image writes. Exact boot-node semantics,
guest parity, PURE/resident lifecycle, packaging and differential evidence
remain open. Contract: `contracts/GuessBootDev.md`.

## 2026-09-20 - Workbench IconPos native candidate checkpoint

Added the Workbench 3.1 `IconPos` installer command as a separate DOS36
resident entry using the captured twelve-slot grammar and public icon.library
DiskObject vectors. The body keeps parser, icon-library, DiskObject and image
leases invocation-local and covers position/type and drawer mutation,
free-position switches, image copying, default creation, parser/type and
storage failures, startup guards, missing DOS and interleaved callers. The
three-CPU receipt `artifacts/iconpos-wb31-native-20260920-v2/qualification.json`
passes fifteen supplied Exec/DOS/icon.library vectors per CPU with thirteen
reachable methods, no managed allocation sites, runtime features/helpers,
external targets, exception/fatal sites, leaks or shared-image writes. Exact
original icon.library semantics, guest parity, PURE/resident lifecycle,
packaging and differential evidence remain open. Contract:
`contracts/IconPos.md`.
The updated goal-plan SHA-256 is
`a010a642c50b6c410b8262792bafa913bc2d060619f1bc306cf7a981505c9192`.

## 2026-09-20 - Workbench Check2090 native candidate checkpoint

Added the Workbench 3.1 `Check2090` installation helper as a separate DOS36 /
expansion33 capability-floor
resident entry. The body opens Expansion v33, calls public `FindConfigDev` for
manufacturer `$202`/product `1`, interprets the ConfigDev flag byte, and keeps
all library ownership invocation-local. The three-CPU receipt
`artifacts/check2090-wb31-native-20260921-v3/qualification.json` passes ten
supplied Exec/Expansion/DOS vectors per CPU, covering controller present,
absent and flagged states, expansion failure, startup guards, missing DOS and
interleaved callers. Exact original result/IoErr behavior, guest parity,
PURE/resident lifecycle, packaging and differential evidence remain open.
Contract: `contracts/Check2090.md`.

## 2026-09-20 - Workbench FindResident native candidate checkpoint

Added the Workbench 3.1 `FindResident` candidate as a separate resident entry
with the captured `MODULE/A` syntax boundary. The body uses invocation-owned
DOS `ReadArgs`/`FreeArgs`, public Exec `FindResident`, and DOS fault/error
vectors, with explicit Workbench-startup, missing-DOS, parser, result-slot,
missing-resident and interleaved-call coverage. The three-CPU receipt
`artifacts/findresident-wb31-native-20260921-v4/qualification.json` passes eleven
supplied DOS 36/Exec vectors per CPU with 10 reachable methods and no managed allocation
sites, runtime features/helpers, external targets, exception/fatal sites,
leaks or shared-image writes. The 2026-09-21 checkpoint above applies the DOS
36 capability floor while retaining the original DOS 37 request as differential
evidence. Exact original diagnostics, guest parity, PURE/resident lifecycle,
packaging and differential evidence remain open. Contract: `contracts/FindResident.md`.

## 2026-09-20 - Workbench Break and ChangeTaskPri startup-boundary checkpoint

The Workbench 3.1 `Break` and `ChangeTaskPri` resident fixtures now add
explicit Workbench-startup and missing-`dos.library` invocations while keeping
their classic syntax candidates and MorphOS matrices separate. The refreshed
receipts pass ten `Break` and twelve `ChangeTaskPri` supplied Exec/DOS vectors
per CPU (30 and 36 total) on 68000/020/040 HUNKs. `Break` HUNKs are
2,632/2,632/2,632 bytes with reachable-method count 10; `ChangeTaskPri`
HUNKs are 2,548/2,548/2,548 bytes with reachable-method count 10. Startup
vectors allocate no parser or body state, and all paths remain leak-free with
no shared-image writes. Exact classic diagnostics/effects, original guest
parity, PURE/resident reuse, packaging and differential evidence remain open.

Receipts: `artifacts/break-wb31-native-20260920-boundaries/qualification.json`
and `artifacts/changetaskpri-wb31-native-20260920-boundaries/qualification.json`.

## 2026-09-20 - Workbench Info missing-DOS checkpoint

The Workbench 3.1 `Info` resident fixture now adds an explicit missing-
`dos.library` invocation while retaining the classic `DEVICE` template,
one- and two-device DOS-list traversal, mixed mounted-volume output and
startup guards. The refreshed receipt
`artifacts/info-wb31-native-20260920-missing-dos-v3/qualification.json` passes
fourteen supplied public Exec/DOS vectors per CPU (42 total) on 68000/020/040
HUNKs. HUNKs are 6,020/6,036/5,964 bytes with reachable-method count 16 and
no leaks or shared-image writes. Exact original output/IoErr precedence,
complete provider behavior, installed metadata, PURE/resident reuse, packaging
and differential evidence remain open.

## 2026-09-20 - Workbench Type startup-boundary checkpoint

The Workbench 3.1 `Type` resident fixture now adds explicit Workbench-startup
and missing-`dos.library` invocations while retaining the classic five-slot
`FROM/A/M,TO/K,OPT/K,HEX/S,NUMBER/S` layout. The refreshed receipt
`artifacts/type-wb31-native-20260920-boundaries/qualification.json` passes
fifteen supplied vectors per CPU (45 total) on 68000/020/040 HUNKs, including
text/NUMBER/HEX/TO, wildcard and diagnostic paths, balanced cleanup, startup,
missing-DOS and interleaved callers. HUNKs are 8,532/8,592/8,400 bytes with
reachable-method count 26 and no leaks or shared-image writes. Exact Workbench
output and diagnostics, packed correspondence, PURE/resident reuse, packaging
and differential evidence remain open.

## 2026-09-20 - Workbench Search missing-DOS checkpoint (historical)

The Workbench 3.1 `Search` resident fixture now adds an explicit missing-
`dos.library` invocation while keeping the MorphOS matrix unchanged. The
refreshed receipt `artifacts/search-wb31-native-20260920-boundaries/
qualification.json` passes eighteen supplied DOS-vector invocations per CPU
(54 total) on 68000/020/040 HUNKs, covering the bounded parser, flat
matcher/file traversal, literal and pattern modes, line output, quiet/file
modes, allocation/read/parser/Ctrl-C, startup and interleaved paths plus the
missing-DOS failure path. HUNKs remain 15,220/15,376/15,204 bytes with
reachable-method count 25 and no leaks or shared-image writes. At this
checkpoint the Workbench binary had not been bound and `ALL` recursion was not
implemented. The later binary identity and bounded traversal candidate are
recorded at the top of this log. Exact runtime grammar/output, complete
recursion, locale behavior, packed correspondence, PURE/resident reuse,
packaging and differential evidence remain open.

## 2026-09-20 - Workbench List missing-DOS checkpoint

The Workbench 3.1 `List` resident fixture now adds an explicit missing-
`dos.library` invocation while keeping the MorphOS matrix unchanged. The
refreshed receipt `artifacts/list-wb31-native-20260920-boundaries/
qualification.json` passes eighteen supplied DOS-vector invocations per CPU
(54 total) on 68000/020/040 HUNKs, covering the existing parser, flat matcher,
pattern/type filters, quick/block/no-header rendering, `TO` output,
allocation/Ctrl-C, startup and interleaved paths plus the missing-DOS failure
path. HUNKs are 8,664/8,724/8,692 bytes with reachable-method count 14 and no
leaks or shared-image writes. Recursive/date/sort/owner/`LFORMAT`/`ALL`, exact
Workbench parity, PURE/resident reuse, packaging and differential evidence
remain open.

## 2026-09-20 - MorphOS RequestChoice timed-request fallback checkpoint

The fixture now also forces `CreateMsgPort` and `CreateIORequest` failure
independently. Both paths fall back to blocking `SysReqHandler` without
opening the timer device, and the three resident images pass sixteen supplied
invocations per CPU with balanced port/request cleanup. The receipt remains
`artifacts/requestchoice-morphos-native-20260918-runtime-v1/
qualification.json`.

## 2026-09-20 - MorphOS RequestChoice interleaving checkpoint

The RequestChoice fixture now adds two interleaved callers to the previous
failure/startup matrix. The same resident image passes fourteen supplied
invocations per CPU, including the independent parser/text ownership of a
normal and percent-body requester while both callers are active. The receipt
remains `artifacts/requestchoice-morphos-native-20260918-runtime-v1/
qualification.json`; this is still bounded native adapter evidence.

## 2026-09-20 - MorphOS RequestChoice failure and startup checkpoint

Expanded the existing MorphOS `RequestChoice` resident fixture from eight to
twelve supplied calls per CPU. The added vectors cover public-screen lock
failure, temporary requester-text allocation failure, timer-device open
fallback, and missing-DOS startup with the entry's `Process.Result2` error;
the prior parser, Intuition-open, timeout, Ctrl-C, Workbench-startup, percent
escaping, and repeat-ownership checks remain. The updated three-CPU receipt is
`artifacts/requestchoice-morphos-native-20260918-runtime-v1/qualification.json`.
This remains bounded native adapter evidence; original guest UI behavior,
Workbench correspondence, PURE/resident lifecycle, packaging, licensing, and
differential parity remain open.

Plan/inventory SHA-256:
`56207039580e1d0f9b8d0f1acf53f5bb918f6acc69455fb28f6862659f36368e`.

## 2026-09-20 - Workbench SetDate startup-boundary checkpoint

The Workbench 3.1 `SetDate` resident fixture now adds explicit Workbench
startup and missing-`dos.library` cases. The refreshed receipt
`artifacts/setdate-native-entry-20260920/qualification.json` passes twelve
supplied ReadArgs/date/matcher/lock/SetFileDate vectors per CPU (36 total) on
68000/020/040 HUNKs, including those startup boundaries, balanced cleanup and
interleaved callers. Original guest diagnostics, MorphOS parity, PURE/resident
reuse, packaging and differential evidence remain open.

## 2026-09-20 - Workbench Date startup-boundary checkpoint

The Workbench 3.1 `Date` resident fixture now adds explicit Workbench startup
and missing-`dos.library` cases. The refreshed receipt
`artifacts/date-native-entry-20260920/qualification.json` passes nine supplied
ReadArgs/date/timer/output vectors per CPU (27 total) on 68000/020/040 HUNKs,
including those startup boundaries, balanced cleanup and interleaved callers.
Original guest precedence and diagnostics, PURE/resident reuse, packaging and
differential evidence remain open.

## 2026-09-20 - Workbench Wait startup-boundary checkpoint

The Workbench 3.1 `Wait` resident fixture now adds explicit Workbench startup
and missing-`dos.library` cases. The receipt
`artifacts/wait-wb31-native-20260920-boundaries/qualification.json` passes
seventeen supplied DOS/Exec/timer vectors per CPU (51 total) on 68000/020/040
HUNKs, including timer/Ctrl-C ownership, balanced cleanup and interleaved
callers. Original guest timing/diagnostics, PURE/resident reuse, packaging and
differential evidence remain open.

## 2026-09-20 - Workbench Avail startup-boundary checkpoint

The Workbench 3.1 `Avail` resident fixture now adds explicit Workbench startup
and missing-`dos.library` cases. The receipt
`artifacts/avail-wb31-native-20260920-boundaries/qualification.json` passes
eleven supplied DOS/Exec/AvailMem vectors per CPU (33 total) on 68000/020/040
HUNKs, including selector, summary and fail-closed `FLUSH` paths, balanced
cleanup and interleaved callers. Original expunge behavior, diagnostics,
PURE/resident reuse, packaging and differential evidence remain open.

## 2026-09-20 - Workbench Dir missing-DOS checkpoint

The Workbench 3.1 `Dir` resident fixture now adds an explicit missing-
`dos.library` startup case alongside its ReadArgs, OPT filtering, public
Lock/ExAll enumeration, output, parser/allocation, Workbench rejection and
interleaved paths. The receipt
`artifacts/dir-wb31-native-20260920-boundaries-v3/qualification.json` passes
sixteen supplied DOS vectors per CPU (48 total) on 68000/020/040 HUNKs with
balanced cleanup and unchanged images. Recursive ALL, interactive INTER, exact
classic parity, PURE/resident reuse, packaging and differential evidence remain
open.

## 2026-09-20 - Workbench DiskChange missing-DOS checkpoint

The Workbench 3.1 `DiskChange` resident fixture now adds an explicit
missing-`dos.library` startup case alongside its existing Workbench rejection,
DeviceProc lookup, ACTION_INHIBIT packet, parser, and interleaved paths. The
receipt `artifacts/diskchange-wb31-native-20260920-boundaries/qualification.json`
passes eleven supplied vectors per CPU (33 total) on 68000/020/040 HUNKs with
balanced cleanup and unchanged images. Classic guest parity, PURE/resident
reuse, packaging and differential evidence remain open.

## 2026-09-20 - Workbench SetClock resident candidate

Added a separate Workbench 3.1 `SetClock` body and DOS 36 resident entry for
the captured `LOAD/S,SAVE/RESET` grammar. It keeps the classic
`battclock.resource` read/write/reset vectors and VBlank `timer.device`
`GetSysTime`/`TR_SETSYSTIME` path, leaving the MorphOS 52+ UTC extension in the
MorphOS profile only. The deterministic receipt
`artifacts/setclock-wb31-native-20260920-candidate/qualification.json` passes
sixteen supplied DOS/Exec/resource/timer vectors per CPU on one shared
68000/68020/68040 resident image, including parser/resource/allocation/device
failures, timer-I/O diagnostics, Workbench startup and missing-DOS boundaries,
repeat ownership and two interleaved callers.
Original Workbench clock behavior, exact help/diagnostics, PURE/resident
classification, minimum stack, licensing, packaging and guest differential
remain open.

## 2026-09-20 - Workbench RequestFile resident candidate

Added a separate Workbench 3.1 `RequestFile` body and resident entry using the
captured thirteen-slot `DRAWER,FILE/K,PATTERN/K,TITLE/K,POSITIVE/K,NEGATIVE/K,
ACCEPTPATTERN/K,REJECTPATTERN/K,SAVEMODE/S,MULTISELECT/S,DRAWERSONLY/S,NOICONS/S,
PUBSCREEN/K` grammar. The candidate opens DOS 36 and ASL 36, maps the common
FileRequester tags, preserves single/multi-select output and cleanup, and
leaves the MorphOS-only `INITIALVOLUMES/S` extension out of the Workbench
result block. Its deterministic receipt
`artifacts/requestfile-wb31-native-20260920-candidate/qualification.json`
passes twelve supplied DOS/ASL vectors per CPU on one shared 68000/68020/68040
resident image, including parser/requester/startup failures, repeat ownership
and two interleaved callers. Original Workbench requester behavior, diagnostics,
PURE/resident classification, minimum stack, licensing, packaging and guest
differential remain open.

## 2026-09-18 - RequestChoice source contract closure

Added the MorphOS 3.20 `RequestChoice` contract from the extracted source
(`requestchoice.c`, SHA-256
`dcadad2e5982a7e4a3904bb3e0d0fa2a91a79e35ee3d9a9c41c684341d4772e6`). The
record freezes the `TITLE/A,BODY/A,GADGETS/A/M,PUBSCREEN/K,TYPE/K,
TIMEOUT=TIMEOUTSECS/K/N` template, percent escaping and gadget joining,
Intuition requester calls, timeout/timer fallback and cleanup, output, and
failure diagnostics. The implementation and three-CPU fixture are recorded in
the following entry; this contract step does not itself change shipping
counts.

## 2026-09-18 - MorphOS RequestChoice resident runtime slice

Added `NativeMorphOSRequestChoiceCommand` and its DOS 37 resident entry. The
body follows the source's ReadArgs ownership, gadget-vector joining, percent
escaping, public-screen lock, EasyRequest and BuildEasyRequest timeout paths,
timer abort/wait cleanup, requester fallback, output, and failure policy. The
independent compiled-HUNK fixture passes eight supplied invocations on each
68000/020/040 image, including parser/open failures, no-timeout and timeout
requesters, Ctrl-C, Workbench startup, percent-body input, and repeat use.
Receipt: `artifacts/requestchoice-morphos-native-20260918-runtime-v1/
qualification.json`. This advances the source/native stage only; original
guest UI behavior, Workbench correspondence, PURE/resident lifecycle,
packaging, licensing, and differential parity remain open.
Plan/inventory SHA-256:
`992190454675377a195316e9e4b048ed5643186358994e7a6ff1fba5c65d3f03`.

## 2026-09-17 - Qualification harness portability closure

The standalone qualification-script regression suite now preserves expected
nonzero child exits and supports both Windows PowerShell 5.1 and PowerShell 7.
The compatibility path replaces unavailable `Convert.FromHexString`,
`Convert.ToHexString`, `SHA256.HashData`, and `Dictionary.TryAdd` calls with
equivalent guarded helpers. Both hosts pass all fail-closed, drift, framework-
closure and HUNK-tail admission checks. This removes a local harness blocker;
reference closure, guest parity, lifecycle/PURE evidence and package admission
remain open.
Plan/inventory SHA-256: `e0baae9a52bbb1bebd4473e19156836724c4c5ccd37df8c96563f43487859b36`.

## 2026-09-17 - Workbench MakeDir resident entry qualification

Added `Workbench31MakeDirEntry` around the verified Workbench 37.2 `NAME/M`
body. The entry opens DOS 36, publishes the original missing-DOS secondary
error through the current process, rejects Workbench startup and malformed
argument boundaries, then owns the final body/library cleanup. The body uses
raw BPTR calls so the resident closure has no nullable-value runtime feature.

The independent compiled-HUNK fixture passes fourteen supplied public-DOS
invocations on each 68000/020/040 image, covering the result slot, empty and
multiple vectors, existing/create/failure ordering, command-owned messages,
parser/allocation failures, startup boundaries and interleaved callers. All
allocations balance and no shared image writes occur. Receipt:
`artifacts/makedir-wb31-native-20260917-runtime-v2/qualification.json`.
Packed correspondence, original guest behavior, full filesystem effects,
installed PURE/resident lifecycle, packaging and differential parity remain
open; this does not change the 0/200 and 0/246 shipping totals.
Plan/inventory SHA-256: `f36ac7aa8c3d9eff6ae0e09dbd9b43ae791c8331fbce94159012670fe6d9b2e8`.

## 2026-09-17 - MorphOS MakeDir source-informed candidate

Added `NativeMorphOSMakeDirCommand` from the independently inspected 50.4
source contract (`NAME/M,ALL/S`). The body uses public DOS ReadArgs, Lock,
CreateDir, Examine, ChangeMode and CurrentDir, restores the caller's current
directory, and keeps path/FIB/error ownership invocation-local. Its DOS 37
resident entry compiles for all three CPUs with zero managed runtime features,
helpers, external targets, exception regions or fatal fault sites. Receipt:
`artifacts/makedir-morphos-native-20260917-qualified-v5/qualification.json`.
This advances the source/native stage only; packed parser/output behavior,
runtime fixture, guest parity, installed purity and package admission remain
open.

Plan/inventory SHA-256: `f36ac7aa8c3d9eff6ae0e09dbd9b43ae791c8331fbce94159012670fe6d9b2e8`.

## 2026-09-17 - MorphOS MakeDir supplied-vector resident qualification

Extended the source-informed MorphOS MakeDir candidate with an independent
compiled-HUNK fixture. It runs fifteen supplied public-DOS invocations on each
68000/68020/68040 image, covering `NAME/M,ALL/S` result ownership, ordinary
creation and failure, `ALL` volume/leaf walking, `CurrentDir` restoration,
parser and allocation failures, diagnostics, startup boundaries and
interleaved callers. All three CPU runs pass with balanced allocations and no
shared-image writes. Receipt:
`artifacts/makedir-morphos-native-20260917-runtime-v10/qualification.json`.
The fixture intentionally does not claim packed-binary correspondence, a real
filesystem handler, original guest parity, installed PURE/resident lifecycle,
or package admission.
Plan/inventory SHA-256: `f36ac7aa8c3d9eff6ae0e09dbd9b43ae791c8331fbce94159012670fe6d9b2e8`.

## 2026-09-17 - Workbench Lock syntax candidate

Added `NativeWorkbench31LockCommand` and its private DOS 36 resident entry.
The Workbench Disk 1/2 HUNK exposes the same
`DRIVE/A,ON/S,OFF/S,PASSKEY` syntax candidate as the source-bound MorphOS
body, so the profile reuses the public `GetDeviceProc`/`DoPkt2` boundary while
keeping startup separate. The three-CPU resident qualification runs eleven
supplied-DOS invocations per CPU with balanced parser/device-process cleanup;
receipt: `artifacts/lock-wb31-native-20260917-qualified-v2/qualification.json`.
This does not claim classic packet/output parity, PURE admission, lifecycle,
packaging or shipping.
Plan/inventory SHA-256: `f36ac7aa8c3d9eff6ae0e09dbd9b43ae791c8331fbce94159012670fe6d9b2e8`.

## 2026-09-17 - Workbench DiskChange syntax candidate

Added `NativeWorkbench31DiskChangeCommand` and its private DOS 36 resident
entry. The Workbench Disk 1/2 HUNK exposes `DEVICE/A`; the candidate shares
the public `DeviceProc`/`ACTION_INHIBIT` body with the source-bound MorphOS
profile while keeping startup separate. The three-CPU qualification runs ten
supplied-DOS invocations per CPU with parser, device, two-stage packet,
startup-boundary and interleaved cleanup checks; receipt:
`artifacts/diskchange-wb31-native-20260917-qualified-v1/qualification.json`.
This remains syntax-candidate evidence and does not claim classic parity,
PURE admission, lifecycle, packaging or shipping.
Plan/inventory SHA-256: `f36ac7aa8c3d9eff6ae0e09dbd9b43ae791c8331fbce94159012670fe6d9b2e8`.

## 2026-09-17 - Workbench Reboot syntax candidate

Added `NativeWorkbench31RebootCommand` and its private DOS 36 resident entry.
The Workbench Disk 1/2 HUNK exposes an empty-template Reboot command; the
candidate shares the public `ReadArgs`, Ctrl-C and `ColdReboot` boundary with
the source-bound MorphOS body while keeping startup separate. The three-CPU
qualification runs ten supplied DOS/Exec invocations per CPU, including
ignored tails, parser/signal/open failures, startup boundaries and interleaved
reuse; receipt:
`artifacts/reboot-wb31-native-20260917-qualified-v1/qualification.json`.
This remains syntax-candidate evidence and does not claim classic reboot
parity, PURE admission, lifecycle, packaging or shipping.
Plan/inventory SHA-256: `f36ac7aa8c3d9eff6ae0e09dbd9b43ae791c8331fbce94159012670fe6d9b2e8`.

## 2026-09-17 - Reference-closure blocker receipt

Recorded the current external-evidence boundary in
`artifacts/reference-closure-20260917/closure-status.json`. The local corpus
contains only Workbench disks 1 and 2 of 6; disks 3 through 6 and machine
variants are unavailable. The complete MorphOS 3.20 ISO is present, but the
installed `MOSSYS:C`/`SYS:C` overlay and resident metadata have not been
observed. The extracted MorphOS Assign member has a hash-bound 5,399-byte
payload, but no safe template can be recovered from its packed strings. The
receipt also records the empty production `filesystem/SYS/C` and the resulting
zero shipping command/profile counts. This is a reference and admission
blocker record, not a command qualification.

## 2026-09-17 - Assign MorphOS public-vector candidate

The MorphOS command reference supplies the eleven-option Assign grammar, which
differs from the Workbench candidate by omitting `EXISTS`. Added
`NativeMorphOSAssignCommand` and its private resident entry using the public DOS
assign vectors and invocation-owned `/M` target traversal. The 68000/020/040
resident HUNK qualification is recorded in
`artifacts/assign-morphos320-native-20260917-qualified-v1/qualification.json`.
LIST, DISMOUNT and VOLS/DIRS/DEVICES intentionally fail closed until the packed
MorphOS 51.1 member is decompressed or exercised in a licensed guest; no
shipping count changes.

The current runtime receipt,
`artifacts/assign-morphos320-native-20260917-runtime-v3/qualification.json`,
passes nineteen supplied public-DOS invocations per CPU with balanced
ownership and no shared image writes. This remains bounded native evidence;
packed correspondence, original guest behavior, lifecycle/PURE admission,
packaging and differential parity are open.

## 2026-09-17 - Assign Workbench public-vector candidate

Added the first Workbench Assign implementation slice. The body retains the
hash-bound classic `NAME,TARGET/M,LIST/S,EXISTS/S,DISMOUNT/S,DEFER/S,PATH/S,
ADD/S,REMOVE/S,VOLS/S,DIRS/S,DEVICES/S` candidate, routes mutation through
the public DOS assign vectors, and keeps target vectors and locks invocation
owned. Successful `AssignLock`/`AssignAdd` calls transfer their locks to DOS;
failed operations and `RemAssignList` comparison locks are released before
parser teardown. The private entry compiles cleanly as resident HUNK images
for 68000, 68020, and 68040. Receipt:
`artifacts/assign-wb31-native-20260917-runtime-v4/qualification.json`. The
independent fixture passes nineteen supplied public-DOS invocations per CPU,
including mutation modes, multi-target ownership, parser/allocation and
startup failures, and interleaved callers; no shared image writes occur.

The original Workbench image was then launched through the licensed ROM for
two unterminated-quote parser edges. Both images agree on ReadArgs error 120,
PrintFault 120, FreeArgs(NULL), no VPrintf, and no filesystem calls, but the
original leaves a command-image-derived post-run IoErr while the candidate
leaves 120. Durable receipt:
`artifacts/assign-parser-edges-20260917/qualification.json`. The parser
sequence is therefore bounded evidence, not a closed parity or shipping gate.

LIST, EXISTS, DISMOUNT, VOLS, DIRS, and DEVICES remain explicitly fail-closed
until original Workbench and MorphOS captures establish their behavior. This
slice does not claim guest parity, handler success, PURE admission, lifecycle
reuse, packaging, or shipping; the ledger remains 0/200 and 0/246.

## 2026-09-17 - Rename original-DOS parser-edge harness

Added `KickstartRomRenameCommandLaunchTests.cs` beside the existing original-DOS
`RunCommand` observer. It binds the private Workbench Rename 37.2 HUNK by its
1140-byte SHA, binds the source-generated 68000 HUNK and map by their current
hashes, and verifies the resident/no-managed-allocation map claims before
loading either image. Both images are run through the real Kickstart 3.1 DOS
`RunCommand` path with public NIL streams and the exact
`FROM/A/M,TO=AS/A,QUIET/S` template. The bounded empty-line and unterminated
quote inputs compare return 20, ReadArgs failure, PrintFault/IoErr and cleanup
without filesystem calls or `FreeArgs` on a failed parse.

The shared observer supports this Rename parser profile without weakening the
existing MakeDir/Copy assertions. With the licensed Kickstart 3.1 V4063 ROM
and explicit private/reference paths, the sibling test build succeeds with
project references disabled and the real guest run passes: four invocations
(original and generated images, two inputs) and two comparisons. Empty-line
and unterminated-quote inputs return 20 with ReadArgs failure and exact
116/120 `IoErr`/`PrintFault` codes; failed parses call no `FreeArgs` and only
show the observed `UnLock(NULL)` common-cleanup vector. The receipt is
`artifacts/rename-parser-edges-20260917/qualification.json` and the TRX is
kept in the sibling test project. This closes only the bounded A01/A03 parser
edge slice; positional/alias/quoting/help/pattern coverage, MorphOS
correspondence, resident/PURE, packaging and shipping remain open. Shipping
stays 0/200 and 0/246.

Plan/inventory SHA-256: `a19ce85e45b64d17d5ce3995fbbd69879611d47343a6d5e510a3dd870b4cdd97`.

## 2026-09-17 - Rename E02/E04/R03 policy matrix

The bounded Rename error-policy gaps are now checked independently for both
profiles. `tools/Commands/verify_rename_error_policy.py` validates the existing
hash-bound native receipts on 68000/020/040: Workbench covers direct and
directory `Rename(FALSE)` with `IoErr=0`, failed output and PrintFault ambient
errors, and cleanup-time secondary errors; MorphOS covers selected-error output
poisoning, the zero-error mutation case, parser fault ambient changes and
successful cleanup-time error changes. The verifier selects one sequential case
per CPU, checks the exact prefix/result/fault policy and 4096-byte stack bound,
and binds both qualification and runtime report hashes.

Receipt: `artifacts/rename-error-policy-20260917/qualification.json`;
verifier SHA-256 `90f2eccade86dba816cb6a16607923fafdc9584eda8c11d0bfca937cc657ab22`.
Plan/inventory SHA-256: `f906f830808220ea5c58c0b3ea5429e8ee7f832497924e279e5404336a7bc439`.
This closes only the supplied-vector E02/E04/R03 rows. Real handler and original
MorphOS output/IoErr behavior, full parser coverage, resident lifecycle, purity,
packaging and shipping admission remain open. The remaining-work audit now marks
this bounded item complete and points to the next real ReadArgs/handler gate.

## 2026-09-17 - Relabel FFS name rejection and recovery

The previous goal turn made progress on cold remount. This turn qualifies
CC12.Relabel.wb31.names.1: four original/replacement pairs against writable
owned DOS1 disks. Thirty name bytes succeed at 0/0. Thirty-one bytes, empty NAME
and Left/Right reach DOS unchanged, fail at 20/210 and print exactly
object name invalid plus LF. ReadArgs succeeds; no frontend truncation, default
name or path sanitization replaces the handler's policy. Intermediate root
sectors prove accepted/rejected label effects before recovery, with unchanged
non-root hashes. Each subsequent SavedDisk rename returns 0/0; payloads,
metadata, allocation and resident/parser/library/private storage remain valid.

Evidence: `artifacts/relabel-name-boundaries-20260916-v2/matrix.json`, with four
hash-bound pair receipts, eight passed TRXs and sixteen corruption controls.
The first matrix remains incomplete: Wait 3 prevented the later script stages
within the fixed boot bound. Removing the unnecessary wait lets direct sector
checks and recovery finish; no execution limit or assertion was relaxed.
Its eight captures and explicit not-qualified.json are retained separately.

The new observer only reads disk root/non-root state at command entry. No
production command, Shell, CPU, handler or scheduler behavior changed, and both
command HUNKs are unchanged. Build succeeds with three existing warnings.
Current verifier/dependency/TRX/observation/control hashes, Python compilation
and whitespace checks pass. All launched processes are terminal. Artifact names
retain the September 16 preparation date; qualification finished September 17.

Next cover OFS and remaining handler-specific character/name behavior through
public DOS. Original PURE classification, platform/launch, complete process
ownership and package gates remain open. Shipping stays 0/200 identities and
0/246 profiles; six inventory reference closure items remain. These four FFS
paths remain separate from the twenty earlier parser/argument cases.

Plan/inventory SHA-256: `9689ad02caa8ff82363cd9eb9b26ed1f67794f8b0f85a76572bb658c839519bc`.

## 2026-09-16 - Relabel saved-volume cold-remount

The previous goal turn made progress by qualifying persisted DOS1 sectors.
CC12.Relabel.wb31.persist.2 now boots a fresh machine with each exact exported
reference/replacement data disk, write-protected. Input receipts bind and
revalidate the prior comparison, observations, actual TRXs and output hashes.
No guest state is restored. Original Mount resolves SavedDisk:, Type reads the
proof by device and volume name, and all 1792 nested guard bytes are read and
matched by count/hash at the actual Close return. Both guests reach Wait.
Neither loads or invokes Relabel, and every disk byte remains unchanged.

Evidence: `artifacts/relabel-cold-remount-20260916/qualified/comparison.json`.
Fresh boot TRXs pass (23/25 seconds); twelve cold-remount corruption controls
reject. The previous writable-disk captures also revalidate with their twelve
controls, recorded separately in `source-controls.json`; these are not new
writing boots. The isolated build passes with three existing warnings, and
Python compilation passes. No production command, Shell, CPU, filesystem,
device or scheduler code changed. The observer adds a fresh read-only mode and
bounded hashing of the larger nested-file read.

This closes bounded DOS1 cold-readback without adding command option counts.
Explicit Mount/ENV: setup and host Exec/device overlays remain. Automatic drive
discovery, OFS and other filesystem/name-boundary cases, other CPU/launch paths,
original PURE classification, full process ownership and package admission stay
open. Continue those per-profile gates; no inventory scope is dropped and no
shipping/PURE status is promoted. All work launched this turn has finished.

Plan/inventory SHA-256: `cdbab96cd3cc2bf667c21cae8f4fed4632bdeaefd606454827514def18f60192`.

## 2026-09-16 - Relabel persists a writable FFS volume label

CC12.Relabel.wb31.persist.1 runs original and replacement Relabel against an
owned writable DOS1 DF1. Each performs device-name then volume-name renames,
returns 0/0 with empty output, preserves per-invocation parser/library/storage
ownership, reuses one unchanged resident image and removes it afterward.
The handler returns BOOL -1 and IoErr 0; RAM success's ambient 210 is not a
universal success result. Type verifies payload access via each volume name.

The observer exports actual device sectors to a new file. Independent readback
proves the final SavedDisk label and valid root checksum; only root label/date/
checksum fields change. Every other sector, payload, file protection, directory
entry and allocation bitmap remains intact. Final receipt:
`artifacts/relabel-persistent-20260916-v6/qualified/comparison.json`.
Both TRXs pass (23 seconds each), and twelve corrupted media/trace controls
reject. The unchanged HUNKs use the v5 isolated observer build; no production
command, Shell, CPU, DOS, device or scheduler implementation was modified.

Initial probes remain with explicit non-qualification records. One-drive
configuration was corrected to two drives; device/volume reads still waited
before Relabel. Explicit original Mount then waited in locale.library. A
bounded read-only EasyStruct capture identified a missing ENV: volume. Original
MakeDir/Assign establish empty RAM-backed ENV:, and original Mount uses explicit
unit-1 DOS1 geometry. This fixes fixture prerequisites without qualifying
automatic DF1 discovery. The private boot derivative remains write-protected;
only our data-only DF1 is writable.

Fresh eight-case baseline boots also pass (31/30 seconds), with twelve controls:
`artifacts/relabel-persistent-20260916-v6/baseline-current/qualified/`.
The earlier `baseline/` replay used obsolete media and was rejected for startup
hash mismatch; it is retained and does not qualify. Build succeeds with three
existing warnings. Python compilation and inventory/media verification pass.

Cold-remount, other filesystem/CPU/launch variants, original PURE classification,
complete process-resource cleanup and shipping/package admission remain open.
Next verify a fresh guest can mount/read the exported volume, then continue
those profile gates. No option or command is dropped; shipping remains 0/200
identities and 0/246 profiles, with six inventory reference closure items open.

Plan/inventory SHA-256: `2c17c3d013a4763f488915643e198777d556568d4d42bd5b85fd13ef97aff70b`.

## 2026-09-16 - Captured caller storage reclamation

The caller-retirement follow-up now records exact releases of its four owned
tc_MemEntry spans, including the Process and stack: 3520 rounded bytes per
worker. Both fresh original/replacement boot captures pass. Immediate free-list
coverage proves release before subsequent allocator reuse; end-of-boot free
snapshots alone did not establish this. The route is original ROM RemTask with
host FreeMem, not the host deferred-reaper route. Original cleanup still uses
the retiring user stack before its final switch, so it provides no waiver of
the separate host cleanup handoff requirements.

Evidence: `artifacts/relabel-caller-memory-20260916-v2/qualified/comparison.json`.
Current verifier/dependency, observations and TRX hashes rechecked; 17 storage,
20 retirement and 21 concurrency/refusal corruption controls pass. Both new
Python tools compile. The initial metadata-only probe remains non-qualifying.
No production command, Shell, CPU or scheduler behavior changed. Arbitrary
resource cleanup, forced termination, native host handoff, original PURE
classification and full platform/package admission remain open.

The intervening user status turn made no implementation progress. On resuming,
the ledger's plan binding was synchronized and inventory verification passed:
200 identities, 246 profiles, six open reference closure items, zero shipping.
Next exercise persistent writable-volume behavior, which RAM-only success
cases do not prove.

Plan/inventory SHA-256: `e7306d5a0148d50a07520bb3209e88f0a27e79a089c922ab37af757652fa0162`.

## 2026-09-16 - Normal caller retirement and media availability recheck

The previous goal turn made progress with missing/invalid replacement-source
retention. This turn first rechecked the whole-command inventory prerequisite:
the known private Workbench input directory still contains only the admitted
Install and Workbench ZIPs, with matching hashes. The dated availability
capture is `reference-captures/wb31-local-media-availability-20260916.json`.
Extras, Fonts, Locale and Storage remain unavailable there; their scope is not
removed and no absence-of-commands or installed-metadata inference is made.
Other executable work remains, so this is not a blocked whole goal.

Existing overlap captures showed the worker disappearing from periodic task
lists, which did not establish how it retired. The observer now reads its
public RemTask entry and terminal frame, known registry node, next task switch
and subsequent ready/wait topology. Fresh original and replacement boots both
observe RemTask(NULL) after the background Relabel returns, no active command
calls, a still-live image and use count 1. The worker does not return to its
retired frame and stays absent from valid task lists. The parent then performs
a successful recovery call on the retained image and removes the same node.

Evidence: `artifacts/relabel-caller-retirement-20260916/qualified/comparison.json`.
Both fresh boot TRXs pass. Twenty retirement corruptions and 21 existing
concurrency/active-replacement corruptions reject. Previous successful and
failed replacement captures re-verify with 24 and 56 controls; these are old
captures, not additional fresh boots. The receipt binds imported concurrency
and common verification sources as well as media, binary and executor hashes.
Build succeeds with three existing warnings; Python compilation and current
receipt/TRX/dependency binding checks pass. All boot processes are terminal.
No command, Shell, CPU, scheduler or device production behavior changed.

This is normal self-removal/unlink/switch evidence under original DOS/Shell
and host Exec/device takeover. It does not prove complete process-memory
reaping, pending packet/notification cleanup, abrupt task death or removal
while command allocations remain live. These limits are explicit in the
receipt and [boot report](relabel-original-dos-boot.md). Next distinguish
process-storage reclamation from unlinking and qualify applicable termination
ownership, while keeping original design, launch/platform and package gates
open. No PURE or shipping gate is promoted; the full 200 identities / 246
profiles and six inventory closure items remain unchanged.

Plan/inventory SHA-256: `ff37b27106f0b21774742453f6cc561c831d96edfa690a2af9c8d4d2dff1146f`.

## 2026-09-16 - Relabel failed replacement retains node and segment

The previous goal turn made progress by proving idle replacement with two
separately tracked image lifetimes. This turn closes two further retention
cases using actual original DOS/Shell calls: a missing source and an existing
16-byte text payload that cannot load as a HUNK. Both original/replacement
pairs complete under one isolated observer build, without patched results,
injected loader failures or changed CPU/device/command implementations.

Missing source fails at Lock with 205 before LoadSeg. Existing invalid source
is locked and examined, its lock is released, then LoadSeg returns zero/212.
FreeArgs retains 212, but original Resident passes 205 to PrintFault and returns
5/205 with exactly `object not found` plus LF. The missing path also returns
5/205. This observed mapping is preserved, not replaced by the loader error or
by the active-replacement refusal level 10. It does not change CopperOS's
separate MorphOS Shell-owned Resident implementation.

Both failure paths keep the same node, count 1 and loaded image. Two later
Relabel calls succeed on that image; all three calls return 0/210 with matching
DOS operations and actual payload/diagnostic readbacks. Final REMOVE returns
0/202 and frees the one segment after all callers return. Code stays unchanged;
the candidate's six allocations, three RDArgs and three DOS leases balance.
No AddSegment/RemSegment/unload occurs during either failed attempt.

Evidence: `artifacts/relabel-failed-replace-20260916/missing/qualified/comparison.json`
and `invalid/qualified/comparison.json` under the same parent. Four boot TRXs
pass. The combined controls receipt rejects 56 corruptions (26 missing-source,
30 invalid-source), including lost registry retention, wrong result/error,
wrong lock ownership, wrong generation, missing/early release and altered
readbacks. Shared per-call verification was extracted from the successful
replacement checker; the old successful captures re-qualify under
`prior-idle-qualified/` with all 24 controls. Those prior captures are retained
runs, not fresh boot claims. Compilation succeeds with three existing warnings;
Python compilation, source/observation/TRX hash checks and inventory validation
pass. All test processes are terminal; no wait or external blocker remains.

The scope remains 200 identities / 246 profiles, with zero shipping admission
and the same six open inventory closure items. No gate is promoted by these
helper lifecycle workloads and the twenty distinct Relabel option scenarios
are not inflated. Next address other loader failures and applicable task/process
retirement, then original PURE classification, remaining launch/platform and
package gates. Full completion remains unproven; the goal stays active.

Plan/inventory SHA-256: `d819cc72d9d1bf09758ff0a7cb909b001aa3814f9a46e4ab8729ea3708c4cbce`.

## 2026-09-16 - Relabel idle replacement and two image lifetimes

The preceding implementation turn produced paired active-replacement-refusal
evidence; the intervening status-only reply did not advance implementation.
This turn revalidated those receipts, completed their documentation, then
implemented and ran the next lifecycle case: successful replacement while idle.
No permission or external-state blocker prevented this work.

Both the original Relabel 37.2 and unchanged replacement HUNK pass. Original
Resident loads the second image before unloading the first and updates the
same registry node without remove/re-add. REPLACE returns 0/0, the node remains
at count 1, and both later Relabel calls use the new image. All three command
calls return 0/210 with matching public DOS operations and actual payload/
diagnostic readbacks. Final REMOVE returns 0/202, preserving original ambient
error behavior. Both image allocations free exactly once, and all six candidate
allocations, three parser objects and three DOS leases balance.

The observer previously tracked only the most recently loaded image. It now
records separate image generations, load/free intervals and allocation/hash
identities and watches CPU writes to all still-live images. Old-image integrity
is checked again at its release, after loading the replacement. It explicitly
rejects unsupported loading while callers are active or calls to an older live
generation; this idle test does not claim arbitrary multi-segment concurrency.
The media verifier checks each image's relocation at its actual load address.
No command, Shell, CPU, device or other production runtime implementation was
changed. Licensed original media and derivative images remain outside the repo.

Final evidence: `artifacts/relabel-idle-replace-20260916/qualified-v2/comparison.json`.
Both boot TRXs pass and 24 corruptions reject. Fresh active-refusal boots on the
same observer pass under `active-regression/qualified`, with 21 controls. The
previous removal, overlap, baseline and extended/cancellation captures re-verify
with 20/12/12/20 controls respectively; these older captures were not rebooted.
The first idle `qualified/` result is retained as intermediate evidence before
the final operation comparison. Build succeeds with three existing warnings;
Python compilation passes. All four boot processes are terminal; no live wait
remains. See [boot report](relabel-original-dos-boot.md) for exact hashes and
reproduction.

Shipping remains 0/200 identities and 0/246 profiles, with the same six open
inventory closure items. Existing Shell internals retain their owner. The next
slice is failed replacement loading and retention of the old usable segment,
then applicable task-death/failure lifecycle, original purity classification,
launch/platform coverage and packaging. Successful replacement by the same
HUNK is not proof of replacement by a different command or while active. Full
scope remains unchanged and the goal is not complete or blocked.

Plan/inventory SHA-256: `1084da8119337493fdde81c2b7a8f0b021cd9da986039a06485e398363c7204e`.

## 2026-09-15 - Relabel active-removal refusal and retained registry lifetime

The previous turn made progress with two-task resident overlap. This turn adds
ordinary Resident REMOVE while the background Relabel is active, followed by
another caller, Cancel, recovery and final removal. Both original/replacement
boots now pass this sequence. The early RemSegment returns zero at count 2,
Resident returns 5/202 and writes exactly `object is in use` plus LF. A later
FindSegment returns the same node/segment; the overlapping RAM call succeeds.
After all three Relabel returns, removal succeeds at count 1 and Resident
returns 0 while preserving observed IoErr 202. Segment storage is freed once.
The command results, separate live parser/storage, six replacement allocation
releases and exact diagnostic/payload readbacks remain correct.

The first probe is retained under `artifacts/relabel-active-remove-20260915/`.
It observed refusal and RAM progress, then stalled at Cancel after another
console window opened; the script-wait loop eventually overflowed the bounded
trace. It is not a pass. Fresh v2 media redirects only the intermediate Type
display to NIL: and preserves its real payload reads. That controls fixture
console interference, not a general focus-routing fix. No execution limit,
command implementation, device or CPU behavior was changed.

Final evidence is `artifacts/relabel-active-remove-20260915-v2/qualified/comparison.json`.
Both final TRXs use the initial probe's isolated observer build. Twenty
corruption controls reject, including wrong removal acceptance/count/node,
missing retained lookup, wrong helper result/error and changed warning bytes.
The previous concurrency pair also re-verifies with twelve controls. The actual
first failed TRX is rejected explicitly. Compilation and Python validation
pass; the build has three existing dependency/analyzer warnings. All boot
processes have terminated and there is no live wait.

This closes the bounded ordinary-removal-refusal case, not forced removal,
active replacement or task death. Remaining launch/platform, original PURE
classification and packaging gates stay open; shipping remains 0/200 and
0/246. The full objective is unchanged. See [boot report](relabel-original-dos-boot.md)
for exact scope, hashes and reproduction. Next address active replacement/other
registry mutations, task-death cleanup and remaining launch/platform cases.

Plan/inventory SHA-256:
`6a457354ff742c4895f8990cd0deeb3c90404388647b59c4c2ac282b3b4fc254`.
Extraction passes with the same six closure items.

## 2026-09-14 - Relabel two-task resident overlap and owner cleanup

The previous goal turn made progress by repairing input forwarding and closing
bounded requester cancellation. This turn adds a paired original/replacement
concurrency workload using the same native command HUNKs. Original Run/Execute
launch a background protected-DF0 invocation; after parent priority demotion,
RAM relabeling begins and returns while the background requester remains open.
The fixture only sends Cancel after it observes two active owners. A guest
completion marker precedes parent recovery and final resident removal.

Both boot tests pass. One loaded segment serves two tasks and three calls, with
explicit per-owner entry/return cycles. Each binary returns 20/214 for protected
DF0 and 0/210 for the overlapping RAM call and recovery; exact diagnostic and
volume payload readbacks match. RDArgs pointers are distinct while live. The
replacement holds two separate allocations per caller during overlap; all six
allocations across the workload are freed by their owner with correct sizes.
All parser/library leases close, code is unchanged and segment removal/free
occurs after all returns. No command, CPU or device implementation was edited.

`artifacts/relabel-concurrent-20260914/qualified-v2/comparison.json` binds final
verification and full observations. The initial `qualified/` receipt remains
separate; v2 adds explicit parser/handler order, result-array ownership and
ordered DOS comparison using the same captures. `controls.json` rejects twelve
ownership/lifetime/input/output corruptions. `prior-scenario-controls.json`
revalidates the earlier three positive argument/help/cancellation pairs and
twenty negative controls; it does not claim fresh execution of those scenarios.
The isolated observer build passes with three pre-existing warnings. Both
boot processes terminated normally; no live wait remains.

This is bounded concurrency evidence under the recorded host Exec/device
provider, not full PURE or shipping admission. Next exercise registry/removal
changes while active and task-death cleanup, then the remaining launch/platform,
classification and package requirements. The entire 200-command/246-profile
objective remains intact and shipping counts remain zero. See
[boot evidence](relabel-original-dos-boot.md) for reproduction and build hashes.

Plan/inventory SHA-256:
`ed0ac6c29291b21f1091da74c58f68abc36bb8722eb17918b4999854e68168ac`.
Extraction passes with the same six reference-closure items.

## 2026-09-14 - Relabel requester cancellation unblocked by input-service fix

The preceding goal/status turn was no progress. Revalidation found the raw-CIA
Cancel test terminal and failed. A separate probe through CopperScreen's normal
host-keyboard entry point delivered four correctly qualified events, but still
waited. Read-only handler/BeginIO/requester/Wait observations then exposed
reinstallation of InputDeviceServices while it temporarily forwards to ROM.
The boot loop rediscovered the service during that interval because IsInstalled
depended on the removed gateway. It now depends on allocated service state.

The regression first fails at the false installation state in
`artifacts/relabel-input-forward-regression-20260914/before.trx`. After the
production fix, all eighteen focused keyboard/input tests pass. Both original
and replacement protected boots now complete under the same executor; the
guest consumes B-down, original EasyRequestArgs returns zero, and both commands
return 20/214 with `disk is write-protected` plus LF. Help/EOF/continuation and
recovery then complete. All seven replacement direct allocations are released;
both binaries close three parser/four DOS leases and remove/free the segment.

Final artifacts are `artifacts/relabel-input-forward-fixed-20260914/`:
`qualified/comparison.json` checks four paired cases and real media/readbacks;
`controls.json` rejects twenty corruptions/incomplete traces. Two fresh baseline
boots and `baseline-qualified/comparison.json` also pass all eight cases under
the fixed runtime; `baseline-controls.json` rejects twelve more corruptions.
The original/replacement native HUNKs are unchanged. Earlier failed input probes
remain tied to their old builds. All test processes have terminated; no live
wait remains. No full CPU/chipset suite or fully original OS claim is made.

The protected cancellation gap is closed within the explicit host Exec/device
provider. Twenty distinct Relabel scenarios now have paired original-DOS
evidence. Next execute remaining handler/parser cases and Workbench launch,
then concurrent resident lifecycle, original classification and packaging;
the complete 200-command objective remains unchanged. Shipping stays 0/200
identities and 0/246 profiles. See [boot report](relabel-original-dos-boot.md).

Plan/inventory SHA-256:
`b42db83ae9eef61d264f761a12ba6f9ad16b6d1b2aa30760cfb7dfc52bdf0e86`.
Inventory extraction and validation pass with the same six closure items.

## 2026-09-13 - Relabel extended parser coverage and protected-disk wait diagnosed

The previous turn was progress. This turn adds eleven completed original-DOS
comparison cases on the unchanged 68000 HUNK: duplicate keywords, unknown tokens,
lowercase keywords, escaped quotes/stars, empty NAME, ReadArgs help/EOF/redirected
continuation and recovery. All exact result/error, ordered operation and output
comparisons pass. Six more volume-label payload readbacks and eleven output-file
readbacks pass. Receipts: `artifacts/relabel-extended-20260913/final-arguments/`
and `final-help/`, each with `comparison.json` and full observations.

The argument scenario balances thirteen directly observed replacement
allocations; help balances five. Both maintain matched parser/DOS library
leases and sequential resident registration/reuse/removal. Verifier development
corrected assumptions about terminal EOF for empty/unterminated files and BPTR
reuse by Type/Wait after the command is removed. Open/readback identity is now
bounded to each match. The existing baseline re-verifies successfully in
`baseline-regression/`, and its twelve controls still reject; the new controls
reject ten corruptions and both real incomplete protected-disk captures.

The protected-DF0 experiment did not complete. Both original/replacement traces
enter DOS Relabel and remain active at the unchanged bound. Read-only observer
extensions then record the original's RenameDisk packet request and matching
handler reply (result 0/error 214), followed by an unreturned Intuition
EasyRequestArgs. No public ErrorReport vector entry was observed. The confirmed
pending requester supersedes the earlier unproven requester inference.
`protected-wait.json` binds the final original-only `dialog-reference.trx` and
explicitly says incomplete, zero completed commands and no shipping/PURE approval.
Earlier `requester-*` and `packet-reference` builds/traces remain separate. All
test processes have terminated and their guests are disposed; there is no live
wait handle. No return value, write-protection flag, CPU state or private DOS
structure was patched to force completion.

Next use controlled input to cancel the actual requester and verify final return,
output and cleanup for both commands. This remains an open integration path,
not a global impasse. Remaining lifecycle/platform/classification/package gates
also stay open. No command source or CPU/handler implementation changed; no
broad native command-suite rerun is claimed. See [boot evidence](relabel-original-dos-boot.md)
for the complete case table and reproduction. Shipping stays 0/200 and 0/246.

Plan/inventory SHA-256 is now
`8ac2f274dbe085fd12af7bd758ef73d20496e2410958406834e27dd18af09b10`.
Inventory extraction passes with the same six closure items.

## 2026-09-13 - Relabel paired original-DOS boot, diagnostics and resident reuse

The previous turn was implementation progress. This turn moves the unchanged
Workbench Relabel HUNK from supplied-vector evidence into autonomous original
Shell/DOS execution. The passive sibling observer now admits Relabel, records
its list/mutation arguments and final RunCommand IoErr, and retains existing
boot bounds without changing CPU, chipset, DOS or handler implementations.

Paired private derivatives change only startup and the existing C:Ed carrier.
Each binary completes eight cases: quoted positional name, reordered keyword/
equals, assign lookup, invalid drive/name, missing/extra arguments and recovery.
Original Type reopens the unchanged payload through four new volume names and
reads four redirected diagnostic files byte for byte. The final verifier checks
actual disk blocks and loaded relocated HUNK bytes, not only media receipts or
success markers. Return levels, final errors and ordered public behavior match;
successful relabeling leaves the observed ambient IoErr 210 on both sides.

The final evidence is `artifacts/relabel-boot-20260913/qualified/comparison.json`;
both diagnostic TRXs and media receipts are adjacent. One segment is registered,
used eight times sequentially, removed and freed per binary. All six parser
leases and eight DOS leases close. The replacement has thirteen directly
observed allocations and matching releases, versus none in the original.
`controls.json` rejects twelve targeted evidence corruptions. Earlier simple
boot runs also completed; verifier development initially corrected the emulator
assembly filename, Type's size-based reads without an extra EOF call, and HUNK
relocation handling. Negative controls were scoped to the command's parser and
resident entry after initial probes removed unrelated Shell events. No failed
or incomplete draft is promoted as final evidence.

See [paired boot report](relabel-original-dos-boot.md) for exact hashes, commands,
readbacks, reproduction steps and provider limits. The original DOS vectors are
checked, but host Exec/device takeover remains part of the emulator build.
Neither forced PURE use nor unchanged code establishes original classification
or full concurrent resident admission. Remaining parser/handler cases, Workbench
launch, other CPUs in the real-OS fixture, MorphOS correspondence and packaging
remain open. No command source changed in this step; no broad command-suite
rerun is claimed. Shipping remains 0/200 and 0/246.

Plan/inventory SHA-256:
`07cb767786aa2ed6f43216611dcfdd4c7840d36c059de72f591f66bb3c52b0bc`.
Inventory extraction passes with the same six reference closure items. The
isolated observer build succeeds with three pre-existing dependency/analyzer
warnings; both final boot tests and all twelve negative controls pass.

## 2026-09-13 - Relabel Workbench behavior corrected from original execution

The preceding status-only turn was no progress. This turn executed the pinned
Workbench Relabel 37.2 and replaced the shared MorphOS assumption with an
independent classic body. Reference differences include assigns in DOS-list
lookup, removal of the final DRIVE byte without colon validation, and ambient
output/handler/FreeArgs errors. The missing-DOS startup now records Result2 122.
ReadArgs and scratch storage remain invocation-owned, and no parser-owned buffer
is changed. The original's buffer mutation is reported separately with explicit
normalization; unsafe underwrite/overflow behavior is not reproduced.

The final receipt is
`artifacts/relabel-wb31-reference-20260913-qualified/qualification.json`:
38 original/replacement comparisons per CPU on 68000/020/040, with both binaries
executing on that CPU. In total, 114 original and 123 generated calls pass.
Generated-only cases cover both allocation failures and a 255-byte drive name.
All five-method HUNKs pass native static checks and sequential shared-image,
stack, library, parser and allocation ownership checks. See the
[Relabel contract](contracts/Relabel.md) for exact hashes, cases and normalization.

Retained failing baselines are under `artifacts/relabel-reference-20260913/`.
The first completed 18 pairs; v2 exposed a fixture mistake treating a null list
result as a held lock; v3 completed 30 pairs and demonstrated the old shared
candidate's differences. Candidate and expanded passing receipts are historical;
the final receipt also binds runtime-report hashes and validates HUNK/executor
identity. Driver guard checks reject output reuse and a wrong original before
builds; the existing successful receipt remains byte-identical.

Validation: 623/623 command tests pass; retained MorphOS Relabel HUNKs pass eight
supplied-vector cases each on all three CPUs (24 total) under the updated runner.
The plan/inventory binding is now
`8848d251f9878c54d8fe33a3402305d5bcec110d05d4cdbf3518a6be03ffb061`.
Inventory extraction passes with the same six reference closure items. No
original media bytes were added to the repository. Real DOS parser/help/fault
rendering and handler mutation, Workbench launch, concurrent resident lifecycle,
PURE classification and packaging remain open; shipping remains 0/200 and 0/246.

## 2026-09-13 - AddBuffers Workbench behavior corrected from original execution

The previous turn was implementation progress; this turn adds independent
reference evidence and corrects the prior shared-body assumption. Executing
the pinned Workbench AddBuffers 37.2 produced a failing 21-case comparison
against the old candidate (`artifacts/addbuffers-reference-20260913/baseline-68000-v3.json`).
The classic body now preserves separate change/query calls, omitted versus
explicit zero, signed query results, null-header faults, parser FAIL versus
handler OK, missing-DOS Result2 122 and ambient output/cleanup error timing.

The new reference runner passes 27 original/replacement comparisons plus one
generated allocation-failure case per generated target (68000/020/040).
The original is explicitly executed on 68000 in every comparison. An attempted
original 68020 run stopped on unsupported exact timing for B232; its failed
receipt is retained. See [AddBuffers contract](contracts/AddBuffers.md) for
the evidence paths and limits. The passing receipt is
`artifacts/addbuffers-wb31-reference-20260913-qualified/qualification.json`:
81 comparisons, 81 original calls, 84 generated calls; five reachable methods
and 1,344 bytes per HUNK, SHA-256
`7e67c35a6aff20de7b7710ee253424e296d8421f281234e835083340733f74ea`.

Plan/inventory SHA-256 is now
`5dda04a3f7e7adab2bdba8ab5a567b914618c30b47ab9534e3ad469bbf47d192`.
Real DOS parser/formatter/handler execution, Workbench startup, full concurrent
resident lifecycle, PURE classification and packaging remain open. Shipping
remains 0/200 commands and 0/246 profiles.

Validation: 623/623 command tests pass; inventory verification passes with the
same six reference closure items. The retained MorphOS AddBuffers HUNKs pass
their six-vector suites on each CPU under the updated executor (18 total).
The qualification driver rejects a populated output directory before building
or replacing evidence; its successful receipt remained byte-identical in the
rejection check. No repeated broad qualification-script test was needed because
the common qualification guard implementation was not changed.

Execution record for the [stable goal plan](D:/Koodit/GIT/CopperOS/Goals/WORKBENCH31_MORPHOS_C_COMMANDS_GOAL.md).
Update state in the [completion ledger](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/completion-ledger.md), leaving
the plan unchanged. Entries distinguish observed results, coordinator-reported
work in progress and unexecuted requirements. Never turn a probe, source note,
or host test into a shipping-command claim.

## 2026-09-12 - MorphOS SetDate bounded native slice

The MorphOS 3.20 `SetDate` identity is bound to
`artifacts/setdate-morphos320.iso.bin` (2050 bytes,
SHA-256 `1A4B0B91A9BA910C9E9D7E25A95078913302C44397405A0E28BDB8F69E486BD9`).
The released source contract confirms the classic ReadArgs template, v37 DOS
open, date-first/time-fallback conversion, literal invalid-token diagnostic,
version-gated extended AnchorPath soft-link policy, reverse directory traversal,
and soft-link update compatibility behavior. The source was not copied into the
repository.

`NativeMorphOSSetDateCommand` and its private root compile as resident HUNKs on
68000/020/040. The durable supplied-vector receipt is
`artifacts/morphos-setdate-native-entry-20260912-qualified/qualification.json`:
12 cases per CPU (36 total), including parser/date failures, AnchorPath
extension setup, directory and soft-link paths, matcher/update/break errors,
cleanup, and interleaved callers. All pass with balanced invocation storage and
zero shared-image writes. Original MorphOS guest parity, PURE reuse, lifecycle,
packaging, and differential evidence remain open.

## 2026-09-12 - Workbench SetDate bounded native slice

The Workbench 3.1 M10 `C:SetDate` HUNK was captured and hashed as
`AFFCE0FCE8EEFB7700E7848D04B666781BD14A1C6F9597E73828BE203415DBCE`.
The exact `FILE/A,WEEKDAY,DATE,TIME,ALL/S` template and observed
`AnchorPath`/`StrToDate`/`MatchFirst`/`SetFileDate`/break paths are recorded in
[contracts/SetDate.md](contracts/SetDate.md).

`NativeWorkbench31SetDateCommand` and its private resident root compile for
68000, 68020, and 68040. The durable supplied-vector qualification now runs
ten cases per CPU (30 total), covering ReadArgs ownership, date fallback,
directory flag transitions, no-match, SetFileDate failure, break warning,
cleanup and interleaved callers. All cases pass with balanced allocations and
zero shared-image writes; the receipt is
`artifacts/setdate-native-entry-20260912-qualified/qualification.json`.
This remains bounded ABI/resource evidence: original guest parity, MorphOS,
PURE reuse, differential evidence, and packaging remain open.

## 2026-09-12 - Workbench Date contract and resident body

The Workbench 3.1 M10 `C:Date` HUNK was captured and hashed as
`9568FA98CD28304D185F07E4DD396340DCD36CB18451CA095323712AC0E29AC7`.
Its exact `DAY,DATE,TIME,TO=VER/K` ReadArgs template, DOS/utility/timer
imports, `%s %s %s` output path, `MODE_NEWFILE` handling, and date/time setter
conversion were recorded in [contracts/Date.md](contracts/Date.md).

`NativeWorkbench31DateCommand` now provides the corresponding bounded native
body through public DOS, Exec, utility, and timer.device calls. The private
root compiles as a resident HUNK for 68000, 68020, and 68040; artifacts are in
`artifacts/date-native-compile-20260912/release/`. The ledger advances only
the Workbench Date row to partial Spec/Options/Semantics/Native evidence.
Native execution vectors, original guest parity, PURE reuse, and packaging
remain open. MorphOS `Date` and the separate `SetDate` identity are untouched.

The durable native-entry qualification now runs seven supplied vectors per CPU
(display, `TO`, setter, parser failure, timer-open failure, and repeated
display/setter) on resident 68000/020/040 HUNKs. All 21 invocations pass with
one image load per CPU, balanced guest allocations and libraries, and zero
shared-image writes; the receipt is
`artifacts/date-native-entry-20260912-qualified/qualification.json`. This is
ABI/resource evidence only and does not close the original-guest, MorphOS,
PURE, differential, or package gates.

## 2026-09-01 - Execute equals default and explicit precedence

The original Workbench 3.1 `C:Execute` ran a disposable 68000 WinUAE probe
whose inner script contains `.KEY filename` and `.DEF filename=fallback`. Its
hash-bound capture records `fallback` for the omitted argument and `explicit`
for a following positional invocation, followed by the caller's `END` marker.
This confirms equals-form default syntax and explicit positional precedence for
one value. The resident Shell has a focused matching test; its DOS-owned
`ReadArgs` binding still owns parsing and the per-invocation frame owns only
scratch/default state.

This is not a native Execute qualification. Invalid/empty default behavior,
diagnostics, return level, IoErr, pending/nested lifecycle and MorphOS behavior
remain open, so both Execute rows and `X2` remain partial.

## 2026-09-01 - Execute empty equals default

A second Workbench 3.1 Execute probe establishes a narrow but surprising empty
case. With `.KEY filename` and `.DEF filename=`, a line containing
`<filename>` is suppressed, while the following ordinary line runs; Execute
returns `RC=0` and `Result2=0`. The capture file is NUL-padded by the
disposable guest copy operation, so the record preserves both the raw image
hash and the authenticated text prefix before its first NUL.

`ShellScriptKeyExpansion` now suppresses that expanded source line through its
existing invocation-owned frame data, rather than adding another parser or any
resident mutable state. The regression covers the exact observed line shape.
Whitespace-form empties, broader substitution cases and all error behavior
remain open; this does not qualify Execute or either profile.

## 2026-09-01 - Shell Execute-export native reachability

The full Shell DOS native-root qualification was rebuilt after the empty-default
change with its Execute begin, poll, child and park exports. The 68000 HUNK and
68020/68040 assembly outputs each report a freestanding runtime profile, zero
managed allocation sites and no non-implemented reachable members. This keeps
the source-observed expansion path usable in the shared pure/resident Shell
architecture. It does not create a standalone `C:Execute` program or exercise
reference/runtime lifecycle behavior, so both Execute command rows remain
partial.

## 2026-09-01 - Execute duplicate defaults keep the first value

The original Workbench command accepts two `.DEF filename value` lines instead
of reporting a malformed directive. The first value supplies `<filename>`, all
following lines run, and the probe records `RC=0` and `Result2=0`. The shared
resident default table currently rejects that case. A direct compact-table
extension incorrectly suppressed the following substitution line and was
reverted rather than left as a false compatibility claim. The first-wins design
needs a separately bounded record-layout repair that preserves lookup behavior.

This remains one classic whitespace-form observation. Invalid defaults,
whitespace-form empties, broader duplicate combinations and MorphOS behavior
are still open.

## 2026-09-01 - Execute accepts an undeclared default

The hash-bound [undeclared-name fixture](reference-captures/execute-wb31-def-undeclared.json)
adds a distinct Workbench 3.1 behavior: after `.KEY filename`, the directive
`.DEF missing fallback` is accepted and ignored. The following ordinary script
line runs, and `C:Execute` returns `RC=0`, `Result2=0`. The current bounded
table rejects defaults whose names are absent from the `.KEY` template, so the
reference behavior remains a repair item. This single whitespace-form capture
does not settle empty/malformed values, diagnostics, IoErr, or MorphOS parity.

## 2026-08-30 - Initial execution checkpoint

**Shipping completion remains 0/200 command identities and 0/246 profiles.**
All 200 required identities have explicit owners and all 58 wb31 plus 188
morphos320 profiles have separate Spec, Options, Semantics, Native, Purity,
Package and Differential gates. No family or final acceptance stage is complete.

Base commits observed during the CC02 audit were CopperOS
`6eb97f02cfebba172fb039dd07dac18940484be9`, CopperSharp68k
`46ac6600c3c1a5ce7aceb2236ae744d63aa1cd15`, and CopperStart
`d30497b315e819ce5cd3881bfe975c818088b642`. These identify audit bases,
not clean trees or qualified artifact sources; relevant changes are in progress.
The inventory is bound to plan SHA-256
`4ff2a6c49d57301f8dd528f61f00022b5638d2eaa51016b873b4025d2bed5be5`.
The native qualification owner must record actual source/build identities,
commands and output hashes for each accepted execution.

### CC00.INVENTORY - Verified selected media and tooling

Implemented the read-only
[inventory extractor/validator](D:/Koodit/GIT/CopperOS/tools/Commands/Inventory/inventory.py),
its [usage/schema documentation](D:/Koodit/GIT/CopperOS/tools/Commands/Inventory/README.md)
and corruption/integrity tests. Generated
[command-inventory.json](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/command-inventory.json),
[media-evidence.json](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/media-evidence.json) and
[authorities.md](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/authorities.md). Original vendor files remain outside
the repository.

Observed selected-media results:

- The complete official MorphOS 3.20 ISO is 471126016 bytes, matches published
  MD5 `70b84b8c0bb1cf9b10b7062fe8809c85`, and has SHA-256
  `3761e191124cfd204a490915f8d285d55969e95b66c5c3cd1cf854bc71dab911`.
  This closes the whole-ISO integrity sub-item left open in the planning snapshot.
- Two original Workbench/Install ZIPs and their ADF members are hash-bound and
  inspected read-only. They supply 70 C file entries and 58 distinct identities;
  MorphOS supplies 188 C entries. Their union is 200 identities and 246 profiles.
- Captured 88 MorphOS installer P additions and 18 resident add/remove script
  statements, with source hash/line provenance. Seven classic profiles and 88
  MorphOS profiles require pure design based on observed evidence.
- All classic raw media P bits are clear; MorphOS ISO POSIX modes do not encode
  native Amiga flags. Installed metadata remains unobserved. The other 151
  profile classifications are unresolved, not NonPure.
- Freeze is absent from the entire traversed 3.20 ISO tree and remains an explicit
  CC39 documented/historical discrepancy. HunspellService is included as required.

Commands executed from the repository root:

```powershell
python tools/Commands/Inventory/inventory.py verify-media
python -m unittest discover -s tools/Commands/Inventory -p 'test_*.py' -v
python tools/Commands/Inventory/inventory.py verify --strict-complete
```

Results: `verify-media` rebuilds and exactly matches both exported factual JSON
objects; **13/13 inventory tests pass**. Strict completion returns **2** while
open closure items remain; that is the expected incomplete-coverage result,
not a passing final gate. A PowerShell wrapper can normalize a child nonzero
code; the validator's native return of 2 is also covered by the tests.

Six closures remain: WB31-DISKS-3-6, WB31-MACHINE-VARIANTS,
WB31-INSTALLED-METADATA, MORPHOS320-INSTALLED-OVERLAY,
COMMAND-RUNTIME-CONTRACTS and FREEZE-INDEX-MEDIA. CC00 is partial.

### CC01.CONTRACTS - Five initial contract documents

Added partial evidence/fixture contracts for
[Eval](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Eval.md),
[Execute](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Execute.md),
[Which](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Which.md),
[PathPart](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/PathPart.md) and
[Quote](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Quote.md).
They cover **eight applicable profiles**, with no fully closed contract or
option matrix. No original command was executed by merely creating these docs.

Eval has exact classic binary and MorphOS release-source templates plus
source-observed MorphOS behavior. Shipped-source equivalence and original
classic behavior remain open; source availability does not grant blanket reuse
permission. The other MorphOS templates remain explicitly documentation-backed
where their packed binaries were not decoded or run.

Execute's existing `FILE/A` wrapper delegates to the one existing Shell. Both
profile Semantics gates remain partial. Correct script-tail and native
launch/resume/result integration without adding a second script parser. No
other external command semantics are claimed implemented by this checkpoint.

### CC02.BASELINE - Host tests and ABI/ownership audit

The [baseline and API audit](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/baseline-and-api-audit.md) records the
initial missing `IAmigaGuestMemory`/Support reference failure, coherent project
reference correction, original startup/API evidence and the targeted ReadArgs
array-alignment correction. Relevant sources include
[ReadArgsCommandSupport.cs](D:/Koodit/GIT/CopperOS/src/System/Shell/ReadArgsCommandSupport.cs),
[its focused cases](D:/Koodit/GIT/CopperOS/tests/Commands/ReadArgsCommandSupportTests.cs) and
[the existing command test platform](D:/Koodit/GIT/CopperOS/tests/Commands/EchoCommandTests.cs).

```powershell
dotnet test tests/Commands/CopperOS.Commands.Tests.csproj --verbosity minimal
```

The audit records **232 passed, 0 failed, 0 skipped**: the 209-case command
baseline plus 23 alignment/boundary regressions. These results were produced by
the foundation implementation/audit owners; ledger seeding did not rerun them.
They preserve the existing 31 Shell internal identities and support boundaries.
They do not establish external command option parity, native DOS parser behavior,
boot completion, packaging or resident safety.

The original image-entry ABI allows clobbering all registers except SP. Audit
and qualify actual call/export boundaries separately; do not invent arbitrary
image-entry callee-save obligations.

### CC03.ENTRY / CC04.ARGS - Implementation present, native qualification open

The native startup owner is
[NativeCommandStartup.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeCommandStartup.cs).
The invocation-owned argument lease is
[NativeCommandArguments.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeCommandArguments.cs),
with [ownership and validation notes](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/argument-ownership.md).
They use the existing resident runtime and real public Exec/DOS call boundaries.

The CC04 owner ran:

```powershell
dotnet build src/Commands/Native/CopperOS.Commands.Native.csproj --configuration Release --verbosity minimal
```

Result: C# build passed with zero warnings/errors. This is input-assembly
validation, not a native execution or parser parity pass.

**Coordinator-reported native state at this checkpoint:** CLI startup
success/failure cases run successfully on 68000 in the new harness. The
Workbench path exposes a compiler volatile-register handling defect, reproduced
with peephole optimization disabled. Compiler investigation/correction is active.
The full 68000/020/040 matrix has not passed, and no shipping command is qualified.
Exact harness invocation, emitted HUNK/source hashes and traces must be added to
the separately owned `qualification-report.md` and `build-manifest.json`
before a pass is claimed.

A synthetic library gateway can validate emitted instructions, call arguments
and cleanup. It cannot certify original Kickstart ReadArgs grammar/help, full
Workbench/MorphOS behavior or real boot integration. Required parser-failure,
allocation-failure, error-preserving cleanup, repeated release, overlapping
invocations and per-CPU paths remain tracked under CC03/CC04/CC06/CC09.

### CC00.LEDGER - Complete seed, no hidden removals

Created this progress log and the complete per-profile ledger. Stable row IDs
are `CCxx.Command.profile`, with all owner stages read from the inventory.
Eight rows have partial Spec/Options evidence and two Execute rows have partial
Semantics. Every Native, Purity-qualification, Package and Differential gate is
open; all other Spec/Options/Semantics gates are open.

The ledger keeps 95 required-pure profiles separate from 151 unresolved
classifications, lists all six media closures and retains the uncounted
`CC39.Freeze.morphos320.discrepancy`. Family service lists are dependency
candidates to resolve by command/mode, not barriers that defer entire families.

Ledger consistency verification checks the exact set of 246 inventory-derived
IDs, ownership/profile counts, gate totals, purity counts, contract links and
the unchanged plan hash. Result: **passed**; 200 identities, 246 unique profile
rows, all six closures retained, and every local file link resolves. This verifies
bookkeeping only; no implementation or acceptance gate was promoted.

### Next eligible slices

1. **CC03.COMPILER-VOLATILE:** finish the compiler correction exposed by the
   Workbench path, retaining the failing native case as a meaningful regression.
2. **CC03/CC04 native retest:** rebuild and execute actual HUNKs for 68000, 68020
   and 68040 through CLI/Workbench success/failure, allocation/parser/error cleanup
   and ownership cases; record exact commands, source revisions, hashes and
   evidence limits. Do not convert gateway parsing into original-DOS parity.
3. **First source-backed command behavior:** proceed to a bounded eligible Eval
   slice after the native foundation retest, with independently written semantics
   from verified observations and an explicit license decision before source
   copying or translation. Keep classic and MorphOS differences/reference gaps
   open where not yet captured.
4. **Independent evidence work:** close the per-row next contract slice and any
   available media/installed-reference closure without waiting on unrelated
   networking, GUI, language or device providers. Execute script-tail integration
   remains an explicit next command slice.

Append subsequent dated results here; amend the affected ledger rows and separate
qualification/build records only when their evidence is complete.

## 2026-08-30 09:45 UTC - First passing native startup matrix

`CC03.COMPILER-VOLATILE` was traced to missing external-call clobber metadata,
not to the command source or only to peephole optimization. A target-owned
`ClobberedRegisters` convention property now gives the Amiga resolver D0/D1/A0/A1
for its supported call forms. Machine-IR interference and emitted effects both
consume the metadata. Other external resolvers keep their existing defaults.

The correction is in the CopperSharp owner, limited to `Compiler/PublicApi.cs`,
`Targets.Amiga/AmigaCompilerPlatform.cs`, the relevant clobber/effect branches in
`Compiler/Backend/CilMachineIrBuilder.cs` and `M68kCodeGenerator.Allocated.cs`,
register validation in `Compiler/Metadata/CompilationModule.cs`, and a new
`Compiler.Tests/AmigaExternalCallClobberTests.cs`. Existing unrelated sibling
changes were preserved. No commit or package publication was made.

The independent compiler regression reproduced 24 failures before correction,
including live pointers across WaitPort and Forbid on 68000/020/040, application/
resident profiles, and default/disabled peephole modes. After correction:

```powershell
dotnet test Compiler.Tests/CopperSharp.Compiler.Tests.csproj -c Release --filter FullyQualifiedName~AmigaExternalCallClobberTests -p:SkipCopperScreenHeadlessProjectReference=true --no-restore --verbosity quiet
dotnet test Compiler.Tests/CopperSharp.Compiler.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~M68kRegisterAllocationTests|FullyQualifiedName~M68kInstructionDataflowTests|FullyQualifiedName~OptionalAmigaBindingTests|FullyQualifiedName~External|FullyQualifiedName~ResidentProfile|FullyQualifiedName~CallsExecLibraryVectors|FullyQualifiedName~PlatformBase|FullyQualifiedName~RegisterAbi' -p:SkipCopperScreenHeadlessProjectReference=true --verbosity minimal
dotnet build Compiler.Cli/CopperSharp.Compiler.Cli.csproj -c Release --verbosity minimal
```

Results: **33 focused tests and 549 related tests pass** (the latter includes
the former); CLI Release builds without warnings/errors. The full compiler
suite was not run.

In CopperOS:

```powershell
pwsh -File tools/Commands/qualify_native.ps1
```

Results: **31 native invocations per CPU, 93 total**, all passing. Each target
is an actual 2,176-byte HUNK with resident invocation state, disabled FPU and no
managed runtime/dependency helpers. Each loaded image is reused for repeated
failure/success and instruction-interleaved calls, with independent arguments,
DOS bases, streams, errors, stacks and allocation ownership. No image writes,
stack imbalance or leaked resources occurred. Recompilation reproduced the
bytes. Peak fixture stack writes were 120 bytes, not a shipping-stack guarantee.

HUNK hashes and compiler/SDK identities are in
[qualification-report.md](qualification-report.md) and
[build-manifest.json](build-manifest.json). The complete generated receipt is
`tests/Commands.NativeRoot/bin/Release/net10.0/qualification/dea8404642094e6fbfb580e1b5bc399e/qualification.json`,
SHA-256 `46af48e93d00f6ea57f817b2ba2dc98742dc89cb6b06ef3775d35fbf27f1fc74`.

The native source, probe and instruction executor were added to `CopperOS.sln`;
the solver did not retain automatically added sibling SDK/compiler projects.
No command was staged into `filesystem/SYS/C`, no P bit was approved, and
shipping completion remains **0/200**. The fixture supplies ReadArgs results;
it does not establish original parsing or full process/boot behavior.

This supersedes the preceding immediate-next sequence. Active bounded work:
complete lease edge cases, refresh stale generated DOS provenance and compare
real explicit-source ReadArgs with the licensed original, and implement Eval's
verified numeric-output primitive. Full command/front-end and per-profile
qualification gates remain open.

## 2026-08-30 - Eval numeric-output primitive and fresh DOS provenance

`CC10.Eval.2a` adds
[EvalNumericFormatter.cs](../../../src/Commands/EvalNumericFormatter.cs) and
[58 focused tests](../../../tests/Commands/EvalNumericFormatterTests.cs).
The formatter independently implements the verified numeric primitive: signed
64-bit decimal, unsigned bit-pattern lowercase hexadecimal and octal, optional
hex prefix/LF and exact byte counts in caller-owned guest memory. It allocates
no scratch storage, writes no implicit NUL, checks capacity/mapping/wrap before
writing, and handles the signed minimum correctly even with checked C# arithmetic.
It uses the SDK guest-memory interface and introduces no command-line parser.

Owner validation: focused **58/58**, checked-arithmetic Release focused **58/58**,
and full pinned command suite **290/290**, zero skips. The coordinator repeated
the full default suite with the same 290 passing result. Guards/exact-short
buffers, 64-bit extremes and a warmed zero-managed-allocation check are included.
Source SHA-256: `a65d39d812c9f562ebde99c9a7664b7354b76098b328c1efb08a4e7ed52a8665`.

The MorphOS Eval Semantics row is partial. Classic behavior remains open, and
there is no Eval expression parser, full LFORMAT, TO front end, shipping entry
or native/original-command qualification yet. No source was copied or translated
from vendor implementation files.

For `CC02.API03`/`CC09`, stale CopperStart DOS receipts were rejected rather than
relabelled as current. The coordinating process ran the existing owner pipeline:

```powershell
# D:/Koodit/GIT/CopperStart
pwsh -File build/Build-DosNativeArtifacts.ps1
pwsh -File build/Build-DosNativeArtifacts.ps1 -SkipBuild -ManifestOnly
```

Both passed without DOS shipping-source edits. Fresh MC68000 HUNK: 867,728 bytes,
SHA-256 `2a008cc1ef4dcfc5896a3d958a1454b5d86f23f0d7e833019856257d2f359286`.
The owner also emitted its 68020/040 assembly smoke outputs; those are not HUNK
execution results. Exact provenance hashes are in the qualification report.

The next original/native parser comparison is implemented as two test-only
files at the existing MedPlayer fixture owner, with 44 explicit-source cases,
strict licensed-ROM/generated-artifact requirements, copied typed results and
immediate IoErr/cursor capture. It must prove original/generated native vectors
execute rather than a host parser or fallback. At this checkpoint its test build
is in progress; **no parser comparison pass is yet claimed**.

## 2026-08-30 - Native numeric qualification and bound foundation receipts

`CC10.Eval.2b` now executes the independently written formatter on all required
CPUs. Native compilation exposed unsupported general 64-bit operations, so the
implementation keeps the public `long`/`ulong` interface and uses existing
register-pair split intrinsics with 32-bit/16-bit integer division steps. No
host formatter, managed allocation or floating-point operation is introduced.
The focused host suite is now **61/61**, including 256 mixed-word values in
each of three independent-oracle cases; checked-arithmetic Release is **61/61**
and the full existing command/Shell suite is **293/293**, zero skips.

Actual execution then exposed `CC02.API10`: a 32-bit parameter changed through
`ref` had its stack home updated, but later reads reused its initial register
value. CopperSharp now reloads the existing home for that machine width.
The isolated before/after result is **18 failed -> 18 passed**, across all
three CPUs with peephole on/off; **99 related tests pass**, including those 18.
The complete source, commands, hashes and unqualified narrow/wide cases are in
[compiler-qualification.md](compiler-qualification.md). The generic heap-context
failure policy and separate unexecuted export-A6 audit remain open. Shipping
command entries requiring heap-backed context are not admitted.

The two private numeric projects are in the solution. Their executor calls
the real public production formatter through a test-only control block and
checks actual output, guards, address boundaries and interleaved callers using
one image per CPU. It provides no host gateways or external native targets.

The hardened qualifier now has separate `Foundation` and `EvalNumeric` modes.
It creates failure receipts before bootstrap, snapshots source/settings and
declared managed dependencies, forces fresh builds, pins and records runtime
and build-tool identities, and checks live/copied input bytes and inventories
around tools and final publication. Reachable assembly names must be the exact
required set, without omissions/duplicates, and each hash must match its bound
managed input. Successful mode-specific latest pointers are published only
after all targets reproduce their bytes and all checks pass.

```powershell
pwsh -File tools/Commands/qualify_native.ps1 -Component EvalNumeric
pwsh -File tools/Commands/qualify_native.ps1 -Component Foundation
pwsh -File tests/Commands.NativeExecution/QualificationScriptRegressionTests.ps1
```

The recorded runs passed **540 numeric** and **168 startup/argument** native
invocations, respectively. All **nine HUNKs reproduce byte for byte**. Numeric
run `093da10d472c4390be4c167100b41523` binds 526 source/settings, 20 managed,
41 restore and 196 host/runtime files; all 25 stages/27 input checks pass.
Foundation run `0f4efb1616bb432080849d29d458dd9b` binds 436 source/settings,
20 managed, 35 restore and 196 host/runtime files; all 43 stages/45 checks pass.
The six negative pipeline checks pass, including modified live/snapshot inputs
and bootstrap failure. Exact receipt/artifact hashes and observed stack usage
are in [qualification-report.md](qualification-report.md) and the
[build manifest](build-manifest.json). These runs bind their recorded source
checkpoints; later source changes require new qualification.

`CC09` also executed all **44 original DOS 40.3 parser cases**, retaining a
sanitized original-only receipt. The generated image needs a bank larger than
512 KiB. The expanded-memory prerequisite revealed an emulator defect: private
Wait/Reschedule gateways outside original Exec's negative table masked a free
MemChunk, whose physical size was 216 but whose bus view contained gateway
bytes. The real provider now claims two checked, previously unmapped continuation
slots, preserves synthetic aliases, rejects mapping/gateway collisions and
uses token-owned cleanup. **17 focused cases pass**, including original licensed
Exec allocation/free of 1 MiB with its actual free-list metadata intact. See
[reference-execution-paths.md](reference-execution-paths.md).

A subsequent DOS build succeeded, but its revalidation caught shared managed
inputs rebuilt after capture; that receipt was not relabelled as current.
The provider/test build window is now closed and a serialized owner rebuild
is underway. Generated parser cases remain zero at this checkpoint. Remaining
next slices are the strict native parser comparison, raw I/O/signal support and
the next verified command behavior group. No command, profile, installed path
or P bit is qualified by these component results; the full goal stays active.

## 2026-08-30 - Archive contract and provider owners

`CC01/CC32.Exe2Arc.1` adds a source-backed contract for the verified MorphOS
3.20 member: exact published `FROM/A,TO,TYPE/K`, seven scanner/extractor layouts,
file naming, diagnostics, return/IoErr, resource ownership and pending fixture
groups. Eight unsafe or ambiguous source paths are explicit compatibility
decisions, never permission to reproduce corruption, leaks or unbounded loops.
The release version agrees with source metadata, but the packed binary has not
been executed or proved equivalent. Its Spec/Options rows become partial;
Semantics and all shipping gates remain open.

`CC07.ARCH` maps all 16 CC31/CC32 commands to 13 concrete dependency owners and
nine implementation slices, including real success/failure/teardown fixtures.
The existing SDK's XAD/XPK declarations and host archive helpers are not native
guest providers. Exe2Arc needs DOS/Exec and can proceed independently. Per-file
license evidence distinguishes LGPL XAD sources, XADList's unspecified Freeware
grant and ObjFW's different grant; no vendor code or build script was copied.
All 17 local links and inventory/owner/dependency coverage were checked.

Files: [Exe2Arc contract](contracts/Exe2Arc.md), SHA-256
`0430708bb7e48a2d5629c1a5d2169da10eb3421de8c5a60c0e0dbb0265f67239`;
[archive provider work package](archive-provider-work-package.md), SHA-256
`64d050271e057a6a3b9dba25e76fa6176264cfc5a87ba97edab89a5faeddbb89`.
All archive command implementations and original-runtime comparisons remain
required work; these documents do not close CC07, CC31 or CC32.

## 2026-08-30 - Native discovery repair and first I/O/MakeDir attempts

Fresh DOS provenance initially passed, but native installation failed after
881 DOS instructions. The address-error frame traced to discovery treating
the real `expansion.library` 40.2 at `$00C000C4` as a private DOS extension,
then reading the bogus owner `$FFFF00AF`. No generated parser case ran. The
bounded owner fix checks public library identity/declared sizes before private
fields, admits a whole owner span through genuine Exec PUBLIC-memory bounds,
and checks its seal, identity and backlinks before deep binding. The global
native `IsMapped` policy is unchanged. **49 focused owner/discovery tests pass**
with source and actual executed-DLL binding; receipt SHA-256
`d6f8cd8a809fc884ff68468badace0d0034d7330758af0638e229ab934b2f79b`.
Native installer/idempotence and parser comparison remain pending. See
[reference-execution-paths.md](reference-execution-paths.md).

The new allocation-free I/O API and 32-case-per-CPU fixture preserve raw DOS
results, tagged immediate IoErr capture, borrowed BPTRs and non-consuming
signals. First expanded Foundation run `a583b8feba6142be9d7043866a096358`
retains **168 passing startup/argument invocations**, but all three I/O
compilations fail with `C68K0010` on the readonly value-record constructor.
Overall status is failed, with no successful-pointer update. Receipt SHA-256
`3538a334349a05f6d448fc51973310bb666ed25fcc271ec94975e0c18b3f6f5d`.
The compiler's existing value-type lowering and framework analysis support
this form without a heap; its static validation gate is being corrected at
that owner. No managed heap or source-level ABI workaround was enabled.

The classic MakeDir body now has an independent native/reference executor.
The original 37.2 binary remains private, pinned to 464 bytes and SHA-256
`23911db49742055d8bddcfe5f8de82cbb2232f8a7ef850d51bfd27f9b54c819b`.
Its 38-case matrix passed on each CPU (**114 original invocations**) with
supplied DOS vectors. Deliberately wrong generated images were rejected;
those exploratory receipts remain failed with zero comparisons. Review added
strict instruction-prefetch bounds, output-call IoErr poisoning and safe
failure-receipt handling, including hardlink/junction overwrite rejection.

First generated MakeDir run `802a2d8c647b4acca7c098dd8bd5adb0` compiled all
three HUNKs but was rejected before execution by the generic zero-framework-
metadata guard. Receipt SHA-256:
`44fbf7c1f84af9d1b659e5ea82aa55f5e927374a57f466567fbc3e69c90c566e`.
The only members are SDK `BPTR?` HasValue/Value intrinsics, which emit native
tests/loads. The runner now admits exactly those bindings for MakeDir only,
still rejecting allocations, runtime helpers, external native targets and
fatal-machine-fault sites. No generated MakeDir execution is claimed yet.

All **13 qualification-script rejection tests pass** in
`4d028c295ea3448eb2849a370c070f5c`, SHA-256
`5e4ef722e51135b4e0cebdf0461ca7cd8c671cd3a7c73cd7d6600ce6aa013521`.
They include external reference byte drift without copying and six attempts
to expand the precise intrinsic allowance. The qualifier source at that run
is `80bad34420cd06fae9492797a2881f62b0904f1cb3abb05669e4e518944f9bb7`.
The three modes have separate latest pointers; MakeDir keeps original and
generated counts separate and never snapshots original bytes.

The archive work-package's owner path typo was corrected from `Sdk.Support`
to `Sdk.Amiga.Support`; its new SHA-256 is
`a0ab72a08f15fb4da03f987dbf30ef8033a13e74b4ffb67e65c15bacbd93d7f3`.
This documentation correction changes no archive implementation or license.
The stable goal plan and all shipping/purity counts remain unchanged.

## 2026-08-30 - Value records, imported-call folding and fixture ABI

`CC02.API11` corrects the no-heap validator's rejection of resolved value-type
constructors. Existing native lowering already uses frame storage; only the
static admission check changed, and it still traverses constructor bodies and
rejects real class/array allocations. The focused matrix changed from 14 failures
and two passes to **16 passes**, including twelve actual native executions and
four allocation-rejection controls. The related subset passed **85 tests
inclusive**, without skips. Receipt SHA-256:
`173e70b9da60aa75e26c2924efe2ece028536d6e25e5bdf65c78387d152bc456`.

The next expanded Foundation attempt, `e0dde03caff74ef78496d9f4c9ddd210`,
compiled all three I/O images but each failed its first Write case by calling
Read. The emitted ReadOnce and WriteOnce methods had the same address. The
report remains failed, SHA-256
`f4e85245b5b0a58f21ed509ccdeadc47eac0bda77d884a21014a1275075ee54c`.
`CC02.API12` fixes imported-call identity during body folding: distinct synthetic
imports previously shared an empty metadata identity. The comparison now checks
the caller-module member token; managed and same-target body folding remain.
All **24 native reproductions failed before and pass after**; **134 related
tests pass inclusive**. Receipt SHA-256:
`1a1e21e6af2d324193ce727367c3fea6ec9175efdcdb2e58781fb3a495ec71dd`.
The rebuilt backend is
`2083e5497e70c9c7162beb84b85e56aa19229a3e3306b5b708eecbc505fe434a`.
SDK declarations, command I/O source and its fixture were unchanged by either
correction. [Compiler qualification](compiler-qualification.md) records both
histories, exact commands, source hashes and test/runtime identities.

The next Foundation attempt, `71dfcdbe1f4944eda6f6599546121da2`, retained
168 startup/argument passes but rejected the three compiled I/O closures before
execution. The sole reported helper was
`__c68k_shared_epilogue_97FEB6A1E88C94DE`; all allocation, exception, fatal-site,
framework-feature and external-target counts were zero. The compiler's report
counts every `__c68k_` label, including this internal return sequence. Its ten
bytes `2002508f4cdf4c1c4e75` return D2 through D0, remove eight local bytes,
restore the six saved registers and return. Both genuine ReadOnce/WriteOnce
branches reach it and their prologues match. It contains no runtime service,
allocation, trap, shared storage or library call. The failed receipt remains
failed, SHA-256
`9deabb2188943d34c595e399b4f889ecd12201467602b712416bb9f64fa3ef19`.
An exploratory execution of the unchanged frozen HUNKs and executor passed
**32 I/O cases per CPU, 96 total**, in
`obj/io-shared-tail-review-9ea4b862e956491fa590a30534735a09/`.
Those observations do not retroactively pass the rejected qualification run.
A precise code/branch admission check and fresh full qualification are next.

The frozen MakeDir artifacts also exposed an overconstraint in their host
fixture. Original NDK STARTUP.ASM explicitly allows the complete image entry
to modify all registers except SP. The fixture now requires entry SP restoration
and records other register preservation as an observation; public-vector A6,
argument/clobber, output, guard and resource checks remain mandatory. No command
or compiler bytes changed for that correction. The original 38 and generated
39 cases passed on each CPU: **114 original, 117 generated and 114 supplied-
vector comparisons**. These are exploratory frozen-artifact results, not a
fresh source-bound qualification or actual DOS/filesystem execution. The review
receipt is private at
`D:/TestData/CopperOSCommands/Workbench31/makedir-frozen-artifacts-entry-abi-review-802a2d8c/fixture-abi-review-summary.json`;
it explicitly retains the old failed envelope and sets source-bound
qualification false. Exported/library callee-save obligations are unchanged.

## 2026-08-30 - Classic Rename contract

`CC01/CC12.Rename.1` adds the [Rename contract](contracts/Rename.md), SHA-256
`921b823d0095e9ac0a888188fc93db9341c108fa45dbd68b76891d9722c6aa78`.
The pinned classic 37.2 member has exact
`FROM/A/M,TO=AS/A,QUIET/S`, directory/direct dispatch, match ordering, three
format strings and result/error/cleanup behavior verified statically against
its code and original NDK. Thirty-six finite fixture groups separate public
API expectations, code-derived behavior and future native observations.
Unchecked path capacity, omitted/repeated MatchEnd and final-IoErr behavior
remain explicit safety/compatibility questions. No original Rename execution
or replacement is claimed; the next slice is an isolated original-only fixture.

MorphOS 50.8 has media identity and required P evidence only; its grammar and
behavior remain open. The ledger now has eight contract documents for thirteen
profiles, eleven partial Spec/Options rows and four partial Semantics rows.
No full gate, shipping profile, installed path or replacement purity is passed.

## 2026-08-30 - ReadArgs fixture runtime refresh

The MedPlayer fixture now invokes the native DOS installer twice, requiring the
same library result without a second owned initialization. Both invocations keep
the existing one-million-instruction bound. A single test-project-only build
with `BuildProjectReferences=false` passed without warnings or errors. The
729 captured source/shared files and 27 canonical runtime DLLs retained their
hashes and timestamps; no shared dependency was rebuilt by that operation.
The build receipt SHA-256 is
`b400cdc277fe13a129ebc86e7deb97b2f237ff693842673b806ada9179af8fb6`.

The canonical emulator had already changed since the historical gateway fix
receipt. Its actual current copy is
`453de5eaac3fc284ce954b1b10e0d08b619c7d9100a06b583c806a412be0edbd`;
the rebuilt test DLL is
`bf30e5ca9f750bf8e778105ac33405cef3f5afc5c61f32c7e41b7dcc5103ca7a`.
The same focused gateway matrix passed **17 tests, zero failures or skips**
on those exact copies without rebuilding. This includes licensed original
Exec allocation/free in real 2 MiB fast RAM. Canonical and executed DLL/source
identities stayed stable. Current TRX SHA-256:
`fdcd260d82be1cea18eea1d65eef0c64621b746e55b86ce46d870c28d0252b00`;
current runtime receipt SHA-256:
`d1de3d77912235987b871ada5a635f963f207b4c96dfbe013de476cff6d33439`.
Exactly one strict ReadArgs differential test was discovered, but it has not
yet run against the refreshed DOS owner. These checks do not add generated
parser cases or a passing comparison. Private receipt paths and the preserved
historical runtime distinction are in
[reference-execution-paths.md](reference-execution-paths.md).

## 2026-08-30 - Expanded Foundation and source-bound MakeDir pass

The exact shared-return-tail admission is implemented without weakening the
runtime or fatal-site policy. It parses actual HUNK code/symbols and checks
the one named ten-byte tail, both caller prologues and terminal branches,
the code/data boundary and relocation exclusions. The compiler's reported
helper count remains one in the I/O artifacts with an attached proof; no
external runtime dependency is approved. All **37 script regressions pass**
(two positive controls, 35 rejection cases). Receipt
`tests/Commands.NativeExecution/obj/qualification-script-regressions/0972bb681ad74ad28a16bcdc987df5d8/script-regressions.json`
has SHA-256
`2a2aa4ec10f1a1b569eca39e840dcdb3bb0e0c66dd0ecd7957e14ad3401ee5e0`.
The qualifier source SHA-256 is
`371695e0a6a2a9533b001e07a772e85dfec56aa95872012166e6de1e47c10399`.

Fresh Foundation run `5b2854ee3ef04f71ad6faab7d95add17` passes **264 actual
native invocations**: 31 startup, 25 argument and 32 I/O cases on each CPU.
All nine HUNKs reproduce identical bytes; the 61 stages and 63 input checks
pass. Receipt SHA-256:
`ffc05feaa384f4a27bbc80349b7d1a6a77c873856c23cff1e0c66ec55b002ff3`;
input manifest:
`6c587c66c8bd08153f82f56db50eff0acd94fdc1b673647a5158cda802aeabaf`.
The [I/O contract](native-io-contract.md) records exact artifacts, raw I/O/error
and signal semantics, repeated/interleaved execution and observed 84-byte stack
extent. Prior failed attempts and exploratory frozen-artifact runs remain
unchanged and are not relabelled as accepted qualification.

Fresh MakeDir run `012c05cf6707457a9f288e8b862dbcf4` passes **114 original
and 117 generated invocations, with 114 comparisons**. All three generated
HUNKs reproduce identical bytes; 25 stages and 27 input checks pass. Receipt
SHA-256:
`e59c8ed1d4f9e2f7a1e7186f301a3feb3552e503e66ef8a1c6152d8d66cbc266`;
input manifest:
`65892f5a53c626207c71934e882cf46453ca5340642e90f2314732c70689c559`.
The external original is hashed and rechecked, never copied. The
[MakeDir contract](contracts/MakeDir.md) records all three images, the extra
generated-only allocation-failure case, exact actual executor/SDK identities
and separate original/generated stack observations. This is a supplied-vector
comparison, not actual DOS parsing, filesystem operation or boot. Classic
MakeDir now has partial native and differential ledger evidence; all shipping,
full-command and purity gates remain open.

## 2026-08-30 - Numeric fatal-site audit and fixed-base arithmetic

Fresh attempt `b02c8f99d14a406f875908c29f2cc3d5` was rejected by the stricter
zero-fatal-site gate before numeric execution. Its compiled HUNK bytes match
historical passing run `093da10d472c4390be4c167100b41523`. Both runs' compiler
reports contain six ILLEGAL sites; the older qualifier did not enforce this
field. The new failure is retained, SHA-256
`d6b6e660ff921408c4e3af69c29aa68198232838eb1edde12d2e3d751d03a7ea`.

Native disassembly traces all six sites to variable-divisor checks in the
private digit routine. Public callers supply only 8, 10 or 16, and that radix
survives through each call, so valid public inputs cannot reach those zero-
divisor guards. The independent formatter now directly extracts hexadecimal
and octal digits with two-word shifts; decimal retains its bounded 16-bit-limb
algorithm with literal divisor 10. This is fixed-base arithmetic, with no
compiler, SDK, public API, buffer-policy or qualifier change.

Existing focused tests pass **61 before and 61 after**, including a separate
61-case run after a checked-arithmetic rebuild. The normal full command/Shell
suite passes **293**, with zero skips; the independent 3 x 256-value oracles
and no-allocation check remain unchanged. Formatter SHA-256:
`9167c6d699a065dab0ac143fd325ea999a27b60897740026d916e9b690e86233`.
Receipt
`tests/Commands/obj/eval-fixed-radix/adff44b3f9724f2fbbd9b68453151202/receipt.json`
has SHA-256
`417ba66a0f987ab0c404908b19ac9b8afcf09ecd797aad246fae84591a4ec154`;
the adjacent fault-site audit has SHA-256
`356bd5d08a66c49b885edd90d3f8cb8dcdb5003787708b3fd8dddc1c37824040`.
Fresh native requalification is required; the gate remains exactly zero fatal
sites. Historical 540-case observations are not a pass of that newer gate.

## 2026-08-30 - Public command-launch contract

The new [native command launch contract](native-command-launch-contract.md)
reserves `CC02.API14` and binds seven original NDK member hashes plus sixteen
local owner-source identities. Its eight readiness/observer cases establish
public CLI/Input/Output, RunCommand argument length/newline/NUL handling,
GetArgStr and input-buffer restoration before a normal parser call is claimed.
Four first MakeDir launches (two original, two generated) remain pending.
No fabricated Process/FileHandle, parser gateway or explicit RDA_Source is
accepted as evidence for the normal NULL-D3 command path. Actual handler and
stdout capture require later separate cases. The stable goal plan is unchanged.

## 2026-08-30 - Fixed-base numeric native qualification

Fresh EvalNumeric run `e767705fdcaf42fb864c4c68c8e3d9a3` passes all **540
native invocations** with **zero fatal-machine-fault sites**, runtime helpers,
external native targets or managed allocations. All three HUNKs reproduce
identical bytes. Its 25 stages and 27 input checks pass, binding 526 source/
settings files, 20 managed files, 41 restore files and 196 host files. Receipt
SHA-256:
`e1e1dcf4c5ccaeee7d754bcac0a899f29b56d72c47bc4174c89e6a1234cf8e08`;
input manifest:
`0e96105f5c014a0e6db8e951caf40a3f73edd8c6f9217e9d0737c39e88b455bb`.
The 68000/020/040 artifacts are 2184/2076/2064 bytes, with observed stack
writes of 192/180/180 bytes on configured 4K/16K stacks. Independent integer
oracles, repeated/interleaved callers and protected image checks pass; no
original Eval command or full command grammar/format/TO behavior is implied.

The [build manifest](build-manifest.json) now records fifteen accepted private
HUNKs: nine Foundation, three EvalNumeric and three classic MakeDir. This is
**921 generated native invocations**, plus **114 original MakeDir invocations
and 114 supplied-vector comparisons**. Every artifact/evidence file hash and
input-manifest hash was rechecked before recording the manifest. Shipping
and staged arrays remain empty; all 246 purity/package gates remain open.
The [qualification report](qualification-report.md) preserves the historical
numeric six-site reports and failed stricter attempt separately from this pass.

## 2026-08-30 - Isolated DOS build and host-test drift

The live DOS owner build produced all three outputs, then rejected its receipt
because shared managed inputs changed during compilation. CodeView/PDB paths
identify a build through `C:/D-drive/` rewriting the same physical dependency
outputs as the `D:/` workspace. The guard was not relaxed and no other task
was stopped. All failed/stale owner receipts remain unaccepted.

A private two-sibling source snapshot under
`D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-20260830T125923Z-be3b3c30`
contains 761 source/settings files, no copied original commands/ROM and no
prebuilt owner binaries. Its nine-project restore graph closes inside that
snapshot. The unchanged private owner build and immediate unchanged
`-SkipBuild -ManifestOnly` both pass. Copied and corresponding live sources
matched at final capture verification. The 871,040-byte 68000 HUNK has SHA-256
`9a9a69046c0595e59e90f087a2f8be948f4ee4a49d8a1b84555a3c7bde41b403`;
full isolated-build receipt SHA-256:
`10a588732c6473ce3054e304f5dd9170cb39ecc374e338b77d3e56d0b989bda6`.
The existing source/compiler-root environment overrides allow the strict
fixture to validate that private provenance without modifying its checks.

The next strict invocation stopped at host preflight: the previously bound
`bf30e5ca...` test DLL had been replaced by `59b833f9...` through the same
alias; five executed DLL hashes changed while the emulator remained `453de5ea...`.
The command-owned ReadArgs/gateway fixture sources were unchanged, but unrelated
test sources also changed. No parser case ran and no older receipt was reused.
A private host-test source/runtime closure is being built to eliminate those
shared-output races, retaining the one-million-instruction bounds, two native
installer calls and all 44 exact original/generated cases. Provider build
provenance alone does not pass installation or parser semantics.

## 2026-08-30 - Private native parser and launch-readiness observations

The frozen private host now builds with exact dependency bindings and passes
all 17 gateway cases. Its DLL SHA256 is
`7899455419ad8c5edc1025c5632061befd8526b6712db6d3c54c5c4726f01871`;
the unchanged emulator is
`453de5eaac3fc284ce954b1b10e0d08b619c7d9100a06b583c806a412be0edbd`.
No failed live-output receipt was relabelled. The strict parser run reaches
44 original and 25 complete generated observations, then fails while copying
the 26th generated `/M` result. All 25 preceding pairs also differ. Its failed
receipt is `18c39884481edffd7634e97deff70ca934ed234809547eaa6f8e41efa31d0959`.
Generated installation returns after 169,739 instructions, and repeated discovery
returns the same base `$002A3EE8` after 6,620 without another initialization.
Those two native observations supplement the 49 managed discovery tests;
they do not establish parser compatibility.

Inspection of the exact generated HUNK identifies CC02.API15: byte option
flags written through a field address are read with MOVE.L by value. The
big-endian load loses their modifier bits. A compiler-owner correction now
preserves field offsets/aggregate layout while using typed byte/word access
and signed/unsigned promotion. The first 36 native regressions fail before and
pass after the candidate on all three CPUs, both peephole modes and both
freestanding/resident profiles. Related regression/owner rebuild gates still
apply; no DOS enum widening or parser mismatch normalization was used.

A separate private original-DOS readiness variant passes R01 and fails R02
on Input=Output=0. The real Process and CLI are present and were observed
through original public queries without structure writes. Receipt
`e0c0790fb501fb76b00ef2b815af814fa1485464c08eec55784b7655723264e4`
has zero command, ReadArgs and RunCommand calls. Public NIL: acquisition,
selection, restoration and closing are the next bounded prerequisite.

## 2026-08-30 - Classic Rename original vector subset

The isolated RN1-WB executor completes 40 returned observations plus eight
separate hazard guard stops on each of 68000 and 68040. The 68020 run stays
failed after 16 returns because Copper68k 1.4.0 lacks exact timing for original
opcode `$12FC` at the next case. It uses no fallback or modified original.
Twelve failure-receipt/input-safety checks pass, including physical aliases
and hardlinks. The package-only build has zero warnings/errors and its 11
recorded inputs remain unchanged, but reproducible build binding is not
claimed. Audit SHA256:
`4a54b9dff3a28c8ef5d2f27ad395f571438c98178ab7caddd625a059ea5ff226`.

The [Rename contract](contracts/Rename.md) records exact branch/output/error
and ownership observations, including unstarted/repeated MatchEnd, omitted
search cleanup and unsafe path accesses. Guard stops are not successful
commands or evidence that original DOS safely supports those paths. No
generated body, original DOS matcher/handler, installed artifact or purity
approval is claimed. All original inputs remain private and unchanged.

## 2026-08-30 - Narrow-field repair accepted for private DOS rebuild

CC02.API15 now passes all 389 selected related tests, including the same 36
native cases that failed before the repair and 12 additional native boundary
cases. The latter verify canonical bool, high-bit char/enums, zero reassignment
and ten nonzero padding bytes inside an explicit 24-byte structure. All 48
new native cases cover 68000/020/040, both peephole modes and freestanding/
resident profiles. No field offset, aggregate layout, parser enum or metadata
change was needed. The compiler CLI builds with zero warnings/errors.

Repair receipt SHA256:
`4e8e7749cc1b86ca28dc10eef4b6105b2284b14dca06e77642c2e1f3253b2658`.
The new backend DLL is
`142298b971344f9e4516f31a484d25381774c1dc76bdf822234b83520f89c725`;
the two tested emitter sources and before/after binaries are frozen beside
the receipt. An unrelated test-export setup failure remains recorded; only
that new bridge fixture's global export was removed before the same related
filter passed. No existing test or compiler option was weakened.

The next private DOS snapshot replaces exactly those two emitter files in
the earlier 761-file source base, preserving the other 759 files. This allows
the separate RunCommand bridge work to continue in the live compiler without
contaminating the parser comparison. Actual parser semantics and shipping/
pure qualification remain separate gates.

## 2026-08-30 - Original DOS public NIL: stream ownership

The separate R04 private host variant passes public stream acquisition,
selection, restoration and cleanup. Original DOS Open(NIL:,1005/1006) returns
real input/output handles; both borrowed zero selections are restored before
the two acquired handles are closed once. All 19 recorded DOS calls preserve
the real task/SP and return immediate IoErr zero. The full 154-slot DOS vector
table remains unchanged, with no DOS gateway or fabricated OS structure.
The initial R02 missing-stream receipt stays failed; it is not relabelled.

R04 receipt SHA256:
`e3d434be39f8687977d585f61faa14f3f8b42047c0fed200c135223ae6a76940`.
There are zero command, ReadArgs or RunCommand invocations and no generated
DOS execution in this run. It proves this component's public stream ownership,
not nonempty input restoration, readable stdout, child scheduling or full boot.
The next native observer/command launch still needs its own public-boundary
execution and provenance checks.

## 2026-08-30 - Complete native parser observations after the field repair

The private two-file compiler derivative passes the unchanged DOS owner build
and manifest-only validation. Its 871,960-byte HUNK has SHA256
`e40796f59eaf2a050c873a70d3feb1a77e7dba9cb2b3add23b66cfb1ad7e868f`.
The remaining 759 captured sources and the frozen host/runtime are unchanged;
the subsequent RunCommand bridge changes are not part of this candidate.

The strict test now completes 44 original and 44 generated observations. All
original results match the prior capture. Native installation, repeated
discovery, code/vector immutability, expunge and loader cleanup pass. The
comparison still fails in 43 pairs, with only required-zero-source matching
every field. Mismatch counts overlap: return 5, IoErr 35, cursor 41, values 8,
FreeArgs-called 5, cleanup IoErr 35. The former /M result-copy failure is gone.
Strict receipt SHA256:
`040acbb21326a9c659316827f1cd96699c708a5218b6dd2678e0c5c8cd53ca74`.
The [reference record](reference-execution-paths.md) retains all preceding
failures and exact paired values. No command or RunCommand has run in this
explicit-source test.

Before any parser production edit, the new 44-row original-derived host suite
also fails 43 rows and passes one. Its strict return/error/cursor differences
match the native observations; it deliberately avoids dereferencing failed
result slots. Memory guards and allocation cleanup pass. This red baseline
is retained at
[baseline-summary.json](D:/TestData/CopperOSCommands/ReadArgsHostRegression/20260830T143234Z-888d80ce/baseline-summary.json),
SHA256 `f10df51baa8ef065c356cb16a0b9684fcaf3588fe1c5089097b4a5f6afb183d8`.
The failing replay row stops at its first priming case: this baseline makes
44 calls and reaches 43 distinct cases; a fully passing suite would make
87 calls, including the ordered 44-case replay. Bounded parser repairs and a
fresh native differential are still required.

## 2026-08-30 - Native normal-return stack bridge

The new ExecuteOnStack bridge retains the old Execute API and uses public
Exec StackSwap twice without reading an old C# frame while the new stack is
active. It places its recovery descriptor on the invocation's stack, so a
command can overwrite every register except SP. The result survives the
second swap; the caller's stack/registers and descriptor are restored.

Twelve unchanged ordinary-call native baselines fail before command entry.
The new bridge then passes twelve CPU/peephole/runtime configurations, each
with two interleaved callers and a repeated first caller: 36 successful native
invocations. Twelve retained failure controls and five signature guards make
29 focused checks; all 418 inclusive related checks pass. The CLI rebuild
has zero warnings/errors. Receipt SHA256:
`dc51a073634ab8b391c7eed3b0c369f30807be62e65e9ec3407a084dac241b94`.

These are compiler/component results with a supplied Exec vector. CopperStart
integration, original Exec execution, normal input/GetArgStr restoration,
Exit/nonlocal unwind and command purity remain open. The fifteen earlier
qualified private artifacts retain their older compiler binding and require
fresh qualification when rebuilt.

## 2026-08-30 - Exe2Arc bounded RAR4/CAB header implementation

CC32.Exe2Arc.2 now has independently written, allocation-free guest-memory
predicates for one RAR4 or CAB candidate. They validate bounded header spans,
use byte reads on unaligned memory, apply the source-observed strict file-size
and LE32 length/table predicates, and return the selected payload length
without reading or writing the payload. Offset zero is rejected without
inventing the surrounding scanner's continuation policy. CAB matching remains
a limited source predicate, not an archive-integrity check.

All 54 new host cases and 347 inclusive Shell/command cases pass, zero skipped.
Builds use pinned packages and BuildProjectReferences=false; compiler/SDK and
CopperStart outputs were not built. The retained initial setup failure came
from an old local-SDK restore graph, corrected by a CopperOS-only package
restore without changing production or test source. Receipt SHA256:
`9405e0dd6c963ecff9ac72b299445577f28576fbee2ea167fbe6089a1a50edd1`.

The [Exe2Arc contract](contracts/Exe2Arc.md) now links exact sources/tests and
next slice XA2. These inert fixtures execute no wrapper and perform no file
I/O. Native qualification, scanner/copy ownership, parser/naming/output,
remaining formats and original packed-command correspondence stay open.

## 2026-08-30 - Exact native ReadArgs corpus passes

The bounded DosCore correction changes the unchanged original-derived host
suite from 43 failed/1 passed to 44 passed, including its full ordered replay:
87 calls. All 107 focused host rows and two separate exploratory installed
native-family tests pass. Public ReadItem/StrToLong and DosCliCore are unchanged.
The host repair receipt is
`4d172e34d185ca850823054ab12d327a851b0bab55a5ca1f0162a4eb4abdf9bd`.

A new private native candidate replaces exactly that one source in the E407
761-file snapshot; 760 files remain byte-identical. The unchanged owner build
and manifest validation pass. Its 873,920-byte HUNK is
`5014fce66228ab170f498474248b387c5aea40f80e39c0404c8cf1cb3280004f`.
The unchanged strict host/corpus executes 44 original and 44 generated cases;
all 44 pairs match return, IoErr, cursor, typed values and cleanup exactly.
Native installation, repeated discovery and code/vector/loader cleanup pass.
Strict receipt `8647612781105ce75818bbfe060e76f38ff28375e667a8b550d37d5e6f191a49`
is separate from both retained failures. The
[parser qualification](readargs-parser-qualification.md) records provenance and
remaining behavior. This explicit-source test contains no command or RunCommand.

## 2026-08-30 - Production native callback integration passes

Only the RunCommand branch of the actual CopperStart callback now uses
ExecuteOnStack. The same final native fixture first fails six normal-return
configurations and passes six non-start controls; afterwards all twelve pass.
Six CPU/peephole configurations execute 18 native commands, with twelve further
callbacks that correctly do not start. The actual callback ABI, result,
descriptor/task bounds, guarded 4K/16K stacks and shared-image integrity pass.
All 39 related tests pass from the normal CopperStart output path.

Receipt `1cc7928b905d5f6228b24abfd97bee43cef93fc163c59f60273fd32a06b81ead`
also retains harness setup failures and four source-root lookup failures from
the receipt-folder run. These are not command semantic failures or successful
command invocations. Exec remains a vector fixture. Input ownership, callback
rollback, nested dispatch and reset/retirement/Exit remain next owner work.

## 2026-08-30 - Original command input and exact observer captures

One additional 25-byte quoted/escaped authored observer passes through original
RunCommand and NULL-D3 ReadArgs, returning `alpha`, `two words`, `a*b`. A later
separate run passes both exact R05/R06 observer cases: LF-only (one byte) and
`alpha beta` plus LF (eleven bytes). The latter receipt is
`9ffd6e9d09f9e6b724f9bbc769ba5927907dbd5acdac9deb622572f19457ca2a`;
the earlier variant remains unchanged. Together these are three authored
observer calls, not distribution/generated command counts. Each observes
GetArgStr equal to entry A0, native ReadArgs/FreeArgs, 42/IoErr 0 and restored
observed state. All have the inherited supervisor SR `$2000` component limit.

The first MakeDir run through original DOS then passes two input comparisons:
two original 37.2 and two unchanged generated command invocations. LF-only
returns 20/0; the seven-byte unterminated quote variant returns 20/120. Native
ReadArgs, command-owned cleanup, argument/code/vector integrity and observed
input/process restoration pass. Receipt
`ae22f088057f5d44b9b167192f08060bb680a334b6a99316e3b2fe7f79dd3394`
does not qualify delivered diagnostic bytes, real filesystem effects, full
boot or the current compiler. The exact fourteen-byte A03 contract input
remains pending in a new capture; the seven-byte variant is not relabelled.

## 2026-08-30 - Exe2Arc last-byte correction and ledger audit

Peer review found a valid header ending at the last uint byte was rejected.
The span check now accounts for the inclusive final byte. Four unchanged new
cases move from two failed/two passed to four passed; the focused/inclusive
host totals are 58/351, zero skipped. Source is
`dd79842d71e0802509d00daaaadbb3ac47a332a15381cb2c4d12dd8a84aac646`;
receipt `02090c22712afe8247826b7129dd48fb7dbe10cbe71feb8f750478b5943523aa`
retains the earlier 54/347 checkpoint. Native physical-address qualification
remains separate from these sparse host uint addresses.

The ledger's classic Rename Semantics cell was corrected from partial to
open: RN1 observes the original only, with no replacement body. Its original
evidence and partial Spec/Options remain. With Exe2Arc's implemented header
primitive, there are five partial Semantics rows and 241 open rows. Inventory,
pure requirements, shipping totals and stable goal bytes are unchanged.

## 2026-08-30 - Exact A03 original-DOS comparison completes

A new capture uses the unchanged fourteen-byte A03 input, `"unterminated`
plus LF. One original and one prior generated MakeDir invocation both return
20/IoErr120 through original RunCommand and NULL-D3 ReadArgs. No FreeArgs is
called after parser failure; both request PrintFault120. The receipt is
`42383f47cf3ef2a1f1c2738592245464ef47cea4f738da46bb796dfad573f591`.
Together with the preceding run, there are three original/three generated
invocations and three comparisons: exact A01/A03 and the seven-byte quote
variant. The latter is still a separate case. Delivered output bytes,
filesystem effects, full boot and current-compiler launch remain unqualified.

## 2026-08-30 - Exe2Arc native header and window correction

The frozen dd79842d header helper passes 228 actual native calls, three
byte-identical rebuilds and ten separate negative controls. Receipt
`8a23cfefb17352f25e33f465352d2e983d755bc87f8a2b0c4cbd4e5c19db0621`
binds that predicate; it contains no scanner or command frontend.
A later source audit finds the outer-window strict threshold differs from
candidate-byte availability. Six new EOF cases fail before correction and
pass afterwards; the host totals become 64 focused/357 inclusive. H2 receipt
`e9909a365d105926106ebb2e6eeb25946a47fec593aacb1f4ce82d9f0e508b16`
does not extend the preceding native evidence to changed code.

The subsequent bounded scanner/copy implementation passes 136 Exe2Arc tests
within 429 inclusive tests, zero skips: 64 headers, 48 scanner and 24 copy.
Receipt `fcffaf7d3984b43f9d6fd706deec42e98af00100898a26a99aede2f2fa985577`
retains exact EOF-window triplets, offset-zero stop, seek traces, short/error
transfers and borrowed-handle/scratch ownership. Native DOS adapter, other
layouts, command cleanup and original-binary correspondence remain open.

## 2026-08-30 - Compiler-repair fixture refresh passes

The unchanged qualifier passes Foundation264, Eval540 and generatedMakeDir117
native calls, plus 114 original MakeDir calls/comparisons. All fifteen HUNKs
reproduce exactly. The [refresh record](command-fixture-requalification.md)
binds the three reports, two identical 570-source captures and final audit
`69433b37337cabdfd2babac4d0af3a8849dc52db6fc7697e45123d2bd334a13a`.
The first Eval run failed strict Windows path identity despite native success;
its receipt remains failed. The retry changes only the private workspace path.

The build manifest now points to the new artifacts and preserves its prior
bytes under `previous-build-manifest.json`. Original-DOS A01/A03 observations
still bind the older MakeDir image. No shipping/P/boot claim, inventory total,
Shell command membership or stable goal byte changes.

## 2026-08-30 - Portable input ownership and native rejection rollback

The [RunCommand input lease](runcommand-input-context-implementation-notes.md)
now owns exact argument bytes, normal buffered-input consumption and caller
lookahead restoration without advancing the underlying stream. Per-task LIFO
contexts, opaque non-reused tokens, stale/foreign/corrupt rejection and deferred
reset/retirement pass 87 focused and 271 inclusive tests, zero skips. Receipt
`95c5d13a8d2d09c28b6bde92259b5dc692edcc6fd13a11ac109f62bf2a295f3b`
retains the failed baselines and the explicit missing first-34 test-DLL
snapshot limitation. The original 44 parser cases remain unchanged.

Both native dispatch loops now forward a proven never-started callback to
Resume(false). The actual direct boundary moves from 24 native failures to
the same 24 passes within 295 inclusive tests. Eighteen child leases are
restored/freed and six stale tokens leave guest state unchanged, across all
three CPUs. Receipt
`b3d3be62c1e2ce6b1e5881a26ea9a8c92c7c5c5a2641ac9ef89af31747351084`
binds the unchanged test corpus and 251 source/141 binary inputs. The legacy
loop rejection change is source-reviewed only. Native normal-input composition,
host callback lifecycle and nonlocal Exit remain separate gates.

## 2026-08-30 - Exe2Arc bounded scanning and copy execute natively

Four additional invalid-provider-count controls raise the host checkpoint to
140 Exe2Arc cases within 433 inclusive tests, with no production change.
Receipt `a6d2a707555eba0b4a3b8250d3ffd932eaba56ddb1befb3f4b1b94d5f91af411`
supersedes the preceding 136/429 host count without changing its history.
The complete bounded scanner/copy closure then passes 231 native component
entries, 77 per CPU, using supplied public DOS vectors. Twenty-four callers
are interleaved on their shared images; all three HUNKs reproduce exactly,
and thirteen separate negative controls pass. Summary
`30022b75d868ae08443b2a52acd32c0064b72e5d2a76edc761c8f3ca65967b00`
binds 601 inputs, 196 host inputs and eighteen phase checks.

The [Exe2Arc contract](contracts/Exe2Arc.md) records 159 scanner and 84 copy
method calls within those entries, 729 supplied DOS-vector calls and the
retained setup/negative-control failures. A constructed-APTR setter compiler
case remained unrepaired at that checkpoint; using the established factory
was not its repair. The later [compiler correction](constructed-scalar-qualification.md)
has a separate source-matched regression and retained-probe rerun. No original,
real-handler, full-command or shipping/P result follows from either checkpoint.

## 2026-08-30 - Original DOS initialization prerequisite separated

Read-only inspection confirms the direct InitResident attempt never returned
and left DOS RootNode.rn_Info null. Its LockDosList stall is retained as a
failed prerequisite, not command behavior. A separate ordinary passive boot
of the licensed Workbench disk reaches a non-null DOS list and valid list
semaphores at chunk nine, without manual InitResident, STOP clearing or private
DOS writes. Root-readiness receipt
`a8d6d96b93a04ed9d46d7c7bbc74d8089818140945a67d6989753a0950cff58a`
passes one test; the exact existing Exec/device boot providers remain declared.

A separate wait for CLI/Input/Output fails at the unchanged 32-chunk limit;
all three public fields remain null on the sampled Process. Receipt
`d5668fa2f8e39990c3bf22e5e4556ebd0f1ec64a02db9735e9a4cc5d7228b97a`
is one failed test, zero skips. Neither probe invokes a command or qualifies
handler I/O, unread-stream restoration or a completed Workbench boot. Details
remain in [reference execution](reference-execution-paths.md).

## 2026-08-30 - Host callback lifecycle and successor return fix

The [host callback owner](dos-host-callback-qualification.md) moves the same
14 regression rows from ten failures/four controls to fourteen passes. Per-task
LIFO records preserve nested commands; Exec StackSwap exchanges public bounds
and descriptors; caller-stack restoration precedes command allocation release.
Missing or invalid starts roll back through the portable owner. Wrong returns
and declined tokens preserve pending ownership. Reset and direct ReleaseTask
defer while an ordinary callback can still return.

Review then exposes a generic successor-callback return-PC bug. One added
InternalLoadSeg regression fails at `$6006` versus expected `$F08C2E`, then passes
unchanged after CompleteDispatch returns its final disposition to the caller.
The final suite passes fifteen focused/thirty-two inclusive tests, zero skipped,
including the existing DOS class with two invalid fixture contexts corrected.
Receipt `842729030ce382cb4c27b7b3dc8be1be7bee6c5e942a75bf003bceebb3457d76`
binds 1,118 stable files around a repeat run. PDB audit
`d5466f282266f8d5e803f0a8af0b1e390f858bb2dd94700ea14ac62ef62009f6`
matches ten retained DLL/PDB pairs and 665 source document checksums.

All failed baselines, intermediate DLLs and setup failures remain. The bounded
private build excludes four newer Intuition files and includes two PDB-matched
Icon sources; it is not whole-tree qualification. No guest instruction or
original command runs in these host tests. Review also identifies the actual
RemTask/reaper integration gap; the new
[retirement owner plan](runcommand-retirement-owner-plan.md) assigns six pending
steps. A direct ReleaseTask deferral followed by a supplied return does not
qualify cancellation of an already removed task.

## 2026-08-30 - Original public list query and scheduler prerequisite

From the valid normal-boot root checkpoint, 32 actual public original DOS calls
complete, including LockDosList, nine NextDosEntry calls returning eight entries,
and UnLockDosList. Every call returns to the same Process/PC/SP with borrowed
state, media and vectors preserved. Receipt
`9afd3d02a91b593d6a4005bedb378bc28977872f4e9bc989904b52cf26270574`
passes one strict test, zero skipped. RAM registration is observed; RAM I/O,
RunCommand, ReadArgs and command execution counts are all zero.

A separate read-only topology observer identifies the sampled Process as DF0.
Initial CLI exists on the ready list at chunk nine but has no selected streams.
All later checkpoints show inconsistent ready/wait topology, including a walk
into the other list's sentinel. Receipt
`47c2c31ade2b5ae2e549906239a63f994318fcabea29a00276e49d08b48b4880`
passes the observation test only; list consistency and application readiness
fail. Attribution to a particular owner is not established by the topology
alone. The [reference record](reference-execution-paths.md) retains exact
fields and the public CreateNewProc prerequisites. No task is repurposed as a
CLI, and no private list or ThisTask field is patched to continue command tests.

## 2026-08-30 - Actual task removal baseline

RET01 calls the real host RemTask/reaper owners with an allocation ledger.
Four safety cases fail: accepting/rejecting a Switch request, or changing
ThisTask to a peer/zero, still permits pool cleanup and two frees while the
actual A7 remains inside the removed task's stack. Two supplied cleanup
controls pass, as do all 32 unchanged DOS tests. The source and before receipt
are frozen at `Q/ret0132a7/receipt.json`, SHA256
`8603b63bed9ec41b896e63c736f73aed713e1160261eb857eedfeef5b420bfb2`.

This is a reproduced host-owner defect, not execution of native Switch or a
command. The [retirement plan](runcommand-retirement-owner-plan.md) keeps
actual scheduler proof, removed-task DOS cleanup and outstanding external
pointer ownership separate. A supplied CPU field copy is not a native or
synthetic scheduler commit.

## 2026-08-30 - Native command input composition, incomplete CPU matrix

The frozen installed native DOS owner now executes six authored normal-input
cases on each of 68000/040: ReadArgs, FGets and FGetC with exact R05/R06 inputs.
The 12 accepted invocations include four NULL-D3 ReadArgs/FreeArgs pairs and
24 StackSwaps. Public/private input state, caller lookahead, provider position,
arguments, stack bounds, outer ABI and owned allocation counts are restored.
These are authored observers using supplied Exec/handler boundaries, not
distribution commands or new original comparisons.

The full matrix is **failed**, two passed CPU rows and one failed, zero skips.
68020 stops during native MakeDosEntry setup on opcode 48F9 at `$00601148`,
with zero provider packets or command entries. Its six planned invocations
remain unqualified; no timing fallback or changed runtime is substituted.
Receipt `f7cd88e94ffbea1b6fa9210d7e10b4afe1ca1a9377fb25d4f23935e8fcd607a7`
binds 253 source/project inputs and 141 binaries. Details and a separately
discovered public Close return-value gap are in the
[input owner notes](runcommand-input-context-implementation-notes.md).

## 2026-08-30 - Constructed scalar payload correction

The compiler now executes a scalar value constructor and passes its completed
payload, rather than the temporary's address. The new tests cover direct
APTR setters, conversions, reads/returns, nonidentity and multiple-argument
constructors across three CPUs, two optimizer modes and Resident/Freestanding.
A source-matched before/after comparison moves 84 failures/205 controls to
289 passes, with zero skips. Five PE/PDB pairs and 192 source-document hashes
match. Receipt
`38ffdb15a602b2315e2581158fd28905e48ca9ee5ad22cba0045e24b2c2b3605`
retains the exact one-file source delta, source/reference bindings and repeats.

The earlier built DLL's eight source mismatches and its extra eleven failures
remain separate; they are not credited to this patch. The original failing
Exe2Arc managed probe is reused without rebuilding or rewriting its setter.
It repeats the old `$0004FF9E` failure before the correction, then passes all
77 bounded 68000 component entries afterward. Reproduction receipt
`91a8779b538b17099533c770c915986d9e55f92e2addf4508c823c28cb2fb74e`
records 243 supplied DOS-vector calls and eight interleaved callers. See the
[full compiler record](constructed-scalar-qualification.md). Shared compiler
outputs, the command build manifest, shipping totals and purity gates are
unchanged.

## 2026-08-30 - 68020 MOVEM prerequisite and retained input replay

The CPU owner now handles exact MC68020 opcode48F9, MOVEM.L register list to
absolute-long memory. An unchanged 18-case selection moves from three failures
and 15 controls to 18 passes; the inclusive 633-case selection moves from three
failures/630 passes to 633 passes. The source-matched
[CPU receipt](D:/Koodit/GIT/CopperOS/obj/cpu-movem-absolute/f49db97675f247e6a81cfc4dbc0ba2d3/fix-receipt.json)
has SHA256 `bcb5091f3bc7686bd3272e666f256649cab59d5745f88aea48ad67cced7612bc`.
The timing assertion uses the documented cache-case formula; it does not
qualify cache misses, custom-chip arbitration or hardware-cycle accuracy.
Later unrelated live M68kCore edits were neither absorbed nor reverted.

Rebinding only the CPU DLL/PDB lets the retained 68020 normal-input HUNK pass
six authored invocations, including two NULL-D3 ReadArgs/FreeArgs pairs and
twelve StackSwaps. Its HUNK and map bytes are identical to the earlier failed
checkpoint. [Replay receipt](D:/Koodit/GIT/CopperOS/obj/dos-native-runcommand-input/d2899a05e9e941e0bee11724fb5861d7/receipt.json)
SHA256 `ca80d76d7cec12542d8de189665f7b7ce46d169962e6cc904014aa667e75d3e8`
binds 253 retained sources and 141 binaries. The original failed three-CPU
matrix remains failed; the historical 000/040 passes and this 020-only pass
use distinct CPU checkpoints. No original command comparison is added.

## 2026-08-30 - Partial synthetic retirement proof

RET02 now requires the actual scheduler's private installation commit before
current-task cleanup. It checks live/saved CPU continuations, task/list state,
retained-address admission and immutable allocation metadata before and after
the DOS barrier. A read-only query extends the protected spans to the existing
Classic CSTK descriptor and its rounded allocation, without changing guest
layout or allocator behavior.

Three unchanged before/after selections cover 31 synthetic, four admission and
15 descriptor cases. All 50 new cases and 34 controls pass in the retained
later baseline. The old RET01 source still has one explicitly retained raw-copy
positive assertion failing; it is not included in an all-green result. The
[retirement record](task-retirement-qualification.md) links exact receipts and
support limits. RET02 remains partial: unsupported allocator/stack forms,
native Switch, provider pointers and DOS callback cancellation are still open.
The next RET03 baseline reproduces four safe-but-stuck callback retirements,
with six guard controls and no premature frees.

## 2026-08-30 - Close BOOL correction, native write-error gate open

The DOS owner now returns a V36+ BOOL from Close, and its startup-script,
Shell-output and variable-writing consumers interpret it correctly. The same
30 portable cases move from 24 failures/six controls to 30 passes; 294 related
checks pass. The host adapter's failed-initialization result moves from two
failures/three controls to five passes, with 37 related host checks passing.
Eight PE/PDB pairs and 484 retained source documents match. Source deltas,
runtime inputs, migrated legacy assertions and setup-only failures are retained
in the [Close qualification record](close-boolean-qualification.md).

Actual native execution then exposes a separate error: ACTION_WRITE returns
DiskFull but successful End erases the failure before Close returns. Cleanup
and ABI guards pass, yet Close reports true and IoErr zero. Fresh per-write
outcomes and first-error preservation are the next owner correction. Portable
BOOL success does not close this native gate, original failure equivalence or
any command/purity/package row.

## 2026-08-30 - Signal state guard and later original boot progress

The actual Signal owner now posts pending bits without moving running/ready
tasks onto the ready list; only matching waiting tasks take the existing wake
path. Four focused tests move from two failures/two controls to four passes.
Sixteen related cases pass after correcting one retained malformed fixture
which had omitted Task.State=Waiting. The production change is one guard;
source/PE/PDB and the earlier failing controls remain separately recorded.

The [original boot replay](reference-execution-paths.md) then changes only the
source-built Exec DLL/PDB in the frozen runtime. The same observer captures
one valid and 23 invalid DOS-root list samples before, and 24 valid samples
after. The guest progresses to InitialCLI and creates CON and RAM tasks.
The immutable pair receipt is
[sigboot3f872a2d](D:/TestData/CopperOSCommands/Q/sigboot3f872a2d/receipt.json),
SHA256 `5fdd6d976061b0c15eef42b7b1aa099eef9a912f08286f156f5ed0ba8be0e23d`.
A later read-only owner observation identifies a startup wait while InitialCLI
runs `C:AddDataTypes`; it does not demonstrate an interactive prompt. There
are no added explicit command, R07, application/stream or full-boot results.

## 2026-08-30 - Read-only retirement context and release-range groundwork

RET03.1 inspects complete portable context/input ownership and the named-task
host callback stack without cancelling or restoring anything. Its shared
65536-visit work budget also bounds nested/repeated scans. The private
[inspection checkpoint](D:/TestData/CopperOSCommands/Q/ret03inspectab01/receipt.json)
passes 31 new portable cases within 337 checks and 26 new host cases within
63 checks. It uses the earlier frozen Close BOOL source base, not a composed
build of all subsequent changes. The four actual RET03 liveness failures and
the callback/provider barrier remain unchanged.

The independent [FreeMem inspection record](free-range-inspection-qualification.md)
qualifies a query for the full rounded Classic release range, typed metadata
alignment, readable non-CSAD probe and bounded valid allocator topology.
Initial missing-API baselines are distinguished from a later real defect:
two odd-metadata cases reached forbidden 68000 word reads in a strict adapter.
With only an even-address admission guard added, the unchanged 95-test DLL
passes all 59 query/owner cases and 36 existing allocator/vector/pool/handler
controls. The final Exec DLL is
`434b93b242bc1cfc31e634fcf89c4c5a2afac61e95d67806bb0c95542a672f04`;
the [final receipt](D:/TestData/CopperOSCommands/Q/freegeomcd492bd1/alignment-after-v2-receipt.json)
is `da6255aaf0e2583b169b30b392d844fc42660cc8feafd9656d9073fa8bbbf607`.

Private source/PE/PDB and six actual reference/Compile bindings are retained,
including rejected fixture/harness setup attempts. Geometry is not allocation
ownership or release permission: the future issuer must prove every surviving
callback, CPU and I/O owner has stopped using the complete range. No cleanup,
native CPU, command, purity or packaging qualification is added here.

## 2026-08-30 - Buffered Close errors and explicit inspection write traps

The separate [Close write-error checkpoint](close-write-error-qualification.md)
now passes 45 actual native cases across 68000/020/040, all 323 selected portable
checks, 64 host checks and 14 existing installed/provider checks. The new
provider operation returns the fresh byte-write result; Close keeps the first
flush error through End and cleanup. A failed flush retains only its unwritten
suffix, so a later Flush/Close does not duplicate a completed prefix. The native
matrix uses frozen sources, one unchanged test DLL and preserved HUNK/maps;
its 30 adapted-before failures and 15 controls remain retained.

The main receipt is
[cc43a7f7](D:/TestData/CopperOSCommands/Q/closeWriteafea7960/write-error-qualification.json).
The [14 existing checks](D:/TestData/CopperOSCommands/Q/closeWriteafea7960/installed-regressions01/receipt.json)
have their own capture. This fixes generated DOS behavior under supplied
providers; it does not establish original error precedence, raw Write/FPutC
failure handling, resident commands or a composed boot image.

[RET03.1](runcommand-retirement-inspection-qualification.md) also has a separate
host adapter checkpoint: the unchanged 74-test DLL moves from 11 missing-adapter
failures and 63 controls to 74 passes. Every write, clear or copy now throws,
including a hypothetical transient mutation that final RAM snapshots alone
could not detect. Its [after receipt](D:/TestData/CopperOSCommands/Q/ret03inspectguard02/after-receipt.json)
is `94f14c0fbb6f2f738dc63419ad4cf998acf1e292d3565f08584687c6f2cf3fbe`.
The four actual retirement liveness failures remain open; this is read-only
inspection, with no lease, stack handoff or cleanup enabled.
## 2026-08-30 - Original Rename 68020 instruction obstacle resolved

The [byte-immediate postincrement correction](cpu-byte-postincrement-qualification.md)
passes all 144 focused and 857 selected CPU cases. Before the correction, the
same test DLL records 60 unsupported-timing failures on 68020/030 and 84 focused
controls on 68000/010/040. The 84 controls retain identical CPU states, cycles,
memory and bus traces. Only the advanced decoder/executor and its appended
internal timing key change; no general timing fallback is added.

The unchanged original Rename fixture now records 40 returned cases and eight
separate hazard stops on each of 68000/020/040. Its before 68020 replay still
stops after 16 returns at opcode12FC, and both older failed matrices remain
retained. CPU DLL/PDB are the only changes between replay runtimes. The
qualification receipt is [0d9c6381](D:/TestData/CopperOSCommands/Q/move12fc861db2a9/qualification.json);
the after CPU is `d29fcdbedf4732ee4f0209d0f2a0ff2bb250a6bda826d9a08ffbdfdaebefdd2f`.
Two byte-identical rebuilds retain 444 unchanged consumed-input records, and
four PE/PDB pairs match all 105 source documents. The original executor has
document matching, not a new source-bound rebuild claim.

`RN1-WB.CPU020` is complete within that supplied-vector scope. `RN2-WB.MATCHEND`
is next; original parser/matcher/handler behavior and an explicit safe policy
for the eight hazards still precede a replacement Rename. No command, profile,
purity or package gate changes, and the original goal/manifest stay unchanged.

## 2026-08-30 - Goal paused; in-flight checks retained

The goal controller reports `paused`. All delegated work has stopped; the two
already-running checks finished and no operation remains running. The
[pause handoff](pause-handoff-20260830.md) records the exact resume points:
raw Write/FPutC has 45 native passes plus 349 portable and 64 host passes, but
its aggregate audit and qualification document are unfinished; RET03.2A has
an accepted 145-host/337-portable checkpoint, followed by a distinct owner fix
that is unbuilt and untested; the original AddDataTypes address-error trace is
retained but its provider cause is not yet qualified. RN2 remains research only.
No completion counts change, and the stable goal and manifest hashes are
unchanged. This is a bookkeeping handoff, not a goal resumption.

## 2026-08-30 - RET03.2A owner getter revalidation

The resumed private after build passes all 152 unchanged host cases after the
retained five-failure stale-binding baseline. The new post-`GetExecBase`
revalidation rejects a changed memory-owner binding rather than recapturing it
as valid. The [qualification record](runcommand-retirement-range-owner-revalidation-qualification.md)
binds the current source, source delta, private host build, unchanged test DLL
and receipts. It remains read-only: no task retirement, free, native scheduling
or command gate is credited. Next RET03 work must census surviving callback,
I/O and scheduler roots before proposing a cleanup consumer.

## 2026-08-31 - Raw Write and FPutC aggregate audit

The prepared aggregate auditor completed and revalidated 1,349 bound inputs.
The [qualification record](direct-write-error-qualification.md) seals 45
generated-native cases over 68000/020/040, 349 portable checks and 64 frozen
host controls. It preserves the red baselines and qualifies only checked-byte
provider failure propagation. Original packet-result behavior, provider capacity
rollback, command integration, residency and package gates remain open.

## 2026-08-31 - Original Utility NamedObject ABI correction

The original `C:AddDataTypes` trace and the NDK 3.1 public `NamedObject`
definition identify a concrete offset-zero ABI violation: generated Utility
stored its private marker where `no_Object` must point to `ANO_UserSpace`. The
[correction record](utility-namedobject-layout-correction.md) binds the source
and its five-passing focused test. A private frozen-runtime replay substitutes
only the rebuilt Utility DLL/PDB and no longer sees the old InitialCLI vector-3
fault or invalid `InitSemaphore` call. Its legacy observer then reaches repeated
vector-8 diagnostics, fills its fixed buffer, and fails at boundary 67,312; the
receipt is `812bc445787163ca5297a1e7c2deb1d12237bf7674afdee45c8060285dfe3990`.
This rules out only the recorded pre-fix boundary. A separate bounded read-only
observer now passes one observation: it stops at the first next exception,
vector 8 at `$00F80BBE` (opcode `$007C`) on boundary 709, without an overflow,
listener mutation or disk change. Receipt
`e5d8dd0c6cfc7f94a94dc2f3c2b2ad0fc4486126f2d43e1b1c0a06a821893dd2`
retains that private result. Vector-8 attribution is still open; no R07, boot,
command or provider-completion credit is added.

## 2026-08-31 - Rejected initial task supervisor-state hypothesis

The bounded follow-up reaches vector 8 at original ROM `$00F80BBE` while the
InitialCLI task's SR is `$0018`. An isolated Exec rebuild changed the new-task
frame default from zero to the supervisor bit, then paired that DLL with the
corrected Utility DLL in the same frozen observer. The result is unchanged:
vector 8 remains the first observed exception at boundary 709 with SR `$0018`.
That initialization is not the owner of this state, so the speculative source
change was reverted. The vector-8 origin and the state transition that precedes
the RAM JMP remain open; no startup, command, R07 or provider-completion result
is claimed.

## 2026-08-31 - Execute raw-tail transport

The bounded Execute X1 implementation no longer feeds all command bytes to
`ReadArgs(FILE/A)`. It asks the DOS-owned ReadItem parser for the raw FILE
boundary, calls ReadArgs only for that prefix, and copies the exact remaining
bytes into the runner and Shell frame before FreeArgs. A focused test confirms
the literal raw suffix `" extra"`; ReadItem leaves the separator unread.
The runner record owns and frees the copied guest span, preserving pure/resident
per-invocation state.

Focused DOS, runner-layout/ownership, Shell-frame and Execute-wrapper tests
pass, and the local DOS-native Shell project builds after restore. This is
transport only: the script engine still has no `.KEY`/`.K` consumer, and no
original binary fixture, pending/native command entry, return/IoErr, purity or
package gate changes are claimed. The two Execute rows remain partial; `X2`
is their next bounded slice.

## 2026-08-31 - Execute original binary vocabulary audit

A read-only extraction from the verified Workbench Disk 2 ADF followed Execute
file-header block 244 to first-data block 245, joined its nine contiguous data
blocks and trimmed the result to 4,432 bytes. Its SHA256 is the already-recorded
`27c2f578b63857ab5fbe03cdf621d553408cc670b9f07d9988ce224d22b57ecd`.
Alongside `FILE/A`, the extracted original contains the directive keyword list
and individual diagnostics for duplicate, illegal and missing `.KEY`, invalid
directives/data/arguments, unsuitable parameters and overlong keys. This adds
static vocabulary evidence to the Execute contract. It does not execute the
original command or supply the `.KEY` grammar, substitutions, diagnostics,
result or IoErr fixtures required before implementing an interpreter stage.
The Execute rows remain partial and keep `X2` as their next slice.

## 2026-08-31 - Execute directive lexical boundary

Added `ShellScriptDirective`, a fixed-width, non-allocating classifier for the
documented dot directive spellings and their `.K`, `.DEF` and `.DOL` aliases.
The script engine now skips `. `, `.\\` and semicolon-leading comment lines
before lookup. It reports unknown dot lines as malformed instead of attempting
to run them as external commands. Fifteen focused directive/comment tests and
the full Commands suite (448 passed) pass; `CopperOS.Shell.Dos` builds with no
warnings or errors.

This deliberately does not treat a recognized directive as implemented. There
is still no frame-owned KEY template/result state, `ReadArgs` binding, default
or delimiter transform, temporary-file lifecycle, original behavior fixture or
native command result. The Execute rows remain partial and `X2` remains open.

## 2026-08-31 - Execute template ownership ABI

Extended the active Shell frame to v3/112 bytes and the DOS runner record to
v3/108 bytes. Every production runner now allocates a 4 KiB template buffer,
publishes it in both records and releases it on unpublished failure or terminal
runner teardown. Focused runner layout/state tests (19) and focused Shell frame,
directive and engine tests (37) pass, and the native DOS Shell project builds.

The buffer remains empty because KEY binding and substitution are not yet
implemented. No directive semantics, fixture, native command result or ledger
completion is claimed.

## 2026-08-31 - Execute KEY admission

The first `.KEY`/`.K` line now copies its template into the runner-owned frame
buffer and validates the retained raw Execute suffix using DOS `ReadArgs`. The
temporary RDArgs result is freed before script execution advances, so no
RDArgs-owned argument pointer survives a pending runner. Duplicate/later KEY,
missing template storage and malformed argument admission fail as script
malformed lines. The new focused binding case plus the full Commands suite
(449 passed), and the focused runner/layout suite (19 passed), pass.

## 2026-08-31 - Execute KEY reference substitution

Following a successfully admitted `.KEY`/`.K`, each script line now reparses
the runner-owned raw tail with the runner-owned template through DOS `ReadArgs`.
Matching case-insensitive `<name>` references are copied into a caller-owned
buffer, the transient RDArgs is freed, then normal alias expansion and
redirection proceed. The production runner uses its existing distinct line,
redirection and alias buffers so neither parser observes overlapping spans.
The focused test proves `.KEY filename/A` plus `Echo <filename>` dispatches
`Echo value`; the complete Commands suite (450 passed), focused runner/layout
suite (19 passed), and native DOS Shell build pass.

## 2026-08-31 - Execute DEF defaults

`.DEF name value` now tokenizes its quoted value through the existing Shell
parser and appends a length-prefixed, case-insensitive default record to unused
capacity in the runner-owned 4 KiB template buffer. When a later ReadArgs pass
leaves the matching `.KEY` value null, the substitution pass emits the newest
recorded default. No persistent RDArgs or managed collection is introduced.
The focused default case and the complete Commands suite (451 passed) pass;
the native DOS Shell project builds without warnings.

Per-reference `$` defaults, bracket/dollar/dot delimiter behavior,
unknown-reference behavior, diagnostics, temporary-file lifecycle and an
original behavior fixture remain open. The Execute rows remain partial and X2
remains open.

## 2026-08-31 - Execute BRA and KET delimiters

The runner-owned template allocation now has a fixed directive header. `.BRA`
and `.KET` update its opening and closing delimiter bytes after strict
one-character token validation. The substitution pass reads those bytes on
every line, so the selected delimiters survive pending execution without an ABI
change or managed state. The focused brace-delimiter case and the complete
Commands suite (452 passed) pass; the native DOS Shell project builds without
warnings.

Per-reference defaults, `.DOLLAR`, `.DOT`, diagnostics, temporary-file
lifecycle and original-fixture behavior remain open. The Execute rows remain
partial and X2 remains open.

## 2026-08-31 - Execute DOLLAR defaults

`.DOLLAR` and its `.DOL` alias now change the runner-owned inline-default
separator. While expanding a matching reference, a non-null ReadArgs value
wins; a null value uses the reference-local default, then the script-wide
`.DEF` record if no inline value was supplied. This leaves the established DEF
storage and ReadArgs ownership path intact. Focused directive tests and the
complete Commands suite (453 passed) pass; the native DOS Shell project builds
without warnings.

`.DOT`, directive ordering, diagnostics, temporary-file lifecycle and original
fixture behavior remain open. The Execute rows remain partial and X2 remains
open.

The CC12 AddBuffers slice now has an independent native command body and
startup entry using DOS `ReadArgs`, `AddBuffers`, `VPrintf`, `PrintFault`, and
`FreeArgs`, with invocation-owned formatting storage. Its first 68000 static
resident entry is 2,396 bytes with ten reachable methods and no managed runtime
features/helpers, external targets, exception regions, or fatal fault sites.
Supplied vector execution and the other CPU receipts are next; no source/binary
parity or real handler behavior is claimed.

Relabel now has a source-observed native body: it validates NAME and DRIVE,
uses a combined device/volume `LockDosList`/`FindDosEntry`/`UnLockDosList`
sequence before invoking Relabel, and owns parser/scratch cleanup per call. Its
first resident 68000 entry is 2,804 bytes with eleven reachable methods and no
managed runtime features/helpers, external targets, exception regions, or fatal
fault sites. Supplied DOS-vector execution and the remaining CPU receipts are
still open.

`qualify_relabel_native.ps1` now passes the bounded Relabel entry across
68000/020/040: eleven reachable methods and eight supplied parser/list/mutation
calls per CPU, with zero managed runtime features/helpers, external targets,
exception regions, fault sites, leaks, or shared-image writes. The receipts are
68000 `48ca44160dd896c5af502a03fc74688035d643d258541c8b8bde81903ef17724`
(2,804 bytes), 68020
`b2d9ce7afe730453fbc84bc9f041f66122c5d036a8b1109042fbbedd6cca3d86`
(2,848 bytes), and 68040
`df3bb8c026666f229f1e86f7db352dfb105ecd98278686e03ec303b092699759`
(2,804 bytes). This is not a real parser, handler, packaged-command, or
reference-parity result.

## 2026-09-04 - Assign evidence boundary

The [Assign contract](contracts/Assign.md) now binds both observed media members
and keeps their evidence separate: the classic 37.4 HUNK exposes a raw-string
syntax candidate, while the packed MorphOS 51.1 member exposes no safe template
candidate. The observed startup resident add/remove statements and MorphOS
installer `P` intent require a pure/reentrant replacement design, but do not
prove installed metadata or a resident lifecycle. The SDK's public assign
vectors are available for a future body; selection among them remains blocked
on controlled reference behavior rather than guessed from switch names.

The [MakeLink contract](contracts/MakeLink.md) now hash-binds MorphOS release
source: ReadArgs, soft/hard public-link calls, HARD target lock/FIB/examine
ownership, FORCE rejection, diagnostic branches, and its deliberately unset
final IoErr policy. The independent native entry passes eleven supplied
parser/link/lock/FIB calls on 68000/020/040 with nine reachable methods and no
runtime features/helpers, external targets, exception regions, fault sites,
leaks, or shared-image writes. HUNK receipts: 68000
`98980b5064e52cfa1f39c29a20aafe35ebe4d6d5bd47f9759933e0dc220bc442`
(2,580 bytes); 68020 and 68040
`7104b59d21ae324ea5116ae77ba63158a2247ffb6ca04894a57040d97f23edf8`
(2,580 bytes). This does not establish packed correspondence, real handler
effects, installed metadata, packaging, or reference parity.

## 2026-09-04 - Delete and Join source boundaries

The [Delete](contracts/Delete.md) and [Join](contracts/Join.md) contracts now
bind their MorphOS 3.20 source members without treating the packed members as
matched. Delete requires a full handler-backed matcher, link, protection and
partial-failure design; Join requires DOS pattern/direct-input behavior, exact
copy writes and incomplete-destination cleanup. Both are retained as source
contracts until their broad real-handler and reference capture gates can be
implemented without inventing filesystem behavior.

Copy now has a [source-bound contract](contracts/Copy.md) for the full MorphOS
multi-mode command. Its mode selection, DIRECT restrictions, matcher, loop,
metadata, exact I/O, cancellation and partial-result rules must be implemented
as one public-DOS command path; the source evidence does not permit a simplified
host-side copy substitute or a full-command claim.

`qualify_addbuffers_native.ps1` now passes the bounded AddBuffers entry across
68000/020/040: ten reachable methods, six supplied parser/handler calls per
CPU, and zero managed runtime features/helpers, external targets, exception
regions, fault sites, leaks, or shared-image writes. Hashes are 68000
`6a65951cbe52b9879149fedfec324fc0cd196b20ff2aba453761c24bf60411ef`,
68020 and 68040
`a1deca85fd41615efb27313b25947c8e326d6e2020b65590ee586220a61384e7`; all
HUNKs are 2,396 bytes. The qualifier supplies parser and handler results and
does not establish a real handler, packaged command, or reference parity.

## 2026-08-31 - Execute DOT directive prefix

`.DOT` now changes the resident directive-prefix byte. Before classifying each
subsequent line, the Shell reads the frame-owned value and passes it to the
allocation-free directive lexer; this includes comment forms as well as the
recognized directive names. The focused changed-prefix test and the complete
Commands suite (454 passed) pass; the native DOS Shell project builds without
warnings.

Directive ordering, diagnostics, temporary-file lifecycle and original fixture
behavior remain open. The Execute rows remain partial and X2 remains open.

## 2026-08-31 - Execute substituted pending runner

Added a lifecycle test for `.KEY filename/A` followed by a substituted external
command that enters a pending continuation. The test verifies the owned template
remains valid while pending, the dispatched command is expanded, and the final
poll releases the continuation and preserves its result. This is CopperOS
ownership coverage only; no original scheduling behavior is claimed.

## 2026-08-31 - Original Execute KEY substitution fixture

Created a checksum-verified disposable copy of the licensed Workbench 3.1 ADF
that changes only the already allocated `S/Startup-Sequence` data blocks. The
headless WinUAE 6.0.3/68000 run executes original `C:Execute` with `.KEY
filename`; its inner script records `fallback`, and the caller records `END`.
The hash-bound capture records the reference command, ROM, emulator, probe and
post-run capture hashes. It proves one positional substitution case only; it
does not close temporary-file, diagnostic, status, directive-order or MorphOS
gates.

The same inner script was also run from a startup probe that omitted `Assign
T:`. It produced the same `fallback` and caller `END` capture. This shows the
observed positional case does not require a caller-installed `T:` assignment;
it does not establish the original temporary-file implementation or failure
behavior when a real temporary-file operation fails.

## 2026-08-31 - Execute DEF grammar tightening

The documented `.DEF` grammar permits a value separated from its formal `.KEY`
name by whitespace or `=`, permits an empty value, and admits only one default
per formal name. The allocation-free resident implementation now validates the
formal name, rejects a duplicate, accepts `name=value` including `name=`, and
retains the decoded value in runner-owned storage. Two focused engine cases
cover empty `name=` expansion and duplicate rejection; the complete Commands
suite passes with 457 tests. This is documentation-guided behavior only: the
attempted disposable WB 3.1 `.DEF` probe did not yield a durable guest capture,
so no original-runtime fixture result is claimed.

## 2026-08-31 - Execute pre-scan ordering correction

The AmigaDOS script manual requires a script starting with a dot command to be
scanned and transformed before dispatch. Its own `.BRA`/`.KET` example appears
after an earlier `ECHO`, so the final delimiter configuration must affect the
whole transformed script rather than only subsequent lines. The local
step-by-step runner remains intentionally open for this behavior; no
first-command directive cutoff is retained. The next Execute slice is a
runner-owned pre-scan and temporary-file lifecycle with original guest fixtures.

## 2026-08-31 - Execute pre-scan ABI work package

The runner audit found that the current DOS record tracks only one input handle,
and the Shell bridge has no transactional input replacement or owned-file
deletion operation. The [pre-scan ABI work package](execute-prescan-abi.md)
defines the required source/transformed ownership fields, exact-write failure
handling, unique `T:` naming, input replacement transaction and acceptance
cases. It avoids a shared temporary filename or Shell static state. This is
design evidence only; the CopperStart ABI and scanner are still open.

The first ABI foundation is now implemented: CopperStart's script runner is
v4/116 bytes and owns a 256-byte temporary-path buffer. Both the native and
host Shell adapters allocate, validate and release that buffer; the DOS runner
release test asserts the exact free. The Commands suite passes 457 tests and
the focused CopperStart runner/layout suite passes 25 tests. Source/reader
replacement, work-file deletion and the scanner remain open.

The runner codec now also validates and publishes a pre-opened temporary reader
with its exact owned path buffer. Its focused test covers replacement input,
path length and rejection of a foreign buffer; 19 focused runner/layout cases
and the native Shell build pass. This is the record-publish half of the
transaction only, not a temporary-file implementation.

The matching Shell-frame transition now switches to a prebuilt replacement
reader and resets line/offset to the start, refusing to retarget a live buffered
input record. Its focused frame test and the full Commands suite (457) pass;
the native Shell build has no warnings or errors.

The Shell script platform now exposes a publish-only replacement-input step.
Both adapters bind the DOS runner record and Shell frame only when the CLI,
frame and current source input agree; source close/delete ordering remains with
the future scanner transaction. The full Commands suite passes 458 tests and
the native Shell build remains clean.

CopperStart runner release now deletes a published temporary path after closing
the active input, and its lifecycle test records that deletion. The focused
runner/layout suite passes 19 tests and the native Shell build is clean. The
scanner still needs to close the original source before publishing its reader.

The runner record is now v5/124 bytes and additionally owns an exact,
NUL-terminated copy of the source path plus its dynamic length. Both Shell
adapters preserve every currently accepted Execute path length (through 65,535
bytes), validate the copy, and free it on all unpublished and terminal paths.
This makes the required no-managed-storage two-pass transform possible: scan
the original reader for global directive state, reopen the owned source path,
then write and publish the temporary reader. The focused CopperStart
runner/layout suite passes 25 tests and the full Commands suite passes 458;
the native Shell project builds without warnings or errors. The actual scanner
and provisional-artifact rollback remain open.

The current DOS line bridge advances a handle and does not seek from its
`currentOffset` argument. The pre-scan therefore has a defined separate-probe
reader: it opens `ScriptSourcePath` only to classify the first line, closes that
probe, and leaves the runner's original reader untouched for no-dot scripts.
The runner source-path lifecycle test now writes its path before record
initialization, so codec validation covers the real NUL terminator; the 25
focused runner/layout tests remain green.

## 2026-08-31 - Execute resident two-pass pre-scan

`ShellScriptPreScanner` now performs the planned no-managed-storage transaction
for a dot-first Execute source. It probes through a separately opened source
reader, scans global `.KEY`, `.DEF`, `.BRA`, `.KET`, `.DOL`, and `.DOT` state
through the existing DOS `ReadArgs` and resident key-expansion helpers, then
reopens the source to write only non-directive lines into the unique
`T:Execute.<frame>` artifact. The active runner reader remains unchanged for a
non-dot script. For a transformed source, the adapter validates, closes, and
publishes the replacement reader; runner release owns its final close/delete.

The new portable tests cover no-dot bypass, late `.BRA`/`.KET` and `.DEF`
directives affecting earlier command lines, and short-write rollback that closes
provisional handles and deletes the unpublished artifact. The full Commands
suite passes 462 tests; the focused pre-scan tests pass 28, the native Shell
project builds cleanly, and
the focused CopperStart runner/layout suite passes 25. Original Execute fixture
comparison and native DOS fault injection are still required before reference
qualification.

## 2026-08-31 - Execute directive-order fixture correction

The hash-bound disposable Workbench 3.1 probe
[`execute-wb31-late-brackets.json`](reference-captures/execute-wb31-late-brackets.json)
executes original `C:Execute` with `.KEY filename`, an earlier `Echo
{filename}`, then `.BRA {` and `.KET }`. Its captured output is
`BEGIN`, literal `{filename}`, and `END`. This contradicts the tentative
documentation-only inference that later delimiters apply retroactively. The
resident pre-scan now copies every source line into the unique runner-owned
temporary input and leaves `.KEY`, `.DEF`, `.BRA`, `.KET`, `.DOL`, and `.DOT`
for the dispatcher to apply in source order. The paired portable regression
covers the preserved temporary stream and verifies that the earlier line stays
literal while a later line expands with the changed delimiters. The full
Commands suite passes 462 tests.

This one original fixture does not qualify temporary-file internals,
diagnostics, status/IoErr, native DOS fault paths, or MorphOS behavior.

## 2026-08-31 - Original WB31 Which direct-lookup fixture

The hash-bound disposable Workbench 3.1 probe
[`which-wb31-basic-lookup.json`](reference-captures/which-wb31-basic-lookup.json)
records original `C:Which Execute` and `C:Which C:Execute`. Each emits the
same `Workbench3.1:C/Execute` line. A following missing-name attempt prevents
the fixture's `END` marker even with `FailAt 20`, proving only an error-level
boundary. It deliberately makes no claim about the exact result, IoErr,
diagnostic stream, lookup categories, switches, or MorphOS. W1-WB remains
open for those required captures and for implementation through a shared
lookup-enumeration owner.

A second disposable fixture records the internal category exactly:
[`which-wb31-internal-category.json`](reference-captures/which-wb31-internal-category.json)
captures `INTERNAL CD`. Its attempted temporary resident setup exits before
the resident query, so no resident label or lifecycle result is claimed.

The corrected resident fixture supplies the normal `Path C:` setup and uses
the exact original `Resident >NIL: C:Execute PURE` / `Resident >NIL: Execute
REMOVE` lifecycle. Its hash-bound result
[`which-wb31-resident-category.json`](reference-captures/which-wb31-resident-category.json)
captures `RES Execute` and reaches `END` after removal. The direct, internal,
and resident labels are now reference-backed; order, ALL/NORES interactions,
failure details, and MorphOS remain open.

The two subsequent hash-bound option fixtures establish that `NORES` selects
`Workbench3.1:C/Execute`, while `ALL` emits `RES Execute` then that identical
path twice. This is controlled evidence that the classic command includes the
resident match first and does not deduplicate equivalent path discoveries. Both
fixtures stop before their post-query marker, so their final status, `IoErr`,
and diagnostic behavior remain open.

`WhichOutputFormatter` now supplies the three observed display forms using
only caller-owned guest storage and exact DOS writes: resolved path, `INTERNAL
<name>`, and `RES <name>`. Four focused tests cover the captured output bytes,
short-write failure, and overlapping work-space rejection; the full Commands
suite passes 466 tests. It is a presentation component only: lookup
enumeration, switches, diagnostics, native entry, and MorphOS behavior remain
separate work.

## 2026-08-31 - WB31 Which bounded native lookup slice

[Workbench31WhichCommand.cs](D:/Koodit/GIT/CopperOS/src/Commands/Native/Workbench31WhichCommand.cs)
now supplies a source-level Workbench 3.1 command body for the verified
`FILE/A,NORES/S,RES/S,ALL/S` grammar. It uses the shared invocation-owned
`ReadArgs` lease, `FindSegment` under a short resident-list protection interval,
and `Lock`/`NameFromLock`/`UnLock` plus the CLI `CommandDirectory` and
`AddPart` for ordinary candidates. It releases each lock before continuing and
never holds `Forbid` while writing. The classic direct, `RES`, `NORES`, and
`ALL` output forms remain reference-backed only through the recorded original
fixtures; this source slice is not a runtime comparison.

An isolated native compile of the body and its startup/argument dependencies
passes with zero warnings. The full native commands project remains blocked
before this command by the existing `NativeExe2ArcIo` reference to missing
`IExe2ArcIo`. The command has no HUNK execution, diagnostics, cancellation,
exact option-combination, disabled-internal, long-path, or MorphOS coverage, so
only `CC10.Which.wb31` semantics advances from open to partial.

## 2026-08-31 - WB31 Which resident default/options fixture

The marker-based disposable fixture
[`which-wb31-resident-default-options.json`](reference-captures/which-wb31-resident-default-options.json)
now records all four classic forms after `Path C:` and `Resident >NIL:
C:Execute PURE`: default `Which Execute` and `Which Execute RES` each emit
`RES Execute`; `NORES` emits `Workbench3.1:C/Execute`; and `ALL` emits the
resident line followed by that identical disk path twice. The fixture reaches
`END` after resident removal. Its `FailAt 20` setup constrains the four query
results only to below 20; it does not establish their exact return level,
`IoErr`, diagnostics, option conflicts, current-directory precedence, or any
MorphOS behavior.

## 2026-08-31 - WB31 Which internal option fixture

The hash-bound disposable
[`which-wb31-internal-options.json`](reference-captures/which-wb31-internal-options.json)
records `INTERNAL CD` for both default `Which CD` and `Which CD RES`. `Which
CD NORES` emits no line and leaves the following marker reachable under
`FailAt 20`. `Which CD ALL` emits `INTERNAL CD` and then prevents the final
`END` marker, which is threshold evidence of a result at least 20 only. This
narrows category inclusion but does not establish an exact return level,
`IoErr`, diagnostic, disabled-internal behavior, or MorphOS parity.

## 2026-08-31 - WB31 Which native HUNK generation

The private
[`Commands.WhichNativeRoot`](D:/Koodit/GIT/CopperOS/tests/Commands.WhichNativeRoot)
provides a real entry for `Workbench31WhichCommand` and builds independently of
the incomplete aggregate native project. The local compiler emits stripped
68000/020/040 HUNK images with valid `HUNK_HEADER` words and recorded hashes in
the [Which contract](contracts/Which.md). This is an actual native code
generation checkpoint, while instruction execution, DOS-vector behavior,
original comparison, purity, packaging, and MorphOS behavior remain open.

## 2026-09-01 - WB31 Which resident cleanup checkpoint

`Workbench31WhichCommand` no longer uses a managed `try/finally`; every result
path reaches one explicit cleanup label that frees only its invocation scratch
and ReadArgs lease before restoring IoErr. The new
[`qualify_which_native_entry.ps1`](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_which_native_entry.ps1)
rebuilds resident 68000/020/040 HUNKs. All three static reports have 14
reachable methods, zero managed allocation sites, exception regions, fatal
machine-fault sites, helpers and external targets; the SDK `nullable-values`
feature remains through `Lock`. This is a static resident checkpoint only. It
does not execute Which instructions, parse actual DOS input, supply filesystem
handlers, or establish original/MorphOS parity or final pure lifecycle behavior.

## 2026-08-31 - WB31 Which positive resident status

The hash-bound [status fixture](reference-captures/which-wb31-status.json)
establishes `RC=0` and `Result2=0` for both default `Which Execute` and
`Which Execute RES` after a pure `Execute` resident is installed. The fixture
stops at its following `NORES` call; no later status is inferred from it. This
closes only those two positive status observations and leaves `NORES`, `ALL`,
miss, diagnostic, cancellation, and MorphOS status behavior open.

## 2026-08-31 - WB31 Which NORES status

The isolated [NORES status fixture](reference-captures/which-wb31-nores-status.json)
now establishes that `Which Execute NORES` emits `Workbench3.1:C/Execute` and
leaves `RC=0`, `Result2=0` with a matching pure resident present. It reaches
`END` after removal. `ALL`, misses, diagnostics, cancellation, internal status,
and MorphOS results remain open.

## 2026-08-31 - WB31 Which ALL status

The [ALL status fixture](reference-captures/which-wb31-all-status.json) binds
the reference order (`RES Execute` followed by the disk path twice) to
`RC=0`, `Result2=0`. It reaches `END` after removing the resident. This does
not resolve the observed internal-only `ALL` failure, missing results,
diagnostics, cancellation, or MorphOS behavior.

## 2026-08-31 - WB31 Which internal-only ALL status

The isolated hash-bound
[internal-only ALL fixture](reference-captures/which-wb31-internal-all-status.json)
uses `FailAt 255`, which itself leaves `RC=0`, `Result2=0`. Original
`C:Which CD ALL` then writes `INTERNAL CD` and leaves `RC=5`, `Result2=205`
before the `END` marker. The native bounded slice preserves that exact observed
result only when an INTERNAL match is the sole eligible result after `ALL`.
Default/RES/NORES internal status, missing names, diagnostics, cancellation,
and MorphOS parity remain open.

## 2026-08-31 - WB31 Which no-match status

The hash-bound [no-match fixture](reference-captures/which-wb31-no-match-status.json)
now records two exact original results under `FailAt 255`: `C:Which CD NORES`
and `C:Which CopperOSMissingCommand` each produce no captured line and leave
`RC=5`, `Result2=205`. The native bounded slice now returns that observed
WARN/ObjectNotFound pair for a completed lookup with no eligible match. This
does not establish `RES`-only misses, diagnostics on a separate stream,
inaccessible paths, cancellation, or MorphOS behavior.

The targeted [RES missing fixture](reference-captures/which-wb31-res-missing-status.json)
then establishes the same `RC=5`, `Result2=205` outcome for `C:Which
CopperOSMissingCommand RES`. The native resident-only early return preserves
that result. Option conflicts, diagnostics, inaccessible paths, cancellation,
and MorphOS behavior remain open.

## 2026-08-31 - WB31 Which internal conflict combinations

The high-threshold [combination fixture](reference-captures/which-wb31-internal-combinations-status.json)
confirms default/RES success and the known NORES/ALL failures for `CD`. It
also records the distinct `NORES RES` result: WARN/5 with `Result2=0`, while
`NORES ALL` is WARN/5 with `Result2=205`. The native resident-only conflict
branch preserves that captured zero secondary result when `ALL` is absent.
The following `RES ALL` invocation did not reach its marker, so this is a
recorded execution boundary rather than a parity result. A shorter isolated
[RES ALL fixture](reference-captures/which-wb31-res-all-status.json) then
establishes `INTERNAL CD`, `RC=0`, `Result2=0`; a matching
[NORES RES ALL fixture](reference-captures/which-wb31-nores-res-all-status.json)
records no output with `RC=5`, `Result2=0`. All eight classic switch
combinations are now captured for the internal `CD` name only.

## 2026-08-31 - WB31 local media availability

A read-only inventory of `D:/TestData/TestImages` found only the selected M10
40.42 Install and Workbench ZIPs. The [availability capture](reference-captures/wb31-local-media-availability-20260831.json)
records their names and byte counts plus the unavailable disks 3–6, machine
variants and clean installed-system evidence. CC00 remains open; this prevents
the two-disk command inventory from being presented as distribution closure.

## 2026-09-01 - MorphOS PathPart bounded native checkpoint

[`MorphOSPathPartCommand`](D:/Koodit/GIT/CopperOS/src/Commands/Native/MorphOSPathPartCommand.cs)
now parses the documented candidate `DIR/K,FILE/K,ADD/K/M` grammar with the
shared native ReadArgs lease. It uses only DOS `PathPart`, `FilePart`, and
`AddPart` for lexical operations and one invocation-owned 1,024-byte buffer;
the companion private native root compiles cleanly and emits valid 68000/020/040
HUNK headers with hashes in the [PathPart contract](contracts/PathPart.md).
No MorphOS runtime or output-parity claim is made: the documented template,
combined-mode presentation, diagnostics, and pure execution remain open.

## 2026-09-01 - MorphOS PathPart resident instruction checkpoint

`MorphOSPathPartCommand` now uses an explicit cleanup path instead of a managed
exception region, allowing its normal DOS-startup root to compile in required
resident YOLO mode. [`qualify_pathpart_native_entry.ps1`](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_pathpart_native_entry.ps1)
builds 68000/020/040 HUNKs and executes 11 supplied post-ReadArgs vectors per
CPU using Copper68k. They cover DIR, FILE, ADD, the current candidate combined
and no-mode paths, parser/allocation failure, repeated calls and interleaved
callers sharing one image. All 33 invocations preserve the image and balance
owned allocations, DOS and RDArgs resources. The suite's DOS adapters provide
synthetic path-helper answers; it does not parse DOS input, use a filesystem,
execute MorphOS, or establish the candidate's observed output/combination
semantics.

## 2026-09-01 - MorphOS Quote READITEM stage

[`QuoteReadItemFormatter`](D:/Koodit/GIT/CopperOS/src/Commands/QuoteReadItemFormatter.cs)
is a caller-owned, bounded transformation for one READITEM rule value. Its
eight focused tests establish plain-token preservation, quoted spaces, star,
quote, LF, empty values, and the MorphOS 3.20 release-note regression that
`A=B` must become `"A=B"`; overlap and short-buffer inputs leave memory
unchanged. It is deliberately only a stage: Quote's source precedence, other
rules, reverse mode, output/error behavior, native entry, purity, and runtime
comparison remain open.

[`QuoteHexFormatter`](D:/Koodit/GIT/CopperOS/src/Commands/QuoteHexFormatter.cs)
now supplies a second bounded forward rule stage. Four focused cases cover
empty, single-byte, published example, overlap, and capacity behavior. The
published MorphOS Library example is secondary evidence, so this does not
freeze the 3.20 executable's casing, reverse parser, stage wrappers, or status.

[`QuoteUriFormatter`](D:/Koodit/GIT/CopperOS/src/Commands/QuoteUriFormatter.cs)
adds a bounded URI-component stage with four focused cases: unreserved bytes,
the published colon/space/hash example, raw control/high bytes, and short
output rejection. Its percent digits are uppercase; that implementation detail
is still a secondary-source candidate pending actual MorphOS execution.

[`QuoteBase64Formatter`](D:/Koodit/GIT/CopperOS/src/Commands/QuoteBase64Formatter.cs)
now supplies the forward Base64 stage. Six focused tests cover empty input,
all padding lengths, the published example, overlap, and short output. Reverse
acceptance and command-level stage behavior remain open.

The same formatter now decodes canonical padded Base64. Its 16 focused cases
include all padding lengths, the published reverse example, malformed padding,
invalid alphabet, non-canonical low bits, overlap, and short output. This
strict reverse validation is bounded CopperOS behavior until MorphOS captures
establish whether its command accepts additional forms.

[`QuoteForwardPipeline`](D:/Koodit/GIT/CopperOS/src/Commands/QuoteForwardPipeline.cs)
now composes the four implemented forward stages with an external byte rule
list and two non-overlapping caller-owned scratch buffers. Two focused cases
verify declared ordering (`READITEM` then `HEX`) and reject unknown rules or
overlapping buffers before writes. It supplies no parser, source, reverse,
option, output, purity, or runtime-parity behavior.

[`QuoteRuleParser`](D:/Koodit/GIT/CopperOS/src/Commands/QuoteRuleParser.cs)
converts a bounded candidate `RULE` string into that list. Its two focused
cases cover all nine documented names, preserved order, ASCII case variation,
space/tab separators, and rejection of empty, unknown, short, or overlapping
inputs before writes. Actual MorphOS case and whitespace behavior is still a
reference-capture requirement.

The private [`Commands.QuoteNativeRoot`](D:/Koodit/GIT/CopperOS/tests/Commands.QuoteNativeRoot)
now links the production parser and four forward stages into a caller-bounded
native probe. It emits three resident-profile 68000/020/040 HUNKs with valid
headers, zero initialized RAM/BSS, and no managed allocation sites; hashes and
the compiler limitations are recorded in the [Quote contract](contracts/Quote.md).
The maps still contain managed strings and four fatal machine-fault sites, and
the root has no native execution fixture or command entry. It is only a static
reachability checkpoint and makes no pure/resident qualification claim.

`QuoteHexFormatter` and `QuoteUriFormatter` now decode strict bounded reverse
forms. The four new focused cases cover uppercase hexadecimal, malformed and
odd hex, mixed-case percent escapes, literal plus preservation, and failure
without destination writes. [`QuoteReversePipeline`](D:/Koodit/GIT/CopperOS/src/Commands/QuoteReversePipeline.cs)
applies `BASE64`, `URI`, and `HEX` in reverse declared order and rejects every
other family before scratch writes. MorphOS reverse permissiveness and ReadItem
unquoting remain open.

## 2026-09-01 - MorphOS Eval bounded expression checkpoint

[`EvalExpressionEvaluator`](D:/Koodit/GIT/CopperOS/src/Commands/EvalExpressionEvaluator.cs)
adds an independently written, caller-bounded parser for the documented
MorphOS 50.7 integer expression subset. Its 23 focused cases cover precedence,
right-associative power, parenthesis, unary operators, decimal/hex/octal/byte
literals, documented word aliases, malformed input, and explicit overflow,
zero-divisor, and shift failures. It introduces no source copying, DOS command
entry, argument ownership, output, LFORMAT, TO, diagnostics, or native parity
claim. The contract records its source-dependent and undefined-behavior gaps.

[`EvalLFormatFormatter`](D:/Koodit/GIT/CopperOS/src/Commands/EvalLFormatFormatter.cs)
adds the bounded `%n`/`%x`/`%o` LFORMAT presentation subset with source-observed
case/literal/no-width behavior and no implicit line feed. Five focused cases
cover formatting and transactional unsupported/short/overlap rejection. `%c`,
TO, argument parsing, diagnostics, and runtime comparison remain open.

The companion `Commands.EvalExpressionNativeRoot` host project builds, but its
first resident HUNK attempt stops at compiler diagnostic `C68K0004`: 64-bit
XOR is unsupported outside addition/subtraction. It emits no artifact and is
recorded as a native compiler boundary, not as Eval native qualification.

## 2026-09-01 - Eval removes the 64-bit XOR lowering stop

`EvalExpressionEvaluator` now computes signed 64-bit XOR and equivalence as
two 32-bit XORs followed by exact signed recombination. Its addition overflow
check uses signed range comparisons, removing the other 64-bit XOR path. The
focused evaluator suite passes 24 cases, including a high-word XOR result; the
full Commands suite passes 539 cases. Resident HUNK compilation now proceeds
past the original diagnostic and stops at the generic borrowed-memory
`IsMapped` interface call, which has no local native binding. No HUNK is
emitted and no native Eval claim changes.

## 2026-09-01 - Eval expression static native reachability

The expression root now compiles resident `memory=None` HUNKs after replacing
the parser's unsupported 64-bit arithmetic with independently written bounded
32-bit lane operations. This includes bitwise operators, signed arithmetic,
literal parsing and shifts while retaining its public `long` interface and
caller-owned memory contract. The compile includes the SDK support assembly so
the constrained `IAmigaGuestMemory` adapter resolves locally.

The generated M68000/M68020/M68040 HUNK SHA-256 values are respectively
`24D6C900C62AA8C2E7C784B61369EE2717720DD663597165C56A6B169336EE36`,
`7BF38E698002F7E70DB52343667B84FC52C180B581964D639A411958A1A075E8`, and
`FD96719373B22215B7F2718B218672302AC68C69C7220C758CE4C1F9ABD52FBB`.
Each report has 53 reachable methods, zero managed-allocation/fatal-machine-
fault/runtime-helper/runtime-feature/external-target sites. The focused host
suite now has 36 evaluator cases and the full Commands suite has 551 passing
cases. No generated instruction fixture, original command run, DOS entry, or
full Eval qualification is claimed.

## 2026-09-01 - WB31 Execute `.DEF` default capture

The hash-bound disposable [default fixture](reference-captures/execute-wb31-def-default.json)
records an original `C:Execute` run whose script declares `.KEY filename` and
`.DEF filename fallback`. With no supplied script argument it writes
`fallback`, then returns to the caller that writes `END`. This confirms one
classic whitespace-default substitution case and supports the bounded Shell
implementation; precedence, equals form, errors, result/IoErr, streams,
temporary files and MorphOS parity remain open.

## 2026-09-01 - Execute accepts observed `.DEF` no-op forms

The resident default table now follows the two subsequent WB31 captures:
a duplicate `.DEF` for an existing `.KEY` name succeeds without replacing its
first value, and a `.DEF` for an undeclared name succeeds without retaining a
record. Focused runner cases verify first-value expansion with a non-keyword
Echo payload and that an undeclared default leaves a later ordinary line
executable. The full Commands suite passes 552 tests. This does not infer
whitespace-empty/malformed behavior, diagnostics, IoErr, nested execution,
native execution, or MorphOS parity.

## 2026-09-04 - CC13 Clone and Trashcan evidence boundary

Clone and Trashcan now have separate CC13 contracts. Each binds the observed
MorphOS 3.20 packed-member identity and version tag without treating that
identity as an executable behavior specification. Clone's installer P addition
requires a pure replacement design; Trashcan's absence from that list leaves its
purity unresolved. Neither has a member in the inspected 3.20 C source archive.
The contracts therefore keep grammar, semantics, runtime behavior, packaging,
and reference comparisons open, and prohibit treating Clone as Copy or Trashcan
as Delete.

## 2026-09-04 - MorphOS Copy mode-selection gate

`NativeMorphOSCopyModeSelection` now models the source's five selected modes,
compatibility FORCE mapping, and invalid-combination gate after positional TO
normalization. Two focused tests cover each single mode and rejection of mode
conflicts, SOFTLINK+ALL, required-target failures, implicit MAKEDIR input, and
DIRECT restrictions. The full Commands suite passes 623 tests.

`qualify_copy_mode_selection_native.ps1` also rebuilds the private 12-byte
mode-control adapter as resident HUNKs for 68000/020/040 and executes fourteen
supplied controls per CPU, including all modes, FORCE mapping, DIRECT, invalid
combinations, and interleaved callers. The two-reachable-method receipt has no
managed runtime features/helpers, external native targets, exception regions,
fatal fault sites, resource leaks, or shared-image writes. Hashes are 68000
`24dc4f7936456c9446a166a4b8fc015db58d379463b96f77ea9167e33cf02d1c`
(1,084 bytes), and 68020/68040
`db6fbd163ee8d20b3f7ea3ef6071c59c65bb98d0c42dfddb3e873c452edd761a`
(1,052 bytes). This is still decision logic only: ReadArgs and positional TO
handling, matcher/traversal, metadata, handlers, packaging, and reference
parity remain open.

**Next CC13.Copy slice:** implement the MorphOS `ReadArgs` boundary with its
exact 24-slot template and invocation-owned `RDArgs`/result storage, including
the source-order positional `TO` normalization needed by the mode gate. Keep
matcher traversal and all filesystem effects outside that bounded parser slice.

## 2026-09-04 - MorphOS Copy ReadArgs and mode gate

`NativeMorphOSCopyArgumentGate` now calls the exact 24-slot source template
through invocation-owned `NativeCommandArguments`, applies default-Copy
positional `TO` adjustment, and then invokes the mode gate. Parser result and
RDArgs ownership are released before the DOS lease closes; source-observed
mode, overwrite, and DIRECT rejections preserve `ERROR_TOO_MANY_ARGS`. Its
private entry is not the external Copy command.

`qualify_copy_argument_gate_native.ps1` rebuilds this 14-method resident HUNK
on 68000/020/040 and executes thirteen supplied-DOS vectors per CPU, including
positional target adjustment, valid and rejected DIRECT cases, parser failure,
conflicts, and interleaved callers. It has no managed runtime features/helpers,
external targets, exception regions, fatal fault sites, resource leaks, or
shared-image writes. Hashes are 68000
`3b663c2e16b4a72dd2eb08c5ef6c1b68dfd588f9c3d763def9a5d237667aa51d`
(5,528 bytes), and 68020/68040
`499f6f588ecefb20f0f1046655d4030cdd40f3c2086a477464dfd5246956b36b`
(5,488 bytes). The supplied parser results do not establish real parser,
matcher, filesystem, metadata, output, packaging, or reference parity.

**Next CC13.Copy slice:** add a caller-owned public-DOS source/destination
operation boundary that opens and closes exactly one regular source/destination
pair around the existing stream loop, keeping recursive matcher and metadata
policy out of that slice.

## 2026-09-04 - MorphOS Copy regular-file pair

`NativeMorphOSCopyFilePair` now opens one regular source before its destination,
runs the existing exact-transfer loop, closes destination before source, removes
the partial destination after a failed transfer, and restores the first open or
transfer `IoErr` after cleanup. The private entry is not the external Copy
command and deliberately excludes matcher traversal, destination creation, and
metadata.

`qualify_copy_file_pair_native.ps1` passes nine supplied vectors on each
68000/020/040 target for success, source/destination open failures, read/short
write failures, Ctrl-C, invalid buffer, and interleaved callers. The seven
reachable methods have no managed runtime features/helpers, external targets,
exception regions, fatal fault sites, leaks, or shared-image writes. Hashes:
68000 `8dc0bba65c05dbc6600529a69383b806f054b14515bd241ac6b24d1da88f07df`
(1,692 bytes), 68020
`c8781ff5b4bde72c0c5458befeff6f871ff298ed7a7258efcf45df4fecd6b2d0`
(1,716 bytes), 68040
`7ff151ac7a655fcb57ec5f0128f625f621b0204ae96f51cc36a2294da7cf7ba1`
(1,692 bytes).

**Next CC13.Copy slice:** implement regular destination preparation and cleanup
for the pair, including the source-observed failed-copy partial-destination
deletion policy, before admitting matcher or recursive behavior.

## 2026-09-04 - MorphOS Copy non-force destination policy

`NativeMorphOSCopyDestination` now independently follows the bounded
`TestDest` order: target lock and FIB examine, unlock before mutation, preserve
an existing required directory, reject DONTOVERWRITE with
`ERROR_OBJECT_EXISTS`, delete an ordinary existing target, and for
FORCEOVERWRITE call `SetProtection(name, 0)` immediately before deleting even
when that protection operation fails.
`qualify_copy_destination_native.ps1` has a five-reachable-method resident
receipt with no managed runtime features/helpers, external targets, exception
regions, fatal fault sites, resource leaks, or shared-image writes:
68000 `abf3cd0b746378f5275a510bc0d5a57b5b5b9cd1f77444d62433dc90b468b5c4`
(1,464 bytes), 68020/68040
`209f2c237dc3c4953cf20d070963156a27b5087da0547c1cd9988f479301450a`
(1,456 bytes). Eleven supplied-DOS vectors per CPU now also cover absent target,
FIB/examine failures, directory preservation, DONTOVERWRITE, ordinary overwrite,
FORCEOVERWRITE including failed protection clearing, delete failure, and
interleaved callers.

**Next CC13.Copy slice:** inspect and isolate MorphOS `OpenDestDir` parent
directory creation, then implement and qualify only its creation/retry path
with supplied-DOS vectors. Keep matcher traversal, metadata, and the command
front end outside that slice.

## 2026-09-04 - MorphOS Copy parent-prefix static stage

`NativeMorphOSCopyDestinationDirectories` now independently implements the
filesystem-only `OpenDestDir` prefix mutation sequence: temporary slash
termination, `TestDest` classification, CreateDir for missing/replaced
prefixes, creation-lock release, restoration, and a final caller-owned shared
lock. It uses raw `CreateDirRaw` specifically to retain pure/resident code; the
Amiga SDK now exposes that raw BPTR return alongside its nullable convenience
API. Its six-method static resident HUNK receipt has no managed runtime
features/helpers, external targets, exception regions, or fatal fault sites:
68000 `ff1cf97b57bc19f8af4e1390abd51d62342d063db4dad8c0dc07561a68aa9c47`
(1,948 bytes), 68020
`58919d26d6f32f6f15b87245c644601c119f696d273682aab8a37eb2ec7c5d2f`
(1,968 bytes), and 68040
`9b1be81dc616388226522f304ff6b93fda8db68bf7c44fe1792c051e842f6e62`
(1,944 bytes). `qualify_copy_destination_directories_native.ps1` now passes
seven supplied public-DOS vectors per CPU for retained, missing, replaced,
CreateDir-failure, final-lock-failure, and interleaved cases; it has no leaks or
shared-image writes.

**Next CC13.Copy slice:** isolate the source's non-filesystem destination
fallback (`TestFileSys` false) and its empty-name lock ownership, then qualify
only that path with supplied-DOS vectors. Keep diagnostics, MAKEDIR's special
result, matcher traversal, metadata, and the command front end outside it.

## 2026-09-04 - MorphOS Copy non-filesystem destination stage

`NativeMorphOSCopyNonFileSystemDestination` now implements the source-observed
colon-temporary `IsFileSystem` query, caller-owned destination-name copy, and
empty-name shared-lock fallback for Copy/MOVE. Its five-method resident
receipt is clean on 68000/020/040; hashes are recorded in the Copy contract.
`qualify_copy_nonfilesystem_native.ps1` passes eight supplied public-DOS
vectors per CPU for filesystem/non-filesystem classification, gating, capacity
and empty-lock failures, no-colon input, and interleaving, with no leaks or
shared-image writes.

## 2026-09-04 - MorphOS Copy metadata stage

`NativeMorphOSCopyMetadata` now independently applies Copy's bounded `SetData`
metadata behavior through public DOS calls. The direct-control resident entry
passes eight supplied vectors on each 68000/020/040 HUNK for NOPRO precedence,
normal ARCHIVE removal, PROX masking, comment propagation, classic and POSIX
date selection, no-op flags, and interleaved callers. The receipt has six
reachable methods with no managed runtime features/helpers, external native
targets, exception regions, fatal fault sites, fixture resource leaks, or
shared-image writes. The comment call uses the explicit documented
`fib_Comment` offset, which the native ABI probe verifies. This does not
integrate metadata into Copy's full source/destination lifecycle.

## 2026-09-04 - MorphOS Copy joined destination-open static stage

`NativeMorphOSCopyOpenDestination` joins the source's non-filesystem
`OpenDestDir` fallback to its normal filesystem prefix path, retaining the
caller-owned lock and propagating the selected branch's `IoErr`. Its eight-method
private root passes five supplied public-DOS vectors on 68000/020/040 for both
branch selections, creation failure, and interleaving, with no fixture leaks or
shared-image writes. The Copy contract records the hashes. Full Copy lifecycle
behavior remains open.

## 2026-09-04 - MorphOS Copy final result policy stage

`NativeMorphOSCopyResultPolicy` now implements the source-observed post-cleanup
order for Ctrl-C, `ERROR_BREAK`, `PrintFault`, primary `RetVal` precedence, and
ERRWARN warning promotion. Its five-method resident entry passes nine supplied
public-DOS vectors on each 68000/020/040 HUNK, including QUIET fault suppression
and interleaved callers. There are no managed runtime features/helpers,
external native targets, exception regions, fatal fault sites, fixture leaks,
or shared-image writes. This policy remains unattached to Copy's complete
parser, matcher, traversal, and handler lifecycle.

## 2026-09-04 - MorphOS Copy loop-guard stage

`NativeMorphOSCopyLoopGuard` now independently implements the `TestLoop`
SameDevice/SameLock/ParentDir walk with source-matching parent-lock ownership.
It has a clean six-method resident receipt on 68000/020/040; hashes are
recorded in the Copy contract. The private direct-control entry passes six
supplied public-DOS vectors per CPU for different devices, recursive target,
ancestor detection, root exhaustion, exact parent-lock cleanup, and interleaved
callers, with no fixture leaks or shared-image writes. This does not yet join
the loop guard to Copy's complete traversal and diagnostics lifecycle.

## 2026-09-04 - MorphOS Delete final-object stage

`NativeMorphOSDeleteObject` now isolates Delete's final public `DeleteFile`
action for an already-selected, deletable object. The private direct-control
root is not the Delete command entry. Its three-CPU resident receipt passes six
supplied vectors per CPU for success, handler failure, null rejection, and
interleaved callers, with five reachable methods and no managed runtime
features/helpers, external native targets, exception regions, fatal machine
fault sites, resource leaks, or shared-image writes. Parser, matcher,
protection/FORCE and parent retry, diagnostics, recursion, packaging, real
handler behavior, and reference parity remain open.

The Amiga SDK now has raw BPTR imports for `CurrentDir` and `ParentDir`, matching
the existing raw `Lock` approach. They remove nullable-value runtime pressure
for a future pure parent-protection retry stage; they do not implement it.

## 2026-09-04 - MorphOS Copy streaming core

The hash-bound [Copy contract](contracts/Copy.md) now has an independent
`NativeMorphOSCopyLoop` implementation of the source-observed normal-path
stream loop. Its caller supplies both DOS handles and its guest buffer. The
core polls Ctrl-C before each read, performs a Write even after the zero-length
EOF read, rejects short writes, and preserves the immediate DOS error for failed
transfers or Ctrl-C. It owns no parser state, buffer, stream, lock, matcher,
metadata, or traversal state.

The private direct-control root is not a user-facing Copy entry. The refreshed
three-CPU resident receipt passes eight supplied direct stream vectors per CPU,
covering EOF, chunks, read and write failures, Ctrl-C, and interleaved callers.
It has six reachable methods and no managed runtime features/helpers, external
native targets, exception regions, fatal machine fault sites, resource leaks,
or shared-image writes. The artifact hashes and remaining command-level gates
are recorded in the contract; real handler behavior, parser/mode integration,
packaging, and reference parity remain open.

## 2026-09-04 - MorphOS Join append-stream stage

Join's hash-bound source shares Copy's exact low-level transfer semantics while
retaining distinct command ownership for source opening, a 262144-byte buffer,
diagnostics, matching, and deletion of an incomplete destination. The new
`NativeMorphOSJoinAppendLoop` preserves only that bounded append stage over
caller-owned handles and guest buffer. Its private direct-control root is not a
Join command entry.

The new three-CPU resident receipt passes eight supplied vectors per CPU for
EOF, chunks, read and write failures, Ctrl-C, and interleaved callers. It has
seven reachable methods and no managed runtime features/helpers, external native
targets, exception regions, fatal machine fault sites, resource leaks, or
shared-image writes. Parser/matcher integration, opening and cleanup, user
output, destination handling, packaging, real handlers, and reference parity
remain open.

## 2026-09-01 - Execute keeps the first surplus whitespace `.DEF` value

The original Workbench 3.1 [surplus-whitespace fixture](reference-captures/execute-wb31-def-extra-value-expansion.json)
shows that `.DEF filename first second` is accepted and expands `<filename>` as
`first`; it returns `RC=0`, `Result2=0`. The resident parser now matches this
bounded spelling by retaining its first whitespace value and ignoring later
tokens. The focused script-engine suite passes 33 cases and the full Commands
suite passes 554. Equals-form surplus
tokens, diagnostics, IoErr, nested execution, native execution, and MorphOS
parity remain open.

## 2026-09-01 - Execute keeps the first surplus equals `.DEF` value

The original Workbench 3.1 [surplus-equals fixture](reference-captures/execute-wb31-def-equals-extra-value.json)
shows that `.DEF filename=first second` is also accepted and expands
`<filename>` as `first`, returning `RC=0`, `Result2=0`. The resident parser no
longer applies a stricter equals-tail rule: it retains the first value after
either documented separator and ignores the remaining tokens. The focused
script-engine suite passes 34 cases and the full Commands suite passes 555. Other malformed directives, diagnostics,
IoErr, nested execution, native execution, and MorphOS parity remain open.

## 2026-09-01 - Execute native export refresh after `.DEF` repairs

The full Execute native-root export rebuilds as an M68000 HUNK and M68020/M68040
assembly after the whitespace and equals `.DEF` tail repairs. Its generated
SHA-256 values are respectively
`294DE49A99832466C8988FEF23E08DE490E3A379A5B0E56EAC9D686529E97D1E`,
`E2FA3B89B4B401C6FE73994905130EF1FBD21F5EF43672542642E110A0D09704`, and
`C970A7ACE88C8DA6DA9CA48E72A8F5D1F039CC1BC84A8AC8CD2076BFC4ACB694`.
Each static compatibility report is compatible with 2,229 reachable methods,
zero managed allocations, runtime helpers, runtime features and external
native targets. Each also retains three fatal machine-fault sites. This is a
static compilation checkpoint only; it does not qualify pure/resident runtime
behavior, Execute lifecycle, or native execution.

## 2026-09-01 - Execute treats a bare `.DEF` as a no-op

The original Workbench 3.1 [bare-DEF fixture](reference-captures/execute-wb31-def-missing-name.json)
shows that `.DEF` with no name or value returns `RC=0`, `Result2=0` and lets a
following ordinary line run. The resident dispatcher now advances that exact
no-name/no-value form without adding a default. The focused script-engine suite
passes 35 cases and the full Commands suite passes 556. Named malformed forms, diagnostics, IoErr, nested execution,
native execution, and MorphOS parity remain open.

## 2026-09-01 - Execute rejects an empty `.DEF` name

The original Workbench 3.1 [empty-name fixture](reference-captures/execute-wb31-def-empty-name.json)
records `.DEF =fallback` returning `RC=10`, `Result2=0` without running its
following ordinary line. The runner already stops with its error result for
that malformed form; the new regression binds this control-flow behavior. The
focused script-engine suite passes 36 cases and the full Commands suite passes
557. The refreshed native export includes the bare-DEF repair on 68000/020/040
and remains statically compatible with 2,229 reachable methods and zero
managed allocations/helpers/features/external targets, but three fatal
machine-fault sites. Diagnostic text, other named malformed forms, nested
execution, native execution, and MorphOS parity remain open.

## 2026-09-01 - Eval bounded external entry core

`EvalCommand` now introduces a caller-workspace external entry core for the
exact Workbench 3.1 and MorphOS 3.20 ReadArgs templates. It assembles the
documented operand sequence, routes it through the bounded expression evaluator
and formatter components, writes a borrowed output or owned `TO` output, and
releases RDArgs on every return. Six host integration cases verify the two
exact templates (including the 36-byte classic workspace), decimal output,
operand and `/M` reconstruction, HEX and LFORMAT output, `TO` open/close
behavior, and the ReadArgs/FreeArgs pair. This is not a full template parser fixture, native entrypoint,
diagnostic/TO failure claim, original-command comparison, or pure/resident
qualification. The full Commands suite passes 566 cases. Exact-capacity LFORMAT
tests now cover signed decimal, hexadecimal, and octal 64-bit boundary values.

The separate native command project now has an invocation-local `NativeEvalCommand`
and private `NativeEvalEntry` root using the real native `ReadArgs` lease and DOS
startup owner. The managed native project and root build. After lane-lowering
LFORMAT sizing, resident HUNK output succeeds for 68000, 68020 and 68040. Each
static report reaches 81 methods with no allocation, fatal machine-fault site,
helper, or external target, but retains the compiler's `nullable-values` feature
through the SDK `DOS.Open` TO result.

The matching private Copper68k fixture executes 14 supplied-vector native
invocations for each CPU from one loaded image. It covers decimal, operand-vector,
HEX, LFORMAT, TO, malformed-expression, parser/allocation failures, repeats and
same-image interleavings, with zero shared-image writes and balanced owned
resources. Its DOS vectors supply already-decoded slots, so it does not prove
actual DOS parsing, Kickstart/CopperStart/MorphOS behavior, original-Eval parity,
or final pure/resident approval.

`tools/Commands/qualify_eval_native_entry.ps1` now rebuilds the resident HUNK
matrix, rejects any static feature set other than the audited `nullable-values`,
runs all 42 supplied-vector invocations, and writes a hash-bound receipt.

## 2026-09-01 - WB31 Eval first native output capture

The original Workbench 3.1 `C:Eval` ran a disposable 68000 WinUAE probe whose
hash-bound [`eval-wb31-basic.json`](reference-captures/eval-wb31-basic.json)
captures three successful calls. `1+2*3` wrote `9`; `1 + 2` wrote `3`; and
`42 LFORMAT="x=%x"` wrote `x=A` without a final LF, so the caller's `END`
marker joined it as `x=AEND`. This establishes those exact output bytes only.
It invalidates any assumption that classic Eval can share the currently
source-observed MorphOS precedence or LFORMAT behavior without more captures.
Grammar boundaries, numeric representation, all other formatting, status,
IoErr, diagnostics, `TO`, ReadArgs failures, pure/resident behavior, and all
MorphOS runtime evidence remain open.

## 2026-09-01 - WB31 Eval classic subset separation

A second original-binary [classic grammar fixture](reference-captures/eval-wb31-classic-grammar.json)
records `1+2*3+4` as `13` and distinguishes the classic X formatter: bare
`%x` renders one low uppercase hex digit, while `%x2` and `%X2` render two.
`EvalCommand` now selects a separate Workbench 3.1 candidate for this observed
arithmetic subset and `EvalClassicLFormatFormatter` for the observed X/x
formatting subset; its three focused command integrations pass inside the full
569-test Commands suite. The implementation accepts only measured arithmetic
operators and parenthesized numeric operands, and the formatter accepts only
measured X/x conversions and widths 1 through 8. It makes no claim about the
remaining classic grammar or LFORMAT conversions.

The existing private native root remains MorphOS-profile only. Its unchanged
resident 68000/020/040 supplied-vector qualification still passes 42
invocations with zero shared-image writes; it does not execute the new classic
candidate or prove a classic native command entry.

## 2026-09-01 - WB31 Eval numeric and grouping subset

The original-binary [numeric fixture](reference-captures/eval-wb31-classic-numeric.json)
establishes selected classic grouping, unary, literal and numeric-format cases:
`20-5*2` returns `30`, `1+(2*3)` returns `7`, `-2+3` returns `1`, both `0x10`
and `#x10` return `16`, and leading-zero `010` returns `8`. It also records
lowercase `%n` decimal and the one/two-low-digit lowercase `%o` forms. The separately selected
classic candidate now carries exactly those parser and formatter paths, and the
full Commands suite passes 577 tests. The refreshed private MorphOS native
matrix still passes 42 supplied-vector invocations on 68000/020/040 with zero
shared-image writes; classic native execution remains absent.

## 2026-09-01 - WB31 Eval remaining operator subset

The original-binary [operator fixture](reference-captures/eval-wb31-classic-operators.json)
adds `%`/`mod`, bitwise AND/OR/XOR/EQV, LSH/RSH and unary complement examples
to the classic record. `1|2&4` returns `0`, reinforcing the captured
left-to-right stream; `6 eqv 3` returns `-6`. The separate Workbench candidate
now implements exactly those non-caret operations using its invocation-local
evaluator. The fixture also records `2^3` returning `2`, but does not explain
its grammar, so caret remains excluded rather than being mapped to MorphOS
power. The focused command integrations pass within the 586-test Commands
suite; the refreshed private MorphOS native HUNK matrix passes all 42
supplied-vector executions. Classic native execution and all unmeasured
operators, ranges, errors, and output behavior remain open.

## 2026-09-01 - WB31 Eval native literal-template entry

The first attempt to compile the captured Workbench Eval candidate through the
shared enum-profile body selected the MorphOS template in generated code. The
native path now uses `NativeWorkbench31EvalEntry` and a separate literal
Workbench `RunWorkbench31` body, so template selection is an explicit resident
code fact. Its qualification script emits 68000/020/040 HUNKs and executes 12
supplied post-ReadArgs vectors per CPU for captured arithmetic, operand vectors,
X/O LFORMAT, TO, caret rejection, parser/allocation failure, repeats and
interleavings. All 36 runs use one loaded image per CPU with zero image writes
and balanced resources. This remains a synthetic DOS-vector test: the original
caret success (`2^3 → 2`), true parser/output/filesystem behavior, and final
classic P/purity remain open.

## 2026-09-01 - WB31 Eval evaluated-prefix behavior

The original-binary [caret fixture](reference-captures/eval-wb31-classic-caret.json)
shows that Workbench Eval accepts the valid evaluated prefix of selected input:
`2^3`, spaced caret forms, and `2^3+4` output `2`; `2+3junk` outputs `5`.
The classic candidate now keeps a completed expression result when an
unrecognized suffix remains, while an incomplete recognized operator such as
`1+` still fails. It does not assign caret semantics. The separate `**` result
of `0` remains captured but unimplemented. The Commands suite passes 589 tests;
the refreshed classic resident HUNK matrix passes 36 supplied-vector invocations
with zero shared-image writes. Full malformed-input behavior, diagnostics,
return/IoErr and original command parity remain open.

## 2026-09-01 - WB31 Which native-entry vector checkpoint

`Workbench31WhichCommand` now has an explicit-cleanup resident entry fixture.
The reproduced 68000/020/040 HUNK matrix has no managed allocation sites,
exception regions, fatal machine-fault sites, helpers, or external native
targets; `nullable-values` is retained only through the SDK `Lock` result. The
new qualifier executes 12 supplied post-ReadArgs DOS/Exec-vector invocations
per CPU (36 total), covering internal/resident/direct-path output, `ALL`, the
captured `NORES RES` conflict, missing RES, parser/allocation failures, repeats,
and instruction-interleaved calls. Each CPU uses one protected image with zero
shared-image writes. The runner supplies DOS lookup results, so it does not
constitute real Workbench parser, filesystem, resident-list, original-command,
installed-purity, or MorphOS qualification.

## 2026-09-02 - MorphOS Quote pure forward-stage checkpoint

`QuoteRuleParser` no longer depends on managed string literals when it matches
the documented rule names. Its scalar byte matcher preserves the focused parser
cases and lets the private Quote forward root compile on 68000/020/040 without
managed runtime features, exception regions, fatal machine-fault sites,
helpers, or external native targets. The reproducible receipt has 30 reachable
methods and eight generated-code control-block calls per CPU: three ordered
forward pipelines, unknown-rule no-write rejection, repeats, and interleaved
callers share one protected image. It does not provide Quote's missing command
entry, DOS parsing, FILE/VAR source handling, output/status behavior, original
parity, or pure lifecycle qualification.

## 2026-09-02 - MorphOS Quote bounded ReadArgs boundary

`QuoteCommand` now writes the documented `RULE/A,FILE/K,VAR/K,STR,NOLINE/S,
NOQUOTES/S,FIRSTLINE/S,REVERSE=UNQUOTE/S` template into invocation-owned guest
storage and delegates parsing/release to `ReadArgs`/`FreeArgs`. The bounded
host path accepts one STR source or a CLI-local then global VAR source, routes
it through the existing candidate pipeline, and applies NOLINE's final-LF
toggle. FILE, NOQUOTES, and FIRSTLINE return an explicit incomplete error
instead of an invented source precedence or presentation rule. Three focused
host cases cover STR READITEM, STR NOLINE, and local-over-global VAR. This adds
no original runtime evidence and is not a native command entry or pure
qualification.

## 2026-09-02 - MorphOS Quote native STR-entry compilation and vector execution

`NativeMorphOSQuoteCommand` now runs the bounded STR forward path through
native `ReadArgs`, five invocation-owned Exec buffers, and DOS output, while
rejecting the unimplemented FILE/VAR/REVERSE/NOQUOTES/FIRSTLINE modes. Its
private entry root compiles resident 68000/020/040 HUNKs with 41 reachable
methods each and no runtime features, exception regions, managed allocations,
fatal faults, helpers, or external targets. The reproducible supplied-vector
fixture now executes 12 calls per CPU: READITEM, HEX/NOLINE, ordered URI/HEX,
invalid-rule and unsupported-REVERSE errors, injected short-write IoErr capture,
parser/allocation failure cleanup, and repeated/interleaved callers on one
protected image. Its DOS adapters do not provide real parsing or MorphOS shell
behavior; real output, lifecycle, installed purity, and original behavior remain
unqualified.

## 2026-09-02 - MorphOS CLI contract boundary

The CC10 [`CLI contract`](contracts/CLI.md) now records the observed MorphOS
3.20 `MorphOS/C/CLI` identity, its packed-binary version tag, and the installer
line requiring P. It explicitly separates the external command from the
existing Shell `NewCLI`/`NewShell` internals, because no template, window
semantics, launch behavior, or runtime result has been captured. The next work
is controlled syntax/window/launch capture followed by one process-owner entry;
this record does not add a command implementation or claim purity qualification.

The MorphOS Library's current CLI page is retained only as a secondary lead: it
states “No Template” and says the command opens a new window instead of a tab.
It cannot freeze the 3.20 packed binary's parser or authorize reuse of NewCLI's
`WINDOW,FROM` grammar.

`CliCommand` now introduces a separate `ShellLaunchKind.Cli` and delegates its
zero-tail candidate launch to the existing DOS/Shell process owner with inherited
streams and directory. It does not create another parser, CLI registry, or
console owner. Nonempty tails return an explicit incomplete error until capture.
Five focused host cases cover that admission boundary, launch failure, and both
continuation lifecycle edges. This is not a native entry, window implementation,
or original behavior claim.

## 2026-09-02 - CC11 Type profile boundary

The CC11 [`Type` contract](contracts/Type.md) now binds both original media
members without conflating their releases: Workbench 3.1 `C/Type` is the
1,496-byte HUNK `type 37.2`, and MorphOS 3.20 `MorphOS/C/Type` is the 3,985-byte
packed-native `Type 50.6`. The classic binary contains
`FROM/A/M,TO/K,OPT/K,HEX/S,NUMBER/S`, recorded only as a syntax candidate. The
packed 3.20 binary exposes no plaintext template, so the classic candidate is
explicitly not transferred to MorphOS.

MorphOS installer line 81 requires P treatment for Type; Workbench's inspected
file header has P clear but does not settle its runtime classification. The
contract freezes public-DOS/`ReadArgs` ownership requirements and a capture
matrix for grammar, file/pattern traversal, rendering, TO/error ownership,
break, and same-SegList lifetime. It adds no Type parser, native artifact, or
runtime-parity result.

The exact 214,038-byte official 3.20 source archive was then inspected without
copying its source. Its hash-bound `c/type/type.c` declares
`FROM/A/M,TO/K,OPT/K,HEX/S,NUMBER/S,NOLINE/S`, uses `ReadArgs`/`FreeArgs`, and
observes `OPT` H/N compatibility, HEX+NUMBER rejection, `TO`/`Output()` choice,
and `MatchFirst` enumeration. The visible 50.6 source date matches the packed
member tag but does not bind the source to that binary. Its notices contain no
established reuse grant, so the findings advance only the MorphOS spec/options
evidence and preserve independent binary/runtime captures as the next gate.

The current Amiga SDK is also verified to expose the public DOS enumeration and
I/O calls required by Type, including `MatchFirst`/`MatchNext`/`MatchEnd` and
the standard `AnchorPath` layout. That layout intentionally omits the source's
MorphOS-only `APEF_LiteralSLinks` extension. The implementation may not write
an inferred extension into classic memory; a versioned provider capability and
literal-soft-link captures are now an explicit 50.6 slice.

## 2026-09-02 - MorphOS Type text-rendering primitive

`TypeTextFormatter` independently implements the source-observed 50.6 text
slice: NUMBER writes the five-column decimal prefix and trailing space before
each logical line; NOLINE suppresses only the final added LF. It preflights
capacity, mapping, and overlap before any write. Ten focused cases cover empty,
terminated/unterminated, numbered, and atomic-failure input; the complete
Commands suite passes 607 tests. This is a pure byte formatter, not a command
entry or source-to-packed-binary/runtime-equivalence claim. DOS traversal,
TO/stream ownership, HEX, breaks, IoErr, native command entry, and P lifecycle
remain open.

`Commands.TypeNativeRoot` now gives the formatter a caller-owned native probe.
The resident 68000/020/040 HUNKs are 2,076/1,944/1,876 bytes and each has ten
reachable methods with zero managed allocations, runtime features, helpers,
external targets, exception regions, and fatal fault sites. During compilation,
the native compiler rejected a `ushort` parameter update; the formatter now
uses a local unsigned counter, preserving host output and enabling all three
native artifacts. The probe now executes ten direct control-block cases on each
CPU under Copper68k: empty/unterminated, NOLINE, numbered, capacity rejection,
and repeated/interleaved calls. Each run requires zero DOS/Exec calls, zero
resident allocations/leaks, and zero shared-image writes. It is component
execution only; no DOS command or original-result claim is made.

`qualify_type_text_native.ps1` now rebuilds those three HUNKs and emits
hash-bound static and Copper68k execution receipts. Its fresh run passed with
ten native invocations per CPU; it deliberately does not claim a DOS command
runtime matrix.

The shared `CopperOS.Commands.Native` project now links the formatter and its
existing Quote parser/pipeline dependencies, so it builds successfully rather
than relying on the private roots alone. The complete Commands suite remains at
607 passing tests. This restores shared-native build coverage; it does not
close Type's runtime or command-entry gates.

`NativeMorphOSTypeTextIo` now supplies the corresponding unbounded public-DOS
text transfer stage for an already-opened input/output pair. It preserves
NUMBER state across Read chunks, flushes logical lines, completes positive
short Writes, captures Read/Write IoErr, and clears/observes Ctrl-C alone. Its
private resident root passes six supplied-DOS-vector calls each on 68000/020/040
for chunking, short writes, NOLINE, empty NUMBER, Ctrl-C, and shared-image
repeat/interleaving. The hash-bound
`qualify_type_text_io_native.ps1` receipt records no leaked guest storage or
image writes. It owns no handles or buffers and is not yet a command entry;
parser, traversal, HEX, TO, and reference/runtime integration remain open.

`NativeMorphOSTypeCommand` now provides the next text/HEX frontend slice: the
source-observed MorphOS ReadArgs template, legacy OPT H/N mapping, `/M` input
vector, public MatchFirst/Next/End loop, borrowed/default or TO-owned output,
and invocation-owned anchor/read/write buffers. It renders independently
specified uppercase HEX rows, including the printable-byte column and final
partial-row blank line, and cannot request the unpublished literal-soft-link
AnchorPath extension. The native project builds,
and a standalone entry now compiles as pure resident HUNKs on 68000/020/040:
24 reachable methods each with zero managed runtime features/helpers, external
targets, exception regions, and fatal fault sites. The shared SDK gained
`DOS.OpenRaw`, a raw BPTR form of the public Open vector, so Type no longer
pulls the nullable-values runtime feature into a required-P image. The
hash-bound `qualify_type_native_entry.ps1` receipt now compiles those HUNKs
and executes twelve supplied DOS parser/matcher vectors per CPU: ordinary and
NUMBER text, a two-member `/M` vector, NOLINE with TO, no-match cleanup,
partial-row HEX with short writes, source-observed ignored-OPT, HEX+NUMBER,
no-match, input-open, and TO-open-failure diagnostics, and repeat/interleaved calls. It
checks the template/result ABI, matcher sequence, stream ownership, workspace
and RDArgs release, selected IoErr publication,
and one protected image. The fixture is explicitly not a real DOS parser,
wildcard/filesystem test, packaged command, or original comparison; those
gates remain open.

## 2026-09-04 - MorphOS Type HEX interruption qualification

The Type entry fixture now includes a 240-byte HEX input interrupted at the
source-observed sixteenth render-cycle poll. It emits the preceding fifteen
complete rows, closes the input, publishes `ERROR_BREAK`, and does not advance
the matcher after the interrupted copy. This exposed and corrected an error
propagation defect: the command had carried the stream return level instead of
the stream `IoErr` into its diagnostic path. The refreshed 24-method
pure/resident HUNK receipt passes thirteen supplied vectors per CPU: 68000 is
8,204 bytes, SHA-256
`baca571885feea152e675bbcbf560d7198911d1c2ce2498e8c56b38cc5bc8e2d`;
68020 is 8,256 bytes, SHA-256
`ed2cdd1eac159d20a35ce1bb2113b7e61059d4e91b68e73c9c657397e58938b5`;
and 68040 is 8,068 bytes, SHA-256
`13306b7553266c17f1bce70c4d6d26189578f22ebfdbb3abd2e661f9a18ed133`.
The qualifier remains bounded: it supplies parser, matcher, streams, and
interrupt signals, and does not establish real-DOS, packaged-command, or
reference-binary equivalence.

## 2026-09-04 - MorphOS Search literal boundary

The Search contract now hashes its inspected 3.20 release-source member and
records the source-observed public-DOS traversal, file/pattern, output,
locale, return-level, and break candidates without claiming packed-binary or
runtime parity. `SearchLiteralMatcher` adds an independent four-case pure
primitive over caller-owned guest byte ranges: exact CASE matching and an
ASCII-only no-CASE fallback, valid nonmatches without mutation, and invalid
range rejection. It has no allocation, state, DOS handles, or locale claim.
The command test project passes 616 tests. Locale-library folding, DOS pattern
calls, line scanning, recursion, output, lifecycle, and all reference gates
remain open.

`qualify_search_literal_native.ps1` now independently rebuilds that primitive,
emits resident HUNKs for 68000/020/040, and executes nine direct control-block
vectors per CPU. Each has seven reachable methods and zero managed runtime
features/helpers, external targets, exception regions, fatal fault sites, DOS
calls, allocation, or shared-image writes. The refreshed hashes are 68000
`ebcc092a41db61d8bff6af53f4363e44ed72287d2a6236617a7a954c4a4bced8`
(1,060 bytes), 68020
`2646773899a00e82a409422f620c1bbdd81e06a50a8a78b15086a15ba07c8c6a`
(1,084 bytes), and 68040
`4135553b44c5d407394ebb73b7d05ece95ef272f1f9400d76b2a125b7011899f`
(1,052 bytes). This remains a literal-transform probe: it does not enter
Search's DOS parser, locale, traversal, I/O, or reference behavior.

The next Search slice is `SearchLineFormatter`, an independent caller-buffer
transform for literal matching over source-observed logical lines. Five host
vectors cover numbered matches, following lines, NONUM, QUIET, control-byte
splitting, ASCII sanitation, and atomic capacity rejection; the command suite
now passes 621 tests. The first resident static compile found variable decimal
division fault sites, so formatting was rewritten over fixed decimal powers.
The current 68000 static root has 19 reachable methods and zero managed runtime
features/helpers, external targets, exception regions, and fatal fault sites.
`qualify_search_line_native.ps1` now closes that direct native gap: 19-method
resident HUNKs execute ten supplied line vectors per CPU with no managed
runtime features/helpers, external targets, exception regions, fault sites,
DOS calls, allocation, or shared-image writes. The hash-bound artifacts are
68000 `60dc3cc0a53f36d8bc315cf42e5d09b5cf93cfb532670c968de12356147eeab0`
(4,284 bytes), 68020
`17dd86fd4bd26d9694735ea13cd073d4c7e20b9713567757e5456b672f1682ab`
(4,368 bytes), and 68040
`8609c7d259827547d1c032816d163866ca5b86a1fa06fa477389f1e4ae84d606`
(4,264 bytes). Locale/pattern, DOS traversal and I/O, lifecycle, and reference
behavior remain distinct open Search gates.

The CC11 source inventory now also hash-binds MorphOS `Info` (27,153 bytes,
`ae11f8ab679916df93ff4306c9cd95beca5726483103c9292f13a4abf6f2945f`) and
`DOSList` (21,044 bytes,
`0ea42d62af32eb6dd455aa28ace38ebf68370b64bca2c519776b8e3f2b976683`).
Their contracts record public-DOS parser, list-lock, traversal, break, and
cleanup obligations from source inspection only; no implementation, packed
binary correspondence, or source-reuse conclusion follows from that evidence.

`NativeDosListTraversal` begins the common Info/DOSList runtime foundation. It
owns an optional `AttemptLockDosList` read lock, iterates `NextDosEntry`, polls
Ctrl-C, sets `ERROR_BREAK`, and performs the exact matching unlock before
return. Its private 68000 root is a 572-byte, two-method resident HUNK with no
managed runtime feature, external target, or fatal fault site. Supplied
`qualify_doslist_traversal_native.ps1` now qualifies that foundation on
68000/020/040. Its two-method resident HUNKs pass six supplied vectors per CPU
for refusal, empty/populated lists, Ctrl-C, and interleaved callers, with no
managed runtime features/helpers, external targets, exception regions, fault
sites, or shared-image writes. Hashes: 68000
`5cefd6abe9af9e877670fcca00e17adcc4eb3e01985492f4ebbca23e436cfa58`
(580 bytes), 68020
`9d883441c9cbba90f6d24960eccf08f1295adec2e719775164b69c05bc3e5ab2`
(588 bytes), and 68040
`1360d1ee46b3196eb2cfed4dd1e489e01abec60fdfe3be830a54a3ff5ef8d867`
(580 bytes). Parser, rendering, command entry, and reference behavior remain
open.


## 2026-09-04 - MorphOS Copy pattern-classifier stage

`NativeMorphOSCopyPatternClassifier` now isolates the release source's
`IsMatchPattern` lifecycle with caller-owned AnchorPath storage: it prepares
the `APF_DOWILD` probe, distinguishes wildcard from literal success, and calls
`MatchEnd` only after MatchFirst succeeds. Its private direct-control root
passes five supplied public-DOS vectors per 68000/020/040 HUNK for wildcard,
literal, MatchFirst failure without MatchEnd, and interleaved callers. The
five-method pure/resident receipt has no runtime features/helpers, external
native targets, exception regions, fatal fault sites, fixture resource leaks,
or shared-image writes. This is a classifier only; `PatCopy` iteration and
DoWork remain open.

## 2026-09-04 - MorphOS Copy flat PatCopy stage

`NativeMorphOSCopyFlatTraversal` now carries a bounded regular-file matcher
pass through `MatchFirst`, `MatchNext`, and `MatchEnd`. It snapshots the
source-sized path and FIB into caller-owned work records, and exposes each
record only at the source's deferred DoWork boundary. Its private root passes
six supplied public-DOS vectors per 68000/020/040 HUNK for one/two files,
initial and advancing matcher failures, and interleaved callers. Directory
entry/exit, parent-lock ownership, recursion, and DoWork itself remain open.

## 2026-09-04 - MorphOS Copy directory-entry static checkpoint

`NativeMorphOSCopyDirectoryEntry` now captures the source's first-directory
and ordinary-directory transitions. Its three resident HUNK builds are pure by
static receipt (five reachable methods and no runtime helpers/features,
external targets, exception regions, or fatal fault sites). No runtime vector
fixture exists yet, so this checkpoint does not close the directory-entry
slice or grant Copy any wider traversal credit.

## 2026-09-01 - Execute accepts whitespace-empty defaults

The hash-bound [whitespace-empty fixture](reference-captures/execute-wb31-def-whitespace-empty.json)
records the original Workbench 3.1 command with `.KEY filename` followed by
`.DEF filename ` (a trailing whitespace separator with no value). It suppresses
the following line that expands `<filename>`, runs `AFTER`, and returns
`RC=0`, `Result2=0`. The existing zero-length default path already produces
that result; a focused runner regression now locks down this second observed
spelling. The focused script-engine tests pass 32 cases and the full Commands
suite passes 553. This does not establish malformed directives, diagnostics, IoErr, nested execution,
native execution, or MorphOS parity.

### 2026-09-05 — Copy fidelity corrections

Corrected the non-filesystem fallback's NULL Lock argument to a real empty string and made standalone/integrated fixtures reject NULL. Removed the flat traversal fixture's AnchorPath/output overlap and added long paths. Native resident qualification passed on 68000/68020/68040: 8 non-filesystem, 5 OpenDest, and 7 snapshot invocations per CPU, without shared-image writes. Corrected receipt locations and limits are recorded in contracts/Copy.md. Streaming DoWork integration and full command delivery remain open.


### 2026-09-05 — Copy directory-entry runtime validation

Added an explicit caller-supplied entry permission to preserve the original dangling-soft-link recursion decision. Qualified first-directory, ordinary, ALL, denied-entry, flag preservation, and interleaved cases through the native root: 7 invocations on each of 68000/68020/68040, no shared-image writes. Receipt: artifacts/qualification-copy-directory-entry-native/qualification.json. The adapter does not perform the soft-link check itself; that and streaming PatCopy/DoWork integration remain required.


### 2026-09-05 — Copy soft-link probe implementation

Implemented public DOS soft-link detection, original warning/QUIET behavior, temporary resource cleanup, saved error restoration and current-directory restoration. Root builds without warnings and compiles resident on all three CPUs; static reports at artifacts/copy-softlink-static. Runtime qualification is pending and remains the next required step before traversal integration.


### 2026-09-05 — Copy soft-link native runtime

Qualified the soft-link probe with 9 invocations per CPU, all passing on 68000/68020/68040. Covers lock success, error discrimination, device and allocation failure, ReadLink result, warning/QUIET arguments, restored directory/error, cleanup and interleaved callers. Receipt: artifacts/qualification-copy-softlink-native/qualification.json. Full traversal integration and original-system parity remain open.


### 2026-09-05 — Copy matcher directory/probe connection

Connected live AnchorPath/AChain/FIB state to the soft-link probe and directory-entry transition. Integrated native vectors passed 9 invocations on each CPU, checking that denied descent retains object work and suppresses DODIR. Receipt: artifacts/qualification-copy-matched-directory-native/qualification.json. Full matcher loop, exit integration and DoWork remain required.


### 2026-09-05 — Copy directory-exit metadata integration

Connected directory exit to ENTERSECOND, destination path-cache reset, parent-directory metadata and CurrentDir restoration. Six integrated vectors pass per CPU, including attribute failure, ParentDir failure and interleaved ownership. Receipt: artifacts/qualification-copy-directory-exit-integrated/qualification.json. Full matcher iteration and DoWork remain open.


### 2026-09-05 — Combined Copy matcher body

Added NativeMorphOSCopyMatchStep to connect reusable snapshots, deferred depth, first-entry/exit/directory branch order and DOS pattern filtering. Native root builds and compiles resident for all three CPUs. Reports: artifacts/copy-match-step-static. Combined runtime coverage, matcher iteration, DoWork and final cleanup remain required.


### 2026-09-05 — Combined matcher-body native verification

Added branch-order, full snapshot, pending-depth, pattern and interleaving vectors for NativeMorphOSCopyMatchStep. Corrected large fixture workspace spacing after the initial interleaving failure. All 11 invocations pass per CPU with no leaks or image writes. Receipt: artifacts/qualification-copy-match-step-native/qualification.json. Matcher iteration and actual deferred/final DoWork remain open.


### 2026-09-05 — Streaming filesystem matcher lifecycle

Implemented the surrounding PatCopy filesystem loop, deferred/final worker calls, matcher diagnostics, parent-error handling and resource release. The invocation-owned generic worker root builds and compiles resident for all three CPUs. Static reports: artifacts/copy-traversal-static. Runtime lifecycle verification, production DoWork, initial classification and non-filesystem routing remain required.


### 2026-09-05 — Streaming matcher native runtime

Qualified 8 lifecycle invocations per CPU, covering deferred/final snapshot identity, MatchEnd order, empty and failed matching, allocation failure, diagnostic error preservation and interleaved callers. All passed without leaked anchors or shared-image writes. Receipt: artifacts/qualification-copy-traversal-native/qualification.json. Worker-driven threshold changes, recursive sequences and production DoWork remain open.


### 2026-09-05 — Worker-driven traversal stopping

Verified primary and secondary result writes through the generic reporting worker, including normal warning continuation, error stopping, ERRWARN stopping and guarded final work. All 12 native invocations pass per CPU with no leaks or image writes. Receipt: artifacts/qualification-copy-traversal-thresholds/qualification.json. Recursive sequences and production file operations remain open.


### 2026-09-05 — DoWork guard correction and path prelude

Source inspection showed the reporting worker lacked DoWork's primary-result guard. Corrected it and the primary-stop assertions: both primary and secondary errors suppress final object action. All 12 cases pass per CPU in qualification-copy-traversal-work-guard. Added the production destination-path prelude using NameFromLock/AddPart and invocation-owned cache state; its native runtime qualification and object-action integration remain open.


### 2026-09-05 — Native destination-path prelude

Qualified work preparation on all three CPUs, 12 invocations each: cached-path truncation, NameFromLock/AddPart arguments and failures, mode bypass, primary/secondary guards and interleaved storage. All pass without leaks or image writes. Receipt: artifacts/qualification-copy-work-preparation-native/qualification.json. Complete object worker remains open.


### 2026-09-05 — File-pair source fidelity correction

DoWork source inspection disproved the earlier source-first Open ordering. Corrected the adapter and native fixture to open destination first, skip source on destination failure, and close/delete destination on source-open failure. Nine invocations pass per CPU in qualification-copy-file-pair-open-order, with no leaked handles or shared-image writes. Full object worker remains open.


### 2026-09-05 — Full CopyFile transfer policy implementation

Added cached buffer allocation/retry, default ExamineFH and gated MorphOS ExamineFH64, size-aware EOF handling and stream fallback. Native root builds and compiles resident on all three CPUs; reports at artifacts/copy-file-transfer-static. Runtime allocation/examination/EOF verification and DoWork integration remain open.


### 2026-09-05 — Native CopyFile transfer validation

All 14 invocations pass per CPU for allocation retry/exhaustion, cache reuse, examination APIs/tags, known-size and stream EOF, premature EOF, Ctrl-C and I/O failure. Extended size above 4 GB is checked; actual carry remains open. Receipt: artifacts/qualification-copy-file-transfer-native/qualification.json. No cache leaks or shared-image writes. Worker integration remains required.


### 2026-09-05 — Copy/MOVE file-operation integration

Connected the full CopyFile transfer to source-lock release, destination-first opens, close ordering, MOVE source deletion, FORCEDELETE and failed-destination cleanup. Preserved post-close KillFileKeepErr timing from the source. Native root builds and compiles resident on all three CPUs. Reports: artifacts/copy-file-operation-static. Integrated runtime qualification and full DoWork dispatch remain open.


### 2026-09-05 — Integrated file-operation native qualification

Nine invocations pass per CPU for COPY/MOVE handle and lock ordering, source/destination failures, transfer cleanup, forced deletion failure and direct-device/interleaved ownership. Completed destination survives failed MOVE source deletion. Receipt: artifacts/qualification-copy-file-operation-native/qualification.json. Full DoWork selection and diagnostics remain open.


### 2026-09-05 — Copy hard/soft link operation

Implemented source LinkFile behavior using MakeLink and device-qualified NameFromLock for soft links. Eight invocations pass per CPU, covering allocation/name/link failures and interleaved callers with no leaks or image writes. Receipt: artifacts/qualification-copy-link-operation-native/qualification.json. DoWork link-mode restrictions remain required.


### 2026-09-05 — TestDest cleanup fidelity

Corrected FIB lifetime and error timing to match original TestDest: unlock before deletion, free FIB afterward, no restoration of an earlier error. Added a cleanup-error vector that would reject the previous implementation. Twelve invocations pass per CPU in qualification-copy-destination-cleanup-order. Directory coordinator remains open.


### 2026-09-05 — Directory operation coordinator

Connected loop/destination policy to directory entry/create, failed-entry lock restoration, previous-destination release, MOVE rename and LINK restrictions. Native root builds and compiles resident on all three CPUs. Reports: artifacts/copy-directory-operation-static. Integrated runtime branches and full DoWork diagnostics remain open.


### 2026-09-05 — Directory coordinator native runtime

Fourteen invocations pass per CPU for creation/entry failures, prior-lock restoration and release, rename, hard-link restrictions, display-only and loop rejection, with interleaved callers. Receipt: artifacts/qualification-copy-directory-operation-native/qualification.json. Full DoWork diagnostics and broader recursive integration remain open.


### 2026-09-05 — Worker progress/error output

Implemented source PrintName and always-verbose PrintNotDone formatting and API order, using invocation-owned VPrintf arguments. Root builds and compiles resident for all CPUs; reports: artifacts/copy-output-static. Runtime formatting and full worker integration remain open.


### 2026-09-05 — Native Copy output verification

Seven cases pass per CPU for exact file/directory spacing, progress dots, Flush(Output()) timing, post-prefix error selection and interleaved arguments. Receipt: artifacts/qualification-copy-output-native/qualification.json. Full object-worker integration remains required.


### 2026-09-05 — Combined DoWork implementation

Assembled NativeMorphOSCopyWork with source mode dispatch, locking, operations, reporting and metadata, implementing the streaming worker interface. Combined root builds and compiles resident on all three CPUs; reports: artifacts/copy-work-static. Updated CP8 to combined-worker runtime and traversal integration. Full command lifecycle and parity remain open.


### 2026-09-05 — Original byte-depth semantics

Corrected depth increment/decrement to source UBYTE wraparound. Added overflow and underflow vectors to the combined match-step suite; all 13 cases pass on each CPU with no leaks or image writes. Receipt: artifacts/qualification-copy-match-step-byte-depth/qualification.json. Combined worker runtime remains the next integration checkpoint.


### 2026-09-05 — Combined DoWork native COPY/DELETE

Eleven invocations pass per CPU through the assembled worker, covering quiet COPY/DELETE operations and failures, source/parent ownership, forced deletion, directory passes and interleaved callers. Receipt: artifacts/qualification-copy-work-native/qualification.json. Other modes, visible diagnostics, metadata and full traversal integration remain open.


### 2026-09-05 — Combined worker MOVE/LINK/direct modes

Extended native worker coverage to MOVE rename/fallback, hard links, soft-file rejection and direct output lock bypass. All 16 cases pass per CPU without leaked buffers or shared-image writes. Receipt: artifacts/qualification-copy-work-modes/qualification.json. Visible reporting, metadata and traversal integration remain open.


### 2026-09-05 — Matcher connected to actual DoWork

Added the filesystem traversal entry that instantiates NativeMorphOSCopyWork and shares invocation state/cache across matches. Combined root builds and compiles resident on all three CPUs; reports: artifacts/copy-traversal-work-static. Runtime sequence verification and full command lifecycle remain open.


### 2026-09-05 — Actual matcher/DoWork native sequences

Five sequence cases pass per CPU with real COPY worker dispatch and payload checks. Verified deferred/final ordering, destination/cache reuse, lock/handle ownership and interleaved caller storage. Receipt: artifacts/qualification-copy-traversal-work-native/qualification.json. Recursive/error sequences and full command lifecycle remain open.


### 2026-09-05 — PatCopy source entry routing

Implemented NativeMorphOSCopyTraversal.Run over the existing matcher and actual worker. Preserves classification-before-device-probe ordering, literal-only old directory syntax, COPY-only non-filesystem routing, restored device-name suffix, source-path copying, FilePart dispatch and temporary SRCNOFILESYS flag lifetime. Classifier scratch is supplied by the invocation, without new shared state or routing allocations. Source lengths must be validated by the command frontend before this entry.

The dedicated NativeMorphOSCopySourceEntry root builds and compiles resident for 68000/020/040 with 32 reachable methods and zero runtime features/helpers, external native targets, exception regions or fatal machine-fault sites. Static reports and checked artifact hashes: artifacts/copy-source-routing-static. This is compilation evidence only; source-routing runtime cases, full command argument/lifecycle integration, recursive/error sequences and original comparisons remain open. No shipping gate closed.

### 2026-09-05 — Native source routing through actual worker

Eight source-entry cases pass per CPU (68000/020/040): empty/wildcard/literal inputs, failed classification, relative names, direct stream input and interleaved filesystem callers. Device-prefix restoration, classification-before-probe/traversal ordering, copied payload, direct FilePart dispatch, no traversal allocation for stream input, flag cleanup and resource ownership are checked. Receipt: artifacts/qualification-copy-source-routing-native/qualification.json; reproducible driver: tools/Commands/qualify_copy_source_routing_native.ps1. The previous five-case traversal suite also passes on each CPU after fixture reuse changes. Directory-first behavior, stream failure/interleaving and full command lifecycle remain open; no shipping gate closes.

### 2026-09-05 — Correct positional destination and DIRECT DELETE admission

Source review before frontend composition found that the earlier argument gate incorrectly limited implicit final-argument TO to default COPY. The original applies it to MOVE and both LINK modes as well (only DELETE and MAKEDIR are excluded). Corrected the admission count and removed unused mode inputs. Also corrected DIRECT DELETE to admit multiple sources: the original single-source restriction is inside the non-DELETE branch. DIRECT COPY still rejects multiple sources after TO normalization.

Added MOVE/HARDLINK/SOFTLINK positional-target, multiple-source DIRECT DELETE and rejecting multiple-source DIRECT COPY cases. All 18 ReadArgs-boundary cases pass on each of 68000/020/040, with no resource leaks or shared-image writes. Receipt: artifacts/qualification-copy-argument-gate-positional/qualification.json. This remains an admission gate that frees parsed arguments; full frontend ownership, actual FROM-vector normalization and operations still need composition. Earlier default-COPY-only documentation is superseded by this source correction.

### 2026-09-05 — Retained Copy argument lease

Split the Copy argument gate into Read (returns a live parser lease), Validate (borrows it by reference) and the existing Run wrapper (releases after validation). This removes the forced early FreeArgs boundary from the API needed by the full command and removes by-value copies of live leases during option reads. The caller must release a successful parse even if later mode admission fails. No source pointer may outlive that release.

The existing 18-case native parser/admission regression passes on all three CPUs through the new composition, with 16 reachable methods and no leaked resources or shared-image writes. Receipt: artifacts/qualification-copy-argument-gate-lease/qualification.json. The structural expected method count was updated from 14 after inspecting the compiled report. The regression still exercises immediate wrapper cleanup, not a command holding the lease across file operations. Extended-help RDArgs setup and end-to-end retained lifetime remain open.

### 2026-09-05 — Copy retained-option state setup

Implemented NativeMorphOSCopyOptionSetup with borrowed source/target/pattern pointers, invocation-owned omitted-source vector, actual positional TO removal, original buffer-unit arithmetic, mode/operation flags and metadata mapping. QUIET does not imply NOREQ; PROX does not remove default PROTECTION. Separate SelectVerbosity preserves post-normalization multi-source and classifier behavior, including failed classification enabling verbose output. The caller must use a fresh state and successful admission; requester changes, pattern compilation and operation dispatch remain outside this helper.

The dedicated parser/setup root builds and compiles resident for all three CPUs without managed runtime features/helpers, external targets, exception regions or fatal machine-fault sites. Reports: artifacts/copy-option-setup-static. This is static evidence only; the new setup values, pointer-vector mutation and retained lifetime still require native runtime qualification. Source review also reconfirmed that SetData orders dates before comments, whereas the current metadata helper orders comments first; correct and requalify that remaining fidelity gap before full command acceptance.

### 2026-09-05 — Correct SetData operation ordering

Moved SetComment after classic/POSIX date application, preserving original SetData order: protection, date, comment. Added order assertions to the native DOS gateways, including continuation after their deliberately failing return values. All eight metadata cases pass on each CPU, with no leaked resources or shared-image writes. Receipt: artifacts/qualification-copy-metadata-order/qualification.json. Running the previous 68000 metadata artifact against the strengthened fixture fails specifically at the premature SetComment call, providing a negative control. This resolves the helper order gap recorded in the prior option-setup checkpoint; full worker/command metadata integration still needs refreshed qualification.

### 2026-09-05 — Original TestLoop null-lock semantics

Corrected NativeMorphOSCopyLoopGuard to use the original do/while traversal and final lock-identity cleanup without a null exemption. Same-device checks now call SameLock once even when the initial destination lock is null. Root exhaustion issues the original UnLock(NULL) when it differs from the caller's destination; the caller-owned destination remains untouched.

Expanded the native fixture with null-destination same-object and null-parent cases, changed root-exhaustion cleanup expectations, and checked SameLock/UnLock arguments. Eight cases pass on each CPU with no leaked resources or shared-image writes. Receipt: artifacts/qualification-copy-loop-guard-null/qualification.json. This closes the bounded helper mismatch, not recursive command or real-handler qualification. Combined directory/worker receipts need rebuilding against the changed body.

### 2026-09-05 — Retained option setup native execution

Eight setup invocations pass per CPU: positional COPY/MOVE, omitted source, CLONE+QUIET, PROX+NOPRO+NOREQ, multi-source DIRECT DELETE and interleaved callers. Checks cover resulting mode/flags/metadata/default buffer/requester intent/source count and live parser ownership during classification. Receipt: artifacts/qualification-copy-option-setup-native/qualification.json; driver: tools/Commands/qualify_copy_option_setup_native.ps1. The 18-case argument-gate regression also passes per CPU after shared fixture changes. Buffer overrides/overflow, wildcard/failure verbosity, complete target pointer verification and lease lifetime across file operations remain open. No command shipping gate closed.

### 2026-09-05 — Retained parser/setup connected to DIRECT operations

Implemented NativeMorphOSCopyDirect from the original separate branch. COPY opens source before destination, runs the existing full transfer, closes destination then source, preserves initial secondary failure on open errors and does not delete failed partial output. DELETE visits every source, ignores individual deletion results and sets secondary OK afterward. Transfer cache ownership remains with the caller. Private NativeMorphOSCopyDirectEntry composes ReadArgs, admission, setup, verbosity and DIRECT while retaining the parser through operations; it is not a shipping frontend or full lifecycle implementation.

The combined root builds and compiles resident on 68000/020/040, with no runtime features/helpers, external targets, exception regions or fatal machine-fault sites. Reports: artifacts/copy-direct-static. Native execution of the new composition, close-error precedence, partial-output behavior and parser lifetime through operations remain unverified. Pattern compilation/non-DIRECT dispatch and exact original startup/finalization remain open.

### 2026-09-05 — DIRECT retained-parser native execution

Seven invocations per CPU pass through ReadArgs, admission, setup and DIRECT: payload copy, input/output open failures, failed write without partial-output deletion, continuing through two failed deletes and interleaved callers. Native gateways require live parser ownership during opens, I/O, close and delete. Checks cover input-first open, output-first close, operation counts, secondary result and cleanup. Receipt: artifacts/qualification-copy-direct-native/qualification.json; reproducible driver: tools/Commands/qualify_copy_direct_native.ps1. The eight-case setup regression passes per CPU after fixture reuse changes. This is still the private DIRECT root; original finalization, requesters, full dispatch and real-handler comparisons remain open.

### 2026-09-05 — Pattern preparation and normal DELETE dispatch

Implemented normal-operation PATTERN allocation/parsing through ParsePatternNoCase, preserving allocation size 2*length+3, MEMF_ANY, failed-parse cleanup and original IoErr timing. An explicitly empty pattern alone synthesizes BadTemplate. Successful pattern storage remains invocation-owned across all sources and is released explicitly. Added the normal DELETE outer source loop connected to actual traversal/DoWork, retaining its primary-only stopping condition and initial secondary OK.

Private NativeMorphOSCopyPatternDeleteEntry builds and compiles resident on all three CPUs without runtime features/helpers, external targets, exception regions or fatal machine-fault sites. Reports: artifacts/copy-pattern-delete-static. Runtime pattern failures, filter behavior across multiple sources, recursive deletion and retained parser/full-command composition remain open. This root uses a supplied source vector; it does not yet join the ReadArgs frontend to normal DELETE.

### 2026-09-05 — Retained ReadArgs joined to normal DELETE

Added private NativeMorphOSCopyParsedDeleteEntry joining the live ReadArgs lease, option setup, verbosity, PATTERN preparation and normal DELETE source loop through actual traversal/DoWork. Its invocation-owned 2610-byte scratch contains the default-source vector/string, classifier AnchorPath, full 2048-byte path snapshot, 260-byte FIB and eight-byte reporting arguments without overlap. Pattern and scratch storage are freed before releasing the parser, so source pointers remain owned throughout operations.

The combined root builds and compiles resident on all CPUs without runtime features/helpers, external targets, exception regions or fatal machine-fault sites. Reports: artifacts/copy-parsed-delete-static. This establishes compilation of the complete normal DELETE operation chain, not runtime correctness or a shipping command. Full parsed traversal runtime cases, original explicit RDArgs/extended help, requester and error-finalization ordering remain open. The private root restricts dispatch to normal DELETE for qualification only; all requested command modes remain in scope.

### 2026-09-05 — Parsed normal DELETE native runtime

Six cases per CPU pass through retained ReadArgs, setup, normal traversal and actual DELETE worker: one/two sources, failed deletion with continuation, ERRWARN stopping and interleaved callers. The fixture checks source-vector advancement, final work after MatchEnd, source/parent unlock order, primary WARN versus secondary OK and parser liveness through delete/cleanup. Receipt: artifacts/qualification-copy-parsed-delete-native/qualification.json; driver: tools/Commands/qualify_copy_parsed_delete_native.ps1. Seven DIRECT regression cases also pass per CPU after shared fixture changes. Private control offset 24 now exposes primary result for this root. PATTERN allocation/filter/error cases, recursive deletion, visible diagnostics and actual command finalization remain open.

### 2026-09-05 — Parsed DELETE pattern execution

Expanded the normal DELETE matrix to ten cases per CPU with accepted/rejected filename filters and invalid/empty PATTERN. Checks enforce seven-byte MEMF_ANY storage for the two-character input, ParsePatternNoCase ABI, compiled storage reuse across both sources, borrowed filename arguments, parser liveness and cleanup. Rejected filters perform no source locks/deletions; invalid/empty patterns perform no traversal and retain secondary FAIL. All ten cases pass per CPU without resource leaks or shared-image writes. Receipt: artifacts/qualification-copy-parsed-delete-pattern/qualification.json. These are supplied pattern-vector outcomes, not proof of parser grammar or original-handler correspondence. Allocation failure, mixed filters, recursive behavior and final diagnostic/IoErr timing remain open.

### 2026-09-05 — State-based command completion stage

Added NativeMorphOSCopyResultPolicy.Complete to map original state flags, query Ctrl-C only for secondary OK, apply original reporting/primary precedence/ERRWARN promotion, store the selected secondary result and then release the transfer cache. The caller must first finish pattern cleanup, requester restoration and parser teardown; state/DOS ownership remains outside this stage. No claim is made that the existing generic parser lease reproduces original explicit RDArgs cleanup effects.

Nine native result cases pass per CPU through Complete with actual signal-vector queries and no leaked resources or image writes. Receipt: artifacts/qualification-copy-completion-native/qualification.json. The fixture has no allocated transfer cache, so cleanup ordering with live cache and full parsed-operation finalization still require tests. Exact explicit RDArgs/extended-help and requester behavior remain open.

### 2026-09-05 — Completion with allocated transfer cache

Extended the completion root and native matrix with allocated-cache variants of every existing result case. Sixteen cases pass per CPU. PrintFault gateways require that no cache free has happened yet; subsequent FreeMem changes IoErr to 902, which is observed after completion. The root verifies that both cache pointer and size are cleared, and the fixture checks one allocation/one free. Receipt: artifacts/qualification-copy-completion-cache/qualification.json. This closes the isolated allocated-cache completion check, not full parser/requester/command integration or original runtime parity.

### 2026-09-05 — Parsed Copy MAKEDIR branch

Implemented NativeMorphOSCopyMakeDirectory with original ParsePattern-based destination classification, source-list stopping conditions and unconditional current-source directory attempt after pattern failure. Added the stateful filesystem directory-opening/reporting path: prefix restoration, creation lock ownership, source diagnostics, primary ERROR selection, existing-directory warning and final shared lock. Unlike the older bounded prefix helper this path includes command reporting/state and skips prefix work for an empty name as the original does. It reuses the current TestDest helper, whose extended-examine parity remains open.

Private NativeMorphOSCopyParsedMakeDirectoryEntry joins retained parsing/setup/verbosity to this branch. The root builds and compiles resident for all CPUs with no runtime features/helpers, external targets, exception regions or fatal machine-fault sites. Reports: artifacts/copy-parsed-makedir-static. Runtime creation, existing-directory, wildcard/failure and output cases remain unverified; no shipping gate closes.

### 2026-09-05 — Parsed Copy MAKEDIR native execution

Seven cases pass per CPU through retained parsing/setup and MAKEDIR: one/two directories, creation failure, ParsePattern failure, wildcard classification and interleaving. Tests enforce the original current-source creation attempt after classification errors, subsequent list stopping, source strings, parse allocation size, parser liveness and created/final lock release order. Receipt: artifacts/qualification-copy-parsed-makedir-native/qualification.json. The ten-case parsed DELETE regression also passes per CPU after shared fixture changes. Existing-directory handling, nested prefixes, visible output, full command lifecycle and original comparisons remain open.

### 2026-09-05 — Stateful directory-target COPY/MOVE routing

Added stateful OpenDestDir routing that selects non-filesystem destinations only for COPY/MOVE, sets DESNOFILESYS before attempting the empty-name lock, and retains the copied destination and flag even if that lock fails. Filesystem destinations use the stateful prefix creation/reporting implementation. Added the admitted directory-target source loop with primary/secondary/DEST_FILE guards, original per-source cancellation ordering, shared root destination ownership and actual PatCopy traversal. This stage intentionally does not classify single-file destinations or change admission results.

Private NativeMorphOSCopyDirectorySourcesEntry builds and compiles resident on all CPUs with no runtime features/helpers, external targets, exception regions or fatal machine-fault sites. Reports: artifacts/copy-directory-sources-static. Runtime directory/stream destination creation, failure/cancellation, parser composition and single-file destination routing remain open. Earlier bounded OpenDestination helper remains unchanged for its prior receipts.

### 2026-09-05 — Directory-target COPY native runtime

Five cases pass per CPU with destination creation followed by zero/one/two matched files and interleaved callers. The suite executes actual traversal and COPY payload operations, verifies creation before source classification, and holds the root destination until all pending work has completed. Receipt: artifacts/qualification-copy-directory-sources-native/qualification.json. Eight source-routing regression cases also pass per CPU. Each case has one source pattern; multiple source patterns, existing or stream destinations, cancellation, MOVE, retained parser integration and full command parity remain open.

### 2026-09-05 — Normal target classification and single-file dispatch

Implemented NativeMorphOSCopyTargetDispatch for normal COPY/MOVE/LINK after PATTERN setup: destination ParsePattern validation, PathPart-based file/directory selection, source MatchFirst classification, destination/source examination, tick-quoted source normalization, parent-directory creation, actual single-file DoWork and stream-source fallback, followed by admitted directory-source dispatch. Source-observed failure mutations and lock-value tests are retained, including target suffix restoration only after successful parent opening. Classic Examine is default; the existing caller-selected extended mode uses Examine64 with invocation-owned tags.

Private NativeMorphOSCopyTargetDispatchEntry builds and compiles resident for all three CPUs without runtime features/helpers, external targets, exception regions or fatal machine-fault sites. Reports: artifacts/copy-target-dispatch-static. No runtime target-branch receipt exists yet. File/existing-directory ambiguity, quoted names, failure cleanup, stream fallback, MOVE/LINK, version gating and retained parser integration all require verification before command parity.

### 2026-09-05 — Unified retained-parser operation dispatch

Added NativeMorphOSCopyOperations to select MAKEDIR, DIRECT or normal PATTERN/DELETE/target dispatch from one admitted option/state record. The normal branch emits the original nothing-processed notice using captured argument verbosity, not later traversal changes to COPYFLAG_VERBOSE, and releases the pattern after that notice. Added NativeMorphOSCopyParsedOperationsEntry joining retained ReadArgs/setup/verbosity to every operation mode with separate path and destination workspaces (4658 total scratch bytes).

The combined root builds and compiles resident on all three CPUs without runtime features/helpers, external targets, exception regions or fatal machine-fault sites. Reports: artifacts/copy-parsed-operations-static. This is the first unified operation root, still with private control/output conventions and unqualified full-mode runtime coverage. Original explicit RDArgs/help, pre-admission side effects, requesters, version gating, finalization and shipping packaging remain open.

### 2026-09-05 — Unified operation root runtime regression

The unified parsed operation HUNK passes the ten DELETE, seven MAKEDIR and seven DIRECT cases on each CPU: 72 invocations total. Added an explicit +unified-copy fixture layout selector limited to those parsed suites; it changes workspace allocation expectations, not operation assertions. The unified DELETE workspace exposes the original metadata tail against an empty destination name, previously skipped by a null destination buffer in the branch root. Added a checked SetProtection callback for that original behavior. Receipt: artifacts/qualification-copy-unified-operations/qualification.json; driver: tools/Commands/qualify_copy_unified_operations_native.ps1. Normal COPY/MOVE/LINK target branches and full shipping lifecycle remain unqualified.

### 2026-09-05 — Native destination classification to directory COPY

Seven cases per CPU pass through target dispatch: zero/one/two directory-target matches, wildcard/invalid destination rejection and interleaving. Checks enforce ParsePattern allocation/cleanup before PathPart, root creation after classification, payload/resource behavior and no filesystem/source-classifier work after rejection. Receipt: artifacts/qualification-copy-target-dispatch-native/qualification.json. Five prior directory-source cases pass per CPU as regression controls. The new suite supplies parser results; it does not establish DOS grammar correctness. Single-file ambiguity, quoting, stream fallback, MOVE/LINK and full parser integration remain open.

### 2026-09-05 — Single-file target native execution

Three invocations per CPU (one successful source/target pair plus interleaved callers) pass the normal single-file target path. Checked destination/source ParsePattern calls, both source classifiers, examined source, unchanged parsed-name lock/copy, parent creation, FilePart naming, payload, output-first normal copy opens and all six lock releases. Receipt: artifacts/qualification-copy-single-target-native/qualification.json. Seven directory/rejected-target regression cases also pass per CPU. This is a positive-path receipt only: source quoting transformation, failures, existing destination ambiguity, stream fallback, MOVE/LINK and retained-parser composition remain open.

### 2026-09-05 — Normal single-file transfer failures

Expanded single-target runtime coverage to six cases per CPU with destination-open, input-open and write failures. Checks enforce primary WARN, balanced source/parent locks, no output deletion after failed destination open, and partial-output deletion only after closing opened handles. Close deliberately changes IoErr before cleanup; the deletion callback verifies this post-close observation. Receipt: artifacts/qualification-copy-single-target-failures/qualification.json. All cases pass without leaks/image writes. Earlier target/source examination and parent-opening failures, transformed quoting, streams, MOVE/LINK and retained-parser target composition remain open.

### 2026-09-05 — Single-file MOVE and LINK target execution

Expanded the normal single-file target suite to ten invocations per CPU: COPY success/open/write failures, MOVE rename and copy/delete fallback, hard-link creation, soft-file rejection and interleaved callers. Checks enforce rename/link arguments and order, source-lock ownership, no transfer for rename/link, fallback source deletion after both closes, soft-file primary WARN and restored target suffix.

Receipt: artifacts/qualification-copy-single-target-modes/qualification.json. All 30 invocations pass across 68000/68020/68040 with balanced resources and no shared-image writes. These are supplied DOS vectors through a private entry, not shipping lifecycle or original-system differential evidence. Directory MOVE/LINK, quoting, earlier selection failures, streams and retained-parser target composition remain open. No shipping gate closes.

### 2026-09-05 — Explicit Copy RDArgs lifecycle integration

Added NativeMorphOSCopyParser with DOS AllocDosObject(RdArgs), original extended help, ReadArgs using caller-cleared 24-LONG storage, successful-parse FreeArgs and unconditional owned-object FreeDosObject. The original stale BUFFER default and QUIET help wording are preserved. New NativeCommandArguments borrowed-storage methods retain parser-owned result pointers without allocating/freeing the caller array or preserving IoErr across parser cleanup. Existing owned parsing remains unchanged; callers must pair each lease with its matching release method.

Added private NativeMorphOSCopyExplicitParserOperationsEntry joining this parser to setup and unified operations. Build succeeds and resident native compilation passes on 68000/68020/68040; reports are artifacts/copy-explicit-parser-static. Runtime parser allocation/failure/help/cleanup verification is still required. This entry remains private and does not yet reproduce original pre-admission effects, requester handling, cache/finalization ordering or full startup. No shipping gate closes.

### 2026-09-05 — Explicit RDArgs native operation execution

The explicit-parser operation entry passes 25 invocations per CPU: DELETE ten, MAKEDIR seven, DIRECT seven and a parser-failure case. The fixture verifies DOS_RDARGS type/tag arguments, non-null caller-supplied ReadArgs object, all 24 initially cleared result slots, SHA-256 of the complete source-observed extended help, FreeArgs before FreeDosObject on successful parsing, and FreeDosObject without FreeArgs on parser failure. Operations retain parser-owned data through execution. FreeDosObject deliberately overwrites IoErr; the private entry still reports its captured fixture result, so this does not qualify original command finalization timing.

Receipt: artifacts/qualification-copy-explicit-parser/qualification.json (75 invocations total). Existing owned-parser operation regression also passes 72 invocations: artifacts/qualification-copy-owned-parser-regression/qualification.json. No resources leak or shared-image writes occur in these supplied-vector runs. DOS_RDARGS allocation failure, interactive DOS help rendering/grammar, requester/version handling, pre-admission effects and shipping finalization remain open. No shipping gate closes.

### 2026-09-05 — Copy requester ownership in explicit operation entry

Added NativeMorphOSCopyRequesters using invocation-owned saved process/window state. It captures pr_WindowPtr, writes -1 only for NOREQ, and restores the original value before parser teardown. Integrated into the explicit-parser operation root around setup/verbosity/operations. The fixture seeds distinct process window pointers, checks suppression during DIRECT success and output-open failure, and verifies restoration at FreeArgs. Native-write permission is restricted to the current parsed invocation's four-byte WindowPointer field, not the entire process.

All 25 cases per CPU pass (75 total) across 68000/68020/68040: artifacts/qualification-copy-requesters/qualification.json. The root has 61 reachable methods and no reported runtime features/helpers, external native targets, exception regions or fatal machine-fault sites. This remains a private entry: requester capture/suppression on rejected admission, final cache/report ordering, version handling, original runtime behavior and shipping qualification remain open. No shipping gate closes.

### 2026-09-05 — Pre-admission Copy setup ordering

Separated no-I/O admission evaluation from error publication. Existing Validate remains an Evaluate + SetIoErr wrapper. The explicit-parser operation entry now performs option/default-source setup, positional target normalization, requester suppression and verbosity classification even when admission will fail, then publishes the rejection instead of running operations. Successful admission no longer resets IoErr before this setup through Validate.

Added rejected DIRECT+CLONE+NOREQ coverage verifying one classifier/cleanup pair before rejection, no early TooManyArguments error, suppressed requester state during classification, restored state before FreeArgs, retained setup values and no file operations. All 26 invocations per CPU pass (78 total); the strengthened DIRECT assertions were rerun on every CPU. Receipt: artifacts/qualification-copy-admission-order/qualification.json. This is supplied-vector private-entry evidence; original state-allocation/startup ordering, final result/cache teardown, version handling and shipping comparisons remain open. Existing wrapper behavior is retained but older static method-count receipts need regeneration after the new Evaluate boundary. No shipping gate closes.

### 2026-09-05 — Normal-ABI Copy lifecycle candidate

Added NativeMorphOSCopyCommand.Run combining explicit parsing, pre-admission setup/verbosity/requesters, all operation dispatch, requester restoration, parser teardown, final diagnostics/result selection, transfer cache release and workspace release in one invocation. A cleared 4754-byte workspace contains the result array and prior operation buffers; parser storage survives operations, and transfer cache survives parser cleanup and final reporting. Added NativeMorphOSCopyCommandEntry with normal command ABI and no private control block. It rejects/replies Workbench startup before opening DOS, uses DOS 37 minimum and closes DOS without restoring an earlier IoErr after final cleanup.

Build succeeds; resident compilation succeeds for 68000/68020/68040 with no reported runtime features/helpers, external native targets, exception regions or fatal machine-fault sites. Reports: artifacts/copy-command-lifecycle-static. This is a command candidate, not a qualified shipping artifact. Whole-entry runtime, allocation and parser failure paths, command output/results, version-gated extended examination, original-runtime differential behavior, packaging and original purity requirements remain unqualified. The original allocates CopyData even after DOS-open failure; this candidate returns immediately on that failure, so startup failure parity remains open. No shipping gate closes.

### 2026-09-05 — Normal Copy command ABI runtime

Added a +command fixture selector restricted to DIRECT lifecycle cases. It invokes the normal command entry without the private 32-byte control block, validates real return levels and final IoErr, and reuses existing payload, open/close, parser/help and requester assertions. The command's single 4754-byte cleared workspace replaces separate result/scratch allocations. Final PrintFault must observe the deliberately poisoned FreeDosObject error 903 before any cache/workspace free; final cleanup leaves IoErr 902 rather than restoring the earlier error. Tests include successful DIRECT copy/delete, input/output/write failure, parser failure, rejected DIRECT+CLONE+NOREQ and interleaved invocations.

Nine cases pass per CPU (27 total), with balanced resources and no shared-image writes. Reports: artifacts/copy-command-lifecycle-static/direct-68000.runtime.json and corresponding 68020/68040 reports. Reproduction driver: tools/Commands/qualify_copy_command_lifecycle_native.ps1. These execute the normal ABI candidate through supplied DOS vectors; normal non-DIRECT modes, startup failures, Ctrl-C/ERRWARN integration, actual DOS parser behavior, original comparisons and shipping qualification remain open. No shipping gate closes.

### 2026-09-05 — Normal command DELETE/MAKEDIR and final cancellation

Extended the normal-ABI command fixture to DELETE and MAKEDIR, preserving their existing parser, pattern, payload, lock and stopping assertions. Final expectations now select primary errors over secondary results and apply ERRWARN promotion. Added a DIRECT copy case whose Ctrl-C becomes pending only after FreeDosObject; it checks the final signal query, PrintFault(ERROR_BREAK) before any cache/workspace free, RETURN_ERROR with ERRWARN, and cleanup's final IoErr rather than an artificially restored break error.

All 27 cases per CPU pass (81 total): DELETE ten, MAKEDIR seven, DIRECT ten. Receipt: artifacts/qualification-copy-command-final-break/qualification.json. The earlier 26-case matrix also completed successfully in artifacts/qualification-copy-command-operations. All invocations use the normal command entry with supplied DOS vectors and no private output control. Normal COPY/MOVE/LINK target composition, startup/allocation failure paths, version gating, original runtime comparisons and shipping qualification remain open. No shipping gate closes.

### 2026-09-05 — DOS version selection and missing-DOS allocation path

The normal Copy command now attempts workspace allocation even after DOS-open failure, then frees any acquired workspace and returns FAIL without DOS calls, matching the original control-flow requirement. This failure branch is implemented but not runtime-qualified yet. The command selects ExtendedExamine from the open library's version/revision: 51.66 or newer; older libraries retain classic calls.

Added normal-command DIRECT cases for DOS 51.65, 51.66 and 52.0. Callbacks reject the wrong legacy/extended examination vector, verify the extended POSIX-date request tags and supply a 64-bit size. All 30 invocations per CPU pass (90 total): artifacts/qualification-copy-command-version-boundaries/qualification.json. The earlier 27-case regression also completed in artifacts/qualification-copy-command-version-gate. Destination TestDest still uses classic Examine and requires the same extended-path integration; the new version predicate does not close that gap. Normal COPY/MOVE/LINK target integration, startup failure runtime, original-runtime comparison and shipping qualification remain open. No shipping gate closes.

### 2026-09-05 — Version-aware destination examination

Added the extended TestDest examination path: clear actual extension flags before examining; use Examine64 with POSIX-date tags when selected, otherwise classic Examine. Stateful directory opening, file work and directory work now pass the command's version choice and dedicated tag workspace. The normal command workspace grows from 4754 to 4770 bytes to retain sixteen invocation-owned tag bytes without overwriting a live source/destination string. The classic bounded helper overload remains available.

All 30 normal-command regression cases pass per CPU (90 total), including existing DIRECT version boundary tests: artifacts/qualification-copy-destination-examine/qualification.json. These regressions do not yet exercise an existing destination through the new Examine64 path; its lock/examine/error/cleanup runtime matrix remains required. Callers enabling ExtendedExamine must now supply ExamineTags (sixteen writable bytes); older private roots that set the flag without that storage require updating before using extended destination cases. Full normal target composition, original-system comparison and shipping qualification remain open. No shipping gate closes.

### 2026-09-05 — Existing destination Examine64 runtime

Added normal-command MAKEDIR cases for an existing directory and failed Examine64 on DOS 51.66. The fixture provides a destination FIB through AllocDosObject, poisons its actual extension flags, checks that the command clears them and supplies the original POSIX-date tags, and verifies no directory creation occurs. Success unlocks the destination before freeing the FIB and subsequently obtains/releases the final lock. Examination failure frees the FIB before releasing the failed target lock, returns primary ERROR, and skips the final lock.

All 32 cases per CPU pass (96 total): artifacts/qualification-copy-existing-destination/qualification.json. The stronger poisoned-flag and FIB/unlock ordering assertions were rerun for all three CPUs. Existing-file deletion/overwrite variants, legacy destination examination, FIB allocation failure, normal COPY/MOVE/LINK composition and original-system qualification remain open. This is supplied-DOS normal-entry evidence, not shipping approval. No shipping gate closes.

### 2026-09-05 — Normal command startup allocation failures

Added runtime cases for missing DOS with successful workspace allocation, workspace allocation failure with DOS available, and both failures together. The first enforces exactly one allocation/free and no parser, fault, SetIoErr or filesystem calls. Allocation failures enforce no free of nonexistent storage and no parser allocation; only the DOS-available case reports the current IoErr. All return FAIL with balanced library ownership and the source-observed cleanup/error timing.

All 35 cases per CPU pass (105 total): artifacts/qualification-copy-startup-allocation/qualification.json. The earlier missing-DOS-only matrix also passed in artifacts/qualification-copy-missing-dos. Workbench startup, DOS_RDARGS allocation failure, normal COPY/MOVE/LINK composition and original-system/shipping qualification remain open. These are supplied-vector runs through the normal command ABI. No shipping gate closes.

### 2026-09-05 — Original Copy Workbench rejection order

Corrected the normal command entry's Workbench path to FindTask/CLI check, WaitPort, Forbid, GetMsg, ReplyMsg, then RETURN_FAIL. The generic startup helper retrieved the message before Forbid, which did not match this command's original source. COPY now performs its own source-specific sequence without opening DOS or allocating state. Other commands retain their existing startup helper.

Added normal-entry Workbench runtime coverage and narrowed the shared fixture's alternate ordering rule to the Copy command selector. Checks enforce zero library opens/closes, allocations or parser calls, one wait/get/reply and final Forbid/GetMsg/ReplyMsg ordering. All 36 cases per CPU pass (108 total): artifacts/qualification-copy-workbench-startup/qualification.json. DOS_RDARGS allocation failure, full normal COPY/MOVE/LINK runtime composition and original-system/packaging/purity qualification remain open. No shipping gate closes.

### 2026-09-05 — DOS_RDARGS allocation failure through normal entry

Added a failed DOS_RDARGS allocation case with NoFreeStore supplied by DOS. The normal command returns FAIL, reports that error before releasing its sole workspace, never calls ReadArgs/FreeArgs/FreeDosObject, and performs no file operation. The test provides no synthetic RDArgs allocation in this case, so an accidental use/free cannot be hidden by fixture storage. Cleanup leaves its own final IoErr as in the existing whole-entry contract.

All 37 cases per CPU pass (111 total): artifacts/qualification-copy-parser-allocation/qualification.json. This completes the currently enumerated normal-entry startup allocation cases, not all command failure behavior or shipping qualification. Normal COPY/MOVE/LINK target composition, wider option/recursive/error coverage, original-system comparisons, packaging and pure/resident acceptance remain open. No shipping gate closes.

### 2026-09-05 — Normal single-file COPY/MOVE/LINK lifecycle

Connected the single-target behavior matrix to NativeMorphOSCopyCommandEntry using a +single-command selector. It supplies ReadArgs input into the command-owned cleared workspace, retains a real fixture-owned RDArgs object until FreeArgs/FreeDosObject, and checks actual command return values/final IoErr rather than private state output. Existing target classification, payload, lock, MOVE rename/fallback, hard-link/soft-file rejection and failure cleanup assertions remain active. Added protection-metadata ABI checks and parser release after the final target unlock, followed by final workspace/cache cleanup.

Ten cases pass per CPU (30 total): artifacts/single-command-68000.json and corresponding 68020/68040 reports, using the hash-bound command HUNKs in artifacts/qualification-copy-parser-allocation. The standard command lifecycle driver now includes this matrix (47 total per CPU on its next complete run). These supplied-DOS cases exercise single literal files, quiet mode and classic source examination; recursive/multiple-source targets, quoting transformations, streams, broader metadata/options and original-runtime/purity/packaging acceptance remain open. No shipping gate closes.

### 2026-09-05 — Normal directory-target COPY composition

Connected the existing directory-target/wildcard traversal fixture to the normal command entry using a +directory-command selector. The command parses into its own workspace, classifies/creates the destination, routes a source pattern, performs pending file operations, releases the target, and only then frees parser state and final workspace/cache. Pointer checks now account for command-owned classifier and path locations while retaining payload, match-step ordering, root lock and cleanup assertions. Cases cover zero/one/two matched files and interleaving; target-pattern rejection cases remain in their prior private fixture.

The combined standard driver passes 52 invocations per CPU (156 total): DELETE ten, MAKEDIR nine, DIRECT/startup eighteen, single-file COPY/MOVE/LINK ten and directory-target COPY five. Receipt: artifacts/qualification-copy-normal-targets/qualification.json. These are normal-ABI supplied-DOS runs. Multiple source patterns, recursive directories, stream targets/sources, transformed quoting, visible output and original-system/purity/packaging acceptance remain open. No shipping gate closes.

### 2026-09-05 — Normal command destination rejection

Included wildcard and invalid destination patterns in the normal directory-target command suite. Both now verify actual command return levels (ERROR for wildcard destination, FAIL for parse failure), target-sized ParsePattern allocation, zero source classification/destination locks, and balanced parser/workspace cleanup. These were previously covered only through the private target-dispatch entry. Quiet behavior is retained; visible diagnostics remain a separate requirement.

The complete normal-entry driver passes 54 cases per CPU (162 total): artifacts/qualification-copy-target-rejection/qualification.json. Multiple source patterns, recursive directories, streams, quoting transformations, visible output and original-system/purity/packaging qualification remain open. No shipping gate closes.

### 2026-09-05 — Quoted source normalization through normal command

Added a source containing a DOS tick escape (SYS:'file0) to the normal single-file COPY matrix. The supplied ParsePattern callback produces SYS:file0; assertions require source classification to receive the original spelling, the normalization allocation to use the original source length, the parser-owned source string to be rewritten in place, and subsequent locks/file operations to use the normalized name. Existing payload, six-lock cleanup and parser-lifetime assertions remain active.

All 55 cases per CPU pass (165 total): artifacts/qualification-copy-quoted-source/qualification.json. This verifies command composition around supplied DOS parsing; it does not independently establish real DOS escape grammar. Failed normalization/lookup, more complex names, multiple/recursive sources, streams, visible output and original-system/purity/packaging acceptance remain open. No shipping gate closes.

### 2026-09-05 — Non-filesystem source through normal COPY

Added NET:file0 as a supplied non-filesystem source to the normal directory-target command matrix. The case verifies classification before IsFileSystem, restoration of the temporarily split device prefix, copying into the command-owned source path, FilePart routing, no filesystem traversal allocation/MatchEnd/source locks, direct DOS payload transfer, and destination/parser/cache cleanup. The fixture now distinguishes the stream's buffer allocation from a filesystem AnchorPath allocation.

All 56 cases per CPU pass (168 total): artifacts/qualification-copy-stream-source/qualification.json. The handler is simulated; actual stream handler integration, non-filesystem destinations, stream failure/cancellation, multiple/recursive sources and original-system/purity/packaging acceptance remain open. No shipping gate closes.

### 2026-09-05 — Multiple source patterns through normal COPY

Added two distinct FROM patterns (SYS:#? and SYS:other#?) to the normal directory-target command matrix. Each supplies one file. Assertions enforce completion of the previous pending copy before the next source classification, a separate matcher lifetime for each pattern, destination path-cache rebuilding per source, preservation of the shared root target lock and reuse of the same transfer-buffer address. The allocation matrix permits the second matcher, not a second transfer buffer. Parser data remains live until all work finishes.

All 57 cases per CPU pass (171 total): artifacts/qualification-copy-multiple-sources/qualification.json. Strengthened buffer-address reuse assertions were rerun on each CPU. Multiple-source failure/cancellation, recursive directories, real handlers, visible output and original-system/purity/packaging acceptance remain open. No shipping gate closes.

### 2026-09-05 — Multiple-source write failure and ERRWARN

Added a write failure on the first of two source patterns through the normal command entry. The default case removes the partial first output after closing its handles, then classifies/transfers the second source with the retained cache and returns WARN. The ERRWARN case skips the second source entirely and returns ERROR. Assertions distinguish the number of processed sources from the input list, check one partial-output deletion, and retain per-source matcher, root lock, cache reuse and parser-lifetime checks.

All 59 cases per CPU pass (177 total): artifacts/qualification-copy-source-failures/qualification.json. Source cancellation and other failure stages, recursive directories, real handlers, visible output and original-system/purity/packaging acceptance remain open. These supplied-DOS normal-entry cases do not close a shipping gate.

### 2026-09-05 — Cancellation between normal COPY sources

Added Ctrl-C becoming pending after the first of two source patterns completes, with and without ERRWARN. Both cases prevent second-source classification, retain the successfully copied first output, release the destination/parser/cache, and perform final cancellation selection after parser teardown. Checks require both the source-loop cancellation observation and the final cancellation query. The command returns WARN normally and ERROR with ERRWARN; no partial-output deletion occurs.

All 61 cases per CPU pass (183 total): artifacts/qualification-copy-between-source-cancel/qualification.json. Mid-transfer and recursive cancellation, real handlers, visible output and original-system/purity/packaging acceptance remain open. No shipping gate closes.

### 2026-09-05 — Visible normal COPY failure reporting

Added a non-QUIET output-open failure through the normal single-file command. The case verifies the additional pre-admission source classification, exact command-owned prefix " not opened for output: ", the PrintFault call using IoErr after VPrintf (deliberately changed by the fixture), and parser liveness during reporting. It rejects duplicate final fault output and the quiet-only metadata tail. The supplied DOS fault renderer emits a marker, so the operating system's localized fault wording is not qualified.

All 62 cases per CPU pass (186 total): artifacts/qualification-copy-visible-failure/qualification.json. Successful verbose output, other diagnostics, recursive behavior and original-system/purity/packaging acceptance remain open. No shipping gate closes.

### 2026-09-05 — Visible input, write and soft-link failures

Extended the normal single-file visible-output matrix to input-open failure, write failure and rejected soft-file linking. Checks require the operation-specific copied./linked. diagnostic prefix, post-prefix IoErr passed to PrintFault, one diagnostic only, no quiet metadata tail, parser liveness and the existing per-path lock/partial-output cleanup. DOS-rendered fault text remains a fixture marker rather than original localized output.

All 65 cases per CPU pass (195 total): artifacts/qualification-copy-visible-failures/qualification.json. Successful verbose output, recursive paths and original-system/purity/packaging qualification remain open. No shipping gate closes.

### 2026-09-05 — Successful verbose normal COPY output

Added a non-QUIET wildcard COPY success case through the normal command entry. Checks cover the extra pre-admission wildcard classification selecting verbosity, exact directory/file indentation and text, creation and copied messages, caller-owned formatting storage, parser liveness and both name flushes before file opens. The expected bytes are "        RAM: (Dir)   [created]\n   file0..copied.\n". No synthetic fault renderer is involved in this successful-output case.

All 66 cases per CPU pass (198 total): artifacts/qualification-copy-verbose-success/qualification.json. Recursive indentation/output, broader operation combinations and original-system/purity/packaging acceptance remain open. No shipping gate closes.

### 2026-09-05 — Verbose empty-match notice

Added the normal-command verbose wildcard case with no matched files. It checks exact destination-creation output followed by "No file was processed.\n", one name flush, no transfer allocation, and notice ordering after matcher/root-lock cleanup but before parser release. This exercises the captured argument-verbosity policy in the complete command lifecycle.

All 67 cases per CPU pass (201 total): artifacts/qualification-copy-empty-notice/qualification.json. Recursive traversal/output and original-system/purity/packaging qualification remain open. No shipping gate closes.

### 2026-09-05 — Classic target-dispatch examination size fields

Corrected classic Examine handling in target dispatch to zero-extend fib_Size and fib_NumBlocks into the MorphOS 64-bit FIB extension fields, as the original _genExamine does, including after a failed Examine call. The native single-target fixture poisons extended high words and supplies classic values with bit 31 set; checks require zero high words and unchanged unsigned low words before releasing the examined source lock. This prevents stale extended sizes and accidental sign extension.

All 67 cases per CPU pass (201 total): artifacts/qualification-copy-classic-size-extension/qualification.json. The new runtime assertion covers successful source Examine in target dispatch; failed Examine and other examination call sites need their own coverage. Recursive end-to-end behavior and original-system/purity/packaging qualification remain open. No shipping gate closes.

### 2026-09-05 — Classic destination examination parity

Applied original _genExamine size/block zero-extension to TestDest's classic Examine path, including examination failure. Added DOS v40 existing-directory success and examination-failure cases through the normal command. The fixture poisons extended high words, supplies unsigned classic values with bit 31 set, and verifies conversion before FIB release, together with existing flag clearing and lock cleanup assertions.

All 69 cases per CPU pass (207 total): artifacts/qualification-copy-classic-destination/qualification.json. Recursive behavior and original-system/purity/packaging qualification remain open. No shipping gate closes.

### 2026-09-05 — Classic file-handle examination extension fields

Completed the classic ExamineFH conversion in the transfer helper: populate size/block 64-bit extension fields by zero-extending the classic ULONGs before the buffer is reused for payload. The DIRECT fixture poisons extended high words and supplies a block count with bit 31 set, then verifies the conversion at the first Read. Extended DOS-version cases retain their existing 64-bit call path.

All 69 cases per CPU pass (207 total): artifacts/qualification-copy-classic-handle/qualification.json. This complements the target-dispatch and destination classic-examination fixes. Failed handle examination and wider transfer edge cases still require their own receipts; recursive and original-system/purity/packaging qualification remain open. No shipping gate closes.


### 2026-09-05 — Unknown source size in DIRECT transfer

Added a normal-command DIRECT case where classic ExamineFH fails after leaving size fields populated. The transfer must ignore that apparent size, read the payload and then EOF, perform the original zero-length EOF Write, and close both handles without deleting output. The fixture also verifies unsigned extension-field conversion following failed examination. This passed without a production change.

All 70 cases per CPU pass (210 total): artifacts/qualification-copy-unknown-size/qualification.json. These are supplied-DOS native execution receipts, not original-system or pure/resident shipping qualification. Recursive end-to-end coverage and full command acceptance remain open.


### 2026-09-05 — DIRECT read failure and premature EOF

Compared the original CopyFile transfer loop and added normal-command cases for Read returning -1 and an examined eight-byte source ending after four bytes. The read-error case forbids Write; premature EOF requires the four-byte payload Write followed by the original zero-length Write and a failure result. Both require output/input close ordering and no DIRECT partial-output deletion. Existing production behavior passed unchanged.

The complete supplied-DOS matrix passes 72 invocations per CPU, 216 total, on 68000/020/040: artifacts/qualification-copy-transfer-errors/qualification.json. These checks do not close reference-runtime, recursive, purity or packaging gates.


### 2026-09-05 — Extended examination transfer failure matrix

Ran unknown-size fallback, Read failure, premature EOF and Write failure through the normal DIRECT command with dos.library 51.66. The supplied vectors require ExamineFH64 and the original PosixDate tag list, reject classic examination at this version, and retain handle, payload, result, parser-lifetime and resource checks. Failed extended examination now has explicit coverage independent of classic FIB conversion. Production code passed unchanged.

All 76 invocations per CPU pass, 228 total on 68000/020/040: artifacts/qualification-copy-extended-transfer-errors/qualification.json. This closes these four version-path coverage gaps only; actual MorphOS/Workbench runtime, recursive behavior, purity and packaging remain unqualified.


### 2026-09-05 — Private COPY roots own extended-examination scratch

Audited all ExtendedExamine assignments. The normal command already supplies its 16-byte workspace slice, but six private roots (Work, TraversalWork, Source, DirectorySources, TargetDispatch and PatternDelete) omitted ExamineTags. Updated each to allocate invocation-owned 16-byte scratch only when extended examination is selected, fail cleanly if allocation fails, and free it after operation cleanup. This prevents those roots from writing tag data through a null pointer when an existing destination is examined. Classic fixtures incur no new allocation.

The root project builds without warnings; all six changed roots compile as compatible 68000 HUNKs under artifacts/qualification-copy-private-tags. This is build/static evidence only. Extended-mode fixture allocation accounting and runtime coverage must be updated before treating these private roots as runtime-qualified; previous private-root artifacts do not prove this revision. The normal command implementation is unchanged.


### 2026-09-05 — Private-root classic execution regression

Executed the newly rebuilt 68000 roots from qualification-copy-private-tags using their existing supplied-DOS suites: TargetDispatch 7, Source/source-routing 8, Work 16, TraversalWork 5 and DirectorySources 5 invocations. All 41 pass with unchanged shared images and no leaked resources. Reports are the corresponding *.runtime.json files in that artifact directory. An initial invocation used the nonexistent selector copy-source-native-entry-vector-fixture; rerunning with the registered source-routing selector passed.

PatternDelete has no registered dedicated runtime suite or qualification script in the current tree, so its new revision remains build/static-only. Extended examination scratch allocation and allocation-failure paths are not exercised by these classic cases. Next validation must cover those paths explicitly rather than infer their success from these receipts.


### 2026-09-05 — Extended worker scratch ownership runtime

Added an extended-mode COPY case to the private Work suite. It requires the new 16-byte scratch allocation before the 512-byte transfer buffer, ExamineFH64 with the expected PosixDate tags, no classic ExamineFH call, and both allocations released. All 17 Work invocations pass on the rebuilt 68000 root: artifacts/qualification-copy-private-tags/Work-extended.runtime.json. This exercises successful scratch ownership; existing-destination use of that scratch and allocation failure remain separate open cases. No shipping gate closes.


### 2026-09-05 — Extended worker scratch allocation failure

Added a private Work case returning NULL for its first 16-byte extended-examination scratch allocation. It requires RETURN_FAIL / ERROR_NO_FREE_STORE, exactly one allocation attempt, no FreeMem, no file operations or lock cleanup, and untouched worker result slots. Standard fixture checks also require balanced library startup/cleanup. All 18 Work cases pass on 68000: artifacts/qualification-copy-private-tags/Work-allocation.runtime.json. This validates the private root's allocation-failure path, not the normal command or other roots' full extended destination behavior.


### 2026-09-05 — Reproducible worker scratch CPU matrix

Updated qualify_copy_work_native.ps1 to require the current 18-case suite and describe the extended scratch cases. The compiler still reports 21 reachable methods, so the existing static assertion remains unchanged. Rebuilt the compiler, root and runner, then compiled/executed 68000/020/040. All 54 supplied-DOS invocations pass, with no shared-image writes or resource leaks: artifacts/qualification-copy-work-scratch-matrix/qualification.json. Successful and failed scratch allocation now have evidence on all three CPUs. Existing-destination tag use remains open; this private-worker receipt is not full command or shipping qualification.


### 2026-09-05 — Existing destination uses extended worker scratch

Added a private Work DONTOVERWRITE case with an existing file. It reaches Examine64 using the allocated tag scratch and a separately owned DOS FIB, checks PosixDate tags, releases the destination lock before the FIB, preserves the destination (no opens/deletes), and releases source locks and scratch. The case preserves the current quiet worker result/DONE behavior rather than assuming an error code from DONTOVERWRITE alone.

All 19 cases per CPU pass (57 total) on 68000/020/040 through qualify_copy_work_native.ps1: artifacts/qualification-copy-work-existing-extended/qualification.json. This directly exercises the repaired destination tag-storage path. Full normal-command option/reference/purity/packaging gates remain open.


### 2026-09-05 — Extended destination examination failure cleanup

Added failed Examine64 to the private Work existing-destination matrix. The failure case requires the DOS FIB to be freed before the destination lock is released (the successful examination case requires the opposite order), then checks source-lock and scratch cleanup with no output opens or deletes. All 20 cases per CPU pass, 60 total on 68000/020/040: artifacts/qualification-copy-work-destination-failure/qualification.json. Production behavior passed unchanged. This is private-worker supplied-DOS evidence, not full reference-system or shipping qualification.


### 2026-09-05 — Destination FIB allocation failure

Added a private Work existing-destination case in which AllocDosObject(FIB) fails. The fixture requires one FIB allocation attempt, no examination, no FIB free, no output open/delete, all three source/parent/destination lock releases and release of the extended scratch buffer. Existing success/failure examination cases now also require exactly one FIB allocation. All 21 cases per CPU pass (63 total) on 68000/020/040: artifacts/qualification-copy-work-fib-allocation/qualification.json. Production code passed unchanged. The private-worker checks do not establish original-system or shipping qualification.


### 2026-09-05 — Reference audit of quiet destination errors and metadata

Re-read MorphOS copy.c SHA256 13be3cb51223c726aa13abbbac28e9a4545a0a36ec32621262c7bda27a69808f: DoWork initializes printok to a non-null empty string (1667 onward); a failed file TestDest sets printerr without changing RetVal (1990 onward); QUIET bypasses PrintNotDone and enters the printok tail, setting DONE and calling SetData (2075 onward). TestDest (2304 onward) confirms the distinct FIB/lock cleanup orders and ObjectExists handling. This explains the existing private-worker result assertions; they are source-backed behavior, not a desired-policy change.

Strengthened all three quiet existing-destination cases (DONTOVERWRITE, examination failure, FIB allocation failure) to enable protection metadata and require one SetProtection on RAM:entry despite no copy. All 63 worker executions pass across 68000/020/040: artifacts/qualification-copy-work-quiet-metadata/qualification.json. These tests cover the protection part of the metadata tail only; date/comment and original runtime comparison remain open.


### 2026-09-05 — Quiet-error date/comment metadata tail

Extended the three existing-destination worker cases to require protection, classic date and comment metadata in that order, with source FIB date/comment values and destination pathname checked. Date/comment calls deliberately return failure; the worker must retain the original result and complete cleanup. Corrected the metadata helper documentation: its caller can select SetData after QUIET errors, not only after successful copies.

All 63 supplied-DOS worker executions pass on 68000/020/040: artifacts/qualification-copy-work-quiet-metadata-tail/qualification.json. This covers classic-date metadata in these quiet-error paths; Posix-date variants, full command combinations and reference-runtime/shipping gates remain open.


### 2026-09-05 — POSIX-date quiet-error metadata variants

Added POSIX-date source-FIB variants of all three quiet existing-destination cases. They require SetFilePosixDate with the source date pointer and NULL tags, forbid the classic date API, and verify protection/date/comment ordering even when date/comment updates fail. Existing classic variants remain in the suite. All 24 cases per CPU pass (72 total), with resource and shared-image checks, on 68000/020/040: artifacts/qualification-copy-work-posix-metadata/qualification.json. This is worker-level supplied-DOS evidence only; full normal-command/reference/purity/packaging acceptance remains open.


### 2026-09-05 — Normal command regression after worker metadata coverage

Rebuilt and reran qualify_copy_command_lifecycle_native.ps1 after the shared fixture changes. All 76 invocations per CPU (228 total) pass across 68000/020/040: artifacts/qualification-copy-command-after-worker-metadata/qualification.json. This revalidates the current normal-command parser, DIRECT, DELETE, MAKEDIR, single-target and directory-target supplied-DOS matrices together; the private worker's new metadata scenarios are not thereby promoted to normal-command coverage.

The worker scratch repair and its focused validation are now complete for Work. Next implementation priority returns to normal-command recursive traversal: actual directory-entry/exit sequences, destination descent/ascent and metadata on exit. Re-read NativeMorphOSCopyDirectoryEntry confirms the first-directory special branch and deferred later-directory work need explicit normal-command coverage. CC40–CC44 and shipping totals remain unchanged.

### 2026-09-05 - Recursive source-order audit

Compared original PatCopy lines 1226-1392 with Traversal, MatchStep, DirectoryEntry and DirectoryExit. Recorded the reference-derived recursive acceptance trace in contracts/Copy-recursive-trace.md, including literal first-directory handling, deferred work/depth ordering and conditional parent-failure propagation. No production discrepancy was established in these transitions. Normal-command recursive runtime evidence remains missing; the next fixture must exercise sibling directory descent/ascent, not just flat-file counts.

### 2026-09-05 - Recursive matcher fixture input

Added CopyRecursiveTrace.cs with six reference-derived records for two sibling directories, each containing a child file and a DIDDIR exit. The record writer replaces the live FIB and pathname on every step, and the trace records expected completed-file counts before MatchNext. This is the matcher-input foundation for the normal-command recursive fixture; it is not yet wired to the command suite, so no recursive runtime pass is claimed. The runner builds successfully with the same three existing nullable warnings. Next work is the recursive DOS handler/lock model and suite wiring.

### 2026-09-05 - Recursive fixture filesystem model

Added CopyRecursiveFileSystem.cs for invocation-owned fixture state: distinct lock identities per acquisition, parent lookup, CurrentDir restoration, separate source payloads, destination directory creation and final sibling-output comparison. It rejects duplicate/unknown lock releases, releasing the active CurrentDir, and output into a nonexistent parent. The runner builds with the same three existing warnings. This model is not yet connected to native DOS callbacks; no runtime pass is claimed. Remaining work is callback integration, matcher-owned locks and normal-command case registration.

### 2026-09-05 - Recursive DOS file and lock callbacks

Added CopyRecursiveFileDos.cs with separate recursive fixture callbacks for lock acquisition/release, ParentDir, CurrentDir, SameDevice, directory creation, destination naming, and actual byte-stream Open/Read/Write/Close operations. Output data is committed to the modeled destination on close and input completion is counted independently. Invocation state is attached to the traversal layout but not initialized/registered yet. The runner builds; the compiler additionally reports the intentionally not-yet-assigned recursive state field. Matcher/parser/destination-FIB callbacks and suite wiring remain unfinished, so no recursive execution result is claimed.

### 2026-09-05 - Recursive matcher callbacks

Added separate MatchFirst/MatchNext/MatchEnd callbacks for the sibling-directory trace. They check classifier flags, traversal break bits, deferred payload completion before advancement, DODIR requests, DIDDIR consumption, and matcher-owned source-lock release at MatchEnd. Matcher chain storage uses the invocation's fixture control area. The runner builds; recursive callbacks are still awaiting parser/destination integration and suite registration, so this remains implementation progress without runtime acceptance.

### 2026-09-05 - Normal-command recursive sibling COPY execution

Connected the recursive file/matcher models through a dedicated +recursive-command selector, normal command ABI, owned ReadArgs parser, target FIB examination and ALL/QUIET options. The normal COPY HUNK successfully creates two sibling directories and copies their distinct child payloads, consumes DODIR/DIDDIR, restores CurrentDir, releases matcher/source/destination locks and closes all file handles. The six-record traversal exceeded the old 200,000-instruction flat-fixture cap; this selector has a bounded 1,000,000-instruction cap while existing selectors retain their limit.

One recursive invocation passes on each CPU (68000/020/040), using HUNKs from qualification-copy-command-after-worker-metadata. Reports: artifacts/recursive-first.json, recursive-68020.json and recursive-68040.json. These are supplied-DOS results, not original-system or purity qualification. Dedicated script integration, repeated/concurrent recursive invocations, richer metadata assertions, literal syntax, MOVE/DELETE and failure branches remain open.

### 2026-09-05 - Recursive interleaving and script integration

Added the recursive selector to qualify_copy_command_lifecycle_native.ps1 and expanded its suite to one sequential plus two interleaved invocations sharing the native image. All 79 cases per CPU pass (237 total) on 68000/020/040, including nine recursive executions: artifacts/qualification-copy-recursive-interleaved/qualification.json. Each recursive invocation owns separate parser, modeled filesystem, locks and handles, and verifies both sibling payloads and cleanup. This establishes supplied-vector interleaving only, not real-OS same-SegList purity. Recursive failure/mode coverage and original-system/packaging acceptance remain open.

### 2026-09-05 - Recursive protection metadata ordering

Strengthened recursive metadata assertions to require exactly four protection updates: first child, first directory, second child, second directory. Each must occur after its child payload completes and before parser release; directory metadata requires CurrentDir naming the destination parent. The final verifier rejects missing updates. All 237 normal-command invocations pass across 68000/020/040, including nine recursive runs: artifacts/qualification-copy-recursive-metadata-order/qualification.json. Date/comment, recursive failure/mode coverage and original-system/purity acceptance remain open.

### 2026-09-05 - Recursive MOVE cross-device fallback

Added a normal-command recursive MOVE case. Rename returns failure for the modeled SYS:-to-RAM: cross-device operation, requiring child payload copying and source-file deletion followed by directory deletion at DIDDIR. The filesystem model rejects nonempty directory deletion and verifies exact source deletion order: first child, first directory, second child, second directory. Destination payloads and all lock/current-directory ownership remain checked.

Four recursive invocations pass per CPU (12 total) on 68000/020/040 using qualification-copy-recursive-interleaved HUNKs. Reports: artifacts/recursive-move.json and recursive-move-{68020,68040}.json. Updated the aggregate driver to expect four recursive cases/80 total per CPU; the expanded full aggregate has not yet been rerun. DELETE, failures and original-system/purity acceptance remain open.

### 2026-09-05 - Full COPY matrix including recursive MOVE

Rebuilt the normal command and ran the expanded aggregate: all 80 invocations per CPU pass (240 total), including 12 recursive executions, on 68000/020/040. Receipt: artifacts/qualification-copy-recursive-move-full/qualification.json. Updated the recursive trace status and future driver scope wording to acknowledge the tested COPY/MOVE subset without claiming all recursion is qualified. Recursive DELETE, literal first-directory syntax, failure cases, original runtime and shipping gates remain open.

### 2026-09-05 - Recursive CLONE saved metadata

Added CLONE to the normal recursive command suite. Each matcher record supplies a distinct date/comment; assertions require the child snapshots and directory-exit snapshots (records 1,2,4,5), with protection/date/comment order and correct resolved destination paths. All five recursive invocations pass on each CPU (15 total), using qualification-copy-recursive-move-full HUNKs: artifacts/recursive-clone-{68000,68020,68040}.json. Updated the driver to five recursive cases/81 total per CPU; that expanded aggregate has not yet run. Full option/failure/reference/purity qualification remains open.

### 2026-09-05 - Recursive CLONE metadata failures

Added recursive CLONE with failing date/comment setters that also set IoErr. Copy still must complete both payloads, process all four metadata tails in order, restore CurrentDir and release every resource with the normal final command result. All 82 cases per CPU pass (246 total), including 18 recursive executions, on 68000/020/040: artifacts/qualification-copy-recursive-clone-errors/qualification.json. This also reruns the prior CLONE success case in the full aggregate. Other recursive failures, DELETE/literal syntax and original-system/purity/packaging gates remain open.

### 2026-09-05 - Normal recursive DELETE

Added DELETE ALL through the normal parser/command entry using the sibling matcher trace. The case has no TO, destination creation or transfer allocation, and requires child-before-directory deletion, no destination files, no live handles/locks, restored CurrentDir and parser cleanup. The fixture preserves the original quiet file-metadata tail targeting the empty destination name. Seven recursive invocations pass per CPU (21 total) on 68000/020/040 using qualification-copy-recursive-clone-errors HUNKs; reports are artifacts/recursive-delete.json and recursive-delete-{68020,68040}.json. The aggregate driver now expects seven recursive/83 total cases per CPU; that expanded aggregate has not yet run. Literal first-directory syntax and failure/reference/purity gates remain open.

### 2026-09-05 - Mixed recursive-mode interleaving

Added shared-image interleaving of COPY with DELETE and MOVE with CLONE. Each invocation retains independent arguments, filesystem, metadata counters and ownership checks. Rebuilt and ran the full aggregate: all 87 cases per CPU pass (261 total), including 33 recursive executions, on 68000/020/040. Receipt: artifacts/qualification-copy-recursive-mixed-modes/qualification.json. This also brings recursive DELETE into the full aggregate. Supplied-DOS interleaving does not replace real-OS pure/resident testing; literal syntax, recursive failures and final acceptance remain open.

### 2026-09-05 - Recursive nonzero protection flags

Replaced zero source protection in recursive matcher records with ARCHIVE, PURE, SCRIPT and record-specific low bits. Metadata callbacks now require archive clearing while preserving pure/script and the correct saved record bits; DELETE's original empty-target metadata tail checks the child bits too. All 33 recursive executions pass across 68000/020/040 using qualification-copy-recursive-mixed-modes HUNKs: artifacts/recursive-protection-{68000,68020,68040}.json. This checks copied file metadata, not executable pure/resident qualification, which remains open.

### 2026-09-05 - Recursive NOPRO and PROX

Added normal recursive NOPRO and PROX cases with nonzero source protections. NOPRO forbids all protection updates while copying both payloads; PROX preserves the original default PROTECTION precedence. The initial fixture accidentally swapped their argument slots; corrected against the exact template (NOPRO index 8, PROX index 9) before accepting results. All 39 recursive executions pass on 68000/020/040: artifacts/recursive-protection-options-{68000,68020,68040}.json using qualification-copy-recursive-mixed-modes HUNKs. Updated driver counts to 13 recursive/89 total per CPU; expanded aggregate not yet rerun. No shipping gate closes.

### 2026-09-05 - Recursive protection full aggregate and gap reconciliation

Rebuilt and ran all 89 cases per CPU: 267 pass on 68000/020/040, including 39 recursive executions. Receipt: artifacts/qualification-copy-recursive-protection-full/qualification.json. Reconciled Copy-recursive-trace.md with current mode/metadata coverage and explicitly separated nested depth, literal syntax, matcher failures, real parser and OS/purity/image gaps. Next recursive work should target an untested traversal branch rather than repeat sibling success. Shipping totals remain unchanged.

### 2026-09-05 - Nested recursive COPY descent/ascent

Added an eight-record nested trace: first/deep/child, exits from deep and first, then second/child and its exit. The filesystem expects three created directories, correct nested payloads, five ordered protection updates, restored CurrentDir and no remaining lock identities. Matcher chain locks now follow each record's actual source parent, so nested soft-link entry probes resolve in the proper directory. All 42 recursive executions pass on 68000/020/040 using qualification-copy-recursive-protection-full HUNKs: artifacts/recursive-nested-{68000,68020,68040}.json. Driver now expects 14 recursive/90 total cases per CPU; expanded aggregate not yet rerun. Literal syntax and traversal failure/reference/purity gates remain open.

### 2026-09-05 - Nested MOVE exit ordering

Added the nested trace to normal-command MOVE fallback. The model requires deletion in the order first/deep/child, first/deep, first, second/child, second, while retaining both correct destination payloads and five ordered metadata updates. Introduced a separate Nested fixture property so nesting can combine with command modes rather than replacing them. All 45 recursive executions pass on 68000/020/040 using qualification-copy-recursive-protection-full HUNKs: artifacts/recursive-nested-move-{68000,68020,68040}.json. Driver expects 15 recursive/91 total cases per CPU; expanded aggregate remains pending. Full reference/purity/shipping acceptance remains open.

### 2026-09-05 - Nested DELETE

Added nested DELETE through the normal command entry. It removes first/deep/child before deep and first, then removes the second subtree; the model requires no destination creation/files, no transfer handles, restored CurrentDir and no lock leaks. DELETE metadata expectations now derive child record indices from the selected trace rather than assuming sibling indices. All 48 recursive executions pass on 68000/020/040 using qualification-copy-recursive-protection-full HUNKs: artifacts/recursive-nested-delete-{68000,68020,68040}.json. Driver expects 16 recursive/92 total cases per CPU; expanded aggregate remains pending. Literal syntax, failures and full reference/purity acceptance remain open.

### 2026-09-05 - Nested CLONE and full nested-mode aggregate

Added nested CLONE and generalized date/comment assertions to derive the five saved metadata records from the selected trace. Each child and directory exit must use its own snapshot and preserve protection/date/comment order. Rebuilt and ran the full aggregate: all 93 cases per CPU pass (279 total), including 51 recursive executions, on 68000/020/040. Receipt: artifacts/qualification-copy-nested-modes-full/qualification.json. This incorporates nested COPY/MOVE/DELETE and earlier protection option additions into the current aggregate. Literal syntax, traversal failures and original-system/purity/shipping gates remain open.

### 2026-09-05 - Literal-directory fixture in progress

Added a literal SYS: source trace with explicit first-directory and final exit records, a non-wild classifier result and distinct record metadata. The current 68000 run fails: MatchEnd is reached at record index 2 of 8 before any child completes. The diagnostic now records matcher index, completed payload count and anchor identity. Existing cases preceding this added case execute, but no expanded suite pass is claimed and aggregate expected counts have not been advanced. Next work must diagnose the literal trace versus command first-directory state transition. The runner builds; the new registered case intentionally remains failing until resolved.

### 2026-09-05 - Literal-directory early-exit diagnosis

Expanded the failing MatchEnd diagnostic with the last 35 DOS/Exec events. The literal case performs a soft-link CurrentDir/Lock probe on the first record, then executes source/root work before reaching record 2; this is inconsistent with the expected first-directory suppression. The fixture does supply a non-wild classifier flag and the source implementation compares classifier result to zero. No production fix is justified yet: the fault must be localized between classifier result preservation, traversal first-state passing and fixture record interpretation.

Generated artifacts/copy-literal-diagnostic.asm from the current root/compiler with symbols enabled to support native tracing. The literal case remains failing and is not counted as qualified; previous passing aggregate receipts remain historical evidence for their earlier fixture revision.

### 2026-09-05 - Literal-directory compiler argument-home fix

Confirmed the literal first-directory failure in generated code: a true Boolean parameter was initialized with MOVE.L into its four-byte home, while its escaped managed address read the high byte. Fixed CopperSharp68k allocated argument-home initialization to store the managed byte/word width; incoming stack values use the low byte/word of the big-endian ABI slot. Narrow argument reads now use the authoritative home after a by-reference mutation rather than the original incoming SSA value. No COPY source semantics were changed.

Added Boolean register/stack argument regressions, checking true and false plus readback after mutation, across three CPUs and two optimizer modes. Before the fix, all 12 new cases failed and the 18 existing cases passed (artifacts/compiler-argument-home/argument-home-before.trx). After the fix, 29/30 pass; optimized 68020 BooleanStackHome stops on Copper68k.UnsupportedM68kTimingException for opcode 0x1EAF at 0x00010046 (artifacts/compiler-argument-home/argument-home-after.trx). This is an unresolved execution-coverage limitation, not a passing test. All six new optimizer-disabled cases pass. Further compiler validation must cover byte/word signedness and this optimizer timing path; do not declare the compiler regression suite fully green.

Rebuilt the compiler CLI and normal command HUNKs. The literal SYS: trace now completes all eight records without changing its expected semantics. Full command aggregate passes 94 invocations per CPU, 282 total, including 54 recursive invocations on 68000/020/040; no shared-image writes or resource leaks. Receipt: artifacts/qualification-copy-literal-home-fix/qualification.json. Driver now expects 18 recursive cases. This supersedes the failing-literal checkpoint only; original-system equivalence, traversal failure coverage, pure/resident acceptance, packaging and all full-scope completion gates remain open.

### 2026-09-05 - Narrow argument-home signedness regression coverage

Extended CopperSharp68k ArgumentHomeMutationTests with byte/sbyte and ushort/short register and stack parameter cases. They inspect original values through escaped references, mutate them, then require direct parameter readback with correct sign/zero extension and intact neighbouring arguments. All 12 added executions pass across 68000/020/040 and fixed-point/disabled optimization. Current focused result is 41/42 passed (artifacts/compiler-argument-home/argument-home-narrow.trx); the sole unresolved case remains optimized 68020 BooleanStackHome's unsupported exact timing for opcode 0x1EAF. No timing exception was suppressed or counted as success.

### 2026-09-05 - Normal recursive matcher terminal errors

Re-read MorphOS copy.c's PatCopy error tail: MatchEnd precedes an unconditional source prefix and saved-error PrintFault, followed by SetIoErr and RETURN_FAIL in the secondary result. Added normal-command cases for terminal MatchNext ObjectNotFound and Break after sibling traversal. Both require diagnostics despite QUIET, no duplicate final fault, original error passed to PrintFault despite prefix output clobbering IoErr, immediate restoration despite PrintFault clobbering IoErr, and parser lifetime through diagnostics. Existing payload, metadata, lock, handle and shared-image assertions remain active. COPY production code passed unchanged.

Full rebuilt aggregate passes 96 invocations per CPU, 288 total, including 60 recursive executions on 68000/020/040. Receipt: artifacts/qualification-copy-terminal-errors/qualification.json. Driver expects 20 recursive cases. This covers matcher errors after the final directory exit only; early traversal errors, pending work at failure, parent-lock failure, original-system equivalence, pure/resident qualification and shipping remain open. No full command or goal gate is closed by these fixture results.

### 2026-09-05 - Pending-file traversal completion and failures

Added normal-command early MatchNext ObjectNotFound and Break with the first child snapshot pending and an owned destination subdirectory still active. Re-read original PatCopy's MatchEnd/error/deferred-work tail and DoWork's RetVal2 guard at copy.c 1667 onward. The failure cases require no file Open, payload, metadata or source mutation; diagnostics still preserve IoErr and occur before parser teardown, and the transient destination lock is released. Added matching ERROR_NO_MORE_ENTRIES at the same boundary: it must copy exactly the first saved child after MatchEnd, preserve its protection metadata and leave the second subtree untouched. No COPY implementation changes were needed.

All 23 recursive cases pass on each CPU. Full rebuilt aggregate passes 99 invocations per CPU, 297 total on 68000/020/040, with no leaked resources or shared-image writes: artifacts/qualification-copy-pending-completion/qualification.json. Driver expectations updated only after the new cases passed. Updated the recursive acceptance table to reflect previously verified nested/literal coverage. Parent failure, dangling links, further interruption boundaries, actual OS/ReadArgs equivalence, pure/resident and shipping gates remain open; this does not close CC13 or the full goal.

### 2026-09-05 - Parent-directory failure during COPY ascent

Added normal-command ParentDir failure at the first directory exit after its child payload has completed. Three source-backed cases distinguish nonzero IoErr (ObjectNotFound), zero IoErr (InvalidLock fallback), and immediate MatchNext NoMoreEntries (no promoted failure). Re-read original PatCopy parentdirerr loop guard and DIDDIR continue: it still calls MatchNext, suppresses directory metadata and only reports the parent failure if another iteration is admitted. Tests require exactly one copied child, untouched second subtree, no directory metadata, restored CurrentDir, no live locks, and normal parser teardown. The two diagnosed cases retain the saved-error output checks; the end-of-enumeration case returns OK without diagnostics. COPY implementation passed unchanged.

All 26 recursive cases pass on each CPU. Full rebuilt aggregate passes 102 invocations per CPU, 306 total on 68000/020/040, with no resource leaks or shared-image writes. Receipt: artifacts/qualification-copy-parent-failure/qualification.json. Updated the driver and recursive trace evidence table. Parent failure in MOVE/DELETE, dangling links, remaining option/OS compatibility, pure/resident and shipping qualification remain open. No complete command or full-goal gate was closed.

### 2026-09-05 - Normal recursive soft-link probe fallback

Re-read and hash-verified the original PatCopy USE_SOFTLINKCHECK branch (copy.c SHA256 13be3cb51223c726aa13abbbac28e9a4545a0a36ec32621262c7bda27a69808f). Added normal-command transient relative Lock failures at the first directory: InvalidLock must avoid device lookup, while ObjectNotFound with GetDeviceProc failure must use the empty relative device name and null previous DevProc. Both remain enterable and subsequently copy both directory payloads. The callbacks require matcher-parent CurrentDir context and restoration of the original IoErr before restoring CurrentDir, even when GetDeviceProc changes it. No ReadLink or FreeDeviceProc is permitted without a device result. Existing full output/metadata/resource assertions remain active. Production COPY passed unchanged.

All 28 recursive cases pass on each CPU. Full rebuilt aggregate passes 104 invocations per CPU, 312 total on 68000/020/040, with no resource leaks or shared-image writes: artifacts/qualification-copy-probe-failure/qualification.json. Driver and recursive evidence table updated. Positive/negative ReadLink, link-buffer allocation failure and dangling-link traversal through the normal entry remain open, as do actual OS, pure/resident and shipping qualification. No full command gate is closed.

### 2026-09-05 - Normal recursive ReadLink fallback

Added negative and zero ReadLink results through normal-command recursive COPY. The probe receives a device object with a port and matcher-parent lock, allocates a 512-byte public buffer, and calls ReadLink with the relative first-directory name and 511-byte limit. Both results permit descent and preserve both copied payloads and metadata. The fixture requires buffer cleanup before FreeDeviceProc, one release, and restoration of the original ObjectNotFound before CurrentDir despite both DOS calls and FreeMem changing IoErr. Added a bounded allocation path for the extra link buffer and later transfer cache; sizes, flags, request tracking, count and cleanup remain checked. Production code passed unchanged.

All 30 recursive cases pass on each CPU. Full rebuilt aggregate passes 106 invocations per CPU, 318 total on 68000/020/040, with no leaked resources or shared-image writes: artifacts/qualification-copy-readlink-fallback/qualification.json. Driver and recursive evidence table updated. Positive ReadLink/dangling-link traversal and link-buffer allocation failure remain open in the normal entry, along with remaining command/OS, pure/resident and shipping gates. No full-scope gate was closed.

### 2026-09-05 - Normal recursive link-buffer allocation failure

Added failure of the 512-byte link-probe allocation after a successful GetDeviceProc. ReadLink must not run, the device must be released without a buffer free, and ObjectNotFound must be restored before CurrentDir despite allocation/device cleanup changing IoErr. Normal directory descent then allocates the separate transfer cache and copies both sibling payloads. The fixture counts five allocation attempts and four successful allocations/frees, while retaining independent bus ownership checks. Renamed the fixture predicate to RecursiveHasLinkDevice so it does not imply ReadLink runs after an allocation failure. COPY production code passed unchanged.

All 31 recursive cases pass on each CPU. Full rebuilt aggregate passes 107 invocations per CPU, 321 total on 68000/020/040, with no leaks or shared-image writes: artifacts/qualification-copy-link-buffer-failure/qualification.json. Driver and recursive evidence table updated. Positive ReadLink/dangling-link traversal through the normal entry remains open; original OS/ReadArgs equivalence, remaining options, pure/resident and shipping gates are also still open. No full command gate closed.

### 2026-09-05 - Normal recursive dangling-link skip under QUIET

Added a positive ReadLink result for the first directory, followed by a valid second directory. The matcher requires DODIR to remain clear for the dangling entry and supplies no child/exit records for it. The filesystem has no lockable first target; it requires exactly one deferred absolute source Lock after the probe is released, which produces WARN. Traversal must then copy the second directory's distinct payload and file/directory metadata, without creating RAM:first. Buffer/device cleanup, saved IoErr, parser lifetime, restored CurrentDir, lock ownership and no QUIET diagnostic remain checked. COPY production code passed unchanged.

All 32 recursive cases pass on each CPU. Full rebuilt aggregate passes 108 invocations per CPU, 324 total on 68000/020/040, with no leaks or shared-image writes: artifacts/qualification-copy-dangling/qualification.json. Updated driver and recursive evidence table. Visible dangling-link warning, ERRWARN behavior and original filesystem link comparison remain open, as do remaining option, OS/ReadArgs, pure/resident and shipping gates. No full command gate closed.

### 2026-09-05 - Dangling-link ERRWARN stop

Added ERRWARN to the normal-command QUIET dangling-link scenario using ReadArgs slot 13. After the deferred source Lock produces WARN, MatchNext still supplies the next record, but no further loop or deferred worker may create a destination or copy a payload. The test requires RETURN_ERROR (10), exactly two MatchNext calls, no output directory/files/metadata, unchanged later source, and complete matcher/device/buffer/parser cleanup. It counts four successful allocations because no transfer cache is needed. The paired case without ERRWARN still copies the later directory and returns WARN. Production implementation passed unchanged.

All 33 recursive cases pass on each CPU. Full rebuilt aggregate passes 109 invocations per CPU, 327 total on 68000/020/040, with no leaks or shared-image writes: artifacts/qualification-copy-dangling-errwarn/qualification.json. Driver and recursive evidence table updated. Visible warning and original filesystem comparison remain open, as do remaining options, actual OS/ReadArgs, pure/resident and shipping qualification. No full command or goal gate closed.

### 2026-09-05 - Visible dangling-link warning with ERRWARN

Added a normal-command visible dangling-link ERRWARN case. It exercises the startup verbosity classifier in addition to traversal classification. The ReadLink fixture poisons buffer byte 511; the warning requires that byte to be terminated while the 512-byte allocation and device are still live, with exact first/missing arguments and invocation-owned VPrintf storage. The deferred absolute source failure prints the original ' not read.: ' prefix followed by one fault. Prefix output deliberately changes IoErr to 901, and PrintFault must receive that post-prefix value, matching PrintNotDone. No duplicate final fault or destination work is allowed. QUIET cases now explicitly verify absence of output and their single classifier invocation. Production COPY passed unchanged.

All 34 recursive cases pass on each CPU. Full rebuilt aggregate passes 110 invocations per CPU, 330 total on 68000/020/040, with no leaks or shared-image writes: artifacts/qualification-copy-dangling-visible/qualification.json. Updated driver and recursive evidence table. Visible continuation after the warning, real filesystem link comparison, remaining options and actual OS/ReadArgs, pure/resident and shipping gates remain open. No full command gate closed.

### 2026-09-05 - Visible continuation after dangling-link warning

Added the visible dangling-link case without ERRWARN. After the warning and deferred source-read fault, normal COPY must create the second directory, copy its child, print exact indentation/name/creation/copy text, and retain WARN. Output callbacks require the two progress-name flushes before file handles are opened, with no payload completed at either flush, and parser ownership throughout output. The paired visible ERRWARN case still prints only the warning/fault and stops. Existing payload, metadata, buffer/device, matcher and shared-image ownership checks remain active. Production code passed unchanged.

All 35 recursive cases pass on each CPU. Full rebuilt aggregate passes 111 invocations per CPU, 333 total on 68000/020/040, with no resource leaks or shared-image writes: artifacts/qualification-copy-visible-continue/qualification.json. Updated driver and recursive evidence table. Real filesystem link behavior, remaining mode/options, actual OS/ReadArgs equivalence, pure/resident and shipping qualification remain open; no full command gate closed.

### 2026-09-05 - Original DOS observations for the full Copy template

Reviewed CC03/CC04/CC13 integration gaps and added a separate LicensedCopyReadArgs test at the existing MedPlayer native reference owner. The old 44-case corpus is unchanged. The new corpus uses the exact current Copy template (checked equal: 275 characters, 24 slots), with empty/positional/multiple/quoted inputs, short and long aliases, mode/metadata switches, negative/invalid/overflow BUFFER and a repeat after failure. Shared fixture capacity checks now admit 96 bytes of slots and 511 template characters within existing nonoverlapping storage; guard bytes, native-vector checks, value copying before FreeArgs, cursor/IoErr observations and instruction limits are unchanged.

The shared build could not copy dependencies because unrelated testhost PID 30644 held its output. Stopped only our build attempt and rebuilt all dependencies under artifacts/copy-original-readargs/isolated-build. The unrelated test process was not interrupted. The isolated licensed original-DOS run passes one test, zero failures/skips, with 12 original observations and zero generated-parser comparisons or command invocations. TRX: artifacts/copy-original-readargs/copy-original-readargs-isolated.trx. Typed observations and source/test/runtime hashes: artifacts/copy-original-readargs/original-copy-observations.json. This uses the existing licensed Exec bootstrap and its documented overlays; it is not normal boot, stream, RunCommand or MorphOS execution.

Observed facts: bare positional source/destination words both remain in FROM/M and TO is null; empty input succeeds with empty slots; short/long option aliases publish corresponding slots; BUFFER many fails with IoErr 115 and no FreeArgs; BUFFER 2147483648 succeeds with raw number 0x80000000; successful cases retain the initialized IoErr sentinel 0x13572468 through cleanup. These are DOS parsing results, not Copy's later admission/normalization results. Compare the generated DOS/native command against these observations next. No reference answer was substituted for native execution, and no CC04/CC13 or shipping gate is closed. Existing 333 command fixture checks remain a separate historical receipt.

### 2026-09-05 - Full Copy template exposes DOS length rejection; owner fix

The native differential using the previously accepted DOS parser snapshot now compares the full 275-character, 24-slot Copy template. The unchanged 44-case baseline passes, but all 12 Copy cases return BadTemplate (114) at cursor zero in generated DOS. Original DOS produces the previously captured parsing observations. Evidence: artifacts/copy-original-readargs/copy-readargs-differential.trx. This is an actual template rejection, not a command worker failure.

Changed live CopperStart DosCore only at the three ReadArgs template checks (public counting, entry parsing, and ReadArgsFromBuffer admission): use the existing CStringLength mapped-memory/address-wrap scanner instead of filename-oriented 255-character validation. Filename checks are untouched. The scanner retains its existing MaximumIoLength bound; this change does not establish arbitrary-length or all-template compatibility. Added host tests at 255/256/275/512 characters, adapter admission, and an unterminated template reaching unmapped memory. Focused ReadArgs/adapter tests pass 56/56, including the original 44 cases; broader DosCoreTests pass 30/30. Receipts: artifacts/readargs-long-template/long-template.trx and dos-core-regression.trx.

The live native owner build fails before emitting a new candidate: C68K0009, CopperStart.Exec.CopperSharpRomMemoryPlatform::.ctor matched zero declarations. No live-native success is claimed. An isolated candidate is being rebuilt from the accepted parser snapshot with only the three scanner replacements, preserving the accepted snapshot unchanged. Its path is recorded in artifacts/readargs-long-template/candidate-root.txt. This isolates parser behavior and does not qualify the full current live DOS. Native comparison and full command/OS/pure/resident/shipping gates remain open pending further evidence.

The first isolated long-template build passed all owner compilation/manifest checks (HUNK B7D9B204EDE4B92EC0EFED7F16B3E979CFF9AE5E6457B1CA4B98E4D8C2D978E9). Native execution retained 44/44 baseline parity but timed out on generated copy-empty within the unchanged instruction budget: zero generated Copy observations qualify. Evidence: artifacts/readargs-long-template/native-comparison.trx and first-native-result.json. The 12 original observations completed again.

Removed repeated whole-template scans from TryReadReadArgsEntry: TryCountReadArgsTemplate and TryFindReadArgsEntryByIndex each validate the mapped template once per traversal and pass the local length to entry decoding. No static cache, allocation, or relaxed instruction limit was introduced. The revised live host ReadArgs and DosCore selection passes 80/80, no skips (long-template-single-scan.trx). A separate source-bound candidate, recorded in scan-candidate-root.txt, preserves the timeout candidate and accepted baseline unchanged. Its native result is still pending at this log entry.

The second native candidate (CD325A1F26969EB418B77ACE442168105457AF6651643C7C7E398071BA87E0B5) passes the 44-case baseline and returns the matching Copy empty-input observation, but times out on copy-positional. Its corpus gate is FAILED, not partial qualification. Evidence: native-single-scan-comparison.trx and second-native-result.json under artifacts/readargs-long-template.

Changed TryFindNextReadArgsEntry and TryFindReadArgsKeyword to traverse entries sequentially instead of restarting decoding from entry zero for each index. Keyword token length is validated once per lookup and supplied to name comparison. No shared state or relaxed bounds. Live host ReadArgs/DosCore tests pass 80/80 again (long-template-keyword-scan.trx). Third isolated owner build and manifest validation pass; HUNK 795A7FE63EB5087BCDFA759362896303A6577C7D2DBA035DE9BFCF15C4191686, candidate path in keyword-candidate-root.txt. Native baseline remains 44/44, but Copy still times out on positional input after returning its empty-input observation (native-keyword-scan-comparison.trx).

Added read-only progress diagnostics to the MedPlayer native ReadArgs fixture, every 100,000 instructions and on failure; instruction budget, native-vector checks and comparisons are unchanged. Corrected diagnostic instance/static wiring after two compilation failures, then rebuilt successfully into a separate progress-test-build directory so earlier test binaries remain intact. The diagnostic run reproduces the timeout (native-keyword-progress.trx): source cursor is 20 from 100,000 instructions onward (the final LF is at offset 20), CPU is not halted, and sampled PCs fall inside CStringLength and TryReadReadArgsEntry. At one million instructions PC=0026D442. This supports investigating the remaining repeated entry scans, including required-argument backfill and final validation; it does not prove an infinite loop or justify a larger budget. No runtime or command gate is closed. The full live native build constructor-resolution error, normal boot, MorphOS runtime, pure/resident and shipping work remain open. All build/test sessions from this step are terminal.

### 2026-09-05 - Full Copy ReadArgs corpus passes native comparison

Revalidated the previous timeout evidence and removed repeated indexed lookups from final required-argument validation. Backfill now makes one sequential pass to locate the last missing eligible required positional string, then retains its original right-to-left assignment order. This avoids quadratic work when no such string exists without changing backfill priority. Live host ReadArgs/DosCore tests pass 80/80. The first isolated candidate (F490A05871FB99D6009ACDFE7680D359CC112B93EE0DED120F1CD03F84F501B5) passes the old 44-case baseline and returns four Copy observations identical to the original; the alias-heavy fifth case still times out. Receipt: artifacts/readargs-required-scan/native-required-scan.trx.

Removed the remaining indexed traversal in TryReadArgsFullFollows: one validated template cursor now walks the entries, ignoring entries through currentIndex and preserving the existing /F, switch, assignment, and early-return decisions. Host ReadArgs/DosCore tests remain 80/80; DosCliCoreTests (including /F combinations) pass 28/28. Receipts: full-follows-scan.trx and cli-full-regression.trx in artifacts/readargs-required-scan. No shared state, cache, extra allocation, relaxed instruction budget or comparison waiver was introduced.

A separate isolated owner build and manifest validation pass for 68000 HUNK and 68020/040 assembly. The source-bound candidate is C:/D-drive/TestData/CopperOSCommands/BuildSnapshots/readargs-full-follows-20260905T134320Z; HUNK A1611E8F1D78555200AB581E92E7B148C25B59CEB79E4EA3283F92841710372A. Native tests now PASS both the unchanged 44-case baseline and all 12 full Copy-template cases: 56 exact original/generated pairs, zero failed/skipped tests. The one-million-instruction per-call limit is unchanged. Typed values, cursor, IoErr, FreeArgs/cleanup, selected native vectors, installer idempotence, loaded-image immutability and expunge checks remain active. Receipt and bound observations: artifacts/readargs-required-scan/qualification.json; TRX: native-full-follows.trx. This resolves the measured Copy template rejection and timeout on the isolated parser candidate.

Scope remains explicit CSource ReadArgs/FreeArgs under the existing licensed Exec bootstrap with documented overlays. No DOS parser host gateways were substituted. The candidate is the accepted baseline plus parser changes, not the entire live DOS closure. The live constructor-resolution build failure, normal command entry/streams/filesystems, full boot, MorphOS runtime, command pure/resident, remaining options and shipping gates remain open. No complete command or full-scope gate is closed; shipping-qualified command count remains zero. Next integrate the fixed parser with the full live native owner and real command execution. All sessions from this step are terminal.

### 2026-09-05 - Live DOS owner build and 56 native parser pairs pass

Reproduced the live C68K0009 constructor failure (artifacts/readargs-live-owner/build-before.log). Exec's copied and canonical assemblies have identical hashes. A reflection probe showed that CopperSharpRomMemoryPlatform is not resolved until CopperSharp.Sdk.Amiga.Support is loaded: the type implements interfaces whose dependency was supplied to the compiler but was not available to reflection beside Exec. The constructor itself is present; DOS constructor calls were not changed.

Changed CopperSharp CompilationModule.LoadExternalMethod to load explicitly supplied, transitively referenced managed dependencies before reflecting the declaration. A visited-assembly set bounds cycles; no extra filesystem probing or target-runtime code is introduced. Added tools/Commands/test_dos_reflection_dependency.ps1: fresh compiler processes build a struct constructor whose interface assembly is outside the input directory. The previous isolated compiler fails the declared-dependency case with the same missing Probe.Value::.ctor diagnostic. The corrected compiler passes that case and still rejects the undeclared dependency. Final receipt: artifacts/readargs-live-owner/reflection-regression-final/result.json. The 17 selected existing managed-assembly/cross-module compiler tests pass (managed-resolution.trx). No full compiler-suite claim is made.

All live native outputs then compiled, but the owner validator rejected RootMethodCount=3 because it retained a historical count of four. Verified the actual current profile and compiler analyzer: one SystemEntry root and two distinct explicitly included exports. The validator now derives the exact expected count as one plus the profile export count. Exact export identities, source/managed hashes, allowed assemblies, zero runtime helpers/features, zero managed allocation sites, zero exception/fault sites, and map checks remain active. A new full owner build passes all compilation and manifest validation stages; build-root-count.log records the result.

The unchanged strict native parser test executable now passes against the FULL LIVE DOS owner: 44 baseline plus 12 full Copy-template pairs, zero failures/skips, unchanged one-million-instruction limit, all typed/cursor/IoErr/cleanup comparisons and native-vector/image/installer/expunge checks active. HUNK A553D004C017FE56CB94DAD9A5D0AA9024251E133A34ACB15216AD06187297E3 (678636 bytes). Source closure has 179 files, SHA256 AC6AFFAF2E04FC7C1FBEF6762F1A74ACD263F3481FC826820B36EF0E463895EA. Evidence: artifacts/readargs-live-owner/live-native-parser.trx and qualification.json. Captured native outputs/maps/reports/manifests are preserved under qualified-inputs; their HUNK hash was checked against both native test runtime bindings. Live sources and managed build outputs are not frozen by this capture.

This resolves the live constructor-resolution and owner root-count blockers; it does not close CC04/CC13 or qualify a shipping command. Evidence still uses explicit CSource in the documented licensed Exec bootstrap, with no selected DOS parser host gateways. Normal streams, RunCommand/full command execution, filesystem behavior, full boot/AddDataTypes, MorphOS PPC reference execution, remaining options and command pure/resident qualification remain open. Shipping-qualified command count remains zero. All sessions from this step are terminal; continue with real command integration.

### 2026-09-05 - Copy normal entry runs through original DOS RunCommand

Revalidated CC03/CC04 and reused the existing MakeDir RunCommand/real Process/CLI/public NIL-stream path instead of creating another parser-only harness. Rebuilt the current Copy normal-entry HUNK and all native fixture dependencies. All 333 supplied-vector invocations pass again (111 per CPU, including 35 recursive cases per CPU), with no resource leaks or image writes. Receipt: artifacts/copy-original-runcommand/current-command/qualification.json. The 68000 HUNK is 24596 bytes, SHA256 BCF4467C7C7B8CB073E68A30FCF7C46B1C7B95D717B092696990FE89CBEB8E24; the resident static report retains 62 reachable methods and zero runtime helpers/features, managed allocation sites, exceptions/fault sites and external native targets.

Extended the existing command observer with an explicit optional profile for template/result-slot count, loaded code size and caller-owned RDArgs. The default MakeDir path retains its assertions. Copy requires the 24 cleared slots and an RDArgs object actually returned by original AllocDosObject, with zero CSource fields; its ExtendedHelp is set by the command. The host never supplies RDA_Source or enters the command directly. Added KickstartRomCopyCommandLaunchTests.cs with two generated Copy parser-failure cases and a separate original MakeDir observer regression. Input bindings are recorded in command-inputs.json and verified before and after execution; these are live file bindings captured after the build, not a frozen source snapshot.

Both tests pass, zero failures/skips. Generated Copy executes via original RunCommand: BUFFER many + LF returns 20 with IoErr/PrintFault 115 in 8393 instructions; an unterminated quoted alpha + LF returns 20 with IoErr/PrintFault 120 in 8479 instructions. Each invokes original ReadArgs once, never calls FreeArgs for a failed parse, and frees its one public RDArgs object and one direct Exec workspace allocation exactly once. The original DOS vectors, command image and argument bytes remain unchanged; Process/CLI identity, argument pointer, stack and selected input fields restore; borrowed streams are restored and only owned NIL handles close. The original MakeDir regression retains its empty/quote results (20/0 and 20/120). These four executions are not original/generated Copy comparisons.

Evidence: artifacts/copy-original-runcommand/copy-command-and-observer.trx and qualification.json, with the earlier successful Copy-only run retained as copy-command-launch.trx. This is real original-DOS normal-entry/input/cleanup evidence for two Copy errors, not successful file work, output-byte capture, DOS LoadSeg, MorphOS binary comparison, full boot, generated-DOS command execution, resident/P admission or shipping qualification. The host HUNK loader still owns public Exec segment allocations. Updated CP8 in completion-ledger.md to remove the obsolete combined-worker-static-only next step. Shipping remains 0/200 and all full-scope gates remain open. All sessions from this step are terminal.

### 2026-09-05 - DIRECT NIL execution identifies an ExamineFH wait

Extended the existing original-DOS RunCommand observer with an explicit successful-DIRECT profile and native ExamineFH/Read/Write entry observations. The Copy input is `FROM NIL: TO NIL: DIRECT QUIET BUFFER 1` plus LF. Production command code and its bound HUNK are unchanged. Expected success still requires return 0, successful ReadArgs/FreeArgs, no progress/fault output, two matching command allocations/frees, and all existing process/vector/image/stream restoration checks. Default MakeDir and Copy error assertions remain unchanged.

The expanded run FAILS the successful Copy case. Original ReadArgs succeeds, both command Open calls execute, and the command allocates its 4770-byte workspace and 512-byte transfer buffer. It then enters original ExamineFH (LVO -390, source handle 527355) and does not return within the unchanged one-million-instruction limit. Final PC is 00F81476, CPU stopped, no new exception; Read and Write are never reached. This narrows the prerequisite failure to examination of the NIL handle in this bootstrap, not parsing or transfer bytes. Do not infer whether this is original NIL semantics or incomplete bootstrap/packet readiness without a direct reference probe. No success, cleanup, or full filesystem qualification is claimed. Unsafe continuation correctly suppresses further guest cleanup calls and disposes the machine.

Both previous Copy error observations and both original MakeDir regression observations still pass. The expanded test remains visibly failing, not skipped or waived. Evidence: artifacts/copy-direct-nil/copy-direct-nil-observed.trx, deduplicated observations.json and source-bound result.json. Earlier first-attempt TRX is retained separately. Next isolate public ExamineFH(NIL:) and packet/handler readiness before changing Copy semantics; then resume full file-operation integration. Shipping remains 0/200; all full-scope gates remain open. Both build/test sessions are terminal.

### 2026-09-05 - ExamineFH wait reproduces without Copy or RunCommand

Added a separate LicensedDosNilExamineReadiness test using the existing public stream fixture. The original open/select/query/restore/close test delegates to the same unchanged path with examination disabled; the new path allocates a 512-byte examination buffer and invokes original ExamineFH directly on the original Open(NIL:, MODE_OLDFILE) handle. It neither loads a command nor calls ReadArgs or RunCommand. Corrected an initial helper-extraction compilation error before execution.

Native run: one pass (original stream lifecycle), one failure (examination). Direct ExamineFH does not reach its return sentinel within the existing bound, ending stopped at 00F81476, exactly as the Copy run. The returned NIL handle before examination has Port=0, Type=0, Position/End=FFFFFFFF and Argument1=0. These are observations of the original handle, not host-supplied replacements. No further guest cleanup is attempted after the suspended call. Receipts: artifacts/nil-examine-readiness/nil-examine.trx, observations.json, result.json; current source and artifacts are hash-bound. Build/test sessions are terminal.

This rules out Copy loading, generated argument parsing and RunCommand as necessary causes of this reproduced wait. It does not establish normal fully booted NIL semantics or prove the precise OS/bootstrap defect. Do not alter Copy to skip examination merely to pass this test. The next integration work should trace the original packet/wait path and repair real filesystem/bootstrap readiness; NIL currently cannot substitute for that dependency. Full Copy success, original MorphOS comparisons, pure/resident and shipping remain open, 0/200 shipping-qualified commands.

### 2026-09-05 - Original NIL examination sends to a null message port

Added read-only Exec PutMsg/GetMsg/WaitPort/Wait and DOS packet-vector observations to the isolated ExamineFH probe. It uses the same register initialization, sentinel, native instruction loop and one-million-instruction limit as the existing invocation helper, retaining nonvolatile register/stack checks on return. No gateway, handle or packet replacement is added. Stream lifecycle control passes; examination still fails visibly.

The trace now identifies the wait mechanism: original DOS reaches Exec PutMsg with A0=0 (destination port), A1=511568 (message), and D2=1034, then polls GetMsg on the process port at 2121532 and calls Wait with mask 256 twice. It finally remains stopped at 00F81476. The NIL handle observed previously has Type=0/Port=0. This is evidence of a null-destination message send followed by waiting for a reply in this bootstrap; it is not evidence that a valid filesystem handler received a packet and failed to respond. Neither Copy nor RunCommand is involved in this isolated reproduction.

Preserve this as a failed reference prerequisite, not a Copy correction or a passing transfer test. No original fully booted comparison has established whether this NIL behavior is universal. Do not spend further success-qualification work assuming NIL supports examination: return to real RAM/filesystem readiness and its LockDosList/boot prerequisite, while retaining this diagnostic. Evidence: artifacts/nil-examine-readiness/nil-examine-trace.trx, trace-observations.json and trace-result.json (source/artifact hashes). Tests: one passed, one failed, no skips. All sessions terminal. No command gate closes; shipping remains 0/200.

### 2026-09-05 - RAM list lock reaches semaphore address 20

Rebuilt and reran LicensedDosRamInputReadiness with added read-only A0 and mapped A0/A1 byte snapshots around existing Exec entry observations. Production code and native instruction bounds are unchanged. LockDosList(29) again fails before opening any RAM file. New evidence identifies its first Exec call as ObtainSemaphoreShared (-678), A0=A1=20 (0x14), followed by Wait(mask 16). Address 20 contains low-memory vector data, not a plausibly initialized DOS semaphore. No host gateways or new exceptions are observed during this call; the CPU ends stopped at 00F81476 after the unchanged million-instruction limit.

This narrows the RAM prerequisite from an unexplained lock wait to an invalid semaphore pointer passed by original DOS. It suggests incomplete/missing DOS state behind the list lock rather than contention by a functioning RAM handler; the exact upstream pointer remains to be traced. Next inspect original DOS RootNode/DosInfo initialization and the LockDosList pointer chain. Do not manufacture a semaphore at address 20, unlock it, or bypass list locking. Evidence: artifacts/ram-list-readiness/ram-list.trx, observations.json, result.json with source hashes. One failed test, no skips; no file created and no command invocation. Session terminal. Full boot, real filesystem/Copy success and shipping gates remain open, 0/200.

### 2026-09-05 - Missing rn_Info confirmed; RAM prerequisite now fails safely

Captured original DOS RootNode immediately before list locking: root address 2120444 is mapped, rn_Info BPTR=0. The invalid semaphore address is therefore consistent with null DosInfo plus DeviceLock offset 20. The existing BootstrapRootReadiness test already defines this prerequisite; this is current-state revalidation within the RAM fixture, not a newly discovered OS requirement. InitializeLicensedDos explicitly permits an unfinished resident initialization for component-only APIs, so library publication alone cannot qualify filesystem readiness.

Added a non-null, non-overflowing, mapped DosInfo prerequisite to the RAM fixture before LockDosList. The gate remains FAILED, not skipped or weakened: it now reports missing rn_Info instead of invoking a semaphore operation against low-memory vectors. The verification run fails at that explicit prerequisite and reports unsafeNativeContinuation=false with no cleanup errors, restoring borrowed state without an unsafe suspended guest call. Production DOS/Copy are unchanged. Retained pre-guard root capture in ram-root.trx/root-observations.json and guarded evidence in ram-root-preflight.trx/preflight-observations.json under artifacts/ram-list-readiness. The local root-build output was rebuilt for the guarded test; earlier TRX remains historical rather than a frozen binary snapshot.

Next use the existing real boot/startup investigation path (KickstartRomDosStartupAddressErrorTraceTests and related readiness tests), reviewing its historical binary bindings before execution. Do not keep treating the component-only direct InitResident harness as a fully initialized filesystem or synthesize rn_Info. All sessions terminal; full filesystem, Copy success, pure/resident, MorphOS and shipping gates remain open, 0/200.

### 2026-09-05 - Current full boot no longer matches historical AddDataTypes entry

Preserved the historical startup address-error test and its three fixed assembly hashes. Added a separate current-build category that verifies explicitly supplied hashes for the test, Exec, emulator and CPU assemblies before using the same original media hashes, machine profile, 14 x 250000 prefix instructions and 250000-boundary observation cap. No runtime, media, CPU state, observer limit or expected fault was changed. Built into artifacts/current-boot-trace/build. The initial binding incorrectly identified the Amiga core rather than the AmigaBootController-owning Emulator DLL and failed before boot; retained it as current-boot.trx. Corrected the binding using the actual Emulator DLL file hash, retained separately as runtime-bindings.json.

The bound current runtime boots far enough to pass mapped RootInfo, task-list consistency and unique Initial CLI checks. It then fails the historical structural entry expectation: current command is IF, not C:AddDataTypes, at the unchanged prefix. Thus the old AddDataTypes failure is not revalidated on this build; do not continue reporting it as an observed current fault or infer boot success. Evidence: current-runtime-boot.trx and runtime-run.log. This changes the next action to sampling current startup progress and locating its actual failure/readiness boundary, rather than patching the old presumed fault. Moved structural-entry capture before the expectation in source so future failed entry checks retain full state; this last diagnostic-order change is not included in the already captured test binary. Both test sessions and build are terminal. Shipping remains 0/200 and no full boot or command gate closes.

### 2026-09-05 - Current startup advances past AddDataTypes and EndCLI

Added a separate passive progress category using the existing 32 x 250000 boot bound. It continues sampling after root publication and records ready/wait lists and the Initial CLI process without issuing guest calls, input or CPU-state patches. The original root-readiness test still stops at its first supported root snapshot. Provider wording now identifies the actually hash-recorded current assembly rather than claiming the old frozen453 build.

The current run passes the bounded observation/root gate (one test, no failures/skips): RootInfo is mapped at chunk 10; Initial CLI reports C:AddBuffers at 11, Resident at 12, IF at 13-14, C:AddDataTypes at 15, C:LoadResource at 16, C:IPrefs at 17, Path at 18, EndCLI at 19-22. Initial CLI is absent from sampled task lists at 23-32; CPU is stopped at F81476. Last exception remains vector 8 at all these later snapshots, rather than the historical AddDataTypes address error. Sampled command names are observations, not proof of their outputs or exact invocation counts. Original disk remains write-protected and byte-identical; original DOS vector checks remain active after publication.

The historical AddDataTypes fault is no longer supported as a current blocker. Do not claim full desktop/Shell/handler correctness from this root/progress test. Next establish a real command process and filesystem interaction from this successfully advanced boot, preserving ongoing task state rather than substituting the incomplete direct InitResident context. Evidence: artifacts/current-boot-progress/boot-progress.trx, observations.json, result.json; runtime assembly hashes are recorded in observations and source/TRX hashes in the result. Build and test sessions terminal. No shipping command is qualified, 0/200; full command, pure/resident and MorphOS gates remain open.

### 2026-09-05 - Prepared disposable disk for normal Shell launch of generated Copy

Extended the existing startup fixture writer with explicit existing-file replacements bounded by their current allocated capacity. It rejects overlapping replacements and oversized payloads, recomputes header checksums, and reads back exact replacement hashes. The original archive is read-only. Found C/Ed has 25088 allocated bytes, sufficient for the 24596-byte generated Copy HUNK. On a private derivative only, C/Ed now contains that exact bound HUNK; this temporary name is a test carrier, not a shipping/install decision or pure/resident qualification.

Created private TestData copy-native-boot-20260905.adf plus artifacts/copy-boot-fixture/media.json. Startup creates RAM:copy-source, invokes the generated command through C:Ed with explicit FROM/TO DIRECT QUIET BUFFER 1, writes a success/failure marker according to shell return status, and waits. This is prepared media, NOT an executed or passing command test. Independent block comparison confirms no bytes outside startup/C/Ed allocations changed and directory paths remain identical (block-verification.json). No original media is placed in the repository. Next boot this derivative with an explicitly bound image receipt and observe command execution plus exact RAM payload/cleanup; do not count a marker alone as payload qualification. Full goal remains active, shipping 0/200.

### 2026-09-05 - Disposable Copy fixture boots through its script

Added a separately selected derivative-image path to the passive boot observer. The original archive/image hashes are verified first, then the explicit derivative receipt and actual image hash; original-media tests retain their image checks. The derivative is write-protected and compared against its own bound image after boot. No guest command call, process replacement, keyboard input or DOS state patch is performed.

The 32 x 250000 observation run passes its root/progress-only gate. RootInfo appears at chunk 10; Initial CLI is sampled in Echo at 11 and Wait at 12, then absent at 13-32. Last exception stays 8. The coarse snapshots do NOT capture C:Ed or prove Copy entry, return, marker contents or RAM payload. Do not infer Copy success from this result. Next add instruction/packet observations around the actual generated image and transfer, or a guest-side readback with independently observed output. The fixture startup currently contains host CRLF line endings; examine actual argument/result observations before attributing any behavior to them. Evidence: artifacts/copy-boot-fixture/copy-boot.trx, boot-observations.json, boot-result.json; Echo process snapshot retained separately. Build/test terminal. Shipping remains 0/200.

### 2026-09-05 - Generated Copy reaches real RAM transfer through original Shell

Attached a read-only retirement listener after DOS publication in the derivative-only boot path, chaining/restoring the prior listener. It captures selected original DOS entry-point calls, bounded input strings and Write buffers; observation cap is 512 calls. No DOS interception, CPU/register modification or native call-limit change. The same bound command/image/script boots under the original Shell.

Trace observes LoadSeg(C:Ed), RunCommand, the exact full Copy ReadArgs template, Open(RAM:copy-source), Open(RAM:copy-target), ExamineFH, Read(source,512), Write(target,25), and two Close calls. The Write buffer captured is 636F70792D6E61746976652D626F6F742D7061796C6F61640A. Original Shell subsequently opens RAM:copy-completed, with no copy-failed open observed, then runs Wait. This establishes command/real-handler progress beyond the incomplete direct-init harness and NIL examination failure. It is call-entry evidence only: return values, actual target readback, exact command-image attribution at each caller, allocator/resource lifecycle and resident admission still need assertions. No shipping or full Copy gate closes.

One bounded boot observation test passes, no skips/failures, no observation overflow reported. Evidence: artifacts/copy-boot-fixture/copy-dos-trace.trx, dos-observations.json, dos-result.json. The derivative disk remains byte-identical/write-protected. Next capture call returns and read back destination bytes through guest DOS, then verify resource and command-image invariants. Build/test terminal; shipping 0/200.

### 2026-09-05 - Copy returns OK after successful real RAM Read/Write and Close

Extended derivative-only DOS observation with return matching by return PC, restored stack and task identity. Capture return D0/IoErr and bounded Read bytes before subsequent guest instructions; flag duplicate pending keys instead of silently overwriting. Built a new private LF-only startup derivative, preserving prior media, adding original C:Type RAM:copy-target after the status branch. Same generated Copy HUNK and original Shell/handlers execute.

The run passes the bounded boot observation. Exact receipt post-check links LoadSeg(C:Ed) return 544657 to RunCommand(D1=544657), which returns 0. Copy Read returns 25 with expected payload, Write requests/returns 25 with identical bytes, both transfer handles Close successfully (-1) exactly once. No observation collision or overflow. Original Type is loaded, runs, opens copy-target and closes successfully, but it does not call the unbuffered Read vector captured here; destination bytes are NOT yet independently verified. Next observe its buffered input calls (FRead/FGetC) or use a guest readback helper; do not equate Type return 0 with exact output.

Evidence: artifacts/copy-boot-fixture/copy-readback.trx, readback-observations.json, readback-media.json and transfer-return-check.json. The startup newline change and added Type are explicit fixture changes, not production changes. No resource-allocation/pure-resident or full command gate closes; shipping 0/200. Build/test sessions terminal.

### 2026-09-05 - Original Type reads the expected destination bytes

Extended derivative-only capture with FGetC/FRead/FGets and FRead element-count-aware bounded byte capture. No disk, command or execution-limit change. One boot observation passes, 224 recorded calls with no overflow or return-key collision. Original Type uses FGetC, not FRead: after its LoadSeg and Open(copy-target), 25 returned characters exactly reconstruct copy-native-boot-payload plus LF, followed by successful Close(-1). Handle matching is restricted to that Open/Close interval to avoid recycled-handle confusion.

The first post-check incorrectly expected an explicit EOF FGetC and failed; Type performs no observed EOF read here. The corrected evidence check asserts exactly the 25 returned bytes and successful close and explicitly records eofObserved=false. This proves matching independently read destination payload bytes, not absence of additional trailing data. Next verify destination file size/EOF independently and make these receipt checks executable qualification assertions, together with command-image attribution and allocator/resident lifecycle. No full command gate closes. Evidence: artifacts/copy-boot-fixture/copy-buffered.trx, buffered-observations.json, destination-readback-check.json. Production unchanged; build/test terminal; shipping 0/200.

### 2026-09-05 - Repeatable Copy transfer evidence verifier

Added tools/Commands/verify_copy_boot_transfer.py to turn the prior ad-hoc receipt checks into an executable bounded evidence gate. It verifies scope, boot/media prerequisites, observation completeness, LoadSeg/RunCommand correlation and return 0, open modes, nonzero distinct handles, matching 25-byte Read/Write counts and payloads, task identity, successful unique transfer closes after Write, and exact original Type character readback within its own open/close interval. Output explicitly leaves destination length, allocations, pure/resident and full command qualification false; it does not authenticate arbitrary observations as a native run or replace source/media provenance checks.

The existing real captured observations pass and produce artifacts/copy-boot-fixture/verified-transfer.json with observation/verifier hashes. Added six corruption controls: failed write count, failed Copy return, trace overflow, return collision, altered transfer payload and removed destination character. Initial missing-character control selected Type argument parsing rather than destination input and was correctly accepted; fixed the control to match the reopened destination handle, then all six corruptions are rejected. Receipt: evidence-negative-controls.json. Production code unchanged, no new native run claimed. Next complete file-length and native ownership/image assertions. Shipping remains 0/200.

### 2026-09-05 - Destination length and complete payload verified

Captured original DOS Examine/ExNext/MatchFirst/MatchNext file information on successful return, using SDK FIB/AnchorPath offsets. The unchanged derivative boots successfully. Copy's source ExamineFH reports 25 bytes; original Type's MatchFirst returns success with filename copy-target and size 25 before reopening that file. The later 25 FGetC bytes match the entire expected payload. This independently establishes destination length without assuming Type makes an explicit EOF call.

Strengthened the reusable verifier to require the successful destination MatchFirst size/name in Type's load-to-open interval and the same task identity. It now sets destinationLengthQualified=true only after exact 25-byte length and existing transfer/readback checks pass. Added a trailing-data corruption control (size 26); all seven negative controls are rejected. Evidence: artifacts/copy-boot-fixture/copy-size.trx, size-observations.json, verified-sized-transfer.json, sized-negative-controls.json. Earlier limited receipts remain historical. Still no allocation lifecycle, loaded-image immutability, pure/resident admission, complete options or full command qualification. Build/test terminal; shipping 0/200.

### 2026-09-05 - Original LoadSeg image unchanged across successful Copy

The derivative boot observer now verifies the command HUNK against its replacement receipt and checks the admitted single-code-segment header before deriving the 19632-byte code span. At original LoadSeg(C:Ed) return it captures the actual loaded code (terminal segment link checked), then compares the complete span at the matched RunCommand return. No image bytes or loader state are patched. The load-to-return snapshot comparison passes; loaded image SHA256 3CFBDC75D9131F454B08355B0A0F94A27683E9C29D7D28895207C379487E592B. This is relocated runtime-image stability for this invocation, not an assertion of byte identity with the unrelocated HUNK or absence of transient writes.

The unchanged reusable transfer verifier also passes on this new trace: return 0, successful 25-byte transfer/handle closes, independently reported destination size 25 and exact original Type readback. Evidence: artifacts/copy-boot-fixture/copy-image.trx, image-observations.json, verified-image-transfer.json. One native boot observation test passed, no failures/skips. Allocation lifecycle and pure/resident admission/reuse still remain open. Next attribute direct allocation/free calls to the loaded image and check ownership across this same successful real-DOS path. Build/test terminal; no full command gate closes, shipping 0/200.

### 2026-09-05 - Copy direct allocations balance in real boot execution

Added Exec AllocMem/FreeMem observations filtered to callers inside the actual loaded Copy code while its RunCommand is active. Track allocated pointers/sizes on returned AllocMem, reject frees of unknown pointers or wrong sizes at entry, remove ownership only when FreeMem returns, and require exactly two matched allocations/frees and no outstanding allocations at Copy return. This does not treat FreeMem's D0 as a success code (the API is void).

Native run passes: workspace 4770 bytes at 2202368 and transfer buffer 512 bytes at 2177768; both free exactly once with matching sizes, buffer before workspace. The loaded-image return comparison and reusable exact-size/payload/handle-close verifier also pass. Evidence: artifacts/copy-boot-fixture/copy-ownership.trx, ownership-observations.json, verified-owned-transfer.json. The verifier's broader allocationLifecycleQualified flag remains false because this direct Exec check does not yet cover RDArgs/library/loader ownership or all failure paths. Pure/resident admission/reuse and transient image-write exclusion remain open. Build/test terminal; shipping 0/200.

### 2026-09-05 - Successful Copy RDArgs and library leases verified

Extended derivative trace with original AllocDosObject/FreeDosObject/FreeArgs and command-attributed Exec OpenLibrary/CloseLibrary. Native run passes. Copy opens DOS base 2147644 and later closes exactly that base. Original AllocDosObject(RdArgs=5,null tags) returns 2174388; ReadArgs receives that pointer as D3 and returns it; FreeArgs receives the same pointer and returns before FreeDosObject(5,same pointer). Both cleanup calls return before the library close. Void cleanup D0 values are recorded, not treated as success status.

The reusable verifier now asserts unique calls within Copy's library lease, pointer/type identities, return presence, task identity and allocation/parse/FreeArgs/FreeDosObject ordering. Existing exact payload/size/handle checks pass; seven prior corruption controls still reject invalid evidence. Evidence: artifacts/copy-boot-fixture/copy-parser-owner.trx, parser-owner-observations.json, verified-parser-owner.json, parser-owner-negative-controls.json. This qualifies these ownership observations for the successful invocation only; all-path allocation lifecycle, resident admission/reuse, transient code writes and full option coverage remain open. Build/test terminal; shipping 0/200.

### 2026-09-05 - Two successful executions reuse one resident Copy segment

Read original disk header flags: Ed and Copy both raw 0, P clear. Preserved fixture flags. Prepared an explicitly forced-admission Resident C:Ed PURE derivative; this is not normal P-bit admission or installed-metadata qualification. The script invokes Ed twice with separate RAM targets, requests Resident Ed REMOVE, then reads both targets with original Type. Observer now preserves one ownership row per RunCommand and resets direct allocation counters at each invocation while retaining the original loaded-image snapshot.

Native run passes. Only one LoadSeg(C:Ed), returning segment 545683; two RunCommand executions use that same segment and task, both return 0, each has two matched direct allocations/frees, and loaded image equals the initial snapshot at both returns. Both destinations independently report size 25 and read back the exact payload via original Type. No trace overflow/collision. Resident removal and final segment unloading still require direct lifecycle observation; the script request alone is not proof. Concurrency, transient image writes, failure/retry reuse, original MorphOS behavior and installed flags remain open.

Evidence: artifacts/copy-boot-fixture/resident-media.json, copy-resident.trx, resident-observations.json, resident-check.json. Prior single-run media/receipts unchanged. No full resident/command gate closes; shipping 0/200. Build/test terminal.

### 2026-09-05 - Original DOS reports resident registration and removal success

Added read-only AddSegment/FindSegment/RemSegment/UnLoadSeg observations to the unchanged forced-PURE two-run fixture. Native run passes with both same-segment ownership/image checks intact and no overflow/collision. Original AddSegment registers loaded segment 545683 with D3=1 and returns -1. FindSegment returns entry 2174388 for both executions and the removal lookup; RemSegment(2174388) subsequently returns -1. This is direct API-return evidence of registration/removal, beyond merely seeing the script request.

No public UnLoadSeg call for Copy's segment is observed; public unloads for the later Type commands are observed. Do not infer a leak or completed deallocation from this difference: original RemSegment can use an internal unload path. Next capture the removed entry's segment association before removal and InternalUnLoadSeg/free activity or post-removal lookup to close the lifecycle evidence. Exact source/command/fixture data remain as in the previous resident receipt. New evidence: artifacts/copy-boot-fixture/copy-removal.trx and removal-observations.json. Full resident qualification, transient-write exclusion, concurrency and failure/retry reuse remain open. Build/test terminal; shipping 0/200.

### 2026-09-05 - Resident Copy segment allocation is freed after removal

Captured RemSegment entry's public Segment fields and matched Exec FreeMem against the allocation header preceding the single loaded Copy segment. The original loader exposes allocation base 2182728 and size 19640 (19632 code bytes plus header). RemSegment entry 2174388 names segment 545683 with use count 1 and returns -1. One matching FreeMem(base 2182728,size 19640) is observed after removal entry and before the next Type load, and returns; void D0 is not interpreted as a status. No public/InternalUnLoadSeg vector is needed for this observed direct free path.

Both prior same-segment executions still return 0 with balanced direct allocations and unchanged code snapshots. No overflow/collision. A post-check links removal to the exact segment and size-matched allocation and rejects missing/duplicate frees. This establishes the segment allocation's release for this forced-PURE sequential scenario, not complete resident metadata, concurrency, transient image writes or error recovery. Evidence: artifacts/copy-boot-fixture/copy-unload.trx, unload-observations.json, unload-check.json. Build/test terminal; full gates remain open, shipping 0/200.

### 2026-09-05 - Resident Copy recovers after a real parser failure

Prepared a separate forced-PURE derivative inserting Ed BUFFER many between the two successful copies. Same command HUNK, original Shell/DOS and existing execution bounds. Observer distinguishes parser failure from successful parsing and expects only the workspace allocation on that failure path, preserving strict matching frees and initial image comparison for each invocation.

Native sequence uses one segment 545683: returns 0,20,0; direct allocation/free counts 2/2,1/1,2/2; image unchanged at each return. The middle original ReadArgs returns 0 with IoErr115, no FreeArgs is called, the exact allocated RDArgs object is freed, and no Open/Read/Write occurs in that command's library lease. The following invocation succeeds and original Type independently reads both exact 25-byte destinations. No overflow/collision. This verifies recovery from this parsing error, not allocation/I/O failure recovery or concurrent reuse.

Evidence: artifacts/copy-boot-fixture/recovery-media.json, copy-recovery.trx, recovery-observations.json, recovery-check.json. Build/test terminal. Full resident/command gates remain open; shipping 0/200.

### 2026-09-05 - No CPU code writes observed during resident recovery sequence

Added a chained CPU bus-phase listener scoped to the loaded Copy code after LoadSeg completes and before its segment FreeMem starts. It records writes whose bus address falls within the code span and fails the boot observation if any are recorded; it does not prevent or redirect writes. The existing retirement listener and phase listener are restored at teardown. Loader relocation and allocator free-list writes are outside this live-image interval by design.

The unchanged success/parser-error/success resident fixture passes with zero recorded CPU writes into the 19632-byte code image. All three initial-image comparisons still pass; returns 0,20,0 and allocation/free pairs 2/2,1/1,2/2 remain. No trace overflow/collision. This is scoped CPU bus observation, not proof about unobserved host/DMA writes or all command options/concurrent runs. Next strengthen observer coverage diagnostics and concurrent resident invocation testing, and consolidate the accumulated checks into durable resident qualification tooling. Evidence: artifacts/copy-boot-fixture/copy-write-guard.trx and write-guard-observations.json. Build/test terminal; full gates remain open, shipping 0/200.

### 2026-09-05 - Durable resident recovery verifier and ledger update

Added tools/Commands/verify_copy_resident_recovery.py to check the collected forced-PURE success/error/success evidence: one loaded/reused segment, per-invocation ownership/image results, original parser error115 cleanup, two exact-size destination readbacks, matching resident removal and returned segment free, and no recorded CPU image writes or trace incompleteness. Output explicitly leaves concurrency, installed flags and full command qualification false. This verifies captured observations; it does not replace native/media/source provenance or independently prove observer coverage.

The current write-guard observation passes; five corruption controls reject a changed segment, image write, missing segment free, failed recovery and wrong destination length. Receipts: verified-resident-recovery.json and resident-verifier-controls.json in artifacts/copy-boot-fixture. Updated CP8 in completion-ledger.md to replace the stale successful-filesystem-work next step with current evidence and actual remaining gaps. No new native run claimed. Shipping stays 0/200 and no full-scope checkbox is closed.

### 2026-09-05 - Resident recovery after real missing-source Open failure

Prepared a separate derivative replacing the middle parser error with a valid DIRECT Copy whose RAM:copy-missing source does not exist. The observer records command-attributed failed Open separately from failed parsing and retains the one-workspace/no-buffer expectation for early Open failure. Same loaded segment executes returns 0,20,0, direct allocation/free counts 2/2,1/1,2/2, and unchanged image at each return.

Middle original ReadArgs succeeds; original Open returns 0/IoErr205. It is the only Open in that invocation: target is not opened, no Read/Write/ExamineFH/Close is attempted. The successful parser result is released through FreeArgs, then the same RDArgs object through FreeDosObject. Following Copy succeeds; both original Type readbacks have exact 25-byte sizes/payloads. No CPU image writes or observation overflow/collision. Evidence: artifacts/copy-boot-fixture/open-failure-media.json, copy-open-failure.trx, open-failure-observations.json, open-failure-check.json. Test passes (2m42s), build/test terminal. Other I/O/allocation failures, concurrency and full qualification remain open; shipping 0/200.

### 2026-09-05 - Missing-source recovery becomes repeatable evidence gate

Extended the resident recovery verifier with an explicit missing-source mode while retaining parser-error mode as default. It now checks successful parsing, a unique source Open in old-file mode returning 0/205, no target/transfer/close after failed source open, exact FreeArgs pointer and FreeDosObject type/identity/ordering. Both historical parser recovery and current missing-source recovery observations pass under their respective modes, with source/verifier hashes in receipts. Four negative controls reject wrong IoErr, wrong open mode, wrong parser pointer and wrong object type. Evidence: verified-source-failure.json, reverified-parser-recovery.json and source-failure-controls.json under artifacts/copy-boot-fixture. No new native run claimed; full gates and concurrency remain open, shipping 0/200.

### 2026-09-05 - Destination-open failure preserves resident reuse and source ownership

Inspected the completed copy-target-failure.trx (one native test passed, 1m21s) and extracted target-failure-observations.json. The same resident segment executes success/failure/success with returns 0,20,0 and balanced direct allocation/free counts 2/2,1/1,2/2. Middle original ReadArgs succeeds, source Open(1005) succeeds, target Open(1006) into RAM:missing-dir fails with IoErr205. Copy closes the owned source exactly once successfully, performs no Read/Write/ExamineFH, releases FreeArgs then FreeDosObject and workspace/library ownership. Both subsequent original Type readbacks have exact 25-byte sizes/payloads; code snapshots remain unchanged and no CPU image writes are recorded.

Extended verify_copy_resident_recovery.py with missing-target-directory mode, requiring the two ordered opens, exact failed target/error, matching successful source Close before parser cleanup, and no transfer. All three real recovery traces (parser, missing source, missing target directory) pass. Five corrupted target traces are rejected: wrong close handle, failed close, wrong target error, wrong target mode and missing close. Receipts: artifacts/copy-boot-fixture/verified-target-failure.json and target-failure-controls.json. This is early destination-open failure coverage, not read/write failure, concurrent reuse, installed admission flags or full option qualification. No command production changes; shipping remains 0/200. Next resident milestone remains concurrent invocation and all-path ownership; do not treat forced PURE admission as original metadata qualification.

### 2026-09-05 - Copy observer tracks independent task ownership

Replaced the boot observer's single active-command flag and shared allocation counters with per-task invocation records. This removes the instrumentation's deliberate rejection of overlapping RunCommand calls while preserving exact per-invocation pointer/size matching, failure-specific allocation counts, and image comparisons. Added copyMaximumActiveInvocations and copyActiveInvocations diagnostics to distinguish actual overlap from merely launching multiple processes. Production Copy and emulator semantics are unchanged.

Built in artifacts/copy-boot-fixture/task-ownership-build (zero errors; one existing xUnit analyzer warning). Reran the unchanged destination-open-failure derivative: one native test passed in 1m32s. The durable missing-target-directory verifier passes on task-ownership-observations.json; maximum active count is 1 and final active count is 0, as expected for this sequential regression. Evidence: copy-task-ownership.trx, verified-task-ownership.json and task-ownership-test.log. No concurrent execution is claimed yet. Next use original Shell background execution and require measured overlap across distinct tasks sharing the same segment, including failure/success overlap. Shipping stays 0/200.

### 2026-09-05 - First concurrent fixture exposes setup failures, no overlap credited

Prepared a separate original-Shell Run background plus foreground Copy fixture using C:Ed disk input and separate RAM destinations. Initial test failed before boot because replacement receipt local_file was relative to the test runner directory. Fixed prepare_execute_fixture_adf.py to resolve replacement paths strictly to absolute paths before recording them; retained original failed receipt/TRX. Corrected receipt run completed its bounded boot observation (one test passed, 2m48s) but NOT command qualification: no Copy LoadSeg/invocations, only initial ROM RunCommand return20 and ReadArgs RCLIM/N failure. Thus no concurrency, transfer or resident evidence is credited.

Inspected startup bytes: new script had seven CRLF endings, while known passing target-failure script has LF only. Prepared startup-concurrent-lf.txt with exact LF bytes for the next run; the line-ending explanation remains to be verified by that run. Evidence: concurrent-test.log/copy-concurrent.trx (path preflight failure), concurrent-bound-media.json/copy-concurrent-bound.trx/concurrent-bound-observations.json (no Copy execution). All processes terminal. Command and emulator production unchanged; shipping0/200. Next prepare a unique LF derivative and rerun, requiring actual distinct-task overlap rather than the boot fact's pass status.

### 2026-09-05 - Actual same-segment overlap, source Close discrepancy remains

LF-only concurrent fixture executes correctly, unlike the CRLF predecessor: native observation passes in 33s. Two distinct tasks (2148712 and 2200288) overlap (maximum active2, final0) using one loaded segment545163. Both return0 with direct allocation/free counts2/2 and unchanged images; no recorded CPU image writes, overflow or return collision. This confirms the prior startup line-ending diagnosis for this fixture. Wait1 remains pending within the unchanged bound, so resident removal is not observed.

Added verify_copy_concurrent_transfer.py to require distinct-task overlap, one loaded segment, separate destinations and per-task exact complete read/write payloads, handle identities and successful closes. It rejects the current native trace: background task2200288's source Close(handle553597) returns0 with IoErr0, although its destination Close succeeds and Copy returns0. Foreground closes succeed. Payload checks preceding the rejected close establish exact24596-byte transfer calls for both tasks, not independent destination readback. NativeMorphOSCopyDirect.cs currently ignores source/output DOS.Close results; reference behavior and trace attribution must be checked before changing policy. Do not weaken the verifier or claim concurrent cleanup qualification.

Evidence: concurrent-lf-media.json, copy-concurrent-lf.trx, concurrent-lf-observations.json, concurrent-lf-test.log. Failed verifier is intentionally retained as a reproducible open case, with no passing receipt. Next localize the zero Close result across original DOS/observer/emulator and compare reference command cleanup policy; overlapping failure/success, removal, and independent readback still required. Production command/emulator unchanged. Shipping0/200.

### 2026-09-05 - Zero Close result confirmed at original DOS return

Added command-owned Close handle snapshots and bounded per-task last16 retired instruction capture to the test observer. Native rerun of unchanged LF concurrent disk fixture passes its boot assertions and reproduces zero source Close. Original DOS returns via RTS at16406666 to exact Copy caller2186314 with D0=0; two instructions earlier opcode0x2006 (MOVE.L D6,D0) at16406660 sets D0=0. Therefore the discrepancy is not simply a return record associated with an unrelated later command; D6's earlier origin/packet result remains untraced. The tail also contains a host service trap during cleanup, but that alone does not establish causation (DOS subsequently restores its result from D6).

Existing Copy contract's DIRECT runtime checkpoint already explicitly covers Close failures being ignored while transfer result is retained, matching NativeMorphOSCopyDirect.cs. Do not change production return policy without original-reference evidence. Preserve strict concurrent cleanup verifier failure while localizing DOS result provenance. Evidence: artifacts/copy-boot-fixture/close-tail-build, copy-close-tail.trx, close-tail-observations.json, close-return-localization.json. Next capture the earlier D6/handler reply path with bounded instrumentation. Command/emulator production unchanged; full gates remain open, shipping0/200.

### 2026-09-05 - Close zero originates at DOS packet result read

Expanded command Close tails to bounded128 retired instructions with D6/A0/A1 state. Unchanged concurrent fixture reproduces the zero result. Matched local licensed ROM disassembly shows the file close callback sends action0x3ef (1007), saves returnedD0 in D7 at0xfa577e, and returns D7; outer Close saves that result in D6 at0xfa586c and returns it after freeing the handle. Native trace reads zero at0xf9fa68 on the packet-call return path; all other three command closes read0xffffffff there. This localizes the discrepancy before handle free, not to the cleanup host trap's later D0 clobber.

The packet return read is still not proof of what the filesystem handler wrote or why. Need capture request/reply identity and result across handler delivery before claiming a handler bug, lost reply or shared state. Evidence: artifacts/copy-boot-fixture/copy-close-origin.trx, close-origin-observations.json, close-packet-result-localization.json, original-close-local-disassembly.txt (local licensed ROM excerpt), close-origin-build and logs. Command/emulator production and return policy remain unchanged. Strict concurrent verifier still rejects cleanup; full gates remain open, shipping0/200.

### 2026-09-05 - Original DF0 handler sends the zero close reply

Added bounded read-only PutMsg/ReplyMsg action1007 packet capture while Copy invocations are active. Validate mapped Message/DosPacket and reciprocal link before recording, with explicit overflow flag. Unchanged native concurrency fixture passes boot assertions and yields four matching request/reply pairs. Packet/message/argument identities and exchanged reply/destination ports match in every pair. Both RAM target replies and foreground DF0 source reply are0xffffffff/0. Background source request Argument1=2214496 receives0/0 from DF0 task2152152, already present at its PutMsg entry, before delivery to Copy. Thus zero is supplied by the original handler reply path, not introduced only at Copy's DOS return or message consumption. Why the handler supplies zero remains open.

Original ROM PutMsg caller0xfaabd2 belongs to common reply routine; local disassembly shows it dispatches the packet already populated by earlier handler logic. Next capture the handler's preceding close-result assignment, distinguishing handler policy from emulator/host-service effects. Evidence: artifacts/copy-boot-fixture/copy-close-packet.trx, close-packet-observations.json, close-handler-reply-localization.json, close-packet-build and logs. No command/emulator production changes or relaxed verifier. Full command/concurrency qualification remains open, shipping0/200.

### 2026-09-05 - Host FreeMem D0 clobber flows into handler close result

Added per-packet bounded128 handler instruction tails from request to reply, with packet Result1 snapshots. Unchanged concurrent fixture reproduces the failure. Background DF0 close frees a32-byte structure: at0xfa9858 D0=32, at0xfa985a JSR reaches host trap0x20073e, trap returnsD0=0 at0xfa985e. Handler then routes through0xfaacae and writes D0/D1 into packet results at0xfaaca0, changing Result1 from0xffffffff to0. No intervening D0 assignment is observed. This establishes the immediate data flow, not the correct replacement register contract.

Current host adapter CopperMod.Amiga.Emulator/CopperStart/Exec/ExecMemoryServices.cs:85 explicitly sets state.D[0]=0 after Free/RecordFree. FreeMem is a void API, so do not invent a success return or simply preserve the input to satisfy Copy. Next verify original Kickstart FreeMem register outcome across relevant allocator cases and add a focused compatibility regression before changing the adapter. Evidence: artifacts/copy-boot-fixture/copy-handler-tail.trx, handler-tail-observations.json, handler-zero-assignment.json, handler-tail-build and logs. Production unchanged; strict concurrent verifier still rejects source cleanup. Shipping0/200, full goal active.

### 2026-09-05 - Correct host FreeMem register result; concurrent transfers pass

Traced licensed40.63 Exec initialization to relative function table0xf8236c; vector35 (-210) resolves to FreeMem0xf81d42. Its valid free calls Deallocate0xf81c9c: D0 becomes (requested+(address&7)+7)&~7 and remains through ordinary Permit return; null-address and zero-length paths retain the input. This is observed register compatibility for a void API, not a public success result. Saved local disassembly original-freemem-disassembly.txt. Changed only host ExecMemoryServices.FreeMem's final D0 from unconditional0 to that span calculation, preserving allocation callbacks/recording and other adapter registers. No Copy policy change or hardware/CPU timing change.

Five new ExecFreeMemRegisterCompatibilityTests cover aligned, rounded, unaligned, zero and null cases with exact callback arguments and other-register preservation; all pass. Thirty-six existing task-descriptor retirement/process-boundary tests pass. Build zero errors, one existing analyzer warning. Unchanged native concurrent fixture now has background source Close=32 (other three closes=-1), two distinct overlapping tasks on one segment, exact24596-byte transfers, balanced ownership and unchanged image. Corrected concurrent verifier's overstrict Close==-1 predicate to DOS BOOL nonzero; historical zero-close trace remains rejected, alongside no-overlap, same-task and failed-write controls. Current strict transfer verifier passes.

Evidence: artifacts/copy-boot-fixture/freemem-register-build, freemem-register-unit.trx, freemem-owner-regressions.trx, copy-freemem-register.trx, freemem-register-observations.json, verified-concurrent-freemem.json, concurrent-freemem-controls.json. This closes the observed host FreeMem/FFS source-close discrepancy and bounded overlapping success-transfer case. Original-native allocator execution comparison across all coalescing/reschedule cases, broader emulator regressions, independent destination readback, overlapping success/failure, removal/replace and full command profiles remain open. Shipping remains0/200.

### 2026-09-05 - Overlapping successful Copy and destination failure verified

Prepared an LF-only concurrent-failure derivative: original Shell Run launches background C:Ed-to-RAM copy; foreground same-segment Copy targets missing RAM directory. Native observation completes with maximum active2/final0 and distinct tasks2200288/2148712 sharing segment545163. Foreground returns20 with OpenFailed=true and direct allocation/free1/1; background returns0 with2/2. Both images unchanged, no recorded code writes or trace overflow/collision.

Extended verify_copy_concurrent_transfer.py with explicit --target-failure mode. Verifies exact return set, per-task failed source ownership and Close success, target0/IoErr205, no failed-path transfer, and successful task's complete24596-byte payload. Added RDArgs allocation/ReadArgs/FreeArgs/FreeDosObject identity/type/order checks to both success and mixed modes. Current mixed trace and prior two-success trace pass. Four corruptions (wrong IoErr, failed source close, wrong parser cleanup pointer, missing overlap) reject. Evidence: artifacts/copy-boot-fixture/concurrent-failure-media.json, copy-concurrent-failure.trx, concurrent-failure-observations.json, verified-concurrent-failure.json, reverified-concurrent-success.json, concurrent-failure-controls.json.

Command/emulator production unchanged this turn. This closes the bounded real-handler overlapping target-failure/success case; independent destination readback and broader stream/directory/signal/stack/allocation-failure and resident lifecycle matrix remain open. No full command gate closes, shipping0/200. Next establish independently read destination bytes after background completion without relying on an uncompleted Wait command or relaxing the existing boot bound.
Negative-control correction: the first ad-hoc wrong-error mutation selected the parent Shell NIL: redirection rather than Copy target Open and was correctly accepted. Scoped mutations to the failed task library lease; all four intended Copy corruptions then reject. No production change was made to force rejection.

### 2026-09-05 - Concurrent destination readback observed; collision gate still rejects

Added bounded per-task/handle FGetC aggregation for original Type opens of copy-foreground/copy-parallel, recording byte count and SHA256 at Close. Prepared LF readback derivative replacing Wait1 with two original Type reads before resident removal. Initial512 event capacity overflowed during Shell argument parsing before readback; retained incomplete trace and increased only observer event storage to2048 (guest32x250000 execution and65536 per-readback byte bounds unchanged).

New run observes both independent readbacks at24596 bytes with hashBCF4467C7C7B8CB073E68A30FCF7C46B1C7B95D717B092696990FE89CBEB8E24; RemSegment names the reused segment545163 and returns-1, followed by matching segment FreeMem. However returnObservationCollision=true, so extended concurrent verifier --readback correctly rejects the whole evidence set. No passing qualification receipt is claimed. Added firstReturnObservationCollision diagnostic to record existing/incoming calls and return key on next build/run; this final diagnostic addition is not yet compiled/tested.

Evidence: artifacts/copy-boot-fixture/concurrent-readback-media.json, copy-concurrent-readback.trx (overflow), copy-readback-capacity.trx/readback-capacity-observations.json (collision), readback-concurrent-build/readback-capacity-build and logs. Next resolve observer collision from actual call identities without weakening completeness checks. Command/emulator production unchanged; shipping0/200.

### 2026-09-05 - Return collision includes interrupt-resumed vector entry

Built/reran first collision diagnostic: duplicate FGetC has same task/caller/stack/arguments. Added EntryRetiredPc/Opcode and reran; initial observation follows JSR opcode0x4eae at2176690, duplicate follows RTE opcode0x4e73 at16257516. Implemented narrow observer resume recognition only for RTE with a matching pending return key, vector/name and captured D0-D4/A1; retains original pending call and counts resumedVectorEntries. This is observation bookkeeping, not CPU/emulator behavior.

Rerun recognizes3 such resumptions, but still rejects completeness: a remaining FGetC collision reports the same JSR PC/opcode for both entries. No relaxation made for this second case. Need retirement-cycle/sequence evidence and the actual call-return path to distinguish duplicate retirement callback from a missed return; do not simply deduplicate repeated JSR. Independent readback/removal remain unqualified because strict verifier rejects returnObservationCollision=true.

Evidence: artifacts/copy-boot-fixture/copy-collision-detail.trx/collision-detail-observations.json, copy-collision-entry.trx/collision-entry-observations.json, copy-resumed-entry.trx/resumed-entry-observations.json and respective builds/logs. All processes terminal. Production unchanged; shipping0/200. Next instrument the unresolved repeated-JSR case without weakening the trace gate.

### 2026-09-05 - Remaining entry collision has distinct cycles, no observed task switch

Added EntryStartCycle/EntryRetireCycle and reran. Repeated JSR metadata is separated by about4160 cycles, not identical timestamps, with same pending FGetC frame/arguments. CPU retirement publisher constructs trace from State.LastInstructionProgramCounter/LastOpcode after completing timing; scheduler context includes these fields, raising a restoration hypothesis. Added EntryPreviousTask and reran: unresolved collision has previousTask2148712 and currentTask2148712 at both entries, so a visible guest task switch does not explain this case. Do not broaden resume handling based on task-switch speculation.

Evidence: artifacts/copy-boot-fixture/copy-collision-cycle.trx/collision-cycle-observations.json and copy-collision-task.trx/collision-task-observations.json, corresponding builds/logs. Strict collision gate continues rejecting readback qualification. Next capture a short chronological retirement window preceding the collision to identify interrupt/host-context restoration without relying solely on last-opcode metadata. No production edits or weakened checks; shipping0/200.

### 2026-09-05 - Concurrent readback and resident removal pass after observer resume correction

Chronological96-entry diagnostic identifies original interrupt handler RTS to host continuation0xf08200 before duplicate entry carrying restored JSR metadata. Source AmigaBootController.FinishExecInterruptDispatch explicitly CopyTaskContextFrom(savedState); this explains unchanged task and restored LastOpcode/LastInstructionPC. Extended observer resume recognition to that registered continuation (resolved from the controller's constant, not a copied address), retaining strict matching pending key/vector/name/D0-D4/A1. Other collisions still fail. No CPU/emulator execution change.

Unchanged readback derivative now passes with8 recognized resumptions, no collision or overflow. Concurrent verifier --readback passes: two distinct overlapping tasks, one resident segment, complete24596-byte transfers and cleanup, original Type independently reopens both destinations with exact byte counts/hashes and matching reported file sizes, then matching RemSegment success and allocation-sized segment FreeMem. Four negative controls reject wrong readback hash, wrong length, missing segment free and a collision flag. Prior collision traces remain failed historical evidence.

Evidence: artifacts/copy-boot-fixture/copy-collision-window.trx/collision-window-observations.json, copy-host-resume.trx/host-resume-observations.json, verified-concurrent-readback.json, concurrent-readback-controls.json and corresponding builds/logs. Bounded concurrent success/readback/removal milestone complete; full resident variation matrix, original installed flags and all command/profile gates remain open, shipping0/200. Next consolidate these real-boot milestones into CP8 ledger and proceed to remaining command options/lifecycle coverage rather than repeating this successful fixture.

### 2026-09-05 - CP8 consolidated and normal Copy real-handler transfers verified

Updated CP8 ledger with concurrent success/failure, independent24596-byte readback and resident removal evidence, replacing stale concurrency-open wording with the remaining wider matrix. Prepared LF normal-mode fixture by removing DIRECT from the two sequential25-byte RAM copies. First run reaches success but stops on observer's obsolete fixed2 allocation expectation: normal traversal makes four allocations, including two temporary path buffers. Moved exact allocation counts to scenario verifiers; observer still validates every pointer/size/free and requires empty ownership at return. Existing DIRECT verifiers retain their exact counts.

Rerun passes: two same-segment normal invocations return0, each4 allocations/frees and unchanged image; no collision/overflow. Added verify_copy_normal_transfer.py requiring scenario-specific allocation sizes (4770,33/35,33,512), normalized destination-first opens, exact25-byte Read/Write, unique successful closes, and original Type exact payload/size readback for both destinations. Initial verifier assumed equal target path allocation size; observed second longer path uses35 rather than33 and corrected explicit scenario expectation. Verifier now passes. This does not qualify all traversal, metadata, failure options or full resident lifecycle.

Evidence: artifacts/copy-boot-fixture/normal-copy-media.json, copy-normal.trx/normal-first-observations.json (obsolete count assertion), copy-normal-ownership.trx/normal-ownership-observations.json, verified-normal-copy.json and builds/logs. No command production changes; shipping0/200. Next add normal-mode overwrite/traversal and metadata cases, preserving original parser/options and resource ownership.

### 2026-09-05 - Normal existing-destination overwrite controls verified

Prepared separate LF resident fixture with distinct existing destination bytes: DONTOVERWRITE on copy-target and FORCEOVERWRITE on copy-target2, both QUIET and BUFFER1. Native run passes. Both same-segment invocations return0; skip path alloc/free3/3 and replacement4/4, code unchanged, no observation overflow/collision. Original Type independently confirms copy-target retains18-byte preserve-existing plus LF, while copy-target2 becomes exact25-byte source payload. DONTOVERWRITE invocation has no Open/Read/Write/Close transfer calls. Return0 is recorded as observed QUIET behavior, not interpreted as proof of copying.

Extended verify_copy_normal_transfer.py with --overwrite mode requiring those per-path counts, no transfer on skip, preserved payload/size, and successful replacement transfer/readback. Existing normal-mode trace still passes. Three negative controls reject changed preserved bytes, failed replacement Write and skip-path allocation leak. Evidence: artifacts/copy-boot-fixture/overwrite-media.json, copy-overwrite.trx, overwrite-observations.json, verified-overwrite.json and overwrite-controls.json. This validates resulting bytes on ordinary writable RAM files; protection-clear/DeleteFile call ordering, protected targets, metadata behavior, non-QUIET diagnostics and original MorphOS runtime parity remain open. Next add protected-target and metadata/recursive traversal coverage. Command/emulator production unchanged, shipping0/200.

### 2026-09-05 - FORCEOVERWRITE clears real delete protection before replacement

Added original SetProtection/DeleteFile vector entry/return capture and a separate LF fixture where original C:Protect sets copy-target2 to mask1 before resident Copy. Native trace confirms setup succeeds, Copy SetProtection(normalized target,0) succeeds before DeleteFile of the same path, which succeeds before destination Open(NewFile). Independent Type checks confirm expected replacement payload/size and preserved DONTOVERWRITE destination; ownership/image checks pass. Also observed metadata-tail SetProtection(mask65519) after work (including skipped destination); full metadata parity remains separate.

Extended normal verifier --overwrite --protected to require actual protection setup, clear/delete/open ordering and results. Fixed size-record scope to the matching original Type LoadSeg-to-Open interval: Protect also emitted a prior MatchFirst for the same filename, which made the previous whole-prefix uniqueness assumption invalid. Protected profile passes; ordinary normal/overwrite regressions pass. Three negative controls reject missing delete protection, failed protection clear and failed delete. Evidence: artifacts/copy-boot-fixture/protected-overwrite-media.json, copy-protected-overwrite.trx, protected-overwrite-observations.json, verified-protected-overwrite.json, protected-overwrite-controls.json and build/logs. Production unchanged. Recursive traversal, metadata parity, non-QUIET diagnostics and full command gates remain open, shipping0/200.

### 2026-09-05 - ALL recursion copies distinct top-level and nested file payloads

Prepared LF original-MakeDir/Echo setup with tree/top, tree/sub/leaf and an empty directory. Same unchanged Copy HUNK runs ALL QUIET BUFFER1 through original Shell/DOS. Native run completes: return0, five matched allocations/frees, unchanged image, no observation overflow/collision. Opens target Ram Disk:tree-copy/top and Ram Disk:tree-copy/sub/leaf before their respective source files. Original Type independently reopens both files.

Added verify_copy_recursive_transfer.py requiring normalized nested paths, per-handle lifetime-scoped transfer/Close checks (avoids recycled handles), exact distinct18/20-byte payloads and independent original Type size/byte readback. Native trace passes; three corruptions reject flattened destination path, failed write and allocation leak. Empty directory type is not captured sufficiently by current FIB observation, so emptyDirectoryQualified remains explicitly false despite the script's original List request. Next add FIB directory type/CreateDir ownership evidence to close that portion, then metadata and error traversal cases.

Evidence: artifacts/copy-boot-fixture/recursive-media.json, copy-recursive.trx, recursive-observations.json, verified-recursive-files.json, recursive-file-controls.json and logs. Production unchanged; full recursive/options and command gates remain open, shipping0/200.

### 2026-09-05 - Recursive empty-directory preservation explicitly observed

Added original CreateDir call capture and signed FIB directory-entry type (classic offset120) to the read-only boot observer. Unchanged recursive fixture passes. Copy creates RAM:tree-copy, Ram Disk:tree-copy/empty and Ram Disk:tree-copy/sub with nonzero returned locks. Original List subsequently returns0 and MatchFirst reports destination empty with directory type2, distinguishing it from a zero-byte file. No Copy Open targets children beneath empty. Both prior independent file payload/size checks still pass.

Extended recursive verifier --directories to require those successful creation calls and independent List directory evidence. Three negative controls reject file type instead of directory, failed creation and failed List. Directory lock lifecycle remains explicitly unqualified (CreateDir lock release not yet captured); this case does not establish arbitrary traversal errors or metadata behavior. Evidence: artifacts/copy-boot-fixture/copy-recursive-directory.trx, recursive-directory-observations.json, verified-recursive-directories.json, recursive-directory-controls.json and build/logs. Production unchanged; shipping0/200. Next cover directory-lock ownership and metadata flags with original DOS calls.

### 2026-09-05 - Recursive CreateDir lock releases verified before reuse

Added read-only Lock/UnLock vector observations, retaining void UnLock return presence without interpreting D0 as success. Unchanged recursive fixture passes. Each of Copy's three successful CreateDir results is followed by matching returned UnLock in the same task before any observed Lock/CreateDir reacquires that pointer. The trace demonstrates actual pointer recycling, so whole-run unique pointer counting would be incorrect.

Extended recursive verifier --directories --created-locks to require these lifetime-scoped releases. Existing recursive payload and directory-type checks pass. Two corruption controls reject an omitted release (even though a later reused pointer is unlocked) and a release without return. Output separately marks createdDirectoryLocksReleased=true and directoryLockLifecycleQualified=false: ParentDir/DupLock/CurrentDir and matcher-owned locks are not fully inventoried by this observer yet. Evidence: artifacts/copy-boot-fixture/copy-recursive-lock.trx, recursive-lock-observations.json, verified-recursive-created-locks.json, recursive-created-lock-controls.json and build/logs. Production unchanged; shipping0/200. Next cover remaining lock-producing APIs and ownership transfers before claiming all-path traversal cleanup.

### 2026-09-05 - Explicit recursive lock ownership and caller directory restoration verified

Added original ParentDir/DupLock/CurrentDir/MatchEnd vector observations. Unchanged recursive fixture passes. Within Copy's lease all explicit successful acquisitions (11 Lock,9 ParentDir,3 CreateDir) match returned UnLock calls using a reference counter that permits pointer reuse; no unexplained nonzero UnLock or remaining explicit ownership. CurrentDir return/input chain restores the initial caller directory after temporary swaps. All four successful MatchFirst anchors receive matching returned MatchEnd, including reused anchor addresses.

Extended recursive verifier --traversal-locks to check acquisition coverage, ownership balance, directory swap chain and matcher cleanup along with existing payload/empty-directory checks. Three negative controls reject lost parent acquisition, broken directory return chain and omitted MatchEnd. Output marks explicitTraversalLocksAndDirectoryRestorationVerified=true but broader directoryLockLifecycleQualified remainsfalse for other paths/failures and internal provider ownership. Evidence: artifacts/copy-boot-fixture/copy-traversal-owner.trx, traversal-owner-observations.json, verified-traversal-ownership.json, traversal-owner-controls.json and build/logs. Production unchanged; shipping0/200. Next metadata options and traversal failure/cancellation coverage remain required.

### 2026-09-05 - Copy FIB alignment corrected; original-DOS metadata verified

The CLONE / NOPRO COM DATES fixture exposed a production workspace defect: FIB offset2438 was two bytes short of longword alignment. Classic DOS transports the pointer as a BPTR, so Examine populated data two bytes before the intended buffer. Source name, protection, date and comment reads were shifted. Earlier metadata-tail masks65519/5308399 are defective historical observations, not metadata parity evidence.

Added two padding bytes after the classifier: workspace4772, path392, FIB2440, warning arguments2700, destination2708, examine tags4756. Updated six full-command native fixtures to those exact sizes/offsets; unrelated helper layouts remain unchanged. All333 invocations pass across68000/68020/68040 with shared images unchanged and resource ownership balanced. New68000 HUNK SHA256 2efdde9b97aeb46c26fcd7eb8475869d183dd0915db8b5f5c07f5b972bfefd87. Historical command-input manifests remain unchanged; alignment-bindings.json identifies this revision.

A new private derivative using that HUNK passes original Shell/DOS metadata execution. verify_copy_metadata_transfer.py checks normal25-byte payloads and independent original Type readback, same-segment reuse and four balanced allocations per invocation, aligned successful source Examine, source protection80/comment metadata-note, exact DateStamp forwarding, successful metadata calls and independently read destination metadata. CLONE yields protection64 (archive cleared), intact date/comment; NOPRO COM DATES makes no SetProtection call and leaves destination protection0 with intact date/comment. Four negative controls reject shifted comment, wrong protection, shifted date and misaligned FIB. Existing normal/overwrite/protected historical receipts still pass with their explicit old4770 default; the metadata verifier requires4772.

Evidence: artifacts/copy-aligned-workspace-qualified/qualification.json and alignment-bindings.json; artifacts/copy-boot-fixture/metadata-aligned-media.json, copy-metadata-aligned.trx, metadata-aligned-observations.json, verified-metadata-aligned.json, metadata-aligned-controls.json. Original bad metadata observations and initial stale-fixture failure are retained. This closes the observed alignment defect and these two metadata profiles only. Full metadata options/failures, traversal cancellation, original MorphOS runtime parity, installed flags and shipping gates remain open; shipping0/200. Next expand metadata option coverage and failure paths using the aligned build.

### 2026-09-05 - Normal-command metadata option precedence verified on original DOS

Prepared a separate derivative with the aligned HUNK, original Protect rwespa producing source protection113, and two resident runs: CLONE PROX and CLONE NOPRO. Both pass payload/ownership and independent original Type metadata readback. CLONE PROX yields97, retaining delete protection with archive cleared, rather than the narrower96 mask. This exercises the existing source-based setup in which PROTECTION remains enabled and takes precedence over PROX; no production semantics were changed. CLONE NOPRO performs no SetProtection and destination remains0. Both retain full metadata-note comment and exact source DateStamp.

Extended verify_copy_metadata_transfer.py with explicit --precedence profile (source113, destinations97/0) and ordered protection/date/comment assertions. Prior aligned CLONE/NOPRO COM DATES trace still passes the strengthened verifier, recorded separately in verified-metadata-aligned-order.json. Three negative controls reject a PROX-only96 application, ignored NOPRO destination protection and reversed date/comment order.

Evidence: artifacts/copy-boot-fixture/startup-metadata-precedence.txt, metadata-precedence-media.json, copy-metadata-precedence.trx, metadata-precedence-observations.json, verified-metadata-precedence.json and metadata-precedence-controls.json. These are execution of the replacement command on original Kickstart DOS, not original MorphOS command runtime parity. Native source/binary unchanged; shipping0/200. Next cover individual metadata defaults/options and failure behavior, then remaining normal traversal cancellation/error cases. Full command and profile gates stay open.

### 2026-09-05 - COM and DATES individually verified on original DOS

Unchanged aligned Copy HUNK executed a separate resident fixture with COM only and DATES only. Both return0 with four balanced allocations, unchanged shared image and independent25-byte original Type readback. Source protection80 becomes64 by default in both cases. COM sets the complete metadata-note comment without SetFileDate; DATES sets the exact source DateStamp without SetComment, and independent destination comment is empty. The verifier deliberately makes no clock-dependent assertion about COM destination creation time: absence of date-setting calls is the bounded evidence there.

Extended verify_copy_metadata_transfer.py with --individual, retaining exact4772 workspace and source metadata checks, call results/order, forbidden unselected setters and independent original Type destination metadata. Existing combined/precedence traces pass regression rechecks saved under separate *individual-regression.json receipts. Four negative controls reject an extra date setter, extra comment setter, a copied comment under DATES and wrong default protection.

Evidence: artifacts/copy-boot-fixture/startup-metadata-individual.txt, metadata-individual-media.json, copy-metadata-individual.trx, metadata-individual-observations.json, verified-metadata-individual.json and metadata-individual-controls.json. No production code changes. Original MorphOS command runtime parity, denied metadata operations, traversal failures/cancellation and all shipping gates remain open; shipping0/200. Next prioritize CC13 denied metadata writes and partial-failure behavior rather than interpreting successful option fixtures as complete qualification.

### 2026-09-05 - Original DOS denies metadata writes; resident recovery observed

A new write-protected private Workbench derivative runs Copy to existing SYS:C/Type with DONTOVERWRITE CLONE QUIET NOREQ, then a normal CLONE RAM copy through the same resident segment. The skipped invocation performs no Open/Read/Write/Close/DeleteFile, but calls SetProtection, SetFileDate and SetComment against Workbench3.1:C/Type in that order. Each returns0/IoErr214 (disk write protected). Copy returns0, matching the implemented ignored-metadata-return policy; the denied metadata was not applied. Whole derivative disk hash remains unchanged.

Second invocation returns0, transfers25 exact bytes, successfully calls all metadata setters, closes both handles and passes independent original Type payload readback. Allocations/frees are3/3 then4/4; code unchanged, final active invocations0, resident segment removal/free verified. Added verify_copy_metadata_denial.py and four negative controls rejecting missing denial, wrong handler error, corrupted recovery write and ownership leak.

Evidence: artifacts/copy-boot-fixture/startup-metadata-denied.txt, metadata-denied-media.json, copy-metadata-denied.trx, metadata-denied-observations.json, verified-metadata-denied.json and metadata-denied-controls.json. No production changes. This is skipped-copy metadata denial, not denial after a successful byte transfer or proof of full partial-failure reporting. CC13's no-false-complete-success requirement remains open: preserve observed behavior as explicit compatibility evidence, do not relabel return0 as metadata success or silently change original semantics. Original MorphOS runtime comparison and non-QUIET/ERRWARN failures still require evidence; shipping0/200.

### 2026-09-05 - Non-QUIET DONTOVERWRITE with ERRWARN observed

Ran a new derivative changing first invocation to DONTOVERWRITE CLONE ERRWARN NOREQ (no QUIET). It returns0, performs no file transfer and no metadata setters, with IoErr203 at library release. Subsequent same-resident CLONE RAM copy returns0 and passes25-byte independent readback, allocation ownership and segment removal. Source NativeMorphOSCopyWork records a destination rejection string but does not set Result to WARN at that branch, so ERRWARN has no WARN to promote. This is observed replacement-command behavior; it is not full original MorphOS runtime parity.

Extended verify_copy_metadata_denial.py --visible-skip to require absent metadata tail and observed203, retaining strict transfer/recovery/ownership checks. First verifier rejected two SegmentFreeMem labels: trace shows actual19640-byte segment free followed later by a4104-byte free at the recycled address. Match segment deallocation using both recorded pointer and size; unrelated later allocations do not count as another segment free. Three controls reject an unexpected metadata call, removal of the actual segment free (leaving the recycled-address event), and a different return code. Previous denied-metadata trace still passes.

Evidence: artifacts/copy-boot-fixture/startup-overwrite-visible.txt, overwrite-visible-media.json, copy-overwrite-visible.trx, overwrite-visible-observations.json, verified-overwrite-visible.json, overwrite-visible-controls.json. No diagnostic Write bytes were captured: diagnosticTextQualified remainsfalse. Next add read-only PrintFault/VPrintf observation to qualify actual diagnostics and correct the observer's recycled-address segment-free labeling. Production unchanged; shipping0/200 and full partial-failure reporting remains open.

### 2026-09-05 - Original-DOS diagnostic calls and segment-free observation verified

Extended the read-only passive observer with PrintFault, VPrintf and PutStr vector entries; captures D1 format strings for VPrintf/PutStr and existing register/return data for PrintFault. SegmentFreeMem classification now requires copyImageLive, preventing a later unrelated free at the recycled address from being labeled as a second segment release. No command, CPU or emulator execution semantics changed. Build passed with one pre-existing xUnit2013 warning in DosRunCommandRetirementInspectionTests.

Reran the unchanged non-QUIET DONTOVERWRITE CLONE ERRWARN derivative. Strict verifier passes existing skip/recovery/ownership checks plus exactly one VPrintf failure-prefix format followed by exactly one returned PrintFault with D1=203 and null header. No trace overflow/collision. Exactly one SegmentFreeMem now appears. Three negative controls reject wrong error214, missing prefix and missing fault. This proves diagnostic calls and order, not exact rendered console text; diagnosticTextQualified remainsfalse.

Evidence: artifacts/copy-boot-fixture/diagnostic-build.log, copy-diagnostic.trx, diagnostic-observations.json, verified-diagnostic.json and diagnostic-controls.json. Shipping0/200. Next qualify formatted output/arguments and broader partial-transfer failures; the observed zero skip result remains compatibility evidence, not a full CC13 exit claim.

### 2026-09-05 - Redirected diagnostic bytes independently verified

New derivative redirects the non-QUIET DONTOVERWRITE CLONE ERRWARN invocation to RAM:copy-diagnostic and invokes original Type on that file after the successful recovery copy. Original Type reads exactly ` not opened for output: object already exists` plus LF; its independent MatchFirst file size agrees. Existing diagnostic-call203/order, skipped file effects, same-segment ownership, recovery payload and resident removal checks pass unchanged.

Extended verify_copy_metadata_denial.py --diagnostic-readback with exact bytes, successful independent open/close, task/handle scoping and original Type size check. Two negative controls reject a changed byte and changed file length. diagnosticTextQualified now records this bounded redirected English diagnostic, not general locale/output or original MorphOS command parity. Corrected Copy.md's obsolete opening status to describe the existing native body and retain all remaining qualification gates.

Evidence: artifacts/copy-boot-fixture/startup-diagnostic-readback.txt, diagnostic-readback-media.json, copy-diagnostic-readback.trx, diagnostic-readback-observations.json, verified-diagnostic-readback.json and diagnostic-readback-controls.json. Production unchanged; shipping0/200. Next move to actual transfer/open failure with ERRWARN and multi-object partial-success coverage; successful skip fixtures do not close those requirements.

### 2026-09-05 - Real destination-open denial returns ERROR with ERRWARN

New derivative targets nonexistent SYS:copy-new with CLONE ERRWARN NOREQ and redirected diagnostics. Original DOS Open(NewFile) fails0/IoErr214 at normalized Workbench3.1:copy-new. Unlike overwrite-skip, Copy returns10; no source Open, Read, Write, Close, DeleteFile or metadata update occurs within the failed invocation. Three allocations/frees balance. The second same-resident CLONE RAM invocation returns0 with four balanced allocations, exact25-byte independent original Type readback and unchanged code; resident removal/free succeeds.

Extended verify_copy_metadata_denial.py --open-denied (with visible diagnostic/readback options) to require returns10/0, open outcomes true/false, exact allocation sizes4772/27/33 then4772/33/33/512, actual failed Open1006/214, diagnostic PrintFault214 and exact ` not opened for output: disk is write-protected` plus LF through original Type with matching size. Three controls reject false success, wrong open error and changed output byte. Previous skip/readback and metadata-denial traces still pass.

Evidence: artifacts/copy-boot-fixture/startup-open-denied.txt, open-denied-media.json, copy-open-denied.trx, open-denied-observations.json, verified-open-denied.json and open-denied-controls.json. This qualifies this ERRWARN destination-open failure/recovery case, not multi-object partial-copy behavior, ordinary WARN without ERRWARN, full-media writes or original MorphOS runtime parity. Production unchanged; shipping0/200. Next cover multi-object success followed by failure and stop/continue semantics.

### 2026-09-05 - Multi-source first success then missing-source failure verified

New original-DOS fixture passes FROM RAM:first RAM:missing RAM:last to RAM:partial in one QUIET invocation. First14-byte payload is copied successfully. Missing source produces two MatchFirst205 results (classification and traversal) and a returned PrintFault205 despite QUIET. Final command returns20 with six balanced allocations/frees and unchanged image. Later source is still examined twice successfully, but no later transfer occurs: source loop continues while secondary failure suppresses traversal work. Do not describe this as an immediate source-loop stop.

Original Type independently confirms first payload and size, while its second run cannot match the last destination (205) and returns10 without Open. Added verify_copy_partial_sources.py checking native20, exact allocation sequence, first transfer/close, failure ordering, later examination, independent surviving bytes and absent later output. Three controls reject false success, lost first output and missing failure evidence.

Evidence: artifacts/copy-boot-fixture/startup-partial-sources.txt, partial-sources-media.json, copy-partial-sources.trx, partial-sources-observations.json, verified-partial-sources.json and partial-sources-controls.json. Production unchanged; shipping0/200. This closes this bounded partial-success/missing-source case only. WARN/ERRWARN continuation, write failures after partial data and broad original MorphOS runtime parity remain open.

### 2026-09-05 - Failed warning fixture exposes observer segment reuse; corrected

Attempted first/blocked/last source fixture with original Protect wed. Protect succeeds setting mask8, but original RAM handler permits Open(OldFile) on blocked and all three files transfer; Copy returns0. This is not a warning-continuation case and earns no warning coverage. The first run then fails observer allocation assertions when a later command reuses the freed Copy segment address. Preserved partial-warning-first-observations.json and copy-partial-warning.trx as failed evidence.

Added copyImageLive guards to both RunCommand entry and completion classification in the passive observer. Built successfully, then reran the unchanged fixture. No failure/overflow/collision, exactly one Copy invocation with six balanced allocations and unchanged image, final active count0; a later LoadSeg reuses the old Copy segment after SegmentFreeMem without being attributed to Copy. Evidence: segment-lifetime-build.log, copy-segment-lifetime.trx, segment-lifetime-observations.json and verified-segment-lifetime.json. No command/CPU/emulator behavior change.

Shipping0/200. Next use a failure mechanism demonstrated by the actual handler (such as an output name colliding with an in-use/protected target) for WARN continuation; read-protection flags on this RAM handler alone are insufficient. Keep the new successful fixture as observer lifetime evidence only, not a warning or full copying qualification.

### 2026-09-05 - In-use source produces real partial failure, not WARN continuation

Changed the middle source to RAM:copy-diagnostic, held open for output by Shell redirection during the invocation. Original DOS MatchFirst returns202 (object in use), including preclassification and actual traversal. Copy transfers first14-byte file, returns20, and does not transfer the last source although later examination succeeds. Independent original Type confirms the first bytes/size and last destination absence. Six allocations/frees balance; one unchanged native image, final active count0 and no trace collision/overflow.

Extended verify_copy_partial_sources.py --in-use to distinguish the202 failure after the completed first write, retain exact file effects and result20, and record observed diagnostic sequence PrintFault202 then final PrintFault0 after subsequent DOS activity changed IoErr. The latter is source-style cleanup/error observation, not proof of correct final diagnostic wording. Prior missing-source205 fixture still passes. Two controls reject wrong handler error and false-success return.

Evidence: artifacts/copy-boot-fixture/startup-partial-inuse.txt, partial-inuse-media.json, copy-partial-inuse.trx, partial-inuse-observations.json, verified-partial-inuse.json and partial-inuse-controls.json. This adds real in-use partial-failure coverage but does not trigger a WARN branch; warning continuation remains open. Next construct a failure at byte-transfer/output-open stage after matching succeeds, using controlled handler behavior or a demonstrated destination condition. Production unchanged; shipping0/200.

### 2026-09-05 - Native middle-write WARN continuation and ERRWARN stop verified

Extended the full-command directory fixture to three supplied source patterns and a selected transfer-failure index. Two new cases copy the first eight-byte payload, return a short Write(0 rather than8) for the middle payload with supplied IoErr221, and either process the third source (return5) or stop before it under ERRWARN (return10). Assertions enforce payload identity, cached buffer reuse, exact per-source matching/open/close order, deletion of RAM:file1 only after its two closes, source/target unlocks, parser retention and final ownership. Quiet metadata-tail behavior remains unchanged.

Generalized fixture source-vector termination and expected counts for three sources. Initial run failed at a fixture allocation expectation that only listed first/second traversal allocations; added the exact additional2330-byte anchor expectation bounded by expected source count. Preserved failed artifacts/copy-middle-write-failure. Complete driver then passes339 invocations (113 per68000/68020/68040), including17 directory cases perCPU, with unchanged command HUNK hashes and no shared-image writes/leaks. New continue case has six allocations/frees and three completed file attempts; stop case has five and two. This is supplied-DOS-vector native command evidence, not original-DOS handler or parser execution.

Evidence: artifacts/copy-middle-write-qualified/qualification.json and directory-68000.runtime.json / directory-68020.runtime.json / directory-68040.runtime.json, corresponding build log and reports. Production command unchanged; shipping0/200. Warning continuation is now covered at the controlled native ABI boundary; original-runtime reproduction, non-QUIET variant, write failure after some bytes were already written and original MorphOS parity remain open.

### 2026-09-06 - Native partial destination data cleanup verified

Added two full-command three-source cases where the middle Write accepts3 of8 bytes and sets supplied IoErr221. The DOS vector fixture now retains modeled per-destination bytes, including the failed three-byte prefix, and verifies DeleteFile removes that exact middle output after its two handle closes. Successful first/third outputs retain their distinct eight-byte contents; ERRWARN stops before creating the third. Repeated writes after a short return fail the fixture. Existing zero-byte failure cases now also verify surviving destination contents rather than only call counts.

Complete native suite passes345 invocations (115 per68000/68020/68040;19 directory cases each), with unchanged command HUNK hashes and no shared-image writes/leaks. New partial-write cases return5/10 and free6/5 allocations respectively. Assertions cover exact partial contents at removal, successful sibling preservation, parser/lock lifetime and cached-buffer behavior. This models public DOS write results; it is not a real full-volume filesystem or original-DOS partial-write test.

Evidence: artifacts/copy-partial-write-qualified/qualification.json, directory-*.runtime.json and partial-write-summary.json; build log artifacts/copy-partial-write-qualified-build.log. Production command unchanged; shipping0/200. Original-runtime write/cancellation failures, non-QUIET failure diagnostics and complete command/profile parity remain open.

### 2026-09-06 - Partial destination cleanup denial verified at native DOS boundary

Added continue/ERRWARN-stop cases where the middle Write accepts3 of8 bytes with supplied error221, then DeleteFile fails with supplied error222. The model retains the exact three-byte partial destination on denied cleanup, verifies prior successful bytes and permitted later output, and requires saved transfer error221 when SetIoErr follows failed deletion. Report event sequences confirm DeleteFile is immediately followed by SetIoErr. Return levels remain5/10; failed deletion is not mislabeled as a removed file.

All351 native invocations pass (117 perCPU,21 directory cases) with unchanged command HUNK hashes, shared-image integrity and balanced resources. New cases free6/5 allocations. This is controlled DOS-vector evidence; no real-filesystem cleanup-denial or full shipping claim follows. Evidence: artifacts/copy-cleanup-denied-qualified/qualification.json, directory-*.runtime.json, cleanup-summary.json and artifacts/copy-cleanup-denied-qualified-build.log. Production unchanged; shipping0/200. Remaining work includes original-runtime partial-write/cleanup/cancellation, non-QUIET variants and complete command/profile parity.

### 2026-09-06 - Non-QUIET partial-write diagnostics and continuation verified

Added visible first-success/middle-partial-write/third-source cases with and without ERRWARN to the full-command directory fixture. Exact expected output includes created-directory line, first copied line, middle `file1.. not copied.: [DOS fault]` line, and a third copied line only when continuing. The DOS fault text is explicitly a supplied placeholder, not original OS localization evidence. PrintFault must receive221/null header after partial-file deletion and before parser release; exactly one fault call occurs.

Extended output-fixture support for failure prefix and per-file Flush after prior handles close, and verbose preclassification over all three retained source arguments. Existing byte-survival/deletion, parser/lock ownership, warning5/error10 and stop/continue assertions remain active. All357 native invocations pass (119 per68000/68020/68040;23 directory cases), with unchanged command HUNKs and no shared-image writes/leaks. Evidence: artifacts/copy-visible-partial-qualified/qualification.json, directory-*.runtime.json and artifacts/copy-visible-partial-qualified-build.log.

Production unchanged; shipping0/200. This adds native ABI/output composition evidence, not original-runtime partial-write behavior or full command parity. Original filesystem full-media/cancellation, broader option combinations and original-profile gates remain open.

### 2026-09-06 - Cancellation during a partially completed native transfer verified

Added two full-command three-source cases with a16-byte middle source. The controlled handler accepts its first8 bytes, then the next public SetSignal poll reports Ctrl-C and remains pending. Assertions require no further Read/Write, both middle handles closed, deletion of the exact8-byte partial destination, intact successful first output and no third output. Normal/ERRWARN results are5/10; both paths stop further sources. This extends the prior between-source cancellation checks to cancellation inside CopyFile.

All363 native invocations pass (121 per68000/68020/68040;25 directory cases). Each new case performs two Read/Write pairs total (first file and first middle chunk), four Close calls and one DeleteFile, with five allocations/frees, retained parser/lock ownership checks, unchanged command HUNK hashes and no shared-image writes/leaks. This is a supplied Ctrl-C signal at the public Exec boundary, not a real guest keyboard/Break delivery or original-runtime cancellation qualification.

Evidence: artifacts/copy-active-cancel-qualified/qualification.json, directory-*.runtime.json, cancellation-summary.json and artifacts/copy-active-cancel-qualified-build.log. Production unchanged; shipping0/200. Real-runtime signal delivery and filesystem cancellation, full-media behavior, remaining command options and original-profile parity remain open.

### 2026-09-06 - Shared classic FIB alignment guard rejects actual misaligned native code

Audited current Native command calls to classic Examine/ExamineFH and FIB storage: Copy uses corrected workspace offset2440 or aligned AllocMem/AllocDosObject storage; MakeLink uses AllocDosObject. No additional misaligned call site was found in this bounded search. Added a shared native gateway assertion for classic Examine, ExNext and ExamineFH requiring a non-null longword-aligned D2 FIB pointer. This prevents supplied vector handlers from accepting the pointer truncation defect previously exposed only by original DOS.

All363 full-command native invocations pass across68000/68020/68040. Added a test-only HUNK negative control: located the unique MOVE.L #2440 immediate at file offset936 and changed only that immediate to2442, retaining workspace allocation size and all other code. The single-command suite rejects it specifically at Examine with the alignment assertion, rather than an unrelated allocation failure. Original and mutated hashes and offset are in misaligned-control.json. This derivative is test-only and must never be installed.

Evidence: artifacts/copy-fib-alignment-guard/qualification.json, misaligned-control.json, copy-68000-misaligned-control.hunk and misaligned-control.log. Production unchanged; shipping0/200. The guard is classic-FIB coverage only, not a blanket proof of every SDK pointer ABI or every command's runtime behavior. Remaining original-runtime/profile gates remain open.

### 2026-09-06 - MakeLink alignment regression and hard-link failure cleanup verified

Ran MakeLink's existing33 native cases under the new shared classic FIB alignment guard; all pass. Inspected the command's hard-link branch and added supplied cross-volume-error215 and forced-directory-link-error203 cases. Strengthened all MakeLink cases to require exact PrintFault count/error/header and FIB-free -> target-UnLock -> parser-FreeArgs ordering. Hard-link failures specifically require MakeLink -> diagnostic -> FIB cleanup, preserving the observed failure report before resource release. Examine failure remains silent as implemented/source-observed; no invented fault was added.

Expanded suite passes39 invocations (13 per68000/68020/68040), with resident shared-image integrity and balanced resources. Evidence: artifacts/makelink-fib-alignment/qualification.json, artifacts/makelink-hard-failure-qualified/qualification.json and perCPU runtime/static receipts, corresponding build logs. These are controlled parser/DOS vectors; actual cross-volume handler behavior and packaged/original-profile parity remain open. Command production unchanged; shipping0/200.

### 2026-09-06 - MakeLink command startup rejection and Workbench ownership verified

Inspection confirms NativeMorphOSMakeLinkEntry already uses a standard length/text command signature and NativeCommandStartup; no duplicate wrapper was added. Added five startup-only cases: missing DOS, Workbench rejection, Workbench with missing DOS, negative entry length and nonzero length with null argument buffer. Fixtures allocate no parser backing for these cases and require no body allocations, parsing, link/lock/FIB operations or body fault diagnostics.

All54 native invocations pass (18 per68000/68020/68040) with balanced library lifetime and shared-image integrity. Report events verify Workbench WaitPort -> GetMsg -> OpenLibrary -> Forbid -> ReplyMsg, including failed library opening. Return/error checks distinguish FAIL with untouched initial IoErr when DOS is unavailable from ERROR/ObjectWrongType or LineTooLong on rejected startup. Existing command-body tests and interleaved calls remain active.

Evidence: artifacts/makelink-startup-qualified/qualification.json and perCPU runtime/static receipts; artifacts/makelink-startup-qualified-build.log. This qualifies the current entry's controlled startup behavior, not original MakeLink startup parity or shipping admission. Production unchanged; shipping0/200. Real parser/handler execution and original-profile packaging remain open.

### 2026-09-06 - MakeLink hard link created and independently read on original DOS

Extended the disposable boot observer with an explicit command_under_test=MakeLink receipt identity and CC12 scope, plus read-only MakeLink vector capture. Existing Copy receipt default/scope and lifecycle field names remain unchanged for backward compatibility. New derivative replaces C:Ed with the hash-bound2580-byte MakeLink68000 HUNK, creates a RAM source, loads forced-PURE resident Ed, invokes FROM RAM:link-alias TO RAM:link-source HARD, removes it, then reads the alias with original Type.

Native command returns0 with one matched workspace allocation/free and unchanged image; real ReadArgs consumes the original template. Original Lock/Examine identify a regular source file, MakeLink receives that lock with hard flag0 and succeeds. FIB release -> target unlock -> parser release identities/order pass. Original Type independently opens link-alias and reads exact18-byte hard-link-payload plus LF with matching file size. Added verify_makelink_boot.py and three controls rejecting a soft flag, failed link result and wrong target lock. Scope explicitly does not qualify installed purity flags or full MakeLink behavior.

Evidence: artifacts/copy-boot-fixture/makelink-hard-media.json, makelink-hard.trx, makelink-hard-observations.json, verified-makelink-hard.json, makelink-hard-controls.json and makelink-build/test logs. Command source unchanged; shipping0/200. Remaining MakeLink work includes soft/dangling links, directory FORCE behavior, repeated/failure resident cases, real cross-volume/unsupported-handler behavior and original-profile/package parity.

### 2026-09-06 - Original DOS rejects MakeLink soft mode as not implemented

A separate derivative invokes the same native MakeLink entry without HARD. Real MakeLink receives soft flag1 and a nonzero target-name pointer, with no target Lock/Examine/FIB operations. Original DOS returns0/IoErr236 (NotImplemented); the command prints fault236 before FreeArgs and returns20. One workspace allocation/free balances, image unchanged and no active invocation remains. Original Type subsequently cannot match the alias (205), consistent with no link creation.

The success-profile verifier correctly rejects this run; no soft-link success credit was taken. Added explicit --soft --unsupported verification requiring the236 failure, return20, no target ownership, diagnostic/parser order and absent alias. A wrong-error mutation is rejected; original hard-link trace remains passing. Soft-success verifier support exists but has no passing original-runtime evidence.

Evidence: artifacts/copy-boot-fixture/makelink-soft-media.json, makelink-soft.trx, makelink-soft-observations.json and verified-makelink-soft-unsupported.json. Production unchanged; shipping0/200. A supporting provider is still required for successful/dangling soft-link validation. This original Kickstart failure is capability evidence, not permission to omit MorphOS soft-link functionality.

### 2026-09-06 - MakeLink resident duplicate-name failure recovery verified on original DOS

Recovered the completed makelink-recovery.trx (Passed), without restarting the test. The real Shell invokes one loaded segment three times: hard-link success, duplicate alias failure203/return20, then second alias success. All three use real ReadArgs and original DOS Lock/Examine/MakeLink. Each has one allocation/free, unchanged image and exact FIB -> lock -> parser cleanup; the failed call prints fault203 before cleanup. Original Type reads both exact18-byte payloads after successful RemSegment and matching segment FreeMem.

Added verify_makelink_resident_recovery.py for the intact three-invocation trace, enforcing single load, same task/segment, return sequence0/20/0, sequential library leases, ownership/order, independent contents/size and removal/free. Nine targeted mutations reject wrong segment, result, error, FIB/lock/parser cleanup, payload, removal result and free size. Initial control selection accidentally targeted unrelated Shell UnLock/FGetC events; restricted mutations to the failed command lease and alias readback before recording passing controls. No production change was needed.

Evidence: artifacts/copy-boot-fixture/makelink-recovery-media.json, makelink-recovery.trx, makelink-recovery-observations.json, verified-makelink-recovery.json and makelink-recovery-controls.json. Shipping remains0/200; forced PURE is not installed purity proof. Next MakeLink requirements include directory FORCE behavior, link identity after source deletion, concurrency, successful soft-link provider and original-profile/package parity. Full goal remains active.

### 2026-09-06 - Original-DOS hard-link aliases survive removal of original filename

A new disposable derivative repeats MakeLink success/duplicate203/success, removes the resident command, then invokes original C:Delete on RAM:link-source before original Type reads either alias. The completed boot test passes; DeleteFile(link-source) returns true with IoErr0. Both aliases independently return the exact18-byte payload and size after deletion. Existing real-parser, per-invocation ownership, unchanged image and resident removal/free checks also pass.

Extended verify_makelink_resident_recovery.py with explicit --source-deleted mode requiring successful deletion of the intended filename after segment release and before each alias read. The prior recovery trace remains passing in ordinary mode, but cannot qualify deletion survival. Four negative controls reject failed deletion, unrelated filename, deletion after readback and the prior no-deletion trace. This establishes survival after original-name removal, not mutation through aliases, all filesystem implementations, installed purity or full MakeLink parity.

Evidence: artifacts/copy-boot-fixture/makelink-identity-media.json, makelink-identity.trx, makelink-identity-observations.json, verified-makelink-identity.json and makelink-identity-controls.json; ordinary regression verified-makelink-recovery-regression.json. Production HUNK unchanged. Shipping0/200; full goal active. Directory FORCE, concurrency, successful soft links, remaining original profiles and packaging remain open.

### 2026-09-06 - Original-DOS directory hard links require FORCE and permit child access

A new disposable startup creates RAM:link-dir/payload, then invokes the same loaded MakeLink image without and with FORCE. The completed original-DOS boot test passes. Returns are20/0: the unforced call examines a directory, prints the exact FORCE-required line and never calls MakeLink; the forced call passes the acquired directory lock with hard flag0 and succeeds. Both invocations release FIB, lock, parser, workspace and library with unchanged image. Resident removal and segment free succeed. Original Type then reads18 exact payload bytes through the startup's RAM:dir-alias/payload path.

Added verify_makelink_directory.py requiring the intact two-call trace, single image, real ReadArgs, directory FIB, diagnostic, hard-link ABI/result, cleanup identities/order, segment removal and child size/bytes. Five negative controls reject missing diagnostic, regular-file classification, wrong rejection FIB cleanup, failed forced link and wrong target lock. The observer does not capture MatchFirst's input string; the exact alias traversal pathname is provided by the hash-bound startup/media fixture, while the trace records matched child and readback. Do not treat this as an arbitrary-path verifier or original-command parity proof.

Evidence: artifacts/copy-boot-fixture/makelink-directory-media.json, startup-makelink-directory.txt, makelink-directory.trx, makelink-directory-observations.json, verified-makelink-directory.json and makelink-directory-controls.json. Production unchanged; shipping0/200. Successful soft-link support, concurrency, remaining Workbench/MorphOS reference parity and packaging remain open. Full goal remains active.

### 2026-09-06 - Directory alias traversal is now observed directly at MatchFirst

Extended the passive DOS observer's existing bounded D1 string capture to MatchFirst. No guest memory, CPU, command or filesystem behavior changes. Rebuilt the harness in match-path-build (success, one existing xUnit warning) and reran the unchanged directory media. makelink-directory-path.trx passes; real MatchFirst receives RAM:dir-alias/payload and returns0. The full directory verifier still passes directory rejection/forced creation, cleanup, resident release and exact18-byte child readback.

verify_makelink_directory.py now requires the observed alias traversal path. The prior trace is intentionally insufficient under this stronger verifier; historical receipts remain preserved with their original verifier hash. Three new negative controls reject the source path substituted for the alias, absent text and another child name. This closes the previously documented path-observation gap without inferring traversal from startup alone.

Evidence: artifacts/copy-boot-fixture/match-path-build.log, makelink-directory-path.trx, makelink-directory-path-observations.json, verified-makelink-directory-path.json and makelink-directory-path-controls.json. Candidate HUNK and media unchanged. Shipping remains0/200; full original-profile parity, soft-link success, concurrency and packaging are still required.

### 2026-09-06 - Original Workbench MakeLink exposes a distinct directory profile

Extracted original MakeLink into private test data and confirmed the previously inventoried700-byte SHA c24b0af713e6f3a252d964c4545da29535bf6652394879a7dc4c57fdce7c02de. Prepared a separate derivative using the same directory startup. Removed the observer's generic positive-direct-allocation assertion: original commands can use stack storage; allocation balance remains mandatory and candidate verifiers still require exact positive counts. Rebuilt reference-command-build successfully and completed makelink-wb31-directory.trx (Passed).

Original Workbench returns20/0 with unchanged image and zero direct Exec allocations. It uses the same ReadArgs template but emits different FORCE text through VPrintf, calls PrintFault(0) on rejection, locks RAM: in addition to the target directory, and has different parser/lock cleanup order. Saved hash-bound side-by-side public-call observations in makelink-directory-profile-comparison.json. These are actual differences, not parity. The generic MorphOS-derived implementation cannot be admitted for the Workbench profile on this evidence.

Evidence: artifacts/copy-boot-fixture/makelink-wb31-directory-media.json, makelink-wb31-directory.trx, makelink-wb31-directory-observations.json, makelink-directory-profile-comparison.json and reference-command-build.log. Original binary is private and excluded from shipping. Shipping remains0/200. Next action is Workbench-specific destination-parent/cycle/error reference coverage and an explicit Workbench implementation/profile; do not change the MorphOS body to erase documented profile differences. Full goal remains active.

### 2026-09-06 - Workbench missing-parent failure and ancestor-walk control flow established

Ran original Workbench MakeLink with HARD FORCE into RAM:missing/dir-alias, followed by a valid alias from the same resident image. Test passes with returns20/0 and unchanged image. First invocation locks source directory, fails Lock(RAM:missing) with205, never reaches MakeLink, prints fault205 and frees parser. Added receipt/hash-bound verify_makelink_wb31_parent.py and saved verified-makelink-wb31-parent.json. This is reference behavior, not candidate qualification.

Local disassembly of the already hash-verified private700-byte original identifies its directory helper: Examine then PathPart(-876), temporarily terminate FROM at that boundary, Lock parent, restore FROM, compare SameLock(-420) and walk ParentDir(-210), releasing ancestor locks. Constants checked against SDK LVO declarations. This explains the extra lock as a loop check; runtime loop evidence remains required. HARD slot appears unread in inspected body; default-kind runtime confirmation remains pending. Noted that failed-parent source-lock release is outside the command lease, so no blanket lock-cleanup parity claim was made.

Evidence: artifacts/copy-boot-fixture/makelink-wb31-parent-media.json, startup-makelink-wb31-parent.txt, makelink-wb31-parent.trx, makelink-wb31-parent-observations.json and verified-makelink-wb31-parent.json. Updated MakeLink contract with bounded runtime/static findings and next implementation dependencies. Shipping0/200; full goal active. Next: confirm loop and default-kind cases, then implement Workbench-specific behavior without changing MorphOS semantics.

### 2026-09-06 - Workbench loop/default reference confirmed; distinct body implemented

Completed makelink-wb31-loop-default.trx (Passed). Original Workbench rejects a direct directory loop even with FORCE, outputs its loop format and never calls MakeLink. The subsequent regular-file invocation without HARD succeeds with MakeLink flag0. Same resident segment returns20/0 unchanged. Saved observations and bounded summary; omission of HARD is bound to the startup/media fixture, not inferred from the template.

Added Workbench31MakeLinkCommand.cs, independently structured around the verified profile: ReadArgs, always-hard link mode, missing-target diagnostic, directory parent/ancestor SameLock loop check before FORCE, Workbench-specific text, exact selected error returned to startup, and invocation-owned parser/FIB/lock lifetime. PathPart truncation is restored immediately after Lock. Unlike reference error paths that leave target cleanup outside the command lease, the new body explicitly releases its own target lock on all paths for resident safety. MorphOS behavior remains separate and unchanged.

Native C# project builds successfully (workbench-makelink-body-build.log). This is initial implementation only: native entry, HUNK execution/ABI, profile error parity, concurrency and packaging remain open. Next turn should qualify this new body rather than treating the build as command completion. Evidence: artifacts/copy-boot-fixture/makelink-wb31-loop-default-media.json, startup-makelink-wb31-loop-default.txt, makelink-wb31-loop-default.trx, observations and summary. Shipping0/200; full goal active.

### 2026-09-06 - Workbench MakeLink entry compiled and five native scenarios pass original DOS

Added Workbench31MakeLinkEntry, linked its body into the native qualification root, and created compile_workbench_makelink_native.ps1. Initial root build identified the missing explicit source include; fixed it and preserved the failed log. Both direct compilation and reusable driver succeed on68000/68020/68040 with11 reachable methods and no runtime dependencies, exception regions or fatal sites. Artifacts/workbench-makelink-native-driver/compilation.json is compilation-only evidence.

Executed the68000 body in one forced-resident five-call startup using original DOS: returns20/0/20/20/0 for unforced directory, forced directory, loop, missing parent and default hard file link. Exact Workbench diagnostics and fault0/0/205 match the bounded reference cases. Both directory-child and default alias paths resolve and independently read18 exact bytes. Added verify_workbench_makelink_native.py asserting per-call parser/FIB/lock/library ownership, same image/returns, link flags/results, error paths, resident removal/free and readback. Five targeted mutations are rejected. Production body needed no change after compilation.

Evidence: artifacts/copy-boot-fixture/workbench-makelink-native-media.json, workbench-makelink-native.trx, workbench-makelink-native-observations.json, verified-workbench-makelink-native.json, workbench-makelink-native-controls.json and driver/build logs. The runtime bound was unchanged. These are five actual candidate paths, not complete Workbench parity or three-CPU runtime qualification. Shipping0/200; full goal active. Next: parser/target/link failures, multi-level ancestors, startup and concurrency, followed by profile packaging.

### 2026-09-06 - Workbench candidate parser/target/duplicate errors match original reference

Prepared identical five-call error/recovery startup on separate private original/candidate media and ran both original-DOS boots to completion (both Passed). Both sequences return0/20/20/20/0, with faults116/205/203 and null headers. Missing-target VPrintf format matches; failed parsing and target lock prevent link calls, while duplicate MakeLink returns0/203. Both first and final aliases independently read18 exact payload bytes after recovery.

Added compare_workbench_makelink_errors.py with semantic comparison plus candidate per-invocation parser/FIB/target ownership and unchanged resident image assertions. Candidate allocates/frees one workspace percall; reference has no direct Exec allocations. Five negative controls reject wrong parser/target errors, duplicate success, incorrect lock release and altered diagnostic format. No production change required. This is bounded behavioral parity, not exact rendered error-text or full IoErr-timing equivalence.

Evidence: artifacts/copy-boot-fixture/workbench-makelink-errors-{reference,candidate}-media.json, corresponding TRX/observations, verified-workbench-makelink-errors.json and workbench-makelink-errors-controls.json. Candidate hash remains1ad973bf9ff05e38a0fc1fe5424451f1374456ff2ecdaf365c699a48a9719a0e. Shipping0/200; full goal active. Remaining Workbench MakeLink work includes deeper ancestors, startup/error exhaustion, three-CPU native execution/concurrency and packaging.

### 2026-09-06 - Workbench native failure/ancestor/interleaving suite passes all three CPUs

Added separate WorkbenchMakeLinkEntrySuite and dispatch, reusing existing invocation parser backing while keeping MorphOS tests unchanged. Eleven cases cover file defaults/explicit HARD, three ancestors, FORCE rejection, loop at third comparison, missing parent/target, allocation/examine/link failures and parser failure. Two interleaved directory calls add success-versus-loop resident coverage. Fixture tracks each lock acquisition/release, validates PathPart restoration at parser release/link call, exact VPrintf argument rendering, errors and invocation DOS base. Strengthened initial fixture handles to be distinct per invocation, then reran successfully.

New qualify_workbench_makelink_native.ps1 passes39 native invocations across68000/68020/68040. Existing MorphOS suite passes54 under the same runner, with no shared image writes in either profile. Candidate production HUNK unchanged. Artifacts/workbench-makelink-native-qualified contains compilation and runtime receipts; distinct-locks artifacts retain MorphOS regressions and prior pass stages are preserved.

These tests supply DOS vectors and do not qualify original handler concurrency, startup behavior or installed purity. Shipping0/200; full goal active. Next priorities include original deeper-ancestor behavior, startup parity, remaining secondary-error/output details, and profile packaging once gates are satisfied.

### 2026-09-06 - Workbench MakeLink missing-library startup fixed and60 native cases pass

Added missing DOS, Workbench with/without DOS, negative length, null buffer and interleaved missing-DOS/success cases. The old HUNK fails specifically with stale31337 instead of original startup122. Corrected Workbench31MakeLinkEntry to obtain current process via Exec.FindTask and write pr_Result2 directly when DOS cannot open. No DOS vector is used on that path; WB reply stays under Finish ownership.

Extended the fixture with narrowly declared current-process Result2 write ownership. Initial correct write was rejected by the old guard; preserved that failure. New assertion reads actual guest Result2, and reports it rather than substituting the fixture IoErr variable. All60 native invocations pass on68000/68020/68040 including20 perCPU and the interleaved failure/success pair. Wrong-field test-only HUNK changes the unique MOVE.L displacement148 to152; memory guard rejects write at0x10098. All54 MorphOS regression cases pass unchanged.

Evidence: artifacts/copy-boot-fixture/workbench-makelink-startup-old.log, workbench-makelink-startup-qualified.log, workbench-makelink-startup-owned-qualified.log; artifacts/workbench-makelink-startup-owned-qualified/qualification.json, runtime/static reports, process-field-control.json and wrong-process-field.log. Production entry HUNK changed; prior original-DOS receipts must not be relabeled as coverage of its new hash. Shipping0/200; full goal active. Remaining work includes original-DOS rerun, full startup/profile parity, actual concurrency and packaging.

### 2026-09-06 - Startup-fixed MakeLink revalidated on original DOS; candidate/ledger records synchronized

Executed the current3184-byte Workbench68000 HUNK through the unchanged five-case startup on a new private derivative. Test and verify_workbench_makelink_native.py both pass: returns20/0/20/20/0, exact diagnostics/faults, balanced parser/FIB/locks/library/workspace, unchanged resident image, removal/free and two18-byte alias readbacks. Current binary SHA fd42d840165936c2dc4ff5980319b47d1e1dd2ccb6e466c48c03548c7ae1050c is bound through the new media receipt; prior receipts were not relabeled.

Packaging audit found neither MakeLink implementation recorded and ledger rows still showing wholly open implementation/native work. Added separate makelink-development-candidates.json for six profile/CPU artifacts, validating native report image hashes/counts before recording them. Bound the new Workbench68000 original-DOS result. Kept this outside the historical accepted packaging manifest and its frozen checkpoint audit; every candidate explicitly remains non-shipping with no PURE admission or installed path. Updated only the two MakeLink ledger rows to partial progress and linked ML1 evidence/remaining work.

Evidence: artifacts/copy-boot-fixture/workbench-makelink-startup-fixed-media.json, workbench-makelink-startup-fixed.trx, observations and verified-workbench-makelink-startup-fixed.json; docs/Commands/Workbench31MorphOS320/makelink-development-candidates.json. Shipping0/200; full goal active. Remaining gates include full profile/options/error coverage, original purity metadata/classification, original runtime concurrency, minimum stack and package integration.

### 2026-09-06 - Original-DOS concurrent MakeLink success and deeper loop rejection verified

Ran current Workbench candidate using original Run background process to reject RAM:link-dir/a/b/c/loop, with a foreground regular hard link. First trace proves two active tasks and returns0/20 but remains in Wait1 before removal/readback; verifier correctly rejects missing removal. Preserved it. A second derivative replaces that timed wait with an original source Type operation before attempting removal, retaining the same command invocations and unchanged execution bound.

Second test/verifier pass: two distinct tasks, same loaded segment, overlapping DOS leases, three ParentDir steps on the loop path, exact loop diagnostic/fault0 and no MakeLink for rejection. Foreground MakeLink succeeds. Per-task locks/FIB/parser/library/workspace all balance, image unchanged, no remaining active invocation. Resident removal and matching segment free occur after both returns; independent Type reads18 exact foreground bytes. Five negative controls reject sequential-only evidence, same task, image mutation, removal failure and wrong readback path.

Evidence: artifacts/copy-boot-fixture/workbench-makelink-concurrent-readback-media.json, corresponding TRX/observations, verified-workbench-makelink-concurrent-readback.json and workbench-makelink-concurrent-controls.json. Updated current candidate record with this bounded concurrency receipt. Prior timed-wait trace remains unqualified for removal/readback. No production changes. Shipping0/200; full goal active. Further purity/profile and installed packaging gates remain open.

### 2026-09-06 - Rename ordinary direct/directory behavior executes on original DOS

Moved CC12 work to Rename, whose detailed contract had only supplied-vector original execution. Extracted the pinned original1140-byte Rename into private test data, verified SHA ebff9d5f1401ca67fe9542bde304c603d21366f81852716c236c5c7ca1e49579 and prepared a direct plus two-source QUIET directory fixture. Added explicit Rename scope and bounded old/new path, ParsePattern, NameFromLock and SameLock observation. First run completed the direct Rename but hit the observer's untracked AllocVec cleanup assertion; preserved failure. Added AllocVec header/base/size tracking matched to subsequent FreeMem, without suppressing the ownership guard.

Rebuilt and reran unchanged media: Passed. Two original calls return0/0 and balance four vector allocations each. Direct mode ends preflight before ParsePattern/Rename; directory mode advances MatchNext232 before each successful Rename, ending preflight plus both searches. Three exact old/new operations observed, then original Type reads13/14 exact destination bytes after resident removal. verify_rename_reference_boot.py passes; three negative controls reject wrong destination, vector-free size and MatchNext result.

Evidence: artifacts/copy-boot-fixture/rename-reference-media.json, rename-reference-vector.trx, observations, verified-rename-reference-vector.json and rename-reference-controls.json; rename-vector-observer-build.log. Original binary remains private. Rename body remains absent: RN2-WB unstarted/repeated/omitted MatchEnd and bounded-path safety decisions still precede implementation. Shipping0/200; full goal active. MakeLink progress remains retained separately.

### 2026-09-06 - Rename unstarted/repeated MatchEnd returns on original DOS

Ran original Rename with missing TO, duplicate existing regular destination and a subsequent valid rename in one resident session. Test and dedicated verifier pass: returns20/20/0. Parser failure116 invokes MatchEnd without MatchFirst. Duplicate Rename failure203 invokes the same anchor's MatchEnd twice (preflight end and common failure end), both returning before fault reporting. Four command-owned AllocVec allocations balance percall, image unchanged, and final source recovery plus untouched target contents read back exactly.

Added verify_rename_matchend_reference.py and three negative controls. Recorded a safe replacement ownership decision: one MatchEnd per attempted search, including failed MatchFirst attempts, without unstarted or repeated calls; preserve visible results and selected faults and label the removed redundant calls as explicit differential normalization. Omitted-search cleanup and buffer-hazard policies remain open, and opaque matcher allocations are not claimed qualified from command-owned allocation counts.

Evidence: artifacts/copy-boot-fixture/rename-matchend-media.json, rename-matchend.trx, observations, verified-rename-matchend.json and rename-matchend-controls.json. Rename replacement remains absent pending remaining contract decisions; shipping0/200 and full goal active. Next: original multiple/wild-source nondirectory branch, then safe bounded Rename body.

### 2026-09-06 - Rename nondirectory return0/diagnostic confirmed; safety decisions frozen

Original multi-source and wildcard-source to regular destination both return0 with destination diagnostic, no Rename and no MatchEnd. QUIET does not suppress it. Recovery rename returns0 and original Type confirms recovered source, untouched second source and existing destination bytes. Dedicated verifier and three negative controls pass. Opaque matcher allocation freedom is not inferred from four balanced command vectors.

Recorded explicit replacement policy in Rename contract: one search end perattempt, added nondirectory cleanup while preserving ambient IoErr, no redundant/unstarted end; reject failed ParsePattern/NameFromLock before mutation using actual OS error, primary20 even for synthetic error0; empty successful destination name210; bounded256-byte complete path composition with terminator, reject overlong/unterminated paths120; preserve prior mutations and original MatchNext/QUIET semantics. Keep selected faults separate from final ambient cleanup error. These decisions permit the independent body next while preserving all original guarded hazard evidence.

Evidence: artifacts/copy-boot-fixture/rename-nondirectory-media.json, rename-nondirectory.trx, observations, verified-rename-nondirectory.json and rename-nondirectory-controls.json. No replacement Rename is claimed yet; shipping0/200 and full goal active. Next implement the full classic direct/directory body under this explicit safety policy, then qualify it against the original cases.

### 2026-09-06 - Workbench Rename body and three-CPU compilation

Added Workbench31RenameCommand with direct and directory modes, original ReadArgs template, QUIET behavior, private AllocVec state, public DOS operations, and the explicit bounded-path/search-ownership policy recorded in Rename.md. Added Workbench31RenameEntry and compile_workbench_rename_native.ps1. Managed build passes; native compilation passes on 68000/68020/68040 (4556/4604/4544 bytes, 11 reachable methods each). Compilation evidence: artifacts/workbench-rename-native/compilation.json. This does not establish runtime parity or resident admission.

Prepared a separate candidate derivative and started the original-DOS direct/directory fixture. Extended verify_rename_reference_boot.py with explicit candidate mode and additional directory FIB ownership assertions; the unchanged original-reference trace still passes. Candidate execution result remains pending. Shipping 0/200; full goal active.

### 2026-09-06 - Candidate Rename direct/directory original-DOS pass

The independent 68000 Workbench candidate (SHA-256 ffc200c9350e86e50c1173c73872c1b6bed8739398579ab71b4f97444eef0d28) passes the original-DOS direct/directory fixture. Two resident invocations return 0/0, share the unchanged segment and each balance four vector allocations. The additional candidate directory FIB is allocated and released by identity. Direct ParsePattern dispatch and directory MatchNext-before-Rename order match the reference checks; all three exact path operations succeed. After resident removal, original Type reads the exact 13/14-byte destination contents.

Evidence: artifacts/copy-boot-fixture/rename-native-media.json, rename-native.trx, rename-native-observations.json and verified-rename-native.json. Candidate verifier mode keeps this separate from original-reference evidence. Three negative controls reject wrong destination, incorrect FIB release and changed resident image (rename-native-controls.json). This covers ordinary 68000 behavior only; error/normalization and boundary cases, 68020/68040 runtime, concurrency, full profile, purity and packaging remain open. Shipping 0/200; goal active.

### 2026-09-06 - Candidate Rename parser/duplicate failure and recovery pass

The current 68000 candidate passes the original-DOS failure fixture (rename-native-errors.trx). Three resident invocations return 20/20/0: ReadArgs error 116, duplicate destination error 203, then successful recovery. Exact recovered and preserved-target bytes match the reference. Each invocation balances four command-owned vectors; the existing-destination FIB is released by identity. The shared image stays unchanged and resident removal succeeds.

verify_rename_matchend_reference.py now has explicit candidate mode: MatchEnd counts 0/1/1 versus original 1/2/1. This verifies the documented removal of unstarted/repeated cleanup without accepting it as exact call-trace parity. The original trace still passes its separate mode. Candidate evidence: artifacts/copy-boot-fixture/rename-native-errors-media.json, rename-native-errors-observations.json, verified-rename-native-errors.json and rename-native-errors-controls.json. Three controls reject wrong fault, repeated cleanup and wrong FIB release. Opaque matcher allocation freedom, final error timing, full output formatting, boundary failures, other-CPU runtime, concurrency, purity and packaging remain unqualified. Shipping 0/200; goal active.

### 2026-09-06 - Candidate Rename nondirectory cleanup and recovery verified

Current 68000 candidate passes the multi-source/wildcard-to-regular-file fixture. All three calls return 0, matching the original unusual rejection status; both rejection calls emit the destination diagnostic, including QUIET, with no Rename/MatchNext. Each candidate rejection ends its attempted search exactly once after the diagnostic, unlike the original omission. Recovery Rename succeeds. Original Type confirms exact recovered, untouched second-source and existing-target payloads. Four vector allocations per invocation, parser ownership and candidate FIB releases balance; resident removal succeeds and image stays unchanged.

Evidence: artifacts/copy-boot-fixture/rename-native-nondirectory-media.json, rename-native-nondirectory.trx, rename-native-nondirectory-observations.json, verified-rename-native-nondirectory.json and rename-native-nondirectory-controls.json. Candidate verifier mode explicitly records the cleanup normalization; original mode still passes. Three negative controls reject missing cleanup, suppressed QUIET diagnostic and wrong return. This is not proof of opaque matcher leak freedom or final IoErr timing.

Added rename-development-candidates.json, validating binary hashes for all three CPUs and receipt/image/observation/report hash chains for the three 68000 fixtures. Eight candidate original-DOS invocations are covered across ordinary, error and nondirectory fixtures. Other-CPU runtime, boundary cases, concurrency, full profile, purity and packaging remain open. No accepted packaging manifest changed; shipping 0/200 and full goal active.

### 2026-09-06 - Candidate Rename native startup/early-break suite passes on three CPUs

Added a separate workbench-rename-startup-vector-fixture in the shared native runner; the original-only Rename executor remains unchanged and hash-restricted. Ten candidate invocations per CPU (30 total) pass on 68000/020/040: missing DOS, Workbench message, Workbench with missing DOS, negative entry length, nonzero length/null pointer, pending Ctrl-C, and interleaved missing-DOS/break plus Workbench caller pairs. Existing runner guards verify current-process Result2=122 for missing DOS, exact startup results/errors, stack restoration, library/message ownership, ABI volatility and unchanged shared image. Break mask 0x1000 and fault304/null header are checked; no parser/workspace allocation occurs on these paths. These candidate startup policies do not establish full original startup parity.

Evidence: artifacts/workbench-rename-native/rename-{68000,68020,68040}.startup.json, hash-bound in rename-development-candidates.json. Build output: artifacts/copy-boot-fixture/rename-startup-runner-build.log; runner: artifacts/rename-startup-runner/bin/CopperOS.Commands.NativeExecution/release/CopperOS.Commands.NativeExecution.dll. Invoke the runner with candidate HUNK, CPU, output JSON and workbench-rename-startup-vector-fixture. Workbench MakeLink's existing 20-case 68000 suite passes as a runner regression (artifacts/rename-startup-runner/makelink-regression.json).

This adds actual candidate native execution on all three CPUs, restricted to startup/early-break scope. Full Rename body failure and boundary vectors, body concurrency, minimum stack, full profile, original startup comparison, purity and packaging remain open. Shipping 0/200; goal active.

### 2026-09-06 - Rename four allocation failure points qualified on three CPUs

Extended the separate candidate startup suite with failure at each of the four AllocVec requests and an interleaved first/fourth allocation failure pair. Sixteen invocations per CPU pass (48 total, including prior startup cases). Vector gateways assert sizes/flags/order, exact pointer ownership and cleanup order including null FreeVec calls. Generic allocation guards enforce no leaks or foreign writes. Each failure reports 103 with null fault header and return20; no ReadArgs or matching occurs. Shared image remains unchanged. This is supplied-vector candidate evidence, not real low-memory DOS execution or original error-timing parity.

Evidence: artifacts/workbench-rename-native/rename-{68000,68020,68040}.allocation.json, hash-bound in rename-development-candidates.json. Runner build artifacts/rename-allocation-runner; build log artifacts/copy-boot-fixture/rename-allocation-runner-build.log. Earlier startup-only reports remain preserved. Body parser/matcher/path boundary vectors, successful body concurrency, full profile and shipping/purity gates remain open. Shipping 0/200; goal active.

### 2026-09-06 - Rename parser failure native coverage on three CPUs

Candidate suite now executes ReadArgs failure116 and synthetic failure with IoErr0, separately and interleaved. Twenty cumulative invocations per CPU pass (60 total including startup/allocation cases). Assertions check exact FROM/A/M,TO=AS/A,QUIET/S template, private result-buffer identity, three initially cleared argument cells, null supplied RDArgs, primary20 in both cases, fault116/null header only for nonzero error, four owned buffer releases and no unstarted MatchEnd or FreeArgs. Shared image remains unchanged. Synthetic error0 is not claimed to arise from original DOS.

Evidence: artifacts/workbench-rename-native/rename-{68000,68020,68040}.parser.json, bound by image/report hashes in rename-development-candidates.json; runner artifacts/rename-parser-runner and build log artifacts/copy-boot-fixture/rename-parser-runner-build.log. Historical smaller suite reports remain preserved. Matcher/path-boundary vectors, successful body concurrency, full profile and shipping/purity gates remain open. Shipping 0/200; full goal active.

### 2026-09-06 - Reproducible Rename candidate qualification driver

Added tools/Commands/qualify_workbench_rename_native.ps1. It requires a fresh output directory, compiles all three resident HUNKs through the existing static gate, builds an isolated native runner, executes the current 20-case suite on each CPU and verifies suite/CPU/count/image-hash/image-write/non-shipping report fields. It records per-CPU report hashes and emits qualification.json only after all checks succeed. It does not claim full command qualification.

Executed successfully with -DotnetPath C:/D-drive/Koodit/GIT/CopperOS/obj/dotnet-sdk-10.0.301/dotnet.exe -OutputDirectory C:/D-drive/Koodit/GIT/CopperOS/artifacts/workbench-rename-entry-qualified. All 60 native invocations pass; rebuilt binary hashes match the existing development candidates, retaining their bounded original-DOS evidence. Driver receipt is bound in rename-development-candidates.json. Build/run log: artifacts/copy-boot-fixture/rename-qualification-driver.log. Next extend candidate vectors into matcher/path failures and successful body concurrency; full profile, purity and packaging remain open. Shipping 0/200; goal active.

### 2026-09-06 - Native initial matcher failure and parser ownership

Extended candidate vectors through successful ReadArgs into MatchFirst failure. Supplied MatchFirst D0=103 with independent IoErr214 returns20/fault214; synthetic IoErr0 returns0 without a fault, preserving the original selected-error policy. Both cases run sequentially and interleaved. Assertions check private parser storage, cleared argument cells, matcher anchor/break configuration, one MatchEnd per attempted search, parser release after MatchEnd and exact four-vector cleanup. No Lock/Rename gateway is available to accidentally accept later mutation. Twenty-four cumulative invocations per CPU pass (72 total).

Updated qualification driver expected coverage and ran it successfully into artifacts/workbench-rename-matcher-qualified. qualification.json and per-CPU runtime reports are bound in rename-development-candidates.json; binary hashes remain unchanged. Log: artifacts/copy-boot-fixture/rename-matcher-driver.log. Build retains three existing nullable warnings outside the Rename suite, zero errors. Supplied vectors do not prove real-DOS synthetic error combinations or opaque matcher leak freedom. Remaining: error205 prefix, successful matcher/direct/path-boundary vectors, successful body concurrency, full profile, purity and packaging. Shipping 0/200; full goal active.

### 2026-09-06 - Rename initial error205 diagnostic survives output failure

Added initial MatchFirst IoErr205 case and interleaving with synthetic zero-IoErr failure. VPrintf asserts the exact prefix template, private argument storage and old/new strings, captures exact output bytes, then deliberately returns -1 and changes IoErr to999. Native code restores205 before MatchEnd/PrintFault; assertions verify that order and fault205/null header. Twenty-seven cumulative invocations per CPU pass (81 total), with unchanged binaries and shared image, balanced parser/vector storage. This tests selected-error preservation under supplied output failure, not exact real-DOS output-failure timing.

Evidence: artifacts/workbench-rename-prefix-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-prefix-driver.log. Qualification driver coverage count updated. Successful matcher/direct/path-boundary vectors, successful body concurrency, full profile and shipping/purity remain open. Shipping0/200; goal active.

### 2026-09-06 - Native direct Rename success and pattern rejection

Extended candidate vectors through successful MatchFirst, missing destination Lock, completed preflight and ParsePattern. Direct success asserts Rename uses the parsed private source buffer and original TO string. ParsePattern failure120 and synthetic failure/IoErr0 both return20 without Rename; the zero-error case emits no invented fault. A successful ParsePattern output containing256 non-NUL bytes is rejected120 before Rename. The success/unterminated pair also executes interleaved with private buffers/parser storage. Thirty-three cumulative cases per CPU pass (99 total), unchanged binary/image and full tracked cleanup.

Evidence: artifacts/workbench-rename-direct-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-direct-driver.log. Driver count/scope updated. This is supplied-vector boundary evidence, not proof that original DOS emits unterminated output. Exact-capacity valid path, directory composition/NameFromLock boundaries, broader direct errors and successful directory concurrency remain required along with full profile, purity and packaging. Shipping0/200; goal active.

### 2026-09-06 - Rename exact-capacity valid parsed path

Added a successful ParsePattern result with255 source bytes and NUL at byte255. Rename receives all255 bytes unchanged, using its private source buffer and original TO. Sequential and interleaved execution against the256-byte unterminated rejection both pass on68000/020/040. This checks both sides of the buffer boundary without truncating valid input. Thirty-six cumulative invocations per CPU pass (108 total), with unchanged binary/image and balanced tracked ownership. Supplied Rename success does not establish that every filesystem accepts a255-byte component.

Evidence: artifacts/workbench-rename-capacity-qualified/qualification.json and per-CPU runtime reports, hash-bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-capacity-driver.log. Directory composition/NameFromLock boundaries, broader direct errors, directory concurrency, full profile, purity and packaging remain required. Shipping0/200; full goal active.

### 2026-09-06 - Native directory name failure and invalid-output boundaries

Added directory-mode cases with successful preflight, private destination lock, allocated/aligned FIB and Examine directory result. NameFromLock failure120 and synthetic failure/IoErr0 both return20; successful empty output rejects210, and256 non-NUL bytes reject120. No case reaches ParsePattern or Rename. Assertions verify256-byte output capacity, preflight cleanup before NameFromLock, FIB release by identity, one destination unlock and parser/vector cleanup. Failure/unterminated cases also run interleaved with distinct owned locks and buffers. Forty-two cumulative invocations per CPU pass (126 total), unchanged binary and shared image.

Evidence: artifacts/workbench-rename-name-qualified/qualification.json and per-CPU runtime reports, hash-bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-name-driver.log. These are supplied-vector invalid-output policies, not original unsafe-path reproduction. Valid directory prefix/basename composition boundaries, successful directory matching/concurrency, wider option/errors, full profile and shipping/purity remain open. Shipping0/200; goal active.

### 2026-09-06 - Directory composition limits and successful native interleaving

Added normal OUT: prefix,251-byte colon-terminated prefix plus4-byte basename (255-byte valid path), and252-byte prefix plus4-byte basename (256-byte rejection). Successful cases traverse two supplied source entries under QUIET. MatchNext overwrites anchor name/path buffers before Rename; the command still supplies intact private SRC:file and composed destination strings. Overflow rejects120 before MatchNext/Rename and ends the attempted search. Normal and capacity-success callers also execute interleaved with distinct locks/FIB/parser/vector storage and shared unchanged image. Forty-seven cumulative invocations per CPU pass (141 total).

Evidence: artifacts/workbench-rename-compose-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-compose-driver.log. Rebuilt HUNK hashes unchanged. Supplied duplicate source entries are control-flow/ownership fixtures, not real filesystem duplicate-move semantics. Slash-appended prefix boundaries, nonquiet output, directory mutation failures, original-DOS concurrency and full profile/purity/packaging remain required. Shipping0/200; goal active.

### 2026-09-06 - Slash-appended directory path boundaries

Added250-byte non-colon prefix plus slash and4-byte basename (valid255-byte destination),251-byte prefix plus slash/basename (256-byte rejection), and255-byte prefix lacking room for slash/NUL. Success preserves the entire composed name; both overflow cases return20/error120 without Rename. The no-room case stops before beginning another match; attempted searches remain balanced by the existing equality guard. Valid and overflow callers also execute interleaved. Fifty-two cumulative cases per CPU pass (156 total), unchanged binary/image and full tracked cleanup.

Evidence: artifacts/workbench-rename-slash-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-slash-driver.log. Nonquiet progress output, directory mutation failures, broader matcher/options, original-DOS concurrency, full profile and purity/packaging remain required. Shipping0/200; full goal active.

### 2026-09-06 - Directory progress output failure does not suppress Rename

Added nonquiet directory traversal with exact two progress lines, private format-argument identity and MatchNext -> VPrintf -> Rename ordering. VPrintf captures each requested line then returns -1 and sets IoErr999; subsequent supplied Rename succeeds and sets IoErr0. Command returns0 and completes both operations. Nonquiet failure-output and QUIET success callers also execute interleaved, checking isolated output streams and owned buffers. Fifty-five cumulative cases per CPU pass (165 total), unchanged binaries/image and balanced tracked resources. Captured requested bytes do not claim that a failing real console would display them fully.

Evidence: artifacts/workbench-rename-progress-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-progress-driver.log. Directory mutation failures, broader matcher/options, original-DOS concurrency, full profile and purity/packaging remain required. Shipping0/200; full goal active.

### 2026-09-06 - Directory second-operation failure after success

Added supplied first Rename success followed by second Rename failure203. The command performs exactly two calls, prints exact SRC:file/OUT:file failure prefix despite QUIET, restores203 after failing VPrintf poisons IoErr999, ends the active search and reports fault203/null header with return20. No retry or compensating Rename occurs. Interleaving with a fully successful directory caller passes with private ownership and output. Fifty-eight cumulative invocations per CPU pass (174 total), unchanged binary/image and balanced tracked resources. This verifies operation sequence; the supplied vector fixture does not itself prove persisted filesystem state after partial failure.

Evidence: artifacts/workbench-rename-partial-qualified/qualification.json and per-CPU runtime reports, hash-bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-partial-driver.log. Real-provider partial-failure persistence, broader matcher/options, original-DOS concurrency, full profile and purity/packaging remain required. Shipping0/200; full goal active.

### 2026-09-06 - Original-DOS partial failure persistence verified

The 68000 candidate completes a real directory rename of RAM:first, then fails203 on RAM:second because target/second exists. A following resident invocation successfully renames the remaining second source to RAM:recovered. Returns20/0, exact operation paths/counts, failure prefix/fault ordering, search/parser cleanup, four balanced command vectors per call, unchanged image and resident removal pass. Original Type independently reads exact first-payload, existing-payload and second-payload bytes from target/first, target/second and recovered respectively. This establishes persisted partial success, preserved destination and recoverable source for this RAM-handler case.

Evidence: artifacts/copy-boot-fixture/rename-native-partial-media.json, rename-native-partial.trx, rename-native-partial-observations.json, verified-rename-native-partial.json and rename-native-partial-controls.json. New verify_rename_partial_boot.py rejects wrong first destination, wrong second error and altered payload. Receipt/report chain is recorded against current68000 candidate. Broader filesystem/error/option coverage, original-DOS concurrency, full profile and purity/packaging remain open. Shipping0/200; goal active.

### 2026-09-06 - Candidate directory FIB allocation failure cleanup

Added AllocDosObject failure103 and synthetic failure/IoErr0 after successful destination Lock and preflight. Both return20, only nonzero error produces a fault, and neither reaches Examine, FreeDosObject, NameFromLock or Rename. Search, parser, directory lock and four command vectors are released; failed FIB is neither used nor freed. Interleaving allocation failure with directory success passes. Sixty-two cumulative cases per CPU pass (186 total), unchanged binaries/shared image. This covers the candidate's additional private FIB allocation, not original stack-FIB parity or real-memory exhaustion.

Evidence: artifacts/workbench-rename-fib-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-fib-driver.log. Broader matcher/options, original-DOS concurrency, full profile and purity/packaging remain required. Shipping0/200; full goal active.

### 2026-09-06 - MatchNext break/error retains current-item processing

Added supplied MatchNext304 and103, each setting matching IoErr and overwriting the anchor buffers. Candidate still renames the saved current item, ends that search and starts the next supplied source pattern, where the behavior repeats. Supplied successful Rename sets IoErr0; final return0/no fault is asserted. The two callers also execute interleaved. Existing exact paths/counts/ownership guards remain active. Sixty-six cumulative cases per CPU pass (198 total), unchanged binaries/shared image.

Evidence: artifacts/workbench-rename-next-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-next-driver.log. This preserves the documented original nonzero-MatchNext control flow; it does not claim full real-signal timing parity. Single-source SameLock branches, later MatchFirst errors, real-DOS concurrency, full profile and purity/packaging remain required. Shipping0/200; goal active.

### 2026-09-06 - Later source-pattern failure205 without initial prefix

Added later MatchFirst205 after a completed first source operation. Candidate performs exactly one Rename, cleans up the failed later search, prints fault205/null header with no VPrintf prefix, and returns20. Interleaving with a fully successful directory caller passes. Sixty-nine cumulative invocations per CPU pass (207 total), unchanged binary/shared image and balanced tracked resources. This is supplied-vector call-flow evidence; persisted partial success is separately covered by the original-DOS duplicate-destination fixture.

Evidence: artifacts/workbench-rename-later-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-later-driver.log. Single-source SameLock dispatch, broader options, original-DOS concurrency, full profile and purity/packaging remain required. Shipping0/200; goal active.

### 2026-09-06 - Single-source directory dispatch and temporary lock ownership

Added one-source vectors with SameLock1 and-1, plus failure to lock the source after successful destination examination. All follow directory traversal and perform exactly one Rename. SameLock receives distinct source/destination BPTRs; successful temporary source locks must be released before final destination cleanup, while missing source lock has no fabricated unlock. SameLock1/-1 callers also execute interleaved with distinct process-owned locks. Seventy-four cumulative invocations per CPU pass (222 total), unchanged binary/shared image and balanced tracked resources.

Evidence: artifacts/workbench-rename-single-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-single-driver.log. SameLock0 direct fallback, broader options, original-DOS concurrency, full profile and purity/packaging remain required. Shipping0/200; goal active.

### 2026-09-06 - SameLock0 direct fallback

Added distinct source/destination BPTRs whose supplied SameLock result is0. Candidate releases temporary source lock, ends preflight, calls ParsePattern and performs exactly one direct Rename using parsed-old and original TO. It does not call NameFromLock or directory traversal. Destination FIB/lock and parser/vector ownership remain balanced. Interleaving this direct fallback with SameLock1 directory traversal passes on all CPUs. Seventy-seven cumulative invocations per CPU pass (231 total), unchanged binary/shared image.

Evidence: artifacts/workbench-rename-same-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-same-driver.log. This verifies dispatch based on SameLock semantics rather than BPTR equality. Broader option/reference coverage, original-DOS concurrency, full profile and purity/packaging remain required. Shipping0/200; goal active.

### 2026-09-06 - Original-DOS concurrent resident Rename succeeds

Two original Shell processes run the same resident68000 Rename image with separate two-source directory moves. The trace proves maximum active invocations2, distinct tasks and overlapping DOS leases. Both return0 with four command vectors balanced per task; parser/FIB/destination-lock identities match cleanup, and MatchNext precedes each of four exact Rename operations. Image stays unchanged. Resident removal succeeds after both calls complete; original Type reads exact background-one/two and foreground-one/two payloads from all four destinations. The intervening source Type is not assumed to synchronize; observed completion/removal order is required by the verifier.

Evidence: artifacts/copy-boot-fixture/rename-native-concurrent-media.json, rename-native-concurrent.trx, rename-native-concurrent-observations.json, verified-rename-native-concurrent.json and rename-native-concurrent-controls.json. New verify_rename_concurrent_boot.py rejects absent overlap, same-task substitution and wrong destination. Receipt/report hashes are bound to current68000 candidate. This is bounded same-image original-DOS concurrency, not full PURE admission or opaque matcher leak freedom. Full option/reference coverage, minimum stack, purity metadata and packaging remain required. Shipping0/200; goal active.

### 2026-09-06 - Explicit4096-byte candidate stack budget

All77 cases per CPU now explicitly configure4096-byte stacks instead of inheriting the runner's16384-byte default. Existing stack write bounds, upper/lower canaries and restored-SP checks pass in all231 invocations, including interleaved callers. Qualification driver rejects reports with a different configured budget or excessive written depth. Maximum observed candidate-written depths by CPU: {'68000': 204, '68020': 204, '68040': 204}. These measurements exclude real DOS/library stack consumption because vector calls are supplied; they establish neither an absolute minimum nor full real-system4096-byte safety.

Evidence: artifacts/workbench-rename-stack-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-stack-driver.log. HUNK hashes unchanged. Full reference/options, real-system stack qualification, purity metadata and packaging remain open. Shipping0/200; goal active.

### 2026-09-06 - Matcher filename/full-path terminator boundaries

Added supplied successful MatchFirst outputs with108 non-NUL filename bytes and256 non-NUL full-path bytes. The valid destination prefix leaves room for a normal name, so these specifically exercise source termination checks rather than destination overflow. Both reject120/return20 before MatchNext or Rename and release attempted search, FIB, directory lock, parser and command vectors. Interleaved malformed-name/malformed-path callers pass. Eighty-one cumulative cases per CPU pass (243 total) on4096-byte configured candidate stacks, unchanged binaries/shared image.

Evidence: artifacts/workbench-rename-anchor-qualified/qualification.json and per-CPU runtime reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-anchor-driver.log. These are explicit safe malformed-provider output policies, not claims that original DOS emits them. Full reference/options, real-system stack qualification, purity metadata and packaging remain open. Shipping0/200; goal active.

### 2026-09-06 - Rename contract coverage audit and ledger correction

Added rename-remaining-work.md mapping every contract row group to bounded evidence and remaining requirements. Revalidated latest243-invocation qualification/report hashes and original-DOS concurrency receipt. Corrected stale ledger/top-level contract prose that still called the body, other-CPU runtime and concurrency absent. No gate promoted to complete. Explicit next work: single-pattern multi-match traversal; Rename(FALSE)/IoErr0, PrintFault failure and cleanup-time IoErr; real ReadArgs aliases/quoting/help; handler/self/wildcard cases; original startup/real stack/purity; separate MorphOS50.8 body; packaging. Shipping0/200; full goal active.

### 2026-09-06 - Single source yields two distinct matches

Added one-source traversal where first MatchNext returns0 and replaces anchor file/SRC:file with next/SRC:next before the first Rename. Candidate still renames SRC:file to OUT:file, then on MatchNext232 renames SRC:next to OUT:next. Exactly two mutation calls and balanced preflight/traversal ends are required. Interleaving with a single-match source caller passes. Eighty-four cumulative invocations per CPU pass (252 total), unchanged binaries/image and4096-byte guarded candidate stacks.

Evidence: artifacts/workbench-rename-multimatch-qualified/qualification.json and per-CPU reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-multimatch-driver.log. Updated W01 audit status. Real wildcard grammar/provider behavior and later errors within one pattern remain open, along with other listed profile/purity/packaging gaps. Shipping0/200; goal active.

### 2026-09-06 - Synthetic direct Rename(FALSE)/IoErr0

Added direct Rename returningFALSE with IoErr0. Candidate emits the original raw FROM/TO prefix, restores zero after VPrintf fails and poisons IoErr999, performs no PrintFault and returns0. It still releases parser, vectors and preflight correctly. Interleaving with direct success passes. Eighty-seven cumulative cases per CPU pass (261 total), unchanged binaries/image and4096-byte guarded candidate stacks. This deliberately preserves original control flow and is explicitly synthetic, not evidence that a real handler uses this status combination.

Evidence: artifacts/workbench-rename-falsezero-qualified/qualification.json and per-CPU reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-falsezero-driver.log. E02 audit updated; directory variant, PrintFault/cleanup IoErr gaps and other full-profile/purity/packaging requirements remain open. Shipping0/200; goal active.

### 2026-09-06 - PrintFault failure and cleanup-time secondary error

Added ReadArgs failure116 with PrintFault returning0 and setting IoErr999; primary return remains20 and final observed secondary result999. Another failure116 reports normally, then each FreeVec sets774..777; final observed error777 remains visible instead of restoring selected116. Both run independently and interleaved, preserving private errors and cleanup. Ninety-one cumulative cases per CPU pass (273 total), unchanged binary/image and4096-byte guarded candidate stacks.

Evidence: artifacts/workbench-rename-ioerr-qualified/qualification.json and per-CPU reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-ioerr-driver.log. E04/R03 audit updated with this limited parser-failure coverage. These injected error mutations are synthetic; original final Process comparison, other APIs/success branches and full-profile/purity/packaging remain open. Shipping0/200; goal active.

### 2026-09-06 - Original-DOS positional wildcard and AS alias

Candidate68000 runs Ed RAM:wild-#? TO RAM:target QUIET followed by Ed RAM:target/wild-one AS RAM:renamed through the original Shell/ReadArgs. One wildcard produces two matches (MatchNext0 then232) and two correct directory mutations; AS selects the direct destination successfully. Both resident calls return0 with four balanced command vectors, parser cleanup and unchanged image. Resident removal succeeds; original Type reads exact wildcard-one/two payloads from renamed and target/wild-two.

Evidence: artifacts/copy-boot-fixture/rename-native-wildcard-media.json, rename-native-wildcard.trx, rename-native-wildcard-observations.json, verified-rename-native-wildcard.json and rename-native-wildcard-controls.json. New verifier rejects wrong wildcard, missing continuation and wrong AS destination. Hash chain bound to current68000 candidate. This covers these concrete grammar/effect cases, not all aliases, duplicate keywords, escapes or full original-command differential. Remaining profile/purity/packaging gates stay open. Shipping0/200; goal active.

### 2026-09-06 - Original/candidate wildcard and AS comparison passes

Ran pinned original Rename37.2 with exactly the candidate wildcard/AS startup script on the same original DOS/ROM and observer. Both pass the scenario verifier: wildcard has two successful effects, AS produces the expected direct move, returns0/0, no command diagnostics, exact final payload bytes and resident removal. compare_rename_wildcard_boot.py binds original hash, candidate binary hash, both media/observation receipts and identical startup/reference-disk/ROM/test-assembly identities. Comparison excludes full call-trace, final IoErr timing and full grammar coverage.

Evidence: artifacts/copy-boot-fixture/rename-reference-wildcard-media.json, rename-reference-wildcard.trx, rename-reference-wildcard-observations.json, verified-rename-reference-wildcard.json, verified-rename-native-wildcard-regression.json and compared-rename-wildcard.json. Comparison report bound in development candidates; W01 audit updated. Original executable remains private. Other options, interactive parser behavior, handler errors, real-system stack and purity/packaging remain open. Shipping0/200; goal active.

### 2026-09-06 - Directory Rename(FALSE)/IoErr0 counterpart

Added second directory operation returningFALSE/IoErr0 after supplied first success. Candidate prints exact SRC:file/OUT:file prefix despite QUIET, restores zero after output poisoning, ends the active search, performs no common fault and returns0. Exactly two Rename calls and balanced cleanup remain required; interleaving with full success passes. Ninety-four cumulative cases per CPU pass (282 total), unchanged binaries/shared image and4096-byte guarded candidate stacks.

Evidence: artifacts/workbench-rename-directoryzero-qualified/qualification.json and per-CPU reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-directoryzero-driver.log. E02 audit updated for both direct and directory synthetic combinations. Full real-provider/options, stack/purity and packaging gaps remain open. Shipping0/200; goal active.

### 2026-09-06 - Successful direct Rename retains cleanup-time IoErr

Added direct success with Rename returningtrue/IoErr0 followed by FreeVec setting774..777. Candidate returns primary0 with final secondary777 and emits no fault. It runs interleaved with parser failure whose cleanup also sets777; primary results remain separately0/20. Ninety-seven cumulative cases per CPU pass (291 total), unchanged binaries/image and guarded4096-byte candidate stacks.

Evidence: artifacts/workbench-rename-successio-qualified/qualification.json and per-CPU reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-successio-driver.log. R03 audit updated. Synthetic cleanup mutations establish candidate policy for these branches, not original final Process equivalence across every API. Other profile, real-stack, purity and packaging requirements remain open. Shipping0/200; goal active.

### 2026-09-06 - MorphOS50.8 source found and independent contract started

Re-read the hash-pinned official3.20 C source archive. It contains c/rename/rename.c and a50.8/27.11.04 version header matching the ISO tag. Recorded exact member hashes in artifacts/rename-morphos-source-inventory.json and behavior differences in rename-morphos50-source-contract.md. Key differences include2048-byte paths, parser10/nondirectory20, forced quiet for single-source directory case, Rename-before-MatchNext, AddPart/FilePart composition, and found-break return5. This invalidates any assumption that classic body can serve both profiles unchanged. Source header has AROS attribution but no explicit grant; archive has no license-named member. Reuse provenance and packed runtime equivalence remain open; no source copied into repo or code adapted. Other Workbench work remains available, so this is not a whole-goal blocker. Shipping0/200; goal active.

### 2026-09-06 - MorphOS Rename native ABI and safe ownership decisions

Verified SDK AnchorPath282/buffer280/foundBreak12 and public FilePart/AddPart vectors. Recorded classic-target workspace arithmetic4378/destination2330 separately from unverified PPC layout. Specified once-per-attempt search cleanup, preserved MorphOS mutation-before-MatchNext order, bounded saved diagnostic source, complete fallback destination without truncation, public AddPart composition, separate selected/ambient errors and private resident state. Additional diagnostic storage failure and safety normalizations require explicit candidate tests. Checked AROS public license attachment/tree provisions; no unsupported claim that a detached source archive or MorphOS modifications are automatically covered. No vendor code imported or adaptation begun. Full goal remains active; no gate closed.

### 2026-09-06 - Second distinct match fails within one source pattern

Added single-pattern file -> next traversal with first Rename success and second failure203. Diagnostic uses saved SRC:next/OUT:next even after MatchNext overwrites anchor buffers; exactly two mutation calls, one traversal end after failure, fault203/return20 and complete tracked cleanup are required. Interleaving with the fully successful two-match caller passes. One hundred cumulative invocations per CPU pass (300 total), unchanged binaries/image and4096-byte guarded candidate stacks.

Evidence: artifacts/workbench-rename-multifailure-qualified/qualification.json and per-CPU reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-multifailure-driver.log. Updated W01 remaining-work audit. This is supplied-vector partial sequence evidence, not additional real-handler persistence evidence. MorphOS source/provenance work remains separate and no full-profile/purity/packaging gate is closed. Shipping0/200; goal active.

### 2026-09-06 - Workbench Examine failure retains direct fallback

Added successful destination Lock/FIB allocation with Examine(FALSE)/IoErr222 despite a supplied positive type field. Classic candidate releases FIB, ends preflight and performs ParsePattern/direct Rename, rather than taking directory traversal or the MorphOS fatal-Examine policy. Supplied Rename succeeds with IoErr0; return0/no fault and destination-lock cleanup are required. Interleaving with normal directory success passes. One hundred three cumulative invocations per CPU pass (309 total), unchanged binaries/image and4096-byte candidate stacks.

Evidence: artifacts/workbench-rename-examine-qualified/qualification.json and per-CPU reports, bound in rename-development-candidates.json; log artifacts/copy-boot-fixture/rename-examine-driver.log. D02 audit updated; original handler injection/runtime parity for this case remains separate. Full profile, purity and packaging stay open. Shipping0/200; goal active.

### 2026-09-07 - Shared-runner MakeLink regression verified

Revalidated six completed MakeLink regression reports produced by the latest Rename-expanded native runner. Workbench20 and MorphOS18 cases pass on each of68000/020/040 (114 total), with report suite/count/CPU, binary hashes, unchanged-image/non-shipping fields and on-disk runner hash checked. All six use one runner binary. Evidence: artifacts/workbench-rename-examine-qualified/makelink-regressions.json and listed per-profile/CPU reports. This retains the previously qualified bounded MakeLink behavior while Rename coverage grows; it does not promote either command to shipping. Full goal active.

### 2026-09-07 - Independent MorphOS Rename body and complete bounded native checkpoint

Implemented NativeMorphOSRenameCommand and its DOS37 native entry without importing
vendor source. It preserves the source-observed50.8 differences: parser10,
nondirectory20,2048-byte paths, EntryType>=0, forced single-source quiet,
Rename-before-MatchNext, FilePart/AddPart, NameFromLock fallback and found-break5.
All parser cells/bases/state are invocation-owned; checked directory diagnostic
storage survives MatchEnd. The three executable variants compile with12 reachable
methods and zero forbidden native compatibility features. The HUNKs are4240/4320/
4228 bytes; exact hashes are bound in rename-development-candidates.json.

Qualification artifacts/morphos-rename-body-qualified/qualification.json passes
56 scenarios, each sequential and interleaved:112 invocations per CPU,336 total.
Maximum candidate stack write depth228 within guarded4096 bytes; this excludes
real DOS stack consumption. Tests include both allocation failures, FIB/Examine,
parser/matcher/rename zero and nonzero errors, quiet/progress, same-lock/type
selection, wildcard traversal, second-item Rename/AddPart failures, path
boundaries and ambient error effects. Independent review corrected two test
assumptions (PrintFault updates IoErr; require DOS37 exactly), with earlier
receipts retained. No confirmed production defect was found in that review.

The original-Kickstart-DOS68000 fixture also passes: ordinary direct rename,
two-match wildcard and AS alias return0/0/0, make four exact mutations, balance
tracked ownership and preserve the shared resident image. Three independent
Type reads after removal return17/13/13 exact bytes. Evidence:
artifacts/copy-boot-fixture/verified-morphos-rename-native.json. This does not run
the original packed MorphOS executable or establish complete MorphOS parity.

The latest shared native runner also passes the existing Workbench Rename309 and
MakeLink Workbench/MorphOS114 invocations (423 total), verified against on-disk
binary/report/runner hashes in shared-runner-regressions.json. Full contract,
actual CopperStart integration, minimum OS stack, installed metadata, complete
resident lifecycle and packaging gates remain open. Shipping0/200; goal active.

### 2026-09-07 - Current Workbench MakeLink error differential refreshed

The acceptance audit identified that the five-case error/recovery comparison
still bound a3140-byte older candidate. Replayed the current3184-byte fd42d840...
HUNK and the original700-byte command with the same current observer/emulator,
startup, ROM and disk. Both terminal tests pass and both return0/20/20/20/0;
parser116, missing-target205, duplicate203, mutation decisions, tracked cleanup
and exact alias readbacks match. Six negative controls reject old binary identity,
wrong returns/trace, changed observer, stale receipt and failed terminal results.
Evidence: artifacts/copy-boot-fixture/verified-workbench-makelink-errors-current-20260907.json,
now bound in makelink-development-candidates.json. Historical evidence is retained.

This closes the stale-current-binary evidence gap only. Rendered diagnostic bytes,
final process IoErr, original startup, real OS minimum stack, CopperStart command
launch, full resident lifecycle and image/protection admission still prevent a
complete Workbench MakeLink profile. Those command-specific gates can close
without waiting for unrelated command families; no global foundation excuse or
partial-as-shipping promotion was introduced.

### 2026-09-08 - MakeLink observable results/options and real stack checkpoint

Closed concrete bounded comparison gaps without changing the production HUNK.
The prior traces already captured pr_Result2 at public RunCommand return; a new
validator now checks all five current/reference results after cleanup. They match:
0/205,20/116,20/205,20/203,0/205. Eight semantic controls keep TRX/observations
mutually consistent and reject missing/changed results or ownership.

Two fresh original-DOS boots save each command's rendered output to separate RAM
files, read after resident removal. All five exact outputs and final errors match,
including two empty success files;12 negative controls pass. Two further fresh
boots compare nine ReadArgs scenarios: positional/reordered/equals keywords,
quoted spaces/escaped quote, FORCE regular target, repeated HARD, duplicate FROM,
unknown argument and missing TO. Six hard-link calls succeed; five aliases have
exact15-byte post-removal readback. Duplicate/unknown arguments fail118, missing
TO116. No command-private parser was added. Option traces have886/916 events and
both terminal tests passed; no trace cap or emulator behavior was changed.

The MedPlayer passive observer now separates same-task HUNK/callee SP samples
from prelude/postlude, supervisor and other tasks. One fresh actual current-HUNK
five-case boot passes with metadata-backed4096-byte stacks and896-byte observed
use below upper bounds,3200 observed SP headroom. There are no unknown/changed/
outside-bound samples. Independent review found no concrete attribution bug.
Twelve stack controls and12 original/candidate option controls reject corruptions;
these direct-inspection controls do not claim end-to-end TRX tamper coverage.

Reports under artifacts/copy-boot-fixture: verified-workbench-makelink-secondary-20260908.json,
verified-workbench-makelink-output-20260908.json, verified-workbench-makelink-options-20260908.json,
verified-workbench-makelink-stack-20260908.json. Exact report/control/HUNK hashes
are bound in makelink-development-candidates.json. The stack observer build is
isolated in artifacts/command-stack-observer-build; old observer builds remain.
No universal minimum-stack, complete original startup/grammar/handler parity,
CopperStart, full PURE/lifecycle or packaging gate is closed by these fixtures.

### 2026-09-08 - CC08 release metadata preflight implemented

Added tools/Commands/verify_command_release.py and22 adversarial tests. The tool
checks independent command/profile/CPU/placement selection, original inventory,
current source/compiler/SDK/input/artifact/dependency hashes, version bytes,
stack metadata, Amiga P policy and manifest collisions. Known native-vector
reports retain only bounded support; arbitrary passed booleans and unknown
schemas cannot discharge complete release gates. All27 current unqualified
records remain rejected. The actual manifest selection correctly exits1 with no
shipping artifact and no fallback (artifacts/command-release-preflight-20260908.json).
Tests rerun successfully; log command-release-preflight-tests-20260908.log.

The preflight is deliberately not an image builder or complete admission engine:
full gate-specific producers/adapters and actual staged-image/protection readback
remain required. See release-preflight.md. Current next work is the finite
remaining MakeLink acceptance list in its contract, particularly remaining
argument/startup/handler cases, CopperStart launch and release metadata/integration.
Shipping0/200; profiles0/246; full goal active. No unrelated goal was changed.

### 2026-09-08 - MakeLink help continuation and EOF compared and recorded

Two fresh paired original-Kickstart-DOS boots ran the known ReadArgs `?` trigger
through real Shell input/output redirection. The original ran first. A complete
argument line creates the expected hard link, returning 0 with final IoErr 205
and exact 28-byte template prompt. Empty input returns 20/116, emits the exact
54-byte template plus required-argument diagnostic, and makes no target mutation.
Independent C:Type runs after resident removal read both input files, both output
files, alias and source; empty input has affirmative EOF. Original/candidate
traces contain 639/645 events, preserve the existing execution bound, complete
all invocations, and retain the same observer/emulator/ROM inputs. Candidate
ownership balances and its shared image is unchanged.

The unchanged 3184-byte fd42d840 candidate alone receives this evidence in
makelink-development-candidates.json. Report
artifacts/copy-boot-fixture/verified-workbench-makelink-help-20260908.json has
SHA-256 9ff6ad3932df14400880aad15a2f8d3ddf1449d88970816103d60111440c5482.
All 20 negative controls reject their deliberate corruptions; controls report
workbench-makelink-help-20260908-controls.json has SHA-256
948906249a7e023ab1158a8720eaa5a27a5ad8673f998f3be17a88eee88f8bec.
The first control attempt targeted an unrelated boot ReadArgs call; the control
was corrected to select the command's DOS lease without weakening the verifier.
Interactive console/signals and full grammar/startup/PURE gates remain open.

### 2026-09-08 - Separate MakeLink versioned development records

Recorded the completed metadata build in
makelink-versioned-development-candidates.json, retaining all six older profile
records and their exact-HUNK evidence. Each new HUNK appends 48 immutable bytes
for `$VER: MakeLink 0.1 (8.9.2026) CopperOS wb31`, preserving original CODE and
tail bytes and changing only two original length words. The new 68000/020/040
images are 3232/3272/3228 bytes, respectively. Two fresh local build directories
produce identical raw and versioned output; raw images equal the old candidates.
Each versioned image passes the existing 20-case native-vector suite, 60 total.

Build qualification SHA-256 is
111e349e2921c077decdcf21543f3c1e9dbc90673c0c6ca750103ae62f1fc08b at
artifacts/workbench-makelink-versioned-20260908/qualification.json. Independent
preservation-audit.json in that directory has SHA-256
0efabee4fe0434497279b0b9e276eff40a849bdb113e74c94a3f8017e88eb053 and records
12 passing structural tests. Bounded independent source review found no concrete
supported-path defect in the append/build tools; exact tool hashes and limitations
are in the new records. The source/build snapshots are not a hermetic cross-host
closure. Native tests use 16384-byte stacks, so the declared 4096-byte minimum is
not qualified. Structural preservation does not transfer old-HUNK OS, help,
concurrency, stack or purity qualification. New-image Version/OS execution,
complete lifecycle, dependency/release/image/protection admission remain open.
An isolated Windows punctuation byte in MakeLink.md was normalized to UTF-8 to
allow ordinary document editing; its meaning is unchanged.

Shipping remains 0 of 200 commands and 0 of 246 profiles. The full goal is active.

### 2026-09-08 - Exact versioned MakeLink OS/Version and retained-image CopperStart evidence

Attached fresh original-DOS/Version receipts only to versioned 68000 candidate
12d1f36504f5a0f7099bc419e4fffc5f43f138d7b4e8745a2ca907bcb73b5f45 (3232 bytes).
Five success/parser/missing/duplicate/recovery cases complete with primary/final
errors 0/205, 20/116, 20/205, 20/203 and 0/205, exact independently read output,
balanced resources and alias readback after resident removal. Original C:Version
40.1 inspects the exact file and relocated CODE and returns 0/0 with complete
27-byte `MakeLink 0.1\nCopperOS wb31\n` readback. The date remains an embedded tag
fact, not rendered FULL output. The unchanged execution bound captures 925 events.

The receipt in artifacts/workbench-makelink-versioned-20260908/version-boot-complete/
verified.json has SHA-256
88e1928b49755de17e5a364f22619bdcf9134f8ab999469a881678f82191ba0c; controls.json
has SHA-256 56d818673650dbcc29999bd43ba055c8af478d1a4b0e87ce23ac932f190e3261.
All 14 controls reject corruption, including twelve semantic changes through the
complete verifier with mutually consistent terminal/observation data. No old-HUNK
evidence was relabeled as versioned-image execution.

Attached native CopperStart evidence only to retained 68000 candidate
fd42d840165936c2dc4ff5980319b47d1e1dd2ccb6e466c48c03548c7ae1050c (3184 bytes).
Five invocations execute real native production DOS RunCommand/ReadArgs,
formatting, MakeLink and packet-provider code with fixture Exec and a bounded
backing handler. They return 0/0, 20/203, 20/205, 20/116 and 0/0. Public DOS reads
prove both aliases retain payload after deleting the target name; the existing
destination and caller streams survive, resources balance, and command/DOS/vector
images are unchanged. This is native DOS/provider integration, not a full boot,
production filesystem, original-runtime differential or 4096-byte stack proof.

Two shared owner fixes were needed: ExecRawDoFmtCore now carries string length
directly instead of an odd-address word access to a numeric byte field, and
DosErrorCore supplies error 116's required-argument text and lowercase error 203
text. Six terminal tests pass: one native test with five invocations and formatter
alignment/length cases 0, 1, 255, 300 and 1024. Source and loaded-input hashes are
bound in artifacts/copperstart-makelink-native-integration/verified-final.json,
SHA-256 90782dbb243afa83d5141c04b1ae4d7b0e5d3f42c657d5453fdb911fd7b44372.
Earlier failed runs remain retained.

Updated both candidate record files, the MakeLink contract, completion ledger and
qualification report. Every new report/source/loaded-file binding was rehashed
before recording. Separate exact-image runtime, complete system/handler, startup,
minimum stack, PURE/resident and release/image/protection requirements remain
open. Shipping is still 0/200 commands and 0/246 profiles; the full goal is active.

### 2026-09-08 - Two build release gates implemented; runtime bridge next

The release preflight now accepts the known versioned Workbench MakeLink build
producer for native-static and reproducible-build only. It reconstructs captured
source and native DLL/JSON/support membership, checks current hashes, exact build
arguments and per-pass paths, validates compiler reachability/dependency reports,
and recomputes the version append before comparing both raw and versioned bytes.
Independent review identified and fixed cross-pass reuse, omitted-input and
trailing-build-argument gaps. All twelve actual-build integration checks and
22 metadata/rejection checks pass; semantic post-IO injections are distinguished
from file/hash tamper controls. The twelve HUNK structural tests also pass.

artifacts/command-release-build-adapters-20260908/verified.json binds three CPU
preflight results, current tools, tests, inventory and the still-empty shipping
manifest. Each result establishes exactly two build gates. Eleven other required
gates, including both PURE gates, remain open. These validation-only proposals
explicitly retain pure_admission=false and are rejected; no candidate was staged
or promoted. Local two-build byte reproducibility is not hermetic/cross-host
reproducibility or runtime behavior evidence.

Root independently reran the complete help/EOF and new Version evidence verifiers
and checked the six native CopperStart terminal results plus current source/input
bindings. Scoped whitespace checks pass. No commits or unrelated goal changes.

The next concrete filesystem issue is production packet routing, not absence of
all filesystem code: MedPlayer's ConfiguredDosHostHandler/ConfiguredDosVolume
already own host files and links. DosServices.ProcessConfiguredPacket currently
routes notification actions only; file operations use another managed entry path.
Work is continuing at that production packet owner so native commands can reach
the existing backend without a test-only replacement filesystem. No DiskBuilder
implementation was found in current source trees; a real packaging owner and
image metadata readback remain required. Shipping0/200; profiles0/246; goal active.

### 2026-09-08 - Production filesystem bridge and disk-image packaging owner

The immediate native-DOS/configured-filesystem routing gap is now implemented
at the production owner. MedPlayer's ConfiguredDosPacketDispatcher translates
guest lock and filehandle packet fields into the existing configured host
filesystem. DosServices publishes distinct volume ports and consumes native
Exec message queues, so stripped paths with a zero base lock reach the correct
volume. It does not substitute another command-private filesystem.

The versioned 68000 MakeLink HUNK, SHA-256
12d1f36504f5a0f7099bc419e4fffc5f43f138d7b4e8745a2ca907bcb73b5f45,
passes five invocations through actual native DOS and production host filesystem:
0/0, 20/203, 20/205, 20/116, 0/0. Both aliases retain content after target deletion;
diagnostics and cleanup pass. Twelve tests pass in the focused native/dispatcher/
formatter project. This native test still uses Exec allocation, delivery and
stack-swap fixtures. Separately, the production DosServices owner test passes six
genuine queued packets across DH0/DH1 with six replies and two locks freed once.
Neither result is relabeled as one combined native system/Shell boot.

The full production emulator builds with zero warnings/errors. The wider test
project has an unrelated duplicate-alias compilation failure; its log is retained,
and the owner regression ran in an isolated test assembly referencing the complete
production emulator. Source/artifact bindings were independently rehashed: 37 for
the native production-filesystem report and 52 for the owner-queue report.
See [integration scope and next steps](copperstart-filesystem-integration.md).

The SDK now matches the original NDK FileHandle ABI: fh_Arg1/fh_Args at byte 36,
fh_Arg2 at byte 40. The corrected struct/layout/codecs and raw-cookie regression
pass 128 focused checks. Source-level ordinary Arguments access remains an alias;
field/reflection/by-reference and precompiled users must rebuild. The corrected
SDK build passes 60 native command invocations and produces the same three
versioned HUNKs. The refreshed 34 release/build checks pass with zero skips;
exactly two build gates pass and eleven required gates remain open. Older build
source snapshots are preserved as history, not current SDK qualifications.

Created tools/DiskBuilder as the actual production packaging owner. It selects
only qualified_shipping_artifacts, invokes pinned external amitools in an isolated
source-snapshot subprocess, and independently verifies the resulting raw DOS1
ADF/HDF tree, payloads, protection bytes, epoch dates, checksums and exact bitmap
allocation. The final bitmap's nonexistent-block padding is canonicalized with
all valid bits and other bytes preserved. Output snapshots must match independent
readback before publication; final output is independently read again and the
receipt is written last. No existing output or live filesystem is overwritten.

All 26 DiskBuilder tests pass. Retained owned-data integration images include two
byte-identical ADFs generated from reversed input order (SHA-256
dc1d0011ee7c7d052145fb2ded8a7e7914a674f46247a282e612e565b4d0b7ab)
and a 52 MiB HDF with bitmap extension (SHA-256
8fb01b4d97297258db3679a1221b3dca3cd1493e48916c745e8b77bbfcd3055c).
Eight files cover an empty file, a file with extension blocks and seven raw
protection patterns. These are generic owned bytes, not command or PURE fixtures.
The actual release CLI rejects the current empty manifest with no image/receipt.
Evidence: artifacts/diskbuilder-qualified-20260908/verified.json, SHA-256
a917ec80c06dc2009ac034ae33abe6748ed327367ade6bafee5d2dc61bb51eec.
Earlier failed root-date, protection and bitmap integration attempts remain
historical evidence. See [DiskBuilder usage and limits](../../../tools/DiskBuilder/README.md).

Current blocking work is a combined native system/Shell launch with production
Exec delivery, complete exact-profile behavior/reference comparison, original
resident/shared-image lifecycle and minimum-stack qualification, and positive
admitted-command packaging/system execution. The filesystem bridge and image
writer are no longer missing. There is no user permission or input dependency.
Shipping remains 0/200 commands and 0/246 profiles; no goal checkbox or release
record is promoted by these bounded integrations.

The final read-only boot-owner audit narrows the next dependency further:
AmigaBoot.InstallBootHostTraps still installs DOS host gateways, and configured
volumes are published into the managed DosServices state. The generated native
DOS library therefore needs production admission plus matching volume publication
before a combined native run is possible. Existing BootController launch tests
provide nearby coverage but direct TryLaunchProgram/D0/A0 entry does not establish
the native RunCommand input lease. This is implementation work, not simply a
missing rerun of an existing end-to-end test. The new integration document names
the production owners and preserves the single existing Shell/process design.

### 2026-09-08 - Native DOS admission through production boot and Exec

The production application-session owner can now admit the identified native DOS
HUNK and publish configured-volume ports through public DOS calls. The combined
installation-stage run passes native initialization/publication, public Exec
OpenLibrary resolution, caller A6/stack restoration and native-vector checks.
The native image remains SHA-256
675560c38cac37a679db980e0595b393ce9417e7ae7fd0a09d4d226999cbbeb7.
The existing initial Task is not fabricated into a Process.

Actual failed runs exposed three production boot/Exec defects: missing library
callback registrations in application boot, a MakeLibrary/RawDoFmt continuation
address collision, and library callback entry using ExecBase in A6 instead of the
selected library. The shared callback owner now uses a distinct initialization
address and saves/restores caller A6 on the real guest stack. Pending AUTOINIT
state is cleared at the existing reset owners. Native timeout quarantine retains
borrowed memory until whole-machine reset; it is not graceful expunge evidence.

Five existing default application/startup/library-construction regressions pass
against the completed boot fixes. Four separate volume-publication/queue ownership
tests pass. Root independently rechecked their 67 and 56 receipt bindings. These
scopes remain distinct from a combined native command or full Shell/system boot.

The compiled public-API session driver creates a child using CreateNewProc and is
prepared to exercise native LoadSeg/RunCommand, production packets and alias
readback after deleting the original target. The first combined attempt does not
reach process creation: a 45-instruction trace shows generated local zero seeds
overwrite the driver's incoming argument. A fresh current compiler regenerates
the identical faulty HUNK, ruling out a stale executable. The compiler memory
promotion pass inserts those seeds before incoming argument definitions. The
ordering fix passes a native execution regression that fails against the
preserved old compiler; both compilers pass the two simpler controls. The
unchanged driver then reaches CreateNewProc successfully. Driver validation and
guest registers were not patched to bypass the failure.

The driver now yields using public AllocSignal/Wait with child Signal after
cleanup. Attempt 14 exposes the next production dependency: application boot
still selects the nonblocking compatibility signal implementation, so Wait
returns immediately and the real child never runs. Native DOS admission must
enable real per-task signals and suspend through the existing synthetic scheduler
boundary. No command or filesystem packet has executed in these combined
attempts yet. The native DOS/MakeLink images remain unchanged; historical command
build source snapshots require refresh after the compiler correction before a
future current-source release admission.

See [native boot integration and executable next steps](native-boot-integration.md)
for attempts, receipts and scope. No permission or input from the user is needed.
Shipping remains 0/200 commands and 0/246 profiles; no PURE, complete runtime or
goal-stage checkbox is promoted.

### 2026-09-08 - Blocking Wait reached; native child publication gap isolated

Attempt 15 routes native-admitted Wait through per-task signals and the existing
outer instruction-boundary scheduler. It makes one blocking Wait call, rather
than repeated compatibility returns. Dispatch then rejects Ready child
0x002703F0 because it has no registered complete CPU context. No child entry,
command entry or filesystem packet occurs. The retained failed result is
artifacts/copperstart-makelink-production-boot/attempt15-command-native-wait/result.json,
SHA-256 3dc2af1c97e40da175e17020d0804c08577ae4b09fd2d9a8100339af773e22b4.

Source tracing confirms native CreateNewProc uses shared ExecTaskCore.AddTask
directly, bypassing the public Exec context-registration owner. It also publishes
Ready before DOS initialization finishes. The next implementation therefore
prepares the child before publication and makes commit/rollback ownership
explicit. A failed publication must not consume caller streams, path or startup
message, expose a stale task-port output, or free task memory twice. BCPL D1
startup and the managed process adapter must retain their existing behavior.
Ready-list scanning or test-supplied context registration would not resolve
these ownership requirements.

The official [AddTask contract](https://developer.amigaos3.net/autodocs/exec.library/AddTask.html)
allows immediate rescheduling and returns task/null from V36. A publication
critical section must also account for
[Forbid being broken by Wait](https://developer.amigaos3.net/autodocs/exec.library/Forbid.html);
Forbid cannot make fallible blocking work after publication safe by itself.

Twelve selected default boot/library/WaitPort/scheduler regressions pass against
the blocking change. The receipt is
artifacts/default-boot-native-scheduler-regression-20260908/qualification.json,
SHA-256 d63f624d166843752d91788ede175fb9f08056c9d31ca4382e956b912ac0e462.
All 22 source inputs match before/after; root independently verified all 66
bindings. The result does not qualify native child execution or a completed
signal wake/retirement cycle. Driver/result storage is retained after a timeout
or failure; done/parent return alone does not prove the child has retired.

See [the current checkpoint and next executable steps](native-boot-integration.md).
No user permission or input is required. All 200 commands/246 profiles remain
in scope, with no shipping or PURE admission promoted by this checkpoint.

### 2026-09-09 - Signal result ownership corrected without native-frame writes

The shared signal/port transition now distinguishes native frame ownership from
the host scheduler's saved contexts. The host retains the signal result already
consumed at wake, returns it once, and leaves later signals available to the next
Wait. Public Signal and PutMsg/ReplyMsg share this result handoff. Native core
callers retain the canonical-frame result path. This fixes reblocking after a
consumed signal and writes into an old guest frame; it does not register the
missing native-created child context.

The same compiled 14-test assembly fails eight cases and passes six against
preserved old production owners; corrected owners pass all 14. These are ten
host signal/port component cases plus four unchanged shared signal-state cases,
with controlled guest structures and a suspension callback. No native CPU
switch or original ROM executes. Receipt:
artifacts/exec-host-wait-resume-20260909/qualification.json, SHA-256
c90f19b16ab01724d59675cd883763f81f05b2950cee47f9b6c0139dabacb819.
Root verified 97 bindings with no mismatches.

The same 12 selected default boot/library/WaitPort/scheduler regressions pass
against the final runtime. All 25 observed source inputs are unchanged and all
69 receipt bindings verify. Receipt:
artifacts/default-boot-wait-resume-regression-20260909/qualification.json,
SHA-256 01f6260efd930f476e5d79f776dbb3b798184ca301ec1e91a9d41dc55a9fc577.
Attempt 16 also passes native DOS installation/public library readiness using
the unchanged native HUNK; it performs 178,240 bootstrap instructions and does
not execute a child or command. Its result SHA-256 is
08584f9091c6d31878c2a97282b8b3bb9b00e54ff2e7891dc640bfa3425fd0a9.
The current builds report existing NU1902 for Microsoft.Build.Tasks.Git
10.0.300; that dependency advisory is not resolved by this work.

Native DOS prepare/commit/rollback publication remains the next launch change.
In particular, publication failure must retain caller streams, paths, segment
ownership and startup messages, and published tasks need exactly one Exec
retirement owner. Actual combined wake/retirement, Shell/system boot and all
command/profile/PURE/packaging requirements remain open. Shipping stays
0/200 commands and 0/246 profiles. No goal stage is marked complete.

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

## 2026-09-09: native DOS normal-return cleanup passes combined MakeLink run

Previous goal status reply was informational; this turn produces new runtime
evidence. Native C/assembler Process publication now supplies the exported
copperstart.dos.process-return entry to public AddTask. The entry resolves DOS
through public Exec, uses DosExitCore for cleanup/callback handling, retries
release while retirement remains pending, then calls terminal public RemTask.
It does not free its executing stack itself. The native producer includes and
requires that export. Its native compatibility report has zero runtime helpers,
features and external native targets.

Attempt 31 uses DOS SHA-256
64c100d23012526df36f7af0950be0aefe48a33f8d4b4684794d772b67bc2f76 with the unchanged
MakeLink and public-join driver. The full production integration passes 1/1:
command result/IoErr zero, expected filesystem effects, real parent restored,
child absent from public FindTask, and no retained native DOS process record.
The runner reports unchanged observed sources and inputs. Build exit 0 retains
one NU1902 dependency warning. Attempt 30 remains the failing prior evidence.

This does not establish exact allocation/CLI-table balance merely from an absent
process record. Next work must strengthen those resource observations, exercise
exit callbacks and deferred provider/notification cleanup on the compiled path,
and audit explicit DOS.Exit, BCPL publication and library/code lifetime. Existing
shared callback tests are not a substitute for native finalizer execution tests.
All family, profile, reference comparison, PURE/resident and packaging gates
remain required. Shipping remains 0/200 commands and 0/246 profiles.

Evidence: artifacts/copperstart-makelink-production-boot/attempt-31-command-native-process-return/runner-receipt.json,
result.json and test.trx; artifacts/dos-native-process-return-20260909/native/
contains the image, map, build request and native compatibility report.

## 2026-09-10: public CLI lookup retirement verified

The previous turn made progress with the compiled normal-return cleanup pass.
This turn strengthens NativeDosBootIntegrationTests with a positive public CLI
lookup before child retirement and a negative lookup for the same CLI number
afterwards. It requires exactly one matching live child within a bounded MaxCli
range; it does not accept an always-empty lookup as evidence.

Attempt 32 passes 1/1 against the unchanged native DOS, MakeLink and driver
images. The runner reports build/test exit 0 and unchanged observed inputs and
sources. Existing process-record, real task retirement, parent return and command
filesystem assertions also pass. The build retains one NU1902 warning.

FindCliProc rejects invalid entries as well as empty slots, so this result proves
public lookup behavior only. The next cleanup audit must inspect the actual
registry slot and allocation ownership, then cover native callbacks, deferred
cleanup and resident reuse. No command/profile/PURE/goal-stage gate is promoted;
shipping remains 0/200 commands and 0/246 profiles.

Evidence: artifacts/copperstart-makelink-production-boot/attempt-32-command-public-cli-retirement/
runner-receipt.json, result.json and test.trx.

## 2026-09-10: raw CLI slot clearing verified; reaper ordering concern found

Attempt 33 extends the preceding public lookup check with direct read-only
observations of the first RootNode.rn_TaskArray range. It verifies the BPTR
conversion, capacity bounds and exact Process message-port value before child
retirement. Afterwards the same slot is zero, root/array addresses are unchanged,
and public MaxCli still validates the registry. The full combined test passes
1/1 with unchanged observed sources/inputs and build/test exit 0. One existing
NU1902 build warning remains. This closes the invalid-entry false-negative gap
for the single-child scenario, not every registry range or allocation owner.

During the allocation audit, source inspection found ExecTaskMemoryCore.Reap
calls ExecListCore.Initialize on the Task memory-list header after FreeEntry
has freed every owned entry. DosOwnedProcessTaskCore places the containing
Process allocation in the first owned entry. ReleaseOwned also probes a marker
after these frees. Next work must test and correct that ordering without writing
to or inferring ownership from freed task storage; preserve separate MorphOS
owned-task behavior and terminal scheduler ownership. This is source evidence,
not yet a reproduced allocator corruption result.

Evidence: artifacts/copperstart-makelink-production-boot/attempt-33-command-cli-slot-retirement/
runner-receipt.json, result.json and test.trx. No command/profile/PURE/stage is
promoted. Shipping stays 0/200 and 0/246; callbacks, deferred cleanup, exact
allocation reclamation, resident reuse and all remaining command families stay
within the full active goal.

## 2026-09-10: reproduced and corrected task reaper write-after-free

The previous turn made progress through raw CLI-slot verification and source
inspection. This turn adds a freed-payload poisoning test for a task whose own
allocation is in tc_MemEntry. The old Exec assembly overwrites the poisoned list
header after freeing that task (expected 0xA5, actual 0). The same compiled test
assembly gives 17 passes/1 failure with old Exec and 18 passes with the fix.
The selection includes existing signal/task removal and MorphOS creation tests.

ExecTaskMemoryCore now detaches the live list before freeing entries. Separate
CSTK allocation metadata is captured and invalidated while live via the helper
shared with ReleaseOwned, then released from that snapshot after list cleanup.
No post-entry-free Task list write or descriptor probe remains in this path.

Attempt 34 passes the complete existing MakeLink integration including private
DOS record absence, public CLI retirement and raw slot clearing. The runner
reports build/test exit 0 and unchanged observed sources/inputs; the build has
one existing NU1902 warning. The integration uses the rebuilt public Exec owner
and unchanged native DOS HUNK. It does not qualify newly compiled native Exec,
exact allocation accounting, malformed/overlapping ownership, callbacks,
deferred cleanup or resident reuse. Those remain required work.

Evidence: artifacts/exec-task-reap-ownership-20260910/checkpoint.json binds the
source subset, identical old/new test assemblies, differing Exec assemblies,
18-test reports and attempt-34 receipt/result/TRX. No shipping/profile/PURE/stage
is promoted; the full 200-command/246-profile goal stays active.

## 2026-09-10: session memory accounting exposes retained bytes

The previous turn corrected the reaper ordering with a discriminating regression.
This turn adds public AvailMem(0) checkpoints immediately before launcher entry
and after successful command/task/CLI cleanup, holding driver/config allocations
at both checkpoints. Flags 0 reports allocator free bytes through the production
QueryPortableAvailableMemory path; it does not request MEMF_TOTAL.

Attempt 35 fails the new strict equality: 8971328 before, 8938288 after, deficit
33040 bytes. All preceding command, child removal, native process-record absence
and raw CLI-slot assertions pass. The runner reports unchanged observed inputs
and sources, build exit 0/test exit 1. The assertion is retained without tolerance.

The deficit equals rounded (16-byte DOS descriptor + 228-byte Process + 32768
stack) = 33016, plus its 24-byte MemList. This is a source-backed size match,
not yet proof of exact retained allocation identities. Exec retirement captures
owned spans and defers reclamation until metadata, scheduler removal/commit and
resumable-stack checks pass. Next inspect the specific failing barrier and
retained spans in the production run. Do not remove these safety checks or scrub
arbitrary guest stack/register contents to make accounting pass.

Evidence: artifacts/copperstart-makelink-production-boot/attempt-35-command-memory-balance/
runner-receipt.json, result.json and test.trx. Exact reclamation, repeat/resident
use, callbacks, deferred provider cleanup and all command/profile gates remain
open. Shipping stays 0/200 commands and 0/246 profiles.

## 2026-09-10: exact retained allocations and failing retirement check located

Previous turn made progress by exposing the 33040-byte memory deficit. Attempt
36 adds read-only reflection diagnostics for the real deferred retirement owner;
it invokes only existing predicate queries, never cleanup or state mutation.
The strict memory assertion still fails, with unchanged observed inputs/sources.

Task 2558280 owns retained spans [2591280,2591304) (24-byte MemList) and
[2558264,2591276) (33012-byte descriptor/Process/stack, rounded to 33016 by Exec).
This accounts for the deficit. Storage validity and Matches both pass, and
FrameIsClear without continuation scanning passes. Full continuation scanning
fails, causing CommonRetirementChecks and CanCompleteRetirement to fail.

The parent has A7/USP 520188, ISP/MSP 1024, and public bounds [1024,520192).
AmigaBoot sets BootSupervisorStackTopAddress and BootChipPublicLowerAddress to
0x400 and publishes that broad lower bound for the initial task. Retirement's
current FrameIsClear scans each nonzero bank to the same public upper bound.
Thus the inactive supervisor banks cause scanning across boot memory instead
of a separately established supervisor stack range. Next identify the exact
blocking word and establish correct bank ownership/bounds through the boot and
retirement contracts; do not bypass the check, scrub guest memory or forge a
scheduler handoff. This diagnosis does not yet prove a safe correction.

Evidence: artifacts/copperstart-makelink-production-boot/attempt-36-command-retirement-owner/
result.json, runner-receipt.json and test.trx. No command/profile/PURE gate is
promoted. The complete goal remains active at 0/200 and 0/246 qualified shipping.

## 2026-09-10: system stack ownership fixes the retained Process allocation

Previous turn located the failing continuation scan. Attempt 37 captures the
exact witness: ISP and MSP start at 1024 and incorrectly scan through address
519244, containing child pointer 2558280. The boot owner already publishes its
system stack as [0,1024); the word lies outside it and below the active USP.

CopperStartExecContext now accepts a separate system-stack range from its boot
owner. AmigaBoot supplies it only outside ROM takeover and while public Exec
system-stack bounds match the boot-established values. Retirement uses it for
supervisor banks, preserving task bounds for user stacks and unknown banks.
All PC/stack-pointer overlap, metadata, scheduler and in-range continuation
checks remain. It does not clear guest memory or fabricate a context switch.

Attempt 38 passes the strict memory equality: 8971328 free bytes before and
after, no pending retirement owner, successful MakeLink and existing task/CLI
cleanup assertions. The runner reports unchanged observed inputs/sources and
build/test exit 0. Six component stack-bound cases cover known empty versus
unknown ranges, real user/supervisor references, out-of-range and removed-span
bank pointers. Together with existing host tests, 24/24 pass. The first component
build failed on missing imports; its log is retained and the corrected retry
uses fresh output directories. One existing NU1902 dependency warning remains.

Evidence: artifacts/copperstart-makelink-production-boot/attempt-37-command-stack-bank-witness/
and attempt-38-command-system-stack-bounds/; artifacts/exec-system-stack-bounds-20260910/
retry-results/system-stack.trx. The final source comment was clarified after the
passing runs without a semantic change. This qualifies one session's balance,
not all allocations, ROM stacks, resident concurrency or deferred DOS cleanup.
Next exercise repeated command sessions and native exit callbacks/deferred
providers while preserving the full family/profile/PURE backlog. Shipping stays
0/200 commands and 0/246 profiles; no goal stage is marked complete.

## 2026-09-10: two consecutive command sessions recover full baseline

Previous turn fixed and qualified system-stack bounds for one session. The
integration runner now accepts --sessions 1..8 (default 1). Sessions share one
boot and installed DOS library, use separate target/alias/diagnostic filenames,
and preserve all command/task/CLI checks. An outer AvailMem(0) checkpoint also
requires the initial baseline after each driver's image/config are freed.

Attempt 39 stopped at an unrelated GraphicsDisplayDatabase compiler error for
monitorPositionProvider. No graphics source was changed by this work. Attempt
40 compiles the current test against all complete production assemblies from
attempt 38: two sessions pass, inputs remain unchanged, and actually loaded
production assembly hashes match. Its prebuilt-receipt explicitly lists unused
references and does not claim a current-source production build.

After the graphics source independently supplied the missing parameter, attempt
41 rebuilt current production and passed both sessions. Build/test exit 0,
unchanged observed source/input hashes, and one existing NU1902 warning. Every
session passes inner and outer free-memory equality, task removal and CLI-slot
clearing. Distinct paths exercise new invocation arguments without boot reset.

Evidence: artifacts/copperstart-makelink-production-boot/attempt-39-command-two-sessions/
(failed build), attempt-40-two-sessions-prebuilt-production/ (identified previous
production binaries), and attempt-41-command-two-sessions-current/ (authoritative
current-source two-session receipt/result/TRX).

These are fresh LoadSeg/UnLoadSeg executions, not a same-SegList purity result.
Next extend the public guest driver for exit callbacks and shared-image resident
execution; retain active-use, concurrency, deferred cleanup and reference-profile
requirements. All 200 commands/246 profiles remain required; shipping stays zero.

## 2026-09-10: native Process exit callback runs exactly once per session

The previous turn qualified two complete sessions and memory recovery. This
turn extends the compiled public guest driver with NP_ExitCode/NP_ExitData tags.
The tagged export takes D0 return code and D1 result-record address, records
count/code in previously reserved offsets 152/156, and returns the code unchanged.
The tag buffer is 80 bytes, retaining its cleared terminator. The native builder
explicitly roots the new export; the driver remains test infrastructure, not a
Shell replacement or shipped command.

The identified compiler builds a driver HUNK with SHA-256
9f52ca81f83d6f9edcee9fe0d33768c58ef022f18bdb0ade75e3b7a6607ecdec.
Its static report has no managed allocation sites, runtime helpers or external
native targets; nullable-values is reported as a runtime feature. The integration
requires exactly one callback and a recorded code matching the actual child
return after real task removal, before allowing driver/config reclamation.

Attempt 42 passes two sessions in one production boot. Both callbacks run once
with code 0; command, task, private process-record, CLI-slot and inner/outer
memory-balance assertions also pass. Build/test exit 0, unchanged observed
source/input hashes, and one existing NU1902 warning are retained.

Evidence: artifacts/copperstart-launcher-exit-callback-20260910/native/build.json,
map and compatibility report; artifacts/copperstart-makelink-production-boot/
attempt-42-command-native-exit-callback/runner-receipt.json, result.json and TRX.
This proves successful normal-return callbacks only. Nonzero callback input and
replacement-result propagation, explicit Exit, deferred provider cleanup and
same-SegList resident concurrency remain required. No shipping/profile/PURE
count or goal stage is promoted; the full 200-command/246-profile goal is active.

## 2026-09-11: disposable same-SegList MakeLink probe is bounded

The CC06 verifier `tools/Commands/verify_workbench_makelink_same_seglist.py`
now accepts the disposable concurrent MakeLink observation without requiring a
`RemSegment` event. It binds two distinct process tasks to one shared segment,
returns 0 and 20, checks balanced per-process allocations and parser ownership,
and rejects image writes or image changes. The resulting report is
`artifacts/workbench-makelink-same-seglist-20260911.json` with observation hash
`92ed60a8b0ae9e27ff8f63590e257304a2f58edcd36b68a670c4f1461547f300` and
verifier hash `2d0d59df793a6382041c096e641dcc875c97ffab09569a630b101f62e633b8f3`.

Because the fixture deliberately does not exercise resident registration or
removal, it records `residentRemovalQualified=false`, `fullCommandQualified=false`
and `pureAdmission=false`. It is useful CC06 evidence, but it does not remove
the production same-SegList, active-use/replacement, deferred-load, task-death,
failure-overlap, or per-command purity blockers. Shipping remains 0/200
commands and 0/246 profiles; no gate or goal stage is promoted.

## 2026-09-11: MorphOS Delete frontend has a bounded native checkpoint

The new
`NativeMorphOSDeleteCommand` owns the MorphOS five-slot `ReadArgs` lease,
2,600-byte cleared invocation workspace, source-vector dispatch and final
result cleanup. It routes object traversal through the shared public-DOS
`MatchFirst`/`MatchNext`/`MatchEnd`, lock, protection and `DeleteFile` stages;
the private FOLLOWLINKS state bit is consumed only by Delete so existing Copy
flags remain unchanged. `NativeMorphOSDeleteEntry` uses the common Workbench
message/DOS startup owner but does not claim the classic four-option Workbench
contract.

`tools/Commands/qualify_delete_command_native.ps1` rebuilt the Compiler CLI,
native root, runner and resident HUNK for all three CPUs. The receipt at
`artifacts/delete-command-native-20260911/qualification.json` records 41
reachable methods and eight supplied-DOS runtime invocations per CPU, with
these artifact sizes/hashes:

| CPU | Bytes | SHA-256 |
| --- | ---: | --- |
| 68000 | 15,252 | `9d1d2841bd3747623006fbd97b56301b5b6cf95885cb1712dd929217adbde243` |
| 68020 | 15,376 | `72f276fdbecc7e7f64cc4d2fb03f337d0736fc1637398c3a872e18060dffc6dd` |
| 68040 | 15,180 | `ee07c5423946ad4f19644ba62ad5a6864b862fa576f51a8b0646196783174e04` |

All three static reports have zero managed runtime features/helpers, external
native targets, exception regions and fatal machine-fault sites. The runtime
fixture checks the exact parser template and five-slot result storage,
parser/empty-vector cleanup, one/two regular-file matcher traversal, lock and
unlock ordering, FORCE protection, partial failure and interleaving; it reports
no leaks or shared-image writes. This remains a bounded frontend checkpoint,
not a full command qualification. Parent-protection retry, exact diagnostics,
recursive/link behavior, packed correspondence, real handler execution,
Workbench parity, packaging and same-SegList production purity remain open.
Shipping remains 0/200 commands and 0/246 profiles.

## 2026-09-11: MorphOS FileNote bounded body

`NativeMorphOSFileNoteCommand` now implements the source-observed
`FILE/A,COMMENT,ALL/S,QUIET/S` body through public DOS calls. It owns a cleared
AnchorPath with 512-byte path storage, an 80-byte invocation-local comment
copy, and diagnostic arguments; truncates comments at 79 bytes; follows the
public recursive `DoDirectory`/`DidDirectory` flags; calls
`MatchFirst`/`MatchNext`/`MatchEnd` and `SetComment`; continues after
per-object SetComment failures with WARN; and keeps the Workbench spelling and
body separate. `NativeMorphOSFileNoteEntry` uses the common DOS/Workbench
startup owner.

`tools/Commands/qualify_filenote_native.ps1` emits the receipt at
`artifacts/filenote-native-20260911/qualification.json`. The resident HUNKs
have 14 reachable methods and zero managed runtime features/helpers, external
native targets, exception regions or fatal fault sites. The supplied-DOS
fixture now passes ten invocations per CPU, including parser failure, no-match,
comment truncation, quiet output, directory descent/exit, SetComment failure,
cleanup and interleaving; the runtime reports are stored beside each HUNK.

| CPU | Bytes | SHA-256 |
| --- | ---: | --- |
| 68000 | 3,680 | `d7e7b210cdfe526c2e9a773055d5f1e06caa08990a449c10a47308bbdd310ceb` |
| 68020 | 3,728 | `aed0f54151e3ec909a92ae207d2eba7f358873dcbc7aa462de1d311c20f127c7` |
| 68040 | 3,672 | `d38bb13d407824c866369eff3827323567cd1db1114fc8c53685fd288d1bdced` |

This is a bounded static and supplied-DOS entry checkpoint. Exact parser and
diagnostic captures, soft-link and volume/device policy, Workbench parity,
packaging and production PURE/resident qualification remain open.
Shipping remains 0/200 commands and 0/246 profiles.

## 2026-09-11: MakeLink profile entry reruns

The MorphOS MakeLink entry receipt (`artifacts/makelink-native-20260911/qualification.json`)
passes 18 supplied DOS parser/link/lock/FIB calls per CPU (54 total). The
resident HUNKs are 2,580 bytes with nine reachable methods; their hashes are
68000 `6f47d87d9f863e71f2c52d8ba8b65b92de50f9681bd900ce51b1d86d130c52b5`,
68020 `0bcb55c6a107c6cf02216f7f59b18e8082b5bcc398e1c43ab4c101fbb11d43b3`,
and 68040 `0bcb55c6a107c6cf02216f7f59b18e8082b5bcc398e1c43ab4c101fbb11d43b3`.

The Workbench MakeLink entry receipt (`artifacts/makelink-wb31-native-20260911/qualification.json`)
passes 20 supplied DOS calls per CPU (60 total), including interleaving and
distinct invocation locks. Both receipts explicitly keep original-DOS launch,
installed flags, complete profile parity, packaging and production
PURE/resident admission as separate open gates.

Shipping remains 0/200 commands and 0/246 profiles.

## 2026-09-11: Search stages and DOSList traversal rerun

The Search building blocks and DOSList traversal were rerun on all required
CPUs. The literal matcher receipt (`artifacts/search-literal-native-20260911/qualification.json`)
passes nine direct calls per CPU with 1,060/1,084/1,052-byte HUNKs (seven
reachable methods) and hashes
`ebcc092a41db61d8bff6af53f4363e44ed72287d2a6236617a7a954c4a4bced8`,
`2646773899a00e82a409422f620c1bbdd81e06a50a8a78b15086a15ba07c8c6a`, and
`4135553b44c5d407394ebb73b7d05ece95ef272f1f9400d76b2a125b7011899f`.

The line formatter receipt (`artifacts/search-line-native-20260911/qualification.json`)
passes ten direct calls per CPU with 4,284/4,368/4,264-byte HUNKs (19
reachable methods) and hashes
`60dc3cc0a53f36d8bc315cf42e5d09b5cf93cfb532670c968de12356147eeab0`,
`17dd86fd4bd26d9694735ea13cd073d4c7e20b9713567757e5456b672f1682ab`, and
`8609c7d259827547d1c032816d163866ca5b86a1fa06fa477389f1e4ae84d606`.

The DOSList traversal receipt (`artifacts/doslist-traversal-native-20260911/qualification.json`)
passes six public-DOS calls per CPU with 580/588/580-byte HUNKs (two reachable
methods) and hashes
`5cefd6abe9af9e877670fcca00e17adcc4eb3e01985492f4ebbca23e436cfa58`,
`9d883441c9cbba90f6d24960eccf08f1295adec2e719775164b69c05bc3e5ab2`, and
`1360d1ee46b3196eb2cfed4dd1e489e01abec60fdfe3be830a54a3ff5ef8d867`.

These are component/traversal receipts only. Search still needs locale,
pattern, parser, filesystem and output integration; DOSList still needs its
outer command, rendering and exact reference behavior. Packaging,
differential comparison and production PURE/resident admission remain open.
Shipping remains 0/200 commands and 0/246 profiles.

## 2026-09-11: Relabel, Rename and AddBuffers entry reruns

The namespace frontends were rerun through their resident 68000/68020/68040
entries. MorphOS Relabel (`artifacts/relabel-native-entry-20260911/qualification.json`)
passed eight supplied calls per CPU; its HUNKs are 2,804/2,848/2,804 bytes,
with hashes
`b5e0042756c36a110dd7a6c3520ee22d42d2b1e0fa79ba2d0226c65ff9cb95bc`,
`a1692cd2b8d54fc6142d96cc2505486102c4963b7ced334f2ae422aa315c139d`, and
`706dc3e09bb92671f8e91766ebb682f1a36a3cedbd24bfda74b60fad78f4ff7b`.

MorphOS Rename (`artifacts/rename-morphos-native-20260911/qualification.json`)
passed 112 supplied calls per CPU (336 total), covering direct and directory
mutation, parser/allocation failures, ordering, quiet/break/IoErr policy and
2048-byte boundaries, with hashes
`38597adb00dc3edfe0944b70dc487cd1591f0c3ddc68342b5b36e7d2619bfc81`,
`1174d54534ca8dd8cdd8c6dada5c9a98b200c8d29fc9e24ed187cc0101fe69ca`, and
`82eb76db793c008d29bada4a94e67dee282c6263b71f7a24fb7bc67209aebe20`.

MorphOS AddBuffers (`artifacts/addbuffers-native-20260911/qualification.json`)
passed six supplied calls per CPU; its 2,396-byte HUNK hashes are
`4516d597ca2f8259ba2d0bd9676ebe923fdcf9c90b6d41d0d2be7f75d38d5cde`,
`7c0f4cd59ff612d85b86135bb33939b406763d76be00849f24af8ff5121e5722`, and
`7c0f4cd59ff612d85b86135bb33939b406763d76be00849f24af8ff5121e5722`.

These receipts remain supplied-vector evidence. Exact original parser and
diagnostic parity, real handler behavior, packaging, reference comparison and
production PURE/resident admission remain open. Shipping remains 0/200
commands and 0/246 profiles.

## 2026-09-12 - MorphOS Type native-entry requalification

Re-ran `tools/Commands/qualify_type_native_entry.ps1` against the current
source and SDK. The MorphOS `Type` resident entry passes thirteen supplied DOS
parser, wildcard, text/HEX stream, `TO`, diagnostic, Ctrl-C, repeat, and
interleaving invocations per CPU (39 total), with balanced invocation-owned
resources, no shared-image writes, and no managed runtime features/helpers,
external targets, exception regions, or fatal fault sites.

Receipt: `artifacts/type-morphos-native-20260912-qualified/qualification.json`.
The current HUNKs are 8,456/8,516/8,324 bytes with SHA-256
`7478aee3a8859a9bbd64f7d4b3843b9e81a95b1ac49cefa0d225f214cbca073e`,
`5ecb3c666fe8336387859a95c5e259e59e22520118c69fc68fee2c3845f05224`, and
`3b9abe6109131392a3256667256a0398a634038d9b718d057e308318fe6ebb62`.
This advances only the MorphOS native gate; original 50.6 packed/source
correspondence, real DOS/handler behavior, exact provider semantics,
Workbench parity, PURE/resident lifecycle, package admission, licensing, and
differential evidence remain open. Shipping remains 0/200 commands and 0/246
profiles.

## 2026-09-12 - Workbench Eval native-entry requalification

Re-ran `tools/Commands/qualify_eval_wb31_native_entry.ps1` for the separate
Workbench 3.1 literal-template entry. The resident 68000/020/68040 HUNKs pass
twelve supplied vectors per CPU (36 total), covering captured arithmetic and
operand reconstruction, X/O formatting, `TO`, caret handling,
parser/allocation failures, repeats, and interleaved callers. Static reports
contain no managed allocation sites, fatal machine-fault sites, runtime helpers,
or external targets; the audited `nullable-values` feature remains only on the
`TO` path, and every run reports zero shared-image writes.

Receipt: `artifacts/eval-wb31-native-20260912-qualified/qualification.json`.
The HUNKs are 17,052/17,212/16,836 bytes with SHA-256
`e0610dcbd290416d2e7586765ce02715ea388a1efc2eb6921d6a7c73384937a2`,
`c62be6703408643d55b7bf8ea0fc709070418c76e37a40a9239b42be2a473d1e`, and
`a6735151f4b7e6ab34c52c462560ace69a0f99f16052144178c51013ab7bd010`.
This advances the Workbench Eval native gate only; real DOS parsing, complete
classic grammar/semantics, original comparison, PURE/resident admission,
packaging, and differential evidence remain open.

## 2026-09-12 - MorphOS Eval native-entry requalification

Re-ran `tools/Commands/qualify_eval_native_entry.ps1` for the MorphOS `Eval`
resident entry. The 68000/020/040 HUNKs pass fourteen supplied vectors per CPU
(42 total), covering bounded expression and numeric/LFORMAT behavior, `TO`,
diagnostics, parser/allocation failures, Ctrl-C, repeats, and interleaved
callers. Static reports retain the audited `nullable-values` feature on `TO`
and contain no managed allocation sites, fatal machine-fault sites, runtime
helpers, or external targets; all runs report zero shared-image writes.

Receipt: `artifacts/eval-morphos-native-20260912-qualified/qualification.json`.
The HUNKs are 25,092/25,508/24,860 bytes with SHA-256
`7623121f7fa8215663fcca5e9b380ebf880a3c152b29002cdad0d723d6b3c46a`,
`67c5654c8126f8d05b9a6a093e5dc18f66a7bd43c02de1c24888905862a9247b`, and
`8636f703f5399f534a04970a443182cf0548a1d3dc6b95d3d5c596fca269c04e`.
This advances the MorphOS Eval native gate only; real DOS/filesystem behavior,
complete 50.7 semantics, original comparison, PURE/resident admission,
packaging, and differential evidence remain open.

## 2026-09-12 - Workbench 3.1 Search syntax-candidate entry slice (historical)

Added `NativeWorkbench31SearchCommand` and the private resident
`Workbench31SearchEntry`, opening DOS 36 and reusing the bounded public-DOS
Search body behind a separate profile entry. The candidate covers the shared
ten-slot parser boundary, flat matcher/file I/O, DOS pattern and line modes,
failure, Ctrl-C, and CLI/Workbench startup guards. It is explicitly a syntax
candidate because no Workbench Search source member or packed binary has been
bound; exact grammar, output, locale, recursion and package behavior remain
open.

`tools/Commands/qualify_workbench31_search_native_entry.ps1` passes seventeen
supplied DOS-vector invocations per CPU (51 total) on resident 68000/020/040
HUNKs. Receipt:
`artifacts/search-wb31-native-20260912-qualified/qualification.json`.
The HUNKs are 15,220/15,376/15,204 bytes with SHA-256
`d4859b41a3facd55d316359f1c679a38b8adead6f850a246e51482b0f878912a`,
`07c5e2859e43743791e60eabe8ae2f870609aedc2e5fa5163a8a45c76e513a63`, and
`0946ad9623fe8530b4e6aca68861fa1c669a7a617a44a4a7c70e62177669c90d`.
Static checks report 25 reachable methods per CPU and no managed allocation
sites, runtime features/helpers, external native targets, exception regions,
fatal fault sites, or shared-image writes. This advances only the Workbench
candidate gates; original binary correspondence, complete differential,
PURE/resident lifecycle, licensing, package admission and shipping remain
open. Shipping remains 0/200 commands and 0/246 profiles.

## 2026-09-11: MorphOS Join frontend static checkpoint

`NativeMorphOSJoinCommand` now owns the MorphOS `FILE/M/A,AS=TO/K/A`
`ReadArgs` lease, a fixed matcher/path/FIB workspace, destination creation and
failure cleanup, per-source DOS opens, the 262,144-byte append buffer lifetime,
wildcard traversal and the existing exact append loop. Its native entry uses
the common DOS/Workbench startup owner. The Workbench `FILE/M/A,AS=TO/K/A`
candidate remains separate and unclaimed until classic reference behavior is
captured.

`tools/Commands/qualify_join_command_native.ps1` emits the static receipt at
`artifacts/join-command-native-20260911/qualification.json`. The resident
HUNKs have 18 reachable methods and zero managed runtime features/helpers,
external native targets, exception regions and fatal fault sites:

| CPU | Bytes | SHA-256 |
| --- | ---: | --- |
| 68000 | 4,208 | `49bedb950e42668dcb1e6e399a198bf065ea4d3e21ce84821020b3b03ac2c251` |
| 68020 | 4,284 | `cf304bb8260fcde8197c64aea53692bf33a3c3ae9d4bde36f2a9b630ee99b3c7` |
| 68040 | 4,200 | `88005823965b71c8874f9340c355705ed72b55b669dbc1c18bb97be323eb5476` |

This is a static frontend checkpoint; no full Join runtime fixture is claimed
yet. Exact diagnostics and no-match behavior, handler execution,
source/destination interaction, packaging, packed correspondence, Workbench
parity, differential evidence and production PURE/resident qualification remain
open. Shipping remains 0/200 commands and 0/246 profiles.

The matcher coordinator now saves `IoErr` across its own `Exec.FreeMem` of each
temporary AnchorPath. The fixture intentionally poisons cleanup calls, so this
repair is observable: Delete now publishes the last handler/matcher error
instead of the allocator's cleanup value. The existing Copy parsed-DELETE
qualification was rerun on all three CPUs (10 invocations each, no image writes
or leaks); its reachable-method expectation is now 49 because of the explicit
error-preservation call.

## 2026-09-11: MorphOS Type external-entry checkpoint

`tools/Commands/qualify_type_native_entry.ps1` rebuilt the MorphOS Type entry
and ran its supplied DOS parser, wildcard, stream, option, diagnostic,
Ctrl-C, repeat and interleaving fixture on each required CPU. The resident
HUNKs remain static-compatible with zero managed runtime features/helpers,
external native targets, exception regions or fatal machine-fault sites; each
invocation releases its `ReadArgs` lease, 17,176-byte workspace, handles and
buffers without shared-image writes:

| CPU | Bytes | SHA-256 | Supplied invocations |
| --- | ---: | --- | ---: |
| 68000 | 8,204 | `125e3dcf7842d34103a730a8d097d8c0d57112d0d57bea36263ec7d3e41211ae` | 13 |
| 68020 | 8,256 | `6d8620dbb4014b0685909476dcd2d10f04dcbdbf3086826604e0441012a39e77` | 13 |
| 68040 | 8,068 | `21413a0e90ff8bb5543193b9139dc8f3dc36ae77b17081de792f59685ee48678` | 13 |

The receipt is `artifacts/type-native-entry-20260911/qualification.json`.
It is still a bounded supplied-vector checkpoint: real DOS parser/filesystem
behavior, the MorphOS literal-soft-link provider, Workbench 37.2 parity,
packaging, differential evidence and production PURE/resident qualification
remain open. Shipping remains 0/200 commands and 0/246 profiles.

## 2026-09-11: Which, PathPart and Quote entry reruns

Three existing command frontends were rebuilt and rerun through their resident
68000/68020/68040 HUNK entries. The receipts record supplied-vector execution,
balanced invocation-owned resources and zero shared-image writes; they remain
bounded checks and do not claim real DOS/parser or original-reference parity.

Which (`artifacts/which-native-entry-20260911/qualification.json`) passed 12
supplied calls per CPU (36 total), with 3,844/3,900/3,832-byte HUNKs and hashes
`897339f2a0f305a6fc96c9de98b07596db073e490a29f8a611fd59ca7d668233`,
`62ef59885f2444154f01330604382304a0308eda090337c9a39d62029b19f6e6`, and
`50ce2224ad1c254a224c6a911e2d81dab5f46435879204c55680f88d5e42e82f`.

PathPart (`artifacts/pathpart-native-entry-20260911/qualification.json`)
passed 11 supplied calls per CPU (33 total), with 3,088/3,148/3,088-byte
HUNKs and hashes
`ad92c91ebce428817c9671fb9701a82843b8160cc3e653fc86ff2fc55611d4dd`,
`e8ba70d5999037496675320f4780c08bf491031c948695542140aa2c02ca9ad8`, and
`f4e0d3836d9edc4ef8d22e761d21c2072aaeabceb6652c057d5b1f809960f52e`.

Quote (`artifacts/quote-native-entry-20260911/qualification.json`) passed 12
supplied calls per CPU (36 total), with 10,388/10,648/10,356-byte HUNKs and
hashes
`b580f4caca4cd58a2d05d3c86db4da2db81b40ae1f652c429304b2e76847c842`,
`2927f2319462d2460d78be23ff630aac298da4b5712282084e647a7798951dac`, and
`00f4c5d500ba7653b444e94da092b27b32c227805d82e1435378943a947622b4`.

All three command profiles still need exact reference captures, real DOS and
handler integration, packaging, differential comparison and production
PURE/resident lifecycle qualification. Shipping remains 0/200 commands and
0/246 profiles.

## 2026-09-11: Eval profile entry reruns

Both Eval profile entries now have fresh supplied-vector receipts. The MorphOS
entry (`artifacts/eval-native-entry-20260911/qualification.json`) passed 14
calls per CPU (42 total), with 25,092/25,508/24,860-byte resident HUNKs,
92 reachable methods and hashes
`7623121f7fa8215663fcca5e9b380ebf880a3c152b29002cdad0d723d6b3c46a`,
`67c5654c8126f8d05b9a6a093e5dc18f66a7bd43c02de1c24888905862a9247b`, and
`8636f703f5399f534a04970a443182cf0548a1d3dc6b95d3d5c596fca269c04e`.

The Workbench entry (`artifacts/eval-wb31-native-entry-20260911/qualification.json`)
passed 12 calls per CPU (36 total), with 17,052/17,212/16,836-byte resident
HUNKs, 63 reachable methods and hashes
`e0610dcbd290416d2e7586765ce02715ea388a1efc2eb6921d6a7c73384937a2`,
`c62be6703408643d55b7bf8ea0fc709070418c76e37a40a9239b42be2a473d1e`, and
`a6735151f4b7e6ab34c52c462560ace69a0f99f16052144178c51013ab7bd010`.

These are native ABI and supplied-vector lifecycle checks only. Real DOS
parsing/OS execution, original Workbench/MorphOS comparison, packaging,
dependency integration and production PURE/resident admission remain open.
Shipping remains 0/200 commands and 0/246 profiles.
## 2026-09-11: MorphOS ChangeTaskPri frontend checkpoint

Added `NativeMorphOSChangeTaskPriCommand` and its private native entry. The
body uses the source-observed `PRI=PRIORITY/A/N,PROCESS/K/N` template, current
task or CLI process lookup, signed -128..127 validation, `SetTaskPri` under
`Forbid`/`Permit`, and the source missing-process/error policy. CopperSharp's
Amiga SDK now exposes the MorphOS `FindTaskByPID` slot at LVO `-994` and a
pointer-indirect `JSR (A3)` wrapper; the body loads the slot and gates the
fallback on Exec version 50.45 or newer.

`tools/Commands/qualify_changetaskpri_native.ps1` emits
`artifacts/changetaskpri-native-20260911/qualification.json`. Three resident
HUNKs compile with twelve reachable methods and zero managed runtime
features/helpers, external native targets, exception regions or fatal
machine-fault sites. The supplied Exec/DOS fixture passes eleven invocations per
CPU, covering current/process/PID targets, both priority bounds, out-of-range
and missing-target errors, parser failure, cleanup and interleaving:

| CPU | Bytes | SHA-256 |
| --- | ---: | --- |
| 68000 | 2,920 | `2639df78fd63bb2971da92fae64c8f04e05d01ba33c6279828353b384a0485b7` |
| 68020 | 2,916 | `b0cd23bc6fa1e6dca1b17c3186aa055a5932d16cfc09358985be3a356f414e9f` |
| 68040 | 2,916 | `b0cd23bc6fa1e6dca1b17c3186aa055a5932d16cfc09358985be3a356f414e9f` |

This remains a bounded static and supplied-vector checkpoint. Exact original
diagnostics/parser behavior, Workbench parity, real task
priority effects, production PURE/resident lifecycle and package placement
remain open. Shipping remains 0/200 commands and 0/246 profiles.

## 2026-09-11: MorphOS Break frontend static checkpoint

Added `NativeMorphOSBreakCommand` and its private native entry. The body uses
the source-observed `PROCESS/N,PORT,ALL/S,C/S,D/S,E/S,F/S` template, DOS
`FindCliProc`, the MorphOS pointer-indirect `FindTaskByPID` slot at Exec LVO
`-994` when
Exec is version 50.45 or newer, Exec `FindPort`/`Signal` under
`Forbid`/`Permit`, and DOS output vectors for the required-target diagnostics.
Signal masks, default Ctrl-C, cleanup and final `IoErr` policy follow the
MorphOS 50.6 source where the public vectors are available.

`tools/Commands/qualify_break_native.ps1` emits
`artifacts/break-native-20260911/qualification.json`. Three resident HUNKs
compile with twelve reachable methods and zero managed runtime features/helpers,
external native targets, exception regions or fatal machine-fault sites. The
same receipt also runs thirteen supplied Exec/DOS invocations per CPU, covering
process and port paths, default/ALL/combined masks, PID fallback, missing
targets, parser failure, cleanup and interleaving:

| CPU | Bytes | SHA-256 |
| --- | ---: | --- |
| 68000 | 3,304 | `7659077986808b492a9cf564c5250fee3a7343f962f96a44585a4a34b9ec7da2` |
| 68020 | 3,300 | `ccdebb3ce07928a164f2d3c4cf19e0c3d808aa7acfe8900fd4a0658bd96eb3c4` |
| 68040 | 3,300 | `ccdebb3ce07928a164f2d3c4cf19e0c3d808aa7acfe8900fd4a0658bd96eb3c4` |

This is a bounded static and supplied-vector frontend checkpoint. Original
reference comparison, concurrent target death, production
PURE/resident lifecycle and package placement remain open.
Shipping remains 0/200 commands and 0/246 profiles.

## 2026-09-11: Workbench Break and ChangeTaskPri bounded profiles

Added separate Workbench 3.1 native entries for `Break` and `ChangeTaskPri`.
The classic Break body uses the captured `PROCESS/A/N,ALL/S,C/S,D/S,E/S,F/S`
candidate, a six-slot result array, classic `FindCliProc`, and `Signal` under
`Forbid`/`Permit`; it does not install or call the MorphOS port/PID paths. The
classic ChangeTaskPri body uses the captured
`PRI=PRIORITY/A/N,PROCESS/K/N` candidate, current or CLI target resolution,
signed priority validation and `SetTaskPri` without the MorphOS PID extension.

`tools/Commands/qualify_break_wb31_native.ps1` and
`tools/Commands/qualify_changetaskpri_wb31_native.ps1` each compile resident
68000/020/040 HUNKs with ten reachable methods and zero managed runtime
features/helpers, external targets, exception regions or fatal machine-fault
sites. The supplied fixtures pass eight Break and ten ChangeTaskPri invocations
per CPU, including successful masks/priority bounds, missing targets, parser
failure, cleanup and interleaving. Receipts are
`artifacts/break-wb31-native-20260911/qualification.json` and
`artifacts/changetaskpri-wb31-native-20260911/qualification.json`.

These are bounded implementation and ABI checkpoints. Exact original
diagnostics/parser behavior, disposable-guest comparison, production
PURE/resident lifecycle, task/target race handling and package placement remain
open; shipping remains 0/200 commands and 0/246 profiles.

## 2026-09-11: Workbench Avail bounded profile

Added `NativeWorkbench31AvailCommand` and the private
`NativeWorkbench31AvailEntry`. The body uses the Workbench 3.1 candidate
template `CHIP/S,FAST/S,TOTAL/S,FLUSH/S`, public Exec `AvailMem` for chip/fast/
total selection and the classic summary rows, with invocation-owned ReadArgs
and formatting storage. Multiple selectors follow template order. `FLUSH` is
parsed but returns `ERROR_NOT_IMPLEMENTED` until the exact v40 expunge sequence
is captured; it is not presented as complete functionality.

`tools/Commands/qualify_avail_wb31_native.ps1` builds resident HUNKs for all
three CPUs and runs nine supplied DOS/Exec invocations per CPU, covering summary
and selector output, precedence, parser failure, fail-closed FLUSH, repeat and
interleaved callers. The receipt is
`artifacts/avail-wb31-native-20260911/qualification.json`:

| CPU | Bytes | SHA-256 |
| --- | ---: | --- |
| 68000 | 2,856 | `125aa151ea5fa9357abd16884dc59e7cfdfd15a67d6dfd4ff4ac6511aafb54e9` |
| 68020 | 2,852 | `83c76b2273c71df2d5d674a674fcd6016f4a7842454273d1d8aba4904ec0d27a` |
| 68040 | 2,852 | `83c76b2273c71df2d5d674a674fcd6016f4a7842454273d1d8aba4904ec0d27a` |

The static reports contain eleven reachable methods, zero managed runtime
features/helpers, external native targets, exception regions and fatal machine
fault sites. Runtime reports show balanced parser/temporary memory ownership,
zero shared-image writes and no leaks. The Workbench Avail profile remains
partial: original output/IoErr, FLUSH, MorphOS implementation, guest
differential, production PURE/resident lifecycle and packaging are open.

The same Workbench Avail fixture was rerun on 2026-09-12 after the profile
boundary additions. The receipt is
`artifacts/avail-wb31-native-20260912-qualified/qualification.json`; the
three HUNKs and nine supplied vectors per CPU are byte-identical to the prior
checkpoint, so no status promotion is claimed.

## 2026-09-11: Workbench Wait bounded profile

Added `NativeWorkbench31WaitCommand` and the private
`NativeWorkbench31WaitEntry`. The body uses the Workbench 3.1 candidate
template `/N,SEC=SECS/S,MIN=MINS/S,UNTIL/K`, public DOS `Delay`, one-unit
defaults, seconds/minutes conversion, signed-value and tick-overflow guards,
and invocation-owned `ReadArgs` storage. `UNTIL` is parsed but returns
`ERROR_NOT_IMPLEMENTED` until the original v37 time-of-day path is captured;
it is not presented as complete functionality.

`tools/Commands/qualify_wait_wb31_native.ps1` builds resident HUNKs for
68000/020/040 and runs eleven supplied DOS/Exec invocations per CPU, covering
default and explicit units, omitted-number defaults, zero, both-unit
precedence, gated `UNTIL`, negative/overflow-safe paths, parser failure,
cleanup and interleaving. The receipt is
`artifacts/wait-wb31-native-20260911/qualification.json`:

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 2464 | `0bec0f285ee588f8c9a207cbb71c1c349aa4f353e462616a8213318370f9e2ab` | 10 | 11 |
| 68020 | 2452 | `81682a2f5949081b1d5e6a937e6dbab953eb4ccba096258d2bb8bab4d350bd2c` | 10 | 11 |
| 68040 | 2452 | `81682a2f5949081b1d5e6a937e6dbab953eb4ccba096258d2bb8bab4d350bd2c` | 10 | 11 |

All static receipts report zero managed runtime features/helpers, external
native targets, exception regions and fatal machine-fault sites. Supplied
fixtures report balanced allocations and no shared-image writes. This remains
a bounded development checkpoint: original reference output/diagnostics,
time-of-day and Ctrl-C behavior, MorphOS implementation, guest differential,
production PURE/resident lifecycle and package placement remain open.

## 2026-09-11: CopperStart MorphOS PID production boundary

CopperStart now publishes MorphOS `FindTaskByPID` through its documented
pointer-indirect slot at `ExecBase-994`, alongside the existing six-byte Exec
vectors. The production entry uses A0 for ExecBase and D0 for the PID, and the
provider resolves live current, ready, and wait-list task identities with a
bounded guest traversal. PID 0 continues to select the current task.

The CopperStart Exec build passes, the focused Exec/MorphOS suite passes 14/14,
and the compiled MC68000 production image passes both the installer publication
check and an executed pointer-entry lookup of a ready task. The provider uses a
guest-address identity while the task is live; exact MorphOS numeric PID
allocation/reuse, cross-process ownership, and concurrent target-death behavior
still require reference and lifecycle evidence. Those gaps remain blockers for
shipping Break and ChangeTaskPri PID profiles.

## 2026-09-11: Inventory evidence re-bound to the active plan

The blocker-ledger correction changed the goal-plan hash, so the generated
`command-inventory.json` and `media-evidence.json` were regenerated from the
same selected Workbench 3.1 M10 pair and verified MorphOS 3.20 ISO. `extract`
and `verify-media` both pass with the 200-identity factual baseline, 70 classic
entries, 188 MorphOS entries, and the six explicit closure items. The 13
synthetic and ledger integrity tests pass. Strict completion remains open by
design because the other Workbench disks, installed metadata, runtime contracts,
and Freeze reconciliation are not yet evidenced.

## 2026-09-12: MorphOS Date bounded native profile

Added `NativeMorphOSDateCommand` and the private
`MorphOSDateEntry` root from the released MorphOS 3.20 `Date` 50.7 source
contract. The body preserves `DAY,DATE,TIME,TO=VER/K,LFORMAT/K`, the
date/time token classification and short-time expansion, VBlank timer-device
setup, 32-bit seconds validation, normal `DateToStr` output and the
`locale.library` `FormatDate` hook path. Timer, locale, DOS parser, output,
diagnostic and cleanup ownership are invocation-local.

`tools/Commands/qualify_morphos_date_native_entry.ps1` compiles resident HUNKs
for 68000/020/040 and runs 13 supplied vectors per CPU (39 total). The durable
receipt is `artifacts/morphos-date-native-entry-20260912-qualified/qualification.json`;
all cases pass with balanced resources, one image load per CPU, zero shared-image
writes, and no managed allocation or native runtime-helper sites. This is an
adapter checkpoint only: no original MorphOS guest parity, PURE/resident reuse,
package admission, or shipping promotion is implied.

## 2026-09-12: MorphOS Wait bounded native slice

Added `NativeMorphOSWaitCommand` and the private `MorphOSWaitEntry` root from
the released MorphOS 3.20 `Wait` 50.3 source contract. The body preserves
`TIME/N,SEC=SECS/S,MIN=MINS/UNTIL/K`, one-second defaults, minute precedence,
midnight-aware `UNTIL`, short DOS delays, asynchronous VBlank timer requests,
Ctrl-C cancellation and source diagnostics.

`tools/Commands/qualify_morphos_wait_native_entry.ps1` compiles resident HUNKs
for 68000/020/040 and runs 15 supplied vectors per CPU (45 total). The durable
receipt is `artifacts/morphos-wait-native-entry-20260912-qualified/qualification.json`;
all cases pass with balanced parser/workspace/timer ownership, one image load
per CPU, zero shared-image writes, and no managed allocation or runtime-helper
sites in the static reports. Original MorphOS guest parity, PURE/resident
reuse, package admission and differential evidence remain open.

## 2026-09-12: MorphOS Protect bounded native slice

Added `NativeMorphOSProtectCommand` and the private
`NativeMorphOSProtectEntry` for the MorphOS 3.20 `Protect` 50.6 source member.
The body preserves the released `FILE/A,FLAGS,ADD/S,SUB/S,ALL/S,QUIET/S`
boundary, active-low/read-write-delete-execute and active-high
hold/script/pure/archive mapping, replacement/add/subtract bit formulas,
AnchorPath recursion flags, quiet progress, mutation/no-match/break/parser
errors, and invocation-owned cleanup. Workbench startup and malformed argument
boundaries are rejected before the CLI-only body.

`tools/Commands/qualify_morphos_protect_native_entry.ps1` compiles resident
68000/020/040 HUNKs and passes fourteen supplied DOS/Exec vectors per CPU (42
total), including interleaved repeat calls. The durable receipt is
`artifacts/protect-morphos-native-20260912-qualified/qualification.json`;
the three HUNKs are 5748, 5788 and 5732 bytes with no fixture leaks or
shared-image writes. Volume/device classification, soft-link descent,
complete wildcard traversal, exact Workbench behavior, packed correspondence,
original guest parity, PURE/resident reuse, package admission and differential
evidence remain open.

## 2026-09-12: Workbench 3.1 Protect syntax-candidate entry

Added `NativeWorkbench31ProtectCommand` and the private resident
`Workbench31ProtectEntry`. The entry opens DOS 36, rejects Workbench startup
and malformed argument boundaries, and reuses the bounded public-DOS Protect
body behind the observed classic
`FILE/A,FLAGS,ADD/S,SUB/S,ALL/S,QUIET/S` six-slot `ReadArgs` boundary.

`tools/Commands/qualify_workbench31_protect_native_entry.ps1` compiles
resident 68000/020/040 HUNKs and passes fourteen supplied DOS/Exec vectors per
CPU (42 total). The durable receipt is
`artifacts/protect-wb31-native-20260912-qualified/qualification.json`; HUNKs
are 5816, 5860 and 5800 bytes with 14 reachable methods per CPU and no shared
image writes or fixture leaks. Exact Workbench guest behavior, complete
filesystem and diagnostic parity, PURE/resident reuse, package admission and
differential evidence remain open.

## 2026-09-12: MorphOS Status bounded native slice

Added `NativeMorphOSStatusCommand` and the private
`NativeMorphOSStatusEntry` for the MorphOS 3.20 `Status` 50.6 media member.
The body preserves the `PROCESS/N,FULL/S,TCB/S,CLI=ALL/S,COM=COMMAND/K`
`ReadArgs` boundary, DOS 51.51 snapshot API selection, legacy DOS CLI-list
fallback, process and command filters, TCB/FULL formatting, missing-process and
parser diagnostics, Ctrl-C polling, and invocation-owned parser cleanup.

`tools/Commands/qualify_morphos_status_native_entry.ps1` compiles resident
68000/020/040 HUNKs and passes fourteen supplied DOS/Exec vectors per CPU (42
total), including empty and one-process lists, filters, parser and
missing-process failures, Ctrl-C, Workbench/boundary rejection, and interleaved
repeat calls. The durable receipt is
`artifacts/status-morphos-native-20260912-qualified/qualification.json`;
complete task population, exact DOS 51.51 guest snapshot behavior, original
Workbench/MorphOS parity, PURE/resident reuse, package admission and
differential evidence remain open.

## 2026-09-12: Workbench Wait bounded native slice

Extended `NativeWorkbench31WaitCommand` beyond its earlier fail-closed
`UNTIL` checkpoint. The resident body now preserves the Workbench
`/N,SEC=SECS/S,MIN=MINS/UNTIL/K` grammar, performs `HH:MM` conversion through
`DateStamp`/`StrToDate`, handles midnight rollover, uses DOS `Delay` for short
waits and an Exec VBlank timer for longer waits, and cancels outstanding
requests on Ctrl-C. The fixture covers parser, allocation, timer-open and
diagnostic paths while retaining the pure/resident boundary.

`tools/Commands/qualify_wait_wb31_native.ps1` compiles resident 68000/020/040
HUNKs and passes fifteen supplied DOS/Exec invocations per CPU (45 total). The
durable receipt is `artifacts/wait-wb31-native-20260912-qualified/qualification.json`;
the Workbench profile still needs original guest parity, PURE/resident reuse,
same-segment lifecycle, package admission and differential evidence.

## 2026-09-12: MorphOS Beep bounded native slice

Added `NativeMorphOSBeepCommand` and the private resident
`NativeMorphOSBeepEntry` from the MorphOS 3.20 source archive's Beep 50.2
contract. The body ignores command-tail bytes, opens `intuition.library` v33,
calls `DisplayBeep(NULL)`, closes the owned library, and returns failure when
the library is unavailable.

`tools/Commands/qualify_morphos_beep_native_entry.ps1` compiles resident
68000/020/040 HUNKs and passes five supplied Exec/Intuition vectors per CPU
(15 total), including open failure, ignored arguments, Workbench startup and
repeat use. The durable receipt is `artifacts/beep-morphos-native-20260912-qualified/qualification.json`;
original guest audio behavior, PURE/resident reuse, package admission and
differential evidence remain open.

## 2026-09-12: MorphOS Lock bounded native slice

Added `NativeMorphOSLockCommand` and the private resident
`NativeMorphOSLockEntry` from the MorphOS 3.20 source archive's AROS-derived
Lock contract. The body preserves `DRIVE/A,ON/S,OFF/S,PASSKEY`, checks the
device/volume node, folds the optional passkey with the source's decimal loop,
uses `ACTION_WRITE_PROTECT` through `DoPkt2`, emits the source result text, and
keeps parser/device-process cleanup ordered.

`tools/Commands/qualify_morphos_lock_native_entry.ps1` compiles resident
68000/020/040 HUNKs and passes eleven supplied DOS vectors per CPU (33 total),
including ON/OFF/no-action, wrong or missing devices, packet and parser
failure, Workbench startup, and interleaved repeat calls. The durable receipt
is `artifacts/lock-morphos-native-20260912-qualified/qualification.json`;
original handler behavior, PURE/resident reuse, package admission and
differential evidence remain open.

## 2026-09-12: MorphOS DiskChange bounded native slice

Added `NativeMorphOSDiskChangeCommand` and the private
`NativeMorphOSDiskChangeEntry` from the MorphOS 3.20 source archive's
DiskChange 50.3 contract. The body preserves `DEVICE/A`, DOS 50 startup,
`DeviceProc`, the `ACTION_INHIBIT` true/false packet sequence, source
diagnostics, and parser cleanup. Workbench startup and invalid
entry boundaries are rejected before parsing because the source has no
Workbench message path.

`tools/Commands/qualify_morphos_diskchange_native_entry.ps1` compiles resident
68000/020/040 HUNKs and passes ten supplied DOS vectors per CPU (30 total),
including successful and failing inhibit paths, parser/device failures,
invalid boundaries, Workbench rejection, and interleaved repeat calls. The
durable receipt is
`artifacts/diskchange-morphos-native-20260912-qualified/qualification.json`;
original handler transitions, PURE/resident reuse, package admission and
differential evidence remain open.

## 2026-09-12: MorphOS Reboot bounded native slice

Added `NativeMorphOSRebootCommand` and the private
`NativeMorphOSRebootEntry` from the MorphOS 3.20 archive's AROS-derived Reboot
50.3 source. The body preserves the empty `ReadArgs` call, parser cleanup,
Ctrl-C `ERROR_BREAK` handling, `ColdReboot`, DOS-open failure, and the source's
literal returning value `666`. The entry rejects Workbench and malformed
startup boundaries before the CLI-only body.

`tools/Commands/qualify_morphos_reboot_native_entry.ps1` compiles resident
68000/020/040 HUNKs and passes ten supplied DOS/Exec vectors per CPU (30 total),
including parser/signal/open failures, ignored command-tail bytes, Workbench
and boundary rejection, and interleaved repeat calls. The durable receipt is
`artifacts/reboot-morphos-native-20260912-qualified/qualification.json`;
real reboot transition, original guest parity, PURE/resident reuse, package
admission and differential evidence remain open.

Regenerated the deterministic command inventory and media evidence after the
goal plan's Reboot, DiskChange, ResList, LibList and DevList progress notes
changed its hash.
`inventory.py extract` passes against the selected Workbench/MorphOS references
and records the current plan SHA
`101a28b5bc8cd5e3bae81af23a9bed0cd90ccdb94409c20591b578dc2590cda2`.
The six reference-closure items remain intentionally open.

## 2026-09-12: MorphOS ResList bounded native slice

Added `NativeMorphOSResListCommand` and the private
`NativeMorphOSResListEntry` from the MorphOS 3.20 archive's AROS-derived
ResList 50.4 source. The body preserves the no-option DOS 37 startup,
`Forbid`-protected Exec resource-list snapshot, growable public buffer,
source header/row formatting, per-row Ctrl-C polling, allocation-failure and
break handling, and ordered cleanup. Workbench startup and malformed entry
boundaries are rejected before the CLI-only body.

`tools/Commands/qualify_morphos_reslist_native_entry.ps1` compiles resident
68000/020/040 HUNKs and passes nine supplied DOS/Exec vectors per CPU (27
total), including empty and one-resource lists, allocation failure, Ctrl-C,
Workbench/boundary rejection, and interleaved repeat calls. The durable receipt
is `artifacts/reslist-morphos-native-20260912-qualified/qualification.json`;
the three HUNKs are 1820, 1872, and 1828 bytes respectively with no fixture
leaks or shared-image writes. Complete resource-list population, original
guest parity, PURE/resident reuse, package admission and differential evidence
remain open.

## 2026-09-12: MorphOS LibList bounded native slice

Added `NativeMorphOSLibListCommand` and the private
`NativeMorphOSLibListEntry` for the MorphOS 3.20 `LibList` 50.6 media member.
The body preserves the no-option DOS 37 boundary, `Forbid`-protected Exec
library-list snapshot, address/name/version/revision/open-count/flags fields,
growable public buffer, source table formatting, per-row Ctrl-C polling,
allocation-failure diagnostic and ordered cleanup. Workbench startup and
malformed entry boundaries are rejected before the CLI-only body.

`tools/Commands/qualify_morphos_liblist_native_entry.ps1` compiles resident
68000/020/040 HUNKs and passes nine supplied DOS/Exec vectors per CPU (27
total), including empty and one-library lists, allocation failure, Ctrl-C,
Workbench/boundary rejection, and interleaved repeat calls. The durable receipt
is `artifacts/liblist-morphos-native-20260912-qualified/qualification.json`;
the three HUNKs are 2016, 2052 and 2020 bytes respectively with no fixture
leaks or shared-image writes. Complete library-list population, exact
packed-binary/source correspondence, original guest parity, PURE/resident
reuse, package admission and differential evidence remain open.

## 2026-09-12: MorphOS ModList bounded native slice

Added `NativeMorphOSModListCommand` and the private
`NativeMorphOSModListEntry` for the MorphOS 3.20 `ModList` 50.4 media member.
The body preserves the `VERBOSE/S` `ReadArgs` boundary, DOS 37 startup,
`Forbid`-protected resident-module indirection traversal, extended/ID-string
revision selection, source table and flag ordering, per-row Ctrl-C polling,
and owned parser/buffer cleanup. Workbench startup and malformed entry
boundaries are rejected before the CLI-only body.

`tools/Commands/qualify_morphos_modlist_native_entry.ps1` compiles resident
68000/020/040 HUNKs and passes twelve supplied DOS/Exec vectors per CPU (36
total), including empty and one-resident lists, ID-string revision parsing,
allocation and parser failures, break, missing DOS, Workbench/boundary
rejection, and interleaved repeat calls. The durable receipt is
`artifacts/modlist-morphos-native-20260912-qualified/qualification.json`;
the three HUNKs are 3388, 3432 and 3376 bytes respectively with no fixture
leaks or shared-image writes. Complete resident-table population, exact
packed-binary/source correspondence, original guest parity, PURE/resident
reuse, package admission and differential evidence remain open.

## 2026-09-12: MorphOS DevList bounded native slice

Added `NativeMorphOSDevListCommand` and the private
`NativeMorphOSDevListEntry` for the MorphOS 3.20 `DevList` 50.3 media member.
The body preserves the no-option DOS 37 boundary, `Forbid`-protected Exec
device-list snapshot, address/name/version/revision/open-count/flags fields,
growable public buffer, source table formatting, per-row Ctrl-C polling,
allocation-failure diagnostic and ordered cleanup. Workbench startup and
malformed entry boundaries are rejected before the CLI-only body.

`tools/Commands/qualify_morphos_devlist_native_entry.ps1` compiles resident
68000/020/040 HUNKs and passes nine supplied DOS/Exec vectors per CPU (27
total), including empty and one-device lists, allocation failure, Ctrl-C,
Workbench/boundary rejection, and interleaved repeat calls. The durable receipt
is `artifacts/devlist-morphos-native-20260912-qualified/qualification.json`;
the three HUNKs are 2016, 2052 and 2020 bytes respectively with no fixture
leaks or shared-image writes. Complete device-list population, exact
packed-binary/source correspondence, original guest parity, PURE/resident
reuse, package admission and differential evidence remain open.

## 2026-09-12: MorphOS PortList bounded native slice

Added `NativeMorphOSPortListCommand` and the private
`NativeMorphOSPortListEntry` for the MorphOS 3.20 `PortList` 50.2 media member.
The body preserves the no-option DOS 37 boundary, `Forbid`-protected Exec
message-port snapshot, copied port and signal-task names, source table bytes,
per-row Ctrl-C polling, allocation-failure fault path and ordered cleanup.
Workbench startup and malformed entry boundaries are rejected before the
CLI-only body.

`tools/Commands/qualify_morphos_portlist_native_entry.ps1` compiles resident
68000/020/040 HUNKs and passes ten supplied DOS/Exec vectors per CPU (30 total),
including empty and one-port lists, allocation and break paths, missing DOS,
Workbench/boundary rejection, and interleaved repeat calls. The durable receipt
is `artifacts/portlist-morphos-native-20260912-qualified/qualification.json`;
the three HUNKs are 2284, 2316 and 2272 bytes respectively with no fixture
leaks or shared-image writes. Complete port-list population, exact
packed-binary/source correspondence, original guest parity, PURE/resident
reuse, package admission and differential evidence remain open.

## 2026-09-12: MorphOS Touch bounded native body

Added `NativeMorphOSTouchCommand` and the private resident
`NativeMorphOSTouchEntry` for the MorphOS 3.20 `Touch` 50.9 source member.
The implementation keeps `ReadArgs`, the `/M` name vector, AnchorPath and
DateStamp storage invocation-owned; follows the source matcher lifecycle and
`ALL` directory flags; supports verbose touched/created/failed output; polls
Ctrl-C; and performs the source's direct-touch/create fallback after
`ERROR_OBJECT_NOT_FOUND`. The classic `DateStamp`/`SetFileDate` path builds in
the Commands project and the resident root compiles on the normal compiler
boundary. `tools/Commands/qualify_morphos_touch_native_entry.ps1` now runs
the supplied DOS/Exec fixture after compiling three resident HUNKs
(68000/020/040: 3932/3956/3924 bytes, 12 reachable methods each). Ten
invocations pass per CPU with balanced ownership and unchanged images; the
receipt is `artifacts/touch-morphos-native-20260912-qualified/qualification.json`.

This is a bounded implementation slice, not shipping evidence. The DOS 51.66
UTC/POSIX timestamp branch, `SoftlinkDODIR`, exact diagnostics, supplied-vector
execution, PURE/resident reuse, installed metadata, packaging and differential
reference runs remain open. Source identity is bound to
`artifacts/morphos320-c-source-extracted/c/touch/touch.c`, 6916 bytes,
SHA-256 `ecd885e0ea573b5503b9ac044936985037fcc02b0c7ddb0efcfb386adc0fe0f`.

## 2026-09-12: Touch runtime receipt and inventory refresh

The Touch qualifier now compiles and executes the resident HUNK on 68000,
68020 and 68040. Ten supplied DOS/Exec invocations pass per CPU, including
parser failure, direct literal fallback, create-on-miss, recursive directory
flags, CurrentDir restoration, mutation failure, Ctrl-C and interleaved calls.
The durable receipt is `artifacts/touch-morphos-native-20260912-qualified/qualification.json`;
no shared-image writes or fixture leaks are observed. The goal plan was updated
with this progress slice and `inventory.py extract`/`verify` pass with current
plan SHA-256 `4ff2a6c49d57301f8dd528f61f00022b5638d2eaa51016b873b4025d2bed5be5`.

## 2026-09-12: MorphOS SetClock bounded native boundary

Added the source-bound `NativeMorphOSSetClockCommand` and private resident
entry. It preserves the MorphOS 3.20 `LOAD/S,SAVE/S,RESET/S` template and uses
the shared DOS `ReadArgs` lease, public `Exec.OpenResource` for
`battclock.resource`, VBlank `timer.device` request setup, classic battclock
vectors, timer `GetSysTime`, and reverse cleanup. The three CPU resident HUNK
builds pass static compatibility checks with fourteen reachable methods and no
managed allocation sites, runtime helpers/features, external targets,
exception regions or fatal fault sites. Receipt:
`artifacts/setclock-morphos-native-20260912-qualified/qualification.json`.

The qualifier now compiles and executes the resident HUNK on 68000/020/040.
Fifteen supplied DOS/Exec/resource/timer invocations pass per CPU with balanced
ownership and unchanged shared images. The MorphOS 52+ UTC vector branch, real
battclock/timer behavior, exact extended-help and diagnostics, Workbench parity,
PURE/resident reuse, package admission and differential reference runs remain
open. Source identity is bound to
`artifacts/morphos320-c-source-extracted/c/setclock/setclock.c`, 5053 bytes,
SHA-256 `414f6c7e2c4d98a48753d7d59107e88b0125ceea53214db0954e0a3513a15ec5`.


## 2026-09-12 - Version source contract checkpoint

Added `contracts/Version.md` for both profiles. The MorphOS `c/version/version.c`
source is 38,220 bytes with SHA-256
`818f64fae8abf914633b4bc807d6b105eb0ccf30e1e48ac1527745d486c22490`; its
include records Version 50.30 and its public-DOS template is
`NAME/M,MD5SUM/S,VERSION/N,REVISION/N,FILE/S,FULL/S,RES/S`. The source
records separate system-version and named-file/resident paths, the positional
version/revision workaround, MD5 and comparison behavior, and optional
version.library/ARexx integrations. The Workbench 3.1 40.1 HUNK and its
`NAME,VERSION/N,REVISION/N,FILE/S,FULL/S,UNIT/N,INTERNAL/S,RES/S` syntax
candidate are bound as media evidence. This is a contract checkpoint only: no
implementation, packed correspondence, guest parity, PURE/resident, lifecycle,
license, or package gate closes.


## 2026-09-12 - MorphOS Version resident system/RES slice

Added `NativeMorphOSVersionCommand` and its private resident entry. The body
uses DOS 37 `ReadArgs` with the source template
`NAME/M,MD5SUM/S,VERSION/N,REVISION/N,FILE/S,FULL/S,RES/S`, keeps parser and
result storage invocation-owned, reports system DOS version/revision through
public state, and resolves resident names through `Exec.FindResident`. The
source's file, MD5, version.library and ARexx provider branches fail closed
with `ERROR_NOT_IMPLEMENTED` pending their exact ABIs and guest fixtures.

`qualify_morphos_version_native_entry.ps1` now compiles resident 68000/020/040
HUNKs and runs fourteen supplied parser/system/RES/provider/boundary vectors per
CPU (42 total). The durable receipt is
`artifacts/version-morphos-native-20260912-qualified/qualification.json`; the
images have 15 reachable methods, no managed runtime features/helpers, external
targets, exception regions or fatal fault sites, balanced parser ownership,
and unchanged shared images. This is a bounded native receipt, not original
MorphOS or Workbench parity, PURE/resident admission, lifecycle, licensing or
package evidence.
## 2026-09-12 - MorphOS TaskList public task snapshot slice

Added `contracts/TaskList.md` and the private resident
`NativeMorphOSTaskListEntry`. The body preserves the complete source
`NAME,ADDRESS/N,VERBOSE/S,STACKTRACE/S,STACKLEVEL/N,NORUN/S,NOWAIT/S,NOREADY/S,INTERNAL/S,REGDUMP/S,REGCHECK/S`
ReadArgs grammar, snapshots current/ready/waiting public Exec lists under
`Disable`, obtains MorphOS `ProcessId` through `NewGetTaskAttrsA`, and formats
the standard task rows after releasing protection. Name/address filters and
`NORUN`/`NOWAIT`/`NOREADY` are covered; sysdebug-backed verbose, stack and
register modes fail closed with `ERROR_NOT_IMPLEMENTED` pending their exact
provider ABI.

`qualify_morphos_tasklist_native_entry.ps1` compiles resident 68000/68020/68040
HUNKs and runs sixteen supplied DOS/Exec vectors per CPU (48 total), including
all-list, per-list, filter, parser, allocation, Ctrl-C, startup-boundary and
interleaved cases. Receipt:
`artifacts/tasklist-morphos-native-20260912-qualified/qualification.json`.
The HUNKs are 5836/5920/5824 bytes with SHA-256
`77af87661fe61142c62fd8ade7080644d6e41b38b13a95ef4e5db740a1923572`,
`e1405556be13306204844ed5a42657fa2dfe5a57cf97c2022996d6c3d0a61896`, and
`009662287e745ee442f62aaa966676b14f8adcafd3d134a5291fe11afb755f17`.
No original guest, complete task population, Workbench, PURE/resident,
lifecycle, differential, licensing or package gate is claimed.

## 2026-09-12 - MorphOS Info public DOS-list snapshot slice

Added source-bound `NativeMorphOSInfoCommand` and private resident entry for
MorphOS 3.20 `c/info/info.c` (27,153 bytes, SHA-256
`ae11f8ab679916df93ff4306c9cd95beca5726483103c9292f13a4abf6f2945f`). The
body preserves
`DISKS/S,VOLS=VOLUMES/S,GOODONLY/S,BLOCKS/S,VERBOSE/S,DEVICES/M`, snapshots
public device/volume nodes under one matching read lock, copies BSTR names into
invocation-owned storage, performs public filesystem `Info` after releasing
the list lock, supports list selection/filtering and `GOODONLY`, and renders
bounded device/volume rows. Locale formatting, advanced pattern syntax and
other provider behavior remain fail-closed/open. `BLOCKS` now formats the
public 32-bit block counters in the bounded device row, and a valid
`FileSysStartupMsg` `VERBOSE` device/unit line is rendered after unlock.
Alternate startup/provider data remains open pending its exact ABI.

`qualify_morphos_info_native_entry.ps1` compiles resident 68000/020/040 HUNKs
and runs seventeen supplied DOS-list/Info vectors per CPU (51 total), including
selection/wildcard filtering, list failure, parser/allocation, Ctrl-C, startup
boundaries, and interleaved calls. Receipt:
`artifacts/info-morphos-native-20260912-qualified/qualification.json`.
The HUNKs are 11716/11572/11512 bytes with SHA-256
`7542e6ff83d2176062050abe0a2754a2a7a3b8c4190bec63bd592ddf622013be`,
`5661dd710b393d6586aea67d6b67983992c2ed023e80e4eb83b6005cf45078b8`, and
`57597dcd1cb8f99995afad6627e030f60cc911115ad708d7df2b99648f1d6498`.
No original guest, exact packed-binary/output parity, PURE/resident lifecycle,
package, licensing, or differential gate is claimed.

## 2026-09-12 - Info wildcard and startup-output follow-up

Extended the source-bound Info stage to use public `ParsePatternNoCase` and
`MatchPatternNoCase` for `DEVICES/M`, including optional-colon normalization and
the source `#?` wildcard form. The existing invocation buffer reserves pattern
scratch separately from copied DOS-list names. Added the valid
`FileSysStartupMsg` device/unit branch for `VERBOSE/S`; alternate startup data
and locale formatting remain open. `BLOCKS/S` continues to format the public
block counters after unlocking the filesystem.

The refreshed resident receipt passes seventeen supplied vectors per CPU (51
across 68000/020/040), with HUNKs 11716/11572/11512 bytes and hashes
`7542e6ff83d2176062050abe0a2754a2a7a3b8c4190bec63bd592ddf622013be`,
`5661dd710b393d6586aea67d6b67983992c2ed023e80e4eb83b6005cf45078b8`, and
`57597dcd1cb8f99995afad6627e030f60cc911115ad708d7df2b99648f1d6498`.

## 2026-09-12 - MorphOS DOSList public DOS-list snapshot slice

Added the source-bound `NativeMorphOSDosListCommand` and private resident
`NativeMorphOSDosListEntry`. The body preserves the released
`NAME,ADDRESS/N,DEVICES/S,VOLUMES/S,ASSIGNS/S,VERBOSE/S` grammar and the
device, volume, assign selection order. Each selected pass uses its public
DOS read lock, snapshots BSTR names and node/task addresses into
invocation-owned storage, unlocks before rendering, supports exact
case-insensitive name and numeric-address filtering, and emits the
source-common node plus bounded mounted-state rows. Provider-specific process
and verbose attribute records remain open.

`qualify_morphos_doslist_native_entry.ps1` compiles resident 68000/020/040
HUNKs and passes seventeen supplied DOS-vector invocations per CPU (51 total),
including default and per-list selection, NAME/ADDRESS filters, verbose rows,
lock refusal, parser/allocation, Ctrl-C, startup boundaries and interleaving.
Receipt:
`artifacts/doslist-morphos-native-20260912-qualified/qualification.json`.
The HUNKs are 4552/4564/4472 bytes with SHA-256
`e0dc3a906e97a4f9d6ed2791a8570f88dd6fc21a8e49d63ff650a61c2944ea6f`,
`c68f53d5c67c506cffad9c11959c99f947ed2a1e13538d344ac4c0524611c268`, and
`c97c69aa68eed467b29e6ffdf562d4ad114299f6f9571d2c6784c096759cfb09`.
This is bounded native evidence only; original guest parity, complete
provider output, PURE/resident lifecycle, licensing, differential and package
gates remain open.

## 2026-09-12 - MorphOS List public matcher snapshot slice

Added the source-bound `NativeMorphOSListCommand` and private resident
`NativeMorphOSListEntry`. The body preserves the recovered 19-slot MorphOS
grammar, implements the bounded public DOS flat matcher path for `DIR/M`,
`P=PAT/K`, `QUICK`, `BLOCK`, `NOHEAD`, `FILES`, `DIRS`, and `TO/K`, and keeps
`AnchorPath`, pattern scratch, `FileInfoBlock`, parser results, and redirected
output invocation-owned. It uses `ParsePatternNoCase`,
`MatchPatternNoCase`, `MatchFirst`, `MatchNext`, and `MatchEnd`, polls Ctrl-C,
and releases matcher, parser, output, and workspace resources on every exit.
Key/date/recursive/sort/owner/`LFORMAT`/`ALL` modes fail closed with
`ERROR_NOT_IMPLEMENTED` pending provider contracts.

`qualify_morphos_list_native_entry.ps1` compiles resident 68000/68020/68040
HUNKs and passes seventeen supplied DOS-vector invocations per CPU (51 total),
covering flat traversal, name/pattern/type filters, quick/block/no-header
rendering, `TO` output lifetime, empty-match, parser/allocation/Ctrl-C,
unsupported modes, startup boundaries, and interleaved callers. Receipt:
`artifacts/list-morphos-native-20260912-qualified/qualification.json`.
This remains bounded native evidence only; packed correspondence, Workbench
parity, real filesystem behavior, PURE/resident lifecycle, differential,
licensing, and package gates remain open.

## 2026-09-12 - Workbench 3.1 List public matcher snapshot slice

Added `NativeWorkbench31ListCommand` and private resident
`Workbench31ListEntry`, reusing the bounded public-DOS matcher implementation
with the 16-slot Workbench 3.1 syntax candidate. The resident entry opens DOS
36 and retains the same parser, matcher, output, Ctrl-C, allocation, and
startup-boundary ownership rules. The media candidate is syntax-only; exact
Workbench runtime behavior remains open.

The shared qualification script, invoked with `-Profile Workbench31`, passes
seventeen supplied DOS-vector invocations per CPU (51 total) on resident
68000/68020/68040 HUNKs. Receipt:
`artifacts/list-wb31-native-20260912-qualified/qualification.json`; HUNKs are
8664, 8724, and 8692 bytes with reachable-method count 14. This remains
bounded ABI evidence, not original guest parity, PURE/resident admission,
packaging, licensing, or shipping evidence.

The goal evidence update was re-bound through `inventory.py extract`; the
generated inventory and media evidence now carry the current goal-plan hash,
and `inventory.py verify` passes with the same six explicit closure items.

## 2026-09-12 - MorphOS Search bounded native entry slice

Added `NativeMorphOSSearchCommand` and the private resident
`NativeMorphOSSearchEntry`, binding the inspected MorphOS 3.20
`FROM/M,SEARCH/A,ALL/S,NONUM/S,QUIET/S,QUICK/S,FILE/S,PATTERN/S,CASE/S,LINES/N`
template to public DOS 37 calls. The bounded body owns `ReadArgs` results and
workspace storage, performs flat `MatchFirst`/`MatchNext`/`MatchEnd` traversal,
reads candidate files through `Open`/`Read`/`Close`, and renders through
`Output`, `VPrintf`, `FPuts`, and `Write`. Literal and DOS-pattern line modes,
`FILE`, `PATTERN`, `CASE`, `NONUM`, `QUIET`, `LINES/N`, empty matches,
parser/allocation/read failures, Ctrl-C, and CLI/Workbench startup boundaries
are covered. `QUICK` is accepted at the parser boundary; `ALL` remains
fail-closed pending recursive/soft-link provider ownership, no locale-library
folding is claimed, and the fixture read buffer is bounded to 8,192 bytes.

`tools/Commands/qualify_morphos_search_native_entry.ps1` passes seventeen
supplied DOS-vector invocations per CPU (51 total) on resident 68000/020/040
HUNKs. Receipt:
`artifacts/search-morphos-native-20260912-qualified/qualification.json`.
The HUNKs are 15,156/15,308/15,140 bytes with SHA-256
`a19affbd8a6a94c99653fff1434b93fd2fd6e474db8c010081ae5e99d1bfe869`,
`290587c2fca16b5490967bbb4b2a8ef1b910ce97e0bcc89ac3e57c9937408775`, and
`f15b38a30a7fdaa4090cb5f60b54dd454601778cd29e72f9a29cf080ca9bdd50`.
Static checks report 24 reachable methods per CPU and no managed allocation
sites, runtime features/helpers, external native targets, exception regions,
fatal fault sites, or shared-image writes. This advances the MorphOS native
gate to partial only; Workbench body, packed correspondence, complete source
output/locale/filesystem parity, PURE/resident lifecycle, licensing,
packaging, and differential evidence remain open. Shipping remains 0/200
commands and 0/246 profiles.

## 2026-09-12 - MorphOS Relabel native-entry requalification

Re-ran `tools/Commands/qualify_relabel_native.ps1` against the current
MorphOS Relabel resident entry. The supplied fixture passes eight parser,
DOS-list and mutation invocations per CPU (24 total) on resident 68000/020/040
HUNKs, including invalid names, missing entries, handler failure, interleaved
calls and cleanup ordering. The eleven reachable methods remain free of
managed allocation sites, runtime helpers/features, external native targets,
exception regions and fatal fault sites; the fixture also reports no leaked
resources or shared-image writes.

Receipt: `artifacts/relabel-morphos-native-20260912-qualified/qualification.json`.
The HUNKs are 2,804/2,848/2,804 bytes with SHA-256
`b5e0042756c36a110dd7a6c3520ee22d42d2b1e0fa79ba2d0226c65ff9cb95bc`,
`a1692cd2b8d54fc6142d96cc2505486102c4963b7ced334f2ae422aa315c139d`, and
`706dc3e09bb92671f8e91766ebb682f1a36a3cedbd24bfda74b60fad78f4ff7b`.
This is bounded public-vector evidence only; real handler mutation, Workbench
grammar, packed/source correspondence, original guest parity, PURE/resident
lifecycle, licensing, package admission and differential gates remain open.

## 2026-09-12 - Workbench 3.1 Relabel syntax-candidate entry

Added `NativeWorkbench31RelabelCommand` and the private resident
`Workbench31RelabelEntry`. The entry opens DOS 36, rejects Workbench startup
and malformed argument boundaries, and shares the bounded public-DOS Relabel
body behind the observed classic `DRIVE/A,NAME/A` boundary.

`tools/Commands/qualify_workbench31_relabel_native_entry.ps1` compiles
resident 68000/020/040 HUNKs and passes eight supplied parser/list/mutation
vectors per CPU (24 total), including invalid names, missing entries, handler
failure, interleaved calls and cleanup ordering. Each HUNK has twelve
reachable methods with no managed allocation sites, runtime features/helpers,
external targets, exception regions, fatal fault sites, fixture leaks or
shared-image writes.

Receipt: `artifacts/relabel-wb31-native-20260912-qualified/qualification.json`.
The HUNKs are 2,872/2,916/2,872 bytes with SHA-256
`8d1910a3b3c5538574f927ad66cc1bd46c1f7a8ccdcae07b0374816a4b09e0ca`,
`feca1ed4e35572437254797e332eb915f0815776f33970a4d275b3568279e908`, and
`8d748794bd68425bb17ab4027c437ffc93b945da74347875d4a7f90bd6a1751e`.
Exact Workbench diagnostics and mutation behavior, original guest parity,
PURE/resident lifecycle, packaging and differential evidence remain open.

## 2026-09-12 - Workbench 3.1 AddBuffers syntax-candidate entry

Added `NativeWorkbench31AddBuffersCommand` and the private resident
`Workbench31AddBuffersEntry`. The entry opens DOS 36, rejects Workbench
startup and malformed argument boundaries, and shares the bounded public-DOS
AddBuffers body behind the observed classic `DRIVE/A,BUFFERS/N` boundary.

`tools/Commands/qualify_workbench31_addbuffers_native_entry.ps1` compiles
resident 68000/020/040 HUNKs and passes six supplied parser/handler/formatting
vectors per CPU (18 total), including changed/query/failure and
repeat/interleaved calls. Each HUNK has eleven reachable methods with no
managed allocation sites, runtime features/helpers, external targets,
exception regions, fatal fault sites, fixture leaks or shared-image writes.

Receipt: `artifacts/addbuffers-wb31-native-20260912-qualified/qualification.json`.
The HUNKs are 2,464 bytes on all CPUs with SHA-256
`67418b8cd91ac34e65fd9546c242e768283c94f1ee0908da78619e4a55985355`,
`70078bef3eb2cce91f6894de083c752e536e7a571a8932bbfa97e339548578e9`, and
`70078bef3eb2cce91f6894de083c752e536e7a571a8932bbfa97e339548578e9`.
Exact Workbench handler behavior, original guest parity, PURE/resident
lifecycle, packaging and differential evidence remain open.

## 2026-09-12 - MorphOS FileNote native-entry requalification

Re-ran `tools/Commands/qualify_filenote_native.ps1` against the current
MorphOS `FileNote` resident entry. The supplied fixture passes ten parser,
matcher, recursive AnchorPath, output and `SetComment` invocations per CPU
(30 total) on resident 68000/020/040 HUNKs, including parser failure, no-match,
comment truncation, quiet output, directory descent/exit, mutation failure,
cleanup and interleaved calls. The entry has fourteen reachable methods and
the static/runtime reports contain no managed runtime features/helpers,
external targets, exception regions, fatal fault sites, leaked invocation
storage or shared-image writes.

Receipt: `artifacts/filenote-morphos-native-20260912-qualified/qualification.json`.
The HUNKs are 3,680/3,728/3,672 bytes with SHA-256
`d7e7b210cdfe526c2e9a773055d5f1e06caa08990a449c10a47308bbdd310ceb`,
`aed0f54151e3ec909a92ae207d2eba7f358873dcbc7aa462de1d311c20f127c7`, and
`d38bb13d407824c866369eff3827323567cd1db1114fc8c53685fd288d1bdced`.
This remains bounded public-vector evidence; exact parser/diagnostic parity,
soft-link and volume/device policy, Workbench behavior, PURE/resident lifecycle,
licensing, package admission and differential gates remain open.

## 2026-09-12 - Workbench 3.1 Filenote syntax-candidate entry

Added `NativeWorkbench31FileNoteCommand` and the separate resident
`Workbench31FileNoteEntry`, which opens DOS 36 and reuses the bounded public-DOS
Filenote body behind a distinct profile boundary. The fixture passes ten
supplied parser, matcher, recursive AnchorPath, output and `SetComment` vectors
per CPU (30 total), including parser failure, no-match, truncation, quiet,
directory descent/exit, mutation failure, cleanup and interleaving.

`tools/Commands/qualify_workbench31_filenote_native.ps1` emits
`artifacts/filenote-wb31-native-20260912-qualified/qualification.json`. The
HUNKs are 3,752/3,800/3,744 bytes with SHA-256
`c5b3f95fc637d7749a97f668a3db87ca0f860522d23e7700ef73f11fe3b268f2`,
`4cb246479873f30943364db4dc819de363aebceb0d8d001d49223ceb376016a7`, and
`7bd403039813b256741d4d7ee3b94c2526f96ec79c6fabe9273bc5b2909e8167`.
The fifteen-method static receipts contain no managed runtime features,
helpers, external targets, exception regions or fatal fault sites, and runtime
checks report no leaks or shared-image writes. This is a Workbench syntax
candidate only; packed correspondence, exact guest behavior, PURE/resident
lifecycle, licensing, packaging and differential gates remain open.

## 2026-09-12 - MorphOS Avail syntax-candidate entry

Added `NativeMorphOSAvailCommand` and the separate DOS 37 resident
`NativeMorphOSAvailEntry`, sharing the bounded public-Exec memory-query body
with the Workbench profile. The supplied fixture passes nine DOS/Exec vectors
per CPU (27 total), covering summary and selector output, precedence, parser
failure, fail-closed `FLUSH`, repeat and interleaved callers.

`tools/Commands/qualify_morphos_avail_native_entry.ps1` emits
`artifacts/avail-morphos-native-20260912-qualified/qualification.json`. The
HUNKs are 2,916/2,912/2,912 bytes with SHA-256
`bb9ca12baacf6d8fa2de88dcd2fe46a04a0f42cc19422d50a71ee37e680f6dbe`,
`21417f9932c6d8b44f9b08fb2ee5217adb63bbb9762f259e90d3031852ba6f66`, and
`21417f9932c6d8b44f9b08fb2ee5217adb63bbb9762f259e90d3031852ba6f66`.
The twelve-method static receipts contain no managed runtime features/helpers,
external targets, exception regions or fatal fault sites; runtime checks report
no leaks or shared-image writes. This is a syntax-candidate native receipt;
exact MorphOS 50.6 behavior, expunge, parity, PURE/resident lifecycle,
licensing, packaging and differential gates remain open.

## 2026-09-12 - Workbench 3.1 Version syntax-candidate entry

Added `NativeWorkbench31VersionCommand` and the separate DOS 36 resident
`Workbench31VersionEntry`. The body keeps the classic eight-slot
`NAME,VERSION/N,REVISION/N,FILE/S,FULL/S,UNIT/N,INTERNAL/S,RES/S` grammar,
reports the public DOS version, and resolves one resident name through
`Exec.FindResident`. FILE, UNIT and INTERNAL provider paths fail closed with
`ERROR_NOT_IMPLEMENTED` until their original filesystem/device/module ABIs are
qualified.

`tools/Commands/qualify_workbench31_version_native_entry.ps1` compiles resident
68000/020/040 HUNKs and runs fifteen supplied parser, system/RES, comparison,
provider-gap, boundary and interleaving vectors per CPU (45 total). The durable
receipt is `artifacts/version-wb31-native-20260912-qualified/qualification.json`;
the HUNKs are 4,136/4,208/4,136 bytes with SHA-256
`0401eb1f0fa7f8332b453d499c87dfcf0ecd5cb4ef40e91346133c27f1372020`,
`5aa9d51cbff8dee7334be1435f8ff3e599fda2f37b22bac3cfc0813c68bd15f9`, and
`ad18b52c1abb03f2e9b0b9325c7ac940333658aaae6d2f64d0418b9d682adc29`.
Static checks report fifteen reachable methods with no managed allocation
sites, runtime features/helpers, external targets, exception regions, fatal
fault sites or shared-image writes. This remains a syntax-candidate native
receipt; exact Workbench positional/output behavior, packed correspondence,
original guest parity, PURE/resident lifecycle, licensing, package admission
and differential evidence remain open.

## 2026-09-12 - MorphOS Which extended syntax candidate

Extended the existing DOS-owned Which lookup owner with the MorphOS six-slot
`FILE/A,NOALIAS/S,ALIAS/S,NORES/S,RES/S,ALL/S` boundary and added the separate
DOS 37 resident `MorphOS320WhichEntry`. Ordinary internal, resident, direct
and `ALL` lookup paths share the classic implementation; `ALIAS` and `NOALIAS`
are parsed and fail closed with `ERROR_NOT_IMPLEMENTED` pending the shipped
provider ABI.

`tools/Commands/qualify_morphos_which_native_entry.ps1` emits
`artifacts/which-morphos-native-20260912-qualified/qualification.json`. The
three resident HUNKs pass seventeen supplied vectors per CPU (48 total), with
16 reachable methods and no managed allocation sites, runtime helpers,
external targets, exception regions, fatal fault sites, leaks or shared-image
writes. Their hashes are
`1c6c65ba2895fcace2d9859d385aefec215489e0e22f82a5a514a932dc336c38`,
`5c49abc2679a17caefe040a4ce12a9c836c446f09919de15d5cd8d5f47c70cb3`, and
`b8c0a1c4ec6d77340e54c7fe894a1f70f6be1e393ffc2d7b8c9399b2f6fc3a39`.
Exact MorphOS grammar/output and alias ordering, original guest parity,
PURE/resident lifecycle, licensing, package admission and differential gates
remain open.

## 2026-09-12 - Workbench Which requalification after profile split

Requalified the classic Workbench Which entry after adding the MorphOS
six-slot boundary. The original four-slot `FILE/A,NORES/S,RES/S,ALL/S` path
still passes twelve supplied vectors per CPU (36 total), with 16 reachable
methods, balanced locks and resident-list protection, and no shared-image
writes. Receipt: `artifacts/which-wb31-native-20260912-qualified/qualification.json`.
The 68000/020/040 HUNKs are 4,340/4,408/4,328 bytes with hashes
`2bcb83887615664803c7de177fd4951a1f6b43496cd3278d5890e4064860b83c`,
`3fc1206dccf6a4e268c764b46d0aa4ad7806f3293818c076073373b442e43d1c`, and
`d580060af1eca01d65d2f25f499e86413d992075c933d54fc2246944ecf43a82`.
This remains bounded classic native evidence; original parser/filesystem
parity, PURE/resident lifecycle, packaging, licensing and differential gates
remain open.

## 2026-09-12 - Workbench 3.1 Status legacy-list candidate

Added `NativeWorkbench31StatusCommand` and the separate DOS 36 resident
`Workbench31StatusEntry`. The candidate preserves the shared
`PROCESS/N,FULL/S,TCB/S,CLI=ALL/S,COM=COMMAND/K` grammar and uses the public
legacy CLI-list path available on Workbench 3.1. Its supplied fixture passes
fourteen parser, snapshot, filter, formatting, missing-process, Ctrl-C,
startup-boundary and interleaved vectors per CPU (42 total).

`tools/Commands/qualify_workbench31_status_native_entry.ps1` emits
`artifacts/status-wb31-native-20260912-qualified/qualification.json`. The
HUNKs are 6,252/6,252/6,156 bytes with SHA-256
`47d8ac39fed78aef7989aa22e74d2c1f3d2237328cee11efef9a953a98926f48`,
`1542858a3c12c05dedcb65639cbe886a39baf25b3ebc341eeb74b88db62604ad`, and
`41cf6d88110b757a4c243a4b8984336334f8531117b71dbcac894a0154523c7b`.
Static checks report 22 reachable methods per CPU with no managed allocation
sites, runtime features/helpers, external targets, exception regions, fatal
fault sites or shared-image writes. Exact classic output and task population,
original guest parity, PURE/resident lifecycle, licensing, package admission
and differential evidence remain open.

## 2026-09-12 - MorphOS PathPart native-entry requalification

Requalified the existing `MorphOSPathPartEntry` resident entry on 68000/020/040.
The supplied fixture passes eleven DOS path-helper, failure, repeat and
interleaved vectors per CPU (33 total), with fourteen reachable methods and no
managed allocation sites, runtime helpers/features, external targets, exception
regions, fatal machine-fault sites, leaks or shared-image writes. Receipt:
`artifacts/pathpart-native-entry-20260912-qualified/qualification.json`. The
HUNK hashes are
`ad92c91ebce428817c9671fb9701a82843b8160cc3e653fc86ff2fc55611d4dd`,
`e8ba70d5999037496675320f4780c08bf491031c948695542140aa2c02ca9ad8`, and
`f4e0d3836d9edc4ef8d22e761d21c2072aaeabceb6652c057d5b1f809960f52e`.
This is bounded native evidence; exact command grammar/output, reference
parity, installed P policy, lifecycle, packaging and differential gates remain
open.

## 2026-09-12 - MorphOS Quote native-entry requalification

Requalified the existing MorphOS `Quote` STR forward resident entry on
68000/020/040. The supplied fixture passes twelve parser, forward-rule,
unsupported-mode, short-write, allocation-failure, repeat and interleaved
vectors per CPU (36 total), with 41 reachable methods and no managed allocation
sites, runtime helpers/features, external targets, exception regions, fatal
machine-fault sites, leaks or shared-image writes. Receipt:
`artifacts/quote-native-entry-20260912-qualified/qualification.json`. The HUNK
hashes are
`b580c4faca4cd58a2d05d3c86db4da2db81b40ae1f652c429304b2e76847c842`,
`2927f2319462d2460d78be23ff630aac298da4b5712282084e647a7798951dac`, and
`00f4c5d500ba7653b444e94da092b27b32c227805d82e1435378943a947622b4`.
The receipt still covers only bounded STR forward behavior; full FILE/VAR/
REVERSE semantics, reference parity, lifecycle, packaging and differential
gates remain open.

## 2026-09-12 - MorphOS Delete frontend requalification

Requalified the MorphOS `Delete` resident frontend on 68000/020/040. The
supplied fixture passes eight parser, matcher, lock, protection, deletion,
partial-failure and interleaved vectors per CPU (24 total), with 41 reachable
methods and no managed allocation sites, runtime helpers/features, external
targets, exception regions, fatal machine-fault sites, leaks or shared-image
writes. Receipt: `artifacts/delete-command-native-20260912-qualified/qualification.json`.
The HUNK hashes are
`9d1d2841bd3747623006fbd97b56301b5b6cf95885cb1712dd929217adbde243`,
`72f276fdbecc7e7f64cc4d2fb03f337d0736fc1637398c3a872e18060dffc6dd`, and
`ee07c5423946ad4f19644ba62ad5a6864b862fa576f51a8b0646196783174e04`.
Parent-protection retry, exact diagnostics, recursive/link behavior, real
handler behavior, Workbench parity, PURE/resident lifecycle, packaging and
differential gates remain open.

## 2026-09-12 - Workbench 3.1 Delete syntax-candidate entry

Added the separate DOS 36 `Workbench31DeleteEntry` and
`NativeWorkbench31DeleteCommand` around the classic four-slot
`FILE/M/A,ALL/S,QUIET/S,FORCE/S` boundary. It shares the public-DOS matcher,
lock, protection and `DeleteFile` worker with the MorphOS frontend while
keeping the MorphOS-only `FOLLOWLINKS` result slot out of the Workbench path.
The resident qualification passes eight supplied-DOS parser, deletion,
failure, startup-boundary and interleaved vectors per CPU (24 total), with 43
reachable methods and no managed allocations, runtime helpers/features,
external targets, exception regions, fatal machine-fault sites, leaks or
shared-image writes. Receipt:
`artifacts/delete-wb31-native-20260912-qualified/qualification.json`. HUNK
hashes are
`c181f2dbcf25bf87d4c4060d6e35366d293451113b04f51f571901879b585004`,
`33e3df055a1a2d876b37787dc8fd87cfeaf60cd0723389456db5592c570e413e`, and
`c0e0305b84767f07bf6e96f7fb92645e05a576993cdc031bac24825b09703151`.
Exact Workbench behavior, diagnostics, recursion/link policy, original parity,
PURE/resident lifecycle, packaging and differential gates remain open.

## 2026-09-12 - Workbench 3.1 Join syntax-candidate entry

Added `NativeWorkbench31JoinCommand` and the separate DOS 36 resident
`Workbench31JoinEntry`. The observed classic `FILE/M/A,AS=TO/K/A` boundary is
shared with the MorphOS public-DOS body, while startup and profile admission
remain separate. Static HUNK compilation passes on 68000/020/040 with 19
reachable methods and no managed allocations, runtime helpers/features,
external targets, exception regions or fatal machine-fault sites. Receipt:
`artifacts/join-wb31-native-20260912-qualified/qualification.json`; the HUNK
sizes are 4,276/4,356/4,268 bytes. No runtime fixture is claimed. Exact
Workbench parser/diagnostic behavior, handler effects, lifecycle,
PURE/resident admission, packaging and differential parity remain open.

## 2026-09-12 - Workbench 3.1 Type syntax-candidate entry

Added `NativeWorkbench31TypeCommand` and the separate DOS 36 resident
`Workbench31TypeEntry`. The classic five-slot
`FROM/A/M,TO/K,OPT/K,HEX/S,NUMBER/S` boundary omits MorphOS `NOLINE` while
sharing the public-DOS text/HEX worker. The supplied fixture passes thirteen
parser, wildcard, text/HEX, diagnostic, Ctrl-C and interleaved vectors per CPU
(39 total), with 26 reachable methods and no managed allocations, runtime
helpers/features, external targets, exception regions, fatal machine-fault
sites, leaks or shared-image writes. Receipt:
`artifacts/type-wb31-native-20260912-qualified/qualification.json`; HUNK hashes
are `246357d25ebc228a08729703e9b37d28dbc93bb71310233006fbcc3106121aca`,
`e628157845acaeb54ded9bf49e753699a83362ec5384590d2bacf11f0c8df3c9`, and
`34e45a1f48c86f878fcf99c911b47dd99e4b089d5f44f33d08f003533ebdfeb0`.
Exact Workbench output/diagnostics, original parity, PURE/resident lifecycle,
packaging and differential gates remain open.
## 2026-09-17 - Workbench 3.1 Info DEVICE candidate

Extracted the original Workbench 3.1 Disk 2 `C/Info` member as a read-only
external observation: v38.2 (11.3.92), 1980 bytes, SHA-256
`873e4f7030f8c5a6dfed3e048c7888f6d9de394af795fa6a6c2322f2f1f605bf`. The
binary exposes the classic `DEVICE` template and the mounted-disk/volume
headers, status strings and row format. The missing Workbench disks 3-6 and
installed overlay remain unavailable, so no broader membership or parity is
inferred.

Added `NativeWorkbench31InfoCommand` and its private DOS36 resident entry.
The candidate uses ReadArgs, snapshots public DOS device/volume names while
holding the combined read lock, releases that lock before filesystem `Info`
queries, and cleans invocation-owned workspace, result and InfoData buffers.
It fails closed for unsupported startup/argument boundaries and does not use
host drive metadata.

The supplied fixture covers default list, explicit `DEVICE`, no-disk,
unreadable, allocation, parser, Workbench startup, malformed entry and
interleaved repeat paths. Three-CPU resident HUNK qualification passes 11
invocations per CPU with zero runtime features/helpers, zero exception/fatal
sites, balanced DOS/Exec ownership and zero shared image writes. Receipt:
`artifacts/info-wb31-native-20260917-qualified-v8/qualification.json`.

This is a native candidate checkpoint only. Exact original output and
IoErr/status precedence, all-device behavior, installed metadata, PURE and
same-SegList lifecycle, packaging, full guest comparison and final shipping
remain open. Shipping remains 0/200 commands and 0/246 profiles.

## 2026-09-17 - Workbench 3.1 Protect resident candidate receipt

Re-ran the existing Workbench 3.1 Protect syntax candidate against the
current resident compiler and fixture. The DOS36 entry keeps the observed
`FILE/A,FLAGS,ADD/S,SUB/S,ALL/S,QUIET/S` boundary and the public-DOS
protection body. Fourteen supplied parser, bit-mapping, mutation, recursive,
failure, break and interleaved vectors pass on each of 68000/020/040, with no
runtime features/helpers, exception/fatal sites, leaks or shared-image writes.
Receipt: `artifacts/protect-wb31-native-20260917-qualified-v1/qualification.json`.

This remains adapter evidence. Exact Workbench guest behavior, complete
filesystem/diagnostic parity, original PURE classification and resident
reuse, packaging and differential comparison remain open.

## 2026-09-17 - Workbench 3.1 FileNote resident candidate receipt

Re-ran the Workbench 3.1 FileNote syntax candidate with the current compiler
and supplied DOS fixture. The DOS36 entry preserves the observed
`FILE/A,COMMENT,ALL/S,QUIET/S` boundary and the public AnchorPath/SetComment
body. Ten supplied parser, no-match, truncation, quiet, recursive, failure,
break and interleaved vectors pass on each of 68000/020/040. Static reports
show zero runtime features/helpers, external targets, exception/fatal sites;
the runtime has no leaks or shared-image writes. Receipt:
`artifacts/filenote-wb31-native-20260917-qualified-v1/qualification.json`.

This is still syntax-candidate evidence. Packed correspondence, exact
Workbench output and diagnostics, PURE/resident admission, production
lifecycle, packaging and differential parity remain open.

## 2026-09-17 - MorphOS 3.20 Dir resident candidate receipt

Bound the MorphOS 3.20 release-source `c/dir/dir.c` grammar and added a
public-DOS candidate with the exact six-slot `DIR,OPT/K,ALL/S,DIRS/S,FILES/S,INTER/S`
template. The body uses invocation-owned `AllocVec` workspace and
`ExAllControl`, `Lock`/`ExAll`, DOS output vectors, and fail-closed handling for
the source's unfinished recursive `ALL` and interactive `INTER` paths.

The supplied fixture covers default, directory-only, file-only, `OPT D`,
ignored-option, empty, unsupported, parser, allocation, startup and
interleaved calls. Three-CPU resident HUNK qualification passes fifteen
invocations per CPU with balanced parser, lock, ExAll and buffer ownership,
zero shared-image writes, and no runtime features/helpers, exception regions
or fatal sites. Receipt:
`artifacts/dir-morphos-native-20260917-qualified-v2/qualification.json`.

This is source/adapter evidence only. Packed correspondence, original guest
listing and diagnostics, wildcard `MatchFirst`, recursive and interactive
behavior, soft-link handling, PURE/resident admission, packaging and
differential parity remain open.

The same bounded worker is now exposed through a separate DOS 36 Workbench
3.1 `Dir` syntax-candidate entry. Its three-CPU resident receipt passes the
same fifteen supplied vectors per CPU with balanced resources and unchanged
shared image; exact Workbench output, parser behavior, package identity,
PURE/resident admission and differential comparison remain open. Receipt:
`artifacts/dir-wb31-native-20260917-qualified-v1/qualification.json`.

## 2026-09-18 - Join frontend runtime receipts

Added supplied public-DOS runtime fixtures for both Join frontends. The
Workbench DOS 36 entry and MorphOS DOS 37 entry each pass eighteen cases per
CPU on 68000/020/040 from one resident image, including ordered source streams,
exact EOF/short-write/read-failure behavior, destination cleanup, open,
parser/workspace-result-buffer allocation and Ctrl-C failures, empty input,
startup/missing-DOS boundaries, malformed argument buffers and
instruction-interleaved callers.
The checks assert `ReadArgs`, matcher, file-handle, buffer, signal, `IoErr` and
free/close/delete ownership; all six HUNKs have zero managed runtime
features/helpers, external targets, exception/fatal sites, leaks or shared-image
writes. Receipts:
`artifacts/join-wb31-native-20260918-runtime-v3/qualification.json` and
`artifacts/join-morphos320-native-20260918-runtime-v2/qualification.json`.

These are bounded native ABI and ownership checkpoints. Exact classic parser
and diagnostics, wildcard/no-match and real-handler behavior, packed
correspondence, original PURE/resident classification and reuse, packaging,
and Workbench/MorphOS differential evidence remain open; shipping remains
0/200 commands and 0/246 profiles.

## 2026-09-18 - MakeLink native qualification rerun

Re-ran both MakeLink frontends against the current resident compiler and
fixtures. The MorphOS 3.20 entry passes eighteen supplied DOS parser, link,
lock, FIB, failure and interleaving vectors on each of 68000/020/040. Static
reports show nine reachable methods with no runtime features/helpers,
external targets, exception/fatal sites or shared-image writes. Receipt:
`artifacts/makelink-morphos-native-20260918-runtime-v2/qualification.json`.

The Workbench 3.1 DOS 36 entry passes twenty supplied vectors per CPU, sixty
invocations total, including parent traversal, loop detection, diagnostics,
startup and interleaving cases, with balanced ReadArgs, DOS-object, lock and
memory ownership and no shared-image writes. Receipt:
`artifacts/makelink-wb31-native-20260918-runtime-v1/qualification.json`.

These remain bounded adapter/runtime receipts. Exact Workbench 3.1 and
MorphOS guest parser/output/diagnostic correspondence, original PURE and
resident admission, minimum-stack proof, packaged command identity, and
differential qualification remain open; shipping remains 0/200 commands and
0/246 profiles.

## 2026-09-18 - RequestFile resident ABI blocker retained

The MorphOS 3.20 RequestFile candidate still compiles statically for all three
CPUs, but its resident execution fixture fails at the cleanup call: `FreeArgs`
receives the invocation buffer pointer in D1 instead of the `RDArgs` pointer
returned by `ReadArgs`. The failure reproduces on 68000, 68020 and 68040 after
isolating the requester tag builder and cleanup helper, so it is retained as a
CopperSharp resident argument-lowering blocker rather than weakened in the
fixture or promoted to a receipt. Diagnostic artifacts remain under
`artifacts/requestfile-morphos-native-20260918-runtime-v1/`.

The cleaned candidate was rerun after removing diagnostic-only fixture code;
the 68000 receipt at
`artifacts/requestfile-morphos-native-20260918-runtime-v21/` still fails at
the same FreeArgs ABI assertion.

RequestFile therefore has no shipping or PURE/resident admission evidence. The
next implementation work can proceed on independent command families while
the compiler ABI issue is isolated for a focused fix.

## 2026-09-18 - Wait native qualification rerun

Re-ran the Workbench 3.1 and MorphOS 3.20 Wait entries with the current
resident compiler. Each frontend passes fifteen supplied DOS/Exec/timer/signal
vectors on 68000/020/040, covering ReadArgs boundaries, seconds/minutes and
`HH:MM`/`UNTIL` conversion, VBlank scheduling, Ctrl-C, diagnostics, startup,
repeat use and interleaved callers. All six HUNKs have zero managed runtime
features/helpers, external targets, exception/fatal sites, leaks or shared
image writes. Receipts:
`artifacts/wait-wb31-native-20260918-runtime-v1/qualification.json` and
`artifacts/wait-morphos320-native-20260918-runtime-v1/qualification.json`.

This remains supplied-vector adapter evidence. Exact Workbench/MorphOS guest
timing and diagnostics, original PURE classification, resident lifecycle,
packaging and differential parity remain open; shipping remains 0/200 commands
and 0/246 profiles.

## 2026-09-18 - RequestFile cleanup lowering isolated again

Recompiled the RequestFile resident candidate through the proven
`NativeCommandArguments` parser lease and reran the 68000 fixture with a
heap-backed copy of the RDArgs value. The guest result slot contains the exact
pointer returned by `ReadArgs`, but the generated `FreeArgs` gateway still
receives the 512-byte requester buffer in D1. The same mismatch is therefore
independent of result-array ownership, helper boundaries, output parameters,
and stack scratch placement. The clean candidate remains unqualified; a
CopperSharp external-register lowering fix plus a three-CPU rerun is required.

## 2026-09-18 - RequestFile ref-owner lifetime experiment

Moved the parser lease release temporarily into the `RunParsed` method while
its `ref NativeCommandArguments` parameter was still live, then rebuilt and
reran the clean 68000 fixture. `FreeArgs` still received the 512-byte requester
buffer in D1. The failure is therefore not caused solely by releasing the
struct after the helper returns; the production source was restored to its
original cleanup order and no diagnostic-only code was retained.

## 2026-09-18 - RequestFile resident qualification unblocked

This entry supersedes the earlier retained-ABI-blocker notes for the bounded
fixture; those notes remain as the historical diagnostic trail.

The retained `FreeArgs` failure was traced to the runtime fixture rather than
the resident argument lowering: the RequestFile fixture had no `DOS.AddPart`
gateway, even though the command's normal single- and multi-select paths call
it. The unregistered vector clobbered the later cleanup observation. The
fixture now supplies `AddPart` with the 512-byte capacity and expected path
checks, and its parser-error case correctly expects result-array cleanup
without requester-buffer allocation.

The clean MorphOS 3.20 resident RequestFile entry now passes seven supplied
invocations on each of 68000, 68020 and 68040, including single-select,
multi-select, cancel, parser failure, ASL allocation failure, Workbench
startup rejection and repeat use. All three HUNK compatibility reports have
zero managed runtime features/helpers, external targets, exception/fatal
sites and shared-image writes; the fixture reports no leaks. Receipt:
`artifacts/requestfile-morphos-native-20260918-runtime-v36/`.

This closes the bounded resident runtime blocker only. Exact MorphOS guest
requester behavior, Workbench correspondence, original PURE/resident
admission, packaging, minimum-stack proof and differential comparison remain
open; shipping remains 0/200 commands and 0/246 profiles.

## 2026-09-18 - PathPart native qualification rerun

Re-ran the MorphOS PathPart resident entry against the current compiler and
execution fixture. The 68000, 68020 and 68040 HUNKs each pass eleven supplied
DOS/path-helper invocations, including parser/result ownership, directory and
file handling, ADD path construction, output failure, allocation/parser
failure, startup boundaries and interleaved callers. Static reports contain
zero managed runtime features/helpers, external targets, exception regions or
fatal machine-fault sites; the fixture reports no leaks or shared-image
writes. Receipt:
`artifacts/pathpart-morphos-native-20260918-runtime-v1/qualification.json`.

This advances only the bounded native gate. Exact MorphOS template and guest
path/output behavior, original PURE/resident lifecycle, packaging and
differential parity remain open.

## 2026-09-18 - Quote native qualification rerun

Re-ran the MorphOS Quote resident STR-forward entry with the current compiler
and fixture. Each of 68000, 68020 and 68040 passes twelve supplied DOS/Exec
vectors, including parser and source handling, NOLINE output, short-write
cleanup, failure paths, startup boundaries and interleaved calls. Static
reports contain zero managed runtime features/helpers, external targets,
exception regions or fatal machine-fault sites; the fixture reports no leaks
or shared-image writes. Receipt:
`artifacts/quote-morphos-native-20260918-runtime-v1/qualification.json`.

This advances only the bounded native gate. The full Quote grammar and guest
behavior, original PURE/resident lifecycle, packaging and differential parity
remain open.

## 2026-09-18 - Eval native qualification rerun

Re-ran both Eval resident entries with the current compiler. The MorphOS
entry passes fourteen supplied vectors per CPU (42 total), and the Workbench
3.1 entry passes twelve per CPU (36 total), covering their current bounded
numeric parser/formatter slices, failure handling, startup boundaries and
interleaved callers. The generated HUNK fixtures report no leaks or
shared-image writes and retain only the documented bounded Eval runtime
feature. Receipts:
`artifacts/eval-morphos-native-20260918-runtime-v1/qualification.json` and
`artifacts/eval-wb31-native-20260918-runtime-v1/qualification.json`.

These receipts do not close the complete Eval grammar, overflow and character
formats, original binary parity, PURE/resident lifecycle, packaging or
differential gates.

## 2026-09-18 - Which MorphOS native qualification rerun

Re-ran the MorphOS Which resident entry against the current compiler and
execution fixture. Each of 68000, 68020 and 68040 passes sixteen supplied
DOS/Exec vectors covering the bounded extended parser, ordinary resident/path
lookup, cleanup and interleaved calls. Static compatibility reports contain
zero managed runtime features/helpers, external targets, exception regions or
fatal machine-fault sites; the fixture reports no leaks or shared-image writes.
Receipt:
`artifacts/which-morphos-native-20260918-runtime-v1/qualification.json`.

This advances only the bounded native gate. ALIAS/NOALIAS provider semantics,
the exact shipped grammar and output, original PURE/resident lifecycle,
packaging and differential parity remain open.

## 2026-09-19 - MorphOS Date native qualification rerun

Re-ran the MorphOS 3.20 Date resident entry against the current compiler and
execution fixture. Each of 68000, 68020 and 68040 passes thirteen supplied
DOS/Exec/timer/locale vectors (39 total), including normal and TO/LFORMAT
output, date and short-time setters, parser failure, timer-open and I/O
failures, locale-open behavior, write failure, cleanup and interleaving. The
three HUNK fixtures report no leaks or shared-image writes and the static
reports contain no managed reachable members, runtime helpers, external
targets, exception regions or fatal machine-fault sites. Receipt:
`artifacts/morphos-date-native-20260919-runtime-v1/qualification.json`.

This advances only the bounded native gate. Original guest output and IoErr,
PURE/resident same-segment lifecycle, package placement and differential
parity remain open.

## 2026-09-19 - Workbench Date native qualification rerun

Re-ran the Workbench 3.1 Date resident entry against the current compiler and
execution fixture. Each of 68000, 68020 and 68040 passes seven supplied
DOS/Exec/timer/output vectors (21 total), covering current output, TO output,
setter parsing, parser failure, timer failure, cleanup and interleaved calls.
The three HUNK fixtures report no leaks or shared-image writes and the static
reports contain no managed reachable members, runtime helpers, external
targets, exception regions or fatal machine-fault sites. Receipt:
`artifacts/date-native-20260919-runtime-v1/qualification.json`.

This advances only the bounded native gate. Original guest precedence,
diagnostics and IoErr, PURE/resident same-segment lifecycle, package placement
and differential parity remain open.

## 2026-09-19 - Info native qualification rerun

Re-ran both Info resident entries against the current compiler and execution
fixture. The MorphOS 3.20 entry passes seventeen supplied DOS-list/Info vectors
per CPU (51 total), covering device and volume selection, filters, GOODONLY,
wildcards, BLOCKS, valid VERBOSE startup data, parser/allocation, Ctrl-C,
startup and interleaving. The Workbench 3.1 DEVICE candidate passes eleven
vectors per CPU (33 total), covering its bounded public-list and failure paths.
All six HUNK fixtures report no leaks or shared-image writes, and the static
reports contain no managed reachable members, runtime helpers, external
targets, exception regions or fatal machine-fault sites. Receipts:
`artifacts/info-morphos-native-20260919-runtime-v1/qualification.json` and
`artifacts/info-wb31-native-20260919-runtime-v1/qualification.json`.

The Workbench runner required an explicit CopperSharp root because its default
`$PSScriptRoot` parameter resolved empty in this invocation; no command code or
fixture was changed. Exact classic output/disk geometry, alternate MorphOS
providers and locale formatting, original guest parity, PURE/resident
lifecycle, packaging and differential gates remain open.

## 2026-09-19 - MorphOS DevList native qualification rerun

Re-ran the MorphOS 3.20 DevList resident entry against the current compiler
and execution fixture. Each of 68000, 68020 and 68040 passes nine supplied
DOS/Exec vectors (27 total), covering empty and populated device snapshots,
formatting, allocation and Ctrl-C paths, Workbench/startup rejection and
interleaved repeat calls. The HUNK fixtures report no leaks or shared-image
writes; static reports contain no managed reachable members, runtime helpers,
external targets, exception regions or fatal machine-fault sites. Receipt:
`artifacts/devlist-morphos-native-20260919-runtime-v1/qualification.json`.

This advances only the bounded native gate. Complete device population,
packed-binary/source correspondence, original guest parity, PURE/resident
lifecycle, package placement and differential comparison remain open.

## 2026-09-19 - MorphOS ModList native qualification rerun

Re-ran the MorphOS 3.20 ModList resident entry against the current compiler
and execution fixture. Each of 68000, 68020 and 68040 passes twelve supplied
DOS/Exec vectors (36 total), covering the VERBOSE parser, resident-table
snapshot, output, allocation and Ctrl-C paths, startup rejection and
interleaved repeat calls. The HUNK fixtures report no leaks or shared-image
writes; static reports contain no managed reachable members, runtime helpers,
external targets, exception regions or fatal machine-fault sites. Receipt:
`artifacts/modlist-morphos-native-20260919-runtime-v1/qualification.json`.

This advances only the bounded native gate. Complete resident population,
packed/source correspondence, original guest parity, PURE/resident lifecycle,
package placement and differential comparison remain open.

## 2026-09-19 - MorphOS PortList and ResList native qualification rerun

Re-ran the MorphOS 3.20 PortList and ResList resident entries against the
current compiler and execution fixture. PortList passes ten supplied
DOS/Exec vectors per CPU (30 total); ResList passes nine per CPU (27 total),
covering their source-backed message-port/resource snapshots, output,
allocation, Ctrl-C, startup rejection and interleaved repeat paths. All six
HUNK fixtures report no leaks or shared-image writes; static reports contain
no managed reachable members, runtime helpers, external targets, exception
regions or fatal machine-fault sites. Receipts:
`artifacts/portlist-morphos-native-20260919-runtime-v1/qualification.json`
and `artifacts/reslist-morphos-native-20260919-runtime-v1/qualification.json`.

These receipts advance only the bounded native gates. Complete population,
packed/source correspondence, original guest parity, PURE/resident lifecycle,
package placement and differential comparison remain open.

## 2026-09-19 - MorphOS Mount contract slice

Added `contracts/Mount.md` from the hash-bound MorphOS 3.20 packed identity and
the extracted `c/mount/mount.c` source. The contract records the exact
source-observed outer `DEVICE/M,FROM/K,DEBUG/S` grammar, the inner mount-file
keyword aliases, device-driver and mount-list search order, Workbench startup
handling, `ReadArgs`/`FreeArgs` and `AllocVec` ownership, `DosEnvec` and
`DeviceNode` construction, handler loading, `AddDosNode`, validation ranges,
and named failure categories. It explicitly separates source evidence from
packed-binary correspondence and licensing.

The two ledger profiles now reflect partial Spec/Options evidence for MorphOS;
the Workbench profile remains open because only its syntax candidate is
available. No native Mount body was added: Expansion/filesystem provider
ownership, exact packed behavior, guest lifecycle, PURE, packaging and
differential gates must be closed before implementation can be admitted.

## 2026-09-19 - MorphOS Format contract slice

Added `contracts/Format.md` from the hash-bound MorphOS 3.20 `Format 50.9`
identity and the extracted 24,895-byte AROS-derived source. The contract
records the exact source `ReadArgs` template, filesystem-selector precedence,
banned system names, device-list and `FileSysStartupMsg` lookup, interactive
confirmation, handler inhibition, trackdisk and TD64 formatting/verification,
SFS tags, icon creation, Ctrl-C handling, cleanup and named failure policy.
It also preserves the source's explicit TODOs for write-protect detection,
retry policy and manual FFS initialization.

`CC21.Format.morphos320` now has partial Spec/Options evidence. No native body
was added because Format is destructive and requires a real trackdisk/device
provider plus disposable guest media; packed correspondence, guest behavior,
PURE, packaging and differential gates remain open.

## 2026-09-19 - Release preflight integrity rebind

The goal plan was updated with the Date/Info/registry receipt status and its
current SHA-256 was rebound into `command-inventory.json`, `media-evidence.json`
and the completion ledger. The live inventory hash is
`b3f38c2e8863716e3a6f2894ae018f1eb6fa9d58639fee35e6affd5edf3900eb`.
Running the release preflight with that hash now reaches the intended
`artifact-selection` rejection for Workbench Date: the manifest contains zero
qualified shipping artifacts and the verifier does not fall back to the
qualification-only HUNK. This closes the stale-hash ambiguity while retaining
the real packaging blocker.

The read-only inventory verifier also passes against the rebound plan hash. It
reports 200 required external identities, 70 Workbench `C:` entries, 188
MorphOS entries, 46 shared identities, and 88 MorphOS installer `+P`
additions. The remaining closure IDs are `WB31-DISKS-3-6`,
`WB31-MACHINE-VARIANTS`, `WB31-INSTALLED-METADATA`,
`MORPHOS320-INSTALLED-OVERLAY`, `COMMAND-RUNTIME-CONTRACTS`, and
`FREEZE-INDEX-MEDIA`; verification therefore remains intentionally partial.

## 2026-09-19 - Mount and Format provider inventory

Audited the concrete provider boundary instead of treating the two remaining
source-backed commands as host-filesystem work. The pinned CopperSharp SDK
already exposes the public Expansion LVOs and `DosEnvec`/`FileSysStartupMsg`/
`DeviceNode` layouts required by Mount. The SDK also exposes classic and TD64
trackdisk commands and `IOExtTD`; CopperStart has a guest-owned
`TrackDiskDeviceCore` with format/readback and protection operations. Their
hash-bound identities and ownership requirements are recorded in
`contracts/Mount.md` and `contracts/Format.md`.

This narrows the blocker: CopperOS still lacks a production guest publication
path that binds Expansion `MakeDosNode`/`AddDosNode`, or a trackdisk media queue,
to the resident command fixtures and CopperStart integration. No Mount or
Format stub was added, and no host filesystem shortcut is permitted.

## 2026-09-19 - Expansion DOS-node vectors fail closed

Added the first executable provider-boundary repair in the sibling CopperStart
and CopperMod sources. `ExpansionCore` now routes the public
`MakeDosNode`/`AddDosNode` LVOs through dedicated platform methods instead of
the compatibility fallback. The current host adapter returns the classic
failure value, so a future Mount body cannot mistake a synthetic host object
for a published DOS node. The CopperStart test suite passes the compatibility
and DOS-node routing tests, including the existing MC68000 Expansion parity
test.

This does not implement DOS-node publication: allocation, `DosEnvec`/
`FileSysStartupMsg` ownership, `AddDosNode` list mutation, rollback and handler
startup remain the next provider work package. Mount and Format therefore stay
out of shipping admission.

## 2026-09-19 - Managed Expansion DOS-node provider

`CopperStart.Dos.DosExpansionNodeCore` now implements the classic five-long
`MakeDosNode` packet. It validates bounded C strings and the 80-byte `DosEnvec`,
allocates a DOS-owned `FileSystemContext` through `DosObjectCore`, copies the
name and startup-device strings into resident BSTR storage, preserves
unit/startup flags/environment, and initializes the Kickstart fallback defaults
(device type, priority 10, stack 4000). `AddDosNode` validates the device node,
applies the supplied boot priority, and publishes through `DosListCore`.
CopperMod wires both methods through the Expansion host context; malformed
input remains fail-closed.

The focused `DosObjectCoreTests.ExpansionMakeDosNodeConsumesClassicPacketAndPublishesDeviceNode`
regression passes. This closes the managed DOS-list half of the provider seam.
It does not qualify native DOS image publication: the native list lock/call must
be handled outside the current Expansion gateway to avoid re-entrant bootstrap
execution, and duplicate/conflict/retirement behavior still needs native
differential evidence.

## 2026-09-19 - Native DOS publication boundary

The CopperMod boot boundary now selects the provider for Expansion
`AddDosNode`: managed sessions publish through `DosListCore`; a separately
installed native DOS image receives the public native `AddDosEntry` vector via
the retained bootstrap call, with the DOS-owned `FileSystemContext` freed if
publication fails or the call faults. The node builder remains shared, so the
guest sees the same Kickstart `DeviceNode`/`FileSysStartupMsg` layout in either
path. The call is guarded by the existing native bootstrap re-entry/fault
state and returns failure rather than fabricating success when that boundary is
unsafe. The emulator still builds cleanly; native duplicate/conflict, late
retirement, and original Mount guest evidence remain open.

## 2026-09-19 - MorphOS Format native trackdisk slice

Added `NativeMorphOSFormatCommand` and a resident `Format` entry using the
source template and Kickstart 3.1 `ReadArgs`/DOS/Exec/trackdisk surfaces. The
bounded body resolves the device startup message and `DosEnvec`, preserves
selector precedence and banned names, handles interactive confirmation and
Ctrl-C, inhibits/uninhibits the handler, owns the message port and I/O request,
performs classic `TD_FORMAT` plus `CMD_READ` verification, and retains source
result/`IoErr` cleanup. Quick and SFS packet paths plus source-style icon
creation are represented, and the first-block write now uses the public
`ID_UNREADABLE_DISK` (`'BAD\0'`) value; icon-provider parity remains open.

`TrackDiskDeviceCoreTests` now cover a complete-track format/readback, TD64
high/low-offset format/readback through a sparse extended-media boundary, and
the fail-closed path when that provider is unavailable (7/7 focused tests
pass). The
resident root builds cleanly and the development HUNK compiles for MC68000,
MC68020 and MC68040 with compatible static reports (30 reachable methods, one
runtime feature, no helpers/external targets/exception regions, ten machine
fault sites per CPU) under
`artifacts/format-morphos-native-dev/`. These receipts do not qualify the
command: a disposable guest trackdisk queue, handler validation, packed binary
correspondence, an installed TD64-capable media callback above 4 GiB,
icon-provider behavior, PURE/package admission and
original/replacement differential evidence remain open.

## 2026-09-19 - Installed trackdisk service format/readback coverage

Extended `CopperMod.Amiga.Tests.TrackdiskDeviceServicesTests` with an installed
`trackdisk.device` vector exercise. A disposable standard ADF-sized media
buffer is formatted through the guest `TD_FORMAT` gateway, read back through
`CMD_READ`, and checked for the complete track pattern and dirty-media write
callback. The adapter now also accepts an optional 64-bit media callback; a
sparse high-offset `TD_FORMAT64`/`TD_READ64` pair exercises that path. The
focused service suite passes 6/6. This closes the device-vector provider
receipt, but it is not yet a resident `Format` command fixture: DOS handler
publication, inhibit/validation packets, and original/replacement guest
differential evidence remain open.

## 2026-09-19 - Trackdisk TD64 media boundary

Extended the portable `TrackDiskDeviceCore` provider contract with explicit
high/low-offset transfer and seek callbacks. The native core now delegates
`TD_FORMAT64`/`TD_READ64`/`TD_WRITE64` without rejecting a nonzero high word,
while providers that cannot represent the address still fail closed. The
CopperStart memory fixture uses sparse guest-owned extended bytes, so a
high-word format/readback is exercised without allocating a multi-gigabyte
array; the focused suite passes 7/7. The installed AmigaBus adapter now has
the same optional high-offset callback and its focused service suite passes
6/6, but the resident Format command still lacks a bound DOS handler/media
queue, so this remains a provider seam repair rather than Format command
qualification.
## 2026-09-19 - Mount provider duplicate transaction

The managed CopperStart `AddDosNode` boundary now publishes the DOS-list entry
before applying the requested boot priority. Duplicate/conflict rejection
therefore leaves the caller-owned `DeviceNode` unchanged and releasable; it no
longer mutates a failed node before `DosListCore` returns
`ERROR_OBJECT_EXISTS`. The focused `DosObjectCoreTests` suite passes 19/19,
including a duplicate publication case that verifies the priority remains at
its `MakeDosNode` default and that the failed node is freed. This is provider
evidence for Mount, not a Mount command implementation or shipping admission.
## 2026-09-19 - DOS production parity export closure

The CopperStart native parity fixtures now include the published
`copperstart.dos.process-return` trampoline whenever a bounded image compiles
the DOS production or ROM roots. This matches the `APTR.ExportAddress` symbol
used by the production `PublishDosProcessTask` path and removes a harness-only
compilation failure. The focused child-launch and direct DOS gateway parity
tests pass, and `DosInstalledVectorCoverageTests` passes both load-address
cases. This only unblocks native fixture execution; it does not establish
original guest parity or command shipping admission.
## 2026-09-19 - Native handler-load fixture closure

The CopperStart installed classic semantic fixture now encodes its synthetic
`loadseg` HUNK correctly: `HUNK_CODE` carries the required payload-length
longword before the one-word `RTS` body. After the DOS process-return export
repair, the two `DosInstalledClassicSemanticCoverageTests` load-address cases
pass, including `LoadSeg`/`UnLoadSeg`, packet-backed file I/O, link handling,
and administration vectors. This removes a malformed-test-image failure and
provides a usable handler-load receipt for the Mount provider path; it remains
fixture evidence rather than original-guest parity or command admission.
## 2026-09-19 - InternalLoadSeg fixture closure

The same classic semantic fixture had a second malformed one-word HUNK image
for `InternalLoadSeg`; it now includes the required `HUNK_CODE` payload length.
Both 68000 load-address cases of
`InstalledClassicPureStreamCliPatternSegmentAndListFamilies` pass, covering
internal segment callbacks and unload ownership in addition to the earlier
public `LoadSeg`/`UnLoadSeg` receipt. The change fixes fixture validity and
strengthens the handler-loader evidence used by the future Mount implementation;
it does not count as Mount or as original guest parity.
The complete `DosInstalledClassicSemanticCoverageTests` class now passes 6/6;
the two lightweight ABI/ledger checks pass alongside both two-case resident
semantic matrices.

## 2026-09-19 - MorphOS Mount explicit-source native slice

Added the first CopperOS native `Mount` body and resident entry for the
source-observed explicit `FROM/K` path. The body uses the exact outer
`DEVICE/M,FROM/K,DEBUG/S` and inner mount-record `ReadArgs` templates, writes a
guest-owned `DosEnvec` and five-long `MakeDosNode` packet, opens
`expansion.library` through Exec, copies handler/startup BSTRs and string
device fields into guest-owned storage, applies the source environment
defaults and stack/priority/global-vector/startup fields, publishes with
public `Expansion.MakeDosNode`/`AddDosNode`, and explicitly rolls back the
guest strings and DOS node on publication failure. The three-CPU resident HUNK
checkpoint is `artifacts/mount-morphos-native-20260919-default-env/qualification.json`;
the resident root uses only public Expansion/DOS vectors and raw guest memory,
so each image compiles with zero managed allocations, runtime features,
helpers, external targets, exception regions, or fatal fault sites. This is
pure/resident static evidence; no runtime fixture, device discovery, real
handler/media guest, Workbench body, differential evidence, or package
admission is claimed.
The bounded slice uses the first outer `DEVICE/M` value when supplied and now
derives the mounted node name from an explicit `FROM/K` filename with DOS
`FilePart`, matching the source fallback. `.info` icon fallback, device
discovery, and the full optional device/matcher matrix remain open. The source
fidelity correction also keeps `de_BootBlocks` zero and passes only
`ADNF_STARTPROC` (when `MOUNT/ACTIVATE` is nonzero) to `AddDosNode`.

The refreshed receipt is `artifacts/mount-morphos-native-20260919-optional-device/qualification.json`:
68000/68020/68040 images are 9760/9868/9744 bytes with SHA-256
`3948492aa747a35a7298e8caa2ed8941af79acce91396da77552892ff3da4aac`,
`5efeb2f2cfe9f1ba0a3035f69c83c5274de3bb411939254d971357e57a9391f7`, and
`9bad2dcdcde918a6c06135ddea170d983ab8aef1f35bb56ebacb41cf821c5926`.
The slice copies handler and string `STARTUP` values into guest BSTRs,
retaining them only after successful `AddDosNode` publication; it does not
invent a direct `LoadSeg` call because the inspected source leaves loading to
the DOS handler path. The slice also copies `DEVICE` as a C string, preserves
numeric-or-string `UNIT`/`FLAGS`, and keeps `CONTROL` in a guest BSTR
referenced by the environment; all such allocations are retained only after
successful node publication.
The environment writer now seeds the same source defaults before applying
record values, including byte-to-longword conversion for `SECTORSIZE` and the
default DOS type/baud/transfer/mask fields.

## 2026-09-19 - MorphOS Mount first-device source search

The native MorphOS Mount body now covers the source-observed CLI resolution
path when `FROM/K` is absent and the first `DEVICE/M` value is present. A
non-device argument is probed as a mount file; a trailing-colon device name is
stripped and tried in the exact source order: current directory,
`DEVS:DOSDrivers/`, `MOSSYS:DEVS/DOSDrivers/`, `SYS:Storage/DOSDrivers/`, and
`MOSSYS:Storage/DOSDrivers/`. Each candidate uses DOS `Open`/`Close`, the
successful candidate remains in an invocation-owned 512-byte C buffer, and the
normal parser reopens it before deriving the node name with `FilePart`.
Three-CPU resident qualification passes with zero managed runtime features,
helpers, external targets, exception regions or fatal fault sites at
`artifacts/mount-morphos-native-20260919-device-search/qualification.json`.
The implementation now iterates every direct `DEVICE/M` vector member,
reopening and releasing the resolved source for each operation. `DEVS:MountList`
filtering, wildcard `MatchFirst`/`MatchNext`, `.info` tool types, real
handler/media execution, Workbench parity, lifecycle/PURE, differential and
package gates remain open.

The wildcard receipts contain 14,332/14,544/14,320-byte MorphOS images and
14,276/14,492/14,268-byte Workbench images for 68000/68020/68040. Their
SHA-256 values are recorded directly in the two qualification JSON files;
all six images have 30 reachable methods and no forbidden native compatibility
features.

After this iteration change the resident images are 12,980/13,148/12,944
bytes for 68000/68020/68040 with SHA-256
`d927a3c05b289666b5c4b3c6415df4668f2cceef8f33a675b976413ad8ce6d30`,
`1765ae77e28fa649692942b5181ead9ee88c7e6e27ef7a3deebea30af5fee9b1`, and
`cc0a674e8ab76eca9d18affdf8e592b83be07ac21d51533d59e3aed95edb8eac`.

The inventory extractor now preserves Workbench Mount's comma-leading inner
template as a binary syntax candidate at byte offset 6,396 instead of losing
it to the ordinary slash-bearing template scanner. `inventory.py verify` and
all 14 inventory unit tests pass; the candidate remains explicitly
non-runtime and does not change the open reference-closure status.

The shared provider regression remains green after the Workbench slot-map
change: CopperStart `DosObjectCoreTests` passes 20/20, including the omitted
device BSTR and duplicate-publication rollback cases. This remains provider
semantic evidence rather than a real Mount guest or original-binary result.

## 2026-09-19 - Mount optional-device provider parity

The inspected MorphOS source permits a mount record with no `DEVICE/K` value:
`MyMakeDosNode` creates an empty startup-device BSTR while still publishing the
filesystem node. CopperStart's public `MakeDosNode` bridge previously rejected
the null packet field, so the native command's source-compatible optional-device
path could not reach the provider. The provider now validates the device field
as optional and writes a zero-length resident BSTR when it is omitted. The
focused `DosObjectCoreTests` suite passes 20/20, including the new
`ExpansionMakeDosNodeAcceptsOmittedDeviceAndPublishesEmptyStartupDevice`
regression. This is provider semantic evidence for the Mount slice; device
discovery, handler/media execution, Workbench parity, lifecycle, purity and
package admission remain open.

## 2026-09-24 MorphOS Version comparison ordering

The MorphOS Version body now matches source `cmpargsparsed()` semantics: only
a requested version/revision above the discovered value returns `RETURN_WARN`;
lower requests return `RETURN_OK`. System and single-RES paths print before
applying that result. The expanded fixture covers lower major/revision,
higher major/revision, revision-only, and resident comparisons. Its refreshed
resident 68000/020/040 receipt passes twenty supplied DOS/Exec vectors per CPU
at `artifacts/version-morphos-native-20260924-compare-v1/qualification.json`.
The Workbench body remains unchanged because its classic comparator has not
been independently captured. FILE/MD5 scanning, optional providers, exact
guest output, packed correspondence, PURE/resident lifecycle, licensing and
package admission remain open.

## 2026-09-19 - MorphOS LibList multi-node traversal checkpoint

The LibList supplied-vector fixture now constructs a linked Exec library list
with zero, one, and three real nodes, distinct names/version/revision/open-count
and flags, and verifies every formatted row. The Ctrl-C case now uses a
three-node list and proves that the first-row signal poll stops traversal while
still freeing the snapshot buffer. The three-CPU resident receipt
`artifacts/liblist-morphos-native-20260919-multinode-v2/qualification.json` passes
eleven invocations per CPU (33 total), including allocation failure, missing
DOS, startup and malformed-entry rejection, cleanup, and interleaved callers.
Full installed population, packed correspondence, original guest differential, PURE/resident
lifecycle, packaging and licensing remain open.

## 2026-09-19 - MorphOS DevList multi-node traversal checkpoint

The DevList fixture now constructs zero, one, and three linked Exec devices
with distinct names/version/revision/open-count and flags, verifies each row,
and uses the three-node list for the Ctrl-C stop case. A missing-DOS startup
case also proves that no Exec snapshot allocation occurs when DOS 37 cannot be
opened. The three-CPU receipt
`artifacts/devlist-morphos-native-20260919-multinode-v2/qualification.json`
passes eleven invocations per CPU (33 total), with balanced buffer/list
ownership and unchanged shared images. Complete installed population, packed
correspondence, original guest differential, PURE/resident lifecycle,
packaging and licensing remain open.

## 2026-09-19 - MorphOS ResList multi-node traversal checkpoint

The ResList fixture now constructs zero, one, and three linked Exec resources
with distinct names and verifies every address/name row. Its Ctrl-C case uses
the three-node list to prove first-row cancellation and buffer cleanup, while a
missing-DOS case proves that the resident entry fails before allocating the
snapshot. The three-CPU receipt
`artifacts/reslist-morphos-native-20260919-multinode-v2/qualification.json`
passes eleven invocations per CPU (33 total) with unchanged shared images.
Complete installed population, original guest differential, PURE/resident
lifecycle, packaging and licensing remain open.

The source-fidelity pass removed the earlier direct `LoadSeg` experiment from
the native body. `FORCELOAD/K` now remains an input to the handler publication
contract, while the resident node carries the copied handler BSTR and no
invented segment ownership. The source-faithful receipt has zero runtime
features/helpers/external targets/exception regions/fatal fault sites on all
three CPUs; real DOS handler loading and publication still need a guest fixture.

## 2026-09-19 - MorphOS PortList multi-node traversal checkpoint

The PortList supplied-vector fixture now constructs zero, one, and three linked
message ports with distinct names, signal bits, task addresses, and task names,
and verifies every formatted row. Its Ctrl-C case uses the three-port list to
prove first-row cancellation, while a missing-DOS case proves that the resident
entry fails before allocating the snapshot. The three-CPU receipt
`artifacts/portlist-morphos-native-20260919-multinode-v2/qualification.json`
passes eleven invocations per CPU (33 total), with balanced buffers and
unchanged shared images. Complete installed population, original guest
differential, PURE/resident lifecycle, packaging and licensing remain open.

## 2026-09-20 - MorphOS Info multi-node traversal checkpoint

The MorphOS Info fixture now constructs zero, one, and two device/volume pairs,
copies all public names and startup data under the DOS-list lock, and verifies
filesystem `Info` allocation/free cycles after unlock for each device. The new
case preserves filtering, GOODONLY, wildcard matching, VERBOSE, BLOCKS,
parser/allocation, Ctrl-C, startup-boundary and interleaved coverage. The
three-CPU receipt
`artifacts/info-morphos-native-20260920-multinode-v1/qualification.json`
passes eighteen invocations per CPU (54 total), with unchanged shared images
and balanced result-array, list-lock, `InfoData`, and parser ownership. The
Workbench 3.1 candidate was requalified with its new two-device case at
twelve invocations per CPU in
`artifacts/info-wb31-native-20260920-multinode-v3/qualification.json`.
Provider/locale behavior, original guest differential, PURE/resident lifecycle,
packaging and licensing remain open.

## 2026-09-20 - Workbench Info multi-device traversal checkpoint

The Workbench 3.1 `Info` fixture now constructs and renders one or two device
nodes for the default `DEVICE`-list path. The second case verifies per-device
lock, `InfoData` allocation, unlock and classic `VPrintf` row ownership while
the explicit `DH0:` path remains unchanged. The three-CPU receipt
`artifacts/info-wb31-native-20260920-multinode-v3/qualification.json` passes
twelve invocations per CPU (36 total), with unchanged resident static closure,
shared images and balanced parser/list/filesystem ownership. Missing Workbench
media, exact original output and disk geometry, original guest differential,
PURE/resident lifecycle, packaging and licensing remain open.

## 2026-09-20 - Workbench Info mounted-volume ordering checkpoint

The Workbench `Info` implementation now emits the mounted-volume header on
the first volume record regardless of whether device rows preceded it. The
fixture adds a mixed two-device/two-volume traversal and validates the classic
direct-string `VPrintf("%s [Mounted]\\n", name)` ABI separately from the packed
device-row formatter. The three-CPU receipt
`artifacts/info-wb31-native-20260920-volumes-v2/qualification.json` passes
thirteen invocations per CPU (39 total), with unchanged static closure,
shared images and balanced list/filesystem ownership. Exact original status
ordering, guest differential, PURE/resident lifecycle, packaging and licensing
remain open.

## 2026-09-20 - MorphOS DOSList multi-node traversal checkpoint

The DOSList supplied-vector fixture now constructs zero, one, and three linked
device, volume, and assign nodes. Each pass verifies its own read lock,
`NextDosEntry` ordering, BSTR name, node address, filtering, verbose mounted
state, and matching unlock; parser, allocation, Ctrl-C, startup, and
interleaved cases remain covered. The three-CPU receipt
`artifacts/doslist-morphos-native-20260920-multinode-v1/qualification.json`
passes eighteen invocations per CPU (54 total), with unchanged shared images
and balanced DOS-list locks and buffers. Provider-specific process/detail rows,
original guest differential, PURE/resident lifecycle, packaging and licensing
remain open.

## 2026-09-20 - Status multi-process traversal checkpoint

The shared Status fixture now constructs one or three legacy CLI processes with
distinct command BSTRs, priorities, global-vector values, and stack metadata.
Both the MorphOS DOS 37 and Workbench DOS 36 resident entries traverse all
three process slots while preserving process/command filters, `TCB`/`FULL`,
missing-process, Ctrl-C, parser, startup and interleaved cases. The MorphOS
receipt `artifacts/status-morphos-native-20260920-multinode-v1/qualification.json`
and Workbench receipt
`artifacts/status-wb31-native-20260920-multinode-v1/qualification.json` each
pass fifteen invocations per CPU (45 total), with unchanged shared images and
balanced parser ownership. The DOS 51.51 `QueryCLIDataTags` path, complete
installed population, original guest differential, PURE/resident lifecycle,
packaging and licensing remain open.

## 2026-09-20 - MorphOS TaskList multi-node traversal checkpoint

The TaskList supplied-vector fixture now builds three linked ready tasks and
three linked waiting tasks with distinct addresses, names, priorities, stack
sizes and MorphOS ProcessId responses. A new three-list case verifies ordering
across the current, ready and waiting snapshots while the existing name,
address, exclusion and Ctrl-C cases retain their single-task expectations. The
three-CPU receipt
`artifacts/tasklist-morphos-native-20260920-multinode-v1/qualification.json`
passes seventeen invocations per CPU (51 total), with balanced `Disable`/
`Enable`, buffer and `ReadArgs` ownership and unchanged shared images. Sysdebug
stack/register modes, complete installed population, original guest
differential, PURE/resident lifecycle, packaging and licensing remain open.

## 2026-09-19 - MorphOS ModList multi-node traversal checkpoint

The ModList supplied-vector fixture now constructs zero, one, and three resident
module table entries with distinct names, versions, revisions, priorities, flags,
and ID strings, and verifies every formatted row. It covers both the normal
revision field and the `VERBOSE` extended/ID-string path. Its Ctrl-C case uses
the three-entry table to prove first-row cancellation, while a missing-DOS case
proves that startup fails before the resident snapshot is allocated. The
three-CPU receipt
`artifacts/modlist-morphos-native-20260919-multinode-v2/qualification.json`
passes thirteen invocations per CPU (39 total), with balanced buffers and
unchanged shared images. Full installed population, original guest differential,
PURE/resident lifecycle, packaging and licensing remain open.

## 2026-09-19 - Workbench 3.1 Mount profile captured

The selected Workbench 3.1 `C/Mount` binary is 6,880 bytes, version
`Mount 40.4 (27.9.93)`, SHA-256
`f47fa83e2efe3af8313b93f01ca79ede9999c62d0b5d7bfb97110e60b44de948`.
Its outer `ReadArgs` template is `DEVICE/M,FROM/K`. The inner template was
captured at byte offset 6,396, including the leading comma, and has 31 slots:
the empty first slot, `SECTORSIZE=BLOCKSIZE`, the empty third slot, then the
source-observed Workbench ordering through `FORCELOAD`. The slot map and exact
strings are recorded in the Mount contract.

`NativeWorkbench31MountCommand` now selects that profile explicitly while
reusing the guest-owned Mount transaction; `NativeWorkbench31MountEntry` opens
DOS 36 and keeps the resident Workbench startup boundary separate from the
MorphOS entry. Three-CPU resident HUNK compilation passes with zero managed
allocations, runtime helpers, external targets, exception regions, or fatal
fault sites at
`artifacts/mount-wb31-native-20260919-wildcard/qualification.json`.
This is a static profile checkpoint. Complete Workbench source selection,
handler/media execution, lifecycle/error parity, PURE, differential and
package gates remain open.

The current Workbench images are 14,276/14,492/14,268 bytes for
68000/68020/68040 with SHA-256
`a315afab12298f7b3fdbdcb7097ce93bd07b0481556fc0994df00ee570a03fac`,
`652352e8be7f5b90f5405bf0ad567e42f93ca698806d94a60f46b9f67bf0f90b`, and
`1dc42cd8ec44f8ccbb93cc3f32dd823b53f6510399fc3231889dc2191a3c2206`.

## 2026-09-19 - Mount wildcard traversal

Direct non-device `DEVICE/M` values now use public DOS `MatchFirst`,
`MatchNext`, and `MatchEnd` with an invocation-owned `AnchorPath` and 512-byte
path buffer. Each match is passed through the same source parser and
guest-owned `MakeDosNode` transaction, and the matcher is ended and its
workspace freed on success, parser failure, or publication failure. A failed
match remains an error; a normal `ERROR_NO_MORE_ENTRIES` boundary is cleared
after at least one source has been processed.

The three-CPU resident receipts are
`artifacts/mount-morphos-native-20260919-wildcard/qualification.json` and
`artifacts/mount-wb31-native-20260919-wildcard/qualification.json`; both pass
the static compatibility gates with no managed allocations, runtime helpers,
external targets, exception regions, or fatal fault sites. MountList block
selection, `.info` tool types, real handler/media execution, and the remaining
runtime/PURE/differential/package gates remain open.

## 2026-09-19 - MountList fallback parser

The shared Mount transaction now implements the source-observed fallback after
the five DOSDrivers probes miss. It opens `DEVS:MountList`, reads the file
through public DOS `Seek`/`Read` calls into invocation-owned guest memory,
applies the source `preparefile` comment and separator normalization, scans
device labels with `ReadItem`, and feeds the selected source cursor to the
profile-specific inner `ReadArgs` template. Device-name comparison is
case-insensitive and strips the command's trailing colon before publishing
the node.

Both Workbench and MorphOS static receipts were regenerated and now report 40
and 39 reachable methods respectively on 68000/020/040. The parser is still a
static checkpoint:
`.info` tool types, real handler/media providers, PURE/resident lifecycle,
original-guest differential behavior, and package admission remain open.
The resident entries now also consume Workbench startup argument lists, switch
to each supplied argument lock, restore the caller directory, reply the startup
message after cleanup, and retain the same static-only qualification boundary.

## 2026-09-20 - MorphOS Avail profile split

MorphOS `Avail` no longer delegates to the Workbench body. The separate DOS 37
resident implementation records the source-observed
`CHIP/S,FAST/S,TOTAL/S,FLUSH/S,H=HUMAN/S` grammar, rejects multiple selectors,
performs the public-memory flush probe, locks summary queries with
`Forbid`/`Permit`, and formats K/M/G/B human values. The receipt
`artifacts/avail-morphos-native-20260920-human-v7/qualification.json` passes
eleven supplied vectors per CPU on 68000/020/040 with no managed runtime
features or shared-image writes. Workbench's nine-vector receipt still passes
unchanged. Packed 50.6 correspondence, exact original output/diagnostics,
guest parity, lifecycle/PURE and package admission remain open.

## 2026-09-19 - Mount icon tool types

Missing DOSDriver definitions now fall through to the source-observed
`icon.library` v37 path. The native body resolves the matching DiskObject,
walks its guest `do_ToolTypes` vector at the Workbench layout offset, skips
comment and `IM?=` entries, bounds the aggregate in a guest-owned buffer, and
parses the eligible tool text with `RDAF_NOPROMPT` before publishing one node.
Direct trailing-colon resolution also probes the matching icon when the five
DOSDrivers files are absent, so `.info`-only definitions reach the same path.
Cleanup releases the DiskObject, parser, result array, tool buffer and icon
library on every branch.

Both three-CPU resident receipts were regenerated after this change and pass
the static compatibility gates. Runtime icon-library behavior, real handler or
media providers, PURE/resident lifecycle, original-guest differential behavior,
and package admission remain open.

## 2026-09-19 - MorphOS DiskFree resident stage

The MorphOS command reference fixes DiskFree's exact template as
`VOLUME,NOPOSTFIX/S,PERCENT/S`. A native resident body now parses that grammar,
locks the selected volume (or the current volume when omitted), calls public
DOS `Info`, computes free bytes and percentage without managed arithmetic
helpers, and releases the `InfoData` block, lock, parser and output storage on
all exits. `NOPOSTFIX` emits the byte-count mode; the default path selects a
bounded KB/MB/GB suffix and `PERCENT` emits the free percentage.

The three-CPU static receipt is
`artifacts/diskfree-morphos-native-20260919-static-v3/qualification.json`.
The supplied-vector runtime receipt is
`artifacts/diskfree-morphos-native-20260919-runtime-v3/qualification.json`;
it passes fifteen invocations per CPU for current/explicit volume selection,
byte and percentage modes, large byte and overflow-safe percentage arithmetic,
parser/provider/allocation failures, startup boundaries, cleanup, and
interleaved callers. These are ABI
and ownership checkpoints only: exact localized output, provider behavior
outside the fixture, original guest differential, PURE/resident lifecycle, and
package admission remain open.

## 2026-09-20 - MorphOS Version FULL resident output

The MorphOS `Version` resident path now locates the numeric version tail in the
guest `$VER:` `rt_IdString` and prints it after the resident name for `FULL`,
matching the source's resident construction while retaining the short version
form for ordinary lookups. The fixture checks
`$VER: dos.library 50.6 (fixture)` producing `dos.library 50.6 (fixture)` and
the three-CPU receipt
`artifacts/version-morphos-native-20260920-full-v3/qualification.json` passes
fourteen supplied vectors per CPU with 17 reachable methods, balanced
ownership and no shared-image writes. File/MD5/provider paths, system FULL
parsing, guest parity, PURE/resident lifecycle and package admission remain
open.

## 2026-09-20 - MorphOS SetClock UTC provider branch

The MorphOS `SetClock` body now implements the source's provider-version gate:
when both `battclock.resource` and `timer.device` are version 52 or newer,
`LOAD` uses `ReadUTCBattClock` and `TR_SETUTCSYSTIME`, while `SAVE` uses
`GetUTCSysTime` and `WriteUTCBattClock`; classic providers retain the original
vectors. The three-CPU resident receipt
`artifacts/setclock-morphos-native-20260920-utc-v3/qualification.json` passes
seventeen supplied vectors per CPU, including classic and UTC load/save/reset
and the source's non-IoErr timer failure diagnostic, with 17 reachable methods,
balanced ownership and no shared-image writes. Real clock-device behavior,
original guest parity, PURE/resident lifecycle and package admission remain
open.

## 2026-09-20 - Workbench Version FULL resident output

The Workbench 3.1 `Version` resident path now locates the numeric version tail
in the guest `$VER:` `rt_IdString` and prints it after the resident name for
`FULL`, while retaining the short version form for ordinary lookups. The
fixture checks `$VER: dos.library 50.6 (fixture)` producing
`dos.library 50.6 (fixture)` and the three-CPU receipt
`artifacts/version-wb31-native-20260920-full-v2/qualification.json` passes
fifteen supplied vectors per CPU with 17 reachable methods, balanced ownership
and no shared-image writes. File/UNIT/INTERNAL providers, exact classic output
and positional behavior, guest parity, PURE/resident lifecycle and package
admission remain open.

## 2026-09-20 - MorphOS WaitForPort resident stage

MorphOS `WaitForPort` now has a source-documented
`PORTNAME/A,I=INTERVAL/K/N,L=LOOP/K/N,D=DISAPPEAR/S` contract and a separate
resident entry. The bounded body uses DOS `ReadArgs`/`FreeArgs`, Exec
`FindPort`/`SetSignal`, and DOS `Delay`, with default interval/loop handling,
presence and disappearance modes, loop exhaustion, numeric overflow, parser
failure, Workbench-startup rejection, Ctrl-C and interleaved calls. The entry
preserves the existing resident startup owner: it receives startup messages,
opens DOS 37, rejects Workbench use for this CLI command, validates the
argument boundary, and closes/replies through `NativeCommandStartup.Finish`.

The three-CPU receipt
`artifacts/waitforport-morphos-native-6d77780fa9644fa2b68fd765d191f27d/qualification.json`
passes ten supplied vectors per CPU on 68000/020/040, with twelve reachable
methods, no managed runtime features, balanced DOS/parser ownership and no
shared-image writes. This is an adapter checkpoint only: exact guest timing,
diagnostics, parser/help behavior, packed correspondence, provider behavior,
original differential, PURE/resident lifecycle, licensing and package
admission remain open.

## 2026-09-20 - MorphOS WaitForLib resident stage

MorphOS `WaitForLib` now has a source-documented
`LIBNAME/A,I=INTERVAL/K/N,L=LOOP/K/N` contract and a separate resident entry.
The bounded body uses DOS `ReadArgs`/`FreeArgs`, Exec `FindName`/`SetSignal`,
and DOS `Delay` to poll the public library list, with the documented default
interval and eleven-check loop, loop exhaustion, numeric-failure,
parser/startup-boundary, Ctrl-C and interleaved paths. The candidate does not
open or load the requested library while polling.

The three-CPU receipt
`artifacts/waitforlib-morphos-native-912808104128434b81af17a62421d3de/qualification.json`
passes ten supplied vectors per CPU on 68000/020/040, with thirteen reachable
methods, balanced DOS/parser ownership and no shared-image writes. This is an
adapter checkpoint only: exact guest timing, diagnostics, parser/help
behavior, packed correspondence, provider behavior, original differential,
PURE/resident lifecycle, licensing and package admission remain open.
## 2026-09-20 - MorphOS WaitForNotification resident stage

MorphOS `WaitForNotification` now has a bounded source-documented native
candidate. `src/Commands/Native/NativeMorphOSWaitForNotificationCommand.cs`
preserves the documented `NAME/A/M,QUIET/S,CONTINUE=CNT/S` grammar, owns the
ReadArgs lease, allocates one Exec signal and request workspace, registers each
name through DOS `StartNotify`, supports continuation after a failed
registration, waits on the signal or Ctrl-C, and calls `EndNotify` for every
successful request before releasing memory and parser state. The resident entry
is `NativeMorphOSWaitForNotificationEntry` and rejects Workbench startup using
the existing lifecycle owner.

The supplied fixture covers one and multiple names, QUIET, failed registration,
CONTINUE with partial and empty registration sets, late registration failure,
Ctrl-C, parser/startup boundaries and interleaved callers. The three-CPU resident
receipt
`artifacts/waitfornotification-morphos-native-0a2f332649224908b505d5f230a1dc49/qualification.json`
passes twelve invocations per CPU with no shared-image writes or leaked guest
resources. Exact original diagnostics/output, packed correspondence, provider
behavior, original differential, PURE/resident lifecycle, licensing and package
admission remain open.

## 2026-09-20 - MorphOS RequestFile interleaving checkpoint

The RequestFile fixture now runs the normal single-selection and complete
option/tag cases as two interleaved callers on the same resident image. The
deterministic receipt
`artifacts/requestfile-morphos-native-20260920-interleaved/qualification.json`
passes twelve supplied DOS/ASL invocations per CPU with unchanged HUNK hashes,
balanced requester/parser/buffer ownership and no shared-image writes.

## 2026-09-20 - Workbench RequestChoice syntax candidate

The Workbench 3.1 `C/RequestChoice` identity is now bound to its 1,120-byte
v39.4 HUNK and captured four-slot candidate
`TITLE/A,BODY/A,GADGETS/M,PUBSCREEN/K`. A separate DOS36/Intuition resident
candidate implements `ReadArgs` ownership, gadget joining, percent escaping,
requester output, parser/lock/allocation/startup failures, repeat cleanup and
two interleaved callers. Its deterministic receipt
`artifacts/requestchoice-wb31-native-20260920-candidate/qualification.json`
passes eleven supplied invocations per CPU on 68000/020/040. This is not yet
Workbench behavioral parity: exact UI/diagnostics, timeout/type policy,
original lifecycle/PURE, packaging and differential evidence remain open.

## 2026-09-20 - MorphOS RequestFile option-tag checkpoint

The MorphOS `RequestFile` fixture now verifies the complete source-derived
14-slot `ReadArgs` boundary, including the optional `INITIALVOLUMES/S` slot,
and checks every corresponding ASL requester tag: initial drawer/file/pattern,
title and button text, save/multi-select/drawers-only modes, icon rejection,
public-screen name, pattern enablement and initial-volume display. It also
covers single and multi-select path output, cancellation, requester-buffer
allocation failure, parser failure, ASL-open failure, Workbench startup
rejection and repeat ownership.

The new qualifier
`tools/Commands/qualify_morphos_requestfile_native_entry.ps1` builds the
resident entry on 68000/020/040. Receipt
`artifacts/requestfile-morphos-native-20260920-interleaved/qualification.json`
passes twelve supplied invocations per CPU, with 16 reachable methods, no
managed runtime features/helpers/external targets/exception or fatal sites,
balanced ownership and no shared-image writes. Exact original requester/UI
behavior, Workbench correspondence, PURE/resident lifecycle, minimum stack,
licensing, package placement and differential evidence remain open.





## 2026-09-22 - MorphOS LoadMonDrvs supplied-vector entry fixture

Added `LoadMonDrvsEntrySuite` and a resident three-CPU fixture for the
candidate entry. Nine invocations per CPU cover default and `FROM`/`EXCEPT`
argument result ownership, parser failure, missing DOS, matcher no-match and
empty-scan cleanup, successful resident initialization, failed initialization
with segment unload, failed `LoadSeg`, repeat/interleaved execution, and zero
shared-image writes. The receipt is
`artifacts/loadmondrvs-morphos-native-entry-20260922-v3/qualification.json`.
Packed correspondence, provider-backed monitor loading, guest parity,
lifecycle/PURE, package and differential gates remain open.

## 2026-09-23 - MorphOS Info DOS 51.8 extended counters

The MorphOS Info resident body now follows the source-observed DOS 51.8 gate
for `GetFileSysAttr`, reads 64-bit total/used block counters, falls back to
`InfoData` for unsupported or failed attributes, clamps an invalid used count,
and renders the K/M/G/T/P size tiers and full `BLOCKS` values. The fixture now
sets the DOS library version on its actual early-returning `OpenLibrary`
handler, and checks legacy DOS 51.7 behavior, successful and partial extended
queries, M/P formatting, and the existing ownership/output cases.

The current MorphOS 68000/020/040 resident receipt passes 27 supplied vectors
per CPU with no shared-image writes or leaked guest resources:
`artifacts/cc11-info-morphos-native-20260923-wide-counters-v2/qualification.json`.
The Workbench 3.1 Info candidate was also rerun against the updated shared
fixture and passes fourteen vectors per CPU:
`artifacts/cc11-info-wb31-native-20260923-harness-v2/qualification.json`.
These fixtures do not close original guest comparison, localized rendering,
complete Workbench reference closure, PURE/resident lifecycle, package
placement, licensing, or shipping admission.

## 2026-09-23 - MorphOS Info device status and name rows

The MorphOS Info device row now reads `InfoData` disk state and filesystem
type, maps the source's read-only, validating, and read/write states, and uses
DOS `NameFromLock` to render the mounted volume name. Successful names lose
their trailing colon; a failed lookup keeps the copied DOS-list device name.
The supplied fixture checks all three source state labels and the blank
unknown-state fallback, PFS type mapping, lookup failure, and the DOS call's
lock/buffer contract.

The three-CPU resident receipt
`artifacts/cc11-info-morphos-native-20260923-device-rows-v2/qualification.json`
passes 56 invocation vectors per CPU with no shared-image writes or leaked
resources. Workbench Info was rerun after the shared fixture update and passes
14 vectors per CPU in
`artifacts/cc11-info-wb31-native-20260923-harness-v7/qualification.json`.
Other startup/provider details, exact device output, original guest parity,
localized output, PURE/resident lifecycle and package admission remain open.

## 2026-09-23 - MorphOS Info startup filesystem type

The device snapshot now follows the source's `FileSysStartupMsg` path for
`DosEnvec.DosType`: it requires a usable startup pointer, a unit whose high byte
is clear, a table size reaching `DE_DOSTYPE`, and a non-zero type. A
DOS-prefixed startup type preserves the filesystem type returned by
`InfoData`. The four new supplied vectors cover an SFS override, short table,
invalid unit and DOS-prefixed value.

The three-CPU resident receipt
`artifacts/cc11-info-morphos-native-20260923-startup-dostype-v1/qualification.json`
passes sixty invocations per CPU with no shared-image writes or leaked guest
resources. The Workbench 3.1 candidate was rechecked against the updated shared
fixture in
`artifacts/cc11-info-wb31-native-20260923-harness-v8/qualification.json` and
still passes fourteen invocations per CPU. Source-specific device error
handling, remaining verbose startup forms, exact guest output/parity,
PURE/resident lifecycle and package admission remain open.

## 2026-09-23 - MorphOS and Workbench List literal SUB filtering

MorphOS `List` now implements `SUB` as a case-insensitive literal file-name
substring filter using public DOS `ParsePatternNoCase` and
`MatchPatternNoCase`. DOS pattern punctuation is escaped, the filter combines
with `P=PAT`, and each parser uses distinct invocation-owned input and compiled
buffers. Its fixture also checks that matching directories are excluded. The
refreshed resident receipt passes twenty supplied vectors per CPU on
68000/020/040:
`artifacts/cc11-list-morphos-native-20260923-sub-v3/qualification.json`.

The separate Workbench 3.1 DOS 36 syntax candidate passes twenty-one vectors
per CPU (63 total) at
`artifacts/cc11-list-wb31-native-20260923-sub-v3/qualification.json`.
These fixtures verify the bounded filter behavior and supplied DOS-vector
ownership, but they do not establish original guest parity, packed
correspondence, recursive/date/sort/owner/LFORMAT/ALL behavior,
PURE/resident reuse, or shipping admission.

## 2026-09-23 - List default fields and KEYS

The shared MorphOS/Workbench candidate now omits the FIB key from ordinary
rows and displays it only with `KEYS`. File rows use byte size or `BLOCK`,
directories display `Dir`, protection values render as eight letters, FIB
timestamps pass through DOS `DateToStr`, and non-empty comments receive the
documented colon prefix. `QUICK` continues to print names without date calls.
The fixture also injects `DateToStr` failure and verifies matcher/workspace
cleanup and error publication.

The resident MorphOS receipt passes twenty-three supplied DOS-vector
invocations per CPU (69 total) at
`artifacts/cc11-list-morphos-native-20260923-format-keys-v3/qualification.json`.
The separate Workbench 3.1 DOS 36 syntax candidate passes twenty-four per CPU
(72 total) at
`artifacts/cc11-list-wb31-native-20260923-format-keys-v3/qualification.json`.
The dates and providers are synthetic; exact header/summary output, original
guest parity, recursion, date filters, sort, owner fields, `LFORMAT`, `ALL`,
PURE/resident reuse and package admission remain open.

## 2026-09-23 - MorphOS TaskList CLI process rows

MorphOS `TaskList` now distinguishes CLI processes from ordinary tasks using
the process CLI pointer. CLI rows use the source format's ` cli` classification
and include the CLI command name in brackets; bare Process nodes without CLI
state keep the ordinary process label. The command reads CLI state only for
Process nodes and copies the BSTR command name into the bounded task snapshot
before formatting outside `Disable`.

The refreshed resident 68000/020/040 receipt
`artifacts/tasklist-morphos-native-20260923-cli-rows-v7/qualification.json`
passes eighteen supplied DOS/Exec vectors per CPU with balanced ownership,
unchanged images and three-node ready/waiting traversal. Sysdebug-backed
verbose/stack/register paths, complete task population, original guest parity,
Workbench behavior, PURE/resident lifecycle and package admission remain open.

## 2026-09-23 - MorphOS TaskList CLI command-name filtering

TaskList's `NAME` filter now follows the inspected source: a match against the
task node name is accepted, and a Process with a CLI also matches its BSTR
`cli_CommandName`. CLI process classification and bracketed command-name output
remain covered. The new fixture filters for `Copy` while the Process node is
named `Worker`, proving the CLI-name path independently of node-name matching.

The refreshed resident 68000/020/040 receipt
`artifacts/tasklist-morphos-native-20260923-cli-name-filter-v2/qualification.json`
passes nineteen supplied DOS/Exec vectors per CPU with balanced ownership,
unchanged images and three-node ready/waiting traversal. MorphOS sysdebug
verbose/stack/register modes, full task population, original guest parity,
Workbench behavior, PURE/resident lifecycle and package admission remain open.

## 2026-09-23 - MorphOS TaskList public task attributes

The resident snapshot now queries MorphOS `NewGetTaskAttrsA` for PID, type,
priority, state, M68k/PPC stack sizes and bounds, and signal masks, using the
classic task fields where the public query can fail. Standard rows use the
source's PID widths and numeric stack-used values. A new 255-character CLI
command-name vector checks BSTR bounds and name output.

The refreshed 68000/020/040 receipt
`artifacts/tasklist-morphos-native-20260923-task-attributes-v1/qualification.json`
passes twenty supplied DOS/Exec vectors per CPU with balanced ownership,
unchanged images and no managed allocation sites, runtime helpers or
exception/fatal-fault sites. Sysdebug-backed verbose/stack/register modes,
complete task population, original guest parity, Workbench behavior,
PURE/resident lifecycle and package admission remain open.

## 2026-09-23 - MorphOS TaskList PPC register dump and verbose rows

TaskList now captures the MorphOS public PPC SRR0/LR/CTR/CR/XER, GPR/FPR,
VSAVE and VSCR attributes for each non-running task while the list snapshot is
protected. `REGDUMP` prints the source's five scalar values and four eight-GPR
rows. `VERBOSE` prints the source's nine state, signal and stack detail lines
for stopped tasks. Since the source reads live A7 for the running current task
and the native ABI has no qualified live-A7 accessor, `VERBOSE` with that task
included fails closed with `ERROR_NOT_IMPLEMENTED`.

The refreshed resident 68000/020/040 receipt
`artifacts/tasklist-morphos-native-20260923-regdump-v1/qualification.json`
passes twenty-six supplied DOS/Exec vectors per CPU (78 total), checking
attribute selectors, byte counts, GPR/FPR tags, output order, combined
`VERBOSE`/`REGDUMP`, ownership, unchanged images and static native closure.
It also verifies the sysdebug v0 open/close lease and failed-open early exit,
and that filtering the running current task out allows verbose
rows for the selected stopped tasks.
Stacktrace symbolization, `INTERNAL`, `REGCHECK`, sysdebug provider calls,
MorphOS system-attribute boundary use, complete task population, original guest
parity and package/lifecycle admission remain open.

## 2026-09-24 - MorphOS TaskList system-boundary attributes

TaskList now calls public MorphOS `NewGetSystemAttrsA` after parsing, with the
source `TAG_DONE` list and source order for emulation start/size, module
start/size, and native/M68k task-exit stub addresses. The fixture validates all
six selectors, 32-bit data sizes, the terminating tag pair, and ordering before
`FreeArgs` on all parsed paths. These values are now captured for later frame
classification, but `STACKTRACE` remains fail-closed until that output path and
the sysdebug stackdump providers are implemented.

The resident 68000/020/040 receipt
`artifacts/tasklist-morphos-native-20260924-systemattrs-v1/qualification.json`
passes twenty-six supplied DOS/Exec vectors per CPU (78 total), retaining the
sysdebug v0 lease/open-failure checks, task attributes, stopped-task
`VERBOSE`, PPC `REGDUMP`, ownership, unchanged images, and static native
closure. Original guest parity, complete task population, PURE/resident
lifecycle, licensing, and package admission remain open.

## 2026-09-24 - MorphOS TaskList sysdebug ABI audit

The locally available MorphOS 3.20 SDK documents `SysDebugFindSeg` as a PPC
SysV call and includes its PPC inline wrapper, but no 68k inline wrapper in the
packaged entries. TaskList's resident output is a 68k HUNK, so calling the PPC
vector as an ordinary classic library function would be an ABI mismatch. The
SDK separately documents the legacy `SegTracker` semaphore and function-pointer
contract for 68k callers; that alternative must be qualified before symbol
output is enabled. Audit: `reference-captures/tasklist-sysdebug-sdk-20260924.json`.

The same SDK audit found public Exec `TASKINFOTYPE_PPC_STACKHISTORY` (`0x34`,
Exec 51.54+) through `NewGetTaskAttrsA`, returning PPC frame-address records.
This is a possible native source for PPC stack frames, but it does not provide
symbol names, Hunk/offset data, ABOX classification, special-frame handling,
or `REGCHECK`. The API explicitly requires `Forbid()` when querying a foreign
task; the current bounded snapshot uses `Disable()`, so its synchronization
contract must be checked before using this selector. No command behavior or
qualification count changed in this documentation-only update.

## 2026-09-24 - MorphOS TaskList scheduler protection

The SDK's foreign-task access rule applies to the existing public
`NewGetTaskAttrsA` snapshots as well as the prospective stack-history selector.
TaskList now holds `Forbid()` while walking the ready/wait lists and capturing
public task attributes, then balances it with `Permit()` before output. The
fixture requires every foreign-task attribute query to occur while forbidden
and checks the balanced release. The refreshed resident 68000/020/040 receipt
passes twenty-six supplied vectors per CPU at
`artifacts/tasklist-morphos-native-20260924-forbid-v1/qualification.json`.
This addresses scheduler protection only; it does not enable
`STACKTRACE`/`INTERNAL`/`REGCHECK` or close original guest, lifecycle, package,
or licensing gates.

## 2026-09-24 - MorphOS TaskList task-buffer growth

TaskList no longer caps its snapshot at 64 entries. It starts with a 128 KiB
`AllocVec` buffer and frees/retries with a doubled buffer when the protected
ready/wait traversal does not fit. A 100-ready-task fixture crosses the initial
capacity, confirms the partial snapshot is discarded, retries at 256 KiB, and
checks all 100 output rows plus balanced allocation/free and `Forbid`/`Permit`
cycles. The refreshed 68000/020/040 receipt passes twenty-seven supplied
DOS/Exec vectors per CPU at
`artifacts/tasklist-morphos-native-20260924-grow-retry-v1/qualification.json`.
This closes the fixture's former three-node-only population limit; source/packed
correspondence, complete MorphOS task population and advanced stack modes,
original guest parity, Workbench behavior, PURE/resident lifecycle, licensing
and package admission remain open.

## 2026-09-24 - MorphOS Version system display fixture

The MorphOS `Version` candidate now has supplied vectors for its system
component sequence, system `FULL` line, Workbench-version comparison and
unavailable-`version.library` fallback. The three-CPU resident qualification
passes 42 vectors per CPU at
`artifacts/version-morphos-native-20260924-system-v5/qualification.json`.
The Workbench DOS36 candidate also passes its 15-vector-per-CPU regression at
`artifacts/version-wb31-native-20260924-system-regression-v1/`. The MorphOS
system candidate still reads the opened `version.library` base directly rather
than locating it through Exec `LibList` under `Forbid`/`Permit`; Ambient ARexx
and file-based fallback are also absent. Neither fixture receipt proves
original guest parity, packed correspondence, PURE/resident lifecycle,
licensing or package admission. CC17 remains open.

## 2026-09-24 - MorphOS Version LibList and FULL parsing follow-up

The system-version body now looks up `version.library` through Exec's
case-insensitive `LibList` under balanced `Forbid()`/`Permit()`, continues when
the MorphOS resident is absent, and preserves the source's partial prefix and
failure result when the version library cannot be opened or is missing from the
list. It copies the selected dotted Workbench version tail into invocation-owned
memory for the existing parser and date formatter, and falls back to the numeric
library version/revision if parsed text disagrees with the node fields.

The resident HUNK and static compatibility checks pass on 68000/020/040; the
fixture passes 49 supplied vectors per CPU. Receipt:
`artifacts/version-morphos-native-20260924-system-v6/qualification.json`.
During qualification, an ABI misunderstanding was corrected: DOS `StrToLong`
returns the converted value, not the number of consumed characters. The token
scan now counts digits directly. The fixture also moved its longer synthetic
`IdString` and library name apart so the case does not overwrite the numeric
argument cells. Workbench's existing 15-vector-per-CPU regression remains
separate and unchanged.

Ambient ARexx and file-based Ambient providers, extended MorphOS resident
revision fallback, FILE fallback/LoadSeg resolution, original guest parity,
packed correspondence, PURE/resident lifecycle, rights, packaging and
installation remain open. This checkpoint does not close CC17 or change the
shipping/profile completion counts.
## 2026-09-24 - MorphOS Version resident parser and extended revision

The source-backed `RES` path now constructs its parsed string from the resident
name and the version tail in `rt_IdString`, rather than taking the version byte
from the resident header on normal reads. It uses the DOS-backed parser for
version/revision and `FULL` date formatting. A versionless ID prints the source
name-only form. Shared formatting now preserves malformed-date extra text
separately and does not duplicate unparenthesized tail text in `FULL` output.

The fallback path now matches the source's resident header behavior after
parse/allocation failure: `rt_Version`, revision -1 on classic residents, and
`rt_Revision` when MorphOS `RTF_EXTENDED` bit 6 is set. The field location is
verified against MorphOS Team's official V50 `exec/resident.h` and the captured
`makeresidentver()` branch in
`reference-captures/version-morphos-resident-sdk-abi-20260924.json`.

MorphOS resident HUNK qualification passes 53 supplied DOS/Exec vectors per
CPU on 68000/020/040, with no shared-image writes or leaked fixture resources.
Workbench's independent DOS 36 candidate still passes fifteen vectors per CPU.
Receipts:
`artifacts/version-morphos-native-20260924-resident-v1/qualification.json` and
`artifacts/version-wb31-native-20260924-resident-regression-v1/qualification.json`.
This remains fixture evidence, not original guest parity or shipping admission.
Ambient ARexx/file providers, FILE fallback/LoadSeg resolution, packed/source
correspondence, original guest comparison, PURE/resident lifecycle, rights,
package and install gates remain open; the 0/200 and 0/246 counts do not change.

## 2026-09-24 - MorphOS Version default Exec LibList lookup

The MorphOS named lookup now follows DOS `FilePart` for both default and `RES`
paths, checks the Exec Resident table, then scans Exec `LibList` under
`Forbid`/`Permit`. Library names are matched case-insensitively. The candidate
captures numeric version/revision while protected, copies the node name before
unlocking, and uses matching `IdString` data for `FULL` date/extra output;
malformed or inconsistent text falls back to the node's numeric fields.
Command-segment lookup remains the next fallback for now and must move behind
the not-yet-implemented source-ordered filesystem library/device candidates.

The refreshed resident qualification at
`artifacts/version-morphos-native-20260924-liblist-v5/qualification.json`
passes 77 supplied DOS/Exec vectors per CPU on 68000/020/040, including
case-folded path-derived LibList lookup, numeric comparison, `FULL` date/extra
formatting, and `RES` basename lookup. HUNKs have 59 reachable methods, no
managed allocation sites, runtime helpers/features, external targets,
exception regions or fatal fault sites; fixtures report no leaks or writes to
the shared resident image. The separate Workbench 3.1 regression passes 15
vectors per CPU at
`artifacts/version-wb31-native-20260924-liblist-regression-v2/`.

Files changed: `src/Commands/Native/NativeMorphOSVersionCommand.cs`,
`tests/Commands.NativeExecution/VersionEntrySuite.cs`,
`tests/Commands.NativeExecution/Program.cs`,
`tools/Commands/qualify_morphos_version_native_entry.ps1`, the Version contract,
the goal plan, the completion ledger, the qualification report and this log.

Commands run: MorphOS native qualification at the v5 receipt path and the
separate Workbench native qualification at the v2 regression path; both pass
on all three CPU profiles. An earlier Workbench rerun exposed a profile-shared
fixture expectation that was incorrectly applied to Workbench `RES`; the
verifier was restricted to MorphOS `FilePart` calls and the full Workbench
regression then passed.

Next: implement and qualify MorphOS `MOSSYS:LIBS/` then `LIBS:` file lookup
with source-ordered extension handling; follow with `DeviceList`, `MOSSYS:DEVS/`
and `DEVS:` candidates, direct name/volume resolution, and finally move
`FindSegment` to its captured last-resort position. Original guest, packed
correspondence, PURE/resident lifecycle, licensing, installed flags and
packaging remain release blockers; CC17 remains open.

## 2026-09-24 - MorphOS Version Utility `Stricmp` lease and cleanup

The loaded-library fallback now uses the captured MorphOS Utility `Stricmp`
vector through an explicit caller-provided A6 base. `utility.library` is opened
at v37 only when a resident-name lookup misses, and the lease is closed after
both a successful match and failure paths. The native fixture now checks the
comparison ABI, the unavailable-library error, balanced open/close calls, and
empty Exec `LibList` setup
without disturbing interleaved system-version cases.

The three-CPU resident qualification passes 78 supplied vectors on each of
68000/020/040 at
`artifacts/version-morphos-native-20260924-liblist-v17/qualification.json`.
Static compatibility reports show 60 reachable methods and no managed
allocation sites, runtime helpers/features, external targets, exception
regions or fatal fault sites; the fixture reports no resource leaks or shared
resident-image writes. The Workbench regression remains clean at 15 vectors
per CPU:
`artifacts/version-wb31-native-20260924-liblist-regression-v4/`.

This closes the tested LibList fallback slice only. MorphOS `MOSSYS:LIBS/`
and `LIBS:` candidates, `DeviceList`, `MOSSYS:DEVS/` and `DEVS:` candidates,
direct file/volume ordering, and placing `FindSegment` last remain next.
Original-guest parity, packed correspondence, PURE/resident lifecycle,
licensing, installed-state evidence, and package admission still block CC17
completion.

## 2026-09-24 - MorphOS BindDrivers provider-failure matrix

Expanded the native `BindDrivers` fixture from eight to seventeen scanner
vectors per CPU. New cases cover directory entries; missing Icon objects,
PRODUCT tool types and ConfigDev results; missing Resident records; raw LoadSeg
and InitResident failures; and NameFromLock/AddPart failure handling. The
provider callbacks assert warning results, exact PrintFault labels, IoErr
publication, ConfigDev chain contents, DiskObject freeing, and segment retain
versus unload policy. The existing source-shaped success case remains covered.

The three-CPU qualification passes seventeen scanner invocations plus the
separate production PRODUCT-parser invocation on each of 68000/020/040. It
reports zero shared-image writes and no leaked resources. HUNK sizes and
reachable-method/static-runtime findings remain unchanged because this step
expanded the fixture only. Receipt:
`artifacts/binddrivers-morphos-native-entry-20260924-failure-paths-v8/qualification.json`.

Files changed: `tests/Commands.NativeExecution/BindDriversEntrySuite.cs`,
`tests/Commands.NativeExecution/Program.cs`,
`tools/Commands/qualify_binddrivers_native_entry.ps1`, the BindDrivers
contract, CC20 goal progress, completion ledger, qualification report and this
log.

Commands run: native execution fixture build; direct 68000/020/040 vector
runs while iterating; and the final three-CPU resident HUNK qualification
script. All final runs pass.

Next: qualify malformed PRODUCT and resident HUNK bounds, multiple PRODUCT
pairs through the full scan, and interleaved provider-backed repeat calls; then
bind the separate Workbench binary and original-guest outputs. No shipping row
is promoted by this fixture evidence.

## 2026-09-24 - MorphOS BindDrivers HUNK bounds and cycle detection

Hardened `FindLibResident` with public Exec `TypeOfMem` checks for the BPTR-
derived segment header, data start and declared HUNK end; overflow checks for
BPTR shifting, longword multiplication and address addition; and a per-record
bound that ensures both the Resident match word and match-tag longword remain
inside the HUNK. Added constant-space fast/slow segment-link cycle detection rather
than a fixed segment-count limit, preserving arbitrarily long valid lists.

The provider fixture now includes a valid Resident in the second HUNK, a
one-longword HUNK, truncated and overflowing extents, an invalid segment link,
and a two-node cyclic segment list. All malformed structures return no Resident and unload the segment; the
second-HUNK case initializes the returned Resident while passing the original
segment BPTR to InitResident. The three-CPU receipt passes twenty-three scanner
vectors plus the production PRODUCT-parser vector on each CPU, with fifteen
reachable methods and no managed runtime features/helpers, external targets,
exceptions, fatal sites, resource leaks or shared-image writes.

Receipt: `artifacts/binddrivers-morphos-native-entry-20260924-hunk-bounds-v9/qualification.json`.

Files changed: `src/Commands/Native/NativeMorphOSBindDriversCommand.cs`,
`tests/Commands.NativeExecution/BindDriversEntrySuite.cs`,
`tests/Commands.NativeExecution/Program.cs`,
`tools/Commands/qualify_binddrivers_native_entry.ps1`, the BindDrivers
contract, CC20 goal progress, completion ledger, qualification report and this
log.

Commands run: native execution fixture build and the three-CPU resident HUNK
qualification script. All final scanner and parser runs pass on 68000/020/040.

Next: exercise multiple PRODUCT pairs through the complete scanner, then
provider-backed repeat/interleaved cases; capture original Workbench and
MorphOS guest behavior and bind the Workbench binary to its independent profile.
No shipping row is promoted by supplied-vector evidence.

### 2026-09-24 - MorphOS BindDrivers end-to-end multiple PRODUCT pairs

Added a provider-backed successful scan for `PRODUCT=514/2|33/-4`. Its first
pair resolves two ConfigDev nodes and its second resolves one. The fixture
checks all five source-ordered `FindConfigDev` calls, the `SetCurrentBinding`
head, and the resulting third-to-second-to-first chain; DiskObject cleanup
releases all three provider nodes. The qualification gate now requires
twenty-four scanner vectors.

Receipt: `artifacts/binddrivers-morphos-native-entry-20260924-multipair-v10/qualification.json`.
The 68000/020/040 resident images pass all twenty-four scanner vectors plus
the production parser vector, with fifteen reachable methods, no runtime
features/helpers, external targets, exception regions or fatal sites, and no
leaks or shared-image writes.

Files changed: `tests/Commands.NativeExecution/BindDriversEntrySuite.cs`,
`tests/Commands.NativeExecution/Program.cs`,
`tools/Commands/qualify_binddrivers_native_entry.ps1`, the BindDrivers
contract, CC20 goal progress, completion ledger, qualification report and this
log.

Commands run: the three-CPU resident HUNK qualification script. All scanner
and parser vectors pass on 68000/020/040. Four unrelated nullable warnings
remain in other native execution fixtures.

Next: qualify malformed tool-type termination and provider-backed repeated /
interleaved scans; then capture original Workbench and MorphOS guest behavior
and bind the Workbench binary to its independent profile. No shipping row is
promoted by supplied-vector evidence.

### 2026-09-24 - MorphOS BindDrivers interleaving and directory flags

Made the BindDrivers scanner fixture storage invocation-local and added two
successful scans that run instruction-by-instruction on one shared resident
HUNK: a single-pair PRODUCT scan and a multiple-pair scan. Their Icon,
PRODUCT, ConfigDev and HUNK records occupy separate ranges. The interleaved
runs exercise balanced DOS/Icon/Expansion leases, matcher cleanup, ConfigDev
ownership and image immutability.

The directory fixture now seeds `APF_DIDDIR`, `APF_DOWILD`, and
`APF_NOMEMERROR`. It verifies that a directory match clears only
`APF_DIDDIR`, that a file match preserves every flag, and that an empty
successful scan keeps its zeroed AnchorPath.

Receipt: `artifacts/binddrivers-morphos-native-entry-20260924-anchor-flags-v12/qualification.json`.
The three-CPU resident images pass twenty-six scanner vectors and the parser
vector on 68000/020/040 with fifteen reachable methods, no runtime features or
helpers, external targets, exception regions or fatal sites, and zero leaks or
shared-image writes.

The Icon `FindToolType` AutoDoc returns a pointer into the matched value string
without a length, so no arbitrary parser limit was added. A malformed
unterminated response from an invalid Icon provider remains unverified; source
parity for valid NUL-terminated ToolTypes is preserved. The directory flag
transition is fixture-verified but still needs original guest comparison.

Files changed: `tests/Commands.NativeExecution/BindDriversEntrySuite.cs`,
`tests/Commands.NativeExecution/Program.cs`,
`tools/Commands/qualify_binddrivers_native_entry.ps1`, the BindDrivers
contract, CC20 goal progress, completion ledger, qualification report and this
log.

Commands run: the three-CPU resident HUNK qualification script and `git diff
--check`. All 26 scanner and parser vectors pass on 68000/020/040; four
unrelated nullable warnings remain in other native execution fixtures.

Next: qualify the malformed-provider boundary only if it can be expressed
without inventing a maximum that changes valid ToolType behavior; then capture
real MorphOS/Workbench guest results and complete profile, lifecycle, rights
and package evidence. No shipping row is promoted by supplied-vector evidence.

### 2026-09-25 - Workbench BindDrivers HUNK probe

Extended the native probe loader to map ordinary multi-HUNK CODE/DATA/BSS
images and apply cross-segment `HUNK_RELOC32` relocations. The original 38.2
Workbench binary now loads, and the runner can create relocated private image
copies. A single-invocation exploratory trace exposed a fixture-layout mismatch:
the first mapping paired the guest's vector target with the wrong synthetic DOS
base. After aligning those test addresses, the command reached
`SetCurrentBinding` with A0 and D0 both `$E000` and returned 5. The synthetic
provider does not initialize authentic Kickstart library/vector structures, so
those observations do not establish Workbench behavior. The test report is
marked observational only; it makes no parity, PURE or shipping claim. The
recorded protection is zero and PURE is not inferred.

The updated MorphOS resident qualification passes 26 scanner vectors plus one
parser vector on 68000/020/040. Receipt:
`artifacts/binddrivers-morphos-native-entry-loader-regression-20260925/qualification.json`.
The one-invocation Workbench trace is retained as
`artifacts/wb31-binddrivers-reference-single-no-match.json`; its status is
`observed`, not `passed`.

Files changed: `tests/Commands.NativeExecution/HunkImage.cs`,
`tests/Commands.NativeExecution/CommandTestBus.cs`,
`tests/Commands.NativeExecution/Program.cs`, the CC20 goal progress and this
log.

Commands run: native execution project build; the three-CPU MorphOS BindDrivers
resident qualification; one observational Workbench binary invocation.

Next: run under authentic Kickstart 3.1 DOS/Expansion vectors, or establish
the expected library bases and structures from direct machine evidence before
interpreting Workbench calls. Do not import MorphOS expectations into this
profile.

Workspace scan: no Kickstart ROM or original bootable Workbench disk image is
available here; `CopperStart/rom` is empty. The ADF/HDF files found are
generated disk-builder and filesystem test fixtures. Authentic guest execution
therefore remains unavailable in this workspace.

## 2026-09-25 - MorphOS LoadMonDrvs resident-walk bounds

The source-backed MorphOS `LoadMonDrvs` candidate previously trusted each
segment's declared HUNK size while scanning for a Resident record. It now
validates segment addresses, size arithmetic and the final declared byte with
Exec `TypeOfMem`; reads the complete Resident match-word/tag pair only when it
fits within that HUNK; and follows valid later HUNKs while rejecting cyclic
segment lists with constant-space cycle detection.

The three-CPU resident qualification passes fourteen supplied invocations per
CPU, including later-HUNK resident discovery, undersized/overflowing/unmapped
HUNK extents, cyclic links, initialization/rollback, repeated/interleaved
execution, and zero shared-image writes. Static reports show 19 reachable
methods and zero managed runtime features/helpers, external native targets,
exception regions or fatal sites. Receipt:
`artifacts/loadmondrvs-morphos-native-entry-20260925-hunk-safety-v4/qualification.json`.
The independent MorphOS `BindDrivers` scanner and PRODUCT-parser regression
also passes 26 scanner invocations plus one parser invocation per CPU; receipt:
`artifacts/binddrivers-morphos-native-entry-regression-20260925-loadmondrvs-hunk-safety/qualification.json`.

This closes a memory-safety gap in the candidate only; no claim is made about
packed-command correspondence, real monitor loading, MorphOS guest behavior,
PURE classification, licensing or package admission. The overall command row
remains open.

## 2026-09-25 - MorphOS SetKeyboard resident-walk bounds

Applied the HUNK safety checks to the MorphOS `SetKeyboard` candidate as well:
Exec `TypeOfMem` guards segment headers and declared ends; size multiplication
and end-address arithmetic are checked; Resident match/tag reads stay within
the current HUNK; later HUNKs remain supported; and cyclic segment chains are
rejected without an arbitrary length limit.

The resident qualification passes sixteen supplied invocations per CPU on
68000/020/040, covering undersized, overflowing, unmapped and cyclic HUNKs plus
existing resource, loading, publication, cleanup and startup cases. Reports
show no managed runtime features/helpers, external native targets, exception
regions, fatal sites, leaks or shared-image writes. Receipt:
`artifacts/setkeyboard-morphos-native-entry-20260925-hunk-safety-v2/qualification.json`.
The independent Workbench 3.1 entry regression passes ten vectors per CPU;
receipt:
`artifacts/setkeyboard-wb31-native-entry-regression-20260925-morphos-hunk-safety/qualification.json`.

The Workbench receipt is candidate-side only. Exact guest behavior, MorphOS
packed-template correspondence, installed PURE metadata, licensing and package
admission remain open for both profile rows.

## 2026-09-25 - MorphOS AddDataTypes shared-list transaction

Added a bounded resident MorphOS `AddDataTypes` slice using Utility's public
named-object API and Exec semaphore/list vectors. It reads the user-space
pointer from `NamedObject.no_Object`, releases the name reference before taking
the list lock, ensures the `binary`, `ascii`, `iff`, and `directory` built-ins,
and inserts their global `Node2` entries in case-insensitive order. The
candidate's pack-2 offsets are composed from public SDK `SignalSemaphore`,
`List`, `DataType`, and `DataTypeHeader` layouts plus source-observed private
field order; they are not yet verified against either packed executable in a
target guest.

The three-CPU resident receipt passes four supplied cases per CPU for
successful initialization, utility-open failure, absent named object, and a
null `no_Object` pointer. It checks named-object and semaphore release order,
all four typed-list insertions, global alphabetical order, initialized node and
header pointers, and zero writes to the command HUNK. Static reports show
fourteen reachable methods, no managed allocations, runtime helpers, external
targets, exception regions or fatal sites. Receipt:
`artifacts/adddatatypes-morphos-native-list-20260925-v1/qualification.json`.

This does not add a runnable CLI or Workbench command entry. `ReadArgs`, the
IFF `DTYP`/`DTHD`/`DTCD` path, FILES scanning, refresh, MorphOS LIST output, the
Workbench profile, original guest comparisons, PURE classification, licensing
and package admission remain open.

## 2026-09-25 - MorphOS AddDataTypes LIST output

Extended the shared-list resident slice to print the MorphOS source-shaped
`base-name, "name"` rows in sorted-list order. It checks Ctrl-C before each
row; on break it flushes `Output()` and calls `PrintFault(ERROR_BREAK)` while
leaving the command result level unchanged. The list walk runs while the caller
holds the datatype-list semaphore.

The refreshed resident 68000/020/040 qualification passes five supplied cases
per CPU: exact four-row built-in output, Ctrl-C before the first row, utility
open failure, missing named object, and null named-object user data. Static
closure is sixteen reachable methods with no managed/runtime helpers,
external targets, exception regions, fatal sites, leaks, or shared-image
writes. Receipt:
`artifacts/adddatatypes-morphos-native-list-20260925-v1/qualification.json`.

At this earlier checkpoint the temporary test root still bypassed `ReadArgs`
and only exercised the existing-list transaction. The later ReadArgs probe is
recorded below; it still does not provide the complete CLI or Workbench entry.
IFF descriptor loading, FILES matching, REFRESH/date handling, Workbench's
distinct profile, authoritative private-layout confirmation, guest parity,
installed PURE metadata, licensing and packaging remain open.

## 2026-09-25 - MorphOS AddDataTypes ReadArgs ownership and option mapping

Added a resident CLI qualification entry that uses the production
`NativeCommandArguments` helper with the MorphOS `FILES/M,QUIET/S,REFRESH/S,LIST/S`
template. The fixture verifies all four result slots, retains the DOS-owned
RDArgs/result-array lease through option use, and frees both before releasing
the datatype-list semaphore. Parser failure and result-array allocation failure
also verify cleanup and `IoErr` preservation. This remains a test entry: FILES
enumeration, REFRESH behavior, and actual CLI/Workbench startup are not wired.

At the time, the receipt at
`artifacts/adddatatypes-morphos-cli-list-20260925-v2/qualification.json` passed
twelve supplied invocations per CPU on resident 68000/020/040 HUNKs. That
directory is now marked invalidated: a later requalification overwrote its
68000 HUNK before failing a static reachable-method-count preflight.

## 2026-09-25 - MorphOS AddDataTypes library startup boundaries

The resident candidate now opens `utility.library` v37, `iffparse.library`
v37, `locale.library` v37 and `datatypes.library` v44 in the order observed in
the MorphOS reference. It closes acquired libraries in reverse order on both
normal completion and each partial-open failure, clears their published SDK
bases, and preserves the failing `IoErr` through cleanup. The existing ReadArgs
slot mapping, lease cleanup, built-in list initialization and LIST behavior
remain covered.

The current receipt
`artifacts/adddatatypes-morphos-cli-libraries-20260925-v3/qualification.json`
passes fifteen supplied invocations per CPU on resident 68000/020/040 HUNKs,
with 24 reachable methods and no managed allocations, runtime helpers,
external targets, exception regions, fatal sites, leaks or shared-image
writes. FILES matching, IFF descriptor loading/registration, DTCD segment
loading, REFRESH, real startup paths, private-ABI confirmation, guest parity,
PURE classification, licensing and package admission remain open.

## 2026-09-25 - MorphOS AddDataTypes FILES scanner no-match boundary

Added the explicit FILES scanning entry path through DOS
`ParsePatternNoCase`, `MatchFirst`, `MatchNext`, and `MatchEnd`. This slice
compiles the `.info`/`.backdrop` exclusion pattern, walks the null-terminated
ReadArgs pattern vector, prepares an AnchorPath, and balances matcher, current
directory, and temporary-buffer ownership. The new fixture returns
`ERROR_NO_MORE_ENTRIES` from `MatchFirst` and verifies `MatchEnd` plus buffer
cleanup. It does not claim a matched-file iteration, directory descent, or IFF
load result.

The qualification fixture had initially used `APTR.ReadUInt32` from host-side
test code, which CopperSharp lowers only inside compiled guest code. Replacing
that read with the fixture bus API removed the harness failure. The refreshed
receipt at
`artifacts/adddatatypes-morphos-files-iff-20260925-v4/qualification.json`
passes sixteen cases per CPU on resident 68000/020/040 HUNKs, with 37 reachable
methods and zero shared-image writes, managed allocations, runtime helpers,
external targets, exception regions, fatal sites, or resource leaks.

This is only the scanner no-match cleanup boundary. Matched-file traversal,
IFF `DTYP/DTHD` descriptor loading/registration, DTCD segment loading, refresh,
CLI/Workbench lifecycle, authoritative private ABI confirmation, Workbench
implementation, original guest comparison, PURE classification, licensing,
and package admission remain open. Shipping remains 0/200 commands and 0/246
profiles.

## 2026-09-25 - MorphOS AddDataTypes matched-file IFF EOC boundary

Extended the native fixture beyond the FILES no-match case. A synthetic match
now supplies a regular file and lock chain, checks case-insensitive exclusion
matching, enters and restores the current directory, opens the matched file,
allocates and configures an IFF handle, registers the `DTYP/DTHD` and
`DTYP/DTCD` properties plus `DTYP/DTTL` collection, and runs `ParseIFF` to the
reference-observed EOC. The fixture reports no stored DTHD property and checks
that the command closes the IFF, closes the DOS file, frees the IFF handle,
restores the directory and ends the matcher. It intentionally does not claim
descriptor registration or DTCD callback behavior.

The refreshed receipt at
`artifacts/adddatatypes-morphos-files-iff-20260925-v5/qualification.json`
passes seventeen supplied cases per CPU on resident 68000/020/040 HUNKs, with
37 reachable methods, zero shared-image writes, no managed/runtime helpers,
and balanced test resources. Directory descent, valid and malformed DTHD
registration, DTCD loading, REFRESH, actual CLI/Workbench startup, private ABI
confirmation, guest comparison, PURE classification, licensing and package
admission remain open. Shipping remains 0/200 commands and 0/246 profiles.

## 2026-09-25 - MorphOS AddDataTypes valid DTHD resident registration

Added a synthetic valid `DTYP/DTHD` file-header case after the successful IFF
EOC. The fixture supplies a bounded header with three inline strings, no mask,
and no DTCD segment. It verifies that the candidate copies header/string data
into its invocation-owned compound allocation, initializes the public header
and two list nodes, inserts the datatype into its type list, and adds Node2 to
the global sorted list. The case also confirms the `DTYP/DTCD` property query
returns absent, so no unsupported function-bearing descriptor is published.

The refreshed receipt at
`artifacts/adddatatypes-morphos-files-iff-20260925-v6/qualification.json`
passes eighteen supplied cases per CPU on resident 68000/020/040 HUNKs, with
37 reachable methods, zero shared-image writes, no managed/runtime helpers,
and balanced test resources. This validates the candidate's memory transfer
and registration path against the fixture's assumed layout; it does not
confirm the private layout against the authentic MorphOS library. Malformed
and duplicate headers, open-descriptor protection, DTCD loading, directory
descent, REFRESH, CLI/Workbench lifecycle, guest comparison, PURE, licensing
and package admission remain open. Shipping remains 0/200 commands and 0/246
profiles.

## 2026-09-25 - MorphOS AddDataTypes DTCD loader wiring

Added resident `M68kExport` callback adapters for `InternalLoadSeg`'s read,
allocate and free entries, with their declared DOS/Exec register contracts.
Each descriptor load now owns a copied public DTCD buffer and a separate
per-load cursor record, passes those callbacks and a stack-size cell to DOS,
publishes the resulting segment and entry address, and frees temporary state
or failed-load storage. The raw DOS LVO binding keeps this path free of
nullable/runtime helpers. Callback addresses come from `APTR.ExportAddress`;
no shared static callback state is used.

The refreshed receipt at
`artifacts/adddatatypes-morphos-files-iff-20260925-v10/qualification.json`
passes twenty command-fixture cases and one callback-wrapper probe per CPU on
resident 68000/020/040 HUNKs. The command root has 41 reachable methods and the
probe has five; both have no managed allocations/runtime helpers, external
targets, faults, leaks or shared-image writes. The command fixture checks DTCD
copy bytes, per-load state, resident callback addresses, `InternalLoadSeg`
register and stack arguments, successful segment/function publication, and
failed-loader cleanup. A second resident entry calls the production read,
allocate and free adapters through the SDK indirect-call wrappers and checks
partial reads, EOF, copied bytes and balanced allocation ownership. The probe
exposed a read-register mismatch: the callback now receives its buffer in A0
and byte count in D0 as published by the SDK. This remains synthetic CPU
fixture evidence, not execution through a real MorphOS DOS loader. Directory
descent, malformed and duplicate descriptors, open-descriptor protection,
exact `AROS_STACKSIZE`, real callback execution and unload behavior, REFRESH,
actual CLI/Workbench startup, the separate Workbench profile, private ABI
confirmation, guest comparison, PURE classification, licensing, package
admission, and shipping remain open.

## 2026-09-25 - MorphOS AddDataTypes REFRESH implementation slice

Implemented the MorphOS source's two-directory refresh path. It acquires
`DEVS:DataTypes/` and `MOSSYS:Devs/DataTypes/` read locks, releases the system
alias when `SameLock` reports the same object, checks dates in that same source
order, and scans changed directories with MOSSYS last. The `DateScan` behavior
matches the released source: if a directory has a first entry, an unchanged
stamp skips scanning and a changed stamp updates the shared list; an empty
directory leaves the stamp alone and is still scanned. The command saves,
hides and restores `pr_WindowPtr` around the refresh operation.

The refreshed three-CPU receipt at
`artifacts/adddatatypes-morphos-files-iff-20260925-v12/qualification.json`
passes twenty-three command-fixture cases and one callback-wrapper probe per
CPU on resident 68000/020/040 HUNKs. REFRESH cases cover same-lock alias
deduplication, unchanged-date skips, empty-directory scans, path order,
date-stamp updates and window-pointer restoration. The command root has 44
reachable methods; the callback probe has five. Both have no managed
allocations, runtime helpers, external targets, exceptions, fatal sites,
leaks or shared-image writes. The old v10 receipt is retained as its earlier
DTCD-only checkpoint.

The REFRESH evidence is a synthetic DOS fixture and does not establish guest
date behavior or the candidate private list ABI. Actual CLI/Workbench process
lifecycle, directory descent and descriptor replacement behavior, malformed
and open descriptors, MorphOS loader callback execution, exact
`AROS_STACKSIZE`, Workbench parity, authentic guest comparison, PURE, rights
and package admission remain open. Shipping stays at 0/200 commands.

## 2026-09-25 - MorphOS AddDataTypes first-directory matcher cases

Extended the resident DOS fixture for the next open FILES boundary. It now
returns a directory from `MatchFirst`, verifies the command requests entry
with `APF_DODIR`, then returns a descriptor file from `MatchNext`. A second
case starts with `APF_DIDDIR` set and checks that the command clears it without
adding another directory-entry request, while preserving an unrelated anchor
flag. Both paths continue through descriptor loading and matcher cleanup.

The refreshed receipt at
`artifacts/adddatatypes-morphos-files-iff-20260925-v13/qualification.json`
passes twenty-five command-fixture cases and one callback-wrapper probe per
CPU on resident 68000/020/040 HUNKs, with 44 command methods, five probe
methods, no managed/runtime helpers or external targets, no leaks, and no
shared-image writes. This verifies only these controlled matcher transitions;
the full matcher-mediated nonrecursive traversal and original guest path order
remain open. No shipping count changes.
## 2026-09-25 - Workbench AddDataTypes list-head ABI cross-check

Extracted the Workbench 3.1 `Libs/DataTypes.library` from the selected M10
Workbench disk in memory after validating the archive and ADF identities. The
15,592-byte HUNK is hash-bound and identifies itself as `DataTypes 40.6
(25.8.93)`. The hash-bound Workbench `C:AddDataTypes` HUNK looks up the Utility
named object `DataTypesList`, dereferences its `no_Object`, and passes list
addresses at byte offsets 46, 60, 74, 88 and 102 through the built-in setup
path. Their association is the sorted, binary, ASCII, IFF and misc lists,
respectively. This confirms those five list-head offsets for Workbench and
matches the candidate assumptions for those fields. The directory
registration path also reads and writes the shared list's first refresh-date
longword at offset 120, then derives the sorted-list head from the same
pointer, confirming the date-field location independently.

Recorded the binary identities, Resident/init metadata, HUNK ranges and
bounded offset findings in
`reference-captures/workbench-datatypes-library-abi-audit-20260925.json` and
updated the AddDataTypes contract and goal checkpoint. Separately bound the
exact MorphOS 3.20 ISO's packed datatype library identity in
`reference-captures/morphos-datatypes-library-binary-audit-20260925.json`.
This is partial static ABI evidence only. It does not prove the longest-mask
field, CompoundDatatype layout, locking or segment ownership, or guest parity.
The MorphOS packed implementation still needs ABI comparison.
Workbench implementation, guest qualification, PURE/resident classification,
licensing and package admission remain open; shipping stays at 0/200 commands
and 0/246 profiles.

## 2026-09-25 - MorphOS AddDataTypes production orchestration

Moved the MorphOS CLI orchestration into the production command body. `Run`
now owns library acquisition, shared-list acquisition and semaphore lifetime,
built-in setup, `ReadArgs` ownership, option dispatch, and reverse cleanup.
`RunWorkbenchStartup` owns the matching Workbench startup processing and
cleanup; the external entry retains responsibility for DOS startup and the
Workbench message receive/reply boundary. Refactored the resident test entry
to delegate to those production methods rather than duplicating the command
flow.

The refreshed qualification at
`artifacts/adddatatypes-morphos-cli-run-20260925-v18/qualification.json`
passes 38 command-fixture cases and one callback-wrapper probe on each of
68000/020/040 resident HUNKs. The command root has 48 reachable methods. All
images have zero shared-image writes, managed allocations, runtime helpers,
external targets, exception regions, fatal sites or fixture leaks. This binds
the production orchestration to the existing CLI/Workbench startup vector
matrix; it remains fixture evidence, not guest parity or shipping admission.
The exact Workbench command body, original MorphOS guest behavior, real DOS
loader callback and unload behavior, PURE/resident classification, licensing
and packaging remain open. Shipping stays at 0/200 commands and 0/246 profiles.

## 2026-09-25 - MorphOS AddDataTypes segment lifetime vectors

Extended the production `AddDataTypes` resident fixture around descriptor
replacement and DTCD ownership. A closed segmented descriptor is unloaded
before its record is freed when replaced. An open segmented descriptor remains
registered and keeps its existing segment; if the incoming descriptor also
loads code, only that discarded candidate segment is unloaded. A matching
same-descriptor update likewise unloads only the newly loaded candidate. The
fixture checks DOS `UnLoadSeg` arguments, retained old segment pointers, and
the order of copied-DTCD cleanup, segment unload, and descriptor release.

The refreshed receipt
`artifacts/adddatatypes-morphos-segment-lifetime-20260925-v19/qualification.json`
passes 41 command vectors plus one callback-wrapper probe per CPU on resident
68000/020/040 HUNKs, with 48 reachable methods, no shared-image writes, and no
fixture leaks. These supplied vectors exercise the replacement's ownership
policy; they do not verify MorphOS's real loader/unloader behavior or prove
guest safety. Exact `AROS_STACKSIZE`, real callback invocation, private ABI,
guest comparison, PURE, licensing and package admission remain open.

## 2026-09-25 - AddDataTypes profile-specific embedded loader stacks

The Workbench 3.1 HUNK's two `InternalLoadSeg` call paths now have explicit
static stack evidence: each initializes its stack-size cell to 4096, including
the embedded-DTCD call at code offset `0x0d62`. The released MorphOS source
uses `AROS_STACKSIZE` for embedded DTCD and uses 4096 separately for its
external function-name loader. The pinned official AROS target header at
commit `d09193345ec4f63bc152730bd576a0469f50282d`,
`arch/ppc-morphos/include/aros/cpu.h` (Git blob
`ca7cdb6bdf4b78b0a783400d64da47c7cde0d059`), sets the MorphOS target value to
32768. The exact historical header-to-binary correspondence for the 2004
MorphOS command remains unproven, and the packed MorphOS executable's wrapped
ELF code was not statically disassembled.

The native candidate now carries a profile-selected DTCD loader stack size
through FILES scans, REFRESH scans, and Workbench startup: 32768 for MorphOS
embedded code and 4096 for Workbench 3.1. New supplied-vector cases cover both
profiles and the MorphOS Workbench-startup path. The MorphOS receipt
`artifacts/adddatatypes-morphos-profile-stacks-20260925-v20/qualification.json`
passes 42 command cases plus one callback-wrapper probe per CPU on resident
68000/020/040 HUNKs. The Workbench receipt
`artifacts/adddatatypes-workbench31-native-20260925-v6/qualification.json`
passes fifteen vectors per CPU. Both verify zero shared-image writes and no
fixture leaks. They do not establish original guest parity, historical header
identity, real DOS-loader callback/unload behavior, PURE, licensing, or package
admission. Evidence is recorded in
`reference-captures/adddatatypes-stacksize-audit-20260925.json`.

## 2026-09-25 - MorphOS LoadMonDrvs EXCEPT and directory vectors

Corrected the supplied-vector fixture so successful `MatchFirst` calls expose
their matched `FileInfoBlock` entry whether or not `EXCEPT` was supplied. Added
vectors proving that the candidate skips an actual matching name
case-insensitively, allows a different name through `EXCEPT` while using an
alternate `FROM` directory, and skips a matched directory before calling
`LoadSeg`. The alternate-directory case also checks `NameFromLock` and the
final driver path.

The refreshed qualification
`artifacts/loadmondrvs-morphos-native-entry-20260925-except-directory-v6/qualification.json`
passes sixteen supplied DOS/Exec vectors per CPU on resident 68000/020/040
HUNKs. The root has 22 reachable methods, zero managed runtime features or
helpers, external targets, exception regions or fatal fault sites, zero
shared-image writes and no fixture leaks. These vectors improve candidate
coverage only; packed parser, authentic guest ordering/effects, diagnostics,
PURE, lifecycle, rights and package gates remain open.

## 2026-09-25 - MorphOS LoadMonDrvs multi-entry scan

Extended the matcher fixture to supply multiple entries through successive
`MatchNext` calls. The new case returns the excluded `PAL` entry followed by
`NTSC` and verifies that the replacement skips only the first, builds the
second driver's path from the selected directory, loads the second segment,
finds its resident, initializes it, and finishes the matcher lifecycle.
Provider assertions and expected call counts now account for every processed
entry and early-stop failure.

The refreshed receipt
`artifacts/loadmondrvs-morphos-native-entry-20260925-multiple-match-v7/qualification.json`
passes seventeen supplied DOS/Exec vectors per CPU on resident 68000/020/040
HUNKs with 22 reachable methods, no runtime helpers/features or external
targets, no leaks, and zero shared-image writes. These vectors still require
original MorphOS guest comparison before parity or shipping can be claimed.

## 2026-09-25 - Workbench SetFont console protection window

Inspected the selected, hash-bound Workbench 3.1 `C/SetFont` HUNK and recorded
the code-section offsets for `Exec.Forbid`, graphics `SetFont`, conditional
old-font `CloseFont`, publication of the new font at `Window.Font` offset
`0x80`, and `Exec.Permit`. The native resident candidate now preserves that
sequence. The fixture checks the protected replacement and verifies that
pre-install failure paths do not enter the protection interval.

The refreshed qualification
`artifacts/setfont-wb31-native-entry-20260925-forbid-permit-v5/qualification.json`
passes sixteen supplied vectors per CPU on resident 68000/020/040 HUNKs. Each
image has nineteen reachable methods, zero managed allocations/runtime
features/helpers/external targets/exception regions/fatal sites, zero shared
image writes, and no fixture leaks. The binary-derived disassembly audit is
`reference-captures/setfont-console-forbid-permit-audit-20260925.json`.

This closes only the replacement-side ordering slice. Original Workbench guest
concurrency and output, handler-specific packet/result behavior, parser and
diagnostic parity, PURE/resident lifecycle, rights, package, and differential
gates remain open. No command or profile shipping status is promoted.

## 2026-09-25 - Workbench SetFont style-output gating

Bounded disassembly of the selected Workbench `C/SetFont` HUNK shows that the
command reads the opened font's `tf_Style` byte before writing terminal style
escapes. Requested italic, bold, and underline escapes are skipped when their
corresponding font bits are already set. The exact string bytes, code offsets
and branches are recorded in
`reference-captures/setfont-style-output-audit-20260925.json`.

Updated the resident candidate to use the opened font style and added vectors
for each pre-styled request plus a mixed case. The refreshed receipt
`artifacts/setfont-wb31-native-entry-20260925-style-gating-v6/qualification.json`
passes twenty vectors per CPU on 68000/020/040. Each HUNK has nineteen
reachable methods, zero managed allocation/runtime feature/helper/external
target/exception/fatal-site findings, zero shared-image writes, and no fixture
leaks.

The vectors validate replacement branches against static HUNK evidence only.
Original guest rendering, handler-specific packet/result behavior, output
failures, PURE/resident lifecycle, rights, package, and differential gates
remain open.

## 2026-09-25 - Workbench SetFont ACTION_DISK_INFO window lookup

The binary's action-25 call is `ACTION_DISK_INFO`, with D3 holding the BPTR of
an `InfoData` structure. After the call, the HUNK reads `InfoData+0x1c` as a
raw `Window*`; a NULL value branches to success without changing the font or
writing output. For a Window, it passes `Window.RastPort` to `SetFont` and
closes/replaces `Window.Font`. This matches the public DOS reference's
description of action 25 and its console-specific InfoData fields, while the
selected Workbench handler still needs guest confirmation.

Replaced the candidate's earlier DosPacket/result-slot model with an
`InfoData` buffer, `id_VolumeNode` Window lookup, and the public Window field
offsets. Added supplied cases for absent windows and a false `DoPkt` result
with a returned window, verifying the original's use of the field rather than
the D0 result. The refreshed receipt
`artifacts/setfont-wb31-native-entry-20260925-console-window-v7/qualification.json`
passes twenty-one vectors per CPU on resident 68000/020/040 HUNKs. Each has
nineteen reachable methods, no managed allocations/runtime features/helpers,
external targets, exception/fatal sites, leaks, or shared-image writes.

The static interpretation is recorded in
`reference-captures/setfont-console-window-audit-20260925.json`. Original
Workbench handler behavior, Window lifetime, output/result behavior, guest
parity, PURE/resident lifecycle, rights, package, and shipping admission remain
open.

## 2026-09-25 - Workbench SetFont PROP font validation

The HUNK tests the `PROP` ReadArgs result after `OpenDiskFont`. With PROP set,
the proportional-font check is skipped. Otherwise, if bit 5 of the returned
`TextFont.Flags` is set, the original closes that font and fails with
`ERROR_OBJECT_WRONG_TYPE` (212). The byte offset, flag mask, and branch offsets
are recorded in
`reference-captures/setfont-proportional-font-audit-20260925.json`.

Added the same check to the resident candidate and vectors for default
rejection and explicit PROP acceptance. The refreshed receipt
`artifacts/setfont-wb31-native-entry-20260925-prop-font-v8/qualification.json`
passes twenty-three vectors per CPU on 68000/020/040 HUNKs, with nineteen
reachable methods, no managed allocations/runtime features/helpers/external
targets/exception/fatal sites, no fixture leaks, and zero shared-image writes.

This follows the selected HUNK's font acceptance path, but actual diskfont
selection and diagnostics still need Workbench guest comparison. The other
SetFont guest, lifecycle/PURE, rights, package, and differential gates remain
open.

## 2026-09-25 - Workbench SetFont result policy

The result-policy audit records the return and IoErr branches recovered from
the selected HUNK: `ReadArgs` failure and sizes at or below four return
`RETURN_FAIL`; OpenDiskFont failure and a missing console task report error
205; a proportional font without `PROP` is closed and reports error 212; a
missing console Window is a successful no-op; and completed output returns
success without testing `PutStr` or `Flush` results. The hash-bound observations
are in `reference-captures/setfont-result-policy-audit-20260925.json`.

The candidate preserves those result paths. The refreshed receipt
`artifacts/setfont-wb31-native-entry-20260925-result-policy-v11/qualification.json`
passes twenty-three supplied vectors per CPU on resident 68000/020/040 HUNKs.
Each image has eighteen reachable methods, no managed allocation sites,
runtime features/helpers, external native targets, exception regions or fatal
machine-fault sites, and zero shared-image writes. The previous v8 receipt is
retained as historical evidence.

This remains candidate behavior checked against static HUNK observations and
supplied fixtures. Original Workbench handler effects, output side-effects,
PURE/resident lifecycle, rights, package admission, and guest differential
comparison remain open; no command or profile shipping status is promoted.

## 2026-09-26 - Workbench SetFont output-failure result

Added a supplied vector where every `PutStr` and `Flush` call reports failure,
including the reset and italic escapes. The Workbench HUNK does not inspect
those return values and still completes with success; the candidate and fixture
now preserve that result. The three-CPU receipt
`artifacts/setfont-wb31-native-entry-20260926-output-failure-v12/qualification.json`
passes twenty-four supplied invocations per resident 68000/020/040 image, with
eighteen reachable methods, no managed allocations/runtime features/helpers,
external native targets, exception/fatal sites, leaks, or shared-image writes.
Original guest output-handler behavior and the remaining SetFont admission
gates remain open.

## 2026-09-26 - Workbench SetFont TextAttr flag options

Disassembly of the selected HUNK resolves the two remaining `TextAttr` flag
switches: the default `ta_Flags` is `0x43`; `PROP` adds
`FPF_PROPORTIONAL` (`0x20`); `SCALE` clears `FPF_DESIGNED` (`0x40`). The
four combined values, result-slot accesses and code offsets are hash-bound in
`reference-captures/setfont-textattr-flags-audit-20260926.json`. Added a
`SCALE`-only vector independently of the existing `PROP` and combined case.

The refreshed resident receipt
`artifacts/setfont-wb31-native-entry-20260926-textattr-flags-v13/qualification.json`
passes twenty-five supplied cases per 68000/020/040 image, with eighteen
reachable methods, zero managed allocations/runtime helpers/features/external
targets/exception/fatal sites, zero shared-image writes, and no fixture leaks.
Actual diskfont selection and Workbench guest behavior remain unverified.

## 2026-09-26 - CC02 Shell/command semantic baseline

Ran `dotnet test tests/Commands/CopperOS.Commands.Tests.csproj --verbosity minimal`
from CopperOS HEAD `5dc8e7dde1787b85ceebd1379587921b6bc01980`; the build succeeded
and all 704 tests passed with zero failures or skips. The current test assembly
hash and environment are recorded in `baseline-and-api-audit.md`. This protects
the existing Shell and managed command behavior while C: command work continues;
it does not qualify external command completeness, native `ReadArgs`, resident
purity, or original guest parity.

The existing Shell native artifacts are from 2026-08-16. Its current qualifier
removes SDK copies from sibling CopperStart output directories and overwrites
the existing HUNK/listing outputs, so those shared outputs were left intact.
An isolated refresh now records the static build at
`artifacts/shell-native-isolated-20260926-v6/qualification.json`: 2,247
reachable methods per target; zero managed allocations, runtime features,
runtime helpers, external targets, or exception regions; and three fatal
machine-fault sites per target. The 68000 output is a HUNK; 020/040 are
assembler listings. The receipt confirms sibling SDK copies and previous
Shell artifacts were not changed. This is not guest execution or release
qualification. The 020 listing shows the three sites are guarded
divide-by-zero `ILLEGAL` instructions in `StackCommand.WriteStackSize`. The
divisor loop appears to prevent those traps from being reached, but static
evidence does not establish guest behavior. CC02 remains open for the
API/ownership reconciliation and behavioral evidence.

## 2026-09-26 - Workbench SetFont SIZE/N low-word behavior

Disassembly of the hash-verified 1,092-byte Workbench SetFont HUNK shows the
command loads the `SIZE/N` LONG, stores its low word to `TextAttr.ta_YSize`, and
then compares that word with four using unsigned `CMP.W`/`BLS`. The candidate
had checked the full 32-bit value, which differed for values whose low word
wraps to 0 through 4. It now masks to the low word before the comparison and
store. The audit is recorded in
`reference-captures/setfont-size-word-audit-20260926.json`.

Added supplied ReadArgs-result vectors for raw values `65536`, `65540`,
`65541`, and `0xFFFFFFFF`. They verify low words 0 and 4 return `RETURN_FAIL`
with error 115, while low words 5 and `0xFFFF` continue to OpenDiskFont. The
refreshed receipt
`artifacts/setfont-wb31-native-entry-20260926-size-word-v16/qualification.json`
passes 29 invocations per CPU on resident 68000/020/040 HUNKs, with 18
reachable methods, no managed allocations/runtime features/helpers/external
targets/exception/fatal sites, no fixture leaks, and zero shared-image writes.
This proves the replacement's HUNK behavior against the captured word
operations and supplied vectors; actual DOS text parsing, Workbench diskfont
behavior, guest parity, PURE/resident lifecycle, rights and package admission
remain open.

The managed command regression suite was rerun after this candidate change:
704 passed, zero failed or skipped. Its assembly hash remains
`14b0c932a1013f8b2e3cd571020fbabaebfe2d6843a238bab893437badeb84db`.

## 2026-09-26 - Workbench SetFont allocation-failure cleanup

Added four resident vectors that fail allocation of the ReadArgs result array,
font name, `TextAttr`, and `InfoData` in turn. The fixture checks each failure's
result/error path, that parsing does not begin when the result array allocation
fails, that `FreeArgs` is paired after successful parsing, and that every
successful allocation is released before return. The refreshed receipt
`artifacts/setfont-wb31-native-entry-20260926-allocation-cleanup-v19/qualification.json`
passes thirty-three invocations per CPU on resident 68000/020/040 HUNKs, with
18 reachable methods, no managed allocation sites/runtime features/helpers,
external targets, exception/fatal sites, leaks, or shared-image writes. These
are supplied allocation and DOS-vector results; Workbench low-memory behavior,
guest parity, PURE/resident lifecycle, rights and package admission remain
open.

After changing the SetFont qualifier's expected vector count, the qualification
script regression suite also passed all 37 cases, including its fail-closed,
source-drift and HUNK-tail checks.

## 2026-09-26 - Workbench SetFont font ownership

Static disassembly of the hash-bound HUNK shows that the successful
no-console-window path retains the `OpenDiskFont` result through shared
cleanup without calling `CloseFont`. The candidate fixture now explicitly
checks that it opens once, does not close, and does not call `SetFont` on this
path. The audit also records post-open failure branches that leave the font
open in the original; the candidate instead closes an untransferred font on
failure to avoid a leak. This is a deliberate cleanup difference, not verified
guest parity. The source-bound audit is
`reference-captures/setfont-font-ownership-audit-20260926.json`.

The refreshed resident receipt
`artifacts/setfont-wb31-native-entry-20260926-font-ownership-v20/qualification.json`
passes 33 supplied invocations per 68000/020/040 image, with 18 reachable
methods, no runtime features/helpers/external targets/exception/fatal sites,
no resource leaks, and no shared-image writes. Original Workbench execution is
still needed to measure real diskfont ownership and to compare the cleanup
difference. PURE/resident, rights, package and differential gates remain open.

## 2026-09-26 - Workbench LoadResource request and hook slices

Added `NativeWorkbench31LoadResourceProtocol` for the source-bound 54-byte
synchronous caller/server message. Its packed fields preserve the current
directory, input/output handles, `NAME/M,LOCK/S,UNLOCK/S` result slots, reply
port, result and `IoErr`. It sends the stack-owned message through the caller's
process port and keeps the DOS `ReadArgs` lease caller-owned across the wait.
The offsets are recorded in
`reference-captures/loadresource-wb31-request-protocol-audit-20260926.json`.

`NativeWorkbench31LoadResourceLoadSeg` implements the captured 16-byte cache,
`SameLock` lookup, one-shot SegList transfer, unmatched saved-vector call,
record removal/unload, and empty/idle teardown that preserves a later
`SetFunction` replacement. Worker state is Exec-allocated and published in its
process `tc_UserData`; no mutable static data is required by the resident
runtime profile.

The qualification script compiles both bounded components to 68000/020/040
HUNKs. Receipt
`artifacts/workbench31-loadresource-hook-static-v4/qualification.json`
passes with 18 reachable methods per CPU and no runtime helpers, external
native targets, exceptions or fatal sites. It does not run the client/server
transaction or hook, implement ReadArgs orchestration/worker startup/resource
actions, prove that the code HUNK remains loaded, or establish guest behavior.
The command remains unchecked; the file's P-clear protection classification
is unchanged.

## 2026-09-26 - Workbench LoadResource request layout executable check

Historical, superseded below: added a host-side mode to
`Commands.NativeExecution` for the then-current
`NativeWorkbench31LoadResourceProtocol.RequestMessage`. It checked
the 54-byte size and all twelve source-bound offsets: `Node`, reply port,
length, flags, result, `IoErr`, current directory, input, output, and the three
`ReadArgs` result slots through the `loadresource-protocol-layout` mode.

The check passed, but was later removed after native execution contradicted
its layout result; that mode is no longer supported.
It was a marshalling/layout assertion only; it did not
exercise `PutMsg`/`WaitPort`, worker startup or shutdown, the installed
`LoadSeg` hook, or Workbench behavior. The next entry records the native tests
and the remaining command/lifecycle gates.

## 2026-09-26 - Workbench LoadResource native requests and hook ownership

Actual HUNK execution found two defects missed by the static/host checks.
The C# `Pack=2` message put `ReplyPort` at native byte 20 rather than 14;
the exported hook began with an empty DOS library-base slot in its independent
resident context. The production client now reserves fourteen stack LONGs
and writes/reads the captured 54-byte protocol with explicit APTR offsets.
The export now binds its DOS slot to the worker-retained base and restores it
before returning. The host-only layout struct and fixture were removed.

Hook ownership review also fixed teardown beneath a newer `SetFunction`
replacement: it restores that newer vector and keeps its own state/DOS
reference alive, returning false. Direct duplicate installation preserves the
existing state instead of saving its own entry as a recursive predecessor.
Null-predecessor rollback happens before state publication. Registry publication
is protected by `Forbid`. Corrected source disassembly moved filesystem `Lock`
before semaphore acquisition and changed teardown to `AttemptSemaphore`;
a busy attempt returns without freeing state or releasing an unowned semaphore.

Native receipts now pass on 68000/020/040:

- `artifacts/workbench31-loadresource-protocol-runtime-20260926-v4/qualification.json`:
  nineteen supplied client transactions per CPU, including two interleaved
  callers, exact stack message fields, parser-pointer lifetime, returned
  LONG results/errors, null borrowed handles, rejected boundaries and cleanup.
- `artifacts/workbench31-loadresource-hook-runtime-0b0711c4c4f64e9da258f57bbdc56978/qualification.json`:
  eleven hook scenarios per CPU, including alias matching and one-shot
  ownership transfer, saved-vector calls, record removal, duplicate rejection,
  allocation/lock failures, newer-patch retention, null rollback and busy retry.

The generated images have no managed allocation sites, runtime helpers or
features, external native targets, exception regions or fatal sites. Executed
paths report no shared-image writes, ownership leaks or damaged stack guards;
hook calls also preserve the public export's callee-saved registers.

The independent source audit corrected catalog ID/string pairing and function
offsets, and recorded exact `CreateNewProc` tags and detached multi-HUNK
ownership in `reference-captures/loadresource-wb31-worker-startup-audit-20260926.json`.
The original primary worker queues its first real request; it does not send
a separate startup acknowledgment. Its teardown helper returns one
unconditionally despite conditional vector restoration, so static control
flow does not prove safe unloading. These corrections replace earlier audit
assumptions, not original-guest observations.

Next: implement full client/worker orchestration and opened-resource actions
using those source contracts, with explicit segment ownership and tests for
worker startup/exit and cross-call persistence. The active hook counter alone
does not protect callback prologue/epilogue code from unloading. Original
guest parity, command completion, lifecycle, rights and package admission stay
open; `CC20.LoadResource.wb31` is still partial with its source PURE bit clear.

## 2026-09-26 - LoadResource opened resources and worker dispatch

The preceding status-only turn made no implementation progress. This follow-up
integrated the production registry, resource actions/messages and single-
request worker in the native probe project, fixed an unsupported IL argument
assignment by using a local NAME/M cursor, and ran native qualifications.

- Registry: ten cases per 68000/020/040; receipt
  `artifacts/workbench31-loadresource-registry-runtime-20260926-v2/qualification.json`.
- Actions/messages: thirty-two cases per CPU; receipt
  `artifacts/workbench31-loadresource-actions-runtime-20260926-v2/qualification.json`.
- Worker dispatch/receive: twenty-six cases per CPU; receipt
  `artifacts/workbench31-loadresource-worker-runtime-20260926-v5/qualification.json`.

The actions use the production installed hook/cache/registry with supplied OS
responses. They preserve device-only segment caching, library/font/catalog
handle ownership, classification quirks, source diagnostics and error sampling
before or after output as appropriate. Library/font provider callbacks do not
recursively load segments in this fixture. The independent source audit is
`reference-captures/loadresource-wb31-resource-actions-audit-20260926.json`.

The worker executes 54-byte requests, lists retained resources, matches NAME/M
patterns using public DOS calls, gives UNLOCK precedence, interprets LOCK's
low word, stops after the first error, restores borrowed context before reply,
and preserves sender-owned pointer arrays and strings. Independent fixture
review added MatchNext-count checks, MatchEnd-before-PrintFault ordering,
catalog ID/default validation and before/after source-hash binding. Requests
run with the original 3000-byte stack; this does not prove the future full
coordinator's maximum stack requirement. The first unsupported-argument
compiler receipt and intermediate receipts remain preserved.

Worker/actions admit only the exact native nullable-BPTR GetValueOrDefault
intrinsic from public DOS.LoadSeg. Their static reports contain no managed
allocations, runtime helpers, external native targets, exception regions or
fatal sites; runtime checks show no shared-image writes. The production native
project builds with zero errors and the existing seventeen Version CS0649
warnings. The contract, goal, ledger and qualification report were updated.

A separate bounded audit improved the runner blocker evidence: the existing
clean CopperScreen Lightweight binary completes 2,000 frames with the
available 40.63 ROM and Workbench disk, without host DOS services or a reported
unsupported feature. Its 35 passive snapshots cannot inspect ExecBase in slow
RAM (`0x00C00B00`), and its final display is striped/garbled. The capture is
recorded in `reference-captures/workbench31-clean-runner-readiness-20260926.md`.
It replaces the overly broad claim that this runner cannot load KS3.1; it
does not establish supported 3.1 operation, DOS/Shell readiness or any command
parity. Next infrastructure work is passive slow-RAM visibility, followed by
readiness inspection and an original-guest command/output/IoErr protocol.

Next command work remains full process startup/exit coordination, correct
message-catalog lifetime and explicit retained-code ownership. Failed font
size parsing also remains a documented fidelity difference. The full
200-command/246-profile goal stays active and unshrunk; these component
receipts do not grant PURE, guest, rights or package admission.

## 2026-09-26 - LoadResource launch/catalog integration and clean guest visibility

The previous turn made concrete implementation/test progress. This turn added
the production client launch transaction and a strict two-HUNK packaging tool,
implemented the source's lazy catalog helper lifetime, corrected service-port
identity, and resolved slow-RAM visibility in the clean guest path.

Launch now finds an existing service or detaches the independent worker tail
before public CreateNewProc with NP_Seglist/FreeSeglist. Failure restores the
chain and preserves IoErr before PrintFault; success sends the real resource
request while keeping ReadArgs alive. Its 45 native cases pass across
68000/020/040 (`workbench31-loadresource-launch-runtime-20260926-v2`).
The source-compatible process choices remain, but NP_Seglist ownership is an
explicit replacement architecture for the original self-unloading worker.
Full child execution and code lifetime are not qualified by supplied process
responses or fixture headers.

The new client/catalog audit confirms that the original close helper leaves
its catalog pointer unchanged, including repeated nested closes during
listing. Messages and Worker now preserve those calls with four bytes of
caller-owned state. Actions capture errors after the diagnostic's CloseCatalog
where required. Hook discovery now uses original Latin-1 `« LoadResource »`.
Current fresh receipts pass Hook 33, Actions 102 and Worker 93 cases:
`workbench31-loadresource-hook-runtime-service-name-20260926-v1`,
`workbench31-loadresource-actions-runtime-20260926-v6`, and
`workbench31-loadresource-worker-runtime-20260926-v7`, all under `artifacts/`.
Source and artifact hashes are recorded; there are no shared-image writes.
The production native project builds with zero errors and existing warnings.

The packer passes ten independent Python tests and structurally combines three
actual probe pairs without modifying CODE bytes. These are explicitly
non-installable, non-runnable command probes. The new ownership design records
the remaining callback prologue/epilogue hazard and the need to wake the worker
when another task consumes the last cached device. Full client/child entry,
startup races and retirement remain next command work.

Using the amiga-cycle-exact observer workflow, the sibling CopperScreen engine
gained only a read-only SlowRam property plus one passing observer regression.
A CopperOS-owned isolated runner performs passive capture and offline analysis.
Its 2,000-frame run preserves baseline CPU/output/cycle fingerprints and all
70 chip-RAM/BMP snapshots. It now sees original Exec/DOS and Initial CLI startup
progress through Workbench, without host OS services or guest/media changes.
See `reference-captures/workbench31-clean-runner-slowram-readiness-20260926.md`
for source/PDB/media bindings and exact receipts.

Next infrastructure work is an authored probe launched normally from a
disposable Workbench image before EndCLI, with exact output/result/IoErr in
guest-owned RAM. A read-only audit confirmed installed amitools can insert it;
no disk was modified. The clean runner's guest disk writes remain unsupported.
Guest parity, the full command inventory, PURE/resident admission, rights and
packaging remain open; the complete goal remains active.


## 2026-09-26: normal Workbench command execution and passive result capture

Implemented an authored native CLI probe, a separate hash-bound copied-ADF
builder and a passive record analyzer/capture wrapper. Preparation preserves
original media, boot/root blocks, unrelated payloads and metadata, filesystem
allocation and raw-block boundaries. Independent review exposed missing slack/
free-block mutation rejection; added strict masks and tamper regressions before
using a real derivative. Source bitmap padding and root residues are preserved,
not repaired. All 24 focused tooling checks pass.

Four fresh original-Workbench boots now capture C:Version through normal
startup, original ReadArgs, SystemTagList and the RAM handler. Normal/minimum39
return 0, minimum999 returns 5 with unchanged version output, and bad numeric
input returns 20 with the Shell failure diagnostic. Every run has unchanged
before/after inputs and a stable public-port record through the final frame.
See `reference-captures/workbench31-guest-command-probe-20260926.md` and aggregate
`artifacts/workbench31-guest-command-captures-20260926-v1/qualification.json`.

No emulator behavior was changed. Guest DF0 writes remain unsupported; this
route uses RAM output. Captured post-System IoErr is explicitly caller state,
not generally established child Result2 propagation. Separate stderr, generated
replacement comparisons, production teardown, fault cases and MorphOS/CopperStart
execution remain open. The full 200-command/246-profile shipping ledger remains
0 admitted; the active goal is unchanged. These captures make the next action
command behavior correction and replacement comparison rather than more boot
presence checks.


### 2026-09-26 Workbench Version comparison and output correction

The original binary audit and normal guest captures establish minimum-major
comparison with output retained on warnings. The candidate's existing supported
system/RES paths now use signed minimum comparisons, including revision-only
requests and ignoring revision when the actual major is greater. Default named
lookup was not replaced with a LibList shortcut; its original provider ordering
remains required work.

The same review corrected two VPrintf ABI mistakes: system output now consumes
the supplied name before numeric LONGs, and FULL output supplies its second
string in the actual second LONG slot. Earlier fixture shortcuts masked those
errors; the fixture now reads the correct consecutive slots.

`artifacts/version-wb31-native-20260926-comparison-v1/qualification.json` passes
32 invocations per CPU on 68000/020/040, 96 total, with 17 reachable methods,
zero shared-image writes/leaks and no managed runtime dependencies. Twelve
source hashes and 33 compiled-input identities are unchanged. Independent code
review found no scoped regression. See
`reference-captures/version-wb31-named-comparison-audit-20260926.json` and the
Version contract for source offsets and scope.

These remain supplied-vector checks of the replacement. The four real guest
captures executed only original Version. Default named/file/unit/internal
providers, complete classic system/RES output and parsing, secondary-error
semantics, original/replacement guest comparison, resident/PURE lifecycle,
licensing and packaging remain open. Shipping admission is unchanged.


### Workbench Resident lookup and ten replacement guest pairs (2026-09-26)

The Workbench candidate now implements the original first named provider: a
case-insensitive walk of the full-name Exec Resident table, including high-bit
continuation pointers, canonical rt_Name, authoritative rt_Version and parsed
signed revision (missing revision is -1). It does not skip ahead to LibList
when this provider misses. The original parses UNIT/INTERNAL/RES but does not
read those slots; only FILE changes search flags. Their syntax is still parsed
by public DOS ReadArgs. Numeric parse failure now returns the original level 20.

FULL now uses the source's DOS StrToLong, Utility Date2Amiga and DOS StrToDate/
DateToStr behavior: low-word day/month/year+1900, signed DateStamp arithmetic,
format 0 fallback, format 4 output and the original first-line Extra placement.
Temporary state belongs to the invocation; the optional Extra copy is freed.
The [FULL/options audit](reference-captures/version-wb31-full-options-audit-20260926.json)
records the original offsets and separately captured results.

`artifacts/version-wb31-native-20260926-resident-first-v2/qualification.json` passes **58 cases per CPU, 174 total**, on 68000/020/040, including
two overlapping named invocations. All images have 23 reachable methods, no
managed dependencies, leaked fixture resources or shared-image writes.
Thirteen source and 33 compiled-input identities were rechecked after the runs.
The 68000 HUNK is 6,912 bytes, SHA-256
`143af192988613f3b29449cb3b095fcd3389b90c924e330f7ab8f8d21603fec7`.

`artifacts/workbench31-guest-version-pairs-20260926-v2/qualification.json` binds **ten exact original/replacement guest pairs** for ordinary
and uppercase dos.library, RES and uppercase RES, FULL, lower/higher requested
major, revision-only warning, higher-major/large-revision success, and invalid
numeric input. Each 2,400-frame run uses a fresh external derivative, original
Kickstart 3.1/DOS and the frozen passive runner. Exact output, return and caller
post-System IoErr agree; saved snapshots and candidate sources/compiled inputs
were rechecked. Independent review re-decoded all ten pairs and found no
admission or comparison errors. The original C/Version protection word 0
(P clear) is preserved; the replacement is experimental, not promoted to PURE.

The earlier candidate's ordinary-name mismatch and RES match remain retained
as negative/positive controls. All 39 builder/analyzer/comparator tests pass.
No original media, ROM or emulator behavior was changed.

Still open: later named providers/FILE, complete no-name system output and the
full grammar/failure matrix; DOS entry minimum 36 versus the original 37 check;
Utility opening after ReadArgs rather than before; success IoErr normalization;
real OS concurrent/resident/PURE lifetime and cleanup; CopperStart/MorphOS
execution; complete media/installed metadata, rights and package qualification.
Caller post-System IoErr is not generally proven child Result2. Ten matching
cases do not complete the command or change the 0/200 shipping admission count.

## 2026-09-27 - Workbench Break and ChangeTaskPri ReadArgs guest parity

Extended the disposable Workbench 3.1 guest probe to replace one explicitly
named existing C command on a fresh derivative while preserving its original
metadata. Paired original runs for missing Break PROCESS and missing
ChangeTaskPri PRIORITY had already recorded the exact DOS error stream, return
20 and caller post-System IoErr116.

The first native replacement comparisons found both candidates prepended their
command name to `PrintFault` and returned 10. Changed the two Workbench-only
parser-failure paths to pass a null fault header and return
`DOS.RETURN_FAIL`; the MorphOS command bodies are unchanged. Tightened the
supplied fixtures to require a null `PrintFault` header on these parser paths
and return level20, while retaining command-specific headers for other
ChangeTaskPri failures.

Refreshed resident qualifications pass ten Break and twelve ChangeTaskPri
vectors per CPU on 68000/020/040, with 13 reachable methods and zero runtime
features/helpers, external targets, exception regions, fatal sites, leaks or
shared-image writes. The exact missing-argument original/candidate pairs now
match command text, every output byte and length, return20 and caller IoErr116.
Neither invocation reaches target lookup, Signal or SetTaskPri. The aggregate
48-identity receipt is
`artifacts/workbench31-guest-command-cc18-readargs-pairs-20260926-v1/qualification.json`.
Generic preparation/capture binding tests were added; all 41 guest-probe tests
pass.

This closes only those two parser-failure invocations. Valid task effects,
other option/error combinations, MorphOS behavior, PURE/resident lifecycle,
CopperStart, full media/right closure and packaging remain open.

## 2026-09-27 - Workbench ChangeTaskPri current-task success pair

Added a safe valid invocation with PROCESS omitted: `C:ChangeTaskPri 0`.
Original Workbench and the candidate both produced zero output bytes, return0
and caller post-System IoErr0. The command's no-PROCESS path ran in fresh
disposable guests; the probe does not read the current task's priority back,
so the comparison is limited to the observed command result. The combined
three-case CC18 receipt is
`artifacts/workbench31-guest-command-cc18-command-pairs-20260927-v1/qualification.json`.
This adds one bounded success result; it does not verify the priority effect,
CLI-target changes, range/missing-target errors, MorphOS PID behavior, or
resident/PURE lifecycle.

## 2026-09-27 - Workbench Break and ChangeTaskPri missing-target parity

Captured original and candidate executions for `C:Break 999999` and
`C:ChangeTaskPri 0 PROCESS 999999`, where process 999999 does not exist. The
original emits unprefixed `Process 999999 does not exist` text, returns20 and
leaves caller post-System IoErr0. Both candidate outputs now match exactly;
neither failure case signals a task or calls `SetTaskPri`.

The fresh three-CPU resident qualifications pass ten Break and twelve
ChangeTaskPri vectors per CPU with 13 reachable methods and no runtime
features/helpers, external targets, exception regions, fatal sites, leaks or
shared-image writes. The aggregate five-case, 91-identity receipt is
`artifacts/workbench31-guest-command-cc18-command-pairs-20260927-v2/qualification.json`.
It combines the two missing-required-argument cases, `ChangeTaskPri 0`, and
these two missing-target cases. This closes only the captured results and
diagnostics: successful Break signal effects, ChangeTaskPri priority readback,
other syntax and errors, MorphOS PID semantics, actual PURE/resident lifecycle,
media/reference closure, package placement and shipping admission remain open.

## 2026-09-27 - Workbench Break and ChangeTaskPri target-effect parity

Extended the disposable guest probe to expand one `@SELF@` placeholder to its
own DOS CLI task number. It waits 150 DOS ticks at stage 3 so passive snapshots
can retain the pre-command owner state. The analyzer now reads the CLI task
number, signed priority and received signal mask from saved guest RAM. The
comparator can require those observed effects in addition to exact output,
return and caller IoErr parity.

Original and candidate `C:ChangeTaskPri 42 PROCESS @SELF@` runs both changed
CLI task 1 from priority 0 to 42. They produced no output, returned 0 and left
caller post-System IoErr 0. The effect comparison is
`artifacts/workbench31-guest-command-changetaskpri-self-priority-candidate-20260927-v1/effect-comparison-v2.json`.

Original and candidate `C:Break @SELF@ C` runs both set Ctrl-C (signal bit 12)
on CLI task 1. Its received-signal mask changed from `0x00000004` to
`0x00001104`; bit 12 was clear before and set afterward. Output remained empty,
return was 0 and caller IoErr was 0. The effect comparison is
`artifacts/workbench31-guest-command-break-self-signal-candidate-20260927-v1/effect-comparison.json`.

The probe HUNK compiles for 68000 with no runtime helpers, managed allocations,
external native targets or exception regions. The 47 focused probe-tool tests
pass. Original command protection word 0 remains preserved. These are one
successful target-effect case per Workbench command; default/ALL and other
Break masks, other target forms and priority boundaries, MorphOS PID semantics,
resident/PURE lifecycle, package placement and full profile closure remain open.

## 2026-09-27 - Workbench ChangeTaskPri out-of-range parity

The original and refreshed candidate were run as
`C:ChangeTaskPri 128 PROCESS @SELF@`. The original prints
`Priority out of range (-128 to +127)` and the Shell's failed-return footer,
returns 20, leaves caller post-System IoErr 0, and leaves CLI task 1 at
priority 0. The first candidate instead emitted the `ObjectTooLarge` fault and
left IoErr 207. The Workbench-only implementation now uses the original custom
diagnostic and clears IoErr; the MorphOS body remains unchanged.

The new [effect-aware comparison](../../../../artifacts/workbench31-guest-command-changetaskpri-128-self-candidate-fixed-20260927-v1/effect-comparison.json)
is `captured-case-equal` for the exact 74-byte output, return, caller IoErr and
unchanged task priority. The refreshed three-CPU resident qualification at
`artifacts/changetaskpri-wb31-native-20260927-range-fix-v1/qualification.json`
passes twelve supplied vectors per CPU and reports 13 reachable methods with
no runtime features/helpers, external targets, exception regions, fatal
sites, leaks or shared-image writes. The 47 focused guest-probe tests pass.

This closes only the captured upper out-of-range case for an existing CLI
target. Lower out-of-range, other target/result precedence, complete option and
diagnostic coverage, MorphOS PID semantics, lifecycle/PURE, licensing and
packaging remain open.

## 2026-09-27 - Workbench ChangeTaskPri range precedence

A safe original/candidate pair combined an invalid priority with a
nonexistent CLI target. Workbench reports the custom priority-range error
before it tries target lookup; the earlier candidate instead reported that
the process was missing. The Workbench body now checks the signed range before
`Forbid` and `FindCliProc`, so this path does not enter task-list protection or
perform a lookup. The paired guest record confirms the same 74-byte output,
return 20, caller post-System IoErr 0 and unchanged owner priority:
`artifacts/workbench31-guest-command-changetaskpri-range-missing-self-candidate-20260927-v1/effect-comparison.json`.

The refreshed qualification passes thirteen supplied DOS vectors per CPU on
resident 68000/020/040 HUNKs, with 13 reachable methods and no runtime
helpers/features, external native targets, exceptions, fatal machine-fault
sites, leaks or shared-image writes:
`artifacts/changetaskpri-wb31-native-20260927-range-precedence-v1/qualification.json`.
The remaining CC18 gaps include other target/result precedence, full parser
and diagnostic coverage, MorphOS PID behavior, installed lifecycle/PURE,
licensing and packaging.

## 2026-09-27 - Workbench ChangeTaskPri valid priority endpoints

Original and candidate Workbench guests ran `C:ChangeTaskPri 127 PROCESS
@SELF@` and `C:ChangeTaskPri -128 PROCESS @SELF@`. Both commands returned 0,
produced no output and left caller post-System IoErr at 0. Saved guest RAM
shows the same CLI task changing from priority 0 to 127 and from 0 to -128 in
both runs. The effect comparisons are
`artifacts/workbench31-guest-command-changetaskpri-pri127-candidate-20260927-v1/effect-comparison.json`
and
`artifacts/workbench31-guest-command-changetaskpri-pri-minus128-candidate-20260927-v1/effect-comparison.json`.

Together with the paired -129/128 failures, this covers both accepted signed
priority endpoints and both adjacent rejected values for a live Workbench CLI.
It does not close other process targets, all error precedence, MorphOS PID
semantics, or resident/PURE lifecycle and packaging.

## 2026-09-27 - Workbench Break default, ALL and combined masks

The original and candidate ran `C:Break @SELF@` with no mask switches,
`C:Break @SELF@ ALL`, and `C:Break @SELF@ D F` in separate fresh guests.
The original's default sets Ctrl-C (`0x1000`); ALL sets Ctrl-C/D/E/F
(`0xF000`); and D/F sets `0xA000`. The effect-aware comparisons require the
requested bits to be clear before System and set at the stable terminal sample
on CLI task 1. All pairs have empty output, return 0, caller post-System IoErr
0 and exact original/candidate effect equality. Receipts:

- `artifacts/workbench31-guest-command-break-default-candidate-20260927-v1/effect-comparison.json`
- `artifacts/workbench31-guest-command-break-all-candidate-20260927-v1/effect-comparison.json`
- `artifacts/workbench31-guest-command-break-df-candidate-20260927-v1/effect-comparison.json`

The guest comparator now supports a required signal mask in addition to a
single-bit expectation; its 49 focused tests pass. Other Break flag
combinations and target IDs, MorphOS behavior, resident/PURE lifecycle and
package admission remain open.

## 2026-09-27 - Workbench Break individual D/E/F mask effects

Fresh original/candidate Workbench guests now also compare the individual
`C:Break @SELF@ D`, `E`, and `F` cases. On CLI task 1, the requested bit was
clear before each command and set afterward: D `0x2000`, E `0x4000`, and F
`0x8000`. All three pairs have empty output, return 0, caller post-System
IoErr 0, and exact captured-effect equality. Receipts:

- `artifacts/workbench31-guest-command-break-d-candidate-20260927-v1/effect-comparison.json`
- `artifacts/workbench31-guest-command-break-e-candidate-20260927-v1/effect-comparison.json`
- `artifacts/workbench31-guest-command-break-f-candidate-20260927-v1/effect-comparison.json`

This extends the bounded Workbench Break mask evidence to default C, explicit
C, individual D/E/F, ALL, and combined D/F. Other combinations/targets,
parser/help and diagnostic cases, MorphOS parity, PURE/resident lifecycle,
licensing and package admission remain open. The focused comparator suite has
49 passing tests.

## 2026-09-27 - Workbench Break combined D/E mask effect

The original and candidate were also run as `C:Break @SELF@ D E` in fresh
guests. CLI task 1 had both selected bits clear before System and both set at
the terminal sample (`0x6000`) in each guest. Output is empty, return is 0,
caller post-System IoErr is 0, and the full captured effect matches. Receipt:

- `artifacts/workbench31-guest-command-break-de-candidate-20260927-v1/effect-comparison.json`

This adds one multi-switch combination alongside D/F. The remaining mask
combinations and targets, MorphOS parity, resident/PURE lifecycle, licensing,
and package admission remain open.

## 2026-09-27 - Workbench Break complete flag-mask fixture

Expanded the Workbench 3.1 supplied-DOS fixture to execute all fifteen
nonempty C/D/E/F combinations, plus the no-switch default and `ALL`. The
refreshed resident HUNK qualification
`artifacts/break-wb31-native-20260927-all-flags-v1/qualification.json` passes
24 invocations per CPU on 68000/020/040. All variants have 13 reachable
methods, zero managed runtime features/helpers, external native targets,
exception regions, fatal sites, leaks or shared-image writes. The 68000 HUNK
retains protection word 0. The production Break body did not change; this
closes the supplied-vector truth table only. Original guest coverage still has
unpaired switch combinations and additional CLI target IDs, while MorphOS parity,
PURE/resident lifecycle, licensing and package admission remain open.

## 2026-09-27 - Workbench Break complete guest flag-mask matrix

Original/candidate guest comparisons now cover every combination of the
Workbench `C`, `D`, `E`, and `F` switches on CLI task 1, including the
no-switch Ctrl-C default, plus `ALL`. The full combinations (sixteen pairs)
and `ALL` all have exact output/result/caller-IoErr equality and the expected
requested signal bits set in both saved guest states. New receipts:

- `artifacts/workbench31-guest-command-break-cd-candidate-20260927-v1/effect-comparison.json` — `0x3000`
- `artifacts/workbench31-guest-command-break-ce-candidate-20260927-v1/effect-comparison-v2.json` — `0x5000`
- `artifacts/workbench31-guest-command-break-cf-candidate-20260927-v1/effect-comparison.json` — `0x9000`
- `artifacts/workbench31-guest-command-break-ef-candidate-20260927-v1/effect-comparison.json` — `0xC000`
- `artifacts/workbench31-guest-command-break-cde-candidate-20260927-v1/effect-comparison.json` — `0x7000`
- `artifacts/workbench31-guest-command-break-cdf-candidate-20260927-v1/effect-comparison.json` — `0xB000`
- `artifacts/workbench31-guest-command-break-cef-candidate-20260927-v1/effect-comparison.json` — `0xD000`
- `artifacts/workbench31-guest-command-break-def-candidate-20260927-v1/effect-comparison.json` — `0xE000`
- `artifacts/workbench31-guest-command-break-cdef-candidate-20260927-v1/effect-comparison.json` — `0xF000`

The first C/E comparator report remains as a rejected run because its requested
mask was mistyped as `0x6000`; the original and candidate state actually agreed
on `0x5000`. The corrected v2 receipt above requires and verifies `0x5000`.
This matrix still exercises only CLI task 1; additional target numbers,
diagnostics, MorphOS parity, resident/PURE lifecycle, licensing and packaging
remain open.

## 2026-09-27 - Workbench Break process-zero diagnostic parity

Captured original and candidate `C:Break 0` executions in separate disposable
guests. Both emitted the exact 54-byte output
`Process 0 does not exist\nC:Break failed returncode 20\n`, returned 20, and
left caller post-System IoErr at 0. No task was signaled. Receipt:

- `artifacts/workbench31-guest-command-break-zero-candidate-20260927-v1/effect-comparison.json`

This adds the zero-valued numeric target error beside the existing missing
CLI case. Successful target effects remain verified only against CLI task 1;
additional target numbers, other diagnostics, MorphOS parity, resident/PURE
lifecycle, licensing and package admission remain open.

## 2026-09-27 - Workbench Version DOS v37 startup requirement

The original C/Version opens `dos.library` at version 0, then checks that the
returned library is at least v37. The candidate Workbench entry now requests
DOS v37, and its native fixture enforces that value for all cases. The refreshed
three-CPU resident qualification passes 58 invocations per CPU on 68000/020/040:

- `artifacts/version-wb31-native-20260927-dos37-v1/qualification.json`

This closes the bounded candidate's DOS v36/v37 startup mismatch. The original
library-open ordering, later ordered lookup providers/FILE path, guest parity,
full lifecycle and profile admission remain open.

## 2026-09-27 - Workbench Version direct FILE path

The Workbench `Version` candidate now scans direct FILE input for `$VER:` tags
across DOS `Read` boundaries, prints normal and `FULL` output, applies minimum
comparisons after printing, and uses DOS `LoadSeg` to find a Resident in HUNK
files that have no version tag. The no-name system path remains active with
`FILE`, and file/segment/scan-buffer resources are released on success and
failure. The updated three-CPU resident receipt passes 72 supplied invocations
per CPU (216 total) on 68000/020/040:

- `artifacts/version-wb31-native-20260927-file-v5/qualification.json`

Fresh original guest captures show `C:Version C:Avail FILE` printing
`avail 40.1`, FULL adding `(02/09/93)`, warning result 5 after output, and the
no-name system report staying active with FILE. Other captures cover direct
FILE's no-provider-fallthrough behavior, the no-tag diagnostic, and the
HUNK-backed `DEVS:clipboard.device` Resident result. Receipts are indexed in
`contracts/Version.md`. Four direct FILE invocations now have matching
replacement-guest comparisons; later ordered providers, broader FILE parity,
full system/error ordering, PURE/resident admission, lifecycle, rights and
packaging remain open.
The shared runner also retains its 94-vector-per-CPU MorphOS Version pass at
`artifacts/version-morphos-native-20260927-wb-fixture-regression-v2/qualification.json`.

### 2026-09-27 - Workbench Version FILE guest parity checkpoint

The original and replacement guests now match in four separate 2,400-frame
captures:

- `C:Version C:Avail FILE` emits `avail 40.1\n` (11 bytes), returns 0, and
  leaves caller post-System IoErr at 0. Comparison:
  `artifacts/workbench31-guest-command-version-file-candidate-20260927-v2/comparison.json`.
- `C:Version C:Avail FILE FULL` emits `avail 40.1 (02/09/93)\n` (22 bytes),
  returns 0, and leaves caller post-System IoErr at 0. Comparison:
  `artifacts/workbench31-guest-command-version-file-full-candidate-20260927-v1/comparison.json`.
- `C:Version C:Avail FILE VERSION 999` emits the same 11-byte line, returns 5
  after printing, and leaves caller post-System IoErr at 0. Comparison:
  `artifacts/workbench31-guest-command-version-file-warning-candidate-20260927-v1/comparison.json`.
- `C:Version DEVS:clipboard.device FILE` emits `clipboard.device 38.8\n`
  (22 bytes), returns 0, and leaves caller post-System IoErr at 0. Comparison:
  `artifacts/workbench31-guest-command-version-file-hunk-candidate-20260927-v1/comparison.json`.

Each candidate media receipt records the original 4,764-byte `C/Version`
identity, preserves metadata including protection 0, and binds the 10,044-byte
replacement HUNK at SHA-256
`f7f33772232a836a1448f268f8e872e0f49ae7f9fe087852dd76c1ce186de88b`.
These pairs close only the four listed invocations. Missing/no-tag errors,
system-mode FILE behavior, later ordered providers, full system/error ordering,
lifecycle, original PURE classification, rights and packaging remain open. The
first candidate preparation exposed a Version snapshot-name selection bug in
the derivative builder; option-family selection is now explicit and has a
regression test. All 50 focused Workbench guest-probe tooling tests pass.

### 2026-09-27 - Workbench Version command-segment provider

The default named Workbench Version path now checks DOS command segments after
the first Resident miss, under a balanced Exec `Forbid`/`Permit`. It tries
`FindSegment` system 0 before system 1, scans the segment list for `$VER:`, and
supports ordinary and FULL output. Internal and disabled command segments use
the original Workbench `shell` Resident fallback. The refreshed resident HUNK
qualification passes 77 supplied invocations per CPU on 68000/020/040 with no
shared-image writes or fixture leaks:

- `artifacts/version-wb31-native-20260927-segments-v4/qualification.json`

The generated HUNK sizes are 11,596 / 11,764 / 11,468 bytes for 68000 / 68020 /
68040. This checkpoint does not yet have original-guest comparisons for command
segments. The bounded trailing-colon DOS-list path is recorded below; LibList,
DeviceList and filesystem providers remain open, along with broader comparison,
error-ordering, PURE/lifecycle, rights and package gates.

### 2026-09-27 - Workbench Version trailing-colon DOS device handler

After the first Resident and command-segment lookups miss, the Workbench
candidate now handles trailing-colon names with DOS `LockDosList` over
Devices|Read, `FindDosEntry` over Devices, and a balanced `UnLockDosList`. It
temporarily removes the trailing colon for `FindDosEntry`, restores it, checks
the DeviceNode startup field, and scans the handler segment list for a Resident.
The existing Resident/FULL formatter handles a hit; `FILE` remains restricted
to its direct-file provider.

The native fixture covers a resident hit, `FULL`, `RES`, missing DeviceNode,
missing startup, missing segment list, missing Resident, exact list-call order,
colon restoration, and FILE isolation. Qualification passes 85 invocations per
CPU on 68000/020/040 with balanced fixture resources and zero shared-image
writes:

- `artifacts/version-wb31-native-20260927-doslist-v6/qualification.json`

HUNK sizes are 12,004 / 12,180 / 11,876 bytes for 68000 / 68020 / 68040. Their
SHA-256 hashes are `c5ee16f55f25508eca21819fd4b7a000d0954292f15cd1f4c8ae2dc6f9667dee`,
`544f139fc35e3a1d6aaefd456739574c2cd3e8778c21bf29e4257eb4c7b43e5c`, and
`29e7f9ff1a167ee7a00085a24227c6077189864138182face229cb3bed260a8e`.

Original and candidate guest runs match in two fresh 2,400-frame pairs:

- `C:Version DF0:` emits `filesystem 40.1\n` (16 bytes), returns 0 and leaves
  caller post-System IoErr 0:
  `artifacts/workbench31-guest-command-version-df0-candidate-20260927-v1/effect-comparison.json`.
- `C:Version DF0: RES` emits the same 16 bytes, returns 0 and leaves caller
  post-System IoErr 0:
  `artifacts/workbench31-guest-command-version-df0-res-candidate-20260927-v1/effect-comparison.json`.

These observations cover only the two listed requests; they do not close
command-segment guest parity or the overall Version contract. LibList,
DeviceList, filesystem providers, broader parser/error behavior, original
PURE/resident lifecycle, rights and package gates remain open.

## 2026-09-27 - Workbench Version ordered named default providers

The Workbench Version candidate now implements the observed default named
provider order through Resident, command segment, trailing-colon DOS device
handler, Exec `LibList`, Exec `DeviceList`, direct complete-name file, `LIBS:`
basename file, and `DEVS:` basename file. The separate `FILE` path remains
restricted to its direct-file provider. The list scans use public Exec calls
and retain owned copies after releasing the protected lists.

The refreshed three-CPU resident qualification passes 92 supplied vectors per
CPU (276 total), with balanced fixture resources and zero shared-image writes:

- `artifacts/version-wb31-native-20260927-files-v5/qualification.json`

The 68000 HUNK is SHA-256
`48260077d3f997bd5262d4a348a3197cba8a5d015cccadd3c47b612a4ba8ec91`.
Original and candidate guest runs match for `C:Version
LIBS:version.library`: both emit `version.library 40.42\n` (22 bytes), return
0, and leave caller post-System IoErr at 0. Receipts:

- `artifacts/workbench31-guest-command-version-liblist-original-20260927-v1/probe-analysis.json`
- `artifacts/workbench31-guest-command-version-default-files-candidate-20260927-v3/effect-comparison.json`

This closes the captured loaded-library-miss-to-`LIBS:`-file path only. It
does not establish complete provider ordering or error behavior, full Version
parity, actual PURE/resident lifecycle, rights, or package admission.

## 2026-09-27 - Workbench Which full CLI path traversal

`Workbench31WhichCommand` no longer stops after 64 DOS-owned CLI path nodes.
It traverses until the list ends and uses constant-space cycle detection to
fail safely on a cyclic path list. The supplied-vector suite now includes an
`ALL` search through 65 path nodes and a cyclic-list cleanup case. The refreshed
Workbench qualification passes 19 invocations per CPU on 68000/020/040 (57
total), with balanced locks and zero shared-image writes:

- `artifacts/which-wb31-native-20260927-unbounded-path-v2/qualification.json`

The 68000 HUNK is 4,548 bytes, SHA-256
`9b38263864c5b84ac23a1477542c38f0e4db94d9a5bc41ae7125f75c860bce4b`. Because
the path walk is shared, MorphOS `Which` was requalified on its existing 17
vectors per CPU; the receipt is
`artifacts/which-morphos-native-20260927-unbounded-path-v1/qualification.json`.
This does not qualify MorphOS alias options or guest behavior.

Two fresh 2,400-frame original/candidate comparisons use the updated HUNK and
the real Workbench `ReadArgs` and filesystem:

- `C:Which C:` emits `Workbench3.1:C\n` (15 bytes), returns 0 and leaves caller
  post-System IoErr at 0:
  `artifacts/workbench31-guest-command-which-c-directory-candidate-20260927-v2/effect-comparison.json`.
- `C:Which C:CopperOSNoSuchEntry` emits no bytes, returns 5 and leaves caller
  post-System IoErr at 205:
  `artifacts/workbench31-guest-command-which-missing-cpath-candidate-20260927-v2/effect-comparison.json`.

The exact caller error is not proof of child `Result2`; these comparisons cover
only one directory and one explicit miss. Keep the remaining current-directory
and assign routes, the 1,024-byte per-candidate buffer boundary and MorphOS
`ALIAS`/`NOALIAS` contract open.

## 2026-09-27 - Workbench Which bare-name and explicit-path ALL parity

Two original guest captures found a real lookup mismatch. `C:Which Execute
ALL` emitted the same C: path twice in the original and once in the first
candidate; `C:Which C:Execute ALL` emitted one original line while the
candidate incorrectly searched it through every CLI path. The Workbench body
now treats `FilePart(FILE) != FILE` as a complete path, skips CLI path entries
for that input, and adds the observed `C:` fallback after CLI path traversal
for bare names. The earlier mismatching receipts remain intact as negative
controls.

The current Workbench native receipt
`artifacts/which-wb31-native-20260927-routes-v3/qualification.json` passes 21
vectors per CPU on resident 68000/020/040 (63 total), including C: fallback,
explicit path, 65 path nodes, and cycle cleanup with no shared-image writes.
Its 68000 HUNK is 4,880 bytes, SHA-256
`ddbd86bf747f54824febc614e02888fdda3881e29c6d5e76e97bacdcf4f82bbd`. The
shared MorphOS path implementation passes its 17-vector-per-CPU regression at
`artifacts/which-morphos-native-20260927-routes-v1/qualification.json`.

Fresh original/candidate pairs now match exactly for `C:Which Execute ALL`
(two `Workbench3.1:C/Execute\n` lines, 46 bytes) and
`C:Which C:Execute ALL` (one such line, 23 bytes). Both return 0 and leave
caller post-System IoErr 0. Receipts:

- `artifacts/workbench31-guest-command-which-all-execute-candidate-20260927-v2/effect-comparison.json`
- `artifacts/workbench31-guest-command-which-all-explicit-c-execute-candidate-20260927-v2/effect-comparison.json`

These bounded cases complement the root-directory and explicit-miss pairs
above. They do not prove child `Result2`, other route/assign collisions, the
non-internal switch matrix, break/failure breadth, long individual names,
MorphOS aliases, installed PURE/resident lifecycle, or shipping admission.

## 2026-09-27 - Workbench Which classic non-internal switch matrix

Captured the original and current resident candidate for all eight classic
switch combinations with the filesystem-resolved non-internal name `Execute`
and for all eight combinations with the absent name `CopperOSMissingWhich`.
All 16 original/candidate comparisons match the exact command text, output,
command return and caller post-System IoErr. The digest
`artifacts/workbench31-guest-command-which-switch-matrix-20260927-v1/evidence-summary.json`
links the comparison receipts and records each bounded result.

The found-name cases confirm that default and `NORES` each print the C: path
once; `ALL` and `NORES ALL` print the identical path twice; and `RES`,
`NORES RES`, `RES ALL`, and `NORES RES ALL` print nothing. In particular,
`NORES RES` and `NORES RES ALL` return 5 with caller IoErr 0, while restrictive
`RES` misses return 5/205. The missing-name set matches the same distinctions.
These two names do not close other lookup categories, route/assign collisions,
failure or break behavior, child `Result2`, MorphOS alias semantics, or
installed PURE/resident lifecycle.

## 2026-09-27 - Workbench Which ReadArgs parity and fresh route rerun

A current-HUNK original/candidate mismatch for parser errors is fixed. The
classic entry calls DOS `PrintFault` with a null header when `ReadArgs`
returns `RETURN_ERROR`, saves/restores the parser IoErr and returns WARN.
Workbench guest pairs now match `C:Which` (required argument, output
`required argument missing\n`, return 5/IoErr 116) and
`C:Which Execute MYSTERY` (output `wrong number of arguments\n`, return
5/IoErr 118). MorphOS behavior remains unchanged until captured against its
original binary.

The resulting resident qualification passes 22 vectors per CPU on
68000/020/040 (66 total), with a 4,932-byte 68000 HUNK, SHA-256
`be283d619f183267e4b1b25f7d2ecb280815a373e4960550b66fc45a79c5d189`, at
`artifacts/which-wb31-native-20260927-readargs-v1/qualification.json`. The
shared MorphOS body passes its 17-vector-per-CPU regression at
`artifacts/which-morphos-native-20260927-readargs-v1/qualification.json`.

Using fresh candidates bound to this HUNK, all prior four route cases and
sixteen `NORES`/`RES`/`ALL` cases were re-analyzed and compared; all 20 match
the original. The refreshed route summary is
`artifacts/workbench31-guest-command-which-route-cases-20260927-v2/evidence-summary.json`;
the option summary is
`artifacts/workbench31-guest-command-which-switch-matrix-20260927-v2/evidence-summary.json`.
Together with the two parser pairs, 22 bounded Workbench guest comparisons
match. Caller post-System IoErr remains distinct from child `Result2`. Other
lookup routes/collisions, diagnostics and failure/break breadth, long names,
MorphOS alias semantics, installed pure/resident lifecycle, licensing, package
and complete differential admission remain open.

## 2026-09-27 - Workbench Which ReadArgs help continuation

Captured two original and current-HUNK candidate invocations through the real
Workbench DOS parser. `C:Which ?` prints the full template followed by
`required argument missing`, returns 5 and leaves caller IoErr 116.
`C:Which Execute ?` prints the same template prefix and then the normal C:
match line, returns 0 and leaves caller IoErr 0; the supplied FILE continues
through lookup. Both exact output byte sequences and observed results match.
The fresh pair summary is
`artifacts/workbench31-guest-command-which-readargs-help-20260927-v1/evidence-summary.json`.
These observations close only the two exact Workbench `?` inputs; MorphOS help,
other option interactions, and full contract/lifecycle gates remain open.

## 2026-09-27 - Workbench Which internal-command switch matrix

All eight classic switch combinations for the internal command name `CD` were
run through the original Workbench guest and the current resident candidate.
Every output byte, return and caller post-System IoErr matched. The results
retain the internal-only `ALL` distinction: it prints `INTERNAL CD\n` yet
returns 5/IoErr 205; `NORES RES` and `NORES RES ALL` return 5/IoErr 0. The
other combinations are recorded in the contract table.

Together with the route, found/missing option, parser-error and help captures,
the current 4,932-byte HUNK has 32 exact bounded guest pairs. Aggregate
receipt: `artifacts/workbench31-guest-command-which-current-hunk-20260927-v1/evidence-summary.json`.
No child Result2 or full installed-lifecycle claim is made.

## 2026-09-27 - Workbench Which regular-file subdirectory lookup

The original command and current resident HUNK were separately executed with
`C:Which S/Startup-Sequence`. Both emit
`Workbench3.1:S/Startup-Sequence\n` (32 bytes), return 0 and leave caller
post-System IoErr 0. Exact comparison:
`artifacts/workbench31-guest-command-which-startup-file-candidate-20260927-v1/effect-comparison.json`;
original record:
`artifacts/workbench31-guest-command-which-startup-file-original-20260927-v1/probe-analysis.json`.
The candidate HUNK is 4,932 bytes with SHA-256
`be283d619f183267e4b1b25f7d2ecb280815a373e4960550b66fc45a79c5d189`.

This is a separate 33rd bounded pair and is not included in the existing
32-case aggregate. The diagnostic derivative modifies the target startup
script to add the probe line, so this demonstrates file lookup and emitted
path only; it does not compare file content or source metadata. Caller IoErr
still does not prove child Result2. Other file classes, route collisions,
MorphOS alias semantics, PURE/resident lifecycle and package admission remain
open.

## 2026-09-27 - Workbench Which directory filtering

The latest resident Workbench HUNK filters directories with DOS `Examine` and
keeps assign roots and regular-file hits. Eight fresh original/candidate guest
comparisons all match exactly: `C:Which S` returns 5/IoErr 205 with no output;
`C:Which S/` returns 5/IoErr 0 with no output;
`C:Which S/Startup-Sequence` emits the exact 32-byte path line and returns
0/IoErr 0; and `C:Which C:` emits `Workbench3.1:C\n` and returns 0/IoErr 0.
An additional assign-root pair, `C:Which SYS:`, emits `Workbench3.1:\n` and
returns 0/IoErr 0. Through that assign, `C:Which SYS:S` rejects the directory
with return 5/IoErr 0, while `C:Which SYS:S/Startup-Sequence` emits the same
32-byte canonical file path and returns 0/IoErr 0.
`C:Which SYS:S/..` emits no line and returns 5/IoErr 205.
The comparisons are recorded under:
`artifacts/workbench31-guest-command-which-subdir-directory-filter-candidate-20260927-v1/`,
`artifacts/which-explicit-subdir-directory-filter-candidate-20260927-v1/`,
`artifacts/which-startup-file-directory-filter-candidate-20260927-v1/`, and
`artifacts/which-c-directory-filter-candidate-20260927-v1/`, and
`artifacts/workbench31-guest-command-which-sys-assign-candidate-20260927-v1/`,
`artifacts/workbench31-guest-command-which-sys-subdir-candidate-20260927-v1/`,
and
`artifacts/workbench31-guest-command-which-sys-startup-file-candidate-20260927-v1/`.
The parent-path comparison is at
`artifacts/workbench31-guest-command-which-sys-parent-candidate-20260927-v1/`.

Native resident qualification passes 31 vectors per CPU on 68000/020/040. The
5,316-byte 68000 HUNK SHA-256 is
`3c7a61a5d84b313dc91874360ba3caa26e7f284c189c8b1c985753a30af21f57`.
Qualification receipt:
`artifacts/which-wb31-native-20260927-sys-parent-v10/qualification.json`.
Earlier mismatching directory-positive captures are retained as negative
controls. These eight exact invocations remain separate from the 32-case
aggregate and do not establish full lookup/error parity, child `Result2`,
MorphOS behavior or installed PURE/resident lifecycle.

The attempted missing-assign invocation `C:Which MISSING:` is retained as
inconclusive: the original guest capture completed its frame schedule but did
not produce a valid complete probe record. It is not used as command-result
evidence; see
`artifacts/workbench31-guest-command-which-missing-assign-original-20260927-v1/probe-analysis.json`.

## 2026-09-27 - MorphOS Which alias-provider candidate

The MorphOS command documentation lists
`FILE/A,NOALIAS/S,ALIAS/S,NORES/S,RES/S,ALL/S`, describes `NOALIAS` as excluding
alias lookup and `ALIAS` as alias-only lookup, and says `ALL` continues through
the search path. The official SDK exposes public DOS
`FindVar(CONST_STRPTR name, ULONG type)`. The candidate now uses
`DOS.FindVar(file, LocalVariableType.Alias)` for exact-name alias matching,
searches aliases before resident/filesystem results by default, skips alias
lookup for `NOALIAS`, and restricts `ALIAS` to alias lookup. It reports the
candidate line `ALIAS <name>`.

The supplied native fixture covers alias-only hit/miss, default alias hit,
`NOALIAS` suppression, file-path fall-through, and interleaved calls. Resident
68000/020/040 qualification passes 20 vectors per CPU (60 total), with no
fixture leaks or shared-image writes:
`artifacts/which-morphos-native-20260927-findvar-v2/qualification.json`.

The MorphOS documentation does not specify alias output, exact ordering,
IoErr/status or conflicting-switch behavior. Those decisions are candidate
semantics, not verified compatibility. No MorphOS guest is currently available
for comparison, so the MorphOS `Which` contract, original pure/resident
lifecycle, package placement and full profile gates remain open.

## 2026-09-27 - Workbench Which cross-profile rebuild identity

The MorphOS alias-provider edit changed the shared source build identity. The
Workbench `Which` entry was requalified for 31 supplied vectors per CPU on
68000/020/040 at
`artifacts/which-wb31-native-20260927-findvar-regression-v1/qualification.json`.
Its 6,308-byte 68000 HUNK has SHA-256
`cb7a3a3aa26e1d12381da5280f7281265ad5b0e5b52648f2f8d94c8918ce7294`. Existing
Workbench guest comparisons remain tied to earlier HUNKs except for one fresh
`C:Which SYS:S/..` comparison. The current HUNK matches empty output, return 5
and caller IoErr 205; receipt:
`artifacts/workbench31-guest-command-which-crossprofile-current-candidate-20260927-v1/effect-comparison.json`.
This remains one bounded parity case, not a refreshed matrix.

## 2026-09-27 - MorphOS Which extended-switch matrix

The supplied native fixture now covers all 32 combinations of MorphOS
`NOALIAS`, `ALIAS`, `NORES`, `RES`, and `ALL`, each with candidate
found/missing inputs, plus an alias-only miss with resident/path alternatives
present. The fixture also accounts for `RES` suppressing alias lookup unless
`ALIAS` requests alias-only behavior. The candidate passes 85 vectors per CPU
on 68000/020/040 without fixture leaks or shared-image writes:
`artifacts/which-morphos-native-20260927-switch-matrix-v3/qualification.json`.

The fixture's result/output expectations encode current candidate assumptions;
they are not MorphOS guest captures. The `ALIAS <name>` output, option
conflicts, default precedence, result/IoErr and exact shipped grammar remain
unverified. The shared Workbench entry was also requalified for 31 vectors per
CPU after the fixture change at
`artifacts/which-wb31-native-20260927-switchmatrix-regression-v1/qualification.json`.

## 2026-09-27 - CC11 Type DOS stream failure paths

The Type resident fixture now injects a DOS `Read` failure and a `Write`
failure after a positive short write. The tests check the source-shaped
`TYPE: can't open` diagnostic, return level, selected `IoErr`, partial output,
and closure of the input handle and matcher context. MorphOS passes 16 supplied
vectors per CPU at
`artifacts/cc11-type-morphos-native-20260927-io-failures-v1/qualification.json`;
the Workbench five-slot syntax candidate passes 17 per CPU at
`artifacts/cc11-type-wb31-native-20260927-io-failures-v1/qualification.json`.
The MorphOS case follows the inspected 50.6 source. Workbench failure behavior
is candidate-modeled. These fixtures do not substitute for original guest
output, parser, handler, or package evidence.

## 2026-09-27 - CC11 Search per-file failures and link warnings

The inspected official MorphOS 3.20 Search source treats per-file Open, Read,
and Seek failures as misses, allowing the outer matcher traversal to continue;
successful completion clears IoErr. Its recursive soft-link helper warns and
skips dangling links. The resident MorphOS entry now follows that policy, while
the separate Workbench syntax candidate retains its independent behavior.

MorphOS Search passes 46 supplied vectors per CPU on 68000/020/040 at
`artifacts/cc11-search-morphos-native-20260927-source-file-errors-v1/qualification.json`.
The Workbench candidate was requalified after the shared fixture changed and
passes 19 vectors per CPU at
`artifacts/cc11-search-wb31-native-20260927-source-error-regression-v1/qualification.json`.
Both report 43 reachable methods and no shared-image writes or fixture leaks.
This adds source-shaped resident coverage only. Original guest output,
real-handler behavior, binary correspondence, PURE/resident lifecycle,
licensing, package admission, and differential gates remain open.

## 2026-09-27 - CC18 MorphOS Break masks and target precedence

The MorphOS Break fixture now exercises every C/D/E/F combination plus the
source-specific PROCESS/PORT rules: nonzero PROCESS wins even if a PORT exists,
zero PROCESS falls through to PORT lookup, and a failed zero-process fallback
keeps the process-0 diagnostic. The latest resident receipt
`artifacts/break-morphos-native-20260927-mask-precedence-v1/qualification.json`
passes 32 vectors per CPU on 68000/020/040 with 16 reachable methods and no
shared-image writes or fixture leaks. It retains the source-backed PID and
port lookup cases. Workbench Break was requalified after the shared fixture
change and passes 24 vectors per CPU at
`artifacts/break-wb31-native-20260927-morphos-matrix-regression-v1/qualification.json`.
These are native source-shaped vectors; original MorphOS guest effects, exact
PID behavior, target races, PURE/resident lifecycle and package gates remain
open.

## 2026-09-27 - CC18 ChangeTaskPri MorphOS PID fallback version floor

Inspection of the released MorphOS 3.20 source confirms the `PROCESS/K/N`
template and that PID fallback is available from Exec 50.45 (or major versions
above 50). The fixture now supplies Exec version/revision and proves that a
50.44 target does not call `FindTaskByPID` even when a result is available,
while 51.0 may use the fallback. The MorphOS resident receipt passes 14
vectors per CPU on 68000/020/040 with 16 reachable methods and no managed
allocation sites or unsupported runtime features/helpers at
`artifacts/changetaskpri-morphos-native-20260927-pid-version-floor-v1/qualification.json`.
Workbench ChangeTaskPri was rerun after the shared fixture update and passes
13 vectors per CPU at
`artifacts/changetaskpri-wb31-native-20260927-current-regression-v1/qualification.json`.
These are supplied-Exec/DOS source-shaped checks. Original MorphOS guest PID
behavior, exact numbering/reuse, liveness/races, full diagnostic parity,
PURE/resident lifecycle and package gates remain open.

## 2026-09-27 - CC18 MorphOS current-task PID identity

The official MorphOS Exec SDK documents `FindTaskByPID(0)` as a special
current-task selector, while `TASKINFOTYPE_PID` returns the task's unique ID.
CopperStart's provider had returned zero for the current task's PID attribute.
It now reports the task's existing guest identity, while PID zero continues to
resolve the current task. The focused `ExecMorphOsExtendedCoreTests` pass all
ten tests, including lookup by the reported current-task ID and by the zero
selector. This corrects the selector/identity distinction only; exact PID
numbering and reuse, original command guest parity, task lifetime stress,
PURE/resident lifecycle and package gates remain open. Reference:
[MorphOS Exec SDK](https://morphos-team.net/sdk/exec.html).

## 2026-09-27 - CC18 MorphOS Break PID fallback version floor

The released MorphOS 50.6 Break source gates `FindTaskByPID` on Exec 50.45 or
newer, including major versions above 50. The supplied fixture now proves the
50.44 case does not call the fallback even when a PID result is available,
while Exec 51.0 may resolve and signal the task. The latest resident receipt
passes 34 vectors per CPU on 68000/020/040 with 16 reachable methods and no
managed allocation sites or unsupported runtime features/helpers at
`artifacts/break-morphos-native-20260927-pid-version-floor-v1/qualification.json`.
The prior receipt remains the all-mask/target-precedence checkpoint. These
are source-shaped vectors, not original guest proof; exact PID numbering and
reuse, target-liveness races, full parity, PURE/resident lifecycle and package
gates remain open. Workbench Break was rerun after the shared fixture change
and passes 24 vectors per CPU at
`artifacts/break-wb31-native-20260927-pid-version-floor-regression-v1/qualification.json`;
its 68000 HUNK hash is unchanged, preserving the applicability of prior guest
effects.

## 2026-09-27 - Current-worktree Shell and task-identity regression check

`dotnet test tests/Commands --no-restore` passes all 806 command/Shell cases
with no skips. The focused CopperStart Exec task identity/reaping filter passes
15 tests. These are current dirty-worktree checks, not a clean-commit baseline
or native command qualification. The test fixes keep the current semantics:
the nested control-record fixture places records outside the expanded frame;
the mock routes only `T:Prompt.*` writes to its prompt-capture stream; and the
compound Echo success examples use ordinary message text instead of the real
`FIRST/K/N` option keyword. CC02 and the MorphOS task-lifecycle gates remain
open.

## 2026-09-27 - DOS Process task-extension layout boundary

Moved the ABI-dependent DOS Process tail-size query into
`ExecMorphOsTaskCreationCore.GetTaskExtensionSize`. DOS now asks the Exec
owner for the version-selected size instead of referencing `ExecLayout`
directly; this preserves the MorphOS version-50 ETask tail and the Workbench
3.1 version-40 zero-extension layout. `dotnet build
src/CopperStart.Dos/CopperStart.Dos.csproj --no-restore` succeeds with zero
warnings and errors. The architecture boundary test passes, and an isolated
artifact-directory run of the architecture, DOS Process publication, and
MorphOS Exec task-creation filters passes 21 tests. The isolation avoids an
already-running full CopperStart Exec test process that holds the shared test
output files. This closes the source-level typed-boundary violation, but
generic AddTask ETask allocation/reaping and MorphOS lifecycle qualification
remain open.

An initial isolated run of the broader DOS/Shell input and object filters
passed 109/117. Four repository-root discovery checks are incompatible with
running their binaries from a temporary artifacts directory. The runner case
was a fixture omission: it lacked the now-required `PromptTemplate`, so
allocation failed before temporary-input publication. Added that workspace to
the fixture, aligned the CLI prompt assertions with the default `%N.%S>` BSTR,
updated the current DOS state version expectation to 28, and corrected the
reset test to assert refusal while a prepared child is live and then abort it
before checking exact-once cleanup. The rerun of the path-independent subset
passes 67 tests. No Shell/DOS product behavior changed in this qualification
pass.

## 2026-09-27 - Generic MorphOS AddTask ETask ownership

Closed the source-level lifetime gap for legacy MorphOS tasks created through
the installed generic `AddTask` vector. The vector now allocates a MorphOS
ETask sidecar with the active ROM allocator policy, records that exact
allocation in a one-entry `tc_MemEntry`, and rolls it back if initialization
fails. Foreign `RemTask` frees only the owned sidecar; self-removal defers
cleanup until the scheduler has selected another task and resumed on the
supervisor continuation stack. Workbench 3.1 version 40 keeps its original
Task layout. Existing ETask extensions are preserved, and `TF_ETASK` is set
only after the extension pointer is published.

The focused lifecycle, versioned AddTask, MorphOS self-RemTask, and vector
classification filter passes 9 tests. Adjacent task-vector, memory ownership,
native-boundary, DOS lifecycle/publication, MorphOS extension, and vector
classification regression filters pass 72 tests. These are managed and
compiled 68000 source checks; original MorphOS guest ID-sequence/reuse parity
and concurrent scheduler changes remain unqualified. This closes the previous
source-level generic AddTask ownership blocker only. It does not change command
shipping totals or profile admission.

## 2026-09-27 - CC18 MorphOS extended ReadArgs help

The released MorphOS 3.20 `Break` and `ChangeTaskPri` sources allocate a
DOS_RDARGS object, set `RDA_ExtHelp`, and pass it to `ReadArgs`. Added an
isolated MorphOS process-control argument helper so these two candidates
preserve the source's question-mark extended help without changing the shared
parser used by other resident HUNKs. Parser failures return RETURN_FAIL, as in
the released command sources. The Release MorphOS resident qualifications
pass 34 Break and 14 ChangeTaskPri supplied vectors per CPU on 68000/020/040,
including exact extended-help and allocation/free fixture checks:
`artifacts/cc18-break-extended-help-20260927-v6/` and
`artifacts/cc18-changetaskpri-extended-help-20260927-v3/`.

The separate Workbench 3.1 bodies retain their null-RDArgs behavior and pass
Release regressions with 24 Break and 13 ChangeTaskPri vectors per CPU, no
leaks, and no shared-image writes:
`artifacts/cc18-break-wb31-extended-help-regression-20260927-v3/` and
`artifacts/cc18-changetaskpri-wb31-extended-help-regression-20260927-v3/`.
These bounded native fixtures do not establish MorphOS guest behavior, full
command parity, exact MorphOS PID numbering/reuse/liveness, PURE/resident
lifecycle, licensing clearance, or package admission. Shipping totals remain
unchanged.

## 2026-09-27 - CC18 initial contracts for LoadLib, FlushLib, and iKill

Added separate command contracts from the MorphOS Library's published
descriptions. They supply concrete parser leads for the three missing CC18
candidates: `PATH/A/M,VERBOSE/S` for LoadLib;
`LIBRARY/A/M,ONCE/S,QUIET/S` for FlushLib; and an optional `TASKNAME` for
iKill, which enters crosshair selection when omitted (left-click selects and
right-click cancels). These references replace an undocumented-syntax blank,
but are not proof of the exact MorphOS 3.20 behavior. The source archive does
not contain these command sources. Their original packed binaries are present
in the selected ISO outside the repository and already have hash/extent
identities in the inventory; disassembly/runtime capture and candidate bodies
remain absent. Accordingly the three contract files leave original-member
behavior inspection,
diagnostics/results/IoErr, side effects, concurrency/lifetime, resident
qualification, rights, and package admission open. No command or profile
shipping status changes.

## 2026-09-27 - CC18 packed-member identity and inventory refresh

Re-read the original `LoadLib`, `FlushLib`, and `iKill` members directly from
the selected private MorphOS 3.20 ISO at `D:/TestData/MorphOSReferences/`,
without mounting the image or copying source binaries into the repository.
Their sizes/hashes match the inventory exactly: LoadLib 2,480 bytes,
`9cd6bfa13dcf94d0f934a24bfc31861fb4c637d865ebc7f63aee2466f3e28c80`; FlushLib
3,755 bytes,
`05c06f989d58d3eca7fb34d5eec2ddb0feea894b126e6857880a0542d7204140`; iKill
18,014 bytes,
`8c06ee672ff86c02f8f99b176435b9fe313400da2ae5f95d51bcaf2c2e10258b`. The
entries use the MorphOS packed-native `7fMOS` format; the version tags and media
identities are confirmed, but the code/help/runtime semantics are not decoded.

Because the goal plan had changed, reran Inventory `extract`, `verify`, and
`verify-media`. All pass against the private media and the current goal hash;
counts remain 200 external names, 188 MorphOS members, 58 Workbench entries,
46 shared identities, 12 Workbench-only names and 246 profiles. The six stated
reference-closure items remain open. The three CC18 contract files now bind
these identities and distinguish media identity from decoded behavior. No
shipping qualification changed.

## 2026-09-27 - CC10 MorphOS Eval lowercase operator prefixes

Compared the independent Eval expression lexer against the selected released
MorphOS 50.7 source. `evalParser.y` (`e43ce37d001bae6012cfd22c7117c0777daccf1a861c5aedc56bf311d81b51df`)
uses `strncmp` with the scanned operator-token length, so its lowercase
`mod`/`xor`/`eqv`/`lsh`/`rsh` checks accept two-letter prefixes `mo`, `xo`,
`eq`, `ls`, and `rs`. Single-letter aliases use a case-insensitive compare.
The candidate previously required each full three-letter word. Updated
`EvalExpressionEvaluator` to preserve the prefix and case behavior, with five
positive short-prefix tests and five uppercase-prefix rejection cases.

The portable `EvalExpressionEvaluatorTests` filter passes 46/46, and the full
managed Commands suite passes 816/816. The refreshed
MorphOS resident entry qualification passes 19 supplied post-ReadArgs vectors
per CPU (57 total) across 68000/020/040 in
`artifacts/cc10-eval-morphos-native-20260927-operator-prefix-v1/qualification.json`.
The HUNKs report 92 reachable methods, only the already-audited
`nullable-values` feature, no leaks, and no shared-image writes. Hashes:
68000 `4b2db101feb89a0a4ef916bab6acfb928c1a1c7ae502f2689f44b7aacdf0c483`,
68020 `1b0fbf19a39b309dba23195b4f6ae4d9cffb672d633e02b752959cb797782d11`,
68040 `d42faf0a9a6314ab8b473920830f26a9d172278e5e87cfccc35101bf2afe2a62`.
These use supplied argument slots; real MorphOS `ReadArgs`, original 3.20
binary parity, source reuse licensing, lifecycle/PURE, and packaging remain
open. Shipping totals remain 0/200 commands and 0/246 profiles.

## 2026-09-28 - CC18 MorphOS ChangeTaskPri explicit PROCESS 0

Added a supplied-DOS regression for the source-defined `PROCESS 0` case.
Unlike omitting PROCESS, a present PROCESS slot containing zero is passed to
`FindCliProc(0)` and then to MorphOS `FindTaskByPID(0)` when Exec supports that
vector. The SDK defines PID zero as the current task. The fixture requires the
pointer-indirect call to receive D0=0 inside Forbid and verifies that the
returned current-task target receives priority 4. The 68000/020/040 resident
qualification passes 15 vectors per CPU (45 total), with 22 reachable methods,
no managed runtime features/helpers, external targets, exception regions,
fatal sites, leaks, or shared-image writes:
`artifacts/changetaskpri-morphos-native-20260928-pid-zero-v1/qualification.json`.
The 68000 HUNK hash is
`7a33599717a58a75c8d185c2fd07a5f1fc61923068129a36cbf0087663588774`; the
020/040 hash is
`8075a7fc6e1bf185fe87a42926f1e04434504298aac88192251e97ec2c0f644e`.
This remains source-shaped fixture evidence, not original MorphOS guest
parity. PID numbering/reuse, liveness/races, PURE/resident lifecycle, rights,
packaging and shipping remain open at 0/200 commands and 0/246 profiles.

## 2026-09-28 - CC18 Workbench ChangeTaskPri PROCESS 0 boundary

Added the matching Workbench vector for explicit `PROCESS 0`. The classic
profile calls only `FindCliProc(0)`, returns the missing-process diagnostic,
and must not dispatch the MorphOS `FindTaskByPID` extension. The refreshed
resident HUNK qualification passes 14 supplied vectors per CPU on 68000/020/040
(42 total), with 13 reachable methods, no runtime features/helpers, external
targets, exception regions, fatal sites, leaks, or shared-image writes. Receipt:
`artifacts/changetaskpri-wb31-native-20260928-process-zero-v1/qualification.json`.
The 68000 HUNK hash is
`14722d2ab7675ff3cda9507f282274edcdb0f92e42ec82b15dcfc228ea5ce7f0`; 020/040
share `d90746f571b973e04ef6bbdce8a15b29f47910c6d8053296489d31c999d42308`.
This is supplied-DOS fixture evidence only; an original Workbench guest pair,
full target/parser coverage, PURE/resident lifecycle, licensing, packaging and
shipping remain open.

## 2026-09-28 - CC18 Workbench ChangeTaskPri PROCESS 0 guest pair

Ran `C:ChangeTaskPri 4 PROCESS 0` against the original Workbench 3.1 command
and the current 3,452-byte resident candidate in separate fresh diagnostic
derivatives of the hash-bound disk 2 image. Both emitted the exact 62-byte
`Process 0 does not exist\nC:ChangeTaskPri failed returncode 20\n`, returned
20, and left caller post-System IoErr at 0. The output/result/IoErr comparison
is `captured-case-equal` at
`artifacts/workbench31-guest-command-changetaskpri-process-zero-candidate-20260928-v1/comparison-output-result-ierr.json`.
The effect-aware comparison was deliberately not accepted because this probe
invocation did not publish a pre-System owner sample; no task-effect claim is
made. The original ADF and ROM remain unchanged. This closes only this one
Workbench target-zero case; MorphOS guest behavior, full argument/target
coverage, lifecycle, PURE, licensing, package admission and shipping remain
open.

## 2026-09-28 - CC18 Workbench Break question-mark and unknown-switch parity

Captured fresh original/candidate Workbench 3.1 guest pairs for `C:Break ?`
and `C:Break 1 Z`. Both pairs are `captured-case-equal` for raw output,
command return and caller post-System IoErr. The question-mark invocation does
not enter extended help; the original ReadArgs path reports the missing
required `PROCESS` argument, emitting 90 bytes and returning 20 with caller
IoErr 116. The unknown extra switch reports `wrong number of arguments`,
emitting 55 bytes and returning 20 with caller IoErr 118. Receipts:
`artifacts/workbench31-guest-command-break-help-candidate-20260928-v1/comparison.json`
and
`artifacts/workbench31-guest-command-break-unknown-switch-candidate-20260928-v1/comparison.json`.
These parser errors precede target resolution. No task-effect claim is made.
MorphOS help/runtime parity, other error and target forms, lifecycle,
PURE/rights/package admission and full shipping qualification remain open.

## 2026-09-28 - CC18 Workbench ChangeTaskPri question-mark and unknown-switch parity

Fresh original/candidate guest pairs match for `C:ChangeTaskPri ?` and
`C:ChangeTaskPri 1 Z`. The question-mark invocation follows Workbench's normal
ReadArgs failure path rather than extended help: exact output is 93 bytes,
return is 20, and caller post-System IoErr is 116. The unknown extra argument
prints `wrong number of arguments` plus the Shell failure line (63 bytes),
returns 20, and leaves caller IoErr at 118. Comparisons:
`artifacts/workbench31-guest-command-changetaskpri-help-candidate-20260928-v1/comparison.json`
and
`artifacts/workbench31-guest-command-changetaskpri-unknown-switch-candidate-20260928-v1/comparison.json`.
Both are parser failures before task lookup; no task-effect claim is made.
MorphOS extended-help guest parity, remaining parser/target cases, lifecycle,
PURE, rights, package admission and complete shipping qualification remain
open.

## 2026-09-28 - CC10 Eval profile-specific 08/09 behavior

Compared `EvalExpressionEvaluator` with selected MorphOS 3.20 `evalParser.y`
(SHA-256 `e43ce37d001bae6012cfd22c7117c0777daccf1a861c5aedc56bf311d81b51df`).
Its `08`/`09` branch falls through to `sscanf("%lli")`, which reads the leading
zero as octal zero, then consumes the remaining digit run. MorphOS source-shaped
`08 + 1` therefore evaluates to 1. CopperOS now preserves this behavior only
for the MorphOS grammar.

Five fresh original Workbench 3.1 guest captures establish a different result:
`C:Eval 08 + 1`, `C:Eval 08`, `C:Eval 08+1`, and `C:Eval 09+1` each output
`0\n`; the `C:Eval 010` control outputs `8\n`. Every invocation returns 0 and
leaves caller post-System IoErr 0. The exact analyses and identities are bound
by `docs/Commands/Workbench31MorphOS320/reference-captures/eval-wb31-leading-zero-20260928.json`.
The Workbench evaluator now stops after the leading zero when 8/9 follows, so
the two target grammars stay separate.

The focused portable Eval set passes 148/148. MorphOS resident entry
qualification passes 20 supplied post-ReadArgs vectors per CPU (60 total) at
`artifacts/cc10-eval-morphos-native-20260928-leading-zero-v1/qualification.json`;
each HUNK has 92 reachable methods, only the audited `nullable-values` feature,
and zero managed allocation sites, fatal sites, runtime helpers, external
targets, fixture leaks, and shared-image writes. Current 68000/020/040 HUNK
hashes are `81093d0b22ad42b2f6302e8bfb1836aa0e236bcb545770af1e99e5d4a0bee556`,
`bcc7a87514bac87e95c8543a36f01d53c5b0f71c7b2562dbca1f2234ff2991f3`, and
`d52a6cff0aa94aa4f807e0e954fb3d5953d27a5ef63e20bc9bf3a0423ec34119`.

Workbench resident entry qualification passes 13 supplied vectors per CPU
(39 total) at
`artifacts/cc10-eval-wb31-native-20260928-leading-zero-prefix-v1/qualification.json`.
An attempted guest substitution using this private resident-root HUNK produced
Workbench's `bad loadfile hunk` error (return 10, caller IoErr 235); that
artifact is not a distributable command file, so no candidate behavior
comparison is credited. The actual command build/package path must provide a
loadable Eval artifact before guest parity can close. No MorphOS guest or real
MorphOS `ReadArgs` run occurred; complete grammar, lifecycle, P/purity,
licensing, package admission, and shipping remain open.

## 2026-09-28 - CC10 Workbench Eval captured-subset matrix

Added the already measured Workbench 37.3 examples from the classic numeric,
grammar, and operator captures to the resident candidate suite. The expanded
matrix covers captured left-to-right subtraction/multiplication, parentheses,
unary minus, `0x`/`#x`/leading-zero octal, division, both modulo spellings,
left-to-right bitwise order, XOR, equivalence, shifts, complement, decimal
LFORMAT, low-digit octal, and one-/two-digit X/x LFORMAT output. The existing
caret prefix, TO, `08`/`09`, parser/allocation, repeat, and interleaving cases
remain in the matrix.

The focused `EvalExpressionEvaluatorTests` pass 67/67, and the broader portable
Eval filter passes 165/165. The refreshed Workbench resident qualification
passes 35 supplied post-ReadArgs vectors per CPU (105 total) on 68000/020/040
at `artifacts/cc10-eval-wb31-native-20260928-captured-matrix-v1/qualification.json`.
All three HUNK hashes are unchanged from the previous resident build; each
compatibility report has 63 reachable methods, only `nullable-values`, zero
managed allocation sites, fatal sites, helpers, external targets, and fixture
leaks; executions report one image load and zero shared-image writes.

This adds candidate-side vectors for selected original observations, not a
guest differential. The root HUNK is still rejected by original Workbench DOS
as `bad loadfile hunk`, the shipping manifest stays empty, and the full classic
grammar, output/errors, ReadArgs parity, lifecycle, PURE, licensing, and package
gates remain open.

## 2026-09-28 - CC10 Workbench Eval symbol-free guest pairs

The Eval resident qualifier now uses the compiler's supported `--symbols off`
option for the Workbench candidate and records that setting in its receipt.
The three-CPU resident matrix still passes 35 supplied post-ReadArgs vectors
per CPU (105 total), with 63 reachable methods, only `nullable-values`, no
managed allocation sites/helpers/external targets/fatal sites/fixture leaks,
one image load per CPU, and zero shared-image writes. The refreshed receipt is
`artifacts/cc10-eval-wb31-native-20260928-loadable-v1/qualification.json`;
its 68000/020/040 HUNK hashes are
`5d51031ab980e334e5cd2064974d7ed9eeeabbf524a2558bdb2f7a09ce51f74e`,
`5f7b2e3eaae387c0bcd1916c82debf6c388d21b97403e0b4daea59d954478775`, and
`95b3d33b84809aac2ae0515bc6df1705336cae9905771661749c58b96a5cef26`.

Original Workbench DOS rejected this Eval root's symbols-on HUNK with
`bad loadfile hunk` (return 10, caller IoErr 235). The symbol-free build was
loaded through the original Shell and its `ReadArgs` path. Exact guest pairs
for `C:Eval 08 + 1`, `C:Eval 010`, and `C:Eval 20-5*2` match original output,
return and caller post-System IoErr. Their receipts are
`artifacts/workbench31-guest-eval-08-candidate-symbols-off-20260928-v1/effect-comparison.json`,
`artifacts/workbench31-guest-eval-010-candidate-symbols-off-20260928-v1/effect-comparison.json`,
and
`artifacts/workbench31-guest-eval-20minus5times2-candidate-symbols-off-20260928-v1/effect-comparison.json`.
These are three case-level parity results, not full Eval qualification. The
private HUNK is not release-manifest-admitted; remaining command behavior,
diagnostics, lifecycle, PURE, licensing, package admission and project-wide
shipping remain open.

## 2026-09-28 - CC10 Workbench Eval expanded guest parity

Seven more fresh original/candidate guest pairs now match: parentheses
(`1+(2*3)` → `7\n`), left-to-right bitwise order (`1|2&4` → `0\n`), XOR
(`6 xor 3` → `5\n`), complement (`~1` → `-2\n`), width-limited octal
LFORMAT (`9 LFORMAT="p=%o2"` → `p=11` with no final LF), equivalence
(`6 eqv 3` → `-6\n`), and the `mod` spelling (`20 mod 6` → `2\n`). Every
comparison reports `captured-case-equal` and exact agreement in raw output,
return 0, and caller post-System IoErr 0. Together with the three earlier
symbol-free pairs, this gives ten case-level guest comparisons; it does not
qualify the full command.

The fresh pairs use hash-bound disposable derivatives of the audited original
Workbench disk 2 and the symbol-free 68000 Eval candidate. Comparison receipts:
`artifacts/workbench31-guest-eval-paren-candidate-20260928-v2/effect-comparison.json`,
`artifacts/workbench31-guest-eval-bitwise-stream-candidate-20260928-v1/effect-comparison.json`,
`artifacts/workbench31-guest-eval-xor-candidate-20260928-v1/effect-comparison.json`,
`artifacts/workbench31-guest-eval-complement-candidate-20260928-v1/effect-comparison.json`,
`artifacts/workbench31-guest-eval-lformat-octal-width-candidate-20260928-v1/effect-comparison.json`,
`artifacts/workbench31-guest-eval-eqv-candidate-20260928-v2/effect-comparison.json`,
and
`artifacts/workbench31-guest-eval-mod-candidate-20260928-v2/effect-comparison.json`.
The native 35-vector matrix remains private; full grammar and option coverage,
diagnostics, original PURE/resident admission, licensing, and release packaging
remain open.

## 2026-09-28 - CC10 Workbench Eval division and shift guest parity

Three more fresh original/candidate pairs match captured Workbench output:
`20/5` → `4\n`, `1 lsh 4` → `16\n`, and `16 rsh 2` → `4\n`. Each reports
`captured-case-equal`, with exact raw output, return 0 and caller post-System
IoErr 0. The bounded captured guest set is now thirteen cases; the complete
command contract and release gates remain open.

Comparison receipts:
`artifacts/workbench31-guest-eval-divide-candidate-20260928-v1/effect-comparison.json`,
`artifacts/workbench31-guest-eval-left-shift-candidate-20260928-v1/effect-comparison.json`,
and
`artifacts/workbench31-guest-eval-right-shift-candidate-20260928-v1/effect-comparison.json`.

## 2026-09-28 - CC10 MorphOS PathPart removes candidate scratch caps

The native `PathPart` body now measures `DIR`, `FILE`, and every `ADD/K/M`
component before allocation. It allocates an invocation-local output buffer
large enough for the selected path operation, with overflow checks against
DOS `Write`'s signed LONG count. This removes the candidate's former 1,024-byte
buffer ceiling and arbitrary 64-component scan ceiling while retaining
`ReadArgs`/`FreeArgs`, public DOS `PathPart`/`FilePart`/`AddPart`, borrowed
`Output()`, and balanced cleanup.

The native qualifier passes 15 supplied post-ReadArgs vectors per CPU (45
total) on 68000/020/040. New cases cover a 1,100-byte directory result, a
1,100-byte file result, 70 `ADD` components producing output longer than 1,024
bytes, and scratch-allocation failure after ReadArgs succeeds. The run reports
18 reachable methods, no runtime features, managed allocations, helpers,
external targets, exception/fatal sites, leaks or shared-image writes. Receipt:
`artifacts/cc10-pathpart-native-20260928-dynamic-output-v5/qualification.json`.
This remains candidate-only fixture evidence; MorphOS reference mode/output,
installed P policy, original parity, lifecycle, licensing and release admission
remain open.

## 2026-09-28 - CC10 Workbench Eval caret-prefix guest parity

A fresh original/candidate Workbench guest pair for C:Eval 2^3 is captured-case-equal. Both sides emit the exact bytes 32 0A, return 0, and leave caller post-System IoErr at 0. The result confirms this captured caret-prefix behavior only; it does not establish general exponentiation semantics or full command parity.

The hash-bound comparison receipt is artifacts/workbench31-guest-eval-caret-candidate-20260928-v1/effect-comparison.json. The original and candidate captures, analyses, and disposable prepared derivatives are preserved under their corresponding workbench31-guest-eval-caret-* and wb31-eval-caret-* paths.

## 2026-09-28 - CC10 Workbench Eval caret after addition guest parity

A fresh original/candidate guest pair for C:Eval 2 + 3 ^ 4 is captured-case-equal. Both sides emit the exact bytes 35 0A, return 0, and leave caller post-System IoErr at 0. Together with C:Eval 2^3, this provides two exact cases for caret-prefix handling; neither proves general exponentiation semantics.

Comparison receipt: artifacts/workbench31-guest-eval-caret-after-add-candidate-20260928-v1/effect-comparison.json. Original and candidate captures, analyses, and prepared derivatives are retained under the corresponding workbench31-guest-eval-caret-after-add-* and wb31-eval-caret-after-add-* paths.

## 2026-09-28 - CC10 Workbench Eval multiplication failure parity

Original Workbench guest observations show C:Eval 2* and C:Eval 2 ** 3 both emit 0 followed by LF, return 0, and leave caller post-System IoErr at 0. The prior candidate instead emitted a Shell return-code-10 failure for both cases. C:Eval 2+ remains a control that emits 2 followed by LF.

The Workbench evaluator now maps only a malformed multiplication operand at end-of-input or an immediate second asterisk to the captured successful zero result. Other malformed operands remain errors. This branch is profile-specific and does not change MorphOS expression parsing.

The refreshed native qualification passes 37 supplied post-ReadArgs vectors per CPU (111 total) on 68000/020/040, one image load per CPU, zero shared-image writes, and balanced fixture resources. Receipt: artifacts/cc10-eval-wb31-native-20260928-incomplete-multiply-v1/qualification.json. The 68000, 68020, and 68040 HUNK SHA-256 values are c1ac17a1981b16c16156f07a18349b68f5dac80122af702c3b4d2af399dbbaf9, 5f59d1bdf537ed590d8258e930cbececf8aea3e0690bc60fab024e2f12bcdede, and 9b079b229bd45387b4be84fefbd460890188c04f985adab28e0fdcc9539b5e93.

Fresh original/candidate guest pairs against this refreshed HUNK match for C:Eval 2+ (2 LF), C:Eval 2* (0 LF), and C:Eval 2 ** 3 (0 LF). All three also match return 0 and caller post-System IoErr 0. Receipts: artifacts/workbench31-guest-eval-trailing-plus-candidate-20260928-v2/effect-comparison.json, artifacts/workbench31-guest-eval-trailing-star-candidate-20260928-v2/effect-comparison.json, and artifacts/workbench31-guest-eval-double-star-candidate-20260928-v2/effect-comparison.json. The total recorded Workbench Eval guest pairs is eighteen across two symbols-off HUNK identities; this remains case-level evidence, not full command qualification.

The focused evaluator suite passes 71 tests, including regressions that keep other malformed multiplication operands as errors. Release packaging, complete grammar and diagnostic coverage, installed P/resident lifecycle, and profile admission remain open.

## 2026-09-28 - CC02/CC03/CC04/CC06 native foundation refresh

The current `tests/Commands` managed suite passes 843 tests. A fresh native
foundation qualification passes all 264 supplied vectors across 68000, 68020,
and 68040: 31 startup, 25 argument-boundary, and 32 I/O cases per CPU. The
hash-bound receipt is
`tests/Commands.NativeRoot/bin/Release/net10.0/qualification/3e1bbbbf271c43288b90a1ec8a141256/qualification.json`.
All nine generated HUNK/suite reports pass, each CPU loads one image, fixture
resources balance, and there are no shared-image writes. The startup suite now
checks stdout as bytes; that fixed a false failure where the expected and
actual Latin-1 byte sequences matched but their decoded Unicode strings did
not.

The fixture and host test builds now use a coherent local CopperSharp SDK
surface for the executor/native root, and the native-root output includes its
declared dependency closure for snapshotting. The host-only Shell project
excludes the newly added DOS-native queue-handler source; the separate DOS
Shell project builds successfully. These results are fixture and build
evidence only: original Kickstart/CopperStart, real DOS parser and I/O,
minimum-stack, PURE/resident, packaging, licensing, and shipping gates remain
open. Shipping remains 0/200 commands and 0/246 profiles.

## 2026-09-28 - CC10 Workbench Eval leading-zero guest parity

Three fresh original/candidate Workbench guest pairs now match for `C:Eval
08`, `C:Eval 08+1`, and `C:Eval 09+1`. Each produces the exact output bytes
`30 0A`, returns 0, and leaves caller post-System IoErr at 0. The comparisons
use the hash-bound symbol-free loadable HUNK with SHA-256
`5d51031ab980e334e5cd2064974d7ed9eeeabbf524a2558bdb2f7a09ce51f74e`.

Receipts:
`artifacts/workbench31-guest-eval-08-alone-candidate-symbols-off-20260928-v2/effect-comparison.json`,
`artifacts/workbench31-guest-eval-08plus1-no-spaces-candidate-symbols-off-20260928-v2/effect-comparison.json`, and
`artifacts/workbench31-guest-eval-09plus1-no-spaces-candidate-symbols-off-20260928-v2/effect-comparison.json`.
Together with the prior comparisons, the captured Workbench Eval set now has
21 distinct case-level original/candidate invocation matches across two
symbols-off HUNK identities. There are 23 passing comparison receipts because
the `08 + 1` pair is a duplicate capture, and `2+` was compared on both HUNK identities.
This does not close the complete Eval grammar, diagnostics, ReadArgs contract,
PURE/resident lifecycle, licensing, release packaging, or shipping gates.
Shipping remains 0/200 commands and 0/246 profiles.
