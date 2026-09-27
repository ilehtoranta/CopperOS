# Workbench Relabel: paired original-DOS boot evidence

Recorded 2026-09-13. CC12.Relabel.wb31 remains partial; no shipping or PURE
admission. This extends the [native comparison contract](contracts/Relabel.md)
using the same replacement HUNK, not a differently built command.

## FFS name boundaries, rejection and recovery (2026-09-17)

CC12.Relabel.wb31.names.1 compares four independent original/replacement boots
against disposable writable DOS1 disks. Every boundary is followed by a normal
`DF1: SavedDisk` rename, payload readback and resident removal. The unchanged
replacement matches the original command and FFS handler:

| NAME passed intact to DOS Relabel | Boundary result / IoErr | Label before recovery | Diagnostic bytes |
| --- | --- | --- | --- |
| Thirty `N` bytes | 0 / 0 | All thirty `N` bytes | Empty |
| Thirty-one `N` bytes | 20 / 210 | `FixtureDisk`, unchanged | `object name invalid` + LF |
| Empty quoted string | 20 / 210 | `FixtureDisk`, unchanged | `object name invalid` + LF |
| `Left/Right` | 20 / 210 | `FixtureDisk`, unchanged | `object name invalid` + LF |

ReadArgs succeeds in all four cases. The invalid names are rejected by the
filesystem, not by an invented frontend limit, truncation, default name or path
rewrite. Failed handler BOOL zero is reported through PrintFault(210,NULL).
Every subsequent recovery returns 0/0 with empty output. The payload remains
readable after rejection/success and through SavedDisk: after recovery.

A bounded read-only observation at each Relabel RunCommand entry records the
actual root sector and a hash of all other disk sectors. Before recovery this
proves the accepted 30-byte label is already persisted and the rejected names
have not replaced the label. Root checksum and permitted label/date/checksum
changes are independently checked; every non-root byte remains identical.
Final exported disks contain SavedDisk with intact file contents, protection,
directory topology and allocation bitmap. Both calls reuse one unchanged
resident image; parser/library/private allocations are owned and released,
and the image is removed/freed only after both returns.

Final receipts are under `artifacts/relabel-name-boundaries-20260916-v2/`:
`{name30,name31,name-empty,name-slash}/qualified/comparison.json`. All eight
boot TRXs pass. `controls.json` rejects sixteen corruptions spanning frontend
truncation, argument rewriting, wrong fault number/text, wrong return/error,
missing recovery, lost labels, non-root writes, changed root metadata with a
repaired checksum, bad storage ownership and substituted final output.

- Test DLL SHA-256: `218fff30c98b09d0ad82a4a3e857229b79c12b66dbe3350bc6c056629ec80b60`.
- Emulator DLL SHA-256: `bd748d00eac30bf50f53917fa1380c097998a0cf065f2324e38c4863b1116ee2`.

The first matrix (`relabel-name-boundaries-20260916/`) is retained as incomplete
evidence. An unnecessary Wait 3 after the boundary call prevented recovery and
diagnostic readback within the existing boot bound. The final script removes
that delay and verifies the actual sectors directly before recovery; the bound
and validation expectations were not relaxed. The initial completed boundary
calls are not counted again as qualified cases.

Use `prepare_relabel_boot.py --scenario <case>` and a fresh data fixture from
`prepare_relabel_data_disk.py`, run `RelabelNameBoundariesV4063` with the usual
boot/data receipt variables, then `verify_relabel_name_boundaries.py` per pair
and `verify_relabel_name_boundary_controls.py` for the matrix. Artifact names
retain the preparation date; qualification finished on September 17.

This adds four filesystem-boundary paths, separate from the earlier twenty
parser/argument cases. It does not generalize FFS errors to other handlers,
qualify OFS or all character/name interactions, or close original PURE,
CPU/launch, process-resource and shipping/package gates. Next cover OFS and
remaining handler-specific behavior through the same public DOS boundary.

## Saved FFS volume survives fresh ROM boots (2026-09-16)

CC12.Relabel.wb31.persist.2 starts a new machine for each of the two disks
exported by persist.1. The input is the exact corresponding `*-after.adf`,
bound to its earlier comparison, observations and actual passed TRX; the
earlier persistence comparison is revalidated before use. No RAM, DOS lists,
handler state or command image is carried from the writing machine.

Both read-only boots mount the saved DOS1 disk through original Mount, resolve
`SavedDisk:`, read the 16-byte proof through both DF1: and the volume name, and
read all 1792 bytes of `SavedDisk:nested/guard`. The larger read is bounded and
hashed at the actual DOS Close return, avoiding trace overflow while preserving
byte count, handle/task ownership and success. Its SHA-256 matches the original
owned payload. Both guests progress to the final Wait after the reads. Neither
loads the Relabel image nor calls Relabel. DF1 stays write-protected and its
exported bytes are identical to the persisted input, including metadata.

Evidence: `artifacts/relabel-cold-remount-20260916/qualified/comparison.json`.
Fresh reference/candidate TRXs pass in 23/25 seconds. Twelve controls reject
non-cold mode, a writable or changed disk, unbound input, hidden Relabel calls,
injected guest state, missing volume lookup, corrupt/short guard reads, failed
Close, incomplete progress and an unresolved requester. The verifier checks
the real derivative boot image separately from loaded-command validation;
zero command execution is an explicit requirement, not fabricated invocation
evidence. This adds cold-readback coverage, not extra command option cases.

- Test DLL SHA-256: `318201e54a2a77ae531f59958c96c2b033eb1d2fb9d7a451fe4f2bc1032cb5a6`.
- Emulator DLL SHA-256: `83f305a728d9825228ec8e064c2b42229ac123dfd6468ca492e7b5c7373ffeaa`.

Prepare private boot media with `prepare_relabel_boot.py --scenario cold-remount`
and bind data with `prepare_relabel_cold_remount.py --source <persist.1-directory>
--directory <fresh>/data`. Use each role's boot/data receipt with the existing
environment variables, run `RelabelColdRemountV4063`, then
`verify_relabel_cold_remount.py` and `verify_relabel_cold_remount_controls.py`.
All output paths must be fresh. Original licensed media remain unchanged.

The explicit Mount/ENV: setup and host Exec/device overlays remain the same
limitations as persist.1. This does not qualify automatic DF1 discovery,
physical disk DMA, OFS, other CPU/launch variants, original PURE design or
shipping. Next cover remaining filesystem/name boundaries and errors while
keeping the full platform, ownership and package gates open.

