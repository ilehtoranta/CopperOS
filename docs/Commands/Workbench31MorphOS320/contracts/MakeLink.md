# MakeLink contract

Profiles: wb31 and morphos320. Goal steps: CC01 and CC12. Recorded:
2026-09-04.

Current checkpoint (2026-09-08): both profiles have independent native bodies.
Workbench has bounded original-command comparisons for actual ReadArgs options,
help continuation/EOF, rendered diagnostics, final process results and handler
effects. Separate versioned development builds now have structural preservation,
two-build local reproducibility and native-vector evidence, refreshed after the
original FileHandle ABI correction; older SDK-bound builds remain historical.
The versioned 68000 image has original-DOS/Version-query evidence and bounded
native CopperStart DOS execution using the production host filesystem with
fixture Exec. A separate managed test verifies the production owner queues;
these do not combine into a full native Shell or system boot.
Current binaries and reports are linked below.
MorphOS packed-original correspondence
remains open. Neither profile has complete shipping or PURE admission.
The initial discovery paragraphs below are historical, not the current backlog.

The selected Workbench 3.1 v40.42 `C/MakeLink` HUNK is 700 bytes with SHA-256
`c24b0af713e6f3a252d964c4545da29535bf6652394879a7dc4c57fdce7c02de`, and
has version tag `makelink 37.4 (6.5.91)` at byte offset 397. Its raw-string
scan finds this candidate at byte offset 370:

```text
FROM/A,TO/A,HARD/S,FORCE/S
```

The candidate may identify two required names and HARD/FORCE switches, but it
does not prove `ReadArgs` use, the default link kind, the meaning or precedence
of either switch, target locking rules, diagnostics, result/IoErr, or cleanup.
It is not safe to infer newer release behavior from this string alone.

The observed MorphOS 3.20 `MorphOS/C/MakeLink` is a 1,708-byte packed native
member, SHA-256
`be646e7c34e3aaa1d4340588b4499393dd02c3d9f507b90a952d034459e21cf7`, with
version tag `MakeLink 50.4 (01.05.2025)` at byte offset 1,668. Its packed form
has no safe template candidate in the inventory. The hash-bound official 3.20
source archive includes `c/makelink/makelink.c`, 3,971 bytes, SHA-256
`7138b5b103a9936c2e0b2559b66d3f2f0ea63ce48c7977049073caf3fceca8a4`.
It invokes DOS `ReadArgs` with:

```text
FROM/A,TO/A,HARD/S,FORCE/S
```

Source inspection observes a soft call to public DOS `MakeLink`. HARD first
takes a shared public `Lock` on TO, allocates a public DOS FIB, examines the
lock, and rejects a directory without FORCE by writing `Hard-links to
directories require the FORCE keyword`. Otherwise it passes the target lock to
the hard `MakeLink` vector. It releases the FIB and target lock on each
post-lock exit. A lock failure writes TO and a fault with an empty header; FIB
and vector failures print a `MakeLink` fault; the source leaves an Examine
failure without an added diagnostic. It frees ReadArgs storage before closing
DOS and does not impose a final `SetIoErr` policy. Its own source retains a
circular-link check as an unresolved warning.

The selected MorphOS installer script contains an observed `P` addition for
MakeLink at `hdinstall.fixc` line 43. That establishes required-pure design
intent, not installed flags, resident table behavior, or replacement
qualification. Workbench's P bit is clear in the observed ADF header but this
does not establish a non-pure design.

`NativeMorphOSMakeLinkCommand` independently implements the observed public
DOS path with invocation-owned parser result storage. The `LockRaw` SDK alias
preserves the same public Lock vector while avoiding nullable runtime support
in a resident caller. Its private entry has nine reachable methods and passes
eleven supplied parser/link/lock/FIB vectors on each target CPU, including soft
and hard links, forced and rejected directories, failure branches, and
interleaved calls. It has no managed runtime features/helpers, external targets,
exception regions, fatal fault sites, leaked resources, or shared-image writes.
The hash-bound resident HUNK receipts are: 68000, 2,580 bytes,
`98980b5064e52cfa1f39c29a20aafe35ebe4d6d5bd47f9759933e0dc220bc442`; 68020,
2,580 bytes, `7104b59d21ae324ea5116ae77ba63158a2247ffb6ca04894a57040d97f23edf8`;
and 68040, 2,580 bytes with the same SHA-256.

