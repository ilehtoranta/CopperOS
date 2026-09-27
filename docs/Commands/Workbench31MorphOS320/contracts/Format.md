# MorphOS 3.20 `Format` contract

Profile: `morphos320`. Goal steps: CC00, CC01, CC07, CC09, CC21. Recorded:
2026-09-21. Status: **bounded native trackdisk slice plus a synthetic
disposable-media resident boundary fixture; no shipping qualification**.

## Reference identity

The MorphOS 3.20 ISO contains `MorphOS/C/Format`, version `Format 50.9
(27.11.04)`, 10,768 bytes, SHA-256
`8a0b237d218a4836ca9b27a1d5b0ae2a876e746d5775e65363bce12f715e7e5f`.
Its packed template, help text and runtime behavior have not yet been
captured. The installer's PURE/resident classification is unresolved.

The partial source archive contains an AROS-derived `c/format/format.c`,
24,895 bytes, SHA-256
`2176577ed2c52ec8e5989a1681645250eb2033bbaea6997052dcf9ae65a439b3`, plus
`format_version.h` identifying the same `Format 50.9` version. Source
availability does not prove packed correspondence, licensing, or a safe
replacement for a destructive disk command.

## Source-observed grammar

The command calls DOS `ReadArgs` with this template:

```text
DEVICE=DRIVE/A/K,NAME/A/K,OFS/S,FFS/S,SFS/S,MSDOS/S,
INTL=INTERNATIONAL=CASESENSITIVE/S,NOINTL=NOINTERNATIONAL/S,
DIRCACHE/S,NODIRCACHE/S,LNFS=LONGFILENAMES/S,NOLNFS=NOLONGFILENAMES/S,
NOICONS/S,QUICK/S,NORECYCLED/S,SHOWRECYCLED/S
```

`DEVICE` and `NAME` are required keyword arguments. `OFS`, `FFS`, `SFS`, and
`MSDOS` select the filesystem family. `INTL`/`NOINTL`, `DIRCACHE`/`NODIRCACHE`,
and `LNFS`/`NOLNFS` select mutually interacting filesystem flags. `QUICK`
requests handler-side quick formatting; `NOICONS` suppresses icon creation;
`NORECYCLED` and `SHOWRECYCLED` affect the SFS format tags. The source gives
the existing device DosEnvec type precedence over selector defaults and uses
FFS as the default classic DOS type when no selector is supplied.

The source rejects banned system device/volume names through the DOS 50+
banned-name lists when available, otherwise using the fixed `MOSSYS`, `SYS`,
`L`, `DEVS`, `LIBS`, `S`, and `C` list. A rejected name sets
`ERROR_INVALID_COMPONENT_NAME` and prints the localized failure heading.

## Source-observed lifecycle and effects

The entry opens `dos.library` and `utility.library` v37, optionally opens
`locale.library` v37 for the format catalog, parses through `ReadArgs`, and
closes all owned libraries after `FreeArgs`. It locks the device DOS list,
resolves the requested device, obtains its `FileSysStartupMsg` and `DosEnvec`,
then unlocks before opening the underlying device.

The interactive path prints an insert-disk prompt, requires an interactive
input stream, waits for confirmation, inhibits the handler with
`ACTION_INHIBIT`, opens the device through an owned message port and
`IOStdReq`, formats and verifies every cylinder, performs quick or full
initialization, waits for `ACTION_DISK_INFO` validation, optionally creates
icons, and uninhibits the handler. Full formatting uses `TD_FORMAT`/`CMD_READ`
or 64-bit `CMD_FORMAT64`/`CMD_READ64` when the partition exceeds 4 GiB.
Successful writes are flushed with `CMD_UPDATE`/`CMD_CLEAR`, then the motor is
stopped and the I/O request, signal, and port are released.

`Ctrl-C` returns `WARN` with `ERROR_BREAK` and drains pending input. Device,
allocation, handler, validation, checksum, and I/O failures print localized
faults and preserve the source result/`IoErr` policy. The source contains
explicit TODOs for write-protect detection, retries, and manual FFS
initialization; those must remain visible rather than being silently filled in.

## Provider inventory

The required device ABI is already declared by the pinned SDK. The
`Amiga.TrackDiskDevice` declarations expose the classic and TD64 command
numbers, `IOExtTD`, and `DriveGeometry`; the current source identity is 2717
bytes with SHA-256
`c67666b43d5561d9ccd87ec77bcc946488c7dffd24dd907c19c6b932a92b18dc`.
CopperStart also contains a guest-owned `TrackDiskDeviceCore` (20,543 bytes,
SHA-256
`85890595cf979a6b1cf59d6d4d878e9296d8f2bc0ecabc11a5abe699d154116e`) with
read/write/format, protection, geometry, change-state, and TD64 dispatch
surfaces.