## Writable FFS volume labels persist in sectors (2026-09-16)

CC12.Relabel.wb31.persist.1 now compares two successful renames on an owned,
writable DOS1 data floppy. The original and unchanged replacement both execute
`DF1: FirstDisk`, then `FirstDisk: SavedDisk`, through original DOS ReadArgs,
list lookup and Relabel calls. Both return **0 / IoErr 0**, with handler BOOL -1
and empty diagnostics. This differs from the observed RAM handler's ambient
IoErr 210; success must not force one universal error value.

The fixture uses two connected floppy drives. The private derivative boot disk
stays write-protected. A separate, freshly created DF1 contains only our
16-byte `proof` and 1792-byte `nested/guard` files, with recorded protection
metadata. Original MakeDir/Assign establish an empty RAM-backed ENV:, then
original Mount mounts unit 1 using an explicit DOS1 geometry in RAM. There is
no guest-memory patch or injected filesystem success. Type reads the proof via
the initial volume name and each new name. Both Relabel invocations reuse one
resident image with balanced parser/library/private-storage ownership and no
image writes; removal frees it afterward.

After the unchanged execution bound, the observer exports the device's actual
sector data to a new file. An independent reader checks the initial filesystem,
then proves only root block 880 changed. Root changes are limited to label,
modification timestamps and checksum; the checksum is valid. Every other block
is byte-identical, including directory/file headers, protection, payload and
allocation bitmap. Final volume spelling is `SavedDisk`. Timestamp changes are
allowed metadata and are not compared as command output; command results,
errors, handler arguments and persisted effects are compared between binaries.

Final evidence: `artifacts/relabel-persistent-20260916-v6/qualified/comparison.json`.
Both boot TRXs pass (23 seconds each); `controls.json` rejects twelve corruptions:
unchanged sectors, bad root checksum, writes outside the root, a changed hash
table with a repaired checksum, missing/wrong command effects, wrong parser or
allocation ownership, premature image release and a substituted export hash.
Executor identities come from the v5 isolated build:

- Test DLL: `2e807d5e7441ed3b1fa447f94349e7fd0c65fc119ed0466ca72426c9afa69fbf`.
- Emulator DLL: `afffcb87f27f2ba2d10b83e4c5875c2cfcb67d08a3bedcc455cc973b686425bc`.

The same observer build also passes fresh eight-case baseline boots (31/30
seconds) and twelve baseline controls, recorded under
`relabel-persistent-20260916-v6/baseline-current/qualified/`. The earlier
`baseline/` replay selected obsolete startup media and was rejected by the
startup-hash check; it remains non-qualifying, without changed expectations.

Earlier attempts remain non-qualifying under `relabel-persistent-20260916` and
`-v2` through `-v5`, with explicit `not-qualified.json` dispositions. The first
attempt used the one-drive default; two-drive volume and direct-device probes
still waited before any Relabel invocation. Explicit Mount then exposed a
locale.library requester. The read-only EasyStruct capture identifies its text
as `Please insert volume`, `ENV`, `in any drive`. Establishing ENV: and an
explicit guest mount resolves this fixture setup. This does not establish or
repair automatic DF1 discovery. Their prerequisite TRX passes never counted as
command passes, and their data disks remain unchanged.

Reproduction uses `prepare_relabel_boot.py --scenario persistent-volume` with
the pinned original and native qualification, plus
`prepare_relabel_data_disk.py --directory <fresh>/data`. Set the existing ROM,
Workbench archive and COPY_BOOT_RECEIPT variables and set
`COPPER_AMIGA_RELABEL_DATA_RECEIPT` to the role's `*-data.json`. Run
`RelabelPersistentVolumeV4063`, then `verify_relabel_persistent_boot.py` and
`verify_relabel_persistent_controls.py`. Every output directory/export must be
new. Original binaries and derivative boot media remain outside the repository;
the data-only ADFs contain no licensed reference content.

This is persisted-sector and native DOS/FFS integration evidence on 68000 with
host Exec/device overlays, not a cold-remount or hardware-DMA qualification.
Cold-remount, other filesystem/CPU/launch variants, original PURE classification
and shipping/package gates remain open. The twenty earlier single-caller
parser/argument cases remain separate from these two FFS success paths.

## Captured Process/stack and MemList storage reclaimed (2026-09-16)

Both original/replacement boots now bind the retiring worker's public
`tc_MemEntry` list to four actual FreeMem calls. The recorded allocations are
two 24-byte MemList nodes, one 20-byte allocation rounded to 24, and one
3444-byte Process/stack allocation rounded to 3448. Total reclaimed storage is
**3520 bytes per worker**. The Process address and complete published process
stack lie inside the last allocation. Every span is allocated at RemTask entry,
freed once with its exact request size, and wholly present in a valid free chunk
immediately after that FreeMem returns, before the terminal switch to another
task. Public memory-header free totals, ordered chunks, region bounds and
non-overlap are independently checked.

The first probe (`artifacts/relabel-caller-memory-20260916/`) inspected ownership
at removal and the allocator only at the end of the boot. Those snapshots were
insufficient: later Type/Wait activity can reuse the addresses. They are retained
as intermediate observations, not proof of either a leak or successful release.
The final v2 observer records allocator state at each actual FreeMem return,
so later allocation reuse cannot substitute for or erase the release evidence.

Provider and execution ordering matter here. The public RemTask slot is not a
host gateway; the original ROM routine performs normal cleanup through the
host FreeMem provider. The host deferred-retirement queue is empty at the final
bound. The source audit confirms that the external-ROM overlay policy permits
FreeMem (-210) but not RemTask (-288). The suspected deferred-host retention
path therefore does **not** explain this workload. The host RemTask/native
handoff proof remains an independent unresolved provider obligation.

Original RemTask still uses the retiring **user stack** during these releases,
then performs its terminal switch. The captures record that fact rather than
inventing an earlier supervisor-stack handoff. Matching this original sequence
does not authorize arbitrary host cleanup of a still-active stack and does not
relax the separate host reaper's safety gates. No scheduler, CPU or allocator
production code was changed to obtain a pass.

Final evidence: `artifacts/relabel-caller-memory-20260916-v2/`:

- `reference.trx`, `candidate.trx` and `qualified/comparison.json` bind the fresh
  pair, exact binary/media/executor and imported verifier dependencies.
