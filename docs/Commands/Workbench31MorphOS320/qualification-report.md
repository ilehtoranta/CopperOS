# Workbench 3.1 / MorphOS 3.20 command qualification

Started 2026-08-30; historical checkpoint updated 2026-09-08. The current
qualification checkpoint is 2026-09-28. Goal remains active. **No new shipping
command is qualified.**
This is an execution record for the stable
[goal plan](../../../Goals/WORKBENCH31_MORPHOS_C_COMMANDS_GOAL.md), not a reduced
replacement objective. Existing unrelated Shell/MUI goals retain their owners.

### 2026-09-28 managed and native foundation refresh

`dotnet test tests/Commands/CopperOS.Commands.Tests.csproj --configuration
Release --no-restore` passes **843 tests** with no failures or skips. The
separate foundation qualification passes nine HUNK/suite executions across
68000/020/040: 31 startup, 25 argument-boundary, and 32 I/O vectors per CPU
(264 total). It records one image load per CPU, no leaked resources, and zero
shared-image writes. The hash-bound receipt is
`tests/Commands.NativeRoot/bin/Release/net10.0/qualification/3e1bbbbf271c43288b90a1ec8a141256/qualification.json`.
It reports no original Kickstart/CopperStart execution, real DOS parser/I/O,
minimum-stack qualification, or PURE/shipping approval. It advances only the
native ABI fixture baseline, not any command profile's admission.

### 2026-09-26 managed regression baseline

`dotnet test tests/Commands/CopperOS.Commands.Tests.csproj --verbosity minimal`
passed all 704 tests with zero failures or skips on CopperOS HEAD
`5dc8e7dde1787b85ceebd1379587921b6bc01980`. This protects the existing Shell
and managed command layer; it does not qualify external C: command coverage or
native guest behavior. An isolated Shell static build is now available at
`artifacts/shell-native-isolated-20260926-v6/qualification.json`. It emits a
68000 HUNK and 020/040 assembler listings, but reports three fatal
machine-fault sites per target; the 020 listing shows three guarded
divide-by-zero `ILLEGAL` instructions in `StackCommand.WriteStackSize`. Its
divisor loop appears to make those traps unreachable, but no guest is executed.
Treat it as a static baseline only; no shipping count or per-command gate is
promoted.

### 2026-09-26 Workbench SetFont size-word parity

The selected SetFont HUNK stores the `SIZE/N` LONG's low word into
`TextAttr.ta_YSize` and compares that unsigned word with four. The candidate
now uses the same low-word comparison. Its refreshed resident fixture passes
33 supplied vectors per CPU on 68000/020/040, including low words 0, 4, 5 and
`0xFFFF` plus cleanup at each owned allocation failure, at
`artifacts/setfont-wb31-native-entry-20260926-allocation-cleanup-v19/qualification.json`.
Each image has eighteen reachable methods, zero managed allocations/runtime
features/helpers/external targets/exception regions/fatal sites, no resource
leaks, and no shared-image writes. This closes the candidate's parity with the
captured SIZE word operations; real ReadArgs text parsing and Workbench guest
diskfont/console behavior remain unverified. SetFont shipping, PURE/resident,
licensing, package, and differential gates remain open.

### 2026-09-26 Workbench SetFont font ownership

The hash-bound HUNK retains the `OpenDiskFont` result on its successful
no-window path and does not close it during shared cleanup. The candidate now
has an explicit fixture assertion for that behavior. Static disassembly also
shows some post-open failures leaving the font open; the candidate closes the
untransferred font on failure to avoid a leak. This is a deliberate ownership
difference whose guest-visible effect and actual diskfont reference count are
unknown until an original Workbench run. The audit is
`reference-captures/setfont-font-ownership-audit-20260926.json`; the refreshed
33-vector-per-CPU receipt is
`artifacts/setfont-wb31-native-entry-20260926-font-ownership-v20/qualification.json`.

### 2026-09-24 MorphOS Version Exec LibList lookup

After the DOS `FilePart`/Exec Resident lookup misses, MorphOS `Version` now
checks Exec's `LibList` under `Forbid`/`Permit` with Utility `Stricmp`, copies
the matched node name before unlocking, and supports numeric version
comparison and `FULL` date/extra output from the library `IdString`. A
caller-provided `utility.library` v37 lease closes on success and failure,
and the unavailable-library path returns the expected error. `RES` also
follows the source's `FilePart` basename behavior. The MorphOS resident fixture
passes 78 supplied vectors per CPU on 68000/020/040, with no leaked resources
or shared-image writes, at
`artifacts/version-morphos-native-20260924-liblist-v17/qualification.json`.
Workbench's separate resident candidate passes its 15-vector-per-CPU
regression at
`artifacts/version-wb31-native-20260924-liblist-regression-v4/`.

This closes only the loaded Exec `LibList` hit slice. The `MOSSYS:LIBS`/`LIBS:`
file candidates, `DeviceList`, `MOSSYS:DEVS`/`DEVS:` candidates, terminal
direct-file/volume resolution and final command-segment priority remain open,
as do original guest parity, packed correspondence, PURE/resident lifecycle,
licensing, installation and package admission. Shipping remains 0/200
commands and 0/246 profiles.

### 2026-09-23 Workbench Which internal switch matrix

The Workbench resident candidate now exercises the eight classic switch
combinations for the captured internal-only `CD` lookup, including output and
result precedence. Both profile fixtures also traverse a two-node CLI path
list after a resident match and preserve duplicate `ALL` output ordering. The
Workbench and MorphOS candidates each pass 17 supplied DOS/Exec vectors per
CPU on resident 68000/020/040 HUNKs (51 per profile), with no shared-image
writes or fixture resource leaks. Receipts:
`artifacts/which-wb31-native-20260923-all-order-v1/qualification.json` and
`artifacts/which-morphos-native-20260923-all-order-v1/qualification.json`.
This remains post-ReadArgs fixture evidence; the other lookup categories, exact
parser and full original-guest behavior remain unverified.

### 2026-09-23 MorphOS DOSList volume verbose rows

MorphOS `DOSList VOLUMES VERBOSE` now captures the source's volume stamp,
formats it with public DOS `DateToStr`, reports illegal dates with the original
stamp values, prints optional disk type, and lists lock paths in source order.
It snapshots all strings and lock rows while the volume read lock is held and
renders them after unlock.

The receipt
`artifacts/doslist-morphos-native-20260923-volume-v1/qualification.json`
passes 34 supplied vectors per CPU on 68000/020/040 (102 total), with 34
reachable methods and no managed allocation sites, runtime features/helpers,
external native targets, exception regions, fatal fault sites, resource leaks
or shared-image writes. Original guest comparison, PURE/resident lifecycle,
source reuse rights, package admission and differential behavior remain open.

### 2026-09-23 MorphOS DOSList assign target and lock-list rows

MorphOS `DOSList` now implements the source-observed AssignNode output branches:
deferred and null targets, optional message-port and lock rows, verbose type,
and source-ordered linked assign locks with optional FileLock details. It copies
lock names and data while holding the DOS read lock and prints after unlock.

The receipt
`artifacts/doslist-morphos-native-20260923-assign-v1/qualification.json`
passes 30 supplied vectors per CPU on 68000/020/040 (90 total), with 32
reachable methods and no managed allocation sites, runtime features/helpers,
external native targets, exception regions, fatal fault sites, resource leaks
or shared-image writes. Volume verbose details, original guest comparison,
PURE/resident lifecycle, source reuse rights, package admission and
differential behavior remain open.

### 2026-09-23 MorphOS DOSList device verbose attributes

MorphOS `DOSList VERBOSE` now captures the source's device handler,
stack/priority/segment/global-vector, serial ID, startup-message, environment,
control and startup values through public `GetDosObjectAttrTagList` calls.
The candidate copies returned strings into invocation-owned storage while the
DOS read lock is held, then emits the captured rows after unlocking in the
source order. Optional attribute failures omit only their corresponding rows.

The receipt
`artifacts/doslist-morphos-native-20260923-verbose-device-v1/qualification.json`
passes 24 supplied vectors per CPU on 68000/020/040 (72 total), with 28
reachable methods and no managed allocation sites, runtime features/helpers,
external native targets, exception regions, fatal fault sites, resource leaks
or shared-image writes. Volume and assign verbose details, original guest
comparison, PURE/resident lifecycle, source reuse rights, package admission
and differential behavior remain open.

### 2026-09-23 MorphOS DOSList public attributes and mounted-state rows

The resident MorphOS `DOSList` candidate now queries node names and message
ports independently through public `GetDosObjectAttrTagList`. Attribute-query
failure follows the source's fallback node output and mounted-row behavior.
Embedded and signal message ports are resolved with `Exec.TypeOfMem` and
public task/message-port fields; no provider-private structure is used. The
fixture covers attribute failures, missing-name filtering, mounted process
rows and unmounted nodes in addition to the existing list/filter/startup/error
paths.

The receipt
`artifacts/doslist-morphos-native-20260923-object-attrs-v1/qualification.json`
passes 23 supplied vectors per CPU on 68000/020/040 (69 total), with 19
reachable methods and no managed allocation sites, runtime features/helpers,
external native targets, exception regions, fatal fault sites, resource leaks
or shared-image writes. Provider-specific verbose detail rows, original guest
comparison, PURE/resident lifecycle, source reuse rights, package admission
and differential behavior remain open.

### 2026-09-23 List recursive ALL resident qualification

MorphOS and Workbench `List` now use the public DOS `AnchorPath` recursion
protocol for `ALL`: set `APF_DODIR` on a discovered directory and clear
`APF_DIDDIR` when returning from it. The shared worker maps Workbench's `ALL`
from result slot 15 and MorphOS's from slot 18. Its nested-directory fixture
checks descent, return-marker suppression, and continuation through remaining
entries. MorphOS passes 31 supplied vectors per CPU and Workbench passes 32 on
68000/020/040 in
`artifacts/cc11-list-morphos-native-20260923-all-v1/qualification.json` and
`artifacts/cc11-list-wb31-native-20260923-all-v1/qualification.json`.
This does not establish original guest ordering, displayed paths, link policy,
or recursive result parity.

### 2026-09-23 List date filters and QUICK precedence

This is the date-range checkpoint; the recursive `ALL` qualification above
records the later implementation and fixture coverage for recursion.

Both `List` resident candidates now parse `SINCE` and `UPTO` through public DOS
`StrToDate`, then apply inclusive date-only bounds to each matched entry's DOS
day stamp. Invalid input exercises the candidate's `ERROR_BAD_TEMPLATE` cleanup
path. `QUICK` takes precedence over `DATES`, prints names only, and skips date
conversion. The 68000/020/040 MorphOS receipt passes 31 supplied vectors per
CPU in
`artifacts/cc11-list-morphos-native-20260923-date-range-quick-v1/qualification.json`;
the Workbench receipt passes 32 per CPU in
`artifacts/cc11-list-wb31-native-20260923-date-range-quick-v1/qualification.json`.
Range cases include entries exactly on both bounds. These fixtures do not
verify original command date syntax, diagnostics, output, packed correspondence,
PURE/resident reuse, or package admission.

### 2026-09-23 List DATES/NODATES resident qualification

The MorphOS and Workbench resident `List` candidates now accept `DATES` with
their documented full-row default and implement `NODATES` by suppressing date
and time columns without calling `DateToStr`. The captured docs describe dates
as the full-row default unless `QUICK` is used. Because the explicit
`DATES QUICK` output has not been captured, that combination remains
fail-closed at this earlier checkpoint; the date-range/QUICK qualification
above supersedes that limitation.

MorphOS passes 26 supplied vectors per CPU in
`artifacts/cc11-list-morphos-native-20260923-nodates-v1/qualification.json`;
Workbench passes 27 per CPU in
`artifacts/cc11-list-wb31-native-20260923-nodates-v1/qualification.json`.
Both receipts cover 68000/020/040 resident HUNKs, row/header output, the
NODATES no-conversion path, and existing parsing, filtering, ownership,
failure, and interleaving cases. This is fixture evidence only; no original
guest parity, complete List behavior, PURE admission, or package admission is
claimed.

### 2026-09-23 MorphOS Search DOS64 Seek64

MorphOS Search now reads `FileInfoBlock.Size64` when DOS is at least 51.28 and
uses public `DOS.Seek64` for files above the classic 32-bit offset limit. The
absolute reset and signed relative line rewinds use the SDK's D2:D3 argument
and D0:D1 return pairs; a `-1` pair is detected before another read and
publishes `IoErr`. Smaller files and older DOS providers keep classic `Size`
and `Seek` behavior.

The three-CPU resident fixture passes 45 supplied vectors per CPU (135 total),
with 40 reachable methods and no runtime helpers, external targets, exception
regions, fatal sites, leaks or shared-image writes. Added cases exercise the
DOS64 threshold, absolute and signed relative requests, and failure handling
using a synthetic large-file FIB. Receipt:
`artifacts/cc11-search-morphos-native-20260923-seek64-v12/qualification.json`.
This confirms the supplied-vector path; it does not establish behavior on an
actual >2 GiB file or real filesystem handler, nor original-guest output.

### 2026-09-23 MorphOS Search source buffer and pre-scan

MorphOS `FindString` now follows the inspected 512 KiB `MEMF_ANY` buffer
algorithm: failed initial requests are halved; when the file exceeds the
buffer, an LF-only pre-scan finds the longest line, the buffer grows when
needed, and the match pass seeks back and rewinds incomplete trailing lines.
Fixtures cover partial pre-scan reads, allocation exhaustion, failed-first-
allocation recovery, incomplete-line rewind with a small halved buffer, CTRL-D
during the pre-scan, and long-line growth/reread with a 524,300-byte file. The
68000/020/040 resident receipt passes 43 invocations per CPU with 40 reachable
methods and no helper/runtime features, leaks, or image writes:
`artifacts/cc11-search-morphos-native-20260923-source-prepass-v9/qualification.json`.
The Workbench syntax candidate passes 19 vectors per CPU, including a matching
line over 8,192 bytes:
`artifacts/cc11-search-wb31-native-20260923-source-prepass-v1/qualification.json`.
At that earlier 43-vector source-buffer checkpoint, the MorphOS `Seek64` path
above 2 GiB remained open because the candidate still used classic DOS `Seek`.
The newer DOS64 qualification above supersedes that limitation. Real large-file
handler and memory-pressure behavior, original output, packed correspondence,
PURE/resident lifecycle, and package admission remain open.

### 2026-09-23 MorphOS Search locale character classes

The inspected Search source declares `LOCALE_VERSION 38`, but the macro is
unused and its active entry opens locale.library v37. The candidate preserves
that v37 minimum, splits lines with `IsCntrl(locale, ch)` except for TAB, and
renders nonprintable bytes using `IsPrint(locale, ch)`. The resident body calls
those existing SDK bindings. The supplied fixture adds a 0x85
control delimiter and a 0xe9 printable output byte, alongside literal and
DOS-pattern context cases. MorphOS passes 37 supplied invocations per CPU on
68000/020/040; Workbench was requalified against the shared renderer and passes
18 per CPU. Receipts are
`artifacts/cc11-search-morphos-native-20260923-locale-classes-v2/qualification.json`
and
`artifacts/cc11-search-wb31-native-20260923-locale-safeguard-v2/qualification.json`.
The fixture supplies those high-byte classifications; actual default-locale
tables, exact guest output, packed correspondence, original guest parity,
8,192-byte-boundary behavior, PURE/resident lifecycle and package admission
remain open.

### 2026-09-23 MorphOS Info filter validation, result faults and source ordering

The MorphOS Info entry follows both source verbose startup paths: a valid
unit/device BSTR renders through `GetDevStr`, while other startup records use
the source's BSTR-style `GetStartupStr` fallback. Per-device `Lock` and `Info`
faults retain WARN until a row succeeds; `GOODONLY` suppresses the diagnostic
without changing the result or selected `IoErr`. Mounted-volume rendering
checks `info_datetime` through DOS `GetVar`; an opened locale uses `FormatDate`
and the command's output hook, while the default path still uses `DateToStr`.
This also follows source ordering: records are compared with utility
`Stricmp`, and the device rows render before the volume section even when the
DOS-list order is reversed. It builds on device state, filesystem type and
volume name from `InfoData` and DOS `NameFromLock`, the DOS-list-name fallback,
guarded startup `DosEnvec` type override, ordered `GetFSysStr` map, 64-bit DOS
51.8 counters, `InfoData` fallback, K/M/G/T/P sizes, mounted-volume dates and
selection/result behavior. The command also prevalidates each `DEVICES/M`
pattern in the source's 128-byte buffer. Malformed patterns return
`RETURN_ERROR`, preserve `IoErr`, publish the entry's `PrintFault`, and exit
before list access. Ctrl-C now publishes a partial device row before the
source's final non-OK `PrintFault`. The inspected source compiles its catalog opening
out under `#if 0`, so the active fallback strings are the source-observed
baseline, subject to packed-binary confirmation. The refreshed 68000/020/040
resident receipt passes 66 supplied vectors per CPU in
`artifacts/cc11-info-morphos-native-20260923-pattern-validation-v3/qualification.json`.
Workbench Info was rerun against the updated shared fixture and passes 14
vectors per CPU in
`artifacts/cc11-info-wb31-native-20260923-pattern-v14/qualification.json`.
These supplied-vector runs do not prove packed-binary or exact guest output,
full provider behavior, original guest parity, PURE/resident lifecycle or
package admission.

### 2026-09-23 Initial CC11 Type, Search, and Dir resident requalification

Current-source CC11 inspection entries were rebuilt on 68000/68020/68040.
Workbench Type passes 15 supplied vectors per CPU and MorphOS Type passes 13;
the two Search entries pass 18 (Workbench) and 17 (MorphOS) per CPU; MorphOS
Dir passes 15 per CPU. Receipts:
`artifacts/cc11-type-wb31-native-20260922-v1/qualification.json`,
`artifacts/cc11-type-morphos-native-20260922-v1/qualification.json`,
`artifacts/cc11-search-wb31-native-20260922-v1/qualification.json`,
`artifacts/cc11-search-morphos-native-20260922-v1/qualification.json`, and
`artifacts/cc11-dir-morphos-native-20260922-v1/qualification.json`, plus the
Workbench Dir receipt
`artifacts/cc11-dir-wb31-native-20260922-v1/qualification.json` with 16
supplied vectors per CPU.
These are current-source resident/static and supplied-vector receipts only;
exact original output, complete recursion/interactive behavior, packed
correspondence, guest parity, PURE/resident admission, packaging, and
differential gates remain open.

### 2026-09-23 MorphOS Search recursive AnchorPath stage

MorphOS Search now follows the inspected `ALL` behavior through DOS
`AnchorPath`: it requests descent for directory entries, handles directory
exit records, builds matched file paths through `NameFromLock`/`AddPart`, and
keeps per-directory indentation. The new fixture traverses a grandchild, two
sibling branches, five files, and each directory exit with distinct current
directory locks; it asserts all reconstructed file paths and output bytes.
The initial bounded locale stage opened locale.library v37 and the default
locale and used `ConvToUpper` for case-insensitive literal matching. A later
source audit confirmed the active v37 open call (the `LOCALE_VERSION 38` macro
is unused) and added the source's `IsCntrl` and `IsPrint` behavior. This earlier
68000/020/040 receipt
passes 21 supplied vectors per CPU in
`artifacts/cc11-search-morphos-native-20260923-nested-v2/qualification.json`.
Workbench Search is bound to a captured 2,476-byte disk 2 binary and its
eight-slot template candidate; its separate DOS 36 resident entry passes 18
vectors per CPU in
`artifacts/cc11-search-wb31-native-20260923-nested-check-v1/qualification.json`;
the MorphOS-only nested fixture is not claimed for Workbench. Static strings do
not prove Workbench runtime behavior. Larger real trees, MorphOS soft-link
policy, QUICK output, packed correspondence, original guest parity,
PURE/resident lifecycle and package admission remain open.

### 2026-09-22 CC10 PathPart, Which, and Quote resident requalification

Current-source CC10 resident entry receipts were regenerated for MorphOS
`PathPart`, `Which`, and `Quote` on 68000/68020/68040. PathPart passes 11
supplied vectors per CPU, MorphOS Which passes 16, and the bounded Quote STR
entry passes 12. The three receipts are
`artifacts/cc10-pathpart-native-20260922-v1/qualification.json`,
`artifacts/cc10-which-morphos-native-20260922-v1/qualification.json`, and
`artifacts/cc10-quote-native-20260922-v1/qualification.json`. Reports retain
zero shared-image writes and the expected resident/static checks. These are
fresh ABI and ownership receipts only: exact shipped grammars, real DOS
parser/filesystem behavior, original guest parity, PURE/resident admission,
licensing, packaging, and differential gates remain open.

### 2026-09-22 CC10 Eval resident requalification

Both current-source Eval entries were rebuilt and exercised on all three CPU
targets. MorphOS Eval passes 14 supplied vectors per CPU (42 total), and the
Workbench literal-template entry passes 12 per CPU (36 total). Receipts:
`artifacts/cc10-eval-morphos-native-20260922-v1/qualification.json` and
`artifacts/cc10-eval-wb31-native-20260922-v1/qualification.json`. The runs
retain the audited `nullable-values` feature only, use one loaded image per
CPU, and report zero shared-image writes. They are supplied-vector evidence,
not real DOS parsing/filesystem behavior, original guest parity, PURE/resident
admission, packaging, or differential qualification.

### 2026-09-23 Exe2Arc scanner dispatch owner

`Exe2ArcScannerSelection` now owns the source scanner table's command-level
dispatch. It routes explicit `TYPE` values to one scanner and preserves the
recorded unspecified order (ZIP, ACE, RAR, CAB, ARJ, LhA, LZH). A `NoMatch`
alone permits fall-through; offset-zero, I/O-stop and unsafe final-position
results terminate selection. The LhA branch restores the input origin before
its fixed 100-byte prefix probe because prior forward scans can leave the
cursor at EOF. Five focused controls pass, the managed Commands suite passes
704 tests, and the native project links/builds the owner with zero warnings or
errors. This remains a component checkpoint and does not qualify the full
frontend, cleanup, guest parity, PURE/resident lifecycle or package admission.

### 2026-09-23 Exe2Arc result and cleanup policy owner

`Exe2ArcResultPolicy` now centralizes command-level return and cleanup
precedence. A completed scan only reports detection; a nonzero completed
extraction reports saved bytes and the source warning. Empty or failed
extraction requests close-then-delete cleanup, while the source-compatible
ZIP Ctrl-C success retains partial output with `ERROR_BREAK`; pre-open
failures do not request deletion. Twelve focused
controls pass, the managed Commands suite passes 704 tests, and the native
project links/builds the policy with zero warnings or errors. It remains an
implementation policy checkpoint, not original diagnostic, final-`IoErr`,
guest-parity, purity or package evidence.

### 2026-09-23 Exe2Arc MorphOS frontend composition

`NativeMorphOSExe2ArcCommand` now composes the parser lease, input/FIB and
workspace ownership, source-order scanner selection, safe generated output
name and literal-TO fallback, ZIP or copy-to-EOF extraction, and cleanup
policy. It never loads or executes the input. The native project builds the
entry with zero warnings or errors. This remains a bounded frontend
composition checkpoint: the saved-byte warning, no-match/type-list and
output-open messages are wired, while exact redirected bytes, full guest
execution, packed correspondence, PURE/resident lifecycle, and package
admission remain open.

### 2026-09-22 Exe2Arc resident frontend static HUNK

The source-linked `tests/Commands.Exe2ArcNativeRoot` entry now compiles the
composed MorphOS frontend as a resident HUNK for all three target CPUs. The
reproducible qualification receipt is
`artifacts/exe2arc-native-static-20260922-v8/qualification.json`, generated by
`tools/Commands/qualify_exe2arc_native_static.ps1`.

| CPU | HUNK bytes | SHA256 |
| --- | ---: | --- |
| 68000 | 27164 | `20ba9c2cb42703f497164366b742117fa1a2dc4d92e32b85b0b125779cae62df` |
| 68020 | 27656 | `07ddfe29b7a82d881861eef57732f760dd28f33ecfe24b3db1e56cf95fedad19` |
| 68040 | 27132 | `f29fc59953b16b3f88d131a491244a9396560d415ac4ccf7bbdc8003f5820bf5` |

Each compatibility report is `IsCompatible=true` with 100 reachable methods,
zero managed allocation sites, runtime features, runtime helpers, external
native targets, exception regions and fatal machine-fault sites. The managed
Commands suite remains at 704 passing tests. This is static compiler and HUNK
evidence only: no original guest execution, filesystem-handler behavior,
redirected transcript, final `IoErr`, PURE/resident admission, packaging or
shipping row is claimed.

The supplied-vector frontend fixture also passes two parser/result failure
cases per CPU (ReadArgs template failure and workspace-allocation failure) in
the three `frontend-smoke-*-parser.json` receipts under
`artifacts/exe2arc-native-static-20260922-v8/`. It verifies balanced DOS
open/close, restored stack, parser ownership and expected result/IoErr values.
It does not provide a real Kickstart guest, filesystem, original parser,
unknown-type path, redirected transcript, PURE admission or shipping evidence.

### 2026-09-22 Exe2Arc bounded source diagnostics

The MorphOS frontend now emits the released fixed diagnostics for input/FIB,
`ExamineFH`, temporary-buffer acquisition, initial dispatch seek, output-open,
and ZIP unknown-record/EOF paths, using null `PrintFault` context and avoiding
duplicate fault output. The managed suite passes 704 tests and the native
project builds with zero warnings/errors. Redirected streams, guest transcript
parity and final post-cleanup `IoErr` remain open; no shipping row changes.

### 2026-09-23 Exe2Arc ACE scanner/copy component extension

The source-backed Exe2Arc slice now includes the ACE scanner path from
`c/xad/exe2arc.c`: `**ACE**` at candidate offset +7, strict `size > 14`
window admission, overlap and seek behavior, offset-zero rejection, EOF
payload sizing and the shared copy-to-EOF component. The allocation-free
`Exe2ArcOutputName` guest-memory builder also covers safe suffix replacement
and bounded/unterminated-name rejection. The three resident HUNK
receipts pass 84 supplied-DOS invocations per CPU (252 total); see
`artifacts/exe2arc-ace-native-20260923/qualification.json`. This does not
qualify the command frontend, ReadArgs/FIB ownership, output naming, original
guest behavior, packed binary correspondence, PURE/resident lifecycle, or
shipping package admission.

