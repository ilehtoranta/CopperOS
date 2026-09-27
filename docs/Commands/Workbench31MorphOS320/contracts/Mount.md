# MorphOS 3.20 `Mount` contract

Profile: `morphos320`. Goal steps: CC00, CC01, CC07, CC19. Recorded:
2026-09-19. Status: **source-bound contract; explicit `FROM` native slice
implemented; full profile remains open**.

## Reference identity

The MorphOS 3.20 ISO contains `MorphOS/C/Mount`, version `Mount 50.13
(24.04.17)`, 16,078 bytes, SHA-256
`2155ad37d85f1712fefdc6dfbd3ee4456b31634f96a579022da8365b703558b1`.
The inventory records the packed identity and version tag, but no executable
template or runtime capture has been obtained. The installed PURE flag is also
unresolved.

The official partial source archive contains an AROS-derived `c/mount/mount.c`
(56,885 bytes, SHA-256
`d08baa374890939e43ebca09e141787acc2d5a7fd7efc8724bc82f1229f5a081`). Source
availability is behavior evidence only; it does not prove correspondence,
reuse permission, or compatibility with the packed MorphOS member.

## Source-observed command grammar

The outer command entry calls DOS `ReadArgs` with exactly:

```text
DEVICE/M,FROM/K,DEBUG/S
```

`DEVICE/M` accepts multiple device names or mount-file patterns. A name ending
in `:` is treated as a DOS device name; the source searches the device-driver
locations `DEVS:DOSDrivers/`, `MOSSYS:DEVS/DOSDrivers/`,
`SYS:Storage/DOSDrivers/`, and `MOSSYS:Storage/DOSDrivers/`, then falls back to
`DEVS:MountList`. A non-device argument is matched with `MatchFirst`/
`MatchNext` and each matching file is processed. `FROM/K` selects an explicit
mount-list source. `DEBUG/S` enables the source's diagnostic `Printf` paths;
these debug strings are not yet proven command output in the packed binary.

Mount-file and mount-list records are parsed with a second source-observed
`ReadArgs` template:

```text
HANDLER/K,EHANDLER=FILESYSTEM/K,DEVICE/K,UNIT/K,FLAGS/K,
SECTORSIZE=BLOCKSIZE/K,SURFACES/K,
SECTORSPERTRACK=BLOCKSPERTRACK/K,SECTORSPERBLOCK/K,RESERVED/K,
PREALLOC/K,INTERLEAVE/K,LOWCYL/K,HIGHCYL/K,BUFFERS/K,BUFMEMTYPE/K,
MAXTRANSFER/K,MASK/K,BOOTPRI/K,DOSTYPE/K,BAUD/K,CONTROL/K,
STACKSIZE/K,PRIORITY/K,GLOBVEC/K,STARTUP/K,MOUNT=ACTIVATE/K,FORCELOAD/K
```

The aliases above are source observations, not a claim that the packed
MorphOS binary accepts every spelling. `MOUNT=ACTIVATE/K` supplies the
activation value; `FORCELOAD/K` controls whether the handler BSTR is retained
on the published node. The source initializes
stack size to 8192, priority to 5, and global vector to -1 before applying
record values. It rejects boot priorities outside -128..127, global vectors
outside -3..-1, and numeric startup values above 255.

## Provider inventory

The required Kickstart declarations are present in the pinned sibling SDK:
`Amiga.Expansion.MakeDosNode`, `Amiga.Expansion.AddDosNode`, and the
`DosEnvec`, `FileSysStartupMsg`, and `DeviceNode` layouts in `Sdk.Amiga`. The
current source identities are:

