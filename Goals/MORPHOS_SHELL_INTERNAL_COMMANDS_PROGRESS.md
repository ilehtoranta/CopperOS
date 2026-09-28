# MorphOS Shell internal commands — implementation progress

This ledger tracks the current CopperOS/MorphOS Shell implementation. Production
code remains freestanding, fixed-width, C-like C#; managed tests are not the
runtime owner.

## 2026-09-28 — Add pre-launch channel allocation and handler-owned retirement

- Added `DosQueueHandlerChannelOwnerCore`. It allocates a named channel and its
  fixed-width buffer from the prepared registry's quantum settings, initializes
  the typed channel record, and publishes it only after both allocations and
  validation succeed. The caller supplies a guest name span; no managed string
  or byte-offset access is retained.
- The handler's bounded pending-I/O pump now reclaims a channel only after its
  reader and writer are closed, no packet remains pending, and the buffer is
  no longer open. It discards unread tail bytes at that terminal boundary,
  removes the channel from the registry, then clears/frees the record and
  buffer. This gives the stop contract a path to an empty registry.
- Added compile-only coverage for channel creation/retirement and rollback on a
  failed buffer allocation. `CopperStart.Dos` built cleanly; the Exec test
  assembly compiled to the isolated
  `CopperStart/artifacts/queue-handler-channel-owner-compile/` output with
  zero errors (existing NU1902 advisory only). Tests were not run. The Shell
  NativeRoot compiled to the isolated
  `CopperOS/artifacts/queue-channel-owner-compile/` output with zero warnings
  or errors.
- Static MC68000/020/040 compilation passed in
  `artifacts/shell-native-queue-channel-owner-20260928-v1/`: 112 reachable
  methods per target, zero managed allocation sites, exception regions,
  runtime features/helpers, external native targets, and fatal machine-fault
  sites. This is static compiler evidence only.
- The Shell still does not create channels from a pipeline plan or route its
  command streams to their endpoints; process-return waiting and completed
  task reclamation also remain open. Shell `|` remains unadmitted.

## 2026-09-28 — Add Queue-Handler process publication and retirement gate

- Added `DosQueueHandlerProcessCore` in CopperStart DOS. `Prepare` allocates
  and initializes the guest-resident registry and fixed-width task-control
  record; `Start` publishes the Shell Queue-Handler entry, stack, priority,
  `Task.UserData`, and a typed output address for the new process message port.
  `TryRetire` refuses reclamation until the task is terminal and its registry
  contains no channels. Control state remains a named struct, with serialized
  field positions private to its codec.
- Added a compile-only regression for publishing the entry and user context.
  The CopperStart Exec test assembly compiled in Release with zero errors and
  the existing transitive `Microsoft.Build.Tasks.Git` 10.0.300 NU1902 advisory;
  no tests were run. Shell DOS and its NativeRoot compiled in Release with zero
  warnings or errors.
- Added native qualification roots for publication and terminal retirement.
  Static MC68000/020/040 compilation passed in
  `artifacts/shell-native-queue-process-start-20260928-v1/` (1,274 reachable
  methods per target) and
  `artifacts/shell-native-queue-process-retire-20260928-v1/` (2,382 methods per
  target), with zero managed allocation sites, exception regions, runtime
  features/helpers, external native targets, or fatal machine-fault sites.
  These are compile-only artifacts; neither root nor guest code was executed.
- This is a publisher/retirement gate, not completed pipeline integration.
  There is no Shell callsite yet to prepare channels from a pipeline plan,
  route `PIPE:` opens to the registered task port, schedule producer/consumer
  commands, wait for process return, or reclaim the task after it has fully
  exited. Shell `|` remains unadmitted; `guestExecution` and
  `shippingAdmission` remain false.

## 2026-09-28 — Add the Queue-Handler task loop and stop contract

- Added `DosQueueHandlerTaskRecord`, its codec, and task core in CopperStart
  DOS. The fixed-width guest record holds the DOS state, channel registry,
  handler port, owner task, stop/terminal flags, and exit result; serialized
  field offsets remain inside that record's codec.
- `Run` is now the owning repeated loop around `WaitAndProcessNext`. A stop
  request is accepted only while running and after the channel registry is
  empty; it sets guest-owned state and signals the task using the validated
  `MsgPort.SignalTask`/`SignalBit` fields. The task rechecks the empty registry
  before marking itself stopped. This prevents a shutdown from abandoning
  channels or their pending packet records.
- Added the Shell-native `copperos.shell.queue-handler-task` entrypoint. It
  recovers the control-record pointer from the current Exec `Task.UserData`
  through `ExecTaskCodec`, reads the DOS-state pointer from the typed record,
  and enters the DOS-owned loop.
- `CopperStart.Exec.Tests` compiled in Release with zero errors and its existing
  transitive `Microsoft.Build.Tasks.Git` 10.0.300 NU1902 advisory; tests were
  not run. `CopperOS.Shell.Dos` and its NativeRoot compiled with zero warnings
  or errors. Static MC68000/020/040 compilation passed for the task entry in
  `artifacts/shell-native-queue-task-20260928-v1/` (130 reachable methods per
  target) and stop path in `artifacts/shell-native-queue-stop-20260928-v1/`
  (31 methods per target); both have zero managed allocations, exception
  regions, runtime features/helpers, external native targets, and fatal
  machine-fault sites. Neither was executed.
- This supplies an owner loop but not its publisher: no pipeline code yet
  allocates/initializes the control record and registry, launches the child
  with this entry, routes `PIPE:` opens to its port, or waits for and reclaims
  the stopped process. Shell `|` execution remains unadmitted;
  `guestExecution` and `shippingAdmission` remain false.

## 2026-09-28 — Add a typed Queue-Handler idle wait step

- Added `WaitDosMessagePort` to the DOS packet boundary and implemented it for
  native Exec using `ExecMsgPortCodec.Read` and the `MsgPort`/`PortFlags`
  structures. It only waits when the port is mapped, configured for signalling,
  owned by the current task, and has a valid signal bit; the wait itself uses
  Exec `WaitPort`, which leaves the message queued for the next `GetMsg`.
- Added `WaitAndProcessNext`: it first polls and pumps pending channel I/O, waits
  only when that bounded step makes no progress, then polls once more. Its
  contract explicitly leaves repetition and startup/shutdown to the process
  owner; this is not yet a resident handler loop. Added compile-only cases for
  a packet arriving during the wait and for foreign/non-signalling ports.
- `CopperStart.Exec.Tests` compiled in Release with zero errors; its existing
  transitive `Microsoft.Build.Tasks.Git` 10.0.300 NU1902 advisory remains. Tests
  were not run. Shell DOS NativeRoot compiled with zero warnings/errors. Static
  MC68000/020/040 compilation passed in
  `artifacts/shell-native-queue-port-wait-20260928-v1/`: each reaches 10 methods
  with zero managed allocations, exception regions, runtime features/helpers,
  external native targets, or fatal machine-fault sites. The emitted code
  includes the native Exec `WaitPort` vector call; this is static evidence only.
- The resident Queue Handler still lacks task startup/port registration and an
  owning repeated loop; channel population/retirement from pipeline plans and
  concurrent Shell scheduling also remain unimplemented. Shell `|` execution
  is still not admitted; `guestExecution` and `shippingAdmission` remain false.

## 2026-09-28 — Add a one-step Queue-Handler packet dispatcher

- Added `DosQueueHandlerPacketCodec` and `DosQueueHandlerPacketCore` in
  CopperStart DOS. `ProcessNext` dequeues one standard packet and dispatches
  open, READ, WRITE, END, and unknown actions. It uses the shared typed packet
  header; serialized message/packet layouts remain in codecs.
- READ/WRITE `WouldBlock` requests are now allocated as guest-resident pending
  records and attached to the relevant channel slot. Each processed packet,
  and each idle step, services one bounded pass over registered pending
  requests; completed or broken-pipe requests are detached, reclaimed, and
  replied to. END closes and reclaims its file record, then the pass can release
  a blocked peer. If pending-record allocation fails after a partial transfer,
  the immediate reply preserves the accepted byte count instead of falsely
  reporting zero progress. The caller still owns the resident loop and decides
  how idle steps wait.
- Added compile-only dispatcher cases for open, immediate READ/WRITE, and a
  blocked read completed by a later write, plus partial-write preservation
  when pending allocation fails. `CopperStart.Exec.Tests` compiled in Release
  with zero errors and the existing transitive NU1902 advisory; tests were not
  run. A dedicated NativeRoot compiled in Release with zero warnings/errors.
- Isolated static compilation passed for MC68000, MC68020, and MC68040 in
  `artifacts/shell-native-queue-dispatch-20260928-v1/`. Each target reaches
  115 methods with zero managed allocations, exception regions, runtime
  features/helpers, external native targets, and fatal machine-fault sites.
  This is static compiler evidence only; neither the root nor guest OS code was
  executed.
- The system does not yet start/register a Queue Handler task, drive repeated
  pump steps or park on idle, populate/retire pipe channels from Shell pipeline
  plans, or integrate concurrent pipeline scheduling. Shell `|` execution
  remains unadmitted; `guestExecution` and `shippingAdmission` remain false.

## 2026-09-28 — Resolve and reply to Queue-Handler open packets

- `DosQueueHandlerOpenCore.Process` now decodes `FindInput`/`FindOutput` into
  `DosQueueHandlerOpenRequest`, resolves the typed BSTR name in the
  guest-resident channel registry, allocates and opens the matching endpoint,
  writes `DOSTRUE` or a DOS error result, and replies through the packet's
  typed reply port. The decoder and service use named structs; packet and BSTR
  layouts stay behind their codecs.
- `FindUpdate` receives `ActionNotKnown`: the current channel contract permits
  one reader or one writer per FileHandle, not a duplex endpoint. Supporting
  update opens needs a deliberate channel/handle model rather than silently
  aliasing an endpoint.
- Added compile-only cases for both supported open actions, rejection of
  `FindUpdate` with an error reply, a successful case-insensitive registry
  lookup/open/reply, and an already-open endpoint error. `CopperStart.Exec.Tests`
  compiled in Release with zero errors; its existing transitive
  `Microsoft.Build.Tasks.Git` NU1902 advisory remains. Tests were not run.
- This is the open-packet service, not yet a resident Queue Handler loop.
  Registry population/retirement, packet polling for all action types, pending
  I/O pumping, and task lifecycle/wakeup integration remain. Shell `|`
  execution is still not admitted; `guestExecution` and `shippingAdmission`
  remain false.

## 2026-09-28 — Add guest-resident Queue-Handler pending I/O

- Added `DosQueueHandlerIoRequest`, `DosQueueHandlerPendingRecord`, and
  `DosQueueHandlerIoCore` in CopperStart DOS. DOS packet fields are decoded
  once into a named request struct. A `WouldBlock` request can now be attached
  to one of the channel's typed reader/writer slots, resumed from its
  guest-resident record after channel progress, and detached for reply delivery.
  The saved record preserves packet identity, buffer, action, and accepted-byte
  progress. Completed reads/writes write the byte count into the original DOS
  packet; EOF completes a read with its accumulated count.
- `DosQueueChannelRecord` now carries the two pending-record pointers in its
  version-2 layout. Same-endpoint close cannot retire a file record while its
  request is pending; opposite-endpoint close remains possible so writer close
  can expose EOF and reader close can expose broken-pipe state. Registry
  retirement refuses either occupied slot. Guest field offsets remain inside
  typed record codecs; packet-service and lifecycle logic consume structs.
- Added compile-only coverage for packet decoding, successful service, and a
  blocked read and partial write resumed after channel progress, including
  attach/resume/detach. Release builds of the CopperStart DOS test assembly and
  Shell DOS NativeRoot passed; tests were compiled, not run. The test build
  retains the existing transitive
  `Microsoft.Build.Tasks.Git` NU1902 advisory.
- Static direct compilation passed for MC68000, MC68020, and MC68040 in
  `artifacts/shell-native-queue-io-20260928-v1/`. Each target reaches 92
  methods with zero managed allocations, exception regions, runtime features
  or helpers, external native targets, and fatal machine-fault sites. The
  25,260-byte MC68000 HUNK SHA-256 is
  `d881384758bc163628e38e1fdb8efe0490d955684a0603122deaa342558ed628`.
  This is static compilation evidence only; no guest execution occurred.
- These are still service and ownership primitives, not a live Queue Handler.
  The handler loop still needs packet polling/reply delivery, READ/WRITE
  wakeup/pumping, open-packet name resolution, and task lifecycle integration.
  Shell `|` execution remains rejected;
  `guestExecution` and `shippingAdmission` remain false.

## 2026-09-28 — Add typed Queue-Handler channel registry

- Added a guest-resident `DosQueueHandlerRegistryRecord` and struct-first
  registry operations in CopperStart DOS. The registry validates its bounded
  linked list, rejects duplicate ASCII-case-insensitive channel names, checks
  each channel against configured quantum/buffer geometry, and only unlinks a
  channel after both endpoints close and buffered data drains. Extended-byte
  name collation remains unresolved for MorphOS parity.
- Added compile-only regression coverage for channel lookup, case folding,
  duplicate prevention, and retirement, plus a dedicated freestanding
  NativeRoot. Release builds passed for `CopperStart.Dos`, the DOS test
  assembly, and Shell DOS NativeRoot. The test assembly retains the existing
  transitive `Microsoft.Build.Tasks.Git` NU1902 advisory; tests were not run.
- Static direct compilation passed for MC68000, MC68020, and MC68040 in
  `artifacts/shell-native-queue-registry-20260928-v4/`. Each target reaches 52
  methods with zero managed allocations, exception regions, runtime features
  or helpers, external native targets, and fatal machine-fault sites. The
  15,652-byte MC68000 HUNK SHA-256 is
  `8f6eefff9ff5fa635ae966bcbab9cebeb86d2d23f5f214abdb13a67085804825`.
  This proves static reachability/compatibility only; the root was not run.
- This is still registry infrastructure, not a serving Queue Handler. DOS
  packet routing, pending read/write packet retention, task parking/wakeup,
  and concurrent Shell pipeline scheduling remain unimplemented. The Shell
  continues to reject `|`; `guestExecution` and `shippingAdmission` remain
  false.

## 2026-09-28 — Add guest-resident typed Queue-Handler channel foundation

- Added `DosQueueChannelRecord` and the named `DosQueueChannelEndpoints` flags
  in CopperStart DOS. The fixed-width guest record embeds the typed
  `DosQueueBufferState`; its bounded channel name follows the record in the
  same DOS-owned allocation. Queue operations exchange typed records and
  transfer results rather than exposing field offsets to Shell logic.
- Guest byte offsets remain only in `DosQueueChannelRecordCodec`, the owner of
  this serialized layout. This keeps struct-first logic while preserving an
  explicit guest-memory encoding boundary.
- The core currently provides persistence, endpoint-open bookkeeping, and
  bounded single-reader/single-writer buffer operations. It is not yet the DOS
  Queue Handler: `PIPE:` resolution/registration, packet retention, task
  parking and wakeup, and concurrent Shell command scheduling remain absent.
  The Shell still rejects `|`; MorphOS 3.20 behavior is not claimed.
- Added compile-only coverage for the typed channel record, inline name,
  endpoint transitions, buffering, and close/EOF behavior, plus a NativeRoot
  compilation entry. Release builds passed for CopperStart DOS (0 warnings),
  its test assembly (0 errors; existing transitive `Microsoft.Build.Tasks.Git`
  NU1902 advisory), and the Shell DOS NativeRoot (0 warnings). Tests and guest
  execution were not run; the channel root has not received a dedicated static
  compiler qualification receipt. `guestExecution` and `shippingAdmission`
  remain false.

## 2026-09-28 — Load DEFER residents on first acquisition

- MorphOS documents `DEFER` as loading a resident command the first time it is
  used. Both the generic DOS registry acquire and the Shell's native acquire
  now use the normal HUNK loader on that first acquire, publish the segment,
  clear `Deferred`, and retain the loaded segment after `Release` for reuse.
- A transient typed `Loading` flag identifies the in-progress record state.
  Removal, replacement, and segment removal refuse records observed in this
  state. This state check is not yet proof of cross-task mutual exclusion; the
  resident registry's concurrent mutation behavior remains an open gate.
- Resident data changes are expressed with `DosResidentEntryRecord` and the
  typed `DosResidentLoadPublication`; guest field offsets stay inside the
  owning record codec. No raw record offsets were added to acquire/loader
  logic.
- Added compile-time regression coverage for first-use loading, retention and
  reuse after release, and operations refusing a record observed in the loading
  state.
