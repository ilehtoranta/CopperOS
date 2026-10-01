# Workbench 3.1 and MorphOS 3.20 command completion ledger

Seeded 2026-08-30 from the [versioned command inventory](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/command-inventory.json).
This is execution state for the [stable goal plan](D:/Koodit/GIT/CopperOS/Goals/WORKBENCH31_MORPHOS_C_COMMANDS_GOAL.md); it does not change
the plan, either reference baseline, or command membership. The inventory binds
plan SHA-256 `dfcca7b0f5f00cdab28554a013b3d6c7d1bd345f856d61a4fd4aeedd5c4ce09d`.

**Qualified shipping commands: 0 of 200. Qualified shipping profiles: 0 of 246.**
There are 58 required `wb31` profiles and 188 required `morphos320` profiles.
Forty-six command identities have both profiles. The 31 existing Shell internal
names remain a separate regression dependency; they are not new external rows.
`Freeze` remains an explicit CC39 discrepancy outside the initial 200.

Latest reference-closure checkpoint (2026-09-23): the externally supplied M10
Install disk 1 ADF is hash-verified and all 20 listed `C:` members, including
installation-only helpers, have durable metadata in
`reference-captures/wb31-disk1-c-command-captures-20260923.json`. This is
identity evidence only. Its three installer/startup script members are also
hash-bound in `reference-captures/wb31-disk1-install-script-captures-20260923.json`;
disks 3-6, installed placement, runtime contracts and all shipping admission
gates remain open.

Latest CC11 DOSList checkpoint (2026-09-23): MorphOS `DOSList` captures device
and volume verbose attributes, source-ordered assign target/lock/type rows,
and linked device/volume/assign lock lists through public DOS vectors. It uses
public `DateToStr` for volume dates and copies returned strings and lock data
before releasing each list lock. Its three-CPU resident receipt passes 34
vectors per CPU in
`artifacts/doslist-morphos-native-20260923-volume-v1/qualification.json`;
original guest comparison, PURE/resident lifecycle, source reuse rights,
package admission and differential evidence remain open.

Latest CC11 DOSList common-row checkpoint (2026-09-23): MorphOS `DOSList` now queries
node names and message ports independently through public
`GetDosObjectAttrTagList`, preserves source fallback behavior when either
attribute is unavailable, and resolves embedded/signal-port processes through
`Exec.TypeOfMem` and public task/message-port fields. Its resident
68000/020/040 receipt passes 23 supplied vectors per CPU in
`artifacts/doslist-morphos-native-20260923-object-attrs-v1/qualification.json`.
Provider-specific verbose detail rows, original guest comparison,
PURE/resident lifecycle, source reuse rights, package admission and
differential behavior remain open.

Latest CC11 Type checkpoint (2026-09-27): MorphOS resident qualification passes
16 supplied vectors per CPU on 68000/020/040, and the separate Workbench
five-slot syntax candidate passes 17 per CPU. New cases inject DOS Read
failure and a positive short Write followed by an error; both preserve the
selected IoErr and close the open input/matcher resources. Receipt paths are
`artifacts/cc11-type-morphos-native-20260927-io-failures-v1/qualification.json`
and
`artifacts/cc11-type-wb31-native-20260927-io-failures-v1/qualification.json`.
The Workbench failure outcomes are candidate-modeled. Original output and
diagnostic parity, packed correspondence, PURE/resident lifecycle, licensing,
and package admission remain open.

Latest CC11 MorphOS Type checkpoint (2026-09-23): the resident entry now
uses the MorphOS 50.67+ extended `AnchorPath` fields for literal soft links,
matching the documented source intent, and returns `ERROR_NOT_IMPLEMENTED`
for an older DOS 50.66 provider before opening streams or matching paths. Its
68000/020/040 fixture receipt passes 14 supplied vectors per CPU with 26
reachable methods. Workbench 3.1 retains classic `AnchorPath` fields and passes
15 supplied vectors per CPU with 27 reachable methods. These fixtures do not
prove original guest output, provider behavior, packed correspondence, or
PURE/resident and package admission.
Latest CC11 resident requalification checkpoint (2026-09-23): MorphOS Info
validates all `DEVICES/M` patterns with the source's 128-byte DOS
`ParsePatternNoCase` buffer before allocating its snapshot or locking the DOS
list; malformed patterns return `RETURN_ERROR`, preserve `IoErr`, and publish
the outer `PrintFault` without touching the list. Ctrl-C is now polled during
source-ordered rendering, preserving the partial device row before the
non-OK `PrintFault`. Records still sort through utility `Stricmp`, with all
device rows before the volume section. The receipt also covers
`GetDevStr`/`GetStartupStr`, per-device Lock/Info policy, `GOODONLY`,
`GetVar("info_datetime")`/Locale `FormatDate`, source startup ownership,
counter fallbacks and type/name rows. The 68000/020/040 resident receipt passes
66 supplied vectors per CPU in
`artifacts/cc11-info-morphos-native-20260923-pattern-validation-v3/qualification.json`.
Workbench Info was rerun against the shared fixture and passes 14 vectors per
CPU in
`artifacts/cc11-info-wb31-native-20260923-pattern-v14/qualification.json`;
that combined test-root HUNK also carries the MorphOS hook export and is not a
Workbench shipping artifact. The inspected source disables catalog opening
under `#if 0`, but packed-binary output, original guest parity, provider
details, PURE/resident lifecycle and package admission remain open.

Latest CC11 Search checkpoint (2026-09-27): MorphOS `Search` retains the
active source's locale.library v37 minimum and uses `IsCntrl` and `IsPrint` for
control delimiters and output sanitization, preserves TAB as data, and advances
displayed line numbers only on LF. The resident 68000/020/040 fixture passes 46
vectors per CPU with 43 reachable methods in
`artifacts/cc11-search-morphos-native-20260927-source-file-errors-v1/qualification.json`.
It covers the source's 512 KiB `MEMF_ANY` buffer sizing, allocation-halving
fallback including failed-first-allocation recovery, LF-only maximum-line
pre-scan, overlong-line growth, incomplete-line rewind/reread, short reads in
the pre-scan and CTRL-D during that pre-scan. For DOS 51.28+, it also covers
`Size64`, DOS `Seek64` absolute/signed-relative offsets, and error handling for
a synthetic file larger than the classic 32-bit offset range. The fixture also
retains locale folding/classes, context output, nested/sibling traversal, soft
links and QUICK behavior. Callbacks exercise 0x85 as a control and 0xe9 as
printable; the real default-locale table and real large-file handler behavior
remain unverified. Source inspection also establishes per-file Open/Read/Seek
failures as misses, continued traversal with cleared IoErr on successful
completion, and dangling soft-link warnings that skip the link. The new MorphOS
vectors cover those outcomes. Workbench Search passes 19 vectors per CPU with
43 reachable methods in
`artifacts/cc11-search-wb31-native-20260927-source-error-regression-v1/qualification.json`,
including a matching line longer than 8,192 bytes written in bounded DOS
chunks. These are fixture results only; real MorphOS >2 GiB handler behavior,
original output parity, packed correspondence, real memory behavior, PURE/
resident admission, licensing, packaging and differential evidence remain
open.
CC10 resident requalification checkpoint (2026-09-22, historical): current-source
MorphOS PathPart, Which, and bounded Quote STR entries pass 11, 16, and 12
supplied vectors per CPU respectively on 68000/020/040. Receipts are
`artifacts/cc10-pathpart-native-20260922-v1/qualification.json`,
`artifacts/cc10-which-morphos-native-20260922-v1/qualification.json`, and
`artifacts/cc10-quote-native-20260922-v1/qualification.json`. These receipts
do not advance shipping rows; exact grammar/output, original parity, PURE,
packaging, licensing and differential gates remain open.

Latest MorphOS PathPart checkpoint (2026-09-28): the candidate now sizes its
invocation-local output buffer from `DIR`, `FILE`, and every `ADD/K/M`
component, removing the former 1,024-byte and 64-component candidate limits.
The resident 68000/020/040 qualification passes 15 supplied vectors per CPU,
including 1,100-byte directory/file output and 70 `ADD` components producing
more than 1,024 output bytes. Receipt:
`artifacts/cc10-pathpart-native-20260928-dynamic-output-v5/qualification.json`.
This remains DOS-adapter fixture evidence; original MorphOS behavior, exact
template/mode semantics, installed P, lifecycle, licensing and package
admission remain open.

Initial CC11 inspection requalification checkpoint (2026-09-23): current-source
Workbench Type passes 15 supplied vectors per CPU, MorphOS Type 13, Workbench
Search 18, MorphOS Search 17, and MorphOS Dir 15 on 68000/020/040. Receipts:
`artifacts/cc11-type-wb31-native-20260922-v1/qualification.json`,
`artifacts/cc11-type-morphos-native-20260922-v1/qualification.json`,
`artifacts/cc11-search-wb31-native-20260922-v1/qualification.json`,
`artifacts/cc11-search-morphos-native-20260922-v1/qualification.json`, and
`artifacts/cc11-dir-morphos-native-20260922-v1/qualification.json`; the
Workbench Dir profile also passes 16 vectors per CPU in
`artifacts/cc11-dir-wb31-native-20260922-v1/qualification.json`. These
receipts do not advance shipping rows; exact original output, complete
recursion/interactive behavior, packed correspondence, guest parity,
PURE/resident admission, packaging, and differential gates remain open.

MorphOS Search now has bounded source-informed `ALL` traversal and locale
folding: its 20-vector per
CPU resident fixture covers AnchorPath directory entry/descent, child-file
search, `NameFromLock`/`AddPart` path building, directory exit and traversal
completion, plus locale.library/OpenLocale ownership and a non-ASCII literal
case pair, in `artifacts/cc11-search-morphos-native-20260923-locale-v4/qualification.json`.
Workbench Search is bound to a captured disk 2 binary and eight-slot template
candidate; its separate DOS 36 entry passes 18 vectors per CPU, including the
same bounded directory traversal fixture, in
`artifacts/cc11-search-wb31-native-20260923-locale-v2/qualification.json`.
This remains partial evidence; deep trees, MorphOS soft links, locale/output
failure details, original guest comparisons, PURE/resident admission and
packaging remain open.

Latest Exe2Arc component checkpoint (2026-09-23): `Exe2ArcScannerSelection`
now binds explicit TYPE dispatch and the unspecified ZIP, ACE, RAR, CAB, ARJ,
LhA, LZH order. Only `NoMatch` falls through; offset-zero and unsafe seek
results remain terminal. Five focused tests pass, the managed Commands suite
passes 704 tests, and the native project builds cleanly with the owner linked.
This does not advance any shipping row: frontend resource ownership,
diagnostics, cleanup, guest parity, PURE/resident qualification and package
admission remain open.

The same checkpoint now includes `Exe2ArcResultPolicy`: completed scans report
detection, only nonzero extraction reports success, and failed/empty extraction
requests close-then-delete cleanup while source-compatible ZIP Ctrl-C retains
partial output with `ERROR_BREAK`; pre-open failures do not. Twelve focused
policy controls pass; the full managed Commands suite passes 704 tests.
This remains component evidence and does not advance the Exe2Arc shipping row.

`NativeMorphOSExe2ArcCommand` now composes those owners into one bounded
MorphOS frontend with parser, input/FIB/workspace, output and extraction
leases. It builds cleanly and never executes the input file. Exact diagnostics,
original guest behavior, resident/PURE lifecycle and package admission remain
open; no shipping row is promoted.

The composed frontend also has a reproducible resident static qualification:
`artifacts/exe2arc-native-static-20260922-v8/qualification.json` records
68000/020/040 HUNKs with 100 reachable methods and zero managed allocations,
runtime features/helpers, external native targets, exception regions or fatal
machine-fault sites. The paired supplied-vector fixture passes two parser and
result-failure cases per CPU with balanced DOS ownership and expected result
and `IoErr` values. This evidence does not promote the Exe2Arc row; original
guest/filesystem execution, exact diagnostics and full output behavior,
PURE/resident lifecycle, packaging and shipping admission remain open.

Latest Workbench `FindResident` checkpoint (2026-09-21): the separate DOS 36
capability-floor resident syntax candidate uses the captured `MODULE/A`
boundary and passes eleven supplied DOS 36/Exec vectors per CPU on
68000/020/040, including found and missing residents, parser/result-slot
failures, startup and missing-DOS guards, and interleaved callers. Receipt:
`artifacts/findresident-wb31-native-20260921-v4/qualification.json`. The
captured binary requests DOS 37; exact original diagnostics/result precedence,
guest parity, PURE/resident lifecycle and package gates remain open.

Latest Workbench `Check2090` checkpoint (2026-09-21): the separate DOS 36 /
expansion 33 capability-floor
resident installation helper passes ten supplied Exec/Expansion/DOS vectors
per CPU on 68000/020/040, covering A2090 controller flag states, expansion
failure, startup boundaries, missing DOS and interleaved callers. Receipt:
`artifacts/check2090-wb31-native-20260921-v3/qualification.json`. The captured
DOS/Expansion37 requests remain differential evidence; original guest
result/IoErr parity, PURE/resident lifecycle and package gates remain open.

Latest Workbench `IconPos` checkpoint (2026-09-20): the separate DOS 36
resident installer candidate passes fifteen supplied Exec/DOS/icon.library
vectors per CPU on 68000/020/040, covering DiskObject position/type and
drawer mutation, free-position switches, image copying, default creation,
parser/type and storage failures, startup boundaries, missing DOS and
interleaved callers. Receipt:
`artifacts/iconpos-wb31-native-20260920-v2/qualification.json`. Exact
original icon.library semantics, guest parity, PURE/resident lifecycle and
package gates remain open.

Latest Workbench `GuessBootDev` checkpoint (2026-09-21): the separate DOS 36
resident installer candidate uses the captured `BOOTDISKNAME` template and
passes sixteen supplied Exec/DOS/utility.library/expansion.library vectors per
CPU on 68000/020/040, covering signed boot priorities, disabled and unusable
filesystems, lock fallback, parser and library-open failures, startup guards,
missing DOS and interleaved callers. Receipt:
`artifacts/guessbootdev-wb31-native-20260921-v2/qualification.json`. Exact
original boot-node semantics, guest parity, PURE/resident lifecycle and package
gates remain open; the candidate floors are DOS36, utility0 and expansion33.

Latest Workbench `ExtractKickstart` checkpoint (2026-09-21): the source-derived
contract, separate resident native candidate, and bounded native trackdisk/DOS
fixture now cover the captured
`DEVICE/A,TO/A,1.3/S` boundary, `DF0:`-style validation, raw
`trackdisk.device` access, `KICKSUP0` boot-block validation, layout-specific
reads, and invocation-owned I/O leases. Three-CPU resident HUNK compilation
passes with fourteen reachable methods and no managed allocation/runtime
features; the fixture passes seventeen vectors per CPU, including parser
ownership, 1.3/default reads, diagnostics, failure paths, cleanup and
interleaving. No guest or shipping gate is claimed; exact image layout,
guest, PURE/resident and package evidence remain open. Receipt:
`artifacts/extractkickstart-wb31-native-20260921-v6/qualification.json`.
See `contracts/ExtractKickstart.md`.

Latest Relabel boundary checkpoint (2026-09-17): four FFS pairs verify
thirty-byte success, thirty-one-byte/empty/slash rejection (20/210), unchanged
labels on failure and successful recovery (0/0). Intermediate sectors, exact
diagnostics, payload/metadata and ownership checks pass, with sixteen controls
(`relabel-name-boundaries-20260916-v2`). Other handlers and full admission remain
open.

Latest Relabel filesystem checkpoint (2026-09-16): two paired device/volume-name
renames on a writable owned DOS1 floppy return 0/0 and persist the final label.
Independent sector readback proves only root label/date/checksum fields changed;
all file bytes, metadata and bitmap remain intact. Twelve corruption controls
pass (`relabel-persistent-20260916-v6/qualified`). Fresh ROM guests now also
mount each exact export read-only, resolve SavedDisk: and read both files
without executing Relabel or changing disk bytes; twelve controls pass
(`relabel-cold-remount-20260916/qualified`). Other filesystem/platform and full
PURE/package gates remain open.

Latest Relabel lifecycle checkpoint (2026-09-16): active replacement refusal,
successful idle replacement, missing/invalid-source retention and normal
background caller self-removal have paired original/replacement boot evidence.
The four captured caller-owned memory spans (3520 rounded bytes, including
Process/stack) are also proven freed before address reuse. Idle replacement preserves the registry node, loads the new image
before releasing the old, and reuses the new image for two calls; both image
lifetimes and per-call ownership pass. These bounded checks do not establish
original PURE classification or shipping admission. See
[Relabel boot evidence](relabel-original-dos-boot.md).

Earlier native launch checkpoint: [native DOS admission](native-boot-integration.md)
executes two consecutive MakeLink sessions in one boot/library instance in
attempt 41. Task/CLI retirement and free-memory balance pass after each complete
session, including driver cleanup. Attempt 42 also verifies one native exit
callback per successful session through public NP_ExitCode/NP_ExitData. Deferred
cleanup, nonzero/replacement callback results,
BCPL, same-SegList concurrency and resident reuse remain open. These sessions
reload the command image and therefore do not qualify PURE execution.
No command, profile, PURE gate or shipping count is promoted.

The totals are a verified seed, not final media closure. Append any newly
established required identity/profile with its evidence and owner, and update
the totals. Never delete a row because its library, device, reference runtime,
or implementation is unavailable. An evidenced scope correction needs a dated
disposition that retains the old identity and reason.

## How to read and update a row

The stable row ID is `CCxx.Command.profile`; command spelling and owner come
from the inventory, not a documentation index. Each row inherits `D-CORE`
below and its listed family dependency ID. Resolve family dependencies per
command and mode; the table does not declare that every member needs every
service in its family.

| Status | Meaning |
| --- | --- |
| open | Required gate has no complete accepted evidence. A source stub, build, or helper does not close it. |
| partial | Identified work/evidence exists, but required cases remain. Link the concrete evidence and next slice. |
| blocked | A named external or implementation prerequisite prevents this gate; other eligible gates may still proceed. |
| pass | All cases for this gate and this exact profile/artifact have accepted evidence. Record report/capture paths and hashes. |
| required/open | Original design evidence requires pure/resident compatibility; the replacement's static and same-SegList runtime qualification is open. |
| audit/open | Original pure/non-pure classification remains unresolved, and replacement qualification is open. This never means NonPure or exemption. |

The seven gates are independent:

- **Spec:** exact profile grammar/template, aliases, lifecycle, effects, streams,
  results, `IoErr`, cancellation and reference identity are frozen.
- **Options:** every applicable option/mode/default has success and relevant
  failure/interaction coverage, including reference parser/help behavior.
- **Semantics:** the replacement implements the contract through the owning guest
  APIs, with meaningful behavior/error/resource checks.
- **Native:** the actual command HUNK and native ABI/resource paths pass the
  required 68000/020/040 execution matrix. Probe results are not command results.
- **Purity:** original classification is resolved; required commands pass
  hash-bound static checks and repeat/concurrent use of one loaded segment,
  including lifecycle/failure cleanup. Other classifications need evidence.
- **Package:** the correct normal or installation-media artifact, version, stack,
  paths, dependencies and Amiga metadata are reproduced and verified in an image.
- **Differential:** mandatory Workbench/MorphOS reference comparisons and boot/
  runtime integration pass, with only explicit justified normalization.

A shipping profile requires all seven gates to pass. A shipping identity requires
every applicable profile. No row below currently passes any gate in full.
A contract file's existence or a media template candidate is not a closed spec
or option matrix. Document findings in the
[progress log](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/progress-log.md); update affected rows only after the
evidence exists. Keep raw-media facts in the inventory distinct from execution
state in this ledger. Qualified artifacts and aggregate acceptance belong in
`build-manifest.json` and `qualification-report.md`, maintained separately.

## Evidence and foundation snapshot

Read the [media authorities](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/authorities.md) and
[hashed media/script evidence](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/media-evidence.json) for exact reference
locations, captures and confidence. Original pure requirements currently apply
to **95 profiles: 7 wb31 and 88 morphos320**. The other **151 profiles remain
unresolved**. All 70 inspected classic file entries have P clear, yet original
scripts force resident use of seven distinct commands. MorphOS installer P
additions are script intent; ISO POSIX modes are not native Amiga protection.
No installed reference system or replacement purity has been qualified.

