# Workbench 3.1 normal-CLI command capture

This CC03/CC09 diagnostic launches an authored 68000 HUNK through the original
Workbench startup CLI. The probe uses original DOS `ReadArgs`, synchronous
`SystemTagList`, `NIL:` input and a guest `RAM:` output file. It publishes a
result record through a named public Exec port. The host reads saved chip/slow
RAM only; it does not install OS services or inject program counters, vectors,
memory, or command input.

The [public API audit](workbench31-guest-probe-public-api-audit-20260926.json)
binds the relevant original NDK 3.1 autodocs and tag declarations. The
[native probe documentation](../../../../tools/Commands/Workbench31GuestProbe/native/README.md)
defines the exact record, ownership, failure codes, and reproduction steps.
Its initial build is
`artifacts/workbench31-guest-probe-native-20260926-v2/build-receipt.json`:
3,192 bytes, 68000, ten reachable methods, no managed runtime dependencies.
Compilation alone is not guest execution evidence.

## Media and execution boundary

The source is the hash-bound Workbench 3.1 M10 rev 40.42 disk 2 already used by
the [unchanged-media passive capture](workbench31-clean-runner-slowram-readiness-20260926.md).
Each diagnostic derivative remains outside the repository under
`D:/TestData/CopperOS-Diagnostics/`. The copied image receives only the authored
`C/CopperProbe` and one invocation before the unique `EndCLI`. The original ADF
is read only and is hashed before and after preparation and execution.

The image builder checks the complete tree, boot blocks, metadata, filesystem
allocation, unrelated payloads and permitted raw block changes. It preserves
original source irregularities rather than silently repairing reference media.
The resulting image is a diagnostic derivative, not original media or a release
image. It does not bypass production command-package admission.

The capture wrapper binds the already qualified passive runner, its dependencies
and engine source/PDB evidence, the ROM, original and derived ADFs, probe HUNK,
preparation receipt and selected tool sources. Each run uses fresh artifacts.
The analyzer follows the public Exec port list; it does not scan RAM for a
convenient magic value. A successful capture requires a complete, stable record,
the expected token, consistent process/port ownership, and untruncated output.

## Qualification limits

- `commandReturn` is the result returned by synchronous `SystemTagList`.
  A nonzero command return can be a successfully captured outcome.
- `postSystemIoErr` is the caller's immediate `IoErr` after that call. The NDK
  does not guarantee it is the child command's `pr_Result2`; command secondary
  error propagation remains unqualified.
- Only the supplied output handle is captured. Separate stderr, interactive
  input, filesystem side effects and general fault injection remain open.
- The diagnostic retains its process, HUNK, port and allocated result buffers
  until the machine is discarded. This is explicit diagnostic ownership, not
  proof of production cleanup, PURE admission or resident reuse.
- The current engine profile is an exploratory Kickstart 3.1 run, not expanded
  product certification. No timing/display/disk behavior is changed here.
- Complete Workbench/MorphOS command parity and the full goal remain open.

## Bound original-command results

All four independent 2,400-frame runs completed. Their first complete record
appears at frame 2,160 and remains identical for five observations through
frame 2,400. Each run has 41 saved snapshots, immutable before/after inputs and
no reported unsupported active feature. Original media and frozen runtime bytes
remain unchanged. No emulator source or binary was changed for these runs.

The [aggregate qualification](../../../../artifacts/workbench31-guest-command-captures-20260926-v1/qualification.json)
binds all four preparation, capture and analysis receipts, plus the authored
probe build and API audit. The executed reference is the original 4,764-byte
`C/Version`, SHA-256
`dc2f55cd48b37bd1efecdbf07d38757463129dd9076ea6bf1b5d967f2189f224`.

| Command | Return | Caller post-System IoErr | Captured output |
| --- | ---: | ---: | --- |
| `C:Version dos.library` | 0 | 0 | `dos.library 40.3\n` (17 bytes) |
| `C:Version dos.library VERSION 39` | 0 | 0 | Same 17 bytes |
| `C:Version dos.library VERSION 999` | 5 | 0 | Same 17 bytes |
| `C:Version dos.library VERSION nope` | 20 | 115 | `bad number\nC:Version failed returncode 20\n` (42 bytes) |

The numeric failure stream includes the Shell's failure line. The output column
shows LF as `\n`; each receipt also contains exact hex, byte length and SHA-256.
These are original-command captures, not original/replacement comparisons.
They establish minimum-version behavior and output retention for these named
queries, and an ordinary `ReadArgs` numeric-error path. They do not identify
which internal lookup provider resolved `dos.library`.

