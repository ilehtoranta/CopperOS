# ChangeTaskPri contract

Profiles: `wb31` and `morphos320`. Goal step: CC18. Recorded: 2026-09-11.

Status: open reference contract with bounded Workbench and MorphOS native
bodies. No original-runtime parity or shipping qualification is claimed.

## Reference identity and grammar

The Workbench 3.1 `C/ChangeTaskPri` HUNK is 460 bytes, SHA-256
`8e7d47b887f2daee51f1588ed1c20d26aa2363c5672c94da5d4ae97e3469c170`, version
37.1. Its binary contains this syntax candidate:

```text
PRI=PRIORITY/A/N,PROCESS/K/N
```

The MorphOS 3.20 `MorphOS/C/ChangeTaskPri` packed member is 1,698 bytes,
SHA-256 `d48577c4c3cf43640d77086ccc56db5fd6342ab973476fdf4cd35279129e42e8`,
version 50.3. The release source uses the same template. `PRIORITY` is a
required signed value from -128 through 127; `PROCESS` optionally selects a
CLI/task number, otherwise the current task is selected. MorphOS 50.2 added
the race-safe lookup and `FindTaskByPID` fallback. Target resolution and
`SetTaskPri` run under `Forbid`/`Permit`; an out-of-range priority reports
`ERROR_OBJECT_TOO_LARGE`, while a missing process prints the process-specific
diagnostic and returns FAIL with cleared `IoErr`.

The MorphOS `FindTaskByPID` fallback uses the public MorphOS Exec pointer slot
at LVO `-994`, available from Exec version 50.45. The native body checks the
running Exec version/revision, loads the slot and invokes the SDK's indirect
`JSR (A3)` wrapper with ExecBase in A0, while keeping target resolution under
`Forbid`/`Permit`.

MorphOS separates the PID value from the current-task selector: the official
SDK documents `FindTaskByPID(0)` as selecting the current task, while
`TASKINFOTYPE_PID` returns the task's unique ID. CopperStart's MorphOS provider
now preserves that distinction: querying the current task's PID returns its
guest task identity, and PID zero remains only the lookup selector. This does
not establish MorphOS's exact numeric assignment or reuse policy; those still
need original-runtime evidence. See the [MorphOS Exec SDK reference](https://morphos-team.net/sdk/exec.html).

## Bounded implementation and receipt

`NativeMorphOSChangeTaskPriCommand` uses DOS `ReadArgs`, invocation-owned
result storage, `FindTask`/`FindCliProc`, the MorphOS pointer-indirect
`FindTaskByPID` wrapper when available, `SetTaskPri`, and DOS diagnostics.
It preserves signed range checking, target protection, required cleanup and
the source's final `IoErr` policy for the bounded classic path. When a target
is missing, it prefers the current process's `pr_CES` (`CurrentError` in the
SDK layout) and falls back to DOS `Output()` when that BPTR is null, matching
the inspected MorphOS source. The private entry uses the common DOS/Workbench
startup owner and has no host process or shared mutable state.

`NativeWorkbench31ChangeTaskPriCommand` is the separate classic profile body.
It uses the captured `PRI=PRIORITY/A/N,PROCESS/K/N` candidate, classic current
or `FindCliProc` target selection, signed -128..127 validation, `SetTaskPri`,
and no MorphOS PID extension.

`tools/Commands/qualify_changetaskpri_native.ps1` emits
`artifacts/changetaskpri-native-20260911/qualification.json`. The receipt runs
eleven supplied Exec/DOS invocations per CPU, including current and explicit
process/PID targets, both priority bounds, out-of-range errors, missing
targets, parser failure, cleanup and interleaving. The resident HUNKs have twelve
reachable methods and zero managed runtime features/helpers, external native
targets, exception regions or fatal machine-fault sites:

| CPU | Bytes | SHA-256 |
| --- | ---: | --- |
| 68000 | 2,920 | `2639df78fd63bb2971da92fae64c8f04e05d01ba33c6279828353b384a0485b7` |
| 68020 | 2,916 | `b0cd23bc6fa1e6dca1b17c3186aa055a5932d16cfc09358985be3a356f414e9f` |
| 68040 | 2,916 | `b0cd23bc6fa1e6dca1b17c3186aa055a5932d16cfc09358985be3a356f414e9f` |

