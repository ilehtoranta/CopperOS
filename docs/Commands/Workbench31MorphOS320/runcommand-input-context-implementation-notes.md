# API14.4: RunCommand input context and owner boundaries

This began as a source audit and a proposed implementation split. The portable
checkpoint below records the subsequently implemented, bounded input layer.
It does not mark
RunCommand, normal NULL-D3 ReadArgs, command Exit, or command residency qualified.
The authoritative launch requirements and original observations remain in
[native-command-launch-contract.md](native-command-launch-contract.md). In that
document the original stream-acquisition prerequisite R04 passes; original
RunCommand/input-restoration observations are still separate work. The existing
44 original-derived ReadArgs cases supply an RDA_Source and do not prove this
launch boundary.

## Authority and bounded behavior

The privately held NDK DOS.DOC RunCommand entry (member offset 134074) requires
the current Process/CLI context, a NUL-terminated argument string and an LF terminator
when ReadArgs consumes it. It describes temporary input-filehandle buffering
and GetArgStr state, restored on return. The GetArgStr entry at 73510 identifies
the argument pointer also supplied in A0. These requirements do not authorize
inventing an LF, replacing Input with a new stream, or treating Process Exit as
an ordinary return. DOS.DOC member SHA256 is
`2e22d1d1b7c00fac5757eead0ef4d49a01c7666f0db5850735193c9597a7cee2`.
No original source or binary is included here.
The entry does not state that `pr_CLI` must be nonzero. The implementation
requires a mapped real Process and established owned Input, but does not
invent a non-null CLI prerequisite.

The separately tested SDK `DosRunCommandCallbacks.ExecuteOnStack` bridge covers
normal RTS and an Exec StackSwap vector fixture. STARTUP.ASM lines 264-267 allow
the complete command to change every register except SP. A DOS library callback
must still preserve its outer library ABI. The bridge's compiler-owner receipt
is `obj/compiler-run-command-bridge/65fa8592a4744984b2e973f5d322b0e9/receipt.json`
in CopperOS, SHA256
`dc51a073634ab8b391c7eed3b0c369f30807be62e65e9ec3407a084dac241b94`.
It does not cover the production DOS callback or input/lifecycle integration.

## Initial audited state and ownership proposal

Source paths in this section are relative to `D:/Koodit/GIT/CopperStart`.

| Owner | Present state and required treatment |
| --- | --- |
| `src/CopperStart.Dos/DosRunCommandCore.cs:7` | The continuation is currently sixteen bytes: token at 0 and StackSwapStruct at 4. Preserve these offsets; append invocation state and move the owned stack after the enlarged record and any aligned argument storage. |
| `DosRunCommandCore.Begin`, line 56 | Validates image/size/mapping and owns a stack, but captures no Process, handle, input buffer or argument pointer. Complete validation and allocation before publishing temporary input state. |
| `DosRunCommandCore.Resume`, line 126 | Token equals the record address; owner/generation checks do not reject address reuse within a generation. There is no current-task or nested-top check. Use a distinct continuation nonce and validated per-task nesting, not address identity alone. |
| `src/CopperStart.Dos/DosProcessCore.cs:36` | GetArgStr/SetArgStr use public `Process.Arguments`. Save and exchange this field on the captured Process. The private process record's `Arguments`/`ArgumentsCapacity` own process-creation storage and must not be replaced or freed. |
| `src/CopperStart.Dos/DosObjectRecords.cs:526` | Capture the private process-record identity, its Task, selected Input, and process-level Pushback. Other private process fields, including LastError, have different ownership. Do not restore the whole process record after a command. |
| `src/CopperStart.Dos/DosObjectRecords.cs:461` | Save the handle's Buffer, OriginalBuffer, BufferSize, BufferIndex, BufferCount, BufferStartLow/High, BufferMode, BufferFlags, buffer ownership/output flag bits, Pushback and LastRead. Keep the same borrowed Input BPTR. Provider, file position/length and unrelated flags are not a disposable invocation snapshot. |
| `src/CopperStart.Dos/DosCore.cs:1663` | FGetC consumes handle pushback and normal buffering; every non-console buffered byte advances PositionLow/High. Merely installing argument bytes here advances the caller's file position incorrectly. Argument-buffer reads need an explicit accounting path that does not advance the underlying stream. |
| `DosCore.ReadInputCharacter` | Normal input first consumes process-level Pushback, then calls FGetC on Input. Save/clear both pushback layers at entry; otherwise caller lookahead can precede the new command's arguments. Keep ReadArgs on this existing DOS route. |
| `DosCore.SetVBuf` and `ReleaseHandleBuffer` | SetVBuf can release the old owned buffer. Do not implement temporary attachment by calling SetVBuf and later restoring a freed pointer. The continuation owns temporary storage; the saved caller buffer remains allocated and untouched. |

`DosCore` is not partial. Its process lookup, handle resolution and buffer
validation helpers are private. A new `DosCore.RunCommandInput.cs` therefore
needs an agreed one-line partial declaration change and narrow hooks in the
existing owner; copying validation or searching process lists independently is
not a clean file-ownership split. Current MemoryInput handles also have special
validation and position semantics: a borrowed buffer cannot simply be installed
under the existing MemoryInput ownership flag.

## Suggested implementation order and file split

1. **Production native bridge only.** Change the RunCommand case in
   `DosNativeEntrypoints.InvokeGuestCallback` to use ExecuteOnStack. Add a new
   `tests/CopperStart.Exec.Tests/DosNativeRunCommandCallbackTests.cs` compiling
   this actual production method. Existing compiler/Amiga/Copper68k references
   and InternalsVisibleTo suffice. Keep all other callback kinds and both
   dispatch loops unchanged. First retain an actual old-path native failure,
   then test the new call across 68000/020/040 and disabled/default peephole
   modes. No global fixture exports, compiler edits or SDK edits are needed.

