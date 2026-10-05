# Generated MC68000 size optimization — 2026-10-05

**61 commands: 247,684 → 245,044 bytes; 2,640 bytes saved (1.07%).**
**Execute: 6,376 → 6,168 bytes.** 57 commands use qualified new selections; others retain their baseline policy and bytes.

The baseline already contains accepted command-source reductions. Every comparison uses the same frozen managed assemblies and dependencies. No command source was changed by this compiler work.

## Toolchain and controls

- Compiler starts at clean `b73f3794246be849706492df0bab2e2ba24636ec`; final source is `3a05e8b76cb55a5420189265b0acc0005798c074` on `codex/code-generator-size` in the isolated compiler checkout.
- MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes, symbols disabled, existing stack limits. Baseline reproduction is byte-identical for all 61 commands.
- New `M68kCodeSizeOptions` switches default to false. CLI/response-file `code-size-passes` and CopperOS `CopperOSCodeSizePasses` select them independently. Legacy ROM options and mutual exclusion are retained.
- The JSON records frozen SDK/input/compiler identities, source and patch hashes, flags, maps, rewrite counts, relocations, largest methods, selection trials and execution receipt hashes.
- `GENERATED-SIZE-REWRITE local-byte-delta` is a local pre-layout measurement, not additive final-image savings. Removed initialization records private storage bytes separately.

## Implemented passes

| Pass | Independent control | Qualified implementation |
| --- | --- | --- |
| A | RemoveRedundantTransport | Full register overwrites, private stack writebacks and self-moves; live NZVC uses a shorter test preserving X. |
| B | CompactGuestMemory | Liveness-proven indexed guest accesses, direct memory tests, retained APTR offsets for indexed long reads and byte/word/long writes. |
| C | NarrowOperations | Remove widening masks for proven low-only stores; retain full-width, address, call and merge consumers. Existing narrow comparisons and private-memory updates remain active. |
| D | EliminateRedundantInitialization | Per-home byte analysis through control flow and calls, with escape/alias/observation checks; implicit initialization is removed only when unobserved or overwritten. |
| E | SizeFirstCosts | Byte-first frame-clear planning and arithmetic/helper costs. A one-store DBRA loop can replace speed-oriented unrolling. |
| F | InlineMemoryHelpers | Restricted private single-use memory helpers and encoded-byte decisions for private leaf helpers with internal branches; keep function-address identity and NoInlining exclusions. |
| G | ShareArithmeticCores | Shared full-width unsigned division cores, existing quotient/remainder fusion and proven bounded unsigned register DIVU. |

EH, GC, dynamic stack and unsupported alias cases stay excluded. More aggressive early byte/word indexed emission grew images and was rejected; late liveness folding retains those opportunities safely.

## Independent and cumulative measurements

These are complete experimental images before per-command rejection. The matrix used a frozen compiler snapshot; the final selection was rebuilt and qualified with the final source. B-final also measures the later indexed emitter.

| Selection | All 61 bytes | Execute | Eval | List | Search | Accepted smaller commands |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| control | 247,684 | 6,376 | 9,980 | 10,648 | 10,760 | 0 |
| A | 246,644 | 6,284 | 9,920 | 10,640 | 10,720 | 23 |
| B | 247,304 | 6,348 | 9,964 | 10,644 | 10,744 | 34 |
| C | 247,564 | 6,332 | 9,980 | 10,648 | 10,748 | 10 |
| D | 247,696 | 6,340 | 10,012 | 10,764 | 10,744 | 35 |
| E | 247,084 | 6,368 | 9,964 | 10,640 | 10,752 | 50 |
| F | 247,472 | 6,372 | 9,980 | 10,644 | 10,748 | 14 |
| G | 247,496 | 6,364 | 9,916 | 10,648 | 10,760 | 7 |
| AB | 246,264 | 6,256 | 9,904 | 10,636 | 10,704 | 36 |
| ABC | 246,144 | 6,212 | 9,904 | 10,636 | 10,692 | 37 |
| ABCD | 246,184 | 6,176 | 9,936 | 10,756 | 10,676 | 39 |
| ABCDE | 245,992 | 6,176 | 9,936 | 10,756 | 10,672 | 40 |
| ABCDEF | 245,772 | 6,168 | 9,936 | 10,752 | 10,656 | 41 |
| combined | 245,584 | 6,152 | 9,872 | 10,752 | 10,656 | 42 |
| B-final | 247,448 | 6,364 | 9,968 | 10,656 | 10,756 | 27 |

