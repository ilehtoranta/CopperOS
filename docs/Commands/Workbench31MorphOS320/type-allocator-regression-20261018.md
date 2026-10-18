# Type allocator size regression — 2026-10-18

## Result

Fixed the allocator's replacement-color ranking in an isolated CopperSharp checkout and integrated the qualified change while preserving unrelated workspace drafts.

| Profile | Before narrow bank repair | Initial repair | Fixed repair |
|---|---:|---:|---:|
| Workbench shipping | 4,696 | 4,700 | 4,696 |
| MorphOS | 4,696 | 4,700 | 4,696 |
| `NativeMorphOSTypeTextIo.Copy` body | 604 | 606 | 602 |

The compiler regression is removed. The fixed images **tie the installed executable size**, so **`out/C/Type` is unchanged**. All 61 installed hashes are retained, totalling **220,920 bytes**. No installed saving is attributed to this fix.

## Why the initial repair grew Type

The streaming helper keeps NUMBER, NOLINE, its byte/line state and several buffer values live across calls. The initial bank repair recovers the incoming NUMBER flag's data register by moving the loop index from D6 to A2. Its traces identify the affected groups as NUMBER v308 and index v222. The allocator subsequently spills the word line-counter phi instead of NUMBER. Both allocations still spill four values and use a 32-byte frame; saved-register storage rises from 36 to 40 bytes.

That repair selected the first legal address color without considering uncolored neighbors' copy preferences. Taking A2 disturbs later loop/phi copy homes and leaves two avoidable register-copy instructions at chunk transitions. Controlled A3 and A4 experiments both remove those copies. The initial helper's net two-byte growth becomes a four-byte executable increase after HUNK alignment.

The fixed helper remains two encoded bytes smaller than the pre-repair helper, but alignment cancels that difference in the complete image. Its saved-register storage remains 40 bytes; this fix removes copy traffic rather than the additional saved register.

## Allocator change

The existing opt-in `SimplifyLoopPhis` repair still relocates at most one eligible long Int32 scalar after ordinary byte/word coloring fails. All fixed-register, pair, reload, GC/byref, reserved-register, clobber and interference exclusions remain.

For equally weighted eligible scalar moves, inspect every legal An destination:

1. Consider only interfering neighbors still on the coloring stack. Already spilled groups do not reserve colors.
2. Predict each neighbor's preferred color using the same allocated-copy preferences, candidate order and forbidden-register checks as ordinary coloring.
3. Sum the loop-weighted demand disturbed by occupying that address color, with saturating arithmetic.
4. Prefer less disturbed demand, retaining deterministic existing order on ties. Keep the scalar's own loop-weighted demand as the primary selection criterion.

This must include flexible long values with preferred address-register copy homes. Two rejected prototypes considered only address-only neighbors, with or without copy preferences; both retained the 4,700-byte regression. Copy affinity matters even when the future value's legal domain includes Dn.

This is a bounded ranking heuristic, not a claim of optimal allocation or universally smaller output. No assignment is committed before a complete legal repair is found. Normal allocation, liveness, preservation and frame verification still run. Compiler-wide defaults and public options are unchanged.

## Qualification

### Compiler regressions

- **721 tests pass** across scalar-repair and register allocation, instruction/CCR dataflow, branch relaxation, resident size execution and frame preservation suites.
- Four new cases cover pending address-only and flexible-copy neighbors, including forbidden preferred colors. Against the preceding compiler, the two positive cases fail and the two forbidden-color cases pass: **19 passed, 2 failed** in the isolated 21-case control. All pass with the fix.
- The compiler assembly exercised by the passing suite is byte-identical to the compiler used to generate the final images.

### Both command profiles on all three CPUs

The unchanged shipping and MorphOS assemblies were run with the original, initially repaired and fixed compilers on **68000, 68020 and 68040**. The Workbench suite contains 17 invocations per image/CPU; MorphOS contains 16. This is **297 unique command invocations** across the three images, both profiles and three CPUs.

