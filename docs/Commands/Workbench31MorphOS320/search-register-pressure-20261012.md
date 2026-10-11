# Search register-pressure investigation — 2026-10-12

**Search: 10,664 → 5,364 bytes (5,300 bytes, 49.7% saved).** The private
MorphOS profile falls from 20,744 to 10,464 bytes with the same optimizer change.
Installed `out/C` images remain unchanged; this is a qualified Search candidate.

## Cause and change

An instrumented compiler reproduces the exact baseline image. Its trace records
812 values selected for spilling in the first allocation round, including
774 phi definitions. The main method has 88 static calls and up to 26 values
live through a call before spilling. These are static SSA/allocation counts,
not runtime call or distinct local counts. Spill totals accumulate across rounds.

The machine optimizer skips copy/phi simplification when a function has loops.
The candidate enables only trivial-phi cleanup in those functions. A phi with
one distinct non-self input carries the same value around the loop and can be
replaced by that input. Existing type, width, register-set, precolor, GC, and
managed-byref checks remain in `CanSubstitute`. Loop copy substitution and loop
memory optimization retain their existing restrictions. The allocator itself
is unchanged. `SimplifyLoopPhis` is an explicit, default-off API/CLI size pass,
restricted to MC68000 resident HUNKs with YOLO exceptions and no managed memory.
The final default-off image is byte-identical to the baseline. This reduces
redundant SSA transport before coloring rather than
changing spill weights or register assignments.

| Main-method measure | Baseline | Candidate |
| --- | ---: | ---: |
| Virtual values before allocation | 2486 | 745 |
| Accumulated spilled values | 978 | 46 |
| Spill reload operations | 530 | 110 |
| Spill storage bytes | 100 | 72 |
| Local frame bytes | 172 | 140 |
| Saved-register bytes | 40 | 40 |
| Allocation rounds | 5 | 2 |

The recorded original Workbench Search is 2,476 bytes: the size ratio improves
from 4.31× to 2.17×. No comparison with a PPC MorphOS original is made.

## Rejected experiment

Coloring constrained groups first grew Search to 10,748 bytes, increased reloads
to 544 and the frame to 180 bytes, and needed six rounds. Its isolated payload,
patch, map, and trace remain in the experiment; that allocator change was reverted.
The diagnostic instrumentation was also removed from the retained compiler.

An unconditional version introduced two failures in the expanded selection:
the FileStats instruction-pattern assertion and ConsoleIo pinned costs
(30,132 expected cycles versus 29,848 observed). The retained pass is opt-in;
those assertions and compiler defaults were preserved. That exploratory run
and its 66 failures remain in JSON alongside the final comparison.

## Verification

- Frozen command assemblies, dependencies, response flags, and runner files are
  hash-checked before and after compilation. The baseline is byte-identical to
  the correctness checkpoint and installed Search. Final compiler source
  reproduces the exact qualified candidate image.
- Workbench Search: 19 supplied vectors per CPU on 68000, 68020, and 68040.
  Output, return levels, IoErr, resource events, repeated invocation, image
  writes, and supported interleaving agree. Per-CPU instruction totals:
  939,327 → 865,491; peak stack:
  520 → 472 bytes.
- Private MorphOS Search passes the same baseline/candidate comparison on all
  three CPUs. Per-case receipts retain its distinct locale and traversal inputs.
- Four new regression cases pass, covering trivial versus changing loop phis, incompatible
  register constraints, retained loop copies, zero-iteration loops, and values
  live across calls. Execution spans 68000/020/040 compilation targets, peephole modes,
  three CPU models, two relocation bases, boundary inputs, and repeated calls.
  The API eligibility case checks default-off behavior and byte
  identity for unqualified CPU targets and the application runtime.
- Final focused compiler selection: 1317 passing,
  0 failing tests. The expanded selection has
  3689 passing and 64
  failing tests; its control has 3688 passing and
  64 failing tests. No additional failed case is
  introduced across the common test membership; the added policy test passes.
  Exact baseline failures and newly passing cases remain in JSON;
  no assertions or budgets were weakened.

## Reproduction and scope

Use `audit_search_pressure.py freeze` with a fresh correctness-checkpoint stage,
then `compile` with isolated baseline/candidate payloads and distinct labels,
adding `--pass SimplifyLoopPhis` only for the candidate, and `qualify` against
the baseline. `compile-morphos` uses the private frozen root;
`qualify --morphos` selects its fixture. `report_search_pressure.py` binds the
measurements and receipts. Local evidence: `artifacts/search-pressure-20261011`.
The [JSON report](search-register-pressure-20261012.json) pins inputs, compiler payloads, patches,
maps, executor hashes, native observations, and compiler-test comparisons.

The retained source is in `../CopperSharp68k-search-pressure-20261011`, based on
the separately committed correctness fix. The primary compiler checkout remains
untouched. The pass remains disabled by default;
only the two Search profiles above were differentially qualified. Broader
command-set rollout remains future work. Real DOS parsing/I/O, original OS execution, minimum-stack execution,
and exact 020/040 hardware timing remain outside these fixtures.