| Stage | Current bounded evidence | Remaining gate / next action |
| --- | --- | --- |
| CC00 | Full official MorphOS ISO integrity verified; 200 identities, 246 profiles and original script events reproduced. Inventory validator: 16 tests pass. | Six closure items below remain. All-media/installed/runtime closure is not complete. |
| CC01 | The contract set now includes a source-bound Protect grammar and bounded native evidence alongside the existing partial behavior/option work; MorphOS CLI, MakeDir, Workbench Type and FileNote still have media identity and/or syntax-candidate evidence only. | Complete exact grammar/options and reference behavior one command/profile at a time. |
| CC02 | [Baseline/API audit](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/baseline-and-api-audit.md); 232 baseline tests pass after Support references/alignment fixes, 433 including later numeric/archive slices. Compiler call/field repairs and the stack bridge pass 418 inclusive checks; native rejection repair passes 24/295. The [constructed-scalar repair](constructed-scalar-qualification.md) passes 109 focused/289 selected checks plus 77 retained Exe2Arc probe entries. The 68020 opcode48F9 correction passes 18 focused/633 selected CPU checks. [Close BOOL semantics](close-boolean-qualification.md) pass 30/294 portable and five/37 host checks; the separate [write-error correction](close-write-error-qualification.md) passes 45 native, 323 portable, 64 host and 14 existing provider checks. | Continue consumed API/version and per-command integration audit. Raw Write/FPutC errors, original Close failure precedence, current composed input/retirement qualification and nonlocal Exit remain open. Component tests do not qualify native commands. |
| CC03 | The current Foundation receipt passes 31 startup, 25 argument-boundary and 32 I/O invocations on each of 68000/020/040: 264 total across nine HUNKs. A separate 3,192-byte authored 68000 probe now runs through original Workbench startup/ReadArgs/SystemTagList and captures ten original Version calls across the v1/v2 aggregates. Ten corrected-candidate pairs now match exactly at `artifacts/workbench31-guest-version-pairs-20260926-v2/qualification.json`; earlier positive/negative controls remain retained. | The diagnostic retains its process/storage for passive capture. Production teardown, varied stacks/faults, child Result2 propagation, broader replacement comparisons and native CopperStart execution remain open. Supplied-vector fixtures are still not full OS execution. |
| CC04 | [Invocation-owned ReadArgs lease](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/argument-ownership.md) passes allocation/parser/cleanup and boundary cases. [Native I/O](native-io-contract.md) passes 96 supplied-vector cases; the [parser repair](readargs-parser-qualification.md) passes 44 exact explicit-source pairs. The [input lease](runcommand-input-context-implementation-notes.md) passes 87/271 portable checks. Historical native input passes six invocations each on 68000/040; a separate CPU-only rebind now passes six on 68020. The [host callback owner](dos-host-callback-qualification.md) passes 15/32. [Synthetic retirement](task-retirement-qualification.md) passes 50 new cases and 34 controls. Read-only RET03.1 inspection passes 31/337 portable and 26/63 host checks; the separate [FreeMem range query](free-range-inspection-qualification.md) passes 95 cases after odd-address repair. | The original failed three-CPU matrix is retained; the later 020-only result is a distinct runtime checkpoint. RET02 is partial: exact DOS callback/stack handoff, native Switch and outstanding packet/notification ownership remain open. The new RET03 host baseline has four liveness failures and six guard controls. Nonlocal Exit, broader parsing and complete resource integration remain open. |
| CC05 | The [production filesystem integration](copperstart-filesystem-integration.md) now connects native DOS packets to the existing configured host filesystem. Twelve focused tests pass, including five versioned MakeLink invocations; a separate real DosServices/Exec queue test verifies two-volume routing and exactly-once replies/lock release. The SDK FileHandle cookie fields are corrected and pass 128 focused checks. | Combine the native command with production Exec delivery and Shell launch in one system run. These separate tests do not qualify complete filesystem semantics, all command options or resident lifecycle. |
| CC06 | The private probe reuses one protected image per CPU for repeated and instruction-interleaved calls; no image writes or fixture leaks. A disposable two-process MakeLink probe now binds one shared segment, distinct tasks, returns 0/20, balanced invocation ownership and an unchanged image; see [same-SegList report](../../../artifacts/workbench-makelink-same-seglist-20260911.json) and [verifier](../../../tools/Commands/verify_workbench_makelink_same_seglist.py). | The probe intentionally omits resident registration/removal and is marked `pureAdmission=false`; per-command original classification, production resident registry lifetimes, active-use/replacement/deferred/task-death cases and full same-SegList OS qualification remain open. |
| CC07 | [Archive provider work package](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/archive-provider-work-package.md) maps all 16 CC31/CC32 commands to 13 dependency owners and nine slices. Exe2Arc's corrected RAR4/CAB/ACE headers, bounded scanners and separate copy pass the existing host checkpoint plus ACE cases. Its refreshed supplied-DOS-vector native checkpoint passes 252 component entries on three CPUs (84 per CPU), including ACE scan/copy and interleaved callers. | Implement the required services and remaining language, network, desktop, media and hardware dependencies. Exe2Arc still needs the other layouts, frontend/cleanup, real handlers and original packed-binary correspondence. Neither archive component checkpoint qualifies a full command. |
| CC08 | The [compiler-repair refresh](command-fixture-requalification.md) retains fifteen private reproducible HUNKs, 921 generated and 114 original MakeDir invocations. The current [release preflight](release-preflight.md) implements native-static and local two-build reproducibility adapters for the known versioned Workbench MakeLink producer on all three CPUs; the refreshed SDK build retains identical command bytes and passes 34 release/build checks. Evidence is bound in [versioned candidates](makelink-versioned-development-candidates.json). The new [production DiskBuilder](../../../tools/DiskBuilder/README.md) uses real release admission and external serialization followed by independent image readback; 26 tests and actual owned-data ADF/52 MiB HDF images pass. | The two build adapters do not admit runtime, startup, reference, dependency, minimum-stack or PURE/resident gates. Eleven other required gates remain for each selected MakeLink CPU; explicit pure_admission=false requests remain rejected. Image fixtures prove serialization and raw metadata only, not command admission or system boot. The actual production CLI rejects the empty shipping manifest without creating outputs. Positive admitted-command packaging and execution remain open. Shipping remains 0/200 commands and 0/246 profiles. |
| CC09 | Classic MakeDir passes 114 supplied-vector comparisons, with 114 original/117 generated invocations. Separately, the latest isolated parser passes all 44 exact pairs; native installation/idempotence/cleanup and the frozen host's 17 gateway checks pass. Original R04, exact R05/R06 observers and one extra 25-byte observer pass. Three original/three generated MakeDir calls through original RunCommand give three comparisons: exact A01/A03 plus a seven-byte quote variant. Normal passive boot reaches valid DOS root/list/semaphores; a subsequent 32-public-call list probe returns eight entries. | Original unread-stream restoration, handler/delivered output and full boot remain open. Generated normal-input composition has historical passing 000/040 subsets and a separately corrected 020 run; none adds an original comparison. Direct InitResident failure is retained. The historical DF0/list corruption is retained. A source-qualified Signal guard passes four focused/16 related checks; its separate boot replay observes 24/24 valid DOS-root list samples and reaches InitialCLI/CON/RAM. A later read-only observer finds a startup wait in C:AddDataTypes, not a demonstrated interactive prompt. Full boot and application/stream readiness remain open; list validity and autonomous startup activity add no command comparisons. See [reference execution](reference-execution-paths.md). |
| CC10-CC38 | Existing Execute wrapper, Eval numeric-output primitive and classic MakeDir CLI body are partial; all external implementations remain unqualified for shipping. | Proceed by the row's next bounded slice. No family is complete. |
| CC39 | Freeze discrepancy preserved. | Resolve release membership using installed/runtime or authoritative release evidence. |
| CC40-CC44 | No final gate has passed. | Full option, purity, native/image, differential/boot and inventory closure remain required. |

### Existing contract work

