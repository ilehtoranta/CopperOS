# Native command launch contract

Reserved dependency: **CC02.API14 — RunCommand argument/input boundary**.
Related command/profile: `CC12.MakeDir.wb31`. Recorded: 2026-08-30.

This is the implementation contract for the next bounded launch slice. It does
not replace the overall goal or the [MakeDir command contract](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/MakeDir.md).
The evidence includes primary-document inspection, identified source gaps
and bounded original-DOS readiness runs. R01 passes; the initial R02 observes
a real Process/CLI but fails because input/output handles are absent. A
separate R04 run passes public NIL: acquisition, selection and cleanup. Exact
R05/R06 and one additional nonempty native observer variant now pass through
original DOS RunCommand and NULL-D3 ReadArgs. Two original/two generated MakeDir
calls also pass for LF-only and a seven-byte unterminated-quote variant. A
separate original/generated pair now passes exact fourteen-byte A03 below.
**No filesystem-handler operation,
complete command/profile or full boot is qualified by this document.**
New-process scheduling and R07-R08 remain pending. Supplied-vector MakeDir and
explicit-source ReadArgs results remain separate evidence with their receipts.

A separate normal-RTS stack bridge has passed bounded native compiler tests:
29 focused checks and 418 inclusive related checks, including 36 successful
native invocations. Its actual CopperStart callback integration now passes 12
focused/39 inclusive checks, with 18 successful command calls. Exec remains a
vector fixture. Input-buffer implementation, rollback and lifetime integration
remain required. Exact sources, failure controls and receipts are in
[compiler qualification](compiler-qualification.md).

## Primary references and reproducible identity

The private [AmigaDeveloperCD.iso](D:/TestData/AmigaDeveloperCD.iso) is
61,476,864 bytes, SHA256
`5d6bfcb213f1395d4c95584dc94d0e36265be355076dae1710bd36fb4dfdbff3`.
The image and these members were rehashed read-only for this audit. Blocks are
2048 bytes; member hashing uses exactly the listed byte count, not sector padding.
No original source, ROM or executable payload is copied into the repository.

| ID | Exact ISO member | Block; bytes | SHA256 |
| --- | --- | --- | --- |
| N1 | `NDK_3.1/Docs/doc/dos.doc` | 17730; 174666 | `2e22d1d1b7c00fac5757eead0ef4d49a01c7666f0db5850735193c9597a7cee2` |
| N2 | `NDK_3.1/Includes&Libs/fd/dos_lib.fd` | 20705; 5765 | `a9b24c1d9fb1053955dfb28eb8dabe92eff5dbde10b952e67d6cee2a80c5c228` |
| N3 | `NDK_3.1/Includes&Libs/fd/exec_lib.fd` | 20709; 5721 | `4da0ae2a91d0758696e0d1f72f8bc748321b5fd14e7e31d8215cda8e002348c2` |
| N4 | `NDK_3.1/Includes&Libs/include_h/dos/dostags.h` | 21063; 5155 | `db4f2d4512f802daa2531d7b007cc97ca179fd1ea123fde9cf1584701e986239` |
| N5 | `NDK_3.1/Includes&Libs/include_h/dos/rdargs.h` | 21073; 4400 | `83e9e1385720c2dcb7f258c467a1d501cc0d3f8796567a32d8da9588e1805d32` |
| N6 | `NDK_3.1/Includes&Libs/include_h/dos/dosextens.h` | 21052; 16933 | `d65038a5297ffa91e3ab1ba063dca102580de63b14c071a1158d4e9ecdfb2f50` |
| N7 | `NDK_3.1/Includes&Libs/startups37/startup.asm` | 22291; 26931 | `a8a75cb9335456699ba1db9858bdb3f6f0dc53eeb18ab90bb8aa796c16b5e0c6` |

N1 entry offsets below are zero-based bytes in the unchanged member. They
identify full entry headings after its table of contents. N2 supplies the
negative library offsets and register mapping; returns are in D0.