The image preparation verifier passes ten focused checks including an external
installed-writer roundtrip over authored FFS data. Its raw-block checks reject
changes to unrelated file slack, unused blocks and undeclared header fields.
It preserves the source's 49 out-of-image free bitmap padding bits and root
word 126 pointing to the bitmap as opaque source residues; neither is used as
extra allocatable storage. The complete original root block is restored on the
derivative and compared byte for byte. The analyzer passes fourteen focused
checks, including incomplete records, changed terminal data and nonzero command
returns. These tests support the capture tooling, not whole-command parity.

The Workbench Version candidate's existing system/RES comparison and output
paths have now been corrected and pass 96 supplied native vectors; see the
[command contract](../contracts/Version.md). That separate fixture result is
not a generated guest comparison. Next work is to implement the remaining
source-ordered lookup/provider paths and compare generated replacements through
the same guest route. Exact child secondary errors, separate streams, CopperStart and MorphOS
execution, fault cases, PURE/resident lifecycle and packaging remain open.


## Additional baselines and first Version replacement pairs

The [second original aggregate](../../../../artifacts/workbench31-guest-command-captures-20260926-v2/qualification.json)
adds six independent captures, making ten original Version cases. RES and
uppercase names return canonical `dos.library 40.3\n`; FULL returns
`dos.library 40.3 (04/01/93)\n`; revision-only minimum 4 returns 5, and
VERSION 39 with REVISION 999 returns 0. Caller post-System IoErr is zero in
all six. Saved snapshots and media/probe/preparation identities were rechecked.
The [FULL/options audit](version-wb31-full-options-audit-20260926.json)
records the original date parsing, public DOS/Utility calls and the parsed but
unused UNIT/INTERNAL/RES slots. These observations do not prove every provider.

The builder can now substitute an explicitly hash-bound authored HUNK for
`C/Version` in a fresh external derivative. It preserves the original file's
metadata, including protection word **0 (P clear)**. It never changes the
licensed original or promotes the replacement to PURE/shipping status.
The capture consumer binds the candidate and retained snapshot. The comparator
independently reads both disk images and re-decodes both saved RAM streams,
requiring the same original medium, ROM, probe, runtime, frame count and command.
It compares exact bytes, return and caller IoErr without normalization.

The [control-pair receipt](../../../../artifacts/workbench31-guest-version-replacement-controls-20260926-v1/qualification.json)
records two actual guest executions of the previous 68000 candidate, SHA-256
`c8f8095d6ea9baea2ebacbdbe2a4e0514f4595515198eccf235ceca3699734be`:

| Command | Original | Previous candidate | Comparison |
| --- | --- | --- | --- |
| `C:Version dos.library RES` | Return 0, caller IoErr 0, 17 bytes | Identical | Captured case equal |
| `C:Version dos.library` | Return 0, caller IoErr 0, 17 bytes | Return 20, caller IoErr 236, 56 bytes containing the unimplemented diagnostic and Shell failure line | Captured case mismatch |

Both comparisons admit the execution evidence; the second remains a failure,
not a rejected or normalized result. All **39** focused builder/analyzer/
comparison tests pass. These positive and negative controls establish a usable
Version replacement comparison route. They do not complete Version, secondary
error semantics, failure/lifecycle coverage, PURE admission or packaging.


## Corrected candidate: ten equal guest pairs

The [new aggregate](../../../../artifacts/workbench31-guest-version-pairs-20260926-v2/qualification.json) binds ten exact comparisons using the
corrected 6,912-byte 68000 Version HUNK
`143af192988613f3b29449cb3b095fcd3389b90c924e330f7ab8f8d21603fec7`.
All ten commands listed in the original aggregates now match exact output,
return and caller post-System IoErr, including FULL and numeric parse failure.
Every captured side passed provenance admission and saved-RAM re-decoding;
independent review recomputed all comparisons without discrepancies. Each run
uses a fresh guest, so this does not establish resident reuse or concurrent
real-OS lifetime. The [Version contract](../contracts/Version.md) records 174
separate native fixture cases and the remaining full-command gates.


## CC18 required-argument parser baselines

The normal Workbench guest probe also captured two parser failures from the
original C commands. [The aggregate receipt](../../../../artifacts/workbench31-guest-command-cc18-parser-baselines-20260926-v1/qualification.json) rechecks
source, runtime and snapshot hashes for both runs:

| Command | Returned level | Caller post-System IoErr | Exact output |
| --- | ---: | ---: | --- |
| `C:Break` | 20 | 116 | `required argument missing\nC:Break failed returncode 20\n` |
| `C:ChangeTaskPri` | 20 | 116 | `required argument missing\nC:ChangeTaskPri failed returncode 20\n` |

Both fail in DOS `ReadArgs` before target lookup, signaling or priority change.
These captures establish only the missing-required-argument path. The bounded
candidate comparison is recorded below; other syntax, command effects and
resident lifetime remain unqualified.

## CC18 missing-required-argument candidate pairs

The [aggregate comparison receipt](../../../../artifacts/workbench31-guest-command-cc18-readargs-pairs-20260926-v1/qualification.json)
binds each original capture to a fresh candidate derivative using the same
Kickstart 3.1 ROM, Workbench disk, probe and frozen passive runner. Both
comparisons admit their evidence and match command text, raw output bytes and
length, return level, and caller post-System IoErr:

| Command | Return | Caller IoErr | Output bytes | Result |
| --- | ---: | ---: | ---: | --- |
| `C:Break` without PROCESS | 20 | 116 | 55 | Exact captured case equal |
| `C:ChangeTaskPri` without PRIORITY | 20 | 116 | 63 | Exact captured case equal |

The first candidates had a command-name `PrintFault` header and returned 10.
Workbench's raw ReadArgs diagnostic and return20 were restored; the supplied
fixtures now check the null PrintFault header and parser result. Original C
member metadata is preserved, including protection word0 for both candidates.
Each invocation fails before process lookup, Signal or SetTaskPri.

This does not qualify successful target actions, other syntax/errors or
precedence, MorphOS behavior, PURE/resident lifecycle, CopperStart integration,
complete source/media rights or package placement. Caller post-System IoErr is
not a general child pr_Result2 observation.

## CC18 ChangeTaskPri default-current-task success pair

The original Workbench command and the diagnostic candidate were also run as
`C:ChangeTaskPri 0`, with PROCESS omitted. Both produced **zero output bytes**,
returned **0** and left caller post-System IoErr at **0**. The [combined receipt](../../../../artifacts/workbench31-guest-command-cc18-command-pairs-20260927-v2/qualification.json)
rechecks this pair alongside the two parser cases. This is only result/output
parity: the probe does not independently read the current task priority after
the command returns, so it is not proof that the priority effect occurred.

## CC18 missing-target pairs

Original and candidate runs used nonexistent process number `999999` for both
commands. The original diagnostic is unprefixed; the exact captured streams and
results are:

| Invocation | Output bytes | Return | Caller post-System IoErr | Candidate comparison |
| --- | ---: | ---: | ---: | --- |
| `C:Break 999999` | 59 | 20 | 0 | Exact captured case equal |
| `C:ChangeTaskPri 0 PROCESS 999999` | 67 | 20 | 0 | Exact captured case equal |

The output text is `Process 999999 does not exist`, followed by the command
failed-return footer. Because no target exists, these cases send no signal and
make no priority change. The five-case combined receipt is
`artifacts/workbench31-guest-command-cc18-command-pairs-20260927-v2/qualification.json`.
This does not qualify successful target effects or close either command.

## CC18 successful Workbench target effects

The probe now expands one `@SELF@` placeholder to its own DOS CLI task number
and pauses 150 DOS ticks before System. This leaves a stage-3 snapshot before
the command effect and a terminal snapshot afterward. The saved-memory analyzer
reports task number, signed priority and received signal mask; the effect
comparator fails if either state is missing or the requested transition does
not occur.

