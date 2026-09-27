# Reference execution paths and ReadArgs component differential

Recorded: 2026-08-30. Supports CC03/CC04/CC07/CC09 and final qualification.
This includes a tooling audit, licensed ReadArgs component captures and bounded
original-DOS command calls. It is not full-boot or complete command parity.

**There is no verified turnkey path here for complete original Kickstart 3.1
and native CopperStart boot, command execution, and exact stream capture.**
The separate 2026-09-26 [normal-CLI guest probe](reference-captures/workbench31-guest-command-probe-20260926.md)
now captures four original Workbench Version invocations on verified disposable
ADF copies using the original startup CLI, ReadArgs, SystemTagList and RAM
handler. It records exact output, returns 0/0/5/20, and stable guest-owned
completion data without host OS services or PC/vector injection. This resolves
the clean Workbench command/output path for these cases. Child Result2
propagation, separate stderr, replacement differential, lifecycle and native
CopperStart/MorphOS execution remain open. Earlier component results below
retain their original boundaries and are not relabelled as this clean route.
The latest strictly bound component run **passes all 44 original/generated**
DOS `ReadArgs` comparisons with an explicit `RDA_Source`. The generated installer
succeeds twice, returning the same owned library without repeating
initialization. The parser-owner follow-up retains the earlier memory,
discovery, 25-result pointer failure and 43-pair semantic failure receipts.
A separate original-DOS R04 test passes public NIL: stream acquisition,
selection, restoration and cleanup. The exact R05 LF-only and R06 `alpha beta`
plus LF observer cases now pass through original `RunCommand` and normal
default-input ReadArgs. The earlier 25-byte observer variant remains separate.
Separately, original MakeDir 37.2 and the existing generated 68000 probe match
on three recorded input pairs through original RunCommand: **three original
and three generated command calls, three comparisons** across two receipts.
These include exact A01/A03 plus a separate seven-byte malformed-input variant.
The first R07 stream prerequisite fails before opening RAM:. A separate
read-only capture confirms that the direct component bootstrap publishes
dos.library and its RootNode while `rn_Info` is still zero. Both failed
readiness receipts retain zero credited commands; they do not invalidate or
extend the earlier bounded command/input results.
A separate passive ROM boot with the unchanged Workbench disk now reaches a
valid original DOS RootNode/DosInfo and native vectors within the existing
boot cap. Its current Process still has no CLI/input/output, so this is root
publication readiness, not usable streams or handler qualification.
The separate follow-up keeps the original 32-chunk cap and fails to observe
a current CLI/input/output context; that missing-context result remains open.
A new test from the initialized root checkpoint passes original public DOS
list locking, eight-entry enumeration and unlocking without requiring a CLI.
RAM is registered but has no handler port in that observation. The checkpoint
appears to belong to the DF0 filesystem handler; no stream is opened there.
A separate public task-list capture confirms the DF0 name and finds an
Initial CLI on the first ready list. Its later 23 checkpoints expose
inconsistent ready/wait links. That observation test passes as a capture;
the scheduler-list validation fails, and child creation is deferred.
Complete command parity, nonempty borrowed-
stream restoration, filesystem, Shell and full boot remain unqualified.

## Local sources and what they establish

The 2026-09-26 [passive slow-RAM capture](reference-captures/workbench31-clean-runner-slowram-readiness-20260926.md)
adds a clean hardware route independent of the MedPlayer component fixtures.
It observes native Exec/DOS, startup CLI command names and the Workbench
Process using unchanged ROM/media. Read-only memory visibility is now adequate
for those structures. The [authored guest probe](reference-captures/workbench31-guest-command-probe-20260926.md)
extends that same frozen runner with copied-media startup invocation and a
passively read public-port result record. Four complete original-command
captures now exist; sampled command names and arbitrary Process Result2 fields
are still not substitute command receipts.

| Source or runner | Observed capability | Limit that must remain explicit |
| --- | --- | --- |
| [CopperScreen Lightweight and normal-CLI guest probe](reference-captures/workbench31-guest-command-probe-20260926.md) | Supported product profile remains PAL OCS A500/Kickstart 1.3. Exploratory 40.63 ROM/Workbench 40.42 execution now includes four original Version calls through normal startup and SystemTagList, with exact guest RAM output and stable result records. Each 2,400-frame capture binds unchanged runtime/media inputs. The earlier unchanged-media replay and read-only SlowRam regression remain separate evidence. | No full-boot certification, replacement comparison, child Result2 guarantee, separate stderr, fault injection or lifecycle admission. The framebuffer remains garbled, but this route does not use visual interaction. The guest disk is write-protected; output uses the original RAM handler. |
| [MedPlayer headless CLI](D:/Koodit/GIT/MedPlayer/CopperScreen.Headless.Cli/Program.cs) and [runner](D:/Koodit/GIT/MedPlayer/CopperScreen.Headless/CopperScreenTestRunner.cs:23) | Launch a HUNK with arguments/stack/CPU selection; bound frames, cycles and instructions; report D0 and CPU/diagnostic snapshots. An existing CLI binary's `--help` was executed successfully. | Constructor and LaunchHunk call `StartApplicationSession`, which installs host services. There is no ROM/disk option or exact stdout/stderr capture option. This is not an original Kickstart runner. |
| [Licensed DOS differential](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosDifferentialTests.cs) | Initializes original ROM dos.library 40.3, checks 154 original DOS vector slots for absence of host gateways, captures selected public API results and immediate IoErr. | Direct InitResident intentionally reaches a bounded early-startup wait without normal filesystem/command-registry startup. Current cases do not execute ReadArgs, RunCommand, Eval or Which. |
| [New ReadArgs differential](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosReadArgsDifferentialTests.cs) and [44-case corpus](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/DosReadArgsDifferentialCases.cs) | The unchanged fixture now passes 44 exact original/generated comparisons through native vectors without DOS host gateways; two native installations, post-case vector/HUNK-byte checks and expunge complete. | A finite explicit-source corpus, not normal input delivery, prompting, command execution or full parser qualification. Earlier 25-result and 43-pair failures remain retained. No copied provider was added to CopperOS. |
| [Original command-input observer](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosCommandInputObserverTests.cs), [exact-case follow-up](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosExactCommandInputObserverTests.cs) and [authored native program](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/DosCommandInputObserverProgram.cs) | Original RunCommand launches three authored observer cases across two receipts; startup D0/A0 and GetArgStr agree, and ReadArgs NAME/M with D3=NULL parses the real default input. Typed values are copied before original FreeArgs; borrowed state and resources restore. | Authored probe commands, not Workbench/MorphOS distribution commands or generated CopperOS commands. NIL: output is not captured. Inherits the component fixture's SR=0x2000; normal user-mode Shell startup, full boot and nonempty borrowed streams remain unqualified. |
| [R07 RAM-input prerequisite](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosRamInputReadinessTests.cs) and [read-only root capture](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosBootstrapRootReadinessTests.cs) | Strictly retained failures: original LockDosList enters an Exec wait before any file opens; a fresh passive memory capture confirms `RootNode.rn_Info == 0` after direct InitResident. | No usable DosList, RAM handler, seekable stream or R07 restoration is qualified. No commands, ReadArgs or RunCommand are called by either readiness test. |
| [Passive ROM boot readiness](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosPassiveBootReadinessTests.cs) | With the unchanged write-protected Workbench disk, DOS publishes a mapped RootNode/DosInfo by chunk nine and all 154 DOS vectors remain native. Six inspected provider source files are tied to the executed frozen modules by matching portable PDB identities/checksums. | Current Process has no CLI or streams. No explicit DOS/command calls, LockDosList or handler tests; zero command coverage credited. Existing Exec/device takeover is declared, and full boot is not qualified. |
| [Initialized DOS list readiness](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosInitializedListReadinessTests.cs) | Thirty-two original public DOS calls return through the normal boot execution boundary at the initialized checkpoint. LockDosList, nine NextDosEntry calls and UnLockDosList copy eight nodes while locked, then restore borrowed state. | Public list readiness only. RAM has no handler port yet; the DF0/volume port matches the current Process message port. No file/stream opens or command calls, and no original-handler, R07 or full-boot qualification. |
| [Passive task topology](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosBootTaskTopologyTests.cs) | Captures public current/ready/wait task names, states and Process fields without changing the CPU or guest structures. The first current Process is named DF0; Initial CLI is separately on the ready list. | Observation passes, but ready/wait validation fails at all 23 later checkpoints. A ready-chain walk reaches the other list's tail sentinel. No writer attribution, child creation, scheduler qualification or command coverage follows from this capture. |
| [Licensed Exec bootstrap and HUNK differential](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomExecDifferentialTests.cs:153) | Boots the supplied ROM until Exec is published; existing HUNK loader/symbol helper can execute a separately generated component in guest memory. | Component context and direct vector invocations replace PC/A7. This is not a normal CLI process/command lifecycle. |
| [Native DOS installed-vector tests](D:/Koodit/GIT/CopperStart/tests/CopperStart.Exec.Tests/DosInstalledVectorCoverageTests.cs:360) | Executes generated MC68000 DOS ReadArgs/FreeArgs through installed vectors, using a caller-supplied CSource and scratch buffer. | Existing case is a small VALUE/A example. Exec, task state and provider fixtures are test infrastructure; they are not a booted native OS. |
| [Native DOS semantic tests](D:/Koodit/GIT/CopperStart/tests/CopperStart.Exec.Tests/DosInstalledClassicSemanticCoverageTests.cs:109) | Generated DOS library runs on NativeParityBus with controlled tasks, streams and packet providers. | The inspected RunCommand case is invalid-input coverage. Private memory-input handles and fabricated task fields must not be used to claim original OS execution. |
| [MedPlayer boot controller](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Emulator/AmigaBoot.cs:1351) | `StartKickstartRomBoot(disk)` loads a ROM and starts its reset vector without the initial host shim. | For Kickstart31 it subsequently enables host Exec/device takeover. Merely choosing a ROM file does not establish an unmodified full reference environment. |
| [CopperStart native artifacts](D:/Koodit/GIT/CopperStart/Docs/NativeArtifacts.md:17) | Separate production/component HUNKs and assembly qualification; physical allocator self-test ROMs. | The documented `CopperStart-System.rom` and `CopperStart-Reference.rom` are not complete DOS/Shell boot ROMs. |
| Older `D:/Koodit/GIT/CopperMod` tree | Older `AmigaMachine` and ROM boot source without the MedPlayer takeover path found in this audit. | `git rev-parse HEAD` failed for this tree; it is not an interchangeable current checkout. No current build/boot/command receipt was established. CopperMod.Tools is chiefly media rendering, not a DOS command CLI. |

Source checkout identifiers observed: CopperStart
`d30497b315e819ce5cd3881bfe975c818088b642`; MedPlayer
`cf4da5dd1c77aa925959319e13940960ad5dad5c`. These identify HEAD, not the absence
of local changes or the identity of existing binaries. Sibling AGENTS.md files
were read. Repowise tools/CLI were unavailable; bounded local searches and
reads were used. The initial fixture implementation added only the two named
MedPlayer test files. Later bounded fixes at the actual emulator and DOS
owners, and their separate regression evidence, are documented below.

## Existing reference and executable checks

The local licensed ROM path
`D:/TestData/ROM/kickstart-3.1-a500.rom` exists and its SHA256 matches the
existing test's pinned 40.63 identity:
`8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`.
Only its hash was recorded; no ROM bytes were emitted or copied. Original
Workbench command/media identities are in
[command-inventory.json](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/command-inventory.json).

These environment variables were unset during the audit:
`COPPER_AMIGA_KICKSTART_ROM`, `COPPER_AMIGA_KICKSTART_VERSION`, and
`COPPERSTART_BOOTSTRAP_ROM`. Existing licensed tests return early when both
ROM settings are absent, so an unconfigured green test count is not reference
execution. A requested reference run must instead fail when its inputs or
selected test cases are missing.

This existing read-only command was verified:

```powershell
dotnet 'D:/Koodit/GIT/MedPlayer/CopperScreen.Headless.Cli/bin/CodexRegression/net10.0/CopperScreen.Headless.Cli.dll' --help
```

For a private ABI probe, this is the CLI's supported invocation shape, not a
record of executing that probe:

```powershell
dotnet 'D:/Koodit/GIT/MedPlayer/CopperScreen.Headless.Cli/bin/CodexRegression/net10.0/CopperScreen.Headless.Cli.dll' --hunk $probeHunk --arguments '7 + 5' --profile A500PalExpanded --cpu AccurateM68000 --stack-size 16384 --max-frames 60 --max-instructions 1000000 --json
```

`$probeHunk` must identify an explicitly built, hash-bound private probe. The
run would be classified as a HUNK/application-session integration run. D0 is
the HUNK's return value, while the CLI exit code distinguishes normal return,
limit/fault and expectation mismatch. Neither is automatically a captured
command's IoErr or stdout.

Read-only test discovery against the existing
`MedPlayer/CopperMod.Amiga.Tests/bin/ExecDiff/net10.0/CopperMod.Amiga.Tests.dll`
reported **no tests matching `Category=LicensedDosDifferential`**. Do not run
that old artifact and interpret a zero-test result as success. Rebuild the
selected test project with current inputs before running the source tests;
verify discovery and a positive expected case count. The subsequent ReadArgs
test was built and executed as recorded below; the old ExecDiff artifact was
not used for that capture.

For a fresh matching MedPlayer test assembly, the existing source-level gate
uses this configuration and filter:

```powershell
$env:COPPER_AMIGA_KICKSTART_ROM = 'D:/TestData/ROM/kickstart-3.1-a500.rom'
$env:COPPER_AMIGA_KICKSTART_VERSION = '3.1'
dotnet test 'D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/CopperMod.Amiga.Tests.csproj' -c Release --no-build --no-restore --filter 'Category=LicensedDosDifferential' --logger 'console;verbosity=normal' --results-directory $privateResults
```

Use a dedicated child process and a disposable results directory, restore any
inherited environment settings, and require the native identity/trace receipts.
This is the existing limited DOS-family gate, not the new ReadArgs gate.
Do not execute the GUI as a headless workaround: the current MedPlayer
[Program.Main](D:/Koodit/GIT/MedPlayer/CopperScreen/Program.cs:17) requests
RealTime process priority and launches Avalonia rather than exposing a bounded
CLI boot/capture contract.

## Reference contamination boundary

MedPlayer `StartKickstartRomBootCore` initially avoids the host shim, but
[TryActivateKickstartRomExecServices](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Emulator/AmigaBoot.cs:1755)
later invokes
[ActivateRomExec](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Emulator/CopperStart/CopperStartRuntime.cs:22),
which installs a Kickstart Exec overlay. The boot path can also install
trackdisk, timer, console, input, keyboard, clipboard and utility services.
The existing A500 Kickstart31 profile explicitly describes Host Exec.

Original DOS vectors can therefore remain native while their supporting
environment contains replacement services. A component result must state this
boundary. Require no gateway at each DOS slot or its resolved target, verify
the executed target belongs to the selected original/generated component, and
record supporting provider identities. Do not describe the entire machine as
unmodified merely because ReadArgs itself is native. Do not disable takeover
by lying about the ROM version.

The application-session DOS provider is the actual portable CopperStart core
through [DosServices](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Emulator/CopperStart/Dos/DosServices.cs:112),
but it executes as host C# behind gateways, not as the generated DOS HUNK.
Its diagnostic write previews are capped, and its generic dispatcher has
fallback/default branches. Those previews are not lossless stdout. A command
qualification run must reject fallback diagnostics and label host/core versus
generated/native execution accurately.

## Implemented explicit-source ReadArgs gate

The two test files below are implemented at the existing emulator/test owner.
The subsequent memory prerequisite diagnosis does not authorize provider
changes. Do not copy DOS/Exec/Shell
implementations, ROMs, shipping HUNKs, or test gateways into CopperOS.

Implemented test-side files:

| Path | Responsibility |
| --- | --- |
| `D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosReadArgsDifferentialTests.cs` | Another partial of the existing `KickstartRomLayersDifferentialTests`; reuse licensed Exec bootstrap, InitializeLicensedDos, Invoke/Allocate/Free, HUNK loader and symbol helpers. Own the reference request validation, original and generated DOS calls, trace comparison and receipts. |
| `D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/DosReadArgsDifferentialCases.cs` | Bounded test-only case/observation records and byte corpora. No command implementation, precomputed reference answers, ROM bytes or private OS implementation. |

The implemented generated side loads the hash-validated
`CopperStart-DOS-Unified-68000.hunk` after original capture, resolves its actual
`copperstart.dos.install-system` export, and requires a distinct installed DOS
base. Its intended context retains the real boot-created Exec/task state;
no private DOS or Process fields are fabricated. The existing loader supplies
the actual segment-list BPTR in A0; A6 is ExecBase. Every ReadArgs call must
reach the return sentinel through its selected ROM/HUNK code. Candidate calls
that enter original ReadArgs are rejected. Required HUNK, compatibility,
artifact-manifest, nine managed input hashes and live source-closure checks
must pass before execution. The test neither rebuilds nor substitutes a
managed parser during a comparison.

If that generated installer cannot yet run against the selected component
Exec context, stop that subcase with explicit evidence. The bounded fallback
could be a separately authorized `DosReadArgsNativeDifferentialTests.cs` under
`CopperStart/tests/CopperStart.Exec.Tests/`, reusing the existing native
installed-DOS fixture and consuming hash-bound original observations. That
retains real generated ReadArgs but must label its Exec/task provider as a
fixture. Factoring a small reusable test context from the existing installed
test is preferable to copying its private DOS setup into CopperOS. This fallback
does not authorize a shipping OS change or turn it into a complete native boot.

Suggested test-only API boundaries:

```text
RequireReferenceInputs() -> validated ROM + generated HUNK/manifest identity
CaptureReadArgs(context, dosBase, case) -> copied, bounded observation
CompareReadArgsObservations(original, generated) -> exact semantic differences
```

The request gate must be explicit. A requested licensed run with absent ROM,
wrong hash/version, absent HUNK/sidecar, no discovered cases, timeout, or a
fallback provider fails. An ordinary test suite that does not request the
licensed corpus should report it as excluded/skipped, never a successful
executed case. The project uses xUnit 2.5.3, so do not assume newer dynamic-skip
APIs exist. Use a test-only opt-in discovery/filter arrangement with a strict
runner preflight, or document the required fixture for this separate gate.

The primary API authority is original NDK
`NDK_3.1/INCLUDES&LIBS/INCLUDE_H/DOS/RDARGS.H` in
[AmigaDeveloperCD.iso](D:/TestData/AmigaDeveloperCD.iso), SHA256
`83e9e1385720c2dcb7f258c467a1d501cc0d3f8796567a32d8da9588e1805d32`.
A non-null RDA_Source.CS_Buffer selects the supplied string instead of
buffered Input. Initialize CS_Length/CS_CurChr and RDA_DAList correctly; retain
the original newline/source semantics. This avoids needing an invented DOS
input handle for the first parser cases.

For each case:

1. Obtain correctly aligned guest storage through the context's documented
   allocation boundary. Allocate/clear the RDArgs, result LONG array, template,
   input and any optional scratch; protect surrounding canaries. Use fresh
   state or correctly reset RDA_Buffer and allocation tracking each time.
2. Initialize the complete result array, RDA_Source and flags. Start with
   explicit newline-terminated byte input and `RDAF_NOPROMPT` so this component
   gate cannot accidentally request console input. Separate this from normal
   flags and later interactive `?` coverage.
3. Verify original DOS version and native vector/gateway identity, then call
   ReadArgs using D1=template, D2=result array, D3=RDArgs. Require the explicit
   return sentinel; budget exhaustion is a failure, not a parser result.
4. Capture success/null result and immediate IoErr before any cleanup API.
   Copy strings, `/N` pointed LONGs, scalar `/S` values and each terminated `/M`
   list while RDArgs storage is valid. Bound every read and reject unmapped,
   misaligned or unterminated results. Compare pointer meaning/contents rather
   than unrelated allocation addresses.
5. Follow FreeArgs and caller-allocated RDArgs ownership rules. Record cleanup
   errors/IoErr separately from the command result; verify canaries, balanced
   allocations and unchanged code/vector tables. Repeat successes and failures
   so stale argument state is observable.

Minimum first corpus:

- Required/optional string, `/K`, scalar `/S`, `/N` including negative and
  invalid/overflow input, `/M` including empty and multiple items, and `/M`
  backfilling a later required argument.