| Original public API | N1 entry offset | N2 LVO; inputs | Required behavior and ownership |
| --- | ---: | --- | --- |
| `RunCommand` | 134074 | -504; D1 SegList BPTR, D2 stack bytes, D3 argument APTR, D4 argument length | Runs in the current process, supplies command arguments, substitutes current-input buffering and GetArgStr while running, and restores both afterward. Stack allocation failure returns -1. The input-buffer setup was added in V37. |
| `CreateNewProc` | 23310 | -498; D1 tag-list APTR | Public process-creation route if the fixture lacks a suitable CLI Process. Specify one of NP_Entry and NP_Seglist. A failed creation does not transfer ownership of caller-provided resources. |
| `Cli` | 17127 | -492; none | Returns the current Process's CLI pointer or NULL. A nonnull Exec ThisTask alone is not evidence of a CLI. |
| `Input` | 83605 | -54; none | Returns a borrowed current-input FileHandle BPTR. The command must not close it. |
| `Output` | 112351 | -60; none | Returns a borrowed current-output FileHandle BPTR. The command must not close it. |
| `ReadArgs` | 121650 | -798; D1 template, D2 LONG result array, D3 optional RDArgs | With no supplied RDArgs, parses normal buffered Input. Zero/default the result slots before calling. Keep returned data live until one matching FreeArgs. |
| `FreeArgs` | 69792 | -858; D1 successful RDArgs | Releases the successful parse's attached storage. A supplied RDArgs itself remains caller-owned; NULL-D3 acquisition is DOS-owned. Do not FreeArgs a failed parse. |
| `GetArgStr` | 73510 | -534; none | Returns the current argument string, corresponding to the command's CLI A0 argument. Record it during the child command and after RunCommand returns. |
| `SetArgStr` | 140796 | -540; D1 new string | If used by a fixture control, restore the previous pointer before Process exit. This setter alone does not establish ReadArgs input buffering. |
| `Open`, `Close` | 110135; 21197 | -30: D1 name, D2 mode; -36: D1 BPTR | Obtain real handles through DOS; Open failure returns zero with IoErr. Close only handles that the fixture actually owns and has not transferred. NIL: is a documented device name, but availability in this partial bootstrap must be observed. |
| `SelectInput`, `SelectOutput` | 138833; 139398 | -294; -300; D1 new BPTR | Return previous default handles. Save and restore those handles; selecting them does not transfer ownership of borrowed streams. |
| `GetProgramName`, `SetProgramName` | 78369; 149199 | -576: D1 buffer, D2 length; -570: D1 name | Set a suitable command name before RunCommand when applicable, check failure, and restore the caller's previous name. Do not assume unlimited CLI name capacity. |
| `IoErr`, `SetIoErr` | 86858; 146325 | -132: none; -462: D1 error | Capture command secondary error before unrelated fixture cleanup or readiness calls can replace it. Preserve the distinction between command-selected error and later restoration/cleanup outcomes. |

For this slice the original ROM is the private
[kickstart-3.1-a500.rom](D:/TestData/ROM/kickstart-3.1-a500.rom), 524,288 bytes,
SHA256 `8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`.
Its image version is 40.63; the existing fixture observes original dos.library
40.3. These are different identities, not a version mismatch. The private
[MakeDir-37.2.hunk](D:/TestData/CopperOSCommands/Workbench31/MakeDir-37.2.hunk)
is 464 bytes, SHA256
`23911db49742055d8bddcfe5f8de82cbb2232f8a7ef850d51bfd27f9b54c819b`.
Both private inputs were rehashed for this audit. The inventory identifies
the original Workbench/Install members; never put these payloads into source
snapshots or generated-artifact manifests.

## Argument, entry and lifecycle contract

1. The RunCommand argument allocation contains command text followed by LF
   and then NUL. D4 counts the text and LF, excluding the NUL. An empty command
   line is therefore bytes `0A 00` with length 1. Do not silently add/remove LF
   inside a compatibility test; record the exact input bytes and length.
2. RunCommand must expose that argument text through both its normal input
   buffering and GetArgStr during execution, and restore the caller's state
   afterward. Passing D0/A0 to an entry or setting GetArgStr alone is insufficient.
   This target uses original Kickstart 3.1; MakeDir's minimum OpenLibrary
   version 36 is not evidence that the V36 RunCommand path has V37 behavior.
3. N7:264-267 defines the image entry as D0 text length and A0 text address,
   with all registers except SP allowed to change. Do not impose the public
   library/subroutine nonvolatile-register rule on an entire command image.
   Retain SP, D0 result, memory/resource guards and actual public-vector ABI
   assertions. A6 is not an incoming DOSBase promise.
4. A valid public-DOS-created Process must actually be current while its
   entry runs. Do not write ThisTask, fabricate a Process/CLI/FileHandle, or
   repurpose a stopped bootstrap task to claim child-process execution.
   Bounded failure to create/schedule such a Process is a failed prerequisite.
5. N4:38-92 makes NP_Entry/NP_Seglist mutually exclusive, NP_Cli opt-in,
   and default input/output opens of NIL:. NP_CloseInput/NP_CloseOutput
   default true: explicitly disable automatic closing when passing borrowed
   handles, or transfer fixture-owned handles with clear exactly-once ownership.
   If passing an externally loaded NP_Seglist, explicitly settle NP_FreeSeglist
   ownership; its default is true. NP_Arguments must not accompany NULL NP_Input.
   For the first slice, use a fixture entry which calls RunCommand and leave
   the separate NP_Arguments mechanism for later coverage.