## Accepted images and remaining Workbench ratios

A–G identify the new switches above. “Baseline” keeps the previous selection. Ratios use genuine admitted Workbench 3.1 HUNK sizes; MorphOS-only commands have no PPC comparison.

| Command | Before | After | Saved | New passes | WB 3.1 | Ratio |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| AddBuffers | 1,084 | 1,084 | 0 | Baseline | 444 | 2.44× |
| AddDataTypes | 7,932 | 7,860 | 72 | ABCDE | 5,880 | 1.34× |
| Assign | 2,124 | 2,100 | 24 | ABCDE | 3,220 | 0.65× |
| Avail | 1,604 | 1,600 | 4 | E | 736 | 2.17× |
| Beep | 268 | 268 | 0 | Baseline | — | — |
| BindDrivers | 2,100 | 2,096 | 4 | B | 1,420 | 1.48× |
| Break | 1,428 | 1,408 | 20 | ABCDE | 432 | 3.26× |
| ChangeTaskPri | 1,424 | 1,404 | 20 | ABCDE | 460 | 3.05× |
| Check2090 | 516 | 504 | 12 | D | 220 | 2.29× |
| Copy | 15,520 | 15,432 | 88 | AB | 5,580 | 2.77× |
| Date | 2,576 | 2,564 | 12 | E | 1,092 | 2.35× |
| Delete | 5,932 | 5,648 | 284 | ABCDE | 1,972 | 2.86× |
| DevList | 1,216 | 1,196 | 20 | ABCDE | — | — |
| Dir | 7,820 | 7,740 | 80 | ABCDEF | 3,440 | 2.25× |
| DiskChange | 1,248 | 1,232 | 16 | ABCDE | 312 | 3.95× |
| DiskFree | 2,024 | 2,012 | 12 | E | — | — |
| DosList | 10,456 | 10,300 | 156 | ABCDEFG | — | — |
| Eval | 9,980 | 9,876 | 104 | ABCDEFG | 2,084 | 4.74× |
| Exe2Arc | 14,708 | 14,708 | 0 | Baseline | — | — |
| Execute | 6,376 | 6,168 | 208 | ABCDEFG | 4,432 | 1.39× |
| ExtractKickstart | 2,712 | 2,692 | 20 | ABCDE | 1,216 | 2.21× |
| FileNote | 2,116 | 2,092 | 24 | ABCDEF | 896 | 2.33× |
| FindResident | 1,104 | 1,096 | 8 | E | 220 | 4.98× |
| Format | 9,428 | 9,288 | 140 | AB | — | — |
| GuessBootDev | 2,168 | 2,132 | 36 | ABCD | 680 | 3.14× |
| IconPos | 2,464 | 2,460 | 4 | B | 1,720 | 1.43× |
| Info | 4,676 | 4,644 | 32 | G | 1,980 | 2.35× |
| Join | 2,308 | 2,272 | 36 | D | 1,200 | 1.89× |
| LibList | 1,224 | 1,204 | 20 | ABCDE | — | — |
| List | 10,648 | 10,640 | 8 | A | 5,108 | 2.08× |
| LoadMonDrvs | 2,632 | 2,572 | 60 | ABCDEF | — | — |
| Lock | 1,580 | 1,568 | 12 | E | 536 | 2.93× |
| MakeDir | 1,308 | 1,288 | 20 | ABCDE | 464 | 2.78× |
| MakeLink | 1,848 | 1,840 | 8 | C | 700 | 2.63× |
| ModList | 2,376 | 2,360 | 16 | AB | — | — |
| Mount | 11,756 | 11,680 | 76 | AB | 6,880 | 1.70× |
| PathPart | 2,124 | 2,120 | 4 | B | — | — |
| PortList | 1,476 | 1,460 | 16 | ABCD | — | — |
| Protect | 3,344 | 3,308 | 36 | ABCDEF | 1,300 | 2.54× |
| Quote | 6,084 | 6,076 | 8 | E | — | — |
| Reboot | 524 | 516 | 8 | D | 84 | 6.14× |
| Relabel | 1,060 | 1,060 | 0 | Baseline | 584 | 1.82× |
| Rename | 2,760 | 2,592 | 168 | ABCD | 1,140 | 2.27× |
| RequestChoice | 2,252 | 2,240 | 12 | E | 1,120 | 2.00× |
| RequestFile | 3,044 | 2,996 | 48 | D | 1,520 | 1.97× |
| ResList | 1,088 | 1,076 | 12 | ABCD | — | — |
| Search | 10,760 | 10,668 | 92 | ABCDEF | 2,476 | 4.31× |
| SetClock | 1,724 | 1,716 | 8 | E | 668 | 2.57× |
| SetDate | 1,860 | 1,852 | 8 | E | 688 | 2.69× |
| SetFont | 2,704 | 2,688 | 16 | ABCD | 1,092 | 2.46× |
| SetKeyboard | 1,660 | 1,652 | 8 | E | 1,412 | 1.17× |
| Status | 3,620 | 3,600 | 20 | E | 828 | 4.35× |
| TaskList | 17,216 | 17,176 | 40 | ABC | — | — |
| Touch | 2,224 | 2,164 | 60 | ABCDEF | — | — |
| Type | 5,424 | 5,252 | 172 | ABCDEFG | 1,496 | 3.51× |
| Version | 8,756 | 8,676 | 80 | F | 4,764 | 1.82× |
| Wait | 2,440 | 2,392 | 48 | G | 852 | 2.81× |
| WaitForLib | 1,392 | 1,344 | 48 | ABCDEF | — | — |
| WaitForNotification | 2,024 | 2,016 | 8 | E | — | — |
| WaitForPort | 1,464 | 1,436 | 28 | ABCDEF | — | — |
| Which | 3,976 | 3,940 | 36 | ABCDE | 1,068 | 3.69× |

