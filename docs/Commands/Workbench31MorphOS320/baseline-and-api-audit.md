# CC02 baseline and bounded native-entry audit

Recorded 2026-08-30 for `Goals/WORKBENCH31_MORPHOS_C_COMMANDS_GOAL.md`.
This report records the existing Shell baseline, startup/API findings, and
subsequent bounded native qualification. It does not qualify a shipping command,
native boot, or the full Workbench/MorphOS inventory.

## Baseline identity and ownership

| Repository | HEAD observed during this audit |
| --- | --- |
| CopperOS | `6eb97f02cfebba172fb039dd07dac18940484be9` |
| CopperSharp68k | `46ac6600c3c1a5ce7aceb2236ae744d63aa1cd15` |
| CopperStart | `d30497b315e819ce5cd3881bfe975c818088b642` |

These are base commits, not clean-tree claims. CopperOS already contained
unrelated MUI edits, generated output, and untracked goal files. CopperSharp
also contained concurrent optimizer, SDK, and build changes. Record the exact
artifact hashes and relevant source diff/revision when running native acceptance.

The initial audit changed only this report. A subsequent authorized correction
changed `src/System/Shell/ReadArgsCommandSupport.cs`, added
`tests/Commands/ReadArgsCommandSupportTests.cs`, and strengthened the existing
test platform in `tests/Commands/EchoCommandTests.cs`. It aligns DOS result
storage without changing command options or semantics. Those initial stages did
not change the compiler, SDK, emulator, native harness, or command tooling, and
the Shell script that removes shadow SDK assemblies was not run. Later authorized
CC02.API09 and CC04 work corrected the compiler's Amiga call masks and added the
native argument suite described below; emulator and SDK source remain unchanged
by these slices.

## Existing command test results

| Attempt | Command | Observed result |
| --- | --- | --- |
| Initial pinned-package baseline | `dotnet test tests/Commands/CopperOS.Commands.Tests.csproj --verbosity minimal` | Build failed before tests: CS0246, `IAmigaGuestMemory` missing at `src/System/Shell/IShellPlatform.cs:12`. |
| Initial local-SDK baseline | `dotnet test tests/Commands/CopperOS.Commands.Tests.csproj --verbosity minimal -p:CopperOSUseLocalCopperSharp=true` | Same CS0246 after the local compiler and SDK built successfully. Changing SDK selection alone did not repair the missing assembly reference. |
| Coordinating implementer's post-fix baseline | Existing Commands test project with the normal pinned SDK | Reported **209 passed, 0 failed, 0 skipped** after adding coherent package/local Support references to `src/System/Shell/CopperOS.Shell.csproj`. This row records the coordinating implementer's run; the audit did not independently rerun it. |
| Alignment regression before the correction | Commands test project filtered to `FullyQualifiedName~ReadArgsCommandSupportTests` | 17 failures, 6 passes. Cases exposed odd/misaligned result slots, mutation of undersized workspaces, and a high-address write before alignment-span rejection. One all-command fixture also needed a valid Ask input handle; that fixture was corrected before the final runs. |
| Alignment regression after the correction | `dotnet test tests/Commands/CopperOS.Commands.Tests.csproj --filter FullyQualifiedName~ReadArgsCommandSupportTests --verbosity minimal` | **23 passed, 0 failed, 0 skipped**. |
| Complete post-alignment baseline | `dotnet test tests/Commands/CopperOS.Commands.Tests.csproj --verbosity minimal` | **232 passed, 0 failed, 0 skipped**, including the original 209 cases and 23 new alignment/boundary cases. |

The missing type was a project dependency problem. Local
`Sdk.Amiga/CopperSharp.Sdk.Amiga.csproj` excludes `GuestMemory/**/*.cs` and
`**/GuestCodecs.cs`; `Sdk.Amiga.Support/CopperSharp.Sdk.Amiga.Support.csproj`
links those definitions and references the same SDK. The Support g20 package
was already installed in the local NuGet cache. Keep the coherent Support
reference; do not create another `IAmigaGuestMemory` definition or mix a local
SDK with a different package ABI.

The 232-case result is a Shell/command semantic baseline. It is not proof of
all external commands, native DOS parsing, a bootable Shell, or resident safety.

### 2026-09-26 current command-test rerun

Most recently reran from CopperOS HEAD `5dc8e7dde1787b85ceebd1379587921b6bc01980`
with the working tree dirty (955 status entries):

