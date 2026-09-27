# Version contract

Profiles: `wb31` and `morphos320`. Goal steps: CC01 and CC17. Recorded:
2026-09-12.

Status: partial implementation and contract evidence; no complete profile or
package admission. The Workbench named default lookup now implements the
ordered Resident, command-segment, trailing-colon DOS device-handler, Exec
LibList, Exec DeviceList, complete-name file, `LIBS:` and `DEVS:` providers.
Its current three-CPU native qualification passes 93 supplied vectors per
CPU. Six bounded default-provider original/replacement guest pairs now match
output, return and caller post-System IoErr: `LIBS:version.library`, explicit
`DEVS:clipboard.device`, basename `clipboard.device`, a bare-name miss, and
explicit `LIBS:` and `DEVS:` misses.
The first Resident provider and FULL formatting also pass ten exact guest
pairs. Workbench direct FILE, no-name system, command-segment and DOS
device-handler checkpoints are recorded below; broader command behavior and all
resident/lifetime/packaging gates remain open. Earlier failed and successful
controls are retained.

The MorphOS 3.20 source member is `c/version/version.c`, 38,220 bytes,
with SHA-256
`818f64fae8abf914633b4bc807d6b105eb0ccf30e1e48ac1527745d486c22490` in the
extracted source set. Its version include records `Version 50.30
(07.06.2023)`. The MorphOS source calls DOS and utility library version 37
and uses this `ReadArgs` template:

```text
NAME/M,MD5SUM/S,VERSION/N,REVISION/N,FILE/S,FULL/S,RES/S
```

The source-observed behavior has two top-level paths. With no `NAME`, it
builds and prints the system version, then compares requested values when
present. With one or more names, it scans each name for a version string,
supports the historical `file version [revision]` positional workaround,
prints each result, and compares a single result against requested version
and revision. `FILE` selects file scanning, `FULL` retains extra version text,
`RES` selects resident lookup, and `MD5SUM` requests the source's MD5 output
and comparison path. Missing versions and comparison mismatches have distinct
`RETURN_FAIL`/`RETURN_WARN` paths in the source; parser and allocation errors
route through `PrintFault(IoErr(), NULL)`. The implementation also contains
optional `version.library` and ARexx/Ambient integrations. The bounded ARexx
query and direct Ambient file-provider paths now have supplied-vector
receipts. The source's `IsFileSystem`/`LoadSeg` resident fallbacks for Ambient
and named FILE requests are covered by bounded supplied vectors; MorphOS object
lookup now covers loaded command segments and internal/disabled shellcmd
fallback, while the full automatic resident/library/device/filesystem search
order remains open. Default and `RES` named lookups now use DOS `FilePart` for
the Exec Resident-table lookup, then check a case-insensitive loaded Exec
`LibList` entry. The source-ordered `MOSSYS:LIBS`/`LIBS:` filesystem
candidates, `DeviceList`, `MOSSYS:DEVS`/`DEVS:` candidates, and terminal input
file or volume lookup remain incomplete. Original-guest and shipping gates
also remain open.

The Workbench 3.1 `C/Version` media member is a 4,764-byte 40.1 HUNK with
SHA-256
`dc2f55cd48b37bd1efecdbf07d38757463129dd9076ea6bf1b5d967f2189f224`.
Its captured syntax candidate is:

```text
NAME,VERSION/N,REVISION/N,FILE/S,FULL/S,UNIT/N,INTERNAL/S,RES/S
```

This classic candidate is not treated as a frozen grammar until a disposable
Workbench 3.1 guest capture confirms positional handling, `UNIT` and
`INTERNAL`, help/EOF, diagnostics, return levels, and resident lookup.

Still open as command qualification and shipping gates: packed-binary/source
correspondence; exact original output and version-string behavior across all
paths; MorphOS system FULL, fallback resolution and optional providers;
broader Workbench FILE replacement-guest parity and any remaining direct-file
edge cases, plus MD5/UNIT/INTERNAL behavior; complete resident-table contents;
Workbench/MorphOS guest parity; original PURE/resident classification,
lifecycle and concurrency; source-reuse rights; and package admission. The
MorphOS direct-FILE MD5 candidate has bounded resident evidence below, while
Workbench has no MD5 implementation candidate yet. Bodies must use DOS
`ReadArgs` and public Exec/DOS/version APIs, and must not inspect host files or
host process state.

The Workbench candidate now has a separate DOS 36 resident entry and keeps the
classic eight-slot template above. Its bounded fixture covers system and RES
lookup, comparison warning, missing resident, parser and startup boundaries,
resident `FULL` `$VER:` tail output, and explicitly fail-closed
FILE/UNIT/INTERNAL provider paths. The refreshed receipt is
`artifacts/version-wb31-native-20260920-full-v2/qualification.json`:

| CPU | HUNK bytes | SHA-256 | vectors | reachable methods |
| --- | ---: | --- | ---: | ---: |
| 68000 | 4,568 | `99440d6a818d222461c2b47397af942c17bbeccb63d737824bebe17d431d8108` | 15 | 17 |
| 68020 | 4,644 | `b3cdc39ae3d8644562ff1f04edc6ff265e97f7b15b2e2e686d2f5ac1a378942a` | 15 | 17 |
| 68040 | 4,564 | `2c6ad2ce7ea3c7a5551f42db9a078f2c51875a7c5ad1fdd9c52bf95c90f1ab0f` | 15 | 17 |

This is a syntax-candidate native receipt only; exact Workbench output and
positional behavior, packed correspondence, original guest parity,
PURE/resident lifecycle, licensing, package admission, and differential
evidence remain open.

## Workbench direct FILE checkpoint (2026-09-27)

The Workbench candidate now implements the captured direct `FILE` path while
preserving the no-name system report when `FILE` is present. It scans the
opened file for `$VER:` across DOS `Read` boundaries, prints normal and `FULL`
output, applies version/revision minima after output, and, when the file begins
with an Amiga HUNK header but has no tag, closes it and uses public DOS
`LoadSeg` to locate a Resident in the returned segment list. File buffers and
loaded segments are released on success and failure. `UNIT` and `INTERNAL`
remain ignored as observed for this command; later default-lookup providers
are still not implemented. The candidate opens `utility.library` v37 before
`ReadArgs`, following the original startup order, and the refreshed fixture
checks FILE provider restriction, `RES` interaction, HUNK Resident fallback,
linked segments, scan-boundary behavior and open/read/allocation cleanup.