## Execution costs and stack

Executed counts below sum each selected command’s complete 68000 fixture set; the JSON contains separate 68020/68040 results. Peak stack is the maximum observed SP displacement, including compiler invocation state. All accepted cases remain within their existing limits.

| Command | Instructions before | After | Delta | Peak stack before/after | Static branches before/after |
| --- | ---: | ---: | ---: | ---: | ---: |
| AddDataTypes | 32,120 | 31,813 | -307 | 516/516 | 343/340 |
| Assign | 11,896 | 11,899 | +3 | 188/188 | 117/117 |
| Avail | 3,810 | 3,891 | +81 | 144/144 | 72/72 |
| BindDrivers | 8,673 | 8,540 | -133 | 512/512 | 116/116 |
| Break | 10,506 | 10,612 | +106 | 148/148 | 73/73 |
| ChangeTaskPri | 4,063 | 4,083 | +20 | 144/144 | 63/63 |
| Check2090 | 977 | 945 | -32 | 60/60 | 22/22 |
| Copy | 6,986,744 | 6,520,111 | -466,633 | 684/684 | 707/707 |
| Date | 3,960 | 4,065 | +105 | 184/184 | 112/112 |
| Delete | 270,451 | 252,261 | -18,190 | 528/528 | 234/235 |
| DevList | 3,316 | 3,288 | -28 | 120/120 | 42/42 |
| Dir | 826,050 | 799,889 | -26,161 | 420/420 | 335/332 |
| DiskChange | 2,214 | 2,213 | -1 | 144/144 | 49/49 |
| DiskFree | 24,804 | 24,984 | +180 | 184/184 | 90/90 |
| DosList | 104,145 | 104,783 | +638 | 548/548 | 466/464 |
| Eval | 147,616 | 146,700 | -916 | 500/500 | 550/541 |
| Execute | 366,426 | 345,083 | -21,343 | 328/328 | 349/346 |
| ExtractKickstart | 9,677 | 9,706 | +29 | 212/212 | 139/139 |
| FileNote | 7,617 | 7,422 | -195 | 212/212 | 91/89 |
| FindResident | 2,152 | 2,194 | +42 | 132/132 | 50/50 |
| Format | 23,938 | 22,631 | -1,307 | 516/516 | 380/380 |
| GuessBootDev | 5,829 | 5,664 | -165 | 172/172 | 97/97 |
| IconPos | 9,494 | 9,378 | -116 | 236/236 | 150/150 |
| Info | 15,351 | 15,367 | +16 | 348/348 | 141/138 |
| Join | 7,059 | 6,751 | -308 | 272/272 | 99/98 |
| LibList | 3,320 | 3,292 | -28 | 120/120 | 42/42 |
| List | 64,775 | 64,746 | -29 | 356/356 | 251/251 |
| LoadMonDrvs | 16,043 | 15,209 | -834 | 228/228 | 147/145 |
| Lock | 4,152 | 4,242 | +90 | 156/156 | 69/69 |
| MakeDir | 3,066 | 3,078 | +12 | 144/144 | 53/53 |
| MakeLink | 7,339 | 7,333 | -6 | 156/156 | 84/84 |
| ModList | 4,677 | 4,650 | -27 | 148/148 | 84/84 |
| Mount | 2,289 | 2,275 | -14 | 304/304 | 660/660 |
| PathPart | 89,031 | 80,438 | -8,593 | 184/184 | 113/113 |
| PortList | 5,197 | 5,075 | -122 | 120/120 | 53/53 |
| Protect | 10,271 | 10,234 | -37 | 220/220 | 144/143 |
| Quote | 24,872 | 25,100 | +228 | 328/328 | 422/422 |
| Reboot | 1,024 | 998 | -26 | 68/68 | 21/21 |
| Rename | 114,069 | 105,463 | -8,606 | 196/196 | 106/105 |
| RequestChoice | 7,347 | 7,467 | +120 | 212/212 | 109/109 |
| RequestFile | 9,857 | 9,465 | -392 | 356/356 | 95/94 |
| ResList | 2,985 | 2,922 | -63 | 104/104 | 41/41 |
| Search | 964,697 | 939,337 | -25,360 | 520/520 | 296/294 |
| SetClock | 5,749 | 5,889 | +140 | 164/164 | 76/76 |
| SetDate | 5,657 | 5,777 | +120 | 180/180 | 93/93 |
| SetFont | 24,914 | 24,638 | -276 | 204/204 | 131/131 |
| SetKeyboard | 2,892 | 2,972 | +80 | 168/168 | 71/71 |
| Status | 8,691 | 9,019 | +328 | 408/408 | 149/149 |
| TaskList | 1,374,922 | 1,369,100 | -5,822 | 664/664 | 671/671 |
| Touch | 5,414 | 5,166 | -248 | 184/184 | 103/101 |
| Type | 84,528 | 83,748 | -780 | 344/344 | 301/298 |
| Version | 374,544 | 370,984 | -3,560 | 780/780 | 458/439 |
| Wait | 15,364 | 15,410 | +46 | 196/196 | 122/117 |
| WaitForLib | 4,006 | 3,991 | -15 | 152/152 | 71/68 |
| WaitForNotification | 14,587 | 14,697 | +110 | 192/192 | 103/103 |
| WaitForPort | 3,981 | 3,983 | +2 | 152/152 | 75/73 |
| Which | 55,893 | 53,984 | -1,909 | 232/232 | 213/213 |

