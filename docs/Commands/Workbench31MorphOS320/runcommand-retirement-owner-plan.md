# RunCommand retirement owner plan

Dependency: **CC02.API14 — RunCommand argument/input boundary**. Proposed work
items below use `CC02.API14.RET01` onward. This is a bounded continuation of the
[native launch contract](native-command-launch-contract.md), not a replacement
for the stable command implementation goal.

This document records a read-only source audit on 2026-08-30. No retirement
implementation, build, host test, native switch test, original-command test,
or shipping qualification was performed for this document. The source hashes
below identify the inspected bytes; they are not a compiler or runtime input
closure. All implementation and qualification steps below remain open.

The current host callback tests establish ordinary return, nesting, rejected
entry, retained ownership after a declined resume, and deferral of reset or a
direct `ReleaseTask` call. They do **not** establish cancellation of a task
which `RemTask` has actually removed from scheduling. The host owner reports
14 initial cases and 32 cases including the existing DOS class and the new
successor regression passing; those are separate host receipts, not execution
performed by this audit and not retirement qualification.

## Observed owners and gap attribution

| Observation | Exact inspected owner | Consequence and attribution |
| --- | --- | --- |
| Host callback records now form a stack per task. Successful command completion exchanges back to the caller stack before the portable owner releases the command allocation; declined completion retains the record and reverses that exchange. | MedPlayer `DosServices.cs:152-198,699-792` | The normal-return ownership path is materially different from abandoning a removed task. Do not use a supplied successful return as evidence of cancellation. |
| The host refuses to release a task or reset DOS while a host callback is pending. | `DosServices.cs:279-308` | Appropriate protection while guest execution can still reach the allocation. It does not itself make the task runnable or guarantee eventual completion. |
| `RemTask` calls the DOS retirement barrier, then unconditionally unlinks/removes the task. A false barrier result queues deferred reaping; it does not postpone task removal. | MedPlayer `ExecTaskServices.cs:101-131` | A callback of the removed task can no longer be expected to return. The direct deferral test, which subsequently supplies a return, does not model this caller. |
| Deferred reaping retries the barrier and then releases pools and `tc_MemEntry` storage. | `ExecTaskServices.cs:176-198` | The queue has no separate proof of CPU stack detachment. A removed task with a pending RunCommand remains pinned indefinitely without an abandonment path. |
| Portable process retirement also refuses to pass a pending RunCommand. | CopperStart `DosCore.cs:287-337` | The liveness gap is not attributable solely to the new host callback patch. The portable barrier already contributes to it. The old host code dropped callback ownership prematurely; restoring that protection exposes, rather than solves, the need for removal-specific cleanup. |
| The native-Exec branch invokes the reaper at an outer instruction boundary, before dispatch/nesting checks. | MedPlayer `AmigaBoot.cs:2866-2902`; `RuntimeInstructionBoundary.cs:38-42` | “Later outer boundary” does not prove that KS Switch has executed. This is an existing reaper safety gap; cancellation must not turn it into a new command-stack use-after-free. |
| Entering native Switch changes the CPU entry PC and pushes a continuation, but does not synchronously execute Switch. | `AmigaBoot.cs:2999-3006,3075-3096` | Immediately after current-task removal, the CPU can still be using the removed task's stack. A successful request to enter Switch is insufficient evidence to free it. |
| The synthetic scheduler has an explicit CPU-context commit at `SwitchTaskContext`. | `AmigaBoot.cs:2954-2979`; `ExecTaskScheduler.cs:28-64` | This is a possible host proof point after the replacement context is installed. The current synthetic branch does not invoke deferred reaping there. Its preceding `CaptureCurrent` also needs a removed-task guard so it cannot recreate a removed synthetic context. These are existing integration gaps. |
| Portable task removal can change `ThisTask` without loading any CPU registers. | CopperStart `ExecTaskLifecycleCore.cs:13-24`; `ExecSchedulerCore.cs:13-35` | `ThisTask != removedTask`, including `ThisTask == 0`, alone is not a stack-detachment proof. |
| DOS termination delegates to the normal Exec `RemTask` owner. | MedPlayer `AmigaBoot.DosTermination.cs:7-8`; CopperStart `DosProcessLifecycleCore.cs:18-104` | Preserve that ownership split. Command `Exit`/nonlocal return behavior requires its own contract; this work must not invent a command return or globally prevent `RemTask`. |
| Native/provider cancellation requires the provider owner to unwind; it is not a task-execution predicate. | CopperStart `IDosPlatform.cs:99-107`; host `DosServices.cs:984-986` | Do not reinterpret `TryCancelProviderTask` or its host `true` result as proof that a command's stack is no longer executing. |

