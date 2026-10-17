# Receiver classification rollout — 17 October 2026

Measured all 61 installed commands with clean `69d01d1` and the isolated receiver-classification correction committed as `9eaa2aa`. 49 commands shrink in that controlled comparison. 47 pass the installation gates, saving **3,104 bytes** against installed images: **225,052 → 221,948 bytes**.

## Inputs and interpretation

Each compiler pair uses identical frozen CIL, dependencies and response flags. MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes and no symbols. Per-command size policies remain unchanged. Removed checkout references are resolved from the retained SDK; every substitution and hash is recorded. Execute uses accepted frozen CIL because unrelated source edits are in progress; its initial old-policy probe is preserved separately and the final comparison uses the accepted SimplifyLoopPhis selection.

The receiver correction classifies ordinary unboxed struct receivers as managed pointers rather than GC references. This removes false object roots and can unlock private-storage initialization elimination. Installed savings can also include earlier qualified compiler improvements: the controlled baseline column isolates the receiver effect. Dir already contains this fix and is retained.

## Per-command results

| Command | Installed | Control | Fixed | Receiver saving | Decision | WB 3.1 ratio after |
|---|---:|---:|---:|---:|---|---:|
| AddBuffers | 1,084 | 1,084 | 1,084 | +0 | Retain | 2.44× |
| AddDataTypes | 7,720 | 7,668 | 7,624 | +44 | Install | 1.30× |
| Assign | 2,100 | 2,100 | 2,056 | +44 | Install | 0.64× |
| Avail | 1,600 | 1,600 | 1,564 | +36 | Install | 2.12× |
| Beep | 268 | 268 | 268 | +0 | Retain | — |
| BindDrivers | 2,096 | 2,096 | 2,096 | +0 | Retain | 1.48× |
| Break | 1,408 | 1,408 | 1,364 | +44 | Install | 3.16× |
| ChangeTaskPri | 1,404 | 1,404 | 1,360 | +44 | Install | 2.96× |
| Check2090 | 504 | 504 | 504 | +0 | Retain | 2.29× |
| Copy | 15,284 | 15,264 | 15,236 | +28 | Install | 2.73× |
| Date | 2,564 | 2,564 | 2,528 | +36 | Install | 2.32× |
| Delete | 5,636 | 5,648 | 5,612 | +36 | Install | 2.85× |
| DevList | 1,196 | 1,196 | 1,196 | +0 | Retain | — |
| Dir | 6,164 | 6,208 | 6,164 | +44 | Retain | 1.79× |
| DiskChange | 1,232 | 1,232 | 1,188 | +44 | Install | 3.81× |
| DiskFree | 2,012 | 2,012 | 1,976 | +36 | Install | — |
| DosList | 9,916 | 9,780 | 9,736 | +44 | Install | — |
| Eval | 9,636 | 9,612 | 9,568 | +44 | Install | 4.59× |
| Exe2Arc | 14,708 | 14,708 | 14,680 | +28 | Retain | — |
| Execute | 5,888 | 5,856 | 5,812 | +44 | Install | 1.31× |
| ExtractKickstart | 2,692 | 2,692 | 2,640 | +52 | Install | 2.17× |
| FileNote | 2,092 | 2,092 | 2,048 | +44 | Install | 2.29× |
| FindResident | 1,096 | 1,096 | 1,060 | +36 | Install | 4.82× |
| Format | 7,960 | 7,732 | 7,696 | +36 | Install | — |
| GuessBootDev | 2,132 | 2,132 | 2,080 | +52 | Install | 3.06× |
| IconPos | 2,460 | 2,460 | 2,424 | +36 | Install | 1.41× |
| Info | 3,520 | 3,456 | 3,420 | +36 | Install | 1.73× |
| Join | 2,272 | 2,272 | 2,228 | +44 | Install | 1.86× |
| LibList | 1,204 | 1,204 | 1,204 | +0 | Retain | — |
| List | 4,652 | 4,652 | 4,616 | +36 | Install | 0.90× |
| LoadMonDrvs | 2,572 | 2,572 | 2,528 | +44 | Install | — |
| Lock | 1,592 | 1,592 | 1,556 | +36 | Install | 2.90× |
| MakeDir | 1,288 | 1,288 | 1,244 | +44 | Install | 2.68× |
| MakeLink | 1,840 | 1,840 | 1,792 | +48 | Install | 2.56× |
| ModList | 2,424 | 2,424 | 2,424 | +0 | Retain | — |
| Mount | 11,340 | 11,192 | 11,156 | +36 | Install | 1.62× |
| PathPart | 2,120 | 2,120 | 2,084 | +36 | Install | — |
| PortList | 1,460 | 1,460 | 1,460 | +0 | Retain | — |
| Protect | 3,308 | 3,308 | 3,264 | +44 | Install | 2.51× |
| Quote | 6,076 | 6,076 | 6,040 | +36 | Install | — |
| Reboot | 516 | 516 | 516 | +0 | Retain | 6.14× |
| Relabel | 1,060 | 1,060 | 1,060 | +0 | Retain | 1.82× |
| Rename | 2,592 | 2,592 | 2,592 | +0 | Retain | 2.27× |
| RequestChoice | 2,240 | 2,240 | 2,204 | +36 | Install | 1.97× |
| RequestFile | 2,996 | 2,996 | 2,952 | +44 | Install | 1.94× |
| ResList | 1,076 | 1,076 | 1,076 | +0 | Retain | — |
| Search | 5,300 | 5,300 | 5,256 | +44 | Install | 2.12× |
| SetClock | 1,716 | 1,716 | 1,680 | +36 | Install | 2.51× |
| SetDate | 1,852 | 1,852 | 1,816 | +36 | Install | 2.64× |
| SetFont | 2,688 | 2,688 | 2,644 | +44 | Install | 2.42× |
| SetKeyboard | 1,652 | 1,652 | 1,616 | +36 | Install | 1.14× |
| Status | 3,600 | 3,600 | 3,564 | +36 | Install | 4.30× |
| TaskList | 14,476 | 14,112 | 14,076 | +36 | Install | — |
| Touch | 2,164 | 2,164 | 2,120 | +44 | Install | — |
| Type | 4,896 | 4,756 | 4,696 | +60 | Install | 3.14× |
| Version | 8,580 | 8,580 | 8,548 | +32 | Install | 1.79× |
| Wait | 2,392 | 2,392 | 2,356 | +36 | Install | 2.77× |
| WaitForLib | 1,344 | 1,344 | 1,300 | +44 | Install | — |
| WaitForNotification | 2,016 | 2,016 | 1,980 | +36 | Install | — |
| WaitForPort | 1,436 | 1,436 | 1,392 | +44 | Install | — |
| Which | 3,940 | 3,940 | 3,896 | +44 | Install | 3.65× |