The source does not prove packed correspondence, Workbench behavior, source
reuse rights, actual handler/link effects, installed metadata, package
placement, or parity. Controlled disposable-reference captures remain required
for cross-volume, unsupported-handler, malformed, Ctrl-C, and final
result/IoErr behavior.


### Native hard-link failure coverage (2026-09-06)

The current native suite passes13 cases perCPU (39 total), including supplied
cross-volume215 and forced-directory link203 errors. Assertions now verify
fault count/error/header and diagnostic-before-FIB-release, FIB-before-lock,
and lock-before-parser cleanup. The shared classic FIB alignment guard passes.
Receipts: artifacts/makelink-hard-failure-qualified/qualification.json and
perCPU runtime reports. These do not prove original handler effects, packaged
command startup, Workbench grammar or shipping parity.

### Startup coverage (2026-09-06)

The existing standard-signature native entry now passes five additional startup cases perCPU: missing DOS, Workbench with/without DOS, negative length and null entry buffer. No body allocation/parsing/link operation occurs; Workbench reply and library ownership remain checked. Total54 native cases pass. See artifacts/makelink-startup-qualified/qualification.json. These are controlled startup assertions, not original-command startup parity.

### Original-DOS hard-link execution (2026-09-06)

The compiled candidate now creates a RAM hard link through original Kickstart3.1 DOS using real ReadArgs, target Lock/Examine and MakeLink. Original Type reads the exact18-byte source payload through the alias. Parser/FIB/lock ownership and unchanged resident image are observed. See artifacts/copy-boot-fixture/verified-makelink-hard.json. This is a forced-PURE disposable derivative, not installed flags, all-path residency or full command/profile parity.

### Original-DOS resident failure recovery (2026-09-06)

The same loaded segment now passes hard-link success, duplicate alias failure
(error203/return20), and a second alias success: returns0/20/0. Every invocation
releases its exact FIB, target lock, parser and library lease, with one matched
workspace allocation/free and no image writes. Original Type reads18 exact
payload bytes from each alias after successful resident removal and segment
free. The verifier checks the complete three-invocation trace without rewriting
it as separate successful runs; nine targeted negative controls are rejected.
Evidence: artifacts/copy-boot-fixture/verified-makelink-recovery.json and
makelink-recovery-controls.json; verifier:
tools/Commands/verify_makelink_resident_recovery.py.
This qualifies sequential forced-PURE recovery only. Installed flags,
concurrency, directory FORCE, soft-link success and full profile parity remain
open. Original DOS soft mode separately returns NotImplemented236; this does
not remove MorphOS soft-link functionality from the required scope.

### Hard-link survival after source-name deletion (2026-09-06)

Original C:Delete successfully removes link-source after the three resident
invocations. Original Type then reads both aliases with exact18-byte payloads
and matching sizes. See artifacts/copy-boot-fixture/verified-makelink-identity.json
and makelink-identity-controls.json. The verifier's --source-deleted profile
requires deletion success and ordering; four negative controls pass. This is
original RAM handler deletion-survival evidence, not alias mutation parity or
all-handler coverage. No installed purity or full-command gate is closed.

### Original-DOS directory FORCE behavior (2026-09-06)

The same resident candidate returns20 without FORCE, prints the required line
and makes no link call; with FORCE it passes the directory lock to MakeLink
and returns0. Both calls clean up correctly. Original Type reads the child
payload through the directory alias specified in the hash-bound startup.
See artifacts/copy-boot-fixture/verified-makelink-directory.json and
makelink-directory-controls.json (five rejected mutations). MatchFirst input
text is not captured, so traversal-path identity also depends on the startup
fixture. This is candidate execution on original DOS, not original-command
parity, installed flags, cycle handling or all-handler qualification.

### Directory traversal path evidence strengthened (2026-09-06)

The passive observer now captures MatchFirst's D1 string. The unchanged
fixture rerun explicitly records RAM:dir-alias/payload with success, followed
by exact child readback. verified-makelink-directory-path.json supersedes the
older path-limited directory receipt for this claim. The verifier now rejects
missing text, traversal via the source directory and a different child; see
makelink-directory-path-controls.json. This closes the path-capture limitation
above, while the other command/profile and installed-purity gaps remain open.

### Workbench 3.1 runtime profile divergence (2026-09-06)

