# SetClock contract

Profiles: `wb31` and `morphos320`. Goal step: CC21. Recorded: 2026-09-20.

The Workbench 3.1 `C:SetClock` HUNK is 668 bytes, version 37.1, SHA-256
`740baa5e0eebd14c2357434e5205a8d720a1533bb6e1439e35be8e1e4af1153c`. Its
captured syntax candidate is the same three-slot `LOAD/S,SAVE/S,RESET/S`
grammar. The Workbench implementation keeps only the classic
`battclock.resource` and `timer.device` vectors; the MorphOS UTC extension is
not applied to this profile.

The MorphOS 3.20 source member is `c/setclock/setclock.c`, 5053 bytes, SHA-256
`414f6c7e2c4d98a48753d7d59107e88b0125ceea53214db0954e0a3513a15ec5`. The
source version include records `SetClock 50.4 (22.2.2018)` and the AROS-derived
implementation. Its template is:

```text
LOAD/S,SAVE/S,RESET/S
```

The command allocates a DOS `RDArgs`, installs its extended help text, and
opens `battclock.resource`, a message port, a `timerequest`, and
`timer.device` unit `UNIT_VBLANK`. `LOAD` reads the battery clock and submits
`TR_SETSYSTIME`; `SAVE` reads the timer system time and writes the battery
clock; `RESET` calls `ResetBattClock`. Cleanup is reverse ordered and the
command prints `PrintFault(error, "SetClock")` for parser, allocation, device,
or missing-action failures.

`src/Commands/Native/NativeMorphOSSetClockCommand.cs` now records that public
classic boundary. It uses DOS `ReadArgs` through the shared invocation-owned
lease, `Exec.OpenResource`, `CreateMsgPort`, `CreateIORequest`, `OpenDevice`,
`DoIO`, and balanced teardown. The private resident startup adapter is
`tests/Commands.AddBuffersNativeRoot/NativeMorphOSSetClockEntry.cs`.

The bounded body calls the classic and MorphOS 52+ UTC battclock resource
vectors and timer `GetSysTime`/`GetUTCSysTime` through small CopperSharp
indirect-call ABI boundaries. The UTC path selects the UTC clock values only
when both providers report version 52 or newer and submits
`TR_SETUTCSYSTIME`; otherwise it retains the classic `TR_SETSYSTIME` path.
The SDK declarations are source-backed by the MorphOS 3.20 headers: UTC
battclock vectors are `-40`/`-46`, timer `GetUTCSysTime` is `-88`, and the UTC
set command is `CMD_NONSTD+4`.

The durable receipt `artifacts/setclock-morphos-native-20260912-qualified/
qualification.json` compiles and executes the resident HUNK on 68000/020/040,
with fifteen supplied DOS/Exec/resource/timer invocations per CPU, balanced
ownership, and unchanged shared images. This remains supplied-vector evidence;
it is not a real battclock/timer guest run.

The refreshed receipt `artifacts/setclock-morphos-native-20260920-utc-v3/
qualification.json` passes seventeen supplied vectors per CPU, including
classic and UTC load/save/reset paths plus the source's non-IoErr timer failure
diagnostic, with 17 reachable methods and unchanged shared images. This remains
supplied-vector evidence; it is not a real battclock/timer guest run.

## Workbench bounded native candidate

`NativeWorkbench31SetClockCommand` uses DOS 36, `battclock.resource`, the
classic battery-clock vectors and `timer.device`'s `GetSysTime`/`TR_SETSYSTIME`
path. The resident entry rejects Workbench startup and preserves the common
startup ownership and result handling. The deterministic receipt
`artifacts/setclock-wb31-native-20260920-candidate/qualification.json` passes
sixteen supplied DOS/Exec/resource/timer vectors per CPU, including parser,
resource, allocation and device failures, timer-I/O diagnostic output, Workbench
startup and missing-DOS boundaries, repeat ownership and two interleaved callers.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 2936 | `b6e8ec34b9427727e5373981e3d2e743682c4fa1eb7eee0bfc2a2cf338298142` | 10 | 16 |
| 68020 | 2936 | `fb217aaa91dd2a0b13483787ade2b7f74eef3cbc11cb39c1c714961fa1020dfb` | 10 | 16 |
| 68040 | 2936 | `fb217aaa91dd2a0b13483787ade2b7f74eef3cbc11cb39c1c714961fa1020dfb` | 10 | 16 |

This is a syntax-bound candidate, not original Workbench parity. Exact clock
device behavior, extended help, diagnostics, PURE/resident classification,
minimum stack, licensing, package placement and guest differential evidence
remain open.

## MorphOS bounded native identities

The three-CPU HUNK identities are:

| CPU | HUNK bytes | SHA-256 |
| --- | ---: | --- |
| 68000 | 3,844 | `08ce11c422f3cd7b3369291815ddf6d4387713d17b07552d6d406d29526736b3` |
| 68020 | 3,840 | `0511618d2539ae4cc6959a82ca67e7b031c58ba9f60c15eb3e6760db8208ba5a` |
| 68040 | 3,840 | `0511618d2539ae4cc6959a82ca67e7b031c58ba9f60c15eb3e6760db8208ba5a` |

Still open: Workbench 3.1 source/media parity, exact extended-help bytes and
diagnostic timing, real battclock/timer guest behavior, PURE and resident
lifecycle, package admission, and differential reference captures.