- Quoted spaces, explicitly empty string, quoted keyword data, literal `*`,
  escaped quote, `*N` and `*E` byte behavior; missing required item, unknown
  keyword, excess item and unterminated quote.
- Newline-terminated input, trailing whitespace, empty line, explicit source
  ending without LF, and repeated calls with reset RDArgs. Record no-LF
  behavior separately rather than assuming all releases accept it.
- Original Eval template with `7 + 5` and original Which template with an ALL
  switch: parse the arguments only. This does not execute either command.
- `?` with NOPROMPT as its own parser case. Real `?` continuation, terminal
  EOF and Ctrl-C remain stream/CLI integration work, not fake-buffer parity.

Save case IDs, exact byte inputs, flag values, original/generated component
hashes, provider identities, normalized results, raw return/IoErr, cleanup
results and pass/fail reasons. Do not save absolute ROM pointers, ROM code
bytes or unrestricted memory dumps as portable fixtures.

## Earlier executed results and prerequisite failures

The test project was rebuilt with `-p:BuildProjectReferences=false`: zero
warnings and errors. The existing emulator dependency was not rebuilt; its
executed DLL SHA256 is
`6C922CDB7E6EBD2A84D4547746426F5389915AFE499D3A2E3F1FC856CFE66358`.
VSTest discovery finds exactly one
`ExplicitSourceReadArgsMatchesLicensedV4063` test, containing 44 distinct
cases. An intentionally unconfigured run failed with **1 failed, 0 passed,
0 skipped**, explicitly naming the required ROM settings. There is no silent
return/skip path for a selected ReadArgs gate.

The first configured run captured **44 original cases: 32 successful parses
and 12 error results**. All selected native ReadArgs calls returned within
their instruction budget; the original DOS vector/gateway checks, copied
results, register preservation and surrounding canaries passed. The original
context was A500 PAL OCS, AccurateM68000, live Agnus DMA, 512 KiB chip RAM and
512 KiB pseudo-fast RAM. Its Exec/device overlay boundary still applies.

The sanitized [original-only receipt](D:/TestData/CopperOSCommands/ReadArgsDifferential/original-readargs-v4063-512k.json)
contains exact input bytes, templates, flags, initial/final cursor, copied
results, IoErr and cleanup IoErr. It records
`originalCasesExecuted=44`, `generatedCasesExecuted=0`,
`comparisonPassed=false`, the original DOS 40.3/ROM 40.63 identities,
timestamp, test/emulator DLL hashes, case-source hash and compiled-corpus
hash. It includes no ROM code bytes or absolute guest pointers. Receipt
SHA256:
`5AEB007A1CF6ACB06FCDEC1F6CC6BBA20498B46157AB1AA747AFF7B949C28E90`.
The compiled input-corpus SHA256 is
`88DB8D1B62D47507FDBC19C0F9403C8C337FC9823CD7504A6D9E6BA58BC6884E`.

Observed behavior in this bounded profile includes:

| Case | Original DOS observation |
| --- | --- |
| Successful parse and FreeArgs | IoErr retains the deliberately set `0x13572468`; it is not automatically cleared. |
| Present/absent `/S` | Exact scalar `FFFFFFFF` / `00000000`. |
| Missing required argument / keyword value / bad number | NULL result and IoErr 116 / 117 / 115, respectively. |
| Extra item or source ending without the final LF | NULL result and IoErr 118. A LF beyond CS_Length does not count. |
| Unterminated quote | NULL result and IoErr 120. |
| Decimal `2147483648` | Successful `/N` parse, pointed LONG bits `80000000`. This is an observation, not an assumed overflow error. |
| Quoted `**`, `*"`, `*N`, `*E` | Copied byte values `2A`, `22`, `0A`, `1B` respectively. |
| `/M` followed by a required destination | Last input item fills the destination; preceding items form the terminated list. |
| Original Eval/Which templates | Their arguments parse in this component context. Neither command was executed. |

The owning CopperStart build regenerated the production image and receipts
before execution. The verified MC68000 HUNK is 867,728 bytes, SHA256
`2A008CC1EF4DCFC5896A3D958A1454B5D86F23F0D7E833019856257D2F359286`.
Its **single 626,536-byte load segment** needs a contiguous allocation larger
than either default 512 KiB bank. The original capture run therefore failed
at native Exec allocation while loading the candidate; see the
[allocation-failure TRX](D:/TestData/CopperOSCommands/ReadArgsDifferential/readargs-512k-allocation-failure.trx).

The next test-only configuration used the public
`MachineOptions.WithRealFastRam(2 * 1024 * 1024)`. It retained the same ROM,
CPU, native DOS checks and existing bootstrap provider. No MemHeader or DOS
state was synthesized. Although fast RAM was mapped, Exec takeover was
Active and ThisTask existed, public `AvailMem(PUBLIC | LARGEST)` returned the
impossible size **4,278,189,664 bytes**. Original DOS initialization then did
not publish its library. The test now checks documented MemHeader/MemChunk
bounds while allowing up to **32 x 250,000 ROM instructions** for readiness.
The latest [strict failed gate](D:/TestData/CopperOSCommands/ReadArgsDifferential/readargs-native-memory-readiness.trx)
still fails after that bound: header span 2,097,120 bytes, declared free
2,083,912 bytes, attributes 1541, but a linked free chunk claims
4,278,189,664 bytes. The result is **1 failed, 0 passed, 0 skipped**; no
generated parser case ran. The read-only diagnosis below identified an Exec
host-gateway placement defect. A bounded correction is being validated at
the existing emulator owner; no parity allow-list or fabricated MemHeader
has been applied. Its owner regression and licensed allocation smoke now
pass. The next strict comparison loads the generated HUNK, but fails in
its native installer before any generated parser case; see the separate
installer diagnosis below.

### Original Exec memory diagnosis and owner correction

The `amiga-cycle-exact` workflow classified this as a Kickstart host-bridge
ownership fault, with the same A500 PAL OCS/AccurateM68000/live-DMA profile.
The private [instruction-boundary trace](D:/TestData/CopperOSCommands/ReadArgsDifferential/exec-fast2m-private-gateways.json)
uses normal ROM boot and public machine configuration, then reads actual
MemHeaders, raw fast RAM, bus-visible values and gateway registrations.
It does not change CPU state, invent memory headers or call a DOS fixture.
Trace SHA256:
`8E6E37C7160757914644C11AC260D67A07C55812296FBD3C41B8505A2A900061`.

The local NDK 3.1 `EXEC/MEMORY.H` defines `MemChunk.mc_Next` at 0 and
`mc_Bytes` at 4; `MemHeader.mh_First/mh_Lower/mh_Upper/mh_Free` are at
16/20/24/28. The inspected header member SHA256 is
`1f811ab2b567d47130cfa071435275645cb43e51bf59b800f8b235327f87513f`.
These offsets agree with the SDK and the fixture's public-field reads.

| ROM progress | Observed state |
| --- | --- |
| 1,032,000 instructions | Real fast RAM is configured at `$00200000`; its native MemHeader and first free chunk have valid bounds and sizes. |
| 1,632,641 instructions, cycle 15,579,316 | Exec takeover becomes Active. ExecBase is `$00200810`, with original negative size 822 bytes, so its table begins at `$002004DA`. The installer places private Wait/Reschedule at `$0020035A`/`$00200354`, outside that table and inside an existing free chunk. |
| 1,633,831 instructions, cycle 15,592,780 | A normal 16-byte Utility state allocation advances the free chunk to `$00200350`. Raw RAM stores `mc_Bytes=$000000D8` (216); the bus reads `$FF000000` because the private Reschedule gateway covers that field. Utility 40.1's own 296-byte negative table is valid and is not the offending overlay. |
| Subsequent AllocMem calls | The allocator consumes the masked length and writes impossible sizes into later physical chunk headers. The eventual public largest-allocation result is 4,278,189,664 bytes. |

This separates the initial gateway read overlay from the later physical RAM
corruption. It excludes the ReadArgs result array, original DOS initializer,
native HUNK loader and test sentinel setup as causes of the first failure:
none had run when the overlap appeared. No custom-chip timing adjustment,
autoconfig workaround or enlarged original library table is justified.

The provider correction is limited to
[ExecServices.cs](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Emulator/CopperStart/Exec/ExecServices.cs),
[ExecPrivateLvos.cs](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Emulator/CopperStart/Exec/ExecPrivateLvos.cs)
and the owner's gateway-model documentation. Synthetic Exec retains its
`-1206`/`-1212` aliases and existing synthetic-only memory extensions.
Original-ROM mode exposes effective private addresses `$00F08D00` and
`$00F08D08`. They are an explicit new two-slot host claim following the
owner's fixed continuation convention; there was no central reservation
allocator or previously reserved guest arena for those addresses.

Admission checks every byte against actual physical and mapped-memory/device
ownership and overlapping gateways before any ROM Exec overlay is installed.
The bus's permissive data mode deliberately answers all addresses, so actual
ownership comes from its instruction/explicit-memory maps and, when strict
data mapping is enabled, its data maps too. The correction does not change
that bus policy. It tracks registration tokens, exposes the effective
addresses, rejects collisions and removes only its own registrations.

The new
[gateway regression](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/ExecPrivateGatewayPlacementTests.cs)
first failed on the exact `216` versus `4278190080` read-through mismatch.
The separate
[licensed allocation smoke test](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomExecPrivateGatewayTests.cs)
first failed on the native 2 MiB memory list before its planned public
1 MiB AllocMem/FreeMem round trip. The
[before-fix TRX](D:/TestData/CopperOSCommands/ReadArgsDifferential/exec-private-gateways-before-fix.trx)
records **2 failed, 0 passed, 0 skipped**, SHA256
`382E318A260DC2F854C4D72A93D51F03DCEE012ED3B6F34BA6EAEB2D533B6FD0`.
The coordinated provider-only and test-only builds both completed with zero
warnings/errors, using `-p:BuildProjectReferences=false`. Recorded hashes and
last-write times for the shared DOS, Exec, SDK and SDK.Support DLLs were
unchanged across both builds. The
[post-correction TRX](D:/TestData/CopperOSCommands/ReadArgsDifferential/exec-private-gateways-after-fix.trx)
records **17 passed, 0 failed, 0 skipped**: 14 gateway ownership/callback/
collision/lifetime cases, two existing boot-policy regressions, and the
strict licensed allocation smoke. TRX SHA256:
`5FBDBE71EA7003EC4E1972E9DB8332410D647DC5AACEA913DFBE29B6877BF09B`.
The executed provider DLL SHA256 is
`9F7B35870AB98441D095FE9ED8835A52BB51043E8E775566B10B7AE33E2572D2`;
the test DLL SHA256 is
`4F623E6F8B940513FDA32B63594463D6EDA1F23D3C4324E70414AE1FBA4B2FD7`.

With the correction, native Exec memory is ready after seven 250,000-
instruction chunks. `AvailMem(PUBLIC | LARGEST)` returns **2,074,976 bytes**.
A real public Exec allocation and free of **1,048,576 bytes** succeed, raw
and bus-visible free-chunk metadata agree, the largest allocation is restored,
and the original 822-byte negative table is unchanged. A separate
[post-correction instruction trace](D:/TestData/CopperOSCommands/ReadArgsDifferential/exec-fast2m-private-gateways-after-fix.json)
also follows the former failure boundary with no gateway covering the free
chunk: at instruction 1,633,831, raw and bus `mc_Bytes` both equal 216.
Trace SHA256 is
`E6648ADD6877D38391DB9D4AA4EBA52E2174F440365122EC6C9CB9505A0CBCDE`.
The [scoped fix receipt](D:/TestData/CopperOSCommands/ReadArgsDifferential/exec-private-gateway-fix-receipt.json)
binds the source files, executed provider/test DLLs and both traces/TRXs;
its SHA256 is
`2EC396B69E8E80E0D21F5CEF29064D9F24692AD6C44088932CA53303E505C767`.

A later test-only build added the native repeated-install assertion. It
completed with zero warnings/errors and left all 729 captured source files
and 27 canonical DLLs unchanged. The canonical emulator had already been
rebuilt by that time, so its new identity was checked independently: the
same **17 gateway regressions pass** on emulator SHA256
`453DE5EAAC3FC284CE954B1B10E0D08B619C7D9100A06B583C806A412BE0EDBD`
and test SHA256
`BF30E5CA9F750BF8E778105AC33405CEF3F5AFC5C61F32C7E41B7DCC5103CA7A`.
Canonical and executed DLL hashes/timestamps and the MedPlayer source files
remained unchanged across that no-build rerun. The
[current-copy TRX](D:/TestData/CopperOSCommands/ReadArgsDifferential/exec-private-gateways-current-copy.trx)
has SHA256
`FDCD260D82BE1CEA18EEA1D65EEF0C64621B746E55B86CE46D870C28D0252B00`;
the [bound current-copy receipt](D:/TestData/CopperOSCommands/ReadArgsDifferential/exec-private-gateways-current-copy-receipt.json)
has SHA256
`D1DE3D77912235987B871ADA5A635F963F207B4C96DFBE013DE476CFF6D33439`.
ReadArgs discovery on this test assembly names exactly one intended test;
that discovery and these gateway regressions do not execute generated DOS.

These are component prerequisites, not DOS parser or full-command
parity. The smoke strictly requires the licensed ROM when its category is
selected; ordinary unconfigured runs must exclude both
`LicensedExecPrivateGateway` and `LicensedDosReadArgsDifferential`.

### Generated installer diagnosis after the gateway correction

For the failing run described here, the DOS owner's rebuild and manifest-only
revalidation both passed before execution. That 68000 HUNK is 869,272 bytes, SHA256
`F237E0DABB4B4AB6A22022193EAEC952E348B4887A868922B69885A4918715DE`;
`dos-build-inputs.json` and `dos-manifest.json` hashes are respectively
`3D509D4E7F4325CFD3127C98D3BC96169BA60CBA5FA5CA0CC394B3F377D400A5`
and `DFF089C11420433238FD95951104B7F37BBF5C94D2A2A39873FDAC3C117888BE`.
The runtime fixture independently revalidates the recorded source closure
and managed/native inputs before accepting this HUNK.

With 2 MiB real fast RAM and the corrected emulator, original native
dos.library 40.3 executes all 44 cases again. Every copied result, IoErr,
source cursor and cleanup result exactly matches the earlier 512 KiB
original-only capture. The new
[original-only receipt](D:/TestData/CopperOSCommands/ReadArgsDifferential/original-readargs-v4063-fast2m.json)
has SHA256
`AFFF55CFDE318E26026FBAAC971F73850FD676766B81EC42081D6104AE579D46`.
It explicitly records **44 original cases, zero generated cases, comparison
false**. It contains no ROM bytes or absolute guest addresses.

The genuine Exec allocation and native HUNK relocation now succeed. The
loader allocates 628,068 bytes at `$00207BE0`; the relocated code begins at
`$00207BE4`. Entry `copperstart.dos.install-system` is `$002A09DA`. The
observer retains the original one-million-instruction limit and call frame;
it reads registers, stack addresses, public library headers and exception
metadata without repairing guest state.

| Boundary | Actual observation |
| --- | --- |
| Native instruction 880 | PC `$00223F76` raises address-error vector 3, stacked PC `$00223F78`, saved SR `$2010`, A0 `$FFFF00AF`, D0 `$18`, A6 `$00200810`, SP `$0007CD48`. |
| Generated symbol | The pinned HUNK map resolves offset `$1C392` to `DosNativeOwnerRecordCodec.ReadState<CopperSharpNativeDosPlatform> +4`; caller `$00223ED0` is `ValidateOwnerHeader +$50`. |
| Foreign library | The scan's current node is real `expansion.library` 40.2 at `$00C000C4`, with negative/positive sizes 164/390. It is being interpreted as a DOS private extension. |
| Invalid ownership inference | The extension value implies state `$FFFF00FF`, then owner `$FFFF00AF`. `ValidateOwnerHeader` reads owner+24 before checking magic; the odd effective address `$FFFF00C7` faults. Native `IsMapped` only checks arithmetic overflow and is not evidence of allocation ownership. |
| Remaining budget | After 881 generated instructions, ROM handles fatal alert `$80000003` and remains at PCs `$00F83626/$00F8362E/$00F83632/$00F8363A`. No public Exec call preceded the exception, and `copperstart.dos.init-owned-system` was never entered. |

This is a native installer fault, not evidence that initialization merely
needs a larger budget. The
[strict failing TRX](D:/TestData/CopperOSCommands/ReadArgsDifferential/readargs-native-installer-library-trace.trx)
records **one executed failure, zero passes/skips**, SHA256
`EEC1DC199A76A8B0FBA74D3567CFDCF929CBC9D70790EA293959C005A776B5BA`.
Its bounded
[address/register trace](D:/TestData/CopperOSCommands/ReadArgsDifferential/native-installer-library-trace.json)
has SHA256
`F9E0C0AE808C55600E04E0448FF73D1ADC1067F89792A79B58FE93575F405377`.
The executed test assembly hash is
`8238BFEA59D20FABAEC26CDACC946F150D9833629BA4B65483676A1C10AFF596`;
the corrected emulator hash remains `9F7B35870AB98441D095FE9ED8835A52BB51043E8E775566B10B7AE33E2572D2`.
All observer builds used `BuildProjectReferences=false`; hashes and last-write
times of the shared DOS/Exec/SDK/SDK.Support DLLs remained unchanged.

The bounded correction is implemented at the actual
[CopperStart.Dos owner](D:/Koodit/GIT/CopperStart/src/CopperStart.Dos/DosNativeOwnedLibraryCore.cs).
It holds the public library scan under Forbid/Permit, checks `NT_LIBRARY`,
the published 1208/84-byte extents, version 51.71 and the `dos.library` name
before reading a private extension. It requires the whole library/owner
range to fit inside one valid `NT_MEMORY` PUBLIC region in the genuine Exec
MemList; merely testing two endpoints cannot exclude a hole between them.
It does not assume a MemHeader is stored inside its own region and does not
alter native `IsMapped`. Range admission is followed by the existing sealed
owner identity, Exec/name/ID backlinks and deeper binding checks. Magic,
self/size/profile and seal must pass before reading the owner state field.

The new
[discovery tests](D:/Koodit/GIT/CopperStart/tests/CopperStart.Exec.Tests/DosInstalledLibraryDiscoveryTests.cs)
use the owner's existing portable allocator and a read-observer hook in its
test memory helper. They are explicitly synthetic owner tests, independent
of the licensed machine. They prove that foreign/small headers are skipped
without reading their extensions, odd/even poisoned pointers and invalid
memory classes/bounds are rejected, identity fields precede state reads,
and foreign predecessors do not hide a valid owned library. Repeated lookup
returns that same library without writes or allocations.

The bound focused run reports **49 passed, zero failed/skipped**: 39 new
cases and ten existing native-library owner tests. A first passing run was
kept provisional because another concurrent task rebuilt the canonical DOS
DLL after the tests finished. The repeated sequence captured the DOS DLL
immediately after its bounded build, required the executed test copy to
match, and checked every owner source/dependency and all 40 runtime DLLs
before and after execution. No input changed during that accepted run.
The actual DOS/test DLL hashes are respectively
`26D2639B6EAF0455F60839A9BF964403B608B82E481D832241FBA05217F9504A`
and `6C437A2A2597A76B27314440B36BA11DBE74CD68DCB1C9457AA099E60EFD367D`.
The [bound TRX](D:/TestData/CopperOSCommands/ReadArgsDifferential/dos-installed-library-discovery-bound.trx)
has SHA256
`04902907DC690B6FB4CAEF2EDF5EA48EA4EAAEFE12B74EFFBC24C2FC5D14DF94`;
the [source/runtime receipt](D:/TestData/CopperOSCommands/ReadArgsDifferential/dos-installed-library-discovery-receipt.json)
has SHA256
`D6F8CD8A809FC884FF68468BADACE0D0034D7330758AF0638E229AB934B2F79B`.
Both project builds used `BuildProjectReferences=false` and completed with
zero warnings/errors. Stale restore metadata initially caused NU1105 before
test compilation; refreshing restore metadata resolved it without building
the shared dependency projects.

The strict generated parser gate remains failed until a fresh source-bound
artifact actually executes and matches the original corpus. The next native
fixture also invokes a successful installer again and requires the same
library without re-entering its initializer, retaining the original bound
for each call. These unit results do not qualify that native behavior.