2. **Portable input lease and normal-return tests.** Extend the private
   RunCommand continuation/codec in `DosRunCommandCore.cs`; implement input
   operations in new `DosCore.RunCommandInput.cs`. Coordinate the partial
   declaration and only the necessary buffered-read/lifecycle hooks in
   `DosCore.cs` after the parser owner finishes. Do not edit the parser or
   `DosCliCore.cs` for this slice. Add new `DosRunCommandInputTests.cs` using
   the existing guest-memory/platform fixtures, real owned Process/CLI/input
   records, and the public normal-input ReadArgs path with D3 zero.

3. **Callback-start rollback and nested host dispatch.** Both
   `DosNativeEntrypoints.DispatchStored` and
   `DosNativeDirectBoundary.CompleteGateway` currently break on a false
   InvokeGuestCallback result without Resume(false). Correct both in a
   separate assigned patch. In MedPlayer, change only
   `CopperMod.Amiga.Emulator/CopperStart/Dos/DosServices.cs` and focused host
   callback tests: replace one pending callback per Task with a validated LIFO
   of invocation records. Preserve outer callbacks across a nested launch.

4. **Reset, retirement and explicit abort.** Add coordinated hooks before
   generic RunCommand-object release and before process stream/text release.
   Prepared callbacks which never started can roll back; an executing command
   requires a proven return or an outer-owner quiescence boundary before its
   stack can be freed. Keep original Exit/nonlocal-unwind implementation and
   reference observations separate. Do not declare the lease generally safe
   while these lifetime paths are missing.

The next bridge patch can proceed independently of steps 2-4. Step 2 needs a
short shared ownership window with the DosCore parser owner; steps 3-4 must be
present before admitting launch-context cleanup as complete.

## Proposed lease transaction

Use invocation-owned guest storage, not static mutable fields or a host-only
argument registry. Extend the record with the captured Task/public Process,
private process-record identity, input BPTR/validated handle identity, parent
continuation/token, previous public argument pointer and process pushback,
saved buffered-input fields, temporary buffer capacity/index/count, and an
explicit prepared/entered/returned-or-quiesced lifetime state. A bounded walk
of the existing typed DOS-object list can establish each task's top context;
no public Process or FileHandle layout needs to change.

Keep the callback's A0 and temporary public GetArgStr equal to the supplied
argument pointer. If copied bytes are used for buffered input, copy exactly
the supplied length; do not append LF. Check all additions and four-byte
rounding before allocation. Existing ordinary-buffer validation requires at
least 208 bytes, so a directly installed buffer must meet that capacity even
for short input. An interior slice of the continuation allocation must never
be marked independently owned by the handle.

Capture the underlying buffer and both pushbacks before publication. Consume
argument bytes through the shared buffered-input owner with explicit
argument-buffer accounting, not by changing the private ReadArgs parser or
undoing all later file-position changes. Save/restore only fields the lease
temporarily owns. A command may perform real file I/O; restoring an entire
handle would discard those effects. The no-underlying-read test cases must
prove that reading arguments causes neither provider reads nor cursor motion.

Publish only after image, Process/CLI, input, mapping, buffer, parent/token and
allocation validation succeed. Capture the intended failure before cleanup.
On a known never-started failure or normal return: validate DOS generation,
current Task, captured identities, unique token and top-of-stack relationship;
restore the caller's buffer/pushbacks/public argument pointer; unlink the
context; then free its storage exactly once. On successful return preserve the
command's D0 and current IoErr rather than restoring the parent's old IoErr.
`DosObjectCore.Free` itself does not set IoErr.

Token allocation needs explicit wrap/reset rules. DOS already has packet and
internal-segment token counters; silently borrowing one is not proof that old
RunCommand tokens cannot identify a newly allocated record. Extend an agreed
nonce facility or allocate a checked nonce and verify all live contexts,
including reuse after reset. Reject wrong-task, duplicate, stale and
out-of-order resumes without modifying any invocation.

## Existing teardown and host hazards

- `DosCore.TryReset` currently calls `DosObjectCore.Reset` before releasing
  handles/processes. With an attached buffer this would free its backing
  continuation first. Check active contexts before destructive work and detach
  safely releasable contexts before generic object cleanup.
- `TryReleaseProcessStateAfterRetirementBarrier` closes streams and frees
  process argument storage before its continuation cleanup. That cleanup
  currently covers packet/list-wait/device-proc state, not RunCommand.
- `DosProcessLifecycleCore.Terminate` releases process state before Exec task
  removal. Provider-task cancellation is not proof that a command has left its
  stack. `DosExitCore` also releases process state; normal callback RTS tests do
  not authorize freeing an active RunCommand stack through this route.
- Host `DosServices.Reset` and `ReleaseTask` erase pending callbacks before the
  portable cleanup result is known. `ContinueCallback` removes the record
  before its Ready check. Preserve ownership until validation and safe cleanup
  succeed; retain it when teardown must defer.
- Host callback start rejects a second callback for the same Task, or a missing
  callback facility, by falling through without aborting the new continuation.
  Invalid StackSwap bounds already call Resume(false), but a missing/unmapped
  descriptor does not. Preflight every start prerequisite and use one rollback
  path for all known never-started failures.
- Host callbacks restore only SP. A nested RunCommand host record must retain
  original SP, applicable task stack bounds, callback kind, token/generation
  and expected return boundary, and validate the top before popping it. The
  void StartGuestSubroutine callback has no acceptance result; define failure
  before execution starts, not a catch-all rollback after arbitrary guest code
  may have run.

## Finite follow-on tests and open observations