```text
dotnet test tests/Commands/CopperOS.Commands.Tests.csproj --verbosity minimal
Passed: 704, Failed: 0, Skipped: 0; duration 407 ms
```

The run rebuilt the Shell and Commands projects using .NET SDK `10.0.303` and
executed the complete `CopperOS.Commands.Tests` assembly. Its SHA-256 is
`14b0c932a1013f8b2e3cd571020fbabaebfe2d6843a238bab893437badeb84db`.
This refreshes the managed Shell/command regression baseline only; it does not
qualify the 200 external commands, native DOS `ReadArgs`, a native Shell boot,
resident safety, or guest parity.

The existing Shell park artifacts were last written on 2026-08-16. The current
`tools/Shell/qualify_native.ps1` deletes four SDK/support DLL copies from the
sibling CopperStart Release outputs and overwrites the existing Shell HUNK and
assembly-listing outputs. Those shared outputs were preserved. A separate
isolated run is now recorded below; CC02 remains open because its static reports
contain fatal machine-fault findings and it does not execute a guest.

### 2026-09-26 isolated Shell native build

Ran `tools/Shell/qualify_native_isolated.ps1` into
`artifacts/shell-native-isolated-20260926-v6/`. It restored and built the Shell
native-root project, copied the CopperStart Exec/DOS managed inputs into the
artifact directory, and emitted one 68000 HUNK plus 68020 and 68040 assembler
listings. The hash-bound receipt is
`artifacts/shell-native-isolated-20260926-v6/qualification.json`.

All three targets reached 2,247 methods with zero managed allocation sites,
runtime features, runtime helpers, external native targets, or exception
regions. Each target's compatibility report also records **three fatal
machine-fault sites**. The 68020 listing maps them to guarded divide-by-zero
`ILLEGAL` instructions in `StackCommand.WriteStackSize`. The divisor starts at
one; the digit loop reduces it by ten and exits when it reaches zero, before
another body iteration. That source-level invariant makes these three traps
appear unreachable, but the static count itself does not establish runtime
behavior. The receipt records a static build baseline only. The 020/040
outputs are assembler listings, not executable HUNKs, and no original
operating-system guest was run.

The isolated receipt confirms `siblingCopperStartSdkCopiesRemoved: false` and
`previousShellNativeArtifactsOverwritten: false`; their hashes and timestamps
remain untouched. This closes the safe-output-handling gap for refreshing
static baseline artifacts, but CC02 still needs investigation of the fatal
sites, API/ownership reconciliation, and the planned behavioral evidence.

The alignment fix uses the absolute guest address after the template's NUL,
not a relative-offset assumption about where the workspace starts. It admits
all four possible workspace address residues and verifies padding plus the full
LONG-array span before writing. The new tests drive public command entrypoints,
including all 30 helper-backed internal commands, exact-fit and one-byte-short
workspaces, guard bytes, and uint-range/alignment overflow. The fake DOS boundary
now refuses odd LONG-array addresses to avoid hiding this MC68000 restriction.

## Existing work retained

- `src/System/Shell/ShellInternalCommand.cs:6` contains 31 distinct internal
  identities; `ShellCommandDispatcher.cs:37` onward routes all 31 to existing
  implementations. The older count of 32 included the `Echo`/`echo` case test.
- `src/Commands/ExecuteCommand.cs` is the existing external wrapper. It delegates
  to the Shell and uses `FILE/A`; `ExecuteCommandTests.cs:36` onward currently
  rejects trailing arguments. Preserve it and resolve script-tail behavior as
  an explicit compatibility/integration item.
- `CommandInvocation`, bounded workspaces, script frames, redirection,
  continuations, and resident use counts remain the shared owners. Production
  command binaries must not acquire a private copy of DOS, Exec, or the Shell
  scheduler by linking qualification roots into the command image.
- `tools/Shell/qualify_native.ps1` produces a 68000 HUNK but 68020/040 assembler
  listings. Its prior compile reports do not establish three executable HUNK
  variants or real Kickstart execution. It was intentionally not rerun here.

## Original command startup evidence

The original local developer CD was inspected read-only with a bounded ISO
directory reader; no files were mounted or original binaries executed.

| Reference inside `D:/TestData/AmigaDeveloperCD.iso` | SHA-256 of the inspected file |
| --- | --- |
| `NDK_3.1/INCLUDES&LIBS/STARTUPS37/STARTUP.ASM` | `a8a75cb9335456699ba1db9858bdb3f6f0dc53eeb18ab90bb8aa796c16b5e0c6` |
| `NDK_3.1/DOCS/DOC/DOS.DOC` | `2e22d1d1b7c00fac5757eead0ef4d49a01c7666f0db5850735193c9597a7cee2` |