Compact clears trade bytes for loop executions: Execute’s audited 22-byte clear has a 12-byte loop alternative with eleven additional DBRA executions. Removing a clear and compacting that same clear are alternative savings and are counted once. Shared division adds a call/return and a transient four-byte return address per shared-core invocation; measured instruction and stack effects above include these costs.

## Qualification

- 1,516 relevant compiler tests passed, including the six branch-liveness regressions, all 32 CCR combinations, partial writes, flags including X, memory width/count/order, aliases, function-pointer identity, stack restoration, HUNK relocation and branch-range tests.
- 843 host command tests passed, including applicable Workbench/MorphOS profile fixtures.
- Execute: complete 151-case suite on each CPU, 453 runs. Eval: all 37 Workbench and 20 MorphOS entry vectors under disabled, bounded and fixed-point peepholes on each CPU (333 and 180 runs); receipts in the JSON.
- Changed accepted shipping entries passed baseline/candidate comparisons on 68000, 68020 and 68040. Output bytes, return codes, IoErr, resource events, allocation ownership/releases, repeated invocation and supported interleaving are compared. Copy keeps compiler-owned invocation context separate from command allocations.
- Fresh source publishes of Execute, List, Search and Reboot match their qualified frozen images exactly. Filtered builds preserve unselected output, and a failed build leaves previous executables unchanged.
- Copper68k package 1.5.1; actual product version `1.5.1+43a3a84f79657cb4c2b32bb58d8e510e3aec9c15`; core SHA256 `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`. Actual receipt identities are retained.