Executed the hash-verified original700-byte Workbench MakeLink under the same
original-DOS directory fixture, substituted at C:Ed in private media. Both
invocations preserve the resident image and return20/0 with zero direct Exec
allocations. Real ReadArgs uses the recorded template. Unlike the current
MorphOS-derived body, the Workbench reference emits
`Links to directories require use of the FORCE keyword` via VPrintf and calls
PrintFault with error0 on rejection. It also locks RAM: in addition to the
source directory, and its FreeArgs/UnLock order differs. Do not infer the
purpose or full semantics of the additional lock from this case alone.

Evidence: artifacts/copy-boot-fixture/makelink-wb31-directory-media.json,
makelink-wb31-directory.trx, makelink-wb31-directory-observations.json and
makelink-directory-profile-comparison.json. The reference binary remains in
private test data, not redistributable source or packaging. This is a bounded
reference observation; the existing MorphOS-derived body does NOT qualify the
Workbench profile. Next: establish Workbench destination-parent/cycle checks,
error and cleanup behavior, then implement its profile explicitly without
changing MorphOS semantics to match Workbench. Keep both profiles required.

### Workbench destination-parent reference (2026-09-06)

Original Workbench with HARD FORCE and FROM RAM:missing/dir-alias locks the
source directory, then attempts Lock(RAM:missing). Error205 stops the command
before MakeLink; PrintFault(205) precedes FreeArgs and return20. A second
invocation with FROM RAM:dir-alias succeeds. Receipt-bound verifier:
tools/Commands/verify_makelink_wb31_parent.py; passing evidence:
artifacts/copy-boot-fixture/verified-makelink-wb31-parent.json.

Static inspection of the hash-bound original HUNK identifies the directory
helper at code offset0x1fa: Examine, PathPart(-876), temporary truncation of
FROM at the returned path boundary (restored after Lock), SameLock(-420)
against successive ParentDir(-210) locks. Equal locks select the loop message;
all ancestor locks are released. These LVOs were checked against Sdk.Amiga.
This is static evidence; a runtime loop case is still required. The inspected
body does not read the HARD slot and always calls hard MakeLink; default-kind
behavior still needs runtime confirmation before implementation.

The failed-parent command lease contains no target UnLock before FreeArgs;
there is an additional UnLock outside the command lease. Do not equate
zero direct Exec allocations with complete DOS-lock ownership proof, or copy
an apparent original leak into the new command without resolving ownership.
Next: loop/default reference cases, then an explicit Workbench body preserving
its observable profile while retaining safe per-invocation ownership.

### Workbench loop/default confirmation and initial body (2026-09-06)

The original reference rejects FROM RAM:link-dir/loop TO RAM:link-dir HARD
FORCE before MakeLink, emits the loop format and returns20. The following
invocation omits HARD, locks the regular target, passes hard flag0 to MakeLink
and succeeds. See makelink-wb31-loop-default-observations.json and summary in
artifacts/copy-boot-fixture. This confirms the static profile distinction.

Added src/Commands/Native/Workbench31MakeLinkCommand.cs as a separate body,
using ReadArgs, public DOS locks/FIB/PathPart/SameLock/ParentDir/MakeLink and
Workbench diagnostics. It restores the invocation-owned FROM separator after
parent Lock and releases every acquired ancestor and target. It deliberately
owns error-path target cleanup rather than relying on process teardown; verify
observable error/return behavior while retaining resident ownership. The
MorphOS body is unchanged. The native project build passes, but this new body
has no qualified native entry, runtime suite or installation selection yet.
Next: add its native entry and original-profile cases, verify emitted HUNK
purity and concurrency, then integrate profile dispatch/packaging. Do not
count the new source or successful C# build as native or shipping completion.

### Workbench candidate native execution (2026-09-06)

Added Workbench31MakeLinkEntry and its source link in the native root project.
The reusable compile_workbench_makelink_native.ps1 passes for68000/68020/68040
with11 reachable methods and no runtime features/helpers, external targets,
exception regions or fatal sites. Entry startup qualification remains separate.

The68000 candidate passes a five-invocation original-DOS boot: directory
without FORCE20, forced directory0, loop20, missing parent20, default regular
hard link0. Both alias paths and18-byte payloads are observed directly. Every
invocation owns/frees one workspace and balances all observed target/ancestor
locks plus FIB/parser/library; resident image unchanged and removal/free pass.
See artifacts/copy-boot-fixture/verified-workbench-makelink-native.json and
workbench-makelink-native-controls.json (five rejected mutations). Default
HARD omission is still supplied by the hash-bound startup, while D3=0 is
observed at MakeLink. No full command, concurrency or installed flag gate is
closed. Next: parser/missing-target/link-failure cases, deeper ancestor walk,
startup parity and supplied-vector three-CPU execution, then packaging.