A read-only source audit identifies likely later parser differences, without
claiming candidate observations. The native ReadArgs wrapper delegates to
[DosCore.ReadArgs](D:/Koodit/GIT/CopperStart/src/CopperStart.Dos/DosCore.cs:2660),
whose success path unconditionally writes `IoErr=0` at line 2707. Buffered
ReadItem and the allocation/free helpers do not otherwise clear successful
explicit-source parsing errors. All 32 successful original cases preserve
the seeded `0x13572468`, including after FreeArgs. The nearest existing owner
tests are in
[DosCliCoreTests](D:/Koodit/GIT/CopperStart/tests/CopperStart.Exec.Tests/DosCliCoreTests.cs:117),
but their success assertions start with zero IoErr; they do not test this
preservation rule. A measured mismatch would justify a bounded seeded-error
success/failure/cleanup slice there, without changing global DOS error policy.

The same audit finds `/S` storing `1`, where the original corpus reports
`FFFFFFFF`, and existing owner tests treating source exhaustion without LF
as success. Those are separate prospective differences. No source change,
mismatch allowance or parity reclassification has been made from this audit;
the strict native corpus remains the next gate.

### Isolated owner build after shared output drift

A subsequent live build produced all three CPU artifacts but correctly
rejected its managed-input fingerprint. Its three compatibility reports
record the same four reachable assemblies, while Exec and SDK.Support were
replaced during the final target at 12:48:42 UTC. Recreated SDK/compiler
shadow DLLs embed `C:\D-drive\...` PDB paths; the expected DLLs embed
`D:\Koodit\...`. This identifies conflicting builds through aliases of the
same workspace, without attributing a particular task. That failed build
was not accepted or used for parser execution.

The replacement build uses private source copies under
[the isolated build directory](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-20260830T125923Z-be3b3c30).
It preserves the CopperStart/CopperSharp68k sibling layout, required build
settings and AGENTS instructions, the compiler framework resource, and SDK
Support source links. Exactly **761 source/settings files** were captured
with matching original/copy hashes and a repeated file inventory. No ROM,
command, or existing compiled binary was copied. Both restores close over
**nine private projects**, with no reference to a live project output.

The **unchanged** private `Build-DosNativeArtifacts.ps1` and its separate
`-SkipBuild -ManifestOnly` validation both pass. CLI and DOS builds report
zero warnings/errors. All private sources remain unchanged and the final
live-source comparison reports zero differences. The 68000 HUNK is
**871,040 bytes**, SHA256
`9A9A69046C0595E59E90F087A2F8BE948F4EE4A49D8A1B84555A3C7BDE41B403`.
Its private `dos-build-inputs.json` and `dos-manifest.json` hashes are
`6CC865E562A8B4A19BF670EB2DCCCD20C8D70F0B766DF167707041D9785A3E46`
and `5918C62EF79138D6EBA5501EB79640A24B03DFCBB54C3EE95DF56A9D6C6AC565`.
The [build receipt](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-20260830T125923Z-be3b3c30/isolated-build-receipt.json)
has SHA256
`10A588732C6473CE3054E304F5DD9170CB39ECC374E338B77D3E56D0B989BDA6`;
it includes the source capture, restore graphs, stage logs and all artifact
identities. The [saved drift diagnosis](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-20260830T125923Z-be3b3c30/live-output-drift.json)
was recorded before cloning. This build was subsequently used by the private
host run below; build acceptance is kept separate from the failed comparison.

### Private host build and first generated parser observations

The next strict run rejected its live-host preflight before executing a test.
The previously qualified `BF30E5...` test DLL had been replaced at
13:09:04.623 UTC by
`59B833F916F5C15618E9BB452DD193B5EA0F751E385F7A0817B7C65BC2400164`.
Its embedded PDB path uses the `C:/D-drive` alias. Five of the 41 previously
bound runtime DLLs changed bytes; Exec also changed timestamp without changing
bytes. The emulator remained `453DE5...`, and both ReadArgs files, both gateway
test files and the two changed Exec provider source files remained exact
matches. Other active tasks had changed unrelated tests and Graphics sources.
No earlier receipt was relabelled, and no BF30 binary backup was found in this
task's private evidence directories.

To remove that race, the complete 119-file C# test tree, original csproj and
AGENTS.md were copied into a new private host snapshot. Its private
`Directory.Build.targets` replaces the six production ProjectReferences with
22 references to copied, hash-pinned DLLs. The same package references and test
assertions are retained. Microsoft.NET.Test.Sdk's 218-byte `Program.cs` was also
copied exactly and selected through its existing `GeneratedProgramFile`
property. No live project or provider was built. The first overlay's missing
full assembly identities, and the next build's unaccounted package source,
remain preserved as unsuccessful setup attempts in that snapshot.

The accepted build contains 123 C# inputs: 119 unchanged test files, the copied
Test SDK program and three generated private files. No C# input or project
reference points to a live repository. All 40 emitted dependency DLLs equal
their captured pins. The receipt also binds 204 resolved references, ten
analyzers, 366 package/toolchain/import files, 110 output files including
deps/runtimeconfig files, and the unchanged 873-file private DOS snapshot.
The private overlay SHA256 is
`B076162739058DF561D1F345F5DEEE6E1FCFCE46D1EA0925EE2B7111849263F9`.

The [private host build receipt](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-host-20260830T132350Z-063f12e4/host-build-receipt.json)
has SHA256
`B35BD59F755FFB900EFDB7CE64C0DC3FE4529240577B059F89958CF9B2E8FD4D`.
Its newly compiled test DLL is
`7899455419AD8C5EDC1025C5632061BEFD8526B6712DB6D3C54C5C4726F01871`,
with the same frozen emulator
`453DE5EAAC3FC284CE954B1B10E0D08B619C7D9100A06B583C806A412BE0EDBD`.
The [private gateway run](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-host-20260830T132350Z-063f12e4/results/private-host-gateways-receipt.json)
passed **17/17, zero failed/skipped**, with unchanged bound inputs; receipt
SHA256 `3512F86D377681EE8B326D69D07F0593B5EF588C3C13F482383FCC696AFC3F82`.
This is an additional runtime binding; the earlier 9F7 and BF30/453 receipts
remain historical evidence.

The [strict private comparison](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-host-20260830T132350Z-063f12e4/results/private-host-readargs-receipt.json)
then executed **one test: failed, zero skipped**. It used the accepted
`9A9A6904...` native HUNK and explicit private source/compiler root overrides.
Its receipt SHA256 is
`18C39884481EDFFD7634E97DEFF70CA934ED234809547EAA6F8E41EFA31D0959`;
the [TRX](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-host-20260830T132350Z-063f12e4/results/private-host-readargs.trx)
SHA256 is `33DC162EC9C00EE11C295B490DA72EC81BC1C9991E18BD3CDFC59A0F23C8A435`.
All source, binary, package, ROM and artifact bindings remained unchanged.

The first native installation returned `0x002A3EE8` in 169,739 instructions,
166,458 inside the HUNK. The second returned the same library in 6,620
instructions, 6,596 inside the HUNK. Both are below the unchanged 1,000,000
instruction limit, produced no new CPU exceptions, and returned with the
expected stack. Only the first entered `init-owned-system`. This exercises
the corrected discovery path against real foreign libraries and an existing
owned library; it does not establish complete DOS or parser parity.

All 44 original observations exactly match the preceding 2 MiB reference
receipt. Twenty-five generated observations completed; every paired record
differs. Recorded examples are:

| Case | Original observation | Generated observation |
| --- | --- | --- |
| `required-missing`, `NAME/A` | NULL, IoErr 116 | Caller RDArgs, empty slot, IoErr 0 |
| `number-positive`, `COUNT/N/A`, `42` | Dereferenced LONG `0x0000002A` | Dereferenced bytes `0x34320000` |
| `number-invalid`, `COUNT/N/A`, `forty` | NULL, IoErr 115 | Caller RDArgs, dereferenced bytes `0x666F7274` |
| `switch-present` | Switch `0xFFFFFFFF` | Switch slot `0x00202F8C` |
| `required-positional` | Preserves seeded IoErr; cursor after LF | IoErr 0; cursor before LF |

Case 26, `multiple-one` (`ITEM/M`, `one` plus LF), returned from native
ReadArgs but failed the fixture's mapped-string assertion while copying the
`/M` list. The remaining 19 candidate observations, including this malformed
one, were not published. No mismatch was waived. Its raw slot/string pointer
and per-call native PC trace were not logged, so a specific pointer value or
whether flags were lost during template parsing, field storage or value
assignment cannot yet be claimed. The relative fixture layout is retained
in the [compact paired failure summary](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-host-20260830T132350Z-063f12e4/results/private-readargs-failure-summary.json),
SHA256 `89D40B4A6856CBF330F2ECEB85CE7090394494F76CD99047DA5EA8E928870A8C`.
Final generated expunge, all-case code immutability and full comparison
completion were not reached. The next work must diagnose the actual modifier
behavior before changing the separate IoErr/cursor semantics.

The accepted private host and DOS artifacts can be replayed without rebuilding
them. Preserve existing receipts and select a new result directory. With the
external licensed ROM already present, the failed comparison is reproducible
using:

```powershell
$privateHost = 'D:/TestData/CopperOSCommands/BuildSnapshots/readargs-host-20260830T132350Z-063f12e4'
$testDll = "$privateHost/MedPlayer/CopperMod.Amiga.Tests/bin/Release/net10.0/CopperMod.Amiga.Tests.dll"
$resultDirectory = "D:/TestData/CopperOSCommands/ReadArgsDifferential/replay-$([Guid]::NewGuid().ToString('N'))"
dotnet vstest $testDll --ListTests '--TestCaseFilter:Category=LicensedDosReadArgsDifferential'
$env:COPPER_AMIGA_KICKSTART_ROM = 'D:/TestData/ROM/kickstart-3.1-a500.rom'
$env:COPPER_AMIGA_KICKSTART_VERSION = '3.1'
$env:COPPERSTART_DOS_READARGS_SOURCE_ROOT = 'D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-20260830T125923Z-be3b3c30/CopperStart'
$env:COPPERSTART_DOS_READARGS_COMPILER_ROOT = 'D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-20260830T125923Z-be3b3c30/CopperSharp68k'
$env:COPPERSTART_DOS_READARGS_PRODUCTION_HUNK = "$env:COPPERSTART_DOS_READARGS_SOURCE_ROOT/artifacts/native/CopperStart-DOS-Unified-68000.hunk"
dotnet vstest $testDll '--TestCaseFilter:Category=LicensedDosReadArgsDifferential' '--Logger:trx;LogFileName=readargs-native-isolated-owner.trx' "--ResultsDirectory:$resultDirectory"
```

Run these in a dedicated child process and verify the private build receipt's
source/runtime identities before and after execution, as the captured
`run-private-host-tests.ps1` does. The replay above is expected to fail for
this artifact; do not count a replay as new coverage. Stop if discovery does
not name the one intended test; require positive execution counts and the
final 44/44 completion marker before declaring parity. The test expects the
recorded Release managed inputs in its explicit roots.
`COPPERSTART_DOS_READARGS_SOURCE_ROOT` and
`COPPERSTART_DOS_READARGS_COMPILER_ROOT` can identify non-default source/compiler
checkouts without copying their implementations. Exclude this category from
ordinary runs that do not request the licensed fixture. Do not treat an
unconfigured run, prerequisite failure or original-only capture as passing
the comparison. Interactive help, prompting, stream EOF/cancel, full boot,
RunCommand and MorphOS PPC execution remain unqualified.

### Two-file compiler correction and complete 44/44 failed comparison

The compiler owner independently reproduced the narrow-instance-field load
fault and retained red/green native tests. Its [389-test related receipt](D:/Koodit/GIT/CopperOS/obj/compiler-narrow-instance-fields/e3103abdb499412abd4ff7bf4bfa18a8/receipt.json)
has SHA256 `4E8E7749CC1B86CA28DC10EEF4B6105B2284B14DCA06E77642C2E1F3253B2658`.
No DOS enum, layout, or parser semantics were changed to accommodate the fault.
The next DOS candidate takes the preceding immutable 761-source snapshot and
replaces **exactly two compiler files** with that owner's frozen tested copies:

| Changed source | SHA256 |
| --- | --- |
| `Compiler/Backend/M68kCodeGenerator.cs` | `573923061278C21D7982D191F48FEDC89F1B8078EFF53014FA47810B9C59FF37` |
| `Compiler/Backend/M68kCodeGenerator.Allocated.cs` | `F9A8374182738B1FE669F47852D9D99BE4AE14031D13BFAD2D9EEACC08829167` |

The remaining **759 sources are byte-identical to the accepted private
baseline**. The new [capture receipt](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-fieldload-20260830T141652Z-bab02b59/capture-receipt.json)
is `D87A1F220B03CF81F4BCC80AB91338898159E17936F180556F4FCDFAF8FC7F06`;
its normalized source-inventory digest is
`002F7D899D8B9F583E9FD9198743328990B102E6088ECB25DB2B8388A5F38A64`.
Original/copy identities were checked before and after capture. The copied
owner script and build driver were unchanged. The [private owner build](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-fieldload-20260830T141652Z-bab02b59/isolated-build-receipt.json)
and independent `-SkipBuild -ManifestOnly` validation both passed, using nine
private projects with all 761 sources unchanged throughout. Its receipt is
`DF341447A5A003EBF1208C1B05766AE1A66D2243B5A11F70ED754D47EAEF7F54`.

| Accepted private output | Bytes | SHA256 |
| --- | ---: | --- |
| 68000 HUNK | 871,960 | `E40796F59EAF2A050C873A70D3FEB1A77E7DBA9CB2B3ADD23B66CFB1AD7E868F` |
| 68020 assembly | 7,869,403 | `593EE24EB0596EB13E53912612605F45F52564DFCA22DABD6253D2254F0CD32D` |
| 68040 assembly | 7,855,608 | `E8E91F3A14CAFEFC2696A1CF264437402147CA23A54B4742B45C0FEE74B08B0A` |
| `dos-build-inputs.json` | 1,864 | `6142EEEF6556A618A0090DEBD104F9F16B45F0F07A049523D848B73885394E44` |
| `dos-manifest.json` | 10,522 | `61FF20C01555C383B9DBEF869EC1B20054AFCFC5EEF5B03A0B75ED34584A23B8` |

This is an explicit derivative, **not an all-current-live-source snapshot**.
At build completion the live compiler hooks had already changed again and
live `Sdk.Amiga/DOS/DosRunCommandCallbacks.cs` differed from the preserved
baseline. Those three live differences are reported in the build receipt;
they did not enter the private candidate. No old snapshot, source manifest,
or failed comparison was relabelled.

The strict test reused the unchanged private `789945...` test DLL,
`453DE5...` emulator, 119-source fixture and its prior 17-test gateway pass.
Both DOS source-root overrides point at the new private candidate. The test
used the same 44 cases and one-million-instruction per-call bound, with no
semantic allow-list. It discovered and executed one test: **failed, zero
skipped**. All bound host, candidate, ROM, package/import and runtime inputs
remained unchanged before/after execution.

All **44 original observations match the earlier 44-result capture exactly**.
All **44 generated observations now complete**. The first native installer
returns `0x002A4278` after 169,756 instructions, including 166,475 in the
loaded HUNK; the repeated installer returns the same library after 6,624
instructions, including 6,600 in the HUNK, without re-entering initialization.
Neither installer halts or reports a new CPU exception. After parsing, both
DOS vector snapshots and the loaded HUNK-byte snapshot remain unchanged;
expunge returns the owned segment list and the loader allocations are released.
Failure is at the final exact semantic comparison, not initialization,
pointer copying, expunge or the instruction bound.

Only `required-zero-source` matches every recorded field. The other 43 pairs
have overlapping mismatch counts: return 5, IoErr 35, cursor 41, copied values
8, FreeArgs-called 5, and cleanup IoErr 35. The complete [paired summary](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-fieldload-20260830T141652Z-bab02b59/readargs-test-results/native-readargs-fieldload-summary.json)
has SHA256 `F9844AA9CEE6840B9211018A6E40CFB52045B5E4BEC8C63244E30D7B1C87D53B`.
The five return disagreements are plain `/A` keyword-space recognition,
positive numeric overflow, required argument backfill following `/M`, missing
final LF, and a source bound excluding the LF. `/S` presence is `1` rather
than original `0xFFFFFFFF`; successful IoErr is cleared rather than preserved;
most cursors remain before the terminating LF. Unterminated quoted input
reports 119 instead of original 120. Ordinary `/N` values and `/M` pointer
arrays now copy correctly, including the Eval template, but that does not
qualify either complete parser or command semantics.

The [bound strict run receipt](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-fieldload-20260830T141652Z-bab02b59/readargs-test-results/private-host-readargs-receipt.json)
is `040ACBB21326A9C659316827F1CD96699C708A5218B6DD2678E0C5C8CD53CA74`;
the [TRX](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-fieldload-20260830T141652Z-bab02b59/readargs-test-results/private-host-readargs.trx)
is `132C4B9358FD26D443417DF32F87F00F064D359384C75048A5DCF7066D5BE6F2`.
Both retain `comparisonPassed=false`. This remains explicit-source component
execution with existing licensed Exec/device overlays, **zero commands and
zero RunCommand calls**, no full boot and no MorphOS runtime claim. The old
25-result `/M` failure remains separately preserved above.

### Parser-owner follow-up: all 44 exact comparisons pass

The [parser-owner qualification record](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/readargs-parser-qualification.md)
describes the subsequent `DosCore.cs` correction and new private
candidate from the preceding compiler-corrected snapshot, replacing that one
source while preserving the other 760 source/settings inputs. The unchanged
owner pipeline and manifest-only validation passed. Its [build receipt](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-parser-20260830T145344Z-1695653f/isolated-build-receipt.json)
has SHA256 `243D32D88592512B27430949B8DA0BDC2B36031D0182AFB98079D5F93879BE82`.
The resulting 873,920-byte 68000 DOS HUNK is
`5014FCE66228AB170F498474248B387C5AEA40F80E39C0404C8CF1CB3280004F`.
This remains an explicit private derivative, not a claim about all current
live compiler, SDK or DOS sources.

The owner ran the unchanged `789945...` host fixture with frozen `453DE5...`
emulator and the new private DOS roots: **one test passed, zero failed/skipped;
44 original and 44 generated cases match every compared field**. The two
native installations return the same library without reinitialization, and
all input, loaded-image, vector and cleanup checks pass. No comparison waiver,
new source buffer or longer instruction bound was introduced.

The [strict receipt](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-parser-20260830T145344Z-1695653f/readargs-test-results/private-host-readargs-receipt.json)
is `8647612781105CE75818BBFE060E76F38FF28375E667A8B550D37D5E6F191A49`;
the [TRX](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-parser-20260830T145344Z-1695653f/readargs-test-results/private-host-readargs.trx)
is `B8D8C7B5715AF07317D3C717EA19D52FFB39DA1F53F5DFDEBCB1F7C7FED9C3BA`;
the [paired summary](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-parser-20260830T145344Z-1695653f/readargs-test-results/native-readargs-parser-summary.json)
is `07B5ED028C519D15768B1AE3EE758AE7B89963951CE887C6396496DE872C9F00`.
This pass is only the finite explicit-source ReadArgs component differential:
**zero commands, zero RunCommand, no prompt/console, full boot or MorphOS claim**.
The preceding failed receipts and their conclusions remain unchanged.

### Original DOS process, CLI and stream readiness

A separate, later private host variant adds only
[KickstartRomDosCommandLaunchReadinessTests.cs](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosCommandLaunchReadinessTests.cs)
to the same 119 captured test sources and 40 pinned dependency DLLs. It leaves
the preceding parser fixture, HUNK and failed comparison unchanged. The first
variant's namespace/size-cast compilation failure remains preserved with zero
tests executed. After the file owner corrected those compile issues, the
new source SHA256 is
`EC5DBA1CB37CD7A48124FBD3A30EF0F27DB7F396530E51A68E1AE7E167489A7E`.

