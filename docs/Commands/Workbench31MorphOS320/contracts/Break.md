# Break contract

Profiles: `wb31` and `morphos320`. Goal step: CC18. Recorded: 2026-09-11.

Status: open reference contract with bounded Workbench and MorphOS native
bodies. No original-runtime parity or shipping qualification is claimed.

## Reference identity and grammar

The Workbench 3.1 `C/Break` HUNK is 432 bytes, SHA-256
`623ed6f6d43d30a5abce4f5b6ac75b89b33a79579c9628c9e4a55a0cfc392c41`, version
37.1. Its source-observed template candidate is:

```text
PROCESS/A/N,ALL/S,C/S,D/S,E/S,F/S
```

The MorphOS 3.20 `MorphOS/C/Break` packed member is 2,055 bytes, SHA-256
`96697e93fe93e11db9890d7a57c2eed1edbf2289e5fbfbaa6cd25741336a59ee`, version
50.6. The official source archive's `c/break/break.c` uses:

```text
PROCESS/N,PORT,ALL/S,C/S,D/S,E/S,F/S
```

`PROCESS` is an optional numeric CLI number, `PORT` is an optional public
message-port name, and the command requires one target. `ALL` selects
`SIGBREAKF_CTRL_C|D|E|F`; otherwise the selected flags are combined, with
Ctrl-C as the default. Target lookup and `Signal` occur under `Forbid`, and
the source emits target-specific diagnostics before returning FAIL when no
target is found. A successful call clears `IoErr`.

The MorphOS 50.6 fallback uses the public MorphOS Exec `FindTaskByPID` pointer
slot at LVO `-994`, available from Exec version 50.45. The native body checks
the running Exec version/revision, loads the function pointer from the slot,
and invokes it through the SDK's `JSR (A3)` wrapper with ExecBase in A0. The
lookup remains under `Forbid`/`Permit`.

## Bounded implementation and receipt

`NativeMorphOSBreakCommand` uses DOS `ReadArgs`, `FindCliProc`, the
MorphOS pointer-indirect `FindTaskByPID` wrapper when available, Exec
`FindPort`/`Signal`, `Forbid`/`Permit`, DOS output vectors and invocation-owned
parser storage. It preserves the source's signal masks, default Ctrl-C,
required-target diagnostics and final `IoErr` handling. It contains no host
process manipulation or shared mutable state. The Workbench body is kept
separate because the exact `PROCESS/A/N` behavior is still only a binary
syntax candidate.

`NativeWorkbench31BreakCommand` is the separate classic profile body. It uses
the captured `PROCESS/A/N,ALL/S,C/S,D/S,E/S,F/S` candidate, a six-slot result
array, classic DOS `FindCliProc`, and the same signal-mask and protection
rules. It deliberately has no MorphOS port or PID fallback.

The initial 2026-09-11 MorphOS receipt is
`artifacts/break-native-20260911/qualification.json`. All three CPU variants
compile as resident HUNKs with twelve reachable methods and zero managed runtime
features/helpers, external native targets, exception regions or fatal machine
fault sites. The same receipt runs thirteen supplied Exec/DOS invocations per
CPU, covering process and port paths, default/ALL/combined masks, missing
targets, PID fallback, parser failure, cleanup and interleaving:

| CPU | Bytes | SHA-256 |
| --- | ---: | --- |
| 68000 | 3,304 | `7659077986808b492a9cf564c5250fee3a7343f962f96a44585a4a34b9ec7da2` |
| 68020 | 3,300 | `ccdebb3ce07928a164f2d3c4cf19e0c3d808aa7acfe8900fd4a0658bd96eb3c4` |
| 68040 | 3,300 | `ccdebb3ce07928a164f2d3c4cf19e0c3d808aa7acfe8900fd4a0658bd96eb3c4` |

The receipt is a bounded static and supplied-vector checkpoint. Exact original
diagnostics, original Workbench/MorphOS differential captures,
concurrent target death, production PURE/resident lifecycle and package
placement remain open.

