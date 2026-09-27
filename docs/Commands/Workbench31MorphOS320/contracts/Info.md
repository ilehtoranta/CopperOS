# Info contract

Profiles: wb31 and morphos320. Goal steps: CC01 and CC11. Recorded: 2026-09-02.

Status: partial source-bound grammar and bounded native evidence for both
profiles. The
official 3.20 `c/info/info.c` is 27,153 bytes with SHA-256
`ae11f8ab679916df93ff4306c9cd95beca5726483103c9292f13a4abf6f2945f`. It
calls DOS ReadArgs with:

```text
DISKS/S,VOLS=VOLUMES/S,GOODONLY/S,BLOCKS/S,VERBOSE/S,DEVICES/M
```

This establishes source-observed 3.20 aliases and switch/list modes. Source
inspection also observes opening `locale.library` at version 38, an
invocation-owned `OpenLocale`, one combined read `LockDosList` over assigns,
volumes, and devices, `NextDosEntry` traversal, and an unconditional matching
`UnLockDosList` before cleanup. It separately uses `FreeArgs`, prints the
selected `IoErr` on failure, and polls for break while rendering device data.
Before scanning, every `DEVICES/M` pattern is checked with a 128-byte
`ParsePatternNoCase` buffer; a malformed pattern returns `RETURN_ERROR`.
The source inserts collected nodes into one case-insensitive `Stricmp` order,
then renders device and volume passes separately. Its `OpenCatalogA` setup and
`GetCatalogStr` path are enclosed in `#if 0`, so this source baseline uses the
default strings; the active date path can still use `info_datetime` and
`FormatDate`.
The companion source fragment `info_morphos1.c` (738 bytes,
SHA-256 `3dc14ef92b25145156e05b4b661ae38a62b2ddd75b837ba4bdb33472b187beba`)
records the DOS 51.8 capability check and `GetFileSysAttr` queries for
`FQA_NumBlocks` and `FQA_NumBlocksUsed`, with `InfoData` counters retained as
fallback and used-block clamping.

`src/Commands/Native/NativeMorphOSInfoCommand.cs`, entered through its private
resident entry, preserves the full source grammar, snapshots active public
volume/device names under one matching read lock, copies BSTR names into
invocation-owned storage, validates each filter before allocating the snapshot
or locking the DOS list, sorts the snapshot with utility `Stricmp`, releases
the list lock before filesystem `Info` queries, and renders devices before
the volume section regardless of DOS-list order. The entry calls the source's
final `PrintFault(IoErr(), NULL)` for non-OK results. It supports default,
`DISKS`, and `VOLS` selection, keeps `DEVICES/M` filtering active alongside
`VOLS`, and honors `GOODONLY`. Volume-only output returns
`RETURN_WARN`; a successful device `Info` row changes the result to
`RETURN_OK`. It renders bounded device and mounted-volume rows through public
DOS calls. Starting at DOS 51.8, `GetFileSysAttr` supplies
64-bit block counts; unsupported or failed queries use the corresponding
`InfoData` counter, and a successful pair clamps used blocks to the total.
Byte-scaled sizes render through K/M/G/T/P tiers, while `BLOCKS` prints the
full 64-bit counters. `VERBOSE` uses the source `GetDevStr` path for a valid
unit and device BSTR, then follows the `GetStartupStr` BSTR-style startup-
pointer fallback when that path cannot produce a device/unit string; both
render after unlock. Per-device `Lock` and `Info` faults keep the result at
`RETURN_WARN` until a device row succeeds, print the device-prefixed fault
unless `GOODONLY` is set, and preserve the selected `IoErr`. Mounted-volume
records copy the volume `DateStamp` while the DOS-list read lock is held, then
render the source-derived `[Mounted]` column after unlock. If DOS `GetVar`
returns `info_datetime` with an opened locale, `FormatDate` writes through an
invocation-owned Hook buffer; otherwise DOS `DateToStr` supplies the default
date fields. A conversion-failure vector covers omitted default date output.
Volume rows also emit
the complete ordered MorphOS 3.20 `GetFSysStr` label map, preserving
first-match behavior for duplicate IDs and the raw four-byte fallback for
unknown types. Device rows use `InfoData` state/type and DOS `NameFromLock` for
the displayed volume name, trim the returned trailing colon, and preserve the
DOS-list device name when name resolution fails. For filesystem type, the
snapshot reads `FileSysStartupMsg.Environment` while holding the DOS-list lock;
it accepts a non-zero `DosEnvec.DosType` only when the unit's high byte is zero
and the table reaches `DE_DOSTYPE`. A DOS-prefixed startup type leaves the
`InfoData` type in control, matching the source's `ID_DOS_DISK` test. Other
startup/provider data, advanced pattern syntax, packed-binary correspondence,
and exact guest output remain open. The inspected source's catalog setup is
compiled out; untranslated default strings are therefore the observed source
behavior, subject to packed-binary confirmation. The entry now matches the
observed startup acquisition order:
`utility.library` v37 is required before `doInfo`; `locale.library` v38 is
optional; when it opens, the default locale is opened before argument parsing
and `CloseLocale` is paired during cleanup, including the source's null-locale
case. A missing utility library fails before parsing, while missing locale
support does not suppress the command. The current three-CPU resident receipt
is `artifacts/cc11-info-morphos-native-20260923-pattern-validation-v3/qualification.json`;
it passes 66 supplied invocations per CPU, including malformed filter
rejection before buffer/list access, non-OK `PrintFault` publication, partial
device output on Ctrl-C, reversed DOS-list ordering, case-insensitive sorting,
device-before-volume rendering, selection/result semantics, every source-table
type label, duplicate-ID precedence and
unknown-type fallback, the DOS 51.7 legacy path, DOS 51.8 extended counters,
independent `InfoData` fallback, used-count clamping, K/M/G/T/P output, all
device state labels and unknown-state fallback, startup `DosEnvec` type
override/table-size/unit guards, type/name rendering and `NameFromLock`
fallback, both verbose startup-string paths, per-device Lock/Info errors and
`GOODONLY` result/`IoErr` behavior, `info_datetime` GetVar and Locale
`FormatDate` output-hook behavior with the default `DateToStr` path,
volume-date snapshot/formatting and failure, mounted/type columns,
provider-open failures, locale cleanup, list traversal and command-body paths,
with no shared-image writes or leaked resources. The
earlier 21-vector startup receipt remains at
`artifacts/cc11-info-morphos-native-20260923-startup-libs-v1/qualification.json`.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 29808 | `87e92c2a88c03dd0e11b5193e365185f353136dc43c053694d4faeda2be4a24f` | 30 | 66 |
| 68020 | 29576 | `443f75e7205eafea9182bf55265d48589f2940bdb66178ed20369b9f17ea1e0c` | 30 | 66 |
| 68040 | 29440 | `a81611cfe383c1f4a6f662759fe770be662f38fd82e81d68c5a01683f06ae21f` | 30 | 66 |

