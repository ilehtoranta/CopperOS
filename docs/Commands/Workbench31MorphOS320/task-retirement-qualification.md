# RunCommand retirement: bounded synthetic-owner qualification

Recorded 2026-08-30. **RET02 is partial; RET03 has read-only groundwork;
RET03-RET06 lifecycle completion remains open.** This is
host-side ownership evidence for the
[retirement implementation steps](runcommand-retirement-owner-plan.md).
It is not actual ROM Switch, nonlocal Exit, command, boot or purity qualification.

## Safety correction

RET01 reproduced four premature cleanup failures with the real host RemTask and
reaper owners. Merely requesting Switch, accepting/rejecting that request, or
changing ThisTask did not establish that the actual CPU had stopped using the
removed task's memory. Its [six-case baseline](D:/TestData/CopperOSCommands/Q/ret0132a7/receipt.json)
remains four failures and two controls passing; SHA256
`8603b63bed9ec41b896e63c736f73aed713e1160261eb857eedfeef5b420bfb2`.

The corrected synthetic path records a private scheduler commit only after its
actual `InstallSyntheticContext` operation installs the replacement CPU
context. The proof is bound to the CPU, scheduler epoch, removal version and
replacement task. Raw architectural field copies and supplied Booleans cannot
produce that commit. Removed tasks cannot be recaptured or republished while
their address remains retained.

Before freeing anything, the owner checks removal state, exclusion from valid
ready/wait lists, the installed scheduler identity, live and saved PC/stack
banks, conservative stack continuation scans, and immutable allocation metadata.
It repeats the execution/metadata checks after the DOS/provider barrier.
`ThisTask` must identify an actual different context. Interrupt and dispatch
critical sections defer current-task retirement.

The owned-memory snapshot includes MemEntry storage and the existing MorphOS
CSTK task descriptor. The new read-only `InspectOwnedAllocation` query validates
the existing descriptor and full Classic allocator release span. In the real
memory fixture, a 1132-byte request frees 1136 rounded bytes, all of which must
be protected. Guest layout, allocation and ReleaseOwned behavior are unchanged.
Malformed, unmapped, TLSF or wrapped ownership is deferred rather than guessed.

## Independent before/after checks

| Selection | Before | After |
| --- | --- | --- |
| Synthetic commit, context and metadata cases | 31 fail | 31 pass |
| Retained-address admission | Two fail, two controls pass | Four pass |
| Real descriptor/rounded-allocation ownership | 13 fail, two controls pass | 15 pass |
| Existing scheduler/task/allocator controls | Retained controls | 34 pass |

Each new selection preserves its test source across its before/after pair.
The later retained baseline also executes all 50 new cases and 34 existing
controls successfully. The unchanged historical RET01 suite now has five
passes and **one intentional historical failure**: its old raw-copy positive
assertion is incompatible with the corrected scheduler proof. That source and
failure have not been edited away or included in an all-green total.

The [RET02 evidence summary](D:/TestData/CopperOSCommands/RetirementBoundary/ret02-summary-20260830.json)
binds the source deltas, individual receipts, actual build/test arguments and
limits; SHA256 `2f1dcf9714e477b68c989b5e60609df3f852aa31b6143a0d1986d5e887a875ab`.
The final descriptor pair is retained as
[before](D:/TestData/CopperOSCommands/Q/ret02descbe02/receipt.json), SHA256
`b0546e8cec8508516d10a53b03fc8979b6b3c22e5917eadb71c8dd76f529db56`,
and [after](D:/TestData/CopperOSCommands/Q/ret02descaf01/receipt.json), SHA256
`d3c3c8629ca4dee95efe7b5a663f6f6ddfba23b23ce14dc96c5e5f9115205a09`.
Its private Exec DLL is
`fd77dc92b1b3093cddd2c711484f2cefb0739e67939accb4676dafe46fa90adc`;
the emulator DLL is
`0f9a864510a0175a621aee21cd681f89c69210adafb48d5e2d5bc95a01296545`.
The build freezes 124 emulator sources and 76 portable Exec sources, using the
earlier pinned CPU. No shared or latest whole-tree build is substituted.

## Remaining ownership handoff

RET03.1 now has a separate read-only inspection checkpoint. It validates the
complete DOS object/parent chains, named Process and retained input ownership,
then matches the complete named-task host RunCommand callback stack. Facts
include raw allocation requests, tokens, descriptors and stack/input records;
they are not a lease, release span or permission to cancel a callback. A shared
65536-visit budget bounds the combined traversals, including repeated host
queries; exhaustion is `WorkLimit`, never successful absence.

The [inspection receipt](D:/TestData/CopperOSCommands/Q/ret03inspectab01/receipt.json),
SHA256 `8d9012c117a30775e177847b62892b665e6450020308b4f502538cc65b3e5070`,
passes 31 new portable cases within 337 inclusive checks and 26 new host cases
within 63 inclusive checks. It captures 94 DOS sources and 125 host sources,
derived from the earlier Close BOOL checkpoint; it is not a composed build of
all later scheduler, Signal, range-query and write-error repairs. Portable
tests trap all writes; host tests compare complete chip RAM and owner/CPU
state, with a source audit for read-only behavior. Those snapshots alone do
not trap a hypothetical transient write followed by restoration. A separate
[host memory-adapter checkpoint](runcommand-retirement-inspection-qualification.md)
now passes all 74 cases with the same test DLL that first recorded 11 failures
and 63 controls. The dedicated adapter throws on every write, clear and copy,
and rejects invalid arithmetic or odd typed reads. These checks do not enable
retirement or compose the old host with the later RET02 scheduler.

The independent [FreeMem range query](free-range-inspection-qualification.md)
now passes 95 cases, including 36 existing controls and a separately reproduced
and repaired odd-word-address admission defect. It supplies conservative
Classic eight-byte release geometry, mapped CSAD probe facts and bounded
allocator topology checks. It cannot authenticate an allocated block's start
or grant release authority. Its final private Exec DLL is separate from the
RET03.1 host/DOS checkpoint and has not been integrated into a cleanup owner.

The new RET03 [host baseline](D:/TestData/CopperOSCommands/Q/ret03hostbe02/receipt.json)
has four liveness failures and six guard controls passing. Real synthetic
installation is observed, but live RunCommand callbacks keep the DOS barrier
closed and no resources are freed. Receipt SHA256
`ede07d406310d304b1d47648acaaa95599501388f47a2dd9452f4021a4ac92dd`.
These four failures remain open after the read-only groundwork. This is the
next owner problem to solve; making the barrier return true is not
a substitute for cancelling exact callback ownership safely.

Required follow-up includes typed RunCommand allocation/stack-bound handoff,
restoration of nested callback frames after actual descheduling, outstanding
packet/notification draining, native Switch completion and reset teardown.
Alternate stack banks outside a task's public bounds, non-Classic/wrapped
allocators and allocator collisions remain unsupported by this finite proof.
The 1 MiB scan and 4096-node walk limits bound a proof attempt; they are not
new Amiga ABI limits. Conservative deferral is safe but does not complete the
required lifecycle or permit shipping/resident qualification.
