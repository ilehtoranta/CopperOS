# Search hot-loop normalization and copies — 2026-10-18

## Accepted images

| Profile | Installed baseline | Accepted | Saved |
|---|---:|---:|---:|
| shipping | 5,144 | 5,100 | 44 |
| morphos | 9,708 | 9,664 | 44 |

Updated only `out/C/Search`. The complete 61-command inventory is **220,812 → 220,768 bytes**. The other 60 installed hashes remain unchanged. List was compiled and qualified as a control; its additional 16/8-byte reductions are retained as measurements without adopting another command in this task.

The retained genuine Workbench 3.1 Search reference is **2,476 bytes**: the accepted shipping image is **2.060×** that size. MorphOS-profile sizes are independent MC68000 measurements, without comparing against native PPC code.

## Cause and fix

The allocator improvement exposed two separate emitter/optimizer gaps. Value-range transfer forgot the known upper 24 bits after memory byte loads. That prevented the existing boolean narrowing from replacing `SEQ; EXT.W; EXT.L; NEG.L` with `SEQ; NEG.B`. Zero staging also survived around an OR of a proven zero operand. Retaining the upper bits, removing redundant zero transport, and preserving live flags with a test eliminates five added instructions per ordinary shipping scanner byte.

A copied register was retained even when its sole use was a comparison or arithmetic source. The bounded forwarding proof substitutes the original register when it remains unchanged and no flags or alternate entry are lost. The last matcher copy required tracking which copied bits remain observable through later byte/word writes. Its internal `Fold` helper preserves the register without reading it; the helper’s return ABI annotation had kept it conservatively live. A bounded leaf check plus the caller’s actual consumers permits forwarding while rejecting real reads, saves, clobbers, opaque instructions and nested calls.

All four added shipping matcher copies are eliminated. `LiteralContains` returns to its pre-allocator 110-byte body. The new proof does not change register assignments, spill constraints, command source, options, startup, ownership, stack limits or build policy. Rewrites remain under the existing generated-code policy and eligibility gates; the more precise byte facts and explicit call-clobber invalidation are shared analysis corrections.

| Hot method | Baseline body | Accepted body |
|---|---:|---:|
| shipping/FormatLines | 490 | 478 |
| shipping/LiteralContains | 118 | 110 |
| morphos/FormatLines | 898 | 894 |
| morphos/LiteralContains | 178 | 170 |

Body savings overlap with image layout, branch sizing and HUNK padding. The final two-byte matcher improvement changes the image hash while leaving its padded byte count unchanged from the first prototype; only the final exact images are accepted.

## Execution costs and stack

| Profile | Calls per image/CPU | Instructions before → accepted | Change | Peak stack bytes |
|---|---:|---:|---:|---:|
| shipping | 30 | 3,571,656 → 3,060,798 | -14.30% | 472 → 472 |
| morphos | 57 | 13,309,815 → 13,239,183 | -0.53% | 696 → 696 |

Instruction totals and stack peaks agree on 68000, 68020 and 68040. These are finite-fixture instruction counts, not cycle or wall-clock timing claims. No stack limit changed.

| Profile | Before the previous allocator change | Current accepted | Change |
|---|---:|---:|---:|
| shipping | 3,195,954 | 3,060,798 | -4.23% |
| morphos | 13,191,464 | 13,239,183 | +0.36% |

Shipping Search now executes fewer fixture instructions than before the allocator size reduction. The MorphOS total still includes a small residual increase against that earlier compiler; this work removes the identified transport/normalization sequences and does not claim to eliminate every cost of its different allocation.

## Qualification

**40 new compiler cases pass**: 39 execution cases and one direct call-clobber analysis case. The execution cases compare before/after images on all three CPUs with all 32 initial CCR combinations including X, varied data bytes/words, all registers, final flags, stack restoration, and guest access counts/width/order. Direct range assertions cover memory and immediate byte loads. The same cases against the frozen baseline have **21 expected failures and 19 passes**; negative safety controls pass. **1,529 existing compiler regressions pass** against the exact accepted compiler, covering dataflow/CCR, allocation, generated/resident size transformations, branch relaxation, relocation and frame preservation.

The final shipping and MorphOS Search entries pass **30 and 57 vector invocations per image/CPU**, respectively, on 68000/68020/68040: **522 Search invocations** across baseline and accepted images. Including List controls, **948 vector invocations** pass. Output bytes, return codes, IoErr, releases and supported repeated/interleaved invocations match; supplied startup, parser, allocation/I/O failure and Ctrl-C cases remain covered.

The final exact shipping image passes original Kickstart 3.1 DOS checks on all three CPUs: **192 Search invocations** at 4/8 KiB stacks, plus **6 independent WinUAE Search invocations**, JIT disabled and 4 KiB stacks. The four Search auditor mutation controls are rejected. List controls add 300 original DOS invocations, six smoke invocations, 18 WinUAE invocations and four auditor controls. The receipts independently bind input images, ROM, harness and outputs; the final image was rerun after the last matcher refinement.

## Reproducibility and limits

The isolated compiler checkout starts at `daab9bc` plus the already qualified feasible-color patch. All comparisons use identical accepted CIL assemblies and adjacent dependencies. Fresh baselines reproduce the installed Search and List images and retained MorphOS qualification images. Command sources are unchanged. Flags remain MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes, symbols off, existing size passes and stack limits.

The accepted compiler DLL is byte-identical across CLI compilation, focused regressions and the frozen full regression runner. Compiler/source patch hashes, SDK/CIL identities, flags, rewrite counts, method and relocation metrics, maps, disassembly and receipts are preserved in the JSON and staging directory. Earlier prototypes, trace compilers and failed test-observer setups are retained separately and are not qualification evidence. Test observations now declare exact SR-read effects and make every final register/CCR observable.

.NET SDK `10.0.401`; vector Copper68k `1.5.1`, SHA-256 `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`. Native core `1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63`, SHA-256 `5780f5084fd44e2d4732969bc0f9ce7ae7e3beffc1135ded92bc46d9b21399f2`. WinUAE `6.0.3.0`, SHA-256 `f3e55700fd811cd543fdfebf6c3221cfaa3d385828f110c6d3eb534c64503123`. Kickstart SHA-256 `8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`.

Original MorphOS/PPC, original-command-image and CopperScreen checks were unavailable. Original DOS/WinUAE checks cover shipping Workbench entries; the alternate profile receives the vector checks. These finite corpora do not establish exhaustive equivalence, a new minimum stack, or unsupported interleaving guarantees. Other commands are not adopted with this compiler change.

The qualified incremental compiler patch is integrated into the main CopperSharp workspace while preserving unrelated drafts; that dirty workspace was not used for qualification. The complete 61-command accepted set was staged, inventory-checked and hash-checked before updating only Search.

Evidence: `artifacts/search-hotloops-20261018`. Tool: `tools/Commands/qualify_search_hotloops.py` (`prepare`, `compile`, `vectors`, `native`, `inspect`, `audit`, `integrate`, `publish`, `report`).