6. Borrowed input/output are restored before any fixture-owned replacements
   are closed. Keep argument allocations, loaded segments and callback control
   storage live until command return and the owning Process's completion.
   Creating a Process successfully is not evidence that it ran or finished.
7. The unchanged classic MakeDir body obtains NAME/M through normal DOS,
   not an injected RDArgs or host parser. Its probe opens DOS version 36,
   calls the body, restores the selected error and closes its owned DOS lease.
   The probe is CLI-only; it receives/replies to no Workbench startup message.
   Workbench launch and missing-library process-exit behavior remain separately
   qualified requirements, not additions to these normal-open launch cases.

## Existing owners and the remaining gap

| Owner / existing seam | What can be reused | What it does not establish |
| --- | --- | --- |
| [NativeCommandArguments.TryRead](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeCommandArguments.cs:34), especially line 77 | Invocation-owned result slot and successful RDArgs lease; exact NULL third ReadArgs argument. | No stdin setup. Changing D3 to a supplied buffer would bypass the launch boundary under test. |
| [MakeDir body](D:/Koodit/GIT/CopperOS/src/Commands/Native/Workbench31MakeDirCommand.cs:20), [probe](D:/Koodit/GIT/CopperOS/tests/Commands.NativeRoot/Workbench31MakeDirProbe.cs:15), [startup/finish](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeCommandStartup.cs:30) | Existing unchanged command path, library ownership and selected return/error policy. | Supplied-vector results do not prove original parser input, DOS diagnostics or real handler behavior. |
| [CreateReadArgsLicensedOracle](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosReadArgsDifferentialTests.cs:110) | Pinned ROM, bounded readiness, supported 2 MiB fast RAM, original native DOS vector checks and input provenance. | Readiness checks Exec memory/ThisTask and requires the documented Exec/device overlay. It is not an unmodified full-OS boot or CLI readiness proof. |
| [InitializeLicensedDos](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosDifferentialTests.cs:155) | Locates and executes the original resident; observes its published library. | Deliberately reaches a bounded early filesystem/device startup wait. Publishing DOS is not completing startup. |
| [Explicit-source parser capture](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosReadArgsDifferentialTests.cs:221) | Instruction-level original-code provenance, live result copies and guarded cleanup observations. | D3 is a supplied RDArgs with RDA_Source and NOPROMPT. The 44 cases are not stream, RunCommand or MakeDir launches. |
| [Existing HUNK loading](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosReadArgsDifferentialTests.cs:60), [OracleContext](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomLayersDifferentialTests.cs:2597) | Real Exec allocations, loaded segment tracking, bounded vector invocation and cleanup. | The direct PC/A7 vector caller cannot manufacture a live native child Process. Segment ownership must be explicit when passed to public launch APIs. |
| [CopperStart RunCommand core](D:/Koodit/GIT/CopperStart/src/CopperStart.Dos/DosRunCommandCore.cs:56), [resume](D:/Koodit/GIT/CopperStart/src/CopperStart.Dos/DosRunCommandCore.cs:126), [native callback](D:/Koodit/GIT/CopperStart/src/CopperStart.Dos/DosNativeEntrypoints.cs:175) | Checks image/arguments, allocates command stack and continuation, invokes entry with D0/A0, frees the owned continuation. | **Source-audited CC02.API14 gap:** no save/substitute/restore of current-input buffering or GetArgStr around the command. This is not an executed original/generated mismatch yet. |
| [Host RunCommand test](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/DosServicesTests.cs:1366), [core tests](D:/Koodit/GIT/CopperStart/tests/CopperStart.Exec.Tests/DosRunCommandCoreTests.cs:27) | Stack/callback/validation test owners. | Callback entry arguments and stack restoration do not test normal ReadArgs delivery or preservation of the caller's unread stream. |
| [TryLaunchProgram](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Emulator/AmigaBoot.cs:11952) | Existing host application launch helper. | Sets entry D0/A0 and begins a subroutine; it is not a substitute for original RunCommand's input-buffer protocol. |

For CopperStart, implement the missing behavior at the owner boundary, not in
MakeDir. Invocation-owned continuation state must preserve the caller's
argument pointer and input-buffer state, expose the replacement argument
buffer during the command, and restore the old state before releasing the
owned storage. Cover ordinary return, entry failure/termination where
supported, allocation failure and nested calls. Do not introduce process-global
mutable argument storage or fix parser tests by substituting RDA_Source.
The exact native failure/termination behavior still needs primary/reference
observations; do not invent it from the successful callback path.

