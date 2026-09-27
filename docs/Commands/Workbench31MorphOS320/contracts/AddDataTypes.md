# `AddDataTypes` contract

Profiles: `wb31`, `morphos320`. Goal stage: CC20. Recorded 2026-09-25.
This is a reference contract and behavior boundary; no profile is shipping
qualified.

## Reference identity

| Profile | Reference | Identity | Evidence limit |
| --- | --- | --- | --- |
| `wb31` | Workbench 3.1 M10 disk 2 `C:AddDataTypes` | 5,880-byte Amiga HUNK, `adddatatypes 39.2 (27.7.92)`, SHA-256 `391da11b39bfc7c492c1b58aa9e442f1c1712bd507380903a88a4534d27c264d` | Original media and binary metadata are captured; no source or executed command comparison. |
| `morphos320` | MorphOS 3.20 ISO `MorphOS/C/AddDataTypes` | ISO extent 175124, 7,751-byte packed member, `AddDataTypes 50.6 (27.11.04)`, SHA-256 `afe23d74f3c3b18cd45f892189a6a26bf1bc8d42a426ec7db816e0c9995d11dd` | Packed member is hash-bound; the released source is behavior evidence only. |

The complete bounded audit is in
[`adddatatypes-reference-audit-20260922.json`](../reference-captures/adddatatypes-reference-audit-20260922.json).

## Invocation boundary

Workbench's binary contains the candidate template
`FILES/M,QUIET/S,REFRESH/S`. The MorphOS source freezes
`FILES/M,QUIET/S,REFRESH/S,LIST/S`. Keep this profile difference until the
Workbench guest establishes whether `LIST` is accepted, ignored or rejected.
Both profiles must use DOS `ReadArgs` and preserve its ownership and result
semantics. A replacement must not invent recursive or host-directory modes.

The command has two startup modes. A Workbench startup waits for and consumes
the `WBStartup` message, switches to each supplied `WBArg` lock, loads the
descriptor, restores the previous current directory and replies only after
all command cleanup. A CLI startup parses the template and performs either a
refresh of the system datatype directories or an explicit FILES scan; MorphOS
also supports its source-defined LIST operation. The Workbench candidate uses
the captured three-result template and does not add LIST.

## Registration ABI boundary