| Original invocation | Captured before → after | Original/candidate result | Receipt |
| --- | --- | --- | --- |
| `C:ChangeTaskPri 42 PROCESS @SELF@` | CLI 1 priority 0 → 42 | Empty output, return 0, caller IoErr 0; exact effect match | [Effect comparison](../../../../artifacts/workbench31-guest-command-changetaskpri-self-priority-candidate-20260927-v1/effect-comparison-v2.json) |
| `C:Break @SELF@ C` | CLI 1 Ctrl-C bit clear → set (`0x1000`) | Empty output, return 0, caller IoErr 0; exact effect match | [Effect comparison](../../../../artifacts/workbench31-guest-command-break-self-signal-candidate-20260927-v1/effect-comparison.json) |
| `C:Break 0` | Process 0 rejected; no task effect | Exact 54-byte missing-process stream, return 20, caller IoErr 0 | [Effect comparison](../../../../artifacts/workbench31-guest-command-break-zero-candidate-20260927-v1/effect-comparison.json) |
| `C:ChangeTaskPri 128 PROCESS @SELF@` | CLI 1 priority 0 → 0 | Exact 74-byte range diagnostic, return 20, caller IoErr 0; no target mutation | [Effect comparison](../../../../artifacts/workbench31-guest-command-changetaskpri-128-self-candidate-fixed-20260927-v1/effect-comparison.json) |
| `C:ChangeTaskPri -129 PROCESS @SELF@` | CLI 1 priority 0 → 0 | Exact 74-byte range diagnostic, return 20, caller IoErr 0; no target mutation | [Effect comparison](../../../../artifacts/workbench31-guest-command-changetaskpri-minus129-self-candidate-20260927-v1/effect-comparison.json) |
| `C:ChangeTaskPri 128 PROCESS @SELF@999999` | CLI 1 priority 0 → 0 | Range diagnostic precedes missing-target lookup; exact 74-byte output, return 20, caller IoErr 0 | [Effect comparison](../../../../artifacts/workbench31-guest-command-changetaskpri-range-missing-self-candidate-20260927-v1/effect-comparison.json) |
| `C:ChangeTaskPri 127 PROCESS @SELF@` | CLI 1 priority 0 → 127 | Empty output, return 0, caller IoErr 0; exact effect match | [Effect comparison](../../../../artifacts/workbench31-guest-command-changetaskpri-pri127-candidate-20260927-v1/effect-comparison.json) |
| `C:ChangeTaskPri -128 PROCESS @SELF@` | CLI 1 priority 0 → -128 | Empty output, return 0, caller IoErr 0; exact effect match | [Effect comparison](../../../../artifacts/workbench31-guest-command-changetaskpri-pri-minus128-candidate-20260927-v1/effect-comparison.json) |
| `C:Break @SELF@` | CLI 1 Ctrl-C clear → set (`0x1000`) | Default mask; empty output, return 0, caller IoErr 0 | [Effect comparison](../../../../artifacts/workbench31-guest-command-break-default-candidate-20260927-v1/effect-comparison.json) |
| `C:Break @SELF@ ALL` | CLI 1 Ctrl-C/D/E/F clear → set (`0xF000`) | Empty output, return 0, caller IoErr 0 | [Effect comparison](../../../../artifacts/workbench31-guest-command-break-all-candidate-20260927-v1/effect-comparison.json) |
| `C:Break @SELF@ D F` | CLI 1 Ctrl-D/F clear → set (`0xA000`) | Combined-mask case; empty output, return 0, caller IoErr 0 | [Effect comparison](../../../../artifacts/workbench31-guest-command-break-df-candidate-20260927-v1/effect-comparison.json) |
| `C:Break @SELF@ D` | CLI 1 Ctrl-D clear → set (`0x2000`) | Empty output, return 0, caller IoErr 0 | [Effect comparison](../../../../artifacts/workbench31-guest-command-break-d-candidate-20260927-v1/effect-comparison.json) |
| `C:Break @SELF@ E` | CLI 1 Ctrl-E clear → set (`0x4000`) | Empty output, return 0, caller IoErr 0 | [Effect comparison](../../../../artifacts/workbench31-guest-command-break-e-candidate-20260927-v1/effect-comparison.json) |
| `C:Break @SELF@ F` | CLI 1 Ctrl-F clear → set (`0x8000`) | Empty output, return 0, caller IoErr 0 | [Effect comparison](../../../../artifacts/workbench31-guest-command-break-f-candidate-20260927-v1/effect-comparison.json) |
| `C:Break @SELF@ D E` | CLI 1 Ctrl-D/E clear → set (`0x6000`) | Combined-mask case; empty output, return 0, caller IoErr 0 | [Effect comparison](../../../../artifacts/workbench31-guest-command-break-de-candidate-20260927-v1/effect-comparison.json) |
| `C:Break @SELF@ C D` | CLI 1 Ctrl-C/D clear → set (`0x3000`) | Combined-mask case; empty output, return 0, caller IoErr 0 | [Effect comparison](../../../../artifacts/workbench31-guest-command-break-cd-candidate-20260927-v1/effect-comparison.json) |
| `C:Break @SELF@ C E` | CLI 1 Ctrl-C/E clear → set (`0x5000`) | Combined-mask case; empty output, return 0, caller IoErr 0 | [Effect comparison](../../../../artifacts/workbench31-guest-command-break-ce-candidate-20260927-v1/effect-comparison-v2.json) |
| `C:Break @SELF@ C F` | CLI 1 Ctrl-C/F clear → set (`0x9000`) | Combined-mask case; empty output, return 0, caller IoErr 0 | [Effect comparison](../../../../artifacts/workbench31-guest-command-break-cf-candidate-20260927-v1/effect-comparison.json) |
| `C:Break @SELF@ E F` | CLI 1 Ctrl-E/F clear → set (`0xC000`) | Combined-mask case; empty output, return 0, caller IoErr 0 | [Effect comparison](../../../../artifacts/workbench31-guest-command-break-ef-candidate-20260927-v1/effect-comparison.json) |
| `C:Break @SELF@ C D E` | CLI 1 Ctrl-C/D/E clear → set (`0x7000`) | Combined-mask case; empty output, return 0, caller IoErr 0 | [Effect comparison](../../../../artifacts/workbench31-guest-command-break-cde-candidate-20260927-v1/effect-comparison.json) |
| `C:Break @SELF@ C D F` | CLI 1 Ctrl-C/D/F clear → set (`0xB000`) | Combined-mask case; empty output, return 0, caller IoErr 0 | [Effect comparison](../../../../artifacts/workbench31-guest-command-break-cdf-candidate-20260927-v1/effect-comparison.json) |
| `C:Break @SELF@ C E F` | CLI 1 Ctrl-C/E/F clear → set (`0xD000`) | Combined-mask case; empty output, return 0, caller IoErr 0 | [Effect comparison](../../../../artifacts/workbench31-guest-command-break-cef-candidate-20260927-v1/effect-comparison.json) |
| `C:Break @SELF@ D E F` | CLI 1 Ctrl-D/E/F clear → set (`0xE000`) | Combined-mask case; empty output, return 0, caller IoErr 0 | [Effect comparison](../../../../artifacts/workbench31-guest-command-break-def-candidate-20260927-v1/effect-comparison.json) |
| `C:Break @SELF@ C D E F` | CLI 1 Ctrl-C/D/E/F clear → set (`0xF000`) | Explicit four-switch case; empty output, return 0, caller IoErr 0 | [Effect comparison](../../../../artifacts/workbench31-guest-command-break-cdef-candidate-20260927-v1/effect-comparison.json) |