The output ownership slice now also has a source-order selector: literal `TO`
open, then the two `AddPart` fallback calls and fallback open only after the
literal failure. Six focused cases verify direct/generated selection, fallback
success, each AddPart failure boundary, and invalid fallback storage. Handle
close/delete and extraction-result policy remain command-owner responsibilities.
The explicit `TYPE/K` gate is now source-aligned as a bounded, allocation-free
case-insensitive matcher for the seven recorded extension/display-name pairs;
unknown, prefix and unterminated values are rejected. The managed Commands
suite passes 685 tests and the native component project builds with zero
warnings or errors. These are still component checks and do not qualify a
full command, original guest parity, resident lifecycle or package admission.
The native frontend now also exposes the exact three-slot DOS `ReadArgs`
ownership boundary (`FROM/A,TO,TYPE/K`), retaining the parser lease for later
resource acquisition. It remains an unqualified component and has no claim for
input/FIB/buffer lifecycle, diagnostics or full command execution.
The managed scanner extension also covers ARJ's `60 ea` marker, variable header
length and reflected CRC, including bad-CRC, later-candidate and truncated-tail
controls. It is source-backed host evidence only; the resident supplied-DOS
receipt still covers RAR4/CAB/ACE and has not been silently expanded.
The scanner also has a bounded LZH path preserving the source's marker and
scan-window byte-20 level behavior, with managed paired-byte and rejection
controls. It remains host component evidence pending a resident receipt.
The fixed LhA HUNK/SFX probe is also covered with big-endian start extraction,
short-read rejection, out-of-file bounds and repeated-seek failure ordering.
This is still managed component evidence and has not expanded the resident
RAR4/CAB/ACE receipt.
The bounded ZIP component now covers backward EOCD selection, central-entry
lookup, prefix correction, latest-candidate stop ordering and unsafe central
tail rejection. It remains host component evidence; ZIP record rewriting,
member copying and cancellation are still unqualified.
The in-place ZIP record rewriter now covers local, central and EOCD signatures,
safe correction application and short/unknown/overflow rejection. It remains a
component owner; streaming record I/O, member copying and Ctrl-C behavior are
not qualified.
The streaming ZIP record extractor now composes the rewriter and exact-chunk
copier for local, central and EOCD records, with managed success and unknown-
record controls. It remains component evidence without diagnostics, break
polling, cleanup or resident qualification.

### 2026-09-23 MorphOS Date packed identity closure

The MorphOS `Date` resident candidate already passes thirteen supplied
DOS/Exec/timer/locale vectors per CPU. Its packed `MorphOS/C/Date` member is
now hash-bound at ISO extent `175288` (3,748 bytes, version 50.7) and tied to
the 9,453-byte released source in
`reference-captures/date-morphos-binary-audit-20260923.json`. This closes
identity evidence only; exact guest diagnostics and `IoErr` precedence,
timer/provider behavior, PURE/resident lifecycle, packaging and differential
gates remain open.

### 2026-09-23 MorphOS Status source-aligned break boundary and identity

The MorphOS `Status` resident body now polls Ctrl-C before rendering each CLI
snapshot row, matching the released source's no-partial-row boundary. Its
packed `MorphOS/C/Status` member (extent `178472`, 3,679 bytes) and 5,709-byte
source are hash-bound in
`reference-captures/status-morphos-binary-audit-20260923.json`. The MorphOS
fixture now supplies DOS 51.51 `QueryCLIDataTagList`/`FreeCLIData` provider
vectors, validates the tag ABI, and exercises provider-side process and
command filters. The legacy `COM` comparison now follows source
`strnicmp` semantics, with a mixed-case `rUn` fixture selecting `Run`; its
three-CPU receipt passes nineteen supplied invocations per CPU. The standalone
qualifier passes `--exports none` so unrelated entry exports stay out of the
image. Guest parity, complete provider task population, exact
diagnostics, PURE/resident lifecycle, packaging and differential gates remain
open. Refreshed receipts are
`artifacts/status-morphos-native-20260924-com-casefold-v1/qualification.json`
and `artifacts/status-wb31-native-20260924-com-casefold-v1/qualification.json`;
the Workbench receipt passes sixteen supplied invocations per CPU.

The Workbench `C/Status` member is also now bound in
`reference-captures/status-wb31-binary-audit-20260923.json` to the selected
disk-2 capture (828 bytes, SHA-256
`fd7b386f103bafba80add97523991389880f9b6987f4534280abb61b8193580c`, version
37.2, candidate five-slot template). Its captured protection word is zero;
this is media identity evidence, not installed PURE or runtime parity.

### 2026-09-23 MorphOS Avail diagnostic and identity closure

The MorphOS `Avail` body now follows the released source's `PrintFault` policy:
parser and output failures report `Avail`, while the multiple-selector warning
does not. The refreshed resident HUNKs pass twelve supplied DOS/Exec vectors on
68000/020/040 with unchanged images and balanced fixture ownership; the same
fixture covers the released fake-chip `ExecBase.MaxLocMem` summary adjustment.
The packed
`MorphOS/C/Avail` member and 9,191-byte source are hash-bound in
`reference-captures/avail-morphos-binary-audit-20260923.json`. This remains
adapter evidence; original guest output, real fake-chip memory-provider
behavior, PURE/resident lifecycle, packaging and differential gates remain
open.

### 2026-09-23 MorphOS Reboot packed identity closure

`MorphOS/C/Reboot` is now hash-bound to the selected ISO member (extent
`178088`, 1,206 bytes, SHA-256
`6173f32fed7ab0cff36fb0c0f5e87f907525877ae67469e001f511c822199024`) and to
the released 955-byte source. The audit records the empty `ReadArgs` template,
the pending Ctrl-C fault path, `ColdReboot`, returning value `666`, and the
literal installer `+P` event. This closes identity evidence only; the actual
terminal reboot handoff, exact diagnostics, resident/PURE lifecycle and guest
differential remain open. Inventory verification passes and the suite passes
21 tests.

The shared resident candidate was then corrected against the released source:
empty-template `ReadArgs` failure no longer suppresses `SetSignal` or
`ColdReboot`. The refreshed MorphOS and Workbench receipts each pass ten
supplied invocations on 68000/020/040 with unchanged images and balanced
fixture ownership. This remains adapter evidence; terminal reboot, PURE,
packaging and guest differential gates remain open.

### 2026-09-23 ConClip MorphOS worker architecture gate

The source audit now records `NP_CodeType=MACHINE_PPC` for the persistent
MorphOS worker. CopperSharp currently emits 68k native command bodies only, so
the normal service cannot be admitted as a substituted 68k worker. The
cross-architecture PPC worker artifact, ownership/cleanup behavior and guest
qualification remain open.

### 2026-09-23 ConClip ABI audit regression

The inventory suite now validates the developer-CD and `CONSOLE_LIB.FD` hashes,
the four captured private vectors, and their A0 argument contracts. The suite
passes 16 tests. This protects evidence integrity only; it does not promote
the persistent command to shipping status.

### 2026-09-23 ConClip OFF control plane

`NativeConClipControl.TryStopExistingWorker` now implements the source-bound
control operation without starting a service: it protects Exec's port list,
looks up `ConClip.rendezvous`, signals the published task with
`SIGBREAKF_CTRL_C`, and releases protection on every branch. The native project
builds this path cleanly. Worker creation, duplicate guarding, hook lifetime,
clipboard behavior, guest differential, PURE and package admission remain open.

### 2026-09-23 ConClip private console binding scaffold

`NativeConClipConsoleRaw` now isolates the four captured private
`console.device` vectors with explicit A6 device-base and A0 hook/data
registers. The native command project builds with zero errors and the command
unit suite passes 623 tests. This is an
implementation scaffold only: no transient clipboard command was introduced,
and persistent worker, callback, guest differential, lifecycle, PURE and
package gates remain open.

### 2026-09-23 ConClip private console ABI evidence

The NDK 3.1 `console.device` FD now binds the private vectors required by the
released source: `GetConSnip` `-54`, `SetConSnip` `-60`, `AddConSnipHook`
`-66`, and `RemConSnipHook` `-72`. The A0 argument and return-value ABI is
recorded in `reference-captures/conclip-console-private-api-audit-20260923.json`.
This resolves the vector-number uncertainty, but no runtime or shipping claim
is made: original Workbench and MorphOS guest behavior, lifecycle and hook
ordering, failure paths, PURE and package admission remain open.

### 2026-09-23 ConClip persistent-service reference contract

`ConClip` now has a hash-bound Workbench/MorphOS identity and a released-source
audit in `reference-captures/conclip-reference-audit-20260923.json`, with the
contract in `contracts/ConClip.md`. The recorded boundary is
`CLIPUNIT=UNIT/N,OFF/S`; the MorphOS source requires a named persistent worker,
Intuition edit-hook, console snip-hook, clipboard IFF transactions and ordered
retirement. No transient implementation was added because it would not satisfy
the command's service semantics. Guest parser/help, provider failure,
retirement/lifecycle, installed protection, package and differential evidence
remain open, so no qualification count changes.

### 2026-09-23 MorphOS capture-audit drift regression

The inventory test suite now mutates a synthetic MorphOS capture audit and
requires the validator to reject its changed SHA-256. This protects the
fail-closed evidence path used by the seven hash-bound MorphOS audits; it does
not qualify command runtime behavior.

### 2026-09-23 MorphOS WaitX reference contract

`MorphOS/C/WaitX` is hash-bound to its 4,230-byte ISO member at extent 178824,
with the 51.1 version tag, documented wait-then-execute purpose, and installer
`+P` event recorded in `contracts/WaitX.md` and
`reference-captures/waitx-morphos-binary-audit-20260923.json`. The packed member
does not yield a trustworthy template or diagnostics, so delay, timer, nested
launch, output, and result behavior are not inferred. Guest, lifecycle, PURE,
package and differential gates remain open. Inventory extraction rechecks the
audit against the pinned ISO.

### 2026-09-23 MorphOS CPU, Stat and ShowConfig reference contracts

MorphOS `CPU`, `Stat`, and `ShowConfig` are now hash-bound to their selected ISO
members (4,123, 4,319, and 15,811 bytes) with version tags and documented
purpose records in their contracts and binary audits. Installer `+P` events are
recorded for CPU and Stat; ShowConfig has no observed installer protection
event, so its purity remains unresolved. Packed templates, provider behavior,
output, guest parity, lifecycle, package and differential gates remain open.
Inventory extraction verifies all three audits against the pinned ISO.

### 2026-09-23 MorphOS Time reference contract

`MorphOS/C/Time` is hash-bound to its 4,999-byte ISO member at extent 178504,
with the 1.0 version tag, documented command-timing purpose, and installer
`+P` event recorded in `contracts/Time.md` and
`reference-captures/time-morphos-binary-audit-20260923.json`. The packed member
does not yield a trustworthy template or diagnostics, so no command-tail,
timer, output, or failure behavior is inferred. Guest, lifecycle, PURE,
package and differential gates remain open. Inventory extraction rechecks the
audit against the pinned ISO extent, byte count and SHA-256.

### 2026-09-23 MorphOS Uptime reference contract

`MorphOS/C/Uptime` is hash-bound to its 2,733-byte ISO member at extent 178788,
with the 50.5 version tag, no-template documentation boundary, and installer
`+P` event recorded in `contracts/Uptime.md` and
`reference-captures/uptime-morphos-binary-audit-20260923.json`. No formatter or
timer behavior is inferred; guest, lifecycle, PURE, package and differential
gates remain open. Inventory extraction now rechecks the audit against the
pinned ISO extent, byte count and SHA-256 before accepting the evidence.

### 2026-09-23 Workbench capture-audit integrity gate

Inventory extraction now validates both durable Workbench `C:` capture audits
against the selected external ADFs, including source identity, pinned image
hash, member membership, byte counts and SHA-256 values. Stale or edited
metadata fails closed; this strengthens reference evidence but does not qualify
runtime behavior or close the remaining shipping gates.

### 2026-09-23 Workbench disk 1 C: command capture

The externally supplied Workbench 3.1 M10 Install disk 1 ADF was hash-verified
and all 20 listed `C:` members, including installation-only helpers, were
captured as metadata with size and SHA-256 records. The audit is
`reference-captures/wb31-disk1-c-command-captures-20260923.json`. This is
reference identity evidence only. The three Install/HDSetup/Update startup and
installer members are also hash-bound in
`reference-captures/wb31-disk1-install-script-captures-20260923.json`; this is
script evidence, not installed-state or execution evidence. Disks 3-6,
installed placement/protection, guest behavior, PURE/resident lifecycle and
package gates remain open.

### 2026-09-23 Workbench disk 2 C: command capture

The externally supplied Workbench 3.1 M10 disk 2 ADF was hash-verified and all
50 listed `C:` members were captured as metadata with size and SHA-256 records.
The audit is
`reference-captures/wb31-disk2-c-command-captures-20260923.json`. This is
reference identity evidence only; disks 3-6, installed placement/protection,
guest behavior, PURE/resident lifecycle and package gates remain open.

### 2026-09-22 MorphOS LoadMonDrvs reference boundary

MorphOS `MorphOS/C/LoadMonDrvs` is hash-bound to its 2,279-byte packed member
at ISO extent 176144. The contract records the documented `FROM/K,EXCEPT`
candidate, DEVS:Monitors default and required PURE event. The packed parser,
driver initialization, guest effects, lifecycle, package and differential gates
remain open; the resident candidate's three-CPU static receipt is
`artifacts/loadmondrvs-morphos-native-static-20260922-v1/qualification.json`.
No host-backed loader is admitted.

### 2026-09-22 IPrefs startup-service contract boundary

Workbench `C:IPrefs` is hash-bound to its 13,848-byte `iprefs 40.7 (2.6.93)`
HUNK and MorphOS `MorphOS/C/IPrefs` to its 43,798-byte packed member. The
contract and audit record the no-template startup service, duplicate guard,
completion message, library floors, preference handler table and installation
validation. No transient replacement is admitted; exact provider behavior,
guest ordering, lifecycle/PURE, package and differential gates remain open.

### 2026-09-22 Workbench SetPatch machine-contract boundary

The identical Install3.1 and Workbench3.1 `C:SetPatch` HUNK is hash-bound to
13,484 bytes (`setpatch 40.16 (14.2.94)`, SHA-256
`745b2f90fabcdeccba099b3302593eff89f393d65bd84a9c6bad1b46063ab5cc`). The
audit and contract record the four switch candidate, non-PURE media word and
machine-specific patch families. No replacement is admitted: exact parser and
patch predicates, machine/ROM byte effects, rollback, lifecycle, package and
differential gates remain open.

### 2026-09-22 AddDataTypes reference boundary

The Workbench 3.1 `C:AddDataTypes` HUNK and MorphOS 3.20 packed member are
hash-bound in
[`adddatatypes-reference-audit-20260922.json`](reference-captures/adddatatypes-reference-audit-20260922.json),
with the public operation and cleanup contract in
[`contracts/AddDataTypes.md`](contracts/AddDataTypes.md). The captured profile
grammars differ (`FILES/M,QUIET/S,REFRESH/S` versus the MorphOS source's added
`LIST/S`). The MorphOS source also records the datatype-list semaphore,
IFF/DTYP/DTCD loading, refresh directory precedence, one-directory matcher
descent and resident segment ownership. These facts establish the next
implementation boundary only; no original guest execution, exact diagnostic or
result comparison, lifecycle/PURE evidence or package admission is present.

### 2026-09-22 Workbench LoadResource binary boundary

The selected Workbench `C:LoadResource` HUNK is hash-bound to the 3,972-byte
`loadresource 40.2 (17.3.93)` member (`51c8d6da726d5d1429e84f36e323218eb94750ab60b650efe768fc2907d16f38`).
The audit records the `NAME/M,LOCK/S,UNLOCK/S` candidate, the observed
Exec/DOS/locale/utility/graphics/diskfont dependency surface, and the
installer-era resource lock/catalog boundary. Exact resource state,
diagnostics, guest behavior, PURE/lifecycle, package and differential gates
remain open; no shipping command is qualified.

### 2026-09-22 Workbench SetFont native entry boundary

The selected Workbench 3.1 `C:SetFont` HUNK is hash-bound to
`setfont 39.1 (2.6.92)` (`c3e1e763b12fd4f710a414443facea49479aa723a94828828dbaee214fe4ad8c`).
The provisional resident entry preserves the seven-slot grammar and public
DOS/graphics/diskfont/utility call boundary. Its three-CPU static and runtime
receipt is
`artifacts/setfont-wb31-native-entry-20260922-v2/qualification.json`, with
sixteen supplied invocations per CPU covering parser and library ownership,
TextAttr/style flags, console packet/font replacement, escape output,
startup guards and interleaved callers. Original guest packet/layout and
result precedence, PURE/lifecycle, package and differential gates remain
open; no shipping command is qualified.

### 2026-09-22 MorphOS SetKeyboard source-informed resident boundary

The verified MorphOS 3.20 packed member is the 2,856-byte `7f4d4f53` payload
at ISO extent 178200 with SHA-256
`f12a418b372b7e2d724a2ba56235e58bcd9135466c26fea8031e12f1af3c8fa4`. Its
bounded unexecuted inspection recovered only the version tag and no plaintext
template or diagnostics; see
`reference-captures/setkeyboard-morphos-packed-inspection-20260922.json`.

The source-informed MorphOS DOS 37 resident candidate implements the released
`IPrefs` keymap loading/publication path while keeping `KEYMAP/A` provisional:
resource reuse, absolute paths, `KEYMAPS:`/`MOSSYS:Devs/Keymaps` fallback,
resident and extended nodes, duplicate-race serialization, default selection,
and segment rollback. Static HUNK compilation and 12 supplied vectors per CPU
pass for MC68000/020/040 in
`artifacts/setkeyboard-morphos-native-entry-20260922-v1/qualification.json`.
This is source-correspondence and supplied-vector evidence only; exact packed
syntax/diagnostics, guest parity, PURE/lifecycle, packaging and differential
gates remain open.

### 2026-09-22 Workbench SetKeyboard runtime boundary

The Workbench 3.1 `SetKeyboard` profile now has a source-bound contract and a
resident entry preserving the captured `KEYMAP/A` grammar. It uses public DOS
`AddPart`/`LoadSeg` and `keymap.library/SetKeyMapDefault`; a successful keymap
segment remains loaded permanently as required by the API. The repeatable
three-CPU static receipt is
`artifacts/setkeyboard-wb31-native-static-20260922-v2/qualification.json`.
The executable boundary receipt is
`artifacts/setkeyboard-wb31-native-entry-20260922-v2/qualification.json`,
with ten supplied vectors per CPU and no shared-image writes or leaked
resources; the resource-reuse vector places the matching `KeyMapNode` at the
list tail. This remains native ABI and fixture evidence only; original guest
diagnostics and behavior, installed lifecycle/PURE metadata, package
admission, and differential comparison remain open.

### 2026-09-21 BindDrivers reference contract

The shared [BindDrivers contract](contracts/BindDrivers.md) now binds the
Workbench 3.1 and MorphOS 3.20 member identities and records the source-backed
no-option expansion-directory scan, v37 library floors, PRODUCT/ConfigDev
selection, HUNK resident discovery, InitResident ownership, and cleanup order.

The first source-bound MorphOS resident body and private entry now compile as
68000/020/040 HUNKs with zero managed runtime features, helpers, external
targets, exception regions, and fatal fault sites. The separate runtime
receipt `artifacts/binddrivers-morphos-native-entry-20260921-v2/qualification.json`
passes seven supplied boundary vectors per CPU, including no-match/empty
scans, open failures, repeat/interleaving, and cleanup ownership. Provider
execution, guest comparison, PURE admission, package placement, and shipping
profile qualification remain open.

### 2026-09-21 MorphOS Format synthetic disposable-media boundary

The MorphOS `Format` resident entry now has a repeatable synthetic
DOS-handler/trackdisk boundary fixture in [Format](contracts/Format.md). The
exact source `ReadArgs` template is exercised by ten supplied vectors per CPU
on MC68000/MC68020/MC68040 (30 total), covering startup and missing-DOS guards,
parser/result-allocation failures, banned system name, missing device,
repeat/interleaving, and full/quick formatting. Full formatting performs one
classic format write/readback plus update/clear/motor cleanup; quick formatting
performs no trackdisk media I/O. The receipt is
`artifacts/format-morphos-native-entry-20260921-v4/qualification.json`.
This is resident ABI and ownership evidence against in-memory disposable media;
production providers, original-guest parity, TD64/SFS/icon behavior, PURE,
and package/image admission remain open.

### 2026-09-21 MorphOS Mount outer-entry boundary

The resident MorphOS `Mount` entry now has a bounded outer-parser fixture in
[Mount](contracts/Mount.md). The exact `DEVICE/M,FROM/K,DEBUG/S` grammar is
executed on MC68000/68020/68040 HUNKs for an empty multiple vector, parser
failure, result allocation failure, and repeat/interleaving cleanup (five
invocations per CPU, 15 total). The receipt is
`artifacts/mount-morphos-native-entry-20260921-v2/qualification.json`.
It proves only DOS startup, result/RDArgs ownership, IoErr preservation and
resource/image cleanup; real DOSDrivers/MountList, icon/Expansion and
handler/provider behavior, Workbench startup, PURE, differential evidence and
package admission remain open.

### 2026-09-21 Workbench Mount outer-entry boundary

The Workbench 3.1 `Mount` resident entry now has a bounded outer-parser
fixture in [Mount](contracts/Mount.md). The exact `DEVICE/M,FROM/K` grammar is
executed on MC68000/68020/040 HUNKs for an empty multiple vector, parser
failure, result allocation failure, repeat/interleaving cleanup, empty and
single-argument Workbench startup messages that return before CLI parsing, a
missing startup argument list, and a startup source-not-found path that
exercises argument-list iteration, `CurrentDir` save/restore, `FilePart`,
Expansion cleanup and icon fallback failure (nine invocations per CPU, 27
total). The receipt is
`artifacts/mount-wb31-native-entry-20260921-v5/qualification.json`.
It proves only DOS36 startup, result/RDArgs ownership, IoErr preservation,
startup reply ordering and resource/image cleanup; successful Workbench startup
source processing, real source/provider behavior, PURE, differential evidence and
package admission remain open.

### 2026-09-21 Workbench ExtractKickstart native fixture

The captured Workbench 3.1 `ExtractKickstart` helper now has a source-derived
contract and separate resident native candidate in
[ExtractKickstart](contracts/ExtractKickstart.md). Its
`DEVICE/A,TO/A,1.3/S` boundary, `DF0:`-style validation, raw
`trackdisk.device` access, `KICKSUP0` boot-block check, source layout reads,
and invocation-owned I/O leases are recorded. Three-CPU HUNK compilation
passes with no managed allocation/runtime features, and the bounded native
trackdisk/DOS fixture passes seventeen supplied vectors per CPU for both media
layouts, diagnostics, failure paths, cleanup and interleaving. No guest or
shipping evidence is claimed; image-layout, guest, PURE/resident and package
gates remain open. Receipt:
`artifacts/extractkickstart-wb31-native-20260921-v6/qualification.json`.

### 2026-09-20 Workbench GuessBootDev bounded native candidate

The Workbench 3.1 `GuessBootDev` installer command now has a separate DOS36
resident entry using the captured `BOOTDISKNAME` template and invocation-owned
boot-node, lock, parser and library state. The three-CPU receipt
`artifacts/guessbootdev-wb31-native-20260921-v2/qualification.json` passes
sixteen supplied Exec/DOS/utility.library/expansion.library vectors per CPU,
covering signed priority selection, disabled and unusable filesystems, lock
fallback, parser and library-open failures, startup boundaries, missing DOS
and interleaved callers. The HUNKs have thirteen reachable methods, no managed
allocation sites, runtime features/helpers, external targets,
exception/fatal sites, leaks or shared-image writes. Exact original boot-node
semantics, guest parity, PURE/resident lifecycle, packaging and differential
evidence remain open. See [GuessBootDev contract](contracts/GuessBootDev.md).

### 2026-09-20 Workbench IconPos bounded native candidate

The Workbench 3.1 `IconPos` installer command now has a separate DOS36
resident entry using the captured twelve-slot grammar and public icon.library
DiskObject vectors. The three-CPU receipt
`artifacts/iconpos-wb31-native-20260920-v2/qualification.json` passes fifteen
supplied Exec/DOS/icon.library vectors per CPU, covering DiskObject
position/type and drawer mutation, free-position switches, image copying,
default creation, parser/type and storage failures, startup boundaries,
missing DOS and interleaved callers. The HUNKs have thirteen reachable
methods and no managed allocation sites, runtime features/helpers, external
targets, exception/fatal sites, leaks or shared-image writes. Exact original
icon.library semantics, guest parity, PURE/resident lifecycle, packaging and
differential evidence remain open. See [IconPos contract](contracts/IconPos.md).

### 2026-09-20 Workbench Check2090 bounded native candidate

The Workbench 3.1 `Check2090` installation helper now has a separate DOS36 /
expansion33 capability-floor
resident entry using public Expansion `FindConfigDev`. The three-CPU receipt
`artifacts/check2090-wb31-native-20260921-v3/qualification.json` passes ten
supplied Exec/Expansion/DOS vectors per CPU, covering A2090 controller flag
states, expansion-open failure, Workbench and malformed startup boundaries,
missing DOS, and interleaved callers. The HUNKs have five reachable methods,
no managed allocation sites, runtime features/helpers, external targets,
exception/fatal sites, leaks, or shared-image writes. Exact original
result/IoErr behavior, guest parity, PURE/resident lifecycle, packaging and
differential evidence remain open. See [Check2090 contract](contracts/Check2090.md).

### 2026-09-21 Workbench FindResident capability-floor candidate