`STARTUP.ASM:264-267` establishes D0 as command-text length and A0 as command
text. **The image entry may modify every register except SP.** Do not impose
the library/subroutine callee-save convention on the whole command entry.
Preservation tests belong to each actual boundary: SP/return integrity and D0
result for an image entry; the declared register convention for an exported
function, callback, or library vector.

`STARTUP.ASM:289-305` reads ExecBase from address 4 and opens `dos.library`.
Incoming A6 is not documented as DOSBase. `STARTUP.ASM:363-365` distinguishes
CLI from Workbench via the Process CLI field. Its Workbench path obtains the
startup message before DOS calls (`524-527`, `625-630`), and replies with
scheduling forbidden so Workbench cannot unload the executing segment before
return (`660-668`). Correct Workbench rejection/failure paths must also consume
and reply to their startup message. Complete owned cleanup before that terminal
handoff; do not insert an unsafe scheduling point after the reply.

`DOS.DOC:4487-4529` describes `RunCommand`: the caller passes the SegList,
stack size, argument pointer, and length; DOS returns the program's result or
`-1` if it cannot allocate the stack. The argument string must end with a
newline for `ReadArgs`. The V37 implementation supplies the input-buffer and
`GetArgStr` handoff and restores it afterward. A native harness must model this
separately from an arbitrary C-style argv vector.

## Existing compiler path: use it before adding another startup mode

The compiler already supports `M68kRuntimeProfile.Resident`
(`Compiler/PublicApi.cs:94`) and CLI `--runtime resident`
(`Compiler.Cli/Program.cs:478`). It explicitly permits a resident image to be
invoked concurrently and places compiler-owned library-base state per invocation.

Use an ordinary selected entry with the exact recognized signature:

```csharp
[M68kEntryPoint]
public static int Main(int argLength, CONST_STRPTR argText)
```

`M68kCodeGenerator.cs:1355-1358` recognizes exactly `int` followed by
`Amiga.CONST_STRPTR`; replacing those with `uint` and `APTR` is not equivalent
to this compiler's image-entry convention. `Sdk.Amiga/Examples/DOS/Program.cs`
already demonstrates this signature, explicit DOS opening, and close on exit.
The simpler Amiga application template uses a no-argument entry and therefore
is not by itself an argument-lifetime probe.

The command build must explicitly choose the resident profile; HUNK CLI output
otherwise defaults to `application`. Use a bounded native closure, `--memory
none`, disabled FPU, the appropriate no-exception lowering, and a deliberate
export selection. `--exports none` avoids pulling unrelated attributed exports
into a standalone command. Produce HUNK output for each CPU rather than treating
assembler output as an executable.

For the first portable probe, normal SDK `Exec.OpenLibrary`,
`DOS.DOSLibraryBase`, DOS vector calls, and `Exec.CloseLibrary` are a suitable
path with **resident** lowering. Select the lowest verified capability version
for every consumed library, device, resource, and ROM vector—not merely the
version requested by the captured binary. Retain the opened base as an
invocation-local value, and close only a successfully acquired library. Use a separate CLI/Workbench startup
decision; the compiler's D0/A0 adapter is not a complete WBStartup owner.

The command-specific version records and unresolved provider floors are kept in
[`capability-floor-audit.md`](capability-floor-audit.md). New commands must add
their consumed vectors and evidence there before changing an `OpenLibrary`
minimum.

The supporting implementation is already present:

- `M68kCodeGenerator.cs:780-801` assigns materialized writable library-base
  slots offsets in invocation storage, reached through A5.
- `M68kCodeGenerator.ManagedPoolRuntime.cs:100-110` saves the startup D0/A0 pair
  before context allocation; `140-144` restores the pair before the entry call.
- Contexts of at most 512 bytes normally use the caller's stack. Larger contexts
  use Exec `AllocMem` and `FreeMem`; see `PublicApi.cs:464-468` and
  `ManagedPoolRuntime.cs:241-278`.
- A5 is reserved for the context. `localloc`, static field access, type
  initialization, managed allocation, boxing, and delegates are rejected in the
  resident profile (`M68kCodeGenerator.cs:748-839`). Fixed value-type locals and
  explicit Exec/DOS allocations remain the appropriate storage choices.