This is a bounded static and supplied-vector checkpoint. Exact original
diagnostics/parser behavior, Workbench parity, real task
priority effects, production PURE/resident lifecycle and package placement
remain open.

`tools/Commands/qualify_changetaskpri_wb31_native.ps1` emits
`artifacts/changetaskpri-wb31-native-20260920-boundaries/qualification.json`.
All three CPU variants compile as resident HUNKs with ten reachable methods
and zero managed runtime features/helpers, external native targets, exception
regions or fatal machine-fault sites. The supplied fixture runs twelve
invocations per CPU for current and CLI targets, both priority bounds, range
errors, missing targets, parser failure, Workbench startup rejection,
missing-DOS startup failure, cleanup and interleaving:

| CPU | Bytes | SHA-256 |
| --- | ---: | --- |
| 68000 | 2,548 | `3eabe39e1e1872c019abcefe8e7e0da24445b83b17e41b4d394ccaa9401ca543` |
| 68020 | 2,548 | `a0e11ad8e218ca51c98e3f61f25379352eedb2b8a44b5622e4fa1316cf5636d1` |
| 68040 | 2,548 | `a0e11ad8e218ca51c98e3f61f25379352eedb2b8a44b5622e4fa1316cf5636d1` |

The startup and missing-DOS vectors allocate no parser or body state and
verify the entry's boundary result/error policy before task lookup or priority
mutation. This remains a bounded static and supplied-vector checkpoint; exact
diagnostics/effects, original guest parity, production PURE/resident lifecycle
and package placement remain open.

Current-source requalification on 2026-09-23 passes the same twelve supplied
vectors per CPU. The resident HUNK now has eleven reachable methods; the
qualification reports no runtime features/helpers, external targets,
exceptions, fatal sites, leaks, or shared-image writes. Receipt:
`artifacts/changetaskpri-wb31-native-20260923-error-stream-fixture-v2/qualification.json`.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 2760 | `a195f6fbde1e82e51267715bcb6114ca7e07c316ebd9e65596781324917bc9f4` | 11 | 12 |
| 68020 | 2756 | `c5b8d6bac07ea96cefc984d0f945bbd44f9f0da8cd7b0b426a1f9e33fa3b8717` | 11 | 12 |
| 68040 | 2756 | `c5b8d6bac07ea96cefc984d0f945bbd44f9f0da8cd7b0b426a1f9e33fa3b8717` | 11 | 12 |

## 2026-09-23 follow-up: missing-process stream routing

The source's missing-process branch calls DOS `Output()` and then redirects
the diagnostic to the current process's `pr_CES` when it is non-null. The
MorphOS resident body now follows that rule through
`DosLayout.Process.CurrentError`; its fixture checks both the `Output()`
fallback and a distinct redirected error stream. The 68000/020/040 receipt
`artifacts/changetaskpri-morphos-native-20260923-error-stream-v2/qualification.json`
passes twelve invocations per CPU with thirteen reachable methods and no
runtime features/helpers, external native targets, exception regions, fatal
sites, leaked resources or shared-image writes.

| CPU | HUNK bytes | SHA-256 | Reachable methods | Invocations |
| --- | ---: | --- | ---: | ---: |
| 68000 | 3200 | `285365ca77f7ff14bcaa626487c200bc9732b86fae555d800b4d7a949da54cb8` | 13 | 12 |
| 68020 | 3196 | `cb4f6797c55c911ced65b6bafba10f1cdc0a348c7d0568d15c71b0252a439dae` | 13 | 12 |
| 68040 | 3196 | `cb4f6797c55c911ced65b6bafba10f1cdc0a348c7d0568d15c71b0252a439dae` | 13 | 12 |

This closes only the supplied-vector stream-routing case. The complete guest
diagnostic/parser comparison, exact PID numbering and reuse, target liveness
and races, PURE/resident lifecycle, and package admission remain open.

## 2026-09-26 original Workbench parser capture

The [normal-startup guest capture](../reference-captures/workbench31-guest-command-probe-20260926.md) includes `C:ChangeTaskPri` with no required PRIORITY. Original DOS returns level20, caller post-System IoErr116, and captures the exact stream `required argument missing\nChangeTaskPri failed returncode 20\n` (see `artifacts/workbench31-guest-command-cc18-parser-baselines-20260926-v1/qualification.json`). This invocation ends in `ReadArgs` before process/target lookup, so it sends no signal and changes no task priority.