## Limits and remaining opportunities

- Original Workbench images and CopperScreen are unavailable on this host; verified reference size/hash receipts only.
- AddBuffers and Relabel entry fixtures fail on unchanged shipping baselines (vector ABI and allocation size respectively). Exe2Arc has no native entry fixture. All three retain byte-identical baselines.
- Copper68k 1.5.1 lacks exact 68020/68040 timing for BCHG #0,D5 in the unchanged negative-divisor prefix; those signed compiler boundary cases run on 68000 only. The unsigned shared core and shipping command fixtures run on all three CPUs.
- Host fixtures cover Workbench and MorphOS profiles. Native qualification covers selected shipping profiles, not execution of PPC MorphOS originals.
- Memory arithmetic remains restricted to existing proven private-memory rewrites. No arbitrary APTR read/modify/write fold is introduced.
- Raw branched helper inlining is limited to fixed-point peepholes and private leaf bodies with internal branches, no stack manipulation, and no address/data fixups.
- No dynamic branch counter or wall-clock performance claim is made. Static branch counts and executed instruction counts are recorded separately.

The audit’s helper sketches and 42 byte-read candidates are investigation bounds. They are not all removable: liveness, canonical wider consumers and alias effects keep some transport. Whole-image helper choices still grow some commands; their final policies reject that growth. Large Workbench gaps remain, especially aggregate-heavy List/Search and parser/formatter implementations.

## Reproduction and delivery

Use `tools/Commands/optimize_codegen_sizes.py` for frozen-input compile, qualify, matrix, select and publish operations. `tools/Commands/report_codegen_sizes.py` regenerates this report. `tools/Commands/build-c.ps1` stages selected commands before refreshing its output and preserves unselected commands for filtered builds.

Build the compiler branch with .NET SDK 10.0.401, then publish with `tools/Commands/build-c.ps1 -CopperSharpRoot C:/Users/ilkle/Koodit/GIT/CopperSharp68k-wt-codegen-size`. The unrelated dirty primary compiler checkout was preserved and does not contain these commits yet. An older compiler cannot interpret the new selected-pass names.

Local immutable inputs, all baseline executables, trials, maps, compiler payloads and native receipts: `C:\Users\ilkle\Koodit\GIT\CopperOS\artifacts\codegen-size-implementation-20261005`. Final staging: `C:\Users\ilkle\Koodit\GIT\CopperOS\artifacts\codegen-size-implementation-20261005\final`.

Separate compiler commits and patch hashes are listed in the JSON. CopperOS tooling, per-command enablement and reporting are separate changes. Publication requires all 61 images, matching hashes, a smaller aggregate and qualification for every changed executable; previous output is retained before stale files are removed.

[Machine-readable report](code-generator-size-optimization-20261005.json)

Published to `C:\Users\ilkle\Koodit\GIT\CopperOS\out\C`: all 61 command hashes match staging. Previous output is retained in `C:\Users\ilkle\Koodit\GIT\CopperOS\artifacts\codegen-size-implementation-20261005\final\previous-C`.
