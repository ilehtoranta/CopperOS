# LibList contract

Profile: `morphos320`. Goal step: CC18. Recorded: 2026-09-12.

The MorphOS 3.20 media contains `MorphOS/C/LibList` as version 50.6
(`7.12.2016`), 2,625 bytes with SHA-256
`98af316cdec7f20b6cdf3180fcfb897562485d4ca723ccb8f84493f35e383c0f`.
The command has no options. Its AROS-derived implementation snapshots the
Exec library list while protected by `Forbid`, copies each library address,
name, version, revision, open count and flags into a growable public buffer,
prints the source header and rows, polls Ctrl-C after each row, then frees the
buffer. Allocation failure reports the library-buffer memory diagnostic,
sets `ERROR_NO_FREE_STORE`, and returns `RETURN_FAIL`; Ctrl-C reports
`ERROR_BREAK` after cleanup. The private entry admits CLI startup only and
opens DOS 37 for the public boundary.

`src/Commands/Native/NativeMorphOSLibListCommand.cs` implements the bounded
DOS/Exec body. The private resident root is
`tests/Commands.AddBuffersNativeRoot/NativeMorphOSLibListEntry.cs`.

## Bounded native receipt

`tools/Commands/qualify_morphos_liblist_native_entry.ps1` compiles resident
68000/020/040 HUNKs and executes eleven supplied DOS/Exec vectors per CPU (33
total): empty, one-library and three-library lists, allocation failure, Ctrl-C,
missing DOS, Workbench and entry-boundary rejection, and interleaved repeat calls. The durable
receipt is
`artifacts/liblist-morphos-native-20260919-multinode-v2/qualification.json`.

Each HUNK has five reachable methods, no managed allocation sites, runtime
helpers, external native targets, exception regions, or fatal machine-fault
sites, and the fixture reports no leaked resources or shared-image writes.
This is adapter evidence only: complete library-list population, exact
packed-binary/source correspondence, original guest parity, PURE/resident
reuse, package admission, and differential comparison remain open.
The AROS-derived source reference is not a license grant for a shipping
replacement; reuse and attribution review remain open.