The [readiness host build](D:/TestData/CopperOSCommands/BuildSnapshots/dos-readiness-host-20260830T135116Z-f317d560/host-build-receipt.json)
passed its same source, reference, package/import and runtime checks. Receipt
SHA256 is `8E64400DFFCC240584AEB01235AFA1CBD831662090B9C6E1E7141D21C720EA3A`.
The test DLL is
`03B269303B4E9D8FBC8D49F07E4E77A23EEFE1FFC3E364BDD9443D3D7497C886`;
the emulator remains the frozen `453DE5...` binary.

Selecting `Category=LicensedDosCommandLaunchReadiness` discovered and executed
one test: **failed, zero skipped**. The [bound run receipt](D:/TestData/CopperOSCommands/BuildSnapshots/dos-readiness-host-20260830T135116Z-f317d560/results/private-original-dos-readiness-receipt.json)
has SHA256
`E0C0790FB501FB76B00EF2B815AF814FA1485464C08EEC55784B7655723264E4`;
the [TRX](D:/TestData/CopperOSCommands/BuildSnapshots/dos-readiness-host-20260830T135116Z-f317d560/results/private-original-dos-readiness.trx)
SHA256 is `17C00807027D52240035B9AF767476821041777A3E6F4D3EE52CC8A1D80C96D5`.
All bound inputs remained unchanged. The aggregate R01-R02 gate failed at R02,
with **R01 accepted and only one of two readiness rows accepted**.

The original DOS 40.3 library is `0x00205AB4`. Its current task is a readable
228-byte `NT_PROCESS` (13) at `0x00205EE0`; a readable 64-byte CLI already
exists at `0x002028B8`. Public `Cli()` agrees with the Process CLI BPTR
`0x00080A2E` converted to an address. Public `Input()`, `Output()` and
`GetArgStr()` all return zero. All four queries preserve the task and expected
stack and report immediate IoErr zero. The original DOS vector bytes are
unchanged; the inspected vector slots and native ROM targets have no DOS host
gateways.

The prerequisite identified by that run was **stream acquisition**, not creation
of a missing Process/CLI. No Process, CLI or FileHandle fields were written to
arrange that observation. No command, ReadArgs or RunCommand invocation was
performed; no generated DOS HUNK was executed by this readiness test.
The [compact readiness summary](D:/TestData/CopperOSCommands/BuildSnapshots/dos-readiness-host-20260830T135116Z-f317d560/results/original-dos-readiness-summary.json)
has SHA256
`D39987792105DA20B139CC3EB0676744361173C87FB59E084CDA9DC9CD2405B9`.
The following independent R04 run tests public stream acquisition and cleanup;
it does not retroactively change the failed R01-R02 receipt.

### Original DOS public NIL: stream acquisition and cleanup

[KickstartRomDosCommandStreamReadinessTests.cs](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosCommandStreamReadinessTests.cs)
adds one R04 test, with source SHA256
`68DB9C823DFF0F186B1D42A345B52066A69CAAF86AB533EC35246D1920592996`.
It reuses the real Process/CLI and invokes public original DOS `Open`,
`SelectInput`, `SelectOutput`, `Input`, `Output`, `Cli`, `IoErr`, and `Close`.
The only caller data is a `NIL:` string allocated and freed through public
original Exec. No Process, CLI, FileHandle or explicit `RDA_Source` fields are
fabricated. Mode values 1005 and 1006 were checked against NDK 3.1
`dos/dos.h` on the local Developer CD (`7791A911...`) and the SDK enum.
The NDK `dos.doc` (`2E22D1D1...`) also establishes that an explicitly opened
handle is closed once and that `Close` deallocates it even on failure.

The new [private host capture](D:/TestData/CopperOSCommands/BuildSnapshots/dos-streams-host-20260830T141219Z-567b5085/host-capture-v3-receipt.json)
contains the immutable 119-test baseline plus the corrected readiness file
and this stream file, the unchanged project/AGENTS files, and the same 40
pinned dependency DLLs. Its [build receipt](D:/TestData/CopperOSCommands/BuildSnapshots/dos-streams-host-20260830T141219Z-567b5085/host-build-receipt.json)
has SHA256 `08717253BF06009B6FC75DFE762E28B99118C02368A07F22CBF8F2D87BB42AE6`.
The actual test DLL is
`8E95ED25F1084798DB9893BE2264F9187E27402DA73079AE27D00EC45E4449CE`;
the emulator remains the frozen `453DE5...` binary. Restore/build inputs,
all copied dependencies, package/import inputs, and outputs were checked.
No live project, SDK, compiler or DOS build ran.

Selecting `Category=LicensedDosCommandStreamReadiness` discovered and executed
exactly **one test: passed, zero failed or skipped**. All bound inputs remained
unchanged. Original DOS 40.3 at `0x00205AB4` used the same real Process
`0x00205EE0` and CLI `0x002028B8`. `Open("NIL:",1005)` returned BPTR
`0x00080B6B`; `Open("NIL:",1006)` returned BPTR `0x00080B79`. Both were real
mapped 44-byte handles. Selection and public query results agreed with the
documented Process fields. Cleanup restored the borrowed zero input/output
selections **before** closing the acquired handles. Each `Close` returned
`0xFFFFFFFF`; neither inherited handle was closed. All 19 recorded DOS calls
returned, preserved the task and expected stack, and reported immediate IoErr
zero. The original vector bytes remained unchanged and the inspected DOS
entries and native ROM targets had no host gateways. No unsafe continuation,
unrestored selection, unclosed known handle or cleanup failure remained.

The [bound R04 run receipt](D:/TestData/CopperOSCommands/BuildSnapshots/dos-streams-host-20260830T141219Z-567b5085/results/private-original-dos-streams-receipt.json)
has SHA256 `E3D434BE39F8687977D585F61FAA14F3F8B42047C0FED200C135223AE6A76940`;
the [TRX](D:/TestData/CopperOSCommands/BuildSnapshots/dos-streams-host-20260830T141219Z-567b5085/results/private-original-dos-streams.trx)
is `FDCEAB3F2FA0FCB78A52FFED002DC08E94007B8F6473C2A2E0E813C71F758F73`.
The [compact result](D:/TestData/CopperOSCommands/BuildSnapshots/dos-streams-host-20260830T141219Z-567b5085/results/original-dos-stream-readiness-summary.json)
is `B2BFCD42A2AAA564979115F1E53BF6A2333744314E52F7EE595C224AA83D8DD6`.
This qualifies only public stream acquisition/restoration/cleanup in this
original DOS component fixture. It invokes **zero commands, zero ReadArgs and
zero RunCommand**, and executes no generated DOS HUNK. NIL: stream ownership
does not establish argument delivery, readable command output, normal Shell
startup, complete command launch, full boot, or MorphOS execution.

### Original RunCommand startup and default-input observer

The NDK 3.1 `dos.doc` description of `RunCommand` establishes its public
register arguments: segment-list BPTR in D1, requested stack bytes in D2,
argument pointer in D3, and argument length in D4. The argument storage must
be NUL terminated and include a terminating LF for ReadArgs. Version 37+
sets up the current input buffering and `GetArgStr`, restoring both on return.
These local primary descriptions, and `dosextens.h` public fields, guided the
test; no ROM routine or implementation was copied. The copper68k-emulation
workflow was used for the authored instruction sequence and CPU trace bounds.

The only new live sources are
[DosCommandInputObserverProgram.cs](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/DosCommandInputObserverProgram.cs),
SHA256 `B1C33C654A094400DDB42D50A8DDBB60E537369EB88722CF52CABD7E822827ED`, and
[KickstartRomDosCommandInputObserverTests.cs](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosCommandInputObserverTests.cs),
SHA256 `61F142A22E478AF180D2FBF5A6E71965C977EE43AE2D0A4FBF4CD93BBA27761A`.
The helper builds a **908-byte authored two-segment 68000 HUNK**, SHA256
`5663FE06E63A52E328E8ED2CA074E950ED23BDA8F44BB1F98742001B34B95400`.
It saves startup D0/A0/A6/SP, opens its own DOS reference through public Exec,
calls public GetArgStr/Input/Output and `ReadArgs("NAME/M",array,NULL)`,
frees the returned RDArgs, closes that DOS reference and returns 42. A
read-only PC checkpoint copies the typed `/M` strings before FreeArgs; it
does not call a host parser, change guest registers or replace a DOS call.
The HUNK loader allocates the code/data segment chain through real public
Exec. The host enters only the original `RunCommand` vector, never the
observer entry. No Process, CLI, FileHandle, task-stack or scheduler metadata
is fabricated, and no explicit `RDA_Source` replaces the input handle.

The [private capture](D:/TestData/CopperOSCommands/BuildSnapshots/dos-command-input-host-20260830T144532Z-886cebc7/host-capture-v3-receipt.json)
preserves the 119-source baseline, frozen R01/R04 test helpers, unchanged
project/AGENTS files and 40 dependency DLLs, adding only these two sources.
Its [qualified host build receipt](D:/TestData/CopperOSCommands/BuildSnapshots/dos-command-input-host-20260830T144532Z-886cebc7/host-build-receipt.json)
is `3E15D77EEABAF2AD2658ECAF8E98EBD7C2893DA2F53FA6E5B6A7DB2391E1F57D`.
The actual test DLL is
`0C9C0898C18217EB2B4CABEFCF59A0B869B7B7DEAFD8EB392328618BB555D318`;
the emulator remains frozen `453DE5...`. No live compiler, SDK, provider or
DOS project was built. Selecting `Category=LicensedDosCommandInputObserver`
discovered and executed **one test: passed, zero failed/skipped**. All bound
inputs remained unchanged. Its two internal readiness assertions cover the
startup and parsing of this single 25-byte variant; they do **not** close the
contract's exact R05 LF-only (length 1) and R06 `alpha beta` plus LF (length 11)
rows. At this point those two cases remained pending; the later exact-case
run below supplies their independent evidence. The original receipt and its
counters are preserved unchanged; this is additional nonempty observer coverage.

Original DOS 40.3 entered its native `RunCommand` target, then the authored
entry once at `0x00202E1C`. It returned after **3,381 instructions**, including
68 authored observer instructions, within the unchanged one-million bound.
No new CPU exception, halt, unsafe continuation or cleanup failure occurred.

| Observation | Captured result |
| --- | --- |
| Argument tail | `alpha "two words" "a**b"` followed by LF; 25 bytes plus stored NUL |
| Startup D0 / A0 | `25` / `0x00203120` |
| Public GetArgStr | `0x00203120`; copied bytes equal the original argument tail |
| Startup A6 | `0x00F9FE9E`, **not** DOSBase; the probe independently opens DOS |
| Startup SP / inherited SR | `0x0020BBDC` / `0x2000` |
| Original DOS-managed temporary task stack | lower `0x00207BE8`, upper `0x0020BBE8` for the requested 16 KiB |
| Original ReadArgs call | D3 is NULL at the native vector; NAME/M returns `alpha`, `two words`, `a*b` |
| Ownership / results | one ReadArgs, one FreeArgs after the typed copy; RunCommand returns 42; immediate IoErr values are zero |
| After return | original task/CLI identity, argument pointer, task stack bounds, Process return address and selected NIL: buffer/position/end fields restored |

The acquired NIL: selections were restored to the borrowed zero input/output
values before Close; the inherited handles were never closed. The probe's
DOS reference, its two caller-owned HUNK allocations and caller string
allocations were released. Original DOS vector bytes and authored code bytes
were unchanged. The counter policy is explicit: **one authored observer
command and one original-DOS RunCommand call**, with **zero original
distribution C: commands, zero generated CopperOS commands and zero command
comparisons**. No generated DOS HUNK executed in this run.

The [bound run receipt](D:/TestData/CopperOSCommands/BuildSnapshots/dos-command-input-host-20260830T144532Z-886cebc7/results/private-original-dos-command-input-observer-receipt.json)
is `8436167BEA33F4E10013FAB34A01919C419BF0425CB0201BF62E7AC7C1955F29`;
the [TRX](D:/TestData/CopperOSCommands/BuildSnapshots/dos-command-input-host-20260830T144532Z-886cebc7/results/private-original-dos-command-input-observer.trx)
is `C87098A41164664EA0CD74E7DE48A4296B6A5E74358676E27CBBC18590347D00`;
the [compact result](D:/TestData/CopperOSCommands/BuildSnapshots/dos-command-input-host-20260830T144532Z-886cebc7/results/original-dos-command-input-observer-summary.json)
is `C6C96C9D57A41A4BDE792F470BE7DA3A67D3F9DC1441B49BDAF21F4A3711D79D`.
This is the inherited supervisor-mode component fixture, not normal
user-mode Shell startup or full boot. The call demonstrates argument delivery
through original DOS without a parser gateway. It does not qualify nonempty
borrowed-stream restoration (R07), real output/error capture with NIL:,
filesystem handlers, complete command behavior or MorphOS execution.

### Exact R05 and R06 observer cases

The later [KickstartRomDosExactCommandInputObserverTests.cs](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosExactCommandInputObserverTests.cs),
SHA256 `340ABD0DF6F9E4C6DA05D55054E21470C994DCE2E62EF1B74C38EE98B2F3BD3A`,
adds exactly the two argument inputs required by the launch contract. It reuses
the unchanged 908-byte authored observer HUNK and original native RunCommand/
ReadArgs trace helper. The original 25-byte test, its private host and its
receipt remain unchanged. No distribution command or generated CopperStart
library is involved in these two additional calls.

The [new private build](D:/TestData/CopperOSCommands/BuildSnapshots/dos-exact-command-input-host-20260830T151452Z-520a149f/host-build-receipt.json)
has SHA256 `3B1DD3803476F88729BC58E933FA5D927D9A09CC2A065E5F029A5097F40DC674`.
It derives from the same frozen 119-source baseline, adding the four frozen
readiness/observer helper files and this one new theory file, with the same
40 dependency DLLs. The test DLL is
`9D2E0D5420D6DC3244820D93520CD192C2C12E6D748044E67F2E350055984A00`;
the emulator remains `453DE5...`. Discovery named **two tests, both passed,
zero failed/skipped**. All source, artifact, package/import and runtime inputs
were unchanged before and after execution.

| Exact contract row | Argument bytes; length excluding NUL | Startup D0; copied NAME/M result | RunCommand / immediate IoErr | Instructions |
| --- | --- | --- | --- | ---: |
| R05 | `0A`; 1 | `1`; null NAME slot | `42 / 0` | 1,066 |
| R06 | `616C70686120626574610A`; 11 | `11`; `alpha`, `beta` | `42 / 0` | 2,716 |

Both observe startup A0 = public GetArgStr = `0x00203120`, with the exact
argument bytes intact, startup SP `0x0020BBDC`, A6 `0x00F9FE9E` and inherited
SR `0x2000`. Each executes 68 authored observer instructions, one original
RunCommand, one original ReadArgs with D3=NULL and one matching FreeArgs after
copying its typed result. The LF-only case still receives a successful DOS-
owned RDArgs, with a null result slot. Both restore the task/Process/CLI,
argument pointer, stack and selected input/output buffer state and release
only owned resources. No new exception, halt, unsafe continuation, byte change
or cleanup error occurs.

The [strict receipt](D:/TestData/CopperOSCommands/BuildSnapshots/dos-exact-command-input-host-20260830T151452Z-520a149f/results/private-original-dos-exact-command-input-observer-receipt.json)
is `9FFD6E9D09F9E6B724F9BBC769BA5927907DBD5ACDAC9DEB622572F19457CA2A`;
the [TRX](D:/TestData/CopperOSCommands/BuildSnapshots/dos-exact-command-input-host-20260830T151452Z-520a149f/results/private-original-dos-exact-command-input-observer.trx)
is `28306E5B4343D5BF1EF769363D73D5E537319675081F53CBB9B4C9A040E5C1A6`;
the [9 KB compact capture](D:/TestData/CopperOSCommands/BuildSnapshots/dos-exact-command-input-host-20260830T151452Z-520a149f/results/original-dos-exact-command-input-compact.json)
is `49A72CE68C07DF7C2BF3FB0B5C7F73D49B617DBC3B37C994CA0B86FAAA6B4C6A`.
These are **two new authored observer invocations**, separate from the earlier
one and from MakeDir's original/generated command counts. The exact R05/R06
component rows now have evidence; R07 nonempty borrowed-stream restoration,
normal user-mode Shell execution, complete boot and generated DOS input delivery
remain unqualified.

### MakeDir input edges through original DOS RunCommand

The new [KickstartRomMakeDirCommandLaunchTests.cs](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomMakeDirCommandLaunchTests.cs),
SHA256 `21B6DF3E545FE60DD685E30A3BE03A69B0E11DBAFD2F1B435F959FF56864940D`,
executes four independent disposable-machine cases. Each uses the real original
Process/CLI, public NIL: handles and original DOS `RunCommand`; the host never
enters the command directly or supplies `RDA_Source`. The native HUNK loader
owns its public Exec allocations. This does not qualify original DOS LoadSeg.

The unchanged reference is **MakeDir 37.2 (5.4.91)**, 464 bytes, SHA256
`23911DB49742055D8BDDCFE5F8DE82CBB2232F8A7EF850D51BFD27F9B54C819B`.
It stays in the private [reference file](D:/TestData/CopperOSCommands/Workbench31/MakeDir-37.2.hunk),
outside source snapshots. The unchanged generated 68000 probe is 1,876 bytes,
SHA256 `6494FE6888CC46350D47A3B2C32161C55D7D5BE3084063F0F3CA618B61CA407F`,
from the previously accepted `012c05cf6707457a9f288e8b862dbcf4` run. Its
[historical qualification](D:/Koodit/GIT/CopperOS/tests/Commands.NativeRoot/bin/Release/net10.0/qualification-makedir/012c05cf6707457a9f288e8b862dbcf4/qualification.json)
is `E59C8ED1D4F9E2F7A1E7186F301A3FEB3552E503E66EF8A1C6152D8D66CBC266`;
the source/input manifest is
`65892F5A53C626207C71934E882CF46453CA5340642E90F2314732C70689C559`.
All recorded 442 source/settings files, 20 copied binaries, 35 restore files,
196 host inputs, reference bytes and 68000 artifact evidence were rechecked.
**No command or compiler was rebuilt; this is not a new compiler qualification.**

The [private host build](D:/TestData/CopperOSCommands/BuildSnapshots/dos-makedir-command-host-20260830T150723Z-0095e8b1/host-build-receipt.json)
passed with receipt
`20E852FEC2950F95662DD7AF6ECA9EED432F445ED7CBA03930D3B4B9D4DD686F`.
It adds only this test to the frozen readiness/observer and 119-source baseline,
retaining the 40 dependency DLLs and private project overlay. The executed
test DLL is `D1A1BA8D14092945DBB3C18AFF25702AD249F44AAD892D1F496026395F2F0382`;
the emulator is still `453DE5...`. Discovery selected one test, which **passed
with zero failed/skipped**. All bound inputs remained unchanged.

| Case; exact input | Original / generated result and immediate IoErr | Original / generated instructions | Native parser and command calls |
| --- | --- | --- | --- |
| Empty LF; `0A`, length 1 | `20 / 0` on both | 2,044 / 2,294 | ReadArgs succeeds with a null NAME slot; one FreeArgs and one command VPrintf |
| Unterminated quote plus LF; `22616C7068610A`, length 7 | `20 / 120` on both | 4,522 / 4,689 | ReadArgs fails with 120; no FreeArgs, one PrintFault(120, NULL) |

The seven-byte malformed case is a valid additional variant. It is not the
contract's exact fourteen-byte A03 input; that pair is recorded separately below.
The LF-only case is the exact A01 input. This four-call receipt stays unchanged.

Each command entered once through original RunCommand. ReadArgs received
`NAME/M`, an aligned zeroed result slot and D3=NULL, and executed original
native DOS. The successful typed value was copied at the native return before
FreeArgs. The generated command's additional result-slot AllocMem/FreeMem pair
was observed exactly once per call; the original had no command-owned pair.
There were no outstanding tracked command allocations, Lock/CreateDir/UnLock
calls, new CPU exceptions, halts or cleanup errors.

Command SP, original Process/CLI/task identity, argument pointer and selected
input buffer/position/end restore. Borrowed selections restore before acquired
NIL: handles close. DOS open count, command code, original DOS vectors and
argument allocation bytes remain unchanged. Output buffering may change while
DOS formats diagnostics; no output-buffer restoration or stdout/stderr-byte
comparison is claimed. The inherited entry SR remains `0x2000`, so this is
the supervisor-mode component fixture, not normal user-mode Shell startup.