### Workbench error/recovery comparison (2026-09-06)

Original and candidate each pass the identical five-invocation startup:
success, missing TO, missing target, duplicate name, success. Both return
0/20/20/20/0; fault codes116/205/203, null fault headers, missing-target
VPrintf format and MakeLink call decisions match. Parser failure performs no
lock/link work. Both outputs read18 exact bytes after recovery. Candidate
locks, FIBs and parser allocations balance within each lease; the resident
image remains unchanged. Five negative controls reject altered error/cleanup
and diagnostic evidence. See verified-workbench-makelink-errors.json and
workbench-makelink-errors-controls.json in artifacts/copy-boot-fixture.

This compares fault inputs and format strings, not exact rendered diagnostic
bytes or every secondary-error timing point. It does not close startup,
concurrency, resource-exhaustion, deeper-ancestor or packaging gates. Production
HUNK unchanged. Next prioritize deeper ancestor traversal and native three-CPU
failure/concurrency qualification before shipping profile integration.

### Workbench three-CPU native suite (2026-09-06)

Added WorkbenchMakeLinkEntrySuite: eleven cases plus two interleaved calls per
CPU,39 total on68000/68020/68040. Coverage includes default/explicit hard mode,
three-level safe ancestors, loop at the third comparison, FORCE rejection,
missing parent/target, failed FIB allocation/Examine/MakeLink and parser error.
The fixture gives interleaved invocations distinct target/ancestor handles and
DOS bases; asserts exact comparisons/traversals, no double release, restored
FROM before parser release, diagnostic arguments, cleanup and unchanged image.

qualify_workbench_makelink_native.ps1 compiles and executes the suite; evidence:
artifacts/workbench-makelink-native-qualified/qualification.json and perCPU
runtime/static receipts. Existing MorphOS suite regresses54/54 under the changed
runner in workbench-makelink-native-distinct-locks/morphos-*.json. These are
supplied DOS vectors, not original-DOS concurrency or installed PURE proof.
Startup parity, actual deeper-ancestor reference runs, complete IoErr/output
coverage and packaging remain open.

### Workbench missing-DOS startup error corrected (2026-09-06)

The original entry's failed OpenLibrary branch stores122 in current process
pr_Result2. A new native startup test rejected the previous candidate (31337
stale error instead of122). Workbench31MakeLinkEntry now obtains its current
process through Exec.FindTask and writes only pr_Result2 when DOS cannot open;
Finish still owns Workbench reply and does not call unavailable DOS.

The suite adds five startup cases and an interleaved missing-DOS/success pair,
now20 cases perCPU/60 total. The missing-DOS assertion reads actual guest
process memory rather than the supplied DOS IoErr variable. The memory guard
allows only that invocation's four-byte Result2 field in explicitly declared
missing-DOS cases. A test-only HUNK moving the write to offset152 is rejected.
All60 pass; MorphOS regression remains54/54. Evidence:
artifacts/workbench-makelink-startup-owned-qualified/qualification.json,
perCPU runtime/static receipts, process-field-control.json and failure log.
Old candidate failure and first guard rejection remain preserved separately.

This fixes the statically observed missing-library profile; Workbench GUI and
invalid entry-buffer cases are candidate startup safety checks, not proof of
full original startup parity. New HUNK hashes supersede earlier candidates;
prior original-DOS receipts remain evidence for their older hashes only.
Original-DOS rerun of the new entry and packaging remain required.

### Current entry original-DOS rerun and development registration (2026-09-06)

The startup-fixed68000 binary (fd42d840165936c2dc4ff5980319b47d1e1dd2ccb6e466c48c03548c7ae1050c)
passes the original-DOS five-case fixture with returns20/0/20/20/0, balanced
resources, unchanged image and both18-byte readbacks. The new receipt is
verified-workbench-makelink-startup-fixed.json in artifacts/copy-boot-fixture;
older receipts remain bound to older binaries.

makelink-development-candidates.json records six separate profile/CPU entries
with binary and native-report hashes. It is explicitly development-only and
not part of the historical accepted build-manifest checkpoint. Workbench68000
also binds the new original-DOS receipt. Ledger rows now accurately mark
partial spec/options/semantics/native/differential progress; purity and package
gates remain open. No installed C:MakeLink or shipping claim follows.