| Test group | Required distinction |
| --- | --- |
| Normal parser | LF-only and nonempty LF-terminated arguments through Input and NULL-D3 ReadArgs; read parsed results before FreeArgs; Input BPTR unchanged; GetArgStr equals A0. |
| Caller buffering | Actually prefetch an unread caller buffer, seed distinct handle/process pushbacks, parse command arguments, then consume the exact original unread data/pushbacks. Check original allocation identity, file position and zero provider reads while consuming arguments. |
| Nesting | Read part of outer arguments, launch inner command on the same Process, resume outer at its previous argument index, and restore caller only after outer return. Check D0 and current IoErr independently. |
| Interleaving | Two real Process identities with distinct input handles and arguments; interleave callbacks without resetting state between checks. |
| Preparation failures | Inner allocation failure, unmapped/unaligned allocation, argument-size/rounding overflow and invalid image/Process/input: no callback entry and no mutation/free of parent storage. |
| Start failures | Missing launcher, null entry, missing/unmapped descriptor, invalid stack bounds and known rejected launch: one rollback, original SP/bounds and outer callback retained. |
| Resume validation | Wrong task, non-top token, duplicate completion and allocator address reuse with an old token: no other context modified or freed. |
| Lifetime | Never-started reset restores buffers before freeing objects; active reset/retirement defers; explicitly quiesced abort unwinds inner first; each buffer, stack and owned stream released only by its owner. |

The buffered argument-exhaustion/refill boundary, raw Read/Seek/Flush,
child SelectInput/SetVBuf, memory-input handles, same handle shared between
tasks, command SetArgStr, and Exit still need source/primary observations where
their effects are not established. Do not invent full-handle restoration or
claim these paths passed from the bounded normal-input tests. The current
`CreateCommandInputHandle` helper changes stream identity and appends LF; it
cannot be substituted to make those tests pass.

## Initial audited source identities

These hashes bind read-only observations, not compiled qualification. DosCore
was concurrently being repaired by the ReadArgs owner; re-hash it before
editing or building. Paths below are relative to the named repository.

| Repository / path | SHA256 |
| --- | --- |
| CopperStart `src/CopperStart.Dos/DosRunCommandCore.cs` | `72738baa92e4ea510c247711ccc535c67cf8878b370b0ee4ffbaa32192297671` |
| CopperStart `src/CopperStart.Dos/DosCore.cs` | `f908d2ff10b0b0f124b9bcfb1f22cf4befbce6da58c82c625ac2e98f27d72a3d` |
| CopperStart `src/CopperStart.Dos/DosObjectRecords.cs` | `76501ce0803c6976e1e24ad87e57d1d77e3786333eeafec95ded36eba8372d86` |
| CopperStart `src/CopperStart.Dos/DosProcessCore.cs` | `36fdfc29f790fad8611fe58490d5fc80f6e570cfa41ae3665db7f7173b87f420` |
| CopperStart `src/CopperStart.Dos/DosObjectCore.cs` | `b4adbae4e8137b41710073334388cda20128c77abe87f3a502ba52ba8c07e76c` |
| CopperStart `src/CopperStart.Dos/DosProcessLifecycleCore.cs` | `e814f4a77cb8e6a0cc10b75f211c0a78f1718ba8198edea1ce9464104f5108a1` |
| CopperStart `src/CopperStart.Dos/DosExitCore.cs` | `b4ebebc1a33a4467de5b64162996aec2d0e4126eca1c0f5d56a2b9c7256f967d` |
| CopperStart `src/CopperStart.Dos/DosNativeEntrypoints.cs` | `328162ae83a71cad9d62369fbbcb8998b59e90a3c9bd2eb12886b941a924bbf7` |
| CopperStart `src/CopperStart.Dos/DosNativeDirectBoundary.cs` | `0645a14d0204dd7bb8263c7ea29d494ed8e5736d719070e109e92206a5701cc9` |
| MedPlayer `CopperMod.Amiga.Emulator/CopperStart/Dos/DosServices.cs` | `c2902d7881d684b39e03b5784c214f9f271b67836559946c329d37c1368c3d7d` |
| CopperStart `tests/CopperStart.Exec.Tests/DosRunCommandCoreTests.cs` | `7967325d3349962f9cf1b72daf482994206b144ef1aa1924b5bb7d2ca59e86c0` |
| CopperStart `tests/CopperStart.Exec.Tests/DosBufferedStreamTests.cs` | `213a97850d2275e214276faba457ffbd8b904a444aeeb4c3e8aaf8150e3df1ec` |
| CopperStart `tests/CopperStart.Exec.Tests/CopperStart.Exec.Tests.csproj` | `14cc74d07809f050abaee3ebb379e4cbae36d63ff409151b0e593476d5002fb3` |

## Implemented portable checkpoint

The implementation chose an invocation-owned input layer in the shared FGetC
owner rather than the initially considered replacement of the handle's private
buffer. `FRead` and `FGets` call FGetC; normal-source `ReadItem`, ReadArgs and
ReadArgs `/F` use `ReadInputCharacter`, which reaches the same FGetC. `UnGetC`
continues to use the handle's existing one-character Pushback/LastRead fields.
The layer is not a private parser or a replacement for DOS ReadArgs.

The NDK 3.1 `DOS/STDIO.H` macros use these buffered DOS APIs for ReadChar,
ReadChars and ReadLn. Member extent 21077, size 838, SHA256
`793e253bb656485d13dc53683ce41ab30bdd1640efc2229bea6ac04daf4ddb31`.
`DOS/DOSEXTENS.H`, extent 21052, size 16933, SHA256
`d65038a5297ffa91e3ab1ba063dca102580de63b14c071a1158d4e9ecdfb2f50`,
publishes the FileHandle layout but does not specify a buffer-pointer/index
interpretation sufficient to invent mirroring here. The existing provider
FileHandle prefix and private handle buffer are already separate: ordinary
FGetC/SetVBuf update only the private extension, and existing tests preserve
provider prefix values. This patch leaves the public prefix unchanged. Direct
observation or modification of `fh_Buf/fh_Pos/fh_End` is **not qualified** by
these tests and remains an explicit existing compatibility gap.