Output bytes, returns, IoErr, exact allocation/release and stream ownership observations match. Cases include text/NUMBER, profile-specific NOLINE handling, HEX, short writes, multiple inputs, supplied matcher/provider failures, read/write failures, HEX Ctrl-C, Workbench startup, missing DOS and repeated/interleaved use within the supported fixture scope. No command options or startup handling were changed.

The private streaming root adds **54 unique invocations**: six cases, three compilers and three CPUs. It covers numbered short reads/writes, NOLINE, empty numbered input, text Ctrl-C at the 256-byte boundary and repeated/interleaved streams. These independently compiled helper-root checks supplement the exact shipping-entry checks.

| Corpus, per image/CPU | Calls | Original instructions | Initial repair | Fixed repair | Peak stack: original / initial / fixed |
|---|---:|---:|---:|---:|---:|
| Workbench entry | 17 | 80,313 | 80,355 | 80,337 | 344 / 344 / 344 |
| MorphOS entry | 16 | 81,167 | 81,209 | 81,191 | 344 / 344 / 344 |
| Private text I/O | 6 | 31,407 | 31,749 | 31,694 | 212 / 216 / 216 |

These measurements agree across all three CPU fixtures. The fix removes 18 instructions per command corpus and 55 per private streaming corpus relative to the initial repair. It still costs 24 and 287 instructions respectively relative to the pre-repair allocation. These are executed instruction counts, not wall-clock speed claims. All measured invocations remain within existing configured stack limits; no limits were increased.

### Earlier size beneficiaries

Seventeen additional images compare the final compiler with the preceding narrow-repair compiler using identical retained inputs: both profiles of AddDataTypes, Delete, Dir, Mount, Search, Info, Version and List, plus Copy's sole implemented shipping body.

All retain their preceding size or become smaller. Search's MorphOS profile additionally measures **9,880 → 9,876 bytes**; this task does not qualify or install that further change. Five control images are byte-identical; the other controls are size comparisons, not new runtime-adoption claims. Their installed binaries remain untouched.

## Reproducibility and integration

Evidence directory: `artifacts/type-allocator-20261018`. Clean compiler work starts at **c536d31** in `CopperSharp68k-type-allocation-20261018`. Original/current compilers, accepted CIL, complete adjacent dependencies and the vector executor are copied from the preceding scalar-allocator rollout. Both control profiles reproduce their earlier 4,696/4,700-byte images byte for byte.

Target settings remain **MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes, symbols off**, with the same effective size passes. No command assembly was rebuilt. SDK/compiler dependency identities, responses, code/data and allocation maps, tests, raw execution receipts, source snapshots and the exact compiler patch are recorded in the JSON report.

The checkout requests .NET SDK 10.0.301, which is unavailable here. Builds run from the CopperOS working directory using the installed **10.0.401 SDK**, without editing either repository's SDK selection. The tested compiler artifact is recorded directly. Existing nullable warnings in `M68kSpillAndCopy` and `AhiBindingTests` are unchanged.

Vector execution uses **Copper68k 1.5.1**, SHA-256 `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`. Responses, images, executor and core hashes are verified before accepting receipt comparisons. The auditor also verifies all 61 retained installed images and refuses to claim a shipping update for this size tie.

The canonical CopperSharp working allocator has unrelated pair-alias/reload drafts. Integration retains the existing guard that excludes its bank-repair path when pair aliases are present. Those drafts were not used to build the qualified compiler. The saved clean-base `compiler-fix.patch` contains only the qualified allocator, tests and documentation changes.

Use `tools/Commands/qualify_type_allocator.py` with a fresh `--stage`: `prepare`, `compile` for original/current/candidate compilers, `vectors`, `text_io`, `controls`, then `audit`. Candidate compiler builds, the legacy regression-control project and integration snapshots are retained alongside the raw receipts. Trace and forced-color probes are diagnostic experiments, not shipping compiler changes.

## Limits

These are finite supplied-vector checks, not original DOS or original MorphOS execution. Original-command image equivalence, original-OS Type qualification, CopperScreen, exhaustive line-counter-wrap cases and unsupported interleavings were not checked here. MorphOS sizes are MC68000 measurements without a PPC comparison. The installed Type executable keeps its earlier accepted image because the candidate is not strictly smaller.