## 2026-09-27 bounded parser parity

The native parser-failure path now prints the raw DOS fault with a null
`PrintFault` header and returns `DOS.RETURN_FAIL`. The first paired guest run
had exposed the old candidate's `ChangeTaskPri:` prefix and return level 10.
`artifacts/workbench31-guest-command-cc18-readargs-pairs-20260926-v1/qualification.json`
records the corrected no-PRIORITY pair: 63 exact output bytes, return 20 and
caller post-System IoErr 116 on both guests. The refreshed three-CPU resident
receipt is
`artifacts/changetaskpri-wb31-native-20260926-readargs-parity-v3/qualification.json`;
it passes twelve supplied vectors per CPU, and the diagnostic replacement
preserves the original `C/ChangeTaskPri` metadata (protection word 0).

This establishes only the missing-required-argument case. Valid current/CLI
target priority changes, options, range/missing-target diagnostics, MorphOS
PID behavior, PURE/resident lifecycle, CopperStart, packaging and shipping
remain open.

## 2026-09-27 bounded current-task success pair

The original Workbench command and the candidate were also run as
`C:ChangeTaskPri 0`, with PROCESS omitted. Both produced no output, returned 0
and left caller post-System IoErr at 0. The combined three-case receipt is
`artifacts/workbench31-guest-command-cc18-command-pairs-20260927-v2/qualification.json`.
This is only an exact command-result comparison: the probe does not read the
current task's priority back, so it does not independently verify the effect.
The candidate pair preserves the original command metadata, including
protection word0. All other success, target and error cases remain open.

## 2026-09-27 bounded missing-target parity

The original and candidate were run as
`C:ChangeTaskPri 0 PROCESS 999999`, targeting a nonexistent CLI process. Both
emitted the exact 67-byte stream
`Process 999999 does not exist\nC:ChangeTaskPri failed returncode 20\n`,
returned20 and left caller post-System IoErr at 0. The candidate diagnostic
now matches Workbench's unprefixed text. No task existed, so neither command
called `SetTaskPri`. The exact pair is included in
`artifacts/workbench31-guest-command-cc18-command-pairs-20260927-v2/qualification.json`;
the current resident receipt is
`artifacts/changetaskpri-wb31-native-20260927-missing-cli-v1/qualification.json`.
The original `C/ChangeTaskPri` protection word remains 0. This does not verify
priority readback on success or close other targets/options/errors, MorphOS PID
semantics, lifecycle or packaging.

## 2026-09-27 bounded CLI priority-effect parity

The original and candidate were run as
`C:ChangeTaskPri 42 PROCESS @SELF@`. The disposable probe expanded `@SELF@` to
its own Workbench CLI task number 1 and retained that task while the command
ran. In both guests the sampled priority changed from 0 at the pre-System
stage-3 observation to 42 at terminal stage 100; the target task number was 1
at both points. Both commands returned 0, produced no output and left caller
post-System IoErr at 0. The [effect comparison](../../../../artifacts/workbench31-guest-command-changetaskpri-self-priority-candidate-20260927-v1/effect-comparison-v2.json)
requires the same task identity and the exact 0-to-42 transition as well as
matching command result fields.

The three-CPU resident receipt is
`artifacts/changetaskpri-wb31-native-20260927-missing-cli-v1/qualification.json`;
the candidate preserves the original protection word 0. This proves one valid
explicit CLI target and priority value. The probe task remains at priority 42
only in the disposable guest. Other targets, MorphOS PID semantics, range and
parser boundaries beyond the captured cases, lifecycle and package gates remain
open.

## 2026-09-27 bounded Workbench out-of-range parity

The original and refreshed Workbench candidate were run as
`C:ChangeTaskPri 128 PROCESS @SELF@`. Both print the exact 74-byte stream
`Priority out of range (-128 to +127)\nC:ChangeTaskPri failed returncode 20\n`,
return `DOS.RETURN_FAIL` (20), and leave caller post-System IoErr at 0. Saved
guest RAM shows CLI task 1 at priority 0 before and after, so neither command
changes the target. See the [effect-aware comparison](../../../../artifacts/workbench31-guest-command-changetaskpri-128-self-candidate-fixed-20260927-v1/effect-comparison.json).