### Original-DOS concurrent success/deep-loop rejection (2026-09-06)

Two original Shell processes overlap on the current Workbench68000 image:
foreground hard link succeeds while background loop rejection walks three
ParentDir steps. Both DOS library leases overlap, task-specific parser/FIB/
target/ancestor ownership balances, and image remains unchanged. After both
return, Resident removal frees the exact segment allocation; Type reads18
correct bytes through RAM:foreground. Verifier:
tools/Commands/verify_workbench_makelink_concurrent.py; passing receipt:
artifacts/copy-boot-fixture/verified-workbench-makelink-concurrent-readback.json.
Five negative controls reject absent overlap, same task, image mutation,
removal failure and wrong readback path.

The first fixture reached Wait1 without recording removal/readback inside the
fixed bound and was correctly rejected. The second uses an original source
Type operation before attempting removal; completion and removal are still
required from observed events, not assumed from that operation. This is one
original-runtime concurrency scenario, not all-path purity or installed flags.

### Workbench real ReadArgs option matrix (2026-09-08)

The current 3184-byte `fd42d840...` candidate and original 700-byte Workbench
command ran the identical 579-byte LF startup script under the same original
Kickstart DOS, RAM handler, observer and emulator. Both completed all nine
invocations, with six successful hard-link calls. Five aliases were independently
read after successful resident removal and returned exact `option-payload\n`
bytes. Repeated HARD has a successful hard-link call in this fixture; an
independent readback for that sixth alias is not claimed.

| Case | Original and candidate result / final IoErr | Observed binding |
| --- | --- | --- |
| Positional FROM and quoted-space TO; no switches | 0 / 205 | Hard link using the complete target name |
| Reordered TO/FROM keywords with HARD | 0 / 205 | Correct source and quoted target |
| FROM=value and TO="quoted value" with HARD | 0 / 205 | Same values and hard-link mode |
| FORCE with a regular-file target | 0 / 205 | Accepted, successful hard link |
| Repeated HARD switch | 0 / 205 | Accepted, successful hard link |
| Repeated FROM keyword | 20 / 118 | ReadArgs fails; no Lock/MakeLink or FreeArgs |
| Extra unknown argument | 20 / 118 | Same parser isolation |
| Missing required TO | 20 / 116 | Same parser isolation |
| Escaped quote inside quoted FROM | 0 / 205 | Literal quote retained; exact alias readback |

The final secondary value is read from the owning process when the public
RunCommand vector returns. Successful MakeLink does not imply IoErr0 on this
provider. Do not add unconditional success-time clearing. Input grammar is still
owned by actual DOS ReadArgs, not a command-private parser.

Evidence: `artifacts/copy-boot-fixture/verified-workbench-makelink-options-20260908.json`;
script `workbench-makelink-options-startup-20260908.txt` in the same directory.
Help continuation and EOF through redirected input are covered separately below.
Interactive console and cancellation, empty names, Latin-1, option-like names and
the remaining path forms are not covered by these nine rows.

### Workbench ReadArgs help continuation and EOF (2026-09-08)

The original 700-byte command and the unchanged 3184-byte candidate
`fd42d840165936c2dc4ff5980319b47d1e1dd2ccb6e466c48c03548c7ae1050c`
ran a second identical RAM-only fixture. Original Shell supplies `?` to the
known ReadArgs template, redirects input from a prepared file, and redirects
output to a separate file. The original boot ran first to establish the actual
EOF result. Neither boot changed the existing 32 by 250000-instruction bound,
observer, ROM, emulator, guest state or public-call execution policy.

| Case | Result / final IoErr | Exact output and effect |
| --- | --- | --- |
| Help followed by `FROM RAM:help-alias TO RAM:link-source` | 0 / 205 | `FROM/A,TO/A,HARD/S,FORCE/S: `, 28 bytes with trailing space; creates a hard link |
| Help with an empty redirected input file | 20 / 116 | `FROM/A,TO/A,HARD/S,FORCE/S: required argument missing\n`, 54 bytes; no target lock or mutation |

After both invocations return and resident removal succeeds, independent C:Type
runs read both input files, both output files, the alias and original source.
The empty input has affirmative EOF with IoErr 0; it is not inferred from a
missing read. Alias and source both contain exact `help-payload\n` bytes. Final
IoErr is sampled at public RunCommand return. The candidate's invocation-private
allocation, parser, target lock and FIB ownership balance; its shared image is
unchanged. The complete traces contain 639 original and 645 candidate events.

