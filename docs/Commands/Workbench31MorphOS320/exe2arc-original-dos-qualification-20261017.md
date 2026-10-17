# Exe2Arc original DOS qualification — 2026-10-17

## Result

**Pass.** The installed receiver-fixed executable (14,652 bytes) and the repaired-source compiler control (14,680 bytes) pass 32 original-DOS contracts on MC68000, MC68020 and MC68040. This adds native OS qualification to the [runtime qualification](exe2arc-runtime-qualification-20261017.md); the compiler saving remains **28 bytes**, already counted there. Exe2Arc is MorphOS-only, so there is no Workbench 3.1 command size ratio.

The installed executable SHA-256 is `6a045b00a39a6ed91a4a1593df18ed585f4e7f1a71f4b3543dce882c4494bed6`. All 61 installed command files remain byte-identical to the frozen inventory (221,892 bytes). No command source or installed executable changed for this qualification.

| CPU | Accepted native calls, both images | Maximum frames per accepted boot | Independent WinUAE calls |
|---|---:|---:|---:|
| 68000 | 118 | 2900 | 110 |
| 68020 | 118 | 2920 | 110 |
| 68040 | 118 | 2810 | 110 |

Native total: **354 calls**. Independent WinUAE total: **330 calls**. Warm-ups are included in these totals. A separate 8-call MC68000 smoke and earlier successful experiments are retained but excluded from these counts.

## Harness and assertions

The normal CLI guest adapts Search's frozen NativeOsSession harness. All command loading, RunCommand execution, argument parsing, file access and cleanup use real Kickstart 3.1 Exec/DOS vectors. Guest code implements no DOS service substitutes. Both segments are loaded once per boot and alternate synchronously in the ordinary batches. Cross-window baseline and candidate each run in a separate boot to respect the fixed execution budget.

The independent corpus covers explicit and automatic ZIP, ACE, RAR, CAB, ARJ, LhA and LZH recognition; ZIP offset correction; generated and quoted names; no suffix; directory fallback; missing/invalid arguments; unknown types and absent signatures; empty/missing inputs; unsafe archive offsets; illegal/truncated ZIP records; output-open failure; pending Ctrl-C; a signature straddling the scanner window; and a 102,413-byte multi-chunk extraction. Fixture payloads are inert and are never executed.

Raw binary requests and receipts are checked against exact stdout bytes, return codes, immediate IoErr, output-file presence and complete extracted bytes. Successful output handles must close and their files must delete. Ten caller-state fields must be restored, including input/output, current directory, CLI, stack fields, allocated signal mask and dos.library open count. Repeated calls exercise 4 KiB and 8 KiB requested stacks and require exactly unchanged free memory after closing/deleting extracted RAM files. Initial filesystem/library warming is recorded separately; arbitrary extra command allocations are not permitted by repeat checks. This is finite free-memory and state qualification, not an allocation-by-allocation ownership tracer.

The verifier compares raw disk bytes with decoded JSON. Ten auditor controls reject altered return code, IoErr, stdout, extracted payload, file presence, close/delete results, caller state, repeat memory and forged JSON. Original evidence is restored byte-for-byte after the forged-JSON control.

## Original system and frozen tools

- Kickstart 3.1 A500 ROM SHA-256: `8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`.
- Search runner snapshot: `artifacts/receiver-rollout-v3-20261017/native/Search/runner`, copied without rebuilding its emulator/OS launcher.
- Copper68k: `1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63`; SHA-256 `5780f5084fd44e2d4732969bc0f9ce7ae7e3beffc1135ded92bc46d9b21399f2`.
- CopperMod.Amiga.Lightweight: `1.0.0+a4e80b68a7b58e6c12b940b9d786b2ac73757965`; SHA-256 `04b17fd0e6d2e3928ff161dbcf28c402d1d7dc93b70d9160c811a839ea91cc7a`.
- WinUAE 6.0.3.0, JIT disabled; SHA-256 `f3e55700fd811cd543fdfebf6c3221cfaa3d385828f110c6d3eb534c64503123`.

The JSON receipt includes compiler/SDK freeze manifests, command identities, source/PDB binding details, generated guest identity, per-run cycles/frames, raw receipt hashes and installed inventory. Native runner/launcher source bindings match; the frozen lightweight binary is authoritative where local source has drifted. Driver compilation uses MC68000 resident HUNK, YOLO, memory disabled, fixed-point peepholes and code-size policy off. Target command images are the unchanged prior qualified images.

## Execution limits and qualification boundaries

Native execution retains Search's 3,000-frame and 120-second limits. Combined cross-window runs exhausted the frame budget on MC68000/MC68040, even after removing warm-ups; those incomplete receipts are retained as failed attempts, never counted as passes. Separating the two target images yields completed exact-contract checks within the same limits. No command behavior or expected output was relaxed.

WinUAE independently confirms all seven formats, automatic recognition, naming cases, parser/scanner errors, Ctrl-C and repeated invocation on all three CPUs. Its combined MC68000 cross-window attempt hit the 45-second host timeout and is retained as an independent-emulator gap. Cross-window and multi-chunk acceptance relies on completed original-DOS native harness runs; no WinUAE claim is made for those two contracts.

This is original DOS CLI execution, not MorphOS PPC execution or actual Workbench-message launch. Existing vector qualification covers Workbench startup and injected allocation/I/O failures; this run adds real missing-file/open failure paths but no native fault injection. Alternation is synchronous, not simultaneous invocation. Requested stack sizes and restoration are checked; original-OS peak stack/instruction counts are not instrumented. The prior runtime measurement remains 940-byte peak stack and 13,843,861 candidate instructions versus 13,844,949 control instructions per complete vector suite/CPU. No CopperScreen or original MorphOS executable comparison was performed.

## Reproduction and evidence

Harness sources: `tools/Commands/Exe2ArcOsQualification/`; orchestration: `tools/Commands/qualify_exe2arc_os.py`. Prepare a fresh stage, then execute; preparation freezes all target images, compiler/SDK/runtime binaries, inputs and generated guest. Current batching splits the heavy cross-window images automatically. The WinUAE confirmation script defaults to its independently qualified subset. Run `control-auditor.py` after the native repeat batch. The report builder combines this run's main stage and separate cross-window stage; new complete stages can supply both arguments identically.

Accepted evidence: `artifacts/exe2arc-os-v2-20261017` and `artifacts/exe2arc-os-cross-split-20261017`. Earlier `exe2arc-os-20261017` and `exe2arc-os-cross-20261017` experiments remain available. Machine-readable report: [qualification receipt](exe2arc-original-dos-qualification-20261017.json).
