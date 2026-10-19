# List and Search remaining register spills — 2026-10-18

## Accepted images

| Command | Shipping before → accepted | Saved | MorphOS before → accepted | Saved |
|---|---:|---:|---:|---:|
| List | 4,604 → 4,540 | 64 | 4,724 → 4,716 | 8 |
| Search | 5,188 → 5,144 | 44 | 9,876 → 9,708 | 168 |

Updated only `out/C/List` and `out/C/Search`. The complete 61-command inventory is **220,920 → 220,812 bytes**, saving **108 bytes**. All other installed hashes are unchanged. No command-source or build-policy changes were made.

Retained genuine Workbench 3.1 media size references: List **5,108 bytes** (accepted image **0.889×**); Search **2,476 bytes** (accepted image **2.078×**). The original media identities and versions are retained in the JSON. These are size references; no new original-command executions are claimed.

## Cause and compiler fix

The allocator compared each group’s available-color count, which excludes forbidden registers, with a degree estimate that still counted conflicts on those forbidden colors. Call-clobbered, fixed and reserved registers could inflate the estimated pressure even when the two values had no usable register in common. This could change the simplification order and choose an avoidable spill.

Under the existing opt-in `SimplifyLoopPhis` policy, scalar degree estimates now intersect each group’s allowed registers with the complement of its forbidden registers before counting a conflict. The interference graph is retained in full for allocation and verification. Register pairs retain the previous approximation; policy-off clients retain their previous behavior. No register, clobber, alias, resource, resident-context or frame constraint is relaxed.

The new four-value reproductions cover byte, word, long data and address-register domains. All four enabled cases fail on clean `daab9bc` and pass with the fix; their four policy-off controls retain the prior allocation. **729 compiler regressions pass**, including scalar repair, allocation, instruction/CCR dataflow, branch relaxation, resident execution and frame preservation.

## What remains spilled

List’s main routine and Search’s main/formatting routines remain the pressure centers. Many traced live-through groups have forbidden mask `0x630f`, leaving D4–D7 and A2–A4; byte flags can use only the data registers. Long-lived options and loop/ownership state still compete with short-lived values and call conventions. These finite traces do not establish a minimum possible spill count.

| Main routine/profile | Spilled SSA values | Reloads | Frame bytes | Encoded body bytes |
|---|---:|---:|---:|---:|
| List/shipping | 20 → 21 | 66 → 57 | 148 → 152 | 2156 → 2078 |
| List/morphos | 21 → 22 | 66 → 65 | 148 → 152 | 2252 → 2230 |
| Search/shipping | 27 → 27 | 98 → 78 | 140 → 132 | 2216 → 2108 |
| Search/morphos | 33 → 36 | 159 → 118 | 176 → 160 | 2870 → 2718 |

Spilled SSA-value counts are not stack-access counts. Some counts increase while reload traffic and complete image size fall. Search’s shipping formatter grows 450 → 490 bytes, and some other small helpers grow; the complete executable still wins. Display names omit overload signatures, so the report retains occurrence indices instead of collapsing the main `Run` routine into a tiny overload. All method records and code/data/relocation metrics remain in the JSON and maps.

## Rejected experiments

| Compiler experiment | List shipping/MorphOS | Search shipping/MorphOS |
|---|---:|---:|
| Clean daab9bc | 4,604/4,724 | 5,188/9,876 |
| Full-width data-to-address repair | 4,604/4,724 | 5,192/9,904 |
| Full-width repair in either direction | 4,604/4,724 | 5,188/9,908 |
| Feasible scalar degree (accepted) | 4,540/4,716 | 5,144/9,708 |

Both broader repair candidates grew Search and were rejected without adoption. Their patches, compilers, maps and images are preserved. The accepted compiler contains only the feasible-degree change, its regression tests and documentation.

## Qualification and execution costs

The exact shipping and MorphOS-profile executables pass supplied-vector fixtures on **68000, 68020 and 68040**. Output bytes, return codes, IoErr, command-visible ownership/releases and supported repeated/interleaved calls match. Workbench startup, parser/I/O/allocation failures and Ctrl-C paths are checked where supplied by the existing fixtures. Every peak stack remains within its existing limit.

**1,146 unique vector invocations** include both compiler baselines and the actual installed shipping images, which differ in bytes from the size-neutral `daab9bc` baselines. Candidate executions reused between these comparisons are counted once.

| Command/profile | Calls per image/CPU | Instructions before → after | Change | Peak stack before → after |
|---|---:|---:|---:|---:|
| List/shipping | 36 | 46,667 → 45,798 | -1.86% | 264 → 272 |
| List/morphos | 35 | 50,448 → 49,676 | -1.53% | 268 → 272 |
| Search/shipping | 30 | 3,195,954 → 3,571,656 | +11.76% | 476 → 472 |
| Search/morphos | 57 | 13,191,464 → 13,309,815 | +0.90% | 712 → 696 |

The displayed per-CPU totals agree on all three CPUs. Shipping “before” is the actual installed image; alternate-profile “before” is the frozen compiler baseline. Size takes priority over execution speed: Search’s additional instructions are retained explicitly. These are instruction counts, not cycle or wall-clock timing measurements.

Original Kickstart 3.1 DOS: **492 invocations** across the shipping List/Search images, all three CPUs, the declared finite corpora and 4/8 KiB stacks; the extra six-invocation List smoke run also passes. Eight auditor mutation controls are rejected. Independent WinUAE checks pass **24 invocations** across all CPUs with JIT disabled and 4 KiB stacks. Output files and return/IoErr records are independently rechecked against the contracts.

## Frozen inputs and limits

Compiler work starts from clean `daab9bc` in `CopperSharp68k-list-search-spills-20261018`. Each comparison uses the same accepted CIL assemblies and complete adjacent dependencies from the preceding rollout. All four fresh baselines reproduce the retained `daab9bc` images, and trace instrumentation is verified byte-neutral. The final candidate compiler is byte-identical to the compiler used by the regression suite; its images reproduce the qualified prototype exactly. The qualified patch is integrated into the main CopperSharp workspace while preserving its unrelated allocator and runtime drafts; that dirty checkout was not built.

Flags: MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes, symbols off, unchanged per-command size passes and stack limits. SDK/assembly identities, compiler patch hashes, effective passes, rewrite counts, largest methods, frame records and relocation counts are preserved.

Actual .NET SDK: `10.0.401`. Vector core: Copper68k `1.5.1`, SHA-256 `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`. Native OS core: `1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63`, SHA-256 `5780f5084fd44e2d4732969bc0f9ce7ae7e3beffc1135ded92bc46d9b21399f2`. WinUAE `6.0.3.0`, SHA-256 `f3e55700fd811cd543fdfebf6c3221cfaa3d385828f110c6d3eb534c64503123`. ROM SHA-256: `8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`.

MorphOS profiles are MC68000 qualification images, not comparisons with PPC executables. No original MorphOS/PPC, original-command-image or CopperScreen checks were available. Original DOS/WinUAE checks cover the shipping Workbench entries; alternate profiles receive the vector checks. No exhaustive equivalence, new minimum stack or unsupported-interleaving guarantee is inferred. Other commands have not been adopted with this compiler change.

Evidence: `artifacts/list-search-spills-20261018`. Tool: `tools/Commands/investigate_list_search_spills.py` (`prepare`, `compile`, `vectors`, `native`, `audit`, `integrate`, `publish`, `report`). Compiler probe build logs and regression receipts are retained. The complete accepted 61-command set is staged and hash-checked before refreshing only the two accepted executables.
