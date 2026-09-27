# Touch contract

Profile: `morphos320`. Goal step: CC14. Recorded: 2026-09-12.

The MorphOS 3.20 C member is `c/touch/touch.c`, 6916 bytes, SHA-256
`ecd885e0ea573b5503b9ac044936985037fcc02b0c7ddb0efcfb386adc0fe0f`. Its
version include records `Touch 50.9 (17.08.2025)` and the AROS-derived
implementation. The source template is:

```text
NAME/A/M,VERBOSE/S,ALL/S
```

`NAME/M` processes every supplied name. Each name is first passed through
`MatchFirst`/`MatchNext`; a literal that returns `ERROR_OBJECT_NOT_FOUND` is
then touched directly. `ALL` requests AnchorPath directory descent and touches
directories on ascent; without `ALL`, a matched directory is touched as an
ordinary target. `VERBOSE` prints `<name>...touched`, `...created`, or
`...failed`. A failed `SetFileDate` with `ERROR_OBJECT_NOT_FOUND` falls back to
`Open(..., MODE_READWRITE)` and `Close` to create the object. Ctrl-C is polled
after each matched entry and returns `ERROR_BREAK`.

`touchname` selects `SetFilePosixDate` with `GetUTCSysTime` on DOS 51.66 or
newer and the classic `DateStamp`/`SetFileDate` path on older DOS. The bounded
body in `src/Commands/Native/NativeMorphOSTouchCommand.cs` implements the
classic path through public DOS 3.1 calls, with invocation-owned AnchorPath and
DateStamp storage. The private resident startup adapter is
`tests/Commands.AddBuffersNativeRoot/NativeMorphOSTouchEntry.cs`.

`tools/Commands/qualify_morphos_touch_native_entry.ps1` builds resident
68000/020/040 HUNKs with zero managed allocation sites, runtime helpers,
external native targets, exception regions or fatal machine-fault sites, then
runs ten supplied DOS/Exec invocations per CPU. The receipt is
`artifacts/touch-morphos-native-20260912-qualified/qualification.json`; the
HUNKs are 3932, 3956 and 3924 bytes respectively, with twelve reachable
methods on each CPU and no shared-image writes or fixture leaks. This is
bounded adapter evidence; the current implementation does not yet prove the
timer-device UTC
branch, `SoftlinkDODIR` classification, exact packed diagnostics, original
guest behavior, PURE/resident lifecycle, package admission, or differential
Workbench/MorphOS comparison.
