# Exe2Arc independent WinUAE qualification — 2026-10-18

## Result

**Pass on MC68000, MC68020 and MC68040.** The cross-window and multi-chunk gaps from the [original DOS qualification](exe2arc-original-dos-qualification-20261017.md) are closed. Both unchanged compiler images pass every one of the 32 contracts independently in WinUAE under original Kickstart 3.1 DOS.

The new six boots add **36 command calls** to the prior 330 independently qualified calls: **366 total**, including warm-ups, across both images. Each CPU has 122 calls and all 32 distinct contracts for each image. These counts do not add the native harness's 354 calls or failed attempts.

| CPU | Combined cross-window batch | Multi-chunk batch | Total independent calls | Contracts per image |
|---|---:|---:|---:|---:|
| 68000 | 56.33 s | 9.98 s | 122 | 32 |
| 68020 | 37.82 s | 5.65 s | 122 | 32 |
| 68040 | 20.26 s | 6.83 s | 122 | 32 |

The control remains **14,680 bytes**, and the installed candidate remains **14,652 bytes**, SHA-256 `6a045b00a39a6ed91a4a1593df18ed585f4e7f1a71f4b3543dce882c4494bed6`. No executable or command source changed.

## Timeout investigation

The retained 45-second failure contains four successful RAR warm-up rows followed by a successful control cross-window extraction: five complete rows of the expected six. The candidate result had not yet been written. There is no error result in that partial receipt.

The same six-call batch now completes on MC68000 in **56.33 seconds**, exceeding the previous 45-second deadline. The control row becomes visible at approximately 32.51 seconds; the timestamp is a one-second host sample of receipt-file growth, not an exact instruction boundary. Timing samples retain elapsed wall time, process CPU time and receipt byte count. The candidate completes with the same exact output, 32-byte extracted payload, return code 0 and IoErr 0 as the control.

`Exe2ArcForwardScanner.Scan` probes successive candidate positions in each window. The cross-window fixture places the marker at offset 102,398, forcing both images to scan approximately 100 KiB before recognizing it. The similarly sized multi-chunk fixture places its marker at offset 32 and completes much sooner despite copying a 102,413-byte payload. The receipts and timings support scan workload as the reason the combined cross-window batch needs more time; they do not indicate an extraction or window-boundary failure. Timings depend on host load and are not CPU performance comparisons.

This was an insufficient host deadline for the combined workload. No scanner, compiler, emulator core or ROM correction was required. The WinUAE wrapper now exposes a bounded timeout (default 120 seconds), explicit CPU/batch selection and a separate evidence directory. It records progress and preserves timed-out runs. Its default matrix includes the heavy batches. The original native harness retains its separate 3,000-frame/120-second limits and single-image cross-window qualification.

All emulation settings are identical to the failed run except the selected CPU model and host output-directory path. JIT remains disabled; the six-call cross-window batch has not been split or shortened. The 102,413-byte multi-chunk extraction also completes on all CPUs. Host runtimes include boot, input preparation, both image calls and clean guest exit; they are not isolated command timing benchmarks.

## Independent assertions and provenance

The new auditor regenerates the complete binary request from the frozen authored cases, checks guest/command/input identities, verifies the original startup script and hardware configuration, and audits the raw guest receipt. Assertions include exact stdout and extracted bytes, return codes, immediate IoErr, output-file existence, successful close/delete and restoration of all ten caller-state fields. The previous repeat batches still require zero free-memory change after output-file cleanup at 4 KiB and 8 KiB requested stack sizes. Cross-window and large-input calls use 4 KiB; their initial warming deltas remain recorded rather than treated as repeat measurements.

All 330 older independent calls were re-audited together with the 36 new calls. The same two unchanged target images were used throughout. The frozen native stage is `artifacts/exe2arc-os-v2-20261017`; new evidence is `artifacts/exe2arc-winuae-heavy-20261018`. Historical failed receipts are retained.

- WinUAE **6.0.3.0**, interpreter/JIT disabled; SHA-256 `f3e55700fd811cd543fdfebf6c3221cfaa3d385828f110c6d3eb534c64503123`.
- Kickstart 3.1 A500 ROM; SHA-256 `8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`.
- The frozen Search-based guest, compiler and SDK remain those recorded in the previous qualification; there was no guest rebuild for this follow-up.

Run `confirm-winuae.ps1` with the frozen stage, fresh `-EvidenceRoot`, `-Batches cross,large` and `-TimeoutSeconds 120` under PowerShell 7. Then run `qualify-winuae.py STAGE EVIDENCE_ROOT` to verify all old and new independent cases and regenerate this report. The [machine-readable receipt](exe2arc-winuae-cross-window-qualification-20261018.json) records each run, timing samples, hashes and full coverage checks.

## Remaining boundaries

There is no remaining WinUAE contract gap in this corpus. This remains original DOS CLI qualification, not MorphOS PPC or actual Workbench-message startup. No native allocation/I/O fault injection, instruction-level concurrent invocation, original-OS peak-stack measurement or CopperScreen run was added. The original report's vector-fixture and execution-cost qualifications remain applicable.