The disposable host-backed volume owner is
[DosHostVolumeMount](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Emulator/CopperStart/Dos/DosHostVolumeMount.cs:6),
[ConfiguredDosHostHandler.CreateDirectory](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Emulator/CopperStart/Dos/ConfiguredDosHostHandler.cs:433).
[PublishConfiguredVolumes](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Emulator/CopperStart/Dos/DosServices.cs:326)
adds entries to CopperStart's own DOS state. **This is a CopperStart-only
integration provider, not an attached handler for the original ROM DOS.**
No original-ROM DOS packet registration or handler lifecycle is established
by those APIs. The [ADF reader](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Emulator/AmigaDosFileSystem.cs:63)
and [test-only image builder](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/AmigaDosFileSystemTests.cs:679)
also do not supply a general writable guest-filesystem installation path.
Writable disk DMA exists at its own owner; native handler boot/readiness and
exact file-backed stdout capture remain prerequisites to filesystem claims.

## Finite original-only readiness and boundary observations

Keep these in a fresh original-DOS context; do not install generated DOS and
then mistake its OpenLibrary result for the original base. R01 and a separate
R04 run have passed; the initial R02 failure is retained. Exact R05/R06 and an
additional 25-byte quoted/escaped observer variant pass in distinct receipts;
new-process scheduling/R07-R08 remain pending. A timeout, absent
input, unexpected provider or missing reference
fails that row and prevents dependent rows; it must not be counted as a skip/pass.

| Stable fixture ID | Setup / action | Required observation, distinct from a passing command |
| --- | --- | --- |
| `CC02.API14.R01` | Verify ROM, original DOS version/base and relevant original vector targets before use. | Record all active Exec/device overlays. ReadArgs/FreeArgs/RunCommand/CLI/stream calls under test must resolve to original native DOS, not host gateways. No full-boot claim. |
| `CC02.API14.R02` | Observe current task provenance and original Cli/Input/Output; no synthetic OS structures. | Record whether a suitable Process, CLI and input/output handles actually exist. Nonnull ThisTask alone fails the CLI prerequisite. |
| `CC02.API14.R03` | If needed, use original CreateNewProc with NP_Cli and a native fixture entry; obtain/select real NIL: handles with explicit ownership. | Observe child scheduling/current identity, successful public handle acquisition and later completion. A published library or successful allocation alone is insufficient. |
| `CC02.API14.R04` | Exercise SelectInput/SelectOutput and restoration using owned replacement handles. | Borrowed originals survive; replacements close once after restoration; no remaining child or stream ownership. Capture failures and immediate IoErr separately. |
| `CC02.API14.R05` | Original RunCommand calls a small native observer with argument bytes `0A 00`, length 1, and ReadArgs NAME/M with D3=0. | Capture entry D0/A0, GetArgStr, successful/failed original ReadArgs, live /M result and FreeArgs. Observe D0 and SP after return. Do not supply RDA_Source. |
| `CC02.API14.R06` | Repeat observer call with ASCII `alpha beta` + LF + NUL, length 11. The observer parses/copies values but performs no filesystem calls. | Capture normal ReadArgs order/strings, GetArgStr during execution and its restoration. This is parser/launch-component evidence, not MakeDir filesystem behavior. |
| `CC02.API14.R07` | From a real seekable/input stream containing known unread bytes, call the same observer and then read the stream normally. | The caller's unread sequence and GetArgStr survive the call. NIL: cannot establish restoration of nonempty buffering. Keep this row pending until a real suitable handler is available. |
| `CC02.API14.R08` | Use a controlled original public allocation-failure boundary for the RunCommand stack, retaining stream/argument sentinels. | Observe -1, no entered command and unchanged caller state/owned-resource balance. Do not force a new global D0=20 compiler policy. This fault case needs a proven injection point and separate provenance. |

R05/R06 establish normal NULL-D3 parsing without directory mutations. R07 is
the later restoration gate and must not be silently satisfied by empty NIL:
input. For R08, resource exhaustion must not damage the boot state or originals;
do not approximate it by an arbitrary huge stack or an unrelated stub failure.

## First real MakeDir launches

Use the unchanged pinned original HUNK first. After the original observations
are accepted, run the unchanged generated MakeDir probe through the same
original-DOS launch path in a separately reset context. Do not reinterpret
the existing 38 original / 39 generated supplied-vector fixture as these runs.