The review also found a separate, newly introduced successor-callback failure
path: `ContinueCallback` originally inspected its initial portable disposition
after `CompleteDispatch` had resolved a rejected successor to `Completed`.
It therefore restored the original DOS-call PC rather than retaining the
callback gateway's post-token PC. The host owner reproduced `0x6006` instead
of `0x00F08C2E` and changed `CompleteDispatch` to take the result by reference.
The final inspected source has that change at `DosServices.cs:115-116,145-146,
194-197,651-677`, with a regression at
`DosServicesRunCommandCallbackTests.cs:265`. This reported correction is not a
retirement or native CPU qualification claim.

## Required cleanup contract

Removal, proof that execution stopped, DOS cancellation, and memory reclamation
are distinct phases. The following order is required before a new cleanup
entry can claim success:

1. Record the exact task's removal through the Exec owner. Keep the Process,
   callback records, command argument storage, and command stacks allocated.
   Reject stale/reset-generation identities and do not treat an arbitrary
   task address or a caller-supplied Boolean as proof of removal.
2. Establish at a scheduler-owned boundary that no guest execution or suspended
   continuation can resume on the retained task's storage. The task must be
   absent from runnable/wait ownership and synthetic saved contexts, and the
   actual CPU context must have left the task. A native Switch request, changed
   `ThisTask`, or elapsed instruction count alone does not pass. Inspect active
   and relevant saved stack banks and the actual switch transition; an interrupt
   or an incomplete switch must keep the allocation alive. Define and test the
   supported proof separately for native KS dispatch and synthetic dispatch.
3. Initiate and drain other owners which may retain pointers into that storage.
   `DosPacketCore.ReleaseTask` at lines 732-806 distinguishes queued removal
   from handler-owned work; `HasTaskRetirementPending` at lines 810-833 retains
   it until the required reply. Notification retirement likewise retains
   request/message memory. Preserve both protocols and provider-stack unwinding.
   CPU detachment does not release a handler's right to access a command buffer.
4. Retire validated RunCommand contexts in LIFO order through their portable
   owner, while the exact Process and borrowed handles are still valid. Restore
   its prior argument pointer and captured lookahead before freeing the
   corresponding owned context. Preserve live stream choices and current
   process fields rather than copying a stale Process record. Handle a closed
   captured input using the existing `InputReleased` ownership fact.
5. Retire each host callback record only after its portable counterpart has
   been accepted for retirement. Then run the existing DOS process cleanup,
   pool cleanup, and Exec `tc_MemEntry` reclamation once. Do not unload a borrowed
   segment or close a borrowed input as part of command-context retirement.

The current RunCommand owner has `Begin`, `Resume`, `HasPending`, and validated
top-context lookup (`DosRunCommandCore.cs:99-235`), but no API for an abandoned,
quiescent task. `Resume` and input restoration explicitly require the current
task (`DosRunCommandCore.cs:190-207`, `DosCore.RunCommandInput.cs:57-95`). A new
owner operation must accept the *named removed task* under an explicit
quiescence capability. It must not temporarily replace `CurrentDosTask`, call
`Resume(..., callbackReturned: false)` for an entry that did run, or manufacture
a guest D0/IoErr outcome. Retirement is a resource operation, not a successful
or failed command return.

The reaper must not call `SetActiveStackPointer` or restore registers of the
replacement task. Saved caller descriptors may restore metadata of the
abandoned task while it remains allocated; they are not permission to move the
current CPU back onto a stack about to be freed. Corrupt ownership or a missing
proof must preserve allocations and report an explicit deferred disposition.

The current `DosCore` early RunCommand barrier also precedes packet retirement.
A distinct begin-retirement phase is therefore needed to start packet and
notification cancellation without prematurely freeing RunCommand storage.
Otherwise waiting for both sides to disappear merely creates another cycle.

