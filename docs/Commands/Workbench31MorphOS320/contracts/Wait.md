# Wait contract

Profiles: `wb31` and `morphos320`. Goal steps: CC01, CC04, CC08 and CC16.
Recorded: 2026-09-12.

Status: bounded Workbench 3.1 native body only. The Workbench v37 media
candidate and the 3.1 command reference agree on this ReadArgs template:

```text
/N,SEC=SECS/S,MIN=MINS/S,UNTIL/K
```

The bounded body uses guest `DOS.Delay` in 1/50-second ticks. An omitted number
defaults to one; `SEC` is the default unit and `MIN` selects minutes. If both
unit switches are present, the classic minute branch wins. Negative values and
tick-count overflow fail with a DOS error. `UNTIL` accepts an `HH:MM` string,
uses `DateStamp`/`StrToDate`, rolls through midnight, and uses the same short
delay or asynchronous VBlank timer path as relative waits. Ctrl-C cancels an
outstanding timer and returns `WARN`.

## Bounded native receipt

`tools/Commands/qualify_wait_wb31_native.ps1` builds the private
`NativeWorkbench31WaitEntry` as a resident HUNK for all required CPUs and runs
the supplied DOS/Exec fixture. The fixture covers the default, explicit
seconds, explicit minutes, omitted-number minute default, zero, both-unit
precedence, `UNTIL` parsing/date conversion, negative value, parser,
allocation, timer-open and Ctrl-C failures, Workbench startup and missing-DOS
boundaries, balanced ownership and interleaved relative callers.

Receipt: `artifacts/wait-wb31-native-20260920-boundaries/qualification.json`.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 3968 | `1069ca09fdc8187e601680d343d3e8adfe60dcea4df1b1f1b4a402d4412ad7fb` | 13 | 17 |
| 68020 | 3912 | `4ea1054674c2f8a6c4ec6bb3047f2e24460eda3c014ff219745d35241ac5cfc9` | 13 | 17 |
| 68040 | 3844 | `c9a2fa71b2ae90759c28eb84489d8a9ef389d51b9eced88f6729214b4b81716c` | 13 | 17 |

The receipt is development evidence only. Exact original diagnostics and
parser/help behavior, guest clock/timer integration, original pure/resident
classification, same-segment lifecycle, packaging and differential comparison
remain open for the Workbench profile.

## MorphOS 3.20 profile

The MorphOS `Wait` identity is version 50.3. Its released source uses the same
template, `TIME/N,SEC=SECS/S,MIN=MINS/S,UNTIL/K`, and defaults to one second.
`SEC` selects 1/50-second ticks, `MIN` selects minutes, and the minute branch
wins when both switches are present. `UNTIL` accepts `HH:MM`, compares it with
the current DOS stamp and rolls through midnight when necessary. Delays up to
one second use DOS `Delay`; longer waits create a VBlank timer request, send
`TR_ADDREQUEST`, wait on the timer signal or `SIGBREAKF_CTRL_C`, and return
`WARN` after cancelling the request on Ctrl-C. Timer-open and allocation
failures retain the source diagnostics and return `FAIL`.

`src/Commands/Native/NativeMorphOSWaitCommand.cs` implements that boundary with
DOS `ReadArgs`, `DateStamp`/`StrToDate`, `Delay`, public Exec timer/message-port
calls and scheduler-visible `Wait`/`SendIO`/`AbortIO`/`WaitIO`. Its private root
is `tests/Commands.MorphOSWaitNativeRoot/MorphOSWaitEntry.cs`.

The durable supplied-vector receipt is
`artifacts/morphos-wait-native-entry-20260912-qualified/qualification.json`:
15 cases per CPU (45 total) pass on resident 68000/020/040 HUNKs, covering
default/explicit/zero delays, minute precedence, `UNTIL`, invalid time,
negative/parser/allocation failures, timer-open failure, Ctrl-C cancellation,
cleanup and interleaving. Static reports contain no managed allocation sites,
exception regions, runtime helpers or external native targets; runtime reports
show one image load per CPU, balanced ownership and zero shared-image writes.

This remains an adapter checkpoint. Original MorphOS guest diagnostics and
clock/timer parity, installed PURE/resident flags, same-segment reuse,
concurrent lifecycle, package placement and differential comparison remain
open.