| Stable launch ID | Existing contract case | Exact input and finite expected observations |
| --- | --- | --- |
| `CC12.MakeDir.3.A01.original` | MK31-A01 | Bytes `0A 00`, length 1. Observe real NAME/M with D3=0, successful RDArgs with absent list, no Lock/CreateDir, return 20 and selected error 0. The command's missing-name diagnostic is known statically, but NIL: output cannot qualify its delivered bytes. |
| `CC12.MakeDir.3.A03.original` | MK31-A03 | Opening quote, ASCII `unterminated`, LF, NUL; length 14. Observe the actual original parser outcome and IoErr. Expected malformed-quote failure follows the 20/E, PrintFault(NULL header), no successful RDArgs/no FreeArgs path. Freeze exact E from the reference; unexpected parsing stops the case rather than changing input or normalizing the result. |
| `CC12.MakeDir.3.A01.generated` | MK31-A01 | Same bytes/public original-DOS setup as A01.original, using the generated probe. Compare copied parser values, selected return/error and command-owned cleanup; admit its separately owned result-slot allocation. |
| `CC12.MakeDir.3.A03.generated` | MK31-A03 | Same malformed input and reference-selected E as A03.original. Compare actual parser/command behavior; do not supply an expected failure through a vector stub. |

The exact A01 pair and exact fourteen-byte A03 pair have now run in distinct
captures. A separate seven-byte quoted-input pair remains additional evidence.
Across those receipts there are three original and three generated calls and
three comparisons; four calls/two comparisons cover the exact contract inputs.
These cases do not cover Workbench startup, ordinary filesystem
names, interactive `?`, EOF/break continuation, installed pure flags or MorphOS.
With NIL: output, record original VPrintf/PrintFault requests and execution
when observable, but mark delivered stdout bytes unavailable. A later real
output file, Flush/Close and independent DOS Read must establish delivered
bytes and short-write/error behavior. Host fault rendering cannot provide it.

For each launch, capture the selected return and IoErr immediately on return,
before observing GetArgStr/restoring fixture state. Trace or inspect documented
public structures read-only; do not write them to arrange the expected result.
Keep the two command images/leases, loaded segments and all transient parse
storage under explicit ownership. The normal-open case must not acquire a
Workbench message or require a library-style whole-image register guarantee.

## Evidence output and executable next slices

The new runner must bind exact ROM/original/candidate hashes, source and loaded
executor/provider hashes, CPU configuration and overlays. Each receipt needs
planned, entered, returned and accepted counts **separately for original,
generated and comparisons**, plus failed prerequisite/case IDs. Record
argument bytes/length, observed Process/CLI/Input/Output identities, original
vector/code provenance, NULL D3, copied parse values before FreeArgs,
immediate return/IoErr, stream/argument restoration, resource balances and
guard/entry-stack observations. Distinguish emitted DOS call arguments,
observed formatted bytes and delivered stream bytes; an unavailable layer
cannot become an empty successful output. Original inputs remain external
and immutable, including on failure-report writes.

| Suggested next-slice ID | Owner and bounded deliverable | Exit gate |
| --- | --- | --- |
| `CC02.API14.1` | This source/primary-document audit. | Contract exists; no execution or provider-fix status implied. |
| `CC02.API14.2` | MedPlayer licensed fixture: implement R01-R04 readiness only, using public APIs and existing provenance/allocator helpers. | Concrete original Process/stream readiness and ownership observations, or an explicit bounded prerequisite failure. |
| `CC02.API14.3` | Same owner: R05/R06 native observer, then R07/R08 when their stream/fault prerequisites are available. | Actual original RunCommand/NULL-D3 input evidence, with unavailable restoration/failure gates left open. |
| `CC12.MakeDir.3a` | Original A01/A03 then generated A01/A03 through original DOS. | Separate two/two launch receipts and two comparisons; no fake parser and no handler/stdout overclaim. |
| `CC02.API14.4` | CopperStart RunCommand continuation owner: implement input/GetArgStr save/substitute/restore with focused tests. | Meaningful caller-buffer, nested-call, cleanup and failure tests; then native public-boundary tests. Host callback arguments alone do not pass. |
| `CC02.API14.5` | Licensed fixture plus CopperStart native DOS owner: repeat applicable launch cases against a coherently built/installed generated DOS. | Original and generated provider outcomes compared only after native installer/input provenance gates pass. Existing explicit-buffer parser results do not replace this gate. |
| `CC12.MakeDir.4a` | Real original handler path: disposable guest working directory and file-backed input/output; add MK31-B01/B02/B03/B04 and quoted/multiple names. | Original filesystem effects and delivered DOS output compared; then separately run CopperStart's HOST: integration and native handler path with providers labelled. |

Readiness/handler gaps are prerequisite work, not permission to remove commands
from scope. Existing goal/ledger owners decide scheduling and record completed
receipts; this document changes neither their totals nor the stable overall plan.

## Follow-up implementation boundaries

The first source-backed readiness fixture now exists at
[KickstartRomDosCommandLaunchReadinessTests.cs](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosCommandLaunchReadinessTests.cs).
It covers **R01-R02 only**, with a separate licensed category and a structured
observation in the test output. It verifies original native DOS vectors, reads
the boot-created Task/Process without rewriting it, and only queries Cli,
Input, Output and GetArgStr after finding a mapped NT_PROCESS. Missing CLI or
handles fails the prerequisite rather than passing a command test. Public
process creation when needed and stream acquisition/restoration (R03-R04)
are subsequent work.