The Workbench 3.1 `FindResident` candidate now has a separate resident entry
using the captured `MODULE/A` syntax and invocation-owned DOS
`ReadArgs`/`FreeArgs` plus public Exec `FindResident`. The three-CPU receipt
`artifacts/findresident-wb31-native-20260921-v4/qualification.json` passes
eleven supplied DOS 36/Exec vectors per CPU, including found and missing
residents, parser and result-slot failures, Workbench startup, missing DOS, and
interleaved callers.
The HUNKs have ten reachable methods and no managed allocation sites, runtime
features/helpers, external targets, exception/fatal sites, leaks, or shared
image writes. The bounded entry uses the lowest verified DOS 36 capability
floor; the captured DOS 37 request remains differential evidence. Exact original
diagnostics, guest parity, PURE/resident lifecycle, packaging and differential
evidence remain open. See [FindResident contract](contracts/FindResident.md).

## Scope and current coverage

### 2026-09-21 Workbench installation-helper contract checkpoint

Static media inspection now binds `Install3.1:C/Prod_Prep` (37,008 bytes,
`$VER: prod_prep 39.1 (22.12.92)`) and `Install3.1:C/UpdateWBFiles` (6,764
bytes, `$VER: updatewbfiles 40.1 (1.4.93)`) to dedicated contracts. The former
records its custom interactive RDB/partition and destructive device surface;
the latter records the `NEWWB:` preference/icon IFF update surface and its
DOS/iffparse/icon/graphics dependencies. No `ReadArgs` grammar was inferred.
Both remain open for guest capture, native implementation, PURE/resident,
package and differential gates.

### 2026-09-17 Relabel FFS name boundaries and recovery

Four original/replacement pairs now verify thirty-byte success and rejection
of thirty-one-byte, empty and slash-containing names. Invalid names reach DOS
intact and return 20/210 with exact fault output; the old label remains on disk.
All four recovery calls return 0/0 and preserve payloads, metadata and ownership.
Intermediate root/non-root observations and final exports bind the actual disk
effects. Eight TRXs and sixteen controls pass under
`artifacts/relabel-name-boundaries-20260916-v2/`. The first matrix with an
unnecessary Wait 3 remains incomplete; limits were not raised to pass it.
These are FFS handler cases, not full option/platform/PURE/package admission.
See [boot report](relabel-original-dos-boot.md).

### 2026-09-16 Relabel persisted disk cold-remount

Fresh ROM guests now read each exact disk exported by the qualified persistent
run. Both resolve SavedDisk:, read the proof through device/volume names and
read all 1792 nested guard bytes. Neither loads or invokes Relabel; both disks
are write-protected and remain byte-identical. The receipt binds and revalidates
the earlier source comparison: `artifacts/relabel-cold-remount-20260916/qualified/`.
Two boot TRXs and twelve corruption controls pass. This closes bounded DOS1
cold-readback coverage without adding command option cases. Explicit Mount,
host Exec/device overlays, other filesystem/platform variants and full
PURE/package gates remain limitations. See [boot report](relabel-original-dos-boot.md).

### 2026-09-16 Relabel persistent writable FFS volume

The original and replacement now pass two real DOS/FFS relabels on an owned
writable DF1, first by device name and then by volume name. Both return 0/0
with empty output and balanced resident/parser/storage ownership. Independent
sector readback proves `SavedDisk` persisted, only root label/date/checksum
fields changed, and both payloads, all file metadata and the bitmap stayed
unchanged. Final receipt: `artifacts/relabel-persistent-20260916-v6/qualified/`;
twelve corruption controls pass. Five earlier probe versions remain explicitly
non-qualifying; the observed Mount/locale requester required an ENV: assign.
The fixture uses an explicit original guest Mount, not repaired auto-discovery.
Cold-remount, other platforms/filesystems and original PURE/package gates remain
open. See [boot report](relabel-original-dos-boot.md).

### 2026-09-16 Relabel caller-owned storage release

Both fresh boots reclaim all four recorded tc_MemEntry spans, including the
Process/stack: 3520 rounded bytes. Exact FreeMem arguments and immediate
post-return free-list coverage establish release before later reuse. Original
RemTask remains native and calls the host FreeMem provider while still using
the retiring user stack; no host safe-handoff waiver is inferred. The initial
end-of-boot snapshots remain insufficient intermediate evidence. Final receipt:
`artifacts/relabel-caller-memory-20260916-v2/qualified/comparison.json`;
17 memory, 20 retirement and 21 concurrency/refusal controls reject. Full
process-resource ownership, host RemTask/native handoff, forced death and
shipping/PURE gates remain open. See [boot report](relabel-original-dos-boot.md).

### 2026-09-16 Relabel caller self-removal and resident reuse

Both fresh boots observe terminal RemTask(NULL) after the background invocation
returns, with no active command calls and resident use count 1. The task stays
absent from valid ready/wait lists after switching away; parent recovery runs
successfully afterward and final removal uses the same node. The receipt is
`artifacts/relabel-caller-retirement-20260916/qualified/comparison.json`;
20 retirement and 21 existing concurrency/refusal corruptions reject. Earlier
idle/failed replacement captures re-verify with 24/56 controls. Complete process
storage reaping, abrupt death and outstanding-resource termination remain open;
no shipping/PURE gate is promoted. See [boot report](relabel-original-dos-boot.md).

The known Workbench media input directory was also rechecked: only the same
Install/Workbench ZIPs are available. The four-disk closure item remains open,
with a dated [availability capture](reference-captures/wb31-local-media-availability-20260916.json).

### 2026-09-16 Relabel retained after failed resident replacement

Both missing and invalid replacement-source cases pass on original/replacement
boots. The same node/segment/count 1 survives, two later calls reuse the retained
image, and final removal frees it once after all returns. Command operations,
readbacks and per-call ownership match. Missing source fails before LoadSeg;
invalid text reaches LoadSeg/212, but original Resident reports 5/205 and exact
`object not found` output in both cases. The checker binds the intermediate and
final errors separately. Evidence: `artifacts/relabel-failed-replace-20260916/`
with two paired comparison receipts and 56 negative controls. The previous
successful-replacement captures re-verify with 24 controls after shared checker
extraction. No production command/runtime changed and no full gate or shipping
count is promoted. See [boot report](relabel-original-dos-boot.md).

### 2026-09-16 Relabel successful idle resident replacement

Both binaries pass successful replacement by another load of the same HUNK:
new image loads before old release, one existing registry node changes from old
to new segment, REPLACE returns 0/0, and two further calls reuse the new image.
All three Relabel calls return 0/210 with matching public DOS operations and
readbacks. Final REMOVE returns 0/202. Each image is checked through its own
free, with no shared-code writes and balanced per-call parser/storage/DOS
ownership. `artifacts/relabel-idle-replace-20260916/qualified-v2/comparison.json`
binds the paired runs; 24 corruptions reject. Fresh active-refusal regression boots also pass
with the generation-aware observer and 21 controls. Retained removal/overlap/
baseline/extended captures re-verify with 20/12/12/20 controls. Only
fixture/observer/verifier code changed. Failed replacement loading, other lifecycle cases and the
original classification/launch/platform/package gates remain open. See the
[boot report](relabel-original-dos-boot.md) for exact scope and reproduction.

### 2026-09-16 Relabel replacement refusal during an active call

Both binaries pass the original Workbench Resident REPLACE/PURE refusal path:
10/202 and exact in-use output, no attempted second load or registry mutation,
same retained node/count 2, successful overlap/recovery and final removal/free
at count 1 (0/202). Per-call storage/parser/DOS ownership remains balanced.
`artifacts/relabel-active-replace-20260915/qualified/comparison.json` binds the
paired runs; 21 corruption controls pass. Earlier removal/overlap evidence
re-verifies with 20/12 controls. This is Relabel lifecycle evidence under the
original helper, not CopperOS/MorphOS Resident qualification. Successful
replacement, forced removal, task death, original classification and remaining
full-profile gates stay open. See [boot report](relabel-original-dos-boot.md).

### 2026-09-15 Relabel ordinary removal attempt during an active call

Both binaries pass active-removal refusal followed by retained segment use and
final removal. Original Resident returns 5/202 with `object is in use` plus LF;
RemSegment refuses at observed count 2. The same registry node serves the next
caller. After Cancel/recovery and all Relabel returns, removal succeeds at count
1 with Resident result 0 and preserved IoErr 202. The three Relabel results,
task-owned parser/storage cleanup and readbacks remain matched.

`artifacts/relabel-active-remove-20260915-v2/qualified/comparison.json` binds both
final executions and their media/build identities. Twenty negative controls
pass, and the preceding concurrency evidence re-verifies with twelve controls.
The first unredirected display probe remains failed; final media redirects the
intermediate Type display to NIL: while preserving real payload reads. This is
fixture isolation, not general input-focus qualification. No command/device/CPU
implementation changed. Active replacement, forced removal, task death and the
remaining full-profile gates stay open. See [boot report](relabel-original-dos-boot.md).

### 2026-09-14 Relabel concurrent callers of one resident segment

A further original/replacement boot pair proves actual two-task overlap:
the background protected invocation retains its parser/storage while the parent
relabels RAM; real Cancel then releases the background call and parent recovery
succeeds. One LoadSeg/AddSegment supplies all three invocations. Exact task/run
cycles establish overlap independently of the reported active count. Distinct
RDArgs and disjoint live replacement allocations, owner/address/size cleanup,
ordered DOS behavior, diagnostic/payload bytes and final segment release pass.

The final receipt is `artifacts/relabel-concurrent-20260914/qualified-v2/comparison.json`.
Twelve targeted corruptions reject, including shared parsers/storage and cleanup
assigned to the other caller. Earlier argument/help/cancellation evidence also
re-verifies with its twenty controls. No command or device implementation was
edited in this slice. This advances concurrent resident evidence, while active
registry changes, task death, other launch/platform modes, original PURE
classification and package admission remain open. See [boot report](relabel-original-dos-boot.md).

### 2026-09-14 Relabel protected-disk cancellation and input forwarding repair

Both original Workbench Relabel and the unchanged replacement now complete
protected-DF0 cancellation through the production host-keyboard entry point.
The actual guest handler receives Left-Amiga+B, EasyRequestArgs returns zero,
and the commands agree on result 20, IoErr 214 and the exact write-protection
diagnostic. Four paired protected/help/recovery executions pass; the replacement
balances seven directly observed allocations and both binaries close three
parser leases, four DOS leases and their resident segment lifetime.

The blocker was a sibling emulator input-service bug: installation was tied to
the temporarily removed BeginIO gateway, causing boot discovery to reinstall
the service over a forwarded ROM call. Installation now follows allocated state
ownership. The focused regression fails before and passes after; all eighteen
keyboard/input tests pass. CPU/chipset timing and the command implementation
are unchanged. Original-CIA and pre-fix host-input failures remain recorded.

`artifacts/relabel-input-forward-fixed-20260914/qualified/comparison.json` binds
both completed traces, private media, command/executor hashes and comparisons.
Its `controls.json` rejects twenty extended/incomplete/cancellation corruptions.
A fresh eight-case baseline pair also passes under this executor, with twelve
more negative controls. See [boot evidence](relabel-original-dos-boot.md).
This reaches twenty distinct paired Relabel scenarios, not whole-profile
qualification. The host Exec/device provider, concurrency, original purity
classification, additional modes/platforms and packaging remain open.

### 2026-09-13 Relabel extended arguments/help and explicit protected-disk gap

The unchanged Workbench 68000 HUNK passes eleven more original-DOS comparisons:
eight argument cases and three help cases. The two final receipts are
`artifacts/relabel-extended-20260913/final-arguments/comparison.json` and
`final-help/comparison.json`. Duplicate DRIVE/NAME and unknown surplus tokens
return 20/118; lowercase keywords and quote/star escapes succeed. Empty NAME
parses but fails in the handler with 20/210 and `object name invalid` plus LF.
Real ReadArgs `?` with NIL: input returns 20/116 and emits template plus fault;
redirected continuation succeeds and leaves the exact template prompt with no LF.
All output files and six further volume-label payload readbacks agree.

The shared media verifier still validates actual private disk blocks, loaded
relocated code and the bound executors. The extended verifier scopes segment
identity to registration/removal (Type/Wait reuse its address afterward), binds
each readback open to its own match, and validates optional terminal EOF against
the examined byte length. The earlier eight-case baseline and twelve rejection
controls pass under the refactored verifier without rerunning the guest. New
controls reject ten corruptions and both actual incomplete protected-disk runs.

Protected DF0 is not admitted. Both initial commands remain inside DOS Relabel
at the execution bound. The original-only packet/dialog trace proves a matched
ACTION_RENAME_DISK reply with 0/214 followed by pending Intuition EasyRequestArgs.
`protected-wait.json` explicitly records incomplete status and zero completed
commands; a readiness-test pass is not a command pass. Its guest is disposed,
not a live wait. Next provide controlled requester cancellation and compare both
final results, diagnostics and cleanup. See [full evidence](relabel-original-dos-boot.md).
No command implementation changed or whole profile gate closed. Shipping stays
0/200 identities and 0/246 profiles.

### 2026-09-13 Relabel paired original-DOS boot and readback

The unchanged Workbench Relabel 68000 HUNK now has real DOS/Shell integration
evidence in [the paired boot report](relabel-original-dos-boot.md). Each binary
executes eight command cases, four new-volume payload readbacks and four exact
diagnostic readbacks. Positional/quoted, reordered keyword/equals, assign lookup,
invalid-drive/name, missing/extra argument and recovery cases agree on public
operations, bytes, return levels and final IoErr. The observed successful IoErr
210 remains intact; it is not converted to failure or cleared.

One loaded segment per binary is registered, reused eight times sequentially,
removed and freed. Six parser leases and eight DOS library leases per binary
close; thirteen directly observed replacement allocations balance. Code remains
unchanged. `artifacts/relabel-boot-20260913/qualified/comparison.json` binds the
TRXs, media receipts, HUNKs and observations. `controls.json` rejects twelve
corruptions, including parser release, resident registration/removal, output
bytes, errors and image identity. Actual private disk blocks are independently
checked; original media and all unlisted helpers remain unchanged.

Both final boot tests pass in the isolated assembly build. This runs original
DOS/Shell under the recorded emulator host Exec/device policy, not a fully
native original OS. Forced PURE sequential use does not resolve original flags
or full resident qualification. Remaining argument/handler cases, Workbench
launch, 020/040 real-OS runs, concurrent lifecycle, MorphOS correspondence and
packaging remain required. Shipping stays 0/200 identities and 0/246 profiles.

### 2026-09-13 Workbench Relabel original comparison and correction