Fresh original guest observations recorded:

- `C:Version C:Avail FILE` prints `avail 40.1`, returns 0 and leaves caller
  IoErr 0 in
  `artifacts/workbench31-guest-command-version-file-original-20260927-v1/probe-analysis.json`.
- `C:Version C:Avail FILE FULL` adds the captured date `02/09/93` in
  `artifacts/workbench31-guest-command-version-file-full-original-20260927-v1/probe-analysis.json`.
- `C:Version C:Avail FILE VERSION 999` prints the version then returns 5 in
  `artifacts/workbench31-guest-command-version-file-warning-original-20260927-v1/probe-analysis.json`.
- `C:Version FILE` retains the system version report. `FILE` on a named missing
  file does not fall through to a Resident or later provider; a no-tag file
  reports the source's `Could not find version information` diagnostic. A
  HUNK-backed `DEVS:clipboard.device FILE` returns `clipboard.device 38.8`.
  Their original-only captures are retained under
  `artifacts/workbench31-guest-command-version-system-file-switch-original-20260927-v1/`,
  `artifacts/workbench31-guest-command-version-resident-file-switch-original-20260927-v1/`,
  `artifacts/workbench31-guest-command-version-file-no-tag2-original-20260927-v1/`,
  and `artifacts/workbench31-guest-command-version-file-hunk-original-20260927-v1/`.

Four 2,400-frame original/replacement pairs now cover direct normal output,
`FULL`, a failed version minimum, and a HUNK-backed Resident:

- Normal `C:Version C:Avail FILE` matches the 11 bytes `avail 40.1\n`, return
  0, and caller post-System IoErr 0 in
  `artifacts/workbench31-guest-command-version-file-candidate-20260927-v2/comparison.json`.
- `C:Version C:Avail FILE FULL` matches the 22 bytes
  `avail 40.1 (02/09/93)\n`, return 0, and caller IoErr 0 in
  `artifacts/workbench31-guest-command-version-file-full-candidate-20260927-v1/comparison.json`.
- `C:Version C:Avail FILE VERSION 999` matches the 11-byte version line, return
  5 after printing, and caller IoErr 0 in
  `artifacts/workbench31-guest-command-version-file-warning-candidate-20260927-v1/comparison.json`.
- `C:Version DEVS:clipboard.device FILE` matches the 22 bytes
  `clipboard.device 38.8\n`, return 0, and caller IoErr 0 in
  `artifacts/workbench31-guest-command-version-file-hunk-candidate-20260927-v1/comparison.json`.

The derivative receipts preserve original `C/Version` metadata, including
protection 0, and bind the candidate HUNK to SHA-256
`f7f33772232a836a1448f268f8e872e0f49ae7f9fe087852dd76c1ce186de88b`. These
four exact observations do not establish general FILE behavior, parity for the
other captured original cases, or PURE/resident admission.

The three-CPU resident qualification is
`artifacts/version-wb31-native-20260927-file-v5/qualification.json`:

| CPU | HUNK bytes | SHA-256 | vectors | reachable methods |
| --- | ---: | --- | ---: | ---: |
| 68000 | 10,044 | `f7f33772232a836a1448f268f8e872e0f49ae7f9fe087852dd76c1ce186de88b` | 72 | 29 |
| 68020 | 10,184 | `e610855974fa8f68a4fee82d0a889942dc2948a5698696bcbe21fb5fd30b114e` | 72 | 29 |
| 68040 | 9,924 | `25f032d0008f964952111e6b6f771d1e3ce45c0e70721235adfd1ff095c51823` | 72 | 29 |

The qualification confirms source and compiled-input hashes remained stable,
zero shared-image writes and balanced fixture resources. It is supplied
DOS/Exec/Utility evidence plus four exact FILE guest invocations, not proof of the
original command's PURE classification. Later ordered providers, complete system and
secondary-error ordering, lifecycle/concurrency, packed correspondence,
rights and package admission remain open.

## Workbench no-name system path checkpoint (2026-09-27)

The original no-name path is now implemented from the Workbench HUNK behavior:
the candidate reads Kickstart version/revision from ExecBase, opens
`version.library` through Exec, uses its library version/revision and IdString
for the Workbench line and `FULL` date, then applies VERSION/REVISION minima
to the Workbench values. DOS `ReadArgs` remains the option parser, and the
command's original DOS v37 startup requirement remains in force. This fixes the
captured `C:Version FILE` mismatch where the earlier candidate reported
`dos.library 40.3` instead of the Kickstart/Workbench system line.

`artifacts/version-wb31-native-20260927-system-v4/qualification.json` passes
72 supplied invocations per CPU on 68000/020/040 with no leaked fixture
resources or shared-image writes:

| CPU | HUNK bytes | SHA-256 | vectors | reachable methods |
| --- | ---: | --- | ---: | ---: |
| 68000 | 10,340 | `59005e5e65b0c031bfadd1382b1f585c4af4f1f45c2bc6f71d6fcdb29b7449d2` | 72 | 29 |
| 68020 | 10,480 | `6181662eec396bb9362e03a3add3630cb2ff7dc2e3edbf1f71e033e7ca658bf5` | 72 | 29 |
| 68040 | 10,220 | `23be26100e50eb2b622a349baf9189582de3bb6ca93cae1d8c0c9999b893a7c5` | 72 | 29 |

Four 2,400-frame original/replacement guest cases match exact output, return
and caller post-System IoErr:

- `C:Version FILE` prints `Kickstart 40.63, Workbench 40.42\n`, returns 0,
  and leaves caller IoErr 0 in
  `artifacts/workbench31-guest-command-version-system-file-candidate-20260927-v2/comparison.json`.
- `C:Version FULL` prints `Kickstart 40.63, Workbench 40.42 (02/18/94)\n`,
  returns 0, and leaves caller IoErr 0 in
  `artifacts/workbench31-guest-command-version-system-full-candidate-20260927-v2/comparison.json`.
- `C:Version VERSION 99` prints the short system line and returns 5 in
  `artifacts/workbench31-guest-command-version-system-min99-candidate-20260927-v2/comparison.json`.
- `C:Version VERSION 39` prints the short system line and returns 0 in
  `artifacts/workbench31-guest-command-version-system-min39-candidate-20260927-v2/comparison.json`.