A separately frozen host variant executed that fixture against original DOS
40.3. **R01 passed and R02 failed; zero rows were skipped.** The actual
NT_PROCESS at `$00205EE0` was readable, with CLI `$002028B8` matching its
public BPTR. Input, Output and GetArgStr all returned zero. All four immediate
IoErr queries returned zero; task/SP and original native vector identities
were unchanged. Thus this context needs public stream acquisition, not a
fabricated Process or CLI. Command, ReadArgs and RunCommand counts are all
zero. Receipt SHA256 is
`e0c0790fb501fb76b00ef2b815af814fa1485464c08eec55784b7655723264e4`,
TRX `17c00807027d52240035b9af767476821041777a3e6f4d3ee52cc8a1d80c96d5`;
the host DLL is `03b269303b4e9d8fbc8d49f07e4e77a23eefe1ffc3e364bdd9443d3d7497c886`.
See [reference execution](reference-execution-paths.md) for the private source,
dependency and toolchain binding. The earlier failed namespace/size compile
variant is retained separately and did not execute a readiness case.

The next private host variant executes
[KickstartRomDosCommandStreamReadinessTests.cs](D:/Koodit/GIT/MedPlayer/CopperMod.Amiga.Tests/KickstartRomDosCommandStreamReadinessTests.cs).
**R04 passes** against the same original DOS/real Process/CLI configuration.
Public Open(NIL:,1005/1006) returns real input/output BPTRs `$00080B6B` and
`$00080B79`. Selection and public queries agree. Both borrowed zero selections
are restored before closing the two acquired handles once; both Close calls
return `$FFFFFFFF`. All 19 recorded DOS calls return with stable task/SP and
immediate IoErr zero, and the full 154-slot DOS vector table remains unchanged.
There are no known unclosed handles, unrestored selections or cleanup failures.
No new Process is created or scheduled, and no command, ReadArgs or RunCommand
is invoked. NIL: ownership does not prove nonempty input-buffer restoration
or delivered stdout. The receipt is
`e3d434be39f8687977d585f61faa14f3f8b42047c0fed200c135223ae6a76940`,
TRX `fdceab3f2fa0fcb78a52ffed002dc08e94007b8f6473c2a2e0e813c71f758f73`;
private host DLL
`8e95ed25f1084798db9893be2264f9187e27402da73079ae27d00ec45e4449ce`.
The original failed R02 receipt remains an observation of its unmodified
initial context; it is not relabelled as this later R04 pass.

A later private variant adds an authored 908-byte two-segment observer.
Original RunCommand enters it once, supplies the 25-byte tail
`alpha "two words" "a**b"` plus LF and its stored NUL, and returns 42 after
3,381 instructions. Public GetArgStr equals startup A0; original
ReadArgs(NAME/M, result, NULL) returns `alpha`, `two words`, `a*b`.
Values are copied at a read-only native checkpoint before the one FreeArgs.
The observer opens/closes its own DOS reference. Startup A6 is `$00F9FE9E`,
not DOSBase. The inherited SR is `$2000`; this is the existing supervisor-mode
component fixture, not normal user-mode Shell startup.

Task/CLI identity, Process argument/return pointer, stack bounds and observed
NIL: buffer fields are restored. The owned NIL: selections are restored before
closing; HUNK/string allocations are released, and original DOS vectors and
observer code remain unchanged. The test passes once with zero skipped.
Its [bound receipt](D:/TestData/CopperOSCommands/BuildSnapshots/dos-command-input-host-20260830T144532Z-886cebc7/results/private-original-dos-command-input-observer-receipt.json)
has SHA256 `8436167bea33f4e10013fab34a01919c419bf0425cb0201bf62e7ac7c1955f29`;
the [compact observations](D:/TestData/CopperOSCommands/BuildSnapshots/dos-command-input-host-20260830T144532Z-886cebc7/results/original-dos-command-input-observer-summary.json)
are `c6c96c9d57a41a4bde792f470be7da3a67d3f9dc1441b49bdaf21f4a3711d79d`.
Counts are one authored observer, one RunCommand, one ReadArgs and one FreeArgs;
zero original-distribution/generated CopperOS commands and zero comparisons.
This extra nonempty variant does not itself close exact LF-only R05 or
`alpha beta` R06, and empty NIL: state does not close R07 or delivered output. See
[reference execution](reference-execution-paths.md) for source/host binding.

