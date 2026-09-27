# WaitForPort contract

Profile: `morphos320`. Goal steps: CC01, CC04 and CC16.
Recorded: 2026-09-20.

Status: bounded MorphOS 3.20 native candidate only. The documented command
grammar is:

```text
PORTNAME/A,I=INTERVAL/K/N,L=LOOP/K/N,D=DISAPPEAR/S
```

`PORTNAME` is required. `INTERVAL` defaults to one second and `LOOP` defaults
to ten checks. Each check uses the public Exec `FindPort` call. Without `D`,
the command succeeds when the named port is present; with `D`, it succeeds when
the named port is absent. A failed check waits with DOS `Delay` for the
requested interval before the next check. `LOOP=0` is the unbounded form and
is terminated by the requested state or Ctrl-C. Negative values and tick
conversion overflow fail with DOS numeric errors; Ctrl-C returns `WARN` with
the break error.

The candidate uses DOS `ReadArgs`/`FreeArgs`, public Exec `FindPort` and
`SetSignal`, and invocation-owned result storage. Its private resident entry
opens DOS 37, rejects Workbench startup, validates the argument boundary, and
replies/cleans up through the existing resident startup owner.

## Bounded native receipt

`tools/Commands/qualify_morphos_waitforport_native_entry.ps1` compiles
`NativeMorphOSWaitForPortEntry` as resident HUNK images for 68000, 68020 and
68040, then runs the supplied Exec/DOS fixture. The ten cases cover immediate
presence, loop exhaustion, disappearance, Ctrl-C, negative interval, tick
overflow, parser failure, Workbench startup rejection and interleaved callers.

Receipt: `artifacts/waitforport-morphos-native-6d77780fa9644fa2b68fd765d191f27d/qualification.json`.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 2776 | `6b842b053541643a2254d88e3185a1426138d819362485e299f53d7bc1cec748` | 12 | 10 |
| 68020 | 2796 | `67d2b358d32540ee9a98fdafd2fb26c4b66174fe61aebe467b0376741c0ef832` | 12 | 10 |
| 68040 | 2764 | `fe80ae88484b037b7b9de4d09e12971d2d92e9c62664064998cfc405b1a08837` | 12 | 10 |

This is an adapter checkpoint, not a shipping approval. Exact MorphOS guest
timing, diagnostics and parser/help behavior, source-to-packed correspondence,
provider behavior outside the fixture, original differential comparison,
PURE classification, same-segment lifecycle, package placement and licensing
remain open.