- Root-only entry register clobbering (`M68kCodeGenerator.cs:319-338`) is
  consistent with the original image-entry allowance. Do not mistake it for a
  purity defect or change the compiler solely to preserve arbitrary entry
  register sentinels. Export adapters have their own obligations.

The default 512-byte threshold is exposed in the compilation request API, but
the inspected CLI does not expose a corresponding threshold option. Use a
compiler-API fixture to force the heap-context path for fault injection; do not
invent an existing CLI option.

### Existing limitation that can produce a false command success

If compiler-owned heap context allocation fails, the current resident adapter
returns scalar D0 **zero** (`ManagedPoolRuntime.cs:281-293`). The existing test
`ResidentProfileReturnsZeroWhenInvocationContextAllocationFails`
(`CompilerExecutionTests.cs:3344`) intentionally expects this generic result.
For an AmigaDOS command, zero means `RETURN_OK` even though its body never ran.

Normal small command closures can avoid that branch by proving their context
stays within the stack threshold. Such proof must be recorded per artifact,
not assumed for future dependency growth. Heap-context command entries remain
unqualified until there is command-specific failure mapping, a checked wrapper,
or another validated strategy. Do not globally change a generic export's result
contract without considering its existing callers.

## Explicit-base alternative

The target already supports `AmigaLibraryBasePolicy.CallerProvided`, mapped to
`M68kExternalBaseSource.Argument` in
`Targets.Amiga/AmigaCompilerPlatform.cs:236-261`. Declarations receive a base
through an A6-annotated parameter; AHI and ConsoleDevice are SDK examples.
This path avoids a compiler-owned global library slot when the caller naturally
has an explicit base.

Use that alternative only when needed by a concrete native adapter or when
resident lowering cannot satisfy a required ABI. Public Exec/DOS signatures and
constants still belong in the SDK. Do not duplicate a parallel handwritten SDK
inside CopperOS, and do not replace every working ordinary call merely to avoid
testing the existing resident profile. `Provided` is a fixed build-time address,
not a portable replacement for dynamically obtained DOSBase; `AutoOpen` is not
implemented by this target.

## API gaps and qualification obligations