| Contract | Applicable rows | What exists; what remains open |
| --- | --- | --- |
| [CLI](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/CLI.md) | morphos320 | Exact 3.20 member identity and the installer P-design requirement are recorded. A distinct bounded external candidate launches the existing Shell/DOS child owner only for the secondary-source zero-tail case; no template, actual launch/window behavior, native artifact, or runtime comparison has been captured. Existing NewCLI/NewShell internals are not treated as this command. |
| [Eval](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Eval.md) | wb31, morphos320 | Original 37.3 captures now include the distinct `08`/`09` prefix behavior and `010` octal control; a separate literal-template classic native root passes 37 supplied vectors per CPU (111 total) for the captured arithmetic, literals, operators and LFORMAT subset. Its symbols-off candidate has 21 distinct exact original/candidate guest matches across arithmetic, leading-zero literals, operator order, unary complement, caret-prefix handling, LFORMAT, and captured multiplication edges; all 21 distinct cases match raw output, return and caller post-System IoErr (see the Eval contract receipts). The 23 passing receipts include a duplicate `08 + 1` capture and an additional HUNK variant for `2+`. Release-source MorphOS template/behavior, independent bounded expression/numeric/LFORMAT subsets, caller-workspace and native-DOS entry cores use versioned ReadArgs templates and borrowed/TO output. The current private MorphOS HUNK passes 20 supplied post-ReadArgs vectors per CPU, including lowercase two-letter prefixes and source-derived `08`/`09` whole-token behavior; it retains only the audited `nullable-values` feature and reports 92 reachable methods with no leaks or shared-image writes. Receipt: `artifacts/cc10-eval-morphos-native-20260928-leading-zero-v1/qualification.json`. The Workbench root is still a private test artifact; only 21 bounded guest invocation cases currently match, and production release packaging, remaining behavior and PURE/resident admission remain open. Both profiles retain the SDK `nullable-values` feature on TO; real DOS parsing, full fixtures, installed policy and pure/resident approval remain open. Reuse licensing is not cleared by source availability. |
| [Execute](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Execute.md) | wb31, morphos320 | Classic template, original resident use, whitespace and equals `.DEF` defaults, explicit positional precedence, empty-default suppression in both spellings, duplicate-first-wins behavior, surplus-token first-value behavior in both spellings, bare-DEF no-op behavior, and empty-name failure behavior are recorded. The bounded wrapper uses DOS ReadItem to isolate FILE/A and retains the untouched raw tail in invocation-owned runner/frame storage before FreeArgs. `.KEY` consumption, native launch/resume/results and profile differential behavior remain open. |
| [Which](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Which.md) | wb31, morphos320 | Classic direct, internal, resident and NORES labels are recorded. Resident-plus-path ALL returns 0/0; internal-only ALL and captured default/NORES/RES no-match cases return WARN/5 with Result2 205; all eight internal-name switch combinations include the `NORES RES` WARN/5, Result2 0 rule. The bounded classic native body passes 12 supplied post-ReadArgs DOS/Exec-vector invocations per resident 68000/020/040 HUNK; the separate MorphOS six-slot candidate passes 16 supplied vectors per CPU and fails ALIAS/NOALIAS closed pending provider evidence. Workbench guest evidence includes a regular `S/Startup-Sequence` file lookup with exact path/output/return/caller-IoErr parity. Real route collisions, other object classes, exact MorphOS template, purity and package gates remain open. |
| [PathPart](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/PathPart.md) | morphos320 | Reference identity, documented template and DOS API basis recorded. The native ReadArgs/DOS-path body sizes invocation-owned output from its inputs and compiles without exception regions; it passes 15 supplied post-ReadArgs vectors per CPU, including 1,100-byte DIR/FILE outputs and 70 ADD components (>1,024-byte result), on one shared resident image. Exact template, mode combinations, real path/output semantics and failures remain open. |
| [Quote](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Quote.md) | morphos320 | Reference identity, documented grammar and release-specific READITEM change recorded. A candidate external ReadArgs boundary accepts one STR or local/global VAR source, applies NOLINE, and explicitly rejects unresolved FILE/NOQUOTES/FIRSTLINE. A separate STR-only native entry compiles as a 41-method resident 68000/020/040 HUNK with zero managed features, faults, helpers, or external targets, and passes 12 supplied post-ReadArgs DOS/Exec-vector calls per CPU, including short-write cleanup. The parser/forward stages separately pass eight direct-control generated-code invocations per CPU. Full source/options, real command I/O/errors, lifecycle, and reference behavior remain open. |
| [Type](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Type.md) | wb31, morphos320 | Exact 37.2 HUNK and 50.6 packed-member identities are bound. The classic `FROM/A/M,TO/K,OPT/K,HEX/S,NUMBER/S` string remains a syntax candidate. Hash-bound MorphOS 50.6 release source independently records `NOLINE`, `ReadArgs`/`FreeArgs`, `OPT` H/N compatibility, ignored-OPT, HEX+NUMBER, no-match, input-open, and TO-open-failure diagnostics, `TO`/`Output()` ownership, match-based traversal, HEX row layout, and its 16-row Ctrl-C polling cadence. Separate MorphOS and Workbench resident entries pass thirteen supplied parser/matcher vectors per CPU on 68000/020/040; the MorphOS entry has 25 reachable methods and the Workbench five-slot candidate has 26, with no leaks or shared-image writes and clean static compatibility. Receipts: `artifacts/type-morphos-native-20260912-qualified/qualification.json` and `artifacts/type-wb31-native-20260912-qualified/qualification.json`. Source-to-packed-binary correspondence, reuse rights, real runtime captures, PURE/resident lifecycle, packaging, and differential gates remain open. |
| [FileNote](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/FileNote.md) | wb31, morphos320 | Workbench `FILE/A,COMMENT,ALL/S,QUIET/S` remains a syntax candidate. MorphOS 50.6 source records comment truncation, recursive AnchorPath handling, `SetComment`, per-object WARN continuation and output shape. The bounded 14-method MorphOS native body and separate 15-method Workbench DOS36 syntax-candidate entry compile as resident HUNKs on 68000/020/040; each receipt runs ten supplied-DOS invocations per CPU with parser/matcher/SetComment ownership, recursive flags, output, cleanup and interleaving. Receipts: `artifacts/filenote-morphos-native-20260912-qualified/qualification.json` and `artifacts/filenote-wb31-native-20260912-qualified/qualification.json`. Exact diagnostics, soft-link/device policy, Workbench parity, packaging and P lifecycle remain open. |
| [Break](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Break.md) | wb31, morphos320 | Both profile bodies remain bounded native candidates. MorphOS 50.6 uses `PROCESS/N,PORT,ALL/S,C/S,D/S,E/S,F/S`, process/port resolution and pointer-indirect `FindTaskByPID` at `ExecBase-994`; the latest 16-method resident HUNK passes 34 supplied Exec/DOS vectors per CPU in `artifacts/break-morphos-native-20260927-pid-version-floor-v1/qualification.json`, including all 16 C/D/E/F masks, PROCESS/PORT precedence, and PID fallback at Exec 50.44, 50.45, and 51.0. Workbench uses `PROCESS/A/N,ALL/S,C/S,D/S,E/S,F/S`; its 13-method resident HUNK passes 24 vectors per CPU at `artifacts/break-wb31-native-20260927-pid-version-floor-regression-v1/qualification.json`. The 68000 HUNK hash is unchanged from the prior guest-comparison receipt, which retains the original/candidate effects for all flag combinations and missing-target cases. MorphOS original guest parity, exact PID numbering/reuse, task races, lifecycle, PURE admission and packaging remain open. |
| [ChangeTaskPri](D:/D-drive/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/ChangeTaskPri.md) | wb31, morphos320 | Both profile bodies now exist as bounded native entries. MorphOS 50.3 uses `PRI=PRIORITY/A/N,PROCESS/K/N`, current/CLI/PID resolution and the pointer-indirect `FindTaskByPID` wrapper; missing-process diagnostics prefer current `pr_CES` and fall back to DOS `Output()`. Its 13-method resident HUNK passes twelve supplied Exec/DOS invocations per CPU in `artifacts/changetaskpri-morphos-native-20260923-error-stream-v2/qualification.json`. Workbench 3.1 uses the same captured grammar with classic current/CLI resolution and no PID extension; the current 13-vector-per-CPU resident HUNK passes in `artifacts/changetaskpri-wb31-native-20260927-range-precedence-v1/qualification.json`. Captured Workbench guest pairs cover missing PRIORITY, no-PROCESS success, missing CLI, valid 0-to-42 and signed-endpoint priority effects, both custom `-129`/`128` range errors, and range-before-missing-target precedence. A fresh exact pair also covers `C:ChangeTaskPri 0 PROCESS 2147483647`, the signed-LONG upper-bound missing-target diagnostic (71 bytes), return 20 and caller IoErr 0; receipt `artifacts/workbench31-guest-command-changetaskpri-max-process-current-candidate-20260927-v1/effect-comparison.json`. The cross-profile Forbid lookup guard passes 186 vectors across MorphOS and Workbench 68000/020/040 fixtures in `artifacts/cc18-forbid-lookup-regression-20260927-v1/qualification.json`. MorphOS PID numbering/reuse, task liveness/races, full reference/differential coverage, lifecycle, PURE and packaging remain open. |
| [Search](D:/D-drive/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Search.md) | wb31, morphos320 | Hash-bound MorphOS release source records the full ReadArgs template and candidate traversal, locale, file/pattern, line-output, result, and break behavior. Workbench disk 2 Search identity and an eight-slot template candidate are recorded separately. Both profiles have bounded resident entries using public DOS parser, AnchorPath descent/exit, matcher/file I/O, DOS patterns, line output, failure, Ctrl-C, and startup paths; current receipts pass 46 MorphOS and 19 Workbench vectors per CPU, each with 43 reachable methods. MorphOS fixtures cover nested and sibling traversal, soft links, QUICK/CTRL-D, the source's 512 KiB pre-scan, allocation-halving recovery, long-line resize, incomplete-line rewind/reread, DOS 51.28+ Size64/Seek64 success and failure above 2 GiB, per-file Open/Read/Seek misses, and dangling-link warnings. Real large-file handlers, exact profile parity, complete output parity, packaging, lifecycle, and guest comparison remain open. |
| [AddBuffers](contracts/AddBuffers.md) | wb31, morphos320 | Original Workbench 37.2 execution disproved the shared candidate. An independent five-method classic body preserves change/query calls, omitted versus explicit zero, signed query results, null-header faults, parser FAIL versus handler OK, missing-DOS Result2, and ambient cleanup/output errors. Each resident target (68000/020/040) passes 27 comparisons against the original on 68000 plus one generated allocation failure; receipt `artifacts/addbuffers-wb31-reference-20260913-qualified/qualification.json`. Original 68020 execution has a retained unsupported emulator timing failure. The separate MorphOS body retains its source-based six-vector fixture. Real DOS parsing/formatting/handlers, Workbench launch, full lifecycle/PURE, packaging and complete reference comparison remain open. |
| [Avail](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Avail.md) | wb31, morphos320 | Workbench 3.1 retains its bounded 11-method resident entry and nine supplied vectors per CPU. MorphOS now has a separate 14-method DOS 37 resident body for the source-observed `CHIP/S,FAST/S,TOTAL/S,FLUSH/S,H=HUMAN/S` grammar; its receipt `artifacts/avail-morphos-native-20260920-human-v7/qualification.json` passes eleven supplied vectors per CPU, including human formatting, selector diagnostics, `FLUSH`, summary locking, parser failure and interleaving. Both profiles remain open for packed correspondence, original guest output/diagnostics, lifecycle/PURE, licensing and package admission. |
| [Status](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Status.md) | wb31, morphos320 | Hash-bound MorphOS 50.6 packed/source identity and selected Workbench disk-2 37.2 identity are recorded in the per-command audit with the shared `PROCESS/N,FULL/S,TCB/S,CLI=ALL/S,COM=COMMAND/K` candidate. The MorphOS bounded resident entry covers the legacy DOS CLI-list path with source-faithful case-insensitive COM matching, DOS 51.51 `QueryCLIDataTagList` provider/tag ABI vectors, provider-side process/command filters, multi-process traversal, TCB/FULL formatting, parser/missing-process failures, source-aligned pre-render Ctrl-C and interleaved calls; its refreshed three-CPU receipt at `artifacts/status-morphos-native-20260924-com-casefold-v1/qualification.json` passes nineteen supplied vectors per CPU. The standalone qualifier uses `--exports none` to exclude unrelated callbacks from the shared root assembly. A separate DOS36 Workbench candidate uses the same legacy path and passes sixteen supplied vectors per CPU at `artifacts/status-wb31-native-20260924-com-casefold-v1/qualification.json`. Guest parity, exact Workbench behavior, complete provider task population, original comparison, lifecycle, purity, packaging and differential evidence remain open. |
| [RequestChoice](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/RequestChoice.md) | morphos320 | MorphOS 3.20 source and SHA-bound template, ReadArgs ownership, percent escaping, gadget joining, public-screen lock, EasyRequest/BuildEasyRequest timeout flow, timer cleanup, output and failure policy are recorded. The resident body and entry pass sixteen supplied Intuition/Exec/timer vectors per CPU on 68000/020/040, including timer-port/request allocation fallback, lock/allocation failures, timer-open fallback, missing-DOS Result2 startup and two interleaved callers; receipt `artifacts/requestchoice-morphos-native-20260921-v2/qualification.json`. The replacement uses DOS36 while retaining Intuition37 pending lower-floor proof; original guest UI, Workbench correspondence, lifecycle/PURE, packaging and differential gates remain open. |
| [Version](D:/D-drive/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Version.md) | wb31, morphos320 | Source-bound MorphOS 50.30 and Workbench 40.1 identities record the two grammar candidates and system/file/resident/MD5 comparison paths. MorphOS now follows source `cmpargsparsed()` greater-than semantics and prints system/single-RES results before returning `RETURN_WARN`; its provider/MD5 candidate and source receipts remain partial. The Workbench first-Resident/FULL candidate passes ten exact original/replacement guest pairs at `artifacts/workbench31-guest-version-pairs-20260926-v2/qualification.json`. The Workbench direct FILE receipt `artifacts/version-wb31-native-20260927-file-v5/qualification.json` passes 72 supplied vectors per CPU (216 total), including direct FILE scanning, FULL output, boundary-spanning tags, provider restriction, HUNK LoadSeg Resident fallback through linked segments, and cleanup. The latest trailing-colon DOS-list receipt `artifacts/version-wb31-native-20260927-doslist-v6/qualification.json` passes 85 vectors per CPU on 68000/020/040, and two original/replacement DF0: guest cases match output, return and caller IoErr; see `contracts/Version.md`. Fresh original-only FILE captures establish normal/FULL output, warning-after-print behavior, system-mode preservation and HUNK Resident fallback. Four exact replacement pairs for normal, FULL, warning-after-output and HUNK Resident behavior now match output, return and caller IoErr; receipts are indexed in `contracts/Version.md`. Missing/no-tag and system-mode FILE parity remain open. Earlier positive/negative controls remain retained; complete profile parity, later Workbench providers, full system/error ordering, packed correspondence, PURE/resident classification, lifecycle, licensing and package admission remain open. |
| [TaskList](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/TaskList.md) | morphos320 | Source-bound AROS-derived TaskList uses the complete ReadArgs template, snapshots current/ready/waiting Exec lists under Forbid/Permit, reads public MorphOS task/system attributes, grows and retries its invocation-owned buffer, filters CLI task and command names, and formats standard rows after releasing protection. The shared provider now implements `TASKINFOTYPE_PID_CLI` (`0x24`), returning `Process.pr_TaskNum` when `Process.pr_CLI` is present and `ETask.UniqueID` for tasks with a MorphOS extension; portable tasks without that extension retain the guest-address fallback. `FindTaskByPID` accepts native unique IDs and live CLI numbers and resolves the current owner after CLI-number reuse. DOS-owned Process allocation adds and initializes an ETask when Exec library version is at least 50; version 40 preserves the classic stack layout. Other standard Exec task paths remain open. The refreshed resident receipt `artifacts/tasklist-morphos-native-pid-cli-20260927-v3/qualification.json` passes 54 DOS/Exec vectors per CPU on 68000/020/040 with 69 reachable methods, no managed allocations/helpers/external targets/exceptions/fatal sites, no leaks and no shared-image writes. Focused provider and compiled production-vector tests pass 14/14, including the native ETask-ID path; the refreshed DOS child/publication/lifecycle regression filter passes 42 cases, including ETask-ID PID lookup and the version-40 layout control. Its fixture covers live A7 for current-task VERBOSE, saved PPC backchain STACKTRACE with STACKLEVEL, SegTracker names, INTERNAL ABOX/task-exit labels, PPC REGDUMP, source-order REGCHECK classification, six NewGetSystemAttrsA boundaries, protected foreign-task reads, and a 100-task 128 KiB-to-256 KiB retry. Other standard Exec task ETask initialization, MorphOS guest ID sequence parity, concurrent target death, original guest output/provider parity, complete task population, package correspondence, PURE/resident lifecycle, licensing and package admission remain open. |
| [FindResident](contracts/FindResident.md) | wb31 | Captured Workbench `MODULE/A` syntax candidate now has a separate DOS36 capability-floor resident entry and eleven supplied DOS36/Exec vectors per CPU in `artifacts/findresident-wb31-native-20260921-v4/qualification.json`, covering found/missing lookups, parser/result-slot failures, startup, missing-DOS and interleaved calls. The original binary requests DOS37; exact diagnostics/result precedence, original guest parity, PURE/resident lifecycle, licensing and package admission remain open. |
| [Check2090](contracts/Check2090.md) | wb31 | Captured Workbench 220-byte installation helper now has a separate DOS36/expansion33 capability-floor resident entry and ten supplied Exec/Expansion/DOS vectors per CPU in `artifacts/check2090-wb31-native-20260921-v3/qualification.json`, covering A2090 controller flag states, expansion failure, startup boundaries, missing DOS and interleaved callers. The captured DOS/Expansion37 requests remain differential evidence; exact result/IoErr behavior, guest parity, PURE/resident lifecycle, licensing and package admission remain open. |
| [IconPos](contracts/IconPos.md) | wb31 | Captured Workbench installer grammar now has a separate DOS36 resident entry and fifteen supplied Exec/DOS/icon.library vectors per CPU in `artifacts/iconpos-wb31-native-20260920-v2/qualification.json`, covering DiskObject mutation, free positions, image copying, default creation, parser/type and storage failures, startup, missing DOS and interleaved callers. Exact original icon.library behavior, guest parity, PURE/resident lifecycle, licensing and package admission remain open. |
| [Protect](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Protect.md) | wb31, morphos320 | MorphOS 50.6 source identity and `FILE/A,FLAGS,ADD/S,SUB/S,ALL/S,QUIET/S` contract are recorded. The bounded MorphOS 13-method and Workbench 14-method resident entries preserve the shared active-low/high bit mapping, replacement/add/subtract forms, recursive AnchorPath flags, quiet output, mutation/no-match/break/parser failures, and interleaved calls; each three-CPU receipt passes fourteen supplied vectors per CPU. Volume/device and soft-link policy, complete wildcard traversal, original comparison, lifecycle, purity, packaging and differential evidence remain open. |
| [Touch](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Touch.md) | morphos320 | MorphOS 50.9 source identity and `NAME/A/M,VERBOSE/S,ALL/S` contract are recorded. The bounded body implements invocation-owned matcher/date storage, recursive directory flags, classic `DateStamp`/`SetFileDate`, verbose/create fallback and Ctrl-C polling. Its resident 68000/020/040 receipt passes ten supplied DOS/Exec invocations per CPU with balanced ownership and unchanged images; UTC/POSIX and soft-link branches, exact diagnostics, original comparison, lifecycle, purity, packaging and differential evidence remain open. |
| [Format](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Format.md) | morphos320 | Packed MorphOS 50.9 identity and the extracted 24,895-byte AROS-derived source are hash-bound. Source records the full `DEVICE=DRIVE/A/K,NAME/A/K,OFS/S,FFS/S,SFS/S,MSDOS/S,INTL/NOINTL,DIRCACHE/NODIRCACHE,LNFS/NOLNFS,NOICONS,QUICK,NORECYCLED,SHOWRECYCLED` grammar, banned-name checks, DosEnvec/device lookup, handler inhibition, trackdisk/TD64 formatting and verification, SFS tags, icon creation, Ctrl-C, cleanup and failure policy. The resident synthetic DOS-handler/trackdisk receipt `artifacts/format-morphos-native-entry-20260921-v4/qualification.json` passes ten supplied vectors per CPU on 68000/020/040, including full write/readback, quick handler initialization, parser/allocation/startup/missing-device/banned-name and interleaving ownership. Packed correspondence, production media/provider binding, destructive guest behavior, TD64/SFS/icon coverage, PURE, packaging and differential gates remain open. |
| [SetClock](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/SetClock.md) | wb31, morphos320 | MorphOS 50.4 source identity and `LOAD/S,SAVE/S,RESET/S` contract are recorded. The bounded resident body parses through the shared DOS lease, owns the message port/request, opens `battclock.resource` and `timer.device`, and now selects the classic or MorphOS 52+ UTC battery-clock/timer vectors from provider versions; receipt `artifacts/setclock-morphos-native-20260920-utc-v3/qualification.json` passes seventeen supplied vectors per CPU with 17 reachable methods, balanced ownership and unchanged images, including the source's non-IoErr timer failure diagnostic. Real clock-device execution, Workbench parity, exact help/diagnostics, lifecycle, purity, packaging and differential evidence remain open. |
| [Wait](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Wait.md) | wb31, morphos320 | Workbench 3.1 has a bounded resident native entry using `/N,SEC=SECS/S,MIN=MINS/UNTIL/K`, guest `DOS.Delay`, `DateStamp`/`StrToDate` `UNTIL`, VBlank timer waits and Ctrl-C cancellation; its three-CPU receipt passes fifteen supplied DOS/Exec invocations per CPU. MorphOS 3.20 now has a separate 12-method resident entry and 15 supplied DOS/Exec/timer/signal invocations per CPU, including `UNTIL` and Ctrl-C cancellation. Exact original guest behavior, purity classification, same-segment lifecycle, packaging and differential comparison remain open. |
| [WaitForLib](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/WaitForLib.md) | morphos320 | The source-documented `LIBNAME/A,I=INTERVAL/K/N,L=LOOP/K/N` contract is recorded. The bounded resident candidate uses DOS `ReadArgs`/`FreeArgs`, Exec `FindName`/`SetSignal`, DOS `Delay`, public-library-list polling, loop exhaustion, numeric-overflow and Ctrl-C paths; its receipt `artifacts/waitforlib-morphos-native-912808104128434b81af17a62421d3de/qualification.json` passes ten supplied invocations per CPU on 68000/020/040. Exact MorphOS guest timing/diagnostics, packed correspondence, provider behavior, original differential, PURE/resident lifecycle, licensing and package admission remain open. |
| [WaitForNotification](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/WaitForNotification.md) | morphos320 | The documented `NAME/A/M,QUIET/S,CONTINUE=CNT/S` contract is recorded. The bounded resident candidate uses DOS `ReadArgs`/`FreeArgs`, `StartNotify`/`EndNotify`, Exec `AllocSignal`/`FreeSignal`/`FindTask`/`Wait`, continuation on failed registrations, request cleanup and Ctrl-C; its receipt `artifacts/waitfornotification-morphos-native-0a2f332649224908b505d5f230a1dc49/qualification.json` passes twelve supplied invocations per CPU on 68000/020/040. Exact original diagnostics/output, packed correspondence, provider behavior, original differential, PURE/resident lifecycle, licensing and package admission remain open. |
| [WaitForPort](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/WaitForPort.md) | morphos320 | The source-documented `PORTNAME/A,I=INTERVAL/K/N,L=LOOP/K/N,D=DISAPPEAR/S` contract is recorded. The bounded resident candidate uses DOS `ReadArgs`/`FreeArgs`, Exec `FindPort`/`SetSignal`, DOS `Delay`, presence/disappearance checks, loop exhaustion, numeric-overflow and Ctrl-C paths; its receipt `artifacts/waitforport-morphos-native-6d77780fa9644fa2b68fd765d191f27d/qualification.json` passes ten supplied invocations per CPU on 68000/020/040. Exact MorphOS guest timing/diagnostics, packed correspondence, provider behavior, original differential, PURE/resident lifecycle, licensing and package admission remain open. |
| [Beep](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Beep.md) | morphos320 | MorphOS 50.2 source contract has no options: open `intuition.library` v33, call `DisplayBeep(NULL)`, close, and return the source result. The bounded resident native entry passes five supplied Exec/Intuition vectors per CPU on 68000/020/040 (15 total), including Workbench startup, open failure, ignored command-tail bytes and repeat use. Original guest audio behavior, PURE/resident lifecycle, package admission and differential comparison remain open. |
| [Relabel](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Relabel.md) | wb31, morphos320 | Workbench's independent five-method body passes 38 original comparisons per CPU plus three generated storage cases. The unchanged 68000 HUNK now passes eight paired original-DOS boot cases with four volume and four diagnostic readbacks per binary, real keyword/quoted/equals parsing, assigns lookup and sequential resident registration/reuse/removal. Exact successful IoErr 210 is preserved in this provider. Borrowed storage remains unchanged with explicit comparison normalization. MorphOS retains its source-bound 11-method body and eight supplied vectors per CPU. Broader parser/handler coverage, MorphOS packed correspondence, concurrent lifecycle, original classification and packaging remain open. Neither body has PURE admission. |
| [Lock](D:/D-drive/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Lock.md) | wb31, morphos320 | MorphOS 50.5 source contract records `DRIVE/A,ON/S,OFF/S,PASSKEY`, device/volume gating, `ACTION_WRITE_PROTECT` packet arguments, decimal passkey folding, result text, and cleanup. Its ten-method resident entry passes eleven supplied DOS vectors per CPU on 68000/020/040 (33 total). A separate Workbench 3.1 DOS 36 syntax candidate now passes the same eleven-vector fixture on all three CPUs; receipt `artifacts/lock-wb31-native-20260917-qualified-v2/qualification.json`. Original handler behavior, classic parity, PURE/resident lifecycle, packaging, and differential comparison remain open. |
| [DiskChange](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/DiskChange.md) | wb31, morphos320 | MorphOS 50.3 source contract records `DEVICE/A`, DOS 50 startup, `DeviceProc`, the two-stage `ACTION_INHIBIT` packet sequence, diagnostics, and parser cleanup. Its nine-method resident entry passes ten supplied DOS vectors per CPU on 68000/020/040 (30 total), including success, ignored second result, lookup/inhibit/parser failures, invalid startup boundaries, Workbench rejection and interleaved repeat calls. A separate Workbench 3.1 DOS 36 syntax candidate now passes the same ten-vector fixture on all three CPUs; receipt `artifacts/diskchange-wb31-native-20260917-qualified-v1/qualification.json`. Original handler transitions, classic parity, PURE/resident lifecycle, packaging, and differential comparison remain open. |
| [Reboot](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Reboot.md) | wb31, morphos320 | MorphOS 50.3 source contract and packed ISO identity are hash-bound in `reference-captures/reboot-morphos-binary-audit-20260923.json`; the audit records the empty `ReadArgs` template, ignored parser-failure transition, Ctrl-C `ERROR_BREAK` path, `ColdReboot` call, literal returning value `666`, and DOS-open failure behavior. The refreshed MorphOS receipt `artifacts/reboot-morphos-native-20260923-parity-v2/qualification.json` and Workbench receipt `artifacts/reboot-wb31-native-20260923-parity-v2/qualification.json` each pass ten supplied DOS/Exec vectors per CPU on 68000/020/040, including parser/signal/open failures, Workbench and boundary rejection, and interleaved repeat calls. Real reboot transition, classic parity, PURE/resident lifecycle, packaging, and differential comparison remain open. |
| [ResList](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/ResList.md) | morphos320 | MorphOS 50.4 source contract records the no-option DOS 37 startup, `Forbid`-protected Exec resource-list snapshot, growable public buffer, header/row formatting, per-row Ctrl-C polling, allocation-failure and break results, and ordered cleanup. The five-method resident native entry passes eleven supplied DOS/Exec vectors per CPU on 68000/020/040 (33 total), including empty, one-resource and three-resource lists, allocation/Ctrl-C/missing-DOS paths, Workbench/boundary rejection, and interleaved repeat calls. Receipt: `artifacts/reslist-morphos-native-20260919-multinode-v2/qualification.json`. Complete resource-list population, original guest parity, PURE/resident lifecycle, packaging, and differential comparison remain open. |
| [LibList](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/LibList.md) | morphos320 | MorphOS 50.6 media identity and the AROS-derived no-option library-list contract are recorded: DOS 37 startup, `Forbid`-protected snapshot, address/name/version/revision/open-count/flags fields, growable buffer, formatting, Ctrl-C and allocation diagnostics. The five-method resident native entry passes eleven supplied DOS/Exec vectors per CPU on 68000/020/040 (33 total), including empty, one-library and three-library lists, allocation/Ctrl-C/missing-DOS paths, Workbench/boundary rejection, and interleaved repeat calls. Receipt: `artifacts/liblist-morphos-native-20260919-multinode-v2/qualification.json`. Complete population, packed-binary/source correspondence, original guest parity, PURE/resident lifecycle, packaging, and differential comparison remain open. |
| [DevList](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/DevList.md) | morphos320 | MorphOS 50.3 media identity and the AROS-derived no-option device-list contract are recorded: DOS 37 startup, `Forbid`-protected snapshot, address/name/version/revision/open-count/flags fields, growable buffer, formatting, Ctrl-C and allocation diagnostics. The five-method resident native entry passes eleven supplied DOS/Exec vectors per CPU on 68000/020/040 (33 total), including empty, one-device and three-device lists, allocation/Ctrl-C/missing-DOS paths, Workbench/boundary rejection, and interleaved repeat calls. Complete population, packed-binary/source correspondence, original guest parity, PURE/resident lifecycle, packaging, and differential comparison remain open. |
| [PortList](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/PortList.md) | morphos320 | MorphOS 50.2 media identity and the AROS-derived no-option message-port-list contract are recorded: DOS 37 startup, `Forbid`-protected snapshot, copied port/task names, growable buffer, source-equivalent formatting, Ctrl-C and allocation fault handling. The five-method resident native entry passes eleven supplied DOS/Exec vectors per CPU on 68000/020/040 (33 total), including empty/one-port/three-port lists, allocation and break paths, missing DOS, Workbench/boundary rejection, and interleaved repeat calls. Complete population, packed-binary/source correspondence, original guest parity, PURE/resident lifecycle, packaging, and differential comparison remain open. |
| [ModList](D:/D-drive/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/ModList.md) | morphos320 | MorphOS 50.4 media identity and the AROS-derived `VERBOSE/S` resident-module-list contract are recorded: DOS 37 startup, `ReadArgs`, resident indirection, revision parsing, growable buffer, formatting, flags, Ctrl-C and allocation/parser diagnostics. The six-method resident native entry passes thirteen supplied DOS/Exec vectors per CPU on 68000/020/040 (39 total), including empty, one-resident and three-resident lists, ID-string revision parsing, allocation/parser/Ctrl-C/missing-DOS paths, Workbench/boundary rejection, and interleaved repeat calls. Receipt: `artifacts/modlist-morphos-native-20260919-multinode-v2/qualification.json`. Complete population, packed-binary/source correspondence, original guest parity, PURE/resident lifecycle, packaging, and differential comparison remain open. |
| [Assign](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Assign.md) | wb31, morphos320 | Hash-bound classic and MorphOS member identities, documented profile grammars, and required-pure/resident design evidence are recorded. Bounded Workbench and MorphOS public-vector mutation candidates now compile as clean resident MC68000/020/040 HUNK images and pass nineteen supplied vectors per CPU; receipts `artifacts/assign-wb31-native-20260917-runtime-v4/qualification.json` and `artifacts/assign-morphos320-native-20260917-runtime-v3/qualification.json`. The original Workbench parser probe agrees on ReadArgs error/cleanup shape for two malformed inputs, but exposes an unresolved post-run IoErr delta. LIST, EXISTS, DISMOUNT and VOLS/DIRS/DEVICES remain fail-closed pending source or guest captures; runtime, installed metadata, lifecycle, packaging and profile parity remain open. |
| [Mount](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Mount.md) | morphos320 | Packed MorphOS 50.13 identity and the extracted 56,885-byte AROS-derived source are hash-bound. Source records the outer `DEVICE/M,FROM/K,DEBUG/S` grammar, device/mount-list search order, inner mount-file options, `ReadArgs` ownership, `DosEnvec`/`DeviceNode` construction, handler loading, `AddDosNode`, validation and failure categories. The resident outer-entry boundary now has a 15-invocation, three-CPU receipt in `artifacts/mount-morphos-native-entry-20260921-v2/`; real source/provider behavior, packed correspondence, exact help/output/IoErr, Workbench behavior, expansion/filesystem providers, PURE, packaging and differential gates remain open. |
| [MakeLink](contracts/MakeLink.md) | wb31, morphos320 | Separate bodies and three-CPU execution exist. Retained-HUNK original-DOS evidence covers options, help/EOF, exact diagnostics/errors, readback and bounded concurrency; that 68000 HUNK also passes five native CopperStart DOS invocations with fixture Exec/handler services after shared formatter/error fixes. Separate versioned images reproduce locally and pass 60 native vectors; the versioned 68000 image additionally has five fresh original-DOS cases and exact original Version readback. Evidence stays bound to each tested hash. See [contract](contracts/MakeLink.md), [qualification checkpoint](qualification-report.md) and [versioned records](makelink-versioned-development-candidates.json). Full options/startup, CopperStart system/handler integration, required purity, minimum stack and packaging remain open; MorphOS also needs soft-link success and original correspondence. |
| [Delete](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Delete.md) | wb31, morphos320 | Hash-bound Workbench and MorphOS source records the classic/follow-links grammars, matcher/link/protection ordering, retry and cleanup requirements, partial-failure policy, break behavior, and required-pure evidence. The five-method native resident final-object `DeleteFile` stage passes six supplied direct vectors per CPU on 68000/020/040. The 41-method MorphOS frontend and separate 43-method Workbench DOS36 four-slot frontend compile as resident 68000/020/040 HUNKs with zero runtime features/helpers, external targets, exception regions and fatal fault sites; each runner passes eight supplied-DOS invocations per CPU with no leaks or shared-image writes. Receipts: `artifacts/delete-command-native-20260912-qualified/qualification.json` and `artifacts/delete-wb31-native-20260912-qualified/qualification.json`. Parent-protection retry, exact diagnostics, recursive/link behavior, real handler behavior, packed correspondence, installed lifecycle/PURE admission, packaging, and reference comparison remain open. |
| [Join](D:/D-drive/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Join.md) | wb31, morphos320 | Hash-bound Workbench and MorphOS source records ReadArgs, ordered pattern/direct input handling, destination cleanup, 262144-byte DOS copy loop, exact writes, break and resource ownership, and required-pure evidence. The append-stream stage passes eight supplied direct vectors per CPU. Both frontends now pass eighteen supplied public-DOS runtime invocations per CPU on resident 68000/020/040 HUNKs, with balanced matcher/stream/buffer/parser ownership and zero shared-image writes; receipts `artifacts/join-wb31-native-20260918-runtime-v3/qualification.json` and `artifacts/join-morphos320-native-20260918-runtime-v2/qualification.json`. Exact guest parser/diagnostics, wildcard/no-match and real-handler behavior, packed correspondence, original PURE/resident lifecycle, packaging and differential parity remain open. |
| [Copy](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Copy.md) | wb31, morphos320 | Hash-bound MorphOS source records the extended grammar, six working modes, DIRECT path, matcher and exact DOS I/O lifecycle, loop/destination handling, metadata behavior, and required-pure evidence. A 14-method resident ReadArgs/mode gate passes thirteen supplied DOS vectors per CPU on 68000/020/040, including positional target adjustment, parser ownership and invalid combinations; its two-method selector separately passes fourteen direct controls. A seven-method public-DOS regular-file pair and six-method streaming core pass nine and eight supplied vectors per CPU for open/transfer/partial-destination cleanup and EOF/transfer/Ctrl-C behavior. A five-method `TestDest`, six-method parent-prefix, five-method non-filesystem destination, eight-method joined destination opener, six-method loop guard, six-method protection/comment/classic-or-POSIX-date, five-method final result policy, five-method `IsMatchPattern`, and six-method flat matcher stage pass eleven, seven, eight, five, six, eight, nine, five, and six supplied vectors respectively. None leaks or writes the shared image where runtime-qualified. Directory recursion, full matcher traversal, real handler behavior, packed correspondence, packaging, and reference comparison remain open. |
| [Clone](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Clone.md) | morphos320 | Hash-bound packed-member identity and required-pure installer evidence are recorded. The inspected MorphOS C archive has no Clone member, so grammar, semantics, handler/runtime behavior, packaging, and reference comparisons remain open; Copy behavior is not inferred. |
| [Trashcan](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Trashcan.md) | morphos320 | Hash-bound packed-member identity and its desktop/trash-owner dependency are recorded. It is absent from the installer P-addition list, which leaves purity unresolved. The inspected MorphOS C archive has no Trashcan member, so grammar, semantics, handler/runtime behavior, packaging, and reference comparisons remain open; Delete behavior is not inferred. |
| [Exe2Arc](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/Exe2Arc.md) | morphos320 | Published-source template, seven layouts and finite fixtures recorded. Corrected RAR4/CAB/ACE headers, scanners and separate copy pass the existing host checkpoint plus ACE cases; the refreshed bounded DOS-vector native closure passes 84 entries per CPU (252 total). Original binary correspondence, other layouts, safety discrepancies, frontend and command-level gates remain open. |
| [MakeDir](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/contracts/MakeDir.md) | wb31, morphos320 | Classic NAME/M, control flow, output/error and ownership have 114 supplied-vector comparisons plus generated-only slot-allocation failures. Three further comparisons use original RunCommand/NULL-D3 ReadArgs, covering exact A01/A03 and a quote variant. MorphOS 50.4 now has an independently source-informed NAME/M,ALL/S resident entry with fifteen supplied public-DOS vectors per CPU, including `ALL` path walking, CurrentDir restoration, parser/allocation failures and interleaving; receipt `artifacts/makedir-morphos-native-20260917-runtime-v8/qualification.json`. Packed correspondence, exact guest behavior, installed lifecycle/PURE, packaging and full profile gates remain open. |
| [Rename](contracts/Rename.md) | wb31, morphos320 | Workbench body, three-CPU native execution and bounded original-DOS effects/concurrency now exist; see [remaining-work audit](rename-remaining-work.md). Full contract and shipping gates remain open. MorphOS50.8 now has an independent body, three-CPU native branch/boundary execution and bounded original-Kickstart-DOS effects; original MorphOS parity and full admission remain open. |

