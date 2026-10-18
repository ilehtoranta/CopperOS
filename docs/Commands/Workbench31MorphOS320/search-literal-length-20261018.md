# Search literal pattern length hoisting — 2026-10-18

## Result

| Profile | Before | Accepted | Saved |
|---|---:|---:|---:|
| shipping | 5,208 | 5,192 | 16 |
| morphos | 9,928 | 9,896 | 32 |

Updated `out/C/Search` to **5,192 bytes** after qualification. The complete 61-command inventory falls from **221,016 to 221,000 bytes**; the other 60 image hashes remain unchanged. Against the genuine Workbench 3.1 Search reference (2,476 bytes, identified in the previous rollout receipts), the size ratio is **2.097×**. The MorphOS profile is an independent MC68000 measurement, without a PPC size comparison.

## Implementation and lifetime proof

`Run` already calculates `searchLength` once while validating the SEARCH argument. Literal preparation copies exactly those bytes into the pattern buffer and writes its NUL. The accepted change retains that known length; `LiteralContains` loads it instead of calling `CStringLength` for every line.

The four-byte cache occupies the beginning of the existing 1,024-byte `userPattern` scratch region, immediately after the 1,024-byte pattern buffer. FILE/PATTERN use that scratch as input to DOS pattern compilation and never enter the literal matcher. When both switches are false, the scratch has no remaining compiler use, so literal preparation stores its length there before file traversal. `LiteralPatternLengthOffset` documents the relative location used by the matcher.

The cache, pattern, input, output, anchor and link-work regions are disjoint within the existing allocation. DOS and locale calls in the literal path receive no pointer to this scratch slot; neither line formatting nor matching modifies it. The allocation remains owned until traversal ends and is freed along the existing release path. No allocation size, lifetime, option, output, return, IoErr, startup or stack-limit policy changes. Empty patterns still fail the literal match; the existing argument-length limit still admits at most 1,022 literal bytes.

The first cached-length candidate exposed a register-allocation consequence: the Workbench matcher moved its separate `matched` boolean to a four-byte stack home. This added a store for each attempted start position and made the long-line case 8,118 instructions slower despite lower aggregate instructions. The final matcher uses its final index to identify a complete match, removing that boolean. Byte reads, case folding and locale conversion retain their order; a mismatch still exits at the same character.

| Matcher | Workbench body / frame / spilled values | MorphOS body / frame / spilled values |
|---|---:|---:|---:|
| Baseline | 128 / 0 / 0 | 206 / 8 / 3 |
| Cache with boolean | 120 / 4 / 1 | 196 / 8 / 3 |
| Index result only | 118 / 0 / 0 | 182 / 8 / 2 |
| Cache and index result | 110 / 0 / 0 | 174 / 8 / 2 |

Body sizes are decoded encoded bytes; frame/spill columns are allocator map records. Complete-executable measurements determine acceptance.

## Experiments

| Variant | Workbench HUNK | MorphOS HUNK | Outcome |
|---|---:|---:|---|
| Frozen source baseline | 5,208 | 9,928 | Control |
| Length once per formatter | 5,240 | 9,992 | Rejected: larger |
| Forward existing length through helpers | 5,236 | 9,976 | Rejected: larger |
| Invocation scratch cache | 5,200 | 9,920 | Qualified; superseded |
| Index result, original length scan | 5,196 | 9,904 | Qualified control |
| Scratch cache and index result | 5,192 | 9,896 | Accepted |

The invocation cache avoids carrying another argument through the line/file helpers. After the index-result change, hoisting contributes a further 4 bytes in Workbench and 8 bytes in the MorphOS profile relative to the independently measured index-only variant. Compiler allocation, peephole and image alignment effects make these complete-image deltas non-additive. All three smaller variants passed both profile vector suites on all three CPUs. Only the final combined variant is installed.

## Qualification and execution costs

The final matrix contains **522 invocations** across baseline/candidate, both profiles, and **68000/68020/68040**. The following counts are per image and per CPU; instruction and peak-stack measurements agree across all three CPU fixtures.

| Profile | Calls | Instructions before → after | Peak stack before → after |
|---|---:|---:|---:|
| shipping | 30 | 3,238,399 → 3,196,067 | 472 → 472 |
| morphos | 57 | 13,260,304 → 13,182,353 | 712 → 712 |