Generic callback kinds need their own owner policies. An InternalLoadSeg
callback can own scratch or externally supplied allocations; an ExAll hook has
enumeration ownership; a ProcessExit callback has termination semantics. A
RunCommand-only retirement entry must reject a mixed stack rather than silently
pop these records. `RET06` below assigns the remaining work explicitly.

## Bounded test-first steps

All IDs in this table are **pending**. Coordinate the source/build window before
any build. Keep before/after sources and failing receipts; preserve earlier host
and native checkpoints as observations of their exact inputs.

| ID | Owner and concrete work | Completion gate |
| --- | --- | --- |
| `CC02.API14.RET01` | Add only MedPlayer `CopperMod.Amiga.Tests/ExecTaskRetirementBoundaryTests.cs` first. Use actual `ExecTaskServices.RemTask` and `ReapDeferredTasks`, the existing task-list helpers and an allocation ledger. Start with H01-H03 below before changing production. | A retained failing baseline demonstrates that a current task can be reaped before its stack has been detached. Test data, not a callback that simply returns “safe”, distinguishes the before/after phases. This is a host owner regression, not native Switch proof. |
| `CC02.API14.RET02` | Exec owner: add a removal-specific handoff separate from the existing pre-removal barrier in `ExecTaskServices.cs`/`CopperStartExecContext.cs`, and connect the appropriate scheduler commit in `AmigaBoot.cs`. Keep removal terminal. Add `CopperMod.Amiga.Tests/DosRunCommandRetirementTests.cs` for actual Exec/DOS integration. | H01-H06 pass without changing `CurrentDosTask`, synthesizing a callback return, or freeing a current stack. Document which dispatch mode has a demonstrated proof. Current native removal remains open until N01-N03 pass. |
| `CC02.API14.RET03` | CopperStart RunCommand owner: design a dedicated, narrowly callable `TryRetireRemovedTask` operation in `DosRunCommandCore.cs`, with named-task restoration in `DosCore.RunCommandInput.cs` and a separate quiescence capability rather than overloading provider cancellation. Add `tests/CopperStart.Exec.Tests/DosRunCommandRemovedTaskTests.cs`. | H04-H09: validated state/task/token/parent chain, exact LIFO restoration and owned frees, no current CPU/result mutation, no foreign or borrowed resource release, and repeat calls cannot double free. Keep normal `Resume` restrictions unchanged. |
| `CC02.API14.RET04` | DOS process/packet/notification owners: introduce the begin/drain phase through `DosCore.cs`/`DosProcessLifecycleCore.cs` and existing packet/notification owners. Connect the host retirement handoff only after CPU and external-memory owners agree. | H10-H12: a handler-consumed packet or notification retains command/task memory; the exact final reply releases it once; foreign tasks and FIFO order remain intact; a deferred reset can finally complete. |
| `CC02.API14.RET05` | MedPlayer native fixture owner: add `CopperMod.Amiga.Tests/RunCommandRetirementNativeTests.cs`, using the existing licensed KS/native provenance and public task-removal path. Do not patch the ROM Switch vector or substitute a parser/Process. | N01-N03 observe actual native Switch and stack detachment. Bind originals, generated DOS, compiler/runtime inputs, machine mode and instruction traces. Host tests cannot substitute for this gate. |
| `CC02.API14.RET06` | Generic callback owners: specify explicit removed-task abort behavior for InternalLoadSeg/InternalUnLoadSeg, ExAll and ProcessExit in their existing portable owners and the shared host dispatcher. | Finite per-kind resource and successor tests, including a generic callback nested inside RunCommand. Until then, mixed stacks are reported pending and are not counted as retired. Command Exit/nonlocal behavior retains a separate contract and fixture gate. |

The first independent implementation slice is **RET01 only**: create the new
test file, bind its source and relevant existing DLLs, run the new tests before
any production edit, and return the exact failing boundary. It can proceed
without editing `DosServices.cs`, changing SDK/compiler APIs, or inventing a
native quiescence assertion.

## Finite regression matrix

