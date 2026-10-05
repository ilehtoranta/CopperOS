# Execute executable size audit — 2026-10-04

This document records the audit before optimization. The subsequent
[implementation and qualification](execute-size-optimization-20261004.md)
reduces the installed executable to 6,376 bytes; the audit measurements below
retain their original baseline and candidate scope.

## Result

Shipping `out/C/Execute` is **6,780 bytes**, compared with **4,432 bytes** for
Workbench 3.1 `execute 37.11 (14.5.91)`: **2,348 bytes larger, or 1.530×**.
The original identity comes from the existing
[hash-bound binary capture](reference-captures/execute-wb31-binary-audit.json).
The current original image is unavailable on this host; this audit does not
repeat its extraction or infer its implementation from its size.

Four isolated source changes produce **6,536 bytes**, a **244-byte / 3.60%**
reduction. All supplied shipping-entry cases pass on 68000, 68020 and 68040.
Peak observed stack falls from **408 to 360 bytes** against the fixture's
unchanged 4,096-byte limit. Instruction counts fall by **15.34%** across the
34 cases per CPU. This measures command instructions and gateway calls,
excluding execution within actual DOS/Exec implementations.

These are audit candidates. Command sources and `out/C/Execute` remain unchanged.
The candidate remains **2,104 bytes / 47.5% larger** than Workbench.

## Reproduction and evidence

The [machine-readable report](execute-size-audit-20261004.json) records source,
compiler, SDK, input, image and runner hashes; per-method sizes; independent
compiler switches; and all candidate qualification summaries.

- Source revision: `330ecad`.
- Fixed compiler checkout: `codex/command-executable-size`, `b73f379`.
- Frozen compiler and SDK: `artifacts/command-executable-size-20261004/final`.
- Settings: 68000, resident HUNK, YOLO exceptions, memory disabled, fixed-point
  peepholes, symbols disabled, code-size policy enabled.
- Existing Copper68k 1.5.1 runner; core hash
  `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`.
- Isolated control rebuilt from current Execute sources against the frozen
  dependencies is **byte-identical** to shipping Execute, SHA256
  `b8821fc852c7aa128c82759be334d17e3be2122c3efc4d0371c7ff21cedc4f24`.
- Experiment sources, builds, maps, receipts, disassembly and unapplied patch:
  `artifacts/execute-size-audit-20261004/`.

Each compiler experiment uses identical frozen command assemblies. Each source
experiment uses the same frozen compiler and dependency assemblies. The
independent source edits are measured before the combined build.

## What occupies the binary

| Component | Bytes |
| --- | ---: |
| Execute implementation and its helpers | 5,520 |
| General argument lease used by `Execute ?` | 514 |
| Entry, DOS lifetime and Workbench startup helpers | 312 |
| Resident entry wrapper | 30 |
| Constant strings and their alignment | 282 |
| HUNK records, relocations and final alignment | 122 |
| **Total** | **6,780** |

The HUNK contains 6,660 bytes of payload including final padding, 17 relocations,
39 reachable methods, no initialized writable data/BSS, no managed allocations,
and no framework/runtime helpers. The compiler's `code-bytes` metric includes
constant data; its actual instruction region is **6,376 bytes**.

| Largest method | Bytes | Responsibility |
| --- | ---: | --- |
| `RunScript` | 822 | Files, transform loop, caller continuation, CLI handoff |
| `Substitute` | 506 | Keys, inline defaults, stored defaults |
| `FindKey` | 494 | Template aliases, modifiers, four output parameters |
| `DefineDefault` | 478 | Default token decoding and storage |
| `ProcessDirective` | 386 | Directive recognition and parameter boundaries |
| `CreateWorkFile` | 384 | Names, collisions, fallback, requester suppression |

**Additional options:** the shipping entry selects the Workbench implementation
directly. Both reference and implementation advertise `FILE/A`; the same eight
directive names are embedded in the original. No MorphOS command body, host
adapter, Shell runner or alternate profile is reachable. The `/S`, `/T`, `/N`
and `/M` handling and some delimiter/default forms still need broader original
qualification; their presence does not establish that we implement more than
Workbench.

**Documentation:** C# comments and XML documentation do not occupy this HUNK.
The 282-byte constant region contains command diagnostics, directive keywords,
`FILE/A`, `dos.library` and the temporary-name prefix. Documentation is not a
material explanation for the size gap.

**Checks and ownership:** bounds, allocation/parser failure handling, temporary
names, requester restoration, nested-input handling and startup cleanup have
real costs. Original equivalents have not been disassembled here, so this audit
does not label those costs as additional Workbench behavior. Some important I/O
checks are absent, as detailed below.

## Measured source opportunities

| Isolated change | Executable bytes | Saved | Instructions across 34 cases | Peak stack |
| --- | ---: | ---: | ---: | ---: |
| Shipping/control | 6,780 | — | 109,294 | 408 |
| Initialize the final state directly | 6,704 | 76 | 108,452 | 360 |
| Expand the 19 `At` address expressions | 6,636 | 144 | 105,159 | 408 |
| Remove redundant `% 10` from the tens digit | 6,752 | 28 | 97,534 | 408 |
| Remove unused `CreateWorkFile` CLI parameter | 6,776 | 4 | 109,267 | 408 |
| **All four together** | **6,536** | **244** | **92,530** | **360** |

Each row has 34 passing invocations on each of the three CPUs. Return/IoErr,
output, faults, file contents, CLI handoff, resource lifetime, vector order,
startup message handling, stack restoration and image immutability pass the
existing fixture. Savings are not additive because register assignment, layout
and branch encoding change in the combined build.