Those are reusable provider inputs, not yet a command qualification path. The
new native slice uses the public `trackdisk.device` ABI and the existing
`TrackDiskDeviceCore` boundary. Portable-core tests now prove a full
classic-track `TD_FORMAT` write/readback plus TD64 high/low-offset delegation
through a sparse extended-media boundary. The installed AmigaBus
`TrackdiskDeviceServices` adapter now accepts an optional 64-bit media callback;
its focused tests cover both the classic-track path and TD64 high/low
format/readback. Providers that do not supply that callback still fail closed.
The CopperOS command fixture now publishes a synthetic DOS handler plus
trackdisk queue to the resident `Format` entry. It exercises inhibition,
message-port/IO request ownership, full-format write/readback, quick-format
handler initialization, disk validation, update/clear, motor stop, and
balanced cleanup against in-memory disposable media. This is a boundary
receipt only: the production CopperMod boot path is not yet bound to a real
DOS handler/media queue.

The Expansion boundary used by the related Mount path is now explicitly
fail-closed for `MakeDosNode`/`AddDosNode`; its dedicated MC68000 parity test
proves that those vectors no longer return a generic compatibility object. The
trackdisk service remains installed for device-level I/O; the synthetic queue
now exercises the complete inhibit/format/readback lifecycle without claiming
production or original-guest media behavior.

## Native implementation slice

`src/Commands/Native/NativeMorphOSFormatCommand.cs` and
`tests/Commands.AddBuffersNativeRoot/NativeMorphOSFormatEntry.cs` now provide a
resident entry using the exact source template and `ReadArgs` ownership. The
slice resolves the DOS device and `DosEnvec`, applies the source selector
precedence and banned-name checks, prompts and handles Ctrl-C, inhibits and
uninhibits the handler, owns the message port/`IOStdReq`, performs classic
`TD_FORMAT`/`CMD_READ` track write/read verification, issues update/clear/motor
cleanup, and preserves source result/`IoErr` mapping. SFS packet tag storage and
quick-format dispatch, source-style `Disk`/`Trashcan` icon creation through
`icon.library` v37, and source-style TD64 command selection for partitions
whose byte offset exceeds 32 bits are present behind the same boundary. The
first write block
uses the public `ID_UNREADABLE_DISK` marker (`'BAD\0'`, `0x42414400`).

The resident HUNK compiles with `IsCompatible=true` for MC68000, MC68020 and
MC68040. The static reports are in
`artifacts/format-morphos-native-dev/format-68000.hunk.static.json` (and the
020/040 siblings); each reports 30 reachable methods, one runtime feature, no
runtime helpers, external native targets or exception regions, and ten
machine-fault sites. These are development receipts, not shipping artifacts.

## Synthetic disposable-media resident boundary fixture (2026-09-21)

`tests/Commands.NativeExecution/FormatEntrySuite.cs` and
`tools/Commands/qualify_morphos_format_native_entry.ps1` exercise the resident
entry with the exact source `ReadArgs` template on MC68000, MC68020, and
MC68040. The receipt
`artifacts/format-morphos-native-entry-20260921-v4/qualification.json` records
ten supplied vectors per CPU (30 total): startup and missing-DOS guards,
parser/result-allocation failures, banned system name, missing device,
repeat/interleaving, and full/quick formatting through a synthetic DOS-handler
and trackdisk queue. Full formatting proves one classic format write and
readback plus update/clear/motor cleanup; quick formatting proves the handler
path without trackdisk media I/O. The fixture verifies `ReadArgs`/`FreeArgs`,
all invocation and fixture allocations, DOS-list lock/find/unlock, handler
inhibition and validation, device I/O ownership, diagnostics/`IoErr`, balanced
cleanup, and zero shared-image writes.

The receipt does not claim original-guest parity, production handler/provider
binding, icon/SFS behavior beyond the exercised `NOICONS` path, TD64 media,
PURE admission, or package/image admission. Those gates remain open below.

This command is destructive and cannot be qualified with a host filesystem
substitute. The synthetic media fixture is only a resident ABI checkpoint;
admission still requires a real disposable guest media run.

Before shipping, capture the packed template/help/catalog strings and exact
diagnostics, establish source-to-binary correspondence and licensing, bind the
production device/trackdisk and icon providers, add TD64 media above 4 GiB, and
exercise SFS/handler validation. Then qualify 68000/020/040 resident and
PURE behavior, run destructive operations only in disposable original and
replacement guests, and stage package/image metadata. Until those gates pass,
`CC21.Format.morphos320` remains partial and all executable/admission gates
remain open.