The initial candidate mismatch was Workbench-specific: it used
`PrintFault(ObjectTooLarge)`, emitted a prefixed fault instead of the custom
text and left IoErr 207. `NativeWorkbench31ChangeTaskPriCommand` now emits the
captured text and clears IoErr; the MorphOS range behavior remains unchanged.
The refreshed three-CPU resident receipt is
`artifacts/changetaskpri-wb31-native-20260927-range-fix-v1/qualification.json`.
The disposable replacement preserves original protection word 0.

Both captured out-of-range guest values (`-129` and `128`) use an existing CLI
target. The original and candidate produce the same 74-byte custom diagnostic,
return 20, leave caller IoErr at 0 and leave CLI task 1 at priority 0. The lower
bound's [effect-aware comparison](../../../../artifacts/workbench31-guest-command-changetaskpri-minus129-self-candidate-20260927-v1/effect-comparison.json)
complements the upper-bound comparison above. Other target/priority
precedence, MorphOS PID numbering/reuse, full diagnostics and parser behavior,
resident/PURE lifecycle and package admission remain open.

## 2026-09-27 bounded range-before-target precedence

With an invalid priority and a missing CLI target, original Workbench reports
the priority-range error before target lookup. The candidate initially looked
up the target first and printed the missing-process message. The Workbench
body now performs signed range validation before `Forbid` or `FindCliProc`.
The [paired guest comparison](../../../../artifacts/workbench31-guest-command-changetaskpri-range-missing-self-candidate-20260927-v1/effect-comparison.json)
uses a valid numeric but nonexistent target and confirms exact output, return
20, caller IoErr 0 and unchanged owner priority. The refreshed supplied-vector
receipt passes thirteen cases per CPU on 68000/020/040:
`artifacts/changetaskpri-wb31-native-20260927-range-precedence-v1/qualification.json`.

## 2026-09-27 bounded valid priority endpoints

Original and candidate guests ran `C:ChangeTaskPri 127 PROCESS @SELF@` and
`C:ChangeTaskPri -128 PROCESS @SELF@`. Both returned 0 with empty output and
caller post-System IoErr 0. The saved CLI task changed from priority 0 to 127
and from 0 to -128 on both sides. See the [127 comparison](../../../../artifacts/workbench31-guest-command-changetaskpri-pri127-candidate-20260927-v1/effect-comparison.json)
and the [-128 comparison](../../../../artifacts/workbench31-guest-command-changetaskpri-pri-minus128-candidate-20260927-v1/effect-comparison.json).

These observations verify that both legal signed limits are accepted and
applied to a live CLI target. They do not establish all priority values or
other target forms, MorphOS PID semantics, or resident/PURE lifecycle and
package admission.

## 2026-09-27 bounded negative PROCESS target

Original and candidate guests ran
`C:ChangeTaskPri 0 PROCESS -1`. Both emit the exact 63-byte stream
`Process -1 does not exist\nC:ChangeTaskPri failed returncode 20\n`, return 20
and leave caller post-System IoErr 0. No `SetTaskPri` target exists or is
mutated. The current 68000 HUNK is 3,456 bytes with SHA-256
`3751abac703732a3b86ebe10aa40bb8b8b9e39fc878b8544ce3e48ea45a8e6c1`, bound by
`artifacts/changetaskpri-wb31-native-20260927-range-fix-v1/qualification.json`.
Exact comparison:
`artifacts/workbench31-guest-command-changetaskpri-negative-process-candidate-20260927-v1/effect-comparison.json`.

This closes one signed negative PROCESS error only. Other target forms,
priority values, MorphOS PID semantics, full parser/error behavior,
resident/PURE lifecycle and package admission remain open.

## 2026-09-27 bounded signed upper-bound PROCESS parity

Original and candidate guests ran
`C:ChangeTaskPri 0 PROCESS 2147483647`, the largest positive signed LONG
accepted by the captured `PROCESS/K/N` grammar. Both emitted the exact 71-byte
stream
`Process 2147483647 does not exist\nC:ChangeTaskPri failed returncode 20\n`,
returned 20 and left caller post-System IoErr at 0. The [effect comparison](../../../../artifacts/workbench31-guest-command-changetaskpri-max-process-current-candidate-20260927-v1/effect-comparison.json)
binds the original/candidate captures and verifies all observed fields.
The tested current 68000 candidate has SHA-256
`14722d2ab7675ff3cda9507f282274edcdb0f92e42ec82b15dcfc228ea5ce7f0`, bound by
`artifacts/changetaskpri-wb31-native-20260927-range-precedence-v1/qualification.json`.