- Revision-only `C:Version REVISION 42` prints the same line and returns 0
  at the equal boundary in
  `artifacts/workbench31-guest-command-version-system-rev42-candidate-20260927-v1/comparison.json`.
- Revision-only `C:Version REVISION 99` prints the same line and returns 5
  above the Workbench revision in
  `artifacts/workbench31-guest-command-version-system-rev99-candidate-20260927-v1/comparison.json`.
- `C:Version VERSION 39 REVISION 99` returns 0 despite the higher requested
  revision in
  `artifacts/workbench31-guest-command-version-system-v39-r99-candidate-20260927-v1/comparison.json`.
- `C:Version VERSION 40 REVISION 42` returns 0 at the equal boundary in
  `artifacts/workbench31-guest-command-version-system-v40-r42-candidate-20260927-v1/comparison.json`.
- `C:Version VERSION 40 REVISION 43` returns 5 above Workbench revision 42 in
  `artifacts/workbench31-guest-command-version-system-v40-r43-candidate-20260927-v1/comparison.json`.

The current v4 HUNK also matches six exact FILE guest cases, extending the
earlier four receipts made with the FILE-only candidate HUNK:

- `C:Version C:Avail FILE`:
  `artifacts/workbench31-guest-command-version-file-candidate-20260927-v3/comparison.json`.
- `C:Version C:Avail FILE FULL`:
  `artifacts/workbench31-guest-command-version-file-full-candidate-20260927-v2/comparison.json`.
- `C:Version C:Avail FILE VERSION 999`:
  `artifacts/workbench31-guest-command-version-file-warning-candidate-20260927-v2/comparison.json`.
- `C:Version DEVS:clipboard.device FILE`:
  `artifacts/workbench31-guest-command-version-file-hunk-candidate-20260927-v2/comparison.json`.
- `C:Version C:MissingVersionFile FILE`:
  `artifacts/workbench31-guest-command-version-missing-file-candidate-20260927-v2/comparison.json`.
- `C:Version DEVS:system-configuration FILE`:
  `artifacts/workbench31-guest-command-version-no-tag-file-candidate-20260927-v2/comparison.json`.

This closes only those invocations. Other signed/parser combinations, absent
`version.library`, secondary-error/result ordering, further FULL/FILE
combinations, later ordered providers, original PURE admission, lifecycle,
rights and package admission remain open. The original `C/Version` protection
word is zero, so this resident HUNK evidence does not certify PURE safety.

## Workbench command-segment provider checkpoint (2026-09-27)

The default named lookup now follows the original Resident provider with DOS
`FindSegment` under a balanced Exec `Forbid`/`Permit`, trying system 0 then
system 1. For an ordinary command segment, it walks the segment list for the
`$VER:` string and parses/prints the version; `FULL` keeps its date and extra
text. Internal and disabled command segments use the Workbench-specific
`shell` Resident fallback. This preserves the original single-name ReadArgs
syntax and uses the public DOS, Exec and Utility calls.

The refreshed resident qualification passes 77 supplied invocations per CPU
on 68000/020/040 with no shared-image writes or fixture leaks:
`artifacts/version-wb31-native-20260927-segments-v4/qualification.json`.
HUNK identities are 11,596 bytes / SHA-256
`2deebeeec6bc803708d8ed3dd9d4c1af150288f5a879590689321a3dd607cfd2` on
68000, 11,764 bytes /
`62ebaefb92534489302e0c5222223249a009296a527d063ae94dddb552c3fe10` on
68020, and 11,468 bytes /
`b98f56f489187c135071fc24ff61f1400f9eacaf352e28c2b9d797582bb1f45c` on
68040. Supplied vectors cover both FindSegment system lists, normal and FULL
tags, and internal/disabled shell fallback. There is not yet an original-guest
comparison for these command-segment cases. The separate trailing-colon
DOS-list checkpoint below covers a bounded device-handler path. LibList,
DeviceList, direct-file and LIBS/DEVS lookup order, further parser and error
cases, original PURE admission, lifecycle, rights, and package admission
remain open.

## Workbench trailing-colon DOS device-handler checkpoint (2026-09-27)

After the first Resident and command-segment lookups miss, the Workbench
candidate now checks names ending in `:` through public DOS
`LockDosList(Devices|Read)`, `FindDosEntry(Devices)` and `UnLockDosList`. It
temporarily removes the final colon for the lookup, restores it, checks the
returned DeviceNode's startup pointer, then scans its segment list for a
Resident and passes the Resident through the existing version and `FULL`
formatter. `FILE` remains on its direct-file path. The fixture covers a hit,
missing entry, missing startup/segment/Resident fallthrough, `RES`, `FULL`,
balanced DOS-list calls and name restoration.

The refreshed resident qualification
`artifacts/version-wb31-native-20260927-doslist-v6/qualification.json` passes
85 supplied invocations per CPU on 68000/020/040 with no shared-image writes
or leaked fixture resources:

| CPU | HUNK bytes | SHA-256 | vectors |
| --- | ---: | --- | ---: |
| 68000 | 12,004 | `c5ee16f55f25508eca21819fd4b7a000d0954292f15cd1f4c8ae2dc6f9667dee` | 85 |
| 68020 | 12,180 | `544f139fc35e3a1d6aaefd456739574c2cd3e8778c21bf29e4257eb4c7b43e5c` | 85 |
| 68040 | 11,876 | `29e7f9ff1a167ee7a00085a24227c6077189864138182face229cb3bed260a8e` | 85 |

Two separate 2,400-frame original/replacement guest comparisons match exactly:

- `C:Version DF0:` emits `filesystem 40.1\n` (16 bytes), returns 0 and leaves
  caller post-System IoErr 0:
  `artifacts/workbench31-guest-command-version-df0-candidate-20260927-v1/effect-comparison.json`.
- `C:Version DF0: RES` emits the same 16 bytes, returns 0 and leaves caller
  post-System IoErr 0:
  `artifacts/workbench31-guest-command-version-df0-res-candidate-20260927-v1/effect-comparison.json`.

These pairs establish only the two captured device-handler requests. They do
not exercise command-segment lookup or establish complete Version behavior.
Other named providers and error ordering, broader guest coverage, original
PURE/resident classification, lifecycle, rights and package admission remain
open.

