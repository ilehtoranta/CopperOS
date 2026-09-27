# RET03: removed-task RunCommand handoff

Status: **design and failing host baseline; no retirement implementation**.
This is the next bounded work package under
[the retirement owner plan](runcommand-retirement-owner-plan.md), not a change
to the overall command goal. RET02 remains partial: synthetic execution proof,
its Classic direct task-owner query, and the stated conservative stack limits.
Native Switch, other allocation families, and DOS external-owner draining are
still separate required gates.

## Recorded boundary

`DosRunCommandRetirementTests.cs` calls the actual Boot-owned Exec, scheduler,
DosServices and portable RunCommand owners. File bytes, allocation accounting
and guest callback entry are supplied; no command instruction or ROM runs.
Initial task selection and replacement selection both use the real synthetic
installation method. The fixture never substitutes `CurrentDosTask` to retire
the removed process.

The accepted red baseline has **four liveness failures and six passing guard
controls**. In each liveness failure, a replacement context has actually been
installed, the removed task is absent from the synthetic contexts, and the real
DOS barrier is reached. It continues to defer its one or two pending callbacks;
no callback allocation, pool or task memory is freed. A second task's active
RunCommand remains unchanged. The 50 RET02 tests and 34 existing host controls
pass. Unchanged RET01 remains five passes and its one documented historical
raw-context-copy expectation failure; it is not a six-pass result.

The separate portable file has **12 passing precondition/ordinary-return
controls**, with **306 inclusive passes**. Its supplied Removed flag and changed
current task are negative premises, never execution proof. It supplies no valid
retirement capability. Ordinary return remains a separate operation.

| Evidence | Identity |
| --- | --- |
| Host receipt | `D:/TestData/CopperOSCommands/Q/ret03hostbe02/receipt.json`, SHA-256 `ede07d406310d304b1d47648acaaa95599501388f47a2dd9452f4021a4ac92dd` |
| Host new test | MedPlayer `CopperMod.Amiga.Tests/DosRunCommandRetirementTests.cs`, 21148 bytes, `ab20e3e6526dbbbd921f4c11519eb99bc6b177fdbc9d9a7d4026161b2db8d50a` |
| Host test DLL | 145920 bytes, `79bb14243da708d83e3cd6c25fad116897b89f0dc6ce5f05b41beb78862a8a16` |
| Host runtime | Existing emulator `0f9a864510a0175a621aee21cd681f89c69210adafb48d5e2d5bc95a01296545`, portable Exec `fd77dc92b1b3093cddd2c711484f2cefb0739e67939accb4676dafe46fa90adc`, Copper68k `bd42669eb9a241c96419fba48ae64183e8e3a0c39a55259ebe32dead681e53b2`; none rebuilt |
| Portable receipt | `D:/TestData/CopperOSCommands/Q/ret03portbe02/receipt.json`, `b3af63795b8054d40a98e876bc7486a37e37f18b10ec78519812526de2be1493` |
| Portable new test | CopperStart `tests/CopperStart.Exec.Tests/DosRunCommandRemovedTaskTests.cs`, 9698 bytes, `deedcfbf584aab6a20b7d308187405ac7ad970006698910aaf5262d48ef0a2ef` |
| Portable test DLL | 268288 bytes, `456cfcbb3bad3e67f6e325657630d85bf01009bd2f54e68c40551631c77075af` |
| Portable provider | API17 host checkpoint DOS `1c4db0fa5b8fe3fae596af2c5c5c466faac75ed0db5f7f7f3ecf98cee29d53ef`; its native Close status is not inferred |

The host and portable rows deliberately have different existing DOS parents.
Do not describe their union as one provider build. Both receipts bind actual
test Compile/ReferencePath membership, selected DLLs, outputs and stable input
hashes. The host capture retains 124 source authorities for its unchanged
emulator, but builds only its test assembly. Portable compilation has 22
explicit source members (20 old classes, TestMemoryPlatform and the new file).

Retain the unsuccessful setup attempts too:

- `ret03hostbe01/receipt.json`, `45fb832f49a0548bfa611678ee2fd0e13a0e737fd31bd792f7b7bfda7bdcaf26`: fixture passed supervisor/user stack arguments in the wrong order and omitted InternalLoadSeg's A6. Only the fixture was corrected before the accepted red baseline.
- `ret03portbe01/receipt.json`, `7d53671b0df8a765d86c990b2799a750c48119ebb731c2f55683e962cde036fa`: setup stopped before build because the parent's 20 test classes list stores TestMemoryPlatform separately. The retry explicitly adds that separately hashed member.

Reproduce into **new** private Q children; these helpers refuse existing outputs:

```powershell
& C:/Python314/python.exe D:/TestData/CopperOSCommands/RetirementBoundary/ret03_host_baseline.py --output D:/TestData/CopperOSCommands/Q/ret03host-review01
& C:/Python314/python.exe D:/TestData/CopperOSCommands/RetirementBoundary/ret03_portable_controls.py --output D:/TestData/CopperOSCommands/Q/ret03port-review01
```

The first command intentionally accepts only the recorded four liveness
failures, six controls and preserved prior outcomes. Neither helper implements
a cancellation or relaxes a production gate.

## Required host authority

Add a **separate removed-task owner binding**; do not reinterpret the existing
`Func<uint, bool>` pre-removal/process barrier as execution authority.

Proposed internal host types in new
`CopperMod.Amiga.Emulator/CopperStart/Exec/ExecTaskRetirementLease.cs`:

- `IRemovedTaskRetirementOwner`: read-only inspection of the exact named task,
  followed by a synchronous attempt to retire the next inspected owner step.
- `RemovedTaskRetirementInspection`: state/generation, token and parent chain,
  immutable ownership fields, full release spans, surviving continuation roots,
  and the exact permitted stack-metadata transitions. This describes data; it
  grants no permission.
- `ExecTaskRetirementLease`: an internal reference type registered only by
  `ExecTaskServices` inside the successful proof branch. Its constructor and
  lifetime do not provide a public issuance path. The issuer additionally
  requires reference identity with its private currently active lease, so a
  copied description, guessed token or separately constructed object cannot
  authorize a step.

The lease binds the actual ExecTaskServices instance, pending-removal object,
task address, ExecBase, scheduler epoch/removal version, installed CPU and CPU
state instances, DOS owner instance, inspected ownership revision and next
context token. The issuer must re-run the actual execution/storage predicate
before each destructive step. `try/finally` invalidates the active lease on
normal completion, deferral or exception. Reset, reincarnation, a different
owner, consumed step, or changed inspection invalidates it. No elapsed boundary,
Switch request, externally supplied Boolean, or `ThisTask` write issues a lease.

The host owner receives it only through this binding; it cannot ask the generic
DOS gateway to mint one. Native dispatch has no issuing path until N01-N03.

## Inspect all owners before freeing any context

The first operation is read-only. Validate the full portable DOS object chain
and the matching host callback stack. Missing and malformed ownership must be
different results. A RunCommand-only operation rejects a mixed callback stack
or a one-sided host/portable mismatch.

For each context, retain the DOS state/generation, allocation header ownership,
allocation size, token, named task/Process record, parent token, captured input,
input-release state, argument storage/length and descriptor address. Match the
host's recorded caller/command stack triples against the live descriptor and
the top task bounds. Reject corrupt, wrapping, unmapped or unwritable metadata
before performing a restoration. Tokens must be strictly nested for that task;
the global allocation chain may interleave other tasks.

There are two additional prerequisites that the red fixture alone does not
close:

1. **Actual release geometry.** DOS passes a four-byte-rounded request to its
   allocation/free delegate. `ExecMemoryCore.FreeMem` can instead follow an
   allocation descriptor; direct Classic free rounds to eight bytes. The
   callback's current stack interval and its requested payload length are not
   automatically the entire released allocation. Add a narrowly read-only
   owner inspection for direct Classic free requests, with the existing memory
   owner/allocator identity and full rounded span. Reject descriptor-remapped,
   pooled, DMA, TLSF or unrecognized forms until their exact owners are covered.
   It must not grant arbitrary free authority or change allocator layout/code.
   Supplied allocation-ledger tests remain distinct from real allocator tests.