This closes one upper-bound missing-process diagnostic only. Other target
forms, MorphOS PID semantics, full parser/error behavior, resident/PURE
lifecycle and package admission remain open.

## 2026-09-27 target lookup protection regression guard

The MorphOS and Workbench `ChangeTaskPri` `FindCliProc` fixture callback now
rejects a lookup unless Exec `Forbid` is active. The existing MorphOS
`FindTaskByPID` callback and `SetTaskPri` callback also require the protected
region. This guards the source-required lookup-to-mutation lifetime against
later code moving lookup outside protection. All three CPU variants pass the
current 12-vector MorphOS and 13-vector Workbench fixtures; the cross-profile
receipt records 186 invocations across both entries and all three CPUs:
`artifacts/cc18-forbid-lookup-regression-20260927-v1/qualification.json`.

This is a fixture guard for lock placement, not concurrent target-death stress
or real MorphOS guest proof. Exact PID numbering/reuse, lifecycle, PURE and
package gates remain open.

## 2026-09-27 bounded PRIORITY keyword alias parity

The original Workbench command and the current resident candidate were run as
`C:ChangeTaskPri PRIORITY=42 PROCESS @SELF@` on fresh disposable derivatives.
Both accepted the long `PRIORITY=` alias form, returned 0 with empty output,
and left caller post-System IoErr at 0. Saved guest RAM confirms that CLI task
1 changed from priority 0 to 42 on both sides. The [effect comparison](../../../../artifacts/workbench31-guest-command-changetaskpri-priority-alias-candidate-20260927-v1/effect-comparison.json)
binds the captures and requires the exact priority transition. The candidate
was the 3,452-byte 68000 HUNK with SHA-256
`14722d2ab7675ff3cda9507f282274edcdb0f92e42ec82b15dcfc228ea5ce7f0` and
preserved the original command metadata, including protection word 0.

This closes one keyword-alias and equals-form success case for a live CLI
target. A separate pair for `C:ChangeTaskPri 42 PROCESS=@SELF@` confirms the
`PROCESS/K/N` field's equals form: both guests again change CLI task 1 from 0
to 42, return 0, emit no output and leave caller IoErr at 0. Its [effect
comparison](../../../../artifacts/workbench31-guest-command-changetaskpri-process-equals-candidate-20260927-v1/effect-comparison.json)
binds this second pair. Together these cover the long PRIORITY alias/equal
form and PROCESS equals form only at priority 42 on the current CLI task. Other
ReadArgs forms, target identities, MorphOS PID semantics, lifecycle, PURE
admission and package placement remain open.

## 2026-09-27 bounded nonnumeric-priority parser parity

Original and candidate guests ran `C:ChangeTaskPri nope`. Both emitted the
exact 48-byte stream `bad number\nC:ChangeTaskPri failed returncode 20\n`,
returned 20, and left caller post-System IoErr at 115. The [comparison](../../../../artifacts/workbench31-guest-command-changetaskpri-priority-nonnumeric-candidate-20260927-v1/effect-comparison.json)
binds the fresh captures; failure occurs during DOS argument parsing, before
target lookup or priority mutation.

This adds one nonnumeric required-argument case. Other invalid parser forms,
missing and conflicting argument combinations, target forms, MorphOS behavior,
lifecycle, PURE admission and package placement remain open.

## 2026-09-27 bounded oversized-priority diagnostics

Original and candidate guests were also run with priority strings just outside
the signed 32-bit range: `2147483648` and `-2147483649`. For each invocation,
both emitted the exact 74-byte Workbench range diagnostic followed by the
command failure line, returned 20 and left caller post-System IoErr at 0. The
separate [positive](../../../../artifacts/workbench31-guest-command-changetaskpri-priority-overflow-positive-candidate-20260927-v1/effect-comparison.json)
and [negative](../../../../artifacts/workbench31-guest-command-changetaskpri-priority-overflow-negative-candidate-20260927-v1/effect-comparison.json)
receipts bind the original and current candidate outputs/results.

