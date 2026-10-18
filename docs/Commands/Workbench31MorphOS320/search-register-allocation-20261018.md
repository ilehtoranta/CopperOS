# Search matcher register-bank spill — 2026-10-18

## Result

Fixed the allocator in an isolated CopperSharp checkout and integrated the change into the existing workspace while preserving unrelated edits. The accepted policy repairs data-only byte/word allocation failures by moving one eligible Int32 scalar into a legal address register. It remains under the existing opt-in `SimplifyLoopPhis` policy; compiler-wide defaults are unchanged.

| Current Search profile | Before | Accepted | Saved |
|---|---:|---:|---:|
| shipping | 5,192 | 5,188 | 4 |
| morphos | 9,896 | 9,880 | 16 |

Refreshed `out/C/Search` to **5,188 bytes**. All 61 commands remain present: **221,000 → 220,996 bytes**. The other 60 hashes are unchanged. The genuine Workbench 3.1 Search size reference is 2,476 bytes, giving a new ratio of **2.095×**. MorphOS-profile MC68000 measurements have no PPC comparison.

## Why caching caused a spill

The original matcher calls `CStringLength` before its loops. Its selected allocation puts the incoming line length in A4, the pattern length in D2 and the boolean match flag in D7. All values fit.

Replacing that call with the cached load changes machine-IR constraints, coalescing and coloring order. The greedy integer preference chooses Dn colors before callee-saved An colors. The cached variant puts the line length in D7; its byte-wide match flag can use only Dn. When the flag is colored, data colors are exhausted even though A4 is free and the line length is legal there. The allocator previously spilled immediately without reconsidering its earlier color choice. This is an avoidable bank choice, not a true shortage across both register banks.

The baseline cached matcher has one spill, a four-byte frame, a byte store for the initial true value on each attempted start, and a reload on the success path. Disassembly and allocation maps identify the changed homes; the diagnostic trace identifies the failed byte group and the eligible line-length scalar. The accepted repair puts the line length back in A4 and keeps the flag in D7. No flag stack home remains.

| Cached-flag Workbench matcher | Before | Accepted |
|---|---:|---:|
| Spilled values | 1 | 0 |
| Spill frame | 4 | 0 |
| Saved-register bytes | 32 | 36 |

The cached Workbench matcher remains 120 encoded bytes while losing its stack traffic. The current index-based matcher remains 110 bytes. Whole-image savings also include repairs in other Search helpers; no local body-size reduction is inferred from the spill count.

The incoming line length is a plain Int32 scalar whose legal register set already includes An. The fix does not loosen instruction constraints or infer new address eligibility. The current command keeps the previously qualified index-result source simplification; there is no reason to reintroduce the boolean.

## Allocator repair

Only after ordinary coloring fails, and only for a data-only byte/word group:

1. Examine its legal data colors. Require exactly one allocated interfering group occupying a candidate color.
2. Require that neighbor to be an unprecolored, non-reload, long-width group containing only non-GC Int32 values and permitting both banks. Exclude register pairs, references and managed byrefs.
3. Find an An color that satisfies the neighbor’s own forbidden set and every already allocated neighbor, also reserving the narrow value’s proposed data color. Call clobbers and reserved registers remain enforced.
4. Among feasible moves, choose the neighbor with the lowest loop-weighted demand. This limits extra transfers back to data registers at arithmetic consumers. Ties retain deterministic candidate order.
5. Commit one complete reassignment, then assign the narrow value. A failed search changes nothing. Existing verification, spill rewriting, frame/preservation planning and emission still follow.

There is no recursive search or instruction/memory reordering. Successful ordinary allocations retain their old path. The dirty main workspace has additional pair-alias and reload-eviction work; its integration conservatively retains that existing path for any function containing pair aliases. Clean-base qualification excludes those unrelated drafts.

## Measurements and rejected prototypes

| Compiler policy | Original WB | Cached flag WB | Current WB | Original MorphOS | Cached flag MorphOS | Current MorphOS |
|---|---:|---:|---:|---:|---:|---:|
| Baseline | 5,208 | 5,200 | 5,192 | 9,928 | 9,920 | 9,896 |
| Broad first-fit repair | 5,208 | 5,204 | 5,192 | 9,924 | 9,912 | 9,900 |
| Broad weighted repair | 5,204 | 5,196 | 5,188 | 9,924 | 9,912 | 9,900 |
| Restricted narrow repair | 5,204 | 5,196 | 5,188 | 9,908 | 9,904 | 9,880 |

The broad first-fit and broad weighted prototypes were rejected: both make the current MorphOS image four bytes larger. Traces show unnecessary wide/rematerializable-value and same-bank address repairs, plus an unfavorable first-fit scalar choice. Restricting the failing value to a data-only byte/word and the relocation to An removes that expansion. Only the restricted repair is integrated. All six final image comparisons are strictly smaller.

Fewer spills alone are not the acceptance criterion. For example, moving a scalar into a previously unused saved register can alter frame cost and add An-to-Dn transfers. Complete encoded HUNK measurements govern adoption.

## Qualification and execution costs

