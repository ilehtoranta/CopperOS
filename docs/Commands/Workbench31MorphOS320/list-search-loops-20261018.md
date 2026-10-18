# List and Search loop optimization — 2026-10-18

## Accepted results

| Command/profile | Before | After | Saved |
|---|---:|---:|---:|
| List/shipping | 4,616 | 4,604 | 12 |
| List/morphos | 4,736 | 4,724 | 12 |
| Search/shipping | 5,256 | 5,208 | 48 |
| Search/morphos | 10,016 | 9,928 | 88 |

Refreshed `out/C/List` and `out/C/Search`. All 61 commands remain present. Total installed size: **221,076 → 221,016 bytes**, saving 60 bytes. The other 59 images retain their hashes.

Workbench 3.1 media references remain List 5,108 bytes and Search 2,476 bytes, identified in the receiver-classification rollout receipt. New ratios are 0.901× and 2.103× respectively. MorphOS profiles receive independent MC68000 measurements; there is no PPC size comparison.

## Changes and proofs

### List: fuse bounded comment scanning and copying

The old `FormatComment` first scanned up to 80 bytes for NUL, then read the comment again while copying it. The accepted loop reads each character once, reuses the first byte for the empty-comment decision, and stops at NUL or 80 bytes. The FIB comment and destination workspace regions are disjoint and stable during the helper. Colon insertion, the final NUL, and the 80-byte limit are unchanged. `FormatComment` falls from 96 to 84 encoded bytes in both profiles.

### Search: admit output chunks and remove implied line bounds

The private output helper receives the invocation-owned, nonzero 8,192-byte buffer through `FormatLines` or `FormatMorphosChunk`. It admits `min(remaining, capacity)` once per chunk, then performs byte conversion and stores without a per-byte flush check. Source and output storage are disjoint. The same bytes are converted in order; each full or final partial chunk reaches the existing `WriteBuffer` before the next source byte is read. The separate newline write, buffered-output flushes, short-write retries, failure handling and locale calls are preserved.

Each entered line-scan loop consumes at least one byte and maintains `offset <= sourceLength`. Therefore `offset > start` and the repeated upper-bound checks after that scan are implied. The incomplete MorphOS line exits before formatting, so the remaining path always has a delimiter and subtracts one byte. The actual scan bounds, delimiter reads, temporary pattern NUL and restoration remain. No per-byte locale dispatch was duplicated.

| Method | Workbench before → after | MorphOS before → after |
|---|---:|---:|
| FormatLines | 484 → 454 | 968 → 932 |
| FormatMorphosChunk | unreachable | 908 → 868 |
| WriteLine | 230 → 212 | 340 → 326 |

## Independent measurements

| Experiment | Workbench bytes | MorphOS bytes | Decision |
|---|---:|---:|---|
| List baseline | 4,616 | 4,736 | Control |
| List invariant row formats | 4,672 | 4,792 | Rejected: larger |
| List invariant row-record offsets | 4,648 | 4,768 | Rejected: larger |
| List first fused comment loop | 4,616 | 4,736 | Measured separately; superseded |
| List cached-byte comment loop | 4,604 | 4,724 | Accepted |
| Search baseline | 5,256 | 10,016 | Control |
| Search output chunks only | 5,236 | 10,004 | Measured separately; superseded |
| Search implied line bounds only | 5,224 | 9,940 | Measured separately; superseded |
| Search combined | 5,208 | 9,928 | Accepted |

Hoisting List formats and record offsets extends live ranges across directory traversal and DOS calls. Their resulting images grow by 56 and 32 bytes; those source variants were rejected. The first fused comment loop was size-neutral and passed both profile suites; the cached-byte revision saves 12 bytes. Both independent Search changes passed both profile suites on all three CPUs before the combined candidate was accepted. Whole-image savings include layout, alignment and peephole interactions; individual deltas must not be added as predictions.

The Workbench build already specializes MorphOS dispatch away. Additional CASE/locale loop duplication has no demonstrated shipping-image benefit. Literal-pattern length caching remains a possible later experiment; it would extend state through line/file helpers. Variable escaped-output growth in List SUB still requires validation; these checks were retained.

## Qualification and execution costs