`tools/Commands/qualify_break_wb31_native.ps1` emits
`artifacts/break-wb31-native-20260920-boundaries/qualification.json`. All three
CPU variants compile as resident HUNKs with ten reachable methods and zero
managed runtime features/helpers, external native targets, exception regions
or fatal machine-fault sites. The supplied fixture runs ten invocations per
CPU for classic process resolution, default/ALL/combined masks, zero and
missing targets, parser failure, Workbench startup rejection, missing-DOS
startup failure, cleanup and interleaving:

| CPU | Bytes | SHA-256 |
| --- | ---: | --- |
| 68000 | 2,632 | `7cfa598ead587ac02653b050710968f0481e90ac67812cc9cd8dfd70f4808a07` |
| 68020 | 2,632 | `5d9f953e5f0dc4b6dba31de4a09ba85e10934c6b0b8ee6388ddd9cad8f20b66b` |
| 68040 | 2,632 | `5d9f953e5f0dc4b6dba31de4a09ba85e10934c6b0b8ee6388ddd9cad8f20b66b` |

The startup and missing-DOS vectors allocate no parser or body state and
verify the entry's boundary result/error policy before any classic target
lookup. This remains a bounded static and supplied-vector checkpoint; exact
diagnostics/effects, original guest parity, production PURE/resident lifecycle
and package placement remain open.

## 2026-09-26 original Workbench parser capture

The [normal-startup guest capture](../reference-captures/workbench31-guest-command-probe-20260926.md) includes `C:Break` with no required PROCESS. Original DOS returns level20, caller post-System IoErr116, and captures the exact stream `required argument missing\nBreak failed returncode 20\n` (see `artifacts/workbench31-guest-command-cc18-parser-baselines-20260926-v1/qualification.json`). This invocation ends in `ReadArgs` before process/target lookup, so it sends no signal and changes no task priority.

## 2026-09-27 bounded parser parity

The native parser-failure path now prints the raw DOS fault with a null
`PrintFault` header and returns `DOS.RETURN_FAIL`. The first paired guest run
had exposed the old candidate's `Break:` prefix and return level 10.
`artifacts/workbench31-guest-command-cc18-readargs-pairs-20260926-v1/qualification.json`
records the corrected `C:Break` no-PROCESS pair: 55 exact output bytes, return
20 and caller post-System IoErr 116 on both guests. The refreshed three-CPU
resident receipt is
`artifacts/break-wb31-native-20260926-readargs-parity-v4/qualification.json`;
it passes ten supplied vectors per CPU, and the diagnostic replacement
preserves the original `C/Break` metadata (protection word 0).

This establishes only the missing-required-argument case. Valid process
resolution, signal masks/effects, other parser and error paths, MorphOS parity,
PURE/resident lifecycle, CopperStart, packaging and shipping remain open.

## 2026-09-27 bounded missing-target parity

The original and candidate were run with `C:Break 999999`, using a process
number that does not exist in the guest. Both emitted the exact 59-byte stream
`Process 999999 does not exist\nC:Break failed returncode 20\n`, returned20,
and left caller post-System IoErr at 0. The candidate now prints the raw
Workbench diagnostic without a `Break:` prefix and does not signal any task.
The exact pair is included in
`artifacts/workbench31-guest-command-cc18-command-pairs-20260927-v2/qualification.json`;
the current resident receipt is
`artifacts/break-wb31-native-20260927-missing-cli-v1/qualification.json`.
Original command protection metadata remains 0. This closes only the
nonexistent-CLI diagnostic; valid targets, signal effects/masks, other errors,
MorphOS parity, lifecycle and package gates remain open.

## 2026-09-27 bounded Ctrl-C target-effect parity

The original and candidate were run as `C:Break @SELF@ C`. In the disposable
guest, the probe expands `@SELF@` to its own Workbench CLI task number 1. Both
commands returned 0 with no output and caller post-System IoErr 0. The saved
Task signal-received mask changed from `0x00000004` to `0x00001104`; bit 12
(`SIGBREAKF_CTRL_C`, `0x00001000`) was clear before and set afterward on the
same CLI task in both guests. The [effect comparison](../../../../artifacts/workbench31-guest-command-break-self-signal-candidate-20260927-v1/effect-comparison.json)
requires that exact task identity and signal-bit transition, in addition to
the original/candidate output and result fields.