These observations close the two tested oversized strings' output and result
behavior only. They do not establish a general conversion rule or cover every
overflow and malformed input combination.

## 2026-09-27 bounded minimum PROCESS target

Original and candidate guests ran
`C:ChangeTaskPri 0 PROCESS=-2147483648`. Both emitted the exact 72-byte
`Process -2147483648 does not exist\nC:ChangeTaskPri failed returncode 20\n`,
returned 20 and left caller post-System IoErr at 0. The [comparison](../../../../artifacts/workbench31-guest-command-changetaskpri-process-min-signed-candidate-20260927-v1/effect-comparison.json)
binds the captures. The minimum signed PROCESS value resolves as a missing
target and matches the current candidate's diagnostic and result.

This adds one target-number boundary only. Other CLI identities, MorphOS PID
semantics, target races, lifecycle and package admission remain open.

## 2026-09-27 MorphOS PID fallback version floor

Inspection of the released MorphOS 3.20 `ChangeTaskPri` source confirms the
template `PRI=PRIORITY/A/N,PROCESS/K/N` and gates `FindTaskByPID` on Exec
version 50.45 or newer (including major versions above 50). The resident
fixture now models the Exec version/revision explicitly: version 50.44 with a
PID result available must not call the fallback, while version 51.0 may call
it. The current three-CPU receipt passes 14 supplied vectors per CPU with 16
reachable methods and no managed allocation sites, runtime features/helpers,
external targets, exception regions or fatal sites:
`artifacts/changetaskpri-morphos-native-20260927-pid-version-floor-v1/qualification.json`.
This adds a source-shaped boundary check; original MorphOS guest PID numbering,
PID reuse, task liveness/races, full diagnostics, lifecycle, PURE and package
gates remain open. Workbench was rerun after the shared fixture change and its
latest 13-vector-per-CPU receipt is
`artifacts/changetaskpri-wb31-native-20260927-current-regression-v1/qualification.json`.

## 2026-09-27 MorphOS current-task PID identity

MorphOS SDK documentation distinguishes the `FindTaskByPID(0)` current-task
selector from `TASKINFOTYPE_PID`, which reports a unique ID for each task. The
CopperStart provider previously returned the selector value as the current
task's ID. It now reports the existing per-live-task guest identity while
retaining zero as the current-task lookup selector. Focused provider tests
verify both selector and reported-ID lookup. This fixes the API distinction;
exact original MorphOS PID numbering and reuse remain unverified.

## 2026-09-27 MorphOS extended ReadArgs help

The released MorphOS source allocates DOS_RDARGS, sets `RDA_ExtHelp`, and
passes the object to `ReadArgs`. The MorphOS candidate now uses a private
process-control argument helper to preserve that help text and to return
RETURN_FAIL on parser failure, matching the bounded source contract. Its
Release qualification passes 14 supplied vectors per CPU on 68000/020/040,
including exact help and RDArgs allocation/free checks, at
`artifacts/cc18-changetaskpri-extended-help-20260927-v3/`. Workbench
ChangeTaskPri remains a separate null-RDArgs body; its current Release
regression passes 13 vectors per CPU at
`artifacts/cc18-changetaskpri-wb31-extended-help-regression-20260927-v3/`.
These source-shaped results do not establish MorphOS guest parity, full
diagnostic coverage, PID numbering/reuse/liveness, PURE/resident lifecycle,
rights clearance, or package admission.

## 2026-09-28 explicit PROCESS 0 selector