| ID | Finding and required follow-up | Owner / status |
| --- | --- | --- |
| CC02.API01 | SDK Support assembly is missing from the initial Shell references. Keep package/local ABI identities coherent. | Shell project dependency; fixed by coordinating implementer, post-fix 209/209 baseline reported. |
| CC02.API02 | Original `ReadArgsCommandSupport.Prepare` placed LONG result slots immediately after the NUL. The authorized correction aligns the absolute result address to four bytes and checks padding/capacity/wrap before writes. | Shared helper fixed; 23 focused and 232 complete host cases pass. Actual Kickstart ReadArgs execution remains part of API03, not claimed by these tests. |
| CC02.API03 | Preserve true DOS `ReadArgs` source, newline, interactive `?`, zero/default result-slot, and `RDArgs` lifetime rules. | Invocation-owned native helper and supplied-vector lifetime/boundary tests pass; original parser/help/source equivalence remains open. |
| CC02.API04 | Generic resident heap-allocation failure returns zero. A command must not report success without running. | Compiler/command startup boundary; qualification open, stack-only bounded closures can proceed with explicit evidence. |
| CC02.API05 | Correct CLI/WBStartup ownership, Process state, result/error propagation, and cleanup need a portable command startup adapter. | Adapter implemented; 31 startup invocations per CPU pass supplied-vector execution, including exactly-once WB message handling. Real OS launch integration remains open. |
| CC02.API06 | Full Execute script-tail compatibility, lookup/script ownership, actual DOS input buffering, and standard launcher integration need tests beyond the existing FILE/A wrapper. | Existing Shell/Execute owners; open integration work. |
| CC02.API07 | Existing Shell qualification roots include CopperStart implementation assemblies. New shipping binaries must reach public vectors without bringing OS implementation or test state into their closure. | Both private command suites pass bounded public-SDK closure checks on all three CPUs; every shipping command still needs its own closure evidence. |
| CC02.API08 | Standard classic API calls need original vector/register/layout/version verification; optional MorphOS services need separate real providers. No complete all-command API audit was performed in this bounded slice. | Versioned command contract and provider ledgers; open. |
| CC02.API09 | External-call masks omitted volatile registers absent from a method signature. WaitPort left a live port in D1; Forbid left the startup message in A0. | Amiga-specific convention metadata plus both backend effect consumers corrected; 33 focused compiler tests and 549 related tests pass. Native startup and argument matrix passes after the fix. |
| CC02.API10 | Address-exposed 32-bit parameters were read from stale incoming SSA values after a by-reference mutation updated their stack homes. Numeric formatting could fail to terminate and write before its destination. | Existing argument homes are now reloaded for 32-bit machine values; 18 native regressions failed before and pass after, with 99 related tests passing inclusive. The numeric component passes 540 native invocations. Narrow/wide mutable homes remain unqualified; see [compiler qualification](compiler-qualification.md). |
| CC02.API11 | The static no-heap validator rejected a resolved value-type constructor even though existing lowering creates a frame temporary. This blocked the eight-byte I/O error record. | Static analyzer now admits value-type construction while traversing its body and rejecting real nested class/array allocations. Sixteen focused cases pass, including twelve native CPU/runtime/optimization cases and four allocation-rejection controls; 85 related tests pass inclusive. This changes validation, not value lowering or the no-heap policy; see [compiler qualification](compiler-qualification.md). |
| CC02.API12 | Body folding compared synthetic imported calls by their default method identity, merging WriteOnce with ReadOnce and distinct cross-module imports. | Imported/platform operands must identify the same caller-module member token before wrappers can fold. All 24 native reproductions failed before and pass after; 134 related tests pass inclusive, retaining same-target/managed folding controls. No SDK or command workaround; see [compiler qualification](compiler-qualification.md). |
| CC02.API13 | Native DOS discovery inspected private fields on an unrelated real Exec library, then faulted through a bogus owner before initialization. | CopperStart discovery now validates public library identity/sizes, complete PUBLIC-memory spans, sealed owner identity/backlinks and DOS state before private/deep access. Forty-nine owner/discovery tests pass. The isolated native image installs in 169,739 instructions and repeated discovery returns the same base in 6,620 without reinitialization. The subsequently corrected parser passes its separate 44-pair explicit-source corpus; see [parser qualification](readargs-parser-qualification.md). |
| CC02.API14 | RunCommand must substitute/restore normal input buffering and GetArgStr. The initial continuation delivered stack/A0/D0 without that protocol; explicit RDA_Source tests cannot qualify NULL-D3 command parsing. | [Native command launch contract](native-command-launch-contract.md) records R04 public NIL: ownership, exact R05/R06 plus one additional observer and three original/three generated MakeDir calls through original RunCommand/NULL-D3 ReadArgs: exact A01/A03 plus a quote variant. The portable input lease passes 87 focused/271 inclusive tests; native rejection rollback passes 24/295; [host callback ownership](dos-host-callback-qualification.md) passes 15/32. The historical native matrix has six authored invocations each passing on 68000/040 and a 68020 setup failure on opcode48F9. A source-matched CPU correction passes 18 focused/633 selected checks; a separately rebound 68020 run now passes its six invocations with identical retained HUNK/map and only the CPU DLL/PDB changed. This does not rewrite the failed historical matrix or establish a homogeneous new three-CPU checkpoint. [Synthetic retirement](task-retirement-qualification.md) passes 50 new cases and 34 controls, but DOS cancellation, native quiescence and nonlocal Exit remain open. Read-only RET03.1 inspection passes 31/337 portable and 26/63 host checks; the independent [FreeMem range query](free-range-inspection-qualification.md) passes 95 cases after strict-word-read alignment repair. Neither enables cleanup. A separate Signal guard replay fixes the sampled boot list corruption and reaches InitialCLI, but startup waits in C:AddDataTypes; R07/R08 still require application readiness and real streams. See [owner notes](runcommand-input-context-implementation-notes.md), [retirement steps](runcommand-retirement-owner-plan.md) and [reference execution](reference-execution-paths.md). |
| CC02.API15 | Native ReadArgs stores byte-backed option flags through their field address, but by-value field access loads a longword at that address, losing modifier bits. | Typed instance-field load/store and promotion are repaired without changing offsets, aggregate transport or the source enum. The same 36 native cases fail before/pass after, with 12 additional boundary cases. The resulting complete 44-case native capture exposed 43 parser mismatches; a separate one-file DosCore correction now passes all 44 exact original/generated pairs, with installation and cleanup. See [compiler qualification](compiler-qualification.md) and [parser qualification](readargs-parser-qualification.md). This closes that explicit-source corpus, not normal command-input behavior. |
| CC02.API16 | A directly constructed transparent scalar was represented by its temporary address. Assigning `new APTR(...)` to DOSLibraryBase stored that address instead of the pointer payload. | [Constructed scalar correction](constructed-scalar-qualification.md) preserves the real constructor and loads its completed four-byte result. A source-matched comparison moves 84 failures/205 controls to 289 passes; five PE/PDB pairs and 192 source documents match. The unchanged original failed Exe2Arc probe then passes all 77 bounded 68000 component entries. No command or shipping gate is closed. |
| CC02.API17 | Public Close used the opposite of the V36+ BOOL convention. Native buffered-write failures could also be swallowed before provider End. | The [BOOL correction](close-boolean-qualification.md) passes 30/294 portable and five/37 host checks. Its actual native ACTION_WRITE=-1/DiskFull failure is retained. A separate [checked-write correction](close-write-error-qualification.md) preserves the first flush error through cleanup and retains only the unwritten suffix for retry: 45 native cases across 68000/020/040, 323 portable checks, 64 host checks and 14 existing installed/provider checks pass. These source-matched checkpoints remain distinct; raw Write/FPutC errors, original failure precedence and full filesystem equivalence remain open. |