- `controls.json` rejects 17 storage-evidence corruptions, including missing/
  wrong-owner/early frees, wrong rounding, missing actual free chunks, substituted
  final snapshots, corrupt allocator totals, wrong providers and a fabricated
  supervisor frame. Mutated duplicate call records are updated together so the
  semantic checks are exercised rather than only duplicate-record equality.
- `retirement-controls.json` and `concurrent-controls.json` reject the existing
  20 self-removal and 21 concurrency/refusal corruptions on this same fresh pair.
- Test DLL SHA-256:
  `2784e93adcaeb95cfcf05d8b53e243d6dbe1a15694d0b192d6236b1db45d08a2`.
- Emulator DLL SHA-256:
  `d1ccf4870b62cb30a390b4799f1b4a364837077452b6a8c1993356901de930cc`.

Use the same `active-replace` media and
`RelabelResidentCallerRetirementV4063` with the expanded read-only observer,
then `verify_relabel_caller_memory.py` and
`verify_relabel_caller_memory_controls.py`. Original media remain private,
execution limits are unchanged, and both command HUNKs are unchanged.

This qualifies reclamation of the **captured four tc_MemEntry spans**, including
the Process/stack, after normal command return under this provider composition.
It is not a complete resource ledger for every Process-associated object,
packet, callback, notification or pool; nor does it qualify forced death or
CopperOS host RemTask native handoff. Those obligations, original PURE design,
remaining launch/platform and packaging remain open. Shipping counts stay zero.

## Normal background caller retirement (2026-09-16)

Fresh original/replacement boots extend the active-replacement-refusal workload
with observation of the background caller's terminal Exec `RemTask(NULL)`.
After its protected Relabel invocation returns, no command invocation remains
active. The resident image is still live and its existing registry node has
use count 1. Both runs switch to another task without returning to the removed
caller's frame. The task is absent from valid ready/wait lists immediately
after the switch and in every later sampled topology through final cleanup.

The parent then executes the recovery Relabel successfully, using the retained
segment after that switch, and removes the same registry node at count 1.
This adds evidence of normal caller self-removal and subsequent resident reuse
beyond merely seeing a task disappear between periodic snapshots. The original
RemTask entry and next other-task cycles are 32742352 and 32743784; the candidate
cycles are 32763748 and 32765182. Exact absolute cycles are capture facts, not
cross-binary equality requirements.

The observer only reads the public entry/register frame, published task lists
and the known resident node. It never calls RemTask itself, alters a task,
changes a result or forces a return from the terminal operation. The existing
original DOS/Shell plus host Exec/device boundary remains: this is not an
untouched-ROM Exec implementation test. Command allocations, parser objects
and DOS leases already balance before retirement. Complete process storage
reaping, pending packets/notifications, abrupt task death and removal while a
command is still active are explicitly **not** qualified by this case.

Evidence is `artifacts/relabel-caller-retirement-20260916/`:

- `reference.trx`, `candidate.trx` and `qualified/comparison.json` bind the fresh
  pair, unchanged private active-replacement media, command hashes and executor.
  The receipt includes the imported concurrency verifier's hash as a dependency.
- `controls.json` rejects 20 retirement corruptions: missing/wrong owner or
  terminal frame, return after removal, active invocation, lost image/count/node,
  absent/wrong switch, retirement/recovery order, invalid task lists, later task
  reappearance/execution and missing later observations.
- `concurrent-controls.json` rechecks this same fresh pair with all 21 existing
  overlap/active-replacement controls. `prior-idle-controls.json` and
  `prior-failed-controls.json` re-verify the retained successful and failed
  replacement captures with 24 and 56 controls; they are not fresh boots.
- Test DLL SHA-256:
  `4d7f3a52b3cc51cbe51f88d5699d3b50bd39812a101f2e283678eb79a154711f`.
- Emulator DLL SHA-256:
  `919ff7333c42716251aaf8112637cd23651af72fa0611f5d4e557db4db6900e6`.

Reproduce with the unchanged `active-replace` derivative scenario and
`RelabelResidentCallerRetirementV4063`, then
`verify_relabel_caller_retirement.py` and
`verify_relabel_caller_retirement_controls.py`. Use fresh output directories
and an isolated build; the original media and derivative bytes remain private.
The instruction bound, input delivery and command HUNK are unchanged. No
production command, Shell, scheduler, CPU or device implementation changed.

Next distinguish process-storage reclamation from task unlinking, and qualify
applicable termination/failure ownership without inventing safe forced-death
behavior that the original design does not provide. Original PURE design,
remaining launch/platform and package gates remain open. This does not promote
shipping counts or add to the twenty already counted Relabel option scenarios.

## Failed replacement retains the usable command (2026-09-16)

Two new paired original/replacement workloads cover a missing replacement file
and an existing non-executable replacement file. Both keep the same resident
registry node, use count 1 and original loaded segment after the failed attempt.
One Relabel invocation runs before the attempt; two further invocations use the
retained image afterward. Every invocation returns 0/210, with matching public
DOS operations, payload readbacks and invocation-owned cleanup. Final REMOVE
returns 0/202 and frees the retained segment exactly once after all three calls.

The unchanged original Workbench Resident has distinct failure stages:

| Replacement source | Observed failure | Resident result / IoErr | Exact diagnostic |
| --- | --- | --- | --- |
| `C:MissingRelabel` | `Lock` returns zero / 205; no LoadSeg or acquired lock to release | 5 / 205 | `object not found` plus LF |
| `RAM:relabel-proof` (16-byte text file) | Lock/Examine succeed, UnLock releases that lock, LoadSeg returns zero / 212; FreeArgs preserves 212 | 5 / 205 | `object not found` plus LF |

In the invalid-file path, original Resident passes 205 to PrintFault after
parser cleanup, despite the loader's error 212. The verifier preserves both
observations; it must not substitute 212 for the helper's public result/error.
This is original-helper behavior exercised with both Relabel binaries, not a
change or qualification of CopperOS's separate MorphOS Shell-owned Resident.
Neither failed attempt calls AddSegment, RemSegment or unload on the retained
command. Lookups before/after the attempt and at final removal bind the same
registry node and segment. The image hash stays unchanged through final free;
all six replacement allocations, three parser objects and three DOS leases
balance. This does not measure arbitrary internal loader allocation failure.

Evidence is `artifacts/relabel-failed-replace-20260916/`:

- `missing/qualified/comparison.json` and `invalid/qualified/comparison.json`
  bind four passing boot TRXs, exact private media, the unchanged original and
  candidate HUNKs, observer build and current verifier sources.
- `controls.json` rejects 56 corruptions (26 missing-file, 30 invalid-file),
  covering lost retention, wrong node/segment/count, unexpected unload, stale
  or extra calls, missing/early/wrong-owner release, image changes, parser/DOS
  cleanup, output/payload bytes, lock ownership and the 212-to-205 distinction.
- `prior-idle-qualified/comparison.json` and `prior-idle-controls.json`
  re-verify the preceding successful replacement captures and their 24
  corruptions after extracting shared call/ownership/readback verification.
  These are retained captures, not new successful-replacement boot runs.
- Test DLL SHA-256:
  `e94966a27645c0f69f00ef5d73abea15e5e93ca5c30e16ad4235a2b6e3880df4`.
- Emulator DLL SHA-256:
  `c0ad2f1c70ce718e8125087b97be67c93948b40226125b671558c1847b72bebc`.

Reproduce with `prepare_relabel_boot.py --scenario replace-missing` and
`--scenario replace-invalid`, separate fresh private/media directories and
`RelabelFailedResidentReplacementV4063`. Run
`verify_relabel_failed_replacement.py` for each pair, then
`verify_relabel_failed_replacement_controls.py` for both. The invalid source is
the actual guest-created text payload, already read back before the attempt;
no injected DOS return or patched command simulates either failure. The boot
bound, 68000 model and original DOS/Shell plus host Exec/device boundary are
unchanged. No command, Shell, CPU or device production implementation changed.

This closes these two source-failure retention cases. Memory exhaustion and
other loader failures, applicable task/process retirement and failure paths,
original PURE classification, other launch/platform modes and packaging remain
open. Twenty distinct Relabel option scenarios remain the earlier count;
Resident-helper lifecycle workloads do not inflate that count or admit a
shipping profile.

## Successful replacement after callers return (2026-09-16)

Original Relabel 37.2 and the unchanged 68000 replacement now pass an idle
replacement workload. Original Shell registers C:Ed, calls Relabel once, runs
`Resident Ed C:Ed REPLACE PURE`, calls the new image twice, then removes it.
The replacement helper returns 0/0. It loads a second image before unloading
the first, and updates the existing registry node without AddSegment or
RemSegment during replacement. Lookups before/after replacement identify the
same node at count 1 and the corresponding old/new segment. Final REMOVE
returns 0/202; the preserved ambient error is observed original behavior.

Both images briefly coexist at disjoint allocations. The observer now keeps a
separate load generation, relocated code hash, allocation and free entry/return
for each image; CPU writes are checked across every still-live image. Each
image remains byte-identical through its release, including the old image
after loading the new one. This fixes the observer's previous single-image
tracking limitation without changing the command or runtime. Unsupported
loading while a command caller is active, or calling an older live generation,
still fails explicitly; it is not silently treated as covered concurrency.

The first call uses generation 1; both subsequent calls reuse generation 2.
All three return 0/210, with matching public DOS operations, three exact payload
readbacks under successive labels and empty command/helper diagnostics.
The replacement owns and releases six allocations, three RDArgs objects and
three DOS leases. Both image allocations are freed once by the original owner:
old release is inside replacement, new release is inside final removal. The
verifier checks actual RunCommand intervals against load/free lifetimes and
does not mistake later address reuse by Type or Wait for command execution.

Evidence is `artifacts/relabel-idle-replace-20260916/`:

- `reference.trx` and `candidate.trx`: both pass under one isolated observer build.
- `qualified-v2/comparison.json`: final paired result, complete observations,
  source/media/HUNK/build bindings and normalized public DOS operations. The
  initial `qualified/` receipt predates the final operation comparison and is
  retained as intermediate evidence.
- `controls.json`: 24 corruptions reject, including missing/early/wrong-owner
  releases, either image's hash, stale-segment calls, missing reuse, extra calls,
  wrong node/count, altered helper result/error, parser/DOS cleanup and readback.
- `active-regression/qualified/comparison.json` and `active-regression/controls.json`:
  fresh original/replacement active-refusal boots pass on this generation-aware
  observer, with 21 corruptions rejected. The same earlier private media bytes
  are reused, with fresh captures and receipts bound to the current executor.
- `prior-removal-controls.json`, `prior-overlap-controls.json`,
  `prior-baseline-controls.json` and `prior-extended-controls.json`: retained
  captures pass the updated media verifier, rejecting 20, 12, 12 and 20
  corruptions respectively. These are verification reruns, not fresh boots.
- Test DLL SHA-256:
  `1bb043dc528be63cdb41ad4f98d865fdc3a4e576a2308aa24efb170ef9387d4f`.
- Emulator DLL SHA-256:
  `6283d8623326ff9a6bff337e2e46865876e90ce6a9edb0f7e5e2b5f8ef389a4e`.

Use `prepare_relabel_boot.py --scenario idle-replace` and
`RelabelIdleResidentReplacementV4063` with fresh private/output directories,
then `verify_relabel_replacement_boot.py` and
`verify_relabel_replacement_controls.py`. Both verifier tools take explicit
artifact paths; they never alter the captured runs or private media. Execution
limits remain 32 chunks of 250,000 instructions. No keyboard intervention is
required by this workload.

This proves replacement with another load of the same command HUNK while idle.
Failed replacement loading and retention of the previous usable command are
next, followed by applicable task-death/failure/lifecycle, original PURE
classification, launch/platform and package gates. It does not qualify
replacement by an arbitrary different command or successful replacement while
active. The original DOS/Shell plus host Exec/device boundary and distinction
from CopperOS's MorphOS Shell-owned Resident remain unchanged. Shipping stays
0/200 commands and 0/246 profiles; this workload does not increase the twenty
distinct single-caller option scenarios already counted.

## Resident replacement refused while a caller is active (2026-09-16)

Both original Relabel 37.2 and the unchanged replacement pass
`Resident Ed C:Ed REPLACE PURE` during the protected background call. Original
Resident returns 10 with IoErr 202 and exactly `object is in use` plus LF.
Unlike ordinary REMOVE (5/202), this path rejects before any LoadSeg,
AddSegment, RemSegment or unload. FindSegment retains the same node/segment at
use count 2; the RAM overlap and later recovery still succeed. After all three
calls return, ordinary removal succeeds at count 1 with 0/202 and one segment
release. All six replacement allocations, three parser leases and three DOS
leases balance; the shared code remains unchanged.