## Workbench ordered named default providers (2026-09-27)

The Workbench candidate now follows the observed default named-lookup sequence:
Resident, command segment, trailing-colon DOS device handler, Exec `LibList`,
Exec `DeviceList`, direct complete-name file, then `LIBS:` and `DEVS:` basename
files. The list scans use public Exec calls and copy matched names and version
strings while the list is protected; filesystem candidates use DOS file calls.
This preserves the earlier Resident and `FULL` formatting behavior and does
not change the separate `FILE`-only provider path.

The current resident qualification
`artifacts/version-wb31-native-76d9914aa0f840b6a26a5fa43e7bc723/qualification.json`
passes 93 supplied invocations per CPU on 68000/020/040 (279 total), with no shared-image
writes or fixture leaks. Its 68000 HUNK is 14,568 bytes with SHA-256
`6e3092af7a356097bd3929c8ba0a93e65e1260d288372a42cb2fa4096429c527`.

Six bounded original/candidate guest cases exercise successful provider order
and final missing-provider diagnostics. They all compare equal on output bytes,
command return and caller post-System IoErr:

- `C:Version LIBS:version.library` emits `version.library 40.42\n` (22 bytes),
  returns 0 and leaves IoErr 0:
  `artifacts/workbench31-guest-command-version-liblist-regress-candidate-20260927-v1/effect-comparison.json`.
- `C:Version DEVS:clipboard.device` emits `clipboard.device 38.8\n` (22
  bytes), returns 0 and leaves IoErr 0:
  `artifacts/workbench31-guest-command-version-devs-explicit-regress-candidate-20260927-v1/effect-comparison.json`.
- `C:Version clipboard.device` emits the same 22 bytes, returns 0 and leaves
  IoErr 0. The 3,600-frame comparison includes the HUNK `LoadSeg` fallback:
  `artifacts/workbench31-guest-command-version-clipboard-basename-regress-candidate-20260927-v1/effect-comparison.json`.
- `C:Version missing-device-copper-test.device` emits
  `object not found\nC:Version failed returncode 20\n`, returns 20 and leaves
  IoErr 205:
  `artifacts/workbench31-guest-command-version-bare-miss-candidate-fix-20260927-v1/effect-comparison.json`.
- `C:Version LIBS:missing.library` and `C:Version DEVS:missing.device` each
  emit the same 48 bytes, return 20 and leave IoErr 205:
  `artifacts/workbench31-guest-command-version-liblist-miss-candidate-20260927-v1/effect-comparison.json`;
  `artifacts/workbench31-guest-command-version-devs-miss-candidate-20260927-v1/effect-comparison.json`.

The final miss now propagates the last DOS provider `IoErr` instead of
replacing it with a generic not-implemented error. This closes only the
three observed miss forms and three success cases, not full lookup-order or
error-parity closure. Other names and provider failures, broader parser/system
behavior, PURE/resident lifecycle, rights, and package admission remain open.

## MorphOS resident FULL checkpoint

The MorphOS resident path now locates the numeric version tail in the guest
`rt_IdString` (including the usual `$VER:` prefix) and emits it after the
resident name when `FULL` is requested, matching the source's resident
construction while retaining the short `name version.revision` form for
ordinary lookups. The supplied fixture now checks
`$VER: dos.library 50.6 (fixture)` producing `dos.library 50.6 (fixture)`.
The refreshed receipt is
`artifacts/version-morphos-native-20260920-full-v3/qualification.json`:

| CPU | HUNK bytes | SHA-256 | vectors | reachable methods |
| --- | ---: | --- | ---: | ---: |
| 68000 | 4,948 | `683042a208ec6eeb1d12d3afeeda687ef6cf19ee9f418331f839889310d8258e` | 14 | 17 |
| 68020 | 5,024 | `0fabfc2a60de1e94de6deb05a3a045fa359215dddf4af7fba83fb41f3981188b` | 14 | 17 |
| 68040 | 4,944 | `09bb30a1ac13b3a2f059b55c48d4258b77265d72417308065931ff8fd0ede9a3` | 14 | 17 |

This remains a supplied-vector checkpoint: file/MD5 scanning, version-library
and ARexx providers, system FULL parsing, original guest comparison,
PURE/resident lifecycle and package admission remain open.

## MorphOS comparison checkpoint

The MorphOS comparator now follows `cmpargsparsed()` from the captured source:
it returns `RETURN_WARN` only when the requested version or applicable
revision is greater than the discovered value. Lower requested values remain
`RETURN_OK`, and both system and single-RES paths print before applying the
comparison result. The expanded supplied fixture covers lower major and
revision requests, higher major and revision requests, revision-only
comparison, and the same print/result behavior for a single resident lookup.
The separate Workbench candidate remains unchanged because its classic
comparison behavior has not been independently captured.

The refreshed resident receipt is
`artifacts/version-morphos-native-20260924-compare-v1/qualification.json`:

| CPU | HUNK bytes | SHA-256 | vectors | reachable methods |
| --- | ---: | --- | ---: | ---: |
| 68000 | 5,216 | `cdd232cef00a808a12475217cce33c1d0908032a5f63ed4d7ce47fc81dd727a5` | 20 | 17 |
| 68020 | 5,300 | `89e3d2ab1365518b87f7f4737dd30006e330739475b29011d5f07c6b82a6a2b4` | 20 | 17 |
| 68040 | 5,220 | `777199801f2247810e0fe69efcd788ff350ee2174e76ed50360b0faa37223f66` | 20 | 17 |

These are fixture vectors only; exact MorphOS guest output, packed
correspondence, FILE/MD5 scanning, optional providers, PURE/resident lifecycle,
licensing and package admission remain open.

## MorphOS direct FILE scan checkpoint

The resident MorphOS candidate now implements the source-backed direct `FILE`
scan for `$VER:` tags using DOS `OpenRaw`, `Read`, `Seek64`, `StrToLong`,
`StrToDate`, and `DateToStr`, with public Exec/DOS allocation and cleanup.
It retains the source's five-byte overlap between reads and the MorphOS v0 ELF
end-seek optimization. `FULL` output parses and preserves the date and extra
text; multiple names, comparison ordering, no-tag reporting, and cleanup/error
boundaries are covered by supplied vectors. The refreshed receipt is
`artifacts/version-morphos-native-20260924-file-v4/qualification.json`:

| CPU | HUNK bytes | SHA-256 | vectors | reachable methods |
| --- | ---: | --- | ---: | ---: |
| 68000 | 11,028 | `9d551b057e331a594975e8fa7aca05aecdcf944cdbba136c0c362355108c03d5` | 32 | 32 |
| 68020 | 11,240 | `1bb4dd027cc917662ad7fa360c6fc32247bb4a87865636a9420ab92ffffe6dee` | 32 | 32 |
| 68040 | 11,016 | `83508cdbb9c599d3f6e5e15a2a1180cc055e949f47294c25b799fdbba5179bdb` | 32 | 32 |

This closes only a bounded direct `$VER:` FILE scan candidate. FILE
fallback/LoadSeg resolution, resident/object lookup gaps, optional
version.library and ARexx providers, exact guest output and original guest
comparison, packed correspondence, PURE/resident lifecycle, licensing and
package admission remain open. The Workbench candidate still fails closed for
FILE/UNIT/INTERNAL and has not gained equivalent file scanning.

## MorphOS MD5SUM checkpoint

The MorphOS resident candidate now computes MD5 incrementally while scanning a
`FILE`, hashes the complete file even when the version tag appears early,
disables the v0 end-seek optimization when a digest is requested, and prints
the 32-character uppercase digest before the parsed version. System and
resident paths print the source's `<no md5sum available>` placeholder; object
resolution without `FILE` remains fail-closed. The independent fixture compares
short and multi-block digests, including a `$VER:` tag split across reads,
against .NET's MD5 implementation and covers
`FULL` date output, unavailable-digest output, post-tag read failure, context
allocation failure, and balanced cleanup. Receipt
`artifacts/version-morphos-native-20260924-md5-v4/qualification.json`:

| CPU | HUNK bytes | SHA-256 | vectors | reachable methods |
| --- | ---: | --- | ---: | ---: |
| 68000 | 15,764 | `61cdac05ef3fe60d02462a5b788ddc3ddc4b6bfaeb655404d6a8d13b948b2bbe` | 40 | 43 |
| 68020 | 15,920 | `58e678445fa5312323eadbf4372273c5e1a118ef9e446d2da259ca6089d5427e` | 40 | 43 |
| 68040 | 15,624 | `64c282215a68676f74d9efcf386ee07a5c341b142bb7dd07e27e43fc97986403` | 40 | 43 |

The 68000 compatibility report is compatible with one root and 43 reachable
methods, with no managed allocation sites, runtime helpers, external native
targets, exception regions, or fatal machine-fault sites. Workbench's separate
DOS 36 resident candidate remains unchanged and passes its 15-vector-per-CPU
regression at `artifacts/version-wb31-native-20260924-md5-regression-v1/`.
These are supplied-vector results only; they do not establish original guest
parity, packed correspondence, PURE/resident lifecycle, licensing, or package
admission.

## MorphOS system-version candidate checkpoint

The MorphOS no-name system path now prints the source-shaped component
sequence from the MorphOS resident, ExecBase version fields, and an opened
`version.library` base. `FULL` appends the version-library ID tail, and
comparison uses the Workbench library version/revision. If `version.library`
cannot be opened, the candidate preserves the source's already-built Kickstart
fields for the final print. The expanded supplied fixture covers component
separators, the full system line, the fallback and comparison boundaries.
Receipt `artifacts/version-morphos-native-20260924-system-v5/qualification.json`:

| CPU | HUNK bytes | SHA-256 | vectors | reachable methods |
| --- | ---: | --- | ---: | ---: |
| 68000 | 16,728 | `90b2045743948e8cb40a6e533a5ffef44e3b8f152e738af4a95db63a9fd64a02` | 42 | 45 |
| 68020 | 16,880 | `52b27272737fd5a6db286309d22eed58178c0a71126847341315662df630a95e` | 42 | 45 |
| 68040 | 16,576 | `50fe9031791625426de6554292bb763e3e59a8b39def93414ae154d7c3ab49b4` | 42 | 45 |

This is still a fixture-only candidate. The later follow-up below closes the
`LibList` lookup and Ambient ARexx request paths under bounded supplied-vector
coverage. Ambient executable fallback, original guest comparison, packed
correspondence, PURE/resident lifecycle, rights and package admission remain
open.

## MorphOS system-version candidate follow-up (2026-09-24)

The resident system path now follows the captured source's lookup sequence:
open `version.library`, enter `Forbid()`, find its case-insensitive node in
Exec's `LibList`, read the node's version, revision and `IdString`, then balance
`Permit()` and close the opened library. If the MorphOS resident is absent, the
candidate continues with the source-shaped Kickstart and Workbench components.
If `version.library` is unavailable or absent from `LibList`, it preserves the
already-emitted prefix and returns failure. For Workbench `FULL`, it copies the
library name and selected dotted-version tail into invocation-owned memory and
uses the existing file-version/date parser; a parsed version that disagrees with
the library node falls back to the numeric node version and revision.

The refreshed qualification is
`artifacts/version-morphos-native-20260924-system-v6/qualification.json`:

| CPU | HUNK bytes | SHA-256 | vectors | reachable methods |
| --- | ---: | --- | ---: | ---: |
| 68000 | 18,404 | `31830d81ce83a8da2c84a734da7830a8a2f6a55fe7c100c7efa4e4a4588a841e` | 49 | 48 |
| 68020 | 18,640 | `c9a5d332a7784b73b59b56b67a2165822a8b10c5b051b666adc68b23ffd38d0c` | 49 | 48 |
| 68040 | 18,252 | `7ec2ffcf85188392244a996de156b98d781e8a9135774c09b0fa4dcca4f41eab` | 49 | 48 |

The source-faithful lookup and parsing vectors pass, as do the static resident
compatibility checks on all three CPUs. This is still fixture evidence, not
original guest parity or final command completion. File-based Ambient provider
behavior, FILE fallback/LoadSeg resolution, guest comparison, packed/source
correspondence, PURE/resident lifecycle, rights, release packaging and
installation remain open. The separate Workbench candidate and its fail-closed
FILE/UNIT/INTERNAL paths are unchanged.
## MorphOS RES IdString and extended-revision follow-up (2026-09-24)