| Fixture | Required observation |
| --- | --- |
| `RC-RT-H01` | Remove the currently executing task with a retirement barrier ready to complete; call the real reaper while `ThisTask` and CPU stack still refer to it. No callback, pool, or memory allocation may be freed. A Switch-request Boolean is not the positive condition. |
| `RC-RT-H02` | Change only `ThisTask` to another task or zero while retaining the old CPU stack. Reaping still defers. Supply a committed replacement context afterward and observe the distinct eligible phase. |
| `RC-RT-H03` | Remove a noncurrent task, keep it absent from native lists and synthetic context storage, and reap once its execution proof is satisfied. Removal stays terminal; no guest return or repeated scheduler registration occurs. |
| `RC-RT-H04` | Remove a task with two nested RunCommands and consumed argument bytes. Retire inner then outer, restoring each argument/lookahead owner before its exact free; no command entry is invoked again. |
| `RC-RT-H05` | A second task remains runnable and has its own active command. Retirement of the first preserves the second's registers, input cursor, argument pointer, IoErr, callback stack and allocations. |
| `RC-RT-H06` | Repeat reaping, deliver an obsolete callback return, and attempt an outer-token-first retirement. No double free, wrong-task consumption or stale token acceptance. |
| `RC-RT-H07` | Reject mismatched state generation, task/Process ownership, corrupt parent token or descriptor. Preserve remaining owned allocations and report the precise deferred stage. |
| `RC-RT-H08` | The removed command selected a different input, or closed its captured input. Respect existing ownership flags; do not restore lookahead into a new/reused wrapper or close a borrowed handle. |
| `RC-RT-H09` | A generic callback is nested inside RunCommand. The narrow RunCommand path must not silently dispose it; only an explicitly implemented per-kind abort can advance retirement. |
| `RC-RT-H10` | A queued packet can be removed before handler consumption; a consumed packet cannot. Retain command buffers and task reply-port memory until the exact final reply, including repeated polling and a foreign reply control. |
| `RC-RT-H11` | A notification message still exposes a request in command/task storage. Keep it alive through removal and reset until the exact message/handler protocol drains, then free once. |
| `RC-RT-H12` | Reset spans removal, blocked provider work, nested callbacks, and later cancellation completion. It neither clears host ownership early nor remains permanently pinned after all owners have actually drained. |
| `RC-RT-N01` | Under original KS dispatch, trace current-task `RemTask`, entry into Switch, intermediate stack operations, and the first proven replacement CPU context. Every free is after that proof; before-Switch and changed-ThisTask-only checkpoints must fail admission. |
| `RC-RT-N02` | Exercise a pending interrupt and a failed/unavailable Switch entry. Neither path permits freeing a stack still reachable by the active or suspended context. No fallback timing/provenance mode is silently selected. |
| `RC-RT-N03` | Repeat nested RunCommand removal with a second task and real packet/notification ownership where available. Report supported CPU/mode outcomes individually; unavailable original/provider prerequisites stay open and are never reported as a pass. |

Existing useful fixtures are `ExecTaskPoolCleanupTests.cs:12,72` (ordering and a
manually released barrier), `DosServicesTests.cs:455-524` (actual Exec/DOS
notification retirement), and `DosServicesRunCommandCallbackTests.cs:110-261`
(supplied ordinary returns and direct deferral). Reuse their owner conventions,
but do not treat their supplied readiness/return events as native evidence.

## Inspected source identity

Roots: **M** = `D:/Koodit/GIT/MedPlayer`; **C** = `D:/Koodit/GIT/CopperStart`.
Line references above apply to these exact bytes. Hashes were read directly
from disk during this audit; later edits require a new source identity.