The final command matrix covers three unchanged retained CIL variants, both profiles, baseline/candidate and **68000/68020/68040**: **1,566 invocations**. Counts below are per image and per CPU; the measurements agree across all three CPU fixtures.

| Source/profile | Calls | Instructions before → after | Peak stack before → after |
|---|---:|---:|---:|
| original/shipping | 30 | 3,238,399 → 3,238,286 | 472 → 476 |
| original/morphos | 57 | 13,260,304 → 13,269,415 | 712 → 712 |
| cached-flag/shipping | 30 | 3,221,035 → 3,212,498 | 472 → 476 |
| cached-flag/morphos | 57 | 13,219,850 → 13,228,961 | 712 → 712 |
| shipping/shipping | 30 | 3,196,067 → 3,195,954 | 472 → 476 |
| shipping/morphos | 57 | 13,182,353 → 13,191,464 | 712 → 712 |

The cached-flag Workbench corpus loses 8,537 instructions, including 8,191 in its long-line case. Current Workbench instructions fall by 113. MorphOS corpora gain 9,111 instructions each (about 0.07%); 8,997 come from the long-file-abandon case. These measured costs are accepted because executable size takes priority. They are instruction counts, not wall-clock speed claims.

Workbench peak stack rises by four bytes to 476. Some MorphOS cases also gain four bytes, while the profile maximum stays 712. Existing 4 KiB/8 KiB stack limits are unchanged and all measured calls remain below their configured limits.

- **717 compiler tests pass**: allocator and new repair regressions, instruction/CCR dataflow, branch relaxation/boundaries, frame preservation and resident code-size execution. New cases cover policy off/on, byte/word recovery, weighted choice, determinism, failed-search atomicity, fixed registers, reloads, GC/byrefs, narrow neighbors, wide/pair failures, forbidden/reserved colors, interference and multiple blockers.
- Existing Search suites retain maximum/empty literals, mixed literal lengths under interleaving, locale behavior, FILE/PATTERN modes, long lines, short writes, buffered-output ordering, failures, Ctrl-C, resource ownership, startup and repeated invocation. Output bytes, returns, IoErr, allocations/releases and other command-visible observations match.
- **192 original Kickstart 3.1 DOS calls** pass on both Workbench images and all three CPUs, including 4 KiB/8 KiB stacks. **Four auditor mutation controls** are rejected. Native caps remain 3,000 frames and 120 seconds.
- **Six independent WinUAE calls** pass across both images and all three CPUs, 4 KiB stacks and JIT disabled, with explicit expected numbered output and return/IoErr values.

Original-DOS and WinUAE runs used the weighted prototype’s Workbench image. The final restricted Workbench image is byte-identical; the auditor verifies both against the same guest input hashes. These receipts therefore cover the exact installed executable. The MorphOS profile has fresh final vector receipts.

## Reproducible inputs and integration

Stage: `artifacts/search-allocator-20261018`. Compiler baseline: clean **9eaa2aa** in `CopperSharp68k-search-allocation-20261018`; candidate adds only the allocator repair. SDK assemblies, CIL and response flags are frozen. Each comparison uses identical command assemblies and complete adjacent dependencies: MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes, symbols off and unchanged size passes. All six clean-baseline HUNK images reproduce their previously qualified images byte for byte.

An initial relocation probe copied only the root CIL and supplied dependencies elsewhere. That changed sizes even with the retained compiler, so its results were excluded. The compiler has adjacent-directory reflection fallbacks for struct-layout metadata in `CompilationModule`; complete neighboring SDK files are therefore part of the reproducible input layout. The probe and controls remain preserved. This task does not change that metadata-discovery behavior.

The JSON report records compiled toolchain identities, complete inputs, effective passes, optimizer and allocation maps, raw receipts, decoded matcher bodies, tests, native source bindings, source snapshots, the qualified patch and the main-workspace integration delta. Isolated source and tests are retained, and the integration preserves the pre-existing dirty allocator work. The dirty main checkout was not used to build the qualified compiler.

Vector core: Copper68k 1.5.1, SHA-256 `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`. Original-DOS core: `1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63`, SHA-256 `5780f5084fd44e2d4732969bc0f9ce7ae7e3beffc1135ded92bc46d9b21399f2`. WinUAE: 6.0.3.0, SHA-256 `f3e55700fd811cd543fdfebf6c3221cfaa3d385828f110c6d3eb534c64503123`. ROM SHA-256 `8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`.

Use `tools/Commands/qualify_search_allocator.py compile --label narrow`, `vectors --label narrow`, `audit` and `publish` against a fresh frozen stage. The tool refuses existing image destinations, verifies the exact image/CIL/runner identities and stages the complete 61-command inventory before replacing Search. Source rebuilds and superseded prototypes use separate compiler outputs.

## Limits

Other commands have not been qualified with this allocator change and their installed images are retained. Original-DOS and WinUAE coverage exercises Workbench binaries; MorphOS-profile coverage uses supplied-vector fixtures. Original MorphOS/PPC, exhaustive original-command equivalence and CopperScreen checks were unavailable. Main-workspace pair-alias drafts remain unqualified by this clean-base run and use the conservative integration guard. Compiler-wide defaults and command contracts remain unchanged.