This does not establish the packed MorphOS binary, the complete Workbench
grammar or invalid disk-info behavior, exact output/status/IoErr parity,
handler lifetime, source reuse rights, or full guest parity. Implement through public DOS
volume/device APIs with invocation-owned locks and buffers; never substitute
host drive metadata. PURE/resident lifecycle and package admission remain
open gates.

The observed Workbench 3.1 Disk 2 `C/Info` binary is v38.2 (11.3.92),
1980 bytes, SHA-256
`873e4f7030f8c5a6dfed3e048c7888f6d9de394af795fa6a6c2322f2f1f605bf`. Its
classic template is `DEVICE`; the bounded Workbench candidate is entered by
`tests/Commands.AddBuffersNativeRoot/Workbench31InfoEntry.cs`, uses DOS 36,
and snapshots the public device/volume list before calling filesystem
`Info`. Its refreshed receipt qualifies resident 68000/020/040 HUNKs with
fourteen supplied vectors per CPU, including one- and two-device DOS-list
traversal, mixed mounted-volume output, startup and missing-DOS guards, no
runtime helpers/features, no fatal sites, and no shared image writes. The
current-source fixture requalification is
`artifacts/cc11-info-wb31-native-20260923-pattern-v14/qualification.json`;
the prior receipt is
`artifacts/info-wb31-native-20260920-missing-dos-v3/qualification.json`.
The HUNK identities are:

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 6228 | `48952cbb3a529a511e7cd7dbc69000a74cf2f778d69513acfb4ec5a82fbe76b8` | 17 | 14 |
| 68020 | 6248 | `0a70dd55c7c860268d2406586dfc0435d4db1166acb62c47ab1a536f402738a8` | 17 | 14 |
| 68040 | 6172 | `76e8f78aaaa00a0daaad0b53ca80d93d25f1c09e547263e20cc333e54cf2e871` | 17 | 14 |

The shared test-root assembly now exposes the MorphOS date-format hook as an
additional root in these Workbench fixture HUNKs. The Workbench entry still
passes its fourteen supplied vectors per CPU; this combined-root artifact is
not an isolated Workbench shipping binary.

The receipt is a candidate gate only: the missing Workbench disks 3-6,
installed overlay, exact original multi-device/error/status parity, and
shipping/PURE approval remain open.

The shared `NativeDosListTraversal` foundation supplies a single read-lock
iteration with guaranteed matching unlock and Ctrl-C publication. Info keeps
its own combined-list snapshot because it must copy BSTR names and DOS types
before unlocking; the supplied Info vectors cover that equivalent ownership
boundary directly.