Exact R05 and R06 subsequently pass in a separate two-test run, with zero
failures/skips. The same authored observer receives LF-only (length 1) and
`alpha beta` plus LF (length 11), respectively. D0 has the exact length;
GetArgStr equals entry A0. Original ReadArgs receives D3 NULL and returns an
absent /M list for R05 or `alpha`, `beta` for R06. Each case performs one
FreeArgs, returns 42/IoErr 0 and restores the observed process/input/stack
state, with code/vector/argument integrity and full owned cleanup. There are
two authored observer calls, zero distribution/generated C: command calls.
Entry SR remains `$2000`; this is a component observation.

The exact-input [receipt](D:/TestData/CopperOSCommands/BuildSnapshots/dos-exact-command-input-host-20260830T151452Z-520a149f/results/private-original-dos-exact-command-input-observer-receipt.json)
has SHA256 `9ffd6e9d09f9e6b724f9bbc769ba5927907dbd5acdac9deb622572f19457ca2a`.
The [compact observations](D:/TestData/CopperOSCommands/BuildSnapshots/dos-exact-command-input-host-20260830T151452Z-520a149f/results/original-dos-exact-command-input-compact.json)
are `49a72ce68c07df7c2bf3fb0b5c7f73d49b617dbc3b37c994ca0b86faaa6b4c6a`.
The earlier 25-byte receipt is unchanged. R07's unread real stream and R08's
controlled original allocation failure remain required, separate observations.

The earlier owner review divided API14.4 into three connected repairs below.
Subsequent source-bound work is recorded in the
[input-context implementation notes](runcommand-input-context-implementation-notes.md)
and [host callback qualification](dos-host-callback-qualification.md). The
portable lease, native bridge/rejection paths and host ordinary-return owner
have passing bounded suites; the historical source defects below are not a
description of every current file. Composed native normal-input launch and
the [removed-task owner steps](runcommand-retirement-owner-plan.md) remain open.

The later [input-context implementation notes](runcommand-input-context-implementation-notes.md)
name exact fields, buffered-position accounting, nonce/nesting requirements,
both native failure dispatchers and reset/retirement ownership. Their separate
receipts distinguish ordinary return and proven non-entry from cancellation
after task removal.

1. **Invocation-owned input state.** Preserve the first sixteen continuation
   bytes and existing StackSwapStruct offset. Capture the public Process,
   its private process-state identity, selected Input BPTR and nested invocation
   relationship. Save public `Process.Arguments`, process-level pushback and
   the stream's buffered-input fields/ownership; private process
   `Arguments/ArgumentsCapacity` instead own process-creation storage and must
   not be replaced. Install a continuation-owned temporary input buffer on
   the **same handle**, copying the supplied length without appending LF and
   without advancing its underlying position. Restore the caller's unread
   buffer and public argument pointer before freeing temporary storage.
   Validation/allocation must finish before publication; failed callback starts
   need the same rollback. Stale, wrong-task and out-of-order resume tokens
   cannot restore another invocation's state. Existing
   `CreateCommandInputHandle` creates a different stream and appends LF, so it
   is not a substitute for this boundary.
2. **Native stack and entry bridge.** The current ordinary C# callback switches
   SP through Exec StackSwap, then accesses locals from the old frame. In the
   source-bound private Unified 68000 HUNK
   `9a9a69046c0595e59e90f087a2f8be948f4ee4a49d8a1b84555a3c7bde41b403`,
   `InvokeGuestCallback` starts at code offset `$20BA`, size 674. StackSwap is
   called at `$22A8`; `$22AC-$22BC` read old-frame fields through the new SP.
   The command call at `$22CA` is followed by a write through A4 at `$22CE`,
   although N7 allows the whole command to destroy A4. The mapped offsets are
   bound by map SHA256
   `782b749cc841c489fc4a6736427c0dff2a41834721296505eb2077bc71bdd67f`.
   This is emitted-code inspection, not a newly executed mismatch. Use a
   dedicated native bridge spanning both switches and the complete command:
   recover its private control state through SP after entry returns, retain
   the command result through the second StackSwap, and restore the original
   stack before accessing the old frame. Preserve the outer DOS library ABI.
   An ordinary indirect-call declaration or a global clobber relaxation cannot
   repair old-frame accesses on another stack.
3. **Callback and termination ownership.** The native dispatcher currently
   leaves a failed callback without `Resume(false)`; the host callback registry
   permits only one pending callback per task and can abandon a nested one.
   Missing callback support, invalid stacks and rejected starts must restore
   the newly installed state. Reset/retirement must unwind nested buffering
   contexts before releasing their streams, and must not free a command stack
   while native code still uses it. Original command Exit is a separate
   reference requirement; it cannot be equated to arbitrary callback failure
   or independent Process termination.