The [strict run receipt](D:/TestData/CopperOSCommands/BuildSnapshots/dos-makedir-command-host-20260830T150723Z-0095e8b1/results/private-original-dos-makedir-commands-receipt.json)
is `AE22F088057F5D44B9B167192F08060BB680A334B6A99316E3B2FE7F79DD3394`;
the [TRX](D:/TestData/CopperOSCommands/BuildSnapshots/dos-makedir-command-host-20260830T150723Z-0095e8b1/results/private-original-dos-makedir-commands.trx)
is `DE085E6F98C4FF58DC4534EA18B1EB53B07C8ECD248F20930DF71C99727A081D`;
the [17 KB compact result](D:/TestData/CopperOSCommands/BuildSnapshots/dos-makedir-command-host-20260830T150723Z-0095e8b1/results/original-dos-makedir-command-compact.json)
is `68089C60F4435D1D168D944479EC749FEA80CFFBD2BD668E3BCE76C2F24230B6`.
Counters are **two original distribution commands, two generated command
calls, four RunCommand calls and two comparisons**, separate from the earlier
authored observer. These two input edges do not qualify filesystem behavior,
diagnostic bytes with NIL:, minimum stack, resident/P admission, other CPUs,
generated CopperStart RunCommand, complete boot or complete MakeDir parity.

### Exact MakeDir A03 malformed-input pair

The new [KickstartRomMakeDirExactQuotedInputTests.cs](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomMakeDirExactQuotedInputTests.cs),
SHA256 `A52E4C9A003FCE035BB5D9F68AB8D590B064958F57EE7600CE24DA38CD7BE50F`,
reuses the unchanged preceding command-launch helper and exactly pinned
original/generated MakeDir artifacts. It supplies opening quote, ASCII
`unterminated`, LF, then NUL: argument bytes
`22756E7465726D696E617465640A`, **length 14 excluding NUL**. No input
normalization or vector-supplied parser result is used. It adds one new
original and one new generated invocation, with no A01 repetition.

The [new private host build](D:/TestData/CopperOSCommands/BuildSnapshots/dos-makedir-exact-quote-host-20260830T152136Z-86a5c8b1/host-build-receipt.json)
passed with SHA256
`E6B0CADE35AA0F0BF428F32F266469DA9FAE37A0420C490750252FC62B2A9130`.
The actual test DLL is
`AA864F1091766CA15B7BB651BC1E6C7B662223BB9124091891597E7A23416E36`;
the emulator remains `453DE5...`. It preserves all frozen helper and dependency
inputs, adding only this fact. The unchanged source-bound `012c05...` command
artifact is reused without rebuilding the command or compiler.

Discovery and execution report **one test passed, zero failed/skipped**.
Both commands return **20 with immediate IoErr 120**. The original native
ReadArgs fails with 120, followed by one PrintFault(120,NULL) and no FreeArgs.
The original takes 5,062 instructions including 50 command instructions;
the generated probe takes 5,229 including 173 command instructions. Its one
four-byte result-slot allocation is freed once. There are no new exceptions,
outstanding tracked command allocations or cleanup failures. The same native
DOS/no-gateway, NULL-D3, SP/process/input restoration, DOS lease and immutable
code/vector/argument checks pass, with all bound host/artifact inputs stable.

The [exact A03 receipt](D:/TestData/CopperOSCommands/BuildSnapshots/dos-makedir-exact-quote-host-20260830T152136Z-86a5c8b1/results/private-original-dos-makedir-exact-a03-receipt.json)
is `42383F47CF3EF2A1F1C2738592245464EF47CEA4F738DA46BB796DFAD573F591`;
the [TRX](D:/TestData/CopperOSCommands/BuildSnapshots/dos-makedir-exact-quote-host-20260830T152136Z-86a5c8b1/results/private-original-dos-makedir-exact-a03.trx)
is `C4EA480D39C74D381EE0EF47A2CFE25AA88D32681C7646E013B40BF3B29B53E2`;
the [11 KB compact result](D:/TestData/CopperOSCommands/BuildSnapshots/dos-makedir-exact-quote-host-20260830T152136Z-86a5c8b1/results/original-dos-makedir-exact-a03-compact.json)
is `EFEA2A1F28F057FAF0AC9CB04585FEA362A2517EA3616D6B68AE3BEC60D611EF`.
This adds **one original, one generated, two RunCommand calls and one
comparison**, separate from the preceding four-call variant and three authored
observer calls. Exact A01/A03 now have original-DOS evidence for 68000 only;
NIL: output bytes, filesystem behavior, resident/P admission, current compiler,
generated DOS RunCommand and full boot remain outside this qualification.

### R07 RAM-input prerequisite: retained native wait before any file opens

The new `KickstartRomDosRamInputReadinessTests.cs` source is
`5041A564B5F349D8360212462A0066F87CF2ED6D03CA9BF528EB9DFFEB6F4B56`.
It attempts public original DOS list locking before creating, writing,
closing, reopening and seeking a private guest RAM: file. Only after real
FGetC buffering would it qualify a stream prerequisite. It never fabricates
FileHandle metadata or substitutes NIL: for unread data.

The frozen host is
`D:/TestData/CopperOSCommands/BuildSnapshots/dos-ram-input-host-20260830T153120Z-cadadbb8`.
Its capture is `2F79148C2F6458A85C98BB3253B796A7AA591E8E8F4691D832F2CA1B90835D21`,
build receipt `4622081DCB8D6A5834A0EE37741174A9A89D0E6172FA120141D93406B692A28B`,
test DLL `D4C7295B333435623EE456578E19B2B4835A43E50A1BF8EEF0446E668AA87156`,
and emulator `453DE5EAAC3FC284CE954B1B10E0D08B619C7D9100A06B583C806A412BE0EDBD`.
Its 126 source/settings files and 40 dependency DLLs use the established
private host build and unchanged licensed ROM identity.

One test executed and **failed; zero skipped**. Public Cli, Input, Output and
GetArgStr queries returned with IoErr zero. The next call, original
`LockDosList` at LVO -654 with D1=29 (`READ|DEVICES|VOLUMES|ASSIGNS`), entered
its native target `0x00FA4564` through vector `0x00205826`. It called original
Exec `ObtainSemaphoreShared` with A1=`0x00000014`, followed by Wait with
D0=16. The call reached a stopped native CPU at `0x00F81476`, SP
`0x0007CEA0`, real task `0x00205EE0`. Of the unchanged 1,000,000 trace
iterations, 999,862 observed that stopped PC. No new exception or executed
host gateway was recorded in this call.

The acquisition never returned. No NextDosEntry, RAM: Open, guest file
creation, ReadArgs, RunCommand or command invocation occurred. No guest
cleanup calls were issued after this unsafe continuation; disposal of the
machine is not evidence of guest lock unwinding or qualified cleanup.
The requested R07 gate remains open.

The immutable receipt is
`results/private-original-dos-ram-input-readiness-receipt.json`, SHA256
`C74F4766C269B22A193B7475AD235FB7449F34DF59DA1538CDA28C46587760E7`;
TRX `F7143110E7442872C2AC803ECE5A6FCDFE413D270EC7D639E8566E1EC4FECB86`.
The separate 17,436-byte compact failure record is
`results/private-original-dos-ram-input-readiness-compact.json`, SHA256
`33F5AD09B8C5CF3F5CC24D37AC0318F03374EB98B24750CC427C624F00C447D0`.
Inputs stayed unchanged. The low semaphore address alone initially suggested
missing DOS startup state; that was an inference until the next capture.

### Read-only root capture: original DosInfo has not been published

A fresh, separate diagnostic calls the same established licensed bootstrap
and then only reads published memory. It does not call LockDosList again.
`KickstartRomDosBootstrapRootReadinessTests.cs` is
`6A1BCEB6729D150A85F1ADA0353D920E26B278BA00EA1442C63ED957D88243DE`.
The frozen host is
`D:/TestData/CopperOSCommands/BuildSnapshots/dos-bootstrap-root-host-20260830T154956Z-6d4a8630`:
capture `AA116F6A1235E5AE2BA87859664FE90B9D228AFF80CF36C5BB15445E7C4325D5`,
build `8BA570CDD7086C6CFB255032BC7E1F30A722CAA519E78AC5197F9D44F9645AB0`,
test DLL `A56554722E672AED0B8C439F46C79248DD1F59FB507E9B91F3E0D278311FF230`,
and the same frozen `453DE5EA...` emulator.

| Observed field | Original value |
| --- | --- |
| dos.library before direct InitResident | Not published |
| Published dos.library | `0x00205AB4`, version 40.3 |
| `dl_Root` field, public offset 34 | Field `0x00205AD6`, RootNode `0x00205AFC`, mapped |
| `rn_Info` field, public offset 24 | Field `0x00205B14`, BPTR zero |
| Decoded DosInfo | Absent; no semaphore object was dereferenced |
| Direct initialization returned to its caller sentinel | No; PC remained `0x00F81476` |
| Original DOS vectors | Unchanged, no DOS host gateway |

Offsets and BPTR interpretation follow the original NDK3.1
`dos/dosextens.h`, SHA256
`D65038A5297FFA91E3AB1BA063DCA102580DE63B14C071A1158D4E9ECDFB2F50`.
`DosInfo.di_DevLock` is offset 20. This separately observed zero `rn_Info`
explains why treating base zero plus that offset as a semaphore cannot prove
a valid DOS list lock. No synthetic DosInfo, semaphore or private state was
created. In particular, low memory at `0x14` was not read as an object.

The test **fails one of one, zero skipped**, because the required root chain
is absent. Zero explicit DOS calls follow bootstrap, and zero commands,
ReadArgs or RunCommand calls are credited. Its receipt is
`results/private-original-dos-bootstrap-root-readiness-receipt.json`, SHA256
`334B8FA41E017F43A9E2B3B038EEC26A745573E98A1A61A36BA97246CE46598C`;
TRX `35BF33EB4AFB3E9D7001423C0789F5390BF6A2E5D5EDBFDFB1E8C9E160BA5928`.
The 4,952-byte compact record is
`results/private-original-dos-bootstrap-root-readiness-compact.json`, SHA256
`A87621AB56AFE215B1BFCF73C5516E14D0AD500C79B7E37D36B45A7DAD10D684`.
All bound inputs stayed unchanged.

The frozen fixture source explains the distinction: `InitializeLicensedDos`
intentionally accepts library publication during a bounded direct
InitResident call, clears the CPU wait latch after that call exhausts its
budget, and permits subsequent direct vector invocations to replace PC/A7.
It does not run the remaining normal filesystem startup. Those existing
component semantics are preserved; they cannot establish a usable DosList or
handler. The diagnostic reports the post-helper CPU state, after that existing
latch clearing, rather than claiming it observed the original STOP latch.

The available normal route is `StartKickstartRomBoot(disk)` on a new machine,
which starts the licensed ROM reset vector with an unchanged write-protected
disk and without the initial synthetic host shim. Its normal boot boundary
advances device time and hardware interrupts. `StartBootFromDisk` and
`StartWorkbenchSession` instead install synthetic host sessions and are not
substitutes. Kickstart31 ROM mode subsequently enables its existing host
Exec/device takeover, so that provider policy must remain explicit even if
native original DOS later publishes its root/list state.

### Passive original ROM boot publishes DosInfo within the existing cap

`KickstartRomDosPassiveBootReadinessTests.cs`, SHA256
`09C5772DDDF9BEA2FF1A901FD89A9E156DC120CD4B1402753EE54FFF17E43349`,
uses a fresh licensed 40.63 ROM machine with the same public 2 MiB fast-RAM,
AccurateM68000 and LiveAgnusDma settings. It starts the original reset vector
with the unchanged Workbench 3.1 disk attached. After reset the fixture never
patches CPU registers/STOP state, calls direct InitResident, invokes a DOS
vector or creates private DOS structures. It observes memory only between
normal boot-controller chunks and stops at the first valid root publication.
The existing bootstrap limit remains **32 chunks of 250,000 instructions**;
the unchanged 1,000,000-instruction direct-call limit is not used by this test.

The explicit licensed archive is
`D:/TestData/TestImages/Workbench v3.1 rev 40.42 (1994)(Commodore)(M10)(Disk 2 of 6)(Workbench)[!].zip`,
403,062 bytes, SHA256
`D93611887ACF91F68F5608A5B7812F03EB16F743F40451B67C93067B04C967ED`.
Its exact same-named `.adf` member is 901,120 bytes, SHA256
`A8F167BAD2897E8C7F2CFE73DE7A9F8DAD10C7A5B1F78CA17F818AB17278B985`.
The image is loaded only into memory; DF0 remains write-protected and its
final bytes match that hash. No ROM or reference disk payload is stored in
the repository or host source snapshot.

The accepted private host is
`D:/TestData/CopperOSCommands/BuildSnapshots/dos-passive-boot-host-20260830T161008Z-336125de`.
It preserves the 119-file baseline plus four frozen helpers and this one new
fact, with 40 unchanged pinned DLLs. Capture SHA256 is
`83DC5E19113D1F16FA8E9A795469D71ACC1CFA77BC9E9132326DF89A956AE67F`,
build receipt `767845AA1FCA0B6B73B705704280019EB4FE4401DCC81C41A3683AA630BE3B71`,
and executed test DLL `76FA661CAD349DF9F104CC0505438F91AC42B2DD1CA3B896C753FF6B74C332D4`.
The emulator stays `453DE5EA...`; its pinned core is
`5A1B03D93CDB65E9706D3D8C7FFE12219D293E1766A593F4937C2A7EFAADA9A7`.

`boot-provider-source-binding.json`, SHA256
`52FDF54DC80F1A2BEDE7FF943D0DAAC74FEF7C2F147F6C91CAC8168300978656`,
binds six copied source files outside the test compilation tree:
AmigaBoot, AmigaDiskImage, BootInstructionBoundary, ExecutionBoundarySchedule,
machine Configuration and the floppy Controller. Their SHA256 document
checksums match portable PDBs whose GUID/stamp match the actual two frozen
modules. The matching emulator PDB is `8AB3C4A431EF026CAD077B8A10C39669A8E5DB72C6A39277532A6C4CE442D2AD`;
the matching core PDB retained in the emulator output is
`B17BC1DC7272BC57C021CA270ADCF0DD43C3D3C10FA956146E7EA36028C94255`.
The different current canonical-core PDB was rejected. This is a precise
source subset binding, not a new build or qualification of all provider code.

The earlier variant `dos-passive-boot-host-20260830T160707Z-837f8721`
is retained with a compile-only failure, log SHA256
`F2E57566E0683CAFEA3F745D8EB66D4D290FBB2279CC7A36EA6C9834BAB90FD6`.
It ran no tests. The new capture independently verifies an externally
observed two-identifier change from `ExecLayout.ExecBase.LibList` to the
actual SDK name `LibraryList`, with all other bytes unchanged. That derivation
is `passive-source-correction-receipt.json`, SHA256
`78BAE6EDE129F35D66B8F86246C284A43C349809D87C79090AB5F7A4C09C8FD5`.
The execution driver is the separately bound
`ReadArgsDifferential/run-private-passive-boot-host-tests-v2.ps1`, SHA256
`54AC0CC543BA4C1C9E467532E00DED4245B0F49F5BB14DF0D01837B130E7CA91`.
It records the actual bootstrap cap distinctly from the unused direct-call
limit and does not rewrite either earlier variant.

One test **passed, zero failed/skipped**, with first observed root readiness
at chunk nine. Chunk eight had a stopped native CPU; the normal boot boundary
subsequently progressed without a fixture latch change. The final observation
is:

| Public observation | Value |
| --- | --- |
| Original DOS / RootNode | `0x0020C53C` / `0x0020C584` |
| `rn_Info` BPTR / mapped DosInfo | `0x00080A87` / `0x00202A1C` |
| DeviceInfo list BPTR | `0x00083018`, recorded only; no unlocked node traversal |
| Three DosInfo semaphore observations | Empty wait queues, NestCount 0, Owner 0, QueueCount -1 |
| Native DOS vectors | 154 without host gateways; SHA256 `6D349D8FE3544F62221789B0139EDA1A27A1FC86CBBB0A2D8B33C1A9A7233D80` |
| Current real Process / SP / PC | `0x0020D6D8` / `0x00210604` / `0x00FCFD8A` |
| Current Process CLI / input / output | All zero |

The strict receipt is
`results/private-original-dos-passive-boot-readiness-receipt.json`, SHA256
`A8D6D96B93A04ED9D46D7C7BBC74D8089818140945A67D6989753A0950CFF58A`;
TRX `505334500BB07B7913955BF7853B8C475C7C314C48CD536D140C19BF959BBCFD`.
The 19,051-byte compact record is
`results/private-original-dos-passive-boot-readiness-compact.json`, SHA256
`EF0CCB4B3EC0C93337C66D997BD3AE3E632DACD5C89E71F99E9EFF1158E6AE79`.
All input, output, provider-source/PDB, media and driver identities stayed
unchanged. There are **zero explicit command/DOS/InitResident calls and zero
command coverage credits**. Autonomous startup-command count is unknown;
the passive snapshots are not a command-execution trace.

This establishes a concrete boot route to original DOS root publication,
including device-time advancement missing from the direct component call.
It does not establish a safe application caller, working LockDosList, original
handler operation, seekable RAM: stream, R07 restoration, complete Shell
startup, full boot or hardware/performance qualification. The separately
recorded checks below test public list readiness from this actual Process
without assuming that a nonzero CLI is required for a list query.

### Continued passive observation does not supply a current CLI/stream context

The separate `KickstartRomDosPassiveCliReadinessTests.cs`, SHA256
`9D6DB375E0AFC8EAE132A4A455FFCB3F5D75E73CC9B282ED55BE8A2DF4EEF1B8`,
retains the accepted root helper unchanged and continues the same normal ROM
reset route until a real current Process has a mapped CLI and selected
input/output handles, or the existing 32-by-250,000 cap ends. It never picks
another task-list entry as the current caller or writes Process/FileHandle
fields to satisfy that condition.

The frozen host is
`D:/TestData/CopperOSCommands/BuildSnapshots/dos-passive-cli-host-20260830T162127Z-63333419`:
127 source/settings files, 40 pinned DLLs, the same ROM/archive/image hashes
and six compiled-source/PDB bindings. Capture SHA256 is
`729698B6ECC7E63DB42CEB5DC328844E53219930BCD2BF3B1E5C729AAF15651A`,
build `64D3A8E22845EEAADE2F0ABF4FDB04570BC9C51EF68A5E265984D0BBFFD75EB5`,
test DLL `B92F67607DC2FEB3BE5B3AC6770BC1430561C4FB9A668CF6DF399E8A31469941`,
and provider-source binding
`6CCF1FDE01BDB70AB273EEC8F26373EE5D4D0439F4E8D7A5BAB323748A5FC19A`.
The parent-helper derivation is separately recorded as
`96CD0E194117309F8B16C2915E76505D3D701DFDF048AACCD44FF1F67FBA8BBA`.

The test **fails one of one, zero skipped** after all 32 bounded chunks.
RootNode/DosInfo stays valid from chunk nine through the final observation.
All 154 original DOS vectors remain without gateways and retain the same
negative-table hash in every captured published state. The original disk
stays write-protected and byte-identical, and all bound inputs stay unchanged.
The sampled current real Process remains `0x0020D6D8`, with CLI, input and
output BPTRs zero at each checkpoint from nine through 32. These are sampled
states, not proof that no other transient context existed between them.

Later sampled PCs repeatedly lie in original Exec around `0x00F814xx`, with
some samples at `0x00F828B2/B8` and `0x00F80BBE`. The final state is PC
`0x00F81420`, SP `0x0020227E`, SR `0x2008`, cycles 118,258,656; the CPU is
neither halted nor stopped. No fatal boot diagnostic was emitted. This is a
bounded missing-context observation, not proof of memory corruption or a
reason to silently lengthen the cap.

The retained receipt is
`results/private-original-dos-passive-cli-readiness-receipt.json`, SHA256
`D5668FA2F8E39990C3BF22E5E4556EBD0F1EC64A02DB9735E9A4CC5D7228B97A`;
TRX `3A024F22E1FAD72657595385A7102F63C6AA456FD2981B93A47D0B2CC8681D33`.
The compact 25,309-byte record is
`results/private-original-dos-passive-cli-readiness-compact.json`, SHA256
`EFDA1A47CDE2E72E3A9C03AB9EDD0C8A537E684799FAF11614BC6305C4EF3F67`.
There are zero explicit DOS/InitResident/command calls and zero command
coverage credits. Autonomous boot-command count stays unknown. This failure
does not undo the separate root-publication prerequisite; public DOS list
queries can next be tested from that valid root/Process checkpoint without
inventing a requirement for a nonzero CLI.