Every expanded vector comparison passes on **68000, 68020 and 68040**. The table gives counts and measurements **per image, per CPU**; these instruction and stack values are identical across the three CPU fixtures.

| Profile | Calls | Instructions before → after | Peak stack before → after |
|---|---:|---:|---:|
| List/shipping | 36 | 49,459 → 46,667 | 264 → 264 |
| List/morphos | 35 | 53,240 → 50,448 | 264 → 264 |
| Search/shipping | 27 | 3,797,817 → 3,075,560 | 476 → 472 |
| Search/morphos | 54 | 13,619,999 → 13,025,228 | 712 → 712 |

The final matrix contains **912 invocations** across both images, both profiles and three CPUs. No measured case increases instructions. Workbench corpus instructions decrease 5.64% for List and 19.02% for Search; MorphOS decreases 5.24% and 4.37%. These are fixture instruction counts, not wall-clock speed claims. Existing stack limits remain enforced.

- New List vectors cover comment lengths 0, 1, 79 and 80.
- New Search vectors cover line lengths 8,191, 8,192, 8,193 and 16,385, plus a failed second-chunk write with exact prior output and call accounting.
- Existing fixtures retain short writes, flush/write errors, empty and incomplete input, parser/allocation/I/O failures, Ctrl-C, locale/control/printable bytes, traversal, startup ownership, repeated and supported interleaved invocations.
- Comparisons retain output bytes, return values, IoErr, allocation/release and other command-visible observations. Image and map identities are tied to each execution receipt.
- Original Kickstart 3.1 DOS: **498 calls** (List 306, Search 192) across both images and all three CPUs, including 4 KiB/8 KiB stacks and the existing 8,193-byte Search line. All pass within the existing 3,000-frame and 120-second limits. Eight auditor mutation controls are rejected.
- Independent WinUAE: **24 calls** across all three CPUs, CLI startup, 4 KiB stacks and JIT disabled. List QUICK/metadata/TO output and Search redirected numbered output/returns pass against their explicit expected bytes. This is the existing small independent corpus, not the complete vector suite.

## Frozen inputs and reproduction

Stage: `artifacts/list-search-loops-20261018`. The JSON report records compiler/SDK hashes, response-file identities, effective passes and rewrite counts, source/build snapshots, source bindings, native runtimes, raw receipt hashes, trial images, map measurements and installed inventory. The accepted source rebuilds reproduce the qualified images byte for byte in both profiles.

Compiler: retained receiver-fixed **9eaa2aa**, checked against the prior qualification manifest. No dirty compiler or SDK checkout was rebuilt. MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes, symbols off, unchanged per-command code-size policy. Shipping source baselines reproduce the installed images byte for byte. MorphOS baselines use rebuilt minimal private entry roots with the same helper dependencies for baseline and candidate; their sizes match the previous qualified aggregates, while assembly layout changes their hashes. All rebuilt baselines pass the explicit fixture expectations.

Vector core: Copper68k 1.5.1, SHA-256 `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`. Original-DOS core: `1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63`, SHA-256 `5780f5084fd44e2d4732969bc0f9ce7ae7e3beffc1135ded92bc46d9b21399f2`. WinUAE: 6.0.3.0, SHA-256 `f3e55700fd811cd543fdfebf6c3221cfaa3d385828f110c6d3eb534c64503123`. ROM SHA-256 `8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`.

Use `tools/Commands/investigate_list_search_loops.py` with `prepare`, `build`, `vectors`, `native`, `audit` and `publish`. Builds accept an explicit source snapshot and label. `vectors --runner` writes expanded receipts to fresh directories. Preserve existing stages; new source or fixture revisions require new labels/stages. Publication verifies a complete 61-image staging inventory before copying the two accepted files.

## Limits

Original-DOS and WinUAE checks run the Workbench-profile baseline and candidate on original OS services. The MorphOS source profile uses supplied-vector fixtures; an original MorphOS OS/PPC comparison and CopperScreen checks were unavailable. Original Workbench command images supply size references, not an exhaustive behavioral oracle. Source bindings confirm the frozen host and guest; the retained lightweight emulator binary is authoritative where its local source differs. No new compiler transformation or policy enablement is included.