## CC02.API09 correction

The instruction executor deliberately overwrites the original Amiga ABI's
D0/D1/A0/A1 scratch registers at every library gateway. It exposed a pre-existing
compiler defect: `CilMachineIrBuilder.ClobbersFor` described only argument,
return, base, cache, and exception-status registers. The post-emission external
call annotation repeated the incomplete mask. Disabling peephole optimization
did not correct the lost values.

`M68kExternalCallConvention` now has an optional init property:

```csharp
public IReadOnlyList<M68kRegister>? ClobberedRegisters { get; init; }
```

The existing positional constructor and Deconstruct signatures are retained.
NULL means no additional registers, so generic platform resolvers keep their
existing contract. The Amiga resolver supplies a read-only D0/D1/A0/A1 list for
every supported library-base policy and Amiga indirect calls. Both machine IR
clobbers and emitted instruction definitions consume that list in addition to
their existing masks. Invalid non-D0-D7/A0-A6 values are rejected as metadata.
No allocator rewrite, new SDK declarations, or weakened gateway clobbers were
needed.

Source scope in `D:/Koodit/GIT/CopperSharp68k`:
`Compiler/PublicApi.cs`, `Targets.Amiga/AmigaCompilerPlatform.cs`,
`Compiler/Backend/CilMachineIrBuilder.cs`,
`Compiler/Backend/M68kCodeGenerator.Allocated.cs`,
`Compiler/Metadata/CompilationModule.cs`, and the new isolated
`Compiler.Tests/AmigaExternalCallClobberTests.cs`. Existing dirty sibling work
was preserved; no commit was created.

The 24 native regression cases failed before correction: GetMsg received
`0xd1d1d1d1` instead of the port, and ReplyMsg received `0xa0a0a0a0` instead of
the message. After correction, **33 focused tests passed**, comprising those
24 instruction executions (two scenarios, three CPUs, default/disabled peephole,
application/resident profiles) and nine contract checks. A related **549-test
subset** also passed, including allocation, instruction dataflow, external
conventions, resident profiles, platform bases, and register ABIs. The full
compiler suite was not run. The Release CLI rebuilt with zero warnings/errors.

| Fixed CLI dependency | SHA-256 |
| --- | --- |
| `CopperSharp.Compiler.Cli.dll` | `e58a6083f914c6b1c869d3946199aea5ac12967b2e166673a790b9119c28e12a` |
| `CopperSharp.Compiler.dll` | `67a5616281b7f9de3bfa9b84ab11523a8c30bb7ec2b8b36f6dbf82b6c48c692f` |
| `CopperSharp.Targets.Amiga.dll` | `cd094de9c91346d733411c994c2d8d9ce72e71882e878e2fbe39e9817b6c7533` |

These files are in `Compiler.Cli/bin/Release/net10.0`. The correction concerns
library call boundaries; the original image-entry permission to change every
register except SP remains intact. This table records the API09 build before
the separately authorized scalar-argument-home correction below; it is not the
final Foundation compiler binding.

### Subsequent scalar argument-home correction

The numeric component's native trial exposed another existing compiler defect:
after an address-exposed scalar parameter was mutated through `ref`, subsequent
`ldarg` lowering could still copy its initial SSA/register value. The coordinating
compiler owner changed only the 32-bit scalar ArgumentHome branch in
`Compiler/Backend/CilMachineIrBuilder.cs` to emit an `ArgumentLoad`, preserving
the API09 call masks and existing frame layout. The new isolated regression is
`Compiler.Tests/ArgumentHomeMutationTests.cs`. That owner reported 18 cases
failing before and passing after the fix, with 99 focused/related tests passing
and a clean Release CLI build. These compiler tests were not independently
rerun by the native-boundary owner. Narrow/wide address-exposed homes remain
outside that correction's qualification.