MakeDir Workbench now also has a DOS36 resident entry receipt at
`artifacts/makedir-wb31-native-20260917-runtime-v2/qualification.json`, with
fourteen supplied public-DOS vectors per CPU. This is bounded ABI evidence;
original guest, lifecycle, purity, packaging and differential gates remain open.
The MorphOS receipt was re-run against the same current source and runner in
`artifacts/makedir-morphos-native-20260917-runtime-v10/qualification.json`;
the three HUNK hashes and fifteen-vector counts remain unchanged.

There are **69 partial Spec rows, 67 partial Options rows, 56 partial Semantics
rows and 60 partial Native rows**; the remaining rows in those columns are
open. The Purity column has 97 `required/open` and 149 `audit/open` rows. The
Package column has four partial rows and 242 open rows; Differential has 96
`required/open` and 150 `audit/open` rows. No gate is passed in full.

Ledger correction, 2026-08-30: classic Rename's Semantics cell was marked
partial by mistake. RN1 executes only the original command; there is no
replacement Rename body. The cell is restored to open under the gate definition
above, while its partial Spec/Options and original reference evidence remain.

## Dependency IDs

`D-CORE` applies to every row: matching CC00 evidence; CC01 complete contract;
CC02 consumed API/owner audit; CC03 native entry/execution; applicable CC04 shared
parsing/output/ownership; CC06/CC41 original purity classification and required
qualification; CC08/CC42 artifacts/image metadata; CC09/CC43 differential and
runtime evidence; CC40 option/behavior closure; CC44 final membership audit.

**`CC03.COMPILER-VOLATILE` is resolved for the observed defect**: the Amiga
call convention declares its volatile registers and both compiler effect models
honor them. The private startup probe passes 31 runs per 68000/020/040 target.
Original OS launch, remaining argument boundaries and parser comparison are the
next foundation slices. The original image entry may modify all registers except SP; do not confuse that
allowance with values that must survive a compiler-generated call boundary.
Evidence and exact artifacts must be recorded before removing this prerequisite.

The family IDs below refer to the unchanged plan. They are candidate dependency
sets, not blanket barriers. Command/mode-specific provider IDs, source locations,
license decisions and fixtures still need CC01/CC02/CC07 resolution. Missing
networking, languages, archives, media, hardware or debugging providers are
required implementation work and cannot become permanent exclusions.

| ID | Owner/scope | Plan dependency baseline; applicability audit still open |
| --- | --- | --- |
| `D-CC10` | Execution, lookup, arithmetic, and quoting (CC10) | CC03-CC04, CC06-CC09; existing Shell execution/lookup owner. |
| `D-CC11` | File and directory inspection (CC11) | CC05, CC09 and versioned DOS enumeration/metadata APIs. |
| `D-CC12` | Directory, assignment, and namespace changes (CC12) | CC05; DOS assigns, namespace and handler support. |
| `D-CC13` | Copying, joining, and removal (CC13) | CC05, metadata support, and the real trash/desktop owner. |
| `D-CC14` | Metadata, sizing, and file writes (CC14) | CC05 and versioned metadata/size/date capabilities. |
| `D-CC15` | Text transforms and comparisons (CC15) | CC05; required encoding, comparison and metadata providers. |
| `D-CC16` | Time and wait commands (CC16) | CC04, guest timers/signals/notifications and process launch. |
| `D-CC17` | Memory, version, and task inspection (CC17) | Exec/DOS inspection APIs and appropriate CPU/config providers. |
| `D-CC18` | Process control and system registries (CC18) | process/signal and system-registry owners. |
| `D-CC19` | Handler and volume lifecycle (CC19) | DOS handler startup/packets, removable/RAD/cache services. |
| `D-CC20` | Boot services and classic preferences (CC20) | applicable expansion/resource/ROM, datatypes, preferences, graphics, keymap, diskfont and notification owners. |
| `D-CC21` | Clock, power, and boot media (CC21) | timer/battery-clock, filesystem/device, bootblock and power providers. Check each profile's actual distribution placement. |
| `D-CC22` | Workbench installation helpers (CC22) | original installer contracts and the relevant device, ROM, resident-list, boot-device, icon and preference-conversion APIs. |
| `D-CC23` | Desktop launch and URL handling (CC23) | existing Shell, Workbench/Ambient-equivalent launch owner, icon, Intuition, URL and MagicBeacon notification services. |
| `D-CC24` | Requesters and clipboard (CC24) | ASL/Intuition/MUI as actually required, clipboard/console owner. |
| `D-CC25` | Editors, installer, and spelling service (CC25) | console/editor facilities, installer language/GUI, and spellchecker.library-compatible IPC/service support. |
| `D-CC26` | ARexx commands and host (CC26) | real compatible interpreter, rexxsyslib, ports and messaging. |
| `D-CC27` | Lua and MUI automation (CC27) | the versioned Lua execution/extension contract and the current MUI replacement's application-inspection/automation interfaces. |
| `D-CC28` | Network setup and identity (CC28) | guest network stack/interface services and actual identity and credential stores required by the reference. |
| `D-CC29` | Network diagnostics, name service, and HTTP (CC29) | guest sockets/protocol stack, resolver, clock and TLS provider. |
| `D-CC30` | Remote filesystems and capture (CC30) | protocol/authentication libraries, DOS handlers and packet capture provider. Split large missing handlers into explicit prerequisite slices. |
| `D-CC31` | Compression and archives (CC31) | Versioned codecs/archive/hash services with source/license admission; concrete per-command IDs and executable slices are in the [archive work package](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/archive-provider-work-package.md). |
| `D-CC32` | XAD archive utilities (CC32) | Real XAD library/client APIs, archive clients and DOS/device integration per the archive work package; share CC31 codecs where contracts match. Exe2Arc needs DOS/Exec, not XAD. |
| `D-CC33` | USB stack and device tools (CC33) | Poseidon-compatible stack/configuration plus applicable device-class protocols and drivers. |
| `D-CC34` | Audio, MIDI, and mixer (CC34) | audio mode registry, AHI/multimedia/MIDI and mixer providers actually used by each reference command. |
| `D-CC35` | Hardware inspection and power management (CC35) | guest PCI, graphics, battery/power/fan, storage and HFS boot metadata providers. Match hardware-specific interfaces to their real owners. |
| `D-CC36` | Raw media and diagnostic tests (CC36) | safe guest block-device, memory-allocation and filesystem test facilities; CC31 compressed-input support where required. |
| `D-CC37` | PFS filesystem utilities (CC37) | a version-compatible PFS handler and maintenance interfaces; license and media-format evidence must be recorded before implementation reuse. |
| `D-CC38` | Debugging, tracing, and translation services (CC38) | each command's actual debugger, trace, debug-log, segment-tracker or translation owner as established by CC01/CC07. |

## Next-slice keys

Every next key is scoped to the command/profile in its row. Independent contract
research may continue alongside native qualification. Once a next
slice is completed, replace its key with a specific dated follow-up and evidence;
do not advance the whole family.