| Provider input | Bytes | SHA-256 | Required use |
| --- | ---: | --- | --- |
| `Sdk.Amiga/Expansion/Expansion.cs` | 4460 | `55883eb64952ceaddde8ad60dbdfad08c2eca0790ae38dfa780497256613afa3` | Open `expansion.library` at the verified V33 capability floor and call `MakeDosNode`/`AddDosNode` through public vectors; the captured V37 request remains differential evidence |
| `Sdk.Amiga/Expansion/ExpansionLvo.cs` | 652 | `f4873a376dcd0c115856d09c477480c711dca4a9cc86c85a96096419228b04b` | Bind the Kickstart 3.1 LVOs without a host shortcut |
| `Sdk.Amiga/DOS/Structures.cs` | 15366 | `b2e7f805c546f9fe2b5981c4a848ff02fd60e815b4b4ce8ea02063e4f9fe90dc` | Encode the source-owned startup and device records |

These declarations are not yet a production Mount provider. CopperStart's
current Expansion parity root only exercises a compatibility vector, while the
CopperMod boot host's generic Expansion trap returns a synthetic object. The
missing work package is the real DOS-node publication path: one owned
`MakeDosNode`/`AddDosNode` transaction, rollback on BSTR or validation
failure, and a guest-visible list/device provider that can be used by the
command fixture and CopperStart integration. This is an API integration gap,
not permission to substitute a host filesystem.

The provider boundary now has a managed DOS-list path. `ExpansionCore`
dispatches `MakeDosNode` and `AddDosNode` through dedicated platform methods.
CopperMod consumes the classic five-long packet, allocates a DOS-owned
`FileSystemContext`, copies the required node-name C string and the optional
device C string into resident BSTR storage (an omitted device becomes an
empty startup-device BSTR), preserves `DosEnvec`, unit, and startup flags, and
publishes the resulting `DeviceNode` through `DosListCore`. Malformed packets
still fail closed; the former synthetic compatibility object is never returned
for these vectors. Focused CopperStart regressions cover creation, exact node
fields, list publication, duplicate rollback, and the omitted-device case.

The native-DOS path now has an outer boot boundary. When a native DOS image is
installed, CopperMod routes the same node through the public native
`AddDosEntry` vector and frees the DOS-owned node on a failed publication; when
no native image is installed, it uses the managed `DosListCore` path. The
native call remains guarded by the bootstrap re-entry/fault state and therefore
fails closed if the current guest call cannot be safely retained. Native
duplicate/conflict behavior, late handler retirement, and a real Mount guest
still require differential evidence before shipping admission.

## Source-observed effects and ownership

The CLI path opens `dos.library`, `utility.library`, and `expansion.library`
at version 37. A Workbench startup path consumes startup arguments and restores
the caller's current directory after each mount file. CLI processing owns the
outer `RDArgs` until `FreeArgs`; each device operation allocates a cleared
public parameter block with `AllocVec` and releases it after processing.

The implementation builds a `DosEnvec`, optional `FileSysStartupMsg`, and
`DeviceNode`, copies handler/startup text into public BSTRs, sets stack,
priority, global vector, startup and activation fields, and publishes the
node with `AddDosNode`. It checks for an already-mounted device before adding
the node. It must call `MatchEnd` for every `MatchFirst` traversal, release
temporary handler/control strings on every failure path, close owned libraries,
and reply to a Workbench startup message.

Observed source failure categories include `ERROR_NO_FREE_STORE`,
`ERROR_OBJECT_EXISTS`, `ERROR_OBJECT_NOT_FOUND`, `ERROR_INVALID_RESIDENT_LIBRARY`,
`ERROR_BAD_NUMBER`, `ERROR_BREAK`, and `ERROR_NO_MORE_ENTRIES`. The source
prints `Mount` faults for ordinary DOS errors and emits explicit diagnostics
for invalid boot priority, global vector, startup range, missing handler/
`DosType`, and failed device publication. Exact output, `IoErr`, and result
precedence still require packed-binary and guest captures.

## Implementation boundary and remaining work