## Qualification and execution cost

Candidates are compared on 68000, 68020 and 68040 using command-visible output bytes, returns, IoErr, ownership and release observations, startup cases and supported repeated/interleaved calls. Distinct implemented MorphOS profiles are compiled and checked independently. MorphOS-only implementations receive no PPC size comparison. Each command keeps its existing options, checks and stack limit.

| Command/profile | Invocations per image | Instructions control → fixed | Peak stack bytes control → fixed |
|---|---:|---:|---:|
| AddDataTypes/shipping | 45 | 94,866 → 94,038 | 516 → 516 |
| AddDataTypes/morphos | 126 | 268,080 → 265,644 | 520 → 520 |
| Assign/shipping | 57 | 35,697 → 32,712 | 188 → 188 |
| Assign/morphos | 57 | 33,900 → 31,149 | 180 → 180 |
| Avail/shipping | 33 | 11,673 → 11,049 | 144 → 144 |
| Avail/morphos | 36 | 30,024 → 28,968 | 236 → 236 |
| Break/shipping | 72 | 31,836 → 29,253 | 148 → 148 |
| Break/morphos | 102 | 74,259 → 62,379 | 244 → 244 |
| ChangeTaskPri/shipping | 42 | 12,249 → 11,688 | 144 → 144 |
| ChangeTaskPri/morphos | 45 | 22,461 → 20,781 | 228 → 228 |
| Copy/shipping | 288 | 19,561,806 → 19,482,966 | 688 → 688 |
| Date/shipping | 27 | 12,195 → 11,727 | 184 → 184 |
| Date/morphos | 39 | 46,800 → 45,648 | 260 → 260 |
| Delete/shipping | 24 | 756,714 → 756,150 | 528 → 528 |
| Delete/morphos | 24 | 757,338 → 756,666 | 528 → 528 |
| DiskChange/shipping | 33 | 6,639 → 6,441 | 144 → 144 |
| DiskChange/morphos | 33 | 6,639 → 6,441 | 144 → 144 |
| DiskFree/shipping | 45 | 74,952 → 74,424 | 184 → 184 |
| DosList/shipping | 102 | 302,658 → 298,968 | 552 → 552 |
| Eval/shipping | 111 | 421,881 → 418,206 | 492 → 492 |
| Eval/morphos | 60 | 361,086 → 358,872 | 736 → 736 |
| Execute/shipping | 453 | 1,017,060 → 1,017,027 | 320 → 320 |
| ExtractKickstart/shipping | 51 | 29,118 → 27,369 | 212 → 212 |
| FileNote/shipping | 30 | 22,266 → 21,471 | 212 → 212 |
| FileNote/morphos | 30 | 22,266 → 21,471 | 212 → 212 |
| FindResident/shipping | 33 | 6,582 → 6,438 | 132 → 132 |
| Format/shipping | 30 | 65,157 → 63,393 | 488 → 488 |
| GuessBootDev/shipping | 48 | 16,992 → 16,533 | 172 → 172 |
| IconPos/shipping | 45 | 28,134 → 25,914 | 236 → 236 |
| Info/shipping | 42 | 38,484 → 38,268 | 336 → 336 |
| Info/morphos | 198 | 861,912 → 854,958 | 736 → 736 |
| Join/shipping | 54 | 20,253 → 19,686 | 272 → 272 |
| Join/morphos | 54 | 20,253 → 19,686 | 272 → 272 |
| List/shipping | 96 | 128,946 → 121,494 | 264 → 264 |
| List/morphos | 93 | 140,259 → 131,349 | 264 → 264 |
| LoadMonDrvs/shipping | 51 | 45,627 → 44,862 | 228 → 228 |
| Lock/shipping | 36 | 14,361 → 13,581 | 160 → 160 |
| Lock/morphos | 36 | 14,361 → 13,581 | 160 → 160 |
| MakeDir/shipping | 42 | 9,234 → 8,970 | 144 → 144 |
| MakeDir/morphos | 45 | 11,814 → 11,355 | 184 → 184 |
| MakeLink/shipping | 60 | 21,999 → 20,751 | 156 → 156 |
| MakeLink/morphos | 54 | 16,257 → 15,321 | 152 → 152 |
| Mount/shipping | 27 | 6,807 → 6,723 | 308 → 308 |
| Mount/morphos | 15 | 4,458 → 4,374 | 260 → 260 |
| PathPart/shipping | 45 | 241,314 → 240,534 | 184 → 184 |
| Protect/shipping | 42 | 30,702 → 29,103 | 220 → 220 |
| Protect/morphos | 42 | 30,702 → 29,103 | 220 → 220 |
| Quote/shipping | 36 | 75,300 → 73,950 | 328 → 328 |
| RequestChoice/shipping | 33 | 22,401 → 21,855 | 212 → 212 |
| RequestChoice/morphos | 48 | 46,833 → 45,681 | 216 → 216 |
| RequestFile/shipping | 36 | 28,395 → 26,511 | 356 → 352 |
| RequestFile/morphos | 36 | 29,640 → 27,630 | 368 → 364 |
| Search/shipping | 66 | 2,594,448 → 2,591,751 | 476 → 476 |
| Search/morphos | 147 | 27,929,553 → 27,921,174 | 712 → 712 |
| SetClock/shipping | 48 | 17,667 → 16,887 | 164 → 164 |
| SetClock/morphos | 51 | 22,182 → 21,222 | 160 → 160 |
| SetDate/shipping | 36 | 17,331 → 16,467 | 180 → 180 |
| SetDate/morphos | 36 | 21,372 → 20,316 | 184 → 184 |
| SetFont/shipping | 99 | 73,914 → 69,576 | 204 → 204 |
| SetKeyboard/shipping | 30 | 8,916 → 8,748 | 168 → 168 |
| SetKeyboard/morphos | 48 | 33,774 → 33,462 | 192 → 192 |
| Status/shipping | 48 | 27,057 → 26,001 | 408 → 408 |
| Status/morphos | 57 | 34,419 → 33,075 | 408 → 408 |
| TaskList/shipping | 171 | 3,996,492 → 3,986,088 | 644 → 644 |
| Touch/shipping | 30 | 15,498 → 14,865 | 184 → 184 |
| Type/shipping | 51 | 242,796 → 240,939 | 344 → 344 |
| Type/morphos | 48 | 245,643 → 243,501 | 344 → 344 |
| Version/shipping | 279 | 1,061,145 → 1,053,147 | 784 → 784 |
| Version/morphos | 282 | 9,718,332 → 9,706,584 | 1524 → 1524 |
| Wait/shipping | 51 | 46,230 → 45,216 | 196 → 196 |
| Wait/morphos | 45 | 46,200 → 45,186 | 196 → 196 |
| WaitForLib/shipping | 30 | 11,973 → 11,421 | 152 → 152 |
| WaitForNotification/shipping | 36 | 44,091 → 43,491 | 192 → 192 |
| WaitForPort/shipping | 30 | 11,949 → 11,253 | 152 → 152 |
| Which/shipping | 93 | 161,952 → 159,516 | 232 → 232 |
| Which/morphos | 255 | 210,498 → 200,781 | 232 → 232 |