The released command checks whether the `PROCESS` result slot is present,
then passes its numeric value to `FindCliProc` and, when eligible, to
`FindTaskByPID`. The MorphOS Exec SDK documents `FindTaskByPID(0)` as returning
the current task. The candidate's supplied-DOS fixture now distinguishes this
case from omitting `PROCESS`: `PROCESS 0` misses `FindCliProc(0)`, calls the
indirect PID vector with D0 equal to zero while Forbid is active, receives the
current-task result, and applies priority 4. It passes 15 supplied invocations
per CPU across 68000/020/040, with 22 reachable methods and no managed runtime
features/helpers, external targets, exception regions, fatal sites, leaks, or
shared-image writes. Receipt:
[`qualification.json`](../../../../artifacts/changetaskpri-morphos-native-20260928-pid-zero-v1/qualification.json).
The 68000 HUNK SHA-256 is
`7a33599717a58a75c8d185c2fd07a5f1fc61923068129a36cbf0087663588774`; the
68020 and 68040 HUNKs share
`8075a7fc6e1bf185fe87a42926f1e04434504298aac88192251e97ec2c0f644e`.
See the [MorphOS Exec SDK](https://morphos-team.net/sdk/exec.html) for the
public `FindTaskByPID` contract.

This is supplied-vector ABI evidence. It does not prove original MorphOS guest
behavior, exact PID numbering/reuse, task liveness under scheduler changes,
resident/PURE lifecycle, rights clearance, or package admission.

The Workbench candidate now has the profile counterpart: explicit
`PROCESS 0` reaches classic `FindCliProc(0)`, reports `Process 0 does not
exist`, and never calls the MorphOS-only PID slot. Its refreshed resident
qualification passes 14 supplied invocations per CPU on 68000/020/040, with
13 reachable methods and no runtime helpers/features, leaks, or shared-image
writes at
[`qualification.json`](../../../../artifacts/changetaskpri-wb31-native-20260928-process-zero-v1/qualification.json).
The 68000 HUNK SHA-256 is
`14722d2ab7675ff3cda9507f282274edcdb0f92e42ec82b15dcfc228ea5ce7f0`; 020/040
share `d90746f571b973e04ef6bbdce8a15b29f47910c6d8053296489d31c999d42308`.
This remains supplied-DOS profile-boundary evidence, not an original Workbench
guest comparison.

The original Workbench 3.1 guest confirms that explicit `PROCESS 0` is a
missing CLI number for this command. Original and candidate runs on fresh
diagnostic derivatives both emit the exact 62-byte stream
`Process 0 does not exist\nC:ChangeTaskPri failed returncode 20\n`, return 20,
and leave caller post-System IoErr at 0. The accepted comparison is
[`comparison-output-result-ierr.json`](../../../../artifacts/workbench31-guest-command-changetaskpri-process-zero-candidate-20260928-v1/comparison-output-result-ierr.json).
It compares output, return and caller IoErr. The effect-aware projection is
not claimed because this invocation has no sampled pre-System owner state.

## 2026-09-28 Workbench question-mark and unknown-switch parsing

Fresh original/candidate guest pairs also match for these parser failures:

| Invocation | Exact output | Return | Caller IoErr | Comparison |
| --- | --- | ---: | ---: | --- |
| `C:ChangeTaskPri ?` | `PRI=PRIORITY/A/N,PROCESS/K/N: required argument missing\nC:ChangeTaskPri failed returncode 20\n` (93 bytes) | 20 | 116 | [`comparison.json`](../../../../artifacts/workbench31-guest-command-changetaskpri-help-candidate-20260928-v1/comparison.json) |
| `C:ChangeTaskPri 1 Z` | `wrong number of arguments\nC:ChangeTaskPri failed returncode 20\n` (63 bytes) | 20 | 118 | [`comparison.json`](../../../../artifacts/workbench31-guest-command-changetaskpri-unknown-switch-candidate-20260928-v1/comparison.json) |

As with `Break`, `?` does not select an extended-help path in Workbench 3.1;
the mandatory PRIORITY argument fails in DOS `ReadArgs`. The unknown extra
argument is rejected by the same parser. Both comparisons cover raw output,
return and caller post-System IoErr only; no task-effect claim is made.
MorphOS extended-help guest parity, other parser forms, target races,
PURE/resident lifecycle, rights and package admission remain open.

## Required gates

- [ ] Capture both profile help/parser behavior and exact range/error output.
- [x] Bind and qualify the MorphOS pointer-indirect `FindTaskByPID` SDK wrapper,
  including A0/D0 register checks, missing-target and cleanup cases.
- [x] Add supplied Exec/DOS fixtures for MorphOS current/process/PID targets
  and the Workbench current/process targets, bounds, missing process, parser
  failure, cleanup and interleaving.
- [x] Add and execute the `ExecBase-994` pointer entry in the CopperStart
  production provider; the compiled 68000 installer and pointer call are
  covered by native tests.
- [ ] Close exact MorphOS PID numbering/reuse and provider task identity/
  liveness handling.
- [ ] Compare original binaries on disposable guests; qualify purity,
  same-SegList reuse and package each profile in its proper image.