Production changes are restricted to `DosRunCommandCore.cs`, the new
`DosCore.RunCommandInput.cs`, the private records/codecs in
`DosObjectRecords.cs`, and narrow partial/FGetC/release/retirement hooks in
`DosCore.cs`. The f908 parser algorithms, `DosCliCore.cs`, public ReadArgs and
StrToLong are unchanged. No compiler, SDK, emulator, project, MedPlayer host
callback registry, or native dispatch-loop source changes belong to this
checkpoint.

The continuation is now 64 bytes. Its first sixteen bytes are unchanged;
StackSwapStruct remains at offset 4. The appended fields are Task (16), private
process record (20), borrowed Input BPTR (24), parent token (28), previous
GetArgStr (32), previous process Pushback (36), previous handle Pushback (40)
and LastRead (44), argument buffer (48), length (52), position (56), and an
Input-released marker (60). Exactly the supplied byte count is copied into
four-byte-aligned storage immediately after the record; the owned stack
follows its rounded capacity. No LF is synthesized. A0 and temporary GetArgStr
remain the caller's supplied pointer, not the copy.

At publication only public `Process.Arguments`, private process Pushback and
private handle Pushback/LastRead temporarily change. Argument consumption
changes the invocation position and handle LastRead. It neither advances the
underlying file cursor nor reads the provider. Caller buffers, their ownership
bits, indices, counts and allocation addresses remain untouched. Normal return
or a proven never-started rollback restores these temporary fields before
unlinking and freeing the continuation. It does not restore a stale whole
process/handle snapshot or old IoErr. A successful return preserves command D0
and current IoErr; an explicit `Resume(..., false, ...)` restores first, then
returns -1 with ObjectWrongType. That false argument is an outer-owner proof
of non-entry, not permission to free an executing command stack.

Existing raw Read/Seek/Flush/SetVBuf behavior is preserved. A child SetVBuf may
replace/free its real underlying buffer without touching the continuation;
the return does not restore that freed buffer. A handle-release hook marks
all matching leases before freeing the wrapper. Saved handle lookahead is
then never written into a wrapper later allocated at the same address.
Process lookahead is restored only while that captured handle remains live
and selected; child selection and its own lookahead are preserved otherwise.
These are owner-safety cases, not original RunCommand observations. Refilling
after argument exhaustion, public buffering observations, cross-task sharing
of one handle, general child input reconfiguration and Exit remain open.

`NextRunCommandToken` is appended at private DOS-state offset 132; it stores
the highest issued token. State size changes 132 to 136 and StateVersion 23
to 24. The private legacy Shell context grows 136 to 140, with ExecBase moving
132 to 136; public Amiga Process/FileHandle/library vector layouts do not move.
Tokens increase from 1, persist across successful Reset/Initialize, and never
wrap. Exhaustion returns NoFreeStore before allocating or publishing another
context. An initialized older private version is rejected by Initialize
without clearing its owner data; this is not a live-version migration. All
consumers of these private layouts need a coherent rebuild/new state. Normal
DOS generation behavior is unchanged; stale tokens cannot alias an allocation
reused in the same generation or after a successful reset of this state.
Destroying/replacing the entire DOS-state owner is a different lifetime and
does not make its old continuation tokens valid inputs to a new library.

A bounded validated walk of the existing typed object list selects the top
context for the current Task and validates its parent chain. Resume declines
wrong-task, non-top, duplicate, stale, orphaned or malformed contexts before
any restoration/free. The exchanged StackSwap descriptor pointers are not
treated as immutable allocation bounds while a command is executing. The
argument allocation bounds remain independently checked.

Portable Reset, Initialize and process retirement defer before provider reset,
provider task cancellation, object cleanup or stream release while a callback
is published. The portable owner cannot yet distinguish a returned request
that has not been started from one already executing; it deliberately assumes
the latter until Resume supplies the proof. The host owner must still retain
callbacks across deferred reset/retirement, admit a validated existing
callback's completion while reset is pending, preserve nested LIFO state and
roll back every proven pre-entry rejection. Native dispatch-loop false-result
rollback remains a following slice. Exit/nonlocal unwind and final host task
quiescence are not implemented by these portable guards.

## Portable checkpoint test evidence

Evidence directory in CopperOS:
`obj/dos-runcommand-input-lease/ab9919f9e4964f9aa1a0e7c239187259/`.

- Initial unchanged-production run: 30 new host rows, **29 failed / 1 passed**.
  This directly showed caller input replacing command arguments, missing
  GetArgStr restoration, wrong/non-top resumes accepted, address-reused token
  acceptance and premature reset/retirement. The one passing row is an
  unchanged close/reuse control, not evidence for the new lease.
- First implementation: **34 passed**, the original 30 new rows plus the four
  adjusted existing RunCommand fixture/layout rows. The original log/TRX is
  retained as that run; it is not relabeled as a larger final run. Its exact
  intermediate test DLL was superseded before copying; the later 39-row test
  DLL is not presented as that binary. The matching production source/DOS
  checkpoint remains retained.
- Nine added safety/descriptor/corruption rows: **37 passed / 2 failed** before
  the changed/closed Input process-lookahead correction. Both failures and
  that intermediate source/binary snapshot remain retained.
- Final focused input/parser run: **87 passed / 0 failed / 0 skipped**: 39 new
  input-lease rows, four existing RunCommand rows and the unchanged 44
  original-derived explicit-source ReadArgs rows. The latter are still host
  tests here, not another execution of the original ROM or generated DOS.
- Related run and a further source-bound rerun: **271 passed / 0 failed /
  0 skipped** each, covering 22 classes. These are repeated runs of the same
  inclusive set, not 542 different test cases. The final rerun revalidated 250
  evaluated source/project/generated-source inputs and 141 dependency/output
  files before and after use, with no source or binary drift. The original
  30 new test bodies and shared fixture are byte-for-byte unchanged after
  removing only the nine appended safety cases.