The resident source remains the current three-CPU receipt
`artifacts/break-wb31-native-20260927-missing-cli-v1/qualification.json`;
the candidate retains the original protection word 0. This closes one valid
CLI target and the C mask only. Default-mask, ALL, D/E/F, combined masks,
target-not-found variants, MorphOS behavior, lifecycle and package gates remain
open. The probe's CLI stays signaled only in the disposable guest, which is
discarded after capture.

## 2026-09-27 bounded default, individual, ALL and combined-mask parity

The original and candidate were also run with the default mask
(`C:Break @SELF@`), each individual `D`, `E`, and `F` switch, `ALL`, and
combined `D E` and `D F`. In fresh guests, CLI task 1 had none of the requested
bits set before each command. The observed masks were Ctrl-C (`0x1000`) by default, D
(`0x2000`), E (`0x4000`), F (`0x8000`), Ctrl-C/D/E/F (`0xF000`) for `ALL`,
D/E (`0x6000`), and D/F (`0xA000`). All seven pairs returned 0, emitted no
output, left caller post-System IoErr at 0, and matched the original effect.

The effect-aware comparator requires each requested signal bit to be clear in
the stage-3 pre-System sample and set in the stable terminal sample, while
allowing unrelated signals to remain visible. Receipts:
[default](../../../../artifacts/workbench31-guest-command-break-default-candidate-20260927-v1/effect-comparison.json),
[ALL](../../../../artifacts/workbench31-guest-command-break-all-candidate-20260927-v1/effect-comparison.json),
[D/F](../../../../artifacts/workbench31-guest-command-break-df-candidate-20260927-v1/effect-comparison.json),
[D](../../../../artifacts/workbench31-guest-command-break-d-candidate-20260927-v1/effect-comparison.json),
[E](../../../../artifacts/workbench31-guest-command-break-e-candidate-20260927-v1/effect-comparison.json),
[F](../../../../artifacts/workbench31-guest-command-break-f-candidate-20260927-v1/effect-comparison.json),
[D/E](../../../../artifacts/workbench31-guest-command-break-de-candidate-20260927-v1/effect-comparison.json).

The full C/D/E/F switch truth table now passes in the refreshed three-CPU
resident fixture at
`artifacts/break-wb31-native-20260927-all-flags-v1/qualification.json`: 24
supplied Exec/DOS invocations per CPU, with 13 reachable methods and no
runtime helpers/features, external targets, exception regions, fatal sites,
leaks or shared-image writes. The 68000 HUNK retains the original protection
word 0. Original/candidate guest captures now also cover every C/D/E/F mask
combination (including default) and `ALL` on CLI 1; the individual receipts
are indexed in the [guest capture record](../reference-captures/workbench31-guest-command-probe-20260926.md).
Additional CLI target IDs, parser/help details, MorphOS ports/PIDs,
PURE/resident lifecycle and package admission remain open.

## 2026-09-27 bounded process-zero diagnostic parity

The original and candidate were run with `C:Break 0`. Both emitted the exact
54-byte stream `Process 0 does not exist\nC:Break failed returncode 20\n`,
returned 20, and left caller post-System IoErr at 0. No task was signaled. The
[effect comparison](../../../../artifacts/workbench31-guest-command-break-zero-candidate-20260927-v1/effect-comparison.json)
binds the original and candidate captures. This adds the zero-valued numeric
target error beside the existing nonexistent `999999` case; valid target
coverage remains the probe's own CLI 1.

## 2026-09-27 bounded signed negative target parity

The original and current Workbench candidate were run with `C:Break -1`. Both
emit the exact 55-byte stream
`Process -1 does not exist\nC:Break failed returncode 20\n`, return 20 and
leave caller post-System IoErr 0. This confirms the captured `PROCESS/A/N`
path preserves a signed negative numeric value in the target diagnostic; no
task is signaled. The current 68000 HUNK is 3,452 bytes with SHA-256
`cd094ef9e367a60cf3225cca2d0deef9fbeee604e41df6bc1f98b96ccc55acdc`, from
`artifacts/break-wb31-native-20260927-all-flags-v1/qualification.json`. Exact
comparison:
`artifacts/workbench31-guest-command-break-negative-process-candidate-20260927-v1/effect-comparison.json`.