The final Foundation run below uses the corrected compiler backend SHA-256
`04760baef99aba282d7703c6da363c913ca733f36af8623e2dc7f7fb47286f95`.
The bound `CilMachineIrBuilder.cs` SHA-256 is
`ed2e931697b933af53d3adab63a6d5f14ef7790e7b1516e97b2cbeaa87014d72`.
The CLI and Amiga target hashes remain as shown above. No SDK, command adapter,
or emulator source change was used to work around either compiler defect.

## Native command qualification recorded

The following is the historical startup/argument checkpoint. The expanded
Foundation run `5b2854ee3ef04f71ad6faab7d95add17` now passes 264 invocations
across nine HUNKs, including 96 I/O cases. Classic MakeDir separately passes
114 original/generated supplied-vector comparisons. Current receipts, source
bindings and limitations are in [qualification-report.md](qualification-report.md),
[native-io-contract.md](native-io-contract.md) and the
[MakeDir contract](contracts/MakeDir.md). They do not establish actual DOS
parser or normal command-launch behavior.

Executed `pwsh -NoLogo -NoProfile -File tools/Commands/qualify_native.ps1`.
Foundation run `5062fe92a7514e2c95eb7c36f0df1658` passed **168 actual CPU invocations**:
31 startup plus 25 argument-boundary calls for each of 68000, 68020, and 68040.
Each CPU/suite executes repeated and interleaved calls on one shared loaded image.
All six HUNK artifacts reproduced byte-for-byte and contain no shared initialized
RAM/BSS or unqualified managed runtime closure.

That checkpoint's receipt is
`tests/Commands.NativeRoot/bin/Release/net10.0/qualification/5062fe92a7514e2c95eb7c36f0df1658/qualification.json`,
SHA-256 `bf5ffb271eead79f17c3d3c48001bb61b3c14c66718ed04da4ac292b0ad7684d`.
The adjacent `input-manifest.json` has SHA-256
`113c97b65e2f6092f2596818aa36ecfe3cb1828f7ff6043729937723f2c8db84`.
It records 436 source/build-setting files, 20 copied managed inputs/runtime files,
35 resolved build-input files, and 196 selected host runtime/build-tool files.
The native executor DLL hash is
`857b0055f70b6f7ee4a94f1d3513864bf881447bfdd4068a92ba09fe78950bbd`;
the Copper68k DLL hash is
`8046d9a2083c198647a02d9146b735629890d9a2e14bc313968a3b48139a2cd5`.

| Startup HUNK CPU | Bytes | Invocations | SHA-256 |
| --- | ---: | ---: | --- |
| 68000 | 2176 | 31 | `b10d8198e157031ad4ead4ddc1cbb5a64da3f2f12da82bbc3fcec2b7e04bc7b0` |
| 68020 | 2176 | 31 | `b2ff2e497a76a52068c99f833b2795e6da60dd3878275145d08c3a24665d65ad` |
| 68040 | 2176 | 31 | `b2ff2e497a76a52068c99f833b2795e6da60dd3878275145d08c3a24665d65ad` |

[argument-ownership.md](argument-ownership.md) records the exact helper API,
25-case boundary inventory, HUNK hashes, and lifetime evidence. Interleaved calls
use 4096-byte and 16384-byte stacks; observed stack-write extents are 120 bytes
for startup and 156 bytes for the boundary probe. No minimum stack is qualified.

Before the final run, independent review identified and corrected two harness
false-pass paths: a DOS call after its library was closed could satisfy final
balanced counts, and a missing WaitPort could satisfy an IndexOf comparison.
DOS calls now require a live successful lease; CloseLibrary accepts its owned
lease only once after argument allocations are released. WB messages require
exactly one WaitPort/GetMsg, explicit nonnegative ordering, and one reply of the
consumed startup message under Forbid. The bus additionally rejects native reads
from freed/guarded/foreign allocations. Earlier combined runs
`71db809adfd14ad5b07a7a40b444e3e3` and `f46d48496f14444aacc76471eae8d902`
are preserved as historical receipts, not qualification of later source changes.