### Public DOS list queries pass at the initialized boot checkpoint

The separate `KickstartRomDosInitializedListReadinessTests.cs`, SHA256
`C6E4E2F212E61E3324B86317D469EC300C95D760976F8063090D9E50E858D136`,
reuses the unchanged passive root helper, original media and frozen runtime.
It stops at the first mapped RootNode/DosInfo under the existing 32-by-250,000
boot cap, verifies the real current Process, and invokes the original public
DOS vectors without requiring a CLI or synthesizing one.

Each call borrows the actual current PC and active stack for its return
frame. Every step uses `ContinueExecutionUntilCycle(..., maxInstructions: 1)`
through the established boot hardware/task boundary, under the unchanged
one-million-step call cap. This permits normal device, interrupt and scheduler
progress during a wait. The fixture never clears STOP/HALT or writes private
DOS structures. It restores only CPU task context after a normal return to
the same Process and SP; elapsed machine time is not rolled back. A failed
continuation would remain failed without synthetic repair.

The private host is
`D:/TestData/CopperOSCommands/BuildSnapshots/dos-initialized-list-host-20260830T164602Z-64cfdf62`:
127 source/settings files and 40 pinned DLLs, with the same ROM/ZIP/ADF and
six provider-source/PDB bindings. Capture SHA256 is
`3003A8D77E5F7154D17C1A39BA32C2957E041B53457B8D20EA943832187D11F0`,
build `7589B533CF5D05CCC39280ABD9C5719A7BF8FDD094889BC2E994B54EF2B33ADD`,
test DLL `C01168A6684ACE64D24E3A324860C55A5D10699A82BE6A5D5635195378D691BE`,
provider-source binding
`DADD93FA42F6E7AE730BB2CF1952EBDC110E5FAD23E4D9787E8865F349A7EEE0`,
and parent derivation
`01B616410DAC0F29C41903E8749B9BA042359F0E4C5B3191474A6D8AD506E40C`.
The private restore/build and exact one-test discovery pass without any
shared production/compiler/provider build.

The test **passes one of one, zero skipped**. At chunk nine the real Process
is `0x0020D6D8`, PC `0x00FCFD8A`, active SP/USP `0x00210604`, SR `0x0718`.
Public `Cli`, `Input`, `Output` and `GetArgStr` all return zero, matching the
actual Process fields. `LockDosList(0x1D)` returns the special cursor
`0x0020C061` in 100 boundary steps; this odd cursor is passed to NextDosEntry,
never interpreted as a DosList node. Nine `NextDosEntry(cursor, 0x1C)` calls
return eight nodes and then zero. `UnLockDosList(0x1D)` returns in 58 steps.
Public IoErr is zero immediately after every observed query. The final
SetIoErr/IoErr pair restores and verifies the borrowed value.

| Locked list name | Public type | Handler port |
| --- | --- | --- |
| Workbench3.1 | 2, volume | `0x0020D734` |
| RAM | 0, device | zero |
| CON | 0, device | zero |
| RAW | 0, device | zero |
| SER | 0, device | zero |
| PAR | 0, device | zero |
| PRT | 0, device | zero |
| DF0 | 0, device | `0x0020D734` |

These names and fields were copied only while the public read lock was held.
The shared DF0/volume port equals the current Process plus the public
MsgPort offset 92. This supports the inference that the checkpoint belongs
to the filesystem handler, rather than an application CLI. It does not prove
that RAM's registered handler can be started or that any handler operation
will complete. No self-directed filesystem request is attempted.

All **32 explicit DOS vector calls** return to the same task/PC/SP, preserve
the public nonvolatile registers and stack banks, and execute no host
gateways. They total 747 boundary steps and advance the recorded machine
clock by 9,466 cycles; these are diagnostics, not a timing qualification.
All 154 DOS vectors, the borrowed Process fields and write-protected disk
bytes remain unchanged; the read lock is released and cleanup has no errors.
The full receipt is
`results/private-original-dos-initialized-list-readiness-receipt.json`, SHA256
`9AFD3D02A91B593D6A4005BEDB378BC28977872F4E9BC989904B52CF26270574`;
TRX `75EC18908578030F3013E541E5D92D0B4A4DB6F6F5B22426B0FB12D3A6EA297D`.
The compact 48,489-byte record is
`results/private-original-dos-initialized-list-readiness-compact.json`, SHA256
`9E5E49AAB6591D1193352734D2B71EC67B01A7342AA5F8D1292264508D0BD1FD`.
All bound source, runtime, package, evidence and media identities stay stable.

There are **zero file opens, ReadArgs, RunCommand or explicit command
invocations**, and zero command-coverage credits. Autonomous startup command
count remains unknown. This closes only the initialized public list-query
prerequisite. The retained direct-InitResident and 32-chunk CLI failures are
unchanged; working streams, original handler behavior, R07 restoration,
application process ownership and full boot remain open.

### Passive task topology exposes a separate scheduler-list prerequisite

`KickstartRomDosBootTaskTopologyTests.cs`, SHA256
`1D0CCDF760876E58279837012A7637B3FE4BFBCC4E5A1846DD0A23569E634163`,
adds only read-only public Task/Process/list observations at paused boot
boundaries. It retains the same 32-by-250,000 cap, media, runtime and passive
root helper. It does not call Exec/DOS, clear CPU state, select another task,
or repair a list. Each list walk records termination and predecessor checks;
an intermediate guest list update is not automatically classified as a fault.

The private host is
`D:/TestData/CopperOSCommands/BuildSnapshots/dos-boot-task-topology-host-20260830T165848Z-47c5a0f8`.
Capture SHA256 is
`7C11FAED53E76C005A88D0D8FA362EFBB8E0186F309FE1F9749A8AEDDDFE5EBD`,
build `8B904A77D0E892D4668CCA4191F4A6DDF3B724C486943010B0B9D2C713CE684E`,
test DLL `E548F54093228E7AECE2C239E570492B20DFF8722C128C7E92F407F9DA55925A`,
provider-source binding
`69F2ED7ACD00DE0A5B0610214335680F34F840957B2102E7DF545CFAA5325763`,
and parent derivation
`887DD76FD88BD437A23F0A1A183950DD3C3CE710AF46F4C74883C4E718B070A9`.
The private build and exact discovery pass; the **observation test passes
one of one, zero skipped**, with unchanged bound inputs and disk/vector bytes.
That pass is separate from the **failed later scheduler-list validation**.

At chunk nine both bounded list walks terminate at their own tails with
consistent predecessor links. The actual current Process is named `DF0`,
priority 10, address `0x0020D6D8`; its message port points back to that Process.
The first ready list contains DF0 and `Initial CLI`, a separate NT_PROCESS at
`0x0020C968`, priority zero. Initial CLI has CLI BPTR `0x00080A2E`, but its
input/output fields are zero. The wait list contains `input.device`, priority
20, and `console.device`, priority five. No existing task is repurposed.

At every later sampled checkpoint, ten through 32, the ready/wait predecessor
or tail checks fail. The ready walk reaches `0x002009B8`, exactly
`ExecBase + TaskWait + 4`: **the wait-list tail sentinel, not an unnamed
Task**. The raw node-shaped decode in the full capture at that address has
no Task meaning. DF0 appears in the ready/wait chains while ThisTask remains
DF0; Initial CLI is no longer reachable in those walks. The failure is not
explained away as one interrupted list update, but its first writer and
responsible owner have not yet been established. No memory or scheduler
repair was attempted.

Receipt `results/private-original-dos-boot-task-topology-receipt.json` is
`47C2C31ADE2B5AE2E549906239A63F994318FCABEA29A00276E49D08B48B4880`;
TRX is `C9CC52D2AE4FE5E90743BE596F3088DB67A92C9DE6174585312C5D73C6CA3FE2`.
The 26,772-byte compact analysis explicitly distinguishes capture success
from list failure and annotates the foreign sentinel:
`results/private-original-dos-boot-task-topology-compact.json`, SHA256
`7CC636255A51287779AADC5EC4B4B179D9E6F2907D78701D7D879EBEBB808A76`.
There are zero explicit Exec/DOS/InitResident/command calls and zero command
credits. The next bounded trace must locate the first transition within the
existing chunk-nine-to-ten instruction interval; it must not increase the
cap, modify providers/ROM/CPU behavior, or manufacture valid list links.

### Task-list trace: strict historical-PC entry check rejected

The first new trace fixture was built and run in
`D:/TestData/CopperOSCommands/BuildSnapshots/dos-task-list-transition-host-20260830T174347Z-a80ff68d`.
It copied the same 119 baseline C# files, four accepted readiness/observer
helpers, the immutable root and topology helpers, and one new trace file:
128 source/settings inputs and the same 40 pinned dependency DLLs. The new
source was `B7CF8B8BF7034887ECCA21CE037EDD8392AEBB48726869BAFED5A172BD02AA84`;
capture `3A542B1E4BF9984BE2DEDFB5D96137F6934F7E39E33AF82EAE4DE63E518A07BB`,
build `C9A21B2E8665EC14B76B65C89C81868C5AB713921EFEF240E2C031B21E6F59C1`,
and executed host DLL `307E06DD1B4373FFBB8545C23ACECBBF6433770A88822BA057231D7B7E8B36F2`.
Private restore/build passed without a provider build.

The single test **failed before installing diagnostic listeners**. At chunk
nine the ROM, media, Exec/DOS/Root/DosInfo and current DF0 addresses matched,
but PC was `0x00FCFEE2`, SP `0x0021060C`, and cycles `20132272`, versus the
earlier `0x00FCFD8A`, `0x00210604`, and `20132394`. No list-write trace or
command coverage was credited. All captured source/runtime/media inputs
remained stable. Preserve the failure receipt
`B4272EF71E7705D9BA526E723DA72B18D5F3158DB15DE579E7A1432CA709049A`
and TRX `17FF2A1A6DE06982C7351C285BFE31CDB2D08B605DB2F78D3447C9FB50DDDB47`
under `results/private-original-dos-task-list-transition*`; this is one
failed test, zero skipped, and zero explicit Exec/DOS/command calls.

The six earlier boot/boundary source files still match the frozen provider
PDBs. Current Bus.cs, Timing.cs and M68kCore.cs inspection copies do **not**
match their frozen 5A1B03/BD42669 document checksums. Their separate binding
`842FB56F4CBC35F92700B540EBE20933DF0EEDB5082F7693620991220066F940`
retains those three mismatches; they cannot establish the frozen runtime's
implementation order. The actual callable tracing APIs compiled against
the pinned runtime, but their execution remains to be observed.

There is an identified unpinned input: PDB-matched Configuration.cs enables
the RTC for A500Pal512KBoot and supplies no clock override. PDB-matched
Machine/RealTimeClock.cs, SHA256
`6D08F913423EED8CE63634656141FFBDEA6A64F9D59DB370F978B20F4236C30F`,
defaults to `DateTimeOffset.Now`. That is a plausible source of boot-position
variation, not proof that it caused this particular 122-cycle difference.

A separately captured next variant may therefore use a structural entry
condition: same fixed runtime/media/profile and chunk-nine bound, an actual
mapped named DF0 Process, valid own-tail/predecessor links in both lists,
no task shared between those lists, and the expected real Initial CLI with
nonzero CLI plus input.device/console.device members. It must record the
actual CPU state and explicitly avoid claiming equality with the historical
PC/SP/cycles. This establishes a consistent topology from which to locate a
first transition; it does not qualify scheduling. The 250,000-step trace
bound and 256-step follow-up remain unchanged, with no provider/CPU/ROM or
private-list writes and no application Process creation.

### Task-list trace: native first writers captured, scheduler still fails

The separate structurally anchored run is frozen at
`D:/TestData/CopperOSCommands/BuildSnapshots/dos-task-list-structural-transition-host-20260830T175127Z-041fbf74`.
It retained the same 128 source/settings inputs and 40 DLL pins, changing
only the new trace fixture's explicitly documented entry check. Its source
is `6971C08744C4596D67F9A90CEB8D769A10F2FD9148E70784200B4B91CCF4F801`,
capture `921401A55B8A083B0BCE6979061C8C49FD63AF79E0484153CDD5F03B3C0E5A1E`,
private build `CD121AB7AA85F88A9DD2ABAA9702EEFFEA4132FE2E9EEC67054634E26581D345`,
and host DLL `B55D29A479C4E0A0735B492629E3E079F703BCDD5C3DB824F6822AE47D2F7EEB`.
The private restore/build and single observation test passed, with zero
skips and stable bound inputs. This run happened to match the old CPU
checkpoint too; equality was recorded, not required or retroactively
assigned to the earlier failed run.

The first inconsistent link is a real native CPU write at step 39,562:
ROM PC `0x00F819E2`, `MOVE.L D0,4(A1)`, writes DF0's predecessor
at `0x0020D6DC` from ready-head sentinel `0x002009A6` to DF0 itself,
`0x0020D6D8`. D0 and A1 both equal DF0. The completed write cycle is
`20609958`, SR `0x2700`, SP `0x00202266`, and ThisTask remains DF0.
Two instructions later, step 39,564 at `0x00F819E8`,
`MOVE.L A1,(A0)` changes DF0's successor from Initial CLI `0x0020C968`
to itself. The ready walk now loops at DF0 and loses Initial CLI.

The preceding native trace reaches Permit, Supervisor, the observed
Schedule vector (`ExecBase - 42`, target `0x00F813A0`), and the priority-list
insertion at `0x00F819C6`. None of that recorded 64-instruction lead-in
executes a host gateway. **DF0 is already both ThisTask and the ready-list
head before this sequence.** The writes therefore establish a native
double insertion; they do not establish why that earlier overlap exists
or attribute it to Permit, a CPU defect, host scheduling, or task reaping.

The later foreign sentinel appears at step 68,379, original Wait PC
`0x00F828B8`: `MOVEM.L D0/A0,(A1)` writes the wait tail `0x002009B8`
into DF0's successor while the ready head still points to DF0. Its completed
first longword write cycle is `20969136`; SR is `0x0710`, SP `0x002105C8`.
The trace stops at step 68,635, exactly 256 steps later and below its
250,000-step cap. The final ready walk still reaches the wrong sentinel,
and the wait walk includes DF0 with an inconsistent predecessor.

There are 68,635 actual retirement callbacks, 170,979 CPU phase callbacks,
and 620 watched-field mutation events. Every changed field overlaps its
associated observed native CPU write. No watched change was observed only
at a host boundary or gateway retirement. No later mutation snapshot restores
both valid lists; all reached task nodes belong to the four watched tasks.
The fixture also records 561 gateway retirements elsewhere in the interval,
with callback identities, so it does not claim a provider-free boot. Both
diagnostic delegates were restored, all 154 original DOS vectors remained
native, the selected Exec vectors and image bytes stayed unchanged, and
read-only captures preserved CPU state. There are zero explicit API or
command calls and zero command credits.

Receipt `results/private-original-dos-task-list-structural-transition-receipt.json`
is `E6C7E1508BBB4B228CA3A4C0DB94B76962C021E470CD183087F584FE701AE992`;
TRX is `FA9E34EECEB070D362BADB6E404C3A411E1463EA214DA6471229F7E7BF6760A2`.
The 142,292-byte compact causal record is
`results/private-original-dos-task-list-structural-transition-compact.json`,
SHA256 `8322CBF0CF43F58B204765D6093B144D50D6607C9BADE6D7AC8D8A2CC0CE8AEE`.
It contains the before/after links, native register/call tails and separately
verified writer mnemonics, without copying ROM bytes. The four-file hook/RTC
inspection binding `70C162D86819A4F9E1803A5AC3E9C2E2CF113033ADDB8B95F15E5DCCF74F6E2F`
still records the three current-source/PDB mismatches; RTC is the additional
matching source. These results qualify the observation, **not** the scheduler,
an application Process, R07, or full boot.

The upstream interval is now bounded precisely by existing observations.
At checkpoint eight, after eight boot chunks capped at 250,000 instructions
each, ThisTask is `0x00203290` (NT_TASK), PC `0x00F81476`, and no DOS library
is published. At checkpoint nine, ThisTask is DF0 and it is already linked
in ready with state **READY (3)**, not RUNNING (2). The current trace never
changes ThisTask. Its first later write of RUNNING occurs at step 39,602,
after the self-links already exist. A new earlier capture must therefore
observe DF0 publication/dispatch within the checkpoint-eight-to-nine interval;
the current trace cannot identify that missing first overlap. No list repair,
CPU/provider change, increased cap, or application Process creation is allowed
as a substitute.

### Earlier dispatch trace: first native dispatch is correct; later host boundary requeues DF0

The missing checkpoint-eight-to-nine interval is now captured separately in
`D:/TestData/CopperOSCommands/BuildSnapshots/dos-task-dispatch-origin-host-20260830T181810Z-bfa48bad`.
The preceding `041fbf74` trace and failed exact-PC preflight remain unchanged.
The new fixture is `KickstartRomDosTaskDispatchOriginTests.cs`, SHA256
`F6D09B7E12257184162D86915771489E4CF0BBC10C858013E54B47CD0F7C11E7`.
Its private capture contains 129 source/settings inputs and the same 40 DLL
pins: capture `9DC40C88EFAEC794EBE3F1466CDC699B813002B7884E3F7D18DFCA738BAE0127`,
build `EAAF5220721B956BCDDF63DBC9096DC631AD64FAC14A6B8E93AAFA0C5328FBA4`,
and executed host DLL `35764E5836156723CE6AF485ACAF98E6265FD6E3D8A316CE1FF5D6E6DB344840`.
The one observation test passed with zero skips and stable bound inputs.

After eight ordinary boot chunks, the fixture independently validates the
actual current task and both public lists, with no published DF0. It then
uses exactly 250,000 public `ContinueExecution(1)` boundaries. This preserves
the public boot runner's behavior while the CPU is stopped; it does not
equate a requested instruction budget with a cycle or a retired instruction.
There are 38,838 actual retirement callbacks, 88,253 CPU phase callbacks and
211,165 boundaries starting with STOP set. The fixture never clears STOP.
It discovers tasks only through ThisTask and valid ready/wait walks, retains
their addresses for observation, and composes/restores the prior listeners.

Three observations locate the first overlap:

1. At step **236,571**, original PC `0x00F819E8` publishes DF0 `0x0020D6D8`
   on the ready list. Initial CLI `0x0020C968` is still current. Both lists
   have valid own sentinels and predecessors.
2. At step **236,816**, original PC `0x00F81480` writes DF0 to ThisTask.
   The ready list is already empty. The original first dispatch therefore
   removes DF0 from ready before making it current; its initial publication
   is not the source of the later double insertion.
3. At step **244,433**, the first CPU phase of the next boot boundary sees
   DF0 both current and ready. Between the preceding return and this fetch,
   ready-head changes from Initial CLI to DF0, Initial CLI's predecessor
   changes to DF0, DF0's state changes **RUNNING (2) to READY (3)**, and its
   SigWait changes from `0x40000100` to zero. None of these four changed
   fields overlaps an observed CPU write. The entry state was ROM PC
   `0x00F808F6`, SP `0x0020F028`, SR zero, cycles `20070656`; the first fetch
   is original Schedule's vector at `0x002007E6`, with SP `0x0020F024` and
   A6 changed to ExecBase. This locates the mutation before native Schedule
   begins executing, within host processing at the instruction boundary.

The immediately preceding actual gateway retirement, step 244,432, is
`TrackdiskDeviceServices.BeginIo` at `0x0020895E`. It returns normally to
original SendIO at `0x00F808F6`. The trace's nested delegate descriptions are
captured callback references, **not a managed call stack**. They do not prove
which reply or signal callbacks ran. The first overlap persists through the
end of this interval; the later `041fbf74` capture independently records the
native double insertion and broken list links that follow.

Of 580 mutation/discovery events, 575 overlap native CPU writes, four are
seen at gateway retirement, and one is the first-phase overlap above. The
four retirement-only changes touch the old `exec.library` task's storage
after removal and reuse. Historical watched addresses are not assumed to
remain live tasks; those changes are not attributed to this DF0 failure.
All 154 DOS vectors observed at the final checkpoint are native, the checked
Exec vectors and media remain unchanged, and listener restoration/read-only
CPU-state checks pass. There are zero explicit Exec/DOS calls, commands,
ReadArgs or RunCommand invocations and zero added command coverage.