| Key | Next bounded slice |
| --- | --- |
| C1 | CC01: bind the selected media binary to its exact ReadArgs template or non-DOS grammar and enumerate all options/modes from matching primary evidence. Record unknowns and a safe reference-capture fixture; do not infer contracts from template-looking strings or pass unverified help arguments to a mutating command. |
| AB-WB31 | CC12 AddBuffers: 27 original-68000 versus generated-target comparisons pass. Next execute real DOS ReadArgs/formatter/handler paths, capture CLI/Workbench launch and resident lifecycle, and resolve the retained original-68020 timing failure. Do not reuse MorphOS change/query or return-level semantics for this profile. |
| RELABEL-WB31 | CC12 Relabel: 38 supplied-vector comparisons per CPU plus three generated storage cases; [paired original-DOS boot](relabel-original-dos-boot.md) passes the eight-case baseline and eleven additional argument/help cases on the unchanged 68000 HUNK, with real readbacks and sequential resident lifecycle. Protected DF0 cancellation now passes on both binaries (2026-09-14): production host-keyboard input reaches the guest handler, original EasyRequestArgs returns zero, and both commands return 20/214 with exact fault output and balanced cleanup. The input-service forwarding/reinstallation defect has a failing-before/passing-after regression; eighteen device tests pass. Twenty distinct paired single-caller scenarios are covered. A further two-task/one-segment boot now proves overlapping protected/RAM calls, separate live RDArgs and replacement storage, owner cleanup and recovery, with twelve corruption controls (relabel-concurrent-20260914/qualified-v2). Ordinary active Resident REMOVE refusal now also passes on both binaries (2026-09-15), preserving the node/segment for overlap and recovery before final removal; twenty controls reject (relabel-active-remove-20260915-v2/qualified). Active REPLACE/PURE refusal also passes (2026-09-16): original Resident returns 10/202 before any second load or registry mutation, keeps the same active node, and later removal succeeds; 21 controls reject (relabel-active-replace-20260915/qualified). Successful idle replacement now also passes with two separately tracked image lifetimes, the same registry node and new-image reuse; 24 controls reject (relabel-idle-replace-20260916/qualified-v2). Missing/invalid replacement source retention now passes in two paired boots, with 56 controls (relabel-failed-replace-20260916); both preserve the same segment for later calls and original Resident reports 5/205, including after invalid LoadSeg/212. Normal background RemTask(NULL), unlink/switch and later parent reuse now pass in fresh boots, with 20 retirement plus 21 concurrency/refusal controls (relabel-caller-retirement-20260916/qualified). All four recorded caller tc_MemEntry spans (3520 rounded bytes, including Process/stack) are now proven freed immediately after each FreeMem, before reuse; 17 memory plus 20/21 lifecycle controls reject (relabel-caller-memory-20260916-v2/qualified). This is original ROM RemTask with host FreeMem, not host RemTask native-handoff qualification. Two writable DOS1 device/volume-name renames now pass with persisted-sector readback and 12 controls (relabel-persistent-20260916-v6/qualified): 0/0, unchanged payload/protection/bitmap, only root label/date/checksum changes. Explicit guest Mount and ENV: setup are required; auto-discovery is not qualified. Fresh ROM guests now also resolve the saved label and read both payloads from each exact write-protected export, with no Relabel execution or disk mutation; 12 controls reject (relabel-cold-remount-20260916/qualified). Four FFS name boundaries now pass with intermediate persisted-label proof and recovery: thirty-byte success; thirty-one-byte/empty/slash rejection at 20/210 with exact fault text; 16 controls reject (relabel-name-boundaries-20260916-v2). Next cover OFS/other handler/name-boundary paths, the broader process-resource ledger, applicable termination/loader failures, remaining handler/parser variants, Workbench launch, original classification and packaging. Keep parser-buffer/overflow normalization explicit. |
| ML1 | CC12 MakeLink: separate Workbench/MorphOS bodies; [contract](contracts/MakeLink.md) and [retained candidates](makelink-development-candidates.json) bind Workbench 60 and MorphOS 54 native cases plus bounded reference/runtime evidence. Old fd42d840 68000 adds five native CopperStart DOS invocations, public payload reads and shared formatter/error fixes; Exec/backing handler are fixtures. Three [versioned records](makelink-versioned-development-candidates.json) bind local two-build equality and 60 native invocations; only new 12d1f365 68000 adds five fresh original-DOS output/error cases and exact original Version readback. Help/EOF, stack and concurrency evidence from old hashes is not transferred. Remaining grammar/startup/handlers, complete CopperStart integration, original classification, full residency/minimum stack and versioned-image release admission remain open. No shipping or complete-profile admission. |
| CP8 | CC13 Copy 3.20: 333 supplied-vector native invocations and 12 original/generated parser pairs pass. Real original Shell/DOS DIRECT transfers verify exact bytes/size, parser/library ownership and balanced allocations. Forced PURE same-segment reuse passes sequential parser/source/target failure recovery; distinct-task overlapping success/success and success/target-failure pass. Two concurrent destinations independently read back all 24596 bytes through original Type, then resident removal frees the matching segment. Evidence: artifacts/copy-boot-fixture/verified-concurrent-readback.json, verified-concurrent-failure.json and verified-resident-recovery.json; tools/Commands/verify_copy_concurrent_transfer.py. Still open: normal traversal/options real-handler coverage, wider resident stream/directory/signal/stack/failure matrix, installed flags/admission, generated-DOS integration, original MorphOS parity and packaging. No full command gate closes. |
| E1-WB | CC01/CC10 Eval: six original 37.3 captures establish selected left-to-right arithmetic/bitwise/shift forms, evaluated-prefix acceptance, parentheses, unary minus/complement, `0x`/`#x` hex, leading-zero octal, the distinct `08`/`09` prefix behavior, and low-digit X/x/O formatting plus `%n`. Double-star/caret grammar, numeric width/overflow, width ranges, all other conversions and errors remain capture requirements before extending the explicitly bounded classic candidate. |
| E2-MOS | CC10.Eval.2c: the independently written numeric-output primitive passes 540 source-bound native invocations with zero fatal sites after fixed-base arithmetic specialization. The source-derived lowercase `mo`/`xo`/`eq`/`ls`/`rs` prefixes and MorphOS `08`/`09` token behavior pass portable tests and 20 resident supplied vectors per CPU; latest receipt `artifacts/cc10-eval-morphos-native-20260928-leading-zero-v1/qualification.json`. Other lexer edges, overflow, character-format and original-binary comparisons remain open. The formatter alone does not implement Eval. |
| E1-MOS | CC10 Eval: after CC03 repair/retest, implement the first behavior slice from the documented 3.20 source observations with independently written code; resolve reuse license before any copying/translation, and retain the source-to-shipped-binary reference gap. |
| X2 | CC10 Execute: original `.KEY` positional, whitespace/equals `.DEF` defaults, explicit positional precedence, empty-default suppression in both spellings, duplicate-first-wins behavior, surplus-token first-value behavior in both spellings, bare-DEF no-op behavior, empty-name failure behavior, and one undeclared-name acceptance/ignore case are captured. The resident default table now keeps the first duplicate, ignores undeclared names, ignores surplus value tokens after the first, and treats a bare directive as a no-op; its bounded runner stops malformed directives. Capture other named malformed directives and diagnostics before pending/nested lifecycle and native results through the bounded script-frame owner. |
| W1-WB | CC01 Which: classic switch combinations match on found non-internal `Execute` and a missing name; a separate guest pair matches the regular `S/Startup-Sequence` file path, output, return and caller IoErr. Bare-name `ALL` repeats the C: path and `NORES RES` returns 5 with caller IoErr 0. Extend captures to other lookup categories, current-directory/assign routes, exact diagnostics, break and failure behavior. |
| W1-MOS | CC01 Which: confirm the shipped template and alias/resident/path precedence, including NOALIAS/ALIAS interactions, using the 3.20 binary. |
| P1 | CC01 PathPart: confirm the shipped template and DIR/FILE/ADD combinations, root/colon/slash behavior, byte output and failures in controlled captures. |
| Q1 | CC01 Quote: confirm shipped grammar and rule inventory, including 3.20 READITEM equals quoting, STR versus FILE/VAR precedence, and output/error bytes. |
| TY1-WB | CC01/CC11 Type 37.2: capture the exact classic grammar beginning with non-mutating `?`/EOF and parse failures. Validate `FROM/A/M,TO/K,OPT/K,HEX/S,NUMBER/S` only against the 37.2 HUNK. Then record file/pattern traversal, output bytes, TO ownership, option/default interactions, result/IoErr, break, and lifetime before admitting a body. |
| TY1-MOS | CC01/CC11 Type 50.6: prove or reject correspondence of the hash-bound partial source's `FROM/A/M,TO/K,OPT/K,HEX/S,NUMBER/S,NOLINE/S`, OPT H/N, HEX+NUMBER, and `APEF_LiteralSLinks` behavior to the packed member. The base SDK already exposes the required public DOS calls, but literal-soft-link behavior needs a versioned MorphOS capability rather than an invented AnchorPath extension. Then capture outputs, TO/error ownership, patterns/links, break, P lifecycle, and same-SegList behavior. Source observation is not binary parity or copy permission. |
| XA2 | CC32.Exe2Arc.2c now includes source-order scanner dispatch, terminal result precedence, close-then-delete policy, ZIP-only Ctrl-C polling with the source's nonzero-count/`ERROR_BREAK` success boundary, and a bounded MorphOS frontend composing ReadArgs, input/FIB/workspace, naming/output, ZIP or copy-to-EOF extraction and cleanup. The frontend emits bounded saved-byte, no-match/type-list and output-open messages and never executes the input. The managed suite passes 704 tests and the native project builds cleanly. Exact redirected diagnostics, post-cleanup IoErr ordering, resident qualification, packed-original comparisons and package admission remain open; no full command claim. |
| MK3-WB | CC02.API14.4-.5 / CC12.MakeDir.4a: original exact A01/A03 launches pass, with the seven-byte quote variant separate. Portable input, native never-started rollback and host ordinary-return ownership pass bounded suites. Native input now has historical 000/040 passes and a separate corrected 020 pass. Continue current composed qualification and the typed RET03 callback/stack handoff after partial RET02. Resolve normal-boot scheduler/application readiness before original R07 and real filesystem/delivered-output comparisons. WB/purity/package gates remain open. See [launch contract](native-command-launch-contract.md). |
| RN2-WB | RN1-WB.CPU020 now completes the original-only supplied-vector subset on 68000/020/040, each 40 returned cases and eight separate hazard stops, with the historical failure preserved. Next execute actual DOS MatchEnd for never-started, already-ended and live searches and choose a safe cleanup/buffer policy. Original parser/matcher/filesystem and generated body remain open. |
| RN3-WB | Independent Workbench Rename body and native entry compile on 68000/020/040. The 68000 original-DOS direct/directory fixture passes with exact destination bytes and tracked cleanup: artifacts/copy-boot-fixture/verified-rename-native.json. Three negative controls reject destination/FIB/image corruption. Original cleanup and buffer hazards have an explicit documented policy. Parser/duplicate failure and recovery also pass (verified-rename-native-errors.json), with explicit MatchEnd normalization. Nondirectory wildcard/multiple-source rejection and recovery also pass; see [development candidates](rename-development-candidates.json) for hash-bound evidence. Three-CPU native execution and original-DOS concurrent resident use now pass in bounded fixtures; the E02/E04/R03 policy matrix is additionally bound by `artifacts/rename-error-policy-20260917/qualification.json`; the licensed original-DOS `RunCommand` harness now passes four bounded Workbench Rename parser-edge invocations and two original/generated comparisons, recorded in `artifacts/rename-parser-edges-20260917/qualification.json`. See [remaining-work audit](rename-remaining-work.md). Full profile, real-system stack, purity and packaging remain open. This supersedes the earlier absent-body status without retroactively changing original-only evidence. |
| RN3-MO | Independent MorphOS50.8 body and DOS37 entry now compile and execute on68000/020/040: artifacts/morphos-rename-body-qualified/qualification.json,112 invocations per CPU. Exact-profile return/quiet/order/error and2048-byte boundary cases run sequentially and interleaved; the E02/E04/R03 policy matrix is additionally bound by `artifacts/rename-error-policy-20260917/qualification.json`. The68000 original-Kickstart-DOS ordinary/wildcard/AS fixture has four verified mutations and exact post-removal readbacks (verified-morphos-rename-native.json). See [source contract checkpoint](rename-morphos50-source-contract.md) and [development candidates](rename-development-candidates.json). Original MorphOS runtime correspondence, complete options/output/IoErr differential, CopperStart integration, real stack/P/lifecycle and packaging remain open. |
| RET03.2A | The [owner revalidation](runcommand-retirement-range-owner-revalidation-qualification.md) passes 152 source-bound host cases after a five-failure stale-binding baseline. It adds no release capability. Next census surviving callback, I/O and scheduler roots before any retirement cleanup design. |
| DW01 | [Bounded raw Write/FPutC failure propagation](direct-write-error-qualification.md) passes 45 native cases, 349 portable checks and 64 host controls. Next capture original packet-result behavior and resolve provider capacity/metadata rollback before any command credit. |
| U1 | [Utility NamedObject ABI correction](utility-namedobject-layout-correction.md) exposes `ANO_UserSpace` through original public `no_Object`, passes five focused tests, and has a one-pass bounded original replay. The old vector-3/odd-`InitSemaphore` boundary is absent; the first next exception is vector 8 at `$00F80BBE`/opcode `$007C`. Next attribute that vector-8 state before any continuation work. |

The explicit-source ReadArgs repair and its strict native comparison pass.
The immediate coordinator sequence is CopperStart's invocation-owned normal
input, callback rollback, nested host dispatch and safe completion during
reset/retirement. In parallel, resolve original handler readiness, implement
the bounded archive I/O slice and requalify existing private artifacts from
the compiler/SDK checkpoint in isolated source snapshots.
Fixed-base numeric code, raw I/O and MakeDir's
supplied-vector checkpoint now pass for their captured source identities.
Native runs using synthetic library gateways test the
emitted code and ownership boundary; they do not stand in for original Kickstart
ReadArgs parsing or licensed Workbench/MorphOS differential execution.

## Per-command/profile gates

All rows inherit D-CORE. `required/open` and `audit/open` are both open
qualification states. Separate table rows preserve version differences even
where implementation is eventually shared.