2. **Surviving host continuations.** In addition to RET02's live CPU and saved
   scheduler frames, inspect other tasks' pending DOS callback/wait return PCs
   and stack descriptors. A later normal callback return may restore a caller
   stack outside that task's current RunCommand bounds. Its validated caller
   triple provides a typed scan interval; an arbitrary outside stack does not.
   No uninspected host continuation may later resume into a released span.

Retain the union of full callback allocations and the existing Exec-owned
task/storage spans for execution-reachability checks throughout the operation.
Do not drop an inner released interval from that check merely because its
object header has been unlinked. Revalidate only still-owned metadata after a
committed step; cleared, already-consumed headers cannot be treated as fresh
ownership or retried frees.

## Portable operation and exact bounds transition

Proposed new CopperStart files:

- `src/CopperStart.Dos/DosRunCommandRetirementTypes.cs`: fixed-width inspection,
  request and outcome records; metadata is not authority.
- `src/CopperStart.Dos/IDosRunCommandRetirementPlatform.cs`: a separate trusted
  owner boundary for validating the currently active host lease and committing
  its exact named-task transition. Existing provider-cancellation methods and
  Faraday's checked byte-write API remain unchanged.
- `src/CopperStart.Dos/DosRunCommandCore.Retirement.cs`: read-only inspection and
  `TryRetireRemovedTask` for the next exact context. Make the existing core class
  partial only when this source window is approved.

`DosHostPlatform` carries the active host lease only in the private retirement
call path. Its ordinary gateway instances have no lease. The portable operation
has no `safe`, `callbackReturned` or caller-constructed permission argument: it
asks this dedicated platform boundary to validate the exact operation against
the active issuer-owned lease. A platform implementation is a trusted owner
adapter, not guest authorization. A test returning “accepted” without the
actual Exec proof does not qualify the integration. Ordinary/native adapters
without this owner path must decline it.

The one-context transaction is:

1. Revalidate state/generation, named task/Process, top token, parent chain,
   header/span identity, input restoration targets, host descriptor and lease
   revision. Inspect before writing. Reject a different task, outer token,
   expired/foreign lease or changed ownership without writes or frees.
2. Commit only the predeclared transition of the removed task's
   `tc_SPLower`, `tc_SPUpper` and `tc_SPReg`: from its inspected top command
   metadata to that context's captured caller triple. The Exec issuer validates
   both triples and updates only these exact captured metadata expectations.
   It must not recapture arbitrary current task fields. Task state stays Removed.
3. Restore that named task's argument pointer and captured lookahead through a
   common validated input-owner helper. Read live process data so current stream
   choice, IoErr and owned storage are preserved. Honor `InputReleased`; never
   restore into a new/reused input wrapper or close a borrowed stream.
4. Free exactly that validated portable context. Pop the corresponding host
   record only after portable acceptance; advance the lease revision/token once.
   Preserve the remaining outer contexts on deferral. A failed/uncertain free
   is not permission to retry an already-cleared context.

No step may call `StackSwap` on the replacement CPU, change its A7/USP/ISP/MSP,
restore its saved registers, synthesize guest D0/IoErr, or change
`CurrentDosTask`. `Resume` retains its existing current-task/ordinary-return
checks. Its known-not-started rollback remains distinct.

The shared input validation can be factored within `DosCore.RunCommandInput.cs`:
the normal wrapper first requires `CurrentDosTask == continuation.Task`; the
new named-task wrapper first requires the active retirement owner. Do not add
a public Boolean bypass to the existing restoration API.

The host implementation can live in a new `DosServices.RunCommandRetirement.cs`
partial file. Preserve the existing Close failure branch and the separately
owned nested write methods. The old public gateway and callback-return flow
must keep their existing behavior.

## Ordered next slices and acceptance

