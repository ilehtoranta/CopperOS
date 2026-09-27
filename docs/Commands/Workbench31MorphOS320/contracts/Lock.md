# Lock contract

Profiles: `wb31` and `morphos320`. Goal step: CC19. Recorded: 2026-09-17.

The MorphOS 3.20 source archive identifies `Lock` as version 50.5
(`27.11.04`), an AROS-derived volume write-protection command. Its template
is:

```text
DRIVE/A,ON/S,OFF/S,PASSKEY
```

The command requires a device or root volume name, rejects a non-device/volume
DOS node with `ERROR_OBJECT_WRONG_TYPE`, and uses `GetDeviceProc` followed by
`ACTION_WRITE_PROTECT` through `DoPkt2`. `ON` supplies `DOSTRUE`; `OFF`
supplies `DOSFALSE`. An optional passkey is folded exactly as the source's
decimal digit loop (`key = key * 10 + character`). Successful operations print
`<drive> locked` or `<drive> unlocked`; packet failure prints the corresponding
attempt message and preserves the packet IoErr. Device-process and parser
state are released before DOS closes.

`src/Commands/Native/NativeMorphOSLockCommand.cs` implements that public DOS
boundary. The private resident root is
`tests/Commands.AddBuffersNativeRoot/NativeMorphOSLockEntry.cs`.

The Workbench 3.1 Disk 1/2 HUNK has the same syntax candidate:
`DRIVE/A,ON/S,OFF/S,PASSKEY`. `NativeWorkbench31LockCommand` keeps a separate
profile and `Workbench31LockEntry` opens DOS 36. It shares the bounded
public-DOS packet body only as an implementation candidate; no classic output,
packet or result parity is inferred from the MorphOS source.

## Bounded native receipt

`tools/Commands/qualify_morphos_lock_native_entry.ps1` compiles resident
68000/020/040 HUNKs and executes eleven supplied DOS vectors per CPU (33 total):
ON/OFF/no-action, passkey conversion, wrong node type, missing device, packet
failure, parser failure, Workbench startup, and interleaved repeat calls. The
durable receipt is
`artifacts/lock-morphos-native-20260912-qualified/qualification.json`.

Each HUNK has one resident root, ten reachable methods, no managed allocation
sites, runtime helpers, external native targets, exception regions, fatal
machine-fault sites, leaked allocations, or shared-image writes. This remains
adapter evidence only: original guest handler behavior, PURE/resident
classification, same-segment lifecycle, package admission, licensing review,
and differential comparison remain open.

The Workbench candidate is qualified separately by
`tools/Commands/qualify_workbench31_lock_native_entry.ps1`; receipt
`artifacts/lock-wb31-native-20260917-qualified-v2/qualification.json` records
eleven supplied-DOS invocations on each resident 68000/020/040 HUNK. This is
syntax-candidate evidence only and leaves the original guest, lifecycle, PURE,
packaging and differential gates open.