| Stable ID | Owner | Spec | Options | Semantics | Native | Purity | Package | Differential | Dependencies | Next |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `CC10.CLI.morphos320` | CC10 | partial | open | open | open | required/open | open | open | D-CC10 | C1 |
| `CC10.Eval.wb31` | CC10 | partial | partial | partial | partial | audit/open | open | open | D-CC10 | Current three-CPU resident fixtures cover 37 supplied vectors per CPU; 21 distinct guest invocation cases match exact output, return and caller IoErr, including fresh `08`, `08+1`, and `09+1` pairs on the symbol-free loadable HUNK; 23 passing receipts include a duplicate `08 + 1` capture and a `2+` comparison on both HUNK identities. Other cases, full grammar, package admission, purity and release gates remain open |
| `CC10.Eval.morphos320` | CC10 | partial | partial | partial | partial | audit/open | open | open | D-CC10 | Current-source three-CPU resident receipt `artifacts/cc10-eval-morphos-native-20260928-leading-zero-v1/qualification.json`; source-to-binary parity, actual ReadArgs, full grammar, purity, package and differential gates remain open |
| `CC10.Execute.wb31` | CC10 | partial | partial | partial | open | required/open | open | open | D-CC10 | X2 |
| `CC10.Execute.morphos320` | CC10 | partial | partial | partial | open | required/open | open | open | D-CC10 | X2 |
| `CC10.PathPart.morphos320` | CC10 | partial | partial | partial | partial | required/open | open | open | D-CC10 | Latest three-CPU resident receipt `artifacts/cc10-pathpart-native-20260928-dynamic-output-v5/qualification.json` passes 15 vectors per CPU, including dynamic outputs and allocation failure; exact template/mode semantics, guest path parity, PURE, package and differential gates remain open |
| `CC10.Quote.morphos320` | CC10 | partial | partial | partial | partial | required/open | open | open | D-CC10 | Current-source three-CPU resident receipt `artifacts/cc10-quote-native-20260922-v1/qualification.json`; full template, guest semantics, PURE, package and differential gates remain open |
| `CC10.Which.wb31` | CC10 | partial | partial | partial | partial | audit/open | open | partial | D-CC10 | Latest Workbench-source build, after the cross-profile MorphOS ALIAS/NOALIAS edit, passes 31 supplied vectors/CPU across resident 68000/020/040; receipt `artifacts/which-wb31-native-20260927-findvar-regression-v1/qualification.json` (68000 HUNK SHA-256 `cb7a3a3aa26e1d12381da5280f7281265ad5b0e5b52648f2f8d94c8918ce7294`). One fresh guest comparison on this HUNK matches `C:Which SYS:S/..` exactly (empty output, return 5, caller IoErr 205); receipt `artifacts/workbench31-guest-command-which-crossprofile-current-candidate-20260927-v1/effect-comparison.json`. Other guest evidence is bound to earlier candidate identities: the 32-case matrix at `artifacts/workbench31-guest-command-which-current-hunk-20260927-v1/evidence-summary.json` (HUNK `be283d619f183267e4b1b25f7d2ecb280815a373e4960550b66fc45a79c5d189`) and seven remaining directory-filter cases (HUNK `3c7a61a5d84b313dc91874360ba3caa26e7f284c189c8b1c985753a30af21f57`). The `MISSING:` attempt remains inconclusive because no complete CopperProbe record was captured. Caller IoErr is not child Result2. Other lookup categories/order, full diagnostics/failure and break behavior, name boundary, MorphOS aliases/help, original PURE/resident lifecycle, package and differential gates remain open |
| `CC10.Which.morphos320` | CC10 | partial | partial | partial | partial | required/open | open | open | D-CC10 | MorphOS candidate calls public DOS `FindVar(..., LV_ALIAS)` and passes 85 supplied vectors/CPU on resident 68000/020/040; receipt `artifacts/which-morphos-native-20260927-switch-matrix-v3/qualification.json`. The vectors include all 32 option combinations with candidate found/missing fixtures and ALIAS-only no-fallthrough; they exercise candidate assumptions, not MorphOS reference behavior. The `ALIAS <name>` line, conflicting-switch outcomes, alias/resident/path order and exact shipped grammar remain unverified; original guest parity, PURE/resident lifecycle, package and differential gates remain open |
| `CC11.Dir.wb31` | CC11 | partial | partial | open | partial | audit/open | open | open | D-CC11 | Workbench DOS 36 syntax candidate; current-source 16-vector/CPU resident receipt `artifacts/cc11-dir-wb31-native-20260922-v1/qualification.json`; exact parity, ALL/INTER, wildcard/soft-link behavior, PURE, package and differential gates remain open |
| `CC11.Dir.morphos320` | CC11 | partial | partial | open | partial | required/open | open | open | D-CC11 | MorphOS source grammar; current-source 15-vector/CPU resident receipt `artifacts/cc11-dir-morphos-native-20260922-v1/qualification.json`; ALL/INTER and packed parity remain open |
| `CC11.DiskFree.morphos320` | CC11 | partial | partial | partial | partial | required/open | open | open | D-CC11 | MorphOS template `VOLUME,NOPOSTFIX/S,PERCENT/S` and public Lock/Info resident body; static receipt plus fifteen-vector/CPU runtime receipt `artifacts/diskfree-morphos-native-20260919-runtime-v3/qualification.json`; exact localized output, large-volume providers, PURE, differential and package gates remain open |
| `CC11.DOSList.morphos320` | CC11 | partial | partial | partial | partial | required/open | open | open | D-CC11 | Three-CPU resident receipt `artifacts/doslist-morphos-native-20260923-volume-v1/qualification.json`; 34 vectors/CPU include zero/one/three-node device/volume/assign passes, public name/MsgPort and device/volume verbose attributes, DateToStr success/failure, disk type and one/three-lock volume lists, deferred/null assign targets, assign/lock-name query failures, linked assign-list rows, mounted/unmounted rows, NAME/ADDRESS filtering, lock refusal, parser/allocation/Ctrl-C, startup and interleaving; original parity, PURE, package and differential gates remain open |
| `CC11.Info.wb31` | CC11 | partial | partial | partial | partial | required/open | open | required/open | D-CC11 | Workbench DOS 36 DEVICE syntax candidate; current-source fourteen-vector/CPU resident receipt `artifacts/cc11-info-wb31-native-20260923-pattern-v14/qualification.json`; its shared test-root HUNK includes an unused MorphOS date-hook export and is not an isolated shipping artifact; exact classic output/disk geometry, installed overlay, PURE, package and differential gates remain open |
| `CC11.Info.morphos320` | CC11 | partial | partial | partial | partial | required/open | open | open | D-CC11 | Current three-CPU resident receipt `artifacts/cc11-info-morphos-native-20260923-pattern-validation-v3/qualification.json`; 66 vectors/CPU cover invalid filter rejection before list access, outer PrintFault/IoErr publication, partial-output Ctrl-C, reversed DOS-list ordering with utility Stricmp sorting and device-before-volume rendering, utility.library v37 and optional locale.library v38/OpenLocale ownership, DOS 51.8 counter fallback/clamping, startup type/name/verbose paths and `info_datetime` formatting; packed-binary output, original parity, PURE, package and differential gates remain open |
| `CC11.List.wb31` | CC11 | partial | partial | partial | partial | audit/open | open | open | D-CC11 | Workbench DOS 36 syntax candidate; current 32-vector/CPU resident receipt `artifacts/cc11-list-wb31-native-20260923-all-v1/qualification.json` (96 total) covers missing-DOS, literal SUB, inclusive SINCE/UPTO date-only filters through DOS StrToDate, recursive ALL via the correctly mapped slot 15 and public APF_DODIR/APF_DIDDIR, invalid-date cleanup, KEYS, DATES/QUICK precedence, NODATES, row fields and interleaving; original recursive path/order, sort/owner/LFORMAT, guest parity, PURE, package and differential gates remain open |
| `CC11.List.morphos320` | CC11 | partial | partial | partial | partial | required/open | open | open | D-CC11 | Current 31-vector/CPU resident receipt `artifacts/cc11-list-morphos-native-20260923-all-v1/qualification.json` (93 total) covers literal SUB, inclusive SINCE/UPTO date-only filters through DOS StrToDate, recursive ALL via public APF_DODIR/APF_DIDDIR, invalid-date cleanup, KEYS, DATES/QUICK precedence, NODATES suppression, documented rows, startup/ownership/output/Ctrl-C and interleaving; original recursive path/order, sort/owner/LFORMAT, exact guest parity, PURE, package and differential gates remain open |
| `CC11.Search.wb31` | CC11 | partial | partial | partial | partial | audit/open | open | open | D-CC11 | Workbench disk 2 binary identity and eight-slot template candidate; current-source 19-vector/CPU, 43-method resident receipt `artifacts/cc11-search-wb31-native-20260927-source-error-regression-v1/qualification.json`; includes file-size-based input allocation and a matching line longer than 8,192 bytes; exact runtime grammar/output, deep traversal, PURE, package and differential gates remain open |
| `CC11.Search.morphos320` | CC11 | partial | partial | partial | partial | audit/open | open | open | D-CC11 | Current-source 46-vector/CPU, 43-method resident receipt `artifacts/cc11-search-morphos-native-20260927-source-file-errors-v1/qualification.json`; preserves locale.library v37 and ConvToUpper/IsCntrl/IsPrint, fixture-classified 0x85 control and 0xe9 printable cases, TAB preservation, LF-only numbering, literal/PATTERN context, 512 KiB MEMF_ANY sizing, halving/recovery, LF-only max-line pre-scan, overlong-line growth, incomplete-line rewind/reread, CTRL-D pre-scan abandonment, DOS 51.28+ Size64/Seek64 above the 32-bit offset limit, source-shaped per-file Open/Read/Seek misses and dangling-link warnings; real locale/large-file handlers, exact guest parity, PURE, package and differential gates remain open |
| `CC11.Type.wb31` | CC11 | partial | open | open | partial | audit/open | open | open | D-CC11 | Workbench DOS 36 five-slot syntax candidate retains classic AnchorPath layout; current-source 17-vector/CPU, 27-method resident receipt `artifacts/cc11-type-wb31-native-20260927-io-failures-v1/qualification.json`, including candidate-modeled Read/Write failure and partial short-write failure; exact output/diagnostics, original parity, PURE, package and differential gates remain open |
| `CC11.Type.morphos320` | CC11 | partial | partial | partial | partial | required/open | open | open | D-CC11 | Current-source 16-vector/CPU, 26-method resident receipt `artifacts/cc11-type-morphos-native-20260927-io-failures-v1/qualification.json`; MorphOS 50.67+ literal-soft-link AnchorPath, explicit 50.66 rejection, DOS Read failure, and a positive short Write followed by selected IoErr failure; exact guest output/diagnostics, packed correspondence, PURE, package and differential gates remain open |
| `CC12.AddBuffers.wb31` | CC12 | partial | partial | partial | partial | audit/open | open | partial | D-CC12 | AB-WB31 |
| `CC12.AddBuffers.morphos320` | CC12 | partial | partial | open | open | required/open | open | open | D-CC12 | C1 |
| `CC12.Assign.wb31` | CC12 | partial | partial | partial | partial | audit/open | open | open | D-CC12 | ASSIGN-WB31-CONTRACT |
| `CC12.Assign.morphos320` | CC12 | partial | partial | partial | partial | required/open | open | open | D-CC12 | ASSIGN-MORPHOS-CONTRACT |
| `CC12.MakeDir.wb31` | CC12 | partial | partial | partial | partial | audit/open | open | partial | D-CC12 | MK3-WB; Workbench resident entry receipt with 14 supplied vectors/CPU; original guest and lifecycle gates remain open |
| `CC12.MakeDir.morphos320` | CC12 | partial | partial | partial | partial | required/open | open | open | D-CC12 | Source-informed NAME/M,ALL/S body; three-CPU resident receipt with 15 supplied vectors/CPU; packed correspondence and guest parity remain open |
| `CC12.MakeLink.wb31` | CC12 | partial | partial | partial | partial | audit/open | open | partial | D-CC12 | ML1 |
| `CC12.MakeLink.morphos320` | CC12 | partial | partial | partial | partial | required/open | open | partial | D-CC12 | ML1 |
| `CC12.Relabel.wb31` | CC12 | partial | partial | partial | partial | audit/open | open | partial | D-CC12 | RELABEL-WB31 |
| `CC12.Relabel.morphos320` | CC12 | partial | partial | open | partial | required/open | open | open | D-CC12 | RELABEL-MORPHOS-HANDLER |
| `CC12.Rename.wb31` | CC12 | partial | partial | partial | partial | audit/open | open | partial | D-CC12 | RN3-WB |
| `CC12.Rename.morphos320` | CC12 | partial | partial | partial | partial | required/open | open | open | D-CC12 | RN3-MO |
| `CC13.Clone.morphos320` | CC13 | open | open | open | open | required/open | open | open | D-CC13 | C1 |
| `CC13.Copy.wb31` | CC13 | open | open | open | open | audit/open | open | open | D-CC13 | C1 |
| `CC13.Copy.morphos320` | CC13 | partial | partial | open | open | required/open | open | open | D-CC13 | CP8 |
| `CC13.Delete.wb31` | CC13 | partial | partial | open | partial | required/open | open | open | D-CC13 | 8-vector/CPU resident frontend receipt; exact classic parity, recursion and diagnostics remain open |
| `CC13.Delete.morphos320` | CC13 | partial | partial | open | partial | required/open | open | open | D-CC13 | 8-vector/CPU resident frontend receipt; parent retry, recursion/link policy and parity remain open |
| `CC13.Join.wb31` | CC13 | open | open | open | partial | audit/open | open | open | D-CC13 | 18-vector/CPU resident frontend receipt; exact classic parser, handlers, lifecycle and parity remain open |
| `CC13.Join.morphos320` | CC13 | partial | partial | open | partial | required/open | open | open | D-CC13 | 18-vector/CPU resident frontend receipt; wildcard/handler behavior, lifecycle and parity remain open |
| `CC13.Trashcan.morphos320` | CC13 | open | open | open | open | audit/open | open | open | D-CC13 | C1 |
| `CC14.FileNote.wb31` | CC14 | partial | partial | open | partial | audit/open | open | open | D-CC14 | FILENOTE-WB31-PARITY; 10-vector/CPU resident receipt |
| `CC14.FileNote.morphos320` | CC14 | partial | partial | partial | partial | required/open | open | open | D-CC14 | FILENOTE-MORPHOS |
| `CC14.FileWrite.morphos320` | CC14 | open | open | open | open | audit/open | open | open | D-CC14 | C1 |
| `CC14.Protect.wb31` | CC14 | partial | partial | partial | partial | audit/open | open | audit/open | D-CC14 | Workbench six-slot syntax candidate; 14-vector/CPU resident receipt |
| `CC14.Protect.morphos320` | CC14 | partial | partial | partial | partial | required/open | open | open | D-CC14 | PROTECT-MORPHOS |
| `CC14.SetDate.wb31` | CC14 | partial | partial | partial | partial | audit/open | open | open | D-CC14 | Workbench resident receipt `artifacts/setdate-native-entry-20260920/qualification.json` passes 12 supplied vectors/CPU, including startup and missing-DOS boundaries; original parity, PURE, packaging and differential gates remain open |
| `CC14.SetDate.morphos320` | CC14 | partial | partial | partial | partial | required/open | open | open | D-CC14 | SETDATE-MORPHOS |
| `CC14.SetFileSize.morphos320` | CC14 | open | open | open | open | audit/open | open | open | D-CC14 | C1 |
| `CC14.Touch.morphos320` | CC14 | partial | partial | partial | partial | required/open | open | open | D-CC14 | TOUCH-MORPHOS |
| `CC15.ConvertText.morphos320` | CC15 | open | open | open | open | audit/open | open | open | D-CC15 | C1 |
| `CC15.MirrorCheck.morphos320` | CC15 | open | open | open | open | audit/open | open | open | D-CC15 | C1 |
| `CC15.MirrorCopy.morphos320` | CC15 | open | open | open | open | audit/open | open | open | D-CC15 | C1 |
| `CC15.Newer.morphos320` | CC15 | open | open | open | open | audit/open | open | open | D-CC15 | C1 |
| `CC15.Replace.morphos320` | CC15 | open | open | open | open | audit/open | open | open | D-CC15 | C1 |
| `CC15.Sort.wb31` | CC15 | open | open | open | open | audit/open | open | open | D-CC15 | C1 |
| `CC15.Sort.morphos320` | CC15 | open | open | open | open | required/open | open | open | D-CC15 | C1 |
| `CC16.Date.wb31` | CC16 | partial | partial | partial | partial | audit/open | open | open | D-CC16 | Three-CPU resident receipt `artifacts/date-native-entry-20260921-v2/qualification.json` passes 9 supplied vectors/CPU, including startup and missing-DOS boundaries; utility v36 is the verified floor for UMult32/UDivMod32 while the captured utility v0 request remains differential evidence; original guest precedence/IoErr, PURE/resident lifecycle, package and differential gates remain open |
| `CC16.Date.morphos320` | CC16 | partial | partial | partial | partial | required/open | open | open | D-CC16 | Hash-bound 3,748-byte `MorphOS/C/Date` identity and 9,453-byte released source are recorded in `contracts/Date.md` and `reference-captures/date-morphos-binary-audit-20260923.json`; resident receipt `artifacts/morphos-date-native-20260919-runtime-v1/qualification.json` remains adapter evidence while exact guest diagnostics/IoErr, PURE/resident lifecycle, package and differential gates remain open |
| `CC16.Time.morphos320` | CC16 | partial | partial | open | open | required/open | open | open | D-CC16 | Hash-bound 4,999-byte 1.0 ISO identity, documented purpose, and installer `+P` event are recorded in `contracts/Time.md` and `reference-captures/time-morphos-binary-audit-20260923.json`; exact command-tail grammar/timer/output behavior, guest parity, PURE/lifecycle and package gates remain open |
| `CC16.Uptime.morphos320` | CC16 | partial | partial | open | open | required/open | open | open | D-CC16 | No-template boundary and 2,733-byte 50.5 binary identity are recorded in `contracts/Uptime.md` and `reference-captures/uptime-morphos-binary-audit-20260923.json`; exact output/timer behavior, guest parity, PURE/lifecycle and package gates remain open |
| `CC16.Wait.wb31` | CC16 | partial | partial | partial | partial | audit/open | open | open | D-CC16 | Workbench resident receipt `artifacts/wait-wb31-native-20260920-boundaries/qualification.json` passes 17 supplied vectors/CPU, including startup and missing-DOS boundaries; original timing/diagnostics, PURE, packaging and differential gates remain open |
| `CC16.Wait.morphos320` | CC16 | partial | partial | partial | partial | required/open | open | open | D-CC16 | WAIT-MORPHOS |
| `CC16.WaitForLib.morphos320` | CC16 | partial | partial | partial | partial | required/open | open | open | D-CC16 | WAITFORLIB-MORPHOS |
| `CC16.WaitForNotification.morphos320` | CC16 | partial | partial | partial | partial | required/open | open | open | D-CC16 | WAITFORNOTIFICATION-MORPHOS |
| `CC16.WaitForPort.morphos320` | CC16 | partial | partial | partial | partial | required/open | open | open | D-CC16 | WAITFORPORT-MORPHOS |
| `CC16.WaitX.morphos320` | CC16 | partial | partial | open | open | required/open | open | open | D-CC16 | Hash-bound 4,230-byte 51.1 ISO identity, documented wait-then-execute purpose, and installer `+P` event are recorded in `contracts/WaitX.md` and `reference-captures/waitx-morphos-binary-audit-20260923.json`; exact grammar/timer/launch behavior, guest parity, PURE/lifecycle and package gates remain open |
| `CC17.Avail.wb31` | CC17 | partial | partial | partial | partial | audit/open | open | open | D-CC17 | Workbench resident receipt `artifacts/avail-wb31-native-20260920-boundaries/qualification.json` passes 12 supplied vectors/CPU, including startup and missing-DOS boundaries; expunge, original diagnostics, PURE, packaging and differential gates remain open |
| `CC17.Avail.morphos320` | CC17 | partial | partial | open | partial | required/open | open | open | D-CC17 | Hash-bound `MorphOS/C/Avail` identity and released-source contract; refreshed resident receipt `artifacts/avail-morphos-native-20260923-fakechipp-v3/qualification.json` covers `PrintFault` parser diagnostics, fake-chip `MaxLocMem` adjustment and twelve supplied vectors/CPU. Guest output, lifecycle/PURE, packaging and differential gates remain open |
| `CC17.CPU.wb31` | CC17 | open | open | open | open | audit/open | open | open | D-CC17 | C1 |
| `CC17.CPU.morphos320` | CC17 | partial | partial | open | open | required/open | open | open | D-CC17 | Hash-bound 4,123-byte 50.10 ISO identity, documented purpose, and installer `+P` event are recorded in `contracts/CPU.md` and `reference-captures/cpu-morphos-binary-audit-20260923.json`; exact grammar/control/output behavior, guest parity, PURE/lifecycle and package gates remain open |
| `CC17.ShowConfig.morphos320` | CC17 | partial | partial | open | open | audit/open | open | open | D-CC17 | Hash-bound 15,811-byte 50.17 ISO identity and documented purpose are recorded in `contracts/ShowConfig.md` and `reference-captures/showconfig-morphos-binary-audit-20260923.json`; no installer protection event was observed, so purity, providers, exact output, guest parity, lifecycle and package gates remain open |
| `CC17.Stat.morphos320` | CC17 | partial | partial | open | open | required/open | open | open | D-CC17 | Hash-bound 4,319-byte 50.2 ISO identity, documented purpose, and installer `+P` event are recorded in `contracts/Stat.md` and `reference-captures/stat-morphos-binary-audit-20260923.json`; exact grammar/counters/output behavior, guest parity, PURE/lifecycle and package gates remain open |
| `CC17.Status.wb31` | CC17 | partial | partial | partial | partial | audit/open | open | open | D-CC17 | STATUS-WB31-CONTRACT |
| `CC17.Status.morphos320` | CC17 | partial | partial | partial | partial | required/open | open | open | D-CC17 | Hash-bound 3,679-byte `MorphOS/C/Status` identity and 5,709-byte released source are recorded in `contracts/Status.md` and `reference-captures/status-morphos-binary-audit-20260923.json`; source-aligned case-insensitive legacy COM matching, Ctrl-C boundary, and DOS 51.51 provider/tag ABI vectors including process/command filtering are covered by the refreshed 19-vector/CPU resident receipt, built with `--exports none` to exclude unrelated callbacks from the shared root assembly. Complete provider population, guest parity, PURE/lifecycle, package and differential gates remain open |
| `CC17.TaskList.morphos320` | CC17 | partial | partial | partial | partial | required/open | open | open | D-CC17 | TASKLIST-MORPHOS |
| `CC17.Version.wb31` | CC17 | partial | partial | open | partial | audit/open | open | open | D-CC17 | VERSION-WB31-CONTRACT; original first Resident provider, FULL public date conversion, parser return 20, DOS v37 startup minimum and direct FILE scanning/HUNK LoadSeg fallback are implemented as a bounded candidate. The latest `artifacts/version-wb31-native-20260927-files-v5/qualification.json` passes 92 cases/CPU (276 total) across 68000/020/040, with source/compiled-input hashes stable and zero shared-image writes. The ordered named provider chain covers Resident, command segment, trailing-colon DOS device handler, Exec LibList/DeviceList, direct-name file, LIBS: and DEVS: files; the exact `C:Version LIBS:version.library` guest pair matches. Earlier FILE, Resident, system, command-segment and DF0: receipts remain separately recorded in `contracts/Version.md`; ten Resident-path pairs and bounded FILE/system/device pairs are preserved there. Broader provider/error-ordering and parser/system parity, child Result2, actual PURE/resident lifecycle, reference closure, rights and packaging remain open |
| `CC17.Version.morphos320` | CC17 | partial | partial | open | partial | audit/open | open | open | D-CC17 | MorphOS resident system/RES/direct-FILE/MD5 candidate passes 78 supplied vectors per CPU; `artifacts/version-morphos-native-20260924-liblist-v17/qualification.json` records three-CPU resident HUNK and static compatibility passes (60 reachable methods). Default/RES uses DOS `FilePart`, then Exec Resident and protected `LibList` lookup with Utility `Stricmp`; the loaded-library path copies the node name and covers numeric comparison and `FULL` date/extra output with a caller-provided utility.library v37 lease closed on success/failure, including library-open failure. RES segment lookup covers both lists and `shellcmd` fallbacks. Exact `LIBS:`/`DEVS:`/DeviceList/direct-file priority, guest parity, packed correspondence, PURE/lifecycle, licensing and package gates remain open |
| `CC18.Break.wb31` | CC18 | partial | partial | partial | partial | audit/open | open | open | D-CC18 | Current three-CPU resident receipt `artifacts/break-wb31-native-20260927-pid-version-floor-regression-v1/qualification.json` passes 24 supplied vectors/CPU with 13 reachable methods and no managed runtime features/helpers, external targets, exception regions, fatal sites, leaks or shared-image writes; it covers every C/D/E/F switch combination and requalifies the shared fixture change. Its 68000 HUNK SHA-256 remains `cd094ef9e367a60cf3225cca2d0deef9fbeee604e41df6bc1f98b96ccc55acdc`, so the earlier original/candidate effect receipts for all sixteen C/D/E/F combinations, including default, plus `ALL`, on CLI 1 remain bound to the same candidate. The five-case guest receipt `artifacts/workbench31-guest-command-cc18-command-pairs-20260927-v2/qualification.json` binds missing-PROCESS and nonexistent-CLI diagnostics; a separate `C:Break 0` pair verifies the exact 54-byte missing-process stream, return 20 and caller IoErr 0. Original metadata including protection0 is preserved. Other process IDs/diagnostics, MorphOS, lifecycle, package and full differential gates remain open |
| `CC18.Break.morphos320` | CC18 | partial | partial | partial | partial | required/open | open | open | D-CC18 | Current resident receipt `artifacts/break-morphos-native-20260927-pid-version-floor-v1/qualification.json` passes 34 supplied vectors/CPU on 68000/020/040 with 16 reachable methods, no managed allocation sites, runtime helpers/features, external targets, exception regions, fatal sites, leaks or shared-image writes. It covers all C/D/E/F masks, PROCESS/PORT precedence, and the source-backed Exec 50.45 PID fallback floor (50.44 forbids fallback; 51.0 permits it). Original MorphOS guest output, exact PID numbering/reuse, target races, PURE/resident lifecycle, package and differential gates remain open |
| `CC18.ChangeTaskPri.wb31` | CC18 | partial | partial | partial | partial | audit/open | open | open | D-CC18 | Current three-CPU resident receipt `artifacts/changetaskpri-wb31-native-20260927-current-regression-v1/qualification.json` passes thirteen supplied vectors/CPU with 13 reachable methods and no managed runtime features/helpers, external targets, exception regions, fatal sites, leaks or shared-image writes. The five-case guest receipt `artifacts/workbench31-guest-command-cc18-command-pairs-20260927-v2/qualification.json` includes exact parser/missing-CLI pairs and `ChangeTaskPri 0` result parity. `artifacts/workbench31-guest-command-changetaskpri-self-priority-candidate-20260927-v1/effect-comparison-v2.json` verifies the original and candidate both changed CLI task 1 from priority 0 to 42 with matching output, return0 and caller IoErr0. Original metadata including protection0 is preserved. Other targets/options/errors, MorphOS PID, lifecycle, package and full differential gates remain open |
| `CC18.ChangeTaskPri.morphos320` | CC18 | partial | partial | partial | partial | required/open | open | open | D-CC18 | Current three-CPU resident receipt `artifacts/changetaskpri-morphos-native-20260927-pid-version-floor-v1/qualification.json` passes fourteen supplied vectors/CPU with 16 reachable methods and no managed allocation sites, runtime features/helpers, external targets, exception regions, fatal sites, leaks or shared-image writes; it covers the source-backed Exec 50.45 PID fallback floor (50.44 forbids fallback; 51.0 permits it). MorphOS SDK selector/identity semantics are also reflected in CopperStart: PID zero selects current task, while the PID attribute reports its task identity; ten focused provider tests pass. Original MorphOS PID numbering/reuse, command guest parity, task liveness/races, diagnostics, lifecycle, PURE and package gates remain open |
| `CC18.DevList.morphos320` | CC18 | partial | partial | partial | partial | required/open | open | open | D-CC18 | Three-CPU resident receipt `artifacts/devlist-morphos-native-20260919-multinode-v2/qualification.json`; eleven vectors/CPU include empty, one-device and three-device lists, allocation/Ctrl-C/missing-DOS paths, startup and interleaving; complete population, packed correspondence, original parity, PURE, package and differential gates remain open |
| `CC18.FlushLib.morphos320` | CC18 | open | open | open | open | audit/open | open | open | D-CC18 | MorphOS Library's provisional `LIBRARY/A/M,ONCE/S,QUIET/S` lead and original member identity (3,755 bytes, SHA-256 `05c06f989d58d3eca7fb34d5eec2ddb0feea894b126e6857880a0542d7204140`, extent 175432) are recorded in `contracts/FlushLib.md`; decode/guest behavior, candidate body and shipping evidence remain open |
| `CC18.iKill.morphos320` | CC18 | open | open | open | open | audit/open | open | open | D-CC18 | MorphOS Library's optional `TASKNAME` and crosshair-selection lead plus original member identity (18,014 bytes, SHA-256 `8c06ee672ff86c02f8f99b176435b9fe313400da2ae5f95d51bcaf2c2e10258b`, extent 178944) are recorded in `contracts/iKill.md`; exact 3.20 task/UI/safety behavior, candidate body and shipping evidence remain open |
| `CC18.LibList.morphos320` | CC18 | partial | partial | partial | partial | required/open | open | open | D-CC18 | MorphOS 50.6 resident receipt `artifacts/liblist-morphos-native-20260919-multinode/qualification.json`: ten vectors/CPU including empty, one-node and three-node linked lists, allocation/Ctrl-C/startup/boundary/interleaving; full population, packed correspondence, PURE, differential and package gates remain open |
| `CC18.LoadLib.morphos320` | CC18 | open | open | open | open | audit/open | open | open | D-CC18 | MorphOS Library's provisional `PATH/A/M,VERBOSE/S` lead and original member identity (2,480 bytes, SHA-256 `9cd6bfa13dcf94d0f934a24bfc31861fb4c637d865ebc7f63aee2466f3e28c80`, extent 176140) are recorded in `contracts/LoadLib.md`; decode/guest behavior, candidate body and shipping evidence remain open |
| `CC18.ModList.morphos320` | CC18 | partial | partial | partial | partial | required/open | open | open | D-CC18 | Three-CPU resident receipt `artifacts/modlist-morphos-native-20260919-multinode-v2/qualification.json`; thirteen vectors/CPU include empty, one-resident and three-resident tables, parser/allocation/Ctrl-C/missing-DOS paths, startup and interleaving; complete resident population, original parity, PURE, package and differential gates remain open |
| `CC18.PortList.morphos320` | CC18 | partial | partial | partial | partial | required/open | open | open | D-CC18 | Three-CPU resident receipt `artifacts/portlist-morphos-native-20260919-multinode-v2/qualification.json`; eleven vectors/CPU include empty, one-port and three-port lists, allocation/Ctrl-C/missing-DOS paths, startup and interleaving; complete population, original parity, PURE, package and differential gates remain open |
| `CC18.ResList.morphos320` | CC18 | partial | partial | partial | partial | required/open | open | open | D-CC18 | Three-CPU resident receipt `artifacts/reslist-morphos-native-20260919-multinode-v2/qualification.json`; eleven vectors/CPU include empty, one-resource and three-resource lists, allocation/Ctrl-C/missing-DOS paths, startup and interleaving; complete population, original parity, PURE, package and differential gates remain open |
| `CC19.DiskCache.morphos320` | CC19 | open | open | open | open | audit/open | open | open | D-CC19 | C1 |
| `CC19.DiskChange.wb31` | CC19 | partial | partial | partial | partial | audit/open | open | open | D-CC19 | Workbench resident receipt `artifacts/diskchange-wb31-native-20260920-boundaries/qualification.json` passes 12 supplied vectors/CPU, including missing-DOS startup; exact handler parity, PURE, packaging and differential gates remain open |
| `CC19.DiskChange.morphos320` | CC19 | partial | partial | partial | partial | required/open | open | open | D-CC19 | DISKCHANGE-MORPHOS |
| `CC19.FSDie.morphos320` | CC19 | open | open | open | open | required/open | open | open | D-CC19 | C1 |
| `CC19.FSList.morphos320` | CC19 | open | open | open | open | required/open | open | open | D-CC19 | C1 |
| `CC19.FSPrefs.morphos320` | CC19 | open | open | open | open | audit/open | open | open | D-CC19 | C1 |
| `CC19.Lock.wb31` | CC19 | partial | partial | partial | partial | audit/open | open | open | D-CC19 | 12-vector/CPU resident receipt; exact handler parity open |
| `CC19.Lock.morphos320` | CC19 | partial | partial | partial | partial | required/open | open | open | D-CC19 | LOCK-MORPHOS |
| `CC19.Mount.wb31` | CC19 | partial | partial | open | open | audit/open | open | open | D-CC19 | Workbench 3.1 binary grammar is captured (outer `DEVICE/M,FROM/K`; inner 31-slot template) and a separate resident profile entry with DOSDrivers, `DEVS:MountList` `ReadItem`/`ReadArgs`, wildcard traversal, Workbench startup iteration, and bounded icon.library `.info` aggregation is qualified on 68000/020/040 in `artifacts/mount-wb31-native-20260919-wildcard/`; the outer-entry boundary now has a 27-invocation three-CPU receipt, including empty and single-argument startup returns plus a source-not-found startup cleanup path, in `artifacts/mount-wb31-native-entry-20260921-v5/`; runtime/provider lifecycle, PURE, differential and package gates remain open |
| `CC19.Mount.morphos320` | CC19 | partial | partial | open | open | audit/open | open | open | D-CC19 | Explicit `FROM` plus bounded direct-DEVICE DOSDrivers resolver, icon.library `.info` fallback, `DEVS:MountList` `ReadItem`/`ReadArgs` fallback, wildcard traversal and multi-device iteration, with three-CPU static receipt in `artifacts/mount-morphos-native-20260921-v2/` and a five-vector-per-CPU outer-entry receipt in `artifacts/mount-morphos-native-entry-20260921-v2/`; expansion33 is the verified `MakeDosNode`/`AddDosNode` capability floor while the captured expansion37 request remains differential evidence; real handler/provider lifecycle, PURE admission, differential and package gates remain open |
| `CC19.RemRAD.wb31` | CC19 | open | open | open | open | audit/open | open | open | D-CC19 | C1 |
| `CC19.RemRAD.morphos320` | CC19 | open | open | open | open | required/open | open | open | D-CC19 | C1 |
| `CC19.RunFS.morphos320` | CC19 | open | open | open | open | audit/open | open | open | D-CC19 | C1 |
| `CC19.UnMount.morphos320` | CC19 | open | open | open | open | required/open | open | open | D-CC19 | C1 |
| [`CC20.AddDataTypes.wb31`](contracts/AddDataTypes.md) | CC20 | partial | open | open | open | audit/open | open | open | D-CC20 | Hash-bound 5,880-byte HUNK audit records the `FILES/M,QUIET/S,REFRESH/S` candidate, non-PURE protection word, startup placement and historical requester boundary; the resident profile-stack receipt `artifacts/adddatatypes-workbench31-native-20260925-v6/qualification.json` passes fifteen vectors per CPU, including the binary-derived 4096-byte DTCD stack argument; guest LIST/diagnostics/runtime/PURE/package/differential gates remain open |
| [`CC20.AddDataTypes.morphos320`](contracts/AddDataTypes.md) | CC20 | partial | open | open | open | audit/open | open | open | D-CC20 | Hash-bound 7,751-byte packed member and released-source contract record `FILES/M,QUIET/S,REFRESH/S,LIST/S`, datatype-list locking, IFF/DTCD loading and refresh paths. The source-informed resident shared-list/LIST slice passes five vectors per CPU in `artifacts/adddatatypes-morphos-native-list-20260925-v1/`; the production CLI/Workbench-startup/FILES/IFF/DTHD/DTCD/REFRESH probe passes 38 command vectors plus one callback-wrapper probe per CPU in `artifacts/adddatatypes-morphos-cli-run-20260925-v18/`. The current segment-lifetime probe passes 41 command vectors plus one callback-wrapper probe per CPU on resident 68000/020/040 HUNKs in `artifacts/adddatatypes-morphos-segment-lifetime-20260925-v19/`, verifying open-segment retention, candidate-segment unload for discarded same/open duplicates, and old-segment unload before freeing a replaced closed descriptor. These are supplied-provider ownership checks only. The v20 profile-stack receipt `artifacts/adddatatypes-morphos-profile-stacks-20260925-v20/` passes 42 command vectors plus one callback probe per CPU and checks the 32768-byte MorphOS embedded-DTCD stack on CLI and Workbench-startup paths. Real MorphOS DOS-loader callback/unload behavior, guest validation of traversal and REFRESH dates, historical AROS_STACKSIZE-to-binary correspondence, deeper original-guest traversal, malformed/duplicate/open descriptor guest behavior, actual CLI/Workbench lifecycle, private ABI, guest parity, PURE, licensing, package and differential gates remain open |
| [`CC20.BindDrivers.wb31`](contracts/BindDrivers.md) | CC20 | partial | partial | partial | partial | audit/open | open | open | D-CC20 | Independent hash-bound Workbench binary audit and separate resident body; `artifacts/workbench31-binddrivers-native-entry-20260925-provider-v4/qualification.json` compiles 68000/020/040 HUNKs and passes twelve supplied-provider vectors per CPU, including library-open order/failures, Lock/Examine/ExNext scan, suffix stripping, strict PRODUCT parsing, successful and failing ConfigDev/LoadSeg/Resident/InitResident paths, CurrentBinding offsets, and Workbench reply after cleanup. Resident candidate and supplied fixtures do not establish original guest semantics, command startup integration, PURE/lifecycle, package or differential gates |
| [`CC20.BindDrivers.morphos320`](contracts/BindDrivers.md) | CC20 | partial | partial | partial | partial | required/open | open | open | D-CC20 | MorphOS 50.3 source-bound body has a three-CPU resident receipt (`artifacts/binddrivers-morphos-native-entry-20260925-currentbinding-order-regression-v2/qualification.json`) with twenty-six scanner and one PRODUCT-parser vector per CPU; includes public CurrentBinding field order, Exec `TypeOfMem` HUNK extent checks, second-HUNK resident discovery, malformed/cyclic range rejection, multiple PRODUCT pairs and ConfigDev prepend order, interleaved scans, `APF_DIDDIR` handling, and provider-warning cleanup. Unterminated Icon responses, packed correspondence, installed overlay, original guest, PURE, licensing, package and differential gates remain open |
| [`CC20.IPrefs.wb31`](contracts/IPrefs.md) | CC20 | partial | open | open | open | audit/open | open | open | D-CC20 | Hash-bound 13,848-byte HUNK audit records the startup-service/no-template boundary, classic library and diagnostic surface, and non-PURE protection word; exact service ordering/runtime/lifecycle/PURE/package/differential gates remain open |
| [`CC20.IPrefs.morphos320`](contracts/IPrefs.md) | CC20 | partial | open | open | open | audit/open | open | open | D-CC20 | Hash-bound 43,798-byte packed member and released-source contract record duplicate guard, long-lived child process/completion protocol, preference-table/handler surface and installation validation; exact packed behavior, guest/lifecycle/PURE/package/differential gates remain open |
| [`CC20.LoadMonDrvs.morphos320`](contracts/LoadMonDrvs.md) | CC20 | partial | partial | open | partial | required/open | open | open | D-CC20 | Hash-bound 2,279-byte packed member and documented `FROM/K,EXCEPT` candidate for DEVS:Monitors/default, alternate paths and excluded-driver behavior. Latest three-CPU resident receipt `artifacts/loadmondrvs-morphos-native-entry-20260925-multiple-match-v7/qualification.json` compiles 22 reachable methods with zero runtime features/helpers/external targets/faults and passes seventeen supplied vectors per CPU, including case-insensitive exclusion of an actual match, an excluded first result followed by a loaded second driver, alternate directory paths, directory-entry skipping, segment bounds, init/rollback, and interleaved calls. Packed parser correspondence, provider-backed monitor effects, original guest, lifecycle/PURE, package and differential gates remain open |
| [`CC20.LoadResource.wb31`](contracts/LoadResource.md) | CC20 | partial | partial | partial | open | audit/open | open | open | D-CC20 | Hash-bound audits cover parser/message ABI, worker startup, library/catalog lifecycle and resource actions. Native fixtures on 68000/020/040 pass 19 protocol, 11 hook, 10 registry, 34 action, 31 worker and 15 launch cases per CPU. Latest receipts are linked in the contract: hook service-name-20260926-v1, actions 20260926-v6, worker 20260926-v7, launch 20260926-v2. Canonical Latin-1 service discovery, source lazy catalog calls/non-cleared pointer/nested closes, diagnostic IoErr after CloseCatalog, detached SegList transfer/rollback and first real request are covered. A strict two-HUNK packer has 10 independent tests; packaged probes are not runnable commands. Complete client/child entry, duplicate startup, callback quiescence and wakeup, actual DOS retirement, original guest parity, malformed font-size fidelity, rights/package/differential gates remain open; original PURE bit stays clear |
| [`CC20.SetFont.wb31`](contracts/SetFont.md) | CC20 | partial | partial | partial | partial | audit/open | open | open | D-CC20 | Hash-bound HUNK and bounded disassembly audits bind `SIZE/N` low-word truncation and unsigned `CMP.W/BLS`, `ACTION_DISK_INFO`/`InfoData`/Window layout, proportional-font rejection without `PROP`, the `Forbid`/font-replacement/`Permit` sequence, `TextAttr` `SCALE`/`PROP` flags, `tf_Style`-gated escape literals, return/IoErr paths, and ignored output failures; three-CPU static receipt `artifacts/setfont-wb31-native-static-20260922-v1/qualification.json` and refreshed 33-vector-per-CPU resident receipt `artifacts/setfont-wb31-native-entry-20260926-font-ownership-v20/qualification.json` (18 reachable methods) cover parser/result/name/TextAttr/InfoData allocation failures and cleanup plus size/TextAttr/packet/window/font/escape/result/startup/interleaving boundaries. Original guest handler behavior, concurrency, PURE/lifecycle, package and differential gates remain open |
| [`CC20.SetKeyboard.wb31`](contracts/SetKeyboard.md) | CC20 | partial | partial | open | partial | audit/open | open | open | D-CC20 | Captured `KEYMAP/A` grammar and source-bound DOS `AddPart`/`LoadSeg`, keymap.resource reuse (including the list-tail node), and keymap.library `SetKeyMapDefault` body; three-CPU static receipt `artifacts/setkeyboard-wb31-native-static-20260922-v2/qualification.json` plus ten-vector-per-CPU runtime receipt `artifacts/setkeyboard-wb31-native-entry-20260922-v2/qualification.json`; original guest diagnostics, lifecycle/PURE, package and differential gates remain open |
| `CC20.SetKeyboard.morphos320` | CC20 | partial | partial | open | partial | audit/open | open | open | D-CC20 | Packed member is hash/extent-bound and its bounded inspection found no plaintext template/diagnostics; source-informed DOS37 resident candidate and 12-vector/CPU receipt `artifacts/setkeyboard-morphos-native-entry-20260922-v1/qualification.json` cover resource reuse, absolute/KEYMAPS:/MOSSYS:Devs/Keymaps loading, resident publication, duplicate race and rollback. Exact packed syntax/diagnostics, guest parity, PURE/lifecycle, package and differential gates remain open |
| [`CC20.SetPatch.wb31`](contracts/SetPatch.md) | CC20 | partial | open | open | open | audit/open | open | open | D-CC20 | Hash-bound 13,484-byte byte-identical Install3.1/Workbench3.1 HUNK audit records the `QUIET/S,NOCACHE/S,REVERSE/S,NOAGA/S` candidate, non-PURE protection and machine-specific patch families; exact parser/patch predicates/runtime/lifecycle/PURE/package/differential gates remain open |
| `CC21.Format.morphos320` | CC21 | partial | partial | open | open | audit/open | open | open | D-CC21 | Resident trackdisk slice and classic/TD64 provider receipts exist; synthetic DOS-handler/trackdisk receipt `artifacts/format-morphos-native-entry-20260921-v4/qualification.json` passes 10 vectors per CPU with full write/readback and quick initialization; production media queue, packed behavior, TD64/SFS/icon, PURE and package gates remain open |
| `CC21.Install.wb31` | CC21 | open | open | open | open | audit/open | open | open | D-CC21 | C1 |
| `CC21.Install.morphos320` | CC21 | open | open | open | open | required/open | open | open | D-CC21 | C1 |
| `CC21.MagTape.wb31` | CC21 | open | open | open | open | audit/open | open | open | D-CC21 | C1 |
| `CC21.Reboot.wb31` | CC21 | partial | partial | partial | partial | required/open | open | open | D-CC21 | 10-vector/CPU resident receipt; reboot transition parity open |
| `CC21.Reboot.morphos320` | CC21 | partial | partial | partial | partial | required/open | open | open | D-CC21 | REBOOT-MORPHOS |
| [`CC21.SetClock.wb31`](contracts/SetClock.md) | CC21 | partial | partial | partial | partial | audit/open | open | open | D-CC21 | Separate DOS36 resident classic-vector candidate passes seventeen supplied vectors per CPU in `artifacts/setclock-wb31-native-20260920-candidate/qualification.json`, including startup and missing-DOS boundaries; exact clock behavior, diagnostics, PURE, lifecycle, package and differential gates remain open |
| `CC21.SetClock.morphos320` | CC21 | partial | partial | partial | partial | required/open | open | open | D-CC21 | SETCLOCK-MORPHOS |
| `CC21.ShutDown.morphos320` | CC21 | open | open | open | open | required/open | open | open | D-CC21 | C1 |
| [`CC22.Check2090.wb31`](contracts/Check2090.md) | CC22 | partial | partial | open | partial | audit/open | open | open | D-CC22 | CHECK2090-WB31-CONTRACT |
| [`CC22.ExtractKickstart.wb31`](contracts/ExtractKickstart.md) | CC22 | partial | partial | open | open | required/open | open | open | D-CC22 | Source-derived resident candidate and seventeen-vector/CPU trackdisk fixture receipt `artifacts/extractkickstart-wb31-native-20260921-v6/qualification.json`; utility0 is the verified existence floor because the replacement calls no utility vector; guest media parity, PURE, packaging and differential gates remain open |
| [`CC22.FindResident.wb31`](contracts/FindResident.md) | CC22 | partial | partial | open | partial | audit/open | open | open | D-CC22 | DOS36 capability-floor resident candidate and eleven-vector/CPU receipt `artifacts/findresident-wb31-native-20260921-v4/qualification.json`; original binary requests DOS37, while diagnostics/result precedence, guest parity, PURE/resident lifecycle, licensing and package admission remain open |
| [`CC22.GuessBootDev.wb31`](contracts/GuessBootDev.md) | CC22 | partial | partial | open | partial | audit/open | open | open | D-CC22 | GUESSBOOTDEV-WB31-CONTRACT |
| [`CC22.IconPos.wb31`](contracts/IconPos.md) | CC22 | partial | partial | open | partial | required/open | open | open | D-CC22 | ICONPOS-WB31-CONTRACT |
| [`CC22.Prod_Prep.wb31`](contracts/Prod_Prep.md) | CC22 | partial | partial | open | open | audit/open | open | open | D-CC22 | Static media contract records the custom interactive RDB/partition command surface and destructive device/resource dependencies; guest grammar, providers, native body, PURE, package and differential evidence remain open |
| [`CC22.UpdateWBFiles.wb31`](contracts/UpdateWBFiles.md) | CC22 | partial | partial | open | open | audit/open | open | open | D-CC22 | Static media contract records the `NEWWB:` preference/icon update surface and DOS/IFFParse/icon/graphics dependencies; exact update matrix, native body, PURE, package and differential evidence remain open |
| `CC23.Beep.morphos320` | CC23 | partial | partial | partial | partial | required/open | open | open | D-CC23 | BEEP-MORPHOS |
| `CC23.IconX.wb31` | CC23 | open | open | open | open | audit/open | open | open | D-CC23 | C1 |
| `CC23.IconX.morphos320` | CC23 | open | open | open | open | required/open | open | open | D-CC23 | C1 |
| `CC23.LoadWB.wb31` | CC23 | open | open | open | open | audit/open | open | open | D-CC23 | C1 |
| `CC23.LoadWB.morphos320` | CC23 | open | open | open | open | required/open | open | open | D-CC23 | C1 |
| `CC23.Open.morphos320` | CC23 | open | open | open | open | audit/open | open | open | D-CC23 | C1 |
| `CC23.OpenURL.morphos320` | CC23 | open | open | open | open | required/open | open | open | D-CC23 | C1 |
| `CC23.SendBeacon.morphos320` | CC23 | open | open | open | open | audit/open | open | open | D-CC23 | C1 |
| `CC23.WBRun.morphos320` | CC23 | open | open | open | open | required/open | open | open | D-CC23 | C1 |
| `CC24.Clip.morphos320` | CC24 | open | open | open | open | required/open | open | open | D-CC24 | C1 |
| [`CC24.ConClip.wb31`](contracts/ConClip.md) | CC24 | partial | partial | open | open | audit/open | open | open | D-CC24 | Hash-bound binary identity, `CLIPUNIT=UNIT/N,OFF/S` syntax candidate, NDK private console ABI evidence, raw-call scaffold, and source-bound OFF control plane are recorded; persistent hook/service behavior, original guest capture, lifecycle, package and differential gates remain open |
| [`CC24.ConClip.morphos320`](contracts/ConClip.md) | CC24 | partial | partial | open | open | audit/open | open | open | D-CC24 | Hash-bound packed identity, released-source persistent worker contract, NDK private console ABI evidence, raw-call scaffold, and source-bound OFF control plane are recorded; no transient Clip substitute is admitted, and guest hook/device behavior, lifecycle, package and differential gates remain open |
| [`CC24.RequestChoice.wb31`](contracts/RequestChoice.md) | CC24 | partial | partial | partial | partial | audit/open | open | open | D-CC24 | Three-CPU DOS36/Intuition candidate receipt `artifacts/requestchoice-wb31-native-20260920-candidate/qualification.json` passes eleven supplied vectors per CPU, including the captured four-slot template and two interleaved callers; exact guest diagnostics/UI, timeout/type policy, PURE, package and differential gates remain open |
| [`CC24.RequestChoice.morphos320`](contracts/RequestChoice.md) | CC24 | partial | partial | partial | partial | required/open | open | open | D-CC24 | Three-CPU resident receipt `artifacts/requestchoice-morphos-native-20260921-v2/qualification.json` passes sixteen supplied Intuition/Exec/timer vectors per CPU, including failure cleanup, missing-DOS startup and two interleaved callers; DOS36 is the verified parser floor while Intuition37 remains pending lower-floor proof; exact guest UI, PURE, package and differential gates remain open |
| [`CC24.RequestFile.wb31`](contracts/RequestFile.md) | CC24 | partial | partial | partial | partial | audit/open | open | open | D-CC24 | Separate DOS36/ASL36 resident syntax candidate keeps the captured 13-slot grammar and passes twelve supplied vectors per CPU in `artifacts/requestfile-wb31-native-20260920-candidate/qualification.json`; exact requester behavior, diagnostics, PURE, lifecycle, package and differential gates remain open |
| [`CC24.RequestFile.morphos320`](contracts/RequestFile.md) | CC24 | partial | partial | partial | partial | required/open | open | open | D-CC24 | Three-CPU resident receipt `artifacts/requestfile-morphos-native-20260921-v4/qualification.json` passes twelve supplied DOS/ASL invocations per CPU, including every option-to-tag mapping, missing-DOS Result2 startup, repeat ownership and two interleaved callers; DOS36 and ASL36 are the verified parser/requester floors while captured V37 requests remain differential evidence; exact requester/reference behavior, PURE admission, package and differential gates remain open |
| `CC24.RequestString.morphos320` | CC24 | open | open | open | open | audit/open | open | open | D-CC24 | C1 |
| `CC25.Ed.wb31` | CC25 | open | open | open | open | audit/open | open | open | D-CC25 | C1 |
| `CC25.Ed.morphos320` | CC25 | open | open | open | open | audit/open | open | open | D-CC25 | C1 |
| `CC25.Edit.wb31` | CC25 | open | open | open | open | audit/open | open | open | D-CC25 | C1 |
| `CC25.HunspellService.morphos320` | CC25 | open | open | open | open | audit/open | open | open | D-CC25 | C1 |
| `CC25.Installer.morphos320` | CC25 | open | open | open | open | audit/open | open | open | D-CC25 | C1 |
| `CC26.HI.morphos320` | CC26 | open | open | open | open | required/open | open | open | D-CC26 | C1 |
| `CC26.RexxMast.morphos320` | CC26 | open | open | open | open | audit/open | open | open | D-CC26 | C1 |
| `CC26.RX.morphos320` | CC26 | open | open | open | open | required/open | open | open | D-CC26 | C1 |
| `CC26.RXC.morphos320` | CC26 | open | open | open | open | required/open | open | open | D-CC26 | C1 |
| `CC26.RXCmd.morphos320` | CC26 | open | open | open | open | required/open | open | open | D-CC26 | C1 |
| `CC26.RXLIB.morphos320` | CC26 | open | open | open | open | required/open | open | open | D-CC26 | C1 |
| `CC26.RXSET.morphos320` | CC26 | open | open | open | open | required/open | open | open | D-CC26 | C1 |
| `CC26.TCC.morphos320` | CC26 | open | open | open | open | required/open | open | open | D-CC26 | C1 |
| `CC26.TCO.morphos320` | CC26 | open | open | open | open | required/open | open | open | D-CC26 | C1 |
| `CC26.TE.morphos320` | CC26 | open | open | open | open | required/open | open | open | D-CC26 | C1 |
| `CC26.TS.morphos320` | CC26 | open | open | open | open | required/open | open | open | D-CC26 | C1 |
| `CC27.Automator.morphos320` | CC27 | open | open | open | open | audit/open | open | open | D-CC27 | C1 |
| `CC27.LuaX.morphos320` | CC27 | open | open | open | open | audit/open | open | open | D-CC27 | C1 |
| `CC28.ID.morphos320` | CC28 | open | open | open | open | required/open | open | open | D-CC28 | C1 |
| `CC28.IfConfig.morphos320` | CC28 | open | open | open | open | audit/open | open | open | D-CC28 | C1 |
| `CC28.Login.morphos320` | CC28 | open | open | open | open | audit/open | open | open | D-CC28 | C1 |
| `CC28.NetConfig.morphos320` | CC28 | open | open | open | open | audit/open | open | open | D-CC28 | C1 |
| `CC28.NetworksHelper.morphos320` | CC28 | open | open | open | open | audit/open | open | open | D-CC28 | C1 |
| `CC28.Offline.morphos320` | CC28 | open | open | open | open | required/open | open | open | D-CC28 | C1 |
| `CC28.Online.morphos320` | CC28 | open | open | open | open | required/open | open | open | D-CC28 | C1 |
| `CC28.Passwd.morphos320` | CC28 | open | open | open | open | required/open | open | open | D-CC28 | C1 |
| `CC28.ShowInterface.morphos320` | CC28 | open | open | open | open | audit/open | open | open | D-CC28 | C1 |
| `CC28.WhoAmI.morphos320` | CC28 | open | open | open | open | audit/open | open | open | D-CC28 | C1 |
| `CC29.ARP.morphos320` | CC29 | open | open | open | open | audit/open | open | open | D-CC29 | C1 |
| `CC29.AskHost.morphos320` | CC29 | open | open | open | open | required/open | open | open | D-CC29 | C1 |
| `CC29.OFDNS.morphos320` | CC29 | open | open | open | open | audit/open | open | open | D-CC29 | C1 |
| `CC29.OFHTTP.morphos320` | CC29 | open | open | open | open | audit/open | open | open | D-CC29 | C1 |
| `CC29.Ping.morphos320` | CC29 | open | open | open | open | audit/open | open | open | D-CC29 | C1 |
| `CC29.Route.morphos320` | CC29 | open | open | open | open | audit/open | open | open | D-CC29 | C1 |
| `CC29.SetClockNTP.morphos320` | CC29 | open | open | open | open | audit/open | open | open | D-CC29 | C1 |
| `CC29.TraceRoute.morphos320` | CC29 | open | open | open | open | audit/open | open | open | D-CC29 | C1 |
| `CC29.WakeOnLAN.morphos320` | CC29 | open | open | open | open | audit/open | open | open | D-CC29 | C1 |
| `CC30.Smb2FS.morphos320` | CC30 | open | open | open | open | audit/open | open | open | D-CC30 | C1 |
| `CC30.SmbFS.morphos320` | CC30 | open | open | open | open | audit/open | open | open | D-CC30 | C1 |
| `CC30.Ssh2FS.morphos320` | CC30 | open | open | open | open | audit/open | open | open | D-CC30 | C1 |
| `CC30.tcpdump.morphos320` | CC30 | open | open | open | open | audit/open | open | open | D-CC30 | C1 |
| `CC31.Bz2.morphos320` | CC31 | open | open | open | open | required/open | open | open | D-CC31 | C1 |
| `CC31.LhA.morphos320` | CC31 | open | open | open | open | audit/open | open | open | D-CC31 | C1 |
| `CC31.LZMADec.morphos320` | CC31 | open | open | open | open | audit/open | open | open | D-CC31 | C1 |
| `CC31.LZMAInfo.morphos320` | CC31 | open | open | open | open | audit/open | open | open | D-CC31 | C1 |
| `CC31.OFArc.morphos320` | CC31 | open | open | open | open | audit/open | open | open | D-CC31 | C1 |
| `CC31.OFHash.morphos320` | CC31 | open | open | open | open | audit/open | open | open | D-CC31 | C1 |
| `CC31.UnRAR.morphos320` | CC31 | open | open | open | open | audit/open | open | open | D-CC31 | C1 |
| `CC31.XZ.morphos320` | CC31 | open | open | open | open | audit/open | open | open | D-CC31 | C1 |
| `CC31.XZDec.morphos320` | CC31 | open | open | open | open | audit/open | open | open | D-CC31 | C1 |
| `CC32.Exe2Arc.morphos320` | CC32 | partial | partial | partial | open | audit/open | open | open | D-CC32 | XA2 |
| `CC32.XAD2LhA.morphos320` | CC32 | open | open | open | open | audit/open | open | open | D-CC32 | C1 |
| `CC32.XADLibInfo.morphos320` | CC32 | open | open | open | open | audit/open | open | open | D-CC32 | C1 |
| `CC32.XADList.morphos320` | CC32 | open | open | open | open | audit/open | open | open | D-CC32 | C1 |
| `CC32.XADUnDisk.morphos320` | CC32 | open | open | open | open | audit/open | open | open | D-CC32 | C1 |
| `CC32.XADUnFile.morphos320` | CC32 | open | open | open | open | audit/open | open | open | D-CC32 | C1 |
| `CC32.XADUnTar.morphos320` | CC32 | open | open | open | open | audit/open | open | open | D-CC32 | C1 |
| `CC33.AddUSBClasses.morphos320` | CC33 | open | open | open | open | audit/open | open | open | D-CC33 | C1 |
| `CC33.AddUSBHardware.morphos320` | CC33 | open | open | open | open | audit/open | open | open | D-CC33 | C1 |
| `CC33.DRadioTool.morphos320` | CC33 | open | open | open | open | audit/open | open | open | D-CC33 | C1 |
| `CC33.PenCamTool.morphos320` | CC33 | open | open | open | open | audit/open | open | open | D-CC33 | C1 |
| `CC33.PsdStackloaderToMOSPrefs.morphos320` | CC33 | open | open | open | open | audit/open | open | open | D-CC33 | C1 |
| `CC33.RocketTool.morphos320` | CC33 | open | open | open | open | audit/open | open | open | D-CC33 | C1 |
| `CC33.SonixCamTool.morphos320` | CC33 | open | open | open | open | audit/open | open | open | D-CC33 | C1 |
| `CC33.USBDevLister.morphos320` | CC33 | open | open | open | open | audit/open | open | open | D-CC33 | C1 |
| `CC33.USBErrorLog.morphos320` | CC33 | open | open | open | open | audit/open | open | open | D-CC33 | C1 |
| `CC34.AddAudioModes.morphos320` | CC34 | open | open | open | open | audit/open | open | open | D-CC34 | C1 |
| `CC34.Play.morphos320` | CC34 | open | open | open | open | required/open | open | open | D-CC34 | C1 |
| `CC34.PlayMidi.morphos320` | CC34 | open | open | open | open | audit/open | open | open | D-CC34 | C1 |
| `CC34.SetMixer.morphos320` | CC34 | open | open | open | open | required/open | open | open | D-CC34 | C1 |
| `CC35.Battery.morphos320` | CC35 | open | open | open | open | required/open | open | open | D-CC35 | C1 |
| `CC35.Eject.morphos320` | CC35 | open | open | open | open | audit/open | open | open | D-CC35 | C1 |
| `CC35.HFSSetMacBoot.morphos320` | CC35 | open | open | open | open | audit/open | open | open | D-CC35 | C1 |
| `CC35.IDEStandby.morphos320` | CC35 | open | open | open | open | audit/open | open | open | D-CC35 | C1 |
| `CC35.PCIScan.morphos320` | CC35 | open | open | open | open | audit/open | open | open | D-CC35 | C1 |
| `CC35.PCIWrite.morphos320` | CC35 | open | open | open | open | audit/open | open | open | D-CC35 | C1 |
| `CC35.PowerMac7FanControl.morphos320` | CC35 | open | open | open | open | audit/open | open | open | D-CC35 | C1 |
| `CC35.PowManTool.morphos320` | CC35 | open | open | open | open | audit/open | open | open | D-CC35 | C1 |
| `CC35.ShowCGXConfig.morphos320` | CC35 | open | open | open | open | audit/open | open | open | D-CC35 | C1 |
| `CC36.FSTest.morphos320` | CC36 | open | open | open | open | audit/open | open | open | D-CC36 | C1 |
| `CC36.HDMBRClear.morphos320` | CC36 | open | open | open | open | required/open | open | open | D-CC36 | C1 |
| `CC36.HDRead.morphos320` | CC36 | open | open | open | open | required/open | open | open | D-CC36 | C1 |
| `CC36.HDTest.morphos320` | CC36 | open | open | open | open | audit/open | open | open | D-CC36 | C1 |
| `CC36.HDWrite.morphos320` | CC36 | open | open | open | open | required/open | open | open | D-CC36 | C1 |
| `CC36.MemTest.morphos320` | CC36 | open | open | open | open | required/open | open | open | D-CC36 | C1 |
| `CC37.PFSDiskValid.morphos320` | CC37 | open | open | open | open | audit/open | open | open | D-CC37 | C1 |
| `CC37.PFSFormat.morphos320` | CC37 | open | open | open | open | audit/open | open | open | D-CC37 | C1 |
| `CC37.PFSList.morphos320` | CC37 | open | open | open | open | audit/open | open | open | D-CC37 | C1 |
| `CC37.PFSMakeRollover.morphos320` | CC37 | open | open | open | open | audit/open | open | open | D-CC37 | C1 |
| `CC37.PFSSetDeldir.morphos320` | CC37 | open | open | open | open | audit/open | open | open | D-CC37 | C1 |
| `CC37.PFSSetFileNameSize.morphos320` | CC37 | open | open | open | open | audit/open | open | open | D-CC37 | C1 |
| `CC37.PFSSetRollover.morphos320` | CC37 | open | open | open | open | audit/open | open | open | D-CC37 | C1 |
| `CC38.ClearRAMDebugLog.morphos320` | CC38 | open | open | open | open | required/open | open | open | D-CC38 | C1 |
| `CC38.Debug.morphos320` | CC38 | open | open | open | open | required/open | open | open | D-CC38 | C1 |
| `CC38.GetRAMDebugLog.morphos320` | CC38 | open | open | open | open | required/open | open | open | D-CC38 | C1 |
| `CC38.SegTracker.morphos320` | CC38 | open | open | open | open | audit/open | open | open | D-CC38 | C1 |
| `CC38.Trance.morphos320` | CC38 | open | open | open | open | audit/open | open | open | D-CC38 | C1 |