The final pipeline creates a running attempt receipt before input resolution
or bootstrap, records stage/exit/log failures, forces all three bootstrap builds,
snapshots raw sources and declared managed dependency closures, and compiles and
executes the copied binaries. It revalidates source inventories/bytes, original
and copied binaries, restore metadata, and selected runtime identities before
and after tool use and at publication. Every execution report identifies its
actually loaded Copper68k/executor DLLs; the script validates their paths/hashes
and the pinned .NET 10.0.11 runtime version. All **43 stages and 45 input checks**
passed. The script also rejects restored project references outside its bound
source projects and binds generated artifact evidence. All success publication
occurs only after the six artifacts and their inputs pass final validation.

`tests/Commands.NativeExecution/QualificationScriptRegressionTests.ps1` passed
six failures-as-expected checks: bootstrap failure, modified source bytes,
added source, modified snapshot binary, modified live binary, and added snapshot
DLL. Disposable bootstrap projects produce durable failed receipts and leave
the prior successful latest pointer unchanged. The exact command/report/hash
are recorded in [argument-ownership.md](argument-ownership.md).

This is generated-instruction/vector-fixture evidence. It grants no original
OS/parser, shipping, P-bit, resident registry, minimum stack, or packaging approval.
The script fails if any CPU/suite or input check fails.
This Foundation receipt binds qualifier revision
`4eba03c9d4527745e98464f4d38783c343ee0e3137bb3153da15e979cc882ac3`;
later script/source edits need another run. Unrelated documents and component
projects are intentionally excluded. The recorded host/runtime identities are
not a hermetic operating-system/environment guarantee, and a valid writable
output location is required to persist a failure receipt.

## Native harness obligations retained

Reuse the public Copper68k core (`M68kCoreFactory`, `IM68kBus`), without emulator
changes. `tests/MuiMaster.NativeExecution/Program.cs` provides a working HUNK
load/relocation and instruction-loop example, but it assumes a single code
hunk, MC68000, a fixed return value of 42, and no OS gateway traps. It is not
already a command harness.

The initial audit identified these obligations for the native harness. They also
apply when extending it to more commands or HUNK layouts; the bounded coverage
completed so far is recorded above:

1. Load and relocate the exact generated HUNK, including every emitted code,
   data, and BSS hunk; reject unsupported records rather than silently dropping
   them. The current fixture accepts exactly one code/constant HUNK and rejects
   all other layouts until their sharing rules are qualified. Check the loader
   entry and separate code/constant spans from stack,
   arguments, guest OS state, and explicit allocations.
2. Execute actual instructions with a bounded count on the selected 68000,
   68020, or 68040 core. Supply D0 length, A0 text, a deliberately non-DOS incoming
   A6, valid ExecBase at address 4, a stack return sentinel, and Process/CLI state.
3. Install test gateways for precisely the public Exec/DOS vectors consumed by
   the probe. Validate register arguments, track opened library handles and
   allocation ownership, capture byte output, and fail unknown vectors. These
   host-side gateways are test instrumentation, never production dependencies.
4. Verify SP/return integrity, D0 result, argument lifetime, configured stack
   sizes and guard zones. Record observed usage without claiming a minimum.
   Do not demand all original image-entry registers survive; do
   separately check exported/callback register contracts.
5. Execute the same loaded code repeatedly, then interleave two CPU contexts on
   the **same code image and bus** with independent stacks, arguments, streams,
   process error state, and acquired bases. Include failure-then-success and
   overlapping failure/success. Compare shared image bytes and trap every write
   into shared code/constant ranges.
6. Inject missing DOS, explicit allocation failure, short output, and invalid
   input at meaningful boundaries; verify cleanup and no false success. Force
   compiler heap-context allocation separately where that mode is to be used.
7. Emit CPU, image hash, compiler/options/dependencies, execution counts,
   resource totals, and uncovered cases. A successful mocked-vector execution
   is native ABI evidence, **not** a claim of actual Kickstart/DOS conformance.

The compiler's existing resident tests at
`Compiler.Tests/CompilerExecutionTests.cs:3236-3468` demonstrate interleaved
same-image execution, stack/heap contexts, allocation failure, export handling,
and startup arguments. They were initially read only; ResidentProfile cases were
subsequently rerun in the 549-test CC02.API09 subset. CopperOS command artifacts
were tested independently as recorded above.

Real Kickstart 3.1 load/RunCommand/ReadArgs execution, CopperStart integration,
Workbench-message lifecycle, complete resource accounting, installed protection
metadata, and original MorphOS differential comparisons remain separate gates.
None is marked complete by the baseline test or this source audit.