The main vector comparisons account for 5,409 invocations per image. Eval additionally runs both profiles with disabled, bounded and fixed-point peepholes, on all three CPUs. Fixture stack limits are checked on every candidate case. Instruction counts include fixture calls and are not wall-clock speed claims.

Across the main vector matrix, instructions fall from 72,585,072 to 72,343,779. No qualified profile/CPU total increases its instruction count or peak stack use.

List and Search additionally compare installed and candidate images under original Kickstart 3.1 DOS: List 306 and Search 192 total calls across both images and all three CPUs, including 4 KiB and 8 KiB stacks, plus eight auditor negative controls. Independent WinUAE confirmations cover both images on all three CPUs. These are CLI original-DOS checks; private MorphOS profiles use supplied-vector fixtures.

TaskList initially differed only in the diagnostic program counter of its live-A7 read, not in any output byte or SP value. The comparator now excludes that instruction address while retaining read count, observed SP, output and all resource observations. Eighteen mutation controls reject SP/read-count/output/return/IoErr/cleanup changes; three controls allow changed instruction addresses. The runtime fixture independently validates live A7 and the printed stack value. Initial receipts and setup corrections are preserved.

Vector runtime: Copper68k 1.5.1, SHA-256 `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`. Original-DOS engine and WinUAE identities, source bindings, ROM identity and raw receipts are retained in the JSON evidence. Original-DOS/CopperScreen coverage is unavailable for the other commands in this rollout; no such coverage is claimed. Exe2Arc lacks a shipping runtime suite and is retained even if it shrinks.