Bootstrap failures (a missing live Copper68k reference assembly, then a test
source call to a nonexistent convenience codec writer) are retained
separately and are not counted as semantic before failures. The successful
build selected implementation assemblies with `BuildProjectReferences=false`
and `ProduceReferenceAssembly=false`; it did not rebuild compiler, SDK or
Copper68k. The live Copper68k implementation had changed externally since the
earlier native callback receipt, so each newly consumed identity is recorded
instead of claiming the earlier CPU binary was reused.

Commands, run from CopperStart:

```powershell
dotnet build src/CopperStart.Dos/CopperStart.Dos.csproj -c Release --no-restore -p:BuildProjectReferences=false -p:ProduceReferenceAssembly=false -v:minimal
dotnet build tests/CopperStart.Exec.Tests/CopperStart.Exec.Tests.csproj -c Release --no-restore -p:BuildProjectReferences=false -p:ProduceReferenceAssembly=false -v:minimal
dotnet vstest <bound-test-dll> '/TestCaseFilter:FullyQualifiedName~DosRunCommandInputTests|FullyQualifiedName~DosRunCommandCoreTests|FullyQualifiedName~DosReadArgsOriginal403Tests' '/Logger:trx;LogFileName=after-input-and-parser.trx' '/ResultsDirectory:<attempt>/after-results'
```

These successful builds report zero warnings/errors. Before/after production
sources, final test bodies, dependency closures, failures, logs and TRX results
are kept under the evidence directory. Final receipt: `receipt.json`, SHA256
`95c5d13a8d2d09c28b6bde92259b5dc692edcc6fd13a11ac109f62bf2a295f3b`.
Its retained `after-bin/CopperStart.Dos.dll` has SHA256
`0b09af6ff0e225ac662071c2e7607f44be47a97a65559820028da560aee08df5`;
the test DLL is
`3aa650b23dd7547a748f3c76f549e726dab104aa33300660e4d5f31b4f29ee2a`.
The final production source hashes are:

| CopperStart source | SHA256 |
| --- | --- |
| `DosCore.cs` | `ae7d985f99585d0f5fd3af3c40b160bc97204ab78950afae17115c2de94068f6` |
| `DosCore.RunCommandInput.cs` | `0aa1fcec7c19b57eb1293b28d617e42a940a2ea909eb6500a84c77d0cd68b978` |
| `DosRunCommandCore.cs` | `3f16a44b5c3f399810bf300bdf5b844b9376684391c0e9c97055f87ca2d9ec62` |
| `DosObjectRecords.cs` | `79a497721ab3e6a7023393b3f79496b7d85e2fa123e8c81bed32e6e608285791` |

The inclusive run re-executes the unchanged actual native callback boundary:
12 rows across 68000/020/040 and default/disabled peephole modes, with 18 native
command invocations and 12 never-started callbacks. Its six HUNK shapes remain
byte-identical to the earlier bridge checkpoint, including the 1208/1216/1224
byte default variants. The actually loaded Copper68k is
`b4aabe2321890b4498ae9693dbad3f2322069522fb5e26772f6879e2d796da65`,
not the earlier `bd42669e...` binary. This still uses an Exec StackSwap vector
fixture; it does not execute the new input layer or original RunCommand on a
CPU. All native callback compatibility reports retain zero runtime helpers,
runtime features, fatal sites, external native targets and managed allocation;
their exact three reachable assembly hashes and loaded runtime identities
match the retained dependency closure.

No original executable, ROM, NDK text or vendor source is copied into the
repository by this slice.

## Native rejection rollback checkpoint

Both native dispatch loops now forward the boolean returned by
`InvokeGuestCallback` to their existing continuation owner. A false return
proves that the callback did not enter; it no longer leaves the published
RunCommand input/stack lease pending. `DispatchStored` uses the existing router
resume path and `DosNativeDirectBoundary.CompleteGateway` uses its existing
`Resume` switch. The bounded loop can continue if an owner requests further
work. Successful callbacks still forward their real result. The callback
implementation, `ExecuteOnStack` bridge, portable input lease, ReadArgs parser,
private layouts and SDK/compiler sources are unchanged by this checkpoint.

The new `DosNativeCallbackRollbackTests.cs` executes the actual direct
completion boundary and actual callback helper on 68000, 68020 and 68040. It
prepares a lease with the real portable owner on the host, then supplies an
explicitly rejected request to native code. The four cases are null entry,
null StackSwap pointer, unsupported callback kind, and a stale token, each
with and without an outer lease: **24 rows**. The three legitimate rejected
requests must restore only their owned top lease; stale tokens must decline
without modifying any prepared guest state. This is fault injection into the
boundary, not a claim that normal RunCommand preparation produces bad request
fields.

Before the two loop changes, **24 failed / 0 passed** after actual native
execution. Each returned normally through one real callback/helper invocation
with no `FreeMem`. The 18 valid rejected requests returned the caller's old
D0 (`0xCA111234`) instead of the failed-start result; the six stale cases left
the supplied callback disposition unresolved. A separate first attempt stopped
at host-fixture `Open` because its file length was omitted. That aborted setup
run is retained separately and is not counted as the semantic baseline.

After the change, **295 passed / 0 failed / 0 skipped**: the same 24 new rows
plus the existing 271 related rows. The new rows prove 18 restored/freed child
leases and six stale-token declines without guest-state mutation. The bus
admits only an exact owned `Exec.FreeMem`, poisons the documented volatile
registers, rejects access after release and rejects every other gateway or
command entry. It checks the direct boundary's preserved registers and stack,
unchanged code and guard bytes, restored arguments/input lookahead, unchanged
provider buffer/position and unchanged outer-lease bytes. No command or
StackSwap is executed by these new cases.