- `CopperStart.Dos` compiled in Release with zero warnings or errors. The
  `CopperStart.Exec.Tests` assembly compiled with one existing Copper68k
  `Microsoft.Build.Tasks.Git` NU1902 advisory; tests were not run. Static
  isolated Shell-task qualification passed for MC68000/020/040 in
  `artifacts/shell-native-deferred-first-acquire-20260928-v2/qualification.json`:
  2,159 reachable methods per target, with zero managed allocations, runtime
  features/helpers, external targets, exception regions, or fatal machine-fault
  sites. Guest execution and shipping admission were not performed.
- Source: [MorphOS Resident](https://library.morph.zone/Shell_Commands/Resident).
  MorphOS runtime/error-code parity, failure recovery under runtime conditions,
  and true cross-task exclusion remain unqualified.

## 2026-09-28 — Protect SYSTEM resident entries

- The MorphOS `Resident` reference says a command admitted with `SYSTEM` is
  placed in the System portion of the resident list and cannot be removed.
  CopperStart DOS now enforces this from the typed resident-record flag across
  name-based removal, `RemSegment`, and replacement, refusing before list or
  HUNK ownership changes.
- Added compile-time regression coverage for all three mutation paths. The
  DOS core and test assembly build successfully; tests were compiled, not run.
  The test-project build retains the existing Copper68k `Microsoft.Build.Tasks.Git`
  NU1902 advisory.
- Static isolated shell-task qualification passed for MC68000, MC68020, and
  MC68040 in
  `artifacts/shell-native-resident-system-immutable-20260928-v1/qualification.json`.
  Each target reports 2,156 reachable methods and zero managed allocation,
  runtime feature/helper, external native target, exception region, or fatal
  machine-fault sites. Guest execution and shipping admission were not
  performed. Exact MorphOS error-code parity and runtime behavior remain open.
- Reference: [MorphOS Resident](https://library.morph.zone/Shell_Commands/Resident).

## 2026-09-28 — Resident alias entries

- `Resident NAME ALIAS target` now creates a distinct alias entry without a
  backing file. Omitting `ADD` or `REPLACE` defaults this form to `ADD`; mixing
  it with `FILE`, `REMOVE`, `PURE=FORCE`, or `DEFER` is rejected. The DOS-owned
  entry stores the target name and a typed alias-entry flag. Lookup and native
  shell acquisition resolve aliases (including bounded chains created by
  later replacement) to the target entry, so the target's HUNK and use count
  are shared rather than loaded or retired twice. The public Segment node for
  an alias exposes the target segment list after `FindSegment` resolves it.
- Removing an alias does not unload its target. Removing a target with a live
  alias is refused. Resident records and lookup state use named structs and
  the record codecs; field offsets remain confined to those codecs.
- The implementation follows the MorphOS
  [Resident reference](https://library.morph.zone/Shell_Commands/Resident),
  which describes `ALIAS/K` as an alias of an existing resident command.
  Exact MorphOS error results, concurrent reuse, and broader `Resident`
  option/order behavior remain unqualified.
- Release compile-only builds passed for `CopperStart.Dos`,
  `CopperOS.Shell.Dos`, and both affected test assemblies. The CopperStart test
  assembly build reports the existing `Microsoft.Build.Tasks.Git` NU1902
  advisory; tests were compiled but not run.
- Static isolated shell-task qualification passed for MC68000, MC68020, and
  MC68040 in
  `artifacts/shell-native-resident-alias-20260928-v3/qualification.json`.
  Each target reports 2,155 reachable methods and zero managed allocation,
  runtime feature/helper, external native target, exception region, or fatal
  machine-fault sites. Guest execution and shipping admission were not
  performed.

## 2026-09-27 — Enforce script failure limits

- `ShellScriptEngine.Step` now reads the CLI's named `CommandLineInterface`
  record through the DOS bridge and synchronizes its `FailLevel` into the
  guest-resident `ShellScriptFrameState`. Synchronous commands and completed
  external children stop the sequence when their return level is greater than
  or equal to the active limit. This makes MorphOS `FailAt` effective rather
  than merely storing a value.
- Script-sequence teardown restores the documented default limit of 10 after
  the active frame is unbound. The `Run` and asynchronous child paths preserve
  their completed command diagnostics across this cleanup.
- Added test source for the default Error threshold, `Failat 11` allowing an
  Error result of 10, and an external child result at the threshold. Existing
  diagnostic tests that intentionally continue after Error now raise the
  threshold to 11.
- Release compile-only builds passed for `CopperStart.Dos`, `CopperOS.Shell.Dos`,
  and the Commands test assembly with zero warnings or errors. A first DOS build
  attempt collided with another process's shared SourceLink output; retrying
  without rebuilding that shared dependency succeeded.
- Full Execute static qualification passed for MC68000, MC68020, and MC68040 in
  `artifacts/shell-failat-enforcement-20260927-v1/qualification.json`. Each
  target reports 2,588 reachable methods and zero managed allocation sites,
  runtime features/helpers, external native targets, exception regions, or
  fatal machine-fault sites. Selected Shell/Exec/DOS source hashes were stable
  across the managed-input build. This is compiler/static evidence only;
  tests, guest execution, and shipping admission were not performed.
- MorphOS references: [FailAt](https://library.morph.zone/Shell_Commands/Failat)
  specifies default 10, termination on result `>= RCLIM`, and reset to 10 at
  command-sequence exit; [If](https://library.morph.zone/Shell_Commands/If)
  documents how WARN/ERROR/FAIL thresholds relate to FailAt.

## 2026-09-27 — Ask answer return semantics

- `Ask` now returns success for Y and `WARN` for N or an empty line. The
  resulting command return level flows through the normal Shell `LastResult`
  record, so a following `If WARN` reads the response rather than an unrelated
  condition flag. The bounded response decoder accepts NUL, LF, or CRLF as an
  empty negative answer; DOS `FGets` retains the line ending. The supplied
  prompt is emitted verbatim; no extra `? ` is appended.
- This follows the MorphOS Ask prose (N/empty is negative and sets WARN) and
  the MorphOS If definition (WARN matches a previous return code of at least
  5). The Ask page's example appears to invert that mapping, so exact MorphOS
  behavior remains unverified pending a runtime trace. See the
  [Ask reference](https://library.morph.zone/Shell_Commands/Ask) and
  [If reference](https://library.morph.zone/Shell_Commands/If).
- The DOS and Shell Release builds and Commands test-assembly compilation pass
  with zero warnings or errors. Test execution and guest execution were not
  performed.
- Full Execute Release static qualification passed for MC68000, MC68020, and
  MC68040 in `artifacts/shell-ask-result-20260927-v2/qualification.json`.
  Each target reports 2,584 reachable methods and zero managed allocation
  sites, runtime features/helpers, external native targets, exception regions,
  or fatal machine-fault sites. The selected source hashes were unchanged
  across the managed-input build; this is compiler/static evidence only, with
  `guestExecution` and `shippingAdmission` false.

## 2026-09-27 — Struct-based command argument slice

- Replaced `ArgumentsStart` in `ShellScriptCommandInvocation` with a bounded
  `ShellScriptTextSlice` (guest pointer plus length). The invocation remains a
  named struct; DOS validates that the argument slice is contained in the
  caller-owned command line and consumes it directly. Rebasing through a line
  offset is no longer part of the command-launch interface.
- Release builds of the Shell/DOS project and CopperOS Commands test assembly
  completed with zero warnings or errors. No tests or guest code were run.
- Full Execute Release static qualification passed for MC68000, MC68020, and
  MC68040 in
  `artifacts/shell-script-protected-command-20260927-structslice-v1/qualification.json`.
  Each target reports 2,583 reachable methods and zero managed allocation
  sites, runtime features/helpers, external native targets, exception regions,
  or fatal machine-fault sites. This is compiler/static evidence only;
  `guestExecution` and `shippingAdmission` remain false.

## 2026-09-27 — NewShell startup-script handoff

- Native `NewShell` expresses the explicit `FROM` to
  `DosChildCliStartup.CommandFile` with named struct fields, and now supplies
  the documented `S:Shell-Startup` default when `FROM` is omitted. The prior
  positional constructor already mapped explicit NewShell `FROM` to
  `CommandFile`; it was not being stored as the prompt. The
  [MorphOS NewShell reference](https://library.morph.zone/Shell_Commands/NewShell)
  documents the startup-script argument and default.
- The default path is copied into DOS-owned CLI BSTR storage before its
  temporary guest buffer is freed on every launch outcome. The separate
  `NewCLI FROM` is now opened as a CON device: it supplies child input, and also
  output/error when `WINDOW` is absent. When both are supplied, `FROM` supplies
  input while `WINDOW` supplies output/error. MorphOS documents `FROM` as the
  CON device to use, but this split-stream mapping is an explicit inference
  pending a MorphOS differential trace; see the
  [MorphOS NewCLI reference](https://library.morph.zone/Shell_Commands/NewCLI).
- Launch-owned console handles are grouped in a named `ShellLaunchResources`
  struct and represented as `BPTR`; guest ABI field layout stays inside the
  existing typed codecs.
- Native code generation also exposed an incompatible conditional expression
  in DOS empty-value normalization. It now uses an explicit typed `APTR` local
  and branch, avoiding a managed-pointer/scalar evaluation-stack merge.
- Shell DOS and native-root managed builds pass with 0 warnings/errors. The
  isolated compiler receipt at
  `artifacts/shell-native-newcli-from-20260927-v2/qualification.json` reports
  successful MC68000, MC68020 and MC68040 builds (zero managed allocation,
  runtime-helper, or exception sites). Its static report still identifies 12
  fatal machine-fault sites, so this is not clean static or release
  qualification. Guest execution, MorphOS differential behavior, tests, and
  shipping admission were not run or claimed.

## 2026-09-27 — Typed resident admission request

- `ShellResidentPolicy.TryAdmit` now consumes a named
  `ShellResidentAdmission` struct instead of six positional scalar arguments.
  The struct makes verified-PURE, forced/unsafe, deferred, and segment-owner
  facts explicit at the call boundary. Forced unverified admission remains
  unsafe and is never promoted to verified PURE.
- Resident entry state remains a typed struct in policy code. Numeric offsets
  remain only in the guest-memory codec where they define the fixed wire
  layout; command/result logic should prefer named fields and typed records.
- This is a typed-boundary refactor only; the runtime manifest/hash attestation
  and native guest qualification gates remain open. Tests were not run.

## 2026-09-27 — Failure-atomic Path updates

- DOS-owned `Path` updates now validate the complete bounded path list and
  allocation chain before mutation. `ADD`/`RESET` allocate and stage every new
  typed Shell item first; `REMOVE` confirms all distinct requested entries
  exist before deleting any. Allocation failure or a missing removal target
  therefore leaves the prior path unchanged.
- New entries are appended in `ReadArgs /M` operand order, and `RESET` rebuilds
  that order. The [MorphOS Path reference](https://library.morph.zone/Shell_Commands/Path)
  defines `RESET` as replacing the path and `ADD` as adding the selected
  directories, but does not state insertion order; preserving operand order and
  appending `ADD` entries is an explicit compatibility inference pending a
  MorphOS differential observation.
- Path-list traversal uses a value-type cursor; no managed collection or
  numeric result-slot indexing was added. The DOS and Shell DOS builds pass
  with **0 warnings and 0 errors**. Tests and native guest qualification were
  not run for this checkpoint.

## 2026-09-27 — Retry-safe image retirement and typed allocation validation

- Process-image, resident-registry, and Shell allocation-list validation now
  consistently accept the known frame-owned Shell record kinds, whose typed
  allocation header stores a frame identity in `ReturnOffset`. Non-frame-owned
  records still require their return value to remain inside the allocation.
  This prevents valid Shell control records from interrupting DOS-wide object
  scans while retaining bounds checks for ordinary records.
- Process image unload and resident-use release now happen before later cleanup
  operations that can defer. Successful retirement clears `Process.SegList`; a
  failed unload or release preserves the Process record so cleanup can retry
  without losing the image claim or releasing a shared resident image twice.
- DOS and Shell DOS projects build with **0 warnings and 0 errors**. Tests and
  native guest qualification were not run for this checkpoint.

## 2026-09-26 — Struct-backed ReadArgs results for core commands

- `If` now decodes all eleven DOS `ReadArgs` slots into a named
  `IfReadArgsResultRecord`; `Stack/N`, `Resident`, `Run`, and `Path` now use
  named result records as well. Alias, Set, Setenv, Unsetenv, NewShell, and
  Skip also use typed records. Single-pointer and single-switch forms use
  dedicated result records, while no-argument forms use an explicit empty
  result record. External `Execute FILE/A` uses a named file result.
- Echo, If, Stack, Resident, Run, Path, and the remaining fixed-slot commands
  share a bounded cursor that validates the complete ULONG span before reading
  named fields. Dynamic script-key expansion now uses a typed result-array
  view; `Fault/M` uses bounded vector and output-buffer records.
- `Path/M` has a separate bounded vector cursor. It accepts at most 64 paths
  with a required NULL terminator and rejects an unterminated/over-limit list
  instead of silently accepting a truncated vector.
- Commands continue to call the DOS-owned parser and release its `RDArgs` with
  `FreeArgs`; Execute retains its `ReadItem` file-prefix boundary and leaves
  the script-argument suffix untouched. Parser grammar and ownership are
  unchanged.
- DOS `ReadArgs` now honors `RDAF_NOALLOC`: its token scratch and result
  strings/numbers/`/M` vectors/`/F` text stay within the caller's typed
  `RDArgs.Buffer`, with token-slot size bounded by the available space and
  exhaustion reported as `ERROR_NO_FREE_STORE`. The workspace is stack-local
  struct state; no result buffers are attached to the DOS allocation list in
  this mode. A 64-byte buffer is covered by tests.
- The Shell command suite passes **705/705**. Shell, Commands, and the Shell
  DOS native-root managed-input projects build with **0 warnings and 0 errors**.
- No Shell command now reads a fixed `ReadArgs` result array with a call-site
  numeric offset. MorphOS parser-edge differential coverage and native runtime
  qualification remain separate gates.

## 2026-09-26 — DOS-backed Why and Fault output

- The MorphOS `Why` and `Fault` command wrappers now reach DOS-owned output
  helpers instead of failing closed in both the generic and native Shell
  adapters. `Why` decodes the typed `CommandLineInterface` record and reports
  its `Result2` through the caller-selected output handle.
- `Fault` traverses the caller-owned error-code array with a typed cursor and
  fixed-width record, reuses one bounded DOS-allocated scratch buffer, and
  emits one translated line per code in MorphOS's documented
  `Fault 204: directory not found` style. The temporary buffer is cleared and
  released on both success and failure.
- `Why` currently translates `CLI.Result2` to the detailed DOS error text,
  following the traditional DOS meaning of Why. MorphOS describes Why as
  displaying the last command's return code but does not specify its exact
  output or field in the command page, so this choice still needs a MorphOS
  runtime comparison.
- Focused DOS error/bridge checks pass **53/53**; the full Shell command suite
  passes **705/705**; and `CopperOS.Shell.Dos` builds with **0 warnings and 0
  errors**.
  The first full-execute native-root attempt stopped because the qualification
  export allow-list omitted `copperstart.dos.process-return`. That allow-list
  is now corrected; see the newer native qualification checkpoint below.
  MorphOS runtime comparison and native boot qualification remain open.

## 2026-09-26 — Empty templates and typed native DOS ownership

- DOS `ReadArgs` now accepts an empty template with empty or whitespace-only
  arguments. Unexpected arguments still fail with `ERROR_TOO_MANY_ARGS`.
  This unblocks no-option commands such as `Why` through the real parser,
  rather than only through a test adapter.
- Explicit-output diagnostic helpers retain the actual allocation/write
  failure across scratch-buffer cleanup. Public `PrintFault` separately
  preserves its documented convention of setting `IoErr` to the requested
  fault code. Odd-addressed `Fault` LONG vectors are rejected before reading.
- Focused DOS error/bridge tests pass **73/73**, and CLI/ReadArgs tests pass
  **91/91**. These are host-side regressions, not MorphOS execution traces.
- Native Begin/Poll/Park adapters bind `CopperSharpNativeDosPlatform` to the
  DOS state, not to ExecBase. Native child context recovery reads the typed
  Process and delegates CLI ownership resolution to DOS. DOS validates the
  typed allocation header, owner state, generation, size and live allocation
  membership before accepting the owner; the Shell does not reproduce private
  DOS offsets. The focused context tests pass **19/19**.
- This fixes DOS-state ownership once the child has ExecBase. It does not fix
  the separate child entry-register/publication contract listed below.

## ReadArgs compatibility boundary — 2026-09-26

- The current production parser is DOS-owned and supports the standard `/A`,
  `/K`, `/N`, `/M`, `/F`, `/S`, and `/T` template modifiers used by the Shell
  command templates. It also honors the `RDArgs` `RDAF_NOALLOC` control flag
  using a caller-buffer workspace rather than parser-owned result allocations.
- `RDAF_NOALLOC` semantics are grounded in the
  [Amiga ROM Kernel Reference Manual, DOS, §15.5.1](https://developer.amigaos3.net/sites/default/files/downloads/2024-10/Amiga_ROM_Kernel_Reference_Manual_DOS.pdf).
  The new tests cover fixed, numeric, `/M`, and `/F` result storage and
  bounded-capacity failure. This adds AmigaDOS heritage coverage; it does not
  close MorphOS-specific differential qualification. Current focused results:
  `DosCliCoreTests` 41/41, DOS 40.3 plus long-template tests 50/50,
  architecture/core tests 78/78, and native DOS library tests 10/10. The
  combined installed-vector run did not complete and is not reported as a pass.
- The existing byte-for-byte differential corpus is explicitly scoped to
  AmigaDOS 40.3 (44 observations); it is not evidence of exact MorphOS 3.20
  parser behavior. The [MorphOS SDK prototype](https://morphos-team.net/sdk/includes/clib/dos_protos.html)
  confirms the public `ReadArgs` ABI, but does not define detailed parsing edge
  semantics. MorphOS-specific differential qualification therefore remains
  open; the [MorphOS 3.16 release notes](https://morphos-team.net/releasenotes/3.16)
  also record a `ReadArgs` buffer-overflow fix, reinforcing the need for bounded
  native-runtime tests in addition to compatibility traces.
- Keep command result layouts as named structs with bounded codecs. Numeric
  guest-memory traversal is limited to the implementation inside those codecs
  and cursors for variable-length result lists; command call sites should not
  read template slots by literal offsets.

## 2026-09-26 — Synchronous command diagnostics and bounded decimal output

- The Shell carries completed primary/secondary results in a named
  `ShellCommandDiagnostics` struct. Both DOS adapters publish its fields through
  the public CLI codec, rather than literal guest-memory offsets.
- A new command begins with fresh process `IoErr` while retaining the previous
  public `CLI.ReturnCode` / `CLI.Result2` for `Why`. The engine captures the
  command's result before closing redirected handles; cleanup preserves the
  first failed close even when subsequent closes succeed. Blank lines and EOF
  do not publish a new public result.
- The real-DOS regression covers invalid numeric `STACK/N` parsing, capture,
  a successful `Close` that clears `IoErr`, typed CLI publication, then an
  empty-template `Why` parse and redirected diagnostic output. Engine tests
  separately cover this ordering with the test platform, successful/nonzero
  results without fresh errors, and pending-child isolation.
- `Stack` and both DOS fault-number writers now extract decimal digits using
  bounded comparison/subtraction, with at most nine subtractions per digit.
  The DOS iterator is a value-type struct. Only the constant ten is used as a
  divisor; no floating point, managed allocation or exception handling was
  introduced. Tests cover zero, decimal boundaries, signed extrema and output
  truncation; out-of-range requested stack sizes remain rejected.
- The final full Commands suite passes **722/722**. A normal CopperStart test
  build followed by the combined DOS diagnostics/error/CLI/ReadArgs/context
  filter passes **199/199**, without source exclusions. An earlier transient
  unrelated Layers test-build failure cleared; no Layers files were changed.
- Asynchronous completion remains a separate gate: the version-1 continuation
  stores only the primary `Result`. It needs a versioned typed outcome field or
  DOS-owned sidecar that survives child teardown before the parent can publish
  the child's secondary error. The parent deliberately does not substitute
  its own `IoErr`. These host tests do not prove native execution or exact
  MorphOS behavior.

| Goal | Status | Checkpoint |
| --- | --- | --- |
| SG00 — authorities and inventory | In progress | MorphOS command inventory and compatibility contract are recorded in the main goal. |
| SG01 — CLI/DOS/Exec prerequisites | In progress | ReadArgs, CLI state, HUNK loading, resident registry, process records, and DOS-owned continuations are implemented; broader DOS API coverage remains. |
| SG02 — parsing and resolution | In progress | Bounded parser, aliases, redirection, command lookup, script frames, labels, and nested control flow are implemented. |
| SG03 — internal commands | In progress | Internal commands use DOS ReadArgs. Synchronous and durable asynchronous outcomes reach the public CLI through typed diagnostics; MorphOS behavioral comparisons remain. |
| SG04 — script execution | In progress | Execute has a persistent DOS-owned runner, nested-frame restoration, external continuation polling, and prepared waits; native file and resident external-image launch now use the same scalar continuation handoff. |
| SG05 — process lifecycle | In progress | Typed prepare/publish/abort, durable primary/secondary completion and independent ReadArgs input snapshots have host coverage, including nested RunCommand and handle reuse. Failed unpublished recovery and broader concurrent admission remain open. |
| SG06 — resident management | In progress | Resident lookup, acquire/release, replacement, deferred entries, alias-to-target resolution and target-removal protection, immutable `SYSTEM` entries, HUNK ownership, Process use-count binding, `SYSTEM`-only listing, and internal-command listing/removal/reactivation are implemented. Ordinary file admission checks the DOS PURE protection bit; forced unmarked files warn and remain unsafe. MorphOS runtime/error-code traces, exact external artifact/hash qualification, full option/order behavior, and concurrent reuse remain open. |
| SG07 — native Shell | In progress | Begin/Poll/Park Execute and fixed-width child-shell/return exports have native build coverage. Child entry resolves ExecBase independently of A6; typed A0/D0 startup and durable completion have host tests. Actual guest execution, runtime staging and boot handoff remain. |
| SG08 — final qualification | Pending | Differential MorphOS traces, fuzzing, concurrent-shell/resource accounting, and boot-level qualification remain. |

## Latest native compiler checkpoint — 2026-09-26

`tools/Shell/qualify_native_isolated.ps1 -IncludeFullExecute` passed with the
frozen inputs and receipt in
`artifacts/shell-readargs-child-input-20260926-v2/qualification.json`.
All three reports contain **2,443 reachable methods**, the freestanding profile,
zero managed allocation sites, zero exception regions, zero runtime features,
zero runtime helpers, zero external native targets, and zero reported fatal
machine-fault sites. The MC68000 artifact
is a HUNK; MC68020 and MC68040 artifacts are assembler listings. The map
includes the real DOS `Why`/`Fault` helpers, typed diagnostics publication,
bounded decimal extraction, typed child-context/bootstrap recovery, prepared
publication/rollback, typed A0/D0 startup-frame preparation, durable completion,
exact wait cleanup, final nested-script diagnostics restoration and the new
typed child buffered-input/UnGetC/ownership path. The managed
input build completed with zero warnings and zero errors. The selected source
snapshots match across that build. The MC68000 HUNK is 762,428 bytes; the MC68020
and MC68040 assembler listings are 8,276,761 and 8,259,713 bytes respectively.

The full export allow-list now also includes `copperstart.dos.process-return`.
Park-only qualification has a separate reachability root. Qualification uses
copied build dependencies without removing SDK files from sibling projects;
the isolated mode freezes both build and compiler inputs and hashes the
artifacts, reports and maps. Its source snapshots are explicitly bounded
evidence, not a claim of exact compiler-consumed source provenance.

This receipt is a static-build baseline, **not** native execution or shipping
admission. Child-input v1 passed an intermediate snapshot; v2 includes the final
post-callback binding refresh in stream adoption and prepared-child cleanup.
Both receipts and all preceding evidence are preserved.

The previous durable-completion checkpoint remains in
`artifacts/shell-child-completion-20260926-v3/qualification.json` with 2,425
reachable methods. Child-completion v1 stopped because the ROM qualification provider
relied on default interface critical-section methods unsupported by the native
compiler. Explicit no-op methods for that serialized provider resolved the
failure; production native completion uses Forbid/Permit. Completion v2 passed
an intermediate source state. Completion v3 includes the linked-Process
retirement guard, exact wait acknowledgement/frame cleanup, malformed-wait
validation and nested-Shell EOF diagnostic preservation. All earlier evidence
is preserved.

The previous publication checkpoint remains in
`artifacts/shell-child-publication-20260926-v3/qualification.json` with 2,351
reachable methods. Its first attempt stopped at a struct-valued conditional
merge unsupported by the native compiler; selecting the scalar pointer before
constructing the typed pointer resolved it without replacing structs with
offsets. Publication v3 included the final codec relocation, Run tag ordering
and allocation-error preservation.

The previous diagnostics checkpoint remains in
`artifacts/shell-execute-20260926-v3/qualification.json` with 2,322 reachable
methods; its earlier v1 decimal variable-divisor findings were already removed.
Native execution across publication/return, durable completion and asynchronous
error-state transfer, MorphOS differential traces and boot qualification remain
open.

## Historical native compiler checkpoint — 2026-08-16

`tools/Shell/qualify_native.ps1 -IncludeFullExecute` passed on 2026-08-16
for MC68000, MC68020, and MC68040. Each report declares the freestanding
runtime profile, `IsCompatible: true`, zero incompatible members, zero managed
allocation sites, and 1,153 reachable methods. The full export set is:

- `copperos.shell.execute-begin`
- `copperos.shell.execute-poll`
- `copperos.shell.execute-park`
- `copperos.shell.child`

The native Run entry is rooted in `tests/Shell.Dos.NativeRoot` as a compile-time
ABI check. Its file-backed path validates a HUNK image, creates a Process-sized
task, publishes a DOS-owned CLI, and records the segment list without managed
allocation or exceptions. The native provider now uses scalar DOS vectors for
file/lock open, close, duplicate, seek, byte I/O, parent, and name operations
without nullable or managed helpers. Relative-lock, packet, and requester
services remain fail-closed; bounded `CON:` WINDOW startup now opens a
temporary console and transfers duplicated child streams. The same root compiles native
continuation teardown and the explicit-file external script-command
image/Process/CLI handoff. Native `Run` now also uses a scalar resident
registry lookup/acquire/release path and binds the acquired image to the child
Process. The native child-shell export creates a Process/CLI handoff for
bounded `FROM` scripts, inherited-console input, or `CON:` WINDOW children and
drives the persistent runner with prepared waits; child completion and remaining
resource edge cases remain under qualification.

Artifacts and compatibility reports are written under
`tests/Shell.Dos.NativeRoot/bin/Release/net10.0`.

## Current DOS/Exec handoff

- Shell child publication first installs a DOS-owned completion object and a
  private Process backlink. Exit captures the child's private `IoErr` before
  callbacks, fixes its primary result after the callback, and makes the outcome
  terminal only after DOS process resources retire. Parent waits are optional
  notification, not completion storage. Poll/acknowledgement never recover a
  retired child through its former CLI or close saved child-handle snapshots.
- Prepared waits recheck terminal completion after registration and before
  parking. Legacy unbound System/wait paths remain separate; native execution
  through the full entry/return/parent-ack sequence is still unqualified.
- `SIGBREAKF_CTRL_C` and `SIGBREAKF_CTRL_D` are consumed through the DOS/Exec
  signal boundary and mapped to Shell break/Ctrl-C/Ctrl-D events. Unrelated
  signal bits remain pending.
- The remaining shipping blockers include native execution of durable
  external-image completion, requester completion, MorphOS differential
  evidence, full resource accounting, and runtime filesystem staging/boot
  integration.

## 2026-09-26 — Prepared-child publication checkpoint — SG05 / SG07

The Shell launch paths now prepare a child separately from publication:

1. DOS prepares an unpublished Process with its owned CLI, copied name/startup
   text and inherited private streams/directory. Shell uses the named
   `DosShellPreparedChild` record to copy argument tails, duplicate the selected
   error stream, bind resident use or transfer loaded-image ownership, and then
   publish once. No child initialization follows successful publication.
2. Script startup and external launch read a typed `DosShellProcessContext`
   from the owning DOS process. They use the live private current-directory
   lock, not `CLI.CurrentDirectoryName` (a display BSTR), nor a potentially stale
   public Process directory field. Complete CLI display/path/prompt inheritance
   remains future work.
3. Native publication preflights the typed 68k startup frame, installs the DOS
   process-return handler through AddTask, and writes argument pointer/length
   into A0/D0 before Permit. The child Shell independently resolves ExecBase
   through the low-memory bootstrap codec rather than assuming A6 is initialized.
4. Ordinary failed publication aborts the unpublished child, releases its copied
   text/private streams/directory/CLI, and releases its resident use exactly
   once while leaving caller-owned image cleanup to the caller. A regression
   test exposed a duplicated-directory leak; prepared-child rollback now
   releases that lock explicitly. Failed/deferred retirement is not reported as
   completed rollback, and image claims are retained; a durable recovery owner
   for that exceptional path is still required.
5. Eight publication tests observe the first publication callback, overwrite
   source buffers before it, reject publication or argument allocation, verify
   resource-list restoration and resident accounting, distinguish live locks
   from display BSTRs, and check native CLI BSTR alignment. Argument/name-copy
   allocation failures report `NoFreeStore`, preserved through rollback. The
   combined architecture, publication, launch, lifecycle, resident, startup-frame
   and Shell bridge subset passes **127/127** on a normal CopperStart test build.
   The startup-frame checks account for 12 of these. This Debug build emits an
   existing NU1902 warning from the unrelated `Microsoft.Build.Tasks.Git`
   dependency in Copper68k; no dependency changes were made here.
6. Nine Shell bootstrap tests validate mapped, aligned and bounded ExecBase
   recovery. Shell polling also accepts a matching already-terminal shared
   continuation idempotently, and a host script-engine test covers completion
   during launch. The full Commands suite passes **735/735**. These Shell tests
   predate the DOS durable early-exit binding checkpoint below.
7. Required Run task-name tags now precede the optional priority tag in both
   adapters; an absent priority no longer terminates tag traversal before the
   task name. The Fault LONG cursor/record codec lives in its own file, keeping
   raw guest reads out of `DosShellNativeBridge` and retaining the strict
   architecture test.

All new boundary state uses named fixed-width structs and DOS/SDK-owned codecs.
The source preference remains: field names in algorithms, numeric ABI layout
only inside codecs. Native raw-memory mapping checks cannot prove physical
mapping; host tests use a bounded memory provider. No managed runtime,
exceptions or floating point were added to these launch paths.

## 2026-09-26 — Durable DOS completion — SG05 / SG07

- `DosChildCompletionRecord` is a typed 60-byte DOS object. The private Process
  record grows from 92 to 96 bytes with a `ChildCompletion` backlink and the
  private DOS state version advances to 25. The public Process and shared
  52-byte continuation ABI remain unchanged. A typed 16-byte outcome carries
  state, primary result, secondary error and resource-retirement status.
- Binding happens before task publication; allocation failure prevents a runnable
  child. Exit records the private error before guest callbacks or cleanup can
  replace it, and saves the callback's final primary result once. Repeated
  conflicting result publication is rejected. A terminal outcome cannot be
  acknowledged while its original private Process backlink remains linked.
- Resource retirement clears continuation snapshots and sets `ResourcesClosed`;
  parent acknowledgement releases durable state, not child handles or tasks.
  Parent/frame teardown detaches continuation storage before freeing it. A live
  child keeps its detached completion object until its own retirement. DOS
  reset refuses a live binding instead of discarding its ownership record.
- Native completion mutations use balanced Forbid/Permit boundaries. Serialized
  host providers and the deterministic ROM qualification provider use explicit
  or default no-op hooks. No blocking DOS operation is performed inside this
  short completion-record boundary; it does not establish a general lock for
  all existing DOS state or prove concurrent reset admission.
- Private directory ownership now distinguishes duplicated/transferred locks
  from borrowed defaults. Normal return and unpublished rollback release only
  owned directories. Deferred task reaping retains its record when later DOS
  cleanup still defers and disarms/re-finds that record across reentrant cleanup
  so a nested sweep cannot reclaim the same task twice.
- `DupLock` no longer aliases a nonnull current-directory lock. Shared locks get
  independent copies; exclusive locks fail with `ERROR_OBJECT_IN_USE`;
  `DupLock(0)` preserves `IoErr` and returns zero without allocating. These are
  [documented Amiga DOS heritage semantics, RKM §6.1.2](https://developer.amigaos3.net/sites/default/files/downloads/2024-10/Amiga_ROM_Kernel_Reference_Manual_DOS.pdf),
  not a claimed MorphOS runtime comparison.
- Real frame-owned continuation tests exposed a heterogeneous-list bug:
  RunCommand retirement and generic DOS object validation treated every
  `ReturnOffset` as an interior pointer, although Shell record kinds store a
  frame identity there. Validation now respects the object discriminator;
  RunCommand's own record still requires zero and validates its full layout.
- Shell adapters read saved child diagnostics before acknowledgement and publish
  them after cleanup, so `Why` receives the child's secondary error rather than
  the parent's current `IoErr`. A launch that succeeds but cannot register its
  frame no longer invents a terminal failure in the child's shared record.
- Nested Shell runners also capture their typed final diagnostics before
  teardown and restore them afterward for the native Process-return snapshot.
  EOF uses the matching last published CLI result, because EOF reads and signal
  checks can already have cleared private `IoErr`; empty/successful scripts
  clear stale errors. Non-EOF failures capture their current error before
  cleanup. Nine regressions cover this boundary. The full Commands suite passes
  **750/750**, and the DOS-backed Shell Debug build has zero warnings/errors.
- Atomic script acknowledgement validates the exact parent/frame/continuation,
  cancels and frees its wait before reclaiming continuation storage, and then
  releases the completion object. Consecutive asynchronous commands can use
  the same frame without finding a stale wait. Frame teardown also cancels
  armed waits before freeing them; unbinding re-finds list links after record
  cleanup instead of using a reclaimed predecessor. Recognized malformed wait
  bodies block bulk reclamation. Cancellation first validates the allocation's
  frame and the live CLI-to-task ownership, without confusing continuations or
  generic buffers that share the Shell record kind.
- The final host-only DOS and architecture sweep passes **950/950** with no
  skips. It includes 25 durable-completion cases, five exact-wait/frame cleanup
  regressions, seven malformed-wait/ownership cases, directory ownership and
  deferred-reap coverage. The filter
  explicitly excludes native/installed suites and eight native-writer alias
  cases embedded in a DOS host test class; this is not a full native test run.
  The new architecture gate rejects all raw integer guest-memory reads/writes
  in the two completion algorithm files, keeping layout access in typed codecs.

## Remaining lifecycle and argument-handoff gates

1. Provide an exact retry/recovery owner for deferred or failed unpublished
   retirement, including convenience Create wrappers and runner teardown. A
   typed prepared-child ticket now covers the primary DOS/Shell publication
   path; broader failure injection and retirement audits remain open.
2. Qualify the implemented child argument-input handoff in actual guest
   execution and compare MorphOS behavior for pre-existing unread buffers and
   mid-command `Flush`, `SetVBuf`, `Read`/`Seek` and stream-selection interactions.
   The typed process prefix follows the existing RunCommand buffered-input
   architecture; it is not yet proof of direct public FileHandle-buffer parity.
3. Qualify concurrent reset admission and concurrent-shell object-list mutation;
   the new completion critical section is not a replacement for auditing all
   existing DOS ownership boundaries. Preserve legacy unbound lifecycle behavior
   until it has equivalent explicit ownership evidence.
4. Complete CLI inheritance and remaining command-line edge handling, then run
   guest execution through real entry, return, parent wait and acknowledgement.
   Host tests and native compiler receipts cannot authorize runtime staging.
   MorphOS differential traces, PURE admission policy, boot integration and
   final resource-accounting qualification remain part of the full goal.

## 2026-09-26 — ReadArgs child argument input — SG03 / SG05 / SG07

- Prepared executable children and `CreateNewProc` now install a DOS-owned
  buffered argument prefix before publication. `Input()` keeps its real handle;
  `FGetC`, `FRead`, `FGets`, `ReadItem` and ordinary `ReadArgs` consume the prefix
  before continuing with that handle's normal input. Raw `Read` and explicit
  `RDArgs.Source` retain their separate paths. No command-specific option parser
  was introduced.
- A named 24-byte `DosProcessArgumentInput` stores the captured input handle,
  independent buffer, length, position and private pushback/last-read values.
  The private Process record grows from 96 to 120 bytes and DOS state version
  advances from 25 to 26; public Process/CLI and shared continuation layouts
  are unchanged. The new algorithm file uses typed codecs, not numeric field
  offsets.
- The buffered snapshot is independent of the owned `pr_Arguments` C string.
  Editing the string or using `SetArgStr` neither changes the input snapshot
  nor rewinds its cursor. Argument reads do not modify a shared parent's handle
  lookahead or provider position. A matching active RunCommand masks the older
  child prefix even when the invocation's own bytes are exhausted; return
  reveals the unchanged child cursor and pushback.
- Shell executable launch sites explicitly request command-line normalization:
  append LF only when absent, and represent no arguments as an empty LF line.
  Literal process arguments retain their original bytes. Native startup accepts
  65,536 bytes to accommodate a 65,535-byte Shell tail plus LF, excluding NUL.
  Nonnull process arguments with null final input are rejected before ownership
  transfers; omitted CreateNewProc input still uses its normal NIL default.
- NewShell without argument text does not receive a synthetic command line.
  The internal Shell entry that handles `GetArgStr` directly consumes its
  startup prefix before running that text, avoiding duplicate execution when
  it later continues reading input.
- Both copies are released on abort, normal retirement and DOS reset. Closing
  the captured handle disables the binding before address reuse, while its
  buffer remains process-owned until retirement. Stream adoption and prepared
  directory cleanup refresh the binding after resource callbacks instead of
  reinstating stale state. Reset also releases existing owned name/argument
  strings that previously escaped its bulk Process cleanup.
- The final scoped host DOS/architecture sweep passes **994/994** with no skips:
  the previous 950 checks, 33 child-input/Shell-publication regressions, ten
  argument-boundary checks, and a strict typed-codec architecture gate. The
  focused ordinary build first passed 107/107. The last three regressions and
  final full host sweep rebuilt the current test sources with project-reference
  builds disabled, using the freshly built DOS Debug assembly and cached
  unrelated dependencies: a concurrent Intuition source change temporarily
  failed its unrelated dependency build with six CS0103 errors. This is a
  scoped DOS result, not a clean full CopperStart build. Commands pass 750/750;
  the DOS-backed Shell Debug build has zero warnings/errors.
- The final isolated native receipt is
  `artifacts/shell-readargs-child-input-20260926-v2/qualification.json`.
  Its fresh selected Shell/Exec/DOS Release graph builds with zero warnings or
  errors, and all three CPU reports pass the freestanding checks. No guest
  execution or shipping admission is claimed.

Reference basis: the official MorphOS SDK archive `sdk-20260529.lha` (SHA-256
`310dfb888b995e32206c5c3aba409847334a5fee049d13aad27b4af0d518ec77`) supplies
`Development/Autodoc/dos.doc` and `Development/gg/os-include/dos/dostags.h`.
Its RunCommand documentation requires LF for ReadArgs and distinguishes buffered
FGetC from direct Read. MorphOS's CreateNewProc entry is sparse; the detailed
NP_Arguments input-copy contract is supported by the primary
[CreateNewProc autodoc](https://developer.amigaos3.net/autodocs/dos.library/CreateNewProc.html)
and [DOS RKM §10.1.1](https://developer.amigaos3.net/sites/default/files/downloads/2024-10/Amiga_ROM_Kernel_Reference_Manual_DOS.pdf).
These sources do not substitute for a MorphOS runtime differential trace.

## 2026-09-27 — Child CLI inheritance and ownership — SG05 / SG07

- The native and generic child-CLI paths now accept a named
  `DosChildCliStartup.ParentCli` and inherit the parent CLI's display-directory
  and prompt BSTRs, `FailLevel`, `DefaultStack`, and `Interactive` value.
  Callers may provide an explicit interactive override for NewShell. The DOS
  process context and Shell launch record carry a typed `CommandLineInterface`
  snapshot; serialized CLI fields remain behind the existing codec.
- `DosCliBstrCodec` and its bounded `DosCliBstrView` own BSTR prefix, alignment,
  copy, and range validation. The child receives separately allocated BSTRs;
  the parent's strings are borrowed only while creating the child.
- `DosShellStateCore.InheritCliState` stages and copies the parent's local
  variables, aliases, and command path before linking the snapshot. Global
  variables remain shared. Child edits therefore affect the child copy, and
  `ReleaseCliState` reclaims its Shell records and nested script-frame state
  before the DOS-owned CLI allocation is freed during process retirement.
- Shell `Run` and script children request the inherited default stack when no
  `/STACK` value is supplied; NewShell explicitly marks its CLI interactive.
  A focused test source covers snapshot isolation/cleanup and CLI scalar/BSTR
  copying, but test executables were not run.
- Debug builds succeed for `CopperStart.Dos`, the focused DOS test project
  (compile only), `CopperOS.Shell.Dos`, and `CopperOS.Shell.Dos.NativeRoot`.
  The isolated three-target freestanding compile receipt is
  `artifacts/shell-cli-inheritance-20260927-v1/qualification.json` and reports
  passed static checks for 68000, 68020, and 68040, with zero managed allocation
  sites and no exception regions. It still reports 12 fatal machine-fault
  sites per target; no guest execution, MorphOS differential, or release
  qualification is claimed.
- Concurrent DOS Shell object-list mutation, low-memory rollback, broader
  retirement retry safety, and end-to-end guest launch/return/wait/acknowledge
  qualification remain open. This is progress toward the inheritance gate,
  not its completion.

## 2026-09-27 — Release native Shell qualification

- Rebuilt the isolated Shell/Exec/DOS native-root graph in Release and compiled
  the full Execute export set for MC68000, MC68020, and MC68040. Each target
  reports 2,494 reachable methods, zero managed allocation sites, zero
  exception regions, zero runtime features/helpers, zero external native
  targets, and zero fatal machine-fault sites. The earlier 12-site count was
  from the separate Debug configuration and is not the production Release
  result.
- The receipt is
  `artifacts/shell-cli-inheritance-20260927-release-v1/qualification.json`.
  This is isolated compiler/static evidence only: it does not execute the Shell
  under CopperStart or MorphOS, stage `Shell-Seg`, prove boot handoff, or close
  resident/PURE, concurrency, or resource-accounting gates.

## 2026-09-26 — Prepared-child rollback ownership — SG05 / SG07

- A typed 48-byte `DosDeferredTaskReapRecord` is reserved before each generic
  or native DOS child task is allocated. The same record tracks unpublished,
  published, and queued-rollback phases, so preparation failures and Shell
  publication failures have a durable owner even when cleanup must wait.
- Shell rollback can move a caller-owned segment or unbound resident-use claim
  into that ticket. Recovery disarms its record around callbacks, records
  completed process cleanup before releasing image claims, and removes the
  ticket before the final `ReleaseDosProcess` handoff. A false final handoff is
  not retried. No continuation or Shell-frame pointer is stored in the ticket.
- Native and generic preparation failures now pass the continuation through
  rollback so it is terminalized before cleanup callbacks. The failure paths
  avoid a second continuation write after cleanup, when its frame may already
  have been released.
- Resident-use release now defers process retirement when the registry refuses
  the release. On success DOS clears the shared `pr_SegList` pointer so later
  cleanup cannot mistake a resident image for a private segment. DOS state
  version advances to 27 for the changed private retirement-record layout.
- `CopperStart.Dos` and `CopperOS.Shell.Dos` Debug builds both succeed with
  zero warnings and errors. Tests and native guest qualification were not run
  for this checkpoint. Failure-injection coverage, low-memory behavior, and a
  full retry-safety audit of every partial DOS cleanup step remain open; this
  is progress toward the gate, not its completion.

## 2026-09-27 — Typed child stream inheritance

- Added the immutable `DosChildInheritedResources` value struct for borrowed
  input, output, error, and current-directory resources. Native and generic DOS
  child preparation now accept this named shape while the existing scalar
  entrypoints remain compatibility wrappers. No guest ABI offsets were added;
  process and CLI layouts remain behind their typed codecs.
- `PrepareShellNative` and loaded-image preparation now duplicate a distinct
  error stream into the child process state before allocating/publishing the
  CLI. If duplication fails, the existing rejected-child retirement path owns
  rollback. Shell NewCLI/NewShell, Run, and script-command launch paths pass
  the complete resource struct. Shell publication no longer attempts a second
  error-handle install.
- Added host-test source for typed stderr ownership, pre-publication state, and
  abort cleanup; updated the Shell publication helper case to provide its error
  handle through typed preparation. Builds succeeded for `CopperStart.Dos`,
  `CopperOS.Shell.Dos`, and `CopperOS.Shell.Dos.NativeRoot` with zero warnings
  or errors. The focused test assembly also compiled with zero errors and one
  existing transitive `NU1902` warning; test executables were not run.
- Release native receipt:
  `artifacts/shell-child-resource-20260927-release-v1/qualification.json`.
  MC68000, MC68020, and MC68040 each report 2,495 reachable methods, zero
  managed allocation sites, zero runtime features/helpers, zero external
  native targets, zero exception regions, and zero fatal machine-fault sites.
  This is isolated compiler/static evidence only; Shell guest execution,
  MorphOS stream-interaction traces, `Shell-Seg` staging, and boot handoff are
  not verified. Lifecycle retry/concurrency and PURE/boot gates remain open.

## 2026-09-27 — Generic NewCLI/NewShell stream creation and console specs

- Implemented the previously missing generic DOS Shell adapter path for
  `NewCLI` / `NewShell`: it now prepares typed child CLI startup and inherited
  input/output/error/current-directory resources, creates the requested
  console handles, and applies bounded default startup-script handling. The
  MorphOS `WINDOW` / `FROM` command forms remain the reference
  ([NewCLI](https://library.morph.zone/Shell_Commands/NewCLI),
  [NewShell](https://library.morph.zone/Shell_Commands/NewShell)).
- Follow-up audit found that ordinary DOS handle duplication reopened every
  console as plain `CON:`, losing caller-supplied `CON:` options. Added the
  named `DosConsoleOpenSpecification` value inside the private handle struct;
  explicit specs are copied into DOS-owned guest memory and independently
  reopened for child streams. Handle close, reset, and rollback paths reclaim
  the copied specification. Public `FileHandle` layout is unchanged; the
  private DOS handle record grows to include this typed metadata. Serialization
  offsets remain confined to the record codec. DOS state version advances to
  28, so an older live private layout is rejected rather than interpreted with
  the new handle size.
- Debug builds succeeded for `CopperStart.Dos`, `CopperOS.Shell.Dos`, and
  `CopperOS.Shell.Dos.NativeRoot` with zero warnings/errors. The focused DOS
  test project compiled (zero errors, one existing transitive `NU1902` package
  warning); no test executables were run.
- Release native qualification receipt:
  `artifacts/shell-generic-newcli-20260927-release-v1/qualification.json`.
  Full Execute roots compiled for MC68000, MC68020, and MC68040; each reports
  2,503 reachable methods, zero managed allocation sites, runtime features or
  helpers, external native targets, exception regions, and fatal machine-fault
  sites. This is isolated compiler/static evidence, not proof of MorphOS
  behavior or guest execution. Stream interaction, child return/wait/ack,
  `Shell-Seg` staging, concurrency/reset, PURE qualification, and boot
  integration remain open gates.

## 2026-09-27 — Private DOS layout version follow-up

- The console-spec record expansion changed the private handle allocation
  schema, so DOS `StateVersion` is now 28. The initialization contract rejects
  an older live layout instead of clearing it or reading its smaller handle
  records as the new structure.
- Rebuilt the DOS library, generic Shell adapter, native-root adapter, and
  focused DOS test assembly. All compiled successfully; the test executable was
  not run. The test build reported the same existing transitive `NU1902`
  warning.
- Re-ran full Execute Release qualification against this source as
  `artifacts/shell-generic-newcli-20260927-release-v2/qualification.json`.
  MC68000, MC68020, and MC68040 each report 2,503 reachable methods and zero
  managed allocation sites, runtime features/helpers, external targets,
  exception regions, and fatal machine-fault sites. Guest execution and
  shipping admission remain false; this receipt does not close MorphOS,
  lifecycle, PURE, or boot gates.

## 2026-09-27 — Interactive child default console

- The MorphOS references describe NewCLI and NewShell as starting a new
  interactive CLI/Shell, with `WINDOW` optional; NewShell’s `FROM` is its
  startup script (default `S:Shell-Startup`). Based on that behavior, both the
  native and generic launch adapters now open a default `CON:` handle when no
  window was supplied and no NewCLI `FROM` console provides the streams.
  NewShell’s `FROM` script remains distinct from its interactive console.
  References: [MorphOS NewCLI](https://library.morph.zone/Shell_Commands/NewCLI),
  [MorphOS NewShell](https://library.morph.zone/Shell_Commands/NewShell).
- Added a typed DOS-owned default-console open path, then routed the handle
  through the existing inherited-resource struct and normal child cleanup.
  Explicit `WINDOW` specs and NewCLI `FROM` console specs retain their existing
  selection behavior. Exact MorphOS device/window interaction still needs
  black-box traces.
- Debug builds of `CopperStart.Dos`, `CopperOS.Shell.Dos`, and
  `CopperOS.Shell.Dos.NativeRoot` succeeded with zero warnings/errors. No test
  executables were run.
- Full Execute Release static qualification passed for MC68000, MC68020, and
  MC68040 in `artifacts/shell-default-console-20260927-release-v1/qualification.json`.
  Each target reports 2,504 reachable methods and zero managed allocation
  sites, runtime features/helpers, external native targets, exception regions,
  or fatal machine-fault sites. This does not establish guest execution,
  MorphOS stream behavior, Shell-Seg staging, or shipping admission.

## 2026-09-27 — Typed ReadArgs numeric values

- Added `ReadArgsLongValueRecord` and its checked codec for `/N` pointees.
  The codec validates an even, mapped four-byte guest span before reading the
  signed `LONG`; callers retain their existing command-specific interpretation.
  Echo, Stack, Failat, Quit, Run, and Fault no longer dereference ReadArgs
  numeric pointers directly. ReadArgs result arrays continue to use their
  named command records/codecs; byte positions remain internal to those codecs.
- Echo's documented `FIRST` / `LEN` behavior (including rightmost characters
  for `LEN` without `FIRST`) is unchanged per the
  [MorphOS Echo reference](https://library.morph.zone/Shell_Commands/Echo).
  The MorphOS documentation does not define negative substring values, so
  their observed interpretation remains an open trace question. Stack's
  documented byte-sized per-Shell behavior remains unchanged
  ([MorphOS Stack reference](https://library.morph.zone/Shell_Commands/Stack)).
- Added unmapped and odd-address cases for Echo and Stack to the test source.
  Debug builds succeeded for `CopperOS.Shell.Dos`,
  `CopperOS.Shell.Dos.NativeRoot`, and the command test assembly with zero
  warnings/errors; no test executable was run.
- Full Execute Release static qualification passed for MC68000, MC68020, and
  MC68040 in `artifacts/shell-readargs-long-record-20260927-release-v2/qualification.json`.
  Each target reports 2,505 reachable methods and zero managed allocation
  sites, runtime features/helpers, external native targets, exception regions,
  or fatal machine-fault sites. Guest execution, MorphOS parity, lifecycle,
  PURE, Shell-Seg staging, and boot integration remain open.

## 2026-09-27 — ReadArgs multiple-template validation

- `CopperStart.Dos` now rejects templates containing more than one `/M`
  qualifier, whether repeated on the same entry or used by separate entries.
  Both forms fail during template validation with `BadTemplate`; command-side
  parsing continues to use named result structs instead of command-specific
  raw result-array offsets.
- Added focused test cases in CopperStart's DOS test assembly. Debug builds of
  `CopperStart.Dos` and `CopperStart.Exec.Tests` compiled successfully; the test
  executable was not run. The test build reported the existing transitive
  `NU1902` warning from Copper68k.
- Full Execute Release static qualification passed for MC68000, MC68020, and
  MC68040 in `artifacts/shell-readargs-multiple-template-20260927-release-v1/qualification.json`.
  Each target reports 2,505 reachable methods and zero managed allocation
  sites, runtime features/helpers, external native targets, exception regions,
  or fatal machine-fault sites. This is static compiler qualification only:
  guest execution and shipping admission remain false.
- The API baseline is the
  [AmigaDOS ROM Kernel Reference Manual, ReadArgs section](https://developer.amigaos3.net/sites/default/files/downloads/2024-10/Amiga_ROM_Kernel_Reference_Manual_DOS.pdf),
  which specifies at most one `/M` entry. MorphOS-specific parser parity has
  not been differentially verified. Interactive `?` help and `RDAF_NOPROMPT`
  behavior remain open ReadArgs work, along with the larger Shell lifecycle,
  MorphOS comparison, PURE, and boot gates.

## 2026-09-27 — ReadArgs `/M` overflow error

- A full `/M` result list now reports `LineTooLong` rather than
  `TooManyArguments`, matching the ReadArgs error contract for excess
  multi-arguments. The change stays in the DOS parser and continues to access
  the list through its named `ReadArgsMultipleVector` view.
- Added a boundary test that supplies 257 values for the supported 256-entry
  `/M` list and checks failure, `LineTooLong`, and cleared result storage.
  `CopperStart.Dos` and its test assembly compiled; no test executable was run.
  The test build emitted the existing transitive `NU1902` warning from
  Copper68k.
- Full Execute Release static qualification passed for MC68000, MC68020, and
  MC68040 in `artifacts/shell-readargs-multiple-overflow-20260927-release-v1/qualification.json`.
  Each target reports 2,505 reachable methods and zero managed allocation
  sites, runtime features/helpers, external native targets, exception regions,
  or fatal machine-fault sites. Guest execution and shipping admission remain
  false. The error-code reference baseline is the
  [AmigaDOS ROM Kernel Reference Manual, ReadArgs section](https://developer.amigaos3.net/sites/default/files/downloads/2024-10/Amiga_ROM_Kernel_Reference_Manual_DOS.pdf);
  MorphOS-specific differential verification remains open.

## 2026-09-27 — ReadArgs interactive help and no-prompt flag

- Added the public typed `DosRdArgsFlags` enum in CopperSharp SDK and used it
  for `RDAF_NOALLOC` and `RDAF_NOPROMPT`; the public `RDArgs.Flags` field stays
  its fixed-width ABI `LONG`. Parsing detects only a lone, unquoted first `?`
  (including the `ReadItem` semicolon terminator), prints the template, and
  retries from standard input. A second lone `?` prints `RDA_ExtHelp` when
  supplied and makes the final retry. With `RDAF_NOPROMPT`, `?` remains a
  normal argument; a quoted `"?"` or `?` followed by another argument is not
  help. Source boundaries and retry state use the existing `RDArgs` / `CSource`
  structs and codecs; help output uses DOS `WriteChars` and stops on write
  failure. `RDAF_STDIN` remains unused as specified by the reference manual.
- Added focused test source for template help/retry, the literal no-prompt
  case, question mark followed by another argument or semicolon, a quoted
  question mark, extended help, and an implicitly created `RDArgs` reading
  standard input. The DOS library and focused test assembly compile
  successfully; tests were not executed. The test-project build reported the
  existing transitive `NU1902` advisory from Copper68k.
- Full Execute Release static qualification passed for MC68000, MC68020, and
  MC68040 in `artifacts/shell-readargs-prompt-20260927-release-v4/qualification.json`.
  Each target reports 2,511 reachable methods and zero managed allocation
  sites, runtime features/helpers, external native targets, exception regions,
  or fatal machine-fault sites. This is static compiler evidence only;
  `guestExecution` and `shippingAdmission` remain false.
- The `?`, `RDA_ExtHelp`, and `RDAF_NOPROMPT` behavior follows the
  [AmigaDOS ROM Kernel Reference Manual, §15.5.1](https://developer.amigaos3.net/sites/default/files/downloads/2024-10/Amiga_ROM_Kernel_Reference_Manual_DOS.pdf).
  MorphOS's SDK confirms the `ReadArgs` ABI, but this interactive behavior has
  not been differentially verified on MorphOS 3.20. Shell lifecycle, PURE,
  runtime, and boot integration gates remain open.

## 2026-09-27 — Resident SYSTEM-filtered listing

- MorphOS documents `Resident` without `FILE` as a list operation and says
  `SYSTEM` shows only system components. The DOS-owned resident listing now
  applies that flag to the named `System` bit in each resident record; an
  unfiltered list still emits all DOS-owned resident entries.
- Added a focused test source that creates one system and one user resident,
  checks the `SYSTEM`-only output, and then checks unfiltered output. The DOS
  library and focused test assembly compile; tests were not executed. The test
  build reports the existing transitive `NU1902` advisory from Copper68k.
- Full Execute Release static qualification passed for MC68000, MC68020, and
  MC68040 in `artifacts/shell-resident-system-filter-20260927-release-v1/qualification.json`.
  Each target reports 2,511 reachable methods and zero managed allocation
  sites, runtime features/helpers, external native targets, exception regions,
  or fatal machine-fault sites. Guest execution and shipping admission remain
  false.
- The list/filter behavior follows the
  [MorphOS Resident reference](https://library.morph.zone/Shell_Commands/Resident).
  This step covers DOS-owned resident entries; the reference's built-in
  internal-command listing, removal, and reactivation behavior remains open,
  as did verified PURE admission at this earlier checkpoint. The later
  protection-bit entry below supersedes that status; exact artifact/hash
  admission and MorphOS runtime traces remain open.

## 2026-09-27 — Resident internal-command controls

- Added a fixed-width `DosShellInternalCommandStateRecord` to the DOS object
  chain. Its named struct fields hold the owning CLI, a disabled-command
  bitmask, and reserved flags; byte offsets remain inside its record codec.
  Missing state means all built-ins are enabled. State is reclaimed when the
  CLI retires or DOS state resets, and a new CLI starts with the defaults.
- Shell resolution now consults DOS-owned availability before dispatch. A
  disabled built-in falls through to ordinary resident/file lookup. With no
  `FILE`, `Resident NAME REMOVE` disables a built-in and `Resident NAME
  REPLACE` restores it. An ordinary list includes active built-ins; `SYSTEM`
  remains a system-resident-only list. This classification follows the
  distinction between internal commands and `SYSTEM` components in the
  [MorphOS Resident reference](https://library.morph.zone/Shell_Commands/Resident);
  exact list order and whether command state is scoped per CLI still need a
  MorphOS trace.
- Added test source for DOS struct-backed state isolation, reactivation,
  listing, and resolution fallback. The CopperOS Commands test assembly and
  CopperStart Exec test assembly compiled successfully; test binaries were
  not run.
- Full Execute Release static qualification passed for MC68000, MC68020, and
  MC68040 in
  `artifacts/shell-resident-internals-20260927-release-v1/qualification.json`.
  Each target reports 2,531 reachable methods and zero managed allocation
  sites, runtime features/helpers, external native targets, exception regions,
  or fatal machine-fault sites. This is static compiler evidence only;
  `guestExecution` and `shippingAdmission` remain false.
- At this checkpoint, verified PURE admission remained open; the later
  protection-bit entry below supersedes that status. The `REMOVE`/`REPLACE`
  internal-command behavior is supported by the
  [AmigaDOS command reference](https://www.jaruzel.com/amiga/amiga-os-command-reference-help/resident.html);
  MorphOS runtime differential verification and boot integration remain open.

## 2026-09-27 — Fail-closed resident admission and typed requests

- This was an interim fail-closed checkpoint and is superseded by the later
  protection-bit admission entry below.
- DOS resident add/replace now take the named fixed-width
  `DosResidentAddRequest` struct internally rather than exposing a long
  positional parameter list. Admission validation consumes that record by
  named fields; guest-memory offsets remain encapsulated by the record codec.
- Until the external command purity qualification is connected, ordinary
  `Resident` add/replace without `PURE=FORCE` now fails with
  `InvalidResidentLibrary` before loading the file. Low-level admission rejects
  records that are neither verified nor explicitly forced. The MorphOS tagged
  segment path no longer treats deferred loading as proof of purity; unverified
  entries are marked unsafe instead of `VerifiedPure`.
- Added test source for unqualified admission refusal before file loading and
  unsafe forced classification. Both CopperStart Exec tests and CopperOS
  Commands tests compiled; test executables were not run. The CopperStart test
  build reports the existing `NU1902` advisory from Copper68k.
- Full Execute Release static qualification passed for MC68000, MC68020, and
  MC68040 in
  `artifacts/shell-resident-admission-20260927-release-v1/qualification.json`.
  Each target reports 2,531 reachable methods and zero managed allocation
  sites, runtime features/helpers, external native targets, exception regions,
  or fatal machine-fault sites. This is static compiler evidence only;
  `guestExecution` and `shippingAdmission` remain false.
- Exact manifest/hash verification was not integrated at this checkpoint;
  the runtime behavior, forced warning, and default operation were added
  later. MorphOS runtime differential behavior and boot integration remain
  open.

## 2026-09-27 — Resident PURE-bit admission and typed management request

- `Resident FILE` now examines the DOS `FileInfoBlock` before loading. An
  ordinary add/replace is admitted only when its named
  `FileProtection.Pure` bit is set. An unmarked file is rejected before HUNK
  reads unless `PURE=FORCE` is supplied. Forced unmarked entries emit a
  warning and remain explicitly unsafe; deferred loading does not establish
  purity. `Resident FILE` with no operation switch defaults to `REPLACE`.
- The command-to-DOS boundary now passes a named fixed-width request struct
  through the Shell platform, DOS bridge, and registry manager. Resident
  entry/admission state also remains struct-backed; byte offsets stay inside
  the codecs that encode the fixed guest ABI records.
- This uses MorphOS's documented PURE protection-bit requirement, force
  behavior, and default `REPLACE` operation
  ([MorphOS Resident reference](https://library.morph.zone/Shell_Commands/Resident)).
  The warning text is CopperOS wording, not a claim of exact MorphOS output.
- The CopperStart Exec test assembly and CopperOS Commands test assembly
  compiled successfully; no test executable or guest code was run. The
  CopperStart build reports the existing transitive `NU1902` advisory.
- Full Execute Release static qualification passed for MC68000, MC68020, and
  MC68040 in
  `artifacts/shell-resident-typed-request-20260927-release-v1/qualification.json`.
  Each target reports 2,549 reachable methods and zero managed allocation
  sites, runtime features/helpers, external native targets, exception regions,
  or fatal machine-fault sites. This is compiler/static evidence only;
  `guestExecution` and `shippingAdmission` remain false.
- Runtime P-bit checking does not close the external exact-hash packaging
  gate: the 200-item MorphOS command inventory records zero qualified
  replacement artifacts and zero observed installed PURE flags; all 88 items
  whose observed design evidence requires PURE remain unqualified. Exact
  MorphOS traces, concurrency, and boot integration remain open.

## 2026-09-27 — DOS command lookup through CLI Path

- Extended the typed DOS Shell process context with its `CommandPath` BPTR,
  sourced from the named `DosProcessStateRecord` rather than a call-site field
  offset. Command lookup now checks resident entries, explicit paths, the
  current-directory lock, then each validated `PathLock` in the CLI command
  path. The resolved candidate is kept in caller-owned path storage; lookup
  classifications flow through both Shell adapters and native launch.
- Added test source for explicit-path classification/path preservation and a
  current-directory miss followed by a CLI-Path hit. CopperOS Shell DOS and
  CopperStart Exec test assemblies compile; tests were not executed.
- Full Execute Release static qualification passed for MC68000, MC68020, and
  MC68040 in
  `artifacts/shell-command-lookup-20260927-release-v1/qualification.json`.
  Each target reports 2,575 reachable methods and zero managed allocation
  sites, runtime features/helpers, external native targets, exception regions,
  or fatal machine-fault sites. This is compiler/static evidence only;
  `guestExecution` and `shippingAdmission` remain false.
- Exact MorphOS path-resolution traces, broader current-directory/CLI-Path
  behavioral coverage, script/file classification edges, PURE artifact
  qualification, and boot integration remain open.

## 2026-09-27 — Struct-backed command lookup result

- Replaced the DOS command lookup's separate `out` classification and length
  values with the named `CommandLookupResult` struct (`Kind`, `PathLength`).
  Shell launch and adapter code now consumes those named fields; the existing
  guest-memory field layouts remain owned by their typed codecs.
- The Shell/DOS project and CopperStart Exec test assembly compile. No tests or
  guest code were run; the Exec build reports its existing transitive
  `NU1902` advisory.
- Full Execute Release static qualification passed for MC68000, MC68020, and
  MC68040 in
  `artifacts/shell-command-lookup-result-struct-20260927-release-v1/qualification.json`.
  Each target reports 2,575 reachable methods and zero managed allocation
  sites, runtime features/helpers, external native targets, exception regions,
  or fatal machine-fault sites. This is compiler/static evidence only;
  `guestExecution` and `shippingAdmission` remain false.
- Script-protected-file classification and nested script execution remain
  open, along with exact MorphOS lookup traces and boot integration.

## 2026-09-27 — FileInfoBlock-backed script classification

- DOS lookup now examines each opened explicit, current-directory, or CLI-Path
  candidate using `ExamineFH` and `DosFileInfoBlockCodec`, then checks the
  typed `FileProtection.Script` bit. The path remains in caller-owned storage;
  candidate metadata is a named value struct. No raw FileInfoBlock field
  offsets were added to Shell logic.
- Added compile-only test source for an S-protected explicit file and retained
  the ordinary-file and CLI-Path lookup cases. The Shell/DOS project and
  CopperStart Exec test assembly compile; tests and guest code were not run.
  The Exec build reports the existing transitive `NU1902` advisory.
- MorphOS's Execute reference documents the filename-only shortcut for
  S-protected files in the search path. Whether explicit-path handling should
  share that shortcut remains unverified; no exact trace claim is made.
- Full Execute Release static qualification passed for MC68000, MC68020, and
  MC68040 in
  `artifacts/shell-script-protection-20260927-release-v1/qualification.json`.
  Each target reports 2,578 reachable methods and zero managed allocation
  sites, runtime features/helpers, external native targets, exception regions,
  or fatal machine-fault sites. This is compiler/static evidence only;
  `guestExecution` and `shippingAdmission` remain false.
- Script execution routing/nesting, exact path behavior, `#!` policy, MorphOS
  traces, and boot integration remain open.

## 2026-09-27 — Struct-backed lookup-to-launch metadata

- Extended the DOS-owned `CommandLookupResult` with named `Origin` and
  `Protection` fields. The Shell boundary now carries one
  `ShellScriptLookupResult` containing kind, origin, `FileProtection`, the
  caller-workspace path pointer, and path length from lookup through launch.
  Shell validation checks these named fields; raw guest-record offsets remain
  in their owning codecs.
- Added compile-only assertions for explicit-file, CLI-Path, and
  script-protection metadata. The Shell/DOS project, CopperOS Commands test
  assembly, and CopperStart Exec test assembly compile successfully. No tests
  or guest code were run; the Exec build reports its existing transitive
  `NU1902` advisory.
- Full Execute Release static qualification passed for MC68000, MC68020, and
  MC68040 in
  `artifacts/shell-lookup-result-struct-20260927-release-v2/qualification.json`.
  Each target reports 2,580 reachable methods and zero managed allocations,
  runtime features/helpers, external targets, exception regions, or fatal
  machine-fault sites. This is static compiler evidence only;
  `guestExecution` and `shippingAdmission` remain false.
- Script-origin/protection metadata is now available for later execution
  policy, but `Script` results are still refused by the HUNK launch path.
  Nested script routing, exact MorphOS path behavior, `#!` policy, traces, and
  boot integration remain open.

## 2026-09-27 — S-protected CLI-Path command dispatch

- Added the named `ShellScriptCommandInvocation` record for the parsed command
  name, line span, and raw argument slice. Normal external launch and the
  script shortcut now use this record rather than re-parsing a raw first-token
  length in the DOS launcher.
- An S-protected result is dispatched only when its typed origin is
  `CommandPath` and its typed protection includes `FileProtection.Script`.
  DOS builds a bounded `Execute "<resolved path>" <original argument tail>`
  line in frame-owned storage and launches the ordinary `Execute` HUNK through
  the existing child continuation lifecycle. The S-protected file itself is
  never passed to the HUNK loader. Explicit-file and current-directory script
  cases still fail closed pending MorphOS evidence.
- MorphOS documents that the Execute command need not be typed when an
  S-protected file is in the search path. Routing that case through the
  external Execute program is an implementation inference, not a claim about
  MorphOS internals. Runtime command availability, quoting edges, exact
  behavior, and full Execute/CLI integration remain unverified.
- The Shell/DOS project and CopperOS Commands test assembly compile; the
  CopperStart Exec test assembly also compiles with its existing transitive
  `NU1902` advisory. No tests or guest code were run.
- Full Execute Release static qualification passed for MC68000, MC68020, and
  MC68040 in
  `artifacts/shell-script-protected-command-20260927-release-v1/qualification.json`.
  Each target reports 2,581 reachable methods and zero managed allocations,
  runtime features/helpers, external targets, exception regions, or fatal
  machine-fault sites. This is static compiler evidence only;
  `guestExecution` and `shippingAdmission` remain false.

## 2026-09-27 — Struct-backed ReadArgs template writer

- Replaced the 460 literal byte-position writes for command ReadArgs templates
  with a sequential `ShellReadArgsTemplateWriter` value type. Call sites append
  bytes in order; the guest-memory offset exists only as the writer's named
  cursor at the memory boundary. ReadArgs result decoding remains in its
  bounded typed cursor codec.
- A static source check confirmed all 26 templates emit their declared length
  plus a NUL terminator. The Shell DOS Release project builds with zero
  warnings and errors. No tests or guest code were run.

## 2026-09-27 — Bounded struct-backed script-command writer

- Replaced the script shortcut's manually incremented `Execute` line cursor
  with a `ShellLaunchTextWriter` value type. Its append helpers validate the
  destination span, capacity, and source range; the encoded line length is
  checked against the precomputed command length before the child launch.
- The Shell DOS Release project builds with zero warnings and errors. No tests
  or guest code were run.

## 2026-09-27 — Struct-backed Execute Shell-number expansion

- Added `<$$>` expansion for the current Shell/CLI number. The Shell workspace
  carries it as a named field, populated from the process task number through
  `DosProcessCodec`; no raw process-structure offsets were added. Decimal
  formatting uses bounded digit emission without floating point or managed
  formatting APIs.
- Added compile-only test source covering a ten-digit Shell number. The Shell
  DOS Release project and CopperOS Commands test assembly both build with zero
  warnings and errors. No tests or guest code were run.
- The AmigaOS Command Reference documents `<$$>` as the current Shell number.
  MorphOS's Execute page documents script execution but does not mention this
  substitution, so it remains an AmigaDOS compatibility baseline pending a
  MorphOS runtime trace.

## 2026-09-27 — Struct-backed Echo ReadArgs template

- Moved Echo's `MESSAGE/M,NOLINE/S,FIRST/N,LEN/N,TO/K` template into the shared
  bounded `ShellReadArgsTemplateWriter`. Echo continues to call the DOS-owned
  `ReadArgs` parser and now obtains its template length from that shared
  template definition; its separate result-record buffer remains unchanged.
- Added a compile-only assertion for the exact template text. The Shell/DOS
  Release project and CopperOS Commands test assembly build with zero warnings
  and errors. No tests or guest code were run.

## 2026-09-27 — ReadArgs `/T` value coverage

- Added CopperStart DOS test source for the `/T` toggle's explicit `ON`/`OFF`
  and `Y`/`N` values, including case-insensitive and `KEY=value` forms, plus
  missing-value and invalid-value errors. This records documented behavior;
  it does not change the existing parser implementation.
- The CopperStart Exec test assembly compiles in Release. The build reports its
  existing transitive `NU1902` advisory from Copper68k. No tests or guest code
  were run.
- The value forms follow the
  [AmigaDOS ROM Kernel Reference Manual, ReadArgs §15.5.1](https://developer.amigaos3.net/sites/default/files/downloads/2024-10/Amiga_ROM_Kernel_Reference_Manual_DOS.pdf).
  MorphOS 3.20 differential behavior remains unverified.

## 2026-09-27 — Typed Execute .DEF records and native static checkpoint

- Execute's script-wide `.DEF` records now use the named
  `ShellScriptDefaultEntry` value type and `ShellScriptDefaultEntryCodec`.
  Record traversal and default lookup consume `NameLength`, `ValueLength`,
  `Name`, `Value`, and `EncodedLength`; big-endian field positions are confined
  to the owning codec. Bounds and guest mappings are validated on each read or
  write.
- The MC68000 compiler rejected mutation of a `uint` method argument in the
  decimal formatter. The formatter now copies the argument to a local before
  decrementing it, matching the native compiler's supported lowering.
- Full Execute Release static qualification passed for MC68000, MC68020, and
  MC68040 in
  `artifacts/shell-native-goal-20260927-v3/qualification.json`. Each target
  reports 2,607 reachable methods and zero managed allocations, runtime
  features/helpers, external native targets, exception regions, or fatal
  machine-fault sites. This is compile/static evidence only;
  `guestExecution` and `shippingAdmission` remain false.

## 2026-09-27 — MorphOS error/output stream merge

- Added the documented `*<>` operator. `ShellRedirectionSpec.ErrorToOutput`
  records the routing decision by name; redirection setup aliases the error
  handle to the selected output handle and does not claim or close it twice.
  Duplicate error destinations are rejected. This implements the
  AmigaDOS Shell reference's “errors to the same file as output” rule; exact
  MorphOS 3.20 behavior remains a runtime differential gate.
- Added compile-only regression sources for `*<>` parsing, conflict with a
  separate error target, and shared-handle close ownership. Release builds of
  `CopperOS.Commands.Tests` and `CopperOS.Shell.Dos` passed with zero warnings
  or errors. Tests and guest code were not run.
- Isolated static Shell-task qualification passed MC68000/020/040 in
  `artifacts/shell-native-redirection-merge-20260927-v1/qualification.json`.
  Each target reports 2,121 reachable methods and zero allocation sites,
  runtime features/helpers, external targets, exception regions, or fatal
  machine-fault sites. The staged 706,528-byte 68000 HUNK has SHA-256
  `a6d452a5176e2d6e16bf0c7b7e79d35049b09dba6f6baa7e6f16f26c3c00abff`;
  the previous staged HUNK remains preserved in
  `artifacts/shell-native-redirection-20260927-v1/`. `guestExecution` and
  `shippingAdmission` remain false.
- No tests or guest code were run. The qualification entry remains the
  compile-time `CapabilityRoot`, not a production Shell startup entry, so its
  HUNK is not staged as `filesystem/SYS/L/Shell-Seg`. Boot and interactive
  Shell qualification remain open.

## 2026-09-27 — Struct-backed Execute directive state

- Moved Execute's six-byte `.KEY` directive metadata behind
  `ShellScriptKeyDirectiveStateCodec`. The Shell now reads and updates named
  opening-bracket, closing-bracket, dollar-marker, and dot-marker fields;
  field offsets and magic bytes are owned by that codec. Default-list records
  continue through `ShellScriptDefaultEntryCodec`.
- Full Execute Release static qualification passed for MC68000, MC68020, and
  MC68040 in
  `artifacts/shell-native-goal-20260927-v4/qualification.json`. Each target
  reports 2,620 reachable methods and zero managed allocations, runtime
  features/helpers, external native targets, exception regions, or fatal
  machine-fault sites. This is compile/static evidence only;
  `guestExecution` and `shippingAdmission` remain false.
- No tests or guest code were run. The compile-time capability root still is
  not a production Shell startup entry, so no runtime HUNK was staged. Boot,
  interactive Shell, and MorphOS differential qualification remain open.

## 2026-09-27 — Production Shell task HUNK staged

- Added the parameterless `ShellTaskEntry` (`copperos.shell.entry`) and kept
  the existing A6-register `ShellChildEntry` ABI as a delegating child entry.
  The first build attempt correctly rejected the child ABI as a HUNK image
  entry; that failed evidence remains in
  `artifacts/shell-native-task-20260927-v1/`. The isolated native qualifier now
  has a distinct `-UseShellTaskEntry` mode rooted at the production task entry.
- Full three-target static qualification passed in
  `artifacts/shell-native-task-20260927-v2/qualification.json`: 2,075 reachable
  methods per CPU and zero managed allocations, runtime features/helpers,
  external native targets, exception regions, or fatal machine-fault sites.
  The 68000 HUNK was copied to `filesystem/SYS/L/Shell-Seg`; its SHA-256 matches
  the receipt. `guestExecution` and `shippingAdmission` remain false.
- The first README edit incorrectly described the `Resident ... SYSTEM` command
  as an available opt-in. A follow-up check found that CopperStart DOS requires
  the file's Amiga `PURE` protection bit for normal resident admission, while
  this staged host file has no explicit Amiga protection record and static
  compilation does not prove reentrancy/re-executability. `filesystem/README.md`
  now marks registration as blocked pending independent PURE qualification and
  explicit runtime-file protection metadata. `PURE=FORCE` is not used. No
  automatic startup sequence or external DOS profile was changed. DOS
  registration, guest execution, startup/interactive traces, and boot
  integration remain open; no tests or guest code were run.
- This gate follows the [MorphOS Resident reference](https://library.morph.zone/Shell_Commands/Resident),
  which says normal resident commands should carry the pure protection bit and
  warns that forcing an unmarked command is unsafe.

## 2026-09-27 — Interactive MorphOS prompt slice

- The script frame now carries a named `Interactive` flag. The runner writes a
  prompt before reading only when the child CLI's `Interactive` field is set
  and its current input is the standard input; script readers remain promptless.
- Added DOS-owned prompt expansion using the typed CLI, task-number, and BSTR
  codecs and `DosShellPromptContext` value type. Child CLI backing storage now
  exposes named `DosCliStorageLayout` fields instead of requiring callers to
  advance through anonymous string offsets. `%N`, `%S`, and `%R` expand the
  Shell number, current directory, and previous return code. New CLIs and
  `Prompt` reset use MorphOS's `%N.%S>` default, while explicitly empty prompt
  templates remain empty.
- New child CLIs reserve the full 255-byte prompt capacity while copying the
  inherited prompt's existing length separately, so later prompt changes and
  reset cannot overrun a short inherited BSTR.
- Backtick command substitution remains unsupported and reports the DOS
  `ActionNotKnown` error, now detected before emitting any prompt bytes so the
  unsupported path cannot leave a partial prompt. It must be implemented
  before this prompt path is considered MorphOS-complete. The behavior and
  substitutions follow the
  [MorphOS Prompt reference](https://library.morph.zone/Shell_Commands/Prompt);
  the interactive-input gate follows the CLI/Shell distinction described in
  the [UserShell documentation](https://wiki.amigaos.net/wiki/Writing_a_UserShell).
- Release builds succeeded for `CopperOS.Shell.Dos`, the Commands test
  assembly, and the CopperStart DOS native-integration test assembly (compile
  only). The integration-test build reported the existing transitive `NU1902`
  advisory in Copper68k. No tests or guest code were run.
- Isolated native Shell-task static qualification passed for MC68000, MC68020,
  and MC68040 in
  `artifacts/shell-native-prompt-20260927-v4/qualification.json`. Each target
  reports 2,085 reachable methods and zero managed allocations, runtime
  features/helpers, external native targets, exception regions, or fatal
  machine-fault sites. `guestExecution` and `shippingAdmission` remain false.
  The 686,504-byte MC68000 HUNK was staged to `filesystem/SYS/L/Shell-Seg` with
  SHA-256 `ab4121d27dab66a610c0077dd5c81b1a3d36eb433813f9b9bc245f6056c5285e`;
  the previous HUNKs remain preserved in the v1, v2, and v3 receipt directories.

## 2026-09-27 — Typed prompt-segment parser foundation

- Added fixed-width `ShellScriptPromptTemplate`, `ShellScriptPromptCursor`,
  and `ShellScriptPromptSegment` structs. The bounded parser returns literal
  slices and backtick-delimited command slices without allocating or invoking
  commands; unmatched delimiters and invalid bounds are rejected.
- This is parser groundwork only. It is not yet connected to the interactive
  prompt continuation path, so command substitution remains unsupported and
  the existing preflight still rejects such prompts before writing output.
- Release builds succeeded for the Shell library, the Commands test assembly,
  and `CopperOS.Shell.Dos` (compile only). No tests or guest code were run, and
  no native HUNK was staged for this parser-only change.

## 2026-09-27 — Prompt literal segments wired into Shell

- The interactive Shell now snapshots its prompt into the runner's bounded
  guest line buffer and preflights typed prompt segments before emitting any
  bytes. Literal segments are sent to DOS as a named `DosShellPromptTextSpan`,
  where `%N`, `%S`, and `%R` are expanded. Backtick commands still take the
  historical `ActionNotKnown` path before any literal prefix is written.
- Release builds succeeded for the Commands test assembly and the DOS-native
  Shell unit (compile only). Isolated static qualification of the production
  Shell task entry passed MC68000/020/040 in
  `artifacts/shell-native-prompt-span-20260927-v1/qualification.json`: 2,097
  reachable methods per target, with zero managed allocations, runtime
  features/helpers, external native targets, exception regions, or fatal
  machine-fault sites. The resulting 689,908-byte 68000 HUNK is staged at
  `filesystem/SYS/L/Shell-Seg`, SHA-256
  `b7d0ca3d1db3d21c053b8b4411be2ee7bae021b879b0623ae0eca91efb0f4e25`.
- No tests or guest code were run. `guestExecution` and `shippingAdmission`
  remain false; MorphOS behavior beyond documented prompt substitutions and
  backtick delimiter recognition remains to be verified. The previous staged
  HUNK is retained in the v4 receipt directory.

## 2026-09-27 — Resumable prompt command substitution

- Implemented backtick prompt commands through the existing Shell alias,
  redirection, internal-command, and external-command paths. Literal spans
  before and after each command keep DOS `%N`, `%S`, and `%R` substitutions.
  The command's default standard output is spooled to a frame-specific
  `T:Prompt.*` file and copied to the prompt output in bounded 4096-byte
  reads; explicit command redirection still takes precedence.
- External commands retain the Shell's existing foreground continuation and
  polling lifecycle. Prompt progress uses a named fixed-width
  `ShellScriptPromptExpansionState` embedded in the Shell frame, and the DOS
  runner records its prompt snapshot and capture-reader ownership. No nested
  engine call or recursive command execution was added. The Shell frame codec
  is version 4 / 132 bytes; the DOS runner codec is version 6 / 132 bytes.
- The [MorphOS Prompt reference](https://library.morph.zone/Shell_Commands/Prompt)
  confirms that commands may be embedded in backticks, but it does not describe
  newline trimming or other output normalization. This implementation currently
  preserves captured bytes, so exact output formatting still needs differential
  compatibility verification.
- Release compile-only builds succeeded for `CopperOS.Shell.Dos` and the
  Commands test assembly with zero warnings or errors. The test sources include
  coverage for captured internal-command output and unmatched backticks, but no
  tests or guest code were run.
- Isolated production Shell-task static qualification passed for MC68000,
  MC68020, and MC68040 in
  `artifacts/shell-native-prompt-command-20260927-v2/qualification.json`.
  Each target reports 2,116 reachable methods and zero managed allocations,
  runtime features/helpers, external native targets, exception regions, or
  fatal machine-fault sites. The 701,376-byte MC68000 HUNK is staged at
  `filesystem/SYS/L/Shell-Seg`, SHA-256
  `a066e47c2114f502edd6c371689934c9a881f3974e092c8adfe92328395b8d69`. The
  prior HUNK remains preserved in the v1 prompt-span artifact directory.
- `guestExecution` and `shippingAdmission` remain false. No tests were run;
  resident/PURE qualification and actual MorphOS behavior remain open.

## 2026-09-27 — Malformed prompt command recovery and refreshed Shell artifact

- A syntax error inside a closed prompt backtick span now follows ordinary
  Shell error handling: diagnostics and `LastResult` are recorded, the typed
  `ShellScriptPromptExpansionState.Cursor` advances to the next prompt segment,
  and subsequent literal prompt text is still emitted. The new regression test
  source covers an unterminated quote in an embedded `Echo` command; tests and
  guest code were not run.
- The continuation remains struct-backed. Prompt execution reads and updates
  named frame/segment/diagnostics fields; no new guest-memory offsets were
  introduced outside `ShellScriptFrameCodec`.
- Updated `src/System/Shell/README.md` to describe the implemented prompt path
  and current frame size/version instead of the obsolete unsupported-path note.
- Release compile-only builds succeeded for the Commands test assembly and
  `CopperOS.Shell.Dos`, both with zero warnings/errors. Native static
  qualification passed for MC68000/20/40 in
  `artifacts/shell-native-prompt-command-20260927-v3/qualification.json`.
  Each target reports 2,117 reachable methods and zero allocation sites,
  runtime features/helpers, external native targets, exception regions, and
  fatal machine-fault sites. The source snapshot includes the updated engine
  hash and was unchanged across qualification's managed build.
- Staged the new 701,720-byte MC68000 HUNK at `filesystem/SYS/L/Shell-Seg`,
  SHA-256
  `1927158fe009618a7882f44d766581bfaeaf7d458184934e568cb12a76076835`.
  The v2 artifact remains preserved. `guestExecution` and `shippingAdmission`
  remain false; MorphOS newline behavior, runtime behavior, and Resident/PURE
  qualification remain open.

## 2026-09-27 — Resident ReadArgs switch normalization

- `ResidentCommand` now validates ReadArgs switch fields and normalizes both
  canonical DOS true (`uint.MaxValue`) and the test/platform `1` representation
  to named 0/1 values before dispatch. It rejects mutually exclusive
  `REMOVE`/`ADD`/`REPLACE` combinations and rejects `ADD`, `FORCE`, `DEFER`, or
  `ALIAS` without a file instead of silently listing residents. `SYSTEM` list
  filtering also uses its normalized value.
- ReadArgs result handling stays struct-backed through
  `ResidentReadArgsResultRecord` and its owning codec; the command wrapper adds
  no raw result-array offsets. This follows the MorphOS [`Resident` command
  reference](https://library.morph.zone/Shell_Commands/Resident).
- Compile-only Release builds succeeded for `CopperOS.Shell.Dos` and the
  Commands test assembly with zero warnings/errors. Native static qualification
  passed MC68000/20/40 in
  `artifacts/shell-native-resident-normalization-20260927-v2/qualification.json`:
  2,119 reachable methods per target, with zero managed allocations, runtime
  features/helpers, external native targets, exception regions, or fatal
  machine-fault sites. The selected source snapshot was unchanged across the
  managed build.
- Staged the 702,028-byte MC68000 HUNK at `filesystem/SYS/L/Shell-Seg`, SHA-256
  `2cf12b291517f1f3e67a931f101e86865b08ef053ce6c270a9752d8abde7b190`.
  Earlier Shell HUNKs remain preserved in their receipt directories. Tests and
  guest code were not run; `guestExecution` and `shippingAdmission` remain
  false. MorphOS runtime behavior and resident/PURE qualification remain open.

## 2026-09-27 — ReadArgs keyword-qualified full arguments

- DOS `ReadArgs` now recognizes a `/F` slot's own keyword before consuming the
  remainder, so `COMMAND/K/F` accepts both `COMMAND value rest` and
  `COMMAND=value rest` without including the keyword or truncating the value
  to one token. A present keyword without a value reports `KeyNeedsArgument`.
- Added compile-only regression source using a named result record for the
  returned full string. The behavior follows the AmigaDOS ReadArgs contract:
  `/K` requires the keyword and `/F` consumes the rest of the line. Exact
  MorphOS 3.20 behavior remains a differential gate.
- Release compile-only builds succeeded for `CopperStart.Exec.Tests` (one
  existing transitive `NU1902` advisory), the CopperOS Commands test assembly,
  and `CopperOS.Shell.Dos`. No tests or guest code were run.
- Isolated static Shell-task qualification passed MC68000/20/40 in
  `artifacts/shell-native-readargs-full-keyword-20260927-v1/qualification.json`.
  Each target reports 2,119 reachable methods and zero managed allocations,
  runtime features/helpers, external native targets, exception regions, or
  fatal machine-fault sites. The selected source snapshot, including
  CopperStart `DosCore.cs`, was unchanged across the managed build.
- Staged the 702,508-byte MC68000 HUNK at `filesystem/SYS/L/Shell-Seg`, SHA-256
  `ec0680fd71d4730ddcf3fc5232c4ca0e0d81f716ecd4c07b70008776fa11337b`.
  The previous staged HUNK remains in the preceding receipt directory.
  `guestExecution` and `shippingAdmission` remain false; MorphOS runtime
  behavior and Resident/PURE qualification remain open.

## 2026-09-27 — Reject incompatible ReadArgs toggle modifiers

- DOS template validation now rejects `/T` combined with `/N`, `/M`, or `/S`.
  These modifiers require different result types or input rules from the
  `/T` boolean-ULONG result; `/A` and `/K` remain usable with `/T`. This
  compatibility rule is an inference from the documented modifier contracts
  in the [Amiga ROM Kernel Reference Manual, DOS §15.5.1](https://developer.amigaos3.net/sites/default/files/downloads/2024-10/Amiga_ROM_Kernel_Reference_Manual_DOS.pdf)
  and still needs MorphOS 3.20 differential confirmation.
- Added compile-only cases for all conflicting orderings. The test source does
  not use raw result-slot offsets. CopperStart `CopperStart.Exec.Tests` builds
  with the existing transitive `NU1902` advisory; the CopperOS Commands test
  assembly and `CopperOS.Shell.Dos` build with zero warnings/errors. No tests or
  guest code were run.
- Isolated static Shell-task qualification passed MC68000/20/40 in
  `artifacts/shell-native-readargs-template-conflicts-20260927-v1/qualification.json`.
  Each target reports 2,119 reachable methods and zero managed allocations,
  runtime features/helpers, external native targets, exception regions, or
  fatal machine-fault sites. The selected source snapshot, including
  CopperStart `DosCore.cs`, was unchanged across the managed build.
- Staged the 702,552-byte MC68000 HUNK at `filesystem/SYS/L/Shell-Seg`, SHA-256
  `6ee73c55e10af833b6f8dc5985fee1fe22d3948f3daa920c6f7b1ff06ae1a489`.
  Earlier HUNK candidates remain preserved in their receipt directories.
  `guestExecution` and `shippingAdmission` remain false; exact MorphOS template
  compatibility and resident/PURE qualification remain open.

## 2026-09-27 — Requalify the struct-backed Shell task artifact

- Rebuilt `CopperOS.Shell.Dos`, the CopperOS Commands test assembly, and
  `CopperStart.Exec.Tests` in Release without executing tests or guest code.
  The two CopperOS builds were warning-free; the CopperStart test assembly
  compiled with the existing transitive `NU1902` advisory.
- Isolated production Shell-task static qualification passed MC68000, MC68020,
  and MC68040 in
  `artifacts/shell-task-struct-forward-20260927-v1/qualification.json`.
  Each target reports 2,119 reachable methods and zero managed allocations,
  runtime features/helpers, external native targets, exception regions, or
  fatal machine-fault sites. The 68000 HUNK hash is
  `6ee73c55e10af833b6f8dc5985fee1fe22d3948f3daa920c6f7b1ff06ae1a489`, which
  matches the currently staged `filesystem/SYS/L/Shell-Seg` byte-for-byte.
- This confirms only the isolated compiler/static artifact. `guestExecution`
  and `shippingAdmission` remain false; MorphOS behavior, resident/PURE
  admission, and boot integration are still open.

## 2026-09-27 — MorphOS error-redirection spellings

- Corrected the Shell redirection subset to recognize the documented `*>` and
  `*>>` error-output operators rather than the non-MorphOS `2>`/`2>>` forms.
  `<`, `>`, and `>>` now also require a whitespace-delimited operator start;
  quoted or attached text remains literal. The parsed targets continue through
  the existing named `ShellRedirectionSpec` and `ShellRedirectionHandles`
  records, with byte layout confined to codecs.
- Added compile-only regression sources for both error-redirection modes, the
  rejected Unix-style spelling, attached `>`, and a script-engine error-stream
  case. Updated the goal and Shell README. The behavior follows the
  [Amiga ROM Kernel Reference Manual, DOS §15.1.1–15.1.2](https://developer.amigaos3.net/sites/default/files/downloads/2024-10/Amiga_ROM_Kernel_Reference_Manual_DOS.pdf);
  MorphOS runtime differential verification remains open. Tests and guest code
  have not been run.
- Release builds of `CopperOS.Commands.Tests` and `CopperOS.Shell.Dos` passed
  with zero warnings and errors. Isolated static Shell-task qualification
  passed MC68000/020/040 in
  `artifacts/shell-native-redirection-20260927-v1/qualification.json`;
  every target reports 2,119 reachable methods and zero managed allocations,
  runtime features/helpers, external targets, exception regions, or fatal
  machine-fault sites. The 702,664-byte 68000 HUNK
  (`6caa27542e650e895a1c647096a32e10119c74bf4fffbf4a2f7b1b0afc5dea3f`) now
  replaces `filesystem/SYS/L/Shell-Seg`; the previous staged HUNK is preserved
  in `artifacts/shell-native-readargs-template-conflicts-20260927-v1/`.
  `guestExecution` and `shippingAdmission` remain false.

## 2026-09-27 — Typed conditional command continuation

- Added bounded `&&` parsing for a whitespace-delimited conditional chain,
  ignoring quoted/star-escaped operators and stopping at semicolon comments.
  The runner compares the completed command's return level with the active
  `Failat` limit before it resumes the right-hand command. An external left
  command may suspend: the named deferred-command record stays in the Shell
  frame until DOS reports its final result.
- Replaced the 132-byte version-4 script frame with version 5 at 152 bytes.
  Deferred command text and the physical resume cursor are represented by
  `ShellScriptDeferredCommandState` / `ShellScriptTextSlice`; their serialized
  byte positions remain inside `ShellScriptFrameCodec`. The workspace lifetime
  requirement is documented. Compile-only parser, frame-codec, synchronous,
  and asynchronous regression sources were added; no tests or guest code were
  executed.
- The documented `&&` behavior follows the Amiga ROM Kernel Reference Manual,
  DOS §15.1.2. Exact MorphOS 3.20 behavior remains a differential gate; the
  [MorphOS Shell command inventory](https://library.morph.zone/Shell_Commands)
  remains the project command-set authority.
- Release builds of `CopperOS.Shell.Dos` and the Commands test assembly passed
  with zero warnings/errors. Isolated static Shell-task qualification passed
  MC68000/020/040 in
  `artifacts/shell-native-conditional-and-20260927-v2/qualification.json`:
  2,127 reachable methods and zero managed allocations, runtime features or
  helpers, external targets, exception regions, or fatal machine-fault sites
  per CPU. The source snapshot was unchanged across its managed build.
- Staged the 711,800-byte MC68000 HUNK at `filesystem/SYS/L/Shell-Seg`, SHA-256
  `7b21088689658d6d612fae23e46aaacff28d3cd8816902c1f646a205cef2ebd9`. The
  previous staged HUNK remains preserved byte-for-byte in
  `artifacts/shell-native-redirection-merge-20260927-v1/`.
  `guestExecution` and `shippingAdmission` remain false.

## 2026-09-27 — Typed compound-operator recognition boundary

- Generalized the bounded compound parser to return named operator identity
  (`ConditionalAnd`, `OutputConcatenation`, or `Pipe`) alongside its typed
  command slices. Quoted, star-escaped, attached, and commented pipe glyphs
  remain literal. Existing `&&` execution continues through the deferred
  command record.
- The script runner now rejects recognized `||` and `|` explicitly before
  command dispatch, rather than passing those operators to a command as
  argument text. This is an intermediate safety boundary, not pipeline
  implementation: `|` still requires the absent Queue-Handler plus concurrent
  reader/writer ownership, and `||` still requires one shared output stream.
- Added compile-only parser cases for `|`, `||`, quoting/escaping, and comment
  boundaries, plus runner cases proving neither side is dispatched. Release
  builds of `CopperOS.Shell.Dos` and `CopperOS.Commands.Tests` succeed with no
  warnings or errors. Isolated static Shell-task qualification passed for
  MC68000/020/040 in
  `artifacts/shell-native-compound-recognition-20260927-v1/qualification.json`:
  2,127 reachable methods per CPU and zero managed allocations, runtime
  features/helpers, external targets, exception regions, or fatal machine-fault
  sites. Its 712,580-byte 68000 HUNK has SHA-256
  `5605d2966c99b4809ce990f5c03f381adfff144a11afee648296b9422a02e69c` and is
  preserved in the receipt directory, not staged as the active Shell segment.
  Tests and guest code were not run; runtime differential and actual pipe/shared
  stream behavior remain outstanding. `guestExecution` and `shippingAdmission`
  remain false.

## 2026-09-27 — Begin typed Queue-Handler buffer foundation

- Added a bounded, single-reader/single-writer DOS queue-buffer core to
  CopperStart DOS as `DosQueueBufferState` and named `DosQueueBufferTransfer`
  structs. Buffer metadata stays in typed fields; no positional result-slot
  offsets are used. The core handles quantum publication, partial transfer,
  backpressure status, writer-close tail flush/EOF, and reader-close reporting
  so the caller can signal a still-open writer with `SIGBREAKF_CTRL_C`.
- This is only the data-buffer primitive. DOS `PIPE:` naming, buffer-count
  allocation policy, DOS packet retention, task parking/wakeup, and concurrent
  command scheduling are not implemented yet. MorphOS 3.20 differential
  behavior remains required before claiming compatibility; the
  [Amiga ROM Kernel Reference Manual, DOS §13.4](https://developer.amigaos3.net/sites/default/files/downloads/2024-10/Amiga_ROM_Kernel_Reference_Manual_DOS.pdf)
  is only the current baseline reference.
- Added compile-only core cases and a NativeRoot reachability root. Release
  builds of CopperStart `CopperStart.Exec.Tests`, `CopperOS.Commands.Tests`,
  and `CopperOS.Shell.Dos` succeeded; the CopperStart build retains its
  existing transitive `NU1902` advisory. No test suite or guest code was run.
- Static direct compilation of the buffer root passed for MC68000, MC68020,
  and MC68040 in
  `artifacts/dos-queue-buffer-native-20260927-v5/`. Each report is freestanding
  with 17 reachable methods and zero managed allocations, runtime helpers,
  exception regions, or fatal machine-fault sites. These are compiler/static
  artifacts only; they have not been executed or admitted as a Shell segment.
  The 3,972-byte MC68000 HUNK SHA-256 is
  `04e9691880cc2c66ce2b20848685e82ff367dff53f1a4f94cfc9efa0755b5805`.
  `guestExecution` and `shippingAdmission` remain false.

## 2026-09-27 — Defer MorphOS output-concatenation operands

- Extended the typed frame continuation with an `OutputConcatenation` kind.
  The Shell now resumes a simple `left || right` pair after synchronous or
  external-child completion, and unlike `&&`, it schedules the right side
  even when the left side reaches the active `Failat` threshold. Both sides
  retain the same inherited output handle in the no-redirection case.
- Added compile-only engine cases for ordered output, a failing left command,
  and a pending external left command, plus frame round-tripping for both
  deferred operator kinds. The Amiga ROM Kernel Reference Manual defines `||`
  as joining command output into a common stream and demonstrates that stream
  feeding a pager; it is only an AmigaOS baseline, not evidence of MorphOS
  parity. This implementation does not yet construct that composable stream
  for a subsequent `|` or implement whole-expression redirection. The
  single-pipe operator remains explicitly rejected until Queue-Handler
  scheduling is integrated. See the [Amiga ROM Kernel Reference Manual, DOS §15.1.2](https://developer.amigaos3.net/sites/default/files/downloads/2024-10/Amiga_ROM_Kernel_Reference_Manual_DOS.pdf)
  and [MorphOS Shell command inventory](https://library.morph.zone/Shell_Commands);
  exact MorphOS 3.20 operator behavior remains a differential gate.
- The Commands tests, Shell DOS, and isolated NativeRoot managed inputs built
  without warnings or errors. The isolated shell-task static qualification
  passed for MC68000/020/040 in
  `artifacts/shell-native-output-concatenation-20260927-v3/qualification.json`:
  2,134 reachable methods per CPU and zero managed allocation sites, runtime
  features/helpers, external targets, exception regions, or fatal machine
  faults. The 714,068-byte 68000 HUNK SHA-256 is
  `de85e4b487069270716b679f5cb4ab50c09cadb408802f70712de3b33bf5d07f`.
  Static artifacts were not run or staged; tests and guest code were not run.
  `guestExecution` and `shippingAdmission` remain false.

## 2026-09-27 — Add typed pipeline planning boundary

- Added `ShellScriptPipelineParser`, which turns an isolated, whitespace-
  delimited `|` chain into a caller-buffered `ShellScriptPipelinePlan` of
  named `ShellScriptPipelineSegment` structs. Segment command text remains a
  bounded `ShellScriptTextSlice`; parser state and API results do not expose
  field offsets. The private segment codec owns the fixed guest-memory layout.
- The planner rejects mixed pipeline/AND-OR expressions and insufficient
  segment capacity. This is a parsing foundation only: the Shell runner still
  rejects `|`, and no command scheduling, pipe-handle creation, or
  Queue-Handler packet lifecycle has been added. MorphOS 3.20 syntax and
  operator semantics remain unverified; the Amiga DOS manual is only a
  baseline reference.
- Added compile-only parser regression sources for multi-command segments,
  quoted pipes, non-pipeline compounds, mixed operators, and capacity bounds.
  `CopperOS.Commands.Tests` built in Release with zero warnings/errors; no
  tests or guest code were run.
- Adopted the struct-first state rule for ongoing work: retained parser,
  pipeline, process, and queue state uses named fixed-width records/enums;
  byte offsets stay inside the codec that owns the serialized layout.
  `guestExecution` and `shippingAdmission` remain false.

## 2026-09-27 — Match MorphOS Echo numeric keywords

- Corrected the DOS `ReadArgs` template used by `Echo` to
  `MESSAGE/M,NOLINE/S,FIRST/K/N,LEN/K/N,TO/K`, matching the MorphOS 3.20
  command reference. `FIRST` and `LEN` are now keyword-qualified numeric
  options, not bare positional `/N` template slots. The named
  `EchoReadArgsResultRecord` and its codec continue to own the typed result
  handoff; only the template length/buffer bound and writer changed.
- Updated the compile-only Echo contract checks and test adapter. The Release
  Commands test project build passed with zero warnings/errors; tests were not
  run. Isolated static Shell-task qualification passed for MC68000/020/040 in
  `artifacts/shell-native-echo-keyword-readargs-20260927-v1/qualification.json`:
  2,134 reachable methods per target and zero managed allocations, runtime
  features/helpers, external targets, exception regions, or fatal machine
  faults. The 714,176-byte MC68000 HUNK SHA-256 is
  `353c4a080c2db3e769f3b4b37a70546b985bf6cad0cfb17437d3b95d9498eca1`.
  This is static compiler evidence only; `guestExecution` and
  `shippingAdmission` remain false. Source: [MorphOS Shell Commands/Echo](https://library.morph.zone/Shell_Commands/Echo).

## 2026-09-27 — Match MorphOS Stack template name

- Corrected the `Stack` ReadArgs template from `STACK/N` to MorphOS's
  documented `SIZE/N`. The `StackReadArgsResultRecord` remains unchanged; the
  template writer, exact byte length, and test adapter now agree on the public
  name. The command continues to report or update the current CLI's default
  stack size, with state carried through the existing DOS boundary.
- Release compilation of `CopperOS.Commands.Tests` passed with zero
  warnings/errors; tests were not run. Isolated static Shell-task
  qualification passed for MC68000/020/040 in
  `artifacts/shell-native-stack-readargs-template-20260927-v1/qualification.json`:
  2,134 reachable methods per target and zero managed allocations, runtime
  features/helpers, external targets, exception regions, or fatal machine
  faults. The 714,156-byte MC68000 HUNK SHA-256 is
  `2000f579e32371515add2ab79b7b3bb54593697278460fdde77b53873bf4917a`.
  This static evidence is not execution or shipping admission. MorphOS source:
  [Shell Commands/Stack](https://library.morph.zone/Shell_Commands/Stack).

## 2026-09-27 — Match MorphOS FailAt argument contract

- Corrected the template from `RCLIM/A/N` to the documented optional
  `RCLIM/N`, and reject zero or negative parsed values before writing the
  CLI-owned failure limit. With no argument, `Failat` now displays the current
  value using the shared allocation-free unsigned decimal writer; its progress
  state is a named struct, and the `ReadArgs` result remains behind the typed
  pointer-result and signed-LONG record codecs rather than command-local
  offsets.
- MorphOS documents the optional `RCLIM/N` template and positive-value rule,
  but its page does not specify omitted-argument output. Reporting the current
  value follows the AmigaOS manual baseline and remains a MorphOS differential
  check, not a verified MorphOS behavior claim. See [MorphOS
  FailAt](https://library.morph.zone/Shell_Commands/Failat) and the [AmigaOS
  command reference](https://wiki.amigaos.net/wiki/AmigaOS_Manual%3A_AmigaDOS_Command_Reference).
- Updated the test adapter and compile-only cases for optional display,
  nonpositive inputs, and the corrected template. Release compilation of
  `CopperOS.Commands.Tests` passed with zero warnings/errors; tests were not
  run. Isolated static Shell-task qualification passed for MC68000/020/040 in
  `artifacts/shell-native-failat-optional-readargs-20260927-v1/qualification.json`:
  2,134 reachable methods per target and zero managed allocations, runtime
  features/helpers, external targets, exception regions, or fatal machine
  faults. The 714,436-byte MC68000 HUNK SHA-256 is
  `a16e23df2435bb6d5102ef58ac7596b637d9e56d662b345c0552226aac9d7f21`.
  This is static compiler evidence only; `guestExecution` and
  `shippingAdmission` remain false.

## 2026-09-27 — Make `If EXISTS` recognize directories

- Replaced the Shell's `Open(OldFile)` existence probe with DOS shared
  `Lock`/`UnLock` in both portable and native platform evaluators. MorphOS
  documents `EXISTS` for files or directories; a file handle probe is too
  narrow for that contract. A failed lookup is now consumed as a false
  condition and its expected `IoErr` is cleared before branch state is
  recorded.
- `NOREQ/S` still parses and travels through the typed condition call, but
  requester suppression is not claimed as complete: the current Shell/DOS
  requester boundary has no explicit per-operation suppression capability,
  so that behavior remains an implementation and MorphOS differential gate.
- The Release Commands project compiled with zero warnings/errors; no tests or
  guest code were run. Isolated Shell-task static qualification passed for
  MC68000/020/040 in
  `artifacts/shell-native-if-exists-lock-20260927-v1/qualification.json`:
  2,134 reachable methods per target and zero managed allocations, runtime
  features/helpers, external targets, exception regions, or fatal machine
  faults. The 714,452-byte MC68000 HUNK SHA-256 is
  `823e02f2724860d14a04813be4c9f7c32592527ac9ec90e982d49a6e7d24fc98`.
  This is static evidence only; `guestExecution` and `shippingAdmission`
  remain false. Source: [MorphOS Shell Commands/If](https://library.morph.zone/Shell_Commands/If).

## 2026-09-27 — Create the internal-command completion ledger

- Added `Goals/MORPHOS_SHELL_INTERNAL_COMMANDS_LEDGER.md` with source ownership
  and explicitly incomplete status for every command identity registered by
  `ShellInternalCommandResolver`. The ledger does not equate source presence,
  a clean build, or static compilation with MorphOS conformance.
- Reconciled the goal's “32” acceptance count with the current enum: there are
  31 callable identities plus the `Unknown` sentinel. `Execute` has its own
  command wrapper and script-engine path, but is not registered as an
  internal identity in the current goal. This discrepancy is recorded rather
  than silently adding an identity or changing command ownership.
- Carried forward concrete remaining gates, including `If NOREQ`, Path volume
  requester policy, the `If VAL` documentation discrepancy, MorphOS-only
  behavior evidence, pipe scheduling, and runtime/boot qualification. This is
  an inventory artifact only; no tests or guest code were run.

## 2026-09-27 — Prefer named state for Execute's ReadArgs template

- Reworked `Execute`'s `FILE/A` template writer to hold its destination and
  sequential position in a named struct, following the existing
  `ReadArgs` template-writer pattern. The emitted `FILE/A\0` bytes and parser
  behavior are unchanged; there are no literal per-byte template offsets in
  the command wrapper.
- The Commands Release project compiled with zero warnings or errors. No tests
  or guest code were run.

## 2026-09-27 — Honor If NOREQ for the EXISTS probe

- The MorphOS `If` contract says `NOREQ` suppresses error requesters, while
  `EXISTS` probes files or directories. The DOS platform adapters now save the
  current Process `WindowPointer` in a named stack-local scope, set it to the
  documented `-1` suppression sentinel around `Lock`/`UnLock`, and restore the
  prior pointer before continuing the script condition. This is process-scoped
  and does not claim to suppress requesters created by a file-system task.
- Both `CopperOS.Commands.Tests` and `CopperOS.Shell.Dos` compiled in Release
  with zero warnings or errors; tests and guest code were not run. Isolated
  three-target static Shell-task qualification passed in
  `artifacts/shell-native-if-noreq-scope-20260927-v1/qualification.json`:
  2,138 reachable methods for MC68000/020/040 and zero managed allocations,
  runtime features/helpers, external targets, exception regions, or fatal
  machine-fault sites. `guestExecution` and `shippingAdmission` remain false.
- `Path QUIET` still has no path-volume requester policy, and MorphOS runtime
  differential evidence for both switches remains open. Sources: MorphOS
  [If](https://library.morph.zone/Shell_Commands/If) and AmigaOS
  [AmigaDOS data structures](https://wiki.amigaos.net/wiki/AmigaDOS_Data_Structures).

## 2026-09-27 — Apply `Path QUIET` at the DOS Process boundary

- `Path` now resolves requested directories into shared DOS locks under the
  caller Process's temporary requester-suppression scope when `QUIET` is set.
  It stages a replacement `PathLock` chain and its owned-lock record before
  swapping the Process and CLI pointers, so failed resolution/allocation leaves
  the prior command path intact. `ADD`, `RESET`, and `REMOVE` update the live
  DOS path searched by command lookup; `SHOW` writes names from that live chain
  rather than relying on Shell-only text copies.
- The implementation keeps Process state in the existing typed record codecs,
  uses the named `DosRequesterScope`, and traverses path-list text/state via
  named cursor/workspace structs and `DosPathLockCodec`. It adds no managed
  collections, exceptions, or runtime dependencies.
- Release compilation passed for `CopperStart.Dos`, `CopperOS.Shell.Dos`, and
  `CopperOS.Commands.Tests` with zero warnings/errors; tests and guest code were
  not run. Isolated MC68000/020/040 static Shell-task qualification passed in
  `artifacts/shell-native-pathlock-20260927-v1/qualification.json`: 2,155
  reachable methods per target, zero managed allocations/runtime features or
  helpers/external targets/exception regions/fatal machine-fault sites. The
  725,212-byte MC68000 HUNK SHA-256 is
  `0278269dc50eba619a8028fe22eaa5c3f9d5a94c36f906245efe2d7d0ea9e308`.
  `guestExecution` and `shippingAdmission` remain false.
- A compile-only build of `CopperStart.Exec.Tests` also succeeded; its one
  `NU1902` advisory names `Microsoft.Build.Tasks.Git` 10.0.300 through the
  referenced Copper68k project. No tests were executed.
- The requester scope controls the caller Process `WindowPointer`; it cannot
  guarantee suppression of requesters generated internally by a file-system
  task. MorphOS runtime differential checks remain open, including missing or
  unmounted volume behavior. Source: [MorphOS Shell Commands/Path](https://library.morph.zone/Shell_Commands/Path).

## 2026-09-27 — Align Path coverage with per-Process DOS ownership

- The existing Shell-state tests only had CLI text records, which no longer
  represented the production path owner after the `PathLock` integration.
  Updated those fixtures to create DOS Process/CLI mappings and copy the live
  path snapshot for a child. Added a source-level case that verifies a failed
  `QUIET ADD` leaves the original Process path pointer and lock intact.
- This test project was compiled only; no tests were run. The compile-only
  build succeeded with the same single `NU1902` advisory for
  `Microsoft.Build.Tasks.Git` 10.0.300 through the referenced Copper68k project.

## 2026-09-27 — Require Path entries to be directories

- `Path` now examines every resolved lock into a typed `FileInfoBlock` and
  rejects files with `DOS.Error.ObjectWrongType` before constructing the
  candidate `PathLock` chain. A failed examination or non-directory entry
  releases the staged lock and leaves the live Process path unchanged.
- Kept the ABI boundary expressed with named structures and codecs: the
  mutation workspace carries its `FileInfoBlock` pointer, `FileInfoBlock` is
  validated via `DosFileInfoBlockCodec`, and the Process/path nodes continue
  through their typed record codecs. No raw guest-structure field offsets were
  added for this behavior.
- `CopperStart.Dos` compiled in Release with zero warnings/errors.
  `CopperStart.Exec.Tests` compiled in Release with zero errors and one
  existing `NU1902` advisory for `Microsoft.Build.Tasks.Git` 10.0.300; tests
  and guest code were not run. Static isolated Shell-task qualification passed
  for MC68000/020/040 in
  `artifacts/shell-native-path-directory-20260927-v1/qualification.json`:
  2,156 reachable methods per target and zero managed allocation sites,
  runtime features/helpers, external targets, exception regions, or fatal
  machine-fault sites. `guestExecution` and `shippingAdmission` remain false.
- MorphOS runtime behavior, including `QUIET` requester policy for mounted and
  unavailable volumes, remains unqualified. Source: [MorphOS Shell Commands/Path](https://library.morph.zone/Shell_Commands/Path).

## 2026-09-27 — Accept chained ReadArgs keyword aliases

- MorphOS documents `=`-separated equivalent names, including a three-name
  switch such as `Fuh=Bar=Chicken/S`. CopperStart previously accepted only one
  alias and rejected any later `=`. `ReadArgs` now validates and matches a
  bounded alias-name list, case-insensitively, while rejecting empty entries
  such as `Fuh==Bar/S` and `Fuh=Bar=/S`.
- The alias list is traversed through named `ReadArgsNameListCursor` and
  `ReadArgsNameSpan` structs; it does not add guest-layout offsets or managed
  state. Added compile-only fixtures covering the primary name, both aliases,
  case folding, and malformed empty aliases.
- `CopperStart.Dos` compiled in Release with zero warnings/errors. The
  `CopperStart.Exec.Tests` project compiled with zero errors and the existing
  `NU1902` advisory for `Microsoft.Build.Tasks.Git` 10.0.300. Tests and guest
  code were not run. Isolated Shell-task qualification passed for MC68000,
  MC68020, and MC68040 in
  `artifacts/shell-native-readargs-alias-chain-20260927-v1/qualification.json`:
  2,157 reachable methods per target and zero managed allocation sites,
  runtime features/helpers, external targets, exception regions, or fatal
  machine-fault sites. `guestExecution` and `shippingAdmission` remain false.
- The MorphOS FAQ is the direct authority for multi-name aliases; wider
  MorphOS 3.20 ReadArgs behavior still requires differential/runtime evidence.
  Source: [MorphOS FAQ](https://morphos-team.net/faq).
