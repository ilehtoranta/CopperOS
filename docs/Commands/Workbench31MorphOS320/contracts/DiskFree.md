# DiskFree contract

Profile: morphos320. Goal steps: CC01 and CC11. Recorded: 2026-09-19.

The MorphOS 3.20 command is the 3,513-byte packed member at ISO logical block
175348, SHA-256
`59d7922c5e9a363b7ee9a222e430c19b3db985bf861cac68ea64539b2b61f60a`.
The published MorphOS command reference records the exact DOS template:

```text
VOLUME,NOPOSTFIX/S,PERCENT/S
```

`VOLUME` is optional and selects the volume to inspect; when omitted the
current volume is used. `NOPOSTFIX` selects a script-friendly byte count and
`PERCENT` selects free space as a percentage. The implementation uses public
DOS `Lock` and `Info`, keeps the `InfoData` and formatting cells in guest
memory, releases the lock before parser teardown, and avoids host filesystem
queries.

The native entry is
[`NativeMorphOSDiskFreeEntry.cs`](D:/D-drive/Koodit/GIT/CopperOS/tests/Commands.AddBuffersNativeRoot/NativeMorphOSDiskFreeEntry.cs),
and the command body is
[`NativeMorphOSDiskFreeCommand.cs`](D:/D-drive/Koodit/GIT/CopperOS/src/Commands/Native/NativeMorphOSDiskFreeCommand.cs).
The three-CPU static resident receipt is
`artifacts/diskfree-morphos-native-20260919-static-v3/qualification.json`.
The supplied-vector runtime receipt is
`artifacts/diskfree-morphos-native-20260919-runtime-v3/qualification.json`;
it passes fifteen invocations per CPU, including explicit/current volumes,
byte and percentage modes, large byte and overflow-safe percentage values,
parser/provider/allocation failures, startup boundaries, cleanup, and
interleaved callers. These receipts
prove resident ABI and bounded public-DOS ownership only. Exact localized
strings, large-volume/64-bit provider behavior outside the fixture, error
precedence, original guest differential, installed PURE/resident metadata,
lifecycle and package admission remain open.
