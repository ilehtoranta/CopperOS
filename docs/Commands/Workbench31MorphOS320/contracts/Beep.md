# Beep contract

Profile: `morphos320`. Goal step: CC23. Recorded: 2026-09-12.

The MorphOS 3.20 source archive identifies `Beep` as version 50.2. The source
has no command options or DOS parser. It opens `intuition.library` at minimum
version 33, calls `DisplayBeep(NULL)`, closes the owned library, and returns
`RETURN_OK`. If the library cannot be opened it returns `RETURN_FAIL` without
emitting command output or inventing a DOS error.

`src/Commands/Native/NativeMorphOSBeepCommand.cs` implements this boundary
using the public Exec and Intuition vectors. The private resident root is
`tests/Commands.AddBuffersNativeRoot/NativeMorphOSBeepEntry.cs`; command-tail
bytes are ignored as in the source `main(void)` entry.

## Bounded native receipt

`tools/Commands/qualify_morphos_beep_native_entry.ps1` compiles the root as a
resident HUNK for 68000/020/040 and runs five supplied Exec/Intuition vectors
per CPU: success, ignored command arguments, Workbench startup, library-open
failure, and repeat use. The durable receipt is
`artifacts/beep-morphos-native-20260912-qualified/qualification.json`.

Each HUNK has four reachable methods, no managed allocation sites, runtime
helpers, external native targets, exception regions, or fatal machine-fault
sites. The fixture verifies the v33 open, `DisplayBeep(NULL)`, close ordering,
open-failure result, repeat behavior, and zero DOS-parser/guest-allocation use.
This is adapter evidence only: original guest audio behavior, PURE/resident
classification, same-segment lifecycle, package admission, licensing review,
and differential comparison remain open.