Evidence: `artifacts/copy-boot-fixture/verified-workbench-makelink-help-20260908.json`
(SHA-256 `9ff6ad3932df14400880aad15a2f8d3ddf1449d88970816103d60111440c5482`).
Its exact media, test, HUNK, observer, emulator, ROM, script and verifier inputs
are hash-bound. Twenty negative controls reject changed output/results, input
redirection, missing EOF, wrong link/content, parser/resource loss, failed removal,
changed bounds, overflow and mismatched terminal evidence; control report
`workbench-makelink-help-20260908-controls.json` has SHA-256
`948906249a7e023ab1158a8720eaa5a27a5ad8673f998f3be17a88eee88f8bec`.
Both reports are bound to the old 68000 development record. This closes these
redirected help/EOF observations, not real console interaction, signal handling,
full options/startup, universal stack or PURE admission.

### Workbench exact diagnostics and final secondary results (2026-09-08)

The existing five-case current-HUNK trace already captured pr_Result2 at public
RunCommand return. A new bound validator compares these values after command and
DOS cleanup: success205, parser116, missing-target205, duplicate203, recovery205.
Eight controls with mutually consistent corrupted TRX/observation pairs reject
wrong/missing values, ownership/lifetime and stack-request substitutions.

A separate paired boot redirects each invocation's output into a distinct RAM
file and independently sizes/reads those files after resident removal. The exact
original and candidate outputs match:

- Success and recovery: empty output files.
- Missing required argument: `required argument missing\n` (26 bytes).
- Missing target: `Can't find RAM:absent object not found\n` (39 bytes).
- Duplicate link: `object already exists\n` (22 bytes).

The redirected cases retain the same primary/secondary pairs as the unredirected
fixture. Twelve controls reject altered bytes/results, falsely empty files,
wrong readback identity/size, incomplete removal, overflow and failed test binding.
Evidence: `verified-workbench-makelink-secondary-20260908.json` and
`verified-workbench-makelink-output-20260908.json` under `artifacts/copy-boot-fixture`.
This closes those bounded observations; it does not prove all output/cleanup
failure combinations or all handler/startup paths.

### Actual command/DOS stack observation (2026-09-08)

A passive observer now records the first HUNK entry through its matching user-mode
return. It checks published task stack bounds at every same-task user sample;
RunCommand pre/postlude, other tasks and supervisor/interrupt samples are separate.
It records the requested size rather than guessing bounds from it. No guest
register, memory or execution policy was changed to obtain these measurements.

All five current Workbench MakeLink error/recovery invocations complete with a
published 4096-byte stack. The deepest observed SP is 896 bytes below its upper
bound (884 below HUNK entry), leaving 3200 bytes of observed SP headroom. HUNK-only
sample depths are152–156 bytes below entry, so the larger measurement includes
real DOS callees. There are no unproven, changed or outside-bound user samples.

This is SP sampling, not proof of all stack writes, allocation ownership,
intra-instruction/transient use, unsampled host gateway work or a universal
minimum stack. `minimumStackQualified` and shipping/P admission remain false.
Evidence: `verified-workbench-makelink-stack-20260908.json`. Twelve stack and
12 original/candidate option inspection controls pass in
`controls-workbench-makelink-stack-options-20260908.json`; these focused controls
do not claim full TRX-binding tamper coverage.

### Remaining Workbench acceptance work after this checkpoint

- [ ] Complete the remaining ReadArgs/lifecycle forms named above, including original launcher and signal behavior.
- [ ] Finish relevant handler/path/error branches and original cleanup/error-policy comparisons beyond the current ordinary/error/directory/loop cases.
- [ ] Extend the versioned-image native DOS/production-filesystem run and separate owner queue test to the required complete system launch and handler contract; their distinct scopes do not close the full runtime gate.
- [ ] Close original pure/resident classification and required lifecycle/ownership tests; retain the verified overlapping-call evidence already recorded.
- [ ] Establish the declared minimum stack across the completed contract, including real OS paths and bounds/write evidence as appropriate.
- [ ] Complete exact versioned-image runtime/dependency/version qualification, release admission, actual image staging and Amiga protection readback; the bounded metadata build below is complete but does not pass this gate.