Each case ran the original Workbench member and the native candidate in fresh
disposable guests using the same Kickstart 3.1 ROM, reference ADF, probe HUNK
and frozen runner. The replacements preserve protection word 0. The changed
priority and pending Ctrl-C signal remain confined to their disposable guest;
neither original media nor the host is modified. These cases close one target
and one option/mask each, not full command parity, other target resolution,
MorphOS semantics, lifecycle or package admission.

## CC18 Break signed-LONG upper-bound missing target

The original and candidate were compared with `C:Break 2147483647`, the largest
positive signed LONG accepted by the captured `PROCESS/A/N` grammar. Both
emitted the exact 63-byte stream
`Process 2147483647 does not exist\nC:Break failed returncode 20\n`, returned
20 and left caller post-System IoErr at 0. The [effect receipt](../../../../artifacts/workbench31-guest-command-break-max-process-candidate-20260927-v1/effect-comparison.json)
binds the original/candidate captures and verifies all observed fields.
This covers one upper-bound missing-target case only; it does not add another
valid target or close Break, MorphOS, lifecycle or package gates.

## CC18 ChangeTaskPri signed-LONG upper-bound missing process

Original and candidate were compared with
`C:ChangeTaskPri 0 PROCESS 2147483647`, the largest positive signed LONG
accepted by the captured `PROCESS/K/N` grammar. Both emitted the exact 71-byte
stream
`Process 2147483647 does not exist\nC:ChangeTaskPri failed returncode 20\n`,
returned 20 and left caller post-System IoErr at 0. The [effect receipt](../../../../artifacts/workbench31-guest-command-changetaskpri-max-process-current-candidate-20260927-v1/effect-comparison.json)
verifies exact output, return and caller IoErr. This closes one numeric upper
bound only; it does not close target, MorphOS, lifecycle or package gates.