Focused owner tests must exercise actual NULL-D3 ReadArgs during the callback,
both pushback layers, a genuinely prefetched unread caller buffer, preserved
Input/GetArgStr/position, nested and interleaved Processes, inner allocation
failure and stale/out-of-order resumes. Bridge tests must execute generated
68000/020/040 instructions with an entry that destroys D1-D7/A0-A6, returns a
chosen D0 and preserves SP. Check outer ABI, both stack bounds and ownership.
These tests do not settle original post-argument refill, raw Read, child
SelectInput/SetVBuf or Exit behavior. Blindly restoring the entire handle
record could discard legitimate underlying stream changes.

## Historical audited source identities

These SHA256 values identify the files read for the source-gap finding, not a
build or native qualification manifest. Both checkouts may contain uncommitted
work; HEAD alone is not source identity. Re-read/re-hash changed owners before
implementation. Source roots are absolute; paths below are relative to the
listed root. Hashes reflect this audit, not future implementation state.

| Root | Path | SHA256 |
| --- | --- | --- |
| `D:/Koodit/GIT/CopperOS` | `src/Commands/Native/NativeCommandArguments.cs` | `5728d3b795a83f1cd7edf21cdfddc6db610fb4f0ce517ebd075e6dd8311ebc82` |
| same | `src/Commands/Native/NativeCommandStartup.cs` | `acdca20c49434b4b11af987adc2c87e25919a50e11d0fa3d5204b1ffeaff30d1` |
| same | `src/Commands/Native/Workbench31MakeDirCommand.cs` | `54b873febd9a764000167f43b6287d3ac2e8b8fa3898d206623f12b91aacf2a4` |
| same | `tests/Commands.NativeRoot/Workbench31MakeDirProbe.cs` | `1b23ea2d72ec5d67eb4305b116bb81ac167df20296e8356328c29d708edd9052` |
| `D:/Koodit/GIT/CopperStart` | `src/CopperStart.Dos/DosRunCommandCore.cs` | `72738baa92e4ea510c247711ccc535c67cf8878b370b0ee4ffbaa32192297671` |
| same | `src/CopperStart.Dos/DosVectorRouter.cs` | `9f6aed3e3a6c016480e929d830c5a0f0815bba6e96d727aac0f2abcc7ca66f5c` |
| same | `src/CopperStart.Dos/DosNativeEntrypoints.cs` | `328162ae83a71cad9d62369fbbcb8998b59e90a3c9bd2eb12886b941a924bbf7` |
| same | `tests/CopperStart.Exec.Tests/DosRunCommandCoreTests.cs` | `7967325d3349962f9cf1b72daf482994206b144ef1aa1924b5bb7d2ca59e86c0` |
| `D:/Koodit/GIT/MedPlayer` | `CopperMod.Amiga.Tests/KickstartRomDosReadArgsDifferentialTests.cs` | `b2b2757c2abb2a2bd9a2df9a1e475e2959fe6481f39fb269767a3ea219819ca4` |
| same | `CopperMod.Amiga.Tests/KickstartRomDosDifferentialTests.cs` | `b38b855f89df5b8580005aef1b006933aad7a8110075417a80df751f701e0c8c` |
| same | `CopperMod.Amiga.Tests/KickstartRomLayersDifferentialTests.cs` | `f574672756e17c6f68676e0c0faef4e91dbbea1bd1338c94191edf5a7ba4be6a` |
| same | `CopperMod.Amiga.Emulator/CopperStart/Dos/DosServices.cs` | `c2902d7881d684b39e03b5784c214f9f271b67836559946c329d37c1368c3d7d` |
| same | `CopperMod.Amiga.Emulator/CopperStart/Dos/ConfiguredDosHostHandler.cs` | `e4c86cc007f95d39697cf95eb0cdafa74dc3bddc627e77c778612ab0436d4c28` |
| same | `CopperMod.Amiga.Emulator/AmigaBoot.cs` | `089c96cf067c5cfce7bf0b2e242147db47ceb4137014afc5b59c5078afece30d` |
| same | `CopperMod.Amiga.Emulator/AmigaHunkProgramLoader.cs` | `7f85f4e7580f8cccebdfd09d073d50ed36bbfd6d6f22a6d33861f27633639e8d` |
| same | `CopperMod.Amiga.Tests/DosServicesTests.cs` | `520c65070f2019d5e893bed57799876077d0f1626610058b9d6cbb5faa3fe397` |

Observed CopperStart HEAD: `d30497b315e819ce5cd3881bfe975c818088b642`.
Observed MedPlayer HEAD: `cf4da5dd1c77aa925959319e13940960ad5dad5c`.
Only this document was written for the original audit slice; no source/test/
build changes or command/runtime execution were performed by that audit.
Later source work and observations are identified separately above and in the
coordinating progress log; the historical hashes are not refreshed by assertion.