This closes one additional signed missing-target case only. Other CLI targets,
errors, MorphOS ports/PIDs, lifecycle, PURE admission and package placement
remain open.

## 2026-09-27 bounded signed upper-bound target parity

The original and current candidate were run with `C:Break 2147483647`, the
largest positive signed LONG accepted by the captured `PROCESS/A/N` grammar.
Both emitted the exact 63-byte stream
`Process 2147483647 does not exist\nC:Break failed returncode 20\n`, returned
20 and left caller post-System IoErr at 0. The exact comparison is recorded in
the [effect receipt](../../../../artifacts/workbench31-guest-command-break-max-process-candidate-20260927-v1/effect-comparison.json).

This closes one upper-bound missing-target diagnostic only. It does not add a
valid target beyond the probe's own CLI, nor establish help behavior, MorphOS
PID/port behavior, lifecycle, PURE admission or package placement.

## 2026-09-27 target lookup protection regression guard

The MorphOS and Workbench Break `FindCliProc` fixture callback now rejects a
lookup unless Exec `Forbid` is active. The existing MorphOS `FindTaskByPID`
callback, MorphOS/Workbench port lookup, and `Signal` callbacks also require
the protected region. This guards the source-required lookup-to-signal lifetime
against later code moving lookup outside protection. All three CPU variants
pass the current 13-vector MorphOS and 24-vector Workbench fixtures; the
cross-profile receipt records 186 invocations across both entries and all
three CPUs:
`artifacts/cc18-forbid-lookup-regression-20260927-v1/qualification.json`.

This is a fixture guard for lock placement, not concurrent target-death stress
or real MorphOS guest proof. Exact PID numbering/reuse, lifecycle, PURE and
package gates remain open.

## 2026-09-27 MorphOS mask and target precedence requalification

The released 50.6 source applies the process path when `PROCESS` is nonzero;
when it is absent or zero, a supplied `PORT` is resolved instead. If that
fallback also fails while a zero-valued PROCESS argument was supplied, the
diagnostic still names process 0, following the source's argument-presence
check. A nonzero missing PROCESS does not fall through to a valid PORT.

The resident fixture now covers all sixteen combinations of C/D/E/F switches,
including the no-switch default, and tests nonzero PROCESS precedence, zero
PROCESS falling through to PORT, and the zero-process diagnostic precedence.
The MorphOS receipt
`artifacts/break-morphos-native-20260927-mask-precedence-v1/qualification.json`
passes 32 invocations per CPU across 68000/020/040 with 16 reachable methods,
no managed allocation sites, runtime features/helpers, external targets,
exception regions, fatal sites, leaks or shared-image writes. HUNKs are
4,220/4,216/4,216 bytes;
the CPU hashes are `2f0384d144b017ed2849033a8e9c8cd96ff1bd6b618363198824a842267b66d3`,
`be316ffe7782e6a9a64b9f6c99591e2d1a39ba44dcd62ca289b8285764258b14`, and the
same 68020/68040 hash. Workbench was requalified after the shared fixture
change and still passes 24 invocations per CPU at
`artifacts/break-wb31-native-20260927-morphos-matrix-regression-v1/qualification.json`.

These are source-shaped native vectors, not MorphOS guest comparisons. Exact
PID numbering/reuse, task-liveness races, original help/output parity,
production PURE/resident lifecycle, and package admission remain open.

## 2026-09-27 MorphOS PID fallback version floor