Evidence is `artifacts/relabel-active-replace-20260915/` (the run began September
15; this record was completed September 16). `qualified/comparison.json` binds
both passing TRXs, exact output/state and executor/media identities.
`controls.json` rejects 21 corruptions, including missing REPLACE, substituted
REMOVE result level, premature loading/registry mutation, incorrect retained
node/count, lost IoErr and altered diagnostics. The preceding removal and
plain overlap captures re-verify with 20 and 12 controls respectively; those
are verifier regression checks, not fresh boots. Test DLL SHA-256 is
`a18079f49bb427019d48dbaa0ffbc8ea824897d4091e5a7de38ab84237703d11`;
emulator DLL SHA-256 is
`ee1898cde7330634065d61d7759e8a0ebc0e1a42f66e92492cc24b919deaaf39`.

Reproduce with `prepare_relabel_boot.py --scenario active-replace`, fresh
private/output directories and
`RelabelActiveResidentReplacementThroughHostKeyboardV4063`, followed by the
concurrent verifier and its controls. The same redirected intermediate Type
and production host-input path as the final removal fixture are retained.
No command, Shell, CPU or device implementation changed for this slice.

This tests Relabel under the unchanged Workbench Resident helper and original
DOS/Shell, with host Exec/device takeover. It does not qualify CopperOS's
separate MorphOS Shell-owned Resident implementation. Successful replacement,
forced removal, task death, original PURE classification, other launch/platform
modes and package admission remain open. The observer explicitly rejects a
second watched image loaded while callers are active instead of overwriting
its tracking and claiming multi-segment coverage.

## Resident removal refused while a caller is active (2026-09-15)

Both original and replacement Relabel pass an extension of the concurrent
workload: the parent runs original `Resident Ed REMOVE` while the background
Relabel is waiting at its protected-disk requester, before starting its own RAM
relabel. Removal is refused, and the same resident node and loaded code segment
remain available for the parent's invocation. After Cancel and recovery, the
parent retries removal and the segment is freed. No command or OS implementation
was edited for this step; the new work is guest execution and verification.

| Operation | Result | Final IoErr | Observed registry/output |
| --- | ---: | ---: | --- |
| Resident REMOVE during the background call | 5 | 202 | RemSegment returns zero at use count 2; output is exactly `object is in use` plus LF. |
| Lookup for the next parent invocation | Non-null node | Not treated as command result | The same registry node still refers to the same segment at count 2. |
| Resident REMOVE after all three Relabel calls | 0 | 202 | RemSegment succeeds at count 1; the segment allocation is released once. |

The counts are captured registry values, not the active invocation counter.
At the refused removal only the background Relabel is active; the parent is
running Resident. The subsequent two Relabel callers still overlap, with
separate RDArgs and two disjoint live replacement allocations each. Their
three results remain 20/214, 0/210 and 0/210. All six direct allocations and
three Relabel parser/DOS leases close correctly. Both volume payloads and all
three Relabel output files are read back, in addition to Resident's diagnostic.
The verifier binds each Resident RunCommand argument tail, return and registry
calls to the relevant owner interval. It requires the retained-node lookup,
refusal without freeing code, later use of that segment, and final removal
after every caller returns. No forced removal or replacement is exercised.

The first probe, retained under `artifacts/relabel-active-remove-20260915/`,
recorded the refusal and successful overlapping RAM call but did not finish.
An additional console window opened before the Cancel sequence; the requester
remained pending and the script-wait loop eventually overflowed the bounded
observation trace. This is failed evidence, not a removal or cancellation pass.
The final startup redirects the intermediate Type command's display to NIL:
while keeping the same actual payload reads and assertions. The later
diagnostic/readback display is unchanged. This controls console interference
in the fixture; it does not prove a general focus-routing fix or qualify the
unredirected case. Execution limits and the input sequence are unchanged.

Final evidence is `artifacts/relabel-active-remove-20260915-v2/`:

- `reference.trx` and `candidate.trx` pass under the same build from the initial
  probe's isolated build directory. Only fresh derivative startup media changed.
- `qualified/comparison.json` and the full observations bind the command,
  media, executor and verifier hashes, all ownership/lifetime comparisons and
  the exact in-use warning and preserved successful-removal IoErr.
- `controls.json` rejects twenty corruptions: the twelve prior concurrency
  controls plus missing/accepted active removal, wrong count/node, missing
  retained lookup, wrong Resident warning level, cleared retry error and
  changed in-use diagnostic.
- `prior-concurrent-controls.json` re-verifies the preceding concurrency pair
  and its twelve controls; it is not a fresh boot of that scenario.
- `failed-probe-rejection.json` records rejection of the actual first failed TRX.
- Test DLL SHA-256: `7602fe9999b2e2099a25b519cd81c7533eec205a38434d1f6436ab56bcaeb1bf`.
- Emulator DLL SHA-256: `ab37c9555846b26543e6a1aa64dddfc531ac4ac4f23d7809d763ca3ce07da677`.

Use `prepare_relabel_boot.py --scenario active-remove` and
`RelabelActiveResidentRemovalThroughHostKeyboardV4063` to reproduce with fresh
media/output directories and the pinned licensed inputs. The concurrent
verifier/control tools below now admit this explicit scenario and require its
additional checks; the plain concurrency scenario retains its original scope.
This adds ordinary active-removal refusal evidence under original DOS/Shell
with host Exec/device takeover. Active replacement, forced removal, task death,
other launch/platform modes, original purity classification and shipping
admission remain open.

## Two tasks sharing one resident segment (2026-09-14)

Both original Relabel 37.2 and the unchanged 68000 replacement now pass a
concurrent resident workload. Original Shell creates a background Execute task;
that task enters the protected-DF0 requester. The parent lowers its priority
with original ChangeTaskPri and relabels RAM while the background command is
still active. The input fixture waits until two overlapping RunCommand owners
have been observed before sending Cancel. A guest script completion marker
then lets the parent run a recovery relabel and remove the resident command.
No fixture-supplied result, task register, or private DOS state substitutes for
the guest's execution or completion marker.

Three invocations use one LoadSeg/AddSegment and two distinct task addresses.
The RAM invocation begins and returns strictly inside the protected invocation's
lifetime. Recovery uses the parent's task after the protected call returns.
The guest uses a script marker rather than an assumed host delay; the existing
32-by-250,000-instruction cap is unchanged. Both commands produce these results:

| Invocation | Result / final IoErr | Evidence |
| --- | --- | --- |
| Background `DF0: MustNotChange` | 20 / 214 | Real Cancel, exact `disk is write-protected` plus LF; protected media unchanged. |
| Parent `RAM: ConcurrentVolume` during the background wait | 0 / 210 | Empty diagnostic; original Type reads the unchanged volume payload. |
| Parent `RAM: Recovery` after background completion | 0 / 210 | Empty diagnostic and a second exact payload readback. |

All three parser and DOS-library leases close in their owning invocation. The
two overlapping RDArgs pointers are distinct. The replacement holds its own
eight-byte ReadArgs result array and drive scratch allocation in each caller;
all four cross-caller pairs overlap in time but have disjoint address ranges.
All six direct allocations across the three calls have matching owner/address/
size releases. Original Relabel makes no directly observed Exec allocations.
The source image remains unchanged, and the single resident segment is removed
and freed after all callers return. This advances shared-image concurrency
evidence; it does not establish original protection flags, arbitrary reentrancy,
task-death behavior, or safety of registry changes while a command is active.

Final evidence is `artifacts/relabel-concurrent-20260914/`:

- `reference.trx` and `candidate.trx` pass under the same observer/runtime build.
- `qualified-v2/comparison.json` and its full observations bind actual private
  media, HUNK/code identity, task/segment lifetimes, ordered public DOS behavior,
  result/errors, input delivery, ownership and byte-exact readbacks.
- `controls.json` rejects twelve corruptions, including a second segment,
  unbound invocation time, aliased RDArgs, overlapping allocation addresses,
  cleanup assigned to the other task, early/missing segment free, changed image,
  premature Cancel, wrong requester result and a changed diagnostic byte.
- The earlier `qualified/` is retained as the initial verifier pass; v2 adds
  explicit parser/handler lifetime ordering, result-array ownership and ordered
  DOS operation comparison without rerunning or replacing the captures.
- Test DLL SHA-256: `c289c95caf62e8ec30c2a4977a570637d142bc145375200b8213922bfabddece`.
- Emulator DLL SHA-256: `10c34fc6c07fdd94ee77914c7c3df2d7ade935a33064013b8ed0069567025cb0`.

Reproduce with `prepare_relabel_boot.py --scenario concurrent`, fresh public
receipt/private media directories and the pinned inputs below. Build the sibling
test project in an isolated artifacts directory, select each new receipt, and
run `RelabelConcurrentResidentThroughHostKeyboardV4063`. Then run
`verify_relabel_concurrent_boot.py` with the receipt/TRX directory, bound test
DLL, native qualification and a fresh output directory. Challenge the accepted
observations using `verify_relabel_concurrent_controls.py`. Both tools expose
their required arguments through `--help`.

No command, CPU or device implementation was edited for this step. The observer
adds per-call entry/return cycles and defers input until overlap is observed;
the startup script supplies concurrency through normal Shell/DOS facilities.
The assembly hashes above identify this build separately from the preceding
cancellation build. Host Exec/device takeover remains explicit. Twenty existing
single-caller scenario comparisons remain distinct from this new three-call
concurrency workload. Shipping and full PURE admission remain open.

## Protected-disk cancellation completed (2026-09-14)

The original Workbench command and unchanged replacement now complete the
previously pending protected-DF0 case. Both return **20**, leave **IoErr 214**,
and write exactly `disk is write-protected` followed by LF. Three subsequent
help/EOF/continuation/recovery cases also match. There are now twenty distinct
paired scenarios: the prior nineteen plus protected-disk cancellation. These
four new paired executions include three previously covered help cases.

The failure was in the emulator's input service lifecycle. `IsInstalled` used
the presence of its BeginIO gateway, which is intentionally removed while
forwarding to original ROM code. The boot loop calls `TryInstall` between
instructions; it reinstalled the service during that interval and interrupted
the forwarded call. The service now uses ownership of its allocated state as
the installation criterion. No CPU, CIA timing, command code, DOS private
structure, requester return value, or disk protection was patched.

The regression in sibling `InputDeviceServicesTests` reproduces boot discovery
while the original BeginIO slot is exposed. It fails before the fix
(`artifacts/relabel-input-forward-regression-20260914/before.trx`). All eighteen
keyboard/input service tests pass afterward. This is a host device adapter fix;
no full chipset/CPU suite or hardware timing qualification is claimed.

