# WaitForLib contract

Profile: `morphos320`. Goal steps: CC01, CC04 and CC16.
Recorded: 2026-09-20.

Status: bounded MorphOS 3.20 native candidate only. The documented command
grammar is:

```text
LIBNAME/A,I=INTERVAL/K/N,L=LOOP/K/N
```

`LIBNAME` is required. `INTERVAL` defaults to one second and `LOOP` defaults
to eleven checks. `LOOP=0` is the unbounded form. Each check searches Exec's
public library list with `FindName`; an absent library is followed by a DOS
`Delay` for the requested interval before the next check. The bounded form
therefore waits for `INTERVAL * (LOOP - 1)` seconds after the initial check.
Negative values and tick-conversion overflow fail with DOS numeric errors.
Ctrl-C returns `WARN` with the break error. The candidate does not open or load
the requested library while polling; the public list is the readiness source.

The candidate uses DOS `ReadArgs`/`FreeArgs`, public Exec `FindName` and
`SetSignal`, and invocation-owned result storage. Its private resident entry
opens DOS 37, rejects Workbench startup, validates the argument boundary, and
replies/cleans up through the existing resident startup owner.

## Bounded native receipt

`tools/Commands/qualify_morphos_waitforlib_native_entry.ps1` compiles
`NativeMorphOSWaitForLibEntry` as resident HUNK images for 68000, 68020 and
68040, then runs the supplied Exec/DOS fixture. The ten cases cover immediate
presence, loop exhaustion, the default loop, Ctrl-C, negative interval, tick
overflow, parser failure, Workbench startup rejection and interleaved callers.

Receipt:
`artifacts/waitforlib-morphos-native-912808104128434b81af17a62421d3de/qualification.json`.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 2756 | `2ff1ac9122ed2ee8cf41ab84659cea62061ce865eb98959c706a4453b611296e` | 13 | 10 |
| 68020 | 2776 | `f3238e94cef7f87598affb2f06d35d2554c6c3829da4f022815fd984bbb1e927` | 13 | 10 |
| 68040 | 2748 | `61d436bee46dc303b03c36157cded844a160c72590527ec69da754ddc27866a2` | 13 | 10 |

This is an adapter checkpoint, not a shipping approval. Exact MorphOS guest
timing, diagnostics and parser/help behavior, source-to-packed correspondence,
provider behavior outside the fixture, original differential comparison,
PURE classification, same-segment lifecycle, package placement and licensing
remain open.