The released MorphOS 50.6 `Break` source calls `FindTaskByPID` only when Exec
is version 50.45 or newer (including major versions above 50). The resident
fixture now models Exec version/revision and verifies both sides of the
boundary: at 50.44 a successful PID callback is forbidden and the command
reports the missing process; at 51.0 the fallback may resolve and signal the
task. The current three-CPU receipt passes 34 supplied vectors per CPU with 16
reachable methods and no managed allocation sites, runtime features/helpers,
external targets, exception regions or fatal sites:
`artifacts/break-morphos-native-20260927-pid-version-floor-v1/qualification.json`.
This remains source-shaped evidence; original guest PID numbering/reuse,
task-liveness races, complete output parity, lifecycle, PURE and package gates
remain open.

Workbench Break was rerun after the shared fixture changed and still passes
24 vectors per CPU in
`artifacts/break-wb31-native-20260927-pid-version-floor-regression-v1/qualification.json`.
Its 68000 HUNK SHA-256 remains
`cd094ef9e367a60cf3225cca2d0deef9fbeee604e41df6bc1f98b96ccc55acdc`, so
the existing Workbench guest-effect comparisons remain tied to the same HUNK.

## 2026-09-27 MorphOS extended ReadArgs help

The released MorphOS source allocates DOS_RDARGS, sets `RDA_ExtHelp`, and
passes the object to `ReadArgs`. The MorphOS candidate now uses a private
process-control argument helper to preserve that help text and to return
RETURN_FAIL on parser failure, matching the bounded source contract. Its
Release qualification passes 34 supplied vectors per CPU on 68000/020/040,
including exact help and RDArgs allocation/free checks, at
`artifacts/cc18-break-extended-help-20260927-v6/`. Workbench Break remains a
separate null-RDArgs body; its current Release regression passes 24 vectors per
CPU at
`artifacts/cc18-break-wb31-extended-help-regression-20260927-v3/`.
These source-shaped results do not establish MorphOS guest parity, full
diagnostic coverage, PID numbering/reuse/liveness, PURE/resident lifecycle,
rights clearance, or package admission.

## 2026-09-28 Workbench question-mark and unknown-switch parsing

Fresh original/candidate guest pairs confirm the classic Workbench `ReadArgs`
behavior for two parser edges:

| Invocation | Exact output | Return | Caller IoErr | Comparison |
| --- | --- | ---: | ---: | --- |
| `C:Break ?` | `PROCESS/A/N,ALL/S,C/S,D/S,E/S,F/S: required argument missing\nC:Break failed returncode 20\n` (90 bytes) | 20 | 116 | [`comparison.json`](../../../../artifacts/workbench31-guest-command-break-help-candidate-20260928-v1/comparison.json) |
| `C:Break 1 Z` | `wrong number of arguments\nC:Break failed returncode 20\n` (55 bytes) | 20 | 118 | [`comparison.json`](../../../../artifacts/workbench31-guest-command-break-unknown-switch-candidate-20260928-v1/comparison.json) |

The first case does not enter an extended-help mode: DOS reports the missing
mandatory PROCESS argument with the captured template text. Both pairs match
exactly for raw output, return and caller post-System IoErr. These two cases
exercise parser failures before target resolution; the comparison does not
sample a task effect. MorphOS extended-help guest parity, other parser/error
forms, full target coverage, PURE/resident lifecycle, rights and package gates
remain open.

## Required gates

- [ ] Capture both profile help/parser behavior, aliases, exact diagnostics,
  result levels and `IoErr` precedence.
- [x] Bind the MorphOS pointer-indirect `FindTaskByPID` SDK wrapper and qualify
  its A0/D0 ABI with supplied PID lookup, missing-target and cleanup cases.
- [x] Add supplied Exec/DOS fixtures for the MorphOS process/port and PID
  paths, plus the Workbench process-only path, every C/D/E/F combination,
  ALL/default masks,
  missing targets, parser failure, cleanup and interleaving.
- [x] Add and execute the `ExecBase-994` pointer entry in the CopperStart
  production provider; the compiled 68000 installer and pointer call are
  covered by native tests.
- [ ] Close exact MorphOS PID numbering/reuse and provider task liveness, then
  guard concurrent target death.
- [ ] Compare original binaries on disposable Workbench and MorphOS guests;
  qualify pure/resident reuse and package each profile in its proper image.