The public `datatypes.library` autodocs expose datatype lookup, object
creation, and release APIs, but no public API for registering a descriptor in
the shared datatype list ([AmigaOS 3 autodocs](https://developer.amigaos3.net/autodocs/datatypes.library/)).
The released MorphOS command source instead finds the Utility named object
`DataTypesList`, reads its `no_Object`, and operates on the private
`DataTypesList`/`CompoundDatatype` layout under its semaphore. This is behavior
evidence, not a license to copy implementation code. Before implementing the
transaction, independently confirm the required field offsets and node
ownership against both packed command references and the target library ABI;
keep any interop definition minimal and document its provenance. If that ABI
cannot be established for a profile, leave that profile open rather than
publishing a partial registration stub.

A supplementary MorphOS ABI capture is recorded in
[`morphos-datatypes-library-source-audit-20260925.json`](../reference-captures/morphos-datatypes-library-source-audit-20260925.json).
The official MorphOS source index lists `datatypes.library` under MorphOS 3.20,
and the linked archive contains `datatypes_intern.h` with the private field
orders used by the candidate. The page currently links an archive whose path
is versioned `3.19`, so its correspondence to the installed MorphOS 3.20
library is not yet hash-bound. The exact packed `MorphOS/Libs/datatypes.library`
member is also captured from the hash-bound 3.20 ISO; its identity is recorded
in [`morphos-datatypes-library-binary-audit-20260925.json`](../reference-captures/morphos-datatypes-library-binary-audit-20260925.json).
It is a packed native binary, and the candidate's private offsets have not yet
been confirmed against its implementation.

The companion Workbench 3.1 `Libs/DataTypes.library` is present on the same
hash-bound disk as `C:AddDataTypes`. Static analysis of the original command
confirms the five list-head offsets in
[`workbench-datatypes-library-abi-audit-20260925.json`](../reference-captures/workbench-datatypes-library-abi-audit-20260925.json):
the sorted list at byte 46, followed by the binary, ASCII, IFF and misc lists
at 60, 74, 88 and 102. Its directory-registration path also reads and writes
the shared list's first refresh-date longword at byte 120, using the same
pointer that it uses to derive the sorted-list address. These findings agree
with the candidate offsets. Static disassembly also confirms the candidate
descriptor offsets for mask storage, DTCD storage, loaded segment/function,
open count and the embedded DTHD boundary. The instruction-level observations
and semantic limits are recorded in the ABI audit. The longest-mask field,
meaningful list-tail fields, node/semaphore ownership and concurrency
semantics remain open, and no guest comparison is recorded. This narrows the Workbench layout
uncertainty but does not establish complete ABI closure for either profile.

The classic command has an additional startup path: when `DataTypesList` is
absent from Utility's named-object namespace, its HUNK calls the Utility
named-object allocation path and initializes a list. A separate Workbench
candidate now implements that fallback and is qualified by a supplied-vector
resident fixture. It requests the captured 140-byte user-space extent, checks
the raw `AllocNamedObjectA` tag vector (`ANO_NameSpace`, `ANO_UserSpace`,
`ANO_Flags`, `TAG_DONE`), initializes the list semaphore and publishes the
object. The public Utility contract places `NamedObject.no_Object` in the
user-space extent ([Utility autodocs](https://developer.amigaos3.net/autodocs/utility.library/)); the exact classic list extent and ownership/lifetime
semantics remain unconfirmed. The Workbench HUNK passes fourteen fixture cases
per CPU, including existing-object reuse, creation/publication, open/parser
failure cleanup, null `no_Object` release, single and multiple FILES patterns,
valid DTHD registration, combined option-slot mapping and precedence, and
classic REFRESH date/scan behavior; the receipt is
[`adddatatypes-workbench31-native-20260925-v5`](../../../../artifacts/adddatatypes-workbench31-native-20260925-v5/qualification.json).
This is not original guest parity or complete ABI closure. The MorphOS profile
retains its distinct source-based LIST/registration path and its private-library
version correspondence and node/segment ownership remain open.

## Shared datatype-list transaction

The command obtains the named `DATATYPESLIST` object and holds its semaphore
while it initializes built-in `binary`, `ascii`, `iff` and `directory`
descriptors, scans descriptors and replaces/inserts entries. Descriptor
matching is case-insensitive. Replacement must preserve the original list
ordering rules: a descriptor with an executable conversion function sorts
before one without it, then mask specificity, pattern use and priority decide
the remaining order. An open existing descriptor is not replaced.

Descriptor files are IFF data with `DTYP/DTHD` and optional `DTYP/DTCD`
chunks. The replacement must use `iffparse.library` and public DOS/Exec APIs,
keep file-backed pointers inside invocation-owned guest storage, and unload a
failed function segment while retaining a successfully initialized segment.
The released MorphOS source currently breaks after the first successful
`ParseIFF` EOC because it records a second-loop crash; this is a compatibility
boundary to compare against the packed command before changing it.

## Scanning and refresh

`FILES` patterns are scanned with `MatchFirst`, `MatchNext` and `MatchEnd`.
The first directory may be entered, but ordinary recursive traversal is not
allowed. `.info` and `.backdrop` entries are excluded case-insensitively.
Ctrl-C stops the scan, prints `ERROR_BREAK` unless `QUIET` is present, and
still balances the matcher and current-directory lease. A nonterminal matcher
error is printed unless `QUIET` is present.

`REFRESH` first checks the datatype-directory date stamp. On MorphOS it checks
`DEVS:DataTypes` and then `MOSSYS:Devs/DataTypes`; the latter has priority and
the two locks are deduplicated when they refer to the same directory. If the
directory contains an entry, a scan is skipped when its date matches the
shared-list stamp; a changed date updates the stamp before scanning. An empty
directory does not update the stamp and is still scanned. Workbench static
disassembly confirms a `REFRESH` date scan of
`DEVS:DataTypes`, followed by a conditional scan of
`DEVS:DataTypes/#?`; the command uses the first matching directory entry
to compare/update its shared date stamp. The Workbench command also opens
`dos.library` v39, `utility.library` v39, `intuition.library` v39,
`iffparse.library` v37 and `locale.library` v38, then attempts to open
`sys/c.catalog` (catalog failure is nonfatal in the observed control flow).
The instruction-level evidence and remaining named-list bootstrap limits
are recorded in `adddatatypes-reference-audit-20260922.json`.

The Workbench resident fixture now exercises this profile-specific refresh
path against supplied DOS vectors: a changed date updates the shared stamp and
scans the wildcard; an unchanged date skips the scan; an empty directory does
not replace the stamp but still scans; and a missing directory lock still
reaches the wildcard scan. It checks lock/FIB and matcher cleanup plus
requester-window restoration. This confirms candidate behavior only; the
original Workbench guest must still establish exact date, result, diagnostic
and `IoErr` semantics.

`LIST` enumerates the sorted list and prints `base-name, "name"` pairs.
Ctrl-C flushes output and reports `ERROR_BREAK`. The MorphOS command body now
owns CLI orchestration in `Run`: it holds the required library leases and
shared-list semaphore around built-in setup, `ReadArgs`, REFRESH/FILES/LIST,
and cleanup. `RunWorkbenchStartup` composes the corresponding startup-message
path; the outer native entry remains responsible for receiving and replying to
the message and for opening DOS. The current resident receipt executes both
paths through fixture DOS vectors. It does not establish original-guest parity,
the DOS loader's callback behavior, or shipping eligibility.

## Resident implementation progress

The MorphOS candidate now has a bounded shared-list transaction in
[`NativeMorphOSAddDataTypesCommand.cs`](../../../../src/Commands/Native/NativeMorphOSAddDataTypesCommand.cs).
Its layout constants are derived from the SDK's public 68k layouts
(`SignalSemaphore` 46 bytes, `List` 14, `DataType` 58, and
`DataTypeHeader` 32) plus the released source's private field ordering. The
candidate finds `DataTypesList`, reads public `NamedObject.no_Object` at offset
zero, releases the named-object reference, and obtains the list semaphore. It
also creates missing `binary`, `ascii`, `iff`, and `directory` built-ins in
their typed lists and inserts their second nodes into the global list with
case-insensitive ordering. With the caller holding the semaphore, `ListTypes`
walks that sorted list, reads each descriptor's base/name pointers from its
header, prints the MorphOS `"%s, \"%s\"\n"` format, and checks Ctrl-C before
each row. The break path flushes DOS output and calls `PrintFault(ERROR_BREAK)`;
it leaves the command result level unchanged as the source does.

The current classic-68k offset candidate is:

| Record | Offsets and size (decimal bytes) |
| --- | --- |
| `DataTypesList` | lock 0; sorted list 46; binary list 60; ASCII list 74; IFF list 88; misc list 102; longest mask 116; date stamp 120; total 132 |
| `CompoundDatatype` | public `DataType` 0; Node2 14; private flags 58; parse-pattern size 62; parse-pattern memory 66; DTCD pointer 70; DTCD size 74; segment 78; function 82; open count 86; inline header 90; total 122 |

These are audit targets for packed-reference and guest checks, not independent
confirmation of the MorphOS library's native ABI.

The original LIST-only resident 68000/68020/68040 receipt remains available at
[`qualification.json`](../../../../artifacts/adddatatypes-morphos-native-list-20260925-v1/qualification.json).
The current three-CPU CLI and MorphOS Workbench startup/library/ReadArgs/
FILES/IFF/DTHD/DTCD/REFRESH-boundary qualification is recorded at
[`qualification.json`](../../../../artifacts/adddatatypes-morphos-cli-run-20260925-v18/qualification.json).
The resident test entry now delegates to the production `Run` and
`RunWorkbenchStartup` methods. It passes thirty-eight command-fixture cases
plus a callback-wrapper probe per CPU, including the prior list initialization,
sorted LIST output and Ctrl-C
behavior; option-slot mapping and ownership; parser/allocation failures; all
four library-open failure boundaries; FILES scanner no-match cleanup, one
synthetic matched file, and first-directory entry with `APF_DODIR`/`APF_DIDDIR`
flag handling; and a nested directory encountered after the first descriptor,
which must not trigger a second directory entry but must still allow a later
matched descriptor to be processed. Its Workbench case consumes a `WBStartup`
message, handles zero file arguments and one descriptor argument, rejects a
missing argument list and a 32-bit `sm_NumArgs`/`WBArg` range wrap, covers a
partial utility-library-open failure, switches
to a descriptor's `WBArg` lock, restores the prior directories, releases the
duplicated startup lock, and replies after command cleanup. The matrix also
covers a valid DTHD descriptor with and without a
DTCD segment; and
duplicate-descriptor cases for open-descriptor preservation, case-insensitive
same-descriptor ID updates, and replacement of a changed closed descriptor.
Malformed-header cases reject a short DTHD, an out-of-range name offset, a
pattern without an in-header NUL terminator, and a mask extending beyond the
declared header.
The DTCD cases verify copied code bytes, invocation-local loader
state, resident callback addresses, the `InternalLoadSeg` register and stack
arguments, successful segment/function publication, and failed-loader
cleanup. A separate resident HUNK invokes the read, allocate and free adapters
through the SDK's indirect-call wrappers and checks partial reads, EOF, copied
bytes and balanced allocation ownership. It caught and fixed the read adapter's
buffer/count mapping to the published A0/D0 contract. The REFRESH cases also
verify lock alias de-duplication, unchanged-date skips, empty-directory scans,
date-stamp updates, path order and process window-pointer restoration. The
command image has 46 reachable methods and the probe has five; both have no managed allocations,
runtime helpers, external targets, exception regions, fatal sites, leaks, or
writes to the command image.

The callback-wrapper assertions are synthetic CPU-fixture evidence. They do
not invoke the callbacks through MorphOS's DOS loader. The released source
uses `AROS_STACKSIZE` for embedded DTCD code and separately uses 4096 for its
external function-name loader path. The pinned official ppc-morphos target
header defines `AROS_STACKSIZE` as 32768; the exact historical header-to-binary
correspondence for AddDataTypes 50.6 remains open. The current MorphOS
candidate now passes 32768 for embedded DTCD loading. The Workbench 3.1 HUNK
passes 4096 at its embedded-DTCD `InternalLoadSeg` call site, confirmed by
static disassembly. The source/header/binary evidence and limitations are
recorded in
[`adddatatypes-stacksize-audit-20260925.json`](../reference-captures/adddatatypes-stacksize-audit-20260925.json).

The v19 supplied-vector qualification adds segment-ownership cases: an open descriptor retains its
existing segment; a newly loaded candidate discarded as an open or same
duplicate unloads only the candidate segment after freeing its copied DTCD
input; and replacement of a closed descriptor unloads its old segment before
freeing the descriptor. These assert the candidate's call order, not the real
DOS loader's unload behavior or guest safety. The REFRESH cases are synthetic
fixture evidence and do not establish guest-equivalent date behavior. The
duplicate cases exercise candidate list mutation and ownership policy but do
not establish guest parity.
The refreshed 68000/020/040 receipt is
[`adddatatypes-morphos-segment-lifetime-20260925-v19`](../../../../artifacts/adddatatypes-morphos-segment-lifetime-20260925-v19/qualification.json)
and passes 41 supplied command vectors per CPU, plus one callback-wrapper
probe per CPU.
The v20 qualification separates the embedded-code stack by profile and adds
MorphOS Workbench-startup DTCD loading plus Workbench 3.1 CLI DTCD loading.
The receipt
[`adddatatypes-morphos-profile-stacks-20260925-v20`](../../../../artifacts/adddatatypes-morphos-profile-stacks-20260925-v20/qualification.json)
passes 42 supplied command vectors and one callback-wrapper probe per CPU on
resident 68000/020/040 HUNKs; the CLI and Workbench startup fixture paths assert
the MorphOS 32768-byte candidate. The separate classic receipt
[`adddatatypes-workbench31-native-20260925-v6`](../../../../artifacts/adddatatypes-workbench31-native-20260925-v6/qualification.json)
passes fifteen vectors per CPU, including the captured 4096-byte DTCD loader
argument. These are fixture checks, not authentic guest comparisons or proof
of the exact 2004 MorphOS target-header value.
Deeper matcher-mediated traversal and authentic guest confirmation of the
nonrecursive boundary, additional malformed DTHD combinations and Workbench
startup structures, plus guest confirmation of the startup lifecycle, original
guest comparison, private ABI confirmation,
PURE classification, licensing, and package admission also remain open. The earlier v2 receipt
remains invalidated because a failed requalification overwrote its 68000 HUNK.

This is an implementation slice, not a complete command or independent ABI
confirmation. The offset calculations and list semantics still need comparison
with both packed references and an authentic target guest. CLI lifecycle,
authoritative MorphOS DTHD/private ABI confirmation and guest registration
behavior, guest comparison of first-directory traversal, real DOS-loader callback execution
and authentic segment unload behavior, exact stack-size correspondence, guest
confirmation of REFRESH/date
behavior, Workbench guest parity, guest confirmation of both startup paths,
full ReadArgs behavior against original guests, PURE classification, and
package gates remain open.

## Required qualification gates

Before either profile can ship, qualify the resident 68000/68020/68040 entries
against at least:

1. ReadArgs ownership for empty, FILES, QUIET and REFRESH in Workbench, plus
   MorphOS LIST cases;
2. CLI and Workbench startup paths, including missing DOS and missing
   datatype/iffparse/utility/locale libraries;
3. empty, missing and malformed IFF descriptors, DTYP header bounds, DTCD
   load failure, failed `InternalLoadSeg`, plus guest confirmation of the
   already fixture-covered duplicate replacement and open-descriptor rules;
4. matcher break/error, exclusion, current-directory restoration and refresh
   date-scan precedence;
5. MorphOS sorted LIST output, exact result/IoErr/diagnostic precedence and
   interleaved callers;
6. same-segment resident reuse, semaphore ownership and zero shared-image
   writes; and
7. original Workbench and MorphOS guest comparisons plus installed PURE and
   package admission evidence.

Until those gates are recorded, ledger status remains partial/open and the
command contributes no shipping count.
