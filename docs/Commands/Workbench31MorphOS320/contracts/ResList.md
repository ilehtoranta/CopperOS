# ResList contract

Profile: `morphos320`. Goal step: CC18. Recorded: 2026-09-12.

The MorphOS 3.20 source archive identifies the AROS-derived `ResList` command
as version 50.4 (`14.3.15`). The archived `c/reslist/reslist.c` is 2,454 bytes
with SHA-256
`1e090709e912f5337e22e67e0f77bc03d8f2bb8863f36915da9b7664ddd59922`.
The command has no options. It opens DOS 37, snapshots the Exec resource list
under `Forbid`, copies resource addresses and names into a growable public
buffer, prints the source table header and rows through `VPrintf`, polls
Ctrl-C after each row, releases the buffer, reports `ERROR_BREAK` on
interruption, and closes DOS. A buffer allocation failure prints the source
memory message and returns `RETURN_FAIL`.

`src/Commands/Native/NativeMorphOSResListCommand.cs` implements this public
Exec/DOS boundary. The private resident root is
`tests/Commands.AddBuffersNativeRoot/NativeMorphOSResListEntry.cs`.

## Bounded native receipt

`tools/Commands/qualify_morphos_reslist_native_entry.ps1` compiles resident
68000/020/040 HUNKs and executes eleven supplied DOS/Exec vectors per CPU (33
total): empty, one-resource and three-resource lists, Ctrl-C, allocation
failure, missing DOS, Workbench and entry-boundary rejection, and interleaved
repeat calls. The durable receipt is
`artifacts/reslist-morphos-native-20260919-multinode-v2/qualification.json`.

Each HUNK has five reachable methods, no managed allocation sites, runtime
helpers, external native targets, exception regions, or fatal machine-fault
sites, and the fixture reports no leaked resources or shared-image writes.
This is adapter evidence only: complete resource-list population, original
guest parity, PURE/resident reuse, package admission, and differential
comparison remain open.