A missing unrelated command family is not a barrier to closing this profile.
MorphOS retains its additional soft-link success, original packed-command
correspondence and profile/lifecycle requirements. The full 200-command goal and
all explicit gates remain unchanged.

### Separate versioned development images (2026-09-08)

`tools/Commands/append_hunk_version.py` accepts the compiler's bounded single
CODE HUNK subset, retaining every original CODE, relocation and symbol byte. It
appends a NUL-delimited `$VER: MakeLink 0.1 (8.9.2026) CopperOS wb31` identifier
and alignment padding, adding 48 bytes. Only the original allocation and CODE
length words change. This is the CopperOS development version, not a claim to be
the original Workbench version. Invalid/unsupported HUNK forms, existing version
tags, changed input hashes and overwrites are rejected.

`build_workbench_makelink_versioned.py` builds in two fresh managed/native output
directories. Both raw and tagged HUNKs are byte-identical between passes for each
CPU; each raw HUNK also equals the corresponding retained old candidate. The
three new images pass the existing 20-case native-vector suite each, including
interleaving, for 60 invocations without shared-image writes. The suite uses
16384-byte stacks and observes 164 written bytes; this does not qualify the
separately declared 4096-byte minimum. DOS 36 is required by the entry; Exec 36
is a declared platform floor obtained through the system base rather than an
OpenLibrary version check. Dependency, compiler, SDK, source snapshot, exact CLI,
native report and transform identities are recorded.

| CPU | Versioned bytes | Versioned SHA-256 |
| --- | --- | --- |
| 68000 | 3232 | `12d1f36504f5a0f7099bc419e4fffc5f43f138d7b4e8745a2ca907bcb73b5f45` |
| 68020 | 3272 | `f2ee57c9d4f79b971d98718ec1585b8cd9780b085699abd04f7aa808789c1fd8` |
| 68040 | 3228 | `34fdd4711ebd79aae834cc064c2cf142e7ec4581a96f9402c06a6df8c2eb566a` |

Current build evidence is `artifacts/workbench-makelink-filehandle-abi-20260908/qualification.json`
(SHA-256 `fcebfca783daef8febb3c93fac71f47fff48510693d9d700b44b356941b00305`).
It refreshes the corrected SDK binding and all 453 current source inputs while
producing identical HUNK bytes. The old SDK-bound
`artifacts/workbench-makelink-versioned-20260908/qualification.json`
(`111e349e2921c077decdcf21543f3c1e9dbc90673c0c6ca750103ae62f1fc08b`)
remains historical. See the [filesystem integration checkpoint](../copperstart-filesystem-integration.md)
for ABI details and separate native/owner execution scopes.
Independent direct byte preservation and the 12 existing structural tests are
recorded in `preservation-audit.json` in the old versioned directory (SHA-256
`0efabee4fe0434497279b0b9e276eff40a849bdb113e74c94a3f8017e88eb053`).
The three records live in
[makelink-versioned-development-candidates.json](../makelink-versioned-development-candidates.json),
separate from the six retained unversioned/profile records. Every record remains
development-only with no installed path, protection admission or shipping credit.

At this metadata-build checkpoint, independent bounded review found no concrete correctness defect in the supported
append/build path. Its limits remain explicit: a structural append cannot prove
runtime control-flow safety for arbitrary input; the observed source/MSBuild
inputs are not a hermetic cross-host closure; no old-HUNK real-DOS, help, stack or
concurrency evidence qualifies a larger versioned image by inheritance. Exact
versioned-image OS execution, Version query, full dependency/release admission,
minimum stack, residency and image/protection evidence remain required. Shipping
remains 0 of 200 commands and 0 of 246 profiles.

### Fresh versioned 68000 original-DOS and Version execution (2026-09-08)

The 3232-byte `12d1f36504f5a0f7099bc419e4fffc5f43f138d7b4e8745a2ca907bcb73b5f45`
image now has its own completed original-Kickstart-DOS fixture. Five MakeLink
invocations cover success, missing required argument, absent target, duplicate
destination and recovery. Their primary/final-secondary pairs are respectively
0/205, 20/116, 20/205, 20/203 and 0/205. Exact redirected diagnostic bytes, direct
allocation/lock/FIB cleanup, unchanged resident image and independent alias
contents after resident removal are verified. These are new-image observations,
not evidence copied from the unchanged CODE prefix or the older candidate.

