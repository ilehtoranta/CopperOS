# List/Search stack size audit — 2026-10-05

**61 commands: 245,044 → 245,020 bytes.**
Only List and Search enable the new pass. Command sources and frozen assemblies are unchanged.

## Accepted reduction

| Command | Before | After | Saved | Workbench 3.1 | Ratio |
| --- | ---: | ---: | ---: | ---: | ---: |
| List | 10,640 | 10,620 | 20 | 5,108 | 2.08× |
| Search | 10,668 | 10,664 | 4 | 2,476 | 4.31× |

`CompactPrivateStackCopies` folds adjacent private stack load/store pairs into one memory-to-memory MOVE when the intermediate register is dead. Width, read-before-write ordering and final NZVC/X are preserved. Word MOVEA, SP updates, alternate entries, metadata barriers and unsupported runtime profiles remain excluded. The public switch defaults to false; CLI/response-file selection and CopperOS per-command settings use its name.

Compiler starts at clean 3a05e8b76cb55a5420189265b0acc0005798c074; final source is f7986bf475b9f6a43e0a7353cdfe57adc33f20f1 on codex/list-search-stack-size. Use that isolated compiler for builds with the new selection. MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes and symbols disabled; SDK/input hashes and complete flags are retained in the JSON.

List records 11 rewrites (22 local bytes); its final image saves 20 bytes after layout/alignment. Search records two rewrites and saves four bytes. Local rewrite deltas are not final HUNK savings.

## What dominates the main methods

| Command | Main body bytes | Frame | Saves | Spill area | Reloads | Allocation rounds |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| List | 8,118 | 236 | 40 | 68 | 537 | 5 |
| Search | 7,434 | 172 | 40 | 100 | 530 | 5 |

These are final allocated IR counts before peepholes. Spilled-value counts accumulate across allocation rounds; they are not distinct permanent stack slots. Address-taken locals are not necessarily escaped locals. New ALLOCATION, FRAME-SLOT and MACHINE-OP map records expose the frame homes and operation counts without changing executable bytes.

The audit identifies register pressure as a major source of traffic: both main methods contain over 500 spill reloads. Existing memory promotion already runs before and after inlining; a further promotion pass needs proof for the remaining address-taken homes. List also constructs four formatting records before selecting one; eliminating redundant transport alone cannot close its Workbench gap.

## Rejected experiments

- Static spill weights instead of loop-weighted costs: List grew 24 bytes; Search grew 104 bytes. Reverted.
- Reusing identical reloads, including different destination registers and disjoint stack stores: neither image became smaller. Reverted.

## Qualification and execution costs

- 1642 focused compiler regressions passed, including six branch-liveness tests, all 32 CCR combinations, partial-register cases, memory effects, stack contents, aliases, HUNK relocation and repeated invocation.
- Native baseline/candidate comparisons passed on 68000, 68020 and 68040: 32 List and 19 Search invocations per CPU (153 comparisons). Output, return codes, IoErr and allocation/resource events match.
- List fixture instructions: 64,746 → 64,306 per CPU; peak stack 356 → 356 bytes.
- Search fixture instructions: 939,337 → 939,327 per CPU; peak stack 520 → 520 bytes.
- Copper68k package 1.5.1, actual product 1.5.1+43a3a84f79657cb4c2b32bb58d8e510e3aec9c15; core SHA256 99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f.
- All 61 images rebuilt with identical frozen managed inputs; the other 59 are byte-identical to their accepted baseline. Inventory and hashes are verified before refreshing out/C.

## Limits and next work

The exploratory full compiler suite did not pass. A clean unchanged 3a05e8b checkout reproduced 53 failures across CopperScreen integration, RunCommand stack bridge, framework identity and .NET profile baseline tests; its receipt is preserved. Other exploratory full-suite failures were not individually investigated, and the full run was stopped. Qualification here uses the passing focused suite and shipping command comparisons.

Original Workbench executable execution and CopperScreen checks remain unavailable. Reference sizes are the previously admitted genuine Workbench HUNK measurements. Native fixtures cover the shipping Workbench profiles; no PPC comparison or new MorphOS-native qualification is claimed. Command source profiles are unchanged.

Next investigate List formatting-home lifetimes and allocator call pressure using the recorded frame slots. Candidate improvements must preserve options and ownership, and be measured independently before acceptance.