## Media closure and explicit discrepancy

| Closure ID | Owner | Status / next evidence |
| --- | --- | --- |
| WB31-DISKS-3-6 | CC00 | open: admit and inventory original Extras, Fonts, Locale and Storage disks. |
| WB31-MACHINE-VARIANTS | CC00 | open: reconcile original 3.1 machine-specific distribution variants. |
| WB31-INSTALLED-METADATA | CC00/CC06 | open: observe a clean original installation, placement, flags and installer/resident lifecycle. |
| MORPHOS320-INSTALLED-OVERLAY | CC00/CC06 | open: observe installed SYS:C/MOSSYS:C lookup, assigns, filtering and resulting Amiga flags. |
| COMMAND-RUNTIME-CONTRACTS | CC01/CC43 | open: complete reference options/behavior and native comparisons per applicable profile. |
| FREEZE-INDEX-MEDIA | CC39 | open: documented/historical Freeze is absent from the selected 3.20 ISO tree. Preserve and resolve the discrepancy; it is not counted in the required 200/246 seed. |

Discrepancy identity: `CC39.Freeze.morphos320.discrepancy`. It is neither a
qualified exclusion nor a shipping command. `HunspellService` is already a
required observed media command and remains in the CC25 row above.

The snapshot is valid only as a progress ledger, not a final-completion signal.
The inventory tool's strict completion mode deliberately fails while these
closure items remain open.

