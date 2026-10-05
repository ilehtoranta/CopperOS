# Execute size optimization — 2026-10-04

## Accepted result

`out/C/Execute` is now **6,376 bytes**, down from the audited **6,780 bytes**:
**404 bytes / 5.96% saved**. The requested 5% goal required 6,441 bytes or less.
The compiler is unchanged; the existing opt-in resident size policy remains in
use with the same fixed compiler and matching SDK.

| Measurement | Before | After |
| --- | ---: | ---: |
| Executable | 6,780 | 6,376 |
| Instructions and shared return sequences | 6,376 | 5,974 |
| Constant data/alignment within the image | 282 | 282 |
| Reachable methods | 39 | 38 |
| HUNK relocations | 17 | 17 |
| Peak observed stack | 408 | 328 |
| Instructions across 151 cases, per CPU | 576,252 | 366,426 |

The fixture executes **36.41% fewer instructions**. It retains its 4,096-byte
stack limit. Instruction counts exclude execution inside real DOS/Exec
implementations; peak stack records minimum A7 at instruction boundaries.
The instruction region saves 402 bytes and HUNK rounding saves another two.

The genuine Workbench 3.1 executable remains 4,432 bytes, so the remaining gap
is **1,944 bytes / 43.9%**, or **1.439×** its size. The original hash and size
come from the previously verified media receipt; no new original-image replay
is asserted.

## Source changes

- Initialize the final invocation state directly, removing the temporary
  aggregate and its copy.
- Replace the tiny `At` calls with direct address expressions, exposing constant
  offsets to code generation; remove the unreachable helper.
- Return key lookup details through one caller-owned `KeyMatch` record passed
  by reference, then forward that record to value expansion. No parser results
  or resource ownership move into this record.
- Format the two temporary-name digits with bounded subtraction. Both callers
  supply 0..99; suffix values and task-number reduction remain identical.
- Recognize ASCII letters with one unsigned range comparison. The uppercasing
  helper retains its original implementation because its proposed rewrite
  generated larger code.
- Remove the unused CLI parameter from temporary-file creation.

Options, directives, help, Workbench startup, diagnostics, selected return/IoErr,
allocation sizes and CLI handoff remain part of the same command contract.
No startup or ownership checks were removed for size.

## Independent experiments

All rows use the same frozen compiler, SDK and dependency assemblies, and all
151 cases pass on each of 68000, 68020 and 68040.

| Candidate relative to audited baseline | Bytes | Saved |
| --- | ---: | ---: |
| Audit's four initial source changes | 6,536 | 244 |
| Initial changes plus bounded digit loop | 6,520 | 260 |
| Initial changes plus both character range rewrites | 6,528 | 252 |
| Initial changes plus one key-result record | 6,444 | 336 |
| Initial changes plus forwarding that record | 6,408 | 372 |
| Combined, including both character rewrites | 6,388 | 392 |
| **Combined, retaining original uppercasing** | **6,376** | **404** |

Register assignment and branch layout make savings non-additive. The final
shipping project build is byte-identical to the accepted isolated experiment.

## Qualification

The existing 34-case shipping-entry suite is extended to **151 cases per CPU**.
Both the preserved 6,780-byte baseline and final executable pass all **453
CPU-case runs** and have matching command-visible observations.

Added cases exercise:

- `FILE/A` prompt success, parser failure and result-allocation failure, with
  exact parser/result ownership and release;
- every two-digit task-number value 0..99, task number 100 and `uint.MaxValue`;
- collisions across suffixes 1..98, selecting suffix 99 without changing existing
  files;
- first and last key result slots, aliases, defaults, mixed-case switches,
  `int.MinValue`, `int.MaxValue` and zero numeric expansion;
- ASCII boundaries and high-byte input; the changed letter predicate is also
  checked against its original expression for all 256 byte values;
- ordinary callee register restoration at the internal `Run` return, covering
  D2..D7 and A2..A6. This distinguishes the DOS executable entry ABI from the
  internal function ABI.

The suite also retains checks for exact work-file contents, output, faults,
return/IoErr, CLI state, vector order, owned memory/locks/handles, requester
restoration, Workbench message reply, stack guards/restoration and image
immutability. Map hashes bind the new register probes to the compiled image.

The Exec/DOS providers are models. Fresh original-image/CopperScreen replay,
full read/write/close failure behavior, capacity boundaries, Ctrl-C/Ctrl-D and
overlapping Shell continuations remain outside these fixtures. The audit's
existing I/O, truncation and previous-input ownership findings remain separate
correctness work; this optimization does not claim to resolve them.

## Reproduction and publication

The [machine-readable receipt](execute-size-optimization-20261004.json) records
the complete size comparison, source patch/input identities, compiler/SDK
hashes, independent experiments, CPU checks and publication manifests.

- Toolchain: clean compiler checkout `b73f379`, frozen CLI and matching SDK from
  `artifacts/command-executable-size-20261004/final/compiler/`.
- Flags: MC68000, resident HUNK, YOLO exceptions, memory disabled, fixed-point
  peepholes, symbols disabled, resident code-size policy enabled.
- Copper68k: 1.5.1, actual product
  `1.5.1+43a3a84f79657cb4c2b32bb58d8e510e3aec9c15`; DLL SHA256
  `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`.
- Build/execution/input receipts:
  `artifacts/execute-optimize-20261004/shipping/`.
- Original installed Execute backup:
  `artifacts/execute-optimize-20261004/shipping/previous-C/Execute`.

The qualified executable was installed through a filtered publication. All
**60 other command hashes and the 61-file command inventory are preserved**.
Total `out/C` size falls from **248,088 to 247,684 bytes**. No stale files were
removed.

Installed Execute SHA256:
`676e9db33b760a8fc82e92c1b74d4f99ee15868ce7bb14bab05c0e2ba58cb060`.