The captured source's `makeresidentver()` path first builds the normal result
from the resident's `rt_Name` plus the numeric tail found in `rt_IdString`.
The candidate now copies that string into invocation-owned memory and parses it
through the existing DOS-backed version/date parser. This makes the parsed
ID-string version authoritative over `rt_Version`, keeps versionless resident
output, and separates unparenthesized trailing text from `FULL` extras. The
fixture also covers malformed parenthetical dates and extra-text line breaks.

When that source parse/allocation path fails, `makeresidentver()` falls back to
`rt_Version` and revision `-1`, except when MorphOS `RTF_EXTENDED` is set; then
it uses `rt_Revision`. The [official MorphOS V50 `exec/resident.h`](https://morphos-team.net/sdk/includes/exec/resident.html)
documents the flag and field. ABI and source evidence are recorded in
`reference-captures/version-morphos-resident-sdk-abi-20260924.json`.

The three-CPU receipt is
`artifacts/version-morphos-native-20260924-resident-v1/qualification.json`:

| CPU | HUNK bytes | SHA-256 | vectors | reachable methods |
| --- | ---: | --- | ---: | ---: |
| 68000 | 20,656 | `c139cf623eb3ff0d971b133e2c49cb25a2be634c9a11754acba20a07996c9164` | 53 | 47 |
| 68020 | 20,936 | `67dc8ded1ba3a02ab6f44837954bd3314b7c807a608cc29095022ddac59bc1a1` | 53 | 47 |
| 68040 | 20,520 | `c525830ca2f9ee9e720d3b84b34c3ebfc8f941912bda6a3d530416d5ff1d3cda` | 53 | 47 |

The separate Workbench DOS 36 candidate was rebuilt and still passes fifteen
vectors per CPU at
`artifacts/version-wb31-native-20260924-resident-regression-v1/qualification.json`.
These receipts prove the supplied HUNK boundary and resource ownership only.
Ambient file providers, FILE fallback/LoadSeg resolution, original guest
parity, packed/source correspondence, PURE/resident lifecycle, rights and
package admission remain open.

## MorphOS Ambient ARexx follow-up (2026-09-24)

The system path now follows the source-backed `AMBIENT` port query: it sends
`VERSION` with `RXCOMM | RXFF_RESULT`, waits for the reply, copies and parses
the `major.revision` result with DOS `StrToLong`, sets the local `Ambient`
variable, and prints the Ambient component. A successful query preserves the
source result level and suppresses Workbench output. Reply argstrings, message,
port, library and `pr_WindowPtr` are balanced on success and failure paths.
The rexxsyslib vector, `RexxMsg` packing/offset and action-flag evidence is in
[`version-morphos-ambient-arexx-sdk-abi-20260924.json`](../reference-captures/version-morphos-ambient-arexx-sdk-abi-20260924.json).

The three-CPU resident receipt is
`artifacts/version-morphos-native-20260924-ambient-arexx-v1/qualification.json`:

| CPU | HUNK bytes | SHA-256 | vectors | reachable methods |
| --- | ---: | --- | ---: | ---: |
| 68000 | 22,856 | `2b936e5a8464c11db5e5d72d9233a38064801f572a66a6887e4395d7504fa9b8` | 58 | 52 |
| 68020 | 23,124 | `c2fd78fafe66ed28625799477937eeec7ff0e10cf54d68cadbfad2137a53db2d` | 58 | 52 |
| 68040 | 22,696 | `ee283bcf98178801fa328968af9c061ce40ee2c0e2bbebf3f9a25bcfffc9aa78` | 58 | 52 |

Vectors include valid, malformed and unavailable Ambient replies, missing-port
and response-allocation paths, local-variable output, message ownership and
cleanup, plus unchanged system, RES, FILE, MD5SUM and Workbench startup
boundaries. Each CPU reports zero shared-image writes. This is fixture evidence
only: Ambient `IsFileSystem`/`LoadSeg` executable fallback, FILE `LoadSeg`
fallback, original guest comparison, PURE/resident lifecycle, licensing and
package admission remain open.

## MorphOS Ambient direct-file providers follow-up (2026-09-24)

After an unsuccessful ARexx query, the system path now reads `ambient_path`
with DOS `GetVar` and attempts that file when present. If its status is not
`RETURN_OK`, it proceeds in source order to `mossys:ambient/ambient`, then
`sys:system/ambient/ambient`. The file provider uses the existing `$VER:`
scanner, sets local variable `Ambient` only after successful parsing, and
restores `pr_WindowPtr` while releasing each file and public buffer.

The three-CPU resident receipt is
`artifacts/version-morphos-native-20260924-ambient-files-v1/qualification.json`:

| CPU | HUNK bytes | SHA-256 | vectors | reachable methods |
| --- | ---: | --- | ---: | ---: |
| 68000 | 24,036 | `c09a00c031196a0bfdc34399b2e554ecbe93395ddb99782a9f6153039f72c8d7` | 61 | 53 |
| 68020 | 24,320 | `0e7e649cf32665163e40194dd8a67a456c52093fcb2797552bc70f803e686d10` | 61 | 53 |
| 68040 | 23,876 | `c625c15e32c959a1a700de546d1dd131385c6586abeff20463c3ca99e11822e0` | 61 | 53 |

The separate Workbench DOS 36 candidate still passes fifteen supplied vectors
per CPU at
`artifacts/version-wb31-native-20260924-ambient-files-regression-v1/`.
These receipts prove fixture behavior and resident compatibility only. At this
checkpoint, the Ambient `IsFileSystem`/`LoadSeg` provider was still open; the
follow-up below closes its bounded resident fallback. FILE fallback/object
lookup, original guest comparison, packed/source correspondence,
PURE/resident lifecycle, licensing, installed classification and package
admission remain open.

## MorphOS Ambient loaded-resident fallback follow-up (2026-09-24)

When the Ambient file scan finds no `$VER:` tag, the provider now calls DOS
`IsFileSystem`. For filesystem-backed names it closes the file before DOS
`LoadSeg`, scans the loaded segment list for a Resident structure, constructs
the resident name plus selected `IdString` version tail, parses the version,
and calls DOS `UnLoadSeg`. If the name is not a filesystem, loading fails, or
the segment contains no Resident, the source-shaped diagnostic is emitted and
the provider sequence continues. Temporary text, scan buffers, file handles
and successfully loaded segments are released on each path.

The three-CPU resident receipt is
`artifacts/version-morphos-native-20260924-ambient-loadseg-v1/qualification.json`:

| CPU | HUNK bytes | SHA-256 | vectors | reachable methods |
| --- | ---: | --- | ---: | ---: |
| 68000 | 25,144 | `d888680113ef299572046d94e6eac5529df7fdfddc18a100c58c2e3c5d9df999` | 66 | 56 |
| 68020 | 25,468 | `95e5024595a153bd7e5b0306f84f2173c93056cc32b4f3343ccf056ab3e0384b` | 66 | 56 |
| 68040 | 24,984 | `0b11530d84d47529b7dc56f915829f7d19a5385157d40eec91ac933abb98b83d` | 66 | 56 |

The fixture covers a loaded resident in the first and a linked second segment,
non-filesystem and LoadSeg failure paths, a segment with no Resident, provider fallback order, and
balanced file/segment/buffer cleanup. Static resident compatibility reports
have zero managed runtime features, external targets, exception regions and
fatal fault sites; all CPUs report zero shared-image writes. The separate
Workbench DOS 36 candidate remains green at fifteen vectors per CPU in
`artifacts/version-wb31-native-20260924-ambient-loadseg-regression-v1/`.
These results remain supplied-vector evidence. MorphOS FILE fallback/object
lookup, original guest comparison, packed/source correspondence,
PURE/resident lifecycle, licensing, installed classification and package
admission remain open.

## MorphOS FILE loaded-resident fallback follow-up (2026-09-24)

When a named MorphOS `FILE` has no `$VER:` tag, the command now uses DOS
`IsFileSystem`, closes the open file, and calls DOS `LoadSeg`. It locates a
Resident through the loaded segment list, combines the resident name with its
version-bearing `IdString` tail, and parses that text through the existing
version formatter. Successful fallback preserves requested version/revision
comparison and full-file MD5SUM output. It frees the temporary text and scan
buffers and always unloads a successfully loaded segment. Non-filesystem,
LoadSeg-failure and missing-Resident vectors retain the captured not-found
behavior.

The three-CPU resident receipt is
`artifacts/version-morphos-native-20260924-file-loadseg-v2/qualification.json`:

| CPU | HUNK bytes | SHA-256 | vectors | reachable methods |
| --- | ---: | --- | ---: | ---: |
| 68000 | 25,772 | `d3c5ba6c3ff760f09807965417470f1b7603ae19cfb4d7e67aaa31aae173eb3e` | 70 | 57 |
| 68020 | 26,108 | `772045dc5f4e675056550290d32280dec0db3b600eb7fe7aea10156226932e59` | 70 | 57 |
| 68040 | 25,612 | `d239d3df44c06dc7af412f23076f6851e6afe30d51c7d7169418364f736b50dc` | 70 | 57 |

The fixture covers FULL resident date/extra formatting, MD5SUM preservation,
source-compatible diagnostic suppression when a digest succeeds but no
version is found, missing-Resident behavior, and ownership cleanup.
Compatibility reports show
zero managed runtime features, external native targets, exception regions,
fatal fault sites and shared-image writes. This remains bounded fixture
evidence. Object lookup, original guest comparison, packed/source
correspondence, PURE/resident lifecycle, licensing, installed classification
and package admission remain open.

## MorphOS resident command-segment lookup follow-up (2026-09-24)

When a named `RES` lookup misses the Exec Resident table, the bounded
MorphOS implementation now calls DOS `FindSegment` under `Forbid`/`Permit`,
searching the normal list then the system list. Ordinary segment lists are
scanned for `$VER:` and their payload is copied before parsing so the resident
command image remains unchanged. `CMD_INTERNAL` and `CMD_DISABLED` segment
entries route to the MorphOS `shellcmd` Resident. Missing segments and missing
tags retain the object-not-found behavior, and the segment/text/scratch paths
release their resources.

The three-CPU receipt is
`artifacts/version-morphos-native-20260924-segment-lookup-v4/qualification.json`:

| CPU | HUNK bytes | SHA-256 | vectors | reachable methods |
| --- | ---: | --- | ---: | ---: |
| 68000 | 28,408 | `fb522599d26f9a074a767791a053fba6cee7e035aa84ab037991b4bcbfe02d14` | 74 | 58 |
| 68020 | 28,752 | `63537332d9a132d799c5074cc05efd44874c1978139c0298c99b31765ffa20b2` | 74 | 58 |
| 68040 | 28,220 | `a73ba842fe0c5b24ce8082b87f7503a450ca5262dd0c8534a4949825e67e6106` | 74 | 58 |

The vectors cover normal and system list hits, missing-segment behavior, and
both internal and disabled fallbacks to `shellcmd`; all CPU images have zero
shared-image writes and no leaked fixture resources. This is still bounded
fixture evidence. The default `NAME` search through filesystem library
candidates, device lists and their filesystem candidates remains incomplete,
as do original
guest comparison, packed/source correspondence, PURE/resident lifecycle,
licensing, installed classification and package admission.

## MorphOS default named Resident lookup follow-up (2026-09-24)

The default named path now starts source-shaped resident lookup by calling DOS
`FilePart` on the supplied name and querying the Exec Resident table with the
resulting basename; `RES` follows the same basename lookup. It suppresses only
the source's terminal arbitrary input-file and volume lookup.
Successful Resident results share the parsed `IdString`, version comparison
and output path; a requested `MD5SUM` keeps the original unavailable-digest
prefix for Resident data.

The three-CPU receipt is
`artifacts/version-morphos-native-20260924-default-resident-v2/qualification.json`:

| CPU | HUNK bytes | SHA-256 | vectors | reachable methods |
| --- | ---: | --- | ---: | ---: |
| 68000 | 29,748 | `2fe227a698aaf80c971aa25097cc937adad10683451d4bea0265bed6a5857906` | 75 | 58 |
| 68020 | 30,128 | `2cdc781bcf116b048d66461d31cb70041c69741aecac22a755f89a61aebcbb9c` | 75 | 58 |
| 68040 | 29,596 | `006ff78b2ef7aee4b91dc7ee0bbd3b337aadedffa51a3a929e6c0e0f7ba631b7` | 75 | 58 |

The supplied vectors cover a path resolved by `FilePart` and Resident MD5
fallback, alongside all previous MorphOS Version cases. The Workbench 3.1
candidate remains green at fifteen vectors per CPU in
`artifacts/version-wb31-native-20260924-default-resident-regression-v2/`.
This does not implement MorphOS's subsequent library, device and filesystem
search candidates, or establish original-guest parity, packed/source
correspondence, PURE/resident lifecycle, licensing, installed classification
or package admission.

## MorphOS default Exec LibList lookup follow-up (2026-09-24)

After the DOS `FilePart`/Exec Resident-table lookup misses, the default named
path now scans the Exec `LibList` under `Forbid`/`Permit`, matching node names
with Utility `Stricmp`. It opens `utility.library` v37 with a caller-provided
base, reads the library's numeric version and revision while the list is
protected, copies the node name into invocation-owned memory before releasing
the lock, and uses the library `IdString` for matching `FULL` date/extra
output. If the parsed ID string is absent, malformed, or disagrees with the
library node's numeric values, it falls back to those numeric values. The
utility lease closes on lookup failure as well as success. `RES` uses the
source-observed `FilePart` basename too.
The candidate then continues to the existing command-segment fallback when no
loaded library matches; the remaining source-ordered filesystem and
`DeviceList` candidates still need implementation before that segment lookup
can occupy its final source position.

The refreshed three-CPU resident receipt is
`artifacts/version-morphos-native-20260924-liblist-v17/qualification.json`:

| CPU | HUNK bytes | SHA-256 | vectors | reachable methods |
| --- | ---: | --- | ---: | ---: |
| 68000 | 33,780 | `9bd9502c346b2d21c04c09c4e4dc927024d6f87ecd6b6e98c65c0d9b67ee6434` | 78 | 60 |
| 68020 | 34,152 | `0c2cb59569b68837952aa412d00183135f6b386b55c864a17ff8ce6fbf3646ae` | 78 | 60 |
| 68040 | 33,580 | `01f8ed2116292418770e006c1f60efb0ad66282a896614001dc6b27547b89a22` | 78 | 60 |

The vectors cover a path-derived, case-insensitive LibList hit with
numeric comparison and `FULL` date/extra output, `RES` basename lookup, and
the unavailable-`utility.library` error path.
All three images have no managed runtime features, exception regions, fatal
machine-fault sites, leaked resources or shared-image writes. A separate
Workbench 3.1 regression passes fifteen vectors per CPU at
`artifacts/version-wb31-native-20260924-liblist-regression-v4/`. These remain
supplied-vector results only. The MorphOS `MOSSYS:LIBS`/`LIBS:` searches,
`DeviceList`, `MOSSYS:DEVS`/`DEVS:`, terminal direct-file/volume ordering and
the final position of command-segment lookup remain open, as do original-guest
parity, packed correspondence, PURE/resident lifecycle, licensing, installed
classification and package admission.


## Workbench minimum comparison and real guest captures (2026-09-26)

The [normal-CLI captures](../reference-captures/workbench31-guest-command-probe-20260926.md)
execute the exact original Workbench C/Version under original DOS. Ordinary
`dos.library` and `VERSION 39` return 0; `VERSION 999` returns 5. All print
`dos.library 40.3\n`. Invalid numeric input returns 20 with the Shell output
stream containing `bad number\nC:Version failed returncode 20\n`. Exact bytes,
caller post-System IoErr and immutable source/media bindings are retained in
`artifacts/workbench31-guest-command-captures-20260926-v1/qualification.json`.
The caller's IoErr is not generally proven to equal child Result2, and the
numeric-error stream includes a Shell-generated failure line.

The [original binary audit](../reference-captures/version-wb31-named-comparison-audit-20260926.json)
locates the CODE comparator at 0x0232–0x0268. It uses signed LONG minimum checks,
after output. A larger major satisfies a request regardless of requested
revision; an equal major compares revision; REVISION without VERSION compares
revision alone. The candidate now applies this rule to its existing supported
system and RES paths and retains both normal and FULL output on warnings.
The normal system formatter now supplies a name before its two numeric LONGs,
and the FULL formatter puts its second string in the second LONG slot. Prior
fixtures incorrectly skipped slots and masked both VPrintf argument errors;
the fixture now consumes the real consecutive argument layout.

Fresh native receipt:
`artifacts/version-wb31-native-20260926-comparison-v1/qualification.json`.
All 32 cases per CPU pass on 68000/020/040: **96 total**, 17 reachable methods,
no leaked resources or shared-image writes and no managed runtime dependencies.
Twelve source identities and compiled inputs are checked before/after execution.
Coverage includes signed requests, major/revision boundaries, revision-only
requests, normal/FULL warning output and the retained provider-gap responses.

This does not implement default named lookup. The original searches a
case-insensitive Resident table, command segments and additional providers in
its own order before its loaded-library fallback; a LibList-only shortcut would
not preserve that behavior. FILE/UNIT/INTERNAL, full classic system/RES output,
case handling, normal secondary-error behavior and other lookup paths remain
open. The candidate still normalizes IoErr whereas the observed ordinary source
comparison path does not. No generated replacement has yet run through these
four clean guest cases. PURE/resident lifecycle, rights, installed metadata and
package admission stay open.


## Workbench Resident lookup and ten replacement guest pairs (2026-09-26)

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
The [FULL/options audit](../reference-captures/version-wb31-full-options-audit-20260926.json)
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
full grammar/failure matrix; Utility opening after ReadArgs rather than before; success IoErr normalization;
real OS concurrent/resident/PURE lifetime and cleanup; CopperStart/MorphOS
execution; complete media/installed metadata, rights and package qualification.
Caller post-System IoErr is not generally proven child Result2. Ten matching
cases do not complete the command or change the 0/200 shipping admission count.

## Workbench DOS v37 startup requirement (2026-09-27)

The original C/Version opens `dos.library` with requested version 0, then
rejects the library unless `lib_Version` is at least 37. The candidate entry
previously requested DOS 36. It now opens DOS 37, and the native fixture
requires that exact requested version on every Workbench case. The refreshed
resident HUNK qualification
`artifacts/version-wb31-native-20260927-dos37-v1/qualification.json` passes
58 supplied invocations per CPU on 68000/020/040, with no shared-image writes
or fixture leaks. This closes the startup-version mismatch for the bounded
candidate. It does not close the original open/library ordering, later ordered
providers, FILE lookup, guest parity for those paths, or other Version gates.