| Slice | Concrete next action | Gate |
| --- | --- | --- |
| `RET03.1` | Add only read-only portable context inspection and matching host inspection, plus new tests. This is the first implementation window to request. | Exact full chain/span/transition facts for valid nested/interleaved contexts; absent versus corrupt is explicit; whole-memory/no-allocation checks; mixed and one-sided ownership decline. No cleanup is enabled. |
| `RET03.2` | Add the issuer-owned lease and narrowly read-only Classic release-range owner query; collect surviving host continuation roots. | Actual synthetic commit required. All live/saved banks, foreign return PCs/stack continuations, rounded padding, invalid ranges, stale epoch/incarnation, foreign issuer and replay cases defer. Include real Classic allocation tests, not just the ledger fixture. |
| `RET03.3` | Add named-task input restoration and the exact three-field metadata transition under that lease. | New tests verify inner then outer restoration while replacement CPU/streams remain intact; an unexpected Task-bound change still defers. A typed transition must be exercised, not a general snapshot refresh. |
| `RET03.4` | Connect one-context release/pop after execution and external owners both agree. | Retained host liveness rows pass under an explicitly supported owner setup; additional real allocator and interleaved allocation-chain cases pass. Report any fixture wiring additions separately from unchanged red/green rows. No double free on polling/stale completion. |
| `RET04` | Implement the existing plan's begin/drain phase for provider packets, notifications and process-owned storage before admitting their destructive release. | H10-H12. A handler-consumed command buffer, notification pointer or owned Process remains alive until its exact owner drains. Do not bypass the existing early barrier just to make liveness pass. |
| `RET05` / `RET06` | Actual native Switch proof and per-kind mixed callback retirement. | N01-N03 and the original finite per-kind owner matrices; no extrapolation from the host synthetic gate. |

Before `RET03.4`, the initial supported owner setup must be stated explicitly.
No cleanup path may assume that a DOS-owned Process, pending packet/notification
or unsupported allocation form is equivalent to the supplied fixture's empty
external-owner set. Keep such work assigned to its owner gate, not silently
removed from the full command goal.

Add finite lease tests that do not yet exist in the 10/12 baselines: capture a
real issued lease then replay it after its step, after reset, for another task,
against another DosServices instance, and after changed token/descriptor/span
metadata. Also test an other-task host callback/wait frame pointing into each
inner/outer release interval. R05 currently checks rejection of an inappropriate
ordinary callback completion, not those future capability cases.
The current host comparison checks D/A, PC, SR, all stack banks and last PC.
The destructive gate must also compare the remaining saved CPU control fields
(VBR, SFC/DFC, CACR/CAAR and last opcode), alongside the exact invocation count.

## Source facts used by this design

Line numbers apply to these inspected copies, not to concurrent live edits.

| Owner | Relevant lines | SHA-256 |
| --- | --- | --- |
| MedPlayer `ExecTaskServices.cs` in `ret02descaf01/MedPlayer/CopperMod.Amiga.Emulator/CopperStart/Exec/` | 109-150 removal; 205-225 pre/post-provider proof; 290-308 actual commit; 468-499 conservative frame/stack scan | `0424164b8e7216fa64d9c1a1bb4486ded4995780bfb0103bff41cd74cdcc5c29` |
| MedPlayer `DosServices.cs` in `close49367ede/host-after-emulator/src/CopperStart/Dos/` | 152-198 ordinary completion; 292-312 release barrier; 327-338 four-byte allocation/free adapter; 699-792 caller/command stack records; 897-900 pending frame | `72e980e1e80a9cad1639a891e493f2f2258117f1cbc7790608e7c526cc42c0f8` |
| CopperStart `DosRunCommandCore.cs` | 100-187 Begin; 190-204 Resume; 244-292 chain validation | `3f16a44b5c3f399810bf300bdf5b844b9376684391c0e9c97055f87ca2d9ec62` |
| CopperStart `DosCore.RunCommandInput.cs` | 57-95 current-task restoration, live Process and released-input handling | `0aa1fcec7c19b57eb1293b28d617e42a940a2ea909eb6500a84c77d0cd68b978` |
| CopperStart `ExecMemoryCore.cs` in `ret02descaf01/CopperStart/src/CopperStart.Exec/` | 160-194 descriptor dispatch and actual free owner | `960d4809d15d293be8d120f539d406a41c1f25205a48e2d661423e2cee4ffe2d` |
| CopperStart `ClassicPolicy.cs` in that same capture | 35-41 allocation alignment; 99-126 free interval and overlap checks | `a320ac22ca364491b11e584b726bd19196adbe89be0ecc8a21061fd15c92b090` |

No production owner, existing test, accepted receipt, original payload, compiler
or SDK was changed for this design/baseline slice.