The first native slice is now present in
[`NativeMorphOSMountCommand.cs`](D:/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSMountCommand.cs)
with the resident entry
[`NativeMorphOSMountEntry.cs`](D:/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSMountEntry.cs).
It accepts the exact outer and inner `ReadArgs` templates for an explicit
`FROM` mount-list source, constructs the five-long `MakeDosNode` packet and
`DosEnvec` in guest memory, copies the handler BSTR, applies the observed
stack/priority/global-vector/startup values, publishes through public
`Expansion.MakeDosNode`/`AddDosNode`, and releases the node, BSTRs and
temporary buffers on failed publication. Three resident HUNK compilations are
recorded in
`artifacts/mount-morphos-native-20260921-v2/qualification.json`.
The bounded CLI path now accepts every direct `DEVICE/M` value without
`FROM/K`: a non-device argument is probed as a mount file, while a trailing
colon is stripped and searched in the five source-observed locations (current
directory, the four DOSDrivers directories) with public DOS `Open`/`Close`
ownership. Each vector member is reopened and released in order; the resolved
filename supplies the node name through DOS `FilePart`. If all five driver
locations miss, the path now reads `DEVS:MountList` into an invocation-owned
guest buffer, applies the source `preparefile` normalization, selects the
case-insensitive `DEVICE:` block with public `ReadItem`, and passes the
selected source cursor to the inner `ReadArgs` parser. Non-device values now
use an invocation-owned `AnchorPath` with public `MatchFirst`/`MatchNext`/
`MatchEnd`, processing every matched file and clearing the normal
`ERROR_NO_MORE_ENTRIES` boundary. If the mount file is absent, the native
slice now opens `icon.library` v37, resolves the corresponding DiskObject,
filters comment and `IM?=` tool types, aggregates the eligible tool text into
one guest-owned `ReadArgs` source, and publishes one node from the resulting
record. This is statically qualified; exact icon-library/runtime behavior is
still a guest gate.
`MOUNT/ACTIVATE` is translated only to the public
`ADNF_STARTPROC` AddDosNode flag, while `DosEnvec.de_BootBlocks` remains zero
as in the source. Handler and string `STARTUP` values are copied into
invocation-owned public BSTRs before `DeviceNode.dn_Handler`/`dn_Startup` are
published; those BSTRs are freed on any failed publication and retained with a
successful node. `DEVICE` is copied as a C string, numeric-or-string
`UNIT`/`FLAGS` retain their source representation, and `CONTROL` is copied as
the `DosEnvec` BSTR pointer before the parser lease is released.
The environment also starts from the source defaults (`512` byte sectors,
two surfaces, one sector per block, eleven blocks per track, two reserved
blocks, cylinder 79, twenty buffers, public buffer type 1, `0x7fffffff`
maximum transfer, `0xfffffffe` mask, and baud 1200) before applying record
overrides.
The resident root uses the public Expansion transaction and raw guest memory,
so the receipt has zero managed runtime features, helpers, external targets,
exception regions, and fatal fault sites. This is a pure/resident static
checkpoint, not a runtime or shipping receipt.

The next bounded runtime gate is recorded in
`artifacts/mount-morphos-native-entry-20260921-v2/qualification.json`. Its
resident 68000/020/040 HUNKs execute five supplied invocations per CPU: the
exact outer `DEVICE/M,FROM/K,DEBUG/S` ReadArgs call with cleared three-slot
result storage, an empty `DEVICE/M` vector no-op, parser failure, result
allocation failure, and repeat/interleaved cleanup. The receipt proves DOS
startup, result/RDArgs ownership, IoErr preservation and image/resource
cleanup in the native entry. It deliberately does not claim real
DOSDrivers/MountList, icon, Expansion or handler/provider execution, Workbench
startup behavior, PURE eligibility, original-guest differential behavior, or
package admission.

The eventual full implementation must use public Kickstart 3.1 DOS/Exec/
Expansion APIs and keep all parser, `DosEnvec`, `DeviceNode`, BSTR,
`AddDosNode`, matcher, and library/message ownership in invocation-local guest
storage. It must not replace device-handler behavior with a host filesystem
shortcut.

