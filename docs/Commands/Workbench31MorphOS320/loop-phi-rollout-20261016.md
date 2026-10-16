# SimplifyLoopPhis command rollout — 2026-10-16

Enabled the existing opt-in pass for **12 commands** after qualification. The complete 61-command inventory falls from **234,100 to 225,336 bytes**, saving **8,764 bytes**.

## Controlled size comparison

All remaining installed commands of at least 4,096 bytes were screened; previously qualified List and Search were excluded. Each off/on pair uses identical frozen managed assemblies and compiler payload. Only SimplifyLoopPhis differs. Fresh baseline sizes differ from installed sizes because the earlier installed toolchain/source inputs differ; those differences are not credited to this pass. Accepted same-assembly pass savings total 9336 bytes.

| Command | Installed | Fresh off | Pass on | Pass saving | Decision |
| --- | ---: | ---: | ---: | ---: | --- |
| AddDataTypes | 7,860 | 7,900 | 7,720 | 180 | Enabled |
| Copy | 15,432 | 15,488 | 15,284 | 204 | Enabled |
| Delete | 5,648 | 5,644 | 5,636 | 8 | Enabled |
| Dir | 7,740 | 7,708 | 6,192 | 1,516 | Enabled |
| DosList | 10,300 | 10,276 | 9,916 | 360 | Enabled |
| Eval | 9,876 | 9,916 | 9,636 | 280 | Enabled |
| Exe2Arc | 14,708 | 14,724 | 14,580 | 144 | Held |
| Execute | 6,168 | 6,168 | 5,888 | 280 | Enabled |
| Format | 9,288 | 9,244 | 7,960 | 1,284 | Enabled |
| Info | 4,644 | 4,640 | 3,520 | 1,120 | Enabled |
| Mount | 11,680 | 11,796 | 11,340 | 456 | Enabled |
| Quote | 6,076 | 6,168 | 6,076 | 92 | Held |
| TaskList | 17,340 | 17,628 | 14,476 | 3,152 | Enabled |
| Type | 5,252 | 5,392 | 4,896 | 496 | Enabled |
| Version | 8,676 | 8,748 | 8,620 | 128 | Held |

Exe2Arc lacks a shipping-entry execution fixture. Quote merely matches its installed size. Version passes its shipping profile but both off/on private MorphOS profiles fail the same null-buffer read: handle 0x530 is correct, buffer is zero, count is 16,385 (`default-name-mossys-libs-elf-preference`). These three policies and installed images remain unchanged.

## Where the savings come from

SimplifyLoopPhis reduces redundant loop-carried SSA transport before register allocation. The resulting frame/spill and peephole changes can amplify the local saving. These are encoded method-body reductions, with downstream layout effects included separately in the executable totals:

- **TaskList**: `Run` 5048 → 2130 bytes; `AddTask` 1136 → 986 bytes; `AppendUnsigned` 256 → 232 bytes.
- **Dir**: `Run` 2436 → 1372 bytes; `ListDirectory` 1300 → 1096 bytes; `ListPattern` 1088 → 944 bytes.
- **Format**: `Run` 3654 → 2746 bytes; `FormatPartition` 1290 → 928 bytes; `WaitValidated` 216 → 204 bytes.
- **Info**: `Run` 3010 → 1888 bytes.
- **Type**: `RunProfile` 1434 → 1098 bytes; `Copy` 732 → 678 bytes; `TryWriteLineNumber` 300 → 254 bytes.

## Shipping execution qualification

Baseline and candidate fixtures execute on 68000, 68020 and 68040. Checks cover the available declared output, return/IoErr, resource ownership, repeated/interleaved invocation, image immutability and configured stack limits. Execute runs all 151 cases per CPU. The following totals are per CPU; instruction and peak-stack observations agree across these CPU modes.