The immutable receipt is
`results/private-original-dos-task-dispatch-origin-receipt.json`, SHA256
`6C54BF384192CB2AD970D7BAEFE4144EEFAAB22E84D56D7AFA4BA515EAD1E140`;
TRX is `E46FECA39B7C922AA16A4B08F6D9C149D76F75A04F8CB9F40C586A342C33E4E5`.
The 409,484-byte compact record is
`results/private-original-dos-task-dispatch-origin-compact.json`, SHA256
`11ABDEEA187721D0C4EA91E28DCA07523AF69262A439C90AE9B0D0540E4CF57D`.
It preserves the first publication, dispatch, overlap, preceding gateway
and native instruction tails without copying reference binaries into the
repository. This run happens to reach the earlier checkpoint-nine PC and
cycle count; RTC-dependent exact-position uncertainty remains explicit.

A separate source audit binds 14 selected owner files to the actual frozen
emulator, Exec and Devices modules through their matching portable PDBs.
Its `signal-owner-source-binding.json` is
`4F9158CEC25E3E6E9330AB6444B848E7DDFFDF46A2C7F168A6CF73C306B0EE1D`.
The bound code connects Trackdisk ProcessPending, ReplyTrackdiskMessage,
ExecPortCore.ReplyMessage/PutMessage and ExecSignalCore.Signal. The compiled
SignalCore source `6534C4AAF767E103F29FB20ED3AC2FE455CF291DCC1D29732EAF317CC1F3330C`
uses matching pending/SigWait bits to clear SigWait and call MoveToReady
without first checking Task.State. That is a concrete source-supported
candidate for the observed RUNNING-to-READY transition, not yet an exact
captured host writer attribution. A separate unchanged-owner conformance
test and bounded callback observation are the next checks. No production
patch, list repair, application Process, scheduler pass, R07 or full-boot
qualification is claimed by this evidence.

### Signal state contract: unchanged owner fails running and ready controls

The original NDK 3.1 `exec.library/Signal` description distinguishes tasks
actually waiting for a matching signal from running or ready recipients.
The former can become ready; the latter retain posted signals for later use.
`Wait` documents consumption when the requested wait completes. Evidence is
bound to Developer CD SHA256
`5D6BFCB213F1395D4C95584DC94D0E36265BE355076DAE1710BD36FB4DFDBFF3`,
member `ndk_3.1/docs/doc/exec.doc`, extent 17816, 162,487 bytes, SHA256
`71DF8EEAB9F6B9873A38BEA96BEC34FD6E44B78AA2D67D8F1B28319C207DD2AD`.
The private paraphrase/section-identity record `original-signal-contract.json`
is `2D2075118FB2140E1918CF4972797E0D35D5BD8467BEC111EC18B320A78D2232`.

New owner tests `ExecSignalStateTransitionConformanceTests.cs`, source
`949F988EDBE7A055F9E586FAD4124B1B26D02BDB269CC67F3E0816F91F95AD14`,
reuse the existing sparse memory fixture. They deliberately construct
**synthetic component state**, not an original Process or boot checkpoint.
The running case retains old node links while both public lists correctly
exclude that task, as observed after native dispatch. Saved-frame canaries
detect writes that must not accompany a merely posted signal.

The private test-only build at
`D:/TestData/CopperOSCommands/Q/sigstatef9b82313` executed the unchanged frozen
169,472-byte Exec DLL `82981A0D8D07A5B26D04075A686CD72176144541AF4B059BCC20148853CA7C4C`.
No production project was rebuilt. Build succeeded without warnings or
errors; four tests were discovered and executed, with **two failures, two
passes and zero skips**:

| Initial state and input | Observed conformance result |
| --- | --- |
| Running; stale matching SigWait | Fails: becomes ready, enters ready, consumes posted bits, clears SigWait and writes saved results. |
| Ready; stale matching SigWait | Fails: consumes posted bits, clears SigWait, writes saved results and requests rescheduling. |
| Waiting; matching signal | Passes the existing owner's completed-Wait result and wait-to-ready transition control. |
| Waiting; nonmatching signal | Passes: accumulates pending bits and leaves state, masks, context and lists unchanged. |

The 20,480-byte test DLL is
`2A4C245A281E313D6D45CBC13B185493E003C4FA7D4DCC2B057FF1CA70BE0C7F`;
receipt `C125A1674EE79C41099D4BFEA9C000725B613372449A4AEC8A359169916E2557`,
TRX `D259A4EE9C9E62582F1A25342B64CC4AD97BD4FB4CB1AB897E958DE4B0B7BD41`.
Sources, resolved references and executed private output remained unchanged.
Two earlier setup attempts remain separate failures with zero executed
tests: `sigstateb92cc4ab` rejected the SDK's external generated Program source;
`sigstateb4d02cde` failed reference resolution because SpecificVersion inputs
lacked full assembly identities. The final variant copies that unchanged
package Program source privately and supplies the exact pin identities;
neither fix changes a test assertion or production code.

This establishes the component state violation. It does not yet attribute
the boot transition to a specific callback, qualify original/native Wait
frame compatibility, or establish a scheduler fix. The separately captured
Trackdisk callback variant must report its own result; no commands, original
ROM execution, R07 or full-boot credit come from these four owner tests.

### Trackdisk callback attribution: the reply requeues the running handler

The separate diagnostic variant at
`D:/TestData/CopperOSCommands/BuildSnapshots/dos-trackdisk-reply-attribution-host-20260830T185426Z-d22734da`
retains the completed earlier observers and adds only
`KickstartRomDosTrackdiskReplyAttributionTests.cs`, SHA256
`8BB10FCA6BEE5F538E5E94346E423ACAC1B44B191B2352E63E60F93D3864A82E`.
Its 130 source/settings inputs and 40 original DLL pins are bound by capture
`8529B656BADCC105F8749D4D2290D3FFF6D9BD3855E2B05008E5BF0473636675`,
build `947DE06E6403B745E87BC95FF185E8CC6F911DBD781257516FD8D63BA1501CA2`,
and host DLL `74D800E4991A5863E5ABDA3BD66B9D21006608155236C8F66611FA75B5996DB7`.
The same original boot, media, public 250,000-boundary cap and structural
checkpoint are used. The only interposition composes the existing instance
Trackdisk reply delegate, observes before/after state, invokes the original
once and restores its identical delegate in `finally`. This instrumentation
is explicit; the earlier uninstrumented `bfa48bad` result remains unchanged.

The single test passes with stable inputs and zero skips. Exactly **one**
reply callback occurs, at step **244,433**; the original is invoked once and
returns once. The actual managed stack includes
BootInstructionBoundary.BeforeInstruction, TryActivateKickstartRomExecServices,
ProcessHostDevices, TrackdiskDeviceServices.ProcessPending and
TrackDiskDeviceCore.ProcessPending. The last two have actual module/token
identities in the receipt; the Devices ProcessPending frame has IL offset
108. Unlike the prior reachable-delegate descriptions, this is an observed
managed call stack.

The callback replies to a successful 512-byte read on reply port
`0x0020D734`, signal bit 8. Before the original callback, its recipient DF0
is ThisTask, **RUNNING (2)**, absent from ready and wait, with SigWait
`0x40000100` and no received signals. After the original callback, the same
DF0 is **READY (3)** and at the ready head, with SigWait zero and received
signals still zero. PC `0x00F808F6`, SP `0x0020F028`, SR zero, cycles
`20070656` and ThisTask are unchanged across the callback. Native Schedule
has not executed yet. The callback effect therefore accounts for the first
running/ready overlap observed independently by the CPU-boundary trace.

The 14-file PDB-bound owner source record connects that original callback
through ExecPortCore to the unchanged ExecSignalCore already failing the
four state controls. This identifies the reply boundary and source defect;
it is not a direct managed memory-write breakpoint inside SignalCore.
Both diagnostic listeners and the identical original reply delegate are
restored, original vectors/media remain intact, and no private guest or CPU
state is repaired. There are zero explicit commands or Exec/DOS API calls.

Receipt `results/private-original-dos-trackdisk-reply-attribution-receipt.json`
is `D8803450F433FAE749718EAE772ACEE7D7C3622E9690FC542390920F39D57FBE`;
TRX is `4F9E8B503E8CE6CB6019CA367243C57E79666570D6DBCD8511D3EF9CEDDA7389`.
The 47,837-byte compact record is
`results/private-original-dos-trackdisk-reply-attribution-compact.json`,
SHA256 `78B31E8E7B40FFE283DE5803166E55071DF90CA0274997F5946D15EDA2081F26`.
This passes attribution only. A source-qualified Signal guard, fresh runtime
binding and actual later scheduler/boot execution remain required; it adds
no application Process, handler qualification, R07 or full-boot credit.

### Signal state guard: source-qualified component correction

The narrow correction in the actual owner,
`CopperStart/src/CopperStart.Exec/ExecSignalCore.cs`, posts received bits as
before, then returns without consuming them unless Task.State is Waiting.
Running and ready recipients retain their wait mask, saved context and list
membership. Only the already supported waiting/matching path can consume
bits and make a task ready. This does not change removed-task handling,
exception signals, native Wait frame conventions or the other scheduler
work. Source SHA256 changes from
`6534C4AAF767E103F29FB20ED3AC2FE455CF291DCC1D29732EAF317CC1F3330C` to
`906FD10487240242E07B81363E336DDE2240745112745364B70944DE14497D00`;
the delta is one state guard and two explanatory comment lines.

Private qualification at
`D:/TestData/CopperOSCommands/Q/signalguard52d400cd` rebuilds the full
**75-file production source set** matching the historical Exec PDB. The
before source set is unchanged; after replaces only ExecSignalCore. Both
use identical compiler/SDK metadata pins, explicit private Compile items
and no ProjectReferences. The copied original project/settings are retained
alongside the explicit private build projection. No shared production
output is used or overwritten. In particular, the rebuilt baseline DLL is
not relabelled as the historical 169,472-byte DLL.

| Bound component run | Result |
| --- | --- |
| Rebuilt before Exec, 168,448 bytes, `878E715CA128EAB26FC2BB399166B37BF0632F90B307F6BBCC5233BE30E0B1C1` | Four unchanged tests: two pass; running and ready cases fail. |
| Guarded after Exec, 168,448 bytes, `059BA449FC59B2CD13F88953A499A0F93416566360275F3BC2616691C8E6B1D9` | The same four-test DLL: four pass, zero skips. |

The test DLL remains
`2A4C245A281E313D6D45CBC13B185493E003C4FA7D4DCC2B057FF1CA70BE0C7F`.
Receipt is `E8680774A01B575D252BA5E4A608880167063D110DA1422ECF1357C4AD2978F4`;
before/after TRX hashes are
`4BFECA23C90F98B699D99A5208DBA361C8C77CC44A27E1BE4C1433992B7450B4`
and `C8418C83817F48A5830ACAF536431AA90BD864BA7EB82DD9D0219F5A11784046`.
All captured source, resolved reference and executed output hashes remain
stable. The separate `pdb-audit.json`, SHA256
`01570D8682A5364C5227E58A4A4E36FFAABF4264E034828C25C1AA65E01735E4`,
matches each PE to its PDB and all **171 source documents** across the two
owner assemblies and the four-case/16-control test assemblies. This
supplements the exact Compile and reference input inventories.

### Existing Signal/Port controls: malformed fixture retained, then corrected

The related run adds nine existing Task/Signal/Trap tests and three existing
Port tests to the unchanged four new state cases. Its first private run at
`D:/TestData/CopperOSCommands/Q/sigcontrols4c197a83` is preserved: the rebuilt
before owner has 14 passes and the same two conformance failures; after has
15 passes and one failure in `PutMessageWakesWaitingSignalTask`. That fixture
set SigWait but never set Task.State to Waiting, leaving the zero/Invalid
state. Its failure is a malformed control, not a new production regression.
The failed receipt is
`A895869FEE3C0809A17432D644E8889C125ECCD880C99968D4437EF25537A91C`;
the after TRX is
`60038D080CF11062861D6AE429F5B1D6C0119E9565418A5C116567A071DC595C`.

Only that fixture's setup is corrected to write TaskState.Waiting before
posting the signal; no assertion changes. The Port test source changes from
`99AE73036AAED66C8E938625E269646D707C3F5EC118FEF49461D53935218F58` to
`BA9E1F933F06C027C6C4C33CE83D8617ABC21A7BB8B9C2206E7DA564F02AFCE8`.
`fixture-state-correction.json`, SHA256
`26CB8705DCABC56120C4D46F68A3ADD46A34507BDDA8E7FAC91DA3AE162D2A34`,
records the single-line delta after the original failure was captured.

The fresh corrected run at
`D:/TestData/CopperOSCommands/Q/sigcontrols3d583c5b` executes the same
100,864-byte test DLL on both owners, SHA256
`C85D2D549D2775679C272085B31515EE6D4C347E7C8BACAA52E4554305ED03EF`.
Before again yields **14 passes and two expected conformance failures**;
after yields **16 passes, zero failures and zero skips**. Receipt is
`C6D938310EA2AF82148DFDF1BC0F02B31B328BB20B42D5FC853FFC8C8C1250D3`;
before/after TRX hashes are
`818E7EDBDF87BD66DA47C7003D2077D4A75378E82EC563E7B9DE25D93F6DD254`
and `3FB8068CCC5BEB1CF3EC8E851ACD5D7C604813D8282757F4754DAE1A09BE46FD`.
These controls use the existing frozen sparse memory helper and an explicit
24-assembly closure from the related Close test capture. Its SDK/compiler
pins differ from the historical boot closure; they are held constant within
this pair and do not promote that closure into an original boot result.

These are component results with zero original ROM executions or command
invocations. A separately frozen runtime containing the corrected owner
must still execute the original boot path before later topology or
application readiness can be claimed. The callback attribution, earlier
bad lists and all original failures remain separate historical evidence.

### Original boot with the source-built Signal guard: later task progress

The separate run-only pair at
`D:/TestData/CopperOSCommands/Q/sigboot3f872a2d` copies the qualified 110-file
historical host output into two fresh private runtime directories. It keeps
the **same** host test DLL `74D800E4...`, emulator `453DE5EA...`, CPU/core,
ROM, write-protected Workbench image and observer source. The only differing
runtime files are the source-built before/after Exec DLLs above and their
matching PDBs. There is no host/test/compiler rebuild, original vector
patch, guest list repair, injected API call or command invocation.

This is an explicit runtime derivation, not an assertion that historical
Exec `82981A0D...` executed again. The unchanged observer reports its actual
test and emulator assembly hashes; the driver separately binds the private
portable Exec input and full dependency closure before and after execution.
The observer does not independently emit an in-process portable Exec hash.
All original source, build, PDB and runtime receipts remain unchanged.

Both runs discover and execute the one unchanged topology observer without
skips, using 32 chunks of 250,000 public execution-budget units. Both pass
**observation capture**; list validity is evaluated separately:

| Bound runtime | Captured topology and progress |
| --- | --- |
| Source-built before Exec `878E715C...` | Chunk 9 reproduces DF0 as current and already ready. Of 24 DOS-root samples, one has well-formed lists and the following 23 do not; ready reaches the foreign wait sentinel. |
| Guarded after Exec `059BA449...` | All 24 DOS-root samples have well-formed lists; none has current in ready. Initial CLI runs at chunk 9, has nonzero input/output by chunk 10, and CON, ramlib and RAM tasks appear. |

The guarded run observes real Initial CLI `0x0020C968`, CLI BPTR
`0x00080A2E`; input/output BPTRs are `0x00084053`/`0x0008402B` at chunk 10.
RAM task `0x00213BB8` appears in wait by chunk 11. Those are public structure
observations, not public stream or handler-operation qualification. By chunk
15 and through chunk 32, Initial CLI is waiting with mask `0x80000000`,
received bits `0x104`, and input BPTR `0x00084045`; ready is empty and the
seven-task wait list remains consistent. The final CPU is naturally stopped
at `0x00F81476`, SR `0x2000`, not halted. No STOP bit is cleared. Whether this
wait represents normal input or an unresolved boot operation still requires
packet/console observation. A current pointer retaining the idle waiting
task is not by itself labelled another list defect.

The immutable pair receipt is
`5FDD6D976061B0C15EEF42B7B1AA099EEF9A912F08286F156F5ED0BA8BE0E23D`;
before/after TRX hashes are
`F9C5DF1E0D8D58DA1C87BB488F2F4AEFC1F56FB92AE7A5C7BB267597EFD63944`
and `74F22ACB889349C955E5AFD73C108AC4148EDE1552FC90A0409CDC6BF8F80435`.
The 343,363-byte `compact.json` is
`DCCC5AB8E2EDEAA3C8954A694110B4CAE5F48080B62E59823FBF105476EBA525`.
The single-run receipts are `83140BDE379E8CCC29DA55CA423F4640C902AB4ADF45E12DA090B5AD719051D9`
and `36E43B0ED6DD9BED6FBE4FA600D9C650873BDEA18DB51FD228442D9D09777816`.
This establishes the guard's effect on the reproduced boot topology and
later progress within the cap. It adds no full boot, application observer,
R07, native generated Exec, or command-comparison credit. RTC-dependent
exact checkpoint positions remain uncontrolled.

### Initial CLI wait: active startup command, no demonstrated prompt

New read-only observation at
`D:/TestData/CopperOSCommands/BuildSnapshots/dos-initial-cli-wait-host-20260830T194326Z-0a1d6ba9`
keeps the prior 130 source/settings inputs and adds only
`KickstartRomDosInitialCliWaitObservationTests.cs`, SHA256
`AC2DBEFABDA258ADB15EA63061B22578897828EF6B014695D01D168824FC9DD6`.
It explicitly substitutes the source-qualified guarded Exec pin, retaining
the other 39 DLL pins. The new private test-only build has zero warnings or
errors and emits host DLL
`C296FB2961C9431FFCE1A19C10BB72923DF8A78582C45379B717F4F4A54EC7F4`.
Capture/build receipts are
`2B3FA1E11F3A3FA5141974B98093F9851F6C343B25002C6C3E99F7AF93715936`
and `F040F868997C00CF377A5F5BB0FC688A324210BBAF575BAFA725552837D3B713`.

The fact additionally reports and asserts the actual **in-process**
portable Exec `059BA449...`, Devices `47D4A435...` and emulator `453DE5EA...`
identities. It reads public Process/CLI/FileHandle/message structures,
bounded active/user stack values, and existing Console/Input owner state.
The latter is explicit read-only host inspection: no queue is drained,
callback replaced, signal posted, input delivered or guest structure written.
Six boot-source and five console/input-source files are bound to matching
PE/PDB modules. The queue offsets are private owner diagnostics, not a new
public ABI. The host PDB audit
`017AFCE30C3BF2D5F397D0ADF59E684BB228D47B3D48A3A5D21F093CA3CCF7D5`
matches all 133 documents, including the 129 captured C# source files.

One test passes with zero skips and stable inputs, completing the same
32-chunk cap. It observes a noninteractive CLI still executing a startup
command. At chunks 15 through 32:

- Initial CLI's command name is `C:AddDataTypes`, its module BPTR is
  `0x00086575`, and `cli_Interactive` is zero. It has not demonstrated a
  return to an interactive prompt.
- The current task waits for `0x80000000`, with received bits `0x104`.
  Native execution remains naturally stopped at `0x00F81476`.
- All captured real Process message queues are empty and correctly
  terminated. This does not cover every standalone/private port in the
  machine or identify the particular port being awaited.
- The installed host console owner has zero units, retained output,
  pending writes, renders and reads. Its installed input owner has zero
  observed input events and zero handlers in its own portable registry.
  That registry is not an audit of the original ROM input-handler list.

Earlier sampled command names include `C:SetPatch`, `C:Copy`, `Assign` and
`C:Mount`. These are autonomous startup observations, not our explicit
command invocations or comparisons. The user stack begins with return
addresses `0x00F828DA` and `0x00F82576`, followed by `0x00217688` and
`0x00FE7822`; the exact blocking API and owning request still need separate
identification. No function or provider fault is assigned from those
addresses alone. The evidence supports an **unfinished startup wait**,
not prompt readiness. R07 must not proceed by injecting input, repurposing
the waiting Initial CLI or fabricating an application context.