### CP8 follow-up: aligned Copy metadata (2026-09-05)

Corrected the normal-command FIB BPTR alignment defect (workspace4772, FIB2440). The rebuilt command passes333 native invocations and original-DOS CLONE / NOPRO COM DATES metadata plus independent payload/readback checks. See progress-log.md and artifacts/copy-boot-fixture/verified-metadata-aligned.json. Earlier binaries and receipts remain historical; their metadata values do not prove parity. No full command gate closes from these bounded cases; shipping remains0/200.

### CP8 follow-up: middle-write failure (2026-09-05)

Native full-command qualification now passes339 invocations, including first-success/middle-short-write/third-source continuation (WARN5) and ERRWARN stop (ERROR10) on all three CPUs. See artifacts/copy-middle-write-qualified/qualification.json. These controlled public-vector cases do not close original-runtime or shipping gates.

### CP8 follow-up: partially written destinations (2026-09-06)

Native qualification passes345 invocations. The middle-source short-write cases now model accepted destination bytes and require their deletion, with prior successful output preserved and ERRWARN stopping later output. See artifacts/copy-partial-write-qualified/qualification.json. Original-runtime partial-write and shipping gates remain open.

### CP8 follow-up: denied partial-file cleanup (2026-09-06)

Native qualification passes351 invocations, including retained partial-file state when cleanup deletion fails, transfer-error restoration, WARN continuation and ERRWARN stop. See artifacts/copy-cleanup-denied-qualified/qualification.json. Original-runtime and shipping gates remain open.

### CP8 follow-up: visible partial-write errors (2026-09-06)

Native qualification passes357 invocations, including exact command-generated failure output, DOS fault-code/order and non-QUIET WARN continuation/ERRWARN stop. DOS-rendered fault text remains a supplied fixture placeholder. See artifacts/copy-visible-partial-qualified/qualification.json. No shipping gate closes.

### CP8 follow-up: cancellation during transfer (2026-09-06)

Native qualification passes363 invocations, including Ctrl-C after accepted middle-file bytes, partial-output removal and prevention of later copies. See artifacts/copy-active-cancel-qualified/qualification.json. This remains supplied-signal evidence; original-runtime and shipping gates remain open.

### CC10 follow-up: Workbench Which directory filtering (2026-09-27)

The Workbench candidate now filters directories using DOS `Examine` while
preserving assign-root and regular-file results. Eight new bounded guest
pairs match original output, return and caller IoErr for `C:Which S`,
`C:Which S/`, `C:Which S/Startup-Sequence`, `C:Which C:`, `C:Which SYS:`,
`C:Which SYS:S`, `C:Which SYS:S/Startup-Sequence` and `C:Which SYS:S/..`.
See the `Which` contract and progress log for the individual receipts. Its
resident 68000/020/040 qualification passes 31 vectors per CPU at
`artifacts/which-wb31-native-20260927-sys-parent-v10/qualification.json`.
These pairs are separate from the earlier 32-case matrix; old failing
directory-positive captures remain negative controls. Other command
semantics and all shipping evidence remain open: 0/200 commands and 0/246
profiles are shipping-qualified.

### CC10 follow-up: Workbench Which cross-profile rebuild identity (2026-09-27)

The later MorphOS alias-provider edit changed the shared source build identity.
The Workbench entry was requalified for 31 supplied vectors per CPU on
68000/020/040 at
`artifacts/which-wb31-native-20260927-findvar-regression-v1/qualification.json`;
the 68000 HUNK is 6,308 bytes with SHA-256
`cb7a3a3aa26e1d12381da5280f7281265ad5b0e5b52648f2f8d94c8918ce7294`. One fresh
guest comparison for `C:Which SYS:S/..` matches empty output, return 5 and
caller IoErr 205 at
`artifacts/workbench31-guest-command-which-crossprofile-current-candidate-20260927-v1/effect-comparison.json`.
The 32-case matrix and other directory-filter comparisons remain tied to their
earlier HUNK identities. This is one bounded guest-parity case, not a full
Workbench parity result.

### CC18 follow-up: MorphOS ChangeTaskPri PROCESS 0 selector (2026-09-28)

MorphOS `ChangeTaskPri` now has a supplied-DOS vector proving that an explicit
`PROCESS 0` reaches `FindTaskByPID(0)` under Forbid and applies the new priority
to the returned current task, distinct from the omitted-PROCESS path. The
resident 68000/020/040 receipt passes 15 vectors per CPU at
`artifacts/changetaskpri-morphos-native-20260928-pid-zero-v1/qualification.json`.
This does not close guest parity, PID identity/reuse/liveness, PURE/resident,
rights or package gates. Shipping remains 0/200 commands and 0/246 profiles.
The Workbench 3.1 counterpart separately verifies that explicit `PROCESS 0`
stays on classic `FindCliProc(0)` and does not call the MorphOS PID vector;
its 14-vector-per-CPU resident receipt is
`artifacts/changetaskpri-wb31-native-20260928-process-zero-v1/qualification.json`.
The original Workbench guest and current candidate also match this exact
invocation's output, return 20, and caller IoErr 0 at
`artifacts/workbench31-guest-command-changetaskpri-process-zero-candidate-20260928-v1/comparison-output-result-ierr.json`;
the comparison makes no task-effect claim because a pre-System owner sample
was unavailable.

### CC18 follow-up: Workbench Break parser edges (2026-09-28)

Original and candidate Workbench guests match for `C:Break ?` and
`C:Break 1 Z`. The first is a required-argument failure (return 20, caller
IoErr 116); the second is a wrong-number-of-arguments failure (return 20,
caller IoErr 118). Both exact output/result/IoErr receipts are linked in the
Break contract. These do not establish MorphOS help parity or full command
qualification.

### CC18 follow-up: Workbench ChangeTaskPri parser edges (2026-09-28)

Original and candidate Workbench guests match for `C:ChangeTaskPri ?` and
`C:ChangeTaskPri 1 Z`: the former returns 20 with caller IoErr 116 for the
missing required priority; the latter returns 20 with IoErr 118 for the extra
argument. Exact output/result/IoErr comparisons are linked from the
ChangeTaskPri contract. These do not establish MorphOS extended-help behavior
or full profile qualification.