| Command | Cases/CPU | Instructions off → on | Peak stack off → on |
| --- | ---: | ---: | ---: |
| AddDataTypes | 15 | 31,370 → 31,272 | 528 → 520 |
| Copy | 96 | 6,852,317 → 6,901,170 | 692 → 696 |
| Delete | 8 | 266,363 → 268,443 | 520 → 520 |
| Dir | 30 | 827,361 → 778,451 | 420 → 384 |
| DosList | 34 | 105,112 → 101,909 | 548 → 552 |
| Eval | 37 | 147,321 → 140,631 | 496 → 496 |
| Execute | 151 | 367,230 → 360,567 | 324 → 320 |
| Format | 10 | 23,192 → 22,459 | 520 → 488 |
| Info | 14 | 15,424 → 12,949 | 348 → 336 |
| Mount | 9 | 2,262 → 2,230 | 304 → 308 |
| TaskList | 57 | 1,438,050 → 1,388,314 | 684 → 668 |
| Type | 17 | 84,955 → 80,532 | 360 → 352 |

Copy adds 48,853 instructions across its 96 cases per CPU (0.71%); Delete adds 2,080 (0.78%). Copy, DosList and Mount each add four bytes to measured peak stack. All remain within unchanged configured stack limits. These finite instruction totals are not hardware timing or a general performance guarantee. Per-case raw receipts retain execution costs and stack observations.

### TaskList live stack pointer

The original strict comparison detected one legitimate output difference: verbose self-reporting prints the live A7 value, which changes with the generated frame. The strengthened fixture records actual A7 at the mapped WriteVerbose instruction, verifies its opcode, and requires the printed pointer to match that observation. Only this verified line is normalized for comparison; every other command-visible field still matches. All 57 cases per CPU pass. A negative control changes the store source from A7 to A5: the old range-only fixture passes, while the strengthened fixture rejects it. The original strict failure and initial unsuccessful probe attempts are preserved in staging.

## Alternate profiles and peepholes

Workbench/MorphOS private fixtures pass for AddDataTypes, Delete, Dir, Info, Mount and Type. Eval runs all 37 shipping and 20 MorphOS vectors per CPU with disabled, bounded and fixed-point peepholes. Version is held as described above. Initial AddDataTypes/Info compilation attempts omitted callback exports; corrected explicit export allowlists pass. Failed attempts remain preserved.

## Reproduction and publication

Evidence: `artifacts/loop-phi-rollout-20261016`. The JSON report binds compiler payload, SDK/input identities, response files, maps, pass counts, method sizes, CPU receipts and complete accepted inventory hashes. The compiler payload is the previously frozen List main candidate (`eb04dac` plus its recorded patches); no compiler implementation or global default changed. Detailed compiler source/patch provenance is retained in `list-loop-phis-20261016.json` and its frozen artifacts.

Flags: MC68000 HUNK, resident runtime, YOLO exceptions, memory disabled, symbols off, fixed-point peepholes. Existing per-command passes remain selected; only SimplifyLoopPhis is appended. Copper68k 1.5.1 SHA-256: `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`.

The fresh default source build of all 12 accepted commands, with no pass override and the frozen compiler CLI, reproduces every qualified candidate byte for byte. The other 49 installed executables are preserved. The complete 61-command inventory is staged and hash-checked before refreshing the selected files in `out/C`; no stale files are removed. Publication status: **completed**.

Tools: `audit_loop_phi_rollout.py`, `qualify_loop_phi_profiles.py`, `qualify_tasklist_live_sp.py`, `control_tasklist_live_sp.py`, `enable_loop_phi_rollout.py`, and this report/publication tool. Run preparation in a fresh staging directory; enabling is a one-time policy edit after successful qualification. Unrelated Shell/ImageBuilder/compiler work is preserved.

## Qualification limits

This rollout uses supplied-vector fixtures. Original DOS, WinUAE/CopperScreen and original-image checks were not rerun for these commands; prior List/Search native qualification remains separate. Actual MorphOS execution, original Workbench functional parity, PURE admission and absolute minimum stack are not established. No PPC binary-size comparison is made. Existing compiler-pass regressions were qualified in the earlier List work; the compiler suite was not rerun for this policy-only rollout. Version’s baseline failure and Exe2Arc shipping-entry coverage remain follow-up work.
