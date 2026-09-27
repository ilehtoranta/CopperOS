# Which contract

Profiles: `wb31`, `morphos320`. Goal steps: CC01 and CC10.
Recorded: 2026-08-30. Status: **partially verified; no native parity claim**.

## Reference identity and templates

| Profile | Reference | Verified member identity |
| --- | --- | --- |
| `wb31` | `Workbench3.1:C/Which`, original Disk 2 ADF | 1,068 bytes; version `which 37.1 (12.1.91)`; SHA256 `35f7f3fee29dd2d56f482537af1c5f5a88b552080e6c26be14f455a3241b7157` |
| `morphos320` | `MorphOS/C/Which` in [official ISO](https://www.morphos-team.net/morphos-3.20.iso) | ISO block 178832, 3,360 bytes; version `Which 50.6 (13.8.2018)`; SHA256 `7ce885056a49d2757e4f551bd274aff3c96404989cb0a5e650c3e5a95b0c9269` |

Original media is local under `D:/TestData/TestImages/`, ZIP
`Workbench v3.1 rev 40.42 (1994)(Commodore)(M10)(Disk 2 of 6)(Workbench)[!].zip`;
the ADF member uses the same base name. ADF SHA256:
`a8f167bad2897e8c7f2cfe73de7a9f8dad10c7a5b1f78ca17f818ab17278b985`.
Its Which file header is block 453. The MorphOS ISO is local at
`D:/TestData/MorphOSReferences/morphos-3.20.iso`; logical blocks are 2,048 bytes.
The verified complete ISO SHA256 is
`3761e191124cfd204a490915f8d285d55969e95b66c5c3cd1cf854bc71dab911`.

The exact classic template is embedded in the original binary:

```text
FILE/A,NORES/S,RES/S,ALL/S
```

The MorphOS packed `7f4d4f53` payload has not been decoded. The following is
the candidate from the [MorphOS Library documentation](https://library.morph.zone/Shell_Commands/Which),
not a verified binary/help transcription:

```text
FILE/A,NOALIAS/S,ALIAS/S,NORES/S,RES/S,ALL/S
```

Use four or six invocation-local ReadArgs slots according to the profile.
Keep the classic `?` template and rejection behavior separate from the extended
profile. Do not add MorphOS alias switches to classic mode.

## Lookup contract and open evidence

The original binary contains the format strings `INTERNAL %s`,
`INTERNAL %s ;(DISABLED)`, and `RES %s`. These are evidence of output categories,
not proof of their complete emitted line endings or the condition for each
branch. Do not replace the literal `RES` with a longer word solely because
prose documentation describes it that way.

The hash-bound disposable fixture
[`which-wb31-basic-lookup.json`](../reference-captures/which-wb31-basic-lookup.json)
now establishes the two direct positive forms: both `Which Execute` and
`Which C:Execute` write `Workbench3.1:C/Execute` plus LF. Its missing-name
attempt stops a `FailAt 20` startup before its `END` marker; it is evidence of
an error-level outcome only, not an exact return code, `IoErr`, or diagnostic.
The separate
[`which-wb31-internal-category.json`](../reference-captures/which-wb31-internal-category.json)
fixture establishes the exact internal label: `Which CD` writes `INTERNAL CD`
plus LF.
The
[`which-wb31-resident-category.json`](../reference-captures/which-wb31-resident-category.json)
fixture establishes the resident-only label: after `Resident C:Execute PURE`,
`Which Execute RES` writes `RES Execute` plus LF; the fixture then removes the
resident before its `END` marker.
The `NORES` fixture
[`which-wb31-nores.json`](../reference-captures/which-wb31-nores.json)
instead writes `Workbench3.1:C/Execute`. The `ALL` fixture
[`which-wb31-all-resident-path.json`](../reference-captures/which-wb31-all-resident-path.json)
writes `RES Execute` first and then the identical disk path twice. The latter
is a controlled proof that classic `ALL` does not deduplicate equivalent path
discoveries; neither fixture establishes its exact final status. The later
marker-based
[`which-wb31-resident-default-options.json`](../reference-captures/which-wb31-resident-default-options.json)
binds all four forms in one fresh fixture: default `Which Execute` and
`Which Execute RES` both write `RES Execute`; `NORES` writes the disk path;
and `ALL` repeats the resident-plus-two-disk ordering. It reaches `END` after
resident removal under `FailAt 20`, establishing only that those four calls do
not return 20 or above in that configuration.
The separate status fixture
[`which-wb31-status.json`](../reference-captures/which-wb31-status.json)
establishes exact `RC=0` and `Result2=0` after default `Which Execute` and
`Which Execute RES` in that resident configuration. It stops on its following
`NORES` call, so it makes no status claim for that or later invocations.
The isolated
[`which-wb31-nores-status.json`](../reference-captures/which-wb31-nores-status.json)
fixture now supplies that missing result: `Which Execute NORES` writes the disk
path and leaves `RC=0`, `Result2=0` before the resident is removed.
The corresponding
[`which-wb31-all-status.json`](../reference-captures/which-wb31-all-status.json)
fixture establishes that the resident-plus-two-disk `ALL` output also leaves
`RC=0`, `Result2=0` and reaches `END` after removal.
The corresponding internal-category option fixture
[`which-wb31-internal-options.json`](../reference-captures/which-wb31-internal-options.json)
shows that default `Which CD` and `Which CD RES` both emit `INTERNAL CD`, while
`Which CD NORES` emits no line and remains below that threshold. `Which CD ALL`
emits `INTERNAL CD`. The isolated
[`which-wb31-internal-all-status.json`](../reference-captures/which-wb31-internal-all-status.json)
now establishes its exact result as `RC=5`, `Result2=205`; this is a precise
internal-only `ALL` observation, not a result claim for the other internal or
missing cases.
The separate
[`which-wb31-no-match-status.json`](../reference-captures/which-wb31-no-match-status.json)
establishes two other classic no-match results: `Which CD NORES` and `Which
CopperOSMissingCommand` both emit no captured line and leave `RC=5`,
`Result2=205`. It does not establish `RES`-only, inaccessible, or diagnostic
behavior. The targeted
[`which-wb31-res-missing-status.json`](../reference-captures/which-wb31-res-missing-status.json)
also binds `Which CopperOSMissingCommand RES` to the same `RC=5`,
`Result2=205` pair.
The partial
[`which-wb31-internal-combinations-status.json`](../reference-captures/which-wb31-internal-combinations-status.json)
freezes default, `NORES`, `RES`, `ALL`, `NORES RES`, and `NORES ALL` for the
internal `CD` name. In particular, `NORES RES` returns `RC=5`, `Result2=0`,
whereas `NORES ALL` returns `RC=5`, `Result2=205`. Its following `RES ALL`
call is separately captured in
[`which-wb31-res-all-status.json`](../reference-captures/which-wb31-res-all-status.json):
it emits `INTERNAL CD` and returns `RC=0`, `Result2=0`. The final
[`which-wb31-nores-res-all-status.json`](../reference-captures/which-wb31-nores-res-all-status.json)
fixture establishes `NORES RES ALL` as no output with `RC=5`, `Result2=0`.
Together, these short probes cover all eight classic switch combinations for
the internal `CD` name; they do not establish the same combinations for other
lookup categories.

The [converted AmigaDOS 3.1 reference](https://www.jaruzel.com/amiga/amiga-os-command-reference-help/which.html)
is a secondary manual transcription: it describes resident, current-directory,
path-list and C: lookup, returning WARN/5 for absence. It also describes
directory lookup and duplicate results with ALL. Bind those expectations to
the original binary by native capture before declaring them frozen.

| Option | Meaning to verify | Required success and failure cases |
| --- | --- | --- |
| `FILE` | Required name/path; may identify a command, ordinary file or directory | Found/missing relative and absolute names, spaces, case, empty string, parent/root path, inaccessible item |
| `NORES` | Exclude resident lookup | Matching disk and resident names, resident-only match, no match |
| `RES` | Restrict lookup to residents | Internal/user/system/disabled entries, disk-only match, no match |
| `ALL` | Continue through all eligible lookup routes | Multiple locations, same object reached through different paths, missing item, mixed categories |
| `NOALIAS` (`morphos320`) | Exclude aliases | Alias shadows a disk/resident name; alias-only match; none |
| `ALIAS` (`morphos320`) | Restrict lookup to aliases | Ordinary/empty/recursive-looking alias values, missing alias, other categories only |

Capture default alias position and label, resident-before-disk order, C:/path
order, command-name case preservation, path spelling, assign expansion,
multi-assign behavior, execute/script protection handling and disabled internal
display. Directory lookup means an implementation must not simply reuse a
launcher that excludes every non-executable object.

NORES+RES and NOALIAS+ALIAS are unresolved; do not invent conflict errors.
The bounded Workbench matrix below now covers all eight classic switch
combinations with one found non-internal filesystem name and one absent name.
Expand that matrix to other lookup categories/routes; capture all 32 MorphOS
combinations, each with found and missing cases. ALL must not deduplicate
results merely because two search routes resolve to the same lock, unless
reference evidence requires it. Exact match ordering is observable behavior.

The Workbench resident candidate now exercises all eight switch combinations
for the captured internal-only `CD` case through supplied post-ReadArgs vectors.
It also traverses a two-node CLI path list after a resident match and emits the
two repeated path results in captured order, preserving duplicates. The MorphOS
candidate uses public DOS `FindVar(name, LV_ALIAS)` for exact-name alias lookup.
Its supplied vectors cover alias-only hit/miss, default alias hit, `NOALIAS`
suppression, file-path fall-through, ordinary path lookup, duplicate CLI path
ordering and interleaving. The candidate currently reports `ALIAS <name>` before
resident/filesystem results; the MorphOS documentation does not specify that
line format or exact order, so both remain inferred and unverified against the
shipped binary. These fixtures do not establish real parser behavior, packed
correspondence or full original-guest parity. Receipts:
`artifacts/which-wb31-native-20260923-all-order-v1/qualification.json`,
`artifacts/which-morphos-native-20260923-all-order-v1/qualification.json`, and
`artifacts/which-morphos-native-20260927-findvar-v2/qualification.json`.

## Existing owner and public APIs

The public alias lookup boundary is available in both selected API sets. The
original NDK 3.1 `DOS_LIB.FD` lists `FindVar(name,type)` in the variable
functions; its local reference copy has SHA-256
`a9b24c1d9fb1053955dfb28eb8dabe92eff5dbde10b952e67d6cee2a80c5c228`. The
MorphOS 3.20 SDK declares `struct LocalVar *FindVar(CONST_STRPTR name,
ULONG type)` in its [official DOS prototypes](https://morphos-team.net/sdk/includes/clib/dos_protos.html).
The [MorphOS Which documentation](https://library.morph.zone/Shell_Commands/Which)
lists `NOALIAS` as excluding alias lookup and `ALIAS` as alias-only lookup. The
candidate uses those facts to call `FindVar` for the requested exact name and
the SDK's alias type. Neither source documents the emitted alias line, lookup
ordering, status/IoErr, or conflicting switch combinations. Those behaviors
remain open until captured from the shipped command or an original MorphOS
guest.

Reuse the Shell's alias, resident and path ownership. Existing
`IShellScriptPlatform` / `TryLookupScriptCommand` supplies a useful lookup seam,
but its first-result launch classification does not by itself prove the full
Which contract. Extend a shared lookup/enumeration service if necessary; do
not implement a separate path list, alias expansion engine, resident registry,
or command launcher inside Which. Finding an alias does not authorize executing
or expanding its contents as a command.

Prefer `Cli`, `FindVar`/appropriate Shell alias access, `FindSegment`, `Lock`,
`Examine`, `NameFromLock`, `UnLock`, `SameLock` where needed, and DOS output
calls. Do not call `LoadSeg` or execute a match just to determine its path.
Preserve current directory, CLI path, alias values and resident counts.

Primary API reference: `NDK_3.1/DOCS/DOC/DOS.DOC` in
[AmigaDeveloperCD.iso](D:/TestData/AmigaDeveloperCD.iso), SHA256
`2e22d1d1b7c00fac5757eead0ef4d49a01c7666f0db5850735193c9597a7cee2`.
Its FindSegment contract requires explicit resident-list protection; it does
not lock the list or increment use counts. Nonnegative entries used outside
the protected interval need a balanced protected use-count increment/decrement.
Negative counts identify system, internal or disabled entries and must not be
modified. Do not hold Forbid while doing blocking output: copy the needed
metadata or retain it by its documented lifetime rules, then release protection.

ReadArgs strings remain borrowed from RDArgs until FreeArgs. Own and release
all obtained locks and temporary names. Output belongs to the caller. Scratch
buffers, error capture and enumeration state belong to each invocation.

## Status, cancellation, purity and fixtures

The classic on-disk header protection is `0x00000000`, with its FFS header
checksum validated. This is evidence that the inspected floppy P bit is clear,
not a proof that the command is unsafe for resident use. Installed policy for
classic mode still belongs to CC00. MorphOS Which is **required pure by
original installer design**: `hdinstall.fixc` line 88 adds P. Script SHA256:
`46751fd064329077fe3f1ebeff6ea47c13159dd84841fac83d2043fb270ca8df`
(2,478 bytes, ISO block 6348). Actual installed flags remain unobserved; ISO
POSIX modes are not AmigaDOS protection. Setting P does not itself install a
resident. Follow the CC00 [manifest](D:/Koodit/GIT/CopperOS/docs/Commands/Workbench31MorphOS320/command-inventory.json).

Success, miss, parse failure, inaccessible path, interrupted search, allocation
failure and failed output need separate captured return/IoErr results. WARN/5
for a classic miss is a manual-derived candidate; MorphOS miss semantics and
precise error text are not yet established. Preserve the immediate command
IoErr during observation rather than obtaining it after another Shell command.

| Fixture group | Required observations |
| --- | --- |
| `WH-PARSE` | Both exact profile templates; every option, missing/empty FILE, unknown option, repeated switches, quoted keyword names, DOS escapes, `?` continuation and EOF |
| `WH-ORDER` | Controlled aliases, residents, current directory, two path directories, C: and multi-assigns; same names in each; capture complete ordered output |
| `WH-CATEGORY` | File, directory, script, non-executable file, internal, disabled internal, positive-use resident, system resident, alias; present and absent |
| `WH-COMBINATION` | 8/32 switch combinations with found/missing inputs; conflicting inclusion/exclusion switches and ALL duplicates |
| `WH-FAIL` | Inaccessible/broken assign, lock/name allocation failure, short/failed output, break during a long search; exact diagnostics, return and IoErr |
| `WH-LIFETIME` | Repeated and overlapping resident invocation; no changed CLI paths/current directory/aliases, no retained locks or resident counts |

## Completion gates

- [ ] Verify MorphOS's exact template and freeze each output label/line format.
- [ ] Capture lookup order, option combinations, exact return levels, IoErr and break.
- [ ] Bind installed P/resident policy and run the fixture matrix against the
  original releases and required CopperOS executable variants.

The official partial MorphOS command source archive contains no Which source.
All observations above are reference facts or marked candidate behavior, not a
license to copy reference implementations. No runtime test is claimed here.

## Bounded MorphOS native candidate

`MorphOS320WhichEntry` now provides a separate DOS 37 resident boundary with
the six-slot candidate template above. It shares the public DOS resident,
current-directory and CLI-path lookup owner with classic Which. `ALIAS` and
`NOALIAS` are parsed but fail closed with `ERROR_NOT_IMPLEMENTED` until the
MorphOS alias provider and exact packed grammar are captured; ordinary lookup
paths remain independent of that provider.

The supplied fixture covers internal/resident/direct results, `ALL`, parser
and allocation failure, startup boundaries, alias-provider gaps and
interleaved calls. The current-source receipt is
`artifacts/cc10-which-morphos-native-20260922-v1/qualification.json`.

| CPU | HUNK bytes | SHA-256 | reachable methods | invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 4,340 | `1c6c65ba2895fcace2d9859d385aefec215489e0e22f82a5a514a932dc336c38` | 16 | 16 |
| 68020 | 4,408 | `5c49abc2679a17caefe040a4ce12a9c836c446f09919de15d5cd8d5f47c70cb3` | 16 | 16 |
| 68040 | 4,328 | `b8c0a1c4ec6d77340e54c7fe894a1f70f6be1e393ffc2d7b8c9399b2f6fc3a39` | 16 | 16 |

This is bounded native evidence only; the official MorphOS binary's exact
template/output and alias ordering, original guest parity, PURE/resident
lifecycle, licensing, package admission and differential gates remain open.

## Implemented bounded presentation primitive

`WhichOutputFormatter` now writes the three observed classic result forms with
caller-owned guest storage and exact-write checking: a resolved path, `INTERNAL
<name>`, and `RES <name>`. It has no lookup, resident acquisition, option,
diagnostic, or command-entry responsibility. Four focused host cases cover the
three reference-backed lines plus short-write and overlapping-buffer rejection.

## Bounded Workbench 3.1 native semantic slice

`Workbench31WhichCommand` now parses the verified classic template through the
shared invocation-owned `ReadArgs` lease. It probes system and user segments
with `FindSegment` only inside a short `Forbid`/`Permit` interval, copies no
segment metadata, and performs output only after `Permit`. It resolves ordinary
objects through `Lock`, `NameFromLock`, and `UnLock`, and walks the current
CLI's DOS-owned `CommandDirectory` path locks using `NameFromLock` and
`AddPart`; therefore it does not create a private command-path or resident
registry. Every acquired file lock is released before the next candidate, and
the command owns only one invocation-local 1,024-byte scratch allocation.

The current path walk traverses the complete DOS-owned CLI list without a
64-node count cap. It uses constant-space cycle detection and reports a cyclic
list as an implementation error when lookup reaches the cycle. A native case
now traverses 65 entries under `ALL`; a separate cycle case checks cleanup.
Each individual candidate still uses a 1,024-byte scratch buffer, so longer
path-name behavior remains open. The body preserves a failed `NameFromLock` or
output `IoErr` across `UnLock`. It deliberately does not yet claim exact
switch-conflict behavior for non-internal objects, disabled internal handling,
all error text, break semantics, long individual names, MorphOS aliases/options,
installed purity, or full command qualification. It does preserve the captured internal-only
`ALL` result by returning WARN with `ObjectNotFound` after emitting `INTERNAL`
when no user resident or filesystem route matches. It likewise maps the
captured classic no-match path to WARN with `ObjectNotFound`. The independent
[`Commands.WhichNativeRoot`](D:/Koodit/GIT/CopperOS/tests/Commands.WhichNativeRoot)
opens DOS and calls the body from a native entry without importing the unrelated
archive adapter. Its explicit cleanup path releases the scratch allocation and
ReadArgs lease before restoring IoErr and closing DOS; the command contains no
managed exception region. [`qualify_which_native_entry.ps1`](D:/Koodit/GIT/CopperOS/tools/Commands/qualify_which_native_entry.ps1)
builds the resident YOLO HUNK matrix:

| CPU | Bytes | SHA-256 |
| --- | ---: | --- |
| 68000 | 3,844 | `e1c42049d869b1b4e072bb7775c588ebdd88bdcfa09bb5edfbddd79bccec1e04` |
| 68020 | 3,900 | `a39049038c2b2332683d345255349addaef1f14177320d4e41e55c2d5fd7e8e5` |
| 68040 | 3,832 | `ae3cefcdd9aa8ace9394e971adda72b496eb8f6473a2a7b7ae5ac2466d85ccbe` |

Each static report has 14 reachable methods, zero managed allocation sites,
exception regions, fatal machine-fault sites, helpers and external targets. The
SDK `nullable-values` feature remains through DOS `Lock`.

The same qualifier now executes each HUNK through the private Copper68k runner:
12 supplied post-`ReadArgs` DOS/Exec-vector invocations per CPU, 36 total. The
matrix covers internal and resident labels, a direct locked path, internal-only
`ALL`, the captured `NORES RES` conflict, a missing resident-only search, and
parser/allocation failure, repeated and instruction-interleaved calls. Every
CPU reuses one protected image with zero image writes and balances the
invocation-owned RDArgs, scratch buffer, locks, and resident-list
`Forbid`/`Permit` intervals. These are native ABI and cleanup checks only: the
vectors do not provide a real DOS parser, Workbench filesystem/resident
registry, original comparison, installed pure lifecycle, or MorphOS behavior.

The current Workbench requalification is recorded at
`artifacts/which-wb31-native-20260912-qualified/qualification.json`. It retains
12 supplied invocations per CPU; the 4,340/4,408/4,328-byte HUNKs have hashes
`2bcb83887615664803c7de177fd4951a1f6b43496cd3278d5890e4064860b83c`,
`3fc1206dccf6a4e268c764b46d0aa4ad7806f3293818c076073373b442e43d1c`, and
`d580060af1eca01d65d2f25f499e86413d992075c933d54fc2246944ecf43a82`.
This requalification does not close the open reference, packaging, or
resident-lifecycle gates.

## Workbench unbounded path traversal and guest pairs (2026-09-27)

The current-source Workbench HUNK removes the arbitrary 64-node CLI path cap.
Its supplied-vector suite passes 19 invocations per CPU (57 total) on resident
68000/020/040 images, including a 65-entry `ALL` walk and a cyclic-list error
case, with balanced locks and no shared-image writes:
`artifacts/which-wb31-native-20260927-unbounded-path-v2/qualification.json`.
The 68000 HUNK is 4,548 bytes with SHA-256
`9b38263864c5b84ac23a1477542c38f0e4db94d9a5bc41ae7125f75c860bce4b`; the
68020 and 68040 hashes are `7681fba78c099f99a0424a17ee0f8ac2d25edc39f01519c660e2bf70740bbd48`
and `ffe934ffff9682e06952943616bef97e6c055c8c975099bcefe3fae6e4d7bc8b`.
The shared MorphOS path-walk regression passes its existing 17 invocations per
CPU at `artifacts/which-morphos-native-20260927-unbounded-path-v1/qualification.json`.

Two fresh 2,400-frame original/candidate guest comparisons exercise the real
Workbench DOS parser and filesystem:

- `C:Which C:` emits `Workbench3.1:C\n` (15 bytes), returns 0 and leaves
  caller post-System IoErr 0. Receipt:
  `artifacts/workbench31-guest-command-which-c-directory-candidate-20260927-v2/effect-comparison.json`.
- `C:Which C:CopperOSNoSuchEntry` emits no bytes, returns 5 and leaves caller
  post-System IoErr 205. Receipt:
  `artifacts/workbench31-guest-command-which-missing-cpath-candidate-20260927-v2/effect-comparison.json`.

These comparisons establish only the root-directory and explicit missing-path
cases. The caller IoErr is not a proven child `Result2`. Same-name collision
ordering, the full switch matrix on non-internal objects, break/error paths,
MorphOS alias behavior, actual PURE/resident lifecycle, packaging, licensing,
and the remaining reference comparisons stay open.

## Workbench bare-name and explicit-path `ALL` follow-up (2026-09-27)

The real Workbench comparisons exposed a route difference that the supplied
vectors did not cover: the first candidate emitted one line for bare-name
`ALL`, while the original emitted the same C: path twice; feeding `C:Execute`
through every CLI path entry also duplicated the candidate line. The resident
body now uses DOS `FilePart` to recognize path-qualified `FILE`, skips CLI path
enumeration for that complete path, and appends the observed `C:` fallback for
bare names after current-directory and CLI-path candidates. The original-only
and mismatching first-candidate receipts remain preserved as diagnostic
controls; they are not parity evidence.

The refreshed Workbench native fixture passes 21 invocations per CPU on
68000/020/040 (63 total), including explicit-path skip, C: fallback, 65 path
nodes, and cycle cleanup, with no shared-image writes:
`artifacts/which-wb31-native-20260927-routes-v3/qualification.json`. Its 68000
HUNK is 4,880 bytes, SHA-256
`ddbd86bf747f54824febc614e02888fdda3881e29c6d5e76e97bacdcf4f82bbd`. The
shared MorphOS implementation passes its 17 existing vectors per CPU at
`artifacts/which-morphos-native-20260927-routes-v1/qualification.json`.

Two fresh 2,400-frame original/candidate pairs match exactly:

- `C:Which Execute ALL` emits `Workbench3.1:C/Execute\n` twice (46 bytes),
  returns 0, and leaves caller post-System IoErr 0:
  `artifacts/workbench31-guest-command-which-all-execute-candidate-20260927-v2/effect-comparison.json`.
- `C:Which C:Execute ALL` emits that line once (23 bytes), returns 0, and
  leaves caller post-System IoErr 0:
  `artifacts/workbench31-guest-command-which-all-explicit-c-execute-candidate-20260927-v2/effect-comparison.json`.

Together with the directory and miss pairs above, this is four bounded
Workbench comparisons, not complete route-order coverage. Caller post-System
IoErr does not prove child `Result2`. Current-directory/assign collisions,
other non-internal switch combinations, failure and break behavior, the
1,024-byte individual-name boundary, MorphOS alias semantics, installed
PURE/resident lifecycle, rights, packaging, and full differential admission
remain open.

## Workbench non-internal option matrix (2026-09-27)

The original and current resident candidate were each executed through the
real Workbench `ReadArgs` and filesystem in 16 fresh 2,400-frame captures:
eight switch combinations for filesystem-resolved `Execute`, then the same
eight for absent `CopperOSMissingWhich`. Every exact command, output byte
sequence, command return and caller post-System IoErr matched. The summary
receipt is
`artifacts/workbench31-guest-command-which-switch-matrix-20260927-v1/evidence-summary.json`;
it links each comparison receipt.

| Switches | Found `Execute` | Missing `CopperOSMissingWhich` |
| --- | --- | --- |
| none | 23 bytes; return 0; IoErr 0 | empty; return 5; IoErr 205 |
| `NORES` | 23 bytes; return 0; IoErr 0 | empty; return 5; IoErr 205 |
| `RES` | empty; return 5; IoErr 205 | empty; return 5; IoErr 205 |
| `ALL` | 46 bytes (same path twice); return 0; IoErr 0 | empty; return 5; IoErr 205 |
| `NORES RES` | empty; return 5; IoErr 0 | empty; return 5; IoErr 0 |
| `NORES ALL` | 46 bytes (same path twice); return 0; IoErr 0 | empty; return 5; IoErr 205 |
| `RES ALL` | empty; return 5; IoErr 205 | empty; return 5; IoErr 205 |
| `NORES RES ALL` | empty; return 5; IoErr 0 | empty; return 5; IoErr 0 |

These outcomes freeze the observed result/IoErr distinction for the two exact
names only. Caller post-System IoErr is not proof of child `Result2`; this does
not establish other name categories, inaccessible routes, output failures,
break handling, alias behavior or installed PURE/resident lifecycle.

## Workbench `ReadArgs` and refreshed guest evidence (2026-09-27)

The classic resident entry now reproduces DOS `ReadArgs` diagnostics for the
observed required-argument and unknown-option failures. `DOS.PrintFault`
renders the original message; the entry restores the parser's IoErr and returns
WARN. This handling is Workbench-only pending a separate MorphOS parser
comparison. The current Workbench qualification passes 22 supplied vectors
per CPU on resident 68000/020/040 HUNKs (66 total), with the 68000 HUNK at
4,932 bytes and SHA-256
`be283d619f183267e4b1b25f7d2ecb280815a373e4960550b66fc45a79c5d189`:
`artifacts/which-wb31-native-20260927-readargs-v1/qualification.json`. The
shared MorphOS code passes 17 supplied vectors per CPU at
`artifacts/which-morphos-native-20260927-readargs-v1/qualification.json`.

Two new real Workbench guest pairs match the original parser failures:

- `C:Which` prints `required argument missing\n`, returns 5, and leaves caller
  post-System IoErr 116:
  `artifacts/workbench31-guest-command-which-parse-missing-file-candidate-20260927-v2/effect-comparison.json`.
- `C:Which Execute MYSTERY` prints `wrong number of arguments\n`, returns 5,
  and leaves caller post-System IoErr 118:
  `artifacts/workbench31-guest-command-which-parse-unknown-option-candidate-20260927-v2/effect-comparison.json`.

The four route pairs and sixteen non-internal option-matrix pairs have also
been re-captured and compared against the original with this HUNK. All 22
bounded guest comparisons match command, output bytes, return and caller
post-System IoErr. Refreshed summaries:
`artifacts/workbench31-guest-command-which-route-cases-20260927-v2/evidence-summary.json`
and
`artifacts/workbench31-guest-command-which-switch-matrix-20260927-v2/evidence-summary.json`.
These checks do not establish child `Result2`, the other lookup categories,
complete route/assign ordering, full diagnostics/failure or break behavior,
MorphOS alias behavior, original PURE/resident lifecycle, licensing or package
admission.

## Workbench `?` parser-help observations (2026-09-27)

Two original/current-candidate guest pairs now cover `?` in the classic
`ReadArgs` template. With no FILE, `C:Which ?` emits exactly
`FILE/A,NORES/S,RES/S,ALL/S: required argument missing\n` (54 bytes),
returns 5 and leaves caller post-System IoErr 116. With FILE supplied,
`C:Which Execute ?` emits exactly
`FILE/A,NORES/S,RES/S,ALL/S: Workbench3.1:C/Execute\n` (51 bytes), returns
0 and leaves caller IoErr 0. The supplied-file case continues through the
normal lookup after the syntax text. Both exact comparisons match the current
4,932-byte Workbench HUNK (`be283d619f183267e4b1b25f7d2ecb280815a373e4960550b66fc45a79c5d189`);
see `artifacts/workbench31-guest-command-which-readargs-help-20260927-v1/evidence-summary.json`.

This covers two Workbench parser/help forms only. It does not establish the
complete classic template interaction matrix or MorphOS grammar/help behavior.

## Workbench internal `CD` guest option matrix (2026-09-27)

All eight classic switch combinations for internal-only `CD` now have fresh
original/current-candidate guest comparisons. Exact output, command return and
caller post-System IoErr agree:

| Switches | Output | Return | Caller IoErr |
| --- | --- | ---: | ---: |
| none | `INTERNAL CD\n` | 0 | 0 |
| `NORES` | empty | 5 | 205 |
| `RES` | `INTERNAL CD\n` | 0 | 0 |
| `ALL` | `INTERNAL CD\n` | 5 | 205 |
| `NORES RES` | empty | 5 | 0 |
| `NORES ALL` | empty | 5 | 205 |
| `RES ALL` | `INTERNAL CD\n` | 0 | 0 |
| `NORES RES ALL` | empty | 5 | 0 |

The aggregate current-HUNK summary
`artifacts/workbench31-guest-command-which-current-hunk-20260927-v1/evidence-summary.json`
now binds these eight cases together with the four route cases, sixteen
non-internal option cases, two parser-error cases and two help cases: 32
bounded Workbench comparisons total. Caller post-System IoErr remains distinct
from child `Result2`; this does not close other internal/resident collisions,
MorphOS aliases/help, lifecycle/purity or package gates.

## Workbench regular-file subdirectory lookup (2026-09-27)

The original Workbench command and current 4,932-byte candidate were run with
`C:Which S/Startup-Sequence` in separate disposable guests. Both emitted the
exact 32-byte line `Workbench3.1:S/Startup-Sequence\n`, returned 0, and left
caller post-System IoErr at 0. The pair is recorded at
`artifacts/workbench31-guest-command-which-startup-file-candidate-20260927-v1/effect-comparison.json`;
the original observation is in
`artifacts/workbench31-guest-command-which-startup-file-original-20260927-v1/probe-analysis.json`.
The candidate HUNK SHA-256 is
`be283d619f183267e4b1b25f7d2ecb280815a373e4960550b66fc45a79c5d189`.

The diagnostic derivative modifies `S/Startup-Sequence` only to insert the
probe invocation, so this establishes regular-file lookup and emitted path
format, not content parity or original-file metadata. The separate pair is not
included in the 32-case aggregate above. Other lookup categories and route
collisions, child `Result2`, MorphOS aliases/help, original PURE/resident
lifecycle, licensing, package admission and full command parity remain open.

## Workbench directory filtering follow-up (2026-09-27)

The Workbench-only lookup fix now uses DOS `Examine` to reject directories
while preserving assign roots. Eight fresh guest pairs with the 5,316-byte
candidate match the original exactly:

| Invocation | Output | Return | Caller IoErr | Evidence |
| --- | --- | ---: | ---: | --- |
| `C:Which S` | empty | 5 | 205 | `artifacts/workbench31-guest-command-which-subdir-directory-filter-candidate-20260927-v1/effect-comparison.json` |
| `C:Which S/` | empty | 5 | 0 | `artifacts/which-explicit-subdir-directory-filter-candidate-20260927-v1/effect-comparison.json` |
| `C:Which S/Startup-Sequence` | `Workbench3.1:S/Startup-Sequence\n` | 0 | 0 | `artifacts/which-startup-file-directory-filter-candidate-20260927-v1/effect-comparison.json` |
| `C:Which C:` | `Workbench3.1:C\n` | 0 | 0 | `artifacts/which-c-directory-filter-candidate-20260927-v1/effect-comparison.json` |
| `C:Which SYS:` | `Workbench3.1:\n` | 0 | 0 | `artifacts/workbench31-guest-command-which-sys-assign-candidate-20260927-v1/effect-comparison.json` |
| `C:Which SYS:S` | empty | 5 | 0 | `artifacts/workbench31-guest-command-which-sys-subdir-candidate-20260927-v1/effect-comparison.json` |
| `C:Which SYS:S/Startup-Sequence` | `Workbench3.1:S/Startup-Sequence\n` | 0 | 0 | `artifacts/workbench31-guest-command-which-sys-startup-file-candidate-20260927-v1/effect-comparison.json` |
| `C:Which SYS:S/..` | empty | 5 | 205 | `artifacts/workbench31-guest-command-which-sys-parent-candidate-20260927-v1/effect-comparison.json` |

The HUNK SHA-256 is
`3c7a61a5d84b313dc91874360ba3caa26e7f284c189c8b1c985753a30af21f57`.
Its resident native qualification passes 31 vectors per CPU on 68000/020/040
at `artifacts/which-wb31-native-20260927-sys-parent-v10/qualification.json`.
The previous directory-positive candidate receipts remain retained as
negative controls. These eight cases are independent of the 32-case aggregate
and do not close other route classes, error/Break behavior, MorphOS behavior,
PURE admission, lifecycle or shipping.

The later MorphOS alias-provider edit changed the shared source build identity.
The rebuilt Workbench entry passes the same 31 supplied vectors per CPU, with
the three-CPU receipt at
`artifacts/which-wb31-native-20260927-findvar-regression-v1/qualification.json`
and a 6,308-byte 68000 HUNK
(`cb7a3a3aa26e1d12381da5280f7281265ad5b0e5b52648f2f8d94c8918ce7294`). The
current identity matches the original for one refreshed `C:Which SYS:S/..`
guest case (empty output, return 5, caller IoErr 205), recorded at
`artifacts/workbench31-guest-command-which-crossprofile-current-candidate-20260927-v1/effect-comparison.json`.
Other guest comparisons above remain attached to their earlier candidate
HUNKs; the 32-case matrix and full Workbench guest parity are not refreshed.

## MorphOS extended-switch candidate matrix (2026-09-27)

The MorphOS candidate uses public DOS `FindVar(..., LV_ALIAS)` and now passes
85 supplied vectors per CPU on resident 68000/020/040 at
`artifacts/which-morphos-native-20260927-switch-matrix-v3/qualification.json`.
The fixture covers all 32 combinations of `NOALIAS`, `ALIAS`, `NORES`, `RES`,
and `ALL`, each with candidate found/missing inputs, plus an alias-only miss
with resident/path alternatives present to check no fallthrough. It also fixes
the fixture model so `RES` suppresses alias lookup unless `ALIAS` explicitly
requests alias-only behavior.

These are candidate vector tests, not captures from the shipped MorphOS
command. The candidate still assumes `ALIAS <name>` formatting and alias-first
default order; conflicting-switch results, exact grammar, original output,
result level, `IoErr`, PURE/resident lifecycle and package behavior remain
open until the MorphOS executable is run or decoded with enough evidence.