This direct-boundary matrix uses the resident compiler profile with peepholes
disabled. Its compiled closure includes the production Resume switch, not
just the exercised RunCommand branch. All six before/after reports record zero
fatal sites, runtime helpers/features, external native targets and managed
allocation. The test loads the compiler's emitted code/relocation view through
the existing NativeParityBus; it records the corresponding HUNK but does not
independently qualify HUNK transport. The existing 12 callback-ABI rows still
cover default and disabled peepholes, including 18 native command invocations,
12 never-started callbacks and 36 fixture StackSwaps. The 44 explicit-source
ReadArgs rows remain host regression checks in this run.

`DispatchStored` rejection is **source-reviewed only** here. It does not expose
a supplied-result boundary, and normal RunCommand preparation validates and
constructs its entry/descriptor. The direct-boundary tests are not attributed
to an unexecuted legacy rejection path. Original RunCommand, actual Exec,
normal NULL-D3 native ReadArgs, host LIFO/reset/retirement, Exit/nonlocal unwind,
and command publication or pure/resident packaging admission remain separate
work.

Evidence: `obj/dos-native-callback-rollback/7f1bf8e85baf4fcba64d77e3c1585e51/`.
The receipt binds 251 source/project/generated-source inputs and 141 binary
files before/after, confirms that only the two production loop files changed,
and verifies unchanged regression source plus actual loaded identities. Tests
ran from retained dependency directories under
`CopperStart/obj/native-callback-rollback-tests/7f1bf8e85baf4fcba64d77e3c1585e51/`.
Both runs use identical compiler, SDK, Support, Exec and CPU binaries. The
actually consumed Copper68k is
`8b7510f64bb9184d0224c3d19f5f0d5f1cc0b18707eb9cbca2473db5b0886dcd`,
not the earlier portable checkpoint's runtime. No emulator rebuild was made
by this slice.

The DOS and test projects were built with the same `BuildProjectReferences=false`
and `ProduceReferenceAssembly=false` commands recorded above; both builds
report zero warnings/errors. The retained `after-test-filter.txt` gives the
exact 23-class filter, and `write_receipt.py` checks the TRX counts, executed
case matrix, source/dependency hashes and emitted artifact identities. Final
receipt SHA256:
`b3d3be62c1e2ce6b1e5881a26ea9a8c92c7c5c5a2641ac9ef89af31747351084`.

| Final input | SHA256 |
| --- | --- |
| `DosNativeEntrypoints.cs` | `948902113c37bfcbba02709a1ec50706295eedd5b6b0fab44f542f3e62a02f88` |
| `DosNativeDirectBoundary.cs` | `428bdef7ee0a6101afa331e4674d9d9190c5a9e888f928a7e739cc5b1150b817` |
| `DosNativeCallbackRollbackTests.cs` | `5aeeebc79cf32ac3db82030a7fb23bb87a07a43bc6f5b5f0b5cd145d23cf954a` |
| Retained `after-bin/CopperStart.Dos.dll` | `3ff7403fdb3f20c07e2d81baa2ff212a7ad71b55ad737fb777c687675adffbd4` |
| Retained `after-bin/CopperStart.Exec.Tests.dll` | `ca1460aa53c10c3762434e354cdefdd72721a1a980b20d975a27b1b13d90b330` |

## Open compatibility gap: public Close return

The normal-input integration trial
`obj/dos-native-runcommand-input/5cadccd5362e4eb086af5dbeb67dfee2/`
executed the authored LF-only observer through installed native RunCommand and
normal NULL-D3 ReadArgs. It returned 42, called FreeArgs once and restored the
caller input, unread buffer, provider position, outer ABI and allocation count.
The trial nevertheless remains **failed**: its final caller cleanup expected
a boolean success value from public `Close`, but received zero after the
provider's successful `ACTION_END` and handle disposal.

This is existing behavior: `DosCore.Close` returns zero on success, and
`DosNativeProviderBridgeTests.cs:792` explicitly expects zero from the native
Close vector. The licensed V40 DOS autodoc declares a boolean success result
for Close starting with V36. Its local authority is the pinned developer CD
(`5d6bfcb213f1395d4c95584dc94d0e36265be355076dae1710bd36fb4dfdbff3`),
DOS.DOC extent 17730, 174666 bytes, Close entry at byte offset 21197. No NDK or
vendor text is copied into this repository.

**Public Close return compatibility remains open.** It needs its own owner
correction and original-derived success/failure tests before commands may rely
on the V36+ boolean contract. This issue belongs with the API gaps tracked by
the [completion ledger](completion-ledger.md); it is separate from the
[normal command-input contract](native-command-launch-contract.md).

The bounded normal-input test records the actual cleanup Close result, proves
one provider End, IoErr zero, and exactly one release each of its explicitly
opened handle, buffer and provider record. It neither standardizes zero as
the public success contract nor claims original Close equivalence. The authored
command itself borrows Input and never closes it. Production Close, provider,
parser, compiler and SDK sources are unchanged by this test slice.

## Native normal-input checkpoint: passing 000/040 subsets, failed 020 matrix

`DosNativeRunCommandInputTests.cs` and its separate fixture execute the installed
native DOS owner through its real RunCommand, Input, GetArgStr, ReadArgs,
FreeArgs, FGets, FGetC and IoErr vectors. The observer is authored test code,
not a distribution command. Each CPU has six planned invocations: ReadArgs,
FGets and FGetC, each with the exact LF-only and `alpha beta` plus LF inputs
from original observations R05/R06. ReadArgs receives NULL D3 and the native
parser owns each returned RDArgs until exactly one native FreeArgs.

