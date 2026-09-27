# PortList contract

Profile: `morphos320`. Goal step: CC18. Recorded: 2026-09-12.

The MorphOS 3.20 media contains `MorphOS/C/PortList` as version 50.2
(`27.11.04`), 1,921 bytes with SHA-256
`d6f64e2181a43b0a19990eabcbce6c92f704b0c01b1021ee6543d2005c9c4485`.
The corresponding source member is `c/portlist/portlist.c` in the official
3.20 C source archive; the extracted source is 3,950 bytes with SHA-256
`0dbf323a44b28c9d4c71543c95f2c0c2a3c40cbd10371e0d2a474963987f9894`.
The AROS-derived body has no options. It opens DOS 37, grows a public buffer
in 2,048-byte steps, snapshots Exec's message-port list under `Forbid`, copies
port and signal-task names into the same buffer, prints the address/name/signal/
task/task-name table, polls Ctrl-C after each row, frees the buffer, and reports
the selected fault after cleanup. Allocation failure preserves the source
failure path and reports the current DOS error; Ctrl-C sets `ERROR_BREAK`.

`src/Commands/Native/NativeMorphOSPortListCommand.cs` implements the bounded
DOS/Exec body. The private resident root is
`tests/Commands.AddBuffersNativeRoot/NativeMorphOSPortListEntry.cs`.

## Bounded native receipt

`tools/Commands/qualify_morphos_portlist_native_entry.ps1` compiles resident
68000/020/040 HUNKs and executes eleven supplied DOS/Exec vectors per CPU (33
total): empty, one-port and three-port snapshots, allocation failure, Ctrl-C,
missing DOS, Workbench and entry-boundary rejection, and interleaved repeat
calls. The durable receipt is
`artifacts/portlist-morphos-native-20260919-multinode-v2/qualification.json`.

Each HUNK has five reachable methods, no managed allocation sites, runtime
helpers, external native targets, exception regions, or fatal machine-fault
sites, and the fixture reports no leaked resources or shared-image writes.
The fixed header bytes are source-equivalent; the native lowering emits that
header through `FPuts` while retaining the source `VPrintf` row ABI. This is
adapter evidence only: complete port-list population, exact packed-binary/
source correspondence, original guest parity, PURE/resident reuse, package
admission, and differential comparison remain open. The AROS-derived source
reference is not a license grant for a shipping replacement; reuse and
attribution review remain open.