1. **State initializer:** `Run` clears a 104-byte frame, constructs a separate
   48-byte aggregate, then copies its twelve longwords into the live state.
   `default(State)` followed by field assignments avoids that temporary/copy.
   State is already passed by `ref` throughout; argument lease copies are not
   the problem here. This also identifies a compiler aggregate initialization
   opportunity beyond the existing read-only result forwarding pass.
2. **Tiny address helper:** `At` has an eight-byte body but 19 call sites.
   Direct expressions expose constant displacements and avoid call-related
   moves/saves. The current single-use inliner cannot perform this transformation
   because the helper has multiple callers. A compiler size decision should
   account for caller simplification, not just the size of duplicated bodies.
3. **Unsigned arithmetic:** the two-digit formatter uses software 32-bit
   restoring division. Both callers guarantee a value below 100: task number
   is reduced modulo 100, and the suffix loop ranges from 1 through 99. Therefore
   `(value / 10) % 10` can become `value / 10` without changing names. General
   32-bit division must retain its full range; using 68000 `DIVU.W` requires
   a proof that its operand and quotient bounds fit.
4. **Unused parameter:** removing the CLI parameter saves four bytes alone,
   but adds no further executable saving to the combined source candidate.

Other compiler evidence includes two `move.l (a7),(a7)` instructions in
`RunWithWork` and substantial register movement around the four `FindKey`
outputs. These are investigation targets, not additional measured savings.
Eliminating a move must preserve flags and memory effects; specializing outputs
must preserve aliases. No such compiler changes were made in this audit.

## Existing compiler policy

| Independently enabled pass | Saved against 6,872-byte policy-off control |
| --- | ---: |
| Unused register-argument staging | 16 |
| Incoming aggregate homes | 0 |
| Read-only aggregate forwarding | 0 |
| Single-use helper inlining | 0 |
| Internal call clustering | 12 |
| Identical bodies | 0 |
| Shared return/restore sequences | 64 |
| **Combined policy** | **92** |

Every independently compiled policy and the combination passes the same
three-CPU fixture. The combined policy is already enabled for shipping Execute.
Fixed-point peepholes converge; disabling or further enabling these existing
switches does not offer an unexplored source of large savings.

## Startup and cleanup

Startup accounts for **342 bytes including the wrapper**, about 5% of the file.
The invocation context is eight bytes on the stack; it does not allocate a heap
context. Execute separately allocates a cleared **4,672-byte work area** and
owns any retained `.KEY` RDArgs/results until preprocessing completes. All
command state is invocation-owned.

Workbench launch is dynamically recognized, its message received, and the
CLI-only operation rejected. Finish closes DOS and replies exactly once under
Forbid. Keep this path: refusing to execute a script from Workbench still
requires completing its message lifecycle. Removing all startup would not
explain most of the 2,348-byte gap.

The optional `?` path brings in the general argument lease, `RunTemplatePrompt`
and `CopyCString`: **756 bytes altogether**. A dedicated fixed-template parser
using existing owned scratch storage might reduce this, but needs prompt,
allocation failure and parser lifetime qualification. Removing help would
change behavior, and is not an accepted size strategy.

## Correctness findings and qualification limits

1. **Unchecked I/O:** `RunScript` ignores both `FWrite` results and `Close`
   results. `FGets` termination and nonpositive `FRead` are treated as completion
   without distinguishing read errors. A failed or partial transform can be
   published as CLI input with a success result. The fixture always succeeds
   these operations and cannot establish fault safety.
2. **Silent expansion truncation:** `Append` discards bytes once the 2,048-byte
   output buffer is full. Capacity overflow neither fails the transform nor
   grows the buffer. Original boundary behavior remains unknown.
3. **Deletion ownership:** `IsWorkFileName` accepts any name starting with
   `T:Command-` or `:T/Command-`. A user script with such a prefix can qualify
   for deletion. A prefix alone does not prove the ownership claimed in the
   source comment; tightening the naming check also cannot prove ownership by
   itself. Original behavior and the selected ownership contract need resolving.

The 34 cases cover ordinary keys/defaults/directives, nested-script concatenation,
name collisions, fallback, selected allocation/open/parser failures, missing
DOS/CLI, invalid entry length and Workbench startup. They do **not** cover
`Execute ?` continuation, short writes, read/close/reopen faults, capacity
boundaries, Ctrl-C/Ctrl-D, or genuinely overlapping resident invocations.
Separate fixture instances reuse the loaded image; this is not interleaved
continuation qualification. Historical original guest captures cover a limited
set of substitutions/defaults, not complete parity. CopperScreen and fresh
original-image checks are unavailable.

## Recommended order

1. Retain the isolated small-source candidate and its receipts for a separate
   implementation change; keep Workbench startup, help and existing options.
2. Resolve the I/O, capacity and previous-input ownership findings with matching
   failure and boundary fixtures before expanding compatibility claims.
3. Investigate compiler aggregate initialization, constant-offset helper
   inlining and bounded unsigned arithmetic using Execute as a reproduction.
4. Measure a fixed-template `?` parser and leaner key lookup independently.
   No savings are claimed for these unimplemented changes.

The audit proves **244 bytes of avoidable source/code-generation overhead**.
It cannot assign every remaining byte of the Workbench gap without inspecting
the original's generated code and qualifying the remaining behavioral boundaries.