The unchanged original C:Version 40.1 then inspects the selected versioned file.
Its original caller, file/load inspection, relocated image bytes and complete
independent RAM-output readback are checked. Version returns 0/0 and renders
exact `MakeLink 0.1\nCopperOS wb31\n` bytes, 27 in total. FULL does not print the
embedded date in this fixture; the date is established from the exact HUNK tag,
not claimed as rendered output. The complete trace contains 925 events within
the unchanged execution bound. No observer or emulator behavior was changed for
this fixture, and the licensed reference disk's other original files remain
unchanged in the disposable derivative.

Evidence in `artifacts/workbench-makelink-versioned-20260908/version-boot-complete/`:
`verified.json` has SHA-256
`88e1928b49755de17e5a364f22619bdcf9134f8ab999469a881678f82191ba0c`;
`controls.json` has SHA-256
`56d818673650dbcc29999bd43ba055c8af478d1a4b0e87ce23ac932f190e3261`.
Fourteen controls reject altered Version caller/file/image/output, MakeLink
content/errors/image, execution bounds and terminal/capture evidence. The twelve
semantic changes pass through the full verifier with mutually consistent
TRX/observations; failed-terminal and stale-capture controls are separate.
Both receipts attach only to the versioned 68000 development record. Other CPUs,
full options/startup, minimum stack, residency, dependency/release and staged
image/protection gates remain open.

### Historical unversioned native CopperStart DOS integration and shared fixes (2026-09-08)

The retained 3184-byte `fd42d840165936c2dc4ff5980319b47d1e1dd2ccb6e466c48c03548c7ae1050c`
command now executes five invocations through the installed native production
CopperStart DOS RunCommand, ReadArgs, formatting, mutation and packet-provider
bridge. Exec services and the bounded backing packet handler are fixtures;
this is not a full CopperStart boot or a production filesystem test. The native
DOS image hash is `fb618a907935b485a24034d15d3498fa0066fc9249ec0e303909971715ad9fb6`.

Execution exposed two shared implementation defects, repaired at their owners:

- `ExecRawDoFmtCore` stored a string length through an odd-address numeric byte
  field using a word access, causing an MC68000 address fault. String length now
  passes directly to string configuration, retaining lengths above 255 without
  that unaligned access.
- `DosErrorCore` now emits `required argument missing` for error 116 and the
  observed lowercase `object already exists` for error 203.

The five command results are 0/0, 20/203, 20/205, 20/116 and 0/0 for success,
duplicate, absent target, missing TO and recovery. Final success IoErr 0 here is
the bounded CopperStart provider's result; it is not substituted for the original
DOS fixture's observed 205. Both successful aliases retain the same underlying
file identity and exact `native-provider-payload\n` bytes through public DOS
reads after the original target name is deleted. The existing destination stays
unchanged, caller input/output survive, invocation allocations balance, and no
locks or files remain live. Command/DOS images and installed vectors remain
unchanged. The command uses a 16384-byte fixture stack; minimum-stack admission
does not follow.

The terminal result has six passing tests: one native integration test containing
those five command invocations, plus five aligned formatter tests for string
lengths 0, 1, 255, 300 and 1024. Evidence:
`artifacts/copperstart-makelink-native-integration/verified-final.json`, SHA-256
`90782dbb243afa83d5141c04b1ae4d7b0e5d3f42c657d5453fdb911fd7b44372`.
It binds the result, terminal test, loaded assemblies and changed source inputs.
This evidence attaches only to the retained unversioned 68000 record. It neither
qualifies the tagged image for CopperStart nor closes full system, filesystem,
minimum stack, PURE/resident or shipping requirements. Historical failed runs
remain retained. Qualified shipping remains 0/200 commands and 0/246 profiles.

### Versioned command with production filesystem and separate owner queues (2026-09-08)

The [filesystem integration checkpoint](../copperstart-filesystem-integration.md)
records the corrected byte-36 `fh_Arg1` ABI, 128 focused SDK/native checks,
and the fresh same-byte command builds above. The versioned 68000 command now
passes five native CopperStart DOS invocations with the actual production
configured filesystem: exact errors/diagnostics, source-name deletion,
independent alias readbacks and cleanup pass. Twelve tests pass in its suite;
Exec allocation/delivery/StackSwap remain fixtures. A separate one-test
`DosServices` owner run verifies two roots and six genuine guest queued packets
with exactly one reply per request and one free per lock. No combined Shell,
full native boot, complete runtime, minimum-stack, P/resident or shipping gate
is claimed. The linked checkpoint binds each receipt and its distinct scope.
