# DevList contract

Profile: `morphos320`. Goal step: CC18. Recorded: 2026-09-12.

The MorphOS 3.20 media contains `MorphOS/C/DevList` as version 50.3
(`27.11.04`), 1,817 bytes with SHA-256
`e7f65a418aa86ad9750df2922c9ad8330af9e47fe6cd04aedd46ff027f478018`.
The command has no options. Its AROS-derived implementation snapshots the
Exec device list while protected by `Forbid`, copies each device address,
name, version, revision, open count and flags into a growable public buffer,
prints the source header and rows, polls Ctrl-C after each row, then frees the
buffer. Allocation failure prints the device-buffer memory diagnostic and
returns `RETURN_FAIL`; Ctrl-C sets `ERROR_BREAK` and reports the fault after
cleanup. The private entry admits CLI startup only and opens DOS 37 for the
public boundary.

`src/Commands/Native/NativeMorphOSDevListCommand.cs` implements the bounded
DOS/Exec body. The private resident root is
`tests/Commands.AddBuffersNativeRoot/NativeMorphOSDevListEntry.cs`.

## Bounded native receipt

`tools/Commands/qualify_morphos_devlist_native_entry.ps1` compiles resident
68000/020/040 HUNKs and executes eleven supplied DOS/Exec vectors per CPU (33
total): empty, one-device and three-device lists, allocation failure, Ctrl-C,
missing DOS, Workbench and entry-boundary rejection, and interleaved repeat
calls. The durable receipt is
`artifacts/devlist-morphos-native-20260919-multinode/qualification.json`.

Each HUNK has five reachable methods, no managed allocation sites, runtime
helpers, external native targets, exception regions, or fatal machine-fault
sites, and the fixture reports no leaked resources or shared-image writes.
This is adapter evidence only: complete device-list population, exact
packed-binary/source correspondence, original guest parity, PURE/resident
reuse, package admission, and differential comparison remain open.
The AROS-derived source reference is not a license grant for a shipping
replacement; reuse and attribution review remain open.
