# DiskChange contract

Profiles: `wb31` and `morphos320`. Goal step: CC19. Recorded: 2026-09-17.

The MorphOS 3.20 source archive identifies `DiskChange` as version 50.3
(`9.8.2014`). The archived `c/diskchange/diskchange.c` is 1,658 bytes with
SHA-256 `b13c779f5c92d0113f63d205a525a59ed689ff97ccf367910a6ca5cec7791068`.
Its template is:

```text
DEVICE/A
```

The command opens DOS 50, parses the required device name, obtains a device
process with `DeviceProc`, and sends `ACTION_INHIBIT` through `DoPkt`: first
with `DOSTRUE`, then with `DOSFALSE` only when the inhibit succeeds. Device
lookup failure reports `error while searching the device`; an inhibit failure
reports `error while inhibiting the device`. Parser failure reports the
`DiskChange` fault header. The command releases `ReadArgs` storage before
closing DOS; the source does not call `FreeDeviceProc` for the returned
filesystem port. No Workbench message path is present in the source.

`src/Commands/Native/NativeMorphOSDiskChangeCommand.cs` implements this public
DOS-vector boundary. The private resident root is
`tests/Commands.AddBuffersNativeRoot/NativeMorphOSDiskChangeEntry.cs`.

The Workbench 3.1 Disk 1/2 HUNK exposes the same `DEVICE/A` syntax candidate.
`NativeWorkbench31DiskChangeCommand` keeps a separate DOS 36 startup entry and
shares the public `DeviceProc`/`ACTION_INHIBIT` body only as a bounded
implementation candidate. The classic packet ordering, diagnostics and
return-level behavior still require an original Workbench capture.

## Bounded native receipt

`tools/Commands/qualify_morphos_diskchange_native_entry.ps1` compiles resident
68000/020/040 HUNKs and executes ten supplied DOS vectors per CPU (30 total):
success, second-packet handling, missing device, inhibit failure, parser
failure, Workbench startup rejection, invalid entry boundaries, and
interleaved repeat calls. The durable receipt is
`artifacts/diskchange-morphos-native-20260912-qualified/qualification.json`.

Each HUNK has nine reachable methods, no managed allocation sites, runtime
helpers, external native targets, exception regions, or fatal machine-fault
sites, and the fixture reports no leaked resources or shared-image writes.
This is adapter evidence only: original guest handler transitions, PURE and
resident lifecycle, package admission, licensing review, and differential
comparison remain open.

The Workbench candidate is qualified by
`tools/Commands/qualify_workbench31_diskchange_native_entry.ps1`; receipt
`artifacts/diskchange-wb31-native-20260920-boundaries/qualification.json`
records eleven supplied-DOS invocations on each resident 68000/020/040 HUNK,
including Workbench startup and missing-DOS boundaries.
This remains syntax-candidate evidence only and does not close classic guest
parity, PURE/resident lifecycle, packaging or differential gates.