Original-DOS runtime: Copper68k `1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63`, SHA-256 `5780f5084fd44e2d4732969bc0f9ce7ae7e3beffc1135ded92bc46d9b21399f2`. Independent emulator: WinUAE `6.0.3.0`, SHA-256 `f3e55700fd811cd543fdfebf6c3221cfaa3d385828f110c6d3eb534c64503123`.

## Retained candidates

- **Dir**: Already installed at this size.
- **Exe2Arc**: Qualification unavailable or failed; retain installed image.

## Reproduction and delivery

Tool: `tools/Commands/rollout_receiver_fix.py`. The JSON report binds compiler payloads, SDK and command assemblies, effective flags, maps and optimizer metrics, largest methods, profile receipts and execution costs. Raw artifacts: `artifacts/receiver-rollout-v3-20261017`; the two earlier incomplete preparation stages are preserved. All 61 command pairs are built before selection. A complete accepted staging inventory is hash-checked before updating selected `out/C` files; unselected files are preserved. No command source or compiler policy change is required.

With the retained inputs available, select a fresh stage and run the tool modes in this order: `prepare`, `roots`, `measure`, `execute_policy`, `vectors`, `eval_modes`, `native`, `repair_profiles`, `publish`, `report`, each with `--stage <absolute-stage-path>`. Native confirmation requires PowerShell 7, the recorded ROM and WinUAE. `independent` recovers only the independent-emulator phase into fresh subdirectories after a failed confirmation attempt. Reports retain all initial failures; fixture names/callback exports were corrected without changing command behavior.