The original raw-CIA cancellation attempt remains failed evidence in
`artifacts/relabel-cancel-20260913/`. CopperScreen normally tries the installed
keyboard service first and only falls back to CIA input if it declines the key.
The separate `RelabelCancelRequesterThroughHostKeyboardV4063` fixture follows
that accepted production service path with no raw-CIA fallback. Left-Amiga+B
is the documented requester Cancel shortcut; see
[Intuition keyboard control](https://wiki.amigaos.net/wiki/Intuition_Keyboard).
The trace records key down/up codes 0x66, 0x35, 0xB5, 0xE6 and qualifiers
0x40, 0x40, 0x40, 0. Each event reaches a guest input handler; B-down is consumed,
and the real EasyRequestArgs returns zero before B-up. The test never creates
an IDCMP reply or supplies a command result. Earlier host-input diagnostic
captures remain failed under their old builds; they are not promoted by this fix.

Final protected-case evidence is in `artifacts/relabel-input-forward-fixed-20260914/`:

- `reference.trx` and `candidate.trx` both pass under the same fixed executor.
- `qualified/comparison.json` verifies original/replacement HUNK and loaded-code
  identities, actual private disk blocks, four exact output readbacks, two
  volume payload readbacks, registered segment reuse/removal/free, three parser
  leases and four DOS leases per binary. The replacement balances all seven
  directly observed allocations; the original uses none directly.
- `controls.json` rejects eight new cancellation corruptions and the twelve
  prior extended/incomplete-trace controls. Three positive pairs are checked.
- `device-controls.trx` records the eighteen focused device tests.
- `baseline-reference.trx` and `baseline-candidate.trx` rerun all eight baseline
  cases under the fixed runtime. `baseline-qualified/comparison.json` passes
  full comparison; `baseline-controls.json` rejects twelve corruptions.
- Test DLL SHA-256: `31dd611627abfc65d72ad9aac4b6572f9a352a6922c882140cee3984fd500947`.
- Emulator DLL SHA-256: `78a03255d6711f51d141b526de58b7ca375c841dff00f2cb4f3debb78dee0800`.

Reproduce the boot with the existing `help-handler-media` receipts, the same
licensed ROM/archive variables described below, and the new host-keyboard test
selector. Run `verify_relabel_extended_boot.py --scenario help-handler` with
explicit `--reference-trx`, `--candidate-trx`, `--media-directory`, bound test
DLL, native qualification receipt and a fresh output directory. The new path
requires real input delivery, packet correlation, requester completion and
the protected diagnostic in addition to all existing media/ownership checks.
Run `verify_relabel_extended_controls.py` with the original extended directory
and `--cancel-directory` pointing to the new qualified observations.

This closes the bounded cancellation gap, not the whole profile. Original
PURE classification, concurrent same-segment calls, task-death cleanup,
Workbench launch, further handler/parser cases, other real-OS CPUs, MorphOS
correspondence and packaging remain required. Host Exec/device takeover is
still part of the execution provider. The historical pending captures below
explain the investigation and do not describe the current fixed result.

## Additional argument/help coverage and protected-disk wait (2026-09-13)

Eleven further original/replacement command pairs pass on the unchanged 68000
HUNK: eight argument cases and three help cases. Evidence is under
`artifacts/relabel-extended-20260913/final-arguments/` and `final-help/`.
Each directory's `comparison.json` binds the TRXs, media, native HUNK and full
observations. Both use the original boot test assembly identified below.

| Additional input | Result / final IoErr | Exact redirected output |
| --- | --- | --- |
| `drive=RAM: name=Lower` | 0 / 210 | Empty |
| Repeated `DRIVE` keyword | 20 / 118 | `wrong number of arguments` + LF |
| Repeated `NAME` keyword | 20 / 118 | `wrong number of arguments` + LF |
| Unknown surplus token | 20 / 118 | `wrong number of arguments` + LF |
| Quoted name with `*"` escape | 0 / 210 | Empty; the actual new label contains `"`. |
| Quoted name with `**` escape | 0 / 210 | Empty; the actual new label contains `*`. |
| Empty quoted NAME | 20 / 210 | `object name invalid` + LF; ReadArgs succeeds and Relabel rejects the empty name. |
| Recovery after argument failures | 0 / 210 | Empty |
| `?` with redirected NIL: input | 20 / 116 | `DRIVE/A,NAME/A: required argument missing` + LF |
| `?` with redirected `RAM: HelpVolume` continuation | 0 / 210 | `DRIVE/A,NAME/A: `, including trailing space and no LF |
| Recovery after help | 0 / 210 | Empty |

Original Type verifies payload access through Lower, the quote/star labels and
Recovery in the argument scenario, then HelpVolume and Recovery in the help
scenario. All eleven output files are independently read, including empty files
and the prompt without a trailing LF. Type may read one terminal EOF for those
files; the verifier still requires the exact metadata byte count. Three failed
argument parsers and one help EOF parser own no returned RDArgs. The replacement
balances thirteen direct allocations in the argument scenario and five in help.
Each pair uses one registered segment per binary, then removes and frees it.

The help trace exposed an evidence-checking issue: after removal, Type and Wait
reuse the command's old segment address. Verification now limits RunCommand
identity to the registration/removal interval, rather than treating the address
as a permanent identity. Payload opens are likewise bounded to their particular
MatchFirst readback, so a later read of the same basename cannot substitute.
`baseline-regression/` re-verifies the earlier eight cases under the factored
media/lifetime checks; `baseline-controls.json` still rejects all twelve earlier
corruptions. New `controls.json` rejects ten extended-case corruptions and both
actual incomplete protected-disk traces. No new command build or source change
was needed for these successful comparisons.

The protected-disk scenario is **not qualified**. In the initial paired
`help-handler-*.trx` runs, both commands reach DOS Relabel with `DF0:` and
`MustNotChange`, then remain active at the unchanged boot bound with zero
completed invocations. Later read-only packet/requester observations establish
the original's precise path:

1. The CLI sends ACTION_RENAME_DISK (9) to the floppy handler.
2. The handler replies on the same packet/message to the CLI port with result 0
   and error 214 (disk write-protected). Media bytes remain unchanged.
3. DOS does not return to the command within the bound. An Intuition
   `EasyRequestArgs` call is pending. No public ErrorReport vector entry was
   observed; that does not mean DOS cannot call its internal reporting code.

`protected-wait.json`, produced by `record_relabel_protected_wait.py`, explicitly
records `status=incomplete`, zero completed commands and no shipping/PURE
approval. It binds `dialog-reference.trx` and its separate observer build. The
earlier `requester-*` and `packet-reference` captures remain distinct stages.
These tests have terminated and disposed their guests; none is a live wait
handle. A readiness-test pass is not a protected-command pass, and the handler's
214 is not yet the command's final IoErr/result. Next provide a controlled input
event to cancel the real requester, then compare final output, return/error and
cleanup for both binaries. Do not patch the return value, disable write
protection or silently omit this requirement.

Preparation now accepts `--scenario arguments`, `--scenario help`, and the
retained `--scenario help-handler`. Use `verify_relabel_extended_boot.py` and
`verify_relabel_extended_controls.py` for the new complete scenarios; the earlier
verifier still owns the unchanged baseline. All use fresh output directories.

## Result and exact scope

The original Relabel 37.2 and the replacement each pass eight autonomous Shell
invocations on disposable Workbench disks. Four successful label changes each
have independent original-Type readback through the new volume name. Four
failures each have their redirected output independently read back after the
resident segment is removed. The paired verifier compares exact bytes, ordered
public operations, return levels and final process errors.

| Case | Shell arguments (redirection omitted) | Result | IoErr after RunCommand | Observed effect/output |
| --- | --- | ---: | ---: | --- |
| Positional/quoted name | `RAM: "First Volume"` | 0 | 210 | New volume path reads the original 16-byte payload. |
| Reordered keywords/equals | `NAME="Second Volume" DRIVE=RAM:` | 0 | 210 | New volume path reads the same payload. |
| Assign lookup | `RLC: AliasVolume` after `Assign RLC: RAM:` | 0 | 210 | FindDosEntry includes assigns; new volume path reads the same payload. |
| Drive without colon | `RAM Trimmed` | 20 | 0 | Looks up `RA`, finds no entry, emits `Invalid device or volume name` plus LF. |
| Invalid new name | `RAM: "Bad:Name"` | 20 | 0 | No list/mutation call; emits `':' not legal character in volume name` plus LF. |
| Missing argument | `DRIVE=RAM:` | 20 | 116 | Original ReadArgs fails; exact fault text is `required argument missing` plus LF. |
| Surplus argument | `DRIVE=RAM: NAME=Ignored EXTRA` | 20 | 118 | Original ReadArgs fails; exact fault text is `wrong number of arguments` plus LF. |
| Recovery | `RAM: FinalVolume` | 0 | 210 | New volume path reads the unchanged payload after all failures. |

Success IoErr 210 is observed, not cleared or reinterpreted as failure. In this
boot environment DOS Relabel returns nonzero with that ambient error. The
replacement preserves it exactly. This does not establish that every handler
or platform leaves the same successful IoErr.

Every successful ReadArgs has one matching FreeArgs (six per binary); neither
failed parser is freed. All eight command DOS library leases close. The original
uses no directly observed command Exec allocations; the replacement's thirteen
allocations have thirteen matching address/size releases. This counts direct
command storage, not all hidden DOS/handler allocations.

There is one C:Ed LoadSeg, one matching successful AddSegment, eight calls using
that segment, successful RemSegment and release of its allocation after the last
call. Both images remain unchanged. `Resident C:Ed PURE` deliberately exercises
sequential shared-segment reuse; it is not original protection-bit evidence or
full PURE qualification. Maximum active invocations is one, so concurrency,
replacement while active and task-death cleanup remain untested here.

## Execution boundary and provenance

The machine boots a licensed 40.63 Kickstart ROM with the unchanged existing
32 x 250,000 instruction bound: A500 PAL, 512 KiB chip RAM, 2 MiB real fast RAM,
accurate 68000 and live Agnus DMA. The observer checks 154 original DOS vectors
without host gateways after DOS publication. The emulator still uses its
assembly-bound host Exec/device takeover policy: this is original DOS/Shell
integration, not a wholly original native operating system or native-handler
qualification. No command outcomes are supplied by the observer, and no CPU
state or private DOS structures are patched to continue execution.

The original Workbench archive and ADF have the inventory's pinned hashes.
Only S/Startup-Sequence and the already allocated C/Ed carrier blocks differ
in each private derivative. The verifier independently opens both original and
derivative media, checks actual startup and HUNK bytes, and compares every other
disk block. Type, Shell, Resident and the other helpers are unchanged. The boot
drive stays write-protected; label mutations affect the disposable guest RAM
volume. C:Ed is only a fixture carrier, not an installation/package decision.

| Input | SHA-256 |
| --- | --- |
| Original Relabel 37.2, 584 bytes | `163ea95df394d2c800a161befae0bea664cb6b8a07dffc2b2b1af3141588a3ff` |
| Replacement 68000 HUNK, 1748 bytes | `60726c52a74d4b046f020ac51cc7fedcc1a21b3b870e5aeacfd47b08c9760df1` |
| Boot test assembly | `86572d3d2a8baaa526bccb184e9efecf134caaca707ab24755e85716098fb871` |
| Emulator assembly | `7d0cb597e55bd72340127f0de9c0865774f05c0dc4fc01b42165a8ac5d3b3b39` |

The verifier relocates each HUNK at its observed load address before comparing
loaded code hashes. Pointer addresses and private allocation differences are
normalized; allocation/argument/library ownership is checked separately. No
normalization is applied to output bytes, lookup strings, flags, handler
arguments, return levels or final errors.

## Durable evidence and reproduction

Final evidence is under `artifacts/relabel-boot-20260913/`:

- `qualified/comparison.json` binds both TRX files, media receipts, executable
  hashes and extracted observations. Eight paired comparisons pass.
- `qualified/reference-observations.json` and `candidate-observations.json`
  contain the complete 1005-event original trace and the replacement trace.
- `reference-diagnostics.trx` and `candidate-diagnostics.trx` are the actual boot
  test results; `diagnostic-media/` holds receipts and the LF-only startup text.
- `controls.json` rejects twelve corruptions: wrong ambient error, missing parser
  release, list flags, volume name, payload, parser diagnostic, registration,
  removal, segment release, loaded-image identity, shared-image write and overflow.
- `build.log` and `build/` retain the isolated observer build. Compilation passes
  with three existing dependency/analyzer warnings. Earlier simple boot traces
  and verifier development directories remain separate from the final receipt.

The observer extension is in the sibling repository:
`../MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosPassiveBootReadinessTests.cs`.
It adds the Relabel test selector, reads list/mutation arguments and final
RunCommand errors, and preserves the existing passive observation model. No
CPU, chipset, DOS, handler or Shell implementation changed in this step.

Reproduce with fresh artifact/private-media directories:

1. Run `tools/Commands/prepare_relabel_boot.py --help` and supply the pinned
   archive, private original HUNK, current native qualification receipt and two
   new directories. It creates paired private ADFs and public hash receipts.
2. Build the sibling `CopperMod.Amiga.Tests.csproj` in Release with an isolated
   `--artifacts-path`. Set `COPPER_AMIGA_KICKSTART_ROM` to the licensed ROM,
   `COPPER_AMIGA_KICKSTART_VERSION=3.1`, and `COPPER_AMIGA_WORKBENCH31_ARCHIVE`
   to the pinned archive. Set `COPPER_AMIGA_COPY_BOOT_RECEIPT` to each new media
   receipt in turn and run `RelabelDerivativeBootProgressV4063`, saving fresh TRX
   results. Despite the legacy environment variable name, the receipt must name
   `Relabel`; do not substitute another command's evidence.
3. Run `tools/Commands/verify_relabel_boot.py --help`, then provide both TRXs,
   both receipts, native qualification, bound test assembly and a new output
   directory. This checks real media and the full pair rather than trusting the
   boot test's readiness-only pass.
4. Run `tools/Commands/verify_relabel_boot_controls.py` against those observations
   and receipts with a fresh output path. All twelve corruptions must reject.

Other argument/continuation variants, non-RAM and completed write-protected
volume behavior, requester cancellation, Workbench
launch, the 020/040 real-OS matrix, concurrent resident lifecycle, installed
classification, MorphOS correspondence, minimum stack and packaging remain
required. These bounded passes do not close any whole-profile gate.
