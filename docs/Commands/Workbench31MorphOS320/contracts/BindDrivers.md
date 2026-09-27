# `BindDrivers` contract

Profiles: `wb31`, `morphos320`. Goal stages: CC00, CC01, CC07, CC20.
Recorded: 2026-09-21; independent Workbench body and native qualification added
2026-09-25. Status: **MorphOS source candidate and Workbench static/body
candidate; original-guest parity, lifecycle, licensing and package gates remain
open**.

This contract freezes the observed command boundary and the source-backed
operation sequence. It is not a claim that the MorphOS source corresponds to
the Workbench binary, and it does not authorize copying source into the
replacement.

## Reference identity

| Profile | Reference member | Identity | Evidence limits |
| --- | --- | --- | --- |
| `wb31` | `Install3.1:C/BindDrivers` and `Workbench3.1:C/BindDrivers` | 1,420-byte Amiga HUNK, `binddrivers 38.2 (31.3.92)`, SHA-256 `8bc40c2cb0bb3bcd927d2f8696713284f5c554c06d6f65842bf7e062e0f60503` | Original media observed twice; no source or executed capture yet. The recorded protection is `0`, so PURE must not be assumed. |
| `morphos320` | `MorphOS/C/BindDrivers` | 4,229-byte packed native member, `BindDrivers 50.3 (26.03.2020)`, SHA-256 `4500691654c8592c8921de9a1df439b2707659b46d8e7a4923f87264cfd93cfb` | Original ISO member observed; installed overlay, execution, exact diagnostics and installed PURE flag remain unobserved. |

The available source evidence is the extracted MorphOS C file
`c/binddrivers/binddrivers.c`, 6,747 bytes, SHA-256
`52520753863DD0C8655210E03BFECA1566448D9DEF1F01245418D5515B8C93A2`, with
`$VER: BindDrivers 50.3 (26.03.2020)`. It is behavior evidence only. The
Workbench profile must be captured independently until binary correspondence
is established. A separate Workbench native body is now present, but its
static HUNK compilation alone is not execution or parity evidence.

## Invocation boundary

The MorphOS source entry is `main(void)`: it has no `ReadArgs` template and no
command-line options. The Workbench binary independently confirms no parser
call or option template. Neither profile should grow a user-facing argument
grammar.

The MorphOS source initializes `error` to `RETURN_FAIL` and changes it to
`RETURN_OK` only after all three required libraries have opened. A
`NameFromLock` or `AddPart` failure changes the result to `RETURN_WARN` after
printing the DOS fault and stops the scan. A failed initial `MatchFirst`, a
missing icon object, a missing `PRODUCT` tool type, a missing `ConfigDev`, a
missing resident, or a failed `LoadSeg` does not by itself change `error` in
the observed source. Exact packed-binary result and `IoErr` precedence remain
open and must be captured before shipping.

## Workbench 3.1 binary profile

The selected Workbench member is the 1,420-byte HUNK identified above. Its
hash-bound offline disassembly is recorded in
[`binddrivers-wb31-code-audit-20260925.json`](../reference-captures/binddrivers-wb31-code-audit-20260925.json).
This profile has materially different scan and PRODUCT behavior from the
MorphOS source; the MorphOS implementation must not be treated as proof of the
Workbench binary.

The Workbench body opens `dos.library` v37, `expansion.library` v37, then
`icon.library` v37. It locks `SYS:Expansion` with `ACCESS_READ`, examines the
lock, obtains the Expansion binding, makes the directory the current directory,
and walks entries with `ExNext`. It suffix-matches `.info` case-insensitively
in each `FileInfoBlock` filename and removes that suffix in place. It does not
use the MorphOS `MatchFirst`/`MatchNext`, `NameFromLock`, or `AddPart` flow, and
the disassembled loop has no per-entry directory-type check before offering a
matching name to Icon.

For a matching icon it calls `GetDiskObject` (not MorphOS's
`GetDiskObjectNew`), obtains `PRODUCT`, and parses pipe-separated decimal
`manufacturer[/product]` fields. The Workbench helper validates every digit
before calling DOS `StrToLong`; the optional product defaults to `-1`. It does
not contain MorphOS's search for `=` or `atoi`-style acceptance of signs,
whitespace and numeric prefixes. It resolves the fields with
`FindConfigDev`, links ConfigDev records, loads the filename without `.info`,
and initializes a Resident under a 16-byte `CurrentBinding`.