Receipt `results/private-original-dos-initial-cli-wait-receipt.json` is
`F4C349DD8FDA240175783E98C1BA9838A93BC1F994456B74522EF931F46AE425`;
TRX is `7BE79BD153658A834CB84C52D4841DF218E7A12B029F6035CD12335D9193AC06`.
The 204,369-byte compact record is
`35EB99496E585D267B9DDE6B2FE6FAD0F464E1B1B2D29D3160677F9CC9332B9D`.
Boot/console-source bindings are
`DD2A84AB062CE21E0935DC93E5CE43C5968EE4132046FE5BD7733FD42D10B889`
and `6C9C12F67CCCB2D68229C4EBD0084B397F0E0B54A2312901C2773FE973B8670E`.
There are zero explicit Exec/DOS calls, commands, ReadArgs or RunCommand
invocations, and no original handler, prompt, application, R07 or full-boot
qualification from this diagnostic pass.

### AddDataTypes call-trace setup: two retained diagnostic failures

The first bounded public-call observer, frozen in
`D:/TestData/CopperOSCommands/BuildSnapshots/dos-startup-wait-calltrace-host-20260830T200012Z-1abc44af`,
reached a valid ready/wait topology with Initial CLI running `C:AddDataTypes`
after fourteen normal boot chunks. It failed while enumerating the published
library list, before installing its diagnostic listeners. Its receipt is
`691A28DE0A80C2B1EACF9596CA7519DAEC9D81FEE95C80C64BB13208D547E47E`;
TRX `53EE22BA195638B074F1768D0EFA15B2B5838AE4E076748150BFEB6FB9D85CAD`.
That is one executed, failed observation and zero traced call boundaries.

A new diagnostic-only variant retained the partial map and the rejected
node without changing the predicate:
`D:/TestData/CopperOSCommands/BuildSnapshots/dos-startup-wait-calltrace-host-20260830T200721Z-acbcc96e`.
It also failed once, with stable inputs, before attaching listeners. Receipt
`FC4D421B628CEF6323709B94C83C56EC003856F8195A3BBDF49BB4B2B066DA0B`;
TRX `21F88C9D923859BEFE822524E16164B0EC05A543E94492D5440637EEBECB1BE2`.
The rejected tenth node was `0x00C000C4`, following `locale.library` at
`0x0021F788`. The bus reported its header mapped; it was not a repeated node.

This rejection exposed a diagnostic range error, not evidence of a corrupt
library list. The already PDB-bound profile source, `Configuration.cs`
SHA256 `8269AE6A35FF791B11F255DE3E977C6371B9CAB7A85B62529A4860F9BDF5E1E9`,
configures 512 KiB pseudo-fast expansion RAM for `A500Pal512KBoot` as well
as its 512 KiB chip RAM; the tests additionally select 2 MiB real fast RAM.
The reused helper had excluded the expansion bank when checking library
headers. A fresh observer records the actual machine options and admits
only the configured physical banks plus the bus mapping check, with even
addresses, library node type, declared header size and predecessor/tail
consistency required. It does not change the profile, guest memory, vectors,
providers, listener policy or instruction bounds. The exact diagnostic delta
is `configured-ram-guard-delta.json`, SHA256
`4D90F716D3EC5FB48B23CF5FC9707817C9606C028EA118546C616D620952D29D`,
under the fresh `dos-startup-wait-calltrace-host-20260830T202117Z-58d24039`
snapshot. An intermediate source-only capture, `56de9a1b`, is explicitly
rejected before build because it used an unavailable SDK field name.
Neither setup failure receives command, R07, application or boot credit.

### AddDataTypes wait: original software-failure requester observed

The corrected diagnostic observer in
`D:/TestData/CopperOSCommands/BuildSnapshots/dos-startup-wait-calltrace-host-20260830T202117Z-58d24039`
passed one discovered/executed test, with no skip, on unchanged guarded
Exec `059BA449…`, emulator `453DE5EA…` and Devices `47D4A435…`.
All 131 parent source/settings inputs and 40 runtime pins were retained;
only the new observer source was added. That source is SHA256
`60D9275A18168F3F3FC89766E952FD9E8879547334DA4938F2FE41B8E62E7299`.
Private build receipt: `8D350C25626E529318DD97C2932EAA4E1EADD345F91F5490A95B8E45521D4C0B`.
The executed 5,364,224-byte test assembly is
`E4446629A6B86B2675A484ADE727BBA8B53DB64D2FA66A4103267221D047CDA9`;
its PDB/source audit is
`C41C62B99A9E833CEFB876563A629B4094383A1E72EFD0CB48721C075A5D78D3`.
The full receipt is
`A5ABB6AA21820919B7E44D412047A3DE0CA68E74F306FFBB0F57F9A0E628381D`;
TRX `97F38FDA08994F957150B8079C8889C0B3EFAC7255F3289055F072DB5AE0740C`.
The smaller `results/startup-requester-compact.json`, SHA256
`8ED3778CA81BC0C7A0B97162ABF75177EB76C4F13EB16C4F28865ED57C91472C`,
binds the full record, source/runtime evidence and the selected public calls.

After fourteen normal boot chunks, the observer used exactly 250,000 public
`ContinueExecution(1)` boundaries: 163,066 instruction retirements, 411,770
CPU phases and 86,935 boundaries beginning in the CPU's natural stopped
state. It recorded 3,716 public entry/target observations without overflowing
the 4,096-record bound. The library list, including `expansion.library` at
`0x00C000C4`, validated against the actual configured RAM banks. CPU state
was unchanged by captures; composed listeners were restored; reference
vectors and the write-protected disk image remained unchanged.

The relevant observed transitions are:

| Boundary step | Actual public call by Initial CLI | Captured evidence |
| --- | --- | --- |
| 10,468 | Exec `InitSemaphore` at `0x002005E2` | A0 is odd `0x554E414D`; return address `0x00219BC6`. This is the observed invalid argument, not yet a writer or exception-frame attribution. |
| 12,044 | Intuition `EasyRequestArgs` at `0x00208CE8` | A6 matches original Intuition base `0x00208F34`; actual target is `0x00FCF6DE`. The public EasyStruct and four arguments are present in the captured caller stack. |
| 15,977 / 136,643 | Intuition `OpenScreenTagList` / `OpenWindowTagList` | Native original entries are observed during the synchronous requester path. |
| 201,552 | Exec `WaitPort` at `0x00200690` | A0 is the empty port `0x00221828`, with signal bit 31 and signal task Initial CLI `0x0020C968`; native target `0x00F8255A`. |
| 201,563 | Exec `Wait` at `0x002006D2` | D0 is `0x80000000`; native target `0x00F82888`. The task subsequently waits and the CPU reaches its natural stopped state. |

These entry and target addresses have no host gateway. They establish an
original synchronous GUI requester, rather than relying on a nearby return
address to infer the owner of the wait. Only strings referenced by the
captured public arguments were decoded from the hash-verified ROM; the
command name comes from the already captured CLI BSTR. The requester title
is `Software Failure`; its body identifies `C:AddDataTypes` and error
`#80000003`, followed by an instruction to wait for disk activity. Its two
gadgets are `Suspend` and `Reboot`. No screenshot, requester dismissal,
keyboard/mouse input or response is claimed.

Original NDK 3.1 `exec/alerts.h`, line 36, defines `ACPU_AddressErr` as
`0x80000003`, an illegal address access including an odd address. The
Developer CD member is
`NDK_3.1/Includes&Libs/include_h/exec/alerts.h`, extent 21,092, 10,865 bytes,
SHA256 `98AFB8D7C350F751BFB9940C191CC414DBA1F27979060641EF865AABFACB75FE`.
The compact record also binds the original EasyStruct declaration and
Intuition/Exec autodocs. This confirms the reported address error, but does
not yet capture the precise faulting CPU instruction, access or exception
frame, or the writer that supplied the malformed semaphore pointer.

This is one passive diagnostic observation with zero explicit command,
Exec/DOS API, ReadArgs or RunCommand invocations credited. Autonomous startup
activity remains separate from command tests. R07 and application/full-boot
readiness stay open; the next bounded observer must capture the earlier
exception boundary without repairing guest state or dismissing the requester.
Pointers retained after a message call may refer to reused stack storage;
their later contents are not continuing message-ownership evidence.

### Public application-Process proposal, deferred pending the startup fault

The original NDK 3.1 documents CreateNewProc at DOS LVO -498 with tags in D1
and a returned Process pointer in D0. It accepts NP_Entry or NP_Seglist and
can be called from a Task. Its defaults include a non-CLI Process, a 4,000-byte
stack and DOS-owned NIL input/output handles. Inheritance can cause DOS I/O,
so a controlled observer should explicitly choose CurrentDir/HomeDir, local
variables, path, console/window and priority. NP_Cli can request a real CLI;
setting private Process fields is unnecessary and prohibited here.

The original headers mark NP_NotifyOnDeath and NP_Synchronous nonfunctional.
They cannot implement a completion protocol. NP_Arguments must not be paired
with a null NP_Input. An assembly entry must return through its original
stack with D0/RTS; dos.library/Exit is not a suitable shortcut. Exec's Wait
must run in genuine user mode, and signal bits belong to the allocating task.
The private `process-launch-api-evidence.json`, SHA256
`1D3C7D02120D7D50E115BC4F0718A6EBD26074CF8DAD4AB0959425A9C28304D9`,
records exact ISO member identities for dos.doc, exec.doc, dostags.h and
tasks.h. No reference implementation was copied. Existing
DosLiveProcessBoundaryTests use synthetic host Tasks and caller switching;
they do not establish the original scheduler path.

After that path is demonstrated, a bounded next implementation can add only
`DosApplicationProcessObserverProgram.cs` and
`KickstartRomDosApplicationProcessReadinessTests.cs` at the existing MedPlayer
test owner:

1. Allocate the authored code, result record and tag/name storage through
   public Exec. Call original CreateNewProc at the actual checkpoint, with
   controlled inheritance, NP_Entry, a 4,096-byte stack, priority zero and
   NP_Cli. Retain every caller-owned allocation until child completion;
   reject failure without assigning ThisTask or editing Process metadata.
2. Require actual native entry into the observer in the returned separate
   Process, with a user-mode stack inside its real allocated bounds. Let the
   child call public FindTask(NULL), Cli, Input and Output and copy observed
   identity/handles into its caller-owned record. Trace original vectors,
   native scheduling and actual current-task changes; an allocated Process
   pointer alone is insufficient.
3. Use a child-owned AllocSignal/Wait/Signal handshake to retain child
   liveness while observing its CLI/streams. Require normal boot scheduler
   progress and the unchanged bounds. Never switch to it manually or infer
   input readiness from its name. Record zero command/ReadArgs/RunCommand
   calls for this authored Process observer.
4. Release the handshake through public Signal, free the child's signal and
   return normally. Observe DOS-owned handle closes, native finalization and
   scheduler handoff before freeing caller-owned code/storage. Require
   proven termination and balanced resources; a terminal marker or machine
   disposal alone cannot qualify cleanup. If handoff fails, retain that
   failure and do not start RAM I/O or R07 stream restoration.

This remains a proposal. No application Process has been created by these
tests. The guarded runtime now preserves ready/wait topology in the observed
interval, but the original startup address error remains unresolved.

## Later disposable Eval/Which command recipe

This recipe identifies remaining implementation steps; there is no existing
CLI flag that completes them today.

1. Create a disposable copy of the licensed Workbench media or a dedicated
   boot image under a unique private run directory. Add a private probe HUNK
   and a minimal S:Startup-Sequence there only. Preserve original input hashes;
   never write the source ZIP/ADF/ROM, attach a live system disk, or mount the
   repository/home directory writable into the guest.
2. Use ROM reset and normal guest DOS/Shell startup, with a controlled and
   recorded provider policy. The probe must be launched by the guest Shell,
   not by replacing the initial PC and inventing a Process/CLI structure.
3. Start with public OpenLibrary, native explicit-source ReadArgs, and a
   private trivial return probe to establish DOS readiness. Then LoadSeg the
   hash-pinned original `C:Eval` or `C:Which`, redirect through genuine DOS
   handles, and invoke RunCommand with a valid newline-terminated argument
   tail and real stack/process ownership. Capture return and IoErr immediately.
4. Run `Eval 7 + 5` and `Which Eval` as controlled cases. Use a private guest
   output file plus DOS readback, or an explicit bounded result buffer owned
   by the probe. Do not mistake a screenshot, diagnostic preview, launcher
   status or `dos.library/Execute` boolean for command stdout/return status.
5. Restore streams, unload the owned segment, release resources and report a
   completion marker. Stop on a bounded guest instruction/cycle/time budget
   and reject an incomplete marker. Export only the intended receipt/output
   bytes, then compare against the separate CopperStart run.

Existing low-level disk support can load an owned ADF byte copy and write
sectors in memory. `AmigaDosFileSystem` exposes reads and launch-request
resolution, not a public file-insertion writer. Building the disposable boot
image, a full boot-ready predicate, lossless command stream capture, normal
RunCommand process/continuation integration, and native CopperStart boot
composition remain concrete provider/tooling work. The managed host startup
runner and synthesized file handles do not close those requirements.

MorphOS 3.20 execution is not available through these MC68k runners. Its ISO,
templates and source observations remain reference material; no PPC command
execution or MorphOS runtime parity is asserted.

### 2026-09-05 - Original DOS observations for the full Copy template

Reviewed CC03/CC04/CC13 integration gaps and added a separate LicensedCopyReadArgs test at the existing MedPlayer native reference owner. The old 44-case corpus is unchanged. The new corpus uses the exact current Copy template (checked equal: 275 characters, 24 slots), with empty/positional/multiple/quoted inputs, short and long aliases, mode/metadata switches, negative/invalid/overflow BUFFER and a repeat after failure. Shared fixture capacity checks now admit 96 bytes of slots and 511 template characters within existing nonoverlapping storage; guard bytes, native-vector checks, value copying before FreeArgs, cursor/IoErr observations and instruction limits are unchanged.

The shared build could not copy dependencies because unrelated testhost PID 30644 held its output. Stopped only our build attempt and rebuilt all dependencies under artifacts/copy-original-readargs/isolated-build. The unrelated test process was not interrupted. The isolated licensed original-DOS run passes one test, zero failures/skips, with 12 original observations and zero generated-parser comparisons or command invocations. TRX: artifacts/copy-original-readargs/copy-original-readargs-isolated.trx. Typed observations and source/test/runtime hashes: artifacts/copy-original-readargs/original-copy-observations.json. This uses the existing licensed Exec bootstrap and its documented overlays; it is not normal boot, stream, RunCommand or MorphOS execution.

Observed facts: bare positional source/destination words both remain in FROM/M and TO is null; empty input succeeds with empty slots; short/long option aliases publish corresponding slots; BUFFER many fails with IoErr 115 and no FreeArgs; BUFFER 2147483648 succeeds with raw number 0x80000000; successful cases retain the initialized IoErr sentinel 0x13572468 through cleanup. These are DOS parsing results, not Copy's later admission/normalization results. Compare the generated DOS/native command against these observations next. No reference answer was substituted for native execution, and no CC04/CC13 or shipping gate is closed. Existing 333 command fixture checks remain a separate historical receipt.

### Full Copy ReadArgs template integration: length fix and remaining scan cost

See progress-log.md's 2026-09-05 "Full Copy template exposes DOS length rejection; owner fix" entry. Live DosCore now uses the existing mapped string scanner for template admission, validates length once per traversal, and walks keyword/positional candidates sequentially. Host ReadArgs/DosCore selection passes 80 tests. Filename validation remains unchanged; no shared cache was added.

Three separately preserved isolated native candidates were built and provenance-checked against the accepted parser baseline. All preserve the old 44-case native comparison. The first times out on Copy empty input; the next two return the matching empty-input observation but time out on positional input. None passes the 12-case Copy corpus. The latest HUNK is 795A7FE63EB5087BCDFA759362896303A6577C7D2DBA035DE9BFCF15C4191686, with path recorded in artifacts/readargs-long-template/keyword-candidate-root.txt. Native diagnostic samples (progress-symbols.json and native-keyword-progress.trx) locate continuing scans in CStringLength/TryReadReadArgsEntry with source cursor 20. Required backfill and final validation still restart entry lookup by index and are the next investigation target. Instruction budget remains unchanged. The live native owner build separately fails with C68K0009 resolving CopperSharpRomMemoryPlatform::.ctor; isolated evidence does not qualify that live closure. CC04/CC13, pure/resident and shipping gates remain open.

### Full Copy parser comparison now passes on the isolated candidate

The required-argument and /F lookahead traversal changes resolve the earlier timeouts for the tested corpus. Native comparison now passes all 12 Copy cases and the unchanged 44 baseline cases within the original instruction budget. HUNK: A1611E8F1D78555200AB581E92E7B148C25B59CEB79E4EA3283F92841710372A. Bound observations/build identities: artifacts/readargs-required-scan/qualification.json. See readargs-parser-qualification.md's 2026-09-05 extension for exact scope. This supersedes the preceding parser-timeout status only for that isolated source-bound candidate. Full live native owner integration, command execution, normal streams/filesystem, MorphOS runtime and command pure/resident/shipping gates remain open.

### Live native DOS integration passes the Copy parser corpus

The live owner build now validates and passes all 56 explicit-source native parser pairs (44 baseline and 12 Copy). The constructor blocker was a reflection dependency-loading failure; it is fixed in the compiler without changing DOS constructor semantics. The validator now checks the actual one-entry/two-export root set. Exact evidence: artifacts/readargs-live-owner/qualification.json and live-native-parser.trx; HUNK A553D004C017FE56CB94DAD9A5D0AA9024251E133A34ACB15216AD06187297E3. This supersedes the prior live-owner build blocker and isolated-only parser status. All normal-stream, full-command, filesystem, boot, MorphOS and command pure/resident/shipping limitations remain. Next work is real command integration, not further template-length fixture expansion.

### Copy normal entry through original DOS RunCommand

Current Copy command HUNK `BCF4467C7C7B8CB073E68A30FCF7C46B1C7B95D717B092696990FE89CBEB8E24` passes all 333 supplied-vector fixtures and now executes two parser-error paths under original DOS RunCommand. [Bound command observations](../../../artifacts/copy-original-runcommand/qualification.json) and [test output](../../../artifacts/copy-original-runcommand/copy-command-and-observer.trx) record the result.

| Input | Return / IoErr | Original ReadArgs calls | Public RDArgs alloc/free | Direct command Exec alloc/free | Instructions |
| --- | --- | --- | --- | --- | --- |
| BUFFER many + LF | 20 / 115 | 1 | 1 / 1 | 1 / 1 | 8393 |
| Unterminated quoted alpha + LF | 20 / 120 | 1 | 1 / 1 | 1 / 1 | 8479 |

Both parse failures report the same error through PrintFault and do not call FreeArgs. Copy's caller-owned RDArgs is obtained through original AllocDosObject; the host verifies zero CSource fields and does not insert input there. Original RunCommand delivers D0/A0 and the current input stream. Actual public NIL handles are selected/restored/closed through DOS. The command's library open count, original vectors, command image, arguments, Process/CLI/stack and borrowed selections restore. The default observer also passes two original MakeDir regression cases. No original MorphOS Copy binary is executed or compared.

These results establish a bounded normal-entry/input/error-cleanup path on original DOS. The segment loader is the existing host HUNK loader backed by public Exec allocations, not DOS LoadSeg. NIL discards output, so visible output bytes remain unqualified. Successful Copy operations, generated-DOS command invocation, filesystem/handler behavior, full options, full boot, original MorphOS runtime and pure/resident admission remain open. Command source/build file bindings were checked before and after the run; they are not a frozen source snapshot. See CP8 for the next work.

#### Isolated NIL examination prerequisite (2026-09-05)

The successful-copy attempt now has an independent control: direct original
ExamineFH on an original NIL input handle also waits at PC 00F81476 without
returning. No Copy image, ReadArgs or RunCommand participates. The existing
public NIL open/select/restore/close regression still passes. Evidence is in
`artifacts/nil-examine-readiness/result.json` and `nil-examine.trx`.
Thus basic stream acquisition does not qualify examination or transfer readiness.
Keep the successful DIRECT command gate open; investigate original packet/wait
and filesystem/bootstrap readiness before any production command change.