Before a profile can ship, capture the packed MorphOS template/help and
diagnostics, establish source-to-binary correspondence and licensing, add the
required expansion/filesystem provider work package, implement Workbench and
MorphOS bodies as separate profile entries, qualify 68000/020/040 resident and
PURE behavior, exercise real mount-list/device-handler guests and failure
paths, and stage the correct package placement. Until then both `CC19.Mount`
profiles remain open in the completion ledger.

## Workbench 3.1 profile

The selected Workbench 3.1 `C/Mount` member is 6,880 bytes, version
`Mount 40.4 (27.9.93)`, SHA-256
`f47fa83e2efe3af8313b93f01ca79ede9999c62d0b5d7bfb97110e60b44de948`.
The outer template is exactly:

```text
DEVICE/M,FROM/K
```

The inner template is present in the packed member at byte offset 6,396 and
includes its leading comma:

```text
,SECTORSIZE=BLOCKSIZE,,SURFACES,SECTORSPERBLOCK,
SECTORSPERTRACK=BLOCKSPERTRACK,RESERVED,PREALLOC,INTERLEAVE,
LOWCYL,HIGHCYL,BUFFERS,BUFMEMTYPE,MAXTRANSFER,MASK,BOOTPRI,
DOSTYPE,BAUD,CONTROL,DEVICE,UNIT,FLAGS,HANDLER,STACKSIZE,
PRIORITY,GLOBVEC,FILESYSTEM,STARTUP,ACTIVATE=MOUNT,EHANDLER,
FORCELOAD
```

The generated inventory now records both this candidate and the outer template
with their byte offsets; both remain syntax-only observations until a parser
call is captured in an original guest.

This grammar has 31 result slots. The shared native transaction uses the
captured Workbench order explicitly: handler 22, error handler 29, device 19,
unit 20, flags 21, sector size 1, surfaces 3, sectors per block 4, blocks per
track 5, reserved 6, preallocation 7, interleave 8, low/high cylinder 9/10,
buffers 11, buffer memory type 12, maximum transfer 13, mask 14, boot priority
15, DOS type 16, baud 17, control 18, stack/priority 23/24, global vector 25,
filesystem 26, startup 27, activation 28, error handler 29, and force-load 30.
The empty slots at 0 and 2 are preserved by the result count and are not
repurposed. The shared transaction accepts `HANDLER`, then Workbench's
`FILESYSTEM`, then `EHANDLER` as the handler-name source; MorphOS keeps its
`EHANDLER=FILESYSTEM` alias at slot 1.

The profile entry is
[`NativeWorkbench31MountCommand.cs`](D:/D-drive/GIT/CopperOS/src/Commands/Native/NativeWorkbench31MountCommand.cs),
with resident startup in
[`NativeWorkbench31MountEntry.cs`](D:/D-drive/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeWorkbench31MountEntry.cs).
The three-CPU static receipt is
`artifacts/mount-wb31-native-20260919-wildcard/qualification.json`; it proves
resident ABI lowering, the shared `DEVS:MountList` parser path, bounded
`icon.library` `.info` aggregation, and the zero-allocation/static
compatibility boundary for this profile. It does not yet prove complete
Workbench source selection, exact icon-library/runtime behavior, handler and
media behavior, startup-message lifecycle, failure precedence, PURE
eligibility, original-binary differential behavior, or package placement.

The separate outer-entry runtime receipt
`artifacts/mount-wb31-native-entry-20260921-v5/qualification.json` now covers
nine supplied invocations per CPU on resident 68000/020/040 HUNKs: exact
`DEVICE/M,FROM/K` parsing with cleared two-slot result storage, an empty
`DEVICE/M` no-op, parser failure, result allocation failure, and
repeat/interleaved cleanup, plus empty and single-argument Workbench startup
messages that return before CLI parsing, a missing startup argument list, and a
startup source-not-found path that exercises argument-list iteration,
`CurrentDir` save/restore, `FilePart`, Expansion cleanup and icon fallback
failure. It proves DOS36 startup, result/RDArgs ownership,
IoErr preservation, startup reply ordering and resource/image cleanup only;
successful Workbench startup source processing, real source/provider behavior, PURE,
differential and package gates remain open.