The final three-CPU attempt **failed**. Its 68000 and 68040 subsets each passed
all six invocations. These 12 accepted commands contain four normal NULL-D3
ReadArgs/FreeArgs pairs, four FGets calls, 24 command FGetC calls and 24 fixture
StackSwaps. The mapped caller has 11 unread bytes of `caller-tail` plus LF
after native prefetch. Each accepted command leaves the backing provider at
position 12 with no provider packet during the command. It restores the same
Input, its public/private record and buffer, process arguments, stack bounds,
outer library ABI and owned allocation count. The next native FGetC returns
the saved caller byte without advancing the provider. Caller cleanup records
the [open Close return gap](#open-compatibility-gap-public-close-return).

The unchanged frozen 68020 core raises `UnsupportedM68kTimingException` for
opcode `48F9` at `$00601148`, profile `Ocs68020_14MHz`, during native
**MakeDosEntry(-696)** setup. It completes 171671 setup instructions before
stopping, with zero provider packets and zero command entries. The first
fixture setup fails; the remaining five are unattempted. All six planned
68020 invocations remain unqualified. No fallback timing mode, skipped test,
replacement runtime or input-fixture change is used to make this matrix pass.
The retained TRX files report two passed and one failed CPU rows, zero skipped.

Receipt:
`obj/dos-native-runcommand-input/bc62d3d24a434cd7853684bdfa5d4986/receipt.json`,
SHA256 `f7cd88e94ffbea1b6fa9210d7e10b4afe1ca1a9377fb25d4f23935e8fcd607a7`.
It verifies all 253 captured source/project inputs and 141 retained binary
files, the exact four reachable native assemblies, loaded paths/MVIDs, all
three HUNK/map/compatibility outputs and the full failed/passed case counts.
All three compiled closures report zero fatal sites, runtime helpers/features,
external targets and managed allocation. Native production source and the
prior accepted DOS dependency closure are unchanged; only the two new test
sources and their test DLL/PDB were added to the execution closure.

| Captured artifact | Bytes | SHA256 |
| --- | ---: | --- |
| 68000 DOS HUNK | 883692 | `f96a00b31bc2f48384ad88b8d7b53e6c83963207e4352d778709586b958b8030` |
| 68020 DOS HUNK, setup failed | 885064 | `2e7ec9a489fbc1ce921e7d137ba449a4c496ec38571557c24012f8b2df311db7` |
| 68040 DOS HUNK | 878908 | `98285a0dc83911f8d82819678edf81a88ead13d8e6f6e9a723472304f096989e` |
| Test DLL | 1614336 | `9c6caeb054f5099dc5e558873c2573eb04ac3bc1ccaa2365ea6e6bfff36f22e7` |

The two new source hashes are
`08058c726676f7d11ba4b5cc81e013d3a967b0437e07c8d5446a2deeda56d5fe`
for `DosNativeRunCommandInputTests.cs` and
`dd6f193e74e26d969548d4998b01d6cd4ea83b10af91a55a80bea96389768d68`
for `DosNativeRunCommandInputFixture.cs`. The consumed DOS remains
`3ff7403fdb3f20c07e2d81baa2ff212a7ad71b55ad737fb777c687675adffbd4`,
compiler `814b2adeb0b2bad5d0fac673c97af1e2a9aed9557f12c6dfac9fbfdce5a4d9e7`,
and Copper68k
`8b7510f64bb9184d0224c3d19f5f0d5f1cc0b18707eb9cbca2473db5b0886dcd`.
External changes to the live CPU DLL are not rebound into this receipt.

The Exec library gateways, StackSwap and bounded file packet handler remain
fixtures. Commands enter with SR `$2700`; this does not reproduce original
R05/R06 entry SR `$2000` or qualify ordinary Shell launch, original RunCommand,
task scheduling, Exit/nonlocal unwind or boot. Each invocation uses fresh
mapped DOS/Process state, so these tests do not establish native nesting or
interleaved shared-image reentrancy. The input is explicitly opened and later
disposed by the caller; the installed DOS/device foundation remains owned by
the fixture. No distribution command, pure/resident packaging, public command
admission or exact hardware timing is claimed by either passing subset.

## Narrow 68020 MOVEM owner correction

The failed setup above emits `48F9 7FFF 006000D8`: a normal-mask
`MOVEM.L D0-D7/A0-A6,($006000D8).L` at `$00601148`. This is a legal absolute
long destination. Normal memory order is D0 through D7, then A0 through A7;
the selected registers are written without modifying those registers or the
condition codes. The independent regression also uses mask `8081` to include
the original A7 and mask `0000` to prove a zero-register transfer. See the
[Motorola programmer's reference, MOVEM, printed pages 4-128 to 4-130](https://www.nxp.com/docs/en/reference-manual/M68000PRM.pdf).

`MovemAbsoluteLongExecutionTests.cs` executes one instruction through each
public 68000, 68020 and 68040 core. The six cases per CPU pair those three
absolute-long transfers with existing address-indirect controls. Each checks
the instruction bytes, exact ascending memory-write order, PC advance,
unchanged SP/registers/SR, original A7 value when included, and untouched
code, stack and surrounding memory. Both the original frozen CPU and a
separately captured current CPU produced **15 passed / 3 failed**. Only the
three 68020 absolute-long cases failed, before publishing any transfer.

The production change adds exactly this opcode to the existing 68020 advanced
timing interpreter. It uses the established instruction-pipe fetches and timed
bus writes, snapshots the register values, and appends an internal timing key.
It does not change `M68kCore.cs`, other addressing-mode timing, or the fallback
policy. The 68000 and 68040 paths retain their prior behavior. The existing
dynamic-timing classification test gains the new key; the timing formula is
unchanged. For the selected 68020 cache-case model, the existing MOVEM cost
`4 + 3n` plus the word-mask/absolute-long calculate-immediate cost `4` gives
`8 + 3n`, or 53, 17 and 8 cycles for these masks. This is the bounded model
from [MC68020UM sections 8.2, 8.2.4 and 8.2.7, printed pages 8-10, 8-17 and 8-29](https://www.nxp.com/docs/en/data-sheet/MC68020UM.pdf);
it is not a claim about cache misses, Amiga arbitration or exact 68040 timing.

Private source-matched before/after builds ran the unchanged focused test
binary: **15 passed / 3 failed before, 18 passed after**. The same related-test
binary ran **630 passed / 3 failed before, 633 passed after**, zero skipped.
The 18 focused rows are included in those 633 rows. All positive controls
retain the same observed state, write order and timing. Existing related
address-error and ordering tests also pass, but this work does not add a new
MOVEM bus-fault or exception-frame qualification.

Both private builds retain the captured `M68kCore.cs` SHA256
`db60fa4a068d5a9f10ae7008ea33ea8a7853113d94c5276443b54754ace9afeb`.
Unrelated live work changed that file to
`1874386d1baa2c66a4261356d420c0e4d8e960e41471dccd893d77a8fb56c13d`
during this investigation. It was neither absorbed nor reverted; these tests
do not qualify that later live Core source. Each private CPU has matching PE/PDB
identity and all 24 source-document checksums. The original frozen CPU's PDB
has an older Core checksum; the separately captured current CPU matches all
24 captured source files. These identities remain distinct in the receipts.

The evidence directory is
`obj/cpu-movem-absolute/f49db97675f247e6a81cfc4dbc0ba2d3/`. Its
`baseline-receipt.json` SHA256 is
`8c074acfd12a4ac740be8430b8876fa341a6e8e92746e18f8e8f06b748ca1482`;
`fix-receipt.json` SHA256 is
`bcb5091f3bc7686bd3272e666f256649cab59d5745f88aea48ad67cced7612bc`.
The receipts bind the captured sources, dependency files, private build logs,
loaded CPU identities, per-case native observations, TRX results and PDB
checks. No shared CPU/compiler/SDK build was performed.

| Corrected owner or test | SHA256 |
| --- | --- |
| `Copper68k/M68kAdvancedTimingInterpreter.cs` | `80601c759838704d9d095c2382e27b8ac79b3b74d27adb1dc9189db35ca8060d` |
| `Copper68k/M68kTimingEngine.cs` | `b53bb1d54bfc043f407c5670992d463f1510733f0d900a5156d29a43cd550a6f` |
| `Copper68k.Tests/MovemAbsoluteLongExecutionTests.cs` | `7db15ba7e91382c7c635abcfd792fa9e93157e5d245052f2b0b0c3697691c679` |
| `Copper68k.Tests/M68kTimingFormulaTests.cs` | `fd8f5215a14fe4b238e5c47054f9742cd4d9135e4085017b2144d2a928ffcf7e` |
| Private corrected `Copper68k.dll` | `29613e4c1f205fdc2b101678db2b0c418683c72ae1ea5562ef88191b199684f8` |
| Private corrected `Copper68k.pdb` | `8138aef8bc45590b923b1b082787d91323f6cf4a716ddb51e0c5b5689f463122` |

## Explicit CPU rebound: six native 68020 input invocations passed

A new attempt, `d2899a05e9e941e0bee11724fb5861d7`, copied all 141 retained
runtime files from the failed matrix and replaced only `Copper68k.dll` and
its PDB with the private corrected pair above. It used the unchanged test
DLL, DOS `3ff7403`, compiler `814b2ade`, SDK and Support. Its source authority
remains the 253 retained source snapshots, with the CPU's 24 private source
documents bound separately. Later live DOS or CPU changes are not included.

The unchanged 68020 test re-emitted the native DOS image and passed all six
authored invocations. The 885064-byte HUNK and its map are **byte-for-byte
identical** to the retained failed 68020 attempt: HUNK SHA256 `2e7ec9a489fbc1ce921e7d137ba449a4c496ec38571557c24012f8b2df311db7`,
map SHA256 `04713f6454fdaf03cd05b649de84743136c4df9f43129a6bc957e6b4ba798e69`.
The emitted closure still has the same four reachable assemblies and zero
fatal sites, runtime helpers/features and external native targets. Neither
the instruction nor the input fixture was bypassed.

The result is **one host row passed, zero failed/skipped**, containing six
native observer invocations, two NULL-D3 ReadArgs/FreeArgs pairs, two FGets
calls, 12 command FGetC calls and 12 fixture StackSwaps. Native execution
totals 5567370 instructions; 262 execute the authored observer itself. Every
case returns 42, preserves the prefetched caller input/provider position,
restores the outer ABI and stack bounds, and releases its invocation-owned
allocations. The old-owner caller cleanup still records Close zero and its
separate compatibility limitation. These executions continue to use the
fixture Exec/StackSwap and bounded file packet handler described above.

The frozen replay command was:

```text
COPPERSTART_NATIVE_INPUT_ARTIFACTS=<new-attempt>/native
dotnet vstest <new-frozen-bin>/CopperStart.Exec.Tests.dll
  --TestCaseFilter:FullyQualifiedName~DosNativeRunCommandInputTests.M68020
  --ResultsDirectory:<new-attempt>/results
  --Logger:trx;LogFileName=rebound68020.trx
```

The validation script checks all captured binaries and retained sources,
the two-file runtime delta, loaded paths/hashes/MVIDs, private CPU source/PDB
binding, exact HUNK/map identity, six-case matrix, ownership/cleanup records
and TRX outcome. Receipt:
`obj/dos-native-runcommand-input/d2899a05e9e941e0bee11724fb5861d7/receipt.json`,
SHA256 `ca80d76d7cec12542d8de189665f7b7ce46d169962e6cc904014aa667e75d3e8`.

The earlier `bc62` three-CPU receipt remains **failed**. Its accepted 68000 and
68040 subsets still identify the original `8b7510` CPU; this new 68020 subset
identifies the private corrected `29613e4c` CPU with retained `db60` Core.
There is no claim that one new CPU binary passed a full three-CPU matrix.
The replay establishes the bounded normal-input behavior only: no original
RunCommand, Shell launch, nested scheduling, Exit/unwind, distribution
command or pure/resident packaging admission is added.