The earlier shared MorphOS body is superseded for Workbench by an independent
DOS 36 resident body. [Relabel's contract](contracts/Relabel.md) records the
pinned original 37.2 identity, observed assigns/list flags, final-byte DRIVE
handling, diagnostics, return/error timing, and the explicit normalization for
borrowed parser storage and unsafe original buffer writes.

`artifacts/relabel-wb31-reference-20260913-qualified/qualification.json` passes
38 original/replacement comparisons on each of 68000/020/040. Both binaries run
on the selected CPU, unlike AddBuffers' separately recorded 68000-only reference.
There are 114 original calls and 123 generated calls, including two allocation
failures and a generated long-drive case per CPU. Five methods are reachable;
native static checks have no managed allocation sites, helpers/features, external
targets, exception regions or fatal sites. Each side reuses one protected image
sequentially; stack, argument, library and allocation ownership checks pass.

HUNK hashes: 68000/040, 1748 bytes,
`60726c52a74d4b046f020ac51cc7fedcc1a21b3b870e5aeacfd47b08c9760df1`;
68020, 1776 bytes,
`ea1bdece28245e1006a2ff3339c8db757be57ef9662e946845f209c59a6d9993`.
Runtime reports bind the original, generated, executor and CPU assembly hashes;
the qualification manifest binds runtime report hashes. Guard checks reject a
populated output directory without changing its receipt and reject a different
original binary before builds (`artifacts/relabel-reference-20260913/driver-guards.json`).

The retained MorphOS HUNKs pass eight supplied-vector cases per CPU under the
updated executor (24 total); they were not rebuilt by that regression check.
The command test suite passes 623/623. Real DOS parsing/fault output, handlers,
Workbench launch, concurrent resident lifecycle, original PURE classification,
MorphOS packed correspondence and package admission remain open. This is partial
reference/ABI evidence, not a shipping or PURE approval. Shipping remains 0/200
identities and 0/246 profiles. The 2026-09-12 candidate sections below are history.

### 2026-09-13: AddBuffers original comparison and classic correction

The Workbench AddBuffers shared candidate failed comparison against the pinned
original 37.2 binary. It has been replaced with an independent public-DOS body;
the [contract](contracts/AddBuffers.md) records the observed change/query,
parser, output, return-level and ambient-error rules. Receipt
`artifacts/addbuffers-wb31-reference-20260913-qualified/qualification.json`
passes 27 comparisons on each generated target (68000/020/040), always against
the original executed on 68000. A generated-only allocation failure also passes
on each target: 81 original calls, 84 generated calls, 81 comparisons. All
three HUNKs are 1,344 bytes, five reachable methods, SHA-256
`7e67c35a6aff20de7b7710ee253424e296d8421f281234e835083340733f74ea`.

The 21-case failing candidate baseline and unsupported original-68020 timing
run remain preserved; see the contract for paths. The passing comparisons use
supplied DOS parser/formatter/handler vectors. Actual DOS/handler execution,
Workbench launch, concurrent resident lifecycle, PURE classification and
packaging remain open. The 2026-09-12 six-vector Workbench receipt below is
historical and no longer describes the corrected classic body.

| Gate | Current evidence | Remaining obligation |
| --- | --- | --- |
| External membership | 200 identities; 58 Workbench profiles and 188 MorphOS profiles, 246 total | Other four Workbench disks, machine variants, installed directories and Freeze reconciliation remain open. |
| Original metadata | 258 file records, whole-image hashes, 88 installer P additions, 18 resident lifecycle statements | Installed flags/lifetimes must be observed; ISO POSIX mode is not an Amiga P bit. |
| Original pure requirements | 95 command/profile rows require pure behavior from observed design evidence | Other 151 rows remain unresolved, not classified non-pure. No replacement P bit is approved. |
| Command contracts | Per-command partial contracts and current evidence are maintained in the completion ledger. Both Rename and MakeLink profiles now have independent native bodies; Workbench MakeLink additionally has bounded exact options/output/final-error comparisons. | All options, interactions, diagnostics and native reference outcomes must be closed per command/profile. |
| Existing implementation | Shell owns31 internal names; standalone command bodies and components remain development work, including independent Copy/Rename/MakeLink bodies and the preserved Execute/Shell integration. See the ledger for each current profile rather than treating early component snapshots as the complete implementation list | Script-tail integration, full command behavior, native command entries and profile qualification still need work; internals are not duplicated. |
| Command completion | **0/200** identities, **0/246** profile rows qualified for shipping | All required families CC10-CC38 and final CC39-CC44 gates remain required. |

## Current blocker checkpoint: 2026-09-12

The inventory and media evidence are synchronized with the current goal-plan
hash. `extract`, `verify-media`, and all 13 inventory tests pass for the
verified 200-identity seed: 70 Workbench entries, 188 MorphOS entries, 46
shared identities, and six explicit closure items. The remaining reference
block is the four unobserved Workbench disks and machine variants, installed
system metadata/placement, and the unresolved `Freeze` discrepancy.

CopperStart now publishes and executes the MorphOS pointer-indirect
`FindTaskByPID` entry at `ExecBase-994`; its focused production/guest boundary
tests pass. That closes the ABI publication slice for the Break and
ChangeTaskPri candidates, but not exact MorphOS PID numbering/reuse, target
liveness races, or reference and lifecycle parity. Their profile rows remain
open for those reasons.

The Workbench `Date` slice now has a captured M10 binary contract and a
resident native entry. Its supplied-vector receipt passes seven cases on each
of 68000/020/040 (21 total), including ReadArgs ownership, current-date
formatting, `TO` output, timer-device submission, parser failure, device-open
failure, and repeated use. This evidence is bounded to Copper68k adapters; it
does not qualify original guest parity, MorphOS `Date`, PURE reuse, or package
admission. The durable receipt is
`artifacts/date-native-entry-20260912-qualified/qualification.json`.

The Workbench `SetDate` slice now has a captured M10 binary contract and a
resident native entry. Its 68000/020/040 HUNKs compile under the bounded
`memory=none`, `exceptions=yolo` profile with no managed allocation sites,
exception regions, runtime helpers, or external native targets. The supplied
DOS receipt runs ten cases per CPU (30 total), covering parser ownership,
date fallback, matcher and `ALL` flags, `SetFileDate`, break, cleanup, and
interleaving; all pass with zero shared-image writes. This remains adapter
evidence and does not qualify original-guest parity, MorphOS `SetDate`, PURE
reuse, or package admission. The receipt is
`artifacts/setdate-native-entry-20260912-qualified/qualification.json`; the
contract is in `docs/Commands/Workbench31MorphOS320/contracts/SetDate.md`.

The Workbench `Wait` slice now has an independent bounded resident native body.
Its 68000/020/040 HUNKs pass fifteen supplied DOS/Exec cases per CPU (45 total),
covering short and asynchronous delays, minute precedence, `UNTIL` date
conversion, parser/allocation/timer-open failures, Ctrl-C cancellation,
cleanup and interleaved relative callers. The receipt is
`artifacts/wait-wb31-native-20260912-qualified/qualification.json`. This is
adapter evidence only; original Workbench guest clock/timer parity, PURE reuse,
same-segment lifecycle, package admission and differential comparison remain
open.

The MorphOS `SetDate` profile now has an independent bounded native body and
receipt. Its resident 68000/020/040 HUNKs pass twelve supplied DOS/Exec cases
per CPU (36 total), including the version-gated extended `AnchorPath` fields,
directory traversal, soft-link update policy, diagnostics, cleanup, and
interleaved callers. This remains adapter evidence: original MorphOS guest
parity, PURE/resident reuse, package admission, and differential runs are still
open. The receipt is
`artifacts/morphos-setdate-native-entry-20260912-qualified/qualification.json`.

The MorphOS `Date` profile now has an independent bounded native body and
receipt. Its resident 68000/020/040 HUNKs pass thirteen supplied DOS/Exec/locale
cases per CPU (39 total), covering normal and `TO`/`LFORMAT` output, date and
short-time setters, parser failure, timer open and I/O failures, locale-open
behavior, write failure, cleanup and interleaving. This remains adapter
evidence: the packed identity is now captured, while original-guest parity,
PURE/resident reuse, package admission and differential runs remain open. The receipt is
`artifacts/morphos-date-native-entry-20260912-qualified/qualification.json`;
the combined contract is in
`docs/Commands/Workbench31MorphOS320/contracts/Date.md`.

The MorphOS `Wait` profile now has an independent bounded native body and
receipt. Its resident 68000/020/040 HUNKs pass fifteen supplied DOS/Exec/timer
and signal cases per CPU (45 total), covering short and asynchronous delays,
minute precedence, `UNTIL` rollover input, parser and allocation failures,
timer-open failure, Ctrl-C cancellation, cleanup and interleaving. This remains
adapter evidence: original MorphOS guest parity, PURE/resident reuse, package
admission and differential runs remain open. The receipt is
`artifacts/morphos-wait-native-entry-20260912-qualified/qualification.json`;
the contract is in `docs/Commands/Workbench31MorphOS320/contracts/Wait.md`.

The MorphOS `Beep` profile now has an independent bounded resident native
entry. Its 68000/020/040 HUNKs pass five supplied Exec/Intuition vectors per
CPU (15 total), covering the v33 library open, `DisplayBeep(NULL)`, close and
open-failure paths, ignored command-tail bytes, Workbench startup, and repeat
use. The receipt is
`artifacts/beep-morphos-native-20260912-qualified/qualification.json`. This is
adapter evidence only; original guest audio behavior, PURE/resident reuse,
package admission and differential comparison remain open.

The MorphOS `Lock` profile now has an independent bounded resident native
entry. Its 68000/020/040 HUNKs pass eleven supplied DOS vectors per CPU (33
total), covering ON/OFF/no-action, decimal passkey folding, device and volume
type checks, missing-device and packet failures, parser failure, Workbench
startup, and interleaved repeat use. The receipt is
`artifacts/lock-morphos-native-20260912-qualified/qualification.json`. This is
adapter evidence only; original guest handler behavior, PURE/resident reuse,
package admission and differential comparison remain open.

The MorphOS `DiskChange` profile now has an independent bounded resident native
entry. Its 68000/020/040 HUNKs pass ten supplied DOS vectors per CPU (30 total),
covering DOS 50 startup, `DEVICE/A` parsing, device lookup, the two-stage
`ACTION_INHIBIT` packet sequence, parser/device/inhibit failures, invalid entry
boundaries, Workbench rejection, and interleaved repeat use. The receipt is
`artifacts/diskchange-morphos-native-20260912-qualified/qualification.json`.
This is adapter evidence only; original guest handler transitions,
PURE/resident reuse, package admission and differential comparison remain open.

The MorphOS `Reboot` profile now has an independent bounded resident native
entry. Its 68000/020/040 HUNKs pass ten supplied DOS/Exec vectors per CPU (30
total), covering the empty `ReadArgs` path, ignored command-tail bytes,
Ctrl-C/error handling, `ColdReboot`, DOS-open failure, Workbench and boundary
rejection, and interleaved repeat use. The receipt is
`artifacts/reboot-morphos-native-20260912-qualified/qualification.json`. This
is adapter evidence only; a real reboot transition, original guest parity,
PURE/resident reuse, package admission and differential comparison remain open.

The MorphOS `ResList` profile now has an independent bounded resident native
entry. Its 68000/020/040 HUNKs pass nine supplied DOS/Exec vectors per CPU (27
total), covering the empty and one-resource snapshots, allocation failure,
Ctrl-C, Workbench and boundary rejection, and interleaved repeat use. The
receipt is `artifacts/reslist-morphos-native-20260912-qualified/qualification.json`.
This remains adapter evidence only: complete resource-list population, original
guest parity, PURE/resident reuse, package admission and differential comparison
remain open.

## 2026-09-12 Workbench Eval native-entry requalification

`tools/Commands/qualify_eval_wb31_native_entry.ps1` requalified the separate
Workbench 3.1 literal-template resident entry. It passes twelve supplied
vectors per CPU (36 total) for captured arithmetic, operand reconstruction,
X/O formatting, `TO`, caret handling, parser/allocation failures, repeats, and
interleaving. The static reports contain no managed allocation sites, fatal
machine-fault sites, runtime helpers, or external targets; the audited
`nullable-values` feature remains on the `TO` path, and all runs report zero
shared-image writes.

The durable receipt is
`artifacts/eval-wb31-native-20260912-qualified/qualification.json`.
The HUNKs are 17,052/17,212/16,836 bytes with SHA-256
`e0610dcbd290416d2e7586765ce02715ea388a1efc2eb6921d6a7c73384937a2`,
`c62be6703408643d55b7bf8ea0fc709070418c76e37a40a9239b42be2a473d1e`, and
`a6735151f4b7e6ab34c52c462560ace69a0f99f16052144178c51013ab7bd010`.
This remains bounded native evidence; real DOS parsing, complete classic
grammar/semantics, original comparison, PURE/resident admission, packaging,
and differential gates remain open.

## 2026-09-12 MorphOS Eval native-entry requalification

`tools/Commands/qualify_eval_native_entry.ps1` requalified the MorphOS `Eval`
resident entry. It passes fourteen supplied vectors per CPU (42 total) for
bounded expression and numeric/LFORMAT behavior, `TO`, diagnostics,
parser/allocation failures, Ctrl-C, repeats, and interleaving. Static reports
retain the audited `nullable-values` feature on `TO` and contain no managed
allocation sites, fatal machine-fault sites, runtime helpers, or external
targets; all runs report zero shared-image writes.

The durable receipt is
`artifacts/eval-morphos-native-20260912-qualified/qualification.json`.
The HUNKs are 25,092/25,508/24,860 bytes with SHA-256
`7623121f7fa8215663fcca5e9b380ebf880a3c152b29002cdad0d723d6b3c46a`,
`67c5654c8126f8d05b9a6a093e5dc18f66a7bd43c02de1c24888905862a9247b`, and
`8636f703f5399f534a04970a443182cf0548a1d3dc6b95d3d5c596fca269c04e`.
This remains bounded native evidence; real DOS/filesystem behavior, complete
50.7 semantics, original comparison, PURE/resident admission, packaging, and
differential gates remain open.

## 2026-09-12 Workbench 3.1 List public matcher snapshot

The Workbench `List` profile now has a bounded resident stage using the
16-slot media syntax candidate and the shared public-DOS flat matcher path.
`Workbench31ListEntry` opens DOS 36 and is covered by parser/result/workspace,
matcher, output, Ctrl-C, allocation, unsupported-mode, startup, and
interleaving vectors. The candidate remains syntax-only until the original
Workbench guest and packed behavior are captured.

`qualify_morphos_list_native_entry.ps1 -Profile Workbench31` passes seventeen
supplied vectors per CPU on resident 68000/020/040 HUNKs (51 total). The
durable receipt is
`artifacts/list-wb31-native-20260912-qualified/qualification.json`; the HUNKs
are 8664, 8724, and 8692 bytes with SHA-256
`9247015a1ef22562ea7330a212642c73c12dd6ba9845143a14d6edbe652fd6c8`,
`56ddbd99a38285bd67225f34f11115f4be6bd462454410594ee37713ec45211c`, and
`a83d7a13eaf37929a17f915e4c38e1cc84bf213934835d51082cd6145e638fa5`.
This does not close Workbench differential, PURE/resident, package, licensing,
or shipping gates.

The MorphOS `LibList` profile now has an independent bounded resident native
entry. Its 68000/020/040 HUNKs pass nine supplied DOS/Exec vectors per CPU (27
total), covering empty and one-library snapshots, allocation failure, Ctrl-C,
Workbench and boundary rejection, and interleaved repeat use. The receipt is
`artifacts/liblist-morphos-native-20260912-qualified/qualification.json`.
This remains adapter evidence only: complete library-list population,
packed-binary/source correspondence, original guest parity, PURE/resident
reuse, package admission and differential comparison remain open.

The MorphOS `DevList` profile now has an independent bounded resident native
entry. Its 68000/020/040 HUNKs pass nine supplied DOS/Exec vectors per CPU (27
total), covering empty and one-device snapshots, allocation failure, Ctrl-C,
Workbench and boundary rejection, and interleaved repeat use. The receipt is
`artifacts/devlist-morphos-native-20260912-qualified/qualification.json`.
This remains adapter evidence only: complete device-list population,
packed-binary/source correspondence, original guest parity, PURE/resident
reuse, package admission and differential comparison remain open.

The MorphOS `PortList` profile now has an independent bounded resident native
entry. Its 68000/020/040 HUNKs pass ten supplied DOS/Exec vectors per CPU (30
total), covering empty and one-port snapshots, allocation failure, Ctrl-C,
missing DOS, Workbench/boundary rejection, and interleaved repeat use. The
receipt is `artifacts/portlist-morphos-native-20260912-qualified/qualification.json`.
This remains adapter evidence only: complete port-list population,
packed-binary/source correspondence, original guest parity, PURE/resident
reuse, package admission and differential comparison remain open.

The MorphOS `ModList` profile now has an independent bounded resident native
entry. Its 68000/020/040 HUNKs pass twelve supplied DOS/Exec vectors per CPU
(36 total), covering empty and one-resident listings, ID-string revision
parsing, allocation/parser failure, Ctrl-C, missing DOS, Workbench/boundary
rejection, and interleaved repeat use. The receipt is
`artifacts/modlist-morphos-native-20260912-qualified/qualification.json`.
This remains adapter evidence only: complete resident-table population,
packed-binary/source correspondence, original guest parity, PURE/resident
reuse, package admission and differential comparison remain open.

The MorphOS `Protect` profile now has an independent bounded resident native
entry. Its 68000/020/040 HUNKs pass fourteen supplied DOS/Exec vectors per CPU
(42 total), covering the `ReadArgs` template, protection-bit polarity and
replacement/add/subtract forms, recursive AnchorPath flags, quiet output,
mutation/no-match/break/parser failures and interleaved repeat use. The
receipt is `artifacts/protect-morphos-native-20260912-qualified/qualification.json`.
This remains adapter evidence only: volume/device and soft-link policy,
complete wildcard traversal, exact Workbench behavior, packed-binary/source
correspondence, original guest parity, PURE/resident reuse, package admission
and differential comparison remain open.

The Workbench 3.1 `Protect` syntax candidate now has a separate DOS 36
resident entry. Its 68000/020/040 HUNKs pass fourteen supplied DOS/Exec
vectors per CPU (42 total) through the observed
`FILE/A,FLAGS,ADD/S,SUB/S,ALL/S,QUIET/S` six-slot boundary while sharing the
bounded public-DOS protection body. The receipt is
`artifacts/protect-wb31-native-20260912-qualified/qualification.json`; it
records HUNKs of 5816/5860/5800 bytes and 14 reachable methods for each CPU.
This remains adapter evidence only: exact Workbench behavior, complete
filesystem and diagnostic parity, original guest comparison, PURE/resident
reuse, package admission and differential comparison remain open.

The MorphOS `Status` profile now has an independent bounded resident native
entry. Its 68000/020/040 HUNKs pass fourteen supplied DOS/Exec vectors per CPU
(42 total), covering the `ReadArgs` template, legacy CLI-list traversal,
process and command filters, TCB/FULL formatting, parser and missing-process
failures, Ctrl-C, startup-boundary rejection and interleaved repeat use. The
receipt is `artifacts/status-morphos-native-20260912-qualified/qualification.json`.
This remains adapter evidence only: the DOS 51.51 `QueryCLIDataTags` path,
complete task population, exact Workbench behavior, packed-binary/source
correspondence, original guest parity, PURE/resident reuse, package admission
and differential comparison remain open.

The MorphOS `Touch` profile now has a bounded resident native body based on
the released 3.20 source. Its 68000/020/040 HUNKs pass ten supplied DOS/Exec
invocations per CPU, covering invocation-owned `ReadArgs`, AnchorPath and
DateStamp storage, the classic DOS 3.1 date mutation path, recursive matcher
flags, CurrentDir restoration, direct-touch fallback, verbose/create
diagnostics, Ctrl-C and interleaving. Receipt:
`artifacts/touch-morphos-native-20260912-qualified/qualification.json`. The
DOS 51.66 UTC/POSIX branch, soft-link policy and exact guest/reference
correspondence remain open.

The global shipping blockers are therefore evidence gates rather than a single
compiler failure: exact per-profile grammar and diagnostics, meaningful command
semantics through guest DOS/Exec services, native and PURE/resident lifecycle
qualification, differential Workbench/MorphOS runs, and final image/package
admission are still required. The ledger currently contains 200 `open` and 46
`partial` profile rows in the specification gate; no profile has all seven
gates passing.

See [completion-ledger.md](completion-ledger.md) for every command/profile and
[authorities.md](authorities.md) for reference availability. The media inventory
is deliberately capable of validating facts while returning incomplete under
its strict completion gate.

Later bounded foundation results are recorded independently:
[native parser qualification](readargs-parser-qualification.md) passes its
44 exact explicit-source pairs; [RunCommand input/dispatch work](runcommand-input-context-implementation-notes.md)
passes 87/271 portable and 24/295 native-rejection checks;
[host callback ownership](dos-host-callback-qualification.md) passes 15/32
host checks. The historical native normal-input matrix passes six authored
invocations on each of 68000/040 and fails during 68020 setup. The subsequent
opcode48F9 CPU correction passes 18 focused/633 selected CPU checks; a separate
68020 run with that CPU now passes six invocations using the same HUNK/map.
The historical matrix is unchanged; no homogeneous new three-CPU result is
inferred. The [Close BOOL correction](close-boolean-qualification.md) passes
30/294 portable and five/37 host checks. The separate
[Close write-error correction](close-write-error-qualification.md) passes 45
native cases across 68000/020/040, 323 portable checks, 64 host checks and 14
existing installed/provider checks. Its adapted-before failures and the earlier
native Close failure remain retained; raw Write/FPutC errors and original
failure precedence are not qualified by this slice. The
[constructed-scalar compiler repair](constructed-scalar-qualification.md)
passes 109 focused/289 selected checks after a source-matched failing baseline,
and the retained original Exe2Arc probe passes 77 bounded 68000 entries.
These are distinct source/build/execution closures. The
[retirement owner steps](runcommand-retirement-owner-plan.md) now have
[partial synthetic-owner evidence](task-retirement-qualification.md): 50 new
cases and 34 controls pass. DOS callback/stack handoff, native Switch and
external pointers remain open; the next RET03 host baseline has four liveness
failures and six guard controls. Read-only
[RET03.1 inspection](runcommand-retirement-inspection-qualification.md) passes
31/337 portable and 26/63 host checks; a separate write-trapping adapter then
passes 74 host checks after an 11-failure/63-control baseline. The independent
[FreeMem range query](free-range-inspection-qualification.md) passes 95 cases
after its strict-word-read admission repair. These queries enable no cleanup.
The later [owner revalidation](runcommand-retirement-range-owner-revalidation-qualification.md)
changes five expected stale-binding failures to 152 passing host cases while
rebuilding only the private emulator runtime. It is still read-only admission
evidence; it neither frees a range nor closes the liveness/native gates.
No component count is added to shipping totals.

The separate [raw Write/FPutC checkpoint](direct-write-error-qualification.md)
passes 45 native cases across 68000/020/040, 349 portable checks and 64 frozen
host controls after a retained 22-failure portable baseline and 33-failure
native baseline. It qualifies only checked-byte provider failure propagation;
original DOS packet results, provider reservation rollback and command/runtime
gates remain open.

Exe2Arc's historical RAR4/CAB component checkpoint passes 140 host cases within
433 inclusive tests and 231 native entries across three CPUs. The current
component receipt adds the source ACE scanner/copy path and passes 84 supplied
DOS entries per CPU (252 total); see
`artifacts/exe2arc-ace-native-20260923/qualification.json`. Real handlers,
other formats,
frontend behavior and packed-original correspondence remain required. Original
DOS now passes a normal-boot public list query, but
[boot/task observations](reference-execution-paths.md) retain the historical
application-readiness failure and inconsistent later task lists. A separately
source-qualified Signal state guard passes four focused/16 related checks.
With only the Exec DLL/PDB changed in the original boot replay, all 24 DOS-root
samples have valid lists, versus one valid and 23 invalid before; InitialCLI,
CON and RAM tasks appear. Later read-only tracing identifies original
`C:AddDataTypes` displaying a Software Failure requester for address error
`80000003`, then waiting through original WaitPort/Wait. A preceding original
InitSemaphore call receives an odd pointer; the producing instruction and
actual fault frame still require attribution. This is not an interactive prompt.
Full boot, application/stream readiness and R07 remain unqualified.

## Verified managed and inventory baseline

Executed from `D:/Koodit/GIT/CopperOS`:

```powershell
dotnet test tests/Commands/CopperOS.Commands.Tests.csproj --verbosity minimal
python -m unittest discover -s tools/Commands/Inventory -p 'test_*.py' -v
python tools/Commands/Inventory/inventory.py verify-media
```

Results after the Eval native-arithmetic adaptation: **293 command/Shell tests passed, zero failed or skipped**; **13
inventory tests passed**; deterministic full private-media re-extraction
matched the checked-in inventory and evidence. The full public MorphOS image
matches its published MD5 and SHA-256
`3761e191124cfd204a490915f8d285d55969e95b66c5c3cd1cf854bc71dab911`.

The initial command build failed because the Shell project omitted the SDK
Support assembly. Coherent local/package references repaired that dependency.
The baseline then had 209 passing tests. An independently reproduced ReadArgs
alignment fault was corrected in the existing shared helper: LONG result arrays
are now aligned by their absolute guest address, and capacity/wrap are checked
before any write. The 23 added cases include all 30 helper-backed internals,
four address residues, exact/short buffers, guards and wrap rejection. Options
and Shell ownership were preserved. See [baseline-and-api-audit.md](baseline-and-api-audit.md).

The independently written `EvalNumericFormatter` now has **61 focused cases** for
signed 64-bit decimal and unsigned bit-pattern hexadecimal/octal, prefix/LF,
extreme values, exact/short/invalid guest spans and guard bytes. The focused
61-case matrix also passed with checked C# arithmetic. Three additional
cases each compare 256 mixed high/low-word values against independent host
integer formatting. A warmed host check observes no managed allocation.
This is a numeric-output primitive: it does not implement
Eval's expression grammar, argument handling, full LFORMAT, TO, diagnostics or
native command entry. Its bounded numeric native qualification is recorded
below; full-command and original-command qualification remain open.

## Native foundation

Production support is in `src/Commands/Native/`:

- `NativeCommandStartup` receives Workbench messages, opens DOS through Exec,
  does not assume incoming A6, restores selected IoErr after cleanup and closes
  the owned library. Workbench reply is last, under Forbid, matching original
  startup ownership.
- `NativeCommandArguments` uses an invocation-owned cleared LONG array and
  actual DOS ReadArgs/FreeArgs. It retains DOS pointer lifetimes, distinguishes
  allocation/parser failure, rejects invalid indices and releases once while
  preserving IoErr. See [argument-ownership.md](argument-ownership.md).
- `NativeCommandIo` makes single DOS Read/Write calls, retains their raw results
  and immediately captures IoErr on -1. Its tagged value distinguishes a
  captured zero from no error capture. Input/Output handles remain borrowed;
  Ctrl-C observation through Exec SetSignal does not consume signal bits. See
  [native-io-contract.md](native-io-contract.md).

The non-shipping root is `tests/Commands.NativeRoot/NativeCommandProbe.cs`.
`tests/Commands.NativeExecution/` executes HUNK instructions with Copper68k
1.4.0, checks public vector registers, poisons permitted volatile registers,
injects failures, accounts for guest allocations, protects shared code/constants
and interleaves two processes instruction by instruction over one loaded image.
No original binaries or ROM contents are embedded in those fixtures.

`tools/Commands/qualify_native.ps1` builds actual 68000/020/040 HUNK variants,
checks native closure, records compiler/SDK/image hashes and recompiles for
byte reproducibility. It writes a successful-run pointer only when all targets
pass. The [build manifest](build-manifest.json) grants no installation or P-bit
admission merely because an artifact compiles.

### Compiler correction and first passing native matrix

The initial 68000 probe compiled but failed the Workbench GetMsg boundary.
The same failure occurs with peephole optimization disabled. Generated code
held the message-port value in D1 across Exec WaitPort; D1 may be destroyed by
that call. Generated Finish code similarly reused A0 after Forbid. These are
compiler call-model errors, not a reason to weaken the fixture's ABI checks or
add volatile-register assumptions to command source.

The cause was incomplete external-call clobber information in machine IR and
emitted instruction effects. CopperSharp now accepts additional target-owned
clobbers; the Amiga resolver declares D0/D1/A0/A1. Both backend sites honor that
metadata. Existing constructor signatures and other resolvers' defaults remain
unchanged. The isolated regressions first reproduced both failures in 24 CPU,
runtime and optimization combinations. **33 focused tests and 549 related
compiler tests pass** after correction; the 549 includes the 33. This was not
a full compiler-suite run. Unrelated sibling edits were preserved.

The first complete command matrix then passed:

| Target | Native invocations | HUNK bytes | HUNK SHA-256 |
| --- | ---: | ---: | --- |
| 68000 | 31 | 2,176 | `6c8f43e3caf33f8d557269f3766f2bb36fc87e4a299b0787d7829846ba30a195` |
| 68020 | 31 | 2,176 | `150ffadc45ea51be012080477cd96cd3a933465b7dbb6ceacf01aa43c8c30df2` |
| 68040 | 31 | 2,176 | `150ffadc45ea51be012080477cd96cd3a933465b7dbb6ceacf01aa43c8c30df2` |

All **93 native invocations** passed, including CLI/Workbench, allocation/parser/
write failures, empty and numeric-zero arguments, signed minimum values,
out-of-range result indices, double release, repeated failure/success and
interleaved processes. Each CPU uses one shared relocated image with independent
process storage. No shared-image writes or leaked fixture resources occurred.
Identical rebuilds reproduced each HUNK. Peak observed stack writes were 120
bytes; this does not establish a minimum shipping stack or full OS stack usage.

Receipt: `tests/Commands.NativeRoot/bin/Release/net10.0/qualification/dea8404642094e6fbfb580e1b5bc399e/qualification.json`,
produced by `pwsh -File tools/Commands/qualify_native.ps1`. The receipt includes
individual case traces and input hashes. Compiler backend SHA-256 was
`67a5616281b7f9de3bfa9b84ab11523a8c30bb7ec2b8b36f6dbf82b6c48c692f`;
Amiga target SHA-256 was
`cd094de9c91346d733411c994c2d8d9ce72e71882e878e2fbe39e9817b6c7533`.

Empty-template/count-overflow and additional boundary cases are a separate
lease suite, now executed as recorded below. The generic resident heap-context allocation-failure
result is also still open (`CC02.API04`); the small stack-context probe does
not qualify that larger path. Original OS/parser, registry and boot gates remain
open even though this public-vector fixture now passes.

### Argument-boundary extension

The startup and new argument-boundary suites passed **168 invocations** in run
`f46d48496f14444aacc76471eae8d902`: 31 + 25 per CPU, six independently compiled
HUNK artifacts. Receipt SHA-256:
`b5a92f113bac228f61445600deddfceee8e9ad50a6e81eef68d1ebc60735d809`.
The boundary cases cover empty templates, invalid/count-overflow inputs,
allocation failure at the largest legal request, double release, failure/success
reuse, and overlapping invocations. Both 4096-byte and 16384-byte configured
stacks are used. Observed writes reach 120 bytes for startup and 160 bytes for
boundaries; neither number is a minimum-stack qualification.

Independent review strengthened the fixture to reject calls after CloseLibrary,
missing/repeated/out-of-order Workbench WaitPort/GetMsg, and reads of freed or
foreign allocations. The 168-run receipt includes those corrections. Its input
binding is historical compiled-binary evidence. The source/runtime-bound
qualification below supersedes it after the second compiler repair.

### Historical source-bound qualification checkpoint

The hardened runner subsequently passed **708 native invocations across nine
HUNK artifacts**: 168 Foundation and 540 EvalNumeric. All nine recompiled to
identical bytes. Each mode has a separate successful-run pointer and exact
required assembly set; every reachable assembly hash matches its copied input.

| Mode / run | Source/settings | Managed input files | Restore files | Host/runtime files | Passed stages / input checks |
| --- | ---: | ---: | ---: | ---: | ---: |
| Foundation / `0f4efb1616bb432080849d29d458dd9b` | 436 | 20 | 35 | 196 | 43 / 45 |
| EvalNumeric / `093da10d472c4390be4c167100b41523` | 526 | 20 | 41 | 196 | 25 / 27 |

Foundation receipt SHA-256:
`67fafb9bf511f5dc55a60a7c4842c7f3607ccff2663b7606dbb3028a6696572a`;
input manifest:
`208c18c616f14941ddf7bb38f9759a2b193b76c25d15a9931bb9a8c535f5ee08`.
EvalNumeric receipt SHA-256:
`37f25fb89a21ab8c44aa00b047ce02605bdbbcd59b6086ed79f25769f7f70222`;
input manifest:
`15b7eaf3f4fa55d4e52aaa836a24fa00aba109d50b40efe9bc07801c89d6fdb7`.
Those historical receipts retain their exact artifact paths and hashes. The
[build manifest](build-manifest.json) follows the newer accepted checkpoints.
The qualifier source hash at both historical runs is
`c841e4dd1bcffde65520ba2e6f6596b23ec4766baad1dda92b5544f5eab7ba76`.

Raw source snapshots, forced fresh builds, copied managed dependencies, restore
metadata, runtime/build-tool identities and the actual loaded executor/core
are bound. Live and copied inputs and inventories are rechecked before/after
tools and before publication. Bootstrap failures retain their own stage, exit
and log; the previous successful pointer remains untouched. The six negative
script checks also pass, in run `6ee2601079cf4507a3adaad75ed6052f`, report SHA-256
`b497a1234f1e38b7602cb828989bae04cb868b49483c7d06cff332eef5ec7d3e`.
See [the runner instructions](../../../tools/Commands/README.md).

This checkpoint binds the sources present at each run. Later source/input
changes require a new run; unrelated documentation is outside those closures.
Host/runtime binding is not a hermetic operating-system claim. Foundation's
observed stack writes are 120/156 bytes for startup/boundary; neither those nor
the configured 4K/16K stacks qualify a minimum stack, original OS or P bit.

### Current source-bound fixture refresh

The [compiler-repair refresh](command-fixture-requalification.md) now passes
the same fifteen private HUNKs: Foundation run `40cf9509a3a1480a9c796649ebe50568`,
EvalNumeric run `18c0fa3254384c3287734b3537a17510`, and MakeDir run
`1fe8f38d9e6a497eb675ed3a94459bc5`. There are 921 generated native calls,
114 original MakeDir calls and 114 supplied-vector comparisons. Every artifact
has a byte-identical rebuild. The manifest records these captured inputs and
retains the prior manifest separately. No shipping or P admission changes.
The first long-path Eval run remains failed; its counts are not added here.

### Historical source-bound fixture checkpoints before compiler repairs

Foundation run `5b2854ee3ef04f71ad6faab7d95add17` passes **264 native
invocations across nine reproducible HUNK artifacts**: 31 startup, 25 argument
and 32 I/O cases per CPU. MakeDir run `012c05cf6707457a9f288e8b862dbcf4`
passes **114 original/generated supplied-vector comparisons**: 38 original
and 39 generated invocations per CPU, with the additional generated case
testing result-slot allocation failure. These counts are separate: 114
original invocations, 117 generated invocations and 114 comparisons.
The fixed-base EvalNumeric run `e767705fdcaf42fb864c4c68c8e3d9a3` adds
540 passing numeric invocations across three reproducible HUNKs. Together
these checkpoints contain **15 private HUNKs and 921 generated native
invocations**, plus the separate 114 original MakeDir invocations.

These recorded artifacts predate the narrow-field and RunCommand-bridge
compiler repairs below. Their source/binary binding remains valid for the
captured inputs; it does not qualify artifacts rebuilt with either newer
compiler. All three modes required a refresh at that checkpoint; the new
source-bound runs above provide it. The manifest admits no shipping or
pure/resident artifact.

| Mode / run | Source/settings | Managed files | Restore files | Host files | Private references | Passed stages / input checks |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Foundation / `5b2854ee3ef04f71ad6faab7d95add17` | 441 | 20 | 35 | 196 | 0 | 61 / 63 |
| EvalNumeric / `e767705fdcaf42fb864c4c68c8e3d9a3` | 526 | 20 | 41 | 196 | 0 | 25 / 27 |
| Workbench31MakeDir / `012c05cf6707457a9f288e8b862dbcf4` | 442 | 20 | 35 | 196 | 1 | 25 / 27 |

Foundation receipt SHA-256:
`ffc05feaa384f4a27bbc80349b7d1a6a77c873856c23cff1e0c66ec55b002ff3`;
input manifest:
`6c587c66c8bd08153f82f56db50eff0acd94fdc1b673647a5158cda802aeabaf`.
EvalNumeric receipt SHA-256:
`e1e1dcf4c5ccaeee7d754bcac0a899f29b56d72c47bc4174c89e6a1234cf8e08`;
input manifest:
`0e96105f5c014a0e6db8e951caf40a3f73edd8c6f9217e9d0737c39e88b455bb`.
MakeDir receipt SHA-256:
`e59c8ed1d4f9e2f7a1e7186f301a3feb3552e503e66ef8a1c6152d8d66cbc266`;
input manifest:
`65892f5a53c626207c71934e882cf46453ca5340642e90f2314732c70689c559`.

All runs retain source, compiled dependency, restore, executor and host/runtime
bindings, including checks before and after execution. The MakeDir reference
is the external 464-byte original, SHA-256
`23911db49742055d8bddcfe5f8de82cbb2232f8a7ef850d51bfd27f9b54c819b`;
its bytes are never copied into the repository or qualification snapshot.
The native SDK and the executor's package SDK are separately identified as the
actual distinct inputs; a matching name is not treated as matching bytes.

The I/O compiler reports one helper: an internal shared return tail. The
qualifier admits only its exact ten bytes and label in this specific fixture,
after checking the real HUNK method symbols, matching prologues, terminal
branch targets, code boundary, relocations and trailing constant data. The
report retains the count of one and the byte-level admission proof. This
does not approve an external runtime dependency or any other helper. All
**37 pipeline regressions pass**, including two positive controls and 35
rejection cases; report SHA-256
`2a2aa4ec10f1a1b569eca39e840dcdb3bb0e0c66dd0ecd7957e14ad3401ee5e0`.
Earlier rejected runs remain failed, including the exploratory frozen-artifact
executions that motivated this precise check.

Configured stacks are 4K and 16K. Peak observed writes are 120/156/84 bytes
for startup/arguments/I/O, 80 for original MakeDir and 140 for generated
MakeDir. Complete command entry must restore SP; other entry registers are
observed according to the original NDK startup contract. Public library-call
arguments, volatile clobbers and callee-save obligations remain checked.
Shared images were protected, and repeated/interleaved callers retained
private resources. None of these measurements qualifies a minimum stack.

The MakeDir executor supplies parsed arguments, filesystem outcomes and output
vectors. It establishes command control-flow/error/ownership correspondence
for those cases, **not** actual DOS parsing, wildcard/filesystem behavior,
original fault rendering, Workbench launch or installed purity. The next real
launch is specified in [native-command-launch-contract.md](native-command-launch-contract.md):
public CLI/Input/Output readiness, then original RunCommand with normal input
buffers and NULL-D3 ReadArgs. Explicit RDA_Source tests cannot close that gate.

## Original/generated DOS parser component

The existing CopperStart DOS artifact/receipts were stale relative to both
their old byte counts and the corrected compiler. The real owner pipeline was
run without modifying DOS implementation source:

```powershell
# From D:/Koodit/GIT/CopperStart
pwsh -File build/Build-DosNativeArtifacts.ps1
pwsh -File build/Build-DosNativeArtifacts.ps1 -SkipBuild -ManifestOnly
```

At that earlier checkpoint both passed. The resulting
`artifacts/native/CopperStart-DOS-Unified-68000.hunk` was 867,728
bytes, SHA-256 `2a008cc1ef4dcfc5896a3d958a1454b5d86f23f0d7e833019856257d2f359286`.
`dos-build-inputs.json` SHA-256 is
`e060cc14abd0f213f6366890bfed53dfe0be9e2bb3930357eea0f0f292abcee4`;
`dos-manifest.json` SHA-256 is
`823673bc44a786fe26e4849e1435d9de3bbefad8cfd6d3d8a1466ca5847f4c92`.
The owner's 68020/040 outputs are assembly smoke artifacts, not executable HUNKs.
These receipts qualify build provenance, not a new DOS semantic/reference pass.
These are historical receipts, before the argument-home compiler correction.
The next component test must execute the actual native parser and reject any
stale input or silently skipped reference. See
[reference-execution-paths.md](reference-execution-paths.md).

After that compiler correction the owner build succeeded again, producing an
869,272-byte HUNK, SHA-256
`f237e0dabb4b4ab6a22022193eaec952e348b4887a868922b69885a4918715de`.
Its immediate manifest-only revalidation **failed**: four shared managed DLLs
had been rebuilt after capture. This later artifact is not accepted as a
current parser input by relabelling its receipt. A serialized owner rebuild
has since reproduced those bytes and **passed manifest-only revalidation**.
The new coherent input receipt is
`3d509d4e7f4325cfd3127c98d3bc96169ba60cba5fa5ca0cc394b3f377d400a5`
and the DOS manifest is
`dff089c11420433238fd95951104b7f37bbf5c94d2a2a39873fdac3c117888be`.

The strict component test has since executed all **44 original DOS 40.3 cases**.
The sanitized original-only receipt is
`D:/TestData/CopperOSCommands/ReadArgsDifferential/original-readargs-v4063-512k.json`,
SHA-256 `5aeb007a1cf6acb06fcdec1f6cc6bba20498b46157ab1aa747aff7b949c28e90`.
It explicitly records zero generated cases and no passing comparison. Original
results include unchanged success IoErr, exact switch bits, newline boundaries,
quoted escapes and `/N` signed-boundary bit patterns.

The generated image's largest segment exceeds the default 512 KiB bank size.
With the public 2 MiB fast-RAM option, strict original-ROM readiness initially
failed before generated DOS could start. Tracing found that two emulator-private
scheduler gateways were registered outside original Exec's real negative table,
masking a free-memory chunk. The raw RAM field held 216 while the bus returned
gateway bytes as an invalid large size. The fix is at the emulator gateway owner,
with a scoped unmapped continuation claim, collision checks and token-owned
teardown. Its 17 focused cases pass, including a licensed original-Exec 1 MiB
allocation/free in 2 MiB fast RAM. The original 822-byte negative table is
unchanged and the largest free block is restored after freeing. No fabricated
MemHeader, ROM modification or parser normalization is used.

At that checkpoint the strict test passed provenance, repeated all 44 original cases
and loaded the generated HUNK through real Exec allocation. Generated parsing
was unexecuted: the native library installer left its code after 881
instructions and does not return within the existing one-million-instruction
bound. This was an initializer failure, not a completed parser comparison or
an allowed parser mismatch.

The native trace identified the owner: installed-library discovery read a
private generated-DOS extension from unrelated original expansion.library,
then dereferenced a bogus owner before initialization. CopperStart now checks
the public library type, exact generated layout/version, complete PUBLIC-memory
spans, sealed owner identity and backlinks before private access. Forty-nine
focused owner/discovery tests pass. The native fixture also invokes installation
twice, requiring the same library without a second owned initialization; that
new native requirement is not passed by the managed tests alone.

A live owner rebuild generated all three outputs but correctly rejected them
when shared dependency DLLs changed during compilation. PDB identities showed
that another build through the `C:/D-drive/` alias had rewritten the same
physical outputs. No other task was stopped and no provenance check was
weakened. A private snapshot of 761 source/settings files, covering nine
projects in CopperStart and CopperSharp, then ran the unchanged owner build
and unchanged `-SkipBuild -ManifestOnly` successfully. The accepted private
68000 HUNK is 871,040 bytes, SHA-256
`9a9a69046c0595e59e90f087a2f8be948f4ee4a49d8a1b84555a3c7bde41b403`.
Its build-input and DOS manifests are
`6cc865e562a8b4a19bf670eb2dcccd20c8d70f0b766df167707041d9785a3e46`
and `5918c62ef79138d6eba5501eb79640a24b03dfcbb54c3ee95df56a9d6c6ac565`.
The full isolated-build receipt has SHA-256
`10a588732c6473ce3054e304f5dd9170cb39ecc374e338b77d3e56d0b989bda6`.
No original command or ROM bytes were copied into that build snapshot.

Before the strict test could execute, its separate live host-test DLL also
changed. That preflight stopped without executing a case or reusing the older
receipt. A new private host snapshot then froze 119 test sources, the exact
test-SDK program and 40 dependency DLLs. Its test assembly has SHA-256
`7899455419ad8c5edc1025c5632061befd8526b6712db6d3c54c5c4726f01871`;
the emulator remains `453de5eaac3fc284ce954b1b10e0d08b619c7d9100a06b583c806a412be0edbd`.
All 17 gateway regressions pass on that bound private host.

That preceding strict run is **failed**, with **44 complete original observations and
25 complete generated observations**. Generated installation returned after
169,739 instructions; a second call returned the same DOS base `$002A3EE8`
after 6,620 instructions without repeating owned initialization. Those are
native installation/discovery observations for the captured image, not parser
passes. The 26th generated case, `multiple-one`, returned from ReadArgs but
failed the host's mapped-string check while copying its `/M` result. It did
not produce a complete observation. All 25 preceding generated observations
also differ; no mismatch is normalized or allowlisted. The strict receipt is
`18c39884481edffd7634e97deff70ca934ed234809547eaa6f8e41efa31d0959`
and its TRX is `33dc162ec9c00ee11c295b490da72ec81bc1c9991e18bd3cdfc59a0f23c8a435`.

Disassembly of that exact HUNK identifies a compiler prerequisite: the
24-byte ReadArgsEntry stores its byte-backed flags with MOVE.B at offset 16,
but a by-value field read uses MOVE.L there, losing the low modifier bits on
the big-endian target. The compiler correction preserves the structure layout
and source enum: the same 36 native cases fail before/pass after; 12 further
padding/bool/char/zero-store cases pass within 389 related checks. See
[compiler qualification](compiler-qualification.md). See
[reference execution](reference-execution-paths.md)
for private roots, unchanged-input checks and retained failures.

The next private candidate replaces exactly the two tested compiler emitter
files while retaining the other 759 source/settings files. The unchanged
owner build and independent manifest-only validation both pass. Its 871,960-byte
68000 HUNK has SHA-256
`e40796f59eaf2a050c873a70d3feb1a77e7dba9cb2b3add23b66cfb1ad7e868f`;
the build-input and DOS manifests are
`6142eeef6556a618a0090debd104f9f16b45f0f07a049523d848b73885394e44`
and `61ff20c01555c383b9dbef869ec1b20054afcfc5eef5b03a0b75ed34584a23b8`.
This explicit derivative excludes the later RunCommand bridge changes.

At that checkpoint the strict comparison completes **44 original and 44 generated
observations**, with all original results unchanged. Both native installations,
vector/code immutability checks, expunge and loader cleanup pass. Initial
installation takes 169,756 instructions and repeated discovery 6,624,
returning the same DOS base `$002A4278`. The test still **fails at exact
semantic comparison**: only `required-zero-source` matches all fields.
Overlapping differences are return 5, IoErr 35, cursor 41, copied values 8,
FreeArgs-called 5 and cleanup IoErr 35. The former `/M` result-copy failure
is gone; switch bits, success IoErr, LF boundaries, plain required keywords,
`/M` required backfill, positive 2147483648 and unterminated-quote errors
now have complete native observations for the parser owner. No mismatch is
normalized or permitted. Strict receipt SHA-256 is
`040acbb21326a9c659316827f1cd96699c708a5218b6dd2678e0c5c8cd53ca74`;
the [paired result summary](D:/TestData/CopperOSCommands/BuildSnapshots/readargs-dos-fieldload-20260830T141652Z-bab02b59/readargs-test-results/native-readargs-fieldload-summary.json)
is `f9844aa9cee6840b9211018a6e40cfb52045b5e4bec8c63244e30d7b1c87d53b`.
This remains explicit-source component coverage with zero command/RunCommand
calls, not normal CLI parsing or a full Workbench/MorphOS differential pass.

The bounded parser-owner correction is now frozen at DosCore SHA-256
`f908d2ff10b0b0f124b9bcfb1f22cf4befbce6da58c82c625ac2e98f27d72a3d`.
The unchanged 44-row original-derived host suite changes from 43 failed/1
passed to 44 passed, making 87 calls including its complete ordered replay.
All 107 focused host rows and two separately counted exploratory installed
native-family tests pass. Public ReadItem/StrToLong and DosCliCore remain
unchanged. The adapter owns an added LF copy when its Shell span lacks one;
public explicit CSource boundaries retain the measured behavior. Wider numeric
bounds, typed backfill, defaults and real CLI/prompt behavior remain unqualified.
The [host repair receipt](D:/TestData/CopperOSCommands/ReadArgsHostRegression/20260830T143234Z-888d80ce/completed-repair-summary.json)
is `4d172e34d185ca850823054ab12d327a851b0bab55a5ca1f0162a4eb4abdf9bd`.
A new private DOS candidate takes exactly this frozen source plus the other
760 unchanged files from the field-load snapshot. Its strict native replay
now passes: **44 original and 44 generated observations, 44 exact pairs**, with
zero return, IoErr, cursor, value or cleanup differences. Native installation,
repeated discovery, code/vector integrity and final cleanup also pass. HUNK
SHA256 is `5014fce66228ab170f498474248b387c5aea40f80e39c0404c8cf1cb3280004f`;
strict receipt is `8647612781105ce75818bbfe060e76f38ff28375e667a8b550d37d5e6f191a49`.
The [parser qualification record](readargs-parser-qualification.md) binds the
unchanged corpus/host and preserves both preceding failed native receipts.
This test runs no C: command or RunCommand.

The separate original-DOS command-launch readiness test accepted R01 and
failed R02 because Input and Output were both zero. The existing real Process
`$00205EE0` has a readable CLI at `$002028B8`; no replacement Process was
fabricated. All four public queries retained task/SP and original vector
identity. Receipt `e0c0790fb501fb76b00ef2b815af814fa1485464c08eec55784b7655723264e4`
contains zero command, ReadArgs or RunCommand invocations. Public stream
acquisition subsequently passes in a separate R04 run: original Open obtains
both NIL: handles, selections are restored before exactly-once Close, and
all 19 recorded calls retain task/SP and unchanged original vectors. Receipt
`e3d434be39f8687977d585f61faa14f3f8b42047c0fed200c135223ae6a76940`
still has zero command/ReadArgs/RunCommand invocations. The initial failure
and later pass stay separate in the [launch contract](native-command-launch-contract.md).

A separate original-DOS run subsequently executes one authored observer with
a 25-byte quoted/escaped tail. Original RunCommand supplies D0/A0 and GetArgStr;
ReadArgs(NAME/M, result, NULL) returns `alpha`, `two words`, `a*b`. One FreeArgs
follows the live value copy. It returns 42 and restores the observed task/CLI,
argument, NIL: buffering and stack state; all original vector/code checks pass.
Receipt `8436167bea33f4e10013fab34a01919c419bf0425cb0201bf62e7ac7c1955f29`
records one observer and zero distribution/generated command invocations.
Exact R05/R06 subsequently pass in a separate two-case receipt
`9ffd6e9d09f9e6b724f9bbc769ba5927907dbd5acdac9deb622572f19457ca2a`:
LF-only yields no /M list, and `alpha beta` plus LF yields those two strings.
There are two more authored observer calls, each with one ReadArgs/FreeArgs,
42/IoErr 0 and restored observed state. Nonempty caller-stream restoration,
delivered output and normal user-mode/full boot remain open; inherited SR is
`$2000` in these component runs.

The separate compiler stack bridge now passes **29 focused checks and 418
inclusive related checks**. Twelve CPU/peephole/runtime configurations perform
36 successful native calls, including interleaving and reuse, while a native
command overwrites D1-D7/A0-A6. Twelve retained ordinary-call controls reproduce
the old-frame read after StackSwap, and five malformed import signatures are
rejected. The bridge preserves the result through the second public Exec
StackSwap and recovers its descriptor using SP. The Exec vector remains a
fixture in these tests, not original Exec execution. Receipt SHA-256 is
`dc51a073634ab8b391c7eed3b0c369f30807be62e65e9ec3407a084dac241b94`.
The actual CopperStart callback integration subsequently passes 12 focused and
39 inclusive related checks: six CPU/peephole configurations make 18 successful
native command calls, with twelve additional non-started callbacks. The same
final test source first recorded six old-path failures and six control passes.
The callback method's outer ABI, returned result, descriptor/task stack bounds
and shared-image integrity pass. Receipt
`1cc7928b905d5f6228b24abfd97bee43cef93fc163c59f60273fd32a06b81ead`
retains separate harness/setup failures. Exec remains a vector fixture; input
buffering/GetArgStr restoration, failure rollback and Exit/nonlocal unwind are
still separate obligations.

The first actual MakeDir input comparison through **original DOS RunCommand**
also passes on 68000: two original 37.2 invocations and two unchanged generated
invocations give two exact comparisons. LF-only returns 20/IoErr 0 with one
FreeArgs and a usage VPrintf request; an unterminated quoted line returns
20/IoErr 120 with no FreeArgs and one PrintFault request. ReadArgs uses normal
input with D3 NULL. Task/SP, CLI, argument and NIL: buffer state restore, and
code/vector/argument integrity and cleanup pass. The
[reference record](reference-execution-paths.md) binds receipt
`ae22f088057f5d44b9b167192f08060bb680a334b6a99316e3b2fe7f79dd3394`.
These are four separate fresh-machine calls using the earlier qualified
generated HUNK, not a new compiler qualification. No filesystem handler or
delivered diagnostic bytes are captured; inherited SR is `$2000`, not a normal
user-mode/full-boot result. MakeDir's complete command gates remain open.

A separate new capture passes the launch contract's exact A03 quoted input,
opening quote plus `unterminated` and LF (fourteen bytes). It adds one original
and one generated call, with one exact comparison and no A01 repeat. Both
return 20/IoErr 120 with failed ReadArgs, one PrintFault request and no FreeArgs;
all state/ownership/integrity checks pass. Receipt
`42383f47cf3ef2a1f1c2738592245464ef47cea4f738da46bb796dfad573f591`
preserves the preceding seven-byte variant separately. The combined count is
six original-DOS RunCommand invocations: three original commands, three
generated commands and three comparisons. Two comparisons cover exact A01/A03.

## Historical Exe2Arc header component

The independent Exe2Arc RAR4/CAB header component now passes 58 host cases within
351 inclusive Shell/command tests. A review found that the initial span check
rejected a valid window ending at the final uint byte; two new cases failed
before correction and pass afterwards, alongside two wrap-rejection controls.
Its [contract](contracts/Exe2Arc.md) records exact source/test hashes and new
host receipt `02090c22712afe8247826b7129dd48fb7dbe10cbe71feb8f750478b5943523aa`,
preserving the preceding 54/347 checkpoint separately.
The frozen `dd79842d...` predicate subsequently passes 228 native invocations,
76 per CPU on 68000/020/040, including 36 interleaved callers. All three HUNKs
reproduce byte for byte; guards and one protected shared image per CPU pass.
Ten separate negative controls reject invalid inputs/reports and deliberately
faulty native bodies. Receipt
`8a23cfefb17352f25e33f465352d2e983d755bc87f8a2b0c4cbd4e5c19db0621`
binds the managed/package/compiler/runtime inputs; it is not a fresh compiler
source rebuild or an original packed-command comparison.

A subsequent source audit found that strict greater-than-7/20 eligibility
belongs to the scanner's window, while a later candidate in an admitted window
may end exactly at EOF. The predicate is corrected separately: six new cases
fail before and pass after, within 64 focused/357 inclusive host checks. Two
older strict-equality expectations are explicitly corrected. Receipt
`e9909a365d105926106ebb2e6eeb25946a47fec593aacb1f4ce82d9f0e508b16`
does not claim native qualification for the corrected source. The earlier
228-call checkpoint remains evidence for its frozen helper, not scanner parity.
DOS scanning/copy, command behavior, original comparison and shipping/purity
remain open.

## Classic Rename reference execution

The unchanged original-only RN1-WB executor now observes **40 returned cases
and eight separate guarded hazard stops on each of 68000/020/040**. The
[CPU correction](cpu-byte-postincrement-qualification.md) passes 144 focused
and 857 inclusive checks, with 60 failures reproduced before the two-file
repair. A separately captured CPU-only rebind completes the original matrix;
the 68000/040 records remain unchanged. The historical 68020 failure after 16
returns, and twelve receipt/input-safety checks, remain retained.

These observations cover supplied library/matcher values, not original DOS
parsing, matching or filesystem effects, and implement no generated Rename.
CPU sources and consumed binaries are bound, but the unchanged executor is
not newly rebuilt. The [Rename contract](contracts/Rename.md) preserves branch,
cleanup, provider and unsafe-path limits. Its next required slices are actual
MatchEnd ownership behavior and original CLI/parser/handler observations.

## Eval native arithmetic work

The first native compilation exposed the compiler's deliberately limited
64-bit arithmetic support. The formatter retains its public `long`/`ulong`
interface, uses existing `M68kRuntime` register-pair split intrinsics and converts
with 32-bit words. Decimal uses 16-bit division steps; hexadecimal/octal use
bit extraction and shifts. It needs no managed integer-format
runtime or floating point. The managed 61-case matrix passes this implementation.

The initial private numeric component root compiled as a 2168-byte MC68000 HUNK with
zero managed allocations/runtime helpers, no external native targets, and no
shared writable RAM/BSS. Actual execution then exposed a compiler error:
mutating a scalar parameter through `ref` updates its stack home, but subsequent
reads still used the incoming register value. The guarded test rejected the
resulting backward write past the numeric output buffer. The compiler now reads
an existing 32-bit argument home after address exposure. All 18 isolated native
cases failed before and pass after the correction; a 99-test related subset
passes, including those 18. Narrow/wide mutable homes remain outside this fix.
See [compiler-qualification.md](compiler-qualification.md).

The historical source-bound EvalNumeric run `093da10d472c4390be4c167100b41523`
then passed its execution matrix:

| CPU | Invocations | HUNK bytes | Peak observed stack writes |
| --- | ---: | ---: | ---: |
| 68000 | 180 | 2,132 | 200 |
| 68020 | 180 | 2,012 | 184 |
| 68040 | 180 | 1,992 | 184 |

One protected image per CPU serves repeated and instruction-interleaved callers.
There are no host gateways, external native calls, managed allocations/runtime
helpers or shared writable RAM/BSS. Actual decimal/hexadecimal/octal bytes match
independent host integer oracles, including high/low-word and capacity/wrap
boundaries. MC68000 physical-limit cases use 24-bit bus addressing; MC68020/040
use synthetic 32-bit boundary regions. The public formatter signatures remain
unchanged. These projects do not install an Eval command or close its expression,
LFORMAT, TO, startup, reference, minimum-stack or required purity gates.

The later zero-fatal-site guard rejected unchanged numeric bytes in run
`b02c8f99d14a406f875908c29f2cc3d5` before execution. Its reports and the
older passing reports both contain six ILLEGAL sites; the old runner did not
check that field. Disassembly identifies six zero-divisor guards in the private
variable-radix routine. Public callers supply only 8, 10 and 16, and native
register/data-flow inspection confirms those values remain intact. Valid public
inputs cannot reach those guards. The failed newer receipt remains failed.

The formatter now specializes its fixed bases: literal decimal divisor 10,
low-four-bit hexadecimal digits and low-three-bit octal digits, shifting two
32-bit words for the latter two. Public `long`/`ulong` signatures, buffer
validation, exact bytes and no-allocation behavior are unchanged. Existing
tests pass 61 focused cases, 61 after a checked-arithmetic rebuild, and the
full 293 command/Shell cases without skips. The tests were not rewritten to
match this arithmetic. The source hash is
`9167c6d699a065dab0ac143fd325ea999a27b60897740026d916e9b690e86233`.

Fresh source-bound run `e767705fdcaf42fb864c4c68c8e3d9a3` passes **540
native invocations**, 25 stages and 27 input checks, including the unchanged
strict **zero fatal-machine-fault sites** requirement on every artifact.
There are no runtime helpers/features, exception regions, external native
targets or managed allocation sites. All three HUNKs reproduce identical bytes.

| CPU | Invocations | HUNK bytes | Peak observed stack writes | HUNK SHA-256 |
| --- | ---: | ---: | ---: | --- |
| 68000 | 180 | 2,184 | 192 | `5c5d2c5cd13414f9cdfad6f1ebd76d45acff59fd2aab34cfd8a45846c70fbc6f` |
| 68020 | 180 | 2,076 | 180 | `dd4c068dd3d6bed0f0aa2f4ab1ce61909154ffa25dea08c586b923d46132e98f` |
| 68040 | 180 | 2,064 | 180 | `4fee1ac99ec9f5cc55d254e33014c270be8f1d24ffa9608dda09d0bad1d19f53` |

Each CPU executes one protected image with sequential and interleaved callers,
4K/16K configured stacks and no host gateway. Exact output bytes still match
independent host integer oracles. This passes the numeric primitive only;
original Eval and full command semantics remain unqualified.

## Evidence boundaries and remaining native gates

The vector fixture supplies explicit ReadArgs results to test native calling
conventions and ownership. It **does not parse the command line** and cannot
establish original DOS grammar, help continuation, original-command parity or
full boot. A native instruction pass is not a shipping command pass.

The explicit-source ReadArgs corpus passes as recorded above. Required
follow-up includes broader parser behavior, generated DOS command launch with
normal input buffers and newline handling,
Workbench/CLI process teardown, low-stack/context failure paths, Shell resident
registry lifetimes, actual image staging/protection readback, and original
Workbench/MorphOS command comparisons. Networking, archives, interpreters,
hardware and filesystem providers remain required work under CC07 and their
command families; they are not silently deferred out of scope. The
[archive provider work package](archive-provider-work-package.md) gives all
16 CC31/CC32 commands concrete service/source/license owners and nine bounded
implementation slices. Exe2Arc's source-backed contract can progress without
XAD; source/binary correspondence and the named safety discrepancies remain
open. No archive implementation or vendor code was added by that audit.


## 2026-09-08 Initial MakeLink acceptance and release-preflight checkpoint (historical)

The unchanged current Workbench68000 HUNK has nine paired real ReadArgs option
cases and five paired redirected-output/error cases. Exact saved diagnostic bytes
and final owning-process secondary results match the original; successful calls
retain IoErr205 in these fixtures. Five aliases from the option fixture have
independent post-removal payload readbacks. A separate existing-trace validator
also verifies the final process error after public RunCommand return.

Actual command/DOS stack sampling covers five error/recovery calls on a published
4096-byte stack:896 observed bytes below its upper bound,3200 SP headroom, no
changed/unproven/outside-bound user samples. This explicitly does not admit a
universal minimum stack or prove every stack write. See the current MakeLink
contract and hash-bound development records for scopes and raw evidence.

The read-only CC08 release preflight passes22 adversarial tests and rejects all27
current qualification/development records as release inputs. It has no complete
release-gate adapters and cannot authorize staging; version/dependency metadata,
full evidence producers, image generation and Amiga metadata readback remain
required work. This infrastructure does not promote a bounded report into full
command qualification. Shipping remains0/200 identities and0/246 profiles.

## 2026-09-08 MakeLink exact-image OS and native CopperStart checkpoint

This later checkpoint supersedes the earlier absence of any tagged-image OS or
native CopperStart command evidence. It does not convert these bounded tests into
full profile, system, filesystem, stack or PURE admission. Current release-adapter
support is tracked separately in [release-preflight.md](release-preflight.md).

The new 3232-byte versioned 68000 HUNK
`12d1f36504f5a0f7099bc419e4fffc5f43f138d7b4e8745a2ca907bcb73b5f45` completes
five original-Kickstart-DOS error/recovery cases. Exact output, final errors,
cleanup and independent post-removal aliases are verified. Original C:Version
40.1 inspects the exact file and relocated CODE, returns 0/0, and produces the
complete independently read 27-byte `MakeLink 0.1\nCopperOS wb31\n` output. The
embedded date is not rendered by FULL here. Fourteen controls reject corruption
through the full verifier; twelve retain mutually consistent TRX/observations,
with failed-terminal and stale-capture checks separate. Bound evidence:
`artifacts/workbench-makelink-versioned-20260908/version-boot-complete/verified.json`
(`88e1928b49755de17e5a364f22619bdcf9134f8ab999469a881678f82191ba0c`) and
`controls.json` in that directory
(`56d818673650dbcc29999bd43ba055c8af478d1a4b0e87ce23ac932f190e3261`).
Only the new versioned 68000 record receives this evidence.

Separately, the retained unversioned 3184-byte fd42d840 candidate executes through
native production CopperStart DOS RunCommand, ReadArgs, formatting, mutation and
packet-provider code. Exec and the bounded backing handler remain fixtures. Five
invocations return 0/0, 20/203, 20/205, 20/116 and 0/0; success IoErr 0 is the
fixture provider's result, not a replacement for original DOS's observed 205.
Public DOS reads prove both aliases retain exact payload after target deletion,
the duplicate destination survives, caller streams remain usable and resources
balance. Command/DOS code and installed vectors are unchanged.

The native run exposed and verified fixes in shared CopperStart owners:
`ExecRawDoFmtCore` passes full string length directly instead of making an
unaligned word access to an odd-address byte field; `DosErrorCore` supplies error
116's required-argument text and error 203's observed lowercase text. Six tests
pass: the five-invocation native test and aligned formatter lengths 0, 1, 255, 300
and 1024. Evidence `artifacts/copperstart-makelink-native-integration/verified-final.json`
has SHA-256 `90782dbb243afa83d5141c04b1ae4d7b0e5d3f42c657d5453fdb911fd7b44372`
and attaches only to the retained fd42d840 68000 record. The 16384-byte fixture
stack does not qualify the declared minimum; no full CopperStart boot is claimed.

The [MakeLink contract](contracts/MakeLink.md), retained development records and
separate versioned records state the remaining exact-image requirements. Older
failed receipts and narrower checkpoints remain historical. Shipping/PURE gates
stay open: 0/200 shipping commands and 0/246 shipping profiles.

## 2026-09-08 Corrected SDK and production filesystem checkpoint

The [filesystem integration record](copperstart-filesystem-integration.md)
supersedes the earlier absence of versioned-command production filesystem
evidence. The original FileHandle `fh_Arg1` is now correctly at byte 36,
`fh_Arg2` remains at 40, and 128 focused SDK/native checks pass. Fresh command
builds in `artifacts/workbench-makelink-filehandle-abi-20260908` bind the corrected
SDK and all 453 current source inputs, reproduce identical HUNK bytes on all
three CPUs, and pass 60 supplied-vector invocations. Qualification SHA-256:
`fcebfca783daef8febb3c93fac71f47fff48510693d9d700b44b356941b00305`.
Older SDK-bound builds remain historical; the unchanged exact-image
original-DOS/Version receipts retain their separate scope.

The versioned 68000 HUNK now also completes five native CopperStart DOS calls
using the production configured packet dispatcher and host filesystem. Twelve
tests pass, with 496 production dispatches, exact diagnostic/error results,
independent alias readback after target deletion and balanced ownership.
Receipt SHA-256:
`e98f576b190fb1ff166dc095cfc932727786fb4a85525fce48bc8c6c5fc7057f`.
Exec allocation, delivery and StackSwap remain fixtures. Separately, one
managed production `DosServices` test verifies two published roots and six
genuine guest queued packets with exactly six replies and two lock frees;
receipt `a2ae8a87ac690af3ddac094707159f8e2d058f21bcf5f9e693c0f7d3a656949a`.
The broad MedPlayer test build's unrelated duplicate-alias failure is retained;
the isolated owner pass is not a complete test-suite pass.

These distinct executions do not form a combined native Shell launch or full
CopperStart boot. Complete runtime/dependency, minimum stack, PURE/resident,
staging/protection and shipping gates remain open. Shipping remains 0/200
commands and 0/246 profiles.

## 2026-09-12 SetClock source and native-boundary checkpoint

The MorphOS `SetClock` profile now has a source-bound contract and a bounded
resident native body. The body uses the source `LOAD/S, SAVE/S, RESET/S`
template, shared DOS `ReadArgs` ownership, `battclock.resource`, `timer.device`
VBlank request setup, classic battclock vectors, timer `GetSysTime`, and
reverse resource cleanup. Its three CPU HUNKs compile with zero managed
allocation sites, runtime helpers/features, external native targets, exception
regions, or fatal machine-fault sites. The supplied-vector fixture now runs
fifteen invocations per CPU with balanced ownership and unchanged images. Receipt:
`artifacts/setclock-morphos-native-20260912-qualified/qualification.json`.
The 68000/020/040 HUNKs are 3388 bytes (SHA-256
`525d5cfe3744282c1d353fcdd5bfa6655bb00c5699d098057b489ef288b3b569`,
`5c5ef4fa79926445c6b679fd567763478b2b59d88bde1ca5b129de4698412ebb`, and
the same 68040 hash) with fourteen reachable methods per CPU.

The MorphOS 52+ UTC vectors are fail-closed pending exact SDK declarations.
No original guest comparison, PURE/resident lifecycle, Workbench parity, package
admission, or differential result is claimed. Shipping remains
0/200 commands and 0/246 profiles.


## 2026-09-12 Version source and native-boundary checkpoint

The MorphOS `Version` profile now has a source-bound contract and a bounded
resident system/RES body. The body uses the source `NAME/M,MD5SUM/S,VERSION/N,
REVISION/N,FILE/S,FULL/S,RES/S` template, invocation-owned DOS `ReadArgs`,
public DOS version state, and `Exec.FindResident`; provider-heavy file, MD5,
version.library and ARexx paths fail closed. Its three CPU HUNKs compile with
zero managed allocation sites, runtime helpers/features, external native
targets, exception regions, or fatal machine-fault sites. The supplied-vector
fixture runs fourteen invocations per CPU with balanced parser ownership and
unchanged images. Receipt: `artifacts/version-morphos-native-20260912-qualified/
qualification.json`. The 68000/020/040 HUNKs are 4520/4588/4516 bytes with SHA-256
`d29914310c0c27bb604ef6597509829fa0f0320966f168b58974ff89ec043b5e`,
`c101bc9110cdf9d97a0d4968bffc0cc36df2faf367de88cfe4194e94e7c1145b`, and
`ee5ed0f32a4aa20d6628819baeb4a45105cf3018fa37b8e6038063809a34e420`.

No original guest comparison, exact output/parsing parity, Workbench body,
PURE/resident lifecycle, package admission, or differential result is claimed.
Shipping remains 0/200 commands and 0/246 profiles.

## 2026-09-12 MorphOS TaskList public task snapshot checkpoint

The MorphOS `TaskList` profile is now source-bound to
`artifacts/morphos320-c-source-extracted/c/tasklist/tasklist.c` (21,603 bytes,
SHA-256 `66fd02c45ac1e91e770bb9b4cefb6966427f94a0b951a4fc21b695b5294ed94b`).
The bounded resident body preserves the complete eleven-field `ReadArgs`
template, snapshots the public current/ready/waiting Exec lists under
`Disable`, obtains `ProcessId` through `NewGetTaskAttrsA`, applies name/address
and list exclusion filters, and formats standard rows after releasing
protection. Sysdebug-backed verbose, stack, and register modes fail closed with
`ERROR_NOT_IMPLEMENTED` pending their exact provider ABI.

`qualify_morphos_tasklist_native_entry.ps1` compiles resident 68000/020/040
HUNKs and runs seventeen supplied vectors per CPU (48 total), including list and
filter behavior, parser and allocation failures, Ctrl-C, startup boundaries,
and interleaved callers. The durable receipt is
`artifacts/tasklist-morphos-native-20260912-qualified/qualification.json`.
The HUNKs are 5836/5920/5824 bytes with SHA-256
`77af87661fe61142c62fd8ade7080644d6e41b38b13a95ef4e5db740a1923572`,
`e1405556be13306204844ed5a42657fa2dfe5a57cf97c2022996d6c3d0a61896`, and
`009662287e745ee442f62aaa966676b14f8adcafd3d134a5291fe11afb755f17`.
This remains a bounded supplied-vector receipt: complete task population,
advanced sysdebug behavior, original guest comparison, Workbench parity,
PURE/resident lifecycle, differential, licensing, and package admission remain
open.

## 2026-09-12 Workbench 3.1 Version syntax-candidate entry

The separate DOS 36 `Workbench31VersionEntry` preserves the classic
`NAME,VERSION/N,REVISION/N,FILE/S,FULL/S,UNIT/N,INTERNAL/S,RES/S` boundary.
Its resident 68000/020/040 HUNKs pass fifteen supplied parser, system/RES,
comparison, provider-gap, startup-boundary and interleaving vectors per CPU
(45 total), with balanced parser ownership, no managed allocation sites,
runtime helpers/features, external targets, exception regions, fatal machine
fault sites, leaks or shared-image writes.

Receipt: `artifacts/version-wb31-native-20260912-qualified/qualification.json`.
The HUNKs are 4,136/4,208/4,136 bytes with SHA-256
`0401eb1f0fa7f8332b453d499c87dfcf0ecd5cb4ef40e91346133c27f1372020`,
`5aa9d51cbff8dee7334be1435f8ff3e599fda2f37b22bac3cfc0813c68bd15f9`, and
`ad18b52c1abb03f2e9b0b9325c7ac940333658aaae6d2f64d0418b9d682adc29`.
This is a syntax-candidate native receipt only; exact Workbench positional and
output behavior, packed correspondence, original guest parity, PURE/resident
lifecycle, licensing, package admission and differential evidence remain open.
## 2026-09-12 MorphOS Info public DOS-list snapshot

The MorphOS `Info` profile now has a bounded source-bound native stage. The
resident body preserves the complete
`DISKS/S,VOLS=VOLUMES/S,GOODONLY/S,BLOCKS/S,VERBOSE/S,DEVICES/M` grammar,
captures active public volume/device nodes under one read lock, releases that
lock before public DOS `Info` queries, supports selection, case-insensitive
DOS wildcard device filters and `GOODONLY`, and renders bounded device/volume rows. The
`BLOCKS/S` path formats public block counters through the bounded fixture, and
the valid `FileSysStartupMsg` `VERBOSE/S` device/unit line is rendered after
unlock. Alternate startup/provider data remains open.

`qualify_morphos_info_native_entry.ps1` passes seventeen supplied vectors per
CPU on resident 68000/020/040 HUNKs (51 total). The artifacts are 11716,
11572, and 11512 bytes with SHA-256
`7542e6ff83d2176062050abe0a2754a2a7a3b8c4190bec63bd592ddf622013be`,
`5661dd710b393d6586aea67d6b67983992c2ed023e80e4eb83b6005cf45078b8`, and
`57597dcd1cb8f99995afad6627e030f60cc911115ad708d7df2b99648f1d6498`.
The durable receipt is
`artifacts/info-morphos-native-20260912-qualified/qualification.json`.
This is not shipping evidence: original guest and packed correspondence,
exact output/status parity, locale/wildcard/provider behavior, PURE/resident
lifecycle, licensing, package admission and differential gates remain open.

## 2026-09-12 MorphOS Search bounded native entry (historical)

The MorphOS `Search` profile now has a bounded source-bound resident entry.
`NativeMorphOSSearchCommand` preserves the inspected ReadArgs template and
uses public DOS 37 parser ownership, `MatchFirst`/`MatchNext`/`MatchEnd`
traversal, file reads, DOS pattern matching, line output, failure paths,
Ctrl-C, and startup-boundary rejection. The supplied stage covers literal,
`FILE`, `PATTERN`, `CASE`, `NONUM`, `QUIET`, and `LINES/N`; `QUICK` is parsed
but its compact presentation is not claimed. At this historical checkpoint,
`ALL` remained fail-closed pending the recursive/soft-link provider contract;
the later bounded AnchorPath candidate and current receipt are recorded above.
Locale folding remains open, and the fixture read buffer is bounded to 8,192
bytes.

`tools/Commands/qualify_morphos_search_native_entry.ps1` passes seventeen
supplied DOS-vector invocations per CPU (51 total) on resident 68000/020/040
HUNKs. The durable receipt is
`artifacts/search-morphos-native-20260912-qualified/qualification.json`.
The HUNKs are 15,156/15,308/15,140 bytes with SHA-256
`a19affbd8a6a94c99653fff1434b93fd2fd6e474db8c010081ae5e99d1bfe869`,
`290587c2fca16b5490967bbb4b2a8ef1b910ce97e0bcc89ac3e57c9937408775`, and
`f15b38a30a7fdaa4090cb5f60b54dd454601778cd29e72f9a29cf080ca9bdd50`.
The static gate reports 25 reachable methods per CPU and no managed allocation
sites, runtime features/helpers, external native targets, exception regions,
fatal fault sites, or shared-image writes. This receipt is bounded native
evidence only; Workbench body, packed correspondence, complete source
output/locale/filesystem parity, original guest comparison, PURE/resident
lifecycle, licensing, package admission, and differential gates remain open.

## 2026-09-12 MorphOS DOSList public DOS-list snapshot

The MorphOS `DOSList` profile now has a bounded source-bound native stage. The
resident body preserves
`NAME,ADDRESS/N,DEVICES/S,VOLUMES/S,ASSIGNS/S,VERBOSE/S`, follows the released
device, volume, assign pass order, uses one matching public read lock per pass,
supports exact case-insensitive name and numeric address selection, and emits
the source-common node and bounded mounted-state rows. Provider-specific
process/detail attributes remain open.

`qualify_morphos_doslist_native_entry.ps1` passes seventeen supplied vectors per
CPU on resident 68000/020/040 HUNKs (51 total). The artifacts are 4552, 4564,
and 4472 bytes with SHA-256
`e0dc3a906e97a4f9d6ed2791a8570f88dd6fc21a8e49d63ff650a61c2944ea6f`,
`c68f53d5c67c506cffad9c11959c99f947ed2a1e13538d344ac4c0524611c268`, and
`c97c69aa68eed467b29e6ffdf562d4ad114299f6f9571d2c6784c096759cfb09`.
The durable receipt is
`artifacts/doslist-morphos-native-20260912-qualified/qualification.json`.
This is not shipping evidence: exact verbose/provider output, original guest
and packed correspondence, PURE/resident lifecycle, licensing, package
admission and differential gates remain open.

## 2026-09-12 MorphOS List public matcher snapshot

The MorphOS `List` profile now has a bounded source-bound native stage. The
resident body preserves the recovered 19-slot grammar and implements flat
public DOS matcher traversal with `DIR/M`, `P=PAT/K`, `QUICK`, `BLOCK`,
`NOHEAD`, `FILES`, `DIRS`, and `TO/K`. Parser/result/workspace ownership,
pattern matching, output restoration, Ctrl-C, empty-match, allocation failure,
unsupported-provider rejection, startup boundaries, and interleaved callers
are covered. Recursive/date/sort/owner/`LFORMAT`/`ALL` modes fail closed until
their provider contracts are captured.

`qualify_morphos_list_native_entry.ps1` passes seventeen supplied vectors per
CPU on resident 68000/020/040 HUNKs (51 total). The durable receipt is
`artifacts/list-morphos-native-20260912-qualified/qualification.json`; the
HUNKs are 8684, 8744, and 8712 bytes with SHA-256
`9d196a15ebe14ef2cdce7d239c8ed2820e5cc04f3ea76bf44dc602a0f686ea85`,
`ef84299d244d9e4251e77d2ad72968ac286280dcb46e6bd88899bd1581fbc5e8`, and
`dcd530108a61bb9c95e393dd924e9f863d9a26d6e97afed23581bc5a66f075ad`. This is not shipping evidence: packed
correspondence, exact Workbench/provider parity, real filesystem behavior,
PURE/resident lifecycle, licensing, package admission, and differential gates
remain open.

## 2026-09-12 Workbench 3.1 Search syntax-candidate entry (historical)

The Workbench 3.1 `Search` candidate now has a separate DOS 36 resident entry
that reuses the bounded public-DOS body. The fixture covers the shared parser,
flat matcher/file I/O, DOS pattern and line modes, failure, Ctrl-C, and
startup-boundary paths. At this checkpoint, no Workbench Search source member
or packed binary was bound to the implementation. The later 2026-09-23 static
binary audit and resident receipt are recorded above; exact runtime behavior,
locale, complete recursion, package identity, lifecycle, and differential
behavior remain open.

`tools/Commands/qualify_workbench31_search_native_entry.ps1` passes seventeen
supplied DOS-vector invocations per CPU (51 total). The durable receipt is
`artifacts/search-wb31-native-20260912-qualified/qualification.json`.
The HUNKs are 15,220/15,376/15,204 bytes with SHA-256
`d4859b41a3facd55d316359f1c679a38b8adead6f850a246e51482b0f878912a`,
`07c5e2859e43743791e60eabe8ae2f870609aedc2e5fa5163a8a45c76e513a63`, and
`0946ad9623fe8530b4e6aca68861fa1c669a7a617a44a4a7c70e62177669c90d`.
Static checks report 25 reachable methods per CPU and no managed allocation
sites, runtime features/helpers, external native targets, exception regions,
fatal fault sites, or shared-image writes. This is candidate native evidence
only; it does not promote the Workbench profile or shipping totals.

## 2026-09-12 Workbench 3.1 Avail requalification

`tools/Commands/qualify_avail_wb31_native.ps1` reran the Workbench 3.1 Avail
resident fixture after the profile-boundary additions. It passes nine supplied
DOS/Exec vectors per CPU (27 total), covering summary and selector output,
precedence, parser failure, fail-closed `FLUSH`, repeat and interleaving. The
three HUNKs remain 2,856/2,852/2,852 bytes with the previously recorded hashes;
this rerun introduces no status promotion.

The durable receipt is
`artifacts/avail-wb31-native-20260912-qualified/qualification.json`.
Original output/IoErr, exact expunge behavior, guest comparison,
PURE/resident lifecycle, packaging and differential evidence remain open.

## 2026-09-12 MorphOS Avail syntax-candidate entry

`tools/Commands/qualify_morphos_avail_native_entry.ps1` compiled the separate
DOS 37 resident `NativeMorphOSAvailEntry` and ran nine supplied DOS/Exec vectors
per CPU (27 total). The fixture covers summary and selector output, selector
precedence, parser failure, fail-closed `FLUSH`, repeat and interleaving. The
twelve-method static receipts contain no managed runtime features/helpers,
external native targets, exception regions or fatal fault sites; runtime checks
report no leaks or shared-image writes.

The durable receipt is
`artifacts/avail-morphos-native-20260912-qualified/qualification.json`.
The HUNKs are 2,916/2,912/2,912 bytes with SHA-256
`bb9ca12baacf6d8fa2de88dcd2fe46a04a0f42cc19422d50a71ee37e680f6dbe`,
`21417f9932c6d8b44f9b08fb2ee5217adb63bbb9762f259e90d3031852ba6f66`, and
`21417f9932c6d8b44f9b08fb2ee5217adb63bbb9762f259e90d3031852ba6f66`.
This is a syntax-candidate native receipt only; exact MorphOS 50.6 grammar,
expunge behavior, diagnostics/output, original guest parity, PURE/resident
lifecycle, licensing, package admission and differential gates remain open.

## 2026-09-12 Workbench 3.1 Filenote syntax-candidate entry

`tools/Commands/qualify_workbench31_filenote_native.ps1` compiled the separate
DOS 36 resident `Workbench31FileNoteEntry` and ran ten supplied DOS vectors per
CPU (30 total). The fixture covers parser failure, matcher/no-match,
comment truncation, quiet output, recursive AnchorPath descent/exit,
`SetComment` failure, cleanup and interleaving. The fifteen-method static
receipts contain no managed runtime features/helpers, external native targets,
exception regions or fatal fault sites; runtime checks report no leaks or
shared-image writes.

The durable receipt is
`artifacts/filenote-wb31-native-20260912-qualified/qualification.json`.
The HUNKs are 3,752/3,800/3,744 bytes with SHA-256
`c5b3f95fc637d7749a97f668a3db87ca0f860522d23e7700ef73f11fe3b268f2`,
`4cb246479873f30943364db4dc819de363aebceb0d8d001d49223ceb376016a7`, and
`7bd403039813b256741d4d7ee3b94c2526f96ec79c6fabe9273bc5b2909e8167`.
This is a syntax-candidate native receipt only; packed/source correspondence,
exact Workbench output and diagnostics, original guest parity, PURE/resident
lifecycle, licensing, package admission and differential evidence remain open.

## 2026-09-12 MorphOS Type native-entry requalification

`tools/Commands/qualify_type_native_entry.ps1` requalified the current
MorphOS `Type` resident entry on 68000/020/040. Its supplied fixture passes
thirteen DOS parser, wildcard, text/HEX stream, `TO`, diagnostic, Ctrl-C,
repeat, and interleaving invocations per CPU (39 total), with balanced
invocation-owned resources and unchanged shared images. Static compatibility
reports contain no managed allocation sites, runtime features/helpers,
external native targets, exception regions, or fatal fault sites.

The durable receipt is
`artifacts/type-morphos-native-20260912-qualified/qualification.json`.
The HUNKs are 8,456/8,516/8,324 bytes with SHA-256
`7478aee3a8859a9bbd64f7d4b3843b9e81a95b1ac49cefa0d225f214cbca073e`,
`5ecb3c666fe8336387859a95c5e259e59e22520118c69fc68fee2c3845f05224`, and
`3b9abe6109131392a3256667256a0398a634038d9b718d057e308318fe6ebb62`.
This is bounded native evidence only; packed/source correspondence, real
DOS/handler behavior, complete provider semantics, Workbench parity,
PURE/resident lifecycle, licensing, package admission, and differential gates
remain open.

## 2026-09-12 MorphOS Relabel native-entry requalification

`tools/Commands/qualify_relabel_native.ps1` requalified the current MorphOS
`Relabel` resident entry on 68000/020/040. Its supplied fixture passes eight
DOS parser/list/mutation invocations per CPU (24 total), covering invalid names,
missing entries, handler failure, interleaved calls and cleanup ordering. The
entry has eleven reachable methods and the static report contains no managed
allocation sites, runtime features/helpers, external native targets, exception
regions or fatal fault sites; runtime checks report no resource leaks or
shared-image writes.

The durable receipt is
`artifacts/relabel-morphos-native-20260912-qualified/qualification.json`.
The HUNKs are 2,804/2,848/2,804 bytes with SHA-256
`b5e0042756c36a110dd7a6c3520ee22d42d2b1e0fa79ba2d0226c65ff9cb95bc`,
`a1692cd2b8d54fc6142d96cc2505486102c4963b7ced334f2ae422aa315c139d`, and
`706dc3e09bb92671f8e91766ebb682f1a36a3cedbd24bfda74b60fad78f4ff7b`.
This receipt is bounded native evidence only; real handler behavior, Workbench
parity, packed/source correspondence, PURE/resident lifecycle, licensing,
package admission and differential evidence remain open.

## 2026-09-12 Workbench 3.1 Relabel syntax-candidate entry

`tools/Commands/qualify_workbench31_relabel_native_entry.ps1` compiled a
separate DOS 36 resident `Relabel` entry on 68000/020/040. Its supplied
fixture passes eight DOS parser/list/mutation invocations per CPU (24 total)
through the observed `DRIVE/A,NAME/A` boundary while sharing the bounded
public-DOS body. Each static/runtime report has no managed allocation sites,
runtime features/helpers, external native targets, exception regions or fatal
fault sites, with no fixture leaks or shared-image writes.

The durable receipt is
`artifacts/relabel-wb31-native-20260912-qualified/qualification.json`.
The HUNKs are 2,872/2,916/2,872 bytes with SHA-256
`8d1910a3b3c5538574f927ad66cc1bd46c1f7a8ccdcae07b0374816a4b09e0ca`,
`feca1ed4e35572437254797e332eb915f0815776f33970a4d275b3568279e908`, and
`8d748794bd68425bb17ab4027c437ffc93b945da74347875d4a7f90bd6a1751e`.
This remains a bounded syntax/body candidate; exact Workbench diagnostics and
mutation behavior, original guest parity, PURE/resident lifecycle, packaging,
and differential evidence remain open.

## 2026-09-12 Workbench 3.1 AddBuffers syntax-candidate entry

`tools/Commands/qualify_workbench31_addbuffers_native_entry.ps1` compiled a
separate DOS 36 resident `AddBuffers` entry on 68000/020/040. Its supplied
fixture passes six DOS parser/handler/formatting invocations per CPU (18
total) through the observed `DRIVE/A,BUFFERS/N` boundary while sharing the
bounded public-DOS body. Each static/runtime report has no managed allocation
sites, runtime features/helpers, external native targets, exception regions or
fatal fault sites, with no fixture leaks or shared-image writes.

The durable receipt is
`artifacts/addbuffers-wb31-native-20260912-qualified/qualification.json`.
The HUNKs are 2,464 bytes on all CPUs with SHA-256
`67418b8cd91ac34e65fd9546c242e768283c94f1ee0908da78619e4a55985355`,
`70078bef3eb2cce91f6894de083c752e536e7a571a8932bbfa97e339548578e9`, and
`70078bef3eb2cce91f6894de083c752e536e7a571a8932bbfa97e339548578e9`.
This remains bounded syntax/body evidence; exact Workbench handler behavior,
original guest parity, PURE/resident lifecycle, packaging and differential
evidence remain open.

## 2026-09-12 MorphOS FileNote native-entry requalification

`tools/Commands/qualify_filenote_native.ps1` requalified the current MorphOS
`FileNote` resident entry on 68000/020/040. Its supplied fixture passes ten
DOS parser, matcher, recursive AnchorPath, output and `SetComment` invocations
per CPU (30 total), covering parser failure, no-match, comment truncation,
quiet output, directory descent/exit, mutation failure, cleanup and
interleaving. The entry has fourteen reachable methods and the static/runtime
reports contain no managed runtime features/helpers, external native targets,
exception regions, fatal fault sites, leaked invocation storage or
shared-image writes.

The durable receipt is
`artifacts/filenote-morphos-native-20260912-qualified/qualification.json`.
The HUNKs are 3,680/3,728/3,672 bytes with SHA-256
`d7e7b210cdfe526c2e9a773055d5f1e06caa08990a449c10a47308bbdd310ceb`,
`aed0f54151e3ec909a92ae207d2eba7f358873dcbc7aa462de1d311c20f127c7`, and
`d38bb13d407824c866369eff3827323567cd1db1114fc8c53685fd288d1bdced`.
This is bounded native evidence only; exact parser/diagnostic parity,
soft-link/device policy, Workbench behavior, PURE/resident lifecycle,
licensing, package admission and differential evidence remain open.

## 2026-09-12 Workbench 3.1 Status legacy-list candidate

The separate DOS 36 `Workbench31StatusEntry` preserves the shared
`PROCESS/N,FULL/S,TCB/S,CLI=ALL/S,COM=COMMAND/K` grammar and exercises the
legacy CLI-list path used by the original Workbench profile. Its resident
68000/020/040 HUNKs pass fourteen supplied parser, snapshot, filter, formatting,
missing-process, Ctrl-C, startup-boundary and interleaved vectors per CPU
(42 total), with balanced parser ownership, no managed allocation sites,
runtime helpers/features, external targets, exception regions, fatal machine
fault sites, leaks or shared-image writes.

Receipt: `artifacts/status-wb31-native-20260912-qualified/qualification.json`.
The HUNKs are 6,252/6,252/6,156 bytes with SHA-256
`47d8ac39fed78aef7989aa22e74d2c1f3d2237328cee11efef9a953a98926f48`,
`1542858a3c12c05dedcb65639cbe886a39baf25b3ebc341eeb74b88db62604ad`, and
`41cf6d88110b757a4c243a4b8984336334f8531117b71dbcac894a0154523c7b`.
This remains bounded native evidence; exact classic output and task population,
original guest parity, PURE/resident lifecycle, licensing, package admission
and differential evidence remain open.

## 2026-09-12 MorphOS Which extended syntax candidate

`MorphOS320WhichEntry` adds a separate DOS 37 resident boundary for the
candidate `FILE/A,NOALIAS/S,ALIAS/S,NORES/S,RES/S,ALL/S` grammar. The public
resident, current-directory and CLI-path lookup owner is shared with classic
Which; ALIAS/NOALIAS provider behavior fails closed until its MorphOS ABI is
qualified. The supplied fixture passes sixteen parser, lookup, provider-gap,
failure, startup-boundary and interleaved vectors per CPU (48 total).

Receipt: `artifacts/which-morphos-native-20260912-qualified/qualification.json`.
The HUNKs are 4,340/4,408/4,328 bytes with SHA-256
`1c6c65ba2895fcace2d9859d385aefec215489e0e22f82a5a514a932dc336c38`,
`5c49abc2679a17caefe040a4ce12a9c836c446f09919de15d5cd8d5f47c70cb3`, and
`b8c0a1c4ec6d77340e54c7fe894a1f70f6be1e393ffc2d7b8c9399b2f6fc3a39`.
Static checks report 16 reachable methods with no managed allocation sites,
runtime helpers/features, external targets, exception regions, fatal machine
fault sites, leaks or shared-image writes. Exact MorphOS grammar/output and
alias ordering, original guest parity, PURE/resident lifecycle, licensing,
package admission and differential evidence remain open.

## 2026-09-12 Workbench 3.1 Which requalification after profile split

The separate DOS 36 `Workbench31WhichEntry` preserves the classic
`FILE/A,NORES/S,RES/S,ALL/S` grammar while sharing the resident/current-directory
and CLI-path lookup owner with the MorphOS candidate. Its supplied fixture passes
twelve parser, lookup, failure, startup-boundary and interleaved vectors per CPU
(36 total). The entry has sixteen reachable methods; static checks report no
managed allocation sites, runtime helpers/features, external targets, exception
regions or fatal machine fault sites. Runtime checks report no leaks or shared
image writes.

Receipt: `artifacts/which-wb31-native-20260912-qualified/qualification.json`.
The HUNKs are 4,340/4,408/4,328 bytes with SHA-256
`2bcb83887615664803c7de177fd4951a1f6b43496cd3278d5890e4064860b83c`,
`3fc1206dccf6a4e268c764b46d0aa4ad7806f3293818c076073373b442e43d1c`, and
`d580060af1eca01d65d2f25f499e86413d992075c933d54fc2246944ecf43a82`.
This remains bounded native evidence; exact classic output and provider
population, original guest parity, PURE/resident lifecycle, licensing, package
admission and differential evidence remain open.

## 2026-09-12 MorphOS PathPart native-entry requalification

The existing MorphOS `PathPart` resident entry was requalified on 68000/020/040.
Its supplied fixture passes eleven DOS path-helper and cleanup vectors per CPU
(33 total), including mode selection, failure paths, repeats and interleaved
callers. The HUNKs are 3,088/3,148/3,088 bytes with SHA-256
`ad92c91ebce428817c9671fb9701a82843b8160cc3e653fc86ff2fc55611d4dd`,
`e8ba70d5999037496675320f4780c08bf491031c948695542140aa2c02ca9ad8`, and
`f4e0d3836d9edc4ef8d22e761d21c2072aaeabceb6652c057d5b1f809960f52e`.
Static checks report fourteen reachable methods with no managed allocation sites,
runtime helpers/features, external targets, exception regions or fatal machine
fault sites; runtime checks report no leaks or shared-image writes. This remains
bounded native evidence; command grammar/output, reference parity, installed P
policy, lifecycle, packaging and differential gates remain open.

## 2026-09-12 MorphOS Quote native-entry requalification

The existing MorphOS `Quote` STR forward resident entry was requalified on
68000/020/040. Its supplied fixture passes twelve DOS parser, forward-rule,
unsupported-mode, short-write, allocation-failure, repeat and interleaved
vectors per CPU (36 total). The HUNKs are 10,388/10,648/10,356 bytes with
SHA-256 `b580c4faca4cd58a2d05d3c86db4da2db81b40ae1f652c429304b2e76847c842`,
`2927f2319462d2460d78be23ff630aac298da4b5712282084e647a7798951dac`, and
`00f4c5d500ba7653b444e94da092b27b32c227805d82e1435378943a947622b4`.
Static checks report 41 reachable methods with no managed allocation sites,
runtime helpers/features, external targets, exception regions or fatal machine
fault sites; runtime checks report no leaks or shared-image writes. The receipt
still covers only bounded STR forward behavior; full FILE/VAR/REVERSE semantics,
reference parity, lifecycle, packaging and differential gates remain open.

## 2026-09-12 MorphOS Delete frontend requalification

The MorphOS `Delete` frontend was requalified as a resident entry on
68000/020/040. Its supplied fixture passes eight parser, matcher, lock,
protection, deletion, partial-failure and interleaved vectors per CPU (24 total)
with 41 reachable methods. Static checks report no managed allocation sites,
runtime helpers/features, external targets, exception regions or fatal machine
fault sites; runtime checks report no leaks or shared-image writes.

Receipt: `artifacts/delete-command-native-20260912-qualified/qualification.json`.
The HUNKs are 15,252/15,376/15,180 bytes with SHA-256
`9d1d2841bd3747623006fbd97b56301b5b6cf95885cb1712dd929217adbde243`,
`72f276fdbecc7e7f64cc4d2fb03f337d0736fc1637398c3a872e18060dffc6dd`, and
`ee07c5423946ad4f19644ba62ad5a6864b862fa576f51a8b0646196783174e04`.
This remains bounded MorphOS frontend evidence; parent-protection retry, exact
diagnostics, recursive/link behavior, real handler behavior, Workbench parity,
PURE/resident lifecycle, packaging and differential gates remain open.

## 2026-09-12 Workbench 3.1 Delete syntax-candidate entry

The separate DOS 36 `Workbench31DeleteEntry` preserves the classic four-slot
`FILE/M/A,ALL/S,QUIET/S,FORCE/S` boundary and shares the bounded public-DOS
matcher, lock, protection and `DeleteFile` worker with the MorphOS frontend.
Its supplied fixture passes eight parser, deletion, failure, startup-boundary
and interleaved vectors per CPU (24 total), with 43 reachable methods. Static
checks report no managed allocation sites, runtime helpers/features, external
targets, exception regions or fatal machine-fault sites; runtime checks report
no leaks or shared-image writes.

Receipt: `artifacts/delete-wb31-native-20260912-qualified/qualification.json`.
The HUNKs are 15,480/15,608/15,408 bytes with SHA-256
`c181f2dbcf25bf87d4c4060d6e35366d293451113b04f51f571901879b585004`,
`33e3df055a1a2d876b37787dc8fd87cfeaf60cd0723389456db5592c570e413e`, and
`c0e0305b84767f07bf6e96f7fb92645e05a576993cdc031bac24825b09703151`.
This remains a bounded syntax/startup candidate; exact Workbench behavior,
diagnostics, recursion/link policy, original parity, PURE/resident lifecycle,
packaging and differential gates remain open.

## 2026-09-12 Workbench 3.1 Join syntax-candidate entry

The separate DOS 36 `Workbench31JoinEntry` preserves the observed classic
`FILE/M/A,AS=TO/K/A` ReadArgs boundary and shares the bounded public-DOS Join
body with MorphOS. Static resident compilation succeeds on 68000/020/040 with
19 reachable methods and no managed allocation sites, runtime helpers/features,
external targets, exception regions or fatal machine-fault sites. The HUNKs
are 4,276/4,356/4,268 bytes.

Receipt: `artifacts/join-wb31-native-20260912-qualified/qualification.json`.
This receipt has no runtime fixture, so exact Workbench parser/diagnostic
behavior, handler effects, source/destination lifecycle, PURE/resident
admission, packaging and differential parity remain open.

## 2026-09-18 Join frontend runtime receipts

The Workbench DOS 36 and MorphOS DOS 37 Join entries now execute through the
same supplied public-DOS fixture. Each resident 68000/020/040 image passes
eighteen invocations, including ordered source streams, exact EOF and short
write/read failures, destination cleanup, parser/open/workspace/result/buffer
allocation and Ctrl-C failures, empty input, malformed argument and
startup/missing-DOS boundaries,
and instruction-interleaved callers. Resource assertions cover matcher,
ReadArgs, file handles, the 262144-byte transfer buffer, signals, `IoErr`,
close/free and incomplete-output deletion; all six images have zero managed
runtime features/helpers, external targets, exception/fatal sites, leaks or
shared-image writes.

Receipts: `artifacts/join-wb31-native-20260918-runtime-v3/qualification.json`
and `artifacts/join-morphos320-native-20260918-runtime-v2/qualification.json`.
The Workbench HUNKs are 4,332/4,408/4,324 bytes and the MorphOS HUNKs are
4,264/4,336/4,256 bytes for 68000/020/040. These remain bounded native
ABI/ownership checkpoints; exact guest parser and diagnostic behavior,
wildcard/no-match and handler semantics, packed correspondence, original
PURE/resident lifecycle, package placement and differential parity remain
open.

## 2026-09-18 MorphOS RequestChoice resident runtime receipt

The MorphOS DOS 37 `NativeMorphOSRequestChoiceEntry` now has a source-bound
resident body using the public DOS, Exec, Intuition, and timer.device vectors.
Its independent fixture passes eight invocations on each 68000/020/040 HUNK,
covering parser and Intuition-open failures, percent-body input, no-timeout
and timeout requester paths, Ctrl-C selection, Workbench startup, and repeat
ownership. Static closure reports zero managed allocation sites, runtime
features/helpers, external targets, exception regions, or fatal machine-fault
sites; runtime checks report balanced parser/text/requester/timer ownership and
zero shared-image writes.

Receipt: `artifacts/requestchoice-morphos-native-20260918-runtime-v1/qualification.json`.
The 68000/020/040 HUNKs are 5,080/5,188/5,080 bytes with SHA-256
`3498d321690c158d4c03a06e161eacd88fa5feae2bd614d8c318e5cb3bbb26c5`,
`952c3cb27a05499ab342cdd67c11fac313b0d25f2c71468fd82e76f1bb966506`, and
`f20d462f2873be38f1f2f0e66f171703182839c4727431b195d030b835d20558`.
These are bounded native checkpoints; original guest UI behavior, Workbench
correspondence, interactive lifecycle, PURE/resident admission, packaging,
licensing, and differential parity remain open.

## 2026-09-12 Workbench 3.1 Type syntax-candidate entry

The separate DOS 36 `Workbench31TypeEntry` preserves the classic five-slot
`FROM/A/M,TO/K,OPT/K,HEX/S,NUMBER/S` boundary and omits the MorphOS-only
`NOLINE` slot. Its supplied fixture passes thirteen parser, wildcard,
text/HEX, diagnostic, Ctrl-C and interleaving vectors per CPU (39 total), with
26 reachable methods. Static checks report no managed allocation sites,
runtime helpers/features, external targets, exception regions or fatal
machine-fault sites; runtime checks report no leaks or shared-image writes.

Receipt: `artifacts/type-wb31-native-20260912-qualified/qualification.json`.
The HUNKs are 8,532/8,592/8,400 bytes with SHA-256
`246357d25ebc228a08729703e9b37d28dbc93bb71310233006fbcc3106121aca`,
`e628157845acaeb54ded9bf49e753699a83362ec5384590d2bacf11f0c8df3c9`, and
`34e45a1f48c86f878fcf99c911b47dd99e4b089d5f44f33d08f003533ebdfeb0`.
This remains bounded candidate evidence; exact Workbench output/diagnostics,
original parity, PURE/resident lifecycle, packaging and differential gates
remain open.

## 2026-09-24 MorphOS Version comparison vectors

The MorphOS Version body now follows the captured `cmpargsparsed()` ordering:
it returns `RETURN_WARN` only when the requested major version, or applicable
revision, exceeds the discovered value. System and single-resident paths print
their result before returning the comparison status. The expanded fixture
covers lower major/revision values, higher major/revision values,
revision-only comparison, and the resident path. The Workbench candidate is
unchanged pending classic guest evidence.

The refreshed resident 68000/020/040 receipt
`artifacts/version-morphos-native-20260924-compare-v1/qualification.json`
passes twenty supplied DOS/Exec invocations per CPU. HUNKs are 5,216/5,300/
5,220 bytes with SHA-256
`cdd232cef00a808a12475217cce33c1d0908032a5f63ed4d7ce47fc81dd727a5`,
`89e3d2ab1365518b87f7f4737dd30006e330739475b29011d5f07c6b82a6a2b4`, and
`777199801f2247810e0fe69efcd788ff350ee2174e76ed50360b0faa37223f66`.

This remains fixture evidence. Exact MorphOS guest output, packed
correspondence, FILE/MD5 and optional provider branches, PURE/resident
lifecycle, licensing, package admission and differential comparison remain
open.







### 2026-09-22 MorphOS LoadMonDrvs supplied-vector entry qualification

The resident LoadMonDrvs candidate now has a three-CPU supplied-vector
qualification receipt at
`artifacts/loadmondrvs-morphos-native-entry-20260922-v3/qualification.json`.
It records nine invocations per CPU, parser/result ownership for default and
`FROM`/`EXCEPT`, parser and DOS-open failures, matcher cleanup, successful
resident initialization, failed initialization with segment unload, failed
`LoadSeg`, repeated and interleaved calls, and zero shared-image writes. This is invocation-boundary
evidence only; packed behavior, provider-backed driver effects, guest parity,
lifecycle/PURE, package and differential gates remain open.

### 2026-09-23 MorphOS and Workbench List literal SUB qualification

The MorphOS 3.20 `List` resident candidate now treats `SUB` as a literal,
case-insensitive file-name substring filter. It escapes DOS pattern
metacharacters, combines with `P=PAT`, excludes matching directories, and
compiles into a separate invocation-owned buffer through public DOS pattern
APIs. Its receipt
`artifacts/cc11-list-morphos-native-20260923-sub-v3/qualification.json` passes
twenty supplied vectors per CPU on 68000/020/040. The Workbench 3.1 DOS 36
syntax candidate passes twenty-one vectors per CPU (63 total) at
`artifacts/cc11-list-wb31-native-20260923-sub-v3/qualification.json`.

These fixtures confirm the bounded parser, filtering, rendering, ownership,
Ctrl-C and interleaving paths, but do not prove original guest parity, packed
correspondence, recursive/date/sort/owner/LFORMAT/ALL behavior, PURE/resident
reuse, licensing or package admission.

### 2026-09-23 List default rows and KEYS

The shared MorphOS/Workbench candidate now displays FIB disk keys only when
`KEYS` is selected. Default output includes file size or the `Dir` marker,
protection letters, DOS-formatted FIB date/time, and an optional colon-prefixed
comment. Quick mode skips date conversion. A failing DOS `DateToStr` vector
checks matcher termination, workspace release, and error publication.

The MorphOS resident receipt
`artifacts/cc11-list-morphos-native-20260923-format-keys-v3/qualification.json`
passes twenty-three supplied vectors per CPU on 68000/020/040. The Workbench
3.1 DOS 36 syntax candidate passes twenty-four vectors per CPU (72 total) at
`artifacts/cc11-list-wb31-native-20260923-format-keys-v3/qualification.json`.
Dates are supplied by a synthetic vector fixture. Exact guest output, headers
and summaries, date filters, recursion, sorting, owner fields, `LFORMAT`,
PURE/resident reuse, licensing, and package admission remain open.

### 2026-09-24 MorphOS TaskList resident feature matrix

The current TaskList resident candidate now includes live A7 capture for the
running current task's `VERBOSE` row, source-shaped PPC backchain `STACKTRACE`
with bounded `STACKLEVEL`, `SegTracker` symbol lookup, `INTERNAL` emulation and
module frame classification, PPC `REGDUMP`, and source-order `REGCHECK`
classification. The source task snapshot uses `Forbid`/`Permit`, six public
`NewGetSystemAttrsA` boundary values, and growing invocation-owned storage; a
100-task fixture forces an initial 128 KiB buffer to be discarded and retried
at 256 KiB. The 68000/020/040 receipt passes 54 supplied DOS/Exec vectors per
CPU, including the live-stack bounds check, with no managed allocation sites,
runtime helpers/features, external targets, exceptions, fatal sites, leaks or
shared-image writes.

Receipt: `artifacts/tasklist-morphos-native-live-a7-v3/qualification.json`.
This remains fixture evidence, not original MorphOS guest or provider parity.
Complete task population, exact package correspondence, PURE/resident
lifecycle, licensing and package admission remain open.

### 2026-09-27 MorphOS TaskList PID_CLI provider

The shared Exec provider now recognizes MorphOS `TASKINFOTYPE_PID_CLI`
(`0x24`). For a Process with a CLI it returns `pr_TaskNum`; for a task with a
MorphOS ETask extension it reads `ETask.UniqueID`; portable task allocations
without an extension retain the task-address fallback. The MorphOS 3.20 SDK
headers define `Task.tc_ETask` at byte 34, `ETask.UniqueID` at byte 24, and
`ExecBase.ex_TaskID` at byte 576; archive and extracted-header hashes are in
the reference capture. MorphOS `NewCreateTaskA` now initializes an 86-byte
ETask in the owned task allocation and assigns a nonzero ID from the ExecBase
counter under `Forbid`/`Permit`, with reap freeing the extension as part of the
task allocation. DOS-owned Process allocation also appends and initializes an
ETask when Exec library version is 50 or newer; Workbench version 40 retains
its classic Process and stack layout. Other standard Exec task paths remain
uncovered.
`FindTaskByPID` accepts native unique IDs and live CLI numbers and
resolves a replacement Process after the original is removed. Focused provider
and compiled production-vector tests pass fourteen cases, including native
ETask-ID lookup, current CLI lookup, selector value, and CLI-number reuse. The
compiled production test also executes the installed `NewGetTaskAttrsA` jump
vector with selector `0x24` and verifies the returned CLI number. The refreshed resident command
receipt passes 54 supplied
vectors per CPU on 68000/020/040 with 69 reachable methods and no managed
allocation sites, runtime helpers/features, external targets, exceptions,
fatal sites, leaks, or shared-image writes:
`artifacts/tasklist-morphos-native-pid-cli-20260927-v3/qualification.json`.
This closes selector dispatch and native ETask-ID reads for tasks with the
extension, and initializes it for MorphOS `NewCreateTaskA` and DOS-owned
Process allocations. The focused provider and compiled production-vector tests
remain 14/14; the refreshed DOS child/publication/lifecycle filter passes 42
cases, including ETask-ID PID lookup and the version-40 layout control. Other
standard Exec task paths, concurrent target death, MorphOS guest ID-sequence
parity, and original guest output/provider parity remain open.

### 2026-09-27 MorphOS DOS-owned Process ETask identity

The DOS-owned Process allocator now checks the Exec library version before
choosing its allocation shape. At version 50 or newer it places an 86-byte
ETask after the Process body and before the child stack, publishes `tc_ETask`,
initializes the parent and child list, and allocates a nonzero ID from
`ex_TaskID` under `Forbid`/`Permit`. Workbench version 40 keeps its original
Process-to-stack boundary and zero ETask pointer. The test creates and readies a
MorphOS Process, resolves its unique ID through `FindTaskByPID`, and then removes
and releases the Process allocation. The DOS process-publication/lifecycle
filter passes 42 tests. This remains supplied-memory unit evidence; actual
MorphOS guest ID sequencing and concurrent target removal are not established.

### 2026-09-24 MorphOS BindDrivers provider-failure matrix

The MorphOS `BindDrivers` resident candidate now qualifies its source-shaped
success path and provider exit branches on 68000/020/040. Seventeen supplied
scanner vectors per CPU cover no-match/empty scans, all three library-open
failures, one successful driver, directory entries, absent Icon object,
`PRODUCT`, and ConfigDev, absent resident, `LoadSeg` and `InitResident`
failures, and NameFromLock/AddPart warnings with `PrintFault` and `IoErr`
checks. The separate production-parser vector still exercises five PRODUCT
strings and nine `FindConfigDev` calls per CPU. All reports show no leaks or
shared-image writes; the static report keeps fourteen reachable main methods
with no managed runtime helpers/features, external native targets, exception
regions or fatal machine-fault sites.

Receipt: `artifacts/binddrivers-morphos-native-entry-20260924-failure-paths-v8/qualification.json`.
This is supplied-vector evidence. Malformed HUNK/PRODUCT bounds, end-to-end
multiple PRODUCT pairs, repeated/interleaved provider-backed scans, Workbench
binary correspondence, real guest behavior, installed PURE/resident flags,
licensing and package placement remain open.

### 2026-09-24 MorphOS BindDrivers bounded HUNK discovery

Resident search now rejects BPTR and HUNK-size arithmetic overflow, unreadable
segment headers/endpoints, short/truncated HUNK extents, invalid links and
cyclic segment lists. It checks guest addresses through Exec `TypeOfMem`, requires the full
Resident match fields to fit, and finds a Resident in the second HUNK. A
constant-space fast/slow walk detects cycles without capping valid HUNK-list
length. The three-CPU resident receipt passes twenty-three scanner vectors and
one production-parser vector on each CPU, with fifteen reachable methods, zero
managed runtime features/helpers, external targets, exception regions, fatal
sites, leaks or shared-image writes.

Receipt: `artifacts/binddrivers-morphos-native-entry-20260924-hunk-bounds-v9/qualification.json`.
This remains fixture evidence. Multi-pair end-to-end scans, malformed tool-type
termination, provider-backed repeat/interleaving, original guest and Workbench
correspondence, installed PURE/resident flags, licensing and package admission
remain open.

### 2026-09-24 MorphOS BindDrivers full-scan PRODUCT pairs

The production MorphOS scan now verifies multiple `PRODUCT` pairs end to end.
The first pair resolves two `ConfigDev` nodes and the second pair resolves a
third; five ordered `FindConfigDev` calls are checked, followed by the exact
three-node prepend chain passed to `SetCurrentBinding`. Cleanup releases all
three nodes through the DiskObject owner.

Receipt `artifacts/binddrivers-morphos-native-entry-20260924-multipair-v10/qualification.json`
passes twenty-four scanner vectors and one separate parser vector per CPU on
68000/020/040. Each main image has fifteen reachable methods and zero managed
runtime features/helpers, external targets, exception regions or fatal sites;
the runs report no leaks or shared-image writes. This closes the supplied-
vector end-to-end multiple-pair case only. Malformed tool-type termination,
provider-backed repeated/interleaved scans, original guest behavior,
Workbench correspondence, installed PURE/resident flags, licensing and
package admission remain open.

### 2026-09-24 MorphOS BindDrivers interleaving and AnchorPath flags

Added two successful instruction-interleaved scanner invocations: one
single-pair and one multiple-pair PRODUCT scan. The fake matcher, DiskObject,
tool-type, PRODUCT, ConfigDev and HUNK memory is distinct for each caller.
Directory fixtures set `APF_DIDDIR` together with unrelated flags and verify
that only `APF_DIDDIR` is cleared before `MatchNext`; file matches preserve the
flags and the empty successful scan retains a zeroed AnchorPath.

Receipt `artifacts/binddrivers-morphos-native-entry-20260924-anchor-flags-v12/qualification.json`
passes twenty-six scanner vectors plus the parser vector per CPU on
68000/020/040. The resident image has fifteen reachable methods and zero
managed runtime features/helpers, external targets, exception regions or
fatal sites; the fixture reports no leaks or shared-image writes. This remains
supplied-vector evidence. Unterminated output from an invalid Icon provider,
real guest concurrency and filesystem behavior, original guest parity,
Workbench correspondence, installed PURE/resident flags, licensing and
package admission remain open.

### 2026-09-26 Workbench LoadResource request and hook static slices

The native candidate now includes the captured 54-byte synchronous request
layout as well as the LoadSeg cache/hook component. Resident-runtime HUNK
qualification compiles both for 68000, 68020 and 68040; each image has 18
reachable methods and no managed runtime features/helpers, external targets,
exception regions or fatal sites. Receipt:
`artifacts/workbench31-loadresource-hook-static-v4/qualification.json`.
The request field mapping is bound separately to the selected HUNK in
`reference-captures/loadresource-wb31-request-protocol-audit-20260926.json`.
Historical host-only layout checks passed here, but subsequent native
execution found different emitted field offsets. That check and unsupported
packed aggregate were removed; the executable results below supersede them.

Static compilation does not exercise the client/server transaction or
callback, validate process scheduling or HUNK lifetime, implement worker
startup/resource actions, or establish guest parity. The original Workbench
file's protection metadata has P clear; no PURE classification is claimed.
Runtime, worker, licensing, package and differential gates remain open.

### 2026-09-26 Workbench LoadResource executable client and hook slices

The corrected client writes the actual Amiga protocol at explicit offsets in
caller-stack storage. Receipt
`artifacts/workbench31-loadresource-protocol-runtime-20260926-v4/qualification.json`
passes nineteen native invocations per CPU on 68000/020/040, including
instruction-interleaved callers, all transmitted fields, live `ReadArgs`
ownership until reply, full-width result/error propagation, null borrowed
handles, parser/allocation/boundary failure and cleanup-clobber preservation.
The first failing native receipt remains in the `20260926-v1` directory;
the host `Pack=2` assertion had not established the emitted ABI.

Receipt
`artifacts/workbench31-loadresource-hook-runtime-0b0711c4c4f64e9da258f57bbdc56978/qualification.json`
passes eleven native scenarios per CPU. These execute actual export calls,
check callee-saved registers and stack, and cover cache aliases/consumption,
unmatched saved-vector ABI, removal, failure cleanup, duplicate install,
null-predecessor rollback, newer-patch retention and failed semaphore attempt
followed by successful retry. They enforce filesystem `Lock` before acquiring
the registry semaphore and nonblocking `AttemptSemaphore` teardown. This
fixture exposed and verified the export's DOS-context initialization fix.

Both suites use the resident runtime and report no managed allocation sites,
runtime features/helpers, external native targets, exceptions, fatal sites or
shared-image writes. Their public OS responses are supplied fixtures; they do
not execute a real worker, original Workbench parser or guest scheduling.
Startup/resource implementation, callback code lifetime, exact original
teardown behavior, PURE/resident classification, rights, package and
differential gates remain open. The selected Workbench file remains P-clear.

### 2026-09-26 Workbench LoadResource registry, resource actions and worker

The new production registry owns copied names and opened library/font/catalog
handles independently of the one-shot LoadSeg cache. Its actual native HUNK
fixture passes ten cases per 68000/020/040, including AddTail ordering,
case-insensitive WORD comparison, failed allocation/retry and close-before-
remove ownership. Receipt:
`artifacts/workbench31-loadresource-registry-runtime-20260926-v2/qualification.json`.

The resource-action/diagnostic fixture passes thirty-two cases per CPU:
`artifacts/workbench31-loadresource-actions-runtime-20260926-v2/qualification.json`.
It executes the native installed hook, source-shaped Resident/font
classification, library/device/font/catalog paths, allocation/provider failure
cleanup, registry/cache retention and diagnostic/IoErr order. A device case
consumes its retained segment through the actual hook. Library/font open
providers do not recursively invoke LoadSeg here. This is a declared fixture
boundary, not proof of loader reentry or guest lifecycle.

The worker fixture passes twenty-six cases per CPU:
`artifacts/workbench31-loadresource-worker-runtime-20260926-v5/qualification.json`.
The probe calls production Dispatch or ReceiveAndDispatch. Checks cover the
54-byte request, invalid flags, no-name versus empty-array behavior, all
opened resource types, exact listing/default text, Locale ID/default pairs,
full-LONG UNLOCK precedence, low-WORD LOCK retention, matcher reset/cleanup,
first-error termination and action error 232, MatchNext counts, preservation
of stored error despite output/cleanup clobbers, and restoration of directory
and streams before reply. Sender arrays/strings are unchanged; reply frees the
message so later native access fails. The supplied stack is 3000 bytes.

The action and worker images have 30 and 29 reachable methods respectively.
No managed allocations, runtime helpers, external native targets, exception
regions, fatal sites or shared-image writes are admitted. Public DOS.LoadSeg
uses precisely one framework intrinsic, parameterless
`BPTR?.GetValueOrDefault`, recorded as `nullable-values`; qualifiers validate
its exact native binding and reject other framework dependencies. Both fresh
receipts bind source/probe/fixture/qualifier inputs before build and verify
their hashes after execution. The full production native project also builds
with zero errors and seventeen pre-existing MorphOS Version CS0649 warnings.

Initial worker qualification retained a failing compiler receipt for
unsupported argument assignment; the same traversal now uses a local cursor.
The resource-action audit independently reviewed the production code against
the selected original binary. Lazy message-catalog lifetime and malformed
font-size scratch semantics remain explicit parity gaps. Complete coordinator
startup/exit, real message queues, cross-command persistence, hook prologue/
epilogue lifetime, original-guest comparison, rights and packaging remain
open. These results do not change the P-clear file classification or shipping
counts.

### 2026-09-26 Exploratory clean Workbench 3.1 runner

The unchanged CopperScreen Lightweight binary completed 2,000 frames with the
available licensed 40.63 ROM and Workbench 40.42 ADF, exit zero and no reported
unsupported feature. Thirty-five passive captures preserve unchanged input
identities and match the no-probe run's final fingerprints. This is a clean
hardware route without the MedPlayer host DOS/Exec overlay, but it is not yet
a qualified 3.1 machine or command runner. The supported product profile
remains 1.3.

Final ExecBase is `0x00C00B00`, outside the probe's ChipRam-only snapshot;
zero complete DOS roots/Process structures could be inspected. The framebuffer
is striped/garbled and does not establish a usable Shell. Readiness is
unproven, not established absent. The next gate is passive slow-RAM capture
before designing guest command/result delivery. See
[`workbench31-clean-runner-readiness-20260926.md`](reference-captures/workbench31-clean-runner-readiness-20260926.md)
for exact arguments, hashes, capture/analysis receipts and framebuffer. No
command coverage, guest parity or shipping credit is assigned.

### 2026-09-26 LoadResource launch, catalog lifecycle and image packaging

The client launch transaction now passes fifteen native cases per CPU (45
total): `artifacts/workbench31-loadresource-launch-runtime-20260926-v2/qualification.json`.
The 14-method image has no managed/runtime features/helpers, allocations,
external targets, exception regions, fatal sites or shared-image writes.
Checks cover serialized lookup, classic process tags, pre-create tail detach,
failure rollback, original first request, parser lease, unchanged sender
payloads, error preservation and balanced Forbid/Permit. Segment headers and
process/message providers are supplied fixtures; no child or real DOS unload
is executed here.

The public NDK-backed design uses independent client/worker CODE hunks and
NP_Seglist plus NP_FreeSeglist so DOS can free worker code after its root
returns. A strict packer and ten independent Python tests verify byte-preserved
CODE and remapped worker relocations, including unrelated load bases and
malformed/alias/overwrite rejection. Three actual probe pairs are packaged in
`artifacts/loadresource-two-hunk-packaging-probes-20260926-00db0d98/`.
They are not full runnable commands and were not executed as combined images.
The [ownership note](reference-captures/loadresource-wb31-worker-image-lifetime-design-20260926.md)
keeps callback entry/exit windows and last-cache-consumption wakeup open.

The source audit now confirms lazy sys/c.catalog calls with a stored pointer
that remains nonzero after CloseCatalog. Production Messages uses an explicit
four-byte state slot and preserves the source's nested listing closes; this
does not assert that repeated closes or subsequent guest pointer use is valid.
Native checks now sample action IoErr after CloseCatalog, test null-open
retries and retained-pointer reuse. The hook's service name is the original
Latin-1 `« LoadResource »`. Fresh current-source receipts are:

- Hook: eleven cases/CPU, `artifacts/workbench31-loadresource-hook-runtime-service-name-20260926-v1/qualification.json`.
- Actions: thirty-four cases/CPU, `artifacts/workbench31-loadresource-actions-runtime-20260926-v6/qualification.json`.
- Worker: thirty-one cases/CPU, `artifacts/workbench31-loadresource-worker-runtime-20260926-v7/qualification.json`.

Action/worker images have 32/31 reachable methods, only their explicitly bound
native nullable-BPTR intrinsic, and zero forbidden runtime dependencies or
shared-image writes. Production native build passes with the same seventeen
Version CS0649 warnings. Full client/worker roots, duplicate-child forwarding,
actual process retirement, callback quiescence, malformed-font fidelity and
original guest parity remain open; no shipping or PURE gate is closed.

### 2026-09-26 Passive slow-RAM follow-up

The [new isolated runner](reference-captures/workbench31-clean-runner-slowram-readiness-20260926.md)
captures slow RAM through a new public read-only engine property. Its focused
observer regression passes 1/1 with zero skips. Source/PDB records bind the
21 engine source files, with the property as the only source difference from
the frozen baseline. The bounded 2,000-frame replay matches CPU/output
fingerprints, cycle count and all 70 chip-RAM/BMP snapshots. Original binaries
and licensed media are unchanged; no host DOS, bus reads, guest writes or input
injection was used.

Saved bytes now establish Exec 40.10, DOS 40.3 and valid sampled task/library
lists, with Initial CLI command names through Workbench startup. At the final
frame the Workbench Process has a CLI, module and streams; the framebuffer
remains garbled. Exact command results/output remain uncaptured. The immediate
next gate is normal guest invocation with a guest-owned completion record.
Installed amitools can insert an authored probe into a disposable ADF copy;
that diagnostic workflow must stay separate from shipping-image admission.
No media was modified in this follow-up. Disk writes by the guest remain
unsupported, so completion data must reside in guest-owned RAM.


### 2026-09-26 Normal startup command capture

The [authored guest probe](reference-captures/workbench31-guest-command-probe-20260926.md)
now executes from the original Workbench startup CLI on a verified disposable
ADF derivative. It uses public ReadArgs and synchronous SystemTagList with
NIL input and the original RAM handler for output. A public named Exec port
points to its allocated completion record. The unchanged passive runner reads
saved chip/slow RAM only; no host DOS services or PC/vector writes are used.

Four original C:Version captures complete, each with 41 snapshots over 2,400
frames and five identical complete observations from frame 2,160 onward.
Ordinary and minimum-39 queries return 0; minimum-999 returns 5 while retaining
the same 17-byte version output. Invalid numeric input returns 20 with 42 bytes
including the Shell failure line; caller post-System IoErr is 115. This field
is not a general guarantee of child Result2 propagation. No generated replacement
was compared in these runs and separate stderr is not captured.

`artifacts/workbench31-guest-command-captures-20260926-v1/qualification.json`
binds all preparation/capture/analysis receipts and the reference Version hash.
All recorded input, runtime, source and snapshot hashes were rechecked after
capture. The same directory contains the combined **24/24** tooling test result
and source identities. The builder preserves all unrelated raw blocks, slack,
metadata and original root bytes; licensed source and derivative media remain
outside the repository. Production package admission is untouched.

This resolves normal clean Workbench invocation and output/return capture for
these cases. It does not close CC03/CC09 as a whole, native CopperStart/MorphOS
execution, fault injection, command parity, resident/PURE lifetime or shipping.


### 2026-09-26 Workbench Version comparison and output correction

The original binary audit and normal guest captures establish minimum-major
comparison with output retained on warnings. The candidate's existing supported
system/RES paths now use signed minimum comparisons, including revision-only
requests and ignoring revision when the actual major is greater. Default named
lookup was not replaced with a LibList shortcut; its original provider ordering
remains required work.

The same review corrected two VPrintf ABI mistakes: system output now consumes
the supplied name before numeric LONGs, and FULL output supplies its second
string in the actual second LONG slot. Earlier fixture shortcuts masked those
errors; the fixture now reads the correct consecutive slots.

`artifacts/version-wb31-native-20260926-comparison-v1/qualification.json` passes
32 invocations per CPU on 68000/020/040, 96 total, with 17 reachable methods,
zero shared-image writes/leaks and no managed runtime dependencies. Twelve
source hashes and 33 compiled-input identities are unchanged. Independent code
review found no scoped regression. See
`reference-captures/version-wb31-named-comparison-audit-20260926.json` and the
Version contract for source offsets and scope.

These remain supplied-vector checks of the replacement. The four real guest
captures executed only original Version. Default named/file/unit/internal
providers, complete classic system/RES output and parsing, secondary-error
semantics, original/replacement guest comparison, resident/PURE lifecycle,
licensing and packaging remain open. Shipping admission is unchanged.


### Workbench Resident lookup and ten replacement guest pairs (2026-09-26)

The Workbench candidate now implements the original first named provider: a
case-insensitive walk of the full-name Exec Resident table, including high-bit
continuation pointers, canonical rt_Name, authoritative rt_Version and parsed
signed revision (missing revision is -1). It does not skip ahead to LibList
when this provider misses. The original parses UNIT/INTERNAL/RES but does not
read those slots; only FILE changes search flags. Their syntax is still parsed
by public DOS ReadArgs. Numeric parse failure now returns the original level 20.

FULL now uses the source's DOS StrToLong, Utility Date2Amiga and DOS StrToDate/
DateToStr behavior: low-word day/month/year+1900, signed DateStamp arithmetic,
format 0 fallback, format 4 output and the original first-line Extra placement.
Temporary state belongs to the invocation; the optional Extra copy is freed.
The [FULL/options audit](reference-captures/version-wb31-full-options-audit-20260926.json)
records the original offsets and separately captured results.

`artifacts/version-wb31-native-20260926-resident-first-v2/qualification.json` passes **58 cases per CPU, 174 total**, on 68000/020/040, including
two overlapping named invocations. All images have 23 reachable methods, no
managed dependencies, leaked fixture resources or shared-image writes.
Thirteen source and 33 compiled-input identities were rechecked after the runs.
The 68000 HUNK is 6,912 bytes, SHA-256
`143af192988613f3b29449cb3b095fcd3389b90c924e330f7ab8f8d21603fec7`.

`artifacts/workbench31-guest-version-pairs-20260926-v2/qualification.json` binds **ten exact original/replacement guest pairs** for ordinary
and uppercase dos.library, RES and uppercase RES, FULL, lower/higher requested
major, revision-only warning, higher-major/large-revision success, and invalid
numeric input. Each 2,400-frame run uses a fresh external derivative, original
Kickstart 3.1/DOS and the frozen passive runner. Exact output, return and caller
post-System IoErr agree; saved snapshots and candidate sources/compiled inputs
were rechecked. Independent review re-decoded all ten pairs and found no
admission or comparison errors. The original C/Version protection word 0
(P clear) is preserved; the replacement is experimental, not promoted to PURE.

The earlier candidate's ordinary-name mismatch and RES match remain retained
as negative/positive controls. All 39 builder/analyzer/comparator tests pass.
No original media, ROM or emulator behavior was changed.

Still open: later named providers/FILE, complete no-name system output and the
full grammar/failure matrix; DOS entry minimum 36 versus the original 37 check;
Utility opening after ReadArgs rather than before; success IoErr normalization;
real OS concurrent/resident/PURE lifetime and cleanup; CopperStart/MorphOS
execution; complete media/installed metadata, rights and package qualification.
Caller post-System IoErr is not generally proven child Result2. Ten matching
cases do not complete the command or change the 0/200 shipping admission count.

### 2026-09-27 Workbench Which path traversal increment

The classic `Which` body now walks the full CLI path list without its former
64-entry cap. A constant-space cycle check returns an error if a full search
reaches a cyclic list. The refreshed Workbench resident HUNK qualification
passes 19 supplied invocations per CPU on 68000/020/040 (57 total), including
65 path nodes and cycle cleanup; no shared-image writes or fixture leaks were
observed. Receipt:
`artifacts/which-wb31-native-20260927-unbounded-path-v2/qualification.json`.
The shared MorphOS path walk also passes its 17-case-per-CPU regression at
`artifacts/which-morphos-native-20260927-unbounded-path-v1/qualification.json`.

Two fresh original/candidate guest pairs match for the root directory
`C:Which C:` (15 output bytes, return 0, caller IoErr 0) and the explicit miss
`C:Which C:CopperOSNoSuchEntry` (empty output, return 5, caller IoErr 205).
Comparison receipts are recorded in the `Which` contract and progress log.
These are bounded cases; they do not prove child Result2, full option or lookup
ordering, original PURE/resident lifecycle, MorphOS aliases or shipping
admission. The 0/200 command and 0/246 profile completion counts are unchanged.

### 2026-09-27 Workbench Which bare-name and explicit-path ALL parity

Two original guest captures exposed route behavior missing from the supplied
vectors: bare-name `ALL` returned the same C: path twice, while path-qualified
`C:Execute ALL` returned it once. The first candidate returned one line for
the bare name and searched an explicit path through every CLI path entry. The
body now uses DOS `FilePart` to identify a path-qualified argument, skips CLI
path enumeration for it, and adds the observed C: fallback for bare names
after the current-directory and CLI-path candidates. Original-only captures
and the first mismatching candidate captures are retained as diagnostic
controls.

The refreshed native fixture passes 21 vectors per resident CPU on
68000/020/040 (63 total), including the C: fallback, explicit-path skip,
65-node path walk, and cyclic-list cleanup, with zero shared-image writes:
`artifacts/which-wb31-native-20260927-routes-v3/qualification.json`. The
68000 HUNK is 4,880 bytes, SHA-256
`ddbd86bf747f54824febc614e02888fdda3881e29c6d5e76e97bacdcf4f82bbd`. The
shared MorphOS implementation still passes 17 vectors per CPU at
`artifacts/which-morphos-native-20260927-routes-v1/qualification.json`.

The two new 2,400-frame guest pairs match exactly: `C:Which Execute ALL`
emits `Workbench3.1:C/Execute\n` twice (46 bytes), and
`C:Which C:Execute ALL` emits it once (23 bytes). Both return 0 and leave
caller post-System IoErr 0. Receipts:

- `artifacts/workbench31-guest-command-which-all-execute-candidate-20260927-v2/effect-comparison.json`
- `artifacts/workbench31-guest-command-which-all-explicit-c-execute-candidate-20260927-v2/effect-comparison.json`

These join the root-directory and explicit-miss comparisons above, but do not
prove child `Result2` or full route order. Current-directory/assign collisions,
non-internal option combinations, break and failure breadth, long individual
names, MorphOS aliases, installed PURE/resident lifecycle, rights, packaging
and complete differential admission remain open.

### 2026-09-27 Workbench Which classic non-internal switch matrix

Captured original and current resident candidate executions for each of the
eight classic `NORES`/`RES`/`ALL` combinations with one filesystem-resolved
non-internal name (`Execute`) and one absent name (`CopperOSMissingWhich`).
All 16 exact original/candidate comparisons match command text, output bytes,
command return and caller post-System IoErr. The summary receipt
`artifacts/workbench31-guest-command-which-switch-matrix-20260927-v1/evidence-summary.json`
links every comparison.

For found `Execute`, no switches and `NORES` each emit the C: path once;
`ALL` and `NORES ALL` emit it twice. `RES`, `NORES RES`, `RES ALL`, and
`NORES RES ALL` emit nothing. The four restrictive miss cases return 5/205;
the two NORES+RES conflicts return 5/0. All absent-name combinations emit no
output, with the same return/IoErr split. Caller post-System IoErr does not
prove child `Result2`. This bounded matrix leaves other name categories,
route/assign collisions, failure and break behavior, long names, MorphOS
aliases, installed PURE/resident lifecycle, rights and packaging open.


### 2026-09-27 Workbench Which parser and refreshed pair checkpoint

The classic Workbench resident entry now returns the original `ReadArgs`
error effects for two captured failures by using DOS `PrintFault`, preserving
IoErr and returning WARN. `C:Which` matches the original message, return 5
and caller IoErr 116; `C:Which Execute MYSTERY` matches message, return 5
and caller IoErr 118. Receipts are recorded in the `Which` contract. This
parser change is deliberately profile-specific; MorphOS remains open.

The updated Workbench HUNK passes 22 supplied vectors per CPU on resident
68000/020/040 (66 total) at
`artifacts/which-wb31-native-20260927-readargs-v1/qualification.json`; its
68000 image is 4,932 bytes, SHA-256
`be283d619f183267e4b1b25f7d2ecb280815a373e4960550b66fc45a79c5d189`.
Shared MorphOS code passes 17 vectors per CPU at
`artifacts/which-morphos-native-20260927-readargs-v1/qualification.json`.
Fresh current-HUNK Workbench captures match in all 20 route and option-matrix
cases, plus the two parser pairs. Summary receipts are
`artifacts/workbench31-guest-command-which-route-cases-20260927-v2/evidence-summary.json`
and
`artifacts/workbench31-guest-command-which-switch-matrix-20260927-v2/evidence-summary.json`.

These bounded cases do not establish child Result2, other lookup categories,
all diagnostics or break/failure paths, MorphOS aliases, original
PURE/resident lifecycle, rights, packages or shipping admission.


### 2026-09-27 Workbench Which `?` parser/help cases

The current Workbench candidate matches two fresh original guest comparisons:
`C:Which ?` prints the exact classic template and required-argument fault,
return 5/IoErr 116; `C:Which Execute ?` prints the template and the normal
C: path, return 0/IoErr 0. This confirms that with FILE present the observed
DOS `ReadArgs` help interaction continues into lookup. Aggregate receipt:
`artifacts/workbench31-guest-command-which-readargs-help-20260927-v1/evidence-summary.json`.
These do not establish MorphOS help/grammar or the full option matrix.


### 2026-09-27 Workbench Which internal-name option matrix

Eight fresh original/candidate guest pairs cover every classic switch
combination for internal `CD`; all command text, output bytes, return values
and caller post-System IoErr agree. Default/`RES`/`RES ALL` emit `INTERNAL
CD`; `ALL` also emits it but returns 5/205; `NORES RES` conflicts return
5/0. The aggregate 32-case current-HUNK receipt is
`artifacts/workbench31-guest-command-which-current-hunk-20260927-v1/evidence-summary.json`.
The matrix remains bounded and does not prove child Result2 or MorphOS alias
semantics.

### 2026-09-27 MorphOS Which extended-switch candidate matrix

The MorphOS native candidate passes 85 supplied vectors per CPU across
68000/020/040 at
`artifacts/which-morphos-native-20260927-switch-matrix-v3/qualification.json`.
The fixture exercises all 32 combinations of `NOALIAS`, `ALIAS`, `NORES`,
`RES`, and `ALL`, each with candidate found/missing inputs, plus the
alias-only-miss/no-fallthrough case. Workbench's shared fixture runner also
passes 31 vectors per CPU at
`artifacts/which-wb31-native-20260927-switchmatrix-regression-v1/qualification.json`.

This is candidate native-vector coverage, not original MorphOS parsing or
output parity. Alias line formatting, alias/resident/path precedence,
conflicting switches, exact return/IoErr, installed flags, lifecycle, package
placement and licensed guest comparison remain open.

### 2026-09-27 CC11 Type stream failure paths

Current Workbench and MorphOS Type resident entries pass 17 and 16 supplied
vectors per CPU, respectively, on 68000/020/040. The fixtures add a DOS Read
failure and a Write failure following a positive short write, then check the
diagnostic, selected IoErr, partial output, and resource closure. Receipts:
`artifacts/cc11-type-wb31-native-20260927-io-failures-v1/qualification.json`
and
`artifacts/cc11-type-morphos-native-20260927-io-failures-v1/qualification.json`.
Only the MorphOS failure semantics are backed by the inspected 50.6 source;
Workbench behavior remains a candidate assumption. These are supplied DOS
vectors, not original-command comparisons or release admission.

### 2026-09-27 CC11 Search per-file errors

Source inspection of the hash-bound MorphOS Search member shows that Open,
Read, and Seek errors for an individual file are handled as per-file misses;
the matcher continues, and successful traversal clears IoErr. A dangling
recursive soft link is warned about and skipped. The MorphOS resident entry
now passes 46 supplied vectors per CPU, including these outcomes, at
`artifacts/cc11-search-morphos-native-20260927-source-file-errors-v1/qualification.json`.
The Workbench syntax candidate passes 19 regression vectors per CPU at
`artifacts/cc11-search-wb31-native-20260927-source-error-regression-v1/qualification.json`.
Both three-CPU receipts report 43 reachable methods, no image writes, and no
fixture leaks. Workbench expectations remain candidate-modeled. The vectors
do not verify original guest output, real filesystem-handler behavior, packed
correspondence, PURE/resident lifecycle, or release admission.

### 2026-09-27 CC18 MorphOS Break option and target matrix

The MorphOS Break resident fixture now covers all sixteen C/D/E/F combinations
and the official 50.6 source's target precedence: nonzero PROCESS suppresses
PORT lookup; zero PROCESS permits PORT lookup; and if both the zero-process
fallback and PORT lookup fail, the process diagnostic takes precedence. The
three-CPU receipt passes 32 DOS/Exec vectors per CPU with 16 reachable methods,
no shared-image writes and no fixture leaks:
`artifacts/break-morphos-native-20260927-mask-precedence-v1/qualification.json`.
Workbench Break was requalified for 24 vectors per CPU after the shared test
runner change at
`artifacts/break-wb31-native-20260927-morphos-matrix-regression-v1/qualification.json`.
This increases source-backed fixture coverage; it does not establish original
MorphOS output/effect parity, PID numbering/reuse, target races, PURE/resident
lifecycle, or release admission.