Workbench corpus instructions decrease 1.31%; MorphOS decreases 0.59%. These are fixture instruction counts, not wall-clock speed claims. The retained setup store adds one instruction to two Workbench cases and nine MorphOS cases where matching never begins. Every other measured case is equal or faster, including the long-line case. No peak stack increase or stack-limit violation occurs.

| Target case | Workbench instructions before → after | MorphOS instructions before → after |
|---|---:|---:|
| literal-many-short-lines | 15,695 → 10,883 | 24,440 → 19,678 |
| literal-maximum-length | 145,548 → 127,110 | 208,414 → 179,740 |
| literal-empty-pattern | 1,500 → 1,444 | 2,123 → 2,070 |

Workbench's existing long-line case improves from 738,954 to 722,504 instructions.

- Added cases cover 64 short misses followed by a match, a maximum-length 1,022-byte literal following a shorter line, and an empty literal with empty/unterminated lines. Interleaved invocations now deliberately use different literal lengths.
- The fixture SEARCH string moved to its own 1,024-byte region inside each process slot. Its old location overlapped the path/device/traversal fixtures for longer strings; this adjustment enables valid maximum-length testing for both baseline and candidate.
- Existing cases retain FILE/PATTERN parsing, locale case/control/printable bytes, buffer boundaries, output ordering, short writes, second-chunk failure, empty files, parser/allocation/I/O errors, Ctrl-C, traversal, Workbench startup and supported repeated/interleaved use.
- Exact comparisons retain output bytes, return values, IoErr, allocation/release and other command-visible observations. Stack use is measured and checked separately.
- **192 original Kickstart 3.1 DOS calls** pass across both images and all three CPUs, using 4 KiB and 8 KiB stacks. The unchanged native corpus includes multiple inputs, traversal, parser/I/O errors, Ctrl-C and the 8,193-byte line. Existing 3,000-frame/120-second caps are unchanged. Four auditor mutation controls are rejected.
- **Six independent WinUAE calls** pass across both images and all three CPUs, using 4 KiB stacks and JIT disabled. Redirected numbered output and return/IoErr values match explicit expected bytes. The independent corpus is the existing small confirmation, not the complete vector suite.

## Inputs, reproduction and limits

Frozen stage: `artifacts/search-literal-length-20261018`. Starting source revision: `ee33f41`. Compiler and SDK assemblies come from the previously qualified receiver-fixed **9eaa2aa** snapshot and are checked against the retained receipt chain. Both source baselines reproduce their prior qualified images byte for byte. The repository source rebuild reproduces both final qualified images byte for byte. Unrelated workspace changes were preserved.

Compiler policy is unchanged: MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes, symbols off and the accepted per-command size passes. The JSON report records compiler/SDK identities, response files, effective passes, rewrites, map measurements, decoded matcher bodies, fixture/source/build identities, runtime bindings, raw receipt hashes and the complete installed inventory.

Vector core: Copper68k 1.5.1, SHA-256 `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`. Original-DOS core: `1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63`, SHA-256 `5780f5084fd44e2d4732969bc0f9ce7ae7e3beffc1135ded92bc46d9b21399f2`. WinUAE: 6.0.3.0, SHA-256 `f3e55700fd811cd543fdfebf6c3221cfaa3d385828f110c6d3eb534c64503123`. ROM SHA-256 `8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`.

Use `tools/Commands/qualify_search_literal_length.py` with `prepare`, `variants`, `build`, `vectors --expanded`, `native`, `audit` and `publish`. Select `--label cached-index` for final qualification/publication; `build --sources` supports the repository source proof. Preserve prior stages and labels. The tool stages and checks all 61 installed images before replacing Search.

The MorphOS source profile uses supplied-vector fixtures. Original-DOS and WinUAE tests exercise the Workbench profile against original OS services. An original MorphOS/PPC run, exhaustive original Workbench command comparison and CopperScreen checks were unavailable. Maximum-length and interleaved tests are vector coverage. The retained lightweight emulator binary remains authoritative where local source differs; frozen host/guest source bindings match. No compiler transformation or policy change is introduced.