That structure is laid out as `cb_ConfigDev`, `cb_FileName`,
`cb_ProductString`, `cb_ToolTypes` at byte offsets 0, 4, 8, and 12. The
classic [`configvars.h`](https://amigadev.elowar.com/read/ADCD_2.1/Includes_and_Autodocs_2._guide/node00FC.html)
and the SDK structure use this order; the MorphOS source assigns its product
string and tool-type pointers to the corresponding named fields.

The Workbench Resident helper scans the declared extent of the current segment
for `RTC_MATCHWORD` and a self-pointing `rt_MatchTag`; the helper does not walk
the segment's next-HUNK link. Failed discovery or `InitResident` unloads the
segment, while a successfully initialized driver remains loaded. Cleanup
restores `CurrentDir`, releases the ConfigBinding, unlocks the directory, and
closes Icon, Expansion and DOS in reverse order.

Static disassembly also shows the result-level transitions: initial
`RETURN_FAIL`, then `RETURN_WARN` after the three libraries open, then
`RETURN_OK` after a valid expansion directory and ConfigBinding acquisition.
Per-driver failures do not change that result. No `PrintFault` or output call
appears in the inspected path. These findings still require an original guest
to settle DOS provider behavior, `IoErr`, exact startup-message effects, and
edge cases such as numeric overflow and malformed tool types.

## Required library and resource floors

The source requests version 37 for each library and opens them in this nested
order:

1. `dos.library` v37;
2. `icon.library` v37;
3. `expansion.library` v37.

The implementation must use public Kickstart APIs and invocation-owned guest
storage for all temporary records. Required calls are:

| Owner | Calls or data | Required behavior |
| --- | --- | --- |
| Exec | `LoadSeg`, `UnLoadSeg`, `InitResident`, `BADDR`, resident/HUNK layouts | Load each driver image, locate its resident, initialize it, and unload only when initialization or resident discovery fails. |
| DOS | `MatchFirst`, `MatchNext`, `MatchEnd`, `NameFromLock`, `AddPart`, `IoErr`, `PrintFault` | Traverse `SYS:Expansion/#?.info`, construct the candidate path, preserve matcher cleanup, and report path failures. |
| Icon | `GetDiskObjectNew`, `FindToolType`, `FreeDiskObject` | Read the `PRODUCT` tool type from each matching `.info` file and free every acquired `DiskObject`. |
| Expansion | `ObtainConfigBinding`, `ReleaseConfigBinding`, `FindConfigDev`, `SetCurrentBinding` | Own the configuration-binding transaction and publish the current binding only for a loadable resident driver. |

The replacement must fail closed when a required library or provider is not
available. It must not use a host directory, host loader, host filesystem, or
synthetic ConfigDev as a substitute for these guest-visible APIs.

## Source-observed scan sequence

After opening the libraries, the source aligns a stack `AnchorPath` to a
four-byte boundary, clears it with `memclr`, obtains the configuration
binding, and runs:

```text
MatchFirst("SYS:Expansion/#?.info", anchor)
while result == 0:
    NameFromLock(anchor.ap_Current->an_Lock, dirname, sizeof dirname)
    AddPart(dirname, anchor.ap_Info.fib_FileName, sizeof dirname)
    if fib_DirEntryType > 0:
        clear APF_DIDDIR
        do not descend (the source's APF_DODIR branch is disabled)
    else:
        diskObject = GetDiskObjectNew(dirname)
        product = FindToolType(diskObject->do_ToolTypes, "PRODUCT")
        configDev = GetConfigDev(product)
        remove the trailing ".info" from dirname
        segment = LoadSeg(dirname)
        resident = FindLibResident(segment)
        binding = { configDev, dirname, do_ToolTypes, product }
        SetCurrentBinding(&binding, sizeof binding)
        if InitResident(resident, segment) == 0:
            UnLoadSeg(segment)
        FreeDiskObject(diskObject)
    result = MatchNext(anchor)
MatchEnd(anchor)
ReleaseConfigBinding()
```

`MatchEnd` is required even when the loop is empty or terminates after a
path-processing failure. `FreeDiskObject` is required on every successful
`GetDiskObjectNew` path, including missing `PRODUCT`, missing configuration,
missing resident, and failed resident initialization. A loaded segment is
retained only when `InitResident` succeeds; the source does not unload a
successfully initialized resident.

The `CurrentBinding` fields are guest pointers into the invocation's path and
icon tool-type storage. They must remain valid for the duration required by
`InitResident`; a managed string or host pointer is not an acceptable backing
store.

## `GetConfigDev` contract

`PRODUCT` is processed by the source as follows:

1. Search the whole string for the first `=` and, if present, start after it.
2. Parse the current position as a manufacturer using decimal `atoi` rules.
3. Find the first `/` after that position. If found, parse the following
   position as the product ID and use that position as the start of the next
   separator search. If absent, use product ID `-1` and search from the
   current position.
4. Find the next `|`; continue after it, or finish when none exists.
5. For each pair, repeatedly call
   `FindConfigDev(previous, manufacturer, product)` and prepend each returned
   `ConfigDev` to a newly linked `cd_NextCD` chain. Supply the chain as
   `cb_ConfigDev` to `CurrentBinding`.

`atoi` accepts leading C whitespace and an optional sign, consumes a decimal
prefix, and returns zero when no digits start at the selected position. The
current MorphOS native candidate now follows the source's whole-string `=`
search and signed decimal-prefix behavior; packed-binary comparison remains
open. Preserve element order, repeated-device linking, and empty/malformed
string behavior until binary evidence proves a difference. Do not invent a
stricter `PRODUCT` grammar.

## `FindLibResident` contract

For each `BPTR` segment list, the source walks every HUNK segment. It treats
the first longword as the next-segment link, derives the segment end from the
HUNK longword count, scans the resident area as words, and accepts the first
`RTC_MATCHWORD` whose `rt_MatchTag` points back to that resident. If no segment
contains such a resident, it returns null and the loaded segment is unloaded.

The implementation must bounds-check the HUNK walk in guest address space and
must not scan host memory. Malformed segment chains, truncated resident areas,
and absent match tags are failure cases requiring deterministic cleanup.

## Cleanup and ownership

Cleanup is nested and reverse ordered:

```text
ReleaseConfigBinding()
CloseLibrary(expansion.library)
CloseLibrary(icon.library)
CloseLibrary(dos.library)
```

The matcher, every `DiskObject`, every failed `LoadSeg`, every temporary
product/path buffer, and every provider lease must be balanced on all early
returns. `ConfigDev` objects returned by the expansion provider are borrowed
unless the captured provider contract explicitly states otherwise; the command
must not free them as if it owned them.

The eventual resident implementation must prove no managed allocation,
exceptions, delegates, reflection, host I/O, or hidden runtime initialization
on the reachable path. The Workbench binary is recorded as non-PURE by its
observed protection word; the MorphOS PURE decision remains an installed
metadata gate, not an implementation assumption.

## Current native checkpoint

The source-bound MorphOS body is now present in
[`NativeMorphOSBindDriversCommand.cs`](C:/D-drive/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSBindDriversCommand.cs),
with a private resident entry in
[`NativeMorphOSBindDriversEntry.cs`](C:/D-drive/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSBindDriversEntry.cs).
The repeatable static receipt
[`qualification.json`](C:/D-drive/Koodit/GIT/CopperOS/artifacts/binddrivers-morphos-native-static-20260921-v3/qualification.json)
compiles the entry as resident HUNK for 68000/020/040 and records zero
managed runtime features, helpers, external native targets, exception regions,
and fatal machine-fault sites. The body uses public DOS, icon, Expansion and
Exec vectors, including a raw allocation-free DOS `LoadSeg` binding, and keeps
the matcher, icon object, ConfigDev chain and library leases invocation-local.
The companion runtime receipt
[`qualification.json`](C:/D-drive/Koodit/GIT/CopperOS/artifacts/binddrivers-morphos-native-entry-20260921-v2/qualification.json)
executes seven supplied boundary vectors per CPU: no-match and empty scans,
library-open failures, repeated/interleaved ownership, and matcher,
ConfigBinding, workspace, and library cleanup. It reports zero shared-image
writes and balanced resources. Provider-backed file/icon/resident
initialization is still unqualified.

## 2026-09-24 follow-up: PRODUCT parser aligned to source

The MorphOS candidate now matches the inspected `GetConfigDev` string
operations: it searches for the first `=` anywhere in the `PRODUCT` string,
uses `atoi`-style signed decimal-prefix parsing for manufacturer and product
fields, searches for `/` and `|` from the same positions as the source, and
keeps the source's repeated `FindConfigDev`/prepend order. The refreshed
three-CPU resident receipt is
`artifacts/binddrivers-morphos-native-entry-20260924-product-parser-v6/qualification.json`.
The main entry compiles fourteen reachable methods with zero runtime
features/helpers, external native targets, exception regions, fatal
machine-fault sites and shared-image writes, and reruns seven scanner-boundary
vectors per CPU. A separate resident parser entry invokes the production
`GetConfigDev` method against five raw PRODUCT strings and checks seven
manufacturer/product pairs across nine `FindConfigDev` calls. It covers a
non-leading `=`, signed and whitespace-prefixed `atoi` values, numeric prefixes,
a missing slash, multiple pipe-separated pairs, empty input, repeated
ConfigDev results and the `cd_NextCD` prepend chain. That parser entry has five
reachable methods, no managed allocation sites or runtime features/helpers,
external targets, exceptions, fatal sites or shared-image writes. At this v6
checkpoint, the parser-specific entry did not exercise directory matching,
Icon calls, driver `LoadSeg`, resident discovery/initialization or packed
Workbench correspondence.

## 2026-09-24 follow-up: provider-backed successful MorphOS scan

The scanner fixture now includes one complete successful driver match, using
supplied DOS, Icon, Expansion, Exec and resident-memory providers. It checks the
`MatchFirst` anchor, `NameFromLock` lock and path buffer, `AddPart` input/output,
Icon `GetDiskObjectNew`/`FindToolType`/`FreeDiskObject`, PRODUCT lookup and
two-node ConfigDev prepending, `.info` removal, raw `LoadSeg`, Resident-record
discovery, `CurrentBinding` guest-pointer contents, `SetCurrentBinding`, and a
successful `InitResident`. The successful path retains the segment and balances
the matcher, ConfigBinding, Icon/DOS/Expansion leases and workspace. A separate
parser entry still exercises the production parser with five PRODUCT strings.

The three-CPU receipt is
`artifacts/binddrivers-morphos-native-entry-20260924-driver-success-v7/qualification.json`.
It passes eight scanner invocations and one parser invocation per CPU. The
main HUNK remains 4,056 / 4,136 / 4,048 bytes for 68000 / 68020 / 68040, with
fourteen reachable methods and zero managed runtime features/helpers, external
native targets, exception regions, fatal machine-fault sites or shared-image
writes. The parser HUNK remains 1,288 / 1,344 / 1,284 bytes with five
reachable methods and no managed allocation sites or shared-image writes.

At this v7 checkpoint the supplied providers did not establish real guest
filesystem/Icon/Expansion behavior or Workbench correspondence. The v8
follow-up below adds failure-path vectors; real-provider and binary-parity gates
remain open.

## 2026-09-24 follow-up: scanner provider-failure matrix

The scanner fixture now covers seventeen invocations per CPU. In addition to
the successful driver path and library-open/no-match boundaries, it exercises
directory entries, absent Icon objects, missing `PRODUCT`, missing ConfigDev,
missing resident, `LoadSeg` failure, `InitResident` failure, and
`NameFromLock`/`AddPart` failures. It asserts the source's result and `IoErr`
behavior, operation-specific `PrintFault` text, matcher/ConfigBinding cleanup,
DiskObject ownership, and whether a successfully loaded segment is retained or
unloaded. All missing-provider and load/init-failure paths release acquired
guest allocations; the success path retains its initialized segment.

The refreshed three-CPU resident receipt is
`artifacts/binddrivers-morphos-native-entry-20260924-failure-paths-v8/qualification.json`.
It passes seventeen scanner invocations and one production-parser invocation
per CPU. Main HUNK sizes and method count remain 4,056 / 4,136 / 4,048 bytes
and fourteen reachable methods for 68000 / 68020 / 68040. Static compatibility
reports retain zero managed runtime features/helpers, external native targets,
exception regions and fatal machine-fault sites; all runtime cases report zero
shared-image writes and no leaked allocations.

At this v8 checkpoint, malformed HUNK bounds and multi-HUNK discovery were
still open. The v9 follow-up below covers those structural paths; real-provider
and binary-parity gates remain open.

v8 receipt table:

| CPU | Main bytes | Main SHA-256 | Main methods | Main vectors | Parser bytes | Parser SHA-256 | Parser calls |
| --- | ---: | --- | ---: | ---: | ---: | --- | ---: |
| 68000 | 4056 | `136d71ba36579a4d473a668e794a6987d8fbd6648955c5df0b009678e2eadeb6` | 14 | 17 | 1288 | `392c4dbf3e913286188a2c4a0d67a646af9ddf1aa4dfbca153ad47b3d6ab1e67` | 9 |
| 68020 | 4136 | `0f1f7ef958cf0715b0ef7baf0914cb8e98ed783f2b5453fad2fb0681a00f4dc8` | 14 | 17 | 1344 | `12b5766fe790bfebfc753be4b410cb795d2399792ecb209ed5a85471cf32734e` | 9 |
| 68040 | 4048 | `e6b111fef91608b488de338f632eaac48e9a4de5e868d6060a57dd23fcb9498b` | 14 | 17 | 1284 | `bde9902faad5d770a7d4557ca8e1018f40abf25beb9cf5d5d481692bb09f2f0e` | 9 |

## 2026-09-24 follow-up: bounded resident segment-list discovery

`FindLibResident` now rejects BPTR shift overflow, unreadable HUNK headers,
short HUNKs, longword-count multiplication overflow, address-end wrap and
declared HUNK extents whose final byte is not guest-addressable according to
public Exec `TypeOfMem`. Its Resident-record loop requires the full match-word
and match-tag fields to fit within the declared HUNK. It follows a valid
second-HUNK link and uses a constant-space fast/slow link walk to reject cyclic
segment lists without imposing a segment-count limit that could reject valid
long lists. An invalid extent/list returns no resident, so the caller unloads
the segment and frees the DiskObject through the already-qualified cleanup
path.

The refreshed receipt is
`artifacts/binddrivers-morphos-native-entry-20260924-hunk-bounds-v9/qualification.json`.
It passes twenty-three scanner vectors and one production-parser vector on each
of 68000/020/040. Main HUNKs have fifteen reachable methods and zero managed
runtime features/helpers, external native targets, exception regions or fatal
machine-fault sites; the runs have no fixture leaks or shared-image writes.
The fixtures check second-HUNK success, a too-short HUNK, truncated and
overflowing declared extents, an invalid BPTR link, and a two-node cyclic
segment list.

Malformed/unterminated tool-type strings, `AnchorPath` directory flag
behavior, multiple PRODUCT pairs through the full scan, repeated/interleaved
provider-backed scans, real OS Icon/Expansion/filesystem behavior, original
guest and Workbench binary correspondence, installed PURE/resident flags,
licensing and package placement remain open.

v9 receipt table:

| CPU | Main bytes | Main SHA-256 | Main methods | Main vectors | Parser bytes | Parser SHA-256 | Parser FindConfigDev calls |
| --- | ---: | --- | ---: | ---: | ---: | --- | ---: |
| 68000 | 4560 | `ec4bc3e497ca00127e0507e0965627f408b76c7dcb17ebbf5e227b8a73caffd0` | 15 | 23 | 1288 | `392c4dbf3e913286188a2c4a0d67a646af9ddf1aa4dfbca153ad47b3d6ab1e67` | 9 |
| 68020 | 4640 | `b405f91655f8be0b95b8dd06397dd42feb454574b147236fc3f458f88d81f6b2` | 15 | 23 | 1344 | `12b5766fe790bfebfc753be4b410cb795d2399792ecb209ed5a85471cf32734e` | 9 |
| 68040 | 4552 | `14ef75b62473b35c5321d5a5c43a44d8f1f4a7aee4fe3bdfc7f7381642f3eb4f` | 15 | 23 | 1284 | `bde9902faad5d770a7d4557ca8e1018f40abf25beb9cf5d5d481692bb09f2f0e` | 9 |

## v10 - full-scan multiple PRODUCT pairs

The successful MorphOS scan now sends `PRODUCT=514/2|33/-4` through the
production parser and provider callbacks. The first pair returns two
manufacturer/product matches; the second returns a third. The fixture checks
the five calls in source order, including the terminating query after each
pair, then verifies that `SetCurrentBinding` receives the third node at the
head of the chain (`third -> second -> first`). All three allocated nodes are
released by the owning DiskObject cleanup path.

Receipt `artifacts/binddrivers-morphos-native-entry-20260924-multipair-v10/qualification.json`
passes twenty-four scanner vectors plus the parser vector per CPU. The
resident HUNKs retain fifteen reachable methods, zero managed runtime
features/helpers, external targets, exception regions or fatal sites, and the
fixture reports zero leaks or shared-image writes.

| CPU | Main bytes | Main SHA-256 | Main methods | Main vectors | Parser bytes | Parser SHA-256 | Parser FindConfigDev calls |
| --- | ---: | --- | ---: | ---: | ---: | --- | ---: |
| 68000 | 4560 | `d0bf4e0eb7514721de8fa023c2599206f41f3671b10db9974fc59cdb4c8d1264` | 15 | 24 | 1288 | `392c4dbf3e913286188a2c4a0d67a646af9ddf1aa4dfbca153ad47b3d6ab1e67` | 9 |
| 68020 | 4640 | `354b8738fd42fe2f19312f36c1bd8e529897d109162c97f4384e6186919bd470` | 15 | 24 | 1344 | `12b5766fe790bfebfc753be4b410cb795d2399792ecb209ed5a85471cf32734e` | 9 |
| 68040 | 4552 | `6f98348573915db4a3b6b4a65d324f37f2363f2cc586c4625e12ddb701e3881a` | 15 | 24 | 1284 | `bde9902faad5d770a7d4557ca8e1018f40abf25beb9cf5d5d481692bb09f2f0e` | 9 |

## v11/v12 - concurrent scans and AnchorPath flags

The qualification fixture now instruction-interleaves the successful
single-pair and multiple-pair scans. Each caller uses a distinct fake
AnchorPath, DiskObject, ToolType array, PRODUCT string, ConfigDev allocation
set and HUNK segment range, so both scans exercise the same resident image
without aliasing each other's provider state.

For matched entries, the matcher sets `APF_DIDDIR | APF_DOWILD |
APF_NOMEMERROR`. The directory case verifies that the command clears only
`APF_DIDDIR` before the next `MatchNext`, while preserving the other bits;
file entries preserve all flags, and an empty successful scan keeps its
zero-initialized AnchorPath. The command body already performs the source-
observed transition; this follow-up adds explicit runtime evidence for it.

Receipt `artifacts/binddrivers-morphos-native-entry-20260924-anchor-flags-v12/qualification.json`
passes twenty-six scanner vectors and one parser vector per CPU on
68000/020/040. Main HUNKs retain fifteen reachable methods with zero managed
runtime features/helpers, external targets, exception regions or fatal sites;
interleaved runs report zero leaks and shared-image writes.

| CPU | Main bytes | Main SHA-256 | Main vectors |
| --- | ---: | --- | ---: |
| 68000 | 4560 | `d0bf4e0eb7514721de8fa023c2599206f41f3671b10db9974fc59cdb4c8d1264` | 26 |
| 68020 | 4640 | `354b8738fd42fe2f19312f36c1bd8e529897d109162c97f4384e6186919bd470` | 26 |
| 68040 | 4552 | `6f98348573915db4a3b6b4a65d324f37f2363f2cc586c4625e12ddb701e3881a` | 26 |

The historical `FindToolType` AutoDoc describes a returned pointer into the
matched value string and provides no length value. The parser therefore keeps
the source's NUL-terminated C-string behavior instead of imposing an arbitrary
maximum that would reject otherwise valid tool types. An unterminated result
from a broken Icon provider remains unverified; no guest-visible compatibility
claim is made for that invalid provider response. See the
[icon.library AutoDoc](https://amigadev.elowar.com/read/ADCD_2.1/Includes_and_Autodocs_3._guide/node0349.html).

Real OS Icon/Expansion/filesystem behavior, original guest and Workbench
binary correspondence, installed PURE/resident flags, licensing and package
placement remain open.

## 2026-09-25 follow-up: independent Workbench body and binding layout

The separate Workbench resident candidate is in
[`NativeWorkbench31BindDriversCommand.cs`](../../../../src/Commands/Native/NativeWorkbench31BindDriversCommand.cs),
with its entry in
[`NativeWorkbench31BindDriversEntry.cs`](../../../../tests/Commands.AddBuffersNativeRoot/NativeWorkbench31BindDriversEntry.cs).
It follows the independently audited Workbench flow: DOS, Expansion, then Icon;
`Lock`/`Examine`/`CurrentDir`/`ExNext`; `GetDiskObject`; strict decimal
`PRODUCT` parsing through DOS `StrToLong`; and current-HUNK Resident discovery.
This body intentionally has no `ReadArgs` grammar because neither reference
does.

The Workbench body now writes `cb_ProductString` at offset 8 and
`cb_ToolTypes` at offset 12. Its repeatable three-CPU static and
supplied-provider qualification is
`artifacts/workbench31-binddrivers-native-entry-20260925-provider-v4/qualification.json`.
All images compile with 21 reachable methods and zero managed runtime features
or helpers, external native targets, exception regions, or fatal sites. Twelve
fixture invocations per CPU cover library-open failures/order, valid and
invalid expansion directories, empty and matching scans, case-insensitive
`.info` removal, rejection of a signed `PRODUCT` field, successful ConfigDev
lookup, `CurrentBinding` offsets, segment Resident discovery, `LoadSeg`,
`InitResident`, startup-message reply after cleanup, and the `LoadSeg` failure,
missing-Resident unload, and rejected-`InitResident` unload paths.
The fixture caught and led to a fix for a case-folding error in the `.info`
suffix helper. The MorphOS
`CurrentBinding` write order was also corrected to the public structure order.
Its refreshed resident receipt
`artifacts/binddrivers-morphos-native-entry-20260925-currentbinding-order-regression-v2/qualification.json`
passes 26 scanner invocations and one parser invocation per CPU for 68000,
68020, and 68040, with zero shared-image writes and balanced fixture resources.
These receipts check supplied public-vector behavior; they do not prove the
packed executable's full semantics or real OS provider behavior.

The supplied-provider failure paths are now covered for both profile bodies.
Next, compare both profiles on their original guests and close installed
metadata, PURE/resident lifecycle, licensing, and package admission. The
Workbench fixture still does not establish behavior on the original guest.

## Qualification plan

The independent `wb31` and `morphos320` resident entries now compile as
68000/020/040 HUNK images. Before any shipping claim, qualify at minimum:

1. all three library-open failure boundaries and the successful no-match scan
   (MorphOS supplied-vector coverage is present; add equivalent Workbench
   provider coverage, then capture original guest results);
2. empty scans, directory entries, missing-icon/`PRODUCT`/`ConfigDev`/resident,
   `LoadSeg`, and `InitResident` failure paths (supplied-vector coverage is
   present; real-provider behavior remains open);
3. `NameFromLock`/`AddPart` warnings, `PrintFault` arguments and `IoErr`
   publication (supplied-vector behavior is covered; original diagnostic and
   precedence parity remains open);
4. multiple PRODUCT pairs and multiple matching `ConfigDev` nodes (the
   parser fixture and MorphOS end-to-end scanner now cover them with
   source-ordered `FindConfigDev` calls and a three-node prepend chain;
   original guest and Workbench evidence remain open);
5. segment-list resident discovery across more than one HUNK and malformed
   bounds (supplied-vector coverage is now present; original guest behavior
   remains open);
6. repeated and interleaved callers with zero leaks, balanced matcher and
   library leases, and no shared-image writes (supplied-vector no-match/empty
   repeats and two successful interleaved provider-backed scans now pass;
   actual guest concurrency behavior remains open); and
7. a real Workbench and MorphOS guest containing expansion `.info` files and a
   loadable resident driver.

The current state is deliberately **not** a shipping qualification. Packed
template, diagnostic text, Workbench source correspondence, installed
selection, provider-backed guest behavior, PURE/resident lifecycle, package
placement, and differential comparison remain open for both profiles.