| Root / file | Bytes | SHA-256 |
| --- | ---: | --- |
| M / `CopperMod.Amiga.Emulator/CopperStart/Dos/DosServices.cs` | 81766 | `86d2a37327c9a2f5e2ace04551a1308300abb0941b1dad38e1a29f962d3af1db` |
| M / `CopperMod.Amiga.Tests/DosServicesRunCommandCallbackTests.cs` | 18111 | `3597ee11b2493e7c814b25dc0b511eb888246bf26e2def1e4255ebda1d25dedb` |
| M / `CopperMod.Amiga.Emulator/CopperStart/Exec/ExecTaskServices.cs` | 11265 | `e8a9592337113ce84f9225a366ceecebc28a687bb9a758e839398cf329d2d83c` |
| M / `CopperMod.Amiga.Emulator/CopperStart/Exec/ExecTaskScheduler.cs` | 2235 | `265e135fa492b2b6c499c962532dd530e7f27f53c10f08230ce6343c53354350` |
| M / `CopperMod.Amiga.Emulator/CopperStart/Exec/CopperStartExecContext.cs` | 4073 | `b0475158459e7e0fb674c52908e923ef6c9381d629138e632c357a1eaf2ae9fd` |
| M / `CopperMod.Amiga.Emulator/AmigaBoot.cs` | 621080 | `089c96cf067c5cfce7bf0b2e242147db47ceb4137014afc5b59c5078afece30d` |
| M / `CopperMod.Amiga.Emulator/AmigaBoot.DosTermination.cs` | 214 | `f2f445243b3fa6f6a6c0f74f46afe99d4b84e7fc2540fdd13edaf4c4643ad8f0` |
| M / `CopperMod.Amiga.Emulator/CopperStart/Runtime/RuntimeInstructionBoundary.cs` | 5778 | `3b6cc72bb812c353a68526abde05b4247fc4c9f98b445b90116afb2bd1df6003` |
| M / `CopperMod.Amiga.Tests/ExecTaskPoolCleanupTests.cs` | 4275 | `048768ac2a3f6e570ab8b13fbeb696f915383219bbcbb4fb03e6276cb0da9dc9` |
| M / `CopperMod.Amiga.Tests/DosServicesTests.cs` | 58951 | `29789636fc780737131954352097128635282091614c43f5a8c93e5bee9b983d` |
| C / `src/CopperStart.Dos/DosRunCommandCore.cs` | 14253 | `3f16a44b5c3f399810bf300bdf5b844b9376684391c0e9c97055f87ca2d9ec62` |
| C / `src/CopperStart.Dos/DosCore.RunCommandInput.cs` | 5552 | `0aa1fcec7c19b57eb1293b28d617e42a940a2ea909eb6500a84c77d0cd68b978` |
| C / `src/CopperStart.Dos/DosCore.cs` | 206395 | `ae7d985f99585d0f5fd3af3c40b160bc97204ab78950afae17115c2de94068f6` |
| C / `src/CopperStart.Dos/DosProcessLifecycleCore.cs` | 9441 | `e814f4a77cb8e6a0cc10b75f211c0a78f1718ba8198edea1ce9464104f5108a1` |
| C / `src/CopperStart.Dos/IDosPlatform.cs` | 10024 | `9f4289a2fc6614ef74f08dff4e85306e65571b400b7cdc4520d909b19c5aea47` |
| C / `src/CopperStart.Dos/DosPacketCore.cs` | 46097 | `5197fb998ca673251d2a71b0c747e45b820bdf447d695d6d7af04de38255c607` |
| C / `src/CopperStart.Dos/DosNotificationCore.cs` | 41168 | `fe1de52b9e03a50ee9b36929b0431a9c03d4f666d0bb4f6e8f425e673381498d` |
| C / `src/CopperStart.Exec/ExecSchedulerCore.cs` | 2129 | `2190bd8be8b8eeb323bf7af8dddd0e35f5c2e34e164514e29f08e9e2fe0e9bb8` |
| C / `src/CopperStart.Exec/ExecTaskLifecycleCore.cs` | 1243 | `c43a147b4dc80c5a1aad5545d29a10707745219a4a42414bc5d6fb232933beb6` |

The compared pre-edit host source is the private
`D:/TestData/CopperOSCommands/Q/a2538df7/before-DosServices.cs`, 76729 bytes,
SHA-256 `c2902d7881d684b39e03b5784c214f9f271b67836559946c329d37c1368c3d7d`.
The intermediate ordinary-lifecycle implementation reviewed before the
successor fix had source SHA-256
`5d48ae29c50ee2745c703bd31c655b68ac10ded2fd1838cc7f6e50e2a482eedd`
and callback-test SHA-256
`d0ec7ff03677514794b3c8a631e1f2dd32dcbbce9b36fdc5ed26aaaf4dbd9f9a`.
Retain those earlier receipts as historical checkpoints. This document
does not copy original ROM or vendor payloads into the repository.
